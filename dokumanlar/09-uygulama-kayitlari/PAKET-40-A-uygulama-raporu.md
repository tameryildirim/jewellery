# Paket 40-A — MASAK risk skorlama + PEP beyanı (FAZ A) · uygulama raporu

> Tarih: 2026-09-18 · Dalga 4 · Roller: Taşkın (backend) + Ertuğ (frontend)
> Durum: **BİTTİ** · Derleme 0 hata · `dotnet/denetim.sh` **TEMİZ** · tam süit **2754/2754 yeşil**

---

## 1. Kapsam — ne yazıldı, ne YAZILMADI

**YAZILDI (Faz A):**
- `cariler` tablosuna 3 BEYAN kolonu (`pep_beyani`, `pep_gorev`, `pep_beyan_tarihi`) + `(tenant_id, pep_beyani)` indeksi
- `cari_risk_profilleri` — APPEND-ONLY tarihçe tablosu + iki indeks
- `RiskMotoru` (SAF), `MasakRiskAyarlari`, `RiskServisi`
- 5 uç: `GET /masak/risk/ozet`, `GET /masak/risk/{cariId}`, `POST /masak/risk/{cariId}/hesapla`,
  `POST /masak/risk/{cariId}/elle`, `PATCH /cari/{id}/pep`
- Uyum ekranına **Risk sekmesi**, Cariler listesine salt okunur seviye/PEP rozeti,
  Cariler düzenleme çekmecesine PEP beyanı, Panel uyum kartına "yüksek riskli cari" satırı

