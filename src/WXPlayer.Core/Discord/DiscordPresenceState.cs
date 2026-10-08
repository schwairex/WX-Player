using System.Text;
using System.Text.RegularExpressions;
using System.Net;

namespace WXPlayer.Core;

public enum PresencePlayback { Opening, Playing, Paused, Buffering, Stopped, Error, Ended }
public sealed record DiscordActivity(string Details,string State,long? Start,long? End,string? Image,bool PublicCatalogueImage=false);
public sealed record PresenceProgramme(string Title,DateTimeOffset Start,DateTimeOffset End);

// Deliberately excludes source config, stream address, headers, IDs and credentials.
public sealed record PresenceMedia(ContentKind Kind,string Title,string EpisodeTitle,int Season,int Episode,string? Image,bool PublicCatalogueImage=false)
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
    public static string? PublicCatalogueArtwork(DiscoveredArtwork artwork,IEnumerable<string>? privateValues=null)
    {
        if(artwork.Provider is not ("IPTV-org" or "TVmaze" or "Wikipedia"))return null;
        if(!Uri.TryCreate(artwork.Url,UriKind.Absolute,out var uri)||uri.Scheme!="https"||!uri.IsDefaultPort||
           uri.UserInfo.Length>0||uri.Query.Length>0||uri.Fragment.Length>0||uri.AbsoluteUri.Length>512||
           uri.HostNameType!=UriHostNameType.Dns||uri.Host.Equals("localhost",StringComparison.OrdinalIgnoreCase)||
           uri.Host.EndsWith(".local",StringComparison.OrdinalIgnoreCase)||uri.Host.EndsWith(".internal",StringComparison.OrdinalIgnoreCase)||
           uri.Host.EndsWith(".test",StringComparison.OrdinalIgnoreCase)||uri.AbsolutePath.Contains('%')||
           !Regex.IsMatch(uri.AbsolutePath,@"\.(?:jpg|jpeg|png|webp|gif)$",RegexOptions.IgnoreCase))return null;
        if(IPAddress.TryParse(uri.Host,out _))return null;
        if(privateValues?.Any(s=>!string.IsNullOrWhiteSpace(s)&&s.Length>=3&&uri.AbsoluteUri.Contains(s,StringComparison.OrdinalIgnoreCase))==true)return null;
        return uri.AbsoluteUri;
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
        Activity=new(PresenceMedia.PublicText(details),PresenceMedia.PublicText(label+suffix),start,end,_media.Image,_media.PublicCatalogueImage);
        _previous=playback;
    }
    public bool SetArtwork(long generation,DiscoveredArtwork artwork,IEnumerable<string>? privateValues=null)
    {
        if(generation!=_generation||_media is null||!_started||Activity is null)return false;
        string? image=PresenceMedia.PublicCatalogueArtwork(artwork,privateValues);
        if(image is null)return false;
        _media=_media with{Image=image,PublicCatalogueImage=true};
        Activity=Activity with{Image=image,PublicCatalogueImage=true};
        return true;
    }
}
