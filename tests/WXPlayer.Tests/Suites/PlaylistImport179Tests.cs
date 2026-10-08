using System.Text;
using WXPlayer.Core;

internal static class PlaylistImport179Tests
{
    internal static async Task RunAsync(Func<string, Func<Task>, Task> test, Action<bool, string> assert, string folder)
    {
        await test("1.7.9 local M3U import tolerates malformed optional logos", async () =>
        {
            string path = Path.Combine(folder, "mixed-line-endings.m3u");
            // A malformed drive-relative logo reproduces the provider's UriFormatException.
            // All addresses are synthetic: no user playlist or credentials enter the tests.
            await File.WriteAllTextAsync(path,
                "#EXTM3U\n" +
                "#EXTINF:-1 tvg-id=\"\" tvg-name=\"Türkçe Kanal\" tvg-logo=\"\" group-title=\"TR ✨ Spor\",Türkçe Kanal\r\nhttps://example.test/live/1.ts\r\n" +
                "#EXTINF:-1 tvg-logo=\"s:broken-poster.jpg\" group-title=\"Filmler\",Film\r\nhttps://example.test/movie/2.mp4\r\n" +
                "#EXTINF:-1 tvg-logo=\"logos/series.png\" group-title=\"Diziler\",Örnek S01E01\r\nhttps://example.test/series/3.mp4\r\n" +
                "#EXTINF:-1 tvg-logo=\"https://example.test/logo.png\",Son Kanal\r\nhttps://example.test/live/4.ts\r\n", new UTF8Encoding(false));
            var source = new SourceConfig { Id = "local-179", Name = "Local fixture", Address = path };
            var store = new LibraryStore(Path.Combine(folder, "local-179.db"));
            await store.InitializeAsync();
            using var providers = new ProviderClient();
            int count = await store.ImportAsync(source, providers.LoadAsync(source, default), null, default);
            assert(count == 4, "every playable entry imports despite missing or malformed artwork");
            var movies = await store.QueryAsync(source.Id, ContentKind.Movie, null, "", false, false, 0);
            assert(movies.Total == 1 && movies.Items.Single().Logo == "", "invalid optional logo becomes empty");
            var live = await store.QueryAsync(source.Id, ContentKind.Live, null, "", false, false, 0);
            assert(live.Total == 2 && live.Items.Any(i => i.Name == "Türkçe Kanal" && i.Logo == "" && i.Category == "TR ✨ Spor"), "Unicode and empty logos survive storage");
            assert(live.Items.Any(i => i.Logo == "https://example.test/logo.png"), "valid logo is preserved");
            var series = (await store.QueryAsync(source.Id, ContentKind.Series, null, "", false, false, 0)).Items.Single();
            var episodes = await store.PlaylistEpisodesAsync(series);
            assert(episodes.Count == 1 && episodes[0].Season == 1 && episodes[0].Episode == 1, "series grouping is preserved");
            assert(episodes[0].Logo == new Uri(new Uri(path), "logos/series.png").AbsoluteUri, "relative artwork still resolves against the local file");
            var reopened = new LibraryStore(Path.Combine(folder, "local-179.db"));
            assert((await reopened.SourcesAsync()).Single().Address == path, "source reload works after import");
        });
        await test("1.7.9 remote M3U malformed artwork does not discard following items", async () =>
        {
            var source = new SourceConfig { Id = "remote-179", Address = "https://example.test/list.m3u" };
            const string text = "#EXTM3U\n#EXTINF:-1 tvg-logo=\"http://[broken\",One\nfirst.ts\n#EXTINF:-1 tvg-logo=\"/logo.png\",Two\nsecond.ts\n";
            var items = new List<ContentItem>();
            await foreach (var item in PlaylistParser.ParseAsync(new StringReader(text), source)) items.Add(item);
            assert(items.Count == 2 && items[0].Logo == "" && items[1].Logo == "https://example.test/logo.png", "bad artwork is isolated and valid relative artwork remains intact");
            assert(items[0].Url == "https://example.test/first.ts" && items[1].Url == "https://example.test/second.ts", "stream addresses remain unchanged");
        });
        await test("1.7.9 malformed stream URI is skipped without aborting the next entry", async () =>
        {
            var source = new SourceConfig { Id = "stream-179", Address = "https://example.test/list.m3u" };
            const string text = "#EXTM3U\n#EXTINF:-1,Broken\nhttp://[broken\n#EXTINF:-1 tvg-id=\"news\",Valid\nvalid.ts\n";
            var items = new List<ContentItem>();
            await foreach (var item in PlaylistParser.ParseAsync(new StringReader(text), source)) items.Add(item);
            assert(items.Count == 1 && items[0].Name == "Valid" && items[0].EpgId == "news" && items[0].Url == "https://example.test/valid.ts", "invalid URI cannot roll back a playable subsequent entry");
        });
    }
}
