using System.Windows;
using System.Windows.Input;

namespace WXPlayer.App;

public partial class MainWindow
{
    private MiniPlayerWindow? _miniPlayer;

    private void ApplyViewingOptions()
    {
        MiniPlayerButton.Visibility = _settings.MiniPlayerEnabled ? Visibility.Visible : Visibility.Collapsed;
        if (!_settings.MiniPlayerEnabled) CloseMiniPlayer();
        if (_current is { } item) { BeginHealthCheck(item); HealthPlaybackAccepted(); }
        else { _channelHealth.Stop(); UpdateHealthBadge(); }
    }

    private void MiniPlayer_Click(object sender, RoutedEventArgs e) => ToggleMiniPlayer();

    private void ToggleMiniPlayer()
    {
        if (_miniPlayer is not null) { CloseMiniPlayer(); return; }
        if (!_ready || !_settings.MiniPlayerEnabled || _current is null || _target is null)
        {
            Status("Mini oynatıcıyı açmak için önce bir içerik oynatın.");
            return;
        }
        if (_fullscreen) ToggleFullscreen();
        var mini = new MiniPlayerWindow(_current.Name, () => CloseMiniPlayer(),
            () => PlayPause_Click(this, new RoutedEventArgs()),
            () => Mute_Click(this, new RoutedEventArgs()));
        mini.Closing += (_, _) =>
        {
            // Transfer the live render HWND before WPF destroys the mini host (also on Alt+F4).
            if (ReferenceEquals(_miniPlayer, mini)) CloseMiniPlayer(false);
        };
        mini.Video.WheelMoved += delta => VolumeSlider.Value = Math.Clamp(VolumeSlider.Value + (delta > 0 ? 5 : -5), 0, 100);
        mini.Video.KeyPressed += key => { if (key == Key.P) CloseMiniPlayer(); else HandleShortcut(key); };
        _miniPlayer = mini;
        try
        {
            mini.Show();
            Video.TransferTo(mini.Video);
            Video.Visibility = Visibility.Collapsed;
            MiniPlaceholder.Visibility = Visibility.Visible;
            _engine.Player.AspectRatio = null;
            _engine.Player.CropGeometry = null;
            _engine.Player.Scale = 0;
            mini.SetPlaying(_engine.Player.IsPlaying);
        }
        catch { CloseMiniPlayer(); throw; }
    }

    private void CloseMiniPlayer(bool closeWindow = true)
    {
        if (_miniPlayer is not { } mini) return;
        _miniPlayer = null;
        Video.Visibility = _current is null ? Visibility.Collapsed : Visibility.Visible;
        if (_current is not null)
        {
            UpdateLayout();
            if (mini.Video.RenderingHandle != IntPtr.Zero) mini.Video.TransferTo(Video);
        }
        if (closeWindow && mini.IsVisible) mini.Close();
        MiniPlaceholder.Visibility = Visibility.Collapsed;
        if (_engine is not null)
        {
            _appliedCrop = null;
            UpdateVideoSizing();
        }
    }

    internal bool SmokeMiniPlayerVisible => _miniPlayer?.IsVisible == true;
    internal IntPtr SmokeMiniVideoHandle => _miniPlayer?.Video.RenderingHandle ?? IntPtr.Zero;
    internal Window? SmokeMiniWindow => _miniPlayer;
    internal void SmokeToggleMiniPlayer() => ToggleMiniPlayer();
}
