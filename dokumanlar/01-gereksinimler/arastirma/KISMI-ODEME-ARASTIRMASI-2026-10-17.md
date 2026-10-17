# Kısmi ödeme ve borçlu satış seçimi: araştırma (17.10.2026)

> Salt okuma araştırmasıdır. Dosya değiştirilmedi, commit atılmadı. Kaynaklara erişim tarihi 17.10.2026.
> Etiketler: **[R]** resmî birincil metin (bu turda okundu) · **[Ü]** üretici beyanı · **[B]** bağımsız ikincil kaynak · **[K]** Mihenk kodu okundu · **[Ç]** çıkarım (doğrulanmış bulgu değil) · **[D]** doğrulanamadı.
> Okunan sayfalarda yapay zekâya yönelik talimat bulunmadı. Zoho sayfasındaki "Open in ChatGPT/Claude" bağlantıları yalnız paylaşım düğmesi; uygulanmadı.

---

## 1. Özet: en önemli 8 bulgu

1. **Borçlu satışı seçmek fatura kesmez.** Fatura teslimden itibaren en geç 7 gün içinde düzenlenir. Süresinde düzenlenmeyen fatura "hiç düzenlenmemiş" sayılır (VUK 231/5) [B1][B2]. Veresiye satışın faturası satış anında kesilir, para geldiğinde değil.
   - Ekranda iki yol ayrı tutulmalı: "Yeni satış" fatura keser; "Borçlu satıştan seç" yalnız tahsilat ve mahsup yapar.
   - Mihenk faturasız satışları 365 güne kadar listeliyor (`FaturaServisi.FaturalanabilirIslemler`, `gun` 1–365) [K]. Faturası kesilmemiş ve 7 günü geçmiş bir veresiye satış seçilirse ceza riski var; kullanıcı uyarılmalı.
2. **Paranın hangi borca sayılacağını kanunen borçlu (müşteri) seçer** (TBK 101/1) [R1].
   - Müşteri bildirmezse ödeme, alacaklının makbuzda gösterdiği borca sayılır; müşteri hemen itiraz ederse bu geçmez (101/2).
   - O da yoksa kanuni sıra uygulanır (102): önce vadesi gelmiş borç → önce takibe konan → vadesi önce gelen → vadeleri aynı olanlar arasında orantılı → hiçbiri vadeli değilse güvencesi en az olan.
   - Sonuç: seçim insanın işidir (SOR). Önerilecek dağıtım TBK 102 sırası olmalı. Mihenk'in bugünkü FIFO'su ise **hareket tarihine** göre yürüyor, vadeye göre değil [K].
3. **Kısmi ödemede kalan borç açık kalır; varsayılan budur.**
   - Alacaklı kısmi ödemeyi reddedebilir. Kabul ederse borç kısmen sönmüş olur (TBK 84) [R1].
   - İncelenen her rakipte de varsayılan "kalan açık" [Ü1][Ü3][Ü5]: QuickBooks "Leave as Underpayment", Odoo "Keep open", Paraşüt kısmi tahsilat.
   - Kalanı silmek her yerde ayrı ve bilinçli bir işlem. Odoo'da "Mark as fully paid" seçilir ve fark bir hesaba yazılır ("Post Difference In") [Ü3].
4. **Kalanı silmek (sonradan indirim) KDV matrahını değiştirir.**
   - Faturada gösterilmeyen, sonradan yapılan iskonto KDVK 25/a'dan yararlanamaz; düzeltme KDVK 35'e göre yapılır [R2][R3].
   - Tebliğe göre düzeltme belgesini **alıcı** satıcı adına düzenler [R3]. Müşteri nihai tüketiciyse fatura düzenleyemez; nasıl yapılacağı bulunamadı [D]. Mali müşavire sorulmalı.
   - Mihenk bunu kendiliğinden yapmamalı.
