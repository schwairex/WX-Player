# WX Player 1.3 — Test raporu

5 Eylül 2026 · Windows x64 · .NET 10 / WPF / LibVLC 3.0.23.

## Arayüz ve oynatma

Gerçek WPF uygulaması yerel bir video ve XMLTV test kaynağıyla çalıştırıldı. Geniş pencere, 940 piksel dar pencere ve 900 × 650 pencere düzenleri incelendi. Arama metninin görünürlüğü, aramanın listeyi filtrelemesi, arama/kategori/listenin aynı panelde olması, sayaçların sol menüye taşınması ve küçük pencerede rehberin görünür kalması doğrulandı.

Timeline ve ses kaydırıcılarında 36 piksel yüksekliğin farklı noktalarında isabet alanı denetlendi. Timeline'ın %50 konumuna gitmesi ve sesin %60 değerini motora uygulaması doğrulandı. Motor ses değerini asenkron güncellediği için sonuç kısa bir bekleme sonrası okundu.

Ses/altyazı penceresi gerçek parça bilgileriyle açıldı; seçim metinleri görsel olarak incelendi. Arama, EPG, ses/altyazı ve küçük pencere önizlemeleri kontrol edildi. Küçük pencerede kırpılan karşılama metni sade bir düzenle düzeltildi.

EPG otomatik yüklendi; hızlı kanal değişimi sonrasında yalnız son seçilen kanalın programları gösterildi. İstatistiklerde gerçek video ve ses bilgileri alındı. Normal video alanında ayrı karartma katmanı olmadığı, tam ekranın monitörü doldurduğu, aynı yerel video penceresini koruduğu, kontrollerin otomatik gizlenip fare hareketiyle açıldığı ve önceki pencereye döndüğü doğrulandı.

İleri sarma, duraklatma, harici altyazı ekleme, TS kaydı ve kaydı yeniden oynatma geçti. Örnek kaydın boyutu 4.993.468 bayt; yeniden oynatımda 60 video karesi çözüldü.

## Performans

100.500 içerik 1.726 ms içinde yerel kütüphaneye aktarıldı. Yükleme sırasında 68 arayüz zamanlayıcı vuruşu ve en fazla 45,4 ms aralık ölçüldü. Bu ölçümler test bilgisayarına aittir; internet indirme süresi dahil değildir.

## Sınırlar

Arayüz görüntüleri WPF çiziminden alındı; bu oturumda GPU video yüzeyi ekran görüntüsüne alınamadı. Oynatma çözülen kareler, motor değerleri ve pencere geometrisiyle doğrulandı. Yerel test kaynakları kullanıldı; gerçek kullanıcı hesapları veya kütüphanesi değiştirilmedi. EPG için sağlayıcının ilgili kanala program verisi sunması gerekir. Canlı yayında timeline yalnız seek destekleyen kaynaklarda etkinleşir.
## Son dağıtım ve regresyon kontrolleri

- 36/36 Core testi geçti: ayrıştırıcılar, veri kalıcılığı, EPG eşleştirme, iptal/geri alma ve güncelleyici doğrulama senaryoları.
- Bağımsız tek EXE çıkarılıp gerçek medya ile tekrar çalıştırıldı. 100.500 içerik 1.737 ms, en uzun arayüz zamanlayıcı aralığı 44,1 ms; oynatma, ses/timeline, EPG, tam ekran, altyazı ve kayıt kontrolleri geçti.
- Gerçek güncelleme denetleyicisi, ayrı klasördeki 1.4.0 test paketini indirdi/doğruladı; eski sürecin kapanmasını bekledi, yeni sürümü etkinleştirdi. Eski EXE tekrar açıldığında yeni sürüme yönlendi; kütüphane korundu. GitHub'a gerçek test sürümü yayımlanmadı.
- Kısıtlı ağdaki ilk paketlemede NuGet güvenlik sorgusu uyarı verdi. Ağ erişimli son restore kontrolü hatasız/uyarısız tamamlandı.
- Dağıtım başlatıcısının Windows dosya özelliklerine ürün adı ve 1.3.0 sürüm bilgisi eklendi.