# PAKET YILDIZ-5B — uygulama raporu (10.10.2026)

Para ve banka 12 ekranının Yıldız yüzü: Kasalar, Kasa kuyruğu, POS cihazları, POS alacakları, Banka, Çek ve senet, Vade takvimi, Kur, Gider ve virman, Hurda ve ramat, Kasa raporu, Gider pusulaları.

Hasımsız paket. Kanıt bu rapordaki ölçümlerdir. Para, veri ve yasal davranışa dokunulmadı. Migration yok. Commit yok.

## Ortam
- API 5164, vekil 5165. CDP portları 9611–9618. Test veritabanı `kuyumcu_test_t3`.
- Kiracı `PAKETYILDIZ5B-TEST Tunalılar Kuyumculuk` (01M4KH4A974G39XS7ADZEYHT8X). `demo-tohumla.js --prova-kiraci --sema yildiz` ile kuruldu.
  - Ek tohum (ürünün kendi uçlarıyla): 2 evrak (çek Ziraat 23 Eki, senet 30 Eki), 2 vezne fişi (VEZNEDE), 1 bireysel hurda alımı (gider pusulası GP-2026-000001), 1 POS cihazı ve kartlı satış.
- Ham çıktılar `/private/tmp/kuyumcu-serit/pYILDIZ-5B/`. Kıyas tabloları ve görüntüler `ham/yildiz-5b/`.

## 1. Yapılanlar

Desen YILDIZ-5A ile aynı:
- Sayfa Yıldız'daysa tuval yüzü çizilir. Özgün ekran yalnız "Tüm ayrıntılar" (ya da eylemin gerektirdiği yerde) açılır (`data-yz-ozgun`).
- Öteki 8 şemada `YildizMi = false`. Öznitelik yazılmaz, DOM birebir aynı kalır.
- Yıldız yüzü istek atmaz ya da yalnız OKUR. Her düğme sayfanın MEVCUT geri çağrısıdır.

### Yeni ortak bileşenler (`Bilesenler/Yildiz/`)

| Bileşen | Tuval kalıbı | Ekranlar |
|---|---|---|
| `YildizOzet.razor` | özet: 104 px rakam + 52 px soluk ek (kuruş/₺ ayrı parça), 20 px cümle, b1/b2, sağda alta yaslı liste + bilgi satırı | Kasalar, Kur, Hurda ve ramat, Kasa raporu |
| `YildizBos.razor` | boş durum: 1×80 çizgi, 40 px ince cümle, 18/300 açıklama, tek hap | POS cihazları, Banka (kayıt yokken) |
| `YildizTakvim.razor` | takvim: ay ızgarası + "Bu ay" listesi | Vade takvimi |
| `YildizForm.razor` | form: .k satırları (sayfanın MEVCUT alan bileşenleri .yz-duzen içinde), "Ayrıntı ekle", Kaydet/Vazgeç, sağda not | Gider ve virman |
| YILDIZ-5A `YildizListe` (yalnız kullanıldı, değiştirilmedi) | liste | Kasa kuyruğu, POS alacakları, Çek ve senet, Gider pusulaları; POS cihazları ve Banka dolu hâli |

- Veri sınıfları ve saf biçimler `YildizOzetVerisi.cs` içinde:
  - `YildizParaBicim.BuyukPara` / `Tutar` — kuruş her zaman iki hane, eksi işareti U+2212, para birimi tutarla birlikte taşınır.
  - `YildizBant` — sayfanın MEVCUT bantları, aynı kimliklerle.

### Sayfalar
- 12 sayfanın razor'una yalnız Yıldız koşullu dal eklendi.
- Mantık sayfa başına yeni bir `*.Yildiz.cs` partial dosyasında.
- Çekmece ve onay pencereleri ORTAK kalır, böylece Yıldız düğmesi aynı çekmeceyi açar.

Ekran ekran:
- **Kasalar:**
  - Büyük rakam: ilk aktif TL kasasının defter bakiyesi.
  - Satırlar: Nakit/Kart/Havale tutarları `/kasalar/nakit-ayrimi`'den. İşlem sayıları `/kasalar/{id}/hareketler?limit=1000`'den; sınıra değerse "1000+" yazar.
  - Bilgi satırı: son kapanış (`/gun-kapanislari?limit=1`), bugün giren ve çıkan.
  - Düğmeler: "Günü kapat" ve "Hareket ekle" MEVCUT çekmeceleri açar. Bugün kapanmışsa kilitlenir ve sebebi yazar. "Geçmiş" MEVCUT geçmiş çekmecesini açar.
