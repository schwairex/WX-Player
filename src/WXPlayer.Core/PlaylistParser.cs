using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace WXPlayer.Core;

public static partial class PlaylistParser
{
    [GeneratedRegex("([\\w-]+)\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)'|([^\\s,]+))", RegexOptions.CultureInvariant)]
    private static partial Regex Attributes();

    public static async IAsyncEnumerable<ContentItem> ParseAsync(TextReader reader, SourceConfig source, [EnumeratorCancellation] CancellationToken ct = default)
    {
        string name = "", group = "Genel", logo = "", epg = "", catchup = "", ua = "", refer = "";
        int days = 0, index = 0;
        bool manifest = false;
        while (await reader.ReadLineAsync(ct) is { } raw)
        {
            ct.ThrowIfCancellationRequested();
            var line = raw.Trim().TrimStart('\uFEFF');
            if (line.Length == 0) continue;
            if (line.StartsWith("#EXT-X-TARGETDURATION") || line.StartsWith("#EXT-X-STREAM-INF"))
            {
                manifest = true;
                yield return new ContentItem { Id = ContentItem.Key(source.Id, source.Address), SourceId = source.Id, Name = source.Name, Url = Resolve(source.Address, source.Address), Category = "Doğrudan yayın" };
                break;
            }
            if (line.StartsWith("#EXTM3U", StringComparison.OrdinalIgnoreCase))
            {
                foreach (Match m in Attributes().Matches(line))
                    if (m.Groups[1].Value is "url-tvg" or "x-tvg-url" && string.IsNullOrWhiteSpace(source.EpgUrl))
                        source.EpgUrl = Value(m).Split(',')[0];
            }
            else if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
            {
                bool quoted = false; char quote = '\0'; int comma = -1;
                for (int i = 8; i < line.Length; i++)
                {
                    if (line[i] is '\"' or '\'') { if (!quoted) { quoted = true; quote = line[i]; } else if (quote == line[i]) quoted = false; }
                    if (line[i] == ',' && !quoted) { comma = i; break; }
                }
                name = comma >= 0 ? line[(comma + 1)..].Trim() : "";
                var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (Match m in Attributes().Matches(comma >= 0 ? line[..comma] : line)) attrs[m.Groups[1].Value] = Value(m);
                if (name.Length == 0) name = attrs.GetValueOrDefault("tvg-name", "");
                group = attrs.GetValueOrDefault("group-title", "Genel");
                logo = attrs.GetValueOrDefault("tvg-logo", ""); epg = attrs.GetValueOrDefault("tvg-id", "");
                catchup = attrs.GetValueOrDefault("catchup-source", "");
                int.TryParse(attrs.GetValueOrDefault("catchup-days", "0"), out days);
            }
            else if (line.StartsWith("#EXTGRP:")) group = line[8..].Trim();
            else if (line.StartsWith("#EXTVLCOPT:http-user-agent=")) ua = AddressPolicy.Header(line[27..]);
            else if (line.StartsWith("#EXTVLCOPT:http-referrer=")) refer = AddressPolicy.Header(line[25..]);
            else if (!line.StartsWith('#'))
            {
                var url = Resolve(line, source.Address);
                if (!AddressPolicy.IsPlayable(url)) continue;
                index++;
                var uri = new Uri(url);
                string label = name.Length > 0 ? name : Path.GetFileNameWithoutExtension(uri.LocalPath);
                if (label.Length == 0) label = $"Kanal {index}";
                string lower = (group + " " + label).ToLowerInvariant();
                var kind = lower.Contains("series") || lower.Contains("dizi") ? ContentKind.Series : lower.Contains("film") || lower.Contains("movie") || lower.Contains("vod") || new[] { ".mp4", ".mkv", ".avi" }.Contains(Path.GetExtension(uri.AbsolutePath).ToLowerInvariant()) ? ContentKind.Movie : ContentKind.Live;
                yield return new ContentItem { Id = ContentItem.Key(source.Id, url), SourceId = source.Id, ProviderId = index.ToString(), Name = label, Category = string.IsNullOrWhiteSpace(group) ? "Genel" : group, Kind = kind, Url = url, Logo = logo, EpgId = epg, Catchup = catchup, CatchupDays = days, UserAgent = ua, Referrer = refer };
                name = ""; group = "Genel"; logo = ""; epg = ""; catchup = ""; ua = ""; refer = ""; days = 0;
            }
        }
        _ = manifest;
    }
    private static string Value(Match m) => m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Success ? m.Groups[3].Value : m.Groups[4].Value;
    private static string Resolve(string address, string origin)
    {
        if (Uri.TryCreate(address, UriKind.Absolute, out var absolute)) return absolute.AbsoluteUri;
        if (Uri.TryCreate(origin, UriKind.Absolute, out var baseUri)) return new Uri(baseUri, address).AbsoluteUri;
        return new Uri(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(origin) ?? ".", address))).AbsoluteUri;
    }
}
