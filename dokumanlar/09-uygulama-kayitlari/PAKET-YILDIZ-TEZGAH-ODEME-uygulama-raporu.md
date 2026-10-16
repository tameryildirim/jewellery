# PAKET YILDIZ-TEZGAH-ODEME — uygulama raporu (tur 2)

Dalga YILDIZ-TEZGAH-ODEME · şerit t7 (API 5176 · vekil 5177 · CDP 9851 · KUYUMCU_TEST_DB=kuyumcu_test_t7) · 10–11.10.2026
Rol: Taşkın (backend, dokunulmadı) + Ertuğ (frontend). Hasımsız paket: kanıt bu rapordur.

## 0. Özet

Kurucunun 10.10 bulgusu: sağdaki ödeme sütununda neyin ne olduğu anlaşılmıyordu. Tutar sağdan kesik çıkıyordu ("0 ₺"). Toplam ve Kaydet ekranın altında kalıyordu. Aşağıda içerik olduğu fark edilmiyordu.

- **Tur 1 (önceki koşum):** ödeme satırları, haplar ve açılan alanlar tek kaydırma kabına alındı. Fark ve Kaydet sütunun dibine sabitlendi. Karma satırı `[araç · kasa] [çizgili tutar alanı] [Çıkar]` düzenine geçti. "Çıkar" ile tek satır kalınca tek ödemeye inilir (FARK 0). "Açık hesap olarak kaydet" .b2 oldu ve yalnız fark > 0 iken, gerçek müşteri seçiliyse görünür.
- **Tur 2 (bu koşum):** tasarım hakeminin üç KRİTİK bulgusu kapatıldı (§ 1). Üç ek kusur da ölçümde bulunup giderildi: 375 px'te açılan alan Kaydet şeridinin arkasında kalıyordu; 1010×640'ta tek ödeme satırı kırpılıyordu; 375 px'te uzun müşteri satırında "Değiştir" taşıyordu. Bir de Y1_10 (sabit renk) ihlali vardı, o da giderildi.

## 1. Önceki doğrulamanın bulguları → kapanış

| # | Bulgu (hakem) | Yapılan | Kanıt |
|---|---|---|---|
| K1 | "+ İndirim"in açtığı alan 1024×768 / 1280×800 / 1366×768'de kabın altında kalıyor; işaret yalnız 1 px çizgi | (a) Bir hap yeni alan açınca o öğe kabın görünür bölgesine kaydırılır: `YildizTezgah.AcilaniGoster` → `document.querySelector(hedef).scrollIntoView({block:'nearest'})`. Hedefler: indirim alanı, para üstü alanı, yeni ödeme satırının tutar alanı. İlk çizimde kaydırma yok. (b) Kap kayıyorsa altta ve üstte belirteç renkli gölge (`--yz-kaydir-golge`) ve 1 px çizgi görünür. Kap sona gelince örtü kalkar. | Matris § 3: S2c (karma iki satır + İndirim) **40/40 kombinasyonda TAM**. 1280×800 koyu ve 1024×768 açık görüntü: `ham/yildiz-tezgah-odeme/tur2-pano-2.png` |
| K2 | 1024×768'de ikinci sıra haplar kabın alt kenarında yarıdan kesik (9 px) | Hap sırası (`#yildiz-tezgah-ekler`) kaydırma kabının **dışına**, kabın hemen ardına alındı. Tuval sırası ÖDEME → satırlar → haplar → FARK korunur. Açılan alanlar kabın içinde, hapların üstünde açılır. | "haplar kırpık" sütunu: masaüstü 36/36 kombinasyonda **0** (375'te sayfa kaydığı için viewport dışı sayılır, kap kırpması değil) |
| K3 | Hakem yeterli görüntüye bakmadı (3 < 4) | 4 yan yana pano Read ile görüldü (§ 4) | `tur2-pano-1..4.png` |

## 2. Yapılanlar (tur 2) ve sapmalar