- **Kur:**
  - Rakam üst şeritle aynı biçimde: has satış, tam lira.
  - Satırlar: has (parite HAS alış/satış), 22 ayar gram, USD, EUR.
  - Gün farkı: önceki işletme gününün son geçerli kaydına göre.
  - "Şimdi güncelle" MEVCUT `SimdiCek`'i, "Elle gir" MEVCUT manuel çekmeceyi çağırır.
- **Hurda ve ramat:**
  - Rakam altın hurda has bakiyesidir. Satırlar son 5 hurda girişidir.
  - "Son gönderim" son HURDA_SATIS kaydıdır.
  - "Rafineriye gönder" özgün ramat formunu açar. "Hurda al" `/islemler?tur=HURDA_ALIS` adresine gider (Gider pusulaları ekranının mevcut yolu).
- **Kasa raporu:**
  - Rakam: dönem kapanışı − açılış devri.
  - Satırlar: dönem hareketleri (nakit giriş · kart · havale · nakit çıkış). Hareketler okunamazsa giriş/çıkış toplamına düşer.
  - Cümledeki gün sayısı yazıyla ("On dört günde …").
  - "Günü kapat" MEVCUT sayım formunu, "Tarih seç" özgün süzgeci açar.
- **Kasa kuyruğu:** VEZNEDE fişler. "Tahsil et" MEVCUT `Sec` + tahsil paneli. Durum VEZNEDE değilse kilitli, sebebi yazılı.
- **POS alacakları:** BEKLİYOR/KISMİ ve kalan > 0 olan satırlar. Alt not toplamı yazar ("Toplam … · komisyon kesildikten sonra bankaya geçer.").
- **Çek ve senet:**
  - Arama ve Hepsi/Alınan/Verilen sekmeleri yüklenmiş listeyi ekranda süzer.
  - "Çek al" MEVCUT `YeniAc`. Satıra basınca MEVCUT `DuzenleAc`.
  - "Tahsil et" MEVCUT `TahsilAc`. Geçiş izni yoksa kilitli, sebebi yazılı.
- **Gider pusulaları:** satıcı araması ekranda süzer. "Pusula düzenle" MEVCUT birincil yoldur (hurda alımı). Satıra basınca MEVCUT detay çekmecesi açılır (liste ucu; TCKN maskeli).
- **Vade takvimi:**
  - Bu ayın evrakları `/evraklar?vadeBas&vadeBit` ile okunur. Tahsil edilmiş ve karşılıksız evrak takvimde gösterilmez.
  - Vadeli gün o günün evrak listesine bağlantıdır (UX-S23).
  - Verilen evrak "· biz" ekiyle, eksi ve kırmızı yazılır.
- **Gider ve virman:**
  - Alanlar özgün formun bileşenleridir; kimlikler aynı (gd-kasa, gd-kategori, gd-tutar, gd-aciklama). Sayı kapısı, kilit sebebi ve `GiderKaydet` de aynı.
  - "Virman" seçimi özgün virman sekmesini açar. `?sekme=virman` bağlantısıyla gelinirse de özgün ekran açılır.
- **POS cihazları / Banka:** kayıt yokken tuvalin boş durumu. Dolu hâl liste kalıbında (tuvalde çizilmedi; bkz. § 8). Düzenle/Aktife al ve Hareketler mevcut yollardır.

### Dosya ve dil
- Sözlük: `Kuyumcu.Core/I18n/Sozlukler.Yildiz5B.cs`, TR + EN. Türkçe metinler tuvaldeki aynen.
- CSS: `tema-yildiz.css` içinde yalnız `/* YILDIZ-5B */ … /* YILDIZ-5B sonu */` bloğu.
  - Değerler tuvalden aynen.
  - Dar ekran kuralları:
    - ≤1360 px: Kasalar ve Hurda sabit sütunları kısalır.
    - ≤1000 px: tek sütun.
    - ≤480 px: büyük rakam ve eki tek satırda kalır (`min(104px,15vw)`, nowrap).

## 2. Tuval kıyası — `tuval-kiyas.mjs`, 1440×900, amblem nav kaynağı (§ 2a)

