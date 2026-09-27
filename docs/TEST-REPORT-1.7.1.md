# WX Player 1.7.1 — doğrulama

| Kontrol | Sonuç |
| --- | --- |
| .NET 10 Release derlemesi | Başarılı; 0 derleme hatası. Çevrimdışı NuGet güvenlik verisi için `NU1900` uyarısı oluştu. |
| Regresyon testleri | **69/69 geçti.** Yerel Discord IPC testleri için Windows named-pipe erişimi olan oturum kullanıldı. |
| Home düzen smoke testi | Başarılı. 1.7.1 afişli film/dizi rafları, yıl metni ve yatay kaydırma doğrulandı. |
| Tam uygulama smoke testi | Başarılı. Katalog araması, canlı EPG, tam ekran dönüşü, oynatma, Discord, favoriler ve mevcut diğer akışlar doğrulandı. |
| Tek EXE smoke testi | Başarılı; 1.7.1 başlatıcısı açıldı, Home ve afişli katalog düzeni doğrulandı. |
| Portable EXE smoke testi | Başarılı; taşınabilir paketin uygulama EXE'si açıldı ve aynı düzen denetimleri geçti. |
| Yayın akışı | Oynatılan kanalın mevcut programı ve ilerleme kartı tam ekrandan dönüşte de görünür. |
| Başlangıç/işlenmemiş hata kayıtları | `startup-error.log` ve `smoke-unhandled.txt` oluşmadı. |

Testler yerel örnek oynatma listesi, üretilmiş test afişleri ve yerel test videosuyla çalıştırıldı. Gerçek sağlayıcı kataloğunun afiş kalitesi ve EPG sunumu kaynağın verilerine bağlıdır.
