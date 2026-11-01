using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WXPlayer.App;

// Only observes the existing ChannelLogo image. No separate load, discovery or cache.
public sealed class LibraryPosterPresentation : Grid
{
    private readonly ChannelLogo _logo=new(){Initials="",ImagePadding=new Thickness(0),ImageStretch=Stretch.Uniform};
    private readonly Grid _fallback=new(){IsHitTestVisible=false};
    private readonly Rectangle _outline=new(){StrokeThickness=1,StrokeDashArray=new DoubleCollection{3,3}};
    private readonly SvgIcon _icon=new("catalog-film");
    private readonly Image _image;
    private readonly DependencyPropertyDescriptor _source;
    private bool _observing;
    public LibraryPosterPresentation()
    {
        _logo.SetBinding(ChannelLogo.UrlProperty,new Binding("Logo"));Children.Add(_logo);
        _fallback.Children.Add(_outline);_fallback.Children.Add(_icon);Children.Add(_fallback);
        _image=_logo.Children.OfType<Image>().Single();_source=DependencyPropertyDescriptor.FromProperty(Image.SourceProperty,typeof(Image));
        Loaded+=LoadedPoster;Unloaded+=UnloadedPoster;SizeChanged+=(_,_)=>Geometry();
    }
    private void LoadedPoster(object sender,RoutedEventArgs e)
    {
        _fallback.Background=(Brush)FindResource("LiveRaised");_outline.Stroke=(Brush)FindResource("LibraryPosterLine");_icon.Foreground=(Brush)FindResource("LibraryPosterInk");
        if(!_observing){_source.AddValueChanged(_image,Refresh);_observing=true;}Geometry();Refresh(null,EventArgs.Empty);
    }
    private void Geometry()
    {
        bool large=ActualWidth>=80;double radius=large?12.8:8.8;
        Clip=new RectangleGeometry(new Rect(0,0,ActualWidth,ActualHeight),radius,radius);_outline.RadiusX=_outline.RadiusY=radius;
        _icon.Width=_icon.Height=large?28.8:17.6;_icon.HorizontalAlignment=HorizontalAlignment.Center;_icon.VerticalAlignment=VerticalAlignment.Center;
        _logo.DecodeWidth=large?240:96;
    }
    private void Refresh(object? sender,EventArgs e)=>_fallback.Visibility=_logo.HasImage?Visibility.Collapsed:Visibility.Visible;
    private void UnloadedPoster(object sender,RoutedEventArgs e){if(_observing){_source.RemoveValueChanged(_image,Refresh);_observing=false;}}
}
