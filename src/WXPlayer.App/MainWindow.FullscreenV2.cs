using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WXPlayer.Core;

namespace WXPlayer.App;

public partial class MainWindow
{
    private Grid? _fullscreenRoot;
    private TextBlock? _fsTitle, _fsSubtitle, _fsBrand, _fsTime, _fsRemaining, _fsVolumeLabel, _fsPageLabel, _fsSearchHint, _fsRecordingTime;
    private SvgIcon? _fsPlayIcon, _fsMuteIcon;
    private MediaSlider? _fsSeek, _fsVolume;
    private Button? _fsLibraryButton, _fsRecordButton, _fsLiveButton, _fsNextChip, _fsSpeedButton, _fsTracksButton;
    private Border? _fsDrawer, _fsToast;
    private TextBox? _fsSearch;
    private Popup? _fsPopup;
    private bool _fsDrawerOpen, _fsEpisodeTab, _fsUpdatingItems;
    private int _fullscreenFadeVersion, _fsDrawerVersion;
    private DateTime? _fsRecordingStarted;
    private readonly Dictionary<string, Button> _fsTabs = new();
    private readonly List<FrameworkElement> _fsCompactHidden = new();
    private readonly List<Action> _fsUnsubscribe = new();
    private readonly DispatcherTimer _fsToastTimer = new() { Interval = TimeSpan.FromMilliseconds(900) };
    private TextBlock? _fsToastText;
    private ContentItem? _fsNextItem;
    private string? _fsNextCurrent;
    private string _fsEpisodesSignature = "";

