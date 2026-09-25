using System.Diagnostics;
using WXPlayer.Core;

namespace WXPlayer.App;

internal static class DiscordPresenceSmoke
{
    internal static async Task RunAsync(MainWindow window,LibraryStore store,PlaybackEngine engine,Dictionary<string,object> results)
    {
        void Check(string key,bool value){results[key]=value;if(!value)throw new InvalidOperationException(key);}
        int index=Array.IndexOf(App.Arguments,"--media");if(index<0)throw new InvalidOperationException("Local --media required.");
        string url=new Uri(Path.GetFullPath(App.Arguments[index+1])).AbsoluteUri;
        var source=new SourceConfig{Id="discord-fixture",Name="Discord QA"};
        var movie=new ContentItem{Id="discord-movie",SourceId=source.Id,Name="Test Movie",Kind=ContentKind.Movie,Url=url};
        var episode=movie with{Id="discord-episode",Name="Test Episode",Kind=ContentKind.Episode,SeriesName="Test Series",SeriesId="discord-series",Season=2,Episode=3};
        var live=movie with{Id="discord-live",Name="Test Channel",Kind=ContentKind.Live,EpgId="fixture.live"};
        var other=live with{Id="discord-other",Name="Other Channel",EpgId="fixture.other"};
        async IAsyncEnumerable<ContentItem> Items(){yield return movie;yield return episode;yield return live;yield return other;await Task.Yield();}
        var now=DateTimeOffset.UtcNow;
        async IAsyncEnumerable<Programme> Guide(){yield return new("fixture.live","Current Programme","",now.AddMinutes(-10),now.AddMinutes(10));yield return new("fixture.other","Other Programme","",now.AddMinutes(-10),now.AddMinutes(10));await Task.Yield();}
        await store.ImportAsync(source,Items(),null,default);await store.ImportEpgAsync(source.Id,Guide(),default);await window.SmokeRefreshAsync(source.Id);
        window.SmokeDiscordEnabled(true);
        await window.SmokePlayAsync(movie);await Wait(()=>engine.Player.IsPlaying&&window.SmokeDiscordActivity?.End is >0);
        Check("discordMovieNative",window.SmokeDiscordActivity!.Details=="Test Movie");
        engine.Player.SetPause(true);await Wait(()=>window.SmokeDiscordActivity is {Start:null,End:null}&&window.SmokeDiscordActivity.State.Contains("Paused"));Check("discordPauseNative",true);
        engine.Player.SetPause(false);await Wait(()=>engine.Player.IsPlaying&&window.SmokeDiscordActivity?.End is >0);
        engine.Player.Time=20000;await Task.Delay(500);
        await Wait(()=>window.SmokeDiscordActivity?.End is{} end&&Math.Abs(end-DateTimeOffset.UtcNow.ToUnixTimeSeconds()-(engine.Player.Length-engine.Player.Time)/1000d)<3);
        Check("discordResumeSeekNative",true);
        await window.SmokePlayAsync(episode);await Wait(()=>window.SmokeDiscordActivity?.State.Contains("S02E03")==true);
        Check("discordSeriesNative",window.SmokeDiscordActivity!.Details=="Test Series"&&window.SmokeDiscordActivity.State.Contains("Test Episode"));
        await window.SmokePlayAsync(live);await Wait(()=>window.SmokeDiscordActivity?.Details=="Current Programme");
        Check("discordEpgNative",window.SmokeDiscordActivity is {Start:>0,End:>0}&&window.SmokeDiscordActivity.State.Contains("Test Channel"));
        var first=window.SmokePlayAsync(live);await Task.Delay(10);var second=window.SmokePlayAsync(other);await Task.WhenAll(first,second);
        await Wait(()=>window.SmokeDiscordActivity?.Details=="Other Programme");await Task.Delay(1200);
        Check("discordLatestChannelWinsNative",window.SmokeDiscordActivity?.State.Contains("Other Channel")==true);
        await engine.StopAsync();await Wait(()=>window.SmokeDiscordActivity is null);Check("discordStopNative",true);
        await window.SmokePlayAsync(movie);await Wait(()=>engine.Player.IsPlaying&&window.SmokeDiscordActivity?.End is >0&&engine.Player.Time>=19000);
        engine.Player.Time=engine.Player.Length-1200;
        try{await Wait(()=>engine.Player.State==LibVLCSharp.Shared.VLCState.Ended&&window.SmokeDiscordActivity is null);}
        catch{results["endState"]=engine.Player.State.ToString();results["endPosition"]=engine.Player.Time;results["endLength"]=engine.Player.Length;throw;}
        Check("discordEndNative",true);
        int errors=0;EventHandler<EventArgs> onError=(_,_)=>Interlocked.Increment(ref errors);engine.Player.EncounteredError+=onError;
        try
        {
            await window.SmokePlayAsync(movie with{Url=new Uri(Path.Combine(App.DataDirectory,"missing-media.mp4")).AbsoluteUri});
            // VLC may already have transitioned from Error to Ended before a UI tick.
            await Wait(()=>Volatile.Read(ref errors)>0&&window.SmokeDiscordActivity is null);Check("discordErrorNative",true);
        }
        finally{engine.Player.EncounteredError-=onError;}
        window.SmokeDiscordEnabled(false);
    }
    private static async Task Wait(Func<bool> condition)
    {var clock=Stopwatch.StartNew();while(!condition()){if(clock.Elapsed>TimeSpan.FromSeconds(15))throw new TimeoutException("Discord native playback assertion");await Task.Delay(100);}}
}
