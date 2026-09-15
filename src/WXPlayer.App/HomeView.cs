using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed record HomeShelf(string Title, string Section, WXPlayer.Core.Page Page);

internal sealed partial class HomeView : ScrollViewer
{
    private const double FeatureHeight = 264;
    private readonly StackPanel _body = new(), _shelves = new();
    private readonly Grid _feature = new() { Height = FeatureHeight, ClipToBounds = true };
    private readonly Grid _toolbar = new(), _searchBox = new();
    private readonly Button _return;
    private readonly Action<ContentItem> _play, _favorite;
    private readonly Action<string> _browse;
    private readonly Action _add;
    internal readonly TextBox Search = new() { MinHeight = 40, Padding = new Thickness(12, 9, 12, 9) };
    internal IReadOnlyList<ContentItem> Items { get; private set; } = [];
    internal ContentItem? Featured { get; private set; }
    internal bool Empty { get; private set; }
    private static Brush Color(string value) => PremiumWindow.Brush(value);
    private static TextBlock Text(string value, int size = 13, string color = "#E9EEF5") => PremiumWindow.Text(value, size, color);

    internal HomeView(Action<ContentItem> play, Action<ContentItem> favorite, Action<string> browse, Action add, Action resume)
    {
        _play = play; _favorite = favorite; _browse = browse; _add = add;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        Content = _body; _body.Margin = new Thickness(0, 0, 12, 0);
        _toolbar.Margin = new Thickness(0, 0, 0, 20);
        _toolbar.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        _toolbar.ColumnDefinitions.Add(new() { Width = new GridLength(270) });
        _toolbar.RowDefinitions.Add(new() { Height = GridLength.Auto });
        _toolbar.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var shortcuts = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        foreach (var (label, section) in new[] { ("Canlı TV", "live"), ("Filmler", "movie"), ("Diziler", "series"), ("Favoriler", "favorites") })
        {
            var button = Action(label, () => browse(section));
            button.Margin = new Thickness(0, 0, 8, 0); button.Padding = new Thickness(12, 8, 12, 8);
            shortcuts.Children.Add(button);
        }
        _toolbar.Children.Add(shortcuts); _searchBox.Children.Add(Search);
        var hint = Text("Kütüphanende ara…", 12, "#98A3B6");
        hint.Margin = new Thickness(14, 0, 0, 0); hint.VerticalAlignment = VerticalAlignment.Center; hint.IsHitTestVisible = false;
        _searchBox.Children.Add(hint);
        Search.TextChanged += (_, _) => hint.Visibility = Search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        Search.GotKeyboardFocus += (_, _) => Search.BorderBrush = Color("#C1EC8B");
        Search.LostKeyboardFocus += (_, _) => Search.BorderBrush = Color("#303A4B");
        System.Windows.Automation.AutomationProperties.SetName(Search, "Ana sayfada içerik ara");
        Grid.SetColumn(_searchBox, 1); _toolbar.Children.Add(_searchBox); _body.Children.Add(_toolbar);
        SizeChanged += (_, _) => LayoutToolbar();
        _return = Action("İzlemeye dön", resume);
        _return.HorizontalAlignment = HorizontalAlignment.Left; _return.Margin = new Thickness(0, 0, 0, 16);
        _return.MaxWidth = 600; _return.Visibility = Visibility.Collapsed; _body.Children.Add(_return);
        _feature.Margin = new Thickness(0, 0, 0, 24); _body.Children.Add(_feature);
        _shelves.Margin = new Thickness(0, 0, 0, 24); _body.Children.Add(_shelves);
    }

    private void LayoutToolbar()
    {
        bool compact = ActualWidth < 780;
        _toolbar.ColumnDefinitions[1].Width = compact ? new GridLength(0) : new GridLength(270);
        Grid.SetColumn(_searchBox, compact ? 0 : 1); Grid.SetRow(_searchBox, compact ? 1 : 0);
        _searchBox.Margin = compact ? new Thickness(0, 12, 0, 0) : new Thickness(12, 0, 0, 0);
        _return.MaxWidth = Math.Max(100, ActualWidth - 30);
    }

