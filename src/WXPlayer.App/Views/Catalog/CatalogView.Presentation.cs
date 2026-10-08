using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WXPlayer.Core;

namespace WXPlayer.App;

// Presentation only: reads ContentItem, reuses existing open/favorite/resume callbacks.
// No provider calls, new persistent state, playback commands or database queries live here.
internal sealed partial class CatalogView
{
    private readonly Grid _header = new() { Height = 73.6, Margin = new Thickness(35.2, 0, 35.2, 0) };
    private readonly Grid _hero = new() { MinHeight = 288, Margin = new Thickness(35.2, 6.4, 35.2, 0) };
    private readonly WrapPanel _filterBar = new() { Margin = new Thickness(35.2, 25.6, 35.2, 0) };
    private readonly ScaleTransform _rem = new(1, 1);
    private readonly Button _moviesSegment = new() { Content = "Filmler" }, _seriesSegment = new() { Content = "Diziler" };
    private Action? _navigateMovies, _navigateSeries;
    private readonly Grid _sourceSlot = new(), _actionsSlot = new();
    public UIElement ResumeControl => _resume;
    internal double RootFontSize { get; private set; } = 16;
    internal Grid Header => _header;
    internal Grid Hero => _hero;

    private void BuildHeader(TextBlock hint)
    {
        var glass = new Border { Background = Color("CatalogHeaderGlass"), Child = _header };
        Tag = glass;
        _body.LayoutTransform = _rem; glass.LayoutTransform = _rem;
        _header.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        _header.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        _header.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        _header.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        var segments = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var button in new[] { _moviesSegment, _seriesSegment })
        {
            button.Padding = new Thickness(20.8, 8, 20.8, 8); button.FontSize = 15.2;
            System.Windows.Automation.AutomationProperties.SetName(button, button.Content.ToString());
            segments.Children.Add(button);
        }
        _moviesSegment.Click += (_, _) => _navigateMovies?.Invoke();
        _seriesSegment.Click += (_, _) => _navigateSeries?.Invoke();
        _header.Children.Add(new CatalogPillBorder { Background = Color("--p2"),
            Padding = new Thickness(4), Child = segments, VerticalAlignment = VerticalAlignment.Center });
        var searchFrame = new Grid { MaxWidth = 384, MinWidth = 150, Height = 41.6,
            Margin = new Thickness(16, 0, 16, 0), HorizontalAlignment = HorizontalAlignment.Right };
        void FitSearch() => searchFrame.Width = Math.Min(384, Math.Max(150, _header.ColumnDefinitions[1].ActualWidth - 32));
        _header.SizeChanged += (_, _) => FitSearch();
        _header.Loaded += (_, _) => FitSearch();
        Search.Style = (Style)Resources["CatalogSearch"]; Search.HorizontalAlignment = HorizontalAlignment.Stretch;
        hint.VerticalAlignment = VerticalAlignment.Center; hint.Margin = new Thickness(44.8, 0, 48, 0);
        hint.TextWrapping = TextWrapping.NoWrap; hint.TextTrimming = TextTrimming.CharacterEllipsis; hint.IsHitTestVisible = false;
        searchFrame.Children.Add(Search); searchFrame.Children.Add(hint);
        searchFrame.Children.Add(new SvgIcon("search") { Width = 18.4, Height = 18.4, StrokeThickness = 1.7,
            Foreground = Color("--mut"), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 0, 0, 0) });
        searchFrame.Children.Add(new Border { CornerRadius = new CornerRadius(6.4), BorderBrush = Color("--line"),
            BorderThickness = new Thickness(1), Padding = new Thickness(7.2, 1.6, 7.2, 1.6),
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 14.4, 0), IsHitTestVisible = false,
            Child = Text("/", 11.2, "--mut") });
        Grid.SetColumn(searchFrame, 1); _header.Children.Add(searchFrame);
        _sourceSlot.Margin = new Thickness(0, 0, 16, 0); _sourceSlot.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(_sourceSlot, 2); Grid.SetColumn(_actionsSlot, 3);
        _actionsSlot.VerticalAlignment = VerticalAlignment.Center;
        _header.Children.Add(_sourceSlot); _header.Children.Add(_actionsSlot);
        ScrollChanged += (_, e) => glass.BorderBrush = e.VerticalOffset > 8 ? Color("--line") : Brushes.Transparent;
        glass.BorderThickness = new Thickness(0, 0, 0, 1);
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Oem2 && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && Keyboard.FocusedElement is not TextBox)
            { Search.Focus(); e.Handled = true; }
            else if (e.Key == Key.Escape && Search.IsKeyboardFocusWithin)
            { Keyboard.Focus(_moviesSegment.IsEnabled ? _moviesSegment : _seriesSegment); e.Handled = true; }
        };
    }

    internal void AttachShell(UIElement source, UIElement actions, Action movies, Action series)
    {
        _navigateMovies = movies; _navigateSeries = series;
        _sourceSlot.Children.Add(source); _actionsSlot.Children.Add(actions);
        UpdatePresentationScale();
    }
    internal void DetachShell()
    { _sourceSlot.Children.Clear(); _actionsSlot.Children.Clear(); }
    internal void UpdatePresentationScale()
    {
        double viewport = Window.GetWindow(this)?.ActualWidth ?? ActualWidth;
        if (viewport <= 0) return;
        RootFontSize = Math.Clamp(viewport * .005 + 8, 14, 20);
        _rem.ScaleX = _rem.ScaleY = RootFontSize / 16;
        bool compact = viewport <= 720;
        _sourceSlot.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
    }
    private void SelectSegment(bool series)
    {
        _moviesSegment.Style = (Style)Resources[series ? "CatalogChip" : "CatalogSelectedChip"];
        _seriesSegment.Style = (Style)Resources[series ? "CatalogSelectedChip" : "CatalogChip"];
    }

    private void BuildHero(IReadOnlyList<ContentItem> items)
    {
        _hero.Children.Clear();
        var item = items.FirstOrDefault(i => i.Progress is { Completed: false }) ?? items.FirstOrDefault();
        _hero.Visibility = item is null ? Visibility.Collapsed : Visibility.Visible;
        if (item is null) return;
        int hue = 0; foreach (char c in item.Name) hue = (hue * 31 + c) % 360;
        var linear = new LinearGradientBrush(Hsl((hue + 50) % 360, .4, .12),
            System.Windows.Media.Color.FromRgb(13, 17, 19), new Point(0, 0), new Point(1, 1));
        var radial = new RadialGradientBrush { Center = new Point(.85, .2), GradientOrigin = new Point(.85, .2),
            RadiusX = .8, RadiusY = 1.2 };
        radial.GradientStops.Add(new GradientStop(Hsl(hue, .4, .26), 0));
        var fadeColor = Hsl(hue, .4, .26); fadeColor.A = 0;
        radial.GradientStops.Add(new GradientStop(fadeColor, .6));
        var backgrounds = new Grid(); backgrounds.Children.Add(new Border { Background = linear });
        backgrounds.Children.Add(new Border { Background = radial });
        var chrome = new Border { Background = linear, BorderBrush = Color("--line"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(25.6), Child = backgrounds };
        backgrounds.SizeChanged += (_, _) => backgrounds.Clip = new RectangleGeometry(
            new Rect(0, 0, backgrounds.ActualWidth, backgrounds.ActualHeight), 25.6, 25.6);
        _hero.Children.Add(chrome);
        var layout = new Grid { Margin = new Thickness(38.4, 32, 38.4, 32) };
        layout.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        layout.ColumnDefinitions.Add(new() { Width = new GridLength(152) });
        _hero.Children.Add(layout);
        var labels = new StackPanel { MaxWidth = 544, HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 20, 0) };
        labels.Children.Add(new TextBlock { Text = item.Progress is { Completed: false } ? "KALDIĞIN YERDEN DEVAM ET" : "KÜTÜPHANENDEN",
            Foreground = Color("--acc"), FontSize = 11.52, FontWeight = FontWeights.Bold });
        var name = Text(item.Name, 38.4); name.FontWeight = FontWeights.ExtraBold;
        name.Margin = new Thickness(0, 8, 0, 4.8); labels.Children.Add(name);
        labels.Children.Add(Text(item.Progress is null ? item.Category : item.Category + " · " + item.ProgressLabel, 15.2, "CatalogMetadata"));
        if (item.Progress is not null)
        {
            var progress = Progress(item); progress.Width = 288; progress.HorizontalAlignment = HorizontalAlignment.Left;
            progress.Margin = new Thickness(0, 17.6, 0, 19.2); labels.Children.Add(progress);
        }
        var playLabel = item.Kind == ContentKind.Series ? "Bölümleri gör" : item.Progress is { Completed: false } ? "Devam et" : "Oynat";
        var action = new Button { Style = (Style)Resources["CatalogPrimary"], HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, item.Progress is null ? 19.2 : 0, 0, 0), Content = IconText("fs-v2-play", playLabel, 17.6, 8.8) };
        action.Click += (_, _) => _open(item);
        System.Windows.Automation.AutomationProperties.SetName(action, playLabel + " · " + item.Name);
        labels.Children.Add(action); layout.Children.Add(labels);
        var poster = Poster(item, 152, 228);
        poster.RenderTransform = new RotateTransform(3); poster.RenderTransformOrigin = new Point(.5, .5);
        poster.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 50, ShadowDepth = 20, Direction = 270, Opacity = 2d / 3, Color = Colors.Black };
        Grid.SetColumn(poster, 1); layout.Children.Add(poster);
    }

    private StackPanel IconText(string icon, string label, double iconSize, double gap)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(new SvgIcon(icon) { Width = iconSize, Height = iconSize, Margin = new Thickness(0, 0, gap, 0) });
        panel.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center }); return panel;
    }

    private Grid BuildCardPresentation(ContentItem item) => CatalogPosterCard.Build(this, item, _open, _favorite);
    private ProgressBar Progress(ContentItem item) => new() { Height = 4, Minimum = 0, Maximum = 1,
        Value = item.Progress?.Fraction ?? 0, Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255)),
        Foreground = Color("--acc"), BorderThickness = new Thickness(0), IsHitTestVisible = false };
    private Grid Poster(ContentItem item, double width, double height) => CatalogPosterCard.Poster(this, item, width, height);
    private static System.Windows.Media.Color Hsl(double hue, double saturation, double lightness)
    {
        double chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation, sector = hue / 60;
        double x = chroma * (1 - Math.Abs(sector % 2 - 1)), m = lightness - chroma / 2;
        var (r,g,b) = sector switch { < 1 => (chroma,x,0d), < 2 => (x,chroma,0d), < 3 => (0d,chroma,x),
            < 4 => (0d,x,chroma), < 5 => (x,0d,chroma), _ => (chroma,0d,x) };
        return System.Windows.Media.Color.FromRgb((byte)Math.Round((r+m)*255), (byte)Math.Round((g+m)*255), (byte)Math.Round((b+m)*255));
    }
}

