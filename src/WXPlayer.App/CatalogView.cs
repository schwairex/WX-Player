using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed record CatalogShelf(string Title, string? Category, WXPlayer.Core.Page Page);

/// <summary>Artwork-first movie and series browsing. All shelves read the user's library.</summary>
internal sealed class CatalogView : ScrollViewer
{
    private const int PageSize = 36;
    private const double PosterWidth = 164;
    private const double PosterHeight = 246;
    private readonly StackPanel _body = new(), _shelves = new();
    private readonly TextBlock _heading, _description, _searchHint;
    private readonly Button _resume;
    private readonly Action<ContentItem> _open, _favorite;
    private int _renderVersion;

    internal TextBox Search { get; } = new() { Width = 280, MinHeight = 42, Padding = new Thickness(13, 8, 13, 8) };
    internal IReadOnlyList<CatalogShelf> Shelves { get; private set; } = [];

    private static Brush Color(string key) => PremiumWindow.Brush(key);
    private static TextBlock Text(string value, int size, string color = "TextPrimaryBrush") => PremiumWindow.Text(value, size, color);

    internal CatalogView(Action<ContentItem> open, Action<ContentItem> favorite, Action resume)
    {
        _open = open; _favorite = favorite;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        Content = _body;
        _body.Margin = new Thickness(0, 0, 12, 0);

        var toolbar = new DockPanel { Margin = new Thickness(0, 0, 0, 25), LastChildFill = true };
        _body.Children.Add(toolbar);
        var searchFrame = new Grid { Width = 280, Height = 42 };
        DockPanel.SetDock(searchFrame, Dock.Right);
        toolbar.Children.Add(searchFrame);
        searchFrame.Children.Add(Search);
        _searchHint = Text("Kütüphanende ara…", 12, "TextMutedBrush");
        _searchHint.VerticalAlignment = VerticalAlignment.Center;
        _searchHint.Margin = new Thickness(14, 0, 0, 0);
        _searchHint.IsHitTestVisible = false;
        searchFrame.Children.Add(_searchHint);
        Search.TextChanged += (_, _) => _searchHint.Visibility = Search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        System.Windows.Automation.AutomationProperties.SetName(Search, "Film ve dizilerde ara");

        var labels = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        toolbar.Children.Add(labels);
        _heading = Text("KÜTÜPHANEN", 11, "TextMutedBrush"); _heading.FontWeight = FontWeights.SemiBold;
        labels.Children.Add(_heading);
        _description = Text("Kütüphanenizdeki içerikler", 12, "TextMutedBrush");
        _description.Margin = new Thickness(0, 5, 0, 0);
        labels.Children.Add(_description);

        _resume = PremiumWindow.Action("İzlemeye dön", resume);
        _resume.HorizontalAlignment = HorizontalAlignment.Left;
        _resume.Margin = new Thickness(0, 0, 0, 22);
        _resume.Visibility = Visibility.Collapsed;
        _body.Children.Add(_resume);
        _body.Children.Add(_shelves);
        PreviewMouseWheel += CatalogWheel;
        SizeChanged += (_, _) =>
        {
            bool compact = ActualWidth < 730;
            DockPanel.SetDock(searchFrame, compact ? Dock.Bottom : Dock.Right);
            searchFrame.Margin = compact ? new Thickness(0, 14, 0, 0) : new Thickness(16, 0, 0, 0);
            searchFrame.Width = compact ? Math.Max(200, ActualWidth - 28) : 280;
            Search.Width = searchFrame.Width;
        };
    }

    internal void NowPlaying(string? title)
    {
        _resume.Content = "İzlemeye dön · " + title;
        _resume.ToolTip = title;
        _resume.Visibility = string.IsNullOrEmpty(title) ? Visibility.Collapsed : Visibility.Visible;
    }

    internal void Loading(bool series)
    {
        _renderVersion++;
        _heading.Text = "KÜTÜPHANEN";
        _description.Text = "Kütüphaneniz hazırlanıyor…";
        _shelves.Children.Clear();
        var progress = Text("Afişler ve kategoriler yükleniyor…", 13, "TextMutedBrush");
        progress.Margin = new Thickness(0, 30, 0, 0);
        _shelves.Children.Add(progress);
    }

