# WX Player 1.7.9 — doğrulama

2 Ekim 2026 · .NET SDK 10.0.401 · Windows x64 · Release

| Kontrol | Sonuç |
| --- | --- |
| Hatanın yeniden üretilmesi | Özel M3U dosyasında 2.506 içerikten sonra, 5.015. satırda logo çözümlemesi `UriFormatException` oluşturdu. Dosya UTF-8 olarak geçerli; boyutu 30.305.829 bayt. |
| Düzeltme öncesi regresyon | Mevcut 73 test geçti; yeni yerel ve uzak M3U testleri aynı URI hatasıyla başarısız oldu (73/75). |
| Release çözüm derlemesi | Başarılı; 0 hata, 0 uyarı. |
| Düzeltme sonrası regresyon | 76/76 geçti. Geçersiz logo, göreli logo, boş logo, Unicode, karma satır sonları, sınıflandırma, kaynak yeniden yükleme ve geçersiz yayın adresi sınandı. |
| Gerçek M3U / Core | 94.222 kayıt ayrıştırıldı ve mevcut ProviderClient → LibraryStore akışıyla işlendi; kaynak yeniden açıldı ve sayfalı sorgu çalıştı. |
| Gerçek M3U / WPF | Özgün SourceWindow doğrulaması ve Bağlan ve yükle handler üzerinden içe aktarıldı; seçili kaynak kaydedildi, başarı metni ve Canlı TV listesi doğrulandı. 3.998 kanal, 10.615 film, 3.204 dizi başlığı. 38.105 ms; işlem boyunca 202 UI timer tick. success:true, tüm kontroller true; errors.log yok. Otomatik dış görsel araması yalnız bu izole QA çalışmasında devre dışı bırakıldı; kaynak görselleri normal yükleme yolunda kaldı. |
| Ana sayfa görünümü | Home178 smoke: success:true; tüm kontroller true. |
| Filmler/Diziler kataloğu | Catalog177 smoke: success:true; tüm kontroller true. |
| Tam oynatma smoke | success:true; yerel video, pause/resume/seek, fullscreen, EPG, mini oynatıcı, kayıt ve mevcut uygulama akışları geçti. Yalnız GPU/ön plan screenshot işaretleri playingWindowCaptured ve fullscreenWindowCaptured false; bunlar başarılı video ekran görüntüsü olarak raporlanmaz. errors.log, startup-error.log ve smoke-unhandled.txt yok. |

Çalıştırılan komutlar:

```powershell
dotnet restore WXPlayer.sln --ignore-failed-sources --disable-parallel -m:1 -nr:false -p:NuGetAudit=false
dotnet build WXPlayer.sln -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false
dotnet run --project tests/WXPlayer.Tests -c Release --no-build -- <sonuc-json>
WXPlayer.exe --smoke --playlist-import <yerel-m3u> --data-dir <izole-klasor>
WXPlayer.exe --smoke --home178-layout-only --data-dir <izole-klasor>
WXPlayer.exe --smoke --catalog-layout-only --data-dir <izole-klasor>
WXPlayer.exe --smoke --media <yerel-video> --data-dir <izole-klasor>
```

İçe aktarma testleri gerçek dosyayı yalnız okur. QA veritabanları ve görsel önbellekleri work/qa klasörlerinde kalır; source/portable paketlerine veya raporlara özel yayın URL, kullanıcı adı/şifre, M3U içeriği ve kullanıcı veritabanı eklenmez. Regresyonlar yalnız sentetik adresler kullanır. Test sonucu, IPTV sağlayıcısındaki tüm yayınların ağ üzerinden oynadığını doğrulamaz.

Kaynak karşılaştırması: normal uygulama akışında yalnız PlaylistParser.Resolve değişti. SourceWindow, ProviderClient, LibraryStore, oynatma, fullscreen, EPG, Discord, Home/katalog ve görsel önbellek dosyaları 1.7.8 ile aynı. Sürüm metadatası, isteğe bağlı QA kodu ve dokümantasyon ayrıca güncellendi. Bağımsız kod incelemesinde engelleyici bulgu yok.
