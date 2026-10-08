using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Input;

namespace WXPlayer.App;

internal static class FullscreenV2Smoke
{
    internal static async Task CheckAsync(MainWindow window, PlaybackEngine engine, Dictionary<string, object> results)
    {
        T? Field<T>(string name) where T:class => typeof(MainWindow).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(window) as T;
        void Check(string name,bool value){results[name]=value;if(!value)throw new InvalidOperationException(name);}
        string originalSection=Field<string>("_section")!; var popupButton=Field<Button>("_fsSpeedButton")!;
        popupButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(100);
        var popup=Field<Popup>("_fsPopup")!;
        var rate=Descendants<Button>(popup.Child).Single(b=>AutomationProperties.GetName(b)=="1.25×");
        rate.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(100);
        Check("fullscreenV2SpeedChangesActualPlayback",Math.Abs(engine.Player.Rate-1.25)<.01&&!popup.IsOpen);
        engine.Player.SetRate(1);
        var tracksButton=Field<Button>("_fsTracksButton")!;tracksButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(100);
        popup=Field<Popup>("_fsPopup")!;
        Check("fullscreenV2TracksUseRealDescriptions",popup.IsOpen&&Descendants<Button>(popup.Child).Count()>=engine.Player.AudioTrackDescription.Length+1);
        var off=Descendants<Button>(popup.Child).Single(b=>AutomationProperties.GetName(b)=="Kapalı");
        void KeyAt(Button b,Key key) => b.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(b),0,key){RoutedEvent=Keyboard.PreviewKeyDownEvent});
        off.Focus();KeyAt(off,Key.Space);await Task.Delay(250);Check("fullscreenV2PopupSpacePauses",!engine.Player.IsPlaying);KeyAt(off,Key.Space);await Task.Delay(250);Check("fullscreenV2PopupSpaceResumes",engine.Player.IsPlaying);
        KeyAt(off,Key.Escape);Check("fullscreenV2PopupEscapeKeepsFullscreen",!popup.IsOpen&&window.SmokeControlsVisible&&window.FullscreenChannels is not null);
        tracksButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(100);popup=Field<Popup>("_fsPopup")!;
        off=Descendants<Button>(popup.Child).Single(b=>AutomationProperties.GetName(b)=="Kapalı");off.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check("fullscreenV2SubtitleOffApplies",engine.Player.Spu<0&&!popup.IsOpen);
        var chrome=Window.GetWindow(window.FullscreenChannels!)!;
        var root=(FrameworkElement)chrome.Content;
        foreach(var size in new[]{(1280d,720d),(2560d,1440d)})
        {
            chrome.Width=size.Item1;chrome.Height=size.Item2;chrome.UpdateLayout();await Task.Delay(50);chrome.UpdateLayout();
            var rem=Math.Clamp(.0062*chrome.ActualWidth+8,14,22);
            var drawer=window.FullscreenBrowser!.TransformToAncestor(chrome).TransformBounds(new Rect(0,0,window.FullscreenBrowser.ActualWidth,window.FullscreenBrowser.ActualHeight));
            Check("fullscreenV2ScalesAt"+size.Item1,Math.Abs(drawer.Width-26*rem)<1&&Math.Abs(root.ActualWidth*((ScaleTransform)root.LayoutTransform).ScaleX-chrome.ActualWidth)<1);
        }
        window.SmokeRevealControls();chrome.UpdateLayout();
        for(int i=0;i<6;i++)window.SmokeToggleFullscreenDrawer();
        await Task.Delay(400);
        Check("fullscreenV2RapidDrawerTogglesKeepFinalState",window.SmokeFullscreenDrawerOpen&&window.FullscreenBrowser!.IsVisible);
        var search=Field<TextBox>("_fsSearch")!;string previousSearch=search.Text;search.Text="Sintel";
        for(int i=0;i<40&&window.ChannelList.Items.Count!=1;i++)await Task.Delay(50);
        Check("fullscreenV2SearchUsesExistingLibrary",window.ChannelList.Items.Count==1&&window.FullscreenChannels!.Items.Count==1);
        search.Text=previousSearch;
        for(int i=0;i<40&&window.ChannelList.Items.Count<2;i++)await Task.Delay(50);
        Check("fullscreenV2SearchClears",window.FullscreenChannels!.Items.Count==window.ChannelList.Items.Count&&window.ChannelList.Items.Count>=2);
        var tabs=Field<Dictionary<string,Button>>("_fsTabs")!;tabs["episodes"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(100);search.Text="Sintel";tabs["movie"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        for(int i=0;i<40&&window.ChannelList.Items.Count!=1;i++)await Task.Delay(50);
        Check("fullscreenV2EpisodeToMovieKeepsVisibleSearch",window.SearchBox.Text=="Sintel"&&window.ChannelList.Items.Count==1);
        search.Text=previousSearch;
        for(int i=0;i<40&&window.ChannelList.Items.Count<2;i++)await Task.Delay(50);
        bool recordEnabled=window.RecordButton.IsEnabled;window.RecordButton.IsEnabled=false;window.SmokeShortcut(Key.R);Check("fullscreenV2RecordingShortcutRespectsBusyState",!engine.Recording);window.RecordButton.IsEnabled=recordEnabled;
        await window.SmokeBrowseAsync(originalSection); window.UpdateLayout();
        Check("fullscreenV2FilteringKeepsVideoFullSize",Math.Abs(window.Video.ActualWidth-window.Root.ActualWidth)<1&&Math.Abs(window.Video.ActualHeight-window.Root.ActualHeight)<1);
        Check("fullscreenV2SeekUsesWholeTrack",Field<MediaSlider>("_fsSeek")!.PointInset==0&&new MediaSlider().PointInset==10);
        // Keyboard Space must control playback even when a library button has focus.
        void Space() => tracksButton.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(tracksButton),0,Key.Space){RoutedEvent=Keyboard.PreviewKeyDownEvent});
        tracksButton.Focus();Space();await Task.Delay(250);Check("fullscreenV2FocusedButtonSpacePauses",!engine.Player.IsPlaying&&engine.Player.Time>0);Space();await Task.Delay(250);Check("fullscreenV2FocusedButtonSpaceResumes",engine.Player.IsPlaying);
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T:DependencyObject
    {
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T value)yield return value;foreach(var nested in Descendants<T>(child))yield return nested;}
    }
}
