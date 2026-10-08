# WX Player 1.5 — Test raporu

12 Eylül 2026 · Windows x64 · .NET 10 / WPF / LibVLC.

## Yeni arayüz

Gerçek uygulama açılarak ana sayfa, arama, geniş/dar sidebar ve küçük pencere görüntüleri incelendi. M3U test kaynağındaki dört içerik ana sayfada doğru türlerde gösterildi. “Sintel” araması tek içeriğe indi; sonuçsuz arama durumu doğrulandı. Sidebar tercihi dosyadan tekrar okunarak geniş ve dar durumların kaydedildiği doğrulandı. 940 piksel genişlikte açılan menü içerik üzerine yerleşti; kompakt menüye dönüş geçti. 900 × 650 pencerede Ayarlar ve EPG görünürlüğü korundu.

İkinci, 36 içerikli yerel katalogla kaynak izolasyonu ve sıra başına 12 kart sınırı kontrol edildi. Favori ve geçmiş öğeleri, HTTP üzerinden sağlayıcı kapaklarının yüklenmesi, ana sayfa kartından gerçek medya başlatma geçti. Oynatma sürerken ana sayfaya gidilip geri dönüldü; aynı yerel video penceresi ve oynatma oturumu korundu.

Tam ekrandaki üst ve alt panelin gerçek genişliği ve sol kenarı ölçülerek eşit olduğu doğrulandı. Alt panel pencereye sığdı. Tam ekranda kategori değiştirme ve kanal seçme, fareyle gösterme/otomatik gizleme, monitörü kaplama, pencere konumunu geri yükleme ve büyütülmüş pencereden gidiş/dönüş geçti.

## Regresyon ve performans

38/38 Core testi geçti. Eski ayarlar ile uyumluluk ve menü tercihinin saklanması yeni teste dahildir. M3U/Xtream/Stalker ayrıştırma, 100.500 içerik, EPG eşleştirme, kaynak izolasyonu, favori/geçmiş, iptal/geri alma ve güncelleyici bütünlük testleri geçti.

Arayüz testi sırasında 100.500 içerik 1.907 ms içinde yüklendi; UI zamanlayıcısının en uzun aralığı 37,2 ms idi. Bunlar test bilgisayarının ölçümleridir; sağlayıcı bağlantı süresini içermez.

Yerel 854 × 480 Sintel videosuyla kare çözme, ileri sarma, duraklatma, ses çubuğu, altyazı ekleme, istatistik verileri, PVR kayıt ve kaydedilen dosyayı tekrar oynatma geçti. EPG izlenen kanalı takip etti. Aynı kanal logosu tek HTTP isteğiyle paylaşıldı.

WPF önizlemeleri GPU video yüzeyini içermez; görüntü üretimi çözülen kareler ve yerel pencere bağlantısıyla doğrulandı. Gerçek ücretli IPTV hesapları ve tüm GPU/DPI birleşimleri bu ortamda sınanmadı.
## Teslim edilen EXE

Bağımsız 1.5.0 EXE ayrı bir çalışma/veri klasörüne çıkarılıp sınandı. Yukarıdaki arayüz ve oynatma kontrolleri dağıtım paketinde de geçti. Yerel canlı tampon ve 20 saniye geri sarma isteği yaklaşık 20,24 saniye gecikmeyle oynatıldı; canlıya dönüldü ve geçici tampon temizlendi. Bu kısa regresyon testi 60 saniyelik uzun yayını yeniden ölçmez; 60 saniyelik uzun süre testi 1.4 raporundadır.

Güncelleme testi aynı EXE'den ayrı bir 1.6.0 deneme paketine yapıldı. İndirmenin doğrulanması, eski sürecin kapanmasını bekleme, yeni sürümün açılması, eski kısayolun yönlendirilmesi ve kütüphanenin korunması geçti. GitHub'a gerçek bir Release gönderilmedi.