5. **Fazla ödeme müşterinin alacağı olarak saklanmalı.**
   - Rakipler fazlayı hep ayrı bir kalem olarak tutup sonra kullandırıyor ya da iade ediyor [Ü1][Ü3][Ü4][B5]: QuickBooks "credit/unapplied", Xero "overpayment/prepayment", Odoo "Outstanding credits", Zoho "Amount in excess".
   - Paraşüt'te fatura ekranından fatura tutarından fazla tahsilat girilemiyor; fazla tutar müşteri kaydından giriliyor [Ü5].
   - **Mihenk bugün:** `GelenOdemeServisi.Karar(BORC)` tutara üst sınır koymuyor [K]. Fazlası negatif TL bakiyeye dönüşüyor. `VadeKapisi` negatif bakiyeyi "bizim tedarikçiye borcumuz" diye yorumluyor [K]. Müşterinin bizdeki parası (avans) tedarikçi borcuyla aynı biçimde görünüyor.
6. **Gram (has) cinsinden borç para borcu değildir.**
   - Bağımsız bir hukuk yazısına göre altın ne Türk parası ne yabancı paradır; TBK 99 doğrudan uygulanmaz [B6][R1].
   - Bu borcu TL ile kapatmak tarafların anlaşmasına bağlı. Kaç gram düşeceği (alış mı, satış mı, anlaşılan fiyat mı) müşterinin niyetine bağlı, yani SOR [Ç].
   - Satış faturası TL bedelle kesildiyse, sonradan alınan TL ile o bedel arasındaki farkın KDVK 24/c'deki "fiyat farkı" ya da "kur farkı" sayılıp sayılmadığını açıklayan altına özgü bir özelge bulunamadı [D]. Mali müşavire sorulmalı.
   - **Mihenk bugün:** veresiye satış cariye yalnız TL borç yazıyor (`CariBakiyeGuncelle(..., toplamTl, 0m)`) [K]. Has borç yalnız elle `VeresiyeGir` ile doğuyor. `TahsilatGirHareket` TL ve has'ı ayrı ayrı düşüyor; TL'yi grama çevirme yok [K]. `KararBekleyenler` yalnız TL borcu (`borcTl`) gösteriyor.
7. **Döviz:** Mihenk USD borcu TL ile kapatmayı bilinçli olarak kapatmış [K]. Gerekçe: bu fiilen döviz satışıdır (32 Sayılı Karar; `DovizTahsilatGir` notu, K27-G). TRY dışındaki havalede karar verilmiyor [K].
   - İkincil kaynaklara göre yurt içinde yerleşik kişiler arasındaki menkul satış sözleşmelerinde bedel döviz cinsinden kararlaştırılamıyor (32 Sayılı Karar 4/g, 2022 değişikliği) [B7]. Döviz cinsinden veresiyenin kendisi müşavire sorulmalı.
8. **Seçimi kaydedecek yer yok; ekranla defter çelişir.**
   - Rakiplerin hepsi ödeme ile belge arasındaki bağı kalıcı satır olarak tutuyor [Ü1][Ü3][Ü4][B4]: QuickBooks ödeme satırı, Odoo kısmi mutabakat (partial reconcile), Xero tahsis (allocation), Zoho uygulanan/fazla tutar.
   - Mihenk açık borcu defterden FIFO ile türetiyor; mahsup tablosu yok ve bu bilinçli bir tercih [K]. Bu yüzden kullanıcı yeni satışı seçse bile FIFO yine en eskiyi kapatır.
   - En küçük çözüm: **FIFO'yu yalnız sıra bakımından ezen** bir mahsup tablosu (§ 4.3). Bakiyeye dokunmaz; satır yazmayan her ödeme yolu bugünkü gibi FIFO'ya düşer.

---

## 2. Rakip tablosu

