using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace WXPlayer.App;

public partial class MainWindow
{
    private ComboBox? _fullscreenCategory;
    internal ListBox? FullscreenChannels {get;private set;}
    private UIElement CreateFullscreenLayout()
    {
        _floatingLayout=new StackPanel();
        var panel=new Grid{Margin=new Thickness(16,12,16,12)};panel.RowDefinitions.Add(new(){Height=GridLength.Auto});panel.RowDefinitions.Add(new(){Height=new GridLength(105)});panel.RowDefinitions.Add(new(){Height=GridLength.Auto});
        var head=new Grid{Margin=new Thickness(0,0,0,10)};head.ColumnDefinitions.Add(new(){Width=new GridLength(150)});head.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});head.ColumnDefinitions.Add(new(){Width=new GridLength(230)});panel.Children.Add(head);
        var kinds=new ComboBox{ItemsSource=new[]{"Tüm içerikler","Canlı TV","Filmler","Diziler","Favoriler"},SelectedIndex=_section switch{"live" or "epg"=>1,"movie"=>2,"series"=>3,"favorites"=>4,_=>0},Margin=new Thickness(0,0,10,0)};
        kinds.SelectionChanged+=async(_,_)=>{_section=kinds.SelectedIndex switch{1=>"live",2=>"movie",3=>"series",4=>"favorites",_=>"home"};_offset=0;SetNav();await SafeAsync(RefreshViewAsync);};head.Children.Add(kinds);
        _fullscreenCategory=new ComboBox{Margin=new Thickness(0,0,10,0),ToolTip="Kategori seçin"};_fullscreenCategory.SetBinding(ItemsControl.ItemsSourceProperty,new Binding("ItemsSource"){Source=CategoryPicker});_fullscreenCategory.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedItemProperty,new Binding("SelectedItem"){Source=CategoryPicker,Mode=BindingMode.TwoWay});Grid.SetColumn(_fullscreenCategory,1);head.Children.Add(_fullscreenCategory);
        var search=new TextBox{ToolTip="İçerik ara",Padding=new Thickness(10,8,10,8)};search.SetBinding(TextBox.TextProperty,new Binding("Text"){Source=SearchBox,Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});var searchBox=new Grid();searchBox.Children.Add(search);var hint=PremiumWindow.Text("İçerik ara…",12,"#90A1B3");hint.Margin=new Thickness(12,0,0,0);hint.VerticalAlignment=VerticalAlignment.Center;hint.IsHitTestVisible=false;hint.Visibility=string.IsNullOrEmpty(search.Text)?Visibility.Visible:Visibility.Collapsed;search.TextChanged+=(_,_)=>hint.Visibility=string.IsNullOrEmpty(search.Text)?Visibility.Visible:Visibility.Collapsed;searchBox.Children.Add(hint);Grid.SetColumn(searchBox,2);head.Children.Add(searchBox);
        FullscreenChannels=new ListBox{ItemTemplate=ChannelList.ItemTemplate,BorderThickness=new Thickness(0)};
        var items=new FrameworkElementFactory(typeof(VirtualizingStackPanel));items.SetValue(VirtualizingStackPanel.OrientationProperty,Orientation.Horizontal);FullscreenChannels.ItemsPanel=new ItemsPanelTemplate(items);
        var style=new Style(typeof(ListBoxItem),(Style)FindResource(typeof(ListBoxItem)));style.Setters.Add(new Setter(WidthProperty,252d));style.Setters.Add(new Setter(MarginProperty,new Thickness(0,0,8,4)));FullscreenChannels.ItemContainerStyle=style;
        ScrollViewer.SetHorizontalScrollBarVisibility(FullscreenChannels,ScrollBarVisibility.Auto);ScrollViewer.SetVerticalScrollBarVisibility(FullscreenChannels,ScrollBarVisibility.Disabled);
        FullscreenChannels.SetBinding(ItemsControl.ItemsSourceProperty,new Binding("ItemsSource"){Source=ChannelList});FullscreenChannels.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedItemProperty,new Binding("SelectedItem"){Source=ChannelList,Mode=BindingMode.TwoWay});Grid.SetRow(FullscreenChannels,1);panel.Children.Add(FullscreenChannels);
        var footer=new DockPanel{Margin=new Thickness(0,6,0,0)};Grid.SetRow(footer,2);panel.Children.Add(footer);
        var pages=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(pages,Dock.Right);footer.Children.Add(pages);
        var previous=PremiumWindow.Action("‹",()=>PrevPage_Click(this,new RoutedEventArgs()));var next=PremiumWindow.Action("›",()=>NextPage_Click(this,new RoutedEventArgs()));previous.MinHeight=next.MinHeight=28;previous.Padding=next.Padding=new Thickness(12,3,12,3);pages.Children.Add(previous);pages.Children.Add(next);
        footer.Children.Add(PremiumWindow.Text("KATEGORİLER  /  İçeriği seçerek izlemeye geçin",10,"#91A99C"));
        _floatingLayout.Children.Add(new Border{Background=PremiumWindow.Brush("#F4131C24"),BorderBrush=PremiumWindow.Brush("#3B4C50"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(16),Margin=new Thickness(0,0,10,10),Child=panel});
        _floatingLayout.Children.Add(ControlsBorder);return _floatingLayout;
    }
}
