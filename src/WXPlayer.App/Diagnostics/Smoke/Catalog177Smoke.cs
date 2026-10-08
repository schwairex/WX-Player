using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WXPlayer.Core;

namespace WXPlayer.App;

// Opt-in smoke fixtures, always using the caller's isolated --data-dir.
internal static class Catalog177Smoke
{
    internal static async Task RunAsync(MainWindow window, LibraryStore store, Dictionary<string, object> results)
    {
        void Check(string key, bool value) { results[key] = value; if (!value) throw new InvalidOperationException("1.7.7: " + key); }
        var source = new SourceConfig { Id = "catalog177", Name = "Görünüm testi · IPTV", Kind = SourceKind.Playlist };
        bool artworkEnabled = ArtworkService.Enabled; ArtworkService.Enabled = false;
        try
        {
            var cache = Path.Combine(App.DataDirectory, "artwork-cache"); Directory.CreateDirectory(cache);
            var palette = new[] { "#284B44", "#544067", "#5C4637", "#244456" };
            for (int i = 0; i < 4; i++)
            {
                string url = "https://catalog-fixture.test/poster-" + i + ".png";
                await File.WriteAllBytesAsync(Path.Combine(cache, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url))).ToLowerInvariant() + ".img"),
                    HomeLayoutSmoke.Poster(600, 900, new[] { "KIYI", "GECE", "YOLCULUK", "UZAK" }[i], palette[i]));
            }
            async IAsyncEnumerable<ContentItem> Items()
            {
                for (int i = 0; i < 96; i++) yield return new() { Id = "catalog177-film-" + i, SourceId = source.Id,
                    Kind = ContentKind.Movie, Name = "Film " + i.ToString("D2") + " (2024)", Category = "Sinema · " + i % 4,
                    Logo = i % 7 == 0 ? "" : "https://catalog-fixture.test/poster-" + i % 4 + ".png", Url = "https://example.test/movie/" + i + ".mp4" };
                for (int i = 0; i < 24; i++) yield return new() { Id = "catalog177-episode-" + i, SourceId = source.Id,
                    Kind = ContentKind.Episode, Name = "Dizi " + i.ToString("D2") + " (2024) S01E01", Category = "Diziler · " + i % 3,
                    Logo = i % 6 == 0 ? "" : "https://catalog-fixture.test/poster-" + i % 4 + ".png", Url = "https://example.test/series/" + i + ".mp4" };
                await Task.Yield();
            }
            await store.ImportAsync(source, Items(), null, default);
            await store.SaveProgressAsync((await store.FindAsync("catalog177-film-1"))!, 1620000, 6600000);
            await window.SmokeRefreshAsync(source.Id); await window.SmokeBrowseAsync("movie");
            await Wait(() => window.SmokeCatalog.Shelves.FirstOrDefault()?.Page.Total == 96);
            var catalog = window.SmokeCatalog; window.UpdateLayout();
            Check("catalog177RealMoviesOnly", catalog.Shelves.First().Page.Items.All(i => i.Kind == ContentKind.Movie));
            Check("catalog177OriginalSourceControl", catalog.IsAncestorOf(window.SourcePicker) && Equals(window.SourcePicker.SelectedItem, source));
            Check("catalog177ShellActionsPreserved", catalog.IsAncestorOf(window.AddSourceButton));
            Check("catalog177ScopedCssTokens", ((SolidColorBrush)catalog.Resources["--ink"]).Color.ToString() == "#FFF2F5EE" &&
                ((SolidColorBrush)Application.Current.Resources["--ink"]).Color.ToString() == "#FFF4F7F1");
            Check("catalog177RootFontClamp", Math.Abs(catalog.RootFontSize - Math.Clamp(window.ActualWidth * .005 + 8, 14, 20)) < .01);
            await Wait(() => Descendants<ChannelLogo>(catalog).Count(l => l.HasImage) >= 10);
            Check("catalog177PosterAspect", Descendants<Grid>(catalog).Where(g => g.Children.OfType<ChannelLogo>().Any()).All(g => Math.Abs(g.Height / g.Width - 1.5) < .001));
            Check("catalog177NoArtworkCard", Descendants<System.Windows.Shapes.Rectangle>(catalog).Any(r => r.IsVisible && r.StrokeDashArray.Count == 2));
            Check("catalog177HeroUsesSavedProgress", Descendants<TextBlock>(catalog.Hero).Any(t => t.Text == "KALDIĞIN YERDEN DEVAM ET"));
            Capture(window, "WXPlayer-1.7.7-filmler.png");
            var firstCard = Descendants<Button>(catalog).First(b => System.Windows.Automation.AutomationProperties.GetName(b).StartsWith("Filmi izle"));
            firstCard.Focus(); await Task.Delay(100); Check("catalog177CardsKeyboardFocusable", firstCard.IsKeyboardFocused);
            var favorite = Descendants<Button>(catalog).First(b => System.Windows.Automation.AutomationProperties.GetName(b).StartsWith("Favoriyi değiştir"));
            var favoriteItem = (ContentItem)favorite.DataContext; favorite.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Wait(() => favoriteItem.IsFavorite);
            Check("catalog177OriginalFavoritePersists", (await store.FindAsync(favoriteItem.Id))?.IsFavorite == true);
            var next = Descendants<Button>(catalog).First(b => Equals(b.ToolTip, "Tüm filmler · ileri"));
            for (int i = 0; i < 12; i++) { next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(90); window.UpdateLayout(); }
            await Wait(() => Descendants<Grid>(catalog).Count(g => g.DataContext is ContentItem && g.Children.OfType<Button>().Count() == 2) > 136);
            Check("catalog177PaginationStillLoads", Descendants<ScrollViewer>(catalog).Any(s => s.HorizontalOffset > 0));
            double top = catalog.Header.TranslatePoint(new Point(), window).Y;
            catalog.ScrollToBottom(); await Task.Delay(150);
            Check("catalog177VerticalScroll", catalog.VerticalOffset > 100 && catalog.ScrollableHeight > 100);
            Check("catalog177StickyHeader", Math.Abs(catalog.Header.TranslatePoint(new Point(), window).Y - top) < .1);
            Descendants<Button>(catalog).First(b => Equals(b.Content, "Tümü")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(80); Check("catalog177AllReturnsToTop", catalog.VerticalOffset == 0);
            catalog.Search.Text = "Film 02"; await Wait(() => catalog.Shelves.FirstOrDefault()?.Page.Total == 1);
            window.UpdateLayout(); Check("catalog177SearchStillBound", window.SearchBox.Text == "Film 02" && catalog.Search.GetRectFromCharacterIndex(0).Height >= 14);
            catalog.Search.Clear(); await Wait(() => catalog.Shelves.FirstOrDefault()?.Page.Total == 96);
            var seriesTab = Descendants<Button>(catalog.Header).First(b => Equals(b.Content, "Diziler"));
            seriesTab.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Wait(() => catalog.Shelves.FirstOrDefault()?.Page.Total == 24);
            Check("catalog177SeriesTabUsesExistingNavigation", catalog.Shelves.First().Page.Items.All(i => i.Kind == ContentKind.Series));
            await Wait(() => Descendants<ChannelLogo>(catalog).Count(l => l.HasImage) >= 10); Capture(window, "WXPlayer-1.7.7-diziler.png");
            window.Width = 900; window.Height = 650; await Task.Delay(180); window.UpdateLayout();
            Check("catalog177CompactHeaderFits", catalog.Header.TranslatePoint(new Point(catalog.Header.ActualWidth, 0), window).X <= window.ActualWidth + .5);
            Check("catalog177CompactSearchFits", catalog.Search.ActualWidth >= 100); Capture(window, "WXPlayer-1.7.7-kucuk-pencere.png");
            window.Width = 2560; window.Height = 1200; await Task.Delay(180); window.UpdateLayout();
            Check("catalog177LargeFontClamp", catalog.RootFontSize == 20);
            await window.SmokeBrowseAsync("home"); window.UpdateLayout();
            Check("catalog177ShellRestoresForHome", window.SmokeHome.IsAncestorOf(window.SourcePicker) && window.SmokeHome.IsAncestorOf(window.AddSourceButton) && !window.TopBar.IsVisible);
            await window.SmokeBrowseAsync("movie"); await window.SmokeBrowseAsync("live"); window.UpdateLayout();
            Check("catalog177ShellRestoresForLive", window.TopBar.IsAncestorOf(window.SourcePicker) && !catalog.IsVisible && window.ContentGrid.IsVisible);
            Check("catalog177LibraryPreserved", (await store.QueryAsync(source.Id, ContentKind.Movie, null, "", false, false, 0)).Total == 96);
        }
        finally { ArtworkService.Enabled = artworkEnabled; }
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        { var child = VisualTreeHelper.GetChild(root, i); if (child is T match) yield return match; foreach (var nested in Descendants<T>(child)) yield return nested; }
    }
    private static async Task Wait(Func<bool> condition)
    { var clock = Stopwatch.StartNew(); while (!condition()) { if (clock.Elapsed > TimeSpan.FromSeconds(8)) throw new TimeoutException("Catalog smoke condition"); await Task.Delay(80); } }
    private static void Capture(Window window, string filename)
    {
        window.UpdateLayout(); var content = (FrameworkElement)window.Content;
        var bitmap = new RenderTargetBitmap((int)content.ActualWidth, (int)content.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var file = File.Create(Path.Combine(App.DataDirectory, filename)); encoder.Save(file);
    }
}

