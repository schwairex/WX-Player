using WXPlayer.Core;

internal static class HomeLibraryTests
{
    internal static async Task RunAsync(Func<string,Func<Task>,Task> test,Action<bool,string> assert,string folder)
    {
        var source=new SourceConfig{Id="home161",Name="Home",Kind=SourceKind.Xtream};
        string path=Path.Combine(folder,"home161.db");var store=new LibraryStore(path);await store.InitializeAsync();
        async IAsyncEnumerable<ContentItem> Items()
        {
            for(int k=0;k<3;k++)for(int i=0;i<100;i++)
                yield return new ContentItem{Id=$"home-{k}-{i}",SourceId=source.Id,Name=$"Title {i:000}",Kind=(ContentKind)k,Logo=i%10==0?"":"https://example.test/poster.jpg"};
            yield return new ContentItem{Id="home-episode",SourceId=source.Id,Name="Episode",Kind=ContentKind.Episode,SeriesId="home-2-1",SeriesName="Title 001",Logo="https://example.test/poster.jpg",Season=1,Episode=1};
            await Task.Yield();
        }
        await store.ImportAsync(source,Items(),null,default);
        await test("Individual recent removal preserves favorites, progress and library; replay restores it",async()=>
        {
            foreach(var id in new[]{"home-0-1","home-1-1","home-2-1"})
            {
                var item=(await store.FindAsync(id))!;
                var playable=item.Kind==ContentKind.Series?(await store.QueryAsync(source.Id,ContentKind.Episode,null,"",false,false,0)).Items.Single():item;
                await store.RememberAsync(id);await store.FavoriteAsync(id,true);await store.SaveProgressAsync(playable,60000,600000);
                await store.RemoveRecentAsync(id);await store.RemoveRecentAsync(id);
                await store.SaveProgressAsync(playable,65000,600000);
                assert(!(await store.QueryAsync(source.Id,null,null,"",false,true,0)).Items.Any(i=>i.Id==id),"periodic progress must not resurrect removed item");
                assert((await store.FindAsync(id)) is {IsFavorite:true},"favorite and catalog retained");
                if(playable.Kind!=ContentKind.Live)assert((await store.ProgressAsync(playable.Id))?.PositionMs==65000,"resume position retained");
                await store.ImportAsync(source,Items(),null,default);
                var reopened=new LibraryStore(path);await reopened.InitializeAsync();
                assert(!(await reopened.QueryAsync(source.Id,null,null,"",false,true,0)).Items.Any(i=>i.Id==id),"removal survives refresh and reopen");
                await store.RememberAsync(id);
                assert((await store.QueryAsync(source.Id,null,null,"",false,true,0)).Items.Any(i=>i.Id==id),"explicit replay restores history");
            }
            await store.RemoveRecentAsync("missing-item");
            assert((await store.QueryAsync(source.Id,null,null,"",false,true,0)).Total==3,"other history retained");
        });
        await test("Home shuffle varies visits, keeps pagination stable and preserves filters",async()=>
        {
            foreach(var kind in new[]{ContentKind.Movie,ContentKind.Series})
            {
                Task<Page> Query(int seed,int offset=0,int limit=48)=>store.QueryAsync(source.Id,kind,null,"",false,false,offset,limit,artworkOnly:true,shuffleSeed:seed);
                var first=await Query(123);var repeat=await Query(123);var next=await Query(456);
                assert(first.Items.Count==48&&first.Total==90,"larger shelf from eligible catalog");
                assert(first.Items.Select(i=>i.Id).SequenceEqual(repeat.Items.Select(i=>i.Id)),"same visit order stable");
                assert(!first.Items.Select(i=>i.Id).SequenceEqual(next.Items.Select(i=>i.Id)),"new visit changes order");
                assert(next.Items.Any(i=>first.Items.All(x=>x.Id!=i.Id)),"selection also varies");
                var page=await Query(123,24,24);
                assert(first.Items.Skip(24).Select(i=>i.Id).SequenceEqual(page.Items.Select(i=>i.Id)),"pagination no duplicates or gaps");
                assert(first.Items.All(i=>i.Kind==kind&&i.SourceId==source.Id&&i.Logo.Length>0),"kind/source/artwork filters");
            }
            var normal=await store.QueryAsync(source.Id,ContentKind.Movie,null,"Title 00",false,false,0);
            assert(normal.Items.Select(i=>i.Name).SequenceEqual(normal.Items.Select(i=>i.Name).Order()),"normal library stays alphabetical");
            var filtered=await store.QueryAsync(source.Id,ContentKind.Movie,null,"Title 00",false,false,0,48,shuffleSeed:123);
            assert(filtered.Total==normal.Total&&filtered.Items.All(i=>i.Name.StartsWith("Title 00")),"search remains respected");
        });
    }
}
