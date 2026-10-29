using System.Windows.Media;

namespace WXPlayer.App;

public partial class MainWindow
{
    private void InitializeComfortPresentation()
    {
        ComfortPresentation.Initialize(this,ComfortRoot,_settings,()=>_ready&&!_closing);
        ComfortPresentation.Changed+=ApplyComfortPresentation;
        Closed+=(_,_)=>ComfortPresentation.Changed-=ApplyComfortPresentation;
    }
    private void ApplyComfortPresentation()
    {
        if(_closing)return;
        if(_liveOverlayRoot is not null)_liveOverlayRoot.LayoutTransform=new ScaleTransform(LiveScale*ComfortPresentation.Factor,LiveScale*ComfortPresentation.Factor);
        if(_fullscreen)UpdateFullscreenViewport();
    }
}
