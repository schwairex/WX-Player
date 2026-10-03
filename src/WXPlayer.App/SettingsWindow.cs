using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed partial class SettingsWindow : PremiumWindow
{
    private readonly StackPanel _pages=new();
    private readonly List<(Button Button,StackPanel Page)> _tabs=[];
    internal bool Saved {get;private set;}
    internal SettingsWindow(Window owner,PlayerSettings settings,LibraryStore store,UpdateController updates,Func<SourceConfig,Task> edit,Func<string?,Task> remove,Func<LibraryCleanup,Task> clear):base(owner,"Ayarlar","İzleme deneyiminizi kendinize göre düzenleyin.","settings",850,760)
    {
        InitializeSettingsAppearance();
        var grid=new Grid();grid.ColumnDefinitions.Add(new(){Width=new GridLength(216)});grid.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});Body.Children.Add(grid);
        var nav=new StackPanel{Margin=new(14.4,17.6,14.4,0)};
        grid.Children.Add(new Border{BorderBrush=SettingsBrush("LiveLine"),BorderThickness=new(0,0,1,0),Child=nav});
        _settingsScroll.Content=_pages;Grid.SetColumn(_settingsScroll,1);grid.Children.Add(_settingsScroll);
        StackPanel Page(string name,string icon)
        {
            var page=new StackPanel();_pages.Children.Add(page);var button=new Button{Content=new IconLabel{Icon=icon,Label=name},Style=SettingsStyle("SettingsNav")};
            System.Windows.Automation.AutomationProperties.SetName(button,name);button.PreviewKeyDown+=SettingsNavKeyDown;
            int index=_tabs.Count;button.Click+=(_,_)=>SelectTab(index);nav.Children.Add(button);_tabs.Add((button,page));return page;
        }
        var playback=Page("Oynatma","play");var shortcuts=Page("Kısayollar","keyboard");var library=Page("Kütüphane","library");var online=Page("Çevrimiçi","globe");var updatePage=Page("Güncellemeler","refresh");
        var video=SettingsCard(playback,"Görüntü ve performans","Donanım ve önbellek tercihleri sonraki yayında uygulanır.");
        var gpu=new CheckBox{Content="GPU donanım ivmesi",IsChecked=settings.HardwareAcceleration};SettingsToggle(video,gpu);
        var output=new ComboBox{ItemsSource=new[]{"Direct3D 11","Direct3D 9","Otomatik"},SelectedIndex=settings.VideoOutput=="direct3d11"?0:settings.VideoOutput=="direct3d9"?1:2,Style=SettingsStyle("SettingsOutput"),Width=158};SettingsRow(video,"Windows video çıkışı",output);
        var adaptive=new CheckBox{Content="Akıllı ağ önbelleği",IsChecked=settings.AdaptiveCache};SettingsToggle(video,adaptive);
        var cache=new TextBox{Text=settings.NetworkCacheMs.ToString(),Style=SettingsStyle("SettingsInput"),Width=104,TextAlignment=TextAlignment.Right};SettingsRow(video,"Önbellek · 200–10000 ms",cache);
        var fill=new CheckBox{Content="Tam ekranda görüntüyü doldur",IsChecked=settings.FullscreenFill};SettingsToggle(video,fill);
        var viewing=SettingsCard(playback,"Mini oynatıcı ve yayın durumu","İzlerken görüntüyü küçük bir pencerede tutun; açtığınız canlı kanalın bağlantı durumunu izleyin.");
        var mini=new CheckBox{Content="Picture-in-Picture / Mini Player düğmesini göster",IsChecked=settings.MiniPlayerEnabled};SettingsToggle(viewing,mini,"Oynatıcıdaki küçük pencere düğmesi videoyu aynı oturumda taşır; oynatma konumunuz korunur.");
        var health=new CheckBox{Content="Canlı kanalın yayın sağlığını otomatik kontrol et",IsChecked=settings.ChannelHealthCheck};SettingsToggle(viewing,health,"Açılan kanalın gerçekten oynatılıp oynatılmadığı izlenir. Yeni bağlantı veya hesap sorgusu yapılmaz.");
        var social=SettingsCard(online,"Discord Rich Presence","Açıldığında izlediğiniz içerik adı ve ilerlemesi Discord profilinizde görünür. Kaynak ve hesap bilgileri paylaşılmaz.");
        var discord=new CheckBox{Content="Discord Rich Presence",IsChecked=settings.DiscordRichPresence};
        System.Windows.Automation.AutomationProperties.SetName(discord,"Discord Rich Presence");SettingsToggle(social,discord,"Discord masaüstü uygulaması gerekir. Değişiklik kaydedildiğinde uygulanır.");
        var record=SettingsCard(playback,"Kayıtlar","Video çıkışı değişikliği yeniden başlatmada uygulanır.");
        var folder=new TextBox{Text=settings.RecordingFolder,Style=SettingsStyle("SettingsInput")};
        var path=new Grid{Margin=new(17.6,15.2,17.6,15.2)};path.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});path.ColumnDefinitions.Add(new(){Width=GridLength.Auto});path.Children.Add(folder);System.Windows.Automation.AutomationProperties.SetName(folder,"Kayıtlar");
        var browse=SettingsAction("Klasör seç",()=>{var d=new OpenFolderDialog();if(d.ShowDialog(this)==true)folder.Text=d.FolderName;});browse.Margin=new(9.6,0,0,0);Grid.SetColumn(browse,1);path.Children.Add(browse);record.Children.Add(path);
        var keys=SettingsCard(shortcuts,"Klavye ile daha hızlı","Sık kullandığınız kontroller elinizin altında.");var keyGrid=_settingsKeys=new Grid{Margin=new(17.6,6.4,17.6,6.4)};keyGrid.ColumnDefinitions.Add(new());keyGrid.ColumnDefinitions.Add(new(){Width=new GridLength(25.6)});keyGrid.ColumnDefinitions.Add(new());keys.Children.Add(keyGrid);int keyIndex=0;
        foreach(var (key,label) in new[]{("Space","Oynat / duraklat"),("F / Esc","Tam ekran / çıkış"),("P","Mini oynatıcı"),("← / →","10 saniye geri / ileri"),("↑ / ↓","Ses seviyesi"),("M","Sesi kapat / aç"),("Z","Görüntüyü sığdır / doldur"),("I","Yayın istatistikleri"),("Ctrl + K","Kütüphanede ara"),("Ctrl + B","Menüyü daralt / genişlet"),("PgUp / PgDn","Önceki / sonraki kanal")})
        {
            SettingsShortcut(keyGrid,keyIndex++,key,label);
        }        var sources=SettingsCard(library,"Bağlı kaynaklar","Kayıtlı kaynakları düzenleyin veya tek tek kaldırın.");
        var sourceRows=new StackPanel();sources.Children.Add(sourceRows);
        var message=Text("",12,"#C5F27A");library.Children.Add(message);
        async Task Execute(Func<Task> task){IsEnabled=false;try{await task();await LoadSources();message.Text="İşlem tamamlandı.";}catch{message.Text="İşlem tamamlanamadı. Devam eden yüklemeyi bitirip tekrar deneyin.";}finally{IsEnabled=true;}}
        async Task LoadSources()
        {
            var items=await store.SourcesAsync();sourceRows.Children.Clear();
            if(items.Count==0)sourceRows.Children.Add(new Border{Padding=new(32),Child=Text("Henüz kaynak eklenmedi.",12,"#8F9996")});
            foreach(var source in items)
            {
                var row=new DockPanel{Margin=new(17.6,12.8,17.6,12.8)};var actions=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(actions,Dock.Right);row.Children.Add(actions);
                var editButton=SettingsAction("Düzenle",async()=>{var result=Dialogs.Source(this,source);if(result is not null)await Execute(()=>edit(result));},small:true);actions.Children.Add(editButton);
                var removeButton=SettingsAction("Kaldır",async()=>{if(Confirm(this,"Kaynağı kaldır",source.Name+" ve bu kaynağın içerikleri kaldırılacak."))await Execute(()=>remove(source.Id));},danger:true);removeButton.Height=33.6;removeButton.FontSize=12.8;removeButton.Margin=new(6.4,0,0,0);actions.Children.Add(removeButton);
                var logo=new Border{Width=38.4,Height=38.4,CornerRadius=new(11.2),Background=SettingsBrush("LiveHover"),Margin=new(0,0,14.4,0),Child=new SvgIcon("library"){Width=20,Height=20,Foreground=SettingsBrush("LiveMuted")}};DockPanel.SetDock(logo,Dock.Left);row.Children.Add(logo);
                var text=Text(source.Name);text.FontWeight=FontWeights.SemiBold;text.TextWrapping=TextWrapping.NoWrap;text.TextTrimming=TextTrimming.CharacterEllipsis;text.ToolTip=source.Name;text.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(text);sourceRows.Children.Add(new Border{BorderBrush=SettingsBrush("LiveLine"),BorderThickness=new(0,sourceRows.Children.Count==0?0:1,0,0),Child=row});
            }
        }
        var artwork=SettingsCard(online,"Eksik afişler ve logolar","Yalnız içerik adı/yılı ile arama yapılır; hesap ve yayın adresleri paylaşılmaz. Eşleşen görseller önbellekte tutulur.");
        var discover=new CheckBox{Content="Eksik görselleri internetten otomatik tamamla",IsChecked=settings.DiscoverArtwork};SettingsToggle(artwork,discover,"Diziler: TVmaze · Film/dizi: Wikipedia · Kanallar: IPTV-org / Wikipedia. Kesin eşleşme bulunamazsa görsel eklenmez.");
        var links=new WrapPanel{Margin=new(17.6,0,17.6,14.4)};artwork.Children.Add(links);
        foreach(var (label,url) in new[]{("TVmaze · CC BY-SA","https://www.tvmaze.com/api#licensing"),("Wikipedia · Görsel kaynakları","https://www.wikipedia.org/"),("IPTV-org · Kanal verileri","https://github.com/iptv-org/database")})
        {var link=SettingsAction(label,()=>{try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url){UseShellExecute=true});}catch{}},small:true);link.Foreground=SettingsBrush("LiveMuted");link.Margin=new Thickness(0,6.4,8,0);links.Children.Add(link);}
        var clean=SettingsCard(library,"Kütüphaneyi temizle","Bu işlemler seçtiğiniz kayıtları tüm kaynaklardan kaldırır.");
        foreach(var (label,kind,description) in new[]{("Favorileri temizle",LibraryCleanup.Favorites,"Tüm favori işaretleri kaldırılacak."),("Son izlenenleri temizle",LibraryCleanup.History,"İzleme geçmişiniz ve kayıtlı bölüm/dakika bilgileriniz temizlenecek."),("Tüm kaynakları kaldır",LibraryCleanup.Sources,"Kaynaklar, kanal listeleri, rehber, favoriler ve izleme geçmişi kaldırılacak.")})
        {
            var b=SettingsAction(label,async()=>{if(Confirm(this,label,description))await Execute(()=>clear(kind));},danger:true);b.HorizontalAlignment=HorizontalAlignment.Right;SettingsRow(clean,null,b,description);
        }
        var update=SettingsCard(updatePage,"WX Player v"+UpdateController.Current.ToString(3),"Resmî depo · github.com/schwairex/WX-Player");
        var automatic=new CheckBox{Content="Yeni sürümleri otomatik kontrol et ve indir",IsChecked=settings.AutoUpdate};SettingsToggle(update,automatic,"Açılışta ve 4 saatte bir kontrol edilir. Yeniden başlatma sizin seçiminizle yapılır.");
        var updateStatus=Text(updates.Status);updateStatus.Foreground=SettingsBrush("LiveMuted");
        void Changed()=>Dispatcher.BeginInvoke(()=>updateStatus.Text=updates.Status);updates.Changed+=Changed;Closed+=(_,_)=>updates.Changed-=Changed;
        var check=SettingsAction("Şimdi kontrol et",async()=>await updates.CheckAsync(true),true);check.HorizontalAlignment=HorizontalAlignment.Right;var updateRow=SettingsRow(update,null,check);updateRow.Children.RemoveAt(0);updateStatus.Margin=new(0,0,12,0);updateRow.Children.Insert(0,updateStatus);
        var error=Text("",12,"#FF8C98");error.MaxWidth=270;error.VerticalAlignment=VerticalAlignment.Center;Footer.Children.Add(error);var close=SettingsAction("Kapat",Close);close.Margin=new(11.2,0,0,0);Footer.Children.Add(close);
        var save=SettingsAction("Değişiklikleri kaydet",()=>
        {
            if(!int.TryParse(cache.Text,out int value)||value is <200 or >10000){error.Text="Önbellek: 200–10000 ms";SelectTab(0);return;}
            try{var full=Path.GetFullPath(folder.Text);if(full.Contains('\''))throw new ArgumentException();Directory.CreateDirectory(full);settings.RecordingFolder=full;}catch{error.Text="Kayıt klasörü geçersiz.";SelectTab(0);return;}
            settings.NetworkCacheMs=value;settings.HardwareAcceleration=gpu.IsChecked==true;settings.AdaptiveCache=adaptive.IsChecked==true;settings.FullscreenFill=fill.IsChecked==true;settings.AutoUpdate=automatic.IsChecked==true;settings.VideoOutput=output.SelectedIndex==0?"direct3d11":output.SelectedIndex==1?"direct3d9":"any";
            settings.DiscoverArtwork=discover.IsChecked==true;settings.DiscordRichPresence=discord.IsChecked==true;settings.MiniPlayerEnabled=mini.IsChecked==true;settings.ChannelHealthCheck=health.IsChecked==true;App.SaveSettings(settings);Saved=true;Close();
        },true);save.Margin=new(11.2,0,0,0);Footer.Children.Add(save);
        SelectTab(0);Loaded+=async(_,_)=>{try{await LoadSources();}catch{message.Text="Kaynaklar okunamadı.";}};
    }
    internal void SmokeShowShortcuts(){SelectTab(1);_tabs[1].Page.Children.OfType<Border>().Last().BringIntoView();}
    internal void SelectTab(int index){for(int i=0;i<_tabs.Count;i++){_tabs[i].Page.Visibility=i==index?Visibility.Visible:Visibility.Collapsed;NavigationVisual.SetIsSelected(_tabs[i].Button,i==index);}_settingsScroll.ScrollToTop();}
}

