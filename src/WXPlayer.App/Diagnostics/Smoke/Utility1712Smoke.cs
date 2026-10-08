using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using WXPlayer.Core;

namespace WXPlayer.App;

// Explicit QA only, isolated --data-dir. Uses the same native playback and picker APIs as the UI.
internal static class Utility1712Smoke
{
    internal static async Task RunAsync(MainWindow window,LibraryStore store,PlaybackEngine engine,PlayerSettings settings,Dictionary<string,object> results)
    {
        void Check(string key,bool ok){results[key]=ok;if(!ok)throw new InvalidOperationException(key);}
        T Field<T>(object view,string name)=>(T)view.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(view)!;
        List<TextBlock> Texts(DependencyObject view)=>Settings1711Smoke.Descendants<TextBlock>(view).ToList();
        bool Has(DependencyObject view,string text)=>Texts(view).Any(t=>t.Text==text);
        void SafeVisuals(Window view)=>Check(view.GetType().Name+"HasNoTextEffects",Settings1711Smoke.Descendants<FrameworkElement>(view).All(e=>e.Effect is null&&e.OpacityMask is null&&e.CacheMode is not BitmapCache));
        Check("utilityStateDisplayFallback",new[]{"Playing","Paused","Buffering","Opening","Stopped","Ended","Error","NothingSpecial","FutureState"}.Select(UtilityDisplayFormatter.State).SequenceEqual(new[]{"Oynuyor","Duraklatıldı","Arabelleğe alınıyor","Açılıyor","Durduruldu","Bitti","Hata","Boşta","FutureState"}));
        Check("utilityOutputDisplayFallback",new[]{"direct3d11","direct3d9","any","custom"}.Select(UtilityDisplayFormatter.Output).SequenceEqual(new[]{"Direct3D 11","Direct3D 9","Otomatik","custom"}));
        Check("miniTitleDisplayConservative",UtilityDisplayFormatter.MiniTitle("TR • ASPOR FHD")==("ASPOR","FHD")&&UtilityDisplayFormatter.MiniTitle("DE: WX Kanal HD")==("WX Kanal","HD")&&UtilityDisplayFormatter.MiniTitle("Film HD Edition")==("Film HD Edition","")&&UtilityDisplayFormatter.MiniTitle("FHD")==("FHD",""));
        Check("trackLabelLanguagesAndFallback",UtilityDisplayFormatter.TrackLabel("Track 1 - [Turkish]")==("Türkçe","Parça 1")&&UtilityDisplayFormatter.TrackLabel("Track 2 - [English]")==("İngilizce","Parça 2")&&UtilityDisplayFormatter.TrackLabel("Track 4 - [Unknown]")==("Track 4 - [Unknown]","")&&UtilityDisplayFormatter.TrackLabel("Ses kapalı")==("Ses kapalı",""));
        foreach(string language in new[]{"German","French","Spanish","Italian","Arabic","Russian","Persian","Japanese"})Check("trackLanguage"+language,UtilityDisplayFormatter.TrackLabel("Track 7 - ["+language+"]").Track=="Parça 7");

        var emptyStats=new StatisticsWindow(window,()=>("",null),engine,settings);emptyStats.Show();await Task.Delay(200);
        Check("statisticsEmptyStatePreserved",Has(emptyStats,"Oynatıcı")&&Has(emptyStats,"Henüz içerik oynatılmıyor")&&Texts(emptyStats).Count(t=>t.Text=="—")==3&&!Has(emptyStats,"Parçalar"));
        Check("statisticsOneSecondLoadedTimer",Field<DispatcherTimer>(emptyStats,"_timer") is {IsEnabled:true} t&&t.Interval==TimeSpan.FromSeconds(1));
        Settings1711Smoke.Save(emptyStats,"WXPlayer-1.7.12-statistics-empty.png");emptyStats.Close();
        Check("statisticsTimerStopsOnClose",!Field<DispatcherTimer>(emptyStats,"_timer").IsEnabled);
        var emptyTracks=new TracksWindow(window,engine);emptyTracks.Show();await Task.Delay(200);
        Check("tracksEmptyStatePreserved",!emptyTracks.AudioPicker.IsEnabled&&!emptyTracks.SubtitlePicker.IsEnabled&&Field<TextBlock>(emptyTracks,"_status").Text=="Önce bir içerik oynatın. Kullanılabilir parçalar burada listelenir.");
        Check("tracksReadOnlyPickerContract",typeof(TracksWindow).GetField("AudioPicker",BindingFlags.Instance|BindingFlags.NonPublic)!.IsInitOnly&&typeof(TracksWindow).GetField("SubtitlePicker",BindingFlags.Instance|BindingFlags.NonPublic)!.IsInitOnly&&emptyTracks.AudioPicker.SelectedValuePath=="Id"&&emptyTracks.SubtitlePicker.SelectedValuePath=="Id");
        Settings1711Smoke.Save(emptyTracks,"WXPlayer-1.7.12-tracks-empty.png");emptyTracks.Close();
        Check("tracksTimerStopsOnClose",!Field<DispatcherTimer>(emptyTracks,"_refresh").IsEnabled);
        string media=App.Arguments[Array.IndexOf(App.Arguments,"--media")+1];
        var source=new SourceConfig{Id="utility1712-qa",Name="Yerel test videosu",Address=Path.Combine(AppContext.BaseDirectory,"samples","open-films.m3u")};
        var item=new ContentItem{Id="utility1712-vod",SourceId=source.Id,Name="TR • Sintel FHD",Kind=ContentKind.Movie,Url=new Uri(Path.GetFullPath(media)).AbsoluteUri};
        async IAsyncEnumerable<ContentItem> Items(){yield return item;await Task.Yield();}
        await store.ImportAsync(source,Items(),null,default);await window.SmokeRefreshAsync(source.Id);await window.SmokeBrowseAsync("all");await window.SmokePlayAsync(item);
        await Wait(()=>engine.Player.IsPlaying&&engine.Player.Length>0);await Task.Delay(500);
        var target=new PlaybackTarget(item.Url);
        string privateUrl="https://username:secret@example.test:1234/live/private-token/channel.ts?password=hidden";
        Check("statisticsAddressMaskPreserved",StatisticsWindow.SafeAddress(privateUrl)=="https://example.test:1234/•••"&&StatisticsWindow.SafeAddress(item.Url).StartsWith("Yerel dosya · "));
        var stats=new StatisticsWindow(window,()=>("Sintel · Yerel test videosu",new PlaybackTarget(privateUrl)),engine,settings);stats.Show();await Task.Delay(1100);
        var summary=Field<StackPanel>(stats,"_top");var statsScroll=Settings1711Smoke.Descendants<ScrollViewer>(stats.Body).Single();
        Check("statisticsSummaryOutsideScroll",!statsScroll.IsAncestorOf(summary)&&Grid.GetRow(statsScroll)==1);
        Check("statisticsMeasurementsBeforeEngineAndTracks",Field<StackPanel>(stats,"_cards").Children[0] is StackPanel first&&Has(first,"Akış ölçümleri")&&Field<StackPanel>(stats,"_cards").Children[1] is StackPanel second&&Has(second,"Oynatma ve motor"));
        Check("statisticsAllOriginalMetricsPresent",new[]{"ÇÖZÜNÜRLÜK","KAYNAK FPS","SES","Giriş bit hızı","Tampon doluluğu","Okunan veri","Çözülen / gösterilen kare","Kaybedilen kare","Akış bozulması / süreksizlik","Durum","Motor / video çıkışı","Donanım çözme tercihi","Ağ önbelleği","Codec","Parça bit hızı"}.All(text=>Has(stats,text)));
        Check("statisticsSelectedAudioPill",Has(stats,"SEÇİLİ")&&!Texts(stats).Any(t=>t.Text=="Ses · Seçili"));
        Check("statisticsNoUnknownLanguage",!Texts(stats).Any(t=>t.Text.Contains("???")));
        Check("statisticsNoPrivateAddressInVisualTree",Settings1711Smoke.Descendants<FrameworkElement>(stats).All(e=>!new[]{e.ToolTip?.ToString()??"",AutomationProperties.GetName(e),e is TextBlock text?text.Text:""}.Any(s=>s.Contains("private-token")||s.Contains("secret")||s.Contains("password=hidden"))));
        Check("statisticsManropeAndClearType",stats.FontFamily.Source.Contains("Manrope")&&TextOptions.GetTextFormattingMode(stats)==TextFormattingMode.Ideal&&TextOptions.GetTextRenderingMode(stats)==TextRenderingMode.ClearType);
        SafeVisuals(stats);Settings1711Smoke.Save(stats,"WXPlayer-1.7.12-statistics.png");
        var anchor=summary.TranslatePoint(new Point(),stats);statsScroll.ScrollToEnd();await Task.Delay(1100);stats.UpdateLayout();
        Check("statisticsFixedSummaryAndScrollSurviveRefresh",statsScroll.VerticalOffset>0&&summary.TranslatePoint(new Point(),stats)==anchor);
        Settings1711Smoke.Save(stats,"WXPlayer-1.7.12-statistics-tracks.png");
        engine.Player.SetPause(true);await Wait(()=>engine.Player.State==VLCState.Paused);await Task.Delay(1100);
        Check("statisticsTranslatedPausedState",Has(stats,"Duraklatıldı"));stats.Close();Check("statisticsPlayingTimerStops",!Field<DispatcherTimer>(stats,"_timer").IsEnabled);engine.Player.SetPause(false);await Wait(()=>engine.Player.IsPlaying);

        string subtitle=Path.Combine(App.DataDirectory,"utility1712-subtitle.srt");File.WriteAllText(subtitle,"1\n00:00:00,000 --> 00:00:20,000\nWX Player gerçek altyazı testi\n");
        Check("tracksExternalSubtitleEngineFlow",engine.Player.AddSlave(MediaSlaveType.Subtitle,new Uri(subtitle).AbsoluteUri,true));
        var tracks=new TracksWindow(window,engine);tracks.Show();await Task.Delay(1200);
        Check("tracksExistingIdAndChoiceSource",tracks.AudioPicker.ItemsSource is IEnumerable<TracksWindow.Choice>&&tracks.SubtitlePicker.ItemsSource is IEnumerable<TracksWindow.Choice>&&tracks.AudioPicker.SelectedValuePath=="Id"&&tracks.SubtitlePicker.SelectedValuePath=="Id"&&tracks.AudioPicker.DisplayMemberPath==""&&tracks.AudioPicker.ItemTemplate is not null);
        Check("tracksSelectedPickerRawTooltip",tracks.AudioPicker.SelectedItem is TracksWindow.Choice choice&&tracks.AudioPicker.ToolTip?.ToString()==choice.Label);
        Check("tracksAccessibleStatusAndPickers",AutomationProperties.GetName(tracks.AudioPicker)=="Ses parçası"&&AutomationProperties.GetName(tracks.SubtitlePicker)=="Altyazı"&&AutomationProperties.GetLiveSetting(Field<TextBlock>(tracks,"_status"))==AutomationLiveSetting.Polite);
        Check("tracksSharedSettingsStyleAndFormats",((Style)tracks.FindResource("UtilityTrackPicker")).BasedOn==tracks.FindResource("SettingsOutput")&&new[]{"SRT","ASS","SSA","VTT","SUB"}.All(text=>Has(tracks,text)));
        Check("tracksPlainStatusNoBox",Field<TextBlock>(tracks,"_status").Parent is Grid g&&g.Background is null);
        SafeVisuals(tracks);Settings1711Smoke.Save(tracks,"WXPlayer-1.7.12-tracks.png");
        tracks.AudioPicker.IsDropDownOpen=true;await Task.Delay(120);
        var popup=(Popup)tracks.AudioPicker.Template.FindName("PART_Popup",tracks.AudioPicker);
        Check("tracksNativeComboPopupAndTemplate",popup.IsOpen&&popup.Child is Border&&tracks.AudioPicker.ItemContainerGenerator.ContainerFromIndex(tracks.AudioPicker.SelectedIndex) is ComboBoxItem selected&&selected.IsSelected);
        Settings1711Smoke.Save((FrameworkElement)popup.Child,"WXPlayer-1.7.12-tracks-popup.png");
        var esc=new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(tracks),0,Key.Escape){RoutedEvent=Keyboard.PreviewKeyDownEvent};
        tracks.AudioPicker.RaiseEvent(esc);
        Check("tracksEscapeClosesOnlyOpenPicker",tracks.IsVisible&&!tracks.AudioPicker.IsDropDownOpen&&esc.Handled);
        var selectedAudio=tracks.AudioPicker.SelectedValue;tracks.AudioPicker.SelectedValue=-1;await Task.Delay(100);
        Check("tracksAudioSelectionAppliesOriginalHandler",engine.Player.AudioTrack==-1&&Field<TextBlock>(tracks,"_status").Text=="Ses tercihiniz uygulandı.");tracks.AudioPicker.SelectedValue=selectedAudio;
        if(tracks.SubtitlePicker.Items.Count>1){tracks.SubtitlePicker.SelectedValue=tracks.SubtitlePicker.Items.OfType<TracksWindow.Choice>().First(c=>c.Id>=0).Id;tracks.SubtitlePicker.SelectedValue=-1;await Task.Delay(100);Check("tracksSubtitleOffAppliesOriginalHandler",engine.Player.Spu==-1&&Field<TextBlock>(tracks,"_status").Text=="Altyazılar kapatıldı.");}
        tracks.Width=540;tracks.UpdateLayout();Settings1711Smoke.Save(tracks,"WXPlayer-1.7.12-tracks-narrow.png");Check("tracksMinimumWidthNoHorizontalScroll",Settings1711Smoke.Descendants<ScrollViewer>(tracks.Body).First().ExtentWidth<=Settings1711Smoke.Descendants<ScrollViewer>(tracks.Body).First().ViewportWidth+1);
        tracks.Close();Check("tracksPlayingTimerStops",!Field<DispatcherTimer>(tracks,"_refresh").IsEnabled);

