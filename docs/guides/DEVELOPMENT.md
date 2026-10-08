# Geliştirme

[README](../../README.md) · [Proje yapısı](PROJECT-STRUCTURE.md)

## Teknoloji

C# / WPF, `net10.0-windows`, Windows x64. Ortak sürüm ve derleme ayarları
`Directory.Build.props` dosyasındadır. Oynatıcı LibVLCSharp kullanır; provider,
kütüphane ve EPG işleri `WXPlayer.Core` içindedir.

Windows üzerinde .NET 10 SDK gerekir. Hazır uygulamayı kullanmak için SDK
gerekmez. Kaynak kökünde `WXPlayer.sln` bulunduğundan komutları bu klasörde çalıştırın.

```powershell
dotnet --list-sdks
dotnet restore WXPlayer.sln
dotnet build WXPlayer.sln -c Release
dotnet run --project tests/WXPlayer.Tests -c Release
dotnet run --project src/WXPlayer.App -c Release
```

Test projesi konsol tabanlı mevcut regresyon çalıştırıcısıdır. Discord protokol
testleri Windows named-pipe erişimi ister; IPC engelleyen sandbox yerine normal
kullanıcı oturumunda çalıştırılmalıdır.

## İzole smoke testleri

Kişisel kütüphaneden ayrı, yeni bir veri klasörü seçin. Tam smoke testinde
en az 40 saniyelik yerel test videosu kullanın:

```powershell
dotnet run --project src/WXPlayer.App -c Release -- --smoke --home178-layout-only --data-dir C:\Temp\WXPlayer-Home-QA
dotnet run --project src/WXPlayer.App -c Release -- --smoke --experience1713-layout-only --data-dir C:\Temp\WXPlayer-Experience-QA
dotnet run --project src/WXPlayer.App -c Release -- --smoke --utility1712-layout-only --data-dir C:\Temp\WXPlayer-Utility-QA
dotnet run --project src/WXPlayer.App -c Release -- --smoke --settings1711-layout-only --data-dir C:\Temp\WXPlayer-Settings-QA
dotnet run --project src/WXPlayer.App -c Release -- --smoke --media C:\Temp\test-video.mp4 --data-dir C:\Temp\WXPlayer-Full-QA
```

`smoke-results.json` içindeki `success` ve kontrol sonuçlarını inceleyin.
`smoke-unhandled.txt` veya `startup-error.log` oluşursa bu bir doğrulama hatasıdır.
WPF ekran yakalama yöntemi yerel video HWND’sini siyah gösterebilir; görüntü
yakalama başarısı gerçek video oynatımıyla aynı kontrol değildir.

## Paketleme

```powershell
.\tools\build.ps1 -OutputPath <yeni-ve-bos-ara-klasor>
```

Araç testleri çalıştırır, self-contained Windows x64 portable ZIP ve tek EXE
başlatıcısını üretir. Var olan publish klasörüyle çakışmayı kabul etmez.
`bin/`, `obj/`, QA verileri, önbellekler ve kullanıcı ayarları kaynak teslimine
alınmaz. Yerel teslim düzeni için [sürüm teslim kılavuzuna](RELEASE-DELIVERY.md) bakın.

## Dosya taşıma kuralları

- WPF `x:Class`, C# namespace, olay adları, Binding yolları ve kaynak anahtarları
  klasör düzeninden bağımsızdır; yalnız klasör değişti diye yeniden adlandırmayın.
- Paylaşılan `*Theme.xaml` dosyaları eskisi gibi `src/WXPlayer.App/` kökündedir.
  Mevcut göreli tema yollarını koruyun; yeni klasör eklemeyin.
- Uygulama fontları ve ikonları `src/WXPlayer.App/Assets/` altında kalır.
- Tasarım kuralları [DESIGN_SYSTEM](../design/DESIGN_SYSTEM.md) ve
  [UI_MODERNIZATION](../design/UI_MODERNIZATION.md) belgelerindedir.

Bu düzenleme uygulama sürümünü, paket sürümlerini veya çalışma mimarisini değiştirmez.
