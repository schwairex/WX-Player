using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;

namespace WXPlayer.App;

public partial class MainWindow
{
    private bool EmbeddedPlayerVisible => _section != "home" && !CatalogVisible && !_fullscreen;
    internal bool SmokeEmbeddedPlayerReady => EmbeddedPlayerVisible
        ? _liveShellAttached && _liveControls?.Owner == this && Window.GetWindow(ControlsBorder) == _liveControls
        : !_liveShellAttached && ControlsBorder.Parent == ViewingPanel;
    private TextBlock? _embeddedLibraryCount;
    // Presentation only: reuse original controls and their events; restore before Home,
    // Catalog, fullscreen or shutdown take ownership. No playback/data state is written.
    private bool _liveShellAttached, _livePositioning;
    private Window? _liveControls;
    private Grid? _liveOverlayRoot;
    private Popup? _liveGearPopup;
    private TextBlock? _liveProgrammeCaption;
    private readonly List<Action> _liveRestore = new();
    private readonly HashSet<(DependencyObject, DependencyProperty)> _liveCaptured = new();
    private double LiveScale => Math.Clamp(.005 * ActualWidth + 8, 14, 20) / 16;
    private Brush LiveBrush(string key) => (Brush)FindResource(key);
    private Style LiveStyle(string key) => (Style)FindResource(key);

