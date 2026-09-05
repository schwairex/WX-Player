# WX Player 1.0 — Teslimat doğrulaması

Tarih: 5 Eylül 2026. Platform: Windows x64. Test edilen dosya: dağıtılan tek dosya `WXPlayer.exe`; ayrıca aynı sürümün WPF derlemesi ve Core test programı.

## Sonuç

**20/20 altyapı testi geçti. Tek EXE açıldı, kendi paketini doğrulayıp çıkardı ve uygulamayı başarıyla çalıştırdı.** Başlatıcı ve uygulama boşluk içeren ayrı test klasörlerinde doğrulandı. Paket kaynak ve çalışma zamanı dosyalarını içerir; test için sistemde kurulu VLC kullanılmadı.

| Ölçüm / işlem | Sonuç |
|---|---|
| 100.500 içerik — Core testinde SQLite içe aktarma | 1.426 ms |
| 100.500 içerik — paketlenmiş WPF uygulamasında | 1.645 ms |
| İçe aktarma sırasında arayüz zamanlayıcısı | 65 kez çalıştı |
| Ölçülen en uzun UI zamanlayıcı aralığı | 36,4 ms |
| Kategori + metin sorgusu ve sayfalama testinin tamamı | 96 ms |
| Gerçek video oynatma | Başarılı; 117 video karesi çözüldü |
| Direct3D / GPU | WPF testinde D3D11VA; NVIDIA GeForce RTX 5070 kullanımı LibVLC çıktısında doğrulandı |
| Seek / duraklatma | Başarılı |
| Ses parçaları | Örnek kaynağın 1 ses parçası bulundu |
| Harici SRT altyazı ekleme | Başarılı |
| PVR TS dosyası | 4.993.468 bayt oluşturuldu |
| Kayıt dosyasını yeniden oynatma | Başarılı; 60 video karesi çözüldü |
| Geniş ve kompakt arayüz | Gerçek WPF görselleri oluşturuldu ve incelendi |
| Derleme | 0 hata, 0 uyarı |

Ölçümler bu bilgisayardaki kontrollü yerel testlere aittir. İnternet indirme süresi, sağlayıcı gecikmesi ve çok farklı donanımlar için hız garantisi değildir. UI testi, işlem sırasında zamanlayıcının çalışmasını ölçer; her olası kullanıcı etkileşimini test eden bir uçtan uca otomasyon paketi değildir.

## Altyapı test kapsamı

* Türkçe arama normalizasyonu; M3U meta verisi, tırnaklı virgül, UTF-8/BOM, göreli adres, kullanıcı aracısı ve referrer.
* TXT ve protokol süzme; HLS master/media manifestinin tek içerik olarak ayrıştırılması.
* Xtream bağlantısından kullanıcı adı/şifre ayrıştırma; DPAPI koruma ve geri okuma.
* XMLTV saat dilimi ve program verisi; harici XML varlıklarının engellenmesi.
* 100.500 satır yükleme, sayfa/kategori/metin sorguları, favori/geçmiş kalıcılığı.
* İptal ve boş kaynak durumunda mevcut kütüphanenin işlem geri almayla korunması.
* Aramada SQL jokerlerinin düz metin olarak işlenmesi; önceden iptal edilen sorgu.
* XMLTV-kanaI eşleşmesi, Catch-Up zaman aralığı ve URL üretimi.
* Kontrollü HTTP yanıtlarıyla Xtream hesap, katalog, bölüm, kısa EPG ve yayın çözümleme.
* Kontrollü HTTP yanıtlarıyla Stalker handshake, cookie/token, sayfalama ve create_link.
* Kaynak silme ve ilişkili verilerin temizlenmesi.

## Henüz gerçek ortamda doğrulanmayanlar

Gerçek Xtream veya Stalker abonelik hesabı sağlanmadı. Bu nedenle sağlayıcı oturumu, tüm portal sürümleri, arşiv sunucusundan gerçek Catch-Up oynatma ve sağlayıcının ikinci bağlantıyla PVR davranışı canlı abonelikle doğrulanmadı. DirectShow kamera/yakalama aygıtı testi yapılmadı. Çoklu ses ve altyazı seçicileri uygulanmış olsa da test medyası tek ses parçası içeriyordu. DRM, kayıt zamanlayıcısı, çevrimdışı arka plan kaydı ve özel Stalker arşiv protokolü bu sürümün kapsamı dışında.

EXE kod imzası içermez. Dosya bütünlüğü teslimattaki SHA-256 listesiyle kontrol edilebilir. İlk çalıştırma kullanıcı klasörüne çıkarma yapar; Windows'a servis kurmaz.
