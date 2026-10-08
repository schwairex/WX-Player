using System.Text.RegularExpressions;

namespace WXPlayer.Core;

/// <summary>Conservative episode identification. Provider API content kinds remain authoritative.</summary>
public static partial class CatalogClassifier
{
    [GeneratedRegex(@"\bS(?<s>\d{1,3})[\s._|\-]*[EB](?<e>\d{1,4})\b|\b(?<s>\d{1,2})x(?<e>\d{1,3})\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant)]
    private static partial Regex Numbered();
    [GeneratedRegex(@"\b(?<e>\d{1,4})\s*\.?\s*B[oö]l[uü]m\b|\bEpisode\s*(?<e>\d{1,4})\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant)]
    private static partial Regex NamedEpisode();
    [GeneratedRegex(@"\bS(?<s>\d{1,3})\b|\b(?<s>\d{1,3})\s*\.?\s*Sezon\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant)]
    private static partial Regex SeasonMarker();
    [GeneratedRegex(@"\b(dizi|diziler|series|sezon)\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant)]
    private static partial Regex SeriesCategory();
    [GeneratedRegex(@"\b(film|filmler|movie|movies|vod)\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant)]
    private static partial Regex MovieCategory();

    public static ContentItem Normalize(ContentItem item,SourceKind sourceKind)
    {
        if(sourceKind!=SourceKind.Playlist||item.SeriesId.Length>0)return item;
        var number=Numbered().Match(item.Name);var episode=NamedEpisode().Match(item.Name);var season=SeasonMarker().Match(item.Name);
        bool hasEpisode=number.Success||episode.Success;
        string urlPath=Uri.TryCreate(item.Url,UriKind.Absolute,out var uri)?uri.AbsolutePath.ToLowerInvariant():"";
        bool isSeries=hasEpisode||season.Success||SeriesCategory().IsMatch(item.Category)||urlPath.Contains("/series/");
        if(isSeries)
        {
            int cut=item.Name.Length;
            foreach(var match in new[]{number,episode,season})if(match.Success)cut=Math.Min(cut,match.Index);
            string title=item.Name[..cut].Trim(' ','-','·','|','.',':','_');if(title.Length==0)title=item.Name;
            int s=number.Success?int.Parse(number.Groups["s"].Value):season.Success?int.Parse(season.Groups["s"].Value):0;
            int e=number.Success?int.Parse(number.Groups["e"].Value):episode.Success?int.Parse(episode.Groups["e"].Value):0;
            return item with{Kind=ContentKind.Episode,SeriesId=ContentItem.Key(item.SourceId,"show:"+ContentItem.SearchKey(title)),SeriesName=title,Season=s,Episode=e};
        }
        bool movie=urlPath.Contains("/movie/")||MovieCategory().IsMatch(item.Category)||(item.Kind!=ContentKind.Live&&new[]{".mp4",".mkv",".avi",".mov",".m4v"}.Contains(Path.GetExtension(urlPath)));
        return item with{Kind=movie?ContentKind.Movie:ContentKind.Live};
    }
}

public sealed record WatchProgress(string ItemId,string SourceId,string SeriesId,string Name,long PositionMs,long DurationMs,bool Completed,long Updated)
{
    public double Fraction=>DurationMs>0?Math.Clamp((double)PositionMs/DurationMs,0,1):0;
    public string Label=>Completed?"İzlendi":DurationMs>0?$"{Clock(PositionMs)} / {Clock(DurationMs)}":$"{Clock(PositionMs)} · Süre bilinmiyor";
    public static string Clock(long ms)=>TimeSpan.FromMilliseconds(Math.Max(0,ms)).ToString(ms>=3600000?@"h\:mm\:ss":@"mm\:ss");
}
