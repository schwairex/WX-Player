using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed partial class HomeView
{
    private CancellationTokenSource? _artworkLoad;
    private int _renderVersion;
    internal bool IsLoadingArtwork { get; private set; }
    private void CancelArtwork() { _artworkLoad?.Cancel(); _artworkLoad?.Dispose(); _artworkLoad = null; _renderVersion++; IsLoadingArtwork = false; }
    internal void Render(string? source, IReadOnlyList<HomeShelf> shelves, string search, ContentItem? recommendation = null)
        => _ = RenderAsync(source, shelves, search, recommendation);
    internal async Task RenderAsync(string? source, IReadOnlyList<HomeShelf> shelves, string search, ContentItem? recommendation = null)
    {
        CancelArtwork(); int version = _renderVersion;
        var cancellation = _artworkLoad = new CancellationTokenSource(); var ct = cancellation.Token;
        var candidates = shelves.ToArray();
        var items = candidates.SelectMany(s => s.Page.Items).Concat(recommendation is null ? [] : new[] { recommendation }).DistinctBy(i=>i.Id).ToArray();
        var ready = new HashSet<string>(items.Where(i => ArtworkCache.TryGet(i.Logo, 480, out _)).Select(i=>i.Id));
        IsLoadingArtwork = true;
        try
        {
            if (ready.Count > 0) DisplayReady();
            else if (Items.Count == 0) { Reset(); Empty = false; ShowState("İzleyecek yeni bir şey keşfet", "Afişler hazırlanıyor…", null, null); }
            async Task<(string Id, bool Ready)> Load(ContentItem item)
            {await ArtworkService.PopulateAsync(item,ct);ct.ThrowIfCancellationRequested();return (item.Id, await ArtworkCache.GetAsync(item.Logo, 480).WaitAsync(ct) is not null);}
            var pending = items.Where(item => !ready.Contains(item.Id)).Select(Load).ToList();
            while (pending.Count > 0)
            {
                await Task.WhenAny(pending).WaitAsync(ct);
                if (!pending.All(t => t.IsCompleted)) await Task.Delay(60, ct);
                bool changed = false;
                foreach (var task in pending.Where(t => t.IsCompleted).ToArray())
                {
                    var result = await task; pending.Remove(task); if (result.Ready) changed |= ready.Add(result.Id);
                }
                ct.ThrowIfCancellationRequested(); if (version != _renderVersion) return;
                if (changed) DisplayReady();
            }
            if (version != _renderVersion) return;
            IsLoadingArtwork = false;
            if (ready.Count == 0) DisplayReady();
        }
        catch (OperationCanceledException) { }
        catch { if (version == _renderVersion) Error(() => Render(source, shelves, search, recommendation)); }

        void DisplayReady()
        {
            if (version != _renderVersion) return;
            double vertical = VerticalOffset;
            string? focusedId = Keyboard.FocusedElement is Button { Tag: ContentItem focusItem } focusButton && IsAncestorOf(focusButton) ? focusItem.Id : null;
            var offsets = _shelves.Children.OfType<StackPanel>().Where(p => p.Tag is string).ToDictionary(p => (string)p.Tag, p => p.Children.OfType<ScrollViewer>().FirstOrDefault()?.HorizontalOffset ?? 0);
            Reset();
            var visible = candidates.Select(s => s with { Page = new WXPlayer.Core.Page(s.Page.Items.Where(i => ready.Contains(i.Id)).Take(12).ToArray(), s.Page.Total) }).ToArray();
            Items = visible.SelectMany(s => s.Page.Items).DistinctBy(i => i.Id).ToArray(); Empty = Items.Count == 0;
            Featured = recommendation is not null && ready.Contains(recommendation.Id) ? recommendation : Items.FirstOrDefault(i => i.Kind is ContentKind.Movie or ContentKind.Series) ?? Items.FirstOrDefault();
            if (Featured is null)
            {
                bool searching = search.Length > 0;
                ShowState(searching ? "Aradığın içerik bulunamadı" : source is null ? "Kütüphanen seni bekliyor" : "Afişli içerik bulunamadı",
                    searching ? "Afişli içerikler arasında sonuç yok. Aramayı temizleyebilir veya tüm kütüphaneye göz atabilirsiniz." : source is null ? "M3U listenizi veya Xtream hesabınızı bağlayın." : "Bu kaynakta şu anda görüntülenebilen afiş yok. İçeriklerin tamamına kütüphaneden ulaşabilirsiniz.",
                    searching ? "Aramayı temizle" : source is null ? "Kaynak ekle" : "Tüm kütüphaneyi aç", () => { if (searching) Search.Clear(); else if (source is null) _add(); else _browse("all"); });
                return;
            }
            BuildHero(Featured, source ?? "Kütüphaneniz");
            foreach (var shelf in visible.Where(s => s.Page.Items.Count > 0)) BuildShelf(shelf);
            foreach (var panel in _shelves.Children.OfType<StackPanel>())
                if (panel.Tag is string key && offsets.TryGetValue(key, out double offset)) panel.Children.OfType<ScrollViewer>().First().ScrollToHorizontalOffset(offset);
            if (focusedId is not null)
                _shelves.Children.OfType<StackPanel>().SelectMany(p => p.Children.OfType<ScrollViewer>()).SelectMany(s => ((StackPanel)s.Content).Children.OfType<Button>()).FirstOrDefault(b => b.Tag is ContentItem i && i.Id == focusedId)?.Focus();
            ScrollToVerticalOffset(vertical);
        }
    }
}

