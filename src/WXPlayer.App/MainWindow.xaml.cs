using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using WXPlayer.Core;

namespace WXPlayer.App;

public partial class MainWindow : Window
{
    private readonly LibraryStore _store=new(Path.Combine(App.DataDirectory,"library.db"));
    private readonly ProviderClient _providers=new();
    private readonly PlayerSettings _settings=App.ReadSettings();
    private readonly CancellationTokenSource _life=new();
    private PlaybackEngine _engine=null!;
    private CancellationTokenSource? _load,_query,_play,_epg;
    private readonly DispatcherTimer _search=new(){Interval=TimeSpan.FromMilliseconds(250)};
    private readonly DispatcherTimer _clock=new(){Interval=TimeSpan.FromSeconds(1)};
    private List<SourceConfig> _sources=[];
    private ContentItem? _current;
    private PlaybackTarget? _target;
    private bool _ready,_suppress,_closing,_closed,_fullscreen;
    private WindowState _previousState;
    private string _section="home";
    private int _offset,_total,_playVersion,_epgVersion,_viewVersion;
    private DateTimeOffset _guideDay=new(DateTime.Today);
    private int _tick;
    private SourceConfig? SelectedSource=>SourcePicker.SelectedItem as SourceConfig;
    private ContentKind? FilterKind=>_section switch{"live" or "epg"=>ContentKind.Live,"movie"=>ContentKind.Movie,"series"=>ContentKind.Series,_=>null};

