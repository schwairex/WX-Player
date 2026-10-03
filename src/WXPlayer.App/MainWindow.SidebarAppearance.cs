using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace WXPlayer.App;

// Selected state is presentation metadata, never a navigation destination/Tag.
public static class NavigationVisual
{
    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.RegisterAttached("IsSelected", typeof(bool), typeof(NavigationVisual), new PropertyMetadata(false));
    public static bool GetIsSelected(DependencyObject element) => (bool)element.GetValue(IsSelectedProperty);
    public static void SetIsSelected(DependencyObject element, bool value) => element.SetValue(IsSelectedProperty, value);
}

public partial class MainWindow
{
    private void SetSidebarAppearance()
    {
        if (_fullscreen || _closing) return;
        bool expanded = SidebarExpanded;
        double scale = LiveScale, rail = ActualWidth <= 1000 ? 64 : 70.4;
        // Live already scales Root. Other pages retain their own content scaling;
        // only their sidebar scales here, giving the same physical rail everywhere.
        Sidebar.LayoutTransform = _liveShellAttached ? Transform.Identity : new ScaleTransform(scale, scale);
        NavColumn.Width = new GridLength(rail * (_liveShellAttached ? 1 : scale));
        Sidebar.Width = expanded ? 232 : rail;
        Sidebar.Padding = new Thickness(expanded ? 16 : 12.8, 17.6, expanded ? 16 : 12.8, 17.6);
        BrandToggle.Width = BrandToggle.Height = 41.6;
        foreach (var button in new[] { HomeNav, LiveNav, MovieNav, SeriesNav, FavoriteNav, EpgNav, RecentNav, RecordingsNav, SettingsNav })
        {
            bool selected = button.Tag is string tag && tag == _section;
            button.Style = LiveStyle("LiveNav"); button.Height = 44.8; button.MinHeight = 0;
            button.Padding = expanded ? new Thickness(8, 0, 8, 0) : new Thickness(0);
            button.Margin = new Thickness(0, 3.2, 0, 3.2); button.FontSize = 14.4; button.FontWeight = FontWeights.SemiBold;
            button.Background = LiveBrush(selected ? "LiveNavSelected" : "LiveBg");
            button.Foreground = LiveBrush(selected ? "LiveAccent" : "LiveMuted");
            button.BorderThickness = new Thickness(0);
            button.HorizontalContentAlignment = expanded ? HorizontalAlignment.Left : HorizontalAlignment.Center;
            NavigationVisual.SetIsSelected(button, selected);
            if (button.Content is IconLabel label) { label.Compact = !expanded; button.ToolTip = label.Label; AutomationProperties.SetName(button, label.Label); }
        }
    }
}
