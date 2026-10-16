# PAKET P-01 — SÜRÜM MÜHRÜ + DALGA 2b DEVİR KAYDI (uygulama raporu)

**Tarih:** 2026-09-18 · **Dalga:** DALGA 0 (kod dokunulmaz) · **Rol:** Taşkın (backend) + Ertuğ (frontend) eli, mühür Tamer Bey
**Plan bloğu:** `PLAN-TEK-SEFER-2026-09-18.md` satır 151-168 · **Sürüm:** 1.0 · **Migration:** HAYIR (gerekmedi)
**Tahmin:** 45 ajan-dk · **Gerçekleşen:** ≈40 ajan-dk (sapma katsayısı ≈0,9)

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (P-00, varsayım yok)

| Önkoşul çıktısı | Diskte | Ölçüm |
|---|---|---|
| `dokumanlar/09-uygulama-kayitlari/KAPSAM-ENVANTERI.md` | **VAR** | 55.821 bayt, 2026-09-18 14:09 |
| `ham/triyaj-kayit.json` (BULGU_DAMGASIZ) | **VAR** | `denetim.sh` P00-4 yeşil |
| `dotnet/denetim.sh` `# >>> P-00` bloğu | **VAR** | KIMLIK_KOKENSIZ 0 · envanter 172 · BULGU_DAMGASIZ 0 · YENI_KIMLIK_DALGA 14 |
| `ham/surum.json` (P-01 girdisi) | **VAR** | 6.453 bayt, 2026-09-16 — plandaki ölçümle birebir |

Önkoşul tam; kapsam daraltmaya gerek olmadı.

---

## § 1 — YAPILANLAR

### 1.1 `ham/surum.json` — karar bekleyen 6 satır kapatıldı

Ölçüm (öncesi): 55 satır, bunların **6'sı "karar bekliyor"** (5 · 39 · 42 · 43 · E-BELGE · 40-B).
Ölçüm (sonrası): **89 satır · 1.0: 76 · 1.1: 12 · KAPSAM-DIŞI: 1 · küme dışı: 0.**

| Paket | Mühür | Dayanak (karar değil, kanıt) |
|---|---|---|
| **5** — etiket/belge şablon motoru | **1.1** | PLAN v2 § 2'nin 36 paketinde motor yok; 1.0 karşılığı P-17'nin SABİT şablonu (§ 5 R-11: "karar gelmeden yalnız sabit şablon yazılır (P-17)") |
| **39** — has altın defteri dökümü | **1.1** | Plan v2'de paket karşılığı yok; önkoşulu (42 + 38 belge altyapısı) 1.0'da yalnız P-17 kadar var. IslemDefteri yazdırması P-17 ile 1.0'da |
| **42** — ortak belge yerleşimi | **1.0** | P-17 ORTAK ÇIKTI ŞABLONU (`Bilesenler/Yazdirma/`, PusulaBelgesi:213, IslemDefteri:307, PanoFiyatServisi:75) 1.0'dadır; belge.css standardının kalanı UX-2-C ile 1.1 |
| **43** — tam mobil uyarlama | **1.1** | **R-13 oyu 2-2 kırıldı.** 1.0 mobil ASGARİSİ P-18'dir (42/42 rotada `scrollWidth ≤ 375`, gerçek tarayıcıda tek tek — BK-24). Tam uyarlama D-11 (44 + PWA-1) ile aynı ele düşer → 1.1 |
| **E-BELGE** | **KAPSAM-DIŞI** | Entegratör sözleşmesi + mali mühür KURUCU KALEMİ **K-2**; ekip uygulamaz. 1.0'ın şartı uygulamak değil, durumu kanıtlı beyan etmek (Mehmet uyum md.2) → `docs/HARIC-LISTESI.md` tek kaynağı (BK-26) |
| **40-B** — yaptırım ad alanı şifrelemesi | **1.1** | KURUCU KALEMİ **K-7** (KVKK aydınlatma metni, ihtiyaç DALGA 7 öncesi) + Mehmet KVKK kapısı açık. 1.0'da 40-A (MASAK risk skorlama) var |

### 1.2 Satırsız kalan iki karar kimliği açıldı

