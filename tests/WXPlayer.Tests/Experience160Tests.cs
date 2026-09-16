using System.Net;
using System.Text;
using System.Text.Json;
using WXPlayer.Core;

internal static class Experience160Tests
{
    internal static async Task RunAsync(Func<string,Func<Task>,Task> test,Action<bool,string> assert,string folder)
    {
        ContentItem Ep(int season,int episode)=>new(){Id=$"s{season}e{episode}",SourceId="source",SeriesId="show",Kind=ContentKind.Episode,Season=season,Episode=episode,Name=$"Episode {episode}"};
        ContentItem[] episodes=[Ep(2,2),Ep(1,10),Ep(2,1),Ep(1,2),Ep(1,1)];
        await test("Episode navigation orders numbers and crosses seasons without wrapping",()=>
        {assert(EpisodeNavigation.Adjacent(Ep(1,2),episodes,1)?.Episode==10,"numeric order");assert(EpisodeNavigation.Adjacent(Ep(1,10),episodes,1)?.Season==2,"next season");assert(EpisodeNavigation.Adjacent(Ep(2,1),episodes,-1)?.Episode==10,"previous season");assert(EpisodeNavigation.Adjacent(Ep(2,2),episodes,1) is null&&EpisodeNavigation.Adjacent(Ep(1,1),episodes,-1) is null,"ends don't wrap");return Task.CompletedTask;});
        await test("Episode navigation isolates source and series; live/movie unaffected",()=>
        {var mixed=episodes.Concat([Ep(1,3) with{SourceId="other"},Ep(1,4) with{SeriesId="other"}]);assert(EpisodeNavigation.Adjacent(Ep(1,2),mixed,1)?.Episode==10,"isolation");assert(EpisodeNavigation.Adjacent(Ep(1,1) with{Kind=ContentKind.Live},episodes,1) is null,"live");assert(EpisodeNavigation.Adjacent(Ep(1,1) with{Kind=ContentKind.Movie},episodes,1) is null,"movie");return Task.CompletedTask;});
        await test("Next episode offer has exact 30 second boundary and handles unknown duration",()=>
        {var a=Ep(1,1);var b=Ep(1,2);assert(!EpisodeNavigation.OfferNext(a,b,true,69999,100000),"before threshold");assert(EpisodeNavigation.OfferNext(a,b,true,70000,100000),"30s");assert(EpisodeNavigation.OfferNext(a,b,true,100000,100000),"end");assert(!EpisodeNavigation.OfferNext(a,b,false,70000,100000)&&!EpisodeNavigation.OfferNext(a,b,true,0,0)&&!EpisodeNavigation.OfferNext(a,b,true,-1,100000)&&!EpisodeNavigation.OfferNext(a,null,true,90000,100000),"invalid/last/window");return Task.CompletedTask;});
        await test("Artwork identity removes provider/quality markers while retaining channel number",()=>
        {assert(ArtworkDiscovery.MatchKey("[TR] Breaking Bad (2008) S01.E02 FHD")=="breakingbad","series clean");assert(ArtworkDiscovery.MatchKey("TR ✦ TRT 1 HD")=="trt1","channel clean");assert(ArtworkDiscovery.MatchKey("TRT 1")!=ArtworkDiscovery.MatchKey("TRT 2"),"number");return Task.CompletedTask;});
        await test("Artwork preserves source image and never sends credentials or stream URL",async()=>
        {
            using var h=new Handler(_=>throw new Exception("No request expected"));using var discovery=new ArtworkDiscovery(Path.Combine(folder,"art-preserve"),h);
            assert(await discovery.ResolveAsync(new(){Name="Show",Logo="https://source.test/poster.jpg"}) is null,"preserve");
            assert(await discovery.ResolveAsync(new(){Name="https://private.test/u/p",Url="https://private.test/u/p"}) is null,"unsafe title");assert(h.Count==0,"no network");
        });
        await test("TVmaze artwork deduplicates concurrent requests and survives restart offline",async()=>
        {
            string dir=Path.Combine(folder,"art-single");using var h=new Handler(req=>{assert(!req.RequestUri!.ToString().Contains("secret"),"metadata only");return Shows(("Example",2020,"https://images.test/poster.jpg"));});
            using(var discovery=new ArtworkDiscovery(dir,h))
            {var item=new ContentItem{Name="Example (2020)",Kind=ContentKind.Series,Url="https://secret.test/u/password"};var result=await Task.WhenAll(Enumerable.Range(0,12).Select(_=>discovery.ResolveAsync(item)));assert(result.All(x=>x?.Url=="https://images.test/poster.jpg")&&h.Count==1,"single request");}
            using var offline=new ArtworkDiscovery(dir,new Handler(_=>throw new HttpRequestException("offline")));assert((await offline.ResolveAsync(new(){Name="Example (2020)",Kind=ContentKind.Series}))?.Provider=="TVmaze","disk");
        });
        await test("Same-name shows require a unique year match",async()=>
        {
            using var h=new Handler(req=>req.RequestUri!.Host=="api.tvmaze.com"?Shows(("Example",1990,"https://images.test/old.jpg"),("Example",2020,"https://images.test/new.jpg")):"{}");
            using var discovery=new ArtworkDiscovery(Path.Combine(folder,"art-years"),h);
            assert((await discovery.ResolveAsync(new(){Name="Example (2020)",Kind=ContentKind.Series}))?.Url.EndsWith("new.jpg")==true,"year");
            assert(await discovery.ResolveAsync(new(){Name="Example",Kind=ContentKind.Series}) is null,"ambiguous names");
        });
        await test("Wikipedia chooses the named film, rejects actor/disambiguation/wrong year",async()=>
        {
            using var h=new Handler(_=>Wiki(new[]{new{title="Example (film)",extract="Example is a 2024 film.",fullurl="https://en.wikipedia.org/wiki/Example",thumbnail=new{source="https://images.test/movie.jpg"}},new{title="Actor",extract="An actor in a 2024 film.",fullurl="https://en.wikipedia.org/wiki/Actor",thumbnail=new{source="https://images.test/actor.jpg"}},new{title="Example (soundtrack)",extract="The soundtrack of the 2024 film Example.",fullurl="https://en.wikipedia.org/wiki/Soundtrack",thumbnail=new{source="https://images.test/album.jpg"}}}));
            using var discovery=new ArtworkDiscovery(Path.Combine(folder,"art-wiki"),h);
            assert((await discovery.ResolveAsync(new(){Name="Example (2024)",Kind=ContentKind.Movie}))?.Url.EndsWith("movie.jpg")==true,"film match");
            assert(await discovery.ResolveAsync(new(){Name="Example (1998)",Kind=ContentKind.Movie}) is null,"wrong year");
        });
        await test("Channel logo matches EPG ID and reuses downloaded index",async()=>
        {
            using var h=new Handler(req=>req.RequestUri!.AbsolutePath.EndsWith("channels.json")?"[{\"id\":\"WX1.tr\",\"name\":\"WX 1\",\"alt_names\":[]},{\"id\":\"WX2.tr\",\"name\":\"WX 2\",\"alt_names\":[]}]":"[{\"channel\":\"WX1.tr\",\"feed\":null,\"in_use\":true,\"format\":\"PNG\",\"url\":\"https://images.test/1.png\"},{\"channel\":\"WX2.tr\",\"feed\":null,\"in_use\":true,\"format\":\"PNG\",\"url\":\"https://images.test/2.png\"}]");
            using var discovery=new ArtworkDiscovery(Path.Combine(folder,"art-channels"),h);
            assert((await discovery.ResolveAsync(new(){Name="Different name",EpgId="WX1.tr",Kind=ContentKind.Live}))?.Url.EndsWith("1.png")==true,"ID");
            assert((await discovery.ResolveAsync(new(){Name="WX 2 HD",Kind=ContentKind.Live}))?.Url.EndsWith("2.png")==true&&h.Count==2,"index reuse");
        });
        await test("Unmatched artwork is cached; cancelling one waiter preserves the other",async()=>
        {
            using var h=new Handler(_=>"[]");using var discovery=new ArtworkDiscovery(Path.Combine(folder,"art-cancel"),h);
            var item=new ContentItem{Name="Missing title",Kind=ContentKind.Series};using var cancel=new CancellationTokenSource();
            var one=discovery.ResolveAsync(item,cancel.Token);var two=discovery.ResolveAsync(item);cancel.Cancel();
            bool cancelled=false;try{await one;}catch(OperationCanceledException){cancelled=true;}
            assert(cancelled&&await two is null,"independent cancellation");int count=h.Count;assert(await discovery.ResolveAsync(item) is null&&h.Count==count,"negative cached");
        });
        await test("Mutable artwork does not change content identity",()=>
        {var item=Ep(1,1);int hash=item.GetHashCode();bool notified=false;item.PropertyChanged+=(_,e)=>notified|=e.PropertyName=="Logo";item.Logo="https://images.test/image.jpg";assert(item.GetHashCode()==hash&&notified,"binding and identity");return Task.CompletedTask;});
    }
    private static string Shows(params (string Name,int Year,string Url)[] shows)=>JsonSerializer.Serialize(shows.Select(s=>new{show=new{name=s.Name,premiered=s.Year+"-01-01",url="https://www.tvmaze.com/shows/1/example",image=new{original=s.Url}}}));
    private static string Wiki(object pages)=>JsonSerializer.Serialize(new{query=new{pages}});
    private sealed class Handler(Func<HttpRequestMessage,string> response):HttpMessageHandler
    {
        private int _count;internal int Count=>_count;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {Interlocked.Increment(ref _count);await Task.Delay(25,ct);return new(HttpStatusCode.OK){Content=new StringContent(response(request),Encoding.UTF8,"application/json")};}
    }
    internal static async Task LiveAsync(string folder)
    {
        using var discovery=new ArtworkDiscovery(Path.Combine(folder,"artwork-live"));
        foreach(var item in new[]{new ContentItem{Name="Breaking Bad (2008)",Kind=ContentKind.Series},new ContentItem{Name="Inception (2010)",Kind=ContentKind.Movie},new ContentItem{Name="TRT 1 HD",EpgId="TRT1.tr",Kind=ContentKind.Live}})
        {var result=await discovery.ResolveAsync(item);Console.WriteLine(JsonSerializer.Serialize(new{item.Name,Artwork=result}));if(result is null)throw new Exception("Live artwork lookup failed: "+item.Name);}
    }
}
