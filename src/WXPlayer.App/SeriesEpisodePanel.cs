using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed class SeriesEpisodePanel : Border
{
    internal ComboBox Seasons { get; } = new() { MinWidth=116, MinHeight=36 };
    internal ListBox Episodes { get; } = new() { BorderThickness=new Thickness(0) };
    private readonly TextBlock _title = PremiumWindow.Text("Bölümler",16);
    private readonly TextBlock _state = PremiumWindow.Text("",12,"#9CACBA");
    private readonly Button _previous, _next, _retry;
    private IReadOnlyList<ContentItem> _episodes = [];
    private ContentItem? _current;
    private bool _binding;
    private sealed record SeasonChoice(int Number) { public override string ToString() => Number==0?"Özel bölümler":"Sezon "+Number; }
    internal SeriesEpisodePanel(Func<ContentItem,Task> play, Func<int,Task> adjacent, Func<Task> retry)
    {
        Background=PremiumWindow.Brush("#111A23");BorderBrush=PremiumWindow.Brush("#293746");BorderThickness=new Thickness(1);CornerRadius=new CornerRadius(14);Padding=new Thickness(16);Margin=new Thickness(0,14,0,0);
        var root=new DockPanel();Child=root;
        var header=new StackPanel();DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        _title.FontWeight=FontWeights.SemiBold;_title.TextWrapping=TextWrapping.NoWrap;_title.TextTrimming=TextTrimming.CharacterEllipsis;header.Children.Add(_title);
        var bar=new DockPanel{Margin=new Thickness(0,10,0,10)};header.Children.Add(bar);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(buttons,Dock.Right);bar.Children.Add(buttons);
        _previous=PremiumWindow.Action("‹",async()=>await adjacent(-1));_previous.ToolTip="Önceki bölüm · Page Up";
        _next=PremiumWindow.Action("Sonraki ›",async()=>await adjacent(1));_next.ToolTip="Sonraki bölüm · Page Down";
        foreach(var button in new[]{_previous,_next}) {button.MinHeight=36;button.Padding=new Thickness(10,5,10,5);buttons.Children.Add(button);System.Windows.Automation.AutomationProperties.SetName(button,button.ToolTip.ToString());}
        bar.Children.Add(Seasons);System.Windows.Automation.AutomationProperties.SetName(Seasons,"Dizinin sezonu");
        _state.Margin=new Thickness(0,0,0,8);_state.TextWrapping=TextWrapping.Wrap;header.Children.Add(_state);
        _retry=PremiumWindow.Action("Tekrar dene",async()=>await retry());_retry.HorizontalAlignment=HorizontalAlignment.Left;header.Children.Add(_retry);
        root.Children.Add(Episodes);System.Windows.Automation.AutomationProperties.SetName(Episodes,"İzlenen dizinin bölümleri");
        VirtualizingPanel.SetIsVirtualizing(Episodes,true);ScrollViewer.SetHorizontalScrollBarVisibility(Episodes,ScrollBarVisibility.Disabled);
        Episodes.ItemTemplate=(DataTemplate)XamlReader.Parse("""
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
              <Button Tag="{Binding}" Padding="0" BorderThickness="0" Background="Transparent" HorizontalContentAlignment="Stretch" ToolTip="{Binding Name}">
                <StackPanel><TextBlock Text="{Binding EpisodeLabel}" Foreground="#BDDCA5" FontSize="11"/>
                  <TextBlock Text="{Binding Name}" FontSize="13" FontWeight="SemiBold" Margin="0,4,0,0" TextTrimming="CharacterEllipsis"/>
                  <TextBlock Text="{Binding ProgressLabel}" FontSize="10" Foreground="#94A6B8" Margin="0,4,0,0" TextTrimming="CharacterEllipsis"/>
                </StackPanel>
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
        _state.Text=items.Length==0?"Bu sezon için bölüm bulunamadı.":$"Şimdi: {_current.EpisodeLabel}  ·  Seçili sezonda {items.Length} bölüm";
    }
}
