using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WXPlayer.App;

public partial class MainWindow
{
    private bool _homeShellAttached;
    private Panel? _homeMenuParent;
    private int _homeMenuIndex;
    private Style? _homeSourceStyle, _homeMenuStyle;
    private Thickness _homeActionMargin, _homeMenuMargin;
    private readonly Dictionary<Button, Style?> _homeActionStyles = new();
    private double _homeStatusSize;
    private Brush? _homeStatusBrush;

    private void DetachHomeShellIfNeeded()
    {
        if (!_homeShellAttached || _section == "home" && !_fullscreen) return;
        _home.DetachShell();
        SourcePicker.Style = _homeSourceStyle; SidebarToggle.Style = _homeMenuStyle; SidebarToggle.Margin = _homeMenuMargin;
        CatalogShellActions.Margin = _homeActionMargin;
        foreach (var pair in _homeActionStyles) pair.Key.Style = pair.Value;
        _homeActionStyles.Clear();
        TopBar.Children.Add(SourcePicker); TopBar.Children.Add(CatalogShellActions);
        _homeMenuParent!.Children.Insert(_homeMenuIndex, SidebarToggle);
        MainArea.Children.Add(BottomBar); BottomBar.Margin = new Thickness(0, 14, 0, 0);
        StatusText.FontSize = _homeStatusSize; StatusText.Foreground = _homeStatusBrush;
        HomeHost.Margin = new Thickness(0, 24, 0, 0); _homeShellAttached = false;
    }
    private void SetHomeAppearance()
    {
        if (_home is null || _section != "home" || _fullscreen) return;
        if (!_homeShellAttached)
        {
            _homeSourceStyle = SourcePicker.Style; _homeMenuStyle = SidebarToggle.Style;
            _homeActionMargin = CatalogShellActions.Margin; _homeMenuMargin = SidebarToggle.Margin;
            _homeMenuParent = (Panel)SidebarToggle.Parent; _homeMenuIndex = _homeMenuParent.Children.IndexOf(SidebarToggle);
            _homeStatusSize = StatusText.FontSize; _homeStatusBrush = StatusText.Foreground;
            TopBar.Children.Remove(SourcePicker); TopBar.Children.Remove(CatalogShellActions);
            _homeMenuParent.Children.Remove(SidebarToggle); MainArea.Children.Remove(BottomBar);
            SourcePicker.Style = (Style)_home.FindResource("CatalogSource"); SidebarToggle.Style = (Style)_home.FindResource("CatalogIconButton"); SidebarToggle.Margin = new Thickness(0);
            CatalogShellActions.Margin = new Thickness(0);
            foreach (var button in CatalogShellActions.Children.OfType<Button>())
            {
                _homeActionStyles[button] = button.Style;
                button.Style = (Style)_home.FindResource(ReferenceEquals(button, AddSourceButton) ? "CatalogAddSource" : "CatalogIconButton");
            }
            // These are the original controls, with their original selection, bindings and events.
            _home.AttachShell(SourcePicker, CatalogShellActions, SidebarToggle, BottomBar); _homeShellAttached = true;
        }
        TopBar.Visibility = Visibility.Collapsed; MainArea.Margin = new Thickness(0); HomeHost.Margin = new Thickness(0);
        BottomBar.Margin = new Thickness(0); StatusText.FontSize = 12; StatusText.Foreground = (Brush)_home.FindResource("--mut");
        _home.UpdatePresentationScale();
    }
}
