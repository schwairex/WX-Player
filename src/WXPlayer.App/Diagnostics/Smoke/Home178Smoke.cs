using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WXPlayer.Core;

namespace WXPlayer.App;

// Explicit QA mode only: the caller supplies an isolated --data-dir.
internal static class Home178Smoke
{
    internal static async Task RunAsync(MainWindow window, LibraryStore store, Dictionary<string, object> results)
    {
        void Check(string key, bool condition) { results[key] = condition; if (!condition) throw new InvalidOperationException(key); }
        var source = new SourceConfig { Id = "home178", Name = "Görünüm testi · IPTV", Kind = SourceKind.Playlist };
        bool enabled = ArtworkService.Enabled; ArtworkService.Enabled = false;
        try
        {
            var cache = Path.Combine(App.DataDirectory, "artwork-cache"); Directory.CreateDirectory(cache);
            string url = "https://home178-fixture.test/poster.png";
            await File.WriteAllBytesAsync(Path.Combine(cache, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url))).ToLowerInvariant() + ".img"), HomeLayoutSmoke.Poster(600, 900, "GÖRÜNÜM TESTİ", "#284B44"));
            async IAsyncEnumerable<ContentItem> Items()
            {
                for (int i = 0; i < 20; i++) yield return new() { Id = "home178-film-" + i, SourceId = source.Id, Kind = ContentKind.Movie, Name = "Film " + i + " (2024)", Category = "[TR] Sinema", Logo = url, Url = "https://example.test/movie/" + i + ".mp4" };
                for (int i = 0; i < 16; i++) yield return new() { Id = "home178-episode-" + i, SourceId = source.Id, Kind = ContentKind.Episode, Name = "Dizi " + i + " S01E01", Category = "[TR] Diziler", Logo = url, Url = "https://example.test/series/" + i + ".mp4" };
                for (int i = 0; i < 5; i++) yield return new() { Id = "home178-live-" + i, SourceId = source.Id, Kind = ContentKind.Live, Name = "WX TV " + i + " FHD", Category = "[TR] Ulusal", Logo = url, Url = "https://example.test/live/" + i + ".ts" };
                await Task.Yield();
            }
            await store.ImportAsync(source, Items(), null, default);
            var film = (await store.FindAsync("home178-film-0"))!; var live = (await store.FindAsync("home178-live-0"))!;
            await store.RememberAsync(film.Id); await store.RememberAsync(live.Id); await store.SaveProgressAsync(film, 1620000, 6600000);
            await window.SmokeRefreshAsync(source.Id); await window.SmokeBrowseAsync("home");
            var home = window.SmokeHome; await Wait(() => !home.IsLoadingArtwork && home.Items.Count > 20); window.UpdateLayout();
            Check("home178HeaderUsesOriginalControls", home.IsAncestorOf(window.SourcePicker) && home.IsAncestorOf(window.AddSourceButton) && home.IsAncestorOf(window.SidebarToggle));
            Check("home178StatusAtPageEnd", home.IsAncestorOf(window.BottomBar) && home.IsAncestorOf(window.CancelButton));
            Check("home178OneVisibleSearch", Descendants<TextBox>(home).Count(t => t.IsVisible) == 1);
            Check("home178RedundantShortcutsHidden", !Descendants<Button>(home).Any(b => b.IsVisible && b.Content is string s && s is "Canlı TV" or "Filmler" or "Diziler" or "Favoriler"));
            Check("home178RowsOrdered", home.PresentationShelves.Select(s => s.Section).SequenceEqual(new[] { "recent", "recent-live", "movie", "series", "live" }));
            Check("home178MoviesSeriesCappedAt12", home.PresentationShelves.Where(s => s.Section is "movie" or "series").All(s => s.Page.Items.Count == 12));
            Check("home178TypesSeparate", home.PresentationShelves.Where(s => s.Section.EndsWith("live")).All(s => s.Page.Items.All(i => i.Kind == ContentKind.Live)));
            Check("home178EmptyFavorites", Descendants<TextBlock>(home).Any(t => t.Text.StartsWith("Henüz favori eklemedin")));
            Check("home178DisplayMetadataOnly", Descendants<TextBlock>(home).Any(t => t.Text == "Ulusal") && (await store.FindAsync(live.Id))!.Category == "[TR] Ulusal");
            Check("home178NoBlurOrShadow", !Descendants<FrameworkElement>(home).Any(e => e.Effect is not null));
            Check("home178SharedCardResource", Descendants<Button>(home).Where(b => b.Tag is ContentItem i && i.Kind != ContentKind.Live).All(b => Equals(b.Style, home.FindResource("CatalogCardButton"))));
            Check("home178ProgressBelowPoster", Descendants<ProgressBar>(home).Any(p => p.Parent is StackPanel && Math.Abs(p.Value - 1620000d / 6600000) < .001));
            Check("home178Channels16By10", Descendants<Grid>(home).Where(g => g.Width == 200 && g.Height == 125).Any());
            double top = home.Header.TranslatePoint(new Point(), window).Y; home.ScrollToBottom(); await Task.Delay(100);
            Check("home178StickyHeader", Math.Abs(home.Header.TranslatePoint(new Point(), window).Y - top) < .1);
            Check("home178FooterReachable", window.StatusText.IsVisible && window.StatusText.TranslatePoint(new Point(), window).Y < window.ActualHeight);
            Capture(window, "WXPlayer-1.7.8-alt-satirlar.png"); home.ScrollToTop(); Capture(window, "WXPlayer-1.7.8-ana-sayfa.png");
            var favorite = Descendants<Button>(home).First(b => b.DataContext is ContentItem i && i.Kind == ContentKind.Movie && System.Windows.Automation.AutomationProperties.GetName(b).StartsWith("Favoriyi değiştir"));
            window.Activate(); favorite.Focus(); await Wait(() => favorite.IsKeyboardFocused);
            var favoriteItem = (ContentItem)favorite.DataContext; favorite.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Wait(() => home.PresentationShelves.Any(s => s.Section == "favorites"));
            Check("home178FavoriteCallbackPersists", (await store.FindAsync(favoriteItem.Id))!.IsFavorite);
            await Wait(() => Descendants<Button>(home).Any(b => b.Tag is HomeFavoriteAction f && f.Item.Id == favoriteItem.Id && b.IsKeyboardFocused));
            Check("home178FavoriteKeepsKeyboardFocus", true);
            var liveFavorite = Descendants<Button>(home).First(b => b.DataContext is ContentItem i && i.Kind == ContentKind.Live && System.Windows.Automation.AutomationProperties.GetName(b).StartsWith("Favoriyi değiştir"));
            liveFavorite.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Wait(() => home.PresentationShelves.Any(s => s.Section == "favorites-live"));
            Check("home178SixRowsAndPreservedLive", home.PresentationShelves.Select(s => s.Section).SequenceEqual(new[] { "recent", "recent-live", "movie", "series", "favorites", "favorites-live", "live" }));
            var all = Descendants<StackPanel>(home).Single(s => Equals(s.Tag, "movie"));
            Descendants<Button>(all).Single(b => Equals(b.Content, "Tümünü gör →")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Wait(() => window.CatalogHost.IsVisible); window.UpdateLayout(); Check("home178ExistingMovieNavigation", window.SmokeCatalog.IsAncestorOf(window.SourcePicker));
            await window.SmokeBrowseAsync("home"); await Wait(() => !home.IsLoadingArtwork); window.Width = 900; window.Height = 650; await Task.Delay(150); window.UpdateLayout();
            Check("home178CompactHeaderFits", Descendants<Button>(home.Header).Where(b => b.IsVisible).All(b => b.TranslatePoint(new Point(b.ActualWidth, 0), window).X <= window.ActualWidth + .5));
            Check("home178RemClamp", Math.Abs(home.RootFontSize - 14) < .001); Capture(window, "WXPlayer-1.7.8-kucuk-pencere.png");
            window.Width = 1440; window.Height = 920; await window.SmokeBrowseAsync("live"); window.UpdateLayout();
            Check("home178OriginalShellRestored", window.TopBar.IsAncestorOf(window.SourcePicker) && window.TopBar.IsAncestorOf(window.SidebarToggle) && window.MainArea.Children.Contains(window.BottomBar));
            Check("home178LibraryUnchanged", (await store.QueryAsync(source.Id, ContentKind.Movie, null, "", false, false, 0)).Total == 20);
        }
        finally { ArtworkService.Enabled = enabled; }
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    { for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) { var child = VisualTreeHelper.GetChild(root, i); if (child is T match) yield return match; foreach (var nested in Descendants<T>(child)) yield return nested; } }
    private static async Task Wait(Func<bool> condition)
    { var clock = Stopwatch.StartNew(); while (!condition()) { if (clock.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("Home 1.7.8 QA"); await Task.Delay(80); } }
    private static void Capture(Window window, string name)
    { window.UpdateLayout(); var content = (FrameworkElement)window.Content; var bitmap = new RenderTargetBitmap((int)content.ActualWidth, (int)content.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var file = File.Create(Path.Combine(App.DataDirectory, name)); encoder.Save(file); }
}




