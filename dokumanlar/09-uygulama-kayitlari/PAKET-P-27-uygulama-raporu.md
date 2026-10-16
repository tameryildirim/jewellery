# PAKET P-27 — F-1 ABONELİK + F-3 PLAN KAPISI (KOŞULSUZ 1.0)

**Kimlik:** R-1 · **Dalga:** 7 · **Tarih:** 2026-09-22 · **Rol:** Taşkın (backend) + Ertuğ (frontend)
**Durum:** BİTTİ — derleme 0 hata / 0 uyarı · `dotnet test` **3895/3895 yeşil, 0 başarısız** ·
`dotnet/denetim.sh` **DENETİM TEMİZ** · canlı doğrulama 5198'de 11 adım · test kiracıları silindi
(`tenants = 9`, başlangıçtaki değer) · **5199'a DOKUNULMADI** · **COMMIT YOK**.

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (varsayım değil, diskte bakıldı)

| Önkoşul | Diskte var mı | Ne bulundu |
|---|---|---|
| **P-09** (K3 dilimi) | **VAR** | `PAKET-P-09-uygulama-raporu.md` · `AdminModulu.PlanGuncelle` servis içi plan doğrulaması + `plan-degisti` denetim izi ile duruyordu; `denetim.sh` içinde `P-09: YETKISIZ_PLAN_YAZAN_UC` sayacı kurulu |
| **P-23** | **VAR** | `PAKET-P-23-uygulama-raporu.md` + `denetim.sh` `# >>> P-23` bloğu |
| **F-2-min** (bu paketin dayandığı kapı) | **VAR** | `Modules/Sistem/KiraciDurumKapisi.cs` (global MVC yazma kapısı) · `SistemAnahtariKapisi` · `tenants.durum/deneme_bitis` |

Kapsam daraltması gerekmedi: iki önkoşul da tamdır.

---

## § 1 — PLAN BLOĞU İLE ÇELİŞKİLER VE SAPMALAR (Anayasa md.1 — gizlenmiyor)

