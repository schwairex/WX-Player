using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed partial class SeriesEpisodePanel
{
    private MainWindow.FilmVisualScope? _watchScope;
    private ListBox? _watchSeasons;
    private Button? _watchPreviousSeason,_watchNextSeason;
    private TextBlock? _watchSummary,_watchMessage;
    private Grid? _watchHeader;
    private Border? _watchSeasonHost;
    private object? _watchScrolledSource;
    private string? _watchScrolledId;
    private bool _watchUpdating;

    internal IDisposable AttachWatchingAppearance(FrameworkElement resources,Slider playbackPosition,Button previous,Button next)
    {
        if(_watchScope is not null)throw new InvalidOperationException("Series presentation already attached.");
        var scope=_watchScope=new MainWindow.FilmVisualScope();
        scope.Undo(()=>{_watchScope=null;_watchSeasons=null;_watchHeader=null;_watchSeasonHost=null;_watchSummary=null;_watchMessage=null;_watchScrolledSource=null;_watchScrolledId=null;});
        var dictionary=new ResourceDictionary{Source=new Uri("/WXPlayer;component/SeriesPanels.xaml",UriKind.Relative)};
        Resources.MergedDictionaries.Add(dictionary);scope.Undo(()=>Resources.MergedDictionaries.Remove(dictionary));
        Brush Brush(string key)=>(Brush)resources.FindResource(key);
        Style Style(string key)=>(Style)FindResource(key);
        scope.Set(this,BackgroundProperty,Brushes.Transparent);scope.Set(this,BorderThicknessProperty,new Thickness(0));scope.Set(this,PaddingProperty,new Thickness(0));scope.Set(this,MarginProperty,new Thickness(0,16,0,0));
        scope.Set(this,TextElement.FontFamilyProperty,resources.FindResource("LiveFont"));scope.Set(this,TextElement.ForegroundProperty,Brush("LiveInk"));
        scope.Set(this,TextOptions.TextFormattingModeProperty,TextFormattingMode.Ideal);scope.Set(this,TextOptions.TextRenderingModeProperty,TextRenderingMode.ClearType);
        var root=new Grid();root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.RowDefinitions.Add(new(){Height=GridLength.Auto});
        var original=Child;Child=root;scope.Undo(()=>Child=original);
        _watchHeader=new Grid{Margin=new Thickness(0,0,0,11.2)};_watchHeader.ColumnDefinitions.Add(new());_watchHeader.ColumnDefinitions.Add(new(){Width=GridLength.Auto});_watchHeader.ColumnDefinitions.Add(new(){Width=GridLength.Auto});_watchHeader.RowDefinitions.Add(new(){Height=GridLength.Auto});_watchHeader.RowDefinitions.Add(new(){Height=GridLength.Auto});root.Children.Add(_watchHeader);
        var heading=new StackPanel{VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,12.8,0)};_watchHeader.Children.Add(heading);
        heading.Children.Add(new TextBlock{Text="DİZİ BÖLÜMLERİ",FontSize=10.88,FontWeight=FontWeights.Bold,Foreground=Brush("LiveAccent")});
        scope.Move(_title,heading,1);scope.Set(_title,TextBlock.FontSizeProperty,16d);scope.Set(_title,TextBlock.ForegroundProperty,Brush("LiveInk"));scope.Set(_title,TextBlock.FontWeightProperty,FontWeights.ExtraBold);scope.Set(_title,MarginProperty,new Thickness(0,2,0,0));
        _watchSeasonHost=new CatalogPillBorder{Background=Brush("LiveRaised"),CornerRadius=new CornerRadius(999),Padding=new Thickness(3.2),Margin=new Thickness(0,0,12.8,0)};Grid.SetColumn(_watchSeasonHost,1);_watchHeader.Children.Add(_watchSeasonHost);
        var seasonGrid=new Grid();seasonGrid.ColumnDefinitions.Add(new(){Width=GridLength.Auto});seasonGrid.ColumnDefinitions.Add(new());seasonGrid.ColumnDefinitions.Add(new(){Width=GridLength.Auto});_watchSeasonHost.Child=seasonGrid;
        Button Arrow(string icon,string name,int delta)
        {
            var button=new Button{Style=Style("LiveButton"),Width=30.4,Height=30.4,Padding=new Thickness(0),VerticalAlignment=VerticalAlignment.Top,ToolTip=name,Content=new SvgIcon(icon){Width=15.2,Height=15.2}};
            AutomationProperties.SetName(button,name);button.Click+=(_,_)=>{int index=Seasons.SelectedIndex+delta;if(index>=0&&index<Seasons.Items.Count)Seasons.SelectedIndex=index;};return button;
        }
        _watchPreviousSeason=Arrow("chevron-left","Önceki sezon",-1);_watchNextSeason=Arrow("chevron-right","Sonraki sezon",1);Grid.SetColumn(_watchNextSeason,2);seasonGrid.Children.Add(_watchPreviousSeason);seasonGrid.Children.Add(_watchNextSeason);
        _watchSeasons=new ListBox{BorderThickness=new Thickness(0),Padding=new Thickness(0),Background=Brushes.Transparent,IsSynchronizedWithCurrentItem=false,MinHeight=30.4,MaxHeight=40};Grid.SetColumn(_watchSeasons,1);seasonGrid.Children.Add(_watchSeasons);AutomationProperties.SetName(_watchSeasons,"Dizinin sezonları");
        var panel=new FrameworkElementFactory(typeof(VirtualizingStackPanel));panel.SetValue(VirtualizingStackPanel.OrientationProperty,Orientation.Horizontal);_watchSeasons.ItemsPanel=new ItemsPanelTemplate(panel);
        _watchSeasons.Resources[typeof(ScrollBar)]=Style("LiveScrollBar");
        _watchSeasons.ItemContainerStyle=Style("SeriesSeasonPill");
        var label=new FrameworkElementFactory(typeof(TextBlock));label.SetBinding(TextBlock.TextProperty,new Binding("."){Converter=new SeasonPillText()});_watchSeasons.ItemTemplate=new DataTemplate{VisualTree=label};
        ScrollViewer.SetHorizontalScrollBarVisibility(_watchSeasons,ScrollBarVisibility.Auto);ScrollViewer.SetVerticalScrollBarVisibility(_watchSeasons,ScrollBarVisibility.Disabled);ScrollViewer.SetCanContentScroll(_watchSeasons,true);
        _watchSeasons.SetBinding(ItemsControl.ItemsSourceProperty,new Binding("ItemsSource"){Source=Seasons});_watchSeasons.SetBinding(Selector.SelectedItemProperty,new Binding("SelectedItem"){Source=Seasons,Mode=BindingMode.TwoWay});
        var pills=_watchSeasons;scope.Undo(()=>{BindingOperations.ClearBinding(pills,Selector.SelectedItemProperty);BindingOperations.ClearBinding(pills,ItemsControl.ItemsSourceProperty);});
        _watchSummary=new TextBlock{FontSize=12.48,Foreground=Brush("LiveMuted"),VerticalAlignment=VerticalAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis,MaxWidth=190};Grid.SetColumn(_watchSummary,2);_watchHeader.Children.Add(_watchSummary);
        _watchMessage=new TextBlock{FontSize=12.48,Foreground=Brush("LiveMuted"),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,8)};Grid.SetRow(_watchMessage,1);root.Children.Add(_watchMessage);
        scope.Move(Episodes,root,root.Children.Count);scope.Set(Episodes,Grid.RowProperty,2);
        scope.Resource(Episodes,typeof(ScrollBar),Style("LiveScrollBar"));
        scope.Set(Episodes,HeightProperty,112d);scope.Set(Episodes,BackgroundProperty,Brushes.Transparent);scope.Set(Episodes,PaddingProperty,new Thickness(0));scope.Set(Episodes,ItemsControl.ItemTemplateProperty,FindResource("SeriesEpisodeCard"));scope.Set(Episodes,ItemsControl.ItemContainerStyleProperty,Style("SeriesEpisodeRow"));
        var horizontal=new FrameworkElementFactory(typeof(VirtualizingStackPanel));horizontal.SetValue(VirtualizingStackPanel.OrientationProperty,Orientation.Horizontal);scope.Set(Episodes,ItemsControl.ItemsPanelProperty,new ItemsPanelTemplate(horizontal));
        scope.Set(Episodes,VirtualizingPanel.IsVirtualizingProperty,true);scope.Set(Episodes,VirtualizingPanel.VirtualizationModeProperty,VirtualizationMode.Recycling);scope.Set(Episodes,ScrollViewer.CanContentScrollProperty,true);scope.Set(Episodes,ScrollViewer.HorizontalScrollBarVisibilityProperty,ScrollBarVisibility.Auto);scope.Set(Episodes,ScrollViewer.VerticalScrollBarVisibilityProperty,ScrollBarVisibility.Disabled);
        scope.Bind(Episodes,LibraryPresentation.PlaybackPositionProperty,new Binding("Value"){Source=playbackPosition});
        scope.Move(_retry,root,root.Children.Count);scope.Set(_retry,Grid.RowProperty,3);scope.Set(_retry,StyleProperty,Style("LiveButton"));scope.Set(_retry,MarginProperty,new Thickness(0,5,0,0));
        // The original disabled-state source and all existing command handlers remain intact.
        scope.Bind(previous,IsEnabledProperty,new Binding("IsEnabled"){Source=_previous});scope.Bind(next,IsEnabledProperty,new Binding("IsEnabled"){Source=_next});
        foreach(var pair in new[]{(previous,_previous),(next,_next)}){scope.Set(pair.Item1,ToolTipProperty,pair.Item2.ToolTip);scope.Set(pair.Item1,AutomationProperties.NameProperty,pair.Item2.ToolTip.ToString()!);}
        var texts=DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty,typeof(TextBlock));texts.AddValueChanged(_state,WatchingChanged);scope.Undo(()=>texts.RemoveValueChanged(_state,WatchingChanged));
        var items=DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty,typeof(ItemsControl));items.AddValueChanged(Episodes,WatchingChanged);items.AddValueChanged(Seasons,WatchingChanged);scope.Undo(()=>{items.RemoveValueChanged(Episodes,WatchingChanged);items.RemoveValueChanged(Seasons,WatchingChanged);});
        Seasons.SelectionChanged+=WatchingSeasonChanged;SizeChanged+=WatchingSizeChanged;scope.Undo(()=>{Seasons.SelectionChanged-=WatchingSeasonChanged;SizeChanged-=WatchingSizeChanged;});
        RefreshWatchingAppearance();return scope;
    }
    private void WatchingChanged(object? sender,EventArgs e)=>RefreshWatchingAppearance();
    private void WatchingSeasonChanged(object sender,SelectionChangedEventArgs e){RefreshWatchingAppearance();if(_watchSeasons?.SelectedItem is{} item)_watchSeasons.ScrollIntoView(item);}
    private void WatchingSizeChanged(object sender,SizeChangedEventArgs e)
    {
        // Resize keeps the user's focused card; source/current changes still reveal playback.
        var anchor=Episodes.IsKeyboardFocusWithin?(Keyboard.FocusedElement as FrameworkElement)?.DataContext as ContentItem:null;
        RefreshWatchingAppearance();
        if(anchor is not null&&Episodes.Items.Contains(anchor))Episodes.ScrollIntoView(anchor);
    }
    private void RefreshWatchingAppearance()
    {
        if(_watchScope is null||_watchUpdating)return;_watchUpdating=true;
        try
        {
            int season=(Seasons.SelectedItem as SeasonChoice)?.Number??0;
            _watchSummary!.Text=SeriesDisplay.Summary(_state.Text,season,Episodes.Items.Count);
            _watchSummary.ToolTip=_watchSummary.Text;
            bool summary=_state.Text.StartsWith("Şimdi oynatılıyor:",StringComparison.Ordinal);
            _watchSummary.Visibility=summary?Visibility.Visible:Visibility.Collapsed;_watchMessage!.Text=summary?"":_state.Text;_watchMessage.Visibility=summary?Visibility.Collapsed:Visibility.Visible;
            bool many=Seasons.Items.Count>1;_watchPreviousSeason!.Visibility=_watchNextSeason!.Visibility=many?Visibility.Visible:Visibility.Collapsed;
            _watchPreviousSeason.IsEnabled=Seasons.SelectedIndex>0;_watchNextSeason.IsEnabled=Seasons.SelectedIndex>=0&&Seasons.SelectedIndex<Seasons.Items.Count-1;
            bool narrow=ActualWidth<700;Grid.SetRow(_watchSeasonHost!,narrow?1:0);Grid.SetColumn(_watchSeasonHost!,narrow?0:1);Grid.SetColumnSpan(_watchSeasonHost!,narrow?3:1);_watchSeasonHost!.HorizontalAlignment=HorizontalAlignment.Left;
            Grid.SetColumn(_watchSummary,narrow?1:2);Grid.SetColumnSpan(_watchSummary,narrow?2:1);
            _watchSeasons!.MaxWidth=Math.Max(75,Math.Min(narrow?500:360,ActualWidth*(narrow?.8:.48)-64));
            _watchScope.Set(Episodes,LibraryPresentation.PlayingItemProperty,_current!);
            if(!ReferenceEquals(_watchScrolledSource,Episodes.ItemsSource)||_watchScrolledId!=_current?.Id)
            {
                _watchScrolledSource=Episodes.ItemsSource;_watchScrolledId=_current?.Id;
                var playing=Episodes.Items.OfType<ContentItem>().FirstOrDefault(e=>e.Equals(_current));if(playing is not null)Episodes.ScrollIntoView(playing);
            }
        }
        finally{_watchUpdating=false;}
    }
    internal bool TryWatchingKey(KeyEventArgs e)
    {
        if(_watchScope is null||Keyboard.Modifiers!=ModifierKeys.None)return false;
        if(_watchSeasons!.IsKeyboardFocusWithin)
        {
            if(e.Key is Key.Left or Key.Right){int index=Seasons.SelectedIndex+(e.Key==Key.Right?1:-1);if(index>=0&&index<Seasons.Items.Count)Seasons.SelectedIndex=index;}
            else if(e.Key is not(Key.Up or Key.Down))return false;
        }
        else if(Episodes.IsKeyboardFocusWithin)
        {
            var element=Keyboard.FocusedElement as DependencyObject;var row=element is null?null:ItemsControl.ContainerFromElement(Episodes,element) as ListBoxItem;
            int index=row is null?Episodes.SelectedIndex:Episodes.ItemContainerGenerator.IndexFromContainer(row);
            if(e.Key is Key.Left or Key.Right)
            {
                if(Episodes.Items.Count>0){index=Math.Clamp(index+(e.Key==Key.Right?1:-1),0,Episodes.Items.Count-1);var item=Episodes.Items[index];Episodes.ScrollIntoView(item);Episodes.UpdateLayout();if(Episodes.ItemContainerGenerator.ContainerFromIndex(index) is ListBoxItem target)CardButton(target)?.Focus();}
            }
            else if(e.Key is Key.Enter or Key.Space)
            {
                if(Keyboard.FocusedElement is ButtonBase)return false;
                if(index>=0&&Episodes.ItemContainerGenerator.ContainerFromIndex(index) is ListBoxItem target)CardButton(target)?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            else if(e.Key is not(Key.Up or Key.Down))return false;
        }
        else return false;
        e.Handled=true;return true;
    }
    private static Button? CardButton(DependencyObject root)
    {
        if(root is Button button)return button;
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)if(CardButton(VisualTreeHelper.GetChild(root,i)) is{} child)return child;
        return null;
    }
    private sealed class SeasonPillText : IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)=>value is SeasonChoice s?s.Number==0?s.ToString():"S"+s.Number:"";
        public object ConvertBack(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)=>Binding.DoNothing;
    }
}
