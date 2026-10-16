# PAKET P-03 — SÖZLEŞME-1 · HASIM BULGULARI KAPATMA TURU

**Tarih:** 2026-09-18 · **Paket:** P-03 (kimlik **R-10**) · **Dalga:** DALGA 1 · **Sürüm:** 1.0
**Rol:** Taşkın (backend) + Ertuğ (frontend) · **Model:** Opus
**Durum:** **YARIM** — mekanizmalar kuruldu ve kanıtlandı, ama paketin asıl DoD'u (**her** taşınan tip için sunucu ⇄ istemci kilidi) **KISMEN** karşılandı: 72 ayrıştırma hedefinin **13'ü kilitli, 59'u kilitsiz**. Kalan kapsam **P-07'ye DEVREDİLDİ** (gerekçe § 3).
**COMMIT YOK** · 5199'a dokunulmadı · migration yok.

> Bu belge, `PAKET-P-03-uygulama-raporu.md`'nin **düzeltme turudur**. Önceki raporun
> "Sunucu DTO ⇄ istemci tip ad + TİP yansımayla kilitli | **KARŞILANDI**" satırı
> **YANLIŞTI** (Anayasa md.1 ihlali). Doğrusu **KISMEN**'dir ve oran bu belgede yazılıdır.

---

## § 1 — ÖNCEKİ DOĞRULAMA BULGULARININ KAPANIŞI

