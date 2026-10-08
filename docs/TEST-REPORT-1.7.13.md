# WXPlayer 1.7.13 doğrulama

5 Ekim 2026 · Windows x64 · .NET SDK 10.0.401 · WPF / .NET 10

| Kontrol | Sonuç |
| --- | --- |
| Restore ve Release build | Başarılı; 0 hata, 0 uyarı |
| Regresyon testleri | 77/77 |
| Yeni 1.7.13 hedefli smoke | 46/46 (success dahil) |
| Ayrı süreçte son kaynak ve Ana sayfa | 3/3 |
| Kaldırılmış son kaynak, yedek seçim ve Ana sayfa | 3/3 |
| Ana sayfa smoke | 24/24 |
| Filmler/Diziler smoke | 23/23 |
| Canlı TV / ortak oynatıcı / EPG / fullscreen / PiP smoke | 33/33 |
| İstatistikler / ses-altyazı / mini oynatıcı smoke | 61/61 |
| Ayarlar / sidebar / kayıt-temizlik smoke | 37/37 |
| Tam smoke | success=true, 181/181 |

Yeni hedefli testler gerçek WPF kontrolleri ve izole SQLite verisi kullanır: iki kaynak, film ve diziler için 18 kategori, son kategorinin yüklenmesi, arama caret koordinatı, sıfır sonuçta sabit hero, yenileme sonrası güncel ilerleme, yenilemenin aramayla kesildiği async yarış, iptal edilen bağlamın görünürlüğü, ortak oynatıcı görünümü, tek kontrol sahibi, fullscreen/Home dönüşü ve bölüm paneline Tab erişimi. Kaynak penceresinde üç türün modal kabulü, mevcut kaynak klonu/kimliği, ipuçları, gizlenen bütün bölümler, alanların korunması, PasswordBox gizliliği, mevcut hata metinleri ve iptal doğrulandı. Eksik kaynak testleri yalnız kopyalanmış QA verisinde yapıldı.

Hataları tekrar üreten kırmızı testler önce kaydedildi; düzeltmelerden sonra aynı kontroller geçti. Bağımsız salt okunur incelemenin belirlediği gizli kalan hero, veri yenilemesi sonrası eski hero ve bölüm paneli odak sorunları düzeltildi. Son inceleme bu alanlarda başka bulgu bildirmedi.

Mevcut tam smoke testinin eski Inter beklentisi yeni ortak oynatıcıdaki Manrope'a göre güncellendi. Eski dikey EPG kontrolü yeni yatay yerleşime, 190 DIP satır içindeki 16 DIP boşluk hesaba katılarak uyarlandı. Native HWND, oynatma, kayıt, duraklatma, seek, fullscreen, PiP, EPG, favoriler ve Discord kontrolleri korundu. İlk tam çalıştırmada Home afişlerinin hazır olmasını bekleyen 6 saniyelik kontrol zaman aşımına uğradı; teşhis için yalnız testte durum dökümü eklendi. Aynı koşul ve süreyle sonraki tam çalıştırma 181/181 geçti; Home üretim kodu değiştirilmedi. Kısıtlı süreçte iki named-pipe testi zaman aşımına uğradı; gerçek Windows IPC erişimiyle 77/77 geçti, Discord koduna dokunulmadı.

Tam testte ekran yakalama işlemleri de başarılı döndü. Ana pencere HWND görüntüsü LibVLC/ayrı kontrol penceresini bütünüyle göstermez; WPF RenderTargetBitmap de yerel video katmanını yakalamaz. Görünür fullscreen görüntüsü yerel videoyu ve kontrolleri gösterir. Bu sınırlı ekran görüntüleri bütün cihazlarda GPU piksel doğrulaması yerine geçmez. Native yüzey boyutu/kimliği/geri dönüşü ayrıca test edildi.

Kaynak karşılaştırması: [PROTECTED-1713.json](PROTECTED-1713.json). Kontrol edilen 40 çekirdek, oynatıcı, native host, fullscreen, Discord, mevcut diğer pencere ve dağıtım dosyası SHA-256 olarak 1.7.12 ile aynı. App.xaml değişmedi. SourceWindow doğrulama/kaydet callback gövdesi birebir aynı; sağlanan logo PNG birebir aynı. Yeni paket eklenmedi; Discord Application ID korunur. Son başarılı QA dizinlerinde startup-error.log, smoke-unhandled.txt veya errors.log yoktur.

~~~powershell
dotnet restore WXPlayer.sln --ignore-failed-sources --disable-parallel -m:1 -nr:false -p:NuGetAudit=false
dotnet build WXPlayer.sln -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false
dotnet run --project tests/WXPlayer.Tests -c Release --no-build -- <izole-sonuc.json>
WXPlayer.exe --smoke --experience1713-layout-only --media <yerel-video> --data-dir <izole-QA>
WXPlayer.exe --smoke --experience1713-layout-only --verify-startup1713 --data-dir <izole-kopya>
WXPlayer.exe --smoke --experience1713-layout-only --verify-missing-source1713 --data-dir <izole-kopya>
WXPlayer.exe --smoke --media <yerel-video> --data-dir <izole-QA>
~~~

Görsel örnekler: [M3U](WXPlayer-1.7.13-source-m3u.png), [Xtream](WXPlayer-1.7.13-source-xtream.png), [Stalker](WXPlayer-1.7.13-source-stalker.png), [Ana sayfa](WXPlayer-1.7.13-ana-sayfa.png). EXE/portable teslim doğrulaması outputs/WXPlayer-1.7.13-package-verification.json ve teslim test raporunda ayrıca kaydedilir.
