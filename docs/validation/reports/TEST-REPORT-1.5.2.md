# WX Player 1.5.2 — Arayüz doğrulaması

15 Eylül 2026 · Windows x64 · .NET 10 / WPF / LibVLC

## Yeni arayüz kontrolleri

Gerçek WPF ana pencere içinde 900, 1440 ve 1920 piksel genişliklerde kontrol edildi. Her boyutta öneri alanı en fazla 264 mantıksal piksel yükseklikte kaldı. Son izlenenler, favoriler, filmler ve diziler raflarında kartların ve görsel alanlarının ölçüleri eşit çıktı.

600×1800 ve 1800×450 görseller kontrollü yerel HTTP sunucusundan gecikmeli yüklendi. Yükleme öncesi/sonrası kart boyutları eşleşti. Eksik afiş, uzun başlık, kayıtlı dakika ve farklı içerik türleri aynı raflarda denendi. Klavye odağı, son karta kaydırma, kartı açma, tümünü gör, dar pencere arama satırı, boş kütüphane, arama temizleme, hata sonrası yeniden deneme ve durum alanlarının sabit yüksekliği doğrulandı.

Ekran görüntüleri test kütüphanesine ait sentetik afiş ve isimler içerir. Gerçek IPTV hesabı veya kullanıcı kütüphanesi kullanılmadı.

## Tekrarlanabilir kontroller

- `dotnet run --project tests/WXPlayer.Tests -c Release` — katalog, EPG, sağlayıcı, veri ve güncelleme birim kontrolleri.
- `WXPlayer.exe --smoke --home-layout-only --data-dir <ayrı-klasör>` — 1.5.2 arayüz kontrolleri ve görüntüleri.
- `WXPlayer.exe --smoke --media <yerel-video> --stress --timeshift --data-dir <ayrı-klasör>` — tam oynatma, EPG, favori, bölüm/konum, kayıt, kısa canlı geri sarma ve 100.500 içerik testi.
- `tools/test-updater.ps1 -AppExe <dağıtım-exe> -OutputPath <ayrı-klasör>` — izole 1.6.0 test paketine güncelleme.

WPF görüntüleri GPU video yüzeyini içermez; oynatma çözülen karelerle ve yerel pencere bağlantısıyla doğrulanır. Sağlayıcıların bütün afiş, codec ve cihaz çeşitleri bu kontrollü testlerle kapsanmaz.

## 1.5.2 dağıtım EXE'siyle sonuçlar

- 45/45 Core testi ve 38 yeni ana sayfa arayüz kontrolü geçti.
- Tek dosya EXE ayrı çalışma/veri klasörüne açıldı. Sürüm bilgisi 1.5.2 olarak doğrulandı.
- 100.500 içerik 3.394 ms içinde yüklendi; arayüz zamanlayıcısında en uzun aralık 46,6 ms oldu. Bu ölçümler test bilgisayarına aittir.
- Oynatma, ses, altyazı, istatistikler, EPG kanal takibi, tam ekran hizası/favorileri ve kayıt dosyasını yeniden oynatma geçti.
- Film ve bölüm seçimi, konum kaydı ve kayıtlı noktadan devam etme geçti.
- Kısa canlı tampon testinde yaklaşık 20 saniye geri sarma, canlıya dönme ve tampon temizliği geçti. Uzun 60 saniye testi bu arayüz güncellemesinde yeniden çalıştırılmadı.

Derleme tamamlandı. Paketleme sırasında NuGet'in çevrimiçi güvenlik verisine erişim uyarısı (NU1900) oluştu; bu turda bağımlılık sürümleri değiştirilmedi. İlk yerel derleme 0 hata/0 uyarı ile tamamlandı.

Otomatik güncelleme aynı 1.5.2 dağıtım EXE'sinden izole 1.6.0 deneme paketine karşı doğrulandı: SHA-256 kontrolü, eski sürecin kapanmasını bekleme, yeni sürümün açılması, eski kısayolun yönlendirilmesi ve kütüphane korunması geçti. Gerçek GitHub Release yayımlanmadı.
