# Kuyumcu ERP — Ekran, Menü, Alan ve İlişki Kıyas Raporu

**Kime:** Kurucu Ramazan Akkocan · **Hazırlayan:** Tamer Bey koordinasyonunda ByGatech ekibi
**Tarih:** 18 Eylül 2026 · **Soru:** *"Ekran başlıkları, input'lar ve arka plandaki ilişkiler olarak doğru yolda mıyım? Rakiplerde de böyle ekranlar ve işlemler var mı?"*

---

## 1. Tek cümlelik cevap

**Doğru yoldasınız — ama yanlış yarıyı bitirmişsiniz.** Defterin *altındaki* katman (has/TL/döviz paralel defter, parti maliyeti, uyum, denetim izi) taranan 40+ rakibin çoğundan sağlam ve bu katman sonradan kurulması **en pahalı** olandır. Kuyumcunun *gün içinde elini sürdüğü* katman — satış fişinin kendisi, menü, arama, gider, hurdanın çıkışı, tamir defteri — geride. Kalan iş "yeni modül yazmak" değil, **var olan defteri günlük ekranlara bağlamak**.

Üç boyutta not:

| Boyut | Durum | Tek cümle |
|---|---|---|
| Arka plan ilişkileri (veri modeli) | Yön doğru, iskelet eksik | Çekirdek rakiplerin üstünde; fiş başlığı ve DB referans bütünlüğü iki yapısal açık |
| Ekranlar ve işlemler | Ortada | Rakip standardının %36'sı tam, %23'ü kısmi, %41'i yok |
| Menü / bilgi mimarisi | Zayıf | 38 bağlantı tek düz liste; gruplama, arama, breadcrumb hiçbiri yok |

---

## 2. Bu rapor nasıl çıkarıldı (ve nelere güvenilmemeli)

37 ajan üç dalgada çalıştı, 6,4 milyon token harcandı, 1.976 araç çağrısı yapıldı.

| Dalga | Ne yaptı | Ajan |
|---|---|---|
| 1 | Kendi ekran/menü/API/veri/UX envanterimiz + piyasa taraması | 18 |
| 2 | 42 ekranın başlık+input dökümü, 62 varlığın ilişki haritası, rakip ekran/alan taraması | 11 |
| 3 | Beş kıyas matrisi + üç hasım doğrulayıcı | 8 |

**Kendi tarafımız — ölçülen gerçek sayılar:** 42 ekran rotası, 38 menü bağlantısı, 36 API modülü (35 canlı, 1'i boş klasör), 273 HTTP uç niteliği, 62 veritabanı tablosu, 49 migration, 20 hareket türü (19'u fiilen yazılıyor), 126 form alanı tek tek incelendi. Her bulgu `dosya:satır` kanıtına bağlandı.

**Rakip tarafı — kanıt seviyesi dürüstçe:**

| Kanıt seviyesi | Kaç ürün |
|---|---|
| Gerçek ekran görüntüsü okundu | 5 (CetaSoft 9 ekran, L10 Barkod 5, KuyumBase 6, KuyumPOS canlı simülasyon, Terminal Gold) |
| Kılavuz / eğitim videosu menü ağacı | 2 (M&L — 181 videoluk ML Akademi ders başlıkları; Milyem) |
| Yalnız özellik listesi (firma beyanı) | 12 TR ürünü + yurt dışı grubun çoğu |

**Bu raporun sınırı budur:** hiçbir rakip ürünün içine girilmedi, tek bir demo hesabı açılmadı. Bu yüzden rapor boyunca *"rakiplerin istisnasız hepsinde var"* türünden cümle **kurulmadı** — hasım doğrulama turu böyle üç cümleyi zaten çürüttü ve düzeltilmiş hâlleri bu rapora girdi (§ 9).

---

## 3. Rakip haritası — kiminle kıyaslandınız

Toplam 193 ürün/kayıt tarandı. Türkiye'de kuyumcuya özel **40'tan fazla** yazılım doğrulandı.

**TR — kuyumcuya özel (üç kuşak):**