| # | Plan ne diyor | Ne yapıldı | Gerekçe |
|---|---|---|---|
| S1 | **Dosyalar:** `Modules/Admin/AdminModulu.cs` · `Modules/Abonelik/` · `Data/Migrations/` · `Kuyumcu.Tests/` | Bunlara **ek olarak** `Data/KuyumcuDbContext.cs`, `Modules/Sistem/KiraciDurumKapisi.cs`, `Modules/Meta/DuzeltmePolitikasi.cs`, `Modules/ModulKayit.cs`, `Kuyumcu.Core/I18n/Sozlukler.cs`, `Kuyumcu.Contracts/Sayfalar/IsletmeSozlesmeleri.cs`, `Kuyumcu.Web/Pages/Isletme.razor`, `dotnet/denetim.sh` değişti | Plan "Migration: EVET — abonelik ve plan tabloları" diyor; EF'te tablo **DbSet + OnModelCreating indeksi olmadan doğmaz** → DbContext zorunlu. DoD "fail-closed etkiler" diyor → yazma kapısı `KiraciDurumKapisi`'dır. DoD "sayaçlar" diyor → `denetim.sh`. DoD "ekranda düğme" (Katman 8) → Isletme.razor + Sozlukler + Contracts. **`Data/Entities.cs`'e DOKUNULMADI** (TEK EL zinciri P-05→P-34): varlıklar plan dosya listesindeki `Modules/Abonelik/` altında tanımlandı. |
| S2 | — | `AdminServisi.PlanGuncelle` **KALDIRILDI**; rota `PATCH /api/v1/admin/plan` **korundu** ve `AbonelikServisi`'ne devretti | `tenants.plan`'a yazan iki yol olamaz (sayaç `YETKISIZ_PLAN_YAZAN_UC`). İstemci sözleşmesi kırılmadı. |
| S3 | — | **Mevcut iki P-09 testi P-27 sözleşmesine uyarlandı** (zayıflatılmadı): `K3_Plan_degisimi_denetim_izine_SATIR_yazar` artık PRO→**BASLANGIC** (indirme) gönderir; `K3_Servis_uydurma_plani_REDDEDER_DTO_ya_guvenmez` `AbonelikServisi` üzerinden koşar | Eski testler PRO→KURUMSAL **yükseltmesinin 200 dönmesini** bekliyordu; P-27 DoD'u bunu **403** yapar. Kural değişimi açıkça kayıtlıdır. |
| S4 | — | `denetim.sh` `BEKLENEN_VARLIK` **kapsamı genişletildi** (Data/ → Kuyumcu.Api geneli), taban 68 → **69** | Sayaç yalnız `Data/` altını tarıyordu; P-27 varlığı `Modules/Abonelik/` altında doğunca sayaca **hiç görünmedi**. "Data dışına koy, sayaçtan kaç" deliği kapatıldı (tavan artırılmadı, delik kapatıldı). `OlcumKapisiTestleri` ve `ham/olcum-2026-09-18.json` aynı genişlemeyle hizalandı. |
| S5 | DoD EK md.4 "yıkıcı eylem → ortak onay modalı" | Plan indirme onayı **yerinde (inline)** alınır, `ModalPencere` **kullanılmadı** | DoD EK md.2 "ModalPencere yalnız yıkıcı onay" + `denetim.sh` `Pages: ModalPencere kullanımı` tavanı (55). Plan indirme **hiçbir kaydı silmez/iptal etmez**; hak kapsamını daraltır. Sessiz yazma yine yasaktır: seçim tek başına PATCH atmaz, kullanıcı açıkça onaylar (bUnit testi: `Plan_degisikligi_ONAYDAN_SONRA_PATCH_ile_gonderilir`). |
| S6 | "abonelik ve plan **tabloları**" | İki tablo: `abonelik_planlari` (kiracı-üstü katalog) + `kiraci_abonelikleri` (IKiraciVarlik) | Katalog kiracı-üstüdür; `KuyumcuDbContext` kapsam denetiminde **gerekçeli beyaz liste** maddesi var (KiraciDaveti/YedekKaydi kalıbının aynısı, yeni desen açılmadı). |
| S7 | Risk: "abonelik tabloları FK borcunu yeniden üretmemeli" | **FK atılmadı**; şema dilimi P-34'ten önce bitirildi, indeksler kuruldu | Depoda `HasOne/WithMany` = 0; FK grubu P-34'te toplu gelir ve bu iki tabloyu da kapsayabilir. |

**Mevzuat/fiyat sabiti yazılmadı (K-16):** katalogda **tutar kolonu yoktur**, hiçbir uç para tahsil
etmez. `denetim.sh` `P-27: ABONELIK_FIYAT_KOLONU = 0` bunu cırcırla dondurur.
→ **Kurucuya not:** rakamlı fiyat ilanı ve ücretli sözleşme hâlâ K-16'dadır; bu paket yalnız
"kim hangi hakka sahip, kim değiştirdi, ne zaman bitiyor" sorusunu kapatır.

---

## § 2 — YAPILANLAR

### 2.1 Veri modeli (`Modules/Abonelik/AbonelikVarliklari.cs` — YENİ)
* `AbonelikPlani` (`abonelik_planlari`): `kod` (PK) · `ad` · `aciklama` · `seviye` · `kullanici_tavan` ·
  `sube_tavan` · `aktif` · zaman damgaları. **Kiracı-üstü** (beyaz liste, gerekçeli).
  Tavanlar NULL: ürün davranışına bağlanmadı → **uygulanmayan sınır vaat edilmedi** (devredildi: P-28 ölçümü).
* `KiraciAbonelik` (`kiraci_abonelikleri`): `IKiraciVarlik` · `plan_kodu` · `baslangic` · `bitis` ·
  `durum` (AKTIF/SONLANDI) · `kaynak` (SISTEM/KIRACI) · `gerekce` · `operator_adi`.
  **Append-only tarihçe:** her değişim yeni satır açar, eskisi SONLANDI olur (silinmez).