1. **`Bilesenler/Yildiz/YildizTezgah.razor`**
   - `#yildiz-tezgah-ekler` kabın dışına alındı. Para üstü ve indirim alanları kabın içinde kaldı. Mevcut id'lerin hiçbiri değişmedi.
   - `OnAfterRenderAsync` → `AcilaniGoster(ilk)`: indirim, para üstü veya karma satır sayısı arttığında hedef bir kez kaydırılır. Yeni JS dosyası yoktur, IJSObjectReference ile yerleşik `scrollIntoView` çağrılır. Veri ve istek yoktur.
2. **`wwwroot/css/tema-yildiz.css` `/* TEZGAH-ODEME */` bloğu**
   - Kap arka planı: üst ve alt örtü (`local`), gölge (`scroll`) ve 1 px çizgi. Renk `--yz-kaydir-golge` özel özelliğinden gelir; kural gövdesinde color-mix yoktur (Y1_10).
   - `@media (max-width:1000px)`: açılan alan ve bacak girdisi için `scroll-margin-top:96px; scroll-margin-bottom:40vh`. Böylece alan alttaki yapışık Fark/Kaydet şeridinin arkasına kaydırılmaz.
   - `@media (min-width:1001px) and (max-height:767px)`: sağ sütun, alt blok ve kap aralıkları 28 → 16 px. Kurucunun ekranı kısa (~1000×640); bu yükseklikte tek ödeme satırı kaptan kırpılıyordu.
   - `.yz-tezgah-ust-satir > .yz-tezgah-degistir { flex-shrink:0 }`: 375 px'te "SATIŞ · DERYA YILDIZLI · BORCU 4.734,30 ₺" satırında "Değiştir" daralıp taşıyordu (ölçer K3=1). Artık satır yazısı sarar.
3. **`Kuyumcu.Tests/YildizTezgahOdemeTestleri.cs`**
   - YTO_01 güncellendi: haplar kabın dışında.
   - YTO_07 genişletildi: gölge, scroll-margin ve kısa ekran aralığı.
   - YTO_08 genişletildi: "Değiştir" daralmaz.
   - Yeni testler YTO_10 (indirim açılınca kaydırma; ilk çizimde ve yeniden çizimde kaydırma yok) ve YTO_11 (para üstü, + Başka ödeme, 3. satır; satır çıkarılınca kaydırma yok).

Sapmalar ve belirsiz eşleşmeler (tuvalde karşılığı olmayan hâller; orkestratör kurucuya gösterir):

| Öğe | Tuvalde | Uygulamada | Gerekçe |
|---|---|---|---|
| Açılan alan konumu | yok (tuval yalnız tek ödeme) | alan, kabın içinde hapların **üstünde** açılır ve görünür bölgeye kaydırılır | Haplar kaptan kırpılmasın (K2). Alan hapların altında açılsaydı kap dışına taşar ya da Fark'ı iterdi. |
| Kaydırma işareti | yok | belirteç renkli 14 px gölge + 1 px çizgi (üst ve alt) | "aşağıda daha var" (kurucu 10.10). Tuval kalıbında bir karşılığı yok; en yakın kalıp `--kenarlik` çizgisi korundu. |
| ≤767 px yükseklik | yok (tuval 1440×900) | aralık 16 px | Kabul aralığı 768–900. 640'ta tek ödeme satırı kırpılmasın diye eklendi. 768 ve üstü tuvalin 28 px'i. |
| Karma satırı tutar sağ kenarı | tek ödeme satırında tutar satır sonunda | `[tutar][Çıkar]`; tutarın ₺ sağ kenarı 1316 (tuval 1376) | Kurucu: "Çıkar satırın sonunda tutarla karışmaz". `hal-karma-koyu` FARK 2 bu bilinçli düzenden gelir (tur 1). |
| "Değiştir" daralmaz | 1440'ta daralma yok | dar ekranda satır yazısı sarar | K3=0. 1440 tuval-kıyası değişmedi (§ 5). |

Para, veri ve yasal davranış değişmedi. POST gövdeleri aynı kaldı (§ 6).

## 3. Gerçek başsız Chrome ölçümü (tablo)

Araç: `/private/tmp/kuyumcu-serit/YILDIZ-TEZGAH-ODEME/odeme2.mjs`. İçinde `sikisma-olc.js` ve DURUM taraması vardır. DURUM taraması kırpma ve örtülme duyarlıdır: `elementFromPoint` ile yapışık şeridin örtmesini de yakalar.

