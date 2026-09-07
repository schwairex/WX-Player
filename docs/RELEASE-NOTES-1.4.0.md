# WX Player 1.4.0

- Sol menü, bağımsız yuvarlatılmış panel, daha sade marka alanı ve belirgin seçili bölüm tasarımıyla yenilendi. Küçük pencerede Ayarlar dahil tüm menü öğeleri görünür kalır.
- HTTP/HTTPS canlı TV için son **60 saniyeye kadar yerel geri sarma** eklendi. Timeline veya yön tuşlarıyla geri gidin; **CANLIYA DÖN** düğmesiyle güncel akışa geçin.
- Tam ekranda fare hareketiyle açılan kategori ve içerik paneli: tür, kategori, arama, yatay kanal kartları ve sayfa gezinmesi. Fare paneldeyken açık kalır, hareketsizlikte kontrollerle birlikte gizlenir.
- Ses çubuğunun yeşil odak arka planı kaldırıldı; büyük tıklama alanı korundu.
- M3U `tvg-logo` ve sağlayıcı logo adresleri arka planda yüklenir. Aynı logo önbellekten paylaşılır; yüklenemediğinde harf simgesi görünür.
- Ses/altyazı ve kaynak ekleme pencereleri yeniden tasarlandı. İstatistiklerde çözünürlük, FPS ve ses özeti kartları eklendi.
- Ayarlar kısayolları ayrı satırlar ve tuş biçimli etiketlerle düzenlendi.

## Canlı geri sarma hakkında

Tampon kanalı açtıktan sonra birikir; açılıştan önceki yayını içermez. İlk saniyelerde geri sarılabilen süre daha kısadır. Kanal değişiminde tampon temizlenir. Aynı canlı bağlantı yerel HLS parçalarına aktarılır; normal izleme ve tekrar bu yerel veriden oynatılır. Kodlama yapılmaz. HLS hazırlığı başlangıca birkaç saniye ekleyebilir.

Sağlayıcının içeriği TS içine aktarılabilir olmalıdır. Desteklenmeyen veya hazırlanamayan akışta doğrudan oynatmaya dönülür. Bu yerel özellik HTTP/HTTPS canlı TV içindir; DRM, RTSP/UDP ve DirectShow için bu sürümde yerel geri sarma sunulmaz. Kaynak başlığı/ses ve altyazı parçaları taşıma kapsayıcısının desteklediği ölçüde korunur.

Rehberdeki eski programları açan sağlayıcı Catch-Up özelliği ayrı kalır. Yerel tampon son 60 saniyeyi tutar; geçici parça dosyaları döndürülür, disk kullanımı yaklaşık 256 MB eşiğinde kontrol edilir. Uzun süre duraklatılmış tekrar penceresi dolunca canlıya dönülür.

## Güncelleme yayını

GitHub'da **v1.4.0** kararlı Release oluşturun; **WXPlayer-1.4.0.exe** ve **SHA256SUMS-1.4.0.txt** dosyalarını ekleyin. 1.2 ve sonraki sürümlerde otomatik güncelleyici açılışta veya dört saatlik kontrolde bunu alır. Mevcut kaynaklar, favoriler ve ayarlar korunur.