using LibVLCSharp.Shared;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed class StatisticsWindow : PremiumWindow
{
    private readonly StackPanel _cards=new();
    private readonly StackPanel _top=new();
    private readonly DispatcherTimer _timer=new(){Interval=TimeSpan.FromSeconds(1)};
    internal StatisticsWindow(Window owner,Func<(string Title,PlaybackTarget? Target)> current,PlaybackEngine engine,PlayerSettings settings):base(owner,"Yayın istatistikleri","Oynatılan içeriğin teknik bilgileri · Her saniye yenilenir","statistics",670,730)
    {
        UtilityWindowAppearance.Apply(this,true);
        Body.RowDefinitions.Add(new(){Height=GridLength.Auto});Body.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});
        Body.Children.Add(_top);
        var scroll=new ScrollViewer{Content=_cards,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new(0,22.4,0,8)};
        scroll.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)]=FindResource("LiveScrollBar");Grid.SetRow(scroll,1);Body.Children.Add(scroll);
        Footer.Children.Add(UtilityWindowAppearance.Action(this,"Kapat",Close));
        void Refresh(){var info=current();Render(info.Title,info.Target,engine,settings);}
        Loaded+=(_,_)=>{Refresh();_timer.Start();};_timer.Tick+=(_,_)=>Refresh();Closed+=(_,_)=>_timer.Stop();
    }
    internal static string SafeAddress(string? address)
    {
        if(!Uri.TryCreate(address,UriKind.Absolute,out var uri))return "Henüz içerik oynatılmıyor";
        if(uri.IsFile)return "Yerel dosya · "+System.IO.Path.GetFileName(uri.LocalPath);
        return $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort?"":":"+uri.Port)}/•••";
    }
    private Brush Theme(string key)=>UtilityWindowAppearance.Brush(this,key);
    private TextBlock Label(string value,double size=14.08,string color="LiveInk")=>UtilityWindowAppearance.Text(this,value,size,color);
    private StackPanel Group(string title,string? description=null)
    {
        var group=new StackPanel{Margin=new(0,0,0,27.2)};
        var heading=Label(title,16);heading.FontWeight=FontWeights.ExtraBold;group.Children.Add(heading);
        if(description is not null){var note=Label(description,13.12,"LiveMuted");note.Margin=new(0,4,0,12.8);note.LineHeight=18.37;group.Children.Add(note);}
        else group.Children.Add(new Border{Height=12.8});
        var rows=new StackPanel();group.Children.Add(new Border{Style=UtilityWindowAppearance.Style(this,"SettingsCard"),Padding=new(17.6,4.8,17.6,4.8),Child=rows});
        _cards.Children.Add(group);return rows;
    }
    private void Value(Panel panel,string key,string value,bool danger=false)
    {
        var row=new Grid{Margin=new(0,11.2,0,11.2)};row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        var label=Label(key,14.08,"LiveMuted");label.Margin=new(0,0,16,0);row.Children.Add(label);
        var number=Label(value,14.08,danger?"DangerBrush":value=="Bildirilmedi"?"LiveMuted":"LiveInk");number.FontWeight=value=="Bildirilmedi"?FontWeights.Medium:FontWeights.SemiBold;number.TextAlignment=TextAlignment.Right;
        Typography.SetNumeralAlignment(number,FontNumeralAlignment.Tabular);Grid.SetColumn(number,1);row.Children.Add(number);
        AutomationProperties.SetName(row,key+" · "+value);
        panel.Children.Add(new Border{BorderBrush=Theme("LiveLine"),BorderThickness=new(0,panel.Children.Count==0?0:1,0,0),Child=row});
    }
    private void Render(string title,PlaybackTarget? target,PlaybackEngine engine,PlayerSettings settings)
    {
        _cards.Children.Clear();_top.Children.Clear();
        using var media=engine.Player.Media;
        var summary=new Grid();summary.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});summary.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        var labels=new StackPanel{Margin=new(0,0,14.4,0)};var name=Label(title.Length>0?title:"Oynatıcı",16.8);name.FontWeight=FontWeights.ExtraBold;name.TextWrapping=TextWrapping.NoWrap;name.TextTrimming=TextTrimming.CharacterEllipsis;labels.Children.Add(name);
        var address=Label(SafeAddress(target?.Url),12.8,"LiveMuted");address.TextWrapping=TextWrapping.NoWrap;address.TextTrimming=TextTrimming.CharacterEllipsis;address.Margin=new(0,1.6,0,0);labels.Children.Add(address);summary.Children.Add(labels);
        string state=engine.Player.State.ToString();string ink=state=="Playing"?"LiveAccent":state is "Opening" or "Buffering"?"UtilityWarning":state=="Error"?"DangerBrush":"UtilityNeutralInk";
        string bg=state=="Playing"?"LiveSelected":state is "Opening" or "Buffering"?"UtilityWarningBg":state=="Error"?"SettingsDangerBg":"SettingsButtonBg";
        // Stack badges at this native dialog width so long titles and translated states do not overlap.
        var badges=new StackPanel{VerticalAlignment=VerticalAlignment.Center};
        badges.Children.Add(UtilityWindowAppearance.Pill(this,UtilityDisplayFormatter.State(state),ink,bg,true));
        if(engine.HasLiveBuffer){var live=UtilityWindowAppearance.Pill(this,$"Canlı · {engine.BufferedSeconds:0} sn tampon","UtilityLiveInk","UtilityLiveBg",true);live.Margin=new(0,6.4,0,0);badges.Children.Add(live);}
        Grid.SetColumn(badges,1);summary.Children.Add(badges);_top.Children.Add(summary);
        var metrics=new System.Windows.Controls.Primitives.UniformGrid{Columns=3,Margin=new(0,16,0,16)};
        var videoTrack=media?.Tracks.FirstOrDefault(t=>t.TrackType==TrackType.Video);
        var audioTrack=media?.Tracks.FirstOrDefault(t=>t.TrackType==TrackType.Audio);
        var vinfo=videoTrack?.TrackType==TrackType.Video?videoTrack?.Data.Video:null;
        foreach(var (label,value) in new[]{("ÇÖZÜNÜRLÜK",vinfo.HasValue?$"{vinfo.Value.Width} × {vinfo.Value.Height}":"—"),("KAYNAK FPS",vinfo.HasValue&&vinfo.Value.FrameRateDen>0?((double)vinfo.Value.FrameRateNum/vinfo.Value.FrameRateDen).ToString("0.##"):"—"),("SES",audioTrack?.TrackType==TrackType.Audio?audioTrack.Value.Data.Audio.Rate/1000d+" kHz":"—")})
        {
            var tile=new StackPanel();var caption=Label(label,10.88,"LiveMuted");caption.FontWeight=FontWeights.Bold;tile.Children.Add(caption);
            var number=Label(value,21.6);number.FontWeight=FontWeights.ExtraBold;number.Margin=new(0,4.8,0,0);Typography.SetNumeralAlignment(number,FontNumeralAlignment.Tabular);tile.Children.Add(number);
            var box=new Border{Style=UtilityWindowAppearance.Style(this,"SettingsCard"),CornerRadius=new(16),Padding=new(16,13.6,16,13.6),Margin=new(0,0,metrics.Children.Count<2?11.2:0,0),Child=tile};AutomationProperties.SetName(box,label+" · "+value);metrics.Children.Add(box);
        }
        _top.Children.Add(metrics);_top.Children.Add(new Border{Height=1,Background=Theme("LiveLine")});
        var statistics=media?.Statistics;
        var counters=Group("Akış ölçümleri","Sayaçlar bu oynatma oturumuna aittir. Motorun bildirmediği alanlar tahmin edilmez.");
        if(statistics is { } input)Value(counters,"Giriş bit hızı",(input.InputBitrate*8).ToString("0.00")+" Mb/sn");
        Value(counters,"Tampon doluluğu",engine.BufferPercent.ToString("0")+" %");
        if(engine.HasLiveBuffer)Value(counters,"Canlı tampon",$"{engine.BufferedSeconds:0} sn · "+(engine.IsReplay?$"{engine.BehindLive:0} sn geride":"Canlı"));
        if(statistics is { } s)
        {
            Value(counters,"Okunan veri",(s.ReadBytes/1048576d).ToString("0.0")+" MB");
            Value(counters,"Çözülen / gösterilen kare",$"{s.DecodedVideo:N0} / {s.DisplayedPictures:N0}");Value(counters,"Kaybedilen kare",s.LostPictures.ToString("N0"),s.LostPictures>0);
            Value(counters,"Akış bozulması / süreksizlik",$"{s.DemuxCorrupted:N0} / {s.DemuxDiscontinuity:N0}",s.DemuxCorrupted>0||s.DemuxDiscontinuity>0);
        }
        var stream=Group("Oynatma ve motor");
        Value(stream,"Durum",UtilityDisplayFormatter.State(engine.Player.State.ToString()));Value(stream,"Motor / video çıkışı","LibVLC · "+UtilityDisplayFormatter.Output(engine.ConfiguredVideoOutput));
        Value(stream,"Donanım çözme tercihi",settings.HardwareAcceleration?"D3D11VA · GPU istenir":"Yazılım");
        Value(stream,"Ağ önbelleği",engine.CacheMs(settings)+" ms");
        if(media is null)return;
        var tracksHeading=Label("Parçalar",16);tracksHeading.FontWeight=FontWeights.ExtraBold;tracksHeading.Margin=new(0,0,0,12.8);_cards.Children.Add(tracksHeading);
        foreach(var track in media.Tracks)
        {
            string codec=Encoding.ASCII.GetString(BitConverter.GetBytes(track.Codec)).Trim('\0',' ');
            var content=new StackPanel();var heading=new WrapPanel{Margin=new(0,14.4,0,8)};
            string kind=track.TrackType switch{TrackType.Video=>"Video",TrackType.Audio=>"Ses",_=>"Altyazı"};
            var kindText=Label(kind,15.2);kindText.FontWeight=FontWeights.ExtraBold;heading.Children.Add(kindText);
            if(track.TrackType==TrackType.Audio&&track.Id==engine.Player.AudioTrack){var selected=UtilityWindowAppearance.Pill(this,"SEÇİLİ","LiveAccent","SettingsSelected");selected.Padding=new(8,1.92,8,1.92);selected.Margin=new(9.6,0,0,0);heading.Children.Add(selected);}
            var detail=Label("Parça "+track.Id+(string.IsNullOrWhiteSpace(track.Language)||track.Language=="???"?"":" · "+track.Language),12.8,"LiveMuted");detail.Margin=new(9.6,0,0,0);detail.VerticalAlignment=VerticalAlignment.Center;heading.Children.Add(detail);content.Children.Add(heading);
            var card=new StackPanel();content.Children.Add(card);_cards.Children.Add(new Border{Style=UtilityWindowAppearance.Style(this,"SettingsCard"),Padding=new(17.6,1.6,17.6,4.8),Margin=new(0,0,0,11.2),Child=content});
            Value(card,"Codec",codec.Length>0?codec:"Bildirilmedi");
            if(track.TrackType==TrackType.Video)
            {
                var v=track.Data.Video;Value(card,"Çözünürlük",$"{v.Width} × {v.Height}");Value(card,"Kaynak FPS",v.FrameRateDen>0?((double)v.FrameRateNum/v.FrameRateDen).ToString("0.###"):"Bildirilmedi");
                double ratio=v.Height>0?(double)v.Width/v.Height*(v.SarDen>0?(double)v.SarNum/v.SarDen:1):0;Value(card,"Görüntü oranı",ratio>0?ratio.ToString("0.###"):"Bildirilmedi");
            }
            if(track.TrackType==TrackType.Audio){Value(card,"Örnekleme hızı",track.Data.Audio.Rate+" Hz");Value(card,"Kanallar",track.Data.Audio.Channels switch{1=>"Mono",2=>"Stereo",var n=>n+" kanal"});}
            Value(card,"Parça bit hızı",track.Bitrate>0?(track.Bitrate/1000d).ToString("0.#")+" kb/sn":"Bildirilmedi");
        }
    }
}
