# İlk kurulum

[README](../../README.md) · [Özellikler](FEATURES.md)

## Hangi dosyayı indirmeliyim?

[GitHub Releases](https://github.com/schwairex/WX-Player/releases/latest)
sayfasındaki Windows x64 paketlerinden birini seçin:

| Dosya | Kullanım |
| --- | --- |
| `WXPlayer-<sürüm>.exe` | Tek dosyalı dağıtım başlatıcısı; paketi açar ve uygulamayı çalıştırır. |
| `WXPlayer-<sürüm>-portable.zip` | Tüm dosyaları bir klasöre çıkarıp içindeki `WXPlayer.exe` ile başlatın. |
| `WXPlayer-<sürüm>-source.zip` | Geliştiriciler için kaynak kod; hazır uygulama değildir. |
| `SHA256SUMS-<sürüm>.txt` | Paketleri doğrulamak için SHA-256 özetleri. |

Windows 10/11 x64 gerekir. Hazır dağıtım .NET 10 çalışma zamanı ve LibVLC
içerir; ayrı SDK veya VLC kurulumu gerekmez. Portable EXE’yi ZIP içinden
doğrudan çalıştırmayın; yanındaki DLL ve VLC klasörleri de çıkarılmalıdır.

## Kaynağınızı ekleyin

1. Uygulamayı açıp **Kaynak ekle** düğmesine basın.
2. Sağlayıcınızın verdiği türü seçin ve kaynağa bir ad verin.
3. Aşağıdaki alanları doldurup **Bağlan ve yükle** düğmesine basın.
4. İçe aktarma tamamlanınca Ana sayfa veya sol menüden içeriklerinizi açın.

| Tür | Gerekli bilgiler |
| --- | --- |
| **M3U / TXT** | M3U/M3U8/TXT adresi veya **Dosyadan seç** ile yerel oynatma listesi. |
| **Xtream Codes** | Sunucu adresi, kullanıcı adı ve şifre. |
| **Stalker Portal** | Uyumlu portal adresi ve sağlayıcının verdiği MAC adresi. |

XMLTV adresiniz varsa ilgili alana yazabilirsiniz. EPG kullanılabilirliği ve
Catch-Up desteği sağlayıcıya bağlıdır. Program IPTV aboneliği veya içerik sağlamaz.

## Kütüphane ve ayarlar

Birden fazla kaynağı üstteki kaynak seçiciden değiştirebilirsiniz. Son seçilen
kaynak yeniden açılışta hatırlanır; uygulama Ana sayfa ile başlar. Favoriler,
son izlenenler ve izleme konumları uygulamanın yerel veri klasöründe tutulur.

Varsayılan veri klasörü `%LOCALAPPDATA%\WXPlayer`’dır. Bu klasör kaynak bilgileri
ve kişisel izleme verileri içerir; GitHub’a veya dağıtım ZIP’ine yüklemeyin.
Geliştirme/test için `--data-dir` ile ayrı bir klasör seçilebilir.

## Bir sorun olursa

- Portable pakette eksik dosya hatası alırsanız arşivi tamamen yeniden çıkarın.
- Kaynak ekleme hatasında dosya/adresin erişilebilir olduğunu ve sağlayıcı
  bilgilerinin doğru olduğunu kontrol edin.
- EPG boşsa sağlayıcı EPG bağlantısını ve kanal eşleşmesini kontrol edin.
- Sorun bildirirken sürüm, hata mesajı ve yeniden üretme adımlarını ekleyin.
  Yayın URL’sini, kullanıcı adını ve şifreyi paylaşmayın.

[Discord kurulumu](DISCORD-PRESENCE.md) · [Tüm belgeler](../INDEX.md)
