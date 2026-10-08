using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace WXPlayer.App;

public partial class MainWindow
{
    private ComboBox? _fullscreenCategory;
    internal ListBox? FullscreenChannels {get;private set;}
    internal Border? FullscreenBrowser {get;private set;}
    private UIElement CreateFullscreenLayout()
    {
        _floatingLayout=new StackPanel{HorizontalAlignment=HorizontalAlignment.Stretch};
        var panel=new Grid{Margin=new Thickness(20,16,20,14)};
        foreach(var height in new[]{GridLength.Auto,GridLength.Auto,new GridLength(96),GridLength.Auto})panel.RowDefinitions.Add(new(){Height=height});
        var title=new DockPanel{Margin=new Thickness(0,0,0,14)};
        var source=PremiumWindow.Text(SelectedSource?.Name??"Kütüphaneniz",10,"TextSecondaryBrush");source.MaxWidth=260;source.TextTrimming=TextTrimming.CharacterEllipsis;source.VerticalAlignment=VerticalAlignment.Center;DockPanel.SetDock(source,Dock.Right);title.Children.Add(source);
        var headingGroup=new StackPanel();var eyebrow=PremiumWindow.Text("KÜTÜPHANENİZ",9,"AccentPrimaryBrush");eyebrow.FontWeight=FontWeights.SemiBold;headingGroup.Children.Add(eyebrow);var heading=PremiumWindow.Text("İzlemeye devam edin",17);heading.FontWeight=FontWeights.SemiBold;heading.Margin=new Thickness(0,3,0,0);headingGroup.Children.Add(heading);title.Children.Add(headingGroup);panel.Children.Add(title);
        var filters=new Grid{Margin=new Thickness(0,0,0,13)};filters.ColumnDefinitions.Add(new(){Width=new GridLength(164)});filters.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});filters.ColumnDefinitions.Add(new(){Width=new GridLength(240)});Grid.SetRow(filters,1);panel.Children.Add(filters);
        void Field(string label,Control control,int column)
        {var stack=new StackPanel{Margin=new Thickness(0,0,column<2?12:0,0)};var caption=PremiumWindow.Text(label,9,"TextMutedBrush");caption.FontWeight=FontWeights.SemiBold;caption.Margin=new Thickness(2,0,0,6);stack.Children.Add(caption);control.MinHeight=38;stack.Children.Add(control);Grid.SetColumn(stack,column);filters.Children.Add(stack);}
        var kinds=new ComboBox{ItemsSource=new[]{"Tüm içerikler","Canlı TV","Filmler","Diziler","Favoriler"},SelectedIndex=_section switch{"live" or "epg"=>1,"movie"=>2,"series"=>3,"favorites"=>4,_=>0}};
        kinds.SelectionChanged+=async(_,_)=>{_section=kinds.SelectedIndex switch{1=>"live",2=>"movie",3=>"series",4=>"favorites",_=>"all"};_offset=0;_suppress=true;CategoryPicker.SelectedIndex=0;_suppress=false;SetNav();await SafeAsync(RefreshViewAsync);};Field("İÇERİK TÜRÜ",kinds,0);
        _fullscreenCategory=new ComboBox{ToolTip="Kategori seçin"};_fullscreenCategory.SetBinding(ItemsControl.ItemsSourceProperty,new Binding("ItemsSource"){Source=CategoryPicker});_fullscreenCategory.SetBinding(Selector.SelectedItemProperty,new Binding("SelectedItem"){Source=CategoryPicker,Mode=BindingMode.TwoWay});Field("KATEGORİ",_fullscreenCategory,1);
        var search=new TextBox{ToolTip="İçerik adıyla ara · Ctrl+K",Padding=new Thickness(12,6,12,6)};search.SetBinding(TextBox.TextProperty,new Binding("Text"){Source=SearchBox,Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});System.Windows.Automation.AutomationProperties.SetName(search,"Tam ekranda içerik ara");Field("İÇERİK ARA",search,2);
        FullscreenChannels=new ListBox{ItemTemplate=(DataTemplate)FindResource("FullscreenContentTemplate"),BorderThickness=new Thickness(0)};
        FullscreenChannels.MouseDoubleClick+=Channel_DoubleClick;
        var items=new FrameworkElementFactory(typeof(VirtualizingStackPanel));items.SetValue(VirtualizingStackPanel.OrientationProperty,Orientation.Horizontal);FullscreenChannels.ItemsPanel=new ItemsPanelTemplate(items);
        var style=new Style(typeof(ListBoxItem),(Style)FindResource(typeof(ListBoxItem)));style.Setters.Add(new Setter(WidthProperty,268d));style.Setters.Add(new Setter(HeightProperty,82d));style.Setters.Add(new Setter(MarginProperty,new Thickness(0,0,10,2)));FullscreenChannels.ItemContainerStyle=style;
        ScrollViewer.SetHorizontalScrollBarVisibility(FullscreenChannels,ScrollBarVisibility.Auto);ScrollViewer.SetVerticalScrollBarVisibility(FullscreenChannels,ScrollBarVisibility.Disabled);
        FullscreenChannels.SetBinding(ItemsControl.ItemsSourceProperty,new Binding("ItemsSource"){Source=ChannelList});FullscreenChannels.SetBinding(Selector.SelectedItemProperty,new Binding("SelectedItem"){Source=ChannelList,Mode=BindingMode.TwoWay});Grid.SetRow(FullscreenChannels,2);panel.Children.Add(FullscreenChannels);
        var empty=PremiumWindow.Text("Bu filtrede içerik bulunamadı. Aramayı veya kategoriyi değiştirin.",12,"TextSecondaryBrush");empty.VerticalAlignment=VerticalAlignment.Center;Grid.SetRow(empty,2);panel.Children.Add(empty);
        void EmptyState()=>empty.Visibility=FullscreenChannels.Items.Count==0?Visibility.Visible:Visibility.Collapsed;
        ((System.Collections.Specialized.INotifyCollectionChanged)FullscreenChannels.Items).CollectionChanged+=(_,_)=>EmptyState();EmptyState();
        var footer=new DockPanel{Margin=new Thickness(0,9,0,0)};Grid.SetRow(footer,3);panel.Children.Add(footer);
        var pages=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(pages,Dock.Right);footer.Children.Add(pages);
        var previous=PremiumWindow.Action("‹ Önceki",()=>PrevPage_Click(this,new RoutedEventArgs()));var next=PremiumWindow.Action("Sonraki ›",()=>NextPage_Click(this,new RoutedEventArgs()));previous.SetBinding(IsEnabledProperty,new Binding("IsEnabled"){Source=PrevPage});next.SetBinding(IsEnabledProperty,new Binding("IsEnabled"){Source=NextPage});
        foreach(var button in new[]{previous,next}){button.MinHeight=28;button.FontSize=10;button.Padding=new Thickness(12,4,12,4);button.Margin=new Thickness(6,0,0,0);pages.Children.Add(button);}
        var count=PremiumWindow.Text("",10,"TextMutedBrush");count.SetBinding(TextBlock.TextProperty,new Binding("Text"){Source=PageLabel});count.VerticalAlignment=VerticalAlignment.Center;footer.Children.Add(count);
        FullscreenBrowser=new Border{Background=PremiumWindow.Brush("#F5192029"),BorderBrush=PremiumWindow.Brush("BorderSubtleBrush"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(16),Margin=new Thickness(0,0,0,10),HorizontalAlignment=HorizontalAlignment.Stretch,Child=panel};
        _floatingLayout.Children.Add(FullscreenBrowser);_floatingLayout.Children.Add(ControlsBorder);return _floatingLayout;
    }
}
