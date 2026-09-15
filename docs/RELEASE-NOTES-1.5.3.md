# WX Player 1.5.3

15 Eylül 2026

- Ana sayfadaki yatay rafların fare tekerleğini yakalayıp dikey kaydırmayı engellemesi düzeltildi. Tekerlek kartların üzerindeyken de sayfayı aşağı/yukarı kaydırır. Shift + tekerlek ilgili rafı yatay kaydırır; gezinme okları ve klavye erişimi korunur.
- Afiş yükleme altyapısı yenilendi. Aynı URL farklı boyutlarda kullanılsa da tek ağ indirmesi yapılır. Çözülen görseller 64 MB hedef bütçeli LRU bellek önbelleğinde saklanır; önbellek 96 görsel sınırında topluca silinmez.
- Görseller uygulamanın veri klasöründeki `artwork-cache` altında hash adlarıyla saklanır. Sonraki açılışlar diskten yararlanır. Disk temizliği 192 MB hedefi ve 7 günlük yaş sınırıyla yapılır. Büyük dosyalar ve çözülen görsel ölçüleri sınırlandırılır; yükleme/çözme arka planda, sınırlı eşzamanlılıkla çalışır.
- Ana sayfa sorguları afiş adresi bulunan kayıtları sayfalama öncesinde seçer. Başarıyla yüklenmiş afişler geldikçe içerik rafları gösterilir. Afişsiz veya çözülemeyen görselli kartlar ana sayfaya alınmaz; tam kütüphanede, favorilerde ve geçmişte kayıtları korunur. Afişlerin ilk indirilişi sağlayıcının yanıt süresine bağlıdır.
- Netflix tarzından esinlenen, afiş ağırlıklı ana sayfa: sınırlı yükseklikte sinematik öneri alanı, ortak 2:3 film/dizi kartları, daha belirgin raf başlıkları ve sade metadata. Afişler oranları bozulmadan kartı doldurur; gerekirse kenarlardan kırpılır. Canlı kanal logoları tamamı görünecek şekilde sığdırılır.
- Aynı ana sayfa yenilenirken mevcut kartlar ve kaydırma konumu korunur. Kaynak veya arama değiştiğinde sayfa başa döner.

Kaynakları yeniden eklemek veya kütüphaneyi temizlemek gerekmez. Ana sayfada görünmeyen içerikler, Canlı TV / Filmler / Diziler veya Tüm kütüphane üzerinden açılabilir.
