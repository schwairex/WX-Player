using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WXPlayer.App;

internal sealed partial class SettingsWindow
{
    private readonly ScrollViewer _settingsScroll = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    private Grid? _settingsKeys;
    private bool _settingsKeysCompact;
    private Style SettingsStyle(string key) => (Style)FindResource(key);
    private Brush SettingsBrush(string key) => (Brush)FindResource(key);
    private void InitializeSettingsAppearance()
    {
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("SettingsTheme.xaml", UriKind.Relative) });
        FontFamily = (FontFamily)FindResource("LiveFont"); FontWeight = FontWeights.Medium; Foreground = SettingsBrush("LiveInk");
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Ideal); TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
        if (Content is Border surface) surface.Background = SettingsBrush("LivePanel");
        _settingsScroll.Padding = new Thickness(28.8, 22.4, 28.8, 32);
        _settingsScroll.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = FindResource("LiveScrollBar");
        AutomationProperties.SetName(_settingsScroll, "Ayarlar");
        void Scale()
        {
            double scale = Math.Clamp(.005 * ActualWidth + 8, 14, 20) / 16;
            Body.LayoutTransform = new ScaleTransform(scale, scale); Footer.LayoutTransform = new ScaleTransform(scale, scale);
            UpdateShortcutLayout();
        }
        Loaded += (_, _) => Scale(); SizeChanged += (_, _) => Scale();
    }
    private void UpdateShortcutLayout()
    {
        bool compact = ActualWidth <= 720;
        if (_settingsKeys is null || compact == _settingsKeysCompact) return;
        _settingsKeysCompact = compact;
        _settingsKeys.ColumnDefinitions[1].Width = new GridLength(compact ? 0 : 25.6);
        _settingsKeys.ColumnDefinitions[2].Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        _settingsKeys.RowDefinitions.Clear(); int count = _settingsKeys.Children.Count;
        for (int i = 0; i < (compact ? count : (count + 1) / 2); i++) _settingsKeys.RowDefinitions.Add(new() { Height = GridLength.Auto });
        for (int i = 0; i < count; i++)
        {
            var row = (Border)_settingsKeys.Children[i]; Grid.SetRow(row, compact ? i : i / 2); Grid.SetColumn(row, compact ? 0 : i % 2 == 0 ? 0 : 2);
            row.BorderThickness = new Thickness(0, 0, 0, i < count - (compact ? 1 : 2) ? 1 : 0);
        }
    }
    private StackPanel SettingsCard(Panel parent, string title, string? subtitle = null)
    {
        var group = new StackPanel();
        group.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.ExtraBold, TextWrapping = TextWrapping.Wrap });
        if (subtitle is not null) group.Children.Add(new TextBlock { Text = subtitle, FontSize = 13.12, Foreground = SettingsBrush("LiveMuted"), Margin = new Thickness(0, 4, 0, 12.8), LineHeight = 18.37, TextWrapping = TextWrapping.Wrap });
        else group.Children.Add(new Border { Height = 12.8 });
        var content = new StackPanel(); group.Children.Add(new Border { Style = SettingsStyle("SettingsCard"), Child = content });
        parent.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 28.8), Child = group });
        return content;
    }
    private Grid SettingsRow(Panel parent, string? label, FrameworkElement control, string? description = null)
    {
        var row = new Grid { MinHeight = 27.2, Margin = new Thickness(17.6, 15.2, 17.6, 15.2) };
        row.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) }); row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        var labels = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 19.2, 0) };
        if (label is not null) labels.Children.Add(new TextBlock { Text = label, FontSize = 14.72, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        if (description is not null) labels.Children.Add(new TextBlock { Text = description, FontSize = 12.8, Foreground = SettingsBrush("LiveMuted"), Margin = new Thickness(0, 3.2, 0, 0), LineHeight = 17.92, TextWrapping = TextWrapping.Wrap });
        row.Children.Add(labels); Grid.SetColumn(control, 1); row.Children.Add(control); control.VerticalAlignment = VerticalAlignment.Center;
        if (control is CheckBox toggle)
        {
            labels.Cursor = Cursors.Hand;
            labels.MouseLeftButtonDown += (_, e) =>
            {
                if (!toggle.IsEnabled) return;
                toggle.Focus(); toggle.SetCurrentValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, toggle.IsChecked != true);
                toggle.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, toggle)); e.Handled = true;
            };
        }
        if (label is not null) AutomationProperties.SetName(control, label);
        parent.Children.Add(new Border { BorderBrush = SettingsBrush("LiveLine"), BorderThickness = new Thickness(0, parent.Children.Count == 0 ? 0 : 1, 0, 0), Child = row });
        return row;
    }
    private void SettingsToggle(Panel parent, CheckBox control, string? description = null)
    {
        control.Style = SettingsStyle("SettingsSwitch"); SettingsRow(parent, control.Content?.ToString(), control, description);
    }
    private Button SettingsAction(string label, Action action, bool primary = false, bool danger = false, bool small = false)
    {
        var button = Action(label, action, primary); button.Style = SettingsStyle(danger ? "SettingsDangerButton" : primary ? "SettingsPrimary" : small ? "SettingsSmallButton" : "SettingsButton");
        button.ClearValue(FrameworkElement.MinHeightProperty);
        button.Margin = new Thickness(0); AutomationProperties.SetName(button, label); return button;
    }
    private void SettingsShortcut(Grid keys, int index, string key, string label)
    {
        if (index % 2 == 0) keys.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var row = new Grid { MinHeight = 23, Margin = new Thickness(0, 11.2, 0, 11.2) };
        row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        row.Children.Add(new TextBlock { Text = label, FontSize = 14.08, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        var caps = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        // Existing display text is split into keycaps; keyboard behavior is untouched.
        foreach (var part in key.Split(new[] { " / ", " + " }, StringSplitOptions.None))
            caps.Children.Add(new Border { Background = SettingsBrush("LiveHover"), BorderBrush = SettingsBrush("LiveLine"), BorderThickness = new Thickness(1, 1, 1, 2), CornerRadius = new CornerRadius(8), Padding = new Thickness(8.8, 4.8, 8.8, 4.8), MinWidth = 30.4, Margin = new Thickness(4.8, 0, 0, 0), Child = new TextBlock { Text = part, FontSize = 11.52, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center } });
        Grid.SetColumn(caps, 1); row.Children.Add(caps);
        var border = new Border { BorderBrush = SettingsBrush("LiveLine"), BorderThickness = new Thickness(0, 0, 0, index < 9 ? 1 : 0), Child = row };
        Grid.SetColumn(border, index % 2 == 0 ? 0 : 2); Grid.SetRow(border, index / 2); keys.Children.Add(border);
    }
    private void SettingsNavKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Up or Key.Down)) return;
        int index = _tabs.FindIndex(t => ReferenceEquals(t.Button, sender));
        index = (index + (e.Key == Key.Down ? 1 : -1) + _tabs.Count) % _tabs.Count;
        SelectTab(index); _tabs[index].Button.Focus(); e.Handled = true;
    }
}

// Background transition without CatalogPillBorder's automatic pill radius.
public sealed class SettingsChrome : Border
{
    private bool _changing;
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        double seconds = CatalogPillBorder.GetTransitionSeconds(this);
        if (_changing || e.Property != BackgroundProperty || !IsLoaded || !SystemParameters.ClientAreaAnimation || seconds <= 0 || e.OldValue is not SolidColorBrush oldBrush || e.NewValue is not SolidColorBrush newBrush || oldBrush.Color == newBrush.Color) return;
        var brush = new SolidColorBrush(newBrush.Color);
        _changing = true; try { SetCurrentValue(BackgroundProperty, brush); } finally { _changing = false; }
        brush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(oldBrush.Color, newBrush.Color, TimeSpan.FromSeconds(seconds)));
    }
}
