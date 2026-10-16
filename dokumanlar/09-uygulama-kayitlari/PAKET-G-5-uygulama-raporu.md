# PAKET G-5 — Satış raporunda "Eski altın (mahsup)" satırı (B-4 devri) — UYGULAMA RAPORU

**Şerit:** SERİT D (etiket + vitrin + rapor adlandırma + G-5) · **Roller:** Taşkın (backend) + Ertuğ (frontend) · **Tarih:** 2026-09-24
**İzolasyon:** test veritabanı `kuyumcu_test_m`, canlı doğrulama portu **5185** (`kuyumcu_net`). Diğer portlara dokunulmadı.
**Kaynak:** PAKET-B-4 raporu § 0.2 ve § S-4 · KARAR-HAS-DEFTERI-2026-09-23 § G-5 (SATIS brüt yazılır; eski altın ayrı HURDA_ALIS satırıdır; ALTIN kalemi kasaya girmez).

---

## 1. İlk ölçüm (spesifikasyonla karşılaştırma)

| İddia | Ölçüm | Sonuç |
|---|---|---|
| Ödeme aracı kırılımı `SatisRaporlari.cs`'de | `SatisRaporServisi.OdemeAraci` → `GET /api/v1/raporlar/odeme-araci` (R12). Kırılım yalnızca **kasa_hareketleri** bacaklarından okunuyor. | DOĞRU. ALTIN bacağı kasaya yazılmadığı için kırılımda **hiç görünmüyordu**. |
| Kırılım ekranda nerede çiziliyor | `Pages/SatisAnalizi.razor` (sekme "odeme", `#sa-odeme-tablo`). Burada `@a.Arac` **ham kod** olarak basılıyor. `Raporlar.razor`'da ödeme kırılımı **yoktu**. | SAPMA (bkz. § 2). |
| ALTIN kaleminin şekli: `kasa_id` NULL, `hareket_id` → HURDA_ALIS | Canlı ölçüm (test kiracısı, `kuyumcu_net`): `ALTIN 9160.00 NULL 1` (kasa NULL, hareket HURDA_ALIS = 1). NAKIT kalemlerinde kasa dolu, HURDA_ALIS eşleşmesi 0. | DOĞRU. Spesifikasyonla çelişki yok. |
| SATIS brüt yazılıyor | SQL: `SATIS 20000.00 … KARMA 10840.00` ve `HURDA_ALIS -9160.00 +4.58 has` | DOĞRU. |

## 2. Sapmalar ve gerekçeleri

| # | Spesifikasyon | Yapılan | Gerekçe |
|---|---|---|---|
| S-1 | ALTIN satırı "ödeme aracı kırılımına" eklenir | Mevcut kasa tabanlı `araclar` dizisine **eklenmedi**. Aynı uca ayrı bir `satisKirilimi` bloğu eklendi. Kaynağı `odeme_kalemleri`, kapsamı yalnız SATIŞ fişleri. ALTIN satırı bu blokta doğuyor. | (1) `araclar` kasa defterinin giriş/çıkışıdır. Tahsilat, tediye ve masraf da içerir; toplamı satış brütüne hiçbir zaman eşit olmaz. Bu yüzden "kırılım toplamı = brüt satış" kabul koşulu ancak satışa özel bir kırılımla sağlanabilir. (2) `araclar`'ı okuyan tek ekran `SatisAnalizi.razor`. Bu dosya SERİT C'nin alanında ve ham `@a.Arac` basıyor. ALTIN oraya eklenseydi, düzeltme yetkim olmayan bir ekranda ham "ALTIN" kodu görünürdü. Bu, kabul koşulu "ham kod ekranda yok"u ihlal ederdi. |
| S-2 | Satır "Raporlar.razor profesyonel yüzde" görünür | Raporlar.razor profesyonel yüzüne yeni bir kart eklendi: `#sk-kart` "Satışlar neyle ödendi?". Kart üstteki YIL/AY seçicisini izliyor ve `/raporlar/odeme-araci?baslangic=<ayın 1'i>&bitis=<ayın son günü>` çağırıyor. | Raporlar.razor'da önceden bir kırılım yoktu, çizilecek yer açıldı. Basit yüz ("Bugün", S3-4) ve mevcut DOM id'leri değişmedi (bkz. § 6). |
| S-3 | Yeni istemci tipi | Tipler `Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs`'e yazıldı: `SatisKirilimi`, `SatisKirilimiSatiri`, `SatisOdemeKirilimiYaniti`. Sunucu bu tipleri **imzasında** döndürüyor: `SatisOdemeKirilimi.Olustur(...) → SatisKirilimi` ve `Zarf(...) → SatisOdemeKirilimiYaniti`. | İlk tam süitte `SozlesmeKapsamTestleri` cırcırı ve `SozlesmeYansimaTestleri` kırmızıya döndü (kilitsiz yeni tip; sunucuda aynı adlı `Satir` iç tipi). İkisi de düzeltildi. Kilitsiz tip sayısı 59'da kaldı (tavan 59). |
| S-4 | Sözlük | Yeni dosya `Kuyumcu.Core/I18n/Sozlukler.SatisKirilimi.cs` açıldı (TR+EN konumsal record; eksik anahtar derleme hatası verir). Nakit, Kart, Havale, Döviz ve Veresiye adları mevcut `Dil.T.Islem.Odeme*` anahtarlarından okunuyor (UX-S31: aynı kavram, tek ad). | `Sozlukler.cs` ortak hub; dokunulmadı. |