### 2.2 Kapılar (`Modules/Abonelik/AbonelikServisi.cs` — YENİ)
* **Tek yazma çekirdeği** `DonemDegistir(...)`: eski dönemi kapatır, yenisini açar, `tenants.plan`
  yansımasını günceller. `tenants.plan`'a **başka hiçbir yerde** atama yoktur.
* **Kiracı yolu** `KiraciPlanDegistir`: yalnız **İNDİRME**. Yükseltme → `403` + **REDDEDILDI**
  denetim kaydı (`PLAN_YUKSELTME_RED`). Mevcut plan katalogda yoksa **fail-closed** (reddeder).
  Plan kodu **trim edilmez** (P-09 K3 dersi: `"KURUMSAL "` kabul edilmez).
* **Sistem yolu** `AbonelikSistemServisi.SistemPlanDegistir`: tek **YÜKSELTME** yolu; sistem anahtarı +
  operatör (≥3) + gerekçe (≥20) + ileri tarihli bitiş. Denetim izi `PLAN_DEGISIMI` damgalı.
  ⚠ **Ayrı sınıf, ölçülmüş zorunluluk:** `/api/v1/sistem/*` anonimdir, istek kapsamında kiracı
  bağlamı yoktur; `KuyumcuDbContext` enjekte eden servis bu uçta **500** verdi (koşumda gerçekten
  verdi) → `SistemServisi` kalıbına geçildi (`SistemKapsami` içinden db).

### 2.3 Fail-closed (`Modules/Sistem/KiraciDurumKapisi.cs`)
`KiraciDurumu` kayıt yapısına **sona, varsayılanlı** `AbonelikBitis` eklendi (mevcut iki argümanlı
çağrılar değişmedi). `YazabilirMi = TemelYazabilirMi && !AbonelikSuresiDoldu`. Süresi dolan kiracı
**yazamaz**, ama **okur, görüntüler, dışa aktarır** (veri rehin alınmaz). Red mesajı abonelik
diliyle yazılır — "deneme süreniz doldu" demek ücretli kiracı için yalan olurdu (UX-S31 · md.1).

### 2.4 Uçlar
| Uç | Kapı | Ne yapar |
|---|---|---|
| `GET /api/v1/abonelik` | JWT + `[RolGerekli("SAHIP")]` | dönem + katalog + tarihçe (50 sınırı bildirilir) |
| `PATCH /api/v1/abonelik/plan` | JWT + SAHIP | **indirme**; yükseltme 403 |
| `PATCH /api/v1/admin/plan` (mevcut rota) | JWT + SAHIP | aynı çekirdeğe devreder (sözleşme korundu) |
| `POST /api/v1/sistem/kiracilar/{id}/abonelik` | `SistemAnahtariKapisi` (JWT geçmez) | tek yükseltme / dönem uzatma |

### 2.5 Ekran (`Kuyumcu.Web/Pages/Isletme.razor`) — Katman 8
Plan artık "seç-kaydet tercihi" değil **abonelik kartıdır**: plan + kaynak rozeti, başlangıç, bitiş
(`Süresiz`), kalan gün (`dönemden bağımsız` alt metniyle — UX-S33), dönem geçmişi + **sınır notu**
(UX-S29), süre dolduysa hata bandı. Seçici **yalnız sunucunun indirilebilir dediği** planları taşır
(UX-S21/S26); indirilecek plan yoksa seçici **çizilmez**, yerine `DegismezIpucu` + gerekçe durur
(UX-S22/S25/S30). Hata "kayıt yok"a çevrilmez (UX-S28). Kaydetme sonrası abonelik yeniden okunur ve
başarı bandı basılır (UX-S23/S27). Sözlük TR+EN (20 yeni anahtar), çıplak metin yok.

