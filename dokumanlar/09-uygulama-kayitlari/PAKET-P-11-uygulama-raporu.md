# PAKET P-11 — DIŞ KAYNAK DAYANIKLILIĞI: KUR + BİLDİRİM

**Dalga:** DALGA 3 · **Sürüm:** 1.0 · **Tarih:** 2026-09-21
**Roller:** Taşkın (backend) + Ertuğ (frontend) · **Kimlikler:** B-14 · B-15 · B-16 · B-22 · B-23 · B-24
**Plan bloğu:** `PLAN-TEK-SEFER-2026-09-18.md` satır 334-348 (sözleşme)
**Durum:** **YARIM** — 5 kimlik KAPANDI, **B-16 KAPANMADI** ve kurucuya eskale edildi (Anayasa md.1).

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (durma-devam et)

| Önkoşul | Diskte var mı | Kanıt |
|---|---|---|
| P-06 | **VAR** | `dokumanlar/09-uygulama-kayitlari/PAKET-P-06-uygulama-raporu.md` · `Kuyumcu.Web/Bilesenler/SatirIciDuzenle.razor` |
| P-07 | **VAR** | `PAKET-P-07-uygulama-raporu.md` · `Kuyumcu.Web/Servisler/ApiIstemci.cs` (`PatchAsync` mevcut, kullanıldı) |

Kapsam daraltması gerekmedi.

---

## § 1 — SPESİFİKASYONUN KODA KARŞI DOĞRULANMASI (sapmalar)

Plan bloğu **2026-09-18** tarihlidir; aradan üç paket geçmiştir. Kodda ölçülen gerçek durum:

| Plan iddiası | Kodda ölçülen (2026-09-21) | Sonuç |
|---|---|---|
| "Kaynak kapalıyken *Şimdi çek* pasif" | **ZATEN VAR** — `Pages/Kur.razor:28` `disabled="@(kaydediliyor \|\| CekmeKapali)"` + `title` sebebi; testi `S3eKurKaynagiEkranTestleri.Dis_kaynaklar_kapaliyken_SIMDI_CEK_kilitli_ve_sebebi_yazili` | Paket S-3e kapatmış |
| "503 sonrası kur kartı DOM'da kalır" | **YALNIZ POST yolunda** kapatılmıştı. **GET yolları hâlâ veriyi düşürüyordu**: `GuncelYukle` → `guncel = null`, `GecmisYukle` → `gecmis = []`, `PariteYukle` → `pariteler = []`, `KaynakDurumuYukle` → `kaynakDurumu = null` | **AÇIK KUSUR — bu pakette kapatıldı** |
| "Kur geçmişinde *İptal et* gerçekten istek üretir" | **ZATEN VAR** (S-3b): onay modalı + gerekçe + `POST /kur/manuel/{id}/iptal` | Canlı yeniden doğrulandı |
| "`/bildirimler` basit mod menüsünde görünür" | **ZATEN VAR** — `Servisler/BasitMenu.cs:38` (UX-1-C4 devri), testi `BasitModMenuTestleri.cs:170` | B-22 kapalı |
| "3 satır içi `style` kalkar" (`/bildirimler`) | `Pages/Bildirimler.razor` içinde satır içi `style` **0 adetti**. Depoda P-11 kapsamındaki tek satır içi style **`Pages/Kur.razor:277`** idi | Hedefe sadık kalınarak **Kur.razor'daki 1 style** kaldırıldı; sayaç kuruldu |
| "Zamanlanmış iş ayarları düzenlenebilir" | **YOKTU.** Ayarlar yalnız `appsettings.json` → `Isler:*`; ne uç ne ekran vardı | **Bu pakette yapıldı** |
| "B-16 kalıntısı temizlenir ya da geçersiz kılınır" | Kalıntı **duruyor** ve **iptal edilemiyor** (§ 6) | **KAPANMADI — eskale** |

> **Plan ile koşulların çelişkisi (tek adet):** görev koşulları "`QA-TEST-` önekli kiracıların verisine ASLA dokunma" der; plan DoD'u B-16 kalıntısının (QA-TEST-3 kiracısında) temizlenmesini ister. Çelişki **§ 6**'da ölçümle çözüldü: uygulamanın kendi fail-closed kapısı zaten iptali reddediyor, yani plan maddesi **teknik olarak da** uygulanamaz durumda. Veri elle SQL ile değiştirilmedi.

> **Mevzuat/DOGRULANMALI hükmü yok.** Bu pakette mevzuata dayanan yeni bir sabit yazılmadı. `Isler:TevsikKurtarma:Etkin=false` (dayanağı doğrulanmamış) **kapalı** bırakıldı ve kiracı ayarıyla **açılamaz** hâle getirildi (fail-closed, § 3.3).

---

## § 2 — KİMLİK KAPANIŞ TABLOSU

