# WXPlayer 1.7.11 doğrulama

4 Ekim 2026 · Windows x64 · .NET SDK 10.0.401 · WPF/.NET 10

| Kontrol | Sonuç |
| --- | --- |
| Restore ve Release build | Başarılı; 0 hata, 0 uyarı |
| Mevcut regresyon testleri | 76/76 |
| Ayarlar ve ortak sidebar smoke | 37/37 (success dahil) |
| Tam ekran pencere sıralaması smoke | 7/7 (success dahil) |
| Canlı TV smoke | 32/32 |
| Ana sayfa smoke | 24/24 |
| Filmler/Diziler smoke | 23/23 |
| Tam smoke | success=true; 178/181 boolean |

Tam smoke'un false kalan üç değeri: playingWindowCaptured, fullscreenWindowCaptured, fullscreenVisibleScreenshot. Bunlar 1.7.10'da da bulunan yerel GPU/ekran yakalama sınırlamalarıdır. WPF görsel katmanı RenderTargetBitmap ile incelendi; bu yöntem LibVLC'nin yerel video HWND'sini yakalamaz. GPU video görüntüsünün birebir ekran görüntüsü doğrulandığı iddia edilmez. İşlev kontrolleri geçer; yeni XAML kaynak, başlangıç veya unhandled hatası yoktur.

Pencere sıralaması testi eski kodda fullscreenDoesNotForceTopmost=false ile başarısız oldu. HWND_NOTOPMOST ve etkinleşmede Topmost'u açan satırın kaldırılması sonrası ayrı bir üst seviye pencere tam ekranın önüne geldi; tam ekran boyutu, orijinal kontrol sahipliği ve normal pencereye dönüş korundu. Normal Canlı TV sahibi ve görsel overlay üzerinde de ayrı pencere öne gelebildi.

Ayarlar smoke: tüm sayfalarda aynı sidebar genişliği/seçim stili; sekiz mevcut CheckBox; beş sabit menü; kaynak düzenleme orijinal dialog/veri; orijinal temizlik ve kaynak kaldırma onay metinleri; iptal ve gerçek SQLite temizleme/kaynak kaldırma; async yeniden yükleme; 199, 10001 ve metin önbelleğinin reddi; geçersiz kayıt yolu; kaydetmeden kapatma; 200/10000 sınırları ve tüm PlayerSettings alanlarının diskteki sonucu.

İnceleme bulguları: dar Ayarlar kısayolları için önce başarısız test, sonra tek sütunla başarılı test; ayar başlıklarının aynı CheckBox'ı değiştirmesi için RED→GREEN; sidebar yazı ağırlığı sabitlendi. Kısayol test kancası yeni bölümü seçer ve son Border'ı görünür yapar. Üstteki PremiumWindow başlık/alt başlık/ikon düzeni ve 850×760 ölçüsü korunur. Efekt, opacity mask veya bitmap cache içeren metin kapsayıcısı bulunmaz.

Kaynak farkı: WXPlayer.Core içindeki 19 kaynak dosyası aynı SHA-256 ile korundu. PlaybackEngine, NativeVideoHost, LiveBuffer, Discord, seri akışı, Home/Catalog veri akışları, PremiumWindow ve UpdateController değişmedi. Settings Kaydet bloğu önceki kaynakla birebir aynıdır. Confirm metinleri/async işlem gövdeleri bağımsız salt okunur incelemede karşılaştırıldı.

Tek istisna, kullanıcının istediği diğer uygulamaların öne gelebilmesi için FullscreenPlacement ve MainWindow'un üstte tutma politikasıdır. Mini oynatıcının mevcut üstte kalma davranışı değişmedi. Yeni paket, ayar, komut, veri modeli, sağlayıcı sorgusu veya öneri mantığı eklenmedi.

Komutlar:

```powershell
dotnet restore WXPlayer.sln --ignore-failed-sources --disable-parallel -m:1 -nr:false -p:NuGetAudit=false
dotnet build WXPlayer.sln -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false
dotnet run --project tests/WXPlayer.Tests -c Release --no-build -- <izole-test-sonuc-json>
WXPlayer.exe --smoke --settings1711-layout-only --data-dir <izole-QA>
WXPlayer.exe --smoke --shell1711-layout-only --data-dir <izole-QA>
WXPlayer.exe --smoke --live1710-layout-only --media <yerel-test-video> --data-dir <izole-QA>
WXPlayer.exe --smoke --home178-layout-only --data-dir <izole-QA>
WXPlayer.exe --smoke --catalog-layout-only --data-dir <izole-QA>
WXPlayer.exe --smoke --media <yerel-test-video> --data-dir <izole-QA>
```

CLI home/NuGet konumları yalnız test işlemlerinde ayarlandı; kalıcı ortam değişikliği yapılmadı. QA örnekleri yalnız --smoke ve ayrı data-dir içinde üretilir; gerçek kullanıcı kütüphanesine dokunulmaz.

Dağıtım EXE/portable doğrulama sonuçları ve SHA-256 dosyası outputs yan dosyalarında bulunur.
