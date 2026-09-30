using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed record HomeShelf(string Title, string Section, WXPlayer.Core.Page Page);

internal sealed partial class HomeView : ScrollViewer
{
    private const double FeatureHeight = 352;
    private readonly StackPanel _body = new(), _shelves = new();
    private readonly Grid _feature = new() { Height = FeatureHeight, ClipToBounds = true };
    private readonly Grid _toolbar = new(), _searchBox = new();
    private readonly Button _return;
    private readonly Action<ContentItem> _play, _favorite;
    private readonly Action<string> _browse;
    private readonly Action _add;
    private readonly Func<ContentItem,Task>? _removeRecent;
    private readonly DispatcherTimer _heroTimer = new() { Interval = TimeSpan.FromSeconds(9) };
    private IReadOnlyList<ContentItem> _heroItems = [];
    private string _heroSource = "";
    private int _heroIndex;
    internal readonly TextBox Search = new() { MinHeight = 40, Padding = new Thickness(12, 9, 12, 9) };
    internal IReadOnlyList<ContentItem> Items { get; private set; } = [];
    internal ContentItem? Featured { get; private set; }
    internal bool Empty { get; private set; }
    private static Brush Color(string value) => PremiumWindow.Brush(value);
    private static TextBlock Text(string value, int size = 13, string color = "TextPrimaryBrush") => PremiumWindow.Text(value, size, color);