| Yeni satır | Mühür | Dayanak |
|---|---|---|
| **UX-2-C** | **1.1** | **R-14.** Kapsam `MIMARI-KALAN-PAKETLER-2026-09-15.md:62-63,143` ile DOĞRULANDI ("UX-2-C yazdırma standardını belge dosyaları donduktan sonra kapatır", dalga 5 hattı 42-A → 5-A → 39-A → UX-2-C). 5 ve 39 1.1 olduğu için UX-2-C de 1.1; 1.0 karşılığı P-17 + P-18 tema belirteçleri |
| **LAYAWAY** | **1.1** | **R-19.** 1.0 bacağı **P-21**'dedir (AVANS + REZERV + bloke adet, sürüm 1.0). Ödeme takvimi/taksit planı tablosu **Y-24**'tür ve plan § 6'da 1.1'dir (P-21 DoD son maddesi: "1.1 için yer bırakılır") |

### 1.3 Plan § 5 kararlarının mühre yansıtılması (karar sahibi başka paket — burada yalnız KAYIT)

`surum.json` 2026-09-16 MÜHÜR 5 hâlini taşıyordu ve **plan v2 ile altı yerde çelişiyordu**. Plan üstündür (görev metni + § 1 md.4):

| Satır | Eski (MÜHÜR 5) | Yeni (plan v2) | Dayanak |
|---|---|---|---|
| F-1 abonelik | 1.1 | **1.0** | § 1 md.4 + § 5 R-1 "KOŞULSUZ 1.0" (P-27) |
| F-3 plan kapısı | 1.1 | **1.0** | § 1 md.4 + § 5 R-1 |
| F-4 telemetri | 1.1 | **1.0** | § 1 md.4 + § 5 R-2 (BK-12 ön şart) |
| S-1 ticari paket | 1.1 | **1.0** | § 5 R-3 + § 6 D-8 ("1.0'A ALINDI") |
| SÖZLEŞME-1 | 1.1 (4/4 ret) | **1.0** | § 5 R-10 + § 2 P-03 (DALGA 1, sürüm 1.0) |
| T-4 (T-4b dilimi) | 1.0 ama "T-4b → 1.1" | **1.0** | § 5 R-5 (P-31) |

### 1.4 PLAN § 2'nin 36 paketi mühre taşındı (tek kaynak)

`#### P-xx` bloklarının **Sürüm** satırı betikle okunup (`scratchpad/p01/muhurle.js`) `surum.json`'a yazıldı: **36 paket, hepsi 1.0.** Gerekçe alanı hangi blok satırından geldiğini söyler; karar o paketin bloğundadır, P-01 yalnız kaydeder. Böylece "plan bir şey, mühür başka şey" durumu mekanik olarak imkânsızlaştı (sayaç P01-3).

### 1.5 TEK KAYNAK İLANI — ikinci kaynak kapatıldı

- **Tek kaynak: `dokumanlar/09-uygulama-kayitlari/ham/surum.json`.** Beslendiği yer: `PLAN-TEK-SEFER-2026-09-18` § 2 (paket blokları) + § 5 (kararlar).
- `betikler/surum-kaydet.js` **yeniden yazıldı**: artık dosyayı sıfırdan yazmaz, **birleştirir**; küme dışı değer (ör. "karar bekliyor") **fail-closed reddedilir (çıkış 3)**; mühürlü bir satırın sürümü ancak `--uzerine-yaz` ile değişir ve ne değiştiği ekrana yazılır.
- `betikler/v1-surum-muhru.js` ve `dokumanlar/02-mimari/SURUM-MUHRU-V1-2026-09-16.md` başına **"ARŞİV — TEK KAYNAK DEĞİLDİR"** damgası kondu; çelişkide `surum.json` geçerlidir denildi ve hangi satırların değiştiği (F-1/F-3/F-4/S-1/SÖZLEŞME-1 → 1.0; 5/39/43/40-B/UX-2-C → 1.1; E-BELGE → KAPSAM-DIŞI) belgeye yazıldı. Bu üç damga `SURUM_IKINCI_KAYNAK` sayacıyla korunuyor.

### 1.6 `dotnet/denetim.sh` — `# >>> P-01` işaretli sayaç bloğu (satır 990-1043)

