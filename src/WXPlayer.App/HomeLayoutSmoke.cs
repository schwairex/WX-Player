using System.Globalization;
using System.Diagnostics;
using System.Windows.Input;
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
        await using var portrait = new SmokeHttpServer(Poster(600, 900, "UZAK KIYI", "#27495B"), delayMs: 650);
        await using var landscape = new SmokeHttpServer(Poster(600, 900, "GECE YOLU", "#564D66"), delayMs: 650);
        await using var logo = new SmokeHttpServer(Poster(400, 200, "WX TV", "#24392F"), delayMs: 650);
        await CacheChecks(results);
        var movies = Enumerable.Range(0, 8).Select(i => new ContentItem
        {
            Id = "layout-movie-" + i, SourceId = "layout", Kind = ContentKind.Movie,
            Name = new[] { "Uzak Kıyı (2024)", "Gece Yolu (2023)", "Bir Şehrin Hikâyesi: Çok Uzun Bir Yolculuğun Ardından Gelen Sabah (2025)", "Sessiz Günler" }[i % 4],
            Category = "Dram · Sinema", Logo = i == 7 ? "" : i == 3 ? "http://127.0.0.1:1/missing.png" : i % 2 == 0 ? portrait.Url : landscape.Url,
            Progress = i == 0 ? new WatchProgress("layout-movie-0", "layout", "", "Uzak Kıyı", 1620000, 6600000, false, 1) : null
        }).ToArray();
        var shows = Enumerable.Range(0, 6).Select(i => new ContentItem { Id = "layout-series-" + i, SourceId = "layout", Kind = ContentKind.Series, Name = i == 0 ? "Kayıp Şehir" : "Kıyı Hikâyeleri · " + i, Category = "Dram · Dizi", Logo = i % 2 == 0 ? landscape.Url : portrait.Url }).ToArray();
        var live = new ContentItem { Id = "layout-live", SourceId = "layout", Kind = ContentKind.Live, Name = "WX TV Haber HD", Category = "Ulusal", Logo = logo.Url };
        ContentItem[] mixed = [live, movies[0], shows[0], movies[2], movies[3], movies[4], movies[5], shows[1], live with { Id = "layout-live2", Name = "WX Kültür" }];
        HomeShelf Shelf(string title, string section, ContentItem[] items) => new(title, section, new WXPlayer.Core.Page(items, items.Length));
        var shelves = new[] { Shelf("Son izlenenler", "recent", mixed), Shelf("Favorilerin", "favorites", mixed), Shelf("Filmler", "movie", movies), Shelf("Diziler", "series", shows), Shelf("Şimdi canlı", "live", [live]) };
        string? played = null, browsed = null; bool retried = false;
        var home = new HomeView(i => played = i.Id, _ => { }, s => browsed = s, () => { }, () => { });
        var original = window.HomeHost.Content; double width = window.Width, height = window.Height;
        try
        {
            window.Width = 1440; window.Height = 960; window.HomeHost.Content = home;
            var rendering = home.RenderAsync("Örnek kütüphane", shelves, "", movies[0]); window.UpdateLayout();
            Check("home153LoadingFeatureBounded", Feature(home).ActualHeight <= 352, results);
            await rendering; window.UpdateLayout();
            var firstFeature = home.Featured?.Id;
            Descendants<Button>(Feature(home)).First(b => Equals(b.ToolTip, "Sonraki öne çıkan içerik")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check("home173FeaturedCarouselAdvances", home.Featured?.Id != firstFeature, results);
            Descendants<Button>(Feature(home)).First(b => Equals(b.ToolTip, "Önceki öne çıkan içerik")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check("home173FeaturedCarouselRestores", home.Featured?.Id == firstFeature, results);
            var before = Cards(home).Select(b => b.RenderSize).ToArray();
            await Until(() => Descendants<ChannelLogo>(home).Count(l => l.HasImage) >= 20);
            await Task.Delay(150); window.UpdateLayout();
            Check("home152AsyncArtworkDoesNotShiftLayout", before.SequenceEqual(Cards(home).Select(b => b.RenderSize)), results);
            CheckGeometry(home, results, "Desktop");
            Check("home153MissingAndBrokenArtworkExcluded", home.Items.All(i => ArtworkCache.HasAddress(i.Logo)) && home.Items.All(i => i.Id != movies[3].Id && i.Id != movies[7].Id), results);
            Check("home153PostersFillTiles", Descendants<ChannelLogo>(home).Where(l => l.ImagePadding.Left == 0).All(l => l.ImageStretch == Stretch.UniformToFill), results);
            await WheelChecks(home, results);
            Save(window, "WX-Player-1.5.3-home.png");
            home.ScrollToVerticalOffset(300); await Task.Delay(80); Save(window, "WX-Player-1.5.3-mixed-shelves.png");
            var favorite = Descendants<StackPanel>(home).Single(p => Equals(p.Tag, "favorites"));
            var last = Cards(favorite).Last(); last.BringIntoView(); await Task.Delay(80);
            Check("home152KeyboardNavigationRevealsLastCard", Descendants<ScrollViewer>(favorite).Single().HorizontalOffset > 0, results);
            var chosen = Cards(favorite).First(); window.Activate(); chosen.Focus();
            await Until(()=>chosen.IsKeyboardFocused); window.UpdateLayout();
            var chrome = (Border)chosen.Template.FindName("Chrome", chosen);
            Check("home152CardFocusVisible", chosen.IsKeyboardFocused && chosen.BorderThickness.Left >= 1 && chrome.BorderBrush.ToString() == "#FFC1EC8B", results);
            chosen.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check("home152CardAction", played == ((ContentItem)chosen.Tag).Id, results);
            var all = Descendants<Button>(favorite).First(b => Equals(b.Content, "Tümünü gör")); all.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check("home152ShelfBrowseAction", browsed == "favorites", results);
            home.ScrollToVerticalOffset(690); await Task.Delay(80); Save(window, "WX-Player-1.5.3-posters.png");
            window.Width = 900; window.Height = 760;
            await home.RenderAsync("Çok uzun kaynak adı ile taşma kontrolü · Örnek kütüphane", shelves, "", movies[2]);
            await Task.Delay(250); window.UpdateLayout(); home.ScrollToTop();
            CheckGeometry(home, results, "Compact"); results["home152CompactWidth"]=home.ActualWidth; Save(window, "WX-Player-1.5.3-compact.png");
            Check("home152CompactSearchOwnRow", Grid.GetRow((UIElement)home.Search.Parent) == 1, results);
            Check("home152HeroActionsWithinBounds", Descendants<Button>(Feature(home)).All(b => b.TranslatePoint(new Point(0, b.ActualHeight), Feature(home)).Y <= Feature(home).ActualHeight), results);
            Save(window, "WX-Player-1.5.3-compact.png");
            window.Width = 1920; window.Height = 1080; await Task.Delay(100); window.UpdateLayout();
            CheckGeometry(home, results, "Wide"); Save(window, "WX-Player-1.5.3-wide.png");
            window.Width = 1440; window.Height = 960;
            await RecentAndDiscoveryChecks(window,results,movies[0],shows[0]);
            await CatalogFixtureChecks(window, results, movies, shows);
            home.Loading(); window.UpdateLayout(); double loadingHeight = Feature(home).ActualHeight; Save(window, "WX-Player-1.5.3-loading.png");
            home.Error(() => retried = true); window.UpdateLayout();
            Descendants<Button>(Feature(home)).Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check("home152ErrorRetry", retried, results); Save(window, "WX-Player-1.5.3-error.png");
            home.Render(null, [], ""); window.UpdateLayout(); Check("home152EmptyLibrary", home.Empty && home.Featured is null, results); Save(window, "WX-Player-1.5.3-empty.png");
            home.Search.Text = "Aranan içerik"; home.Render("Örnek", [], home.Search.Text); window.UpdateLayout();
            Descendants<Button>(Feature(home)).Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check("home152EmptySearchClear", home.Search.Text.Length == 0, results);
            Check("home152StableStateHeight", Feature(home).ActualHeight == loadingHeight, results);
        }
        finally { window.HomeHost.Content = original; window.Width = width; window.Height = height; }
    }
    private static async Task CatalogFixtureChecks(MainWindow window, Dictionary<string, object> results,
        ContentItem[] movies, ContentItem[] shows)
    {
        var original = window.CatalogHost.Content;
        var homeVisibility = window.HomeHost.Visibility;
        var catalogVisibility = window.CatalogHost.Visibility;
        string originalTitle = window.PageTitle.Text;
        var catalog = new CatalogView(_ => { }, _ => { }, () => { });
        var filmItems = Enumerable.Range(0, 20).Select(i => movies[i % movies.Length] with
        {
            Id = "catalog-film-" + i,
            Name = movies[i % movies.Length].Name + " (2024)"
        }).ToArray();
        var showItems = Enumerable.Range(0, 18).Select(i => shows[i % shows.Length] with
        {
            Id = "catalog-series-" + i,
            Name = shows[i % shows.Length].Name + " (2025)"
        }).ToArray();
        try
        {
            window.HomeHost.Visibility = Visibility.Collapsed;
            window.CatalogHost.Visibility = Visibility.Visible;
            window.CatalogHost.Content = catalog;
            WXPlayer.Core.Page Fetch(ContentItem[] items, int offset, int limit) =>
                new(items.Skip(offset).Take(limit).ToArray(), items.Length);
            void Render(bool series, ContentItem[] items)
            {
                catalog.Render(series, "Örnek kütüphane", "",
                    [new CatalogShelf(series ? "Tüm diziler" : "Tüm filmler", null, Fetch(items, 0, 12)),
                     new CatalogShelf(series ? "Dram dizileri" : "Dram filmleri", "Dram", Fetch(items, 0, 8))],
                    (_, offset, limit, _) => Task.FromResult(Fetch(items, offset, limit)), CancellationToken.None);
                window.PageTitle.Text = series ? "Diziler" : "Filmler";
                window.UpdateLayout();
            }
            Render(false, filmItems);
            await Until(() => Descendants<ChannelLogo>(catalog).Count(l => l.HasImage) >= 10);
            window.UpdateLayout();
            Check("catalog171PosterTiles", Descendants<Grid>(catalog).Count(g => g.Width == 164 &&
                g.Children.OfType<ChannelLogo>().Any(l => l.HasImage)) >= 10, results);
            Check("catalog171MovieYear", Descendants<TextBlock>(catalog).Any(t => t.Text == "2024"), results);
            Save(window, "WX-Player-1.7.1-movies-fixture.png");
            var next = Descendants<Button>(catalog).First(b => Equals(b.ToolTip, "Tüm filmler · ileri"));
            next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.UpdateLayout();
            Check("catalog171CarouselScroll", Descendants<ScrollViewer>(catalog).Any(s => s.HorizontalOffset > 0), results);
            Render(true, showItems);
            await Until(() => Descendants<ChannelLogo>(catalog).Count(l => l.HasImage) >= 10);
            window.UpdateLayout();
            Check("catalog171SeriesPosterTiles", Descendants<Grid>(catalog).Count(g => g.Width == 164 &&
                g.Children.OfType<ChannelLogo>().Any(l => l.HasImage)) >= 10, results);
            Save(window, "WX-Player-1.7.1-series-fixture.png");
        }
        finally
        {
            window.CatalogHost.Content = original;
            window.CatalogHost.Visibility = catalogVisibility;
            window.HomeHost.Visibility = homeVisibility;
            window.PageTitle.Text = originalTitle;
        }
    }
    private static async Task WheelChecks(HomeView home, Dictionary<string, object> results)
    {
        home.ScrollToTop(); home.UpdateLayout();
        var button = Cards(home).First();
        void Wheel(int delta) => button.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, delta) { RoutedEvent = UIElement.PreviewMouseWheelEvent });
        Wheel(-120); await Task.Delay(50); home.UpdateLayout();
        Check("home153WheelOverCardsScrollsPage", home.VerticalOffset > 0, results);
        for (int i = 0; i < 60; i++) { Wheel(-120); home.UpdateLayout(); }
        Check("home153WheelReachesBottom", Math.Abs(home.VerticalOffset - home.ScrollableHeight) < 1, results);
        for (int i = 0; i < 60; i++) { Wheel(120); home.UpdateLayout(); }
        Check("home153WheelReachesTop", home.VerticalOffset == 0, results);
    }
    private static async Task RecentAndDiscoveryChecks(MainWindow window,Dictionary<string,object> results,ContentItem movie,ContentItem show)
    {
        var films=Enumerable.Range(0,60).Select(i=>movie with{Id="discovery-film-"+i,Name="Film "+i}).ToArray();
        var shows=Enumerable.Range(0,60).Select(i=>show with{Id="discovery-show-"+i,Name="Dizi "+i}).ToArray();
        ContentItem[] recent=[films[0],films[1]];
        HomeShelf Shelf(string title,string key,ContentItem[] items)=>new(title,key,new WXPlayer.Core.Page(items,items.Length));
        HomeShelf[] Shelves()=>[Shelf("Son izlenenler","recent",recent),Shelf("Favorilerin","favorites",[films[0]]),Shelf("Filmler","movie",films),Shelf("Diziler","series",shows)];
        int played=0;HomeView home=null!;
        home=new HomeView(_=>played++,_=>{},_=>{},()=>{},()=>{},async item=>
        {
            recent=recent.Where(i=>i.Id!=item.Id).ToArray();
            await home.RenderAsync("Test kütüphanesi",Shelves(),"",films[0]);
        });
        var original=window.HomeHost.Content;
        try
        {
            window.HomeHost.Content=home;await home.RenderAsync("Test kütüphanesi",Shelves(),"",films[0]);window.UpdateLayout();
            StackPanel Section(string key)=>Descendants<StackPanel>(home).Single(p=>Equals(p.Tag,key));
            Button[] RemoveButtons()=>Descendants<Button>(Section("recent")).Where(b=>System.Windows.Automation.AutomationProperties.GetName(b).StartsWith("Son izlenenlerden kaldır · ")).ToArray();
            Check("home161Discovery48MoviesAndSeries",Cards(Section("movie")).Count()==48&&Cards(Section("series")).Count()==48,results);
            foreach(var key in new[]{"movie","series"})
            {
                var section=Section(key);var scroll=Descendants<ScrollViewer>(section).Single();
                Descendants<Button>(section).Single(b=>b.ToolTip is string tip&&tip.EndsWith("Sonraki içerikler")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(60);window.UpdateLayout();Check("home161HorizontalPaging"+key,scroll.HorizontalOffset>0,results);
            }
            var card=Cards(Section("recent")).First();var size=card.RenderSize;
            var remove=RemoveButtons()[0];card.Focus();window.UpdateLayout();
            Check("home161RemoveVisibleOnKeyboardFocus",remove.Opacity==1&&remove.IsHitTestVisible,results);
            Check("home161RemoveDoesNotResizeCard",card.RenderSize==size&&size==Cards(Section("favorites")).First().RenderSize,results);
            remove.Focus();home.ScrollToVerticalOffset(300);await Task.Delay(60);
            Check("home161RemoveAcceptsKeyboardFocus",remove.IsKeyboardFocused,results);
            Save(window,"WX-Player-1.6.1-recent-remove.png");
            remove.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Until(()=>Cards(Section("recent")).Count()==1);window.UpdateLayout();
            Check("home161RemovalDoesNotPlayOrChangeFavorites",played==0&&Cards(Section("favorites")).Single().Tag is ContentItem i&&i.Id==films[0].Id,results);
            Check("home161RemovalRestoresKeyboardFocus",RemoveButtons()[0].IsKeyboardFocused,results);
            RemoveButtons()[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Until(()=>!Descendants<StackPanel>(home).Any(p=>Equals(p.Tag,"recent")));
            Check("home161LastRecentRemovalKeepsOtherShelves",Cards(Section("movie")).Count()==48&&Cards(Section("favorites")).Count()==1,results);
        }
        finally { window.HomeHost.Content=original; }
    }
    private static async Task CacheChecks(Dictionary<string, object> results)
    {
        await using var server = new SmokeHttpServer(Poster(600, 1800, "ÖNBELLEK", "#27495B"), delayMs: 350);
        var clock = Stopwatch.StartNew();
        var images = await Task.WhenAll(Enumerable.Range(0, 18).Select(i => ArtworkCache.GetAsync(server.Url, new[] {96, 480, 960}[i % 3])));
        results["artwork153ColdMs"] = clock.ElapsedMilliseconds;
        Check("artwork153SingleDownloadAcrossSizes", server.Connections == 1 && images.All(i => i is not null), results);
        Check("artwork153DecodedSizeBounded", images.All(i => i!.PixelHeight <= 1024 && i.PixelWidth <= 960), results);
        await server.DisposeAsync(); ArtworkCache.ClearMemoryForTest(); clock.Restart();
        var cached = await ArtworkCache.GetAsync(server.Url, 480);
        results["artwork153DiskMs"] = clock.ElapsedMilliseconds;
        Check("artwork153DiskCacheWorksOffline", cached is not null && server.Connections == 1, results);
        var memory = await ArtworkCache.GetAsync(server.Url, 480);
        Check("artwork153MemoryReusesFrozenFrame", ReferenceEquals(memory, cached) && cached!.IsFrozen && ArtworkCache.MemoryBytes <= 64L * 1024 * 1024, results);
        await using var many = new SmokeHttpServer(Poster(96, 144, "WX", "#27495B"));
        var first = await ArtworkCache.GetAsync(many.Url + "?item=0", 96);
        await Task.WhenAll(Enumerable.Range(1, 112).Select(i => ArtworkCache.GetAsync(many.Url + "?item=" + i, 96)));
        Check("artwork153CacheSurvivesMoreThan96Images", ArtworkCache.TryGet(many.Url + "?item=0", 96, out var stillCached) && ReferenceEquals(first, stillCached), results);
        await using var invalid = new SmokeHttpServer(System.Text.Encoding.UTF8.GetBytes("not an image"));
        Check("artwork153BadImageRejected", await ArtworkCache.GetAsync(invalid.Url, 480) is null, results);
        _ = await ArtworkCache.GetAsync(invalid.Url, 480);
        Check("artwork153FailedImageDoesNotRetryImmediately", invalid.Connections == 1, results);
    }
    private static Grid Feature(HomeView home) => (Grid)((StackPanel)home.Content).Children[2];
    private static IEnumerable<Button> Cards(DependencyObject root) => Descendants<Button>(root).Where(b => b.Tag is ContentItem);
    private static void CheckGeometry(HomeView home, Dictionary<string, object> results, string suffix)
    {
        Check("home152FeatureBounded" + suffix, Feature(home).ActualHeight is > 0 and <= 352 && Feature(home).ActualWidth <= home.ActualWidth, results);
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




