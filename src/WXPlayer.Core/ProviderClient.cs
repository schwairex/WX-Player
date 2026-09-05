using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace WXPlayer.Core;

public sealed partial class ProviderClient : IDisposable
{
    private readonly HttpClient _http;
    public ProviderClient(HttpMessageHandler? handler=null)
    {
        _http=handler is null?new HttpClient(new SocketsHttpHandler{AutomaticDecompression=DecompressionMethods.All,ConnectTimeout=TimeSpan.FromSeconds(15),PooledConnectionLifetime=TimeSpan.FromMinutes(5)}):new HttpClient(handler);
        _http.Timeout=TimeSpan.FromMinutes(5);_http.DefaultRequestHeaders.UserAgent.ParseAdd("WXPlayer/1.2");
    }
    public IAsyncEnumerable<ContentItem> LoadAsync(SourceConfig s,CancellationToken ct) => s.Kind switch
    { SourceKind.Xtream=>XtreamAsync(s,ct),SourceKind.Stalker=>StalkerAsync(s,ct),_=>PlaylistAsync(s,ct) };
    private async IAsyncEnumerable<ContentItem> PlaylistAsync(SourceConfig s,[EnumeratorCancellation]CancellationToken ct)
    {
        if(File.Exists(s.Address))
        {
            using var reader=new StreamReader(new FileStream(s.Address,FileMode.Open,FileAccess.Read,FileShare.Read,65536,true),Encoding.UTF8,true);
            await foreach(var item in PlaylistParser.ParseAsync(reader,s,ct))yield return item;
        }
        else
        {
            using var response=await _http.GetAsync(AddressPolicy.Http(s.Address),HttpCompletionOption.ResponseHeadersRead,ct);response.EnsureSuccessStatusCode();
            await using var stream=await response.Content.ReadAsStreamAsync(ct);using var reader=new StreamReader(stream,Encoding.UTF8,true,65536);
            await foreach(var item in PlaylistParser.ParseAsync(reader,s,ct))yield return item;
        }
    }
    public static SourceConfig ParseXtreamAddress(SourceConfig s)
    {
        var uri=AddressPolicy.Http(s.Address);
        var query=uri.Query.TrimStart('?').Split('&',StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Split('=',2)).Where(x=>x.Length==2).ToDictionary(x=>Uri.UnescapeDataString(x[0]),x=>Uri.UnescapeDataString(x[1].Replace("+"," ")),StringComparer.OrdinalIgnoreCase);
        s.Username=query.GetValueOrDefault("username",s.Username);s.Password=query.GetValueOrDefault("password",s.Password);
        string path=uri.AbsolutePath.TrimEnd('/');if(path.EndsWith(".php",StringComparison.OrdinalIgnoreCase))path=path[..(path.LastIndexOf('/')+1)].TrimEnd('/');
        s.Address=uri.GetLeftPart(UriPartial.Authority)+path;
        if(string.IsNullOrWhiteSpace(s.Username)||string.IsNullOrWhiteSpace(s.Password))throw new InvalidOperationException("Xtream kullanıcı adı ve şifresi gerekli.");return s;
    }
    private static string E(string text)=>Uri.EscapeDataString(text);
    public static string Api(SourceConfig s,string action="",string extra="")=>$"{s.Address.TrimEnd('/')}/player_api.php?username={E(s.Username)}&password={E(s.Password)}"+(action.Length>0?$"&action={action}":"")+extra;
    private async Task<JsonDocument> JsonAsync(string url,CancellationToken ct,HttpRequestMessage? request=null)
    {
        using var req=request??new HttpRequestMessage(HttpMethod.Get,AddressPolicy.Http(url));
        using var response=await _http.SendAsync(req,HttpCompletionOption.ResponseHeadersRead,ct);response.EnsureSuccessStatusCode();
        await using var stream=await response.Content.ReadAsStreamAsync(ct);return await JsonDocument.ParseAsync(stream,new JsonDocumentOptions{MaxDepth=64},ct);
    }
    public static string Str(JsonElement e,string key,string fallback="")=>e.ValueKind==JsonValueKind.Object&&e.TryGetProperty(key,out var v)&&v.ValueKind!=JsonValueKind.Null?v.ToString():fallback;
    private static int Num(JsonElement e,string key,int fallback=0)=>int.TryParse(Str(e,key),out var n)?n:fallback;
    private async IAsyncEnumerable<ContentItem> XtreamAsync(SourceConfig s,[EnumeratorCancellation]CancellationToken ct)
    {
        ParseXtreamAddress(s);
        using(var auth=await JsonAsync(Api(s),ct))
        { if(!auth.RootElement.TryGetProperty("user_info",out var info)||Str(info,"auth")!="1")throw new InvalidOperationException("Xtream oturumu açılamadı. Adres ve hesap bilgilerini kontrol edin."); }
        foreach(var (action,categories,kind) in new[]{("get_live_streams","get_live_categories",ContentKind.Live),("get_vod_streams","get_vod_categories",ContentKind.Movie),("get_series","get_series_categories",ContentKind.Series)})
        {
            var groups=new Dictionary<string,string>();
            using(var doc=await JsonAsync(Api(s,categories),ct)) if(doc.RootElement.ValueKind==JsonValueKind.Array)foreach(var c in doc.RootElement.EnumerateArray())groups[Str(c,"category_id")]=Str(c,"category_name","Genel");
            using var response=await _http.GetAsync(Api(s,action),HttpCompletionOption.ResponseHeadersRead,ct);response.EnsureSuccessStatusCode();
            await using var stream=await response.Content.ReadAsStreamAsync(ct);
            await foreach(var row in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(stream,cancellationToken:ct))
            {
                ct.ThrowIfCancellationRequested();string id=Str(row,kind==ContentKind.Series?"series_id":"stream_id");if(id.Length==0)continue;
                yield return new ContentItem{Id=ContentItem.Key(s.Id,$"{kind}:{id}"),SourceId=s.Id,ProviderId=id,Name=Str(row,"name","İsimsiz"),Category=groups.GetValueOrDefault(Str(row,"category_id"),"Genel"),Kind=kind,Logo=Str(row,kind==ContentKind.Series?"cover":"stream_icon"),EpgId=Str(row,"epg_channel_id"),Extension=Str(row,"container_extension","mp4"),CatchupDays=Num(row,"tv_archive")==1?Num(row,"tv_archive_duration",1):0};
            }
        }
        if(s.EpgUrl.Length==0)s.EpgUrl=$"{s.Address.TrimEnd('/')}/xmltv.php?username={E(s.Username)}&password={E(s.Password)}";
    }
    public async Task<List<ContentItem>> EpisodesAsync(SourceConfig s,ContentItem series,CancellationToken ct)
    {
        if(s.Kind==SourceKind.Stalker)return await StalkerEpisodesAsync(s,series,ct);
        if(s.Kind!=SourceKind.Xtream)return [series];
        using var doc=await JsonAsync(Api(s,"get_series_info","&series_id="+E(series.ProviderId)),ct);
        var list=new List<ContentItem>();
        if(!doc.RootElement.TryGetProperty("episodes",out var seasons))return list;
        IEnumerable<(string Season,JsonElement Episodes)> entries=seasons.ValueKind==JsonValueKind.Object?seasons.EnumerateObject().Select(x=>(x.Name,x.Value)):seasons.ValueKind==JsonValueKind.Array?seasons.EnumerateArray().Select((x,i)=>(i.ToString(),x)):[];
        foreach(var (season,episodes) in entries)
        {
            if(episodes.ValueKind!=JsonValueKind.Array)continue;
            foreach(var row in episodes.EnumerateArray())
            {
                var id=Str(row,"id");if(id.Length==0)continue;
                list.Add(new ContentItem{Id=ContentItem.Key(s.Id,"episode:"+id),SourceId=s.Id,ProviderId=id,Name=$"S{season.PadLeft(2,'0')} · B{Str(row,"episode_num").PadLeft(2,'0')}   {Str(row,"title",series.Name)}",Category=series.Name,Kind=ContentKind.Episode,Extension=Str(row,"container_extension","mp4")});
            }
        }return list;
    }
    public async Task<PlaybackTarget> ResolveAsync(SourceConfig s,ContentItem item,CancellationToken ct)
    {
        if(s.Kind==SourceKind.Stalker)return await ResolveStalkerAsync(s,item,ct);
        if(s.Kind!=SourceKind.Xtream)return new(item.Url,item.UserAgent,item.Referrer);
        var type=item.Kind switch{ContentKind.Movie=>"movie",ContentKind.Episode=>"series",_=>"live"};
        var ext=item.Kind==ContentKind.Live?"ts":item.Extension.TrimStart('.');if(!System.Text.RegularExpressions.Regex.IsMatch(ext,"^[a-zA-Z0-9]{1,8}$"))ext="mp4";
        return new($"{s.Address.TrimEnd('/')}/{type}/{E(s.Username)}/{E(s.Password)}/{E(item.ProviderId)}.{ext}");
    }
    public static PlaybackTarget CatchupTarget(SourceConfig s,ContentItem item,Programme p)
    {
        if(item.CatchupDays<=0||p.Start>=DateTimeOffset.Now||p.Start<DateTimeOffset.Now.AddDays(-item.CatchupDays))throw new InvalidOperationException("Bu program için sağlayıcının tekrar izleme aralığı kullanılamıyor.");
        if(s.Kind==SourceKind.Xtream)
        {int minutes=Math.Max(1,(int)Math.Ceiling((p.End-p.Start).TotalMinutes));return new($"{s.Address.TrimEnd('/')}/timeshift/{E(s.Username)}/{E(s.Password)}/{minutes}/{p.Start.UtcDateTime:yyyy-MM-dd:HH-mm}/{E(item.ProviderId)}.ts");}
        if(item.Catchup.Length>0)
        {
            string url=item.Catchup.Replace("{utc}",p.Start.ToUnixTimeSeconds().ToString()).Replace("{utcend}",p.End.ToUnixTimeSeconds().ToString()).Replace("{duration}",((int)(p.End-p.Start).TotalSeconds).ToString()).Replace("${start}",p.Start.ToUnixTimeSeconds().ToString()).Replace("${end}",p.End.ToUnixTimeSeconds().ToString());
            if(url.Contains('{')||!AddressPolicy.IsPlayable(url))throw new InvalidOperationException("Bu sağlayıcının Catch-Up şablonu desteklenmiyor.");return new(url,item.UserAgent,item.Referrer);
        }
        throw new InvalidOperationException("Sağlayıcı bu içerik için tekrar izleme bağlantısı sunmuyor.");
    }