| Ürün | Ekran / yol | Kısmi ödeme | Birden çok belgeye bölme | Varsayılan sıra | Fazla ödeme | Kalanı silme | Geri alma | Kaynak |
|---|---|---|---|---|---|---|---|---|
| **QuickBooks** | Receive Payment → açık faturalar işaretlenir, her birine tutar yazılır | "Leave as Underpayment" | Var; dağıtılan toplam gelen tutara eşit olmalı | Otomatik uygulama açıksa **en eski açık fatura**; ayardan kapatılabilir ("Automatically apply payments") | Kalan alacak (credit) ya da "Unapplied Amount" olur; sonra "Discounts and Credits" ile uygulanır | Bu sayfada yok | Transaction History → Go To → yanlış faturanın işareti kaldırılır, doğrusu işaretlenir | [Ü1][Ü2] · topluluk şikâyeti: "yanlışlıkla birden çok fatura seçilmiş" [B3] |
| **Xero** | Find & Match'te banka satırı bölünür; Add Payment | Var; kalan faturada açık durur | Bölme (split) | Kendiliğinden tahsis yok; kullanıcılar en eskiye kendiliğinden tahsis istiyor (fikir panosu) | Overpayment (iade edilecek) / Prepayment (hesaba avans); sonra faturaya tahsis edilir; API'de Allocations | Bulunamadı [D] | Tahsis kaldırılıp yeniden yapılır | Xero Central okunamadı; kaynaklar ikincil [B4] |
| **Odoo 17** | Register payment | "Keep open" (Partial bandı) | Payments Matching / Auto-Reconcile | Fatura dışı ödeme "outstanding" hesapta bekler; yeni faturada mavi bant + "Add" | "Outstanding Credits" | "Mark as fully paid" + "Post Difference In" hesabı | Unreconcile: ödeme kalır, faturayla bağı kopar | [Ü3] |
| **Zoho Books** | Payments Received → + New; "Pay full amount" | Var | Split, "Pay in Full", "Clear applied amount" | Bu sayfada yok [D] | "Amount in excess" → overpayment; iade More → Refund | Yok [D] | Yok [D] | [Ü4] |
| **Paraşüt** | Fatura üzerinden "TAHSİLAT EKLE" ya da müşteri kaydından | Tutar değiştirilerek | Müşteri kaydından girilen tutar kendiliğinden dağıtılır | **Vadesi en çok gecikmiş fatura önce** | Fatura ekranında fatura tutarından fazla girilemez; müşteri ekranında fazlanın ne olduğu yazmıyor [D] | Yok | Yalnız "SİL" | [Ü5] |
| **Logo İşbaşı** | — | [D] | [D] | [D] | [D] | [D] | [D] | Yardım belgesi bulunamadı |
| **KolayBi'** | — | [D] | [D] | [D] | [D] | [D] | [D] | Bulunamadı; önceki araştırmada "ödeme düştü, fatura eşleşmedi" şikâyeti var (M3c, bu turda yeniden doğrulanmadı) |
| **Mikro** | Cari hareketler ve mahsup fişi (eğitim videosu özetinden) | [D] | [D] | [D] | [D] | [D] | [D] | Doğrulanamadı |
| **Milyem PLUS** | "Havale düşer, fatura çıkar" | Sayfada yok | — | — | — | — | — | [Ü6] |
| **SarrafPro** | Banka hareketi tahsilat ya da ödeme kaydına bağlanır | Sayfada yok | — | — | — | — | — | [Ü7] |
| **Pusula / KuyumHub** | Proje içi Pusula referansında "ödeme bacağı fiş satırı, FARK 0,00" kalıbı var; veresiye kapama ekranı belgelenmemiş | [D] | [D] | [D] | [D] | [D] | [D] | PUSULA-REF (proje belgesi); KuyumHub bulunamadı |

**Rakip örüntüsü** (QuickBooks, Odoo, Zoho ve Paraşüt üretici belgeleri; Xero için ikincil kaynaklar):
- Kısmi ödemede kalan açık kalır.
- Fazla ödeme ayrı bir "müşteri alacağı" kalemi olur.
- Kalanı silme ayrı, adı konmuş bir işlemdir.
- Dağıtım ya kendiliğinden yapılır (QuickBooks en eski, Paraşüt en gecikmiş) ya da elle düzeltilir.
- **Gram borca TL tahsilatın nasıl işlendiği hiçbir rakipte belgelenmemiş** [D].

---

## 3. Mevzuat

