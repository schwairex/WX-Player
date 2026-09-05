using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WXPlayer.Core;

public sealed record ReleaseUpdate(Version Version,string Tag,string AssetName,Uri Download,long Size,string? Sha256,Uri? Checksums);
public sealed record PreparedUpdate(Version Version,string Path,string Sha256);

public sealed class GitHubUpdater : IDisposable
{
    public const string Repository="schwairex/WX-Player";
    public const string LatestUrl="https://api.github.com/repos/"+Repository+"/releases/latest";
    private readonly HttpClient _http;
    private string? _etag;
    public GitHubUpdater(HttpMessageHandler? handler=null)
    {
        _http=handler is null?new HttpClient(new SocketsHttpHandler{AutomaticDecompression=DecompressionMethods.All,ConnectTimeout=TimeSpan.FromSeconds(20)}):new HttpClient(handler);
        _http.Timeout=TimeSpan.FromMinutes(20);_http.DefaultRequestHeaders.UserAgent.ParseAdd("WXPlayer/1.2");_http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }
    public static bool TryVersion(string? tag,out Version version)
    {
        version=new(0,0,0);if(tag is null||!Regex.IsMatch(tag,@"^[vV]?\d+\.\d+(\.\d+)?$"))return false;
        if(!Version.TryParse(tag.TrimStart('v','V'),out var v))return false;
        version=new(v.Major,v.Minor,Math.Max(0,v.Build));return true;
    }
    private static Uri AssetUrl(string address)
    {
        var uri=new Uri(address,UriKind.Absolute);
        if(uri.Scheme!="https"||uri.Host!="github.com"||!uri.IsDefaultPort||uri.UserInfo.Length>0||!uri.AbsolutePath.StartsWith("/"+Repository+"/releases/download/",StringComparison.Ordinal))throw new InvalidOperationException("Güncelleme dosyası beklenen GitHub deposuna ait değil.");
        return uri;
    }
    public static ReleaseUpdate? ParseRelease(string json,Version current)
    {
        using var doc=JsonDocument.Parse(json);var root=doc.RootElement;
        if(root.GetProperty("draft").GetBoolean()||root.GetProperty("prerelease").GetBoolean())return null;
        string tag=root.GetProperty("tag_name").GetString()??"";if(!TryVersion(tag,out var version)||version<=new Version(current.Major,current.Minor,Math.Max(0,current.Build)))return null;
        var assets=root.GetProperty("assets").EnumerateArray().ToArray();
        var choices=assets.Where(a=>Regex.IsMatch(a.GetProperty("name").GetString()??"",@"^WXPlayer(?:-\d+\.\d+\.\d+)?(?:-win-x64)?\.exe$",RegexOptions.IgnoreCase)).ToArray();
        if(choices.Length!=1)throw new InvalidOperationException("Sürümde tek bir WXPlayer Windows x64 EXE dosyası bulunmalı.");
        var asset=choices[0];long size=asset.GetProperty("size").GetInt64();if(size<1024||size>1024L*1024*1024)throw new InvalidOperationException("Güncelleme boyutu geçerli değil.");
        string? digest=asset.TryGetProperty("digest",out var d)?d.GetString():null;
        string? hash=digest is not null&&Regex.IsMatch(digest,@"^sha256:[a-fA-F0-9]{64}$")?digest[7..].ToLowerInvariant():null;
        Uri? sums=null;foreach(var a in assets)if(Regex.IsMatch(a.GetProperty("name").GetString()??"",@"^SHA256SUMS(?:-\d+\.\d+\.\d+)?\.txt$",RegexOptions.IgnoreCase))sums=AssetUrl(a.GetProperty("browser_download_url").GetString()!);
        if(hash is null&&sums is null)throw new InvalidOperationException("Güncelleme için SHA-256 doğrulama bilgisi eksik.");
        return new(version,tag,asset.GetProperty("name").GetString()!,AssetUrl(asset.GetProperty("browser_download_url").GetString()!),size,hash,sums);
    }
    public async Task<ReleaseUpdate?> CheckAsync(Version current,CancellationToken ct,bool force=false)
    {
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(35));
        using var request=new HttpRequestMessage(HttpMethod.Get,LatestUrl);request.Headers.Add("X-GitHub-Api-Version","2022-11-28");if(!force&&_etag is not null)request.Headers.TryAddWithoutValidation("If-None-Match",_etag);
        using var response=await _http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,timeout.Token);
        if(response.StatusCode is HttpStatusCode.NotModified or HttpStatusCode.NotFound)return null;
        response.EnsureSuccessStatusCode();string json=await LimitedTextAsync(response.Content,2*1024*1024,timeout.Token);
        var update=ParseRelease(json,current);if(update is null)_etag=response.Headers.ETag?.ToString();return update;
    }
    private static async Task<string> LimitedTextAsync(HttpContent content,int limit,CancellationToken ct)
    {
        await using var stream=await content.ReadAsStreamAsync(ct);using var output=new MemoryStream();byte[] buffer=new byte[8192];int read;
        while((read=await stream.ReadAsync(buffer,ct))>0){if(output.Length+read>limit)throw new InvalidOperationException("Sunucu yanıtı boyut sınırını aşıyor.");output.Write(buffer,0,read);}return System.Text.Encoding.UTF8.GetString(output.ToArray());
    }
    public async Task<PreparedUpdate> DownloadAsync(ReleaseUpdate update,string dataDirectory,IProgress<int>? progress,CancellationToken ct)
    {
        string? expected=update.Sha256;
        if(expected is null)
        {
            using var sums=await _http.GetAsync(update.Checksums!,HttpCompletionOption.ResponseHeadersRead,ct);sums.EnsureSuccessStatusCode();
            string text=await LimitedTextAsync(sums.Content,65536,ct);
            foreach(string line in text.Split('\n')){var m=Regex.Match(line.Trim(),@"^([a-fA-F0-9]{64})\s+\*?(.+)$");if(m.Success&&m.Groups[2].Value==update.AssetName)expected=m.Groups[1].Value.ToLowerInvariant();}
        }
        if(expected is null)throw new InvalidOperationException("EXE için SHA-256 özeti bulunamadı.");
        string root=Path.Combine(dataDirectory,"updates");Directory.CreateDirectory(root);
        string final=Path.Combine(root,$"WXPlayer-{update.Version}-{expected[..12]}.exe"),temp=final+"."+Guid.NewGuid().ToString("N")+".partial";
        if(File.Exists(final)&&await HashAsync(final,ct)==expected)return new(update.Version,final,expected);
        try
        {
            using var response=await _http.GetAsync(update.Download,HttpCompletionOption.ResponseHeadersRead,ct);response.EnsureSuccessStatusCode();
            await using(var input=await response.Content.ReadAsStreamAsync(ct))await using(var output=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None,65536,true))
            {
                byte[] buffer=new byte[65536];long total=0;int n,last=-1;
                while((n=await input.ReadAsync(buffer,ct))>0){total+=n;if(total>update.Size)throw new InvalidOperationException("İndirilen dosyanın boyutu beklenenden farklı.");await output.WriteAsync(buffer.AsMemory(0,n),ct);int percent=(int)(total*100/update.Size);if(percent!=last){last=percent;progress?.Report(percent);}}
                if(total!=update.Size)throw new InvalidOperationException("Güncelleme indirmesi eksik kaldı.");
            }
            if(await HashAsync(temp,ct)!=expected)throw new InvalidOperationException("Güncelleme doğrulanamadı. Mevcut sürüm korunuyor.");
            await using(var exe=File.OpenRead(temp)){if(exe.ReadByte()!='M'||exe.ReadByte()!='Z')throw new InvalidOperationException("İndirilen dosya Windows EXE değil.");}
            File.Move(temp,final,true);return new(update.Version,final,expected);
        }
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public static async Task<string> HashAsync(string path,CancellationToken ct){await using var stream=File.OpenRead(path);return Convert.ToHexString(await SHA256.HashDataAsync(stream,ct)).ToLowerInvariant();}
    public void Dispose()=>_http.Dispose();
}

