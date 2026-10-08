using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed partial class SourceWindow : PremiumWindow
{
    internal SourceConfig? Result {get;private set;}
    internal SourceWindow(Window owner,SourceConfig? current=null):base(owner,current is null?"Kütüphanenizi bağlayın":"Kaynağı düzenleyin","Kendi kaynağınız. Tüm içerikleriniz tek yerde.","library",760,790)
    {
        var content=SourceContent();
        var types=new UniformGridCompat{Margin=new Thickness(0,0,0,24)}; // evenly sized source choices
        content.Children.Add(types);int kind=(int)(current?.Kind??SourceKind.Playlist);
        var buttons=new List<Button>();
        var details=SourceSection(content,"Bağlantı bilgileri","Sağlayıcınızın verdiği adresi veya yerel listenizi kullanın.");
        TextBox Field(Panel panel,string label,string value="")=>SourceField(panel,label,value);
        var name=Field(details,"KAYNAK ADI",current?.Name??"Kütüphanem");var address=Field(details,"SUNUCU / OYNATMA LİSTESİ",current?.Address??"");
        var browse=SourceAction("Dosyadan seç",()=>{var d=new OpenFileDialog{Filter="Oynatma listeleri|*.m3u;*.m3u8;*.txt"};if(d.ShowDialog(this)==true)address.Text=d.FileName;});SourceAddressRow(details,address,browse);
        var credentials=SourceSection(content,"Hesabınız","Bilgileriniz bu Windows kullanıcısı için şifrelenir.");var user=Field(credentials,"KULLANICI ADI",current?.Username??"");SourceCaption(credentials,"ŞİFRE");var password=new PasswordBox{Password=current?.Password??"",MinHeight=44,Style=(Style)FindResource("SourcePassword")};System.Windows.Automation.AutomationProperties.SetName(password,"ŞİFRE");credentials.Children.Add(SourceHint(password,"Şifreniz"));
        var portal=SourceSection(content,"Portal kimliği","Sağlayıcının hesabınıza tanımladığı cihaz adresi.");var mac=Field(portal,"MAC ADRESİ",current?.Mac??"");
        var guide=SourceSection(content,"Program rehberi","İsteğe bağlı · Boş bırakırsanız kaynaktan otomatik keşfedilir.");var epg=Field(guide,"XMLTV ADRESİ / DOSYASI",current?.EpgUrl??"");
        void Select(int selected){kind=selected;for(int i=0;i<buttons.Count;i++){buttons[i].Style=(Style)FindResource(i==kind?"SourceSelectedOption":"SourceOption");}((FrameworkElement)credentials.Parent).Visibility=kind==1?Visibility.Visible:Visibility.Collapsed;((FrameworkElement)portal.Parent).Visibility=kind==2?Visibility.Visible:Visibility.Collapsed;browse.Visibility=kind==0?Visibility.Visible:Visibility.Collapsed;_sourceHints[address].Text=kind switch{1=>"http://sunucu.com:8080",2=>"http://portal.sunucu.com/c/",_=>"https://sunucu.com/liste.m3u veya dosya yolu"};}
        foreach(var (title,icon) in new[]{("M3U / TXT","playlist"),("Xtream Codes","server"),("Stalker Portal","globe")})
        {int index=buttons.Count;var b=new Button{Content=new IconLabel{Icon=icon,Label=title},Margin=new(0,0,index<2?11.2:0,0)};System.Windows.Automation.AutomationProperties.SetName(b,title);System.Windows.Input.KeyboardNavigation.SetTabIndex(b,index);b.Click+=(_,_)=>Select(index);b.PreviewKeyDown+=(_,e)=>{if(e.Key is System.Windows.Input.Key.Left or System.Windows.Input.Key.Right){int next=(index+(e.Key==System.Windows.Input.Key.Left?2:1))%3;Select(next);buttons[next].Focus();e.Handled=true;}};buttons.Add(b);Grid.SetColumn(b,index);types.Children.Add(b);}Select(kind);
        var error=new TextBlock{Text=""};error.VerticalAlignment=VerticalAlignment.Center;var actions=SourceFooter(error);actions.Children.Add(SourceAction("Vazgeç",Close));actions.Children.Add(SourceAction("Bağlan ve yükle",()=>
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
        actions.Children.OfType<Button>().Last().Margin=new Thickness(11.2,0,0,0);
    }
    private sealed class UniformGridCompat : Grid{internal UniformGridCompat(){for(int i=0;i<3;i++)ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});}}
}
