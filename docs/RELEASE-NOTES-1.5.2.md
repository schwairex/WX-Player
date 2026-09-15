# WX Player 1.5.2

15 Eylül 2026

- Ana sayfanın öneri alanı kompakt, nötr bir panele dönüştürüldü. Alan 264 mantıksal piksel ile sınırlandırılır; büyük veya sonradan yüklenen afişler sayfayı büyütmez.
- Son izlenenler ve favorilerde film, dizi ve canlı kanal kartları aynı genişlik/yüksekliği kullanır. Film ve dizi raflarında ortak dikey afiş ölçüleri uygulanır.
- Başlıklar için iki satırlık sabit alan ayrılır. Uzun adlar kısaltılır; tam ad araç ipucunda görülebilir. İzleme ilerlemesi bulunan raflarda bütün kartlara aynı bilgi alanı ayrılır.
- Arka plan görseli ve yoğun renk geçişleri kaldırıldı; daha sakin yüzeyler, küçük bölüm başlıkları ve tutarlı aralıklar kullanıldı.
- Raf gezinme okları başlık satırına alındı. Düğmeler görünen raf genişliğine göre ilerler; ulaşılabilecek başka içerik yoksa pasifleşir.
- Dar pencerede arama ayrı satıra geçer. Kartlarda görünür klavye odağı ve hafif fare vurgusu bulunur.
- Afiş yüklenemediğinde baş harfler gösterilir. Yükleniyor, boş kütüphane, sonuç bulunamadı ve yeniden denemeli hata durumları düzenlendi.

Kaynaklar, favoriler, izleme konumları ve oynatma altyapısı korunur. Güncelleme için kütüphaneyi silmek veya kaynakları yeniden eklemek gerekmez. Görseller yalnız kaynağın sağladığı adreslerden yüklenir.

Arayüz, kullanıcının sağladığı SKILL.md belgesindeki sade tasarım, bilgi hiyerarşisi, tutarlı ölçüler ve erişilebilirlik ilkelerine göre mevcut C#/WPF mimarisi içinde düzenlendi.
