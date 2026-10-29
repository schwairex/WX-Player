using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace WXPlayer.App;

// Visual-only lifetime, separate from playback and the fullscreen timer.
internal sealed class EmbeddedControlsAutoHide : IDisposable
{
    private static readonly TimeSpan HideDelay = TimeSpan.FromSeconds(3);
    private readonly Window _window;
    private readonly NativeVideoHost _video;
    private readonly FrameworkElement[] _layers;
    private readonly Func<bool> _playing, _menuOpen;
    private readonly DispatcherTimer _timer = new();
    private readonly HashSet<FrameworkElement> _hovered = new();
    private bool _disposed, _wasPlaying, _keyboardInteraction;

    internal EmbeddedControlsAutoHide(Window window, NativeVideoHost video, FrameworkElement[] layers, Func<bool> playing, Func<bool> menuOpen)
    {
        _window = window; _video = video; _layers = layers; _playing = playing; _menuOpen = menuOpen;
        _timer.Interval = HideDelay; _timer.Tick += Tick;
        window.PreviewMouseMove += PointerMoved;
        window.PreviewMouseDown += PointerDown;
        window.PreviewKeyDown += KeyDown;
        window.GotKeyboardFocus += FocusChanged;
        window.LostKeyboardFocus += FocusChanged;
        window.Closed += Closed;
        video.PointerMoved += NativePointerMoved;
        video.KeyPressed += NativeKeyPressed;
        foreach (var layer in layers)
        {
            layer.MouseEnter += LayerEntered; layer.MouseLeave += LayerLeft;
            if (layer.IsMouseOver) _hovered.Add(layer);
        }
        RefreshState();
    }
    internal void RefreshState()
    {
        if (_disposed) return;
        bool playing = _playing();
        if (!playing) { Show(); _timer.Stop(); }
        else if (!_wasPlaying) { Show(); Restart(); }
        _wasPlaying = playing;
    }
    private void Show()
    {
        foreach (var layer in _layers) if (layer.Visibility != Visibility.Visible) layer.Visibility = Visibility.Visible;
    }
    private void Restart() { _timer.Stop(); if (!_disposed && _playing() && _hovered.Count == 0) _timer.Start(); }
    private void Activity(bool keyboard)
    {
        if (_disposed) return;
        _keyboardInteraction = keyboard; Show(); Restart();
    }
    internal void KeyboardEntry() => Activity(true);
    private void PointerMoved(object sender, MouseEventArgs e)
    {
        Activity(false);
        // Movement never invokes a command or marks a click/drag as handled.
    }
    private void PointerDown(object sender, MouseButtonEventArgs e) => Activity(false);
    private void LayerEntered(object sender, MouseEventArgs e) { if (_disposed) return; _hovered.Add((FrameworkElement)sender); Show(); _timer.Stop(); }
    private void LayerLeft(object sender, MouseEventArgs e) { if (_disposed) return; _hovered.Remove((FrameworkElement)sender); Restart(); }
    private bool InControls(object? value) => value is DependencyObject target && _layers.Any(l => ReferenceEquals(l,target) || l.IsAncestorOf(target));
    private void KeyDown(object sender, KeyEventArgs e) => Activity(e.Key == Key.Tab || InControls(e.OriginalSource));
    private void FocusChanged(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (_disposed) return;
        bool entering = InControls(e.NewFocus);
        if (!entering && !_keyboardInteraction) return;
        _keyboardInteraction = entering;
        Show(); Restart();
    }
    private void NativePointerMoved() => Activity(false);
    private void NativeKeyPressed(Key key) => Activity(false);
    private void Closed(object? sender, EventArgs e) => Dispose();
    private void Tick(object? sender, EventArgs e)
    {
        _timer.Stop();
        if (_disposed) return;
        if (!_playing()) { Show(); return; }
        if (_menuOpen() || Mouse.Captured is not null || _hovered.Count > 0 || _keyboardInteraction) { Restart(); return; }
        foreach (var layer in _layers) layer.Visibility = Visibility.Hidden;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _timer.Stop(); _timer.Tick -= Tick;
        _window.PreviewMouseMove -= PointerMoved; _window.PreviewMouseDown -= PointerDown;
        _window.PreviewKeyDown -= KeyDown; _window.GotKeyboardFocus -= FocusChanged; _window.Closed -= Closed;
        _window.LostKeyboardFocus -= FocusChanged;
        _video.PointerMoved -= NativePointerMoved; _video.KeyPressed -= NativeKeyPressed;
        foreach (var layer in _layers) { layer.MouseEnter -= LayerEntered; layer.MouseLeave -= LayerLeft; }
        _hovered.Clear();
        Show();
    }
}
