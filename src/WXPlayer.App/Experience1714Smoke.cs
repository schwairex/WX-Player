using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using WXPlayer.Core;

namespace WXPlayer.App;

// Explicit, isolated QA. Routed WPF input, native video, and existing player actions.
internal static class Experience1714Smoke
{
    internal static async Task RunEmbeddedAsync(MainWindow window, LibraryStore store, PlaybackEngine engine, Dictionary<string, object> results)
    {
        void Check(string key, bool ok) { results[key] = ok; if (!ok) throw new InvalidOperationException(key); }
        var source = new SourceConfig { Id = "embedded1714", Name = "Yerel QA", Address = Path.Combine(AppContext.BaseDirectory, "samples", "open-films.m3u") };
        string media = App.Arguments[Array.IndexOf(App.Arguments, "--media") + 1];
        var item = new ContentItem { Id = "embedded1714-movie", SourceId = source.Id, Name = "Sintel", Kind = ContentKind.Movie, Url = new Uri(Path.GetFullPath(media)).AbsoluteUri };
        async IAsyncEnumerable<ContentItem> Items() { yield return item; await Task.Yield(); }
        await store.ImportAsync(source, Items(), null, default);
        await window.SmokeRefreshAsync(source.Id); await window.SmokeBrowseAsync("all"); await window.SmokePlayAsync(item);
        await Wait(() => engine.Player.IsPlaying && window.Video.IsVisible);
        var overlay = window.OwnedWindows.Cast<Window>().Single(w => w.Title == "WX Player live controls");
        var layers = ((Grid)overlay.Content).Children.OfType<Border>().Where(b => b.IsAncestorOf(window.NowTitle) || b.IsAncestorOf(window.ControlsBorder)).ToArray();
        Check("embeddedTwoChromeLayers", layers.Length == 2);
        IntPtr handle = engine.Player.Hwnd;
        void MoveTo(FrameworkElement element, double dx = 0)
        {
            foreach (var layer in layers)
                layer.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = layer.IsAncestorOf(element) ? Mouse.MouseEnterEvent : Mouse.MouseLeaveEvent });
            overlay.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.PreviewMouseMoveEvent });
        }
        results["embeddedInputDriver"]="WPF routed events; desktop cursor is not moved";
        overlay.Activate(); Keyboard.ClearFocus(); MoveTo(window.VideoBorder);
        await Task.Delay(1800);
        Check("embeddedVisibleBeforeThreeSeconds", layers.All(b => b.IsVisible));
        await Task.Delay(1650);
        Check("embeddedHidesAfterThreeSeconds", layers.All(b => !b.IsVisible));
        window.Activate();
        var bridge=window.ViewingPanel.Children.OfType<Control>().Single(c=>AutomationProperties.GetName(c)=="Oynatıcı kontrolleri");
        bridge.Focus();await Task.Delay(100);
        Check("embeddedTabBridgeRevealsAndFocuses",layers.All(b=>b.IsVisible)&&ReferenceEquals(Keyboard.FocusedElement,window.PreviousChannelButton));
        await Task.Delay(3400);
        Check("embeddedTabBridgeProtectsKeyboardFocus",layers.All(b=>b.IsVisible));
        long before = engine.Player.Time; MoveTo(window.VideoBorder, 8); await Task.Delay(160);
        Check("embeddedFirstMoveOnlyReveals", layers.All(b => b.IsVisible) && engine.Player.IsPlaying && engine.Player.Hwnd == handle && engine.Player.Time >= before);
        MoveTo(window.PlayButton); await Task.Delay(3400);
        Check("embeddedHoverControlsStaysVisible", layers.All(b => b.IsVisible));
        var gear = Settings1711Smoke.Descendants<ToggleButton>(overlay).Single(b => AutomationProperties.GetName(b) == "Oynatıcı seçenekleri");
        gear.IsChecked = true; MoveTo(window.VideoBorder, 12); await Task.Delay(3400);
        Check("embeddedOpenMenuStaysVisible", layers.All(b => b.IsVisible)); gear.IsChecked = false;
        window.SmokeTogglePlayback(); await Wait(() => engine.Player.State == VLCState.Paused); await Task.Delay(3400);
        Check("embeddedPausedStaysVisible", layers.All(b => b.IsVisible));
        window.SmokeTogglePlayback(); await Wait(() => engine.Player.IsPlaying);
        overlay.Activate(); window.PlayButton.Focus();
        window.PlayButton.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(overlay), 0, Key.Tab) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        await Task.Delay(3400);
        Check("embeddedKeyboardControlsStayVisible", layers.All(b => b.IsVisible));
        Keyboard.ClearFocus(); MoveTo(window.VideoBorder, 16);
        window.SeekSlider.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left) { RoutedEvent=UIElement.PreviewMouseLeftButtonDownEvent });
        Check("embeddedSliderInteractionStarted", window.SeekSlider.IsInteracting); await Task.Delay(3400);
        Check("embeddedSliderCaptureStaysVisible", layers.All(b => b.IsVisible));
        window.SeekSlider.Value=.1; Mouse.Capture(null);
        window.SeekSlider.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice,Environment.TickCount) { RoutedEvent=Mouse.LostMouseCaptureEvent });
        window.SmokeToggleMiniPlayer(); await Wait(() => window.SmokeMiniPlayerVisible); await Task.Delay(150);
        var controllerField = typeof(MainWindow).GetField("_liveAutoHide", BindingFlags.Instance | BindingFlags.NonPublic);
        Check("embeddedMiniStopsAutoHide", controllerField is not null && controllerField.GetValue(window) is null);
        window.SmokeToggleMiniPlayer(); await Wait(() => !window.SmokeMiniPlayerVisible && engine.Player.Hwnd == handle);
        await Task.Delay(150); MoveTo(window.VideoBorder, 20);
        Settings1711Smoke.Save(overlay, "WXPlayer-1.7.14-embedded-visible.png");
        await Task.Delay(3400); Check("embeddedHidesAfterMiniReturn", layers.All(b => !b.IsVisible));
        Settings1711Smoke.Save(overlay, "WXPlayer-1.7.14-embedded-hidden.png");
        await window.SmokeBrowseAsync("home");
        Check("embeddedNavigationDisposesAutoHide", controllerField!.GetValue(window) is null && !overlay.IsVisible && engine.Player.Hwnd == handle);
        window.SmokeFullscreen(); await Task.Delay(120);
        var fullscreenTimer = (DispatcherTimer)typeof(MainWindow).GetField("_hideControls", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
        Check("embeddedFullscreenTimeoutUnchanged", fullscreenTimer.Interval == TimeSpan.FromSeconds(3.5));
        window.SmokeFullscreen();
    }

    private static async Task Wait(Func<bool> test)
    {
        var end = DateTime.UtcNow.AddSeconds(12);
        while (!test() && DateTime.UtcNow < end) await Task.Delay(40);
        if (!test()) throw new TimeoutException("1.7.14 UI QA wait");
    }
}
