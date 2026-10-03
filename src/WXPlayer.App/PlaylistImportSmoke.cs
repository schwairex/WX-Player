using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using WXPlayer.Core;

namespace WXPlayer.App;

// Explicit smoke mode only. Never copy playlist contents or stream addresses into reports.
internal static class PlaylistImportSmoke
{
    internal static async Task RunAsync(MainWindow window, LibraryStore store, Dictionary<string, object> results)
    {
        void Check(string key, bool value) { results[key] = value; if (!value) throw new InvalidOperationException(key); }
        int arg = Array.IndexOf(App.Arguments, "--playlist-import");
        if (arg < 0 || arg + 1 >= App.Arguments.Length || !File.Exists(App.Arguments[arg + 1]))
            throw new InvalidOperationException("An existing local playlist is required for import smoke.");
        string file = Path.GetFullPath(App.Arguments[arg + 1]);
        var source = new SourceConfig { Id = "playlist-import-smoke", Name = "Yerel M3U doğrulaması", Address = file, Kind = SourceKind.Playlist };
        bool artwork = ArtworkService.Enabled; ArtworkService.Enabled = false;
        try
        {
            // Exercise the real source dialog's validation and existing submit handler.
            var dialog = new SourceWindow(window, source);
            dialog.Loaded += (_, _) => dialog.Dispatcher.BeginInvoke(new Action(() =>
            {
                dialog.Footer.Children.OfType<Button>().Single(b => Equals(b.Content, "Bağlan ve yükle")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (dialog.Result is null) dialog.Close();
            }));
            dialog.ShowDialog();
            Check("localFileAcceptedBySourceDialog", dialog.Result is { Kind: SourceKind.Playlist } && dialog.Result.Address == file);
            var timer = Stopwatch.StartNew();
            int heartbeats = 0;
            var pulse = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            pulse.Tick += (_, _) => heartbeats++;
            pulse.Start();
            try { await window.SmokeImportSourceAsync(dialog.Result!); }
            finally { pulse.Stop(); }
            results["importElapsedMs"] = timer.ElapsedMilliseconds;
            results["uiHeartbeats"] = heartbeats;
            var saved = (await store.SourcesAsync()).SingleOrDefault(s => s.Id == source.Id);
            Check("sourcePersistedAndReloaded", saved is not null && saved.Address == file && window.SourcePicker.SelectedItem is SourceConfig selected && selected.Id == source.Id);
            Check("importSuccessStatus", window.StatusText.Text.StartsWith("✓") && !window.StatusText.Text.Contains("İşlem tamamlanamadı"));
            Check("importKeptUiResponsive", heartbeats > 2);
            var stats = await store.StatsAsync(source.Id);
            results["liveChannels"] = stats.Live; results["movies"] = stats.Movies; results["series"] = stats.Series;
            Check("libraryContainsContent", stats.Total > 0);
            await window.SmokeBrowseAsync("live");
            Check("liveLibraryVisible", window.ChannelList.Items.Count > 0 && window.ChannelList.Items.Cast<ContentItem>().All(i => i.Kind == ContentKind.Live));
            Check("noImportErrorLog", !File.Exists(Path.Combine(App.DataDirectory, "errors.log")));
        }
        finally { ArtworkService.Enabled = artwork; }
    }
}

public partial class MainWindow
{
    internal Task SmokeImportSourceAsync(SourceConfig source) => ImportSourceAsync(source);
}
