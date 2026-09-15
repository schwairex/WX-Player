using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WXPlayer.Core;

namespace WXPlayer.App;

// Controlled UI fixtures only: no provider account, network artwork or user library is used.
internal static class HomeLayoutSmoke
{
    internal static async Task RunAsync(MainWindow window, Dictionary<string, object> results)
    {
        await using var portrait = new SmokeHttpServer(Poster(600, 1800, "UZAK KIYI", "#27495B"), delayMs: 650);
        await using var landscape = new SmokeHttpServer(Poster(1800, 450, "GECE YOLU", "#564D66"), delayMs: 650);
        await using var logo = new SmokeHttpServer(Poster(400, 200, "WX TV", "#24392F"), delayMs: 650);
        var movies = Enumerable.Range(0, 8).Select(i => new ContentItem
        {
            Id = "layout-movie-" + i, SourceId = "layout", Kind = ContentKind.Movie,
            Name = new[] { "Uzak Kıyı (2024)", "Gece Yolu (2023)", "Bir Şehrin Hikâyesi: Çok Uzun Bir Yolculuğun Ardından Gelen Sabah (2025)", "Sessiz Günler" }[i % 4],
            Category = "Dram · Sinema", Logo = i == 3 ? "http://127.0.0.1:1/missing.png" : i % 2 == 0 ? portrait.Url : landscape.Url,
            Progress = i == 0 ? new WatchProgress("layout-movie-0", "layout", "", "Uzak Kıyı", 1620000, 6600000, false, 1) : null
        }).ToArray();
        var shows = Enumerable.Range(0, 6).Select(i => new ContentItem { Id = "layout-series-" + i, SourceId = "layout", Kind = ContentKind.Series, Name = i == 0 ? "Kayıp Şehir" : "Kıyı Hikâyeleri · " + i, Category = "Dram · Dizi", Logo = i % 2 == 0 ? landscape.Url : portrait.Url }).ToArray();
        var live = new ContentItem { Id = "layout-live", SourceId = "layout", Kind = ContentKind.Live, Name = "WX TV Haber HD", Category = "Ulusal", Logo = logo.Url };
        ContentItem[] mixed = [live, movies[0], shows[0], movies[2], movies[3], live with { Id = "layout-live2", Name = "WX Kültür" }];
        HomeShelf Shelf(string title, string section, ContentItem[] items) => new(title, section, new WXPlayer.Core.Page(items, items.Length));
        var shelves = new[] { Shelf("Son izlenenler", "recent", mixed), Shelf("Favorilerin", "favorites", mixed), Shelf("Filmler", "movie", movies), Shelf("Diziler", "series", shows), Shelf("Şimdi canlı", "live", [live]) };
        string? played = null, browsed = null; bool retried = false;
        var home = new HomeView(i => played = i.Id, _ => { }, s => browsed = s, () => { }, () => { });
        var original = window.HomeHost.Content; double width = window.Width, height = window.Height;
        try
        {
            window.Width = 1440; window.Height = 960; window.HomeHost.Content = home;
            home.Render("Örnek kütüphane", shelves, "", movies[0]); window.UpdateLayout();
            var before = Cards(home).Select(b => b.RenderSize).ToArray();
            await Until(() => Descendants<ChannelLogo>(home).Count(l => l.HasImage) >= 20);
            await Task.Delay(150); window.UpdateLayout();
            Check("home152AsyncArtworkDoesNotShiftLayout", before.SequenceEqual(Cards(home).Select(b => b.RenderSize)), results);
            CheckGeometry(home, results, "Desktop");
            Save(window, "WX-Player-1.5.2-home.png");
            home.ScrollToVerticalOffset(300); await Task.Delay(80); Save(window, "WX-Player-1.5.2-mixed-shelves.png");
            var favorite = Descendants<StackPanel>(home).Single(p => Equals(p.Tag, "favorites"));
            var last = Cards(favorite).Last(); last.BringIntoView(); await Task.Delay(80);
            Check("home152KeyboardNavigationRevealsLastCard", Descendants<ScrollViewer>(favorite).Single().HorizontalOffset > 0, results);
            var chosen = Cards(favorite).First(); chosen.Focus(); window.UpdateLayout();
            var chrome = (Border)chosen.Template.FindName("Chrome", chosen);
            Check("home152CardFocusVisible", chosen.IsKeyboardFocused && chosen.BorderThickness.Left >= 1 && chrome.BorderBrush.ToString() == "#FFC1EC8B", results);
            chosen.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check("home152CardAction", played == ((ContentItem)chosen.Tag).Id, results);
            var all = Descendants<Button>(favorite).First(b => Equals(b.Content, "Tümünü gör")); all.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check("home152ShelfBrowseAction", browsed == "favorites", results);
            home.ScrollToVerticalOffset(690); await Task.Delay(80); Save(window, "WX-Player-1.5.2-posters.png");
            window.Width = 900; window.Height = 760;
            home.Render("Çok uzun kaynak adı ile taşma kontrolü · Örnek kütüphane", shelves, "", movies[2]);
            await Task.Delay(250); window.UpdateLayout(); home.ScrollToTop();
            CheckGeometry(home, results, "Compact"); results["home152CompactWidth"]=home.ActualWidth; Save(window, "WX-Player-1.5.2-compact.png");
            Check("home152CompactSearchOwnRow", Grid.GetRow((UIElement)home.Search.Parent) == 1, results);
            Check("home152HeroActionsWithinBounds", Descendants<Button>(Feature(home)).All(b => b.TranslatePoint(new Point(0, b.ActualHeight), Feature(home)).Y <= Feature(home).ActualHeight), results);
            Save(window, "WX-Player-1.5.2-compact.png");
            window.Width = 1920; window.Height = 1080; await Task.Delay(100); window.UpdateLayout();
            CheckGeometry(home, results, "Wide"); Save(window, "WX-Player-1.5.2-wide.png");
            window.Width = 1440; window.Height = 960;
            home.Loading(); window.UpdateLayout(); double loadingHeight = Feature(home).ActualHeight; Save(window, "WX-Player-1.5.2-loading.png");
            home.Error(() => retried = true); window.UpdateLayout();
            Descendants<Button>(Feature(home)).Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check("home152ErrorRetry", retried, results); Save(window, "WX-Player-1.5.2-error.png");
            home.Render(null, [], ""); window.UpdateLayout(); Check("home152EmptyLibrary", home.Empty && home.Featured is null, results); Save(window, "WX-Player-1.5.2-empty.png");
            home.Search.Text = "Aranan içerik"; home.Render("Örnek", [], home.Search.Text); window.UpdateLayout();
            Descendants<Button>(Feature(home)).Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check("home152EmptySearchClear", home.Search.Text.Length == 0, results);
            Check("home152StableStateHeight", Feature(home).ActualHeight == loadingHeight, results);
        }
        finally { window.HomeHost.Content = original; window.Width = width; window.Height = height; }
    }
    private static Grid Feature(HomeView home) => (Grid)((StackPanel)home.Content).Children[2];
    private static IEnumerable<Button> Cards(DependencyObject root) => Descendants<Button>(root).Where(b => b.Tag is ContentItem);
    private static void CheckGeometry(HomeView home, Dictionary<string, object> results, string suffix)
    {
        Check("home152FeatureBounded" + suffix, Feature(home).ActualHeight is > 0 and <= 264 && Feature(home).ActualWidth <= home.ActualWidth, results);
        foreach (var section in new[] { "recent", "favorites", "movie", "series" })
        {
            var panel = Descendants<StackPanel>(home).Single(p => Equals(p.Tag, section)); var cards = Cards(panel).ToArray();
            Check("home152EqualCards_" + section + suffix, cards.Length > 1 && cards.Select(c => c.RenderSize).Distinct().Count() == 1, results);
            Check("home152EqualArtwork_" + section + suffix, cards.Select(c => ((Grid)((StackPanel)c.Content).Children[0]).RenderSize).Distinct().Count() == 1, results);
        }
    }
    private static void Check(string key, bool condition, Dictionary<string, object> results) { results[key] = condition; if (!condition) throw new Exception("Home layout regression: " + key); }
    private static async Task Until(Func<bool> condition) { for (int i = 0; i < 100; i++) { if (condition()) return; await Task.Delay(100); } throw new TimeoutException("Fixture artwork was not displayed."); }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) { var child = VisualTreeHelper.GetChild(root, i); if (child is T found) yield return found; foreach (var nested in Descendants<T>(child)) yield return nested; }
    }
    private static void Save(Window window, string name)
    {
        window.UpdateLayout(); var root = (FrameworkElement)window.Content;
        var bitmap = new RenderTargetBitmap((int)root.ActualWidth, (int)root.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(root);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var file = File.Create(Path.Combine(App.DataDirectory, name)); encoder.Save(file);
    }
    private static byte[] Poster(int width, int height, string name, string color)
    {
        var visual = new DrawingVisual();
        using (var draw = visual.RenderOpen())
        {
            draw.DrawRectangle(PremiumWindow.Brush(color), null, new Rect(0, 0, width, height));
            draw.DrawEllipse(PremiumWindow.Brush("#98ABA9"), null, new Point(width * .68, height * .3), width * .23, width * .23);
            draw.DrawRectangle(PremiumWindow.Brush("#172532"), null, new Rect(0, height * .57, width, height * .43));
            var title = new FormattedText(name, CultureInfo.GetCultureInfo("tr-TR"), FlowDirection.LeftToRight, new Typeface("Segoe UI"), Math.Min(width * .09, height * .15), Brushes.White, 1) { MaxTextWidth = width * .8 };
            draw.DrawText(title, new Point(width * .1, height * .76));
        }
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var data = new MemoryStream(); encoder.Save(data); return data.ToArray();
    }
}

