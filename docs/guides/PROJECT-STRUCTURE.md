# Proje yapısı

Uygulamanın 1.7.13 öncesindeki fiziksel dosya düzeni geri getirildi.
Yeni README ve onun kullandığı logo/görsel/kılavuzlar korunur.

```text
WXPlayer/
├── WXPlayer.sln
├── Directory.Build.props
├── src/
│   ├── WXPlayer.App/
│   │   ├── App.xaml(.cs), MainWindow.xaml(.cs), MainWindow.*.cs
│   │   ├── HomeView*.cs, CatalogView*.cs
│   │   ├── *Window.cs, *Window.Appearance.cs
│   │   ├── *Theme.xaml, FullscreenV2.xaml
│   │   ├── PlaybackEngine.cs, LiveBuffer.cs
│   │   ├── ArtworkCache.cs, ArtworkService.cs
│   │   ├── DiscordConfiguration.cs, UpdateController.cs
│   │   ├── *Smoke.cs, SvgIcon.cs, NativeVideoHost.cs ve diğer yardımcılar
│   │   └── Assets/                     Uygulama simgeleri ve fontlar
│   └── WXPlayer.Core/
│       ├── Models.cs, ProviderClient*.cs, PlaylistParser.cs
│       ├── LibraryStore*.cs, CatalogClassifier.cs, EpisodeNavigation.cs
│       ├── EpgService.cs, XmlTvParser.cs
│       ├── Discord*.cs, ArtworkDiscovery.cs, GitHubUpdater.cs
│       └── Diğer mevcut çekirdek dosyaları
├── tests/WXPlayer.Tests/                Test dosyaları eski düz konumlarında
├── docs/                               Eski belgeler ve README’nin bağlı belgeleri
│   ├── assets/branding/                 README logoları
│   ├── assets/screenshots/              README ekran görüntüleri
│   └── guides/                         README kılavuzları
├── design/
├── tools/
├── samples/
├── licenses/
└── .github/workflows/
```

Dosya taşıması için yapılan WPF tema/logo URI değişiklikleri de geri alındı.
Sınıflar, namespace, kaynak anahtarları, olaylar, veri modeli, sürüm ve Discord
Application ID düzenleme öncesindeki 1.7.13 ile aynıdır.

Yerel güncel kaynak:
`E:\Codex\2026-09-05\bu-klas-r-i-indekileri-analiz\work\WXPlayer-1.7.13`.
Teslimler önceki gibi çalışma kökünün `outputs/` klasöründedir. Eski çalışma
ve sürüm klasörleri de taşıma öncesindeki konumlarına geri alınmıştır.

GitHub’a yeni klasörleri yalnız eklemek eski dosyaları kaldırmaz. Aynı sınıfın
iki konumda kalması CS0101/CS0111 hatalarına yol açar. Geri dönüş yaması bu
çiftleri kaldırır, güncel 1.7.13 dosyalarını eski yollarına koyar ve README’ye dokunmaz.

[README](../../README.md) · [GitHub düzeltme kılavuzu](GITHUB-README.md)