    private Brush Fs(string key) => (Brush)FindResource(key);
    private TextBlock FsText(string text, double size, FontWeight? weight = null, string color = "--ink") => new() { Text = text, FontSize = size, FontWeight = weight ?? FontWeights.Medium, Foreground = Fs(color), TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
    private Button FsIcon(string icon, string label, Action action, bool prominent = false)
    {
        var button = new Button { Style = (Style)FindResource(prominent ? "FullscreenPlayButton" : "FullscreenIconButton"), Content = new SvgIcon(icon) { Width = prominent ? 27.2 : 22.4, Height = prominent ? 27.2 : 22.4, StrokeThickness = prominent ? 0 : 1.8 }, ToolTip = new ToolTip { Content = label, Style = (Style)FindResource("FullscreenToolTip"), Placement = PlacementMode.Top, VerticalOffset = -9.6 } };
        if (icon is "rewind-10" or "forward-10") { var content = new Grid(); var glyph = (SvgIcon)button.Content; button.Content = null; content.Children.Add(glyph); content.Children.Add(new TextBlock { Text = "10", FontSize = 8, FontWeight = FontWeights.ExtraBold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }); button.Content = content; }
        ((ToolTip)button.ToolTip).Opened += (_, _) => { var tip = (ToolTip)button.ToolTip; double scale = (_fullscreenRoot?.LayoutTransform as ScaleTransform)?.ScaleX ?? 1; tip.LayoutTransform = new ScaleTransform(scale, scale); }; ToolTipService.SetInitialShowDelay(button, 0); System.Windows.Automation.AutomationProperties.SetName(button, label); button.Click += (_, _) => action(); return button;
    }
    private static LinearGradientBrush Fade(Color first, Color last) => new(first, last, 90);
    private static readonly Color FsTransparent = Color.FromArgb(0, 0, 0, 0);
    [StructLayout(LayoutKind.Sequential)] private struct FsPointer { public int X, Y; }
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out FsPointer point);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int virtualKey);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    private bool FullscreenPopupHasFocus => _fsPopup?.IsOpen == true && _fsPopup.Child is Visual visual && PresentationSource.FromVisual(visual) is System.Windows.Interop.HwndSource source && source.Handle == GetForegroundWindow();
    private bool _fsRecordKeyHeld;
    private FsPointer _fsLastPointer;
    internal bool SmokeSuspendPointerReveal { get; set; }
    private void RevealFullscreenControlsFromPointer()
    {
        if (SmokeSuspendPointerReveal || !_fullscreen || !GetCursorPos(out var point)) return;
        if (point.X == _fsLastPointer.X && point.Y == _fsLastPointer.Y) return;
        _fsLastPointer = point; RevealFullscreenControls();
    }
    internal void SmokeFullscreenPointerMotion() { if (GetCursorPos(out var point)) { _fsLastPointer = new FsPointer { X = point.X - 1, Y = point.Y }; RevealFullscreenControlsFromPointer(); } }

    private UIElement CreateFullscreenLayoutV2()
    {
        _fsDrawerOpen = true; _fsEpisodeTab = _current?.Kind == ContentKind.Episode; _fsTabs.Clear(); _fsCompactHidden.Clear(); _fsEpisodesSignature = ""; GetCursorPos(out _fsLastPointer);
        var root = _fullscreenRoot = new Grid { ClipToBounds = true, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        root.SetValue(TextElement.FontFamilyProperty, FindResource("FullscreenFont")); root.SetValue(TextElement.FontSizeProperty, 16d); root.SetValue(TextElement.FontWeightProperty, FontWeights.Medium);
        var top = new Border { VerticalAlignment = VerticalAlignment.Top, Padding = new Thickness(32, 22.4, 32, 56), Background = Fade(Color.FromArgb(191, 0, 0, 0), FsTransparent) };
        var header = new Grid(); header.ColumnDefinitions.Add(new() { Width = new GridLength(48) }); header.ColumnDefinitions.Add(new() { Width = new GridLength(16) }); header.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) }); header.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        var back = FsIcon("fs-v2-back", "Geri (Esc)", ToggleFullscreen); back.Style = (Style)FindResource("FullscreenBackButton"); header.Children.Add(back);
        var titles = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; _fsTitle = FsText("", 24, FontWeights.ExtraBold); _fsSubtitle = FsText("", 14.4, color: "--mut"); _fsSubtitle.Margin = new Thickness(0, 2.4, 0, 0); titles.Children.Add(_fsTitle); titles.Children.Add(_fsSubtitle); Grid.SetColumn(titles, 2); header.Children.Add(titles);
        _fsBrand = FsText("", 12.48, FontWeights.Bold);
        var brand = new Border { Background = new SolidColorBrush(Color.FromArgb(153, 12, 15, 17)), BorderBrush = Fs("--line"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(999), Padding = new Thickness(12.8, 7.2, 12.8, 7.2), Child = _fsBrand, VerticalAlignment = VerticalAlignment.Center, MaxWidth = 280, Margin = new Thickness(16, 0, 0, 0) }; Grid.SetColumn(brand, 3); header.Children.Add(brand); top.Child = header; root.Children.Add(top);

        var bottom = new Border { VerticalAlignment = VerticalAlignment.Bottom, Padding = new Thickness(32, 96, 32, 22.4), Background = new LinearGradientBrush(new GradientStopCollection { new(FsTransparent, 0), new(Color.FromArgb(224, 0, 0, 0), .65) }, 90) };
        var body = new StackPanel(); bottom.Child = body; var times = new DockPanel { Margin = new Thickness(0, 0, 0, 1.6) };
        var rightTime = new StackPanel { Orientation = Orientation.Horizontal }; DockPanel.SetDock(rightTime, Dock.Right); times.Children.Add(rightTime); _fsRemaining = FsText("", 15.2, color: "--mut"); rightTime.Children.Add(_fsRemaining);
        _fsLiveButton = new Button { Style = (Style)FindResource("FullscreenMenuButton"), Content = "● CANLIYA DÖN", Foreground = Fs("--live"), Visibility = Visibility.Collapsed }; _fsLiveButton.Click += GoLive_Click; System.Windows.Automation.AutomationProperties.SetName(_fsLiveButton, "Canlı yayına dön"); rightTime.Children.Add(_fsLiveButton);
        _fsTime = FsText("00:00 / 00:00", 16.8, FontWeights.SemiBold, "--mut"); times.Children.Add(_fsTime); body.Children.Add(times);
        _fsSeek = new MediaSlider { Minimum = 0, Maximum = 1, Style = (Style)FindResource("FullscreenSeekStyle"), ToolTip = "Yayın konumu" }; System.Windows.Automation.AutomationProperties.SetName(_fsSeek, "Yayın konumu");
        _fsSeek.InteractionCommitted += async (_, _) => { if (!_ready || !_fullscreen || _fsSeek is null) return; double position = _fsSeek.Value; if (_engine.HasLiveBuffer) await SafeAsync(() => _engine.RewindLiveAsync((1 - position) * _engine.BufferedSeconds, _life.Token)); else if (_engine.Player.IsSeekable) { await FinishClipBeforeSeekAsync(); _engine.Player.Position = (float)position; _discordForceTiming = true; } }; body.Children.Add(_fsSeek);
        var controls = new Grid { Margin = new Thickness(0, 4.8, 0, 0) }; controls.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); controls.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) }); controls.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        void AddLeft(Button b) { b.Margin = new Thickness(0, 0, 2.4, 0); left.Children.Add(b); }
        AddLeft(FsIcon("fs-v2-prev", "Önceki bölüm (P)", () => ChangeChannel(-1))); AddLeft(FsIcon("rewind-10", "10 sn geri (←)", () => { Seek(-10000); ShowFullscreenToast("−10 sn"); }));
        var play = FsIcon("fs-v2-pause", "Oynat / Duraklat", () => { PlayPause_Click(this, new RoutedEventArgs()); UpdateFullscreenChrome(); }, true); play.Margin = new Thickness(8, 0, 10.4, 0); _fsPlayIcon = (SvgIcon)play.Content; left.Children.Add(play);
        AddLeft(FsIcon("forward-10", "10 sn ileri (→)", () => { Seek(10000); ShowFullscreenToast("+10 sn"); })); AddLeft(FsIcon("fs-v2-next", "Sonraki bölüm (N)", () => ChangeChannel(1)));
        var volume = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6.4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center }; var mute = FsIcon("fs-v2-vol", "Sesi kapat (M)", () => { Mute_Click(this, new RoutedEventArgs()); UpdateFullscreenChrome(); }); _fsMuteIcon = (SvgIcon)mute.Content; volume.Children.Add(mute);
        _fsVolume = new MediaSlider { Width = 104, Minimum = 0, Maximum = 100, Style = (Style)FindResource("FullscreenVolumeStyle"), Margin = new Thickness(6.4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center }; _fsVolume.SetBinding(RangeBase.ValueProperty, new Binding("Value") { Source = VolumeSlider, Mode = BindingMode.TwoWay }); System.Windows.Automation.AutomationProperties.SetName(_fsVolume, "Ses düzeyi"); volume.Children.Add(_fsVolume); _fsCompactHidden.Add(_fsVolume);
        _fsVolumeLabel = FsText("", 12.8, color: "--mut"); _fsVolumeLabel.Width = 25.6; _fsVolumeLabel.Opacity = 0; _fsVolumeLabel.Margin = new Thickness(6.4, 0, 0, 0); volume.Children.Add(_fsVolumeLabel);
        volume.MouseEnter += (_, _) => _fsVolumeLabel?.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200))); volume.MouseLeave += (_, _) => _fsVolumeLabel?.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200))); left.Children.Add(volume); controls.Children.Add(left);
        var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center }; _fsRecordingTime = FsText("", 12.8, FontWeights.ExtraBold, "--live"); _fsRecordingTime.Margin = new Thickness(0, 0, 6.4, 0); _fsRecordingTime.Visibility = Visibility.Collapsed; right.Children.Add(_fsRecordingTime);
        void AddRight(Button b) { b.Margin = new Thickness(0, 0, 2.4, 0); right.Children.Add(b); }
        _fsSpeedButton = new Button { Style = (Style)FindResource("FullscreenIconButton"), Width = double.NaN, Padding = new Thickness(12.8, 0, 12.8, 0), Content = "1×", FontSize = 15.2, FontWeight = FontWeights.ExtraBold }; System.Windows.Automation.AutomationProperties.SetName(_fsSpeedButton, "Oynatma hızı"); _fsSpeedButton.Click += (_, _) => ShowFullscreenSpeed(); AddRight(_fsSpeedButton);
        _fsTracksButton = FsIcon("fs-v2-sub", "Ses ve altyazı (C)", ShowFullscreenTracks); AddRight(_fsTracksButton); var info = FsIcon("fs-v2-info", "Akış bilgisi (I)", () => Statistics_Click(this, new RoutedEventArgs())); AddRight(info); _fsCompactHidden.Add(info);
        _fsRecordButton = FsIcon("fs-v2-rec", "Kaydet (R)", () => Record_Click(this, new RoutedEventArgs())); _fsRecordButton.SetBinding(IsEnabledProperty, new Binding("IsEnabled") { Source = RecordButton }); AddRight(_fsRecordButton); var pip = FsIcon("fs-v2-pip", "Pencere içinde pencere", ToggleMiniPlayer); AddRight(pip); _fsCompactHidden.Add(pip);
        right.Children.Add(new Border { Width = 1, Height = 25.6, Background = Fs("--line"), Margin = new Thickness(9.6, 0, 9.6, 0) }); _fsLibraryButton = FsIcon("fs-v2-list", "Kütüphane (L)", ToggleFullscreenDrawer); AddRight(_fsLibraryButton); AddRight(FsIcon("fs-v2-exit", "Tam ekrandan çık (F)", ToggleFullscreen)); Grid.SetColumn(right, 2); controls.Children.Add(right); body.Children.Add(controls); root.Children.Add(bottom);
        _fsNextChip = new Button { Style = (Style)FindResource("FullscreenChipButton"), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(32, 0, 0, 153.6), Visibility = Visibility.Collapsed };
        var chipContent = new StackPanel { Orientation = Orientation.Horizontal }; chipContent.Children.Add(new TextBlock { Text = "Sıradaki bölüm", FontSize = 14.4, FontWeight = FontWeights.ExtraBold }); chipContent.Children.Add(new Border { BorderBrush = new SolidColorBrush(Color.FromArgb(68, 16, 22, 10)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5.6), Padding = new Thickness(6.4, .8, 6.4, .8), Margin = new Thickness(9.6, 0, 0, 0), Child = new TextBlock { Text = "N", FontSize = 11.2, FontWeight = FontWeights.Bold } }); _fsNextChip.Content = chipContent; System.Windows.Automation.AutomationProperties.SetName(_fsNextChip, "Sıradaki bölüm"); _fsNextChip.Click += (_, _) => PlayFullscreenNextEpisode(); root.Children.Add(_fsNextChip);
        _fsToastText = FsText("", 16, FontWeights.Bold); _fsToast = new Border { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Padding = new Thickness(20.8, 11.2, 20.8, 11.2), CornerRadius = new CornerRadius(999), Background = new SolidColorBrush(Color.FromArgb(179, 12, 15, 17)), Child = _fsToastText, Opacity = 0, IsHitTestVisible = false }; Panel.SetZIndex(_fsToast, 5); root.Children.Add(_fsToast); _fsToastTimer.Tick += FullscreenToastTick;
        CreateFullscreenDrawer(root); UpdateFullscreenTabs(); UpdateFullscreenChrome(); return root;
    }

    private void CreateFullscreenDrawer(Grid root)
    {
        _fsDrawer = new Border { Width = 416, Margin = new Thickness(0, 83.2, 24, 0), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, CornerRadius = new CornerRadius(22.4), Background = Fs("--glass"), BorderBrush = Fs("--line"), BorderThickness = new Thickness(1), ClipToBounds = true, RenderTransform = new TranslateTransform() }; _fsDrawer.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, BlurRadius = 60, ShadowDepth = 20, Opacity = .5333 }; Panel.SetZIndex(_fsDrawer, 1);
        System.Windows.Automation.AutomationProperties.SetName(_fsDrawer, "Kütüphane"); var grid = new Grid(); grid.RowDefinitions.Add(new() { Height = GridLength.Auto }); grid.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) }); grid.RowDefinitions.Add(new() { Height = GridLength.Auto }); _fsDrawer.Child = grid;
        var head = new StackPanel { Margin = new Thickness(17.6, 17.6, 17.6, 8) }; grid.Children.Add(head); var heading = new DockPanel(); var close = FsIcon("fs-v2-x", "Kapat", ToggleFullscreenDrawer); close.Width = close.Height = 36.8; DockPanel.SetDock(close, Dock.Right); heading.Children.Add(close); heading.Children.Add(FsText("Kütüphanem", 17.6, FontWeights.ExtraBold)); head.Children.Add(heading);
        var tabs = new UniformGrid { Rows = 1 }; foreach (var (section, label) in new[] { ("episodes", "Bölümler"), ("favorites", "Favoriler"), ("live", "Canlı"), ("series", "Dizi"), ("movie", "Film") }) { string selected = section; var tab = new Button { Content = label, Style = (Style)FindResource("FullscreenTabButton"), Margin = new Thickness(0, 0, 4.8, 0) }; System.Windows.Automation.AutomationProperties.SetName(tab, label + " filtresi"); tab.Click += async (_, _) => await SafeAsync(() => SelectFullscreenTabAsync(selected)); _fsTabs[section] = tab; tabs.Children.Add(tab); }
        _fsTabs["movie"].Margin = new Thickness(0); head.Children.Add(new Border { Background = new SolidColorBrush(Color.FromArgb(18, 255, 255, 255)), CornerRadius = new CornerRadius(14.4), Padding = new Thickness(4), Margin = new Thickness(0, 12.8, 0, 12.8), Child = tabs });
        var searchWrap = new Grid(); _fsSearch = new TextBox { Style = (Style)FindResource("FullscreenSearch") }; System.Windows.Automation.AutomationProperties.SetName(_fsSearch, "Ara"); searchWrap.Children.Add(_fsSearch); searchWrap.Children.Add(new SvgIcon("search") { Width = 17.6, Height = 17.6, Foreground = Fs("--mut"), StrokeThickness = 1.8, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(12.8, 0, 0, 0) });
        _fsSearchHint = FsText("Bu listede ara…", 14.4, color: "--mut"); _fsSearchHint.Margin = new Thickness(40, 0, 0, 0); _fsSearchHint.IsHitTestVisible = false; searchWrap.Children.Add(_fsSearchHint); _fsSearch.TextChanged += (_, _) => { if (_fsSearchHint is not null) _fsSearchHint.Visibility = _fsSearch.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed; if (_fsEpisodeTab) RefreshFullscreenEpisodes(); else SearchBox.Text = _fsSearch.Text; }; head.Children.Add(searchWrap); if (!_fsEpisodeTab) _fsSearch.Text = SearchBox.Text;
        // Keep category/page filtering available without adding controls absent from v2.
        _fullscreenCategory = new ComboBox(); _fullscreenCategory.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("ItemsSource") { Source = CategoryPicker }); _fullscreenCategory.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedItem") { Source = CategoryPicker, Mode = BindingMode.TwoWay });
        var menu = new ContextMenu(); searchWrap.ContextMenu = menu; menu.Opened += (_, _) => { menu.Items.Clear(); foreach (var category in CategoryPicker.Items.Cast<object>()) { var entry = new MenuItem { Header = category.ToString(), IsCheckable = true, IsChecked = Equals(category, CategoryPicker.SelectedItem) }; entry.Click += (_, _) => CategoryPicker.SelectedItem = category; menu.Items.Add(entry); } menu.Items.Add(new Separator()); var prev = new MenuItem { Header = "Önceki sayfa", IsEnabled = PrevPage.IsEnabled }; prev.Click += PrevPage_Click; menu.Items.Add(prev); var next = new MenuItem { Header = "Sonraki sayfa", IsEnabled = NextPage.IsEnabled }; next.Click += NextPage_Click; menu.Items.Add(next); }; searchWrap.ToolTip = "Kategori ve sayfa seçenekleri için sağ tıklayın";
        FullscreenChannels = new ListBox { ItemTemplate = (DataTemplate)FindResource("FullscreenDrawerItemTemplate"), ItemContainerStyle = (Style)FindResource("FullscreenRowStyle"), BorderThickness = new Thickness(0), Padding = new Thickness(9.6, 8, 9.6, 9.6), Background = Brushes.Transparent, Foreground = Fs("--ink"), FontFamily = (FontFamily)FindResource("FullscreenFont") };
        FullscreenChannels.MouseDoubleClick += (sender, args) => { if (!_fsEpisodeTab) Channel_DoubleClick(sender, args); }; FullscreenChannels.SelectionChanged += async (_, _) => { if (_fsEpisodeTab && !_fsUpdatingItems && FullscreenChannels?.SelectedItem is ContentItem episode && episode.Id != _current?.Id) await SafeAsync(() => PlayItemAsync(episode)); };
        ScrollViewer.SetVerticalScrollBarVisibility(FullscreenChannels, ScrollBarVisibility.Auto); ScrollViewer.SetHorizontalScrollBarVisibility(FullscreenChannels, ScrollBarVisibility.Disabled); VirtualizingPanel.SetVirtualizationMode(FullscreenChannels, VirtualizationMode.Recycling); Grid.SetRow(FullscreenChannels, 1); grid.Children.Add(FullscreenChannels);
        var empty = FsText("Sonuç bulunamadı.", 14.4, color: "--mut"); empty.HorizontalAlignment = HorizontalAlignment.Center; empty.Margin = new Thickness(16, 32, 16, 32); Grid.SetRow(empty, 1); grid.Children.Add(empty);
        var items = FullscreenChannels.Items; System.Collections.Specialized.NotifyCollectionChangedEventHandler changed = (_, _) => { empty.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed; UpdateFullscreenCount(); }; ((System.Collections.Specialized.INotifyCollectionChanged)items).CollectionChanged += changed; _fsUnsubscribe.Add(() => ((System.Collections.Specialized.INotifyCollectionChanged)items).CollectionChanged -= changed);
        var footer = new DockPanel { Margin = new Thickness(17.6, 11.2, 17.6, 11.2) }; var hint = FsText("L ile aç / kapat", 12.8, color: "--mut"); DockPanel.SetDock(hint, Dock.Right); footer.Children.Add(hint); _fsPageLabel = FsText("", 12.8, color: "--mut"); footer.Children.Add(_fsPageLabel); var divider = new Border { BorderBrush = Fs("--line"), BorderThickness = new Thickness(0, 1, 0, 0), Child = footer }; Grid.SetRow(divider, 2); grid.Children.Add(divider);
        FullscreenBrowser = _fsDrawer; root.Children.Add(_fsDrawer); BindFullscreenLibrary(); empty.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void UpdateFullscreenViewport()
    {
        if (_fullscreenRoot is null || _floatingControls is null || _floatingControls.ActualWidth <= 0) return;
        double width = _floatingControls.ActualWidth, height = _floatingControls.ActualHeight, scale = Math.Clamp(.0062 * width + 8, 14, 22) / 16;
        _fullscreenRoot.Width = width / scale; _fullscreenRoot.Height = height / scale; if (_fullscreenRoot.LayoutTransform is not ScaleTransform transform || Math.Abs(transform.ScaleX - scale) > .0001) _fullscreenRoot.LayoutTransform = new ScaleTransform(scale, scale);
        if (_fsDrawer is not null) { _fsDrawer.MaxHeight = Math.Max(1, height / scale - 267.2); _fsDrawer.Width = width <= 720 ? Math.Max(1, width / scale - 32) : 416; _fsDrawer.Margin = new Thickness(width <= 720 ? 16 : 0, 83.2, width <= 720 ? 16 : 24, 0); }
        if (_fsToast is not null) _fsToast.Margin = new Thickness(0, height / scale * .38, 0, 0); foreach (var element in _fsCompactHidden) element.Visibility = width <= 720 ? Visibility.Collapsed : Visibility.Visible;
    }
    private void UpdateFullscreenCount() { if (_fsPageLabel is not null && FullscreenChannels is not null) _fsPageLabel.Text = $"{(_fsEpisodeTab ? FullscreenChannels.Items.Count : _total):N0} içerik"; }
    private void BindFullscreenLibrary()
    {
        if (FullscreenChannels is null) return; _fsUpdatingItems = true;
        try { BindingOperations.ClearBinding(FullscreenChannels, ItemsControl.ItemsSourceProperty); BindingOperations.ClearBinding(FullscreenChannels, Selector.SelectedItemProperty); if (_fsEpisodeTab) { FullscreenChannels.ItemTemplate = (DataTemplate)FindResource("FullscreenEpisodeTemplate"); _fsEpisodesSignature = ""; RefreshFullscreenEpisodes(); } else { FullscreenChannels.ItemTemplate = (DataTemplate)FindResource("FullscreenDrawerItemTemplate"); FullscreenChannels.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("ItemsSource") { Source = ChannelList }); FullscreenChannels.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedItem") { Source = ChannelList, Mode = BindingMode.TwoWay }); } }
        finally { _fsUpdatingItems = false; } UpdateFullscreenCount();
    }
    private void RefreshFullscreenEpisodes()
    {
        if (!_fsEpisodeTab || FullscreenChannels is null) return; var current = _current; IReadOnlyList<ContentItem> episodes = current?.Kind == ContentKind.Episode ? EpisodeNavigation.Ordered(current, _episodeCache) : [];
        string q = ContentItem.SearchKey(_fsSearch?.Text ?? ""), signature = current?.Id + "|" + q + "|" + string.Join(',', episodes.Select(e => e.Id)); if (signature == _fsEpisodesSignature) return; _fsEpisodesSignature = signature;
        bool updating = _fsUpdatingItems; _fsUpdatingItems = true; try { FullscreenChannels.ItemsSource = episodes.Where(e => q.Length == 0 || ContentItem.SearchKey(e.Name + " " + e.EpisodeLabel).Contains(q)).ToList(); FullscreenChannels.SelectedItem = FullscreenChannels.Items.Cast<ContentItem>().FirstOrDefault(e => e.Id == current?.Id); } finally { _fsUpdatingItems = updating; } UpdateFullscreenCount();
    }
    private async Task SelectFullscreenTabAsync(string section)
    {
        if (!_fullscreen) return; CloseFullscreenPopup(); _fsEpisodeTab = section == "episodes"; if (!_fsEpisodeTab) { _section = section; _offset = 0; _suppress = true; CategoryPicker.SelectedIndex = 0; _suppress = false; SearchBox.Text = _fsSearch?.Text ?? ""; SetNav(); await RefreshViewAsync(); } BindFullscreenLibrary(); UpdateFullscreenTabs();
    }
    private void UpdateFullscreenTabs()
    {
        foreach (var (section, tab) in _fsTabs) { bool active = _fsEpisodeTab ? section == "episodes" : section == _section; tab.Background = active ? Fs("--acc") : Brushes.Transparent; tab.Foreground = active ? new SolidColorBrush(Color.FromRgb(16, 22, 10)) : Fs("--mut"); }
        if (_fsLibraryButton is not null) { _fsLibraryButton.Background = _fsDrawerOpen ? Fs("FsActive") : Brushes.Transparent; _fsLibraryButton.Foreground = _fsDrawerOpen ? Fs("--acc") : Fs("--ink"); }
    }
    private void ToggleFullscreenDrawer()
    {
        if (_fsDrawer is null) return; var drawer = _fsDrawer; _fsDrawerOpen = !_fsDrawerOpen; int version = ++_fsDrawerVersion; if (_fsDrawerOpen) drawer.Visibility = Visibility.Visible;
        var transform = (TranslateTransform)drawer.RenderTransform; var slide = new DoubleAnimation(_fsDrawerOpen ? 0 : drawer.ActualWidth + 48, TimeSpan.FromMilliseconds(350)) { EasingFunction = new CssBezierEase() }; slide.Completed += (_, _) => { if (version == _fsDrawerVersion && !_fsDrawerOpen && ReferenceEquals(drawer, _fsDrawer)) drawer.Visibility = Visibility.Collapsed; }; transform.BeginAnimation(TranslateTransform.XProperty, slide); UpdateFullscreenTabs(); if (_fsDrawerOpen) { _hideControls.Stop(); RevealFullscreenControls(); } else RestartControlsTimer();
    }
    private void HideFullscreenControls175()
    {
        if (_fsDrawerOpen || _fsPopup?.IsOpen == true || _engine is null || !_engine.Player.IsPlaying || Mouse.Captured is not null || _fullscreenCategory?.IsDropDownOpen == true || Keyboard.FocusedElement is ComboBox { IsDropDownOpen: true }) return;
        _hideControls.Stop(); var window = _floatingControls; if (window is null) return; int version = ++_fullscreenFadeVersion; var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300)); fade.Completed += (_, _) => { if (version != _fullscreenFadeVersion || window != _floatingControls) return; window.Hide(); window.BeginAnimation(Window.OpacityProperty, null); window.Opacity = 1; }; window.BeginAnimation(Window.OpacityProperty, fade);
    }
    private void UpdateFullscreenChrome()
    {
        if (_fullscreenRoot is null || _engine is null || _closing) return; UpdateFullscreenViewport(); var item = _current; var player = _engine.Player;
        if ((GetAsyncKeyState(0x52) & 0x8000) == 0) _fsRecordKeyHeld = false;
        if (_fsTitle is not null) _fsTitle.Text = item?.Kind == ContentKind.Episode && item.SeriesName.Length > 0 ? item.SeriesName : item?.Name ?? "WX Player";
        if (_fsSubtitle is not null) _fsSubtitle.Text = item?.Kind == ContentKind.Episode ? $"S{item.Season:00} · B{item.Episode:00} — {item.Name}" : item?.Category ?? "";
        if (_fsBrand is not null) _fsBrand.Text = _sources.FirstOrDefault(s => s.Id == item?.SourceId)?.Name ?? SelectedSource?.Name ?? "Kütüphanem";
        if (_fsPlayIcon is not null) _fsPlayIcon.Icon = player.IsPlaying ? "fs-v2-pause" : "fs-v2-play"; if (_fsMuteIcon is not null) _fsMuteIcon.Icon = player.Mute ? "fs-v2-mut" : "fs-v2-vol";
        if (_fsRecordButton is not null) { _fsRecordButton.Foreground = _engine.Recording ? Fs("--live") : Fs("--ink"); _fsRecordButton.Background = _engine.Recording ? new SolidColorBrush(Color.FromArgb(41, 255, 92, 108)) : Brushes.Transparent; }
        if (_engine.Recording) _fsRecordingStarted ??= DateTime.UtcNow; else _fsRecordingStarted = null; if (_fsRecordingTime is not null) { _fsRecordingTime.Visibility = _engine.Recording ? Visibility.Visible : Visibility.Collapsed; _fsRecordingTime.Text = "● REC " + FsClock((long)(DateTime.UtcNow - (_fsRecordingStarted ?? DateTime.UtcNow)).TotalMilliseconds); }
        if (_fsSpeedButton is not null) _fsSpeedButton.Content = player.Rate.ToString("0.##", CultureInfo.InvariantCulture) + "×";
        if (_fsSeek is not null) { _fsSeek.IsEnabled = _engine.HasLiveBuffer ? _engine.BufferedSeconds >= 2 : player.IsSeekable; if (!_fsSeek.IsInteracting && player.Position >= 0) _fsSeek.Value = _engine.HasLiveBuffer ? Math.Clamp(1 - _engine.BehindLive / Math.Max(1, _engine.BufferedSeconds), 0, 1) : player.Position; } if (_fsVolumeLabel is not null) _fsVolumeLabel.Text = ((int)VolumeSlider.Value).ToString();
        if (item?.Kind == ContentKind.Live) { if (_fsTime is not null) _fsTime.Text = _engine.IsReplay ? $"−{_engine.BehindLive:0} sn" : "CANLI"; if (_fsRemaining is not null) _fsRemaining.Text = "● CANLI"; }
        else { long length = Math.Max(0, player.Length), position = Math.Max(0, player.Time); if (_fsTime is not null) _fsTime.Text = FsClock(position) + " / " + FsClock(length); if (_fsRemaining is not null) { double seconds = Math.Max(0, length - position) / 1000d / Math.Max(.1, player.Rate); _fsRemaining.Text = length > 0 ? $"−{FsClock(length - position)} · {DateTime.Now.AddSeconds(seconds):HH:mm}'de biter" : ""; } }
        if (_fsLiveButton is not null) _fsLiveButton.Visibility = _engine.HasLiveBuffer && _engine.IsReplay ? Visibility.Visible : Visibility.Collapsed; RefreshFullscreenEpisodes(); UpdateFullscreenTabs(); UpdateFullscreenCount();
    }
    private static string FsClock(long ms) { var time = TimeSpan.FromMilliseconds(Math.Max(0, ms)); return time.TotalHours >= 1 ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}" : $"{(int)time.TotalMinutes:00}:{time.Seconds:00}"; }
    private bool HandleFullscreenShortcut(Key key)
    {
        switch(key)
        {
            case Key.Space: PlayPause_Click(this,new RoutedEventArgs()); return true;
            case Key.Left: Seek(-10000); ShowFullscreenToast("−10 sn"); return true;
            case Key.Right: Seek(10000); ShowFullscreenToast("+10 sn"); return true;
            case Key.L: ToggleFullscreenDrawer(); return true;
            case Key.N: ChangeChannel(1); return true;
            case Key.P: ChangeChannel(-1); return true;
            case Key.R:
                bool held = (GetAsyncKeyState(0x52) & 0x8000) != 0;
                if (held && _fsRecordKeyHeld) return true;
                _fsRecordKeyHeld = held;
                if(RecordButton.IsEnabled) Record_Click(this,new RoutedEventArgs()); return true;
            case Key.C: ShowFullscreenTracks(); return true;
            case Key.Escape when _fsPopup?.IsOpen==true: CloseFullscreenPopup(); return true;
            default: return false;
        }
    }
    private void ShowFullscreenToast(string text) { if (_fsToast is null || _fsToastText is null) return; _fsToastText.Text = text; _fsToast.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200))); _fsToastTimer.Stop(); _fsToastTimer.Start(); }
    private void FullscreenToastTick(object? sender, EventArgs e) { _fsToastTimer.Stop(); _fsToast?.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200))); }
    private void CloseFullscreenPopup() { if (_fsPopup is not null) { _fsPopup.IsOpen = false; _fsPopup.Child = null; _fsPopup = null; } }
    private void ShowFullscreenPopup(Button anchor, StackPanel content)
    {
        if (_fsPopup?.PlacementTarget == anchor && _fsPopup.IsOpen) { CloseFullscreenPopup(); return; } CloseFullscreenPopup(); double scale = (_fullscreenRoot?.LayoutTransform as ScaleTransform)?.ScaleX ?? 1;
        var surface = new Border { MinWidth = 208, Padding = new Thickness(8), CornerRadius = new CornerRadius(16), Background = Fs("--glass"), BorderBrush = Fs("--line"), BorderThickness = new Thickness(1), Child = content, LayoutTransform = new ScaleTransform(scale, scale) }; surface.SetValue(TextElement.FontFamilyProperty, FindResource("FullscreenFont")); surface.SetValue(TextElement.ForegroundProperty, Fs("--ink")); surface.PreviewKeyDown += Window_KeyDown;
        _fsPopup = new Popup { AllowsTransparency = true, StaysOpen = false, PlacementTarget = anchor, Placement = PlacementMode.Top, VerticalOffset = -12.8 * scale, Child = surface };
        _fsPopup.Opened += (_, _) => content.Children.OfType<Button>().FirstOrDefault()?.Focus();
        _fsPopup.Closed += (_, _) => { if (_fullscreen && !_closing && anchor.IsVisible) anchor.Focus(); RestartControlsTimer(); };
        _fsPopup.IsOpen = true; _hideControls.Stop();
    }
    private void FsMenuHeading(StackPanel panel, string title) { var text = FsText(title, 11.52, FontWeights.Bold, "--mut"); text.Margin = new Thickness(11.2, 8, 11.2, 4.8); panel.Children.Add(text); }
    private void FsMenuChoice(StackPanel panel, string title, bool selected, Action action) { var row = new DockPanel(); var mark = FsText(selected ? "✓" : "", 14.4, color: selected ? "--acc" : "--ink"); DockPanel.SetDock(mark, Dock.Right); row.Children.Add(mark); row.Children.Add(FsText(title, 14.4, color: selected ? "--acc" : "--ink")); var b = new Button { Style = (Style)FindResource("FullscreenMenuButton"), Content = row }; System.Windows.Automation.AutomationProperties.SetName(b, title); b.Click += (_, _) => action(); panel.Children.Add(b); }
    private void ShowFullscreenSpeed()
    {
        if (_fsSpeedButton is null) return; var panel = new StackPanel(); FsMenuHeading(panel, "OYNATMA HIZI"); foreach (float speed in new[] { .5f, .75f, 1f, 1.25f, 1.5f, 2f }) { float chosen = speed; FsMenuChoice(panel, speed.ToString("0.##", CultureInfo.InvariantCulture) + "×", Math.Abs(_engine.Player.Rate - speed) < .01, () => { if (_engine.Player.SetRate(chosen) != 0) ShowFullscreenToast("Bu yayın hız değişimini desteklemiyor"); else { _discordForceTiming = true; UpdateFullscreenChrome(); } CloseFullscreenPopup(); }); } ShowFullscreenPopup(_fsSpeedButton, panel);
    }
    private void ShowFullscreenTracks()
    {
        if (_fsTracksButton is null) return; var panel = new StackPanel(); FsMenuHeading(panel, "SES"); var audio = _engine.Player.AudioTrackDescription; foreach (var track in audio) { int id = track.Id; FsMenuChoice(panel, string.IsNullOrWhiteSpace(track.Name) ? id < 0 ? "Ses kapalı" : "Ses " + id : track.Name, _engine.Player.AudioTrack == id, () => { _engine.Player.SetAudioTrack(id); CloseFullscreenPopup(); }); } if (audio.Length == 0) panel.Children.Add(FsText("Ses parçası bulunamadı", 14.4, color: "--mut"));
        FsMenuHeading(panel, "ALTYAZI"); FsMenuChoice(panel, "Kapalı", _engine.Player.Spu < 0, () => { _engine.Player.SetSpu(-1); CloseFullscreenPopup(); }); foreach (var track in _engine.Player.SpuDescription.Where(t => t.Id >= 0)) { int id = track.Id; FsMenuChoice(panel, string.IsNullOrWhiteSpace(track.Name) ? "Altyazı " + id : track.Name, _engine.Player.Spu == id, () => { _engine.Player.SetSpu(id); CloseFullscreenPopup(); }); }
        panel.ContextMenu = new ContextMenu(); var external = new MenuItem { Header = "Altyazı dosyası ekle…" }; external.Click += (_, _) => { CloseFullscreenPopup(); Tracks_Click(this, new RoutedEventArgs()); }; panel.ContextMenu.Items.Add(external); ShowFullscreenPopup(_fsTracksButton, panel);
    }
    private void PlayFullscreenNextEpisode() { var next = _fsNextItem; if (next is null || _current?.Id != _fsNextCurrent) return; CloseNextEpisode(); _ = SafeAsync(() => PlayItemAsync(next, true)); }
    private void DisposeFullscreenChrome()
    {
        ++_fsDrawerVersion; CloseFullscreenPopup(); _fsToastTimer.Stop(); _fsToastTimer.Tick -= FullscreenToastTick; foreach (var unsubscribe in _fsUnsubscribe) unsubscribe(); _fsUnsubscribe.Clear(); _fsTabs.Clear(); _fsCompactHidden.Clear(); if (_fsVolume is not null) BindingOperations.ClearAllBindings(_fsVolume); if (FullscreenChannels is not null) BindingOperations.ClearAllBindings(FullscreenChannels); _fsNextChip = null; _fsNextItem = null; _fsNextCurrent = null; _fsSearch = null; _fsSeek = _fsVolume = null; _fsToast = null; _fsToastText = null; _fullscreenCategory = null; _fsRecordingStarted = null;
    }
    internal bool SmokeFullscreenDrawerOpen => _fsDrawerOpen;
    internal void SmokeToggleFullscreenDrawer() => ToggleFullscreenDrawer();
    private sealed class CssBezierEase : EasingFunctionBase { private readonly KeySpline _spline = new(.2, .8, .2, 1); protected override double EaseInCore(double t) => _spline.GetSplineProgress(t); protected override Freezable CreateInstanceCore() => new CssBezierEase(); }
}
