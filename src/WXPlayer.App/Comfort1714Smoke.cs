using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WXPlayer.Core;

namespace WXPlayer.App;

internal static class Comfort1714Smoke
{
    private static bool Enabled(PlayerSettings settings) => typeof(PlayerSettings).GetProperty("ComfortMode")?.GetValue(settings) is true;
    private static void F8(Window window) => window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(window),0,Key.F8){RoutedEvent=Keyboard.PreviewKeyDownEvent});
    internal static async Task RunAsync(MainWindow window,LibraryStore store,PlaybackEngine engine,PlayerSettings settings,Dictionary<string,object> results)
    {
        void Check(string key,bool ok){results[key]=ok;if(!ok)throw new InvalidOperationException(key);}
        if(App.Arguments.Contains("--comfort1714-restore-only"))
        {
            bool expected=!App.Arguments.Contains("--expect-comfort-off");
            await Task.Delay(150);
            Check("comfortRestartRestoresSetting",Enabled(settings)==expected);
            Check("comfortRestartAppliesScale",expected?Scale(window.Sidebar)>1:Math.Abs(Scale(window.Sidebar)-Math.Clamp(window.ActualWidth*.005+8,14,20)/16)<.01);
            return;
        }
        bool baseline=App.Arguments.Contains("--comfort1714-baseline-only");
        if(!baseline)
        {
            var preferences=window.CreateSettingsWindow();preferences.Show();await Task.Delay(100);
            try
            {
                var toggle=Settings1711Smoke.Descendants<CheckBox>(preferences).SingleOrDefault(c=>AutomationProperties.GetName(c)=="Rahat mod");
                Check("comfortSettingAvailable",toggle is not null);
                double before=Scale(window.Sidebar);toggle!.IsChecked=true;await Task.Delay(80);
                Check("comfortSettingWaitsForSave",!Enabled(settings)&&Math.Abs(Scale(window.Sidebar)-before)<.01);
                preferences.Width+=16;await Task.Delay(100);Check("comfortUnsavedToggleSurvivesResize",toggle.IsChecked==true&&!Enabled(settings));
                Settings1711Smoke.Descendants<TextBox>(preferences).Single(t=>AutomationProperties.GetName(t)=="Kayıtlar").Text=Path.Combine(App.DataDirectory,"recordings");
                Settings1711Smoke.Descendants<Button>(preferences).Single(b=>Equals(b.Content,"Değişiklikleri kaydet")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(150);
                Check("comfortSaveAppliesAndPersists",preferences.Saved&&Enabled(settings)&&Enabled(App.ReadSettings())&&Scale(window.Sidebar)>before);
                F8(window);await Task.Delay(100);Check("comfortF8TurnsOffAndPersists",!Enabled(settings)&&!Enabled(App.ReadSettings()));
                var sync=window.CreateSettingsWindow();sync.Show();await Task.Delay(100);
                var syncedToggle=Settings1711Smoke.Descendants<CheckBox>(sync).Single(c=>AutomationProperties.GetName(c)=="Rahat mod");
                F8(sync);await Task.Delay(100);Check("comfortShortcutSynchronizesOpenSettings",Enabled(settings)&&syncedToggle.IsChecked==true);
                F8(sync);sync.Close();
            }
            finally{if(preferences.IsVisible)preferences.Close();}
        }
        await using var art=new SmokeHttpServer(HomeLayoutSmoke.Poster(240,360,"WX","#27312D"));
        var source=new SourceConfig{Id="comfort1714",Name="Yerel QA kütüphanesi",Address=Path.Combine(AppContext.BaseDirectory,"samples","open-films.m3u")};
        string media=new Uri(Path.GetFullPath(App.Arguments[Array.IndexOf(App.Arguments,"--media")+1])).AbsoluteUri;
        var movie=new ContentItem{Id="comfort1714-movie",SourceId=source.Id,Name="Sintel",Kind=ContentKind.Movie,Url=media,Logo=art.Url,Category="Filmler"};
        var live=movie with{Id="comfort1714-live",Name="Yerel canlı yayın",Kind=ContentKind.Live,Category="Kanallar"};
        var series=movie with{Id="comfort1714-series",Name="Yerel dizi",Kind=ContentKind.Series,Category="Diziler"};
        var episode=movie with{Id="comfort1714-episode",Name="Yerel dizi · Bölüm 1",Kind=ContentKind.Episode,SeriesId=series.Id,SeriesName=series.Name,Season=1,Episode=1};
        async IAsyncEnumerable<ContentItem> Items(){yield return movie;yield return live;yield return series;yield return episode;await Task.Yield();}
        await store.ImportAsync(source,Items(),null,default);await window.SmokeRefreshAsync(source.Id);
        int captures=0;
        foreach(var size in new[]{(1920d,1080d),(1366d,768d)})
        {
            window.Width=size.Item1;window.Height=size.Item2;window.UpdateLayout();await Task.Delay(100);
            foreach(bool enabled in baseline?new[]{false}:new[]{false,true})
            {
                if(Enabled(settings)!=enabled)F8(window);await Task.Delay(120);
                string prefix=$"{size.Item1:0}x{size.Item2:0}-{(enabled?"on":"off")}";
                Check(prefix+"_settingMatches",Enabled(settings)==enabled);
                results[prefix+"_mainScale"]=new{comfort=ComfortPresentation.Factor,root=window.ComfortRoot.LayoutTransform.ToString(),sidebar=Scale(window.Sidebar)};
                foreach(string page in new[]{"home","live","movie","series"})
                {
                    await window.SmokeBrowseAsync(page);
                    if(page=="live"){await window.SmokePlayAsync(live);await Wait(()=>engine.Player.IsPlaying);}
                    window.UpdateLayout();await Task.Delay(160);
                    var view=page=="home"?(FrameworkElement)window.SmokeHome:page is "movie" or "series"?window.SmokeCatalog:window.ViewingPanel;
                    Check(prefix+"_"+page+"_visible",view.IsVisible&&view.ActualWidth>0&&view.ActualHeight>0);
                    var bounds=window.Sidebar.TransformToAncestor(window).TransformBounds(new Rect(window.Sidebar.RenderSize));
                    Check(prefix+"_"+page+"_sidebarFits",bounds.Right<=window.ActualWidth+2&&bounds.Bottom<=window.ActualHeight+2);
                    Settings1711Smoke.Save(window,prefix+"-"+page+".png");captures++;
                    if(page=="live")
                    {
                        var overlay=window.OwnedWindows.Cast<Window>().Single(w=>w.Title=="WX Player live controls");
                        Settings1711Smoke.Save(overlay,prefix+"-live-overlay.png");
                        Check(prefix+"_liveNativeBounds",Aligned(window.Video));
                        if(!baseline)
                        {
                            IntPtr liveHandle=engine.Player.Hwnd;F8(window);await Task.Delay(100);
                            Check(prefix+"_liveToggleKeepsSession",engine.Player.Hwnd==liveHandle&&engine.Player.IsPlaying&&Aligned(window.Video));F8(window);await Task.Delay(100);
                        }
                    }
                }
                await window.SmokeBrowseAsync("all");await window.SmokePlayAsync(movie);await Wait(()=>engine.Player.IsPlaying);
                IntPtr render=engine.Player.Hwnd;
                if(!baseline)
                {
                    F8(window);await Task.Delay(120);Check(prefix+"_toggleKeepsVideo",engine.Player.Hwnd==render&&engine.Player.IsPlaying&&Aligned(window.Video));
                    F8(window);await Task.Delay(120);
                }
                window.SmokeFullscreen();await Task.Delay(160);
                var chrome=window.OwnedWindows.Cast<Window>().Single(w=>w.Title=="WX Player controls");
                Settings1711Smoke.Save(chrome,prefix+"-fullscreen.png");captures++;
                Check(prefix+"_fullscreenNativeBounds",Aligned(window.Video));
                results[prefix+"_nativeDesktopCapture"]=WindowCapture.SaveVisibleFullscreen(window,Path.Combine(App.DataDirectory,prefix+"-fullscreen-native.png"),chrome);
                window.SmokeFullscreen();await Task.Delay(100);
                foreach(var pair in new (string Name,PremiumWindow Window)[]{("settings",window.CreateSettingsWindow()),("source",new SourceWindow(window)),("statistics",new StatisticsWindow(window,()=> (movie.Name,new PlaybackTarget(media)),engine,settings)),("tracks",new TracksWindow(window,engine))})
                {
                    var dialog=pair.Window;dialog.Show();await Task.Delay(120);dialog.UpdateLayout();
                    results[prefix+"_"+pair.Name+"_geometry"]=new{dialog.Width,dialog.Height,root=((FrameworkElement)dialog.Content).LayoutTransform.ToString(),surfaceScale=Scale((FrameworkElement)dialog.Content)};
                    if(!enabled)Check(prefix+"_"+pair.Name+"_normalSurface",((FrameworkElement)dialog.Content).LayoutTransform.Value.IsIdentity);
                    if(enabled)Check(prefix+"_"+pair.Name+"_withinWorkArea",WithinWorkArea(dialog));
                    Check(prefix+"_"+pair.Name+"_footerVisible",dialog.Footer.IsVisible&&dialog.Footer.ActualHeight>0);
                    Settings1711Smoke.Save(dialog,prefix+"-"+pair.Name+".png");captures++;
                    if(dialog is TracksWindow tracks)
                    {
                        tracks.AudioPicker.IsDropDownOpen=true;await Task.Delay(100);
                        var popup=(Popup)tracks.AudioPicker.Template.FindName("PART_Popup",tracks.AudioPicker);
                        results[prefix+"_trackPopupScaleDiagnostic"]=new{popupScale=Scale((FrameworkElement)popup.Child),pickerScale=Scale(tracks.AudioPicker),childLoaded=((FrameworkElement)popup.Child).IsLoaded,layout=((FrameworkElement)popup.Child).LayoutTransform.ToString(),logicalParent=LogicalTreeHelper.GetParent(popup.Child)?.GetType().Name,templateParent=((FrameworkElement)popup.Child).TemplatedParent?.GetType().Name,placement=popup.PlacementTarget?.GetType().Name,popupTemplate=popup.TemplatedParent?.GetType().Name};
                        if(enabled)Check(prefix+"_trackPopupMatchesScale",Math.Abs(Scale((FrameworkElement)popup.Child)-Scale(tracks.AudioPicker))<.03);
                        Settings1711Smoke.Save((FrameworkElement)popup.Child,prefix+"-tracks-popup.png");tracks.AudioPicker.IsDropDownOpen=false;
                    }
                    if(enabled && pair.Name=="settings")
                    {
                        dialog.Width=dialog.MaxWidth;dialog.Height=dialog.MaxHeight;await Task.Delay(100);
                        double smallerWidth=dialog.Width-24,smallerHeight=dialog.Height-24;
                        dialog.Width=smallerWidth;dialog.Height=smallerHeight;await Task.Delay(120);
                        Check(prefix+"_cappedDialogCanShrink",Math.Abs(dialog.Width-smallerWidth)<2&&Math.Abs(dialog.Height-smallerHeight)<2);
                    }
                    dialog.Close();
                }
            }
        }
        if(!baseline)
        {
            if(!Enabled(settings))F8(window);
            await window.SmokeBrowseAsync("all");await window.SmokePlayAsync(episode);await Wait(()=>engine.Player.IsPlaying);
            IntPtr episodeHandle=engine.Player.Hwnd;F8(window);await Task.Delay(100);
            Check("comfortEpisodeToggleKeepsSession",engine.Player.Hwnd==episodeHandle&&engine.Player.IsPlaying&&Aligned(window.Video));F8(window);await Task.Delay(100);
            var picker=window.SourcePicker;picker.IsDropDownOpen=true;await Task.Delay(100);
            var sourcePopup=(Popup)picker.Template.FindName("PART_Popup",picker);
            Check("comfortSourcePopupMatchesScale",Math.Abs(Scale((FrameworkElement)sourcePopup.Child)-Scale(picker))<.03);
            F8(window);await Task.Delay(120);F8(window);await Task.Delay(120);
            Check("comfortOpenPopupTracksToggle",sourcePopup.IsOpen&&Math.Abs(Scale((FrameworkElement)sourcePopup.Child)-Scale(picker))<.03);picker.IsDropDownOpen=false;
            var tip=new ToolTip{Content="Kaynak seçin",PlacementTarget=picker,IsOpen=true};await Task.Delay(100);
            Check("comfortToolTipMatchesScale",Math.Abs(Scale(tip)-Scale(picker))<.03);tip.IsOpen=false;
            var menu=new ContextMenu{PlacementTarget=picker};menu.Items.Add(new MenuItem{Header="Kütüphane"});menu.IsOpen=true;await Task.Delay(100);
            Check("comfortContextMenuMatchesScale",Math.Abs(Scale(menu)-Scale(picker))<.03);menu.IsOpen=false;
            IntPtr native=window.Video.RenderingHandle;
            PostMessage(native,0x0100,new IntPtr(0x77),new IntPtr(1));await Task.Delay(120);
            Check("comfortNativeF8Toggles",!Enabled(settings));
            PostMessage(native,0x0100,new IntPtr(0x77),new IntPtr(0x40000001));await Task.Delay(100);
            Check("comfortNativeF8RepeatDoesNotToggle",!Enabled(settings));
            PostMessage(native,0x0101,new IntPtr(0x77),new IntPtr(0xC0000001L));
            PostMessage(native,0x0100,new IntPtr(0x77),new IntPtr(1));await Task.Delay(120);
            Check("comfortNativeF8NextPressToggles",Enabled(settings));PostMessage(native,0x0101,new IntPtr(0x77),new IntPtr(0xC0000001L));
            window.SmokeToggleMiniPlayer();await Wait(()=>window.SmokeMiniPlayerVisible);
            var mini=window.SmokeMiniWindow!;
            Check("comfortMiniRemainsUnscaled",Math.Abs(Scale((FrameworkElement)mini.Content)-1)<.001&&mini.Width==520&&mini.Height==400);
            window.SmokeToggleMiniPlayer();
            Check("comfortDefaultResource",window.FindResource("ComfortModeScale") is double factor&&factor==1.5);
        }
        results["comfortScreenshotCount"]=captures;
        results["comfortResolutionScope"]="WPF window viewports at 100% DPI; native desktop capture flags record availability separately";
    }
    private static double Scale(FrameworkElement element)
    {
        Visual root=element;while(VisualTreeHelper.GetParent(root) is Visual parent)root=parent;
        var transform=element.TransformToAncestor(root);var a=transform.Transform(new Point());var b=transform.Transform(new Point(1,0));return (b-a).Length;
    }
    private static bool Aligned(NativeVideoHost video)
    {
        var a=video.PointToScreen(new Point());var b=video.PointToScreen(new Point(video.ActualWidth,video.ActualHeight));GetWindowRect(video.RenderingHandle,out var actual);
        return Math.Abs(a.X-actual.Left)<3&&Math.Abs(a.Y-actual.Top)<3&&Math.Abs(b.X-actual.Right)<3&&Math.Abs(b.Y-actual.Bottom)<3;
    }
    private static bool WithinWorkArea(Window window)
    {
        var hwnd=new WindowInteropHelper(window).Handle;var info=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};GetMonitorInfo(MonitorFromWindow(hwnd,2),ref info);GetWindowRect(hwnd,out var rect);
        return rect.Right-rect.Left<=(info.Work.Right-info.Work.Left)*.9+3&&rect.Bottom-rect.Top<=(info.Work.Bottom-info.Work.Top)*.9+3;
    }
    private static async Task Wait(Func<bool> test){var until=DateTime.UtcNow.AddSeconds(15);while(!test()&&DateTime.UtcNow<until)await Task.Delay(40);if(!test())throw new TimeoutException("Comfort QA wait");}
    [StructLayout(LayoutKind.Sequential)]private struct RectNative{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)]private struct MonitorInfo{public int Size;public RectNative Monitor,Work;public uint Flags;}
    [DllImport("user32.dll")]private static extern bool GetWindowRect(IntPtr hwnd,out RectNative rect);
    [DllImport("user32.dll")]private static extern IntPtr MonitorFromWindow(IntPtr hwnd,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern bool GetMonitorInfo(IntPtr hwnd,ref MonitorInfo info);
    [DllImport("user32.dll")]private static extern bool PostMessage(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam);
}
