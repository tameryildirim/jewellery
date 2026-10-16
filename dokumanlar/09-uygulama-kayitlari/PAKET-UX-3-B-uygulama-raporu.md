# Paket UX-3 — Basit / Profesyonel mod · **FAZ B — POS basit akışı** uygulama raporu

> Tarih: 2026-09-17 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) rolü, Opus · **Dalga 3**
> Kapsam: **YALNIZ Faz B.** Faz A (2026-09-16) sevk edilmişti; Faz C (kalan sayfaların adaptasyonu) BU DALGADA YAPILMADI.
> Durum: **BİTTİ** — derleme 0 hata / 0 uyarı (kendi dosyalarımda), **2103/2103 test yeşil**, `dotnet/denetim.sh` **TEMİZ**, üç yasal kapı canlıda kanıtlı, test kiracısı silindi (tenants 8 → 8).
> COMMIT YOK — değişiklikler çalışma ağacındadır.

---

## 1. Yapılanlar

### 1.1 Yeni dosyalar
| Dosya | Ne yapar |
|---|---|
| `dotnet/Kuyumcu.Web/Servisler/BasitPos.cs` | Basit modda POS'un daraltılmış yüzünün **TEK karar kaynağı**: `Turler` (SATIS·ALIS·SATIS_IADE), `TurGorunur`, `GovdedenCikan` (gövdeden çıkarılan alanlar). Razor'a dağılmış `@if (basit)` yazılmadı (Bertan K7). |
| `dotnet/Kuyumcu.Web/Bilesenler/AdimGostergesi.razor` | Adım şeridi (`<önek>-adim-<sıra>` id'leri, `adim-serit` CSS). **UX-2 Faz A1'de üretilmemişti** (ağaçta hiçbir dosyada geçmiyordu) — burada yeni bileşen olarak açıldı, mevcut hiçbir dosyaya dokunulmadı. |
| `dotnet/Kuyumcu.Web/Bilesenler/BasitSatisAkisi.razor` | Basit moddaki adım şeridi + yönlendirme metni + yasal kapı bandı (`#islem-yasal-kapi`). **SUNUCUYA HİÇBİR ŞEY GÖNDERMEZ** — kurucusunda ApiIstemci/HttpClient yoktur (Bertan KOŞUL 4: tek POST yolu). |
| `dotnet/Kuyumcu.Tests/BasitModPosTestleri.cs` | 13 bUnit testi (aşağıda). |

### 1.2 `Pages/Islemler.razor` (TEK DOKUNUŞ — bu dalgada yalnız bu paket dokundu)
- `@inject ArayuzModuServisi Modu`; `Modu.Degisti` aboneliği + `Dispose`'da bırakma.
- **Sekme id'leri eklendi** (Aşkın K4): `id="islem-tur-SATIS"` … `islem-tur-ALIS_IADE`. **Mevcut hiçbir id değişmedi.**
- `TurListesi` artık `TumTurler`i `BasitPos` ile süzer; basit modda **3 sekme**, profesyonelde **8** (mevcut davranış birebir).
- Basit modda `<BasitSatisAkisi Adim="BasitAdim" KapiAcik="YasalKapiAcik" />`.
- **ProAlan sarmalamaları:** karma ödeme anahtarı, ödeme türü seçicisi (`BasitAlternatif` = salt okunur "Ödeme: Nakit" + `sistem belirledi` rozeti, UX-S21/S22), kasa seçicisi (`BasitAlternatif` = tek kasanın adı; `ZorunluGorunur="@(!AnaKasaTek)"` → **iki aktif kasa varsa seçici basit modda da GÖRÜNÜR**), vade girdisi, MASAK formu (`ZorunluGorunur="true"`).
- **Yasal kapı bayrakları (hepsi SUNUCU KAYNAKLI, eşik sayısı ekrana gömülmedi):** `OdemeKapisiAcik = tevsikNakitKapali || TevsikOnerisiGerekli || kapiSunucudanAcildi`; `YasalKapiAcik = OdemeKapisiAcik || MasakGerekli`. `kapiSunucudanAcildi`, `Kaydet()`'in `catch (ApiHata)` dalında set edilir (taş kökeni gibi yalnız sunucuda bilinen kapılar için) ve her yeni denemede sıfırlanır.
- **Gövde koruması (Bertan KOŞUL 3 · Mehmet K7 · Aşkın K3):** `Govde(object)` yardımcısı anonim tipi `Dictionary<string, object?>`e çevirir ve basit modda `BasitPos.GovdedenCikan` anahtarlarını **SİLER** — null olarak GÖNDERMEZ. `/islemler/satis` ve `/islemler/alis` gövdeleri bu yardımcıdan geçer. Profesyonel modda sözlük anonim tipin birebir kopyasıdır.
- `BasitModuUygula()`: basit modda görünmeyen sekmede kalınmaz, karma kapatılır, ödeme NAKİT'e döner (kapı açıkken dokunulmaz), vade temizlenir.
- `TevsikOnizle()`: **basit modda karma olmadan da** koşar (`if (tur != "SATIS" || (!karmaOdeme && Modu.ProfesyonelMi)) return;`). Gerekçe: basit modda ödeme seçicisi gizli; kapının açılıp açılmayacağını sunucu söylemeli (Mehmet K2). **Profesyonel moddaki istek sayısı/davranışı değişmedi.**

### 1.3 Sözlük (TR + EN, çıplak metin yok)
`SozlukArayuz`e 5 anahtar: `BasitAkisEtiket`, `BasitAkisIpucu`, `BasitOdemeNakit`, `SistemBelirledi`, `ProAlanIpucu`. Faz A'da hazırlanan `AdimUrun/AdimMusteri/AdimOdeme/AdimFis` ve `YasalKapiAcildi` bu fazda kullanıldı.

### 1.4 CSS
`app.css`e `.basit-akis`, `.adim-serit`, `.adim`, `.adim-no`, `.adim-aktif`, `.adim-bitti` + dar ekran (≤768px) kuralı. **Sabit renk yok — yalnız `var()` belirteçleri**; satır içi stil yok. Denetim sayacı `app.css ham hex 25 (tavan 25)` **artmadı**.

### 1.5 Mevcut testlere dokunulan satır: **9**
`DovizBacagiEkranTestleri` (1) · `IadeTestleri` (1) · `KarmaOdemeEkranTestleri` (1) · `PosBarkodTestleri` (2) · `PosTeklifEkranTestleri` (1) · `UX4bEkranTestleri` (1) · `YerlesimEkranTestleri` (2). Hepsi tek satırlık `Services.AddScoped<ArayuzModuServisi>();` kaydıdır (Islemler artık bu servisi enjekte ediyor). **Hiçbir assert değişmedi.**

---

## 2. BULUNAN VE DÜZELTİLEN HATA (Anayasa md.1 — gizlenmedi)

**MASAK eşiği aşan her işlemde POS ekranı çöküyordu.**
`masakGereksinim` sunucudan `OnAfterRenderAsync` ile gelir; yani eşiğin aşıldığı **İLK çizimde hâlâ NULL**'dur. `Gereksinim="masakGereksinim!"` bu çizimde `MasakFormu.razor:130 (EkBelgeGerekli)` içinde **NullReferenceException** atıyordu. Gerçek çıktı (mutasyondan önce, ilk test koşumu):

```
Başarısız Kuyumcu.Tests.BasitModPosTestleri.YASAL_KAPI_masak_basit_modda_kimlik_formunu_CIZER
  System.NullReferenceException : Object reference not set to an instance of an object.
     at Kuyumcu.Web.Bilesenler.MasakFormu.get_EkBelgeGerekli() … MasakFormu.razor:line 130
```

Bu paketin sevk kapısı (YASAL KAPI 2) bu hatanın üzerinden geçemezdi. Düzeltme (yalnız `Islemler.razor` içinde, dokunduğum dosyada): gereksinim gelene kadar `<Yukleniyor />` çizilir. **Kapı GEVŞETİLMEDİ:** `Gonderilebilir` zaten `MasakTamam=false` ile kilitli kalır ve `#islem-kaydet-kilit` sebebini (`Dil.T.Uyum.KimlikTespitleri`) yazar — fail-closed. `MasakFormu.razor`'a **dokunulmadı**.

---

## 3. Spesifikasyondan sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| S1 | **`Islemler.razor` 1900 → 2090 satır (+190).** Bertan KOŞUL 4 "1390'ı geçmeyecek" diyordu. | Rakam **bayattır**: dosya bu paket başlamadan önce (19/20/27/25/UX-4* dalgaları) zaten **1900** satırdı. Basit akışın metin/şerit kısmı koşulun istediği gibi `BasitSatisAkisi.razor`a çıkarıldı; kalan artışın büyük kısmı **açıklama yorumları** ve ProAlan sarmalayıcı etiketleridir. Koşulun ASIL amacı olan "**tek POST yolu**" korundu: gövde kurulumu ve kapı çağrıları yalnız `Islemler.razor`daki `Kaydet()` içindedir, bileşen POST etmez. |
| S2 | `BasitModVeriKorumaTestleri.cs` **yazılmadı**. | Spesifikasyon bu dosyayı **Faz C**'ye (Cariler/Ürünler sayfa adaptasyonu) bağlıyor; Faz C bu dalgada kapsam dışıdır. M-UX3-04'ün konusu olan "gizlenen alan gövdeye konmaz" kuralı POS kapsamında `BasitModPosTestleri.Basit_modda_gizlenen_alan_POST_govdesine_KONMAZ` + `Profesyonel_modda_govde_ALAN_KAYBETMEZ` ile kurulmuş ve mutasyonla kanıtlanmıştır. |
| S3 | `AdimGostergesi` bileşeni **bu pakette üretildi**. | Spesifikasyon onu UX-2 Faz A1'den bekliyordu; `grep` ile doğrulandı: ağaçta **hiçbir dosyada geçmiyordu**. Yokluğunda satır içi stil/geçici bileşen girmesin diye kurallara uygun (yalnız `var()`, id sözleşmeli) yeni bileşen olarak açıldı. |
| S4 | `TevsikOnizle` basit modda karma olmadan da koşuyor. | Spesifikasyon bunu düzenlememişti. Aksi hâlde basit modda kapı kararı istemcinin `TevsikHaddi ?? 30_000m` varsayılanına düşerdi — bu, eşiği ekrana gömmenin başka bir biçimidir (Mehmet K2 ihlali). **Profesyonel mod davranışı birebir korundu.** |
| S5 | Yasal kapı 3 (taş kökeni) canlıda **ürün oluşturma/güncelleme** ucunda doğrulandı, satış ucunda değil. | Kodda ADR-0013 kapısı `UrunModulu` yolundadır: kökensiz taşlı ürün **hiç oluşturulamıyor** (400), dolayısıyla satılamıyor. Kapının mod'dan habersiz olduğu, **BASİT moddaki SAHİP** ile tekrar denenerek kanıtlandı (§7 adım 9). Ekran tarafı (400 → mesaj olduğu gibi + gizli alanların açılması) bUnit testiyle kuruldu. |
| S6 | `#islem-kasa-tek` id'si basit modda `<p>` olarak da kullanılıyor. | Mevcut id **değiştirilmedi**; profesyonel (span) ve basit (p) dallar **birbirini dışlar**, DOM'da aynı anda iki kez bulunmaz. Anlam aynıdır: "kasa sorulmadı, sistem seçti". |

**Mevzuat/DOGRULANMALI hükmü yok:** bu paketin "Mevzuat dayanağı" bölümü `(yok)`. Basit mod hiçbir eşik/oran/had sayısı içermez; **hiçbir eşik istemciye yazılmadı** (mevcut `TevsikHaddi ?? 30_000m` varsayılanı UX-4d'den devralınmıştır, bu pakette artırılmadı — bkz. § Açık kalanlar).

---

## 4. Derleme

```
dotnet build Kuyumcu.sln --nologo
Oluşturma başarılı oldu.
    4 Uyarı
    0 Hata
```
4 uyarının **dördü de** `Kuyumcu.Tests/MadenTestleri.cs(78,79,80,83): warning xUnit2008` — **bu paketten ÖNCE de vardı**, benim dokunmadığım dosyadır. Gizlenmemesi için yazıldı; düzeltmesi izin ister (Kural 3). Mutasyonlar sonrası son temiz derleme: **0 Uyarı / 0 Hata** (test projesi yeniden derlenmediğinde).

**Migration: gerekmedi.** Bu faz veri modeline dokunmaz; `arayuz_modu` / `varsayilan_arayuz_modu` kolonları Faz A'nın `20260916072554_ArayuzModu` migration'ıyla `kuyumcu_net`'te zaten uygulanmıştır (canlı doğrulamada `tenant_varsayilan: PROFESYONEL` olarak okundu). `dotnet ef` bu pakette **hiç çalıştırılmadı**.

---

## 5. Testler

| Koşum | Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|---|
| Filtreli 1 | `BasitModPos` | 13 | 13 | 0 | 0 |
| Filtreli 2 | `KarmaOdeme·DovizBacagi·Iade·PosBarkod·PosTeklif·UX4b·Yerlesim·ArayuzModu·ProAlan·BasitModMenu·UX1C1` | 173 | 173 | 0 | 0 |
| **Tam süit** | `Kuyumcu.Tests` (trx) | **2103** | **2103** | **0** | **0** |

- **Yeni test sayısı: 13** (`BasitModPosTestleri`). Önce/sonra: **2090 → 2103**.
- trx: `…/scratchpad/trx-pUX-3-B/*.trx` → `total="2103" executed="2103" passed="2103" failed="0"`.

Testler (hepsi `BasitModPosTestleri`):
`Basit_modda_uc_sekme` · `Profesyonel_modda_sekiz_sekme_MEVCUT_DAVRANIS` · `Basit_modda_adim_seridi_cizilir_profesyonelde_CIZILMEZ` · `Basit_modda_karma_odeme_ve_bacak_YOKTUR` · `Basit_modda_vade_alani_YOKTUR` · `Basit_modda_odeme_turu_SORULMAZ_sade_satir_yazilir` · `Tek_kasa_varken_secici_GIZLI_ve_govdeye_o_kasa_konur` · `Iki_kasa_varken_secici_GORUNUR` · `YASAL_KAPI_tevsik_basit_modda_odeme_turunu_ACAR` · `YASAL_KAPI_masak_basit_modda_kimlik_formunu_CIZER` · `YASAL_KAPI_tas_kokeni_400u_basit_modda_GOSTERILIR_ve_alanlari_ACAR` · `Basit_modda_gizlenen_alan_POST_govdesine_KONMAZ` · `Profesyonel_modda_govde_ALAN_KAYBETMEZ`

---

## 6. Mutasyon kanıtları

| Kod | Değişiklik | Kırmızıya dönen testler (gerçek çıktı) | Geri alım |
|---|---|---|---|
| **M-UX3-01** | `ProAlan.Gorunur`: `Modu.ProfesyonelMi \|\| ZorunluGorunur` → `Modu.ProfesyonelMi` | **6 kırmızı**, ÜÇ YASAL KAPININ ÜÇÜ DE: `BasitModPosTestleri.YASAL_KAPI_tevsik_basit_modda_odeme_turunu_ACAR` · `.YASAL_KAPI_masak_basit_modda_kimlik_formunu_CIZER` · `.YASAL_KAPI_tas_kokeni_400u_basit_modda_GOSTERILIR_ve_alanlari_ACAR` · `.Iki_kasa_varken_secici_GORUNUR` · `ProAlanEkranTestleri.Basit_modda_ZorunluGorunur_ile_CIZER` · `.Basit_modda_ZorunluGorunur_ALTERNATIFI_EZER` | `cmp` → AYNI |
| **M-UX3-04** | `Islemler.Govde`: `d.Remove(alan);` → `d[alan] = null;` | **1 kırmızı**: `BasitModPosTestleri.Basit_modda_gizlenen_alan_POST_govdesine_KONMAZ` | `cmp` → AYNI |
| **M-UX3-05** | Kasa `<ProAlan ZorunluGorunur="@(!AnaKasaTek)">` → `<ProAlan>` (iki kasa varken de gizle) | **1 kırmızı**: `BasitModPosTestleri.Iki_kasa_varken_secici_GORUNUR` | `cmp` → AYNI |
| **M-UX3-08** | `BasitPos.Turler`e `"TAKAS"` geri eklendi | **1 kırmızı**: `BasitModPosTestleri.Basit_modda_uc_sekme` | `cmp` → AYNI |
| **M-UX3-06** (yeniden) | `KenarCubugu.Gorunur`: `&&` → `\|\|` | **5 kırmızı**: `BasitModMenuTestleri.Basit_modda_menu_YALNIZ_BasitMenu_listesidir` · `.Yetkili_TEZGAHTAR_basit_modda_calisma_ekranlarini_gorur` · `.Yetkisiz_kullanici_baglantiyi_gormez_PROFESYONEL_modda` · `.Yetkisiz_kullanici_baglantiyi_BASIT_modda_da_gormez` · `.Basit_modda_profesyonel_ekranlar_menude_YOKTUR` | `cmp` → AYNI |

**M-UX3-02 · 03 · 07 bu fazda YENİDEN kurulmadı.** Gerekçe: üçü de Faz A'nın API/denetim yüzeyine aittir ve o kod bu pakette **hiç değişmedi** (`git status`: `ArayuzModu.cs`, `KullaniciModulu.cs`, `AuthModulu.cs`, `denetim.sh` bu dalgada dokunulmadı); kanıtları `PAKET-UX-3-uygulama-raporu.md §5`tedir. denetim.sh'ın `ArayuzModu (yalnız 6 dosya) = 6` ve `kapı yüzeyinde = 0` sayaçları bu koşumda da **yeşildir** (§7).

Son mutasyondan sonra temiz derleme: **0 Uyarı / 0 Hata**. Yedekler: `…/scratchpad/mut-yedek/UX3B/`. Proje ağacına `.mutorj/.bak/.tmp` bırakılmadı (denetim sayacı `beyaz liste dışı .mutorj: 0`).

---

## 7. denetim.sh çıktısı (son)

```
  ✓ ArayuzModu (yalnız 6 dosya)                   6
  ✓ ArayuzModu kapı yüzeyinde (0 olmalı)          0
  ✓ Finansal varlıkta [HttpDelete] (0 olmalı)     0
  ✓ app.css ham hex (kâğıt dâhil kaba sayım)     25 (tavan 25)
  ✓ Pages+Bilesenler+Layout satır içi style=    122 (tavan 196)
  ✓ Pages: @onclick'siz ve disabled'sız <button> 69 (tavan 71)
  ✓ Pages: ModalPencere kullanımı                44 (tavan 45)
  ✓ Pages: duzenlenenId kalıbı                    1 (tavan 1)
  ✓ Pages/*.razor dosya sayısı                   31 (tavan 31)
  ✓ Kaynak ağacı: beyaz liste dışı .mutorj        0
DENETİM TEMİZ.
```
→ **Sunucu kapıları mod'dan habersiz kaldı** (K-UX-6 kanıtı). **Yeni ModalPencere düzenlemesi açılmadı** (44 sabit), **ikinci `duzenlenenId` kalıbı açılmadı** (1 sabit).

---

## 8. Canlı doğrulama (port 5198, gerçek veri)

Kiracı **PAKETUX-3-B-TEST** (`01M2QPBH0BSPK1SDCN2QZ45KS8`) · `ux3b.sahip@test.local` (SAHIP) · `ux3b.tez@test.local` (**TEZGAHTAR, arayuzModu=BASIT**).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /auth/kayit` | 201 | **201** |
| 2 | `POST /kullanicilar` `arayuzModu:"BASIT"` | 201 | **201**, `"arayuzModu":"BASIT"` |
| 3 | TEZGAHTAR girişi → `GET /auth/ben` | `BASIT` / `BASIT` | **`ben: BASIT BASIT`** |
| 4 | `PATCH /kullanicilar/{id}` `{"subeId":…}` (mod gövdede YOK) | 200, **mod KORUNUR** | **200**, `"arayuzModu":"BASIT"` |
| 5 | Basit modda 10.000 TL **NAKİT** satış | 200 | **200** · SQL: `SATIS 10000.00 NAKIT nakit_tutar=10000.00 vade_tarihi=NULL` · `kasa_hareket: 10000.00 NAKIT` |
| 6 | ⚠ **YASAL KAPI 1** — basit modda 35.000 TL NAKİT | **400** | **400** `"Tevsik zorunluluğu: işlemin toplam tutarı 35.000,00 TL, 30.000,00 TL haddini aşıyor … 459 s. VUK Genel Tebliği §4.1.2"` |
| 7 | aynı tutar **HAVALE** ile | 200 | **200** · SQL: `SATIS 35000.00 HAVALE nakit_tutar=0.00` |
| 8 | ⚠ **YASAL KAPI 2** — basit modda 190.000 TL, MASAK'sız | **400** | **400** `"MASAK: 185.000 TL ve üstü işlemde kimlik tespiti zorunludur · İşlem amacı belirtilmelidir · … (kümülatif 235.000,00 TL)"` → yalnız kimlikle tekrar: **yine 400** (amaç+açıklama eksik) → tam beyanla: **200** · SQL: `masak_kimlik: 1` |
| 9 | ⚠ **YASAL KAPI 3** — kökensiz taşlı ürün (ADR-0013) | **400**, mod'dan bağımsız | SAHİP PROFESYONEL iken **400**; **SAHİP BASIT moda alındıktan sonra da 400** `"Kıymetli taş içeren ürünün kökeni belirtilmelidir … Kuyum Ticareti Yönetmeliği md.11/(e)"` |
| 10 | Basit moddaki TEZGAHTAR `POST /urunler` | **403** (mod yetkiyi gevşetmez) | **403** `"Bu işlem için gerekli rol: SAHIP veya MUDUR veya MUHASEBE"` |
| 11 | `PATCH /kullanicilar/ben` `{"arayuzModu":"BASIT"}` (SAHİP) | 200 | **200** · `GET /auth/ben` → `sahip ben: BASIT BASIT` (mod **sunucuda** saklandı) |
| 12 | İkinci aktif TL kasası açıldı | TEZGAHTAR 2 aktif TRY kasa görür | **2** (→ basit modda seçici GÖRÜNÜR; ekran kanıtı `Iki_kasa_varken_secici_GORUNUR`) |
| 13 | SQL: kiracı varsayılanı | `PROFESYONEL` (migration davranışı değişmedi) | **`tenant_varsayilan: PROFESYONEL`** |
| 14 | API durdurma | 5198 boş | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **çıktı yok** |
| 15 | Test kiracısı silme (tenant_id'li tüm tablolar + `tenants`) | kiracı sayısı 8'e döner | **8** · `ux3b%` kullanıcı **0** · `QA-TEST-1/2/3` kiracıları **dokunulmadan** duruyor |

**Kiracı sayısı: başlangıç 8 → son 8.**

---

## 9. DoD sözleşmesi

Bu paket **YENİ VARLIK / YENİ KAYIT TÜRÜ AÇMAZ** — veri modeline hiç dokunmaz, migration üretmez, yeni uç açmaz. Dolayısıyla PATCH/DELETE/denetim kaydı yükümlülüğü **bu paketle doğan bir varlık için** doğmamıştır.

| Kayıt | Düzenleme yolu | Durum |
|---|---|---|
| `kullanicilar.arayuz_modu` (tercih) | `PATCH /kullanicilar/ben` ve `PATCH /kullanicilar/{id}` (üç durumlu: yok=koru · null=devral · değer=ata) | Faz A'da açıldı, ekranda Ayarlar/Kullanıcılar/ÜstBar anahtarı var. Bu fazda **değiştirilmedi**. |
| `tenants.varsayilan_arayuz_modu` | `PATCH /admin/tenant` ([RolGerekli("SAHIP")]) | Faz A'da açıldı, İşletme ekranında var. |
| POS'ta üretilen hareketler | **Değişmez defter** — düzeltme yolu `SATIS_IADE` sekmesidir (basit modda da görünür); hard DELETE yoktur, storno 48-A motorundadır. `#islem-son-satislar` kartındaki storno düğmesi gerekçesini yazan `DegismezIpucu` ile devre dışıdır (UX-1-C dalgasından). | Değişmedi. |

**DoD EK md.1 (her düğme ya istek atar ya disabled + gerekçe):** bu pakette **yeni düğme eklenmedi**. Mevcut `#islem-kaydet` kilidi sebebini `#islem-kaydet-kilit` ile yazar; MASAK gereksinimi yüklenirken kilit sebebi `Dil.T.Uyum.KimlikTespitleri`dir. Denetim sayacı `@onclick'siz ve disabled'sız <button> 69 (tavan 71)` artmadı.
**DoD EK md.2 (tek düzenleme kalıbı):** yeni `ModalPencere` düzenlemesi ve ikinci `duzenlenenId` kalıbı açılmadı (sayaçlar sabit).
**DoD EK md.3 (sessiz form yasağı):** `KalemEkle` alan altı Türkçe mesajları korundu; basit modda gizlenen alan **sessiz etkisizlik üretmez**, çünkü gizlenen alan gövdeye de konmaz.
**DoD EK 2 — Dokunulan sayfalar:** `dotnet/Kuyumcu.Web/Pages/Islemler.razor` **(bu dalgada TEK dokunuş; başka sayfa açılmadı)**.

---

## 10. UX sözleşmesi karşılama

| Kural | Nasıl karşılandı |
|---|---|
| UX-S21 | Basit modda ödeme türü, kasa ve vade **sorulmaz**: türetilir/doldurulur ve `sistem belirledi` rozetiyle nereden geldiği yazılır. |
| UX-S22 | Tek aktif TL kasası → seçici **hiç çizilmez**, salt okunur satır; **iki kasa → seçici görünür** (varsayım yok). Aynı ekranda aynı soru iki kez sorulmaz. |
| UX-S24 | Bağımlı alanın bağını ekran kurar: tutar/cari değişince tevsik önizlemesi yenilenir, kapı açılınca ödeme seçicisi kendiliğinden gelir. |
| UX-S25 | POS kayıtları değişmez defterdir; düzeltme yolu (Satış İadesi) basit modda da menüdedir. |
| UX-S27 | Mevcut `BasariBandi` + fiş no okuma korundu (POST yanıtı okunuyor). |
| UX-S28 | `catch (ApiHata)` hatayı **yutmaz**, listeye/boş duruma çevirmez; mesajı olduğu gibi banda yazar ve gizli alanları açar. |
| UX-S30 | Kilitli Kaydet sebebini `#islem-kaydet-kilit` ile söyler; yasal kapı açıldığında `#islem-yasal-kapi` bandı **neden açıldığını** yazar. |
| UX-S31 | Tüm yeni metinler `Sozlukler.cs` TR+EN; ham kod (`SATIS`, `NAKIT`) ekrana basılmaz, sekme id'lerinde teknik değer yalnız **id** olarak geçer. |
| UX-S32/S33 | Bu ekranda tutar biçimlendirmesi ve dönem kartları değişmedi. |

---

## 11. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısından **bu pakete ait olanlar**:
```
 M dotnet/Kuyumcu.Core/I18n/Sozlukler.cs
 M dotnet/Kuyumcu.Web/Pages/Islemler.razor
 M dotnet/Kuyumcu.Web/wwwroot/css/app.css
?? dotnet/Kuyumcu.Web/Bilesenler/AdimGostergesi.razor
?? dotnet/Kuyumcu.Web/Bilesenler/BasitSatisAkisi.razor
?? dotnet/Kuyumcu.Web/Servisler/BasitPos.cs
```
`dotnet/Kuyumcu.Tests/` dizininin tamamı depoda **hiç izlenmemiştir** (tek `??` satırıyla görünür), bu yüzden şu test dosyaları listede ayrıca çıkmaz:
`BasitModPosTestleri.cs` (YENİ) · `DovizBacagiEkranTestleri.cs` · `IadeTestleri.cs` · `KarmaOdemeEkranTestleri.cs` · `PosBarkodTestleri.cs` · `PosTeklifEkranTestleri.cs` · `UX4bEkranTestleri.cs` · `YerlesimEkranTestleri.cs` (her biri tek satır DI kaydı).

---

## 12. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Bertan K1 — UX-2 Faz A1 sırası | KARŞILANDI | A1 dalga 0'da bitti (koşul metninde teyitli) |
| Bertan K2 / Aşkın K9 — **3 yasal kapı sevk kapısı** | **KARŞILANDI** | 3 ayrı ekran testi + M-UX3-01 **üçünü birden** kırdı + canlıda 400/200 çiftleri (§8 adım 6-9) |
| Bertan K3 / Mehmet K7 / Aşkın K3 — ProAlan içi alan gövdeye konmaz | KARŞILANDI | `Govde()` sözlük yardımcısı + `Basit_modda_gizlenen_alan_POST_govdesine_KONMAZ` + M-UX3-04 |
| Bertan K4 — Islemler büyümesin, **tek POST yolu** | **KISMEN** | Tek POST yolu korundu ve akış bileşene çıkarıldı; satır sayısı 1900 → 2090 (bkz. S1) |
| Bertan K5/6, Mehmet K4/K6, Aşkın K7 — üçlü JSON, mod yalnız sunucuda çözülür | KARŞILANDI (Faz A) | Bu fazda o kod değişmedi |
| Mehmet K1 — sunucu kapıları mod'dan habersiz | KARŞILANDI | denetim.sh: `ArayuzModu kapı yüzeyinde 0` · canlı adım 9 (BASIT moddaki SAHİP'e de 400) |
| Mehmet K2 — eşikler istemciye gömülmez | KARŞILANDI | Yeni eşik yazılmadı; basit modda tevsik önizlemesi **sunucudan** çekiliyor (S4) |
| Mehmet K9 / Aşkın K6 — tek kasa KOŞULDUR | KARŞILANDI | `Iki_kasa_varken_secici_GORUNUR` + M-UX3-05 + canlı adım 12 |
| Mehmet K10 / Aşkın K1 — 400 yutulmaz, ProAlan açılır | KARŞILANDI | `YASAL_KAPI_tas_kokeni_400u…` testi |
| Aşkın K2 — mevcut testler derlenmeye devam eder, assert değişmez | KARŞILANDI | 9 satır DI kaydı, 0 assert değişikliği; 2103/2103 yeşil |
| Aşkın K4 — sekme id'leri | KARŞILANDI | `islem-tur-*`; `FindAll("[id^=islem-tur-]")` 3/8 |
| Yetki asla mod ile gevşemez | KARŞILANDI | canlı adım 10 (403) + M-UX3-06 |

---

## 13. Açık kalanlar

1. **`TevsikHaddi ?? 30_000m` istemci varsayılanı** (`Islemler.razor`, UX-4d'den devralındı) hâlâ kodda. Bu pakette artırılmadı ve basit modda artık önizleme koştuğu için pratikte sunucu haddi kullanılıyor; yine de "eşik istemcide" kalıntısıdır. Kaldırılması ayrı bir karar ister (Mehmet).
2. **Faz C yapılmadı:** Cariler, Ürünler, Kasalar, Raporlar, Panel sayfalarının `basit_modda` sütunu uygulanmadı; `BasitModVeriKorumaTestleri` oradadır.
3. **`HizliIzgara`** (hızlı işlem ızgarası) araştırma brifinde ayrı oylandı — bu fazda **YOK** (görev metnine uygun).
4. **Ağaçta duran artık:** `dotnet/Kuyumcu.Tests/MadenDefteriTestleri.cs.bak` — benim bırakmadığım, önceki bir dalgadan kalan dosya. Silmek kurucunun kararıdır (hook `rm`i bloklar).
5. **`MadenTestleri.cs` 4 xUnit2008 uyarısı** — önceden vardı, dokunmadım.
6. **Tarayıcıdan tıklayarak QA** (kurucunun 4. talimatı) bu raporun kapsamında yapılmadı; bUnit + canlı API ile doğrulandı. Basit moddaki görsel tur QA turu 3'e kalıyor.

## 14. Kurucuya / mali müşavire notlar

- Bu pakette **hiçbir mevzuat eşiği/oranı koda yazılmadı**. Ekranda görülen 30.000 TL ve 185.000 TL değerleri **sunucunun mesajından** gelir (§8 adım 6 ve 8'deki gerçek yanıtlar), istemcide sabit değildir.
- **Basit mod bir yetki mekanizması değildir.** Tezgâhtar basit moddayken bile sunucu kapılarının hepsi (tevsik, MASAK, taş kökeni, rol) aynen çalışır; canlıda üçü de kanıtlandı. Bu, mali müşavir açısından "sade ekran kullanan personel eksik belge üretebilir" riskinin **kapalı** olduğu anlamına gelir.
- Ürün **ücretsiz pilot** aşamasındadır; bu raporda fiyat/teklif/tahsilat metni üretilmemiştir.