| Sayaç | Değer | Ölçüm komutu (betiğe yorum olarak yazıldı) |
|---|---|---|
| `SURUM_KARAR_BEKLEYEN` | **0** (satır 89) | `grep -c '"surum":'` eksi `grep -cE '"surum": "(1\.0\|1\.1\|KAPSAM-DIŞI)"'` |
| sürüm satırı (cırcır taban) | **89** (≥ 89) | aynı grep; taban BUGÜN ölçüldü, belgeden kopyalanmadı |
| `SURUM_PLANSIZ_PAKET` | **0** | plandaki her `^#### P-[0-9]{2}` kimliği `surum.json`'da aranır |
| `SURUM_IKINCI_KAYNAK` | **0** | arşiv belgesi + arşiv betiği damgası + `surum-kaydet.js` küme kontrolü |

Tavan/eşik artırılmadı; iki cırcır (89 satır, 0 karar bekleyen) yalnız sıkılaşabilir.

### 1.7 DALGA 2b DEVİR KAYDI (7 paket)

Kaynak: `ORKESTRATOR-DURUM.md` 17:12 → 22:26 satırları (kapı 2026-09-16 22:11: build 0/0 · denetim TEMİZ · **1650/1650** · 33 migration · tenants 7).

| Paket | Durum | Doğrulayıcı | Devrettiği yer (yarım/açık kalan) |
|---|---|---|---|
| **UX-4d** | **BİTTİ** | tur 1 GEÇTİ (1394) | Raporlar.razor md.11/12/21 alanları, "Aktifleştir" düğmesi, alan altı mesaj bağlama → UX-1-C (2c) · bugünkü planda **P-12/P-13/P-22** |
| **UX-1-B** | **BİTTİ** | tur 1 GEÇTİ (1439) | Ekran bağlama UX-1-C, ürün PATCH `reyonId:null` üç-durum, gider pusulası belge alanları → 2c · bugünkü planda **P-06/P-10/P-13** |
| **38-A** | **BİTTİ** | tur 2 GEÇTİ (1473) | ELLE yolunda `hasDeger ?? 0` fail-closed 400 → UX-1-C2 · bugünkü planda **P-22**. Faz B (e-Arşiv/UBL) → **E-BELGE / KAPSAM-DIŞI** |
| **48-A** | **BİTTİ** | tur 1 GEÇTİ (1508) | TAKAS/HURDA_ALIS/TRANSFER + çok bacaklı fiş stornosu 409 (ayrı paket + ayrı oy) → bugünkü planda **P-20/P-21/P-23**; rol yetersizliğinde 409 yerine 403 → **P-09**; raporlarda StornoSuzgeci → **P-24/P-25** |
| **45-API** | **BİTTİ** | tur 1 GEÇTİ (1540) | SAHİP kilitlenme runbook'u (T-1 üretim yayını bunsuz başlamaz) → **P-33**; anahtar rotasyonuna MFA gizi maddesi → **K-5 / P-09**; MFA kapatmada kod tüketimi + uç adı hizalaması + `mfa-kurtarma-kullanildi` denetim olayı → **P-09**; Faz B (yönetici mfa-sıfırlama) → 1.1 |
| **6-A** | **BİTTİ** | tur 1 GEÇTİ (1590) | `/bildirimler` basit mod menüsü → **P-14**; vade bildiriminde müşteri adı (rol bazlı istisna, ekip oyuna açık) → **P-11**; Faz B TEVSIK_KURTARMA yapılandırma varsayılanı KAPALI |
| **3-A** | **BİTTİ** | tur 1 GEÇTİ (1650) | Faz B bilinçli kapsam dışı; sipariş→satış dönüşümü → **P-21** |

**Terk edilen paket: 0. Yarım kalan paket: 0.** Yedisi de doğrulayıcıdan geçti ve orkestratör kapısı 22:11'de mühürlendi. Devredilen kalemler yukarıda P-xx karşılıklarıyla yazıldı; hiçbiri sahipsiz bırakılmadı.

### 1.8 R-15 — sıra 0 mührü (sadeleştirme yeni modülden ÖNCE)

