using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace WXPlayer.App;

// Visual tree only; SourceWindow owns source configuration and validation.
internal sealed partial class SourceWindow
{
    private readonly Dictionary<TextBox, TextBlock> _sourceHints = new();
    private Brush SourceBrush(string key) => (Brush)FindResource(key);
    private StackPanel SourceContent()
    {
        UtilityWindowAppearance.Apply(this);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("SourceTheme.xaml", UriKind.Relative) });
        var content = new StackPanel();
        Body.Children.Add(new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        return content;
    }
    private StackPanel SourceSection(Panel parent, string title, string description)
    {
        // Direct parent of fields includes header, description and card background.
        var group = new Grid { Margin = new Thickness(0, 0, 0, 20.8) };
        for (int i = 0; i < 3; i++) group.RowDefinitions.Add(new() { Height = GridLength.Auto });
        group.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.ExtraBold });
        var copy = new TextBlock { Text = description, FontSize = 13.12, LineHeight = 18.368, Foreground = SourceBrush("LiveMuted"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 12.8) };
        Grid.SetRow(copy, 1); group.Children.Add(copy);
        var card = new Border { Background = SourceBrush("LiveRaised"), BorderBrush = SourceBrush("LiveLine"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(17.6) };
        Grid.SetRow(card, 2); group.Children.Add(card);
        var fields = new StackPanel { Margin = new Thickness(19.2, 17.6, 19.2, 17.6) };
        Grid.SetRow(fields, 2); group.Children.Add(fields); parent.Children.Add(group);
        return fields;
    }
    private void SourceCaption(Panel parent, string label)
    {
        parent.Children.Add(new TextBlock { Text = label, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = SourceBrush("SourceLabelInk"), Margin = new Thickness(16, parent.Children.Count == 0 ? 0 : 16, 0, 8) });
    }
    private Grid SourceHint(Control field, string value)
    {
        var hint = new TextBlock { Text = value, FontSize = 15.2, Foreground = SourceBrush("SourceHintInk"), Margin = new Thickness(16, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false, TextTrimming = TextTrimming.CharacterEllipsis };
        var wrapper = new Grid(); wrapper.Children.Add(field); wrapper.Children.Add(hint);
        if (field is TextBox text)
        {
            _sourceHints[text] = hint;
            void Sync() => hint.Visibility = text.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            text.TextChanged += (_, _) => Sync(); Sync();
        }
        else if (field is PasswordBox secret)
        {
            void Sync() => hint.Visibility = secret.Password.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            secret.PasswordChanged += (_, _) => Sync(); Sync();
        }
        return wrapper;
    }
    private TextBox SourceField(Panel panel, string label, string value = "")
    {
        SourceCaption(panel, label);
        var field = new TextBox { Text = value, MinHeight = 44, Style = (Style)FindResource("SourceInput") };
        AutomationProperties.SetName(field, label);
        string hint = label switch { "KULLANICI ADI" => "Kullanıcı adınız", "MAC ADRESİ" => "00:1A:79:00:00:00", "XMLTV ADRESİ / DOSYASI" => "http://…/epg.xml veya dosya yolu", _ => "" };
        panel.Children.Add(SourceHint(field, hint)); return field;
    }
    private void SourceAddressRow(Panel fields, TextBox address, Button browse)
    {
        var wrapper = (Grid)address.Parent; fields.Children.Remove(wrapper);
        var row = new Grid(); row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        row.Children.Add(wrapper); row.Children.Add(browse); Grid.SetColumn(browse, 1);
        browse.Content = new IconLabel { Icon = "source-folder", Label = "Dosyadan seç" };
        browse.Margin = new Thickness(11.2, 0, 0, 0); fields.Children.Add(row);
    }
    private Button SourceAction(string label, Action action, bool primary = false)
    {
        var button = Action(label, action, primary); button.ClearValue(MinHeightProperty); button.Margin = new Thickness(0);
        button.Style = (Style)FindResource(primary ? "SourcePrimary" : "SourceButton");
        AutomationProperties.SetName(button, label); return button;
    }
    private StackPanel SourceFooter(TextBlock error)
    {
        Footer.Orientation = Orientation.Vertical; Footer.HorizontalAlignment = HorizontalAlignment.Stretch;
        var grid = new Grid(); grid.ColumnDefinitions.Add(new()); grid.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); Footer.Children.Add(grid);
        error.FontSize = 13.6; error.Foreground = SourceBrush("DangerBrush"); error.MaxWidth = 384; error.TextWrapping = TextWrapping.Wrap;
        var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12.8, 0) };
        row.SetBinding(VisibilityProperty, new Binding("Text") { Source = error, Converter = new ErrorVisibility() });
        row.Children.Add(new SvgIcon("source-warning") { Width = 16, Height = 16, Foreground = SourceBrush("DangerBrush"), Margin = new Thickness(0, 0, 8, 0) });
        row.Children.Add(error); grid.Children.Add(row);
        var actions = new StackPanel { Orientation = Orientation.Horizontal }; Grid.SetColumn(actions, 1); grid.Children.Add(actions);
        grid.SizeChanged += (_, _) => error.MaxWidth = Math.Min(384, Math.Max(80, grid.ColumnDefinitions[0].ActualWidth - 40));
        return actions;
    }
    private sealed class ErrorVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
