# WX Player 1.7.7 — doğrulama

2 Ekim 2026, Windows x64, .NET SDK 10.0.401, Release.

| Kontrol | Sonuç |
| --- | --- |
| `dotnet restore` | Başarılı. SDK MSBuild çoklu düğümünü sandbox içinde başlatamadığı için tek düğüm (`-m:1`, `--disable-parallel`) kullanıldı. Dağıtım çalışma zamanları mevcut yerel NuGet önbelleğinden restore edildi. |
| `dotnet build WXPlayer.sln -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false` | 0 hata, 0 uyarı. |
| `dotnet run --project tests/WXPlayer.Tests -c Release --no-build` | **73/73 geçti.** İlk sandbox çalıştırmasında iki named-pipe IPC testi zaman aşımına uğradı; aynı mevcut testler gerçek Windows IPC erişimiyle başarılı oldu. Testler veya Discord kodu bu nedenle değiştirilmedi. |
| `--smoke --catalog-layout-only` | `success:true`; gerçek SQLite katalog, kaynak/eylem kontrol kimlikleri, CSS kaynak izolasyonu, rem sınırları, 2:3 afiş, `np` kartı, gerçek progress hero, klavye odağı, gerçek favori kaydı, asenkron sayfalama, dikey kaydırma, sabit başlık, Tümü ile üste dönüş, arama binding'i, mevcut dizi navigasyonu, küçük/büyük pencere ve Home/Canlı TV için başlığın geri alınması doğrulandı. |
| `--smoke --home-layout-only` | `success:true`. Mevcut Home ve katalog regresyonları başarılı. Önceki katalog testlerindeki afiş genişliği kontrolü yeni referansın 11rem/176 tasarım koordinatına uyarlandı. |
| Yerel Sintel videosuyla `--smoke --media ...` | `success:true`. Video, pause/resume, seek, tam ekran v2, EPG, kayıt, mini oynatıcı, favori/geçmiş, izleme konumu, dizi ve Discord akışları başarılı. |
| XAML ve uygulama hata kayıtları | Başarılı QA dizinlerinde `startup-error.log`, `smoke-unhandled.txt` veya `errors.log` yok. |

73 test sonucu, katalog/Home/tam smoke JSON dosyaları ve katalog PNG'leri teslim klasöründe sürüm adıyla saklanır. Görünüm testlerindeki örnek afişler yalnız izole test verileridir.

Tam smoke sırasında üç yerel video/foreground screenshot işareti `false` döndü (`playingWindowCaptured`, `fullscreenWindowCaptured`, `fullscreenVisibleScreenshot`). Smoke başarı sonucunu etkilemeyen bu masaüstü yakalama kontrolleri başarılı olarak raporlanmaz. Teslim edilen katalog PNG'leri WPF render'ından alınmıştır; yerel video screenshot'u değildir.

`src/WXPlayer.Core` dosyalarının tamamı, `PlaybackEngine`, `NativeVideoHost`, tam ekran kaynakları/yerleşimi, Discord yapılandırması, `MainWindow.Catalog.cs` veri/sayfalama bağlantısı, sağlayıcılar, EPG, ayarlar ve veritabanı mantığı 1.7.6 kaynağıyla hash karşılaştırmasında aynı kaldı. MainWindow C# değişiklikleri sayfa görünümünü seçen mevcut noktalardan görsel başlığı bağlama/geri alma çağrısı ve sürüm yazısıyla sınırlıdır.

Tarayıcı yerel HTML açılışını güvenlik nedeniyle reddetti. WPF PNG'leri ve kaynak değerleri kontrol edildi; tarayıcı–WPF piksel eşitliği iddia edilmez. CSS blur/letter-spacing ve metin/gölge çizimi farkları [görsel raporda](DESIGN-QA-1.7.7.md), eklenmeyen öğeler [sürüm notlarında](../../releases/notes/RELEASE-NOTES-1.7.7.md) açıklanır.

Mevcut C#/WPF projesinde ayrı bir JavaScript lint/typecheck komutu yok; C#/XAML doğrulaması çözüm derlemesiyle yapıldı. Gerçek sağlayıcı hesabıyla uzun süreli ağ testi bu yerel doğrulamanın kapsamı dışındadır.
