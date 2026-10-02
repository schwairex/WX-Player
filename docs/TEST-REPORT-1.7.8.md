# WX Player 1.7.8 — doğrulama

2 Ekim 2026 · Windows x64 · .NET SDK 10.0.401 · Release

| Kontrol | Sonuç |
| --- | --- |
| Çözüm restore ve build | Başarılı; 0 hata, 0 uyarı. Sandbox MSBuild kısıtı için tek düğüm kullanıldı. |
| Mevcut regresyonlar | 73/73 geçti; JSON raporu teslim klasöründe. Discord named-pipe testleri gerçek Windows IPC erişimiyle çalıştırıldı. |
| Home smoke | success:true; 68 olumlu boolean (başarı alanı dahil), hiç false kontrol yok. Dikey/yatay kaydırma, eşit kartlar, arama, yükleme/hata/boş durum, hero okları, klavye odağı ve son izleneni kaldırma doğrulandı. |
| Yeni Home görünümü | success:true; 24 olumlu boolean, hiç false kontrol yok. Özgün üst bar kontrolleri, tek arama, alt durum notu, tür ayrımı ve sıra, ilk12, paylaşılan afiş stili, afiş altı progress, 16:10 kanal kartı, boş favoriler, yalnız görüntü metadata temizliği, gölgesiz/blur olmadan çizim, favori kaydı/odak, mevcut navigasyon ve dar pencere/rem sınırı doğrulandı. |
| Katalog regresyonu | success:true; 23 olumlu boolean, hiç false kontrol yok. Filmler/Diziler, kaynak kontrolünün taşınması, arama, favori, asenkron sayfalama, odak, afişsiz kart, sabit başlık ve ölçekleme korundu. |
| Tam oynatma smoke | success:true. Yerel Sintel videosuyla oynatma, pause/resume/seek, fullscreen v2, EPG, ses/altyazı, kayıt, mini oynatıcı, favori/geçmiş, film/dizi izleme konumu ve Discord doğrulandı. |
| Hata kayıtları | Başarılı QA klasörlerinde startup-error.log, smoke-unhandled.txt veya errors.log yok. |

Komutlar:

```powershell
dotnet restore WXPlayer.sln --ignore-failed-sources --disable-parallel -m:1 -nr:false -p:NuGetAudit=false
dotnet build WXPlayer.sln -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false
dotnet run --project tests/WXPlayer.Tests -c Release --no-build
dotnet run --project src/WXPlayer.App -c Release --no-build -- --smoke --home-layout-only --data-dir <izole-klasor>
dotnet run --project src/WXPlayer.App -c Release --no-build -- --smoke --home178-layout-only --data-dir <izole-klasor>
dotnet run --project src/WXPlayer.App -c Release --no-build -- --smoke --catalog-layout-only --data-dir <izole-klasor>
dotnet run --project src/WXPlayer.App -c Release --no-build -- --smoke --media <yerel-video> --data-dir <izole-klasor>
```

Tam smoke masaüstü fare erişimiyle çalıştırıldı. Sandbox içindeki ilk çalıştırma fiziksel fare konumunu okuyamadığından kontrol gösterme adımında tamamlanamadı. Oynatma veya fullscreen kodu bu nedenle değiştirilmedi. Üç yerel video screenshot işareti false dönebilir; JSON sonuçlarında isimleri saklanır ve başarılı video ekran görüntüsü olarak raporlanmaz. WPF Ana sayfa PNG görselleri gerçek WPF görsel ağacından alınır; GPU video yüzeyi değildir.

Core, MainWindow.Home/Catalog veri sorguları, PlaybackEngine, NativeVideoHost, FullscreenV2/Placement, App.xaml, ChannelLogo, ArtworkService/Cache ve Discord yapılandırması 1.7.7 ile hash karşılaştırmasında aynı. Ana sayfanın480/kataloğun400 afiş çözme genişliği korunur. Hero mevcut9s zamanlayıcısıyla kalır. UI-only değişiklikler; yerel kaynaklar, paylaşılan salt görsel kart üretimi, görsel satır ayrımı/ilk12, özgün kontrollerin taşınması ve odak kimliğidir. Veritabanı şeması, API, oynatıcı veya kullanıcı ayarları değiştirilmedi.

Gerçek sağlayıcı hesapları ve uzun süreli ağ koşulları bu yerel doğrulamanın kapsamı dışındadır. Ayrı JavaScript lint/typecheck yok; C#/XAML çözüm derlemesi kullanıldı.
