# WX Player

**Kendi kaynağınız. Kendi kütüphaneniz.** Windows için C# / WPF ile geliştirilmiş yerel IPTV ve medya oynatıcısı. Koyu arayüz, Fluent tasarımından esinlenen kontroller, Direct3D video çıkışı ve SQLite tabanlı yerel veri altyapısı.

![WX Player](docs/WX-Player-preview.png)

## Çalıştırma

Windows 10/11 **x64** içindir. Windows 11 önerilir. Dağıtım .NET 10 çalışma zamanını ve LibVLC'yi içerir; ayrıca VLC veya .NET 10 kurmanız gerekmez. Tek dosya başlatıcısı Windows'un .NET Framework 4.x bileşenini kullanır.

* **WXPlayer.exe**: Tek dosyadır. İlk açılışta bileşenleri `%LOCALAPPDATA%\WXPlayer\application\1.0.0-<paket özeti>` konumuna çıkarır, sonraki açılışlarda bu kopyayı kullanır. Yönetici yetkisi veya sistem kurulumu istemez. İlk açılış için yaklaşık 500 MB boş alan ayırın.
* **WXPlayer-win-x64.zip**: Taşınabilir dağıtım. ZIP'in **tamamını** bir klasöre çıkarın ve içindeki `WXPlayer.exe` dosyasını çalıştırın. İçindeki EXE'yi tek başına başka klasöre taşımayın.
* EXE henüz ticari kod imzalama sertifikasıyla imzalanmamıştır. Paket bütünlüğü `SHA256SUMS.txt` ile doğrulanabilir.

## İlk kullanım

1. **Kaynak ekle** ile bağlantı türünü seçin.
2. M3U/M3U8/TXT dosyası veya URL girin; Xtream için sunucu, kullanıcı adı ve şifreyi yazın. `get.php?username=...&password=...` bağlantısını yapıştırırsanız hesap bilgileri ayrıştırılır. Stalker için sağlayıcının portal uç noktasını ve hesabınıza tanımladığı MAC adresini girin.
3. Kaydet ve yükle. İlerleme alt çubukta görünür; **İptal** mevcut kütüphaneyi korur. Bir kaynağın yüklemesi başarısız olursa önceki veriler silinmez.
4. Bir içeriğe tıklayın. Xtream dizilerinde bölüm seçicisi açılır. Yıldız favoriyi değiştirir. Filtreler, arama ve sayfa düğmeleri büyük listelerde de kullanılabilir.
5. EPG için kaynağa XMLTV URL/dosya yolunu ekleyin ve yayın akışındaki **↻** düğmesini kullanın. Xtream ve M3U başlığı rehber adresini otomatik sağlayabilir. Xtream kısa rehberi, yerel program bulunmadığında istenir.

Uygulama abonelik, ücretli kanal, hesap veya içerik sunmaz. Örnek kütüphane düğmesi açık lisanslı Blender filmlerinin bağlantılarını yükler; bu bağlantıların kullanılabilirliği uzaktaki sunucuya bağlıdır.

## Özellikler ve kapsam

