# WX Player 1.7.2 — doğrulama

Windows x64, .NET 10 Release yapılandırmasında test edildi.

| Kontrol | Sonuç |
| --- | --- |
| Solution derleme | Başarılı; 0 hata. NuGet güvenlik verisine erişim olmadığı için NU1900 uyarısı var. |
| Regresyon testleri | 72/72 başarılı; ayar kalıcılığı, sağlık durumu ve kayıt başlangıç politikası testleri dahil. |
| Home smoke | Başarılı; ana sayfa yerleşimi ve XAML kaynakları açıldı. |
| Dizi deneyimi smoke | Başarılı; bölüm listesi, bölüm geçişi ve sonraki bölüm kartı çalıştı. |
| Tam smoke | Başarılı; tam ekran panel hizası, mini oynatıcıda aynı oynatma oturumu, canlı kanal sağlık göstergesi, EPG ve mevcut oynatıcı akışları doğrulandı. |
| Kayıt smoke | Başarılı; 52,2 sn kaynakta 20. saniyeden başlayan 2,3 sn izleme aralığından yaklaşık 5,8 sn TS üretildi. Kaynağın tamamı veya 20. saniyeye kadarki bölüm alınmadı. Anahtar kare hizası nedeniyle aralık bire bir çerçeve hassasiyetinde değildir. |
| Paketlenmiş uygulama | Taşınabilir EXE ve tek dosya dağıtım EXE'si ayrı veri klasörleriyle Home smoke testini geçti. Tek dosyalı sürümün açılım kökü test ortamındaki yazılabilir klasöre yönlendirildi. |
| Başlangıç/işlenmeyen hata logları | Smoke klasörlerinde `startup-error.log` ve `smoke-unhandled.txt` oluşmadı. |

Gerçek IPTV sağlayıcıları, bağlantı sınırları ve korumalı akışlar yerel test medyasıyla simüle edilmedi. Kanal sağlık göstergesi açılış/oynatma durumunu ölçer; uzun süreli yayın kalite izleyicisi değildir.
