# PAKET P-51 — Sipariş taslağında başlık kimliği düzenlenebilir (cari · tür · şube)

> Şerit: **SERİT A — SİPARİŞ + FATURA** · Kaynak: QA turu 8 triyajı (`TRIYAJ-TUR8-2026-09-23.md`, satır 82)
> Ciddiyet: **KRİTİK** · Triyaj tahmini 2,0 saat (TAHMİN, ölçüm değil)
> İzolasyon: test veritabanı `kuyumcu_test_a` · canlı doğrulama portu **5197** (5198/5199/5200'e dokunulmadı)
> Tarih: 2026-09-23

---

## 1. Ölçüm önce — triyajın doğrulayamadığı kalem KAPANDI

Triyaj § 5 md.6: *"Sipariş varlığında `SubeId` sütununun varlığı DOĞRULANAMADI (`Data/Entities.cs`
açılmadı). Sütun yoksa P-51 migration içerir."*

**Ölçüm:** varlık `Data/Entities.cs`te DEĞİL, `dotnet/Kuyumcu.Api/Data/Siparis.cs` dosyasındadır
(dosyanın kendi başlığı bunu yazıyor: *"Entities.cs'e DOKUNULMADI (çakışma azaltma)"*).
`Siparis.cs:34` → `public class Siparis : IKiraciVarlik, ISubeVarlik`, `Siparis.cs:51` →
`[Column("sube_id"), MaxLength(36)] public string? SubeId { get; set; }`.

**Sonuç: sütun VAR. MIGRATION GEREKMEDİ.** `dotnet ef` çalıştırılmadı, migration sayısı değişmedi.

Triyajın ikinci ölçülen iddiası (`SiparisModulu.cs:504-545` dallarında `cariId`/`tur`/`subeId` YOK)
**doğrulandı** — `Guncelle` yalnız `sozVerilenGun`, `aciklama`, `ustaCariId`, `emanetHareketId`,
`tahminiTutarTl`, `kurSnapshotId`, `kalemler` dallarını taşıyordu.

---

## 2. Yapılanlar

### 2.1 Sunucu — `Kuyumcu.Api/Modules/Siparis/SiparisModulu.cs`

1. **`SiparisServisi.BaslikDuzenlendiNotu = "BASLIK_DUZENLENDI"`** sabiti eklendi.
2. **`Guncelle`ye üç yeni PATCH dalı** (mevcut `Durum != TASLAK` kapısının ALTINDA — kapı DEĞİŞMEDİ):
   - `tur` → kapalı liste (`SiparisTuru.Tumu`) doğrulaması; `null` gönderimi açık 400
     ("Sipariş türü silinemez…"). Tür SİLİNEMEZ çünkü kayıt türsüz var olamaz.
   - `cariId` → boş/null açık 400 ("Cari seçimi zorunlu — sipariş carisiz bırakılamaz");
     dolu ise `IslemDogrulayici.CariDogrula` ile **kiracı kapısı** (yabancı cari → 404).
   - `subeId` → `null`/boş = SİL (şube opsiyoneldir, PATCH sözleşmesi);
     dolu ise `IslemDogrulayici.SubeDogrula` ile kiracı kapısı.
3. **Bağ doğrulaması yeni değerlere göre yapılır:** `ustaCariId` ve `emanetHareketId` dalları artık
   `s.Tur`/`s.CariId` yerine `yeniTur`/`yeniCariId` ile doğrulanır — "önce türü değiştir, sonra
   ustayı bağla" iki adımlık tuzağı doğmaz.
4. **Tür değişimi mevcut bağları geçersiz kılıyorsa SESSİZCE null'lanmaz, AÇIK 400 döner:**
   - usta carisi bağlıyken tür MUSTERI_SIPARISI'na çevrilemez (usta yalnız ATOLYE/TAMIR'de tutulur);
   - emanet girişi bağlıyken tür TAMIR dışına çevrilemez;
   - tür ATOLYE_SIPARISI'na çevriliyorsa usta carisi ZORUNLUDUR (aynı istekte gelebilir).
   Her üç metin de kullanıcıya **ne yapması gerektiğini** söyler (UX-S30).