public static class UpdateActivation
{
    public static string Pointer(string data)=>Path.Combine(data,"active-update.txt");
    public static bool IsManagedPath(string data,string path)=>Path.GetFullPath(path).StartsWith(Path.GetFullPath(Path.Combine(data,"updates"))+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&Path.GetExtension(path).Equals(".exe",StringComparison.OrdinalIgnoreCase);
    public static PreparedUpdate? Read(string data,Version current)
    {
        try
        {
            var lines=File.ReadAllLines(Pointer(data));if(lines.Length!=3||!GitHubUpdater.TryVersion(lines[0],out var version)||version<=new Version(current.Major,current.Minor,Math.Max(0,current.Build))||!Regex.IsMatch(lines[1],"^[a-f0-9]{64}$")||!IsManagedPath(data,lines[2]))return null;
            using var stream=File.OpenRead(lines[2]);if(Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant()!=lines[1])return null;
            return new(version,lines[2],lines[1]);
        }catch{return null;}
    }
    public static async Task ActivateAsync(string data,PreparedUpdate update,Version running,CancellationToken ct=default)
    {
        if(new Version(running.Major,running.Minor,Math.Max(0,running.Build))!=update.Version||!IsManagedPath(data,update.Path)||await GitHubUpdater.HashAsync(update.Path,ct)!=update.Sha256)throw new InvalidOperationException("Yeni sürüm başlatma doğrulaması başarısız.");
        string path=Pointer(data),temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{await File.WriteAllLinesAsync(temp,[update.Version.ToString(),update.Sha256,Path.GetFullPath(update.Path)],ct);File.Move(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}
    }
}
