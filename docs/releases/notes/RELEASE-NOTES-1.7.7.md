# WX Player 1.7.7

Filmler ve Diziler sayfaları, kullanıcının `wx-player-filmler-diziler.html` referansındaki görünümün mevcut WPF uygulamasına aktarılmasıyla yenilendi. Oynatıcı ve kütüphane mantığı değişmedi; yeni bağımlılık eklenmedi.

- Sayfaya özel `--bg`, `--p1`, `--p2`, `--p3`, `--ink`, `--mut`, `--acc`, `--line` kaynakları. Tam ekran ve diğer sayfaların aynı isimli kaynakları değiştirilmedi.
- Gömülü Manrope, `clamp(14px, .5vw + 8px, 20px)` ölçeklemesi ve referansın rem ölçüleri.
- Sabit Filmler/Diziler başlığı, mevcut aramaya bağlı arama kutusu, mevcut kaynak seçici ve kaynak işlemleri.
- Gerçek katalogdan hero; kayıtlı izleme konumu varsa ilerleme ve Devam et, yoksa mevcut Oynat/Bölümleri gör eylemi. Diziye basıldığında mevcut bölüm seçici açılır.
- 11rem genişliğinde 2:3 afişler, başlık/metadata, gerçek favori yıldızı ve varsa kayıtlı ilerleme. Görsel alınamadığında aynı boyutlu `np` kartı.
- Referansın kart hareketi ve gölge süreleri, odak halkaları, isimlendirilmiş ikon düğmeleri. Azaltılmış Windows animasyon ayarı gözetilir.
- Kategori rafları ve mevcut asenkron sayfalama korunur. Kategori kısayolları rafı görünür alana taşır; Tümü üst tarafa döner.

## Karşılığı olmadığı için eklenmeyen referans öğeleri

- Yeni eklenen filtresi ve YENİ rozeti: mevcut katalog verisinde güvenilir eklenme tarihi yok.
- Bu sayfaya özel Favoriler filtresi, A–Z/yıl sıralaması, raf/ızgara seçimi ve Tümünü gör ile yeni grid görünümü: mevcut katalog akışında karşılıkları yok. Genel Favoriler sayfası kullanılmaya devam eder.
- Yeni detay/özet penceresi, sezon sayısı ve uydurma yıl/runtime/genre alanları. Kaynak modelinde bulunmayan bilgiler üretilmez; mevcut bölüm seçimi ve oynatma davranışı korunur.
- HTML'deki sahte oynatım bilgili mini kartın pause/fullscreen düğmeleri. Mevcut ayrı mini oynatıcı aynen kalır; katalogdaki İzlemeye dön kısayolu mevcut dönüş callback'ini kullanır.

## Referansta bulunmasa da korunan işlevler

Kütüphane özeti, kaynak seçenekleri, kayıtlar, ayrı Filmler/Diziler navigasyonu, kaynak/öğe toplamı, durum ve iptal satırı, mevcut dizi bölüm seçici ve mini oynatıcı korunur. Sidebar, Home, canlı TV, EPG ve oynatıcı bu sürümde yeniden tasarlanmaz.

## Görsel doğrulamanın sınırı

Çalışan WPF ekranı 1440 ve 900 DIP pencerelerde incelendi; kaynak renk/ölçüleri karşılaştırıldı. Yerel HTML'yi tarayıcıda açma girişimi URL güvenlik politikası tarafından reddedildi. Kısıtlama aşılmadı; tarayıcı–WPF piksel eşitliği doğrulanmış değildir. WPF'nin CSS `backdrop-filter` ve metin letter-spacing karşılıkları yoktur; yazı rasterizasyonu ve gölge çizimi de tarayıcıyla aynı değildir. HTML'nin sahte posterleri yerine gerçek kaynak görselleri kullanılır.

Görünüm testlerindeki örnek içerikler yalnız `--smoke` ile ayrı veri klasöründe hazırlanır; dağıtımın normal açılışında örnek kütüphane oluşturulmaz.
