using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using WXPlayer.Core;

namespace WXPlayer.App;

internal static class Dialogs
{
    private static StackPanel Form(Window owner,string title,int width,out Window window)
    {
        window=new Window{Owner=owner,Title=title,Width=width,SizeToContent=SizeToContent.Height,MaxHeight=owner.ActualHeight,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false};
        var panel=new StackPanel{Margin=new Thickness(26)};window.Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        panel.Children.Add(new TextBlock{Text=title,FontSize=24,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,18)});return panel;
    }
    private static void Label(Panel p,string text)=>p.Children.Add(new TextBlock{Text=text,FontSize=12,Foreground=new SolidColorBrush(Color.FromRgb(160,177,197)),Margin=new Thickness(0,12,0,6)});
    private static TextBox Field(Panel panel,string label,string value=""){Label(panel,label);var t=new TextBox{Text=value};panel.Children.Add(t);return t;}
    private static Button Button(Panel panel,string text,RoutedEventHandler action,bool primary=false){var b=new Button{Content=text,Margin=new Thickness(0,16,0,0)};if(primary)b.Style=(Style)Application.Current.FindResource("Primary");b.Click+=action;panel.Children.Add(b);return b;}
    private static void Note(Panel panel,string text)=>panel.Children.Add(new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,Foreground=new SolidColorBrush(Color.FromRgb(146,163,183)),FontSize=11,LineHeight=18,Margin=new Thickness(0,12,0,0)});
    public static SourceConfig? Source(Window owner,SourceConfig? current=null)
    {
        var p=Form(owner,current is null?"Kütüphanenizi bağlayın":"Kaynağı düzenle",560,out var w);
        Note(p,"Oynatma listenizi veya sağlayıcınızın verdiği hesabı ekleyin. Hesap bilgileri bu Windows kullanıcısı için şifrelenir.");
        Label(p,"Bağlantı türü");var type=new ComboBox{ItemsSource=new[]{"M3U / M3U8 / TXT","Xtream Codes API","Stalker Portal"},SelectedIndex=(int)(current?.Kind??SourceKind.Playlist)};p.Children.Add(type);
        var name=Field(p,"Kaynak adı",current?.Name??"Kütüphanem");
        var address=Field(p,"Kaynak adresi · URL veya yerel dosya",current?.Address??"");
        var browse=Button(p,"Dosyadan seç…",(_,_)=>{var d=new OpenFileDialog{Filter="Oynatma listesi|*.m3u;*.m3u8;*.txt|Tüm dosyalar|*.*"};if(d.ShowDialog(w)==true)address.Text=d.FileName;});
        var credentials=new StackPanel();p.Children.Add(credentials);var user=Field(credentials,"Kullanıcı adı",current?.Username??"");Label(credentials,"Şifre");var password=new PasswordBox{Password=current?.Password??""};credentials.Children.Add(password);
        var macPanel=new StackPanel();p.Children.Add(macPanel);var mac=Field(macPanel,"Sağlayıcınızın tanımladığı MAC adresi",current?.Mac??"");Note(macPanel,"Örnek portal yolu: https://sunucu/stalker_portal/server/load.php. Portal.php uç noktası da girilebilir.");
        var epg=Field(p,"XMLTV rehber adresi veya dosyası (isteğe bağlı)",current?.EpgUrl??"");
        Note(p,"Xtream için get.php bağlantısını da yapıştırabilirsiniz; kullanıcı adı ve şifre otomatik ayrıştırılır. Boş XMLTV adresi kaynaktan keşfedilir.");
        var error=new TextBlock{Foreground=Brushes.LightSalmon,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0)};p.Children.Add(error);
        SourceConfig? result=null;
        void Sync(){credentials.Visibility=type.SelectedIndex==1?Visibility.Visible:Visibility.Collapsed;macPanel.Visibility=type.SelectedIndex==2?Visibility.Visible:Visibility.Collapsed;browse.Visibility=type.SelectedIndex==0?Visibility.Visible:Visibility.Collapsed;}type.SelectionChanged+=(_,_)=>Sync();Sync();
        Button(p,"Kaydet ve yükle  →",(_,_)=>
        {
            try
            {
                var s=current is null?new SourceConfig():current with{};s.Name=name.Text.Trim();s.Kind=(SourceKind)type.SelectedIndex;s.Address=address.Text.Trim();s.Username=user.Text.Trim();s.Password=password.Password;s.Mac=mac.Text.Trim();s.EpgUrl=epg.Text.Trim();
                if(s.Name.Length==0||s.Address.Length==0)throw new InvalidOperationException("Kaynak adı ve adresi gerekli.");
                if(!File.Exists(s.Address)||s.Kind!=SourceKind.Playlist)AddressPolicy.Http(s.Address);
                if(s.EpgUrl.Length>0&&!File.Exists(s.EpgUrl))AddressPolicy.Http(s.EpgUrl);
                if(s.Kind==SourceKind.Xtream)ProviderClient.ParseXtreamAddress(s);
                if(s.Kind==SourceKind.Stalker&&!System.Text.RegularExpressions.Regex.IsMatch(s.Mac,"^([0-9a-fA-F]{2}:){5}[0-9a-fA-F]{2}$"))throw new InvalidOperationException("Geçerli bir MAC adresi gerekli.");
                result=s;w.DialogResult=true;
            }catch(Exception ex){error.Text=ex is InvalidOperationException?ex.Message:"Adres bilgilerini kontrol edin.";}
        },true);
        return w.ShowDialog()==true?result:null;
    }
    public static bool Settings(Window owner,PlayerSettings settings)
    {
        var p=Form(owner,"Oynatıcı ayarları",540,out var w);
        var gpu=new CheckBox{Content="GPU donanım ivmesini kullan (D3D11VA)",IsChecked=settings.HardwareAcceleration};p.Children.Add(gpu);
        Label(p,"Windows video çıkışı");var output=new ComboBox{ItemsSource=new[]{"Direct3D 11 (önerilen)","Direct3D 9 (uyumluluk)","Otomatik"},SelectedIndex=settings.VideoOutput=="direct3d11"?0:settings.VideoOutput=="direct3d9"?1:2};p.Children.Add(output);
        var adaptive=new CheckBox{Content="Akıllı önbellek: kesintilerde sonraki yayının tamponunu artır",IsChecked=settings.AdaptiveCache};p.Children.Add(adaptive);
        var cache=Field(p,"Ağ önbelleği · milisaniye (200 – 10000)",settings.NetworkCacheMs.ToString());
        var folder=Field(p,"Kayıt klasörü",settings.RecordingFolder);
        Button(p,"Klasör seç…",(_,_)=>{var d=new OpenFolderDialog{Title="Kayıt klasörünü seçin"};if(d.ShowDialog(w)==true)folder.Text=d.FolderName;});
        Note(p,"Video çıkışı değişikliği yeniden başlatmada; donanım ve önbellek değişikliği sonraki oynatmada uygulanır. Kayıt, sağlayıcıda ikinci bir bağlantı kullanır.");
        Note(p,"Kısayollar: Space oynat/duraklat · ← / → 10 sn · ↑ / ↓ ses\nF tam ekran · M sessiz · Page Up / Down kanal · Ctrl+K arama · Esc çıkış\nVideo üzerinde fare tekerleği sesi değiştirir; liste üzerinde içerikleri kaydırır.");
        var error=new TextBlock{Foreground=Brushes.LightSalmon};p.Children.Add(error);
        Button(p,"Ayarları kaydet",(_,_)=>{if(!int.TryParse(cache.Text,out var n)||n<200||n>10000){error.Text="Önbellek 200 – 10000 aralığında olmalı.";return;}try{var full=Path.GetFullPath(folder.Text);if(full.Contains('\''))throw new ArgumentException();Directory.CreateDirectory(full);settings.RecordingFolder=full;}catch{error.Text="Kayıt klasörü geçerli ve yazılabilir olmalı.";return;}settings.NetworkCacheMs=n;settings.HardwareAcceleration=gpu.IsChecked==true;settings.AdaptiveCache=adaptive.IsChecked==true;settings.VideoOutput=output.SelectedIndex==0?"direct3d11":output.SelectedIndex==1?"direct3d9":"any";w.DialogResult=true;},true);
        return w.ShowDialog()==true;
    }
    public static ContentItem? Episode(Window owner,IReadOnlyList<ContentItem> episodes)
    {
        var p=Form(owner,"Bölümler",650,out var w);var list=new ListBox{ItemsSource=episodes,DisplayMemberPath="Name",Height=380};p.Children.Add(list);ContentItem? result=null;
        void Choose(){if(list.SelectedItem is ContentItem item){result=item;w.DialogResult=true;}}
        list.MouseDoubleClick+=(_,_)=>Choose();Button(p,"Seçili bölümü oynat",(_,_)=>Choose(),true);w.ShowDialog();return result;
    }
    public static void Tracks(Window owner,PlaybackEngine engine)
    {
        var p=Form(owner,"Ses ve altyazılar",490,out var w);
        Label(p,"Ses parçası");var audio=new ComboBox{ItemsSource=engine.Player.AudioTrackDescription,DisplayMemberPath="Name",SelectedValuePath="Id",SelectedValue=engine.Player.AudioTrack};p.Children.Add(audio);audio.SelectionChanged+=(_,_)=>{if(audio.SelectedValue is int id)engine.Player.SetAudioTrack(id);};
        Label(p,"Altyazı parçası");var sub=new ComboBox{ItemsSource=engine.Player.SpuDescription,DisplayMemberPath="Name",SelectedValuePath="Id",SelectedValue=engine.Player.Spu};p.Children.Add(sub);sub.SelectionChanged+=(_,_)=>{if(sub.SelectedValue is int id)engine.Player.SetSpu(id);};
        Button(p,"Harici altyazı yükle…",(_,_)=>{var d=new OpenFileDialog{Filter="Altyazı|*.srt;*.ass;*.ssa;*.vtt;*.sub"};if(d.ShowDialog(w)==true){if(engine.Player.AddSlave(LibVLCSharp.Shared.MediaSlaveType.Subtitle,new Uri(d.FileName).AbsoluteUri,true))w.Close();else MessageBox.Show(w,"Altyazı eklenemedi.","WX Player");}});
        Note(p,"Parça listeleri yayın başladıktan sonra görünür. Birden fazla parça için kaynağın bunları sunması gerekir.");Button(p,"Tamam",(_,_)=>w.Close(),true);w.ShowDialog();
    }
    public static (string Video,string Audio)? Capture(Window owner)
    {
        var p=Form(owner,"DirectShow aygıtını aç",500,out var w);Note(p,"Windows'taki kamera / yakalama aygıtının tam adını girin. Boş video adı varsayılan aygıtı seçer; 'none' bir girişi kapatır.");var video=Field(p,"Video aygıtı");var audio=Field(p,"Ses aygıtı","none");Button(p,"Aygıtı aç",(_,_)=>w.DialogResult=true,true);return w.ShowDialog()==true?(video.Text,audio.Text):null;
    }
}
