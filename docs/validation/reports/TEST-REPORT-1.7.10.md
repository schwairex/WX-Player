# WX Player 1.7.10 — doğrulama

3 Ekim 2026 · .NET SDK 10.0.401 · Windows x64 · Release

| Kontrol | Sonuç |
| --- | --- |
| İlk yerleşim testi / kırmızı | 1.7.9 görünümünde `liveHeaderAlignedWithViewingColumn:false`. Yeni görsel test değişiklikten önce başarısız oldu. |
| Release çözüm derlemesi | Başarılı; 0 hata, 0 uyarı. |
| Regresyonlar | 76/76 geçti. Sağlayıcı, M3U, EPG, SQLite, güncelleme, Discord, bölüm/geçmiş ve önbellek testleri korundu. |
| Canlı TV v3 görsel/akış smoke | success:true; 32/32 boolean kontrol geçti. Arama, kategori, favori, 150 öğelik sayfalama, logo yedeği, Manrope, gerçek EPG/ilerleme, Sharp text ayarları, özgün pause/resume düğmeleri, gear Space, gün gezinmesi, PiP, küçük pencere, drawer, fullscreen ve Home/Catalog/varsayılan ebeveyn geri yükleme doğrulandı. |
| Ana sayfa | Home178: success:true; 24/24 boolean kontrol. |
| Filmler / Diziler | Catalog177: success:true; 23/23 boolean kontrol. |
| Tam oynatma smoke | success:true; 181 boolean sonuçtan 178 true. Oynatma, seek/pause/resume, fullscreen, mini oynatıcı, sağlık, gerçek XMLTV/EPG eşleştirme, kayıt, altyazı, istatistik, bölüm toparlanması ve Discord akışları geçti. |
| Ekran görüntüsü sınırı | playingWindowCaptured, fullscreenWindowCaptured ve fullscreenVisibleScreenshot false. GPU/ön plan video yakalaması başarılı olarak raporlanmaz. Canlı TV PNG'leri WPF katmanı ve popup için RenderTargetBitmap ile üretildi; native video yüzeyi siyah görünür. |
| Korunan kaynaklar | 1.7.9 ile SHA-256 karşılaştırılan 46 çekirdek/oynatıcı/görsel önbellek/Home/Catalog/fullscreen/settings dosyası aynı. App.xaml aynı; global kaynaklar değiştirilmedi. |
| Kod incelemesi | Bağımsız salt okunur incelemede gear klavye, overlay Alt+F4 kapanışı, yüzey mouse hareketleri ve karma DPI yerleşimi bulguları düzeltildi; kalan engelleyici bulgu yok. Karma DPI monitörler arasında fiziksel sürükleme bu makinede ayrıca manuel doğrulanmadı. |

Eski smoke testindeki ayrı GuideNowPanel görünürlüğü yeni yatay karttaki görünür gerçek program/ilerleme kontrolüyle karşılandı. Slider tıklama testi referansın 24 DIP yüksekliğine uyarlandı; tüm dikey aralıkta hit test yapılır. Ses çubuğunda dolu yeşil arka plan olmadığı artık görsel template kökünden denetlenir; track'ın beyaz yarı saydam fırçası yanlışlıkla hata sayılmaz. Fonksiyonel kontroller kaldırılmadı.

```powershell
dotnet restore WXPlayer.sln --ignore-failed-sources --disable-parallel -m:1 -nr:false -p:NuGetAudit=false
dotnet build WXPlayer.sln -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false
dotnet run --project tests/WXPlayer.Tests -c Release --no-build -- <sonuc-json>
WXPlayer.exe --smoke --live1710-layout-only --media <yerel-video> --data-dir <izole-klasor>
WXPlayer.exe --smoke --home178-layout-only --data-dir <izole-klasor>
WXPlayer.exe --smoke --catalog-layout-only --data-dir <izole-klasor>
WXPlayer.exe --smoke --media <yerel-video> --data-dir <izole-klasor>
```

QA kaynakları ve posterleri yalnız `--smoke` altında izole veri klasörlerinde üretilir; normal uygulamaya örnek içerik eklenmez. Kullanıcı veritabanı, ayarlar, gerçek özel M3U/stream URL veya sağlayıcı kimlik bilgileri dağıtım/source paketlerine eklenmez. Kayıt/EPG verisi sağlayıcının sunduğu yeteneklere bağlı kalır.

## Dağıtım doğrulaması

Dağıtım EXE ve portable çalıştırma sonuçları yanında ayrı JSON raporları verilir. Uygulama başlatılırken özel QA veri klasörleri ve EXE açılım klasörü kullanılır; normal kullanıcı verileri değiştirilmez.