| Ekran (öğe) | BASİT koyu · açık | PRO koyu · açık |
|---|---|---|
| Kasalar (28) | 28/28 · 28/28 | 28/28 · 28/28 |
| Kur (28) | 28/28 · 28/28 | 28/28 · 28/28 |
| Hurda ve ramat (20) | 20/20 · 20/20 | 20/20 · 20/20 |
| Kasa raporu (27) | 27/27 · 27/27 | 27/27 · 27/27 |
| Kasa kuyruğu (22) | 22/22 · 22/22 | 22/22 · 22/22 |
| Çek ve senet (31) | 31/31 · 31/31 | 31/31 · 31/31 |
| Gider pusulaları (21) | 21/21 · 21/21 | 21/21 · 21/21 |
| POS cihazları, boş (8) | 8/8 · 8/8 | 8/8 · 8/8 |
| Banka, boş (8) | 8/8 · 8/8 | 8/8 · 8/8 |
| POS alacakları (15) | 15/15 · 15/15 | 15/15 · 15/15 |
| Vade takvimi (18) | 17/18 · 17/18 | 17/18 · 17/18 |
| Gider ve virman (14) | 9/14 · 10/14 | 10/14 · 10/14 |

Yazı ölçüleri (boy, ağırlık, harf aralığı, renk, aile) ve kuruş/₺ parçası 12 ekranın hepsinde AYNI. Kalan FARK'lar:
- **Vade takvimi `main` boyu (828 → 909):**
  - Tuvalde `.y` kutusu 900 px'te kırpılır (overflow hidden). Uygulamada ızgara ve altındaki "+ Tüm ayrıntılar" sayfayı uzatır.
  - Izgara, hücreler ve liste AYNI.
- **Gider ve virman:**
  - (a) `main` boyu ve b1/b2 alt kenarı +29 px. Sebep: tuvalde olmayan **Kategori** satırı. Sunucuda zorunlu alan; tek kategori varsa gizlenir (UX-S22).
  - Tuvaldeki **Belge** satırı ürün davranışı olmadığı için çizilmedi (§ 8).
  - (b) Not metni yüksekliği: tuvalin 2. paragrafı ("fatura fotoğrafı …") çizilmedi (§ 8).
  - Koyu BASİT'teki 5. fark ilk koşuma aittir: "Ayrıntı ekle" genişliği. Düzeltildi (`align-self: stretch`), sonraki koşumlarda AYNI.

Tuval HTML kusuru:
- Y_Kasalar, Y_Kasakuyrugu ve Y_Cekvesenet'te `a.sat` satırının içinde `a.b3` var. Tarayıcı iç içe `<a>`'yı böler; tuval görüntüsünde "Geçmiş/Tahsil et" hapı satırın altına tam genişlik düşer. Kurucunun gördüğü de budur.
- YILDIZ-5A ile aynı karar: kıyas için tuval kopyasında dış `<a>` `<div>` yapıldı (`/private/tmp/kuyumcu-serit/pYILDIZ-5B/tuval/`). Uygulamada hap satırın sağında.
- § 8 belirsiz eşleşme 1'e yazıldı.

Read ile bakılan görüntüler (2):
1. `ham/yildiz-5b/k1-kasalar-koyu-BASIT-yan-yana.png` — tuval | uygulama; iç içe `<a>` kusuru burada görüldü.
2. `g/m-375-dort.png` — Kasalar · Çek ve senet · Gider ve virman · Vade takvimi, 375 koyu.

## 3. Gerçek Chrome sıkışma ölçümü (`sikisma-olc.js`, 12 rota)

| Kombinasyon | K1 | K3 taşan/kırık | K5 TRY | K9 binen | K10c kesik | K12 | yatay |
|---|---|---|---|---|---|---|---|
| BASİT 1280 koyu+açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| BASİT 375 koyu+açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| BASİT 1440 açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| PRO 1280 koyu+açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| PRO 375 koyu+açık | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |

- **İlk ölçümde bulunup düzeltilenler:**
  - 1280: Kasalar ad sütunu taşıyordu, Hurda'da "Hurda alış · Bugün" kırılıyordu.
  - 375: Kasalar ",92 ₺" eki rakamdan kopuyordu.
