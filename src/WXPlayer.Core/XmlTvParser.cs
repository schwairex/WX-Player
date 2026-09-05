using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

namespace WXPlayer.Core;

public static class XmlTvParser
{
    public static async IAsyncEnumerable<Programme> ParseAsync(Stream stream,[EnumeratorCancellation]CancellationToken ct=default)
    {
        using var reader=XmlReader.Create(stream,new XmlReaderSettings{Async=true,DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=512L*1024*1024,IgnoreComments=true});
        while(await reader.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            if(reader.NodeType!=XmlNodeType.Element||reader.LocalName!="programme")continue;
            using var sub=reader.ReadSubtree();
            var p=await XElement.LoadAsync(sub,LoadOptions.None,ct);
            if(!TryDate((string?)p.Attribute("start"),out var start)||!TryDate((string?)p.Attribute("stop"),out var stop)||stop<=start)continue;
            string channel=(string?)p.Attribute("channel")??"";
            if(channel.Length==0)continue;
            yield return new Programme(channel,(string?)p.Element("title")??"Program",(string?)p.Element("desc")??"",start,stop);
        }
    }
    public static bool TryDate(string? text,out DateTimeOffset date)
    {
        date=default;if(string.IsNullOrWhiteSpace(text))return false;
        text=text.Trim();var parts=text.Split(' ',StringSplitOptions.RemoveEmptyEntries);
        string value=parts[0],offset=parts.Length>1?parts[1]:"+0000";
        if(value.Length!=14||offset.Length!=5)return false;
        return DateTimeOffset.TryParseExact(value+" "+offset.Insert(3,":"),"yyyyMMddHHmmss zzz",CultureInfo.InvariantCulture,DateTimeStyles.None,out date);
    }
}
