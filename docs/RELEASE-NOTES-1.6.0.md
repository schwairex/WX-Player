# WX Player 1.6.0

15 Eylül 2026

## Dizi izlerken daha kolay bölüm geçişi

- Dizi bölümünü oynatırken normal penceredeki yayın akışı alanı otomatik olarak **sezon ve bölüm paneline** dönüşür. Oynatılan bölümün sezonu seçilir ve bölüm vurgulanır.
- Sezon seçicisinden diğer sezonlara bakabilirsiniz; yalnız sezonu değiştirmek oynatmayı kesmez. Bir bölümün üzerine tıklayarak izlemeye geçebilirsiniz.
- Paneldeki önceki/sonraki düğmeleri, oynatıcının önceki/sonraki kontrolleri ve Page Up / Page Down, dizi izlerken aynı dizinin bölümleri arasında geçer. Sayısal sezon/bölüm sırası kullanılır; sezon sonunda varsa sonraki sezonun ilk bölümüne gidilir. Son bölümden başa dönülmez.
- **Tam ekranda bölümün bitmesine 30 saniye kaldığında “Sonraki bölüm” kartı gösterilir.** Düğmeye basmak sonraki bölümü baştan açar ve tam ekranı korur. Kendiliğinden bölüm atlanmaz.
- Geri sarıp son 30 saniyenin dışına çıktığınızda kart gizlenir. Kart kapatma düğmesiyle o bölüm için susturulabilir. Sonraki bölüm veya bilinen süre yoksa kart gösterilmez.
- Kaynak ve dizi kimlikleriyle ayrılan bölüm önbelleği aynı diziye geçişlerde tekrar API yüklemesini azaltır. Yükleme, boş liste ve hata/tekrar deneme durumları bulunur. Geç gelen sonuçlar izlenmekte olan yeni içeriğin panelini değiştirmez.
- İzleme konumu geçişten önce kaydedilir. **Canlı TV rehberi ve film oynatma davranışı korunur.**

## Eksik afiş ve kanal logolarını otomatik tamamlama

- Kaynağın görseli varsa öncelikle o kullanılır. Boş/eksik görseller için görünümde gereken içerikler arka planda aranır; 100.000 kayıt için aynı anda internet isteği başlatılmaz.
- Dizilerde **TVmaze**, filmlerde ve alternatif eşleşmelerde **Türkçe/İngilizce Wikipedia**, canlı kanallarda **IPTV-org kanal/logo dizini** ve Wikipedia kullanılır. Kullanıcıdan API anahtarı istenmez.
- İçerik adı, türü ve varsa yılı karşılaştırılır. Kanal numaraları korunur; tanınan ülke/kalite/sezon ekleri sadeleştirilir. Aynı adlı farklı yapımlar, alakasız kişiler ve film müziği albümleri ayıklanır. Kanal kimliği varsa IPTV-org dizininde önceliklendirilir.
- Bulunan afiş/logolar mevcut bellek/disk görsel önbelleğiyle açılır. Eşleştirmeler `artwork-discovery` altında saklanır; aynı başlık için eşzamanlı istekler birleştirilir. Başarısız aramalar kısa süre içinde tekrarlanmaz, servis hız sınırlarına bekleyerek uyulur.
- Ana sayfa, eksik görselleri başarıyla tamamlanan içerikleri de raflarına alır. Eşleşemeyen veya yüklenemeyen görseller ana sayfaya eklenmez; içerik tam kütüphanede kalır.
- **Ayarlar → Kütüphane → Eksik afişler ve logolar** bölümünden internetten görsel tamamlama kapatılabilir. Servis kaynaklarına aynı alandan ulaşılabilir.

İnternete yalnız temizlenmiş içerik adı/yılıyla metadata sorgusu yapılır; kaynak adresiniz, oynatma URL'niz ve hesap bilgileriniz gönderilmez. Her yapımın görseli bu kataloglarda bulunmayabilir; belirsiz eşleşmeye rastgele afiş atanmaz. İlk indirme internet ve servis yanıt hızına bağlıdır. Kaynakta mevcut ancak bozuk bir görsel adresi, bu sürümün **eksik adres** aramasının kapsamında değildir.

## Belgeler ve dağıtım

README'nin rozetleri, mevcut tasarımı ve geçmiş sürüm kayıtları korunarak 1.6.0 bölümü eklenmiştir. Kaynakları yeniden eklemek veya kullanıcı verilerini temizlemek gerekmez.

- Dağıtım: `WXPlayer-1.6.0.exe`, `WXPlayer-1.6.0-portable.zip`, kaynak ZIP ve SHA-256 dosyası.
- GitHub Release etiketi: `v1.6.0`. Güncelleme için tek dosya dağıtım EXE'sini kullanın.
- Teknik doğrulama: [1.6.0 test raporu](TEST-REPORT-1.6.0.md).
- API kaynakları: [TVmaze](https://www.tvmaze.com/api), [MediaWiki PageImages](https://www.mediawiki.org/wiki/Extension:PageImages), [IPTV-org API](https://github.com/iptv-org/api).

Görsellerin hakları ilgili sahiplerine aittir. Harici servis metadata lisansları için [üçüncü taraf bildirimlerine](../THIRD-PARTY-NOTICES.md) bakın.
