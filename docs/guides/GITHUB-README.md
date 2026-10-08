# GitHub dosya yapısını geri alma

GitHub’da eski kaynak dosyaları ile yeni alt klasörlerdeki kopyaları birlikte
kaldığından aynı sınıflar iki kez derleniyordu. `CS0111` hatası yerelde de üretildi.
Uygulamanın güncel 1.7.13 kodu eski dosya yollarına geri getirildi.
Yeni README ve logolar korunur.

## Hazır düzeltme paketini uygulama

1. `WXPlayer-1.7.13-GitHub-layout-fix.zip` paketini bilgisayarda ayrı bir klasöre çıkarın.
2. GitHub Desktop’ta mevcut WX-Player deposunu açın. **Repository → Show in Explorer**
   ile yerel depo klasörünün yolunu öğrenin. Yüklü değişikliklerinizi önce kaydedin.
3. PowerShell’de paket klasöründen çalıştırın:

```powershell
.\Apply-GitHub-Layout-Fix.ps1 -RepositoryPath "YEREL-DEPO-KLASORU"
```

4. Betik önce deponun doğru olduğunu, çalışma ağacının temiz olduğunu ve yamanın
   uygulanabildiğini kontrol eder. Yeni klasörlerdeki çift kaynak dosyalarını
   kaldırır ve güncel dosyaları eski konumlarına getirir. README hash’i değişmez.
5. GitHub Desktop’ta değişiklikleri inceleyip **Commit** ve **Push origin** yapın.
   GitHub Actions yeni commit için yeniden çalışır.

Betik token veya şifre istemez; otomatik commit/push yapmaz. Yalnız ZIP yüklemek
GitHub’daki yeni klasörleri silmez, dolayısıyla derleme hatasını tek başına çözmez.

## Kaynak ZIP

`WXPlayer-1.7.13-source-restored.zip` eski düzene alınmış tam kaynaktır.
İçindeki `WXPlayer/` klasörünün içeriği depo köküne karşılık gelir.
Mevcut `.git` klasörünü koruyun. `src/` ve `tests/` klasörleri birleştirilerek
kopyalanmamalı; eski/yeni çiftleri bırakmadan değiştirilmelidir. Hazır yama bu
işlemi kontrollü yapar.

README’nin göreli görsel yolları için `docs/assets/branding/` ve
`docs/assets/screenshots/` korunur. Bu belge/görsel dosyaları C# derlemesine girmez.
Ek bir logo URL’si oluşturmanız gerekmez.

[README](../../README.md) · [Proje yapısı](PROJECT-STRUCTURE.md)
