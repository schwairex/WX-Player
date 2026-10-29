using System.ComponentModel;
using System.Windows.Controls;

namespace WXPlayer.App;

public partial class MainWindow
{
    private EmbeddedControlsAutoHide? _liveAutoHide;
    private void StopEmbeddedAutoHide() { _liveAutoHide?.Dispose(); _liveAutoHide = null; }
    private void LiveOwnerClosing(object? sender, CancelEventArgs e) { if (_closing) StopEmbeddedAutoHide(); }
    private void UpdateEmbeddedAutoHide()
    {
        if (!_liveShellAttached || _liveControls is null || _liveOverlayRoot is null || _miniPlayer is not null || _closing)
        { StopEmbeddedAutoHide(); return; }
        if (_liveAutoHide is null)
        {
            var layers = _liveOverlayRoot.Children.OfType<Border>().Where(b => b.IsAncestorOf(NowTitle) || b.IsAncestorOf(ControlsBorder)).ToArray();
            _liveAutoHide = new EmbeddedControlsAutoHide(_liveControls, Video, layers,
                () => !_closing && _engine is not null && _engine.Player.IsPlaying,
                () => _liveGearPopup?.IsOpen == true || SeekSlider.IsInteracting || VolumeSlider.IsInteracting);
        }
        _liveAutoHide.RefreshState();
    }
}
