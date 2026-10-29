# WXPlayer 1.7.14 doğrulama

20 Ekim 2026'da başlatıldı · Windows x64 · .NET SDK 10.0.401 · WPF / .NET 10

Dal: `rahat-mod-ve-duzeltmeler`; başlangıç: `f6609e0`. Çalışma deposu `E:\WX Player`.
Test verileri ve günlükler: `E:\Codex\2026-09-05\bu-klas-r-i-indekileri-analiz\work\qa\v1714-20261020`.
Kullanıcının gerçek kütüphanesi/ayarları yerine ayrı `--data-dir` klasörleri kullanıldı.

| Aşama | Doğrulama | Sonuç |
| --- | --- | --- |
| 1 — Rozetler | Önce hatayı üreten geometri testi; sonra İstatistik/Ses-altyazı smoke, Release build ve regresyonlar | Geçti; build 0 hata / 0 uyarı, 77/77 regresyon |
| 2 — Normal oynatıcı | Gerçek 3 sn süre, ilk hareket, hover, menü, duraklatma, klavye, Tab köprüsü, slider, mini, gezinme ve kapanış | Geçti (`stage2-final-green`); tam ekran 3,5 sn |
| 3 — HWND kapısı | Önce bağımsız dış kök prototipi: Movie/Live/Episode, iki pencere boyutu, kapalı → 1.5 → kapalı | Geçti (`hwnd-outer-prototype`); aynı HWND, doğru piksel sınırları, oynatma devam ediyor |
| 3 — Görünüm | 1920×1080 ve 1366×768 pencere boyutları × kapalı/açık × 9 görünüm | 36 durum geçti, PNG'ler kaydedildi (`stage3-state-green/matrix-results.json`) |
| 3 — Etkileşim | Yerel video HWND'sine F8 mesajı, tekrar bitinin bastırılması, sonraki basış; açık ComboBox, ToolTip, ContextMenu | Geçti; aynı toplam görsel ölçek |
| 3 — Kalıcılık | Kaydet, ayrı süreçte yeniden aç, alanı bulunmayan eski settings.json, açık Ayarlar ile F8 tutarlılığı | Geçti (`stage3-restart.json`, `stage3-old-settings`) |
| Son Release build | `final-package-build.log` | 0 hata / 0 uyarı |
| Son regresyonlar | `stage3-final-tests.json` | 78/78 |
| Mevcut yardımcı pencereler | `legacy-utility` | Geçti |
| Mevcut Canlı TV | `legacy-live-verified` | Geçti |
| Mevcut Ayarlar / SmokeShowShortcuts | `legacy-settings` | Geçti; mevcut sekiz ayar ve yeni tek anahtar, F8 listesi |
| Mevcut Ana sayfa / katalog / ortak oynatıcı | `legacy-home`, `legacy-catalog`, `legacy-experience-verified` | Geçti |
| Tam uygulama smoke | `legacy-full` | `success=true`; mevcut oynatma/seek/pause, fullscreen, PiP, EPG, favori, kayıt ve Discord kontrolleri geçti |

## Yaklaşım ve a–g riskleri

**A yaklaşımı:** Ana pencereye açılışta tek dış WPF kökü eklenir; çalışma sırasında yalnız `LayoutTransform` değişir. Diyalogların mevcut yüzeyi, ayrı normal/tam ekran kontrol kökleri ve popup içerikleri aynı sunum hizmetini kullanır. Mevcut rem hesabı ve PerMonitorV2 manifesti korunur. B yaklaşımıyla tek tek boyut/yerleşim değiştirilmedi.

| Risk | Uygulama ve kanıt |
| --- | --- |
| a — NativeVideoHost / airspace | Video yeniden ebeveynlenmez veya yeniden yüklenmez. GetWindowRect ile PointToScreen sınırları karşılaştırıldı; canlı, film ve bölümde aynı render HWND ve oynatma devamı doğrulandı. Prototip geçmeden yaygınlaştırma yapılmadı. |
| b — Popup | Ortak seçici şablonları, ToolTip ve ContextMenu mevcut üst ölçek dahil ölçülür; yalnız eksik oran uygulanır. WPF'nin taşıdığı ölçeğin iki kez uygulanması regresyonla bulundu ve düzeltildi. Açık listede F8 de test edildi. |
| c — Ayrı kontroller | Normal overlay ve tam ekran viewport'una mevcut rem ölçeği × ek Rahat mod çarpanı uygulanır. Otomatik gizleme süreleri ayrı kalır. |
| d — Afiş/logo | Yalnız decode genişliği etkin çarpan ve DPI ile büyütülür. İstenen genişlik değişmedikçe görsel yeniden istenmez. Kaynak keşfi, URL'ler ve ArtworkCache değiştirilmedi. |
| e — Diyaloglar | Normal ölçüler saklanır; aynı monitörün DPI'ye çevrilmiş çalışma alanının %90'ına sınırlandırılır; mevcut ScrollViewer'lar korunur. Kapalı modda normal ölçüler geri gelir. Sınıra dayanmış pencereyi küçültme hatası önce testte üretildi, sonra düzeltildi. |
| f — Mini | Kayda/konuma müdahale edilmedi; mevcut 520×400 boyutu ve kök ölçeği 1 olarak doğrulandı. |
| g — Küçük ekran | Mantıksal ana içerik için 1100 genişlik ve 650 yükseklik alt sınırları çarpanı azaltır. 1920×1080 pencerede ek çarpan 1.5; 1366×768 pencerede yaklaşık 1.122. Mevcut DPI/rem bunun dışında korunur. |