        IntPtr originalHwnd=engine.Player.Hwnd;window.SmokeToggleMiniPlayer();await Wait(()=>window.SmokeMiniWindow is {IsVisible:true});await Task.Delay(200);
        var mini=(MiniPlayerWindow)window.SmokeMiniWindow!;mini.UpdateLayout();
        Check("miniNativeVideoSameHwndAndVisible",engine.Player.Hwnd==originalHwnd&&GetParent(engine.Player.Hwnd)==mini.Video.Handle&&IsWindowVisible(engine.Player.Hwnd));
        Check("miniOpaqueWindowAndMatchedFrame",!mini.AllowsTransparency&&mini.Content is Border frame&&frame.Background==mini.Background&&frame.CornerRadius==new CornerRadius(10));
        Check("miniDimensionsChromeAndTopmost",mini.Width==520&&mini.Height==400&&mini.MinWidth==380&&mini.MinHeight==250&&mini.Topmost&&!mini.ShowInTaskbar&&mini.ResizeMode==ResizeMode.CanResize&&WindowChrome.GetWindowChrome(mini) is {CaptionHeight:0} chrome&&chrome.ResizeBorderThickness==new Thickness(6));
        Check("miniBottomRightPlacement",Math.Abs(mini.Left-Math.Max(SystemParameters.WorkArea.Left,SystemParameters.WorkArea.Right-mini.Width-22))<2&&Math.Abs(mini.Top-Math.Max(SystemParameters.WorkArea.Top,SystemParameters.WorkArea.Bottom-mini.Height-22))<2);
        Check("miniTitleQualityAndRawTooltip",Has(mini,"Sintel")&&Has(mini,"FHD")&&Field<TextBlock>(mini,"_title").ToolTip?.ToString()==item.Name);
        var buttons=Settings1711Smoke.Descendants<Button>(mini).ToArray();
        Button Button(string label)=>buttons.Single(b=>AutomationProperties.GetName(b)==label);
        Check("miniAccessibleTabOrder",KeyboardNavigation.GetTabIndex(Button("Kapat ve ana pencereye dön"))==0&&KeyboardNavigation.GetTabIndex(Button("Ana pencereye dön"))==1&&KeyboardNavigation.GetTabIndex(Button("Oynat / duraklat · Space"))==2&&KeyboardNavigation.GetTabIndex(Button("Sesi kapat / aç"))==3);
        Check("miniCompactStripsAndIcons",((Grid)((Border)mini.Content).Child).RowDefinitions[0].Height.Value==52&&((Grid)((Border)mini.Content).Child).RowDefinitions[2].Height.Value==56&&Button("Ana pencereye dön").ActualWidth==36&&Button("Kapat ve ana pencereye dön").ActualWidth==36);
        var progress=Field<ProgressBar>(mini,"_progress");mini.SetProgress(30000,120000,false);
        Check("miniNoninteractiveThreePixelProgress",progress.Height==3&&!progress.IsHitTestVisible&&!progress.Focusable&&progress.Value==.25&&progress.Visibility==Visibility.Visible&&Field<TextBlock>(mini,"_time").Text=="00:00:30  /  00:02:00");
        SafeVisuals(mini);Settings1711Smoke.Save(mini,"WXPlayer-1.7.12-mini-vod.png");
        Button("Oynat / duraklat · Space").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));await Wait(()=>engine.Player.State==VLCState.Paused);Check("miniOriginalPauseCallback",!engine.Player.IsPlaying);
        Button("Oynat / duraklat · Space").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));await Wait(()=>engine.Player.IsPlaying);Check("miniOriginalResumeCallback",engine.Player.Hwnd==originalHwnd);
        bool muted=engine.Player.Mute;Button("Sesi kapat / aç").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));await Task.Delay(150);Check("miniOriginalMuteCallback",engine.Player.Mute!=muted);Button("Sesi kapat / aç").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        mini.SetProgress(0,0,true);mini.UpdateLayout();Check("miniSeparateLivePillAndHiddenEyebrow",Field<TextBlock>(mini,"_time").Text=="●  CANLI"&&Field<TextBlock>(mini,"_time").Visibility==Visibility.Collapsed&&Field<TextBlock>(mini,"_eyebrow").Visibility==Visibility.Collapsed&&Field<Border>(mini,"_liveBadge").IsVisible&&progress.Visibility==Visibility.Collapsed);
        Settings1711Smoke.Save(mini,"WXPlayer-1.7.12-mini-live.png");
        mini.SetProgress(0,0,false);Check("miniUnknownDurationPreserved",Field<TextBlock>(mini,"_time").Text=="Yayın hazırlanıyor"&&progress.Visibility==Visibility.Collapsed);
        mini.Width=380;mini.Height=250;mini.SetProgress(30000,120000,false);mini.UpdateLayout();Settings1711Smoke.Save(mini,"WXPlayer-1.7.12-mini-narrow.png");
        Check("miniMinimumSizeVideoAndControlsVisible",mini.Video.ActualWidth>300&&mini.Video.ActualHeight>100&&buttons.All(b=>b.IsVisible));
        Button("Ana pencereye dön").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));await Wait(()=>!window.SmokeMiniPlayerVisible);
        Check("miniReturnRestoresNativeVideo",engine.Player.Hwnd==originalHwnd&&GetParent(originalHwnd)==window.Video.Handle&&engine.Player.IsPlaying);
        window.SmokeToggleMiniPlayer();await Wait(()=>window.SmokeMiniWindow is {IsVisible:true});
        var again=(MiniPlayerWindow)window.SmokeMiniWindow!;
        Settings1711Smoke.Descendants<Button>(again).Single(b=>AutomationProperties.GetName(b)=="Kapat ve ana pencereye dön").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));await Wait(()=>!window.SmokeMiniPlayerVisible);
        Check("miniCloseAlsoRestoresNativeVideo",GetParent(originalHwnd)==window.Video.Handle&&engine.Player.IsPlaying);
    }
    private static async Task Wait(Func<bool> test){var until=DateTime.UtcNow.AddSeconds(15);while(!test()&&DateTime.UtcNow<until)await Task.Delay(40);if(!test())throw new TimeoutException("Utility 1.7.12 QA wait");}
    [DllImport("user32.dll")]private static extern IntPtr GetParent(IntPtr hwnd);
    [DllImport("user32.dll")]private static extern bool IsWindowVisible(IntPtr hwnd);
}
