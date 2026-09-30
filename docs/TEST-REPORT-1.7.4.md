# WX Player 1.7.4 — doğrulama

Windows x64 ve .NET 10 Release yapılandırmasında doğrulandı.

| Kontrol | Sonuç |
| --- | --- |
| Solution derleme | Başarılı, 0 derleme hatası. Çevrimdışı NuGet güvenlik verisi nedeniyle `NU1900` uyarısı görülebilir. |
| Regresyon testleri | 72/72 başarılı. |
| Home yerleşim smoke | Başarılı. |
| Gerçek video ile tam smoke | Tam ekran dış HWND ve iç LibVLC yüzeyi 2560×1440 olarak eşleşti. EPG ve mini pencere boyutlandırmalarında yüzey ölçüleri eşleşti; yerel render başlığı boş kaldı; mini oynatıcıda çözülmüş kareler artmaya devam etti. |
| Paketlenmiş EXE ve portable | İki dağıtım da ayrı veri klasörleriyle Home smoke testini geçti. Tek dosya başlatıcı sıfır çıkış kodu verdi; iki `smoke-results.json` dosyasında `success: true`. |

Testlerde yerel örnek video ve sahte kütüphane kullanıldı; gerçek IPTV sağlayıcılarının her ağ koşulunu temsil etmez. GPU ile çizilen video WPF `RenderTargetBitmap` görüntüsüne yansımayabilir. Bu nedenle yerel pencere ölçüleri, görünürlük ve çözülmüş kareler ayrıca denetlendi.
