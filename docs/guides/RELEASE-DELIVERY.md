# Yerel teslim düzeni

Kalıcı kök `E:\Codex\2026-09-05\bu-klas-r-i-indekileri-analiz`.
Dosya düzeni kullanıcı talebiyle taşıma öncesindeki yapıya geri alındı.

- Güncel kaynak: `work/WXPlayer-1.7.13/`.
- SDK/CLI home/NuGet: `work/dotnet/`, `work/dotnet-home/`, `work/nuget/`.
- Test videosu: `work/sintel-trailer.mp4`.
- Uygulama teslimleri: `outputs/` içindeki sürümlü EXE, portable/source ZIP ve özetler.
- Geri dönüş teslimi: `outputs/WXPlayer-1.7.13-source-restored.zip` ve
  `outputs/WXPlayer-1.7.13-GitHub-layout-fix.zip`.

Önceki paketler değiştirilmeden korunmuştur. Geri dönüş uygulama sürümünü
artırmaz; yalnız dosya düzeni geri alınmış ve yeni README korunmuştur.
Yeni uygulama sürümleri mevcut `tools/build.ps1` ile ayrı bir ara klasöre hazırlanır.
Kişisel veriler, bin/obj, QA ve önbellekler kaynak ZIP’e alınmaz.
