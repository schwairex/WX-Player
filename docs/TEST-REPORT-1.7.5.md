# WX Player 1.7.5 — doğrulama

Windows x64 ve .NET 10 Release yapılandırmasında doğrulandı.

| Kontrol | Sonuç |
| --- | --- |
| `dotnet build WXPlayer.sln -c Release --no-restore` | Başarılı; 0 hata. Çevrimdışı NuGet güvenlik verisi için `NU1900` uyarısı. |
| Regresyon testleri | 73/73 başarılı. Erken kesilen bölümün tamamlandı sayılmaması için yeni test dahil. |
| Home smoke | `success: true`. |
| Yerel video ile tam smoke | `success: true`. Tam ekran monitörü ve sağ panelin 380 px genişlik / 84 px üst / 24 px sağ konumu doğrulandı. 3,5 sn gizlenme, `L` ile aç/kapa, favori, kategori, kanal seçimi, mini oynatıcı, EPG ve kayıt akışları kontrol edildi. |
| Dizi deneyimi smoke | `success: true`. Duraklatılmış içerik kendi kendine başlamadı; manuel devam ve oynarken erken kesintiden otomatik toparlanma kayıtlı konumu korudu. Yalnız son 30 saniyede bölüm önerisi, öneriyi kapatma ve sıradaki bölüme geçiş doğrulandı. |

Gerçek oynatma sırasında 1920×1080 tam ekran görüntüsü `WX-Player-1.7.5-fullscreen-visible.png` olarak alınıp kullanıcının HTML referans görseliyle karşılaştırıldı. Üst başlık, alt tek kontrol sırası, zaman çizelgesi ve sağ çekmecenin ölçü ve konumları örtüşüyor. Görüntüdeki doğal farklar: referans statik gradyan ve örnek başlıklar gösteriyor; uygulama gerçek videoyu ve kaynaktan gelen içerikleri gösteriyor. WPF/LibVLC'nin ayrı yerel yüzeyinde CSS `backdrop-filter` bulanıklığı bulunmadığından panel aynı RGBA değeriyle yarı saydam, fakat arkasındaki video bulanık değil. Windows yerel kategori açılır listesi HTML `<select>` ile piksel düzeyinde aynı değil.

Bu testler yerel örnek video ve test kütüphanesi kullanır; sağlayıcıların uzun süreli ağ kopması davranışları kaynak ve bağlantıya göre değişebilir. Test edilen kesinti, oynatım durdurulduktan sonra kayıtlı konumdan yeniden çözümleme ve başlatma akışını kapsar.
