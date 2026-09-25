using System.Windows;
using WXPlayer.Core;
using LibVLCSharp.Shared;

namespace WXPlayer.App;

public partial class MainWindow
{
    private readonly DiscordPresenceState _discordState=new();
    private DiscordPresenceService? _discord;
    private ContentItem? _discordItem;
    private long _discordGeneration;
    private int _discordPlayVersion,_discordSignalPending;
    private bool _discordAccepted,_discordForceTiming;
    private CancellationTokenSource? _discordGuideCancellation;
    private Task? _discordGuideTask;
    private Task? _discordArtworkTask;
    private long _discordArtworkRequestedGeneration=-1;
    private DateTimeOffset _discordGuideNext;
    private IReadOnlyList<PresenceProgramme> _discordProgrammes=[];

    internal DiscordActivity? SmokeDiscordActivity=>_discordState.Activity;
    internal void SmokeDiscordEnabled(bool enabled){_settings.DiscordRichPresence=enabled;_discordGuideNext=DateTimeOffset.MinValue;UpdateDiscordPlayback();}

    private void InitializeDiscord()
    {
        _discord=new(DiscordConfiguration.ApplicationId);
        _discord.SetEnabled(_settings.DiscordRichPresence);
        var player=_engine.Player;
        player.Playing+=DiscordPlaybackChanged;player.Paused+=DiscordPlaybackChanged;
        player.Stopped+=DiscordPlaybackChanged;player.EndReached+=DiscordPlaybackChanged;
        player.EncounteredError+=DiscordPlaybackChanged;player.LengthChanged+=DiscordPlaybackChanged;
    }
    private void DiscordPlaybackChanged(object? sender,EventArgs e)
    {
        // Native callbacks carry no content metadata. Read the current player state on
        // the dispatcher rather than replaying an old event's state after a channel switch.
        if(_closing||Interlocked.Exchange(ref _discordSignalPending,1)!=0)return;
        try{Dispatcher.BeginInvoke(()=>
        {
            Interlocked.Exchange(ref _discordSignalPending,0);
            if(!_closing)UpdateDiscordPlayback();
        });}catch(InvalidOperationException){Interlocked.Exchange(ref _discordSignalPending,0);}
    }
    private void BeginDiscordSelection()
    {
        _discordAccepted=false;_discordItem=null;_discordForceTiming=false;
        _discordGuideCancellation?.Cancel();_discordGuideCancellation?.Dispose();
        _discordGuideCancellation=CancellationTokenSource.CreateLinkedTokenSource(_life.Token);
        _discordArtworkRequestedGeneration=-1;
        // Keep the cancelled job owned until it completes, including at shutdown.
        _discordGuideNext=DateTimeOffset.MinValue;_discordProgrammes=[];
        _discordGeneration=_discordState.Begin(null);_discord?.Publish(null);
    }
    private string[] DiscordPrivateValues(ContentItem item)
    {
        var source=_sources.FirstOrDefault(s=>s.Id==item.SourceId);
        var values=new List<string>{source?.Username??"",source?.Password??"",source?.Mac??""};
        foreach(string address in new[]{source?.Address??"",item.Url})
        {
            if(!Uri.TryCreate(address,UriKind.Absolute,out var uri))continue;
            values.AddRange(uri.UserInfo.Split(':'));
            foreach(var pair in uri.Query.TrimStart('?').Split('&'))
            {int split=pair.IndexOf('=');if(split>=0)values.Add(Uri.UnescapeDataString(pair[(split+1)..]));}
        }
        return values.Where(s=>s.Length>0).Distinct().ToArray();
    }
    private void AcceptDiscordPlayback(ContentItem item,int version)
    {
        if(_closing||version!=_playVersion)return;
        _discordItem=item;_discordPlayVersion=version;_discordAccepted=true;
        _discordGeneration=_discordState.Begin(PresenceMedia.FromContent(item,DiscordPrivateValues(item)));
        UpdateDiscordPlayback();
    }
    private void UpdateDiscordPlayback()
    {
        if(_closing||_discord is null||_engine is null)return;
        _discord.SetEnabled(_settings.DiscordRichPresence);
        if(!_discordAccepted||_discordItem is null||_discordPlayVersion!=_playVersion){_discord.Publish(null);return;}
        var player=_engine.Player;var now=DateTimeOffset.UtcNow;
        var status=player.State switch
        {
            VLCState.Playing=>PresencePlayback.Playing,VLCState.Paused=>PresencePlayback.Paused,
            VLCState.Buffering=>PresencePlayback.Buffering,VLCState.Ended=>PresencePlayback.Ended,
            VLCState.Error=>PresencePlayback.Error,VLCState.Stopped or VLCState.NothingSpecial=>PresencePlayback.Stopped,
            _=>PresencePlayback.Opening
        };
        var programme=_discordProgrammes.FirstOrDefault(p=>p.Start<=now&&p.End>now);
        _discordState.Observe(_discordGeneration,status,player.Time,player.Length,now,programme,_discordForceTiming,player.Rate);
        _discordForceTiming=false;_discord.Publish(_discordState.Activity);
        if(_settings.DiscordRichPresence&&_discordState.Activity is{Image:null}&&
           _discordArtworkRequestedGeneration!=_discordGeneration&&
           (_discordArtworkTask is null||_discordArtworkTask.IsCompleted))
        {
            _discordArtworkRequestedGeneration=_discordGeneration;
            _discordArtworkTask=RefreshDiscordArtworkAsync(_discordItem,_discordGeneration,_discordGuideCancellation?.Token??_life.Token);
        }
        if(_settings.DiscordRichPresence&&_discordItem.Kind==ContentKind.Live&&status is PresencePlayback.Playing or PresencePlayback.Paused
            &&now>=_discordGuideNext&&(_discordGuideTask is null||_discordGuideTask.IsCompleted))
        {
            _discordGuideNext=now.AddSeconds(30);
            _discordGuideTask=RefreshDiscordGuideAsync(_discordItem,_discordGeneration,_discordGuideCancellation?.Token??_life.Token);
        }
    }
    private async Task RefreshDiscordArtworkAsync(ContentItem item,long generation,CancellationToken ct)
    {
        try
        {
            // Use only title/EPG ID for public catalogues. Never submit IPTV URLs or credentials.
            var lookup=new ContentItem{Name=item.Name,Kind=item.Kind,SeriesName=item.SeriesName,EpgId=item.EpgId};
            var artwork=await ArtworkService.Discovery.ResolveAsync(lookup,ct);
            if(artwork is null||ct.IsCancellationRequested||_closing||generation!=_discordGeneration)return;
            if(_discordState.SetArtwork(generation,artwork,DiscordPrivateValues(item)))_discord?.Publish(_discordState.Activity);
        }
        catch(OperationCanceledException){}
        catch{ /* Artwork lookup must not affect playback or Discord text presence. */ }
    }
    private async Task RefreshDiscordGuideAsync(ContentItem item,long generation,CancellationToken ct)
    {
        try
        {
            // Independent of the day selected in the visible EPG browser.
            var programmes=await _store.EpgAsync(item,DateTimeOffset.UtcNow).WaitAsync(ct);
            if(programmes.Count==0&&_sources.FirstOrDefault(s=>s.Id==item.SourceId) is{} source)
            {
                using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(5));
                programmes=await _providers.ShortEpgAsync(source,item,timeout.Token);
            }
            if(ct.IsCancellationRequested||_closing||generation!=_discordGeneration)return;
            var secrets=DiscordPrivateValues(item);
            _discordProgrammes=programmes.Select(p=>new PresenceProgramme(
                PresenceMedia.FromContent(new ContentItem{Name=p.Title},secrets).Title,p.Start,p.End)).ToArray();
            UpdateDiscordPlayback();
        }
        catch(OperationCanceledException){}
        catch{ /* Missing EPG keeps the channel-only presence; no playback impact. */ }
    }
    private async Task ReplayWithDiscordAsync()
    {
        if(_target is null)return;
        var item=_discordItem??_current;int version=++_playVersion;BeginDiscordSelection();
        try{await _engine.PlayAsync(_target,_settings,_life.Token);if(item is not null)AcceptDiscordPlayback(item,version);}
        catch{if(version==_playVersion)_discord?.Publish(null);throw;}
    }
    private async Task DisposeDiscordAsync()
    {
        if(_engine is not null)
        {
            var player=_engine.Player;
            player.Playing-=DiscordPlaybackChanged;player.Paused-=DiscordPlaybackChanged;
            player.Stopped-=DiscordPlaybackChanged;player.EndReached-=DiscordPlaybackChanged;
            player.EncounteredError-=DiscordPlaybackChanged;player.LengthChanged-=DiscordPlaybackChanged;
        }
        _discordGuideCancellation?.Cancel();
        if(_discord is not null)await _discord.DisposeAsync();
        if(_discordGuideTask is not null)try{await _discordGuideTask;}catch{}
        if(_discordArtworkTask is not null)try{await _discordArtworkTask;}catch{}
        _discordGuideCancellation?.Dispose();_discord=null;
    }
}
