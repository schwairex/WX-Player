using System.Collections.Concurrent;
using System.Net.Http;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;

namespace WXPlayer.App;

// One network transfer per URL across all views/sizes; bounded decoded LRU and persistent byte cache.
internal static class ArtworkCache
{
    private const long MemoryBudget = 64L * 1024 * 1024, DiskBudget = 192L * 1024 * 1024;
    private const int MaxDownload = 8 * 1024 * 1024;
    private sealed record Frame(BitmapSource Bitmap, long Bytes, LinkedListNode<string> Node);
    private static readonly object Gate = new();
    private static readonly Dictionary<string, Frame> Frames = new();
    private static readonly LinkedList<string> Recent = new();
    private static readonly ConcurrentDictionary<string, Lazy<Task<BitmapSource?>>> Decoding = new();
    private static readonly ConcurrentDictionary<string, Lazy<Task<byte[]?>>> Downloads = new();
    private static readonly ConcurrentDictionary<string, DateTime> Failed = new();
    private static readonly SemaphoreSlim Work = new(12), Network = new(12), DecodeSlots = new(4);
    private static readonly HttpClient Client = new(new SocketsHttpHandler { MaxConnectionsPerServer = 12, PooledConnectionLifetime = TimeSpan.FromMinutes(10), AutomaticDecompression = System.Net.DecompressionMethods.All }) { Timeout = TimeSpan.FromSeconds(6) };
    private static long _memory;
    private static int _writes;
    private static string Folder => Path.Combine(App.DataDirectory, "artwork-cache");
    internal static bool HasAddress(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
    private static string Key(string url, int width) => Math.Clamp(width, 96, 960) + "|" + url;
    private static string FileKey(string url) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(url))).ToLowerInvariant();
    internal static bool TryGet(string url, int width, out BitmapSource? bitmap)
    {
        lock (Gate)
        {
            if (Frames.TryGetValue(Key(url, width), out var frame)) { Recent.Remove(frame.Node); Recent.AddLast(frame.Node); bitmap = frame.Bitmap; return true; }
        }
        bitmap = null; return false;
    }
    internal static async Task<BitmapSource?> GetAsync(string url, int width)
    {
        if (!HasAddress(url)) return null;
        if (TryGet(url, width, out var cached)) return cached;
        if (Failed.TryGetValue(url, out var until) && until > DateTime.UtcNow) return null;
        string key = Key(url, width);
        var operation = Decoding.GetOrAdd(key, _ => new Lazy<Task<BitmapSource?>>(() => Task.Run(() => DecodeAsync(url, width))));
        try { return await operation.Value.ConfigureAwait(false); }
        finally { Decoding.TryRemove(new KeyValuePair<string, Lazy<Task<BitmapSource?>>>(key, operation)); }
    }
    private static async Task<BitmapSource?> DecodeAsync(string url, int width)
    {
        await Work.WaitAsync().ConfigureAwait(false);
        try
        {
            var operation = Downloads.GetOrAdd(url, _ => new Lazy<Task<byte[]?>>(() => ReadBytesAsync(url)));
            byte[]? bytes;
            try { bytes = await operation.Value.ConfigureAwait(false); }
            finally { Downloads.TryRemove(new KeyValuePair<string, Lazy<Task<byte[]?>>>(url, operation)); }
            if (bytes is null) { MarkFailed(url); return null; }
            await DecodeSlots.WaitAsync().ConfigureAwait(false);
            BitmapSource bitmap;
            try
            {
                bitmap = await Task.Run(() =>
                {
                    using var data = new MemoryStream(bytes, false);
                    var decoder = BitmapDecoder.Create(data, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    int naturalWidth = decoder.Frames[0].PixelWidth, naturalHeight = decoder.Frames[0].PixelHeight;
                    int targetWidth = Math.Clamp(width, 96, 960);
                    targetWidth = Math.Max(1, Math.Min(targetWidth, (int)(1024d * naturalWidth / Math.Max(1, naturalHeight))));
                    data.Position = 0;
                    var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad;
                    image.DecodePixelWidth = targetWidth; image.StreamSource = data; image.EndInit(); image.Freeze(); return (BitmapSource)image;
                }).ConfigureAwait(false);
            }
            finally { DecodeSlots.Release(); }
            string key = Key(url, width); long size = (long)bitmap.PixelWidth * bitmap.PixelHeight * 4;
            lock (Gate)
            {
                if (!Frames.ContainsKey(key)) { var node = Recent.AddLast(key); Frames[key] = new Frame(bitmap, size, node); _memory += size; }
                while (_memory > MemoryBudget && Recent.First is { } oldest)
                { var old = Frames[oldest.Value]; _memory -= old.Bytes; Frames.Remove(oldest.Value); Recent.RemoveFirst(); }
            }
            Failed.TryRemove(url, out _); return bitmap;
        }
        catch { try { File.Delete(Path.Combine(Folder, FileKey(url) + ".img")); } catch (IOException) { } catch (UnauthorizedAccessException) { } MarkFailed(url); return null; }
        finally { Work.Release(); }
    }
    private static void MarkFailed(string url)
    {
        if (Failed.Count > 512) foreach (var item in Failed.Where(p => p.Value <= DateTime.UtcNow)) Failed.TryRemove(item.Key, out _);
        if (Failed.Count >= 512) foreach (var item in Failed.OrderBy(p => p.Value).Take(128)) Failed.TryRemove(item.Key, out _);
        Failed[url] = DateTime.UtcNow.AddSeconds(30);
    }
    private static async Task<byte[]?> ReadBytesAsync(string url)
    {
        string path = Path.Combine(Folder, FileKey(url) + ".img");
        try
        {
            var info = new FileInfo(path);
            if (info.Exists && info.Length is > 0 and <= MaxDownload && info.LastWriteTimeUtc > DateTime.UtcNow.AddDays(-7))
                return await File.ReadAllBytesAsync(path).ConfigureAwait(false);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        await Network.WaitAsync().ConfigureAwait(false);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            using var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaxDownload) return null;
            await using var input = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            using var data = new MemoryStream(); byte[] buffer = new byte[16384]; int count;
            while ((count = await input.ReadAsync(buffer, timeout.Token).ConfigureAwait(false)) > 0)
            { if (data.Length + count > MaxDownload) return null; data.Write(buffer, 0, count); }
            byte[] bytes = data.ToArray();
            try
            {
                Directory.CreateDirectory(Folder);
                string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try { await File.WriteAllBytesAsync(temp, bytes).ConfigureAwait(false); File.Move(temp, path, true); }
                finally { if (File.Exists(temp)) File.Delete(temp); }
                if (Interlocked.Increment(ref _writes) % 16 == 1) TrimDisk();
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return bytes;
        }
        catch { return null; }
        finally { Network.Release(); }
    }
    private static void TrimDisk()
    {
        try
        {
            var files = new DirectoryInfo(Folder).EnumerateFiles("*.img").OrderBy(f => f.LastWriteTimeUtc).ToArray();
            long bytes = files.Sum(f => f.Length);
            foreach (var file in files)
            {
                if (bytes <= DiskBudget && file.LastWriteTimeUtc > DateTime.UtcNow.AddDays(-7)) break;
                long size = file.Length; file.Delete(); bytes -= size;
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
    internal static void ClearMemoryForTest() { lock (Gate) { Frames.Clear(); Recent.Clear(); _memory = 0; } }
    internal static long MemoryBytes { get { lock (Gate) return _memory; } }
}