Plan § 5 R-15 kararı **KABUL** olarak zaten yazılı ve DALGA 4 buna göre dizilmiş. P-01'in işi bunu mühürlemek: **Y-5 / Y-9 / Y-29 sadeleştirme kalemleri (menü/bilgi mimarisi, etiket çarpışmaları, kabuk) P-14 ve P-15'tedir ve DALGA 4'te yeni modül açan paketlerden (P-16, P-17, P-18) ÖNCE gelir.** Plan § 2 DALGA 4 sırası bunu birebir sağlıyor (P-14 → P-15 → P-16 → P-17 → P-18); ayrıca `Pages/Islemler.razor` tek-el zinciri (P-15 dokunmaz → P-16 → P-18) bu sırayı kilitliyor. Sıra bozulursa kapı `TEK_EL_IHLAL` ile kırılır (BK-05, P-02'de kurulacak). **Mühür: sıra 0 geçerlidir, DALGA 4 yeniden dizilmez.**

---

## § 2 — KİMLİK KAPANIŞ TABLOSU (zorunlu)

| Kimlik | Durum | Kanıt |
|---|---|---|
| **R-9** — sürüm mührü (açılış) | **KAPANDI** | `ham/surum.json` 89 satır, küme dışı 0; `dotnet/denetim.sh:990-1043` `# >>> P-01` bloğu; `./denetim.sh` → "P-01: SURUM_KARAR_BEKLEYEN 0 (satır 89)". Kapanış mührü P-35'te |
| **R-11** — paket 5 şablon motoru | **KAPANDI** | `surum.json` `"5" → 1.1`; 1.0 karşılığı plan § 2 P-17 (satır 428-440) |
| **R-12** — paket 39 has defteri dökümü | **KAPANDI** | `surum.json` `"39" → 1.1` |
| **R-13** — paket 43 tam mobil (oy 2-2) | **KAPANDI** | `surum.json` `"43" → 1.1`; 1.0 asgarisi P-18 (plan satır 441-453, BK-24). Oy kırıldı → D-11 ve P-18 sıraya girebilir |
| **R-14** — UX-2-C sürümü | **KAPANDI** | `surum.json` `"UX-2-C" → 1.1`; kapsam `MIMARI-KALAN-PAKETLER-2026-09-15.md:62-63,143` ile doğrulandı ("doğrulanmalı" etiketi kalktı) |
| **R-15** — § 10 sıra 0 mührü | **KAPANDI** | § 1.8; plan § 2 DALGA 4 sırası (P-14 → P-15 → P-16 → P-17 → P-18) + § 7 `Islemler.razor` tek-el zinciri |
| **R-19** — layaway | **KAPANDI** | `surum.json` `"LAYAWAY" → 1.1`; 1.0 bacağı P-21 (plan satır 488-502) |

**Kapsam dışına çıkılmadı:** başka paketin kimliğine dokunulmadı. § 1.3'teki altı satır **karar değil kayıttır** — kararları plan § 5 (R-1, R-2, R-3, R-5, R-10) zaten vermiş, uygulamaları P-27/P-28/P-29/P-31/P-03'tedir.

---

## § 3 — SÖZLEŞME-DoD (Bertan koşulu)

Bu paket **kod dokunmayan** bir belge/mekanizma paketidir: **yeni varlık yok, yeni uç yok, yeni ekran yok, migration yok** → PATCH / DELETE / `[RolGerekli]` / `DenetimServisi` / ekran düzenleme yolu maddeleri **konusuz**dur (uydurma uç açılmadı).

| "Kayıt türü" | Düzenlenebilir mi | Nasıl |
|---|---|---|
| `ham/surum.json` satırı | **EVET** | `surum-kaydet.js` ile birleştirilerek; sürüm değiştirmek `--uzerine-yaz` ister ve ne değiştiğini basar (denetlenebilir "PATCH" semantiği: gönderilmeyen paket satırı KORUNUR, silinmez) |
| Küme dışı sürüm (ör. "karar bekliyor") | **HAYIR — bilerek** | `DegismezIpucu` karşılığı: betiğin reddi Türkçe gerekçe basar ("kararı önce ekip oyuyla kapat, PLAN § 5") ve çıkış 3 verir; ayrıca `denetim.sh` cırcırı ile kapı kırılır |
| Mühürlü satırın SİLİNMESİ | **HAYIR** | Cırcır taban 89: satır sayısı düşerse denetim BAŞARISIZ (finansal kayıttaki "hard DELETE yasağı"nın belge karşılığı) |

---

## § 4 — DERLEME, TEST, DENETİM