    internal void Render(bool series, string source, string search, IReadOnlyList<CatalogShelf> shelves,
        Func<string?, int, int, CancellationToken, Task<WXPlayer.Core.Page>> fetch, CancellationToken token)
    {
        int version = ++_renderVersion;
        Shelves = shelves;
        _heading.Text = "KÜTÜPHANEN";
        int total = shelves.FirstOrDefault()?.Page.Total ?? 0;
        _description.Text = source + "  ·  " + total.ToString("N0") + (series ? " dizi" : " film");
        _shelves.Children.Clear();
        ScrollToTop();
        if (total == 0)
        {
            var empty = new StackPanel { Margin = new Thickness(0, 80, 0, 0), HorizontalAlignment = HorizontalAlignment.Center };
            var title = Text(search.Length > 0 ? "Eşleşen içerik bulunamadı" : "Bu bölüm henüz boş", 19);
            title.FontWeight = FontWeights.SemiBold; title.HorizontalAlignment = HorizontalAlignment.Center;
            empty.Children.Add(title);
            var detail = Text(search.Length > 0 ? "Başka bir adla aramayı deneyin." : "Kütüphanenize film veya dizi içeren bir kaynak ekleyin.", 12, "TextMutedBrush");
            detail.Margin = new Thickness(0, 10, 0, 0); detail.HorizontalAlignment = HorizontalAlignment.Center;
            empty.Children.Add(detail); _shelves.Children.Add(empty);
            return;
        }
        foreach (var shelf in shelves.Where(s => s.Page.Items.Count > 0))
            BuildShelf(shelf, fetch, token, version);
    }

