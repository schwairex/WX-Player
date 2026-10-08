# WX Player 1.7.6 — doğrulama

Windows x64, .NET SDK 10.0.401 ve Release yapılandırması.

| Kontrol | Sonuç |
| --- | --- |
| `dotnet build WXPlayer.sln -c Release --no-restore` | Başarılı; 0 hata, 0 uyarı. |
| Regresyon testleri | **73/73 başarılı.** |
| Home testleri | Tam smoke içinde başarılı: kaydırma, kartlar, arama, afiş önbelleği, favori ve tekli geçmiş silme. |
| Gerçek yerel video ile tam smoke | `success: true`. Kaynaklar, EPG, kayıt, mini oynatıcı, oynatma, duraklatma/devam, izleme konumu ve Discord davranışları doğrulandı. |
| v2 tam ekran | 1280 ve 2560 DIP pencere genişliğinde rem ölçeklendirmesi; 26rem çekmece, 5.2rem üst / 1.5rem sağ konumu; monitörü kaplama, aynı yerel video handle'ı, filtreden sonra tam boy video ve çıkışta pencere yerleşimini geri yükleme başarılı. |
| Etkileşimler | Hızlı çekmece geçişleri, favori yıldızı, arama temizleme ve Bölümler → Film arama senkronizasyonu, gerçek hız/ses/altyazı seçenekleri, popup içinde Space/Escape, düğme odaktayken Space ve meşgul kayıt işlemi sırasında R koruması başarılı. |
| Gizlenme | Panel kapalı ve oynatım aktifken 3,5 saniye + 300ms solma; pointer hareketiyle geri gelme başarılı. Yalnız otomatik süre kontrolü fiziksel masaüstü fare hareketlerinden izole edildi. Normal çalışmada bu izolasyon etkin değildir. |
| Dizi deneyimi | `success: true`: duraklatma, kaldığı yerden devam, erken kesintiden toparlanma, son 30 saniyedeki düğme, düğmeyle sonraki bölüm, sezon geçişi ve canlı TV/film ayrımı. |
| XAML / kaynaklar | Derleme ve çalışan arayüzde kaynak ayrıştırma veya yinelenen anahtar hatası yok. |
| Uygulama hata kayıtları | Başarılı doğrulama klasörlerinde `smoke-unhandled.txt` veya `startup-error.log` oluşmadı. |

Yerel video ile tam ekran PNG alındı ve HTML kaynak ölçüleriyle karşılaştırıldı. WPF şeffaf gradyandaki beyaz interpolasyon düzeltildi. Normal pencere düzeninin tam ekrandaki videoyu küçültmesi engellendi. Popup HWND odak değişimi tamamlanmadan tam ekran kontrollerinin gizlenmesi önlendi.

**Görsel sınırlar:** HTML'nin `backdrop-filter` bulanıklığı ayrı LibVLC video yüzeyinde uygulanmıyor; yazı rasterizasyonu ve piksel tabanlı kenar/gölge ölçeklenmesi tarayıcıdan farklı olabilir. Referansın statik önizleme, intro süresi ve tampon yüzdesi gerçek yayın verisi olarak kullanılmadı. Tarayıcı, yerel `file://` HTML açılışını güvenlik nedeniyle reddetti; bu kısıtlama aşılmadı. Karşılaştırma çalışan WPF ekran görüntüsü ve HTML kaynak değerleri üzerinden yapıldı.

LibVLC'nin `SetThumbNailClip failed: 0x800706f4` görev çubuğu thumbnail tanısı bazı test açılışlarında yazıldı; video çözümleme, kayıt ve smoke kontrolleri başarılı kaldı. Bu tanı XAML ya da uygulama istisnası değildir.

Sağlayıcı/core, `PlaybackEngine`, `NativeVideoHost`, `FullscreenPlacement` ve Discord yapılandırması orijinal kaynakla hash karşılaştırmasında değişmedi. Gerçek IPTV sağlayıcılarının uzun süreli ağ kesintileri bu yerel testin kapsamı dışındadır.
