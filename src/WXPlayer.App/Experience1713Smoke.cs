using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using WXPlayer.Core;

namespace WXPlayer.App;

// Opt-in regression fixtures; all writes use the explicit isolated --data-dir.
internal static class Experience1713Smoke
{
    internal static async Task RunAsync(MainWindow w, LibraryStore store, PlayerSettings settings, Dictionary<string, object> r)
    {
        void Check(string key, bool value) => r[key] = value;
        Check("startupHome", w.HomeHost.IsVisible && !w.ContentGrid.IsVisible);
        if (App.Arguments.Contains("--verify-startup1713") || App.Arguments.Contains("--verify-missing-source1713"))
        {
            if (App.Arguments.Contains("--verify-missing-source1713"))
                Check("startupMissingSourceUsesAndSavesFallback", w.SourcePicker.SelectedItem is SourceConfig selected &&
                    selected.Id == w.SourcePicker.Items.Cast<SourceConfig>().First().Id && App.ReadSettings().LastSourceId == selected.Id);
            else Check("startupRestoresLastSource", (w.SourcePicker.SelectedItem as SourceConfig)?.Id == "experience1713-X");
            Finish(); return;
        }
        var x = new SourceConfig { Id = "experience1713-X", Name = "X", Address = "https://example.test/x.m3u" };
        var y = x with { Id = "experience1713-Y", Name = "Y" };
        int mediaArg = Array.IndexOf(App.Arguments, "--media");
        string media = mediaArg >= 0 ? new Uri(Path.GetFullPath(App.Arguments[mediaArg + 1])).AbsoluteUri : "https://example.test/video.ts";
        await using var art = new SmokeHttpServer(HomeLayoutSmoke.Poster(96, 144, "WX", "#29332F"));
        async IAsyncEnumerable<ContentItem> Items(SourceConfig source)
        {
            foreach (var kind in new[] { ContentKind.Movie, ContentKind.Series })
                for (int i = 0; i < 18; i++)
                    yield return new() { Id = source.Id + kind + i, SourceId = source.Id, Kind = kind, Name = $"İçerik {i:D2}", Category = $"Kategori {i:D2}", SeriesId = kind == ContentKind.Series ? source.Id + "show" + i : "", Logo = art.Url, Url = media, IsFavorite = i == 0 };
            yield return new() { Id = source.Id + "-live", SourceId = source.Id, Kind = ContentKind.Live, Name = "TR • WX Kanal FHD", Category = "Haber", Logo = art.Url, Url = media, IsFavorite = true };
            for (int i = 1; i <= 2; i++) yield return new() { Id = source.Id + "-episode" + i, SourceId = source.Id, Kind = ContentKind.Episode, Name = $"İçerik 17 S01E{i:D2}", Category = "Kategori 17", SeriesId = source.Id + ContentKind.Series + 17, SeriesName = "İçerik 17", Season = 1, Episode = i, Logo = art.Url, Url = media };
            await Task.Yield();
        }
        await store.ImportAsync(x, Items(x), null, default);
        await store.ImportAsync(y, Items(y), null, default);
        await w.SmokeRefreshAsync(x.Id);
        if (App.Arguments.Contains("--hero1713-race-only"))
        {
            await HeroRace(w, store, x, Check); Finish(); return;
        }
        await w.SmokeBrowseAsync("home");
        w.SmokeHome.Search.Text = "İçerik";
        await Settle();
        Check("homeSearchStartsLeft", CaretStartsLeft(w.SmokeHome.Search));
        w.SmokeHome.Search.Clear();
        foreach (string section in new[] { "movie", "series" })
        {
            await w.SmokeBrowseAsync(section); await Settle();
            var catalogue = w.SmokeCatalog;
            var hero = catalogue.Hero.Children.Cast<UIElement>().ToArray();
            catalogue.Loading(section == "series", false);
            catalogue.Loading(section == "series", true);
            Check(section + "CancelledContextRestoresHero", catalogue.Hero.Visibility == Visibility.Visible && hero.SequenceEqual(catalogue.Hero.Children.Cast<UIElement>()));
            await w.SmokeBrowseAsync(section); await Settle();
            hero = catalogue.Hero.Children.Cast<UIElement>().ToArray();
            var chips = Settings1711Smoke.Descendants<Button>(catalogue).Where(b => b.Content is string s && s.StartsWith("Kategori ")).ToArray();
            Check(section + "AllProviderCategories", chips.Length == 18 && chips.Any(b => Equals(b.Content, "Kategori 17")));
            var last = chips.FirstOrDefault(b => Equals(b.Content, "Kategori 17"));
            if (last is not null)
            {
                last.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Settle();
                Check(section + "LateCategoryUsable", Settings1711Smoke.Descendants<TextBlock>(catalogue).Any(t => t.Text == "İçerik 17"));
            }
            catalogue.Search.Text = "İçerik 17"; await Settle();
            Check(section + "SearchStartsLeft", CaretStartsLeft(catalogue.Search));
            Check(section + "HeroStableInSearch", hero.SequenceEqual(catalogue.Hero.Children.Cast<UIElement>()) && catalogue.Hero.Visibility == Visibility.Visible);
            catalogue.Search.Text = "eşleşmeyen-başlık"; await Settle();
            Check(section + "HeroStableForNoResults", hero.SequenceEqual(catalogue.Hero.Children.Cast<UIElement>()) && catalogue.Hero.Visibility == Visibility.Visible);
            Check(section + "CategoriesRemainDuringSearch", Settings1711Smoke.Descendants<Button>(catalogue).Count(b => b.Content is string s && s.StartsWith("Kategori ")) == 18);
            catalogue.Search.Clear(); await Settle();
            Settings1711Smoke.Save(w, section + "-1713.png");
        }
        await w.SmokeBrowseAsync("movie");
        var progressed = (await store.FindAsync(x.Id + ContentKind.Movie + 0))!;
        await store.SaveProgressAsync(progressed, 6500, 20000);
        await w.SmokeRefreshAsync(x.Id); await Settle();
        Check("catalogueRefreshReadsFreshHeroProgress", Settings1711Smoke.Descendants<TextBlock>(w.SmokeCatalog.Hero).Any(t => t.Text == "KALDIĞIN YERDEN DEVAM ET"));
        await HeroRace(w, store, x, Check);
        var live = (await store.QueryAsync(x.Id, ContentKind.Live, null, "", false, false, 0)).Items.Single();
        await w.SmokeBrowseAsync("live");
        if (mediaArg >= 0) await w.SmokePlayAsync(live);
        foreach (string section in new[] { "favorites", "epg", "recent", "movie", "series" })
        {
            await w.SmokeBrowseAsync(section); await Settle();
            if (section is "movie" or "series")
                ((Button)w.SmokeCatalog.ResumeControl).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Settle();
            Check(section + "UsesModernPlayer", Field<bool>(w, "_liveShellAttached") && w.ContentGrid.IsVisible && w.GuidePanel.Background?.ToString() == "#00FFFFFF");
            Check(section + "OriginalControlsRetained", w.PlayButton.IsVisible && w.FullscreenButton.IsVisible && w.RecordButton.Parent is not null);
        }
        Settings1711Smoke.Save(w, "shared-player-1713.png");
        w.SmokeFullscreen(); await Settle();
        Check("fullscreenDetachesEmbeddedOverlay", !Field<bool>(w, "_liveShellAttached"));
        w.SmokeFullscreen(); await Settle();
        Check("fullscreenRestoresModernPlayer", Field<bool>(w, "_liveShellAttached"));
        if (mediaArg >= 0)
        {
            var series = (await store.FindAsync(x.Id + ContentKind.Series + 17))!;
            var episode = (await store.PlaylistEpisodesAsync(series)).First(i => i.Episode == 1);
            await w.SmokePlayAsync(episode); await Settle();
            var overlay = Field<Window>(w, "_liveControls");
            overlay.Activate(); w.FullscreenButton.Focus();
            overlay.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(overlay), 0, Key.Tab) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            await Settle();
            Check("episodeTabReachesEpisodePanel", w.SmokeSeriesPanel.IsKeyboardFocusWithin);
            Check("episodePanelReplacesEpgOnly", w.SmokeSeriesPanel.IsVisible && !w.GuidePanel.IsVisible);
        }
        await w.SmokeBrowseAsync("home"); await Settle();
        Check("homeDetachesOverlay", !Field<bool>(w, "_liveShellAttached"));
        // Exercise a real source-selection event, not the suppressed QA reload.
        w.SourcePicker.SelectedItem = w.SourcePicker.Items.Cast<SourceConfig>().Single(s => s.Id == y.Id);
        await Settle();
        w.SourcePicker.SelectedItem = w.SourcePicker.Items.Cast<SourceConfig>().Single(s => s.Id == x.Id);
        await Settle();
        using (var json = JsonDocument.Parse(File.Exists(Path.Combine(App.DataDirectory, "settings.json")) ? File.ReadAllText(Path.Combine(App.DataDirectory, "settings.json")) : "{}"))
            Check("lastSourcePersisted", json.RootElement.TryGetProperty("LastSourceId", out var id) && id.GetString() == x.Id);
        await SourceChecks(w, Check);
        Finish();

