# WX Player 1.6.3

- Discord Rich Presence, oynatılan kanalın logosunu veya film/dizinin afişini büyük görsel olarak gösterebilir. Önce paylaşılması güvenli genel görsel adresi kullanılır; bu yoksa mevcut herkese açık görsel kataloğunda içerik adı ve EPG kimliğiyle eşleşme aranır.
- Sağlayıcının özel görsel adresi, yayın adresi ve hesap bilgileri Discord'a veya görsel kataloğuna gönderilmez. Eşleşme yoksa, adres uygun değilse veya Discord görseli reddederse WX Player simgesi kalır.
- Canlı TV'de EPG programı ve kanal adı korunurken Discord süre çubuğu kaldırıldı. Film ve dizi için kalan süre, duraklatma ve ileri sarma davranışı korundu.
- Hızlı kanal geçişlerinde eski görsel sorgusunun sonucu yeni içeriği değiştiremez. Discord görseli reddederse sonraki içeriklerin görselleri yine denenir.
- Mevcut Discord Application ID sabiti değiştirilmedi. Yeni paket veya bağımlılık eklenmedi.

[Discord kurulumu ve gizlilik](../../guides/DISCORD-PRESENCE.md) · [Test raporu](../../validation/reports/TEST-REPORT-1.6.3.md)
