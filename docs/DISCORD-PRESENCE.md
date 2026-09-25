# Discord Rich Presence

## Etkinleştirme

Discord Developer Portal'da WX Player adına bir uygulama oluşturun. Application ID değerini `src/WXPlayer.App/DiscordConfiguration.cs` dosyasındaki `ApplicationId` sabitine yazın. Bot tokeni veya client secret gerekmez; bunları projeye eklemeyin. Başka projenin ID'si kullanılmaz.

Ardından `dotnet restore`, `dotnet build WXPlayer.sln -c Release`, `dotnet run --project tests/WXPlayer.Tests -c Release` komutlarını çalıştırın. Dağıtım için `tools/build.ps1 -OutputPath <yeni-klasör>` ile yeniden paketleyin. Teslim edilen placeholder içeren EXE/portable paketlerde Discord bağlantısı bilinçli olarak etkin değildir; oynatıcı normal çalışır.

Discord masaüstünü açın. WX Player'da Ayarlar → Oynatma → Discord Rich Presence kutusunu açıp kaydedin. Discord hesabınızın etkinlik paylaşımı da açık olmalıdır. Varsayılan kapalıdır; mevcut ayarlar geriye uyumludur.

## Davranış ve mimari

- `DiscordPresenceState`: saf içerik/durum projeksiyonu. İçerik seçimi nesli eski sonuçları reddeder. Playing görülmeden presence oluşmaz. Film/dizi zamanları saniye cinsinden Discord başlangıç/bitiş zamanlarına çevrilir; duraklamada zamanlar kaldırılır, devam/seek ile yeniden hesaplanır. Bilinmeyen uzunluk için süre uydurulmaz.
- `MainWindow.Discord`: mevcut LibVLC olaylarını dispatcher'a taşır; olaydan gelen eski içerik verisini kullanmaz. Gerçek güncel oynatıcı durumu okunur. TimeChanged aboneliği yoktur. Var olan UI saati üzerinden durum gözlenir; eşit activity tekrar gönderilmez.
- Canlı EPG görünür takvim gününden bağımsızdır. Yerel rehber arka planda sorgulanır; gerekiyorsa beş saniye sınırlı Xtream kısa rehberi kullanılır. Önbellekteki program sınırı her UI saatinde kontrol edilir, rehber sorgusu 30 saniye aralıklıdır. Kanal değişimi önceki sorguyu iptal eder; nesil kontrolü eski sonucu reddeder.
- `DiscordPresenceService`: tek arka plan sahibi ve en güncel activity posta kutusu. Discord yoksa 15 saniye aralıkla yeniden bağlanır; bağlantı sonrası en güncel snapshot gönderilir. Aynı durumlar tekilleştirilir. OFF, stop, hata, bitiş ve kapanışta activity temizlenir.
- `DiscordIpcConnection`: .NET'in `NamedPipeClientStream` API'siyle Windows `discord-ipc-0..9` protokolü; handshake/READY, çerçeve sınırları, ping/pong, nonce eşleme, ERROR ve iptal/timeout. Yeni NuGet bağımlılığı yoktur. UI ve playback thread'inde bağlantı kurulmaz. Kapanışta worker iptal edilir, clear için 750 ms sınır uygulanır, pipe ve olay abonelikleri kapatılır.

## Gizlilik ve görseller

Activity modeli yayın URL'si, kaynak nesnesi, provider ID, kullanıcı adı/şifre, HTTP başlığı veya token taşımaz. Görünen metinlerden URL'ler ve bilinen kaynak sırları ayıklanır. Sağlayıcıya ait afiş/logo URL'leri gönderilmez; bu adresler yol içinde dahi kimlik bilgisi içerebilir.

Yalnız sorgusuz, kullanıcı bilgisiz HTTPS TMDB/TVmaze/Wikimedia görselleri izinli desenlere uyduğunda paylaşılır. Diğer durumlarda görselsiz presence kullanılır. Discord görseli reddederse aynı bağlantı görselsiz devam eder. Yeni bir afiş indirmesi veya metadata servisi çağrısı yapılmaz. Discord'un nihai görsel gösterimi istemci desteğine bağlıdır.

## Doğrulama

`dotnet run --project tests/WXPlayer.Tests -c Release` saf durum testleri, sahte bağlantılı yeniden bağlanma/OFF/yavaş bağlantı yarışları ve gerçek yerel named-pipe protokol testlerini içerir. Windows IPC'yi engelleyen sandbox altında pipe testleri çalışmaz; normal kullanıcı oturumunda çalıştırılmalıdır.

`dotnet run --project src/WXPlayer.App -c Release -- --smoke --discord-presence --media <yerel-video.mp4> --data-dir <ayrı-QA-klasörü>` gerçek LibVLC film/dizi, EPG, pause/resume/seek, hızlı kanal geçişi, stop ve bitiş durumlarını doğrular. En az 40 saniyelik video gerekir. Sonuç `smoke-results.json` dosyasındadır. Bu test gerçek Discord hesabına activity gönderildiğini kanıtlamaz; Application ID girildikten sonra masaüstü Discord profilinde manuel kontrol gerekir.

## İncelenen referanslar

[NuvioDesktop](https://github.com/NuvioMedia/NuvioDesktop) içindeki `composeApp/src/desktopMain/kotlin/com/nuvio/app/features/discordrpc/DiscordPresenceManager.kt`, `DiscordActivity.kt`, `DiscordIpcClient.kt` incelendi: ayrı arka plan yöneticisi, bağlantı tekrar denemesi, değişmeyen activity'yi atlama ve zaman damgası yaklaşımı. Kotlin kodu veya uygulama ID'si kopyalanmadı. WPF dispatcher, LibVLC durumları ve mevcut kaynak/rehber akışlarına özgü C# uygulaması yazıldı.

Protokol: [Discord RPC belgeleri](https://github.com/discord/discord-api-docs/blob/main/developers/topics/rpc.mdx).
