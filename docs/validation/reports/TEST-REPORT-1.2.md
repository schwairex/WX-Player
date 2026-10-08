# WX Player 1.2 — Test raporu

Tarih: 5 Eylül 2026 · Windows x64 · .NET 10 / WPF / LibVLC 3.0.23.

## Sonuçlar

* 36 Core testi geçti: 20 mevcut altyapı testi ve 16 yeni EPG/güncelleme/veri temizleme regresyonu.
* Gerçek WPF uygulamasında EPG'nin otomatik indirilmesi, kanal adıyla eşleşmesi ve hızlı kanal değişimi sonrasında yalnız son kanalın programlarının görünmesi geçti. Test programları yerel XMLTV örneğidir; abonelik verisi değildir.
* EPG ilerleme çizgisinde `Progress` salt okunur özelliğine yapılan hatalı TwoWay bağlama gerçek arayüz testinde yakalandı ve OneWay olarak düzeltildi.
* 100.500 içerik UI testinde 1.778 ms'de aktarıldı; işlem sırasında 70 arayüz zamanlayıcı vuruşu, en uzun 37,2 ms aralık ölçüldü. İnternet indirme süresi dahil değildir; farklı sistemler için hız garantisi değildir.
* Yerel videodan h264, 854×480, 24 FPS video bilgisi ve ses örnekleme bilgisi okundu. İstatistik penceresi, üç ayar sekmesi, güncelleme bildirimi ve EPG arayüzü PNG önizlemeleriyle incelendi.
* Tam ekran monitör sınırları, aynı yerel video penceresini koruma, kontrol gizleme/gösterme, sığdır/doldur, normal/büyütülmüş pencereye dönüş kontrolleri geçti.
* Oynatma, ileri sarma, duraklatma, ses parçaları, harici altyazı, TS kayıt ve kaydı tekrar çözme geçti. Örnek kayıt 4.993.468 bayt.

## EPG ve veri testleri

Standart XMLTV DOCTYPE dış DTD indirmeden kabul edilir; harici entity kullanılmaz. Dakika hassasiyetindeki tarihler, UTC/offset, eksik bitişin aynı kanalın sonraki başlangıcından türetilmesi; `tvg-name`, göreli rehber URL'si, gzip imzası, HTTP tarafından açılmış `.gz` yanıtı test edildi.

Kanal eşleştirme kimliği önce kullanır; Türkçe ve kalite eki normalizasyonu ile adları eşleştirir. Aynı kaynaktaki belirsiz eşleşmede yanlış kanal seçilmediği ve elle seçimle düzeltilebildiği test edildi. Kaynaklar birbirinin rehberine karışmaz. Hatalı XMLTV'de programlar, kanal eşleştirme dizini ve önbellek tarihi korunur. Eşzamanlı 12 rehber isteği tek indirme kullandı. Xtream kısa EPG, alternatif uç nokta, sunucu saat dilimi ve M3U get.php kimliği ayrıştırması kontrollü HTTP yanıtlarıyla doğrulandı.

Önceki items şeması üzerine ek sütun migrasyonu; kaynak/favori/geçmişin korunması; favori ve geçmişin ayrı temizlenmesi; tüm kaynakları kaldırma kapsamı test edildi. Gerçek kullanıcı kütüphanesi testte kullanılmadı.

## Otomatik güncelleme

Yeni/eski sürüm karşılaştırması, taslak/ön sürüm eleme, yanlış depo adresini reddetme, dosya boyutu, SHA-256 asset digest ve SHA256SUMS yedeği test edildi. Bozuk, eksik ve iptal edilmiş indirmeler etkin sürümü değiştirmedi ve geçici dosyayı temizledi. Değiştirilmiş yerel EXE etkinleştirilmedi. Ağ/404/rate-limit durumları uygulama sürümünü değiştirmedi.

`tools/test-updater.ps1` gerçek UpdateController ve Launcher koduyla izole bir 1.3.0 test EXE'sini indirdi, doğruladı, eski sürecin kapanmasını bekledi ve yeni sürümü açtı. Yeni sürüm etkinleştikten sonra eski EXE yeniden çalıştırılarak yeni sürüme yönlendirme kontrol edildi. Kütüphane dosyası korundu. Gerçek depoya test yayını gönderilmedi; gelecekteki gerçek bir GitHub Release'in ağ üzerinden uçtan uca dağıtımı henüz gerçekleşmedi.

## Kapsam sınırları

Gerçek Xtream/M3U abonelik rehberi, her sağlayıcı formatı ve tüm Windows/GPU/çoklu monitör birleşimleri test edilmedi. Sağlayıcının program yayımlamadığı kanal/gün için EPG üretilemez. Çok anlamlı kanal adları elle eşleştirme gerektirebilir. Her kaynak için tek keşfedilen XMLTV adresi kullanılır. XMLTV'de final programın bitişi yoksa süre uydurulmaz.

GPU yüzeyini içeren gerçek pencere ekran görüntüsü bu izole oturumda alınamadı; video kontrolleri çözülen kareler, yerel video bağlantısı ve pencere geometrisiyle doğrulandı. WPF arayüz önizlemeleri incelendi. İstatistiklerde motorun bildirmediği kapsayıcı/piksel formatı ya da gerçek GPU kullanım yüzdesi iddia edilmez.

## Son teslim paketi doğrulaması

Son tek dosya EXE ile bütün UI/medya testleri tekrar geçti: 100.500 içerik 1.599 ms, 63 UI zamanlayıcı vuruşu, en uzun 42 ms aralık. EPG otomatik yükleme ve son kanala bağlılık; video/ses istatistikleri; tam ekran ve kayıt kontrolleri başarılı.

Son EXE üzerinde güncelleyici entegrasyonu da tekrar geçti: doğrulanan 1.3.0 yerel test paketi eski sürecin kapanmasını bekledi; yeni sürüm etkinleştirildi; aynı eski EXE yeniden açıldığında yeni sürüme yönlendi; kütüphane korundu. Başlatıcıda Windows ortamında Path/PATH adlarının birlikte bulunması halinde oluşabilen .NET Framework sözlük hatası giderildi. Teslim paketi bu düzeltmeyi içerir.
