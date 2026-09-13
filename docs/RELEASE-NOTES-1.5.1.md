# WX Player 1.5.1

- Ana sayfa sıraları **Filmler** ve **Diziler** olarak adlandırıldı. M3U içeriğinde bölüm/sezon işaretleri dosya uzantısından önce değerlendirilir; bölümler film sırasına girmez.
- M3U dizileri kaynak ve dizi adına göre tek kartta toplanır. Sezonlar aynı kart altında kalır. Dizi kartına tıklanınca sezon ve bölüm seçimi açılır. Xtream bölüm bilgileri de bu pencereye taşındı.
- 1.5'te saklanmış M3U kütüphaneleri ilk açılışta arka planda dönüştürülür. Kaynağı yeniden eklemek gerekmez. Bölümlere verilmiş eski favori ve geçmiş işaretleri dizi kartına aktarılır.
- Büyük üst alan her uygulama oturumunda film/dizi kataloğundan bir öneri seçer. Alternatif varsa önceki açılışın önerisi tekrarlanmaz. Öneri, son izlenen içerik listesinden seçilmez; varsa sağlayıcı afişi önceliklendirilir.
- Afiş, başlık ve tür etiketleri daha düzenli kartlarda gösterilir. Afişlerin oranı korunur; kanal logoları ayrı, daha yatay kartlarda yer alır. Afişin yüklenmesi üst alanı büyütmez.
- Tam ekran yıldızı, oynatmayı değiştirmeden favori ekler/kaldırır ve simgesini anında günceller. Klavyeyle odaklanılan düğmeler Space/Enter ile kullanılabilir.
- Filmlerin dakikası ve dizilerin son izlenen bölümü/dakikası saklanır. Ana sayfa, içerik listesi ve bölüm penceresinde ilerleme gösterilir. Yeniden oynatım desteklenen akışta kayıtlı noktadan devam eder. Bölüm penceresinde **Baştan oynat** seçeneği de vardır.

## İzleme bilgisi

Oynatım konumu yaklaşık 5 saniyede bir; duraklatma, içerik değişimi, ana sayfaya dönüş ve normal kapanışta kaydedilir. Tamamlanan içerik **İzlendi** olarak işaretlenir. Eski sürümler dakika bilgisi kaydetmediğinden, önceki izlemelerin dakikası sonradan oluşturulamaz; bu bilgi 1.5.1 ile birikmeye başlar. Ayarlar → Son izlenenleri temizle, kayıtlı bölüm/dakika bilgisini de temizler.

## Kaynak bilgisi

M3U'da `S01E02`, `S01.E02`, `S01 B02`, `1x02`, `1. Sezon 2. Bölüm` gibi açık işaretler tanınır. Aynı isim, aynı kaynağın içinde gruplanır; farklı kaynaklar birleştirilmez. Sezon/bölüm bilgisi olmayan belirsiz adlar için bölüm numarası uydurulmaz. Xtream katalog türleri ve sağlayıcının sunduğu bölüm bilgileri kullanılır.

Afiş ve logolar kaynağın `tvg-logo`, cover/poster veya Xtream katalog alanlarından gelir. Görsel sunulmamış ya da yüklenememişse başlık kapağı gösterilir. Harici bir film veritabanından izinsiz veya eşleşmesi belirsiz afiş atanmaz.

Güncelleme için GitHub'da **v1.5.1** kararlı Release oluşturup teslim edilen EXE ve SHA256SUMS dosyasını Assets alanına ekleyin.