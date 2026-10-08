# README ve klasör düzenleme doğrulaması — 2026-10-08

Uygulama sürümü **1.7.13** olarak korunmuştur. Bu teslim README/marka belgelerini
ve fiziksel klasör düzenini günceller; yeni uygulama özelliği eklemez.

## Değişiklik sınırı

- `work/WXPlayer/` tek güncel kaynak oldu; önceki ağaçlar `archive/sources/` altında korundu.
- 216 kaynak/belge dosyası sorumluluklarına göre klasörlere taşındı.
- Altı C# görsel yardımcısında tema URI’si ve MainWindow XAML’de tema/logo URI’leri
  yeni klasörlere göre düzeltildi. Başka C#/XAML mantık değişikliği yoktur.
- App.xaml, namespace, olaylar, Binding yolları, kaynak anahtarları, oynatma,
  provider/EPG/veritabanı/test kodu, proje/CI ayarları ve Discord ID aynı kaldı.
- README yenilendi; ayrıntılar bağlantılı kılavuzlara taşındı. 25 sürüm kaydı korundu.
- Verilen yatay SVG bayt düzeyinde aynıdır. Açık tema türevinde yalnız Player
  yazısı koyulaştırıldı ve artık geçerli olmayan imza metaverisi kaldırıldı.

## Derleme ve test

```powershell
dotnet restore WXPlayer.sln --ignore-failed-sources --disable-parallel -m:1 -nr:false -p:NuGetAudit=false
dotnet build WXPlayer.sln -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false
dotnet run --project tests/WXPlayer.Tests -c Release --no-build --no-restore -- <ayri-test-sonuc-dosyasi>
```

Restore/build çıkış kodu 0; **0 uyarı, 0 hata**. Mevcut bağımlılık önbelleği
kullanıldı; paket veya sürüm değişmedi. Sürece özel CLI home/NuGet yolları
`work/tooling/` altında seçildi; kalıcı ortam ayarı değiştirilmedi.
Windows named-pipe testleri normal kullanıcı oturumunda çalıştırıldı.

| Kontrol | Sonuç |
| --- | --- |
| Regresyon | 77/77 |
| `--experience1713-layout-only` | 46/46 |
| `--utility1712-layout-only` | 61/61 |
| `--settings1711-layout-only` | 37/37 |
| `--home178-layout-only` | 24/24 |
| `--catalog-layout-only` | 23/23 |
| `--live1710-layout-only` | 33/33 |
| Tam smoke, yerel Sintel videosuyla | 181/181 |

Yedi smoke akışı da çıkış kodu 0 ve `success=true` verdi. Yeni QA klasörlerinde
`startup-error.log`, `smoke-unhandled.txt` veya `errors.log` oluşmadı.

## Dosya ve görsel doğrulaması

- Önceki güncel kaynak ağacındaki 327 dosyanın tamamı korundu; arşiv kopyasının
  tüm SHA-256 değerleri orijinal kayıtla eşleşti.
- 112 kod/yapılandırma dosyası karşılaştırıldı: yalnız belirtilen 7 dosyada
  kaynak URI değişiklikleri var. Diğer içerikler aynı.
- Çalışma alanındaki 747 taşıma kaydı hedeflerle doğrulandı; dosya özetleri eşleşti.
- Önceki 1.7.13 EXE, portable ve kaynak ZIP’i aynı SHA-256 değerleriyle korunuyor.
- Markdown/HTML yerel belge bağlantıları kontrol edildi; kırık dosya bağlantısı yok.
- Mevcut Edge ve kurulu Markdown renderer ile açık/koyu tema önizlemesi alındı.
  İki temada logo, ekran görüntüleri ve Shields rozetlerinin tamamı yüklendi.
  Önizleme GitHub görünümüne yakın yerel render’dır; GitHub’a yayın yapılmadı.

Ham sonuçlar yerel çalışma alanında `work/qa/docs-structure-2026-10-08/` altında;
taşıma planı ve özetler `archive/operations/2026-10-08/` altında tutulur.
Teslim ZIP’leri ayrıca giriş listesi ve her girişin dosya özetiyle doğrulanır.
