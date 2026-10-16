# PAKET YILDIZ-4C-2 — uygulama raporu (10.10.2026)

Bu paket YILDIZ-4C'nin tamamlamasıdır. 4C'de Docker çöktüğü için yapılamayan işler bu pakette koşuldu: gerçek Chrome ölçümü, canlı satış SQL'i, Bugün'ün iki satırı, 8 şema farkı ve tam süit.

- 4C'deki kod baştan yazılmadı. Önce ölçüldü.
- Ölçümde **dört gerçek fark** çıktı. Dördü de düzeltildi ve yeniden ölçüldü (§ 1).
- Hasımsız paket: kanıt bu rapordaki ölçümlerdir.

Ortam:
- API 5160 (kendi PID'i), vekil 5161, CDP 9701–9706.
- Tek Chrome profili: `/private/tmp/kuyumcu-serit/pYILDIZ-4C-2/profil`.
- Kiracı `PAKETYILDIZ4C2-TEST Tunalılar Kuyumculuk` (01M4JJQKDCRASPEJF30KZDBRH2). `demo-tohumla.js --prova-kiraci --sema yildiz` ile kuruldu: 6 cari, 12 ürün, 9 satış.
- Kullanıcı adı "Hakan Tunalı" yapıldı (`PATCH /kullanicilar/ben` 200).
- Tüm ham çıktılar `/private/tmp/kuyumcu-serit/pYILDIZ-4C-2/` altında. Tuval kıyas tabloları ve görüntüleri `ham/yildiz-4c/hal/c2-*` ve `ham/yildiz-4c/c2s-*` altında.

## 1. Ölçümde çıkan farklar ve düzeltmeleri

Hepsi yalnız Yıldız dalında yapıldı. Para, veri ve yasal davranışa dokunulmadı.

### 1.1 Karma satırında kasa eki kesiliyordu
- **Ne görüldü:** "Kart · An…" yazıyordu (1440, BASİT koyu, Read ile görüldü). Tuvalde "Nakit · Ana Kasa".
- **Neden:** Araç seçicisinin tetiği `width: 100%` ile 100 px yer kaplıyordu. Metin ise 43 px'ti.
- **Düzeltme:** Tetik `width: max-content` yapıldı. Kasa eki artık tam (75 px, x=1045).

### 1.2 Karmada ölçer bulguları (sikisma K3 2/0, K6 2/2)
- **Ne görüldü:** Gizli "Tutar" etiketinin iç metni 1 px kutudan taşıyordu (K3). Açılan araç listesi 100 px genişlikteydi (K6).
- **Düzeltme:** Gizli etiketin iç metni kırpıldı. Liste en az 320 px yapıldı.
- **Sonuç:** K3 0/0, K6 0/2.

### 1.3 Karma tutarı tuvalde satırın sağ ucunda, uygulamada değildi (tuval-kiyas karma FARK 2)
- **Ne görüldü:** Tutarın sağ kenarı tuvalde 1376 px, uygulamada 1316 px. Sebep: tur 3'te "Çıkar" satırın en sağına konmuştu. Satırlar arası aralık da tuvalde 74 px, uygulamada 89 px'ti.
- **Düzeltme:** Satır tuvalin `.sat` ölçüsüne getirildi:
  - `padding 22px 0`, `gap 24px`, ortalı hizalama.
  - Tutar 3. sütunda, satırın sağ ucunda.
  - "Çıkar" (.b2) ad ile tutarın arasında.
  - Satır yüksekliği 29 px, satırlar arası boşluk 0.
- **Sonuç:** karma 11/11 AYNI (BASİT + PRO × koyu + açık).

### 1.4 375 px'te ₺ işareti tutardan ayrılıyordu
- **Ne görüldü:** 375 px karmada tutar ve ₺ ayrı satırlara düşüyordu (Read ile görüldü). Sebep: girdinin 8rem tabanı 130 px sütuna sığmıyordu.
- **Düzeltme:** `flex-wrap: nowrap` ve girdi `flex: 1 1 0`.
- **Sonuç:** tutar (x=173, w=114) ve ₺ (x=292) aynı satırda.

### 1.5 Peşin art arda sorusunun düğmeleri eski `.btn` idi
- **Ne görüldü:** Son hal koşumunda 10-basari hâlinde ESKI=2 çıktı: `#pesin-soru-evet` ve `#pesin-soru-hayir` `.btn.btn-ikincil` taşıyordu. Görünüşleri tuvale uyuyordu (pesin-soru 4/4 AYNI), ama sınıfları eskiydi.
- **Düzeltme:**
  - `PesinArtArdaSoru.razor`'a `[Parameter] bool Yildiz` eklendi. Yıldız'da sınıf `yz-b2` olur. Id, geri çağrı ve kural aynı kalır.
  - `Islemler.Yildiz.cs` bu parametreyi `true` geçer.
  - CSS'te bu düğmeler `.yz-duzen :is(.btn,.dugme)` küçük .b2 grubuna eklendi.
  - Öteki şemalar özgün `.btn` ile çizilir.
- **Sonuç:** son koşumda 128 hâlin hepsinde ESKI=0. pesin-soru 4/4 AYNI (4 kombinasyon).

## 2. Gerçek (başsız) Chrome ölçümü

### 2.1 Tezgâh etkileşim hâlleri — `hal.mjs`
Ölçülen hâller (16): boş, 1 kalem, 2 kalem, kalem düzenleme, indirim, para üstü, karma, karma-2, müşteri açık, müşteri ara, müşteri yok, eşik/MASAK, eşikte müşteri açık, hata (kilit), başarı, sunucu hatası.

Kombinasyonlar: BASİT/PRO × koyu/açık × 1440/375 = 8 × 16 = **128 ölçüm** (son kod; son koşum `son3.log`).

| Sayaç | 1440 (4 kombinasyon × 16) | 375 (4 kombinasyon × 16) |
|---|---|---|
| ESKI (görünür eski seçici: table/.kart/.btn/select/… + "Sütunlar", "(F2)", "Başka ödeme ekle"…) | **0** | **0** |
| Kaydet ekranda | 64/64 | 64/64 |
| Yatay kaydırma (sw > genişlik) | 0 | 0 |
| K1 [hidden] görünen / K3 taşan / K5 TRY | 0 / 0 / 0 | 0 / 0 / 0 |
| Satan satırı (BASİT) | 0 | 0 |
| Müşteri katmanı kendiliğinden açılır (08-esik) | hayır (`musteriAcik=false`) | hayır |

Gerekçeli kalan sayaçlar:
- **K7 (karma 1440, 1 adet):** iki sütunlu düzende sol barkod alanı ile sağdaki ödeme satırı aynı "görsel satır" sayılıyor. Tuvalde de düzen aynı.
- **K9 müşteri katmanı (1440, 6 adet):** katman `overflow: auto` ile kendi içinde kayıyor. `elementFromPoint` ile doğrulandı: katman dışındaki satırlar görünmüyor (`gorunur:false`). Ölçer kırpmayı bilmiyor; yanlış pozitif.
- **K9 PRO kalem düzenleme (1 adet):** aynı kırpma durumu. Görüntüde kalemler kendi içinde kayıyor.
- **375 "TOPLAM-ORTULU" ve K9:** 375'te Fark + Kaydet çubuğu sabit; kabul "Kaydet ekranda" bunu gerektiriyor. Toplam kaydırma 0'dayken çubuğun altında kalıyor. Kaydırınca görünüyor: `q7` probu, scrollY 433, toplam rakamının üstündeki öğe = TOPLAM.
- **K4:** boy/bölüm bilgisi, eşik yok.

Bantlar:
- **Başarı bandı:** 8 kombinasyonda görüldü. Örnek: "Son fiş: Satış · 13:02 · 11.101,36 ₺ Fiş yazdır" (`hal-*.out` 1. koşum ve `satis.out`). Son koşumda 10-basari Peşin art arda sorusu hâline denk geldi; o hâl de ölçüldü (ESKI 0).
- **Hata bandı:** "Satış: yetersiz satılabilir stok. Stok 3,000 adet … talep 4 adet." — 8/8, Kaydet ekranda.

### 2.2 Tuval kıyası — `betikler/tur/tuval-kiyas.mjs` (1440×900; BASİT + PRO × koyu + açık)

| Hâl | BASİT koyu / açık | PRO koyu / açık |
|---|---|---|
| kalem düzeltme (PRO) | — | 7/7 · 7/7 |
| karma (düzeltme sonrası) | 11/11 · 11/11 | 11/11 · 11/11 |
| indirim + para üstü | 7/7 · 7/7 | 7/7 · 7/7 |
| müşteri alanı | 4/4 · 4/4 | 4/4 · 4/4 |
| müşteri seçim satırları | 5/5 · 5/5 | 5/5 · 5/5 |
| **Bugün iki satır (canlı veri)** | **7/7 · 7/7** | **7/7 · 7/7** |
| hata | 1/1 · 1/1 | 1/1 · 1/1 |
| başarı | 4/4 · 4/4 | 4/4 · 4/4 |
| Peşin art arda sorusu (son kod) | 4/4 · 4/4 | 4/4 · 4/4 |
| kimlik (MASAK) | 5/5 · 5/5 | 5/5 · 5/5 |
| Müşteriler süzgeç | 2/2 · 2/2 | 2/2 · 2/2 |

Toplam taşma 0.

Eşleme dosyalarında yapılan uyarlamalar. Kopyalar `pYILDIZ-4C-2/esleme/` altında; özgün `ham/yildiz-4c/esleme/` dosyalarına dokunulmadı.
- **hata:** 104 barkodu yerine 111 kullanıldı. Ölçüm için 104'e stok girilmişti, yetersiz stok hatası o barkodla artık oluşmuyordu.
- **başarı:** 081 yerine 012 kullanıldı (081'in stoku bitti).
- **kimlik:** müşteri seçiminden önce "Değiştir" tıklaması eklendi. Tur 3 KRİTİK-1 düzeltmesinden sonra katman kendiliğinden açılmıyor.

Statik ekranlar (`c2s-*`):
- Tezgâh 35/36, Müşteriler 27/28, Tüm ekranlar 65/67.
- Tek FARK her ekranda "menü seçili" öğesinin x konumu: tuval 264,8 ↔ uygulama 388,8 (+124 px). Sebep: YILDIZ-AMBLEM başlığa "MİHENK" yazısını (110 px) ekledi. Kabuk bu paketin alanı değil; açık kalanlarda.
- Tüm ekranlar'da kare sayısı 41/39. Sebep: başka şerit /satin-alma ve /irsaliyeler karelerini ekledi (veri). Bu yüzden `main` boyu da farklı.

### 2.3 Sıkışma (`sikisma-olc.js`) — `/`, `/islemler`, `/cariler`, `/ekranlar` × 1440/1280/375 × koyu/açık × BASİT/PRO (48 ölçüm)
- **K1, K3, K5 (TRY), yatay kaydırma:** hepsi 0.
- **K7 /cariler ±14:** 4B'den beri bilinen durum; tuvalle aynı.
- **K8 "r-zarar":** ölçerin yanlış pozitifi. Tuvaldeki metin "Kâr-zarar"; desen "â"dan sonra bölüyor.
- **K9 /islemler 375:** sabit alt çubuk (§ 2.1).
- **K2/K4:** bilgi sayaçları.
- Ölçüm 1.5 düzeltmesinden önce yapıldı. 1.5 yalnız Peşin sorusu açıkken görünen düğmenin sınıfını değiştiriyor; o hâl § 2.1'de son kodla ölçüldü.

### 2.4 Read ile bakılan görüntüler (4)
1. `yan-1.png`: 1440 karma + müşteri katmanı (fark 1.1 bulundu).
2. `kirp-pk03.png`: PRO kalem düzenleme. K9'un kırpma olduğu doğrulandı.
3. `yan-375.png`: 375 kalem, karma, sunucu hatası (fark 1.4 bulundu).
4. `yan-son.png`: tuval | uygulama karma (koyu BASİT, son kod) + 375 karma. Tutar sağ uçta, "Nakit · Ana Kasa", "Çıkar" arada.

## 3. Canlı satış + SQL (BASİT koyu 1440 ve PRO açık 1440; gerçek Chrome ile ekrandan)

| Adım | Ekran sonucu | SQL (`hareketler` / `kasa_hareketleri`) |
|---|---|---|
| S1: 1 kalem nakit → Peşin sorusu "Hayır" | "Son fiş: Satış · 14:00 · 11.099,68 ₺" | SATIS 11099.68 NAKIT · kasa NAKIT 11099.68 |
| S2: karma (2 kalem; Nakit 5.000 + Kart) | "Son fiş: Satış · 14:00 · 22.199,36 ₺" | SATIS 22199.36 **KARMA** nakit_tutar 5000.00 · kasa NAKIT 5000.00 + KART 17199.36 |
| S3: BASİT indirim %5 | "… 10.544,70 ₺ · Uygulanan iskonto: 554,98 ₺" | tl_tutari 10544.70 · indirim_tutari 554.98 · liste_fiyati 11099.68 |
| P1: PRO indirim %5 | "… 21.089,40 ₺ · Uygulanan iskonto: 1.109,97 ₺" | tl_tutari 21089.40 · indirim_tutari 1109.97 · liste_fiyati 22199.37 |

Ham çıktılar `satis.out`, `satis-pro.out`, `satis-sql.txt`.

POST gövdesinin platinle birebir aynı olduğunu `Y4C_23` (BASİT + PRO) sınıyor, yeşil. Bu paket satış yoluna dokunmadı. Peşin sorusu için yalnız düğme sınıfı değişti; `Y4C_26` "Hayır" cevabının gövdesinde `ayniKisiBeyani:false` olduğunu doğruluyor.

Ölçüm stoğu: tohumdaki 5 ürüne test kiracısında `POST /islemler/alis` ile 20'şer adet girildi (200). Ürünün kendi ucu kullanıldı; bu veri kiracıyla birlikte silindi.

## 4. Bugün iki satır — canlı veri
- **Senaryo SQL'i (yalnız kendi test kiracımda):**
  - `UPDATE hareketler SET tarih = tarih - INTERVAL 31 DAY` — tohumun EMANET_GIRIS satırı (01M4JJR77PE7…), row_count 1.
  - `UPDATE kasa_hareketleri SET tarih = tarih - INTERVAL 1 DAY ORDER BY tarih LIMIT 1` — row_count 1. Satır 2026-10-09'a geçti; gün kapanışı yok.
- **Sonuç:** tuval-kiyas Bugün 7/7 AYNI (BASİT + PRO × koyu + açık). "Dünkü gün sonu kapatılmadı · Kasa · Kapat" ve "Derya Yıldızlı'nın emaneti · 9,16 g has · 30 günü aştı" satırları canlı veriden geliyor.
- Satırlar salt okunur: Kapat → /kasalar, Aç → /emanet. Eşik `YildizBugunVerisi.EmanetEsikGun = 30`.

## 5. 8 eski şema piksel farkı — `betikler/tur/sema-fark.mjs` (son derleme)
- Kiracı ve kullanıcı şeması `platin`e alındı (`ayar.mjs`, tek giriş). Ardından ikisi de `yildiz`e geri alındı.
- **BASİT:** 64 karşılaştırma · **FARK 0**.
- **PRO:** 64 karşılaştırma · **FARK 0**.
- Not: ilk deneme "Yıldız kabuğu hâlâ çiziliyor" diyerek durdu, çünkü yalnız kiracı şeması değişmişti. Kullanıcı düzeyi şema da değiştirildi ve yeniden koşuldu.

## 6. Testler
- **Yeni:** `Kuyumcu.Tests/Yildiz4C2Testleri.cs`
  - CSS: Y4C2_01–06.
  - bUnit: `Yildiz4C2SoruTestleri.Y4C2_07` (Yıldız ve platin teorisi; iki düğme de geri çağrıyı tetikler).
- **Uyarlanan (4C):**
  - `Y4C_41` ve `Y4C_46`: "Çıkar" `grid-column: 2`.
  - `Y4C_26`: Yıldız sayfasında soru düğmeleri `.btn` değil `yz-b2` (2 adet).

| Koşum | Toplam | Geçti | Kaldı |
|---|---|---|---|
| `Yildiz4C\|Yildiz4B\|Yildiz3\|Yildiz2T\|YildizAmblem\|Yasal3Ekran` | 140 | 140 | 0 |
| `Yildiz\|Yasal3Ekran\|RaporDort` (son derleme, t1) | 230 | 228 | 2 (RaporDort KH01/KH02, başka şerit) |
| Tam süit 1 (`trx-pYILDIZ-4C-2/*.trx`) | 12105 (12102 koşuldu) | 12099 | 3 |
| Tam süit 2 (son derleme) | 12105 (12102 koşuldu) | 12100 | 2 (RaporDort, başka şerit) |

Tam süit 1'deki 3 kırmızı:
- **`Y4C_26` (bayat ikili).** Mutasyon betiği kaynağı yedekten geri koydu (`cmp` AYNI). Ama `copy2` eski değişiklik zamanını da geri getirdi. MSBuild artımlı derlemede dosyayı güncel sandı; ikili M32 mutasyonuyla (Yildiz=false) kaldı.
  - Düzeltme: kaynak dosyalara `touch` → yeniden derleme. `Y4C_26` yeşil (t7.log).
  - Tam süit bu yüzden son derlemeyle bir kez daha koşuldu (§ 6.1).
- **`RaporDortTestleri.RD_KH01` ve `RD_KH02`:** "Rapor dönemi en çok 400 gün olabilir". Başka şerit (RAPOR); bu paketin dokunmadığı sunucu kuralı.

### 6.1 Tam süit 2 (son derleme, KUYUMCU_TEST_DB=kuyumcu_test_t1)
- **Toplam 12105:** 12102 koşuldu · **12100 geçti** · 2 kaldı (`trx-pYILDIZ-4C-2/tam2.trx`).
- Kırmızı 2: `RaporDortTestleri.RD_KH01` ve `RD_KH02`. İkisi de "Rapor dönemi en çok 400 gün olabilir" (RAPOR şeridi; bu paketin dokunmadığı sunucu kuralı).
- `Y4C_26` dahil Yıldız testlerinin hepsi yeşil.
- **Bu paketin ürettiği yeni kırmızı 0.**

## 7. Mutasyon (`mut.py`; yedek `/private/tmp/kuyumcu-serit/mut-yedek/pYILDIZ-4C-2/`; tek eşleşme doğrulanır; geri al + `filecmp`)

| Kod | Mutasyon | Kırmızı test(ler) | Geri alındı |
|---|---|---|---|
| M25-01 | Araç seçicisi `width: max-content` kaldırıldı | Y4C2_01 (+ Y4C_44 PRO*) | cmp AYNI |
| M26-01 | Açılan liste `min-width: 320px` kaldırıldı | Y4C2_02 | cmp AYNI |
| M27-01 | Gizli etiket iç metni kırpılmaz | Y4C2_03 | cmp AYNI |
| M28-01 | Tutar `nowrap` kaldırıldı | Y4C2_05 | cmp AYNI |
| M29-01 | Eski `150px auto` düzeni (tutar sağ uçta değil) | Y4C2_06 | cmp AYNI |
| M30-01 | Satırlar arası boşluk yine 12 px | Y4C2_06 | cmp AYNI |
| M31-01 | Peşin sorusu Yıldız'da yine `.btn` | Y4C2_07 (yildiz: True), Y4C_26 | cmp AYNI |
| M32-01 | Islemler.Yildiz soruya `Yildiz=false` geçer | Y4C_26 | cmp AYNI |

\* M25 koşumunda Y4C_44 PRO da kırmızı. CSS'e bağlı bir test değil; o koşumda başka şeridin aynı anda derleme yapmasından doğan kararsızlık olabilir. Doğrulanmadı, bilgi olarak yazıldı.

Son mutasyondan sonra:
- `touch` + temiz derleme `dotnet build Kuyumcu.sln` → **0 Uyarı, 0 Hata** (`build-son3.log`).
- Daha önceki tam (artımsız) derlemede 7 uyarı görüldü: SonKalanlar, YasalKapi2, Yildiz2Testleri vb. Hiçbiri bu paketin dosyası değil.

## 8. Denetim (`bash dotnet/denetim.sh`)
- Ara koşum: 314 ✓. ✗ satırları:
  - **P-11 DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48 > 47:** bu paket razor'da `catch` yazmadı. Aday: `Pages/SatinAlma.razor` (izlenmeyen yeni dosya, SATINALMA şeridi) — 1 eşleşme. `Cariler.razor` 6 eşleşme; bu paket dokunmadı.
  - **P-30:** bilinen, kapsam dışı.
  - **GIRDI-2:** `MASAKESZAMANSinirTestleri.cs:187` (t5).
- Bu paketin sayaçları ✓.
- Son koşum (tam süit 2'den sonra): 315 ✓. ✗ satırları yalnız P-30 ve GIRDI-2 (t5).
  - P-11 bu koşumda ✓; başka şerit düzeltmiş.
  - Bu paketin sayaçları ✓.

## 9. Temizlik
- API (dotnet run 32990 ve alt süreci 33008) ve vekil kendi PID'leriyle durduruldu. 5160/5161 dinleyen 0.
- Kendi Chrome'um (9705, eski fark koşumundan kalmıştı) kendi PID'iyle kapatıldı. Profilim için çalışan süreç 0.
- `zsh kiraci-temizle.sh 01M4JJQKDCRASPEJF30KZDBRH2 --uygula` rc 0. 75 tenant_id tablosunda bu kiracıya ait satır **0**.
- **tenants sayısı başlangıçta 22, sonda 23.** Benim kiracım silindi (0). Artış başka şeritlerden: listede yeni `PAKETYASAL-KAPI-2-TEST`, `PAKETEKSIK-B1-SATINALMA-TEST` var. Onlara dokunmadım.
- QA-TEST, 5199, 5200 ve 5241'e dokunulmadı.

## 10. Olaylar (Anayasa md.1)
1. **Test çakışması.** Bir filtreli test koşumunu (t6) `KUYUMCU_TEST_DB` vermeden başlattım; varsayılan `kuyumcu_test` veritabanına bağlandı. Aynı anda başka bir şeridin derlemesi de vardı: apphost imzalama yarışı ve "__EFMigrationsHistory already exists" hatası görüldü. Koşum t1 ile tekrarlandı. Varsayılan test veritabanını kullanan başka bir şerit varsa o koşumu etkilemiş olabilir.
2. **Başka şeridin süitine denk gelen derleme.** İlk filtreli koşumumda test projesini derledim. O sırada başka bir şerit `--no-build` tam süit koşuyordu (PID 99233, tam4.trx). İkilisi o sırada yenilenmiş olabilir.
3. **Disk.** Boş alan çalışma sırasında ~20 GB'tan 3,5 GB'a indi; sonra 6 GB'a çıktı. Benim dizinim 270 MB. Görev sınırı yüzünden disk incelemesi yapılmadı. Orkestratöre not.

## 11. DoD sözleşmesi
Yeni varlık ya da uç yok; yalnız görünüm.
- **Ödeme satırı:** araç seçicisi, tutar, "Çıkar" (aynı `BacakSil`).
- **Peşin sorusu:** "Evet" ve "Hayır" aynı geri çağrılar. "Hayır" aynı fişi `ayniKisiBeyani=false` ile gönderir.
- **Bugün satırları:** salt okunur.

Finansal varlıklarda silme yok.

## 12. Belirsiz eşleşme (orkestratör kurucuya gösterir)

| Öğe | Uygulamada | Neden |
|---|---|---|
| Karmada satırı çıkarma | "Çıkar" (.b2, 14 px) ad ile tutar arasında | Tuvalde karmadan satır çıkarma çizilmemiş. Tutar tuvaldeki gibi sağ uçta kalsın diye ortaya alındı (tur 3'te sağ uçtaydı, kıyas FARK veriyordu) |
| Karma tutar biçimi | Girdi "5.000" ve "13.026,72" gösteriyor; tuval "15.000,00 ₺" | Girdi alanının ortak biçimleyicisi kuruş sıfırsa ",00" yazmıyor. Değiştirmek paylaşılan Fiş girdisinin davranışı demek; bu pakette yapılmadı (açık kalan 2) |
| Kalan nakit hakkı satırı (karma) | Tuvalin not satırı | Yasal bilgi; önceki turla aynı |
| Peşin sorusu düğmeleri | Küçük .b2 | Tuvalde soru hâli yok; Fark bloğunun b2 kalıbı |
| 375 toplam | Sabit çubuğun altında, kaydırınca görünür | Tuvalde 375 yok; "Kaydet ekranda" kabulü |

## 13. Dokunulan sayfalar
- `Pages/Islemler.Yildiz.cs`: 1 satır (`Yildiz` parametresi). Gerekçe: Chrome ESKI sayacı (§ 1.5). 4A/4B/4C'de de açılmıştı; ikinci dokunuş gerekçesi bu ölçüm.
- `Bilesenler/Fis/PesinArtArdaSoru.razor`: yalnız sınıf seçimi (Yıldız dalı).

## 14. Değişen dosyalar
- `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css` — 4C bloğu: karma satırı, araç seçicisi, gizli etiket, 375 tutar, Peşin sorusu b2.
- `dotnet/Kuyumcu.Web/Bilesenler/Fis/PesinArtArdaSoru.razor`.
- `dotnet/Kuyumcu.Web/Pages/Islemler.Yildiz.cs`.
- `dotnet/Kuyumcu.Tests/Yildiz4C2Testleri.cs` (yeni).
- `dotnet/Kuyumcu.Tests/Yildiz4CTestleri.cs` (Y4C_26, Y4C_41, Y4C_46).
- Migration: gerekmedi.

## 15. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Tezgâh her etkileşim hâli gerçek Chrome'da, ESKI = 0 | ✓ | 128/128 ESKI 0 (`son3.log`, `hal-*.out`) |
| tuval-kiyas tolerans içinde | ✓ | tüm hâller AYNI (§ 2.2) |
| Kaydet ve bantlar ekranda (1440 + 375) | ✓ | 128/128 Kaydet ekranda; hata/başarı bandı 8/8 |
| Satış davranışı aynı + canlı satış/karma/indirim SQL | ✓ | Y4C_23; § 3 tablosu |
| Müşteriler / Tüm ekranlar kalan bulgu | Kısmi | Tek FARK menü x +124 (AMBLEM kabuğu, alan dışı); 41/39 kare (veri) |
| Bugün iki satır canlı | ✓ | 7/7 × 4 (SQL senaryosu § 4) |
| 8 eski şema FARK 0 | ✓ | 64 + 64, FARK 0 (son derleme) |
| Mutasyon ≥ 4, kırmızı, cmp AYNI | ✓ | 8/8 |
| Build 0/0 | ✓ | build-son3.log |
| Denetim (P-30 hariç) bu paket için temiz | ✓ | son koşumda yalnız P-30 + GIRDI-2 (t5) |
| Tam süit yeni kırmızı yok | ✓ | 12100/12102; 2 kırmızı RaporDort (başka şerit) |
| Test kiracısı silindi | ✓ | 75 tablo 0 satır |

## 16. Açık kalanlar
1. **(Kabuk, YILDIZ-AMBLEM)** "MİHENK" yazısı menüyü tuvale göre 124 px sağa itiyor. Statik ekranlarda tek FARK bu. Kurucu amblemi onayladıysa tuval kıyas eşlemesi güncellenmeli; onaylamadıysa kabuk düzeltilmeli.
2. **(Paylaşılan Fiş girdisi)** Karma tutar girdisi kuruş sıfırsa ",00" göstermiyor ("5.000"). Tuval "15.000,00 ₺" istiyor. Girdi biçimleyicisi ortak bileşen; hasımlı pakete devir önerilir.
3. **(375 kalem satırı, 4A)** Ad sütunu dar: "22 Ayar / Çeyrek / Altın" üç satıra kırılıyor (K3 kırık 0, ama sıkışık). Tuvalde 375 yok.
4. **(Başka şerit)** RaporDort `RD_KH01` ve `RD_KH02` kırmızı ("Rapor dönemi en çok 400 gün"). P-11 48/47 (aday `SatinAlma.razor`). GIRDI-2 (t5). P-30.
5. **(Ölçer)** sikisma K9, `overflow: auto` kırpmasını bilmiyor. K8 "Kâr-zarar" yanlış pozitif.
6. **(4C'den)** Eski `pYILDIZ-4C/anahtar` dosyasının silinmesi kurucu/orkestratöre bırakılmıştı; hâlâ duruyor.

## 17. Kurucu / mali müşavir
Mevzuat hükmü yazılmadı. Eşik sayısı istemciye eklenmedi. Emanet 30 gün eşiği 4C'deki ayar sabiti.

---

## TUR 2 (10.10, ~15:30–16:10) — tasarım hakemi KRİTİK: karma kaydı sonrası eski "Ödeme şekli / Kasa" formu açık kalıyordu

**Durum: YARIM** (düzeltme + test + mutasyon + gerçek Chrome karma ölçümü TAMAM; 128 hâllik yeniden ölçüm, 8 şema farkı ve tam süit bu turda BİTİRİLEMEDİ — ayrıntı aşağıda).

### Düzeltme
- `Bilesenler/Yildiz/YildizTezgah.razor`: `OnParametersSet` ile `B.Karma` true → false geçişinde `odemeDuzen = false`. "+ Başka ödeme"nin açtığı panel, karma kaydedilip fiş sıfırlanınca (ya da bacaklar teke inince) açık kalmaz. Yalnız görünüm durumu; satış/ödeme verisi değişmedi.
- Karma olmadan kullanıcının satıra dokunup açtığı panel yeniden çizimde açık kalır (aşırı geniş değil).

### Test
- Yeni bUnit: `Yildiz4CTestleri.Y4C2_08_karma_kaydi_sonrasi_odeme_duzen_paneli_kapanir` (karma → kayıt sonrası panel yok, `#islem-aldim-odeme`/`Tamam` yok, tek "Nakit · Ana Kasa" satırı var; denetim yarısı: karmasız açılan panel korunur).
- `FullyQualifiedName~Yildiz` (t1): **194/194 yeşil**. Build `Kuyumcu.sln`: **0 Uyarı 0 Hata** (`r2/build2.log`).

### Mutasyon (`r2/mut2.py`; yedek `mut-yedek/pYILDIZ-4C-2-r2/`; geri yüklemede `copyfile` + `utime` — tur 1'deki bayat ikili tuzağına karşı)
| Kod | Mutasyon | Kırmızı test | Geri alındı |
|---|---|---|---|
| M33-01 | sıfırlama satırı silindi | Y4C2_08 (ikinci koşum; ilk koşumda CoreCompile atlandı → bayat ikili, yeşil kaldı, tekrarlandı) | cmp AYNI |
| M33-02 | `oncekiKarma` izlenmez | Y4C2_08 | cmp AYNI |
| M33-03 | aşırı geniş: karma değilse her çizimde kapat | Y4C2_08 | cmp AYNI |
Son mutasyondan sonra `touch` + temiz derleme 0/0, Yıldız 194/194.

### Gerçek başsız Chrome — hakemin `basari.mjs` senaryosu (çeyrek 11.099,68 = Nakit 5.000 + Kart 6.099,68), kayıt SONRASI
| Mod | Tema | Genişlik | düzen paneli | "Ödeme şekli" metni | `#islem-aldim-odeme` | ESKI | Kaydet ekranda | Bant |
|---|---|---|---|---|---|---|---|---|
| BASİT | koyu/açık | 1440 | yok | yok | yok | 0 | evet | Son fiş: Satış · 11.099,68 ₺ |
| PRO | koyu/açık | 1440 | yok | yok | yok | 0 | evet | aynı (açıkta önce Peşin sorusu → Hayır) |
| BASİT | koyu/açık | 375 | yok | yok | yok | 0 | evet | aynı |
| PRO | koyu/açık | 375 | yok | yok | yok | 0 | evet | aynı |
| BASİT tek ödeme | açık | 1440 | yok | yok | yok | 0 | evet | aynı |
Görüntüler (Read ile bakıldı): `r2/g/basari-BASIT-aydinlik-1440-karma.png` (temiz: "Nakit · Ana Kasa 0,00 ₺" altında hap satırı, eski form yok), `r2/g/basari-PROFESYONEL-karanlik-375-karma.png`.
Not: prob çıktısındaki "Ya al … ınırının" kesikliği ölçüm betiğimin hatasıdır (şablon dizgisinde `\s` → `s`); ekranda metin "Yasal kurallar uygun · nakit 30.000 ₺ sınırının altında" (görüntüde doğrulandı).

### Canlı SQL (kendi kiracım 01M4JXAP3HTD03M4NQB1Y5T397, `r2/satis-sql.txt`)
9 KARMA satış: her biri `hareketler` SATIS KARMA tl_tutari 11099.68 nakit_tutar 5000.00 · `kasa_hareketleri` KART 6099.68 + NAKIT 5000.00. Tek ödeme: NAKIT 11099.68. Satış davranışı değişmedi.

### Denetim (`r2/denetim.log`)
315 ✓; ✗ yalnız P-30 (bilinen) ve GIRDI-2 (`MASAKESZAMANSinirTestleri.cs:187`, t5). Bu paketin sayaçları ✓.

### BİTİRİLEMEYENLER (Anayasa md.1)
1. 128 hâllik `hal.mjs` yeniden koşumu: yalnız B-k-1440 (16 hâl, ESKI toplam 0) ve B-a-1440'ın 8 hâli (ESKI 0) tamamlandı; kalan 6 kombinasyon, görev süresi sonu nedeniyle durduruldu.
2. 8 şema piksel farkı (`r2/fark.sh` hazır) bu turda koşulmadı. Değişiklik yalnız `YildizTezgah` (Yıldız dışı şemada çizilmeyen bileşen) içinde; tur 1 sonucu 64+64 FARK 0 — yine de bu turda ölçülmedi.
3. Tam süit (`trx-pYILDIZ-4C-2/tam-r2.trx`) arka planda başlatıldı, sonuç alınamadan görev bitti.

### Temizlik
API 5160 + vekil 5161 + Chrome 9701 kendi PID'leriyle durduruldu; `kiraci-temizle.sh 01M4JXAP3HTD03M4NQB1Y5T397 --uygula` rc 0; kiracı satırı 0. tenants başta 22, sonda 23 (artış başka şeritlerin kiracıları: PAKETYILDIZTIPO, PAKETISLEM-BANT-2, PAKETYASAL-KAPI-2 — dokunulmadı). QA-TEST, 5199/5200/5241 dokunulmadı.

### Değişen dosyalar (tur 2)
- `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizTezgah.razor`
- `dotnet/Kuyumcu.Tests/Yildiz4CTestleri.cs` (Y4C2_08)
