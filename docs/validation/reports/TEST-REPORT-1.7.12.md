# WXPlayer 1.7.12 doğrulama

4 Ekim 2026 · Windows x64 · .NET SDK 10.0.401 · WPF / .NET 10

| Kontrol | Sonuç |
| --- | --- |
| Restore ve Release build | Başarılı; 0 hata, 0 uyarı |
| Mevcut regresyon testleri | 76/76 |
| İstatistikler / ses-altyazı / mini oynatıcı smoke | 61/61 (success dahil) |
| Mevcut Ayarlar / sidebar / gerçek kayıt-temizlik smoke | 37/37 (success dahil) |
| Mevcut Canlı TV / EPG / fullscreen / mini akışı | 32/32 (success dahil) |
| Tam smoke | success=true, 178/181 |

Tam smoke'da yalnız playingWindowCaptured, fullscreenWindowCaptured ve fullscreenVisibleScreenshot false; önceki 1.7.11 sürümünün aynı yerel GPU ekran yakalama sınırlamalarıdır. WPF RenderTargetBitmap görüntüleri görsel katmanı gösterir; LibVLC'nin HWND video içeriğini yakalamaz. Yerel videonun gerçek piksel ekran görüntüsü doğrulandığı iddia edilmez. Native HWND kimliği, parent, görünürlük ve ana pencereye dönüş ayrıca doğrulanır.

Yeni hedefli test: gerçek yerel video, ses kapatma/geri seçme, harici SRT ekleme ve altyazı kapatma; boş medya/disabled seçiciler; kaynak adresinin Text/ToolTip/Automation'da gizliliği; timer başlama/durma; sabit istatistik özeti/kaydırma ve yeniden çizim; birimler/formatsal varsayılanlar; seçili ses rozeti; Track dil dönüştürücüsünün bilinen/ham fallback'leri; seçili ComboBox ham araç ipucu; popup ve Esc; mini oynatma/duraklatma/ses geri çağrıları; 22 px konum, WindowChrome, minimum ve varsayılan boyutlar; canlı/süresiz durum; aynı HWND ile dönüş/kapat.

Kaynak karşılaştırması: [PROTECTED-1712.json](../manifests/PROTECTED-1712.json). Kontrol edilen 29 çekirdek/oynatma/yerel video/tam ekran/mini taşıma/Ayarlar/ortak tema dosyasında SHA-256 farkı yok. TracksWindow.RefreshTracks ve Browse gövdeleri, StatisticsWindow.SafeAddress önceki kaynakla birebir aynı. Ses/altyazı SelectionChanged mesajları ve motor çağrıları korunur. Hiçbir yeni paket, ayar veya iş komutu eklenmedi.

Açık ComboBox Esc testi önce başarısız oldu: PremiumWindow'ın mevcut genel Esc kapatma işleyicisi liste kapanmadan pencereyi kapatıyordu. Yalnız TracksWindow'da sınıf düzeyinde açık menü Esc işlemi eklendi; kapalı menüde eski davranış korunur. Seçim/yeni değer zamanlaması değiştirilmedi.

Değişmemiş Ayarlar testinin sabit 200 ms beklemesi bir çalıştırmada erken kontrol yaptı. Hata sonrası salt okunur SQLite incelemesinde favorilerin temizlendiği ve kaynağın korunduğu görüldü; taze veriyle eski test de 37/37 geçti. Yalnız testte, modal kapanış sonrası orijinal DB/UI sonuçlarını 8 saniye sınırıyla bekleme eklendi. Üretim Ayarlar/async callback kodu değişmedi; aynı doğrulamalar korunur.

Bağımsız salt okunur kod incelemesinde bulunan seçili öğe araç ipucu eksikliği düzeltildi ve gerçek ComboBox üzerinde doğrulandı. İkinci inceleme yeni önemli sorun bulmadı. Hedefli son QA'da yeni XAML/kaynak ayrıştırma, başlangıç veya unhandled hatası yoktur.

~~~powershell
dotnet restore WXPlayer.sln --ignore-failed-sources --disable-parallel -m:1 -nr:false -p:NuGetAudit=false
dotnet build WXPlayer.sln -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false
dotnet run --project tests/WXPlayer.Tests -c Release --no-build -- <izole-sonuc.json>
WXPlayer.exe --smoke --utility1712-layout-only --media <yerel-video> --data-dir <izole-QA>
WXPlayer.exe --smoke --settings1711-layout-only --data-dir <izole-QA>
WXPlayer.exe --smoke --live1710-layout-only --media <yerel-video> --data-dir <izole-QA>
WXPlayer.exe --smoke --media <yerel-video> --data-dir <izole-QA>
~~~

Görsel örnekler: WXPlayer-1.7.12-statistics.png, WXPlayer-1.7.12-statistics-tracks.png, WXPlayer-1.7.12-tracks.png, WXPlayer-1.7.12-mini-vod.png ve WXPlayer-1.7.12-mini-live.png.

Paket doğrulaması: outputs içindeki gerçek tek EXE'de 61/61; ZIP'ten yeni dizine açılan portable sürümde 61/61 pencere smoke, 37/37 Ayarlar ve success=true 178/181 tam smoke. Aynı üç GPU yakalama işareti dışında false yok. Başlangıç, unhandled ve launcher hata dosyası üretilmedi.