| Konu | Kural | Kaynak | Mihenk'e etkisi |
|---|---|---|---|
| Fatura zamanı | Teslimden itibaren en geç 7 gün; süresinde düzenlenmeyen fatura "hiç düzenlenmemiş" sayılır. Sürenin teslimi izleyen gün başladığına dair İstanbul VDB özelgesi (17.02.2020) aktarılıyor | VUK 231/5: iki bağımsız ikincil kaynak [B1][B2]; birincil VUK metni bu turda açılmadı | Borçlu satış seçimi fatura doğurmaz. Faturasız ve 7 günü geçmiş satış seçilirse uyarı. Bu sınırı aşan faturaya VUK 353 cezası uygulanıp uygulanamayacağı tartışmalı (Danıştay 2020/1199 E. aktarılıyor) [B2] |
| Vergiyi doğuran olay | Mal teslimiyle doğar. Fatura teslimden önce verilirse, faturadaki tutarla sınırlı olarak fatura anında doğar | KDVK 10/a, 10/b [R2] | Kapora almak teslim değildir; fatura zorunluluğu doğurmaz [Ç]. Kapora için fatura kesilirse KDV o tutarla doğar [R2]. Mihenk'te kapora bir TAHSILAT hareketi [K]; bu yapıyla uyumlu |
| Vade farkı | Faturada gösterilen vade farkı matraha girer. Geç ödemeden doğan yeni vade farkı **ayrıca faturalanır**; KDV oranı ilk teslimin oranıdır | KDVK 24/c; KDV GUT III/A-5.2 ve III/B-1.2.1 [R2][R3] | Geç ödemeye "fark" eklemek fatura gerektirir. SOR; kendiliğinden yapılmaz |
| Kur farkı | Döviz cinsinden ya da dövize endeksli bedel sonradan ödenirse: satıcı lehine farkta satıcı fatura keser; alıcı lehine farkta alıcı fatura düzenler. Dönem sonu değerlemesinden KDV hesaplanmaz | KDVK 24/c; GUT III/A-5.3 [R3] | Gram borcun bu kapsama girip girmediği **bulunamadı** [D] |
| İskonto | Faturada gösterilen ve ticari teamüle uygun iskonto matraha girmez. Sonradan yapılan iskonto için KDVK 35'e göre düzeltilir; belgeyi alıcı düzenler | KDVK 25/a, 35; GUT III/A-6.1 [R2][R3] | "Kalanı sil" kendiliğinden yapılmamalı. Nihai tüketicide yöntem müşaviredir |
| Döviz çevrimi | Döviz, vergiyi doğuran olay anındaki cari kurla TL'ye çevrilir | KDVK 26 [R2] | — |
| Kısmi ödeme | Alacaklı kısmi ödemeyi reddedebilir; kabul ederse ikrar edilen kısım ifa edilmiş olur | TBK 84 [R1] | Kısmi ödemede kalanın açık kalması kanunla uyumlu |
| Hangi borca mahsup | Borçlu bildirir (101/1). Bildirmezse makbuzda gösterilen borç, hemen itiraz edilmedikçe (101/2). O da yoksa kanuni sıra (102, bkz. § 1 madde 2) | TBK 101, 102 [R1] | Müşterinin sözü ya da dekont açıklaması önce gelir. Öneri TBK 102 sırasıyla yapılır. Mahsubun **makbuzda/dekontta yazılı olması** hukuken anlamlı (101/2) |
| Kısmi ödemede faiz ve giderler | Borçlu faiz ve gider ödemesinde gecikmemişse kısmi ödemeyi ana borca saydırabilir | TBK 100 [R1] | Vade farkı ya da faiz uygulanıyorsa sıra etkilenir |
| Makbuz | Borçlu makbuz isteyebilir; kısmi ödemede ödemenin senede işlenmesini isteyebilir | TBK 103 [R1] | Tahsilat makbuzu / ekstre satırında "hangi satışa, kalan ne" yazmalı |
| Para borcu | Para borcu Türk parasıyla ödenir; yabancı para borcu ödeme günündeki rayiçle TL ile ödenebilir (aksi kararlaştırılmamışsa) | TBK 99 [R1] | Altın para değildir; 99 doğrudan uygulanmaz [B6] |
| Taksitle satış | Yazılı şekil zorunlu; peşin bedel, ilave bedel ve taksit bilgileri sözleşmede yazılır | TBK 253 [R1] | Vadeli ve taksitli veresiye bu kapsama girebilir. Tüketici mevzuatı (6502) bu turda araştırılmadı [D] |
| Fazla ödemenin muhasebesi | 340 Alınan Sipariş Avansları: gelecekteki teslim için peşin alınan tutar; fatura kesilince 120 ile mahsup edilir | İkincil [B8]; Tek Düzen Hesap Planı birincil metni açılmadı [D] | Müşteri avansı ayrı görünmeli (§ 1 madde 5) |
| Döviz cinsinden sözleşme | Yerleşikler arası menkul satışta döviz cinsinden ya da dövize endeksli bedel kısıtlı (2022 değişikliği) | 32 Sayılı Karar 4/g ve Tebliğ 2008-32/34: ikincil [B7]; birincil metin açılmadı | Mihenk'teki çapraz kapatma yasağı yerinde. Döviz cinsinden veresiyenin kendisi müşavire |
| Nakit sınırı (459 tevsik) | Mihenk'te mevcut kapı. Havale banka kanalıdır; dekont günü MASAK'a sayılır, kararda `MasakOlcuTl=0` | [K] | Değişmez. Ancak taksit kapısı (TB-15) aynı FIFO'yu okuyor; açık atama bu kapıyı da beslemeli (§ 4.3) |

