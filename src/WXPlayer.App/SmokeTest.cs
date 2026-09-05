using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using WXPlayer.Core;

namespace WXPlayer.App;

internal static class SmokeTest
{
    public static async Task RunAsync(MainWindow window,LibraryStore store,PlaybackEngine engine,ProviderClient providers,PlayerSettings settings)
    {
        var results=new Dictionary<string,object>();
        try
        {
            if(App.Arguments.Contains("--stress"))
            {
                var stressSource=new SourceConfig{Id="smoke-stress",Name="Performans testi",Address="https://example.test/list.m3u"};
                var sw=Stopwatch.StartNew();double last=0,maxGap=0;int heartbeats=0;
                var timer=new DispatcherTimer(DispatcherPriority.Normal){Interval=TimeSpan.FromMilliseconds(16)};
                timer.Tick+=(_,_)=>{double now=sw.Elapsed.TotalMilliseconds;maxGap=Math.Max(maxGap,now-last);last=now;heartbeats++;};timer.Start();
                int count=await store.ImportAsync(stressSource,StressItems(stressSource),null,default);timer.Stop();
                results["stressItems"]=count;results["stressImportMs"]=sw.ElapsedMilliseconds;results["uiHeartbeatsDuringImport"]=heartbeats;results["maxUiGapMs"]=Math.Round(maxGap,1);
                if(count!=100500||heartbeats<2||maxGap>1000)throw new Exception("UI responsiveness check failed.");
                await store.DeleteSourceAsync(stressSource.Id);
            }
            var source=new SourceConfig{Id="smoke-demo",Name="Örnek · Açık filmler",Address=Path.Combine(AppContext.BaseDirectory,"samples","open-films.m3u")};
            await store.ImportAsync(source,providers.LoadAsync(source,default),null,default);await window.SmokeRefreshAsync(source.Id);
            await Task.Delay(500);window.UpdateLayout();SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-preview.png"));results["startup"]=true;
            double width=window.Width;window.Width=940;window.UpdateLayout();SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-compact.png"));results["responsive"]=window.ActualWidth<=950;window.Width=width;
            var settingsWindow=window.CreateSettingsWindow();settingsWindow.Show();await Task.Delay(150);
            SaveWindow(settingsWindow,Path.Combine(App.DataDirectory,"WX-Player-settings.png"));settingsWindow.SelectTab(1);settingsWindow.UpdateLayout();await Task.Delay(100);SaveWindow(settingsWindow,Path.Combine(App.DataDirectory,"WX-Player-library-settings.png"));settingsWindow.SelectTab(2);settingsWindow.UpdateLayout();SaveWindow(settingsWindow,Path.Combine(App.DataDirectory,"WX-Player-update-settings.png"));settingsWindow.Close();results["settingsPages"]=true;
            var updateWindow=new UpdateWindow(window,new PreparedUpdate(new Version(1,3,0),"test-only",new string('0',64)),()=>Task.CompletedTask);updateWindow.Show();await Task.Delay(100);SaveWindow(updateWindow,Path.Combine(App.DataDirectory,"WX-Player-update-prompt.png"));updateWindow.Close();results["updatePrompt"]=true;
            int mediaArg=Array.IndexOf(App.Arguments,"--media");
            if(mediaArg>=0&&mediaArg+1<App.Arguments.Length)
            {
                var target=new PlaybackTarget(new Uri(Path.GetFullPath(App.Arguments[mediaArg+1])).AbsoluteUri);
                window.Video.Visibility=Visibility.Visible;window.WelcomePanel.Visibility=Visibility.Collapsed;window.UpdateLayout();
                await engine.PlayAsync(target,settings,default);
                await WaitUntil(()=>engine.Player.IsPlaying,TimeSpan.FromSeconds(15));
                await Task.Delay(1800);using(var media=engine.Player.Media)results["decodedFrames"]=media?.Statistics.DecodedVideo??0;
                results["playing"]=engine.Player.IsPlaying;results["seekable"]=engine.Player.IsSeekable;
                uint videoWidth=0,videoHeight=0;engine.Player.Size(0,ref videoWidth,ref videoHeight);results["sourceVideoSize"]=$"{videoWidth}x{videoHeight}";
                results["rendererIsEmbedded"]=engine.Player.Hwnd==window.Video.Handle&&window.Video.Handle!=IntPtr.Zero;
                results["noDetachedOverlayInNormalView"]=window.SmokeNoVideoOverlay;
                var typeface=new Typeface(window.FontFamily,FontStyles.Normal,FontWeights.Normal,FontStretches.Normal);
                results["embeddedInterFont"]=typeface.TryGetGlyphTypeface(out var glyph)&&glyph.FontUri.ToString().Contains("Inter-Regular",StringComparison.OrdinalIgnoreCase);
                SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-playing-ui.png"));
                window.Activate();await Task.Delay(250);results["playingWindowCaptured"]=WindowCapture.Save(window,Path.Combine(App.DataDirectory,"WX-Player-playing-native.png"));
                var original=FullscreenPlacement.WindowBounds(window);IntPtr originalHost=window.Video.Handle;
                PostMessage(window.Video.Handle,0x0100,new IntPtr(0x46),IntPtr.Zero);await Task.Delay(700);window.UpdateLayout();
                var full=FullscreenPlacement.WindowBounds(window);var monitor=window.SmokeMonitorBounds;
                results["fullscreenCoversMonitor"]=full.Left==monitor.Left&&full.Top==monitor.Top&&full.Width==monitor.Width&&full.Height==monitor.Height;
                results["fullscreenVideoFillsClient"]=Math.Abs(window.Video.ActualWidth-window.Root.ActualWidth)<1&&Math.Abs(window.Video.ActualHeight-window.Root.ActualHeight)<1;
                results["fullscreenKeepsNativeHandle"]=originalHost==window.Video.Handle;
                results["fullscreenHasFillCrop"]=!string.IsNullOrWhiteSpace(engine.Player.CropGeometry);
                results["fullscreenCropGeometry"]=engine.Player.CropGeometry??"";
                await Task.Delay(3000);results["fullscreenControlsAutoHide"]=!window.SmokeControlsVisible;
                SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-fullscreen-layout.png"));
                results["fullscreenWindowCaptured"]=WindowCapture.Save(window,Path.Combine(App.DataDirectory,"WX-Player-fullscreen-native.png"));
                PostMessage(window.Video.Handle,0x0200,IntPtr.Zero,new IntPtr((30<<16)|40));await Task.Delay(150);results["fullscreenControlsReveal"]=window.SmokeControlsVisible;
                window.SmokeFit();results["fitPreservesAspectRatio"]=string.IsNullOrEmpty(engine.Player.CropGeometry)&&string.IsNullOrEmpty(engine.Player.AspectRatio);window.SmokeFit();
                window.SmokeFullscreen();await Task.Delay(500);window.UpdateLayout();
                var restored=FullscreenPlacement.WindowBounds(window);results["windowPlacementRestored"]=original.Left==restored.Left&&original.Top==restored.Top&&original.Width==restored.Width&&original.Height==restored.Height;
                results["normalUiRestoredAfterFullscreen"]=window.Sidebar.IsVisible&&window.ControlsBorder.Parent==window.ViewingPanel&&window.SmokeNoVideoOverlay;
                results["normalCropCleared"]=string.IsNullOrEmpty(engine.Player.CropGeometry);
                foreach(var key in new[]{"rendererIsEmbedded","noDetachedOverlayInNormalView","embeddedInterFont","fullscreenCoversMonitor","fullscreenVideoFillsClient","fullscreenKeepsNativeHandle","fullscreenHasFillCrop","fullscreenControlsAutoHide","fullscreenControlsReveal","fitPreservesAspectRatio","windowPlacementRestored","normalUiRestoredAfterFullscreen","normalCropCleared"})
                    if(!Equals(results[key],true))throw new Exception("UI regression failed: "+key);
                window.WindowState=WindowState.Maximized;await Task.Delay(200);var maximized=FullscreenPlacement.WindowBounds(window);
                window.SmokeFullscreen();await Task.Delay(200);window.SmokeFullscreen();await Task.Delay(200);var back=FullscreenPlacement.WindowBounds(window);
                results["maximizedRoundTrip"]=window.WindowState==WindowState.Maximized&&maximized.Width==back.Width&&maximized.Height==back.Height;
                if(!Equals(results["maximizedRoundTrip"],true))throw new Exception("Maximized placement regression.");window.WindowState=WindowState.Normal;await Task.Delay(200);
                var epgSource=new SourceConfig{Id="smoke-epg",Name="EPG testi · Yerel örnek",EpgUrl=Path.Combine(App.DataDirectory,"fixture-epg.xml")};
                var now=DateTimeOffset.Now;string Stamp(DateTimeOffset d)=>d.ToString("yyyyMMddHHmmss zzz").Replace(":","");
                File.WriteAllText(epgSource.EpgUrl,$"<!DOCTYPE tv SYSTEM 'xmltv.dtd'><tv><channel id='wx.news'><display-name>WX Haber</display-name></channel><channel id='wx.culture'><display-name>WX Kültür</display-name></channel><programme channel='wx.news' start='{Stamp(now.AddMinutes(-20))}' stop='{Stamp(now.AddMinutes(40))}'><title>Güne Bakış · Test programı</title></programme><programme channel='wx.culture' start='{Stamp(now.AddMinutes(-20))}' stop='{Stamp(now.AddMinutes(40))}'><title>Kültür Atlası · Test programı</title></programme><programme channel='wx.culture' start='{Stamp(now.AddMinutes(40))}' stop='{Stamp(now.AddMinutes(100))}'><title>Sonraki program · Test</title></programme></tv>");
                var news=new ContentItem{Id="smoke-news",SourceId=epgSource.Id,Name="WX Haber FHD",EpgName="WX Haber",Kind=ContentKind.Live,Url=target.Url};
                var culture=new ContentItem{Id="smoke-culture",SourceId=epgSource.Id,Name="WX Kültür HD",Kind=ContentKind.Live,Url=target.Url};
                async IAsyncEnumerable<ContentItem> Channels(){yield return news;yield return culture;await Task.Yield();}
                await store.ImportAsync(epgSource,Channels(),null,default);await window.SmokeRefreshAsync(epgSource.Id);
                await window.SmokePlayAsync(news);results["epgAutomaticallyLoaded"]=window.EpgList.Items.Cast<Programme>().Single().Title.StartsWith("Güne Bakış");
                var first=window.SmokePlayAsync(news);await Task.Delay(10);var second=window.SmokePlayAsync(culture);await Task.WhenAll(first,second);
                results["epgFollowsLatestChannel"]=window.EpgList.Items.Cast<Programme>().All(p=>p.ChannelId=="wx.culture")&&window.EpgList.Items.Count==2&&window.GuideTitle.Text.Contains("WX Kültür");
                if(!Equals(results["epgAutomaticallyLoaded"],true)||!Equals(results["epgFollowsLatestChannel"],true))throw new Exception("EPG UI integration failed.");
                await WaitUntil(()=>engine.Player.IsPlaying,TimeSpan.FromSeconds(15));await Task.Delay(900);
                SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-epg.png"));
                var statistics=new StatisticsWindow(window,()=> ("Sintel · Yerel test videosu",target),engine,settings);statistics.Show();await Task.Delay(1200);SaveWindow(statistics,Path.Combine(App.DataDirectory,"WX-Player-statistics.png"));statistics.Close();
                using(var statsMedia=engine.Player.Media){var tracks=statsMedia!.Tracks;results["statisticsVideoTrack"]=tracks.Any(t=>t.TrackType==LibVLCSharp.Shared.TrackType.Video&&t.Data.Video.Width==854);results["statisticsAudioTrack"]=tracks.Any(t=>t.TrackType==LibVLCSharp.Shared.TrackType.Audio&&t.Data.Audio.Rate>0);}
                if(!Equals(results["statisticsVideoTrack"],true)||!Equals(results["statisticsAudioTrack"],true))throw new Exception("Statistics metadata missing.");
                if(engine.Player.IsSeekable){engine.Player.Time=2000;await Task.Delay(400);results["seek"]=engine.Player.Time>=1900;}
                engine.Player.Pause();await Task.Delay(350);results["pause"]=engine.Player.State==LibVLCSharp.Shared.VLCState.Paused;engine.Player.Pause();
                results["audioTracks"]=engine.Player.AudioTrackDescription.Count(t=>t.Id>=0);
                var subtitle=Path.Combine(App.DataDirectory,"test-subtitle.srt");File.WriteAllText(subtitle,"1\n00:00:00,000 --> 00:00:08,000\nWX Player subtitle test\n");
                results["externalSubtitle"]=engine.Player.AddSlave(LibVLCSharp.Shared.MediaSlaveType.Subtitle,new Uri(subtitle).AbsoluteUri,true);
                settings.RecordingFolder=Path.Combine(App.DataDirectory,"recordings");var recorded=await engine.StartRecordingAsync(target,"Smoke test",settings);await Task.Delay(3500);await engine.StopRecordingAsync();results["recordBytes"]=File.Exists(recorded)?new FileInfo(recorded).Length:0;
                results["recordFile"]=recorded;
                await engine.PlayAsync(new PlaybackTarget(new Uri(recorded).AbsoluteUri),settings,default);await WaitUntil(()=>engine.Player.IsPlaying,TimeSpan.FromSeconds(10));await Task.Delay(1000);using(var media=engine.Player.Media)results["recordDecodedFrames"]=media?.Statistics.DecodedVideo??0;
                if(Convert.ToInt32(results["decodedFrames"])<=0||Convert.ToInt32(results["recordDecodedFrames"])<=0||Convert.ToInt64(results["recordBytes"])<=0||!Equals(results["pause"],true)||!Equals(results["seek"],true))throw new Exception("Media assertion failed.");
            }
            int fixtureArg=Array.IndexOf(App.Arguments,"--restart-fixture");
            if(fixtureArg>=0&&fixtureArg+1<App.Arguments.Length)
            {
                byte[] fixture=await File.ReadAllBytesAsync(App.Arguments[fixtureArg+1]);
                string hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(fixture)).ToLowerInvariant();
                string json=JsonSerializer.Serialize(new{tag_name="v1.3.0",draft=false,prerelease=false,assets=new[]{new{name="WXPlayer.exe",size=fixture.Length,digest="sha256:"+hash,browser_download_url="https://github.com/schwairex/WX-Player/releases/download/v1.3.0/WXPlayer.exe"}}});
                using var controller=new UpdateController(settings,default,new GitHubUpdater(new UpdateFixtureHandler(json,fixture)));
                await controller.CheckAsync(true);if(controller.Ready is null)throw new Exception("Fixture update not staged: "+controller.Status);
                Environment.SetEnvironmentVariable("WXPLAYER_TEST_PARENT",Environment.ProcessId.ToString());
                await controller.RestartAsync();results["updaterRestartDispatched"]=true;
            }
            results["success"]=true;
        }
        catch(Exception ex){results["success"]=false;results["error"]=ex.ToString();}
        File.WriteAllText(Path.Combine(App.DataDirectory,"smoke-results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
        await window.Dispatcher.InvokeAsync(window.Close,DispatcherPriority.ApplicationIdle);
    }
    private sealed class UpdateFixtureHandler(string json,byte[] executable):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=request.RequestUri!.Host=="api.github.com"?new StringContent(json):new ByteArrayContent(executable)});
    }
    private static async Task WaitUntil(Func<bool> condition,TimeSpan timeout){var sw=Stopwatch.StartNew();while(!condition()){if(sw.Elapsed>timeout)throw new TimeoutException("Playback did not start.");await Task.Delay(100);}}
    [DllImport("user32.dll")]private static extern bool PostMessage(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam);
    private static async IAsyncEnumerable<ContentItem> StressItems(SourceConfig source)
    {
        for(int i=0;i<100500;i++){yield return new ContentItem{Id=ContentItem.Key(source.Id,i.ToString()),SourceId=source.Id,Name=$"Örnek Kanal {i:D6}",Category="Performans",Url=$"https://example.test/{i}.ts"};if(i%1000==0)await Task.Yield();}
    }
    private static void SaveWindow(Window window,string path)
    {
        window.UpdateLayout();var content=(FrameworkElement)window.Content;var bitmap=new RenderTargetBitmap((int)content.ActualWidth,(int)content.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(content);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(path);encoder.Save(file);
    }
}


