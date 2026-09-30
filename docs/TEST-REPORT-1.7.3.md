# WX Player 1.7.3 — doğrulama

Windows x64 ve .NET 10 Release yapılandırmasında doğrulandı.

| Kontrol | Sonuç |
| --- | --- |
| Solution derleme | Başarılı, 0 derleme hatası. NuGet güvenlik verisi çevrimdışı olduğunda `NU1900` uyarısı oluşuyor. |
| Regresyon testleri | 72/72 başarılı. |
| Home yerleşim smoke | Başarılı: karusel ileri/geri gezinmesi; dar/geniş pencere, afiş ve raf yerleşimi. |
| Gerçek video ile tam smoke | Başarılı: mini oynatıcıda aynı HWND'nin taşınması, görünür render yüzeyi ve artan çözülmüş kareler; pencereyi doğrudan kapatınca yüzeyin geri dönüşü; film/bölüm duraklat-devam et ve konum koruma; tam ekran, EPG ve mevcut akışlar. |
| Paketlenmiş EXE ve portable | Her iki paket ayrı veri klasörleriyle Home smoke testini geçti; karusel ileri/geri ve geniş pencere kontrolleri dahil. |

Test medyası ve sahte kütüphane, gerçek IPTV sağlayıcılarının ağ davranışlarını bütünüyle temsil etmez. GPU ile çizilen video WPF `RenderTargetBitmap` ekran görüntüsünde görünmeyebilir; bu yüzden mini oynatıcı için aynı etkin HWND'nin taşındığı ve oynatma oturumunun kesilmediği ayrı ayrı kontrol edildi.
