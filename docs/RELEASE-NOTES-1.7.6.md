# WX Player 1.7.6

Tam ekran arayüzü, sağlanan `iptv-fullscreen-v2.html` referansının yerleşim ve değerleriyle mevcut WPF oynatıcıya uyarlandı.

- Manrope fontu; `--ink`, `--mut`, `--acc`, `--live`, `--line`, `--glass` renk anahtarları; tek alt kontrol sırası, üst başlık ve sağ kütüphane çekmecesi.
- Kök yazı boyutu `clamp(14px, .62vw + 8px, 22px)` ile ölçeklenir. Çekmece 26rem genişliğinde, üstten 5.2rem ve sağdan 1.5rem uzaklıktadır. Çekmece geçişi 350ms, kontrollerin solması 300ms; oynatım ve kapalı panel sırasında hareketsizlik süresi 3,5 saniyedir.
- Bölümler, Favoriler, Canlı, Dizi ve Film sekmeleri; gerçek kaynak verisiyle arama, seçili içerik göstergesi ve favori yıldızları. Bölüm listesindeki ilerleme kayıtlı izleme verisinden gelir.
- Hız, ses ve altyazı menüleri gerçek LibVLC seçeneklerine bağlıdır. Kayıt, PiP, istatistikler, ses ve zaman çizelgesi mevcut işlemleri kullanır. Kategori ve sayfalama, arama alanının sağ tık menüsündedir; dış altyazı dosyası mevcut diyalog üzerinden ses/altyazı menüsünün sağ tık seçeneğiyle eklenir.
- **Sıradaki bölüm**, onaylanan HTML yerleşimindeki sol alt küçük düğmedir. Gerçek dizinin son 30 saniyesindeki mevcut öneri kuralı korunur. Space, yön tuşları, L ve mevcut kısayollar çalışır; popup içindeki klavye odağı da oynatıcı kısayollarına bağlıdır.
- Hızlı panel geçişleri, arama filtresi senkronizasyonu, pasif fare olayları ve devam eden kayıt işleminde tekrar tetikleme koruması kontrol edildi.

Sağlayıcı, stream çözümleme, SQLite, EPG, Discord Application ID, mini oynatıcı video yüzeyi ve oynatma motoru değiştirilmedi.

## Referansla kalan farklar

WPF/LibVLC ayrı yerel video yüzeyinde CSS `backdrop-filter` uygulanmaz; aynı RGBA tonu kullanılır, arka video bulanıklaştırılmaz. WPF yazı rasterizasyonu ve piksel tabanlı gölge/kenarların ölçeklenmesi tarayıcıyla birebir değildir. Referanstaki sahte intro bitişi, sabit tampon yüzdesi ve statik zaman çizelgesi önizlemesi gerçek veri olarak sunulmadı; bu veri yokken ilgili öğeler gösterilmez. Gerçek içerik, süre, logo ve kaynak adları kullanılır. Uygulama HTML/WebView'e taşınmadı.

[Doğrulama raporu](TEST-REPORT-1.7.6.md) · [GitHub Release kılavuzu](GITHUB-RELEASES.md)
