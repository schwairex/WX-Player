# WX Player — GitHub Releases ile güncelleme yayımlama

Hedef depo: https://github.com/schwairex/WX-Player

## 1.2'yi yayımlama

1. Kaynak ZIP içindeki `WXPlayer` klasörünün **içeriğini** deponun köküne yükleyin. `.github/workflows/windows.yml` ve `Directory.Build.props` kökte olmalı.
2. GitHub **Releases → Draft a new release** yolundan `v1.2.0` etiketiyle kararlı bir sürüm hazırlayın.
3. Teslim edilen **WXPlayer-1.2.0.exe** dosyasını sürümün Assets alanına ekleyin. Dosya adı `WXPlayer.exe` olarak da kullanılabilir. Aynı sürüme yalnız **bir** WXPlayer EXE ekleyin. İsteğe bağlı kaynak/portable ZIP eklenebilir. Taşınabilir ZIP içindeki küçük EXE güncelleme paketi değildir; tek dosya dağıtım EXE'sini ekleyin.
4. GitHub'ın asset digest alanı SHA-256 sağlıyorsa başka manifest gerekmez. Ek güvence ve eski API uyumluluğu için teslim edilen `SHA256SUMS-1.2.0.txt` dosyasını da ekleyin. EXE adını değiştirirseniz bu dosyadaki ad da aynı olmalı. “Pre-release” seçmeden **Publish release** yapın.

Kaynak kodunu yüklemek veya commit oluşturmak tek başına güncelleme yayımlamaz. Taslak ve ön sürümler otomatik kurulmaz. Kontrol noktası: https://api.github.com/repos/schwairex/WX-Player/releases/latest

## Sonraki sürümleri otomatik paketleme

1. `Directory.Build.props` içindeki `<Version>` değerini örneğin `1.3.0` yapın. Sürüm numarası `major.minor.patch` biçiminde olmalı. Başlatıcı sürümü bu dosyadan otomatik üretilir.
2. Değişiklikleri gönderin; aynı commit için `v1.3.0` Git etiketini oluşturup gönderin.
3. Dahil edilen GitHub Actions akışı testleri çalıştırır, Windows x64 EXE ve ZIP oluşturur, SHA-256 dosyasını üretir ve bu etikete ait GitHub Release'e ekler. Etiket/uygulama sürümü uyuşmazsa yayın durdurulur. Depoda Actions çalıştırılabilmeli; release işi kendi `GITHUB_TOKEN` yetkisiyle `contents: write` kullanır. Kullanıcı uygulamasına token eklemeyin.

Manuel paketleme: Windows + .NET 10 SDK ortamında `./tools/build.ps1`. Yeni ve boş bir çıktı klasörü seçin. Derleme çıktıları: `WXPlayer.exe`, `WXPlayer-win-x64.zip`, `SHA256SUMS.txt`.

## Kullanıcı deneyimi

* 1.1'den 1.2'ye ilk geçiş elle yapılır. 1.2 sonrasında uygulama açıkken 4 saatte bir, ayrıca açılışta kontrol yapılır.
* Yeni EXE arka planda indirilir; indirme durumu Ayarlar → Güncellemeler'de görünür. Ağ kesilirse mevcut uygulama çalışmayı sürdürür ve sonraki kontrolde indirme yeniden denenir.
* Yalnız doğru depoya ait HTTPS release asset'i kabul edilir. Boyut ve SHA-256 eşleşmeyen dosya çalıştırılmaz. İndirme kesilirse geçici dosya temizlenir; uygulama sürümü değişmez.
* İndirme bittiğinde “Yeni bir sürüm mevcut, güncellemek için uygulamayı yeniden başlatın” penceresi açılır. Kullanıcı “Daha sonra” ya da “Güncelle ve yeniden başlat” seçer. Devam eden kayıt önce durdurulmalıdır.
* Yeni sürüm ilklenmeden etkin sürüm işaretçisi değiştirilmez. Kaynaklar, favoriler, geçmiş, EPG ve ayarlar aynı veri klasöründe kalır. Veritabanı şeması değişen gelecek sürümlerde ayrıca geriye uyumlu migration yazılmalıdır.

## Güncelleyici testi

`./tools/test-updater.ps1 -AppExe ./artifacts/WXPlayer.exe -OutputPath ./artifacts/updater-test-unique`

Bu test kendi geçici 1.3.0 fixture EXE'sini üretir; gerçek GitHub'da yayın oluşturmaz. 1.2'nin indirme/doğrulama/yeniden başlatma kodunu çalıştırır, eski süreç kapanmadan yenisinin başlamadığını ve eski EXE'nin etkin yeni sürüme yönlendiğini kontrol eder. Test verileri ayrı klasörde tutulur. Fixture EXE'yi Releases'e yüklemeyin.
