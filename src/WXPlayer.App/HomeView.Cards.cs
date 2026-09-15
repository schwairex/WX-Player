using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WXPlayer.Core;

namespace WXPlayer.App;

internal sealed partial class HomeView
{
    private static Grid Artwork(ContentItem item, double width, double height)
    {
        bool live = item.Kind == ContentKind.Live;
        var image = new Grid { Width = width, Height = height, Background = Color("#1B2530"), ClipToBounds = true };
        Round(image, 8);
        image.Children.Add(new ChannelLogo
        {
            Url = item.Logo, Initials = item.Initials, DecodeWidth = live ? 240 : 480,
            ImageStretch = Stretch.Uniform, ImagePadding = new Thickness(live ? 20 : 0)
        });
        return image;
    }
    private void BuildShelf(HomeShelf shelf)
    {
        // Geometry belongs to the shelf, never to an individual item's media kind.
        bool posters = shelf.Section is "movie" or "series";
        double width = posters ? 160 : 224, artHeight = posters ? 224 : 126;
        bool hasProgress = shelf.Page.Items.Any(i => i.Progress is not null);
        var section = new StackPanel { Margin = new Thickness(0, 0, 0, 24), Tag = shelf.Section };
        _shelves.Children.Add(section);
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 10), LastChildFill = true }; section.Children.Add(header);
        var cards = new StackPanel { Orientation = Orientation.Horizontal };
        var scroll = new ScrollViewer { Content = cards, HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, CanContentScroll = false };
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        DockPanel.SetDock(actions, Dock.Right); header.Children.Add(actions);
        var all = Action("Tümünü gör", () => _browse(shelf.Section));
        all.Background = Brushes.Transparent; all.BorderBrush = Brushes.Transparent; all.FontSize = 11; all.Foreground = Color("#ABB9C9"); all.Padding = new Thickness(10, 6, 10, 6); actions.Children.Add(all);
        double Step() => Math.Max(width + 16, Math.Floor(scroll.ViewportWidth / (width + 16)) * (width + 16));
        var prev = Arrow("chevron-left", shelf.Title + " · Önceki içerikler", () => scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset - Step()));
        var next = Arrow("chevron-right", shelf.Title + " · Sonraki içerikler", () => scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset + Step()));
        actions.Children.Add(prev); actions.Children.Add(next);
        var caption = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var title = Text(shelf.Title, 18); title.FontWeight = FontWeights.SemiBold; caption.Children.Add(title);
        var count = Text(shelf.Page.Total.ToString("N0"), 11, "#98A3B6"); count.Margin = new Thickness(10, 4, 0, 0); caption.Children.Add(count); header.Children.Add(caption);
        section.Children.Add(scroll);
        void UpdateArrows() { next.IsEnabled = scroll.HorizontalOffset < scroll.ScrollableWidth - 1; prev.IsEnabled = scroll.HorizontalOffset > 1; }
        scroll.ScrollChanged += (_, _) => UpdateArrows(); scroll.Loaded += (_, _) => UpdateArrows();
        foreach (var item in shelf.Page.Items)
        {
            var card = new StackPanel { Width = width }; card.Children.Add(Artwork(item, width, artHeight));
            var name = Text(item.Name, 12); name.FontWeight = FontWeights.SemiBold;
            name.Height = 36; name.LineHeight = 18; name.Margin = new Thickness(0, 10, 0, 4); name.TextTrimming = TextTrimming.CharacterEllipsis; card.Children.Add(name);
            var category = Text(posters ? item.Category : item.KindLabel + "  ·  " + item.Category, 10, "#98A3B6");
            category.Height = 16; category.TextWrapping = TextWrapping.NoWrap; category.TextTrimming = TextTrimming.CharacterEllipsis; card.Children.Add(category);
            if (hasProgress)
            {
                var progress = new StackPanel { Height = 36, Margin = new Thickness(0, 8, 0, 0) };
                var label = Text(item.ProgressLabel, 10, "#BDD2A8"); label.Height = 16; label.TextWrapping = TextWrapping.NoWrap; label.TextTrimming = TextTrimming.CharacterEllipsis; progress.Children.Add(label);
                progress.Children.Add(new ProgressBar { Minimum = 0, Maximum = 1, Value = item.Progress?.Fraction ?? 0, Height = 3, Margin = new Thickness(0, 6, 0, 0), Foreground = Color("#BBDD95"), Background = Color("#293440"), Visibility = item.Progress is null ? Visibility.Hidden : Visibility.Visible });
                card.Children.Add(progress);
            }
            var button = new Button
            {
                Content = card, Tag = item, Padding = new Thickness(5), Margin = new Thickness(0, 0, 4, 0),
                Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, BorderThickness = new Thickness(1),
                ToolTip = item.Name + (item.ProgressLabel.Length > 0 ? "\n" + item.ProgressLabel : ""), VerticalAlignment = VerticalAlignment.Top
            };
            // A real border is required by the app's keyboard-focus template.
            button.MouseEnter += (_, _) => button.Background = Color("#19212C");
            button.MouseLeave += (_, _) => button.Background = Brushes.Transparent;
            System.Windows.Automation.AutomationProperties.SetName(button, (item.Kind == ContentKind.Series ? "Bölümleri aç · " : "İzle · ") + item.Name);
            button.Click += (_, _) => _play(item); cards.Children.Add(button);
        }
    }
    private static Button Arrow(string icon, string label, Action action)
    {
        var button = Action("", action); button.Content = new SvgIcon(icon) { Width = 14, Height = 14 };
        button.Width = 34; button.MinHeight = 34; button.Padding = new Thickness(8); button.Margin = new Thickness(6, 0, 0, 0); button.ToolTip = label;
        System.Windows.Automation.AutomationProperties.SetName(button, label); return button;
    }
}
