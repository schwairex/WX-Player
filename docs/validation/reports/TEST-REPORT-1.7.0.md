# WX Player 1.7.0 doğrulama

- `dotnet build WXPlayer.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false`: başarılı, 0 hata. Çevrimdışı NuGet güvenlik denetimi için NU1900 uyarısı var.
- `dotnet run --project tests/WXPlayer.Tests -c Release --no-restore -p:UseSharedCompilation=false`: 69/69 geçti. GitHub Release içinde genel ve sürüme özel EXE birlikte bulunduğunda doğru varlığın seçilmesi için regresyon testi dahil.
- Home layout smoke: geçti. Masaüstü, kompakt ve geniş kart boyutları, yatay raf gezinmesi, arama, klavye odağı ve kaydırma kontrolleri geçti.
- Tam uygulama smoke: geçti. Açık/kapalı menü, sabit içerik konumu, arama, kütüphane, EPG, ayarlar ve güncelleme penceresi denetlendi.
- Paketlenmiş tek dosya EXE ve portable uygulama smoke: ikisi de geçti.
- Gerçek GitHub yayınına yazma veya canlı IPTV kaynağıyla oynatma yapılmadı; güncelleme seçimi ağ yanıtı fixture'larıyla doğrulandı.
