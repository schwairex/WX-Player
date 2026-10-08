# 1.7.7 görsel doğrulama

Hedef: kullanıcının `wx-player-filmler-diziler.html` dosyası. Uygulama: mevcut WPF/.NET 10 projesi; tarayıcı veya WebView gömülmedi.

Kontrol edilen ekranlar: Filmler, Diziler, 900×650 pencere, mevcut Home ve gerçek videolu tam smoke akışları. PNG'ler çalışan WPF ağacından alınır; katalog tamamen WPF olduğundan afiş ve başlıkları içerir. QA afişleri üretim kütüphanesine eklenmez.

Kaynak ölçüleriyle karşılaştırmada kapsül radius normalizasyonu, kaynak adının yazı rengi, arama alanının genişliği ve hero gradyanının alfa interpolasyonu düzeltildi. Kategori kısayolları, odak, favori ve sayfalama test edildi. Sidebar ve diğer sayfalar kullanıcı kapsamı gereği korundu.

**final result: blocked — yalnız tarayıcı ile birebir piksel karşılaştırması.** Yerel `file://` HTML açılışı tarayıcı güvenlik politikası tarafından reddedildi. Başka URL, tarayıcı yüzeyi veya dolaylı yöntemle engel aşılmadı. Kaynak-değer/WPF ekran kontrolü yapılabildi; tarayıcı screenshot karşılaştırması yapılamadı.

Bilinen farklar ve mevcut mantıkta karşılığı olmayan öğeler [sürüm notlarında](../../releases/notes/RELEASE-NOTES-1.7.7.md) listelenir. Bu rapor piksel eşitliği iddiası değildir.
