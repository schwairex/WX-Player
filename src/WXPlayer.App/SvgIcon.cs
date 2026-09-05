using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

namespace WXPlayer.App;

/// <summary>Renders the bundled SVG paths directly as WPF vector geometry; never rasterizes icons.</summary>
public sealed class SvgIcon : FrameworkElement
{
    public static readonly DependencyProperty IconProperty=DependencyProperty.Register(nameof(Icon),typeof(string),typeof(SvgIcon),new FrameworkPropertyMetadata("play",FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ForegroundProperty=TextElement.ForegroundProperty.AddOwner(typeof(SvgIcon),new FrameworkPropertyMetadata(Brushes.White,FrameworkPropertyMetadataOptions.Inherits|FrameworkPropertyMetadataOptions.AffectsRender));
    private static readonly Dictionary<string,List<(Geometry Shape,bool Fill)>> Cache=new();
    public string Icon {get=>(string)GetValue(IconProperty);set=>SetValue(IconProperty,value);}
    public Brush Foreground {get=>(Brush)GetValue(ForegroundProperty);set=>SetValue(ForegroundProperty,value);}
    public SvgIcon(){Width=20;Height=20;IsHitTestVisible=false;}
    public SvgIcon(string name):this(){Icon=name;}
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);if(string.IsNullOrWhiteSpace(Icon))return;
        if(!Cache.TryGetValue(Icon,out var paths))
        {
            if(!System.Text.RegularExpressions.Regex.IsMatch(Icon,"^[a-z-]+$"))return;
            var resource=Application.GetResourceStream(new Uri($"pack://application:,,,/WXPlayer;component/Assets/Icons/{Icon}.svg"));
            if(resource is null)return;
            using var stream=resource.Stream;using var reader=XmlReader.Create(stream,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null});
            var document=XDocument.Load(reader);paths=[];
            foreach(var p in document.Descendants().Where(e=>e.Name.LocalName=="path"))
            {var geometry=Geometry.Parse((string?)p.Attribute("d")??"");geometry.Freeze();paths.Add((geometry,(string?)p.Attribute("fill")=="currentColor"));}
            Cache[Icon]=paths;
        }
        double scale=Math.Min(ActualWidth,ActualHeight)/24;
        dc.PushTransform(new TranslateTransform((ActualWidth-24*scale)/2,(ActualHeight-24*scale)/2));dc.PushTransform(new ScaleTransform(scale,scale));
        var pen=new Pen(Foreground,1.65){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round};
        foreach(var (shape,fill) in paths)dc.DrawGeometry(fill?Foreground:null,pen,shape);
        dc.Pop();dc.Pop();
    }
}

public sealed class IconLabel : StackPanel
{
    private readonly SvgIcon _icon=new();private readonly TextBlock _label=new(){Margin=new Thickness(12,0,0,0),VerticalAlignment=VerticalAlignment.Center};
    public string Icon {get=>_icon.Icon;set=>_icon.Icon=value;}
    public string Label {get=>_label.Text;set=>_label.Text=value;}
    public bool Compact {get=>_label.Visibility==Visibility.Collapsed;set=>_label.Visibility=value?Visibility.Collapsed:Visibility.Visible;}
    public IconLabel(){Orientation=Orientation.Horizontal;VerticalAlignment=VerticalAlignment.Center;Children.Add(_icon);Children.Add(_label);}
}

public sealed class FavoriteIconConverter : IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>Equals(value,true)?"star-filled":"star";
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
}