    public MainWindow()
    {
        InitializeComponent();
        Loaded+=async(_,_)=>await InitializeAsync();
        SourceInitialized+=(_,_)=>{try{int dark=1;DwmSetWindowAttribute(new WindowInteropHelper(this).Handle,20,ref dark,sizeof(int));}catch{/* Older Windows falls back to the system title bar. */}};
        _search.Tick+=async(_,_)=>{_search.Stop();_offset=0;await SafeAsync(QueryAsync);};
        _clock.Tick+=(_,_)=>Tick();
    }
    [DllImport("dwmapi.dll")]private static extern int DwmSetWindowAttribute(IntPtr hwnd,int attr,ref int value,int size);
    private async Task InitializeAsync()
    {
        await SafeAsync(async()=>
        {
            await _store.InitializeAsync();
            _engine=new PlaybackEngine(_settings);Video.MediaPlayer=_engine.Player;
            _engine.Player.Playing+=(_,_)=>Ui(()=>{PlaybackBadge.Text="OYNATILIYOR";PlayButton.Content="Ⅱ";OverlayText.Text=_current?.Name??"WX PLAYER";});
            _engine.Player.Paused+=(_,_)=>Ui(()=>{PlaybackBadge.Text="DURAKLATILDI";PlayButton.Content="▶";});
            _engine.Player.EndReached+=(_,_)=>Ui(()=>{PlaybackBadge.Text="YAYIN BİTTİ";PlayButton.Content="▶";});
            _engine.Player.EncounteredError+=(_,_)=>Ui(()=>{PlaybackBadge.Text="BAĞLANTI HATASI";Status("Yayın açılamadı. Adres / hesap / bağlantı sınırını kontrol edin; ▶ ile tekrar deneyin.");PlayButton.Content="▶";});
            _engine.RecordingFailed+=message=>Ui(async()=>{await SafeAsync(async()=>{await _engine.StopRecordingAsync();UpdateRecordButton();Status(message);});});
            VolumeSlider.Value=_settings.Volume;_ready=true;await ReloadSourcesAsync();await RefreshViewAsync();SetNav();_clock.Start();
            if(App.Arguments.Contains("--smoke"))await SmokeTest.RunAsync(this,_store,_engine,_providers,_settings);
        });
    }
    private void Ui(Action action){if(!_closing)Dispatcher.BeginInvoke(()=>{if(!_closing)action();});}
    private void Status(string message){StatusText.Text=message;StatusText.ToolTip=message;}
    private async Task SafeAsync(Func<Task> action)
    {
        try{await action();}catch(OperationCanceledException){if(!_closing)Status("İşlem iptal edildi.");}
        catch(Exception ex)
        {
            if(_closing)return;
            string message=ex switch{InvalidOperationException=>ex.Message,HttpRequestException h when h.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden=>"Erişim reddedildi. Sağlayıcı bilgilerini ve hesap durumunu kontrol edin.",HttpRequestException=>"Sunucuya ulaşılamadı veya sunucu isteği reddetti.",System.Text.Json.JsonException=>"Sağlayıcı beklenen JSON verisini döndürmedi. API adresini kontrol edin.",System.Xml.XmlException=>"XMLTV dosyası geçerli değil veya güvenli ayrıştırma sınırını aşıyor.",UnauthorizedAccessException=>"Dosya veya klasör için erişim izni yok.",IOException=>"Dosya okunamadı / yazılamadı. Konumu ve boş disk alanını kontrol edin.",_=>"İşlem tamamlanamadı. Kaynak bilgilerini kontrol edip yeniden deneyin."};
            Status(message);File.AppendAllText(Path.Combine(App.DataDirectory,"errors.log"),$"{DateTimeOffset.UtcNow:O} {ex.GetType().Name}\n");
        }
    }
    private void SetBusy(bool busy)
    {BusyBar.Visibility=CancelButton.Visibility=busy?Visibility.Visible:Visibility.Collapsed;AddSourceButton.IsEnabled=!busy;SourcePicker.IsEnabled=!busy;}
    private async Task ReloadSourcesAsync(string? select=null)
    {
        var id=select??SelectedSource?.Id;_sources=await _store.SourcesAsync();_suppress=true;SourcePicker.ItemsSource=_sources;SourcePicker.SelectedItem=_sources.FirstOrDefault(s=>s.Id==id)??_sources.FirstOrDefault();_suppress=false;
    }
    private async Task RefreshViewAsync()
    {
        if(!_ready)return;var id=SelectedSource?.Id;int version=++_viewVersion;
        var statsTask=_store.StatsAsync(id);var categoriesTask=_store.CategoriesAsync(id,FilterKind);await Task.WhenAll(statsTask,categoriesTask);
        if(version!=_viewVersion)return;
        var stats=await statsTask;LiveCount.Text=stats.Live.ToString("N0");MovieCount.Text=stats.Movies.ToString("N0");SeriesCount.Text=stats.Series.ToString("N0");FavoriteCount.Text=stats.Favorites.ToString("N0");
        string? old=CategoryPicker.SelectedItem as string;var cats=await categoriesTask;_suppress=true;CategoryPicker.ItemsSource=cats;CategoryPicker.SelectedItem=cats.Contains(old??"")?old:cats[0];_suppress=false;await QueryAsync();
        if(SelectedSource?.UpdatedAt is{} time)Status($"●  Yerel kütüphane hazır · Son güncelleme: {time.LocalDateTime:dd MMM HH:mm}");
    }
    private async Task QueryAsync()
    {
        if(!_ready)return;_query?.Cancel();_query?.Dispose();var cts=_query=CancellationTokenSource.CreateLinkedTokenSource(_life.Token);
        try
        {
            var page=await _store.QueryAsync(SelectedSource?.Id,FilterKind,CategoryPicker.SelectedIndex>0?CategoryPicker.SelectedItem as string:null,SearchBox.Text.Trim(),_section=="favorites",_section=="recent",_offset,150,cts.Token);
            if(cts.IsCancellationRequested)return;
            _total=page.Total;_suppress=true;ChannelList.ItemsSource=page.Items;ChannelList.SelectedItem=page.Items.FirstOrDefault(x=>x.Id==_current?.Id);_suppress=false;
            EmptyList.Visibility=page.Items.Count==0?Visibility.Visible:Visibility.Collapsed;
            EmptyTitle.Text=_sources.Count==0?"İlk kaynağınızı ekleyin":"Burada henüz içerik yok";
            EmptyDescription.Text=_sources.Count==0?"M3U, Xtream veya Stalker ile tüm içeriklerinize tek yerden ulaşın.":"Aramayı veya kategori filtresini değiştirin. Favoriler için bir içeriğin yıldızına dokunun.";
            DemoButton.Visibility=_sources.Count==0?Visibility.Visible:Visibility.Collapsed;
            ResultsCount.Text=$"{page.Total:N0} içerik";PageLabel.Text=page.Total==0?"0 içerik":$"{_offset+1:N0}–{Math.Min(_offset+150,page.Total):N0} / {page.Total:N0}";
            PrevPage.IsEnabled=_offset>0;NextPage.IsEnabled=_offset+150<page.Total;
        }catch(Exception) when(cts.IsCancellationRequested){/* A newer query owns the view. */}
    }
    private async void AddSource_Click(object sender,RoutedEventArgs e)
    {if(_load is not null)return;var source=Dialogs.Source(this);if(source is not null)await ImportSourceAsync(source);}
    private async Task ImportSourceAsync(SourceConfig source)
    {
        if(_load is not null)return;_load=CancellationTokenSource.CreateLinkedTokenSource(_life.Token);SetBusy(true);Status("Kaynak bağlanıyor…");
        try{await SafeAsync(async()=>{var progress=new Progress<ImportProgress>(p=>Status(p.Message));int count=await _store.ImportAsync(source,_providers.LoadAsync(source,_load.Token),progress,_load.Token);await ReloadSourcesAsync(source.Id);_offset=0;await RefreshViewAsync();Status($"✓  {count:N0} içerik yüklendi. Rehber için yayın akışındaki ↻ düğmesini kullanın.");});}
        finally{_load?.Dispose();_load=null;SetBusy(false);}
    }
    private async void Refresh_Click(object sender,RoutedEventArgs e){if(SelectedSource is{} s)await ImportSourceAsync(s with{});else AddSource_Click(sender,e);}
    private void ManageSource_Click(object sender,RoutedEventArgs e)
    {
        var menu=new ContextMenu();var edit=new MenuItem{Header="Kaynağı düzenle"};edit.Click+=async(_,_)=>{if(SelectedSource is{} s&&_load is null&&Dialogs.Source(this,s) is{} updated)await ImportSourceAsync(updated);};menu.Items.Add(edit);
        var delete=new MenuItem{Header="Kaynağı kütüphaneden kaldır"};delete.Click+=async(_,_)=>{if(SelectedSource is not{} s||_load is not null)return;if(MessageBox.Show(this,$"'{s.Name}' kaynağı ve bu kaynağın favorileri kaldırılsın mı?","Kaynağı kaldır",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;await SafeAsync(async()=>{await _store.DeleteSourceAsync(s.Id);await ReloadSourcesAsync();_offset=0;await RefreshViewAsync();});};menu.Items.Add(delete);
        menu.Items.Add(new Separator());var direct=new MenuItem{Header="DirectShow yakalama aygıtını aç…"};direct.Click+=async(_,_)=>{if(Dialogs.Capture(this) is{} capture)await SafeAsync(async()=>{_play?.Cancel();_current=null;_target=null;ShowVideo();NowTitle.Text="DirectShow · "+(capture.Video.Length>0?capture.Video:"Varsayılan aygıt");await _engine.PlayCaptureAsync(capture.Video,capture.Audio,_settings);});};menu.Items.Add(direct);menu.IsOpen=true;
    }
    private async void Demo_Click(object sender,RoutedEventArgs e)
    {
        var path=Path.Combine(AppContext.BaseDirectory,"samples","open-films.m3u");await ImportSourceAsync(new SourceConfig{Id="wx-open-films",Name="Örnek · Açık filmler",Address=path});
    }
    private async void Source_Changed(object sender,SelectionChangedEventArgs e){if(_suppress||!_ready)return;_offset=0;await SafeAsync(RefreshViewAsync);}
    private async void Category_Changed(object sender,SelectionChangedEventArgs e){if(_suppress||!_ready)return;_offset=0;await SafeAsync(QueryAsync);}
    private void Search_Changed(object sender,TextChangedEventArgs e){if(SearchHint is not null)SearchHint.Visibility=SearchBox.Text.Length==0?Visibility.Visible:Visibility.Collapsed;if(!_ready)return;_search.Stop();_search.Start();}
    private async void Navigate_Click(object sender,RoutedEventArgs e)
    {
        _section=(string)((Button)sender).Tag;_offset=0;_suppress=true;CategoryPicker.SelectedIndex=0;_suppress=false;SetNav();await SafeAsync(RefreshViewAsync);
    }
    private void SetNav()
    {
        foreach(var b in new[]{HomeNav,LiveNav,MovieNav,SeriesNav,FavoriteNav,EpgNav,RecentNav}){bool selected=(string)b.Tag==_section;b.Background=selected?new SolidColorBrush(Color.FromRgb(39,52,34)):Brushes.Transparent;b.Foreground=selected?(Brush)FindResource("Accent"):new SolidColorBrush(Color.FromRgb(171,185,204));}
        PageTitle.Text=_section switch{"live"=>"Canlı TV","movie"=>"Filmler","series"=>"Diziler","favorites"=>"Favorilerim","epg"=>"Program rehberi","recent"=>"Son izlenenler",_=>"Ana sayfa"};
        ListTitle.Text=_section=="home"?"Keşfetmeye başlayın":PageTitle.Text;GuideRow.Height=_section=="epg"?new GridLength(1,GridUnitType.Star):new GridLength(190);
    }
    private async void Channel_Selected(object sender,SelectionChangedEventArgs e){if(_suppress||!_ready)return;if(ChannelList.SelectedItem is ContentItem item)await SafeAsync(()=>PlayItemAsync(item));}
    private async void Channel_DoubleClick(object sender,MouseButtonEventArgs e){if(ChannelList.SelectedItem is ContentItem item&&item.Id==_current?.Id&&!_engine.Player.IsPlaying)await SafeAsync(()=>PlayItemAsync(item));}
    private async Task PlayItemAsync(ContentItem item)
    {
        _play?.Cancel();_play?.Dispose();var cts=_play=CancellationTokenSource.CreateLinkedTokenSource(_life.Token);int version=++_playVersion;
        var source=_sources.FirstOrDefault(s=>s.Id==item.SourceId);if(source is null)return;
        var historyId=item.Id;
        try
        {
            Status("Yayın hazırlanıyor…");
            if(item.Kind==ContentKind.Series&&source.Kind!=SourceKind.Playlist)
            {var episodes=await _providers.EpisodesAsync(source,item,cts.Token);if(cts.IsCancellationRequested)return;if(episodes.Count==0){Status("Bu dizi için bölüm listesi alınamadı. Sağlayıcının API desteğini kontrol edin.");return;}var selected=Dialogs.Episode(this,episodes);if(selected is null)return;item=selected;}
            var target=await _providers.ResolveAsync(source,item,cts.Token);if(version!=_playVersion)return;
            _current=item;_target=target;NowTitle.Text=item.Name;OverlayText.Text=item.Name;PlaybackBadge.Text="BAĞLANIYOR";ShowVideo();
            await _engine.PlayAsync(target,_settings,cts.Token);if(version!=_playVersion)return;await _store.RememberAsync(historyId);Status($"{item.Name} · Tampon {_engine.CacheMs(_settings)} ms · F: tam ekran");_guideDay=new(DateTime.Today);await LoadGuideAsync();
        }catch(OperationCanceledException){/* A later channel selection wins. */}
    }
    private void ShowVideo(){WelcomePanel.Visibility=Visibility.Collapsed;Video.Visibility=Visibility.Visible;}
    private async void Favorite_Click(object sender,RoutedEventArgs e)
    {
        e.Handled=true;if(((Button)sender).Tag is not ContentItem item)return;await SafeAsync(async()=>{bool value=!item.IsFavorite;await _store.FavoriteAsync(item.Id,value);item.IsFavorite=value;_suppress=true;ChannelList.Items.Refresh();_suppress=false;var stats=await _store.StatsAsync(SelectedSource?.Id);FavoriteCount.Text=stats.Favorites.ToString("N0");if(_section=="favorites")await QueryAsync();});
    }
    private async void PrevPage_Click(object sender,RoutedEventArgs e){_offset=Math.Max(0,_offset-150);await SafeAsync(QueryAsync);}
    private async void NextPage_Click(object sender,RoutedEventArgs e){if(_offset+150<_total)_offset+=150;await SafeAsync(QueryAsync);}
    private async Task LoadGuideAsync()
    {
        _epg?.Cancel();_epg?.Dispose();var cts=_epg=CancellationTokenSource.CreateLinkedTokenSource(_life.Token);int version=++_epgVersion;GuideDate.Text=_guideDay.LocalDateTime.Date==DateTime.Today?"BUGÜN":_guideDay.LocalDateTime.ToString("dd MMM");
        EpgList.ItemsSource=null;EpgEmpty.Visibility=Visibility.Visible;
        var item=_current;if(item is null||item.Kind!=ContentKind.Live){EpgEmpty.Text="Program rehberi canlı kanallarda görüntülenir.\n*Tekrar izleme, sağlayıcının Catch-Up desteğine bağlıdır.";return;}
        try
        {
            var programmes=await _store.EpgAsync(item,_guideDay);if(cts.IsCancellationRequested||version!=_epgVersion)return;
            if(programmes.Count==0&&_guideDay.LocalDateTime.Date==DateTime.Today&&_sources.FirstOrDefault(s=>s.Id==item.SourceId) is{} source)
                programmes=await _providers.ShortEpgAsync(source,item,cts.Token);
            if(cts.IsCancellationRequested||version!=_epgVersion)return;
            EpgList.ItemsSource=programmes;EpgEmpty.Visibility=programmes.Count==0?Visibility.Visible:Visibility.Collapsed;EpgEmpty.Text="Bu kanal / gün için rehber verisi yok.\nXMLTV adresini kaynağa ekleyip ↻ ile güncelleyin.";
            if(programmes.FirstOrDefault(p=>p.IsNow) is{} current)EpgList.ScrollIntoView(current);
        }catch(OperationCanceledException){}catch{if(version==_epgVersion)EpgEmpty.Text="Rehber alınamadı. XMLTV veya sağlayıcı erişimini kontrol edin.";}
    }
    private async void RefreshEpg_Click(object sender,RoutedEventArgs e)
    {
        if(_load is not null)return;var source=_current is null?SelectedSource:_sources.FirstOrDefault(s=>s.Id==_current.SourceId);if(source is null){Status("Önce bir kaynak ekleyin.");return;}
        _load=CancellationTokenSource.CreateLinkedTokenSource(_life.Token);SetBusy(true);Status("XMLTV rehberi arka planda yükleniyor…");
        try{await SafeAsync(async()=>{var count=await _providers.LoadEpgAsync(source,_store,_load.Token);await LoadGuideAsync();Status($"✓  {count:N0} program rehbere kaydedildi.");});}finally{_load?.Dispose();_load=null;SetBusy(false);}
    }
    private async void PreviousDay_Click(object sender,RoutedEventArgs e){_guideDay=_guideDay.AddDays(-1);await SafeAsync(LoadGuideAsync);}
    private async void NextDay_Click(object sender,RoutedEventArgs e){_guideDay=_guideDay.AddDays(1);await SafeAsync(LoadGuideAsync);}
    private async void Epg_DoubleClick(object sender,MouseButtonEventArgs e)
    {
        if(_current is null||EpgList.SelectedItem is not Programme p)return;var source=_sources.First(s=>s.Id==_current.SourceId);
        await SafeAsync(async()=>{var target=ProviderClient.CatchupTarget(source,_current,p);_play?.Cancel();_target=target;NowTitle.Text=_current.Name+" · "+p.Title;await _engine.PlayAsync(target,_settings,_life.Token);Status("Tekrar izleme · "+p.Title);});
    }
    private async void PlayPause_Click(object sender,RoutedEventArgs e)
    {
        if(!_ready)return;if(_engine.Player.IsPlaying||_engine.Player.State==LibVLCSharp.Shared.VLCState.Paused)_engine.Player.Pause();else if(_target is not null)await SafeAsync(()=>_engine.PlayAsync(_target,_settings,_life.Token));else if(ChannelList.SelectedItem is ContentItem i)await SafeAsync(()=>PlayItemAsync(i));
    }
    private async void Record_Click(object sender,RoutedEventArgs e)
    {
        if(!_ready)return;await SafeAsync(async()=>
        {
            RecordButton.IsEnabled=false;
            try{if(_engine.Recording){string? path=await _engine.StopRecordingAsync();Status(path is not null&&File.Exists(path)&&new FileInfo(path).Length>0?"✓  Kayıt kaydedildi: "+path:"Kayıt dosyası oluşmadı; sağlayıcının bağlantı sınırını kontrol edin.");}
            else{if(_target is null||!_engine.Player.IsPlaying){Status("Kayda başlamak için önce bir yayın oynatın.");return;}await _engine.StartRecordingAsync(_target,_current?.Name??"Yayın",_settings);Status("●  Kayıt başladı. Kanal değiştirdiğinizde kayıt ilk yayında devam eder.");}UpdateRecordButton();}
            finally{RecordButton.IsEnabled=true;}
        });
    }
    private void UpdateRecordButton(){RecordButton.Content=_engine.Recording?"■":"●";RecordButton.ToolTip=_engine.Recording?"Kaydı bitir":"Kaydı başlat";}
    private void Tracks_Click(object sender,RoutedEventArgs e){if(_ready)Dialogs.Tracks(this,_engine);}
    private void Settings_Click(object sender,RoutedEventArgs e){if(Dialogs.Settings(this,_settings)){App.SaveSettings(_settings);Status("Ayarlar kaydedildi.");}}
    private void Recordings_Click(object sender,RoutedEventArgs e){try{Directory.CreateDirectory(_settings.RecordingFolder);Process.Start(new ProcessStartInfo{FileName=_settings.RecordingFolder,UseShellExecute=true});}catch{Status("Kayıt klasörü açılamadı.");}}
    private void Volume_Changed(object sender,RoutedPropertyChangedEventArgs<double> e){if(VolumeLabel is not null)VolumeLabel.Text=((int)e.NewValue).ToString();_settings.Volume=(int)e.NewValue;if(_engine is not null)_engine.Player.Volume=_settings.Volume;}
    private void Mute_Click(object sender,RoutedEventArgs e){if(!_ready)return;_engine.Player.Mute=!_engine.Player.Mute;MuteButton.Content=_engine.Player.Mute?"×":"♪";}
    private void Video_MouseWheel(object sender,MouseWheelEventArgs e){VolumeSlider.Value=Math.Clamp(VolumeSlider.Value+(e.Delta>0?5:-5),0,100);e.Handled=true;}
    private void Video_Click(object sender,MouseButtonEventArgs e){if(e.ClickCount==2)ToggleFullscreen();else Focus();}
    private void Seek_Released(object sender,MouseButtonEventArgs e){if(_ready&&_engine.Player.IsSeekable)_engine.Player.Position=(float)SeekSlider.Value;}
    private void Seek(long ms){if(_engine.Player.IsSeekable)_engine.Player.Time=Math.Clamp(_engine.Player.Time+ms,0,Math.Max(0,_engine.Player.Length));else Status("Bu canlı yayın ileri / geri sarmayı desteklemiyor. Geçmiş programlar için Catch-Up kullanın.");}
    private void PreviousChannel_Click(object sender,RoutedEventArgs e)=>ChangeChannel(-1);
    private void NextChannel_Click(object sender,RoutedEventArgs e)=>ChangeChannel(1);
    private async void ChangeChannel(int delta)
    {
        if(ChannelList.Items.Count==0)return;int next=ChannelList.SelectedIndex+delta;
        if(next>=ChannelList.Items.Count&&_offset+150<_total){_offset+=150;await SafeAsync(QueryAsync);next=0;}
        else if(next<0&&_offset>0){_offset=Math.Max(0,_offset-150);await SafeAsync(QueryAsync);next=ChannelList.Items.Count-1;}
        ChannelList.SelectedIndex=Math.Clamp(next,0,ChannelList.Items.Count-1);if(ChannelList.SelectedItem is{} selected)ChannelList.ScrollIntoView(selected);
    }
    private void Fullscreen_Click(object sender,RoutedEventArgs e)=>ToggleFullscreen();
    private void ToggleFullscreen()
    {
        _fullscreen=!_fullscreen;
        if(_fullscreen){_previousState=WindowState;WindowStyle=WindowStyle.None;WindowState=WindowState.Maximized;NavColumn.Width=new GridLength(0);Sidebar.Visibility=TopBar.Visibility=StatsBar.Visibility=FilterBar.Visibility=LibraryPanel.Visibility=GuidePanel.Visibility=BottomBar.Visibility=Visibility.Collapsed;ListColumn.Width=GapColumn.Width=new GridLength(0);MainArea.Margin=new Thickness(0);GuideRow.Height=new GridLength(0);}
        else{WindowStyle=WindowStyle.SingleBorderWindow;WindowState=_previousState;Sidebar.Visibility=TopBar.Visibility=StatsBar.Visibility=FilterBar.Visibility=LibraryPanel.Visibility=GuidePanel.Visibility=BottomBar.Visibility=Visibility.Visible;SetNav();ApplyLayout();}
    }
    private void Window_KeyDown(object sender,KeyEventArgs e)
    {
        if(!_ready)return;if(e.Key==Key.K&&Keyboard.Modifiers.HasFlag(ModifierKeys.Control)){SearchBox.Focus();SearchBox.SelectAll();e.Handled=true;return;}
        if(Keyboard.FocusedElement is TextBoxBase or PasswordBox||Keyboard.FocusedElement is ComboBox)return;
        switch(e.Key){case Key.Space:PlayPause_Click(this,e);break;case Key.F:ToggleFullscreen();break;case Key.Escape:if(_fullscreen)ToggleFullscreen();else return;break;case Key.M:Mute_Click(this,e);break;case Key.Left:Seek(-10000);break;case Key.Right:Seek(10000);break;case Key.Up:VolumeSlider.Value+=5;break;case Key.Down:VolumeSlider.Value-=5;break;case Key.PageUp:ChangeChannel(-1);break;case Key.PageDown:ChangeChannel(1);break;default:return;}e.Handled=true;
    }
    private void Window_SizeChanged(object sender,SizeChangedEventArgs e){if(MainArea is not null&&!_fullscreen)ApplyLayout();}
    private void ApplyLayout()
    {
        bool compact=ActualWidth<1180;NavColumn.Width=new GridLength(compact?72:210);ListColumn.Width=new GridLength(compact?245:355);GapColumn.Width=new GridLength(compact?14:20);MainArea.Margin=new Thickness(compact?16:28,22,compact?16:28,16);
        Sidebar.Padding=new Thickness(compact?8:18,26,compact?8:18,18);
        BrandWordmark.Visibility=NavLabel.Visibility=PromoCard.Visibility=VersionLabel.Visibility=compact?Visibility.Collapsed:Visibility.Visible;
        var buttons=new[]{HomeNav,LiveNav,MovieNav,SeriesNav,FavoriteNav,EpgNav,RecentNav,RecordingsNav,SettingsNav};
        var labels=new[]{"⌂    Ana sayfa","▣    Canlı TV","▷    Filmler","▤    Diziler","☆    Favorilerim","▦    Program rehberi","◷    Son izlenenler","●    Kayıtlarım","⚙    Ayarlar"};
        for(int i=0;i<buttons.Length;i++){buttons[i].Content=compact?labels[i][..1]:labels[i];buttons[i].ToolTip=labels[i][5..];buttons[i].HorizontalContentAlignment=compact?HorizontalAlignment.Center:HorizontalAlignment.Left;System.Windows.Automation.AutomationProperties.SetName(buttons[i],labels[i][5..]);}
    }
    private void Tick()
    {
        if(!_ready||_closing)return;var player=_engine.Player;SeekSlider.IsEnabled=player.IsSeekable;
        if(!SeekSlider.IsMouseCaptureWithin&&player.Position>=0)SeekSlider.Value=player.Position;
        if(player.IsPlaying)PlaybackBadge.Text=player.IsSeekable?TimeSpan.FromMilliseconds(Math.Max(0,player.Time)).ToString(@"hh\:mm\:ss")+" / "+TimeSpan.FromMilliseconds(Math.Max(0,player.Length)).ToString(@"hh\:mm\:ss"):"● CANLI";
        if(++_tick%30==0)EpgList.Items.Refresh();if(_tick%15==0)App.SaveSettings(_settings);
    }
    private void Cancel_Click(object sender,RoutedEventArgs e)=>_load?.Cancel();
    private async void Window_Closing(object? sender,CancelEventArgs e)
    {
        if(_closed)return;e.Cancel=true;if(_closing)return;
        if(_engine?.Recording==true&&MessageBox.Show(this,"Devam eden kayıt sonlandırılıp uygulama kapatılsın mı?","Kayıt devam ediyor",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
        _closing=true;_life.Cancel();_load?.Cancel();_play?.Cancel();_epg?.Cancel();_query?.Cancel();_clock.Stop();_search.Stop();
        try{App.SaveSettings(_settings);if(_engine is not null){Video.MediaPlayer=null;await _engine.DisposeAsync();}_providers.Dispose();}finally{_closed=true;Close();}
    }
    internal async Task SmokeRefreshAsync(string id){await ReloadSourcesAsync(id);await RefreshViewAsync();}
    internal void SmokeNavigate(string section){_section=section;SetNav();}
}
