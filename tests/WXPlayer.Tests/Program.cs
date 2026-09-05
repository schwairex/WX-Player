using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using WXPlayer.Core;

var results=new List<object>();int failures=0;
async Task Test(string name,Func<Task> test){var sw=Stopwatch.StartNew();try{await test();results.Add(new{name,passed=true,ms=sw.ElapsedMilliseconds});Console.WriteLine("PASS "+name+" "+sw.ElapsedMilliseconds+" ms");}catch(Exception ex){failures++;results.Add(new{name,passed=false,error=ex.ToString()});Console.WriteLine("FAIL "+name+": "+ex.Message);}}
void Assert(bool value,string why){if(!value)throw new Exception(why);}
async Task<List<T>> Collect<T>(IAsyncEnumerable<T> input){var list=new List<T>();await foreach(var x in input)list.Add(x);return list;}
var folder=Path.Combine(Path.GetTempPath(),"WXPlayer-Tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
await Test("Turkish search normalization",()=>{Assert(ContentItem.SearchKey("İSTANBUL ışık ŞÖLEN ÇAĞRI") == "istanbul isik solen cagri","Turkish insensitive search");return Task.CompletedTask;});
var source=new SourceConfig{Id="test",Name="Fixture",Address="https://example.test/list.m3u"};
await Test("M3U attributes, quoted commas, Unicode, headers, category and relative URL",async()=>
{
    var text="\uFEFF#EXTM3U x-tvg-url=\"https://example.test/epg.xml\"\n#EXTINF:-1 tvg-id=\"trt1\" group-title=\"Haber, Türkiye\" tvg-name=\"Türkçe\",TRT, Örnek\n#EXTVLCOPT:http-user-agent=WX Test\n#EXTVLCOPT:http-referrer=https://example.test/\nstream.ts\n";
    var s=source with{};var items=await Collect(PlaylistParser.ParseAsync(new StringReader(text),s));Assert(items.Count==1,"count");var i=items[0];Assert(i.Name=="TRT, Örnek"&&i.Category=="Haber, Türkiye"&&i.EpgId=="trt1","metadata");Assert(i.UserAgent=="WX Test"&&i.Referrer=="https://example.test/","headers");Assert(i.Url=="https://example.test/stream.ts","relative URL");Assert(s.EpgUrl.EndsWith("epg.xml"),"epg discovery");
});
await Test("TXT lists ignore unsafe protocols",async()=>{var i=await Collect(PlaylistParser.ParseAsync(new StringReader("https://example.test/a.ts\nftp://example.test/file\nrtsp://example.test/b\n"),source));Assert(i.Count==2,"unsafe protocol filtered");});
await Test("M3U8 HLS media and master manifest each produce one item",async()=>{foreach(var tag in new[]{"#EXT-X-TARGETDURATION:10","#EXT-X-STREAM-INF:BANDWIDTH=1200000"}){var i=await Collect(PlaylistParser.ParseAsync(new StringReader("#EXTM3U\n"+tag+"\n#EXTINF:10,\nseg1.ts\n#EXTINF:10,\nseg2.ts"),source));Assert(i.Count==1&&i[0].Url==source.Address,"manifest treated as stream");}});
await Test("Xtream get.php credentials and prefix parsing",()=>{var s=ProviderClient.ParseXtreamAddress(new SourceConfig{Address="https://example.test/iptv/get.php?username=u%2B1&password=p%26word&type=m3u_plus"});Assert(s.Username=="u+1"&&s.Password=="p&word"&&s.Address=="https://example.test/iptv","decoded credentials");return Task.CompletedTask;});
await Test("Source credentials Windows DPAPI round trip",()=>{var s=source with{Password="secret-test",Username="u"};var p=SecretVault.Protect(s);Assert(!p.Contains("secret-test")&&SecretVault.Unprotect(p).Password=="secret-test","DPAPI");return Task.CompletedTask;});
await Test("XMLTV timezone, Unicode, missing stop handling",async()=>{var xml="<tv><programme channel='trt1' start='20260905120000 +0300' stop='20260905130000 +0300'><title>Öğle</title><desc>Türkçe rehber</desc></programme><programme channel='x' start='20260905120000 +0300'><title>Invalid</title></programme></tv>";using var stream=new MemoryStream(Encoding.UTF8.GetBytes(xml));var list=await Collect(XmlTvParser.ParseAsync(stream));Assert(list.Count==1&&list[0].Start.UtcDateTime.Hour==9&&list[0].Title=="Öğle","XMLTV parsed");});
await Test("XMLTV external entity is rejected",async()=>{using var stream=new MemoryStream(Encoding.UTF8.GetBytes("<!DOCTYPE tv [<!ENTITY x SYSTEM 'file:///c:/windows/win.ini'>]><tv><programme>&x;</programme></tv>"));bool blocked=false;try{await Collect(XmlTvParser.ParseAsync(stream));}catch(System.Xml.XmlException){blocked=true;}Assert(blocked,"DTD blocked");});
var store=new LibraryStore(Path.Combine(folder,"library.db"));await store.InitializeAsync();
async IAsyncEnumerable<ContentItem> Large(int count,[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct=default){for(int i=0;i<count;i++){ct.ThrowIfCancellationRequested();yield return new ContentItem{Id=ContentItem.Key(source.Id,i.ToString()),SourceId=source.Id,ProviderId=i.ToString(),Name=$"Kanal {i:D6}",Category=i%2==0?"Haber":"Spor",Url=$"https://example.test/{i}.ts",Kind=ContentKind.Live,EpgId="epg-"+i};if(i%1000==0)await Task.Yield();}}
await Test("100,500 items asynchronous transactional import",async()=>{int n=await store.ImportAsync(source,Large(100500),null,default);Assert(n==100500,"import count");var stats=await store.StatsAsync(source.Id);Assert(stats.Live==100500,"stored count");});
await Test("Paged search and category filtering",async()=>{var p=await store.QueryAsync(source.Id,ContentKind.Live,"Haber","Kanal 099",false,false,0);Assert(p.Total==500&&p.Items.Count==150&&p.Items.All(x=>x.Category=="Haber"),"search/page");var second=await store.QueryAsync(source.Id,ContentKind.Live,"Haber","Kanal 099",false,false,150);Assert(second.Items[0].Id!=p.Items[0].Id,"page differs");});
await Test("Favorites and history survive reopening database",async()=>{string id=ContentItem.Key(source.Id,"50");await store.FavoriteAsync(id,true);await store.RememberAsync(id);var reopened=new LibraryStore(Path.Combine(folder,"library.db"));var p=await reopened.QueryAsync(null,null,null,"",true,true,0);Assert(p.Total==1&&p.Items[0].Id==id,"persistence");});
await Test("Cancelled import rolls back and preserves old library",async()=>{using var cancel=new CancellationTokenSource();async IAsyncEnumerable<ContentItem> Cancelled(){for(int i=0;i<600;i++){yield return new ContentItem{Id="new"+i,SourceId=source.Id,Name="New"};if(i==510)cancel.Cancel();await Task.Yield();}}bool cancelled=false;try{await store.ImportAsync(source,Cancelled(),null,cancel.Token);}catch(OperationCanceledException){cancelled=true;}Assert(cancelled&&(await store.StatsAsync(source.Id)).Live==100500,"atomic rollback");});
await Test("Empty import preserves library",async()=>{bool failed=false;try{await store.ImportAsync(source,Large(0),null,default);}catch(InvalidOperationException){failed=true;}Assert(failed&&(await store.StatsAsync(source.Id)).Live==100500,"empty rollback");});
await Test("SQL wildcard characters treated literally",async()=>{var p=await store.QueryAsync(source.Id,null,null,"%_",false,false,0);Assert(p.Total==0,"LIKE escaping");});
await Test("Cancellation requested before query",async()=>{using var cts=new CancellationTokenSource();cts.Cancel();bool stopped=false;try{await store.QueryAsync(null,null,null,"",false,false,0,150,cts.Token);}catch(OperationCanceledException){stopped=true;}Assert(stopped,"query cancellation");});
await Test("XMLTV import and channel match",async()=>{async IAsyncEnumerable<Programme> Entries(){yield return new("epg-50","Test programme","Description",DateTimeOffset.Now.AddHours(-1),DateTimeOffset.Now.AddHours(1));await Task.Yield();}await store.ImportEpgAsync(source.Id,Entries(),default);var p=await store.QueryAsync(source.Id,null,null,"Kanal 000050",false,false,0);var guide=await store.EpgAsync(p.Items[0],DateTimeOffset.Now.AddHours(-2));Assert(guide.Count==1&&guide[0].IsNow,"guide join");});
await Test("Catch-Up eligibility and encoded Xtream URL",()=>{var s=new SourceConfig{Kind=SourceKind.Xtream,Address="https://example.test",Username="u+",Password="p/"};var item=new ContentItem{ProviderId="42",CatchupDays=7};var start=DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds());var p=new Programme("","News","",start,start.AddHours(1));var target=ProviderClient.CatchupTarget(s,item,p);Assert(target.Url.Contains("/timeshift/u%2B/p%2F/60/")&&target.Url.EndsWith("/42.ts"),"catchup");bool blocked=false;try{ProviderClient.CatchupTarget(s,item,p with{Start=DateTimeOffset.Now.AddDays(-10)});}catch(InvalidOperationException){blocked=true;}Assert(blocked,"expired guide blocked");return Task.CompletedTask;});
await Test("Xtream mocked authentication/catalogue/episodes/EPG and resolution",async()=>
{
    using var client=new ProviderClient(new FixtureHandler(req=>
    {
        string q=req.RequestUri!.Query;
        if(q.Contains("get_series_info"))return "{\"episodes\":{\"1\":[{\"id\":\"44\",\"episode_num\":1,\"title\":\"Pilot\",\"container_extension\":\"mkv\"}]}}";
        if(q.Contains("get_short_epg"))return "{\"epg_listings\":[{\"title\":\"TmV3cw==\",\"description\":\"\",\"start_timestamp\":1000,\"stop_timestamp\":2000}]}";
        if(q.Contains("categories"))return "[{\"category_id\":\"1\",\"category_name\":\"Category\"}]";
        if(q.Contains("get_live_streams")||q.Contains("get_vod_streams"))return "[{\"stream_id\":42,\"name\":\"Test\",\"category_id\":\"1\",\"tv_archive\":1,\"tv_archive_duration\":7}]";
        if(q.Contains("get_series"))return "[{\"series_id\":43,\"name\":\"Series\"}]";
        return "{\"user_info\":{\"auth\":1}}";
    }));
    var s=new SourceConfig{Kind=SourceKind.Xtream,Address="https://example.test",Username="u",Password="p"};var items=await Collect(client.LoadAsync(s,default));Assert(items.Count==3&&items[0].Category=="Category"&&items[0].CatchupDays==7,"catalogue");var eps=await client.EpisodesAsync(s,items[2],default);Assert(eps.Count==1,"episodes");Assert((await client.ResolveAsync(s,eps[0],default)).Url.EndsWith("/series/u/p/44.mkv"),"episode URL");Assert((await client.ShortEpgAsync(s,items[0],default))[0].Title=="News","base64 epg");
});
await Test("Stalker mocked handshake, paginated lists, create_link",async()=>
{
    int livePages=0;using var client=new ProviderClient(new FixtureHandler(req=>
    {
        var q=req.RequestUri!.Query;
        Assert(req.Headers.Contains("Cookie"),"portal cookie");
        if(q.Contains("handshake"))return "{\"js\":{\"token\":\"fixture-token\"}}";
        Assert(req.Headers.Authorization?.Parameter=="fixture-token","bearer");
        if(q.Contains("get_profile"))return "{\"js\":{}}";
        if(q.Contains("get_genres")||q.Contains("get_categories"))return "{\"js\":[{\"id\":\"1\",\"title\":\"Test\"}]}";
        if(q.Contains("create_link"))return "{\"js\":{\"cmd\":\"ffmpeg https://example.test/live.ts\"}}";
        if(q.Contains("type=itv")){livePages++;return q.Contains("p=1")?"{\"js\":{\"total_items\":2,\"data\":[{\"id\":\"1\",\"name\":\"One\",\"cmd\":\"ffmpeg http://localhost/ch/1\"}]}}":"{\"js\":{\"total_items\":2,\"data\":[{\"id\":\"2\",\"name\":\"Two\",\"cmd\":\"ffmpeg http://localhost/ch/2\"}]}}";}
        return "{\"js\":{\"total_items\":0,\"data\":[]}}";
    }));
    var s=new SourceConfig{Kind=SourceKind.Stalker,Address="https://example.test/stalker_portal/c/",Mac="00:1A:79:00:00:01"};var items=await Collect(client.LoadAsync(s,default));Assert(items.Count==2&&livePages==2,"pagination");var target=await client.ResolveAsync(s,items[0],default);Assert(target.Url=="https://example.test/live.ts","resolved command; never executes shell");
});
await Test("Source deletion removes its content, favorites and guide",async()=>{await store.DeleteSourceAsync(source.Id);Assert((await store.StatsAsync(source.Id)).Total==0&&(await store.SourcesAsync()).Count==0,"source deletion");});
await RegressionTests.RunAsync(Test,Assert,folder);
Console.WriteLine($"{results.Count-failures}/{results.Count} passed");
string output=args.Length>0?Path.GetFullPath(args[0]):Path.Combine(folder,"results.json");Directory.CreateDirectory(Path.GetDirectoryName(output)!);File.WriteAllText(output,JsonSerializer.Serialize(new{passed=results.Count-failures,total=results.Count,results},new JsonSerializerOptions{WriteIndented=true}));
return failures==0?0:1;

sealed class FixtureHandler(Func<HttpRequestMessage,string> responder):HttpMessageHandler
{protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(responder(request),Encoding.UTF8,"application/json")});}