| Özellik | Bu sürümdeki uygulama |
|---|---|
| M3U / M3U8 / TXT | UTF-8/BOM, EXTINF, grup, tvg-id, rehber adresi, göreli URL, kullanıcı aracısı ve referrer; M3U8 medya/master manifestini tek yayın olarak açma |
| Xtream Codes | Hesap doğrulama, canlı TV / VOD / dizi kategorileri, isteğe bağlı bölüm yükleme, kısa EPG, XMLTV ve timeshift URL üretimi |
| Stalker | MAG uyumlu handshake, profil, tür/kategori, sayfalı katalog, create_link; temel bölüm listeleri. Sağlayıcının cihaz ve oturum varyantlarına göre uyarlama gerekebilir |
| Büyük kütüphane | Akış halinde ayrıştırma, arka plan işi, SQLite WAL, atomik işlem, iptal, 150 satırlık sayfalar ve WPF satır sanallaştırması |
| Akıllı arama | 250 ms gecikmeli arama; Türkçe karakterleri sadeleştirme; kategori, tür, favori ve geçmiş filtreleri; önceki sorguyu iptal etme |
| Yerel önbellek | Kalıcı katalog; açılışta yeniden indirme gerektirmez. Elle yenileme. Ağ tamponu ayarlanabilir; tekrarlayan tamponlamada sonraki yayının tamponu artar |
| Windows medya | LibVLC 3.0.23 / LibVLCSharp 3.10.1; Direct3D 11 veya 9; D3D11VA GPU çözme, yazılım çözme seçeneği |
| DirectShow | Kaynak menüsünden Windows kamera/yakalama aygıtını adıyla açma. Ana IPTV oynatma hattı LibVLC + Direct3D'dir; ayrı özel DirectShow filtre grafiği değildir |
| Ses / altyazı | Akıştaki parça seçimi, harici SRT/ASS/SSA/VTT/SUB ekleme, sessiz, ses seviyesi |
| PVR | Ayrı oynatıcıyla TS remux kaydı, kayıt sırasında kanal değiştirme, kayıt klasörünü açma, kapanışta dosyayı tamamlama |
| Catch-Up | XMLTV geçmiş programını çift tıklama; Xtream timeshift ve `{utc}`, `{utcend}`, `{duration}`, `${start}`, `${end}` M3U şablonları |
| EPG | Kanal/gün rehberi, şimdi/sıradaki/geçmiş, ilerleme çizgisi, XMLTV ve XMLTV.gz, program açıklaması araç ipucu |
| Arayüz | Kategorili ana sayfa, sayaçlar, canlı TV / film / dizi / favoriler, kompakt simge menüsü, tam ekran, koyu başlık çubuğu |

**Sağlayıcıya bağlı sınırlar:** Gerçek Xtream/Stalker aboneliği verilmediği için bu entegrasyonlar kontrollü API yanıtlarıyla test edildi. Portal sürümleri; MAC dışında seri numarası, device ID, ek kimlik doğrulama veya farklı dizi uç noktaları isteyebilir. Bu sürüm bunların hepsini kapsamaz. Stalker'a özel arşiv protokolü desteklenmez; Stalker kanallarında XMLTV rehberi kullanılabilir. Xtream Catch-Up URL'si UTC saatinden üretilir; farklı sunucu saat dilimi bekleyen sağlayıcılar için uyarlama gerekir. Catch-Up için sağlayıcı arşivi gerekir; canlı yayında ileri/geri sarma ancak akış seek destekliyorsa mümkündür.

PVR, sağlayıcınızda ikinci bir eşzamanlı bağlantı açar. Bu sürümde zamanlanmış kayıt, kapalı uygulamada kayıt, yerel timeshift halkası, DRM/Widevine/PlayReady ve abonelik yönetim sunucusu yoktur. Kayıt TS kabına yeniden paketlenir; uyumsuz codec/kaynaklar kaydedilemeyebilir. DirectShow aygıt keşfi otomatik değildir; Windows aygıtının tam adı girilir. Bu teslimat kendi istemci/veri altyapınızı sağlar, bir IPTV içerik sunucusu kurmaz.

## Kısayollar

| Girdi | İşlem |
|---|---|
| Space | Oynat / duraklat |
| ← / → | Desteklenen akışta 10 saniye geri / ileri |
| ↑ / ↓ | Ses +5 / −5 (yukarı artırır) |
| F / Esc | Tam ekranı aç-kapat / tam ekrandan çık |
| M | Sessiz |
| Page Up / Page Down | Önceki / sonraki içerik; sayfa sınırından devam eder |
| Ctrl+K | Aramaya odaklan |
| Video üzerinde tekerlek | Ses seviyesini değiştir |
| Liste üzerinde tekerlek | Listeyi kaydır |
| Videoya çift tık | Tam ekran |
| Geçmiş EPG programına çift tık | Destekleniyorsa tekrar izle |

Metin ve şifre alanlarına yazarken oynatıcı kısayolları devreye girmez.

## Veri ve mahremiyet

Veriler `%LOCALAPPDATA%\WXPlayer` altında kalır. Uygulama analitik veya telemetri servisine bağlanmaz. Ağ istekleri eklediğiniz kaynaklara ve seçtiğiniz yayınlara gider.

