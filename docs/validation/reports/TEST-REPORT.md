# WX Player 1.5.1 — Test raporu

13 Eylül 2026 · Windows x64 · .NET 10 / WPF / LibVLC.

## Katalog ve veri koruma

45 Core testi geçti. Yeni kontroller; 500T örneğindeki bölüm adının tanınması, S01E02 / S01.E02 / 1x02 / Türkçe sezon-bölüm biçimleri, tek dizi kartı altında sayısal bölüm sıralaması, afiş aktarımı, kaynaklar arası ayrım ve eski kayıtlı kütüphanenin yeniden indirme olmadan dönüştürülmesini kapsar.

Eski bölüm favorileri ve geçmişi dizi kartına taşındı. Favori kaldırılıp kaynak yenilendiğinde işaret tekrar oluşmadı. Film/dizi konumları veritabanı yeniden açıldıktan ve katalog yenilendikten sonra korundu. Geçmiş temizleme ve kaynak kaldırmada ilgili konumlar da kaldırıldı. Favori/binding değişiminde içerik kimliğinin sabit kalması ayrıca doğrulandı.

## Gerçek arayüz ve oynatma

M3U örnek kaynağı ana sayfada filtrelendi; arama metni, dar/geniş menü ve 900 × 650 pencere kontrolleri geçti. Sağlayıcı logo/afişi yerel HTTP sunucusundan indirildi. Afiş yüklenirken üst alanın sabit yükseklikte kalması görsel olarak kontrol edildi.

Üç bölümden oluşan 500T test kaynağı, yalnız bir dizi kartında gösterildi. Gerçek bölüm penceresinden ikinci bölüm seçildi. Yaklaşık 16. saniye kaydedildi, ana sayfada S01/B02 bilgisi oluştu; bölüm penceresi son izlenen bölümü seçti. Yeniden oynatım aynı bölümde kayıtlı noktaya döndü. Film için de 12. saniyede kayıt ve devam etme kontrolü geçti.

Tam ekran yıldızına fare olayı gönderildi; favori veritabanına işlendi, simge güncellendi ve oynatılan içerik değişmedi. İkinci tıklamayla favori kaldırıldı. Tam ekran panellerinin genişlik/hizası, fareyle gösterme ve otomatik gizleme, kategori değişimi, EPG takibi, ses, seek, altyazı, istatistikler, PVR ve kayıt dosyasını yeniden oynatma geçti.

100.500 içerik arayüz testinde 2.495 ms içinde içe aktarıldı; arayüz zamanlayıcısındaki en uzun aralık 42,6 ms idi. Ölçümler bu test bilgisayarına aittir.

WPF görüntüleri GPU video yüzeyini içermez. Video, çözülen kareler ve yerel pencere bağlantısıyla doğrulandı. Gerçek IPTV hesaplarının tüm adlandırma/codec çeşitleri ve tüm ekran kartları test edilmedi; sağlayıcıya ait afiş/metadata eksikliği varsa uygulama bunları uydurmaz. Önceki sürümler izleme dakikası kaydetmediğinden geriye dönük dakika üretilemez.
## Dağıtım paketi doğrulaması

Teslim edilen bağımsız 1.5.1 EXE ayrı çalışma/veri klasörüne açılarak sınandı. Ana sayfa, bölüm seçimi, film/bölüm konumuna dönüş, EPG, tam ekran favori ve PVR kontrolleri geçti. Kısa canlı tampon regresyonunda geri sarma, canlıya dönme ve tampon temizliği geçti; 60 saniyelik uzun süre testi bu turda yeniden çalıştırılmadı.

Aynı EXE'den ayrı 1.6.0 deneme paketine güncelleme testi yapıldı. SHA-256 doğrulaması, önceki sürecin kapanmasını bekleme, yeni sürümün açılması, eski kısayolun yönlendirilmesi ve kütüphanenin korunması geçti. Gerçek GitHub Release yayımlanmadı.
