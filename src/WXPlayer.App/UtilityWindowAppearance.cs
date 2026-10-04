using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WXPlayer.App;

// Scoped styling of the existing PremiumWindow tree; no common shell/lifecycle changes.
internal static class UtilityWindowAppearance
{
    internal static Brush Brush(FrameworkElement owner, string key) => (Brush)owner.FindResource(key);
    internal static Style Style(FrameworkElement owner, string key) => (Style)owner.FindResource(key);
    internal static void Apply(PremiumWindow window, bool refreshDot = false)
    {
        window.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("UtilityTheme.xaml", UriKind.Relative) });
        window.FontFamily = (FontFamily)window.FindResource("LiveFont"); window.FontWeight = FontWeights.Medium;
        window.Foreground = Brush(window, "LiveInk"); window.Background = Brush(window, "LivePanel");
        TextOptions.SetTextFormattingMode(window, TextFormattingMode.Ideal); TextOptions.SetTextRenderingMode(window, TextRenderingMode.ClearType);
        if (window.Content is Border surface && surface.Child is Grid root)
        {
            surface.Background = Brush(window, "LivePanel");
            // Preserve the header icon/title/subtitle tree and native window controls.
            if (root.Children[0] is DockPanel head)
            {
                if (head.Children[0] is Border icon) { icon.Background = Brush(window, "SettingsSelected"); if (icon.Child is SvgIcon svg) svg.Foreground = Brush(window, "LiveAccent"); }
                if (head.Children[1] is StackPanel labels && labels.Children[1] is TextBlock subtitle)
                {
                    subtitle.Foreground = Brush(window, "LiveMuted");
                    if (refreshDot)
                    {
                        const string suffix = "Her saniye yenilenir";
                        string text = subtitle.Text;
                        subtitle.Inlines.Clear();
                        subtitle.Inlines.Add(new Run(text[..text.IndexOf(suffix, StringComparison.Ordinal)]));
                        subtitle.Inlines.Add(new InlineUIContainer(new Ellipse { Width = 7.2, Height = 7.2, Fill = Brush(window, "LiveAccent"), Margin = new Thickness(0, 0, 6.4, 0) }) { BaselineAlignment = BaselineAlignment.Center });
                        subtitle.Inlines.Add(new Run(suffix));
                    }
                }
            }
        }
        void Scale()
        {
            double scale = Math.Clamp(.005 * window.ActualWidth + 8, 14, 20) / 16;
            window.Body.LayoutTransform = new ScaleTransform(scale, scale); window.Footer.LayoutTransform = new ScaleTransform(scale, scale);
        }
        window.Loaded += (_, _) => Scale(); window.SizeChanged += (_, _) => Scale();
    }
    internal static TextBlock Text(FrameworkElement owner, string text, double size = 14.08, string color = "LiveInk") =>
        new() { Text = text, FontSize = size, Foreground = Brush(owner, color), TextWrapping = TextWrapping.Wrap };
    internal static Border Pill(FrameworkElement owner, string text, string ink = "UtilityNeutralInk", string background = "SettingsButtonBg", bool dot = false)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        if (dot) content.Children.Add(new Ellipse { Width = 7.2, Height = 7.2, Fill = Brush(owner, ink), Margin = new Thickness(0, 0, 6.4, 0), VerticalAlignment = VerticalAlignment.Center });
        var label = Text(owner, text, 11.84, ink); label.FontWeight = FontWeights.Bold; content.Children.Add(label);
        var pill = new Border { Background = Brush(owner, background), CornerRadius = new CornerRadius(999), Padding = new Thickness(12, 5.6, 12, 5.6), Child = content, VerticalAlignment = VerticalAlignment.Center };
        AutomationProperties.SetName(pill, text); return pill;
    }
    internal static Button Action(PremiumWindow owner, string text, Action callback)
    {
        var button = PremiumWindow.Action(text, callback); button.Style = Style(owner, "SettingsButton");
        button.ClearValue(FrameworkElement.MinHeightProperty); AutomationProperties.SetName(button, text); return button;
    }
}
