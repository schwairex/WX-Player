# WXPlayer 1.7.16

2 Kasım 2026

- Dizi seçildikten sonraki izleme görünümü, HTML referansına göre Filmler ile aynı ortak kategori/liste/oynatıcı katmanını kullanır. Ortak stiller ve kontroller ikinci kez tanımlanmadı.
- DİZİ BÖLÜMLERİ panelinde gerçek sezon seçimine bağlı haplar ve yatay, sanallaştırılmış bölüm kartları bulunur. Oklar yalnız çok sezon varsa görünür; ilk/son sezonda ilgili ok pasiftir. Sezon gezinmesi oynatmayı değiştirmez.
- Bölüm kartında sol/sağ yalnız odağı taşır; Enter/tıklama mevcut bölüm oynatma yolunu kullanır. Oynatıcı odağındaki seek/ses/Space/F/M/PgUp/PgDn korunur.
- İlk/son bölümdeki pasifleşme panelin mevcut düğmelerinden oynatıcı düğmelerine bağlandı. Paneldeki tekrar eden bölüm düğmeleri görünümden çıkarıldı; komutlar aynıdır.
- Dizi adı, SxxExx rozeti, bölüm durumu ve ilerleme yalnız gerçek veriden gösterilir. Belirsiz/Unicode bölüm adları ham kalır; kod yoksa boş rozet gösterilmez. Oynayan bölüm sezon/dizi değişiminde görünür alana alınır; yeniden boyutlandırma/Rahat mod, gezinilen kart odağını korur.

Filmler, Canlı TV ve Ana sayfa korunur. Yeni otomatik sonraki bölüm eklenmedi: tam ekranda son 30 saniyedeki Sıradaki bölüm düğmesi aynı davranır. Bölüm yükleme, devam konumu, progress/izlendi kaydı, kaynak/provider/EPG/oynatıcı motoru, ayarlar ve Discord yapılandırması değişmedi. Yeni şeffaf pencere/blur/ağır gölge yok.

Sonuçlar ve ortam sınırları: [TEST-REPORT-1.7.16.md](TEST-REPORT-1.7.16.md).
