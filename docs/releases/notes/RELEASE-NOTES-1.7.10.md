# WX Player 1.7.10

3 Ekim 2026

Canlı TV görünümü `wx-player-canli-tv-v3.html` referansının WPF karşılığına taşındı. Gerçek kaynak, kanal, logo ve program verileri kullanılır; örnek HTML verisi normal uygulamaya eklenmez.

- Tam yükseklikte kanal paneli, kaynak seçici ve sade başlık.
- Manrope, koyu yüzeyler, yeşil vurgu, okunaklı kanal satırları ve hover/odak/favori durumları.
- Video üzerinde özgün oynatma kontrolleri; gerçek tampon bilgisi ve canlıya dönüş düğmesi mevcut koşullarıyla çalışır.
- Yatay, sanallaştırılmış EPG kartları; geçmiş/şimdiki/sonraki programların mevcut verileri, gün gezinmesi ve rehber boyutlandırma korunur.
- Kayıt, PiP, ses/altyazı, görüntü oranı ve istatistik düğmeleri oynatıcı dişli menüsüne taşındı. Kütüphane özeti ve kaynak seçenekleri başlıkta görünür kaldı.
- Arama, kategori, favori, sayfalama, kaynak ekleme/yenileme, EPG eşleştirme/yenileme/genişletme, sağlık göstergesi ve iptal kontrolleri korunur.

## Referansa uyarlama sınırları

WPF `HwndHost` üzerinde sıradan XAML video üzerine çizilemediğinden, yalnız Canlı TV kontrol görselleri sahibi ana pencere olan şeffaf bir pencerede yer alır. Video HWND'si, LibVLC ve tam ekran sahipliği değiştirilmez. Yerleşim fiziksel piksel koordinatlarıyla videoya uyar; sayfa değişiminde ve tam ekrana geçişte özgün kontroller geri verilir.

- CSS değişkenleri `LiveTheme.xaml` içindeki yerel `LiveTokens` sözlüğünde korunur; diğer sayfaların aynı isimli token'larını geçersiz kılmaz.
- `rem/clamp` değerleri 16 DIP tabanı ve mevcut pencere/DPI ölçeğiyle karşılanır.
- Backdrop blur yerine yarı saydam koyu fırça kullanılır. Metin kapsayıcılarında OpacityMask, Effect veya BitmapCache yoktur.
- Referanstaki uydurma akış/bitrate/çözünürlük değerleri, kalite çipleri ve kategori başına adetler eklenmedi. Mevcut istatistik penceresi gerçek yayın bilgisini sunmaya devam eder.
- “Şimdiye git” için mevcut bir komut bulunmadığından yeni komut eklenmedi. Mevcut rehber yükleme akışının şimdiki programa kaydırması korunur.
- Normal pencerede referansın yeni otomatik gizleme/kayıt/toast zamanlayıcıları eklenmedi. Mevcut tam ekran 3,5 saniyelik gizleme davranışı aynı kaldı.
- Ses çubuğu erişilebilirliğini korumak için görünür tutuldu; kategori adları ve ham kanal isimleri değiştirilmedi.
- Mevcut kanal sayfalama ve alt durum/iptal satırı referansta bulunmasa da korundu.

Oynatma, sağlayıcı, arama/kategori sorgusu, EPG yükleme, geri sarma, veritabanı, kayıt ve Discord kodu değiştirilmedi. [Doğrulama raporu](../../validation/reports/TEST-REPORT-1.7.10.md).
