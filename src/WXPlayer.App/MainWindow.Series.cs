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
    private void CloseNextEpisode(){_nextEpisodeWindow?.Close();_nextEpisodeWindow=null;_offeredEpisode=null;}
    private void UpdateNextEpisode()
    {
        if(_engine is null)return;
        var current=_current;var next=EpisodeNavigation.Adjacent(current,_episodeCache,1);
        bool ended=_engine.Player.State==LibVLCSharp.Shared.VLCState.Ended;
        long duration=_engine.Player.Length>0?_engine.Player.Length:_lastDuration;
        long position=ended?duration:_engine.Player.Time;
        bool show=EpisodeNavigation.OfferNext(current,next,_fullscreen,position,duration)&&_dismissedEpisode!=current?.Id&&!_closing;
        if(!show){CloseNextEpisode();return;}
        if(_nextEpisodeWindow is not null&&_offeredEpisode==current!.Id){if(IsActive&&!_nextEpisodeWindow.IsVisible)_nextEpisodeWindow.Show();return;}
        if(!IsActive&&_floatingControls?.IsActive!=true)return;
        CloseNextEpisode();_offeredEpisode=current!.Id;
        var panel=new StackPanel{Margin=new Thickness(20,16,20,18)};
        var header=new DockPanel();panel.Children.Add(header);
        var dismiss=PremiumWindow.Action("×",()=>{_dismissedEpisode=current.Id;CloseNextEpisode();});dismiss.Padding=new Thickness(6,0,6,0);dismiss.MinWidth=28;dismiss.MinHeight=28;dismiss.ToolTip="Bu bölüm için öneriyi kapat";System.Windows.Automation.AutomationProperties.SetName(dismiss,"Sonraki bölüm önerisini kapat");DockPanel.SetDock(dismiss,Dock.Right);header.Children.Add(dismiss);
        var heading=PremiumWindow.Text("SIRADAKİ BÖLÜM",10,"AccentPrimaryBrush");heading.FontWeight=FontWeights.SemiBold;heading.VerticalAlignment=VerticalAlignment.Center;header.Children.Add(heading);
        var episode=PremiumWindow.Text(next!.EpisodeLabel,17);episode.FontWeight=FontWeights.SemiBold;episode.Margin=new Thickness(0,10,0,0);panel.Children.Add(episode);
        var label=PremiumWindow.Text(next.Name,11,"TextSecondaryBrush");label.TextWrapping=TextWrapping.NoWrap;label.TextTrimming=TextTrimming.CharacterEllipsis;label.ToolTip=next.Name;label.Margin=new Thickness(0,4,0,14);panel.Children.Add(label);
        var play=PremiumWindow.Action("▶   Sonraki bölümü oynat",async()=>{if(_current?.Id!=current.Id)return;CloseNextEpisode();await SafeAsync(()=>PlayItemAsync(next,true));},true);play.Margin=new Thickness(0);play.MinHeight=42;panel.Children.Add(play);
        _nextEpisodeWindow=new Window{Owner=this,Title="Sonraki bölüm",Style=null,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,AllowsTransparency=true,Background=Brushes.Transparent,ShowInTaskbar=false,ShowActivated=false,Width=390,Height=166,FontFamily=(FontFamily)FindResource("AppFont"),Foreground=PremiumWindow.Brush("TextPrimaryBrush"),Content=new Border{Background=PremiumWindow.Brush("#F5131A24"),BorderBrush=PremiumWindow.Brush("BorderSubtleBrush"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(14),Child=panel}};
        _nextEpisodeWindow.PreviewKeyDown+=Window_KeyDown;_nextEpisodeWindow.Show();PlaceNextEpisode();
    }
    private void PlaceNextEpisode(){if(_nextEpisodeWindow is not null)_fullscreenPlacement.PlaceEpisodePrompt(this,_nextEpisodeWindow,_floatingControls?.IsVisible==true?_floatingControls.Height+36:24);}
    internal SeriesEpisodePanel SmokeSeriesPanel=>_seriesPanel;
    internal bool SmokeNextEpisodeVisible=>_nextEpisodeWindow?.IsVisible==true;
    internal void SmokeNextEpisodeTick()=>UpdateNextEpisode();
    internal Task SmokeNextEpisodeAsync()=>PlayAdjacentEpisodeAsync(1);
    internal void SmokeClickNextEpisode()=>((_nextEpisodeWindow?.Content as Border)?.Child as StackPanel)?.Children.OfType<Button>().LastOrDefault()?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    internal void SmokeDismissNextEpisode(){_dismissedEpisode=_current?.Id;CloseNextEpisode();}
}
