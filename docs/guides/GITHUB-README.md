# README ve logoyu GitHub’a yükleme

[README](../../README.md) · [Proje yapısı](PROJECT-STRUCTURE.md)

## Yalnız README ve görselleri güncellemek

Hazırlanan `WXPlayer-GitHub-README.zip` arşivini bilgisayarınızda çıkarın.
İçindeki `README.md` depo köküne, `docs/` klasörü aynı adıyla depo kökünün
altına yerleşmelidir. `LICENSE` ve `THIRD-PARTY-NOTICES.md` de mevcut konumlarında
kalır. ZIP dosyasını tek başına GitHub’a yüklemek README’yi güncellemez.

```text
depo-koku/
├── README.md
├── LICENSE
├── THIRD-PARTY-NOTICES.md
└── docs/
    ├── INDEX.md
    ├── CHANGELOG.md
    ├── guides/
    ├── assets/
    │   ├── branding/
    │   │   ├── wx-logo-horizontal-light-text.svg
    │   │   └── wx-logo-horizontal-dark-text.svg
    │   └── screenshots/
    ├── design/
    ├── releases/
    └── validation/
```

1. [WX-Player deposunda](https://github.com/schwairex/WX-Player) **Code** sekmesini açın.
2. **Add file → Upload files** seçin; çıkarılan paketin **içeriğini** sürükleyin.
   Üst paket klasörünü yüklemeyin: README depo kökünde olmalıdır.
3. Yolların `docs/assets/branding/...` gibi korunduğunu önizlemede kontrol edin.
4. Bir commit açıklaması yazıp değişiklikleri kaydedin. Korumalı dal varsa
   yeni dal/PR akışını kullanın.

GitHub tarayıcı yüklemesi bir defada en çok 100 dosya, dosya başına 25 MiB
kabul eder. Paket bu sayıyı aşarsa klasörleri ayrı yükleyin veya GitHub Desktop
kullanın. Her yüklemede depo kökünden başlayın; klasörlerin yanlışlıkla
`docs/docs/` gibi iç içe yerleşmesine izin vermeyin.

Logolar için ayrıca harici barındırma, Discord asset yükleme veya bir URL
oluşturma gerekmez. README göreli yolları kullanır. GitHub’ın açık temasında
koyu yazılı, koyu temasında sizin verdiğiniz açık yazılı yatay logo görünür.
Asıl SVG değiştirilmeden saklanmıştır; açık tema türevinin geometrisi aynıdır.

## Düzenlenmiş kaynak ağacını da kullanmak

`WXPlayer-1.7.13-organized-source.zip` içindeki `WXPlayer/` klasörünün
**içeriği** depo köküne karşılık gelir. Bu klasör Git deposu içermez; mevcut
deponun `.git` klasörünü ve geçmişini koruyun.

En güvenli yol mevcut GitHub deposunu GitHub Desktop ile ayrı bir yerel
klasöre klonlayıp düzenlenmiş kaynak içeriğini bu çalışma ağacına uygulamaktır.
`src/`, `tests/` ve `docs/` için yalnız yeni dosyaları kopyalamak yeterli değildir:
eski yerlerindeki taşınmış `.cs` ve `.xaml` dosyaları da kaldırılmalıdır.
Aksi halde aynı sınıf iki kez derlenebilir. Önce yerel değişikliklerinizi
yedekleyin; bu üç klasörü düzenlenmiş paketteki karşılıklarıyla değiştirin.
Diğer proje kök dosyalarını paketten kopyalayın, `.git` klasörünü koruyun.

GitHub Desktop değişiklik listesinde taşınan dosyaların eski yoldan silinip
yeni yola eklendiğini, uygulama sürümünün değişmediğini kontrol edin. Build ve
testi çalıştırın; ardından commit/push işlemini siz yapın. Bu teslim sırasında
GitHub’a otomatik gönderim yapılmamıştır.

`bin/`, `obj/`, `settings.json`, veritabanı, IPTV kimlik bilgileri, test verileri
ve yerel önbellekler yüklenmez. EXE/portable ZIP’ler **Code** yerine
[Releases](GITHUB-RELEASES.md) alanına eklenir.

## Yükleme sonrası kontrol

- Depo ana sayfasında logo ve ekran görüntüleri açılıyor mu?
- Açık/koyu tema değişince logo yazısı okunuyor mu?
- Kurulum, özellikler ve sürüm geçmişi bağlantıları açılıyor mu?
- Sürüm rozeti GitHub’da yayımlanan son kararlı sürümü gösteriyor mu?

Rozet yerel kaynak sürümünü değil, yayımlanmış GitHub Release’i gösterir.
README değiştirmek yeni uygulama sürümü yayımlamaz.

GitHub belgeleri: [dosya yükleme](https://docs.github.com/en/repositories/working-with-files/managing-files/adding-a-file-to-a-repository),
[README ve göreli yollar](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes),
[temaya uygun görsel](https://docs.github.com/en/get-started/writing-on-github/getting-started-with-writing-and-formatting-on-github/quickstart-for-writing-on-github).
