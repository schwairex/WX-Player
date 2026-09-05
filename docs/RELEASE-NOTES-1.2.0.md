# WX Player 1.2.0

Bu sürümde yayın akışı, oynatma istatistikleri, ayarlar ve uygulama güncellemeleri yenilendi.

- Canlı kanal seçildiğinde EPG otomatik yüklenir. XMLTV EPG Parser & Channel Matcher; kanal kimliği, tvg-name ve rehber adlarını eşleştirir. Sıkıştırılmış XMLTV, Xtream yedek sorguları, önbellek ve elle kanal eşleştirme desteklenir.
- EPG ilerleme çubuğundaki görüntüleme hatası giderildi. Hızlı kanal değişiminde önceki kanalın programları yeni kanala uygulanmaz.
- Oynatıcıya yayın istatistikleri eklendi. **I** tuşuyla codec, çözünürlük, FPS, ses bilgileri ve akış sayaçları açılır.
- Ayarlar üç sekmeyle yeniden tasarlandı. Kaynakları düzenleme/kaldırma, favorileri ve son izlenenleri ayrı temizleme eklendi.
- GitHub Releases tabanlı otomatik güncelleme: açılışta ve 4 saatte bir kontrol, arka planda indirme, SHA-256 doğrulaması ve kullanıcının seçimiyle yeniden başlatma.
- 1.1 sürümündeki Inter font, SVG simgeler, yerel video alanı ve tam ekran iyileştirmeleri korunur.

**1.1 kullanıcıları:** Bu EXE'yi bir kez elle indirip eski uygulamayı kapatarak açın. Eski kısayolunuzu yeni EXE'ye yönlendirin. Sonraki sürümler uygulama içinden alınabilir. Kaynaklarınız ve favorileriniz korunur.

Windows 10/11 x64. EPG için sağlayıcınızın o kanal/gün için rehber verisi sunması gerekir. Testler: 36 altyapı/regresyon testi; gerçek uygulama EPG ve video kontrolleri; izole güncelleme/yeniden başlatma testi.