## 3. Yapılanlar

### Sunucu — `Kuyumcu.Api/Modules/Rapor/SatisRaporlari.cs`
- `SatisOdemeKirilimiHesapla` (private): Dönemin SATIŞ hareketleri tenant, tarih ve şube süzgecinden geçiyor, storno çifti dışlanıyor. Fiş anahtarı `fis_id` boşsa hareketin kendi id'si; bu, `OdemeKalemFabrikasi.Turet` ile aynı kural. Fiş başına brüt ile `odeme_kalemleri` (aktif, aynı fiş kümesi) **sunucuda gruplanıyor**, satırlar belleğe çekilmiyor.
- `SatisOdemeKirilimi.Olustur` (saf, tek kaynak):
  - araç başına tutar ve fiş sayısı hesaplanıyor. `kasayaGirer = OdemeAraci.KasayaGirer(arac)`; ALTIN ve VERESIYE için false.
  - Tutarı 0 olan araç satırı **çizilmiyor**.
  - Kalemi olmayan eski fiş `BILINMIYOR` satırında brüt tutarıyla beyan ediliyor (Anayasa md.1: sıfır sayılmaz, başka araca yazılmaz).
  - Döndürülen alanlar: `brutSatisTl`, `kirilimToplamTl`, `farkTl` (uydurulmaz), `kasayaGirenTl` (yalnız kasaya giren araçlar, **eski altın hariç**), `mahsupAltinTl`, `veresiyeTl`, `fisAdet`, `kalemsizFisAdet`, `kalemsizTl`.
- `OdemeAraci` yanıtına `satisKirilimi` alanı eklendi. `baslangic` ve `bitis` artık `Zarf`'tan okunuyor, değerleri aynı. Mevcut alanlar (`araclar`, `veresiye`, `bilinmeyen`, `storno`, `guvenilirlik`) **değişmedi**.
- Hareket ya da defter yazımına dokunulmadı (yalnız okuma). Yeni uç açılmadı. `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` + `[ModulYetkisi("rapor")]` + `[Authorize]` mevcut sınıftan geliyor.

