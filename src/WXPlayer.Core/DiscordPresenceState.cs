using System.Text;
using System.Text.RegularExpressions;

namespace WXPlayer.Core;

public enum PresencePlayback { Opening, Playing, Paused, Buffering, Stopped, Error, Ended }
public sealed record DiscordActivity(string Details,string State,long? Start,long? End,string? Image);
public sealed record PresenceProgramme(string Title,DateTimeOffset Start,DateTimeOffset End);

// Deliberately excludes source config, stream address, headers, IDs and credentials.
public sealed record PresenceMedia(ContentKind Kind,string Title,string EpisodeTitle,int Season,int Episode,string? Image)
{
    public static PresenceMedia FromContent(ContentItem item,IEnumerable<string>? privateValues=null)
    {
        var secrets=privateValues?.Where(s=>!string.IsNullOrWhiteSpace(s)).ToArray()??[];
        string Clean(string value)
        {
            foreach(string secret in secrets)value=value.Replace(secret,"",StringComparison.OrdinalIgnoreCase);
            return PublicText(value);
        }
        string? image=PublicImage(item.Logo);
        if(image is not null&&secrets.Any(s=>image.Contains(s,StringComparison.OrdinalIgnoreCase)))image=null;
        string title=Clean(item.Kind==ContentKind.Episode&&item.SeriesName.Length>0?item.SeriesName:item.Name);
        return new(item.Kind,title.Length>0?title:"WX Player",Clean(item.Name),item.Season,item.Episode,image);
    }
    public static string PublicText(string text)
    {
        text=Regex.Replace(text,@"(?:[a-z][a-z0-9+.-]*://|www\.)\S+|\b(?:password|passwd|username|token|authorization|secret)\s*[=:]\s*\S+","",RegexOptions.IgnoreCase);
        text=Regex.Replace(text,@"\s+"," ").Trim();
        var result=new StringBuilder();int bytes=0;
        foreach(var rune in text.EnumerateRunes())
        {
            if(System.Text.Rune.IsControl(rune))continue;
            if(bytes+rune.Utf8SequenceLength>120)break;
            result.Append(rune);bytes+=rune.Utf8SequenceLength;
        }
        return result.ToString();
    }
    public static string? PublicImage(string value)
    {
        if(!Uri.TryCreate(value,UriKind.Absolute,out var uri)||uri.Scheme!="https"||!uri.IsDefaultPort||uri.UserInfo.Length>0||uri.Query.Length>0||uri.Fragment.Length>0)return null;
        // Provider-hosted artwork can embed credentials even in paths. Only known public
        // artwork CDNs with constrained paths are shared; arbitrary provider logos are not.
        bool safe=uri.Host switch
        {
            "image.tmdb.org"=>Regex.IsMatch(uri.AbsolutePath,@"^/t/p/(?:w[0-9]+|original)/[a-zA-Z0-9_-]+\.(?:jpg|png|webp)$"),
            "static.tvmaze.com"=>Regex.IsMatch(uri.AbsolutePath,@"^/uploads/images/(?:original_untouched|medium_portrait|medium_landscape)/[0-9]+/[0-9]+\.(?:jpg|png|webp)$"),
            "upload.wikimedia.org"=>uri.AbsolutePath.StartsWith("/wikipedia/")&&!uri.AbsolutePath.Contains('%'),
            _=>false
        };
        return safe?uri.AbsoluteUri:null;
    }
}

// Updated on the application dispatcher. Generation rejects results from older media.
public sealed class DiscordPresenceState
{
    private PresenceMedia? _media;
    private long _generation;
    private bool _started;
    private PresencePlayback _previous;
    private long? _anchor;
    public DiscordActivity? Activity {get;private set;}
    public long Begin(PresenceMedia? media)
    {_generation++;_media=media;_started=false;_anchor=null;_previous=PresencePlayback.Opening;Activity=null;return _generation;}
    public void Observe(long generation,PresencePlayback playback,long positionMs,long durationMs,DateTimeOffset now,PresenceProgramme? programme=null,bool forceTiming=false,double rate=1)
    {
        if(generation!=_generation)return;
        if(_media is null||playback is PresencePlayback.Stopped or PresencePlayback.Error or PresencePlayback.Ended or PresencePlayback.Opening)
        {Activity=null;_started=false;_anchor=null;_previous=playback;return;}
        if(playback==PresencePlayback.Playing)_started=true;
        if(!_started)return;
        bool running=playback==PresencePlayback.Playing;
        string suffix=running?"":playback==PresencePlayback.Paused?" · Paused":" · Buffering";
        string details=_media.Title,state;
        long? start=null,end=null;
        if(_media.Kind==ContentKind.Live)
        {
            state="Live TV";
            if(programme is not null&&programme.Start<=now&&programme.End>now)
            {
                details=PresenceMedia.PublicText(programme.Title);
                if(details.Length==0)details=_media.Title;
                state=_media.Title+" · Live TV";
                if(running){start=programme.Start.ToUnixTimeSeconds();end=programme.End.ToUnixTimeSeconds();}
            }
        }
        else
        {
            state=_media.Kind==ContentKind.Episode?$"S{_media.Season:00}E{_media.Episode:00} · {_media.EpisodeTitle}":"Movie";
            if(running&&durationMs>0&&durationMs<TimeSpan.FromDays(365).TotalMilliseconds)
            {
                rate=double.IsFinite(rate)&&rate>0?rate:1;
                double position=Math.Clamp(positionMs,0,durationMs)/1000d;
                long candidate=now.ToUnixTimeSeconds()-(long)(position/rate);
                if(_anchor is null||_previous!=PresencePlayback.Playing||forceTiming||Math.Abs(candidate-_anchor.Value)>2)_anchor=candidate;
                start=_anchor;end=start+(long)(durationMs/1000d/rate);
            }
        }
        // Reserve room for status so long episode titles cannot truncate "Paused".
        string label=PresenceMedia.PublicText(state);
        while(Encoding.UTF8.GetByteCount(label+suffix)>120&&label.Length>0)label=label[..^1];
        Activity=new(PresenceMedia.PublicText(details),PresenceMedia.PublicText(label+suffix),start,end,_media.Image);
        _previous=playback;
    }
}