* Kaynak yapılandırması ve hesap şifreleri Windows DPAPI `CurrentUser` ile şifrelenir; başka kullanıcıya veya başka bilgisayara doğrudan taşınamaz.
* `library.db`: katalog, favoriler, son izlenenler, EPG. **Oynatma listesinden gelen yayın URL'leri ve başlıkları bu SQLite dosyasında düz metindir**; URL içine gömülü token/şifreler de buna dahildir. Xtream yayın URL'leri mümkün olduğunda oynatma anında hesap bilgilerinden üretilir.
* `settings.json`: hassas olmayan ayarlar. `errors.log`: yalnız hata türü ve zaman; kaynak URL'leri loglanmaz.
* Kaynak silme o kaynağın kataloğunu, favorilerini, geçmişini ve rehberini kaldırır. Kayıt dosyaları ayrıca kullanıcı tarafından yönetilir.
* Tam kaldırma: uygulamayı kapatın, uygulama klasörünü ve istenirse `%LOCALAPPDATA%\WXPlayer` verilerini silin. Kayıtlar varsayılan olarak Videolar/WX Player altındadır.

## Kaynaktan geliştirme

Gerekli: Windows x64, **.NET 10 SDK**, NuGet erişimi. WPF kaynakları normal Visual Studio veya başka C# editöründe açılabilir.

```powershell
dotnet build WXPlayer.sln -c Release
dotnet run --project src/WXPlayer.App -c Release
dotnet run --project tests/WXPlayer.Tests -c Release -- artifacts/test-results.json
./tools/build.ps1
```

`build.ps1` testleri çalıştırır, çalışma zamanı dahil taşınabilir ZIP ve tek dosya başlatıcı EXE üretir. Çıktı klasörü varsayılan `artifacts`; tekrar paketlerken yeni bir `-OutputPath` verin. Tek EXE üretiminde Windows .NET Framework 4 C# derleyicisi kullanılır; ana uygulama .NET 10'dur. Kod imzalama sertifikası bu depoya dahil değildir.

```text
src/WXPlayer.Core    modeller, ayrıştırma, sağlayıcılar, XMLTV, SQLite, DPAPI
src/WXPlayer.App     WPF, oynatıcı motoru, diyaloglar, klavye/fare, görsel test
tests/WXPlayer.Tests bağımsız ağsız doğrulama programı, 100.500 içerik testi
tools               EXE başlatıcısı ve yeniden üretilebilir paketleme betiği
samples             açık lisanslı örnek içerik bağlantıları
.github/workflows   Windows CI derleme ve paketleme
```

Başlangıç ve medya testleri izole veri klasörüyle çalıştırılabilir:

```powershell
./artifacts/WXPlayer-win-x64/WXPlayer.exe --smoke --stress --data-dir C:\Temp\WXPlayer-QA --media C:\Temp\test.mp4
```

Bu test gerçek WPF penceresini oluşturur, PNG önizlemeleri ve JSON sonuç dosyası yazar; yerel MP4 ile video, seek, pause, ses parçaları, harici altyazı ve PVR/yeniden oynatma işlemlerini dener. `--stress`, içe aktarma sırasında UI zamanlayıcısının çalıştığını ölçer. Medya testi dosyanın desteklenen ve seek edilebilir olmasını gerektirir. Başlatıcıyı izole denemek için `WXPLAYER_APP_ROOT` ortam değişkeni çıkarma klasörünü değiştirebilir.

## GitHub'a yükleme

Kaynak ZIP'i çıkarın; **içindeki proje dosyalarını** yeni GitHub deponuza yükleyin. ZIP'in kendisini kaynak ağacı yerine yüklemeyin. EXE/taşınabilir ZIP, büyük dosya olduklarından deponun **Releases** bölümüne eklenebilir. Depoda özel kullanıcı bilgileri, gerçek abonelik URL'leri veya veritabanı bulunmaz. GitHub'a yükleme bu teslimat sırasında yapılmadı.

Uygulama kodu MIT lisanslıdır. Dahil edilen codec bileşenlerinin lisansları ayrıdır; `THIRD-PARTY-NOTICES.md` ve `licenses/` dosyalarını koruyun. Özel multimedya motoru sıfırdan yazılmış değildir; WX Player kendi uygulama ve veri katmanları üzerinde açık kaynak LibVLC'yi kullanır.
