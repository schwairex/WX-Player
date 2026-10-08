# WX Player 1.6.3 test raporu

- `dotnet restore WXPlayer.sln -m:1 -p:UseSharedCompilation=false`: başarılı. NuGet güvenlik açığı servisine erişilemediğinden NU1900 uyarısı oluştu; paketler yerel önbellekten geri yüklendi.
- `dotnet build WXPlayer.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false`: 0 derleme hatası. NU1900 uyarısı dışında yeni kod uyarısı yok.
- Regresyon testleri: **68/68 geçti**. İki Windows named-pipe testi kısıtlı çalışma ortamında iptal edildi; normal Windows IPC izinleriyle aynı tam test takımı 68/68 geçti.
- Home smoke ve genel smoke: başarılı; `smoke-unhandled.txt` ve `startup-error.log` oluşmadı.
- Gerçek LibVLC/Discord smoke: film ve bölüm afişi, güvenli olmayan kaynak logosu yerine herkese açık katalog logosu, iki canlı kanal arasında yeni görselin korunması, EPG metni ve canlı TV'de süre bulunmaması; duraklatma, devam, seek, stop, bitiş ve oynatma hatası: tüm kontroller geçti.
- Paketlenmiş portable uygulama ve tek EXE için aynı Discord/LibVLC smoke senaryosu başarıyla çalıştı.

Discord istemcisinin hesabınızda uzak görseli gerçekten gösterdiği, otomatik smoke testiyle doğrulanamaz; Discord masaüstü uygulamasında manuel kontrol gerekir. Herkese açık katalogda eşleşmeyen veya Discord'un kabul etmediği görsellerde uygulama simgesi güvenli yedektir.
