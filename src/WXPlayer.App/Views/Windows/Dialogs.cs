using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using WXPlayer.Core;

namespace WXPlayer.App;

internal static class Dialogs
{
    private static StackPanel Form(Window owner,string title,int width,out Window window)
    {
        window=new Window{Style=(Style)Application.Current.FindResource("AppWindow"),Owner=owner,Title=title,Width=width,SizeToContent=SizeToContent.Height,MaxHeight=owner.ActualHeight,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false};
        var panel=new StackPanel{Margin=new Thickness(26)};window.Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        panel.Children.Add(new TextBlock{Text=title,FontSize=24,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,18)});return panel;
    }
    private static void Label(Panel p,string text)=>p.Children.Add(new TextBlock{Text=text,FontSize=12,Foreground=new SolidColorBrush(Color.FromRgb(160,177,197)),Margin=new Thickness(0,12,0,6)});
    private static TextBox Field(Panel panel,string label,string value=""){Label(panel,label);var t=new TextBox{Text=value};panel.Children.Add(t);return t;}
    private static Button Button(Panel panel,string text,RoutedEventHandler action,bool primary=false){var b=new Button{Content=text,Margin=new Thickness(0,16,0,0)};if(primary)b.Style=(Style)Application.Current.FindResource("Primary");b.Click+=action;panel.Children.Add(b);return b;}
    private static void Note(Panel panel,string text)=>panel.Children.Add(new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,Foreground=new SolidColorBrush(Color.FromRgb(146,163,183)),FontSize=11,LineHeight=18,Margin=new Thickness(0,12,0,0)});
    public static SourceConfig? Source(Window owner,SourceConfig? current=null){var window=new SourceWindow(owner,current);return window.ShowDialog()==true?window.Result:null;}
    public static ContentItem? Episode(Window owner,IReadOnlyList<ContentItem> episodes)
    {
        var p=Form(owner,"Bölümler",650,out var w);var list=new ListBox{ItemsSource=episodes,DisplayMemberPath="Name",Height=380};p.Children.Add(list);ContentItem? result=null;
        void Choose(){if(list.SelectedItem is ContentItem item){result=item;w.DialogResult=true;}}
        list.MouseDoubleClick+=(_,_)=>Choose();Button(p,"Seçili bölümü oynat",(_,_)=>Choose(),true);w.ShowDialog();return result;
    }
    public static (string Video,string Audio)? Capture(Window owner)
    {
        var p=Form(owner,"DirectShow aygıtını aç",500,out var w);Note(p,"Windows'taki kamera / yakalama aygıtının tam adını girin. Boş video adı varsayılan aygıtı seçer; 'none' bir girişi kapatır.");var video=Field(p,"Video aygıtı");var audio=Field(p,"Ses aygıtı","none");Button(p,"Aygıtı aç",(_,_)=>w.DialogResult=true,true);return w.ShowDialog()==true?(video.Text,audio.Text):null;
    }
}

