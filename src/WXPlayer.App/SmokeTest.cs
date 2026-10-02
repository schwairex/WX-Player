using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using WXPlayer.Core;

namespace WXPlayer.App;

internal static class SmokeTest
{
    public static async Task RunAsync(MainWindow window,LibraryStore store,PlaybackEngine engine,ProviderClient providers,PlayerSettings settings)
    {
        var results=new Dictionary<string,object>();
        try
        {
            if(App.Arguments.Contains("--catalog-layout-only"))
            {
                await Catalog177Smoke.RunAsync(window,store,results);results["success"]=true;
                File.WriteAllText(Path.Combine(App.DataDirectory,"smoke-results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));window.Close();return;
            }
            if(App.Arguments.Contains("--discord-presence"))
            {
                await DiscordPresenceSmoke.RunAsync(window,store,engine,results);results["success"]=true;
                File.WriteAllText(Path.Combine(App.DataDirectory,"smoke-results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));window.Close();return;
            }
            if(App.Arguments.Contains("--experience-160"))
            {
                await Experience160Smoke.RunAsync(window,store,engine,results);results["success"]=true;
                File.WriteAllText(Path.Combine(App.DataDirectory,"smoke-results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));window.Close();return;
            }
            if(App.Arguments.Contains("--recording-172"))
            {
                int recordMediaArg=Array.IndexOf(App.Arguments,"--media");if(recordMediaArg<0)throw new InvalidOperationException("A local test video is required.");
                var target=new PlaybackTarget(new Uri(Path.GetFullPath(App.Arguments[recordMediaArg+1])).AbsoluteUri);
                await engine.PlayAsync(target,settings,default);await WaitUntil(()=>engine.Player.IsPlaying&&engine.Player.Length>30000,TimeSpan.FromSeconds(12));
                results["originalLengthMs"]=engine.Player.Length;engine.Player.Time=20000;
                await WaitUntil(()=>engine.Player.Time>=19000,TimeSpan.FromSeconds(8));
                long start=engine.Player.Time;settings.RecordingFolder=Path.Combine(App.DataDirectory,"recordings");
                string path=await engine.StartRecordingAsync(target,"Positioned recording",settings,RecordingStartPolicy.StartAt(ContentKind.Episode,true,start),true);
                await Task.Delay(2500);results["viewedIntervalMs"]=engine.Player.Time-start;
                await engine.StopRecordingAsync(engine.Player.Time);results["recordBytes"]=new FileInfo(path).Length;
                await engine.PlayAsync(new PlaybackTarget(new Uri(path).AbsoluteUri),settings,default);
                await WaitUntil(()=>engine.Player.IsPlaying,TimeSpan.FromSeconds(10));
                await Task.Delay(1000);results["recordedLengthMs"]=engine.Player.Length;
                results["recordedOnlySelectedInterval"]=Convert.ToInt64(results["recordBytes"])>0&&engine.Player.Length>0&&engine.Player.Length<Convert.ToInt64(results["originalLengthMs"])/3&&engine.Player.Length<=Convert.ToInt64(results["viewedIntervalMs"])+5000;
                if(!Equals(results["recordedOnlySelectedInterval"],true))throw new Exception("Recording includes media outside the selected interval.");
                results["success"]=true;
                File.WriteAllText(Path.Combine(App.DataDirectory,"smoke-results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));window.Close();return;
            }
            await HomeLayoutSmoke.RunAsync(window,results);
            if(App.Arguments.Contains("--home-layout-only"))
            {
                results["success"]=true;
                File.WriteAllText(Path.Combine(App.DataDirectory,"smoke-results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
                window.Close();return;
            }
            var selectionTest=new EpisodeWindow(window,new ContentItem{Name="Seçim testi",Kind=ContentKind.Series},Enumerable.Range(1,3).Select(n=>new ContentItem{Id="selection-"+n,Name="Bölüm "+n,Kind=ContentKind.Episode,Season=1,Episode=n}).ToArray());
            selectionTest.Show();await Task.Delay(150);selectionTest.Episodes.SelectedIndex=1;int selectedIndex=selectionTest.Episodes.SelectedIndex;selectionTest.Close();if(selectedIndex!=1)throw new Exception("Episode selection changed unexpectedly to "+selectedIndex);
            if(App.Arguments.Contains("--stress"))
            {
                var stressSource=new SourceConfig{Id="smoke-stress",Name="Performans testi",Address="https://example.test/list.m3u"};
                var sw=Stopwatch.StartNew();double last=0,maxGap=0;int heartbeats=0;
                var timer=new DispatcherTimer(DispatcherPriority.Normal){Interval=TimeSpan.FromMilliseconds(16)};
                timer.Tick+=(_,_)=>{double now=sw.Elapsed.TotalMilliseconds;maxGap=Math.Max(maxGap,now-last);last=now;heartbeats++;};timer.Start();
                int count=await store.ImportAsync(stressSource,StressItems(stressSource),null,default);timer.Stop();
                results["stressItems"]=count;results["stressImportMs"]=sw.ElapsedMilliseconds;results["uiHeartbeatsDuringImport"]=heartbeats;results["maxUiGapMs"]=Math.Round(maxGap,1);
                if(count!=100500||heartbeats<2||maxGap>1000)throw new Exception("UI responsiveness check failed.");
                await store.DeleteSourceAsync(stressSource.Id);
            }
            var source=new SourceConfig{Id="smoke-demo",Name="Örnek · Açık filmler",Address=Path.Combine(AppContext.BaseDirectory,"samples","open-films.m3u")};
            await using var startupArt = new SmokeHttpServer(await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory,"samples","test-logo.ico")));
            async IAsyncEnumerable<ContentItem> StartupItems(){await foreach(var item in providers.LoadAsync(source,default))yield return item with{Logo=startupArt.Url};}
            await store.ImportAsync(source,StartupItems(),null,default);await window.SmokeRefreshAsync(source.Id);
            await Task.Delay(500);window.UpdateLayout();SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-preview.png"));results["startup"]=true;
            results["homeUsesSelectedSource"]=window.HomeHost.IsVisible&&!window.ContentGrid.IsVisible&&window.SmokeHome.Items.Count==4&&window.SmokeHome.Items.All(i=>i.SourceId==source.Id)&&window.SmokeHome.Featured?.Kind==ContentKind.Movie;
            window.SmokeHome.Search.Text="Sintel";await WaitUntil(()=>window.SmokeHome.Items.Count==1,TimeSpan.FromSeconds(5));
            window.UpdateLayout();results["homeSearchVisible"]=window.SmokeHome.Search.GetRectFromCharacterIndex(0).Height>=14&&window.SmokeHome.Items.Single().Name.Contains("Sintel");SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-home-search.png"));
            window.SmokeHome.Search.Text="no-match-9853";await WaitUntil(()=>window.SmokeHome.Empty&&window.SmokeHome.Featured is null,TimeSpan.FromSeconds(5));results["homeEmptySearch"]=window.SmokeHome.Items.Count==0;
            window.SmokeHome.Search.Clear();await WaitUntil(()=>window.SmokeHome.Items.Count==4,TimeSpan.FromSeconds(5));
            double contentX=window.MainArea.TranslatePoint(new Point(),window.Root).X;
            window.SmokeSidebar();window.UpdateLayout();results["sidebarExpands"]=window.NavColumn.Width.Value==76&&window.Sidebar.ActualWidth==232&&App.ReadSettings().SidebarExpanded==true&&Math.Abs(window.MainArea.TranslatePoint(new Point(),window.Root).X-contentX)<1;SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-sidebar-expanded.png"));
            window.SmokeSidebar();window.UpdateLayout();results["sidebarCollapses"]=window.NavColumn.Width.Value==76&&window.Sidebar.ActualWidth==76&&App.ReadSettings().SidebarExpanded==false;SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-sidebar-collapsed.png"));
            double width=window.Width;window.Width=940;window.UpdateLayout();SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-compact.png"));results["responsive"]=window.ActualWidth<=950;
            window.SmokeSidebar();window.UpdateLayout();results["sidebarSmallDrawer"]=window.NavColumn.Width.Value==76&&window.Sidebar.ActualWidth==232&&window.BrandToggle.IsVisible;SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-home-small.png"));window.SmokeSidebar();window.Width=width;
            foreach(string key in new[]{"homeUsesSelectedSource","homeSearchVisible","homeEmptySearch","sidebarCollapses","sidebarExpands","sidebarSmallDrawer"})if(!Equals(results[key],true))throw new Exception("1.5 home regression: "+key);
            await window.SmokeBrowseAsync("movie");
            window.UpdateLayout();results["movieCatalogVisible"]=window.CatalogHost.IsVisible&&!window.ContentGrid.IsVisible&&window.SmokeCatalog.Shelves.First().Page.Items.All(i=>i.Kind==ContentKind.Movie)&&window.SmokeCatalog.Shelves.First().Page.Total>=2;
            SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-movies.png"));
            window.SmokeCatalog.Search.Text="Sintel";await WaitUntil(()=>window.SmokeCatalog.Shelves.FirstOrDefault()?.Page.Total==1,TimeSpan.FromSeconds(5));
            results["catalogSearchVisible"]=window.SmokeCatalog.Search.GetRectFromCharacterIndex(0).Height>=14&&window.SmokeCatalog.IsVisible;
            window.SmokeCatalog.Search.Clear();await WaitUntil(()=>window.SmokeCatalog.Shelves.FirstOrDefault()?.Page.Total>=2,TimeSpan.FromSeconds(5));
            await window.SmokeBrowseAsync("series");window.UpdateLayout();results["seriesCatalogVisible"]=window.CatalogHost.IsVisible&&!window.ContentGrid.IsVisible&&window.SmokeCatalog.Shelves.First().Page.Total==0;
            SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-series.png"));
            foreach(string key in new[]{"movieCatalogVisible","catalogSearchVisible","seriesCatalogVisible"})if(!Equals(results[key],true))throw new Exception("1.7.1 catalog regression: "+key);
            await window.SmokeBrowseAsync("all");
            window.SearchBox.Text="Sintel";await WaitUntil(()=>window.ChannelList.Items.Count==1,TimeSpan.FromSeconds(4));window.UpdateLayout();
            var caret=window.SearchBox.GetRectFromCharacterIndex(0);results["searchTextVisible"]=caret.Height>=14&&window.SearchBox.ActualHeight>=24&&window.SearchHint.Visibility==Visibility.Collapsed;
            results["searchFiltersChannels"]=window.ChannelList.Items.Cast<ContentItem>().Single().Name.Contains("Sintel");
            SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-search.png"));window.SearchBox.Clear();await Task.Delay(400);
            results["unifiedLibraryPanel"]=window.LibraryPanel.IsAncestorOf(window.SearchBox)&&window.LibraryPanel.IsAncestorOf(window.CategoryPicker)&&window.LibraryPanel.IsAncestorOf(window.ChannelList);
            results["summaryInSidebar"]=window.Sidebar.IsAncestorOf(window.StatsBar);
            double oldWidth=window.Width,oldHeight=window.Height;window.Width=900;window.Height=650;await Task.Delay(150);window.UpdateLayout();
            results["compactSettingsVisible"]=window.SettingsNav.TranslatePoint(new Point(0,window.SettingsNav.ActualHeight),window.Root).Y<=window.Root.ActualHeight;results["smallWindowGuideVisible"]=window.GuidePanel.ActualHeight>=175&&window.GuidePanel.TranslatePoint(new Point(0,window.GuidePanel.ActualHeight),window.Root).Y<=window.Root.ActualHeight;
            SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-small.png"));window.Width=oldWidth;window.Height=oldHeight;await Task.Delay(150);
            foreach(string key in new[]{"searchTextVisible","searchFiltersChannels","unifiedLibraryPanel","summaryInSidebar","smallWindowGuideVisible","compactSettingsVisible"})if(!Equals(results[key],true))throw new Exception("1.3 layout regression: "+key);
            var sourceWindow=new SourceWindow(window);sourceWindow.Show();await Task.Delay(150);SaveWindow(sourceWindow,Path.Combine(App.DataDirectory,"WX-Player-source.png"));sourceWindow.Close();
            var settingsWindow=window.CreateSettingsWindow();settingsWindow.Show();await Task.Delay(150);
            SaveWindow(settingsWindow,Path.Combine(App.DataDirectory,"WX-Player-settings.png"));settingsWindow.SelectTab(1);settingsWindow.UpdateLayout();await Task.Delay(100);SaveWindow(settingsWindow,Path.Combine(App.DataDirectory,"WX-Player-library-settings.png"));settingsWindow.SelectTab(2);settingsWindow.UpdateLayout();SaveWindow(settingsWindow,Path.Combine(App.DataDirectory,"WX-Player-update-settings.png"));settingsWindow.SmokeShowShortcuts();await Task.Delay(150);SaveWindow(settingsWindow,Path.Combine(App.DataDirectory,"WX-Player-shortcuts.png"));settingsWindow.Close();results["settingsPages"]=true;
            var updateWindow=new UpdateWindow(window,new PreparedUpdate(new Version(1,7,1),"test-only",new string('0',64)),()=>Task.CompletedTask);updateWindow.Show();await Task.Delay(100);SaveWindow(updateWindow,Path.Combine(App.DataDirectory,"WX-Player-update-prompt.png"));updateWindow.Close();results["updatePrompt"]=true;
            int mediaArg=Array.IndexOf(App.Arguments,"--media");
            if(mediaArg>=0&&mediaArg+1<App.Arguments.Length)
            {
                var target=new PlaybackTarget(new Uri(Path.GetFullPath(App.Arguments[mediaArg+1])).AbsoluteUri);
                window.Video.Visibility=Visibility.Visible;window.WelcomePanel.Visibility=Visibility.Collapsed;window.UpdateLayout();
                await engine.PlayAsync(target,settings,default);
                await WaitUntil(()=>engine.Player.IsPlaying,TimeSpan.FromSeconds(15));
                await Task.Delay(1800);using(var media=engine.Player.Media)results["decodedFrames"]=media?.Statistics.DecodedVideo??0;
                results["playing"]=engine.Player.IsPlaying;results["seekable"]=engine.Player.IsSeekable;
                uint videoWidth=0,videoHeight=0;engine.Player.Size(0,ref videoWidth,ref videoHeight);results["sourceVideoSize"]=$"{videoWidth}x{videoHeight}";
                results["rendererIsEmbedded"]=engine.Player.Hwnd==window.Video.RenderingHandle&&window.Video.RenderingHandle!=IntPtr.Zero;
                results["noDetachedOverlayInNormalView"]=window.SmokeNoVideoOverlay;
                var typeface=new Typeface(window.FontFamily,FontStyles.Normal,FontWeights.Normal,FontStretches.Normal);
                results["embeddedInterFont"]=typeface.TryGetGlyphTypeface(out var glyph)&&glyph.FontUri.ToString().Contains("Inter-Regular",StringComparison.OrdinalIgnoreCase);
                SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-playing-ui.png"));
                window.Activate();await Task.Delay(250);results["playingWindowCaptured"]=WindowCapture.Save(window,Path.Combine(App.DataDirectory,"WX-Player-playing-native.png"));
                var original=FullscreenPlacement.WindowBounds(window);IntPtr originalHost=window.Video.RenderingHandle;
                window.SmokeFullscreen();await Task.Delay(700);window.UpdateLayout();
                var chrome=Window.GetWindow(window.FullscreenChannels!)!;
                double rem=Math.Clamp(.0062*chrome.ActualWidth+8,14,22);
                var drawerBounds=window.FullscreenBrowser!.TransformToAncestor(chrome).TransformBounds(new Rect(0,0,window.FullscreenBrowser.ActualWidth,window.FullscreenBrowser.ActualHeight));
                results["fullscreenV2RemDrawerWidth"]=Math.Abs(drawerBounds.Width-26*rem)<1;
                results["fullscreenV2RemDrawerPosition"]=Math.Abs(drawerBounds.Top-5.2*rem)<1&&Math.Abs(chrome.ActualWidth-drawerBounds.Right-1.5*rem)<1;
                if(!Equals(results["fullscreenV2RemDrawerWidth"],true)||!Equals(results["fullscreenV2RemDrawerPosition"],true))throw new Exception("Fullscreen v2 rem scaling does not match the HTML viewport.");
                var full=FullscreenPlacement.WindowBounds(window);var monitor=window.SmokeMonitorBounds;
                results["fullscreenCoversMonitor"]=full.Left==monitor.Left&&full.Top==monitor.Top&&full.Width==monitor.Width&&full.Height==monitor.Height;
                results["fullscreenVideoFillsClient"]=Math.Abs(window.Video.ActualWidth-window.Root.ActualWidth)<1&&Math.Abs(window.Video.ActualHeight-window.Root.ActualHeight)<1;
                results["fullscreenKeepsNativeHandle"]=originalHost==window.Video.RenderingHandle;
                results["fullscreenHasFillCrop"]=!string.IsNullOrWhiteSpace(engine.Player.CropGeometry);
                results["fullscreenCropGeometry"]=engine.Player.CropGeometry??"";
                GetClientRect(window.Video.Handle,out var nativeHost);GetWindowRect(window.Video.RenderingHandle,out var nativeRender);
                results["fullscreenNativeHostSize"]=$"{nativeHost.Width}x{nativeHost.Height}";
                results["fullscreenNativeRenderSize"]=$"{nativeRender.Width}x{nativeRender.Height}";
                results["fullscreenNativeSurfaceFillsHost"]=nativeHost.Width==nativeRender.Width&&nativeHost.Height==nativeRender.Height;
                results["fullscreenBeforeHideToggle"]=window.SmokeHideState;
                if(window.SmokeFullscreenDrawerOpen){window.SmokeShortcut(System.Windows.Input.Key.L);results["fullscreenLShortcutClosesDrawer"]=!window.SmokeFullscreenDrawerOpen;}
                // An operator can use the desktop during QA; isolate only this timed assertion from physical pointer input.
                window.SmokeSuspendPointerReveal=true;
                try { await Task.Delay(4200);results["fullscreenControlsAutoHide"]=!window.SmokeControlsVisible;results["fullscreenAutoHideState"]=window.SmokeHideState; }
                finally { window.SmokeSuspendPointerReveal=false; }
                SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-fullscreen-layout.png"));
                results["fullscreenWindowCaptured"]=WindowCapture.Save(window,Path.Combine(App.DataDirectory,"WX-Player-fullscreen-native.png"));
                window.Activate();window.SmokeFullscreenPointerMotion();
                await WaitUntil(()=>window.SmokeControlsVisible,TimeSpan.FromSeconds(4));
                if(!window.SmokeFullscreenDrawerOpen){window.SmokeShortcut(System.Windows.Input.Key.L);results["fullscreenLShortcutOpensDrawer"]=window.SmokeFullscreenDrawerOpen;}await Task.Delay(400);results["fullscreenControlsReveal"]=window.SmokeControlsVisible;results["fullscreenBrowser"]=window.FullscreenChannels?.Items.Count==window.ChannelList.Items.Count;
                var floating=Window.GetWindow(window.FullscreenChannels)!;floating.UpdateLayout();var browser=window.FullscreenBrowser!;
                var star=Descendants<System.Windows.Controls.Button>(window.FullscreenChannels!).First(b=>b.Tag is ContentItem);
                var starred=(ContentItem)star.Tag;string titleBeforeStar=window.NowTitle.Text;
                star.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice,0,System.Windows.Input.MouseButton.Left){RoutedEvent=UIElement.PreviewMouseLeftButtonDownEvent});
                await WaitUntil(()=>starred.IsFavorite,TimeSpan.FromSeconds(4));await Task.Delay(100);
                results["fullscreenFavoriteUpdatesWithoutSwitching"]=window.NowTitle.Text==titleBeforeStar&&Descendants<SvgIcon>(star).Any(i=>i.Icon=="star-filled")&&(await store.FindAsync(starred.Id))?.IsFavorite==true;
                star.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice,0,System.Windows.Input.MouseButton.Left){RoutedEvent=UIElement.PreviewMouseLeftButtonDownEvent});await WaitUntil(()=>!starred.IsFavorite,TimeSpan.FromSeconds(4));
                if(!Equals(results["fullscreenFavoriteUpdatesWithoutSwitching"],true))throw new Exception("Fullscreen favorite interaction failed");
                await FullscreenV2Smoke.CheckAsync(window,engine,results);
                var overlayBounds=FullscreenPlacement.WindowBounds(floating);
                results["fullscreenOverlayCoversMonitor"]=overlayBounds.Left==monitor.Left&&overlayBounds.Top==monitor.Top&&overlayBounds.Width==monitor.Width&&overlayBounds.Height==monitor.Height;
                var browserPoint=browser.TranslatePoint(new Point(),floating);
                var visibleDrawer=browser.TransformToAncestor(floating).TransformBounds(new Rect(0,0,browser.ActualWidth,browser.ActualHeight));
                results["fullscreenDrawerPlacement"]=Math.Abs(visibleDrawer.Width-26*rem)<2&&Math.Abs(visibleDrawer.Right-(floating.ActualWidth-1.5*rem))<3&&Math.Abs(browserPoint.Y-5.2*rem)<3&&visibleDrawer.Bottom<=floating.ActualHeight-11.5*rem+1;
                if(!Equals(results["fullscreenOverlayCoversMonitor"],true)||!Equals(results["fullscreenDrawerPlacement"],true))throw new Exception("Fullscreen drawer does not match the reference layout.");
                SaveWindow(floating,Path.Combine(App.DataDirectory,"WX-Player-fullscreen-controls.png"));
                window.Activate();await Task.Delay(250);
                results["fullscreenVisibleScreenshot"]=WindowCapture.SaveVisibleFullscreen(window,Path.Combine(App.DataDirectory,"WX-Player-1.7.6-fullscreen-visible.png"),floating);
                window.SmokeFit();results["fitPreservesAspectRatio"]=string.IsNullOrEmpty(engine.Player.CropGeometry)&&string.IsNullOrEmpty(engine.Player.AspectRatio);window.SmokeFit();
                window.SmokeFullscreen();await Task.Delay(500);window.UpdateLayout();
                var restored=FullscreenPlacement.WindowBounds(window);results["windowPlacementRestored"]=original.Left==restored.Left&&original.Top==restored.Top&&original.Width==restored.Width&&original.Height==restored.Height;
                results["normalUiRestoredAfterFullscreen"]=window.Sidebar.IsVisible&&window.ControlsBorder.Parent==window.ViewingPanel&&window.SmokeNoVideoOverlay;
                results["normalCropCleared"]=string.IsNullOrEmpty(engine.Player.CropGeometry);
                var priorGuideHeight=window.GuideRow.Height;
                window.GuideRow.Height=new GridLength(Math.Max(190,window.GuideRow.ActualHeight+80));window.UpdateLayout();await Task.Delay(150);
                GetClientRect(window.Video.Handle,out var guideHost);GetWindowRect(window.Video.RenderingHandle,out var guideRender);
                results["guideResizeKeepsNativeSurfaceSized"]=guideHost.Width==guideRender.Width&&guideHost.Height==guideRender.Height;
                results["mainRenderWindowTitleHidden"]=GetWindowTextLength(window.Video.RenderingHandle)==0;
                window.GuideRow.Height=priorGuideHeight;window.UpdateLayout();
                foreach(var key in new[]{"rendererIsEmbedded","noDetachedOverlayInNormalView","embeddedInterFont","fullscreenCoversMonitor","fullscreenVideoFillsClient","fullscreenNativeSurfaceFillsHost","fullscreenKeepsNativeHandle","fullscreenHasFillCrop","fullscreenControlsAutoHide","fullscreenControlsReveal","fitPreservesAspectRatio","windowPlacementRestored","normalUiRestoredAfterFullscreen","normalCropCleared","guideResizeKeepsNativeSurfaceSized","mainRenderWindowTitleHidden"})
                    if(!Equals(results[key],true))throw new Exception("UI regression failed: "+key);
                window.WindowState=WindowState.Maximized;await Task.Delay(500);var maximized=FullscreenPlacement.WindowBounds(window);
                window.SmokeFullscreen();await Task.Delay(200);window.SmokeFullscreen();
                await WaitUntil(()=>{var bounds=FullscreenPlacement.WindowBounds(window);return window.WindowState==WindowState.Maximized&&bounds.Width==maximized.Width&&bounds.Height==maximized.Height;},TimeSpan.FromSeconds(5));
                var back=FullscreenPlacement.WindowBounds(window);
                results["maximizedRoundTrip"]=window.WindowState==WindowState.Maximized&&maximized.Width==back.Width&&maximized.Height==back.Height;
                if(!Equals(results["maximizedRoundTrip"],true))throw new Exception("Maximized placement regression.");window.WindowState=WindowState.Normal;await Task.Delay(200);
                var epgSource=new SourceConfig{Id="smoke-epg",Name="EPG testi · Yerel örnek",EpgUrl=Path.Combine(App.DataDirectory,"fixture-epg.xml")};
                var now=DateTimeOffset.Now;string Stamp(DateTimeOffset d)=>d.ToString("yyyyMMddHHmmss zzz").Replace(":","");
                File.WriteAllText(epgSource.EpgUrl,$"<!DOCTYPE tv SYSTEM 'xmltv.dtd'><tv><channel id='wx.news'><display-name>WX Haber</display-name></channel><channel id='wx.culture'><display-name>WX Kültür</display-name></channel><programme channel='wx.news' start='{Stamp(now.AddMinutes(-20))}' stop='{Stamp(now.AddMinutes(40))}'><title>Güne Bakış · Test programı</title></programme><programme channel='wx.culture' start='{Stamp(now.AddMinutes(-20))}' stop='{Stamp(now.AddMinutes(40))}'><title>Kültür Atlası · Test programı</title></programme><programme channel='wx.culture' start='{Stamp(now.AddMinutes(40))}' stop='{Stamp(now.AddMinutes(100))}'><title>Sonraki program · Test</title></programme></tv>");
                await using var logos=new SmokeHttpServer(await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory,"samples","test-logo.ico")) );
                var news=new ContentItem{Id="smoke-news",SourceId=epgSource.Id,Name="WX Haber FHD",Category="Haber",Logo=logos.Url,EpgName="WX Haber",Kind=ContentKind.Live,Url=target.Url};
                var culture=new ContentItem{Id="smoke-culture",SourceId=epgSource.Id,Name="WX Kültür HD",Category="Kültür",Logo=logos.Url,Kind=ContentKind.Live,Url=target.Url};
                async IAsyncEnumerable<ContentItem> Channels(){yield return news;yield return culture;await Task.Yield();}
                await store.ImportAsync(epgSource,Channels(),null,default);window.SmokeNavigate("live");await window.SmokeRefreshAsync(epgSource.Id);
                settings.ChannelHealthCheck=true;
                await window.SmokePlayAsync(news);await WaitUntil(()=>window.SmokeHealthState==ChannelHealthState.Healthy,TimeSpan.FromSeconds(8));results["channelHealthDetectsPlaying"]=true;
                window.SmokeToggleMiniPlayer();await Task.Delay(300);
                results["miniPlayerOwnsSamePlayback"]=window.SmokeMiniPlayerVisible&&engine.Player.Hwnd==window.SmokeMiniVideoHandle&&engine.Player.IsPlaying&&!window.Video.IsVisible;
                results["miniKeepsOutputHandle"]=engine.Player.Hwnd==originalHost;
                results["miniRenderAttachedAndVisible"]=window.SmokeMiniWindow is MiniPlayerWindow miniHost&&GetParent(engine.Player.Hwnd)==miniHost.Video.Handle&&IsWindowVisible(engine.Player.Hwnd);
                if(window.SmokeMiniWindow is MiniPlayerWindow sizedMini)
                {
                    sizedMini.Width+=80;sizedMini.Height+=45;sizedMini.UpdateLayout();await Task.Delay(200);
                    GetClientRect(sizedMini.Video.Handle,out var miniHostRect);GetWindowRect(sizedMini.Video.RenderingHandle,out var miniRenderRect);
                    results["miniNativeSurfaceFillsHost"]=miniHostRect.Width==miniRenderRect.Width&&miniHostRect.Height==miniRenderRect.Height;
                    results["renderWindowTitleHidden"]=GetWindowTextLength(sizedMini.Video.RenderingHandle)==0;
                }
                int miniFramesBefore;using(var media=engine.Player.Media)miniFramesBefore=media?.Statistics.DecodedVideo??0;
                await Task.Delay(700);
                using(var media=engine.Player.Media)results["miniDecodedFramesAdvance"]=(media?.Statistics.DecodedVideo??0)>miniFramesBefore;
                if(window.SmokeMiniWindow is{} miniWindow)SaveWindow(miniWindow,Path.Combine(App.DataDirectory,"WX-Player-1.7.3-mini-player.png"));
                if(!Equals(results["miniKeepsOutputHandle"],true)||!Equals(results["miniRenderAttachedAndVisible"],true)||!Equals(results["miniNativeSurfaceFillsHost"],true)||!Equals(results["renderWindowTitleHidden"],true)||!Equals(results["miniDecodedFramesAdvance"],true))throw new Exception("Mini player did not keep a visible, correctly sized VLC output surface.");
                window.SmokeToggleMiniPlayer();await Task.Delay(200);
                results["miniPlayerRestoresNativeVideo"]=!window.SmokeMiniPlayerVisible&&engine.Player.Hwnd==window.Video.RenderingHandle&&engine.Player.IsPlaying;
                if(!Equals(results["miniPlayerOwnsSamePlayback"],true)||!Equals(results["miniPlayerRestoresNativeVideo"],true))throw new Exception("Mini player transfer interrupted playback.");
                window.SmokeToggleMiniPlayer();window.SmokeMiniWindow?.Close();await Task.Delay(150);
                results["miniWindowCloseRestoresVideo"]=!window.SmokeMiniPlayerVisible&&engine.Player.Hwnd==window.Video.RenderingHandle&&engine.Player.IsPlaying;
                if(!Equals(results["miniWindowCloseRestoresVideo"],true))throw new Exception("Closing the mini window did not return the video surface.");
                results["epgAutomaticallyLoaded"]=window.EpgList.Items.Cast<Programme>().Single().Title.StartsWith("Güne Bakış");
                results["epgNowSummary"]=window.GuideNowTitle.Text.StartsWith("Güne Bakış")&&window.GuideNowProgress.Value>0;
                var first=window.SmokePlayAsync(news);await Task.Delay(10);var second=window.SmokePlayAsync(culture);await Task.WhenAll(first,second);
                // The following programme may start after midnight and belong to tomorrow's guide.
                results["epgFollowsLatestChannel"]=window.EpgList.Items.Cast<Programme>().All(p=>p.ChannelId=="wx.culture")&&window.EpgList.Items.Count>=1&&window.GuideTitle.Text.Contains("WX Kültür")&&window.GuideNowTitle.Text.Contains("Kültür Atlası");
                if(!Equals(results["epgAutomaticallyLoaded"],true)||!Equals(results["epgFollowsLatestChannel"],true)||!Equals(results["epgNowSummary"],true))throw new Exception("EPG UI integration failed.");
                await WaitUntil(()=>engine.Player.IsPlaying,TimeSpan.FromSeconds(15));await Task.Delay(900);
                window.SmokeDiscordEnabled(true);
                await WaitUntil(()=>window.SmokeDiscordActivity?.Details=="Kültür Atlası · Test programı",TimeSpan.FromSeconds(35));
                results["discordLiveEpgLatestChannel"]=window.SmokeDiscordActivity is {Start:null,End:null}&&window.SmokeDiscordActivity.State.Contains("WX Kültür");
                if(!Equals(results["discordLiveEpgLatestChannel"],true))throw new Exception("Discord live EPG integration failed.");
                                await window.SmokePlayAsync(news);window.SmokeFullscreen();await Task.Delay(200);window.CategoryPicker.SelectedItem="Kültür";
                await WaitUntil(()=>window.FullscreenChannels?.Items.Count==1,TimeSpan.FromSeconds(5));window.FullscreenChannels!.SelectedIndex=0;
                await WaitUntil(()=>window.NowTitle.Text=="WX Kültür HD",TimeSpan.FromSeconds(5));await Task.Delay(1000);
                results["fullscreenSelectsChannel"]=window.NowTitle.Text=="WX Kültür HD";SaveWindow(Window.GetWindow(window.FullscreenChannels)!,Path.Combine(App.DataDirectory,"WX-Player-fullscreen-categories.png"));
                window.SmokeFullscreen();window.CategoryPicker.SelectedIndex=0;await Task.Delay(500);window.UpdateLayout();
                IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject{for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T found)yield return found;foreach(var d in Descendants<T>(child))yield return d;}}
                await WaitUntil(()=>Descendants<ChannelLogo>(window.ChannelList).Any(l=>l.HasImage),TimeSpan.FromSeconds(6));
                results["channelLogosLoaded"]=Descendants<ChannelLogo>(window.ChannelList).Any(l=>l.HasImage);results["logoDownloadDeduplicated"]=logos.Connections==1;
                window.VolumeSlider.Focus();results["volumeFocusHasNoGreenFill"]=window.VolumeSlider.Background is SolidColorBrush brush&&brush.Color.A==0;
                results["epgGuideHeight"]=Math.Round(window.GuidePanel.ActualHeight);
                results["epgNowPanelVisible"]=window.GuideNowPanel.IsVisible;
                if(!Equals(results["epgNowPanelVisible"],true))throw new Exception("Current EPG programme panel is hidden after fullscreen.");
                SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-epg.png"));
                var statistics=new StatisticsWindow(window,()=> ("Sintel · Yerel test videosu",target),engine,settings);statistics.Show();await Task.Delay(1200);SaveWindow(statistics,Path.Combine(App.DataDirectory,"WX-Player-statistics.png"));statistics.Close();
                using(var statsMedia=engine.Player.Media){var tracks=statsMedia!.Tracks;results["statisticsVideoTrack"]=tracks.Any(t=>t.TrackType==LibVLCSharp.Shared.TrackType.Video&&t.Data.Video.Width==854);results["statisticsAudioTrack"]=tracks.Any(t=>t.TrackType==LibVLCSharp.Shared.TrackType.Audio&&t.Data.Audio.Rate>0);}
                if(!Equals(results["statisticsVideoTrack"],true)||!Equals(results["statisticsAudioTrack"],true))throw new Exception("Statistics metadata missing.");
                var trackWindow=new TracksWindow(window,engine);trackWindow.Show();await Task.Delay(250);SaveWindow(trackWindow,Path.Combine(App.DataDirectory,"WX-Player-tracks.png"));results["modernTracksPopulated"]=trackWindow.AudioPicker.Items.Count>0&&trackWindow.SubtitlePicker.Items.Count>0;trackWindow.Close();
                bool FullHit(MediaSlider slider){for(int y=2;y<slider.ActualHeight;y+=6)if(slider.InputHitTest(new Point(slider.ActualWidth*.6,y)) is null)return false;return slider.ActualHeight>=36;}
                results["largeSliderHitTargets"]=FullHit(window.SeekSlider)&&FullHit(window.VolumeSlider);
                int originalVolume=engine.Player.Volume;window.VolumeSlider.SmokeCommitAt(10+(window.VolumeSlider.ActualWidth-20)*.6);await Task.Delay(250);results["volumeSliderValue"]=window.VolumeSlider.Value;results["volumeEngineValue"]=engine.Player.Volume;results["volumeSliderApplies"]=Math.Abs(engine.Player.Volume-60)<=1;window.VolumeSlider.Value=originalVolume;
                window.SeekSlider.SmokeCommitAt(10+(window.SeekSlider.ActualWidth-20)*.5);await Task.Delay(300);results["timelineCommitSeeks"]=Math.Abs(engine.Player.Position-.5)<.08;
                foreach(string key in new[]{"fullscreenBrowser","fullscreenSelectsChannel","channelLogosLoaded","logoDownloadDeduplicated","volumeFocusHasNoGreenFill","modernTracksPopulated","largeSliderHitTargets","volumeSliderApplies","timelineCommitSeeks"})if(!Equals(results[key],true))throw new Exception("1.3 player regression: "+key);
                if(engine.Player.IsSeekable){engine.Player.Time=2000;await Task.Delay(400);results["seek"]=engine.Player.Time>=1900;}
                engine.Player.Pause();await Task.Delay(350);results["pause"]=engine.Player.State==LibVLCSharp.Shared.VLCState.Paused;engine.Player.Pause();
                results["audioTracks"]=engine.Player.AudioTrackDescription.Count(t=>t.Id>=0);
                var subtitle=Path.Combine(App.DataDirectory,"test-subtitle.srt");File.WriteAllText(subtitle,"1\n00:00:00,000 --> 00:00:08,000\nWX Player subtitle test\n");
                results["externalSubtitle"]=engine.Player.AddSlave(LibVLCSharp.Shared.MediaSlaveType.Subtitle,new Uri(subtitle).AbsoluteUri,true);
                settings.RecordingFolder=Path.Combine(App.DataDirectory,"recordings");var recorded=await engine.StartRecordingAsync(target,"Smoke test",settings);await Task.Delay(3500);await engine.StopRecordingAsync();results["recordBytes"]=File.Exists(recorded)?new FileInfo(recorded).Length:0;
                results["recordFile"]=recorded;
                await engine.PlayAsync(new PlaybackTarget(new Uri(recorded).AbsoluteUri),settings,default);await WaitUntil(()=>engine.Player.IsPlaying,TimeSpan.FromSeconds(10));await Task.Delay(1000);using(var media=engine.Player.Media)results["recordDecodedFrames"]=media?.Statistics.DecodedVideo??0;
                if(Convert.ToInt32(results["decodedFrames"])<=0||Convert.ToInt32(results["recordDecodedFrames"])<=0||Convert.ToInt64(results["recordBytes"])<=0||!Equals(results["pause"],true)||!Equals(results["seek"],true))throw new Exception("Media assertion failed.");
                var homeSource=new SourceConfig{Id="smoke-home",Name="Ana sayfa testi · Yerel katalog"};
                async IAsyncEnumerable<ContentItem> HomeItems(){for(int i=0;i<36;i++){yield return new ContentItem{Id="home-"+i,SourceId=homeSource.Id,Name="Test içeriği "+i.ToString("D2"),Category=i<24?"Filmler":"Diziler",Kind=i<24?ContentKind.Movie:ContentKind.Series,Logo=logos.Url,Url=target.Url};await Task.Yield();}}
                await store.ImportAsync(homeSource,HomeItems(),null,default);await store.FavoriteAsync("home-1",true);await store.RememberAsync("home-2");
                await window.SmokeRefreshAsync(homeSource.Id);await window.SmokeBrowseAsync("home");window.UpdateLayout();
                // A newer async artwork render can supersede the awaited render (especially on a cold self-contained launch).
                await WaitUntil(()=>!window.SmokeHome.IsLoadingArtwork&&window.SmokeHome.Items.Count==36&&window.SmokeHome.Items.All(i=>i.SourceId==homeSource.Id),TimeSpan.FromSeconds(6));
                // 1.6.1 expanded discovery shelves to 48; this fixture has 24 movies + 12 series.
                results["homeShelvesBoundedAndIsolated"]=window.SmokeHome.Items.Count==36&&window.SmokeHome.Items.Count(i=>i.Kind==ContentKind.Movie)==24&&window.SmokeHome.Items.Count(i=>i.Kind==ContentKind.Series)==12&&window.SmokeHome.Items.All(i=>i.SourceId==homeSource.Id);
                results["homeFavoritesAndHistory"]=window.SmokeHome.Items.Any(i=>i.Id=="home-1"&&i.IsFavorite)&&window.SmokeHome.Items.Any(i=>i.Id=="home-2");
                await WaitUntil(()=>Descendants<ChannelLogo>(window.SmokeHome).Any(l=>l.DecodeWidth>=480&&l.HasImage),TimeSpan.FromSeconds(6));results["homeProviderPostersLoaded"]=true;
                var homeItem=window.SmokeHome.Items.First(i=>i.Kind==ContentKind.Movie);await window.SmokeOpenHomeAsync(homeItem);await WaitUntil(()=>engine.Player.IsPlaying,TimeSpan.FromSeconds(12));await Task.Delay(400);
                int movieVersion=window.SmokePlaybackVersion;
                var pauseWatch=Stopwatch.StartNew();window.SmokeTogglePlayback();
                await WaitUntil(()=>engine.Player.State==LibVLCSharp.Shared.VLCState.Paused,TimeSpan.FromSeconds(3));pauseWatch.Stop();
                results["movie173PauseResponsive"]=pauseWatch.ElapsedMilliseconds<500;
                long pausedAt=engine.Player.Time;window.SmokeTogglePlayback();
                await WaitUntil(()=>engine.Player.IsPlaying,TimeSpan.FromSeconds(3));
                results["movie173ResumeKeepsPosition"]=window.SmokePlaybackVersion==movieVersion&&engine.Player.Time>=Math.Max(0,pausedAt-1000);
                if(!Equals(results["movie173PauseResponsive"],true)||!Equals(results["movie173ResumeKeepsPosition"],true))throw new Exception("Movie playback toggle was slow or restarted the item.");
                results["homeCardStartsPlayback"]=window.NowTitle.Text==homeItem.Name&&window.ContentGrid.IsVisible&&!window.HomeHost.IsVisible;
                await WaitUntil(()=>window.SmokeDiscordActivity?.Details==homeItem.Name&&window.SmokeDiscordActivity.End is >0,TimeSpan.FromSeconds(5));
                results["discordMovieFromRealPlayback"]=true;
                engine.Player.SetPause(true);await WaitUntil(()=>window.SmokeDiscordActivity is {End:null}&&window.SmokeDiscordActivity.State.Contains("Paused"),TimeSpan.FromSeconds(5));
                results["discordPauseFreezesTimer"]=true;
                engine.Player.SetPause(false);await WaitUntil(()=>engine.Player.IsPlaying&&window.SmokeDiscordActivity?.End is >0,TimeSpan.FromSeconds(5));
                engine.Player.Time=20000;await Task.Delay(500);
                await WaitUntil(()=>window.SmokeDiscordActivity?.End is{} end&&Math.Abs(end-DateTimeOffset.UtcNow.ToUnixTimeSeconds()-(engine.Player.Length-engine.Player.Time)/1000d)<3,TimeSpan.FromSeconds(5));
                results["discordResumeSeekCountdown"]=true;
                var homeHandle=window.Video.RenderingHandle;await window.SmokeBrowseAsync("home");await Task.Delay(200);results["homeNavigationKeepsPlayback"]=engine.Player.IsPlaying&&engine.Player.Hwnd==homeHandle&&window.HomeHost.IsVisible;
                await window.SmokeBrowseAsync("movie");results["homeReturnKeepsPlayer"]=engine.Player.IsPlaying&&window.Video.RenderingHandle==homeHandle&&window.CatalogHost.IsVisible;
                foreach(string key in new[]{"homeShelvesBoundedAndIsolated","homeFavoritesAndHistory","homeProviderPostersLoaded","homeCardStartsPlayback","homeNavigationKeepsPlayback","homeReturnKeepsPlayer"})if(!Equals(results[key],true))throw new Exception("Home integration: "+key);
                await Task.Delay(500);engine.Player.Time=12000;await Task.Delay(800);await window.SmokeSaveProgressAsync();
                var checkpoint=await store.ProgressAsync(homeItem.Id);results["moviePositionStored"]=checkpoint is{PositionMs:>=11500,Completed:false};
                await window.SmokePlayAsync(culture);await window.SmokePlayAsync(homeItem);await WaitUntil(()=>engine.Player.IsPlaying&&engine.Player.Time>=11500,TimeSpan.FromSeconds(15));results["movieResumesFromStoredPosition"]=true;
                var seriesSource=new SourceConfig{Id="smoke-series151",Name="Dizi kitaplığı · Yerel test"};
                async IAsyncEnumerable<ContentItem> Episodes151(){for(int i=1;i<=3;i++){yield return new ContentItem{Id="series151-"+i,SourceId=seriesSource.Id,Name=$"500T (2021) S01 500T - {i}. Bölüm - Başlık - S01.E{i:00}",Category="TR ✦ Gain",Logo=logos.Url,Kind=ContentKind.Movie,Url=target.Url};await Task.Yield();}}
                await store.ImportAsync(seriesSource,Episodes151(),null,default);await window.SmokeRefreshAsync(seriesSource.Id);await window.SmokeBrowseAsync("home");
                var show=window.SmokeHome.Items.Single();results["homeSeriesHasOneCard"]=show.Kind==ContentKind.Series&&show.Name=="500T (2021)";
                var choices=await window.SmokeEpisodesAsync(seriesSource,show);var picker=new EpisodeWindow(window,show,choices);picker.Show();await Task.Delay(200);SaveWindow(picker,Path.Combine(App.DataDirectory,"WX-Player-episodes.png"));picker.Close();
                async Task ChooseEpisode(int index)
                {
                    var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(150)};timer.Tick+=(_,_)=>{var dialog=window.OwnedWindows.OfType<EpisodeWindow>().FirstOrDefault();if(dialog is null)return;timer.Stop();dialog.Episodes.SelectedIndex=index;results["episodeSelectionIndex"]=dialog.Episodes.SelectedIndex;results["episodeSelectionName"]=((ContentItem?)dialog.Episodes.SelectedItem)?.Name??"none";dialog.Footer.Children.OfType<System.Windows.Controls.Button>().Last().RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));};timer.Start();try{await window.SmokeOpenHomeAsync(show);}finally{timer.Stop();}
                }
                await ChooseEpisode(1);results["episodePlayedName"]=window.NowTitle.Text;await WaitUntil(()=>engine.Player.IsPlaying,TimeSpan.FromSeconds(15));await Task.Delay(700);engine.Player.Time=16000;await Task.Delay(800);await window.SmokeSaveProgressAsync();
                int episodeVersion=window.SmokePlaybackVersion;long episodeAt=engine.Player.Time;
                window.SmokeTogglePlayback();await WaitUntil(()=>engine.Player.State==LibVLCSharp.Shared.VLCState.Paused,TimeSpan.FromSeconds(3));
                window.SmokeTogglePlayback();await WaitUntil(()=>engine.Player.IsPlaying,TimeSpan.FromSeconds(3));
                results["episode173ResumeDoesNotRestart"]=window.SmokePlaybackVersion==episodeVersion&&engine.Player.Time>=episodeAt-1000;
                if(!Equals(results["episode173ResumeDoesNotRestart"],true))throw new Exception("Episode restarted on pause/resume.");
                await window.SmokeBrowseAsync("home");var watched=window.SmokeHome.Items.Single();results["seriesLastEpisodeAndTimeVisible"]=watched.Progress is{PositionMs:>=15500}&&watched.ProgressLabel.Contains("B02");
                window.UpdateLayout();SaveWindow(window,Path.Combine(App.DataDirectory,"WX-Player-watch-progress.png"));
                var updatedChoices=await window.SmokeEpisodesAsync(seriesSource,watched);var progressPicker=new EpisodeWindow(window,watched,updatedChoices);progressPicker.Show();await Task.Delay(150);results["episodePickerSelectsLastEpisode"]=((ContentItem?)progressPicker.Episodes.SelectedItem)?.Episode==2;SaveWindow(progressPicker,Path.Combine(App.DataDirectory,"WX-Player-episode-progress.png"));progressPicker.Close();
                await window.SmokePlayAsync(culture);await ChooseEpisode(1);await WaitUntil(()=>engine.Player.IsPlaying&&engine.Player.Time>=15500,TimeSpan.FromSeconds(15));results["episodeResumesFromStoredPosition"]=true;
                foreach(string key in new[]{"moviePositionStored","movieResumesFromStoredPosition","homeSeriesHasOneCard","seriesLastEpisodeAndTimeVisible","episodePickerSelectsLastEpisode","episodeResumesFromStoredPosition"})if(!Equals(results[key],true))throw new Exception("1.5.1 playback progress: "+key);
                await WaitUntil(()=>window.SmokeDiscordActivity?.State.Contains("S01E02")==true,TimeSpan.FromSeconds(5));
                results["discordSeriesEpisodeFromRealPlayback"]=window.SmokeDiscordActivity!.Details.Contains("500T");
                await engine.StopAsync();await WaitUntil(()=>window.SmokeDiscordActivity is null,TimeSpan.FromSeconds(5));results["discordStopClears"]=true;
                window.SmokeDiscordEnabled(false);
            }
            if(App.Arguments.Contains("--timeshift")&&mediaArg>=0)
            {
                var target=new PlaybackTarget(new Uri(Path.GetFullPath(App.Arguments[mediaArg+1])).AbsoluteUri);
                bool soak=App.Arguments.Contains("--timeshift-soak");
                await using var broadcast=new SmokeHttpServer(await File.ReadAllBytesAsync((string)results["recordFile"]),true);
                if(soak)target=new PlaybackTarget(broadcast.Url);
                await engine.PlayAsync(target,settings,default,true);
                results["liveBufferStarted"]=engine.HasLiveBuffer;results["liveBufferStatus"]=engine.TimeshiftStatus;
                await WaitUntil(()=>engine.Player.IsPlaying,TimeSpan.FromSeconds(20));await Task.Delay(soak?70000:8000);
                results["liveBufferedSeconds"]=engine.BufferedSeconds;results["singleUpstreamConnection"]=!soak||broadcast.Connections==1;results["sixtySecondWindow"]=!soak||Math.Abs(engine.BufferedSeconds-60)<.1;
                using(var media=engine.Player.Media)results["liveLocalDecodedFrames"]=media?.Statistics.DecodedVideo??0;
                await engine.RewindLiveAsync(soak?60:20);await WaitUntil(()=>engine.Player.IsPlaying,TimeSpan.FromSeconds(15));await Task.Delay(1000);
                await Task.Delay(2000);results["liveReplay"]=engine.IsReplay;results["replayTime"]=engine.Player.Time;results["replayLength"]=engine.Player.Length;results["liveReplayDelay"]=engine.BehindLive;
                using(var media=engine.Player.Media)results["replayDecodedFrames"]=media?.Statistics.DecodedVideo??0;
                await engine.GoLiveAsync();await WaitUntil(()=>engine.Player.IsPlaying,TimeSpan.FromSeconds(15));await Task.Delay(1000);
                results["returnedToLive"]=!engine.IsReplay&&engine.HasLiveBuffer;
                if(!Equals(results["sixtySecondWindow"],true)||!engine.HasLiveBuffer||!Equals(results["liveReplay"],true)||Convert.ToInt32(results["liveLocalDecodedFrames"])<=0||Convert.ToInt32(results["replayDecodedFrames"])<=0)throw new Exception("Live buffer integration failed");
                await engine.StopAsync();results["liveBufferCleaned"]=!Directory.EnumerateDirectories(Path.Combine(App.DataDirectory,"timeshift")).Any();
            }
            int fixtureArg=Array.IndexOf(App.Arguments,"--restart-fixture");
            if(fixtureArg>=0&&fixtureArg+1<App.Arguments.Length)
            {
                byte[] fixture=await File.ReadAllBytesAsync(App.Arguments[fixtureArg+1]);
                string hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(fixture)).ToLowerInvariant();
                string json=JsonSerializer.Serialize(new{tag_name="v1.7.0",draft=false,prerelease=false,assets=new[]{new{name="WXPlayer.exe",size=fixture.Length,digest="sha256:"+hash,browser_download_url="https://github.com/schwairex/WX-Player/releases/download/v1.7.0/WXPlayer.exe"}}});
                using var controller=new UpdateController(settings,default,new GitHubUpdater(new UpdateFixtureHandler(json,fixture)));
                await controller.CheckAsync(true);if(controller.Ready is null)throw new Exception("Fixture update not staged: "+controller.Status);
                Environment.SetEnvironmentVariable("WXPLAYER_TEST_PARENT",Environment.ProcessId.ToString());
                await controller.RestartAsync();results["updaterRestartDispatched"]=true;
            }
            results["success"]=true;
        }
        catch(Exception ex){results["success"]=false;results["error"]=ex.ToString();}
        File.WriteAllText(Path.Combine(App.DataDirectory,"smoke-results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
        await window.Dispatcher.InvokeAsync(window.Close,DispatcherPriority.ApplicationIdle);
    }
    private sealed class UpdateFixtureHandler(string json,byte[] executable):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=request.RequestUri!.Host=="api.github.com"?new StringContent(json):new ByteArrayContent(executable)});
    }
    private static async Task WaitUntil(Func<bool> condition,TimeSpan timeout){var sw=Stopwatch.StartNew();while(!condition()){if(sw.Elapsed>timeout)throw new TimeoutException("Playback did not start.");await Task.Delay(100);}}
    [DllImport("user32.dll")]private static extern bool PostMessage(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam);
    [DllImport("user32.dll")]private static extern IntPtr GetParent(IntPtr hwnd);
    [DllImport("user32.dll")]private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")]private static extern bool GetClientRect(IntPtr hwnd,out FullscreenPlacement.Rect rect);
    [DllImport("user32.dll")]private static extern bool GetWindowRect(IntPtr hwnd,out FullscreenPlacement.Rect rect);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetWindowTextLength(IntPtr hwnd);
    private static async IAsyncEnumerable<ContentItem> StressItems(SourceConfig source)
    {
        for(int i=0;i<100500;i++){yield return new ContentItem{Id=ContentItem.Key(source.Id,i.ToString()),SourceId=source.Id,Name=$"Örnek Kanal {i:D6}",Category="Performans",Url=$"https://example.test/{i}.ts"};if(i%1000==0)await Task.Yield();}
    }
    private static void SaveWindow(Window window,string path)
    {
        window.UpdateLayout();var content=(FrameworkElement)window.Content;var bitmap=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(content);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(path);encoder.Save(file);
    }
}





