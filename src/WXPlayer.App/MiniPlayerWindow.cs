using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;

namespace WXPlayer.App;

internal sealed class MiniPlayerWindow : Window
{
    internal NativeVideoHost Video { get; } = new();
    private readonly TextBlock _title = new() { TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 13, FontWeight = FontWeights.SemiBold };
    private readonly TextBlock _eyebrow = new() { Text = "ŞİMDİ OYNATILIYOR", FontSize = 10, FontWeight = FontWeights.SemiBold };
    private readonly TextBlock _time = new() { FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
    private readonly SvgIcon _playIcon = new("pause") { Width = 18, Height = 18 };
    private readonly SvgIcon _volumeIcon = new("volume") { Width = 17, Height = 17 };
    private readonly ProgressBar _progress = new() { Minimum = 0, Maximum = 1, Height = 4, BorderThickness = new Thickness(0) };

    internal MiniPlayerWindow(string title, Action restore, Action playPause, Action mute)
    {
        Title = "WX Player · Mini oynatıcı";
        Width = 520; Height = 350; MinWidth = 380; MinHeight = 250;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.CanResize;
        WindowChrome.SetWindowChrome(this, new WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new Thickness(6), GlassFrameThickness = new Thickness(0), CornerRadius = new CornerRadius(0), UseAeroCaptionButtons = false });
        ShowInTaskbar = false; Topmost = true;
        Background = PremiumWindow.Brush("BackgroundBaseBrush");
        Foreground = PremiumWindow.Brush("TextPrimaryBrush");
        FontFamily = (FontFamily)Application.Current.FindResource("AppFont");
        Left = Math.Max(SystemParameters.WorkArea.Left, SystemParameters.WorkArea.Right - Width - 22);
        Top = Math.Max(SystemParameters.WorkArea.Top, SystemParameters.WorkArea.Bottom - Height - 22);

        var frame = new Border { Background = PremiumWindow.Brush("BackgroundSecondaryBrush"), BorderBrush = PremiumWindow.Brush("BorderSubtleBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10) };
        Content = frame;
        var layout = new Grid(); frame.Child = layout;
        layout.RowDefinitions.Add(new() { Height = new GridLength(60) });
        layout.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new() { Height = new GridLength(64) });

        var header = new Grid { Margin = new Thickness(16, 0, 10, 0), Background = Brushes.Transparent };
        header.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        header.MouseLeftButtonDown += (_, e) => { if (e.LeftButton == MouseButtonState.Pressed && e.OriginalSource is not Button) DragMove(); };
        layout.Children.Add(header);
        var titleArea = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
        _eyebrow.Foreground = PremiumWindow.Brush("AccentPrimaryBrush"); titleArea.Children.Add(_eyebrow);
        _title.Margin = new Thickness(0, 4, 0, 0); titleArea.Children.Add(_title);
        header.Children.Add(titleArea);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(actions, 1); header.Children.Add(actions);
        var returnButton = IconButton("maximize", "Ana pencereye dön", restore);
        returnButton.Width = 132;
        returnButton.Background = PremiumWindow.Brush("SurfaceElevatedBrush");
        returnButton.Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new SvgIcon("maximize") { Width = 15, Height = 15, Margin = new Thickness(0, 0, 7, 0) }, new TextBlock { Text = "Ana pencere", FontSize = 11, VerticalAlignment = VerticalAlignment.Center } } };
        actions.Children.Add(returnButton);
        var close = IconButton("close", "Kapat ve ana pencereye dön", restore); close.Margin = new Thickness(6, 0, 0, 0); actions.Children.Add(close);

        var videoFrame = new Border { Background = Brushes.Black, Margin = new Thickness(1, 0, 1, 0), Child = Video };
        Grid.SetRow(videoFrame, 1); layout.Children.Add(videoFrame);
        Video.DoubleClicked += restore;

        var footer = new Grid { Margin = new Thickness(16, 0, 16, 0) };
        footer.RowDefinitions.Add(new() { Height = new GridLength(12) });
        footer.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(footer, 2); layout.Children.Add(footer);
        _progress.Foreground = PremiumWindow.Brush("PlayerProgressFillBrush");
        _progress.Background = PremiumWindow.Brush("PlayerProgressTrackBrush");
        _progress.VerticalAlignment = VerticalAlignment.Bottom;
        footer.Children.Add(_progress);
        var controls = new Grid { Margin = new Thickness(0, 5, 0, 0) };
        controls.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        controls.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        controls.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        Grid.SetRow(controls, 1); footer.Children.Add(controls);
        var play = IconButton("pause", "Oynat / duraklat · Space", playPause); play.Content = _playIcon;
        play.Background = PremiumWindow.Brush("AccentSubtleBrush"); play.Foreground = PremiumWindow.Brush("AccentPrimaryBrush");
        controls.Children.Add(play);
        _time.Foreground = PremiumWindow.Brush("TextSecondaryBrush"); _time.Margin = new Thickness(12, 0, 0, 0);
        Grid.SetColumn(_time, 1); controls.Children.Add(_time);
        var muteButton = IconButton("volume", "Sesi kapat / aç", mute); muteButton.Content = _volumeIcon;
        Grid.SetColumn(muteButton, 2); controls.Children.Add(muteButton);
        SetTitle(title);
    }

    private static Button IconButton(string icon, string label, Action action)
    {
        var button = new Button { Content = new SvgIcon(icon) { Width = 17, Height = 17 }, ToolTip = label, Width = 36, Height = 36, MinHeight = 36, Padding = new Thickness(7), Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
        System.Windows.Automation.AutomationProperties.SetName(button, label);
        button.Click += (_, _) => action();
        return button;
    }
    internal void SetTitle(string title) { _title.Text = title; _title.ToolTip = title; }
    internal void SetPlaying(bool playing) => _playIcon.Icon = playing ? "pause" : "play";
    internal void SetMuted(bool muted) => _volumeIcon.Icon = muted ? "volume-off" : "volume";
    internal void SetProgress(long time, long length, bool live)
    {
        _progress.Visibility = live || length <= 0 ? Visibility.Collapsed : Visibility.Visible;
        _progress.Value = length > 0 ? Math.Clamp(time / (double)length, 0, 1) : 0;
        _eyebrow.Text = live ? "CANLI YAYIN" : "ŞİMDİ OYNATILIYOR";
        _time.Text = live ? "●  CANLI" : length > 0 ? $"{TimeSpan.FromMilliseconds(Math.Max(0,time)).ToString(@"hh\:mm\:ss")}  /  {TimeSpan.FromMilliseconds(length).ToString(@"hh\:mm\:ss")}" : "Yayın hazırlanıyor";
        _time.Foreground = PremiumWindow.Brush(live ? "AccentPrimaryBrush" : "TextSecondaryBrush");
    }
}
