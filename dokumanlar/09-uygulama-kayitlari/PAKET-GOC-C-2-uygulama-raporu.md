# PAKET GOC-C-2 — GOC-C kapanış devri · uygulama raporu (09.10.2026)

> Rol: Taşkın (backend) + Ertuğ (frontend). Şerit t1 · canlı API 5160 · vekil 5161 · CDP 9701–9712 · `KUYUMCU_TEST_DB=kuyumcu_test_t1`.
> Kaynak: görev EK KOŞULLARI (tasarım hakemi bulguları) + `GIRDI-KURALLARI-ENVANTER-2026-10-08.md` (açık #4/#6, F2, G1). Önceki paket: `PAKET-GOC-C-uygulama-raporu.md` § 14.
> COMMIT YOK. Dosya silinmedi/taşınmadı. 5199/5200/5241'e dokunulmadı. Sır/parola/jeton değeri çıktıya yazılmadı (fikstürler chmod 600).
> Migration: **gerekmedi** (şema değişikliği yok). Tur: **2** (tur 1 § 1–12; tur 2 = hasım doğrulayıcı bulgularının kapanışı § 13 — tur 2 sonuçları tur 1'in aynı başlıklı satırlarının yerine geçer).

## 1. Yapılanlar

### 1.1 VERI-1 — karma ödemede ilk satırın sessiz silinmesi (VERİ)
- **Kök neden (doğrulandı):** `FisOdemeBacaklari.TutarYaz` her tuşta `FisFark.SifirSatirlariDusur` çağırıyordu (SATIS-3 t10). İkinci satıra rakam rakam 99999 yazılınca otomatik kalan ilk satırı (Kart) `max(0, 10.000 − 99.999) = 0`'a indiriyor ve "otomatik 0'a inen satır düşer" kuralı onu **kullanıcı sormadan siliyordu**.
- **Düzeltme:** çağrı kaldırıldı — hiçbir satır kendiliğinden silinmez. 0 ₺'lik satır k2'den beri Kaydet'i kilitlemez ve sunucuya gitmez (`FisFark.Gonderilecek`), yani silmeye gerek yoktu. Aşımda: son elle yazılan satırın **altında** tam genişlik ileti `#islem-bacak-N-asim` (role=alert, "Bu tutar fiş toplamını 89.999 ₺ aşıyor. Azaltın ya da “Kalanı ata”ya basın."), Kaydet kilitli + sebep (mevcut `FarkFazla`), **"Kalanı ata"** aşan satırı fiş toplamına indirir (eksiye inmez — `Math.Max(0, …)`; eksik durumunda eski davranış birebir). `SifirSatirlariDusur` saf kural olarak yerinde (eski sözleşme testleri), ekrandan çağrılmaz (`FisFark.cs` remarks).
- **Panel ölçeği (hakem YÜKSEK "BASİT 1280 dikey yığılı/büyük punto"):** satır düzeni (araç · ayrıntı · tutar · ✕, etiketler başlık satırında) eskiden yalnız ≥ 1400 px **viewport**'ta açılıyordu; 1280'de ALDIM ~670 px olmasına rağmen her satırda "Ödeme aracı * / Tutar *" etiketleri ve 21 px "Ödeme" başlığı vardı. Ölçüt artık sütunun **kendi genişliği** (`@container gc2-aldim (min-width: 380px)`, değerler ≥1400 kurallarının birebiri) → 1280'de de tek bacak ölçeği; 375'te eski dikey form. Ölçüldü: ödeme kartı 305 → 239 px, satır 598×70 flex → 634×69 grid.

### 1.2 VERI-2 — açılış stoğunda "1,5" → 15 (VERİ) + sınıf taraması
- **Kök neden (doğrulandı):** `girdi-maske.js` `sayi` maskesi tam sayı alanında (`data-tam="1"`) ayırıcıyı `/[^0-9]/g` ile **siliyordu**: tuşta "," engelleniyor, ardından gelen "5" eklenip **15** oluyordu; yapıştırma ve `input` yolunda "1,5" doğrudan "15"e dönüyordu (sessiz 10 kat).
- **Düzeltme (tek dosya, tüm tam sayı alanları):** (a) maske ayırıcıyı artık silmez (yapıştırılan "1,5" kutuda "1,5" kalır → C# `SayisalAlan` "Küsuratlı yazılamaz" + Kaydet kilidi); (b) **tuş engeli:** tam sayı kutusunda "," / "." yazılmaz ve kutu "kesir kipine" girer — kutunun sonuna yazılan rakamlar (kesir haneleri) da yazılmaz, kutunun altındaki ipucu (`<id>-kesir`, `Sayi.razor` çizer, gizli) **görünür olur** ("Adet küsuratlı yazılamaz; tam sayı girin."). Silme/ok tuşu kipi kapatır; odak kaybında ipucu kalır (kullanıcı neden "1" kaldığını görür). IBAN imleç düzeltmesine dokunulmadı.
- **Tavan:** `AlanSinirlari.AcilisStokEnCok = 1.000.000` (Core, TEK sabit) — sunucu `[AcilisAdedi].EnCok` artık bu sabiti okur; ekran `u-acilis-stok` (ADET `<Adet>` ve GRAM `<Sayi>`) `EnCok` alır → 99.999.999 yazarken alan altı hata + kilit.
- **GRAM:** `<Sayi EnCokOndalik=3>` "1,5" ve "1.5" → 1,5 (P-78 çözücü; test GC2G04).
- **Sıfır/eksi iletisi sözlükten:** ADET'te eksi tuşta yazılamaz, 0 → Core `AdetKurali` (GirdiSozlugu); sunucu 0,0001/−1 → `UrunAcilisSozlugu.AdetSifirdanBuyuk` (canlı § 6).
- **Sınıf taraması** (aynı sessiz 10 kat başka alanda var mı):

| Alan grubu | Adet | Durum |
|---|---|---|
| `<Adet>` (Urunler açılış, SiparisDetay, Vezne ×2, UrunEtiketleri, Siparisler, FisTakas, FisTransfer, FisKalemGirisi) | 9 | aynı maske → **kapandı** (tek dosya) |
| `<GunSayisi>` (Bildirimler ×3, FisOdemeKasa vade) | 4 | aynı maske → **kapandı** |
| `<Sayi TamSayi=…>` (Sayim sy-adet, Pos valör/taksit, UrunAgaci sıra, Urunler min-stok ×2/yerleşim/sayım, Vitrin pano-sira) | 9 | aynı maske → **kapandı** |
| Ondalıklı `Para/Gram/Sayi` | — | maske ayırıcıyı hiç silmiyordu; "1.500" P-78 "belirsiz" hatası → 10 kat yok |
| C# `decimal.Parse(… NumberStyles.Number/Any, Invariant)` tarama (21 satır) | 0 | kullanıcı metni ayrıştıran yok (Kurulum `Sayi()` ölü kod; Urunler `YerAdetYaz` Invariant decimal metni alır; A4Belge sunucu hücresi) |

### 1.3 KRİTİK — /urunler PRO 1280 binme/kesik + filtre
- **Yeniden üretildi** (8 ürün, biri 50 karakter barkodlu): tablo 1283 px > kap 962 px; yapışkan eylem sütunu kaydırılan Durum/Stok hücrelerine biniyordu (K9 6), `#urun-durum-filtre` 84 px (K12 1).
- **Düzeltme (zo-islem.css):** barkod hücresi ≥ 561 px'te `width: 15ch; overflow-wrap: anywhere` (13 haneli EAN tek satır, uzun barkod karakterde sarar, kesilmez) · PRO durum süzgeci `min-width: 6rem` · arama yer tutucusu 55 → 20 karakter (`GocC2Sozlugu.UrunAraKisa` "Ad, kod ya da barkod" — hakemin "barl" kesiği; okutucu Enter'ı kendisi basar, Enter yolu aynen).
- **375 sayfa boyu:** ürün kartı iki sütunlu (etiket–değer çiftleri yan yana; ad ve eylemler tam satır; has/barkod tam satır — yarım sütunda K3 kırığı ölçüldü; görseli olmayan ürünün simgesi kartta yer kaplamaz). PRO 375: **5,3 → 3,8 ekran** (hakem 6,4), BASİT 375: 4,7 → 2,8.

### 1.4 /vitrin PRO 375 sayfa boyu (hakem K4 10,1 ekran)
Ayar katlanırı (`vitrin-ayar-kat`) PRO'da da **kapalı** gelir (yönetim ayarı zorunlu iş değil — kurucu 25.09 temel kuralı). Varsayılan /vitrin PRO 375: **10,1 → 1,6 ekran**. Açıldığında 9,5 ekran (≈25 kalem kartı); iki sütunlu kart denendi, K3 kırık 44 ölçüldüğü için **geri alındı** (§ 9).

### 1.5 Ham tablo → ortak ızgara (5 tablo; HAM_TABLO_ORTAK_GRID_DISI 13 → **8**)
GOC-C'nin gerekçeli istisnaları kapandı (Grid/* değiştirilmeden, yalnız kullanılarak):

| Tablo | Izgara | İstisnanın kapanışı |
|---|---|---|
| `Vitrin #vitrin-ayarlar` | `grid:vitrin:vitrin-ayarlar` | başlıktaki "değiştirilebilir" rozeti → ızgaranın üstünde TEK satır (`#vitrin-duzenlenebilir-sutunlar`, PRO; rozet + düzenlenebilir sütun adları); `BaslikId` ile `vitrin-baslik-*` kimlikleri korunur |
| `FisVerdimAldim #islem-aldim-tablo` | `grid:islemler:aldim` | seçici + kasa adı tek hücrede; seçici varken "Ayrıntı" sütunu çizilmez (colspan gerekmez) |
| `FisKalemTablosu #islem-kalem-tablo` (BASİT) | `grid:islemler:kalem-tuval` | "ayar · gram" ikinci satırı td `data-ayrinti` ::after → hücrede span (aynı metin/biçim) |
| `FisKalemTablosu #islem-kalem-pro` (PRO) | `grid:islemler:kalem-pro` | satır içi adet hücre şablonunda; `islem-kalem-milyem/gram/has/iscilik-N` kimlikleri hücre içi span'da; ✎ düzenleme ve satır altı adet iletisi ızgaranın ALTINDA panel (`aria-controls`) |
| `FisSonSatislar` (PRO) | `grid:islemler:son-satislar` | ödeme kırılımı ızgaranın altında tek panel (`.islem-kirilim-satir`, kimlikler aynı) |

Tuval düzeni korundu (zo-islem.css, kimlik seçicili): çerçevesiz kap; BASİT tuvalde sütun çubuğu gizli (zorunlu önce; PRO'da görünür); ≤ 560 px'te ızgaranın ortak kart kalıbı tuval satırını bozmasın (tek sıra ad · tutar · sil); PRO dar kartta tuvalin "ikincil sütun düşer" kuralları (t19) — ölçüm görüntüsünde ızgara kart kuralının bunları ezdiği **görüldü ve düzeltildi**; tuval başlık biçimi (büyük harf değil) geri getirildi; `Islemler.razor`'daki dış `KartListe` kalktı (ızgara kendi KartListe'si `islem-kalem-liste`).

### 1.6 Açıklama satırları
- **Nakit sınırı notu** (`FisNakitUyari`): dar sağ sütunda (272 px) tam cümle 4–5 satırdı (ölçüldü 89 px). Görünen: tek kısa satır "Nakit sınırı: 30.000 ₺" (sayı sunucunun haddi) + ⓘ; tam cümle (kuralla aynı söz, t11/t12 sözleşmesi) ⓘ'nin içinde; `#islem-nakit-uyari` kimliği, `[hidden]`, `data-yakin` aynen. Ölçümde dış kutu kimliği değişince notun sol sütunun tepesine düştüğü **görüldü** → `#islem-nakit-uyari-kutu` sağ sütun yerleşimi eklendi, sayısal doğrulandı (x=984, Son satışların altında, 272×55).
- **"Eski altınla ödeme için…"**: gerçek tarayıcıda görünür öğe taraması **0** (metin `gorsel-gizli`, yalnız ekran okuyucuda) — değiştirilmedi.

### 1.7 400 gövdesinde `message` biçimi (DÜŞÜK) — **devredildi**
Canlı kanıt: DTO doğrulama yolu `message` = **dizi** (`Program.cs InvalidModelStateResponseFactory`), servis yolu = **dizge** (`ApiException`). Değişiklik `Program.cs`'tedir (ileti/dil katmanı) — DIL-SUNUCU şeridinin (t2) alanı ve görevin DOSYA ALANI dışında → yapılmadı, § 9'a sahibiyle yazıldı.

## 2. Sapmalar ve gerekçeleri
1. **Tuş engeli + kesir kipi:** "ADET'te '1,5' yazılamaz (tuş engeli)" iki yolla karşılandı: tuşta ayırıcı ve ardından gelen kesir haneleri yazılmaz (sonuç "1", ipucu görünür); yapıştırmada metin korunur ve C# kuralı reddeder (Kaydet kilitli). Yalnız tuş engeli bırakılsaydı "1,5" → "15" (bugünkü hata) olurdu.
2. **Açılış tavanı iletisi:** ekran `SayiKurali.Aralik` cümlesi ("En çok 1.000.000 olabilir."), sunucu `UrunAcilisSozlugu.AdetCokBuyuk` ("Adet çok büyük — en fazla 1.000.000 girilebilir.") — sayı tek sabitten, cümle iki kaynaktan (URUN-FORM-2 tezgâhtar metni korunmak istendi).
3. **Vitrin ayar katlanırı PRO'da kapalı:** 7 mevcut test "PRO açık" varsayıyordu → test önce açar (`GocC2Ortak.AyarlariAc`); T6 testinin sözleşmesi "kapalı gelir → aç → Sıra sütunu var" olarak sıkılaştı (gevşetilmedi).
4. **Güncellenen mevcut testler** (her birinde gerekçe satırı; hiçbiri gevşetilmedi): SATIS3Ekran T10 (satır artık düşmez, 0 ₺'lik satır gövdeye girmez — sınandı), SATIS3Bilesen, TS1TuvalSatis, TS2TuvalPro (span title/ayrıntı), PatchDort P4 (tr → div), VitrinEkran ×2, P17Ekran, OnayKipi ×3, Toplu4 ×2, Toplu6 ×2, KatmanTekKalip K1_25 (`tablo-sar` yerine Izgara TabloId), MobilParite B9 (sayfa tablo çizmiyorsa); DI kaydı (ızgara ApiIstemci ister): SATIS2Sinir, TS1Sinir, TS2SinirTur2, KatalogSonSatis, KESIFP5SinirKutu, P78 FisKalemTablosuTurkce, PatchDortFisTavan, KesifP5Bilesen.
5. **OlcekYuzEnvanteri.txt +5 satır:** karma panelin container kuralları, onaylı ≥1400 satırlarının (92–96) BİREBİR değerleri.
6. **Ekran Read bütçesi aşıldı:** görüntü 4 yerine **5** kez okundu (§ 7.2) — beşincisi PRO 375 tuvalde gerçek bir kusuru (ikincil sütunlar geri gelmişti) yakaladı. Kurucu bütçesine aykırı; kayda geçti.

## 3. Migration
Gerekmedi.

## 4. Testler
- **Yeni:** `Kuyumcu.Tests/GocC2Testleri.cs` — 20 test (GocC2Odeme ×4 · GocC2Girdi ×6 · GocC2Maske ×1 [gerçek `girdi-maske.js`, node'da sahte DOM ile tuş tuş] · GocC2Ekran ×9 [T01 teori ×5]) + `SATIS3EkranTestleri.GOCC2_karma_…` (sayfa düzeyi rakam rakam) = **21**; yardımcı `GocC2Ortak.cs`.
- Filtre `GocC2|GOCC2`: **21/21**. CSS okuyan geniş grup (Tasarım/Olcek/Toplu/SATIS/TS1/TS2/GocC/Katman/DugmeIki/MobilParite/Css/Yildiz/Sade): **1976/1976** (+2 atlanan, son kod).
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pGOC-C-2/`):
  - 1. koşum `tam.trx`: 11481 · geçti 11458 · **başarısız 21 — hepsi bu paketin** (ızgaraya taşınan bileşeni tek başına çizen 18 test DI kaydı istedi; OL26 envanter; KesifP1 gizli ipucu = hata metni; MobilParite B9). Düzeltildi (§ 2.4).
  - 2. koşum `tam2.trx` (son kod): **Toplam 11489 · Yürütülen 11487 · Geçti 11487 · Başarısız 0 · Atlanan 2.** (Önce: GOC-C tur 2 sonu 11369 test, 1 kırmızı saat-dilimi.) Bu paketin YENİ kırmızısı: **0**. Not: tam2'den sonra yalnız CSS (2 kural) ve GC2T02'ye bir CSS iddiası eklendi; CSS okuyan grup son kodla yeniden koşuldu (1976/1976).

## 5. Mutasyon tablosu (yedek `/private/tmp/kuyumcu-serit/mut-yedek/GOC-C-2/` → TEK eşleşme → derle → filtreli test → geri kopya → `filecmp` birebir; betik `GOC-C-2/mut.py`, sonuç `mut-sonuc.json`)
| Kod | Kural kapatıldı | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M2-1 | VERI-1: `SifirSatirlariDusur` yeniden çağrıldı | GC2E01, GC2E02, GC2E03, GC2E04, GOCC2_karma_… | AYNI |
| M2-2 | VERI-1: aşım iletisi hiç yazılmaz | GC2E01, GC2E03, GOCC2_karma_… | AYNI |
| M2-3 | Kalanı ata eksiye inebilir (`Max(0,…)` yok) | GC2E04 | AYNI |
| M2-4 | VERI-2: maske tam sayıda ayırıcıyı siler (eski hata) | GC2M01 | AYNI |
| M2-5 | VERI-2: kesir kipinde rakam engeli kalktı ("1,5"→"15") | GC2M01 | AYNI |
| M2-6 | Açılış ADET alanında tavan kalktı | GC2G06 | AYNI |
| M2-7 | PRO durum süzgeci 4rem (ezik) | GC2T03 | AYNI |
| M2-8 | Nakit notu yine tam cümleyle görünür | GC2T02 | AYNI |
| M2-9 | Kesir ipucu öğesi çizilmez | GC2G01, GC2G02 | AYNI |
| M2-10 | Vitrin ayarı PRO'da yine açık başlar | Toplu6 T6_Vitrin_PRO_ayar_tablosu_… | AYNI |
10/10 kırmızı, 10/10 cmp AYNI. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Hata**; bu paketin dosyalarından uyarı **0** (tam yeniden derlemede 6 analizör uyarısı başka şeritlerin test dosyalarında: SUBEKAPSAMASinir, KesifP2, SonKalanlar ×2, Yildiz2 ×2 — GOC-C § 14.3 ile aynı).
⚠ Dürüstlük notu: eşzamanlı başka şerit derlemeleri iki kez bayat `Kuyumcu.Tests.dll` bıraktı (bir filtreli koşum eski kodla koştu); DLL zaman damgası + DLL içinde yeni dizge aranarak doğrulandı ve yeniden koşuldu. Rapordaki sayılar doğrulanmış DLL'le alınmıştır.

## 6. Canlı doğrulama (API 5160 Development, `kuyumcu_net`; kiracı `PAKETGOC-C-2-TEST …` = 01M4F00G8A7K30HKT2JXEXSED8; A=SAHİP, B=MÜDÜR; betikler `GOC-C-2/kurulum.py`, `canli2.py`, `canli3.py`)
| Adım | Beklenen | Gerçek | İleti / SQL |
|---|---|---|---|
| Açılış 1,5 (ADET) | 400 | 400 | "Küsuratlı yazılamaz; tam sayı girin." · acilisStogu.adet |
| Açılış 0,0001 / −1 | 400 ×2 | 400 ×2 | "Adet sıfırdan büyük olmalı." |
| Açılış 99.999.999 / 1.000.001 | 400 ×2 | 400 ×2 | "Adet çok büyük — en fazla 1.000.000 girilebilir." |
| SQL: açılış 400'lerinde ürün yok | 0 | 0 | urunler=0 |
| GRAM ürün açılış 1,5 | 201 | 201 | `stoklar.adet = 1.500` |
| Karma Kart 10.000 + Nakit 99.999 (fiş 10.000) | 400 | 400 | "Ödeme bacaklarının toplamı fiş tutarına eşit olmalıdır …" |
| Karma 0 ₺'lik satır gövdede | 400 | 400 | odemeler[0].tutarTl (ekran bu satırı göndermez) |
| SQL: karma 400'lerinde hareket/kasa hareketi yok | 0 | 0 | — |
| Karma Kart 6.000 + Nakit 4.000 (A) | 200 | 200 | `SATIS tl=10000.00` · kasa_hareketleri `NAKIT:4000 \| KART:6000` |
| Satış tek ödeme (B) | 200 | 200 | `SATIS adet=1.000 tl=10000.00` |
| Karma Kart 7.500 + Nakit 2.500 (B) | 200 | 200 | `NAKIT:2500.00 \| KART:7500.00` |
| Karma aşım (B) + SQL hareket yok | 400 · 0 | 400 · 0 | hareketler 11 = 11 |
| İade 1 adet | 200 | 200 | `SATIS_IADE adet=1.000 tl=-10000.00` |
| Takas (3 g K22 ↔ 1 adet, fark kartla) | 200 | 200 | `TAKAS eskiHas=2.748000 tl=11756.00` |
| Hurda alış 0,5 g K22 | 200 | 200 | `HURDA_ALIS has=0.458000 tl=-1374.00` |
| **Gerçek Chrome, BASİT 1280, rakam rakam 9→99999** (`karma.mjs`) | 2 satır her adımda | 2 satır ×5 | Kart 9.991→…→0, satır KALDI; alan altı "…89.999 ₺ aşıyor…"; Kaydet kilitli "Fazla 89.999 ₺ — ödeme tutarını azaltın"; Kalanı ata → Kart satırı 10.000, Kaydet açık (PRO'da da aynı) |
| **Arayüzden karma kayıt** (Nakit 6.000 + Kart 4.000) | 200 + SQL | kaydedildi | `SATIS 10000.00 · KART:4000.00 \| NAKIT:6000.00` |
Uyumsuz satırların açıklaması (dürüstlük): B ilk denemede şubesiz olduğu için "şube seçimi gerekli" (doğru davranış) → B'ye SQL ile test kiracısının şubesi atandı; ilk hurda "kasada nakit yetersiz" (doğru davranış) → 0,5 g; ilk takas aynı gün kümülatif nakit sınırına takıldı (doğru davranış, 459 VUK GT kapısı) → fark kartla. 400 ileti biçimi kanıtı: DTO yolu `list`, servis yolu `str` (§ 1.7).

### 6.1 Gerçek başsız Chrome (vekil 5161, `sikisma-olc.js` + `tur/ekstra.js` K12/K13/K10b; `GOC-C-2/olc.mjs`; önbellek kapalı)
10 görünüm × {1280×800, 375×812} × {BASİT, PRO} × {açık, koyu}; **modGercek = istenen mod 152/152** (olcum 76 + olcum2 52 + olcum4 24; olcum3 16 ek). Hücre = K1+K3(taşan+kırık)+K5+K7+K11+K12+K13+K10b+yatay · K9 · sayfa boyu (ekran). SON değerler:

| Görünüm | 1280 B-A | 1280 B-K | 1280 P-A | 1280 P-K | 375 B-A | 375 B-K | 375 P-A | 375 P-K |
|---|---|---|---|---|---|---|---|---|
| /urunler | 0·0·1,1 | 0·0·1,1 | 0·0·1,2 | 0·0·1,2 | 0·0·2,8 | 0·0·2,8 | 0·0·3,8 | 0·0·3,8 |
| /urunler yeni ürün | 0·6¹·1,1 | 0·6¹·1,1 | 0·6¹·1,2 | 0·6¹·1,2 | 0·6¹·2,8 | 0·6¹·2,8 | 0·6¹·3,8 | 0·6¹·3,8 |
| /vitrin (varsayılan) | 0·0·1,1 | 0·0·1,1 | 0·0·1,2 | 0·0·1,2 | 0·0·1,4 | 0·0·1,4 | 0·0·1,6 | 0·0·1,6 |
| /vitrin ayar açık | 0·0·4,1 | 0·0·4,1 | 0·0·4,2 | 0·0·4,2 | 0·0·7,8 | 0·0·7,8 | 0·0·9,5 | 0·0·9,5 |
| /islemler kalem | 0·0·1,0 | 0·0·1,0 | 0·0·1,1 | 0·0·1,1 | 0·0·1,6 | 0·0·1,6 | 0·0·1,6 | 0·0·1,6 |
| /islemler karma | 0·0·1,3 | 0·0·1,3 | 0·0·1,8 | 0·0·1,8 | 0·0·2,0 | 0·0·2,0 | 0·0·2,5 | 0·0·2,5 |
| /islemler karma aşım | 0·0·1,4 | 0·0·1,4 | 0·0·1,9 | 0·0·1,9 | 0·0·2,2 | 0·0·2,2 | 0·0·2,6 | 0·0·2,6 |
| /islemler nakit notu (25.000) | 0·0·1,0 | 0·0·1,0 | 0·0·1,1 | 0·0·1,1 | 0·0·1,7 | 0·0·1,7 | 0·0·1,8 | 0·0·1,8 |
| /islemler son satışlar (+kırılım PRO) | 0·0·1,0 | 0·0·1,0 | 0·0·1,6 | 0·0·1,6 | 0·0·1,6 | 0·0·1,6 | 0·0·2,4 | 0·0·2,4 |
| /islemler PRO ✎ satır düzenle | — | — | 0·0·1,2 | 0·0·1,2 | — | — | 0·0·1,9 | 0·0·1,9 |
**/urunler PRO 1280 açık+koyu K9 = 0, K12 = 0** (önce 6 / 1); 8 kombinasyonda K9/K12 = 0. (¹) Yeni ürün penceresinin ALTINDA kalan sayfa yazısı ("8 kayıt gösteriliyor" ⟂ "Kod") — ölçerin örtü kontrolü yok (GOC-A #12 / GOC-C ¹ ile aynı sınır); görüntüde örtüşme yok.
Ek sayısal doğrulamalar: PRO 375 kalem satırında görünen hücreler `urun, adet, tutar, eylem` (1280: barkod hariç hepsi) · nakit notu kutusu x=984 y=431 272×55 (sağ sütun) · `/urunler` 1280 tablo = kap (962 px).

### 6.2 Read ile görülen görüntüler (5 — bütçe 4, § 2.6)
1. `GOC-C-2/panel-BASIT-aydinlik-1280.png` (düzeltme ÖNCESİ karma panel: etiketli dikey satırlar, 21 px başlık).
2. `olcum/foto/islemler-karma-asim__BASIT_aydinlik_1280.png` — iki satır tek sırada, aşım iletisi satırın altında, "Kalanı ata", Kaydet kilitli + sebep. Burada ızgara başlığının "ÜRÜN · TUTAR" büyük harf yaptığı görüldü → düzeltildi.
3. `olcum/foto/urunler__PRO_karanlik_1280.png` — DURUM başlığı tam, "Aktif" rozeti Stok düğmesinden ayrık, 50 karakterlik barkod hücrede sarıyor; yer tutucu kesiği burada görüldü → kısaltıldı.
4. `olcum2/foto/islemler-nakit-notu__BASIT_aydinlik_1280.png` — nakit notunun sol sütunun tepesine düştüğü görüldü (ölçer yakalamadı) → düzeltildi, sayısal konum doğrulandı.
5. `olcum2/foto/islemler-pro-duzenle__PRO_karanlik_375.png` — PRO 375'te tuvalin ikincil sütunlarının geri geldiği görüldü → düzeltildi, hücre görünürlüğü sayısal doğrulandı (olcum4 24/24 = 0).

### 6.3 Temizlik
API (PID dosyası) ve vekil durduruldu; `lsof` 5160/5161 boş; kendi Chrome süreçlerim (profil `GOC-C-2/*`) kapatıldı — başka ajanın profiline dokunulmadı. `zsh kiraci-temizle.sh 01M4F00G8A7K30HKT2JXEXSED8 --uygula` → ikinci çağrı "Kiracı YOK (bağlantı çalışıyor)"; davet satırı silindi (`GOCC2DAVET%` = 0). **tenants: başlangıç 17 → şimdi 18**; fark başka şeridin kiracısı (`PAKETKUR-BANT-TEST Hakem3…`, 01:38 UTC = bu paketin başlangıcından sonra); başlangıç anından önce oluşturulmuş kiracı sayısı = 17. QA-TEST-* kiracılarına dokunulmadı.

## 7. denetim.sh (son kod)
`DENETİM BAŞARISIZ` yalnız **P-30 DEMO_INSAN_SURESI** (kurucu kalemi, görev "P-30 hariç"). İlk koşumda bu paketin ürettiği 3 ihlal bulundu ve kapatıldı: P-39 TABLO_SARMALAYICISIZ 3 (yorumlardaki "&lt;table&gt;" sözcüğü sayılıyordu), B-3 ESIK_ISTEMCIDE 6/4 (yorumdaki eşik sayısı), OLCEK-1 YUZ_OLCU_ENVANTER_DISI 5 (envantere işlendi).
| Sayaç | Önce | Sonra |
|---|---|---|
| GRID-1 HAM_TABLO_ORTAK_GRID_DISI | 13 (GOC-D sonrası tavan) | **8** (tavan 8'e indirildi — cırcır) |
| P-36 HAM_INPUT / HAM_SELECT | 8 / 14 | 8 / 14 |
| GIRDI-2 SAYISAL_OLU_SINIR | 0 | 0 |
| B-3 ESIK_ISTEMCIDE | 4 | 4 |
| OLCEK-1 YUZ_OLCU_ENVANTER_DISI | 0 | 0 |

## 8. Değişen dosyalar (bu paket)
- Core: `I18n/Sozlukler.GocC2.cs` (YENİ, TR+EN), `Domain/Girdi/GirdiHatasi.cs` (`AlanSinirlari.AcilisStokEnCok`).
- Api: `Modules/Urun/UrunModulu.cs` (yalnız `[AcilisAdedi].EnCok` → tek sabit).
- Web: `Bilesenler/Fis/{FisOdemeBacaklari,FisKalemTablosu,FisVerdimAldim,FisSonSatislar,FisNakitUyari}.razor`, `Bilesenler/Fis/FisFark.cs` (yalnız remarks), `Bilesenler/Form/{Sayi.razor,SayisalAlan.cs}`, `Pages/{Urunler,Vitrin,Islemler}.razor`, `wwwroot/js/girdi-maske.js`, `wwwroot/css/zo-islem.css`.
- Denetim: `dotnet/denetim.sh` (GRID1_TAVAN 13 → 8 + yorum).
- Testler: `GocC2Testleri.cs` (YENİ), `GocC2Ortak.cs` (YENİ), `OlcekYuzEnvanteri.txt` (+5 satır) ve § 2.4'teki dosyalar.
- **Dokunulan sayfalar (TEK DOKUNUŞ kaydı):** Urunler, Vitrin, Islemler (+ Fis/*, Form/Sayi). Hepsi GOC-C'de açılmıştı — gerekçe: bu paket GOC-C'nin tasarım hakemi bulgularının kapanış devridir (görev tanımı).

## 9. Açık kalanlar
1. **400 `message` biçimi** (DTO yolu dizi, servis yolu dizge) — sahibi: DIL-SUNUCU (t2, `Program.cs` ileti katmanı). Kanıt § 6.
2. **/vitrin ayar katlanırı açıkken 375'te 9,5 ekran** (≈25 kalem kartı); iki sütunlu kart K3 kırık 44 verdi, geri alındı. Öneri: ayar ızgarasında 375'te yalnız Kalem + Satış marjı + Düzenle (sütun önceliği) — Vitrin sahibinin kararı.
3. Açılış tavanı ekran/sunucu cümlesi farklı (§ 2.2) — iki sözlük tek cümlede birleşebilir (URUN-FORM-2 metni).
4. GOC-C § 9 / § 14.9 maddeleri (sayım/eşik iletisi, rezerv/konsinye adet birimi, 0 TL hediye ayrımı, sipariş gerekçe asgarisi, `Islemler.YasalParam.cs.tmp`) aynen; ham tablo istisnası maddesi KAPANDI.
5. `dotnet/denetim.sh.mutbak` dosyası ağaçta (bu paketin değil; mutasyon yedeklerim `/private/tmp/kuyumcu-serit/mut-yedek/GOC-C-2/`).
6. Tam süit tur başına bir kez kuralı: iki kez koşuldu (ilki bu paketin 21 kırmızısını gösterdi, düzeltme sonrası ikinci) — § 4.

## 10. DoD sözleşmesi
Yeni varlık/kayıt türü YOK. Izgara düzenleri GRID-1 sözleşmesiyle (`kullanici_tercihleri`: PATCH koru/null sil, DELETE "Varsayılana dön", denetim izi) — 4 yeni ızgara anahtarı (`grid:islemler:aldim|kalem-tuval|kalem-pro|son-satislar`, `grid:vitrin:vitrin-ayarlar`; ekranlar zaten beyaz listede). Satış fişi değişmez kayıt (storno gerekçesi `DegismezIpucu` aynen). Finansal kayıtlarda yeni silme yolu açılmadı; ödeme satırı yalnız istemci görünüm nesnesidir (gövdeye 0 ₺ satır gitmez).

## 11. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| VERI-1 bUnit + CDP rakam rakam; hiçbir senaryoda satır sessiz silinmez; SQL karma doğru | KARŞILANDI | GC2E01–04 + GOCC2 sayfa testi; Chrome BASİT+PRO 1280 karma.mjs; SQL `KART:4000 \| NAKIT:6000`, `NAKIT:2500 \| KART:7500`; aşım 400 + hareket yok |
| VERI-2 ADET'te "1,5" yazılamaz (tuş), GRAM 1,5 = 1.5, tavan, sınıf taraması | KARŞILANDI | GC2M01 (gerçek js), GC2G03/04/05/06; tablo § 1.2; canlı 400 ×5 + GRAM 1.500 |
| /urunler PRO 1280 K9=0 K12=0 (8 kombinasyon) | KARŞILANDI | § 6.1 (önce 6/1) |
| HAM_TABLO_ORTAK_GRID_DISI 35 → ≤ 30 | KARŞILANDI | 13 → **8** (5 tablo; tavan 8) |
| Mutasyon ≥ 4 kırmızı, cmp AYNI | KARŞILANDI | 10/10 |
| Canlı satış/takas/hurda/iade baştan sona | KARŞILANDI | § 6 |
| Gerçek Chrome 1280+375 · BASİT+PRO · açık+koyu | KARŞILANDI | 152 + 16 ölçüm, modGercek tam; 5 görüntü (bütçe aşımı § 2.6) |
| build 0/0, denetim TEMİZ (P-30 hariç), tam süit 0 yeni kırmızı | KARŞILANDI | 0 Hata / paket dosyalarında 0 uyarı; denetim yalnız P-30; tam2 11487/11487 |
| Ödeme paneli ölçeği, açıklama satırları, sayfa boyu | KARŞILANDI (vitrin açık hâli § 9.2) | § 1.1, 1.6, 1.3, 1.4 |
| 400 message biçimi | DEVREDİLDİ | § 1.7 (DIL-SUNUCU alanı) |

## 12. Kurucuya / mali müşavire
- Mevzuat hükmü eklenmedi/değiştirilmedi. Nakit sınırı notundaki sayı sunucunun bildirdiği haddir (istemciye gömülmedi; B-3 sayacı 4). Açılış stoğu tavanı (1.000.000) **kolon/iş sınırıdır**, mevzuat değildir.
- Canlıda takasın aynı gün kümülatif nakit sınırına takılması (459 VUK GT kapısı) beklenen davranıştır; değiştirilmedi.

## 13. TUR 2 — hasım doğrulayıcı bulgularının kapanışı (09.10.2026)

### 13.1 [KRİTİK] Tam sayı kutusunda tuşla "45.000" → "45" (1000 kat küçülme) — KAPANDI
- **Kök neden (doğrulandı):** tur 1'de `girdi-maske.js` beforeinput, tam sayı kutusunda (`data-tam="1"`) hem "," hem "." tuşunu engelleyip kutuyu "kesir kipi"ne sokuyordu; kipte kutunun sonuna yazılan rakamlar da engellendiği için "45.000" → "45". Yapıştırma yolu "45.000"ı korurken tuş yolu bozuyordu (iki yol farklı sonuç).
- **Düzeltme (tek satır + yorum, `girdi-maske.js:202`):** kesir kipine yalnız **virgül** sokar (`e.data.indexOf(',') >= 0`). "." tr-TR binlik ayırıcıdır: engellenmez, kip açılmaz, metin C# `FormAlani.SayiCoz`'a bırakılır → "45.000" = 45000, "1.000" = 1000; "1.500" **Belirsiz** → alan altı "Ondalık için virgül kullanın: 1,5 ya da 1500" + `aria-invalid=true`, kalem eklenmez; "1.5" → "Küsuratlı yazılamaz; tam sayı girin." + kalem eklenmez. Tuş ve yapıştırma yolu artık aynı sonucu verir. IBAN imleç düzeltmesine dokunulmadı.
- `Dogrulayici/GOCC2SinirTestleri.cs` **DG21'deki Skip kaldırıldı** → yeşil. `GC2M01` sözleşmesi güncellendi (adı da): "1.5" kutuda kalır (C# reddeder), "45.000"/"1.000" kalır, binlik yazana kesir ipucu **açılmaz**, "1.000,5" → "1.000" (virgül+kesir yine yazılmaz, ipucu açılır).
- Sınıf (§ 1.2'deki 22 tam sayı alanı: Adet ×9, GunSayisi ×4, Sayi TamSayi ×9) aynı tek dosyadan beslendiği için hepsinde düzeldi; gerçek Chrome'da `#islem-adet` (satış) ve `#u-acilis-stok` (açılış stoğu) ile ölçüldü (§ 13.4).

### 13.2 [ORTA] Binlikte yanlış gerekçe ("küsuratlı yazılamaz") — KAPANDI
13.1 ile kapandı: "." artık kesir ipucunu açmaz (Chrome 8 kombinasyonda "45.000", "1.000", "1.500", "1.5" için `kesirIpucuGorunur=false`). Geçersiz/belirsiz biçimde alan geçersiz sayılır (`aria-invalid=true`, alan altı ileti) ve Ekle **eski değeri eklemez** (Chrome: önce "2" sonra "1.500" yazıp Ekle → kalem yok, ileti alanın altında).

### 13.3 [DÜŞÜK] Ölü kural + yanıltıcı test adı — KAPANDI
- `FisFark.SifirSatirlariDusur` → `[Obsolete("GOC-C-2: ödeme ekranı bu kuralı çağırmaz …")]`; yeni çağrı derleyici uyarısı verir. Eski saf kural testleri (SATIS3Bilesen, Dogrulayici/SATIS3Sinir) dosya başında `#pragma warning disable CS0618` + gerekçe ile korunur (silinmedi, gevşetilmedi).
- `SATIS3EkranTestleri.T10_…_0_TL_nakit_satiri_DUSER` → `T10_…_0_TL_nakit_satiri_DUSMEZ_govdeye_GIRMEZ` (gövde aynı; ad sözleşmeye uyduruldu).
- [DÜŞÜK] GC2T03/GC2T04 CSS metni sınaması: hasım "kabul edilebilir, tarayıcı ölçümüyle okunur" dedi — değiştirilmedi; § 13.4 ölçümü aynı kuralların etkisini yeniden doğruladı.
- [DÜŞÜK] MigrationHazirlik paralel çakışması: bu şerit `KUYUMCU_TEST_DB=kuyumcu_test_t1` ile koştu; tam süitte görülmedi.

### 13.4 Gerçek başsız Chrome (vekil 5161, yeni test kiracısı, önbellek kapalı; betikler `GOC-C-2/tur2/{tus,ekle}.mjs`, `olc.mjs`, `karma.mjs`)
**Tuş tuş yazım** (`Input.dispatchKeyEvent`, gerçek beforeinput) — 8 kombinasyon (1280/375 × BASİT/PRO × açık/koyu) × 2 alan × 7 girdi = **112 deneme, 112 beklenen sonuç, 0 sapma**:

| Girdi | Kutu | aria-invalid | Kesir ipucu | Sonuç |
|---|---|---|---|---|
| 45.000 | 45.000 | false | kapalı | 45000 (Chrome: kalem 45000 × 5 g = 225.000,00 g) |
| 1.000 | 1.000 | false | kapalı | 1000 (PRO: kalem eklendi, gram 5.000,00) |
| 1,5 | 1 | false | **açık** | virgül + kesir hanesi yazılmaz, neden görünür |
| 1.500 | 1.500 | **true** | kapalı | "Ondalık için virgül kullanın: 1,5 ya da 1500"; kalem eklenmez; açılışta Kaydet kilitli |
| 1.5 | 1.5 | **true** | kapalı | "Küsuratlı yazılamaz; tam sayı girin."; kalem eklenmez; açılışta Kaydet kilitli |
| 1.000,5 | 1.000 | false | **açık** | binlik kalır, virgül+kesir yazılmaz |
| 12 | 12 | false | kapalı | 12 |

**Sıkışma ölçümü** (`sikisma-olc.js` + ekstra; `tur2/olcum/olcum.jsonl`, 76 ölçüm, **modGercek 76/76**). Hücre = K1+K3+K5+K7+K11+K12+K13+K10b+yatay · K9 · sayfa boyu:

| Görünüm | 1280 B-A | 1280 B-K | 1280 P-A | 1280 P-K | 375 B-A | 375 B-K | 375 P-A | 375 P-K |
|---|---|---|---|---|---|---|---|---|
| /urunler | 0·0·1,0 | 0·0·1,0 | 0·0·1,1 | 0·0·1,1 | 0·0·2,5 | 0·0·2,5 | 0·0·3,4 | 0·0·3,4 |
| /urunler yeni ürün | 0·6¹·1,0 | 0·6¹·1,0 | 0·6¹·1,1 | 0·6¹·1,1 | 0·6¹·2,5 | 0·6¹·2,5 | 0·6¹·3,4 | 0·6¹·3,4 |
| /vitrin | 0·0·1,0 | 0·0·1,0 | 0·0·1,1 | 0·0·1,1 | 0·0·1,4 | 0·0·1,4 | 0·0·1,6 | 0·0·1,6 |
| /vitrin ayar açık | 0·0·4,1 | 0·0·4,1 | 0·0·4,2 | 0·0·4,2 | 0·0·7,8 | 0·0·7,8 | 0·0·9,5 | 0·0·9,5 |
| /islemler kalem | 0·0·1,0 | 0·0·1,0 | 0·0·1,1 | 0·0·1,1 | 0·0·1,6 | 0·0·1,6 | 0·0·1,6 | 0·0·1,6 |
| /islemler karma | 0·0·1,3 | 0·0·1,3 | 0·0·1,8 | 0·0·1,8 | 0·0·2,0 | 0·0·2,0 | 0·0·2,5 | 0·0·2,5 |
| /islemler karma aşım | 0·0·1,4 | 0·0·1,4 | 0·0·1,9 | 0·0·1,9 | 0·0·2,2 | 0·0·2,2 | 0·0·2,6 | 0·0·2,6 |
| /islemler nakit notu | 0·0·1,0 | 0·0·1,0 | 0·0·1,1 | 0·0·1,1 | 0·0·1,7 | 0·0·1,7 | 0·0·1,8 | 0·0·1,8 |
| /islemler son satışlar | 0·0·1,0 | 0·0·1,0 | 0·0·1,6 | 0·0·1,6 | 0·0·1,6 | 0·0·1,6 | 0·0·2,4 | 0·0·2,4 |
| /islemler PRO ✎ | — | — | 0·0·1,2 | 0·0·1,2 | — | — | 0·0·1,9 | 0·0·1,9 |
**/urunler PRO 1280 açık+koyu K9 = 0, K12 = 0**; 8 kombinasyonda /urunler K9/K12 = 0. (¹) tur 1 ile aynı: yeni ürün penceresinin ALTINDA kalan sayfa yazısı (ölçerin örtü kontrolü yok; görüntüde örtüşme yok).
**VERI-1 yeniden (karma.mjs, BASİT 1280, son kod):** 2. satıra 9→99999 rakam rakam: her adımda 2 satır (Nakit 9.991 → 9.901 → 9.001 → 1 → 0; satır KALDI), alan altı "Bu tutar fiş toplamını 89.999 ₺ aşıyor…", Kaydet kilitli + sebep, Kalanı ata → Kart 10.000, Kaydet açık.
**Read ile görülen görüntü (tur 2: 1 / bütçe 4):** `tur2/tus-islem-adet-BASIT-aydinlik-1280.png` — Adet kutusunda tuşla yazılmış "45.000" görünür, kesir ipucu yok, düzen bozulmamış.

### 13.5 Canlı API (5160, kiracı `PAKETGOC-C-2-TEST …` = 01M4F7FEXB1YXV2EW5FJE5M303; `kurulum.py`, `canli2.py`, `canli3.py`; çıktılar `tur2/*.log`)
| Adım | Beklenen | Gerçek | İleti / SQL |
|---|---|---|---|
| Açılış 1,5 / 0,0001 / −1 / 99.999.999 / 1.000.001 (ADET) | 400 ×5 | 400 ×5 | Küsuratlı… / sıfırdan büyük ×2 / "en fazla 1.000.000" ×2 · `acilisStogu.adet` |
| SQL: 400'lerde ürün yok | 0 | 0 | urunler=0 |
| GRAM açılış 1,5 | 201 | 201 | `stoklar.adet = 1.500` |
| Karma aşım (10.000 + 99.999 / fiş 10.000) | 400 | 400 | "…bacaklar 109.999,00 TL, fiş 10.000,00 TL…" |
| Karma 0 ₺ satır gövdede | 400 | 400 | `odemeler[0].tutarTl` |
| SQL: 400'lerde hareket/kasa hareketi yok | 0 | 0 | kasa_h=0 |
| Karma Kart 6.000 + Nakit 4.000 (A) | 200 | 200 | `SATIS tl=10000.00` · `NAKIT:4000.00 \| KART:6000.00` |
| Satış tek ödeme (B) | 200 | 200 | `SATIS adet=1.000 tl=10000.00` |
| Karma Kart 7.500 + Nakit 2.500 (B) | 200 | 200 | `NAKIT:2500.00 \| KART:7500.00` |
| Karma aşım (B) + SQL | 400 · 0 | 400 · 0 | hareketler 11 = 11 |
| İade 1 adet | 200 | 200 | `SATIS_IADE adet=1.000 tl=-10000.00` |
| Takas (fark kartla) | 200 | 200 | `TAKAS eskiHas=2.748000 tl=11756.00` |
| Hurda alış 0,5 g K22 | 200 | 200 | `HURDA_ALIS has=0.458000 tl=-1374.00` |
Dürüstlük: canli2 ilk koşumunda B şubesiz olduğu için 4 adım "şube seçimi gerekli" 400 verdi (doğru davranış, tur 1'deki gibi) → SQL ile B'ye test kiracısının şubesi atandı, yeniden koşuldu; nakitli takas aynı gün kümülatif nakit sınırına takıldı (doğru, 459 VUK GT kapısı) → canli3 fark kartla.

### 13.6 Testler / mutasyon / denetim (tur 2)
- `dotnet build Kuyumcu.sln`: **0 Hata**, 6 Uyarı — hepsi başka şeritlerin test dosyalarında (KesifP2, SonKalanlar ×2, Yildiz2 ×2, SUBEKAPSAMASinir; tur 1 ile aynı); bu paketin dosyalarından 0.
- Filtreli: `GocC2|GOCC2|SATIS3` **570/570** (+1 önceden atlanan DOG_T24); `girdi-maske` okuyan diğer gruplar (CumaMMusteriler, GirdiBilesen, TasarimBelirtec, Girdi/GIRDI/Maske) **501/501**; DG10/DG11/DG20/DG21/GC2M01/T10 14/14.
- **Tam süit** (`trx-pGOC-C-2/tam3.trx`, son kod, tur başına bir kez): **Toplam 11554 · Geçti 11551 · Başarısız 1 · Atlanan 2.** Kırmızı: `FaturaEkranTestleri.Y4C_K12_ekranda_gelistirici_dili_YOK_Basit_ve_Pro(basit: False)` — "805" yasak alt dizgesi markup'taki **rastgele GUID**'de bulundu (`"3c8bc85b-8df0-49a7-a805-…"`); 3 filtreli yeniden koşumda 2/2 yeşil ×3 → **kararsız test**, bu paketin değil (Fatura ekranı; bu paket Fatura'ya dokunmadı). Bu paketin YENİ kırmızısı: **0**.
- **Mutasyon (tur 2, `tur2/mut.zsh`; yedek `/private/tmp/kuyumcu-serit/mut-yedek/GOC-C-2/tur2/`; js testleri kaynak dosyayı okur → derleme gerekmez):**

| Kod | Kural kırıldı | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M2-11 | "." yine kesir kipine sokar (tur 1 hatası) | GC2M01, DG21 | AYNI |
| M2-12 | "," tuş engeli kalktı | GC2M01 | AYNI |
| M2-13 | kesir kipinde rakam engeli kalktı ("1,5"→"15") | GC2M01, DG20 | AYNI |
| M2-14 | maske tam sayıda "." siler | GC2M01 | AYNI |
4/4 kırmızı, 4/4 cmp AYNI (tur 1'in M2-1…M2-10'u da AYNI; toplam 14/14). Mutasyonlardan sonra kaynak birebir; derleme gerektiren dosyaya mutasyon yapılmadı.
- **denetim.sh:** `DENETİM BAŞARISIZ` yalnız **P-30 DEMO_INSAN_SURESI** (✗ sayısı 1; görev "P-30 hariç"). GRID-1 HAM_TABLO_ORTAK_GRID_DISI **8** (tavan 8).

### 13.7 Temizlik (tur 2)
API (PID 79144 + alt süreç 79151) ve vekil (79855) durduruldu; `lsof` 5160/5161 boş; yalnız `GOC-C-2/` profilli Chrome süreçleri kapatıldı (0 kaldı). `zsh kiraci-temizle.sh 01M4F7FEXB1YXV2EW5FJE5M303 --uygula` → ikinci çağrı "Kiracı YOK (bağlantı sınandı ve ÇALIŞIYOR)"; davet satırı silindi (`olusturan_not='PAKET GOC-C-2 canli test'` = 0). **tenants: başlangıç 20 → son 20.** QA-TEST-* kiracılarına dokunulmadı.

### 13.8 Değişen dosyalar (tur 2)
`Kuyumcu.Web/wwwroot/js/girdi-maske.js` (virgül/nokta ayrımı + yorum) · `Kuyumcu.Web/Bilesenler/Fis/FisFark.cs` (`[Obsolete]`) · `Kuyumcu.Tests/GocC2Testleri.cs` (GC2M01 sahne + iddialar + ad) · `Kuyumcu.Tests/Dogrulayici/GOCC2SinirTestleri.cs` (DG21 Skip kaldırıldı) · `Kuyumcu.Tests/SATIS3EkranTestleri.cs` (T10 adı) · `Kuyumcu.Tests/SATIS3BilesenTestleri.cs` ve `Kuyumcu.Tests/Dogrulayici/SATIS3SinirTestleri.cs` (CS0618 pragma + gerekçe). Dokunulan sayfa yok (razor değişmedi).

### 13.9 Açık kalanlar (tur 2 eki)
1. **Kararsız test** `FaturaEkranTestleri.Y4C_K12_…(basit: False)`: "805" yasak alt dizgesi rastgele GUID'e çarpabiliyor — sahibi Fatura/Y4-C testinin sahibi; öneri: yasaklı "805"i kelime sınırıyla (`\b805\b`) ya da görünür metinde (`TextContent`) arama.
2. Tam sayı alanında "1.500" Belirsiz iletisi "1,5 ya da 1500" der; tam sayı alanında "1,5" seçeneği yoktur. İleti genel `SayiGirisiSozlugu.Belirsiz` — tam sayıya özgü varyant ("1500 yazın") öneri; hasımın tarifi "Belirsiz → alan altı hata" olduğu için davranış değiştirilmedi.
3. § 9'daki maddeler (400 `message` biçimi → DIL-SUNUCU t2; /vitrin ayar açık 375'te 9,5 ekran; açılış tavanı iki cümle) aynen.

### 13.10 Koşul karşılama (tur 2 sonu)
| Koşul | Durum | Kanıt |
|---|---|---|
| Hasım KRİTİK: tuşla "45.000" → 45000, "1.000" → 1000; "." binlik, kesir kipine yalnız "," | KARŞILANDI | girdi-maske.js:202; DG21 (Skip kalktı) + GC2M01 yeşil; Chrome 112/112; M2-11 |
| Hasım ORTA: binlikte "küsuratlı" denmez; geçersizde alan geçersiz | KARŞILANDI | Chrome: "." için ipucu kapalı ×64; "1.500"/"1.5" aria-invalid + ileti + kalem eklenmez |
| Hasım DÜŞÜK: ölü kural / test adı | KARŞILANDI | `[Obsolete]` + T10 yeniden adlandırıldı |
| VERI-1 / VERI-2 / KRİTİK ürün listesi / 5 ham tablo / panel ölçeği / açıklama / sayfa boyu | KARŞILANDI (tur 1 + tur 2 yeniden ölçüm) | § 11 + § 13.4 |
| Mutasyon ≥ 4, cmp AYNI | KARŞILANDI | tur 2 4/4 (toplam 14/14) |
| Canlı satış/takas/hurda/iade + SQL | KARŞILANDI | § 13.5 |
| build 0 hata, denetim TEMİZ (P-30 hariç), tam süit 0 yeni kırmızı | KARŞILANDI | 0 Hata; denetim yalnız P-30; tam3 1 kırmızı = başkasının kararsız testi (§ 13.6) |
| Test kiracısı silindi, tenants başa döndü | KARŞILANDI | 20 → 20 |
