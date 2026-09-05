using Microsoft.Data.Sqlite;
using System.Runtime.CompilerServices;

namespace WXPlayer.Core;

public enum LibraryCleanup { Favorites, History, Sources }
public sealed partial class LibraryStore
{
    public Task ClearAsync(LibraryCleanup kind)=>WriteAsync(c=>
    {
        using var tx=c.BeginTransaction();using var cmd=c.CreateCommand();cmd.Transaction=tx;
        cmd.CommandText=kind switch{
            LibraryCleanup.Favorites=>"DELETE FROM favorites",
            LibraryCleanup.History=>"DELETE FROM history",
            LibraryCleanup.Sources=>"DELETE FROM favorites;DELETE FROM history;DELETE FROM epg_matches;DELETE FROM epg_aliases;DELETE FROM epg_state;DELETE FROM epg;DELETE FROM items;DELETE FROM sources;",
            _=>throw new ArgumentOutOfRangeException(nameof(kind))};
        cmd.ExecuteNonQuery();tx.Commit();
    });
    public Task SaveSourceAsync(SourceConfig s)=>WriteAsync(c=>{using var cmd=c.CreateCommand();cmd.CommandText="UPDATE sources SET secret=$v WHERE id=$id";cmd.Parameters.AddWithValue("$id",s.Id);cmd.Parameters.AddWithValue("$v",SecretVault.Protect(s));cmd.ExecuteNonQuery();});
    public Task SetEpgMatchAsync(string item,string channel)=>WriteAsync(c=>{using var cmd=c.CreateCommand();cmd.CommandText=channel.Length==0?"DELETE FROM epg_matches WHERE item=$i":"INSERT OR REPLACE INTO epg_matches VALUES($i,$c)";cmd.Parameters.AddWithValue("$i",item);cmd.Parameters.AddWithValue("$c",channel);cmd.ExecuteNonQuery();});
    public Task<List<string>> EpgChannelsAsync(string source,string search)=>Task.Run(()=>
    {
        using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT DISTINCT channel FROM epg_aliases WHERE source=$s AND ($q='' OR instr(key,$q)>0) ORDER BY channel LIMIT 100";
        cmd.Parameters.AddWithValue("$s",source);cmd.Parameters.AddWithValue("$q",EpgChannelMatcher.Key(search));using var r=cmd.ExecuteReader();var list=new List<string>();while(r.Read())list.Add(r.GetString(0));return list;
    });
    public Task<DateTimeOffset?> EpgUpdatedAsync(string source)=>Task.Run(()=>
    {
        using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT updated FROM epg_state WHERE source=$s";cmd.Parameters.AddWithValue("$s",source);
        return cmd.ExecuteScalar() is long value?(DateTimeOffset?)DateTimeOffset.FromUnixTimeSeconds(value):null;
    });
    public Task<int> ImportEpgAsync(string source,IAsyncEnumerable<Programme> programmes,CancellationToken ct)
    {
        async IAsyncEnumerable<XmlTvEntry> Entries([EnumeratorCancellation]CancellationToken token=default){await foreach(var p in programmes.WithCancellation(token))yield return new(p.ChannelId,Programme:p);}
        return ImportXmlTvAsync(source,Entries(ct),ct);
    }
    public async Task<int> ImportXmlTvAsync(string source,IAsyncEnumerable<XmlTvEntry> entries,CancellationToken ct)
    {
        await _writer.WaitAsync(ct);try{return await Task.Run(async()=>
        {
            using var c=Open();using var tx=c.BeginTransaction();using var cmd=c.CreateCommand();cmd.Transaction=tx;
            cmd.CommandText="DELETE FROM epg WHERE source=$s;DELETE FROM epg_aliases WHERE source=$s";cmd.Parameters.AddWithValue("$s",source);cmd.ExecuteNonQuery();
            cmd.CommandText="INSERT INTO epg VALUES($s,$c,$t,$d,$start,$end)";
            foreach(var p in new[]{"$c","$t","$d","$start","$end"})cmd.Parameters.AddWithValue(p,"");cmd.Prepare();
            using var alias=c.CreateCommand();alias.Transaction=tx;alias.CommandText="INSERT OR IGNORE INTO epg_aliases VALUES($s,$c,$k,$kind)";
            foreach(var p in new[]{"$s","$c","$k","$kind"})alias.Parameters.AddWithValue(p,"");alias.Parameters["$s"].Value=source;alias.Prepare();
            void Alias(string channel,string value,int kind){if(value.Length==0)return;alias.Parameters["$c"].Value=channel;alias.Parameters["$k"].Value=value;alias.Parameters["$kind"].Value=kind;alias.ExecuteNonQuery();}
            var seen=new HashSet<string>();int count=0;
            await foreach(var entry in entries.WithCancellation(ct))
            {
                ct.ThrowIfCancellationRequested();
                if(seen.Add(entry.ChannelId))Alias(entry.ChannelId,EpgChannelMatcher.Key(entry.ChannelId),0);
                if(entry.Names is{} names)foreach(string name in names){Alias(entry.ChannelId,EpgChannelMatcher.Key(name),1);Alias(entry.ChannelId,EpgChannelMatcher.NameKey(name),2);}
                if(entry.Programme is not{} p)continue;
                cmd.Parameters["$c"].Value=p.ChannelId;cmd.Parameters["$t"].Value=p.Title;cmd.Parameters["$d"].Value=p.Description;cmd.Parameters["$start"].Value=p.Start.ToUnixTimeSeconds();cmd.Parameters["$end"].Value=p.End.ToUnixTimeSeconds();cmd.ExecuteNonQuery();count++;
            }
            if(count==0)throw new InvalidOperationException("XMLTV program verisi içermiyor; önceki rehber korundu.");
            using var state=c.CreateCommand();state.Transaction=tx;state.CommandText="INSERT OR REPLACE INTO epg_state VALUES($s,$t)";state.Parameters.AddWithValue("$s",source);state.Parameters.AddWithValue("$t",DateTimeOffset.UtcNow.ToUnixTimeSeconds());state.ExecuteNonQuery();
            ct.ThrowIfCancellationRequested();tx.Commit();return count;
        },ct);}finally{_writer.Release();}
    }
    private static string? Match(SqliteConnection c,ContentItem item)
    {
        using var cmd=c.CreateCommand();cmd.Parameters.AddWithValue("$s",item.SourceId);cmd.Parameters.AddWithValue("$i",item.Id);
        cmd.CommandText="SELECT channel FROM epg_matches WHERE item=$i";
        if(cmd.ExecuteScalar() is string manual)return manual;
        cmd.Parameters.AddWithValue("$k",item.EpgId.Trim());
        if(item.EpgId.Length>0){cmd.CommandText="SELECT channel FROM epg WHERE source=$s AND channel=$k LIMIT 1";if(cmd.ExecuteScalar() is string exact)return exact;}
        cmd.Parameters.AddWithValue("$kind",0);
        var candidates=new[]{(0,EpgChannelMatcher.Key(item.EpgId)),(1,EpgChannelMatcher.Key(item.EpgName)),(1,EpgChannelMatcher.Key(item.Name)),(0,EpgChannelMatcher.Key(item.EpgName)),(0,EpgChannelMatcher.Key(item.Name)),(2,EpgChannelMatcher.NameKey(item.EpgName)),(2,EpgChannelMatcher.NameKey(item.Name))};
        foreach(var (kind,key) in candidates)
        {
            if(key.Length==0)continue;
            cmd.Parameters["$k"].Value=key;cmd.Parameters["$kind"].Value=kind;
            cmd.CommandText="SELECT DISTINCT channel FROM epg_aliases WHERE source=$s AND kind=$kind AND key=$k LIMIT 2";
            using var r=cmd.ExecuteReader();if(!r.Read())continue;string id=r.GetString(0);if(r.Read())return null;return id;
        }
        // Read old caches created before alias indexing was introduced.
        cmd.Parameters["$k"].Value=item.Name;cmd.CommandText="SELECT channel FROM epg WHERE source=$s AND channel=$k LIMIT 1";return cmd.ExecuteScalar() as string;
    }
    public Task<List<Programme>> EpgAsync(ContentItem item,DateTimeOffset date)=>Task.Run(()=>
    {
        using var c=Open();string? channel=Match(c,item);if(channel is null)return new List<Programme>();
        using var cmd=c.CreateCommand();cmd.CommandText="SELECT DISTINCT channel,title,description,start,end FROM epg WHERE source=$s AND channel=$c AND end>$from AND start<$to ORDER BY start LIMIT 500";
        cmd.Parameters.AddWithValue("$s",item.SourceId);cmd.Parameters.AddWithValue("$c",channel);cmd.Parameters.AddWithValue("$from",date.ToUnixTimeSeconds());cmd.Parameters.AddWithValue("$to",date.AddDays(1).ToUnixTimeSeconds());
        using var r=cmd.ExecuteReader();var list=new List<Programme>();while(r.Read())list.Add(new(r.GetString(0),r.GetString(1),r.GetString(2),DateTimeOffset.FromUnixTimeSeconds(r.GetInt64(3)),DateTimeOffset.FromUnixTimeSeconds(r.GetInt64(4))));return list;
    });
}
