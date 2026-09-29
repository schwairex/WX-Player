using System.Windows;
using System.Windows.Media;
using WXPlayer.Core;

namespace WXPlayer.App;

public partial class MainWindow
{
    private readonly ChannelHealthMonitor _channelHealth = new(TimeSpan.FromSeconds(15));
    private bool _healthAwaitingPlayback;

    private void BeginHealthCheck(ContentItem item)
    {
        _channelHealth.Stop();
        _healthAwaitingPlayback = _settings.ChannelHealthCheck && item.Kind == ContentKind.Live;
        if (_healthAwaitingPlayback) _channelHealth.Begin(DateTimeOffset.UtcNow);
        UpdateHealthBadge();
    }

    private void HealthPlaybackAccepted()
    {
        _healthAwaitingPlayback = false;
        CheckChannelHealth();
    }

    private void HealthPlaybackFailed()
    {
        _healthAwaitingPlayback = false;
        _channelHealth.MarkFailed(_channelHealth.CurrentGeneration);
        UpdateHealthBadge();
    }

    private void CheckChannelHealth()
    {
        if (!_settings.ChannelHealthCheck || _current?.Kind != ContentKind.Live)
        {
            _channelHealth.Stop();
            UpdateHealthBadge();
            return;
        }
        if (!_healthAwaitingPlayback)
        {
            if (_engine.Player.IsPlaying)
                _channelHealth.MarkPlaying(_channelHealth.CurrentGeneration);
            else if (_engine.Player.State == LibVLCSharp.Shared.VLCState.Error)
                _channelHealth.MarkFailed(_channelHealth.CurrentGeneration);
            if (_engine.Player.State != LibVLCSharp.Shared.VLCState.Paused)
                _channelHealth.Tick(DateTimeOffset.UtcNow);
        }
        UpdateHealthBadge();
    }

    private void UpdateHealthBadge()
    {
        var state = _channelHealth.State;
        HealthBadge.Visibility = state == ChannelHealthState.None ? Visibility.Collapsed : Visibility.Visible;
        HealthText.Text = state switch
        {
            ChannelHealthState.Checking => "○  KONTROL EDİLİYOR",
            ChannelHealthState.Healthy => "●  ÇALIŞIYOR",
            ChannelHealthState.Unavailable => "●  BAĞLANTI YOK",
            _ => ""
        };
        HealthText.Foreground = (Brush)FindResource(state switch
        {
            ChannelHealthState.Healthy => "AccentPrimaryBrush",
            ChannelHealthState.Unavailable => "ErrorBrush",
            _ => "TextSecondaryBrush"
        });
    }

    internal ChannelHealthState SmokeHealthState => _channelHealth.State;
}
