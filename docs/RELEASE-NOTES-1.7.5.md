# WX Player 1.7.5

Bu sürüm, uzun süre duraklatılmış bir bölümün akışı sona erdiğinde ilerlemeyi korur ve tam ekran oynatıcıyı sağdan açılan kütüphane ile yeniden düzenler.

- LibVLC akışı gerçek bölüm sonundan önce `Ended`/hata durumuna geçerse içerik tamamlandı sayılmaz. Oynarken kesilirse en fazla iki kez yeni bağlantı çözülerek kayıtlı konumdan otomatik sürdürülür; kullanıcı duraklattıysa kendi kendine başlamaz. Oynat düğmesiyle de kaldığı yerden devam edilir. Sıradaki bölüm önerisi yalnızca gerçekten son 30 saniyeye gelindiğinde gösterilir.
- Tam ekranda üst başlık, altta tek zaman çizelgesi ve kontrol sırası, sağda açılır kütüphane paneli yer alır. Kaynak, arama, kategori, favori ve bölüm bilgileri mevcut kütüphaneden gelir. Panel `L` veya menü düğmesiyle açılır ve kapanır.
- Panel kapalı ve video oynuyorsa kontroller 3,5 saniye hareketsizlikten sonra 350 ms içinde solar. Hareket, kontrolleri geri getirir. Klavye odak halkaları ve ekran okuyucu adları korunur.
- Referans HTML'deki Manrope fontu ve `--ink`, `--mut`, `--acc`, `--live`, `--glass`, `--line` renkleri yalnız tam ekran katmanına uygulandı. Mini oynatıcı, sağlayıcılar, EPG ve kütüphane akışları aynı kaldı.

WPF yerel video yüzeyi nedeniyle referanstaki örnek arka plan yerine gerçek video görünür. Ayrı saydam pencere üzerinde CSS `backdrop-filter` bulanıklığı uygulanamaz; yarı saydam panel tonu ve ölçüler korunmuştur. Sistem `ComboBox` açılır menüsü HTML `<select>` ile piksel düzeyinde aynı değildir.

[Doğrulama raporu](TEST-REPORT-1.7.5.md) · [GitHub Release kılavuzu](GITHUB-RELEASES.md)
