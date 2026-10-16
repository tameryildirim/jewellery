# KARAR — Eski altınla ödeme HANGİ DEFTERE işlenir? (D3 / B-4 kilidi)

> **Tarih:** 23 Eylül 2026 · **Mühür:** Tamer Bey (Patron/Orkestratör)
> **Mercekler:** Bertan (mimari) · Mustafa (FinOps) · Mehmet (güvenlik/uyum, veto alanı) · Aşkın (QA)
> **Kapadığı kalem:** `KARARLAR-D1-D11-2026-09-23.md` § D3 — *"Karma ödemeye ALTIN bacağı · OY: KURUCUYA"*.
> Kurucu 23 Eylül'de kararı ekibe geri verdi ("bana neden soruyorsun, araştır"). **Karar ekibindir; mali müşavir teyidi doğrulama olarak beklenir (K-10 ailesi), kararı ERTELEMEZ.**
>
> **Anayasa md. 1:** Bu belgede birincil kaynaktan (mevzuat metni / GİB / RG) satır satır doğrulanamayan hiçbir hüküm "DOĞRULANDI" sayılmamıştır; § 7'de adıyla listelenmiştir. **Doğrulanamayan hiçbir hüküm koda sabit yazılmaz** — § 4 md. 14 yapılandırma kuralını bağlayıcı kılar.
> **Bu turda KOD YAZILMADI.** Hiçbir `.cs`/`.razor` değişmedi; yalnız bu dosya üretildi. Commit yok.

---

## 0. TEK CÜMLE

> **Müşterinin ödeme olarak verdiği eski altın, HER İKİ DEFTERE BİRDEN işlenir: aynı `hareketler` satırında `has_grami` (+, fiziki ölçü) ve `tl_tutari` (−, mali ölçü) kolonlarına — ve bu satır satışın bir ödeme satırı DEĞİL, satışla aynı transaction içinde yazılan AYRI bir `HURDA_ALIS` hareketidir; çünkü trampa iki ayrı teslimdir (KDVK md. 2/5) ve defterin birimi TL'dir (VUK md. 215/2), gram ise o TL'nin değerleme girdisidir (VUK md. 274/A).**

**Seçenek:** (A) yalnız has ❌ · (B) yalnız TL ❌ · **(C) İKİSİ BİRDEN ✅**

**Hangisi asıl?** İkisi farklı sorulara cevap verir ve biri diğerinin yerine geçemez:

| | ASIL OLAN | Neden |
|---|---|---|
| **Fiziki / ölçü aslı** | **HAS GRAM** | Kullanıcı gram + ayar girer, sistem hası türetir. Kuyumcunun **pozisyonu** (elde ne kadar metal var) yalnız buradan okunur. Gram yazılmazsa fiyat hareketi kâr gibi görünür. |
| **Mali / yasal aslı** | **TL** | VUK md. 215/2 kayıtların Türk para birimiyle tutulmasını şart koşar. Gider pusulası TL tutar ister. **Kâr/zarar ve cari bakiye yalnız TL bacağından okunur.** |

**"Has defteri" yasal bir defter DEĞİLDİR.** Mevzuatta gram esaslı defter tutma hakkı yoktur (§ 7 md. 1). Bizim `has_grami` kolonumuz, TDHP'de 153 alt hesaplarının **miktar boyutuna** karşılık gelen işletme içi pozisyon defteridir. Bu yüzden "sadece has defterine yazalım" seçeneği **mevzuata aykırıdır ve reddedilmiştir.**

