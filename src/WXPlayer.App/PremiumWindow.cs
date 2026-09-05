using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace WXPlayer.App;

internal class PremiumWindow : Window
{
    internal readonly Grid Body=new();
    internal readonly StackPanel Footer=new(){Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
    internal PremiumWindow(Window owner,string title,string subtitle,string icon,int width=780,int height=650)
    {
        Owner=owner;Title=title+" · WX Player";Width=width;Height=Math.Min(height,Math.Max(480,owner.ActualHeight-30));MinWidth=Math.Min(width,540);MinHeight=420;
        Style=(Style)Application.Current.FindResource("AppWindow");WindowStartupLocation=WindowStartupLocation.CenterOwner;ShowInTaskbar=false;
        SourceInitialized+=(_,_)=>{int dark=1;DwmSetWindowAttribute(new WindowInteropHelper(this).Handle,20,ref dark,4);};
        var root=new Grid{Margin=new Thickness(28)};root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});root.RowDefinitions.Add(new(){Height=GridLength.Auto});Content=new Border{Background=(Brush)FindResource("Bg"),Child=root};
        var head=new DockPanel{Margin=new Thickness(0,0,0,24)};
        var badge=new Border{Background=Brush("#253322"),CornerRadius=new(14),Padding=new(14),Margin=new(0,0,16,0),Child=new SvgIcon(icon){Width=26,Height=26,Foreground=Brush("#C1EC8B")}};DockPanel.SetDock(badge,Dock.Left);head.Children.Add(badge);
        var labels=new StackPanel{VerticalAlignment=VerticalAlignment.Center};labels.Children.Add(new TextBlock{Text=title,FontSize=26,FontWeight=FontWeights.SemiBold});labels.Children.Add(new TextBlock{Text=subtitle,Foreground=Brush("#98A3B6"),FontSize=12,Margin=new(0,6,0,0),TextWrapping=TextWrapping.Wrap});head.Children.Add(labels);root.Children.Add(head);
        Grid.SetRow(Body,1);root.Children.Add(Body);Footer.Margin=new(0,20,0,0);Grid.SetRow(Footer,2);root.Children.Add(Footer);
        PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape){Close();e.Handled=true;}};
    }
    [DllImport("dwmapi.dll")]private static extern int DwmSetWindowAttribute(IntPtr hwnd,int attr,ref int value,int size);
    internal static SolidColorBrush Brush(string hex)=>(SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
    internal static TextBlock Text(string text,int size=13,string color="#E9EEF5")=>new(){Text=text,FontSize=size,Foreground=Brush(color),TextWrapping=TextWrapping.Wrap};
    internal static StackPanel Card(Panel parent,string title,string? subtitle=null)
    {
        var content=new StackPanel();content.Children.Add(new TextBlock{Text=title,FontSize=15,FontWeight=FontWeights.SemiBold,Margin=new(0,0,0,8)});
        if(subtitle is not null)content.Children.Add(new TextBlock{Text=subtitle,FontSize=12,Foreground=Brush("#98A3B6"),TextWrapping=TextWrapping.Wrap,LineHeight=19,Margin=new(0,0,0,14)});
        parent.Children.Add(new Border{Background=Brush("#151C26"),BorderBrush=Brush("#283240"),BorderThickness=new(1),CornerRadius=new(12),Padding=new(20),Margin=new(0,0,0,14),Child=content});return content;
    }
    internal static Button Action(string label,Action action,bool primary=false)
    {
        var button=new Button{Content=label,Margin=new(8,0,0,0),MinHeight=38};if(primary)button.Style=(Style)Application.Current.FindResource("Primary");button.Click+=(_,_)=>action();return button;
    }
    internal static void Row(Panel parent,string label,UIElement value)
    {
        var grid=new Grid{Margin=new(0,7,0,7)};grid.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});grid.ColumnDefinitions.Add(new(){Width=new GridLength(240)});var text=Text(label,12,"#A8B5C7");text.VerticalAlignment=VerticalAlignment.Center;grid.Children.Add(text);Grid.SetColumn(value,1);grid.Children.Add(value);parent.Children.Add(grid);
    }
    internal static bool Confirm(Window owner,string title,string description)
    {
        var w=new PremiumWindow(owner,title,description,"library",560,300){MinHeight=270,Height=300,ResizeMode=ResizeMode.NoResize};bool yes=false;
        w.Body.Children.Add(Text("Bu işlem geri alınamaz. Kayıt dosyalarınız diskte kalır.",13,"#A8B5C7"));w.Footer.Children.Add(Action("Vazgeç",w.Close));w.Footer.Children.Add(Action("Temizle",()=>{yes=true;w.Close();},true));w.ShowDialog();return yes;
    }
}

