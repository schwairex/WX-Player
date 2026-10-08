# WX Player 1.7.9

2 Ekim 2026 · Windows x64

- Yerel M3U dosyalarında geçersiz logo/afiş adresinin tüm içe aktarmayı geri aldırması düzeltildi. İsteğe bağlı görsel çözümlenemiyorsa boş bırakılır; oynatılabilir içerikler yüklenir.
- Geçerli HTTP ve göreli görsel adresleri, UTF-8/Türkçe adlar, karma satır sonları, kanal kategorileri ve dizi/bölüm gruplaması korunur. Çözümlenemeyen yayın adresleri mevcut oynatılabilirlik denetiminde atlanır.
- Yerel dosya, uzak liste ve geçersiz yayın adresi için üç regresyon testi; gerçek kaynak penceresi ve içe aktarma akışını denetleyen isteğe bağlı smoke testi eklendi.

Kullanıcının yaklaşık 30 MB M3U dosyası değiştirilmeden test edildi. Önce 2.507. içeriğin hatalı isteğe bağlı görsel adresinde `UriFormatException` oluştuğu doğrulandı; düzeltmeden sonra dosya ayrıştırma, SQLite kayıt, kaynak yeniden yükleme ve uygulamadaki Canlı TV listesi adımlarını geçti. Kayıt sayısı dizi bölümlerini de içerir; dizi sayacı ise birleştirilmiş dizi başlıklarını gösterir.

Arayüz, oynatma motoru, Xtream/Stalker işlemleri, EPG, favoriler, geçmiş, veritabanı şeması, Discord Application ID ve kullanıcı ayarları değiştirilmedi. Kaynak dosyası veya özel IPTV bağlantıları dağıtım paketlerine alınmadı.

Kullanım: **Kaynak ekle → M3U / TXT → Dosyadan seç → Bağlan ve yükle**. M3U dosyasını düzenlemeniz veya Xtream hesabınızı değiştirmeniz gerekmez.

[Doğrulama raporu](../../validation/reports/TEST-REPORT-1.7.9.md)
