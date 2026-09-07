using LibVLCSharp.Shared;
using WXPlayer.Core;

namespace WXPlayer.App;

public sealed class PlaybackEngine : IAsyncDisposable
{
    public LibVLC Vlc { get; }
    public string ConfiguredVideoOutput {get;}
    public float BufferPercent {get;private set;}
    public MediaPlayer Player { get; }
    private MediaPlayer? _recorder;
    private LiveBuffer? _live;
    private PlayerSettings? _liveSettings;
    private PlaybackTarget? _liveTarget;
    private DateTime _replayEdge;
    private double _replayLength;
    public bool HasLiveBuffer=>_live is not null;
    public bool IsReplay {get;private set;}
    public double BufferedSeconds=>_live?.Available??0;
    public double BehindLive=>IsReplay?Math.Clamp(_replayLength-Player.Time/1000d+(DateTime.UtcNow-_replayEdge).TotalSeconds,0,120):0;
    public string TimeshiftStatus {get;private set;}="";
    private readonly SemaphoreSlim _gate=new(1,1);
    private int _extraCache;
    private DateTime _lastBuffer;
    public bool Recording=>_recorder is not null;
    public string? RecordingPath {get;private set;}
    public event Action<string>? RecordingFailed;

    public PlaybackEngine(PlayerSettings settings)
    {
        ConfiguredVideoOutput=settings.VideoOutput;LibVLCSharp.Shared.Core.Initialize();
        Vlc=new LibVLC("--no-video-title-show","--no-osd","--no-snapshot-preview","--no-metadata-network-access","--no-lua", "--vout="+settings.VideoOutput);
        Player=new MediaPlayer(Vlc){EnableHardwareDecoding=settings.HardwareAcceleration,EnableKeyInput=false,EnableMouseInput=false,Volume=settings.Volume};
        Player.Buffering+=(_,e)=>{BufferPercent=e.Cache;if(e.Cache<30&&(DateTime.UtcNow-_lastBuffer).TotalSeconds>8){_extraCache=Math.Min(4800,_extraCache+400);_lastBuffer=DateTime.UtcNow;}};
    }
    public int CacheMs(PlayerSettings settings)=>Math.Clamp(settings.NetworkCacheMs+(settings.AdaptiveCache?_extraCache:0),200,10000);
    private Media MakeMedia(PlaybackTarget target,PlayerSettings settings)
    {
        if(!AddressPolicy.IsPlayable(target.Url))throw new InvalidOperationException("Bu yayın adresinin protokolü desteklenmiyor.");
        var media=new Media(Vlc,new Uri(target.Url));
        media.AddOption(":network-caching="+CacheMs(settings));media.AddOption(":file-caching=500");
        media.AddOption(":avcodec-hw="+(settings.HardwareAcceleration?"d3d11va":"none"));
        if(target.UserAgent.Length>0)media.AddOption(":http-user-agent="+AddressPolicy.Header(target.UserAgent));
        if(target.Referrer.Length>0)media.AddOption(":http-referrer="+AddressPolicy.Header(target.Referrer));return media;
    }
    public async Task PlayAsync(PlaybackTarget target,PlayerSettings settings,CancellationToken ct,bool live=false)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await Task.Run(()=>{Player.Stop();_live?.Dispose();_live=null;IsReplay=false;},ct);
            ct.ThrowIfCancellationRequested();TimeshiftStatus="";
            if(live)
            {
                try
                {
                    using var input=MakeMedia(target,settings);
                    _live=await Task.Run(()=>new LiveBuffer(Vlc,input),ct);_liveTarget=target;_liveSettings=settings;
                    TimeshiftStatus="Canlı tampon hazırlanıyor…";await _live.WaitReadyAsync(ct);
                    using var local=MakeMedia(new PlaybackTarget(new Uri(_live.Playlist).AbsoluteUri),settings);
                    if(!Player.Play(local))throw new IOException("Yerel canlı akış açılamadı.");
                    TimeshiftStatus="Son 60 saniye · Yerel tampon";
                }
                catch(OperationCanceledException){_live?.Dispose();_live=null;throw;}
                catch{_live?.Dispose();_live=null;TimeshiftStatus="Bu yayında yerel geri sarma kullanılamıyor.";using var direct=MakeMedia(target,settings);if(!Player.Play(direct))throw new IOException("Yayın açılamadı.");}
            }
            else{using var media=MakeMedia(target,settings);if(!Player.Play(media))throw new InvalidOperationException("Oynatma başlatılamadı.");}
            Player.Volume=settings.Volume;
        }finally{_gate.Release();}
    }
    public async Task RewindLiveAsync(double seconds,CancellationToken ct=default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if(_live is null||_liveSettings is null)return;
            double behind=Math.Clamp(seconds,0,Math.Min(60,_live.Available));
            if(behind<1){PlayLiveEdge();return;}
            await Task.Run(Player.Stop);_live.ClearReplays();
            var snapshot=await _live.SnapshotAsync(ct);_replayLength=snapshot.Length;_replayEdge=DateTime.UtcNow;
            using var media=MakeMedia(new PlaybackTarget(new Uri(snapshot.Path).AbsoluteUri),_liveSettings);
            media.AddOption(":start-time="+Math.Max(0,snapshot.Length-behind).ToString("0.###",System.Globalization.CultureInfo.InvariantCulture));
            IsReplay=true;if(!Player.Play(media)){IsReplay=false;PlayLiveEdge();}Player.Volume=_liveSettings.Volume;
            // TS seeking needs the demuxer's measured duration, which is available after opening.
            var deadline=DateTime.UtcNow.AddSeconds(8);
            while(IsReplay&&(!Player.IsPlaying||Player.Length<=0)&&DateTime.UtcNow<deadline){ct.ThrowIfCancellationRequested();await Task.Delay(50,ct);}
            if(IsReplay&&Player.IsSeekable&&Player.Length>0){_replayLength=Player.Length/1000d;Player.Time=Math.Max(0,Player.Length-(long)(behind*1000));}
        }finally{_gate.Release();}
    }
    private void PlayLiveEdge()
    {
        if(_live is null||_liveSettings is null)return;
        Player.Stop();IsReplay=false;_live.ClearReplays();
        using var media=MakeMedia(new PlaybackTarget(new Uri(_live.Playlist).AbsoluteUri),_liveSettings);Player.Play(media);Player.Volume=_liveSettings.Volume;
    }
    public async Task GoLiveAsync(){await _gate.WaitAsync();try{await Task.Run(PlayLiveEdge);}finally{_gate.Release();}}
    public async Task MaintainLiveAsync()
    {
        if(!await _gate.WaitAsync(0))return;
        try
        {
            if(_live is null)return;
            if(_live.OverBudget||_live.Failed)
            {
                await Task.Run(()=>{Player.Stop();_live.Dispose();_live=null;IsReplay=false;});
                TimeshiftStatus="Canlı tampon sınırına ulaşıldı · Doğrudan yayın";
                if(_liveTarget is not null&&_liveSettings is not null){using var media=MakeMedia(_liveTarget,_liveSettings);Player.Play(media);}
            }
            else if(IsReplay&&((Player.State==VLCState.Paused&&BehindLive>63)||Player.State==VLCState.Ended))await Task.Run(PlayLiveEdge);
        }finally{_gate.Release();}
    }
    public async Task StopAsync(){await _gate.WaitAsync();try{await Task.Run(()=>{Player.Stop();_live?.Dispose();_live=null;IsReplay=false;});}finally{_gate.Release();}}
    public async Task PlayCaptureAsync(string video,string audio,PlayerSettings settings)
    {
        await StopAsync();await _gate.WaitAsync();try{await Task.Run(()=>{using var media=new Media(Vlc,"dshow://",FromType.FromLocation);media.AddOption(":dshow-vdev="+AddressPolicy.Header(video));media.AddOption(":dshow-adev="+AddressPolicy.Header(audio));media.AddOption(":live-caching="+settings.NetworkCacheMs);if(!Player.Play(media))throw new InvalidOperationException("DirectShow aygıtı başlatılamadı.");});}finally{_gate.Release();}
    }    public async Task<string> StartRecordingAsync(PlaybackTarget target,string title,PlayerSettings settings)
    {
        await _gate.WaitAsync();try{return await Task.Run(()=>
        {
            if(_recorder is not null)throw new InvalidOperationException("Bir kayıt zaten devam ediyor.");
            Directory.CreateDirectory(settings.RecordingFolder);
            string safe=string.Concat(title.Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'_':c));if(safe.Length>70)safe=safe[..70];
            string path=Path.Combine(Path.GetFullPath(settings.RecordingFolder),$"{safe}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..5]}.ts");
            if(path.Contains('\''))throw new InvalidOperationException("Kayıt klasörünün yolunda tek tırnak kullanmayın.");
            using var media=MakeMedia(target,settings);media.AddOption($":sout=#std{{access=file,mux=ts,dst='{path.Replace('\\','/')}'}}");media.AddOption(":sout-all");
            var recorder=new MediaPlayer(Vlc);recorder.EncounteredError+=(_,_)=>RecordingFailed?.Invoke("Kayıt akışı kesildi. Dosyayı ve sağlayıcının eşzamanlı bağlantı sınırını kontrol edin.");
            if(!recorder.Play(media)){recorder.Dispose();throw new InvalidOperationException("Kayıt başlatılamadı.");}
            _recorder=recorder;RecordingPath=path;return path;
        });}finally{_gate.Release();}
    }
    public async Task<string?> StopRecordingAsync()
    {
        await _gate.WaitAsync();try{return await Task.Run(()=>{var p=RecordingPath;_recorder?.Stop();_recorder?.Dispose();_recorder=null;RecordingPath=null;return p;});}finally{_gate.Release();}
    }
    public async ValueTask DisposeAsync(){await StopRecordingAsync();await _gate.WaitAsync();try{await Task.Run(()=>{Player.Stop();_live?.Dispose();_live=null;Player.Dispose();Vlc.Dispose();});}finally{_gate.Release();}}
}

