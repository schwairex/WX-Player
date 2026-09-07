using LibVLCSharp.Shared;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed class StatisticsWindow : PremiumWindow
{
    private readonly StackPanel _cards=new();
    private readonly DispatcherTimer _timer=new(){Interval=TimeSpan.FromSeconds(1)};
    internal StatisticsWindow(Window owner,Func<(string Title,PlaybackTarget? Target)> current,PlaybackEngine engine,PlayerSettings settings):base(owner,"Yayın istatistikleri","Oynatılan içeriğin teknik bilgileri · Her saniye yenilenir","statistics",670,730)
    {
        Body.Children.Add(new ScrollViewer{Content=_cards,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});Footer.Children.Add(Action("Kapat",Close));
        void Refresh(){var info=current();Render(info.Title,info.Target,engine,settings);}
        Loaded+=(_,_)=>{Refresh();_timer.Start();};_timer.Tick+=(_,_)=>Refresh();Closed+=(_,_)=>_timer.Stop();
    }
    internal static string SafeAddress(string? address)
    {
        if(!Uri.TryCreate(address,UriKind.Absolute,out var uri))return "Henüz içerik oynatılmıyor";
        if(uri.IsFile)return "Yerel dosya · "+System.IO.Path.GetFileName(uri.LocalPath);
        return $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort?"":":"+uri.Port)}/•••";
    }
    private void Render(string title,PlaybackTarget? target,PlaybackEngine engine,PlayerSettings settings)
    {
        _cards.Children.Clear();
        using var media=engine.Player.Media;
        var metrics=new System.Windows.Controls.Primitives.UniformGrid{Columns=3,Margin=new(0,0,0,16)};
        var videoTrack=media?.Tracks.FirstOrDefault(t=>t.TrackType==TrackType.Video);
        var audioTrack=media?.Tracks.FirstOrDefault(t=>t.TrackType==TrackType.Audio);
        var vinfo=videoTrack?.TrackType==TrackType.Video?videoTrack?.Data.Video:null;
        foreach(var (label,value) in new[]{("ÇÖZÜNÜRLÜK",vinfo.HasValue?$"{vinfo.Value.Width} × {vinfo.Value.Height}":"—"),("KAYNAK FPS",vinfo.HasValue&&vinfo.Value.FrameRateDen>0?((double)vinfo.Value.FrameRateNum/vinfo.Value.FrameRateDen).ToString("0.##"):"—"),("SES",audioTrack?.TrackType==TrackType.Audio?audioTrack.Value.Data.Audio.Rate/1000d+" kHz":"—")})
        {
            var tile=new StackPanel();tile.Children.Add(Text(label,10,"#8DA69C"));var number=Text(value,19);number.FontWeight=FontWeights.SemiBold;number.Margin=new(0,10,0,0);tile.Children.Add(number);
            metrics.Children.Add(new Border{Background=Brush("#192B2B"),BorderBrush=Brush("#365049"),BorderThickness=new(1),CornerRadius=new(13),Padding=new(15),Margin=new(0,0,8,0),Child=tile});
        }
        _cards.Children.Add(metrics);var stream=Card(_cards,title.Length>0?title:"Oynatıcı",SafeAddress(target?.Url));
        void Value(Panel panel,string key,string value)=>Row(panel,key,Text(value));
        Value(stream,"Durum",engine.Player.State.ToString());Value(stream,"Motor / video çıkışı","LibVLC · "+engine.ConfiguredVideoOutput);
        Value(stream,"Donanım çözme tercihi",settings.HardwareAcceleration?"D3D11VA · GPU istenir":"Yazılım");
        Value(stream,"Ağ önbelleği",engine.CacheMs(settings)+" ms");Value(stream,"Tampon doluluğu",engine.BufferPercent.ToString("0")+" %");
        if(engine.HasLiveBuffer)Value(stream,"Canlı tampon",$"{engine.BufferedSeconds:0} sn · "+(engine.IsReplay?$"{engine.BehindLive:0} sn geride":"Canlı"));
        if(media is null)return;
        foreach(var track in media.Tracks)
        {
            string codec=Encoding.ASCII.GetString(BitConverter.GetBytes(track.Codec)).Trim('\0',' ');
            var card=Card(_cards,track.TrackType switch{TrackType.Video=>"Video",TrackType.Audio=>"Ses"+(track.Id==engine.Player.AudioTrack?" · Seçili":""),_=>"Altyazı"},"Parça "+track.Id+(string.IsNullOrWhiteSpace(track.Language)?"":" · "+track.Language));
            Value(card,"Codec",codec.Length>0?codec:"Bildirilmedi");
            if(track.TrackType==TrackType.Video)
            {
                var v=track.Data.Video;Value(card,"Çözünürlük",$"{v.Width} × {v.Height}");Value(card,"Kaynak FPS",v.FrameRateDen>0?((double)v.FrameRateNum/v.FrameRateDen).ToString("0.###"):"Bildirilmedi");
                double ratio=v.Height>0?(double)v.Width/v.Height*(v.SarDen>0?(double)v.SarNum/v.SarDen:1):0;Value(card,"Görüntü oranı",ratio>0?ratio.ToString("0.###"):"Bildirilmedi");
            }
            if(track.TrackType==TrackType.Audio){Value(card,"Örnekleme hızı",track.Data.Audio.Rate+" Hz");Value(card,"Kanallar",track.Data.Audio.Channels switch{1=>"Mono",2=>"Stereo",var n=>n+" kanal"});}
            Value(card,"Parça bit hızı",track.Bitrate>0?(track.Bitrate/1000d).ToString("0.#")+" kb/sn":"Bildirilmedi");
        }
        var s=media.Statistics;var counters=Card(_cards,"Akış ölçümleri","Sayaçlar bu oynatma oturumuna aittir. Motorun bildirmediği alanlar tahmin edilmez.");
        Value(counters,"Giriş bit hızı",(s.InputBitrate*8).ToString("0.00")+" Mb/sn");Value(counters,"Okunan veri",(s.ReadBytes/1048576d).ToString("0.0")+" MB");
        Value(counters,"Çözülen / gösterilen kare",$"{s.DecodedVideo:N0} / {s.DisplayedPictures:N0}");Value(counters,"Kaybedilen kare",s.LostPictures.ToString("N0"));Value(counters,"Akış bozulması / süreksizlik",$"{s.DemuxCorrupted:N0} / {s.DemuxDiscontinuity:N0}");
    }
}
