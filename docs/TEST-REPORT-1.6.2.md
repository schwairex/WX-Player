# WX Player 1.6.2 — Doğrulama

Windows x64 / .NET 10 / WPF / LibVLCSharp. Yeni NuGet paketi eklenmedi.

## Derleme ve regresyon

- `dotnet restore`: başarılı.
- `dotnet build WXPlayer.sln -c Release`: 0 hata, 0 uyarı.
- Core regresyonları: 67/67 geçti. Önceki 59 test ile 8 yeni Discord testi birlikte çalıştırıldı.
- Discord testleri: gerçek Playing öncesi paylaşmama; pause/resume/seek zamanları; normal süre akışında aynı activity; EPG sınırları ve eski nesil reddi; dizi/sezon/bölüm; gizli veri/görsel filtresi; ayar geçişi/kalıcılık; offline/reconnect; geciken bağlantıda yeni kanal ve OFF; dispose sırasında iptal; görselsiz fallback; gerçek yerel named-pipe framing/handshake/READY/ping/pong/nonce/ERROR/null-clear/disconnect.
- Named-pipe testleri sandbox dışında normal Windows kullanıcı ortamında geçti. Sandbox bağlantıyı engellediğinde iki test bekleme süresini dolduruyordu; kod değişmeden normal ortamda geçti.

## Gerçek LibVLC / Discord durum entegrasyonu

`--smoke --discord-presence --media <yerel-video> --data-dir <ayrı-klasör>` sonucunda 9/9 kontrol geçti ve `success: true`:

1. Film adı ve countdown.
2. Pause durumunda zamanın kaldırılması.
3. Resume ve seek sonrasında doğru kalan süre.
4. Dizi adı, S02E03 ve bölüm başlığı.
5. Gerçek yerel EPG programı, kanal ve zamanlar.
6. Hızlı kanal değişiminde son kanalın kazanması.
7. Stop sonrasında temizleme.
8. Media end sonrasında temizleme.
9. Gerçek VLC EncounteredError olayı sonrasında temizleme.

Eksik medya dosyası son kontrolün bilinçli fixture'ıdır; VLC'nin bu dosyaya ilişkin hata çıktısı beklenir. WPF unhandled/startup hata dosyası oluşmadı.

## İnceleme ve sınırlar

- Kaynak farkı önceki 1.6.1 kaynak ZIP'iyle karşılaştırıldı. PlaybackEngine, FullscreenPlacement, provider, EPG servisi ve veritabanı implementasyonları değişmedi. JSON ayarına yalnız DiscordRichPresence alanı eklendi.
- Tek worker/tek IPC yazarı; immutable son activity, içerik nesli ve playVersion kontrolü; geç gelen replay hatasının yeni activity'yi temizlememesi; iptal edilen EPG işinin kapanışta beklenmesi; native olay aboneliklerinin kaldırılması gözden geçirildi.
- Eski tam medya smoke testindeki 24 kart beklentisinin değişmemiş 1.6.1 paketinde de başarısız olduğu doğrulandı: 1.6.1 rafları artık 48'e kadar çıkıyor, fixture 24 film + 12 dizi içeriyor. Test 36 içerik/tür/kaynak kontrolüne düzeltildi. Odak ve büyütme kontrolleri pencere durumunun hazır olmasını bekliyor.
- Bu Windows oturumunda geniş `--smoke --media` akışının fullscreen auto-hide/pencere yerleşimi kontrolleri tutarlı geçmedi. Native ekran yakalama da aktif pencere koşulu nedeniyle başarısız olabildi. Bu akışın tamamı geçmiş sayılmaz; fullscreen üretim kodu değiştirilmedi. Etkileşimsiz bir masaüstü oturumunda manuel tam ekran kontrolü önerilir.
- Gerçek Discord Application ID kaynağa girildikten sonra Windows x64 EXE ve portable paket yeniden üretildi. Discord hesabının profil görünümü bu otomatik testlerle doğrulanmadı. Taşıma protokolü gerçek yerel test pipe'ıyla; uygulama durumları gerçek LibVLC ile doğrulandı. Yayın öncesi Discord masaüstünde görünümü manuel kontrol edin.
- Gerçek IPTV hesabı, internet EPG sağlayıcısı, uzun süreli timeshift ve GitHub yayınlama bu turda denenmedi. Mevcut provider/EPG/auto-updater regresyonları geçti.

Paket testlerinin ham sonuçları ve SHA-256 manifesti teslim klasöründeki sürüme özel dosyalardadır.
