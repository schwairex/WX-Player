# Yerel sürüm teslim düzeni

Kalıcı çalışma kökü: `E:\Codex\2026-09-05\bu-klas-r-i-indekileri-analiz`.
Kullanıcının talimatına göre yeni yerel çıktılar bu kökün uygun alt klasörüne hazırlanır.

| Konum | Amaç |
| --- | --- |
| `work/WXPlayer/` | Güncel kaynak. |
| `work/builds/<surum>/` | Yeni, sürüme özel derleme ve publish ara çıktıları. |
| `work/qa/<calisma>/` | İzole test verileri ve ham sonuçlar. |
| `outputs/releases/<surum>/` | Kullanıcıya teslim edilen uygulama paketleri. |
| `outputs/releases/<surum>/reports/` | Sürüm notları ve doğrulama raporları. |
| `outputs/releases/<surum>/screenshots/` | Arayüz görüntüleri. |
| `outputs/github/<tarih>/` | README/logo paketi ve belge/yapı düzenleme teslimleri. |
| `archive/` | Önceki kaynaklar, QA ve ara çıktılar; silinmez. |

Yalnız `src/.../bin` altındaki geliştirme EXE’si teslim sayılmaz.

## Her uygulama sürümünde

- `WXPlayer-X.Y.Z.exe`: `tools/build.ps1` ile üretilen tek dosya dağıtım başlatıcısı.
- `WXPlayer-X.Y.Z-portable.zip`: uygulama, .NET çalışma zamanı, VLC, belgeler ve lisanslar.
- `WXPlayer-X.Y.Z-source.zip`: `WXPlayer/` köklü temiz kaynak arşivi.
- `SHA256SUMS-X.Y.Z.txt`: üç dağıtım dosyasının SHA-256 özeti.
- Sürüm notları, test raporu, GitHub yayınlama kılavuzu ve uygun önizlemeler.

Araç çıktıları `work/builds/` altında yeni bir klasöre hazırlanır; son paketler
`outputs/releases/<surum>/` içine alınır. Eski teslimlerin üzerine yazılmaz.

```powershell
.\tools\build.ps1 -OutputPath <yeni-ara-klasor>
```

Araç test, self-contained publish, ZIP ve tek dosya EXE üretir. `-NoRestore`
yalnız bağımlılıklar önceden restore edilmişse kullanılır. CI’nin `artifacts/`
varsayılanı değişmez.

Kaynak ZIP’e `src`, `tests`, `tools`, `docs`, `design`, `licenses`, `samples`,
`.github` ve depo kök dosyaları alınır. `bin`, `obj`, QA, veritabanı, kullanıcı
ayarları, günlükler, kimlik bilgileri ve geçici dosyalar alınmaz.

Dağıtımdan önce sürüm, arşiv içerikleri ve SHA-256 doğrulanır. Portable ve tek
EXE ayrı veri klasörlerinde smoke testinden geçirilir. Doğrulanmayan özellikler
test edilmiş olarak raporlanmaz. GitHub’a yayınlama ayrı bir işlemdir.

2026-10-08 README/klasör düzenlemesinde uygulama sürümü artırılmamıştır.
Mevcut 1.7.13 EXE/portable/source paketleri aynı baytlarla `releases/1.7.13/`
altında korunur; düzenlenmiş kaynak ZIP’i `github/2026-10-08/` altında ayrı isim alır.