    internal HomeView(Action<ContentItem> play, Action<ContentItem> favorite, Action<string> browse, Action add, Action resume, Func<ContentItem,Task>? removeRecent = null)
    {
        _play = play; _favorite = favorite; _browse = browse; _add = add;
        _removeRecent = removeRecent;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        Content = _body; _body.Margin = new Thickness(0, 0, 12, 0);
        _toolbar.Margin = new Thickness(0, 0, 0, 24);
        _toolbar.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        _toolbar.ColumnDefinitions.Add(new() { Width = new GridLength(270) });
        _toolbar.RowDefinitions.Add(new() { Height = GridLength.Auto });
        _toolbar.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var shortcuts = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        foreach (var (label, section) in new[] { ("Canlı TV", "live"), ("Filmler", "movie"), ("Diziler", "series"), ("Favoriler", "favorites") })
        {
            var button = Action(label, () => browse(section));
            button.Margin = new Thickness(0, 0, 8, 0); button.Padding = new Thickness(14, 8, 14, 8);
            shortcuts.Children.Add(button);
        }
        _toolbar.Children.Add(shortcuts); _searchBox.Children.Add(Search);
        var hint = Text("Kütüphanende ara…", 12, "TextMutedBrush");
        hint.Margin = new Thickness(14, 0, 0, 0); hint.VerticalAlignment = VerticalAlignment.Center; hint.IsHitTestVisible = false;
        _searchBox.Children.Add(hint);
        Search.TextChanged += (_, _) => hint.Visibility = Search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        Search.GotKeyboardFocus += (_, _) => Search.BorderBrush = Color("AccentPrimaryBrush");
        Search.LostKeyboardFocus += (_, _) => Search.BorderBrush = Color("BorderSubtleBrush");
        System.Windows.Automation.AutomationProperties.SetName(Search, "Ana sayfada içerik ara");
        Grid.SetColumn(_searchBox, 1); _toolbar.Children.Add(_searchBox); _body.Children.Add(_toolbar);
        SizeChanged += (_, _) => LayoutToolbar();
        PreviewMouseWheel += HomeWheel;
        _return = Action("İzlemeye dön", resume);
        _return.HorizontalAlignment = HorizontalAlignment.Left; _return.Margin = new Thickness(0, 0, 0, 16);
        _return.MaxWidth = 600; _return.Visibility = Visibility.Collapsed; _body.Children.Add(_return);
        _feature.Margin = new Thickness(0, 0, 0, 30); _body.Children.Add(_feature);
        _shelves.Margin = new Thickness(0, 0, 0, 30); _body.Children.Add(_shelves);
        _heroTimer.Tick += (_, _) => { if (IsVisible && !_feature.IsMouseOver && _heroItems.Count > 1 && Search.Text.Length == 0) MoveHero(1); };
        Loaded += (_, _) => _heroTimer.Start();
        Unloaded += (_, _) => _heroTimer.Stop();
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
    internal void Loading(bool clear = true)
    {
        CancelArtwork(); if (!clear && Items.Count > 0) return; Reset(); ScrollToTop(); Empty = false;
        ShowState("Kütüphanen hazırlanıyor", "İçerikleriniz listeleniyor…", null, null);
        var skeletons = new StackPanel { Orientation = Orientation.Horizontal, ClipToBounds = true, Height = 200 };
        for (int i = 0; i < 8; i++) skeletons.Children.Add(new Border { Width = 160, Height = 200, Background = Color("SurfaceBrush"), CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 16, 0) });
        _shelves.Children.Add(skeletons);
    }
    internal void Error(Action retry)
    {
        CancelArtwork(); Reset(); Empty = false;
        ShowState("Kütüphane görüntülenemedi", "İçerikler şu anda yüklenemiyor. Yeniden deneyebilirsiniz.", "Yeniden dene", retry);
    }
    private void Reset() { _feature.Children.Clear(); _shelves.Children.Clear(); Items = []; Featured = null; _heroItems = []; }
    private void ShowState(string title, string description, string? action, Action? callback)
    {
        var copy = new StackPanel { Margin = new Thickness(28), VerticalAlignment = VerticalAlignment.Center };
        var heading = Text(title, 24); heading.FontWeight = FontWeights.SemiBold; copy.Children.Add(heading);
        var detail = Text(description, 13, "TextMutedBrush"); detail.Margin = new Thickness(0, 12, 0, 20); copy.Children.Add(detail);
        if (action is not null && callback is not null)
        {
            var button = Action(action, callback, true); button.HorizontalAlignment = HorizontalAlignment.Left; copy.Children.Add(button);
        }
        _feature.Children.Add(new Border { Background = Color("SurfaceBrush"), CornerRadius = new CornerRadius(12), Child = copy });
    }
    private void BuildHero(ContentItem item, string source)
    {
        _heroSource = source;
        _heroItems = Items.Where(i => i.Kind is ContentKind.Movie or ContentKind.Series).Take(7).ToArray();
        if (item.Kind is ContentKind.Movie or ContentKind.Series && _heroItems.All(i => i.Id != item.Id))
            _heroItems = new[] { item }.Concat(_heroItems).Take(7).ToArray();
        if (_heroItems.Count == 0) _heroItems = [item];
        int previous = _heroItems.ToList().FindIndex(i => i.Id == Featured?.Id);
        _heroIndex = previous >= 0 ? previous : 0;
        RenderHero();
    }
    private void MoveHero(int direction)
    {
        if (_heroItems.Count < 2) return;
        _heroIndex = (_heroIndex + direction + _heroItems.Count) % _heroItems.Count;
        RenderHero();
    }
    private void RenderHero()
    {
        if (_heroItems.Count == 0) return;
        var item = Featured = _heroItems[_heroIndex];
        _feature.Children.Clear();
        var hero = new Grid { Height = FeatureHeight - 2, ClipToBounds = true, Background = Color("SurfaceBrush") };
        Round(hero, 12);
        _feature.Children.Add(hero);
        var backdrop = new ChannelLogo { Url = item.Logo, Initials = "", DecodeWidth = 720, ImageStretch = Stretch.UniformToFill, ImagePadding = new Thickness(0), Opacity = .45, HorizontalAlignment = HorizontalAlignment.Right, Width = 710, Height = FeatureHeight + 30, Effect = new BlurEffect { Radius = 25 } };
        hero.Children.Add(backdrop);
        var baseColor = ((SolidColorBrush)Color("BackgroundBaseBrush")).Color;
        hero.Children.Add(new Border { Background = new LinearGradientBrush(new GradientStopCollection {
            new(System.Windows.Media.Color.FromArgb(252,baseColor.R,baseColor.G,baseColor.B),0),
            new(System.Windows.Media.Color.FromArgb(235,baseColor.R,baseColor.G,baseColor.B),.45),
            new(System.Windows.Media.Color.FromArgb(100,baseColor.R,baseColor.G,baseColor.B),1) }, new Point(0,0),new Point(1,0)) });
        var poster = Artwork(item, 194, 290);
        poster.HorizontalAlignment = HorizontalAlignment.Right; poster.VerticalAlignment = VerticalAlignment.Center;
        poster.Margin = new Thickness(0, 0, 52, 0); poster.Effect = new DropShadowEffect { BlurRadius = 22, ShadowDepth = 8, Opacity = .42, Color = Colors.Black };
        hero.Children.Add(poster);
        var copy = new StackPanel { Margin = new Thickness(36, 22, 24, 48), VerticalAlignment = VerticalAlignment.Center, MaxWidth = 610, HorizontalAlignment = HorizontalAlignment.Left };
        hero.Children.Add(copy);
        var eyebrow = Text("WX PLAYER  /  ÖNE ÇIKAN " + item.KindLabel.ToUpperInvariant(), 10, "AccentPrimaryBrush");
        eyebrow.FontWeight = FontWeights.SemiBold; copy.Children.Add(eyebrow);
        var title = Text(item.Name, 34); title.FontWeight = FontWeights.SemiBold; title.LineHeight = 39; title.MaxHeight = 82;
        title.TextTrimming = TextTrimming.CharacterEllipsis; title.ToolTip = item.Name; title.Margin = new Thickness(0, 16, 0, 10); copy.Children.Add(title);
        var metadata = Text(item.Category + "   •   " + _heroSource, 12, "TextSecondaryBrush");
        metadata.TextWrapping = TextWrapping.NoWrap; metadata.TextTrimming = TextTrimming.CharacterEllipsis;
        metadata.Margin = new Thickness(0, 0, 0, 20); copy.Children.Add(metadata);
        var actions = new WrapPanel();
        string playLabel = item.Kind == ContentKind.Series ? "Bölümleri keşfet" : item.Kind == ContentKind.Live ? "Canlı izle" : "Şimdi izle";
        var play = Action(playLabel, () => _play(item), true); play.Margin = new Thickness(0, 0, 8, 6); actions.Children.Add(play);
        var favorite = Action(item.IsFavorite ? "Favorilerimde" : "+  Favorilere ekle", () => _favorite(item));
        favorite.Margin = new Thickness(0, 0, 0, 6); actions.Children.Add(favorite); copy.Children.Add(actions);
        var navigation = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(36, 0, 0, 20) };
        hero.Children.Add(navigation);
        for (int i = 0; i < _heroItems.Count; i++)
        {
            int index = i;
            var dot = new Button { Width = i == _heroIndex ? 25 : 9, Height = 8, MinHeight = 8, Padding = new Thickness(0), Margin = new Thickness(0,0,7,0),
                Background = Color(i == _heroIndex ? "AccentPrimaryBrush" : "TextMutedBrush"), BorderThickness = new Thickness(0), ToolTip = $"Öne çıkan {i+1}: {_heroItems[i].Name}" };
            System.Windows.Automation.AutomationProperties.SetName(dot,dot.ToolTip.ToString());
            dot.Click += (_,_) => { _heroIndex = index; RenderHero(); };
            navigation.Children.Add(dot);
        }
        if (_heroItems.Count > 1)
        {
            var arrows = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0,0,26,18) };
            arrows.Children.Add(Arrow("chevron-left", "Önceki öne çıkan içerik", () => MoveHero(-1)));
            arrows.Children.Add(Arrow("chevron-right", "Sonraki öne çıkan içerik", () => MoveHero(1)));
            hero.Children.Add(arrows);
        }
        hero.SizeChanged += (_, _) =>
        {
            bool narrow = hero.ActualWidth < 800;
            copy.MaxWidth = Math.Max(200, Math.Min(610, hero.ActualWidth - (narrow ? 230 : 300)));
            backdrop.Width = Math.Min(710, hero.ActualWidth * .7);
            poster.Width = narrow ? 160 : 194; poster.Height = narrow ? 240 : 290;
            poster.Margin = new Thickness(0,0,narrow?28:52,0);
            title.FontSize = narrow ? 27 : 34; title.LineHeight = narrow ? 32 : 39;
        };
    }
    private void HomeWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta == 0 || SystemParameters.WheelScrollLines == 0) return;
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
        {
            DependencyObject? current = e.OriginalSource as DependencyObject;
            while (current is not null && current != this)
            {
                if (current is ScrollViewer shelf) { shelf.ScrollToHorizontalOffset(shelf.HorizontalOffset - e.Delta * 2); e.Handled = true; return; }
                current = current is Visual ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
            }
        }
        double step = SystemParameters.WheelScrollLines < 0 ? ViewportHeight : SystemParameters.WheelScrollLines * 36;
        ScrollToVerticalOffset(VerticalOffset - e.Delta / 120d * step);
        e.Handled = true;
    }
    private static void Round(FrameworkElement element, double radius)
    {
        element.SizeChanged += (_, _) => element.Clip = new RectangleGeometry(new Rect(0, 0, element.ActualWidth, element.ActualHeight), radius, radius);
    }
}