    private void InitializeLiveAppearance()
    {
        LocationChanged += LiveLocationChanged;
        StateChanged += LiveLocationChanged;
        LayoutUpdated += LiveLayoutUpdated;
        PreviewKeyDown += LiveVisualKeyDown;
        Closed += LiveOwnerClosed;
    }
    private void LiveOwnerClosed(object? sender, EventArgs e)
    {
        LocationChanged -= LiveLocationChanged; StateChanged -= LiveLocationChanged;
        LayoutUpdated -= LiveLayoutUpdated; PreviewKeyDown -= LiveVisualKeyDown;
        Closed -= LiveOwnerClosed;
        DetachLiveAppearance();
    }
    private void LiveLocationChanged(object? sender, EventArgs e) => PositionLiveControls();
    private void LiveLayoutUpdated(object? sender, EventArgs e)
    {
        if (!_liveShellAttached || _livePositioning || _closing) return;
        PositionLiveControls(); UpdateLiveVisuals();
        // Existing EPG visibility logic still updates the original fields. The same
        // current programme is presented in the horizontal card and video caption.
        if (GuideNowPanel.Visibility != Visibility.Collapsed) GuideNowPanel.Visibility = Visibility.Collapsed;
    }
    private void LiveVisualKeyDown(object sender, KeyEventArgs e)
    {
        if (!_liveShellAttached || e.Handled) return;
        if (e.Key == Key.Escape && _liveGearPopup?.IsOpen == true) { _liveGearPopup.IsOpen = false; e.Handled = true; return; }
        if (e.Key == Key.Oem2 && Keyboard.Modifiers == ModifierKeys.None && Keyboard.FocusedElement is not TextBoxBase and not PasswordBox and not ComboBox)
        { SearchBox.Focus(); SearchBox.SelectAll(); e.Handled = true; }
    }
    private void LiveOverlayKeyDown(object sender, KeyEventArgs e)
    {
        LiveVisualKeyDown(sender, e);
        if (e.Handled || Keyboard.FocusedElement is ToggleButton && e.Key is Key.Space or Key.Enter) return;
        Window_KeyDown(sender, e);
    }
    private void LiveControlsClosing(object? sender, CancelEventArgs e)
    {
        if (!_liveShellAttached || _closing) return;
        e.Cancel = true;
        // Alt+F4 must use the original owner shutdown/recording-confirmation flow.
        Dispatcher.BeginInvoke(() => { if (_liveShellAttached && !_closing) Close(); });
    }
    private static bool LiveInteractiveOrigin(DependencyObject? origin)
    {
        while (origin is not null)
        {
            if (origin is ButtonBase or Slider or TextBoxBase or ComboBox) return true;
            origin = origin is Visual ? VisualTreeHelper.GetParent(origin) : LogicalTreeHelper.GetParent(origin);
        }
        return false;
    }
    private void LiveOverlayPointerDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2 && !LiveInteractiveOrigin(e.OriginalSource as DependencyObject))
        { Fullscreen_Click(sender, new RoutedEventArgs()); e.Handled = true; }
    }
    private void LiveOverlayWheel(object sender, MouseWheelEventArgs e)
    {
        if (LiveInteractiveOrigin(e.OriginalSource as DependencyObject)) return;
        VolumeSlider.Value += e.Delta > 0 ? 5 : -5; e.Handled = true;
    }
    private void DetachLiveShellIfNeeded()
    {
        if (_liveShellAttached && !EmbeddedPlayerVisible) DetachLiveAppearance();
    }
    private void DetachLiveAppearance()
    {
        if (!_liveShellAttached) return;
        _liveShellAttached = false;
        if (_liveGearPopup is not null) _liveGearPopup.IsOpen = false;
        if (_liveControls is { } controls)
        {
            controls.PreviewKeyDown -= LiveOverlayKeyDown; controls.Closing -= LiveControlsClosing; controls.DpiChanged -= LiveLocationChanged;
            controls.PreviewMouseDown -= LiveOverlayPointerDown; controls.PreviewMouseWheel -= LiveOverlayWheel;
            controls.Content = null; controls.Close(); _liveControls = null;
        }
        // Restore parents before visual DPs, in reverse insertion order.
        for (int i = _liveRestore.Count - 1; i >= 0; i--) _liveRestore[i]();
        _liveRestore.Clear(); _liveCaptured.Clear(); _liveOverlayRoot = null; _liveGearPopup = null; _liveProgrammeCaption = null; _embeddedLibraryCount = null;
    }
    private void LiveSet(DependencyObject target, DependencyProperty property, object value)
    {
        if (_liveCaptured.Add((target, property)))
        {
            var binding = BindingOperations.GetBindingBase(target, property);
            var local = target.ReadLocalValue(property);
            _liveRestore.Add(() => { if (binding is not null) BindingOperations.SetBinding(target, property, binding); else if (local == DependencyProperty.UnsetValue) target.ClearValue(property); else target.SetValue(property, local); });
        }
        target.SetValue(property, value);
    }
    private void LiveMove(FrameworkElement element, Panel target)
    {
        var parent = (Panel)element.Parent; int index = parent.Children.IndexOf(element);
        parent.Children.Remove(element); target.Children.Add(element);
        _liveRestore.Add(() => { target.Children.Remove(element); parent.Children.Insert(Math.Min(index, parent.Children.Count), element); });
    }
    private void LiveScrollbars(FrameworkElement element)
    {
        bool existing = element.Resources.Contains(typeof(ScrollBar)); object? previous = existing ? element.Resources[typeof(ScrollBar)] : null;
        element.Resources[typeof(ScrollBar)] = LiveStyle("LiveScrollBar");
        _liveRestore.Add(() => { if (existing) element.Resources[typeof(ScrollBar)] = previous; else element.Resources.Remove(typeof(ScrollBar)); });
    }
    private void LiveAdd(Panel parent, UIElement child) { parent.Children.Add(child); _liveRestore.Add(() => parent.Children.Remove(child)); }
    private void LiveButtonAppearance(Button button, string style, double icon = 20.8)
    {
        LiveSet(button, FrameworkElement.StyleProperty, LiveStyle(style));
        // Local values from the old templates otherwise override the explicit styles.
        foreach (var property in new[] { Control.PaddingProperty, FrameworkElement.MarginProperty, Control.BackgroundProperty, Control.BorderBrushProperty, Control.BorderThicknessProperty, Control.ForegroundProperty, FrameworkElement.MinWidthProperty, FrameworkElement.MinHeightProperty, Control.FontSizeProperty })
        {
            if (_liveCaptured.Add((button, property))) { var local = button.ReadLocalValue(property); _liveRestore.Add(() => { if (local == DependencyProperty.UnsetValue) button.ClearValue(property); else button.SetValue(property, local); }); }
            button.ClearValue(property);
        }
        LiveSet(button, AutomationProperties.NameProperty, button.ToolTip?.ToString() ?? "");
        if (button.Content is SvgIcon glyph) { LiveSet(glyph, FrameworkElement.WidthProperty, icon); LiveSet(glyph, FrameworkElement.HeightProperty, icon); LiveSet(glyph, Control.ForegroundProperty, button.Foreground); }
    }
    private void SetLiveAppearance()
    {
        if (!EmbeddedPlayerVisible || _closing) return;
        if (!_liveShellAttached)
        {
            if (!ViewingPanel.Children.Contains(ControlsBorder)) return; // fullscreen has not yet restored its controls
            _liveShellAttached = true;
            AttachLiveChrome(); AttachLiveGuide(); AttachLiveControls();
        }
        ApplyLiveMetrics(); UpdateLiveVisuals();
    }
    private void AttachLiveChrome()
    {
        LiveSet(this, Control.FontFamilyProperty, FindResource("LiveFont")); LiveSet(this, Control.FontWeightProperty, FontWeights.Medium);
        LiveSet(this, TextOptions.TextFormattingModeProperty, TextFormattingMode.Ideal);
        LiveSet(this, TextOptions.TextRenderingModeProperty, TextRenderingMode.ClearType);
        LiveSet(Root, Panel.BackgroundProperty, LiveBrush("LiveBg"));
        LiveSet(MainArea, FrameworkElement.MarginProperty, new Thickness(0));
        LiveSet(ContentGrid, FrameworkElement.MarginProperty, new Thickness(0));
        LiveSet(ContentGrid, Grid.RowProperty, 0); LiveSet(ContentGrid, Grid.RowSpanProperty, 2);
        var headerRow = new RowDefinition { Height = new GridLength(70.4) }; var bodyRow = new RowDefinition();
        ContentGrid.RowDefinitions.Add(headerRow); ContentGrid.RowDefinitions.Add(bodyRow);
        _liveRestore.Add(() => { ContentGrid.RowDefinitions.Remove(headerRow); ContentGrid.RowDefinitions.Remove(bodyRow); });
        LiveMove(TopBar, ContentGrid); LiveSet(TopBar, Grid.ColumnProperty, 2); LiveSet(TopBar, Grid.RowProperty, 0);
        LiveSet(LibraryPanel, Grid.RowSpanProperty, 2); LiveSet(ViewingPanel, Grid.RowProperty, 1);
        LiveSet(ViewingPanel, FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 19.2));
        LiveSet(TopBar, FrameworkElement.MarginProperty, new Thickness(0)); LiveSet(TopBar, FrameworkElement.HeightProperty, 70.4);
        LiveSet(TopBar, FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        LiveSet(SidebarToggle, UIElement.VisibilityProperty, Visibility.Collapsed); LiveSet(ShellGreeting, UIElement.VisibilityProperty, Visibility.Collapsed);
        LiveSet(PageTitle, FrameworkElement.MarginProperty, new Thickness(0)); LiveSet(PageTitle, Control.FontSizeProperty, 20d); LiveSet(PageTitle, TextBlock.FontWeightProperty, FontWeights.ExtraBold);
        LiveSet(PageTitle, TextBlock.ForegroundProperty, LiveBrush("LiveInk"));
        var titleStack = (StackPanel)PageTitle.Parent;
        LiveSet(titleStack, StackPanel.OrientationProperty, Orientation.Horizontal); LiveSet(titleStack, FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        var count = new TextBlock { FontSize = 14.4, Foreground = LiveBrush("LiveMuted"), Margin = new Thickness(9.6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        _embeddedLibraryCount = count; LiveAdd(titleStack, count);
        LiveSet(SourcePicker, FrameworkElement.StyleProperty, LiveStyle("LiveSource")); LiveSet(CatalogShellActions, FrameworkElement.MarginProperty, new Thickness(12.8, 0, 0, 0));
        foreach (var button in CatalogShellActions.Children.OfType<Button>())
        { LiveButtonAppearance(button, ReferenceEquals(button, AddSourceButton) ? "LiveAddSource" : "LiveHeaderButton"); LiveSet(button, FrameworkElement.MarginProperty, new Thickness(0, 0, 3.2, 0)); }
        LiveSet(TopBar.ColumnDefinitions[1], ColumnDefinition.WidthProperty, new GridLength(170));
        var librarySurface = LibraryPanel.Children.OfType<Border>().First();
        LiveSet(librarySurface, Border.BackgroundProperty, LiveBrush("LivePanel")); LiveSet(librarySurface, Border.CornerRadiusProperty, new CornerRadius(0));
        LiveSet(librarySurface, Border.BorderBrushProperty, LiveBrush("LiveLine")); LiveSet(librarySurface, Border.BorderThicknessProperty, new Thickness(0, 0, 1, 0));
        LiveSet(FilterBar, FrameworkElement.MarginProperty, new Thickness(16, 17.6, 16, 9.6));
        LiveSet((FrameworkElement)ListTitle.Parent, UIElement.VisibilityProperty, Visibility.Collapsed); LiveSet(CategoryCaption, UIElement.VisibilityProperty, Visibility.Collapsed);
        LiveSet(SearchBorder, FrameworkElement.HeightProperty, 41.6); LiveSet(SearchBorder, Border.CornerRadiusProperty, new CornerRadius(14.4));
        LiveSet(SearchBorder, Border.BackgroundProperty, LiveBrush("LiveRaised")); LiveSet(SearchBorder, Border.BorderBrushProperty, LiveBrush("LiveLine"));
        LiveSet(SearchBox, Control.FontSizeProperty, 14.4); LiveSet(SearchBox, Control.ForegroundProperty, LiveBrush("LiveInk")); LiveSet(SearchHint, TextBlock.FontSizeProperty, 14.4);
        LiveSet(SearchHint, TextBlock.TextProperty, "Kanal ara"); LiveSet(SearchHint, TextBlock.ForegroundProperty, LiveBrush("LiveMuted"));
        LiveSet(CategoryPicker, FrameworkElement.StyleProperty, LiveStyle("LiveCombo")); LiveSet(CategoryPicker, FrameworkElement.HeightProperty, 41.6); LiveSet(CategoryPicker, FrameworkElement.MarginProperty, new Thickness(0, 9.6, 0, 0));
        LiveScrollbars(ChannelList); LiveScrollbars(EpgList);
        LiveSet(ChannelList, FrameworkElement.MarginProperty, new Thickness(9.6, 6.4, 8, 9.6));
        LiveSet(ChannelList, ItemsControl.ItemTemplateProperty, FindResource("LiveChannelTemplate")); LiveSet(ChannelList, ItemsControl.ItemContainerStyleProperty, LiveStyle("LiveChannelRow"));
        LiveSet(VideoBorder, Border.CornerRadiusProperty, new CornerRadius(22.4)); LiveSet(VideoBorder, Border.BackgroundProperty, Brushes.Black);
        LiveSet(VideoBorder, Border.BorderBrushProperty, LiveBrush("LiveLine")); LiveSet(VideoBorder, Border.BorderThicknessProperty, new Thickness(1));
        LiveSet(WelcomePanel, Panel.BackgroundProperty, LiveBrush("LivePanel"));
        LiveSet(BottomBar, FrameworkElement.MarginProperty, new Thickness(25.6, 0, 25.6, 8)); LiveSet(StatusText, TextBlock.ForegroundProperty, LiveBrush("LiveMuted"));
        foreach (var button in new[] { PrevPage, NextPage }) LiveButtonAppearance(button, "LiveHeaderButton", 16);
    }
    private void ApplyLiveMetrics()
    {
        if (!_liveShellAttached) return;
        double scale = LiveScale; bool narrow = ActualWidth <= 1000;
        LiveSet(MainArea, FrameworkElement.MarginProperty, new Thickness(0)); LiveSet(ContentGrid, FrameworkElement.MarginProperty, new Thickness(0));
        LiveSet(BottomBar, FrameworkElement.MarginProperty, new Thickness(25.6, 0, 25.6, 8));
        LiveSet(Root, FrameworkElement.LayoutTransformProperty, new ScaleTransform(scale, scale));
        LiveSet(ListColumn, ColumnDefinition.WidthProperty, new GridLength(narrow ? 288 : 360)); LiveSet(GapColumn, ColumnDefinition.WidthProperty, new GridLength(25.6));
        LiveSet(ViewingPanel, FrameworkElement.MarginProperty, new Thickness(0, 0, 25.6, 19.2)); LiveSet(TopBar, FrameworkElement.MarginProperty, new Thickness(0, 0, 25.6, 0));
        if (_guideHeight is null) LiveSet(GuideRow, RowDefinition.HeightProperty, new GridLength(190));
        if (_liveOverlayRoot is not null) _liveOverlayRoot.LayoutTransform = new ScaleTransform(scale, scale);
        LiveSet(VolumeSlider, FrameworkElement.WidthProperty, narrow ? 64d : 88d);
        PositionLiveControls();
    }
    private void AttachLiveGuide()
    {
        LiveSet(GuidePanel, Border.BackgroundProperty, Brushes.Transparent); LiveSet(GuidePanel, Border.CornerRadiusProperty, new CornerRadius(0));
        LiveSet(GuidePanel, Border.PaddingProperty, new Thickness(0)); LiveSet(GuidePanel, FrameworkElement.MarginProperty, new Thickness(0, 16, 0, 0));
        var grid = (Grid)GuidePanel.Child; var oldHeader = grid.Children.OfType<Grid>().First();
        LiveSet(oldHeader, UIElement.VisibilityProperty, Visibility.Collapsed);
        var header = new Grid { Margin = new Thickness(0, 0, 0, 11.2) };
        foreach (var length in new[] { GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto }) header.ColumnDefinitions.Add(new ColumnDefinition { Width = length });
        LiveMove(GuideHeading, header); LiveSet(GuideHeading, TextBlock.FontSizeProperty, 16d); LiveSet(GuideHeading, TextBlock.FontWeightProperty, FontWeights.ExtraBold); LiveSet(GuideHeading, FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        var date = (StackPanel)GuideDate.Parent; LiveMove(date, header); LiveSet(date, Grid.ColumnProperty, 1); LiveSet(date, FrameworkElement.MarginProperty, new Thickness(12.8, 0, 0, 0));
        var datePill = new CatalogPillBorder { Background = LiveBrush("LiveRaised"), CornerRadius = new CornerRadius(999), Padding = new Thickness(3.2) };
        header.Children.Remove(date); datePill.Child = date; header.Children.Add(datePill); Grid.SetColumn(datePill, 1); datePill.Margin = new Thickness(12.8, 0, 0, 0);
        _liveRestore.Add(() => { datePill.Child = null; header.Children.Add(date); });
        LiveSet(date, FrameworkElement.MarginProperty, new Thickness(0));
        foreach (var button in new[] { PreviousDayButton, NextDayButton }) { LiveButtonAppearance(button, "LiveHeaderButton", 15.2); LiveSet(button, FrameworkElement.WidthProperty, 28.8); LiveSet(button, FrameworkElement.HeightProperty, 28.8); }
        LiveSet(GuideDate, TextBlock.FontSizeProperty, 12.8); LiveSet(GuideDate, TextBlock.FontWeightProperty, FontWeights.Bold); LiveSet(GuideDate, TextBlock.ForegroundProperty, LiveBrush("LiveInk")); LiveSet(GuideDate, FrameworkElement.MarginProperty, new Thickness(9.6, 0, 9.6, 0));
        var actions = (StackPanel)MatchEpgButton.Parent; LiveMove(actions, header); LiveSet(actions, Grid.ColumnProperty, 3);
        foreach (var button in new[] { MatchEpgButton, RefreshEpgButton, ExpandGuideButton }) { LiveButtonAppearance(button, "LiveHeaderButton", 16); LiveSet(button, FrameworkElement.WidthProperty, 28.8); LiveSet(button, FrameworkElement.HeightProperty, 28.8); }
        LiveAdd(grid, header);
        LiveSet(EpgList, ItemsControl.ItemsPanelProperty, FindResource("LiveEpgPanel")); LiveSet(EpgList, ItemsControl.ItemTemplateProperty, FindResource("LiveEpgTemplate")); LiveSet(EpgList, ItemsControl.ItemContainerStyleProperty, LiveStyle("LiveEpgCard"));
        LiveSet(EpgList, ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Auto); LiveSet(EpgList, ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        LiveSet(EpgList, Control.PaddingProperty, new Thickness(0)); LiveSet(EpgList, FrameworkElement.MarginProperty, new Thickness(0));
        LiveSet(EpgEmptyPanel, Border.BackgroundProperty, LiveBrush("LivePanel")); LiveSet(EpgEmptyPanel, Border.CornerRadiusProperty, new CornerRadius(16));
        LiveSet(GuideNowPanel, UIElement.VisibilityProperty, Visibility.Collapsed);
    }
    private void AttachLiveControls()
    {
        var root = _liveOverlayRoot = new Grid { Background = Brushes.Transparent };
        var top = new DockPanel { Margin = new Thickness(28.8, 22.4, 28.8, 48), LastChildFill = true };
        var topBand = new Border { VerticalAlignment = VerticalAlignment.Top, Background = new LinearGradientBrush(Color.FromArgb(166, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), 90), Child = top };
        root.Children.Add(topBand);
        var title = new StackPanel { Margin = new Thickness(0, 0, 16, 0) }; top.Children.Add(title);
        LiveMove(NowTitle, title); LiveSet(NowTitle, TextBlock.FontSizeProperty, 20.8); LiveSet(NowTitle, TextBlock.FontWeightProperty, FontWeights.ExtraBold); LiveSet(NowTitle, TextBlock.ForegroundProperty, LiveBrush("LiveInk"));
        _liveProgrammeCaption = new TextBlock { FontSize = 13.6, Foreground = LiveBrush("LiveCaption"), Margin = new Thickness(0, 2.4, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis };
        _liveProgrammeCaption.SetBinding(TextBlock.TextProperty, new Binding("Text") { Source = GuideNowTitle }); title.Children.Add(_liveProgrammeCaption);
        LiveSet(HealthBadge, UIElement.VisibilityProperty, Visibility.Collapsed);
        var bottom = new Border { VerticalAlignment = VerticalAlignment.Bottom, Background = new LinearGradientBrush(Color.FromArgb(0, 0, 0, 0), Color.FromArgb(209, 0, 0, 0), 90), Padding = new Thickness(28.8, 64, 28.8, 19.2) }; root.Children.Add(bottom);
        var bottomStack = new StackPanel(); bottom.Child = bottomStack;
        var timeline = new Grid(); timeline.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); timeline.ColumnDefinitions.Add(new ColumnDefinition()); timeline.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); bottomStack.Children.Add(timeline);
        LiveMove(PlaybackBadge, timeline); LiveSet(PlaybackBadge, TextBlock.FontSizeProperty, 12.48); LiveSet(PlaybackBadge, TextBlock.FontWeightProperty, FontWeights.SemiBold); LiveSet(PlaybackBadge, TextBlock.ForegroundProperty, LiveBrush("LiveCaption")); LiveSet(PlaybackBadge, FrameworkElement.MarginProperty, new Thickness(0, 0, 14.4, 0));
        LiveMove(SeekSlider, timeline); LiveSet(SeekSlider, Grid.ColumnProperty, 1); LiveSet(SeekSlider, FrameworkElement.StyleProperty, LiveStyle("LiveSlider")); LiveSet(SeekSlider, FrameworkElement.HeightProperty, 24d); LiveSet(SeekSlider, FrameworkElement.MarginProperty, new Thickness(0));
        LiveMove(LiveEdgeButton, timeline); LiveSet(LiveEdgeButton, Grid.ColumnProperty, 2); LiveButtonAppearance(LiveEdgeButton, "LiveButton"); LiveSet(LiveEdgeButton, FrameworkElement.MinWidthProperty, 99.2); LiveSet(LiveEdgeButton, Control.FontSizeProperty, 11.52); LiveSet(LiveEdgeButton, Control.FontWeightProperty, FontWeights.ExtraBold); LiveSet(LiveEdgeButton, Control.ForegroundProperty, LiveBrush("LiveOnAir")); LiveSet(LiveEdgeButton, FrameworkElement.MarginProperty, new Thickness(14.4, 0, 0, 0));
        LiveMove(ControlsBorder, bottomStack); LiveSet(ControlsBorder, Border.BackgroundProperty, Brushes.Transparent); LiveSet(ControlsBorder, Border.CornerRadiusProperty, new CornerRadius(0)); LiveSet(ControlsBorder, Border.PaddingProperty, new Thickness(0));
        foreach (var button in new[] { PreviousChannelButton, NextChannelButton, MuteButton, FullscreenButton }) LiveButtonAppearance(button, "LiveIconButton");
        LiveButtonAppearance(PlayButton, "LivePlayButton", 23.2);
        LiveSet(VolumeSlider, FrameworkElement.StyleProperty, LiveStyle("LiveSlider")); LiveSet(VolumeSlider, Control.ForegroundProperty, LiveBrush("LiveAccent")); LiveSet(VolumeSlider, FrameworkElement.HeightProperty, 24d); LiveSet(VolumeLabel, UIElement.VisibilityProperty, Visibility.Collapsed);
        var menuActions = new StackPanel();
        foreach (var button in new[] { RecordButton, MiniPlayerButton, TracksButton, FitButton, StatisticsButton }) { LiveMove(button, menuActions); LiveButtonAppearance(button, "LiveMenuAction", 18.4); }
        var toggle = new ToggleButton { Width = 44.8, Height = 44.8, Content = new SvgIcon { Icon = "settings", Width = 20.8, Height = 20.8 }, ToolTip = "Oynatıcı seçenekleri", Foreground = LiveBrush("LiveInk") };
        AutomationProperties.SetName(toggle, "Oynatıcı seçenekleri");
        // A ToggleButton template is separate from Button to retain native keyboard toggling.
        toggle.Style = LiveStyle("LiveGearButton");
        var rightButtons = (StackPanel)FullscreenButton.Parent; rightButtons.Children.Insert(0, toggle); _liveRestore.Add(() => rightButtons.Children.Remove(toggle));
        _liveGearPopup = new Popup { PlacementTarget = toggle, Placement = PlacementMode.Top, VerticalOffset = -12.8, AllowsTransparency = true, StaysOpen = false, Child = new Border { Background = LiveBrush("LiveGlass"), BorderBrush = LiveBrush("LiveLine"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(17.6), Padding = new Thickness(12.8), Width = 272, Child = menuActions } };
        _liveGearPopup.SetBinding(Popup.IsOpenProperty, new Binding("IsChecked") { Source = toggle, Mode = BindingMode.TwoWay });
        TextElement.SetFontFamily(_liveGearPopup.Child, (FontFamily)FindResource("LiveFont")); TextElement.SetForeground(_liveGearPopup.Child, LiveBrush("LiveInk"));
        TextOptions.SetTextFormattingMode(_liveGearPopup, TextFormattingMode.Ideal); TextOptions.SetTextRenderingMode(_liveGearPopup, TextRenderingMode.ClearType);
        root.Children.Add(_liveGearPopup);
        root.Children.Add(new LiveVideoCorners { Fill = LiveBrush("LiveBg"), IsHitTestVisible = false });
        _liveControls = new Window { Title = "WX Player live controls", Owner = this, WindowStyle = WindowStyle.None, AllowsTransparency = true, Background = Brushes.Transparent, ShowInTaskbar = false, ShowActivated = false, ResizeMode = ResizeMode.NoResize, Content = root, FontFamily = (FontFamily)FindResource("LiveFont"), Foreground = LiveBrush("LiveInk") };
        TextOptions.SetTextFormattingMode(_liveControls, TextFormattingMode.Ideal); TextOptions.SetTextRenderingMode(_liveControls, TextRenderingMode.ClearType);
        _liveControls.PreviewKeyDown += LiveOverlayKeyDown; _liveControls.Closing += LiveControlsClosing; _liveControls.DpiChanged += LiveLocationChanged;
        _liveControls.PreviewMouseDown += LiveOverlayPointerDown; _liveControls.PreviewMouseWheel += LiveOverlayWheel;
        var bridge = new Control { Width = 1, Height = 1, Opacity = 0, Focusable = true, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, FocusVisualStyle = null };
        AutomationProperties.SetName(bridge, "Oynatıcı kontrolleri");
        KeyboardFocusChangedEventHandler enter = (_, _) => { if (_liveControls?.IsVisible == true) { _liveControls.Activate(); PreviousChannelButton.Focus(); } };
        bridge.GotKeyboardFocus += enter;
        ViewingPanel.Children.Insert(1, bridge);
        _liveRestore.Add(() => { bridge.GotKeyboardFocus -= enter; ViewingPanel.Children.Remove(bridge); });
        KeyEventHandler tab = (_, e) =>
        {
            if (e.Key != Key.Tab || _liveGearPopup?.IsOpen == true) return;
            bool reverse = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            if (!reverse && ReferenceEquals(Keyboard.FocusedElement, FullscreenButton))
            {
                Activate();
                bool focused = _seriesPanel?.IsVisible == true
                    ? _seriesPanel.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))
                    : PreviousDayButton.IsVisible && PreviousDayButton.Focus();
                if (!focused) focused = ChannelList.Focus();
                e.Handled = focused;
            }
            else if (reverse && ReferenceEquals(Keyboard.FocusedElement, PreviousChannelButton)) { Activate(); ChannelList.Focus(); e.Handled = true; }
        };
        _liveControls.PreviewKeyDown += tab;
        var tabWindow = _liveControls; _liveRestore.Add(() => tabWindow.PreviewKeyDown -= tab);
        PositionLiveControls();
    }
    private void UpdateLiveVisuals()
    {
        if (!_liveShellAttached) return;
        if (_embeddedLibraryCount is not null)
        {
            _embeddedLibraryCount.Text = _section == "live" ? LiveCount.Text + " kanal" : ResultsCount.Text;
            _embeddedLibraryCount.Visibility = ActualWidth <= 1000 ? Visibility.Collapsed : Visibility.Visible;
        }
        LiveSet(SearchHint, TextBlock.TextProperty, _section is "live" or "epg" ? "Kanal ara" : "Bu kütüphanede ara");
        HealthBadge.Visibility = Visibility.Collapsed;
        if (_liveProgrammeCaption is not null) _liveProgrammeCaption.Visibility = _guideNow is null ? Visibility.Collapsed : Visibility.Visible;
        // Mirrors existing replay state, without changing the GoLive command or value.
        if (_engine is not null && _engine.IsReplay)
        { LiveSet(LiveEdgeButton, Control.BackgroundProperty, LiveBrush("LiveInk")); LiveSet(LiveEdgeButton, Control.ForegroundProperty, LiveBrush("LiveDarkInk")); LiveSet(LiveEdgeButton, Control.PaddingProperty, new Thickness(12.8, 5.6, 12.8, 5.6)); }
        else { LiveSet(LiveEdgeButton, Control.BackgroundProperty, Brushes.Transparent); LiveSet(LiveEdgeButton, Control.ForegroundProperty, LiveBrush("LiveOnAir")); LiveSet(LiveEdgeButton, Control.PaddingProperty, new Thickness(0)); }
    }
    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    private static extern bool LiveSetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
    private void PositionLiveControls()
    {
        if (!_liveShellAttached || _liveControls is null || _livePositioning || _closing || !IsLoaded) return;
        _livePositioning = true;
        try
        {
            if (!IsVisible || WindowState == WindowState.Minimized || !VideoBorder.IsVisible || VideoBorder.ActualWidth < 1 || VideoBorder.ActualHeight < 1) { _liveControls.Hide(); return; }
            var a = VideoBorder.PointToScreen(new Point(1, 1)); var b = VideoBorder.PointToScreen(new Point(VideoBorder.ActualWidth - 1, VideoBorder.ActualHeight - 1));
            var hwnd = new WindowInteropHelper(_liveControls).EnsureHandle();
            var actual = FullscreenPlacement.WindowBounds(_liveControls);
            int x = (int)Math.Round(a.X), y = (int)Math.Round(a.Y), width = Math.Max(1, (int)Math.Round(b.X - a.X)), height = Math.Max(1, (int)Math.Round(b.Y - a.Y));
            // Position the visual overlay in physical pixels, as fullscreen already
            // does. Owner/overlay can temporarily have different DPIs while moving.
            if (actual.Left != x || actual.Top != y || actual.Width != width || actual.Height != height)
                LiveSetWindowPos(hwnd, IntPtr.Zero, x, y, width, height, 0x0010 | 0x0004);
            if (!_liveControls.IsVisible) _liveControls.Show();
        }
        finally { _livePositioning = false; }
    }
}

// Masks only the native video corners, not text or control containers. No blur,
// bitmap cache, opacity mask, timer or rendering-HWND ownership changes.
internal sealed class LiveVideoCorners : FrameworkElement
{
    internal Brush Fill { get; init; } = Brushes.Black;
    protected override void OnRender(DrawingContext dc)
    {
        var rect = new Rect(new Point(), RenderSize);
        var corners = Geometry.Combine(new RectangleGeometry(rect), new RectangleGeometry(rect, 22.4, 22.4), GeometryCombineMode.Exclude, null);
        dc.DrawGeometry(Fill, null, corners);
    }
}
