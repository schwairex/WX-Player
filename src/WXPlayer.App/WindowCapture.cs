using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WXPlayer.App;

/// <summary>QA capture of this application's own HWND. Never captures the desktop or other windows.</summary>
internal static class WindowCapture
{
    [StructLayout(LayoutKind.Sequential)]private struct BitmapInfo
    {public uint Size;public int Width,Height;public ushort Planes,BitCount;public uint Compression,SizeImage;public int XPels,YPels;public uint ColorsUsed,ColorsImportant;}
    public static bool Save(Window window,string path)
    {
        var hwnd=new WindowInteropHelper(window).Handle;var rect=FullscreenPlacement.WindowBounds(window);
        if(GetForegroundWindow()!=hwnd)return false;
        IntPtr dc=GetWindowDC(hwnd),memory=CreateCompatibleDC(dc),bitmap=CreateCompatibleBitmap(dc,rect.Width,rect.Height),previous=SelectObject(memory,bitmap);
        try
        {
            if(!BitBlt(memory,0,0,rect.Width,rect.Height,dc,0,0,0x40CC0020))return false;
            var info=new BitmapInfo{Size=40,Width=rect.Width,Height=-rect.Height,Planes=1,BitCount=32};var bytes=new byte[checked(rect.Width*rect.Height*4)];
            if(GetDIBits(memory,bitmap,0,(uint)rect.Height,bytes,ref info,0)==0)return false;
            var image=BitmapSource.Create(rect.Width,rect.Height,96,96,PixelFormats.Bgr32,null,bytes,rect.Width*4);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var output=File.Create(path);encoder.Save(output);return true;
        }
        finally{SelectObject(memory,previous);DeleteObject(bitmap);DeleteDC(memory);ReleaseDC(hwnd,dc);}
    }
    [DllImport("user32.dll")]private static extern IntPtr GetWindowDC(IntPtr hwnd);
    [DllImport("user32.dll")]private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]private static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
    [DllImport("gdi32.dll")]private static extern bool BitBlt(IntPtr target,int x,int y,int width,int height,IntPtr source,int sourceX,int sourceY,uint operation);
    [DllImport("gdi32.dll")]private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")]private static extern IntPtr CreateCompatibleBitmap(IntPtr dc,int width,int height);
    [DllImport("gdi32.dll")]private static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
    [DllImport("gdi32.dll")]private static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")]private static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")]private static extern int GetDIBits(IntPtr dc,IntPtr bitmap,uint start,uint lines,byte[] bits,ref BitmapInfo info,uint usage);
}
