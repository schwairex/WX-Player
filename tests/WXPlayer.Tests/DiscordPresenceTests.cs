using System.Collections.Concurrent;
using System.Text.Json;
using WXPlayer.Core;

internal static class DiscordPresenceTests
{
    internal static async Task RunAsync(Func<string,Func<Task>,Task> test,Action<bool,string> assert)
    {
        var now=DateTimeOffset.FromUnixTimeSeconds(1800000000);
        var film=new ContentItem{Id="film",Name="A Film",Kind=ContentKind.Movie};
        await test("Discord waits for playback; timestamps deduplicate progress and follow pause resume seek",()=>
        {
            var state=new DiscordPresenceState();long id=state.Begin(PresenceMedia.FromContent(film));
            state.Observe(id,PresencePlayback.Opening,0,600000,now);assert(state.Activity is null,"not on click");
            state.Observe(id,PresencePlayback.Paused,0,600000,now);assert(state.Activity is null,"not paused before playback");
            state.Observe(id,PresencePlayback.Playing,120000,600000,now);
            var first=state.Activity;assert(first?.Start==1799999880&&first.End==1800000480,"120 elapsed / 480 remaining");
            for(int i=1;i<=60;i++)state.Observe(id,PresencePlayback.Playing,120000+i*1000,600000,now.AddSeconds(i));
            assert(state.Activity==first,"no updates from clock ticks");
            state.Observe(id,PresencePlayback.Paused,180000,600000,now.AddSeconds(60));
            assert(state.Activity is {Start:null,End:null}&&state.Activity.State.Contains("Paused"),"pause freezes timer");
            state.Observe(id,PresencePlayback.Playing,180000,600000,now.AddSeconds(90));
            assert(state.Activity?.End==1800000510,"resume accounts for 30-second pause");
            state.Observe(id,PresencePlayback.Playing,300000,600000,now.AddSeconds(91),forceTiming:true);
            assert(state.Activity?.End==1800000391,"seek recalculates remaining");
            foreach(var status in new[]{PresencePlayback.Stopped,PresencePlayback.Error,PresencePlayback.Ended})
            {state.Observe(id,PresencePlayback.Playing,0,600000,now);state.Observe(id,status,0,600000,now);assert(state.Activity is null,"terminal state clears");}
            return Task.CompletedTask;
        });
        await test("Discord current EPG changes, missing EPG fallback and stale generations",()=>
        {
            var state=new DiscordPresenceState();var live=new ContentItem{Id="one",Name="Channel One",Kind=ContentKind.Live};
            long old=state.Begin(PresenceMedia.FromContent(live));
            var programme=new PresenceProgramme("Evening News",now.AddMinutes(-10),now.AddMinutes(20));
            state.Observe(old,PresencePlayback.Playing,0,0,now,programme);
            assert(state.Activity is {Details:"Evening News",State:"Channel One · Live TV",Start:1799999400,End:1800001200},"EPG timestamps");
            state.Observe(old,PresencePlayback.Playing,0,0,now.AddMinutes(21),programme);
            assert(state.Activity is {Details:"Channel One",Start:null,End:null},"expired EPG falls back");
            var second=programme with{Title="Next Programme",Start=now.AddMinutes(20),End=now.AddMinutes(40)};
            state.Observe(old,PresencePlayback.Playing,0,0,now.AddMinutes(21),second);
            assert(state.Activity?.Details=="Next Programme","programme boundary");
            long current=state.Begin(PresenceMedia.FromContent(live with{Id="two",Name="Channel Two"}));
            state.Observe(current,PresencePlayback.Playing,0,0,now);
            state.Observe(old,PresencePlayback.Playing,0,0,now,programme);
            assert(state.Activity?.Details=="Channel Two","old async result rejected");
            return Task.CompletedTask;
        });
        await test("Discord series fields, duration bounds and private artwork filtering",()=>
        {
            var state=new DiscordPresenceState();
            var episode=film with{Kind=ContentKind.Episode,SeriesName="My Show",Name="Pilot",Season=2,Episode=3,Url="https://private.test/u/password/123",Logo="https://private.test/logo?token=secret"};
            long id=state.Begin(PresenceMedia.FromContent(episode,["password","secret"]));
            state.Observe(id,PresencePlayback.Playing,10000,100000,now);
            assert(state.Activity?.Details=="My Show"&&state.Activity.State.Contains("S02E03")&&state.Activity.State.Contains("Pilot"),"series and episode");
            assert(state.Activity?.Image is null,"provider logo never shared");
            string payload=DiscordIpcConnection.SerializeActivity(state.Activity,"n",123);
            assert(!payload.Contains("password")&&!payload.Contains("secret")&&!payload.Contains("private.test"),"no IPTV credentials or address");
            state.Observe(id,PresencePlayback.Playing,-100,0,now);
            assert(state.Activity is {Start:null,End:null},"unknown duration has no fake countdown");
            var safe=PresenceMedia.FromContent(film with{Logo="https://image.tmdb.org/t/p/w500/abc123.jpg"});
            assert(safe.Image is not null,"public poster allowed");
            foreach(string image in new[]{"https://image.tmdb.org/t/p/w500/a.jpg?token=x","https://user:pass@image.tmdb.org/t/p/w500/a.jpg","file:///secret","https://image.tmdb.org.evil.test/a.jpg"})
                assert(PresenceMedia.FromContent(film with{Logo=image}).Image is null,"unsafe asset");
            var redacted=PresenceMedia.FromContent(film with{Name="password https://provider.test/token"},["password"]);
            assert(!JsonSerializer.Serialize(redacted).Contains("password")&&!redacted.Title.Contains("provider.test"),"title redaction");
            assert(!JsonSerializer.Deserialize<PlayerSettings>("{}")!.DiscordRichPresence,"opt-in migration");
            assert(JsonSerializer.Deserialize<PlayerSettings>(JsonSerializer.Serialize(new PlayerSettings{DiscordRichPresence=true}))!.DiscordRichPresence,"setting persists");
            return Task.CompletedTask;
        });
        await test("Discord worker coalesces updates, reconnects, clears on disable and disposes",async()=>
        {
            var connections=new ConcurrentQueue<FakeConnection>();int attempts=0;bool available=false;
            await using var service=new DiscordPresenceService("123456789012345678",ct=>
            {
                Interlocked.Increment(ref attempts);if(!Volatile.Read(ref available))throw new IOException("offline");
                var connection=new FakeConnection();connections.Enqueue(connection);return Task.FromResult<IDiscordConnection>(connection);
            },TimeSpan.FromMilliseconds(40),TimeSpan.FromMilliseconds(10));
            var activity=new DiscordActivity("A Film","Movie",100,200,null);
            service.SetEnabled(true);service.Publish(activity);
            await Until(()=>Volatile.Read(ref attempts)>1);Volatile.Write(ref available,true);
            await Until(()=>connections.TryPeek(out var c)&&c.Sent.Contains(activity));
            var first=connections.Single();for(int i=0;i<100;i++)service.Publish(activity);
            await Task.Delay(100);assert(first.Sent.Count==1,"identical snapshots not sent again");
            first.Connected=false;
            await Until(()=>connections.Count>=2&&connections.Last().Sent.Contains(activity));
            var second=connections.Last();service.SetEnabled(false);
            await Until(()=>second.Disposed);assert(second.Sent.Last() is null,"off clears");
            service.SetEnabled(true);await Until(()=>connections.Count>=3&&connections.Last().Sent.Contains(activity));
            await service.DisposeAsync();int count=attempts;service.Publish(activity);service.SetEnabled(true);
            await Task.Delay(100);assert(attempts==count&&connections.All(c=>c.Disposed),"no reconnect after dispose");
        });
        await test("Discord delayed connection uses latest selection and OFF wins during connect",async()=>
        {
            foreach(bool disable in new[]{false,true})
            {
                var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var release=new TaskCompletionSource<IDiscordConnection>(TaskCreationOptions.RunContinuationsAsynchronously);
                var connection=new FakeConnection();
                await using var service=new DiscordPresenceService("123456789012345678",ct=>{entered.TrySetResult();return release.Task.WaitAsync(ct);},TimeSpan.FromMilliseconds(20),TimeSpan.FromMilliseconds(10));
                service.Publish(new("Old Channel","Live TV",null,null,null));service.SetEnabled(true);
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
                service.Publish(new("New Channel","Live TV",null,null,null));if(disable)service.SetEnabled(false);
                release.SetResult(connection);
                await Until(()=>disable?connection.Disposed:connection.Sent.Any(a=>a?.Details=="New Channel"));
                assert(connection.Sent.All(a=>a?.Details!="Old Channel"),"late connection cannot send old selection");
                if(disable)assert(connection.Sent.All(a=>a is null),"OFF prevents activity after delayed connect");
            }
            using var enteredCancel=new SemaphoreSlim(0,1);
            await using var pending=new DiscordPresenceService("123456789012345678",async ct=>{enteredCancel.Release();await Task.Delay(Timeout.Infinite,ct);throw new IOException();});
            pending.SetEnabled(true);await enteredCancel.WaitAsync(TimeSpan.FromSeconds(2));
            await pending.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
        });
        await test("Discord missing application ID stays inert; rejected artwork retries without images",async()=>
        {
            int calls=0;await using(var inert=new DiscordPresenceService("invalid",ct=>{calls++;throw new IOException();},TimeSpan.FromMilliseconds(10)))
            {inert.SetEnabled(true);inert.Publish(new("Title","Movie",null,null,null));await Task.Delay(80);assert(calls==0,"placeholder cannot connect");}
            var connection=new FakeConnection{RejectImages=true};
            await using var service=new DiscordPresenceService("123456789012345678",ct=>Task.FromResult<IDiscordConnection>(connection),TimeSpan.FromMilliseconds(20),TimeSpan.FromMilliseconds(10));
            service.SetEnabled(true);service.Publish(new("Title","Movie",null,null,"https://image.tmdb.org/t/p/w500/a.jpg"));
            await Until(()=>connection.Sent.Any(a=>a is {Image:null}));
            assert(connection.Sent.Last()?.Details=="Title","safe imageless fallback");
        });
    }
    internal static async Task Until(Func<bool> condition)
    {for(int i=0;i<200;i++){if(condition())return;await Task.Delay(10);}throw new TimeoutException("Discord test condition");}
    private sealed class FakeConnection:IDiscordConnection
    {
        public volatile bool Connected=true;public bool IsConnected=>Connected;
        public volatile bool Disposed;public bool RejectImages;
        public ConcurrentQueue<DiscordActivity?> Sent {get;}=new();
        public Task SetActivityAsync(DiscordActivity? activity,CancellationToken ct)
        {ct.ThrowIfCancellationRequested();if(RejectImages&&activity?.Image is not null)throw new DiscordCommandException();Sent.Enqueue(activity);return Task.CompletedTask;}
        public ValueTask DisposeAsync(){Disposed=true;Connected=false;return ValueTask.CompletedTask;}
    }
}