**YAZILMADI (Faz B — mühür 4 K9/K10 gereği dalga 8'e devredildi):**
`yaptirim_kayitlari`, `yaptirim_eslesmeleri`, `YaptirimServisi.cs`, `YaptirimKapisi.cs`,
`IYaptirimKaynagi`, Uyum ekranının "Yaptırım" sekmesi. `MasakKapisi.cs`'in 8 çağrı noktasına
**tek satır eklenmedi** (dosya hiç açılmadı).

`RiskGirdi` sözleşmesindeki `YaptirimEslesmesiVar` alanı motor tarafında DURUYOR (Faz B ona
yaslanacak) ama `RiskServisi` onu **her zaman `false`** verir ve bunun gerekçesi kodda yazılıdır:
"alanı varmış gibi doldurmak uydurma olurdu" (Anayasa md.1).

---

## 2. Spesifikasyonun koda karşı doğrulanması (sapmalar ve gerekçeleri)

| # | Spesifikasyon / tasarım iddiası | Koddaki gerçek | Karar |
|---|---|---|---|
| 1 | "`DenetimServisi`'ne `YazmaKaydet` **eklenir**" | `DenetimIzi.cs:135-141`'de **ZATEN VAR**, `SaveChanges` çağırmıyor, `CariModulu`'nda kullanımda | **PAKETTEN ÇIKARILDI** (Bertan kısmi vetosu + Mehmet K40-1). Mevcut imza **değiştirilmeden çağrıldı**; ikinci bir yazma-izi yolu açılmadı |
| 2 | `denetim.sh` ExecuteUpdate beyaz listesi "4 kalem" | Gerçek değer **5** (Auth ×4 + Kur ×1; F-2-min davet tüketimi eklenmiş) | Sayaç **5'te SABİT KALDI** — bu pakette büyütülmedi |
| 3 | `denetim.sh` `SAYFA_RECORD_TAVAN=122` | Gerçek değer **129** (T-3 Yedekler.razor 7 tip getirmiş) | Tavan **129'da SABİT KALDI**; Pages altında **yeni istemci record açılmadı** — mevcut `CariVeri`/`CariDetayVeri` genişletildi |
| 4 | `MasakRiskAyarlari` alan listesi | Motorun çalışabilmesi için 6 ek ayar gerekiyordu | `NakitAgirlikOrani`, `BandAltOran`, `BandUstOran`, `StructuringAsgariIslem`, `DegerlendirmePencereGunu`, `ElleGerekceAsgari`, `OzetListeSiniri`, `KimlikEksikPuan`, `UlkeRiskPuan` **eklendi**. Gerekçe: aksi hâlde bu rakamlar KODA GÖMÜLÜRDÜ (ADR-0005 ihlali) |
| 5 | `RiskGirdi.KimlikEksikSayisi` puana nasıl girer? | Tasarımda katsayı **YOK** | `KimlikEksikPuan` **varsayılan 0**: uydurulmuş bir katsayı skoru sessizce şişirirdi. Kuyumcu açıkça değer verirse işler (testle kanıtlı) |
| 6 | `YuksekRiskliUlkeler` için puan | Tasarımda katsayı **YOK** | `UlkeRiskPuan` eklendi ama **liste BOŞ** — listeyi biz uyduramayız (FATF/HMB listesini uyum görevlisi girer) |
| 7 | `Ozet` sorgusu için `GroupBy(...).First()` | Pomelo'da gruptan varlık döndüren projeksiyon istemciye kaçabilir | "daha yenisi YOK" (NOT EXISTS) desenine çevrildi; `(tenant_id, cari_id, olusturuldu)` indeksinden karşılanır |
| 8 | Yeni varlık kayıt yeri | `DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR` kapısı | `CariRiskProfili` **düzeltme politikası haritasına** `S(...)` (salt okunur) olarak eklendi; alternatif yol `api/v1/masak/risk/{cariId}/elle` |

### Mevzuat işaretleri (kurucuya / mali müşavire)
- **`MasakRisk:Etkin` VARSAYILAN `false`** ve bu bilinçlidir: risk temelli yaklaşımın *kuyumcuyu
  bağlayan* biçimi (hangi skorda hangi tedbir zorunlu) **DOĞRULANMADI**. Kapalıyken skor hesaplanır
  ve gösterilir, **hiçbir kapı kurmaz**. Açılması mali müşavir/uyum görevlisi teyidine bağlıdır.
- **Puanların kendisi mevzuat değil POLİTİKADIR.** Varsayılanlar başlangıç değerleridir; "doğru"
  oldukları iddia edilmez. `appsettings.json` `MasakRisk._not` alanında bu açıkça yazılıdır.
- **Risk skoru hiçbir koşulda satış / MASAK / tevsik kapısına bağlanmadı.** Mevzuatta doğrulanmış
  "yüksek riskliyle işlem yasaktır" hükmü YOKTUR; koymak uydurma olurdu.
- **`YuksekRiskliUlkeler` BOŞ** — resmî liste kurucudan/uyum görevlisinden gelmelidir.

---

## 3. Migration

**Ad:** `20260918011039_MasakRiskPep`
**Üretim:** `dotnet ef migrations add MasakRiskPep --project Kuyumcu.Api` (build'li, `--no-build` KULLANILMADI)
**Dosyalar:** `20260918011039_MasakRiskPep.cs` = 4.343 bayt · `.Designer.cs` = 181.649 bayt (ikisi de > 0)

`Up()` gövdesi okundu ve beklenen değişikliği içeriyor:
- `cariler` + `pep_beyan_tarihi datetime(6) NULL`, `pep_beyani tinyint(1) NOT NULL DEFAULT 0`, `pep_gorev varchar(200) NULL`
- `cari_risk_profilleri` tablosu (id, tenant_id, cari_id, kaynak, puan **int**, seviye, sebepler text, gerekce, kullanici_id, hesaplanma_tarihi, olusturuldu)
- İndeksler: `IX_cariler_tenant_id_pep_beyani`, `IX_cari_risk_profilleri_tenant_id_cari_id_olusturuldu`, `IX_cari_risk_profilleri_tenant_id_seviye`

**kuyumcu_net'e uygulandı:**
```
Applying migration '20260918011039_MasakRiskPep'.  Done.
```
SQL kanıtı:
```
pep_beyan_tarihi  datetime(6)    YES   NULL
pep_beyani        tinyint(1)     NO    0
pep_gorev         varchar(200)   YES   NULL
```
`__EFMigrationsHistory` en son satır: `20260918011039_MasakRiskPep`.

---

## 4. Testler

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Paket ÖNCESİ (hesaplanan taban) | 2698 | 2698 | 0 | 0 |
| Paket SONRASI (tam süit, son koşum) | **2754** | **2754** | **0** | **0** |

**Yeni test sayısı: 56**
- `RiskMotoruTestleri.cs` [saf] — **14** (ilk test: *OrtaEsik değiştirilince seviye değişir*, Bertan koşulu)
- `RiskServisiTestleri.cs` [DB] — **17**
- `PepBeyaniTestleri.cs` [DB] — **6**
- `RiskEkranTestleri.cs` [bUnit] — **11**
- `HttpBoruHattiTestleri.cs` [HTTP] — **+4**
- `CariEkranTestleri.cs` [bUnit] — **+4**

**Yol boyunca yakalanan gerçek kapı (gizlenmedi):** ilk tam süit koşumunda
`DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR` **KIRMIZI** döndü
("Politika haritasında damgası olmayan varlık(lar): CariRiskProfili"). Varlık haritaya eklenerek
çözüldü — bu, yeni varlık ekleyen her paketin geçmesi gereken meşru bir kapıdır.

Derleme: **0 hata**. `-t:Rebuild` ile ölçülen uyarı: **5**, hepsi bu paketin DOKUNMADIĞI
dosyalarda (`EkstreEkranTestleri.cs` ×3, `EkstreIceAktarmaTestleri.cs` ×2 — xUnit2031, paket 32
mirası). Bu paketin kendi dosyalarının uyarı katkısı **0**.

---

## 5. Mutasyon kanıtları

Yöntem: kaynak scratchpad'e kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli değişiklik →
filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile birebir aynılık.

| Kod | Değişiklik | KIRMIZIYA dönen test(ler) | Geri alındı |
|---|---|---|---|
| M40-1 | Seviye eşikleri koda gömüldü (`>= 70` / `>= 40`) | `RiskMotoruTestleri.OrtaEsik_degistirilince_seviye_degisir_esik_koda_gomulu_degildir` | cmp birebir |
| M40-2 | `RiskProfiliniKoru()` gövdesi kaldırıldı (append-only koruması) | `RiskServisiTestleri.Mevcut_profil_satirinda_UPDATE_reddedilir`, `...Profil_satiri_SILINEMEZ` | cmp birebir |
| M40-3 | `Hesapla` yeni satır yerine mevcut satırı GÜNCELLER | `RiskServisiTestleri.Iki_hesaplama_IKI_SATIR_yazar_eskisini_guncellemez` | cmp birebir |
| M40-4 | Elle ezmede gerekçe uzunluğu kontrolü kaldırıldı | `RiskServisiTestleri.Elle_ezme_gerekcesiz_400_doner`, `...Elle_ezmede_gerekce_asgarisi_YAPILANDIRMADAN_gelir` | cmp birebir |
| M40-5 | `DenetimServisi.YazmaKaydet` çağrısı silindi | `RiskServisiTestleri.Elle_ezme_gerekceliyse_ELLE_satiri_ve_denetim_kaydi_yazilir` | cmp birebir |
| M40-6 | Sebep listesine belge no + cari adı sızdırıldı | `RiskServisiTestleri.Risk_yanitlarinda_belge_no_alani_YOKTUR` | cmp birebir |
| M40-7 | `MasakRisk:Etkin` varsayılanı `true` yapıldı | `RiskServisiTestleri.Etkin_FALSE_varsayilandir_ve_yuksek_riskte_bile_satis_akisi_ETKILENMEZ`, `...Ozet_seviye_dagilimini_ve_PEP_sayisini_dondurur` | cmp birebir |
| M40-8 | `YuksekRiskliUlkeler` varsayılanı uydurma listeyle dolduruldu | `RiskMotoruTestleri.Ulke_listesi_BOSken_uyruk_hic_puan_uretmez` | cmp birebir |
| M40-9 | PEP'te görev zorunluluğu kaldırıldı | `PepBeyaniTestleri.Beyan_true_gorev_bos_400_doner_ve_hicbir_alan_degismez`, `...Pep_ucu_carinin_DIGER_alanlarina_dokunmaz` | cmp birebir |
| M40-10 | PEP ucu carinin telefon/adres alanlarını da eziyor (kurul engel 4) | `PepBeyaniTestleri.Pep_ucu_carinin_DIGER_alanlarina_dokunmaz` | cmp birebir |
| M40-11 | Kiracı kapsamı kaldırıldı (`IgnoreQueryFilters` + tenant koşulu yok) | `RiskServisiTestleri.Yabanci_kiracinin_carisi_icin_hesapla_404_doner_ve_satir_yazilmaz` | elle geri alındı (aşağıya bak) |
| M40-12 | Ekranda ön yükleme yasağı delindi (cari seçilmeden tekil uç çağrılıyor) | `RiskEkranTestleri.Cari_secilmeden_tekil_risk_ucu_HIC_cagrilmaz` | cmp birebir |
| M40-13 | Cariler çekmecesinde PEP kilidi kaldırıldı | `CariEkranTestleri.PEP_isaretli_gorev_BOS_iken_KAYDET_KAPALI` | cmp birebir |

**Hiçbir testi kırmayan mutasyon ÇIKMADI** — test açığı yok.

### ⚠ Mutasyon betiğinin kendi hatası (gizlenmedi)
Betiğin geri-alma denetimi kusurluydu: geri kopyaladığı dosyayı **aynı yedekle** karşılaştırıyordu,
bu yüzden *yedeğin kendisi kirlendiğinde* fark edemezdi. İki mutasyon artığı çalışma ağacında kaldı:
1. `RiskServisi.CariBul` içinde `IgnoreQueryFilters()` (M40-11 artığı)
2. `CariModulu.PepGuncelle` içinde `cari.Telefon = null; cari.Adres = null;` (M40-10 artığı)

**İkisi de yakalandı ve temizlendi:** (1) `denetim.sh`'in `IgnoreQueryFilters (yasak)` sayacı,
(2) tam süit (`Pep_ucu_carinin_DIGER_alanlarina_dokunmaz` KIRMIZI). Temizlikten sonra
**derleme 0 hata · denetim TEMİZ · 2754/2754 yeşil** yeniden koşuldu; kritik satırlar tek tek
grep ile doğrulandı (`Etkin` varsayılansız, `YuksekRiskliUlkeler = []`, eşikler `ayarlar.`'dan,
`RiskProfiliDegistirilemez` yerinde, gerekçe kontrolü yerinde, `YazmaKaydet` yerinde,
`KaydetAktif=... && PepGecerli`, `IgnoreQueryFilters` = **0**).

---

## 6. Denetim çıktısı

```
./dotnet/denetim.sh
...
  ✓ IgnoreQueryFilters (yasak)                     0
  ✓ Find/FindAsync (yasak)                         0
  ✓ ExecuteUpdate/Delete (beyaz liste 5)           5
  ✓ Hareketler.Add (yalnız HareketFabrikasi)       1
  ✓ IKiraciVarlik uygulayan varlık                55 (en az 51)
  ✓ Pages içinde istemci record tanımı           129 (tavan 129)
  ✓ Pages: @onclick'siz ve disabled'sız <button>  71 (tavan 71)
  ✓ Kültürsüz biçimli ToString (Web)              44 (tavan 44)
...
DENETİM TEMİZ.
```
`BEKLENEN_VARLIK` 50 → **51** yükseltildi (gerekçesi betikte yazılı: `CariRiskProfili`).
Diğer hiçbir sayaç/tavan **büyütülmedi**.

---

## 7. Canlı doğrulama (localhost:5198, kiracı `RISK-TEST`)

API `ASPNETCORE_ENVIRONMENT=Development ... --urls http://localhost:5198` ile arka planda
çalıştırıldı. **5199'a dokunulmadı** (kurucunun inceleme sunucusu koşum boyunca ve sonrasında
3 dinleyiciyle ayakta kaldı).

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | `PATCH /cari/{pep}/pep` — `pepBeyani:true`, görev YOK | 400 | **400** | "Siyasi nüfuz sahibi beyanı işaretliyse görev alanı zorunludur…" |
| 2 | Aynı uç, görev DOLU | 200 | **200** | `{"pepBeyani":true,"pepGorev":"Belediye meclis uyesi","pepBeyanTarihi":"2026-09-18T01:37:39Z"}` |
| 3 | SQL: `cariler.pep_*` | dolu | **dolu** | `1 \| Belediye meclis uyesi \| tarih NOT NULL = 1`; diğer iki cari `0 \| NULL \| 0` |
| 4 | SQL: PEP ucu diğer alanları ezmedi mi | telefon/adres değişmemiş | **değişmemiş** (üçü de NULL, başlangıçtaki hâl) | — |
| 5 | `POST /masak/risk/{normal}/hesapla` | 200, puan 0 DUSUK | **200** — puan **0**, `DUSUK`, sebepler `[]` | girdi: oran 0, toplam 0 |
| 6 | `POST /masak/risk/{pep}/hesapla` | 200 | **200** — puan **45**, `ORTA`, `["PEP_BEYANI","NAKIT_AGIRLIK"]` | girdi: pep true, oran 1, toplam 20.000 |
| 7 | `POST /masak/risk/{nakit}/hesapla` | 200 | **200** — puan **15**, `DUSUK`, `["NAKIT_AGIRLIK"]` | girdi: oran 1, toplam 25.000 |
| 8 | **perl ile bağımsız formül** (appsettings rakamlarıyla, motor koduna bakmadan) | üç puan birebir tutmalı | **BİREBİR TUTTU** → `NORMAL 0/DUSUK · PEP 45/ORTA · NAKIT 15/DUSUK` | — |
| 9 | 2. kez `hesapla` (PEP carisi) | yeni SATIR (güncelleme değil) | **200** | SQL: `cari_risk_profilleri` PEP carisi için **2 satır** (diğer ikisi 1'er) |
| 10 | `POST /masak/risk/{pep}/elle`, gerekçe **10** karakter | 400 | **400** | "…en az 20 karakter olmalıdır (girilen: 10)" |
| 11 | Aynı uç, gerekçe **52** karakter | 200 + `kaynak='ELLE'` | **200** — `kaynak ELLE`, `seviye YUKSEK`, `puan 45` | SQL: 3. satır `ELLE \| YUKSEK \| 45 \| [] \| "Uyum gorevlisi…"` |
| 12 | `denetim_kayitlari` (`varlik_turu='CariRiskProfili'`) | artmalı | **4 → 6** (+2: otomatik satır izi + GEREKÇELİ `YazmaKaydet`) | — |
| 13 | `MasakRisk:Etkin=false` iken **YÜKSEK riskli** cariyle işlem | 200 (risk kapı kurmaz) | **200** — tahsilat yazıldı | — |
| 14 | `GET /masak/risk/{pep}` | 200, tarihçe 3, kimlik verisi YOK | **200** — `guncel YUKSEK/ELLE`, tarihçe **3**, `kuralEtkin false`, `belgeNo` **yok** | — |
| 15 | `GET /masak/risk/ozet` | 200, dağılım + liste | **200** — `dusuk 2, orta 0, yuksek 1, pepSayisi 1, tazelenecek 0, profilsiz 0, kuralEtkin false, listeSiniri 50`, `yuksekRiskliler [("PEP Musteri",45,"YUKSEK")]` | — |
| 16 | `GET /cari` rozet alanları | `riskSeviye` + `pepBeyani` | **geldi** — `Nakit DUSUK/false · Normal DUSUK/false · PEP YUKSEK/true` | — |

**Temizlik:** API PID ile durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş** (0 satır);
`lsof -nP -iTCP:5199` **3 satır** (kurucunun sunucusu dokunulmadan ayakta).
`information_schema`'dan `tenant_id` kolonu olan **54 tablodan** `RISK-TEST` kiracısının satırları
silindi; `tenants` **8 → 9 → 8** (başlangıç değerine döndü), `cari_risk_profilleri` **0**,
`kiraci_davetleri` **0**. **`QA-TEST-1/2/3` kiracıları yerinde ve dokunulmadı.**

---

## 8. DoD sözleşmesi (Bertan mührü)

| Varlık / kayıt türü | Düzenleme yolu | PATCH semantiği | Silme / pasife alma | Yetki | Denetim kaydı | Ekran eylemi |
|---|---|---|---|---|---|---|
| **PEP beyanı** (`cariler.pep_*`) | `PATCH /api/v1/cari/{id}/pep` | **GERÇEK PATCH** — gönderilmeyen alan "değişmedi"; `CariGuncelleDto` gövdesi KULLANILMAZ, diğer kolonlara dokunulmaz | Beyan `false`'a çekilince görev + tarih temizlenir (geri dönüş yolu) | `[ModulYetkisi("cari")]` + `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` | `DenetimYazmaDinleyicisi` (otomatik, cari güncellemesi) | Cariler → Düzenle çekmecesi: onay kutusu + görev alanı; görev boşken **Kaydet KİLİTLİ** ve sebebi yazılı |
| **Risk profili** (`cari_risk_profilleri`) | **SALT OKUNUR — geçmiş satır DEĞİŞTİRİLEMEZ** | — | **Hard DELETE YASAK** (`RiskProfiliDegistirilemez`, SaveChanges düzeyinde) | `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]`, elle ezme **MUDUR+** | `DenetimServisi.YazmaKaydet` (elle ezmede, aynı SaveChanges) + otomatik satır izi | **`DegismezIpucu`** ile gerekçe ve düzeltme yolu ekranda yazılı; düzeltme = **Yeniden hesapla** ya da **Seviyeyi elle değiştir** (ikisi de YENİ satır) |

**Neden salt okunur:** risk profili append-only bir denetim kaydıdır — "o gün hangi skorla işlem
yaptın" sorusu denetimde sorulur. Gerekçe ekranda `DegismezIpucu` ile gösterilir, sessiz bırakılmaz
(UX-S25). Kayıt ayrıca `DuzeltmePolitikasi` haritasına `SALT_OKUNUR` olarak damgalandı.

---

## 9. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Kapsam yalnız FAZ A; yaptırım tarafı YAZILMADI | **KARŞILANDI** | `grep -rn "yaptirim\|Yaptirim" Kuyumcu.Api` → yalnız `RiskSebebi.YaptirimEslesmesi` + `YaptirimPuan` (Faz B sözleşmesi); `MasakKapisi.cs` **hiç açılmadı** |
| `DenetimIzi.cs`'e DOKUNULMADI, mevcut `YazmaKaydet` çağrıldı | **KARŞILANDI** | `git status`: `DenetimIzi.cs` değişmemiş; `RiskServisi.cs:226` mevcut imzayı çağırıyor |
| Elle ezmede iz + profil TEK SaveChanges'ta | **KARŞILANDI** | `RiskServisi.Elle`: `Add` → `YazmaKaydet` → tek `SaveChangesAsync`; canlı: denetim 4→6 |
| `RiskMotoru` SAF (DB/ağ/DateTime yok) | **KARŞILANDI** | Dosyada `db`, `DateTime`, `await` **yok**; 14 saf test |
| TEK sabit rakam koda yazılmadı | **KARŞILANDI** | M40-1 mutasyonu KIRMIZI; paketin İLK testi bu |
| DB testlerinde ayar ctor'dan verilir (IConfiguration DEĞİL) | **KARŞILANDI** | `RiskServisiTestleri.Servis(db, ayar, esik, denetim)` |
| `YuksekRiskliUlkeler` BOŞ → uyruk 0 puan | **KARŞILANDI** | `RiskMotoruTestleri.Ulke_listesi_BOSken_uyruk_hic_puan_uretmez` + M40-8 |
| `MasakRisk:Etkin` varsayılan FALSE, TevsikKapisi deseniyle | **KARŞILANDI** | `RiskAyarlari.cs:28` varsayılansız `bool`; `appsettings` `false`; canlı adım 13-15 |
| Risk skoru HİÇBİR kapıya bağlanmadı | **KARŞILANDI** | M40-7 mutasyonu satış akış testini KIRMIZI yaptı; canlı adım 13 (YÜKSEK riskli cariyle işlem 200) |
| Sebep listesi ŞİB gerekçesini OTOMATİK DOLDURMUYOR | **KARŞILANDI** | `MasakVarliklari.cs` şüpheli bildirim yolu hiç açılmadı; Risk sekmesi yalnız gösterir |
| `cari_risk_profilleri` APPEND-ONLY, Update/Delete reddedilir | **KARŞILANDI** | `KuyumcuDbContext.RiskProfiliniKoru()`; testler + M40-2/M40-3 |
| Profil yazımı TEK yoldan (`RiskServisi`) | **KARŞILANDI** | `grep "CariRiskProfilleri.Add"` → yalnız `RiskServisi.cs` (2 çağrı, aynı sınıf) |
| Kiracı izolasyonu: `IKiraciVarlik` + global filtre + 2 indeks + cariler indeksi | **KARŞILANDI** | Migration indeksleri; `denetim.sh` varlık sayacı 51'e çekildi; `Yabanci_kiracinin_carisi…` testi + M40-11 |
| ExecuteUpdate beyaz listesi büyütülmedi | **KARŞILANDI** | `denetim.sh` → 5 (paket öncesi hâl) |
| KVKK okuma izi: tekil uçta `OkumaKaydet`, listede `ListeOkumaKaydet` | **KARŞILANDI** | `RiskServisi.Getir` / `.Ozet`; `Tekil_risk_ucu_OKUMA_IZI_duser` testi |
| `sebepler` JSON'ında yalnız kod sabiti | **KARŞILANDI** | `Sebepler_YALNIZ_kod_sabitidir…` + `Risk_yanitlarinda_belge_no_alani_YOKTUR` + M40-6 |
| Pages'te yeni istemci record açılmadı, tip sözleşmesi birebir | **KARŞILANDI** | `denetim.sh` 129/129; `puan` sunucuda `int` ↔ istemcide `int` |
| `PATCH /cari/{id}/pep` gerçek PATCH; ad/telefon/adres sıfırlanmıyor | **KARŞILANDI** | `PepBeyaniTestleri` ×3 + M40-9/M40-10 + canlı adım 4 |
| `pep_beyan_tarihi` sunucuda set edilir | **KARŞILANDI** | `CariPepDto`'da alan YOK; canlı adım 2 |
| Açılan her veri girişi aynı pakette düzenlenebilir (kurucu şartı) | **KARŞILANDI** | PEP → Cariler çekmecesi; risk seviyesi → Uyum/Risk "elle değiştir"; tek düzeltilemeyen kalem (geçmiş satır) `DegismezIpucu` ile gerekçeli |
| Yeni sayfa/menü AÇILMADI | **KARŞILANDI** | `KenarCubugu.razor` **değişmedi**; sekme mevcut çubuğa eklendi |
| Ön yükleme YASAK (cari seçilmeden tekil uç 0 kez) | **KARŞILANDI** | `Cari_secilmeden_tekil_risk_ucu_HIC_cagrilmaz` + M40-12 |
| Her düğme gerçek istek atar (DoD EK md.1) | **KARŞILANDI** | bUnit: `uy-risk-hesapla` → POST hesapla; `cekmece-risk-elle-kaydet` → POST elle; `Goruntule` → GET risk/{id}; `denetim.sh` ölü düğme sayacı 71'de sabit |
| Tek düzenleme kalıbı (Cekmece), yeni ModalPencere düzenlemesi yok | **KARŞILANDI** | Elle ezme **Cekmece** ile yazıldı; `denetim.sh` sayaçları sabit |
| Sözlükler TR+EN, çıplak metin yok | **KARŞILANDI** | `SozlukUyum` +38 anahtar, `SozlukCari` +7, `SozlukPanel` +1; hepsi TR ve EN'de dolu (derleme `required` ile zorlar) |
| `disabled="false"` dizge tuzağına düşülmedi | **KARŞILANDI** | Kilit `Cekmece.KaydetAktif` bayrağıyla; bUnit `HasAttribute("disabled")` testleri |
| Migration prosedürü (build'li, `--no-build` yok, dosyalar okundu) | **KARŞILANDI** | §3 |
| Test kiracısı silindi, `tenants` başlangıca döndü | **KARŞILANDI** | 8 → 9 → **8**; `QA-TEST-*` dokunulmadı |
| 5199'a dokunulmadı | **KARŞILANDI** | Koşum sonrası `lsof -iTCP:5199` **3 satır** |

---

## 10. Değişen dosyalar

**Yeni:**
```
dotnet/Kuyumcu.Api/Modules/Masak/RiskAyarlari.cs
dotnet/Kuyumcu.Api/Modules/Masak/RiskMotoru.cs
dotnet/Kuyumcu.Api/Modules/Masak/RiskServisi.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260918011039_MasakRiskPep.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260918011039_MasakRiskPep.Designer.cs
dotnet/Kuyumcu.Tests/RiskMotoruTestleri.cs
dotnet/Kuyumcu.Tests/RiskServisiTestleri.cs
dotnet/Kuyumcu.Tests/PepBeyaniTestleri.cs
dotnet/Kuyumcu.Tests/RiskEkranTestleri.cs
```

**Değişen:**
```
dotnet/Kuyumcu.Api/Data/Entities.cs                       (Cari + 3 PEP kolonu)
dotnet/Kuyumcu.Api/Data/MasakVarliklari.cs                (CariRiskProfili, RiskKaynagi, RiskSeviyesi, RiskProfiliDegistirilemez)
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs               (DbSet, 3 indeks, RiskProfiliniKoru)
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
dotnet/Kuyumcu.Api/Modules/Masak/MasakModulu.cs           (4 yeni uç)
dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs             (CariPepDto, PepGuncelle, PATCH /pep, liste rozeti, Getir pep alanları)
dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs     (CariRiskProfili damgası)
dotnet/Kuyumcu.Api/Modules/ModulKayit.cs                  (RiskServisi DI)
dotnet/Kuyumcu.Api/Program.cs                             (MasakRiskAyarlari singleton)
dotnet/Kuyumcu.Api/appsettings.json                       ("MasakRisk" bölümü)
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                     (TR+EN, 46 anahtar + 4 çevirici metot)
dotnet/Kuyumcu.Web/Servisler/UyumServisi.cs               (4 risk metodu + 6 sözleşme tipi)
dotnet/Kuyumcu.Web/Pages/Uyum.razor                       (Risk sekmesi + elle ezme çekmecesi)
dotnet/Kuyumcu.Web/Pages/Cariler.razor                    (PEP alanları, risk/PEP rozeti)
dotnet/Kuyumcu.Web/Pages/Panel.razor                      (yüksek riskli cari satırı)
dotnet/denetim.sh                                         (BEKLENEN_VARLIK 50 -> 51)
dotnet/Kuyumcu.Tests/HttpBoruHattiTestleri.cs             (+4 rol/400 testi)
dotnet/Kuyumcu.Tests/HttpOrtami.cs                        (PatchJson kısayolu)
dotnet/Kuyumcu.Tests/CariEkranTestleri.cs                 (+4 PEP/rozet testi)
```

### Dokunulan Razor sayfaları (DoD EK 2 — TEK DOKUNUŞ)
`Kuyumcu.Web/Pages/Uyum.razor` · `Kuyumcu.Web/Pages/Cariler.razor` · `Kuyumcu.Web/Pages/Panel.razor`

**COMMIT YAPILMADI** — çalışma ağacında bırakıldı.

---

## 11. Açık kalanlar / kurucuya notlar

1. **`dotnet/Kuyumcu.Tests/GeciciProbe.cs` — 0 baytlık geçici dosya.** Kiracı filtresinin
   davranışını ölçmek için açıldı, işi bitince `printf '' >` ile boşaltıldı; TurkReport hook'u
   `rm`'i blokladığı için **silinemedi**. Kurucu terminalden silebilir:
   `rm dotnet/Kuyumcu.Tests/GeciciProbe.cs`
2. **Mali müşavir / uyum görevlisi teyidi gereken üç kalem:** (a) `MasakRisk:Etkin`'in açılıp
   açılmayacağı ve hangi skorda hangi tedbirin zorunlu olduğu, (b) puan katsayılarının kuyumcu
   politikasına uygunluğu, (c) `YuksekRiskliUlkeler` listesinin resmî kaynağı (FATF/HMB).
   Üçü de şu an **tutucu** (kapalı / boş) durumdadır.
3. **Faz B devredildi (dalga 8):** yaptırım listesi, ad eşleşmesi, `YaptirimKapisi`,
   `IYaptirimKaynagi`, Uyum "Yaptırım" sekmesi. `RiskGirdi.YaptirimEslesmesiVar` sözleşmesi hazır.
4. **`KimlikEksikPuan` varsayılan 0** — bu kalem şu an skora HİÇ girmiyor. Katsayı kuyumcu/mali
   müşavir tarafından belirlenmedikçe uydurulmadı.
5. **Structuring (eşiğe yakın işlem) canlıda gösterilemedi:** tevsik haddi (30.000 TL) aynı gün
   aynı cariyle nakit işlemi sınırladığı için 148.000–185.000 bandında canlı işlem üretilemedi.
   Kural **DB testiyle** kanıtlandı (`Esige_yakin_tekrarlayan_islem_ESIGE_YAKIN_sebebi_uretir`,
   girdiÖzeti `esigeYakinIslem = 2`).
6. **Paketten bağımsız bulgu (orkestratöre):** `Kuyumcu.Api/Data/Migrations/` altında
   **4 adet uygulanmamış** `FiyatListesiVeKampanya` migration dosyası var
   (`20260915131748`, `132123`, `132303`, `132645`) — `__EFMigrationsHistory`'de 47 satır,
   diskte 51 migration. Paket 49 mirası; bu pakette **dokunulmadı**.
7. **Derlemede 5 uyarı** var ve hepsi paket 32 mirası `Ekstre*` test dosyalarında (xUnit2031).
   Bu paketin katkısı 0; temizlik ayrı bir kalem olarak kalıyor.
