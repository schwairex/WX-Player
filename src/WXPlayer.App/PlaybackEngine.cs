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
    public async Task PlayAsync(PlaybackTarget target,PlayerSettings settings,CancellationToken ct)
    {
        await _gate.WaitAsync(ct);try{await Task.Run(()=>{ct.ThrowIfCancellationRequested();Player.Stop();ct.ThrowIfCancellationRequested();using var media=MakeMedia(target,settings);Player.EnableHardwareDecoding=settings.HardwareAcceleration;if(!Player.Play(media))throw new InvalidOperationException("Oynatma başlatılamadı.");Player.Volume=settings.Volume;},ct);}finally{_gate.Release();}
    }
    public async Task StopAsync(){await _gate.WaitAsync();try{await Task.Run(Player.Stop);}finally{_gate.Release();}}
    public async Task PlayCaptureAsync(string video,string audio,PlayerSettings settings)
    {
        await _gate.WaitAsync();try{await Task.Run(()=>{Player.Stop();using var media=new Media(Vlc,"dshow://",FromType.FromLocation);media.AddOption(":dshow-vdev="+AddressPolicy.Header(video));media.AddOption(":dshow-adev="+AddressPolicy.Header(audio));media.AddOption(":live-caching="+settings.NetworkCacheMs);if(!Player.Play(media))throw new InvalidOperationException("DirectShow aygıtı başlatılamadı.");});}finally{_gate.Release();}
    }
    public async Task<string> StartRecordingAsync(PlaybackTarget target,string title,PlayerSettings settings)
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
    public async ValueTask DisposeAsync(){await StopRecordingAsync();await _gate.WaitAsync();try{await Task.Run(()=>{Player.Stop();Player.Dispose();Vlc.Dispose();});}finally{_gate.Release();}}
}