### Ekran — `Kuyumcu.Web/Pages/Raporlar.razor` (profesyonel yüz)
- `#sk-kart` kartı:
  - Kaynak rozeti "Sunucu hesabı" (UX-S21).
  - Özet kartları (`#sk-ozet`): Satış toplamı (brüt), Kasaya giren ("Eski altın ve veresiye hariç"), Eski altın (mahsup). Eski altın kartı yalnız tutar > 0 ise çiziliyor.
  - Tablo `#sk-tablo`: araç adı sözlükten, tutar `Bicim.Para(…, "TRY")` ile, fiş sayısı ve "Kasaya girer / Kasaya girmez" bilgisi var. Alt satır `#sk-toplam` sunucunun `kirilimToplamTl` değerini gösteriyor.
  - `#sk-kayitsiz` notu ve `#sk-fark` uyarı bandı yalnız ilgili değer sıfırdan farklıysa görünüyor.
  - Boş dönemde `BosDurum` gösteriliyor. Hata olursa `HataBandi` çıkıyor ve boş durum basılmıyor (UX-S28).
- Ham kod eşlemesi `SatisAracAdi`: NAKIT, KART, HAVALE, DOVIZ ve VERESIYE `Dil.T.Islem.Odeme*` anahtarlarından; ALTIN "Eski altın (mahsup)", BILINMIYOR "Ödeme şekli kayıtsız", bilinmeyen kod "Diğer" olarak gösteriliyor. Ham kod hiçbir durumda basılmıyor.
- Yeni düğme yok (salt okunur rapor kartı). Satır içi `style` 0, sabit renk 0.

### Sözleşme ve sözlük
- `Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs`: +3 record.
- `Kuyumcu.Core/I18n/Sozlukler.SatisKirilimi.cs` (YENİ): 20 anahtar, TR + EN.

## 4. DoD sözleşmesi (düzenlenebilirlik)

| Varlık / kayıt | Düzenleme yolu | Gerekçe |
|---|---|---|
| Satış ödeme kırılımı (`satisKirilimi`) | **Salt okunur rapor**; yeni varlık ya da kayıt türü yok | Türetilmiş bir görünüm. Kaynak kayıtlar `hareketler`, `odeme_kalemleri` ve `gider_pusulalari`. Hepsi finansal; düzeltme yolları B-4 ve 48-A'da tanımlı (SATIS için satış iadesi; HURDA_ALIS storno motorunda ertelenen tür, bkz. B-4 § S-1). Bu paket veri doğurmuyor, dolayısıyla PATCH/DELETE/denetim kaydı gerekmiyor. |

Yeni tablo, kolon ya da uç yok. **Migration: gerekmedi.**

## 5. Testler

**Önce/sonra:** Tam süit toplamı **4874 → 4885** (+11 yeni test). Başka şeritler aynı anda test ekleyip çıkarıyor; +11'in tamamı bu paketin G5 testleri.

