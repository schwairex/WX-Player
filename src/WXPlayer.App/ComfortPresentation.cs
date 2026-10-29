using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WXPlayer.Core;

namespace WXPlayer.App;

// Presentation only. OS DPI remains owned by WPF; the native video HWND is not reparented.
public static class ComfortPresentation
{
    private const double MinimumLogicalWidth = 1100, MinimumLogicalHeight = 650, WorkAreaRatio = .9;
    private static readonly List<WindowScale> Windows = [];
    private static readonly ConditionalWeakTable<FrameworkElement, PopupScale> Popups = new();
    private static readonly List<WeakReference<PopupScale>> OpenPopups = [];
    private static PlayerSettings? _settings;
    private static WindowScale? _main;
    private static Func<bool>? _ready;
    private static bool _enabled, _refreshing;
    internal static event Action? Changed;
    internal static double Factor => _enabled ? _main?.Factor ?? 1 : 1;
    private static double Requested => (double)Application.Current.FindResource("ComfortModeScale");

    internal static void Initialize(Window window, FrameworkElement root, PlayerSettings settings, Func<bool> ready)
    {
        _settings = settings; _enabled = settings.ComfortMode; _ready = ready;
        _main = Register(window, root, true);
        ComponentDispatcher.ThreadPreprocessMessage += Preprocess;
        Application.Current.Exit += Exiting;
    }
    internal static void Attach(PremiumWindow window)
    {
        if (window.Content is FrameworkElement root) Register(window, root, false);
    }
    private static WindowScale Register(Window window, FrameworkElement root, bool main)
    {
        var state = new WindowScale(window, root, main); Windows.Add(state);
        window.PreviewKeyDown += KeyDown; window.Closed += WindowClosed;
        return state;
    }
    internal static double NormalWidth(Window window)
    {
        var state = Windows.FirstOrDefault(w => ReferenceEquals(w.Window, window));
        return state is { Scaled: true } ? state.NormalWidth : window.ActualWidth;
    }
    internal static int ArtworkWidth(FrameworkElement view,int width)
    {
        var window=Window.GetWindow(view);
        if(!_enabled||window is MiniPlayerWindow)return width;
        double factor=Windows.FirstOrDefault(w=>ReferenceEquals(w.Window,window))?.Factor??Factor;
        return (int)Math.Clamp(Math.Ceiling(width*factor*VisualTreeHelper.GetDpi(view).DpiScaleX),1,int.MaxValue);
    }
    internal static void ApplySettings()
    {
        if (_settings is null) return;
        _enabled = _settings.ComfortMode; Refresh();
    }
    internal static void Toggle()
    {
        if (_settings is null || _ready?.Invoke() != true) return;
        bool previous = _settings.ComfortMode; _settings.ComfortMode = !previous;
        try { App.SaveSettings(_settings); }
        catch { _settings.ComfortMode = previous; throw; }
        ApplySettings();
    }
    private static void KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Handled || e.Key != Key.F8 || Keyboard.Modifiers != ModifierKeys.None || _ready?.Invoke() != true) return;
        e.Handled = true; if (!e.IsRepeat) Toggle();
    }
    private static void Preprocess(ref MSG message, ref bool handled)
    {
        if (handled || message.message != 0x0100 || message.wParam.ToInt32() != 0x77 || _ready?.Invoke() != true) return;
        if ((GetAsyncKeyState(0x10) & 0x8000) != 0 || (GetAsyncKeyState(0x11) & 0x8000) != 0 || (GetAsyncKeyState(0x12) & 0x8000) != 0) return;
        // Own-thread keyboard message, before NativeVideoHost consumes it. Bit 30 rejects auto-repeat.
        handled = true; if ((message.lParam.ToInt64() & (1L << 30)) == 0) Toggle();
    }
    private static void WindowClosed(object? sender, EventArgs e)
    {
        if (sender is not Window window) return;
        window.PreviewKeyDown -= KeyDown; window.Closed -= WindowClosed;
        var state = Windows.FirstOrDefault(w => ReferenceEquals(w.Window, window));
        state?.Dispose(); if (state is not null) Windows.Remove(state);
    }
    private static void Exiting(object sender, ExitEventArgs e)
    {
        ComponentDispatcher.ThreadPreprocessMessage -= Preprocess; Application.Current.Exit -= Exiting;
        foreach (var state in Windows.ToArray()) { state.Window.PreviewKeyDown -= KeyDown; state.Window.Closed -= WindowClosed; state.Dispose(); }
        Windows.Clear(); OpenPopups.Clear(); Changed = null; _settings = null; _main = null; _ready = null;
    }
    private static void Refresh()
    {
        if (_refreshing) return;
        _refreshing = true;
        try
        {
            foreach (var state in Windows.OrderByDescending(s => s.IsMain).ToArray()) state.Apply();
            Changed?.Invoke();
            foreach (var state in Windows) if (state.Window.IsLoaded) state.Window.UpdateLayout();
            for (int i = OpenPopups.Count - 1; i >= 0; i--)
                if (OpenPopups[i].TryGetTarget(out var popup) && popup.View.IsLoaded) popup.RequestApply(); else OpenPopups.RemoveAt(i);
        }
        finally { _refreshing = false; }
    }
    private static Rect WorkArea(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (hwnd != IntPtr.Zero && GetMonitorInfo(MonitorFromWindow(hwnd, 2), ref info))
        {
            double dpi = Math.Max(1, GetDpiForWindow(hwnd)) / 96d;
            return new Rect(info.Work.Left / dpi, info.Work.Top / dpi, (info.Work.Right - info.Work.Left) / dpi, (info.Work.Bottom - info.Work.Top) / dpi);
        }
        return SystemParameters.WorkArea;
    }
    private sealed class WindowScale : IDisposable
    {
        internal readonly Window Window;
        internal readonly bool IsMain;
        private readonly FrameworkElement _root;
        private readonly PropertySlot _transform, _format;
        private bool _updating, _captured;
        private double _normalWidth, _normalHeight, _minWidth, _minHeight, _maxWidth, _maxHeight, _lastWidth, _lastHeight;
        private Rect _lastArea;
        internal bool Scaled { get; private set; }
        internal double Factor { get; private set; } = 1;
        internal double NormalWidth => _normalWidth;
        internal WindowScale(Window window, FrameworkElement root, bool main)
        {
            Window = window; _root = root; IsMain = main;
            _transform = new(root, FrameworkElement.LayoutTransformProperty); _format = new(root, TextOptions.TextFormattingModeProperty);
            window.Loaded += Loaded; window.SizeChanged += Resized; window.DpiChanged += DpiChanged; window.LocationChanged += Moved;
        }
        private void Loaded(object sender, RoutedEventArgs e) => Refresh();
        private void DpiChanged(object sender, DpiChangedEventArgs e) => Refresh();
        private void Moved(object? sender, EventArgs e) { if (_enabled && Window.IsLoaded && WorkArea(Window) != _lastArea) Refresh(); }
        private void Resized(object sender, SizeChangedEventArgs e)
        {
            if (_updating || _refreshing) return;
            if (Scaled && !IsMain)
            {
                // The previous display size may have been capped to the monitor's work area.
                // Convert the changed dimension directly, so Apply does not undo a user shrink.
                if (e.WidthChanged && Math.Abs(Window.Width - _lastWidth) > .01) _normalWidth = Window.Width / Factor;
                if (e.HeightChanged && Math.Abs(Window.Height - _lastHeight) > .01) _normalHeight = Window.Height / Factor;
            }
            Refresh();
        }
        internal void Apply()
        {
            if (_updating || !Window.IsLoaded) return;
            _updating = true;
            try
            {
                if (!Scaled && !IsMain)
                {
                    _normalWidth = Window.Width; _normalHeight = Window.Height;
                    _minWidth = Window.MinWidth; _minHeight = Window.MinHeight; _maxWidth = Window.MaxWidth; _maxHeight = Window.MaxHeight; _captured = true;
                }
                _lastArea = WorkArea(Window);
                Factor = !_enabled ? 1 : IsMain
                    ? Math.Max(1, Math.Min(Requested, Math.Min(Window.ActualWidth / MinimumLogicalWidth, Math.Max(1,Window.ActualHeight-SystemParameters.WindowCaptionHeight-16) / MinimumLogicalHeight)))
                    : Math.Min(ComfortPresentation.Factor, Math.Max(1,Math.Min(_lastArea.Width/MinimumLogicalWidth,_lastArea.Height/MinimumLogicalHeight)));
                if (_enabled)
                {
                    _root.LayoutTransform = new ScaleTransform(Factor, Factor); TextOptions.SetTextFormattingMode(_root, TextFormattingMode.Ideal);
                    if (!IsMain)
                    {
                        double maxWidth = Math.Min(_maxWidth,_lastArea.Width*WorkAreaRatio), maxHeight = Math.Min(_maxHeight,_lastArea.Height*WorkAreaRatio);
                        Window.MinWidth = Window.MinHeight = 0; Window.MaxWidth = maxWidth; Window.MaxHeight = maxHeight;
                        Window.MinWidth = Math.Min(_minWidth*Factor,maxWidth); Window.MinHeight = Math.Min(_minHeight*Factor,maxHeight);
                        Window.Width = Math.Clamp(_normalWidth*Factor,Window.MinWidth,maxWidth); Window.Height = Math.Clamp(_normalHeight*Factor,Window.MinHeight,maxHeight);
                        _lastWidth = Window.Width; _lastHeight = Window.Height;
                        if (!double.IsNaN(Window.Left)) Window.Left = Math.Clamp(Window.Left,_lastArea.Left,_lastArea.Right-Window.Width);
                        if (!double.IsNaN(Window.Top)) Window.Top = Math.Clamp(Window.Top,_lastArea.Top,_lastArea.Bottom-Window.Height);
                    }
                }
                else
                {
                    _transform.Restore(); _format.Restore();
                    if (Scaled && !IsMain && _captured)
                    {
                        Window.MinWidth = Window.MinHeight = 0; Window.MaxWidth = _maxWidth; Window.MaxHeight = _maxHeight;
                        Window.MinWidth = _minWidth; Window.MinHeight = _minHeight; Window.Width = _normalWidth; Window.Height = _normalHeight;
                    }
                }
                Scaled = _enabled;
            }
            finally { _updating = false; }
        }
        public void Dispose() { Window.Loaded -= Loaded; Window.SizeChanged -= Resized; Window.DpiChanged -= DpiChanged; Window.LocationChanged -= Moved; }
    }
    private sealed class PropertySlot
    {
        private readonly DependencyObject _target; private readonly DependencyProperty _property; private readonly object _local; private readonly BindingBase? _binding;
        internal PropertySlot(DependencyObject target, DependencyProperty property) { _target=target;_property=property;_local=target.ReadLocalValue(property);_binding=BindingOperations.GetBindingBase(target,property); }
        internal void Restore() { if(_binding is not null)BindingOperations.SetBinding(_target,_property,_binding);else if(_local==DependencyProperty.UnsetValue)_target.ClearValue(_property);else _target.SetValue(_property,_local); }
    }

    public static readonly DependencyProperty ScalePopupProperty = DependencyProperty.RegisterAttached("ScalePopup",typeof(bool),typeof(ComfortPresentation),new PropertyMetadata(false,PopupRegistration));
    public static bool GetScalePopup(DependencyObject value)=>(bool)value.GetValue(ScalePopupProperty);
    public static void SetScalePopup(DependencyObject value,bool enabled)=>value.SetValue(ScalePopupProperty,enabled);
    private static void PopupRegistration(DependencyObject value,DependencyPropertyChangedEventArgs e)
    {
        if(value is not FrameworkElement view)return;
        if((bool)e.NewValue){view.Loaded+=PopupLoaded;view.Unloaded+=PopupUnloaded;if(view.IsLoaded)PopupLoaded(view,new RoutedEventArgs());}
        else {view.Loaded-=PopupLoaded;view.Unloaded-=PopupUnloaded;if(Popups.TryGetValue(view,out var state))state.Restore();}
    }
    internal static void RegisterPopup(FrameworkElement view,FrameworkElement anchor,bool matchBase=false)
    {
        var state=Popups.GetValue(view,v=>new PopupScale(v));state.Anchor=anchor;state.MatchBase=matchBase;
        SetScalePopup(view,true);if(view.IsLoaded)state.RequestApply();
    }
    private static void PopupLoaded(object sender,RoutedEventArgs e)
    {
        var view=(FrameworkElement)sender;var state=Popups.GetValue(view,v=>new PopupScale(v));
        if(!OpenPopups.Any(w=>w.TryGetTarget(out var p)&&ReferenceEquals(p,state)))OpenPopups.Add(new(state));state.RequestApply();
    }
    private static void PopupUnloaded(object sender,RoutedEventArgs e){if(Popups.TryGetValue((FrameworkElement)sender,out var state))state.Restore();}
    private sealed class PopupScale
    {
        internal readonly FrameworkElement View;internal FrameworkElement? Anchor;internal bool MatchBase;
        private readonly PropertySlot _transform,_maxWidth,_maxHeight;
        private bool _pending;
        internal PopupScale(FrameworkElement view){View=view;_transform=new(view,FrameworkElement.LayoutTransformProperty);_maxWidth=new(view,FrameworkElement.MaxWidthProperty);_maxHeight=new(view,FrameworkElement.MaxHeightProperty);}
        private FrameworkElement? FindAnchor()
        {
            if(Anchor is not null)return Anchor;
            if(View is ToolTip tip)return tip.PlacementTarget as FrameworkElement;
            if(View is ContextMenu menu)return menu.PlacementTarget as FrameworkElement;
            DependencyObject? parent=View;
            for(int i=0;i<12&&parent is not null;i++)
            {
                if(parent is Popup popup)return popup.PlacementTarget as FrameworkElement??popup.TemplatedParent as FrameworkElement;
                parent=LogicalTreeHelper.GetParent(parent)??(parent as FrameworkElement)?.TemplatedParent;
            }
            return null;
        }
        internal void Apply()
        {
            if(!View.IsLoaded)return;
            var anchor=FindAnchor();var window=anchor is null?null:Window.GetWindow(anchor);
            if(anchor is null||window is null||!anchor.IsLoaded||!window.IsLoaded||window is MiniPlayerWindow||(!_enabled&&!MatchBase)){Restore();return;}
            anchor.UpdateLayout();View.UpdateLayout();
            double scale=Math.Max(.1,VisualScale(anchor));
            var a=View.LayoutTransform.Transform(new Point());var b=View.LayoutTransform.Transform(new Point(1,0));
            double ownScale=Math.Max(.1,(b-a).Length),inheritedScale=Math.Max(.1,VisualScale(View)/ownScale);
            // Template Popups can already carry their parent's transform in WPF's popup root.
            // Supply only the missing ratio, instead of multiplying the same scale twice.
            double local=scale/inheritedScale;View.LayoutTransform=new ScaleTransform(local,local);
            if(_enabled){var work=WorkArea(window);View.MaxWidth=work.Width*WorkAreaRatio/scale;View.MaxHeight=work.Height*.75/scale;}
            else {_maxWidth.Restore();_maxHeight.Restore();}
        }
        internal void RequestApply()
        {
            Apply();if(_pending)return;_pending=true;
            View.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,new Action(()=>{_pending=false;if(View.IsLoaded)Apply();}));
        }
        private static double VisualScale(FrameworkElement element)
        {
            Visual root=element;while(VisualTreeHelper.GetParent(root)is Visual parent)root=parent;
            var transform=element.TransformToAncestor(root);var a=transform.Transform(new Point());var b=transform.Transform(new Point(1,0));return (b-a).Length;
        }
        internal void Restore(){_transform.Restore();_maxWidth.Restore();_maxHeight.Restore();}
    }
    [StructLayout(LayoutKind.Sequential)]private struct NativeRect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)]private struct MonitorInfo{public int Size;public NativeRect Monitor,Work;public uint Flags;}
    [DllImport("user32.dll")]private static extern IntPtr MonitorFromWindow(IntPtr hwnd,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")]private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")]private static extern short GetAsyncKeyState(int key);
}
