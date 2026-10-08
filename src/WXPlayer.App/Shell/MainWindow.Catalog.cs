using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WXPlayer.Core;

namespace WXPlayer.App;

public partial class MainWindow
{
    private CatalogView _catalog = null!;
    private CancellationTokenSource? _catalogLoad;
    private int _catalogVersion;
    private bool _catalogPlaying;
    private (string? Source, ContentKind Kind)? _catalogHeroContext;
    private IReadOnlyList<ContentItem> _catalogHeroItems = [];

    private bool CatalogSection => _section is "movie" or "series";
    private bool CatalogVisible => CatalogSection && !_catalogPlaying && !_fullscreen;

    private void InitializeCatalog()
    {
        _catalog = new CatalogView(
            item => _ = SafeAsync(() => OpenCatalogItemAsync(item)),
            item => _ = SafeAsync(() => ToggleFavoriteAsync(item)),
            () => { _catalogPlaying = true; ApplyPageLayout(); });
        _catalog.Search.SetBinding(TextBox.TextProperty,
            new Binding("Text") { Source = SearchBox, Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        CatalogHost.Content = _catalog;
    }

    private Task RefreshCatalogAsync() => RefreshCatalogAsync(false);
    private async Task RefreshCatalogAsync(bool searchOnly)
    {
        if (!_ready || !CatalogVisible) return;
        // A data/navigation refresh invalidates the snapshot immediately. A search
        // arriving during its awaits must not resurrect the pre-refresh hero.
        if (!searchOnly) { _catalogHeroContext = null; _catalogHeroItems = []; }
        _catalogLoad?.Cancel(); _catalogLoad?.Dispose();
        var cts = _catalogLoad = CancellationTokenSource.CreateLinkedTokenSource(_life.Token);
        int version = ++_catalogVersion;
        bool series = _section == "series";
        var kind = series ? ContentKind.Series : ContentKind.Movie;
        string? source = SelectedSource?.Id;
        string sourceName = SelectedSource?.Name ?? "Tüm kaynaklar";
        string search = SearchBox.Text.Trim();
        var heroContext = (source, kind);
        bool preserveHero = searchOnly && _catalogHeroContext == heroContext && _catalog.Hero.Children.Count > 0;
        _catalog.Loading(series, preserveHero);
        try
        {
            var categories = await _store.CategoriesAsync(source, kind);
            cts.Token.ThrowIfCancellationRequested();
            var names = categories.Skip(1).Where(c => !string.IsNullOrWhiteSpace(c)).Take(5).ToArray();
            var requests = new List<(string Title, string? Category)>
            {
                (series ? "Tüm diziler" : "Tüm filmler", null)
            };
            requests.AddRange(names.Select(name => (name, (string?)name)));
            var pages = await Task.WhenAll(requests.Select((request, index) =>
                _store.QueryAsync(source, kind, request.Category, search, false, false, 0,
                    index == 0 ? 40 : 24, cts.Token)));
            var featured = preserveHero ? _catalogHeroItems : search.Length == 0 ? pages[0].Items :
                (await _store.QueryAsync(source, kind, null, "", false, false, 0, 40, cts.Token)).Items;
            if (cts.IsCancellationRequested || version != _catalogVersion || !CatalogVisible ||
                source != SelectedSource?.Id || (series ? "series" : "movie") != _section) return;
            var shelves = requests.Select((request, index) =>
                new CatalogShelf(request.Title, request.Category, pages[index])).ToArray();
            _catalogHeroContext = heroContext; _catalogHeroItems = featured;
            _catalog.Render(series, sourceName, search, shelves,
                (category, offset, limit, token) => _store.QueryAsync(source, kind, category, search,
                    false, false, offset, limit, token), cts.Token,
                categories.Skip(1).Where(c => !string.IsNullOrWhiteSpace(c)).ToArray(), featured, preserveHero);
            _catalog.NowPlaying(_current?.Name);
        }
        catch (OperationCanceledException) { }
        catch
        {
            if (version == _catalogVersion && !cts.IsCancellationRequested && CatalogVisible &&
                source == SelectedSource?.Id && (series ? "series" : "movie") == _section)
            {
                _catalog.Render(series, sourceName, search, [], (_, _, _, _) =>
                    Task.FromResult(new WXPlayer.Core.Page([], 0)), cts.Token, featured: preserveHero ? _catalogHeroItems : [], preserveHero: preserveHero);
                if (!preserveHero) { _catalogHeroContext = null; _catalogHeroItems = []; }
            }
        }
    }

    private async Task OpenCatalogItemAsync(ContentItem item)
    {
        var previous = _current;
        _catalogPlaying = true;
        ApplyPageLayout();
        await PlayItemAsync(item);
        if (ReferenceEquals(previous, _current) && item.Kind == ContentKind.Series)
        {
            // Closing the episode picker returns to the catalogue.
            _catalogPlaying = false;
            ApplyPageLayout();
        }
    }

    private void ReturnCatalog_Click(object sender, RoutedEventArgs e)
    {
        if (!CatalogSection) return;
        _catalogPlaying = false;
        ApplyPageLayout();
        _ = SafeAsync(RefreshCatalogAsync);
    }

    internal CatalogView SmokeCatalog => _catalog;
}
