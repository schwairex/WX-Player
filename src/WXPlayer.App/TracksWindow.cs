using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using Microsoft.Win32;

namespace WXPlayer.App;

internal sealed class TracksWindow : PremiumWindow
{
    internal sealed record Choice(int Id,string Label){public override string ToString()=>Label;}
    internal readonly ComboBox AudioPicker=new(){MinHeight=48};
    internal readonly ComboBox SubtitlePicker=new(){MinHeight=48};
    private readonly TextBlock _status=Text("Seçimler oynatılan yayına anında uygulanır.",12);
    private readonly DispatcherTimer _refresh=new(){Interval=TimeSpan.FromSeconds(1)};
    private readonly PlaybackEngine _engine;
    private bool _sync;
    private string _signature="";
    internal TracksWindow(Window owner,PlaybackEngine engine):base(owner,"Ses ve altyazılar","Yayının dilini ve altyazı tercihlerini düzenleyin.","subtitles",700,680)
    {
        UtilityWindowAppearance.Apply(this);
        _engine=engine;var panel=new StackPanel();var scroll=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
        scroll.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)]=FindResource("LiveScrollBar");Body.Children.Add(scroll);
        var audio=TrackCard(panel,"Dinleme dili","SES PARÇASI  /  Tercihiniz anında uygulanır.");audio.Children.Add(AudioPicker);
        var sub=TrackCard(panel,"Altyazı tercihleri","YERLEŞİK VEYA HARİCİ  /  İzleme dilinizi seçin.");sub.Children.Add(SubtitlePicker);
        var external=UtilityWindowAppearance.Action(this,"Altyazı dosyası ekle",()=>Browse());external.HorizontalAlignment=HorizontalAlignment.Left;external.Padding=new(16,0,16,0);
        external.Content=new StackPanel{Orientation=Orientation.Horizontal,Children={new SvgIcon("plus"){Width=16.8,Height=16.8,Margin=new(0,0,8,0)},new TextBlock{Text="Altyazı dosyası ekle",VerticalAlignment=VerticalAlignment.Center}}};
        var formats=new WrapPanel{VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Right};
        foreach(string format in new[]{"SRT","ASS","SSA","VTT","SUB"})formats.Children.Add(new Border{Background=Theme("LivePanel"),BorderBrush=Theme("LiveLine"),BorderThickness=new(1),CornerRadius=new(7.2),Padding=new(8.8,3.52,8.8,3.52),Margin=new(5.6,0,0,4),Child=new TextBlock{Text=format,FontSize=11.2,FontWeight=FontWeights.Bold,Foreground=Theme("LiveMuted")}});
        var addRow=new Grid{Margin=new(0,14.4,0,0)};addRow.ColumnDefinitions.Add(new(){Width=GridLength.Auto});addRow.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
        addRow.Children.Add(external);Grid.SetColumn(formats,1);formats.Margin=new(14.4,0,0,0);addRow.Children.Add(formats);
        sub.Children.Add(new Border{BorderBrush=Theme("LiveLine"),BorderThickness=new(0,1,0,0),Margin=new(0,14.4,0,0),Child=addRow});
        _status.Foreground=Theme("LiveMuted");_status.FontSize=13.6;_status.LineHeight=19;AutomationProperties.SetLiveSetting(_status,AutomationLiveSetting.Polite);
        var info=new Grid{Margin=new(3.2,0,3.2,0)};info.ColumnDefinitions.Add(new(){Width=GridLength.Auto});info.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
        info.Children.Add(new SvgIcon("utility-info"){Width=17.6,Height=17.6,Margin=new(0,0,11.2,0),Foreground=Theme("LiveMuted"),VerticalAlignment=VerticalAlignment.Top});Grid.SetColumn(_status,1);info.Children.Add(_status);panel.Children.Add(info);
        foreach(var picker in new[]{AudioPicker,SubtitlePicker}){picker.SelectedValuePath="Id";picker.Style=UtilityWindowAppearance.Style(this,"UtilityTrackPicker");}
        AutomationProperties.SetName(AudioPicker,"Ses parçası");AutomationProperties.SetName(SubtitlePicker,"Altyazı");
        AudioPicker.SelectionChanged+=(_,_)=>{if(!_sync&&AudioPicker.SelectedValue is int id){_status.Text=engine.Player.SetAudioTrack(id)?"Ses tercihiniz uygulandı.":"Bu ses parçası şu anda seçilemiyor.";}};
        SubtitlePicker.SelectionChanged+=(_,_)=>{if(!_sync&&SubtitlePicker.SelectedValue is int id){_status.Text=engine.Player.SetSpu(id)?id<0?"Altyazılar kapatıldı.":"Altyazı tercihiniz uygulandı.":"Bu altyazı şu anda seçilemiyor.";}};
        _refresh.Tick+=(_,_)=>RefreshTracks();Loaded+=(_,_)=>{RefreshTracks();_refresh.Start();};Closed+=(_,_)=>_refresh.Stop();Footer.Children.Add(UtilityWindowAppearance.Action(this,"Tamam",Close));
    }
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        // Keep standard ComboBox Escape handling ahead of the existing PremiumWindow close handler.
        // No track selection, timer or player state is changed here.
        if(e.Key==Key.Escape&&(AudioPicker.IsDropDownOpen||SubtitlePicker.IsDropDownOpen))
        {
            AudioPicker.SetCurrentValue(ComboBox.IsDropDownOpenProperty,false);SubtitlePicker.SetCurrentValue(ComboBox.IsDropDownOpenProperty,false);e.Handled=true;
        }
        base.OnPreviewKeyDown(e);
    }
    private System.Windows.Media.Brush Theme(string key)=>UtilityWindowAppearance.Brush(this,key);
    private StackPanel TrackCard(Panel parent,string title,string caption)
    {
        // This window alone splits the original copy; PremiumWindow.Card remains unchanged.
        var parts=caption.Split("  /  ",2,StringSplitOptions.None);
        var group=new StackPanel{Margin=new(0,0,0,25.6)};var heading=new WrapPanel();
        heading.Children.Add(new TextBlock{Text=title,FontSize=16,FontWeight=FontWeights.ExtraBold,VerticalAlignment=VerticalAlignment.Center});
        var tag=UtilityWindowAppearance.Pill(this,parts[0],"UtilityTagInk");tag.Padding=new(8.8,2.4,8.8,2.4);tag.Margin=new(9.6,0,0,0);heading.Children.Add(tag);group.Children.Add(heading);
        group.Children.Add(new TextBlock{Text=parts.Length>1?parts[1]:"",FontSize=13.12,Foreground=Theme("LiveMuted"),Margin=new(0,4.8,0,12.8),TextWrapping=TextWrapping.Wrap});
        var content=new StackPanel();group.Children.Add(new Border{Style=UtilityWindowAppearance.Style(this,"SettingsCard"),Padding=new(17.6,16,17.6,16),Child=content});parent.Children.Add(group);return content;
    }
    private void RefreshTracks()
    {
        var audio=_engine.Player.AudioTrackDescription.Select(t=>new Choice(t.Id,t.Id<0?"Ses kapalı":string.IsNullOrWhiteSpace(t.Name)?"Ses parçası "+t.Id:t.Name)).ToArray();
        var subs=_engine.Player.SpuDescription.Select(t=>new Choice(t.Id,t.Id<0?"Altyazı kapalı":string.IsNullOrWhiteSpace(t.Name)?"Altyazı "+t.Id:t.Name)).ToList();
        if(!subs.Any(t=>t.Id<0))subs.Insert(0,new(-1,"Altyazı kapalı"));
        string key=string.Join('|',audio.Select(t=>t.ToString()))+string.Join('|',subs.Select(t=>t.ToString()));
        _sync=true;try{if(key!=_signature){AudioPicker.ItemsSource=audio.Length>0?audio:[new Choice(-1,"Henüz bir ses parçası bulunamadı")];SubtitlePicker.ItemsSource=subs;_signature=key;}AudioPicker.SelectedValue=_engine.Player.AudioTrack;SubtitlePicker.SelectedValue=_engine.Player.Spu;AudioPicker.IsEnabled=audio.Any(t=>t.Id>=0);SubtitlePicker.IsEnabled=subs.Count>1;}finally{_sync=false;}
        using var media=_engine.Player.Media;if(media is null)_status.Text="Önce bir içerik oynatın. Kullanılabilir parçalar burada listelenir.";
    }
    private void Browse()
    {
        var dialog=new OpenFileDialog{Title="Altyazı dosyası seçin",Filter="Altyazı dosyaları|*.srt;*.ass;*.ssa;*.vtt;*.sub"};
        if(dialog.ShowDialog(this)!=true)return;
        try{_status.Text=_engine.Player.AddSlave(MediaSlaveType.Subtitle,new Uri(dialog.FileName).AbsoluteUri,true)?"Altyazı eklendi · "+System.IO.Path.GetFileName(dialog.FileName):"Altyazı eklenemedi. Bir içerik oynatıldığından emin olun.";_signature="";RefreshTracks();}catch{_status.Text="Altyazı dosyası okunamadı.";}
    }
}

