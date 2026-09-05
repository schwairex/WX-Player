using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed class SettingsWindow : PremiumWindow
{
    private readonly StackPanel _pages=new();
    private readonly List<(Button Button,StackPanel Page)> _tabs=[];
    internal bool Saved {get;private set;}
    internal SettingsWindow(Window owner,PlayerSettings settings,LibraryStore store,UpdateController updates,Func<SourceConfig,Task> edit,Func<string?,Task> remove,Func<LibraryCleanup,Task> clear):base(owner,"Ayarlar","İzleme deneyiminizi kendinize göre düzenleyin.","settings",850,760)
    {
        var grid=new Grid();grid.RowDefinitions.Add(new(){Height=GridLength.Auto});grid.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});Body.Children.Add(grid);
        var nav=new StackPanel{Orientation=Orientation.Horizontal,Margin=new(0,0,0,20)};grid.Children.Add(nav);
        var scroll=new ScrollViewer{Content=_pages,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetRow(scroll,1);grid.Children.Add(scroll);
        StackPanel Page(string name,string icon)
        {
            var page=new StackPanel();_pages.Children.Add(page);var button=new Button{Content=new IconLabel{Icon=icon,Label=name},Margin=new(0,0,10,0),Padding=new(16,10,16,10)};
            int index=_tabs.Count;button.Click+=(_,_)=>SelectTab(index);nav.Children.Add(button);_tabs.Add((button,page));return page;
        }
        var playback=Page("Oynatma","play");var library=Page("Kütüphane","library");var updatePage=Page("Güncellemeler","refresh");
        var video=Card(playback,"Görüntü ve performans","Donanım ve önbellek tercihleri sonraki yayında uygulanır.");
        var gpu=new CheckBox{Content="GPU donanım ivmesi",IsChecked=settings.HardwareAcceleration};video.Children.Add(gpu);
        var output=new ComboBox{ItemsSource=new[]{"Direct3D 11","Direct3D 9","Otomatik"},SelectedIndex=settings.VideoOutput=="direct3d11"?0:settings.VideoOutput=="direct3d9"?1:2};Row(video,"Windows video çıkışı",output);
        var adaptive=new CheckBox{Content="Akıllı ağ önbelleği",IsChecked=settings.AdaptiveCache};video.Children.Add(adaptive);
        var cache=new TextBox{Text=settings.NetworkCacheMs.ToString()};Row(video,"Önbellek · 200–10000 ms",cache);
        var fill=new CheckBox{Content="Tam ekranda görüntüyü doldur",IsChecked=settings.FullscreenFill};video.Children.Add(fill);
        var record=Card(playback,"Kayıtlar","Video çıkışı değişikliği yeniden başlatmada uygulanır.");
        var folder=new TextBox{Text=settings.RecordingFolder};record.Children.Add(folder);var browse=Action("Klasör seç",()=>{var d=new OpenFolderDialog();if(d.ShowDialog(this)==true)folder.Text=d.FolderName;});browse.HorizontalAlignment=HorizontalAlignment.Left;browse.Margin=new(0,12,0,0);record.Children.Add(browse);
        var keys=Card(playback,"Kısayollar");keys.Children.Add(Text("Space · oynat/duraklat     F · tam ekran     Z · sığdır/doldur\nM · sessiz     I · istatistikler     Ctrl+K · arama",12,"#98A3B6"));
        var sources=Card(library,"Bağlı kaynaklar","Kayıtlı kaynakları düzenleyin veya tek tek kaldırın.");
        var sourceRows=new StackPanel();sources.Children.Add(sourceRows);
        var message=Text("",12,"#C1EC8B");library.Children.Add(message);
        async Task Execute(Func<Task> task){IsEnabled=false;try{await task();await LoadSources();message.Text="İşlem tamamlandı.";}catch{message.Text="İşlem tamamlanamadı. Devam eden yüklemeyi bitirip tekrar deneyin.";}finally{IsEnabled=true;}}
        async Task LoadSources()
        {
            var items=await store.SourcesAsync();sourceRows.Children.Clear();
            if(items.Count==0)sourceRows.Children.Add(Text("Henüz kaynak eklenmedi.",12,"#98A3B6"));
            foreach(var source in items)
            {
                var row=new DockPanel{Margin=new(0,8,0,8)};var actions=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(actions,Dock.Right);row.Children.Add(actions);
                actions.Children.Add(Action("Düzenle",async()=>{var result=Dialogs.Source(this,source);if(result is not null)await Execute(()=>edit(result));}));
                actions.Children.Add(Action("Kaldır",async()=>{if(Confirm(this,"Kaynağı kaldır",source.Name+" ve bu kaynağın içerikleri kaldırılacak."))await Execute(()=>remove(source.Id));}));
                var text=Text(source.Name);text.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(text);sourceRows.Children.Add(row);
            }
        }
        var clean=Card(library,"Kütüphaneyi temizle","Bu işlemler seçtiğiniz kayıtları tüm kaynaklardan kaldırır.");
        foreach(var (label,kind,description) in new[]{("Favorileri temizle",LibraryCleanup.Favorites,"Tüm favori işaretleri kaldırılacak."),("Son izlenenleri temizle",LibraryCleanup.History,"İzleme geçmişiniz temizlenecek."),("Tüm kaynakları kaldır",LibraryCleanup.Sources,"Kaynaklar, kanal listeleri, rehber, favoriler ve izleme geçmişi kaldırılacak.")})
        {
            var b=Action(label,async()=>{if(Confirm(this,label,description))await Execute(()=>clear(kind));});b.Margin=new(0,5,0,5);b.HorizontalAlignment=HorizontalAlignment.Stretch;clean.Children.Add(b);
        }
        var update=Card(updatePage,"WX Player v"+UpdateController.Current.ToString(3),"Resmî depo · github.com/schwairex/WX-Player");
        var automatic=new CheckBox{Content="Yeni sürümleri otomatik kontrol et ve indir",IsChecked=settings.AutoUpdate};update.Children.Add(automatic);
        update.Children.Add(Text("Açılışta ve 4 saatte bir kontrol edilir. Yeniden başlatma sizin seçiminizle yapılır.",12,"#98A3B6"));
        var updateStatus=Text(updates.Status);updateStatus.Margin=new(0,18,0,16);update.Children.Add(updateStatus);
        void Changed()=>Dispatcher.BeginInvoke(()=>updateStatus.Text=updates.Status);updates.Changed+=Changed;Closed+=(_,_)=>updates.Changed-=Changed;
        var check=Action("Şimdi kontrol et",async()=>await updates.CheckAsync(true),true);check.Margin=new(0);check.HorizontalAlignment=HorizontalAlignment.Left;update.Children.Add(check);
        var error=Text("",12,"#FFAD9F");error.VerticalAlignment=VerticalAlignment.Center;Footer.Children.Add(error);Footer.Children.Add(Action("Kapat",Close));
        Footer.Children.Add(Action("Değişiklikleri kaydet",()=>
        {
            if(!int.TryParse(cache.Text,out int value)||value is <200 or >10000){error.Text="Önbellek: 200–10000 ms";SelectTab(0);return;}
            try{var full=Path.GetFullPath(folder.Text);if(full.Contains('\''))throw new ArgumentException();Directory.CreateDirectory(full);settings.RecordingFolder=full;}catch{error.Text="Kayıt klasörü geçersiz.";SelectTab(0);return;}
            settings.NetworkCacheMs=value;settings.HardwareAcceleration=gpu.IsChecked==true;settings.AdaptiveCache=adaptive.IsChecked==true;settings.FullscreenFill=fill.IsChecked==true;settings.AutoUpdate=automatic.IsChecked==true;settings.VideoOutput=output.SelectedIndex==0?"direct3d11":output.SelectedIndex==1?"direct3d9":"any";
            App.SaveSettings(settings);Saved=true;Close();
        },true));
        SelectTab(0);Loaded+=async(_,_)=>{try{await LoadSources();}catch{message.Text="Kaynaklar okunamadı.";}};
    }
    internal void SelectTab(int index){for(int i=0;i<_tabs.Count;i++){_tabs[i].Page.Visibility=i==index?Visibility.Visible:Visibility.Collapsed;_tabs[i].Button.Background=Brush(i==index?"#2D3D26":"#202734");_tabs[i].Button.Foreground=Brush(i==index?"#C1EC8B":"#A8B5C7");}}
}

