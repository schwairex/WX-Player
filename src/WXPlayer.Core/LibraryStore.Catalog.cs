using Microsoft.Data.Sqlite;

namespace WXPlayer.Core;

public sealed partial class LibraryStore
{
    private static void InitializeCatalog(SqliteConnection c)
    {
        using var tx=c.BeginTransaction();using var cmd=c.CreateCommand();cmd.Transaction=tx;
        foreach(var (name,type) in new[]{("series_id","TEXT NOT NULL DEFAULT ''"),("series_name","TEXT NOT NULL DEFAULT ''"),("season","INTEGER NOT NULL DEFAULT 0"),("episode","INTEGER NOT NULL DEFAULT 0")})
        {cmd.CommandText=$"SELECT COUNT(*) FROM pragma_table_info('items') WHERE name='{name}'";if(Convert.ToInt32(cmd.ExecuteScalar())==0){cmd.CommandText=$"ALTER TABLE items ADD COLUMN {name} {type}";cmd.ExecuteNonQuery();}}
        cmd.CommandText="""
            CREATE INDEX IF NOT EXISTS ix_items_series ON items(series_id,season,episode);
            CREATE TABLE IF NOT EXISTS playback_progress(item TEXT PRIMARY KEY,source TEXT NOT NULL,series TEXT NOT NULL,name TEXT NOT NULL,position INTEGER NOT NULL,duration INTEGER NOT NULL,completed INTEGER NOT NULL,updated INTEGER NOT NULL);
            CREATE INDEX IF NOT EXISTS ix_progress_series ON playback_progress(series,updated DESC);
            CREATE TABLE IF NOT EXISTS catalog_meta(version INTEGER PRIMARY KEY);
            SELECT COUNT(*) FROM catalog_meta WHERE version=151;
            """;
        bool migrated=Convert.ToInt32(cmd.ExecuteScalar())>0;
        if(!migrated)
        {
            cmd.CommandText="SELECT secret FROM sources";var sources=new List<SourceConfig>();using(var r=cmd.ExecuteReader())while(r.Read())sources.Add(SecretVault.Unprotect(r.GetString(0)));
            foreach(var source in sources.Where(s=>s.Kind==SourceKind.Playlist))
            {
                string last="";
                while(true)
                {
                    cmd.Parameters.Clear();cmd.CommandText="SELECT id,name,category,kind,url FROM items WHERE source=$s AND id>$last ORDER BY id LIMIT 500";cmd.Parameters.AddWithValue("$s",source.Id);cmd.Parameters.AddWithValue("$last",last);
                    var batch=new List<ContentItem>();using(var r=cmd.ExecuteReader())while(r.Read())batch.Add(new ContentItem{Id=r.GetString(0),SourceId=source.Id,Name=r.GetString(1),Category=r.GetString(2),Kind=(ContentKind)r.GetInt32(3),Url=r.GetString(4)});
                    if(batch.Count==0)break;last=batch[^1].Id;
                    foreach(var original in batch)
                    {
                        var item=CatalogClassifier.Normalize(original,SourceKind.Playlist);if(item.Kind==original.Kind&&item.SeriesId.Length==0)continue;
                        cmd.Parameters.Clear();cmd.CommandText="UPDATE items SET kind=$k,series_id=$p,series_name=$n,season=$s,episode=$e WHERE id=$id";
                        cmd.Parameters.AddWithValue("$k",(int)item.Kind);cmd.Parameters.AddWithValue("$p",item.SeriesId);cmd.Parameters.AddWithValue("$n",item.SeriesName);cmd.Parameters.AddWithValue("$s",item.Season);cmd.Parameters.AddWithValue("$e",item.Episode);cmd.Parameters.AddWithValue("$id",item.Id);cmd.ExecuteNonQuery();
                    }
                }
                RebuildSeries(c,tx,source.Id);
            }
            cmd.Parameters.Clear();cmd.CommandText="UPDATE history SET played=played*1000 WHERE played<100000000000;INSERT INTO catalog_meta VALUES(151)";cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }
    private static void RebuildSeries(SqliteConnection c,SqliteTransaction tx,string source)
    {
        c.CreateFunction<string,string>("wx_normalize",ContentItem.SearchKey);
        using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.Parameters.AddWithValue("$s",source);
        cmd.CommandText="""
            INSERT OR REPLACE INTO items(id,source,provider,name,category,kind,url,logo,epg,extension,catchup,days,ua,referrer,search_text,epg_name,series_id,series_name,season,episode)
            SELECT series_id,source,'',MIN(series_name),MIN(category),2,'',MAX(logo),'','mp4','',0,'','',wx_normalize(MIN(series_name)||' '||MIN(category)),'','','',0,0
            FROM items WHERE source=$s AND kind=3 AND series_id<>'' GROUP BY series_id;
            INSERT OR IGNORE INTO favorites SELECT DISTINCT series_id FROM items i JOIN favorites f ON f.id=i.id WHERE i.source=$s AND i.series_id<>'';
            DELETE FROM favorites WHERE id IN(SELECT id FROM items WHERE source=$s AND kind=3 AND series_id<>'');
            INSERT INTO history SELECT series_id,MAX(h.played) FROM items i JOIN history h ON h.id=i.id WHERE i.source=$s AND i.series_id<>'' GROUP BY series_id
            ON CONFLICT(id) DO UPDATE SET played=MAX(history.played,excluded.played);
            """;
        cmd.ExecuteNonQuery();
    }
    public async Task<ContentItem?> FindAsync(string id,CancellationToken ct=default)=>(await QueryAsync(null,null,null,"",false,false,0,1,ct,itemId:id)).Items.FirstOrDefault();
    public async Task<ContentItem?> RecommendationAsync(string? source,string? previous,CancellationToken ct=default,bool artworkOnly=false)
    {
        var page=await QueryAsync(source,null,null,"",false,false,0,1,ct,recommend:true,exceptId:previous,artworkOnly:artworkOnly);
        return page.Items.FirstOrDefault()??(await QueryAsync(source,null,null,"",false,false,0,1,ct,recommend:true,artworkOnly:artworkOnly)).Items.FirstOrDefault();
    }
    public async Task<IReadOnlyList<ContentItem>> PlaylistEpisodesAsync(ContentItem series,CancellationToken ct=default)
        =>(await QueryAsync(series.SourceId,ContentKind.Episode,null,"",false,false,0,int.MaxValue,ct,parent:series.Id)).Items;
    public Task<Dictionary<string,WatchProgress>> ProgressForSeriesAsync(string series,CancellationToken ct=default)=>Task.Run(()=>
    {
        using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT item,source,series,name,position,duration,completed,updated FROM playback_progress WHERE series=$s";cmd.Parameters.AddWithValue("$s",series);
        var result=new Dictionary<string,WatchProgress>();using var r=cmd.ExecuteReader();while(r.Read()){ct.ThrowIfCancellationRequested();var p=ReadProgress(r);result[p.ItemId]=p;}return result;
    },ct);
    public Task<WatchProgress?> ProgressAsync(string item)=>Task.Run(()=>
    {using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT item,source,series,name,position,duration,completed,updated FROM playback_progress WHERE item=$i";cmd.Parameters.AddWithValue("$i",item);using var r=cmd.ExecuteReader();return r.Read()?ReadProgress(r):null;});
    private static WatchProgress ReadProgress(SqliteDataReader r)=>new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetInt64(4),r.GetInt64(5),r.GetBoolean(6),r.GetInt64(7));
    public Task SaveProgressAsync(ContentItem item,long position,long duration,bool completed=false)=>WriteAsync(c=>
    {
        if(item.Kind is not(ContentKind.Movie or ContentKind.Episode)||position<0)return;
        duration=Math.Max(0,duration);position=duration>0?Math.Clamp(position,0,duration):position;completed|=duration>0&&position>=Math.Max(duration*.95,duration-15000);
        using var cmd=c.CreateCommand();cmd.CommandText="""
            INSERT INTO playback_progress VALUES($i,$s,$p,$n,$pos,$dur,$done,$t)
            ON CONFLICT(item) DO UPDATE SET name=excluded.name,series=excluded.series,position=excluded.position,duration=excluded.duration,completed=excluded.completed,updated=excluded.updated;
            INSERT OR REPLACE INTO history SELECT $owner,$t WHERE EXISTS(SELECT 1 FROM items WHERE id=$owner);
            """;
        cmd.Parameters.AddWithValue("$i",item.Id);cmd.Parameters.AddWithValue("$s",item.SourceId);cmd.Parameters.AddWithValue("$p",item.SeriesId);cmd.Parameters.AddWithValue("$n",item.Kind==ContentKind.Episode&&item.Episode>0?(item.Season>0?$"S{item.Season:00} · B{item.Episode:00}":$"Bölüm {item.Episode}"):item.Name);cmd.Parameters.AddWithValue("$pos",position);cmd.Parameters.AddWithValue("$dur",duration);cmd.Parameters.AddWithValue("$done",completed);cmd.Parameters.AddWithValue("$t",DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());cmd.Parameters.AddWithValue("$owner",item.SeriesId.Length>0?item.SeriesId:item.Id);cmd.ExecuteNonQuery();
    });
}

