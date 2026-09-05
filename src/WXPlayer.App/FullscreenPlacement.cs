using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WXPlayer.App;

internal sealed class FullscreenPlacement
{
    [StructLayout(LayoutKind.Sequential)]internal struct Rect {public int Left,Top,Right,Bottom;public int Width=>Right-Left;public int Height=>Bottom-Top;}
    [StructLayout(LayoutKind.Sequential)]private struct Point {public int X,Y;}
    [StructLayout(LayoutKind.Sequential)]private struct Placement {public int Length,Flags,ShowCmd;public Point Min,Max;public Rect Normal;}
    [StructLayout(LayoutKind.Sequential)]private struct MonitorInfo {public int Size;public Rect Monitor,Work;public uint Flags;}
    private Placement _saved;private WindowStyle _style;private ResizeMode _resize;private bool _topmost;
    public Rect Bounds {get;private set;}
    public void Enter(Window window)
    {
        IntPtr hwnd=new WindowInteropHelper(window).Handle;
        _saved=new Placement{Length=Marshal.SizeOf<Placement>()};GetWindowPlacement(hwnd,ref _saved);
        _style=window.WindowStyle;_resize=window.ResizeMode;_topmost=window.Topmost;
        var info=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};if(!GetMonitorInfo(MonitorFromWindow(hwnd,2),ref info))throw new InvalidOperationException("Ekran boyutu okunamadı.");Bounds=info.Monitor;
        window.WindowState=WindowState.Normal;window.WindowStyle=WindowStyle.None;window.ResizeMode=ResizeMode.NoResize;window.Topmost=true;
        SetWindowPos(hwnd,new IntPtr(-1),Bounds.Left,Bounds.Top,Bounds.Width,Bounds.Height,0x0020|0x0040);
    }
    public void Exit(Window window)
    {
        window.Topmost=_topmost;window.WindowStyle=_style;window.ResizeMode=_resize;
        SetWindowPlacement(new WindowInteropHelper(window).Handle,ref _saved);
    }
    public void PlaceControls(Window owner,Window controls)
    {
        var ownerHwnd=new WindowInteropHelper(owner).Handle;double scale=GetDpiForWindow(ownerHwnd)/96d;
        int width=(int)(Math.Min(900,Math.Max(360,Bounds.Width/scale-48))*scale),height=(int)(controls.Height*scale);
        controls.Width=width/scale;
        SetWindowPos(new WindowInteropHelper(controls).Handle,IntPtr.Zero,Bounds.Left+(Bounds.Width-width)/2,Bounds.Bottom-height-(int)(24*scale),width,height,0x0010|0x0004);
    }
    public static Rect WindowBounds(Window window){GetWindowRect(new WindowInteropHelper(window).Handle,out var rect);return rect;}
    [DllImport("user32.dll")]private static extern bool GetWindowPlacement(IntPtr hwnd,ref Placement placement);
    [DllImport("user32.dll")]private static extern bool SetWindowPlacement(IntPtr hwnd,ref Placement placement);
    [DllImport("user32.dll")]private static extern IntPtr MonitorFromWindow(IntPtr hwnd,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")]private static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")]private static extern bool GetWindowRect(IntPtr hwnd,out Rect rect);
    [DllImport("user32.dll")]private static extern uint GetDpiForWindow(IntPtr hwnd);
}
