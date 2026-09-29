using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WXPlayer.App;

internal sealed class MiniPlayerWindow : Window
{
    internal NativeVideoHost Video { get; } = new();
    private readonly TextBlock _title = new() { TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 13, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _playIcon = new() { FontSize = 17, VerticalAlignment = VerticalAlignment.Center };

    internal MiniPlayerWindow(string title, Action restore, Action playPause, Action mute)
    {
        Title = "WX Player · Mini oynatıcı";
        Width = 500; Height = 340; MinWidth = 360; MinHeight = 240;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.CanResize;
        ShowInTaskbar = false; Topmost = true;
        Background = PremiumWindow.Brush("BackgroundSecondaryBrush");
        Foreground = PremiumWindow.Brush("TextPrimaryBrush");
        FontFamily = (FontFamily)Application.Current.FindResource("AppFont");
        Left = Math.Max(SystemParameters.WorkArea.Left, SystemParameters.WorkArea.Right - Width - 22);
        Top = Math.Max(SystemParameters.WorkArea.Top, SystemParameters.WorkArea.Bottom - Height - 22);

        var frame = new Border { Background = PremiumWindow.Brush("SurfaceBrush"), BorderBrush = PremiumWindow.Brush("BorderSubtleBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(11) };
        Content = frame;
        var layout = new Grid(); frame.Child = layout;
        layout.RowDefinitions.Add(new() { Height = new GridLength(42) });
        layout.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new() { Height = new GridLength(48) });

        var header = new DockPanel { Margin = new Thickness(12, 0, 8, 0), LastChildFill = true };
        layout.Children.Add(header);
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        DockPanel.SetDock(actions, Dock.Right); header.Children.Add(actions);
        var back = PremiumWindow.Action("Ana pencereye dön", restore);
        back.Padding = new Thickness(8, 4, 8, 4); back.FontSize = 10; back.ToolTip = "Görüntüyü ana oynatıcıya taşı";
        actions.Children.Add(back);
        var close = PremiumWindow.Action("×", restore);
        close.Padding = new Thickness(8, 4, 8, 4); close.Margin = new Thickness(4, 0, 0, 0); close.ToolTip = "Mini oynatıcıyı kapat";
        actions.Children.Add(close);
        _title.Text = title; _title.Margin = new Thickness(5, 0, 8, 0); header.Children.Add(_title);
        header.MouseLeftButtonDown += (_, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };

        var videoFrame = new Border { Background = Brushes.Black, Margin = new Thickness(1, 0, 1, 0), Child = Video };
        Grid.SetRow(videoFrame, 1); layout.Children.Add(videoFrame);
        Video.DoubleClicked += restore;

        var footer = new DockPanel { Margin = new Thickness(14, 4, 14, 4) };
        Grid.SetRow(footer, 2); layout.Children.Add(footer);
        var muteButton = PremiumWindow.Action("Ses", mute); muteButton.ToolTip = "Sesi kapat / aç";
        muteButton.Padding = new Thickness(10, 4, 10, 4); DockPanel.SetDock(muteButton, Dock.Right); footer.Children.Add(muteButton);
        var playback = PremiumWindow.Action("", playPause); playback.Padding = new Thickness(11, 4, 11, 4);
        playback.Content = _playIcon; playback.ToolTip = "Oynat / duraklat"; DockPanel.SetDock(playback, Dock.Left); footer.Children.Add(playback);
        footer.Children.Add(new TextBlock { Text = "MİNİ OYNATICI", FontSize = 10, Foreground = PremiumWindow.Brush("TextMutedBrush"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) });
    }

    internal void SetTitle(string title) { _title.Text = title; _title.ToolTip = title; }
    internal void SetPlaying(bool playing) => _playIcon.Text = playing ? "Ⅱ" : "▶";
}
