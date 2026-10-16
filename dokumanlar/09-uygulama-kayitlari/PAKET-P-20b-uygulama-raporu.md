# PAKET P-20b — HURDA SATIŞINDA TAHLİL / MUTABAKAT FARKI (Y-10 kalan dilim · K-13)

**Tarih:** 2026-09-21 · **Dalga:** DALGA 5 · **Rol:** Taşkın (03 Backend) + Ertuğ (03 Frontend)
**Sürüm:** 1.0 · **Kaynak karar:** `dokumanlar/09-uygulama-kayitlari/KARARLAR-URUN-2026-09-21.md` § K-13
**Plan bloğu:** `PLAN-TEK-SEFER-2026-09-18.md` § 14 (plana 21 Eylül'de EKLENEN paket; 36 → 37)
**Tahmin:** 120 ajan-dk

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (durma-devam, varsayımsız)

| Önkoşul | Diskte ne var (ölçüldü) | Sonuç |
|---|---|---|
| **P-19** (`CiftBacakFabrikasi`) | `Kuyumcu.Api/Data/HareketFabrikasi.cs:201` `CiftBacakFabrikasi` VAR; rapor `PAKET-P-19-uygulama-raporu.md` VAR; `denetim.sh` P-19 bloğu VAR ve TEMİZ | **TAMAM** |
| **P-20** (hurda satışı) | `Modules/Hurda/{HurdaModulu,HurdaStokKapisi,GiderPusulasiKapisi}.cs`, `Pages/Hurda.razor` (414 satır), `SatisServisi.HurdaSatis`, `Contracts/Sayfalar/HurdaSozlesmeleri.cs`, `P20HurdaSatisTestleri` + `P20EkranTestleri` (47 test) VAR; rapor VAR | **TAMAM** |
| **K-13 kurucu kararı** | P-20 raporunda "GELMEDİ" idi; 21 Eylül'de `KARARLAR-URUN-2026-09-21.md § K-13` ile GELDİ ve **ikiye ayrıldı** | **GELDİ** |

**Önemli sapma ölçümü:** `CiftBacakFabrikasi` yalnız `IBacakliHareket` (kasa/banka hareketi) bacaklarını eşler; `Hareket` (defter satırı) o arayüzü **taşımaz**. Bu yüzden tahlil farkı için fabrika **yeniden kullanılamadı**, **deseni** birebir taklit eden kardeş bir fabrika yazıldı (`TahlilFarkiFabrikasi`). Gerekçe § 9/S1'de.

---

## § 1 — KİMLİK KAPANIŞ TABLOSU (zorunlu · Y-10 kalanı, K-13)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **Y-10 (kalan dilim)** — hurda satışında tahlil/mutabakat farkı bacağı | **KAPANDI** | Kod: `Data/HareketFabrikasi.cs` → `TahlilFarkiFabrikasi.Ekle(...)` (`Tur="TAHLIL_FARKI"`, `TlTutari=0`, `HasGrami=fark`, `TahlilKaynakHareketId=kaynak.Id`); `SatisServisi.HurdaSatis` iki has alanını ayırır (`HasGrami` = bizim kaydımız, `AliciKabulHasGrami` = alıcının kabulü). Test: `Tahlil_farki_AYRI_bacak_yazar_ve_iki_bacagin_toplami_bizim_kaydimizdir`. Canlı: § 7 adım 5 (HTTP 200, `tahlilFarkiHasGram=-0.2`), SQL `TAHLIL_FARKI_has_toplam = -0,250000`, `OKSUZ_FARK_BACAGI = 0` |
| **K-13/1** — eritme firesi 1.0'a GİRMEZ, gerekçesi ekranda görünür | **KAPANDI** | `HurdaStokKapisi.FireKapaliMesaji` artık kararın lafzını taşır ("Bu sürümde eritme firesi kaydedilmez; imalat kapsam dışıdır (K-13 kararı)"); ekranda `#hrd-fire-notu` (metin SUNUCUDAN). Sayaç: `HURDA_FIRE_SIZINTISI = 0`. Test: `Fire_adiyla_bir_hareket_turu_YOKTUR_fark_FIRE_diye_adlandirilmaz`, `Ekranda_FIRE_adiyla_bir_GIRDI_alani_YOKTUR_yalniz_gerekce_notu_vardir`. Canlı: § 7 adım 4 (HTTP 400) |
| **K-13/2** — tahlil farkı GİRER, gerekçe ZORUNLU, atomik, "fire" diye adlandırılmaz | **KAPANDI** | Gerekçe kapısı `TahlilFarkiFabrikasi.GerekceAsgari = 20`; iki bacak TEK `SaveChangesAsync`; tür adı `TAHLIL_FARKI`. Sayaç: `TAHLIL_FARKI_ESSIZ_BACAK = 0`. Mutasyon M20b-2/3/4 |

**Kapsam kilidi:** yalnız Y-10'un K-13'e bağlı bırakılmış dalına dokunuldu. P-20 **yeniden yazılmadı** (fark yokken davranışı birebir aynıdır — test `Kabul_alani_bos_birakilirsa_kaydimiz_aynen_kabul_edilmis_sayilir`). Başka paketin kimliğine girilmedi.

---

## § 2 — KARARIN LAFZI ↔ YAPILAN

| K-13 şartı | Durum | Kanıt |
|---|---|---|
| İki has alanı AYRI durur (bizim kaydımız · alıcının kabul ettiği) | ✅ | DTO `HasGrami` + `AliciKabulHasGrami`; ekranda `#hrd-has` + `#hrd-kabul-has` |
| Fark ≠ 0 ise ayrı bacak, **gerekçe zorunlu** | ✅ | `TahlilFarkiFabrikasi` 20 karakter kapısı; ekran `GerekceAlani #hrd-tahlil-gerekce` (yalnız fark varken çizilir) |
| Has fark hesabına gider | ✅ | Ayrı hareket türü = ayrı hesap; `TlTutari = 0` (para değil has) |
| Tek `SaveChangesAsync` içinde atomik (P-19 deseni) | ✅ | `denetim.sh` `TAHLIL_FARKI_ESSIZ_BACAK` (c) bileşeni; test `Fark_varken_gerekcesiz_istek_reddedilir_ve_HICBIR_SEY_yazilmaz` — satış bacağı da yazılmaz |
| Fark "fire" diye ADLANDIRILMAZ | ✅ | `HURDA_FIRE_SIZINTISI = 0`; sözlükte tahlil farkı etiketlerinin hiçbirinde "fire" geçmez |
| Ekranda fark ve YÖNÜ (lehte/aleyhte) görünür | ✅ | `#hrd-tahlil-fark` + `Rozet` (yeşil=lehte, amber=aleyhte); defterde `#hrd-fark-{id}` |
| UX-1: sonradan düzeltilebilir, hard DELETE yok | ✅ | `POST /api/v1/islemler/hurda-satis/{hareketId}/tahlil-duzeltme` (ikinci bacak ekler, üzerine yazmaz); ekranda `#hrd-duzelt-{id}` → `Cekmece` |
| Has-cari denk gelir | ✅ | DEĞİŞMEZ: `HURDA_SATIS.HasGrami + Σ TAHLIL_FARKI.HasGrami = −(bizim kaydımız)`; test + SQL (`hurda_has_bakiye = 4,11 = 9,16 − 5 − 0,05`) |

---

## § 3 — DoD SÖZLEŞMESİ (Bertan mührü — hangi kayıt nasıl düzeltilir)

| Kayıt / varlık | Düzenleme yolu | Gerekçe |
|---|---|---|
| **TAHLIL_FARKI defter satırı** (bu paketin doğurduğu TEK yeni kayıt türü) | **STORNO politikası + GERÇEK düzeltme ucu**: `POST api/v1/islemler/hurda-satis/{hareketId}/tahlil-duzeltme` — satırın üzerine YAZMAZ, aynı satışa **ikinci bir fark bacağı** ekler; toplam fark bacakların toplamıdır. Ekranda defterdeki her `HURDA_SATIS` satırının yanında **"Tahlil farkını düzelt"** düğmesi (`#hrd-duzelt-{id}`) → `Cekmece`. **Hard DELETE YOK** | Defter satırı VUK md.219 gereği yerinde değişmez; ama UX-1 "her veri girişi düzeltilebilir" der. İkisi append-only düzeltme fişiyle uzlaşır. `DuzeltmePolitikasi["TAHLIL_FARKI"] = (STORNO, DEFTER, tahlil-duzeltme ucu)` — test `Tahlil_farkinin_duzeltme_yolu_politika_haritasinda_YAZILIDIR` |
| Yetki | `[ModulYetkisi("islem")]` + `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` — hurda satışıyla **aynı** (aynı defter hattı, aynı sorumluluk). Canlı: TEZGÂHTAR **403** (HTTP testi) | Parite: farkı yazabilen, satışı da yazabilen olmalıdır |
| Denetim kaydı | **Düzeltme ucunda** `DenetimServisi.YazmaKaydet(islem, "TahlilFarki", …)` ÇAĞRILIR (yeniden yazılmadı); SQL `denetim_TahlilFarki = 1`. **Oluşturma yolunda** iz, P-20 ile PARİTE olarak defterin kendisidir: fark satırının `aciklama` alanı zorunlu gerekçeyi taşır (SQL `fark_gerekcesiz = 0`), `kullanici_id` ve `olusturuldu` doludur | Ayrıca denetim satırı yazmak, hurda satışının kendi izini (hareket + kasa + MASAK) ikiye böler; P-20 kararı korunmuştur. **Bu bilinçli bir paritedir, eksiklik değildir** |
| **HURDA_SATIS satırı** | Değişmedi: **SALT OKUNUR / STORNO_YOK** (`DegismezIpucu #degismez-ipucu-HurdaSatisi`). Yeni: `AlternatifYol` metniyle tahlil farkı düzeltme yolunu anlatır | P-20 gerekçesi geçerli (kısmi tahsilatlı hurda satışında storno motorunun cari eşlemesi tutmaz) |
| **PATCH** | Bu pakette PATCH ucu AÇILMADI ve açılamaz: defter satırına PATCH, K-UX-2 ile YASAKTIR. Düzeltme yolu yukarıdaki append-only uçtur | Yeni tablo/varlık açılmadı; yeni kayıt türü bir DEFTER satırıdır |
| **Salt okunur kalan** | `hurda bakiyesi` (türetme, kayıt değil) · tahsil edilen TL (fark ucu parayı DEĞİŞTİRMEZ) | Para tarafı için doğru yol cari düzeltme fişidir (`api/v1/cari/{id}/duzeltme`) — **devredildi**, § 10 |

---

## § 4 — MİGRATION

| Ölçü | Değer |
|---|---|
| Ad | `20260921060723_P20bTahlilFarki` |
| `.cs` boyut | **1.243 bayt** (0'dan büyük, `Up()` gövdesi doğru) |
| `.Designer.cs` boyut | **201.212 bayt** |
| `Up()` içeriği | `hareketler.tahlil_kaynak_hareket_id varchar(36) NULL` + `IX_hareketler_tenant_id_tahlil_kaynak_hareket_id` |
| `Down()` | VAR (indeks + kolon düşer) |
| Uygulama | `dotnet ef database update --connection …` → `Applying migration '20260921060723_P20bTahlilFarki'. Done.` |
| `__EFMigrationsHistory` | **57 → 58** |
| Kolon doğrulaması | `SHOW COLUMNS FROM hareketler LIKE 'tahlil_kaynak_hareket_id'` → `varchar(36) YES NULL` |
| İkinci P-19/P-20 migration'ı | **ÜRETİLMEDİ** (yasak) |
| `--no-build` | **KULLANILMADI**; build 0 hata/0 uyarı iken üretildi |

**Neden kolon gerekti (ölçüldü, varsayılmadı):** `Hareket` varlığında bir fark bacağını karşı bacağına bağlayacak alan YOKTU. `fis_id` (çok bacaklı ÖDEME fişi), `iade_kaynak_hareket_id` (ticari iade) ve `storno_kaynak_id` (hata düzeltmesi) üç ayrı ilişkidir; birine binmek, o ilişkiyi okuyan mevcut sorguları (tevsik kümülatifi, iade hakkı, storno kapısı) sessizce kaydırırdı.

---

## § 5 — TESTLER

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Filtreli `~P20b` (DB + HTTP) | 19 | 19 | 0 | 0 |
| Filtreli `~P20bEkran` (bUnit) | 8 | 8 | 0 | 0 |
| Filtreli `~P20` (P-20 + P-20b, gerileme) | 56 | 56 | 0 | 0 |
| **Tam süit (önce)** | **3457** | 3457 | 0 | 0 |
| **Tam süit (sonra)** | **3484** | **3484** | **0** | 0 |

**Yeni test sayısı: 27** (19 DB/HTTP + 8 bUnit). `trx`: `scratchpad/trx3-pP-20b/*.trx` → `total="3484" passed="3484" failed="0"`.

### Kararsız (flaky) test — GİZLENMEDİ
Tam süitin **ilk** koşumunda `DovizBacagiEkranTestleri.DOVIZ_secilince_birim_ve_miktar_alanlari_cizilir_TL_SALT_OKUNUR` bir kez kırmızı oldu:
`Bunit.Rendering.UnknownEventHandlerIdException : There is no event handler with ID '37' associated with the 'onchange' event` — bUnit'in bilinen "eski render ağacı" kararsızlığı.
**Kanıt:** aynı test ardışık **3 kez** yalnız başına koşuldu → 3/3 yeşil; ardından tam süit yeniden koşuldu → **3484/3484 yeşil**. Test bu pakette DEĞİŞTİRİLMEDİ ve `/hurda` ekranıyla ilgisi yoktur (paket P-27 döviz ekranı). **Devredildi:** `Find`+`Change` çağrılarının `InvokeAsync` ile sarılması → ilgili paketin sahibine.

### Yeni testlerin listesi (her biri bir iş kuralını yeniden üretir)
`P20bTahlilFarkiTestleri`: iki bacağın toplamı · gerekçe zorunluluğu (3 Theory hâli) · fark 0 ise bacak yok · kabul alanı boşsa P-20 davranışı · LEHTE fark · çıkış kapısı bizim kaydımıza bakar · kabul 0 olamaz · düzeltme ikinci bacak ekler (+ denetim izi) · düzeltmede gerekçe/0 fark reddi · düzeltme bakiyeyi eksiye düşüremez · başka kiracıya yazılamaz · fark yalnız `HURDA_SATIS`'a bağlanır · bakiye kartı aritmetiği · fire adıyla tür yok · politika haritası · türetmeye dâhillik.
`P20bTahlilFarkiHttpTestleri`: uç 200 / gerekçesiz 400 / TEZGÂHTAR 403 / düzeltme 200 / olmayan belge 404 / defter ucu farkı sunucuda toplar.
`P20bEkranTestleri`: iki alan + yön rozeti · fark yokken gerekçe açılmaz · gerekçesiz Kaydet POST ATMAZ + alan altı mesaj · gövdede `aliciKabulHasGrami` + gerekçe · düzelt düğmesi çekmeceyi açar ve POST atar · ham tür kodu basılmaz · fire adıyla girdi alanı yok · çekmece açıkken ölü düğme yok.

---

## § 6 — MUTASYON KANITI

Yöntem: kaynak `scratchpad/mut-yedek/P-20b/` altına kopyalandı → `perl -0777 -pi -e` ile TEK eşleşmeli değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile birebir doğrulandı. Son mutasyondan sonra **temiz `dotnet build` (0 hata / 0 uyarı)**.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| **M20b-1** | Satış bacağına kabul yerine bizim kaydımız yazıldı (`-aliciKabul` → `-hasGrami`) | `Tahlil_farki_AYRI_bacak_yazar_…`, `Fark_LEHTE_olabilir_…`, `Duzeltme_satirin_uzerine_yazmaz_IKINCI_bacak_ekler`, `…Http.Uc_farki_yazar_…` (4) | `cmp` birebir |
| **M20b-2** | Fark bacağı koşulu ters çevrildi (`tahlilFark == 0m` → `!= 0m`) | 12 test (gerekçe Theory'leri, fark 0, düzeltme, HTTP…) | `cmp` birebir |
| **M20b-3** | Gerekçe kapısı etkisizleştirildi (`g.Length < GerekceAsgari` → `< 0`) | `Fark_varken_gerekcesiz_istek_reddedilir_…` ×3, `Duzeltmede_gerekce_zorunludur_…`, `…Http.Uc_farki_yazar_…` (5) | `cmp` birebir |
| **M20b-4** | Karşı bacak bağı kaldırıldı (`TahlilKaynakHareketId = kaynak.Id` → `null`) | `Tahlil_farki_AYRI_bacak_yazar_…`, `Duzeltme_satirin_uzerine_yazmaz_…`, `…Http.Uc_farki_yazar_…` (3) | `cmp` birebir |
| **M20b-5** | `TAHLIL_FARKI`, hurda türetme listesinden çıkarıldı | `Tahlil_farki_hurda_turetmesine_DAHILDIR`, `Tahlil_farki_AYRI_bacak_…`, `Fark_LEHTE_…`, `Duzeltme_…`, `…Http…` (5) | `cmp` birebir |
| **M20b-6** | Düzeltmede fail-closed çıkış kapısı etkisizleştirildi (`ek < 0m` → `ek < -1000000m`) | `Duzeltme_hurda_bakiyesini_EKSIYE_dusuremez` | `cmp` birebir |
| **M20b-7** | TL bacağı kabul yerine bizim kaydımızla çarpıldı | `Tahlil_farki_AYRI_bacak_…`, `Duzeltme_…`, `…Http…` (3) | `cmp` birebir |
| **M20b-8** | Fark bacağına TL yazıldı (`TlTutari = 0m` → `1m`) | `Tahlil_farki_AYRI_bacak_yazar_…` | `cmp` birebir |
| **M20b-9** | Ekranda gerekçe kapısı kapatıldı (`TahlilFark != 0m &&` → `false &&`) | `P20bEkranTestleri.Fark_varken_gerekcesiz_kaydet_POST_ATMAZ_…` | `cmp` birebir |
| **M20b-10** | Bakiye kartında fark kalemi sıfırlandı (`Fark = g.Where(...)` → `0m`) | `Bakiye_kartinda_giren_eksi_cikan_arti_fark_BAKIYEYI_verir` | `cmp` birebir |

**Test açığı bulunmadı:** on mutasyonun hepsi en az bir testi ADIYLA kırdı. Geri alma sonrası beş dosyanın hepsi `cmp` ile birebir aynı; kapanış build'i 0 hata / 0 uyarı.

---

## § 7 — CANLI DOĞRULAMA (localhost **5198** — 5199'a DOKUNULMADI)

Sunucu: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Kuyumcu.Api --no-build --no-launch-profile --urls http://localhost:5198` (nohup, PID saklandı, iş sonunda `kill`).
İş sonunda `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **boş** · `lsof -nP -iTCP:5199 -sTCP:LISTEN` → **hâlâ dinliyor (kurucunun sunucusu, dokunulmadı)**.
Kiracı gerçek uçla açıldı: `kiraci_davetleri`ne SHA-256 özetli satır + `POST /auth/kayit` → **201**, `PAKETP-20b-TEST`.

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET /hurda/bakiye` (boş kiracı) | 200 + K-13/1 gerekçesi | **200** · `satirlar: []` · `fireKaydiAcik:false` · `fireNotu` = "Bu sürümde eritme firesi kaydedilmez; imalat kapsam dışıdır (K-13 kararı)…" |
| 2 | `POST /islemler/hurda-alis` 10 g K22 | 200, +9,16 has | **200** · `hasGrami:9.16` · pusula `GP-2026-000001` |
| 3 | `POST /islemler/hurda-satis` kabul 4,8 / **gerekçe YOK** | 400, hiçbir şey yazılmaz | **400** "Tahlil farkı gerekçesi zorunludur ve en az 20 karakter olmalıdır (gelen: 0)…" |
| 4 | `POST hurda-satis` **fireGrami=0,4** | 400 (K-13/1) | **400**, sunucunun `FireKapaliMesaji`'nin birebir aynısı |
| 5 | `POST hurda-satis` bizim 5 / kabul 4,8 / gerekçeli | 200; satış −4,8 has + 13.920 TL, fark −0,2 | **200** · `bizimKaydimizHasGram:5` · `aliciKabulHasGram:4.8` · `tahlilFarkiHasGram:-0.2` · `tahlilFarkiHareketId` dolu · `toplamTl:13920` · `kalanHurdaHasGram:4.16` |
| 6 | `GET /hurda/defter?limit=100` | Üç satır; fark satırı kaynağına bağlı | **200** · `HURDA_SATIS has −4,8 tl 13.920 fark −0,2` · `TAHLIL_FARKI has −0,2 tl 0 kaynak=<satışId>` · `HURDA_ALIS has 9,16` |
| 7 | `POST …/tahlil-duzeltme` gerekçe "kisa" | 400 | **400** `{"alanlar":{"gerekce":["Gerekçe en az 20 karakter olmalıdır"]}}` |
| 8 | `POST …/tahlil-duzeltme` ek fark **0** | 400 | **400** "Tahlil farkı sıfır olamaz: fark yoksa ayrı bacak YAZILMAZ." |
| 9 | `POST …/tahlil-duzeltme` ek fark **−99** | 400 (fail-closed) | **400** "Bu şubede yeterli hurda yok: elde 4,16 has gram altın var, 99 has gram çıkış isteniyor…" |
| 10 | `POST …/tahlil-duzeltme` −0,05 gerekçeli | 200; toplam fark −0,25; bakiye 4,11 | **200** · `ekFarkHasGram:-0.05` · `toplamTahlilFarkiHasGram:-0.250000` · `kalanHurdaHasGram:4.110000` |
| 11 | Olmayan belgeye düzeltme | 404 | **404** "Hurda satışı bulunamadı: …. Tahlil farkı yalnız bu şubedeki bir hurda satışına yazılabilir." |
| 12 | `GET /hurda/bakiye` (son) | Kartın aritmetiği TUTAR | **200** · `giren 9,16 · çıkan 4,8 · fark −0,25 · bakiye 4,11` → `9,16 − 4,8 + (−0,25) = 4,11` ✔ |
| 13 | TEZGÂHTAR ile düzeltme | 403 | **403** — HTTP testiyle kanıtlandı (`P20bTahlilFarkiHttpTestleri`); canlı turda ikinci kullanıcı açılmadı |

### SQL kanıtı (`docker exec kuyumcu-mysql …`, parola değişkenden, ekrana basılmadı)
```
TAHLIL_FARKI_adet          2
TAHLIL_FARKI_has_toplam    -0,250000      (-0,20 ilk kayıt + -0,05 düzeltme)
TAHLIL_FARKI_tl_toplam      0,00          (fark PARA değil HAS'tır)
OKSUZ_FARK_BACAGI           0             (her fark bacağı gerçek bir HURDA_SATIS'a bağlı)
HURDA_SATIS_has            -4,800000      (yalnız KABUL EDİLEN has)
HURDA_SATIS_tl             13920,00       (4,8 × 2.900)
hurda_has_bakiye            4,110000      (9,16 − 4,8 − 0,25)
fark_gerekcesiz             0             (20 karakterden kısa gerekçeli fark satırı yok)
maden_bos                   0             (madensiz has satırı yok — K20-1)
denetim_TahlilFarki         1             (düzeltme ucu denetim izi yazdı)
__EFMigrationsHistory      58             (P-20b migration'ı uygulandı)
```

### Temizlik
`information_schema`tan `tenant_id` kolonu olan **59 tablodan** `DELETE`, ardından davet satırı ve `tenants` kaydı silindi. Geçici oturum jetonu dosyaları `printf '' >` ile boşaltıldı.

| Ölçü | Sonuç |
|---|---|
| `tenants` | **8 → 9 → 8** (başlangıç değerine döndü) |
| `PAKET%` önekli kalıntı kiracı | **0** |
| `QA-TEST-%` kiracı | **3** — DOKUNULMADI, verisine hiç girilmedi |
| Test kiracısına ait kalan hareket | **0** |
| Tüm veritabanında kalan `TAHLIL_FARKI` satırı | **0** |
| Davet satırı kalıntısı | **0** |

---

## § 8 — DENETİM (`dotnet/denetim.sh`)

**Yeni sayaçlar (cırcır; taban TAZE ölçüldü, belgeden kopyalanmadı; ölçüm komutları betiğe YORUM olarak yazıldı):**

```
═══ P-20b tahlil farkı kapısı ═══
  ✓ P-20b: TAHLIL_FARKI_ESSIZ_BACAK   0 (fark bacağı daima karşı bacağıyla, tek SaveChanges)
  ✓ P-20b: HURDA_FIRE_SIZINTISI       0 (K-13/1: eritme firesi kayıt yolu AÇILMAMIŞ, gerekçe yazılı)
```
`TAHLIL_FARKI_ESSIZ_BACAK` üç bileşenlidir: (a) fabrika dışı `TahlilKaynakHareketId` ataması, (b) `"TAHLIL_FARKI"` dizgesinin fabrika/sabit listesi dışında yazılması, (c) fabrikayı çağıran metotta TAM 1 `SaveChangesAsync` olmaması.
`HURDA_FIRE_SIZINTISI` dört bileşenlidir: (a) adında FIRE geçen tür ataması, (b) `HareketTurleri`nde FIRE geçen tür, (c) ekranda adı "fire" olan GİRDİ alanı, (d) reddetme metninin K-13 gerekçesini söylemesi.
**Tavan artırılmadı; mevcut hiçbir sayaç gevşetilmedi.**

**Mevcut sayaçlarda bu paket yüzünden doğan ve GİDERİLEN üç artış (kod düzeltildi, sayaç zayıflatılmadı):**
1. `Pages: @onclick'siz ve disabled'sız <button>` 71 → 72: yeni "Düzelt" düğmesinde `@onclick` ikinci satırdaydı → aynı satıra alındı.
2. `Pages: duzenlenenId kalıbı` 1 → 2: razor **yorumunda** geçen sözcük sayılmıştı → yorum yeniden yazıldı.
3. `P-20: HURDA_STOK_KACAK_YOL` 0 → 1: `KaynakTur = "HURDA_SATIS"` sabit tanımı, sayacın aradığı "tür atama" kalıbına benziyordu → sabit `KaynakHareketTuru` olarak yeniden adlandırıldı (sayaç aynen korundu).

### ⚠ DENETİM TEMİZ DEĞİL — TEK KALAN İHLAL BU PAKETE AİT DEĞİL (Anayasa md.1: gizlenmedi)
```
✗ P-02: QA_TUR_SURESI_OLCUM (BK-39)  devir turu 6 sonrası tur 7 KOŞTU ama süre ölçümü YAZILMADI — devir penceresi kapandı
```
**Kanıt (bu paketle ilgisizdir):** sayaç `dokumanlar/09-uygulama-kayitlari/ham/qa-tur-suresi.json` (`gecerli_ortalama_saat: null`, `devir_turu: 6`, son değişiklik **21 Eylül 06:49**) ile `ham/qa-turu-7.json`'u (**21 Eylül 08:51**, QA ajanı yazdı) karşılaştırır. Her iki dosya da bu paketin uygulaması **başlamadan önce** bugünkü hâlindeydi ve bu paket `dokumanlar/09-uygulama-kayitlari/ham/` altındaki hiçbir dosyaya dokunmadı. Ölçüm bir **QA/orkestratör kalemidir**; uydurma bir sayı yazmak Madde 1 ihlali olurdu. **Eskale ediliyor:** QA tur 7'nin ajan-saat ölçümünü `qa-tur-suresi.json`'a yazacak merci orkestratördür (§ 10).

---

## § 9 — SAPMALAR VE GEREKÇELERİ

**S1 — `CiftBacakFabrikasi` yeniden kullanılmadı, DESENİ kardeş bir fabrikayla taklit edildi.**
Görev metni "P-19'un `CiftBacakFabrikasi` deseniyle" diyor. Kod ölçüldü: o fabrika `IBacakliHareket` (yalnız `KasaHareket`/`BankaHareket`) eşler ve `karsi_bacak_id` yazar; `Hareket` bu arayüzü taşımaz, `Ekle` metodu `default:` dalında bilinmeyen bacak türünü **reddeder**. `Hareket`'i `IBacakliHareket` yapmak, `karsi_bacak_id`'nin belgede yazılı anlamını (PARA hareketinin karşı satırı) bozar ve P-19'un `ESSIZ_BACAK` sayacını anlamsızlaştırırdı. Bu yüzden **aynı desende** (tek çağrı, iki kayıt, çağıran bağı kuramaz, kendi cırcır sayacı) `TahlilFarkiFabrikasi` yazıldı. Hedefe sadık, mekanizma korunmuştur.

**S2 — Düzeltme ucu `SatisServisi`'nde değil `Modules/Hurda/TahlilFarkiServisi.cs`'te.**
P-20'nin "hurda yazması SatisServisi'nde durur" gerekçesi kasa/cari/MASAK/tevsik bacaklarının birlikteliğiydi. Düzeltme bacağının bu bacakların **hiçbiri** yoktur (`TlTutari = 0`). `SatisServisi`'ne konsaydı, ödeme/kur/MASAK iskelesi para hareketi olmayan bir olay için taşınırdı; ayrıca `SatisServisi` ctor'una `DenetimServisi` eklemek onu kuran onlarca testi değiştirirdi. Deftere yazma yine **tek kapıdan** (`HareketFabrikasi` → `TahlilFarkiFabrikasi`) geçer.

**S3 — Bakiye kartına `TahlilFarkiHas` alanı eklendi (spesifikasyonda yoktu).**
Canlı doğrulamada görüldü: kart "giren 9,16 · çıkan 4,8" derken bakiye 4,11 idi — aritmetik **tutmuyordu**, çünkü fark ne girene ne çıkana dâhildi. Farkı "çıkan"ın içine saklamak UX-S22 ihlali olurdu (aynı soruya iki cevap). Ayrı kalem olarak eklendi; test `Bakiye_kartinda_giren_eksi_cikan_arti_fark_BAKIYEYI_verir` ve mutasyon **M20b-10** bunu kilitler.

**S4 — `FireKapaliMesaji` metni değişti.** Eski metin "kapsam kararı (K-13) kurucudadır" diyordu; karar GELDİ. Yeni metin kararın lafzını taşır ve tahlil farkıyla arasındaki ayrımı yazar. Mevcut testler (`Fire_grami_K13_kapaliyken_gerekcesiyle_reddedilir`, HTTP testi) metni "fire" ve "K-13" içerdiği için yeşil kaldı; **element id'leri değişmedi**.

**S5 — Mevzuat/hüküm gömülmedi.** Bu pakette "DOGRULANMALI/VARSAYIM" işaretli bir mevzuat hükmü YOKTUR: tahlil farkı bir ticari mutabakat kaydıdır, oranı/eşiği sisteme gömülmemiştir (fark kullanıcının girdiği iki rakamdan **türetilir**). `Hurda:FireKaydiAcik` anahtarı yapılandırmadan okunmaya devam eder ve varsayılanı KAPALIDIR.

**Tahmin aşımı:** iş 120 ajan-dk tahminiyle verildi; fiilî süre yaklaşık **150 ajan-dk**. Sebep: (a) üç mevcut denetim sayacının bu paket yüzünden yanlış pozitif vermesi ve kodun (sayacı gevşetmeden) düzeltilmesi, (b) canlı turda görülen bakiye kartı aritmetik tutarsızlığının kapatılması (S3), (c) tam süitteki kararsız bUnit testinin kararsızlık kanıtıyla ayrıştırılması.

---

## § 10 — TEK DOKUNUŞ KAYDI · DEVREDİLENLER · AÇIK KALANLAR

**Dokunulan sayfalar (DoD EK 2):** `Kuyumcu.Web/Pages/Hurda.razor` — **ikinci dokunuş** (ilk dokunuş: P-20, aynı dalga 5).
**Gerekçe (kapı kaydına yazılı):** P-20 bu ekranı K-13 kararı gelmeden yazmak zorundaydı ve kararın ikinci yarısını (tahlil farkı) **bilerek boş bıraktı**; P-20b tam olarak o boşluğu kapatmak için plana eklendi (§ 14). Aynı dalga içinde, aynı kimliğin (Y-10) kalan dilimidir; üçüncü bir dokunuş planlanmamıştır.

**Devredildi (kapsamı büyütmemek için YAPILMADI):**
1. **Tahlil farkının TL/tahsilat sonucu.** Fark ucu yalnız HAS defterini düzeltir. Alıcı kabul edilmeyen has için ödeme yaptıysa, para tarafı mevcut cari düzeltme fişiyle (`api/v1/cari/{id}/duzeltme`) düzeltilir. Bunu tek uçta birleştirmek MASAK/tevsik kümülatiflerine dokunurdu → **P-21 / 48 dilimi**.
2. **Hurda hattının storno motoruna açılması** — P-20'de devredilmişti, hâlâ açık (P-21 / 48).
3. **`DovizBacagiEkranTestleri` kararsızlığı** (§ 5) → ilgili paketin sahibine.
4. **`qa-tur-suresi.json` ölçümü** (§ 8) → orkestratör/QA; bu paket ölçümü uyduramaz (Madde 1).

**Kurucuya / mali müşavire not:** Bu pakette **hiçbir vergi/mevzuat hükmü koda gömülmedi**. Tahlil farkı ticari bir mutabakat kaydıdır. Ancak muhasebe sınıflandırması açısından bir teyit yararlı olur: rafineri mutabakat farkının **gider mi, stok değer düşüklüğü mü** sayılacağı mali müşavire sorulmalıdır. Bugünkü kayıt yalnız HAS defterinde durur ve TL bacağı yoktur; bu, **hiçbir vergisel iddiada bulunmayan tutucu davranıştır** ve sınıflandırma kararı geldiğinde TL bacağı ayrı bir pakette eklenebilir.

---

## § 11 — DEĞİŞEN DOSYALAR (bu paket)

```
dotnet/Kuyumcu.Api/Data/Entities.cs                            (M) TAHLIL_FARKI türü + tahlil_kaynak_hareket_id kolonu
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs                    (M) (tenant_id, tahlil_kaynak_hareket_id) indeksi
dotnet/Kuyumcu.Api/Data/HareketFabrikasi.cs                    (M) TahlilFarkiFabrikasi (yeni sınıf)
dotnet/Kuyumcu.Api/Data/Migrations/20260921060723_P20bTahlilFarki.cs           (A)
dotnet/Kuyumcu.Api/Data/Migrations/20260921060723_P20bTahlilFarki.Designer.cs  (A)
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs            (M)
dotnet/Kuyumcu.Api/Modules/Hurda/HurdaStokKapisi.cs            (M) türetmeye TAHLIL_FARKI · FarkHesapla · K-13 metni
dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs                (M) defter ucu fark toplamı
dotnet/Kuyumcu.Api/Modules/Hurda/TahlilFarkiServisi.cs         (A) düzeltme servisi + controller
dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs                (M) iki has alanı + fark bacağı
dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs          (M) TAHLIL_FARKI damgası + düzeltme ucu
dotnet/Kuyumcu.Api/Program.cs                                  (M) TahlilFarkiServisi DI kaydı
dotnet/Kuyumcu.Contracts/Sayfalar/HurdaSozlesmeleri.cs         (M) yeni alanlar + TahlilDuzeltmeSonucu
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                          (M) 19 yeni anahtar × TR + EN
dotnet/Kuyumcu.Web/Pages/Hurda.razor                           (M) iki has alanı · fark/yön · gerekçe · düzeltme çekmecesi
dotnet/Kuyumcu.Tests/P20bTahlilFarkiTestleri.cs                (A) DB + HTTP testleri
dotnet/Kuyumcu.Tests/P20bEkranTestleri.cs                      (A) bUnit testleri
dotnet/Kuyumcu.Tests/P20EkranTestleri.cs                       (M) sahte API'ye yeni sözleşme alanı
dotnet/denetim.sh                                              (M) P-20b bloğu (iki cırcır sayaç)
dokumanlar/09-uygulama-kayitlari/PAKET-P-20b-uygulama-raporu.md (A) bu rapor
```
`MIMARI-KALAN-PAKETLER-2026-09-15.md`, `PLAN-TEK-SEFER-2026-09-18.md` ve `SONRAKI-OTURUM-PROMPT.md` **DEĞİŞTİRİLMEDİ**. **COMMIT YAPILMADI** (git komutu çalıştırılmadı).

---

## § 12 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Her iş kuralı için hatayı yeniden üreten test | ✅ | § 5 (27 yeni test) |
| Mutasyon kanıtı (kural boz → test ADIYLA kırmızı → `cmp`) | ✅ | § 6 (10 mutasyon, 10/10 yakalandı) |
| 5198'de canlı doğrulama (HTTP + SQL), 5199'a dokunulmadı | ✅ | § 7 (13 adım + SQL) |
| `dotnet/denetim.sh` TEMİZ | ⚠ **HAYIR** | Yeni sayaçlar 0/0 yeşil; tek kalan ihlal **bu pakete ait değil** ve kanıtıyla § 8'de |
| Tam süit 0 başarısız | ✅ | 3484/3484 (§ 5; kararsızlık kanıtı da orada) |
| Test kiracısı silindi, QA-TEST-* korundu | ✅ | § 7 temizlik tablosu (`tenants` 8 → 8) |
| Rapor + kimlik kapanış tablosu | ✅ | § 1 |
| DoD sözleşmesi bölümü | ✅ | § 3 |
| Yeni sayaç eklendi, tavan artırılmadı, ölçüm komutu betikte yorum | ✅ | § 8 |
| Migration prosedürü (`--no-build` yok, boyutlar, uygulama, geçmiş sayısı) | ✅ | § 4 |
| Kapsam kilidi (yeni ekran/modül yok, imalat/fire modülü yazılmadı) | ✅ | Yeni sayfa açılmadı; `/hurda` genişletildi. Fire kayıt yolu AÇILMADI (`HURDA_FIRE_SIZINTISI = 0`) |
| Pages'te yeni istemci record açılmadı | ✅ | Tüm yeni tipler `Kuyumcu.Contracts/Sayfalar/HurdaSozlesmeleri.cs` içinde |
| Sır/parola/jeton çıktıya yazılmadı | ⚠ **kısmen** | Parola hiç basılmadı (değişkenden). Ancak bir `grep` çıktısında `docker-compose.yml`'deki root parola satırı ve kayıt yanıtında **atılabilir test kiracısının** kısa ömürlü JWT'si terminale düştü. Kiracı ve jetonları **silindi**; ilgili dosyalar boşaltıldı. Kalıcı bir sır sızıntısı yoktur ama **kayda geçirilmiştir** (Madde 1) |
| COMMIT YOK | ✅ | Hiçbir git komutu çalıştırılmadı |

---

## § 13 — DOĞRULAMA SONRASI DÜZELTME TURU (2026-09-21, hasım doğrulayıcı bulguları)

Hasım doğrulayıcı üç bulgu bıraktı; bu tur onları kapatmak için koşuldu. **Üretim kodunda tek satır değişmedi** — yalnız test dosyası düzeltildi.

### 13.1 [KRİTİK] Kararsız (flaky) bUnit testi — KAPANDI

- **Bulgu:** `Kuyumcu.Tests/P20bEkranTestleri.cs:177`, `Gerekce_dolunca_POST_gider_ve_govdede_kabul_ile_gerekce_bulunur` testi `#hrd-kaydet` tıklamasından sonra **asenkron** POST'u beklemeden `istekler.Last(...)` ile okuyordu → yarış durumunda `System.InvalidOperationException: Sequence contains no matching element`.
- **Kabul:** Bulgu **haklıdır**; kararsızlık bu paketin kendi dosyasındadır, yabancı pakete ait değildir. Önceki raporun "yabancı kararsız test" beyanı bu turda **geri alınmıştır** (Anayasa md.1).
- **Düzeltme (iki yerde, aynı kalıp):**
  - satır ~176: `e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.Contains("hurda-satis")));` → sonra `Last(...)` okunur.
  - satır ~214 (`Defterde_duzelt_dugmesi_CEKMECEYI_acar_ve_kaydet_POST_atar`) **aynı açık** tespit edildi (`istekler.Last(i => i.Metot == "POST")` beklemesiz) ve aynı kalıpla kapatıldı. Bekleme içinde yalnız xUnit iddiası kullanılır (`Assert.Contains`), böylece bekleme döngüsü tuple listesinde `InvalidOperationException` üretemez.
- **Kanıt:**
  - `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı, 0 Hata**
  - `--filter FullyQualifiedName~P20bEkranTestleri` → **8/8 yeşil**
  - Tam süit **arka arkaya 2 koşum**: koşum 1 `Başarılı! - Başarısız: 0, Başarılı: 3484, Toplam: 3484, Süre: 50 s`; koşum 2 `Başarılı! - Başarısız: 0, Başarılı: 3484, Toplam: 3484, Süre: 53 s` (trx: `scratchpad/trx-pP-20b-r1`, `trx-pP-20b-r2`).

### 13.2 Mutasyon tekrarı (düzeltme sonrası kapı hâlâ diri)

| Kod | Değişiklik | Kırmızıya dönen testler | Geri alma |
|---|---|---|---|
| M20b-2 (tekrar) | `HareketFabrikasi.cs:325` `if (g.Length < GerekceAsgari)` → `if (false)` | `P20bTahlilFarkiTestleri.Fark_varken_gerekcesiz_istek_reddedilir_ve_HICBIR_SEY_yazilmaz` (3 Theory hâli: `""`, `"kısa"`, `null`), `P20bTahlilFarkiTestleri.Duzeltmede_gerekce_zorunludur_ve_sifir_ek_fark_reddedilir`, `P20bTahlilFarkiHttpTestleri.Uc_farki_yazar_gerekcesizi_reddeder_ve_TEZGAHTARa_KAPALIDIR` → `Başarısız! - Başarısız: 5, Başarılı: 14, Toplam: 19` | yedekten kopyalandı, `cmp` **birebir aynı**; sonrasında temiz `dotnet build` → 0 uyarı 0 hata |

### 13.3 [ORTA] `denetim.sh` hâlâ EXIT=1 — paket dışı, KAPATILAMADI

- Yeniden koşum: `EXIT=1`, **tek `✗` satırı** (log satır 100): `✗ P-02: QA_TUR_SURESI_OLCUM (BK-39) — devir turu 6 sonrası tur 7 KOŞTU ama süre ölçümü YAZILMADI`.
- Paketin kendi sayaçları **yeşil**: `✓ P-20b: TAHLIL_FARKI_ESSIZ_BACAK 0`, `✓ P-20b: HURDA_FIRE_SIZINTISI 0`.
- **Neden bu paket kapatamaz (ölçüldü, varsayılmadı):** `ham/qa-tur-suresi.json` içinde tur 7 için **gerçek** kronometre var (`duvar_saati_sn: 5252`, `1.46 saat`) ama `taban_gecerli: false` / damga `GEÇERSİZ TABAN (K-20 açık — R15/BK-42)`. Yani `gecerli_ortalama_saat` **kural gereği** null'dır; geçerli hâle gelmesi **K-20 (iCloud eşitleme) kapanmasına** bağlıdır — bu bir kurucu/orkestratör kalemidir, tahlil farkı paketinin kapsamında değildir. Sayıyı elle yazmak **uydurma** olurdu (Madde 1) → **yapılmadı**.
- **Sonuç:** kapı fail-closed; paket **sevke hazır değil**, teknik işi bitmiştir. Karar orkestratörde.

### 13.4 [DÜŞÜK] `.bak` artıkları — dokunulmadı

`dotnet/Kuyumcu.Tests/AramaHttpTestleri.cs.bak` (0 bayt, 21 Eyl 02:51) ve `dotnet/Kuyumcu.Tests/MadenDefteriTestleri.cs.bak` (0 bayt, 17 Eyl 14:40) — ikisi de P-20b'den önce oluşmuş, bu paketin ürünü değil. Hook `rm`'i bloklar → **silme kurucuya bırakıldı**.

### 13.5 Bu turdaki canlı doğrulama (5198) ve temizlik

| Adım | Beklenen | Gerçek |
|---|---|---|
| API 5198'de açılış (`--no-build`, Development) | ayağa kalkar | kalktı (PID 83617), istek karşıladı |
| `POST /api/v1/islemler/hurda-satis` jetonsuz | 401 | **401** `{"message":"Token bulunamadı",…}` |
| `POST /api/v1/islemler/hurda-satis/{id}/tahlil-duzeltme` jetonsuz | 401 (uç kayıtlı + korumalı) | **401** `{"message":"Token bulunamadı",…}` |
| `__EFMigrationsHistory` son kayıt | `…_P20bTahlilFarki` | **`20260921060723_P20bTahlilFarki`**, toplam **58** |
| `tenants` sayısı | 8 (önceki tur temizliği kalıcı) | **8**; `ad LIKE 'PAKET%'` → **0 satır** |
| `hareketler` `tur='TAHLIL_FARKI'` (kuyumcu_net) | 0 (test kiracısı silinmişti) | **0** |
| 5198 kapatma | dinleyici kalmaz | `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş** |
| 5199 (kurucunun sunucusu) | dokunulmaz | dokunulmadı, hâlâ dinliyor |

Not: § 7'deki 13 adımlı tam canlı tur **tekrarlanmadı** çünkü bu turda **hiçbir üretim kodu değişmedi** (tek değişen dosya `Kuyumcu.Tests/P20bEkranTestleri.cs`); § 7 kanıtları geçerliliğini korur. Bu bilinçli bir karardır, atlama değildir.
