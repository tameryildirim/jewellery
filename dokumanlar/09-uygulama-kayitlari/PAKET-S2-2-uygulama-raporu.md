# PAKET S2-2 — Tezgâh fişi: düğme ızgarası + tek grid VERDİM/ALDIM + FARK · uygulama raporu

**Tarih:** 2026-09-24 · **Şerit:** S2 (Tezgâh fişi) · **Rol:** Taşkın + Ertuğ (Opus)
**Spesifikasyon:** `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § S-2 madde 2.2 + 2.3 · taslak `sadelestirme/tuval/project/Tezgah.dc.html` (canvas notu n1)
**Durum:** BİTTİ. Paket kapısının 7 maddesi sağlandı. Tam süitte kalan kırmızıların hepsi başka şeritlere ait (§ 10).

---

## 1. Ne yapıldı

**2.2 — Düğme ızgarası (basit mod).** Yeni bileşen `Bilesenler/Fis/FisIslemIzgarasi.razor`.
- Pro modda sekme şeridi Islemler.razor'dan buraya birebir taşındı. 8 sekme aynen duruyor; sınıflar ve id'ler de aynı.
- Basit modda ekranda "Ne yapıyorsun?" başlığı altında 8 büyük düğme var:
  - `BasitPos.Turler`'den gelen 5 tür: Satış, Alış, Hurda Alış, Takas, Satış İadesi.
  - `BasitPos.Kisayollar`'dan gelen 3 kısayol: Tahsilat, Ödeme, Emanet.
- Düğmeler `.ekran-karo` sınıfını kullanıyor: 68 px yükseklik (≥ 44 px), her birinde ikon var. Seçili tür `.ekran-karo-birincil` ile gösteriliyor.
- **Her tür düğmesi `#islem-tur-{TÜR}` id'sini koruyor.**
- Kısayollar:
  - **Emanet** → `/emanet`.
  - **Tahsilat** → `/cariler`. Seçili müşteri gerçek bir müşteriyse bağlantı `/cariler?cariId=…` olur. Cariler sayfası bu süzgeci zaten okuyor (P-15 DerinBaglanti). Peşin Müşteri seçiliyse düz liste açılır.
  - **Ödeme** → kilitli. Sebebi düğmenin altında yazıyor (`#islem-git-odeme-kilit`), bkz. § 2 S3.

**2.3 — Tek grid VERDİM / ALDIM + FARK (basit SATIŞ).** Yeni bileşen `Bilesenler/Fis/FisVerdimAldim.razor`.
- **Yerleşim:** kalemler (VERDİM) ve ödeme (ALDIM) aynı kartta iki sütunda duruyor. Altta FARK satırı var.
- **Sütunların içeriği mevcut bileşenlerdir:** `FisKalemTablosu` (satır içi düzenleme), fiş iskontosu, `FisOdemeKasa`, `FisOdemeBacaklari`. Hiçbir özellik ve hiçbir id kaybolmadı. Sayfa bu bölümleri `RenderFragment` olarak bir kez tanımlıyor; pro modda eski yerlerinde, basit satışta sütunlarda çiziliyor. İşaretleme kopyalanmadı.
- **FARK kuralı tek yerde:** `Bilesenler/Fis/FisFark.cs` (saf sınıf).
  - FARK = VERDİM (net toplam) − ALDIM (bacak toplamı).
  - Kaydet yalnız FARK tam 0 iken açılır. Sayfanın `KarmaGonderilebilir` kilidi de `FisFark.KaydetAcik` ile aynı kuralı okuyor; `BacakKalan` da `FisFark.Hesapla` üzerinden hesaplanıyor.
  - Farkın hâli ekranda yazıyor: "Fark yok", "Ödeme eksik" ya da "Ödeme fazla".
- **Tek ödeme:** Tek ödemeli fişte FARK zaten 0'dır. ALDIM sütununda tek ödeme satırı durur ("Tamamı tek ödemeyle alınıyor").
  - "Ödemeyi böl" → tek ödeme ilk bacak olur (araç, kasa, fişin tamamı).
  - "Tek ödemeye dön" → bacaklar silinir.
