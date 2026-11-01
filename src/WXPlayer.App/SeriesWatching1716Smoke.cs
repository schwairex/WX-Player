using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WXPlayer.Core;

namespace WXPlayer.App;

// Explicit, isolated presentation QA; real controls, existing playback and store.
internal static class SeriesWatching1716Smoke
{
    internal static async Task RunAsync(MainWindow w,LibraryStore store,PlaybackEngine engine,Dictionary<string,object> r)
    {
        void Check(string name,bool ok){r[name]=ok;if(!ok)throw new InvalidOperationException(name);}
        if(App.Arguments.Contains("--series1716-format-only")||App.Arguments.Contains("--series1716-unicode-only"))
        {
            var type=typeof(MainWindow).Assembly.GetType("WXPlayer.App.SeriesDisplay");Check("seriesFormatterExists",type is not null);
            string Text(string method,ContentItem item)=>(string)type!.GetMethod(method,BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[item])!;
            var ep=new ContentItem{Kind=ContentKind.Episode,SeriesName="56 Gün",Season=1,Episode=1,Name="S01 · B01  56 Gün - S01E01 - 1. Bölüm"};
            Check("seriesTitleKeepsNameNumber",Text("Title",ep)=="56 Gün");Check("seriesEpisodeCode",Text("Code",ep)=="S01E01");Check("seriesCaption",Text("Caption",ep)=="Sezon 1 · Bölüm 1");Check("seriesEpisodeCardTitle",Text("EpisodeTitle",ep)=="1. Bölüm");
            Check("seriesUnknownTitleRaw",Text("Title",ep with{SeriesName="",Season=0,Episode=0,Name="Özel – İSTANBUL"})=="Özel – İSTANBUL");
            if(App.Arguments.Contains("--series1716-unicode-only")){Check("seriesUnicodeDigitsRaw",Text("EpisodeTitle",ep with{Name="Özel S١E٢"})=="Özel S١E٢");return;}
            Check("seriesDescriptiveCardRaw",Text("EpisodeTitle",ep with{Name="Pilot"})=="Pilot");
            Check("seriesUnicodeDigitsRaw",Text("EpisodeTitle",ep with{Name="Özel S١E٢"})=="Özel S١E٢");
            Check("seriesNamedSuffix",Text("EpisodeTitle",ep with{Name="56 Gün - 1. Bölüm"})=="1. Bölüm");Check("seriesConflictingCodeRaw",Text("EpisodeTitle",ep with{Name="56 Gün S01E03"})=="56 Gün S01E03");
            Check("seriesUnknownCardRaw",Text("EpisodeTitle",ep with{Episode=0,Name="Pilot"})=="Pilot");return;
        }
        string media=new Uri(Path.GetFullPath(App.Arguments[Array.IndexOf(App.Arguments,"--media")+1])).AbsoluteUri;
        var source=new SourceConfig{Id="series1716",Name="Yerel QA",Kind=SourceKind.Playlist};
        var episodes=new List<ContentItem>();
        foreach(int season in new[]{1,3,5,6,7,8,9,10,11,12,13,14})
            for(int e=1;e<=(season==1?120:season==3?10:1);e++)episodes.Add(new(){Id=$"series1716-{season}-{e}",SourceId=source.Id,SeriesId="series1716-show",SeriesName="56 Gün",Season=season,Episode=e,Name=$"56 Gün - S{season:00}E{e:00} - {e}. Bölüm",Kind=ContentKind.Episode,Category="TR ✨ Diziler",Url=media});
        for(int e=1;e<=10;e++)episodes.Add(new(){Id=$"series1716-single-{e}",SourceId=source.Id,SeriesId="series1716-single",SeriesName="Tek Sezon "+string.Concat(Enumerable.Repeat("İstanbul WWW ",30)),Season=1,Episode=e,Name=$"Tek Sezon - S01E{e:00}",Kind=ContentKind.Episode,Category="Original ✨ Series",Url=media});
        var movie=new ContentItem{Id="series1716-movie",SourceId=source.Id,Name="Yerel film",Kind=ContentKind.Movie,Category="Filmler",Url=media};
        var live=movie with{Id="series1716-live",Name="Yerel kanal",Category="Canlı kanallar",Kind=ContentKind.Live};
        async IAsyncEnumerable<ContentItem> Items(){foreach(var ep in episodes)yield return ep;yield return movie;yield return live;await Task.Yield();}
        await store.ImportAsync(source,Items(),null,default);await store.SaveProgressAsync(episodes[1],52000,52000,true);await store.SaveProgressAsync(episodes[2],12000,52000,false);
        await w.SmokeRefreshAsync(source.Id);w.Width=1920;w.Height=1080;r["windowDpi"]=VisualTreeHelper.GetDpi(w).PixelsPerInchX;
        foreach(string section in new[]{"movie","live","home"})
        {
            w.Width=1920;w.Height=1080;w.UpdateLayout();await Task.Delay(200);
            await w.SmokeBrowseAsync(section);
            if(section!="home"){await w.SmokePlayAsync(section=="movie"?movie:live);await FilmWatching1715Smoke.Wait(()=>engine.Player.IsPlaying);if(section=="movie")((Button)w.SmokeCatalog.ResumeControl).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
            w.UpdateLayout();await Task.Delay(180);var signature=Signature(w);r[section+"Signature"]=signature;
            File.WriteAllText(Path.Combine(App.DataDirectory,section+"-signature.json"),JsonSerializer.Serialize(signature));Settings1711Smoke.Save(w,section+"-before.png");
        }
        await w.SmokeBrowseAsync("series");var show=(await store.FindAsync("series1716-show"))!;var loaded=await w.SmokeEpisodesAsync(source,show);
        await w.SmokePlayAsync(loaded.Single(e=>e.Season==1&&e.Episode==1));await FilmWatching1715Smoke.Wait(()=>engine.Player.IsPlaying&&w.SmokeSeriesPanel.Episodes.Items.Count==120);
        ((Button)w.SmokeCatalog.ResumeControl).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));w.UpdateLayout();await Task.Delay(180);
        Settings1711Smoke.Save(w,"series-wide.png");
        if(App.Arguments.Contains("--series1716-baseline-only"))return;
        var categories=Settings1711Smoke.Descendants<ListBox>(w).SingleOrDefault(e=>AutomationProperties.GetName(e)=="Dizi kategorileri");Check("seriesSharesCategoryColumn",categories is{IsVisible:true}&&categories.ActualWidth>150);
        Check("seriesHasNoMovieInfo",!Settings1711Smoke.Descendants<Border>(w).Any(b=>AutomationProperties.GetName(b)=="Film bilgisi"&&b.IsVisible));
        Check("seriesNativeAligned",Aligned(w.Video));
        var panel=w.SmokeSeriesPanel;var list=panel.Episodes;
        var overlay=w.OwnedWindows.Cast<Window>().Single(x=>x.Title=="WX Player live controls");IntPtr hwnd=engine.Player.Hwnd;
        var pills=Settings1711Smoke.Descendants<ListBox>(panel).Single(x=>AutomationProperties.GetName(x)=="Dizinin sezonları");
        Button Arrow(string name)=>Settings1711Smoke.Descendants<Button>(panel).Single(x=>AutomationProperties.GetName(x)==name);
        var seasonHost=(Border)((Grid)pills.Parent).Parent;
        Check("seriesSeasonHostHasCssCorners",seasonHost.CornerRadius.TopLeft<=seasonHost.ActualHeight/2+.1);
        r["seriesScrollbars"]=Settings1711Smoke.Descendants<ScrollBar>(list).Select(b=>new{orientation=b.Orientation.ToString(),b.IsVisible,b.ActualWidth,b.ActualHeight,height=double.IsFinite(b.Height)?(double?)b.Height:null,b.MinHeight,heightSource=DependencyPropertyHelper.GetValueSource(b,FrameworkElement.HeightProperty).BaseValueSource.ToString(),liveStyle=ReferenceEquals(b.Style,panel.FindResource("LiveScrollBar")),styleSource=DependencyPropertyHelper.GetValueSource(b,FrameworkElement.StyleProperty).BaseValueSource.ToString()}).ToArray();
        Check("seriesThinHorizontalScroll",Settings1711Smoke.Descendants<ScrollBar>(list).Any(b=>b.Orientation==Orientation.Horizontal&&b.IsVisible&&ReferenceEquals(b.Style,panel.FindResource("LiveScrollBar"))&&b.Template.FindName("ScrollbarChrome",b) is FrameworkElement chrome&&chrome.ActualHeight<=8));
        Check("seriesPillsShareOriginalChoice",ReferenceEquals(pills.SelectedItem,panel.Seasons.SelectedItem)&&pills.Items.Count==12);
        Check("seriesFirstSeasonArrowDisabled",!Arrow("Önceki sezon").IsEnabled&&Arrow("Sonraki sezon").IsEnabled);
        Check("seriesPlayerFirstEpisodeDisabled",!w.PreviousChannelButton.IsEnabled&&w.NextChannelButton.IsEnabled);
        var bridge=Settings1711Smoke.Descendants<Control>(w.ViewingPanel).Single(c=>AutomationProperties.GetName(c)=="Oynatıcı kontrolleri");w.Activate();bridge.Focus();await Task.Delay(100);
        r["seriesTabBridgeReachesEnabledControl"]=ReferenceEquals(Keyboard.FocusedElement,w.PlayButton);
        var containers=Settings1711Smoke.Descendants<ListBoxItem>(list).ToArray();
        r["horizontalDiagnostics"]=new{containers=containers.Length,list.ActualWidth,list.ActualHeight,panels=Settings1711Smoke.Descendants<VirtualizingStackPanel>(list).Select(p=>p.Orientation.ToString()).ToArray(),virtualizing=VirtualizingPanel.GetIsVirtualizing(list),scrolling=ScrollViewer.GetCanContentScroll(list)};
        Check("seriesHorizontalVirtualization",Settings1711Smoke.Descendants<VirtualizingStackPanel>(list).Any(x=>x.Orientation==Orientation.Horizontal)&&containers.Length>0&&containers.Length<30);
        Check("seriesWatchedFromProgress",Settings1711Smoke.Descendants<TextBlock>(list).Any(t=>t.Text=="✓ İzlendi"&&t.IsVisible));
        int changes=0;SelectionChangedEventHandler changed=(_,_)=>changes++;panel.Seasons.SelectionChanged+=changed;
        int version=w.SmokePlaybackVersion;string raw=w.NowTitle.Text;long before=engine.Player.Time;
        Arrow("Sonraki sezon").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(100);panel.Seasons.SelectionChanged-=changed;
        Check("seriesSeasonOneOriginalEvent",changes==1&&panel.Seasons.SelectedIndex==1&&list.Items.Count==10&&ReferenceEquals(pills.SelectedItem,panel.Seasons.SelectedItem));
        Check("seriesBrowsingSeasonKeepsVideo",w.NowTitle.Text==raw&&w.SmokePlaybackVersion==version&&engine.Player.IsPlaying&&engine.Player.Time>=before-100);
        Check("seriesOtherSeasonHasNoPlayingCard",!Settings1711Smoke.Descendants<TextBlock>(list).Any(t=>t.Text=="Şimdi oynuyor"&&t.IsVisible));
        pills.SelectedIndex=11;await Task.Delay(80);Check("seriesLastSeasonArrowDisabled",!Arrow("Sonraki sezon").IsEnabled&&Arrow("Önceki sezon").IsEnabled);
        pills.SelectedIndex=0;w.UpdateLayout();await Task.Delay(100);
        var current=(ContentItem)list.Items[0];var first=Card(list,current);w.Activate();first.Focus();before=engine.Player.Time;double volume=w.VolumeSlider.Value;
        PressKey(w,System.Windows.Input.Key.Right);await Task.Delay(100);
        Check("seriesCardRightOnlyFocus",Keyboard.FocusedElement is Button{Tag:ContentItem focused}&&focused.Episode==2&&w.NowTitle.Text==raw&&w.SmokePlaybackVersion==version&&Equals(list.SelectedItem,current)&&w.VolumeSlider.Value==volume&&engine.Player.Time<before+2500);
        PressKey(w,System.Windows.Input.Key.Left);Check("seriesCardLeftOnlyFocus",Keyboard.FocusedElement is Button{Tag:ContentItem left}&&left.Episode==1&&w.NowTitle.Text==raw);
        PressKey(w,System.Windows.Input.Key.Right);
        var focusedButton=Keyboard.FocusedElement as Button??throw new InvalidOperationException("Episode card has no focus");
        focusedButton.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(focusedButton),Environment.TickCount,System.Windows.Input.Key.Enter){RoutedEvent=Keyboard.KeyDownEvent});
        await FilmWatching1715Smoke.Wait(()=>w.NowTitle.Text.Contains("S01E02")&&engine.Player.IsPlaying&&panel.Episodes.SelectedItem is ContentItem{Episode:2});
        Check("seriesEnterUsesOriginalPlay",w.SmokePlaybackVersion==version+1&&w.PreviousChannelButton.IsEnabled);
        w.Activate();w.Video.Focus();await FilmWatching1715Smoke.Wait(()=>engine.Player.Length>30000);before=engine.Player.Time;
        PressKey(w,System.Windows.Input.Key.Right);await FilmWatching1715Smoke.Wait(()=>engine.Player.Time>=before+8000);before=engine.Player.Time;
        PressKey(w,System.Windows.Input.Key.Left);await FilmWatching1715Smoke.Wait(()=>engine.Player.Time<before-7000);Check("seriesPlayerSeekUnchanged",true);
        PressKey(w,System.Windows.Input.Key.Up);Check("seriesPlayerVolumeUp",w.VolumeSlider.Value==volume+5);PressKey(w,System.Windows.Input.Key.Down);Check("seriesPlayerVolumeDown",w.VolumeSlider.Value==volume);
        bool muted=engine.Player.Mute;PressKey(w,System.Windows.Input.Key.M);await FilmWatching1715Smoke.Wait(()=>engine.Player.Mute!=muted);PressKey(w,System.Windows.Input.Key.M);await FilmWatching1715Smoke.Wait(()=>engine.Player.Mute==muted);
        PressKey(w,System.Windows.Input.Key.Space);await FilmWatching1715Smoke.Wait(()=>engine.Player.State==LibVLCSharp.Shared.VLCState.Paused);PressKey(w,System.Windows.Input.Key.Space);await FilmWatching1715Smoke.Wait(()=>engine.Player.IsPlaying);Check("seriesPlayerSpaceAndMute",true);
        PressKey(w,System.Windows.Input.Key.PageDown);await FilmWatching1715Smoke.Wait(()=>w.NowTitle.Text.Contains("S01E03")&&engine.Player.IsPlaying);PressKey(w,System.Windows.Input.Key.PageUp);await FilmWatching1715Smoke.Wait(()=>w.NowTitle.Text.Contains("S01E02")&&engine.Player.IsPlaying);Check("seriesPlayerAdjacentUnchanged",true);
        var far=loaded.Single(e=>e.Season==1&&e.Episode==80);await w.SmokePlayAsync(far);await FilmWatching1715Smoke.Wait(()=>engine.Player.IsPlaying&&panel.Episodes.SelectedItem is ContentItem{Episode:80});w.UpdateLayout();await Task.Delay(120);
        var visible=Card(list,far);var position=visible.TranslatePoint(new Point(),list);Check("seriesPlayingCardScrolled",visible.IsVisible&&position.X>=-2&&position.X<list.ActualWidth);
        Check("seriesHeaderCleanAndRawTooltip",Settings1711Smoke.Descendants<TextBlock>(overlay).Any(t=>t.Text=="56 Gün"&&Equals(t.ToolTip,far.Name))&&Settings1711Smoke.Descendants<TextBlock>(overlay).Any(t=>t.Text=="S01E80")&&w.NowTitle.Text==far.Name);
        var cleanHeading=Settings1711Smoke.Descendants<TextBlock>(overlay).Single(t=>t.Text=="56 Gün"&&Equals(t.ToolTip,far.Name));Check("seriesCodeBesideTitle",cleanHeading.ActualWidth<250);
        panel.Error();w.UpdateLayout();r["seriesErrorRemainsReadable"]=Settings1711Smoke.Descendants<TextBlock>(panel).Any(t=>t.Text.StartsWith("Bölümler alınamadı.")&&t.IsVisible&&t.TextWrapping==TextWrapping.Wrap);panel.Show(far,loaded);w.UpdateLayout();
        var episodeCode=Settings1711Smoke.Descendants<TextBlock>(overlay).Single(t=>t.Text=="S01E80");string savedCode=episodeCode.Text;episodeCode.Text="";r["seriesEmptyCodeHasNoBadge"]=((FrameworkElement)episodeCode.Parent).Visibility==Visibility.Collapsed;episodeCode.Text=savedCode;
        Settings1711Smoke.Save(w,"series-playing-80.png");Settings1711Smoke.Save(overlay,"series-controls.png");
        var browse=(ContentItem)list.Items[110];list.ScrollIntoView(browse);list.UpdateLayout();var browsingCard=Card(list,browse);w.Activate();browsingCard.Focus();int browseVersion=w.SmokePlaybackVersion;
        w.Width=1366;w.Height=768;w.UpdateLayout();await Task.Delay(150);var focusedAfter=Keyboard.FocusedElement as Button;
        r["seriesResizeKeepsBrowsingFocus"]=focusedAfter?.Tag is ContentItem focusedItem&&focusedItem.Equals(browse)&&focusedAfter.TranslatePoint(new Point(),list).X>=-2&&focusedAfter.TranslatePoint(new Point(),list).X<list.ActualWidth&&w.SmokePlaybackVersion==browseVersion;
        w.Width=1920;w.Height=1080;w.UpdateLayout();await Task.Delay(80);

        foreach(var size in new[]{(1366d,768d),(900d,700d)}){w.Width=size.Item1;w.Height=size.Item2;w.UpdateLayout();await Task.Delay(180);Check("seriesFits"+size.Item1,Aligned(w.Video)&&w.Video.ActualHeight>100&&panel.TranslatePoint(new Point(0,panel.ActualHeight),w).Y<w.ActualHeight);Settings1711Smoke.Save(w,"series-"+size.Item1+".png");}
        w.Width=1920;w.Height=1080;PressKey(w,System.Windows.Input.Key.F8);w.UpdateLayout();await Task.Delay(150);Check("seriesComfortVideo",Aligned(w.Video)&&engine.Player.Hwnd==hwnd);Check("seriesF8KeepsBrowsingFocus",Keyboard.FocusedElement is Button{Tag:ContentItem f8Item}&&f8Item.Equals(browse)&&((FrameworkElement)Keyboard.FocusedElement).TranslatePoint(new Point(),list).X>=-2&&((FrameworkElement)Keyboard.FocusedElement).TranslatePoint(new Point(),list).X<list.ActualWidth);Settings1711Smoke.Save(w,"series-comfort.png");PressKey(w,System.Windows.Input.Key.F8);
        w.SmokeFullscreen();await FilmWatching1715Smoke.Wait(()=>engine.Player.IsPlaying&&engine.Player.Length>30000);engine.Player.Time=engine.Player.Length-28000;await Task.Delay(700);w.SmokeNextEpisodeTick();Check("seriesExistingThirtySecondOffer",w.SmokeNextEpisodeVisible);w.SmokeClickNextEpisode();await FilmWatching1715Smoke.Wait(()=>w.NowTitle.Text.Contains("S01E81")&&engine.Player.IsPlaying);w.SmokeFullscreen();w.UpdateLayout();await Task.Delay(120);
        Check("seriesFullscreenRestoresCategory",Settings1711Smoke.Descendants<ListBox>(w).Single(x=>AutomationProperties.GetName(x)=="Dizi kategorileri").SelectedItem is not null&&Aligned(w.Video)&&engine.Player.Hwnd==hwnd);
        w.SmokeToggleMiniPlayer();await FilmWatching1715Smoke.Wait(()=>w.SmokeMiniPlayerVisible);w.SmokeToggleMiniPlayer();await FilmWatching1715Smoke.Wait(()=>!w.SmokeMiniPlayerVisible&&engine.Player.Hwnd==hwnd);Check("seriesMiniReturn",Aligned(w.Video));
        var single=(await store.FindAsync("series1716-single"))!;var singleEpisodes=await w.SmokeEpisodesAsync(source,single);await w.SmokePlayAsync(singleEpisodes[0]);await FilmWatching1715Smoke.Wait(()=>panel.Seasons.Items.Count==1&&engine.Player.IsPlaying);w.UpdateLayout();await Task.Delay(100);
        var singleOverlay=w.OwnedWindows.Cast<Window>().Single(x=>x.Title=="WX Player live controls");
        await FilmWatching1715Smoke.Wait(()=>Settings1711Smoke.Descendants<TextBlock>(singleOverlay).Any(t=>t.Text==single.Name.Trim()));w.UpdateLayout();
        var longHeading=Settings1711Smoke.Descendants<TextBlock>(singleOverlay).First(t=>t.Text==single.Name.Trim());
        r["seriesLongHeadingWidth"]=longHeading.ActualWidth;r["seriesHeadingViewport"]=singleOverlay.ActualWidth;r["seriesLongHeadingFits"]=longHeading.ActualWidth<singleOverlay.ActualWidth;
        Check("seriesSingleSeasonArrowsHidden",!Arrow("Önceki sezon").IsVisible&&!Arrow("Sonraki sezon").IsVisible);
        await w.SmokePlayAsync(singleEpisodes.Last());await FilmWatching1715Smoke.Wait(()=>engine.Player.IsPlaying&&panel.Episodes.SelectedItem is ContentItem{Episode:10});Check("seriesLastEpisodePlayerDisabled",!w.NextChannelButton.IsEnabled&&w.PreviousChannelButton.IsEnabled);
        await FilmWatching1715Smoke.Wait(()=>engine.Player.Length>30000);engine.Player.Time=engine.Player.Length-1800;await Task.Delay(1000);version=w.SmokePlaybackVersion;await FilmWatching1715Smoke.Wait(()=>engine.Player.State==LibVLCSharp.Shared.VLCState.Ended);await Task.Delay(950);
        Check("seriesNaturalEndDoesNotAutoAdvance",w.SmokePlaybackVersion==version&&w.NowTitle.Text==singleEpisodes.Last().Name);Check("seriesNaturalEndSaved",(await store.ProgressAsync(singleEpisodes.Last().Id))?.Completed==true);
        foreach(string section in new[]{"movie","live","home"})
        {
            w.Width=1920;w.Height=1080;w.UpdateLayout();await Task.Delay(200);
            await w.SmokeBrowseAsync(section);
            if(section!="home"){await w.SmokePlayAsync(section=="movie"?movie:live);await FilmWatching1715Smoke.Wait(()=>engine.Player.IsPlaying);if(section=="movie")((Button)w.SmokeCatalog.ResumeControl).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
            w.UpdateLayout();await FilmWatching1715Smoke.Wait(()=>JsonSerializer.Serialize(Signature(w))==JsonSerializer.Serialize(r[section+"Signature"]));var signature=Signature(w);File.WriteAllText(Path.Combine(App.DataDirectory,section+"-after-signature.json"),JsonSerializer.Serialize(signature));Settings1711Smoke.Save(w,section+"-after.png");
            Check("seriesRestores_"+section,JsonSerializer.Serialize(signature)==JsonSerializer.Serialize(r[section+"Signature"]));
        }
        Check("seriesEdgePresentation",Equals(r["seriesErrorRemainsReadable"],true)&&Equals(r["seriesLongHeadingFits"],true)&&Equals(r["seriesTabBridgeReachesEnabledControl"],true)&&Equals(r["seriesResizeKeepsBrowsingFocus"],true)&&Equals(r["seriesEmptyCodeHasNoBadge"],true));

    }
    private static Button Card(ListBox list,ContentItem item)=>Settings1711Smoke.Descendants<Button>(list).Single(b=>b.Tag is ContentItem c&&c.Equals(item));
    internal static object Signature(MainWindow w)
    {
        object Rect(FrameworkElement e){var p=e.TranslatePoint(new Point(),w);return new{X=e.IsVisible?Math.Round(p.X,2):0,Y=e.IsVisible?Math.Round(p.Y,2):0,Width=e.IsVisible?Math.Round(e.ActualWidth,2):0,Height=e.IsVisible?Math.Round(e.ActualHeight,2):0,e.IsVisible};}
        return new{library=Rect(w.LibraryPanel),video=Rect(w.VideoBorder),guide=Rect(w.GuidePanel),home=Rect(w.HomeHost),list=Rect(w.ChannelList),font=w.FontFamily.Source};
    }
    internal static void PressKey(Window w,System.Windows.Input.Key key)=>w.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(w),Environment.TickCount,key){RoutedEvent=Keyboard.PreviewKeyDownEvent});
    internal static bool Aligned(NativeVideoHost video){var a=video.PointToScreen(new Point());var b=video.PointToScreen(new Point(video.ActualWidth,video.ActualHeight));GetWindowRect(video.RenderingHandle,out var rect);return Math.Abs(a.X-rect.Left)<3&&Math.Abs(a.Y-rect.Top)<3&&Math.Abs(b.X-rect.Right)<3&&Math.Abs(b.Y-rect.Bottom)<3;}
    [StructLayout(LayoutKind.Sequential)]private struct Rect{public int Left,Top,Right,Bottom;}
    [DllImport("user32.dll")]private static extern bool GetWindowRect(IntPtr hwnd,out Rect rect);
}
