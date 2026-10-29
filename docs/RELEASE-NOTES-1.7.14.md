# WXPlayer 1.7.14

20 Ekim 2026

- Yayın istatistikleri ile Ses ve altyazılar pencerelerinin rozetleri, yüksekliğe göre yuvarlanan ortak `CatalogPillBorder` ile düzeltildi.
- Normal oynatıcının başlığı ve alt kontrolleri, oynatma sırasında 3 saniye hareketsizlikten sonra gizlenir. Fare hareketi kontrolleri gösterir. Kontroller üzerinde fare/klavye odağı, açık menü, sürüklenen kaydırıcı, duraklatma ve oynatılmayan durumlar korunur. Tam ekranın mevcut 3,5 saniyesi değişmedi.
- **Rahat mod:** Ayarlar → Oynatma → Görünüm. Kaydettikten sonra uygulanır; **F8** anında açar/kapatır ve kaydeder. Önceki ayar dosyalarında varsayılan kapalıdır.
- Ek ölçek tek kaynakta `ComfortModeScale = 1.5` olarak tanımlıdır. Küçük pencerelerde taşmayı önlemek için sınırlandırılır. Açılır listeler, ipuçları, bağlam menüleri ve ayrı oynatıcı kontrolleri aynı ölçeği kullanır. Diyaloglar monitörün çalışma alanının %90'ını aşmaz. Mini oynatıcı büyütülmez.

Mevcut DPI/rem ölçeklemesi, oynatma oturumu ve tasarım korunur. Yeni paket, blur, ağır gölge, bitmap cache veya şeffaf pencere eklenmedi. PlaybackEngine, LiveBuffer, sağlayıcılar, EPG, veritabanı ve Discord Application ID değiştirilmedi.

Doğrulama ve gerçek ekranla doğrulanamayan koşullar: [TEST-REPORT-1.7.14.md](TEST-REPORT-1.7.14.md).