---

## 4. Veri modeli

### 4.1 Rakiplerdeki kalıp
- **Dağıtım satırı:** (ödeme, belge, tutar, tarih) [Ü1][Ü3][Ü4][B4].
  - Belgenin açık tutarı = belge tutarı − Σ dağıtım.
  - Ödemenin kullanılmamış kısmı (fazlası) = ödeme tutarı − Σ dağıtım.
- **Geri alma** dağıtım satırını kaldırır; ödeme ve belge yerinde kalır. Odoo'da "unreconcile" ile ödeme kalır, bağ kopar [Ü3].
- **Varsayılan dağıtım** kendiliğinden yapılır (QuickBooks en eski, Paraşüt en gecikmiş); kullanıcı elle düzeltebilir.

### 4.2 Mihenk bugün [K]
- `Modules/Vade/VadeKapisi.cs` → `DefteriYuru`
  - Cari hareketleri `Tarih, Id` sırasıyla yürür.
  - Borç doğuran satır bir parti açar: veresiye payı, vade, `FisId`.
  - Borç azaltan satır partileri en eskiden tüketir (FIFO).
  - Defter ile `cari_bakiyeler` tutmazsa fark gizlenmez: "vadesiz" ya da "kaynaksız" olarak dışarı verilir, fazlası en yeni partiden kırpılır.
  - Kod yorumu mahsup tablosunun bilinçli olarak yok olduğunu söylüyor. Gerekçe: satır yazmayan ödeme yolları bakiyeden sapardı.
- `AcikPartiler` partileri kaynak hareket ve `FisId` ile döndürüyor. **Borçlu satış listesi bugünkü veriden zaten türetilebilir:** `Tur=="SATIS"` olan açık partiler [K].
- `Modules/Banka/GelenOdemeModulu.cs`
  - `Karar(BORC)`: gelen tutarın tamamı tek bir TAHSILAT olur (`TlTutari=-h.Tutar`). Hedef satış yok, tutar sınırı yok.
  - `Karar(SATIS)`: borca dokunmaz.
  - Geri alma = TAHSILAT'ın stornosu.
- `Modules/Cari/CariModulu.cs` → `TahsilatGirHareket`: TL ve has bağımsız düşer; TL'yi has'a çevirmez; hedef satış almaz.
- **Eksikler:**
  - (i) Kullanıcının seçimi kaydedilemiyor; FIFO ekranla çelişir.
  - (ii) Sıra hareket tarihine göre, TBK 102 ise vadeye göre (çoğu durumda aynı sonucu verir, vade farklıysa ayrışır).
  - (iii) Fazla ödeme negatif bakiyeye karışıyor.
  - (iv) Has borç karar listesinde yok.

### 4.3 Önerilen en küçük değişiklik
```
cari_mahsuplar (append-only)
  id, tenant_id, cari_id,
  odeme_hareket_id   -- TAHSILAT (ya da azaltan) hareket
  borc_hareket_id    -- açık partiyi doğuran SATIS hareketi
  tutar_tl  decimal(15,2) > 0
  kullanici_id, olusturuldu
```
**Kurallar:**
- **`DefteriYuru` değişikliği:** borç azaltan satırda önce o satırın mahsup hedefleri tüketilir (hedef hâlâ açıksa, en çok kalanı kadar). Kalan tutar bugünkü FIFO'ya devam eder.
  - Satırı olmayan her ödeme yolu bugünkü gibi davranır; bakiyeden sapma olmaz.
  - Tablo bakiyeyi **değiştirmez**, yalnız hangi partinin açık kalacağını belirler. Böylece "tek doğruluk kaynağı defterdir" ilkesi korunur.
