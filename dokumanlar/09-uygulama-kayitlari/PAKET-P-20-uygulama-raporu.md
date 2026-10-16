# PAKET P-20 — HURDA SATIŞI / RAMAT ÇIKIŞI (Y-10)

**Tarih:** 2026-09-21 · **Dalga:** DALGA 5 · **Rol:** Taşkın (03 Backend) + Ertuğ (03 Frontend)
**Sürüm:** 1.0 · **Plan bloğu:** `PLAN-TEK-SEFER-2026-09-18.md` satır 478-487
**Kapsam kimliği:** Y-10 · **Önkoşullar:** P-19 (bitmiş) · **K-13 kurucu kararı (GELMEDİ)**

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (durma-devam, varsayımsız)

| Önkoşul | Diskte ne var | Sonuç |
|---|---|---|
| **P-19** | `dokumanlar/09-uygulama-kayitlari/PAKET-P-19-uygulama-raporu.md` VAR; `Data/HareketFabrikasi.cs` içinde `CiftBacakFabrikasi` VAR; `denetim.sh` P-19 bloğu VAR ve TEMİZ | **TAMAM** |
| **K-13** (imalat/fire sınırı) | Kurucu kararı **YOK** (`KURUCUYA-KARARLAR-2026-09-16.md` ve § 4 tablosunda "DALGA 5 öncesi" olarak bekliyor) | **GELMEDİ** → plan DoD'sinin dediği gibi **fire/ramat farkı YAZILMADI**; kapı yapılandırmadan okunur, **varsayılanı KAPALI**, ekranda gerekçe görünür |
| Önceki oturumdan kalan P-20 iskeleti | `Modules/Hurda/HurdaStokKapisi.cs` (kapı) · `Modules/Hurda/HurdaModulu.cs` okuma uçları · `Sabitler.HareketTurleri` içinde `"HURDA_SATIS"` · `HurdaAyarlari.FireKaydiAcik` | **YAZMA YOLU, EKRAN, SÖZLÜK, TEST, SAYAÇ YOKTU** — bu paket onları yazdı. Ayrıca `DuzeltmePolitikasi` kaydı eksik olduğu için `Sabitler_HareketTurleri_ile_DuzeltmePolitikasi_TAM_ortusur` testi **kırmızıya düşecek durumdaydı**; bu pakette kapatıldı |

---

## § 1 — KİMLİK KAPANIŞ TABLOSU (zorunlu)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **Y-10** — HURDA SATIŞI hareket türü + has/TL karşı bacağı | **KAPANDI** | Kod: `Kuyumcu.Api/Modules/Satis/SatisModulu.cs` → `HurdaSatisDto` + `SatisServisi.HurdaSatis(...)` (`Tur="HURDA_SATIS"`, `HasGrami = -has`, `TlTutari = +toplam`), uç `POST /api/v1/islemler/hurda-satis` `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]`. Test: `P20HurdaSatisTestleri.HurdaSatisi_has_bacagini_EKSILTIR_ve_TL_bacagini_yazar`. Canlı: § 6 adım 7 (HTTP 200, `hasGrami=-5`, `tlTutari=+14.500`), SQL `HURDA_SATIS_has_toplam = -6,000000` |

**Kapsam kilidi:** yalnız Y-10'a dokunuldu. Başka paketin kimliğine girilmedi.

---

## § 2 — PLAN BLOĞU ↔ YAPILAN (DoD karşılama)

| Plan maddesi | Durum | Kanıt |
|---|---|---|
| `HURDA_SATIS` has bacağını **eksiltir** ve TL/has karşı bacağı yazar | ✅ | `SatisModulu.cs` `HasGrami = Yuvarla6(-hasGrami)` · `TlTutari = Yuvarla2(toplamTl)`; mutasyon **M20-2** bu kuralı kırınca test kırmızı |
| **K-13 gelmeden fire/ramat farkı yazılmaz**; ekranda gerekçe görünür | ✅ | `HurdaStokKapisi.DogrulaFire` + `HurdaAyarlari.FireKaydiAcik = false` (yapılandırmadan okunur, KODA GÖMÜLÜ DEĞİL); ekranda `#hrd-fire-notu` (metin **sunucudan** gelir). Test: `Fire_grami_K13_kapaliyken_gerekcesiyle_reddedilir` + `Fire_anahtari_acilinca_istek_reddedilmez` + ekran testi `Fire_kaydinin_YOKLUGU_ekranda_gerekcesiyle_gorunur`. Canlı: § 6 adım 4 (HTTP 400) |
| `MadenKapisi` ayarsız hurda yazılmasını engeller | ✅ | İki katman: (1) servis içi elle doğrulama — `Geçersiz maden: '...'` 400 (DataAnnotations yalnız controller yolunda çalışır); (2) `HareketFabrikasi.Ekle` → `MadenKapisi.HareketDogrula` (has bacağı olan satır madensiz yazılamaz). Test: `Gecersiz_maden_ile_hurda_satisi_reddedilir`, `Baska_madenin_bakiyesi_altin_hurdasini_karsilamaz`. Canlı: § 6 adım 5 |
| Sayaç `HURDA_STOK_KACAK_YOL = 0` | ✅ | `dotnet/denetim.sh` `# >>> P-20` bloğu; taze ölçüm **0** (4 bileşen), ayrıca destek sayacı `FIRE_VARSAYILANI` |
| Migration **ŞARTLI** — "hurda stok bakiyesi ayrı izleniyorsa kolon gerekir (önce ölçülür)" | ✅ **ölçüldü → gerekmedi** | § 4 |

