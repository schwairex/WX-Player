using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WXPlayer.Core;

namespace WXPlayer.App;

public partial class MainWindow
{
    private SeriesEpisodePanel _seriesPanel=null!;
    private string _episodeCacheKey="";
    private IReadOnlyList<ContentItem> _episodeCache=[];
    private Window? _nextEpisodeWindow;
    private string? _dismissedEpisode, _offeredEpisode;
    private void InitializeSeries()
    {
        _seriesPanel=new SeriesEpisodePanel(item=>SafeAsync(()=>PlayItemAsync(item)),direction=>SafeAsync(()=>PlayAdjacentEpisodeAsync(direction)),()=>SafeAsync(async()=>{_episodeCacheKey="";await LoadGuideAsync();})){Visibility=Visibility.Collapsed};
        Grid.SetRow(_seriesPanel,2);ViewingPanel.Children.Insert(ViewingPanel.Children.IndexOf(GuideSplitter),_seriesPanel);
    }
    private void ApplySeriesVisibility()
    {
        if(_seriesPanel is null)return;
        bool series=_current?.Kind==ContentKind.Episode;
        _seriesPanel.Visibility=series&&!_fullscreen?Visibility.Visible:Visibility.Collapsed;
        GuidePanel.Visibility=!series&&!_fullscreen?Visibility.Visible:Visibility.Collapsed;
        if(!series||!_fullscreen)CloseNextEpisode();
    }
    private async Task LoadSeriesPanelAsync(ContentItem episode,CancellationToken ct)
    {
        _seriesPanel.Loading(episode);ApplySeriesVisibility();
        try
        {
            var source=_sources.FirstOrDefault(s=>s.Id==episode.SourceId);if(source is null)return;
            var series=await _store.FindAsync(episode.SeriesId,ct);
            if(series is null) { if(!ct.IsCancellationRequested&&_current?.Id==episode.Id)_seriesPanel.Error();return; }
            var episodes=await LoadEpisodesAsync(source,series,ct);
            if(ct.IsCancellationRequested||_current?.Id!=episode.Id)return;
            _seriesPanel.Show(episode,EpisodeNavigation.Ordered(episode,episodes));UpdateNextEpisode();
        }
        catch(OperationCanceledException){}
        catch{if(!ct.IsCancellationRequested&&_current?.Id==episode.Id)_seriesPanel.Error();}
    }
    private Task PlayAdjacentEpisodeAsync(int direction)
    {
        var next=EpisodeNavigation.Adjacent(_current,_episodeCache,direction);
        return next is null?Task.CompletedTask:PlayItemAsync(next,true);
    }
    private void CloseNextEpisode(){_nextEpisodeWindow?.Close();_nextEpisodeWindow=null;_offeredEpisode=null;_fsNextItem=null;_fsNextCurrent=null;if(_fsNextChip is not null)_fsNextChip.Visibility=Visibility.Collapsed;}
    private void UpdateNextEpisode()
    {
        if(_engine is null)return;
        var current=_current;var next=EpisodeNavigation.Adjacent(current,_episodeCache,1);
        bool ended=_engine.Player.State==LibVLCSharp.Shared.VLCState.Ended;
        long duration=_engine.Player.Length>0?_engine.Player.Length:_lastDuration;
        long position=ended?_lastPosition:_engine.Player.Time;
        bool show=EpisodeNavigation.OfferNext(current,next,_fullscreen,position,duration)&&_dismissedEpisode!=current?.Id&&!_closing;
        if(!show){CloseNextEpisode();return;}
        _offeredEpisode = current!.Id; _fsNextCurrent = current.Id; _fsNextItem = next;
        if (_fsNextChip is not null) { _fsNextChip.Visibility = Visibility.Visible; _fsNextChip.ToolTip = next!.EpisodeLabel + " — " + next.Name; }
    }
    private void PlaceNextEpisode() { }
    internal SeriesEpisodePanel SmokeSeriesPanel=>_seriesPanel;
    internal bool SmokeNextEpisodeVisible=>_fsNextChip?.IsVisible==true;
    internal void SmokeNextEpisodeTick()=>UpdateNextEpisode();
    internal Task SmokeNextEpisodeAsync()=>PlayAdjacentEpisodeAsync(1);
    internal void SmokeClickNextEpisode()=>_fsNextChip?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    internal void SmokeDismissNextEpisode(){_dismissedEpisode=_current?.Id;CloseNextEpisode();}
}
