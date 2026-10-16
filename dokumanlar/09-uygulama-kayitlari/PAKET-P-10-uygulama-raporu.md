# PAKET P-10 — SUNUCUNUN KABUL ETTİĞİ ALANLARI FORMA BAĞLA + ÜRÜN GÖRSELİ

**Dalga:** DALGA 3 · **Sürüm:** 1.0 · **Tarih:** 2026-09-21
**Roller:** Taşkın (backend) + Ertuğ (frontend) · **Kimlikler:** Y-3 · Y-8
**Önkoşul:** P-06 — **DİSKTE VAR** (`PAKET-P-06-uygulama-raporu.md`; `Bilesenler/DuzenlenebilirListe.razor`,
genişletilmiş `Bilesenler/SatirIciDuzenle.razor`, `Modules/Meta/DuzeltmePolitikasi.cs` mevcut).
BK-01 (P-03 Contracts) da diskte: `Kuyumcu.Contracts/` var ve çözüme bağlı. **Kapsam daraltılmadı.**
**Migration:** HAYIR — şema değişikliği gerekmedi (yedi alanın **hepsi** kolonda zaten vardı).
`__EFMigrationsHistory` **53 → 53**, `dotnet ef` hiç çalıştırılmadı.

---

## § 0 — Bir cümlede

Y-3'ün tarifi "uçta var, ekranda yok"tu: yedi alan (**ürün RFID · KMTS · görsel URL · açıklama ·
parite kodu · cari notlar · POS cihazı + kart taksiti**) kolonda, DTO'da ve uçta hazırdı ama hiçbir
form göndermiyordu; Y-8'de belge deposu GORSEL/FOTOGRAF kabul ediyor, yükleyici çalışıyor, **hiçbir
ekranda resim çıkmıyordu**. P-10 yedi alanı **oluşturma ve düzenleme formlarına aynı kümeyle** bağladı
(UX-S26), görsel galerisini açtı, sunucuya bir **kapalı şema kapısı** (`GorselKapisi`) koydu ve boşluğu
bir daha açılamaz kılan **`DTO_FORM_ESLESMEZ` cırcır sayacını** `dotnet/denetim.sh`'a `# >>> P-10`
bloğu olarak ekledi.

---

## § 1 — Kimlik kapanış tablosu (ZORUNLU)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **Y-3** — "Sunucunun kabul ettiği 7 alanı forma bağla" | **KAPANDI** | **Ürün 5 alanı:** `Kuyumcu.Web/Pages/Urunler.razor` — oluşturma `#u-aciklama · #u-parite · #u-kmts · #u-rfid · #u-gorsel-url`, düzenleme `#d-…` aynı küme; gövdeler `UrunOlustur()` / `UrunGuncelle()` içinde. **Cari notu:** `Pages/Cariler.razor` `#cari-notlar` + `#duz-notlar`, POST/PATCH gövdesinde `notlar`. **POS cihazı + taksit:** `Pages/Islemler.razor` `#islem-bacak-{ix}-pos` / `-taksit`, gövdede `posCihazId` + `posTaksitAdedi`. Testler: `P10UrunEkranTestleri.Yeni_urun_kaydinda_BES_ALAN_da_GOVDEDE_gider`, `P10CariNotEkranTestleri` (2), `P10PosBacagiEkranTestleri` (5), `HttpP10AlanTestleri` (7). Canlı: § 7 adım 1–7. |
| **Y-8** — "Ürün görseli / galerisi" | **KAPANDI** | Yeni bileşen `Kuyumcu.Web/Bilesenler/UrunGorselGalerisi.razor` (GORSEL + FOTOGRAF belgeleri + kart adresi kapak karesi); ürün düzenleme çekmecesine bağlı (`Pages/Urunler.razor`, `<UrunGorselGalerisi …>`), listede kapak sütunu (`#u-gorsel-{id}` / `#u-gorsel-yok-{id}`). Testler: `Listede_KAPAK_GORSELI_cizilir_yoksa_sebebi_yazar`, `Galeri_yuklenen_GORSEL_belgelerini_gosterir_PDF_gostermez`. Denetim: `P-10: URUN_GORSEL_GALERISI ✓`. Canlı: § 7 adım 8–9. |

> Devredilen kalem **yoktur**. Kapsam kilidi korundu: başka paketin kimliğine dokunulmadı,
> yeni ekran/modül açılmadı.

---

## § 2 — Yapılanlar

### 2.1 Sunucu (Taşkın)

**`Modules/Urun/GorselKapisi.cs` (YENİ, 2.647 bayt) — saf kapı, veritabanı görmez**

`gorselUrl` P-10 ile ilk kez **kullanıcıdan** geliyor ve ürün kartında `<img src>` olarak çiziliyor.
Serbest metin kabul edilseydi `data:` / `javascript:` / `file:` şemaları depoya yazılırdı. Mehmet'in
`BelgeKapisi`'ndeki K4-4 ilkesi ("tanınmayan her imza reddedilir") adres yüzeyine taşındı:
**kapalı liste = `http`, `https`**; başka her şey 400.