- **Değişmezler (test):**
  - Σ mahsup(odeme) ≤ |ödeme TL|
  - Σ mahsup(borc) ≤ partinin o anki kalanı
  - Bakiye mahsup satırından etkilenmez
  - Stornolanan ödemenin satırları yok sayılır (`StornoCiftiniDisla` deseni)
- **Geri alma:** bugünkü storno yolu (TAHSILAT storno) yeterli. Silme yok.
- **Tevsik / taksit (TB-15):** `AcikPartiler` ve `TuketimleriOynat` aynı yürüyüşü kullandığı için atama kendiliğinden yansır. Bu ayrıca test edilmeli.
- **Fazla ödeme:** yeni tablo gerekmez.
  - Müşteri türündeki carinin negatif TL bakiyesi ekranda "müşterinin hesabında X TL var" diye gösterilmeli. Bugün tedarikçi yönüyle aynı biçimde görünüyor [K].
  - Sonraki veresiye satış bunu FIFO ile kendiliğinden kapatır. Bugünkü "fark en yeni partiden kırpılır" kuralı bunu doğru sonuca götürüyor gibi görünüyor [Ç]; test edilmeli.
- **Gram borç:** TL → has çevirisi ayrı bir hareket olmalı (TL tahsilat + has düşümü + kullanılan fiyat + fark izi). KDV niteliği müşavirden gelmeden tasarım kesinleşmez [D].

---

## 5. Mihenk önerisi

### (a) Varsayılan davranış
- Liste müşterinin **açık satışlarını** gösterir: vadesi en eski olan üstte (TBK 102). Satır başına tarih · fiş/fatura no · kalan TL · "faturası var/yok" rozeti.
- Gelen tutar listeye **önerilen dağıtım** olarak yayılır: vadesi en eski olandan başlar, işaretler hazır gelir. Kullanıcı işaretleri ya da tutarları değiştirebilir.
- Kısmi ödemede kalan **açık kalır** (OTO; TBK 84 ve tüm rakipler).
- Fazla ödeme **müşterinin hesabında kalır** (OTO). Ekranda bir satırla gösterilir.
- Dekont açıklamasında fiş ya da fatura numarası geçiyorsa o satış önerilir. Bu, TBK 101/1'deki borçlu beyanına karşılık gelir.

### (b) OTO / ÖNERİ / SOR

