using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WXPlayer.Core;

namespace WXPlayer.App;

// Opt-in, isolated UI QA. Uses the real existing selection/playback paths.
internal static class FilmWatching1715Smoke
{
    internal static async Task RunAsync(MainWindow w,LibraryStore store,PlaybackEngine engine,Dictionary<string,object> r)
    {
        void Check(string key,bool ok){r[key]=ok;if(!ok)throw new InvalidOperationException(key);}
        if(App.Arguments.Contains("--film1715-format-only"))
        {
            var display=typeof(MainWindow).Assembly.GetType("WXPlayer.App.LibraryDisplay");
            Check("filmDisplayFormatterPresent",display is not null);
            string Text(string method,string raw)=>(string)display!.GetMethod(method,BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!.Invoke(null,[raw])!;
            foreach(var row in new[]{("Title","2012 (2009)","2012"),("Year","2012 (2009)","2009"),("Title","2046","2046"),("Year","2046",""),("Title","HABABAM SINIFI","Hababam Sınıfı"),("Note","Evlilik Güzeldir [PAZARTESİ]","Pazartesi"),("Category","TR ✨ Dublaj Film","Dublaj Film"),("Group","TR ✨ Dublaj Film","TÜRKİYE"),("Group","XYZ ✨ Kategori","XYZ"),("Category","Belgesel","Belgesel"),("Group","Belgesel","")})
                Check("filmDisplay_"+row.Item1+"_"+row.Item2,Text(row.Item1,row.Item2)==row.Item3);
            return;
        }
        await using var art=new SmokeHttpServer(HomeLayoutSmoke.Poster(240,360,"WX","#27312D"));
        string media=new Uri(Path.GetFullPath(App.Arguments[Array.IndexOf(App.Arguments,"--media")+1])).AbsoluteUri;
        var source=new SourceConfig{Id="film1715",Name="Yerel QA",Address=Path.Combine(AppContext.BaseDirectory,"samples","open-films.m3u")};
        var movies=Enumerable.Range(0,310).Select(i=>new ContentItem{Id="film1715-"+i,SourceId=source.Id,Name=i==0?"HABABAM SINIFI (1975)":i==1?"Evlilik Güzeldir [PAZARTESİ]":"Film "+i.ToString("000"),Category=i<155?"TR ✨ Dublaj Film":"Original ✨ Movies",Kind=ContentKind.Movie,Url=media,Logo=i==0?art.Url:i==2?"http://127.0.0.1:1/missing-poster.png":""}).ToArray();
        var live=movies[0] with{Id="film1715-live",Name="Yerel kanal",Kind=ContentKind.Live,Category="Canlı kanallar"};
        var series=movies[0] with{Id="film1715-series",Name="Yerel dizi",Kind=ContentKind.Series};
        var episode=movies[0] with{Id="film1715-episode",Name="Bölüm 1",Kind=ContentKind.Episode,SeriesId=series.Id,SeriesName=series.Name,Season=1,Episode=1};
        async IAsyncEnumerable<ContentItem> Items(){foreach(var item in movies)yield return item;yield return live;yield return episode;await Task.Yield();}
        await store.ImportAsync(source,Items(),null,default);await w.SmokeRefreshAsync(source.Id);
        w.Width=1920;w.Height=1080;
        var footerTrace=new List<object>();r["footerTrace"]=footerTrace;
        void Footer(string phase)=>footerTrace.Add(new{phase,visibility=w.BottomBar.Visibility.ToString(),height=w.BottomBar.ActualHeight,film=Settings1711Smoke.Descendants<Border>(w).Any(b=>AutomationProperties.GetName(b)=="Film bilgisi"&&b.IsVisible)});
        foreach(var pair in new[]{("live",live),("series",episode)})
        {
            await w.SmokeBrowseAsync(pair.Item1);await w.SmokePlayAsync(pair.Item2);await Wait(()=>engine.Player.IsPlaying);
            if(pair.Item1=="series")((Button)w.SmokeCatalog.ResumeControl).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            w.UpdateLayout();await Task.Delay(180);
            Footer(pair.Item1+"Before");var signature=Signature(w);r[pair.Item1+"Signature"]=signature;
            File.WriteAllText(Path.Combine(App.DataDirectory,pair.Item1+"-signature.json"),JsonSerializer.Serialize(signature));
            Settings1711Smoke.Save(w,pair.Item1+"-unchanged.png");
        }
        if(App.Arguments.Contains("--film1715-baseline-only"))return;
        await w.SmokeBrowseAsync("movie");await w.SmokePlayAsync(movies[0]);await Wait(()=>engine.Player.IsPlaying);
        ((Button)w.SmokeCatalog.ResumeControl).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));w.UpdateLayout();await Task.Delay(200);
        var pane=Settings1711Smoke.Descendants<ListBox>(w).SingleOrDefault(e=>AutomationProperties.GetName(e)=="Film kategorileri");
        Check("filmCategoryColumnVisible",pane is {IsVisible:true}&&pane.ActualWidth>150);
        var info=Settings1711Smoke.Descendants<Border>(w).Single(e=>AutomationProperties.GetName(e)=="Film bilgisi");
        var overlay=w.OwnedWindows.Cast<Window>().Single(e=>e.Title=="WX Player live controls");
        IntPtr handle=engine.Player.Hwnd;r["actualDpi"]=GetDpiForWindow(new System.Windows.Interop.WindowInteropHelper(w).Handle);
        Check("filmNativeVideoAligned",Aligned(w.Video));
        Check("filmRealCategoryStrings",pane!.Items.Cast<string>().SequenceEqual(w.CategoryPicker.Items.Cast<string>()));
        Check("filmColumnsOrdered",pane.TranslatePoint(new Point(),w).X<w.ChannelList.TranslatePoint(new Point(),w).X&&w.ChannelList.TranslatePoint(new Point(),w).X<w.VideoBorder.TranslatePoint(new Point(),w).X);
        Check("filmNoSummaryRow",!Settings1711Smoke.Descendants<TextBlock>(info).Any(t=>t.Text.Contains("özet",StringComparison.OrdinalIgnoreCase)));
        Check("filmGuideHiddenOnlyHere",!w.GuidePanel.IsVisible&&info.IsVisible);
        Check("filmCurrentInfoTitle",Settings1711Smoke.Descendants<TextBlock>(info).Any(t=>t.Text=="Hababam Sınıfı")&&w.NowTitle.Text=="HABABAM SINIFI (1975)");
        Check("filmBackAboveTitle",w.CatalogBackButton.TranslatePoint(new Point(),overlay).Y<w.ControlsBorder.TranslatePoint(new Point(),overlay).Y);
        await Wait(()=>Settings1711Smoke.Descendants<ChannelLogo>(info).Any(l=>l.HasImage));Check("filmExistingPosterLoader",true);
        Settings1711Smoke.Save(w,"film-wide.png");Settings1711Smoke.Save(overlay,"film-wide-controls.png");
        if(App.Arguments.Contains("--film1715-preview")){await Task.Delay(180000);return;}
        var infoNext=Settings1711Smoke.Descendants<Button>(info).Single(b=>AutomationProperties.GetName(b)=="Sıradaki film");
        w.ChannelList.SelectedIndex=0;string first=((ContentItem)w.ChannelList.Items[0]).Name,second=((ContentItem)w.ChannelList.Items[1]).Name;await Wait(()=>engine.Player.IsPlaying&&w.NowTitle.Text==first);
        Check("filmInfoNextAvailable",infoNext.IsVisible);infoNext.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Wait(()=>engine.Player.IsPlaying&&w.NowTitle.Text==second);
        w.PreviousChannelButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Wait(()=>engine.Player.IsPlaying&&w.NowTitle.Text==first);Check("filmExistingNextPreviousActions",true);
        await w.SmokePlayAsync(movies[0]);await Wait(()=>engine.Player.IsPlaying&&w.NowTitle.Text==movies[0].Name);
        w.SearchBox.Text="HABABAM";await Wait(()=>w.ChannelList.Items.Count==1);w.UpdateLayout();await Task.Delay(100);
        Check("filmRenderedTitleAndRawData",Settings1711Smoke.Descendants<TextBlock>(w.ChannelList).Any(t=>t.Text=="Hababam Sınıfı")&&((ContentItem)w.ChannelList.Items[0]).Name=="HABABAM SINIFI (1975)");
        Check("filmPlayingMarker",Settings1711Smoke.Descendants<SvgIcon>(w.ChannelList).Any(i=>i.Icon=="statistics"&&i.IsVisible));
        Check("filmSinglePagePagerHidden",!((FrameworkElement)w.PageLabel.Parent).IsVisible);
        var favorite=Settings1711Smoke.Descendants<Button>(info).Single(b=>AutomationProperties.GetName(b)=="Favoriyi değiştir");
        favorite.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Wait(()=>Settings1711Smoke.Descendants<TextBlock>(info).Any(t=>t.Text=="Favorilerde"));
        Check("filmFavoriteUsesStore",(await store.FindAsync(movies[0].Id))!.IsFavorite);
        w.SearchBox.Text="Film 002";await Wait(()=>w.ChannelList.Items.Count==1&&((ContentItem)w.ChannelList.Items[0]).Name=="Film 002");w.UpdateLayout();await Task.Delay(150);
        Check("filmPosterlessFallback",Settings1711Smoke.Descendants<LibraryPosterPresentation>(w.ChannelList).Any(p=>Settings1711Smoke.Descendants<SvgIcon>(p).Any(i=>i.Icon=="catalog-film"&&i.IsVisible)));
        Check("filmFavoriteSurvivesFilterOut",Settings1711Smoke.Descendants<TextBlock>(info).Any(t=>t.Text=="Favorilerde"));
        favorite.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Wait(()=>Settings1711Smoke.Descendants<TextBlock>(info).Any(t=>t.Text=="Favorilere ekle"));Check("filmFilteredOutFavoriteStillToggles",!(await store.FindAsync(movies[0].Id))!.IsFavorite);
        Check("filmSearchDoesNotReplaceInfo",Settings1711Smoke.Descendants<TextBlock>(info).Any(t=>t.Text=="Hababam Sınıfı"));
        w.SearchBox.Text="__no_movie_matches__";await Wait(()=>w.ChannelList.Items.Count==0);Check("filmEmptySearchKeepsCurrent",info.IsVisible&&w.NowTitle.Text==movies[0].Name);
        w.SearchBox.Clear();await Wait(()=>w.ChannelList.Items.Count==150);
        w.NextPage.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Wait(()=>w.PageLabel.Text.StartsWith("151"));
        Check("filmOriginalPaging",w.PageLabel.Text.Contains("310")&&w.ChannelList.Items.Count==150);
        w.PrevPage.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Wait(()=>w.PageLabel.Text.StartsWith("1–"));
        int categoryEvents=0;SelectionChangedEventHandler categoryChanged=(_,_)=>categoryEvents++;w.CategoryPicker.SelectionChanged+=categoryChanged;
        pane.SelectedItem="TR ✨ Dublaj Film";await Wait(()=>w.ChannelList.Items.Count==150&&w.PageLabel.Text.Contains("155")&&w.ChannelList.Items.Cast<ContentItem>().All(i=>i.Category=="TR ✨ Dublaj Film"));
        w.CategoryPicker.SelectionChanged-=categoryChanged;Check("filmCategorySingleSelectionEvent",categoryEvents==1);Check("filmCategoryUsesOriginalSelector",Equals(w.CategoryPicker.SelectedItem,"TR ✨ Dublaj Film")&&w.PageLabel.Text.Contains("155"));
        Check("filmHeaderKeepsLibraryTotal",Settings1711Smoke.Descendants<TextBlock>((DependencyObject)w.PageTitle.Parent).Any(t=>t.Text=="310 içerik"));
        w.Activate();pane.Focus();double volume=w.VolumeSlider.Value;long time=engine.Player.Time;int version=w.SmokePlaybackVersion;
        PressKey(w,Key.Right);await Task.Delay(120);
        Check("filmPaneRightOnlyChangesFocus",w.ChannelList.IsKeyboardFocusWithin&&w.VolumeSlider.Value==volume&&w.SmokePlaybackVersion==version&&engine.Player.Time>=time-200&&engine.Player.Time<time+2500);
        PressKey(w,Key.Left);Check("filmPaneLeftOnlyChangesFocus",pane.IsKeyboardFocusWithin&&w.VolumeSlider.Value==volume);
        pane.SelectedIndex=0;await Wait(()=>w.ChannelList.Items.Count==150&&w.PageLabel.Text.Contains("310"));
        w.ChannelList.SelectedIndex=10;await Wait(()=>engine.Player.IsPlaying&&w.NowTitle.Text==((ContentItem)w.ChannelList.Items[10]).Name);
        w.ChannelList.Focus();PressKey(w,Key.Down);await Wait(()=>w.ChannelList.SelectedIndex==11&&engine.Player.IsPlaying&&w.NowTitle.Text==((ContentItem)w.ChannelList.Items[11]).Name);
        Check("filmListDownDoesNotChangeVolume",w.VolumeSlider.Value==volume);
        PressKey(w,Key.Up);await Wait(()=>w.ChannelList.SelectedIndex==10&&w.NowTitle.Text==((ContentItem)w.ChannelList.Items[10]).Name);
        Check("filmListUpDoesNotChangeVolume",w.VolumeSlider.Value==volume);
        PressKey(w,Key.Right);await Task.Delay(80);Check("filmRightEntersExistingPlayerControls",ReferenceEquals(Keyboard.FocusedElement,w.PreviousChannelButton));
        await Wait(()=>engine.Player.IsPlaying&&engine.Player.Length>30000);time=engine.Player.Time;
        PressKey(overlay,Key.Right);await Wait(()=>engine.Player.Time>=time+8000);Check("filmPlayerRightStillSeeks",engine.Player.Hwnd==handle);
        time=engine.Player.Time;PressKey(overlay,Key.Left);await Wait(()=>engine.Player.Time<=time-7000);Check("filmPlayerLeftStillSeeks",w.VolumeSlider.Value==volume);
        PressKey(overlay,Key.Up);Check("filmPlayerUpStillAdjustsVolume",w.VolumeSlider.Value==volume+5);PressKey(overlay,Key.Down);Check("filmPlayerDownStillAdjustsVolume",w.VolumeSlider.Value==volume);
        bool mute=engine.Player.Mute;PressKey(overlay,Key.M);await Wait(()=>engine.Player.Mute!=mute);Check("filmPlayerMStillMutes",engine.Player.Mute!=mute);PressKey(overlay,Key.M);await Wait(()=>engine.Player.Mute==mute);
        // A focused button keeps its native Space activation. Exercise the global shortcut on the video surface.
        w.Activate();w.Video.Focus();await Task.Delay(50);
        Check("filmVideoFocusOutsidePanes",!pane.IsKeyboardFocusWithin&&!w.ChannelList.IsKeyboardFocusWithin&&Keyboard.FocusedElement is not Button);
        PressKey(w,Key.Space);await Wait(()=>engine.Player.State==LibVLCSharp.Shared.VLCState.Paused);Check("filmPlayerSpacePauses",!engine.Player.IsPlaying);
        w.Activate();w.ChannelList.Focus();PressKey(w,Key.Enter);await Wait(()=>engine.Player.IsPlaying);Check("filmListEnterUsesExistingResume",true);
        int oldIndex=w.ChannelList.SelectedIndex;PressKey(overlay,Key.PageDown);await Wait(()=>w.ChannelList.SelectedIndex==oldIndex+1&&engine.Player.IsPlaying);Check("filmPlayerPageDownStillSelectsNext",w.ChannelList.SelectedIndex==oldIndex+1);
        PressKey(overlay,Key.PageUp);await Wait(()=>w.ChannelList.SelectedIndex==oldIndex&&engine.Player.IsPlaying);
        var layers=((Grid)overlay.Content).Children.OfType<Border>().Where(b=>b.IsAncestorOf(w.NowTitle)||b.IsAncestorOf(w.ControlsBorder)).ToArray();
        void MoveOffControls(){Keyboard.ClearFocus();foreach(var layer in layers)layer.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice,Environment.TickCount){RoutedEvent=Mouse.MouseLeaveEvent});overlay.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice,Environment.TickCount){RoutedEvent=Mouse.PreviewMouseMoveEvent});}
        overlay.Activate();await Task.Delay(180);MoveOffControls();var hiddenAt=System.Diagnostics.Stopwatch.StartNew();await Wait(()=>layers.All(b=>!b.IsVisible));r["filmIdleHiddenMilliseconds"]=hiddenAt.ElapsedMilliseconds;
        var controller=typeof(MainWindow).GetField("_liveAutoHide",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(w);
        r["autoHideDiagnostic"]=controller!.GetType().GetFields(BindingFlags.Instance|BindingFlags.NonPublic).Where(f=>f.FieldType==typeof(bool)).ToDictionary(f=>f.Name,f=>f.GetValue(controller));
        var timer=(System.Windows.Threading.DispatcherTimer)controller.GetType().GetField("_timer",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(controller)!;
        r["autoHideTimer"]=new{timer.IsEnabled,seconds=timer.Interval.TotalSeconds};r["autoHideGuards"]=new{menu=gearOpen(),seek=w.SeekSlider.IsInteracting,volume=w.VolumeSlider.IsInteracting,capture=Mouse.Captured?.GetType().Name??"none",hovered=((System.Collections.IEnumerable)controller.GetType().GetField("_hovered",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(controller)!).Cast<object>().Count()};
        bool gearOpen()=>Settings1711Smoke.Descendants<System.Windows.Controls.Primitives.ToggleButton>(overlay).Any(b=>b.IsChecked==true);
        r["autoHideState"]=engine.Player.State.ToString();r["autoHideFocus"]=Keyboard.FocusedElement?.GetType().Name??"none";r["autoHideLayers"]=layers.Select(b=>new{b.IsVisible,b.IsMouseOver}).ToArray();
        Check("filmExistingThreeSecondAutoHide",layers.All(b=>!b.IsVisible));
        long beforeMove=engine.Player.Time;overlay.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice,Environment.TickCount){RoutedEvent=Mouse.PreviewMouseMoveEvent});
        Check("filmFirstMoveOnlyReveals",layers.All(b=>b.IsVisible)&&engine.Player.IsPlaying&&engine.Player.Time>=beforeMove&&engine.Player.Hwnd==handle);
        var gear=Settings1711Smoke.Descendants<System.Windows.Controls.Primitives.ToggleButton>(overlay).Single(b=>AutomationProperties.GetName(b)=="Oynatıcı seçenekleri");gear.IsChecked=true;await Task.Delay(3400);Check("filmOpenMenuPreventsHiding",layers.All(b=>b.IsVisible));
        Check("filmHeaderActionsRetainedInMenu",w.LibrarySummaryButton.IsVisible&&w.SourceOptionsButton.IsVisible);gear.IsChecked=false;
        MoveOffControls();w.SmokeTogglePlayback();await Wait(()=>engine.Player.State==LibVLCSharp.Shared.VLCState.Paused);await Task.Delay(3400);Check("filmPausePreventsHiding",layers.All(b=>b.IsVisible));w.SmokeTogglePlayback();await Wait(()=>engine.Player.IsPlaying);
        overlay.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice,Environment.TickCount){RoutedEvent=Mouse.PreviewMouseMoveEvent});
        string badge=w.PlaybackBadge.Text;w.PlaybackBadge.Text="BAĞLANIYOR";w.UpdateLayout();
        r["filmConnectingDiagnostic"]=Settings1711Smoke.Descendants<TextBlock>(overlay).Where(t=>t.Text.Contains("Bağlan")||t.Text.Contains("BAĞLAN")).Select(t=>new{t.Text,t.IsVisible}).ToArray();
        Check("filmConnectingPresentation",Settings1711Smoke.Descendants<TextBlock>(overlay).Count(t=>t.Text=="Bağlanıyor…"&&t.IsVisible)>=2);Settings1711Smoke.Save(overlay,"film-connecting.png");w.PlaybackBadge.Text=badge;
        PressKey(overlay,Key.F);await Wait(()=>w.SmokeControlsVisible);Check("filmFStillFullscreen",!info.IsVisible&&Aligned(w.Video)&&engine.Player.Hwnd==handle);
        var fullscreen=w.OwnedWindows.Cast<Window>().Single(e=>e.Title=="WX Player controls");PressKey(fullscreen,Key.Escape);await Wait(()=>Settings1711Smoke.Descendants<Border>(w).Any(b=>AutomationProperties.GetName(b)=="Film bilgisi"&&b.IsVisible));
        Footer("afterFullscreen");Check("filmCategorySurvivesFullscreen",Equals(w.CategoryPicker.SelectedItem,"Tüm kategoriler"));Check("filmFullscreenRestoresSameSession",engine.Player.Hwnd==handle&&engine.Player.IsPlaying&&Aligned(w.Video));
        foreach(var size in new[]{(1366d,768d),(900d,700d)})
        {
            w.Width=size.Item1;w.Height=size.Item2;w.UpdateLayout();await Task.Delay(150);
            Check("filmCategoryHeading_"+size.Item1,Settings1711Smoke.Descendants<TextBlock>(w.FilterBar).Any(t=>t.Text=="Tüm kategoriler"&&t.IsVisible));
            Settings1711Smoke.Save(w,$"film-{size.Item1:0}.png");
            Check("filmFits_"+size.Item1,w.VideoBorder.ActualWidth>200&&w.VideoBorder.ActualHeight>100&&Aligned(w.Video));
        }
        var toggle=Settings1711Smoke.Descendants<Button>(w).Single(b=>AutomationProperties.GetName(b)=="Kategorileri aç");
        Check("filmNarrowDrawerButton",toggle.IsVisible);toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));w.UpdateLayout();await Task.Delay(80);
        pane=Settings1711Smoke.Descendants<ListBox>(w).Single(e=>AutomationProperties.GetName(e)=="Film kategorileri");
        Check("filmNarrowDrawerStaysOutsideVideo",pane.IsVisible&&pane.TranslatePoint(new Point(pane.ActualWidth,0),w).X<=w.VideoBorder.TranslatePoint(new Point(),w).X+1);
        Settings1711Smoke.Save(w,"film-narrow-drawer.png");
        var currentOverlay=w.OwnedWindows.Cast<Window>().Single(e=>e.Title=="WX Player live controls");currentOverlay.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=Mouse.PreviewMouseDownEvent});
        Check("filmOverlayOutsideClickClosesDrawer",!pane.IsVisible);toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));pane.Focus();PressKey(w,Key.Escape);
        w.Width=1920;w.Height=1080;PressKey(w,Key.F8);w.UpdateLayout();await Task.Delay(150);Check("filmComfortKeepsVideo",Aligned(w.Video)&&engine.Player.Hwnd==handle);Settings1711Smoke.Save(w,"film-comfort.png");PressKey(w,Key.F8);
        w.SmokeToggleMiniPlayer();await Wait(()=>w.SmokeMiniPlayerVisible);w.UpdateLayout();await Task.Delay(120);
        Check("filmMiniRetainsMainPlaceholderLayout",w.MiniPlaceholder.IsVisible&&Settings1711Smoke.Descendants<Border>(w).Any(b=>AutomationProperties.GetName(b)=="Film bilgisi"&&b.IsVisible)&&typeof(MainWindow).GetField("_liveAutoHide",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(w) is null);
        w.SmokeToggleMiniPlayer();await Wait(()=>!w.SmokeMiniPlayerVisible&&engine.Player.Hwnd==handle);w.UpdateLayout();
        Check("filmMiniReturnKeepsLayoutAndVideo",Aligned(w.Video)&&Settings1711Smoke.Descendants<Border>(w).Any(b=>AutomationProperties.GetName(b)=="Film bilgisi"&&b.IsVisible));
        w.CatalogBackButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Wait(()=>w.CatalogHost.IsVisible);Footer("afterCatalogueBack");Check("filmBackUsesOriginalCatalogue",w.CatalogHost.IsVisible&&engine.Player.Hwnd==handle);
        foreach(var pair in new[]{("live",live),("series",episode)})
        {
            await w.SmokeBrowseAsync(pair.Item1);await w.SmokePlayAsync(pair.Item2);await Wait(()=>engine.Player.IsPlaying);
            if(pair.Item1=="series")((Button)w.SmokeCatalog.ResumeControl).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            w.UpdateLayout();await Task.Delay(150);var signature=Signature(w);
            Footer(pair.Item1+"After");File.WriteAllText(Path.Combine(App.DataDirectory,pair.Item1+"-after-signature.json"),JsonSerializer.Serialize(signature));Settings1711Smoke.Save(w,pair.Item1+"-after.png");
            Check("filmRestores_"+pair.Item1,JsonSerializer.Serialize(signature)==JsonSerializer.Serialize(r[pair.Item1+"Signature"]));
        }
    }
    private static void PressKey(Window window,Key key)=>window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(window),Environment.TickCount,key){RoutedEvent=Keyboard.PreviewKeyDownEvent});
    private static bool Aligned(NativeVideoHost video){var a=video.PointToScreen(new Point());var b=video.PointToScreen(new Point(video.ActualWidth,video.ActualHeight));GetWindowRect(video.RenderingHandle,out var rect);return Math.Abs(a.X-rect.Left)<3&&Math.Abs(a.Y-rect.Top)<3&&Math.Abs(b.X-rect.Right)<3&&Math.Abs(b.Y-rect.Bottom)<3;}
    [StructLayout(LayoutKind.Sequential)]private struct NativeRect{public int Left,Top,Right,Bottom;}
    [DllImport("user32.dll")]private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")]private static extern bool GetWindowRect(IntPtr hwnd,out NativeRect rect);
    private static object Signature(MainWindow w)
    {
        object Rect(FrameworkElement e){var p=e.TranslatePoint(new Point(),w);return new{X=Math.Round(p.X,2),Y=Math.Round(p.Y,2),Width=Math.Round(e.ActualWidth,2),Height=Math.Round(e.ActualHeight,2),e.IsVisible};}
        return new{library=Rect(w.LibraryPanel),video=Rect(w.VideoBorder),guide=Rect(w.GuidePanel),list=Rect(w.ChannelList),font=w.FontFamily.Source,categoryStyle=w.CategoryPicker.Style?.TargetType.FullName};
    }
    internal static async Task Wait(Func<bool> condition){var end=DateTime.UtcNow.AddSeconds(15);while(!condition()&&DateTime.UtcNow<end)await Task.Delay(40);if(!condition())throw new TimeoutException("Film 1.7.15 UI wait");}
}
