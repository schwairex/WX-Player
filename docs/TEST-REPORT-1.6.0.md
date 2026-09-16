# WX Player 1.6.0 — Test raporu

15 Eylül 2026 · Windows x64 · .NET 10 / WPF / LibVLC

## Otomatik doğrulamalar

57/57 ağdan bağımsız Core testi geçti. Gerçek herkese açık servis entegrasyonu dahil 58/58 test geçti.

- Bölümlerin sayısal sıralanması (2 → 10), sezon sonunda sonraki sezona geçiş, ilk/son bölüm sınırları ve kaynak/dizi ayrımı.
- Sonraki bölüm önerisinin tam 30 saniye eşiği; normal pencere, canlı TV, film, son bölüm ve bilinmeyen süre durumları.
- Eksik görsel adı sadeleştirme; kanal numarasını koruma; kaynağın mevcut görselini değiştirmeme.
- Aynı adlı dizilerin yıla göre ayrılması; belirsiz sonuçların reddedilmesi. Wikipedia'daki film müziği albümü ve kişi sonuçlarının film afişi olarak kabul edilmemesi.
- Eşzamanlı aynı başlık aramalarının birleştirilmesi, bir bekleyenin iptalinin diğerini bozmaması, olumsuz sonuç önbelleği, yeniden açılışta çevrimdışı eşleştirme kullanımı.
- Kanal kimliğiyle logo bulma ve indirilen kanal/logo dizinini tekrar kullanma.
- Logo değişirken WPF kayıt kimliği/hash değerinin sabit kalması ve binding bildirimi.

Gerçek API kontrollerinde `Breaking Bad (2008)` TVmaze'den, `Inception (2010)` Wikipedia'dan, `TRT 1 HD / TRT1.tr` IPTV-org'dan eşleşti. Yalnız herkese açık örnek adlar kullanıldı; özel IPTV hesabı kullanılmadı. Bu kontrol eşleşme URL'lerini doğrular; tüm katalog için kapsam garantisi değildir.

## WPF dizi deneyimi

18/18 yeni arayüz/oynatma kontrolü geçti:

- Eksik film/dizi/kanal görsellerinin çözümleyici ve mevcut görsel önbelleği üzerinden ana sayfaya gelmesi; kaynak verilerinin korunması.
- Bölüm panelinin EPG alanını değiştirmesi, doğru sezon/bölüm seçimi ve 900 × 650 pencerede sığması.
- Sezon seçiminin oynatmayı kesmemesi.
- Tam ekran önerisinin erken görünmemesi, son 30 saniyede görünmesi ve geri sarınca gizlenmesi.
- Gerçek öneri düğmesine tıklayarak sonraki bölümü açma, tam ekranı ve önceki bölüm konumunu koruma.
- Sonraki sezona geçiş; son bölümde öneri olmaması; tam ekrandan çıkınca doğru sezonun görünmesi.
- Canlı kanala dönünce EPG panelinin geri gelmesi ve filmlerde bölüm paneli/önerisi gösterilmemesi.

Testlerde üç bölümlük yerel katalog, yerel video ve kontrollü metadata yanıtları kullanıldı. Görsel byte önbelleği yerel örnek görselle hazırlandı; WPF testi dış servislerden afiş indirme hızını ölçmez. Önizlemelerdeki video alanı WPF RenderTargetBitmap ile yakalanamaz; oynatma motorunun durumuyla ayrıca doğrulandı. Panel ve öneri ekran görüntüleri görsel olarak incelendi.

## Önceki özelliklerin kontrolü

Mevcut ana sayfa/afiş kontrolleri, gerçek yerel video oynatma, ses/altyazı, seek, EPG'nin son kanalı takip etmesi, tam ekran panel hizası/favoriler, PVR kaydı ve kaydı yeniden oynatma geçti. Film ve dizi konumunun saklanması ve geri yüklenmesi doğrulandı.

100.500 içerik 6.114 ms içinde yüklendi; arayüz zamanlayıcısındaki en uzun aralık 47,5 ms oldu. Yaklaşık 20 saniyelik yerel canlı geri sarma, canlıya dönüş ve tampon temizliği geçti. 60 saniyelik uzun tampon testi bu turda yeniden çalıştırılmadı. Ölçümler bu test bilgisayarına aittir.

## Dağıtım ve güncelleme kontrolü

Tek dosya 1.6.0 dağıtımı ayrı uygulama/veri klasörüne açılarak 18 yeni WPF kontrolü tekrar çalıştırıldı ve geçti. İzole 1.7.0 deneme paketine güncellemede SHA-256 doğrulaması, eski sürecin kapanmasını bekleme, yeni sürümün etkinleşmesi, eski kısayolun yönlendirilmesi ve kütüphanenin korunması doğrulandı. Gerçek GitHub yayını oluşturulmadı.

Gerçek görsel byte indirmelerinde TVmaze afişi ve Wikimedia film afişi/kanal logosu doğrulandı. Wikimedia kimliksiz isteklere 403 döndürdüğü için görsel istemcisine WX Player User-Agent bilgisi eklendi; bu başlıkla JPEG ve PNG yanıtları HTTP 200 olarak alındı. Harici servisler zaman içinde farklı hız sınırları veya erişim kuralları uygulayabilir.

## Tekrarlama

```powershell
dotnet run --project tests/WXPlayer.Tests -c Release -- artifacts/core-results.json
dotnet run --project tests/WXPlayer.Tests -c Release -- artifacts/public-artwork-results.json --artwork-live
WXPlayer.exe --smoke --experience-160 --media C:\Temp\test.mp4 --data-dir C:\Temp\WXPlayer-160-QA
WXPlayer.exe --smoke --stress --timeshift --media C:\Temp\test.mp4 --data-dir C:\Temp\WXPlayer-Regression-QA
./tools/test-updater.ps1 -AppExe ./artifacts/WXPlayer.exe -OutputPath ./artifacts/updater-unique
```

Güncelleyici betiği 1.7.0 deneme paketi kullanır; gerçek GitHub yayını oluşturmaz. Testler ayrı veri klasörlerinde çalıştırılmalıdır.

Derleme başarılıdır. Önceden restore edilmiş paketlerde NuGet güvenlik verisine erişilemediğini belirten NU1900 uyarısı görüldü; bağımlılık sürümleri değiştirilmedi. Sağlayıcı katalog kapsamı, bağlantı hızı, farklı Stalker varyantları ve gerçek yayın codec çeşitleri bu sonuçların kapsamı dışındadır.