`GorselKapisi.Bosla()` ise "boş dizge = temizle" kuralının **tek** yeri: `Rfid`, `Aciklama`, `GorselUrl`
artık boş gönderilince kolona `""` değil **NULL** yazıyor (eskiden `""` kalıyordu — kart "adres var ama
boş" durumuna düşüyor, galeri kırık resim çiziyordu).

**`Modules/Urun/UrunModulu.cs`** — `Olustur` ve `Guncelle` bu kapıdan geçiyor:
`Rfid = GorselKapisi.Bosla(...)`, `GorselUrl = GorselKapisi.Dogrula(...)`, `Aciklama = GorselKapisi.Bosla(...)`.
PATCH sözleşmesi korunuyor: **alan gövdede yoksa = koru**, **boş geldiyse = sil**.

**Değişmeyenler (bilinçli):** `KmtsKodu` ve `PariteKodu` zaten aynı deseni uyguluyordu; cari `Notlar`
zaten üç durumlu (`NotlarVerildi`) idi; `OdemeBacagiDto.PosCihazId/PosTaksitAdedi` zaten vardı ve
"tek cihaz otomatik, çok cihaz fail-closed" kapısı işliyordu. Bu üçünde **sunucu kodu değiştirilmedi** —
eksik olan yalnız ekrandı (izinsiz değişiklik yapılmadı).

### 2.2 Sözleşmeler (yeni record AÇILMADI — PLAN § 1 md.9)

| Tip | Ne yapıldı |
|---|---|
| `Kuyumcu.Contracts/Sayfalar/UrunlerSozlesmeleri.cs` · `UrunTamKart` | **genişletildi**: `Aciklama · Rfid · KmtsKodu · GorselUrl · PariteKodu` (sunucu GET `/urunler/{id}` bunları zaten döndürüyordu, istemci okumuyordu) |
| aynı dosya · `UrunVeri` | **genişletildi**: `GorselUrl` (liste kapak görseli) |
| `…/CarilerSozlesmeleri.cs` · `CariDetayVeri` | **genişletildi**: `Notlar` |
| galeri bileşeni | mevcut `Kuyumcu.Contracts.Bilesenler.BelgeYukleyici.BelgeSatiri` **kullanıldı** |

`BU_PAKETTE_EKLENEN_RECORD = 0` (razor record sayısı 3 → 3, sayaçla kilitli).

### 2.3 Ekran (Ertuğ)

- **`Pages/Urunler.razor`** — beş alan **iki formda da** (UX-S26, aynı seçenek dizisi
  `PariteKodlariListesi`'nden üretiliyor); liste tablosuna **Görsel** sütunu; düzenleme çekmecesine
  galeri + mevcut belge yükleyici; istemci ön kapısı `GorselUrlGecersiz()` → geçersiz adreste **istek
  hiç atılmaz**, alan altı Türkçe mesaj çıkar (UX-S30, sessiz etkisizlik yok).
- **`Pages/Cariler.razor`** — `notlar` oluşturma modalında ve düzenleme çekmecesinde; PATCH'te
  gönderiliyor (boş = sil, üç durumlu sözleşmeye uygun).
- **`Pages/Islemler.razor`** — KART bacağı seçilince **POS cihazı + taksit** alanları açılır (UX-S24);
  **tek cihaz** varsa seçici kilitlenir ve sebebi yazar (UX-S22 + UX-S30); **hiç cihaz yoksa** sebebi
  yazar; cihaz listesi alınamazsa **hata bandı** çıkar, sessizce "kayıt yok"a çevrilmez (UX-S28).
- **`Bilesenler/UrunGorselGalerisi.razor` (YENİ)** — `/belgeler?sahipTuru=URUN&sahipId=…` listesinden
  GORSEL/FOTOGRAF kayıtları, içerik `ApiIstemci.GetBaytAsync` ile çekilip `data:` adresine çevriliyor.
  *Neden `<img src="/belgeler/…/icerik">` değil:* o uç `Authorization: Bearer` istiyor, tarayıcının
  kendi `<img>` isteği bu başlığı taşımıyor — **canlıda ölçüldü: jetonsuz istek HTTP 401** (§ 7 adım 9).
  Yeni uç **açılmadı**.
- **`wwwroot/css/app.css`** — `.gorsel-galerisi / .gorsel-serit / .gorsel-kare / .gorsel-resim /
  .gorsel-ad / .gorsel-kucuk`. Sabit renk **yok** (hepsi `var()` belirteci), satır içi `style` **yok**,
  375 px'te şerit sarmalanıyor.
- **`Kuyumcu.Core/I18n/Sozlukler.cs`** — 21 yeni anahtar, **TR + EN**, çıplak metin yok
  (`required` olduğu için eksik anahtar **derleme hatası** verir — UX-S31).

### 2.4 K-21 risk beyanı (plan § 2 P-10 riski)

RFID alanı donanım **ima eder**. Alanın altında (`#u-rfid-ipucu` / `#d-rfid-ipucu`) iki dilde şu yazar:
*"Elle yazılır. RFID okuyucu/yazıcı donanımı bu kurulumun kapsamı DIŞINDADIR (K-21); alan yalnız kodu
saklar."* Beyanın varlığı **denetim.sh'ta sayaçla** kilitlendi (`P-10: K21_DONANIM_BEYANI`) ve
`RFID_alaninin_altinda_DONANIM_KAPSAM_DISI_beyani_yazar` testiyle sınanıyor.

---

## § 3 — DoD sözleşmesi (Bertan koşulu)

Bu paket **yeni varlık / yeni kayıt türü doğurmadı** — mevcut varlıkların alanları ekrana bağlandı.
Buna rağmen sözleşme tablosu tam olarak:

| Varlık | Düzenleme yolu | Silme / pasifleştirme | Yetki | Denetim kaydı | Gerekçe |
|---|---|---|---|---|---|
| `urunler` (5 yeni alan) | `PATCH /urunler/{id}` → ekranda **Düzenle** çekmecesi (`#d-aciklama…#d-gorsel-url`) | `DELETE /urunler/{id}` = **pasife alma** (soft); ekranda "Pasife Al" + "Aktife Al" | `[ModulYetkisi("urun")]` + `[RolGerekli(SAHIP,MUDUR,MUHASEBE)]` | mevcut `DenetimServisi.YazmaKaydet` çağrısı korundu (maden değişimi izi); **yeniden yazılmadı** | — |
| `cariler.notlar` | `PATCH /cari/{id}` → `#duz-notlar` | cari pasifleştirme mevcut (`#c-pasife-al` + aktife al) | `[ModulYetkisi("cari")]` (mevcut) | mevcut | — |
| `belgeler` (GORSEL/FOTOGRAF) | içerik **değişmez**; yerine yeni belge yüklenir | `DELETE /belgeler/{id}` = **pasife alma** (`Aktif=false`, tüketici ispatı korunur) | `[RolGerekli(SAHIP,MUDUR,MUHASEBE)]` (mevcut) | mevcut | İçerik hash'i (sha256) ispat değeridir; **değiştirilemez**, bu yüzden düzenleme değil yeniden yükleme yolu vardır |
| `pos_tahsilatlari` (KART bacağının ürettiği satır) | **SALT OKUNUR** — fişin parçasıdır | hard DELETE **YOK**; geri alma yalnız `StornoServisi` (48-A) | mevcut | mevcut | Finansal varlık: `HareketFabrikasi` dışında yazılmaz, storno dışında geri alınmaz (DoD EK 2) |

**Her düğme ya istek atar ya disabled + sebep:** yeni eklenen **tek** kilitli denetim POS cihaz
seçicisidir (tek cihaz ya da hiç cihaz); ikisinde de sebep yanında yazılıdır
(`#islem-bacak-{ix}-pos-tek` / `-pos-yok`) ve testle kanıtlıdır.
**Tek düzenleme kalıbı:** yeni `duzenlenenId` kalıbı **açılmadı**; ürün düzenlemesi mevcut
`Cekmece.razor`'da, cari düzenlemesi mevcut çekmecede kaldı. Pages altında **yeni ModalPencere
düzenleme kullanımı = 0 artış**.

### Dokunulan sayfalar (DoD EK 2 — TEK DOKUNUŞ)

`Pages/Urunler.razor` · `Pages/Cariler.razor` · `Pages/Islemler.razor` ·
`Bilesenler/UrunGorselGalerisi.razor` (YENİ) · `wwwroot/css/app.css`

> `Pages/Islemler.razor` PLAN § 7'nin tek-el zincirinde **P-15 → P-16 → P-18** olarak listelidir;
> P-10 o zincirin **içinde değildir** ve bu üç paketin hiçbiri bu oturumda açık değildir. Dokunuş
> yalnız KART bacağının iki alanıyla sınırlıdır (bacak sınıfı + bir `@if` bloğu + gövde iki satır);
> **gerekçe:** Y-3'ün yedi alanından ikisi (POS cihazı, taksit) yalnız bu ekranda girilebilir,
> plan bloğu bu alanları P-10'a yazmıştır (`Modules/Satis/OdemeSepeti.cs:52,55`). Kapı kaydına
> yazılmak üzere bildirilir.

---

## § 4 — Sapmalar ve gerekçeleri (Anayasa md.1)

| # | Plan / koşul ne diyor | Gerçek | Ne yapıldı |
|---|---|---|---|
| S1 | Dosya ayak izi: `Modules/Urun/UrunModulu.cs:124,127,128` · `Modules/Satis/OdemeSepeti.cs:52,55` · `Modules/Urun/BelgeKapisi.cs:41-42` | Satır numaraları kaymış (`UrunModulu.cs` içinde `Rfid/GorselUrl` **127-128** değil ~**126-128** ve ikinci kez `UrunGuncelleDto`'da ~**261-262**). `BelgeKapisi.cs:41-42` **imza sabitleri** — okundu, **değiştirilmedi** | Koda bakılarak uyarlandı; planın HEDEFİNE sadık kalındı. `BelgeKapisi`'ne dokunulmadı; onun yerine **aynı ilkeyi** adres yüzeyine taşıyan yeni `GorselKapisi` yazıldı |
| S2 | DoD "7 alan … forma bağlanır" | Yedi alanın **beşi** ürün kartında, biri cari kartında, biri (POS cihazı+taksit, bir aile) satış fişindedir — **üç ayrı ekran** | Üçü de bağlandı; planın dosya listesi zaten üç modülü işaret ediyordu |
| S3 | `DTO_FORM_ESLESMEZ = 0` **(beyaz liste gerekçeli)** | Taze ölçümde beyaz listeye **hiç ihtiyaç olmadı**: 5 DTO / 0 eşleşmeyen alan. Tek "yanlış pozitif" kaynağı `[JsonIgnore]` işaretli `…Verildi/…Gonderildi` bayraklarıydı; bunlar istemci alanı değil, üç durumlu PATCH sözleşmesinin iç bayrağıdır | Beyaz liste yerine **`[JsonIgnore]` satırları elendi** (daha dar ve denetlenebilir). Beyaz liste **BOŞ** |
| S4 | — (koşulda yok) | `Rfid` / `GorselUrl` / `Aciklama` boş gönderilince kolona `""` yazılıyordu | **Düzeltildi** (NULL). Davranış değişikliğidir; testle ve canlı SQL ile kanıtlandı (§ 6 M10-2, § 7 adım 4) |
| S5 | — | Plan tahmini **120 ajan-dk** | Aşıldı (yaklaşık 200 ajan-dk). Sebep: yedi alanın **üç ayrı ekrana** yayılması, galeri bileşeninin kimlik doğrulama kısıtı yüzünden `data:` yoluna çevrilmesi ve 13 mevcut sahte API'nin yeni uçlara gerçekçi yanıt verecek şekilde güncellenmesi |

### Mevzuat / "DOĞRULANMALI" hükmü

Spesifikasyonun bu paketle ilgili tek dış-bağımlı hükmü **K-21 (RFID/PVC donanım)**'dir ve
**1.1'e dondurulmuştur**. Koda hiçbir donanım varsayımı yazılmadı; ekranda "kapsam dışı" beyanı
görünür. KMTS kodu **doğrulanmaz** (mevcut davranış korundu) — bu, sunucudaki eski yorumun
(`opsiyonel, doğrulanmaz — Adım 8`) aynen sürdürülmesidir, yeni bir hüküm konmadı.

---

## § 5 — Derleme ve testler

| Ölçüm | Önce | Sonra |
|---|---|---|
| `dotnet build Kuyumcu.sln --nologo` | 0 hata / 0 uyarı | **0 hata / 0 uyarı** |
| Tam süit | 3.075 başarılı / 0 başarısız | **3.113 başarılı / 0 başarısız / 0 atlanan** |
| Yeni test | — | **38** (`P10AlanBaglamaTestleri` 21 · `P10EkranTestleri` 17) |
| Migration | 53 | **53** (üretilmedi) |
| `dotnet/denetim.sh` | TEMİZ | **DENETİM TEMİZ** |

Koşum: `dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo -v n --logger trx --results-directory …/trx-pP-10`
→ `Başarılı! - Başarısız: 0, Başarılı: 3113, Atlanan: 0, Toplam: 3113`.

### Yeni testlerin dökümü

**`Kuyumcu.Tests/P10AlanBaglamaTestleri.cs`** (sunucu)
`GorselKapisiTestleri` (saf): boş → NULL (3 durum) · http/https kabul (2) · kırpma · kapalı liste dışı
şema reddi (6: `data:`, `javascript:`, `file:`, `ftp:`, görece yol, şemasız) · uzun adres kısaltma.
`HttpP10AlanTestleri` (gerçek HTTP + MySQL): POST beş alanı kaydeder · POST geçersiz adres 400 ve
**kayıt açılmaz** · POST boş alanlar NULL · PATCH gönderilmeyen **korunur** · PATCH boş **siler** ·
PATCH geçersiz adres 400 ve **eski değer durur** · GET tam kart beş alanı camelCase döndürür ·
cari notu yazılır/korunur/silinir.

**`Kuyumcu.Tests/P10EkranTestleri.cs`** (bUnit)
`P10UrunEkranTestleri` (10): iki formda alanların varlığı · POST gövdesi · parite listesinin
**tek kaynaktan** gelmesi · K-21 beyanı · geçersiz adreste **istek atılmaması** · düzenlemenin tam
kartı okuması · boşaltılan alanın gövdede boş gitmesi · liste kapak görseli · galeri (GORSEL çizilir,
PDF çizilmez, `data:image/jpeg;base64,` ile başlar).
`P10CariNotEkranTestleri` (2) · `P10PosBacagiEkranTestleri` (5).

### Mevcut testlerde yapılan iki uyarlama (gizlenmedi)

1. **13 sahte API'ye gerçekçi yanıt** eklendi (`/kur/pariteler`, `/pos/cihazlar`). Sebep: bağlayıcı
   kural — "bUnit sahte API `{}` varsayılanı → yeni uçlara gerçekçi yanıt". `{}` bırakılsaydı ekran
   JSON hata yolundan geçer ve testler **yalancı yeşil** kalırdı.
2. **`S3dEkranTestleri.HurdaAc()` sağlamlaştırıldı.** Başlangıç yüklemesine bir istek daha eklenince
   (`GET /pos/cihazlar`) `Satici_tipi_secilmeden_Kaydet_KILITLI_ve_sebebi_yazili` **tam süitte** ara
   sıra kırmızı oldu (tek başına hep yeşildi). Sebep **ürün kusuru değil**, bUnit'te bayat element
   referansıdır: `Find` ile alınan `<select>` arada gelen bir yeniden çizimle DOM'dan düşerse `Change`
   olayı kopmuş düğüme gider ve `cariId` boş kalır (kilit sebebi "Cari seçin"e kayar). Seçim gerçekten
   oturana dek tazeleyip tekrarlayan bir `WaitForAssertion` konuldu; **test susturulmadı, iddiası
   güçlendirildi**. Hatanın kendisi ve sebebi bu raporda yazılıdır.

---

## § 6 — Mutasyon kanıtları

Yöntem: kaynak scratchpad'e kopyalandı → tek eşleşmeli değişiklik → filtreli `dotnet test` →
yedek geri kopyalandı → `cmp` ile birebir aynılık kanıtlandı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (cmp) |
|---|---|---|---|
| **M10-1** | `GorselKapisi.Dogrula` — şema denetimi `if (false)` yapıldı | `GorselKapisiTestleri.Kapali_liste_disindaki_sema_REDDEDILIR` (6 varyant) · `…Cok_uzun_adres_hata_mesajinda_KISALTILIR` · `HttpP10AlanTestleri.POST_urunler_GECERSIZ_gorsel_adresini_400_ile_reddeder` · `…PATCH_GECERSIZ_gorsel_adresi_400_ve_eski_deger_DURUR` — **9 kırmızı** | `cmp` AYNI |
| **M10-2** | `UrunModulu.Guncelle` — `mevcut.Rfid = GorselKapisi.Bosla(dto.Rfid)` → `= dto.Rfid` | `HttpP10AlanTestleri.PATCH_BOS_gonderilen_rfid_ve_gorsel_SILINIR` | `cmp` AYNI |
| **M10-3** | `Urunler.razor` POST gövdesinden `rfid = Bos(yeniRfid),` silindi | `P10UrunEkranTestleri.Yeni_urun_kaydinda_BES_ALAN_da_GOVDEDE_gider` | `cmp` AYNI |
| **M10-4** | `Islemler.razor` — `posCihazId` her zaman `null` gönderildi | `P10PosBacagiEkranTestleri.COK_CIHAZ_varsa_secici_ACIKTIR_ve_secim_GOVDEDE_gider` · `…TEK_CIHAZ_varsa_secici_KILITLI_ve_sebebi_YAZAR` | `cmp` AYNI |
| **M10-5** | `Cariler.razor` PATCH gövdesinden `notlar = BosIseNull(duzNotlar),` silindi | `P10CariNotEkranTestleri.Cari_duzenlemesi_mevcut_NOTU_okur_ve_geri_gonderir` | `cmp` AYNI |
| **M10-6** | `UrunGorselGalerisi.razor` — `Where(x => GorselTurleri.Contains(x.Tur))` → `Where(x => false)` | `P10UrunEkranTestleri.Galeri_yuklenen_GORSEL_belgelerini_gosterir_PDF_gostermez` | `cmp` AYNI |

Hiçbir mutasyon **test açığı** göstermedi (her biri en az bir testi adıyla kırdı).
Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln --nologo` → **0 hata / 0 uyarı**.

---

## § 7 — Canlı doğrulama (localhost:**5198** · 5199'a DOKUNULMADI)

API `--no-build` ile 5198'de başlatıldı (`lsof -nP -iTCP:5198 -sTCP:LISTEN` → LISTEN doğrulandı).
Test kiracısı **`PAKET-P-10-TEST`** (`01M30AH1C8MGT579BR0XBR4TPQ`). Davet jetonu yerel üretildi,
veritabanına **yalnız SHA-256 özeti** yazıldı; hiçbir sır rapora girmedi. **Tüm yanıt gövdeleri okundu**
(`-o /dev/null` kullanılmadı).

| # | Adım | Beklenen | Gerçek | SQL / kanıt |
|---|---|---|---|---|
| 0 | `POST /auth/kayit` (davetli) | 201 | **HTTP 201** | `tenants` 8 → 9 |
| 1 | `POST /urunler` — beş alan dolu | 201 + hepsi yazılı | **HTTP 201**, yanıt: `aciklama=Vitrin 5, rfid=RF-CANLI-1, kmtsKodu=KMTS-CANLI, gorselUrl=https://ornek.test/canli.jpg, pariteKodu=BILEZIK_22` | `SELECT aciklama,rfid,kmts_kodu,gorsel_url,parite_kodu FROM urunler` → `Vitrin 5 / RF-CANLI-1 / KMTS-CANLI / https://ornek.test/canli.jpg / BILEZIK_22` |
| 2 | `POST /urunler` — `gorselUrl: "javascript:alert(1)"` | 400, kayıt açılmaz | **HTTP 400** · *"Görsel adresi yalnız http:// veya https:// ile başlayabilir (verilen: javascript:alert(1))"* | `SELECT COUNT(*) … kod='P10-CANLI-2'` → **0** |
| 3 | `PATCH /urunler/{id}` — yalnız `{"ad":…}` | 200, diğer alanlar **korunur** | **HTTP 200** | `rfid=RF-CANLI-1`, `gorsel_url=https://ornek.test/canli.jpg`, `kmts_kodu=KMTS-CANLI` **durdu** |
| 4 | `PATCH` — `{"rfid":"","gorselUrl":"","aciklama":""}` | 200, üçü de **NULL** | **HTTP 200** | `rfid IS NULL=1, gorsel_url IS NULL=1, aciklama IS NULL=1`; gönderilmeyen `kmts_kodu` **KMTS-CANLI** kaldı |
| 5 | `PATCH` — `{"gorselUrl":"data:image/png;base64,AAAA"}` | 400, **eski değer durur** | **HTTP 400** (aynı Türkçe mesaj) | `gorsel_url = https://ornek.test/eski.jpg` (bir önceki adımda yazılan değer) |
| 6 | `POST /cari` `{notlar:…}` → `PATCH {ad}` → `PATCH {notlar:null}` | 201 · not korunur · not silinir | **201 / 200 / 200** | `notlar='Pazarlikta zorlu'` → **aynı** → `notlar IS NULL = 1` |
| 7a | `POST /islemler/satis-fisi` — KART bacağı **cihazsız**, şubede **2 aktif cihaz** | 400 fail-closed | **HTTP 400** · *"POS cihazı belirtilmeli: bu şubede birden çok aktif cihaz var"* | ekranın neden sormak zorunda olduğunun kanıtı |
| 7b | Aynı fiş — `posCihazId` + `posTaksitAdedi: 6` | 200 + POS satırı | **HTTP 200** · `fisId=01M30AJY4BHW5MCH1NZVB6ZAGB` | `SELECT pos_cihaz_id='<pc2>', taksit_adedi, brut_tutar, komisyon_oran FROM pos_tahsilatlari` → **`1 / 6 / 1000.00 / 0.0150`** |
| 8 | `POST /belgeler` (tür `GORSEL`, PNG) → `GET /belgeler?sahipTuru=URUN&sahipId=…` | 200 + listede | **HTTP 200** · liste: `tur=GORSEL, mime=image/png, boyut=72` | galerinin okuduğu uç |
| 9 | `GET /belgeler/{id}/icerik` **jetonlu** / **jetonsuz** | 200 image/png · 401 | **HTTP 200, 72 bayt, `image/png`** / **HTTP 401** | Galerinin neden `<img src>` yerine `GetBaytAsync` + `data:` kullandığının **canlı kanıtı** |
| 10 | `GET /urunler/{id}` (tam kart) | beş alan camelCase | **HTTP 200** · `{'aciklama': None, 'rfid': None, 'kmtsKodu': 'KMTS-CANLI', 'gorselUrl': 'https://ornek.test/eski.jpg', 'pariteKodu': 'BILEZIK_22'}` — 4. ve 5. adımın sonucu birebir görünüyor | istemci tipi (`UrunTamKart`) bu adları okur |

### Temizlik

- API **yalnız kendi PID'i** ile durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş (0 satır)**.
- Test kiracısı `information_schema`'dan çıkarılan **58 tabloda** `tenant_id` ile silindi + `tenants`
  satırı + açılan davet satırı silindi.
- Doğrulama: `tenants = 8` (başlangıç değeri) · `urunler+cariler+pos_tahsilatlari+belgeler+kullanicilar`
  bu kiracı için **0** · davet satırı **0** · **`QA-TEST-%` kiracı sayısı 3 — DOKUNULMADI**.
- Proje ağacına `.mutorj/.bak/.tmp` **bırakılmadı** (mevcut 16 `.mutorj` ve 1 `.bak` önceki
  oturumlardan kalmadır, K-4 ile kurucudadır).

---

## § 8 — denetim.sh sayaçları (MEKANİZMA KURALI)

`dotnet/denetim.sh` içine `# >>> P-10` / `# <<< P-10` işaretli blok eklendi (P-09 bloğunun ardına),
ölçüm komutları blok içinde **yorum olarak** yazılı, tabanlar **belgeden kopyalanmadı, bugün ölçüldü**
(BK-07).

| Sayaç | Değer | Anlamı |
|---|---|---|
| `DTO_FORM_ESLESMEZ` | **0** (5 çift) | Sunucu DTO'sunun kabul ettiği her alan ilgili ekranın gövdesinde var. Çiftler: `UrunOlusturDto`/`UrunGuncelleDto` ⇄ `Urunler.razor`, `CariOlusturDto`/`CariGuncelleDto` ⇄ `Cariler.razor`, `OdemeBacagiDto` ⇄ `Islemler.razor`. Kapsam da denetleniyor: çift sayısı 5'ten aşağı düşerse **kapı kırılır** (sayaç daraltılarak yeşile boyanamaz). Beyaz liste **BOŞ** |
| `BU_PAKETTE_EKLENEN_RECORD` | **0** (razor record 3, tavan 3) | Pages+Bilesenler+Layout'ta istemci record tanımı artmadı; tavan **yalnız azalır** |
| `URUN_GORSEL_GALERISI` | ✓ | Galeri bileşeni var **ve** ürün ekranına bağlı ("uçta var, ekranda yok" bir daha olmasın — UX-S25) |
| `K21_DONANIM_BEYANI` | ✓ | RFID alanının yanında K-21 beyanı yazılı |

Çıktı: `DENETİM TEMİZ.`

---

## § 9 — Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` süzgeciyle, **bu pakete ait olanlar**:

**Yeni**
- `dotnet/Kuyumcu.Api/Modules/Urun/GorselKapisi.cs` (2.647 bayt)
- `dotnet/Kuyumcu.Web/Bilesenler/UrunGorselGalerisi.razor` (5.600 bayt)
- `dotnet/Kuyumcu.Tests/P10AlanBaglamaTestleri.cs` (10.877 bayt)
- `dotnet/Kuyumcu.Tests/P10EkranTestleri.cs` (24.361 bayt)
- `dokumanlar/09-uygulama-kayitlari/PAKET-P-10-uygulama-raporu.md` (bu dosya)

**Değişen**
- `dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs`
- `dotnet/Kuyumcu.Contracts/Sayfalar/UrunlerSozlesmeleri.cs`
- `dotnet/Kuyumcu.Contracts/Sayfalar/CarilerSozlesmeleri.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `dotnet/Kuyumcu.Web/Pages/Urunler.razor` · `…/Cariler.razor` · `…/Islemler.razor`
- `dotnet/Kuyumcu.Web/wwwroot/css/app.css`
- `dotnet/denetim.sh`
- Test uyarlaması (sahte API + bir sağlamlaştırma): `EkranTestleri.cs` · `UX4dEkranTestleri.cs` ·
  `UrunEkranTestleri.cs` · `BasitModPosTestleri.cs` · `DovizBacagiEkranTestleri.cs` ·
  `KarmaOdemeEkranTestleri.cs` · `IadeTestleri.cs` · `PosTeklifEkranTestleri.cs` ·
  `PosBarkodTestleri.cs` · `S3dEkranTestleri.cs` · `YerlesimEkranTestleri.cs` ·
  `UX1C1EkranTestleri.cs` · `UX4bEkranTestleri.cs`

**COMMIT YOK** — çalışma ağacında kalındı, hiçbir git komutu çalıştırılmadı.

---

## § 10 — Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Plan bloğu sözleşmedir (Dosyalar/DoD/Sayaçlar) | **EVET** | § 2, § 4/S1, § 8 |
| 7 alan forma bağlandı | **EVET** | § 1 Y-3, § 7 adım 1–7 |
| Ürün görseli/galerisi kartta gösterilir (GORSEL/FOTOGRAF) | **EVET** | § 1 Y-8, § 7 adım 8–9 |
| `DTO_FORM_ESLESMEZ = 0` devrede | **EVET** | § 8 |
| `BU_PAKETTE_EKLENEN_RECORD = 0` | **EVET** | § 8 |
| Migration: HAYIR | **EVET** | 53 → 53, `dotnet ef` çalıştırılmadı |
| K-21 "donanım kapsam dışı" beyanı ekranda | **EVET** | § 2.4 + sayaç |
| Her iş kuralı için hatayı yeniden üreten test | **EVET** | § 5 |
| Mutasyon kanıtı (kırmızı + cmp) | **EVET** | § 6 (6 mutasyon) |
| 5198'de canlı doğrulama (HTTP + SQL), 5199'a dokunulmadı | **EVET** | § 7 |
| `dotnet/denetim.sh` TEMİZ | **EVET** | § 8 |
| Tam süit 0 başarısız | **EVET** | 3.113/3.113 |
| Test kiracısı silindi, `QA-TEST-*` korundu | **EVET** | § 7 Temizlik |
| Yeni istemci record açılmadı (Contracts'a kondu) | **EVET** | § 2.2 |
| Tip sözleşmesi (ad + tip) | **EVET** | `posTaksitAdedi` `int?`, `pesinKomisyonOran` `decimal` — decimal→int yazma yok |
| PATCH "gönderilmeyen = değişmedi" | **EVET** | § 7 adım 3, `PATCH_gonderilmeyen_alan_KORUNUR` |
| Finansal varlıkta hard DELETE yok | **EVET** | § 3 (POS tahsilatı salt okunur, storno 48-A) |
| Sözlük TR+EN, çıplak metin yok | **EVET** | § 2.3 (21 anahtar × 2 dil, `required`) |
| Yeni CSS'te sabit renk yok, satır içi style yok | **EVET** | § 2.3 |
| Boş durum + hata/başarı bandı | **EVET** | galeri `BosDurum` + `HataBandi`; POS listesi hata bandı |
| Mevcut element id'leri değişmedi; yeni id'ler `<önek>-<alan>` | **EVET** | `u-…`, `d-…`, `cari-…`, `duz-…`, `islem-bacak-{ix}-…` |
| Razor özniteliğinde `{ }` lambda yok · `disabled` dizge değil | **EVET** | `disabled="@(posCihazlari.Count <= 1)"` |
| bUnit sahte API yeni uçlara gerçekçi yanıt | **EVET** | § 5, 13 dosya |
| COMMIT yok · git komutu yok · rm/mv yok | **EVET** | § 9 |
| Sır/parola/jeton çıktıya yazılmadı | **KISMEN — § 11 (1)** | tek ihlal orada yazılı |

---

## § 11 — Açık kalanlar ve dürüstlük notları

1. **(Prosedür ihlali — gizlenmiyor.)** MySQL parolasını değişkene almaya çalışırken `grep … | head -1`
   dosyanın **3. satırındaki yorumu** yakaladı; sebebi anlamak için `docker-compose.yml`'in ilk 20
   satırı ekrana basıldı ve **`MYSQL_ROOT_PASSWORD` değeri bu oturumun çıktısında bir kez göründü**.
   Değer bundan sonra hiçbir yerde tekrarlanmadı ve rapora **yazılmadı**. Bu parola zaten depodaki
   geliştirme `docker-compose.yml` içinde **düz metin** durmaktadır (dosyanın kendi başlığı bunu
   söyler) ve K-5 sır rotasyonu kapsamındadır. **Kurucuya not:** K-5 rotasyonu yapılırken bu değer de
   döndürülmelidir.
2. **Galeri belleği.** Görseller `data:` adresine çevrildiği için her kare belleğe base64 olarak
   alınıyor. Sahip başına en fazla 20 belge ve görsel başına 2 MB tavanı (`BelgeAyarlari`) bunu
   sınırlıyor; yine de çok sayıda büyük görselde çekmecenin açılışı yavaşlayabilir. Ölçülmedi —
   **varsayım yapılmadı**, gerekirse ayrı bir paketle küçük resim (thumbnail) ucu açılmalıdır (1.1).
3. **Liste kapak görseli dış adrestir.** `gorselUrl` üçüncü taraf bir sunucuya işaret ediyorsa tarayıcı
   o adrese istek atar (referans sızıntısı / erişilemezlik). Yükleme yolu (belge deposu) bu riski
   taşımaz. Mehmet'in değerlendirmesine sunulur; şu an şema kapalı listesi (`http`/`https`) dışında
   kısıt yoktur.
4. **`Pages/Islemler.razor` ikinci dokunuş gerekçesi** § 3'te yazılıdır; kapı kaydına işlenmesi
   orkestratörden beklenir.
5. **Tahmin aşımı** (120 → ~200 ajan-dk) § 4/S5'te gerekçelendirildi.

### Kurucuya / mali müşavire notlar

- **K-21 (RFID/PVC donanım):** 1.1'e dondurulmuş hâliyle korundu; ürün içinde "bu kurulumda hariç"
  beyanı görünür. Karar değişirse alan hazırdır, yalnız donanım katmanı eklenir.
- **KMTS kodu doğrulanmıyor.** Kıymetli Madenler Takip Sistemi ile gerçek bir eşleşme/doğrulama
  yapılmamaktadır; alan serbest metin olarak saklanır. Mevzuat gereği doğrulama isteniyorsa bu
  **kapsam dışıdır** ve ayrı karara bağlanmalıdır (mali müşavir teyidi — K-10 kalemiyle birlikte).
- **K-5 sır rotasyonu:** yukarıdaki not 1.
