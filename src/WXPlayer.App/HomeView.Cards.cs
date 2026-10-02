using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed partial class HomeView
{
    private sealed record RecentRemoval(ContentItem Item);
    private Grid Artwork(ContentItem item, double width, double height) => CatalogPosterCard.Poster(this, item, width, height, 480);
    private void BuildHomeRows(IReadOnlyList<HomeShelf> visible)
    {
        HomeShelf? Find(string key) => visible.FirstOrDefault(s => s.Section == key);
        var rows = new List<HomeShelf>();
        void Add(string title, string key, HomeShelf? source, Func<ContentItem, bool>? include = null)
        {
            if (source is null) return;
            var items = source.Page.Items.Where(i => include?.Invoke(i) ?? true).Take(12).ToArray();
            if (items.Length > 0) rows.Add(new(title, key, new WXPlayer.Core.Page(items, items.Length)));
        }
        Add("İzlemeye devam et", "recent", Find("recent"), i => i.Kind != ContentKind.Live);
        Add("Son izlenen kanallar", "recent-live", Find("recent"), i => i.Kind == ContentKind.Live);
        Add("Filmler", "movie", Find("movie")); Add("Diziler", "series", Find("series"));
        Add("Favori filmler ve diziler", "favorites", Find("favorites"), i => i.Kind != ContentKind.Live);
        Add("Favori kanallar", "favorites-live", Find("favorites"), i => i.Kind == ContentKind.Live);
        // Preserve the existing live discovery row, which is absent from the HTML sample.
        Add("Şimdi canlı", "live", Find("live"));
        PresentationShelves = rows;
        foreach (var row in rows) BuildShelf(row);
        if (Search.Text.Trim().Length == 0 && Find("favorites") is { Page.Total: 0 })
        {
            var empty = new Grid { Height = 112, Margin = new Thickness(35.2, 32, 35.2, 0) };
            empty.Children.Add(new System.Windows.Shapes.Rectangle { Stroke = Color("HomeEmptyLine"), StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 3, 3 }, RadiusX = 19.2, RadiusY = 19.2 });
            var label = Text("Henüz favori eklemedin. Bir içeriğin üzerindeki yıldıza dokun.", 14.4, "--mut");
            label.TextAlignment = TextAlignment.Center; label.VerticalAlignment = VerticalAlignment.Center; label.Margin = new Thickness(20); empty.Children.Add(label); _shelves.Children.Add(empty);
        }
    }
    private void BuildShelf(HomeShelf shelf)
    {
        bool channels = shelf.Section is "live" or "recent-live" or "favorites-live";
        double width = channels ? 200 : CatalogPosterCard.PosterWidth;
        var section = new StackPanel { Margin = new Thickness(0, 32, 0, 0), Tag = shelf.Section }; _shelves.Children.Add(section);
        var header = new DockPanel { Margin = new Thickness(35.2, 0, 35.2, 14.4) }; section.Children.Add(header);
        string destination = shelf.Section is "recent-live" ? "recent" : shelf.Section is "favorites-live" ? "favorites" : shelf.Section;
        var all = HomeAction("Tümünü gör →", () => _browse(destination), "CatalogButton");
        all.FontSize = 13.12; all.FontWeight = FontWeights.SemiBold; all.Foreground = Color("--mut"); DockPanel.SetDock(all, Dock.Right); header.Children.Add(all);
        var heading = new StackPanel { Orientation = Orientation.Horizontal };
        var title = Text(shelf.Title, 18.4); title.FontWeight = FontWeights.Bold; heading.Children.Add(title);
        var count = Text(shelf.Page.Items.Count.ToString(), 12.8, "--mut"); count.FontWeight = FontWeights.SemiBold; count.Margin = new Thickness(11.2, 4, 0, 0); heading.Children.Add(count); header.Children.Add(heading);
        var cards = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(35.2, 6.4, 19.2, 16) };
        var scroll = new ScrollViewer { Content = cards, HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, CanContentScroll = false };
        var gallery = new Grid(); gallery.Children.Add(scroll); section.Children.Add(gallery);
        double Step() => Math.Max(width + 16, Math.Floor(scroll.ViewportWidth / (width + 16)) * (width + 16));
        Button Arrow(string icon, string label, Action callback, HorizontalAlignment alignment)
        {
            var button = HomeAction(label, callback, "HomeShelfArrow"); button.Content = new SvgIcon(icon) { Width = 20.8, Height = 20.8 };
            button.ToolTip = label; button.HorizontalAlignment = alignment; button.VerticalAlignment = VerticalAlignment.Center; button.Margin = new Thickness(16, 0, 16, 0); return button;
        }
        var prev = Arrow("chevron-left", shelf.Title + " · Önceki içerikler", () => scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset - Step()), HorizontalAlignment.Left);
        var next = Arrow("chevron-right", shelf.Title + " · Sonraki içerikler", () => scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset + Step()), HorizontalAlignment.Right);
        gallery.Children.Add(prev); gallery.Children.Add(next);
        void UpdateArrows() { next.IsEnabled = scroll.HorizontalOffset < scroll.ScrollableWidth - 1; prev.IsEnabled = scroll.HorizontalOffset > 1; }
        scroll.ScrollChanged += (_, _) => UpdateArrows(); scroll.Loaded += (_, _) => UpdateArrows();
        foreach (var item in shelf.Page.Items)
        {
            UIElement card = channels ? ChannelCard(item) : CatalogPosterCard.Build(this, item, _play, _favorite, home: true);
            if (shelf.Section is "recent" or "recent-live" && _removeRecent is not null) card = RecentCard(card, item);
            cards.Children.Add(card);
        }
    }
    private Grid ChannelCard(ContentItem item)
    {
        var wrapper = new Grid { Width = 200, Margin = new Thickness(0, 0, 16, 0), DataContext = item, VerticalAlignment = VerticalAlignment.Top };
        var content = new StackPanel();
        var artwork = new Grid { Width = 200, Height = 125, Background = Color("--p2"), Clip = new RectangleGeometry(new Rect(0, 0, 200, 125), 16, 16) };
        var logo = new ChannelLogo { Url = item.Logo, Initials = "", DecodeWidth = 480, ImageStretch = Stretch.Uniform, ImagePadding = new Thickness(24) }; artwork.Children.Add(logo);
        var fallback = Text(HomeDisplayFormatter.Clean(item.Name), 24); fallback.FontWeight = FontWeights.ExtraBold;
        fallback.Margin = new Thickness(12.8); fallback.TextAlignment = TextAlignment.Center; fallback.VerticalAlignment = VerticalAlignment.Center; fallback.IsHitTestVisible = false; artwork.Children.Add(fallback);
        var image = logo.Children.OfType<Image>().Single();
        var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(Image.SourceProperty, typeof(Image));
        void UpdateFallback(object? sender, EventArgs e) => fallback.Visibility = logo.HasImage ? Visibility.Collapsed : Visibility.Visible;
        bool observing = false;
        artwork.Loaded += (_, _) => { if (!observing) { descriptor.AddValueChanged(image, UpdateFallback); observing = true; } UpdateFallback(null, EventArgs.Empty); };
        artwork.Unloaded += (_, _) => { if (observing) { descriptor.RemoveValueChanged(image, UpdateFallback); observing = false; } };
        var live = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(9.6) };
        live.Children.Add(new System.Windows.Shapes.Ellipse { Width = 6.08, Height = 6.08, Fill = Color("--live"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5.6, 0) });
        var badge = Text("CANLI", 9.6, "HomeLiveText"); badge.FontWeight = FontWeights.ExtraBold; live.Children.Add(badge); artwork.Children.Add(live); content.Children.Add(artwork);
        var title = Text(HomeDisplayFormatter.Clean(item.Name), 14.72); title.FontWeight = FontWeights.SemiBold; title.Margin = new Thickness(0, 11.2, 0, 0); title.TextWrapping = TextWrapping.NoWrap; title.TextTrimming = TextTrimming.CharacterEllipsis; content.Children.Add(title);
        var metadata = new DockPanel { Margin = new Thickness(0, 2.4, 0, 0) };
        string quality = HomeDisplayFormatter.Quality(item.Name);
        if (quality.Length > 0) { var q = new Border { Background = Color("CatalogButtonGlass"), CornerRadius = new CornerRadius(4.8), Padding = new Thickness(5.6, .8, 5.6, .8), Child = Text(quality, 9.92, "CatalogMetadata") }; DockPanel.SetDock(q, Dock.Right); metadata.Children.Add(q); }
        var group = Text(HomeDisplayFormatter.Clean(item.Category), 12.48, "--mut"); group.TextWrapping = TextWrapping.NoWrap; group.TextTrimming = TextTrimming.CharacterEllipsis; metadata.Children.Add(group); content.Children.Add(metadata);
        var play = new Button { Style = (Style)FindResource("HomeChannelCard"), Content = content, Tag = item, ToolTip = item.Name }; play.Click += (_, _) => _play(item);
        System.Windows.Automation.AutomationProperties.SetName(play, "İzle · " + item.Name); wrapper.Children.Add(play);
        var favorite = new Button { Style = (Style)FindResource("CatalogFavorite"), Tag = new HomeFavoriteAction(item), Content = new SvgIcon(item.IsFavorite ? "catalog-star-filled" : "catalog-star") { Width = 16, Height = 16 }, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 8, 8, 0), ToolTip = "Favoriyi değiştir" };
        System.Windows.Automation.AutomationProperties.SetName(favorite, "Favoriyi değiştir · " + item.Name); favorite.Click += (_, e) => { e.Handled = true; _favorite(item); }; wrapper.Children.Add(favorite);
        return wrapper;
    }
    private static ScrollViewer? ShelfScroll(DependencyObject root) => VisualChildren<ScrollViewer>(root).FirstOrDefault();
    private static IEnumerable<T> VisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) { var child = VisualTreeHelper.GetChild(root, i); if (child is T found) yield return found; foreach (var nested in VisualChildren<T>(child)) yield return nested; }
    }
    private Grid RecentCard(UIElement play, ContentItem item)
    {
        // Sibling actions prevent a remove click from triggering playback. The overlay
        // takes no layout space, so recent cards retain exactly the existing geometry.
        var wrapper=new Grid { VerticalAlignment=VerticalAlignment.Top };
        wrapper.Children.Add(play);
        var remove=new Button
        {
            Style=(Style)FindResource("CatalogIconButton"),
            Content=new SvgIcon("close") { Width=16,Height=16 },Tag=new RecentRemoval(item),
            Width=32,Height=32,MinHeight=32,Padding=new Thickness(8),Margin=new Thickness(0,8,64,0),
            HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,
            Background=(Brush)FindResource("SurfaceElevatedBrush"),Foreground=(Brush)FindResource("TextPrimaryBrush"),
            BorderBrush=(Brush)FindResource("BorderSubtleBrush"),BorderThickness=new Thickness(1),
            ToolTip="Son izlenenlerden kaldır",Opacity=0,IsHitTestVisible=false
        };
        System.Windows.Automation.AutomationProperties.SetName(remove,"Son izlenenlerden kaldır · "+item.Name);
        wrapper.Children.Add(remove);
        void Reveal()
        {
            bool visible=wrapper.IsMouseOver||wrapper.IsKeyboardFocusWithin;
            remove.Opacity=visible?1:0;remove.IsHitTestVisible=visible;
        }
        wrapper.MouseEnter+=(_,_)=>Reveal();wrapper.MouseLeave+=(_,_)=>Reveal();
        wrapper.IsKeyboardFocusWithinChanged+=(_,_)=>Reveal();
        bool removing=false;
        remove.Click+=async(_,e)=>
        {
            // Keep keyboard focus available to DisplayReady while blocking repeat clicks.
            e.Handled=true;if(removing)return;removing=true;
            try { await _removeRecent!(item); }
            finally { removing=false; }
        };
        return wrapper;
    }
    private IEnumerable<Button> ShelfButtons() => VisualChildren<Button>(_shelves);
    private static Button Arrow(string icon, string label, Action action)
    {
        var button = Action("", action); button.Content = new SvgIcon(icon) { Width = 14, Height = 14 };
        button.Width = 34; button.MinHeight = 34; button.Padding = new Thickness(8); button.Margin = new Thickness(6, 0, 0, 0); button.ToolTip = label;
        System.Windows.Automation.AutomationProperties.SetName(button, label); return button;
    }
}





