using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Threading;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed record HomeShelf(string Title, string Section, WXPlayer.Core.Page Page);

internal sealed partial class HomeView : ScrollViewer
{
    private const double FeatureHeight = 352;
    private readonly StackPanel _body = new(), _shelves = new();
    private readonly Grid _feature = new() { Height = FeatureHeight, ClipToBounds = true };
    private readonly Grid _toolbar = new(), _searchBox = new();
    private readonly Button _return;
    private readonly Action<ContentItem> _play, _favorite;
    private readonly Action<string> _browse;
    private readonly Action _add;
    private readonly Func<ContentItem,Task>? _removeRecent;
    private readonly DispatcherTimer _heroTimer = new() { Interval = TimeSpan.FromSeconds(9) };
    private IReadOnlyList<ContentItem> _heroItems = [];
    private string _heroSource = "";
    private int _heroIndex;
    internal readonly TextBox Search = new() { MinHeight = 40, Padding = new Thickness(12, 9, 12, 9) };
    internal IReadOnlyList<ContentItem> Items { get; private set; } = [];
    internal ContentItem? Featured { get; private set; }
    internal bool Empty { get; private set; }
    private Brush Color(string value) => (Brush)FindResource(value);
    private TextBlock Text(string value, double size = 13, string color = "--ink") => new() { Text = value, FontSize = size, FontFamily = (FontFamily)FindResource("CatalogFont"), Foreground = Color(color), TextWrapping = TextWrapping.Wrap };

    internal HomeView(Action<ContentItem> play, Action<ContentItem> favorite, Action<string> browse, Action add, Action resume, Func<ContentItem,Task>? removeRecent = null)
    {
        _play = play; _favorite = favorite; _browse = browse; _add = add; _removeRecent = removeRecent;
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("HomeTheme.xaml", UriKind.Relative) });
        Style = (Style)FindResource("CatalogViewport");
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        Content = _body; _body.Margin = new Thickness(0, 6.4, 0, 48);
        // Keep the shortcut actions and resume callback, but hide their redundant chrome.
        _toolbar.Visibility = Visibility.Collapsed;
        foreach (var (label, section) in new[] { ("Canlı TV", "live"), ("Filmler", "movie"), ("Diziler", "series"), ("Favoriler", "favorites") })
            _toolbar.Children.Add(Action(label, () => browse(section)));
        _body.Children.Add(_toolbar);
        _return = Action("İzlemeye dön", resume); _return.Visibility = Visibility.Collapsed; _body.Children.Add(_return);
        _feature.Height = double.NaN; _feature.MinHeight = 288; _feature.MaxHeight = FeatureHeight;
        _feature.Margin = new Thickness(35.2, 6.4, 35.2, 0); _body.Children.Add(_feature);
        _shelves.Margin = new Thickness(0); _body.Children.Add(_shelves);
        _body.Children.Add(_footer);
        BuildHomeHeader();
        SizeChanged += (_, _) => UpdatePresentationScale();
        PreviewMouseWheel += HomeWheel;
        _heroTimer.Tick += (_, _) => { if (IsVisible && !_feature.IsMouseOver && _heroItems.Count > 1 && Search.Text.Length == 0) MoveHero(1); };
        Loaded += (_, _) => { UpdatePresentationScale(); _heroTimer.Start(); };
        Unloaded += (_, _) => _heroTimer.Stop();
    }
    private static Button Action(string label, Action action, bool primary = false)
    {
        var button = PremiumWindow.Action(label, action, primary); button.Margin = new Thickness(0); return button;
    }
    internal void NowPlaying(string? title)
    {
        _return.Content = new TextBlock { Text = "İzlemeye dön · " + title, TextTrimming = TextTrimming.CharacterEllipsis };
        _return.ToolTip = title; _return.Visibility = Visibility.Collapsed;
    }
    internal void Loading(bool clear = true)
    {
        CancelArtwork(); if (!clear && Items.Count > 0) return; Reset(); ScrollToTop(); Empty = false;
        ShowState("Kütüphanen hazırlanıyor", "İçerikleriniz listeleniyor…", null, null);
        var skeletons = new StackPanel { Orientation = Orientation.Horizontal, ClipToBounds = true, Height = 200 };
        for (int i = 0; i < 8; i++) skeletons.Children.Add(new Border { Width = 160, Height = 200, Background = Color("--p2"), CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 16, 0) });
        _shelves.Children.Add(skeletons);
    }
    internal void Error(Action retry)
    {
        CancelArtwork(); Reset(); Empty = false;
        ShowState("Kütüphane görüntülenemedi", "İçerikler şu anda yüklenemiyor. Yeniden deneyebilirsiniz.", "Yeniden dene", retry);
    }
    private void Reset() { _feature.Children.Clear(); _shelves.Children.Clear(); Items = []; Featured = null; _heroItems = []; PresentationShelves = []; }
    private void ShowState(string title, string description, string? action, Action? callback)
    {
        var copy = new StackPanel { Margin = new Thickness(28), VerticalAlignment = VerticalAlignment.Center };
        var heading = Text(title, 24); heading.FontWeight = FontWeights.SemiBold; copy.Children.Add(heading);
        var detail = Text(description, 13, "--mut"); detail.Margin = new Thickness(0, 12, 0, 20); copy.Children.Add(detail);
        if (action is not null && callback is not null)
        {
            var button = Action(action, callback, true); button.HorizontalAlignment = HorizontalAlignment.Left; copy.Children.Add(button);
        }
        _feature.Children.Add(new Border { Background = Color("--p2"), CornerRadius = new CornerRadius(12), Child = copy });
    }
    private void BuildHero(ContentItem item, string source)
    {
        _heroSource = source;
        _heroItems = Items.Where(i => i.Kind is ContentKind.Movie or ContentKind.Series).Take(7).ToArray();
        if (item.Kind is ContentKind.Movie or ContentKind.Series && _heroItems.All(i => i.Id != item.Id))
            _heroItems = new[] { item }.Concat(_heroItems).Take(7).ToArray();
        if (_heroItems.Count == 0) _heroItems = [item];
        int previous = _heroItems.ToList().FindIndex(i => i.Id == Featured?.Id);
        _heroIndex = previous >= 0 ? previous : 0;
        RenderHero();
    }
    private void MoveHero(int direction)
    {
        if (_heroItems.Count < 2) return;
        _heroIndex = (_heroIndex + direction + _heroItems.Count) % _heroItems.Count;
        RenderHero();
    }
    private void RenderHero() => RenderHeroPresentation();
    private void HomeWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta == 0 || SystemParameters.WheelScrollLines == 0) return;
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
        {
            DependencyObject? current = e.OriginalSource as DependencyObject;
            while (current is not null && current != this)
            {
                if (current is ScrollViewer shelf) { shelf.ScrollToHorizontalOffset(shelf.HorizontalOffset - e.Delta * 2); e.Handled = true; return; }
                current = current is Visual ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
            }
        }
        double step = SystemParameters.WheelScrollLines < 0 ? ViewportHeight : SystemParameters.WheelScrollLines * 36;
        ScrollToVerticalOffset(VerticalOffset - e.Delta / 120d * step);
        e.Handled = true;
    }
    private static void Round(FrameworkElement element, double radius)
    {
        element.SizeChanged += (_, _) => element.Clip = new RectangleGeometry(new Rect(0, 0, element.ActualWidth, element.ActualHeight), radius, radius);
    }
}