```
dotnet build Kuyumcu.sln --nologo   → 0 Uyarı · 0 Hata (BUILD_EXIT=0)
dotnet test Kuyumcu.Tests ... --logger trx
  → total="2893" executed="2893" passed="2893" failed="0"  (TEST_EXIT=0, 38,2 sn)
dotnet/denetim.sh                    → DENETİM TEMİZ (exit 0)
```

- **Test sayısı önce/sonra: 2893 / 2893 — yeni test 0.** Dürüst gerekçe (md.1): bu pakette C# kodu değişmedi; paketin "testi" `denetim.sh`'ın dört yeni sayacıdır ve dördü de mutasyonla kırmızıya çevrildi (§ 5). Süite sahte test eklenmedi.
- trx: `scratchpad/trx-pP-01/_Ramazan-MacBook-Air_2026-09-18_14_12_44.trx`
- Migration sayısı değişmedi: `__EFMigrationsHistory = 49` (SQL ile ölçüldü).

---

## § 5 — MUTASYON KANITLARI (4/4 kırmızı)

| Kod | Değişiklik | Kırmızıya dönen denetim satırı | Geri alma |
|---|---|---|---|
| **M-P01-1** | `surum.json`'da "43" satırı `1.1` → `karar bekliyor` (perl -0777) | `✗ P-01: SURUM_KARAR_BEKLEYEN 1 (beklenen 0)` → `denetim.sh` exit **1** | `cmp` birebir aynı |
| **M-P01-2** | `LAYAWAY` satırı silindi (89 → 88) | `✗ P-01: sürüm satırı 88 (en az 89)` → exit **1** | `cmp` birebir aynı |
| **M-P01-3** | `P-35` satırı silinip yerine dolgu satırı kondu (sayı 89 sabit) | `✗ P-01: SURUM_PLANSIZ_PAKET 1 (beklenen 0)` → exit **1** | `cmp` birebir aynı |
| **M-P01-4** | Arşiv belgesinden "TEK KAYNAK DEĞİLDİR" damgası kaldırıldı | `✗ P-01: SURUM_IKINCI_KAYNAK 1 (beklenen 0)` → exit **1** | `cmp` birebir aynı |