**Oy:** Bertan **EVET** · Mustafa **EVET** · Mehmet **KOŞULLU** (§ 3.2'deki ALTIN yolu kapanmadan B-4 açılmaz — aksi hâlde para hatası çoğalır) · Aşkın **KOŞULLU** (§ 5 test listesi ve § 6 mutasyonları yeşil olmadan sevk yok). **Mühürlendi.**

---

## 1. CEVAP VE DAYANAĞI

### 1.1 Neden "yalnız has" olamaz — VUK md. 215/2

Kayıt ve belgelerde Türk para birimi kullanılır; TL dışı defter tutma izni (VUK md. 215 + 404/569 Sıra No'lu GT) yalnız belirli koşullardaki mükelleflere ve **para birimi** için tanınır. **Altın bir para birimi değildir.**

- Kaynak: VUK md. 215 (GİB mevzuat sayfası), uygulama yorumu Alomaliye / Muhasebetr SMMM yayınları.
- **Doğrulama düzeyi:** hükmün varlığı iki bağımsız mali müşavir yayınından teyit edildi; tam metin birincil kaynaktan satır satır alınamadı (§ 7 md. 2).

**Karşı iddia çürütüldü:** "VUK Geçici md. 35 ile kuyumcular Altın ve Gümüş Esaslı Muhasebe Sistemine geçebilir" iddiası **YÜRÜRLÜKTEKİ HÜKÜM DEĞİLDİR.** Kaynak makale sistemin *getirilmesini TALEP ediyor*, uygulandığını söylemiyor (SMMM yayını, Serdar Karakuş). **Bu iddiaya DAYANILMAZ.**

### 1.2 Neden "yalnız TL" de olamaz — VUK md. 274/A + ölçülen fiyat mekaniği

7524 s. Kanunla eklenen VUK md. 274/A: *kıymetli madenler borsa rayici ile değerlenir; bu hüküm kıymetli maden ile olan alacak ve borçlar hakkında da geçerlidir.*

Mevzuat altın varlığını **TL'ye çevirerek (DEĞERLEYEREK)** tanır. Yani:
> **gram, değerlemenin GİRDİSİDİR; defterin birimi değildir. Girdi saklanmazsa değerleme yapılamaz.**

Ölçülen fiyat mekaniği bunu zorunlu kılar: gram altının TL fiyatı ≈ ons × USD/TRY ÷ 31,1035. Ons ve kur bağımsız hareket eder. **Altın bacağı yalnız TL yazılırsa kuyumcunun metal pozisyonu kaybolur ve fiyat hareketi kâr gibi görünür.**

### 1.3 Neden "ayrı bir HURDA_ALIS hareketi", "satışın ödeme satırı" değil — KDVK md. 2/5 + 23/e

**KDVK md. 2/5 — trampa iki ayrı teslim hükmündedir.** Müşterinin eski altınını verip yeni ürün alması hukuken TEK işlem değil, İKİ AYRI TESLİMDİR:
1. Kuyumcunun müşteriye **satışı** (fatura, özel matrah),
2. Müşterinin kuyumcuya **eski altın satışı** (gider pusulası, VUK md. 234).

**Bedelin altın olarak alınması teslimin niteliğini değiştirmez.** Eski altın bir "ödeme aracı" değil, ayrı bir **ALIŞ**tır.

**KDVK md. 23/e — özel matrah:** ziynet eşyası tesliminde matrah, külçe altın bedeli düşüldükten sonra kalan tutardır (e-arşiv özel matrah nedeni kodu 805). **Mahsup, fatura matrahını DÜŞÜRMEZ** — ayrı belgeyle (gider pusulası) dengelenir.

**Bundan doğan SIRA — bağlayıcıdır:**

| # | Adım | TL bacağı | Has bacağı |
|---|---|---|---|
| 1 | **SATIS** hareketi, **BRÜT** fiş bedeliyle + özel matrah KDV | **+ brüt** | **− satılan ürünün hası** |
| 2 | **HURDA_ALIS** hareketi, gider pusulasıyla | **− eski altının net TL değeri** | **+ eski altının hası** |
| 3 | Fark NAKIT/KART/HAVALE/DOVIZ/VERESIYE bacaklarıyla kapanır | kasaya/cariye | 0 |

**Sıra tersine çevrilirse (satışı net tutardan yazmak) KDV matrahı eksik çıkar — vergi hatasıdır.** Her iki hareket **AYNI `KurSnapshotId` ile ve TEK transaction'da** yazılır.

### 1.4 Sektör ve rakip teamülü — aynı yöne çıkıyor (mevzuat kanıtı DEĞİL, teamül kanıtı)

- **Rize SMMMO yayını** (SMMM Yalçın Sütütemiz, *Kuyumculuk Sektörü Muhasebesi ve Vergilendirilmesi*): altın cinsinden yükümlülük defterde **İKİ ÖLÇÜYLE** taşınır — aynı tabloda "Has Miktarı (gr)" ve "Kayıtlı Tutarı (TL)" yan yana; kapanışta gram, gün maliyetiyle değerlenir, fark ayrı hesaba (780) yazılır. Altın `153 01 000 Has Altın`'da izlenir, gram açıklamada durur, dışarı giden altın 950/951 nazım hesaplarda.
- **Dezy** (rakip): hurda alışını *"has gram ve TL olarak çift kayıt eder"*; fişte brüt gram, ayar, milyem, has gram, kur ve ödenen tutar birlikte.
- **CetaSoft** (rakip): cari ve emanet karşılıkları **4 kur ile paralel** (TL, Has, USD, EURO).
- **Pusula** (rakip, bizim ekran ölçümümüz): `14 HURDASI · 2,55 g × 2.156,8627 TL/g · 5.500,00 TL` satırı KASA/AKBANK/AÇIK HESAP ile **aynı grid'de**, **FARK 0,00 TL** kuralıyla kapanıyor; aynı kalem ana ekranda gram (Barkodlu Stoklar) ve has (Açık Pozisyon) olarak yaşıyor.
  - *Kanıt düzeyi: ekran envanteri — Pusula'nın veri modeli kamuya açık değil, veritabanında tek satır mı iki satır mı yazdığı ÇIKARIMDIR.*

**Rakip kanıtından çıkan bağlayıcı sonuç:** fişin **denkliği TL üzerinden** kurulur. Altın bacağının bir **TL tutarı olmak ZORUNDADIR**; salt gram bacağı fişi denkleştiremez. Bu, bizim `OdemeSepeti.Coz`'daki "bacak toplamı = fiş toplamı, TOLERANS YOK" kuralıyla birebir uyumludur.

### 1.5 Belge zinciri — VUK md. 234

Nihai tüketiciden eski altın alımı **gider pusulası** ile belgelenir; pusulada gram, ayar ve satıcı kimliği bulunur. **Altın bacağı BELGE ÜRETİR.** B-4'te altın bacağı seçilince ürün gider pusulası hattını **mutlaka** tetiklemelidir — bugünkü `HURDA_ALIS` zaten tetikliyor.

**Bayraklanan mevzuat çelişkisi ÇÖZÜLDÜ (ölçümle):** "gider pusulası ile halktan alınan altınlarda GVK md. 94/13 gereği hiçbir tevkifat yapılmaz" iddiası ile bizim %2 stopajımız **çelişmiyor**. Ölçüm:

```
GiderPusulasiKapisi.cs:43  EsnafMuafStopajOran       = 0.02m
GiderPusulasiKapisi.cs:46  NihaiTuketiciStopajOran   = 0m   // "tevkifat yok"
GiderPusulasiKapisi.cs:91  StopajOran(saticiTipi) => EsnafMuaf → 0.02 · NihaiTuketici → 0 · _ → 0
```

Perakende kapsamda (müşteri = nihai tüketici) stopaj **zaten 0'dır**; %2 yalnız esnaf muaflığından yararlanan satıcıya uygulanır ve oran **yapılandırmadan okunur (ADR-0005)**, koda gömülü değildir. Çelişki yoktur; iddia farklı satıcı tipini konuşuyordu.

---

## 2. KUR SORUSU — TL karşılığı hangi kurla yazılır?

### 2.1 Mevzuatın söylediği ve SÖYLEMEDİĞİ

| Ne | Durum |
|---|---|
| **Dönem sonu DEĞERLEME** kuru | VUK md. 274/A: **borsa rayici**; yoksa veya muvazaalı ise maliyet bedeli. |
| **Günlük ALIM fiyatı** | **Mevzuatta belirlenmemiştir — SERBESTTİR.** Hurda alımı için İKO kuru mu, BİST Kıymetli Madenler Piyasası rayici mi kullanılacağı hiçbir hükümde yazmaz (§ 7 md. 5). |

### 2.2 Ölçülen bugünkü davranış (iddia değil, kod)

```
SatisModulu.cs:891   var hasGrami = HasHesap.ToHasGram(dto.GramAgirlik, ayar);
SatisModulu.cs:892   var toplamTl = Yuvarla2(hasGrami * dto.BirimFiyatTlHasGram);
SatisModulu.cs:883   await IslemDogrulayici.KurDogrula(db, dto.KurSnapshotId, islem.TenantId);
Entities.cs:341      [Column("altin_has_fiyati", ...)] public decimal AltinHasFiyati   // TEK fiyat
```

**Üç ölçülmüş gerçek:**
1. Hurdanın TL değeri **kur kaydından değil, fişteki `BirimFiyatTlHasGram` alanından** gelir — yani **dükkânın kendi alış fiyatından**, HAS GRAM başına.
2. `KurSnapshotId` **zorunludur ve doğrulanır**, ama fiyatı o belirlemez; kur kaydı **referans ve dondurma** görevi görür.
3. `kur_snapshots` tablosunda **tek bir `altin_has_fiyati` vardır** — ayrı alış/satış (spread) kolonu **YOKTUR**.

### 2.3 KARAR — KUR KURALI (bağlayıcı)

> **Altın bacağının TL karşılığı, İŞLEM ANINDA, dükkânın kendi HAS GRAM başına hurda alış fiyatıyla hesaplanır ve satışla AYNI `KurSnapshotId` ile dondurulur. Gün sonu kuru kullanılmaz; fiş sonradan yeniden değerlenmez.**

Gerekçe (üçü birden):
- **Mevzuat:** günlük alım fiyatı serbesttir; "borsa rayici" yalnız dönem sonu değerlemesi içindir → işlem anındaki dükkân fiyatı mevzuata aykırı değildir.
- **Ölçüm:** bugünkü `HURDA_ALIS` zaten böyle çalışıyor; kurucunun *"bugünkü yolu ölçüp aynısını yap"* talimatı karşılanmıştır.
- **Mimari:** tek fişte iki farklı kur kaydı kullanılırsa satışın hası ile alışın hası farklı anlara demirlenir ve fiş **kendi içinde tutarsız** olur.

**Yardımcı kurallar — hepsi bağlayıcı:**

| # | Kural |
|---|---|
| K-KUR-1 | Bir fişte **TEK** `KurSnapshotId` vardır; SATIS ve HURDA_ALIS hareketleri **aynısını** taşır. |
| K-KUR-2 | `KurSnapshotId` **ZORUNLUDUR** ve `IptalEdildi = true` kayıt kabul edilmez (fail-closed). |
| K-KUR-3 | **Alış fiyatı UYDURULMAZ.** Sistem `altin_has_fiyati`'ndan kendiliğinden bir **iskonto/spread türetemez** — o sayı dükkânın ticari kararıdır, sistemin bilgisi değildir (Anayasa md. 1). |
| K-KUR-4 | **TUTUCU VARSAYILAN:** `Hurda:AlisIskontoOran` yapılandırma alanının **varsayılanı `null`**'dur. `null` iken alan **boş açılır ve elle girilmesi ZORUNLUDUR**; ekran kur kaydındaki has fiyatını yalnız **etiketli referans** olarak gösterir, alana yazmaz. Oran yapılandırmada set edilirse ön-dolgu `has_fiyat × (1 − oran)` olur ve **düzenlenebilir kalır.** |
| K-KUR-5 | **Dönem sonu yeniden değerleme (VUK md. 274/A) KAPSAM DIŞIDIR ve uydurulmaz.** Kolon ve kur kaydı bu kancayı zaten mümkün kılıyor; B-4 bunu **uygulamaz**, ayrı paket olarak açık kalır. |

---

## 3. MEVCUT DAVRANIŞLA UYUM

### 3.1 `HURDA_ALIS` — kararımızla **TAM UYUMLU**, aynen kullanılacak

Ölçülen (SatisModulu.cs:936-980): **tek `Hareket` satırı, iki bacak.**

```
TlTutari  = Yuvarla2(-odenecek)   // kasadan çıkış / borç (stopaj sonrası NET)   :958
HasGrami  = Yuvarla6(hasGrami)    // has gram girişi (fiziki akış)               :959
Maden     = Ayarlar.MadenBul(ayar.Id) ?? Maden.Altin                            :961
OdemeTuru = odenen <= 0 ? "VERESIYE" : odenen >= odenecek ? dto.OdemeTuru : "KARMA"
KurSnapshotId zorunlu · gider pusulası + MASAK + cari + kasa AYNI transaction'da
```

Ek ölçülmüş kurallar — **korunacak**:

| Kural | Kanıt | B-4'e etkisi |
|---|---|---|
| **Has bacağı CARİYE ASLA yazılmaz.** *"hurda satın alınmıştır, müşterinin altın alacağı doğmaz"* | SatisModulu.cs:876-878, :975-976 (`CariBakiyeGuncelle(..., -kalan, 0m, ...)`) | Karma ödemede altın bacağı cariyi **gramla alacaklandırmaz**; TL'ye çevrilip fişi kapatır. |
| MASAK eşiği **BRÜT** işlem tutarına bakar | SatisModulu.cs:894-896 | B-4 eşiği **fişin brüt toplamından** değerlendirir; *"müşteri sadece 10.000 TL nakit verdi"* diye eşik atlanmaz. |
| Tevsik ölçüsü de **BRÜT** işlem tutarı | SatisModulu.cs:918-930 (`islemToplamTutari: toplamTl`) | Aynı ölçü korunur. |
| Fiyatlama **HAS GRAM** başınadır, brüt gram başına değil | SatisModulu.cs:891-892 | B-4 aynı formülü çağırır; ayrı hesap yolu açılırsa iki farklı yuvarlama doğar. |
| Kıymetsiz ayarda has 0 döner — sahte has üretilmez | Kuyumcu.Core/Domain/Ayarlar.cs:235-236 | Aynen. |

### 3.2 Tek etiketli `OdemeTuru = "ALTIN"` — **KARARIMIZLA ÇELİŞİYOR ve KANITLI BİR PARA HATASIDIR**

**Ölçüm (bu turda kod okunarak birebir doğrulandı):**

| Adım | Kanıt | Sonuç |
|---|---|---|
| `"ALTIN"` DTO'da **kabul ediliyor** | `SatisModulu.cs:59` → `[Required, RegularExpression("NAKIT\|KART\|HAVALE\|ALTIN\|VERESIYE")]` | Uç bu değeri **reddetmiyor**. |
| `"ALTIN"` sabit listede var | `Entities.cs:112` → `OdemeTurleri = ["NAKIT","KART","HAVALE","ALTIN","DIGER"]` | Defter değeri geçerli. |
| `"VERESIYE"` olmadığı için **kasa ZORUNLU** ve TRY doğrulanıyor | `SatisModulu.cs:664-670` | TL kasası devreye giriyor. |
| Ödeme izi **boş** | `SatisModulu.cs:700-703` → `OdemeIzi.Etiketten(...)`; `HareketFabrikasi.cs:58-63` → `_ => Yok` | Nakit 0, veresiye 0 → **tevsik kapısı hiç tetiklenmiyor.** |
| Hareket bacakları | `SatisModulu.cs:715-716` → `TlTutari = +toplamTl`, `HasGrami = −toplamHas` | **Yalnız SATILAN ürünün** stok çıkışı. |
| Kasa | `SatisModulu.cs:753-757` → `KasaGuncelle(kasa, +toplamTl, ...)` | **TL kasası fişin TAMAMI kadar artıyor.** |
| Müşterinin **getirdiği** altın | `grep -rn ALTIN dotnet/Kuyumcu.Api --include=*.cs` → ALTIN için **özel dal YOK** | **Hiçbir has girişi yazılmıyor.** |

> **BULGU (KRİTİK, kanıtlı):** Bugün `OdemeTuru = "ALTIN"` etiketi fiilen şu demektir: **"nakit gibi kasaya TL yaz, ama tevsik kapısını tetikleme."** Sonuç: **kasada olmayan TL varmış gibi görünür**, **alınan altın stok/has olarak hiç görünmez** ve **459 s. VUK tevsik kümülatifi sessizce eksik kalır.**

Bu boşluk kod tabanında **zaten belgelenmiştir** ve bilinçli olarak üstüne inşa edilmemiştir:

```
OdemeSepeti.cs:9-16   ⚠ ALTIN BİLEREK YOKTUR. Tek etiketli ALTIN yolu bugün TL kasasına TL
                       yazıyor ve tevsik kapısını hiç tetiklemiyor; bunun bilinçli bir muhasebe
                       kararı mı yoksa boşluk mu olduğu belgelenmemiştir. Belgelenmemiş bir
                       davranışın üstüne bacak inşa edilmez (Anayasa md.1).
OdemeSepeti.cs:16     [Required, RegularExpression("NAKIT|KART|HAVALE|VERESIYE|DOVIZ")]   ← ALTIN yok
OdemeKalemi.cs:71-74  ⚠ ALTIN BİLEREK YOKTUR — OdemeBacagiDto.Arac ile AYNI liste.
Islemler.razor:1437   /// Sunucunun kabul ettiği bacak araçları — ALTIN YOKTUR (bkz. OdemeBacagiDto).
TevsikKapisi.cs:133-136 3. ALTIN İLE ÖDEME: OdemeTuru="ALTIN" kapıyı hiç tetiklemez ve nakit payı
                       0 yazılır. Altınla ödemenin tevsik karşısındaki niteliği mevzuattan
                       doğrulanmadı (araştırma sürüyor, 2026-09-14).
```

**`Islemler.razor:1437` bir ekran eksiği DEĞİLDİR** — sunucu sözleşmesinin ve o mühürlü kararın yansımasıdır. Bu belge, o kararın beklediği belgelemeyi **üretir ve boşluğu kapatır.**

**KARAR (Mehmet'in KOŞULU):**

> **Tek etiketli `OdemeTuru = "ALTIN"` yolu YANLIŞTIR ve B-4 bu yolu TAKLİT ETMEZ. B-4 açılmadan ÖNCE kapatılır; aksi hâlde hata çoğaltılmış olur.**

### 3.3 Değişimin GÖÇÜ — "mevcut hâli bozulmasın" şartı nasıl karşılanır

Defter **yerinde değişmez**; geçmiş satır ne silinir ne düzeltilir. Göç beş maddedir:

| # | Madde | Neden |
|---|---|---|
| **G-1** | `SatisDto.OdemeTuru` regex'inden **`ALTIN` çıkarılır** (`SatisModulu.cs:59`). Yeni istek 400 döner; hata metni kullanıcıyı **altın bacağına** yönlendirir (ör. *"Altınla ödeme artık ödeme bacağı olarak girilir: fişe ALTIN bacağı ekleyin."*). | Yeni yanlış kayıt **doğmaz** (fail-closed). |
| **G-2** | `Sabitler.OdemeTurleri` içinde **`"ALTIN"` KALIR** (`Entities.cs:112`). | Geçmiş satırlar okunabilir kalır; tür kapısı (`HareketFabrikasi.cs:~95`) onları 400'e düşürmez. Listeden silmek **geçmişi geçersizleştirirdi**. |
| **G-3** | **ÖNCE SAY, SONRA KONUŞ:** canlı veride `hareketler WHERE odeme_turu='ALTIN'` **sayılır** ve sevk raporuna sayısıyla yazılır. **Sayı bilinmeden G-1 uygulanmaz.** Sayı bu belgede **ÖLÇÜLMEMİŞTİR** — bu bir açık kalemdir, tahmin edilmez. | Anayasa md. 1. |
| **G-4** | Geçmiş `ALTIN` satırları **onarılmaz.** Raporlarda **ayrı sayılır ve "ölçülmemiş yol" rozetiyle** gösterilir; sessizce toplanmaz. Düzeltme gerekirse yolu **storno/düzeltme politikasıdır** (`Modules/Meta/DuzeltmePolitikasi.cs`), toplu UPDATE değil. | Defter append-only; `DenetimAppendOnlyTestleri` bunu zaten zorluyor. |
| **G-5** | `SatisRaporlari.cs:155`'teki ödeme aracı kırılımı (`NAKIT/KART/HAVALE/DOVIZ/ALTIN`) **kasa_hareketleri bacaklarından** okur. Yeni `ALTIN` bacağı **kasaya girmediği için** (§ 4 md. 6) bu kırılımda kendiliğinden görünmez → kırılıma **ayrı bir "ALTIN (mahsup)" kalemi** `hareketler.tur='HURDA_ALIS'` + fiş bağı üzerinden eklenir. | Aksi hâlde rapor altın bacağını **hiç göstermez** ve kullanıcı ödemenin nereye gittiğini göremez. |

### 3.4 `TAKAS` — hazır şablon, bozulmaz

`TAKAS` bugün çalışan **tek** "müşteri eski altın veriyor" yoludur ve kararımızla uyumludur:

```
SatisModulu.cs:2043-2047   TlTutari = farkTl                        // yeni değer − eski değer
                           HasGrami = Yuvarla6(eskiHas − yeniUrunHas)  // NET fiziki has
SatisModulu.cs:1978-1988   K20-3: maden değiştiren takas REDDEDİLİR
                           → "işlemi hurda alışı ve satış olarak ayrı ayrı girin"
```

**B-4 TAKAS'ı DEĞİŞTİRMEZ.** İkisinin işbölümü nettir ve belgelenir:

| | TAKAS | B-4 ALTIN BACAĞI |
|---|---|---|
| Ne zaman | Aynı madende **mal ↔ mal** değişimi | Karma ödeme: **nakit + eski altın** (+ kart/havale/veresiye) |
| Kaç hareket | **1** (net) | **2** (SATIS brüt + HURDA_ALIS) |
| KDV özel matrahı | Net üzerinden (mevcut davranış) | **Brüt satış üzerinden — § 1.3 sırası** |
| Gider pusulası | (bugün yok) | **ZORUNLU** |

> **AÇIK KALEM (yeni, bu turda doğdu):** TAKAS'ta gider pusulası hattının bulunmaması ile B-4'te zorunlu olması bir **asimetridir**. Mevzuat açısından ikisi de VUK md. 234 kapsamında görünür. **Bu belge TAKAS'ı DEĞİŞTİRMEZ** (izinsiz değişiklik yok, ayrı karar) ama asimetriyi kayda geçirir ve mali müşavir sorusuna § 7.2'de ekler.

---

## 4. B-4 İÇİN BAĞLAYICI ŞARTNAME

> Uygulayıcı bu listeyi **doğrudan** alır. Her madde ya kod ya testtir; hiçbiri yorum değildir.

### 4.1 Sözleşme (DTO / liste)

| # | Madde |
|---|---|
| **B4-1** | `OdemeBacagiDto.Arac` regex'i `NAKIT\|KART\|HAVALE\|VERESIYE\|DOVIZ\|ALTIN` olur (`OdemeSepeti.cs:16`). |
| **B4-2** | `OdemeAraci.Tumu` dizisine `Altin` eklenir (`OdemeKalemi.cs:~91`). **B4-1 ve B4-2 AYNI commit'te değişir** — ayrışırsa sunucunun kabul ettiği bacak kalem olarak yazılamaz ve `FIS_TOPLAM_ODEME_KALEMI_FARKI` kapısı kırılır. |
| **B4-3** | `Islemler.razor:1437` `BacakAraclari` listesine `("ALTIN", Dil.T.Islem.OdemeAltin)` eklenir; sözlük anahtarı `Kuyumcu.Core/I18n/Sozlukler.cs`'e eklenir (TR + mevcut diller). |
| **B4-4** | ALTIN bacağı **yeni alanlar** taşır: `AyarId` (zorunlu), `GramAgirlik` (> 0, zorunlu), `BirimFiyatTlHasGram` (> 0, zorunlu — K-KUR-4), `Satici` (`HurdaSaticiDto`, zorunlu). Bu alanlar **YALNIZ** ALTIN bacağında verilebilir; başka bacakta gelirse 400 (DOVIZ'in `ParaBirimi`/`DovizTutar` kuralıyla aynı sertlik, `OdemeSepeti.cs:153-159` deseni). |
| **B4-5** | ALTIN bacağında `KasaId` **VERİLEMEZ** (400). Para kasadan geçmez — VERESIYE bacağıyla aynı kural. |
| **B4-6** | Bir fişte **EN FAZLA BİR** ALTIN bacağı olabilir (VERESIYE kuralıyla simetrik, `OdemeSepeti.cs:126-128` deseni). Gerekçe: iki ayrı ayarın hası tek pusulaya sığmaz; iki parça altın **iki ayrı fiş** ya da mevcut `HurdaAlis` ucudur. |
| **B4-7** | `EnFazlaBacak = 5` **değişmez**. |

### 4.2 Hesap ve denklik

| # | Madde |
|---|---|
| **B4-8** | `hasGrami = HasHesap.ToHasGram(GramAgirlik, ayar)` — **tek kaynak**, ayrı formül yazılmaz. |
| **B4-9** | `brutTl = Yuvarla2(hasGrami × BirimFiyatTlHasGram)` — **HAS GRAM başına**, brüt gram başına değil. |
| **B4-10** | `stopajOran = hurda.StopajOran(saticiTipi)` · `stopajTutar = Yuvarla2(brutTl × stopajOran)` · `netTl = Yuvarla2(brutTl − stopajTutar)`. |
| **B4-11** | **Bacağın `TutarTl` değeri `netTl`'dir** (müşterinin eline geçen değer = fişi kapatan değer). İstemcinin gönderdiği `TutarTl` sunucununkine **KURUŞU KURUŞUNA** eşit değilse fiş **REDDEDİLİR** (DOVIZ bacağındaki aynı sertlik, `OdemeSepeti.cs:~195`). |
| **B4-12** | `brutTl` ve `stopajTutar` **gider pusulasına** yazılır (belge verisi), fiş denkliğine girmez. |
| **B4-13** | `CozulmusSepet.NakitToplam`'a ALTIN payı **KATILMAZ**; `VeresiyeToplam`'a da katılmaz. (DOVIZ'in nakde katılması **özel bir karardır ve korunur**; altın nakit değildir.) |
| **B4-14** | Bacak toplamı = fiş toplamı kuralı **TOLERANSSIZ** korunur; ALTIN bacağı `netTl` ile toplama girer. |

### 4.3 Deftere yazma

| # | Madde |
|---|---|
| **B4-15** | Deftere yazmanın **TEK KAPISI `HareketFabrikasi.Ekle`'dir.** Yeni yazma yolu **AÇILAMAZ** (`denetim.sh` `Hareketler.Add` çağrısını yalnız orada sayar). |
| **B4-16** | **SATIS hareketi BRÜT yazılır:** `TlTutari = +fisToplam` (altın mahsubu **düşülmeden**), `HasGrami = −satılan ürün hası`, `Maden = ürünün ayarından`. § 1.3 sırası. |
| **B4-17** | **HURDA_ALIS hareketi AYRI satır olarak, AYNI transaction'da yazılır:** `Tur="HURDA_ALIS"`, `TlTutari = Yuvarla2(−netTl)`, `HasGrami = Yuvarla6(+hasGrami)`, `Maden = Ayarlar.MadenBul(ayar.Id) ?? Maden.Altin`, `KurSnapshotId` = **fişin kur kaydı**, `CariId` = fişin carisi, `Adet = null` (gramla alınır — uydurulmaz). |
| **B4-18** | Bu HURDA_ALIS satırının ödeme izi **`OdemeIzi.Yok` (0, 0, null)**'dur: para el değiştirmez (mahsup), alacak doğmaz. **`OdemeIzi` sözleşmesine altın payı alanı EKLENMEZ** — `HareketFabrikasi.cs:5-10` gerekçesi (*"Kart/havale/altın payı burada tutulmaz: onlar ne nakittir ne alacak"*) **geçerliliğini korur.** |
| **B4-19** | **Has bacağı CARİYE YAZILMAZ.** `CariBakiyeGuncelle` çağrısında `hasGramDelta = 0m` (K3). Altın bacağı **kasaya da yazmaz.** |
| **B4-20** | **Gider pusulası ZORUNLUDUR** ve **AYNI commit'te** doğar (`hurda.Dogrula` + `FaturaNoUretici.SiradakiPusula` + `hurda.Kaydet`). *"Alım var, belgesi yok"* hâli doğamaz. Satıcı kimliği **tutar eşiği olmadan** zorunludur (VUK md. 234). |
| **B4-21** | **MASAK:** `DogrulaKumulatif` fişin **BRÜT toplamıyla** çağrılır (bugünkü davranış korunur); altın bacağı eşiği düşürmez. |
| **B4-22** | **Tevsik (459 s. VUK):** `sepet.NakitToplam > 0` ise fişin **brüt toplamı** `islemToplamTutari` olarak verilir. **Altın payı nakde sayılmaz** — ama fişin brütü hadde bakar, yani "yarısını altın verelim" ile had **delinemez.** Bu, TevsikKapisi.cs:133-136'daki açık maddeyi **kapatır**: altınla ödeme kapıyı **fişin brütü üzerinden** tetikler. |
| **B4-23** | `dto.KurSnapshotId` zorunlu ve `IptalEdildi = false` olmalı (K-KUR-2). |
| **B4-24** | **MİGRATION GEREKMEZ.** Ölçüldü: `hareketler` zaten `has_grami decimal(15,6)` + `tl_tutari` + `maden` + `kur_snapshot_id` taşıyor; `cari_bakiyeler` zaten `tl_bakiye decimal(15,2)` + `has_gram_bakiye decimal(15,6)` **ayrı kolonlarına** sahip (`Entities.cs:468-469`). *(D3'teki "migration ÖLÇÜLEMEDİ" kalemi bununla KAPANDI — yeni kolon yok. `odeme_kalemleri` tarafında ALTIN aracı mevcut kolonlara sığar; uygulayıcı bunu şema anlığıyla tekrar doğrular.)* |

### 4.4 Yapılandırma (Anayasa md. 1 — doğrulanamayan hüküm koda gömülmez)

| # | Madde | Varsayılan |
|---|---|---|
| **B4-25** | `Hurda:EsnafMuafStopajOran` | `0.02` (mevcut, korunur) |
| **B4-26** | `Hurda:NihaiTuketiciStopajOran` | `0m` (mevcut, korunur) |
| **B4-27** | `Hurda:AlisIskontoOran` — **YENİ** | **`null` = ön-dolgu YOK, elle giriş zorunlu** (K-KUR-4) |
| **B4-28** | `Kdv:OzelMatrahKodu` | **`""` = KAPALI** (mevcut, `Program.cs:159-162`). B-4 bunu **açmaz**; özel matrah kararı fatura tarafındadır. |
| **B4-29** | `Masak:KimlikTespitEsigi` | mevcut değer **değişmez**; 185.000 TL eşiği rehberden **gözle doğrulanmadan** koda yazılmaz (§ 7 md. 3). |
| **B4-30** | **Hiçbir mevzuat oranı/eşiği B-4 ile koda sabitlenmez.** Yeni sabit eklenirse `denetim.sh` ADR-0005 kapısı kırmızı dönmelidir. |

### 4.5 Ekran (Ertuğ)

| # | Madde |
|---|---|
| **B4-31** | ALTIN bacağı seçilince açılan alanlar: **ayar (seçim) · gram (sayı) · TL/has-gram (sayı) → türetilen has gram (salt okunur) → türetilen TL (salt okunur)**. Sıra bu olacak: **gram + ayar → milyem → has gram → fiyat → TL.** Kullanıcı **TL'yi doğrudan yazmaz.** |
| **B4-32** | Satıcı kimliği alanları (VUK md. 234) bacak açılır açılmaz görünür; eksikse **kaydet düğmesi çalışmaz** (fail-closed, sunucuya gitmeden). |
| **B4-33** | `FARK` göstergesi (bacak toplamı − fiş toplamı) **0,00 TL** olmadan kaydedilemez — Pusula teamülüyle aynı. |
| **B4-34** | **Her veri girişi düzenlenebilir** (kurucu şartı): gram, ayar ve birim fiyat alanları kayıttan önce serbestçe değiştirilebilir; her değişiklikte has ve TL **anında** yeniden türetilir. |
| **B4-35** | Fişte ALTIN bacağı varken ekran, **iki belge doğacağını** (satış fişi + gider pusulası) kullanıcıya **kayıttan önce** söyler. |

---

## 5. TEST LİSTESİ (Aşkın — hepsi yazılacak, hepsi yeşil olmadan sevk yok)

Hedef dosyalar: `Kuyumcu.Tests/KarmaOdemeEkranTestleri.cs`, `MuhasebeTestleri.cs`, `GiderPusulasiTestleri.cs`, `P23OdemeKalemiTestleri.cs` + yeni `B4AltinBacagiTestleri.cs`.

| # | Test | Beklenen |
|---|---|---|
| T-1 | `AltinBacagi_IkiDeftere_Birden_Yazar` | Fiş sonrası **2 hareket**: `SATIS` (`TlTutari=+brüt`, `HasGrami=−ürün hası`) ve `HURDA_ALIS` (`TlTutari=−netTl`, `HasGrami=+eski has`). |
| T-2 | `AltinBacagi_Satis_BRUT_Yazilir` | Altın mahsubu **satışın `TlTutari`'sini DÜŞÜRMEZ**. (§ 1.3 / KDVK 23/e) |
| T-3 | `AltinBacagi_Cariye_Has_Yazmaz` | `cari_bakiyeler.has_gram_bakiye` **değişmez** (K3). |
| T-4 | `AltinBacagi_Kasaya_Yazmaz` | Fiş sonrası TL kasası **yalnız nakit/kart/havale/döviz bacakları kadar** artar; altın payı kadar **artmaz**. *(§ 3.2'deki hatanın doğrudan testi.)* |
| T-5 | `AltinBacagi_GiderPusulasi_Ayni_Committe_Dogar` | `gider_pusulalari` satırı + `pusula_no` var; pusula **BRÜT** taşır, hareket **NET**. |
| T-6 | `AltinBacagi_Satici_Kimligi_Yoksa_400` | Tutar ne olursa olsun reddedilir (VUK md. 234). |
| T-7 | `AltinBacagi_Fis_Denkligi_Toleranssiz` | `netTl` bir kuruş şaşarsa **400**; hiçbir yazma başlamaz. |
| T-8 | `AltinBacagi_Istemci_TL_si_Sunucununkine_Esit_Degilse_400` | B4-11. |
| T-9 | `AltinBacagi_Nakit_Toplamina_Katilmaz` | `NakitTutar` yalnız NAKIT + DOVIZ paylarını taşır. |
| T-10 | `AltinBacagi_Tevsik_Fis_Brutu_Uzerinden_Tetiklenir` | Brüt had üstünde + nakit bacak var → **400**; *"yarısını altın verelim"* haddi delemez. |
| T-11 | `AltinBacagi_Masak_Esigi_Bruten_Olculur` | Nakit payı düşük ama fiş brütü eşik üstü → MASAK kimlik tespiti **istenir**. |
| T-12 | `AltinBacagi_Maden_Ayardan_Gelir_Gumus_Altina_Karismaz` | Gümüş bacağı **altın has toplamına girmez** (K20-2, `Entities.cs:715-718`). |
| T-13 | `AltinBacagi_Kiymetsiz_Ayarda_Has_Sifir` | `HasHesaplanir=false` → `HasGrami = 0`; sahte has yok. |
| T-14 | `AltinBacagi_Tek_KurSnapshot` | SATIS ve HURDA_ALIS satırlarının `kur_snapshot_id` değeri **AYNI** (K-KUR-1). |
| T-15 | `AltinBacagi_Iptal_Edilmis_Kur_Reddedilir` | `IptalEdildi=true` → **400** (K-KUR-2). |
| T-16 | `AltinBacagi_Birden_Fazla_Bacak_Reddedilir` | İki ALTIN bacağı → **400** (B4-6). |
| T-17 | `AltinBacagi_KasaId_Verilemez` | **400** (B4-5). |
| T-18 | `Altin_Disi_Bacakta_Ayar_Gram_Alanlari_Reddedilir` | NAKIT bacağında `GramAgirlik` → **400** (B4-4). |
| T-19 | `OdemeKalemi_Altin_Araci_Yazilabilir` | `OdemeAraci.Gecerli("ALTIN") == true`; fiş toplamı = kalem toplamı (B4-2). |
| T-20 | `Tek_Etiketli_ALTIN_OdemeTuru_Artik_Reddedilir` | `POST /satis` `OdemeTuru="ALTIN"` → **400** ve hata metni altın bacağına yönlendirir (G-1). |
| T-21 | `Sabitler_OdemeTurleri_ALTIN_Icerir` | Geçmiş satırlar okunabilir kalır; tür kapısı onları 400'e düşürmez (G-2). |
| T-22 | `AltinBacagi_Tum_Yazmalar_Tek_Transactionda` | Pusula üretimi patlarsa **hiçbir** hareket/kasa/cari satırı kalmaz. |
| T-23 | `AltinBacagi_Hurda_Has_Bakiyesine_Dusar` | `HurdaStokKapisi.Bakiye(...)` fiş sonrası `+hasGrami` artar (HURDA_ALIS listeye dahil). |
| T-24 | `AltinBacagi_Storno_Simetrik` | Fiş stornolanınca **iki hareket de** ters işaretle döner; hurda has bakiyesi ve kasa **başlangıca** eşitlenir. |
| T-25 | `AltinBacagi_Yapilandirma_Varsayilani_Tutucu` | `Hurda:AlisIskontoOran` set edilmemişken sunucu **fiyat türetmez**; alan boş gelirse **400** (K-KUR-3/4). |
| T-26 | `AltinBacagi_Ekran_Fark_Sifir_Olmadan_Kaydetmez` | Blazor ekran testi (B4-33). |
| T-27 | `AltinBacagi_Ekran_Alanlar_Duzenlenebilir` | Gram/ayar/fiyat değişince has ve TL **yeniden türetilir** (B4-34). |

**Kapılar:** `dotnet test` → **0 başarısız** · `dotnet/denetim.sh` → **TEMİZ** · 0 uyarı 0 hata · canlıda gerçek veriyle doğrulama · **test kiracısı silinir.**

---

## 6. MUTASYON LİSTESİ (kanıt — her biri KIRMIZI dönmeli)

`dotnet/mut.sh` ile uygulanır. Kırmızı dönmeyen mutasyon = **o kuralı koruyan test yok** demektir ve sevk durdurulur.

| # | Mutasyon | Kırmızı döndürmesi gereken test |
|---|---|---|
| M-1 | SATIS `TlTutari = +fisToplam` → `+(fisToplam − altinNetTl)` (satışı **net** yaz) | T-2 |
| M-2 | HURDA_ALIS satırının `HasGrami` yazımını **sil** (yalnız TL yaz) | T-1, T-23 |
| M-3 | HURDA_ALIS satırının `TlTutari` yazımını **sil** (yalnız has yaz) | T-1 |
| M-4 | `CariBakiyeGuncelle(..., hasGramDelta: 0m)` → `hasGrami` | T-3 |
| M-5 | ALTIN bacağı için `KasaGuncelle(kasa, +netTl, ...)` **ekle** | T-4 |
| M-6 | `CozulmusSepet.NakitToplam`'a altın payını **ekle** | T-9 |
| M-7 | `tevsik.Dogrula(... islemToplamTutari: fisToplam)` → `sepet.NakitToplam` | T-10 |
| M-8 | `masak.DogrulaKumulatif(... fisToplam)` → `fisToplam − altinNetTl` | T-11 |
| M-9 | Gider pusulası üretimini **koşullu** yap (`if (brutTl > esik)`) | T-5, T-6 |
| M-10 | Pusulaya **NET** yaz (brüt yerine) | T-5 |
| M-11 | Toleransı gevşet: `if (Math.Abs(bacakToplam − fisToplam) > 0.01m)` | T-7 |
| M-12 | `Maden = Maden.Altin` sabitle (ayardan türetme) | T-12 |
| M-13 | `HasHesap.ToHasGram` yerine `gram × ayar.Milyem / 1000` **elle** yaz (yuvarlamasız) | T-13 |
| M-14 | HURDA_ALIS satırına fişin değil **yeni** bir `KurSnapshotId` ver | T-14 |
| M-15 | `OdemeIzi.Yok` → `OdemeIzi.Nakitten(netTl)` | T-9, T-10 |
| M-16 | `SatisDto.OdemeTuru` regex'ine `ALTIN`'ı **geri ekle** | T-20 |
| M-17 | `Sabitler.OdemeTurleri`'nden `"ALTIN"`'ı **sil** | T-21 |
| M-18 | `Hurda:AlisIskontoOran` varsayılanını `0.02m` yap (sessiz spread uydur) | T-25 |
| M-19 | HURDA_ALIS yazımını transaction **dışına** al | T-22 |
| M-20 | ALTIN bacak sayısı kapısını **kaldır** | T-16 |

---

## 7. AÇIK KALAN — MALİ MÜŞAVİR TEYİDİ (K-10 ailesi)

> **Karar ERTELENMEZ.** Ekip kararını verdi (§ 0). Aşağıdakiler **doğrulama** olarak beklenir; her biri **yapılandırmadan okunur**, koda gömülü değildir (B4-25…B4-30) — cevap geldiğinde **tek satır** değişir, kod değişmez.

### 7.1 Mali müşavire sorulacak — TEK CÜMLE

> **"Müşterinin satış bedelinin bir kısmını ödemek için verdiği eski altın; (a) gider pusulalı ayrı bir hurda alımı sayılıp satış faturası BRÜT bedelle mi düzenlenmelidir, yoksa (b) satış bedelinden mahsup edilip fatura NET tutar üzerinden mi düzenlenmelidir?"**

Ekibin cevabı **(a)**'dır (KDVK md. 2/5 trampa + VUK md. 234 + KDVK md. 23/e birleşiminden). Bu çıkarım **tek bir hükümde yazılı değildir** (§ 7.2 md. 4).

### 7.2 Doğrulanamayanlar — adıyla listelenmiştir

| # | Kalem | Durum |
|---|---|---|
| 1 | **VUK Geçici md. 35** ("altın/gümüş esaslı muhasebe sistemine geçiş hakkı") | **DOĞRULANAMADI ve ÇÜRÜTÜLDÜ.** mevzuat.gov.tr'ye erişimde TLS sertifika hatası; GİB VUK sayfası tam metin döndürmedi. SMMM yayını bunun henüz bir **TALEP** olduğunu gösteriyor. **Bu iddiaya dayanılmadı.** |
| 2 | **KDVK md. 2/5, 17/4-g, 23/e** tam metni | İçerik **iki bağımsız** mali müşavir/YMM yayınından teyit edildi; GİB kanun sayfası gövde metni döndürmedi. *"Mevzuat metninden aynen alındı" DENEMEZ.* |
| 3 | **MASAK 185.000 TL kimlik tespiti eşiği** | Birincil rehber (ms.hmb.gov.tr PDF) indirildi, metni **otomatik çıkarılamadı**; eşik yalnız ikincil kaynaklardan okundu. **Güncel değeri rehberden gözle doğrulanmalı.** → B4-29: eşik **değiştirilmez**. |
| 4 | **Karma ödemede belge düzenine dair doğrudan GİB özelgesi** | **BULUNAMADI.** § 1.3 sırası üç hükmün **birleştirilmesiyle** çıkarılmıştır. → § 7.1'in tek cümlesi bunu sorar. |
| 5 | **Hurda alımında hangi KUR** (İKO kuru mu, BİST KMP rayici mi) | Mevzuatta **ayrıca belirlenmemiştir**; VUK md. 274/A yalnız **dönem sonu değerlemesi** için "borsa rayici" der. → K-KUR-3/4 tutucu varsayılanı bu belirsizliği karşılar. |
| 6 | **VUK md. 234'ün 7338 s. Kanun (2021) sonrası güncel metni** | *"zati eşyalarını satan kimselerden satın alınan altın, mücevher"* ibaresinin aynen durup durmadığı **doğrulanamadı**; elimizdeki alıntı **2017/2018** tarihli SMMM yayınından. |
| 7 | **459 s. VUK Tebliği karşısında trampa/takas** | Tebliğ metninde *"takas/trampa/ayni"* kelimeleri **geçmiyor**; kod bunu zaten yazıyor (`SatisModulu.cs:2011-2016`): *"Mali müşavir teyidi beklemektedir."* → B4-22 kapıyı **yalnız sıkılaştırır** (fişin brütü), gevşetmez. |
| 8 | **Kesilen stopajın vergi dairesine BORÇ olarak muhasebeleştirilmesi** | Üründe **YOK** (`SatisModulu.cs:906-908` açık kaydı). Bu sorunun kapsamı dışındadır, **B-4 ile birlikte açık kalır.** |
| 9 | **TAKAS'ta gider pusulası hattının olmaması** | B-4 ile doğan **asimetri** (§ 3.4). Ayrı karar konusudur; bu belge TAKAS'ı **değiştirmez.** |
| 10 | **Canlıda `odeme_turu='ALTIN'` satır sayısı** | **ÖLÇÜLMEMİŞTİR.** G-3 gereği **sayılmadan** G-1 uygulanmaz. |
| 11 | **Pusula'nın veritabanı davranışı** | Yalnız ekran görüntüsünden **çıkarım**; veri modeli kamuya açık değil. Kanıt düzeyi: ekran envanteri, kod değil. |
| 12 | Gramino, Milyem Plus, ZarifSoft, Terminal Gold, Kuyumex, KuyumPOS, Bakiyem | Tanıtım sayfalarında eski altının hangi deftere işlendiğine dair **tek cümle yok**. **Hiçbiri doğrulanmadı.** |
| 13 | `kadsis.com` (DenizBank KAD-SİS) | Sunucu erişilemedi (ECONNREFUSED). Destekleyici kaynak **doğrulanamadı.** |
| 14 | Toptancı-perakendeci hesabının **hangi oranda** gramla kapandığı | Sayısal/ankete dayalı kaynak **bulunamadı**; elde yalnız niteliksel esnaf beyanı var. |
| 15 | *"197 sayılı hesap"* kıymetli maden hesabıdır iddiası | **YANLIŞ — peşine düşülmedi.** TDHP 197 = Sayım ve Tesellüm Noksanları. `01900 Kıymetli Madenler` **bankalar** TDHP'sinin nazım hesabıdır, ticari işletmeninki değil. Has takibinin mevzuattaki yeri **153 alt hesapları + envanter miktar kaydıdır** (VUK md. 186: envanter saymak, **ölçmek, tartmak** ve değerlemektir). |

---

## 8. RİSK — yanlış defter seçilirse hangi rakam yanlış çıkar

| Yanlış seçim | Bozulan rakam | Nasıl bozulur | Yakalayan test |
|---|---|---|---|
| **(A) Yalnız has** | **Kâr/zarar · cari bakiye · KDV matrahı** | Satışın TL bedeli eksik kalır; gider pusulasına yazacak TL tutar doğmaz → **belgesiz alım**. Mali defter tutmaz. | T-1, T-5 |
| **(B) Yalnız TL** | **METAL POZİSYONU (açık pozisyon)** | Alınan altın stok/has olarak görünmez. Ons yükselince fiyat hareketi **kâr gibi** okunur; kuyumcu elinde olmayan metali **satar**. | T-1, T-12, T-23 |
| **Satışı NET yazmak** | **KDV matrahı — VERGİ HATASI** | Özel matrah (KDVK 23/e) eksik hesaplanır; beyan eksik çıkar. | **T-2** |
| **Altını kasaya TL yazmak** *(bugünkü `OdemeTuru="ALTIN"` hatası)* | **KASA BAKİYESİ + tevsik kümülatifi** | Kasada **olmayan TL** varmış gibi görünür; gün sonu sayımı tutmaz; 459 s. VUK haddi sessizce delinir. | **T-4, T-10, T-20** |
| **Has'ı cariye yazmak** | **CARİ BAKİYE** | Satın alınmış hurda, müşterinin **altın alacağı** gibi görünür; kuyumcu aynı altını iki kez borçlanır. | **T-3** |
| **Gümüşü altın hasına katmak** | **TOPLAM HAS (K20-2)** | 1 g gümüş ile 1 g altın aynı sayılır; pozisyon **şişer**. | **T-12** |
| **Gider pusulasını atlamak** | **Belge düzeni — VUK md. 234** | Belgesiz alım; denetimde maliyet **kabul edilmez**. | **T-5, T-6** |
| **Farklı kur kaydı kullanmak** | **Fişin iç tutarlılığı** | Satışın hası ile alışın hası farklı anlara demirlenir; kâr **yapay** çıkar. | **T-14** |
| **Sessiz spread uydurmak** | **Alış maliyeti → kâr** | Sistemin uydurduğu iskonto dükkânın gerçek fiyatı değildir; maliyet ve kâr **baştan yanlış** doğar (Anayasa md. 1 ihlali). | **T-25** |

**En pahalı tek risk:** *satışı net yazmak* (vergi hatası, geriye dönük düzeltilemez) ve *altını kasaya TL yazmak* (bugün canlıda **açık**). İkisini de **T-2** ve **T-4** doğrudan hedefler.

---

## 9. SEVK ÖNCESİ KONTROL LİSTESİ

- [ ] G-3: canlıda `odeme_turu='ALTIN'` satır sayısı **sayıldı** ve rapora yazıldı
- [ ] § 5'teki 27 test yazıldı, `dotnet test` **0 başarısız**
- [ ] § 6'daki 20 mutasyonun **hepsi KIRMIZI** döndü
- [ ] `dotnet/denetim.sh` **TEMİZ** (ADR-0005 sabit kapısı dahil)
- [ ] 0 uyarı / 0 hata
- [ ] Canlıda (5198) gerçek veriyle karma fiş doğrulandı: 2 hareket + 1 pusula + kasa yalnız nakit payı kadar arttı
- [ ] Test kiracısı **silindi**
- [ ] `SONRAKI-OTURUM-PROMPT.md` ve `ORKESTRATOR-DURUM.md` güncellendi
- [ ] § 7.1'in tek cümlesi mali müşavire **iletildi** (cevap beklenir, sevki durdurmaz)

---

*Belge sonu. Mühür: Tamer Bey. Karar bağlayıcıdır; değişikliği yeni bir ekip oyu gerektirir.*