---

## § 3 — DoD SÖZLEŞMESİ (Bertan mührü — hangi kayıt nasıl düzeltilir)

| Kayıt / varlık | Düzenleme yolu | Gerekçe |
|---|---|---|
| **HURDA_SATIS defter satırı** (yeni kayıt türü) | **SALT OKUNUR — STORNO_YOK**; ekranda `DegismezIpucu` (`#degismez-ipucu-HurdaSatisi`) gerekçeyi ve doğru yolu yazar | Defter satırı VUK md.219 gereği yerinde değişmez. Ters kayıt motoru (`StornoServisi.StornolanabilirTurler`) bu türü **tanımıyor**: motorun cari etkisi türe göre AÇIK eşlemeden okunur ve **kısmi tahsilatlı** hurda satışında o eşleme tutmaz (cariye TL bacağının TAMAMI değil, yalnız tahsil edilmeyen kısım yazılır). Olmayan uç **VAAT EDİLMEDİ** (UX-S25'in tersi de yasak). **DEVREDİLDİ:** storno motorunun hurda hattına açılması → P-21 / 48 dilimi |
| **Hurda bakiyesi** | Kayıt değil, **TÜRETME**: `HurdaStokKapisi` (tek kaynak). Düzeltilecek bir satırı yoktur; alış/satış satırları değişince kendiliğinden değişir | UX-S22: aynı sorunun iki cevabı olamaz |
| Finansal varlıkta hard DELETE | **YOK** — bu pakette hiçbir DELETE ucu açılmadı | Değişmez kural |

**Yeni varlık/tablo AÇILMADI**, bu yüzden PATCH/DELETE/`[ModulYetkisi]`/denetim kaydı üçlüsü yeni bir varlığa değil, **mevcut defter hattına** uygulanır: yazma `HareketFabrikasi` üzerinden, yetki `[ModulYetkisi("islem")] + [RolGerekli(...)]`, iz ise defterin kendisidir (hareket satırı + kasa hareketi + MASAK kaydı).

---

## § 4 — MİGRATION: **GEREKMEDİ** (ölçüldü, varsayılmadı)

Plan "ŞARTLI — hurda stok bakiyesi **ayrı izleniyorsa** kolon gerekir (**önce ölçülür**)" diyordu. Ölçüm:

- `HurdaAlis` `stoklar` tablosunda **satır açmıyor** (kod: `SatisModulu.HurdaAlis` — stok/maliyet partisi bacağı yok, yorumu da bunu söylüyor).
- `stoklar` tablosunda hurda kolonu **yok**.
- Yani hurdanın tek kaynağı `hareketler` defteridir → bakiye bir **kolon değil, türetmedir**.

Kolon açmak, defterden zaten türetilebilen değeri **ikinci bir gerçek** hâline getirir (iki kaynak = sessiz sapma). Bu yüzden migration üretilmedi.
**Kanıt:** canlı `__EFMigrationsHistory` = **57** (paket öncesi ve sonrası aynı; yeni migration dosyası yok).

---

## § 5 — TESTLER

| Koşum | Toplam | Başarılı | Başarısız | Not |
|---|---|---|---|---|
| Filtreli `~P20` (son hâl) | 29 | 29 | 0 | 14 DB/servis + 1 HTTP + 14 bUnit |
| Ekran testleri 4 kez arka arkaya | 14×4 | 14×4 | 0 | kararsızlık giderildikten sonra (aşağıda) |
| **Tam süit (paket öncesi taban)** | 3457 | 3452 | 5 | 5 kırmızı **bu paketin** sayım sözleşmelerinden: `SozlesmeKapsamTestleri` ×2, `MenuMimarisiTestleri`, `RotaBaslikTestleri`, `OlcumKapisiTestleri` — **gizlenmedi**, § 5.2'de çözüldü |
| **Tam süit (son)** | **3457** | **3457** | **0** | — |

**Yeni test sayısı: 29** (`Kuyumcu.Tests/P20HurdaSatisTestleri.cs` 15 + `Kuyumcu.Tests/P20EkranTestleri.cs` 14).

### § 5.1 — Yeni testler ne sınıyor
- Has bacağı eksilir, TL bacağı yazılır, kasa artar, bakiye 9,16 → 4,16.
- Elde olmayan hurda satılamaz (3 ayrı yol: hiç alış yok · bakiyeden fazla · kümülatif ikinci çıkış).
- K-13: fire kapalıyken 400 + hiçbir şey yazılmaz; anahtar açılınca istek reddedilmez.
- Geçersiz/boş maden reddi; başka madenin bakiyesi altın çıkışını karşılamaz.
- Tahsil edilmeyen tutar için cari zorunlu; kısmi tahsilatta kalan cariye **ALACAK** yazılır (+13.500).
- Kiracı izolasyonu: A kiracısının hurdası B'de satılamaz, A'nınki bozulmaz.
- Düzeltme politikası storno **vaat etmez**; MASAK kapısı hurda satışını da kapsar (alış-satış paritesi).
- Ekran: ölü düğme yok · tek maden/tek kasa seçicisi gizlenir · fiyat kurdan gelir + rozet · elle girilen fiyat ezilmez · boş/aşkın gönderimde alan altı mesaj ve **POST atılmaz** · başarı bandına sunucunun KALAN bakiyesi yazılır · bakiye yokken düğme kilitli + sebep · sunucu hatası boş duruma çevrilmez · sınır notu · fire notu · `DegismezIpucu` (içinde düğme yok) · bakiye kartı formu ayarlar.

### § 5.2 — Tam süitte çıkan 5 kırmızı ve çözümü (Anayasa md.1: gizlenmedi)
| Test | Sebep | Çözüm |
|---|---|---|
| `SozlesmeKapsamTestleri.Kilitsiz_tip_listesi_circir_ile_donduruldu` | Yeni istemci tipleri (`HurdaBakiyeYaniti`, `HurdaDefterYaniti`, `HurdaSatisSonucu`) sunucu imzasıyla **kilitli değildi** | Sunucu artık `Task<object>` yerine **Kuyumcu.Contracts tipini** döndürüyor (`HurdaStokServisi.Bakiye/Defter`, `SatisServisi.HurdaSatis` ve üç controller metodu). Tipler derleyiciyle kilitlendi; cırcır listesi ARTMADI |
| `SozlesmeKapsamTestleri.Kapsam_orani_beyan_edilen_degerle_ayni` | Aynı sebep | Aynı çözüm |
| `MenuMimarisiTestleri` (37→38) | `/hurda` menü bağlantısı eklendi | Test sayıları gerekçeli güncellendi (37→38, 39→40) |
| `RotaBaslikTestleri.Yansima_TAM_42_rota_bulur` (45→46) | Yeni `@page "/hurda"` | Gerekçeli 46 |
| `OlcumKapisiTestleri.qa_turu_rota_listesini...` | `betikler/qa-turu.js` `ROTA_KAPSAM_BEKLENEN` 45'te kalmıştı | 46 yapıldı (fail-closed kapı çalıştı: eksik rota turu başlatmıyor) |

### § 5.3 — Kararsız (flaky) test düzeltmesi — dürüstlük notu
İlk yazımda iki bUnit testi (`Bos_has_gram...`, `Kaydedilince_sunucunun_KALAN_BAKIYESI...`) **rastgele** kırmızı/yeşil geliyordu: `Find` render kuyruğunu beklemiyordu ve kur gelmeden düğme kilitliyken tıklanıyordu. Testler `WaitForElement` / `WaitForAssertion` ve "düğme açılana kadar bekle" ile deterministik hâle getirildi; ardından **4 ardışık koşumda 14/14 yeşil**. Kararsızlığın kendisi bir bulgudur ve buraya yazıldı.

---

## § 6 — MUTASYON KANITI (her kural için)

Yöntem: kaynak `scratchpad/mut-yedek/P-20/` altına kopyalandı → `perl -0777 -pi -e` ile TEK eşleşmeli değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile birebir doğrulandı. Son mutasyondan sonra **temiz `dotnet build` (0 hata / 0 uyarı)**.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| **M20-1** | `DogrulaCikis` çağrısı kaldırıldı (çıkış kapısı) | `Elde_olmayan_hurda_satilamaz_ve_hicbir_sey_yazilmaz`, `Ikinci_cikis_ilk_cikistan_kalani_asamaz`, `Hic_hurda_alinmamis_subede_satis_reddedilir`, `Baska_madenin_bakiyesi_altin_hurdasini_karsilamaz`, `Baska_kiracinin_hurdasi_bu_kiracida_satilamaz` (5) | `cmp` birebir |
| **M20-2** | Has bacağının işareti ters çevrildi (`-hasGrami` → `hasGrami`) | `HurdaSatisi_has_bacagini_EKSILTIR_ve_TL_bacagini_yazar`, `Ikinci_cikis_ilk_cikistan_kalani_asamaz`, `Fire_anahtari_acilinca_istek_reddedilmez`, `P20HurdaHttpTestleri.Uc_calisir_bakiye_dusurur_ve_TEZGAHTARa_KAPALIDIR` (4) | `cmp` birebir |
| **M20-3** | `DogrulaFire` (K-13 kapısı) kaldırıldı | `Fire_grami_K13_kapaliyken_gerekcesiyle_reddedilir` | `cmp` birebir |
| **M20-4** | "tahsil edilmeyen tutar için cari zorunlu" kapısı `if (false)` yapıldı | `Tahsil_edilmeyen_tutar_icin_ramatci_carisi_zorunludur` | `cmp` birebir |
| **M20-5** | Bakiye türetmesinde maden ayrımı (`Where(h => h.Maden == maden)`) kaldırıldı | `Baska_madenin_bakiyesi_altin_hurdasini_karsilamaz` | `cmp` birebir |
| **M20-6** | Ekranda kilit sebebinden bakiye koşulu kaldırıldı (UX-S30) | `P20EkranTestleri.Bakiye_yokken_kaydet_KILITLI_ve_sebebi_yazili` | `cmp` birebir |

**Test açığı bulunmadı:** altı mutasyonun hepsi en az bir testi ADIYLA kırdı.
*(M20-3 ve M20-5 koşumlarında ayrıca kararsız bir bUnit testi kırmızı göründü; o kararsızlık § 5.3'te giderildi ve mutasyon tablosundaki eşleşmeler bundan etkilenmez.)*

---

## § 7 — DENETİM (`dotnet/denetim.sh`)

```
═══ P-20 hurda stok kapısı ═══
  ✓ P-20: HURDA_STOK_KACAK_YOL                     0 (çıkış kapısı tek yol, bakiye tek kaynak)
  ✓ P-20: FIRE_VARSAYILANI                         kapalı (K-13 kurucu kararı bekliyor)

DENETİM TEMİZ.
```

### § 7.1 — DÜRÜSTLÜK NOTU: denetim.sh'ın SON koşumu KIRMIZI — ama sebebi P-20 DEĞİL

Paketin kendi kapıları ve tüm diğer sayaçlar yeşilken (08:35 koşumu: **DENETİM TEMİZ**), günün
ilerleyen saatinde alınan **son** koşumda tek bir satır kırmızıya döndü:

```
✗ P-02: QA_TUR_SURESI_OLCUM (BK-39)   devir turu 6 sonrası tur 7 KOŞTU ama süre ölçümü YAZILMADI — devir penceresi kapandı
```

**Sebep ve sahiplik (ölçüldü, tahmin edilmedi):**
`dokumanlar/09-uygulama-kayitlari/ham/qa-turu-7.json` dosyası **bu oturumda 08:50-08:51'de
OLUŞTU** (`stat`: birth 2026-09-21 08:50) — **bu ajan qa-turu.js'i hiç çalıştırmadı**; tur 7
paralel yürüyen QA/orkestratör akışından geldi. BK-39 kapısı "devir turu (6) sonrası bir tur
koştuysa ve `qa-tur-suresi.json`a geçerli ortalama yazılmadıysa DENETİMİ KIR" diyor; tur 7
ölçümü dosyaya **`GEÇERSİZ TABAN (K-20 açık)`** damgasıyla düştüğü için `gecerli_ortalama_saat`
hâlâ `null`.

**Bu ajan bu satırı DÜZELTMEDİ ve düzeltmemelidir:** ölçmediği bir süreyi dosyaya yazmak
Anayasa md.1 ihlali olurdu; kalem **P-02/BK-39 sahibinindir** ve kökü **K-20 (iCloud
eşitlemesi açık)** kurucu kalemidir. **P-20'nin kendi sayaçları ve tüm diğer kapılar yeşildir**
(`✗` listesinde P-20 satırı YOKTUR).

**Yeni sayaç `HURDA_STOK_KACAK_YOL` (cırcır, yalnız azalır) dört bileşenin toplamıdır — hepsi TAZE ölçüldü (0):**
1. `Tur = "HURDA_SATIS"` yazan ama `DogrulaCikis` çağırmayan metot → 0
2. Hurda tür listesini (`HurdaTurleri`) kapı dışında okuyan dosya → 0
3. Tür listesinin ikinci kez elle yazılması → 0
4. Ekranın bakiyeyi kendi türetmesi (`Pages/Hurda.razor` içinde toplama) → 0

Ölçüm komutları betiğe **yorum olarak** yazıldı. **Hiçbir tavan artırılmadı**, iki istisna dışında — ve o ikisi tavan değil **birebir sayım sözleşmesidir** (yeni sayfa eklenince tanım gereği artar; P-19'un `/gider` sayfasında kurduğu emsalin aynısı):
- `Pages/*.razor dosya sayısı` 45 → **46** (gerekçe betikte yazılı: `/islemler` PLAN § 7'de P-15→P-16→P-18 TEK EL zincirindedir, bu paket oraya giremez; ayrıca hurda bakiyesi bir **stok görünümüdür**, işlem formu değil — UX-S22)
- `P-02: ROTA_KAPSAM` 45 → **46** (+ `ham/olcum-2026-09-18.json` ROTA_KAPSAM notlu güncelleme + `betikler/qa-turu.js` `ROTA_KAPSAM_BEKLENEN` 46)

Denetimin diğer sayaçları (`OLU_DUGME` 71, `DUZ_SELECT_LIMIT` açık 9, `CONTRACTS_DISI_MODEL` 0, `GUVENLIK_ELLE_IZOLASYON` 0) **artmadı**; bunu sağlamak için:
- her `<button>` `@onclick`/`disabled` ile **aynı satırda** yazıldı,
- ramatçı seçimi düz 200'lük açılır liste yerine **`KayitSecici`** (sunucuda arayan seçici) ile yapıldı,
- şube süzgeci elle yazılmadı: `HurdaStokKapisi` tek sorgu kaynağını `SubeSuzgeci.OturumSubesinde` ile kurar,
- istemci bakiye tipi `HurdaBakiyeVeri` adıyla **tek tanım** oldu (sunucudaki eski `HurdaBakiyeSatiri` kaydı kaldırıldı).

---

## § 8 — CANLI DOĞRULAMA (localhost **5198** — 5199'a DOKUNULMADI)

Sunucu: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Kuyumcu.Api --no-build --no-launch-profile --urls http://localhost:5198` (**PID 36577**, iş sonunda `kill`).
`lsof -nP -iTCP:5198 -sTCP:LISTEN` → **boş** · `lsof -nP -iTCP:5199 -sTCP:LISTEN` → **hâlâ dinliyor (kurucunun sunucusu, dokunulmadı)**.
Kiracı gerçek uçla açıldı: `kiraci_davetleri`ne SHA-256 özetli satır + `POST /auth/kayit` → **201**, `PAKETP-20-TEST`.

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET /api/v1/hurda/bakiye` (boş kiracı) | 200 + boş liste + K-13 gerekçesi | **200** · `satirlar: []` · `fireKaydiAcik:false` · `fireNotu` K-13 metniyle dolu |
| 2 | `POST /islemler/hurda-satis` (stok yok) | 400 fail-closed | **400** "Bu şubede yeterli hurda yok: elde 0 has gram altın var…" |
| 3 | `POST /islemler/hurda-alis` 10 g K22 | 200, 9,16 has girer | **200** · `hasGrami=9.16` · pusula `GP-2026-000001` · `GET /hurda/bakiye` → `hasGram 9.16` |
| 4 | `POST hurda-satis` **fireGrami=0,4** | 400 (K-13) | **400**, mesaj sunucunun `FireKapaliMesaji`'nin birebir aynısı |
| 5 | `POST hurda-satis` `maden=TENEKE` | 400 | **400** "Geçersiz maden: 'TENEKE'… ALTIN, GUMUS, PLATIN, PALADYUM, CELIK" |
| 6 | `POST hurda-satis` 99 has | 400 | **400** "elde 9,16 has gram altın var, 99 has gram çıkış isteniyor" |
| 7 | `POST hurda-satis` 5 has × 2.900 | 200; has −5, TL +14.500; kalan 4,16 | **200** · `hasGrami:5` · `toplamTl:14500` · `kalanHurdaHasGram:4.16` |
| 8 | `GET /hurda/bakiye` + `GET /hurda/defter` | Tek kaynak aynı rakamı söyler | **200/200** · bakiye `hasGram 4.16` (`girenHas 9.16`, `cikanHas 5.0`) · defterde `HURDA_SATIS` satırı `hasGrami:-5.0`, `tlTutari:14500` |
| 9 | `POST hurda-satis` kısmi tahsilat, **carisiz** | 400 | **400** "Tahsil edilmeyen 2800 TL için ramatçı carisi zorunludur…" |
| 10 | `POST hurda-satis` kısmi tahsilat, **cari ile** | 200, kalan cariye alacak | **200** · `tahsilEdilenTl:100` · `kalanTl:2800` · `kalanHurdaHasGram:3.16` |
| 11 | TEZGÂHTAR rolü ile yazma | 403 | **403** — HTTP testiyle kanıtlandı (`P20HurdaHttpTestleri`); canlı turda ikinci kullanıcı açılmadı |

### SQL kanıtı (`docker exec kuyumcu-mysql …`, parola değişkenden)
```
HURDA_SATIS_adet          2
HURDA_SATIS_has_toplam    -6,000000     (5 + 1 has çıkış)
HURDA_SATIS_tl_toplam     17400,00      (14.500 + 2.900)
hurda_has_bakiye           3,160000     (9,16 − 6)
nakit_veresiye            0,00 / 2800,00  (HAVALE tahsilat nakit payı DEĞİL; kalan veresiye)
maden_bos                 0             (madensiz has satırı yok — K20-1)
cari_bakiye_tl            2800,00       (ramatçıdan ALACAK)
__EFMigrationsHistory     57            (yeni migration YOK)
```

### Temizlik
`information_schema`tan `tenant_id` kolonu olan **59 tablodan** `DELETE`, ardından davet satırı ve `tenants` kaydı silindi.

| Ölçü | Sonuç |
|---|---|
| `tenants` | **8 → 9 → 8** (başlangıç değerine döndü) |
| `PAKET%` önekli kalıntı kiracı | **0** |
| `QA-TEST-%` kiracı | **3** — DOKUNULMADI, verisine hiç girilmedi |
| Test kiracısına ait kalan hareket | **0** |
| Davet satırı kalıntısı | **0** |

---

## § 9 — SAPMALAR VE GEREKÇELERİ

1. **Yazma yolu `Modules/Hurda/` yerine `Modules/Satis/SatisModulu.cs`'te.**
   Plan dosya listesi `Modules/Hurda/HurdaModulu.cs:377`'yi sayıyor ama hurda satışı kasa + cari + MASAK + tevsik + kur bacaklarını **birlikte** yazar; bu bacakların tek yeri `SatisServisi`'dir ve `HURDA_ALIS` de oradadır. İkinci bir yazma motoru açılsaydı tevsik kümülatifi ile MASAK eşiği iki ayrı yerden yürür ve biri diğerinden saparadı. `SatisModulu.cs` PLAN § 7 TEK EL zincirlerinde **yoktur** (P-21 ona sonradan dokunacak, sıra korunuyor). Okuma tarafı planın dediği yerde (`HurdaModulu.cs`) kaldı.
2. **`Pages/Hurda.razor` YENİ sayfa olarak açıldı** (plan böyle yazıyor). `/islemler`e sekme eklemek TEK EL zincirini ve tek-dokunuş kuralını çiğnerdi.
3. **MASAK kapısına `HURDA_SATIS` eklendi** (`MasakKapisi.KapiyaTabiTurler`). Plan bunu yazmıyordu; eklenmeseydi eşik üstü her hurda çıkışı kimlik tespitinden **kaçış yolu** olurdu — alışta kapalı olan kapı satışta açık kalamaz (fail-closed parite). Bu bir kapsam genişletmesi değil, yeni hareket türünün kendi güvenlik bacağıdır.
4. **`DuzeltmePolitikasi`'na `HURDA_SATIS` kaydı** eklendi (STORNO_YOK). Zorunluydu: `Sabitler.HareketTurleri` ile politika haritası arasında TAM-ÖRTÜŞME testi var.
5. **Migration üretilmedi** — § 4 (plan "önce ölçülür" diyordu; ölçüldü).
6. **Fire hesabı kurulmadı.** Anahtar açılsa bile fire'ın **muhasebeleşmesi** (gider mi, maliyet mi) mali müşavir teyidine bağlıdır; bu sürüm o hesabı yazmaz, yalnız isteği reddetmeyi bırakır. Yapılandırma varsayılanı **KAPALI** (tutucu).
7. **Sözlükte yeni blok**: `SozlukHurda` TR + EN (çıplak metin yok). Menü ve rota başlığı da sözlükten.

---

## § 10 — DEĞİŞEN / EKLENEN DOSYALAR

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısında birçok dosya zaten izlenmiyor (dizin düzeyinde `??`). Bu pakette **elle dokunulan** dosyalar:

**Yeni:**
- `dotnet/Kuyumcu.Web/Pages/Hurda.razor` (18.048 bayt)
- `dotnet/Kuyumcu.Contracts/Sayfalar/HurdaSozlesmeleri.cs` (1.774 bayt)
- `dotnet/Kuyumcu.Tests/P20HurdaSatisTestleri.cs` (18.328 bayt)
- `dotnet/Kuyumcu.Tests/P20EkranTestleri.cs` (13.541 bayt)
- `dokumanlar/09-uygulama-kayitlari/PAKET-P-20-uygulama-raporu.md` (bu dosya)

**Değişen:**
- `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs` (HurdaSatisDto + HurdaSatis + uç)
- `dotnet/Kuyumcu.Api/Modules/Hurda/HurdaStokKapisi.cs` (şube süzgeci mekanizması, Contracts tipi)
- `dotnet/Kuyumcu.Api/Modules/Hurda/HurdaModulu.cs` (okuma uçları tipli yanıt)
- `dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs` (HURDA_SATIS kapıya tabi)
- `dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs` (HURDA_SATIS politikası)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (SozlukHurda TR+EN, Nav.Hurda)
- `dotnet/Kuyumcu.Core/Erisim/RotaModulleri.cs` (`/hurda` → `islem`)
- `dotnet/Kuyumcu.Web/Layout/MenuTanimi.cs`, `dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs`
- `dotnet/Kuyumcu.Web/wwwroot/css/app.css` (`.hurda-bakiye-kart` — yalnız yerleşim, sabit renk yok)
- `dotnet/denetim.sh` (P-20 sayaç bloğu + iki sayım sözleşmesi)
- `dotnet/Kuyumcu.Tests/MenuMimarisiTestleri.cs`, `RotaBaslikTestleri.cs` (sayım güncellemesi)
- `dokumanlar/09-uygulama-kayitlari/betikler/qa-turu.js` (ROTA_KAPSAM_BEKLENEN 46)
- `dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json` (ROTA_KAPSAM 46, notlu)

**Commit YAPILMADI**, hiçbir git komutu çalıştırılmadı (yalnız `git status`).

---

## § 11 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Plan bloğu DoD (4 madde) | ✅ | § 2 |
| Kimlik kapanış tablosu | ✅ | § 1 |
| Sayaç `HURDA_STOK_KACAK_YOL = 0`, taze ölçüm, tavan artmadı | ✅ | § 7 |
| Migration prosedürü (şartlı) | ✅ gerekmedi | § 4 |
| Her iş kuralı için hatayı yeniden üreten test | ✅ | § 5.1 |
| Mutasyon kanıtı (kır → kırmızı → geri al → `cmp`) | ✅ 6/6 | § 6 |
| 5198'de canlı doğrulama (HTTP + SQL), 5199'a dokunulmadı | ✅ | § 8 |
| `dotnet/denetim.sh` TEMİZ | ⚠ **KISMEN** | P-20 satırları ve diğer tüm kapılar yeşil; son koşumda tek kırmızı `P-02: QA_TUR_SURESI_OLCUM` (BK-39) — **bu paketin dışında**, sebebi ve sahipliği § 7.1 |
| Tam süit 0 başarısız | ✅ 3457/3457 | § 5 |
| Test kiracısı silindi, `QA-TEST-*` korundu | ✅ | § 8 |
| Derleme 0 hata / 0 uyarı | ✅ | `dotnet build Kuyumcu.sln --nologo` → 0 Uyarı, EXIT 0 |
| UX sözleşmesi S21-S33 (ilgili maddeler) | ✅ | S21/S22/S23/S25/S27/S28/S29/S30/S31/S32 ekran testleriyle; S24 (bağımlı alan) fiyat-kur bağıyla; S26 tek form olduğu için konusuz; S33 dönem seçicisi olmadığı için konusuz |
| DoD EK 1 (ölü düğme yok · tek düzenleme kalıbı · sessiz etkisizlik yok · yıkıcı eylem onayı) | ✅ | Ölü düğme testi; yeni `ModalPencere` düzenleme kalıbı AÇILMADI (denetim sayacı artmadı); alan altı mesaj testleri; **yıkıcı eylem yok** (silme/iptal ucu açılmadı) |
| DoD EK 2 (tek dokunuş) | ✅ | **Dokunulan sayfalar: `Pages/Hurda.razor` (YENİ, ilk dokunuş)** — başka razor sayfasına dokunulmadı |
| Ücretsiz pilot kuralı (fiyat/teklif/tahsilat metni üretilmez) | ✅ | Bu pakette fiyat/teklif metni üretilmedi |

---

## § 12 — AÇIK KALANLAR / DEVREDİLENLER

1. **K-13 (KURUCU):** imalat/fire sınırı kararı hâlâ yok. Karar gelince yapılacak iş: `Hurda:FireKaydiAcik` yapılandırma anahtarını açmak **yetmez** — fire'ın muhasebe bacağı (gider mi maliyet mi) ayrı bir paket işidir.
2. **Storno:** `HURDA_SATIS` ters kayıt motoruna **alınmadı** (gerekçe § 3). **Devredildi: P-21 / 48 storno dilimi.**
3. **MASAK eşik üstü hurda satışında kimlik/belge akışı** ekran tarafında `MasakFormu` ile bağlanmadı — bu ekranda MASAK bloğu gönderilmiyor; eşik üstü çıkışta sunucu 400 verir ve kullanıcı POS/işlemler ekranını kullanır. **Devredildi:** hurda ekranına MASAK formu bağlanması (UX dilimi).
4. **`P-02: QA_TUR_SURESI_OLCUM` (BK-39) kırmızı** — § 7.1. Sahibi P-02/QA akışı; kökü **K-20**. P-20 bu satırı çözmedi (ölçmediği süreyi yazamaz).
5. **Hurda satışında gider pusulası/fatura belgesi yok**: satış tarafında belge bizim kestiğimiz fatura olur; fatura bağı bu pakette kurulmadı. **Devredildi:** fatura köprüsü paketi.

---

## § 13 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

1. **K-13 bekliyor** (yukarıda). Karar gelmeden fire yazılmıyor; ürün bunu ekranda **açıkça** söylüyor.
2. **Mali müşavire:** hurda **çıkışının** (ramatçıya satış) KDV ve belge rejimi bu pakette **modellenmedi** — defter satırı ve para bacağı yazılır, belge bacağı yazılmaz. Özel matrah (KDV 23/e) ve KMTS beyanı ile ilişkisi **K-10** kaleminin kapsamındadır; koda hiçbir oran/hüküm **gömülmedi**.
3. **Güvenlik / yöntem hatası (Anayasa md.1, gizlenmiyor):** canlı doğrulamanın başında MySQL parolası, `docker-compose.yml`in **yorum satırını** yakalayan hatalı bir `grep` yüzünden bir kez oturum çıktısına düştü. Komut hemen doğru biçimiyle (`grep -E 'MYSQL_ROOT_PASSWORD:' … | awk '{print $2}'`) tekrarlandı ve parola bir daha basılmadı. Bu, P-19 raporunda da kayda geçen aynı tuzaktır; parola **zaten K-5 rotasyon kalemindedir** — rotasyon bu sebeple de gereklidir.

---

## § 14 — SONUÇ

**P-20'nin kendi işi BİTTİ** (tam süit 3457/3457 · P-20 sayaçları 0 · canlı 11 adım kanıtlı), ancak
`denetim.sh` **bütünüyle** TEMİZ değildir: paket dışı `P-02: QA_TUR_SURESI_OLCUM` satırı kırmızıdır
(§ 7.1). Kapı kararı orkestratöründür. Plan bloğunun dört DoD maddesi de ölçümle karşılandı: has bacağı eksilir + TL karşı bacağı yazılır (M20-2 + canlı 7) · K-13 gelmeden fire yazılmaz ve gerekçe ekranda görünür (M20-3 + canlı 4) · madensiz/geçersiz madenli hurda yazılamaz (canlı 5) · sayaç `HURDA_STOK_KACAK_YOL = 0` eklendi. Migration ölçüldü ve **gerekmedi**. Tavan artırılmadı; iki sayım sözleşmesi yeni sayfa nedeniyle gerekçeli güncellendi.