- **K7 (Çek ve senet, Gider pusulaları):** başlık ile arama/hap alt hizası tuvaldeki yerleşimin kendisi (aynı ±14). YILDIZ-5A listeleriyle aynı.
- **K8 "r-zarara":** ölçerin yanlış pozitifi ("kâr-zarara").
- **`yildiz-tipo-tara.mjs`** (12 rota, 1440, koyu+açık, katman 0): 766 düğüm, **KURAL DIŞI 0**.
  - "Kesik yazı 2" iki ekranda da `label.gorsel-gizli`dir: arama bileşeninin ekran okuyucu etiketi, görünmez.
- 2 bayat CSS ölçümü ve bir kaybolan CSS düzenlemesi yaşandı (§ 9). Son tablo, önbellek temizlenip CSS bütünlüğü doğrulandıktan sonraki koşumdur.

## 4. Davranış — her ekranda ana işlem canlı (`davranis.mjs`, Yıldız BASİT) + SQL

| Ekran | Yıldız düğmesi → mevcut yol | Sonuç |
|---|---|---|
| Kasalar | "Hareket ekle" → çekmece → Kaydet | bant "Gelir 1.250 ₺ kaydedildi. Yeni bakiye: 205.867,92 ₺"; rakam 205.867,92 ₺ oldu. SQL: `kasa_hareketleri 1250.00 NAKIT "YILDIZ-5B canli hareket"` |
| Gider ve virman | Yıldız formu (gd-tutar, gd-aciklama) → Kaydet | bant "Gider kaydedildi: Elektrik · 1.250 ₺ · Ana Kasa → 204.617,92 ₺". SQL: `kasa_hareketleri −1250.00 NAKIT "Elektrik faturası · Ekim"` |
| Çek ve senet | "Tahsil et" → tahsil çekmecesi → Kaydet | bant "Tahsil edildi — …"; satır durumu "Tahsil edildi". SQL: `kiymetli_evraklar CEK TAHSIL_EDILDI 22174.73` |
| Kasa kuyruğu | "Tahsil et" → özgün tahsil paneli → Tahsil et | bant "Tahsil edildi (Defter fişi: 01M4KKGSS3…)". SQL: `vezne_fisleri TAHSILAT → TAHSIL` |
| Kur | "Şimdi güncelle" | `POST /kur/guncelle` 200 (doğrudan çağrıda da 200). Fiyat değişmediği için aynı anlık görüntü döndü (`kur_snapshots` 1). 3 sn'lik başarı bandı 4 sn beklemede kayboldu. |
| Gider pusulaları | satır → detay çekmecesi | çekmece "Gider pusulası — Detay / Düzenle … GP-2026-000001" |
| POS cihazları | "Düzenle" → çekmece | "Düzenle × Cihaz Adı* Banka* Komisyon (%)* …" |
| Banka | "Hesap ekle" → pencere | "Yeni hesap × Banka Adı* Hesap Adı* IBAN* …" |
| Hurda ve ramat | "Rafineriye gönder" | özgün ramat formu açıldı (`data-yz-ozgun="hurda"`) + "Özete dön" |
| Kasa raporu | "Günü kapat" | özgün sayım ekranı (`data-yz-ozgun="kasa-raporu"`) |
| Vade takvimi | gün 30 | `/evraklar?vadeBas=2026-10-30&vadeBit=2026-10-30` |
| POS alacakları | "+ Tüm ayrıntılar" | özgün ekran (`data-yz-ozgun="pos-tahsilatlar"`) |

## 5. 8 eski şema piksel farkı — `sema-fark.mjs` (kiracı + kullanıcı `platin`, 12 rota × 8 şema × açık/koyu)
- **BASİT: 192 karşılaştırma · FARK 0.**
- **PRO: 192 karşılaştırma · FARK 0.**

## 6. Yıldız koruması — Müşteriler / Tezgâh / Tüm ekranlar (4D eşlemeleri)
- BASİT koyu ve PRO açık koşuldu. Kalan farkların hiçbiri bu paketin dosyalarından değil.
- **Müşteriler (29/31):** iki fark da veri.
  - "Toptancı ·" öneki için Anadolu Döküm segmenti TOPTAN olmalı; 4D bunu PATCH ile kurmuştu, bu kiracıda kurulmadı.
  - Satır 3 borç rengi: bu paketin canlı işlemleri cari bakiyesini değiştirdi.
