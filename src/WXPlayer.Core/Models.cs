using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WXPlayer.Core;

public enum SourceKind { Playlist, Xtream, Stalker }
public enum ContentKind { Live, Movie, Series, Episode }
public sealed record SourceConfig
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Kütüphanem";
    public SourceKind Kind { get; set; }
    public string Address { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Mac { get; set; } = "";
    public string EpgUrl { get; set; } = "";
    public DateTimeOffset? UpdatedAt { get; set; }
    public override string ToString() => Name;
}
public sealed record ContentItem
{
    public string Id { get; init; } = "";
    public string SourceId { get; init; } = "";
    public string ProviderId { get; init; } = "";
    public string Name { get; init; } = "";
    public string Category { get; init; } = "Genel";
    public ContentKind Kind { get; init; }
    public string Url { get; init; } = "";
    public string Logo { get; init; } = "";
    public string EpgId { get; init; } = "";
    public string Extension { get; init; } = "mp4";
    public string Catchup { get; init; } = "";
    public int CatchupDays { get; init; }
    public string UserAgent { get; init; } = "";
    public string Referrer { get; init; } = "";
    public bool IsFavorite { get; set; }
    public string FavoriteGlyph => IsFavorite ? "★" : "☆";
    public string Initials => string.Concat(Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x => x[0])).ToUpperInvariant();
    public string KindLabel => Kind switch { ContentKind.Movie => "FİLM", ContentKind.Series => "DİZİ", ContentKind.Episode => "BÖLÜM", _ => "CANLI" };
    public static string Key(string source, string stable) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source + "|" + stable)))[..32];
    public static string SearchKey(string text) => string.Concat(text.Normalize(NormalizationForm.FormD).Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)).ToLowerInvariant().Replace('ı','i');
}
public sealed record Programme(string ChannelId, string Title, string Description, DateTimeOffset Start, DateTimeOffset End)
{
    public string TimeLabel => $"{Start.LocalDateTime:HH:mm} – {End.LocalDateTime:HH:mm}";
    public string DayLabel => Start.LocalDateTime.ToString("dd MMM, ddd");
    public bool IsNow => Start <= DateTimeOffset.Now && End > DateTimeOffset.Now;
    public string StateLabel => IsNow ? "ŞİMDİ" : End <= DateTimeOffset.Now ? "GEÇMİŞ" : "SIRADAKİ";
    public double Progress => Math.Clamp((DateTimeOffset.Now - Start).TotalSeconds / Math.Max(1, (End - Start).TotalSeconds) * 100, 0, 100);
}
public sealed record Page(IReadOnlyList<ContentItem> Items, int Total);
public sealed record ImportProgress(int Count, string Message);
public sealed record LibraryStats(int Live, int Movies, int Series, int Favorites)
{ public int Total => Live + Movies + Series; }
public sealed record PlaybackTarget(string Url, string UserAgent = "", string Referrer = "");

public sealed class PlayerSettings
{
    public bool HardwareAcceleration { get; set; } = true;
    public string VideoOutput { get; set; } = "direct3d11";
    public int NetworkCacheMs { get; set; } = 1200;
    public bool AdaptiveCache { get; set; } = true;
    public int Volume { get; set; } = 75;
    public string RecordingFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "WX Player");
}

public static class SecretVault
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("WXPlayer.Source.v1");
    public static string Protect(SourceConfig source) => Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(source)), Entropy, DataProtectionScope.CurrentUser));
    public static SourceConfig Unprotect(string data) => JsonSerializer.Deserialize<SourceConfig>(ProtectedData.Unprotect(Convert.FromBase64String(data), Entropy, DataProtectionScope.CurrentUser))!;
}

public static class AddressPolicy
{
    public static Uri Http(string address)
    {
        if (!Uri.TryCreate(address.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
            throw new InvalidOperationException("Geçerli bir http:// veya https:// adresi girin.");
        return uri;
    }
    public static bool IsPlayable(string address) => Uri.TryCreate(address, UriKind.Absolute, out var uri) && new[] { "https", "http", "rtsp", "rtmp", "udp", "rtp", "file" }.Contains(uri.Scheme);
    public static string Header(string text) => text.Replace("\r", "").Replace("\n", "");
}