5. **Cari değişimi emanet bağını sağlama alır:** yeni cari ile eski emanet girişi eşleşmiyorsa 400.
6. **Geçiş dökümüne append-only iz:** başlık değiştiyse `siparis_gecisleri`ne
   `onceki_durum = yeni_durum = TASLAK`, `gerekce = "BASLIK_DUZENLENDI: cari X → Y · tür A → B · şube …"`
   satırı, **aynı transaction içinde** düşer. Başlık değişmeyen PATCH iz BIRAKMAZ (gürültü yok).

### 2.2 Ekran — `Kuyumcu.Web/Pages/SiparisDetay.razor`

- Düzenleme formuna **tür · cari · şube** seçicileri eklendi (`sipd-d-tur`, `sipd-d-cari`, `sipd-d-sube`).
- **Seçenekler SAYFA AÇILIŞINDA yüklenir** (`OnInitializedAsync` → `SeceneklerYukle`): Faturalar
  ekranındaki "düzenleme açılınca boş liste" hatası TEKRARLANMADI.
- Kaydın KENDİ carisi ilk 50'nin içinde olmasa bile seçenekten DÜŞMEZ (sessiz cari kayması yok).
- **UX-S22** — tek şubede seçici çizilmez, değer salt okunur satırdır (`sipd-d-sube-sabit` + rozet).
- **UX-S29** — cari listesinin sunucu sınırı ekranda yazar (`sipd-d-cari-sinir`).
- **UX-S28/S30** — sunucunun açık gerekçesi hem hata bandında hem alanların altında
  (`sipd-d-baslik-hata`) okunur; form KAPANMAZ, girilen değer kaybolmaz.
- **DoD EK md.3** — carisiz kaydetme denemesi alan altı Türkçe mesajla durur, istek ATILMAZ.
- **Mevcut eleman kimlikleri DEĞİŞMEDİ**; yeni kimlikler `sipd-d-<alan>` önekiyle.

### 2.3 Sözleşme — `Kuyumcu.Contracts/Sayfalar/SiparisDetaySozlesmeleri.cs`
`CariSecenegi` ve `SubeSecenegi` record'ları eklendi (istemci record'u razor içine yazılmaz kuralı).

---

## 3. Sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| S1 | Spesifikasyonun işaret ettiği "**Birleşik koşullar**" bölümü `TRIYAJ-TUR8-2026-09-23.md` içinde **YOKTUR** (başlıklar § 0–§ 5). | Uydurulmadı. Bağlayıcı koşullar olarak görev metnindeki "EK KOŞULLAR / KABUL KOŞULLARI / TEST LİSTESİ / MUTASYON LİSTESİ" ve Katman 8 UX sözleşmesi alındı. |
| S2 | `Siparis` varlığı `Data/Entities.cs`te değil `Data/Siparis.cs`te. | Dosya ayak izi koda karşı doğrulandı; hedef değişmedi, migration gerekmedi. |
| S3 | Ham `select`/`input` öğeleri yerine ortak **`Secici`** bileşeni kullanıldı; cari için ayrı arama kutusu açılmadı. | `denetim.sh` P-36c `HAM_SELECT_SERBEST`/`HAM_INPUT_SERBEST` **fail-closed 0**'dır ve SiparisDetay SERBEST ekrandır. İlk yazımda ham öğeler kullanıldı, denetim KIRMIZI verdi (kanıt § 7), ortak bileşene geçildi. Arama `Secici`nin kendi kutusundadır. |
| S4 | **Tür ATOLYE_SIPARISI'na çevrilirken usta carisi ZORUNLU**, ama `Olustur`da hâlâ opsiyonel. | Kabul koşulu bunu açıkça istiyor. Ancak bu, UX-S26 ("oluşturma formu düzenleme formuyla aynı alan kümesini taşır") ile **gerilim** yaratır. Mevcut kod değiştirilmedi (izinsiz değişiklik yasağı). **Kurucuya/Bertan'a not:** ya `Olustur` da aynı zorunluluğu almalı, ya bu zorunluluk kaldırılmalı. Karar verilmeden ikisi farklı kalıyor — § 12'de açık kalem. |
| S5 | Razor yorumlarında `select`/`input` sözcükleri köşeli parantezsiz yazıldı. | `denetim.sh` P-36 `HAM_SELECT`/`HAM_INPUT` sayaçları **ham grep**'tir, yorumları AYIKLAMAZ (P-36c sayaçları ayıklar). Yorumdaki `<select>` sözcüğü sayaca giriyor ve sayacı yalancı yapıyordu; gerçek bir ham öğe YOKTUR (ölçüm: SiparisDetay.razor'da `<select|input|textarea>` = **0**). Bu bir sayaç kaçamağı değil, sayacın bilinen naifliğine karşı yazım tercihidir ve yorumda gerekçesi yazılıdır. |

