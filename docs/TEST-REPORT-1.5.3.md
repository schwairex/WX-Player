# WX Player 1.5.3 — Test raporu

15 Eylül 2026 · Windows x64 · .NET 10 / WPF / LibVLC

## Katalog ve ana sayfa

46/46 Core testi geçti. Yeni kontrol, afiş filtresinin sayfalama öncesinde uygulanmasını; afişsiz kayıtların tam kütüphane, favori ve geçmişte korunmasını; önerilerde afiş şartını doğrular.

Gerçek WPF penceresinde 900 / 1440 / 1920 genişliklerde afiş, kart ölçüsü, taşma, klavye odağı, boş/hata durumu ve gezinme kontrolleri geçti. Kart üzerinden gönderilen fare tekerleği olaylarıyla sayfanın en altına ve tekrar en üstüne erişildi. Boş adresli ve bozuk görselli içerikler ana sayfadan elendi. Film/dizi afişlerinin kartı doldurma modu doğrulandı.

## Afiş önbelleği

Aynı URL için 96 / 480 / 960 çözme genişliklerinde eşzamanlı 18 çağrı, tek HTTP isteği oluşturdu. Sunucu kapatılıp bellek önbelleği boşaltıldıktan sonra görsel diskten açıldı. Kontrollü, 350 ms gecikmeli yerel testte ilk indirme/çözme yaklaşık 415 ms, diskten yükleme 7 ms sürdü; bunlar internet sağlayıcısı için hız garantisi değildir.

112 ek görsel yüklenmesine rağmen önceki küçük afiş bellek önbelleğinde kaldı. Bozuk görüntü çözülemedi ve hemen tekrar indirilmedi. Çözülen görsellerin boyut sınırı, dondurulmuş bitmap'in yeniden kullanılması ve bellek bütçesi kontrol edildi.

## Oynatma regresyonu

Yerel video ile oynatma, ses, altyazı, seek, istatistikler, kayıt ve kayıt dosyasını yeniden oynatma geçti. EPG'nin kanal değişimini takip etmesi; tam ekran panellerinin hizası, favori davranışı ve otomatik gizlenmesi doğrulandı. Ana sayfadan içerik açma, oynatma devam ederken ana sayfaya dönme, film/bölüm konumunu kaydetme ve devam etme geçti.

Önizlemeler kontrollü test kütüphanesine ait sentetik afişler içerir. WPF ekran görüntüleri GPU video yüzeyini içermez; video çözülen karelerle ve pencere bağlantısıyla doğrulanır. Gerçek IPTV hesaplarının bütün görsel/codec çeşitleri kapsanmaz.

## Tekrarlama

- `dotnet run --project tests/WXPlayer.Tests -c Release`
- `WXPlayer.exe --smoke --home-layout-only --data-dir <ayrı-klasör>`
- `WXPlayer.exe --smoke --stress --timeshift --media <yerel-video> --data-dir <ayrı-klasör>`
- `tools/test-updater.ps1 -AppExe <dağıtım-exe> -OutputPath <ayrı-klasör>`

Derlemeler tamamlandı. Ortamın NuGet çevrimiçi güvenlik verisine erişememesi nedeniyle NU1900 uyarısı oluştu; bağımlılık sürümleri bu güncellemede değiştirilmedi.

## Dağıtım EXE'si doğrulaması

Teslim edilen 1.5.3 EXE ayrı çalışma ve veri klasörüne açıldı. 51 ana sayfa/afiş kontrolü geçti. Fare tekerleğiyle sayfanın başına/sonuna erişim, afişsiz/bozuk görsellerin elenmesi, tek indirme ve diskten afiş açma doğrulandı.

100.500 içerik 3.575 ms içinde yüklendi; arayüz zamanlayıcısında en uzun aralık 43,1 ms oldu. Film ve bölüm konumuna dönüş, EPG, tam ekran favorileri, PVR ve yaklaşık 20 saniyelik canlı geri sarma/canlıya dönüş geçti. Tampon dosyaları durdurma sonrası temizlendi. Uzun 60 saniye testi bu turda yeniden çalıştırılmadı.

Otomatik güncelleme aynı dağıtım EXE'sinden izole 1.6.0 deneme paketine karşı doğrulandı. SHA-256 doğrulaması, eski sürecin kapanmasını bekleme, yeni sürümün açılması, eski kısayolun yönlendirilmesi ve kütüphanenin korunması geçti. Gerçek GitHub Release yayımlanmadı.