- **Eski kuşak masaüstü:** M&L Yazılım (1992'den beri, 10 ayrı paket, kendi beyanıyla ~6.000 firma — pazar lideri), CetaSoft, Pusula, Expert Barkod
- **Yeni kuşak bulut:** KuyumPOS, Kuyum Plus (Konasoft), Terminal Gold, Milyem/Milyem Plus, KuyumHub, ZarifSoft, Kuyumcum.net, KuyumBase, L10, Kuynet, Meadify, ONEPiR, SarrafPro, Ariyen, Safir Kuyum, Jewelium, Dara
- **Dikey takma çözümler:** Kuvvem, Goldist, Aybil, Softnorm, Kuyumex

**Bulunamadı — gerçek rakip değiller:** AKINSOFT, ETA, Zirve, Vega'nın kuyum dikeyi **yoktur** (dördünün de ürün listesi tarandı). "Kuyumcusoft" ve "Nirvana Kuyumcu" adında ürün bulunamadı.

**TR — genel ticari:** Logo (Tiger/Netsis/Start/İşbaşı/Diva), Mikro, Nebim V3, Paraşüt, BizimHesap, DİA, Uyumsoft, Sentez, Odoo TR. **Kritik bulgu:** 16 üründen **hiçbirinin** resmî materyalinde has altın, milyem, ayar, gram bazlı stok veya has-cari geçmiyor. Stok omurgaları adet + renk/beden matrisi. Kuyum için bu "üstüne uyarlanacak eksik" değil, **farklı bir çekirdek**.

**Yurt dışı:** ABD/AB — The Edge, Jewel360, JewelMate, Luxare (eski Diaspark), WJewel, ARMS, Jewelry Shopkeeper, Rain, Lightspeed, Clientbook, Punchmark, GemFind, Valigara. Hindistan/Körfez — Ornate, SwarnApp, Marg, J-Soft, GehnaERP, Akrut, ERIONT, WinGold Next, SUNFACET, Jwelly, Visual ACE-GOLD, DoFort, LOGIC ERP.

---

## 4. EKRAN kıyası — rakip standardının %41'i sizde yok

83 satırlık matris kuruldu. Rakip-standart sayılan 69 ekranda tablo şöyle:

| Sonuç | Adet | Oran |
|---|---|---|
| Sizde tam var | 25 | %36 |
| Sizde kısmi (başka ekrana gömülü / yarım) | 16 | %23 |
| Sizde yok | 28 | %41 |
| **Ayrıca: rakiplerde görülmeyen, sizin fazlanız** | **14** | — |

### 4.1 Satışı doğrudan engelleyen tek ekran

**e-Fatura / e-Arşiv gönderimi ve gelen kutusu.** Kodda GİB/UBL entegrasyonuna ait **tek satır yok** (grep doğrulandı: 0 sonuç). `/faturalar` ekranı iç belge üretiyor, dışarı göndermiyor. 1 Ocak 2026'dan itibaren e-Arşiv kapsamı dışındaki mükelleflerin faturaları **tutarına bakılmaksızın** e-Arşiv olmak zorunda (VUK 509 § IV.2.4.3) — yani kuyumcuda kâğıt fatura yolu kapandı.

### 4.2 Eksik ekranlar — etkiye göre

| Etki | Ekran | Rakipte | Sizde bugün |
|---|---|---|---|
| Satışı engeller | e-Fatura/e-Arşiv gönderim + gelen kutusu | Neredeyse hepsi | Yok — iç fatura var, GİB yok |
| Yüksek | Kur panosu / vitrin ekranı | Çoğu | Hesap sınıfı yazılmış (`PanoFiyatServisi.cs`), API ucu ve ekran yok |
| Yüksek | Envanter yaşlandırma / ölü stok | Bazıları | Cari yaşlandırma var, stok yaşlandırma yok |
| Yüksek | Personel prim / hakediş | Çoğu | Hiçbir katmanda yok; fişte "satıcı personel" alanı bile yok |
| Yüksek | Döviz alım-satım (sarraf) | Çoğu | Döviz yalnız ödeme aracı — bkz. § 9 uyarısı |
| Orta | Tamir/servis iş emri (tam) | Çoğu | `/siparisler` altında sekme; deftere yazmıyor, kapora ve usta alanı yok |
| Orta | Rezerv / ayırtma / kapora | Çoğu | Hiç yok — ürün "ayrıldı" durumuna alınamıyor |
| Orta | Gider / masraf ekranı | Çoğu | Ayrı ekran yok; kasa hareketine sıkışmış, gider kategorisi ve has karşılığı yok |
| Orta | Satınalma siparişi (PO) + mal kabul | Çoğu | Yok |
| Orta | Satış/alış irsaliyesi | Çoğu | Hiçbir katmanda yok |
| Orta | Müşteri KYC kartı (doğum tarihi, uyruk, segment, temsilci) | Çoğu | Cari kartında yok; bu veriler yalnız işlem bazlı MASAK kaydında |
| Orta | Ustaya metal çıkış / atölye metal defteri | Çoğu | ATOLYE cari türü var, metal defteri yok |
| Orta | Muhasebeleştirme / hesap kodu / mizan | Çoğu | Yok — mali müşavire yalnız genel dışa aktarım |
| Orta | Ekspertiz / değerleme belgesi | Çoğu (ABD) | Yok |
| Orta | Set birleştirme / bozma | Çoğu | Yok |
| Orta | Belge/fiş şablon tasarımcısı | Çoğu | Yok — çıktılar koda gömülü |
| Düşük | Teklif / proforma, marka-model-koleksiyon, istek listesi, sadakat, hediye çeki, RFID akışı, seri no, e-ticaret senkronu | Değişken | Yok |

### 4.3 Sizin fazlanız — 14 ekran

Bunlar taramada rakip karşılığı bulunamayan veya çok nadir görülen ekranlar:

`/denetim-izi` (okuma işlemleri bile kaydediliyor) · `/uyum` (4 sekmeli MASAK: kimlik tespiti, şüpheli işlem, saklama süresi, risk) · `/vezne` (tezgâhtar–kasiyer görev ayrılığı) · `/yedekler` (geri dönüş **tatbikatı** + taahhüt edilen ↔ ölçülen RPO/RTO) · `/veri-ice-aktarim` (kuru provalı) · `/veri-disa-aktarim` (maskeleme + gerekçe kaydı) · `/bildirimler` (arka plan işlerinin şeffaf denetimi) · `/kurulum` (adımlı sihirbaz + açılış bakiyeleri) · `/kilavuz` (ürün içi kılavuz — taramada hiçbir rakipte indirilebilir kılavuz bulunamadı) · `/pos` + `/pos/tahsilatlar` (POS valör defteri: bankadan ne zaman ne kadar yatacak) · `/banka/ekstre/{id}` (IBAN'dan cari bulma ile eşleştirme) · `/raporlar/kar-zarar` (TL **ve** has gram iki bacaklı kâr) · basit/profesyonel mod.

**Dürüst ağırlıklandırma (hasım turunun düzeltmesi):** Bu 14'ün hepsi eşit güçte değil. *Denetim izi* ayrışma sayılamaz — Logo'da F9 Kayıt Bilgisi + Kullanıcı İzleme olarak standart. *MASAK* ise 4 TR üründe geçiyor (KuyumPOS'ta en pahalı pakete kilitli). Gerçekten emsalsiz olanlar: **yedek tatbikatı, kuru provalı veri aktarımı, maskeleme gerekçesi, POS valör defteri, iki bacaklı kâr-zarar, vezne görev ayrılığı.**

Ve bir uyarı: bu 14 fazlanın **hiçbiri kuyumcunun satın alma kararını tek başına vermez.** O karar kur ekranı, satış fişi ve tamir defterinde verilir.

---

## 5. MENÜ kıyası — en zayıf boyut

**Bugünkü durum:** Kenar çubuğunda 38 bağlantı, **tek düz liste**, grup başlığı yok, açılır alt menü yok, ayraç yok. Sıralama bir taksonomiye değil **paket teslim kronolojisine** dayanıyor — dosyanın kendi yorumları hangi bağlantıyı hangi paketin eklediğini yazıyor. Sonuç: Siparişler (28. sıra), satışın kalbi İşlemler'den (5. sıra) **23 sıra uzakta**.

Telafi mekanizmalarının tamamı eksik: global arama yok, komut paleti yok, breadcrumb yok, şube seçici yok, sık kullanılanlar yok, Panel'de kısayol ızgarası yok. Kabuk tercihleri üç ayrı yere dağılmış (dil kenar üstte, mod üstte, tema Ayarlar'da, çıkış kenar altta).

**Etiket çarpışmaları** yanlış ekrana girmeye yol açıyor — her çiftin biri veri girişi, diğeri rapor:

| Çift | Sorun |
|---|---|
| İşlemler ↔ İşlem Defteri | Biri satış yapar, biri rapor gösterir |
| Kasalar ↔ Kasa Defteri | Aynı |
| Vade Takvimi ↔ Vade Takibi | İkisi de rapor, ayrımı belirsiz |
| POS Cihazları ↔ POS Alacakları | Tanım vs. defter |

**Rakiplerde durum — kanıt seviyesiyle:** Menüsü ekran görüntüsüyle doğrulanabilen iki kuyumcu ürününün **ikisi de gruplu**: CetaSoft 15 menü + 9 ikonluk hızlı ray, Terminal Gold 11 başlık (5'i alt menülü). Buna karşılık ekran kanıtlı iki küçük ürün (L10, KuyumBase) düz liste kullanıyor. *Yani "20 rakipten sıfırı düz liste" cümlesi kurulamaz* — ama 38 bağlantılık düz liste hiçbir üründe görülmedi.

**Öneri (hazır malzemeyle):** 38 bağlantı 8 katlanır gruba bölünürse kapalı başlangıçta ekranda **9 satır** kalır. Proje bu tekniği zaten biliyor — Rapor Merkezi 16 raporu 4 grupta topluyor, ama kenar çubuğuna taşınmamış. Komut paletinin kaynağı da hazır: `RotaBasliklari.cs` 42 rotayı başlığa bağlıyor ve çift yönlü testle kilitli.

**Yargı:** Menü düzeltilerek kurtarılmaz, **yeniden kurulmalı**. Kalan paketler geldiğinde liste 40–45'e çıkar ve *"ürün bana bile karmaşık"* tespitiniz kalıcılaşır.

---

## 6. İŞLEM TÜRÜ kıyası — çekirdek başa baş, çıkış kapıları kapalı

46 işlem türü kıyaslandı: **20 tam · 12 kısmi · 13 yok · 1 belirsiz.**

Sizde kodda 20 hareket türü var (`Sabitler.HareketTurleri`): ALIS, SATIS, TAKAS, HURDA_ALIS, EMANET_GIRIS, EMANET_CIKIS, VERESIYE, TAHSILAT, TRANSFER, SAYIM_DUZELTME, ISCILIK, SATIS_IADE, ALIS_IADE, CARI_DUZELTME, DOVIZ_VERESIYE, DOVIZ_TAHSILAT, ACILIS, EVRAK_GIRIS, EVRAK_KARSILIKSIZ, EVRAK_CIRO — 19'u fiilen yazılıyor.

**Çekirdek tezgâh döngüsü rakiplerle başa baş:** satış, alış, hurda alımı, takas, işçilik, iade, veresiye, tahsilat, transfer, sayım, gün sonu. Bazı yerlerde daha disiplinli: storno politikası haritası, gider pusulası + stopaj, MASAK kapısı, açılış devri, kıymetli evrak.

**Zayıflık tek bir yerde toplanıyor: malın ve paranın dükkândan ÇIKIŞ yolları.**

| Eksik işlem | Yaygınlık | Bugün kullanıcı ne yapmak zorunda |
|---|---|---|
| **Hurda satışı** (toptancıya/ramata sevk) | Neredeyse hepsi | Hiçbir yolu yok — biriken hurda stoktan **hiç düşmüyor**; tek çıkış ALIS_IADE ya da elle sayım düzeltmesi, ikisi de defteri yalan söyletir |
| **Virman** (kasa ↔ banka) | Neredeyse hepsi | İki ayrı defterde iki bağımsız kayıt; para bir an iki yerde veya hiçbir yerde |
| Mahsup (ödemeyi borç kalemine bağlama) | Çoğu | Tahsilat toplu düşüyor; hangi tahsilat hangi fişi kapattı izlenemiyor |
| Rezerv / kapora / avans | Çoğu | Ya hiç yazılmıyor ya cariye tahsilat geçilip elde takip ediliyor |
| Siparişin satışa dönüştürülmesi | Çoğu | Sipariş elle kapatılıp sıfırdan satış fişi kesiliyor; ikisi bağlanmıyor |
| Konsinye çıkış / iade | Çoğu | Ürün kartı pasife alınarak "kapatılıyor" |
| Değişim (ürünü ürünle) | Çoğu | İki ayrı fiş (iade + satış), birbirine bağlanmıyor |
| Şubeler arası nakit transferi | Çoğu | İki elle kayıt; yolda olan para hiçbir yerde |
| Çek/senet cirosu | Çoğu | Tür listede duruyor, işlem yok |
| Personel primi, komisyoncu hakedişi, parapuan | Değişken | Elle |

Fire/ifraz/ramat bilinçli kapsam kararıdır (Katman 1 = imalat, kapsam dışı). **Ancak perakendeci de biriken hurdasını erittiriyor** — bu kararın yeniden bakılması gerekiyor.

---

## 7. FORM ALANI (input) kıyası — asıl sorduğunuz şey

126 alan dört formda tek tek kıyaslandı: **41 tam · 36 kısmi · 49 yok.**

| Form | Alan | Tam | Kısmi | Yok |
|---|---|---|---|---|
| Ürün / stok kartı | 35 | 13 | 13 | 9 |
| Müşteri / cari kartı | 31 | 8 | 7 | 16 |
| Satış fişi (başlık + kalem + tahsilat) | 41 | 11 | 11 | 19 |
| Hurda / eski altın alım | 19 | 9 | 5 | 5 |

### 7.1 Çekirdek ölçü alanları — tam, hatta fazlasıyla

Ayar, milyem, **gerçek milyem**, gram, taş gramajı, has gram, paralel TL/has/döviz defteri, kur çapası — hepsi var. Üstelik rakiplerde bulunmayan uyum alanları fazladan var: taş kökeni ibaresi (Kuyum Ticareti Yönetmeliği md. 11/e — 24 Haziran 2026'da zorunlu oldu), PEP beyanı, gider pusulası satıcı kimliği + stopaj, tevsik haddi kapısı, kademeli MASAK formu.

Ürün kartında **56 giriş alanı** var; pırlanta 4C (karat/renk/berraklık/kesim), sertifika kurumu+no, konsinye sahibi ve birim bedeli, rodyum kaplama, reyon — hepsi ekranda.

### 7.2 Kırılma noktası: satış fişi bir kuyumcu fişi gibi görünmüyor

Rakiplerin ekran görüntülerinden okunan kuyumcu kalem satırı şu: **Ayar · Milyem · Gram · Has · Adet · Birim Fiyat** (+ işçilik). Sizin satış fişinizin kalem satırında bu kolonlar **yok** — ürün, adet, birim fiyat var.

| Satış fişinde eksik | Not |
|---|---|
| Kalem satırında ayar/milyem/gram/has | Model olarak VAR — `fatura_kalemleri` bu altılıyı tam taşıyor; satış fişine taşınmamış |
| Satır ve fiş iskontosu | Tek yol birim fiyatı elle ezmek; pazarlık defterde iz bırakmıyor |
| Satıcı personel | `kullanici_id` **kaydeden** kişidir; kodun kendisi "prim kararına dayanak olamaz" diye uyarı basıyor |
| Fiş yazdırma | Satış fişi, tahsilat makbuzu, emanet teslim belgesi hiç basılmıyor (yazdırma deseni 3 yerde kurulu: gider pusulası, ürün etiketi, işlem defteri) |
| Eski altın ödeme bacağı | Rakiplerde tahsilat satırının tipi M(mal)/N(nakit)/K(kart) olabiliyor; sizde takas ayrı sekme |
| Yuvarlama, para üstü, KDV satırı, fiş notu | Yok |
| Metinle ürün/cari arama | Yok — açılır liste 200 kayıtla sınırlı, 200'ün üstü yalnız barkodla bulunuyor |

### 7.3 Kartlarda kimlik ve ticaret alanları zayıf

**Cari kartında yok:** ünvan–ad ayrımı (tek "Ad/Firma Adı"), TCKN/VKN ayrımı (tek "Vergi No", hane doğrulaması yok), **vergi dairesi** (sadece kendi işletme kartınızda var), doğum tarihi/uyruk, kimlik belgesi, iskonto oranı, segment, temsilci, özel gün, ikinci telefon, IBAN, parapuan. `Cari.Notlar` kolonu ve DTO'su **var ama formda alan yok**.

**Ürün kartında yok:** tedarikçi, alış maliyeti, kâr marjı, KDV oranı, marka/model/koleksiyon, seri no, yüzük ölçüsü, min stok/sipariş noktası, fire, gram başına veya yüzdesel işçilik (yalnız sabit TL), taş adedi ve çoklu taş satırı, taş türü (yakut/zümrüt).

Maliyet/marj/KDV başka tablolara dağılmış (`StokPartisi`, `FiyatListesi`, `FaturaKalemi`) — bu mimari olarak **doğru**, ama tezgâhtar hiçbir ekranda *"bu yüzük bana kaça mal oldu"* sorusunun cevabını göremiyor.

### 7.4 Sessiz ölüm: sunucunun kabul ettiği ama ekranın göndermediği alanlar

En ucuz kazanç burada — iş "yeni özellik" değil, **ekrana alan koymak**:

POS cihazı · kart taksiti · ürün kartında RFID · KMTS kodu · görsel URL · açıklama · parite kodu. (Hasım turu bu listeden "vade"yi çıkardı: `/islemler` vade günü alanını gönderiyor.)

---

## 8. ARKA PLAN İLİŞKİLERİ — veri modeli kıyası

47 ilişki/yapı kararı incelendi: **9 tam · 9 kısmi · 29 yok.**

### 8.1 Doğru olan çekirdek (ve bu kısım kıymetli)

İki bacaklı (TL + has gram) tek append-only defter · ayar/milyem/has tek kaynaktan (`Kuyumcu.Core/Domain/Ayarlar.cs`) · 5 madenli has hattı (altın, gümüş, platin, paladyum, çelik) · cari hesapta 5 paralel bakiye (TL + has + USD/EUR/GBP) · FIFO/LIFO/ORTALAMA parti maliyeti · her satırın kur fotoğrafına bağlanması · iade ve storno için **ayrı** kaynak bağları · kiracı izolasyonunun hem okuma (global query filter) hem **yazma** (SaveChangesInterceptor) tarafında kapalı olması.

Bu set taranan rakiplerin çoğunun üstünde ve **sonradan kurulması en zor olan kısımdır.**

### 8.2 Yapısal açık 1 — fiş başlığı

Sektörün ekranla doğrulanmış deseni (CetaSoft "Alışveriş Föyü", M&L "Peşin Satış İşlemleri"): **başlık + satılanlar kalemleri + tahsil edilenler kalemleri.**

Sizde fiş, `hareketler.fis_id` **grup anahtarından** ibaret; başlık tablosu bilinçli olarak açılmamış. Sonuç zincirleme: fiş iskontosu, satış temsilcisi, fiş notu, fiş toplamı, tamir teslimi, taksit planı, fatura başlık bağı — hepsi asılacak yer bulamıyor. `hareketler` tablosunda **adet kolonu bile yok**; satılan adet has gramdan bölünerek türetiliyor.

**Hasım turunun düzeltmesi:** Fiş başlığı *sıfırdan* yok değil — `vezne_fisleri` + `vezne_fis_kalemleri` zaten bir başlık/kalem çiftidir ve hazırlayan + tahsil eden kasiyeri, fiş notunu, toplamı, iptal gerekçesini, kalem adedini, birim fiyatını ve işçiliğini taşıyor. Yani karar *"yeni tablo aç"* değil, **"vezne başlığını genelleştir mi"**.

### 8.3 Yapısal açık 2 — veritabanında sıfır referans bütünlüğü

Bunu ajan raporuyla yetinmeyip kendim doğruladım:

```
HasOne / WithMany / [ForeignKey] / ForeignKey(  →  0 sonuç
Migration'larda ForeignKey                      →  0 sonuç
HasIndex                                        →  135
```

Tüm ilişkiler `string(36)` kolon eşleşmesidir. Kardinalite, zorunluluk ve silme davranışı veritabanında **değil**, yalnız servis kodunda. Hiçbir CASCADE/RESTRICT tanımı yok; yetim kayıt veritabanınca engellenmiyor — bu yüzden projede ayrı bir `yetim-tara.sh` betiği var.

Kritik unique kısıtlar da eksik: `stoklar(tenant, sube, urun)` yok, `urunler.kod` ve `urunler.barkod` yok, `cariler.vergi_no` yok, `banka_hesaplari.iban` yok. Barkod tekilliği "önce SELECT sonra INSERT" desenine bırakılmış — eşzamanlı iki kayıt aynı barkodu alabilir ve POS'ta yanlış ürün gelir.

*Bu bir rakip kıyası değil, mühendislik doğrusudur — rakibin ekranından görülemez ama sizin veritabanınızın sağlığıdır.*

### 8.4 Eksik ilişkiler — özet

| İlişki | Durum | Pratik sonucu |
|---|---|---|
| Fiş başlığı + kalem | Yok (vezne tarafında var) | Fiş düzeyinde hiçbir şey tutulamıyor |
| Ödeme kalemi tablosu | Yok | Karma ödeme üç ayrı yerden birleştirilerek okunuyor |
| İşlem → satış temsilcisi | Yok | Prim hesabının dayanağı üretilmiyor |
| Ürün → tedarikçi | Yok | "Bu malı kimden, kaça aldım" ürün kartından cevaplanamıyor |
| Çoklu taş satırı | Yok | Bir ürün ancak **tek** taş anlatabiliyor |
| Tekil parça / seri no | Yok | İkinci el/antika/tek parça segmenti bu modelde çalışmaz |
| Emanet varlığı | Yok (yalnız iki hareket türü) | "Hangi emanet parçası iade edildi" cevapsız; takip yalnız toplam has gram |
| Rezerv / bloke adet | Yok | "Bu yüzük müşteriye ayrıldı, satılamaz" tutulamıyor |
| Taksit planı | Yok | Çok taksitli satış izlenemiyor |
| Fatura → hareket ID bağı | Metin | `fatura_no` serbest string; "bu fişin faturası kesildi mi" metin eşleşmesiyle soruluyor |
| Tamir → ücret → parça → teslim | Yarım | Tamir ücreti hiçbir yerde tutulmuyor |
| Şube izolasyonu | Yok | Global filtre yalnız kiracı için; şube süzgeci her uçta elle — atlandığı uçta başka şubenin verisi gelir |
| e-belge alanları (ETTN/GİB/senaryo) | Yok | Faturada e-belge kimliği taşınamıyor |
| KVKK rıza kaydı | Yok | Erişim izi var, rıza/geri çekme kaydı yok |
| Polimorfik bağlar (6 adet) | Doğrulanmıyor | Hedefin varlığını garanti eden kısıt yok |

### 8.5 Yargı

**Yön hatası yok.** Temel doğru, iskelet eksik. Fiş başlığı + ödeme kalemi + emanet varlığı bugün açılırsa ucuz; kalan paket kuyruğu bu boşlukların üstüne inşa edilirse her paket ayrı ayrı yeniden yazılır.

Tekil parça/seri no ise farklı bir karar: hasım turu bunu *"karar zaten verilmiş"* saydı — ikinci el/antika/tek parça segmenti **kapsam dışı ilan edilip kayda geçirilmeli**, dondurulmuş kapsamla tutarlı olan budur.

---

## 9. Hasım doğrulama — rapordan çıkarılanlar

Üç bağımsız ajan matrisleri çürütmeye çalıştı: **3 iddia çürütüldü, 20 iddia zayıfladı, kalanı ayakta.** Dürüstlük gereği düzeltmeler:

| Matrisin iddiası | Gerçek |
|---|---|
| "Kur ekranında alış/satış makası yok" | **Yanlış.** `Parite` kaydı Alis/Satis taşıyor, `/kur` ekranı iki sütunu da çiziyor. Eksik olan **dükkânın kendi marjı** — parite başına marj girilip saklanamıyor (hesap sınıfı hazır) |
| "22 Ayar Hurda ≠ Bilezik ayrımı yok" | **Yanlış.** `HURDA_22` ve `BILEZIK_22` ayrı kodlar, ikisi de otomatik eşleşmeye bilinçli kapalı. Satır matristen çıkarıldı |
| "Rakiplerin istisnasız hepsinde 6 kolonlu kuyumcu satırı var" | **Kurulamaz.** Bu kolon kümesinin tamamı yalnız 2 üründe ekranla doğrulandı (CetaSoft, M&L) |
| "Döviz alım-satım ilk beşe girmeli" | **Çıkarıldı.** Perakende kuyumcu lisanssız döviz ticareti yapamaz (32 Sayılı Karar); mevcut tasarım doğru |
| "Emanet varlığını aç" | **Önce hukuk.** Kuyum Ticareti Yönetmeliği md. 10/3 kuyum işletmesinin emanet almasını yasaklıyor; kapsam "tamir kabulü / tedarikçi konsinyesi" olarak daraltılmalı — **kurucu kalemidir** |
| "Personel primi ilk beşte" | **Bölündü.** Fişe *satıcı personel alanı* şimdi konsun (ucuz); prim **hesabı** ertelensin — 1-5 kullanıcılı dükkânda karşılığı zayıf |
| "20 rakipten sıfırı düz liste menü kullanıyor" | **Kurulamaz.** Ekranla doğrulanan 2 kuyumcu ürünü gruplu, 2 küçük ürün düz liste |
| "Mevzuat kaynaklı satılamazlık = e-Fatura" | **Eksik.** Üç kalem var: (1) KDV 23/e özel matrahın varsayılan açık olması, (2) ÖKC/Z raporu yol ayrımı, (3) e-Arşiv. Sıra: **(1) → (2) → (3)** |
| Sayı hataları | Menü 36 değil **38**; `RotaBasliklari` 41 değil **42** rota; `window.print` 2 değil **3** yerde |

Ayrıca hasım turunun tespiti: bu matrislerin "ilk beş" listeleri satın alma kararını değil, **ürünün demoda ve ilk haftada çökmemesini** belirler. Satın alma kararının ilk dört kriteri (ücretsiz deneme, kurulum+eğitim, destek yanıt süresi, referans) ürün dışıdır.

---

## 10. Yargı: doğru yolda mısınız?

**Evet — üç şartla.**

**1. Doğru olan ne:** Kuyumculuğun matematiğini (has/milyem/ayar/paralel defter/parti maliyeti) ve uyum katmanını (MASAK, tevsik, gider pusulası + stopaj, denetim izi, KVKK maskeleme, taş kökeni ibaresi) *sonradan eklenemeyecek* derinlikte kurmuşsunuz. Pazarda "hem kuyumculuk matematiğini tam yapan **hem de** KMTS/MASAK/denetim izini standart veren" bir ürün taramada doğrulanamadı — **bu boşluk gerçek ve sizin elinizde.**

**2. Yanlış olan ne:** Aynı titizlik tezgâh katmanına uygulanmamış. Kuyumcu ürünü günde 200 kez satış fişinden, kur ekranından ve menüden kullanır; bu üçü sizin en zayıf üç boyutunuz. 38 bağlantılık düz menü ve 200 kayıtlık arama kutusuz açılır liste, bir demo turunda uyum katmanının tamamını gölgeler.

**3. Tehlike ne:** Kalan paketler mevcut iskeletin üstüne inşa edilirse, fiş başlığı ve ödeme kalemi boşluğu her pakette yeniden karşınıza çıkar. Ucuz olan şimdi; pahalı olan sonra.

### Sıralama (hasım turunun düzelttiği hâliyle)

| # | İş | Neden burada |
|---|---|---|
| 0 | **Global arama** (üst bar + ürün/cari seçicisinde sunucu taraflı arama) | Tüm matrislerin ortak birinci sırası; bugün 200 kayıt üstü görünmüyor |
| 0 | **Menü/IA yeniden kurulumu** (8 katlanır grup, etiket çarpışmalarının düzeltilmesi) | Sadeleştirme, yeni modülden önce gelir — kapsam dondurulmuş durumda |
| 1 | **Yazılmış ama bağlanmamış işler**: pano API ucu + vitrin ekranı, ürün görselinin gösterilmesi, sunucunun kabul ettiği 7 alanın forma konması, düzeltme politikasının her satırda rozet olarak görünmesi | En düşük maliyet, en görünür kazanç; kapsam büyümüyor |
| 2 | **Mevzuat üçlüsü**: KDV 23/e özel matrah varsayılanı → ÖKC yol ayrımı → e-Arşiv entegratörü | (1) ve (2) geliştirme, (3) sözleşme+kontör = **kurucu kalemi** |
| 3 | **Fiş başlığının genelleştirilmesi** (vezne başlığı üzerinden) + kuyumcu kalem satırı + fiş yazdırma | Diğer her şey buna asılıyor |
| 4 | **Çıkış kapıları**: hurda satışı, kasa↔banka virmanı, gider ekranı (kategori + has karşılığı) | Defteri yalan söylemekten kurtarır |
| 5 | Tamir defterinin deftere bağlanması, kapora/avans, sipariş→satış dönüşümü, fişte satıcı personel alanı | Sıklık sırası orta |
| — | **Ertelenenler:** prim hesabı, seri no/tekil parça (kapsam dışı ilan et), döviz alım-satım (lisans engeli), ekspertiz, layaway, sadakat | Gerekçeleri § 9'da |

### Kurucuya kalan kararlar

1. **e-Fatura entegratörü** — sözleşme + kontör (para kalemi, ekip karar veremez)
2. **Emanet modülünün hukuki durumu** — Kuyum Ticareti Yönetmeliği md. 10/3 karşısında kapsam daraltması için dış hukuki görüş
3. **Fire/ramat kararının gözden geçirilmesi** — perakendeci de hurdasını erittiriyor; Katman 1 sınırı burada bir istisna istiyor mu?
4. **İkinci el/antika/tek parça segmenti** — kapsam dışı ilan edilip kayda geçsin mi?

---

## 11. Kanıt ve sınırlar

- Kendi taraf bulgularının tamamı `dosya:satır` kanıtlıdır ve salt okuma ile çıkarılmıştır. **Hiçbir kod değiştirilmedi, hiçbir test çalıştırılmadı** (talimatınız gereği).
- Rakip bulguları **firma beyanı ve yayımlanmış ekran görüntüsü** seviyesindedir. 17 TR ürününden 3'ünde gerçek ekran görüntüsü, 2'sinde kılavuz/video, 12'sinde yalnız özellik listesi kanıtı vardır. Hiçbir rakip ürünün içine girilmedi.
- Rakip listesi pazarın tamamı değildir; uyum/fatura katmanında konumlanan bazı oyuncular bu taramada ürün ekranı düzeyinde incelenmedi.
- Ekran görüntüsü kanıtı ürünün **o günkü** hâlini gösterir; eski bir ekran görüntüsünden "rakipte yok" sonucu çıkarılmadı.
- Referans sayıları (M&L ~6.000 firma, KuyumPOS 250+ mağaza) firmaların kendi beyanıdır, doğrulanmamıştır.