| Durum | Karar | Gerekçe |
|---|---|---|
| Tek açık satış, tutar kalana tam eşit | ÖNERİ (ayarla OTO, Geri al'lı; kurucu kararı § 7-2) | Müşterinin niyeti (yeni alışveriş mi?) veride yok |
| Tutar, seçilen satışların toplamından az | OTO: kalan açık · ÖNERİ: vade sırasıyla dağıtım | TBK 84, 102 |
| Birden çok satış, müşteri hangisini dediyse | SOR (kullanıcı seçer) | TBK 101/1: seçim borçlunun |
| Fazla ödeme | OTO: "hesapta kalsın" · SOR yalnız iade ya da "yeni satış yap" seçeneği için | Rakip örüntüsü; iade para hareketi doğurur |
| Kalanı silme / indirim (kuruş farkı dahil) | SOR + yetki (SAHIP/MUDUR); varsayılan kapalı | KDVK 35, GUT III/A-6.1; nihai tüketicide yöntem belirsiz [D] |
| Gram (has) borca TL ödeme | SOR: hangi fiyat (alış / satış / anlaşılan), kaç gram düşer; önizleme | Para borcu değil [B6]; KDV niteliği bilinmiyor [D] |
| Döviz borca TL ödeme | Kapalı (bugünkü gibi) | 32 Sayılı Karar [K][B7] |
| Seçilen satış faturasız ve 7 günü geçmiş | SOR + uyarı ("bu satışın faturası süresinde kesilmemiş") | VUK 231/5 [B1][B2] |
| Geç ödemeye vade farkı eklemek | SOR (ayrı fatura gerekir) | GUT III/A-5.2 [R3] |
| Seçimi değiştirme | Geri al (storno) ve yeniden işle | Mevcut desen [K] |

### (c) En sade ekran (tek birincil düğme)
```
Ahmet Yılmaz'dan 12.500 TL geldi · 17.10 · Ziraat
  ( ) Yeni satış        (•) Borcundan düş
  ┌ Borcu olan satışlar ───────────────────────────┐
  │ [✓] 02.09  Fiş 1043  Bilezik   kalan  8.000 TL │
  │ [✓] 20.09  Fiş 1101  Kolye     kalan  6.000 TL │  ← 4.500 düşer
  └────────────────────────────────────────────────┘
  Düşülecek 12.500 · Kalan borç 1.500 TL
  [ Borcundan düş ]                       (Geri al'lı)
```
- Tutarlar eşitse "Kalan borç 0". Fazla varsa "Müşterinin hesabında 2.000 TL kalacak" satırı çıkar.
- Gram borç varsa ayrı bir satır görünür ("Altın borcu 5,2 gr") ve seçilince fiyat sorusu gelir.
- Açıklama satırı yok, jargon yok ("mahsup" kelimesi ekranda geçmez).

### (d) Veri modeli
§ 4.3'teki `cari_mahsuplar` tablosu: tek tablo, append-only, bakiyeyi değiştirmez, FIFO'yu yalnız sıra bakımından ezer. Ek olarak:
- `BankaKararDto`'ya `Mahsuplar: [{borcHareketId, tutar}]` alanı.
- `KararBekleyenler`'e açık satış listesi ve has borç alanı.
- Müşteri türündeki carinin negatif bakiyesi "alacağı" diye etiketlenir.

### (e) Mali müşavire sorulacaklar
1. Nihai tüketiciye satıştan sonra kalan borcu silmek ya da indirim yapmak: KDV düzeltmesi hangi belgeyle yapılır? Alıcı fatura düzenleyemiyor.
2. Gram cinsinden veresiye: satış faturasındaki TL bedel ile sonradan alınan TL arasındaki fark "fiyat farkı" mı, "kur farkı" mı, yoksa KDV dışı mı? Kim fatura keser? 23/e özel matrahı nasıl etkilenir?
3. Gram borç tamamen altınla (hurda ya da has) kapanırsa bu işlem alış mı, iade mi? Hangi belge kesilir?
4. Müşterinin fazla ödemesi 340 hesabında mı, 120'nin alacak bakiyesi olarak mı izlenmeli? Uzun süre bekleyen avansın riski nedir?
5. 7 günü geçmiş faturasız veresiye satış bulunursa ne yapılmalı: geriye dönük fatura mı, ceza beyanı mı?
6. Geç ödemeye vade farkı uygulanırsa fatura zamanı ve KDV oranı nedir (GUT III/A-5.2'nin kuyum için uygulanışı)?
7. Yerleşik müşteriye döviz cinsinden ya da altına endeksli veresiye açmak 32 Sayılı Karar 4/g bakımından serbest mi?

---

## 6. Açık sorular (doğrulanamadı)
- Logo İşbaşı, KolayBi', Mikro, Pusula, KuyumHub'ın kısmi ödeme, fazla ödeme ve gram borç ekranları: herkese açık belge bulunamadı. Kuyumcuya gösterilip sorulmalı.
- Faturalog borçlu satışı nasıl eşliyor, kısmi ödemeyi nasıl işliyor: bu turda bakılmadı.
- Xero'nun resmî yardım sayfaları (Xero Central) içerik döndürmedi; Xero satırı ikincil kaynaklara dayanıyor.
- Altına endeksli satışta fark için GİB özelgesi bulunamadı.
- 6502 Tüketicinin Korunması Kanunu'nun taksitli veresiye yükümlülükleri araştırılmadı.
- Kuyumcuya sorulacaklar: Müşteri havalede "hangi borç" diye belirtiyor mu? Gram borç TL ile kapanırken bugün hangi fiyat kullanılıyor? Fazla ödemede ne yapılıyor (iade mi, hesapta mı)?

---

## 7. Kaynakça (erişim 17.10.2026)

**Resmî birincil:**
- [R1] 6098 TBK md. 84, 99–104, 253: https://www.mevzuat.gov.tr/mevzuatmetin/1.5.6098.pdf
- [R2] 3065 KDVK md. 10, 24, 25, 26, 35: https://www.mevzuat.gov.tr/mevzuatmetin/1.5.3065.pdf
- [R3] KDV Genel Uygulama Tebliği III/A-5.2, III/A-5.3, III/A-6.1, III/B-1.2: https://www.mevzuat.gov.tr/MevzuatMetin/yonetmelik/9.5.19631.pdf

**Üretici:**
- [Ü1] QuickBooks, ödeme uygulama sorunları: https://quickbooks.intuit.com/learn-support/en-us/help-article/accounting-bookkeeping/resolve-common-issues-applying-payment-towards/L2UMXN8tt_US_en_US
- [Ü2] QuickBooks mobil, en eski faturaya uygulama: https://quickbooks.intuit.com/community/help-articles-128/receive-an-invoice-payment-using-the-quickbooks-online-mobile-app-284363
- [Ü3] Odoo 17 Payments: https://www.odoo.com/documentation/17.0/applications/finance/accounting/payments.html
- [Ü4] Zoho Books Payments Received: https://www.zoho.com/books/help/payments-received/basic-functions.html
- [Ü5] Paraşüt, tahsilat kaydetme: https://www.parasut.com/kullanim-kilavuzu/tahsilatlarinizi-kaydetmek
- [Ü6] Milyem PLUS: https://milyemplus.com/
- [Ü7] SarrafPro: https://sarraf.pro

**Bağımsız ikincil:**
- [B1] TÜRMOB / ISMMMO VUK değişiklikleri: https://ismmmo.org.tr/dosya/2509/Mevzuat-Dosya/vuk-degisiklikler.pdf
- [B2] M. B. Altaş (YMM), "7 gün süresi karmaşası", Alomaliye 08.01.2026: https://www.alomaliye.com/2026/01/08/vergi-usul-kanunu-kapsaminda-fatura-duzenlenmesinde-7-gun-suresi-karmasasi/
- [B3] QuickBooks topluluğu, istemeden bölünen ödeme: https://quickbooks.intuit.com/community/payments-3/qbo-check-payment-automatically-splits-across-multiple-invoices-if-the-amount-field-in-the-payment-screen-is-modified-18943
- [B4] Xero, kısmi ödeme ve fazla ödeme: https://evolutionplatform.poolwerx.com.au/support/solutions/articles/13000109471-xero-record-a-part-payment-on-an-invoice · https://www.accountingweb.co.uk/node/151481 · https://productideas.xero.com/forums/967115-invoices-quotes/suggestions/45973705-sales-invoice-auto-allocate-overpayments-based-o
- [B5] Xero prepayment / overpayment ayrımı: https://www.accountingweb.co.uk/node/175732
- [B6] Av. F. Öztürk, "Altın borcu…", Hukukçular Evi 18.09.2026: https://hukukcularevi.com/altin-borcu-icra-takibi-odunc-altin-aynen-iade-davasi-iik-24/
- [B7] 32 Sayılı Karar 4/g: https://kpmgvergi.com/yayinlar/mali-bultenler/vergi/doviz-cinsinden-veya-dovize-endeksli-sozlesme-sinirlamasiyla-ilgili-teblig-yayimlandi/151 · https://www.muhasebetr.com/yazarlarimiz/huseyinergut/015
- [B8] 120 / 340 mahsubu: https://www.muhasebenews.com/?p=165557

**Kod [K]:**
- /Users/ramazanakkocan/ByGatech-yerel/jewellery/dotnet/Kuyumcu.Api/Modules/Vade/VadeKapisi.cs
- /Users/ramazanakkocan/ByGatech-yerel/jewellery/dotnet/Kuyumcu.Api/Modules/Banka/GelenOdemeModulu.cs
- /Users/ramazanakkocan/ByGatech-yerel/jewellery/dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs (TahsilatGirHareket, DovizTahsilatGir)
- /Users/ramazanakkocan/ByGatech-yerel/jewellery/dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs (veresiye satış TL bakiye yazımı, satır 898/1880)
- /Users/ramazanakkocan/ByGatech-yerel/jewellery/dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs (FaturalanabilirIslemler, 365 gün)
- /Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/02-mimari/KARAR-MERKEZI-MIMARI-2026-10-17.md § 7–8
- Önceki araştırma: /Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/01-gereksinimler/arastirma/MUADIL-ARASTIRMASI-2026-10-17.md
