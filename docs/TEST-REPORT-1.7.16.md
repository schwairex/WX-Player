# WXPlayer 1.7.16 doğrulama

2 Kasım 2026 · Windows x64 · .NET 10 WPF

Dal: `dizi-izleme-1.7.16`; taban: `8e7dbdb` (1.7.15). Depo: `E:\WX Player`.
QA kanıtları: `E:\Codex\2026-09-05\bu-klas-r-i-indekileri-analiz\work\qa\v1716-20261101`.
Testler ayrı `--data-dir`, yerel gerçek video ve izole kütüphane kullanır; kullanıcının özel kaynağı değiştirilmedi.

## Sonuçlar

| Doğrulama | Sonuç / kanıt |
| --- | --- |
| Release build | 0 hata, 0 uyarı; `final-build.log` |
| Çekirdek testler | 78/78; yayın betiği de aynı testleri çalıştırır |
| Dizi izleme | `final-series-ready`: kategori/liste/oynatıcı ortaklığı, Film bilgisi yokluğu, gerçek sezon seçimi, ilk/son sezon okları, tek sezonda okların gizlenmesi |
| Yatay liste | 120 bölüm için 6 container; recycling ve CanContentScroll korunur; oynayan 80. bölüme kaydırma, başka sezonda yanlış oynuyor işaretinin olmaması |
| Seçim ve klavye | Tek orijinal SelectionChanged; sezon gezinirken video/oturum değişmez. Bölüm ←→ yalnız odak; Enter aynı mevcut Button.Click yolunu bir kez kullanır. Oynatıcıda seek/ses/Space/M/PgUp/PgDn korunur |
| Bölüm sınırları | Oynatıcı önceki/sonraki IsEnabled, panelin aynı mevcut düğmelerine bağlı; ilk/son bölüm kontrolü geçer. Panelin tekrar eden düğmeleri görünümden çıkarıldı |
| Devam ve bitiş | Mevcut progress/izlendi kaydı ve devam/kurtarma testleri geçer. Doğal sonda yeni otomatik oynatma yok. Tam ekranda son 30 saniyelik teklif ve tıklama ile sonraki bölüm aynıdır |
| Görsel uç durumlar | Uzun başlık, hata metninin okunması, boş kodda rozetin tamamen gizlenmesi, Unicode/belirsiz adlarda ham metin; `final-series-ready` ve `final-format-ready` |
| Ölçü ve odak | 1920×1080, 1366×768, 900×700 pencere; dar kategori çekmecesi, F8, resize sırasında gezinilen kart odağı korunur |
| Video | Gerçek yerel oynatma, aynı render HWND, GetWindowRect ile video yuvası hizası; tam ekran/mini dönüşünde kategori ve oynatma korunur |
| Filmler | `final-film-isolated`: mevcut kategoriler, arama, sayfalama, favori, oynatma ve odak regresyonu geçer |
| Mevcut UI | Experience160, Live, Home, Catalog, embedded1714, comfort1714 ve full smoke başarı JSON'ları `ui-suite.json` içinde |
| Sayfa karşılaştırması | Gerçek 8e7dbdb kaynak tabanı + aynı fixture/viewport: Filmler, Canlı TV ve Ana sayfa görünür geometri ve font eşit; öncesi/sonrası PNG'ler |
| Koruma | 36/36 korunan dosyanın SHA256'sı aynı. SeriesEpisodePanel.cs yalnız `partial` bildirimi dışında aynı; constructor, Loading/Error/Show/Filter ve tüm callback gövdeleri korunur |

İlk eski embedded otomatik gizleme smoke koşusu 3,4 saniyelik örnekleme penceresinde zamanlama hatası verdi. Korunan controller değiştirilmedi. Gerçek 1.7.15 tabanında aynı test ve 1.7.16'nın ayrı tekrarında başarı alındı (`baseline-embedded`, `retry-embedded`). Bu ilk başarısız sonuç gizlenmedi.

Filmler testinin son toplu tekrarında `filmFirstMoveOnlyReveals` anlık örneklemesi başarısız oldu; önceki koşu ve son ayrı `final-film-isolated` koşusu geçti. Controller ve eski test dosyası değişmedi. Bir tanı kaydında WPF Auto/NaN değeri JSON üretimini engelledi; yalnız yeni QA kaydı null ile düzeltildi. Eksik JSON olan bu denemeler başarı sayılmadı.

Bağımsız son incelemedeki Unicode sayı ayrıştırma ve resize odağı bulguları RED→GREEN ile düzeltildi; eksik kodda boş rozet de gerçek görünüm testiyle düzeltildi. İkinci inceleme turu veya ertelenen kritik/önemli bulgu yok. İnceleyici iş mantığını değiştirmedi; GUI doğrulamalarını ana çalışma yürüttü.

## Dosyalar ve ortak bileşenler