- **Açık hesap:** FARK > 0 ise "Açık hesap olarak kaydet" düğmesi çıkar.
  - Gerçek müşteride farkı mevcut VERESİYE bacağıyla kapatır ve **tek POST yolu `Kaydet()`**'i çağırır.
  - Peşin Müşteri'de, müşteri seçilmemişken ya da zaten bir VERESİYE bacağı varken düğme kilitlidir ve sebebini yazar (UX-S30).

**İlk bakış ölçüsü (≤ 3 alan + ≤ 12 düğme).** `FisKalemGirisi` basit modda elle ekleme satırını (ürün, adet, fiyat, Ekle) `<details id="islem-elle">` içine katlıyor ("Elle ekle").
- Alanlar DOM'da duruyor, id'ler aynı.
- Ürün seçilince ya da alanda hata varsa katman kendiliğinden açılıyor.
- Ölçüm (`Basit_satista_ilk_bakista_en_cok_UC_alan_ve_ON_IKI_dugme`): ızgara + fiş kartı içinde kapalı `<details>` dışında kalanlar sayıldı.
  - Alan: müşteri, barkod, açıklama → **3**.
  - Düğme: 8 ızgara, barkod ara, "Elle ekle", "Ödemeyi böl", Kaydet → **≤ 12**. Test eşiği iddia ediyor.

**Diğer:**
- `Kaydet()`'in karma (fiş) gövdesi artık `Govde(...)`'den geçiyor. Basit modda gizli `vadeGun` anahtarı karma gövdeye de konmuyor (Mehmet K7; eskiden karma gövde bu kuralın dışındaydı).
- Tür listesi ve adları `Bilesenler/Fis/FisTurleri.cs`'e, fiş iskontosu satırı `Bilesenler/Fis/FisIskontoSatiri.razor`'a taşındı. İkisi de birebir taşıma. Sebep: sayfanın 1.400 satır sınırı.
- Yeni metinler ayrı sözlükte: `Kuyumcu.Core/I18n/TezgahFisiSozlugu.cs` (TR + EN, konumsal record → eksik anahtar derleme hatası verir). Tür adları pro sekmeleriyle aynı kaynaktan geliyor (`Dil.T.Islem.*`, UX-S31). Tahsilat ve Emanet adları mevcut `Dil.T.Cari.Tahsilat` ve `Dil.T.Nav.Emanet`.

## 2. Sapmalar ve gerekçeleri

| # | Spesifikasyon | Uygulanan | Gerekçe |
|---|---|---|---|
| S1 | "Birleşik koşullar" bölümü BAĞLAYICI | Planda bu adda bölüm **yok** (B-3 raporu da aynısını ölçmüştü) | Bağlayıcı olarak plan § 3, § 5B, S-2 2.2/2.3, D2/D3 kararları (`KARARLAR-D1-D11`) alındı |
| S2 | `BasitPos.Turler` 3 → 6 (SATIS, ALIS/HURDA_ALIS, TAHSILAT, ODEME, SATIS_IADE, TAKAS) | Turler = 5 sayfa türü; TAHSILAT/ODEME/EMANET ayrı `Kisayollar` dizisinde | TAHSİLAT ve ÖDEME bu sayfanın işlem türü değil. `TumTurler`'de yoklar, `Kaydet()` onları bilmiyor. Türe eklenselerdi tıklanınca boş form çıkardı. Karar yine tek yerde (BasitPos). |
| S3 | ÖDEME düğmesi | Kilitli + sebep | Tedarikçiye ödeme ucu **yok**: CariModulu'nda yalnız `{id}/tahsilat`, `{id}/veresiye`, `{id}/duzeltme` var (grep ile doğrulandı). DoD EK md.1 gereği düğme ölü değil, kilitli ve sebebini yazıyor. Uç açılınca `Kisayollar`'da tek satır değişir. |
| S4 | D3: "basit modda karma **her zaman** açık (`ZorunluGorunur`)" | Karma basit modda **erişilebilir**: "Ödemeyi böl" ile açılıyor. Varsayılan tek ödeme. | UX-3 yasal kapı sözleşmesi (tevsik → ödeme türü seçicisi açılır, `#islem-odeme-sade`, tek kasa gövdesi) tek ödeme üzerine kurulu ve 13 BasitModPos testiyle kilitli. Karma varsayılan yapılsaydı bu kapı testleri anlamını yitirirdi. Seçilen yolda tek ödemede FARK kendiliğinden 0 (3 tık: okut → Kaydet), bölünce Pusula formülü aynen işliyor. Yasal kapıların hiçbiri gevşemedi. |
| S5 | Taslakta VERDİM kompakt liste (Ürün · Ayar · Gram · Tutar) | VERDİM sütununda mevcut `FisKalemTablosu` | Özellik silinmez: satır içi adet/fiyat/iskonto düzeltmesi, hediye, sil düğmeleri korunmalı. |
| S6 | Taslakta tür adları "Alış · eski altın", "İade", "Değişim" | Mevcut `Dil.T.Islem.*` adları | UX-S31 (aynı kavram tek ad). Ad göçü D10 sözlük kuyruğunda, `Sozlukler.cs` ortak hub. |
| S7 | Sabit renk yok, belirteç var | `app.css`'in mevcut belirteçli sınıfları (`.ekranlar-izgara`, `.ekran-karo`, `.form-grid-2`, `.ekranlar-grup-baslik`) | `app.css` ve `index.html` bu şeridin dosya alanında değil; scoped CSS de yüklenmiyor. Yeni dosyalarda satır içi style 0, ham hex 0 (test sınıyor). |
| S8 | TAKAS = "değişim" düğmesi | TAKAS basit ızgarada, adı "Takas" | S6 ile aynı gerekçe |

