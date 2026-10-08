# WX Player 1.7.8

2 Ekim 2026 · Windows x64 · .NET 10 / WPF

Ana sayfa, wx-player-ana-sayfa.html referansından WPF karşılıklarıyla çevrildi. Manrope, koyu yüzeyler, tek arama, özgün kaynak seçici ve kaynak eylemleri, sol metin/sağ eğik afiş, butonların altındaki ok/nokta grubu ve 16:10 kanal kartları aynı yerel tasarım dilinde gösterilir.

Satırlar: İzlemeye devam et, Son izlenen kanallar, Filmler, Diziler, Favori filmler ve diziler, Favori kanallar. Boş satırlar gizlenir. Kullanıcının açık onayıyla mevcut HomeShelf movie/series listelerinin ilk 12 öğesi kullanılır; mevcut sorgu, öneri ve sıralama değişmez. Afiş kartları CatalogCardButton ve ortak CatalogPosterCard görselini kullanır. Ana sayfada blur ve ağır gölge yoktur.

Hero mevcut 9 saniyelik zamanlayıcısı ve seçim davranışıyla korunur. Yeni zamanlayıcı veya komut eklenmez. Arama Binding, oynatma, bölüm seçimi, favori/geçmiş kayıtları, sağlayıcılar, EPG, Discord, ayarlar, tam ekran, mini oynatıcı, kayıt ve görsel yükleme/önbellek mantığı korunur.

Yalnız görünümden çıkarılanlar: üstteki Canlı TV/Filmler/Diziler/Favoriler kısayolları ve İzlemeye dön etiketi. Bunların özgün callback işlemleri silinmedi. Korunan ek işlevler: Şimdi canlı satırı, son izlenenlerden tekli kaldırma, kütüphane özeti, kaynak seçenekleri, menü daraltma/genişletme ve durum/iptal kontrolleri.

HTML örnek detay modalı, sahte özeti/toast bildirimleri, koleksiyonlar menüsü ve uydurma içerik verileri eklenmedi. Mevcut oynatma ve dizi bölüm seçimi akışları kullanılır. WPF AutomationProperties.Name ve odak stilleri, HTML aria-label/focus karşılıklarıdır. CSS clamp/rem mevcut katalog ölçeklendirmesiyle, backdrop-filter ise yarı saydam koyu fırçayla karşılanır. Veri değişmeden yalnız formatter ile köşeli parantezli etiketler temizlenir; bilinmeyen süre gizlenir.