- `MainWindow.FilmAppearance.cs`: mevcut tek kategori/liste/oynatıcı katmanı Series için de kullanılır; Film bilgisi ve favori/sonraki Film sunumu yalnız Film'de. Aynı FilmVisualScope, geri alma ve mevcut bindirme kullanılır.
- `MainWindow.SeriesAppearance.cs` (yeni): yalnız Series izleme koşulu, dizi adı/kod/alt başlık ve panel görsel bağlantısı.
- `SeriesEpisodePanel.Appearance.cs` (yeni): aynı Seasons/Episodes kontrollerinin geri alınabilir yeni yerleşimi. Sezon hapları aynı ItemsSource/SelectedItem'a bağlı; oklar aynı SelectedIndex'i değiştirir. Yeni sezon durumu, sorgu veya zamanlayıcı yok.
- `SeriesPanels.xaml` (yeni): yalnız SeriesSeasonPill, SeriesEpisodeRow, SeriesEpisodeButton ve SeriesEpisodeCard. LibraryPanels/LiveTheme kaynakları kullanılır; Film kategori/liste/oynatıcı stillerinin ikinci tanımı yok.
- `SeriesDisplayConverters.cs` (yeni): salt görüntü; dizi adı, SxxExx, sezon/bölüm açıklaması, güvenilir bölüm adı ve mevcut progress gösterimi. Bilinmeyen, çelişen veya Unicode sayılı ad ham kalır. Ham başlık ToolTip'te korunur.
- `MainWindow.xaml`: tek LibraryMovieTemplate içinde Series yıl görünürlüğü; yeni liste şablonu yok.
- `LibraryCategoryPane.cs`, `LibraryDisplayConverters.cs`, `LibraryPosterPresentation.cs`: ortak kategori erişilebilir adı, gerçek oynayan dizi kimliği ve Series yedek ikonu. Aynı afiş kaynağı/yükleyici/cache kullanılır.
- `MainWindow.LiveAppearance.cs`: ortak katman koşulu ve SeriesCount; devre dışı önceki düğmesinde Tab köprüsü mevcut oynat düğmesine ulaşır.
- `SeriesEpisodePanel.cs`: yalnız partial. `SmokeTest.cs` + yeni `SeriesWatching1716Smoke.cs`: yalnız açıkça istenen izole QA.
- `Directory.Build.props`: 1.7.16. Referans `design/wx-player-dizi-izleme.html` ve sürüm notları/geçmiş/dizin/test raporu.

Yeniden kullanılanlar: LibraryCategoryPane, LibraryMovieTemplate/Row, LibraryPosterPresentation, LibraryText/Playing, LibraryTag/InfoButton/BackButton/VolumeReveal; LiveBrush/LiveButton/LiveMenuAction/LiveFocus/LiveSlider/LiveProgress/LivePlayButton/LiveScrollBar; CatalogPillBorder. Sezon haplarının CSS 999 köşesi mevcut CatalogPillBorder ile sınırlandırılır; sabit 4,8 DIP başlık rozeti Border kullanır.

## Korunan mantık

MainWindow.xaml.cs, MainWindow.Series.cs, MainWindow.Progress.cs, MainWindow.Catalog.cs, EpisodeWindow, FullscreenV2/Placement, MiniPlayer, EmbeddedControlsAutoHide, ComfortPresentation, NativeVideoHost, PlaybackEngine, LiveBuffer, provider/EPG/veritabanı/modeller, afiş yükleme/cache ve Discord yapılandırması değişmedi. README değişmedi. Kaynakların açılışı, bölüm yüklenmesi, devam konumu ve son 30 saniyelik teklif aynı akıştadır.

Yatay düzen sanallaştırma/seçim/scroll testlerini geçtiğinden dikey yedeğe dönülmedi. Sezon/dizi değişiminde gerçek oynayan kart görünür alana alınır; kullanıcı başka kartlara odaklandığında resize/F8 bu gezinmeyi zorla oynayan karta döndürmez.

## Eklenmeyen öğeler ve referans farkları

- Mockup Önizleme/Durum paneli, sahte içerik/afiş/kaynak, kategori sayıları, veride bulunmayan sezon toplamları, özet/Film bilgisi, yeni otomatik sonraki bölüm yok. Onay bekleyen yeni metin yok.
- Eski bölüm paneli Önceki/Sonraki düğmeleri görünümden çıkarıldı; aynı komutlar ve pasifleşme oynatıcıda korunur. Sezon okları yalnız sezon seçimini değiştirir.
- Gerçek Türkçe süre biçimi ve masaüstü başlık çubuğu korunur. Blur/ağır gölge eklenmez; mevcut yarı saydam koyu fırçalar kullanılır. Mevcut şeffaf kontrol bindirmesi dışında yeni pencere yok.
- Yatay kaydırma çizgisi ortak LiveScrollBar'ın 6 DIP görselidir. WPF, sistem ScrollViewer şablonunda 17 DIP giriş alanı ayırır; bu standart alan korunur. Denenen yerel yükseklik override'ı kaldırılmıştır; yeni scrollbar şablonu kopyalanmadı.
- Dar pencerede sezon grubu ikinci satıra iner; kategori çekmecesi aynı Film bileşenidir. Gerçek veri sayısına göre boşluk/taşma, sahte HTML verisinden farklı olabilir.

## Gerçek ortam sınırları

- Gerçek pencere DPI'si 96 (%100). %125/%150 fiziksel monitör DPI'si doğrulanmadı. F8/Rahat mod bunların yerine sayılmadı.
- Kaydedilen PNG'ler WPF katmanıdır: HWND videosu bitmap'e dahil değildir ve siyah görünebilir. Video gerçekten oynadı; HWND sürekliliği ve fiziksel sınırları ayrıca ölçüldü. Birleşik native video + bindirme masaüstü ekran görüntüsü doğrulanamadı.
- Yerel HTML tarayıcı güvenlik politikası nedeniyle açılamadı; alternatif yüzeyle aşılmadı. CSS kaynak değerleri incelendi, WPF PNG'leri karşılaştırıldı; tarayıcıyla piksel piksel eşitlik doğrulanmış sayılmaz.
- Gerçek özel IPTV hesabı ve sağlayıcı ağı kullanılmadı. Test fixture'ı yerel video ile canlı/film/bölüm türlerini çalıştırdı; sağlayıcı mantığı kapsam dışıdır.

Temiz kaynak ZIP build'i, EXE/portable açılışları ve SHA256 sonuçları teslim manifestinde bulunur. Commit/push yapılmadı.

Önerilen commit: `feat(ui): reuse movie watching layout for series (1.7.16)`.