# Paket 3 — Sipariş çatısı (durum makinesi) · Faz A — uygulama raporu

> Tarih: 2026-09-16 · Dalga 2 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) rolü
> Durum: **BİTTİ** (Faz A). Faz B kapsam dışı bırakıldı (aşağıda gerekçeli).

---

## 0. Önkoşul doğrulaması (ilk adım)

| Önkoşul (4-A, dalga 1) | Kanıt | Sonuç |
|---|---|---|
| `belgeler` tablosu | `Kuyumcu.Api/Data/UrunBelgesi.cs` → `[Table("belgeler")] class Belge`, DbSet `Belgeler` (KuyumcuDbContext:74) | VAR |
| `BelgeYukleyici` bileşeni | `Kuyumcu.Web/Bilesenler/BelgeYukleyici.razor` | VAR |
| `BelgeSahipTuru.Siparis` | `UrunBelgesi.cs:67` `public const string Siparis = "SIPARIS";` | VAR |
| Modül-başı idempotent tohumlama (paket 6-A) | `YetkiModulu.cs` → `ModulMarker(modul) => "__tohum:{modul}__"` + `TohumlaGerekiyorsa` | VAR, **yeniden yazılmadı** |

DURULMADI; uygulamaya geçildi.

---

## 1. Yapılanlar

### Veri modeli (YENİ dosya — `Entities.cs`'e DOKUNULMADI)
`dotnet/Kuyumcu.Api/Data/Siparis.cs`
- `Siparis` (tablo `siparisler`) — no, tür, durum, cari, şube, söz verilen gün, açıklama, tahmini tutar + kur, **opsiyonel** `emanet_hareket_id`, `guncellendi` (iyimser kilit damgası).
- `SiparisKalemi` (tablo `siparis_kalemleri`) — urun_id? / tanim? / ayar_id? / gram / adet / sıra.
  **Katman 1 sızıntısı YOK:** fire, usta payı, ayar düşürme alanı eklenmedi.
- `SiparisGecisi` (tablo `siparis_gecisleri`) — append-only geçiş izi.
- Kapalı listeler: `SiparisTuru` (3), `SiparisDurumu` (**6** — kapalı liste testle kilitli).
- `SiparisGecisiDegistirilemez` istisnası.

### Durum makinesi (saf sınıf)
`dotnet/Kuyumcu.Api/Modules/Siparis/SiparisDurumMakinesi.cs`
- `Izinli(tur, mevcut, yeni)`, `Hedefler(tur, mevcut)`, `GerekceGerekli(mevcut, yeni)`, `Terminal(durum)`.
- Tablo spesifikasyondaki gibi; `TESLIM_EDILDI` ve `IPTAL` tabloda anahtar TAŞIMAZ → terminal. Bilinmeyen tür/durum → `false` (fail-closed).
- Aynı dosyada `SiparisGecikme.Gecikti(sozVerilenGun, durum, bugun)` — **saat soyutlaması** (bkz. §7).

### Sipariş numarası
`dotnet/Kuyumcu.Api/Modules/Siparis/SiparisNo.cs` — `S-YYYY-NNNNNN`, kiracı bazlı.
Tekillik **veritabanında**: `(tenant_id, no)` UNIQUE. Çakışmada **sınırlı döngü** (`AzamiDeneme = 5`), tükenirse **409** — sessiz ikinci ULID YOK. Sayaç tablosu AÇILMADI.

### Uçlar
`dotnet/Kuyumcu.Api/Modules/Siparis/SiparisModulu.cs` (DTO + servis + controller — Emanet deseni)
Sınıf düzeyi: `[Authorize]` + `[Yetki.ModulYetkisi("siparis")]`.

| Fiil | Yol | `[RolGerekli]` | Not |
|---|---|---|---|
| POST | `/api/v1/siparisler` | SAHIP, MUDUR, MUHASEBE, TEZGAHTAR | TASLAK doğar + ilk geçiş satırı (`onceki=null`) |
| GET | `/api/v1/siparisler` | (modül yetkisi) | limit ≤ **200**, toplam + durum kırılımı sunucuda GroupBy |
| GET | `/api/v1/siparisler/{id}` | (modül yetkisi) | tam kart + kalemler + geçişler |
| PATCH | `/api/v1/siparisler/{id}` | SAHIP, MUDUR, MUHASEBE, TEZGAHTAR | **yalnız TASLAK**, aksi 400 |
| POST | `/api/v1/siparisler/{id}/gecis` | SAHIP, MUDUR, MUHASEBE, TEZGAHTAR — **IPTAL yalnız SAHIP/MUDUR** (servis kapısı, 403) | durum + geçiş satırı AYNI transaction |
| GET | `/api/v1/siparisler/{id}/gecisler` | SAHIP, MUDUR, MUHASEBE | append-only döküm |

