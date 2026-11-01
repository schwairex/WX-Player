# WXPlayer 1.7.15 doğrulama

30 Ekim 2026 · Windows x64 · .NET 10 WPF
Dal: `film-izleme-1.7.15`; başlangıç: `a348af8` (1.7.14). Depo: `E:\WX Player`.
QA: `E:\Codex\2026-09-05\bu-klas-r-i-indekileri-analiz\work\qa\v1715-20261030`.
Ayrı --data-dir klasörleri, yerel gerçek video ve izole kütüphane kullanıldı.

| Doğrulama | Sonuç |
| --- | --- |
| Release build | 0 hata / 0 uyarı (`review-green-build.log`) |
| Çekirdek regresyon | 78/78; paket build betiği de 78/78 |
| Film izleme | suite-film: success=true; kategori, arama, boş sonuç, 150'lik sayfalama, favori/store, mevcut önceki/sonraki, Enter ve odak koşullu klavye |
| Film görseli | Gerçek afiş, afişsiz/hatalı afiş yedeği, başlık/yıl/not, oynayan kimliği, özetin olmaması, gerçek toplam, bağlanma sunumu |
| Ölçüler | 1920×1080, 1366×768, 900×700 pencere; dar çekmece HWND üstünü kapatmıyor; F8 açık/kapalı; doğru fiziksel video sınırları |
| Klavye | Bölmelerde oklar seek/ses tetiklemiyor. Video/oynatıcıda seek/ses/M/Space/F/PgUp/PgDn mevcut davranışı sürdürüyor |
| Gizleme | Aynı 3 saniyelik timer (son ölçüm 3115 ms); ilk hareket yalnız gösteriyor, açık menü/duraklatma koruması. Eski embedded1714 smoke hover/klavye/slider/mini korumalarını da geçti |
| Geçiş | Tam ekran, katalog, mini, Canlı TV, Diziler; aynı HWND ve devam eden oynatma |
| Canlı TV/Diziler | Gerçek eski a348af8 kaynak kopyasının aynı fixture'ı ile geometri/stil eşitliği; yan yana görseller |
| Mevcut UI | live/catalog/experience/home/comfort/utility/settings/full: tamamı success=true (`ui-suite.json`) |
| Korunan kod | 33/33 SHA256 aynı; MainWindow.xaml.cs yalnız onaylı TryFilmPaneKey çağrısı dışında bütünüyle aynı |
| Bağımsız inceleme | 2 önemli bulgu RED→GREEN, kritik veya ertelenen küçük bulgu yok (`review-summary.md`) |

İlk sandbox regresyon koşusunda 74/78: I/O erişim reddi ve disk fixture hataları. Aynı testler normal yerel ortamda ve paket build betiğinde 78/78 geçti. VLC'nin normal stderr günlüğü PowerShell 5 test betiğinde hata sayılıyordu; çalıştırıcı düzeltildi, başarı JSON'dan ayrıca denetlendi.

## Değişen dosyalar ve bileşenler

- LibraryPanels.xaml: benzersiz Library anahtarları; LiveTheme fırçaları, LiveButton/LiveMenuAction/LiveFocus/LiveSlider/LiveChannelRow ve CatalogPillBorder yeniden kullanılır.
- LibraryCategoryPane.cs: aynı CategoryPicker kaynak string'leri ve seçimi; ortak kategori görünümü, sorgu/sıralama yok. SelectedItem bağı ItemsSource'tan önce çözülür; çıkışta seçim korunur.
- LibraryDisplayConverters.cs: yalnız görüntü (başlık/yıl/not/kategori/grup/süre), oynayan kimliği ve attached sunum özellikleri; veri değişmez.
- LibraryPosterPresentation.cs: aynı ChannelLogo sonucunu gözler; yeni indirme/cache yok, unload abonelik temizliği var.
- MainWindow.FilmAppearance.cs: yalnız Film izleme sırasında geri alınabilir görsel scope; aynı kontrol nesneleri ve mevcut olaylar. Başlıktaki kütüphane/kaynak seçenekleri mevcut oynatıcı menüsüne taşındı.
- MainWindow.xaml: film satırı şablonu; MainWindow.LiveAppearance.cs: film scope kancaları; MainWindow.xaml.cs: yalnız odak koşullu sunum çağrısı.
- FilmWatching1715Smoke.cs ve SmokeTest.cs: açıkça çağrılan izole QA; Directory.Build.props: 1.7.15; sürüm notları/test raporu/geçmiş dizini güncellendi. README değişmedi.