- **Tezgâh (35/36):** "ödeme1 kasa" satır yüksekliği 21,75 → 18,16.
  - Kaynak `wwwroot/css/yasal.css:109`: `.yz-tezgah-odeme-ad > .yz-tezgah-kasa { line-height: 1.2109 }`.
  - Bu dosya YASAL-KAPI-3 şeridine (t9) ait; `YildizTezgah.razor` da 22:34'te başka şeritçe değişti. Bu paket ikisine de dokunmadı. Sahibine devir: § 10.
- **Tüm ekranlar (66/67):** `main` boyu. Kare sayısı 41/39 (öteki paketlerin kareleri); 4D'de de aynıydı.
- **Bugün:** kod değişmedi. Senaryo gerektiren `hal-bugun` 4D'de de ölçülemedi.

## 7. Testler ve mutasyon

**Yeni testler:** `Kuyumcu.Tests/Yildiz5BTestleri.cs`, 12 test, filtreli koşum **12/12 yeşil**.
- `Y5B_01`: özet kalıbı.
- `Y5B_02`: ayrıntı görünümü ve B2Href.
- `Y5B_03`: boş durum.
- `Y5B_04`: takvim.
- `Y5B_05`: form.
- `Y5B_06`: para biçimi.
- `Y5B_07`: Kasalar saf kuralları.
- `Y5B_08`: Kasa raporu.
- `Y5B_09`: liste ekranlarının saf kuralları.
- `Y5B_10`: CSS ızgaraları ve kapsam (sabit renk 0).
- `Y5B_11`: sayfa dalları; öteki şemada özgün ekran aynen; Yıldız yüzünde yazma isteği yok.
- `Y5B_12`: tuval metinleri ve EN karşılığı.

**Mutasyonlar:** her biri `perl -0777 -pi` ile tek eşleşme → filtreli test → yedek geri → `cmp`.

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M5B-1 | `Kasalar.YildizArac`: KART → NAKIT | Y5B_07 | AYNI |
| M5B-2 | CSS `.yz-ozet--kasalar` 170px → 160px | Y5B_10 | AYNI |
| M5B-3 | `YildizOzet` b1 `disabled` koşulu silindi | Y5B_01 | AYNI |
| M5B-4 | `Evraklar.YildizSuz` sekme süzgeci → `true` | Y5B_09 | AYNI |
| M5B-5 | `Gider.YildizAyrintida` sekme koşulu silindi | Y5B_11 | AYNI |
| M5B-6 | `BuyukPara` eksi işareti silindi | Y5B_06 | AYNI |
| M5B-7 | `EvrakTakvimi.YildizTutar` yön ters | Y5B_09 | AYNI |
| M5B-8 | `PosTahsilatlar.YildizBekliyor` durum koşulu silindi | Y5B_09 | AYNI |

**Derleme ve süit:**
- Son mutasyondan sonra `dotnet build Kuyumcu.sln`: **0 Hata · 0 Uyarı**.
- Tam süit öncesi son derleme: 0 Hata · 6 Uyarı. Uyarıların hepsi başka şeridin test dosyalarında (xUnit analizörü, `Dogrulayici/SUBEKAPSAMASinirTestleri.cs` CS8601).
- **Tam süit** (bir kez, son kod; trx `/private/tmp/kuyumcu-serit/trx-pYILDIZ-5B`): **12397 test · 12388 geçti · 6 kırmızı · 3 atlandı.**
- 6 kırmızının hiçbiri bu paketin dosyasına dokunmuyor → **yeni kırmızı 0**. Sahipleri § 10'da.

## 8. Belirsiz eşleşme (orkestratör kurucuya gösterir)

