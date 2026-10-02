# Yerel sürüm teslim düzeni

Kullanıcının tercih ettiği kalıcı teslim klasörü:

`E:\Codex\2026-09-05\bu-klas-r-i-indekileri-analiz\outputs`

2026-10-01 tarihli kullanıcı talimatına göre sonraki tüm yerel çıktılar `E:\Codex\2026-09-05\bu-klas-r-i-indekileri-analiz` kökü altında uygun klasöre hazırlanır:

- `outputs`: teslim edilecek EXE, portable ZIP, kaynak ZIP, sürüm notları, README, doğrulama raporları, SHA-256 dosyaları ve arayüz önizlemeleri.
- `work/release-X.Y.Z`: sürüme özel paketleme ve derleme ara çıktıları.
- `work/qa-X.Y.Z-*`: ayrı test verileri, geçici çalıştırma klasörleri ve ham doğrulama çıktıları.

Yeni bir çıktı türü hazırlanırken mevcut klasör düzeni incelenerek uygun konum belirlenir. Bu talimat önceki C: teslimlerini taşımayı veya E: üzerindeki mevcut kaynak ağacının üzerine yazmayı tek başına gerektirmez; kaynak ağacının güncelliği ayrıca doğrulanır.

Sonraki yerel sürümler de bu klasöre teslim edilir. Yalnız src/.../bin altındaki geliştirme EXE'si teslim sayılmaz.

Her sürüm için:

- WXPlayer-X.Y.Z.exe — tools/build.ps1 tarafından üretilen tek dosya dağıtım başlatıcısı.
- WXPlayer-X.Y.Z-portable.zip — self-contained Windows x64 uygulama, .NET çalışma zamanı, VLC, belgeler ve lisanslar.
- WXPlayer-X.Y.Z-source.zip — WXPlayer/ köklü temiz kaynak arşivi.
- SHA256SUMS-X.Y.Z.txt — üç dağıtım dosyasının SHA-256 özeti.
- SURUM-NOTLARI-X.Y.Z.md, WXPlayer-X.Y.Z-testler.md ve GITHUB-YAYINLAMA-X.Y.Z.md.
- Güncel README ve uygun arayüz önizlemeleri.

Derleme araçlarının ara çıktıları work altında sürüme özel yeni bir klasöre hazırlanır; son dosyalar outputs içine kopyalanır. Eski sürüm dosyaları silinmez veya yeni sürümle üzerine yazılmaz.

tools/build.ps1 -OutputPath <yeni-ara-klasör> test, self-contained publish, ZIP ve tek dosya EXE üretir. -NoRestore yalnız bağımlılıklar önceden restore edilmişse kullanılabilir. CI'nin artifacts varsayılanı değişmez.

Kaynak ZIP'e src, tests, tools, docs, licenses, samples, .github ve depo kök dosyaları alınır. bin, obj, QA dizinleri, veritabanları, kullanıcı ayarları, günlükler, kimlik bilgileri ve geçici dosyalar alınmaz.

Dağıtımdan önce sürüm bilgilerini ve arşiv içeriklerini doğrulayın; portable ve tek dosya EXE smoke testlerini ayrı veri klasörlerinde çalıştırın. Doğrulamadığınız özellikleri test edilmiş olarak raporlamayın. GitHub'a yayınlama ayrı bir işlemdir.