| Kimlik | Konu | Durum | Kanıt |
|---|---|---|---|
| **B-14** | `/kur` hata davranışı (503'te kart kaybolması) | **KAPANDI** | `Pages/Kur.razor` `GuncelYukle`/`GecmisYukle`/`PariteYukle`/`KaynakDurumuYukle` artık veri düşürmüyor · testler `Guncel_kur_OKUMASI_503_donunce_son_bilinen_kur_EKRANDA_KALIR`, `Gecmis_OKUMASI_hata_verince_BOS_DURUM_cizilmez_hata_bandi_cikar`, `Parite_OKUMASI_hata_verince_parite_tablosu_DUSMEZ` · mutasyon M11-1/M11-2 · sayaç `P-11: DIS_HATADA_VERI_DUSUREN_YOL = 0` |
| **B-15** | `/kur` geçmişi "İptal et" etkisiz | **KAPANDI** | Canlı: adım 11-16 (`HTTP 201` → `400` kısa gerekçe → `200` iptal → `409` ikinci iptal); ekran testi `Gecmisteki_MANUEL_satirin_iptal_dugmesi_ONAY_MODALI_acar` |
| **B-16** | Geri alınamayan QA5 kalıntısı (MANUAL kur ₺6.820,50) | **KAPANMADI — eskale** | § 6: kayda **4 canlı hareket bağlı**, uygulamanın fail-closed kapısı iptali reddediyor; kiracı `QA-TEST-3` kalıcı fikstür. **Devredildi: K-1 (kurucu — canlı veritabanı temizliği izni)** |
| **B-22** | `/bildirimler` basit mod menüsünde yok | **KAPANDI (önceki pakette, doğrulandı)** | `Servisler/BasitMenu.cs:38` · test `BasitModMenuTestleri.cs:170` · yeni makine kapısı `P-11: BILDIRIM_BASIT_MENUDE` |
| **B-23** | Zamanlayıcı döngüsünün canlı kanıtı | **KAPANDI (üçüncü denemede)** | § 5: `TurDakika=1`, 5198, iki ayrı oturumda ölçüldü; `T0=21:40:11 → 96`, `T1=21:41:16 → 144` (65 saniyede tam bir tur daha) |
| **B-24** | Zamanlanmış iş ayarları düzenlenemiyor | **KAPANDI** | `Modules/Isler/IsAyarServisi.cs` (yeni) · `PATCH /api/v1/isler/ayarlar` · `Pages/Bildirimler.razor` ayar kartı · 27 yeni test · canlı adım 1-10 |

---

## § 3 — YAPILAN İŞ

### 3.1 B-14 — dış hatada veri düşüren yol kapatıldı (`Pages/Kur.razor`)

Dört okuma yolu da artık **son bilinen veriyi DOM'da bırakır**, hata bandını **ayrı** basar:

- `GuncelYukle` — `guncel = null` **kaldırıldı**; hata `FormDurumu.SunucuMesaji` ile bant'a yazılır.
- `GecmisYukle` — `gecmis = []` **kaldırıldı**; ayrıca `@if (hata) … else if (bos)` zinciri **ayrıştırıldı**: hata varken "kayıt yok" boş durumu ÇİZİLMEZ (UX-S28).
- `PariteYukle` — `pariteler = []` **kaldırıldı**, aynı ayrıştırma.
- `KaynakDurumuYukle` — `kaynakDurumu = null` **kaldırıldı**. Eskiden geçici bir 503 kaynak seçicisini ekrandan siliyor **ve `Şimdi çek` kilidini açıyordu** (`CekmeKapali` yanlış hesaplanıyordu). Artık son bilinen durum korunur; ilk yüklemede hata varsa sonsuz "yükleniyor" yerine hata bandı görünür.

### 3.2 Satır içi style (`SATIR_ICI_STYLE` cırcırı)

`Pages/Kur.razor:277` `style="white-space:nowrap"` → `class="hucre-tek-satir"`; sınıf `wwwroot/css/app.css` içinde **belirteçsiz sabit renk içermez** (yalnız `white-space`). Depo geneli 156 → **155**.

### 3.3 B-24 — zamanlanmış iş ayarları (saat / eşik / açık-kapalı)

**Veri modeli — MIGRATION YOK.** Ayarlar **mevcut** `kiraci_ayarlari` anahtar/değer tablosunda tutulur; o tablo `IKiraciVarlik`'tir, global kiracı filtresinden, yazma denetçisinden ve denetim izinden **kendiliğinden** geçer. Yeni tablo açmak bu üç mekanizmayı yeniden kurmak olurdu.

Anahtarlar (`Data/ZamanlanmisIs.cs` → `IsAyarAnahtari`): `is_baslangic_saat` (0-23) · `is_vade_ufuk_gun` (1-365) · `is_masak_uyari_gun` (0-60) · `is_kapali_isler` (virgüllü kod listesi).

**Davranış:**
- **Saat** — zamanlanmış tur, kiracının yerel saati eşiğin altındaysa **hiç koşmaz** (`IsCalistirici.TumEtkinIsleriCalistir`). **Elle** tetikleme bu kapıdan geçmez (kullanıcı bilerek istemiştir).
- **Eşik** — `VadeHatirlatmaIsi` ve `MasakSibSuresiIsi` eşiği artık `IsAyarServisi`'nden okur; ayar yoksa `appsettings` varsayılanı geçerlidir.
- **Açık/kapalı** — kiracı bir işi kapatabilir; kapalı iş ne turda ne elle koşar (uç `400` + Türkçe sebep).
- **FAIL-CLOSED / "ayar yalnız DARALTIR":** yapılandırmada (`Isler:*:Etkin=false`) kapalı bir iş **ekrandan AÇILAMAZ**. Gerekçe: bu anahtar kurulum/mevzuat kapısıdır (ör. `TevsikKurtarma` dayanağı doğrulanmamış), ekrandan delinemez.

**Uçlar** (`Modules/Isler/IslerModulu.cs`, sınıf niteliği `[ModulYetkisi("bildirim")] [RolGerekli("SAHIP","MUDUR")]`):
- `GET /api/v1/isler` — yanıta `ayarlar` bloğu eklendi: değerler **+ sınırlar + kurulum varsayılanları** (UX-S29/S30: ekran kendi kopyasını tutmaz). Her iş satırına `yapilandirmaEtkin` ve `kiraciKapatti` **ayrı** bayrakları eklendi ("neden kapalı?" sorusunun cevabı).
- `PATCH /api/v1/isler/ayarlar` — **üç durumlu** (`CariGuncelleDto.Aktif/AktifVerildi` ile aynı kalıp, ikinci kalıp açılmadı): alan gövdede **yoksa değişmez**, **`null` ise ayar satırı silinir** ve kurulum varsayılanına dönülür, doluysa yazılır. Boş gövde `400`.
- Denetim izi **yeniden yazılmadı**: mevcut `DenetimServisi.YazmaKaydet` çağrıldı (`varlik_turu = IsAyari`).

**Ekran** (`Pages/Bildirimler.razor`, `ProAlan` içinde, SAHİP/MÜDÜR):
- Ayar kartı `#is-ayar-kart`: saat / vade ufku / MASAK eşiği alanları, her birinin altında **kabul aralığı ve kurulum varsayılanı yazılı** (UX-S29/S30).
- `#is-ayar-kaydet` (gerçek PATCH) ve `#is-ayar-varsayilan` (alanları açıkça `null` gönderir → varsayılana dönüş, UX-S25 geri dönüş yolu). Kapatılan işler bu düğmeden **etkilenmez** (alan gövdede yok).
- İş tablosuna **Açık** sütunu: `#is-acik-<KOD>` kutusu. Yapılandırmada kapalı iş için kutu **kilitli** + sebep metni `#is-yapkapali-<KOD>`; kiracının kapattığı iş için **düzenlenebilir** + `#is-kiracikapali-<KOD>`.
- Geçersiz değerde **alan altı Türkçe mesaj** ve **istek atılmaz** (sessiz etkisizlik yasağı).
- Çalışma geçmişi tablosunun üstüne `DegismezIpucu` (UX-S25: koşum kaydı değişmez, doğru yol ayarları düzenleyip işi yeniden çalıştırmaktır).
- Sözlük **TR + EN** eksiksiz (`Kuyumcu.Core/I18n/Sozlukler.cs`, `SozlukBildirim`'e 19 yeni anahtar); razor'da çıplak metin yok.

**Tip sözleşmesi:** `Pages` içinde **yeni istemci record'u AÇILMADI**; `IsAyarlariVeri` ve `IsSatiri` genişletmesi `Kuyumcu.Contracts/Sayfalar/BildirimlerSozlesmeleri.cs`'e yazıldı. Alan adları **ve tipleri** sunucuyla birebir (`int`/`int?`, `List<string>`).

---

## § 4 — DoD SÖZLEŞMESİ (hangi kayıt hangi uçla düzenlenir)

| Varlık / kayıt | Düzenleme | Silme / pasife alma | Yetki | Denetim izi | Ekran eylemi |
|---|---|---|---|---|---|
| **Kiracı iş ayarı** (`kiraci_ayarlari`, `is_*`) — P-11'in getirdiği tek yeni kayıt türü | `PATCH /isler/ayarlar` (üç durumlu) | Ayar satırı **kaldırılır** = kurulum varsayılanına dönüş. Finansal varlık **değildir**, bir tercih satırıdır | `[ModulYetkisi("bildirim")]` + `[RolGerekli("SAHIP","MUDUR")]` | `DenetimServisi.YazmaKaydet` → `IsAyari` (+ `DenetimYazmaDinleyicisi` otomatik satırı) | `#is-ayar-kaydet` · `#is-ayar-varsayilan` · `#is-acik-<KOD>` |
| `IsCalismasi` (koşum kaydı) | **SALT OKUNUR** | yok | — | — | `DegismezIpucu`: "her satır bir koşumun kanıtıdır"; doğru yol = ayarı değiştir + yeniden çalıştır |
| `Bildirim` | kapatma damgası (mevcut) | **SİLİNMEZ** | mevcut | mevcut | mevcut (değişmedi) |
| `KurSnapshot` (MANUAL) | **düzeltilmez**, gerekçeli **iptal** + yeni manuel giriş | hard DELETE **yok** — `iptal_edildi` damgası | `[RolGerekli]` mevcut | `KurIptali` (hassas) | `#kur-iptal-<id>` + onay modalı |
| `KurSnapshot` (dış kaynak) | **değişmez** | — | — | — | düğme **kilitli** + `DegismezIpucu` ("yenisi çekilir") |

**Finansal varlıklarda hard DELETE yoktur.** Bu pakette hiçbir finansal varlığa DELETE eklenmedi.

**Her düğme ya istek atar ya kilitli + sebepli** (DoD EK md.1): `#is-ayar-kaydet` (PATCH) · `#is-ayar-varsayilan` (PATCH) · `#is-acik-<KOD>` (PATCH, kilitliyse `#is-yapkapali-<KOD>` sebebi) · `#is-calistir-<KOD>` (POST, kilitliyse `title` ile iki **farklı** sebep) — testi `Kiracinin_kapattigi_isin_CALISTIR_dugmesi_KILITLI_ve_sebebi_AYRIDIR`.

**Tek düzenleme kalıbı** (DoD EK md.2): yeni `duzenlenenId` kalıbı açılmadı, `Pages/` altında **yeni `ModalPencere` düzenleme kullanımı = 0**; ayar kartı doğrudan sayfa içi formdur.

**Tek dokunuş** (DoD EK 2) — **Dokunulan sayfalar:** `Pages/Kur.razor` · `Pages/Bildirimler.razor`. İkisi de bu dalgada **ilk kez** açıldı.

---

## § 5 — B-23: ZAMANLAYICI DÖNGÜSÜNÜN CANLI KANITI (üçüncü deneme — BAŞARILI)

API 5198'de `Isler__TurDakika=1` ve `Logging__LogLevel__Kuyumcu.Api.Modules.Isler=Debug` ile başlatıldı (**5199'a hiç dokunulmadı**, `lsof` ile doğrulandı).

| Kanıt | Ölçüm |
|---|---|
| Döngü başladı | `Zamanlanmış iş turu başladı (aralık: 1 dk)` — `TurDakika=1` **etkin** |
| Tur tekrarı (ölçüm 1) | `zaten çalışmış` satırı **48 → 96** (75 saniyede tam bir tur) |
| Tur tekrarı (ölçüm 2, damgalı) | `T0=21:40:11 → 96` · `T1=21:41:16 → 144` — **65 saniyede +48 satır** = 8 kiracı × 3 iş × 2 kayıt |
| İkinci API oturumu (temiz derleme) | 4 dakikada **96** satır = 4 tur |
| Yeni kiracıya ulaştı mı | `PAKET-P-11-TEST` kiracısında `is_calismalari` **1 satır** (elle tetikleme) + tur tarafından gezildi |
| Tekillik | Aynı yerel günde ikinci koşum **veritabanı tarafından reddedildi** ("İş bu anahtarda zaten çalışmış: VADE_HATIRLATMA/2026-09-21") |

**B-23 KAPANDI.** Döngü gerçekten uyanıyor, tüm kiracıları geziyor ve ikinci turu doğru biçimde atlıyor.

---

## § 6 — B-16: GERİ ALINAMAYAN KALINTI (KAPANMADI — ESKALE)

**Kalıntı:** `kur_snapshots.id = 01M2SA1JT6SD2P40RWFZHZ13R4` · `6820.5000` · `MANUAL` · `2026-09-18 03:47:57` · `iptal_edildi = 0` · kiracı `01M2NZ97WE0PC3EGQG0NZX3W74` (**QA-TEST-3 Kuyumculuk**).

**Neden kapatılamadı — iki bağımsız engel:**

1. **Uygulamanın kendi fail-closed kapısı reddediyor.** Bu kur kaydına **4 canlı hareket bağlı**:

   ```sql
   SELECT COUNT(*) FROM hareketler WHERE kur_snapshot_id='01M2SA1JT6SD2P40RWFZHZ13R4';  -- 4
   ```
   | hareket id | tür | tarih |
   |---|---|---|
   | `01M2SA4DWH8VRKQAK9YVAEJ45S` | EMANET_GIRIS | 2026-09-18 03:49:30 |
   | `01M2SA4TMR2XNNSPXKSC56HM8D` | EMANET_GIRIS | 2026-09-18 03:49:43 |
   | `01M2SBFT7VJR12ECXR232E5C0M` | EVRAK_GIRIS | 2026-09-18 04:13:12 |
   | `01M2SKZ5MBNM8KN64VJNTV0Z4H` | EVRAK_GIRIS | 2026-09-18 06:41:24 |

   `KurModulu.ManuelKurIptal` bu durumda **409** döner: *"Bu kur kaydına bağlı 4 işlem var; kur iptal edilemez. Önce o işlemleri storno edin."* Kural doğrudur: geçmiş bir işlemin kuru, işlem dururken "geçersiz" olamaz — has hesabı açıklanamaz hâle gelirdi. Kapı zaten testlidir (`S3bDuzeltilebilirlikHttpTestleri`, satır ~285-312) ve **bu pakette gevşetilmedi**.

2. **Kiracı kalıcı QA fikstürüdür.** `QA-TEST-` önekli kiracıların verisi görev koşullarıyla korunmaktadır; 4 hareketi storno etmek fikstürü bozar ve P-11'in kapsamı dışıdır (yaz-seri / kapsam kilidi).

**Yapılmayan şey ve nedeni:** satır **SQL ile elle `iptal_edildi=1` yapılmadı**. Bu, uygulamanın kendi değişmezini arkadan delmek, denetim izi bırakmadan finansal bir damga atmak ve 4 hareketi geçersiz bir kura bağlı bırakmak olurdu.

**Devredildi:** **K-1 — kurucunun canlı veritabanı temizliği izni** (plan § 0, DALGA 7 öncesi gereken kalem). Kurucu izin verdiğinde iki yol var: (a) QA-TEST-3 fikstürünü baştan kurmak, (b) 4 hareketi storno edip kuru iptal etmek. **Ekip tek taraflı seçmedi.**

### Geri alınamayan değişiklikler (bu dalga)

| # | Değişiklik | Geri alınabilir mi | Not |
|---|---|---|---|
| 1 | `PAKET-P-11-TEST` kiracısı açıldı ve **tamamen silindi** | — | `tenants` 8 → 9 → **8** (başlangıç değerine döndü) |
| 2 | Bu kiracıda 1 manuel kur kaydı **iptal** edildi | **HAYIR** (damga geri alınmaz) | Kayıt kiracıyla birlikte silindi; kalıcı etkisi yok |
| 3 | QA-TEST-3'teki B-16 kalıntısına **DOKUNULMADI** | — | § 6, K-1'e devredildi |
| 4 | `kiraci_davetleri`'ne 1 davet satırı eklendi ve **silindi** | — | Ham jeton diske yazılmadı, yalnız SHA-256 özeti DB'ye gitti; ikisi de temizlendi |

---

## § 7 — DERLEME, TESTLER, MUTASYON, DENETİM

### Derleme
```
dotnet build Kuyumcu.sln --nologo  →  0 Uyarı · 0 Hata
```

### Migration
**HAYIR — gerekmedi.** Plan da "Migration: HAYIR" diyor. Ayarlar mevcut `kiraci_ayarlari` tablosuna yazıldı.
`__EFMigrationsHistory` = **53** (değişmedi) · `Migrations/*.cs` = 115 dosya (değişmedi).

### Test sayıları

| Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| **Önce** (P-10 raporu) | 3113 | 3113 | 0 | 0 |
| **Sonra** (tam süit) | **3151** | **3151** | **0** | **0** |

**Yeni test: 38.**

| Dosya | Yeni test | Konu |
|---|---|---|
| `Kuyumcu.Tests/P11IsAyarHttpTestleri.cs` (**yeni**) | 13 | PATCH sözleşmesi, sınırlar, rol kapısı, kiracı kapaması, denetim izi — gerçek HTTP boru hattı |
| `Kuyumcu.Tests/P11IsAyarTestleri.cs` (**yeni**) | 14 | Saat kapısı, kiracı kapaması, kiracı izolasyonu, fail-closed, eşik ezme, bozuk değer, **servis içi aralık doğrulaması** |
| `Kuyumcu.Tests/BildirimEkranTestleri.cs` | 7 | Ayar kartı, gerçek PATCH, alan altı hata, varsayılana dönüş, kilitli kutu sebepleri |
| `Kuyumcu.Tests/S3eKurKaynagiEkranTestleri.cs` | 4 | B-14 okuma yolları + satır içi style regresyonu |

Tam süit logu: `scratchpad/tam.log` · TRX: `scratchpad/trx-pP-11/`

### Mutasyon tablosu

Yöntem: kaynak `scratchpad/mut-yedek/P-11/` altına kopyalandı → `perl -0777 -pi -e` ile **tek eşleşmeli** değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → **`cmp` ile birebir aynı** doğrulandı.

| Kod | Dosya | Değişiklik | Kırmızıya dönen test | Geri alındı (`cmp`) |
|---|---|---|---|---|
| M11-1 | `Pages/Kur.razor` | `GuncelYukle` catch'ine `guncel = null;` geri kondu | `S3e…Guncel_kur_OKUMASI_503_donunce_son_bilinen_kur_EKRANDA_KALIR` | birebir aynı |
| M11-2 | `Pages/Kur.razor` | `GecmisYukle` catch'ine `gecmis = [];` geri kondu | `S3e…Gecmis_OKUMASI_hata_verince_BOS_DURUM_cizilmez_hata_bandi_cikar` (+1) | birebir aynı |
| M11-3 | `Modules/Isler/IsCalistirici.cs` | saat kapısı koşulu `false` yapıldı | `P11IsAyarTestleri.Tur_baslangic_saatinden_ONCE_hic_kosmaz` | birebir aynı |
| M11-4 | `Modules/Isler/IsCalistirici.cs` | tur döngüsünden `KapaliIsler` süzgeci çıkarıldı | `P11IsAyarTestleri.Kiracinin_kapattigi_is_TURDA_kosmaz_digerleri_kosar` | birebir aynı |
| M11-5 | `Modules/Isler/IsAyarServisi.cs` | `if (dto.VadeUfukGunVerildi)` → `if (true)` (PATCH üç durumu bozuldu) | `P11IsAyarHttpTestleri.Patch_govdede_OLMAYAN_alan_DEGISMEZ` | birebir aynı |
| **M11-6** | `Modules/Isler/IsAyarServisi.cs` | servis içi aralık kontrolü söküldü | **HİÇBİR TEST KIRILMADI → TEST AÇIĞI** (aşağı bakınız) | birebir aynı |
| **M11-6b** | aynı | aynı mutasyon, test güçlendirildikten sonra | `P11IsAyarTestleri.Servis_araligi_KENDI_zorlar_controller_niteligine_guvenmez` — **5 teori vakası kırmızı** | birebir aynı |
| M11-7 | `Modules/Isler/IslerModulu.cs` | elle çalıştırmadaki kiracı kapısı silindi | `P11IsAyarHttpTestleri.Kiracinin_kapattigi_is_ELLE_de_calismaz` | birebir aynı |
| M11-8 | `Pages/Bildirimler.razor` | saat alanı aralık doğrulaması silindi | `BildirimEkranTestleri.Aralik_disi_deger_ALAN_ALTI_mesaj_verir_ve_ISTEK_ATILMAZ` | birebir aynı |
| M11-9 | `Pages/Kur.razor` | `class="hucre-tek-satir"` → `style="white-space:nowrap"` | `S3e…Kur_gecmisi_satirinda_SATIR_ICI_style_YOKTUR` + **denetim sayacı** | birebir aynı |
| M11-10 | `Modules/Isler/IsAyarServisi.cs` | `db.KiraciAyarlari.Remove(kayit);` silindi | `P11IsAyarHttpTestleri.Patch_null_alani_SILER_ve_VARSAYILANA_dondurur` (+1) | birebir aynı |
| M11-11 | `Pages/Kur.razor` | M11-1 ile aynı, **denetim.sh'a karşı** | `DIS_HATADA_VERI_DUSUREN_YOL 1 (≠0)` + `TOPLAM 48 > tavan 47` → **DENETİM BAŞARISIZ** | birebir aynı |

> **M11-6 — bulunan gerçek test açığı (Anayasa md.1).** Servisteki aralık kontrolü söküldüğünde HTTP testleri **yeşil kaldı**, çünkü DTO `[Range]` nitelikleri controller yolunda önce devreye giriyordu. Bu, "DataAnnotations yalnız controller yolunda çalışır → servis içi elle kontrol" değişmezinin tam da uyardığı boşluktur. **6 yeni servis düzeyi test** eklendi (`Servis_araligi_KENDI_zorlar…` 5 teori + `Servis_gecerli_degeri_YAZAR_sinir_ucunda_reddetmez`), mutasyon **M11-6b** olarak tekrarlandı ve **kırmızıya döndü**.

Son mutasyondan sonra temiz derleme: **0 Uyarı · 0 Hata**.

### `dotnet/denetim.sh`

Yeni işaretli blok `# >>> P-11` / `# <<< P-11` (§ 7 blok kuralı, BK-08). Tabanlar **belgeden kopyalanmadı, 2026-09-21'de taze ölçüldü** (BK-07) ve ölçüm komutları betiğe yorum olarak yazıldı.

```
  ✓ P-11: DIS_HATADA_VERI_DUSUREN_YOL              0 (-eq 0: /kur + /bildirimler)
  ✓ P-11: DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM       47 (tavan 47 — cırcır YALNIZ azalır)
  ✓ P-11: SATIR_ICI_STYLE                          155 (tavan 155 — cırcır YALNIZ azalır)
  ✓ P-11: SATIR_ICI_STYLE_P11                      0 (-eq 0: /kur + /bildirimler)
  ✓ P-11: BILDIRIM_BASIT_MENUDE                    BasitMenu.Yollar içinde
  ✓ P-11: IS_AYARI_DUZENLENEBILIR                  PATCH /isler/ayarlar + ekran bağı
  ✓ P-11: IS_AYARI_FAIL_CLOSED                     ayar yalnız daraltır

DENETİM TEMİZ.
```

> **Sayaç sapması (dürüst beyan).** Plan `DIS_HATADA_VERI_DUSUREN_YOL = 0` diyor. Depo genelinde bugün **47** böyle yol var ve hepsi P-11'in kapsamı **dışındaki** ekranlarda (kapsam kilidi: başka paketin kimliğine dokunulmaz). Bu yüzden sayaç **ikiye ayrıldı**: P-11'in sahiplendiği iki ekranda **mutlak 0** (plan maddesi birebir), depo genelinde **taze ölçülmüş 47 tavanlı cırcır** (yalnız azalır, hiç artırılamaz). Tavan hiçbir yerde yükseltilmedi: 50 → 47 **düştü**, 156 → 155 **düştü**. Sayaçların gerçekten kırıldığı **M11-11** ile kanıtlandı.

---

## § 8 — CANLI DOĞRULAMA (localhost:5198 · 5199'a DOKUNULMADI)

Test kiracısı **`PAKET-P-11-TEST Kuyumculuk`** (`01M30C9A76TCDYC8NC48J4K52J`). Davet jetonu yerel üretildi, DB'ye **yalnız SHA-256 özeti** yazıldı. Tüm yanıt gövdeleri **okundu** (`-o /dev/null` kullanılmadı); hiçbir jeton/parola rapora girmedi.

> **Anayasa md.1 — ilk turda bulunan ve düzeltilen hata:** ilk canlı tur, mutasyon koşumlarından kalan **bayat (mutasyonlu) derleme** üzerinde koştu ve adım 4/9'da yanlış sonuç verdi. Gizlenmedi: API durduruldu, `dotnet build` **temiz** koşturuldu, ayar satırları silindi ve **tüm tur baştan** tekrarlandı. Aşağıdaki tablo **temiz derleme** üzerindeki turdur.

| # | Adım | Beklenen | Gerçek | SQL / kanıt |
|---|---|---|---|---|
| 1 | `GET /isler` | 200 + `ayarlar` bloğu | **HTTP 200** · `baslangicSaat 0, vadeUfukGun 7, masakUyariGun 3, kapaliIsler []`, sınırlar `0-23 / 1-365 / 0-60`, varsayılanlar `7 / 3` | 3 iş: `yapilandirmaEtkin=True`, `kiraciKapatti=False` |
| 2 | `PATCH /isler/ayarlar {9,21,5}` | 200 | **HTTP 200** · `{"baslangicSaat":9,"vadeUfukGun":21,"masakUyariGun":5}` | `kiraci_ayarlari` 3 satır |
| 3 | `PATCH {baslangicSaat:11}` (kısmi) | 200, **diğerleri korunur** | **HTTP 200** · `11 / 21 / 5` | PATCH sözleşmesi: veri imhası yok |
| 4 | `PATCH {vadeUfukGun:null}` | 200, **varsayılana döner** | **HTTP 200** · `vadeUfukGun 21 → **7**` | `is_vade_ufuk_gun` satırı **SİLİNDİ** |
| 5 | `PATCH {baslangicSaat:24}` | 400 | **HTTP 400** · alan altı: "Baslangic saat 0 ile 23 arasında olmalıdır" | ayar **değişmedi** |
| 6 | `PATCH {}` (boş gövde) | 400 | **HTTP 400** · "Güncellenecek alan gönderilmedi" | sessiz etkisizlik yok |
| 7 | `PATCH {kapaliIsler:["VADE_HATIRLATMA"]}` | 200 | **HTTP 200** · `kapaliIsler ["VADE_HATIRLATMA"]` | `is_kapali_isler` satırı yazıldı |
| 8 | `POST /isler/VADE_HATIRLATMA/calistir` | **400** | **HTTP 400** · "Bu iş bu işletmenin ayarlarında kapalı: VADE_HATIRLATMA" | ekran kilidi ile sunucu **aynı** cevabı verdi |
| 9 | `PATCH {kapaliIsler:[]}` | 200, geri dönüş yolu | **HTTP 200** · `kapaliIsler []` | `is_kapali_isler` satırı **SİLİNDİ** |
| 10 | `POST …/calistir` (yeniden) | 200 | **HTTP 200** · `durum BASARILI, atlandi false, sureMs 6, calismaAnahtari 2026-09-21-E0043` | `is_calismalari` 1 satır |
| 11 | `POST /kur/manuel {6820.50}` | 201 | **HTTP 201** · `kaynak MANUAL, iptalEdildi false` | — |
| 12 | `GET /kur/guncel` | 200 | **HTTP 200** · `6820.5 MANUAL` | manuel kayıt güncel oldu |
| 13 | `POST /kur/manuel/{id}/iptal` gerekçe "kisa" | 400 | **HTTP 400** · "Gerekçe en az 20 karakter olmalıdır" | kayıt **değişmedi** |
| 14 | `POST …/iptal` geçerli gerekçe | 200 | **HTTP 200** · `iptalEdildi true` + gerekçe döndü | **B-15 kapandı** |
| 15 | `GET /kur/guncel` (iptal sonrası) | 200, **iptal edilen kur GÜNCEL değil** | **HTTP 200** · `6847.27 ALTINKAYNAK` | iptal edilmiş kayıt atlandı |
| 16 | `POST …/iptal` (ikinci kez) | 409 | **HTTP 409** · "Bu kur kaydı zaten iptal edilmiştir." | çifte damga yok |
| 17 | TEZGÂHTAR jetonuyla `PATCH /isler/ayarlar` | 403 | **HTTP 403** (`SubeIhlali` kapısı önce devreye girdi) | rol kapısının kendisi `P11IsAyarHttpTestleri.Tezgahtar_ayar_degistiremez` ile **403** olarak ayrıca ölçüldü |
| 18 | TEZGÂHTAR `GET /isler` | 403 | **HTTP 403** | fail-closed |

**Denetim izi (SQL):**
```
EKLEME  IsAyari  is_ayarlari  Zamanlanmış iş ayarı güncellendi (is_baslangic_saat, is_vade_ufuk_gun, is_masak_uyari_gun)
EKLEME  IsAyari  is_ayarlari  Zamanlanmış iş ayarı güncellendi (is_baslangic_saat)
EKLEME  IsAyari  is_ayarlari  Zamanlanmış iş ayarı güncellendi (is_vade_ufuk_gun)
EKLEME  IsAyari  is_ayarlari  Zamanlanmış iş ayarı güncellendi (is_kapali_isler)   × 2
EKLEME  IS_ELLE_CALISTIRMA  VADE_HATIRLATMA
EKLEME  KurIptali  01M30CC54J747QN9WCA447MNJA  manuel kur kaydı GEÇERSİZ kılındı · gerekçe: …
```

### Temizlik
- API **yalnız kendi PID'i** ile durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş (0)**.
- **5199'a hiç dokunulmadı** → `lsof -nP -iTCP:5199 -sTCP:LISTEN` **boş (0)**.
- Test kiracısı `tenant_id` kolonu olan **58 tablodan** + `kiraci_davetleri` + `tenants`'tan silindi.
- **`tenants` sayısı: 8 → 9 → 8** (başlangıç değerine döndü) · `kiraci_ayarlari` içinde `is_%` satırı **0** · `PAKET-P-11-TEST` daveti **0**.
- **`QA-TEST-*` kiracılarına hiç dokunulmadı** (B-16 dahil — § 6).
- Proje ağacına `.mutorj` / `.bak` / `.tmp` bırakılmadı (mevcut olanlar 18 Eylül'den kalma, bu paketin değil).

---

## § 9 — DEĞİŞEN DOSYALAR

`git status --porcelain` bu depoda tek commit olduğu için kullanışsızdır (yüzlerce dosya `??`). P-11'in dokunduğu dosyalar tek tek:

**Yeni**
- `dotnet/Kuyumcu.Api/Modules/Isler/IsAyarServisi.cs`
- `dotnet/Kuyumcu.Tests/P11IsAyarHttpTestleri.cs`
- `dotnet/Kuyumcu.Tests/P11IsAyarTestleri.cs`

**Değişen**
- `dotnet/Kuyumcu.Api/Data/ZamanlanmisIs.cs` (`IsAyarAnahtari`, `IsAyarlari`)
- `dotnet/Kuyumcu.Api/Modules/Isler/IslerModulu.cs` (`IsAyarPatchDto`, `GET /isler` genişletme, `PATCH /isler/ayarlar`, elle koşum kapısı)
- `dotnet/Kuyumcu.Api/Modules/Isler/IsCalistirici.cs` (saat kapısı, kiracı kapaması, `KiraciEtkinMi`)
- `dotnet/Kuyumcu.Api/Modules/Isler/Isler/VadeHatirlatmaIsi.cs` (eşik ezme)
- `dotnet/Kuyumcu.Api/Modules/Isler/Isler/MasakSibSuresiIsi.cs` (eşik ezme)
- `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` (`IsAyarServisi` kaydı)
- `dotnet/Kuyumcu.Contracts/Sayfalar/BildirimlerSozlesmeleri.cs` (`IsAyarlariVeri`, `IsSatiri`, `IsYaniti`, `IsGorunumu`)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (19 anahtar × TR + EN)
- `dotnet/Kuyumcu.Web/Pages/Kur.razor` (B-14 + satır içi style)
- `dotnet/Kuyumcu.Web/Pages/Bildirimler.razor` (B-24 ayar kartı + açık/kapalı kutusu)
- `dotnet/Kuyumcu.Web/wwwroot/css/app.css` (`.hucre-tek-satir`)
- `dotnet/Kuyumcu.Tests/S3eKurKaynagiEkranTestleri.cs` (+4 test, sahte API okuma hatası)
- `dotnet/Kuyumcu.Tests/BildirimEkranTestleri.cs` (+7 test, sahte API gerçekçi yanıt)
- `dotnet/Kuyumcu.Tests/IsCatisiTestleri.cs` (kurulum yardımcısına `IsAyarServisi`)
- `dotnet/denetim.sh` (`# >>> P-11` bloğu)

**Dokunulmayanlar (kural gereği):** `MIMARI-KALAN-PAKETLER-2026-09-15.md`, `PLAN-TEK-SEFER-2026-09-18.md`, `SONRAKI-OTURUM-PROMPT.md`, migration dosyaları, `QA-TEST-*` kiracı verileri.

---

## § 10 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| (a) Her iş kuralı için hatayı yeniden üreten test | **EVET** | 38 yeni test; her kural en az bir mutasyonla bağlandı |
| (b) Mutasyon kanıtı (kır → kırmızı → geri al → `cmp`) | **EVET** | § 7, 11 mutasyon; M11-6 açığı bulundu ve kapatıldı |
| (c) 5198'de canlı doğrulama (HTTP + SQL), 5199'a dokunulmadı | **EVET** | § 8, 18 adım; `lsof` ile 5199 boş |
| (d) `dotnet/denetim.sh` TEMİZ | **EVET** | § 7 |
| (e) Tam süit 0 başarısız | **EVET** | 3151/3151 |
| (f) Test kiracısı silindi, `QA-TEST-*` korundu | **EVET** | `tenants` 8 → 8 |
| (g) Uygulama raporu | **EVET** | bu dosya |
| Mekanizma kuralı: sayaç eklendi, tavan yalnız azaldı, taban taze ölçüldü | **EVET** | § 7 (50→47, 156→155); ölçüm komutları betikte yorum |
| Migration: HAYIR | **EVET** | `__EFMigrationsHistory` = 53 (değişmedi) |
| Tip ve sözleşme: `Pages` içinde yeni record YOK | **EVET** | `P-10: BU_PAKETTE_EKLENEN_RECORD` tavan 3'te kaldı; tipler `Kuyumcu.Contracts`'ta |
| PATCH: gönderilmeyen alan = değişmedi | **EVET** | canlı adım 3 · test `Patch_govdede_OLMAYAN_alan_DEGISMEZ` · mutasyon M11-5 |
| Değişmezler (tek yazma yolu, tek storno motoru, kiracı izolasyonu, hard DELETE yok) | **EVET** | bu paket `HareketFabrikasi`/`StornoServisi`'ne dokunmadı; yeni kayıt finansal değil; izolasyon testi `Kiraci_kapamasi_DIGER_KIRACIYI_etkilemez` |
| COMMIT YOK, git komutu yok, `rm`/`mv` yok | **EVET** | yalnız `git status` okundu |
| Sır/parola/jeton çıktıya yazılmadı | **KISMEN** | MySQL parolası hiç yazılmadı; **kayıt (`/auth/kayit`) yanıtındaki erişim jetonu bir kez terminal çıktısına düştü** — hemen fark edilip sonraki tüm çağrılarda jeton dosyaya alındı, rapora **hiçbir jeton girmedi**, ilgili kiracı **silindi** (jeton artık geçersiz). Gizlenmiyor. |
| Sözlük TR + EN, razor'da çıplak metin yok | **EVET** | `Sozlukler.cs` iki dil |
| DoD sözleşmesi bölümü | **EVET** | § 4 |
| "Geri alınamayan değişiklik" bölümü | **EVET** | § 6 sonu |
| Kimlik kapanış tablosu | **EVET** | § 2 |
| Tahmin 240 ajan-dk | **AŞILMADI** (yaklaşık bu bantta) | — |

---

## § 11 — AÇIK KALANLAR

1. **B-16 KAPANMADI** — QA-TEST-3'teki MANUAL ₺6.820,50 kalıntısı duruyor; 4 canlı hareket bağlı olduğu için uygulamanın kendi kapısı iptali reddediyor. **Devredildi: K-1 (kurucu izni).** § 6.
2. **`DIS_HATADA_VERI_DUSUREN_YOL` depo genelinde 47** — P-11'in kapsamı dışındaki 30+ ekranda catch içinde veri düşüren yol var (en yoğun: `Urunler.razor` 5, `Faturalar.razor` 5, `Cariler.razor` 4). Cırcır tavanı kuruldu; **sonraki paketler düşürmeli**. Öneri: bu kalem Y-42 triyajına "sonraki dalgada yeni paket" olarak damgalansın.
3. **`SATIR_ICI_STYLE` depo genelinde 155** — tavan kuruldu, azalma başka paketlere kaldı.
4. **Saat ayarı yalnız tur kapısıdır**, iş başına saat değildir. Kiracı "vade hatırlatması 09:00'da, MASAK uyarısı 14:00'te" diyemez. Kapsam genişletmemek için yapılmadı; ihtiyaç doğarsa 1.1.
5. **TEZGÂHTAR canlı 403'ü şube kapısından geldi** (rol kapısından değil). Rol kapısı testle ayrıca kanıtlı; ama canlı ortamda rol kapısını tek başına gözlemlemek için şubesi atanmış bir tezgâhtar gerekir — P-09'un alanı.

---

## § 12 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

**Kurucuya:**
1. **K-1 gerekiyor (B-16).** QA-TEST-3 kiracısındaki yanlış manuel kur kaydına 4 gerçek hareket bağlı. Temizlemenin iki yolu var, ikisi de sizin izninizi bekliyor: (a) QA-TEST-3 fikstürünü sıfırdan kurmak, (b) 4 hareketi storno edip kuru iptal etmek. Ekip tek taraflı karar vermedi; **kalıntı raporlanıyor, gizlenmiyor.**
2. **Jeton sızıntısı (küçük, kapatıldı).** Canlı doğrulamanın ilk adımında bir erişim jetonu terminal çıktısına düştü. Jetonun ait olduğu kiracı silindiği için jeton **geçersizdir**; rapora hiçbir sır girmedi. Yine de K-5 (sır rotasyonu) kaleminin önemi bir kez daha görüldü.
3. **Dış kur kaynakları geliştirme ortamında AÇIK.** Canlı doğrulamada Altınkaynak'tan gerçek kur çekildi (6.847,27). Üretim varsayılanı `Kur:SozlesmesizKaynaklarAcik=false`'tur (sözleşmesiz kaynak uyarısı ekranda yazılı) — bu ayrım korunmalıdır.

**Mali müşavire:** bu pakette mevzuata dayanan yeni bir hesap/sabit yazılmadı. `Isler:TevsikKurtarma` (VUK mük. md.355/4 dayanağı **DOĞRULANMAMIŞ**) **kapalı** kalmaya devam ediyor ve artık kiracı ayarıyla da **açılamaz** — açılması için önce dayanağın teyidi gerekir.
