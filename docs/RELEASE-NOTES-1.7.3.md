# WX Player 1.7.3

Bu sürüm mini oynatıcıyı ve film/dizi izleme akışını iyileştirir. Ana sayfanın öne çıkan alanı, kütüphanedeki afişli içerikler arasında gezilebilen sinematik bir karusele dönüştürüldü.

- Mini oynatıcı, etkin LibVLC görüntü HWND'sini değiştirmeden kendi penceresine taşır. Bu, oynatma sırasında boş/gri video yüzeyi oluşmasına yol açan pencere değiştirme yolunu kaldırır. Pencere doğrudan kapatıldığında veya `Alt+F4` kullanıldığında görüntü yüzeyi yok edilmeden ana oynatıcıya döner. Başlık, geri dön, oynat/duraklat, ses, canlı durumu ve isteğe bağlı ilerleme göstergesi yeniden düzenlendi.
- Film veya bölüm duraklatılırken ilerleme veritabanına kaydedilmeden önce oynatıcıya duraklat komutu verilir. Devam ettirme, tekrar oynatma çağrısı yapmadan aynı medya oturumunda çalışır.
- Açılma/tamponlanma sırasında oynat/duraklat düğmesi bir bölümü baştan başlatmaz. Seçili öğeye çift tıklama da duraklatılmış içeriği yeniden açmak yerine devam ettirir.
- Ana sayfa karuseli yalnız mevcut kaynağın afişi hazır içeriklerini kullanır; oklar, konum göstergeleri ve dokuz saniyelik otomatik geçiş eklenmiştir. Fare karuselin üzerindeyken otomatik geçiş durur. Dizi seçiminde mevcut bölüm seçme akışı korunur.

Kütüphane, sağlayıcı, EPG, Discord Rich Presence, canlı tampon ve kayıt biçimi değiştirilmedi.

[Doğrulama raporu](TEST-REPORT-1.7.3.md) · [GitHub Release kılavuzu](GITHUB-RELEASES.md)
