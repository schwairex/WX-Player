# Proje ve çalışma klasörü düzeni

[README](../../README.md) · [Geliştirme](DEVELOPMENT.md)

## Kaynak deposu

```text
WXPlayer/
├── WXPlayer.sln                 Çözüm giriş noktası
├── Directory.Build.props        .NET ve uygulama sürümü
├── src/
│   ├── WXPlayer.App/
│   │   ├── App.xaml(.cs)         Başlatma ve ortak kaynaklar
│   │   ├── Shell/                MainWindow ve mevcut partial sınıfları
│   │   ├── Views/
│   │   │   ├── Home/             Ana sayfa
│   │   │   ├── Catalog/          Film/dizi kataloğu
│   │   │   ├── Windows/          Ayarlar, kaynak, mini oynatıcı ve diyaloglar
│   │   │   └── Shared/           Ortak pencere görünüm yardımcıları
│   │   ├── Themes/               XAML tema ve stilleri
│   │   ├── Controls/             Tekrar kullanılan görsel kontroller
│   │   ├── Presentation/         Yalnız görüntü formatlayıcıları
│   │   ├── Interop/              Native video host ve tam ekran yerleşimi
│   │   ├── Services/
│   │   │   ├── Playback/         Oynatma ve canlı tampon
│   │   │   ├── Artwork/          Afiş yükleme ve önbellek
│   │   │   ├── Discord/          Uygulama kimliği
│   │   │   └── Updates/          Güncelleme koordinasyonu
│   │   ├── Diagnostics/Smoke/    Mevcut uygulama smoke testleri
│   │   └── Assets/               Uygulama PNG/ICO, font ve SVG ikonları
│   └── WXPlayer.Core/
│       ├── Domain/               Veri modelleri ve ayarlar
│       ├── Providers/            M3U, Xtream, Stalker
│       ├── Library/              SQLite, kategori ve bölüm yardımcıları
│       ├── Epg/                  Rehber servisleri ve XMLTV
│       ├── Playback/             Oynatma politikaları
│       ├── Artwork/              Görsel keşfi
│       ├── Discord/              Presence ve IPC
│       └── Updates/              GitHub güncelleyici
├── tests/WXPlayer.Tests/
│   ├── Program.cs               Test çalıştırıcısı
│   └── Suites/                  Mevcut test grupları
├── docs/
│   ├── INDEX.md                 Belge giriş noktası
│   ├── CHANGELOG.md             Eksiksiz sürüm geçmişi
│   ├── guides/                  Kullanım, geliştirme ve GitHub kılavuzları
│   ├── design/                  Tasarım kuralları
│   ├── assets/branding/         GitHub logoları
│   ├── assets/screenshots/      Ekran görüntüleri
│   ├── releases/notes/          Geçmiş sürüm notları
│   ├── validation/              Test raporları ve korunan dosya kayıtları
│   └── superpowers/             Önceki tasarım/uygulama planları
├── design/                      Onaylanan HTML görünüm referansları
├── tools/                       Paketleme, başlatıcı ve güncelleyici testi
├── samples/                     Açık test içerikleri
├── licenses/                    Üçüncü taraf lisansları
└── .github/workflows/           Mevcut Windows CI
```

Fiziksel gruplama sorumluluklara göre yapıldı. Namespace, API, oynatıcı,
provider, EPG ve veri akışları korunur; bu bir MVVM geçişi değildir.

## Yerel çalışma alanı

Kalıcı kök: `E:\Codex\2026-09-05\bu-klas-r-i-indekileri-analiz`.

| Klasör | İçerik |
| --- | --- |
| `work/WXPlayer/` | Tek güncel kaynak ağacı. |
| `work/tooling/` | Yerel SDK/CLI home ve NuGet önbellekleri. |
| `work/fixtures/` | Yerel test videosu. |
| `work/qa/<calisma>/` | Ayrı test verileri ve ham sonuçlar. |
| `work/builds/<surum>/` | Gelecek derleme/paketleme ara çıktıları. |
| `outputs/releases/<surum>/` | Teslim edilen EXE, portable/source ZIP ve SHA-256 dosyaları. |
| `outputs/releases/<surum>/reports/` | Sürüm notları ve doğrulama raporları. |
| `outputs/releases/<surum>/screenshots/` | Teslim edilen arayüz görüntüleri. |
| `outputs/github/<tarih>/` | README/logo yükleme paketi ve düzenlenmiş kaynak ZIP. |
| `archive/sources/` | Önceki kaynak ağaçları; güncel geliştirme burada yapılmaz. |
| `archive/builds/`, `archive/qa/` | Önceki ara çıktılar ve test verileri. |
| `archive/operations/<tarih>/` | Taşıma planı, doğrulama kayıtları ve işlem betikleri. |
| `archive/legacy/` | Eski ve sürümü kesin sınıflanamayan arşivler. |

Kökteki çalışma alanı `README.md` dosyası ve `outputs/LATEST.md` nereye
bakılacağını gösterir. Kaynak deposuna yalnız `work/WXPlayer/` **içeriği** yüklenir;
çalışma alanının `archive/`, `outputs/`, QA ve önbellekleri depoya yüklenmez.

2026-10-08 düzenlemesinde önceki kaynaklar ve teslim paketleri silinmeden
korunmuştur. Eski sürüm paketleri yeniden üretilmemiştir; düzenlenmiş kaynak
ayrı isimle teslim edilir. Uygulama sürümü 1.7.13 olarak kalır.