| # | Tuval | Uygulama | Gerekçe |
|---|---|---|---|
| 1 | Y_Kasalar / Kasakuyrugu / Cekvesenet: b3 hapı iç içe `<a>` yüzünden satırın ALTINDA tam genişlik | hap satırın sağında, satır 85 px | Tuvaldeki kalıbın niyeti (YILDIZ-5A Emanet ile aynı karar). Kıyas düzeltilmiş tuval kopyasıyla yapıldı. |
| 2 | Kasalar tek kasa ("Ana Kasa · Türk lirası") | ilk aktif TL kasası; öteki kasalar "Tüm ayrıntılar"da | Tuvalde çok kasa hâli yok. |
| 3 | Kasalar "İşlem" sütunu, "SON KAPANIŞ dün yok" | işlem = kasanın tüm hareket sayısı (≤1000, aşarsa "1000+"); son kapanış "bugün/dün/8 Eki/yok" | "dün yok" kalıbı veri değil, cümle. |
| 4 | Kur cümlesi "…ekranda 'eski kur' yazar" | "…son kur kalır ve saati burada yazar." | Uygulamada "eski kur" yazısı yok (Anayasa md.1). |
| 5 | Kur 22 ayar/USD/EUR alış | "—" | Sunucu bu birimler için tek fiyat veriyor; alış uydurulmadı. |
| 6 | Hurda cümlesi "…ramat dönünce eklenir"; satırda Ayar/Gram | cümle ilk iki parça; Ayar/Gram "—" | Ramat dönüşü kaydı ve defter satırında ayar/gram yok. |
| 7 | Kasa kuyruğu notu "…Tek kişilik dükkânda bu ekran kapalı kalır." | "…görev ayrımı kapalı kalır." | Ekran kapanmıyor; ayar görev ayrımıdır. |
| 8 | POS alacakları "Fiş · Müşteri" sütunları | "POS · Banka" | POS alacağı fişe/müşteriye bağlı dönmüyor (sunucu yanıtında alan yok). |
| 9 | POS cihazları / Banka: yalnız boş hâl çizili | dolu hâl liste kalıbında (Cihaz · Banka · Bekleyen · Komisyon · Düzenle; Hesap · IBAN · Bakiye · Hareketler) | En yakın tuval kalıbı. |
| 10 | Gider: "Belge · isteğe bağlı / Fatura fotoğrafı" satırı ve notun 2. paragrafı | çizilmedi | Gider belgesi yükleme ürün davranışı değil; ölü alan konmadı. |
| 11 | Gider: Kategori satırı yok | Kategori seçici (birden çok kategori varken) | Sunucuda zorunlu alan; tek kategoride gizli (UX-S22). |
| 12 | Gider "Virman" seçimi tuvalde aynı formda | "Virman" özgün virman sekmesini açar | Virman kaynak/hedef, araç ve yetersiz bakiye kapıları özgün ekranda (davranış değişmesin). |
| 13 | Ekranlarda "+ Tüm ayrıntılar" yok | `.ekle` kalıbında var (özette eylem satırında, takvimde ızgara altında) | Özellik silinmez; tuval kalıbı. |
| 14 | Vade takvimi ay gezinmesi yok | yalnız içinde bulunulan ay; başka aralık "Tüm ayrıntılar"da | Tuval aynen. |

## 9. Süreç notları (Anayasa md.1)
- **Paylaşılan CSS'te düzenleme kaybı:** `tema-yildiz.css`'e yaptığım bir ekleme (≤480 rakam kuralı) kısa süre sonra dosyada yoktu. Aynı dosyaya eşzamanlı yazan başka şerit muhtemel.
  - Kural yeniden eklendi.
  - Son durumda bloğun bütün kuralları sayılarak doğrulandı.
  - M5B-2 mutasyonu da bu dosyaya ~1 dk yedekle yazdı. O pencerede başka şeridin yazısı olduysa geri yüklemeyle ezilmiş olabilir; ayrıştırılamadı.
- **Bayat CSS:** başsız Chrome profili eski CSS'i önbellekten sundu. `Network.clearBrowserCache` ile temizlendi ve yeniden ölçüldü.
- Davranış betiği Kur başarı bandını 4 sn sonra okudu; bant 3 sn'de kapanıyor. Ucun 200 yanıtı doğrudan çağrıyla ayrıca doğrulandı.

## 10. Denetim ve açık kalanlar
`denetim.sh` (son koşum) — iki ✗, ikisi de bu paketin değil:
- **P-30 DEMO_INSAN_SURESI:** kural dışı (kurucu kalemi).
- **GIRDI-2 TEST_GECERSIZ_FIKSTUR 1:** `Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187`, IBAN fikstürü. Bu paketin dosyası değil.

Ara koşumda paketin kendi ✗'i: **TOPLU-1 TARIH_TEK 2** (`EvrakTakvimi.Yildiz.cs`, `KasaRaporu.Yildiz.cs` ham `ToString("MMMM")`). `Bicim.GunBicim`'e taşındı → ✓.
B-3 ESIK_ISTEMCIDE (ara koşumda 5) Yasal-Kapı-3 dosyalarındaydı (`Islemler.YasalKapi3.cs`), son koşumda ✓.