    private static Button Action(string label, Action action, bool primary = false)
    {
        var button = PremiumWindow.Action(label, action, primary); button.Margin = new Thickness(0); return button;
    }
    internal void NowPlaying(string? title)
    {
        _return.Content = new TextBlock { Text = "İzlemeye dön · " + title, TextTrimming = TextTrimming.CharacterEllipsis };
        _return.ToolTip = title; _return.Visibility = string.IsNullOrEmpty(title) ? Visibility.Collapsed : Visibility.Visible;
    }
    internal void Loading()
    {
        Reset(); Empty = false;
        ShowState("Kütüphanen hazırlanıyor", "İçerikleriniz listeleniyor…", null, null);
        var skeletons = new StackPanel { Orientation = Orientation.Horizontal, ClipToBounds = true, Height = 200 };
        for (int i = 0; i < 8; i++) skeletons.Children.Add(new Border { Width = 160, Height = 200, Background = Color("#141922"), CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 16, 0) });
        _shelves.Children.Add(skeletons);
    }
    internal void Error(Action retry)
    {
        Reset(); Empty = false;
        ShowState("Kütüphane görüntülenemedi", "İçerikler şu anda yüklenemiyor. Yeniden deneyebilirsiniz.", "Yeniden dene", retry);
    }
    private void Reset() { _feature.Children.Clear(); _shelves.Children.Clear(); Items = []; Featured = null; }
    private void ShowState(string title, string description, string? action, Action? callback)
    {
        var copy = new StackPanel { Margin = new Thickness(28), VerticalAlignment = VerticalAlignment.Center };
        var heading = Text(title, 24); heading.FontWeight = FontWeights.SemiBold; copy.Children.Add(heading);
        var detail = Text(description, 13, "#98A3B6"); detail.Margin = new Thickness(0, 12, 0, 20); copy.Children.Add(detail);
        if (action is not null && callback is not null)
        {
            var button = Action(action, callback, true); button.HorizontalAlignment = HorizontalAlignment.Left; copy.Children.Add(button);
        }
        _feature.Children.Add(new Border { Background = Color("#141922"), BorderBrush = Color("#252C38"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Child = copy });
    }
    internal void Render(string? source, IReadOnlyList<HomeShelf> shelves, string search, ContentItem? recommendation = null)
    {
        Reset(); Items = shelves.SelectMany(s => s.Page.Items).DistinctBy(i => i.Id).ToArray(); Empty = Items.Count == 0;
        Featured = recommendation ?? Items.FirstOrDefault(i => i.Kind == ContentKind.Movie && !string.IsNullOrWhiteSpace(i.Logo)) ?? Items.FirstOrDefault(i => i.Kind == ContentKind.Movie) ?? Items.FirstOrDefault();
        if (Featured is null)
        {
            bool searching = search.Length > 0;
            ShowState(searching ? "Aradığın içerik bulunamadı" : source is null ? "Kütüphanen seni bekliyor" : "Bu kaynakta henüz içerik yok",
                searching ? "Farklı bir ad deneyin veya aramanızı temizleyin." : "M3U listenizi veya Xtream hesabınızı bağlayın. Filmleriniz, dizileriniz ve canlı kanallarınız burada görünsün.",
                searching ? "Aramayı temizle" : "Kaynak ekle", () => { if (searching) Search.Clear(); else _add(); });
            return;
        }
        BuildHero(Featured, source ?? "Kütüphaneniz");
        foreach (var shelf in shelves.Where(s => s.Page.Items.Count > 0)) BuildShelf(shelf);
    }
    private void BuildHero(ContentItem item, string source)
    {
        // Explicit bounds prevent asynchronously loaded, tall artwork from expanding a ScrollViewer's content.
        var hero = new Grid { Height = FeatureHeight - 2, ClipToBounds = true };
        hero.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        hero.ColumnDefinitions.Add(new() { Width = new GridLength(192) });
        var copy = new StackPanel { Margin = new Thickness(28, 20, 20, 20), VerticalAlignment = VerticalAlignment.Center, MaxWidth = 660, HorizontalAlignment = HorizontalAlignment.Left };
        hero.Children.Add(copy);
        var eyebrow = Text("SENİN İÇİN SEÇTİK  ·  " + item.KindLabel, 10, "#C1EC8B"); eyebrow.FontWeight = FontWeights.SemiBold; copy.Children.Add(eyebrow);
        var title = Text(item.Name, 26); title.FontWeight = FontWeights.SemiBold; title.LineHeight = 32;
        title.MaxHeight = 64; title.TextTrimming = TextTrimming.CharacterEllipsis; title.ToolTip = item.Name; title.Margin = new Thickness(0, 12, 0, 10); copy.Children.Add(title);
        var metadata = Text(item.Category + "  ·  " + source, 12, "#98A3B6"); metadata.TextWrapping = TextWrapping.NoWrap; metadata.TextTrimming = TextTrimming.CharacterEllipsis; metadata.Margin = new Thickness(0, 0, 0, 18); copy.Children.Add(metadata);
        var actions = new WrapPanel();
        string playLabel = item.Kind == ContentKind.Series ? "Bölümleri keşfet" : item.Kind == ContentKind.Live ? "Canlı izle" : "Filmi izle";
        var play = Action(playLabel, () => _play(item), true); play.Margin = new Thickness(0, 0, 8, 6); actions.Children.Add(play);
        var favorite = Action(item.IsFavorite ? "Favorilerimde" : "Favorilere ekle", () => _favorite(item)); favorite.Margin = new Thickness(0, 0, 0, 6); actions.Children.Add(favorite); copy.Children.Add(actions);
        var poster = Artwork(item, 144, 216); poster.Margin = new Thickness(12, 22, 28, 22); Grid.SetColumn(poster, 1); hero.Children.Add(poster);
        hero.SizeChanged += (_, _) =>
        {
            bool narrow = hero.ActualWidth < 620;
            hero.ColumnDefinitions[1].Width = new GridLength(narrow ? 144 : 192);
            poster.Width = narrow ? 108 : 144; poster.Height = narrow ? 162 : 216;
            poster.Margin = new Thickness(8, 20, narrow ? 20 : 28, 20);
            title.FontSize = narrow ? 22 : 26; title.LineHeight = narrow ? 28 : 32; title.MaxHeight = narrow ? 84 : 64;
            copy.Margin = new Thickness(narrow ? 20 : 28, 16, 16, 16);
        };
        _feature.Children.Add(new Border { Background = Color("#141B24"), BorderBrush = Color("#293440"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Child = hero, ClipToBounds = true });
    }
    private static void Round(FrameworkElement element, double radius)
    {
        element.SizeChanged += (_, _) => element.Clip = new RectangleGeometry(new Rect(0, 0, element.ActualWidth, element.ActualHeight), radius, radius);
    }
}
