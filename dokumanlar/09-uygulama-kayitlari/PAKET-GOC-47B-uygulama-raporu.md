# PAKET GOC-47B — Veri göçü Faz B: açılış bakiyesi + açılış stoku (DEVIR partisi) · UYGULAMA RAPORU

> Tarih: 2026-10-01 · Şerit t2 (API 5162 · vekil 5163 · CDP 9905/9906 · test DB kuyumcu_test_t2)
> Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Model: Opus · COMMIT YOK
> Durum: **BİTTİ — TUR 3** (orkestratör 14:20 bulgusu SAYI_BELIRSIZ kapatıldı; § T3). Tur 3 tam süit: **8838 · 8836 geçti · 0 başarısız · 2 atlandı**. § 0 tur 2, § 1–13 tur 1 kaydıdır.

## T3. TUR 3 — orkestratör bulgusu: belirsiz sayı (binlik ayracına benzeyen nokta) (2026-10-01)

**İlke (orkestratör 14:20):** belirsiz sayı ASLA sessizce yorumlanmaz. Eski davranış: `'45.000'` Invariant okumayla **45** (kırk beş bin yazan kullanıcıya 1000 kat eksik bakiye), `'1.500'` has → 1,5.

| Ne | Düzeltme |
|---|---|
| Kalıp | `CsvOkuyucu.BinlikBelirsizMi`: `^[+-]?[1-9]\d{0,2}(\.\d{3})+$` (CultureInvariant, 100 ms zaman aşımı). '0.916' (0 ile başlar), '45.5', '1.25', '13.9232', '45000', '1500.50' kalıba UYMAZ → geçerli. Baştaki işaret de kapsanır ('-1.500' belirsiz) — eksi bakiye aynı riski taşır. |
| Kod / mesaj | `SAYI_BELIRSIZ` (`CsvOkuyucu.SayiBelirsizKodu`, `DefterHataKodu.SayiBelirsiz`); mesaj: `"<kolon>: '<hücre>' belirsiz. Binlik ayracı kullanmayın; 45000 ya da 45.5 yazın."` |
| Faz B (defter) | `DefterOrtak.SayiOku` — okuma ÖNCESİ kalıp; kapsadığı kolonlar: tlBakiye, hasGramBakiye, usd/eur/gbpBakiye, adet, birimMaliyetTl (tüm Faz B sayı hücreleri bu tek yoldan geçer). |
| Faz A (kart) — “gram” kolonu buradadır | `CariKaynagi.BelirsizSayi(...)` yardımcı: cari kartı krediLimitiTl, varsayilanVadeGun; ürün kartı gramAgirlik, tasAgirlik, iscilikTl, satisFiyati. (Faz A'nın virgül davranışı — § 0.7 açık kalan — bu turda DEĞİŞTİRİLMEDİ; yalnız belirsiz-nokta kapısı eklendi.) |
| Ekran | “Dosyayı nasıl hazırlarım?” katlanırında TÜM kaynaklar için `#ia-binlik-notu` (örnekli: 45.000 / 1.500 belirsiz; 45000, 45.5 yazın; 0.916, 13.9232 geçerli). Hata tablosunda kod sözlükten: “Sayı belirsiz” / “Ambiguous number” (`Goc47BSozlugu.HkSayiBelirsiz`, TR+EN; `BinlikNotu` TR+EN). Yeni CSS yok, satır içi stil yok. |

### T3.1 Hasım test dosyasında sözleşme güncellemesi (gerekçeli)
Doğrulayıcının `Dogrulayici/GOC47BSinirTestleri.cs` dosyasında 4 test ESKİ sözleşmeyi (45.000 = 45; kod SAYI_GECERSIZ) kodluyordu ve orkestratör kararıyla doğrudan çelişiyordu (ilk filtreli koşum: 4 kırmızı, `r3-t1.log`). Yalnız beklenen değerler yeni karara çekildi, her değişiklik yorumla işaretlendi (“GOC-47B tur 3”):
- `Kurus_alti_TL_yuvarlanmaz_TUTAR_ONDALIK`: '100.105' → SAYI_BELIRSIZ; kuruş-altı kuralı kalıba girmeyen '1000.105' ve '100.1051' ile TUTAR_ONDALIK olarak korunur (kapsam DARALMADI).
- `Nokta_sozlesmesi_45_000_kirk_bes_okunur_1_500_bir_bucuk` → **yeniden adlandırıldı** `Nokta_sozlesmesi_45_000_ve_1_500_BELIRSIZ_45_5_kirk_bes_bucuk_okunur` (45.000/1.500 → SAYI_BELIRSIZ; 45.5 ve 1.5 okunur).
- `GOC47BSinirTestleriTur2.Dg2_TL_45_000_…` ve `Dg2_Turk_bicimi_…` içinde '45.000' / '1.500.000' beklenen kodu SAYI_BELIRSIZ ('1.500,50' ve '45 000' SAYI_GECERSIZ kaldı).
→ **Hasımdan bu dört değişikliği yeniden onaylaması istenir.**

### T3.2 Derleme · testler
- `dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra, temiz): **0 Hata · 0 Uyarı** (`r3-b-temiz.log`). (İlk derlemede görülen 2 uyarı başka şeritlerin dosyalarındaydı — `SUBEKAPSAMASinirTestleri.cs`, `KesifP2Testleri.cs` — temiz derlemede 0.)
- Yeni testler (+3 metot / +12 vaka): `Goc47BSayiTestleri.Binlik_benzeri_noktali_sayi_her_kolonda_SAYI_BELIRSIZ` (Theory ×4: 45.000 · 1.500 · 2.500.000 · -1.500; her biri TL, has, USD, adet, maliyet, kredi limiti TL, gram, taş, işçilik, fiyat kolonunda; mesaj metni birebir) · `Belirsiz_olmayan_nokta_ondalik_GECERLI` (Theory ×6: 45.5 · 0.916 · 13.9232 · 45000 · 1.25 · 1500.50; has değeri birebir okunur, hiçbir kolonda SAYI_BELIRSIZ/SAYI_GECERSIZ yok) · DB: `Goc47BTestleri.Binlik_benzeri_45_000_PROVADA_SAYI_BELIRSIZ_deftere_yazilmaz` (3 satır: 45.000 TL, 1.500 has → 2 SAYI_BELIRSIZ; 45000 / 0.916 → geçerli; yazilanKayit 0, bakiye 0) · bUnit: `Sutun_eslestirme_…` artık `#ia-binlik-notu` metnini de doğrular.
- Filtreli (`Goc47B|GOC47BSinir|IceAktarim|CsvOkuyucu|AcilisBakiye`): **127/127** (`r3-t2.log`).
- **Tam süit** (tur başına bir kez, son kodla): **8838 toplam · 8836 geçti · 0 başarısız · 2 atlandı** — trx `/private/tmp/kuyumcu-serit/trx-pGOC-47B-r3/` (Counters failed="0"). → **Yeni kırmızı 0.**

### T3.3 Mutasyon (hepsi geri alındı, `cmp` AYNI; günlükler `GOC-47B/mut-M47B-18..22.log`)
| Kod | Değişiklik | Kırmızı test(ler) |
|---|---|---|
| M47B-18 | Kalıp kapatıldı: `(\.\d{3})+$` → `(\.\d{3}){9}$` | `Goc47BSayiTestleri.Binlik_benzeri_…` ×4 · `Goc47BTestleri.Binlik_benzeri_45_000_PROVADA_…` · `GOC47BSinirTestleriTur2.Dg2_TL_45_000_…`, `Dg2_TL_1_500_…`, `Dg2_USD_2_500_…`, `Dg2_Turk_bicimi_…` · `GOC47BSinirTestleri.Kurus_alti_…`, `Nokta_sozlesmesi_…` (11 kırmızı) |
| M47B-19 | Ürün kartında `gramAgirlik` belirsiz kapısından çıkarıldı | `Binlik_benzeri_noktali_sayi_her_kolonda_SAYI_BELIRSIZ` ×4 |
| M47B-20 | Cari kartında `krediLimitiTl` belirsiz kapısından çıkarıldı | `Binlik_benzeri_noktali_sayi_her_kolonda_SAYI_BELIRSIZ` ×4 |
| M47B-21 | Kalıp `[1-9]` → `\d` (0.916 belirsiz sayılır) | `Belirsiz_olmayan_nokta_ondalik_GECERLI(0.916)` · `Binlik_benzeri_45_000_PROVADA_…` · `Dg2_adet_0_001_…` · `Kurus_alti_…` |
| M47B-22 | Ekrandan `#ia-binlik-notu` silindi | `Goc47BEkranTestleri.Sutun_eslestirme_zorunlu_eksikse_PROVA_kilitli_sebep_yazar_secince_govdeye_gider` |

### T3.4 denetim.sh (tur 3, `r3-denetim.log`)
Bu paketin sayaçları ✓: Iceaktarim/ dogrudan Add 0 · İçe aktarım → mevcut servis 2 · Kurulum/ dogrudan Add 0 · Açılış → cari yolu 1 · Sınırsız ToListAsync 145 (tavan 145) · CARI_BAKIYE_DOGRUDAN_YAZIM 0. İki ✗: **P-30 DEMO_INSAN_SURESI** (görevde hariç) ve **CARI_BAKIYE_AYNA_UYUSMAZ YENİ 1** → cari `01M3VJECXKEKV3Y6QNAADBVPT1`, kiracı **“PAKETTOPLU-1-TEST Tasarım Hakemi 4 …”** (TOPLU-1 şeridi, 11:08'de açılmış; hareketleri SATIS ×3 + TAHSILAT ×1 — açılış/içe aktarım hareketi YOK). Bu paketin ürünü değil; sahibine (TOPLU-1) yazıldı.

### T3.5 Canlı doğrulama (localhost:5162, kiracı `PAKETGOC-47B-TEST R3 Kuyumcu`, son kodla)
| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| C3-1 | Açılış bakiyesi **prova**: 45.000 TL · 1.500 has · 2.500 USD · 2.500.000 TL · (45000 TL + 0.916 has + 45.5 USD) | 200; 4 SAYI_BELIRSIZ, 1 geçerli; yazma 0 | **200** · toplam 5 · geçerli 1 · hatalı 4 · `yazilanKayit 0` · satır 2–5 `SAYI_BELIRSIZ` (“tlBakiye: '45.000' belirsiz. Binlik ayracı kullanmayın; 45000 ya da 45.5 yazın.” vb.) · özet TL 45000 · has 0,916 · USD 45,5 |
| C3-2 | Açılış stoku **prova**: adet 1.500 · maliyet 2.500 | 200; 2 SAYI_BELIRSIZ | **200** · hatalı 2 (`adet: '1.500' belirsiz…`, `birimMaliyetTl: '2.500' belirsiz…`) · yazilanKayit 0 |
| C3-3 | Ürün kartı (Faz A) **prova**: gramAgirlik 1.500 · 13.9232 | 200; 1 SAYI_BELIRSIZ, 1 geçerli | **200** · geçerli 1 · hatalı 1 (`gramAgirlik: '1.500' belirsiz…`) · yazilanKayit 0 |
| C3-4 | SQL (yazma 0) | 0 | `hareketler` **0** · `stok_partileri` **0** · `acilis_kalemleri` **0** · `urunler` **0** · sıfır-dışı `cari_bakiyeler` **0**; `ice_aktarim_satirlari`: acilis-bakiye PROVA GECERLI 1 / SAYI_BELIRSIZ 4 · acilis-stok PROVA SAYI_BELIRSIZ 2 · urun PROVA GECERLI 1 / SAYI_BELIRSIZ 1 (uygulanan_satir 0) |
(İlk iki prova 400 “Henüz kur kaydı yok” döndü — yeni kiracı; `POST /kur/guncelle` 200 sonrası tekrarlandı. Ekran ölçümünün ilk koşumu kullanıcı başına içe aktarım oran sınırına (5/sa) takıldı — `durum3/kosum-429.log`; ikinci SAHİP kullanıcıyla (API ile MUDUR açıldı, yalnız bu test kiracısında SQL ile SAHİP) yeniden koşuldu.)

**Temizlik:** API 5162 / vekil 5163 LISTEN **0**, Chrome 9907 süreç **0**. `zsh kiraci-temizle.sh <id> --uygula` → ikinci çağrı “Kiracı YOK”; `tenant_id` kolonlu **75 tabloda kalan satır 0**; davet satırı silindi (1). `tenants`: başlangıç **13**, son **13**; `PAKETGOC%` 0; QA-TEST-* 4 (dokunulmadı).

### T3.6 Gerçek tarayıcı ölçümü (başsız Chrome, CDP 9907, `sikisma-olc.js`; `durum3/olcum.jsonl`, `kosum.log`)
| Durum | Genişlik | BASİT açık | BASİT koyu | PRO açık | PRO koyu |
|---|---|---|---|---|---|
| S0 “Dosyayı nasıl hazırlarım?” açık (`#ia-binlik-notu` görünür) | 1280 / 375 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |
| S2 belirsiz dosya (sütun eşleştirmeli) kuru prova → hata tablosu “Sayı belirsiz” ×2 | 1280 / 375 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |

“0” = K1 gizli-görünen · K3 taşan/kırık · K5 TRY · K9 binen · K11 · yatay kaydırma hepsi 0. 375'te hata tablosunun SEBEP sütunu kendi kabında yatay kayar (sayfa kaymaz; tur 2 ile aynı). Görüntüler Read ile görüldü: `/private/tmp/kuyumcu-serit/GOC-47B/durum3/foto/veri_ice_aktarim__S2-belirsiz-prova__PRO_karanlik_375.png`, `…__S0-nasil-katlanir__BASIT_aydinlik_1280.png` (+14 görüntü aynı dizinde).

### T3.7 Tur 3 değişen dosyalar
- DEĞİŞTİ `dotnet/Kuyumcu.Api/Modules/Iceaktarim/CsvOkuyucu.cs` (BinlikBelirsizMi, SayiBelirsizKodu, SayiBelirsizMesaji) · `DefterKaynaklari.cs` (DefterHataKodu.SayiBelirsiz, DefterOrtak.SayiOku ön kapı) · `Kaynaklar.cs` (CariKaynagi.BelirsizSayi + cari/ürün Dogrula çağrıları)
- DEĞİŞTİ `dotnet/Kuyumcu.Core/I18n/Sozlukler.Goc47B.cs` (BinlikNotu, HkSayiBelirsiz TR+EN; HataKoduAdi eşlemesi) · `dotnet/Kuyumcu.Web/Pages/VeriIceAktarim.razor` (`#ia-binlik-notu`)
- DEĞİŞTİ `dotnet/Kuyumcu.Tests/Goc47BTestleri.cs` · `dotnet/Kuyumcu.Tests/Dogrulayici/GOC47BSinirTestleri.cs` (T3.1)
- Dokunulan sayfalar (MÜHÜR 2): yalnız `VeriIceAktarim.razor` (bu şeridin sayfası; gerekçe: orkestratör bulgusu, aynı paket turu 3).
- Migration: **gerekmedi** (şema değişmedi).

### T3.8 Tur 3 açık kalanlar
- **[TOPLU-1 sahibine]** CARI_BAKIYE_AYNA_UYUSMAZ YENİ 1 — TOPLU-1 test kiracısındaki cari (T3.4).
- **[Hasım]** T3.1'deki dört test beklentisinin yeni sözleşmeye çekilmesinin onayı.
- § 0.7'deki iki kalem (yaşlandırma kovası → Rapor sahibi; Faz A virgül okuması) aynen açık. Not: Faz A'da artık belirsiz-nokta reddediliyor; virgül ('1,5' → 15) hâlâ NumberStyles.Number ile okunuyor.

## 0. TUR 2 — hasım doğrulayıcı bulgularının kapatılması (2026-10-01)

| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| **[KRİTİK]** CSV sayı okuyucu virgülü binlik sayıyor ('1,5' → 15, '1500,50' → 150050) | `CsvOkuyucu.KatiSayiOku` (yeni): `NumberStyles.AllowLeadingSign \| AllowDecimalPoint`, binlik YOK. Faz B'nin tüm sayı hücreleri (tlBakiye, hasGramBakiye, usd/eur/gbpBakiye, adet, birimMaliyetTl) `DefterOrtak.SayiOku` üzerinden bu okuyucuya bağlandı. Virgül içeren hücre → `SAYI_GECERSIZ`, mesaj: "'1,5' okunamadı — ondalık ayracı NOKTA, binlik ayracı yok (ör. 1,5 yerine 1.5; 1.500,50 yerine 1500.50 yazın)". Faz A'nın `SayiOku`su DEĞİŞTİRİLMEDİ (kart alanları; § 0.4 açık kalan). Ekran: “Dosyayı nasıl hazırlarım?” katlanırına `#ia-ondalik-notu` (Goc47B sözlüğü `OndalikNotu`, TR+EN) | Doğrulayıcının `GOC47BSinirTestleri.Turkce_virgul_1_5_has_ON_KAT_okunmaz` + `Turkce_virgul_1500_50_TL_YUZ_KAT_okunmaz` **yeşil**; mutasyon M47B-15; canlı adım C-6 |
| **[ORTA]** Açılış bakiyesi cari yaşlandırmada 0–30 diliminde | Bu şeridin dosya alanı dışında (Rapor modülü). **Sahibine verildi:** “ACILIS türü cari yaşlandırmada BELIRSIZ (vadesi bilinmeyen) kovasına düşmeli” — ayrı paket/karar (§ 11) | — |
| **[DÜŞÜK]** Storno istisnası ADR'de yazılı değil; ters SAYIM_DUZELTME'de Adet pozitif | **ADR-0019** yazıldı (`dokumanlar/02-mimari/adr/0019-acilis-gocu-geri-alma-ters-kayit.md` + README satırı; 0016 numarası reyon ADR'sine verilmiş olduğundan 0019). Adet: `Hareket.Adet` sözleşmesi (Data/Entities.cs, “İŞARETSİZ (≥ 0): yön TlTutari ve HasGrami kolonlarındadır … kapı HareketFabrikasi.Ekle”) gereği pozitif kalır — eksi yazmak fabrika kapısını ihlal eder ve yönü iki kez sayar; ADR-0019 madde 4'te gerekçelendi. Kod değişmedi | ADR dosyası |
| **[DÜŞÜK]** Has tavanı DB kolonunun (decimal(15,6)) üstünde (1e9 provadan geçip uygulamada taşıyordu) | `DefterOrtak.HasTavani = 999_999_999.9999m`; üstü provada `TUTAR_ASIRI` | `Goc47BSayiTestleri.Has_tavani_…` + M47B-16; canlı C-6/C-7 (1e9 → TUTAR_ASIRI; 999.999.999,9999 uygulandı, `has_gram_bakiye = 999999999.999900`) |

Ek ekran düzeltmesi (ölçümde bulundu): hata tablosunun KOD hücresi uzun mesaj yanında 1280'de “Sayı / okunamadı” diye iki satıra kırılıyordu (K3 kırık 1) → hücreye mevcut `hucre-tek-satir` sınıfı (yeni CSS yok). Yeniden ölçüm 0.

### 0.1 Derleme · testler
- `dotnet build Kuyumcu.sln --nologo` → **0 Hata · 0 Uyarı** (son kodla, `r2-b3.log`).
- Yeni testler (+8 vaka / 4 metot): `Goc47BTestleri.Virgullu_sayi_iceren_satir_HATA_deftere_ON_KAT_yazilmaz` (DB: tırnaklı "1,5" ve "1500,50" → 2 satır SAYI_GECERSIZ, mesaj “1.5” önerir, bakiye 0) · `Goc47BSayiTestleri.Virgullu_sayi_hucresi_her_defter_kolonunda_SAYI_GECERSIZ` (Theory ×5: 1,5 · 1500,50 · 1,000 · 1.500,50 · 1,500.50; her biri TL/has/USD/adet/maliyet kolonunda) · `Nokta_ondalik_ve_isaret_okunur_us_ve_bosluk_red` · `Has_tavani_DB_kolonuna_sigar_999999999_9999_kabul_ustu_TUTAR_ASIRI`; bUnit `Sutun_eslestirme_…` katlanırı açıp `#ia-ondalik-notu` metnini doğrular.
- Filtreli `Goc47B|GOC47BSinir`: **40/40**; geniş filtre (`Goc47B|GOC47BSinir|IceAktarim|CsvOkuyucu|AcilisBakiye`, son kodla): **108/108**.
- Tam süit (tur başına bir kez): **8804 toplam · 8802 geçti · 0 başarısız · 2 atlandı** (`TOPLU1SinirTestleriTur2.T2_Kasa_adi_kesme_isaretli`, `SATIS3EkranTestleri.DOG_T24_…` — başka şeritlerin Skip'i). trx: `/private/tmp/kuyumcu-serit/trx-pGOC-47B-r2/`. Tam süitten sonra yalnız razor'da KOD hücresine `hucre-tek-satir` sınıfı eklendi; ardından ilgili 108 test yeniden yeşil. → **Yeni kırmızı 0.**

### 0.2 Mutasyon (tur 2; hepsi geri alındı, `cmp` AYNI)
| Kod | Değişiklik | Kırmızı test(ler) |
|---|---|---|
| M47B-15 | `KatiSayiOku` → `NumberStyles.Number` (binlik geri açılır) | `Goc47BSayiTestleri.Virgullu_sayi_hucresi_her_defter_kolonunda_SAYI_GECERSIZ` (1,5 · 1500,50 · 1,000 · 1,500.50) · `Goc47BTestleri.Virgullu_sayi_iceren_satir_HATA_deftere_ON_KAT_yazilmaz` · `GOC47BSinirTestleri.Turkce_virgul_1_5_has_ON_KAT_okunmaz` · `Turkce_virgul_1500_50_TL_YUZ_KAT_okunmaz` (7 kırmızı) |
| M47B-16 | Has tavanı eski değere (`AdetTavani * 1000m`) | `Goc47BSayiTestleri.Has_tavani_DB_kolonuna_sigar_999999999_9999_kabul_ustu_TUTAR_ASIRI` |
| M47B-17 | Ekrandan `#ia-ondalik-notu` satırı silinir | `Goc47BEkranTestleri.Sutun_eslestirme_zorunlu_eksikse_PROVA_kilitli_sebep_yazar_secince_govdeye_gider` |
Son mutasyondan sonra temiz derleme 0/0. Günlükler `/private/tmp/kuyumcu-serit/GOC-47B/mut-M47B-1{5,6,7}.log`.

### 0.3 denetim.sh (tur 2)
Tek ✗ **P-30 DEMO_INSAN_SURESI** (görevde hariç). Bu paketin sayaçları ✓: Iceaktarim/ dogrudan Add 0 · İçe aktarım → mevcut servis 2 · Kurulum/ dogrudan Add 0 · Açılış → cari yolu 1 · Sınırsız ToListAsync 145 (tavan 145) · CARI_BAKIYE_DOGRUDAN_YAZIM 0 · CARI_BAKIYE_AYNA_UYUSMAZ yeni 0.

### 0.4 Canlı doğrulama tur 2 (localhost:5162, kiracı `PAKETGOC-47B-TEST R2 Kuyumcu`, son kodla)
| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| C-1 | 2.000 ürün kartı (Faz A) prova + uygula | 200 | 200 (2000 geçerli) · 200 uygulanan 2000 (87 sn) |
| C-2 | 2.000 satır açılış stoku **prova** | 200, ≤ 60 sn, yazma 0 | **200 · ~1 sn** · 2000 geçerli · `yazilanKayit 0` · adet 3999 · maliyeti bilinmeyen 1999 · bilinen maliyet 3.001.000,00 |
| C-3 | **Uygula** | 200 | 200 (10 sn) · SQL `stok_partileri`: **DEVIR 2000 parti · 3999 adet · NULL maliyet 1000 · sıfır 0** · `hareketler` SAYIM_DUZELTME 2000 (TL 0, has 31.128,886) · stok 3999 · `acilis_kalemleri` STOK UYGULANDI 2000 |
| C-4 | 50 cari açılış bakiyesi prova + uygula (TL+has+USD/EUR/GBP) | 200/200 | prova 50 geçerli (TL 121.120 · has 181,4715 · USD 500 · EUR 277,5 · GBP −100) · uygula 50 · SQL ACILIS TRY 50 (TL 121.120, has −181,4715) + USD/EUR/GBP 5'er · `cari_bakiyeler` Σ TL 121.120 · has 181,4715 · USD 500 · EUR 277,5 · GBP −100 |
| C-5 | Ekstre aynası ×50 | 0 | `GET /cari/{id}/ekstre` **200×50 · bakiyeUyusmazligiHas 0: 50/50 · bakiyeUyusmazligiTl 0: 50/50** |
| C-5b | Aynı dosya ikinci prova + uygula | prova uyarır, uygula 409 | prova 200 `ayniDosyaOturumId` = ilk oturum, 50 satır HATA (ZATEN_ACILIS) · uygula **409** “Bu dosya (içeriği birebir aynı) daha önce uygulandı …” |
| C-6 | **Virgül/tavan provası** (tırnaklı "1,5" has · "1500,50" TL · has 1000000000 · has 999999999.9999) | 3 HATA, 1 geçerli | **200** · geçerli 1 · hatalı 3: satır 2 `SAYI_GECERSIZ` (“hasGramBakiye: '1,5' okunamadı — ondalık ayracı NOKTA …”) · satır 3 `SAYI_GECERSIZ` (tlBakiye '1500,50') · satır 4 `TUTAR_ASIRI` · özet has 999.999.999,9999 |
| C-7 | C-6 oturumunu uygula | yalnız geçerli satır; DB taşması yok | 200 uygulanan 1 / hatalı 3 · SQL: tek >1000 bakiye `has_gram_bakiye = 999999999.999900` (Sınır 1–3 bakiyesi değişmedi) |
| C-8 | Geri-al (C-6, bakiye, stok) | 200, önceki hâl | 200 `geriAlinan` 1 / 50 / 2000 (stok 39 sn) · SQL: Σ TL/has/USD/EUR/GBP **0**, sıfır-dışı bakiyeli cari **0** · ACILIS 132 satır net 0 (silme yok) · SAYIM_DUZELTME 4000 satır net has 0 · stok **0** · DEVIR adet_kalan 0 (giren 3999 korunur) · tüketim 2000 · kalem CARI 51 + STOK 2000 GERI_ALINDI · ayna 50/50 = 0 |

Fikstür: davet satırı SQL ile (yalnız jeton özeti), ek kullanıcılar API ile MUDUR açılıp yalnız bu test kiracısında SQL ile SAHİP + şubeye yükseltildi (içe aktarım oran sınırı kullanıcı başına 5/sa). Parolalar/jetonlar chmod 600 dosyalarda; ekrana basılmadı.
**Temizlik:** API 5162 ve vekil 5163 LISTEN **0**; Chrome 9907 süreç **0**. `zsh kiraci-temizle.sh <id> --uygula` → ikinci çağrı “Kiracı YOK”; `tenant_id` kolonlu **75 tabloda kalan satır 0**; davet satırı silindi (1). `tenants`: başlangıç 13, son **14** — fark `PAKETTOPLU-1-TEST Dogrulama3` (TOPLU-1 şeridi, 13:54'te açıldı); `PAKETGOC%` **0**, QA-TEST-* **4 (dokunulmadı)**.

### 0.5 Gerçek tarayıcı ölçümü tur 2 (başsız Chrome, CDP 9907, `sikisma-olc.js`)
| Durum | Genişlik | BASİT açık | BASİT koyu | PRO açık | PRO koyu |
|---|---|---|---|---|---|
| S0 “Dosyayı nasıl hazırlarım?” açık (açılış bakiyesi, `#ia-ondalik-notu` görünür) | 1280 / 375 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |
| S2 virgüllü dosya (sütun eşleştirmeli) kuru prova → hata tablosu | 1280 / 375 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |

“0” = K1 gizli-görünen · K3 taşan/kırık · K5 TRY · K9 binen · K11 · yatay kaydırma hepsi 0 (ilk koşumda S2 1280'de K3 kırık 1 = KOD hücresi “Sayı okunamadı” → düzeltildi, yeniden ölçüm 0; ilk koşum `olcum-tur1.jsonl`). K2 en geniş blok %98. K8 jargon 1 = sol menüdeki “Cari” (kabuk, bu sayfa değil). 375'te hata tablosu kendi kabında yatay kayar (sayfa kaymaz). Görüntüler Read ile görüldü: `/private/tmp/kuyumcu-serit/GOC-47B/durum2/foto/veri_ice_aktarim__S2-virgul-prova__PRO_karanlik_375.png`, `…__S2-virgul-prova__BASIT_aydinlik_1280.png` (+16 görüntü aynı dizinde). Ölçüm: `durum2/olcum.jsonl`, `kosum.log`.

### 0.6 Tur 2 değişen dosyalar
- DEĞİŞTİ `dotnet/Kuyumcu.Api/Modules/Iceaktarim/CsvOkuyucu.cs` (KatiSayiOku eklendi) · `DefterKaynaklari.cs` (DefterOrtak.SayiOku, HasTavani, 5 çağrı noktası)
- DEĞİŞTİ `dotnet/Kuyumcu.Core/I18n/Sozlukler.Goc47B.cs` (OndalikNotu TR+EN) · `dotnet/Kuyumcu.Web/Pages/VeriIceAktarim.razor` (`#ia-ondalik-notu`, KOD hücresi `hucre-tek-satir`)
- DEĞİŞTİ `dotnet/Kuyumcu.Tests/Goc47BTestleri.cs` (+DB testi, +`Goc47BSayiTestleri`, bUnit iddiası)
- YENİ `dokumanlar/02-mimari/adr/0019-acilis-gocu-geri-alma-ters-kayit.md` · DEĞİŞTİ `adr/README.md` (0019 satırı)
- Dokunulan sayfalar (MÜHÜR 2): yalnız `VeriIceAktarim.razor` (bu şeridin sayfası; gerekçe: hasım bulgusu düzeltmesi, aynı paket turu 2).

### 0.7 Tur 2 açık kalanlar
- **[ORTA → Rapor sahibi]** ACILIS türü cari yaşlandırmada “vadesi bilinmeyen” kovasına düşmeli (şimdi 0–30). Ayrı paket/karar.
- **Faz A** (`cari`/`urun` kart kaynakları) hâlâ `CsvOkuyucu.SayiOku` (NumberStyles.Number) kullanıyor: kart alanlarında (gramAgirlik, iscilikTl, satisFiyati, krediLimitiTl) '1,5' → 15 okunur. Deftere yazmaz ama ürün gramı/fiyatı 10 kat girebilir. 47-A dosyası/testleri bu turun kapsamı dışında bırakıldı; **aynı katı okuyucuya geçirilmesi önerilir** (ayrı küçük paket, 47-A testleri gözden geçirilerek).

## 1. Yapılanlar

- **İki yeni içe aktarım kaynağı** (47-A çerçevesi AYNEN kullanıldı — yeni çerçeve yok): `acilis-bakiye` (cari TL · has · USD/EUR/GBP) ve `acilis-stok` (DEVIR partisi). `IDefterKaynagi : IIceAktarilabilir` sözleşmesi ile prova/uygula/geri-al aynı uçlardan (`/api/v1/ice-aktarim/...`) yürür; sınıf düzeyi `[RolGerekli("SAHIP")]` + `[ModulYetkisi("veri-aktarim")]`, oran sınırı `ice-aktarim` 5/sa aynen.
- **Tek yazıcı:** cari bakiyesi `CariServisi.AcilisYaz → DuzeltmeYaz → CariBakiyeYazici` yolundan yazılır (CariModulu DEĞİŞMEDİ). TL+has tek `ACILIS` hareketi; her döviz bacağı ayrı `ACILIS` hareketi (TL=0) — TRY dışı satırda TL taşınmaz.
- **Açılış stoku:** `SAYIM_DUZELTME` hareketi (TL=0, `HareketFabrikasi.Ekle`) + stok artışı + `MaliyetMotoru.PartiAc(DEVIR)` (maliyet boşsa **NULL**, sıfır yazılmaz; `0` girilirse satır HATA) + yerleşim girişi + T-2'nin `AcilisKalemi(STOK, parti_id)` kaydı.
- **T-2 ile ortak şema/kapılar:** her satır `AcilisKalemi` açar → sihirbazla CSV aynı cariye/ürün-şubeye İKİNCİ açılış veremez (ZATEN_ACILIS). Kurulum **mühürlüyse 409**, açılış penceresi kapandıysa 409 (CSV mühürün arka kapısı değil).
- **Geri alma = ters kayıt** (silme yok, ADR-0004): cari için ters `ACILIS`, stok için ters `SAYIM_DUZELTME` + parti `adet_kalan=0` + tüketim satırı. DEVIR partisi tüketilmişse (satılmışsa) **409, hiçbir kalem geri alınmaz**. Geri alınan kalemin benzersiz anahtarı serbest bırakılır (`GERI:<id>`) → düzeltilmiş dosya yeniden uygulanabilir.
- **Aynı dosya (SHA-256) ikinci kez uygulanamaz → 409**; prova yanıtı `ayniDosyaOturumId` ile önceden uyarır.
- **Sütun eşleştirme** (otomatik tahmin yok, KVKK): `kolonEslemesi {bizimKolon: dosyaBasligi}` gövde alanı; ekranda dosyanın başlık satırı okunur, her alan için seçici; zorunlu eşleşme eksikse “Önce kontrol et” kilitli ve sebebi yanında yazar (UX-S30). Hepsi eşleşmişse katlanır KAPALI gelir.
- **Kuralar:** has en çok 4 ondalık (yuvarlanmaz, HAS_ONDALIK); emanet kolonu içeren dosya **dosya düzeyinde 400** (karar 3); cari/ürün bulunamazsa kart ÜRETİLMEZ (CARI_YOK/URUN_YOK); peşin müşteri sistem carisi reddedilir; ürünün şubede stoğu varsa STOK_VAR (açılış yalnız boş stoğa); tek kur fotoğrafı (kiracının en güncel kaydı; yoksa oturum açılmaz); tüm kayıtlar uygulama anı (UtcNow).
- **Ekran (VeriIceAktarim.razor):** kaynak adları, “Dosyayı nasıl hazırlarım?” katlanırında işaret/emanet/maliyet notları, eşleştirme katlanırı, prova özeti (UX-S23: TL/has/döviz toplamları; stokta adet, has, bilinen maliyet, **maliyeti bilinmeyen adet → /raporlar/kar-zarar bağlantısı**), aynı-dosya uyarısı, hata kodu sözlükten çevrilir (UX-S31), defter geri almasında ayrı onay metni + ipucu + sonuç bandı (`geriAlinan`).
- **Ek düzeltme (47-A çerçevesindeki gizli hata):** uygulama istisnasında rollback sonrası bağlam temizlenmiyordu; IPTAL yazımı aynı hatayı yeniden üretip oturumu PROVA'da asılı bırakıyordu → `db.ChangeTracker.Clear()` (mutasyon M47B-11 ile kanıtlı).

### Dokunulan sayfalar (MÜHÜR 2)
- `Kuyumcu.Web/Pages/VeriIceAktarim.razor` — bu şeridin dosyası (47-A sayfası; gerekçe: Faz B aynı ekranın devamı).

## 2. Sapmalar ve gerekçeleri

| # | Şartname | Kod gerçeği | Yapılan |
|---|---|---|---|
| S-1 | Açılış bakiyesi `CARI_DUZELTME` | T-2 + CARI-BAKIYE paketleri açılış için `ACILIS` türünü ve `CariServisi.AcilisYaz`ı TEK YAZICI olarak mühürledi | `AcilisYaz` kullanıldı (tür `ACILIS`; ayna kuralı ACILIS'i CARI_DUZELTME ile aynı sayar) |
| S-2 | Açılış stoku `UrunServisi.StokSayimGuncelle` | Bu metot kendi transaction'ını açar (dış transaction ile iç içe → hata), SAYIM_FAZLA partisi açar (DEVIR değil), değeri ÜZERİNE yazar; UrunModulu “değişmez” ve Y4-C şeridiyle ortak | Yazıcı `Modules/Kurulum/AcilisYazici.cs`'te (T-2'nin STOK açılış şeması; Kurulum/ altında HareketFabrikasi serbest, Add yasakları korunur). Hareket türü şartnamedeki gibi SAYIM_DUZELTME |
| S-3 | “Geri alma storno ile” (48-A motoru) | 48-A `StornolanabilirTurler` ACILIS/SAYIM_DUZELTME içermez (bilinçli 409); eklemek mühürden sonra da genel ekrandan açılış stornosunu açardı | T-2 GeriAl deseni: tek yazıcıdan ters kayıt + kalem GERI_ALINDI; mühürden sonra 409. StornoServisi'ne dokunulmadı |
| S-4 | Kur, “oturum açılırken alınan snapshot” | Bunu saklamak migration gerektirirdi | Uygulama anında tek kur (prova da kur yoksa 400 verir); kur kimliği denetim izine yazılır. Satır başına kur uydurulmaz. **Migration gerekmedi** |
| S-5 | Faz B'de GUNCELLE politikası | — | Sunulmadı; ATLA yalnız ZATEN_ACILIS/YINELENEN_SATIR'ı atlar |
| S-6 | Kart satırları gibi `IceAktarimKolonu` adları | Sütun eşleştirme ekranında ham camelCase etiket jargondu | Etiket: “Ürün kodu (kod)” biçiminde okunur ad + dosya başlığı |
| S-7 | 47-A testi `Gecmis_hareket_kaynagi_YOKTUR…` acilis-bakiye/acilis-stok'u 400 bekliyordu | Faz B ile artık geçerli kaynak | Liste satis/kasa/fatura/hareket/gecmis-satis olarak güncellendi (seçenek 3 VETO aynen sınanıyor) |
| S-8 | 47-A ekran fikstürü `aaaa…` içerikli CSV | Ekran artık başlık satırını okuyor; zorunlu kolonsuz dosyada prova kilitli | Fikstür `tur,ad` başlığıyla başlar (IceAktarimEkranTestleri.Csv) |

Mevzuat: bu paket yeni yasal eşik/oran **eklemedi** (para el değiştirmez; MASAK/tevsik kapıları tetiklenmez — CariModulu düzeltme gerekçesiyle aynı). Kaynak gerekmedi; tahmin yazılmadı.

## 3. Derleme ve migration
- `dotnet build Kuyumcu.sln --nologo` → **0 Hata · 0 Uyarı** (son temiz derleme; ara derlemelerde görülen 2 uyarı `SUBEKAPSAMASinirTestleri.cs` ve `KesifP2Testleri.cs` — başka şeritlerin dosyaları).
- Migration: **gerekmedi** (mevcut `ice_aktarim_*`, `acilis_kalemleri`, `stok_partileri` yetti).

## 4. Testler
Yeni: `Kuyumcu.Tests/Goc47BTestleri.cs` — **20 test** (16 DB · 1 HTTP · 3 bUnit), hepsi yeşil.
DB: Acilis_bakiyesi_TEK_YAZICIDAN_ACILIS_hareketi_uretir_bakiye_ve_ayna_tutarli · PROVA_acilis_defter_ve_stoga_YAZMAZ · Has_4_ondaligi_asan_satir_HATA_yuvarlanmaz_karar5 · Emanet_kolonu_olan_dosya_REDDEDILIR_bakiyeye_girmez_karar3 · Cari_bulunamazsa_kart_URETILMEZ_satir_HATA · Sutun_eslestirme_dosya_basligini_bizim_alana_cevirir · Acilis_stoku_DEVIR_partisi_acar_ve_birim_maliyet_NULL_kalir · Acilis_stoku_maliyet_SIFIR_satir_HATA_stok_var_satir_HATA · Bir_satir_patlarsa_TUM_acilis_geri_alinir_kismi_goc_YOK · Geri_al_TERS_KAYITLA_bakiye_ve_stok_onceki_haline_doner_kayit_silinmez · Acilis_stogu_tuketilmisse_geri_al_REDDEDILIR_kismi_geri_alma_YOK · Ayni_dosya_IKINCI_kez_uygulanamaz_409_SHA256 · ATLA_politikasi_zaten_acilisi_olan_satiri_atlar · Muhurlu_kurulumda_acilis_icee_aktarimi_409_arka_kapi_YOK · Baska_kiracinin_acilis_oturumu_geri_alinamaz_404 · Iki_bin_satirlik_acilis_stoku_provasi_60_saniyede_biter.
HTTP: Ayni_dosya_ikinci_uygulama_HTTP_409_ozet_camelCase. bUnit: Sutun_eslestirme_zorunlu_eksikse_PROVA_kilitli_sebep_yazar_secince_govdeye_gider · Prova_ozeti_maliyetsiz_adet_baglantisi_ayni_dosya_uyarisi_ve_cevrilmis_hata_kodu · Defter_kaynaginda_geri_al_TERS_KAYIT_onayi_ister_ve_sonucu_banda_yazar.

Filtreli (Goc47B|IceAktarim|CsvOkuyucu|AcilisBakiye|ZoBelge): **325/325** yeşil (eski 47-A + T-2 + ZO-BELGE testleri dahil).
Tam süit (tek koşum, son kod öncesi etiket değişikliği hariç aynı kod): **8782 toplam · 8778 geçti · 2 başarısız** (§ 7, ikisi de başka şeridin). trx: `/private/tmp/kuyumcu-serit/trx-pGOC-47B`.
Not: tam süitten sonra yalnız eşleştirme ETİKET metni değişti (`G.KolonEtiketi`, sözlük + razor 1 satır); Goc47B ekran testleri bu değişikliğe bağlı değil, derleme 0/0.

## 5. Mutasyon kanıtları (hepsi geri alındı, `cmp` AYNI)

| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M47B-1 | DEVIR partisinde NULL yerine 0 maliyet (`parti.BirimMaliyetTl ??= 0m`) | Acilis_stoku_DEVIR_partisi_acar_ve_birim_maliyet_NULL_kalir |
| M47B-2 | Defter atlanıp bakiye doğrudan `CariBakiyeYazici`'ye yazılır | Acilis_bakiyesi_TEK_YAZICIDAN_ACILIS_hareketi_uretir_bakiye_ve_ayna_tutarli |
| M47B-3 | Satır satır commit (transaction bölünür) | Bir_satir_patlarsa_TUM_acilis_geri_alinir_kismi_goc_YOK (beklenen 0 hareket, gerçek 2) |
| M47B-4 | ZATEN_ACILIS kapısı kapatılır | ATLA_politikasi_zaten_acilisi_olan_satiri_atlar |
| M47B-5 | SHA-256 aynı-dosya kapısı kapatılır | Ayni_dosya_IKINCI_kez_uygulanamaz_409_SHA256 + Goc47BHttpTestleri.Ayni_dosya_ikinci_uygulama_HTTP_409_ozet_camelCase |
| M47B-6 | Emanet kolonu yasağı boşaltılır | Emanet_kolonu_olan_dosya_REDDEDILIR_bakiyeye_girmez_karar3 |
| M47B-7 | Has ondalık sınırı 4→6 | Has_4_ondaligi_asan_satir_HATA_yuvarlanmaz_karar5 |
| M47B-8 | Tüketilmiş parti kapısı kapatılır | Acilis_stogu_tuketilmisse_geri_al_REDDEDILIR_kismi_geri_alma_YOK |
| M47B-9 | Mühür kapısı silinir | Muhurlu_kurulumda_acilis_icee_aktarimi_409_arka_kapi_YOK |
| M47B-10 | Geri almada anahtar serbest bırakılmaz | Geri_al_TERS_KAYITLA_bakiye_ve_stok_onceki_haline_doner_kayit_silinmez |
| M47B-11 | Rollback sonrası `ChangeTracker.Clear` silinir | Bir_satir_patlarsa_TUM_acilis_geri_alinir_kismi_goc_YOK (oturum IPTAL yerine PROVA) |
| M47B-12 | Sıfır maliyet kapısı kapatılır | Acilis_stoku_maliyet_SIFIR_satir_HATA_stok_var_satir_HATA |
| M47B-13 | Ekran eşlemeyi gövdeye koymaz | Goc47BEkranTestleri.Sutun_eslestirme_zorunlu_eksikse_PROVA_kilitli_sebep_yazar_secince_govdeye_gider |
| M47B-14 | Geri almada stok düşülmez | Geri_al_TERS_KAYITLA_bakiye_ve_stok_onceki_haline_doner_kayit_silinmez |

Yöntem: `/private/tmp/kuyumcu-serit/GOC-47B/mut.sh` (yedek `/private/tmp/kuyumcu-serit/mut-yedek/GOC-47B/`). Son mutasyondan sonra temiz derleme 0/0.

## 6. denetim.sh
Son koşum: tek ✗ **P-30 DEMO_INSAN_SURESI** (görev metninde hariç tutulan, bu paketle ilgisiz). Bu paketin sayaçları: `Iceaktarim/ altında dogrudan Add 0` ✓ · `İçe aktarım → mevcut servis çağrısı 2` ✓ · `Kurulum/ altında dogrudan Add 0` ✓ · `Açılış → mevcut cari yolu 1` ✓ · `CARI_BAKIYE_DOGRUDAN_YAZIM 0` ✓ · `Sınırsız ToListAsync 145 (tavan 145)` ✓ (ilk koşumda yeni 5 sorgu 150 verdi → hepsine `.Take(SorguTavani+1)` + aşımda 400) · `CARI_BAKIYE_AYNA_UYUSMAZ (canlı) yeni 0` ✓ (50 carinin açılışı kuyumcu_net'te dururken ölçüldü). İlk koşumda görülen 1 “yeni” ayna uyuşmazlığı (cari 01M3VC0J…) başka bir şeridin geçici kiracısıydı; bir sonraki sorguda satır yoktu.

## 7. Tam süitteki kırmızılar (bu paketin ürünü DEĞİL)
- `OrtakGorunumSaflik.O7_ortak_css_sabit_renk_ve_gizleyen_tasma_tasimaz` — `wwwroot/css/ortak.css` (13:02'de değişti, `.secici-metin--tam` `line-clamp` + `overflow:hidden`): **TOPLU-1 şeridi** (Secici). Bu paket CSS'e dokunmadı.
- `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter_DOGRUDAN_ekler_bekleyen_arama_IPTAL` — tek başına koşunca **yeşil** (yük altında kararsız); Satış ekranı, bu paketle ilgisiz.

## 8. Canlı doğrulama (localhost:5162, kiracı `PAKETGOC-47B-TEST A Kuyumcu`)

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | 2.000 ürün kartı (Faz A) prova+uygula | 200 · 2000 | 200 · 2000 geçerli / uygula 200, 2000 (87 sn) |
| 2 | 2.000 satır açılış stoku **prova** | 200, ≤ 60 sn, yazma 0 | **200 · 0,34 sn** · 2000 geçerli · `yazilanKayit 0` · özet: adet 3999, maliyeti bilinmeyen 1999, bilinen maliyet 3.001.000 ₺ · SQL: parti 0 · hareket 0 · stok 0 · kalem 0 |
| 3 | Açılış stoku **uygula** | 200 | **200** (9 sn) · SQL `stok_partileri`: **DEVIR 2000 parti · 3999 adet · NULL maliyet 1000 · sıfır maliyet 0** · `hareketler` SAYIM_DUZELTME 2000, TL 0, has 31.128,886 · stok 3999 · `acilis_kalemleri` STOK 2000 |
| 4 | Stok yaşlandırma raporu | maliyeti bilinmeyen görünür | **200** · kova 0–30: adet 3999, maliyetiBilinmeyenAdet **1999** |
| 5 | Stok **geri-al** | 200, stok önceki hâl | **200** `geriAlinan 2000` (24 sn) · stok **0** · parti adet_kalan 0 (giren 3999 korunur) · SAYIM_DUZELTME 4000 satır (silme yok) · kalem GERI_ALINDI 2000, anahtar serbest 2000 · tüketim satırı 2000 |
| 6 | 50 cari açılış bakiyesi **prova** (TL+has+USD/EUR/GBP) | 200 | **200** · 50 geçerli · özet TL 121.120 · has 181,4715 · USD 500 · EUR 277,5 · GBP −100 · SQL: ACILIS 0, bakiye≠0 olan 0 |
| 7 | **Uygula** | 200 | **200** 50 · SQL: ACILIS TRY 50 (TL 121.120, has −181,4715) · USD 5 · EUR 5 · GBP 5 (TL 0) · cari_bakiyeler Σ TL 121.120 · Σ has 181,4715 |
| 8 | 50 caride **ekstre aynası** | uyuşmazlık 0 | `GET /cari/{id}/ekstre` ×50 → **200 ×50 · bakiyeUyusmazligiHas 0: 50/50 · bakiyeUyusmazligiTl 0: 50/50** · denetim.sh canlı ayna yeni 0 |
| 9 | Aynı dosya (ikinci prova oturumu) **ikinci uygulama** | 409 | **409** “Bu dosya (içeriği birebir aynı) daha önce uygulandı…” |
| 10 | Bakiye **geri-al** | bakiye 0 | **200** `geriAlinan 50` · Σ TL/has/USD/EUR/GBP = **0** · ACILIS satırları 130 (silme yok, net 0) · ayna 50/50 = 0 |
| 11 | Denetim izi | hassas, gerekçeli | `IceAktarim` hassas=1 **9 kayıt** (prova/uygula “tek kur: …”/geri alındı “TERS KAYITLA”) · ACILIS 130 · AcilisKalemi 4100 |
| 12 | Cari yaşlandırma | — | 200 · 0–30 dilimi 121.120 (açılış uygulama anıyla tarihlendiği için “BELIRSIZ” değil — § 11) |

Kiracı ve kullanıcılar: kayıt uç davetli (`kiraci_davetleri`'ne 2 test daveti yazıldı; kayıt oran sınırı nedeniyle tek kiracı açılabildi). İçe aktarım oran sınırı kullanıcı başınaydı (5/sa); ek SAHİP kullanıcıları API ile MUDUR açılıp test kiracısında SQL ile SAHİP + şubeye yükseltildi (fikstür, yalnız bu kiracıda). Parolalar `chmod 600` dosyalarda; ekrana basılmadı.

**Temizlik:** API (5162) ve vekil (5163) durduruldu → LISTEN **0**; Chrome (9905/9906) süreç **0**. `zsh kiraci-temizle.sh <id> --uygula` → kiracı, hareket, cari, ürün satırı **0**; davet satırları **2 silindi**. `tenants`: başlangıçta 14, sonda **13** — aradaki fark başka şeritlerin eşzamanlı kiracılarıdır; `PAKETGOC%` kiracı **0**, QA-TEST-* **4 (dokunulmadı)**.

## 9. Gerçek tarayıcı ölçümü (başsız Chrome, `sikisma-olc.js` / tur.mjs)

| Rota / durum | Genişlik | BASİT açık | BASİT koyu | PRO açık | PRO koyu |
|---|---|---|---|---|---|
| /veri-ice-aktarim (açılış + 50 açıcı katmanı) | 1280 | 0 | 0 | 0 | 0 |
| aynı | 375 | 0 | 0 | 0 | 0 |
| S1 sütun eşleştirme açık | 1280 / 375 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |
| S2 kuru prova sonucu (özet + hata tablosu) | 1280 / 375 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |

“0” = K1 gizli-görünen · K3 taşan/kırık · K5 TRY · K7 · K8 jargon · K9 binen · K10 · K11 hata bandı · K12/K13 · yatay kaydırma hepsi 0; K2 dar sütun yok (en geniş blok %97–98). Tek tur bulgusu: üst çubuktaki genel arama (`#ust-kayit-ara`) 375'te SERBEST-YÜZEN katman — kabuk bileşeni, bu sayfaya ait değil. Görüntüler okundu: `/private/tmp/kuyumcu-serit/GOC-47B/durum/foto/veri_ice_aktarim__S2-prova-sonucu__BASIT_aydinlik_1280.png`, `…__PRO_karanlik_375.png` (+ S1 ve temel tur fotoğrafları `/private/tmp/kuyumcu-serit/GOC-47B/tur/foto/`). 375'te hata tablosu kendi kabında yatay kayar (sayfa kaymaz).

## 10. DoD sözleşmesi
| Varlık | Yol | Not |
|---|---|---|
| `ice_aktarim_oturumlari` (Faz B oturumu) | PATCH `/ice-aktarim/{id}` (yalnız PROVA) · POST `/iptal` · POST `/geri-al` | 47-A uçları aynen |
| `acilis_kalemleri` (CARI/STOK) | Düzenlenmez — **ters kayıtla geri alınır** (`/geri-al`), ekranda Geri al düğmesi + onay | Finansal iz; silme yok |
| `hareketler` (ACILIS / SAYIM_DUZELTME) | Hard DELETE yok; ters satır | ADR-0004 |
| `stok_partileri` (DEVIR) | Geri almada `adet_kalan=0` + tüketim satırı | Satılmış partide 409 |
| Satır sonucu (`ice_aktarim_satirlari`) | Değişmez — DegismezIpucu (“dosyayı düzeltip yeniden prova”) | 47-A |

## 11. Açık kalanlar
- Tur 1 tam süit kırmızıları § 7 (TOPLU-1 ortak.css · SATIS3 kararsız) — tur 2 tam süitinde ikisi de yok (0 başarısız).
- Cari yaşlandırma açılış bakiyesini “0–30” diliminde gösteriyor; şartname “vadesi bilinmeyen” bekliyordu. Rapor modülü bu şeridin dosya alanı dışında — ayrı karar/paket (hasım ORTA bulgusu; sahibine). Faz A sayı okuyucusu için § 0.7.
- Rakip program profilleri (Dezy/KuyumHub) bilinçli olarak YOK (ek kural).
- Emanet açılışı CSV ile alınmıyor (karar 3); toplu emanet açılışı istenirse EmanetModulu'nun dış transaction'a katılması gerekir (ayrı paket).
- `SAHIP` rolündeki şubesiz kullanıcı açılış yapamaz (400 “şube seçimi gerekli”) — mevcut sistem davranışı.

## 12. Kurucu / mali müşavir
- Açılış bakiyeleri ve açılış stoku **uygulama tarihiyle** deftere girer (geçmiş tarih kabul edilmez). Mali müşavir: devir tarihinin (ör. 31.12) ayrıca belgelenmesi gerekiyor mu? Kodda bu yönde hüküm yok, tahmin yazılmadı.
- Açılış stoku maliyeti bilinmeyen kalemler kâr-zararda “maliyeti bilinmeyen” sayılır; mali müşavirin envanter değerlemesi için ayrı bilgi gerekebilir.

## 13. Değişen dosyalar
- YENİ `dotnet/Kuyumcu.Api/Modules/Kurulum/AcilisYazici.cs`
- YENİ `dotnet/Kuyumcu.Api/Modules/Iceaktarim/DefterKaynaklari.cs`
- DEĞİŞTİ `dotnet/Kuyumcu.Api/Modules/Iceaktarim/IceAktarimModulu.cs` · `Kaynaklar.cs` (kayıt listesi)
- DEĞİŞTİ `dotnet/Kuyumcu.Core/Sozlesmeler/IceAktarimSozlesmeleri.cs`
- YENİ `dotnet/Kuyumcu.Core/I18n/Sozlukler.Goc47B.cs`
- DEĞİŞTİ `dotnet/Kuyumcu.Web/Pages/VeriIceAktarim.razor`
- YENİ `dotnet/Kuyumcu.Tests/Goc47BTestleri.cs` · DEĞİŞTİ `IceAktarimTestleri.cs` (S-7) · `IceAktarimEkranTestleri.cs` (S-8)
(`git status --porcelain` bu dizinleri izlenmeyen `??` olarak gösteriyor — depo ilk commit'ten beri bu ağaçları izlemiyor.)