### 2.6 DoD SÖZLEŞMESİ (Bertan koşulu — hangi varlık nasıl düzeltilir)

| Varlık | Düzenleme yolu | PATCH/DELETE | Yetki | Denetim | Ekranda |
|---|---|---|---|---|---|
| `KiraciAbonelik` | **Yeni dönem açmak** (kayıt yerinde düzeltilmez) | İndirme: `PATCH /abonelik/plan` · Yükseltme/uzatma: `POST /sistem/kiracilar/{id}/abonelik`. **Hard DELETE YOK** — eski dönem `SONLANDI` olur | SAHIP (indirme) · sistem anahtarı (yükseltme) | `DenetimServisi.YazmaKaydet` / `SistemYazmaKaydet` **çağrılır** (yeniden yazılmadı) | Seçici + onay; yol yoksa `DegismezIpucu` |
| `AbonelikPlani` | **Salt okunur** (kiracı-üstü katalog) | yok | operatör | — | Katalog listelenir; değişmezlik gerekçesi ekranda |

`DuzeltmePolitikasi` haritasına ikisi de gerekçeli `SALT_OKUNUR` damgasıyla eklendi
(`DuzeltmePolitikasiTestleri` yeni varlığı damgasız bırakmaya izin vermez).

---

## § 3 — MIGRATION

* **Ad:** `20260922031115_AbonelikVePlanKapisi`
* **Boyut:** `.cs` **7 345 bayt** · `.Designer.cs` **230 530 bayt** (ikisi de > 0, `Up()` okundu)
* **İçerik:** iki tablo + 4 indeks + **katalog tohumu** (BASLANGIC/PRO/KURUMSAL, `ON DUPLICATE KEY`)
  + **geri doldurma** (her kiracıya `tenants.plan`'dan **süresiz** açılış dönemi; yeniden koşmaya
  dayanıklı; planı katalogda olmayan kiracıya satır **üretilmez** — uydurulmaz).
* **Geri doldurma öncesi salt-okunur etki ölçümü** (plan § 1 md.10): `tenants = 9`, hepsinin planı
  katalogda (BASLANGIC 8 · PRO 1), katalog dışı **0** → beklenen 9 satır.
* **Uygulandı:** `dotnet ef database update --connection … kuyumcu_net` → `Applying migration … Done.`
* **Kanıt:** `__EFMigrationsHistory` **66 → 67** · `abonelik_planlari` 3 satır (seviye 10/20/30, hepsi aktif)
  · `kiraci_abonelikleri` **9 satır, 9'u AKTIF, 9'u süresiz** → mevcut kiracıların yazması kesilmedi.
* `--no-build` **kullanılmadı**; build yeşilken üretildi.

---

## § 4 — TESTLER

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Önce (paket öncesi taban, hesaplanan) | 3872 | 3872 | 0 | 0 |
| P-27 + P-09 filtreli | 51 → 52 | hepsi | 0 | 0 |
| İşletme ekranı (bUnit) | 45 | 45 | 0 | 0 |
| **Sonra — TAM SÜİT** | **3895** | **3895** | **0** | **0** |

**Yeni test sayısı: 23** — `Kuyumcu.Tests/P27AbonelikTestleri.cs` (20) + `IsletmeEkranTestleri.cs` (3).
Uyarlanan (zayıflatılmayan) testler: `P09KimlikErisimTestleri` (2), `DenemeBandiEkranTestleri` (1),
`OlcumKapisiTestleri` (kapsam), `SozlesmeYansimaTestleri` (3 yeni kilitli çift + iç içe kayıt
normalleştirmesi).

---

