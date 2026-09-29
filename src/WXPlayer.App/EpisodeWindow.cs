using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed record EpisodeSelection(ContentItem Item,bool Restart);
internal sealed class EpisodeWindow : PremiumWindow
{
    internal ListBox Episodes {get;}=new(){BorderThickness=new Thickness(0)};
    internal ComboBox Seasons {get;}=new(){MinHeight=40,Margin=new Thickness(0,0,0,12)};
    internal EpisodeSelection? Result {get;private set;}
    internal EpisodeWindow(Window owner,ContentItem series,IReadOnlyList<ContentItem> episodes):base(owner,series.Name,"Sezonunu ve bölümünü seç. Kaldığın yer burada saklanır.","series",820,690)
    {
        Body.ColumnDefinitions.Add(new(){Width=new GridLength(172)});Body.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
        var cover=new StackPanel{Margin=new Thickness(0,0,22,0)};Body.Children.Add(cover);
        cover.Children.Add(new Border{Height=210,CornerRadius=new CornerRadius(12),Background=Brush("#1C2934"),Child=new ChannelLogo{Url=series.Logo,Initials=series.Initials,DecodeWidth=480,ImageStretch=Stretch.Uniform,ImagePadding=new Thickness(8)}});
        var note=Text(series.ProgressLabel.Length>0?"SON İZLENEN\n"+series.ProgressLabel:"Bölümler oynatıldıkça izleme durumları burada görünür.",11,"#9CACBA");note.Margin=new Thickness(0,18,0,0);cover.Children.Add(note);
        var right=new DockPanel();Grid.SetColumn(right,1);Body.Children.Add(right);
        var picker=new StackPanel();DockPanel.SetDock(picker,Dock.Top);right.Children.Add(picker);
        var eyebrow=Text("BÖLÜM LİSTESİ",10,"AccentPrimaryBrush");eyebrow.FontWeight=FontWeights.SemiBold;eyebrow.Margin=new Thickness(0,0,0,8);picker.Children.Add(eyebrow);
        picker.Children.Add(Seasons);
        var count=Text("",11,"TextMutedBrush");count.Margin=new Thickness(0,0,0,10);picker.Children.Add(count);
        right.Children.Add(Episodes);
        Episodes.ItemContainerStyle=new Style(typeof(ListBoxItem),(Style)Application.Current.FindResource(typeof(ListBoxItem))){Setters={new Setter(MarginProperty,new Thickness(0,0,0,7)),new Setter(PaddingProperty,new Thickness(11,10,11,10))}};
        Episodes.ItemTemplate=(DataTemplate)XamlReader.Parse("""
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
              <Grid Height="58"><Grid.ColumnDefinitions><ColumnDefinition Width="48"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                <Border Width="38" Height="38" Background="{DynamicResource SurfaceElevatedBrush}" CornerRadius="9" VerticalAlignment="Center"><TextBlock Text="{Binding Episode}" FontSize="15" FontWeight="SemiBold" Foreground="{DynamicResource AccentPrimaryBrush}" HorizontalAlignment="Center" VerticalAlignment="Center"/></Border>
                <StackPanel Grid.Column="1" VerticalAlignment="Center">
                  <TextBlock Text="{Binding EpisodeLabel}" FontSize="12" FontWeight="SemiBold" Foreground="{DynamicResource AccentPrimaryBrush}"/>
                  <TextBlock Text="{Binding Name}" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="12" Margin="0,4,0,0" TextTrimming="CharacterEllipsis" ToolTip="{Binding Name}"/>
                  <TextBlock Text="{Binding ProgressLabel}" Foreground="{DynamicResource TextMutedBrush}" FontSize="10" Margin="0,4,0,0" TextTrimming="CharacterEllipsis"/>
                </StackPanel>
              </Grid>
            </DataTemplate>
            """);
        VirtualizingPanel.SetIsVirtualizing(Episodes,true);ScrollViewer.SetCanContentScroll(Episodes,true);ScrollViewer.SetHorizontalScrollBarVisibility(Episodes,ScrollBarVisibility.Disabled);
        var seasonValues=episodes.Select(i=>i.Season).Distinct().Order().ToArray();Seasons.ItemsSource=seasonValues.Select(s=>s==0?"Bölümler":"Sezon "+s).ToArray();
        var last=episodes.Where(i=>i.Progress is not null).OrderByDescending(i=>i.Progress!.Updated).FirstOrDefault();
        void Filter(){if(Seasons.SelectedIndex<0)return;var items=episodes.Where(i=>i.Season==seasonValues[Seasons.SelectedIndex]).OrderBy(i=>i.Episode).ThenBy(i=>i.Name).ToArray();Episodes.ItemsSource=items;Episodes.SelectedItem=items.FirstOrDefault(i=>i.Id==last?.Id)??items.FirstOrDefault();count.Text=$"Bu sezonda {items.Length} bölüm · Seçmek için bir bölüme tıklayın";}
        Seasons.SelectionChanged+=(_,_)=>Filter();
        void Choose(bool restart){if(Episodes.SelectedItem is ContentItem item){Result=new(item,restart);DialogResult=true;}}
        var start=Action("Baştan oynat",()=>Choose(true));var play=Action("Bölümü oynat",()=>Choose(false),true);Footer.Children.Add(Action("Kapat",Close));Footer.Children.Add(start);Footer.Children.Add(play);
        Episodes.SelectionChanged+=(_,_)=>{var item=Episodes.SelectedItem as ContentItem;play.IsEnabled=start.IsEnabled=item is not null;play.Content=item?.Progress is {Completed:false,PositionMs:>=5000}?"Kaldığın yerden devam et":"Bölümü oynat";};
        Episodes.MouseDoubleClick+=(_,_)=>Choose(false);Seasons.SelectedIndex=Math.Max(0,Array.IndexOf(seasonValues,last?.Season??seasonValues.FirstOrDefault()));
    }
}
