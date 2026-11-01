using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WXPlayer.Core;

namespace WXPlayer.App;

public partial class MainWindow
{
    private bool FilmWatchingVisible=>EmbeddedPlayerVisible&&_section=="movie"&&_catalogPlaying&&_current?.Kind==ContentKind.Movie;
    private FilmVisualScope? _filmScope;
    private LibraryCategoryPane? _filmCategories;
    private ColumnDefinition? _filmCategoryColumn;
    private Border? _filmInfo;
    private ContentItem? _filmFavoriteItem;
    private Button? _filmCategoryToggle,_filmFavorite,_filmNext;
    private TextBlock? _filmFavoriteLabel,_filmNextLabel,_filmElapsed,_filmTotal;
    private SvgIcon? _filmFavoriteIcon;
    private StackPanel? _filmConnecting;
    private FrameworkElement? _filmPager;
    private bool _filmUpdating,_filmDrawer,_filmNarrow;
    private bool? _filmStatusHidden;

    private void UpdateFilmAppearance()
    {
        if(_filmUpdating)return;
        if(!LibraryWatchingVisible){DetachFilmAppearance();return;}
        if(_filmScope is not null&&_libraryScopeSeries!=SeriesWatchingVisible)DetachFilmAppearance();
        if(!_liveShellAttached||_liveOverlayRoot is null)return;
        _filmUpdating=true;
        try
        {
            if(_filmScope is null)AttachFilmAppearance();
            var scope=_filmScope!;
            _filmNarrow=ActualWidth/ComfortPresentation.Factor<1100;
            scope.Set(_filmCategoryColumn!,ColumnDefinition.WidthProperty,new GridLength(_filmNarrow?0:224));
            scope.Set(ListColumn,ColumnDefinition.WidthProperty,new GridLength(_filmNarrow?304:336));
            _filmCategories!.Width=_filmNarrow?240:224;Grid.SetColumnSpan(_filmCategories,_filmNarrow?2:1);
            _filmCategories.Visibility=!_filmNarrow||_filmDrawer?Visibility.Visible:Visibility.Collapsed;
            _filmCategoryToggle!.Visibility=_filmNarrow?Visibility.Visible:Visibility.Collapsed;
            scope.Set(_filmPager!,VisibilityProperty,_total>150?Visibility.Visible:Visibility.Collapsed);
            scope.Set(GuideRow,RowDefinition.HeightProperty,GridLength.Auto);
            scope.Set(GuidePanel,VisibilityProperty,Visibility.Collapsed);scope.Set(GuideSplitter,VisibilityProperty,Visibility.Collapsed);
            LibraryPresentation.SetPlayingItem(ChannelList,_current);
            scope.Set(ChannelList,LibraryPresentation.CategoryDetailsVisibleProperty,CategoryPicker.SelectedIndex==0);
            if(FilmWatchingVisible)
            {
            // Keep the real last displayed instance when filtering hides the playing film.
            // The existing favorite handler updates that instance, not other query copies.
            var shown=ChannelList.Items.OfType<ContentItem>().FirstOrDefault(x=>x.Equals(_current));
            if(shown is not null)_filmFavoriteItem=shown;
            else if(_filmFavoriteItem?.Equals(_current)!=true)_filmFavoriteItem=_current;
            shown=_filmFavoriteItem!;
            _filmInfo!.DataContext=_current;_filmFavorite!.Tag=shown;
            int next=ChannelList.SelectedIndex+1;
            var nextItem=next>=0&&next<ChannelList.Items.Count?ChannelList.Items[next] as ContentItem:null;
            _filmNext!.Visibility=!_filmNarrow&&nextItem is not null&&!nextItem.Equals(_current)?Visibility.Visible:Visibility.Collapsed;
            _filmNextLabel!.Text=nextItem is null?"":"Sıradaki · "+LibraryDisplay.Title(nextItem.Name);
            _filmFavorite!.Visibility=_filmNarrow?Visibility.Collapsed:Visibility.Visible;
            }
            else UpdateSeriesWatchingPresentation();
            bool connecting=PlaybackBadge.Text=="BAĞLANIYOR";
            _filmConnecting!.Visibility=connecting?Visibility.Visible:Visibility.Collapsed;
            if(_engine.Player.State==LibVLCSharp.Shared.VLCState.Paused&&_engine.Player.Length>0)
            {
                _filmElapsed!.Text=TimeSpan.FromMilliseconds(Math.Max(0,_engine.Player.Time)).ToString(@"hh\:mm\:ss");
                _filmTotal!.Text=TimeSpan.FromMilliseconds(_engine.Player.Length).ToString(@"hh\:mm\:ss");
            }
            else{_filmElapsed!.Text=LibraryDisplay.Elapsed(PlaybackBadge.Text);_filmTotal!.Text=LibraryDisplay.Total(PlaybackBadge.Text);}
            bool redundant=CancelButton.Visibility!=Visibility.Visible&&StatusText.Text.StartsWith((_current?.Name??"")+" · Tampon ",StringComparison.Ordinal);
            if(_filmStatusHidden!=redundant){scope.Set(StatusText,VisibilityProperty,redundant?Visibility.Collapsed:Visibility.Visible);_filmStatusHidden=redundant;}
        }
        finally{_filmUpdating=false;}
    }
    private void AttachFilmAppearance()
    {
        var s=_filmScope=new FilmVisualScope();_libraryScopeSeries=SeriesWatchingVisible;
        _filmCategoryColumn=new ColumnDefinition();ContentGrid.ColumnDefinitions.Insert(0,_filmCategoryColumn);s.Undo(()=>ContentGrid.ColumnDefinitions.Remove(_filmCategoryColumn));
        s.Set(LibraryPanel,Grid.ColumnProperty,1);s.Set(ViewingPanel,Grid.ColumnProperty,3);s.Set(TopBar,Grid.ColumnProperty,3);
        _filmCategories=new LibraryCategoryPane(CategoryPicker,SeriesWatchingVisible?"Dizi kategorileri":"Film kategorileri"){HorizontalAlignment=HorizontalAlignment.Left};Grid.SetRowSpan(_filmCategories,2);Panel.SetZIndex(_filmCategories,12);s.Add(ContentGrid,_filmCategories);
        s.Set(CategoryPicker,VisibilityProperty,Visibility.Collapsed);s.Set(ChannelList,ItemsControl.ItemTemplateProperty,FindResource("LibraryMovieTemplate"));s.Set(ChannelList,ItemsControl.ItemContainerStyleProperty,FindResource("LibraryMovieRow"));
        s.Set(ChannelList,MarginProperty,new Thickness(9.6,1.6,8,9.6));
        var listHead=new Grid{Margin=new Thickness(1.6,0,1.6,12.8)};listHead.ColumnDefinitions.Add(new());listHead.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        var labels=new Grid{VerticalAlignment=VerticalAlignment.Center};labels.ColumnDefinitions.Add(new());labels.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        var category=FilmText("",17.6,"LiveInk");category.FontWeight=FontWeights.ExtraBold;
        category.SetBinding(TextBlock.TextProperty,new Binding("SelectedItem"){Source=CategoryPicker,Converter=(IValueConverter)FindResource("LibraryText"),ConverterParameter="Category"});labels.Children.Add(category);
        var count=FilmText("",12.48,"LiveMuted");count.Margin=new Thickness(9.6,0,0,0);count.SetBinding(TextBlock.TextProperty,new Binding("Text"){Source=ResultsCount});Grid.SetColumn(count,1);labels.Children.Add(count);listHead.Children.Add(labels);
        _filmCategoryToggle=new Button{Content="Kategoriler",Style=LiveStyle("LibraryInfoButton"),FontSize=12.48,Padding=new Thickness(12.8,0,12.8,0),Height=30.4};
        AutomationProperties.SetName(_filmCategoryToggle,"Kategorileri aç");_filmCategoryToggle.Click+=FilmCategoryToggle;Grid.SetColumn(_filmCategoryToggle,1);listHead.Children.Add(_filmCategoryToggle);s.Add(FilterBar,listHead,0);
        _filmPager=(FrameworkElement)PageLabel.Parent;s.Set(_filmPager,MarginProperty,new Thickness(16,11.2,16,11.2));
        if(_liveGearPopup?.Child is Border{Child:Panel menu})
            foreach(var action in new[]{LibrarySummaryButton,SourceOptionsButton}){s.Move(action,menu,menu.Children.Count);FilmButton(action,"LiveMenuAction");}
        if(FilmWatchingVisible){_filmInfo=BuildFilmInfo();Grid.SetRow(_filmInfo,2);s.Add(ViewingPanel,_filmInfo);}

        var title=(StackPanel)NowTitle.Parent;
        s.Set(NowTitle,VisibilityProperty,Visibility.Collapsed);
        var displayTitle=FilmText("",20.8,"LiveInk");displayTitle.FontWeight=FontWeights.ExtraBold;displayTitle.Margin=new Thickness(0,11.2,0,0);
        displayTitle.SetBinding(TextBlock.TextProperty,new Binding("Text"){Source=NowTitle,Converter=(IValueConverter)FindResource("LibraryText"),ConverterParameter="Title"});displayTitle.SetBinding(ToolTipProperty,new Binding("Text"){Source=NowTitle});s.Add(title,displayTitle,1);
        if(SeriesWatchingVisible)
        {
            _seriesHeading=displayTitle;BindingOperations.ClearBinding(displayTitle,TextBlock.TextProperty);
            title.Children.Remove(displayTitle);var heading=new Grid{HorizontalAlignment=HorizontalAlignment.Left};heading.ColumnDefinitions.Add(new());heading.ColumnDefinitions.Add(new(){Width=GridLength.Auto});s.Add(title,heading,1);s.Add(heading,displayTitle);
            _seriesCode=FilmText("",9.92,"LiveCaption");var code=new Border{Background=LiveBrush("LiveRaised"),CornerRadius=new CornerRadius(4.8),Padding=new Thickness(5.6,.8,5.6,.8),Margin=new Thickness(9.6,11.2,0,0),Child=_seriesCode,VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(code,1);code.SetBinding(VisibilityProperty,new Binding("Text"){Source=_seriesCode,Converter=new FilmEmptyVisibility()});s.Add(heading,code);
            _seriesCaption=FilmText("",13.6,"LiveCaption");_seriesCaption.Margin=new Thickness(0,2.4,0,0);s.Add(title,_seriesCaption,2);
            AttachSeriesWatchingPresentation(s);
        }
        if(_liveProgrammeCaption is not null)s.Set(_liveProgrammeCaption,VisibilityProperty,Visibility.Collapsed);
        s.Move(CatalogBackButton,title,0);FilmButton(CatalogBackButton,"LibraryBackButton");s.Set(CatalogBackButton,VisibilityProperty,Visibility.Visible);s.Set(CatalogBackButton,HorizontalAlignmentProperty,HorizontalAlignment.Left);
        var timeline=(Grid)SeekSlider.Parent;
        s.Set(PlaybackBadge,VisibilityProperty,Visibility.Collapsed);s.Set(LiveEdgeButton,VisibilityProperty,Visibility.Collapsed);s.Set(SeekSlider,Control.ForegroundProperty,LiveBrush("LiveAccent"));
        _filmElapsed=FilmText("",12.48,"LiveCaption");_filmElapsed.FontWeight=FontWeights.SemiBold;_filmElapsed.Margin=new Thickness(0,0,14.4,0);s.Add(timeline,_filmElapsed);
        _filmTotal=FilmText("",12.48,"LiveCaption");_filmTotal.FontWeight=FontWeights.SemiBold;_filmTotal.Margin=new Thickness(14.4,0,0,0);Grid.SetColumn(_filmTotal,2);s.Add(timeline,_filmTotal);
        Typography.SetNumeralAlignment(_filmElapsed,FontNumeralAlignment.Tabular);Typography.SetNumeralAlignment(_filmTotal,FontNumeralAlignment.Tabular);
        var volume=(StackPanel)VolumeSlider.Parent;var reveal=new Border{Style=LiveStyle("LibraryVolumeReveal"),HorizontalAlignment=HorizontalAlignment.Left};
        // Restore the original slider to its original panel; its input/commit handlers stay attached.
        int index=volume.Children.IndexOf(VolumeSlider);volume.Children.Remove(VolumeSlider);reveal.Child=VolumeSlider;volume.Children.Insert(index,reveal);
        s.Undo(()=>{reveal.Child=null;volume.Children.Remove(reveal);volume.Children.Insert(index,VolumeSlider);});
        s.Set(VolumeSlider,WidthProperty,88d);
        _filmConnecting=new StackPanel{HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false};
        _filmConnecting.Children.Add(new Ellipse{Width=41.6,Height=41.6,Stroke=LiveBrush("LiveAccent"),StrokeThickness=3,StrokeDashArray=new DoubleCollection{18,8},HorizontalAlignment=HorizontalAlignment.Center});
        var connecting=FilmText("Bağlanıyor…",15.2,"LiveInk");connecting.Margin=new Thickness(0,12.8,0,0);_filmConnecting.Children.Add(connecting);s.Add(_liveOverlayRoot!,_filmConnecting);
        var textChanges=DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty,typeof(TextBlock));
        textChanges.AddValueChanged(PlaybackBadge,FilmStateTextChanged);textChanges.AddValueChanged(StatusText,FilmStateTextChanged);
        s.Undo(()=>{textChanges.RemoveValueChanged(PlaybackBadge,FilmStateTextChanged);textChanges.RemoveValueChanged(StatusText,FilmStateTextChanged);});
        PreviewMouseDown+=FilmOutsidePointer;s.Undo(()=>PreviewMouseDown-=FilmOutsidePointer);
        var overlay=_liveControls!;
        overlay.AddHandler(Mouse.PreviewMouseDownEvent,new MouseButtonEventHandler(FilmOutsidePointer),true);
        s.Undo(()=>overlay.RemoveHandler(Mouse.PreviewMouseDownEvent,new MouseButtonEventHandler(FilmOutsidePointer)));
        Video.Clicked+=FilmNativePointer;Video.DoubleClicked+=FilmNativePointer;
        s.Undo(()=>{Video.Clicked-=FilmNativePointer;Video.DoubleClicked-=FilmNativePointer;});
    }
    private void FilmStateTextChanged(object? sender,EventArgs e)=>UpdateFilmAppearance();
    private Border BuildFilmInfo()
    {
        var card=new Border{Style=LiveStyle("LibraryInfoCard"),Margin=new Thickness(0,16,0,0)};AutomationProperties.SetName(card,"Film bilgisi");
        var grid=new Grid();grid.ColumnDefinitions.Add(new(){Width=GridLength.Auto});grid.ColumnDefinitions.Add(new());grid.ColumnDefinitions.Add(new(){Width=GridLength.Auto});card.Child=grid;
        grid.Children.Add(new LibraryPosterPresentation{Width=89.6,Height=134.4});
        var text=new StackPanel{Margin=new Thickness(19.2,0,19.2,0),VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(text,1);grid.Children.Add(text);
        var eyebrow=FilmText("ŞİMDİ OYNATILIYOR",10.88,"LiveAccent");eyebrow.FontWeight=FontWeights.Bold;text.Children.Add(eyebrow);
        var title=FilmText("",20,"LiveInk");title.FontWeight=FontWeights.ExtraBold;title.Margin=new Thickness(0,4,0,1.6);title.SetBinding(TextBlock.TextProperty,DisplayBinding("Name","Title"));title.SetBinding(ToolTipProperty,new Binding("Name"));text.Children.Add(title);
        var note=FilmText("",12.8,"LiveMuted");note.Style=LiveStyle("LibraryOptionalText");note.SetBinding(TextBlock.TextProperty,DisplayBinding("Name","Note"));text.Children.Add(note);
        var tags=new WrapPanel{Margin=new Thickness(0,8,0,0)};text.Children.Add(tags);
        foreach(var pair in new[]{("Name","Year"),("Category","Category"),("Category","Group")})
        {
            var label=FilmText("",12.16,"LibraryTagInk");label.SetBinding(TextBlock.TextProperty,DisplayBinding(pair.Item1,pair.Item2));
            var tag=new CatalogPillBorder{Style=LiveStyle("LibraryTag"),Child=label};tag.SetBinding(VisibilityProperty,new Binding("Text"){Source=label,Converter=new FilmEmptyVisibility()});tags.Children.Add(tag);
        }
        var actions=new StackPanel{VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(actions,2);grid.Children.Add(actions);
        _filmFavoriteLabel=FilmText("",13.44,"LiveInk");_filmFavoriteLabel.FontWeight=FontWeights.Bold;
        _filmFavoriteIcon=new SvgIcon("star"){Width=16.8,Height=16.8,Foreground=LiveBrush("LiveAccent"),Margin=new Thickness(0,0,8,0)};
        var favoriteContent=new StackPanel{Orientation=Orientation.Horizontal};favoriteContent.Children.Add(_filmFavoriteIcon);favoriteContent.Children.Add(_filmFavoriteLabel);
        _filmFavorite=new Button{Style=LiveStyle("LibraryInfoButton"),Content=favoriteContent};_filmFavorite.Click+=Favorite_Click;_filmFavorite.PreviewMouseLeftButtonDown+=Favorite_PreviewMouseDown;AutomationProperties.SetName(_filmFavorite,"Favoriyi değiştir");actions.Children.Add(_filmFavorite);
        _filmFavoriteLabel.SetBinding(TextBlock.TextProperty,new Binding("Tag.IsFavorite"){Source=_filmFavorite,Converter=new FilmFavoriteLabel()});
        _filmFavoriteIcon.SetBinding(SvgIcon.IconProperty,new Binding("Tag.IsFavorite"){Source=_filmFavorite,Converter=(IValueConverter)FindResource("FavoriteIcon")});
        _filmNextLabel=FilmText("",13.44,"LiveInk");_filmNextLabel.MaxWidth=190;
        _filmNext=new Button{Style=LiveStyle("LibraryInfoButton"),Content=_filmNextLabel,Margin=new Thickness(0,8,0,0)};_filmNext.Click+=NextChannel_Click;AutomationProperties.SetName(_filmNext,"Sıradaki film");actions.Children.Add(_filmNext);
        return card;
    }
    private Binding DisplayBinding(string path,string part)=>new(path){Converter=(IValueConverter)FindResource("LibraryText"),ConverterParameter=part};
    private TextBlock FilmText(string text,double size,string brush)=>new(){Text=text,FontSize=size,Foreground=LiveBrush(brush),VerticalAlignment=VerticalAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis};
    private void FilmButton(Button button,string key)
    {
        foreach(var p in new[]{Control.BackgroundProperty,Control.BorderBrushProperty,Control.BorderThicknessProperty,Control.ForegroundProperty,Control.PaddingProperty,MarginProperty,MinWidthProperty,MinHeightProperty})_filmScope!.Clear(button,p);
        _filmScope!.Set(button,StyleProperty,FindResource(key));
    }
    private void FilmCategoryToggle(object sender,RoutedEventArgs e){_filmDrawer=!_filmDrawer;UpdateFilmAppearance();if(_filmDrawer)_filmCategories?.Picker.Focus();}
    private void FilmOutsidePointer(object sender,MouseButtonEventArgs e)
    {
        if(_filmDrawer&&(ReferenceEquals(sender,_liveControls)||(_filmCategories?.IsMouseOver!=true&&_filmCategoryToggle?.IsMouseOver!=true)))FilmNativePointer();
    }
    private void FilmNativePointer(){if(_filmDrawer){_filmDrawer=false;UpdateFilmAppearance();}}
    private void DetachFilmAppearance()
    {
        if(_filmScope is null)return;
        bool updating=_filmUpdating;_filmUpdating=true;
        try
        {
            _filmCategories?.Disconnect();_filmScope.Dispose();_filmScope=null;
            ChannelList.ClearValue(LibraryPresentation.PlayingItemProperty);
            _filmCategories=null;_filmCategoryColumn=null;_filmInfo=null;_filmConnecting=null;_filmPager=null;_filmDrawer=false;_filmStatusHidden=null;
        }
        finally{_filmUpdating=updating;}
    }
    private bool TryFilmPaneKey(KeyEventArgs e)
    {
        if(!LibraryWatchingVisible||_filmCategories is null||Keyboard.Modifiers!=ModifierKeys.None)return false;
        if(SeriesWatchingVisible&&_seriesPanel.TryWatchingKey(e))return true;
        bool categories=_filmCategories.Picker.IsKeyboardFocusWithin,items=ChannelList.IsKeyboardFocusWithin;
        if(!categories&&!items)return false;
        if(e.Key==Key.Enter&&Keyboard.FocusedElement is ButtonBase)return false;
        var list=categories?_filmCategories.Picker:ChannelList;
        if(e.Key is Key.Up or Key.Down)
        {
            if(list.Items.Count>0){list.SelectedIndex=Math.Clamp(list.SelectedIndex+(e.Key==Key.Down?1:-1),0,list.Items.Count-1);list.ScrollIntoView(list.SelectedItem);if(list.ItemContainerGenerator.ContainerFromIndex(list.SelectedIndex) is ListBoxItem row)row.Focus();}
        }
        else if(e.Key==Key.Right){if(categories){ChannelList.Focus();}else{_liveAutoHide?.KeyboardEntry();_liveControls?.Activate();if(!PreviousChannelButton.Focus())PlayButton.Focus();}}
        else if(e.Key==Key.Left){if(items){_filmDrawer=_filmNarrow;UpdateFilmAppearance();_filmCategories.Picker.Focus();}else (SeriesWatchingVisible?SeriesNav:MovieNav).Focus();}
        else if(e.Key==Key.Enter){if(categories){_filmDrawer=false;UpdateFilmAppearance();ChannelList.Focus();}else Channel_DoubleClick(ChannelList,new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left));}
        else if(e.Key==Key.Escape&&_filmDrawer){_filmDrawer=false;UpdateFilmAppearance();_filmCategoryToggle?.Focus();}
        else return false;
        e.Handled=true;return true;
    }
    private sealed class FilmEmptyVisibility : IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)=>string.IsNullOrWhiteSpace(value?.ToString())?Visibility.Collapsed:Visibility.Visible;
        public object ConvertBack(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)=>Binding.DoNothing;
    }
    private sealed class FilmFavoriteLabel : IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)=>value is true?"Favorilerde":"Favorilere ekle";
        public object ConvertBack(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)=>Binding.DoNothing;
    }
    internal sealed class FilmVisualScope : IDisposable
    {
        private readonly List<Action> _undo=[];private readonly HashSet<(DependencyObject,DependencyProperty)> _saved=[];
        private void Capture(DependencyObject target,DependencyProperty property)
        {
            if(!_saved.Add((target,property)))return;
            var binding=BindingOperations.GetBindingBase(target,property);var local=target.ReadLocalValue(property);
            _undo.Add(()=>{if(binding is not null)BindingOperations.SetBinding(target,property,binding);else if(local==DependencyProperty.UnsetValue)target.ClearValue(property);else target.SetValue(property,local);});
        }
        internal void Set(DependencyObject target,DependencyProperty property,object value){Capture(target,property);if(!Equals(target.GetValue(property),value))target.SetValue(property,value);}
        internal void Clear(DependencyObject target,DependencyProperty property){Capture(target,property);target.ClearValue(property);}
        internal void Bind(DependencyObject target,DependencyProperty property,BindingBase binding){Capture(target,property);BindingOperations.SetBinding(target,property,binding);}
        internal void Resource(FrameworkElement target,object key,object value)
        {
            bool had=target.Resources.Contains(key);object? previous=had?target.Resources[key]:null;
            target.Resources[key]=value;_undo.Add(()=>{if(had)target.Resources[key]=previous;else target.Resources.Remove(key);});
        }
        internal void Undo(Action action)=>_undo.Add(action);
        internal void Add(Panel panel,UIElement child,int index=-1){if(index<0)panel.Children.Add(child);else panel.Children.Insert(index,child);_undo.Add(()=>panel.Children.Remove(child));}
        internal void Move(FrameworkElement child,Panel panel,int index)
        {
            var old=(Panel)child.Parent;int previous=old.Children.IndexOf(child);old.Children.Remove(child);panel.Children.Insert(index,child);
            _undo.Add(()=>{panel.Children.Remove(child);old.Children.Insert(Math.Min(previous,old.Children.Count),child);});
        }
        public void Dispose(){for(int i=_undo.Count-1;i>=0;i--)_undo[i]();_undo.Clear();_saved.Clear();}
    }
}
