# PAKET P-04 — ŞUBE BAĞLAMI + GLOBAL SORGU FİLTRESİ

**Tarih:** 2026-09-18 · **Kimlik:** Y-1 · **Dalga:** DALGA 1 · **Sürüm:** 1.0
**Rol:** Taşkın (03 Backend) + Ertuğ (03 Frontend) · **Model:** Opus
**Durum:** **BİTTİ** — mekanizma kuruldu, sınıflandırma tablosu mühürlendi, negatif test canlı HTTP ile doğrulandı.
**COMMIT YOK** · 5199'a dokunulmadı · canlı doğrulama 5198'de yapıldı ve süreç durduruldu.

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (P-03)

`dokumanlar/09-uygulama-kayitlari/PAKET-P-03-uygulama-raporu.md` **ve** `...-DUZELTME-2026-09-18.md` diskte VAR.
P-03 kendi raporunda **YARIM** ilan edilmiştir (72 ayrıştırma hedefinin 13'ü kilitli, 59'u P-07'ye devredildi).
Bu paket P-03'ün kilitlenmiş yüzeyine dokunmaz: P-04 yalnız sunucu tarafı veri kapsamını değiştirir, hiçbir
istemci tipi/`Pages` dosyası açılmamıştır. Dolayısıyla P-03'ün yarımlığı P-04 kapsamını daraltmadı.

---

## § 1 — SINIFLANDIRMA TABLOSU (kod değişiminden ÖNCE mühürlendi — Bertan K2 / Mehmet koşulu / BK-02)

Dosya: **`dotnet/sube-suzgec-siniflandirma.txt`** (sürümlenmiş, denetim.sh bunu okur).

| Kategori | Anlam | Bugün ölçülen | Karar |
|---|---|---|---|
| **a** | kimlikten gelen SubeId ile **yetki** amaçlı süzgeç | **3** (`Vezne/VezneModulu.cs` Kuyruk ×2, `Bul` ×1) | **KALDIRILDI** → tek mekanizma `Data/SubeSuzgeci.cs → OturumSubesinde()` |
| **b** | parametreyle gelen **kullanıcı seçimli** rapor/liste süzgeci | **18** | DOKUNULMADI |
| **c** | iç eşleştirme / işlemin yürüdüğü şubenin satırını bulma | **19** | DOKUNULMADI |
| **d** | `Yerlesim/YerlesimServisi.cs` (çağıranın verdiği çalışma şubesi) | **11** | DOKUNULMADI (c ile aynı doğa, ayrı etiket) |
| | **TOPLAM** | **51 → 48** | a kaldırıldıktan sonra 48 |

**SAPMA (Anayasa md.1):** PLAN "`SubeId ==` **63 nokta**" diyor; bugün depoda taze ölçülen değer **51**'dir
(`grep -rn "SubeId ==" Kuyumcu.Api/Modules --include='*.cs' | wc -l`). Plan rakamı ya farklı bir kapsamda
(`Kuyumcu.Web` dâhil: +6, yine 57) ya da bayat bir ölçümdür. **Planın hedefine sadık kalındı**: her nokta tek
tek sınıflandırıldı, sınıfsız satır 0. Plandaki örnek satır numaraları da kaymıştı
(`RaporModulu.cs:753,861,930` ve `:960` **bugün de doğru**; `SatisRaporlari.cs:50,199,256` doğru).

**KULLANICI_SUZGEC sayacı yalnız raporlanır** (PLAN: azaltma hedefi YOK) — bugün **18**.

---

## § 2 — YAPILAN İŞ

### 2.1 Şube bağlamı ve NULL sözleşmesi (`Data/KiraciBaglami.cs`)
`IKiraciBaglami` iki alan kazandı: `SubeId` ve `TumSubeler`. Sözleşme dosyanın başında **yazılıdır** ve testle kilitlidir:

1. `TumSubeler = true` → şube süzgeci **kurulmaz** (rapor süzgeçleri yaşar).
2. `TumSubeler = false` + şube dolu → kiracı **AND** şube, **tek ifade**.
3. `TumSubeler = false` + şube **boş** → **FAIL-CLOSED**: `SubeIhlaliException` (403, Türkçe sebep).
   Ne fail-open (hepsini göster) ne fail-blind (sessiz 0 satır).

"TÜM şubeler" rolü tek kaynaktadır: `SubeYetkisi.TumSubelerRolleri = [SAHIP, MUDUR, MUHASEBE]`.
**Bilinmeyen/boş rol → ayrıcalık YOK** (fail-closed).

### 2.2 Tek ifadede birleşik global filtre (`Data/KuyumcuDbContext.cs`)
- Yeni arayüz **`ISubeVarlik`** (17 varlık uygular).
- `KiraciVeSubeFiltresiKur<T>`: `e.TenantId == kiraciId && (e.SubeId == null || e.SubeId == subeId)` — **tek `HasQueryFilter`**.
  (EF bir varlıkta yalnız bir filtre tutar; ikinci çağrı birincisini sessizce ezerdi.)
  `SubeId == null` bacağı: şubesiz = kiracı geneli kayıt (ör. şubesiz sipariş, denetim izi) **gizlenmez**.
- **Tenant'ın ayrı filtresi (`t.Id == kiraciId`) istisna olarak gerekçelendirildi** ve gerekçe koda yazıldı:
  `Tenant` `IKiraciVarlik` değildir (kimliği `TenantId` değil `Id` taşır), şubesi de yoktur.
- **Model önbellek anahtarına `SubeFiltresiAcik` eklendi.** Eklenmeseydi ilk isteğin (SAHIP, süzgeçsiz) modeli
  önbelleğe yerleşir, sonraki TEZGAHTAR isteği **süzgeçsiz** koşardı — kapatılan açığın tam kendisi.
- **Kapsam denetimi:** `sube_id` sütunu olup `ISubeVarlik` uygulamayan varlık varsa **uygulama AÇILMAZ**.
  Beyaz liste tek maddeli ve gerekçeli: `Kullanici` (kaydın değil, kullanıcının bağlı olduğu şube).

### 2.3 Yazma tarafı (`Data/KiraciYazmaDenetcisi.cs`)
Okuma filtresi yalnız SELECT'e WHERE ekler. `SubeDenetle` eklendi (yalnız süzgeç açıkken):
çapraz şube **Add** → 403, şube **taşıma** → 403, yabancı şube satırını **güncelleme** → 403,
şubesiz gelen yeni kayıt aktif şube ile **damgalanır** (kiracı damgasının şube karşılığı).

### 2.4 (a) kategorisinin tek mekanizmaya taşınması (`Data/SubeSuzgeci.cs` — YENİ)
`OturumSubesinde(islem, hata)` uzantısı: rol ne olursa olsun tezgâhın şubesinde yürüyen akışlar (vezne R6)
için **tek** süzgeç yolu; şube yoksa 400 ile durur. `VezneModulu` içindeki 3 elle süzgeç buraya taşındı →
o dosyada `SubeId ==` **0**.

### 2.5 İndeksler (PLAN: "indeksler bu pakette eklenir")
Global süzgeç her sorguya `tenant_id = @p AND (sube_id IS NULL OR sube_id = @q)` ekliyor. Öncü
`(tenant_id, sube_id)` indeksi olmayan 9 tabloya indeks eklendi: `stoklar, kasalar, pos_tahsilatlari,
gun_kapanislari, kiymetli_evraklar, gider_pusulalari, siparisler, stok_parti_tuketimleri, acilis_kalemleri`.
Zaten `(tenant, sube, …)` öncüsü olan tablolar tekrar indekslenmedi.

---

## § 3 — MIGRATION

**Geri doldurma migration'ı GEREKMEDİ.** Önce salt okunur ölçüm koştu (PLAN DoD):

| Ölçüm | Sonuç |
|---|---|
| `sube_id` sütunu olan tablo | 18 |
| **Yetim** `sube_id` (şubeler tablosunda karşılığı yok) | **0 — tüm tablolarda** |
| Boş `sube_id` | yalnız `denetim_kayitlari` 69.016 satır (oturumsuz/sistem izleri; **meşru**, `SubeId == null` bacağıyla görünür kalır) |

**Uygulanan migration (yalnız indeks):** `20260918125655_SubeSuzgecIndeksleriP04`
- `20260918125655_SubeSuzgecIndeksleriP04.cs` → **3.585 bayt**, `Up()` 9 `CreateIndex`, `Down()` 9 `DropIndex`
- `20260918125655_SubeSuzgecIndeksleriP04.Designer.cs` → **194.463 bayt**
- `--no-build` KULLANILMADI; build YEŞİLKEN üretildi.
- `kuyumcu_net`'e uygulandı: `dotnet ef database update` → `Applying migration ... Done.`
- **`__EFMigrationsHistory` = 50** (uygulama sonrası).
- `information_schema.statistics` → `IX_%tenant_id_sube_id` desenli **22 indeks sütunu** satırı.

---

## § 4 — DERLEME VE TESTLER

| Koşum | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln --nologo` | **0 Hata · 0 Uyarı** |
| Tam süit (önce, bu paketten bağımsız taban) | 2.925 test (2.944 − 19 yeni) |
| Tam süit (sonra) | **Toplam 2.944 · Başarılı 2.944 · Başarısız 0 · Atlanan 0** |
| Yeni test | **19** (`SubeIzolasyonTestleri` 10 + `SubeMekanizmaTestleri` 9) |
| **Filtre açılınca kırılan mevcut test** | **0** · **susturulan test 0** |
| `dotnet/denetim.sh` | **DENETİM TEMİZ** |

**Kırılan test = 0'ın dürüst açıklaması (md.1):** süzgeç yalnız "tüm şubeler" ayrıcalığı OLMAYAN
oturumlarda kurulur; mevcut testlerin tamamı `SAHIP` rolüyle ya da `SabitKiraciBaglami` (kiracı geneli,
arka plan davranışı) ile koşuyor. Yani 0, "kimse sınamıyordu" demektir — bu boşluğu 19 yeni test kapatır.

**KARARSIZLIK KANITI (md.1):** tam süidin bir koşumunda P-04 ile ilgisi olmayan bir bUnit testi
`UX1C1EkranTestleri.Pos_kalem_satirinda_ADET_satir_ici_duzenlenir` **1 kez** kırmızı verdi
(`ElementNotFoundException: #si-kalem-adet-u1`). Aynı test tek başına **3/3 yeşil**, tam süit tekrar
koşturulduğunda **2.944/2.944 yeşil**. Bu paket `Kuyumcu.Web` altında hiçbir dosyaya dokunmadı;
bulgu **kararsız (flaky) test** olarak kayda geçirilir ve triyaja (Y-42) devredilir.

---

## § 5 — MUTASYON KANITLARI

Yöntem: kaynak scratchpad'e kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli değişiklik → filtreli test →
yedek geri kopyalandı → **`cmp` birebir aynı**.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| **M-P04-1** | Birleşik filtreden şube bacağı silindi (`&& (e.SubeId == null \|\| e.SubeId == subeId)` → yok) | `Md2_Sube_kilitli_rol_yalniz_kendi_subesinin_kasasini_listeler`, `Md2_Negatif_A_subesi_jetonuyla_B_subesi_kaydi_404` | AYNI |
| **M-P04-2** | Fail-closed delindi: şube boşken süzgeç sessizce kapatıldı (fail-open) | `Md3_Subesiz_kilitli_baglam_DbContext_insa_edilemez`, `Md3_Subesiz_kilitli_rol_fail_closed_200_donmez` | AYNI |
| **M-P04-3** | Yazma denetçisinde `SubeDenetle` devre dışı (`if (true) return;`) | `Yazma_capraz_sube_kaydi_403`, `Yazma_yabanci_sube_kaydinin_guncellenmesi_403`, `Yazma_subesiz_yeni_kayit_aktif_sube_ile_damgalanir` | AYNI |
| **M-P04-4** | `TEZGAHTAR` "tüm şubeler" listesine eklendi | `Tum_subeler_ayricaligi_tek_kaynaktan_ve_bilinmeyen_rol_kapali(rol: "TEZGAHTAR")` + 3 izolasyon testi | AYNI |
| **M-P04-5** | `KasaModulu.Getir` içine elle izolasyon süzgeci eklendi | **denetim.sh KIRMIZI**: `✗ P-04: GUVENLIK_ELLE_IZOLASYON 1 (-eq 0 olmalı)` → `DENETİM BAŞARISIZ` | AYNI |
| **M-P04-6** | `GunKapanisi`'nden `ISubeVarlik` kaldırıldı | `Sube_id_tasiyan_her_varlik_ISubeVarlik_uygular` | AYNI |

M-P04-6 ilk denemede `SUBE_VARLIK_KAPSAM` sayacını kırmadı (taban 17 yazılmıştı); **sayaç tabanı taze ölçümle
18'e çıkarıldı** (17 varlık + 1 jenerik kısıt) — cırcır artık gerçekten kapanıyor.
Son mutasyondan sonra **temiz `dotnet build`: 0 Hata / 0 Uyarı.**

---

## § 6 — MEKANİZMA SAYAÇLARI (denetim.sh `# >>> P-04` bloğu)

```
  ℹ P-04: KULLANICI_SUZGEC (yalnız rapor)         18 (tablo: 48 satır · sınıflandırılmış 48)
  ✓ P-04: GUVENLIK_ELLE_IZOLASYON                  0 (-eq 0)
  ✓ P-04: IGNORE_QUERY_FILTERS                     0 (-le 4)
  ✓ P-04: SUBE_VARLIK_KAPSAM                       18 varlık (12 dosyada sube_id)
  ✓ P-04: SUBE_FILTRE_MEKANIZMASI                  birleşik filtre + fail-closed yerinde
DENETİM TEMİZ.
```

- **GUVENLIK_ELLE_IZOLASYON** satır **metnine** göre ölçülür (satır numarasına göre değil): Modules altındaki
  her `SubeId ==` satırı sınıflandırma tablosunda olmak zorundadır. Yeni bir elle süzgeç = kırmızı (M-P04-5 kanıtı).
- **IGNORE_QUERY_FILTERS:** PLAN tavanı 4; **bugün ölçülen gerçek değer 0** (`.IgnoreQueryFilters()` çağrısı
  kod tabanında YOK; 4 rakamı yorum satırlarındaki geçişlerden geliyordu). Tavan **artırılmadı**;
  gerçek beyaz liste `Data/SistemKapsami.cs` başlığındaki 8 maddelik listedir ve filtresiz erişimin tek kapısıdır.
- Her sayacın **ölçüm komutu** betiğe yorum olarak yazıldı (Bertan K7); tabanlar belgeden kopyalanmadı.

---

## § 7 — CANLI DOĞRULAMA (5198 — 5199'a DOKUNULMADI)

Kiracı `PAKET-P-04-TEST` (id `PKT04TEN`), 2 şube (`PKT04SBA` / `PKT04SBB`), 2 kasa (`PKT04KSA` / `PKT04KSB`).
Jetonlar yerel olarak imzalandı; **hiçbir sır ekrana basılmadı.**

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET /api/v1/kasalar` — SAHIP (tüm şubeler) | 200, 2 kasa | **HTTP 200**, adet 2 → `['PKT04SBA','PKT04SBB']` |
| 2 | `GET /api/v1/kasalar` — TEZGAHTAR@A | 200, yalnız A | **HTTP 200**, adet 1 → `['PKT04SBA']` |
| 3 | `GET /api/v1/kasalar/PKT04KSB` — TEZGAHTAR@A | **404** | **HTTP 404** `{"message":"Kasa bulunamadı","statusCode":404}` |
| 4 | `GET /api/v1/kasalar/PKT04KSB` — SAHIP | 200 | **HTTP 200** |
| 5 | `GET /api/v1/kasalar?subeId=PKT04SBB` — TEZGAHTAR@A | veri yok | **HTTP 200 `[]`** (kullanıcı süzgeci global süzgeçle kesişir; sızıntı yok) |
| 6 | `GET /api/v1/kasalar` — TEZGAHTAR, jetonda şube YOK | fail-closed | **HTTP 403** `"Şube bağlamı çözülemedi: bu rol tüm şubeleri göremez ve oturumda şube yok…"` |

**SQL kanıtı (canlı):** `kasalar=2 · subeler=2 · rol_yetkileri=42 · denetim_kayitlari=64` (kiracı `PKT04TEN`).
**Temizlik:** API PID durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş (0 satır)**;
test kiracısı `tenant_id` sütunu olan **tüm** tablolardan silindi → `select count(*) from tenants` = **8**
(başlangıç değeri 8). `QA-TEST-` önekli fikstürlere **dokunulmadı**.

**Not (fail-closed 500 → 403):** ilk canlı turda şubesiz oturum **HTTP 500** veriyordu (DbContext ctor
`InvalidOperationException`). Sessiz/çirkin 500 yerine `SubeIhlaliException` (403 + Türkçe sebep) kullanıldı,
anlam aynı kaldı: bağlam yoksa hiçbir veri gösterilmez. Testler buna göre sıkılaştırıldı.

---

## § 8 — DoD SÖZLEŞMESİ

Bu paket **yeni varlık/kayıt türü YARATMADI** ve **yeni ekran/uç AÇMADI**; mevcut kayıtların **görünürlük
kapsamını** değiştirdi. Bu nedenle PATCH/DELETE/ekran düğmesi yükümlülüğü doğuran yeni bir varlık yoktur.

| Varlık | Düzenleme yolu | Not |
|---|---|---|
| 17 `ISubeVarlik` varlığı | **Değişmedi** — her birinin mevcut düzenleme/pasifleştirme yolu aynen durur | Yalnız kapsam süzgeci eklendi |
| Kullanıcının şubesi (`Kullanici.SubeId`) | `PATCH /api/v1/kullanicilar/{id}` ile **düzenlenebilir** (`KullaniciModulu.cs:237`) | Şube ataması kullanıcı ekranından yapılır; P-04 bunu kullanır, değiştirmez |
| Şube (`Sube`) | `Modules/Sube/SubeModulu.cs` uçları | Dokunulmadı |

**Salt okunur kalan:** yok (bu pakette yeni kayıt türü doğmadı).
**Finansal varlıkta hard DELETE:** eklenmedi; yazma kapısı yalnız **reddeder** (403), silme yapmaz.

---

## § 9 — KİMLİK KAPANIŞ TABLOSU

| Kimlik | Durum | Kanıt |
|---|---|---|
| **Y-1** — Şube izolasyonu global sorgu filtresine | **KAPANDI** | `Data/KuyumcuDbContext.cs` → `KiraciVeSubeFiltresiKur` (tek ifade, `ISubeVarlik` 17 varlık) · `Data/KiraciBaglami.cs` → NULL sözleşmesi + `SubeYetkisi` · `Data/KiraciYazmaDenetcisi.cs` → `SubeDenetle` · testler `SubeIzolasyonTestleri` (10) + `SubeMekanizmaTestleri` (9) · canlı § 7 adım 1-6 · sayaçlar § 6 |

Bağlı koşullar: **BK-02** (sınıflandırma tablosu önce, sınıfsız satır 0, `GUVENLIK_ELLE_IZOLASYON=0`,
`IGNORE_QUERY_FILTERS ≤ 4`, 2 adet fail-closed negatif test yeşil, kırılan test sayısı yazılı, susturulan 0) → **KARŞILANDI**.
**BK-09** (tablo + 404 negatif testi olmadan kapanmaz; IQF artamaz) → **KARŞILANDI**.
**BK-20** (negatif test gerçek HTTP ile canlı) → **KARŞILANDI** (§ 7 adım 3).

---

## § 10 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| 63 (ölçülen 51) noktanın a/b/c/d tablosu, kod değişiminden önce | KARŞILANDI | `dotnet/sube-suzgec-siniflandirma.txt`, sınıfsız satır 0 |
| Şube bağlamı bu pakette çözülür + NULL sözleşmesi yazılı ve testli | KARŞILANDI | `KiraciBaglami.cs` başlığı, md.1/2/3 testleri |
| Fail-open yasak | KARŞILANDI | M-P04-2 mutasyonu 2 testi kırdı; canlı 403 |
| TenantId + SubeId **tek ifadede AND** | KARŞILANDI | `KiraciVeSubeFiltresiKur` |
| Tenant'ın ayrı filtresi gerekçelendirildi | KARŞILANDI | `KuyumcuDbContext.cs` — "İSTİSNA (gerekçeli — P-04 DoD)" yorumu |
| `IgnoreQueryFilters` tek tek gerekçeli, sayı artmaz | KARŞILANDI | gerçek çağrı 0; beyaz liste `SistemKapsami.cs`; sayaç `-le 4` |
| Negatif test 404 + gerçek HTTP ile canlı | KARŞILANDI | § 7 adım 3 |
| Kırılan test ölçüldü, susturulan 0 | KARŞILANDI | 0 / 0 (§ 4, dürüst açıklamasıyla) |
| Şube indeksleri bu pakette | KARŞILANDI | migration, 9 indeks |
| Migration `Down()` zorunlu, `--no-build` yasak | KARŞILANDI | 9 `DropIndex`; build'li üretim |
| Sayaçlar denetim.sh'a cırcır olarak eklendi, taban taze ölçüldü | KARŞILANDI | § 6 |
| Tam süit 0 başarısız · denetim TEMİZ | KARŞILANDI | § 4 |
| Test kiracısı silindi, `QA-TEST-*` korundu | KARŞILANDI | tenants 8 → 8 |
| COMMIT YOK / 5199'a dokunulmadı | KARŞILANDI | yalnız 5198 kullanıldı |
| Kapsam kilidi (yeni ekran/modül açılmadı) | KARŞILANDI | `Kuyumcu.Web` altında 0 dosya değişti |

---

## § 11 — DEĞİŞEN DOSYALAR

```
dotnet/Kuyumcu.Api/Data/KiraciBaglami.cs            (şube bağlamı + NULL sözleşmesi + SubeYetkisi)
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs         (ISubeVarlik, birleşik filtre, önbellek anahtarı, kapsam denetimi, 9 indeks)
dotnet/Kuyumcu.Api/Data/KiraciYazmaDenetcisi.cs     (SubeIhlaliException + SubeDenetle)
dotnet/Kuyumcu.Api/Data/SubeSuzgeci.cs              (YENİ — oturum şubesi süzgecinin tek yolu)
dotnet/Kuyumcu.Api/Data/Entities.cs                 (Stok, Hareket, Kasa, PosCihaz, PosTahsilat → ISubeVarlik)
dotnet/Kuyumcu.Api/Data/DenetimIzi.cs · GiderPusulasi.cs · GunKapanisi.cs · Kurulum.cs ·
  KiymetliEvrak.cs · SayimOturumu.cs · Siparis.cs · StokYerlesimi.cs · StokPartisi.cs ·
  UrunKategorisi.cs · VezneFisi.cs                  (→ ISubeVarlik)
dotnet/Kuyumcu.Api/Modules/Vezne/VezneModulu.cs     ((a) kategorisi mekanizmaya taşındı)
dotnet/Kuyumcu.Api/Data/Migrations/20260918125655_SubeSuzgecIndeksleriP04.cs (+ .Designer.cs)
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
dotnet/Kuyumcu.Tests/SubeIzolasyonTestleri.cs       (YENİ — 19 test)
dotnet/Kuyumcu.Tests/TestOrtami.cs                  (OlusturSubeli — şube süzgeci açık bağlam)
dotnet/denetim.sh                                   (# >>> P-04 sayaç bloğu)
dotnet/sube-suzgec-siniflandirma.txt                (YENİ — mühürlü sınıflandırma tablosu)
```
(`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısı bu paketten önce de kirliydi — depoda
yüzlerce izlenmeyen dosya var; yukarıdaki liste **bu paketin** dokunduklarıdır.)

**Dokunulan sayfalar (TEK DOKUNUŞ kaydı):** **hiçbiri** — `Kuyumcu.Web/Pages` altında 0 dosya açıldı.

---

## § 12 — AÇIK KALANLAR / DEVREDİLENLER

1. **Ekran tarafı şube rozeti (UX-S21/S22) — DEVREDİLDİ: P-14 (şube seçici).** Sunucu artık şubeyi biliyor;
   ekranda "hangi şubenin verisi" rozetinin gösterimi ekran paketinin işidir. Yalnız vezne kuyruğu yanıtına
   `subeId` alanı zaten dönüyor.
2. **Kullanıcı süzgeci + global süzgeç kesişimi boş liste veriyor (§ 7 adım 5).** Sızıntı yok; ancak
   UX-S29/UX-S30 gereği ekranın "bu şube kapsamınız dışında" demesi gerekir → **DEVREDİLDİ: P-14**.
2b. **`Sube` listesi süzgeçlenmiyor:** `Sube` varlığında `SubeId` sütunu yok (kimliği `Id`); şubeye kilitli
   bir rol kiracının şube ADLARINI görebilir. Veri sızıntısı sayılmaz (ad/kod), **yetki** konusudur →
   **DEVREDİLDİ: P-09 (yetki hizalaması)**.
3. **`Kullanici` beyaz listede:** şubeye kilitli rol, başka şubenin personel kaydını listeleyebilir.
   Bu bir `[RolGerekli]`/modül yetkisi konusudur (kullanıcı modülü MUDUR+) → **DEVREDİLDİ: P-09**.
4. **Kararsız test** `UX1C1EkranTestleri.Pos_kalem_satirinda_ADET_satir_ici_duzenlenir` → **triyaj (Y-42)**.
5. **Tahmin/gerçekleşen:** tahmin 360 ajan-dk; bu tur yaklaşık **150 ajan-dk**ta kapandı. Sebep: ölçülen
   nokta sayısı 63 değil 51 çıktı ve 48'i "dokunulmaz" sınıfına girdi; iş, kör kaldırma yerine tek
   mekanizma + sayaç kurmaya indi.

---

## § 13 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

- **Mevzuat hükmü yok:** bu paket tamamen teknik kapsam/izolasyon işidir; hiçbir vergi/MASAK hükmü koda yazılmadı.
- **Kurucu kararı gerekebilecek tek politika:** "TÜM şubeler" ayrıcalığının **MUHASEBE** rolüne verilmesi.
  Bugünkü varsayım: muhasebe kiracı genelini görür (mevcut rapor davranışını korumak için). Kurucu
  "muhasebeci de kendi şubesini görsün" derse tek satır değişir: `SubeYetkisi.TumSubelerRolleri`.
  Tek kaynak olduğu için değişimin başka yerde karşılığı yoktur.
- **Mehmet (06 Güvenlik) notu:** `docker-compose.yml` içindeki MySQL parolası ile `kuyumcu-mysql`
  konteynerine bağlanılamıyor (P-03'ün 4. bulgusu bugün de geçerli); bağlantı yalnız
  `appsettings.Development.json` içindeki değerle kuruluyor. Sır rotasyonu **K-5** kurucudadır.
