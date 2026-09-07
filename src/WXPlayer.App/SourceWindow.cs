using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed class SourceWindow : PremiumWindow
{
    internal SourceConfig? Result {get;private set;}
    internal SourceWindow(Window owner,SourceConfig? current=null):base(owner,current is null?"Kütüphanenizi bağlayın":"Kaynağı düzenleyin","Kendi kaynağınız. Tüm içerikleriniz tek yerde.","library",760,790)
    {
        var content=new StackPanel();Body.Children.Add(new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        var types=new UniformGridCompat(); // evenly sized source choices
        content.Children.Add(types);int kind=(int)(current?.Kind??SourceKind.Playlist);
        var buttons=new List<Button>();
        var details=Card(content,"Bağlantı bilgileri","Sağlayıcınızın verdiği adresi veya yerel listenizi kullanın.");
        TextBox Field(Panel panel,string label,string value=""){var caption=Text(label,11,"#9DADBE");caption.Margin=new(0,10,0,7);panel.Children.Add(caption);var field=new TextBox{Text=value,MinHeight=44};panel.Children.Add(field);return field;}
        var name=Field(details,"KAYNAK ADI",current?.Name??"Kütüphanem");var address=Field(details,"SUNUCU / OYNATMA LİSTESİ",current?.Address??"");
        var browse=Action("Dosyadan seç",()=>{var d=new OpenFileDialog{Filter="Oynatma listeleri|*.m3u;*.m3u8;*.txt"};if(d.ShowDialog(this)==true)address.Text=d.FileName;});browse.HorizontalAlignment=HorizontalAlignment.Left;browse.Margin=new(0,12,0,0);details.Children.Add(browse);
        var credentials=Card(content,"Hesabınız","Bilgileriniz bu Windows kullanıcısı için şifrelenir.");var user=Field(credentials,"KULLANICI ADI",current?.Username??"");credentials.Children.Add(Text("ŞİFRE",11,"#9DADBE"));var password=new PasswordBox{Password=current?.Password??"",MinHeight=44,Margin=new(0,7,0,0)};credentials.Children.Add(password);
        var portal=Card(content,"Portal kimliği","Sağlayıcının hesabınıza tanımladığı cihaz adresi.");var mac=Field(portal,"MAC ADRESİ",current?.Mac??"");
        var guide=Card(content,"Program rehberi","İsteğe bağlı · Boş bırakırsanız kaynaktan otomatik keşfedilir.");var epg=Field(guide,"XMLTV ADRESİ / DOSYASI",current?.EpgUrl??"");
        void Select(int selected){kind=selected;for(int i=0;i<buttons.Count;i++){buttons[i].Background=Brush(i==kind?"#283F36":"#18232E");buttons[i].BorderBrush=Brush(i==kind?"#789966":"#30414E");}((FrameworkElement)credentials.Parent).Visibility=kind==1?Visibility.Visible:Visibility.Collapsed;((FrameworkElement)portal.Parent).Visibility=kind==2?Visibility.Visible:Visibility.Collapsed;browse.Visibility=kind==0?Visibility.Visible:Visibility.Collapsed;}
        foreach(var (title,icon) in new[]{("M3U / TXT","library"),("Xtream Codes","tv"),("Stalker Portal","source")})
        {int index=buttons.Count;var b=new Button{Content=new IconLabel{Icon=icon=="source"?"library":icon,Label=title},MinHeight=52,Margin=new(0,0,index<2?8:0,16)};b.Click+=(_,_)=>Select(index);buttons.Add(b);Grid.SetColumn(b,index);types.Children.Add(b);}Select(kind);
        var error=Text("",11,"#FFB6A3");error.MaxWidth=310;error.VerticalAlignment=VerticalAlignment.Center;Footer.Children.Add(error);Footer.Children.Add(Action("Vazgeç",Close));Footer.Children.Add(Action("Bağlan ve yükle",()=>
        {
            try
            {
                var source=current is null?new SourceConfig():current with{};source.Name=name.Text.Trim();source.Kind=(SourceKind)kind;source.Address=address.Text.Trim();source.Username=user.Text.Trim();source.Password=password.Password;source.Mac=mac.Text.Trim();source.EpgUrl=epg.Text.Trim();
                if(source.Name.Length==0||source.Address.Length==0)throw new InvalidOperationException("Kaynak adı ve adresi gerekli.");
                if(!File.Exists(source.Address)||source.Kind!=SourceKind.Playlist)AddressPolicy.Http(source.Address);
                if(source.EpgUrl.Length>0&&!File.Exists(source.EpgUrl))AddressPolicy.Http(source.EpgUrl);
                if(source.Kind==SourceKind.Xtream)ProviderClient.ParseXtreamAddress(source);
                if(source.Kind==SourceKind.Stalker&&!System.Text.RegularExpressions.Regex.IsMatch(source.Mac,"^([0-9a-fA-F]{2}:){5}[0-9a-fA-F]{2}$"))throw new InvalidOperationException("Geçerli bir MAC adresi gerekli.");
                Result=source;DialogResult=true;
            }catch(Exception ex){error.Text=ex is InvalidOperationException?ex.Message:"Bağlantı bilgilerini kontrol edin.";}
        },true));
    }
    private sealed class UniformGridCompat : Grid{internal UniformGridCompat(){for(int i=0;i<3;i++)ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});}}
}
