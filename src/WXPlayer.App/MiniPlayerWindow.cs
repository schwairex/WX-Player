using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Shapes;

namespace WXPlayer.App;

internal sealed class MiniPlayerWindow : Window
{
    internal NativeVideoHost Video { get; } = new();
    private readonly TextBlock _title = new() { TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 14, FontWeight = FontWeights.Bold };
    private readonly TextBlock _eyebrow = new() { Text = "ŞİMDİ OYNATILIYOR", FontSize = 10, FontWeight = FontWeights.Bold };
    private readonly TextBlock _time = new() { FontSize = 12, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _quality = new() { FontSize = 10, FontWeight = FontWeights.ExtraBold };
    private readonly Border _qualityBadge = new();
    private readonly Border _liveBadge = new();
    private readonly SvgIcon _playIcon = new("fs-v2-pause") { Width = 17, Height = 17, StrokeThickness = 0 };
    private readonly SvgIcon _volumeIcon = new("volume") { Width = 18, Height = 18 };
    private readonly ProgressBar _progress = new() { Minimum = 0, Maximum = 1, Height = 3, BorderThickness = new Thickness(0) };

    internal MiniPlayerWindow(string title, Action restore, Action playPause, Action mute)
    {
        Title = "WX Player · Mini oynatıcı";
        Width = 520; Height = 400; MinWidth = 380; MinHeight = 250;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.CanResize;
        WindowChrome.SetWindowChrome(this, new WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new Thickness(6), GlassFrameThickness = new Thickness(0), CornerRadius = new CornerRadius(0), UseAeroCaptionButtons = false });
        ShowInTaskbar = false; Topmost = true;
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("UtilityTheme.xaml", UriKind.Relative) });
        Background = Theme("LivePanel"); Foreground = Theme("LiveInk"); FontFamily = (FontFamily)FindResource("LiveFont"); FontWeight = FontWeights.Medium;
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Ideal); TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
        Left = Math.Max(SystemParameters.WorkArea.Left, SystemParameters.WorkArea.Right - Width - 22);
        Top = Math.Max(SystemParameters.WorkArea.Top, SystemParameters.WorkArea.Bottom - Height - 22);

        var frame = new Border { Background = Background, BorderBrush = Theme("LiveLine"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10) };
        Content = frame;
        var layout = new Grid(); frame.Child = layout;
        layout.RowDefinitions.Add(new() { Height = new GridLength(52) });
        layout.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new() { Height = new GridLength(56) });

        var header = new Grid { Margin = new Thickness(16, 0, 10, 0), Background = Brushes.Transparent };
        header.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        header.MouseLeftButtonDown += (_, e) => { if (e.LeftButton == MouseButtonState.Pressed && !InButton(e.OriginalSource as DependencyObject)) DragMove(); };
        layout.Children.Add(header);
        var titleArea = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
        var titleRow = new Grid(); titleRow.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); titleRow.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); titleRow.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        _title.VerticalAlignment = VerticalAlignment.Center; titleRow.Children.Add(_title);
        _quality.Foreground = Theme("UtilityNeutralInk");
        _qualityBadge.Background = Theme("SettingsButtonBg"); _qualityBadge.CornerRadius = new(5); _qualityBadge.Padding = new(6,2,6,2); _qualityBadge.Margin = new(8,0,0,0); _qualityBadge.Child = _quality; _qualityBadge.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(_qualityBadge,1); titleRow.Children.Add(_qualityBadge); titleArea.Children.Add(titleRow);
        void FitTitle()=>_title.MaxWidth=Math.Max(0,titleRow.ActualWidth-(_qualityBadge.Visibility==Visibility.Visible?_qualityBadge.ActualWidth+8:0));
        titleRow.SizeChanged+=(_,_)=>FitTitle();_qualityBadge.SizeChanged+=(_,_)=>FitTitle();
        _eyebrow.Foreground = Theme("LiveMuted"); _eyebrow.Margin = new(0,3,0,0); titleArea.Children.Add(_eyebrow); header.Children.Add(titleArea);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(actions, 1); header.Children.Add(actions);
        var returnButton = IconButton("maximize", "Ana pencereye dön", restore); KeyboardNavigation.SetTabIndex(returnButton,1); actions.Children.Add(returnButton);
        var close = IconButton("close", "Kapat ve ana pencereye dön", restore); close.Margin = new Thickness(0); KeyboardNavigation.SetTabIndex(close,0); actions.Children.Add(close);

        var videoFrame = new Border { Background = Brushes.Black, Child = Video };
        Grid.SetRow(videoFrame, 1); layout.Children.Add(videoFrame);
        Video.DoubleClicked += restore;

        var footer = new Grid();
        Grid.SetRow(footer, 2); layout.Children.Add(footer);
        _progress.Style = UtilityWindowAppearance.Style(this,"UtilityMiniProgress"); _progress.VerticalAlignment = VerticalAlignment.Top; footer.Children.Add(_progress);
        var controls = new Grid { Margin = new Thickness(14,3,10,0) };
        controls.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        controls.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        controls.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); footer.Children.Add(controls);
        var play = IconButton("pause", "Oynat / duraklat · Space", playPause); play.Content = _playIcon; play.Style = UtilityWindowAppearance.Style(this,"UtilityMiniPlay"); KeyboardNavigation.SetTabIndex(play,2); controls.Children.Add(play);
        _time.Foreground = Theme("LiveCaption"); _time.Margin = new Thickness(12,0,12,0); Typography.SetNumeralAlignment(_time,FontNumeralAlignment.Tabular);
        var status = new Grid { VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(status,1); controls.Children.Add(status); status.Children.Add(_time);
        var live = new StackPanel { Orientation = Orientation.Horizontal };
        live.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = Theme("LiveOnAir"), Margin = new(0,0,7,0), VerticalAlignment = VerticalAlignment.Center });
        live.Children.Add(new TextBlock { Text = "CANLI", FontSize = 11, FontWeight = FontWeights.ExtraBold, Foreground = Theme("UtilityLiveInk") });
        _liveBadge.Background = Theme("UtilityLiveBg"); _liveBadge.CornerRadius = new(999); _liveBadge.Padding = new(11,5,11,5); _liveBadge.Margin = new(12,0,0,0); _liveBadge.HorizontalAlignment = HorizontalAlignment.Left; _liveBadge.Child = live; _liveBadge.Visibility = Visibility.Collapsed;
        AutomationProperties.SetName(_liveBadge,"CANLI"); status.Children.Add(_liveBadge);
        var muteButton = IconButton("volume", "Sesi kapat / aç", mute); muteButton.Content = _volumeIcon; KeyboardNavigation.SetTabIndex(muteButton,3);
        Grid.SetColumn(muteButton, 2); controls.Children.Add(muteButton);
        SetTitle(title);
    }
    private Brush Theme(string key) => UtilityWindowAppearance.Brush(this,key);
    private static bool InButton(DependencyObject? node)
    {
        while(node is not null){if(node is System.Windows.Controls.Primitives.ButtonBase)return true;node=node is Visual?VisualTreeHelper.GetParent(node):null;}return false;
    }
    private Button IconButton(string icon, string label, Action action)
    {
        var button = new Button { Content = new SvgIcon(icon) { Width = 18, Height = 18 }, ToolTip = label, Style = UtilityWindowAppearance.Style(this,"UtilityMiniIcon") };
        AutomationProperties.SetName(button, label); button.Click += (_, _) => action(); return button;
    }
    internal void SetTitle(string title)
    {
        var display = UtilityDisplayFormatter.MiniTitle(title); _title.Text = display.Title; _title.ToolTip = title;
        _quality.Text = display.Quality; _qualityBadge.Visibility = display.Quality.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        AutomationProperties.SetName(_qualityBadge,display.Quality);
    }
    internal void SetPlaying(bool playing) => _playIcon.Icon = playing ? "fs-v2-pause" : "fs-v2-play";
    internal void SetMuted(bool muted) => _volumeIcon.Icon = muted ? "volume-off" : "volume";
    internal void SetProgress(long time, long length, bool live)
    {
        _progress.Visibility = live || length <= 0 ? Visibility.Collapsed : Visibility.Visible;
        _progress.Value = length > 0 ? Math.Clamp(time / (double)length, 0, 1) : 0;
        _eyebrow.Text = live ? "CANLI YAYIN" : "ŞİMDİ OYNATILIYOR";
        _time.Text = live ? "●  CANLI" : length > 0 ? $"{TimeSpan.FromMilliseconds(Math.Max(0,time)).ToString(@"hh\:mm\:ss")}  /  {TimeSpan.FromMilliseconds(length).ToString(@"hh\:mm\:ss")}" : "Yayın hazırlanıyor";
        _time.Foreground = Theme(length > 0 ? "LiveCaption" : "LiveMuted");
        _eyebrow.Visibility = live ? Visibility.Collapsed : Visibility.Visible;
        _time.Visibility = live ? Visibility.Collapsed : Visibility.Visible;
        _liveBadge.Visibility = live ? Visibility.Visible : Visibility.Collapsed;
    }
}