PlaybackEngine, NativeVideoHost, LiveBuffer, provider/EPG/veritabanı/model, Catalog/Series mantığı, eski oynatma/arama/kategori/sayfalama/favori handler gövdeleri, afiş yükleme/önbellek ve Discord yapılandırması aynı.

## Referans farkları, eklenmeyen öğeler ve kararlar

- Özet satırı ve geleceğe dönük hazırlık, kategori sayıları, örnek veriler, mockup Önizleme/Durum paneli yok.
- Önceki Canlı TV 3-bölmeli HTML uygulanmamıştı ve bulunmadı. Yeni Library kategori/liste kaynakları tek tanımdır; Canlı TV/Diziler'e bu sürümde uygulanmaz.
- 16 DIP/rem karşılığı, Manrope, mevcut DPI/rem ve F8 sistemi korunur. Yeni stiller yalnız eksik görseller için; diğer sayfalara global stil uygulanmaz.
- Bilgi kartı gerçek oynayan filme bağlıdır. Sonraki ad zaten sayfada biliniyorsa aynı NextChannel_Click kullanılır; sayfa sınırında yeni sorgu yok, ad bilinmiyorsa bu kart gizli. Mevcut alt Next ve favori yıldızı kalır; dar görünümde bilgi kartı butonları gizlidir.
- Yeni blur/ağır gölge/şeffaf pencere/bitmap cache yok. Onaylanan mevcut bindirmeler kullanılır; eşitleyici ve bağlanma halkası statik. HTML blur/gölgeleri uygulanmaz. Masaüstü başlık çubuğu/WPF scrollbar/odak davranışı ve hh:mm:ss süre biçimi korunur.
- Küçük afiş dış kutusu HTML'deki 2.5×3.5rem, görüntü içeride 2:3 oranını korur.
- Mini açıkken ana pencerenin film listesi/bilgi kartı ve mevcut video yer tutucusu korunur. Native aktarım/auto-hide temizliği eski davranıştır; etkin mini ve dönüş testi geçti. Planın mini geçiş ifadesi bu uyumluluk kararıyla uygulanır.
- Çalışma mevcut checkout'ta onaylı ayrı daldadır; ek worktree yok. QA kanıtları kullanıcı klasör tercihine göre dış work/qa altında tutulur. Dar eşik pencere DIP / ComfortFactor üzerinden; rem yalnız öğeleri ölçekler. Yanlış tercih edilirse dar eşik veya mini ana-pencere sunumu görsel olarak ayarlanır; motor değişikliği gerektirmez.

## Gerçek ortam sınırları

- GetDpiForWindow=96 (%100). Gerçek %125/%150 monitör DPI'si doğrulanmadı. F8 1.5 bu testlerin yerine sayılmadı.
- PNG'ler WPF katmanıdır; HWND video bitmap'e dahil değildir. Yerel video gerçekten oynadı, HWND kimliği ve GetWindowRect sınırları ayrıca test edildi. Windows yakalama aracı bir denemede process-id hatası, diğerinde yanlış pencere görüntüsü döndürdü; bu görüntü kanıt olarak kaydedilmedi. Birleşik native-video ekran görüntüsü doğrulanamadı.
- Yerel HTML görüntüleme tarayıcı güvenlik politikasıyla engellendi; başka yüzeyle aşılmadı. CSS kaynak ölçüleri/renkleri ve sağlanan PNG incelendi; HTML ile piksel piksel tarayıcı karşılaştırması yapılmış sayılmıyor.
- Kullanıcının özel IPTV hesabı kullanılmadı. Yerel film/kanal/bölüm fixture'ı oynatıldı; sağlayıcı davranışı değiştirilmedi.

Bağımsız inceleme iş mantığını kapsam dışında bıraktı; hash ve 78 test ile koruma kontrol edildi. İnceleyici rakip GUI testi çalıştırmadı; yukarıdaki seri GUI sonuçları ana çalışmadan gelir. Paket bütünlük sonuçları teslim klasöründeki manifestte yer alır. Commit/push yapılmadı.

Önerilen commit: `feat(ui): add three-pane film watching view (1.7.15)`.
