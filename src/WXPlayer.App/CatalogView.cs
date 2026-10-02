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
internal sealed partial class CatalogView : ScrollViewer
{
    private const int PageSize = 36;
    private const double PosterWidth = 176; // 11rem at the 16px design coordinate size.
    private const double PosterHeight = 264;
    private readonly StackPanel _body = new(), _shelves = new();
    private readonly TextBlock _heading, _description, _searchHint;
    private readonly Button _resume;
    private readonly Action<ContentItem> _open, _favorite;
    private int _renderVersion;

    internal TextBox Search { get; } = new();
    internal IReadOnlyList<CatalogShelf> Shelves { get; private set; } = [];

    private Brush Color(string key) => (Brush)Resources[key];
    private TextBlock Text(string value, double size, string color = "--ink") => new()
    { Text = value, FontSize = size, Foreground = Color(color), TextWrapping = TextWrapping.Wrap };

    internal CatalogView(Action<ContentItem> open, Action<ContentItem> favorite, Action resume)
    {
        _open = open; _favorite = favorite;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        Content = _body;
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("CatalogTheme.xaml", UriKind.Relative) });
        Style = (Style)Resources["CatalogViewport"];
        _body.Margin = new Thickness(0, 12.8, 0, 112);
        _searchHint = Text("Film veya dizi ara", 14.4, "--mut");
        BuildHeader(_searchHint);
        Search.TextChanged += (_, _) => _searchHint.Visibility = Search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        System.Windows.Automation.AutomationProperties.SetName(Search, "Film ve dizilerde ara");

        _heading = Text("KÜTÜPHANEN", 11.52, "--acc");
        _description = Text("Kütüphanenizdeki içerikler", 13.6, "--mut");
        _resume = new Button { Style = (Style)Resources["CatalogChip"], Padding = new Thickness(14.4, 11.2, 14.4, 11.2),
            HorizontalAlignment = HorizontalAlignment.Right, MaxWidth = 336, ToolTip = "İzlemeye dön" };
        _resume.Click += (_, _) => resume();
        System.Windows.Automation.AutomationProperties.SetName(_resume, "Oynatıcıya dön");
        _resume.Visibility = Visibility.Collapsed;
        _body.Children.Add(_hero);
        _body.Children.Add(_filterBar);
        _body.Children.Add(_description);
        _description.Margin = new Thickness(35.2, 10, 35.2, 0);
        _body.Children.Add(_shelves);
        PreviewMouseWheel += CatalogWheel;
        SizeChanged += (_, _) => UpdatePresentationScale();
    }

    internal void NowPlaying(string? title)
    {
        _resume.Content = new TextBlock { Text = "İzlemeye dön · " + title, TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 300, FontSize = 13.6, FontWeight = FontWeights.Bold };
        _resume.ToolTip = title;
        _resume.Visibility = string.IsNullOrEmpty(title) ? Visibility.Collapsed : Visibility.Visible;
    }

    internal void Loading(bool series)
    {
        _renderVersion++;
        SelectSegment(series);
        _hero.Visibility = Visibility.Collapsed;
        _filterBar.Children.Clear();
        _heading.Text = "KÜTÜPHANEN";
        _description.Text = "Kütüphaneniz hazırlanıyor…";
        _shelves.Children.Clear();
        var progress = Text("Afişler ve kategoriler yükleniyor…", 14.4, "--mut");
        progress.Margin = new Thickness(35.2, 48, 35.2, 0);
        _shelves.Children.Add(progress);
    }

    internal void Render(bool series, string source, string search, IReadOnlyList<CatalogShelf> shelves,
        Func<string?, int, int, CancellationToken, Task<WXPlayer.Core.Page>> fetch, CancellationToken token)
    {
        int version = ++_renderVersion;
        Shelves = shelves;
        SelectSegment(series);
        _heading.Text = "KÜTÜPHANEN";
        int total = shelves.FirstOrDefault()?.Page.Total ?? 0;
        _description.Text = source + "  ·  " + total.ToString("N0") + (series ? " dizi" : " film");
        _shelves.Children.Clear();
        BuildHero(shelves.FirstOrDefault()?.Page.Items ?? []);
        _filterBar.Children.Clear();
        var all = new Button { Content = "Tümü", Style = (Style)Resources["CatalogSelectedChip"] };
        all.Click += (_, _) => ScrollToTop();
        _filterBar.Children.Add(all);
        ScrollToTop();
        if (total == 0)
        {
            var empty = new StackPanel { Margin = new Thickness(32, 80, 32, 0), HorizontalAlignment = HorizontalAlignment.Center };
            var title = Text(search.Length > 0 ? "Eşleşen içerik bulunamadı" : "Bu bölüm henüz boş", 18.4);
            title.FontWeight = FontWeights.SemiBold; title.HorizontalAlignment = HorizontalAlignment.Center;
            empty.Children.Add(title);
            var detail = Text(search.Length > 0 ? "Başka bir adla aramayı deneyin." : "Kütüphanenize film veya dizi içeren bir kaynak ekleyin.", 14.4, "--mut");
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
        var section = new StackPanel { Margin = new Thickness(0, 32, 0, 0) };
        _shelves.Children.Add(section);
        var header = new DockPanel { Margin = new Thickness(35.2, 0, 35.2, 14.4) };
        section.Children.Add(header);
        var previous = Arrow("chevron-left", shelf.Title + " · geri");
        var next = Arrow("chevron-right", shelf.Title + " · ileri");
        var heading = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var title = Text(shelf.Title, 18.4); title.FontWeight = FontWeights.Bold;
        heading.Children.Add(title);
        var count = Text("  " + shelf.Page.Total.ToString("N0"), 12.8, "--mut");
        count.VerticalAlignment = VerticalAlignment.Bottom; count.Margin = new Thickness(5, 0, 0, 3);
        heading.Children.Add(count); header.Children.Add(heading);

        var cards = new StackPanel { Orientation = Orientation.Horizontal };
        var scroll = new ScrollViewer
        {
            Content = cards, HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, CanContentScroll = false,
            Padding = new Thickness(35.2, 6.4, 19.2, 16)
        };
        var row = new Grid(); row.Children.Add(scroll); section.Children.Add(row);
        previous.HorizontalAlignment = HorizontalAlignment.Left; next.HorizontalAlignment = HorizontalAlignment.Right;
        previous.Margin = new Thickness(16, 0, 0, 32); next.Margin = new Thickness(0, 0, 16, 32);
        row.Children.Add(previous); row.Children.Add(next);
        void RevealArrows(bool visible)
        {
            var duration = TimeSpan.FromSeconds(SystemParameters.ClientAreaAnimation ? .2 : 0);
            previous.BeginAnimation(OpacityProperty, new DoubleAnimation(visible ? 1 : 0, duration));
            next.BeginAnimation(OpacityProperty, new DoubleAnimation(visible ? 1 : 0, duration));
        }
        section.MouseEnter += (_, _) => RevealArrows(true);
        section.MouseLeave += (_, _) => RevealArrows(section.IsKeyboardFocusWithin);
        section.IsKeyboardFocusWithinChanged += (_, _) => RevealArrows(section.IsMouseOver || section.IsKeyboardFocusWithin);
        if (shelf.Category is not null)
        {
            var chip = new Button { Content = shelf.Title, Style = (Style)Resources["CatalogChip"], Margin = new Thickness(0, 0, 9.6, 0) };
            chip.Click += (_, _) => section.BringIntoView();
            _filterBar.Children.Add(chip);
        }
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

    private Grid Card(ContentItem item) => BuildCardPresentation(item);

    private Button Arrow(string icon, string label)
    {
        var button = new Button
        {
            Style = (Style)Resources["CatalogIconButton"],
            Content = new SvgIcon(icon) { Width = 20.8, Height = 20.8, StrokeThickness = 1.7 },
            Width = 44.8, Height = 44.8, MinHeight = 0,
            Background = Color("CatalogArrowGlass"), BorderBrush = Color("--line"), BorderThickness = new Thickness(1),
            Opacity = 0, ToolTip = label
        };
        System.Windows.Automation.AutomationProperties.SetName(button, label);
        CatalogPillBorder.SetTransitionSeconds(button, .2);
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
