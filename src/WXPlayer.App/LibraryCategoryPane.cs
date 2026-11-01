using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace WXPlayer.App;

// A visual proxy for the existing selector. Same strings, same selected item, same handler.
internal sealed class LibraryCategoryPane : Border
{
    internal readonly ListBox Picker=new(){BorderThickness=new Thickness(0),Background=System.Windows.Media.Brushes.Transparent,Margin=new Thickness(9.6,0,9.6,12.8),IsSynchronizedWithCurrentItem=false};
    internal LibraryCategoryPane(ComboBox original,string name="Film kategorileri")
    {
        BorderThickness=new Thickness(0,0,1,0);
        SetResourceReference(BackgroundProperty,"LibraryCategoryBackground");SetResourceReference(BorderBrushProperty,"LiveLine");
        var grid=new Grid();grid.RowDefinitions.Add(new(){Height=GridLength.Auto});grid.RowDefinitions.Add(new());Child=grid;
        var heading=new TextBlock{Text="KATEGORİLER",FontSize=10.88,FontWeight=FontWeights.ExtraBold,Margin=new Thickness(19.2,19.2,19.2,8)};heading.SetResourceReference(TextBlock.ForegroundProperty,"LiveMuted");grid.Children.Add(heading);
        Picker.SetResourceReference(ItemsControl.ItemTemplateProperty,"LibraryCategoryTemplate");Picker.SetResourceReference(ItemsControl.ItemContainerStyleProperty,"LibraryCategoryRow");
        Picker.SetBinding(ItemsControl.ItemsSourceProperty,new Binding("ItemsSource"){Source=original});
        Picker.SetBinding(Selector.SelectedItemProperty,new Binding("SelectedItem"){Source=original,Mode=BindingMode.TwoWay});
        Picker.Resources[typeof(ScrollBar)]=original.FindResource("LiveScrollBar");
        VirtualizingPanel.SetIsVirtualizing(Picker,true);VirtualizingPanel.SetVirtualizationMode(Picker,VirtualizationMode.Recycling);
        ScrollViewer.SetCanContentScroll(Picker,true);ScrollViewer.SetHorizontalScrollBarVisibility(Picker,ScrollBarVisibility.Disabled);
        AutomationProperties.SetName(Picker,name);Grid.SetRow(Picker,1);grid.Children.Add(Picker);
    }
    internal void Disconnect()
    {
        // Detach the two-way selection before clearing ItemsSource, which resets selection.
        BindingOperations.ClearBinding(Picker,Selector.SelectedItemProperty);
        BindingOperations.ClearBinding(Picker,ItemsControl.ItemsSourceProperty);
    }
}