**Tam süitteki 6 kırmızı** (hepsi başkasına ait):
- `RaporDortTestleri.RD_KH01`, `RD_KH02` — "Rapor dönemi en çok 400 gün olabilir" (sunucu rapor kapısı; rapor şeridi).
- `Yildiz4CTestleri.Y4C_44` (PRO) — Tezgâh 'islem-urun' seçeneği boş (Islemler/Tezgâh şeridi; YildizTezgah 22:34'te değişti).
- `DegisimEkranTestleri.DG_N_PRO_degisimde_biz_odersek_HAVALE_otomatik` — `#islem-odeme-otomatik` yok (fiş ödeme tablosu; YASAL-KAPI-3 / t9).
- `YasalKapi2NakitSiniriEkranTestleri.YK3_07` (PRO) — YASAL-KAPI-3 (t9).
- `S3dEkranTestleri.MUKELLEF_saticida_tedarikci_fatura_no_istenir_TCKN_istenmez` — tezgâh hurda satıcı formu (t5/t9).

**Yıldız koruma devri:** `yasal.css:109`'daki `line-height: 1.2109` Tezgâh tuval kıyasında "ödeme1 kasa" farkı üretiyor → YASAL-KAPI-3 sahibine.

## 11. Test kiracısı ve süreçler
- API (PID 93664) ve vekil yalnız kendi PID'leriyle durduruldu; 5164 ve 5165 portları boş. Kalan kendi Chrome'um (9615) kendi PID'iyle kapatıldı.
- `zsh kiraci-temizle.sh 01M4KH4A974G39XS7ADZEYHT8X --uygula` çalıştırıldı (75 tablo). Sonra SQL: bu kimlikle kiracı **0**.
- Kiracı sayısı: başlangıçta **23**, sonda **24**. Fark bu paketin değil; arada başka şeritler kiracı açtı (`PAKETYASAL-KAPI-3-TEST 48ad08`, `PAKETTZO-TEST …`, `PAKET-EKRAN-DENETIM …`, `PAKETSATINALMA-EKRAN-2-TEST t5`). `QA-TEST-*` fikstürlerine dokunulmadı.

## 12. DoD sözleşmesi
- Yeni varlık, uç ve migration yok.
- Ekranda doğan her düğme ya MEVCUT bir düzenleme/oluşturma yoluna gider ya da kilitli olup sebebini yazar:
  - Kasa günü kapalı → kilit + sebep.
  - Evrak geçişi yok → kilit + sebep.
  - Vezne fişi VEZNEDE değil → kilit + sebep.
  - Gider kilidi → başlık + alan altı mesaj.
- Finansal kayıtlar (kasa/banka hareketi, evrak, pusula) yalnız mevcut storno/iptal yollarıyla düzeltilir. Bu yollar "Tüm ayrıntılar"dadır (UX-S25).

## 13. Değişen dosyalar
Yeni:
- `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizOzet.razor`, `YildizOzetVerisi.cs`, `YildizBos.razor`, `YildizTakvim.razor`, `YildizForm.razor`
- `dotnet/Kuyumcu.Web/Pages/{Kasalar,Kur,Hurda,KasaRaporu,Vezne,PosTahsilatlar,Pos,Banka,Evraklar,EvrakTakvimi,Gider,GiderPusulalari}.Yildiz.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Yildiz5B.cs`
- `dotnet/Kuyumcu.Tests/Yildiz5BTestleri.cs`

Değişen (yalnız Yıldız dalı ya da blok):
- `dotnet/Kuyumcu.Web/Pages/{Kasalar,Kur,Hurda,KasaRaporu,Vezne,PosTahsilatlar,Pos,Banka,Evraklar,EvrakTakvimi,Gider,GiderPusulalari}.razor`
- `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css` (`/* YILDIZ-5B */` bloğu)

Dokunulan sayfalar (TEK DOKUNUŞ kaydı): yukarıdaki 12 razor.

## 14. Kurucu / mali müşavir
- Mevzuat hükmü yazılmadı. Pusula ekranındaki "(VUK md. 234)" tuval metnidir ve üründe zaten geçen atıfla aynıdır (`Sozlukler.ParaDemo3.cs` "gider pusulası için gerekli (VUK md.234)"). Yeni eşik ya da oran yok.