    private void BuildShelf(CatalogShelf shelf,
        Func<string?, int, int, CancellationToken, Task<WXPlayer.Core.Page>> fetch, CancellationToken token, int version)
    {
        var section = new StackPanel { Margin = new Thickness(0, 0, 0, 31) };
        _shelves.Children.Add(section);
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
        section.Children.Add(header);
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        DockPanel.SetDock(actions, Dock.Right); header.Children.Add(actions);
        var previous = Arrow("chevron-left", shelf.Title + " · geri");
        var next = Arrow("chevron-right", shelf.Title + " · ileri");
        actions.Children.Add(previous); actions.Children.Add(next);
        var heading = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var title = Text(shelf.Title, 18); title.FontWeight = FontWeights.SemiBold;
        heading.Children.Add(title);
        var count = Text("  " + shelf.Page.Total.ToString("N0"), 11, "TextMutedBrush");
        count.VerticalAlignment = VerticalAlignment.Bottom; count.Margin = new Thickness(5, 0, 0, 3);
        heading.Children.Add(count); header.Children.Add(heading);

        var cards = new StackPanel { Orientation = Orientation.Horizontal };
        var scroll = new ScrollViewer
        {
            Content = cards, HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, CanContentScroll = false
        };
        section.Children.Add(scroll);
        int loaded = 0;
        bool loading = false;
        void AddCards(IEnumerable<ContentItem> items)
        {
            foreach (var item in items) cards.Children.Add(Card(item));
        }
        AddCards(shelf.Page.Items); loaded = shelf.Page.Items.Count;
        void UpdateArrows()
        {
            previous.IsEnabled = scroll.HorizontalOffset > 1;
            next.IsEnabled = scroll.HorizontalOffset < scroll.ScrollableWidth - 1 || loaded < shelf.Page.Total;
        }
        async Task LoadMore()
        {
            if (loading || loaded >= shelf.Page.Total || token.IsCancellationRequested || version != _renderVersion) return;
            loading = true;
            try
            {
                var page = await fetch(shelf.Category, loaded, PageSize, token);
                if (token.IsCancellationRequested || version != _renderVersion) return;
                AddCards(page.Items);
                loaded += page.Items.Count;
                if (page.Items.Count == 0) loaded = page.Total;
                UpdateArrows();
            }
            catch (OperationCanceledException) { }
            catch
            {
                // A failed page stays available for the next navigation attempt.
                _description.Text = "Bazı içerikler yüklenemedi. Kaydırarak yeniden deneyin.";
            }
            finally { loading = false; }
        }
        previous.Click += (_, _) => scroll.ScrollToHorizontalOffset(Math.Max(0, scroll.HorizontalOffset - Math.Max(PosterWidth + 14, scroll.ViewportWidth * .8)));
        next.Click += async (_, _) =>
        {
            if (scroll.ScrollableWidth - scroll.HorizontalOffset < PosterWidth * 2) await LoadMore();
            scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset + Math.Max(PosterWidth + 14, scroll.ViewportWidth * .8));
        };
        scroll.ScrollChanged += async (_, _) =>
        {
            UpdateArrows();
            if (scroll.HorizontalOffset > 0 && scroll.ScrollableWidth - scroll.HorizontalOffset < PosterWidth * 2)
                await LoadMore();
        };
        scroll.Loaded += (_, _) => UpdateArrows();
    }

    private Grid Card(ContentItem item)
    {
        var wrapper = new Grid { Width = PosterWidth, Margin = new Thickness(0, 0, 14, 0), VerticalAlignment = VerticalAlignment.Top };
        var content = new StackPanel();
        var poster = new Grid { Width = PosterWidth, Height = PosterHeight, Background = Color("SurfaceElevatedBrush"), ClipToBounds = true };
        poster.SizeChanged += (_, _) => poster.Clip = new RectangleGeometry(new Rect(0, 0, poster.ActualWidth, poster.ActualHeight), 9, 9);
        var art = new ChannelLogo
        {
            DataContext = item, Url = item.Logo, Initials = item.Initials, DecodeWidth = 400,
            ImageStretch = Stretch.UniformToFill, ImagePadding = new Thickness(0)
        };
        var scale = new ScaleTransform(1, 1);
        art.RenderTransform = scale; art.RenderTransformOrigin = new Point(.5, .5);
        poster.Children.Add(art); content.Children.Add(poster);
        var name = Text(item.Name, 13); name.FontWeight = FontWeights.SemiBold;
        name.Margin = new Thickness(0, 9, 0, 0); name.TextTrimming = TextTrimming.CharacterEllipsis;
        name.TextWrapping = TextWrapping.NoWrap; name.ToolTip = item.Name;
        content.Children.Add(name);
        string year = Regex.Match(item.Name, @"(?<!\d)(?:19|20)\d{2}(?!\d)").Value;
        var meta = Text(year.Length > 0 ? year : item.Category, 11, "TextMutedBrush");
        meta.Margin = new Thickness(0, 5, 0, 0); meta.TextTrimming = TextTrimming.CharacterEllipsis;
        content.Children.Add(meta);
        var open = new Button
        {
            Content = content, Padding = new Thickness(0), Background = Brushes.Transparent,
            BorderThickness = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Left,
            VerticalContentAlignment = VerticalAlignment.Top, ToolTip = item.Name
        };
        System.Windows.Automation.AutomationProperties.SetName(open,
            (item.Kind == ContentKind.Series ? "Bölümleri aç · " : "Filmi izle · ") + item.Name);
        open.Click += (_, _) => _open(item);
        wrapper.Children.Add(open);

        var favoriteIcon = new SvgIcon { Width = 16, Height = 16, Foreground = Color("AccentPrimaryBrush") };
        BindingOperations.SetBinding(favoriteIcon, SvgIcon.IconProperty,
            new Binding(nameof(ContentItem.IsFavorite)) { Source = item, Converter = (IValueConverter)Application.Current.FindResource("FavoriteIcon") });
        var favorite = new Button
        {
            Content = favoriteIcon, Width = 34, Height = 34, MinHeight = 34,
            Padding = new Thickness(7), HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 8, 8, 0),
            Background = Color("SurfaceBrush"), BorderBrush = Color("BorderSubtleBrush"),
            BorderThickness = new Thickness(1), Opacity = 0, IsHitTestVisible = false,
            ToolTip = "Favoriyi değiştir"
        };
        System.Windows.Automation.AutomationProperties.SetName(favorite, "Favoriyi değiştir · " + item.Name);
        favorite.Click += (_, e) => { e.Handled = true; _favorite(item); };
        wrapper.Children.Add(favorite);
        void Reveal(bool show)
        {
            favorite.Opacity = show ? 1 : 0;
            favorite.IsHitTestVisible = show;
            double target = show ? 1.025 : 1;
            var duration = TimeSpan.FromMilliseconds(150);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(target, duration));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(target, duration));
        }
        wrapper.MouseEnter += (_, _) => Reveal(true);
        wrapper.MouseLeave += (_, _) => Reveal(wrapper.IsKeyboardFocusWithin);
        wrapper.IsKeyboardFocusWithinChanged += (_, _) => Reveal(wrapper.IsMouseOver || wrapper.IsKeyboardFocusWithin);
        return wrapper;
    }

    private static Button Arrow(string icon, string label)
    {
        var button = new Button
        {
            Content = new SvgIcon(icon) { Width = 15, Height = 15 },
            Width = 34, Height = 34, MinHeight = 34,
            Padding = new Thickness(7), Margin = new Thickness(6, 0, 0, 0),
            ToolTip = label
        };
        System.Windows.Automation.AutomationProperties.SetName(button, label);
        return button;
    }

    private void CatalogWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta == 0 || SystemParameters.WheelScrollLines == 0) return;
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
        {
            DependencyObject? current = e.OriginalSource as DependencyObject;
            while (current is not null && current != this)
            {
                if (current is ScrollViewer shelf)
                {
                    shelf.ScrollToHorizontalOffset(shelf.HorizontalOffset - e.Delta * 2);
                    e.Handled = true; return;
                }
                current = current is Visual ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
            }
        }
        double step = SystemParameters.WheelScrollLines < 0 ? ViewportHeight : SystemParameters.WheelScrollLines * 36;
        ScrollToVerticalOffset(VerticalOffset - e.Delta / 120d * step);
        e.Handled = true;
    }
}