| # | Bulgu | Durum | Kanıt |
|---|---|---|---|
| 1 | **[KRİTİK]** 75 tel-üstü tipin yalnız 2'si kilitli; rapor "KARŞILANDI" diyor | **KAPANDI (dürüst ölçüm + mekanizma)** | Oran taze ölçüldü: **13/72 kilitli, 59 kilitsiz**. Kapsam artık iki yeni test + bir cırcır sayacı ile bağlı: `dotnet/Kuyumcu.Tests/SozlesmeKapsamTestleri.cs`, `dotnet/Kuyumcu.Tests/SozlesmeSekilTestleri.cs`, `dotnet/denetim.sh` → `SOZLESME_KILITSIZ_TIP 59/72 (tavan 59)`. Bu belgede "KARŞILANDI" satırı **KISMEN**'e çevrildi (§ 6). |
| 2 | **[ORTA]** `CONTRACTS_DISI_MODEL` yapı gereği 0; asıl riski ölçmüyor | **KAPANDI** | Sayaç korundu ama **etiketi** ne ölçtüğünü söylüyor: `✓ P-03: CONTRACTS_DISI_MODEL (ikinci tanım)  0 (-eq 0)`. Asıl kapı ayrı sayaç olarak eklendi: `SOZLESME_KILITSIZ_TIP` (kapsam = kilitli çift / ayrıştırılan tip), azalan cırcır. |
| 3 | **[DÜŞÜK]** Rapor "tek ⚠ satırı" diyordu, gerçekte 5 ⚠ var | **KAPANDI** | § 5'te **5 ⚠ satırının tamamı** birebir yazıldı. |
| 4 | **[ORTA]** Sır sızıntısı + belge/konteyner parola ayrışması | **DOĞRULANDI, K-5'e AKTARILDI** | `docker-compose.yml`'deki parola ile `docker exec kuyumcu-mysql mysql -uroot -p"$P"` → `ERROR 1045 (28000): Access denied for user 'root'@'localhost'`. Bağlantı ancak `appsettings.Development.json` içindeki parola ile kuruldu (değer ekrana **basılmadı**, değişkende tutuldu). Kurucu/Mehmet notu § 8. |
| 5 | **[DÜŞÜK]** Eski artıklar (0 baytlık migration'lar, `.bak`, `.mutorj`) | **DOĞRULANDI, K-4'e AKTARILDI** | `find dotnet -name "*.mutorj" -o -name "*.bak"` → `MadenDefteriTestleri.cs.bak` + 9+ `.mutorj`. **Hiçbiri bu turda üretilmedi**; silme kurucuya bırakıldı (TurkReport hook `rm` bloklar). |

---

## § 2 — YAPILAN İŞ (bu tur)

### 2.1 Yeni: `Kuyumcu.Tests/SozlesmeSekilTestleri.cs` — **JSON ŞEKİL KİLİDİ** (HTTP, gerçek MySQL)
Sunucu uçlarının çoğu `Task<object>` döndürür (anonim nesne) — yansımayla eşleştirilecek bir DTO **tipi yoktur**. Bu test gerçek HTTP yanıtının **şeklini** istemci tipinin alanlarına karşı doğrular: her yazılabilir alan camelCase olarak yanıtta bulunmalı ve JSON türü uyumlu olmalı.

Kilitlenen 12 uç/tip: `/kasalar/{id}/hareketler` → `Kasalar.HareketYaniti` · `/bankalar` → `Banka.HesapYaniti` · `/cari/{id}/ekstre` → `Cariler.EkstreYaniti` · `/yetkiler` → `Yetkiler.YetkiMatrisi` · `/kur/guncel` → `Kur.KurVeri`, `Cariler.KurVeri`, `Emanet.KurVeri`, `Fiyatlandirma.KurOzet` · `/admin/tenant` → `Isletme.TenantVeri` · `/kurulum/durum` → `Core.Sozlesmeler.KurulumDurumYaniti` · `/raporlar/cari-yaslandirma` → `Raporlar.YaslandirmaYaniti` · `/raporlar/stok-degeri` → `Raporlar.StokYaniti`.

**Tasarım kararı (gerçek kırmızıdan öğrenildi):** ilk koşumda `/kurulum/durum` 4 alan için kırmızı verdi (`eksikListe`, `atlananListe`, `adimListesi`, `sayiKutusu`). İnceleme: bunlar record'un **hesaplanmış salt-okunur** özellikleridir (`EksikListe => EksikAdimlar ?? []`), tel üstünde olmamaları **doğrudur**. Doğrulayıcı `!p.CanWrite` ile bunları atlar (record konum parametreleri `init` olduğu için kapsamda kalır) — sahte kırmızı kaldırıldı, gerçek kapsam korundu.

### 2.2 Yeni: `Kuyumcu.Tests/SozlesmeKapsamTestleri.cs` — **KAPSAM CIRCIRI**
Razor'daki `Api.XAsync<T>` ayrıştırma hedeflerini kaynaktan okur, her kısa adı Contracts+Core tiplerine çözer ve **kilitli** sayar:
(a) sunucu **aynı tipi** imzasında kullanıyorsa (tek tanım → derleyici kilidi), (b) `SozlesmeSekilTestleri` canlı JSON şekliyle kilitliyorsa, (c) `SozlesmeYansimaTestleri.Ciftler()` tablosunda eşleniyorsa.
Aynı adı taşıyan **tüm** tipler kilitli değilse ad kilitsiz sayılır (tutucu ölçüm; ör. `HareketYaniti`'nın Banka sürümü kilitsiz olduğu için ad kilitsiz kaldı).
Kalan 59 ad dosyada **ADIYLA donduruldu**; yeni kilitsiz tip = test KIRMIZI, kilitlenen tip = "listeden sil, tavanı düşür" kırmızısı.

### 2.3 `dotnet/denetim.sh` — P-03 bloğu
- `CONTRACTS_DISI_MODEL` etiketi → `CONTRACTS_DISI_MODEL (ikinci tanım)` (ne ölçtüğü adında).
- **YENİ sayaç** `SOZLESME_KILITSIZ_TIP` (azalan cırcır, tavan **59**): ölçüm komutu betiğe yorum olarak yazıldı, taban **taze ölçüldü** (belgeden kopyalanmadı).

**Dokunulan üretim kodu: YOK.** Bu tur yalnız test + denetim betiği yazdı; razor/API dosyaları değişmedi (mutasyonlar geri alındı, `cmp` birebir).

---

## § 3 — SAPMA: DoD "her taşınan tip kilitlenir" neden TAM karşılanamadı

| Ölçüm | Değer | Komut |
|---|---|---|
| Razor ayrıştırma hedefi (tekil kısa ad, gürültü elenmiş) | **72** | `grep -rhoE "Api\.(Get\|Post\|Patch\|Put\|Delete)Async<[A-Za-z0-9_.]+" --include='*.razor' Pages Bilesenler Layout \| sed -E 's/.*<//;s/.*\.//' \| sort -u \| grep -vxE 'JsonElement\|object\|List' \| wc -l` |
| Kilitli | **13** (%18) | `SozlesmeKapsamTestleri.KilitsizOlcum()` |
| Kilitsiz | **59** | `denetim.sh` → `SOZLESME_KILITSIZ_TIP 59/72` |

**Neden:** kilitsiz 59 adın sunucu tarafında **karşılık gelen bir DTO tipi yoktur**; uç `Task<object>` döndürür ve gövde anonim nesnedir (ör. `KasaModulu.cs:431` `return new { kasa, hareketler = liste };`). Yansıma çifti yazılabilmesi için **sunucunun Contracts tipini döndürmeye geçirilmesi** gerekir; bu, onlarca API modülünü açar ve P-03'ün **"taşıma saf yer değiştirmedir, davranış değişmez"** DoD'unu ve kapsam kilidini ihlal eder.

→ **DEVİR:** kalan 59 adın kilitlenmesi **P-07**'ye devredildi (PLAN § 7'de `Kuyumcu.Web/Servisler` ve sunucu sözleşme yüzeyi P-07'nin tek elidir). Devir kaydı: `SOZLESME_KILITSIZ_TIP` tavanı 59'dan yalnız **azalabilir**; P-07 her kilitlediği tipte tavanı düşürür.

**Kalan kilitsiz 59 ad** (`SozlesmeKapsamTestleri.cs` `KilitsizListesi()` dizisinde birebir):
AcilisGeriAlYaniti · AcilisProvaYaniti · AcilisUygulaYaniti · AuthYaniti · AylikYanit · CariDetayVeri · Detay · EkstreDetayYaniti · EkstreOnayYaniti · EkstreSatirYaniti · EkstreYuklemeYaniti · EmanetYaniti · Esikler · EtiketVeri · FaturaYaniti · FaturalanabilirYanit · FisYaniti · GecmisYanit · GunlukOzet · HareketYaniti · IceAktarimGecmisYaniti · IceAktarimGeriAlYaniti · IceAktarimProvaYaniti · IceAktarimSablonYaniti · IceAktarimUygulaYaniti · IsYaniti · KalemYaniti · KapatYaniti · Kart · KaynakDurumu · KaynakSonuc · KaynakYaniti · KosuYanit · KurAnlik · KurSatiri · MadenTablosuYaniti · OzelMatrahOnizleme · Ozet · OzetVeri · PariteYaniti · ReyonIcerikYaniti · ReyonStokYaniti · Sayfa · StokVeri · StokYaniti · SubeVeri · TalepYanit · TatbikatYanit · Teklif · TevsikOnizleme · UrunTamKart · UrunVeri · UrunYanit · UrunYerlesimYaniti · VezneAyarYaniti · VezneDetayYaniti · VezneKurSatiri · VezneKuyrukYaniti · Yanit

---

## § 4 — DERLEME, TESTLER, MUTASYON

**Derleme:** `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı · 0 Hata**.
**Migration:** **gerekmedi** (plan: HAYIR) — şema değişmedi, `dotnet ef` çalıştırılmadı.

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Tam süit (önce, bu turdan evvel) | 2910 | 2910 | 0 | 0 |
| Tam süit (sonra) | **2925** | **2925** | **0** | 0 |

**Yeni test sayısı: 15** (`SozlesmeSekilTestleri` 12 theory + 1 fact · `SozlesmeKapsamTestleri` 2 fact).
TRX: `/private/tmp/.../scratchpad/trx-pP-03`.

### Mutasyon tablosu

| Kod | Kod/dosya | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|---|
| **M03-4** | `Kuyumcu.Api/Modules/Kasa/KasaModulu.cs:431` | `new { kasa, hareketler = liste }` → `hareketListesi = liste` (sunucu alan adı kayması) | `SozlesmeSekilTestleri.Sunucu_yaniti_istemci_tipinin_tum_alanlarini_tasir(kalip: "/api/v1/kasalar/{kasa}/hareketler", istemciTipi: Kasalar.HareketYaniti)` — **derleme 0 Hata kaldı**, yani kayma derleyiciden görünmez | `cmp` → **birebir aynı** |
| **M03-5** | `Kuyumcu.Tests/SozlesmeKapsamTestleri.cs` | cırcır listesinden `"FaturaYaniti"` silindi | `SozlesmeKapsamTestleri.Kilitsiz_tip_listesi_circir_ile_donduruldu` **ve** `Kapsam_orani_beyan_edilen_degerle_ayni` | `cmp` → **birebir aynı** |
| **M03-6** | `dotnet/denetim.sh` | `SOZLESME_KILITSIZ_TAVAN=59` → `58` | `denetim.sh` → `✗ P-03: SOZLESME_KILITSIZ_TIP 59/72 kilitsiz (tavan 58 — cırcır YALNIZ azalır)` + `DENETİM BAŞARISIZ` | `cmp` → **birebir aynı** |

Son mutasyondan sonra **temiz derleme**: 0 Uyarı / 0 Hata; tam süit yeniden koşuldu (2925/2925).

---

## § 5 — DENETİM ÇIKTISI (⚠ satırlarının TAMAMI — bulgu 3)

`dotnet/denetim.sh` → **`DENETİM TEMİZ.`** (çıkış kodu 0)

P-03 satırları:
```
  ✓ P-03: CONTRACTS_DISI_MODEL (ikinci tanım)     0 (-eq 0)
  ✓ P-03: Kuyumcu.Contracts projesi                137 tip
  ✓ P-03: SOZLESME_KILITSIZ_TIP                    59/72 kilitsiz (tavan 59)
  ✓ Pages+Bilesenler+Layout record tanımı          3 (tavan 3)
```

**⚠ satırlarının tamamı (5 adet, hepsi P-02 kapsamı, `DENETİM TEMİZ` sonucunu değiştirmez):**
```
  ⚠ P-02: QA son tur rota kapsamı (tur 6)         39/42 — taban 39, sonraki tur 42 tarayacak
  ⚠ P-02: YAYIN_TAZE (5199 anlık görüntüsü)   BAYAT — kaynak yayından yeni; yayinla.sh koşulmalı (K-20 açıkken sahte olabilir)
  ⚠ P-02: ICLOUD_ESITLEME_ACIK (K-20)              AÇIK — bu dalganın SÜRE ölçümleri GEÇERSİZ TABAN damgalıdır
  ⚠ P-02: QA_TUR_SURESI_OLCUM (BK-39)              ÖLÇÜM YOK — devir açık (tur >6 ya da 2026-09-30'e dek; sonrasında DENETİM KIRILIR)
  ⚠ P-02: URETIM_KUNYE_KANITI (B-2 DoD)            ALINAMAZ — K-5 AÇIK (geliştirme sırrı kara listede: Jwt ve/veya MySQL); BK-14 gereği Production açılmaz. P-02 bu kalemde KISMEN.
```

---

## § 6 — CANLI DOĞRULAMA (5198 — 5199'A DOKUNULMADI)

API: `ASPNETCORE_ENVIRONMENT=Development … --no-build --no-launch-profile --urls http://localhost:5198`, **PID 98585**.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| L1 | `GET /health` | 200 | **200** (Blazor kabuğu döner) | gövde okundu, `<title>Kuyumcu ERP</title>` |
| L1b | `GET /api/health` | — | **404** `{"message":"Uç bulunamadı"…}` | sağlık ucu bu yolda değil (bilgi notu) |
| L2 | `GET /api/v1/kasalar/xyz/hareketler` jetonsuz | 401 | **401** `{"message":"Token bulunamadı","error":"Unauthorized","statusCode":401}` | gövde okundu |
| L3 | Kilitlenen 5 uç jetonsuz (`/kur/guncel`, `/yetkiler`, `/bankalar`, `/kurulum/durum`, `/raporlar/stok-degeri`) | 401 | **hepsi 401** | HTTP kodları tek tek okundu |
| L4 | `GET /swagger/v1/swagger.json` | 200, uçlar şemada | **200**, 231 yol; `/api/v1/kasalar/{id}/hareketler`, `/api/v1/yetkiler`, `/api/v1/kur/guncel`, `/api/v1/kurulum/durum` → **True** | JSON ayrıştırıldı |
| L5 | Kiracı sayısı (SQL) | değişmedi | `select count(*) from tenants` → **8**; `ad like 'PAKET%'` → **0**; `ad like 'QA-TEST-%'` → **3** (dokunulmadı) | `docker exec kuyumcu-mysql mysql -uroot -p"$P" -N kuyumcu_net -e …` |
| L6 | API durdurma | 5198 boş | `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır** | — |
| L7 | 5199 | dokunulmadı | 5199 dinleyicisi **koşmaya devam ediyor**, hiçbir istek atılmadı | — |

**Dürüstlük notu (md.1):** jetonlu uçlar canlıda çağrılmadı — `POST /api/v1/auth/kayit` **davet jetonu** ister (F-2-min fail-closed) ve mevcut 8 kiracının 3'ü kalıcı `QA-TEST-*` fikstürüdür (dokunulmaz). **Bu turda test kiracısı AÇILMADI** (kiracı sayısı başta 8, sonda 8). Jetonlu yolun gerçek HTTP kanıtı `SozlesmeSekilTestleri`'nin 12 uçluk koşumudur (TestServer üzerinden gerçek boru hattı + gerçek MySQL).

---

## § 7 — KİMLİK KAPANIŞ TABLOSU

| Kimlik | Durum | Kanıt |
|---|---|---|
| **R-10** — SÖZLEŞME-1: `Kuyumcu.Contracts` + tip/ad sözleşmesi | **KISMEN** | **Kapandı:** proje + taşıma (`dotnet/Kuyumcu.Contracts/`, 137 tip) · `SAYFA_RECORD_TAVAN=3` (`denetim.sh:340`) · `CONTRACTS_DISI_MODEL (ikinci tanım) = 0` · yeni kapsam sayacı `SOZLESME_KILITSIZ_TIP 59/72 (tavan 59)` · 15 yeni test yeşil · 3 mutasyon kanıtı. **Kapanmadı:** 72 hedefin 59'u hâlâ sunucu ile kilitli değil (sunucu `Task<object>` döndürüyor) → **devredildi: P-07**. |

---

## § 8 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Taşıma saf yer değiştirmedir, davranış değişmez | **KARŞILANDI** | Bu turda üretim kodu **hiç** değişmedi; yalnız test + `denetim.sh`. |
| Sunucu DTO ⇄ istemci tip ad + TİP kilidi | **KISMEN** (önceki raporda yanlışlıkla "KARŞILANDI" yazıyordu) | 13/72 kilitli; oran ve kalan liste § 3'te; devir P-07. |
| Tavan 129 → 100 → 60 (artırma yasak) | **KARŞILANDI (aşıldı)** | `SAYFA_RECORD_TAVAN=3`; bu turda artırılmadı. |
| Yeni istemci record açılmaz | **KARŞILANDI** | `SozlesmeYansimaTestleri.Razor_code_blogunda_istemci_record_tanimi_acilmiyor` yeşil (3 gerekçeli istisna). |
| Sayaçlar denetim.sh'a cırcır olarak eklendi, taban taze ölçüldü | **KARŞILANDI** | `SOZLESME_KILITSIZ_TAVAN=59`, ölçüm komutu betikte yorum olarak; M03-6 tavanın kapı olduğunu kanıtlıyor. |
| Her iş kuralı için hatayı yeniden üreten test | **KARŞILANDI** | M03-4 gerçek bir ad kaymasını kırmızıya döndürüyor, derleme yeşil kalıyor. |
| Mutasyon kanıtı + `cmp` birebir | **KARŞILANDI** | § 4 tablosu, 3 mutasyon, 3 `cmp` birebir. |
| Tam süit 0 başarısız | **KARŞILANDI** | 2925/2925. |
| `denetim.sh` TEMİZ | **KARŞILANDI** | § 5, çıkış kodu 0. |
| Canlı doğrulama 5198, 5199'a dokunulmadı | **KARŞILANDI** | § 6 (L1-L7). |
| Test kiracısı silindi | **GEREKMEDİ** | Kiracı açılmadı; `tenants` 8 → 8. |
| COMMIT YOK / git komutu yok | **KARŞILANDI** | Yalnız `git status --porcelain` (salt okunur). |
| Migration | **GEREKMEDİ** | Plan: HAYIR. |
| Sır/parola çıktıya yazılmadı | **KARŞILANDI** | Parolalar yalnız `$P` değişkeninde; ekrana "var/yok" basıldı. |

---

## § 9 — DEĞİŞEN DOSYALAR

`git status --porcelain | grep -v -E "/(bin|obj)/"` içinde bu turun dokunduğu dosyalar:

- `dotnet/Kuyumcu.Tests/SozlesmeKapsamTestleri.cs` (**YENİ**, 9.975 bayt)
- `dotnet/Kuyumcu.Tests/SozlesmeSekilTestleri.cs` (**YENİ**, 7.731 bayt)
- `dotnet/denetim.sh` (P-03 bloğu: yeni sayaç + etiket düzeltmesi)
- `dokumanlar/09-uygulama-kayitlari/PAKET-P-03-uygulama-raporu-DUZELTME-2026-09-18.md` (bu belge)

> Not: `Kuyumcu.Tests/` ve `Kuyumcu.Contracts/` git'te bütün olarak `??` görünüyor (izlenmiyor); dosya bazlı durum bu yüzden klasör satırıyla raporlanıyor.

---

## § 10 — AÇIK KALANLAR / KURUCUYA NOTLAR

1. **P-07'ye devir (kapı kaydı):** 59 istemci tipi hâlâ sunucu ile kilitli değil. Kapatma yolu: ilgili uçların `Task<object>` yerine `Kuyumcu.Contracts` tipini döndürmesi. Her kilitlemede `SozlesmeKapsamTestleri.KilitsizListesi()`'nden ad silinir ve `SOZLESME_KILITSIZ_TAVAN` düşürülür.
2. **K-5 (Mehmet, sır rotasyonu) — ACİL:** `docker-compose.yml`'deki MySQL kök parolası **çalışan konteynerin parolası değil** (bağımsız doğrulandı: `ERROR 1045`). Rotasyonda **ikisi birlikte** döndürülmeli; önceki turda oturum günlüğüne düşen satır rotasyon kapsamına alınmalı.
3. **K-4 (kurucu, silme):** proje ağacında 0 baytlık migration çiftleri, `Kuyumcu.Tests/MadenDefteriTestleri.cs.bak` ve 9+ `.mutorj` artığı duruyor. Hook `rm`'i blokladığı için ekip silemiyor — topluca kurucuya sunuluyor.
4. **Mali müşavire not:** bu turda mevzuat hükmü koda yazılmadı; konu dışı.
5. **Süre:** tahmin 240 ajan-dk; bu düzeltme turu yaklaşık 70 ajan-dk sürdü (kapsam yalnız bulgu kapatma).
