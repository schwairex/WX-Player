using Microsoft.Data.Sqlite;

namespace WXPlayer.Core;

public sealed class LibraryStore(string path)
{
    private readonly SemaphoreSlim _writer = new(1, 1);
    private SqliteConnection Open()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = true, DefaultTimeout = 30 }.ToString());
        c.Open(); return c;
    }
    public Task InitializeAsync() => Task.Run(() =>
    {
        using var c = Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS sources(id TEXT PRIMARY KEY,secret TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS items(id TEXT PRIMARY KEY,source TEXT NOT NULL,provider TEXT,name TEXT NOT NULL,category TEXT,kind INTEGER,url TEXT,logo TEXT,epg TEXT,extension TEXT,catchup TEXT,days INTEGER,ua TEXT,referrer TEXT,search_text TEXT NOT NULL DEFAULT '');
            CREATE INDEX IF NOT EXISTS ix_items_filter ON items(source,kind,category,name COLLATE NOCASE);
            CREATE TABLE IF NOT EXISTS favorites(id TEXT PRIMARY KEY);
            CREATE TABLE IF NOT EXISTS history(id TEXT PRIMARY KEY,played INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS epg(source TEXT,channel TEXT,title TEXT,description TEXT,start INTEGER,end INTEGER);
            CREATE INDEX IF NOT EXISTS ix_epg ON epg(source,channel,start);
            """;
        cmd.ExecuteNonQuery();
        cmd.CommandText="SELECT COUNT(*) FROM pragma_table_info('items') WHERE name='search_text'";
        if(Convert.ToInt32(cmd.ExecuteScalar())==0)
        {
            cmd.CommandText="ALTER TABLE items ADD COLUMN search_text TEXT NOT NULL DEFAULT ''";cmd.ExecuteNonQuery();
            c.CreateFunction<string,string>("wx_normalize",ContentItem.SearchKey);cmd.CommandText="UPDATE items SET search_text=wx_normalize(name||' '||category)";cmd.ExecuteNonQuery();
        }
    });
    public Task<List<SourceConfig>> SourcesAsync() => Task.Run(() =>
    {
        using var c = Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT secret FROM sources ORDER BY rowid";
        using var r = cmd.ExecuteReader(); var list = new List<SourceConfig>();
        while (r.Read()) list.Add(SecretVault.Unprotect(r.GetString(0)));
        return list;
    });
    public async Task<int> ImportAsync(SourceConfig source, IAsyncEnumerable<ContentItem> input, IProgress<ImportProgress>? progress, CancellationToken ct)
    {
        await _writer.WaitAsync(ct);
        try
        {
            return await Task.Run(async () =>
            {
                using var c = Open(); using var tx = c.BeginTransaction();
                using (var del = c.CreateCommand()) { del.Transaction = tx; del.CommandText = "DELETE FROM items WHERE source=$s"; del.Parameters.AddWithValue("$s", source.Id); del.ExecuteNonQuery(); }
                using var insert = c.CreateCommand(); insert.Transaction = tx;
                insert.CommandText = "INSERT OR REPLACE INTO items VALUES($id,$s,$p,$n,$c,$k,$u,$l,$e,$x,$a,$d,$ua,$r,$search)";
                foreach (var key in new[] { "$id", "$s", "$p", "$n", "$c", "$k", "$u", "$l", "$e", "$x", "$a", "$d", "$ua", "$r", "$search" }) insert.Parameters.Add(new SqliteParameter(key, ""));
                insert.Prepare(); int count = 0;
                await foreach (var item in input.WithCancellation(ct))
                {
                    ct.ThrowIfCancellationRequested();
                    object[] values = [item.Id, source.Id, item.ProviderId, item.Name, item.Category, (int)item.Kind, item.Url, item.Logo, item.EpgId, item.Extension, item.Catchup, item.CatchupDays, item.UserAgent, item.Referrer, ContentItem.SearchKey(item.Name+" "+item.Category)];
                    for (int i = 0; i < values.Length; i++) insert.Parameters[i].Value = values[i];
                    insert.ExecuteNonQuery(); count++;
                    if (count % 500 == 0) progress?.Report(new(count, $"{count:N0} içerik işleniyor…"));
                }
                if (count == 0) throw new InvalidOperationException("Kaynakta oynatılabilir içerik bulunamadı. Önceki kütüphane korundu.");
                ct.ThrowIfCancellationRequested();
                source.UpdatedAt = DateTimeOffset.Now;
                using var save = c.CreateCommand(); save.Transaction = tx; save.CommandText = "INSERT OR REPLACE INTO sources VALUES($id,$secret)";
                save.Parameters.AddWithValue("$id", source.Id); save.Parameters.AddWithValue("$secret", SecretVault.Protect(source)); save.ExecuteNonQuery();
                tx.Commit(); progress?.Report(new(count, $"{count:N0} içerik hazır")); return count;
            }, ct);
        }
        finally { _writer.Release(); }
    }
    public Task<Page> QueryAsync(string? source, ContentKind? kind, string? category, string search, bool favorites, bool recent, int offset, int limit = 150, CancellationToken ct = default) => Task.Run(() =>
    {
        using var c = Open(); using var cmd = c.CreateCommand();
        using var reg = ct.Register(cmd.Cancel);
        var where = " WHERE ($s='' OR i.source=$s) AND ($k=-1 OR i.kind=$k) AND ($c='' OR i.category=$c) AND ($q='' OR i.search_text LIKE $q ESCAPE '~')";
        if (favorites) where += " AND f.id IS NOT NULL";
        if (recent) where += " AND h.id IS NOT NULL";
        string from = " FROM items i LEFT JOIN favorites f ON f.id=i.id LEFT JOIN history h ON h.id=i.id";
        cmd.Parameters.AddWithValue("$s", source ?? ""); cmd.Parameters.AddWithValue("$k", kind is null ? -1 : (int)kind);
        cmd.Parameters.AddWithValue("$c", category ?? "");
        cmd.Parameters.AddWithValue("$q", search.Length == 0 ? "" : "%" + ContentItem.SearchKey(search).Replace("~", "~~").Replace("%", "~%").Replace("_", "~_") + "%");
        cmd.CommandText = "SELECT COUNT(*)" + from + where;
        int total = Convert.ToInt32(cmd.ExecuteScalar()); ct.ThrowIfCancellationRequested();
        cmd.CommandText = "SELECT i.id,i.source,i.provider,i.name,i.category,i.kind,i.url,i.logo,i.epg,i.extension,i.catchup,i.days,i.ua,i.referrer,f.id IS NOT NULL" + from + where + (recent ? " ORDER BY h.played DESC" : " ORDER BY i.name COLLATE NOCASE") + " LIMIT $limit OFFSET $offset";
        cmd.Parameters.AddWithValue("$limit", limit); cmd.Parameters.AddWithValue("$offset", offset);
        using var r = cmd.ExecuteReader(); var list = new List<ContentItem>();
        while (r.Read()) { ct.ThrowIfCancellationRequested(); list.Add(new ContentItem { Id=r.GetString(0),SourceId=r.GetString(1),ProviderId=r.GetString(2),Name=r.GetString(3),Category=r.GetString(4),Kind=(ContentKind)r.GetInt32(5),Url=r.GetString(6),Logo=r.GetString(7),EpgId=r.GetString(8),Extension=r.GetString(9),Catchup=r.GetString(10),CatchupDays=r.GetInt32(11),UserAgent=r.GetString(12),Referrer=r.GetString(13),IsFavorite=r.GetBoolean(14) }); }
        return new Page(list,total);
    }, ct);
    public Task<LibraryStats> StatsAsync(string? source) => Task.Run(() =>
    {
        using var c=Open(); using var cmd=c.CreateCommand(); cmd.CommandText="SELECT kind,COUNT(*) FROM items WHERE ($s='' OR source=$s) GROUP BY kind"; cmd.Parameters.AddWithValue("$s",source??"");
        var counts=new int[4]; using(var r=cmd.ExecuteReader()) while(r.Read()) counts[r.GetInt32(0)]=r.GetInt32(1);
        cmd.CommandText="SELECT COUNT(*) FROM items i JOIN favorites f ON f.id=i.id WHERE ($s='' OR i.source=$s)";
        return new LibraryStats(counts[0],counts[1],counts[2]+counts[3],Convert.ToInt32(cmd.ExecuteScalar()));
    });
    public Task<List<string>> CategoriesAsync(string? source, ContentKind? kind) => Task.Run(() =>
    {
        using var c=Open(); using var cmd=c.CreateCommand(); cmd.CommandText="SELECT DISTINCT category FROM items WHERE ($s='' OR source=$s) AND ($k=-1 OR kind=$k) ORDER BY category COLLATE NOCASE";
        cmd.Parameters.AddWithValue("$s",source??"");cmd.Parameters.AddWithValue("$k",kind is null?-1:(int)kind);
        using var r=cmd.ExecuteReader();var list=new List<string>{"Tüm kategoriler"};while(r.Read())list.Add(r.GetString(0));return list;
    });
    public async Task FavoriteAsync(string id,bool value) => await WriteAsync(c => { using var cmd=c.CreateCommand();cmd.CommandText=value?"INSERT OR IGNORE INTO favorites VALUES($id)":"DELETE FROM favorites WHERE id=$id";cmd.Parameters.AddWithValue("$id",id);cmd.ExecuteNonQuery(); });
    public async Task RememberAsync(string id) => await WriteAsync(c => { using var cmd=c.CreateCommand();cmd.CommandText="INSERT OR REPLACE INTO history VALUES($id,$t)";cmd.Parameters.AddWithValue("$id",id);cmd.Parameters.AddWithValue("$t",DateTimeOffset.Now.ToUnixTimeSeconds());cmd.ExecuteNonQuery(); });
    public async Task DeleteSourceAsync(string id) => await WriteAsync(c =>
    {
        using var tx=c.BeginTransaction(); using var cmd=c.CreateCommand(); cmd.Transaction=tx;cmd.Parameters.AddWithValue("$s",id);
        cmd.CommandText="DELETE FROM favorites WHERE id IN(SELECT id FROM items WHERE source=$s); DELETE FROM history WHERE id IN(SELECT id FROM items WHERE source=$s); DELETE FROM items WHERE source=$s; DELETE FROM epg WHERE source=$s; DELETE FROM sources WHERE id=$s;";cmd.ExecuteNonQuery();tx.Commit();
    });
    private async Task WriteAsync(Action<SqliteConnection> action)
    {
        await _writer.WaitAsync();try { await Task.Run(()=>{using var c=Open();action(c);}); } finally { _writer.Release(); }
    }
    public async Task<int> ImportEpgAsync(string source,IAsyncEnumerable<Programme> programmes,CancellationToken ct)
    {
        await _writer.WaitAsync(ct);try { return await Task.Run(async()=>
        {
            using var c=Open();using var tx=c.BeginTransaction();using var cmd=c.CreateCommand();cmd.Transaction=tx;
            cmd.CommandText="DELETE FROM epg WHERE source=$s";cmd.Parameters.AddWithValue("$s",source);cmd.ExecuteNonQuery();
            cmd.CommandText="INSERT INTO epg VALUES($s,$c,$t,$d,$start,$end)";
            foreach(var p in new[]{"$c","$t","$d","$start","$end"})cmd.Parameters.AddWithValue(p,"");cmd.Prepare();int count=0;
            await foreach(var p in programmes.WithCancellation(ct))
            {
                ct.ThrowIfCancellationRequested();cmd.Parameters["$c"].Value=p.ChannelId;cmd.Parameters["$t"].Value=p.Title;cmd.Parameters["$d"].Value=p.Description;cmd.Parameters["$start"].Value=p.Start.ToUnixTimeSeconds();cmd.Parameters["$end"].Value=p.End.ToUnixTimeSeconds();cmd.ExecuteNonQuery();count++;
            }
            if(count==0)throw new InvalidOperationException("XMLTV program verisi içermiyor; önceki rehber korundu.");
            ct.ThrowIfCancellationRequested();tx.Commit();return count;
        },ct); }finally{_writer.Release();}
    }
    public Task<List<Programme>> EpgAsync(ContentItem item,DateTimeOffset date) => Task.Run(()=>
    {
        using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT channel,title,description,start,end FROM epg WHERE source=$s AND (channel=$c OR channel=$n) AND end>$from AND start<$to ORDER BY start LIMIT 200";
        cmd.Parameters.AddWithValue("$s",item.SourceId);cmd.Parameters.AddWithValue("$c",string.IsNullOrEmpty(item.EpgId)?item.Name:item.EpgId);cmd.Parameters.AddWithValue("$n",item.Name);cmd.Parameters.AddWithValue("$from",date.ToUnixTimeSeconds());cmd.Parameters.AddWithValue("$to",date.AddDays(1).ToUnixTimeSeconds());
        using var r=cmd.ExecuteReader();var list=new List<Programme>();while(r.Read())list.Add(new(r.GetString(0),r.GetString(1),r.GetString(2),DateTimeOffset.FromUnixTimeSeconds(r.GetInt64(3)),DateTimeOffset.FromUnixTimeSeconds(r.GetInt64(4))));return list;
    });
}