Senaryolar:
- S1: 1 kalem, tek ödeme
- S1b: tek ödeme + Para üstü
- S2: + Başka ödeme
- S2b: 2. satıra 10.000
- S2c: karma + İndirim
- S2e: 3. satır
- S3: Çıkar ile tek satır (kurucu hâli)
- S4: İndirim %5
- S5: açık hesap
- S6: açık hesap sonucu

Matris (t2, 40 kombinasyon): {BASİT, PRO} × {koyu, açık} × {1024, 1280, 1366, 1440} × {768, 900} + 1280×800 + 375×812. Satır başına 11 ölçüm. **40/40 kombinasyon sorun 0.**

- Toplam, FARK ve Kaydet her senaryoda görünür alanda (EVET).
- Tutar kesilmesi 0. Görünür binme 0. Yatay kaydırma 0.
- K1, K3 ve K5 = 0.
- S1b, S2, S2c, S2e ve S4 alanları TAM.
- Açık hesap düğmesi .b2.
- Tablo: `ham/yildiz-tezgah-odeme/tur2-olcum-tablosu.md`. Senaryo dökümü: `tur2-olcum-matris.txt`.

Son kod ile yeniden ölçüm (t4, scroll-margin, kısa ekran aralığı ve Değiştir düzeltmelerinden sonra): 375×812 BASİT/PRO × koyu/açık, 1024×768 B-k ve P-a, 1280×800 P-k, 1440×900 B-a → **8/8 sorun 0**. Örtülme kontrolü de dahildir (`tur2-olcum-son.txt`).

Tek istisna, kabul aralığının dışındaki 1010×640'tır (kurucunun kısa ekranı):
- Tek ödeme satırı, Toplam, FARK ve Kaydet tam görünür.
- Açılan alanların alt önizleme satırı ("Tahmini iskonto", "Para üstü: Alınanı yazın") kabın dibinde kısmen kırpık; kaydırmayla erişilir.

K9 (ölçerin geometrik binmesi) 2–6 çıkıyor. Bunların hepsi kaydırma kabının kırptığı, görünmeyen içerik ile kap dışındaki öğenin geometrik kesişimidir (örnek: kap dışına kaymış "Kalanı ata" ⟂ "+ Başka ödeme"). Kırpma duyarlı görünür binme her kombinasyonda 0. Tur 1 tabanında da K9 1–6 vardı.

Kurucu ekranı ÖNCE/SONRA (1010×640 PRO koyu, kurucu2.mjs):
- ÖNCE: "Havale · Merkez Kasa 0 ₺" kesik; FARK 79.338,60 ₺; birincil gibi duran açık hesap.
- SONRA: tek satır "Havale · Ana Kasa 44.398,74 ₺" tam; FARK 0 ₺ ✓; Kaydet ve yazdır görünür.
- Görüntü: `tur2-pano-1.png`.

"Neyin ne olduğu" metin taraması (her ödeme satırında tür, kasa ve tutar), örnekler:
- `Kart | · Ana Kasa | girdi=10.000₺ [alan-cizgi 1px solid]`
- `Havale · Ana Kasa 44.398,74 ₺`

## 4. Görülen görüntüler (Read, 4 adet)

