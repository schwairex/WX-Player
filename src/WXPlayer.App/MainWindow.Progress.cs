using WXPlayer.Core;
using LibVLCSharp.Shared;

namespace WXPlayer.App;

public partial class MainWindow
{
    private bool _progressReady;
    private long _lastPosition,_lastDuration;
    private bool _episodePausedByUser;
    private int _episodeRecoveryAttempts,_episodeRecoveryScheduledVersion=-1;
    private long _episodeRecoveryPosition;
    private void RecoverInterruptedEpisode()
    {
        if(_current is not {Kind:ContentKind.Episode} episode||_episodePausedByUser||!_progressReady||_lastPosition<5000||
           PlaybackProgressPolicy.NaturalEnd(_lastPosition,_lastDuration)||_episodeRecoveryAttempts>=2||_episodeRecoveryScheduledVersion==_playVersion)return;
        int version=_playVersion;_episodeRecoveryScheduledVersion=version;_episodeRecoveryAttempts++;_episodeRecoveryPosition=_lastPosition;
        _ = SafeAsync(async()=>
        {
            try
            {
                await _store.SaveProgressAsync(episode,_lastPosition,_lastDuration,false);
                await Task.Delay(750,_life.Token);
                if(version!=_playVersion||_current?.Id!=episode.Id||_episodePausedByUser||_closing||
                   _engine.Player.State is not(VLCState.Ended or VLCState.Error or VLCState.Stopped))return;
                await PlayItemAsync(episode,recovery:true);
            }
            finally{if(_episodeRecoveryScheduledVersion==version)_episodeRecoveryScheduledVersion=-1;}
        });
    }
    private Task SavePlaybackProgressAsync(bool completed=false)
    {
        if(!_progressReady||_current is not{Kind:ContentKind.Movie or ContentKind.Episode} item||_engine is null)return Task.CompletedTask;
        var player=_engine.Player;
        if(player.State is not(VLCState.Playing or VLCState.Paused or VLCState.Ended))return Task.CompletedTask;
        long duration=player.Length>0?player.Length:_lastDuration;
        long position=player.State==VLCState.Ended?_lastPosition:player.Time>=0?player.Time:_lastPosition;
        bool naturalEnd=completed&&PlaybackProgressPolicy.NaturalEnd(position,duration);
        _lastPosition=position;_lastDuration=duration;
        return _store.SaveProgressAsync(item,position,duration,naturalEnd);
    }
    private async Task RestorePlaybackProgressAsync(WatchProgress? progress,int version,CancellationToken ct)
    {
        try
        {
            for(int i=0;i<150;i++)
            {
                ct.ThrowIfCancellationRequested();if(version!=_playVersion)return;
                if(_engine.Player.IsPlaying&&(_engine.Player.Length>0||_engine.Player.IsSeekable))
                {
                    if(progress is {Completed:false,PositionMs:>=5000}&&_engine.Player.IsSeekable)
                        _engine.Player.Time=_engine.Player.Length>0?Math.Min(progress.PositionMs,Math.Max(0,_engine.Player.Length-1000)):progress.PositionMs;
                    _progressReady=true;return;
                }
                await Task.Delay(100,ct);
            }
            if(version==_playVersion)_progressReady=true;
        }catch(OperationCanceledException){}
    }
    private async Task<IReadOnlyList<ContentItem>> LoadEpisodesAsync(SourceConfig source,ContentItem series,CancellationToken ct)
    {
        string key=source.Id+"|"+series.Id;
        var episodes=_episodeCacheKey==key?_episodeCache:source.Kind==SourceKind.Playlist?await _store.PlaylistEpisodesAsync(series,ct):await _providers.EpisodesAsync(source,series,ct);
        var progress=await _store.ProgressForSeriesAsync(series.Id,ct);
        ct.ThrowIfCancellationRequested();
        var result=episodes.Select(item=>item with{Kind=ContentKind.Episode,SeriesId=series.Id,SeriesName=series.Name,Logo=item.Logo.Length>0?item.Logo:series.Logo,Progress=progress.GetValueOrDefault(item.Id)}).ToArray();
        _episodeCacheKey=result.Length>0?key:"";_episodeCache=result;return result;
    }
    internal Task SmokeSaveProgressAsync()=>SavePlaybackProgressAsync();
    internal void SmokeRecoverInterruptedEpisode()=>RecoverInterruptedEpisode();
    internal Task<IReadOnlyList<ContentItem>> SmokeEpisodesAsync(SourceConfig source,ContentItem series)=>LoadEpisodesAsync(source,series,default);
}
