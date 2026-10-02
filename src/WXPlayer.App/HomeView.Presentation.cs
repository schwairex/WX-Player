using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed partial class HomeView
{
    private readonly Grid _header = new() { Height = 73.6, Margin = new Thickness(35.2, 0, 35.2, 0) };
    private readonly Grid _sourceSlot = new(), _actionsSlot = new(), _menuSlot = new();
    private readonly Grid _footer = new() { Margin = new Thickness(35.2, 40, 35.2, 0) };
    private readonly ScaleTransform _rem = new(1, 1);
    public UIElement? ResumeControl => null;
    internal double RootFontSize { get; private set; } = 16;
    internal Grid Header => _header;
    internal Grid Feature => _feature;
    internal IReadOnlyList<HomeShelf> PresentationShelves { get; private set; } = [];

    private void BuildHomeHeader()
    {
        var glass = new Border { Background = Color("CatalogHeaderGlass"), Child = _header, BorderThickness = new Thickness(0, 0, 0, 1) };
        Tag = glass; _body.LayoutTransform = _rem; glass.LayoutTransform = _rem;
        foreach (var width in new[] { GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto, GridLength.Auto })
            _header.ColumnDefinitions.Add(new() { Width = width });
        var title = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        _menuSlot.Margin = new Thickness(0, 0, 12, 0); title.Children.Add(_menuSlot);
        var heading = Text("Ana sayfa", 21.6); heading.FontWeight = FontWeights.ExtraBold; title.Children.Add(heading); _header.Children.Add(title);
        _searchBox.MaxWidth = 384; _searchBox.MinWidth = 130; _searchBox.Height = 41.6;
        _searchBox.Margin = new Thickness(16, 0, 16, 0); _searchBox.HorizontalAlignment = HorizontalAlignment.Right;
        void FitSearch() => _searchBox.Width = Math.Min(384, Math.Max(130, _header.ColumnDefinitions[1].ActualWidth - 32));
        _header.SizeChanged += (_, _) => FitSearch(); _header.Loaded += (_, _) => FitSearch();
        Search.Style = (Style)FindResource("CatalogSearch"); Search.ClearValue(MinHeightProperty); Search.ClearValue(PaddingProperty);
        _searchBox.Children.Add(Search);
        var hint = Text("Kütüphanede ara", 14.4, "--mut"); hint.Margin = new Thickness(44.8, 0, 48, 0);
        hint.VerticalAlignment = VerticalAlignment.Center; hint.TextWrapping = TextWrapping.NoWrap; hint.TextTrimming = TextTrimming.CharacterEllipsis; hint.IsHitTestVisible = false; _searchBox.Children.Add(hint);
        Search.TextChanged += (_, _) => hint.Visibility = Search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        _searchBox.Children.Add(new SvgIcon("search") { Width = 18.4, Height = 18.4, Foreground = Color("--mut"), Margin = new Thickness(16, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, IsHitTestVisible = false });
        _searchBox.Children.Add(new Border { CornerRadius = new CornerRadius(6.4), BorderThickness = new Thickness(1), BorderBrush = Color("--line"), Padding = new Thickness(7.2, 1.6, 7.2, 1.6), Margin = new Thickness(0, 0, 14.4, 0), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false, Child = Text("/", 11.2, "--mut") });
        System.Windows.Automation.AutomationProperties.SetName(Search, "Ana sayfada içerik ara");
        Grid.SetColumn(_searchBox, 1); _header.Children.Add(_searchBox);
        _sourceSlot.Margin = new Thickness(0, 0, 16, 0); _sourceSlot.VerticalAlignment = _actionsSlot.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(_sourceSlot, 2); Grid.SetColumn(_actionsSlot, 3); _header.Children.Add(_sourceSlot); _header.Children.Add(_actionsSlot);
        ScrollChanged += (_, e) => glass.BorderBrush = e.VerticalOffset > 8 ? Color("--line") : Brushes.Transparent;
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Oem2 && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && Keyboard.FocusedElement is not TextBox)
            { Search.Focus(); e.Handled = true; }
            else if (e.Key == Key.Escape && Search.IsKeyboardFocusWithin) { Keyboard.Focus(this); e.Handled = true; }
        };
    }
    internal void AttachShell(UIElement source, UIElement actions, UIElement menu, UIElement status)
    { _sourceSlot.Children.Add(source); _actionsSlot.Children.Add(actions); _menuSlot.Children.Add(menu); _footer.Children.Add(status); UpdatePresentationScale(); }
    internal void DetachShell()
    { _sourceSlot.Children.Clear(); _actionsSlot.Children.Clear(); _menuSlot.Children.Clear(); _footer.Children.Clear(); }
    internal void UpdatePresentationScale()
    {
        double viewport = Window.GetWindow(this)?.ActualWidth ?? ActualWidth;
        if (viewport <= 0) return;
        RootFontSize = Math.Clamp(viewport * .005 + 8, 14, 20); _rem.ScaleX = _rem.ScaleY = RootFontSize / 16;
        _sourceSlot.Visibility = viewport <= 720 ? Visibility.Collapsed : Visibility.Visible;
    }
    private Button HomeAction(string label, Action callback, string style, string? icon = null)
    {
        var button = new Button { Style = (Style)FindResource(style), Content = label };
        if (icon is not null) button.Content = new IconLabel { Icon = icon, Label = label };
        button.Click += (_, _) => callback(); System.Windows.Automation.AutomationProperties.SetName(button, label); return button;
    }
    private void RenderHeroPresentation()
    {
        if (_heroItems.Count == 0) return;
        var item = Featured = _heroItems[_heroIndex]; _feature.Children.Clear();
        var hero = new Grid { MinHeight = 224, Margin = new Thickness(38.4, 32, 38.4, 32) };
        hero.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        hero.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        var surface = new Border { CornerRadius = new CornerRadius(25.6), BorderBrush = Color("--line"), BorderThickness = new Thickness(1), Background = HeroBackground(item.Name), Child = hero };
        _feature.Children.Add(surface);
        var copy = new StackPanel { MaxWidth = 544, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 24, 0) };
        hero.Children.Add(copy);
        var eyebrow = Text("ÖNE ÇIKAN " + (item.Kind == ContentKind.Series ? "DİZİ" : item.Kind == ContentKind.Live ? "KANAL" : "FİLM"), 11.52, "--acc"); eyebrow.FontWeight = FontWeights.Bold; copy.Children.Add(eyebrow);
        var title = Text(HomeDisplayFormatter.Clean(item.Name), 38.4); title.FontWeight = FontWeights.ExtraBold; title.Margin = new Thickness(0, 8, 0, 4.8); title.MaxHeight = 92; title.TextTrimming = TextTrimming.CharacterEllipsis; title.ToolTip = item.Name; copy.Children.Add(title);
        var metadata = Text(HomeDisplayFormatter.Clean(item.Category) + " · " + _heroSource, 15.2, "CatalogMetadata"); metadata.TextWrapping = TextWrapping.NoWrap; metadata.TextTrimming = TextTrimming.CharacterEllipsis; copy.Children.Add(metadata);
        var actions = new WrapPanel { Margin = new Thickness(0, 20.8, 0, 0) };
        var play = HomeAction("Şimdi izle", () => _play(item), "CatalogPrimary", "fs-v2-play"); play.Margin = new Thickness(0, 0, 11.2, 0); actions.Children.Add(play);
        var favorite = HomeAction(item.IsFavorite ? "Favorilerde" : "Favorilere ekle", () => _favorite(item), "CatalogButton", item.IsFavorite ? "catalog-star-filled" : "catalog-star");
        favorite.Height = 46.4; favorite.Padding = new Thickness(22.4, 0, 22.4, 0); favorite.FontSize = 14.72; favorite.Background = Color("CatalogButtonGlass"); actions.Children.Add(favorite); copy.Children.Add(actions);
        var navigation = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 22.4, 0, 0) }; copy.Children.Add(navigation);
        Button Arrow(string icon, string label, Action callback)
        {
            var button = HomeAction(label, callback, "CatalogIconButton"); button.Content = new SvgIcon(icon) { Width = 16, Height = 16 }; button.Width = button.Height = 32; button.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 255, 255, 255)); button.Foreground = Color("--ink"); button.Margin = new Thickness(0, 0, 6.4, 0); button.ToolTip = label; return button;
        }
        if (_heroItems.Count > 1) navigation.Children.Add(Arrow("chevron-left", "Önceki öne çıkan içerik", () => MoveHero(-1)));
        for (int i = 0; i < _heroItems.Count; i++)
        {
            int index = i;
            var dot = HomeAction("Öne çıkan " + (i + 1) + ": " + _heroItems[i].Name, () => { _heroIndex = index; RenderHero(); }, "HomeDot");
            dot.Content = null; dot.Width = i == _heroIndex ? 25.6 : 8; dot.Background = i == _heroIndex ? Color("--acc") : new SolidColorBrush(System.Windows.Media.Color.FromArgb(77, 255, 255, 255)); dot.Margin = new Thickness(0, 0, 6.4, 0); navigation.Children.Add(dot);
        }
        if (_heroItems.Count > 1) navigation.Children.Add(Arrow("chevron-right", "Sonraki öne çıkan içerik", () => MoveHero(1)));
        var poster = CatalogPosterCard.Poster(this, item, 152, 228, 480); poster.RenderTransform = new RotateTransform(3); poster.RenderTransformOrigin = new Point(.5, .5); poster.VerticalAlignment = VerticalAlignment.Bottom; Grid.SetColumn(poster, 1); hero.Children.Add(poster);
        hero.SizeChanged += (_, _) => { poster.Visibility = (Window.GetWindow(this)?.ActualWidth ?? ActualWidth) <= 720 ? Visibility.Collapsed : Visibility.Visible; copy.MaxWidth = Math.Max(160, Math.Min(544, hero.ActualWidth - (poster.IsVisible ? 176 : 0))); };
    }
    private static Brush HeroBackground(string title)
    {
        int hue = 0; foreach (char c in title) hue = (hue * 31 + c) % 360;
        var rectangle = new RectangleGeometry(new Rect(0, 0, 1, 1));
        var layers = new DrawingGroup();
        layers.Children.Add(new GeometryDrawing(new LinearGradientBrush(new GradientStopCollection { new(Hsl((hue + 50) % 360, .4, .12), 0), new(System.Windows.Media.Color.FromRgb(13, 17, 19), 1) }, new Point(0, 0), new Point(1, 1)), null, rectangle));
        var radial = new RadialGradientBrush { Center = new Point(.85, .2), GradientOrigin = new Point(.85, .2), RadiusX = .8, RadiusY = 1.2 };
        radial.GradientStops.Add(new(Hsl(hue, .4, .26), 0)); radial.GradientStops.Add(new(Colors.Transparent, .6));
        layers.Children.Add(new GeometryDrawing(radial, null, rectangle));
        return new DrawingBrush(layers) { Stretch = Stretch.Fill };
    }
    private static System.Windows.Media.Color Hsl(double hue, double saturation, double lightness)
    {
        double c = (1 - Math.Abs(2 * lightness - 1)) * saturation, x = c * (1 - Math.Abs(hue / 60 % 2 - 1)), m = lightness - c / 2;
        var rgb = hue switch { < 60 => (c, x, 0d), < 120 => (x, c, 0d), < 180 => (0d, c, x), < 240 => (0d, x, c), < 300 => (x, 0d, c), _ => (c, 0d, x) };
        return System.Windows.Media.Color.FromRgb((byte)Math.Round((rgb.Item1 + m) * 255), (byte)Math.Round((rgb.Item2 + m) * 255), (byte)Math.Round((rgb.Item3 + m) * 255));
    }
}


