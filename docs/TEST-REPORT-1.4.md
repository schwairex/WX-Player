# WX Player 1.4 — Test raporu

7 Eylül 2026 · Windows x64 · .NET 10 / WPF / LibVLC.

## 1.4 özellikleri

Gerçek uygulama 1440 × 920 ve 900 × 650 pencere düzenlerinde açıldı. Arama/kategori/listenin birlikte çalışması, küçük pencerede Ayarlar ve rehberin görünürlüğü kontrol edildi. Ses kaydırıcısı odaklandığında arka planın saydam kaldığı ve sesin %60 değerine uygulandığı doğrulandı.

Tam ekranda kategoriden Kültür seçildi, yatay içerik listesinden WX Kültür HD oynatıldı. İzlenen kanal ve EPG buna göre değişti. Panelin hareketsizlikte gizlenmesi, fare hareketiyle geri gelmesi ve normal pencereye dönüş kontrolleri geçti. GPU yüzeyi WPF önizlemelerinde görünmez; gerçek video çözülen kareler ve yerel pencere bağlantısıyla doğrulandı.

İki kanal aynı yerel HTTP logo adresini kullandı. Görsel iki satırda da yüklendi; sunucu yalnız bir logo indirmesi gördü. Logo adresi değişen sanallaştırılmış öğelerde eski yanıtı uygulamama koruması vardır. Küçük pencere, ses/altyazı, kaynak ekleme, istatistikler ve tam ekran seçim paneli görüntüleri incelendi.

## Canlı tampon

Yerel HTTP sunucusu kesintisiz, zaman damgaları ilerleyen MPEG-TS test yayını verdi. Uygulama tek üst bağlantıyla yerel HLS parçaları oluşturdu. Uzun testte görünür tampon 60 saniyeye ulaştı; canlı yerel oynatımda 2.769 kare çözüldü. 60 saniye geri sarma isteğinde tekrar görüntüsü çözüldü; ardından canlıya dönüldü. Kanal/oynatma durdurulunca geçici tampon klasörü temizlendi.

TS anahtar kareleri ve segment sınırları nedeniyle seek noktası tam kare hassasiyetinde değildir. Tampon açılıştan itibaren birikir. Gerçek IPTV hesapları, tüm sağlayıcı codec çeşitleri, DRM veya tüm GPU/monitör birleşimleri test edilmedi. Desteklenmeyen kaynakta geri sarma garantisi verilmez; doğrudan oynatma yedeği vardır.

## Teknik kaynak

Yerel segment üretimi LibVLC'nin [livehttp modülünü](https://github.com/videolan/vlc/blob/3.0.x/modules/access_output/livehttp.c) kullanır; dosya döndürme, 60 saniyelik seçim aralığı, tekrar dosyası ve canlıya dönme yönetimi WX Player kodundadır. Önceki PVR / sağlayıcı Catch-Up altyapısı korunur.
## Dağıtım doğrulaması

37/37 Core testi geçti. Bağımsız EXE çıkarılıp gerçek medya ile sınandı: 100.500 içerik 1.682 ms içinde yüklendi; arayüz zamanlayıcısındaki en uzun aralık 37,6 ms idi. 60 saniye geri sarma isteği sonrası ölçülen gecikme yaklaşık 60,6 saniye; tekrar oynatımında 178 kare çözüldü. Ses, altyazı, PVR, EPG ve tam ekran dönüşü kontrolleri geçti. Bu ölçümler test bilgisayarına aittir; sağlayıcı ağ gecikmesini kapsamaz.

Son 1.4.0 EXE'de kontrol pencereleri, klavye etiketleri ve oynatma testleri tekrar geçti. Otomatik güncelleme testi ayrı bir 1.5.0 deneme paketiyle tamamlandı: doğrulanan indirme, önceki sürecin kapanmasını bekleme, yeni sürümü açma, eski kısayolun yönlenmesi ve kütüphanenin korunması geçti. Gerçek GitHub Release yayımlanmadı.
