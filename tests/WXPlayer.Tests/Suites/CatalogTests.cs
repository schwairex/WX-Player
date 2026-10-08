using Microsoft.Data.Sqlite;
using WXPlayer.Core;

internal static class CatalogTests
{
    internal static async Task RunAsync(Func<string,Func<Task>,Task> test,Action<bool,string> assert,string folder)
    {
        var source=new SourceConfig{Id="catalog151",Name="Catalog",Kind=SourceKind.Playlist,Address="https://example.test/list.m3u"};
        ContentItem Episode(int season,int episode)=>new(){Id="ep-"+season+"-"+episode,SourceId=source.Id,Name=$"500T (2021) S{season:00} 500T - {episode}. Bölüm - Başlık - S{season:00}.E{episode:00}",Category="TR ✦ Gain",Kind=ContentKind.Movie,Logo="https://example.test/poster.jpg",Url=$"https://example.test/series/u/p/{season}{episode}.mp4"};
        var movie=new ContentItem{Id="movie151",SourceId=source.Id,Name="Bir Film (2024)",Category="Filmler",Kind=ContentKind.Movie,Url="https://example.test/movie/1.mp4"};
        var store=new LibraryStore(Path.Combine(folder,"catalog151.db"));await store.InitializeAsync();
        await test("Home artwork filter runs before pagination and preserves full catalog",async()=>
        {
            var artSource=new SourceConfig{Id="artwork153",Name="Artwork",Kind=SourceKind.Xtream};
            async IAsyncEnumerable<ContentItem> ArtItems(){for(int i=0;i<30;i++){yield return new ContentItem{Id="art-empty-"+i,SourceId=artSource.Id,Name="A Missing "+i,Kind=ContentKind.Movie,Logo=i%2==0?"":"ftp://example.test/art"};await Task.Yield();}for(int i=0;i<3;i++)yield return new ContentItem{Id="art-valid-"+i,SourceId=artSource.Id,Name="Z Poster "+i,Kind=(ContentKind)i,Logo="https://example.test/"+i+".jpg"};}
            await store.ImportAsync(artSource,ArtItems(),null,default);
            var page=await store.QueryAsync(artSource.Id,null,null,"",false,false,0,2,artworkOnly:true);
            assert(page.Total==3&&page.Items.Count==2&&page.Items.All(i=>i.Id.StartsWith("art-valid-")),"filter before limit");
            assert((await store.QueryAsync(artSource.Id,null,null,"",false,false,0)).Total==33,"full library preserved");
            await store.FavoriteAsync("art-empty-0",true);await store.RememberAsync("art-empty-0");
            assert((await store.QueryAsync(artSource.Id,null,null,"",true,true,0,artworkOnly:true)).Total==0,"home favorites/history artwork filter");
            assert((await store.QueryAsync(artSource.Id,null,null,"",true,true,0)).Total==1,"hidden card still available in library");
            var recommendation=await store.RecommendationAsync(artSource.Id,null,artworkOnly:true);
            assert(recommendation is not null&&recommendation.Logo.StartsWith("https://"),"recommendation requires artwork");
            await store.DeleteSourceAsync(artSource.Id);
        });
        await test("Content selection identity survives binding and favorite changes",()=>
        {var item=movie with{};var set=new HashSet<ContentItem>{item};int hash=item.GetHashCode();item.PropertyChanged+=(_,_)=>{};item.IsFavorite=true;assert(hash==item.GetHashCode()&&set.Contains(item)&&set.Remove(item),"stable hash for WPF item selection");assert(!item.Equals(item with{Id="another"}),"different content remains distinct");return Task.CompletedTask;});
        async IAsyncEnumerable<ContentItem> Items(){yield return Episode(1,10);yield return Episode(2,1);yield return Episode(1,2);yield return movie;await Task.Yield();}
        await test("M3U episode names classify before video extensions; live names remain live",()=>
        {
            var item=CatalogClassifier.Normalize(Episode(1,1),SourceKind.Playlist);assert(item.Kind==ContentKind.Episode&&item.SeriesName=="500T (2021)"&&item.Season==1&&item.Episode==1,"reported 500T title");
            foreach(var name in new[]{"Dizi Adı S02E03","Dizi Adı 2x03","Dizi Adı - 2. Sezon 3. Bölüm","Dizi Adı S02.B03"}){var normalized=CatalogClassifier.Normalize(movie with{Name=name},SourceKind.Playlist);assert(normalized.Kind==ContentKind.Episode&&normalized.SeriesName=="Dizi Adı"&&normalized.Season==2&&normalized.Episode==3,"episode convention: "+name);}
            assert(CatalogClassifier.Normalize(movie with{Name="Dizi TV",Category="Canlı TV",Url="https://example.test/live/1.ts"},SourceKind.Playlist).Kind==ContentKind.Live,"channel name does not determine kind");
            assert(CatalogClassifier.Normalize(movie,SourceKind.Xtream).Kind==ContentKind.Movie,"API kind preserved");return Task.CompletedTask;
        });
        await test("Playlist imports group seasons into one series, keep posters and sorted episodes",async()=>
        {
            await store.ImportAsync(source,Items(),null,default);var films=await store.QueryAsync(source.Id,ContentKind.Movie,null,"",false,false,0);var shows=await store.QueryAsync(source.Id,ContentKind.Series,null,"",false,false,0);
            assert(films.Total==1&&films.Items[0].Id==movie.Id&&shows.Total==1,"catalog partition");assert(shows.Items[0].Name=="500T (2021)"&&shows.Items[0].Logo.EndsWith("poster.jpg"),"series metadata");
            var episodes=await store.PlaylistEpisodesAsync(shows.Items[0]);assert(episodes.Count==3&&episodes[0].Episode==2&&episodes[1].Episode==10&&episodes[2].Season==2,"numeric episode order");
            assert((await store.QueryAsync(source.Id,null,null,"",false,false,0)).Total==2,"all excludes episode rows");
        });
        await test("Watch positions survive reopen and refresh; series exposes latest episode",async()=>
        {
            var show=(await store.QueryAsync(source.Id,ContentKind.Series,null,"",false,false,0)).Items[0];var eps=await store.PlaylistEpisodesAsync(show);
            await store.SaveProgressAsync(movie,125000,5400000);await store.SaveProgressAsync(eps[0],73000,3000000);await Task.Delay(2);await store.SaveProgressAsync(eps[1],82000,3000000);
            var reopened=new LibraryStore(Path.Combine(folder,"catalog151.db"));await reopened.InitializeAsync();var state=await reopened.FindAsync(show.Id);assert(state?.Progress?.ItemId==eps[1].Id&&state.Progress.PositionMs==82000&&state.ProgressLabel.Contains("B10"),"series checkpoint");
            await store.ImportAsync(source,Items(),null,default);assert((await store.FindAsync(movie.Id))?.Progress?.PositionMs==125000,"refresh retains movie position");
            assert((await store.StatsAsync(source.Id)).Series==1,"series count counts shows");
        });
        await test("Completed media, recommendation exclusion, and clearing progress",async()=>
        {
            await store.SaveProgressAsync(movie,121000,0);assert((await store.ProgressAsync(movie.Id)) is{PositionMs:121000,Completed:false,DurationMs:0},"elapsed time retained even when provider omits duration");
            await store.SaveProgressAsync(movie,5400000,5400000);assert((await store.ProgressAsync(movie.Id))?.Completed==true,"end state");
            var first=await store.RecommendationAsync(source.Id,null);var next=await store.RecommendationAsync(source.Id,first!.Id);assert(first.Id!=next!.Id&&next.Kind!=ContentKind.Episode,"next startup suggestion differs when alternatives exist");
            await store.ClearAsync(LibraryCleanup.History);assert(await store.ProgressAsync(movie.Id) is null&&(await store.QueryAsync(source.Id,null,null,"",false,true,0)).Total==0,"history removal clears checkpoints");
        });
        await test("Cached 1.5 playlist migrates without reimport and favorites stay removable",async()=>
        {
            var path=Path.Combine(folder,"old151.db");var old=new LibraryStore(path);await old.InitializeAsync();
            using(var c=new SqliteConnection("Data Source="+path))
            {
                c.Open();using var cmd=c.CreateCommand();cmd.CommandText="""
                    INSERT INTO sources VALUES($s,$secret);
                    INSERT INTO items(id,source,provider,name,category,kind,url,logo,epg,extension,catchup,days,ua,referrer,search_text,epg_name) VALUES('old-ep',$s,'1',$name,'TR Gain',1,'https://example.test/1.mp4','https://example.test/cover.jpg','','mp4','',0,'','','500t','');
                    INSERT INTO favorites VALUES('old-ep');INSERT INTO history VALUES('old-ep',1750000000);
                    DELETE FROM catalog_meta;
                    """;cmd.Parameters.AddWithValue("$s",source.Id);cmd.Parameters.AddWithValue("$secret",SecretVault.Protect(source));cmd.Parameters.AddWithValue("$name",Episode(1,1).Name);cmd.ExecuteNonQuery();
            }
            await old.InitializeAsync();var shows=await old.QueryAsync(source.Id,ContentKind.Series,null,"",true,true,0);assert(shows.Total==1&&shows.Items[0].Name=="500T (2021)","legacy favorite/history transferred");
            assert((await old.QueryAsync(source.Id,ContentKind.Movie,null,"",false,false,0)).Total==0,"legacy misclassified movie removed from films");
            await old.FavoriteAsync(shows.Items[0].Id,false);await old.ImportAsync(source,Items(),null,default);assert((await old.QueryAsync(source.Id,null,null,"",true,false,0)).Total==0,"removed show favorite not resurrected");
            await old.InitializeAsync();assert((await old.QueryAsync(source.Id,ContentKind.Series,null,"",false,false,0)).Total==1,"idempotent migration");
        });
        await test("Series identities and checkpoints remain source-scoped",async()=>
        {
            var other=source with{Id="catalog-other"};async IAsyncEnumerable<ContentItem> Other(){yield return Episode(1,2) with{Id="other-ep",SourceId=other.Id};await Task.Yield();}
            await store.ImportAsync(other,Other(),null,default);var show=(await store.QueryAsync(source.Id,ContentKind.Series,null,"",false,false,0)).Items[0];var second=(await store.QueryAsync(other.Id,ContentKind.Series,null,"",false,false,0)).Items[0];assert(show.Id!=second.Id,"group keys include source");
            var ep=(await store.PlaylistEpisodesAsync(second))[0];await store.SaveProgressAsync(ep,60000,1200000);await store.DeleteSourceAsync(other.Id);assert(await store.ProgressAsync(ep.Id) is null&&await store.FindAsync(show.Id) is not null,"source cleanup isolation");
        });
    }
}

