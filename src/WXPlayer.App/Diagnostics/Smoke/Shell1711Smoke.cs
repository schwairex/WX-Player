using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WXPlayer.App;

internal static class Shell1711Smoke
{
    internal static async Task RunAsync(MainWindow window, Dictionary<string, object> results)
    {
        void Check(string key, bool ok) { results[key] = ok; if (!ok) throw new InvalidOperationException(key); }
        window.SmokeFullscreen(); await Task.Delay(200);
        try
        {
            Check("fullscreenDoesNotForceTopmost", !window.Topmost);
            var other = new Window { Title = "WXPlayer QA independent application", Width = 360, Height = 240, WindowStartupLocation = WindowStartupLocation.CenterScreen };
            try
            {
                other.Show(); other.Activate(); await Task.Delay(300);
                var probe = new WindowInteropHelper(other).Handle;
                var owner = new WindowInteropHelper(window).Handle;
                Check("otherWindowCanAppearAboveFullscreen", Above(probe, owner));
                other.Close(); window.Activate(); await Task.Delay(100);
                Check("reactivationDoesNotForceTopmost", !window.Topmost);
                var chrome = window.OwnedWindows.Cast<Window>().Single(w => w.Title == "WX Player controls");
                chrome.Activate(); await Task.Delay(100);
                Check("fullscreenControlsRetainOwnership", chrome.Owner == window && !chrome.Topmost && !window.Topmost);
                var bounds = FullscreenPlacement.WindowBounds(window);
                Check("fullscreenStillCoversMonitor", bounds.Width > 900 && bounds.Height > 650);
            }
            finally { if (other.IsVisible) other.Close(); }
        }
        finally { window.SmokeFullscreen(); }
        await Task.Delay(100);
        Check("normalWindowRemainsNonTopmost", !window.Topmost);
    }
    internal static bool Above(IntPtr upper, IntPtr lower)
    {
        for (var handle = GetWindow(upper, 2); handle != IntPtr.Zero; handle = GetWindow(handle, 2))
            if (handle == lower) return true;
        return false;
    }
    [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr handle, uint command);
}
