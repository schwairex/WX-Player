# Ana sayfa 1.7.8 — görsel doğrulama

Referans: kullanıcının wx-player-ana-sayfa.html dosyası. Ana sayfa WPF olarak çevrildi; webview eklenmedi.

- --bg/#0A0C0E, --p1/#101417, --p2/#171C20, --p3/#1F262B, --ink/#F2F5EE, --mut/#8F9996, --acc/#C5F27A, --live/#FF6B78 kaynakları HomeTheme/CatalogTheme kapsamında kalır. Global fullscreen anahtarları değiştirilmedi.
- Manrope, clamp(14,.005*pencere-genisligi+8,20)/16 kök ölçeği,4.6rem başlık,2.2rem sayfa kenarları,11rem2:3 afişler,12.5rem16:10 kanallar,3derece eğik hero afişi, aktif geniş nokta ve buton altı ok/nokta satırı uygulandı.
- Ana sayfada blur veya ağır DropShadowEffect yok. Yarı saydam koyu başlık ve radial/linear WPF DrawingBrush arka planı kullanıldı.
- Afiş kartları Filmler/Diziler ile aynı CatalogCardButton ve CatalogPosterCard yapısını kullanır. Görsel kaynak yükleme/önbellek davranışı korunur; Ana sayfa480 çözme boyutuyla devam eder.
- WPF odak stili ve AutomationProperties.Name, web aria-label/focus karşılıklarıdır. Satır okları sürekli sönük, hover/odakta belirgin kalır. Kaydırma, Tab/Enter/Space ve mevcut oynatıcı kısayolları korunur.

PNG dosyaları izole QA kütüphanesinden alınmıştır; üretim uygulamasına sahte içerik eklenmedi. Ana sayfa, alt satırlar ve900x650 pencere görüntüleri incelendi. Tarayıcı ile piksel eşitliği iddia edilmez; kullanıcı talebine göre backdrop blur ve ağır gölge kaldırıldı, metin/gradient/focus yerel WPF çizimine çevrildi. Eklenmeyen ve korunan ek öğeler RELEASE-NOTES-1.7.8.md içinde listelidir.