### Değiştirilen mevcut dosyalar
- `Data/KuyumcuDbContext.cs` — 3 DbSet, 6 indeks (`(tenant_id,no)` UNIQUE · **`(tenant_id, durum, soz_verilen_gun)`** · `(tenant_id,tur,durum)` · `(tenant_id,cari_id)` · kalem · geçiş) + **append-only zorlaması**: `SaveChanges/SaveChangesAsync` override → `SiparisGecisi` için `Modified/Deleted` → istisna (DenetimYazmaDinleyicisi deseni).
- `Modules/ModulKayit.cs` — tek satır DI.
- `Modules/Yetki/YetkiModulu.cs` — `Moduller.Tumu` + `Varsayilan` (MUDUR/MUHASEBE/TEZGAHTAR) `"siparis"`. `TohumlamaOncesi` **büyütülmedi** → mekanizma yeni anahtarı eski kiracılarda da açar.
- `Modules/Meta/DuzeltmePolitikasi.cs` — üç yeni varlığın düzeltme damgası (§9 DoD).
- `Kuyumcu.Core/I18n/Sozlukler.cs` — `SozlukSiparis` (61 anahtar) + `SozlukNav.Siparisler`; **TR ve EN birlikte**.
- `Kuyumcu.Web/Layout/KenarCubugu.razor` — `Gorunur("siparis", "/siparisler")` NavLink.
- `Kuyumcu.Web/Layout/RotaBasliklari.cs` — `/siparisler`, `/siparisler/{Id}`.
- `dotnet/denetim.sh` — `BEKLENEN_VARLIK` 17 → **20**.

### Ekranlar (YENİ)
- `Kuyumcu.Web/Pages/Siparisler.razor` — tür sekmeleri (**atölye + tamir `ProAlan` içinde**, basit modda gizli), durum süzgeci, "yalnız gecikenler", sunucu sınırı uyarısı, özet kartı, boş durum, hata/başarı bandı, yeni sipariş formu (cari arama+seçim, tek şubede seçici gizli + salt okunur satır, kalem satırları, **fiyat `GET /fiyat/teklif` ile sunucudan**).
- `Kuyumcu.Web/Pages/SiparisDetay.razor` — kart, kalem tablosu, append-only zaman çizelgesi, **TASLAK'ta düzenleme formu** (oluşturmayla aynı alan kümesi), terminalde `DegismezIpucu`, yalnız izinli geçiş düğmeleri, IPTAL için ortak onay modalı + `GerekceAlani(10)`.
- bUnit sahte API'lara yeni uçların **gerçekçi yanıtları** eklendi (`SiparisEkranTestleri`).

---

## 2. Spesifikasyondan sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| 1 | **Faz B tümüyle kapsam dışı**: `TESLIM_EDILDI` için `hareketId` zorunluluğu, kapora bağı, `/siparisler/ozet` ucu, `BelgeModulu.cs` SIPARIS dalı ve `BelgeDeposuTestleri.cs:137` davranış değişikliği YAPILMADI. | Görev metni "yalnız Faz A" dedi. `BelgeDeposuTestleri`'ndeki yeşil test **silinmedi, dokunulmadı** (Faz B'de güncellenecek). |
| 2 | `siparis_gecisleri`'nde **`hareket_id` kolonu AÇILMADI**. | Faz A'da hiç yazılmayacak bir kolon "kullanılmayan alan" olurdu; Faz B kendi migration'ıyla ekler (Anayasa md.1 — varsayım yok). |
| 3 | Spesifikasyon tohumlama borcunu **paket 29**'a bağlamıştı; mekanizma paket **6-A**'da kurulmuş. | Kod gerçeği: `YetkiModulu.cs` `ModulMarker` + `TohumlaGerekiyorsa`. Yeniden yazılmadı, yalnız yeni anahtar eklendi; HTTP regresyon testiyle kilitlendi. |
| 4 | PATCH gövdesi DTO yerine ham `JsonElement` okunuyor. | "Gövdede yoksa KORU, null ise SİL" sözleşmesi nullable DTO ile **ölçülemez** (gönderilmedi ≠ null). |
| 5 | Append-only zorlaması ayrı bir `SaveChangesInterceptor` yerine `KuyumcuDbContext` override'ı. | Yeni interceptor `Program.cs` + `TestOrtami.cs` kaydı isterdi; `Program.cs` "DOKUNULMAYACAK" listesinde. Davranış aynı, kapsama daha geniş (her bağlam). |
| 6 | Atölye siparişinde `ONAYLANDI → HAZIR` geçişi YOK (spesifikasyon tablosuna sadık; müşteri siparişi ve tamirde VAR). | Spesifikasyon tablosu böyle; ayrıca iş anlamı doğru — mal dış atölyeye verilmeden hazır olamaz. Saf testle kilitlendi. |
| 7 | `emanet_hareket_id` doğrulaması spesifikasyondan **daha sıkı**: aynı kiracı **ve aynı cari** şartı. | Mehmet'in Faz B koşulunun ruhu ("başka müşterinin hareketi bağlanmasın") Faz A'daki emanet bağına da uygulandı. Mutasyon M148-2 ile ölçüldü. |
| 8 | `Bicim.Para(tutar, paraBirimi)` yok; `Bicim.Tl` kullanıldı, etiket "(TRY)" yazıyor. | Alan adı `tahmini_tutar_tl` — tek para birimi TL'dir, "varsayılan TL" varsayımı yok (UX-S32'nin amacı korunmuş). |

**Mevzuat:** spesifikasyonun `DOGRULANDI` işaretli iki hükmü (459 VUK tevsik haddi, MASAK 185.000 TL eşiği) için **sipariş modülüne yeni kapı KONULMADI** — para el değiştirmediği için ölçüm ödeme anındadır ve mevcut kapılar zaten çalışır. Sipariş kimliği MASAK kümülatifine **sokulmadı**. `DOGRULANMALI` işaretli 6502/TKHK tamir belgesi yükümlülüğü **koda hiç yazılmadı** (§11 notu).

