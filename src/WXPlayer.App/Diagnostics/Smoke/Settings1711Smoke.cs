using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WXPlayer.Core;

namespace WXPlayer.App;

internal static class Settings1711Smoke
{
    internal static async Task RunAsync(MainWindow window, LibraryStore store, Dictionary<string, object> results)
    {
        void Check(string key, bool ok) { results[key] = ok; if (!ok) throw new InvalidOperationException(key); }
        await window.SmokeBrowseAsync("live"); window.UpdateLayout(); await Task.Delay(150);
        var rail = window.Sidebar.TransformToAncestor(window).TransformBounds(new Rect(window.Sidebar.RenderSize));
        foreach (string section in new[] { "home", "movie", "series", "favorites", "epg", "recent", "live" })
        {
            await window.SmokeBrowseAsync(section); window.UpdateLayout(); await Task.Delay(100);
            var current = window.Sidebar.TransformToAncestor(window).TransformBounds(new Rect(window.Sidebar.RenderSize));
            var selected = new[] { window.HomeNav, window.LiveNav, window.MovieNav, window.SeriesNav, window.FavoriteNav, window.EpgNav, window.RecentNav }.Where(NavigationVisual.GetIsSelected).ToArray();
            Check("sidebarConsistent_" + section, Math.Abs(current.Width - rail.Width) < 1 && selected.Length == 1 && Equals(selected[0].Tag, section) && ReferenceEquals(selected[0].Style, window.FindResource("LiveNav")));
        }
        Check("liveHeaderHealthLabelHidden", !window.HealthBadge.IsVisible);
        var overlay = window.OwnedWindows.Cast<Window>().Single(w => w.Title == "WX Player live controls");
        Check("liveHeaderLivePillRemoved", !Descendants<TextBlock>(overlay).Any(t => t.Text == "●  CANLI"));
        Save(window, "WXPlayer-1.7.11-sidebar.png");
        var other = new Window { Title = "WXPlayer QA normal stacking", Width = 320, Height = 220 };
        try
        {
            other.Show(); other.Activate(); await Task.Delay(150);
            Check("otherWindowAboveNormalLiveOwnerAndOverlay", Shell1711Smoke.Above(new System.Windows.Interop.WindowInteropHelper(other).Handle, new System.Windows.Interop.WindowInteropHelper(window).Handle) && Shell1711Smoke.Above(new System.Windows.Interop.WindowInteropHelper(other).Handle, new System.Windows.Interop.WindowInteropHelper(overlay).Handle));
        }
        finally { other.Close(); }
        var source = new SourceConfig { Id = "settings1711-qa", Name = "WX test kütüphanesi", Address = Path.Combine(AppContext.BaseDirectory, "samples", "open-films.m3u") };
        async IAsyncEnumerable<ContentItem> Items() { yield return new() { Id = "settings1711-item", SourceId = source.Id, Name = "WX test kanalı", Kind = ContentKind.Live, Url = "https://example.test/qa.ts" }; await Task.Yield(); }
        await store.ImportAsync(source, Items(), null, default); await store.FavoriteAsync("settings1711-item", true);
        var before = JsonSerializer.Serialize(App.ReadSettings());
        var settings = window.CreateSettingsWindow();
        try
        {
            settings.Show(); await Task.Delay(150);
            Check("settingsWindowSizeAndFont", settings.Width == 850 && settings.Height == 760 && settings.FontFamily.Source.Contains("Manrope"));
            Check("settingsSharpText", TextOptions.GetTextFormattingMode(settings) == TextFormattingMode.Ideal && TextOptions.GetTextRenderingMode(settings) == TextRenderingMode.ClearType);
            Check("settingsAllEightOriginalToggles", Descendants<CheckBox>(settings).Count() == 8 && Descendants<CheckBox>(settings).All(c => !string.IsNullOrEmpty(AutomationProperties.GetName(c))));
            var nav = Descendants<Button>(settings).Where(b => b.Content is IconLabel).ToArray();
            Check("settingsFiveFixedSections", nav.Select(b => ((IconLabel)b.Content).Label).SequenceEqual(new[] { "Oynatma", "Kısayollar", "Kütüphane", "Çevrimiçi", "Güncellemeler" }));
            var left = nav[0].PointToScreen(new Point());
            for (int i = 0; i < nav.Length; i++)
            {
                nav[i].RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); settings.UpdateLayout(); await Task.Delay(100);
                Check("settingsSectionNavigation_" + i, NavigationVisual.GetIsSelected(nav[i]) && nav.Count(NavigationVisual.GetIsSelected) == 1 && nav[0].PointToScreen(new Point()) == left);
                Save(settings, "WXPlayer-1.7.11-settings-" + i + ".png");
            }
            settings.SmokeShowShortcuts(); settings.UpdateLayout(); await Task.Delay(100);
            var last = Descendants<TextBlock>(settings).Single(t => t.Text == "Önceki / sonraki kanal");
            Check("shortcutsHookSelectsAndRevealsLastCard", NavigationVisual.GetIsSelected(nav[1]) && last.IsVisible && last.PointToScreen(new Point()).Y < settings.PointToScreen(new Point(0, settings.ActualHeight - 50)).Y);
            settings.Width = 540; settings.UpdateLayout(); await Task.Delay(150);
            Check("settingsNarrowShortcutsKeepReadableLabels", last.ActualWidth >= 80 && last.PointToScreen(new Point()).X >= settings.PointToScreen(new Point()).X);
            settings.Width = 850; settings.UpdateLayout();
            settings.SelectTab(0); settings.UpdateLayout();
            var gpu = Descendants<CheckBox>(settings).Single(c => Equals(c.Content, "GPU donanım ivmesi")); bool originalGpu = gpu.IsChecked == true;
            var caption = Descendants<TextBlock>(settings).Single(t => t.Text == "GPU donanım ivmesi");
            caption.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0, System.Windows.Input.MouseButton.Left) { RoutedEvent = System.Windows.Input.Mouse.MouseDownEvent });
            Check("settingsCaptionStillTogglesOriginalControl", gpu.IsChecked != originalGpu);
            Check("settingsNoTextRasterizationEffects", Descendants<FrameworkElement>(settings).All(e => e.Effect is null && e.CacheMode is null && e.OpacityMask is null));
            settings.SelectTab(2); settings.UpdateLayout();
            var edit = Descendants<Button>(settings).Single(b => Equals(b.Content, "Düzenle"));
            _ = settings.Dispatcher.BeginInvoke(() => edit.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            await Wait(() => settings.OwnedWindows.Cast<Window>().Any(w => w is SourceWindow));
            var editor = settings.OwnedWindows.Cast<Window>().Single(w => w is SourceWindow);
            Check("settingsSourceEditUsesOriginalDialogAndData", Descendants<TextBox>(editor).Any(t => t.Text == source.Name)); editor.Close(); await Task.Delay(100);
            var clear = Descendants<Button>(settings).Single(b => Equals(b.Content, "Favorileri temizle"));
            _ = settings.Dispatcher.BeginInvoke(() => clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            await Wait(() => settings.OwnedWindows.Cast<Window>().Any(w => w.Title == "Favorileri temizle · WX Player"));
            var confirm = settings.OwnedWindows.Cast<Window>().Single(w => w.Title == "Favorileri temizle · WX Player");
            Check("settingsOriginalCleanupConfirmText", Descendants<TextBlock>(confirm).Any(t => t.Text == "Tüm favori işaretleri kaldırılacak."));
            Descendants<Button>(confirm).Single(b => Equals(b.Content, "Vazgeç")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(100);
            Check("settingsCancelledCleanupRetainsFavorites", (await store.FindAsync("settings1711-item"))!.IsFavorite);
            _ = settings.Dispatcher.BeginInvoke(() => clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            await Wait(() => settings.OwnedWindows.Cast<Window>().Any(w => w.Title == "Favorileri temizle · WX Player"));
            confirm = settings.OwnedWindows.Cast<Window>().Single(w => w.Title == "Favorileri temizle · WX Player");
            Descendants<Button>(confirm).Single(b => Equals(b.Content, "Temizle")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            // The modal close returns before its async owner callback starts; IsEnabled can still be true.
            await WaitAsync(async () => settings.IsEnabled && (await store.FindAsync("settings1711-item")) is { IsFavorite: false } && (await store.SourcesAsync()).Any(s => s.Id == source.Id));
            Check("settingsConfirmedCleanupRunsOriginalCallback", (await store.FindAsync("settings1711-item")) is { IsFavorite: false } && (await store.SourcesAsync()).Any(s => s.Id == source.Id));
            var remove = Descendants<Button>(settings).Single(b => Equals(b.Content, "Kaldır"));
            _ = settings.Dispatcher.BeginInvoke(() => remove.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            await Wait(() => settings.OwnedWindows.Cast<Window>().Any(w => w.Title == "Kaynağı kaldır · WX Player"));
            confirm = settings.OwnedWindows.Cast<Window>().Single(w => w.Title == "Kaynağı kaldır · WX Player");
            Check("settingsOriginalSourceConfirmText", Descendants<TextBlock>(confirm).Any(t => t.Text == source.Name + " ve bu kaynağın içerikleri kaldırılacak."));
            Descendants<Button>(confirm).Single(b => Equals(b.Content, "Temizle")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await WaitAsync(async () => settings.IsEnabled && !(await store.SourcesAsync()).Any(s => s.Id == source.Id) && Descendants<TextBlock>(settings).Any(t => t.Text == "Henüz kaynak eklenmedi."));
            Check("settingsSourceRemovalAndAsyncReloadPreserved", !(await store.SourcesAsync()).Any(s => s.Id == source.Id) && Descendants<TextBlock>(settings).Any(t => t.Text == "Henüz kaynak eklenmedi."));
            var cache = Descendants<TextBox>(settings).Single(t => AutomationProperties.GetName(t).StartsWith("Önbellek"));
            var folder = Descendants<TextBox>(settings).Single(t => AutomationProperties.GetName(t) == "Kayıtlar");
            var save = Descendants<Button>(settings).Single(b => Equals(b.Content, "Değişiklikleri kaydet"));
            foreach (string bad in new[] { "199", "10001", "abc" })
            {
                cache.Text = bad; save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check("settingsRejectsCache_" + bad, !settings.Saved && settings.IsVisible && NavigationVisual.GetIsSelected(nav[0]) && JsonSerializer.Serialize(App.ReadSettings()) == before);
            }
            cache.Text = "200"; folder.Text = "invalid'folder"; save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check("settingsRejectsInvalidFolder", !settings.Saved && settings.IsVisible && JsonSerializer.Serialize(App.ReadSettings()) == before);
            Descendants<Button>(settings).Single(b => Equals(b.Content, "Kapat")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check("settingsCancelDoesNotSave", !settings.Saved && JsonSerializer.Serialize(App.ReadSettings()) == before);
        }
        finally { if (settings.IsVisible) settings.Close(); }
        foreach (int value in new[] { 200, 10000 })
        {
            var persist = window.CreateSettingsWindow(); persist.Show(); await Task.Delay(100);
            try
            {
                foreach (var c in Descendants<CheckBox>(persist)) c.IsChecked = value == 200;
                Descendants<ComboBox>(persist).Single().SelectedIndex = value == 200 ? 1 : 2;
                Descendants<TextBox>(persist).Single(t => AutomationProperties.GetName(t).StartsWith("Önbellek")).Text = value.ToString();
                var folder = Path.Combine(App.DataDirectory, "recordings-" + value);
                Descendants<TextBox>(persist).Single(t => AutomationProperties.GetName(t) == "Kayıtlar").Text = folder;
                Descendants<Button>(persist).Single(b => Equals(b.Content, "Değişiklikleri kaydet")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var read = App.ReadSettings(); bool enabled = value == 200;
                Check("settingsSavesAllOriginalFields_" + value, persist.Saved && read.NetworkCacheMs == value && read.RecordingFolder == folder && read.HardwareAcceleration == enabled && read.AdaptiveCache == enabled && read.FullscreenFill == enabled && read.AutoUpdate == enabled && read.VideoOutput == (enabled ? "direct3d9" : "any") && read.DiscoverArtwork == enabled && read.DiscordRichPresence == enabled && read.MiniPlayerEnabled == enabled && read.ChannelHealthCheck == enabled);
            }
            finally { if (persist.IsVisible) persist.Close(); }
        }
    }
    private static async Task Wait(Func<bool> test) { var until = DateTime.UtcNow.AddSeconds(8); while (!test() && DateTime.UtcNow < until) await Task.Delay(50); if (!test()) throw new TimeoutException("Settings QA wait"); }
    private static async Task WaitAsync(Func<Task<bool>> test) { var until = DateTime.UtcNow.AddSeconds(8); while (!await test() && DateTime.UtcNow < until) await Task.Delay(50); if (!await test()) throw new TimeoutException("Settings QA async wait"); }
    internal static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T self) yield return self;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Descendants<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    internal static void Save(FrameworkElement view, string file)
    {
        view.UpdateLayout(); var bitmap = new RenderTargetBitmap((int)Math.Ceiling(view.ActualWidth), (int)Math.Ceiling(view.ActualHeight), 96, 96, PixelFormats.Pbgra32); bitmap.Render(view);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap)); using var stream = File.Create(Path.Combine(App.DataDirectory, file)); png.Save(stream);
    }
}