1. `tur2-pano-1.png`: kurucunun webp'si ile SONRA (1010×640).
2. `tur2-pano-2.png`: 1280×800 BASİT koyu ve 1024×768 BASİT açık, karma + İndirim. İndirim alanı görünür, haplar tam, FARK ve Kaydet görünür.
3. `tur2-pano-3.png`: 1024×768 PRO açık para üstü alanı ve 1280×800 PRO koyu 3. satır. Yeni satır görünür, üstte "yukarıda daha var" gölgesi.
4. `tur2-pano-4.png`: 375 BASİT koyu karma + İndirim (t2, scroll-margin öncesi; alan şeridin arkasındaydı, t4'te giderildi ve ölçümle doğrulandı), 1440×900 PRO açık açık hesap, 1440×900 PRO koyu tek ödeme.

## 5. Tuval kıyası (betikler/tur/tuval-kiyas.mjs, 1440×900)

| Eşleme | Sonuç | Açıklama |
|---|---|---|
| tezgah-koyu | 36 öğe · AYNI 35 · **FARK 1** | `ödeme1 kasa` line-height 21.75 → 18.16. Kaynağı bu paket DEĞİL: `wwwroot/css/yasal.css:109` `.yz-tezgah-odeme-ad > .yz-tezgah-kasa { line-height: 1.2109 }` (YASAL şeridi, 10.10 22:40). Getcomputed kural izi alındı. 4D kıyasında (10.10 19:06) AYNI idi. |
| tezgah-acik | 36 · AYNI 35 · FARK 1 | aynı kural |
| hal-indirim-koyu | 7 · AYNI 7 · FARK 0 | — |
| hal-karma-koyu | 11 · AYNI 9 · FARK 2 | `[tutar][Çıkar]` bilinçli düzen (belirsiz eşleşme tablosu) |

Bugün, Müşteriler ve Tüm ekranlar: bu paketin bütün seçicileri `.yz-tezgah` / `.yz-tezgah-karma` kapsamında olduğu için o ekranlara dokunulmadı. Kıyas koşulmadı (dokunulan sayfa: yalnız Tezgâh).

## 6. Canlı doğrulama (API 5176, vekil 5177, gerçek Chrome arayüzünden Kaydet)

POST gövdeleri sayfadaki `fetch` dinlenerek alındı. Yanıtlar okundu.

| Senaryo | Uç | HTTP | Gövde (özet) | SQL (hareketler / odeme_kalemleri) |
|---|---|---|---|---|
| C1 tek ödeme | POST /islemler/satis | 200 | odemeTuru NAKIT, birimFiyatTl 11747.11 | SATIS 11747.11 NAKIT · kalem NAKIT 11747.11 |
| C2 nakit + kart | POST /islemler/satis-fisi | 200 | NAKIT 9987.69 + KART 5000 (taksit 1) | SATIS 14987.69 KARMA nakit 9987.69 · kalemler NAKIT 9987.69 / KART 5000.00 |
| C3 İndirim %5 | POST /islemler/satis-fisi | 200 | fisIskontoYuzde 5, NAKIT 2280.00 (2400 × 0,95) | SATIS 2280.00 indirim_tutari 120.00 · NAKIT 2280.00 |
| C4 açık hesap | POST /islemler/satis-fisi | 200 | NAKIT 2000 + VERESIYE 1200.00 | SATIS 3200.00 KARMA nakit 2000 veresiye 1200 · NAKIT 2000 / VERESIYE 1200 |

Sunucu kuralları aynen çalıştı. Önceki denemelerde aynı müşteriye nakit toplamı 30.000 TL'yi aşınca 400 `NAKIT_SINIRI` döndü ve "Peşin Müşteri tekrarı" sorusu (400) geldi. Bu kurallara dokunulmadı.

- SQL dökümü: `/private/tmp/kuyumcu-serit/YILDIZ-TEZGAH-ODEME/sql-sonra-t2.txt`.
- Gövde eşitliği bUnit ile kilitli: YTO_05 (Çıkar sonrası tek ödeme gövdesi, elle seçilen tek ödemeyle aynı) ve YTO_06 (açık hesap VERESIYE bacağı).

## 7. Test sayıları

- Paket sınıfı `YildizTezgahOdemeTestleri`: tur 1'de 11 → tur 2'de **13**, hepsi yeşil.
- Tam süit (son kod, `--logger trx`, `/private/tmp/kuyumcu-serit/trx-pYILDIZ-TEZGAH-ODEME`): **12506 test · 12499 geçti · 4 başarısız · 3 atlandı**. 4 kırmızının hiçbiri bu paketin değil (§ 10).
  - Aynı tur içindeki ilk tam süitte 7 kırmızı vardı. Bunlardan **Y1_10 bu paketindi** (kaydırma gölgesinde kural gövdesinde color-mix). Özel özelliğe taşınarak düzeltildi; ikinci koşumda yeşil.
  - Y4C_44 ve Yasal4 Y4A_tur4 ikinci koşumda yeşil; kararsız/eşzamanlı şerit kaynaklı.
- Derleme: `dotnet build Kuyumcu.sln` → **0 hata 0 uyarı** (son temiz derleme). Ara tam derlemelerde görülen 6–8 uyarının tamamı başka şeritlerin test dosyalarında: SUBEKAPSAMA, KesifP2, SonKalanlar, Yildiz2, YildizKatman.
- Migration: gerekmedi.

## 8. Mutasyon tablosu

Her satırda yöntem aynı: yedek alındı, tek eşleşmeli değişiklik yapıldı, `dotnet test --filter YildizTezgahOdemeTestleri` koşuldu, yedek geri kopyalandı, `cmp` ile karşılaştırıldı. Sonuncudan sonra temiz build alındı.

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| M-YTO-1 | kap `overflow-y: auto` → `visible` | YTO_07 | AYNI |
| M-YTO-2 | açık hesap `.yz-b2` → `.yz-b3` | YTO_02, YTO_06 | AYNI |
| M-YTO-3 | `TekeIndirilebilir = false && …` | YTO_03, YTO_05 | AYNI |
| M-YTO-4 | açık hesap görünürlüğünden `GercekMusteri` koşulu silindi | YTO_06 (×2) | AYNI |
| M-YTO-5 | karma tutar sütunu 2 → 3 | YTO_06, YTO_08 | AYNI |
| M-YTO-6 | teke inme eşiği `>= 2` → `>= 0` | YTO_04 | AYNI |
| M-YTO-7 | tutar alanı alt çizgisi silindi | YTO_04, YTO_08 | AYNI |
| M-YTO-8 | haplar yeniden kabın içine (tur 2) | YTO_01 | AYNI |
| M-YTO-9 | indirim kaydırma hedefi null | YTO_10 | AYNI |
| M-YTO-10 | para üstü kaydırma hedefi null | YTO_11 | AYNI |
| M-YTO-11 | yeni satır hedefi `{n-1}` → `{n}` | YTO_11 | AYNI |
| M-YTO-12 | ilk çizimde de kaydır (`if (true)`) | YTO_10 | AYNI |
| M-YTO-13 | alt gölge katmanı silindi | YTO_07 (satır 311) — bkz. not | AYNI |
| M-YTO-14 | dar ekran `scroll-margin-bottom` silindi | YTO_07 | AYNI |
| M-YTO-15 | kısa ekran aralığı 16 → 28 | YTO_07 | AYNI |
| M-YTO-16 | "Değiştir" `flex-shrink: 0` → `1` | YTO_08 | AYNI |

Not (Anayasa md.1): M-YTO-13'ün renk belirtecine taşındıktan sonraki **ilk** koşumu ve aynı mutasyonun ilk elle denemesi build'li koşumda YEŞİL kaldı. Sebebi tespit edilemedi; aynı ağaçta eşzamanlı başka bir build olasılığı var. Sonraki iki koşum (biri `--no-build`, biri build'li tam sınıf) YTO_07'yi satır 311'de KIRMIZI yaptı. Her denemede `cmp` AYNI.

## 9. Denetim (dotnet/denetim.sh)

Sonuç: `DENETİM BAŞARISIZ`. İki ✗ var, ikisi de bu paketin değil:
- `P-30 DEMO_INSAN_SURESI (BK-45)`: kabulde hariç tutulan madde.
- `GIRDI-2 TEST_GECERSIZ_FIKSTUR = 1`: `Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` IBAN fikstürü (MASAK eşzamanlılık doğrulayıcı şeridi).

Bu paketin sayaçları ✓: Pages/ ModalPencere artışı 0, sabit renk 0 (Y1_10 yeşil). Çıktı: `/private/tmp/kuyumcu-serit/YILDIZ-TEZGAH-ODEME/denetim.log`.

## 10. Açık kalanlar (sahibiyle)

1. **Tezgâh tuval kıyası FARK 1** (`ödeme1 kasa` line-height): `wwwroot/css/yasal.css:109`, YASAL şeridi (YASAL-KAPI-3 / t9 alanı). 1.2109 yerine tuvalin 1.45'i (21.75 px) gerekir ya da kural yalnız `.yz-tezgah-kasa` satır yüksekliğini bırakmalı. Bu paket ortak dosyaya dokunmadı.
2. Tam süitte başkasına ait kırmızılar:
   - `RaporDortTestleri.RD_KH01`, `RD_KH02` ("Rapor dönemi en çok 400 gün"): rapor şeridi.
   - `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula…`: YASAL şeridi.
   - `Yildiz5ATestleri.Y5A_04_emanet_satirlari_fifo_storno_esik` ("Emanette" ≠ "Asti"): YILDIZ-5A şeridi (t1).
3. Denetim: `GIRDI-2 TEST_GECERSIZ_FIKSTUR` (MASAKESZAMAN doğrulayıcı testi) ve `P-30 BK-45` (kurucu kronometresi).
4. 768 px'in altındaki yüksekliklerde (kabul dışı; 1010×640) açılan alanların son önizleme satırı kabın dibinde kısmen kırpık kalır. Kaydırma ve gölge işaretiyle erişilir. Kurucu daha kısa ekranlar için ek düzen isterse ayrı karar gerekir.
5. Ölçer `sikisma-olc.js` K9'u kaydırma kabının kırptığı içerikte de sayıyor. Kırpma duyarlı binme 0 olsa da K9 2–6 görünür. Ölçer bakımı orkestratöre önerilir.

## 11. DoD sözleşmesi

Yeni varlık veya kayıt türü yok, sunucu ucu yok, migration yok. Düzenleme yolları mevcut sayfa bileşenlerinindir:
- Ödeme satırı: araç, kasa ve tutar FisOdemeBacaklari'nda; "Çıkar" ve "− Tek ödeme" ile.
- İndirim: FisIskontoSatiri.
- Açık hesap: AcikHesapKaydet; kapı ve gövde aynı.

Her düğme ya istek atar ya da disabled ve sebep (title) taşır. Ekran başına tek birincil düğme (YTO_02). Yıkıcı eylem eklenmedi.

## 12. Kurucuya / mali müşavire

Mevzuat hükmü yazılmadı ve değiştirilmedi. Nakit sınırı, MASAK ve tevsik kuralları sunucuda aynen çalıştı (canlıda NAKIT_SINIRI 400 gözlendi). Mali müşavir sorusu yok.

## 13. Değişen dosyalar (bu paket)

`git status --porcelain | grep -v -E "/(bin|obj)/"` içinde bu paketin dokunduğu dosyalar (ağaçta hepsi `??`, yani izlenmiyor):
- `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizTezgah.razor` (tur 1 + tur 2)
- `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizTezgahVerisi.cs` (tur 1: `TekeIndirilebilir`)
- `dotnet/Kuyumcu.Web/Pages/Islemler.Yildiz.cs` (tur 1: `TekeIndirilebilir`, `AcikHesapGorunur` koşulu)
- `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css` (yalnız `/* TEZGAH-ODEME */` bloğu)
- `dotnet/Kuyumcu.Tests/YildizTezgahOdemeTestleri.cs`

Dokunulan sayfalar: yalnız Tezgâh (Islemler, Yıldız dalı). FisOdemeBacaklari, yasal.css ve ortak JS değiştirilmedi.

## 14. Temizlik

- Kendi süreçlerim durduruldu: API (5176), vekil (5177) ve kendi profilimle açılan başsız Chrome (9851). Üç port da boş.
- Test kiracıları `zsh kiraci-temizle.sh <id> --uygula` ile silindi: 01M4KQ9EZXWSWHJ5J5SBTDSSM1 ve 01M4KW3MWTK50XTRM9QB1HERWZ. Kalan satır 0, `tenants` sorgusu 0.
- `tenants` sayısı: ikinci kurulumdan önce 24, sonra 23. Fark, eşzamanlı şeritlerin kendi kiracılarını silmesinden geliyor; kalan PAKET kiracıları listelendi ve bu paketin kiracısı yok.
- QA-TEST kiracılarına dokunulmadı.
