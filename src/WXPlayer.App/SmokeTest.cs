using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
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
            int mediaArg=Array.IndexOf(App.Arguments,"--media");
            if(mediaArg>=0&&mediaArg+1<App.Arguments.Length)
            {
                var target=new PlaybackTarget(new Uri(Path.GetFullPath(App.Arguments[mediaArg+1])).AbsoluteUri);
                window.Video.Visibility=Visibility.Visible;window.WelcomePanel.Visibility=Visibility.Collapsed;
                await engine.PlayAsync(target,settings,default);
                await WaitUntil(()=>engine.Player.IsPlaying,TimeSpan.FromSeconds(15));
                await Task.Delay(1800);using(var media=engine.Player.Media)results["decodedFrames"]=media?.Statistics.DecodedVideo??0;
                results["playing"]=engine.Player.IsPlaying;results["seekable"]=engine.Player.IsSeekable;
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
            results["success"]=true;
        }
        catch(Exception ex){results["success"]=false;results["error"]=ex.ToString();}
        File.WriteAllText(Path.Combine(App.DataDirectory,"smoke-results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
        await window.Dispatcher.InvokeAsync(window.Close,DispatcherPriority.ApplicationIdle);
    }
    private static async Task WaitUntil(Func<bool> condition,TimeSpan timeout){var sw=Stopwatch.StartNew();while(!condition()){if(sw.Elapsed>timeout)throw new TimeoutException("Playback did not start.");await Task.Delay(100);}}
    private static async IAsyncEnumerable<ContentItem> StressItems(SourceConfig source)
    {
        for(int i=0;i<100500;i++){yield return new ContentItem{Id=ContentItem.Key(source.Id,i.ToString()),SourceId=source.Id,Name=$"Örnek Kanal {i:D6}",Category="Performans",Url=$"https://example.test/{i}.ts"};if(i%1000==0)await Task.Yield();}
    }
    private static void SaveWindow(Window window,string path)
    {
        window.UpdateLayout();var content=(FrameworkElement)window.Content;var bitmap=new RenderTargetBitmap((int)content.ActualWidth,(int)content.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(content);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(path);encoder.Save(file);
    }
}