Tek ayarlanabilir çarpan: `App.xaml` → `ComfortModeScale`, **1.5**. Tek yeni ayar: varsayılan `false` olan `PlayerSettings.ComfortMode`; mevcut `App.SaveSettings` kullanılır. Yer: **Ayarlar → Oynatma → Görünüm**. Kısayol: **F8**, değiştirici tuşsuz; basılı tutma bir kez açıp kapatır.

## Koruma ve düzeltme kanıtı

- PlaybackEngine, LiveBuffer, ProviderClient, EpgService/XmlTvParser, LibraryStore dosyaları ve DiscordConfiguration olmak üzere 10 dosya başlangıç SHA-256 değerleriyle aynı (`protected-verified.json`). `Models.cs` içindeki tek değişiklik onaylanan bool alandır. README değişmedi.
- Yeni blur, ağır gölge, OpacityMask, BitmapCache veya şeffaf pencere eklenmedi. Mevcut native overlay'ler korunur. Metinler `Ideal` biçimlendirme kullanır.
- Mod kapalıyken Kaynak ve Ses/altyazı ekranlarının her iki pencere boyutundaki dört PNG'si özellik öncesi PNG'lerle SHA-256 düzeyinde aynıdır (`off-image-comparison.json`). Dinamik afiş/izleme/saat verileri ve yeni ayar satırı için bütün uygulamanın piksel eşitliği iddia edilmez.
- Salt okunur incelemede bulunan Tab ile gizli kontrollere giriş ve sınıra dayanmış diyalog boyutlandırması hataları yeni testlerde önce başarısız oldu; düzeltmelerden sonra geçti. Tab köprüsü kontrolleri önce açar ve klavye odağı varken korur.
- Eski Canlı TV testinin gece yarısına yakın EPG örnekleri ertesi güne taşıyıp yanlış zaman aşımı üretmesi yalnız test verisinde düzeltildi. Eski ortak oynatıcı testi, görünürlüğü ölçmeden önce yeni davranışa uygun fare hareketi gönderir. Üretim EPG/oynatma kodu değiştirilmedi.

## Gerçek ekranla doğrulanamayan koşullar

Ölçümler **%100 DPI'de WPF pencere boyutlarıdır**; fiziksel monitör çözünürlüğü değiştirilmedi. Tam ekran, mevcut monitörü kullanır. Fiziksel 1366×768 ekran, %125/%150 DPI, farklı DPI'li monitörler arasında taşıma, TV/yatak mesafesinde okunabilirlik ve gerçek uzaktan kumanda girişi elle doğrulanmadı.

WPF RenderTargetBitmap, LibVLC'nin native video yüzeyini yakalamaz; bu yüzden PNG'lerde siyah video alanı olabilir. Bu çalışmanın dört isteğe bağlı native masaüstü yakalama kontrolü ön plan koşulunu sağlayamadı (`nativeDesktopCapture=false`). Oynatma, görünür HWND ve koordinat eşitliği doğrulandı; GPU/video pikseli veya fiziksel ekranda bulanıklık olmadığına dair tam görsel onay verilmez. Routed WPF input ve HWND'ye gönderilen klavye mesajları kullanıldı; masaüstü faresi taşınmadı.

LibVLC D3D11 test çıktısında `SetThumbNailClip failed: 0x800706f4` satırları görüldü; derleme uyarısı değildir. İlgili oturumlarda kare çözme ve oynatma/HWND kontrolleri geçti. Korunan motor dosyası değiştirilmedi.

## Ayrı commit önerileri

1. `fix: correct statistics and track badge shapes`
2. `feat: auto-hide embedded player controls after idle`
3. `feat: add comfort mode with persisted F8 toggle`

Commit/push yapılmadı; değişiklikler onaylanan ayrı dalın çalışma ağacındadır. Günlükler, karşılaştırmalar ve paket doğrulaması teslim klasöründe tutulur.
