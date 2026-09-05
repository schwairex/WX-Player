using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace WXPlayer.App;
public sealed class MediaSlider : Slider
{
    public bool IsInteracting {get;private set;}
    public event EventHandler? InteractionCommitted;
    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e){if(!IsEnabled)return;Focus();IsInteracting=true;CaptureMouse();SetPoint(e.GetPosition(this).X);e.Handled=true;}
    protected override void OnPreviewMouseMove(MouseEventArgs e){if(IsInteracting&&IsMouseCaptured&&e.LeftButton==MouseButtonState.Pressed){SetPoint(e.GetPosition(this).X);e.Handled=true;}base.OnPreviewMouseMove(e);}
    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e){if(!IsInteracting)return;SetPoint(e.GetPosition(this).X);IsInteracting=false;ReleaseMouseCapture();InteractionCommitted?.Invoke(this,EventArgs.Empty);e.Handled=true;}
    protected override void OnLostMouseCapture(MouseEventArgs e){bool commit=IsInteracting;IsInteracting=false;if(commit)InteractionCommitted?.Invoke(this,EventArgs.Empty);base.OnLostMouseCapture(e);}
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.Key is Key.Left or Key.Right or Key.Home or Key.End or Key.PageUp or Key.PageDown)InteractionCommitted?.Invoke(this,EventArgs.Empty);}
    private void SetPoint(double x)=>Value=Minimum+Math.Clamp((x-10)/Math.Max(1,ActualWidth-20),0,1)*(Maximum-Minimum);
    internal void SmokeCommitAt(double x){SetPoint(x);InteractionCommitted?.Invoke(this,EventArgs.Empty);}
}