## 3. Id envanteri
- `Islemler.razor` (paket öncesi) içindeki literal `id="…"` listesi, sonraki `Islemler.razor + Bilesenler/Fis/*.razor` birleşimiyle karşılaştırıldı. **Silinen id: 0** (`comm -23` boş).
- `islem-tur-{deger}` dinamik kalıbı artık `FisIslemIzgarasi.razor`'da, her iki modda da çiziliyor.
- `S21FisYapiTestleri.Id_envanteri_korunur_hicbir_id_silinmedi` yeşil.
- Yeni id'ler: `islem-izgara`, `islem-git-tahsilat`, `islem-git-odeme`, `islem-git-odeme-kilit`, `islem-git-emanet`, `islem-verdim-aldim`, `islem-verdim`, `islem-verdim-bos`, `islem-verdim-toplam`, `islem-aldim`, `islem-aldim-tek`, `islem-odeme-bol`, `islem-odeme-bol-kilit`, `islem-odeme-birlestir`, `islem-fark`, `islem-fark-tutar`, `islem-fark-durum`, `islem-acik-hesap`, `islem-acik-hesap-kilit`, `islem-elle`, `islem-elle-ac`.

## 4. Migration
**Gerekmedi.** Şema değişikliği yok, sunucuya dokunulmadı. Sunucu kuralları (fark ≠ 0 → 400, Peşin Müşteri'ye VERESİYE → 400) B-3'te zaten vardı ve canlıda yeniden sınandı.

## 5. DoD sözleşmesi
Yeni varlık ya da kayıt türü yok, yeni uç yok.

| Kayıt | Düzenleme / düzeltme yolu |
|---|---|
| Ödeme bacağı (kaydetmeden önce) | Araç, tutar, kasa, POS; sil; kalanı ata (`FisOdemeBacaklari`); "Tek ödemeye dön" hepsini geri alır |
| Açık hesap bacağı | Bacak listesinde görünür, silinebilir, tutarı düzeltilebilir. Kayıt sonrası borç Müşteriler → Tahsilat'tan kapanır (Tahsilat kısayolu) |
| Kaydedilmiş fiş | Değişmez: mevcut `DegismezIpucu` → Satış İadesi (S2-1'den aynen) |

Her düğme ya istek atıyor ya da ekranda bir iş yapıyor. Kilitli olanlar sebebini yazıyor: Ödeme kısayolu, boş fişte "Ödemeyi böl", Peşin'de "Açık hesap".

## 6. Testler

**Yeni: `Kuyumcu.Tests/S22TezgahFisiTestleri.cs`, 22 yöntem / 26 test, 26/26 yeşil**
- **Saf (7):** `FisFark_fark_VERDIM_eksi_ALDIM_tek_odemede_sifir`, `FisFark_Kaydet_YALNIZ_tam_sifirda_acik`, `FisFark_acik_hesap_Pesin_Musteride_KILITLI_gercek_caride_ACIK`, `FisFark_acik_hesap_bacagi_VERESIYE_kasasiz_ve_farki_kapatir`, `FisFark_ilk_bacak_tek_odemeyi_tasir_tanimadigi_arac_NAKITe_duser`, `BasitPos_izgara_bes_tur_uc_kisayol_ODEME_kilitli`, `TezgahFisi_sozlugu_TR_EN_bos_anahtar_yok_ve_esik_sayisi_yok`
- **bUnit ızgara (4):** `Basit_izgara_sekiz_buyuk_dugme_id_ler_korunur_kisayollar_gercek_adres`, `Izgarada_tur_dugmesi_turu_degistirir`, `Tahsilat_gercek_musteride_o_musteriye_Pesin_Musteride_listeye_acilir`, `Pro_modda_izgara_YOK_sekiz_sekme_AYNEN`
- **bUnit ilk bakış (2):** `Basit_satista_ilk_bakista_en_cok_UC_alan_ve_ON_IKI_dugme`, `Basit_modda_elle_satiri_KATLI_urun_secilince_KENDILIGINDEN_acik`
- **bUnit FARK (8):** `Tek_odemede_VERDIM_ALDIM_tek_kartta_FARK_sifir_KAYDET_acik`, `Odemeyi_bol_tek_odeme_ILK_bacak_olur_fis_toplami_ve_kasasiyla`, `FARK_kapanmadan_KAYDET_kilitli_kapaninca_nakit_kart_TEK_POSTta_gider`, `Fazla_odemede_KAYDET_kilitli_ve_sebep_yazar`, `Acik_hesap_Pesin_Musteride_KILITLI_sebep_yazar_istek_gitmez`, `Acik_hesap_gercek_caride_farki_VERESIYE_bacagiyla_TEK_POSTta_kaydeder`, `Tek_odemeye_don_bacaklari_siler_tek_odeme_geri_gelir`, `Bos_fiste_bol_kilitli_ve_sebep_yazar`
- **Yapı (Theory ×5):** `Yeni_fis_dosyalari_istek_atmaz_satir_ici_style_ve_sabit_renk_yok`

**Değiştirilen test (spesifikasyon değişti):** `BasitModPosTestleri.Basit_modda_uc_sekme` → `Basit_modda_izgarada_bes_tur`. UX-3'ün 3 sekme iddiası plan 2.2 ile 5 türe genişledi; ISCILIK/TRANSFER/ALIS_IADE'nin basitte yokluğu hâlâ iddia ediliyor. Başka mevcut teste dokunulmadı.

**Filtreli POS + S22 koşumu** (S2-1'in 22 sınıflık POS filtresi + S22, `pS2-2/pos-filtre.txt`): **258/261**. Kalan 3 kırmızının hiçbiri bu paketin değil (§ 10).

**Tam süit** (`trx-pS2-2/_Mac_2026-09-24_00_27_45.trx`, `KUYUMCU_TEST_DB=kuyumcu_test_s22`):

| Koşum | Toplam | Geçti | Kaldı | Kırmızılar |
|---|---|---|---|---|
| Taban (paket öncesi, 23:54) | 4.541 | 4.537 | 4 | `MigrationHazirlikTestleri.EksikMigrationliSemadaBekleyenListesiDolar`, `SozlesmeKapsamTestleri.Kapsam_orani_beyan_edilen_degerle_ayni`, `SozlesmeKapsamTestleri.Kilitsiz_tip_listesi_circir_ile_donduruldu`, `YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner` |
| Son (00:27) | 4.612 | 4.607 | 5 | `P10CariNotEkranTestleri` ×2, `S32MusterilerEkranTestleri.S32_12_…`, `S21FisYapiTestleri.Fis_bilesenleri_ApiIstemci_enjekte_etmez_ve_istek_atmaz`, `S21FisYapiTestleri.Islemler_razor_1400_satiri_asmaz` |

**Madde (7): bu paketin ürettiği yeni kırmızı = 0.**
- `P10CariNot` ×2 ve `S32Musteriler`: sebep `Pages/Cariler.razor` (S3-A şeridi). Hata iletisi: "Cannot provide a value for property 'Modu' on type 'Cariler'".
- `Fis_bilesenleri_ApiIstemci`: iletide "FisAltinBacagi.cs 'ApiIstemci' içeriyor" yazıyor. Bu dosya eşzamanlı çalışan B-4/S2-4 şeridinin, 00:15'te oluştu.
- `Islemler_razor_1400`: bu paketin sonunda sayfa 1.398 satırdı. Aynı anda çalışan B-4/S2-4 şeridi sayfaya ALTIN bacağı satırları ekleyince 1.401'e çıktı. Kendi yorum satırlarımı sıkıştırıp sayfayı **1.397**'ye indirdim. Son filtreli koşumda bu test **yeşil**.
- Taban kırmızılarının 4'ü de son koşumda yeşil; başka şeritler kapatmış.

## 7. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/S2-2/`'ye kopyalandı → `perl -0777 -pi` ile tek eşleşmeli değişiklik → S22 + BasitModPos filtresi (39 test) → yedek geri kopyalandı → `cmp`. Derleme yarışında (başka şeridin yarım dosyası ya da kilitli `blazor.boot.json`) 40 sn arayla 6 deneme yapıldı. M-S22-3'ün ilk koşusu derleme yarışına düştü ve yeniden koşuldu. Hepsi yalnız bu paketin kendi dosyalarında yapıldı; eşzamanlı şeridin de düzenlediği Islemler.razor mutasyona sokulmadı.

| Kod | Değişiklik | Kırmızıya dönen testler | cmp |
|---|---|---|---|
| M-S22-1 | `FisFark.KaydetAcik`: `fark == 0m` → `fark >= 0m` (FARK kuralı) | `FisFark_Kaydet_YALNIZ_tam_sifirda_acik`, `FARK_kapanmadan_KAYDET_kilitli_…` | AYNI |
| M-S22-2 | `Hesapla`: `verdim - bacaklar` → `verdim + bacaklar` | `FisFark_fark_VERDIM_eksi_ALDIM…`, `FARK_kapanmadan…`, `Acik_hesap_gercek_caride…`, `Fazla_odemede…`, `Odemeyi_bol…` | AYNI |
| M-S22-3 | Peşin Müşteri kilidi söküldü | `FisFark_acik_hesap_Pesin_Musteride_KILITLI…`, `Acik_hesap_Pesin_Musteride_KILITLI…` | AYNI |
| M-S22-4 | Açık hesap bacağı `VERESIYE` → `NAKIT` | `FisFark_acik_hesap_bacagi_VERESIYE…`, `Acik_hesap_gercek_caride…` | AYNI |
| M-S22-5 | `BasitPos.Turler`'den TAKAS çıkarıldı | `BasitPos_izgara_bes_tur…`, `Basit_izgara_sekiz_buyuk_dugme…`, `Basit_modda_izgarada_bes_tur` | AYNI |
| M-S22-6 | Ekrandaki FARK bacakları yok sayar (`Hesapla(Toplam, null)`) | `FARK_kapanmadan…`, `Acik_hesap_gercek_caride…`, `Fazla_odemede…`, `Acik_hesap_Pesin…` | AYNI |
| M-S22-7 | Tahsilat derin bağlantısı `cariId` taşımaz | `Tahsilat_gercek_musteride_o_musteriye…` | AYNI |
| M-S22-8 | Elle satırı hiç kendiliğinden açılmaz | `Basit_modda_elle_satiri_KATLI_urun_secilince_KENDILIGINDEN_acik` | AYNI |
| M-S22-9 | "Ödemeyi böl" ilk bacağı 0 TL | `Odemeyi_bol_tek_odeme_ILK_bacak…`, `FisFark_ilk_bacak…` | AYNI |
| M-S22-10 | ÖDEME kısayolu kilitsiz (`/vezne`) | `BasitPos_izgara_bes_tur_uc_kisayol_ODEME_kilitli`, `Basit_izgara_sekiz_buyuk_dugme…` | AYNI |

**10/10 kırmızı.** Test açığı çıkmadı. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 hata, 0 uyarı** (`pS2-2/build-son.log`).

## 8. Denetim (`dotnet/denetim.sh`)

Koşum çıktısı `pS2-2/denetim.log`, tek ✗ var:

```
✗ Pages: @onclick'siz ve disabled'sız <button>  74 (tavan 71 — ARTIŞ)
DENETİM BAŞARISIZ
```

Bu sayaç bu paketin değil. Ölçüm dosya dosya yapıldı:

| Dosya | Sayı |
|---|---|
| Cariler.razor | 15 |
| Uyum.razor | 12 |
| Bildirimler.razor | 7 |
| Vezne.razor | 6 |
| Kasalar.razor | 6 |
| … | … |
| Islemler.razor | **0** |

Islemler.razor paket öncesi 1'di (sekme `<button>`'ı çok satırlı olduğu için sayılıyordu); o düğme bileşene taşındı. Yani bu paket sayacı **1 azalttı**. Artış S3-A/S3-B şeritlerinin dosyalarından geliyor (Cariler, Bildirimler, Kasalar).

Bu şeridin sayaçlarının hepsi ✓: P-06 düzenleme kalıbı, B-3 ESIK_ISTEMCIDE, P-10/P-16 DTO_FORM_ESLESMEZ, satır içi style, emoji. Yeni dosyalarda eşik sayısı yok.

## 9. Canlı doğrulama (5192, kuyumcu_net)

API, bu paketin kodunu içeren `bin/Debug/net8.0`'ın scratchpad kopyasından koşturuldu (`pS2-2/api-bin`, PID 4186). Web dll'inde `FisVerdimAldim` var (grep ile doğrulandı). Test kiracısı: `PAKETS2-2-TEST` (davetli kayıt). Gövdeler `Kaydet()`'in basit modda kurduğu biçimde (`vadeGun` anahtarı yok).

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | POST /auth/kayit | 201 | 201 | jeton var |
| 2 | GET /kur/guncel | 200+id | 200 | kur var |
| 3 | GET /cari/sistem-carisi | 200 | 200 | sistemCarisi=True |
| 4 | POST /cari + /urunler, kasa, PATCH stok | 201/201/201/200 | 201/201/**200**/200 | kasa oluşturma 200 döndü, kasa var |
| 6 | Tek ödeme (FARK kendiliğinden 0), Peşin nakit | 200 | **200** | kaydedildi |
| 7 | Ödemeyi böl: nakit 6.000 + kart 4.000, FARK 0 | 200 + fisId | **200** | fisId var |
| 8 | FARK 4.000 > 0, Peşin | 400 | **400** | "Ödeme bacaklarının toplamı fiş tutarına eşit olmalıdır: bacaklar 6.000,00 TL, fiş 10.000,00 TL (fark -4.000,00 TL)" |
| 9 | Açık hesap Peşin Müşteri'de | 400 | **400** | "Peşin Müşteri'ye borç (veresiye / açık hesap) yazılamaz: fiş farkı sıfır olmalıdır…" |
| 10 | Açık hesap olarak kaydet: gerçek cari nakit 6.000 + VERESİYE 4.000 | 200 + fisId | **200** | fisId var |
| 11 | FARK < 0 (12.000 ödeme) | 400 | **400** | "… bacaklar 12.000,00 TL, fiş 10.000,00 TL (fark 2.000,00 TL)" |
| 12 | GET /islemler/fis/{böl} | 200 + 2 bacak | 200 | NAKIT:6000, KART:4000 |
| 13 | GET /islemler/fis/{açık hesap} | 200 + 2 bacak | 200 | NAKIT:6000, VERESIYE:4000 |
| 14 | GET /cari/{id} (Tahsilat hedefi) | 200 | 200 | ad=S22 Müşteri |
| 15 | GET /islemler · /emanet · /cariler?cariId=x (kısayol hedefleri) | 200 | 200 ×3 | `<title>MİHENK` |

**SQL kanıtı** (`pS2-2/canli-sql.txt`):
```
hareketler (SATIS): NAKIT 10000 fişsiz · KARMA 10000 fişli · KARMA 10000 fişli
odeme_kalemleri: NAKIT 10000 (kasasız, tek satış) · NAKIT 6000 + KART 4000 · NAKIT 6000 + VERESIYE 4000 (kasasız)
```
Reddedilen 3 deneme (8, 9, 11) deftere yazılmadı: 3 SATIS hareketi ve 5 ödeme kalemi, başarılı 3 isteğe birebir karşılık geliyor.

**Sonrası:**
- API durduruldu (`kill 4186`). `lsof -iTCP:5192` boş.
- Kiracı `betikler/kiraci-temizle.sh --uygula` ile silindi: 70 tablo tarandı, 78 satır + 1 tenant silindi. Davet satırı da silindi.
- `tenants` başlangıçta 10'du, sonunda yine 10. `QA-TEST-*` ve `DEMO-Vitrin` kiracılarına dokunulmadı.
- API açılışında `is_calismalari` benzersizlik hatası loglandı (`MASAK_SIB_SURESI-2026-09-24` çift anahtar). Sebep: başka bir API örneği günün işini önceden yazmış. Bu paketle ilgisi yok.

**Dürüst kapsam:** Tarayıcıda gerçek ekran turu yapılmadı, çünkü giriş için parola gerekiyor ve bu ajan parola girmez. Ekran davranışı 26 S22 ve 235 POS bUnit testiyle sınandı. Kurucu 5199'da basit modda /islemler'i açarak ızgarayı ve VERDİM/ALDIM kartını gözle teyit edebilir.

## 10. Açık kalanlar

- **A-1 (eşzamanlılık, orkestratöre).** B-4/S2-4 "altın bacağı" şeridi bu paketle **aynı anda aynı dosyalarda** çalıştı: `Pages/Islemler.razor`, `FisOdemeKasa.razor`, `FisOdemeBacaklari.razor`, `FisModelleri.cs`, yeni `FisAltinBacagi.cs` (00:10–00:15). Değişiklikler ağaçta birlikte duruyor ve derleme yeşil. O şeridin yarım işinin bıraktığı kırmızı: `S21FisYapiTestleri.Fis_bilesenleri_ApiIstemci_enjekte_etmez_ve_istek_atmaz` (`FisAltinBacagi.AyarlariYukle(Api)`). **Sahibi: B-4/S2-4.** Ayrıca 1.400 satır bütçesi ortak: şu an 1.397, yani 3 satır pay kaldı.
- **A-2.** `P10CariNotEkranTestleri` ×2 ve `S32MusterilerEkranTestleri.S32_12_…` kırmızı. **Sahibi: S3-A** (`Cariler.razor` ArayuzModuServisi enjeksiyonu; testler servisi kaydetmiyor).
- **A-3.** denetim.sh "Pages: @onclick'siz ve disabled'sız <button>" 74 > 71. **Sahibi: S3-A/S3-B** (Cariler, Bildirimler, Kasalar). Bu paket bu sayacı 1 azalttı.
- **A-4 (ürün, kurucuya).** Tedarikçiye "Ödeme" ucu yok. Izgarada kilitli ve sebebini yazıyor. Uç açılırsa `BasitPos.Kisayollar`'da tek satır değişir.
- **A-5 (D3 kalanı).** ALTIN bacağı bu paketin kapsamı dışında (S2-4 / B-4). "Ödemeyi böl" ilk bacağa tanımadığı aracı (ALTIN/VERESİYE) NAKİT olarak, kasası boş taşıyor (fail-closed; kasa tekse otomatik atanıyor).
- **A-6 (görsel).** Izgara `.ekranlar-izgara` (8 sütun, dar ekranda 4/2) ve `.ekran-karo` sınıflarını Tüm Ekranlar sayfasıyla paylaşıyor. Tezgâha özel sınıf gerekirse `app.css` sahibine (S-1) iş düşer.
- **Mevzuat:** Bu paket hiçbir mevzuat hükmü yazmadı. Tevsik, MASAK ve Peşin Müşteri kapıları sunucuda aynen duruyor. Ekrana eşik sayısı girmedi (sözlük testi rakam dizisini yasaklıyor).

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Basit modda düğme ızgarası, ≥ 44 px, ikonlu, `#islem-tur-*` korunur | ✓ | `.ekran-karo` 68 px; `Basit_izgara_sekiz_buyuk_dugme…` |
| EMANET `/emanet` derin bağlantı | ✓ | test + canlı 200 |
| Pro modda 8 sekme AYNEN | ✓ | `Pro_modda_izgara_YOK_sekiz_sekme_AYNEN`, `Profesyonel_modda_sekiz_sekme_MEVCUT_DAVRANIS` |
| VERDİM/ALDIM aynı kartta iki sütun + FARK | ✓ | `Tek_odemede_VERDIM_ALDIM_tek_kartta…` |
| FARK 0 → Kaydet açık; ≠ 0 kilitli | ✓ | bUnit + canlı 8/11 → 400 |
| FARK > 0 + gerçek cari → "Açık hesap olarak kaydet" (VERESİYE yolu) | ✓ | bUnit + canlı 10 → 200 |
| Peşin Müşteri'de fark ≠ 0 kaydedilemez | ✓ | ekran kilidi + canlı 8/9 → 400 |
| İlk bakışta ≤ 3 alan + ≤ 12 düğme | ✓ | `Basit_satista_ilk_bakista_en_cok_UC_alan_ve_ON_IKI_dugme` |
| Canlı nakit + kart iki bacak 200 | ✓ | canlı 7 |
| FARK kuralı bozulunca test kırmızı | ✓ | M-S22-1, M-S22-2, M-S22-6 |
| TEK POST yolu; yeni bileşen ApiIstemci yok | ✓ | yapı testi; açık hesap `Kaydet()`'i çağırır |
| Mevcut id silinmedi | ✓ | § 3 |
| Özellik silinmez | ✓ | elle satırı katlı (DOM'da), kalem tablosu tam, pro aynen |
| Sabit renk / satır içi style / CDN yok | ✓ | yapı testi (5 dosya) |
| Sozlukler.cs'e dokunulmadı; ayrı sözlük TR + EN | ✓ | `TezgahFisiSozlugu.cs` |
| Eşik sayısı istemciye gömülmedi | ✓ | sözlük testi, B-3 sayacı ✓ |
| Dosya alanı | ✓ (+) | Islemler.razor, Bilesenler/Fis/*, BasitPos.cs; ek olarak `Kuyumcu.Core/I18n/TezgahFisiSozlugu.cs` (koşul gereği), testler |
| Paket kapısı 7 madde | ✓ | build 0/0 · 26/26 · sayaçlarım ✓ · 10/10 mutasyon · canlı · kiracı silindi · yeni kırmızı 0 |

## 12. Değişen dosyalar (bu paketin payı)
```
 M dotnet/Kuyumcu.Web/Pages/Islemler.razor
 M dotnet/Kuyumcu.Web/Servisler/BasitPos.cs              (izlenmeyen dosya, bu pakette değişti)
 M dotnet/Kuyumcu.Web/Bilesenler/Fis/FisKalemGirisi.razor (basit modda elle satırı katlı)
 A dotnet/Kuyumcu.Web/Bilesenler/Fis/FisIslemIzgarasi.razor
 A dotnet/Kuyumcu.Web/Bilesenler/Fis/FisVerdimAldim.razor
 A dotnet/Kuyumcu.Web/Bilesenler/Fis/FisIskontoSatiri.razor
 A dotnet/Kuyumcu.Web/Bilesenler/Fis/FisFark.cs
 A dotnet/Kuyumcu.Web/Bilesenler/Fis/FisTurleri.cs
 A dotnet/Kuyumcu.Core/I18n/TezgahFisiSozlugu.cs
 A dotnet/Kuyumcu.Tests/S22TezgahFisiTestleri.cs
 M dotnet/Kuyumcu.Tests/BasitModPosTestleri.cs            (1 test, § 6)
 A dokumanlar/09-uygulama-kayitlari/PAKET-S2-2-uygulama-raporu.md
```
- `Bilesenler/Fis/` ve `Servisler/BasitPos.cs` git'te izlenmediği için `git status` bunları `??` olarak gösteriyor.
- `FisAltinBacagi.cs` ile `FisOdemeKasa`/`FisOdemeBacaklari`/`FisModelleri` içindeki ALTIN değişiklikleri B-4/S2-4 şeridine ait.
- Commit yapılmadı.

Kanıt dosyaları: `scratchpad/pS2-2/` (`once/`, `taban-*`, `pos*.log`, `s22-*.log`, `mut-*.log`, `mutlar.log`, `build-son.log`, `tam.log`, `denetim.log`, `canli-*.txt`, `temizlik.txt`) ve `scratchpad/trx-pS2-2/`, `scratchpad/trx-pS2-2-taban/`.