---

## 3. Derleme

```
dotnet build Kuyumcu.sln --nologo   →  0 Hata   0 Uyarı
```
(son temiz derleme, mutasyon zinciri geri alındıktan sonra tekrarlandı)

---

## 4. Migration

- Ad: **`20260916184850_SiparisCatisi`**
- Üretim: `DOTNET_ENVIRONMENT=Development dotnet ef migrations add SiparisCatisi --project Kuyumcu.Api` (**build'li**, `--no-build` KULLANILMADI)
- İçerik doğrulandı: `CreateTable siparisler / siparis_kalemleri / siparis_gecisleri` + 6 `CreateIndex` (biri `unique: true`), `Down` üç tabloyu düşürüyor. Dosyalar 8.007 B / 123.024 B (0 bayt değil).
- Uygulama: `dotnet ef database update --connection "…database=kuyumcu_net…"` → `Applying migration '20260916184850_SiparisCatisi'. Done.`
- Kanıt:
```
SHOW TABLES LIKE 'siparis%';  →  siparis_gecisleri | siparis_kalemleri | siparisler
```

---

## 5. Testler

| Koşum | Toplam | Geçti | Başarısız |
|---|---|---|---|
| Paket 3 filtresi (`~Siparis`) | 59 (58'i yeni + 1 mevcut `BelgeDeposuTestleri`) | 59 | 0 |
| **Tam süit (önce)** | 1.592 | 1.592 | 0 |
| **Tam süit (sonra)** | **1.650** | **1.650** | **0** |

**Yeni test sayısı: 58**
- `SiparisDurumMakinesiTestleri.cs` [saf] — 8
- `SiparisTestleri.cs` [DB] — 24
- `HttpSiparisTestleri.cs` [HTTP] — 9
- `SiparisEkranTestleri.cs` [bUnit] — 17

TRX: `…/scratchpad/trx-p3/`

### Ara kırmızılar (gizlenmedi — Anayasa md.1)
İlk tam süit koşumu **3 kırmızı** verdi; üçü de bu paketin doğurduğu regresyonlardı ve çözüldü:

1. `RotaBaslikTestleri.Yansima_TAM_28_rota_bulur` — beklenen 28, gerçek 30.
   → İki yeni sayfa bilinçli eklendi; test `Yansima_TAM_30_rota_bulur` olarak güncellendi (sayı sabiti kasıtlı bir kapı).
2. `DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR` — "damgası olmayan varlık(lar): Siparis, SiparisKalemi, SiparisGecisi".
   → `DuzeltmePolitikasi.cs`'e üç damga eklendi (§9).
3. `BildirimHttpTestleri.Eski_marker_li_kiracida_bildirim_anahtari_TOHUMLANIR` — MUDUR "bildirim" göremiyordu.
   → **Kök neden**: fikstür "paket 6 öncesi kiracı"yı kurarken yalnız `bildirim` satırlarını siliyordu. İkinci bir tohumlama-sonrası anahtar (`siparis`) gelince markerı silinmiş ama **satırları duran** `siparis` yüzünden tohumlama INSERT'i benzersizlik ihlaline düşüyor, `catch (DbUpdateException)` TÜM partiyi (bildirim dahil) yutuyordu. Fikstür `Moduller.TohumlamaOncesi` listesine göre silecek biçimde düzeltildi — **mekanizmaya dokunulmadı**. Üretimde gerçek eski kiracıda bu durum oluşmaz (tohumlama-sonrası anahtarın satırı da yoktur).

---

## 6. Mutasyon kanıtları

Yöntem: kaynak `…/scratchpad/mut-yedek/paket3/` altına kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli değişiklik → filtreli `dotnet test -v n` → yedek geri kopyalandı → **`cmp` ile birebir aynılık kanıtlandı** (her turda 3/3 dosya).

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (cmp) |
|---|---|---|---|
| M143-1 | `SiparisDurumMakinesi.Izinli` her zaman `true` | `Izinli_gecis_listesi_tur_bazinda_birebir_dogrulanir`, `TESLIM_EDILDI_terminaldir_hicbir_cikis_yok`, `IPTAL_terminaldir_hicbir_cikis_yok`, `Atolye_siparisi_ONAYLANDIdan_dogrudan_HAZIRa_gecemez`, `Bilinmeyen_tur_ve_durum_false_doner_fail_closed`, `SiparisTestleri.Yasak_gecis_TASLAKtan_TESLIM_EDILDIye_reddedilir`, `HttpSiparisTestleri.Gecersiz_durum_ve_yasak_gecis_400_doner` (7) | OK |
| M144-1 | PATCH'in TASLAK kapısı kaldırıldı (terminal düzenlenebilir olur) | `TESLIM_EDILDI_sonrasi_PATCH_reddedilir`, `HttpSiparisTestleri.PATCH_TASLAK_disinda_400_doner` | OK |
| M145-1 | `GerekceGerekli` IPTAL'de `false` döner | `Geri_gecis_ve_IPTAL_icin_gerekce_zorunludur`, `Iptal_gerekcesiz_reddedilir_gerekceliyse_gecis_satiri_yazilir` | OK |
| M146-1 | Geçişte `siparis_gecisleri` satırı yazılmaz (yalnız durum güncellenir) | `Iptal_gerekcesiz_reddedilir_gerekceliyse_gecis_satiri_yazilir`, `Gecis_dokumu_her_adimda_bir_satir_buyur` | OK |
| M147-1 | Geçiş satırı **doğrulamalardan önce** ve transaction dışında ayrı `SaveChanges` ile yazılır | `Yasak_gecis_TASLAKtan_TESLIM_EDILDIye_reddedilir`, `Iptal_gerekcesiz_reddedilir…`, `Gecis_dokumu_her_adimda_bir_satir_buyur` | OK |
| M148-1 | `Bul()` içindeki açık `TenantId ==` koşulu kaldırıldı | **İLK DENEMEDE YEŞİL (test açığı)** → `Servisin_kendi_tenant_sarti_global_filtre_OLMASA_da_yabanci_siparisi_reddeder` testi eklendi (filtresiz/sistem bağlamıyla ölçer) → **M148-1b tekrarında KIRMIZI** | OK |
| M148-2 | `EmanetDogrula`'daki `h.CariId == cariId` şartı kaldırıldı | `Ayni_kiracida_baska_carinin_emanet_hareketi_tamire_baglanamaz` | OK |
| M149-1 | Tahmini tutar varken kur zorunluluğu kaldırıldı | `Tahmini_tutar_varken_kur_yoksa_reddedilir` | OK |
| M151-1 | Liste ucundaki `.Take(etkinLimit)` kaldırıldı | `HttpSiparisTestleri.Limit_1000_istense_de_en_cok_200_satir_doner` (test `?limit=2` → 2 satır + `dahaFazlasiVar` ile güçlendirildi) | OK |
| M152-1 | Fiyat ekranda hesaplanır (`/fiyat/teklif` çağrılmaz) | `SiparisEkranTestleri.Fiyat_ekranda_hesaplanmaz_teklif_ucu_kurSnapshotId_ile_cagrilir` | OK |
| M-KILIT-1 | İyimser kilitte koşulsuz `ToUniversalTime()` (canlıda bulunan hatanın kendisi) | `Guncel_damga_KABUL_edilir_Zli_ve_Zsiz_bicimde` | OK |

**M150 (Faz B — teslimde `hareketId` zorunluluğu) KOŞULMADI:** ilgili kod Faz A'da yok. Faz B paketiyle birlikte koşulmalıdır.

**Test açığı bulgusu (M148-1):** servis içindeki açık `TenantId ==` koşulu, kiracıya sabitlenmiş bağlamda **global sorgu filtresiyle çakışan ikinci bir katmandır**; tek başına ölçülmüyordu. İki yeni test eklendi: (a) `Global_kiraci_filtresi_siparis_tablolarini_kapsar` — asıl kapıyı ölçer, (b) `Servisin_kendi_tenant_sarti_…` — filtresiz bağlamda ikinci katmanı ölçer. Mutasyon tekrarında kırmızı.

---

## 7. M146/M147 ölçüm yöntemi ve "Geciken rozeti" saat soyutlaması (Aşkın'ın koşulu)

**M146/M147 hata enjeksiyonu:** ayrı bir hata enjeksiyon altyapısı kurulmadı; **ürünün kendi fail-closed kapıları** enjeksiyon olarak kullanıldı:
1. `TASLAK → TESLIM_EDILDI` (durum makinesi reddi) ve
2. gerekçesiz `IPTAL` (gerekçe kapısı reddi)
çağrıları 400 attıktan **sonra** SQL sayımı yapılır: `siparisler.durum` DEĞİŞMEMİŞ ve `COUNT(siparis_gecisleri) == 1` olmalıdır. Geçiş satırı transaction dışına çıkarıldığında (M147-1) ya da hiç yazılmadığında (M146-1) bu sayım bozulur.

**"Geciken rozeti" saat soyutlaması:** projede saat soyutlaması YOK ve bu paket için yeni bir tane açmak ikinci bir zaman gerçeği doğururdu. Çözüm üç parçalı:
1. Karar saf bir işlevde: `SiparisGecikme.Gecikti(sozVerilenGun, durum, bugun)` — **"bugün" PARAMETRE** → saf testle ölçülür (sınır günü, terminal durum, söz verilmemiş sipariş).
2. Sunucu bunu `Zaman.YerelBugun()` ile çağırır (K5 — gece 00:00-03:00 penceresi işletme saat dilimine göre).
3. **Ekran kendi saatine BAKMAZ**; sunucunun döndürdüğü `geciken` bayrağını çizer → bUnit testi sahte saat GEREKTİRMEZ (`Geciken_rozeti_SUNUCUNUN_bayragindan_cizilir`). Bu aynı zamanda UX-S21'dir: sunucunun bildiği değer istemcide yeniden hesaplanmaz.

---

## 8. Denetim çıktısı

```
./dotnet/denetim.sh
  ✓ IgnoreQueryFilters (yasak)                     0
  ✓ new KuyumcuDbContext (yalnız fabrika)          1
  ✓ Find/FindAsync (yasak)                         0
  ✓ ExecuteUpdate/Delete (beyaz liste 4)           4
  ✓ Hareketler.Add (yalnız HareketFabrikasi)       1      ← DEĞİŞMEDİ
  ✓ VeresiyeKapanisi (yalnız Etiketten)            2
  ✓ IKiraciVarlik uygulayan varlık                40      (alt sınır 17 → 20)
  ✓ HasQueryFilter                                 2
  ✓ Finansal varlıkta [HttpDelete] (0 olmalı)      0
DENETİM TEMİZ.
```

---

## 9. DoD sözleşmesi (Bertan koşulu + MÜHÜR 2)

| Varlık | Düzenleme yolu | Pasife alma / yıkıcı yol | Yetki | Denetim kaydı | Ekran eylemi |
|---|---|---|---|---|---|
| `Siparis` | `PATCH /api/v1/siparisler/{id}` — **yalnız TASLAK** | Hard DELETE **YOK**; `POST /{id}/gecis` ile `IPTAL` (gerekçe ≥10 zorunlu) | `[ModulYetkisi("siparis")]` + `[RolGerekli]`; IPTAL yalnız SAHIP/MUDUR | `DenetimYazmaDinleyicisi` SaveChanges'ı dinler → Ekleme/Güncelleme kendiliğinden düşer (**yeniden yazılmadı**) | `SiparisDetay` → "Düzenle" (TASLAK) / `DegismezIpucu` (terminal) + onay modallı "İptal et" |
| `SiparisKalemi` | Siparişin PATCH ucundan (kalem kümesi sil+yaz) | — | Sipariş ucuyla aynı | aynı dinleyici | Düzenleme formundaki kalem satırları |
| `SiparisGecisi` | **DEĞİŞMEZ (append-only)** — `SaveChanges` UPDATE/DELETE'i istisnayla reddeder | — | Okuma: SAHIP/MUDUR/MUHASEBE | aynı dinleyici (Ekleme) | Zaman çizelgesinde `AppendOnly` notu; düzeltme yolu **yeni bir geçiş**tir |

**Neden salt okunur:** geçiş izi "kim, ne zaman, hangi gerekçeyle" damgasıdır; sonradan düzeltilebilseydi iz olmaktan çıkardı (ADR-0004 ruhu **koda döküldü**, yorumla bırakılmadı).

**DoD EK (kurul tur 1):**
1. **Her düğme ya istek atar ya disabled + sebep**: teklif düğmesi (`#sip-f-teklif` → sebep `#sip-f-teklif-sebep`), iptal düğmesi tezgâhtarda disabled + `#sipd-gecis-IPTAL-sebep`, iptal onayı gerekçe kısa iken disabled. bUnit testleri: `Urun_secilmeden_teklif_dugmesi_kilitlidir_ve_sebebini_soyler`, `Tezgahtar_icin_iptal_dugmesi_kilitli_ve_sebebi_yazili`, `Iptal_dugmesi_gerekce_bosken_istegi_GONDERMEZ`, `Gecis_dugmesi_gercek_istek_atar…`.
2. **Tek düzenleme kalıbı**: çok alanlı düzenleme sayfa içi formda (Cekmece 2c'de gelecek); **yeni `duzenlenenId` + ModalPencere düzenleme kalıbı AÇILMADI** — `ModalPencere` yalnız yıkıcı onay (IPTAL) için kullanıldı. `denetim.sh` sayaçları değişmedi.
3. **Sessiz form yasağı**: cari seçilmeden kaydet → `#sip-f-cari-hata`; kalemsiz → `#sip-f-kalem-hata` / `#sipd-d-kalem-hata`; gerekçe kısa → `#sipd-iptal-gerekce-hata`. İstek gönderilmez (test kanıtlı).
4. **Yıkıcı eylem onayı + geri dönüş yolu**: IPTAL ortak onay modalı + gerekçe. **Geri dönüş yolu:** IPTAL terminaldir (bilinçli); `DegismezIpucu` doğru yolu yazar — "gerekçeli iptal edin ve yenisini açın". Aktife alma YOKTUR çünkü iptal edilmiş söz geri alınmaz, yenisi verilir.

**MÜHÜR 2 — Dokunulan sayfalar (tek dokunuş kaydı):**
- `Kuyumcu.Web/Pages/Siparisler.razor` (YENİ), `Kuyumcu.Web/Pages/SiparisDetay.razor` (YENİ)
- `Kuyumcu.Web/Layout/KenarCubugu.razor` (tek NavLink bloğu — menü kaydı zorunlu)
Storno taklidi ("ters iade") YAPILMADI; sipariş zaten deftere yazmaz. 9 Eylül sızıntı satırlarına dokunulmadı.

---

## 10. Canlı doğrulama (localhost:5198 — 5199'a DOKUNULMADI)

Test kiracısı: **`PAKET3-TEST Kuyumcu`** (`01M2NSG8WQH4RDQT66RV3EJS1K`). Başlangıç: `tenants=7`, `hareketler=122`, `kasa_hareketleri=64`.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | Kayıt + cari + kur | 201 / 201 / 200 | aynı | `/kur/guncel` ilk 12 sn 404 döndü, beklendi → `id=01M2NSH4XY…` |
| 2 | Müşteri siparişi oluştur | 200, `durum=TASLAK`, `no=S-2026-000001` | aynı | yanıt gövdesi okundu (`-o /dev/null` kullanılmadı) |
| 3 | Geçiş dökümü | 1 satır, `NULL->TASLAK` | `1  NULL->TASLAK` | `SELECT COUNT(*) … FROM siparis_gecisleri` |
| 4 | `TASLAK → TESLIM_EDILDI` | **400** | 400 `'TASLAK' durumundan 'TESLIM_EDILDI' durumuna geçilemez (MUSTERI_SIPARISI)` | |
| 5 | Zincir ONAYLANDI→HAZIR→TESLIM_EDILDI | 3×200 | 200/200/200, durumlar sırayla | |
| 6 | Terminalden `TASLAK`'a dön | **400** | 400 | |
| 7 | Terminalde PATCH | **400** | 400 `Yalnız TASLAK durumundaki sipariş düzenlenebilir (bu sipariş: TESLIM_EDILDI)` | |
| 8 | Geçiş dökümü (4 adım) | 4 satır, sıralı | `4  NULL->TASLAK,TASLAK->ONAYLANDI,ONAYLANDI->HAZIR,HAZIR->TESLIM_EDILDI` | SQL |
| 9 | **Defter tekeli** | `hareketler`/`kasa_hareketleri` DEĞİŞMEZ | 122 / 64 → **122 / 64** | SQL önce/sonra |
| 10 | Gecikmiş sipariş (söz verilen gün = dün) | `geciken=true`, süzgeçte görünür | `gecikenSayisi=1`, `toplam=1`, satır `S-2026-000002 ATOLYE_SIPARISI TASLAK` | `GET /siparisler?geciken=true` |
| 11 | Gerekçesiz IPTAL | **400** + durum ve log DEĞİŞMEZ | 400; SQL `TASLAK  1` | hata enjeksiyonu ölçümü |
| 12 | Gerekçeli IPTAL | 200, gerekçe loga yazılır | `durum=IPTAL`; SQL `TASLAK IPTAL  "Atolye siparisi musteri talebiyle iptal edildi"` | |
| 13 | EMANET_GIRIS (mevcut uç) | 200, has bakiye **yalnız buradan** değişir | `hasGrami=4.58`; `cari_bakiyeler = 0.00 / -4.580000` | |
| 14 | TAMIR siparişi + emanet bağı | 200, `emanetHareketId` dolu | `no=S-2026-000003`, `emanetHareketId=01M2NSJ13S…`; has bakiye **değişmedi** (0.00 / -4.58), `hareketler=123` (yalnız emanet girişi) | |
| 15 | Yabancı/olmayan kimlik | **404** | 404 `Sipariş bulunamadı` | |
| 16 | TEZGAHTAR sipariş oluşturur | **200** | 200 | |
| 17 | TEZGAHTAR iptal eder | **403** | 403 `Bu işlem için gerekli rol: SAHIP veya MUDUR` | |
| 18 | TEZGAHTAR geçiş dökümü | **403** | 403 `… SAHIP veya MUDUR veya MUHASEBE` | |
| 19 | Yetki matrisinden `siparis` kaldır → TEZGAHTAR | **403** + menüde yok | 403 `Bu ekran için yetkiniz yok (siparis)`; `/yetkiler/benim` → `["banka","cari","fatura","islem","kasa","urun"]` | menü kaybı sunucu kaynaklıdır |
| 20 | SAHIP `?limit=1000` | `limit=200` | `limit=200, azamiLimit=200, toplam=4` | |
| 21 | İyimser kilit — **GÜNCEL** damga | **200** | ilk denemede **409 (HATA)** → düzeltildi → **200** | §12 |
| 22 | İyimser kilit — **ESKİ** damga | **409** | 409 `Bu sipariş siz görüntüledikten sonra değişti.` | |
| 23 | PATCH sözleşmesi | gönderilmeyen korunur / null siler | `sozVerilenGun=2026-10-15, aciklama=null` → sonra `sozVerilenGun=null` | |

**Temizlik:** API PID'leri (71358, 72485) `kill` ile durduruldu, `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**. `information_schema`'dan `tenant_id` kolonu olan **40 tablodan** test kiracısı silindi.

```
tenants            = 7    (başlangıçla aynı)
siparisler         = 0
siparis_kalemleri  = 0
siparis_gecisleri  = 0
hareketler         = 122  (başlangıçla aynı)
kasa_hareketleri   = 64   (başlangıçla aynı)
```
`QA-TEST-1` ve `QA-TEST-2` kiracılarına **DOKUNULMADI**.

---

## 11. Canlıda bulunan gerçek hata (gizlenmedi)

**İyimser kilit her geçişi 409 ile reddediyordu.** `IyimserKilitDogrula` gelen damgada koşulsuz `ToUniversalTime()` çağırıyordu. MySQL'den okunan `guncellendi` `Kind=Unspecified` döner ve JSON'a **Z'siz** (`"2026-09-16T19:02:23.554933"`) yazılır; istemci onu aynen geri gönderince .NET değeri **yerel** sanıp +3 saat kaydırıyor ve GÜNCEL damga bile eskimiş görünüyordu. Kilit, "iki kullanıcıyı ayırt eden" bir kilitten "herkesi durduran" bir kilide dönüşmüştü.

- **Neden birim testlerden geçti:** yalnız "eskimiş damga 409 alır" ölçülmüştü; "güncel damga 200 alır" ölçülmemişti. Test açığı.
- **Düzeltme:** yalnız `Kind == Local` (açık ofsetli) damga UTC'ye çevrilir; `Utc` ve `Unspecified` zaten UTC'dir.
- **Kilitleme:** `Guncel_damga_KABUL_edilir_Zli_ve_Zsiz_bicimde` testi + mutasyon **M-KILIT-1** (eski davranış geri konunca test kırmızı).
- **Canlıda yeniden doğrulandı:** güncel damga → 200, eski damga → 409 (tablo satır 21-22).

---

## 12. Değişen dosyalar (bu pakete ait; `git status --porcelain | grep -v -E "/(bin|obj)/"` süzülmüş)

**YENİ**
```
dotnet/Kuyumcu.Api/Data/Siparis.cs
dotnet/Kuyumcu.Api/Modules/Siparis/SiparisDurumMakinesi.cs
dotnet/Kuyumcu.Api/Modules/Siparis/SiparisNo.cs
dotnet/Kuyumcu.Api/Modules/Siparis/SiparisModulu.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260916184850_SiparisCatisi.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260916184850_SiparisCatisi.Designer.cs
dotnet/Kuyumcu.Web/Pages/Siparisler.razor
dotnet/Kuyumcu.Web/Pages/SiparisDetay.razor
dotnet/Kuyumcu.Tests/SiparisDurumMakinesiTestleri.cs
dotnet/Kuyumcu.Tests/SiparisTestleri.cs
dotnet/Kuyumcu.Tests/HttpSiparisTestleri.cs
dotnet/Kuyumcu.Tests/SiparisEkranTestleri.cs
dokumanlar/09-uygulama-kayitlari/PAKET-3-uygulama-raporu.md
```
**DEĞİŞTİRİLEN**
```
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
dotnet/Kuyumcu.Api/Modules/ModulKayit.cs
dotnet/Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs
dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs
dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor
dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs
dotnet/denetim.sh
dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs          (rota sayısı 28 → 30)
dotnet/Kuyumcu.Tests/BildirimHttpTestleri.cs        (fikstür kök-neden düzeltmesi)
```
**COMMIT YAPILMADI** (kurucunun kırmızı çizgisi). `dokumanlar/02-mimari/**` ve `SONRAKI-OTURUM-PROMPT.md` **açılmadı bile**.

---

## 13. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Önkoşul 4-A doğrulandı, yoksa DUR | ✅ | §0 |
| Tohumlama: paket 6-A mekanizması **yeniden yazılmadan** kullanıldı; mevcut kiracılarda sessiz 403 yok, HTTP testiyle kanıtlı | ✅ | `Eski_markerli_kiracida_MUDUR_siparis_ucunu_gorur_sessiz_403_yok`; canlı adım 19 |
| APPEND-ONLY `siparis_gecisleri` (Modified/Deleted → istisna) + test | ✅ | `KuyumcuDbContext.GecisIziniKoru`; `Gecis_izi_APPEND_ONLYdir_guncelleme_ve_silme_istisna_atar` |
| IPTAL yalnız SAHIP/MUDUR; TEZGAHTAR → 403 | ✅ | HTTP testi + canlı 17 |
| PATCH yalnız TASLAK | ✅ | DB + HTTP testleri + canlı 7 |
| Yabancı kiracı 404 | ✅ | `Baska_kiracinin_siparisi_404_doner`, `Yabanci_kiracinin_siparisi_404_doner`, canlı 15 |
| Liste ≤ 200 | ✅ | `Limit_1000_istense_de_en_cok_200_satir_doner`, canlı 20, M151-1 |
| Fiyat sunucudan | ✅ | `Fiyat_ekranda_hesaplanmaz…`, M152-1 |
| Sipariş no: `(tenant_id,no)` UNIQUE + **sınırlı yeniden deneme**, tükenince 409; iki paralel oluşturma testi | ✅ | `SiparisNo.AzamiDeneme=5`; `Iki_paralel_olusturma_ayni_numarayi_alamaz` |
| İyimser kilit (guncellendi damgası) geçişte | ✅ | `Iyimser_kilit_eskimis_damgayi_409…` + `Guncel_damga_KABUL_edilir…`; canlı 21-22 |
| `(tenant_id, durum, soz_verilen_gun)` indeksi | ✅ | `KuyumcuDbContext`, migration `CreateIndex` |
| Katman 1 sızıntısı yok (fire/usta/ayar düşürme alanı yok) | ✅ | `Siparis.cs` kalem alanları |
| Durum listesi kapalı 6 durum | ✅ | `Durum_listesi_KAPALI_alti_durumdur` |
| TAMIR'de EMANET_GIRIS opsiyonel + ekranda "defterde görünmez" uyarısı | ✅ | `Tamir_siparisine_emanet_hareketi_opsiyonel_baglanir…`; `#sip-f-emanet-uyarisi` / `#sipd-emanet-uyarisi` |
| Faz B hareketId bağı bu fazda YOK | ✅ | §2/1 |
| M146/M147 hata enjeksiyonu yöntemi ve saat soyutlaması raporda | ✅ | §7 |
| UX-S21…S33 | ✅ (kısmi not) | §14 |
| MÜHÜR 2: finansal hard DELETE yok; yeni uçlarda ModulYetkisi + RolGerekli + denetim; DoD 4 kural; tek dokunuş | ✅ | §9; `denetim.sh` "Finansal varlıkta [HttpDelete] = 0" |
| Mimari belgeye / paket spesifikasyonuna / SONRAKI-OTURUM-PROMPT'a dokunulmadı | ✅ | §12 |
| COMMIT yok | ✅ | çalışma ağacında |

### §14 — UX sözleşmesi karşılama notları
- **S21** şube oturumdan, fiyat sunucudan, ayar/gram ürün seçilince türetilir, `geciken` sunucudan — rozetlerle kaynak söylenir.
- **S22** tek şubede seçici çizilmez (`Tek_subede_sube_secici_cizilmez…`).
- **S23** liste özet kartı durum kırılımı + gecikmiş sayısını gösterir. **Eksik:** sayılar henüz süzgeçli listeye **bağlantı** değil (§15).
- **S24** ürün seçilince ayar/gram dolar; tahmini tutar girilince kur zorunlu olur.
- **S25** TASLAK düzenlenir; terminalde `DegismezIpucu` + alternatif yol.
- **S26** düzenleme formu oluşturmayla aynı alanları taşır; tür listesi tek diziden (`FormTurleri`).
- **S27** POST/PATCH/geçiş yanıtları okunur; sipariş no ve yeni durum banda yazılır; IPTAL önce onay ister.
- **S28** `catch` içinde liste BOŞALTILMAZ, hata bandı ayrıdır.
- **S29** `dahaFazlasiVar` → `#sip-sinir-uyarisi`, azami 200 yazılı.
- **S30** kilitli düğmelerin sebebi yazılı.
- **S31** ham kod ekrana basılmaz (`Ham_durum_kodu_ekrana_basilmaz_sozlukten_cevrilir`); sözlükte eksik anahtar `required` ile derleme hatası.
- **S32** tek para birimi TL, etiket "(TRY)" (§2/8).
- **S33** bu ekranda dönem seçici YOK → uygulanmaz.

---

## 15. Açık kalanlar

1. **Faz B**: `TESLIM_EDILDI` için `hareketId` (aynı kiracı **ve aynı cari**), kapora referansı, `/siparisler/ozet`, `BelgeModulu` SIPARIS dalı + `BelgeDeposuTestleri.cs:137` davranış değişikliği (test **silinmeyecek**, güncellenecek), `siparis_gecisleri.hareket_id` kolonu için ikinci migration. **M150 mutasyonu Faz B'de koşulmalı.**
2. **UX-S23 tam karşılanmadı**: özet kartındaki sayılar (durum kırılımı, gecikmiş adet) henüz tıklanabilir bağlantı değil. Küçük bir dalga işi.
3. **Append-only sınırı**: `ExecuteUpdate/ExecuteDelete` ve doğrudan SQL, `SaveChanges`'ı atlar → koruma onları görmez (denetim izindeki sınırın aynısı; `denetim.sh` beyaz listeyle sayıyor).
4. **`guncellendi` alanının JSON'da tutarsız Kind'i**: oluşturma yanıtında `…Z`, sonraki GET'lerde Z'siz dönüyor (izlenen varlık vs. veritabanından okuma). Kilit artık ikisini de kabul ediyor ama **API yüzeyinde tarih Kind'inin tek biçime oturtulması** ayrı bir iş kalemi — bu paket dışıdır ve tüm modülleri ilgilendirir.
5. **Sipariş makbuzu / tamir belgesi** paket 5'e bırakıldı (spesifikasyona uygun).
6. Şube bazlı numaralandırma talebi **doğrulanmadı**; kiracı bazlı kaldı.

---

## 16. Kurucuya / mali müşavire notlar

1. **6502 sayılı TKHK ve Satış Sonrası Hizmetler Yönetmeliği — tamir/servis belgesi yükümlülüğü (`DOGRULANMALI`)**: perakende kuyumcunun tamire aldığı müşteri eşyası için belge verme yükümlülüğünün kapsamı (yalnız yetkili servis mi, her tamir mi) **doğrulanmadı**. Bu yüzden **koda hiçbir zorunluluk yazılmadı**; tamir makbuzu paket 5'e bırakıldı ve o paket geldiğinde **yapılandırmadan okunan, varsayılan KAPALI** bir özellik olarak tasarlanacaktır (ADR-0005). **Mali müşavir/hukuk görüşü gerekir.**
2. **Tamirde emanet girişi opsiyonel**: küçük tamirlerde (zincir lehimi) sürtünme doğmasın diye zorunlu yapılmadı; ekran her seferinde "müşteri eşyası **defterde görünmez**" uyarısı basıyor. Zorunlu olsun mu — **ekip oyuyla karar bekliyor**, karara kadar opsiyonel.
3. **İptal geri alınamaz**: iptal edilen sipariş yeniden aktifleştirilmez; yeni sipariş açılır. Gerekçe ve iz kalıcıdır.
4. **Sipariş para/stok/kâr-zarar doğurmaz** — kapora ve teslim hâlâ mevcut tahsilat/satış uçlarından geçer; tevsik ve MASAK kapıları oralarda çalışmaya devam eder. Sipariş kimliği MASAK kümülatif ölçümüne **bilinçli olarak sokulmadı**.
