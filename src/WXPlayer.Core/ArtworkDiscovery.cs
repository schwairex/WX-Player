using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WXPlayer.Core;

public sealed record DiscoveredArtwork(string Url, string Provider, string PageUrl, DateTimeOffset Expires);

/// <summary>Public metadata only: never sends source URLs, credentials or provider category names.</summary>
public sealed class ArtworkDiscovery : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _directory;
    private readonly CancellationTokenSource _life = new();
    private readonly ConcurrentDictionary<string,Lazy<Task<DiscoveredArtwork?>>> _pending = new();
    private readonly SemaphoreSlim _slots = new(4), _pace = new(1), _indexLock = new(1);
    private readonly Dictionary<string,DateTimeOffset> _nextRequest = new();
    private JsonDocument? _channels, _logos;
    private DateTimeOffset _indexExpires;
    private DateTimeOffset _indexRetry;
    private Dictionary<string,string[]> _channelNames=new();
    private Dictionary<string,string> _channelLogos=new();
    private HashSet<string> _channelIds=new(StringComparer.OrdinalIgnoreCase);
    public ArtworkDiscovery(string directory,HttpMessageHandler? handler=null)
    {
        _directory=directory;
        _http=handler is null?new HttpClient():new HttpClient(handler);
        _http.Timeout=TimeSpan.FromSeconds(9);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("WXPlayer/1.6.0 (+https://github.com/schwairex/WX-Player)");
    }
    public static string CleanTitle(string title)
    {
        title=Regex.Replace(title,@"^\s*(?:\[[^\]]{1,32}\]\s*|(?:TR|EN|US|UK|DE|FR)\s*[:|]\s*)+","",RegexOptions.IgnoreCase);
        title=Regex.Replace(title,@"^\s*(?:TR|EN|US|UK|DE|FR)\s*[✦★|:-]\s*","",RegexOptions.IgnoreCase);
        title=Regex.Replace(title,@"\b(?:S\d{1,3}(?:[ ._-]*[EB]\d{1,4})?|\d{1,2}x\d{1,3})\b.*$","",RegexOptions.IgnoreCase);
        title=Regex.Replace(title,@"\b(?:FHD|UHD|HD|SD|4K|8K|RAW|HEVC|H264|H265|1080p|720p|2160p)\b","",RegexOptions.IgnoreCase);
        title=Regex.Replace(title,@"\((?:19|20)\d{2}\)|\[(?:19|20)\d{2}\]","");
        title=Regex.Replace(title,@"[\[\]()]+", " ");
        return Regex.Replace(title,@"\s+"," ").Trim(' ','-','|','·',':');
    }
    public static string MatchKey(string title)=>Regex.Replace(ContentItem.SearchKey(CleanTitle(title)),@"[^\p{L}\p{N}]","");
    private static string Year(string title)=>Regex.Match(title,@"[\[(]((?:19|20)\d{2})[\])]").Groups[1].Value;
    private static string String(JsonElement e,string name)=>e.ValueKind==JsonValueKind.Object&&e.TryGetProperty(name,out var value)&&value.ValueKind==JsonValueKind.String?value.GetString()??"":"";
    private static bool Https(string url)=>Uri.TryCreate(url,UriKind.Absolute,out var u)&&u.Scheme=="https";
    private static bool Missing(string logo)=>string.IsNullOrWhiteSpace(logo)||logo.Trim() is "null" or "-" or "N/A";

    public async Task<DiscoveredArtwork?> ResolveAsync(ContentItem item,CancellationToken ct=default)
    {
        if(!Missing(item.Logo))return null;
        string name=item.Kind==ContentKind.Episode&&item.SeriesName.Length>0?item.SeriesName:item.Name;
        string title=CleanTitle(name);if(title.Length<2||title.Length>180||title.Contains("://")||title.Contains('@'))return null;
        var kind=item.Kind==ContentKind.Episode?ContentKind.Series:item.Kind;
        string key=ContentItem.Key("artwork-v1",kind+"|"+MatchKey(title)+"|"+Year(name)+"|"+(kind==ContentKind.Live?item.EpgId:""));
        ct.ThrowIfCancellationRequested();
        if(_pending.Count>=64&&!_pending.ContainsKey(key))return null;
        var lazy=_pending.GetOrAdd(key,_=>new Lazy<Task<DiscoveredArtwork?>>(()=>Task.Run(()=>FindAsync(key,title,Year(name),kind,item.EpgId))));
        var task=lazy.Value;
        _=task.ContinueWith(_=>_pending.TryRemove(new KeyValuePair<string,Lazy<Task<DiscoveredArtwork?>>>(key,lazy)),TaskScheduler.Default);
        return await task.WaitAsync(ct);
    }
    private async Task<DiscoveredArtwork?> FindAsync(string key,string title,string year,ContentKind kind,string epg)
    {
        var ct=_life.Token;string path=Path.Combine(_directory,key+".json");
        try
        {
            if(File.Exists(path)&&new FileInfo(path).Length<8192)
            {var cached=JsonSerializer.Deserialize<DiscoveredArtwork>(await File.ReadAllTextAsync(path,ct));if(cached is not null&&cached.Expires>DateTimeOffset.UtcNow)return cached.Url.Length>0?cached:null;}
        }catch(Exception ex) when(ex is IOException or JsonException or UnauthorizedAccessException){}
        await _slots.WaitAsync(ct);
        DiscoveredArtwork? result=null;bool failed=false;
        try
        {
            try
            {
            if(kind==ContentKind.Live)result=await ChannelAsync(title,epg,ct);
            else if(kind==ContentKind.Series)
            {
                using var shows=await JsonAsync("https://api.tvmaze.com/search/shows?q="+Uri.EscapeDataString(title),ct);
                var matches=shows.RootElement.EnumerateArray().Select(x=>x.GetProperty("show"))
                    .Where(x=>MatchKey(String(x,"name"))==MatchKey(title)&&(year.Length==0||String(x,"premiered").StartsWith(year,StringComparison.Ordinal))).ToArray();
                if(matches.Length==1&&matches[0].TryGetProperty("image",out var img))
                {string url=String(img,"original");if(Https(url))result=new(url,"TVmaze",String(matches[0],"url"),DateTimeOffset.UtcNow.AddDays(30));}
            }
            }catch(Exception ex) when(ex is HttpRequestException or JsonException or OperationCanceledException or InvalidOperationException or IOException){failed=true;}
            if(result is null&&!ct.IsCancellationRequested)result=await WikipediaAsync(title,year,kind,ct);
        }
        catch(Exception ex) when(ex is HttpRequestException or JsonException or OperationCanceledException or InvalidOperationException or IOException){failed=true;}
        finally{_slots.Release();}
        try
        {
            Directory.CreateDirectory(_directory);string temp=path+".tmp";
            var entry=result??new DiscoveredArtwork("","","",DateTimeOffset.UtcNow.Add(failed?TimeSpan.FromMinutes(5):TimeSpan.FromHours(24)));
            await File.WriteAllTextAsync(temp,JsonSerializer.Serialize(entry),ct);File.Move(temp,path,true);
        }catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or OperationCanceledException){}
        return result;
    }
    private async Task<JsonDocument> JsonAsync(string url,CancellationToken ct,int limit=2_000_000)
    {
        string host=new Uri(url).Host;
        await _pace.WaitAsync(ct);
        try
        {
            var wait=_nextRequest.GetValueOrDefault(host)-DateTimeOffset.UtcNow;
            if(wait>TimeSpan.Zero)await Task.Delay(wait,ct);
            _nextRequest[host]=DateTimeOffset.UtcNow.AddMilliseconds(600);
        }finally{_pace.Release();}
        using var response=await _http.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,ct);
        if(response.StatusCode==HttpStatusCode.TooManyRequests)
        {
            await _pace.WaitAsync(ct);try{_nextRequest[host]=DateTimeOffset.UtcNow.AddSeconds(15);}finally{_pace.Release();}
        }
        response.EnsureSuccessStatusCode();
        if(response.Content.Headers.ContentLength>limit)throw new IOException("Metadata size limit");
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(10));
        await using var input=await response.Content.ReadAsStreamAsync(timeout.Token);using var output=new MemoryStream();var buffer=new byte[16384];
        int count;while((count=await input.ReadAsync(buffer,timeout.Token))>0){if(output.Length+count>limit)throw new IOException("Metadata size limit");output.Write(buffer,0,count);}
        return JsonDocument.Parse(output.ToArray());
    }
    private async Task<DiscoveredArtwork?> WikipediaAsync(string title,string year,ContentKind kind,CancellationToken ct)
    {
        foreach(string language in new[]{"tr","en"})
        {
            string query=title+(kind==ContentKind.Movie?" film":kind==ContentKind.Series?language=="tr"?" dizi":" television series":" television channel");
            string url=$"https://{language}.wikipedia.org/w/api.php?action=query&format=json&formatversion=2&generator=search&gsrnamespace=0&gsrlimit=5&gsrsearch={Uri.EscapeDataString(query)}&prop=pageimages%7Cpageprops%7Cextracts%7Cinfo&piprop=thumbnail&pithumbsize=600&pilicense=any&exintro=1&explaintext=1&exsentences=2&inprop=url";
            using var doc=await JsonAsync(url,ct);
            if(!doc.RootElement.TryGetProperty("query",out var q)||!q.TryGetProperty("pages",out var pages))continue;
            var matches=new List<JsonElement>();
            foreach(var page in pages.EnumerateArray())
            {
                if(page.TryGetProperty("pageprops",out var props)&&props.TryGetProperty("disambiguation",out _))continue;
                string pageTitle=String(page,"title");string baseTitle=Regex.Replace(pageTitle,@"\s*\([^)]*\)\s*$","");
                var qualifier=Regex.Match(pageTitle,@"\(([^)]*)\)\s*$");
                if(qualifier.Success&&!Regex.IsMatch(qualifier.Groups[1].Value,@"^(?:19|20)\d{2}$|\b(film|filmi|dizi|dizisi|series|channel)$",RegexOptions.IgnoreCase))continue;
                if(MatchKey(baseTitle)!=MatchKey(title))continue;
                string description=String(props,"wikibase-shortdesc");
                string extract=ContentItem.SearchKey(description.Length>0?description:String(page,"extract"));
                string type=kind==ContentKind.Movie?@"\b(film|filmi|filmidir|filmdir|movie)\b":kind==ContentKind.Series?@"\b(series|dizi|dizisi|dizisidir)\b":@"\b(channel|kanal|kanali|kanalidir|television network)\b";
                if(!Regex.IsMatch(extract,type)||year.Length>0&&!extract.Contains(year)&&!pageTitle.Contains(year))continue;
                if(page.TryGetProperty("thumbnail",out var thumb)&&Https(String(thumb,"source")))matches.Add(page);
            }
            if(matches.Count==1)return new(String(matches[0].GetProperty("thumbnail"),"source"),"Wikipedia",String(matches[0],"fullurl"),DateTimeOffset.UtcNow.AddDays(30));
        }
        return null;
    }
    private async Task<DiscoveredArtwork?> ChannelAsync(string title,string epg,CancellationToken ct)
    {
        await _indexLock.WaitAsync(ct);
        try
        {
            if(_channels is null||_indexExpires<=DateTimeOffset.UtcNow)
            {
                if(_indexRetry>DateTimeOffset.UtcNow)return null;
                _indexRetry=DateTimeOffset.UtcNow.AddMinutes(5);
                var channels=await IndexAsync("channels",ct);JsonDocument logos;
                try{logos=await IndexAsync("logos",ct);}catch{channels.Dispose();throw;}
                _channels?.Dispose();_logos?.Dispose();_channels=channels;_logos=logos;_indexExpires=DateTimeOffset.UtcNow.AddDays(1);
                var names=new Dictionary<string,List<string>>();_channelIds.Clear();
                foreach(var c in _channels.RootElement.EnumerateArray())
                {
                    string id=String(c,"id");_channelIds.Add(id);
                    var aliases=new List<string>{String(c,"name")};if(c.TryGetProperty("alt_names",out var alt)&&alt.ValueKind==JsonValueKind.Array)aliases.AddRange(alt.EnumerateArray().Select(n=>n.GetString()??""));
                    foreach(string name in aliases){string normalized=MatchKey(name);if(normalized.Length==0)continue;if(!names.TryGetValue(normalized,out var ids))names[normalized]=ids=[];if(!ids.Contains(id))ids.Add(id);}
                }
                _channelNames=names.ToDictionary(p=>p.Key,p=>p.Value.ToArray());_channelLogos.Clear();
                foreach(var l in _logos.RootElement.EnumerateArray())
                    if((!l.TryGetProperty("in_use",out var use)||use.ValueKind!=JsonValueKind.False)&&String(l,"feed").Length==0&&new[]{"PNG","JPEG","GIF","APNG"}.Contains(String(l,"format").ToUpperInvariant())&&Https(String(l,"url")))_channelLogos.TryAdd(String(l,"channel"),String(l,"url"));
            }
            var matched=_channelIds.TryGetValue(epg,out string? exact)?new[]{exact}:_channelNames.GetValueOrDefault(MatchKey(title))??[];
            if(matched.Length!=1)return null;
            return !_channelLogos.TryGetValue(matched[0],out var logo)?null:new(logo,"IPTV-org","https://github.com/iptv-org/database",DateTimeOffset.UtcNow.AddDays(7));
        }finally{_indexLock.Release();}
    }
    private async Task<JsonDocument> IndexAsync(string name,CancellationToken ct)
    {
        string path=Path.Combine(_directory,name+".json");
        try{if(File.Exists(path)&&File.GetLastWriteTimeUtc(path)>DateTime.UtcNow.AddDays(-1)&&new FileInfo(path).Length<20_000_000)return JsonDocument.Parse(await File.ReadAllBytesAsync(path,ct));}catch(Exception ex) when(ex is IOException or JsonException){}
        var doc=await JsonAsync("https://iptv-org.github.io/api/"+name+".json",ct,20_000_000);
        try{Directory.CreateDirectory(_directory);await File.WriteAllTextAsync(path+".tmp",doc.RootElement.GetRawText(),ct);File.Move(path+".tmp",path,true);}catch(IOException){}
        return doc;
    }
    public void Dispose(){_life.Cancel();_http.Dispose();}
}