**Mevzuat/yasal hüküm:** bu paket hiçbir mevzuat hükmü içermez (sipariş anında para el değiştirmez;
tevsik/MASAK eşiği burada ölçülmez — modül başlığındaki mevcut beyan korundu). Mali müşavire not yok.

---

## 4. Migration

**GEREKMEDİ.** `siparisler.sube_id` sütunu zaten vardı (`Data/Siparis.cs:51`), `cari_id` ve `tur`
sütunları zaten vardı. `dotnet ef` hiç çalıştırılmadı; migration sayısı **66 → 66** (değişmedi).
Geçiş izi için yeni tablo/kolon AÇILMADI: mevcut `siparis_gecisleri.gerekce` alanı kullanıldı ve
`SiparisDurumu` kapalı listesi **genişletilmedi** (durum enflasyonu yasağı).

---

## 5. Testler

| Kapsam | Önce | Sonra | Yeni |
|---|---|---|---|
| `SiparisTestleri` (DB/servis) | 33 | **38** | +5 |
| `P51EkranTestleri` (bUnit, yeni dosya) | 0 | **5** | +5 |
| **Toplam yeni test** | | | **10** |

**Yeni testler (spesifikasyon listesi birebir):**
- `TaslakSiparisinCarisiDegistirilebilir`
- `TaslakDisiSiparisteBaslikPatchReddedilir`
- `TurDegisiminceGecersizBagAcikHataVerir`
- `SubeDegisimiKiraciDisiSubeyiReddeder`
- `BaslikDegisimiGecisDokumuneYazilir`

**Ekran testleri:**
- `Duzenleme_formunda_cari_tur_sube_secicileri_dolu_gelir`
- `Secilen_yeni_cari_ve_tur_PATCH_govdesinde_gider`
- `Tek_subede_secici_cizilmez_deger_salt_okunur_satirdir`
- `Sunucu_baslik_degisimini_reddederse_alan_alti_mesaj_cikar_form_kapanmaz`
- `Cari_secilmeden_kaydedilirse_alan_alti_hata_cikar_ve_istek_atilmaz`

Filtreli koşumlar: `SiparisTestleri` 38/38 yeşil · `P51EkranTestleri` 5/5 yeşil
(`KUYUMCU_TEST_DB=kuyumcu_test_a`).

**TAM SÜİT** (`KUYUMCU_TEST_DB=kuyumcu_test_a dotnet test … --logger trx`):

```
Başarılı!  - Başarısız: 0, Başarılı: 3985, Atlanan: 0, Toplam: 3985, Süre: 4 m 33 s
```

