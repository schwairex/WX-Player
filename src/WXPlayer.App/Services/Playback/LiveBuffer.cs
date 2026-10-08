using System.Globalization;
using LibVLCSharp.Shared;

namespace WXPlayer.App;

// One upstream connection is remuxed into a bounded, local HLS window. No transcoding.
internal sealed class LiveBuffer : IDisposable
{
    private readonly MediaPlayer _ingest;
    internal string DirectoryPath {get;}
    internal string Playlist=>Path.Combine(DirectoryPath,"live.m3u8");
    internal bool Failed {get;private set;}
    internal LiveBuffer(LibVLC vlc,Media media)
    {
        DirectoryPath=Path.Combine(App.DataDirectory,"timeshift",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(DirectoryPath);
        string p=DirectoryPath.Replace('\\','/');
        if(p.Contains('\''))throw new InvalidOperationException("Canlı tampon klasöründe tek tırnak kullanılamaz.");
        media.AddOption($":sout=#std{{access=livehttp{{seglen=2,numsegs=35,delsegs=true,splitanywhere=true,index='{p}/live.m3u8',index-url='part-########.ts'}},mux=ts,dst='{p}/part-########.ts'}}");
        media.AddOption(":sout-all");
        _ingest=new MediaPlayer(vlc);_ingest.EncounteredError+=(_,_)=>Failed=true;
        if(!_ingest.Play(media)){Dispose();throw new InvalidOperationException("Canlı tampon başlatılamadı.");}
    }
    internal List<(string Path,double Seconds)> Segments()
    {
        var list=new List<(string,double)>();
        try{double duration=0;foreach(string raw in File.ReadAllLines(Playlist)){string line=raw.Trim();if(line.StartsWith("#EXTINF:"))double.TryParse(line[8..].TrimEnd(','),NumberStyles.Float,CultureInfo.InvariantCulture,out duration);else if(line.StartsWith("part-",StringComparison.Ordinal)&&Path.GetFileName(line)==line&&line.EndsWith(".ts")){string file=Path.Combine(DirectoryPath,line);if(File.Exists(file)&&duration>0)list.Add((file,duration));duration=0;}}}catch(IOException){}
        return list;
    }
    internal double Available=>Math.Min(60,Segments().Sum(s=>s.Seconds));
    internal bool OverBudget {get{try{return new DirectoryInfo(DirectoryPath).EnumerateFiles().Sum(f=>f.Length)>256L*1024*1024;}catch(IOException){return false;}}}
    internal async Task WaitReadyAsync(CancellationToken ct)
    {
        var until=DateTime.UtcNow.AddSeconds(25);
        while(Segments().Count<2){ct.ThrowIfCancellationRequested();if(Failed||OverBudget||DateTime.UtcNow>until)throw new IOException("Yayın yerel tampon için hazırlanamadı.");await Task.Delay(100,ct);}
    }
    internal async Task<(string Path,double Length)> SnapshotAsync(CancellationToken ct)
    {
        var segments=Segments();double duration=0;int first=segments.Count;
        while(first>0&&duration<62){first--;duration+=segments[first].Seconds;}
        if(duration<1)throw new IOException("Geri sarma tamponu henüz hazır değil.");
        string output=Path.Combine(DirectoryPath,"replay-"+Guid.NewGuid().ToString("N")+".ts");
        await using(var destination=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read,65536,true))
        {foreach(var segment in segments.Skip(first)){await using var input=new FileStream(segment.Path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete,65536,true);await input.CopyToAsync(destination,ct);}}
        return(output,duration);
    }
    internal void ClearReplays(){try{foreach(string file in Directory.EnumerateFiles(DirectoryPath,"replay-*.ts"))try{File.Delete(file);}catch(IOException){}}catch(IOException){}}
    public void Dispose(){_ingest?.Stop();_ingest?.Dispose();try{Directory.Delete(DirectoryPath,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
}
