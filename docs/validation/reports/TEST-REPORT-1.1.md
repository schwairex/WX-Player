# WX Player 1.1 — Arayüz ve oynatma doğrulaması

Tarih: 5 Eylül 2026. Windows x64, Direct3D 11 / D3D11VA. Testler kullanıcı kütüphanesinden ayrı geçici veri klasörleriyle çalıştırılır.

## Bu sürümde değişenler

Inter 4.1 gömülü font; 34 özgün SVG simge; düzenlenmiş arama, menü ve oynatıcı kontrolleri. LibVLCSharp.WPF'nin saydam yardımcı penceresi yerine kendi yerel child HWND video alanımız kullanılıyor. Tam ekran, fiziksel monitör sınırlarına açılıyor; kontrol paneli video alanından yer ayırmadan geçici olarak gösteriliyor.

## Arayüz regresyonları

Aşağıdaki kontroller gerçek WPF uygulaması ve yerel video oynatıcıyla başarıyla çalıştı:

* Gömülü Inter font dosyasının gerçekten seçilmesi.
* Video HWND'sinin oynatıcıya bağlanması; normal oynatmada ayrı görünür yardımcı pencere bulunmaması.
* `F` klavye mesajıyla tam ekrana girme; tüm monitör sınırlarını kaplama.
* Video alanının tüm istemci alanını doldurması; sabit kontrol satırının yer ayırmaması.
* Tam ekran geçişinde native video handle'ının değişmemesi.
* Ekranı doldur için 16:9 gibi sadeleştirilmiş oran kullanılması; sığdır modunda kırpma/en-boy zorlamasının kalkması.
* 2,5 saniye sonrasında kontrollerin gizlenmesi; native fare hareketiyle yeniden görünmesi.
* Önceki pencere konumu ve boyutunun tam olarak geri gelmesi.
* Büyütülmüş pencere → tam ekran → büyütülmüş pencere geçişi.
* Tam ekrandan sonra menü ve kontrollerin geri gelmesi; normal görünümde tam ekran kırpmasının temizlenmesi.
* Geniş ve 940 px kompakt tasarımların PNG olarak oluşturulup incelenmesi.

Native tam ekran alanı testi 2560×1440 monitörde gerçekleştirildi. Test videosu 854×480 Sintel örnek dosyasıdır; video karesine gömülü sinema şeritleri vardır. Bu gömülü şeritler viewport kaynaklı boşluklardan farklıdır ve otomatik siyah kenar algılama bu sürümün kapsamı dışındadır.

## Korunan altyapı

20 Core testi; M3U/M3U8/TXT, Türkçe arama, XMLTV, DPAPI, favoriler, iptal/geri alma, sayfalama, kontrollü Xtream/Stalker API yanıtları ve kaynak silme davranışlarını kapsar. Bunlar paketleme sırasında yeniden çalıştırılır.

Arayüz değişikliği sonrasındaki yerel büyük liste ölçümünde 100.500 içerik 1.422 ms'de içe aktarıldı. Arayüz zamanlayıcısı işlem sırasında 57 kez çalıştı; en uzun zamanlayıcı aralığı 41,5 ms oldu. Bu ölçümler internet indirme süresini içermez ve diğer sistemler için hız garantisi değildir.

Gerçek video çözme, duraklatma, ileri sarma, ses parçası keşfi, harici SRT ekleme, TS kaydı ve kayıt dosyasını yeniden oynatma kontrolleri geçti. Örnek kayıtta 4.993.468 bayt dosya oluştu; yeniden oynatmada video kareleri çözüldü. D3D11VA donanım çözmesi korundu.

Gerçek Xtream/Stalker aboneliği, farklı monitör ölçeklerinin bütün birleşimleri ve bütün ekran kartları test edilmedi. Mevcut kaynak/favori veri şeması değiştirilmedi. EXE imzasızdır; SHA-256 özeti teslimatta bulunur.

## Teslim edilen EXE kontrolü

Paketlenmiş 1.1.0 EXE ayrı veri ve açma klasörleriyle başlatıldı; çıkış kodu 0, genel sonuç başarılı. 100.500 içerik 1.792 ms'de içe aktarıldı; arayüz zamanlayıcısı 71 kez çalıştı, en uzun aralık 34,6 ms oldu. Yerel video oynatma, tam ekran sınırları, kontrol gizleme/gösterme, sığdır/doldur, pencere konumu geri yükleme ve kayıt dosyasını yeniden oynatma kontrolleri geçti.

Geniş ve kompakt WPF arayüz önizlemeleri incelendi. GPU video yüzeyini içeren gerçek pencere ekran görüntüsü bu test oturumunda alınamadı; tam ekran doğrulaması pencere/video sınırları, yerel pencere bağlantısı ve davranış kontrollerine dayanır.
