# WXPlayer 1.7.11

4 Ekim 2026

- Ana sayfa, Filmler, Diziler, Favoriler, Program rehberi ve Son izlenenler, Canlı TV ile aynı sidebar stilini ve ölçeklenen ikon hizasını kullanır. Genişleyen menü içerik alanını yeniden boyutlandırmaz.
- Canlı TV video başlığındaki ÇALIŞIYOR etiketi ve sağ üst CANLI rozeti kaldırıldı. Sağlık kontrolü ayarı ve mevcut izleme mantığı korunur.
- Tam ekranın gerçek kaynak adı, mevcut palete uygun yuvarlatılmış bir etiket ve kütüphane ikonu ile gösterilir. Uzun adlar kırpılır; tam ad araç ipucundadır.
- Tam ekran ana penceresinin zorunlu Topmost/Windows HWND_TOPMOST kullanımı kaldırıldı. Başka uygulamalar WXPlayer önüne gelebilir. Mini oynatıcının mevcut üstte tutma davranışı korunur.
- Ayarlar: sabit sol menü, sağda kaydırılabilir içerik; Oynatma, Kısayollar, Kütüphane, Çevrimiçi ve Güncellemeler. Mevcut ayarlar aynı nesneleri ve kayıt atamalarını kullanır. Anahtarlar, girdiler, kaynak ve temizleme satırları HTML referansının renk/ölçüleriyle düzenlendi.
- Kısayollar dar pencerede tek sütuna geçer; eski test kancası ilgili bölümü seçer ve son kartı görünür yapar. Ayar başlıklarına tıklama, aynı CheckBox'ı değiştirir.

Pencerenin 850×760 ölçüsü ve PremiumWindow başlık/alt başlık/ikon düzeni korunur. Manrope statik font dosyaları, Ideal/ClearType ve WPF DPI ölçümleri kullanılır. Metin kapsayıcılarında blur, efekt, bitmap cache ve opacity mask yoktur.

Referansın sahte sürüm/kaynak/yol verileri kullanılmadı. HTML'deki toast, yapay güncelleme zamanlayıcısı ve ayrı onay sistemi eklenmedi; mevcut UpdateController ve Confirm akışı çalışır. Discord ve afiş ayarları Çevrimiçi bölümüne, kısayollar kendi bölümüne taşındı. Hiçbir mevcut ayar veya işlem kaldırılmadı.

Kaynaklar, EPG, oynatma, kayıt, geri sarma, PiP, Discord, kullanıcı verileri ve görsel önbellek motorları bu sürümde değiştirilmedi. Yalnız ana pencerenin üstte tutulması, istenen pencere sıralaması düzeltmesi kapsamında değişti.

Dağıtım: WXPlayer-1.7.11.exe, WXPlayer-1.7.11-portable.zip ve WXPlayer-1.7.11-source.zip. Test ayrıntıları: [TEST-REPORT-1.7.11.md](../../validation/reports/TEST-REPORT-1.7.11.md).