| Dosya | Test | Tür |
|---|---|---|
| `Kuyumcu.Tests/G5SatisKirilimiHttpTestleri.cs` | `G5_Kirilimda_eski_altin_mahsup_satiri_gorunur` | HTTP (gerçek boru hattı, satis-fisi → odeme-araci) |
| | `G5_Nakit_toplami_altin_bacagi_kadar_SISMEZ` | HTTP (kasa kırılımında da NAKIT 10.840, ALTIN yok) |
| | `G5_Altinsiz_gunde_satir_cizilmez` | HTTP |
| | `G5_Kirilim_toplami_brut_satisa_esittir` | HTTP + DB (brüt = DB'deki SATIS TL toplamı) |
| | `G5SatisKirilimiSafTestleri.G5_Kalemsiz_fis_bilinmiyor_satirinda_beyan_edilir_fark_sifir` | saf |
| | `G5SatisKirilimiSafTestleri.G5_Kalem_toplami_brutu_tutmazsa_fark_uydurulmaz` | saf |
| `Kuyumcu.Tests/G5SatisKirilimiEkranTestleri.cs` | `G5_Ekranda_ham_ALTIN_kodu_basilmaz` (TR + EN, sorgu tam ay) | bUnit |
| | `G5_Ekran_kasaya_giren_karti_eski_altini_katmaz` | bUnit |
| | `G5_Ekran_altinsiz_donemde_eski_altin_satiri_ve_karti_yok` | bUnit |
| | `G5_Ekran_satissiz_donemde_bos_durum` | bUnit |
| | `G5_Ekran_hata_bos_duruma_cevrilmez` | bUnit |

Değişen eski test dosyası (silme yok): `RaporlarEkranTestleri.cs` sahte API'sine `/raporlar/odeme-araci` için gerçekçi bir boş dönem yanıtı eklendi. Böylece `'{}'` varsayılanı kalmadı.

**Filtreli koşumlar (hepsi `KUYUMCU_TEST_DB=kuyumcu_test_m`):**
- G5 + Rapor* + Sozlesme* + B4AltinBacagi* (son temiz derlemeden sonra): **244/244**.
- G5 + RaporlarEkran + Sozlesme*: **125/125**.
- G5 tek başına: **11/11**.

**Tam süit (3 koşum):**
- Koşum 1: 4885 → 4880 geçti, 5 kırmızı. Bunlardan **3'ü bu paketin**: `SozlesmeKapsamTestleri` ×2 ve `SozlesmeYansimaTestleri` ×1. Düzeltme S-3'te; düzeltmeden sonra filtreli koşumda hepsi yeşil. Kalan 2 kırmızı `YedekEkranTestleri` (başka şerit). O sırada `Yedekler.razor` 04:51'de başka biri tarafından düzenleniyordu; tek başına tekrar koşulunca **18/18 yeşil** çıktı.
- Koşum 2: 4885 → 4879 geçti, 6 kırmızı: `UX4dSafTestleri.M17`, `EmanetEkranTestleri`, `S35EmanetEkranTestleri.S35_14`, `P20EkranTestleri`, `UX1C3EkranTestleri.C3_41`, `UX4cEkranTestleri.M10`. Hepsi sayı biçimi farkı (ör. `Expected "1.000 g" Actual "1,000 g"`, `"12.5000 has"` bulunamadı). Neden `Kuyumcu.Web/Servisler/Bicim.cs` dosyasının 04:56'da başka bir şerit tarafından değiştirilmesi. Bu dosya ortak bileşen, bu paket ona **dokunmadı**. **Bu paketin ürünü değil.**
- **Koşum 3 (son, trx: `scratchpad/trx-pG-5c`): 4890/4890 GEÇTİ, 0 başarısız, çıkış kodu 0.** Bicim ve Yedek kırmızıları sahiplerince kapatılmış; bu paketin kırmızısı yok.

## 6. Mutasyon kanıtları

Yöntem: yedek `scratchpad/mut-yedek/G-5/` → `perl -0777 -pi` ile tek eşleşmeli değişiklik (her birinde eşleşme = 1) → `dotnet test --filter G5SatisKirilimi` → yedeği geri kopyala → `cmp` birebir. İki tur koşuldu; ikincisi son kodla (Contracts tipli `Olustur`).

| Kod | Değişiklik | Kırmızıya dönen testler (tur 2) | Geri alındı |
|---|---|---|---|
| M-G5-1 | ALTIN kalemi kırılımdan çıkarıldı (`.Where(… && s.Arac != Altin)`) | **G5_Kirilimda_eski_altin_mahsup_satiri_gorunur**, G5_Kirilim_toplami_brut_satisa_esittir, G5_Kalemsiz_fis… | cmp BİREBİR |
| M-G5-2 | ALTIN tutarı NAKIT satırına eklendi (`GroupBy(Arac==ALTIN ? NAKIT : Arac)`) | **G5_Nakit_toplami_altin_bacagi_kadar_SISMEZ**, G5_Kirilimda…, G5_Kirilim_toplami…, G5_Kalemsiz_fis… | cmp BİREBİR |
| M-G5-3 | Sözlük yerine ham kod basıldı (`"ALTIN" => arac`) | **G5_Ekranda_ham_ALTIN_kodu_basilmaz** | cmp BİREBİR |
| M-G5-4 | "Kasaya giren" bütün araçları topluyor (`Where(s => true)`) | G5_Nakit_toplami…, G5_Kirilim_toplami…, G5_Kalemsiz_fis… | cmp BİREBİR |
| M-G5-5 | Eski altın kartı her zaman çiziliyor (`@if (true)`) | **G5_Ekran_altinsiz_donemde_eski_altin_satiri_ve_karti_yok** (tekrar koşumu, tek kırmızı) | cmp BİREBİR |
| M-G5-6 | Brüt yerine net (brüt − altın) | **G5_Kirilim_toplami_brut_satisa_esittir**, G5_Kalemsiz_fis… | cmp BİREBİR |
| M-G5-7 | Kalemsiz fiş satırı atlanıyor (`if (false)`) | **G5_Kalemsiz_fis_bilinmiyor_satirinda_beyan_edilir_fark_sifir** | cmp BİREBİR |

**Dürüstlük notu:** Tur 2'nin M-G5-5 koşumunda, beklenen ekran testine ek olarak 3 sunucu testi de kırmızı döndü (`kasayaGirenTl` 20000 geldi). Bunun nedeni paylaşılan `bin/`'in o an M-G5-4 mutantının Api derlemesini taşımasıydı; paralel şeritlerin derleme kilidi yüzünden kopyalama gecikti. M-G5-5 tek başına tekrar koşuldu ve yalnız hedef test kırmızı döndü (`scratchpad/g5-mut-sonuc-m5.txt`). Ardından yapılan temiz derlemede ağaç yedekle `cmp` birebir aynıydı ve mutasyon izi 0 çıktı (`s => true` / `@if (true)` / `"ALTIN" => arac` grep = 0). Bu derlemeden sonra 244/244 yeşil.

## 7. denetim.sh

`./denetim.sh` → **rc=0, "DENETİM TEMİZ."** Bu paketi ilgilendiren sayaçlar:
- `P-03: SOZLESME_KILITSIZ_TIP 59/97 kilitsiz (tavan 59)`: yeni tip kilitli, artış yok.
- `Pages: @onclick'siz ve disabled'sız <button> 59 (tavan 71)`: yeni düğme yok.
- `Pages: ModalPencere kullanımı 48 (tavan 55)`: değişmedi.
- `P-18: SABIT_RENK 0`.

**DOM id (Raporlar.razor):** kaldırılan **0**; eklenen 7 (`sk-fark`, `sk-kart`, `sk-kayitsiz`, `sk-kaynak`, `sk-ozet`, `sk-tablo`, `sk-toplam`) ve dinamik `sk-satir-<arac>`.

## 8. Canlı doğrulama (5185, `kuyumcu_net`)

Sürücü: `scratchpad/pG-5/canli.sh` + `canli.py` (B-4 deseni). Davet satırı doğrudan eklendi, parola rastgele üretildi, hiçbir sır ekrana basılmadı. Test kiracısı `PAKETG5-TEST-CANLI`. Yanıt gövdeleri ayrıştırılarak okundu.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | POST /auth/kayit | 201 | 201 | jeton var |
| 2 | GET /kur/guncel | 200 + id | 200 | kur var |
| 3 | GET /islemler/altin-bacagi/ayarlar | 200 | 200 | iskonto null |
| 4 | POST /cari + /urunler (+ /kasalar) | 201/201 | 201/201 (+200) | — |
| 5 | Stok 10 | 200 | 200 | — |
| 6 | GET /raporlar/odeme-araci (satış yok) | 200, satırlar=[] | 200 | satır 0, brüt 0 |
| 7 | Fiş 20.000 = NAKIT 10.840 + ALTIN 9.160 | 200 | 200 | net 9160, nakitToplam 10840 |
| 8 | Kırılımda ALTIN satırı | ALTIN 9160, kasayaGirer=false | 200 | ALTIN=9160.0, kasayaGirer=False, fiş=1, mahsup=9160.0 |
| 9 | Nakit ŞİŞMEZ | NAKIT 10840, kasaya giren 10840, kasa NAKIT 10840 | 200 | NAKIT=10840.0, kasayaGiren=10840.0, kasa NAKIT=[10840.0], kasa ALTIN=[] |
| 10 | Kırılım toplamı = brüt | 20000 = 20000, fark 0 | 200 | brüt 20000.0, toplam 20000.0, fark 0.0, kalemsiz 0 |
| 11 | Altınsız fiş NAKIT 15.000 | 200 | 200 | — |
| 12 | İki fiş sonrası | brüt 35000 = toplam, kasaya giren 25840 | 200 | brüt 35000, toplam 35000, fark 0, kasayaGiren 25840, satırlar [(NAKIT 25840, 2 fiş), (ALTIN 9160, 1 fiş)] |
| 13 | Altınsız dönem (Ocak 2026) | satırlar=[] | 200 | satırlar=[], mahsup 0 |
| 14 | Anonim GET /raporlar/odeme-araci | 401 | 401 | — |

SQL kanıtı (test kiracısı):
```
-- hareketler (tur, tl, has, maden, odeme_turu, nakit, veresiye, kur_var, fis_id_bos, adet)
SAYIM_DUZELTME     0.00  91.600000 ALTIN NULL   0.00     0.00 1 1 NULL
HURDA_ALIS     -9160.00   4.580000 ALTIN NULL   0.00     0.00 1 1 NULL
SATIS          20000.00  -9.160000 ALTIN KARMA 10840.00  0.00 1 0 1.000
SATIS          15000.00  -9.160000 ALTIN KARMA 15000.00  0.00 1 0 1.000
-- odeme_kalemleri (arac, tutar, kasa, hareket=HURDA_ALIS)
NAKIT 10840.00 <kasa> 0
ALTIN  9160.00 NULL   1
NAKIT 15000.00 <kasa> 0
-- kasa_hareketleri: NAKIT 10840.00 · NAKIT 15000.00   (eski altın kasaya girmedi)
-- G-3: hareketler.odeme_turu='ALTIN' (tüm vt) = 0
```
Brüt satış (SATIS TL toplamı 35.000) = rapor `brutSatisTl` 35.000 ✓.

**Kapanış:**
- API (PID 5013) `kill` ile durduruldu; `lsof -iTCP:5185 -sTCP:LISTEN` boş.
- Test kiracısının satırları, `information_schema`'da `tenant_id` kolonu olan bütün tablolardan silindi (kalan satır 0). `tenants` satırı ve `kiraci_davetleri` satırı da silindi.
- **tenants: önce 10 → sonra 10.** `QA-TEST-` kiracılarına dokunulmadı.

## 9. Dokunulan sayfalar (DoD EK 2 — TEK DOKUNUŞ)

`Pages/Raporlar.razor`. Gerekçe: G-5 kabul koşulu satırın bu ekranın profesyonel yüzünde görünmesini istiyor. S3-4'ün "Bugün" basit yüzü ve `S34RaporBugunTestleri` bozulmadı; filtreli koşumlarda yeşil.

## 10. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| ALTIN bacaklı fiş sonrası kırılımda "Eski altın (mahsup)" satırı tutarıyla görünür | KARŞILANDI | G5_Kirilimda…, G5_Ekranda…, canlı 8 |
| Nakit/kart/havale toplamı ALTIN kadar şişmez | KARŞILANDI | G5_Nakit…, G5_Ekran_kasaya_giren…, canlı 9/12 |
| ALTIN bacaksız günde satır çizilmez | KARŞILANDI | G5_Altinsiz…, G5_Ekran_altinsiz…, canlı 13 |
| Kırılım toplamı = brüt satış (fark 0) | KARŞILANDI | G5_Kirilim_toplami…, canlı 10/12 + SQL |
| Ham "ALTIN" kodu ekranda yok | KARŞILANDI | G5_Ekranda_ham_ALTIN_kodu_basilmaz (TR + EN), M-G5-3 |
| Sunucuda hesaplanır, istemci toplam uydurmaz (UX-S21) | KARŞILANDI | Kartlar sunucu alanlarını basıyor; M-G5-4 sunucu tarafında yakalandı |
| Hareket/defter yazımına dokunulmaz | KARŞILANDI | Yalnız okuma; HareketFabrikasi'ya dokunulmadı |
| Dosya sınırı | KARŞILANDI | Yalnız SatisRaporlari.cs, Raporlar.razor, RaporlarSozlesmeleri.cs, yeni Sozlukler.SatisKirilimi.cs ve test dosyaları |
| Mevzuat | YENİ YORUM YOK | KARAR-HAS-DEFTERI (KDVK 2/5 trampa = iki teslim, SATIS brüt) olduğu gibi uygulandı; yeni hüküm yazılmadı |

## 11. Değişen dosyalar

```
M  dotnet/Kuyumcu.Api/Modules/Rapor/SatisRaporlari.cs          (?? olarak listelenir: dosya HEAD'de yok, başka paketten)
M  dotnet/Kuyumcu.Web/Pages/Raporlar.razor
M  dotnet/Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs
A  dotnet/Kuyumcu.Core/I18n/Sozlukler.SatisKirilimi.cs
A  dotnet/Kuyumcu.Tests/G5SatisKirilimiHttpTestleri.cs
A  dotnet/Kuyumcu.Tests/G5SatisKirilimiEkranTestleri.cs
M  dotnet/Kuyumcu.Tests/RaporlarEkranTestleri.cs               (sahte API'ye odeme-araci yanıtı)
A  dokumanlar/09-uygulama-kayitlari/PAKET-G-5-uygulama-raporu.md
```
Commit yapılmadı. `RaporModulu.cs` bu pakette **değiştirilmedi** (git'teki M işareti önceki paketlerden).

## 12. Açık kalanlar (sahipleriyle)

1. **SERİT C (SatisAnalizi.razor):** Ekran kasa kırılımında `@a.Arac`'ı **ham kod** olarak basıyor (NAKIT, KART… — UX-S31 ihlali; bu paketten önce de vardı). İsterse artık `satisKirilimi` bloğunu da gösterebilir; tipler `Kuyumcu.Contracts.Sayfalar.Raporlar`'da. Bu paket dosyaya dokunmadı.
2. **`Bicim.cs` sahibi (tam süit koşum 2; koşum 3te KAPANMIŞ):** 04:56'daki değişiklik 6 testi kırdı (sayı ayracı `1.000 g` → `1,000 g`): UX4dSaf.M17, EmanetEkran, S35_14, P20Ekran, UX1C3.C3_41, UX4c.M10. Bu paketin ürünü değil.
3. **`YedekEkranTestleri` ×2 (koşum 1; koşum 3te KAPANMIŞ):** `Yedekler.razor` yazılırken kırmızıydı, tek başına koşulunca yeşil. Sahibi Yedekler şeridi.
4. **`EtiketEkranTestleri.cs:145` xUnit1031 uyarısı:** aynı şeritteki etiket paketinin dosyası; bu paket yazmadı.
5. **Ortam:** Paralel şeritler paylaşılan `bin/obj`'yi temizledi ya da kilitledi. Birkaç derleme ve test denemesi MSB3026/MSB3030/CS0016 ve eksik DLL (`NUlid`, `Microsoft.IdentityModel.Protocols`) hatasıyla düştü. Kodla ilgisi yok; yeniden denemede yeşil. Kayıtlar `scratchpad/g5-b3-kilit.log`, `g5-b6-kilit.log`, `g5-t6..t8.log`.

## 13. Kurucuya / mali müşavire not

Paket yalnız raporlama yapıyor. "Eski altın (mahsup)" satırı, KARAR-HAS-DEFTERI'nin zaten belgelediği modeli ekranda **görünür** kılıyor: KDVK md. 2/5'e göre trampa iki ayrı teslimdir, SATIS brüt yazılır, eski altın gider pusulalı HURDA_ALIS'tir. Yeni bir mevzuat yorumu yapılmadı, eşik ya da oran eklenmedi. "Kasaya giren" kartının eski altını ve veresiyeyi dışlaması bir mevzuat hükmü değil, kasa gerçeğinin yansımasıdır (kasa_hareketleri ile birebir, canlı 9).