        void Finish()
        {
            var failed = r.Where(pair => pair.Value is false).Select(pair => pair.Key).ToArray();
            if (failed.Length > 0) throw new InvalidOperationException("1.7.13: " + string.Join(", ", failed));
        }
    }
    private static async Task SourceChecks(MainWindow owner, Action<string, bool> check)
    {
        var original = new SourceConfig { Name = "Düzenleme", Kind = SourceKind.Xtream, Address = "http://example.test:8080", Username = "retained-user", Password = "retained-secret", Mac = "00:1A:79:00:00:00" };
        var view = new SourceWindow(owner, original); view.Show(); await Settle();
        var inputs = Settings1711Smoke.Descendants<TextBox>(view).ToArray();
        var password = Settings1711Smoke.Descendants<PasswordBox>(view).Single();
        Button Option(string text) => Settings1711Smoke.Descendants<Button>(view).Single(b => b.Content is IconLabel label && label.Label == text);
        Button Action(string text) => Settings1711Smoke.Descendants<Button>(view).Single(b => b.Content is string s && s == text || b.Content is IconLabel label && label.Label == text);
        Option("M3U / TXT").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Settle();
        check("sourceWholeXtreamSectionHidden", !Settings1711Smoke.Descendants<TextBlock>(view).Single(t => t.Text == "Hesabınız").IsVisible);
        var browse = Action("Dosyadan seç");
        check("sourceBrowseBesideAddress", browse.IsVisible && Math.Abs(browse.TranslatePoint(new Point(), view).Y - inputs[1].TranslatePoint(new Point(), view).Y) < 6);
        Option("Stalker Portal").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Settle();
        check("sourceStalkerSectionVisible", Settings1711Smoke.Descendants<TextBlock>(view).Single(t => t.Text == "Portal kimliği").IsVisible && !browse.IsVisible);
        Option("Xtream Codes").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Settle();
        check("sourceTypeChangesKeepValues", inputs[2].Text == original.Username && password.Password == original.Password);
        check("sourcePasswordRemainsPrivate", password.ToolTip is null && AutomationProperties.GetName(password) != original.Password);
        check("sourceApprovedHintsExist", Settings1711Smoke.Descendants<TextBlock>(view).Any(t => t.Text == "Kullanıcı adınız") && Settings1711Smoke.Descendants<TextBlock>(view).Any(t => t.Text == "http://sunucu.com:8080"));
        inputs[0].Clear(); Action("Bağlan ve yükle").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Settle();
        check("sourceOriginalValidationText", Settings1711Smoke.Descendants<TextBlock>(view).Any(t => t.Text == "Kaynak adı ve adresi gerekli.") && view.Result is null);
        inputs[0].Text = original.Name; Settings1711Smoke.Save(view, "source-xtream-1713.png");
        Option("M3U / TXT").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Settle(); Settings1711Smoke.Save(view, "source-m3u-1713.png");
        Option("Stalker Portal").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Settle(); Settings1711Smoke.Save(view, "source-stalker-1713.png");
        Action("Vazgeç").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        check("sourceCancelUnchanged", view.Result is null && original.Username == "retained-user" && original.Password == "retained-secret");
        foreach (var kind in new[] { SourceKind.Playlist, SourceKind.Xtream, SourceKind.Stalker })
        {
            var config = original with { Kind = kind, Address = kind == SourceKind.Playlist ? Path.Combine(AppContext.BaseDirectory, "samples", "open-films.m3u") : original.Address };
            var modal = new SourceWindow(owner, config);
            modal.Loaded += (_, _) => modal.Dispatcher.BeginInvoke(() =>
                Settings1711Smoke.Descendants<Button>(modal).Single(b => Equals(b.Content, "Bağlan ve yükle")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            bool? accepted = modal.ShowDialog();
            check("sourceModalResult_" + kind, accepted == true && modal.Result is { } result && result.Id == config.Id && result.Kind == kind && result.Username == config.Username && result.Password == config.Password && result.Mac == config.Mac && !ReferenceEquals(result, config));
        }
    }
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    private static async Task HeroRace(MainWindow w, LibraryStore store, SourceConfig source, Action<string, bool> check)
    {
        await w.SmokeBrowseAsync("movie");
        var first = (await store.FindAsync(source.Id + ContentKind.Movie + 0))!;
        var next = (await store.FindAsync(source.Id + ContentKind.Movie + 17))!;
        await store.SaveProgressAsync(first, 6500, 20000);
        await w.SmokeRefreshAsync(source.Id);
        await store.SaveProgressAsync(first, 20000, 20000, true);
        await store.SaveProgressAsync(next, 5500, 20000);
        var refresh = typeof(MainWindow).GetMethod("RefreshCatalogAsync", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(bool) }, null)!;
        var full = (Task)refresh.Invoke(w, new object[] { false })!;
        w.SmokeCatalog.Search.Text = next.Name;
        var search = (Task)refresh.Invoke(w, new object[] { true })!;
        await Task.WhenAll(full, search);
        w.UpdateLayout();
        check("searchDuringRefreshUsesFreshHero", Settings1711Smoke.Descendants<TextBlock>(w.SmokeCatalog.Hero).Any(t => t.Text == next.Name && Math.Abs(t.FontSize - 38.4) < .01));
        w.SmokeCatalog.Search.Clear();
    }
    private static bool CaretStartsLeft(TextBox input)
    {
        input.UpdateLayout();
        return input.GetRectFromCharacterIndex(0).X <= input.Padding.Left + 8;
    }
    private static async Task Settle() => await Task.Delay(650);
}
