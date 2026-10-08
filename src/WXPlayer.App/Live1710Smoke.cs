using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WXPlayer.Core;

namespace WXPlayer.App;

// Isolated, explicit QA only. Normal application data is never seeded.
internal static class Live1710Smoke
{
    internal static async Task RunAsync(MainWindow window, LibraryStore store, PlaybackEngine engine, Dictionary<string, object> results)
    {
        void Check(string key, bool value) { results[key] = value; if (!value) throw new InvalidOperationException(key); }
        await window.SmokeBrowseAsync("live"); window.UpdateLayout();
        Check("liveHeaderAlignedWithViewingColumn", Math.Abs(window.TopBar.TranslatePoint(new Point(), window).X - window.ViewingPanel.TranslatePoint(new Point(), window).X) < 1);
        Check("liveHeaderBesideFullHeightLibrary", Math.Abs(window.LibraryPanel.TranslatePoint(new Point(), window).Y - window.TopBar.TranslatePoint(new Point(), window).Y) < 1);
        Check("liveChannelVirtualizationPreserved", System.Windows.Controls.VirtualizingPanel.GetIsVirtualizing(window.ChannelList) && System.Windows.Controls.VirtualizingPanel.GetVirtualizationMode(window.ChannelList) == VirtualizationMode.Recycling);
        Check("liveManropeFont", window.FontFamily.Source.Contains("Manrope"));
        var controls = new[] { window.PlayButton, window.RecordButton, window.MiniPlayerButton, window.MuteButton };
        var source = new SourceConfig { Id = "live1710-qa", Name = "WX test kütüphanesi", Address = Path.Combine(AppContext.BaseDirectory, "samples", "open-films.m3u") };
        await using var art = new SmokeHttpServer(HomeLayoutSmoke.Poster(96, 96, "WX", "#2A3330"));
        int mediaArg = Array.IndexOf(App.Arguments, "--media");
        string media = mediaArg >= 0 ? new Uri(Path.GetFullPath(App.Arguments[mediaArg + 1])).AbsoluteUri : "https://example.test/qa.ts";
        async IAsyncEnumerable<ContentItem> Items()
        {
            for (int i = 0; i < 305; i++) yield return new() { Id = "live1710-" + i, SourceId = source.Id, Name = $"TR • WX Kanal {i:D3} FHD", Category = i < 200 ? "TR ✨ Ulusal" : "TR ✨ Haber", Kind = ContentKind.Live, Logo = i == 2 ? "" : art.Url, Url = media, EpgId = "live1710-epg" };
            await Task.Yield();
        }
        await store.ImportAsync(source, Items(), null, default);
        async IAsyncEnumerable<Programme> Guide()
        {
            var now = DateTimeOffset.Now;
            yield return new("live1710-epg", "Geçmiş program", "Gerçek EPG alanı", now.AddHours(-1), now.AddMinutes(-15));
            yield return new("live1710-epg", "Şimdi yayında", "EPG açıklaması", now.AddMinutes(-15), now.AddMinutes(30));
            yield return new("live1710-epg", "Sonraki program", "Sonraki açıklama", now.AddMinutes(30), now.AddHours(1));
            yield return new("live1710-epg", "Akşam programı", "Dördüncü açıklama", now.AddHours(1), now.AddHours(2));
            await Task.Yield();
        }
        await store.ImportEpgAsync(source.Id, Guide(), default); await window.SmokeRefreshAsync(source.Id);
        Check("liveRealSourceControlsRetained", window.SourcePicker.SelectedItem is SourceConfig s && s.Id == source.Id && window.AddSourceButton.IsVisible);
        Check("liveOriginalPaging", window.ChannelList.Items.Count == 150 && window.NextPage.IsEnabled);
        var first = (ContentItem)window.ChannelList.Items[0];
        window.ChannelList.UpdateLayout();
        var container = (ListBoxItem)window.ChannelList.ItemContainerGenerator.ContainerFromIndex(0);
        var favorite = Descendants<Button>(container).Single(b => ReferenceEquals(b.Tag, first));
        favorite.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Wait(() => first.IsFavorite);
        Check("liveOriginalFavoriteHandler", (await store.FindAsync(first.Id))!.IsFavorite);
        window.SearchBox.Text = "Kanal 00"; await Wait(() => window.ChannelList.Items.Count == 10);
        Check("liveSearchKeepsRawNames", window.ChannelList.Items.Cast<ContentItem>().All(i => i.Name.StartsWith("TR •")));
        window.SearchBox.Clear(); await Wait(() => window.ChannelList.Items.Count == 150);
        window.CategoryPicker.SelectedIndex = window.CategoryPicker.Items.IndexOf("TR ✨ Haber"); await Wait(() => window.ChannelList.Items.Count == 105);
        Check("liveOriginalCategoryFilter", window.ChannelList.Items.Cast<ContentItem>().All(i => i.Category == "TR ✨ Haber"));
        window.CategoryPicker.SelectedIndex = 0; await Wait(() => window.ChannelList.Items.Count == 150);
        if (mediaArg >= 0)
        {
            await window.SmokePlayAsync(first); await Wait(() => window.EpgList.Items.Count >= 3); await Task.Delay(1200);
            Check("liveHorizontalVirtualizedEpg", Descendants<VirtualizingStackPanel>(window.EpgList).Any(p => p.Orientation == Orientation.Horizontal));
            Check("liveOriginalProgrammeFields", window.EpgList.Items.Cast<Programme>().Any(p => p.Title == "Şimdi yayında" && p.IsNow));
            var nowContainer = (ListBoxItem)window.EpgList.ItemContainerGenerator.ContainerFromItem(window.EpgList.Items.Cast<Programme>().First(p => p.IsNow));
            var nowProgress = Descendants<ProgressBar>(nowContainer).Single();
            Check("liveEpgProgressUsesRealProgramme", nowProgress.Value > 0 && nowProgress.Visibility == Visibility.Visible);
                        var indicator = (FrameworkElement)nowProgress.Template.FindName("PART_Indicator", nowProgress);
            Check("liveEpgIndicatorIsPainted", indicator.ActualWidth > 0 && indicator.ActualHeight > 0);
            results["liveEpgProgressSize"] = $"{nowProgress.ActualWidth:0.0}x{nowProgress.ActualHeight:0.0}";
            var overlay = window.OwnedWindows.Cast<Window>().SingleOrDefault(w => w.Title == "WX Player live controls");
            Check("liveOverlayUsesOriginalControls", overlay is not null && overlay.IsAncestorOf(window.PlayButton) && overlay.IsAncestorOf(window.SeekSlider));
            Check("liveOverlaySharpText", overlay is not null && TextOptions.GetTextFormattingMode(overlay) == TextFormattingMode.Ideal && TextOptions.GetTextRenderingMode(overlay) == TextRenderingMode.ClearType);
            Check("liveOverlayAlignedAfterResize", Math.Abs(overlay!.ActualWidth - window.VideoBorder.ActualWidth * Math.Clamp(.005 * window.ActualWidth + 8, 14, 20) / 16 + 2) < 3);
            window.PlayButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Wait(() => engine.Player.State == LibVLCSharp.Shared.VLCState.Paused);
            Check("liveOriginalPauseHandler", !engine.Player.IsPlaying);
            window.PlayButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Wait(() => engine.Player.IsPlaying);
            Check("liveOriginalResumeHandler", engine.Player.Hwnd == window.Video.RenderingHandle);
            var gear = Descendants<System.Windows.Controls.Primitives.ToggleButton>(overlay).Single(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Oynatıcı seçenekleri");
            overlay.Activate(); gear.Focus(); await Task.Delay(100);
            var space = new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, PresentationSource.FromVisual(overlay), 0, System.Windows.Input.Key.Space) { RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent };
            overlay.RaiseEvent(space);
            Check("liveGearSpaceDoesNotTogglePlayback", ReferenceEquals(System.Windows.Input.Keyboard.FocusedElement, gear) && !space.Handled && engine.Player.IsPlaying);
            gear.IsChecked = true; await Task.Delay(100);
            var menu = Descendants<System.Windows.Controls.Primitives.Popup>(overlay).Single();
            Check("liveGearRetainsOriginalActions", menu.IsOpen && Descendants<Button>(menu.Child).Contains(window.RecordButton) && Descendants<Button>(menu.Child).Contains(window.MiniPlayerButton) && Descendants<Button>(menu.Child).Contains(window.TracksButton));
            Save(window, overlay, Path.Combine(App.DataDirectory, "WXPlayer-1.7.10-canli-tv-menu.png"));
            gear.IsChecked = false;
            window.NextDayButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Wait(() => window.EpgList.Items.Count == 0);
            window.PreviousDayButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Wait(() => window.EpgList.Items.Count >= 3);
            Check("liveOriginalEpgDayNavigation", window.EpgList.Items.Cast<Programme>().Any(p => p.IsNow));
            window.SmokeToggleMiniPlayer(); await Wait(() => window.SmokeMiniPlayerVisible);
            Check("liveMiniPlayerKeepsSamePlayback", engine.Player.IsPlaying && engine.Player.Hwnd == window.SmokeMiniVideoHandle);
            window.SmokeToggleMiniPlayer(); await Wait(() => !window.SmokeMiniPlayerVisible);
            Check("liveMiniPlayerReturnsSameVideoHost", engine.Player.IsPlaying && engine.Player.Hwnd == window.Video.RenderingHandle);
            window.Width = 980; window.UpdateLayout(); await Task.Delay(100);
            Check("liveNarrowRemLayout", Math.Abs(window.ListColumn.Width.Value - 288) < .1 && window.ChannelList.IsVisible && window.TopBar.IsVisible);
            Save(window, overlay, Path.Combine(App.DataDirectory, "WXPlayer-1.7.10-canli-tv-kucuk.png"));
            window.Width = 1440; window.UpdateLayout(); await Task.Delay(100);
            window.SmokeSidebar(); window.UpdateLayout();
            Check("liveDrawerDoesNotResizeContent", window.Sidebar.Width == 232 && window.NavColumn.Width.Value == 70.4);
            window.SmokeSidebar(); window.UpdateLayout();
            Save(window, overlay, Path.Combine(App.DataDirectory, "WXPlayer-1.7.10-canli-tv.png"));
            window.SmokeFullscreen(); await Task.Delay(100);
            Check("liveFullscreenRestoresOriginalOwnership", !window.OwnedWindows.Cast<Window>().Any(w => w.Title == "WX Player live controls") && window.SmokeControlsVisible);
            window.SmokeFullscreen(); await Task.Delay(100);
            Check("liveFullscreenReturnsToReferenceLayout", window.OwnedWindows.Cast<Window>().Any(w => w.Title == "WX Player live controls") && window.ChannelList.IsVisible);
        }
        await window.SmokeBrowseAsync("home");
        Check("liveHomeShellRestored", window.HomeHost.IsVisible && window.SmokeHome.IsAncestorOf(window.SourcePicker));
        await window.SmokeBrowseAsync("movie");
        Check("liveCatalogShellRestored", window.CatalogHost.IsVisible && window.SmokeCatalog.IsAncestorOf(window.SourcePicker));
        await window.SmokeBrowseAsync("all");
        Check("liveAllLibraryUsesModernPlayer", window.OwnedWindows.Cast<Window>().Any(w => w.Title == "WX Player live controls") && window.SmokeEmbeddedPlayerReady);
        // 1.7.13 intentionally shares the appearance with every embedded library.
        // Home is now the destination that restores the original control parents.
        await window.SmokeBrowseAsync("home");
        Check("liveDefaultControlParentsRestored", window.ViewingPanel.Children.Contains(window.ControlsBorder) && controls[0] == window.PlayButton && controls[1] == window.RecordButton && controls[2] == window.MiniPlayerButton);
        Check("liveDefaultEpgRestored", !Descendants<VirtualizingStackPanel>(window.EpgList).Any(p => p.Orientation == Orientation.Horizontal));
    }
    internal static IEnumerable<T> Descendants<T>(DependencyObject? root) where T : DependencyObject
    { if (root is null) yield break; for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) { var child = VisualTreeHelper.GetChild(root, i); if (child is T typed) yield return typed; foreach (var node in Descendants<T>(child)) yield return node; } }
    private static async Task Wait(Func<bool> condition) { var deadline = DateTime.UtcNow.AddSeconds(15); while (!condition()) { if (DateTime.UtcNow > deadline) throw new InvalidOperationException("Live QA timed out"); await Task.Delay(40); } }
    private static void Save(Window window, Window? overlay, string path)
    {
        window.UpdateLayout(); var visual = new DrawingVisual(); using (var dc = visual.RenderOpen())
        {
            var baseImage = Snapshot(window); dc.DrawImage(baseImage, new Rect(0, 0, window.ActualWidth, window.ActualHeight));
            if (overlay is not null) { var top = window.PointFromScreen(overlay.PointToScreen(new Point())); dc.DrawImage(Snapshot(overlay), new Rect(top, new Size(overlay.ActualWidth, overlay.ActualHeight)));
                foreach (var popup in Descendants<System.Windows.Controls.Primitives.Popup>(overlay).Where(p => p.IsOpen))
                {
                    if (popup.Child is not FrameworkElement child) continue;
                    child.UpdateLayout(); var a = window.PointFromScreen(child.PointToScreen(new Point())); var b = window.PointFromScreen(child.PointToScreen(new Point(child.ActualWidth, child.ActualHeight)));
                    dc.DrawImage(Snapshot(child), new Rect(a, b));
                }
            }
        }
        var result = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth), (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32); result.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(result)); using var stream = File.Create(path); encoder.Save(stream);
    }
    private static BitmapSource Snapshot(FrameworkElement window) { var bitmap = new RenderTargetBitmap(Math.Max(1, (int)Math.Ceiling(window.ActualWidth)), Math.Max(1, (int)Math.Ceiling(window.ActualHeight)), 96, 96, PixelFormats.Pbgra32); bitmap.Render(window); return bitmap; }
}
