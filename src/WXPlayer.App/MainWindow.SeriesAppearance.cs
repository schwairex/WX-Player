using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WXPlayer.Core;

namespace WXPlayer.App;

public partial class MainWindow
{
    private bool SeriesWatchingVisible=>EmbeddedPlayerVisible&&_section=="series"&&_catalogPlaying&&_current?.Kind==ContentKind.Episode;
    private bool LibraryWatchingVisible=>FilmWatchingVisible||SeriesWatchingVisible;
    private bool _libraryScopeSeries;
    private TextBlock? _seriesHeading,_seriesCode,_seriesCaption;

    private void AttachSeriesWatchingPresentation(FilmVisualScope scope)
    {
        var presentation=_seriesPanel.AttachWatchingAppearance(this,SeekSlider,PreviousChannelButton,NextChannelButton);scope.Undo(presentation.Dispose);
        scope.Undo(()=>{_seriesHeading=null;_seriesCode=null;_seriesCaption=null;});
    }
    private void UpdateSeriesWatchingPresentation()
    {
        if(_current is not{} item)return;
        _seriesHeading!.Text=SeriesDisplay.Title(item);_seriesHeading.ToolTip=item.Name;
        _seriesCode!.Text=SeriesDisplay.Code(item);_seriesCode.Visibility=_seriesCode.Text.Length==0?Visibility.Collapsed:Visibility.Visible;
        _seriesCaption!.Text=SeriesDisplay.Caption(item);_seriesCaption.Visibility=_seriesCaption.Text.Length==0?Visibility.Collapsed:Visibility.Visible;
    }
}
