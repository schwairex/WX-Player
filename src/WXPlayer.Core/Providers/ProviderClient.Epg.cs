using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace WXPlayer.Core;

public sealed partial class ProviderClient
{
    public static SourceConfig? XtreamForEpg(SourceConfig source)
    {
        if(source.Kind==SourceKind.Xtream)return source with{};
        if(source.Kind!=SourceKind.Playlist)return null;
        try{if(new Uri(source.Address).Query.Contains("username=",StringComparison.OrdinalIgnoreCase))return ParseXtreamAddress(source with{Kind=SourceKind.Xtream});}catch{}
        return null;
    }
    public async Task DiscoverEpgAsync(SourceConfig source,CancellationToken ct)
    {
        if(source.EpgUrl.Length>0)return;
        if(XtreamForEpg(source) is{} xtream){source.EpgUrl=$"{xtream.Address.TrimEnd('/')}/xmltv.php?username={E(xtream.Username)}&password={E(xtream.Password)}";return;}
        if(source.Kind!=SourceKind.Playlist)return;
        Stream stream;HttpResponseMessage? response=null;
        if(File.Exists(source.Address))stream=File.OpenRead(source.Address);
        else{response=await _http.GetAsync(AddressPolicy.Http(source.Address),HttpCompletionOption.ResponseHeadersRead,ct);response.EnsureSuccessStatusCode();stream=await response.Content.ReadAsStreamAsync(ct);}
        using(response)await using(stream)
        {
            using var reader=new StreamReader(stream);
            // Header discovery is bounded and never reimports a large playlist.
            char[] chars=new char[16384];int count=await reader.ReadAsync(chars.AsMemory(),ct);
            using var header=new StringReader(new string(chars,0,count));
            await foreach(var _ in PlaylistParser.ParseAsync(header,source,ct)){break;}
        }
    }
    public async Task<List<Programme>> ShortEpgAsync(SourceConfig original,ContentItem item,CancellationToken ct)
    {
        var s=XtreamForEpg(original);if(s is null)return [];
        string streamId=item.ProviderId;
        if(original.Kind==SourceKind.Playlist)
        {
            if(!Uri.TryCreate(item.Url,UriKind.Absolute,out var uri))return [];
            streamId=Path.GetFileNameWithoutExtension(uri.AbsolutePath);
            if(!long.TryParse(streamId,out _))return [];
        }
        foreach(string action in new[]{"get_short_epg","get_simple_data_table"})
        {
            try
            {
                using var doc=await JsonAsync(Api(s,action,"&stream_id="+E(streamId)+"&limit=100"),ct);
                var list=new List<Programme>();
                if(doc.RootElement.ValueKind!=JsonValueKind.Object||!doc.RootElement.TryGetProperty("epg_listings",out var rows)||rows.ValueKind!=JsonValueKind.Array)continue;
                TimeZoneInfo zone=TimeZoneInfo.Utc;
                bool needsZone=rows.EnumerateArray().Any(r=>!long.TryParse(Str(r,"start_timestamp"),out _));
                if(needsZone)
                {
                    using var server=await JsonAsync(Api(s),ct);
                    if(server.RootElement.TryGetProperty("server_info",out var info))try{zone=TimeZoneInfo.FindSystemTimeZoneById(Str(info,"timezone","UTC"));}catch(TimeZoneNotFoundException){}
                }
                foreach(var r in rows.EnumerateArray())
                {
                    if(!EpgTime(Str(r,"start_timestamp"),Str(r,"start"),zone,out var a)||!EpgTime(Str(r,"stop_timestamp",Str(r,"end_timestamp")),Str(r,"end",Str(r,"stop")),zone,out var b)||b<=a)continue;
                    list.Add(new(item.EpgId,Decode(Str(r,"title")),Decode(Str(r,"description")),a,b));
                }
                if(list.Count>0)return list.OrderBy(p=>p.Start).Distinct().ToList();
            }
            catch(OperationCanceledException){throw;}
            catch(Exception ex)when(ex is HttpRequestException or JsonException or InvalidOperationException){/* XMLTV and alternate API remain available. */}
        }
        return [];
    }
    private static bool EpgTime(string stamp,string date,TimeZoneInfo zone,out DateTimeOffset result)
    {
        result=default;
        if(long.TryParse(stamp,out long epoch))try{result=DateTimeOffset.FromUnixTimeSeconds(epoch);return true;}catch(ArgumentOutOfRangeException){return false;}
        if(!DateTime.TryParseExact(date,new[]{"yyyy-MM-dd HH:mm:ss","yyyy-MM-dd HH:mm"},CultureInfo.InvariantCulture,DateTimeStyles.None,out var value))return false;
        if(zone.IsInvalidTime(value))return false;
        result=new DateTimeOffset(value,zone.GetUtcOffset(value));return true;
    }
    private static string Decode(string value)
    {
        try{string text=new UTF8Encoding(false,true).GetString(Convert.FromBase64String(value));return text.Any(c=>char.IsControl(c)&&c is not ('\r' or '\n' or '\t'))?value:text;}catch(FormatException){return value;}catch(DecoderFallbackException){return value;}
    }
    public async Task<int> LoadEpgAsync(SourceConfig source,LibraryStore store,CancellationToken ct)
    {
        await DiscoverEpgAsync(source,ct);string address=source.EpgUrl;
        if(address.Length==0)throw new InvalidOperationException("Bu kaynak bir EPG adresi bildirmiyor. Kaynağı düzenleyip sağlayıcınızın XMLTV adresini ekleyin.");
        if(File.Exists(address)){await using var file=File.OpenRead(address);return await ReadEpgStreamAsync(file,source.Id,store,ct);}
        if(Uri.TryCreate(address,UriKind.Absolute,out var local)&&local.IsFile){await using var file=File.OpenRead(local.LocalPath);return await ReadEpgStreamAsync(file,source.Id,store,ct);}
        using var response=await _http.GetAsync(AddressPolicy.Http(address),HttpCompletionOption.ResponseHeadersRead,ct);response.EnsureSuccessStatusCode();
        await using var stream=await response.Content.ReadAsStreamAsync(ct);return await ReadEpgStreamAsync(stream,source.Id,store,ct);
    }
    private static async Task<int> ReadEpgStreamAsync(Stream stream,string id,LibraryStore store,CancellationToken ct)
    {
        byte[] prefix=new byte[2];int count=0;while(count<2){int n=await stream.ReadAsync(prefix.AsMemory(count,2-count),ct);if(n==0)break;count+=n;}
        using var joined=new PrefixStream(prefix.AsMemory(0,count).ToArray(),stream);
        // Inspect magic bytes: .php endpoints can return gzip, and HTTP may already decompress .gz.
        if(count==2&&prefix[0]==0x1f&&prefix[1]==0x8b){using var gzip=new GZipStream(joined,CompressionMode.Decompress,true);return await store.ImportXmlTvAsync(id,XmlTvParser.ReadAsync(gzip,ct),ct);}
        return await store.ImportXmlTvAsync(id,XmlTvParser.ReadAsync(joined,ct),ct);
    }
    private sealed class PrefixStream(byte[] prefix,Stream inner):Stream
    {
        private int _position;
        public override int Read(byte[] buffer,int offset,int count){if(_position<prefix.Length){int n=Math.Min(count,prefix.Length-_position);Array.Copy(prefix,_position,buffer,offset,n);_position+=n;return n;}return inner.Read(buffer,offset,count);}
        public override ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken ct=default){if(_position<prefix.Length){int n=Math.Min(buffer.Length,prefix.Length-_position);prefix.AsMemory(_position,n).CopyTo(buffer);_position+=n;return ValueTask.FromResult(n);}return inner.ReadAsync(buffer,ct);}
        public override Task<int> ReadAsync(byte[] buffer,int offset,int count,CancellationToken ct)=>ReadAsync(buffer.AsMemory(offset,count),ct).AsTask();
        public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;
        public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
        public override void Flush(){}public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();public override void SetLength(long value)=>throw new NotSupportedException();public override void Write(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
    }
}
