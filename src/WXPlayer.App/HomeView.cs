using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed record HomeShelf(string Title, string Section, WXPlayer.Core.Page Page);

internal sealed partial class HomeView : ScrollViewer
{
    private const double FeatureHeight = 300;
    private readonly StackPanel _body = new(), _shelves = new();
    private readonly Grid _feature = new() { Height = FeatureHeight, ClipToBounds = true };
    private readonly Grid _toolbar = new(), _searchBox = new();
    private readonly Button _return;
    private readonly Action<ContentItem> _play, _favorite;
    private readonly Action<string> _browse;
    private readonly Action _add;
    internal readonly TextBox Search = new() { MinHeight = 40, Padding = new Thickness(12, 9, 12, 9) };
    internal IReadOnlyList<ContentItem> Items { get; private set; } = [];
    internal ContentItem? Featured { get; private set; }
    internal bool Empty { get; private set; }
    private static Brush Color(string value) => PremiumWindow.Brush(value);
    private static TextBlock Text(string value, int size = 13, string color = "#E9EEF5") => PremiumWindow.Text(value, size, color);

    internal HomeView(Action<ContentItem> play, Action<ContentItem> favorite, Action<string> browse, Action add, Action resume)
    {
        _play = play; _favorite = favorite; _browse = browse; _add = add;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        Content = _body; _body.Margin = new Thickness(0, 0, 12, 0);
        _toolbar.Margin = new Thickness(0, 0, 0, 20);
        _toolbar.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        _toolbar.ColumnDefinitions.Add(new() { Width = new GridLength(270) });
        _toolbar.RowDefinitions.Add(new() { Height = GridLength.Auto });
        _toolbar.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var shortcuts = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        foreach (var (label, section) in new[] { ("Canlı TV", "live"), ("Filmler", "movie"), ("Diziler", "series"), ("Favoriler", "favorites") })
        {
            var button = Action(label, () => browse(section));
            button.Margin = new Thickness(0, 0, 8, 0); button.Padding = new Thickness(12, 8, 12, 8);
            shortcuts.Children.Add(button);
        }
        _toolbar.Children.Add(shortcuts); _searchBox.Children.Add(Search);
        var hint = Text("Kütüphanende ara…", 12, "#98A3B6");
        hint.Margin = new Thickness(14, 0, 0, 0); hint.VerticalAlignment = VerticalAlignment.Center; hint.IsHitTestVisible = false;
        _searchBox.Children.Add(hint);
        Search.TextChanged += (_, _) => hint.Visibility = Search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        Search.GotKeyboardFocus += (_, _) => Search.BorderBrush = Color("#C1EC8B");
        Search.LostKeyboardFocus += (_, _) => Search.BorderBrush = Color("#303A4B");
        System.Windows.Automation.AutomationProperties.SetName(Search, "Ana sayfada içerik ara");
        Grid.SetColumn(_searchBox, 1); _toolbar.Children.Add(_searchBox); _body.Children.Add(_toolbar);
        SizeChanged += (_, _) => LayoutToolbar();
        PreviewMouseWheel += HomeWheel;
        _return = Action("İzlemeye dön", resume);
        _return.HorizontalAlignment = HorizontalAlignment.Left; _return.Margin = new Thickness(0, 0, 0, 16);
        _return.MaxWidth = 600; _return.Visibility = Visibility.Collapsed; _body.Children.Add(_return);
        _feature.Margin = new Thickness(0, 0, 0, 24); _body.Children.Add(_feature);
        _shelves.Margin = new Thickness(0, 0, 0, 24); _body.Children.Add(_shelves);
    }

    private void LayoutToolbar()
    {
        bool compact = ActualWidth < 780;
        _toolbar.ColumnDefinitions[1].Width = compact ? new GridLength(0) : new GridLength(270);
        Grid.SetColumn(_searchBox, compact ? 0 : 1); Grid.SetRow(_searchBox, compact ? 1 : 0);
        _searchBox.Margin = compact ? new Thickness(0, 12, 0, 0) : new Thickness(12, 0, 0, 0);
        _return.MaxWidth = Math.Max(100, ActualWidth - 30);
    }

