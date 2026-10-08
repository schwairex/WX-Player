using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WXPlayer.Core;

namespace WXPlayer.App;

// Shared poster presentation. Callback ownership, artwork loader and model remain unchanged.
internal static class CatalogPosterCard
{
    internal const double PosterWidth = 176, PosterHeight = 264;
    private static TextBlock Label(FrameworkElement owner, string text, double size, string key) => new()
    { Text = text, FontSize = size, FontFamily = (FontFamily)owner.FindResource("CatalogFont"),
        Foreground = (Brush)owner.FindResource(key), TextWrapping = TextWrapping.Wrap };
    internal static Grid Build(FrameworkElement owner, ContentItem item, Action<ContentItem> openItem, Action<ContentItem> favoriteItem, bool home = false)
    {
        Brush Color(string key) => (Brush)owner.FindResource(key);
        TextBlock Text(string value, double size, string key = "--ink") => Label(owner, value, size, key);
        var wrapper = new Grid { Width = PosterWidth, Margin = new Thickness(0, 0, 16, 0),
            VerticalAlignment = VerticalAlignment.Top, DataContext = item };
        var content = new StackPanel();
        var poster = Poster(owner, item, PosterWidth, PosterHeight, home ? 480 : 400); content.Children.Add(poster);
        var shadow = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 10, ShadowDepth = 2, Direction = 270, Opacity = .4, Color = Colors.Black };
        if (!home) poster.Effect = shadow;
        var shift = new TranslateTransform(); poster.RenderTransform = shift;
        var name = Text(home ? HomeDisplayFormatter.Clean(item.Name) : item.Name, 14.72); name.FontWeight = FontWeights.SemiBold;
        name.Margin = new Thickness(0, 11.2, 0, 0); name.TextTrimming = TextTrimming.CharacterEllipsis;
        if (home) name.Height = 20; name.TextWrapping = TextWrapping.NoWrap; name.ToolTip = item.Name; content.Children.Add(name);
        string year = System.Text.RegularExpressions.Regex.Match(item.Name, @"(?<!\d)(?:19|20)\d{2}(?!\d)").Value;
        var meta = Text(home ? HomeDisplayFormatter.Metadata(item) : year.Length > 0 ? year : item.Category, 12.48, "--mut");
        meta.Margin = new Thickness(0, 2.4, 0, 0); meta.TextTrimming = TextTrimming.CharacterEllipsis;
        if (home) meta.Height = 18; meta.TextWrapping = TextWrapping.NoWrap; content.Children.Add(meta);
        var open = new Button { Style = (Style)owner.Resources["CatalogCardButton"], Content = content, ToolTip = item.Name, Tag = home ? item : null };
        System.Windows.Automation.AutomationProperties.SetName(open,
            (item.Kind == ContentKind.Series ? "Bölümleri aç · " : "Filmi izle · ") + item.Name);
        open.Click += (_, _) => openItem(item); wrapper.Children.Add(open);
        var overlay = new Grid { Height = PosterHeight, VerticalAlignment = VerticalAlignment.Top,
            Opacity = 0, IsHitTestVisible = false, Background = new LinearGradientBrush(new GradientStopCollection {
                new(Colors.Transparent, .4), new(System.Windows.Media.Color.FromArgb(166, 0, 0, 0), 1) }, new Point(0,0), new Point(0,1)) };
        overlay.Clip = new RectangleGeometry(new Rect(0, 0, PosterWidth, PosterHeight), 16, 16);
        overlay.RenderTransform = shift;
        overlay.Children.Add(new Border { Width = 51.2, Height = 51.2, CornerRadius = new CornerRadius(999),
            Background = Color("--ink"), Child = new SvgIcon("fs-v2-play") { Width = 19.2, Height = 19.2,
                Foreground = Color("CatalogDarkInk"), Margin = new Thickness(1.6, 0, 0, 0) } });
        wrapper.Children.Add(overlay);
        if (item.Progress is not null || home)
        {
            var progress = Progress(owner, item); progress.VerticalAlignment = VerticalAlignment.Bottom;
            if (home) { progress.Visibility = item.Progress is null ? Visibility.Hidden : Visibility.Visible; progress.Margin = new Thickness(0, 4, 0, 0); content.Children.Insert(1, progress); } else poster.Children.Add(progress);
        }
        var favoriteIcon = new SvgIcon { Width = 16, Height = 16, StrokeThickness = 1.7 };
        favoriteIcon.SetBinding(SvgIcon.IconProperty, new Binding(nameof(ContentItem.IsFavorite)) {
            Converter = (IValueConverter)owner.Resources["CatalogFavoriteIcon"] });
        favoriteIcon.SetBinding(SvgIcon.ForegroundProperty, new Binding(nameof(Button.Foreground)) { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1) });
        var favorite = new Button { Style = (Style)owner.Resources["CatalogFavorite"], Content = favoriteIcon, Tag = home ? new HomeFavoriteAction(item) : null,
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 8, 8, 0), ToolTip = "Favoriyi değiştir", RenderTransform = shift };
        System.Windows.Automation.AutomationProperties.SetName(favorite, "Favoriyi değiştir · " + item.Name);
        favorite.Click += (_, e) => { e.Handled = true; favoriteItem(item); }; wrapper.Children.Add(favorite);
        void Reveal(bool show)
        {
            var duration = TimeSpan.FromSeconds(SystemParameters.ClientAreaAnimation ? .25 : 0);
            shift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(show ? -4.8 : 0, duration));
            if (!home) shadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty, new DoubleAnimation(show ? 36 : 10, duration));
            if (!home) shadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.ShadowDepthProperty, new DoubleAnimation(show ? 16 : 2, duration));
            if (!home) shadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, new DoubleAnimation(show ? 2d / 3 : .4, duration));
            overlay.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(show ? 1 : 0,
                TimeSpan.FromSeconds(SystemParameters.ClientAreaAnimation ? .2 : 0)));
        }
        wrapper.MouseEnter += (_, _) => Reveal(true);
        wrapper.MouseLeave += (_, _) => Reveal(wrapper.IsKeyboardFocusWithin);
        wrapper.IsKeyboardFocusWithinChanged += (_, _) => Reveal(wrapper.IsMouseOver || wrapper.IsKeyboardFocusWithin);
        return wrapper;
    }

    private static ProgressBar Progress(FrameworkElement owner, ContentItem item) => new() { Height = 4, Minimum = 0, Maximum = 1,
        Value = item.Progress?.Fraction ?? 0, Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255)),
        Foreground = (Brush)owner.FindResource("--acc"), BorderThickness = new Thickness(0), IsHitTestVisible = false };

    internal static Grid Poster(FrameworkElement owner, ContentItem item, double width, double height, int decodeWidth = 400)
    {
        Brush Color(string key) => (Brush)owner.FindResource(key);
        TextBlock Text(string value, double size, string key = "--ink") => Label(owner, value, size, key);
        var poster = new Grid { Width = width, Height = height, Clip = new RectangleGeometry(new Rect(0, 0, width, height), 16, 16) };
        var logo = new ChannelLogo { DataContext = item, Initials = "", DecodeWidth = decodeWidth,
            ImageStretch = Stretch.UniformToFill, ImagePadding = new Thickness(0) };
        logo.SetBinding(ChannelLogo.UrlProperty, new Binding(nameof(ContentItem.Logo)) { Source = item });
        poster.Children.Add(logo);
        var placeholder = new Grid { Background = Color("--p2"), IsHitTestVisible = false };
        placeholder.Children.Add(new Rectangle { Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(26,255,255,255)),
            StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 3, 3 }, RadiusX = 16, RadiusY = 16 });
        var labels = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14.4) };
        labels.Children.Add(new SvgIcon("catalog-film") { Width = 28.8, Height = 28.8, StrokeThickness = 1.7,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75,85,90)), HorizontalAlignment = HorizontalAlignment.Center });
        var title = Text(item.Name, 14.4, "--mut"); title.FontWeight = FontWeights.SemiBold;
        title.TextAlignment = TextAlignment.Center; title.Margin = new Thickness(0, 9.6, 0, 0);
        labels.Children.Add(title); placeholder.Children.Add(labels); poster.Children.Add(placeholder);
        // Observe only the existing image loader's visual output; no separate network request.
        var image = logo.Children.OfType<Image>().Single();
        var descriptor = DependencyPropertyDescriptor.FromProperty(Image.SourceProperty, typeof(Image));
        void Refresh(object? sender, EventArgs e) => placeholder.Visibility = logo.HasImage ? Visibility.Collapsed : Visibility.Visible;
        bool observing = false;
        poster.Loaded += (_, _) => { if (!observing) { descriptor.AddValueChanged(image, Refresh); observing = true; } Refresh(null, EventArgs.Empty); };
        poster.Unloaded += (_, _) => { if (observing) { descriptor.RemoveValueChanged(image, Refresh); observing = false; } };
        return poster;
    }

}