**0 başarısız.** (3985 sayısı, aynı anda koşan diğer şeritlerin eklediği testleri de kapsar;
paket öncesi kapı kaydı 3869 idi — aradaki fark yalnız P-51'e ait değildir.)
trx: `…/scratchpad/trx-pP-51/`

---

## 6. Mutasyon kanıtları

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı |
|---|---|---|---|
| **M-1** | `Guncelle`deki `if (s.Durum != SiparisDurumu.Taslak)` → `if (false)` | `TaslakDisiSiparisteBaslikPatchReddedilir` · (ayrıca mevcut `TESLIM_EDILDI_sonrasi_PATCH_reddedilir`, `HttpSiparisTestleri.PATCH_TASLAK_disinda_400_doner`) | `cmp` → **birebir aynı** |
| **M-2** | `cariId` dalındaki kiracı kapısı (`IslemDogrulayici.CariDogrula(db, c, islem.TenantId)`) silindi | `SubeDegisimiKiraciDisiSubeyiReddeder` (testin içinde şube **ve** cari eşdeğeri birlikte ölçülür) | `cmp` → **birebir aynı** |
| **M-3** | Tür değişiminde bağ doğrulaması atlandı (`if (yeniTur != s.Tur)` → `if (false)`) | `TurDegisiminceGecersizBagAcikHataVerir` | `cmp` → **birebir aynı** |

Her mutasyondan sonra derleme yapıldı (0 hata), test koşuldu, yedek geri kopyalandı ve `cmp` ile
birebir aynılık KANITLANDI. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln` → **0 hata, 0 uyarı**.
Test açığı bulunmadı (her mutasyon en az bir testi kırdı).

---

## 7. Denetim (`dotnet/denetim.sh`)

| Koşum | Sonuç | Not |
|---|---|---|
| 1. koşum (ham select/input ile) | **BAŞARISIZ** | `P-36c: HAM_SELECT_SERBEST 3`, `HAM_INPUT_SERBEST 1` — **benim ihlalimdi**, gizlenmedi |
| 2. koşum (ortak `Secici`ye geçildi) | **BAŞARISIZ** | `P-36: HAM_SELECT 31 > 30` — kaynak: razor yorumumdaki `<select>` sözcüğü (sayaç yorum ayıklamaz) |
| 3. koşum (yorum yazımı düzeltildi) | **DENETİM TEMİZ** | `HAM_SELECT 30 (tavan 30)` · `HAM_INPUT 42 (tavan 42)` · `HAM_SELECT_SERBEST 0` · `HAM_INPUT_SERBEST 0` |

Ölçüm: `SiparisDetay.razor` içinde `<select|input|textarea>` sayısı = **0**.

---

## 8. Canlı doğrulama (localhost:**5197** · `kuyumcu_net`)

Test kiracısı: **`PAKETP-51-TEST`** (`01M375WTBKZ47AHMTM3X2MTXHT`) — iş sonunda tamamen silindi.
**5199 portuna DOKUNULMADI** (koşum boyunca ve sonrasında ayakta). 5198/5200 kullanılmadı.

| # | İstek | Beklenen | Gerçek | Yanıt (özet) |
|---|---|---|---|---|
| 1 | `POST /siparisler` (TASLAK, cari A) | 200 | **200** | `S-2026-000001`, `tur=MUSTERI_SIPARISI`, `subeId=<merkez>` |
| 2 | `PATCH /siparisler/{id}` `{"cariId":"<cari B>"}` | **200** | **200** | `cariId=<B>`, `cariAd="P51 Musteri B"` |
| 3 | `GET /siparisler/{id}` | 200 | **200** | yeni cari GÖRÜNÜYOR: `cariId=<B>`, `cariAd="P51 Musteri B"` |
| 4 | `PATCH` `{"tur":"ATOLYE_SIPARISI"}` (usta boş) | **400** | **400** | `Tür ATOLYE_SIPARISI olarak değiştirilemez: dış atölye siparişinde işi kimin yaptığı (usta carisi) boş bırakılamaz. Aynı istekte ustaCariId gönderin.` |
| 5 | `PATCH` `{"subeId":"<kendi 2. şube>"}` | 200 | **200** | `subeId=<şube-2>` |
| 6 | `PATCH` `{"subeId":"<YABANCI kiracının şubesi>"}` | **404** | **404** | `Şube bulunamadı` |
| 7 | `POST /siparisler/{id}/gecis` → ONAYLANDI | 200 | **200** | durum ONAYLANDI |
| 8 | `PATCH` `{"cariId":…}` (TASLAK DIŞI) | **400** | **400** | `Yalnız TASLAK durumundaki sipariş düzenlenebilir (bu sipariş: **ONAYLANDI**)` — metin **durum adını içeriyor** |
| 9 | `GET /siparisler/{id}/gecisler` | 200 | **200** | 3 satır: doğuş + `BASLIK_DUZENLENDI: cari X → Y` + `BASLIK_DUZENLENDI: şube X → Y` |

**SQL kanıtı (`kuyumcu_net`):**

| Sorgu | Sonuç |
|---|---|
| `SELECT cari_id, tur, sube_id, durum FROM siparisler WHERE id=…` | `<cari B>`, `MUSTERI_SIPARISI`, `<şube-2>`, `ONAYLANDI` → reddedilen tür/cari PATCH'leri **hiçbir şey yazmadı** |
| `SELECT COUNT(*) FROM siparis_gecisleri WHERE siparis_id=…` | `4` (doğuş + 2 başlık izi + ONAYLANDI geçişi) |
| `… AND gerekce LIKE 'BASLIK_DUZENLENDI%'` | `2` |
| `SELECT COUNT(*) FROM hareketler WHERE tenant_id=…` | **`0`** — sipariş modülü deftere YAZMADI |
| `SELECT COUNT(*) FROM kasa_hareketleri WHERE tenant_id=…` | **`0`** |
| `SELECT varlik_turu, eylem, COUNT(*) FROM denetim_kayitlari …` | `Siparis GUNCELLEME 3` · `SiparisGecisi EKLEME 4` → **denetim izi kendiliğinden düştü** (DenetimYazmaDinleyicisi; ikinci yazım yapılmadı) |

**Temizlik:**
- API PID durduruldu · `lsof -nP -iTCP:5197 -sTCP:LISTEN` → **boş (0 satır)**.
- `tenant_id` kolonu olan **70 tablonun tamamından** kiracı satırları silindi; doğrulama sorgusu
  (her tabloda `COUNT(*)>0 HAVING`) → **hiç satır dönmedi**.
- Canlı doğrulama için eklenen `kiraci_davetleri` satırı silindi (`COUNT(*)=0`).
- `tenants` sayısı: **başlangıç 9 → son 9** ✅
- `QA-TEST-` önekli kalıcı QA fikstürlerine **dokunulmadı**; 5199 sunucusuna dokunulmadı.

---

## 9. Değişen dosyalar

| Dosya | Tür |
|---|---|
| `dotnet/Kuyumcu.Api/Modules/Siparis/SiparisModulu.cs` | değişti |
| `dotnet/Kuyumcu.Web/Pages/SiparisDetay.razor` | değişti |
| `dotnet/Kuyumcu.Contracts/Sayfalar/SiparisDetaySozlesmeleri.cs` | değişti |
| `dotnet/Kuyumcu.Tests/SiparisTestleri.cs` | değişti (+5 test) |
| `dotnet/Kuyumcu.Tests/P51EkranTestleri.cs` | **YENİ** |

`Kuyumcu.Core/I18n/Sozlukler.cs`'e **DOKUNULMADI** (şerit kuralı): yeni ekran metni gerekmedi —
kullanılan tüm anahtarlar (`Siparis.Tur`, `Siparis.Cari`, `Siparis.Sube`, `Siparis.SubeOturumdan`,
`Siparis.CariGerekli`, `Siparis.SinirUyarisi`, `TurMusteri/TurAtolye/TurTamir`) SÖZLÜKTE ZATEN VARDI.
Sunucunun 400 metinleri ekranda olduğu gibi gösterilir (tek kaynak).
Şerit dışı dosyalara (denetim.sh, Kasa/Vezne/Maliyet/Rapor, yayın betikleri, Yedekler, Kurulum)
dokunulmadı.

**Dokunulan sayfalar (DoD EK 2 — TEK DOKUNUŞ):** `Pages/SiparisDetay.razor`.
⚠ P-52 aynı sayfaya dokunacaktır (triyaj § 3.3: `P-51 → P-52` zinciri); bu **planlı** ikinci
dokunuştur ve gerekçesi triyaj çakışma haritasında yazılıdır.

---

## 10. Koşul karşılama tablosu

| Kabul koşulu | Durum | Kanıt |
|---|---|---|
| TASLAK siparişte `PATCH {cariId}` 200 döner, `GET` yeni cariyi gösterir | ✅ | Canlı #2, #3 · `TaslakSiparisinCarisiDegistirilebilir` |
| TASLAK dışı siparişte aynı PATCH 400 döner ve metin **durum adını** içerir | ✅ | Canlı #8 (`… bu sipariş: ONAYLANDI`) · `TaslakDisiSiparisteBaslikPatchReddedilir` |
| `PATCH {tur:'ATOLYE_SIPARISI'}` ustaCariId boşken açık hata, kayıt değişmez | ✅ | Canlı #4 + SQL (`tur` hâlâ `MUSTERI_SIPARISI`) · `TurDegisiminceGecersizBagAcikHataVerir` |
| Düzenleme formunda cari/tür/şube seçicileri en az 1 seçenekle dolu gelir (boş liste yasak) | ✅ | `Duzenleme_formunda_cari_tur_sube_secicileri_dolu_gelir` (tür 3, cari ≥1 + kaydın carisi seçili, şube 2) |
| Başlık değişimi sonrası `/gecisler` dökümünde değişiklik kaydı görünür | ✅ | Canlı #9 + SQL (`baslik_izi = 2`) · `BaslikDegisimiGecisDokumuneYazilir` |
| Tür değişimi mevcut bağları geçersiz kılıyorsa açık 400, sessiz null'lama yok | ✅ | `TurDegisiminceGecersizBagAcikHataVerir` (a/b/c şıkları) |
| Şube: varlık + kiracı doğrulaması | ✅ | Canlı #5, #6 · `SubeDegisimiKiraciDisiSubeyiReddeder` |
| Mevcut 509-511 TASLAK kapısı korundu | ✅ | Kod değişmedi; M-1 mutasyonu kapının canlı olduğunu kanıtlıyor |
| Migration gerekip gerekmediği İLK İŞ ölçüldü | ✅ | § 1 |
| `dotnet build Kuyumcu.sln` 0 hata 0 uyarı | ✅ | § 11 |
| `denetim.sh` TEMİZ | ✅ | § 7 |
| Test kiracısı silindi, `tenants` sayısı döndü | ✅ | § 8 temizlik (9 → 9) |

### DoD sözleşmesi (Bertan koşulu)

Bu paket **yeni varlık/kayıt türü DOĞURMAZ**; mevcut `Siparis` varlığının üç alanını düzenlenebilir
yapar. Yine de sözleşme tablosu:

| Varlık | Düzenleme yolu | Silme / pasife alma | Yetki | Denetim | Ekranda eylem |
|---|---|---|---|---|---|
| `Siparis` (başlık: cari · tür · şube) | `PATCH /api/v1/siparisler/{id}` — gövdede yoksa KORU, `subeId: null` ise SİL | Hard DELETE **YOK**; vazgeçilen sipariş `IPTAL` durumuna geçirilir (gerekçe ≥10 karakter) | `[Yetki.ModulYetkisi("siparis")]` + `[RolGerekli("SAHIP","MUDUR","MUHASEBE","TEZGAHTAR")]` (mevcut, değişmedi) | `DenetimYazmaDinleyicisi` SaveChanges'ı dinler → `Siparis GUNCELLEME` kaydı kendiliğinden düşer (canlı kanıt: 3 kayıt). İkinci yazım YAPILMADI (çift kayıt olurdu) | `sipd-duzenle` → tür/cari/şube seçicileri + `sipd-d-kaydet` |
| `SiparisGecisi` (başlık izi) | **SALT OKUNUR — DegismezIpucu gerekçesi:** append-only'dir; `KuyumcuDbContext` bu varlıkta `Modified`/`Deleted` görürse `SiparisGecisiDegistirilemez` fırlatır. "Değiştirilebilen geçiş izi, iz değildir." | yok | `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` (GET) | kendisi izdir | geçiş dökümü kartı |

**Finansal varlık dokunuşu YOK:** `hareketler`, `kasa_hareketleri`, `banka_hareketleri`, `faturalar`
tablolarına bu pakette tek satır yazılmaz (canlı SQL kanıtı: 0/0).

**DoD EK md.1 (her düğme ya istek atar ya disabled):** bu pakette yeni **düğme** eklenmedi; eklenen
üç öğe `Secici` bileşenidir (kendi sözleşmesi ve testleri var). `sipd-d-kaydet` düğmesinin iki yolu
da ölçüldü: geçerli formda PATCH atar (`Secilen_yeni_cari_ve_tur_PATCH_govdesinde_gider`), carisiz
formda alan altı mesaj verir ve **istek atmaz** (`Cari_secilmeden_kaydedilirse…`).

**DoD EK md.2 (tek düzenleme kalıbı):** `ModalPencere` düzenleme için KULLANILMADI (bu ekranda modal
yalnız yıkıcı İPTAL onayındadır, değişmedi). İkinci bir `duzenlenenId` kalıbı açılmadı.

---

## 11. Derleme

```
dotnet build Kuyumcu.sln --nologo
    0 Hata
    0 Uyarı
```

---

## 12. Açık kalanlar / kurucuya notlar

1. **(Bertan/kurucu kararı) ATOLYE_SIPARISI ↔ usta carisi asimetrisi.** Kabul koşulu gereği *tür
   değişiminde* usta carisi ZORUNLU yapıldı, ama `Olustur`da hâlâ OPSİYONEL. UX-S26 iki formun aynı
   alan kümesini istemesini emreder. **Karar gerekiyor:** (a) `Olustur` da zorunlu kılınsın, ya da
   (b) tür değişimindeki zorunluluk kaldırılsın. İzinsiz değişiklik yasağı gereği mevcut `Olustur`
   koduna dokunulmadı.
2. **Cari listesi sınırı 50.** Ekranda yazılı (UX-S29) ve `Secici`nin kendi arama kutusuyla daraltılır,
   ama arama **istemcide** yapılır (yalnız çekilen 50 kayıt içinde). Sunucu tarafı arama için
   `Secici`nin `UzakTur` kipi vardır; `/arama` ucunda cari türü olup olmadığı **DOĞRULANMADI** —
   ayrı paket işi.
3. **Eşzamanlı şerit gürültüsü (Anayasa md.1 — gizlenmiyor):** koşum sırasında `Kuyumcu.Tests`
   bir kez **benim dışımdaki** bir sebeple derlenmedi
   (`YedekEkranTestleri.cs(109,72): error CS1061: 'sonGerekce' …` — YEDEK/KUR şeridinin P-56 çalışması).
   Hata kendiliğinden kapandı; P-51 kodu etkilenmedi. Ayrıca `denetim.sh` HAM_INPUT sayacı bir ara
   43 → 44'e çıktı (benim dosyamda 0 iken) ve tekrar 42'ye döndü — başka şeridin ara durumu.
4. **Mali müşavire not YOK** — bu pakette mevzuat hükmü yoktur.