    private static Button Action(string label, Action action, bool primary = false)
    {
        var button = PremiumWindow.Action(label, action, primary); button.Margin = new Thickness(0); return button;
    }
    internal void NowPlaying(string? title)
    {
        _return.Content = new TextBlock { Text = "İzlemeye dön · " + title, TextTrimming = TextTrimming.CharacterEllipsis };
        _return.ToolTip = title; _return.Visibility = string.IsNullOrEmpty(title) ? Visibility.Collapsed : Visibility.Visible;
    }
    internal void Loading(bool clear = true)
    {
        CancelArtwork(); if (!clear && Items.Count > 0) return; Reset(); ScrollToTop(); Empty = false;
        ShowState("Kütüphanen hazırlanıyor", "İçerikleriniz listeleniyor…", null, null);
        var skeletons = new StackPanel { Orientation = Orientation.Horizontal, ClipToBounds = true, Height = 200 };
        for (int i = 0; i < 8; i++) skeletons.Children.Add(new Border { Width = 160, Height = 200, Background = Color("#141922"), CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 16, 0) });
        _shelves.Children.Add(skeletons);
    }
    internal void Error(Action retry)
    {
        CancelArtwork(); Reset(); Empty = false;
        ShowState("Kütüphane görüntülenemedi", "İçerikler şu anda yüklenemiyor. Yeniden deneyebilirsiniz.", "Yeniden dene", retry);
    }
    private void Reset() { _feature.Children.Clear(); _shelves.Children.Clear(); Items = []; Featured = null; }
    private void ShowState(string title, string description, string? action, Action? callback)
    {
        var copy = new StackPanel { Margin = new Thickness(28), VerticalAlignment = VerticalAlignment.Center };
        var heading = Text(title, 24); heading.FontWeight = FontWeights.SemiBold; copy.Children.Add(heading);
        var detail = Text(description, 13, "#98A3B6"); detail.Margin = new Thickness(0, 12, 0, 20); copy.Children.Add(detail);
        if (action is not null && callback is not null)
        {
            var button = Action(action, callback, true); button.HorizontalAlignment = HorizontalAlignment.Left; copy.Children.Add(button);
        }
        _feature.Children.Add(new Border { Background = Color("#141922"), BorderBrush = Color("#252C38"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Child = copy });
    }
    private void BuildHero(ContentItem item, string source)
    {
        var hero = new Grid { Height = FeatureHeight - 2, ClipToBounds = true, Background = Color("#141B24") };
        Round(hero, 12);
        var art = new ChannelLogo { Url = item.Logo, Initials = "", DecodeWidth = 480, ImageStretch = Stretch.UniformToFill, ImagePadding = new Thickness(0), Opacity = .35, HorizontalAlignment = HorizontalAlignment.Right, Width = 510, Height = FeatureHeight - 2 };
        hero.Children.Add(art);
        var shade = new LinearGradientBrush(); shade.StartPoint = new Point(0, .5); shade.EndPoint = new Point(1, .5);
        shade.GradientStops.Add(new GradientStop(((SolidColorBrush)Color("#141B24")).Color, 0));
        shade.GradientStops.Add(new GradientStop(((SolidColorBrush)Color("#FF141B24")).Color, .43));
        shade.GradientStops.Add(new GradientStop(((SolidColorBrush)Color("#24141B24")).Color, 1));
        hero.Children.Add(new Border { Background = shade, IsHitTestVisible = false });
        var poster = Artwork(item, 172, 258); poster.HorizontalAlignment = HorizontalAlignment.Right; poster.VerticalAlignment = VerticalAlignment.Center; poster.Margin = new Thickness(0, 0, 28, 0); hero.Children.Add(poster);
        var copy = new StackPanel { Margin = new Thickness(30, 22, 24, 22), VerticalAlignment = VerticalAlignment.Center, MaxWidth = 580, HorizontalAlignment = HorizontalAlignment.Left };
        hero.Children.Add(copy);
        var eyebrow = Text("SENİN İÇİN SEÇTİK  ·  " + item.KindLabel, 10, "#C1EC8B"); eyebrow.FontWeight = FontWeights.SemiBold; copy.Children.Add(eyebrow);
        var title = Text(item.Name, 30); title.FontWeight = FontWeights.SemiBold; title.LineHeight = 36;
        title.MaxHeight = 72; title.TextTrimming = TextTrimming.CharacterEllipsis; title.ToolTip = item.Name; title.Margin = new Thickness(0, 14, 0, 12); copy.Children.Add(title);
        var metadata = Text(item.Category + "  ·  " + source, 12, "#B1BECC"); metadata.TextWrapping = TextWrapping.NoWrap; metadata.TextTrimming = TextTrimming.CharacterEllipsis; metadata.Margin = new Thickness(0, 0, 0, 22); copy.Children.Add(metadata);
        var actions = new WrapPanel();
        string playLabel = item.Kind == ContentKind.Series ? "Bölümleri keşfet" : item.Kind == ContentKind.Live ? "Canlı izle" : "Filmi izle";
        var play = Action(playLabel, () => _play(item), true); play.Margin = new Thickness(0, 0, 8, 6); actions.Children.Add(play);
        var favorite = Action(item.IsFavorite ? "Favorilerimde" : "Favorilere ekle", () => _favorite(item)); favorite.Margin = new Thickness(0, 0, 0, 6); actions.Children.Add(favorite); copy.Children.Add(actions);
        hero.SizeChanged += (_, _) =>
        {
            bool narrow = hero.ActualWidth < 800;
            copy.MaxWidth = Math.Max(200, Math.Min(580, hero.ActualWidth - 254));
            art.Width = Math.Min(510, hero.ActualWidth * .58);
            shade.GradientStops[1].Offset = Math.Max(.43, 1 - art.Width / Math.Max(1, hero.ActualWidth));
            title.FontSize = narrow ? 25 : 30; title.LineHeight = narrow ? 30 : 36; title.MaxHeight = narrow ? 90 : 72;
        };
        _feature.Children.Add(hero);
    }
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




