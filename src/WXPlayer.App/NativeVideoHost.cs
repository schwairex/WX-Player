using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using LibVLCSharp.Shared;

namespace WXPlayer.App;

/// <summary>A child HWND confined to the video rectangle. No detached transparent window covers the app.</summary>
public sealed class NativeVideoHost : HwndHost
{
    private MediaPlayer? _player;
    public MediaPlayer? MediaPlayer
    {
        get=>_player;
        set{if(_player is not null&&_player.Hwnd==Handle)_player.Hwnd=IntPtr.Zero;_player=value;if(_player is not null&&Handle!=IntPtr.Zero)_player.Hwnd=Handle;}
    }
    public event Action? PointerMoved;
    public event Action<int>? WheelMoved;
    public event Action? Clicked;
    public event Action? DoubleClicked;
    public event Action<Key>? KeyPressed;
    private bool _subscribed;private int _lastX=int.MinValue,_lastY=int.MinValue;private uint _lastClick;
    protected override HandleRef BuildWindowCore(HandleRef parent)
    {
        var hwnd=CreateWindowEx(0,"static","WX Player video",0x40000000|0x10000000|0x04000000|0x02000000|0x00000104,0,0,1,1,parent.Handle,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);
        if(hwnd==IntPtr.Zero)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        if(_player is not null)_player.Hwnd=hwnd;
        if(!_subscribed){ComponentDispatcher.ThreadPreprocessMessage+=Preprocess;_subscribed=true;}
        return new HandleRef(this,hwnd);
    }
    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        if(_subscribed){ComponentDispatcher.ThreadPreprocessMessage-=Preprocess;_subscribed=false;}
        if(_player is not null&&_player.Hwnd==hwnd.Handle)_player.Hwnd=IntPtr.Zero;
        DestroyWindow(hwnd.Handle);
    }
    private void Preprocess(ref MSG message,ref bool handled)
    {
        if(handled||Handle==IntPtr.Zero||!(message.hwnd==Handle||IsChild(Handle,message.hwnd)))return;
        switch(message.message)
        {
            case 0x0200:
                int x=(short)(message.lParam.ToInt64()&0xffff),y=(short)((message.lParam.ToInt64()>>16)&0xffff);
                if(x!=_lastX||y!=_lastY){_lastX=x;_lastY=y;PointerMoved?.Invoke();}break;
            case 0x020A:WheelMoved?.Invoke((short)((message.wParam.ToInt64()>>16)&0xffff));handled=true;break;
            case 0x0201:
                uint now=unchecked((uint)Environment.TickCount);
                if(now-_lastClick<GetDoubleClickTime()){_lastClick=0;DoubleClicked?.Invoke();}else{_lastClick=now;Clicked?.Invoke();}handled=true;break;
            case 0x0203:DoubleClicked?.Invoke();handled=true;break;
            case 0x0100:KeyPressed?.Invoke(KeyInterop.KeyFromVirtualKey(message.wParam.ToInt32()));handled=true;break;
        }
    }
    protected override IntPtr WndProc(IntPtr hwnd,int msg,IntPtr wParam,IntPtr lParam,ref bool handled)
    {
        if(msg==0x0014){handled=true;return new IntPtr(1);}return base.WndProc(hwnd,msg,wParam,lParam,ref handled);
    }
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]private static extern IntPtr CreateWindowEx(int extended,string cls,string title,int style,int x,int y,int width,int height,IntPtr parent,IntPtr menu,IntPtr instance,IntPtr param);
    [DllImport("user32.dll")]private static extern bool DestroyWindow(IntPtr hwnd);
    [DllImport("user32.dll")]private static extern bool IsChild(IntPtr parent,IntPtr child);
    [DllImport("user32.dll")]private static extern uint GetDoubleClickTime();
}
