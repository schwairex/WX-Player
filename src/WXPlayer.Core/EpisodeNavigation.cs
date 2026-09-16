namespace WXPlayer.Core;

public static class EpisodeNavigation
{
    public static ContentItem[] Ordered(ContentItem current, IEnumerable<ContentItem> episodes) => episodes
        .Where(e => e.Kind == ContentKind.Episode && e.SourceId == current.SourceId && e.SeriesId == current.SeriesId)
        .DistinctBy(e => e.Id).OrderBy(e => e.Season).ThenBy(e => e.Episode > 0 ? e.Episode : int.MaxValue)
        .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToArray();

    public static ContentItem? Adjacent(ContentItem? current, IEnumerable<ContentItem> episodes, int direction)
    {
        if (current is not { Kind: ContentKind.Episode } || current.SeriesId.Length == 0) return null;
        var ordered = Ordered(current, episodes);
        int index = Array.FindIndex(ordered, e => e.Id == current.Id);
        int next = index + Math.Sign(direction);
        return index >= 0 && direction != 0 && next >= 0 && next < ordered.Length ? ordered[next] : null;
    }

    public static bool OfferNext(ContentItem? current, ContentItem? next, bool fullscreen, long position, long duration) =>
        fullscreen && current?.Kind == ContentKind.Episode && next is not null && duration > 0 && position >= 0 &&
        position <= duration && duration - position <= 30_000;
}
