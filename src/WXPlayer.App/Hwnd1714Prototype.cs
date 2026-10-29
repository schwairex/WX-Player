using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LibVLCSharp.Shared;
using WXPlayer.Core;

namespace WXPlayer.App;

// Approval gate only. Never enabled by normal startup; does not persist settings.
internal static class Hwnd1714Prototype
{
    private static double CandidateScale => (double)Application.Current.FindResource("ComfortModeScale");
    internal static async Task RunAsync(MainWindow window, LibraryStore store, PlaybackEngine engine, Dictionary<string, object> results)
    {
        void Check(string name, bool value) { results[name] = value; if (!value) throw new InvalidOperationException(name); }
        var source = new SourceConfig { Id = "hwnd1714", Name = "Yerel HWND prototipi", Address = Path.Combine(AppContext.BaseDirectory,"samples","open-films.m3u") };
        string media = new Uri(Path.GetFullPath(App.Arguments[Array.IndexOf(App.Arguments,"--media")+1])).AbsoluteUri;
        var items = new[] { ContentKind.Movie, ContentKind.Live, ContentKind.Episode }.Select(kind => new ContentItem { Id="hwnd1714-"+kind, SourceId=source.Id, Name="HWND · "+kind, Kind=kind, Url=media }).ToArray();
        async IAsyncEnumerable<ContentItem> Items() { foreach(var item in items) yield return item; await Task.Yield(); }
        await store.ImportAsync(source,Items(),null,default); await window.SmokeRefreshAsync(source.Id); await window.SmokeBrowseAsync("all");
        var savedWidth=window.Width;var savedHeight=window.Height;
        // Match the proposed startup-only outer WPF root, before any stream starts.
        var content=(UIElement)window.Content;window.Content=null;
        var comfortRoot=new Border{Child=content};window.Content=comfortRoot;
        results["hwndPrototypeScale"]=CandidateScale;
        results["hwndDesktopDips"]=SystemParameters.VirtualScreenWidth+"x"+SystemParameters.VirtualScreenHeight;
        try
        {
            foreach(var item in items)
            {
                await window.SmokePlayAsync(item);await Wait(()=>engine.Player.IsPlaying&&engine.Player.Time>500);
                IntPtr render=engine.Player.Hwnd;
                foreach(var size in new[]{(1920d,1080d),(1366d,768d)})
                {
                    window.Width=size.Item1;window.Height=size.Item2;window.UpdateLayout();await Task.Delay(150);
                    var original=comfortRoot.LayoutTransform;
                    try
                    {
                        int phase=0;
                        foreach(double factor in new[]{1d,CandidateScale,1d})
                        {
                            string key=$"hwnd_{item.Kind}_{size.Item1:0}_{phase++}_{factor:0.0}";
                            long before=engine.Player.Time;
                            comfortRoot.LayoutTransform=new ScaleTransform(factor,factor);
                            window.UpdateLayout();await Task.Delay(350);
                            var start=window.Video.PointToScreen(new Point());
                            var end=window.Video.PointToScreen(new Point(window.Video.ActualWidth,window.Video.ActualHeight));
                            GetWindowRect(window.Video.Handle,out var host);GetWindowRect(render,out var actual);
                            var expected=new[]{start.X,start.Y,end.X-start.X,end.Y-start.Y};
                            var measured=new[]{(double)actual.Left,actual.Top,actual.Right-actual.Left,actual.Bottom-actual.Top};
                            results[key+"_bounds"]=new{expected,measured,host=new[]{host.Left,host.Top,host.Right-host.Left,host.Bottom-host.Top},dpi=VisualTreeHelper.GetDpi(window.Video).DpiScaleX};
                            Check(key+"_aligned",expected.Zip(measured).All(p=>Math.Abs(p.First-p.Second)<=2));
                            Check(key+"_sameVideoSession",engine.Player.Hwnd==render&&GetParent(render)==window.Video.Handle&&IsWindowVisible(render)&&engine.Player.IsPlaying&&engine.Player.Time>=before-300);
                        }
                    }
                    finally {comfortRoot.LayoutTransform=original;window.UpdateLayout();}
                }
            }
            results["hwndPrototypePassed"]=true;
        }
        finally {window.Width=savedWidth;window.Height=savedHeight;window.UpdateLayout();}
    }
    private static async Task Wait(Func<bool> test)
    {
        var end=DateTime.UtcNow.AddSeconds(15);while(!test()&&DateTime.UtcNow<end)await Task.Delay(40);
        if(!test())throw new TimeoutException("HWND prototype playback wait");
    }
    [StructLayout(LayoutKind.Sequential)]private struct Rect{public int Left,Top,Right,Bottom;}
    [DllImport("user32.dll")]private static extern bool GetWindowRect(IntPtr hwnd,out Rect rect);
    [DllImport("user32.dll")]private static extern IntPtr GetParent(IntPtr hwnd);
    [DllImport("user32.dll")]private static extern bool IsWindowVisible(IntPtr hwnd);
}
