using System.Windows;
using System.Windows.Controls;

namespace WXPlayer.App;

public partial class MainWindow
{
    private bool _catalogShellAttached;
    private Style? _catalogSourcePreviousStyle;
    private readonly Dictionary<Button, Style?> _catalogActionPreviousStyles = new();
    private Thickness _catalogActionPreviousMargin;

    // Only moves shell chrome; all original controls, selections and handlers are retained.
    // VideoBorder, ControlsBorder, fullscreen HWND ownership and playback state are untouched.
    private void SetCatalogAppearance()
    {
        if (_catalog is null) return;
        bool visible = CatalogVisible;
        if (visible && !_catalogShellAttached)
        {
            _catalogSourcePreviousStyle = SourcePicker.Style;
            _catalogActionPreviousMargin = CatalogShellActions.Margin;
            TopBar.Children.Remove(SourcePicker); TopBar.Children.Remove(CatalogShellActions);
            SourcePicker.Style = (Style)_catalog.Resources["CatalogSource"];
            CatalogShellActions.Margin = new Thickness(0);
            foreach (var button in CatalogShellActions.Children.OfType<Button>())
            {
                _catalogActionPreviousStyles[button] = button.Style;
                button.Style = (Style)_catalog.Resources[ReferenceEquals(button, AddSourceButton) ? "CatalogAddSource" : "CatalogIconButton"];
            }
            _catalog.AttachShell(SourcePicker, CatalogShellActions,
                () => MovieNav.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)),
                () => SeriesNav.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            _catalogShellAttached = true;
        }
        else if (!visible && _catalogShellAttached)
        {
            _catalog.DetachShell();
            SourcePicker.Style = _catalogSourcePreviousStyle;
            CatalogShellActions.Margin = _catalogActionPreviousMargin;
            foreach (var pair in _catalogActionPreviousStyles) pair.Key.Style = pair.Value;
            _catalogActionPreviousStyles.Clear();
            TopBar.Children.Add(SourcePicker); TopBar.Children.Add(CatalogShellActions);
            _catalogShellAttached = false;
        }
        if (visible)
        {
            TopBar.Visibility = Visibility.Collapsed;
            MainArea.Margin = new Thickness(0);
            CatalogHost.Margin = new Thickness(0);
            // The persistent status/cancel strip is retained below the content.
            BottomBar.Margin = new Thickness(35.2, 0, 35.2, 8);
            _catalog.UpdatePresentationScale();
        }
        else if (!_fullscreen)
        {
            TopBar.Visibility = Visibility.Visible;
            bool narrow = ActualWidth < 1180 || ActualHeight < 780;
            MainArea.Margin = new Thickness(narrow ? 20 : 32, 26, narrow ? 20 : 32, 18);
            CatalogHost.Margin = new Thickness(0, 24, 0, 0);
            BottomBar.Margin = new Thickness(0, 14, 0, 0);
        }
    }
}
