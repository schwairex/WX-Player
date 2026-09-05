using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace WXPlayer.Core;

public sealed record XmlTvEntry(string ChannelId, IReadOnlyList<string>? Names = null, Programme? Programme = null);

/// <summary>Streaming XMLTV EPG Parser. External DTDs are ignored, never downloaded.</summary>
public static class XmlTvParser
{
    public static async IAsyncEnumerable<XmlTvEntry> ReadAsync(Stream stream,[EnumeratorCancellation] CancellationToken ct=default)
    {
        using var reader=XmlReader.Create(stream,new XmlReaderSettings{Async=true,DtdProcessing=DtdProcessing.Ignore,XmlResolver=null,MaxCharactersInDocument=512L*1024*1024,IgnoreComments=true});
        var pending=new Dictionary<string,Programme>();
        while(await reader.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            if(reader.NodeType!=XmlNodeType.Element || reader.LocalName is not ("channel" or "programme"))continue;
            string type=reader.LocalName;
            using var sub=reader.ReadSubtree();
            var p=await XElement.LoadAsync(sub,LoadOptions.None,ct);
            if(type=="channel")
            {
                string id=((string?)p.Attribute("id")??"").Trim();
                if(id.Length>0)yield return new(id,p.Elements().Where(e=>e.Name.LocalName=="display-name").Select(e=>e.Value.Trim()).Where(n=>n.Length>0).ToArray());
                continue;
            }
            string channel=((string?)p.Attribute("channel")??"").Trim();
            if(channel.Length==0 || !TryDate((string?)p.Attribute("start"),out var start))continue;
            if(pending.Remove(channel,out var previous) && start>previous.Start && start-previous.Start<=TimeSpan.FromDays(1))
                yield return new(channel,Programme:previous with{End=start});
            string Text(string name)=>p.Elements().Where(e=>e.Name.LocalName==name).OrderByDescending(e=>(string?)e.Attribute("lang")=="tr").FirstOrDefault()?.Value.Trim()??"";
            var programme=new Programme(channel,Text("title") is {Length:>0} title?title:"Program",Text("desc"),start,start);
            if(TryDate((string?)p.Attribute("stop"),out var stop) && stop>start)yield return new(channel,Programme:programme with{End=stop});
            else if(pending.Count<100000)pending[channel]=programme;
        }
        // No invented duration for a final programme without a stop.
    }
    public static async IAsyncEnumerable<Programme> ParseAsync(Stream stream,[EnumeratorCancellation]CancellationToken ct=default)
    {await foreach(var entry in ReadAsync(stream,ct))if(entry.Programme is{} p)yield return p;}
    public static bool TryDate(string? text,out DateTimeOffset date)
    {
        date=default;if(string.IsNullOrWhiteSpace(text))return false;
        var parts=text.Trim().Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);
        string value=parts[0],offset=parts.Length>1?parts[1]:"+0000";
        offset=offset.ToUpperInvariant() switch{"UTC" or "GMT" or "Z"=>"+0000","BST"=>"+0100",_=>offset};
        if(value.Length is not (12 or 14))return false;
        if(value.Length==12)value+="00";
        if(offset.Length==5)offset=offset.Insert(3,":");
        return DateTimeOffset.TryParseExact(value+" "+offset,"yyyyMMddHHmmss zzz",CultureInfo.InvariantCulture,DateTimeStyles.None,out date);
    }
}

/// <summary>Channel Matcher: exact IDs first, then unique normalized names. No guessed fuzzy matches.</summary>
public static class EpgChannelMatcher
{
    public static string Key(string value)=>string.Concat(ContentItem.SearchKey(value.Trim()).Where(char.IsLetterOrDigit));
    public static string NameKey(string value)
    {
        string normalized=ContentItem.SearchKey(value);
        normalized=Regex.Replace(normalized,@"^\s*(tr|tur|turkey|turkiye)\s*[:|\-]\s*", "");
        normalized=Regex.Replace(normalized,@"\b(uhd|fhd|hd|sd|4k|8k|hevc|h265|h264)\b", "");
        return Key(normalized);
    }
}