## § 5 — MUTASYON KANITLARI

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| **M27-1** | `if (mevcut is null \|\| yeni.Seviye > mevcut.Seviye)` → `if (false)` (yükseltme kapısı) | `Yukseltme_kiraci_tarafindan_REDDEDILIR_403` · `Yukseltme_reddi_denetim_izine_yazilir` · `Yukseltme_abonelik_ucunden_de_REDDEDILIR` · `Katalogda_olmayan_plandan_degisim_FAIL_CLOSED_reddedilir` | `cmp` **AYNI** |
| **M27-2** | Kiracı yolundaki `denetim.YazmaKaydet(...)` satırı silindi | `Iz_indirmede_yazilir` · `K3_Plan_degisimi_denetim_izine_SATIR_yazar` | `cmp` **AYNI** |
| **M27-3** | `AbonelikSuresiDoldu` → `false` (fail-closed) | `Durum_abonelik_bitisi_gecmisse_YAZAMAZ` · `FailClosed_suresi_dolan_kiraci_YAZAMAZ` · `FailClosed_operator_donemi_uzatinca_yazma_GERI_ACILIR` | `cmp` **AYNI** |
| **M27-4** | `gerekce.Length < 20` → `< 0` (sistem yolu gerekçe kapısı) | **HİÇBİRİ — TEST AÇIĞI** (istek DTO'da 400 alıyordu, servis kapısı sınanmıyordu) | `cmp` **AYNI** |
| **M27-4b** | Aynı mutasyon, **test güçlendirildikten sonra** | `Sistem_SERVISI_gerekcesiz_ve_operatorsuz_istegi_REDDEDER` (yeni test: servisi DI'dan çözüp doğrudan çağırır) | `cmp` **AYNI** |
| **M27-5** | `AdminServisi.AiToggle` içine ikinci bir `tenant.Plan = "KURUMSAL";` yazma yolu eklendi | `denetim.sh` → `P-27: YETKISIZ_PLAN_YAZAN_UC 1 ≠ 0` **ve** `P-09: YETKISIZ_PLAN_YAZAN_UC 1 ≠ 0` → **DENETİM BAŞARISIZ** | `cmp` **AYNI** |

Son mutasyondan sonra: `dotnet build Kuyumcu.sln` → **0 hata / 0 uyarı**.
Yedekler: `…/scratchpad/mut-yedek/P-27/` (proje ağacına artık bırakılmadı).

---

## § 6 — DENETİM (`dotnet/denetim.sh`)

**Sonuç: `DENETİM TEMİZ.`**

Eklenen/ güncellenen cırcır sayaçları (`# >>> P-27` … `# <<< P-27`), tabanları **taze ölçüldü**,
ölçüm komutu betiğe yorum olarak yazıldı:

| Sayaç | Değer | Ölçüm |
|---|---|---|
| `P-27: YETKISIZ_PLAN_YAZAN_UC` | **0** | `AbonelikServisi.cs` dışındaki `X.Plan =` atamaları (yorumlar sökülerek) |
| `P-27: ADMIN_SERVIS_PLAN_YAZAR` | **0** | `AdminModulu.cs`'de yalnız controller devri (servis metodu geri gelemez) |
| `P-27: PLAN_DEGISIMI_IZSIZ` | **0** | `DonemDegistir` çağrı sahası 2 ↔ `IzDamgasi` 2 |
| `P-27: ABONELIK_FIYAT_KOLONU` | **0** | katalogda `ucret/fiyat/tutar` kolonu yok (K-16) |
| `P-09: YETKISIZ_PLAN_YAZAN_UC` (güncellendi) | **0** | hedef dosya `AbonelikServisi.cs`; kapı şartları **sertleşti**: katalog doğrulaması + seviye kıyası + 403 + iki iz damgası + iki `[RolGerekli("SAHIP")]` |
| `BEKLENEN_VARLIK` | **68 → 69** | kapsam Data/ → Kuyumcu.Api geneli (delik kapatıldı); Entities 25 + Data/diğer 43 + Modules 1 |

Hiçbir tavan **artırılmadı**: `ModalPencere` (55), `ölü düğme` (71), `sınırsız ToListAsync` (145) ve
`SAYFA_RECORD_TAVAN` (3) **aynı kaldı** — kod bu tavanlara uyacak biçimde yazıldı
(inline onay · `@onclick` aynı satırda · katalog sorgusuna `Take(PlanTavani)` · istemci tipleri `Kuyumcu.Contracts`'ta).

---

## § 7 — CANLI DOĞRULAMA (localhost:**5198** — 5199'a DOKUNULMADI)

Test kiracıları: **`PAKET27-TEST`** ve **`PAKET27-TEST-2`** (sistem daveti + `POST /auth/kayit`).
Sistem anahtarı yalnız **süreç ortam değişkeninde** verildi; dosya değişmedi, değer çıktıya yazılmadı.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 0 | `GET /saglik` | Healthy | **`Healthy`** | yanıt okundu |
| 1 | `POST /sistem/davetler` (gerekçe ≥20) | 201 + jeton | **201** | jeton dosyaya alındı, basılmadı |
| 2 | `POST /auth/kayit` | 201 | **201**, `tenantId` döndü | — |
| 3 | `GET /abonelik` | 200 · plan BASLANGIC · indirilebilir **boş** | **200** `planKodu=BASLANGIC seviye=10 indirilebilirPlanlar=[] yukseltmeKiraciyaKapali=true` | gövde okundu |
| 4 | `PATCH /abonelik/plan` `{KURUMSAL}` | **403** | **403** `"Plan yükseltme işletme hesabından yapılamaz…"` | F-3 kapısı canlı |
| 5 | `POST /sistem/kiracilar/{id}/abonelik` `{KURUMSAL, bitis=+30g}` | 200 | **200** `eskiPlan=BASLANGIC plan=KURUMSAL kalanGun=30` | tek yükseltme yolu |
| 6 | `PATCH /abonelik/plan` `{PRO}` (indirme) | 200 | **200** `donem.kaynak=KIRACI` | — |
| 7 | `PATCH /admin/plan` `{BASLANGIC}` (eski rota) | 200 | **200** `plan=BASLANGIC` | sözleşme korundu |
| — | **SQL (kiracı 1)** | 3 dönem · 1 red + 3 iz | `KURUMSAL/SONLANDI/SISTEM` · `PRO/SONLANDI/KIRACI` · `BASLANGIC/AKTIF/KIRACI` · iz: `REDDEDILDI PLAN_YUKSELTME_RED…` + 3 × `PLAN_DEGISIMI…` · `tenants.plan=BASLANGIC` | `docker exec … mysql` |
| 8 | Kiracı 2'ye **süresi dolmuş** dönem (SQL) → `POST /cari` | **403** abonelik dilinde | **403** `"Abonelik döneminiz sona erdi…"` | fail-closed canlı |
| 9 | Kiracı 2 · `GET /cari`, `GET /abonelik` | 200 | **200 / 200** | veri rehin alınmadı |
| 10 | `POST /sistem/…/abonelik` `{PRO, bitis=+15g}` → `POST /cari` | 200 → 201 | **200 → 201** | önbellek boşaltıldı, yenileme gecikmesiz |
| 11 | `POST /sistem/…/abonelik` `{gerekce:"kisa"}` · aynı uç **JWT** ile | 400 · 403 | **400** `"Gerekçe en az 20 karakter olmalıdır"` · **403** | iki kapı da canlı |

**Temizlik:** API PID `kill` → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **0 satır** (5199 dinlemeye devam —
kurucunun sunucusuna dokunulmadı). İki test kiracısı `tenant_id` taşıyan **68 tablodan** +
`kiraci_davetleri`'nden + `tenants`'tan silindi.
Son sayımlar: **`tenants = 9`** (başlangıçtaki değer) · `kiraci_abonelikleri = 9` (yalnız göç
satırları) · `PAKET27%` davet **0** · **`QA-TEST-` önekli 4 kiracıya DOKUNULMADI** ·
`__EFMigrationsHistory = 67`.

---

## § 8 — KİMLİK KAPANIŞ TABLOSU

| Kimlik | Durum | Kanıt |
|---|---|---|
| **R-1** (F-1 abonelik + F-3 plan kapısı, koşulsuz 1.0) | **KAPANDI** | **Şema:** `Modules/Abonelik/AbonelikVarliklari.cs:37-96` + migration `20260922031115` (canlı `__EFMigrationsHistory 66→67`, 9 dönem geri dolduruldu). **Yetki kapısı:** `AbonelikServisi.cs:167-176` (seviye kıyası + 403) · test `Yukseltme_kiraci_tarafindan_REDDEDILIR_403` · canlı adım 4. **Canlı denetim izi:** `AbonelikServisi.cs:178-182` ve `AbonelikSistemServisi` iz bloğu · test `Iz_indirmede_yazilir` · canlı SQL (1 × `PLAN_YUKSELTME_RED`, 3 × `PLAN_DEGISIMI`). **Fail-closed:** `KiraciDurumKapisi.cs:87-99` · test `FailClosed_suresi_dolan_kiraci_YAZAMAZ` + `…OKUYABILIR_veri_rehin_alinmaz` · canlı adım 8-10. **Sayaçlar:** `denetim.sh` `P-27` bloğu, ikisi de 0. |

**Devredilenler (kapsam büyütülmedi, gizlenmiyor):**
* **Plan TAVANLARININ ürün davranışına bağlanması** (kullanıcı/şube sınırı) → **devredildi: P-28**
  (ölçüm önce, zorlama sonra). Kolonlar NULL doğdu, hiçbir yerde sınır vaat edilmiyor.
* **Operatör konsolu ekranı** (`/sistem/kiracilar` Blazor yüzü) → **devredildi: dalga 6 T-4b hattı**
  (F-2-min mührü K5). Bu pakette uç var, operatör ekranı yok; kullanım runbook/curl iledir.
* **Fiyat, teklif, tahsilat** → **K-16 (kurucu)**. Ücretli sözleşme metni üretilmedi (MÜHÜR 2).
* **Abonelik tablolarının FK'ları** → **P-34** (şema dilimi bilerek önce bitirildi).

---

## § 9 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Plan bloğu Dosyalar/DoD/Sayaçlar sözleşmesi | **KARŞILANDI** (+ § 1'de yazılı 7 sapma) | bu rapor |
| Migration prosedürü (build yeşil · `--no-build` yok · boyut + Up okundu · kuyumcu_net'e uygulandı) | **KARŞILANDI** | § 3 |
| Yeni varlık IKiraciVarlik + tenant_id indeksi | **KARŞILANDI** (`KiraciAbonelik`; katalog gerekçeli beyaz listede) | `KuyumcuDbContext` indeks bloğu + beyaz liste |
| DoD sözleşmesi (PATCH / pasife alma / yetki / denetim / ekran ya da DegismezIpucu) | **KARŞILANDI** | § 2.6 |
| DoD EK md.1 (her düğme istek atar ya da disabled + sebep) | **KARŞILANDI** | `i-plan-onay-*` düğmeleri istek atar; seçici yoksa `DegismezIpucu` çizilir (bUnit) |
| DoD EK md.2 (tek düzenleme kalıbı · ModalPencere yalnız yıkıcı onay) | **KARŞILANDI** | ModalPencere **kullanılmadı**, sayaç artmadı (§ 1 S5) |
| DoD EK md.3 (sessiz etkisizlik yok) | **KARŞILANDI** | seçim → onay → PATCH; hata bandı; başarı bandı |
| DoD EK 2 (tek dokunuş) | **KARŞILANDI** | **Dokunulan sayfalar: `Kuyumcu.Web/Pages/Isletme.razor` (tek sayfa, bu dalgada ilk dokunuş)** |
| UX sözleşmesi (S21-S33) | **KARŞILANDI** | § 2.5'te madde madde |
| Finansal varlıkta hard DELETE yok | **KARŞILANDI** | dönem `SONLANDI` olur; DELETE ucu yok |
| Tek yazma yolu / tek storno motoru / kiracı izolasyonu | **KARŞILANDI** | abonelik defter yazmaz; `HareketFabrikasi`/`StornoServisi` dokunulmadı |
| Sır/parola/jeton çıktıya yazılmadı | **KARŞILANDI** | parola değişkenden, jetonlar dosyada |
| COMMIT YOK · git/rm/mv yok | **KARŞILANDI** | çalışma ağacında |
| Tam süit 0 başarısız · denetim TEMİZ · test kiracısı silindi | **KARŞILANDI** | § 4 · § 6 · § 7 |
| Tahmin 300 ajan-dk | **AŞILMADI (yaklaşık sınırda)** | ek yük: sistem ucunun DbContext kısıtı (500) ve 4 sayaç/tavan çakışması |

---

## § 10 — DEĞİŞEN DOSYALAR

**Yeni:**
`dotnet/Kuyumcu.Api/Modules/Abonelik/AbonelikVarliklari.cs` ·
`dotnet/Kuyumcu.Api/Modules/Abonelik/AbonelikServisi.cs` ·
`dotnet/Kuyumcu.Api/Modules/Abonelik/AbonelikModulu.cs` ·
`dotnet/Kuyumcu.Api/Data/Migrations/20260922031115_AbonelikVePlanKapisi.cs` (+ `.Designer.cs`) ·
`dotnet/Kuyumcu.Tests/P27AbonelikTestleri.cs` ·
`dokumanlar/09-uygulama-kayitlari/PAKET-P-27-uygulama-raporu.md`

**Değişen:**
`dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` ·
`dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs` ·
`dotnet/Kuyumcu.Api/Modules/Sistem/KiraciDurumKapisi.cs` ·
`dotnet/Kuyumcu.Api/Modules/Admin/AdminModulu.cs` ·
`dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs` ·
`dotnet/Kuyumcu.Api/Modules/ModulKayit.cs` ·
`dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` ·
`dotnet/Kuyumcu.Contracts/Sayfalar/IsletmeSozlesmeleri.cs` ·
`dotnet/Kuyumcu.Web/Pages/Isletme.razor` ·
`dotnet/denetim.sh` ·
`dotnet/Kuyumcu.Tests/{P09KimlikErisimTestleri,IsletmeEkranTestleri,DenemeBandiEkranTestleri,OlcumKapisiTestleri,SozlesmeYansimaTestleri}.cs` ·
`dokumanlar/09-uygulama-kayitlari/ham/olcum-2026-09-18.json`

(`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısında görünen diğer `M` kayıtları
**bu paketten önce** vardı — `Data/Entities.cs` dâhil; bu pakette Entities.cs'e DOKUNULMADI.)

---

## § 11 — AÇIK KALANLAR / NOTLAR

1. **Kurucuya (K-16):** fiyat rakamı ve ücretli sözleşme metni hâlâ sizdedir; şemada tutar kolonu
   **yoktur** ve sayaçla kapalı tutulmaktadır. Rakam gelince katalog kolonu + ayrı kapı kaydı gerekir.
2. **Kurucuya:** `Sistem__Anahtari` üretimde ortam değişkeninden gelmelidir (K-5 sır rotasyonu);
   yer tutucu ile bu uçlar **503**'tür (fail-closed) — yani abonelik yönetimi anahtar olmadan çalışmaz.
3. **Mali müşavire not gerekmedi:** bu pakette vergi/mevzuat hükmü koda yazılmadı.
4. **P-28 için:** plan tavanları (kullanıcı/şube) ölçülmeden zorlanmayacak; kolonlar hazır, NULL.
5. **P-34 için:** `kiraci_abonelikleri.tenant_id → tenants.id` ve `plan_kodu → abonelik_planlari.kod`
   FK adayıdır; şema dilimi bilerek önce bitirildi.
6. **Operatör konsolu ekranı yok** (devredildi) — bu bir **UX-S25 borcudur** ve triyaj (Y-42) kuralı
   gereği kimliklenmelidir.
