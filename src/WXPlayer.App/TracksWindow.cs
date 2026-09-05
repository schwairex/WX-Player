using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using Microsoft.Win32;

namespace WXPlayer.App;

internal sealed class TracksWindow : PremiumWindow
{
    internal sealed record Choice(int Id,string Label){public override string ToString()=>Label;}
    internal readonly ComboBox AudioPicker=new(){MinHeight=44};
    internal readonly ComboBox SubtitlePicker=new(){MinHeight=44};
    private readonly TextBlock _status=Text("Seçimler oynatılan yayına anında uygulanır.",12,"#A3B5C8");
    private readonly DispatcherTimer _refresh=new(){Interval=TimeSpan.FromSeconds(1)};
    private readonly PlaybackEngine _engine;
    private bool _sync;
    private string _signature="";
    internal TracksWindow(Window owner,PlaybackEngine engine):base(owner,"Ses ve altyazılar","Yayının dilini ve altyazı tercihlerini düzenleyin.","subtitles",700,680)
    {
        _engine=engine;var panel=new StackPanel();Body.Children.Add(new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        var audio=Card(panel,"Ses dili","Yayının sunduğu ses parçalarından birini seçin.");audio.Children.Add(AudioPicker);
        var sub=Card(panel,"Altyazı","Yerleşik altyazıları seçin veya kendi dosyanızı ekleyin.");sub.Children.Add(SubtitlePicker);
        var external=Action("Altyazı dosyası ekle",()=>Browse(),true);external.HorizontalAlignment=HorizontalAlignment.Left;external.Margin=new(0,16,0,0);sub.Children.Add(external);
        sub.Children.Add(new TextBlock{Text="SRT · ASS · SSA · VTT · SUB",Foreground=Brush("#7D91A8"),FontSize=10,Margin=new(0,10,0,0)});
        var info=new Border{Background=Brush("#17271F"),BorderBrush=Brush("#314A37"),BorderThickness=new(1),Padding=new(16),CornerRadius=new(10),Child=_status};panel.Children.Add(info);
        foreach(var picker in new[]{AudioPicker,SubtitlePicker}){picker.DisplayMemberPath="Label";picker.SelectedValuePath="Id";}
        AudioPicker.SelectionChanged+=(_,_)=>{if(!_sync&&AudioPicker.SelectedValue is int id){_status.Text=engine.Player.SetAudioTrack(id)?"Ses tercihiniz uygulandı.":"Bu ses parçası şu anda seçilemiyor.";}};
        SubtitlePicker.SelectionChanged+=(_,_)=>{if(!_sync&&SubtitlePicker.SelectedValue is int id){_status.Text=engine.Player.SetSpu(id)?id<0?"Altyazılar kapatıldı.":"Altyazı tercihiniz uygulandı.":"Bu altyazı şu anda seçilemiyor.";}};
        _refresh.Tick+=(_,_)=>RefreshTracks();Loaded+=(_,_)=>{RefreshTracks();_refresh.Start();};Closed+=(_,_)=>_refresh.Stop();Footer.Children.Add(Action("Tamam",Close,true));
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

