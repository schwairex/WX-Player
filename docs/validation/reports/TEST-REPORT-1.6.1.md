# WX Player 1.6.1 — Test ve dağıtım raporu

16 Eylül 2026 · Windows x64 · WPF / LibVLC · SDK 10.0.400 · self-contained .NET 10.0.11

## Regresyon testleri

1.6.1 sürüm bilgisiyle yeniden çalıştırılan 59/59 Core testi geçti.

- Son izlenenlerden tekli kaldırma: film, dizi ve canlı TV; diğer geçmiş kayıtları, favoriler ve kaldığınız dakika korunur.
- İlerleme kaydı kaldırılan kartı geri getirmez. Kütüphane yenilemesi ve yeniden açılış sonrasında kaldırma korunur; içeriği tekrar açmak geçmişe ekler.
- Film/dizi raflarında ziyaretler arasında farklı seçim, aynı ziyaret içinde kararlı sıra, sayfalama, arama, kaynak, tür ve afiş filtreleri.
- Mevcut sağlayıcı, XMLTV/EPG, güncelleme doğrulaması, katalog sınıflandırması, bölüm gezinmesi ve görsel eşleştirme regresyonları.
- 100.500 içerikli içe aktarma testi de geçti.

## Paketlenmiş uygulama

Her iki dağıtım ayrı veri klasöründe standart tam smoke testiyle çalıştırıldı ve smoke-results.json içinde success: true doğrulandı:

1. Self-contained portable uygulama.
2. outputs/WXPlayer-1.6.1.exe tek dosya dağıtımı; izole çalışma klasörüne açılıp içindeki uygulama çalıştırıldı.

Doğrulananlar: 48 film / 48 dizi kartı, yatay sayfalama, kaldırma düğmesinin klavye odağıyla görünmesi, kart ölçülerinin korunması, kaldırmanın oynatmayı tetiklememesi, favorilerin korunması, klavye odağının yeni karta aktarılması, son geçmiş kartının kaldırılması, ana sayfa kaydırma, farklı pencere boyutları, arama, dar/geniş sidebar, ayarlar ve güncelleme penceresi.

İki paket testinde smoke-unhandled.txt, startup-error.log veya launcher-error.log oluşmadı. Tek dosya EXE ve iç uygulama sürüm bilgileri 1.6.1 olarak doğrulandı.

## Kapsam ve sınırlar

- Bu turda --media ile video/codec, PVR, uzun süreli canlı geri sarma veya gerçek IPTV hesabı testi çalıştırılmadı.
- Güncelleme mantığının mevcut Core testleri geçti; iki süreçli güncelleme/yeniden başlatma entegrasyonu bu paketleme turunda yeniden çalıştırılmadı.
- Gerçek GitHub Release yayımlanmadı; harici afiş servislerinin canlı entegrasyon testi bu turda yeniden çalıştırılmadı.
- Derleme başarılı. Mevcut NuGet önbelleğinde güvenlik verisine erişilemediğine dair NU1900 uyarıları vardı; yeni bağımlılık eklenmedi veya paket sürümleri değiştirilmedi.

## Teslim

EXE, portable ZIP ve temiz kaynak ZIP için SHA256SUMS-1.6.1.txt kullanılır. EXE ilk açılışta kendi paketini çıkaran dağıtım başlatıcısıdır; src/bin içindeki küçük geliştirme EXE'si değildir.

Portable ZIP bütünüyle çıkarılmalıdır; içindeki EXE tek başına taşınmaz. Kaynak arşivine bin, obj, QA veritabanları, günlükler, kullanıcı ayarları ve test çalışma klasörleri dahil edilmez.