    private static string Portal(SourceConfig s)
    {
        var uri=AddressPolicy.Http(s.Address);var url=uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        if(url.EndsWith(".php",StringComparison.OrdinalIgnoreCase))return url;
        if(url.EndsWith("/c",StringComparison.OrdinalIgnoreCase))url=url[..^2];return url+"/server/load.php";
    }
    private async Task<JsonDocument> PortalCallAsync(SourceConfig s,string token,string type,string action,string extra,CancellationToken ct)
    {
        var url=$"{Portal(s)}?type={type}&action={action}&JsHttpRequest=1-xml{extra}";
        var req=new HttpRequestMessage(HttpMethod.Get,url);
        req.Headers.Add("Cookie",$"mac={E(s.Mac)}; stb_lang=en; timezone=Europe%2FIstanbul");
        req.Headers.TryAddWithoutValidation("X-User-Agent","Model: MAG250; Link: WiFi");
        if(token.Length>0)req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
        return await JsonAsync(url,ct,req);
    }
    private static JsonElement Js(JsonDocument doc)=>doc.RootElement.TryGetProperty("js",out var js)?js:doc.RootElement;
    private async Task<string> HandshakeAsync(SourceConfig s,CancellationToken ct)
    {
        if(!System.Text.RegularExpressions.Regex.IsMatch(s.Mac,"^([0-9a-fA-F]{2}:){5}[0-9a-fA-F]{2}$"))throw new InvalidOperationException("Sağlayıcınızın tanımladığı geçerli MAC adresini girin.");
        using var d=await PortalCallAsync(s,"","stb","handshake","",ct);string token=Str(Js(d),"token");
        if(token.Length==0)throw new InvalidOperationException("Portal oturumu açılamadı. Portal URL ve tanımlı MAC adresini kontrol edin.");
        using var profile=await PortalCallAsync(s,token,"stb","get_profile","&hd=1&stb_type=MAG250&image_version=218",ct);return token;
    }
    private async IAsyncEnumerable<ContentItem> StalkerAsync(SourceConfig s,[EnumeratorCancellation]CancellationToken ct)
    {
        var token=await HandshakeAsync(s,ct);
        foreach(var(type,kind) in new[]{("itv",ContentKind.Live),("vod",ContentKind.Movie),("series",ContentKind.Series)})
        {
            var groups=new Dictionary<string,string>();
            using(var d=await PortalCallAsync(s,token,type,type=="itv"?"get_genres":"get_categories","",ct))
            {var js=Js(d);if(js.ValueKind==JsonValueKind.Array)foreach(var g in js.EnumerateArray())groups[Str(g,"id")]=Str(g,"title","Genel");}
            int page=1,loaded=0;var seen=new HashSet<string>();
            while(true)
            {
                ct.ThrowIfCancellationRequested();
                using var d=await PortalCallAsync(s,token,type,"get_ordered_list",$"&p={page}&genre=*&category=*&sortby=number",ct);
                var js=Js(d);if(js.ValueKind!=JsonValueKind.Object||!js.TryGetProperty("data",out var rows)||rows.ValueKind!=JsonValueKind.Array)break;
                int added=0;
                foreach(var row in rows.EnumerateArray())
                {
                    string id=Str(row,"id");if(id.Length==0||!seen.Add(id))continue;added++;
                    yield return new ContentItem{Id=ContentItem.Key(s.Id,type+":"+id),SourceId=s.Id,ProviderId=id,Name=Str(row,"name","İsimsiz"),Category=groups.GetValueOrDefault(Str(row,type=="itv"?"tv_genre_id":"category_id"),"Genel"),Kind=kind,Url=Str(row,"cmd"),Logo=Str(row,"logo",Str(row,"screenshot_uri")),EpgId=Str(row,"xmltv_id"),CatchupDays=0};
                }
                loaded+=added;if(added==0||(Num(js,"total_items")>0&&loaded>=Num(js,"total_items")))break;
                if(++page>10000)throw new InvalidOperationException("Portal sayfa sınırını aştı; içe aktarma durduruldu.");
            }
        }
    }
    private async Task<List<ContentItem>> StalkerEpisodesAsync(SourceConfig s,ContentItem series,CancellationToken ct)
    {
        string token=await HandshakeAsync(s,ct);using var d=await PortalCallAsync(s,token,"series","get_ordered_list","&movie_id="+E(series.ProviderId)+"&p=1",ct);
        var js=Js(d);var list=new List<ContentItem>();
        if(js.ValueKind==JsonValueKind.Object&&js.TryGetProperty("data",out var rows)&&rows.ValueKind==JsonValueKind.Array)foreach(var r in rows.EnumerateArray())
        {
            var id=Str(r,"id");var cmd=Str(r,"cmd");
            if(r.TryGetProperty("series",out var episodes)&&episodes.ValueKind==JsonValueKind.Array)
            {foreach(var episode in episodes.EnumerateArray()){string number=episode.ToString();list.Add(new ContentItem{Id=ContentItem.Key(s.Id,"episode:"+id+":"+number),SourceId=s.Id,ProviderId=number,Name=Str(r,"name",series.Name)+" · Bölüm "+number,Category=series.Name,Kind=ContentKind.Episode,Url=cmd});}}
            else if(cmd.Length>0)list.Add(new ContentItem{Id=ContentItem.Key(s.Id,"episode:"+id),SourceId=s.Id,ProviderId=id,Name=Str(r,"name",series.Name),Category=series.Name,Kind=ContentKind.Episode,Url=cmd});
        }
        return list;
    }
    private async Task<PlaybackTarget> ResolveStalkerAsync(SourceConfig s,ContentItem item,CancellationToken ct)
    {
        string token=await HandshakeAsync(s,ct),type=item.Kind==ContentKind.Live?"itv":"vod";
        using var d=await PortalCallAsync(s,token,type,"create_link","&cmd="+E(item.Url)+(item.Kind==ContentKind.Episode?"&series="+E(item.ProviderId):""),ct);
        string cmd=Str(Js(d),"cmd").Trim();if(cmd.StartsWith("ffmpeg ",StringComparison.OrdinalIgnoreCase)||cmd.StartsWith("ffrt ",StringComparison.OrdinalIgnoreCase))cmd=cmd[(cmd.IndexOf(' ')+1)..].Trim();
        if(!AddressPolicy.IsPlayable(cmd))throw new InvalidOperationException("Portal oynatılabilir bir bağlantı döndürmedi.");return new(cmd,"Mozilla/5.0 (QtEmbedded; U; Linux; C) AppleWebKit/533.3 MAG250 stbapp ver: 4 rev: 1812");
    }
    public void Dispose()=>_http.Dispose();
}

