using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed class SeriesEpisodePanel : Border
{
    internal ComboBox Seasons { get; } = new() { MinWidth=156, MinHeight=38 };
    internal ListBox Episodes { get; } = new() { BorderThickness=new Thickness(0) };
    private readonly TextBlock _title = PremiumWindow.Text("Bölümler",18);
    private readonly TextBlock _state = PremiumWindow.Text("",12,"#9CACBA");
    private readonly Button _previous, _next, _retry;
    private IReadOnlyList<ContentItem> _episodes = [];
    private ContentItem? _current;
    private bool _binding;
    private sealed record SeasonChoice(int Number) { public override string ToString() => Number==0?"Özel bölümler":"Sezon "+Number; }
    internal SeriesEpisodePanel(Func<ContentItem,Task> play, Func<int,Task> adjacent, Func<Task> retry)
    {
        Background=PremiumWindow.Brush("SurfaceBrush");BorderBrush=PremiumWindow.Brush("BorderSubtleBrush");BorderThickness=new Thickness(1);CornerRadius=new CornerRadius(14);Padding=new Thickness(18,15,18,13);Margin=new Thickness(0,14,0,0);
        var root=new DockPanel();Child=root;
        var header=new StackPanel();DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        var eyebrow=PremiumWindow.Text("DİZİ BÖLÜMLERİ",10,"TextMutedBrush");eyebrow.FontWeight=FontWeights.SemiBold;eyebrow.Margin=new Thickness(0,0,0,5);header.Children.Add(eyebrow);
        _title.FontWeight=FontWeights.SemiBold;_title.TextWrapping=TextWrapping.NoWrap;_title.TextTrimming=TextTrimming.CharacterEllipsis;header.Children.Add(_title);
        var bar=new DockPanel{Margin=new Thickness(0,12,0,9)};header.Children.Add(bar);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(buttons,Dock.Right);bar.Children.Add(buttons);
        _previous=PremiumWindow.Action("← Önceki",async()=>await adjacent(-1));_previous.ToolTip="Önceki bölüm · Page Up";
        _next=PremiumWindow.Action("Sonraki →",async()=>await adjacent(1));_next.ToolTip="Sonraki bölüm · Page Down";
        foreach(var button in new[]{_previous,_next}) {button.MinHeight=38;button.Padding=new Thickness(13,5,13,5);button.Margin=new Thickness(6,0,0,0);buttons.Children.Add(button);System.Windows.Automation.AutomationProperties.SetName(button,button.ToolTip.ToString());}
        bar.Children.Add(Seasons);System.Windows.Automation.AutomationProperties.SetName(Seasons,"Dizinin sezonu");
        _state.Margin=new Thickness(0,0,0,7);_state.TextWrapping=TextWrapping.Wrap;header.Children.Add(_state);
        _retry=PremiumWindow.Action("Tekrar dene",async()=>await retry());_retry.HorizontalAlignment=HorizontalAlignment.Left;header.Children.Add(_retry);
        root.Children.Add(Episodes);System.Windows.Automation.AutomationProperties.SetName(Episodes,"İzlenen dizinin bölümleri");
        VirtualizingPanel.SetIsVirtualizing(Episodes,true);ScrollViewer.SetHorizontalScrollBarVisibility(Episodes,ScrollBarVisibility.Disabled);
        Episodes.ItemContainerStyle=new Style(typeof(ListBoxItem),(Style)Application.Current.FindResource(typeof(ListBoxItem))){Setters={new Setter(MarginProperty,new Thickness(0,0,0,6)),new Setter(PaddingProperty,new Thickness(11,9,11,9))}};
        Episodes.ItemTemplate=(DataTemplate)XamlReader.Parse("""
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
              <Button Tag="{Binding}" Padding="0" BorderThickness="0" Background="Transparent" HorizontalContentAlignment="Stretch" ToolTip="{Binding Name}">
                <Grid Height="52"><Grid.ColumnDefinitions><ColumnDefinition Width="44"/><ColumnDefinition Width="*"/><ColumnDefinition Width="120"/></Grid.ColumnDefinitions>
                  <Border Width="36" Height="36" CornerRadius="9" Background="{DynamicResource SurfaceElevatedBrush}" VerticalAlignment="Center"><TextBlock Text="{Binding Episode}" Foreground="{DynamicResource AccentPrimaryBrush}" FontWeight="SemiBold" FontSize="14" HorizontalAlignment="Center" VerticalAlignment="Center"/></Border>
                  <StackPanel Grid.Column="1" Margin="10,0,8,0" VerticalAlignment="Center">
                    <TextBlock Text="{Binding EpisodeLabel}" Foreground="{DynamicResource AccentPrimaryBrush}" FontSize="10" FontWeight="SemiBold"/>
                    <TextBlock Text="{Binding Name}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="13" FontWeight="SemiBold" Margin="0,4,0,0" TextTrimming="CharacterEllipsis"/>
                  </StackPanel>
                  <TextBlock Grid.Column="2" Text="{Binding ProgressLabel}" FontSize="10" Foreground="{DynamicResource TextMutedBrush}" TextAlignment="Right" VerticalAlignment="Center" TextTrimming="CharacterEllipsis"/>
                </Grid>
              </Button>
            </DataTemplate>
            """);
        Episodes.AddHandler(Button.ClickEvent,new RoutedEventHandler(async(_,e)=>{if(e.OriginalSource is Button{Tag:ContentItem item}){e.Handled=true;await play(item);}}));
        Seasons.SelectionChanged+=(_,_)=>{if(!_binding)Filter();};
    }
    internal void Loading(ContentItem item) { _current=item;_title.Text=item.SeriesName.Length>0?item.SeriesName:"Dizinin bölümleri";_state.Text="Sezonlar ve bölümler hazırlanıyor…";_episodes=[];Episodes.ItemsSource=null;Seasons.ItemsSource=null;_previous.IsEnabled=_next.IsEnabled=false;_retry.Visibility=Visibility.Collapsed; }
    internal void Error() { _state.Text="Bölümler alınamadı. İzlemeye devam edebilir veya tekrar deneyebilirsiniz.";_retry.Visibility=Visibility.Visible; }
    internal void Show(ContentItem current,IReadOnlyList<ContentItem> episodes)
    {
        _current=current;_episodes=episodes;_title.Text=current.SeriesName.Length>0?current.SeriesName:"Dizinin bölümleri";_title.ToolTip=_title.Text;
        _binding=true;Seasons.ItemsSource=episodes.Select(e=>e.Season).Distinct().Order().Select(s=>new SeasonChoice(s)).ToArray();Seasons.SelectedItem=Seasons.Items.Cast<SeasonChoice>().FirstOrDefault(s=>s.Number==current.Season);_binding=false;
        _previous.IsEnabled=EpisodeNavigation.Adjacent(current,episodes,-1) is not null;_next.IsEnabled=EpisodeNavigation.Adjacent(current,episodes,1) is not null;
        _retry.Visibility=episodes.Count==0?Visibility.Visible:Visibility.Collapsed;Filter();
    }
    private void Filter()
    {
        if(_current is null)return;
        var items=_episodes.Where(e=>e.Season==(Seasons.SelectedItem as SeasonChoice)?.Number).ToArray();Episodes.ItemsSource=items;
        Episodes.SelectedItem=items.FirstOrDefault(e=>e.Id==_current.Id);if(Episodes.SelectedItem is {} selected)Episodes.ScrollIntoView(selected);
        _state.Text=items.Length==0?"Bu sezon için bölüm bulunamadı.":$"Şimdi oynatılıyor: {_current.EpisodeLabel}   ·   Bu sezonda {items.Length} bölüm";
    }
}
