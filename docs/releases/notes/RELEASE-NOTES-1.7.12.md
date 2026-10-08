# WXPlayer 1.7.12

4 Ekim 2026

- Yayın istatistikleri: sabit içerik başlığı ve maskelenmiş adres, durum/tampon rozetleri, üç nötr ölçüm kutusu; altında Akış ölçümleri, Oynatma ve motor, Parçalar sırası. Veriler, birimler, ondalık basamaklar ve saniyelik yenileme korunur.
- Durum ve video çıkışı yalnız görüntüde Türkçeleştirilir. Seçili ses parçası küçük bir rozetle işaretlenir. Bildirilmeyen değerler sönük, kayıp/bozulma sayaçları sıfırdan büyükse kırmızı gösterilir.
- Mini oynatıcı: 520×400 varsayılan boyut, 52 px başlık ve 56 px alt şerit; temiz görüntü adı ve kalite rozeti, iki ikonlu dönüş/kapat düğmesi, beyaz oynat düğmesi, sade süre/canlı rozeti ve etkileşimsiz ilerleme çizgisi.
- Ses ve altyazılar: ortak Ayarlar stilinden türetilen ComboBox'lar, bölüm rozetleri, tanınan Track N - [Language] adları için Türkçe görüntü dönüştürücüsü, ham ad araç ipuçları, hafif altyazı ekleme düğmesi ve biçim etiketleri. Durum mesajı kutusuz ve ekran okuyucuya uygun.
- Açık ses/altyazı seçim menüsünde Esc yalnızca menüyü kapatır; menü kapalıyken mevcut pencere kapatma davranışı sürer.

Yalnız görsel katman değişti. StatisticsWindow 670×730, TracksWindow 700×680 ölçü istekleri ve PremiumWindow başlık/alt başlık/ikon düzeni korunur. Mini oynatıcı üstte kalır; aynı yerel video HWND'si aktarılır. Şeffaf mini pencere, videoya WPF bindirmesi veya yeni DWM kodu eklenmedi.

Referanslardaki önizleme düğmeleri/paneli, sahte kanallar ve ölçümler, kopyala/dışa aktar gibi yeni işlemler uygulamaya eklenmedi. İlerleme çizgisi atlama yapmaz; mini oynatıcıya ses kaydırıcısı, otomatik gizleme veya yeni komut eklenmedi.

Ses/altyazı listesinin saniyelik SelectedValue atama politikası değiştirilmedi. Oynatma, kaynaklar, EPG, kayıt, geri sarma, Discord, ayarlar, veri ve önbellek aynı kalır. Manrope, ortak koyu yüzeyler, Ideal/ClearType, klavye odağı ve WPF DPI mekanizması kullanılır; metinlerde blur/efekt/bitmap cache/opacity mask yoktur.

Dağıtım: WXPlayer-1.7.12.exe, WXPlayer-1.7.12-portable.zip, WXPlayer-1.7.12-source.zip.
Doğrulama: [TEST-REPORT-1.7.12.md](../../validation/reports/TEST-REPORT-1.7.12.md).
