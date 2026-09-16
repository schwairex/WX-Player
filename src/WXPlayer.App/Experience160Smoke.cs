using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WXPlayer.Core;

namespace WXPlayer.App;

internal static class Experience160Smoke
{
    internal static async Task RunAsync(MainWindow window,LibraryStore store,PlaybackEngine engine,Dictionary<string,object> results)
    {
        void Check(string key,bool value){results[key]=value;if(!value)throw new InvalidOperationException("1.6.0: "+key);}
        int arg=Array.IndexOf(App.Arguments,"--media");if(arg<0)throw new InvalidOperationException("A local video is required.");
        string media=new Uri(Path.GetFullPath(App.Arguments[arg+1])).AbsoluteUri;
        var source=new SourceConfig{Id="experience160",Name="1.6.0 · Deneme kütüphanesi",Kind=SourceKind.Playlist};
        async IAsyncEnumerable<ContentItem> Items()
        {
            foreach(var (s,e) in new[]{(1,1),(1,2),(2,1)})yield return new(){Id=$"experience-{s}-{e}",SourceId=source.Id,Name=$"Kıyı Hikâyeleri (2024) S{s:00}E{e:00}",Category="Diziler",Kind=ContentKind.Episode,Url=media};
            yield return new(){Id="experience-movie",SourceId=source.Id,Name="Uzak Kıyı (2024)",Category="Filmler",Kind=ContentKind.Movie,Url=media};
            yield return new(){Id="experience-live",SourceId=source.Id,Name="WX TV",Category="Ulusal",Kind=ContentKind.Live,EpgId="WXTV.tr",Url=media};await Task.Yield();
        }
        var original=ArtworkService.Discovery;bool enabled=ArtworkService.Enabled;
        using var discovery=new ArtworkDiscovery(Path.Combine(App.DataDirectory,"discovery-fixture"),new MetadataHandler());
        try
        {
            // Seed the actual on-disk byte cache with local test artwork: no external request in UI tests.
            byte[] bytes=await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory,"samples","test-logo.ico"));
            string cache=Path.Combine(App.DataDirectory,"artwork-cache");Directory.CreateDirectory(cache);
            foreach(string image in new[]{"series","movie","live"})
            {string url=$"https://artwork-fixture.test/{image}.png";await File.WriteAllBytesAsync(Path.Combine(cache,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url))).ToLowerInvariant()+".img"),bytes);}
            ArtworkService.Discovery=discovery;ArtworkService.Enabled=true;
            await store.ImportAsync(source,Items(),null,default);await window.SmokeRefreshAsync(source.Id);await window.SmokeBrowseAsync("home");
            Check("missingArtworkAppearsOnHome",window.SmokeHome.Items.Count==3&&window.SmokeHome.Items.All(i=>i.Logo.StartsWith("https://artwork-fixture.test/")));
            Check("sourceArtworkDataUnchanged",(await store.QueryAsync(source.Id,null,null,"",false,false,0)).Items.All(i=>i.Logo.Length==0));
            Capture(window,"WX-Player-1.6.0-discovered-artwork.png");
            var series=(await store.QueryAsync(source.Id,ContentKind.Series,null,"",false,false,0)).Items.Single();
            var episodes=await window.SmokeEpisodesAsync(source,series);
            await window.SmokeBrowseAsync("series");await window.SmokePlayAsync(episodes.First(e=>e.Season==1&&e.Episode==1));
            await Wait(()=>engine.Player.IsPlaying&&engine.Player.Length>35000);await Task.Delay(700);
            Check("seriesReplacesGuide",window.SmokeSeriesPanel.IsVisible&&!window.GuidePanel.IsVisible);
            Check("currentSeasonSelected",window.SmokeSeriesPanel.Seasons.SelectedIndex==0&&window.SmokeSeriesPanel.Episodes.Items.Count==2);
            var selected=window.SmokeSeriesPanel.Episodes.SelectedItem as ContentItem;Check("playingEpisodeSelected",selected?.Episode==1);
            Capture(window,"WX-Player-1.6.0-episodes.png");
            double width=window.Width,height=window.Height;window.Width=900;window.Height=650;await Task.Delay(200);window.UpdateLayout();
            Check("compactEpisodePanelFits",window.SmokeSeriesPanel.ActualHeight>=175&&window.SmokeSeriesPanel.TranslatePoint(new Point(0,window.SmokeSeriesPanel.ActualHeight),window.Root).Y<=window.Root.ActualHeight);
            Capture(window,"WX-Player-1.6.0-episodes-small.png");window.Width=width;window.Height=height;
            window.SmokeSeriesPanel.Seasons.SelectedIndex=1;Check("seasonSwitchDoesNotInterrupt",window.SmokeSeriesPanel.Episodes.Items.Count==1&&window.NowTitle.Text.Contains("S01E01"));
            window.SmokeSeriesPanel.Seasons.SelectedIndex=0;
            window.SmokeFullscreen();window.Activate();engine.Player.Time=1000;await Task.Delay(350);window.SmokeNextEpisodeTick();Check("noEarlyNextEpisode",!window.SmokeNextEpisodeVisible);
            engine.Player.Time=engine.Player.Length-28000;await Task.Delay(400);window.SmokeNextEpisodeTick();await Wait(()=>window.SmokeNextEpisodeVisible);
            Check("nextEpisodeWithin30Seconds",window.SmokeNextEpisodeVisible);
            var prompt=window.OwnedWindows.Cast<Window>().Single(w=>w.Title=="Sonraki bölüm");Capture(prompt,"WX-Player-1.6.0-next-episode.png");
            engine.Player.Time=1000;await Task.Delay(400);window.SmokeNextEpisodeTick();Check("seekBackHidesNextEpisode",!window.SmokeNextEpisodeVisible);
            engine.Player.Time=engine.Player.Length-28000;await Task.Delay(400);window.SmokeNextEpisodeTick();await Wait(()=>window.SmokeNextEpisodeVisible);
            window.SmokeClickNextEpisode();await Wait(()=>window.NowTitle.Text.Contains("S01E02")&&engine.Player.IsPlaying);await Task.Delay(700);
            Check("nextEpisodeClickKeepsFullscreen",!window.Sidebar.IsVisible&&window.NowTitle.Text.Contains("S01E02")&&!window.SmokeNextEpisodeVisible);
            Check("previousEpisodeProgressSaved",await store.ProgressAsync(episodes.First(e=>e.Season==1&&e.Episode==1).Id) is{PositionMs:>5000});
            await window.SmokeNextEpisodeAsync();await Wait(()=>window.NowTitle.Text.Contains("S02E01")&&engine.Player.IsPlaying);await Task.Delay(400);
            Check("nextCrossesSeason",window.NowTitle.Text.Contains("S02E01"));engine.Player.Time=engine.Player.Length-15000;await Task.Delay(350);window.SmokeNextEpisodeTick();Check("lastEpisodeHasNoNext",!window.SmokeNextEpisodeVisible);
            window.SmokeFullscreen();Check("currentSeasonAfterFullscreen",window.SmokeSeriesPanel.IsVisible&&window.SmokeSeriesPanel.Seasons.SelectedIndex==1);
            var live=(await store.FindAsync("experience-live"))!;await window.SmokePlayAsync(live);Check("liveGuideRestored",window.GuidePanel.IsVisible&&!window.SmokeSeriesPanel.IsVisible);
            var movie=(await store.FindAsync("experience-movie"))!;await window.SmokePlayAsync(movie);await Wait(()=>engine.Player.IsPlaying);window.SmokeFullscreen();engine.Player.Time=engine.Player.Length-15000;await Task.Delay(350);window.SmokeNextEpisodeTick();Check("movieHasNoEpisodeOffer",!window.SmokeNextEpisodeVisible);window.SmokeFullscreen();
            Check("seriesDoesNotAffectMovies",!window.SmokeSeriesPanel.IsVisible);
        }
        finally{ArtworkService.Enabled=enabled;ArtworkService.Discovery=original;}
    }
    private static async Task Wait(Func<bool> condition){var timer=Stopwatch.StartNew();while(!condition()){if(timer.Elapsed>TimeSpan.FromSeconds(15))throw new TimeoutException("1.6.0 UI/media wait timed out");await Task.Delay(100);}}
    private static void Capture(Window window,string name)
    {window.UpdateLayout();var content=(FrameworkElement)window.Content;var bitmap=new RenderTargetBitmap((int)content.ActualWidth,(int)content.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(content);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(App.DataDirectory,name));encoder.Save(file);}
    private sealed class MetadataHandler:HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            string url=request.RequestUri!.ToString();string json;
            if(url.Contains("tvmaze"))json=JsonSerializer.Serialize(new[]{new{show=new{name="Kıyı Hikâyeleri",premiered="2024-01-01",url="https://www.tvmaze.com/shows/1/test",image=new{original="https://artwork-fixture.test/series.png"}}}});
            else if(url.Contains("channels.json"))json="[{\"id\":\"WXTV.tr\",\"name\":\"WX TV\",\"alt_names\":[]}]";
            else if(url.Contains("logos.json"))json="[{\"channel\":\"WXTV.tr\",\"feed\":null,\"in_use\":true,\"format\":\"PNG\",\"url\":\"https://artwork-fixture.test/live.png\"}]";
            else json=JsonSerializer.Serialize(new{query=new{pages=new[]{new{title="Uzak Kıyı",extract="Uzak Kıyı, 2024 yapımı bir film.",fullurl="https://tr.wikipedia.org/wiki/Test",thumbnail=new{source="https://artwork-fixture.test/movie.png"}}}}});
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(json,Encoding.UTF8,"application/json")});
        }
    }
}
