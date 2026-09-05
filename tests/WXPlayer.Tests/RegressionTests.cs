using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using WXPlayer.Core;

internal static class RegressionTests
{
    internal static async Task RunAsync(Func<string,Func<Task>,Task> test,Action<bool,string> assert,string folder)
    {
        async Task<List<T>> Collect<T>(IAsyncEnumerable<T> input){var list=new List<T>();await foreach(var item in input)list.Add(item);return list;}
        string Xml(string id="guide.tr",string name="TRT 1",string title="Günün haberi")=>$"<?xml version='1.0'?><!DOCTYPE tv SYSTEM 'https://invalid.test/xmltv.dtd'><tv><channel id='{id}'><display-name>{name}</display-name></channel><programme channel='{id}' start='202609050900 +0300' stop='20260905100000 +0300'><title>{title}</title></programme></tv>";
        var day=new DateTimeOffset(2026,9,5,0,0,0,TimeSpan.FromHours(3));
        await test("XMLTV standard DOCTYPE accepted without external fetch; abbreviated timezone dates",async()=>{using var stream=new MemoryStream(Encoding.UTF8.GetBytes(Xml()));var entries=await Collect(XmlTvParser.ReadAsync(stream));assert(entries.Count==2&&entries[0].Names![0]=="TRT 1"&&entries[1].Programme!.Start.UtcDateTime.Hour==6,"channel+programme metadata");});
        await test("Missing XMLTV stop inferred only from next same-channel programme",async()=>{string xml="<tv><programme channel='a' start='202609050900 +0300'><title>A</title></programme><programme channel='b' start='202609050930 +0300' stop='202609051000 +0300'/><programme channel='a' start='202609051000 +0300' stop='202609051100 +0300'/></tv>";using var stream=new MemoryStream(Encoding.UTF8.GetBytes(xml));var list=await Collect(XmlTvParser.ParseAsync(stream));assert(list.Count==3&&list.Single(p=>p.Title=="A").End.Hour==10,"same-channel inferred end");});
        await test("Playlist EPG header relative path and tvg-name are preserved",async()=>{var source=new SourceConfig{Address="https://example.test/folder/list.m3u"};var items=await Collect(PlaylistParser.ParseAsync(new StringReader("#EXTM3U X-TVG-URL='guide.xml.gz'\n#EXTINF:-1 tvg-name='TRT 1',TR: TRT 1 FHD\nhttps://example.test/1.ts"),source));assert(source.EpgUrl=="https://example.test/folder/guide.xml.gz"&&items[0].EpgName=="TRT 1","metadata discovery");});
        var store=new LibraryStore(Path.Combine(folder,"epg-v12.db"));await store.InitializeAsync();
        async Task Import(string source,string xml){using var stream=new MemoryStream(Encoding.UTF8.GetBytes(xml));await store.ImportXmlTvAsync(source,XmlTvParser.ReadAsync(stream),default);}
        await Import("a",Xml());
        await test("Channel Matcher exact ID, normalized names and tvg-name fallback",async()=>
        {
            foreach(var item in new[]{new ContentItem{SourceId="a",EpgId="guide.tr"},new ContentItem{SourceId="a",EpgId="GUIDE.TR"},new ContentItem{SourceId="a",Name="TR: TRT 1 FHD"},new ContentItem{SourceId="a",Name="Provider decoration",EpgName="TRT 1"}})
                assert((await store.EpgAsync(item,day)).Single().Title=="Günün haberi","expected matched programme");
        });
        await test("Channel Matcher source isolation and ambiguous alias rejection; manual override",async()=>
        {
            string xml=Xml().Replace("</tv>","<channel id='other'><display-name>TRT 1</display-name></channel><programme channel='other' start='202609050900 +0300' stop='202609051000 +0300'><title>Other</title></programme></tv>");await Import("b",xml);
            var ambiguous=new ContentItem{Id="manual",SourceId="b",Name="TRT 1"};assert((await store.EpgAsync(ambiguous,day)).Count==0,"ambiguous names must not guess");
            assert((await store.EpgAsync(new ContentItem{SourceId="unknown",EpgId="guide.tr"},day)).Count==0,"source isolation");
            await store.SetEpgMatchAsync(ambiguous.Id,"other");assert((await store.EpgAsync(ambiguous,day)).Single().Title=="Other","manual mapping");await store.SetEpgMatchAsync(ambiguous.Id,"");assert((await store.EpgAsync(ambiguous,day)).Count==0,"auto reset");
        });
        await test("Failed XMLTV refresh retains previous programmes, aliases and timestamp",async()=>
        {
            var before=await store.EpgUpdatedAsync("a");bool failed=false;try{await Import("a","<tv><channel id='wrong'/><programme");}catch(System.Xml.XmlException){failed=true;}
            assert(failed&&(await store.EpgAsync(new ContentItem{SourceId="a",Name="TRT 1"},day)).Count==1&&await store.EpgUpdatedAsync("a")==before,"atomic cache retention");
        });
        await test("Gzip signature detected on .php and already-decompressed .gz accepted",async()=>
        {
            using var raw=new MemoryStream();using(var gzip=new GZipStream(raw,CompressionMode.Compress,true))gzip.Write(Encoding.UTF8.GetBytes(Xml()));
            using var provider=new ProviderClient(new Handler((req,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(req.RequestUri!.AbsolutePath.EndsWith(".php")?raw.ToArray():Encoding.UTF8.GetBytes(Xml()))})));
            foreach(string endpoint in new[]{"guide.php","guide.xml.gz"})assert(await provider.LoadEpgAsync(new SourceConfig{Id="gzip",EpgUrl="https://example.test/"+endpoint},store,default)==1,"compressed import");
        });
        await test("EPG background refresh deduplicates downloads and observes cache freshness",async()=>
        {
            int count=0;using var provider=new ProviderClient(new Handler(async(req,ct)=>{Interlocked.Increment(ref count);await Task.Delay(150,ct);return new(HttpStatusCode.OK){Content=new StringContent(Xml())};}));
            var service=new EpgService(provider,store,default);var source=new SourceConfig{Id="singleflight",EpgUrl="https://example.test/epg"};await Task.WhenAll(Enumerable.Range(0,12).Select(_=>service.RefreshAsync(source)));await service.RefreshAsync(source);assert(count==1,"one request for concurrent refresh and fresh cache");await service.StopAsync();
        });
        await test("Xtream M3U get.php EPG discovery and stream ID extraction",async()=>
        {
            using var provider=new ProviderClient(new Handler((req,_)=>{assert(req.RequestUri!.Query.Contains("stream_id=987"),"URL stream id, not playlist ordinal");return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"epg_listings\":[{\"title\":\"TmV3cw==\",\"start_timestamp\":1000,\"end_timestamp\":2000}]}")});}));
            var source=new SourceConfig{Address="https://example.test/get.php?username=user&password=pass"};await provider.DiscoverEpgAsync(source,default);assert(source.EpgUrl.Contains("xmltv.php?username=user&password=pass"),"derived XMLTV");var list=await provider.ShortEpgAsync(source,new ContentItem{ProviderId="3",Url="https://example.test/live/user/pass/987.ts"},default);assert(list.Single().Title=="News","short EPG from M3U account");
        });
        await test("Xtream fallback endpoint, plain UTF8 title, server timezone",async()=>
        {
            using var provider=new ProviderClient(new Handler((req,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(req.RequestUri!.Query.Contains("get_short_epg")?"{}":req.RequestUri.Query.Contains("get_simple_data_table")?"{\"epg_listings\":[{\"title\":\"Türkçe Haber\",\"start\":\"2026-09-05 09:00:00\",\"end\":\"2026-09-05 10:00:00\"}]}":"{\"server_info\":{\"timezone\":\"Europe/Istanbul\"}}")})));
            var list=await provider.ShortEpgAsync(new SourceConfig{Kind=SourceKind.Xtream,Address="https://example.test",Username="u",Password="p"},new ContentItem{ProviderId="1"},default);assert(list.Single().Title=="Türkçe Haber"&&list[0].Start.UtcDateTime.Hour==6,"fallback+timezone");
        });
        await test("v1.1 database migration preserves sources, favorites, history and adds tvg-name",async()=>
        {
            string path=Path.Combine(folder,"old-schema.db");using(var c=new SqliteConnection("Data Source="+path)){c.Open();using var cmd=c.CreateCommand();cmd.CommandText="CREATE TABLE items(id TEXT PRIMARY KEY,source TEXT,provider TEXT,name TEXT,category TEXT,kind INTEGER,url TEXT,logo TEXT,epg TEXT,extension TEXT,catchup TEXT,days INTEGER,ua TEXT,referrer TEXT,search_text TEXT);";cmd.ExecuteNonQuery();}
            var migrated=new LibraryStore(path);await migrated.InitializeAsync();async IAsyncEnumerable<ContentItem> Items(){yield return new(){Id="old",SourceId="old",Name="TRT 1",EpgName="Guide name"};await Task.Yield();}await migrated.ImportAsync(new SourceConfig{Id="old"},Items(),null,default);await migrated.FavoriteAsync("old",true);await migrated.RememberAsync("old");await migrated.InitializeAsync();var found=await migrated.QueryAsync(null,null,null,"",true,true,0);assert(found.Total==1&&found.Items[0].EpgName=="Guide name","migration persisted data");
            await migrated.ClearAsync(LibraryCleanup.Favorites);assert((await migrated.QueryAsync(null,null,null,"",true,false,0)).Total==0&&(await migrated.QueryAsync(null,null,null,"",false,true,0)).Total==1,"favorites only");await migrated.ClearAsync(LibraryCleanup.History);assert((await migrated.StatsAsync(null)).Total==1&&(await migrated.QueryAsync(null,null,null,"",false,true,0)).Total==0,"history only");await migrated.ClearAsync(LibraryCleanup.Sources);assert((await migrated.SourcesAsync()).Count==0&&(await migrated.StatsAsync(null)).Total==0,"all sources");
        });
        byte[] payload=new byte[4096];new Random(42).NextBytes(payload);payload[0]=(byte)'M';payload[1]=(byte)'Z';string hash=Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        string Release(string tag="v1.3.0",string? digest=null,bool prerelease=false,string? url=null)=>JsonSerializer.Serialize(new{tag_name=tag,draft=false,prerelease,assets=new[]{new{name="WXPlayer.exe",size=payload.Length,digest=digest??"sha256:"+hash,browser_download_url=url??"https://github.com/schwairex/WX-Player/releases/download/"+tag+"/WXPlayer.exe"}}});
        await test("Updater semantic versions reject downgrade, prerelease and unrelated assets",()=>
        {
            assert(GitHubUpdater.ParseRelease(Release(),new(1,2,0))!.Version==new Version(1,3,0),"newer");assert(GitHubUpdater.ParseRelease(Release("v1.10.0"),new(1,9,0)) is not null,"numeric version");assert(GitHubUpdater.ParseRelease(Release("v1.1.0"),new(1,2,0)) is null&&GitHubUpdater.ParseRelease(Release(prerelease:true),new(1,2,0)) is null,"older/prerelease excluded");bool rejected=false;try{GitHubUpdater.ParseRelease(Release(url:"https://evil.test/WXPlayer.exe"),new(1,2,0));}catch(InvalidOperationException){rejected=true;}assert(rejected,"foreign asset rejected");return Task.CompletedTask;
        });
        await test("Updater download verifies hash and size then atomically stages executable",async()=>
        {
            using var updater=new GitHubUpdater(new Handler((req,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=req.RequestUri!.Host=="api.github.com"?new StringContent(Release()):new ByteArrayContent(payload)})));
            var release=await updater.CheckAsync(new(1,2,0),default);var ready=await updater.DownloadAsync(release!,Path.Combine(folder,"updates-ok"),null,default);assert(File.Exists(ready.Path)&&ready.Sha256==hash&&!File.Exists(UpdateActivation.Pointer(Path.Combine(folder,"updates-ok"))),"staged but not activated");
            bool mismatch=false;try{await UpdateActivation.ActivateAsync(Path.Combine(folder,"updates-ok"),ready,new(1,2,0));}catch(InvalidOperationException){mismatch=true;}assert(mismatch,"mismatched app version not activated");await UpdateActivation.ActivateAsync(Path.Combine(folder,"updates-ok"),ready,new(1,3,0));assert(UpdateActivation.Read(Path.Combine(folder,"updates-ok"),new(1,2,0))?.Path==ready.Path,"verified forward pointer");File.AppendAllText(ready.Path,"corruption");assert(UpdateActivation.Read(Path.Combine(folder,"updates-ok"),new(1,2,0)) is null,"tampered cached update rejected");
        });
        await test("Updater corrupt, truncated and cancelled downloads keep active version unchanged",async()=>
        {
            foreach(string mode in new[]{"corrupt","truncated","cancelled"})
            {
                string data=Path.Combine(folder,"bad-"+mode);Directory.CreateDirectory(data);File.WriteAllText(UpdateActivation.Pointer(data),"existing version");using var cancel=new CancellationTokenSource();
                byte[] bytes=payload.ToArray();if(mode=="corrupt")bytes[40]^=1;if(mode=="truncated")bytes=bytes[..1024];if(mode=="cancelled")cancel.Cancel();
                using var updater=new GitHubUpdater(new Handler((_,ct)=>{ct.ThrowIfCancellationRequested();return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(bytes)});}));
                bool failed=false;try{await updater.DownloadAsync(GitHubUpdater.ParseRelease(Release(),new(1,2,0))!,data,null,cancel.Token);}catch(Exception e)when(e is InvalidOperationException or OperationCanceledException){failed=true;}
                assert(failed&&File.ReadAllText(UpdateActivation.Pointer(data))=="existing version"&&!Directory.EnumerateFiles(Path.Combine(data,"updates")).Any(),"failed download never activates and removes partial");
            }
        });
        await test("Updater SHA256SUMS fallback works when API digest is missing",async()=>
        {
            string json=JsonSerializer.Serialize(new{tag_name="v1.3.0",draft=false,prerelease=false,assets=new object[]{new{name="WXPlayer.exe",size=payload.Length,browser_download_url="https://github.com/schwairex/WX-Player/releases/download/v1.3.0/WXPlayer.exe"},new{name="SHA256SUMS.txt",browser_download_url="https://github.com/schwairex/WX-Player/releases/download/v1.3.0/SHA256SUMS.txt"}}});
            using var updater=new GitHubUpdater(new Handler((req,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=req.RequestUri!.AbsolutePath.EndsWith(".txt")?new StringContent(hash+"  WXPlayer.exe\n"):new ByteArrayContent(payload)})));
            var ready=await updater.DownloadAsync(GitHubUpdater.ParseRelease(json,new(1,2,0))!,Path.Combine(folder,"checksum-fallback"),null,default);assert(ready.Sha256==hash,"checksum verified");
        });
        await test("Updater 404, offline, and rate limit never stage an update",async()=>
        {
            foreach(var status in new[]{HttpStatusCode.NotFound,HttpStatusCode.Forbidden,HttpStatusCode.ServiceUnavailable})
            {
                using var updater=new GitHubUpdater(new Handler((_,_)=>Task.FromResult(new HttpResponseMessage(status))));try{assert(await updater.CheckAsync(new(1,2,0),default) is null,"404 no release");}catch(HttpRequestException){assert(status!=HttpStatusCode.NotFound,"request failure surfaced for retry");}
            }
        });
    }
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> response):HttpMessageHandler
    {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)=>response(request,cancellationToken);}
}
