using System.Text.RegularExpressions;
using WXPlayer.Core;

namespace WXPlayer.App;

// Display only. Never writes the ContentItem or its persisted metadata.
internal static class HomeDisplayFormatter
{
    internal static string Clean(string? value) => Regex.Replace(Regex.Replace(value ?? "", @"\[[^\]]*\]", ""), @"\s{2,}", " ").Trim();
    internal static string Metadata(ContentItem item)
    {
        string category = Clean(item.Category);
        string progress = Clean(item.ProgressLabel);
        if (progress.Contains("Süre bilinmiyor", StringComparison.OrdinalIgnoreCase)) progress = "";
        string kind = item.Kind is ContentKind.Series or ContentKind.Episode ? "Dizi" : "Film";
        return string.Join(" · ", new[] { kind, progress.Length > 0 ? progress : category }.Where(s => s.Length > 0));
    }
    internal static string Quality(string name) => Regex.Match(name, @"(?i)(?<!\w)(4K|UHD|FHD)(?!\w)").Value.ToUpperInvariant();
}

internal sealed record HomeFavoriteAction(ContentItem Item);