**Test açığı yok** — dört sayacın dördü de kendi mutasyonuyla kırıldı. (İlk denemede M-P01-2/M-P01-3'ün `perl` deseni eşleşmediği için sayaçlar yeşil kalmıştı; bu bir test açığı değil **mutasyonun uygulanmamasıydı** — gerekçe: gerekçe metinlerinde JSON-kaçışlı tırnak var. Mutasyon `node` ile yeniden uygulandı ve ikisi de kırmızı verdi. Madde 1 gereği yazıldı.)

**Ek kanıt — betiğin fail-closed davranışı:**
```
node surum-kaydet.js <"karar bekliyor" içeren girdi>  → REDDEDİLDİ, EXIT=3, dosya değişmedi
node surum-kaydet.js <"43 → 1.0" diyen eski taslak>   → "mühür korundu 1 [43 (mühürlü 1.1 ≠ gelen 1.0)]", dosya cmp ile birebir aynı
```
Son mutasyondan sonra `denetim.sh` yeniden koşuldu: **TEMİZ.**

---

## § 6 — CANLI DOĞRULAMA (5198; 5199'a DOKUNULMADI)

Bu paket hiçbir uç/ekran değiştirmediği için canlı adımlar **gerileme kontrolüdür** (yeni davranış iddiası yok).

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET http://localhost:5198/` | 200 (SPA) | **HTTP 200** |
| 2 | `POST /api/v1/auth/giris` gövde `{}` | 400 + Türkçe alan mesajları, 500 YOK | **HTTP 400** · `{"message":["E-posta zorunludur","Şifre zorunludur"],...,"istekKimligi":"01M2T3NQWG..."}` |
| 3 | `GET /api/v1/cari` (jetonsuz) | 401 | **HTTP 401** · `{"message":"Token bulunamadı",...}` |
| 4 | `GET /api/v1/yok-boyle-bir-uc` | 404 JSON (SPA fallback değil) | **HTTP 404** · `{"message":"Uç bulunamadı","yol":"/api/v1/yok-boyle-bir-uc"}` |

**SQL kanıtı:** `SELECT COUNT(*) FROM tenants` → **8** (önce) / **8** (sonra) · `WHERE ad LIKE 'PAKET%'` → **0** · `__EFMigrationsHistory` → **49**.
**Test kiracısı açılmadı** (yazma işlemi yok) → silinecek kiracı yok; `QA-TEST-*` fikstürlerine dokunulmadı.
**Sunucu kapatıldı:** `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır**. `5199` üç dinleyiciyle **çalışır durumda bırakıldı** (kurucunun inceleme sunucusu, dokunulmadı).

---

## § 7 — KURUCU KALEMLERİ İHTİYAÇ TARİHİ TABLOSU (BK-36 / Ramazan K4 — kurucuya iletilir)

Plan § 4'te **26 kurucu kalemi** var; **ihtiyaç tarihi boş alan = 0** (ölçüldü). En yakın tarihe göre sıralı özet:

| Sıra | Kalem | İhtiyaç tarihi | Gelmezse |
|---|---|---|---|
| 1 | **K-20** iCloud eşitlemesini kapatmak | **Hemen — DALGA 0 öncesi (GECİKTİ)** | Tüm takvim geçersiz. *Bugünkü ölçüm: `bird` süreci %98,7 CPU; yük 2,60 — derleme bu koşumda hızlıydı ama risk duruyor* |
| 2 | **K-5** sır rotasyonu (JWT, MySQL parolası) | **DALGA 1 öncesi** | Yayın fail-closed durur (Mehmet) |
| 3 | **K-4** depo temizliği (5 `.mutorj`, 8 adet 0 baytlık migration) | DALGA 1 öncesi | Grep tabanlı sayaçlar kirli kalır (ekip silemez, hook `rm`'i bloklar) |
| 4 | **K-3** kur verisi için yazılı izin/abonelik | DALGA 3 öncesi | Pilot varsayılanı TCMB + elle has/gram |
| 5 | **K-24** negatif açılış / KMH istisnası · **K-25** gider pusulası stopaj+7 gün | DALGA 3 öncesi | Mevcut tutucu davranış korunur, ekranda gerekçe yazar |
| 6 | **K-13** Katman 1 / fire sınırı | DALGA 5 öncesi | P-20 bekler, P-26'nın Y-36 dilimi yazılmaz |
| 7 | **K-23** sipariş ATOLYEDE/HAZIR geri dönüşü | DALGA 5 öncesi | P-22 o yolu açmaz |
| 8 | **K-10** mali müşavir (KDV 23/e, KMTS, vade farkı KDV'si) | **DALGA 6 öncesi** | P-26 Y-40 anahtarı açılmaz |
| 9 | **K-1** canlı DB sızıntı satırlarının silinmesi · **K-7** KVKK metni (40-B) | DALGA 7 öncesi | P-34 (FK) ve P-30 demo başlayamaz; 40-B 1.1'de bile açılamaz |
| 10 | **K-19** dış iletişim imzası (pilot daveti) | DALGA 4 sonrası | Pilot başlamaz → F-8 destek satırı boş → ücretli tarih verilemez |
| 11 | **K-2** e-Fatura entegratör sözleşmesi · **K-11** ÖKC · **K-6** bulut hesabı · **K-9** alan adı/TLS · **K-17** rakip deneme hesabı · **K-18** yedek anahtarı · **K-26** kısayol adresi | P-29 / P-33 / T-1 öncesi | İlgili kalem `HARIC-LISTESI.md`ye "hariç" yazılır |
| 12 | **K-16** rakamlı fiyat İLANI | En sonda | Ürün hazır olur, fiyat ilan edilemez |
| — | **K-8** commit kararı | Her an | Talimat değişmedikçe **commit yok** (bu pakette de commit yapılmadı) |
| — | **K-12**, **K-14**, **K-15**, **K-21**, **K-22** | 1.1 | 1.0'ı bloklamaz |

**Mühürle doğrudan ilgili iki kalem:** E-BELGE'nin KAPSAM-DIŞI mührü **K-2**'ye, 40-B'nin 1.1 mührü **K-7**'ye bağlıdır. İkisi gelirse mühür `--uzerine-yaz` ile tek satırda güncellenir.

---

## § 8 — SAPMALAR VE GEREKÇELERİ

1. **"denetim.sh seri sırası P-01 ile başlar" (Bertan K8) — fiilen P-00 ile başladı.** P-00 kendi `# >>> P-00` bloğunu zaten yazmıştı (bugün 14:09). İşaretli blok kuralı (§ 7) bunu zaten meşru kılıyor; P-01 bloğunu P-00'ınkinden sonra ekledim. **Plan metniyle çelişki raporlanmıştır**; kapsamı büyütmemek için P-00 bloğuna dokunulmadı.
2. **Plan ile `surum.json` çelişkisi (6 satır)** § 1.3'te plan lehine kapatıldı; çelişkinin kendisi kayıt altındadır (arşiv belgesine de yazıldı).
3. **Yeni test 0.** Kod değişmediği için süite test eklenmedi; mekanizma kanıtı dört denetim sayacı + dört mutasyondur (§ 5). Gizlenmedi.
4. **Kurul turu koşulmadı.** R-11/R-12/R-14/R-19 plan § 5'te "ekip oyuyla mühürlenir" diyor; bu paket tek uygulayıcı koşumudur, çok-ajanlı kurul turu açılmadı. Uygulanan kural plan § 2 P-01'in kendi hükmüdür: *"kırılmazsa Tamer Bey mühürler — kurucuya çıkılmaz."* Her mühür plan/mimari metnine bağlı kanıtla gerekçelendirildi ve **dalga 0 kapısındaki kurul turu tarafından tek satırda geri alınabilir** (`--uzerine-yaz`). Madde 1 gereği yazıldı.
5. **`docker-compose.yml:22` healthcheck satırında MySQL parolası açık yazılı.** Parola değişkenle alınırken bu satır ekrana yansıdı (değeri bu raporda tekrarlanmıyor). **K-5 sır rotasyonu kapsamına ek madde olarak kurucuya bildirilir**; rotasyonda healthcheck satırı da değişmeli.

---

## § 9 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| `surum.json`'da "karar bekliyor" = 0, her paket {1.0, 1.1, KAPSAM-DIŞI} | **KARŞILANDI** | denetim: `SURUM_KARAR_BEKLEYEN 0 (satır 89)` |
| R-15 sıra 0 mührü | **KARŞILANDI** | § 1.8 |
| R-13 oyu 2-2 kırıldı, kurucuya çıkılmadı | **KARŞILANDI** | § 1.1 "43 → 1.1"; kurucuya hiçbir karar sorulmadı |
| Dalga 2b'nin 7 paketi bitti/yarım/terk olarak yazıldı | **KARŞILANDI** | § 1.7 (7 BİTTİ, 0 yarım, 0 terk) + devir hedefleri |
| Mevcut mühür mekanizması ↔ `surum.json` ilişkisi, tek kaynak ilanı | **KARŞILANDI** | § 1.5 + `SURUM_IKINCI_KAYNAK 0` |
| Kurucu kalemleri ihtiyaç tarihi tablosu (BK-36) | **KARŞILANDI** | § 7; boş alan 0 (26/26 dolu) |
| Sayaç `SURUM_KARAR_BEKLEYEN = 0` cırcır olarak `denetim.sh`'ta | **KARŞILANDI** | `dotnet/denetim.sh:990-1043`, ölçüm komutları yorum olarak yazılı (BK-07), taban taze ölçüldü |
| Mutasyon kanıtı (her kural için) | **KARŞILANDI** | § 5, 4/4 kırmızı, `cmp` ile geri alındı |
| Tam süit 0 başarısız | **KARŞILANDI** | 2893/2893 |
| `denetim.sh` TEMİZ | **KARŞILANDI** | exit 0, "DENETİM TEMİZ." |
| Canlı doğrulama 5198, 5199'a dokunulmadı | **KARŞILANDI** | § 6 |
| Test kiracısı silindi | **KONUSUZ** | kiracı açılmadı; tenants 8 → 8, `PAKET%` = 0 |
| Migration | **GEREKMEDİ** | plan "Migration: HAYIR"; `__EFMigrationsHistory` 49 → 49 |
| COMMIT YOK | **KARŞILANDI** | hiçbir git komutu çalıştırılmadı |
| `MIMARI-KALAN-PAKETLER` ve paket spesifikasyonuna dokunulmadı | **KARŞILANDI** | damgaları 15 Eylül · 18 Eylül 13:53 (P-01 öncesi), § 10 |

---

## § 10 — DEĞİŞEN DOSYALAR

**DÜRÜSTLÜK NOTU (md.1):** `git status --porcelain` bu dosyaların hiçbirini `M` olarak göstermez — `dokumanlar/` ve `dotnet/denetim.sh` depoda **izlenmiyor** (`?? dokumanlar/`, `?? dotnet/denetim.sh`, `?? dotnet/dokumanlar/`). Bu yüzden değişen dosyalar `ls -la` (boyut + damga) ile listelenir:

| Dosya | Boyut | Damga | İşlem |
|---|---|---|---|
| `dokumanlar/09-uygulama-kayitlari/ham/surum.json` | 17.154 bayt (önce 6.453) | 14:14 | 6 karar kapatıldı, 2 satır açıldı, 6 satır plana hizalandı, 36 plan paketi eklendi |
| `dokumanlar/09-uygulama-kayitlari/betikler/surum-kaydet.js` | 3.092 bayt (önce 1.201) | 14:13 | birleştirme + fail-closed küme kontrolü |
| `dokumanlar/09-uygulama-kayitlari/betikler/v1-surum-muhru.js` | 6.007 bayt (önce 5.289) | 14:13 | "ARŞİV — TEK KAYNAK DEĞİLDİR" başlığı |
| `dokumanlar/02-mimari/SURUM-MUHRU-V1-2026-09-16.md` | 18.830 bayt (önce 18.131) | 14:14 | arşiv damgası + hangi satırların geçersiz olduğu |
| `dokumanlar/09-uygulama-kayitlari/ORKESTRATOR-DURUM.md` | 126.993 bayt (önce 124.269) | 14:17 | P-01 devralma kaydı eklendi |
| `dotnet/denetim.sh` | 59.274 bayt (önce 56.188) | 14:14 | `# >>> P-01` … `# <<< P-01` bloğu (satır 990-1043) |
| `dokumanlar/09-uygulama-kayitlari/PAKET-P-01-uygulama-raporu.md` | YENİ | 14:17 | bu rapor |

Yedekler (geri alma için): `scratchpad/mut-yedek/P-01/*.orig` — altı dosyanın P-01 öncesi hâli.
`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısında bu paketten doğan **yeni `M` satırı yoktur**; kod (`.cs`, `.razor`) dosyası değiştirilmedi.

`PLAN-TEK-SEFER-2026-09-18.md`, `MIMARI-KALAN-PAKETLER-2026-09-15.md` ve `SONRAKI-OTURUM-PROMPT.md` **değiştirilmedi**. Kod (`.cs`, `.razor`) dosyası **değiştirilmedi**.

---

## § 11 — AÇIK KALANLAR

1. **Dalga 0 kapısındaki kurul turu** dört mührü (R-11, R-12, R-14, R-19) oylamayla teyit etmeli; itiraz olursa `surum-kaydet.js --uzerine-yaz` tek satırda değiştirir (§ 8.4).
2. **K-2 gelirse E-BELGE** KAPSAM-DIŞI'dan 1.1'e taşınır; **K-7 gelirse 40-B** 1.1 kuyruğunda paket alır.
3. **`SURUM_KARAR_BEKLEYEN` cırcırı P-02'nin taban tazeleme turunda** `ham/olcum-<tarih>.json`'a da yazılmalı (BK-07'nin "tabanlar sürümlenir" maddesi P-02'nin işidir).
4. **P-35 kapanış mührü** R-9'un ikinci yarısıdır; bu pakette kapatılmadı (plan gereği).

## § 12 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

- **Kurucuya:** § 7 tablosu bu paketle birlikte iletilir. **K-20 (iCloud) ve K-5 (sır rotasyonu) ihtiyaç tarihleri GEÇTİ** — DALGA 1 bunlar olmadan da başlar ama yayın Mehmet tarafından fail-closed durdurulur. Ayrıca § 8.5: `docker-compose.yml` healthcheck satırındaki parola rotasyona dâhil edilmeli.
- **Mali müşavire:** bu pakette mevzuat hükmü koda yazılmadı; E-BELGE KAPSAM-DIŞI mührü bir **mevzuat kararı değil, kaynak/sözleşme kararıdır** (K-2). KDV 23/e ve KMTS teyidi K-10'da beklemeye devam ediyor.
