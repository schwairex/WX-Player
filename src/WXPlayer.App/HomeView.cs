using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed record HomeShelf(string Title,string Section,WXPlayer.Core.Page Page);

internal sealed class HomeView : ScrollViewer
{
    private readonly StackPanel _body=new();
    internal readonly TextBox Search=new(){MinHeight=42,Padding=new Thickness(12,8,12,8)};
    private readonly StackPanel _shelves=new();
    private readonly Grid _feature=new();
    private readonly Button _return;
    private readonly Action<ContentItem> _play;
    private readonly Action<ContentItem> _favorite;
    private readonly Action<string> _browse;
    private readonly Action _add;
    internal IReadOnlyList<ContentItem> Items {get;private set;}=[];
    internal ContentItem? Featured {get;private set;}
    internal bool Empty {get;private set;}
    private static Brush Color(string value)=>PremiumWindow.Brush(value);
    private static TextBlock Text(string value,int size=13,string color="#E9EEF5")=>PremiumWindow.Text(value,size,color);
    internal HomeView(Action<ContentItem> play,Action<ContentItem> favorite,Action<string> browse,Action add,Action resume)
    {
        _play=play;_favorite=favorite;_browse=browse;_add=add;
        VerticalScrollBarVisibility=ScrollBarVisibility.Auto;HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled;Content=_body;
        var header=new Grid{Margin=new Thickness(0,0,10,20)};header.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});header.ColumnDefinitions.Add(new(){Width=new GridLength(270)});
        var shortcuts=new WrapPanel{VerticalAlignment=VerticalAlignment.Center};header.Children.Add(shortcuts);
        foreach(var (label,section) in new[]{("Canlı TV","live"),("Filmler","movie"),("Diziler","series"),("Favoriler","favorites")})
        {var button=Action(label,()=>browse(section));button.Margin=new Thickness(0,0,8,4);button.Padding=new Thickness(14,8,14,8);shortcuts.Children.Add(button);}
        var searchBox=new Grid();searchBox.Children.Add(Search);var hint=Text("Kütüphanende ara…",12,"#8EA0B3");hint.Margin=new Thickness(14,0,0,0);hint.VerticalAlignment=VerticalAlignment.Center;hint.IsHitTestVisible=false;searchBox.Children.Add(hint);Search.TextChanged+=(_,_)=>hint.Visibility=Search.Text.Length==0?Visibility.Visible:Visibility.Collapsed;
        System.Windows.Automation.AutomationProperties.SetName(Search,"Ana sayfada içerik ara");Grid.SetColumn(searchBox,1);header.Children.Add(searchBox);_body.Children.Add(header);
        _return=Action("İzlemeye dön",resume);_return.HorizontalAlignment=HorizontalAlignment.Left;_return.Margin=new Thickness(0,0,0,16);_return.Visibility=Visibility.Collapsed;_body.Children.Add(_return);
        _feature.Margin=new Thickness(0,0,10,26);_body.Children.Add(_feature);_shelves.Margin=new Thickness(0,0,10,24);_body.Children.Add(_shelves);
    }
    private static Button Action(string label,Action action,bool primary=false)
    {var b=PremiumWindow.Action(label,action,primary);b.Margin=new Thickness(0);return b;}
    internal void NowPlaying(string? title){_return.Content="▶  İzlemeye dön · "+title;_return.Visibility=string.IsNullOrEmpty(title)?Visibility.Collapsed:Visibility.Visible;}
    internal void Loading(){_feature.Children.Clear();_shelves.Children.Clear();Items=[];Featured=null;Empty=false;_feature.Children.Add(Text("Kütüphanen hazırlanıyor…",20,"#A9BAC8"));}
    internal void Render(string? source,IReadOnlyList<HomeShelf> shelves,string search)
    {
        Items=shelves.SelectMany(s=>s.Page.Items).DistinctBy(i=>i.Id).ToArray();Empty=Items.Count==0;_feature.Children.Clear();_shelves.Children.Clear();
        Featured=Items.FirstOrDefault(i=>i.Kind==ContentKind.Movie&&!string.IsNullOrWhiteSpace(i.Logo))??Items.FirstOrDefault(i=>i.Kind==ContentKind.Movie)??Items.FirstOrDefault();
        if(Featured is null)
        {
            var empty=new StackPanel{Margin=new Thickness(38)};empty.Children.Add(Text(search.Length>0?"Aradığın içerik bulunamadı":source is null?"Kütüphanen seni bekliyor":"Bu kaynakta henüz içerik yok",30));
            var caption=Text(search.Length>0?"Farklı bir ad deneyin veya aramanızı temizleyin.":"M3U listenizi veya Xtream hesabınızı bağlayın. Filmleriniz, dizileriniz ve canlı kanallarınız burada yerini alsın.",14,"#A5B7C3");caption.Margin=new Thickness(0,18,0,26);empty.Children.Add(caption);
            var add=Action(search.Length>0?"Aramayı temizle":"Kaynak ekle",()=>{if(search.Length>0)Search.Clear();else _add();},true);add.HorizontalAlignment=HorizontalAlignment.Left;empty.Children.Add(add);
            _feature.Children.Add(new Border{Background=Color("#182830"),BorderBrush=Color("#344D54"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(20),MinHeight=290,Child=empty});return;
        }
        BuildHero(Featured,source??"Kütüphaneniz");
        foreach(var shelf in shelves.Where(s=>s.Page.Items.Count>0))BuildShelf(shelf);
    }
    private static Brush Backdrop(ContentItem item)
    {
        string[] colors=["#24484B","#313F60","#56403B","#3E3658","#2D4840"];
        int index=(int)(item.Name.Aggregate(0u,(n,c)=>n*31+c)%colors.Length);
        return new LinearGradientBrush(((SolidColorBrush)Color(colors[index])).Color,((SolidColorBrush)Color("#111A25")).Color,55);
    }
    private void BuildHero(ContentItem item,string source)
    {
        var hero=new Grid{Background=Backdrop(item),ClipToBounds=true};Round(hero,20);
        hero.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});hero.ColumnDefinitions.Add(new(){Width=new GridLength(260)});
        var art=new ChannelLogo{Url=item.Logo,Initials="",DecodeWidth=900,ImageStretch=Stretch.UniformToFill,ImagePadding=new Thickness(0),Opacity=.16};Grid.SetColumnSpan(art,2);hero.Children.Add(art);
        var copy=new StackPanel{Margin=new Thickness(34,30,24,30),VerticalAlignment=VerticalAlignment.Center};hero.Children.Add(copy);
        var eyebrow=Text("KÜTÜPHANENDEN  /  "+item.KindLabel,10,"#C1EC8B");eyebrow.FontWeight=FontWeights.SemiBold;copy.Children.Add(eyebrow);
        var title=Text(item.Name,36);title.FontWeight=FontWeights.Bold;title.MaxHeight=98;title.TextTrimming=TextTrimming.CharacterEllipsis;title.Margin=new Thickness(0,14,0,14);copy.Children.Add(title);
        copy.Children.Add(Text(item.Category,14,"#CBDBDD"));var sourceLabel=Text(source,12,"#8FA9B4");sourceLabel.Margin=new Thickness(0,8,0,24);copy.Children.Add(sourceLabel);
        var actions=new WrapPanel();actions.Children.Add(Action("▶  Şimdi izle",()=>_play(item),true));var favorite=Action(item.IsFavorite?"✓  Favorilerimde":"＋  Favorilere ekle",()=>_favorite(item));favorite.Margin=new Thickness(10,0,0,0);actions.Children.Add(favorite);copy.Children.Add(actions);
        var poster=Artwork(item,210,272,false);poster.Margin=new Thickness(10,18,28,18);Grid.SetColumn(poster,1);hero.Children.Add(poster);
        _feature.Children.Add(new Border{CornerRadius=new CornerRadius(20),BorderBrush=Color("#354750"),BorderThickness=new Thickness(1),Child=hero,MinHeight=310});
    }
    private static void Round(FrameworkElement element,double radius)
    {element.SizeChanged+=(_,_)=>element.Clip=new RectangleGeometry(new Rect(0,0,element.ActualWidth,element.ActualHeight),radius,radius);}
    private static Grid Artwork(ContentItem item,double width,double height,bool live)
    {
        var image=new Grid{Width=width,Height=height,Background=Backdrop(item),ClipToBounds=true};Round(image,12);
        var watermark=Text(item.Initials,live?28:60,"#506B78");watermark.FontWeight=FontWeights.Bold;watermark.HorizontalAlignment=HorizontalAlignment.Center;watermark.VerticalAlignment=VerticalAlignment.Center;image.Children.Add(watermark);
        image.Children.Add(new ChannelLogo{Url=item.Logo,Initials="",DecodeWidth=live?192:480,ImageStretch=live?Stretch.Uniform:Stretch.UniformToFill,ImagePadding=new Thickness(live?22:0)});
        var shade=new Border{Background=new LinearGradientBrush(Colors.Transparent,ColorConverter.ConvertFromString("#C80D151D") is System.Windows.Media.Color c?c:Colors.Black,90)};image.Children.Add(shade);
        var badge=Text(item.KindLabel,9,"#DEEDD7");badge.FontWeight=FontWeights.SemiBold;
        image.Children.Add(new Border{Child=badge,Background=Color("#D523342F"),CornerRadius=new CornerRadius(5),Padding=new Thickness(8,4,8,4),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(10)});
        var caption=Text(item.Name,live?15:19);caption.FontWeight=FontWeights.SemiBold;caption.MaxHeight=54;caption.TextTrimming=TextTrimming.CharacterEllipsis;caption.VerticalAlignment=VerticalAlignment.Bottom;caption.Margin=new Thickness(14);image.Children.Add(caption);
        return image;
    }
    private void BuildShelf(HomeShelf shelf)
    {
        var section=new StackPanel{Margin=new Thickness(0,0,0,28)};_shelves.Children.Add(section);
        var header=new DockPanel{Margin=new Thickness(0,0,0,14)};section.Children.Add(header);
        var all=Action("Tümünü gör  ›",()=>_browse(shelf.Section));all.Background=Brushes.Transparent;all.BorderThickness=new Thickness(0);all.FontSize=11;all.Foreground=Color("#BADAAC");DockPanel.SetDock(all,Dock.Right);header.Children.Add(all);
        var caption=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};var title=Text(shelf.Title,21);title.FontWeight=FontWeights.SemiBold;caption.Children.Add(title);var count=Text(shelf.Page.Total.ToString("N0"),11,"#849AA9");count.Margin=new Thickness(12,5,0,0);caption.Children.Add(count);header.Children.Add(caption);
        var row=new Grid();row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});section.Children.Add(row);
        var cards=new StackPanel{Orientation=Orientation.Horizontal};var scroll=new ScrollViewer{Content=cards,HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled};row.Children.Add(scroll);
        var arrows=new StackPanel{VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(10,0,0,35)};Grid.SetColumn(arrows,1);row.Children.Add(arrows);var next=Action("›",()=>scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset+550));arrows.Children.Add(next);var prev=Action("‹",()=>scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset-550));prev.Margin=new Thickness(0,8,0,0);arrows.Children.Add(prev);
        System.Windows.Automation.AutomationProperties.SetName(next,shelf.Title+" · Sonraki kartlar");System.Windows.Automation.AutomationProperties.SetName(prev,shelf.Title+" · Önceki kartlar");
        scroll.ScrollChanged+=(_,_)=>{next.IsEnabled=scroll.HorizontalOffset<scroll.ScrollableWidth-1;prev.IsEnabled=scroll.HorizontalOffset>1;};
        foreach(var item in shelf.Page.Items)
        {
            bool live=item.Kind==ContentKind.Live;double width=live?224:166;
            var card=new StackPanel{Width=width};card.Children.Add(Artwork(item,width,live?132:214,live));var name=Text(item.Name,12);name.FontWeight=FontWeights.SemiBold;name.MaxHeight=34;name.Margin=new Thickness(0,10,0,4);name.TextTrimming=TextTrimming.CharacterEllipsis;card.Children.Add(name);var category=Text(item.Category,10,"#8298A9");category.TextTrimming=TextTrimming.CharacterEllipsis;category.MaxHeight=16;card.Children.Add(category);
            var button=new Button{Content=card,Tag=item,Padding=new Thickness(0),Margin=new Thickness(0,0,16,0),Background=Brushes.Transparent,BorderThickness=new Thickness(0),ToolTip=item.Name,VerticalAlignment=VerticalAlignment.Top};System.Windows.Automation.AutomationProperties.SetName(button,"İzle · "+item.Name);button.Click+=(_,_)=>_play(item);cards.Children.Add(button);
        }
    }
}

