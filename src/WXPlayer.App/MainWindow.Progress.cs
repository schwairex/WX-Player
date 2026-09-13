using WXPlayer.Core;
using LibVLCSharp.Shared;

namespace WXPlayer.App;

public partial class MainWindow
{
    private bool _progressReady;
    private long _lastPosition,_lastDuration;
    private Task SavePlaybackProgressAsync(bool completed=false)
    {
        if(!_progressReady||_current is not{Kind:ContentKind.Movie or ContentKind.Episode} item||_engine is null)return Task.CompletedTask;
        var player=_engine.Player;
        if(player.State is not(VLCState.Playing or VLCState.Paused or VLCState.Ended))return Task.CompletedTask;
        long duration=player.Length>0?player.Length:_lastDuration;long position=completed&&duration>0?duration:player.Time>=0?player.Time:_lastPosition;
        if(completed)position=Math.Max(position,_lastPosition);_lastPosition=position;_lastDuration=duration;
        return _store.SaveProgressAsync(item,position,duration,completed);
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
        var episodes=source.Kind==SourceKind.Playlist?await _store.PlaylistEpisodesAsync(series,ct):await _providers.EpisodesAsync(source,series,ct);
        var progress=await _store.ProgressForSeriesAsync(series.Id,ct);
        return episodes.Select(item=>item with{Kind=ContentKind.Episode,SeriesId=series.Id,SeriesName=series.Name,Logo=item.Logo.Length>0?item.Logo:series.Logo,Progress=progress.GetValueOrDefault(item.Id)}).ToArray();
    }
    internal Task SmokeSaveProgressAsync()=>SavePlaybackProgressAsync();
    internal Task<IReadOnlyList<ContentItem>> SmokeEpisodesAsync(SourceConfig source,ContentItem series)=>LoadEpisodesAsync(source,series,default);
}
