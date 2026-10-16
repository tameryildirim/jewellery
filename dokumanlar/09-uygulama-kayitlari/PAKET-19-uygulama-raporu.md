# PAKET 19 — Vezne modeli: tezgâhtar satar, vezne tahsil eder (Faz A) — uygulama raporu

> Dalga 3 · Katman 2'nin son paketi · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend)
> Tarih: 2026-09-17 · **COMMIT YOK** (çalışma ağacında) · Kapsam: **yalnız Faz A**

---

## 1. Özet

Tezgâhtarın hazırladığı, veznenin tahsil ettiği **bekleyen fiş kuyruğu** kuruldu.
Vezne **kendi defterini yazmaz**: para el değiştirdiği an `SatisServisi.SatisFisi`
çağrılır, böylece tevsik (459 s. VUK), MASAK, gün kilidi, K6 kasa para birimi,
vade/limit, taş kökeni, konsinye ve stok kapıları **bugünkü tek yoldan** çalışır.
Kasa farkının sorumlusu netleşti: deftere yazılan hareketin `kullanici_id`'si
**tahsil edendir**, hazırlayan vezne fişinde saklanır (R11).

Üç yeni tablo (`vezne_fisleri`, `vezne_fis_kalemleri`, `kiraci_ayarlari`), yedi yeni
uç, bir yeni ekran (`/vezne`), İşletme ekranında "vezne ayrımı zorunlu" anahtarı,
TR+EN sözlük, **57 yeni test** ve **11 mutasyon kanıtı** üretildi.

### Önkoşul doğrulaması (kod kanıtı, ilk adım)

| Önkoşul | Beklenen | Kanıt (kod) | Sonuç |
|---|---|---|---|
| Paket 14 — karma ödeme | `POST /islemler/satis-fisi` + `OdemeSepeti` var | `SatisModulu.cs:1817` (`[HttpPost("satis-fisi")]`), `SatisModulu.cs:828` (`SatisServisi.SatisFisi`), `Modules/Satis/OdemeSepeti.cs` (`OdemeBacagiDto`, 5 bacak sınırı) | **BİTMİŞ** |
| Paket 16 — iade | `satis-iade` / `alis-iade` uçları var | `SatisModulu.cs:1834` ve `:1838`, `IadeDto` (`SatisModulu.cs:252`), `Kuyumcu.Tests/IadeTestleri.cs` | **BİTMİŞ** |
| MÜHÜR 5 ÖNKOŞUL T-5 | T-5 raporu + denetim.sh boş-migration kuralı | `PAKET-T-5-uygulama-raporu.md` var; `dotnet/denetim.sh` § 12 (`BOS_MIGRATION_BEYAZ`, "Migrations: beyaz liste dışı 0 baytlık .cs", "BuildTargetModel'siz Designer") | **KARŞILANDI** (migration üretildi) |

---

## 2. Spesifikasyondan sapmalar ve gerekçeleri

| # | Spesifikasyon / koşul | Yapılan | Gerekçe |
|---|---|---|---|
| S1 | `tenants.vezne_zorunlu` kolonu | **Yapılmadı.** Ayar yeni `kiraci_ayarlari` (IKiraciVarlik) tablosunda anahtar/değer olarak tutuluyor (`KiraciAyarAnahtari.VezneZorunlu`). | Bertan'ın bağlayıcı koşulu: `Tenant` IKiraciVarlik DEĞİL; global filtre, `KiraciYazmaDenetcisi` ve denetim izi ona işlemez. Kanıt: canlı `denetim_kayitlari`'nda `KiraciAyari` türü 2 satır düştü (§ 7). |
| S2 | R1 — vezne_zorunlu iken TEZGAHTAR doğrudan satış yapamaz | **Faz B'ye bırakıldı.** `SatisModulu.cs`'ye **dokunulmadı**, `VezneKapisi.DogrudanSatisaIzinVar` **yazılmadı**. | Görev metni "KAPSAM: yalnız Faz A"; belge § Kapsam R1'i ve `Islemler.razor` "Vezneye gönder" akışını açıkça **Faz B**'ye koyuyor. R1'i ekranı olmadan sevk etmek, ayarı açan sahibin tezgâhtarına 403 verip **alternatif yol göstermeyen** bir ürün üretirdi (UX-S30 ihlali). Sonuç: **M-V1 mutasyonu bu pakette ölçülemedi** (§ 6). |
| S3 | R2 — dört göz kuralı (belge § Kapsam'da Faz B) | **Faz A'da uygulandı.** | Ayar anahtarı Faz A kapsamında; anahtarı etkisi olmadan sevk etmek "tıklanınca hiçbir şey olmayan düğme" yasağının (DoD EK/1) ihlali olurdu. R2 tümüyle vezne tahsil ucunun içindedir, `SatisModulu.cs`'ye dokunmaz. Ekranda ve ayar açıklamasında Faz A'daki etkisi **birebir** yazılıdır ("Açıkken bir fişi hazırlayan kullanıcı onu tahsil edemez"). |
| S4 | `vezne_fis_kalemleri.adet` decimal(15,3) | **decimal(12,3)** yazıldı. | Bertan koşulu + kod gerçeği: `Stok.Adet` `decimal(12,3)` (`Entities.cs:304`). İki ölçek ayrışırsa tahsilde sessiz yuvarlama olurdu. A15 açık sorusu bu kanıtla kapandı. |
| S5 | Tahsil ucu için metot düzeyi `[ModulYetkisi("kasa")]` | **Ayrı controller**: `VezneTahsilController`, sınıf düzeyinde `[Yetki.ModulYetkisi("kasa")]`. | Mehmet koşulu: `ModulYetkisiAttribute`'un metot düzeyinde sınıf düzeyini geçersiz kıldığı DOĞRULANMADI. Canlı kanıt: kasa yetkisi kaldırılmış TEZGAHTAR → `403 "Bu ekran için yetkiniz yok (kasa)"` (§ 7). |
| S6 | Atomluk: benzersiz indeks + "fis_id'li hareket var mı" kontrolü | **Sıralama tersine çevrildi:** fiş ÖNCE iyimser kilitle `VEZNEDE → TAHSIL`e geçirilir (`VezneFisi.Guncellendi` `IsConcurrencyToken` → `SaveChanges` `WHERE guncellendi = @eski`), **ancak sonra** defter yazılır; `SatisFisi` hata verirse fiş `VEZNEDE`'ye geri alınır. Ayrıca `(tenant_id, hareket_fis_id)` BENZERSİZ indeksi ve `HareketFisId is not null` ön kontrolü duruyor. | Spesifikasyonun kendi riskinin ("defterde satır var, fiş VEZNEDE kalır, ikinci tahsil DENENEBİLİR") kökünü kurutur: ikinci istek durumu `VEZNEDE` bulamaz → 409. Süreç 3. adımda düşerse fiş `TAHSIL` + `hareket_fis_id = NULL` kalır; kuyruk bunu `mutabakatGerekli` bayrağıyla gösterir (risk azaltma (c)). Test: `Defter_yazildiktan_sonra_fis_bagi_kaybolsa_bile_IKINCI_tahsil_defterde_satir_URETMEZ`. |
| S7 | `/vezne` ekranı yalnız kuyruk + tahsil paneli | Ekrana **yeni fiş formu + Cekmece ile düzenleme + gerekçeli iptal** de kondu. | Faz A'da fiş açan başka ekran yok (`Islemler.razor` Faz B). Aksi hâlde "sunucuda uç var, ekranda düğme yok" (UX-S25) ve DoD "her yeni varlık ekranda Düzenle/Pasife al ile doğar" ihlal edilirdi. |
| S8 | Kuyruk 10 sn'de bir yenilenir **ya da** elle yenile | **Yalnız elle yenile** (`#vz-yenile`). Zamanlayıcı yok. | Aşkın koşulu (kararsızlık) + A2'deki "Yükleniyor'da kalma" hatası. bUnit testi zamanlayıcı beklemiyor. |
| S9 | denetim.sh `BEKLENEN_VARLIK` 15 → 17 | **Yapılmadı** (mutlak sayı yazılmaz — çapraz bulgu 3). Yerine iki **yeni kural** eklendi: `Vezne/ altında defter yazımı = 0` ve `Vezne → SatisFisi çağrısı ≥ 1`. | Bertan koşulu. `BEKLENEN_VARLIK` alt sınırdır (`-ge`), 31 varlıkla zaten geçiyor. |
| S10 | İstemci yanıt modelleri sayfada | Modeller `Kuyumcu.Core/Sozlesmeler/VezneSozlesmeleri.cs`'e kondu. | denetim.sh "Pages içinde istemci record tanımı" tavanı (S-2 / MÜHÜR 5 KOŞUL 3) dondurulmuş; yeni uçlar ortak sözleşme tipinden tüketilir. |
| S11 | `denetim.sh` "Pages/*.razor dosya sayısı" tavanı 30 | 31'e çıkarıldı (mevcut değer okunup **bir** artırıldı). | Paket bilerek bir sayfa ekliyor; tavan yeni sayfa **sızmasını** yakalamaya devam ediyor. |

**Mevzuat işaretleri (spesifikasyon § Mevzuat dayanağı):**
tevsik ve MASAK **DOĞRULANMIŞ** hükümlerdir ve bu pakette **yeniden yazılmadı** —
yalnız mevcut kapıların tahsil anında çalıştığı kanıtlandı. Görev ayrılığı / dört göz
**VARSAYIM** olarak işaretliydi; bu yüzden koda "yasa gereği" bir kural KONMADI:
kural kiracı ayarıdır ve **varsayılanı KAPALI**dır (canlı kanıt: yeni kiracıda
`GET /admin/vezne → {"zorunlu":false}`).

---

## 3. Derleme ve migration

```
dotnet build Kuyumcu.sln --nologo   →   0 Uyarı   0 Hata
```

**Migration:** `20260917094711_VezneModeli`
(`dotnet/Kuyumcu.Api/Data/Migrations/20260917094711_VezneModeli.cs`, 7.096 bayt;
Designer 130.140 bayt, `BuildTargetModel` **var** → T-5 hijyen kuralı temiz).

`Up()` gövdesi: `kiraci_ayarlari`, `vezne_fis_kalemleri`, `vezne_fisleri` tabloları +
4 indeks (`(tenant_id, anahtar)` UNIQUE · `(tenant_id, fis_id, sira)` ·
`(tenant_id, hareket_fis_id)` UNIQUE · `(tenant_id, sube_id, durum, olusturuldu)`).
`Down()`: üç tabloyu düşürür. Geri doldurma yok (yeni kavram).

**kuyumcu_net uygulama kanıtı:**

```
dotnet ef database update … --connection "…database=kuyumcu_net…"
Applying migration '20260917094711_VezneModeli'.  Done.

mysql> SHOW TABLES LIKE 'vezne%';  → vezne_fis_kalemleri, vezne_fisleri
mysql> SHOW TABLES LIKE 'kiraci_ayarlari';  → kiraci_ayarlari
```

---

## 4. Yapılanlar — dosya dosya

**YENİ**
- `dotnet/Kuyumcu.Api/Data/VezneFisi.cs` — `VezneDurum` (ACIK·VEZNEDE·TAHSIL·IPTAL), `VezneFisi`, `VezneFisKalemi`, `KiraciAyari`, `KiraciAyarAnahtari` (hepsi `IKiraciVarlik`).
- `dotnet/Kuyumcu.Api/Modules/Vezne/VezneKapisi.cs` — saf kapı sınıfı: `DortGozDogrula` (R2), `TahsilDurumuDogrula` (R3), `DuzenlemeDurumuDogrula` (R4), `IptalDurumuDogrula` (R5), `SahiplikDogrula`.
- `dotnet/Kuyumcu.Api/Modules/Vezne/VezneModulu.cs` — DTO'lar, `VezneAyari` (ayar okuma), `VezneServisi` (Olustur/Guncelle/Veznele/Kuyruk/Detay/Tahsil/Iptal), `VezneController` (`islem`), `VezneTahsilController` (`kasa`).
- `dotnet/Kuyumcu.Core/Sozlesmeler/VezneSozlesmeleri.cs` — istemci yanıt sözleşmeleri.
- `dotnet/Kuyumcu.Web/Pages/Vezne.razor` — `/vezne` ekranı.
- `dotnet/Kuyumcu.Api/Data/Migrations/20260917094711_VezneModeli.cs` + `.Designer.cs`.
- `dotnet/Kuyumcu.Tests/VezneTestleri.cs` · `HttpVezneTestleri.cs` · `VezneEkranTestleri.cs`.

**DEĞİŞTİRİLEN**
- `KuyumcuDbContext.cs` — 3 DbSet, 4 indeks, `Guncellendi` eşzamanlılık jetonu. (Kiracı filtresi yansımayla otomatik.)
- `Modules/Admin/AdminModulu.cs` — `VezneAyarGetir` + `VezneToggle` + `GET/PATCH /admin/vezne` (sınıf düzeyi `[RolGerekli("SAHIP")]`).
- `Modules/ModulKayit.cs` — `AddScoped<Vezne.VezneServisi>()`.
- `Modules/Meta/DuzeltmePolitikasi.cs` — `VezneFisi` (DÜZENLENEBİLİR), `VezneFisKalemi` (SALT_OKUNUR/sistem), `KiraciAyari` (DÜZENLENEBİLİR) damgaları.
- `Kuyumcu.Core/I18n/Sozlukler.cs` — `SozlukVezne` (66 anahtar, TR+EN) + `Sozluk.Vezne` + `SozlukNav.Vezne`.
- `Kuyumcu.Web/Layout/KenarCubugu.razor` — Kasa grubuna `/vezne` bağlantısı (`Gorunur("kasa", "/vezne")`).
- `Kuyumcu.Web/Layout/RotaBasliklari.cs` — `/vezne` başlığı.
- `Kuyumcu.Web/Bilesenler/Ikonlar.cs` — `Vezne = "wallet"` (sprite BÜYÜTÜLMEDİ; `Kasalar` ile aynı sembolü paylaşır).
- `Kuyumcu.Web/Pages/Isletme.razor` — "Vezne ayrımı zorunlu" anahtarı (`#i-vezne-zorunlu`, yalnız SAHİP görür) + `VezneYukle`/`VezneDegistir`.
- `dotnet/denetim.sh` — vezne tek-yazma-yolu kuralı (2 sayaç) + sayfa tavanı 30 → 31.
- `Kuyumcu.Tests/IkonTestleri.cs`, `RotaBaslikTestleri.cs` — menü rotası ve rota sayısı sözleşmeleri (30 → 31).

**Dokunulan sayfalar (MÜHÜR 2 — TEK DOKUNUŞ):**
`Kuyumcu.Web/Pages/Vezne.razor` (**YENİ**) · `Kuyumcu.Web/Pages/Isletme.razor`
(gerekçe: spesifikasyon § Ekranlar, ayar anahtarının tek meşru yeri).
`Islemler.razor`'a **DOKUNULMADI** (Faz B).

---

## 5. Testler

| Koşum | Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|---|
| Önce (paket öncesi taban) | tam süit | 1862 | 1862 | 0 | 0 |
| Filtreli 1 | `~VezneTestleri` (DB + HTTP) | 32 | 32 | 0 | 0 |
| Filtreli 2 | `~HttpVezne` | 9 | 9 | 0 | 0 |
| Filtreli 3 | `~VezneEkranTestleri` (bUnit) | 18 | 18 | 0 | 0 |
| Filtreli 4 | `~RotaBaslik` | 37 | 37 | 0 | 0 |
| Tam süit (1. koşum) | tümü | 1919 | 1917 | **2** | 0 |
| Tam süit (2. koşum) | tümü | **1919** | **1919** | **0** | 0 |

**Yeni test sayısı: 57** (VezneTestleri 23 · VezneKapisiTestleri 7 · HttpVezneTestleri 9 · VezneEkranTestleri 18). Taban: 1919 − 57 = **1862**.

### 1. tam koşumdaki iki kırmızı (Anayasa md.1 — gizlenmedi)

1. `DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR` —
   *"Politika haritasında damgası olmayan varlık(lar): VezneFisi, VezneFisKalemi, KiraciAyari"*.
   **GERÇEK EKSİK, DÜZELTİLDİ:** üç varlık `Modules/Meta/DuzeltmePolitikasi.cs` haritasına damgalandı.
2. `UX1C1EkranTestleri.Pos_gecersiz_kalemde_EKLE_sessizce_gecmez_ALAN_ALTI_mesaj_verir` —
   *`Bunit.ElementNotFoundException: '#alan-hata-islem-adet'`*.
   **KARARSIZLIK (bu paketle ilgisi yok):** `Islemler.razor`'a bu pakette dokunulmadı.
   Kanıt: yalıtılmış koşum `--filter "FullyQualifiedName~UX1C1EkranTestleri"` →
   **35/35 yeşil**; ikinci tam koşumda da yeşil (1919/1919). Paralel koşumda
   paylaşılan ekran fikstürü kaynaklı; **açık kalan** olarak § 10'a yazıldı.

---

## 6. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/paket19/` altına kopyalandı → `perl -0777 -pi -e`
ile **tek eşleşmeli** değişiklik → filtreli koşum → yedek geri kopyalandı → `cmp` ile
birebir aynılık kanıtlandı. Son mutasyondan sonra `dotnet build Kuyumcu.sln` **0 hata 0 uyarı**.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (cmp) |
|---|---|---|---|
| M-V1 | `VezneKapisi.DogrudanSatisaIzinVar` gövdesi boşaltılır | **ÖLÇÜLEMEDİ — Faz B kapsamı.** Kapı bu pakette yazılmadı (sapma S2). Faz B'nin kabul şartıdır. | — |
| M-V2 | `DortGozDogrula` eşitlik kontrolü `if (false)` yapıldı | `VezneKapisiTestleri.Dort_goz_yalniz_ayar_acikken_uygulanir` · `VezneTestleri.Vezne_ayrimi_ACIKKEN_hazirlayan_kendi_fisini_tahsil_EDEMEZ` · `HttpVezneTestleri.Vezne_ayrimi_acikken_hazirlayan_kendi_fisini_tahsil_edemez_400` (3 kırmızı / 57) | ✔ cmp birebir |
| M-V3 | Tahsilde `VezneKapisi.TahsilDurumuDogrula` çağrısı silindi | `VezneTestleri.ACIK_fis_tahsil_EDILEMEZ` (1 kırmızı / 57) | ✔ |
| M-V4 | Migration'daki `(tenant_id, hareket_fis_id)` indeksi `unique: false` yapıldı (şema kaynağı migration'dır) | `VezneTestleri.Ayni_defter_fisi_IKINCI_vezne_fisine_baglanamaz_VERITABANI_reddeder` (1 kırmızı / 32) — test uygulama kapısını **bilerek atlar**, yalnız indeks katmanını ölçer | ✔ |
| M-V5 | PATCH'teki `DuzenlemeDurumuDogrula` çağrısı silindi | `VezneTestleri.VEZNEDE_fisin_kalemleri_DEGISTIRILEMEZ` | ✔ |
| M-V6 | Vezne modülüne `db.Hareketler.Add(...)` eklendi (derlenir) | `dotnet/denetim.sh` → `✗ Vezne/ altında defter yazımı (0 olmalı) 1 (beklenen 0)` → **DENETİM BAŞARISIZ**; geri alınca **DENETİM TEMİZ** | ✔ |
| M-V7 | Kuyruk sorgusundaki `&& f.SubeId == subeId` kaldırıldı | `VezneTestleri.Baska_subenin_fisi_kuyrukta_GORUNMEZ_ve_tahsil_edilemez` | ✔ |
| M-V8 | Tahsilde kur gövde yerine **fişten** alındı | `VezneTestleri.Deftere_TAHSIL_aninda_gonderilen_kur_yazilir_fisinki_degil` | ✔ |
| M-V9 | `SatisFisi`'ye `islem with { Id = fis.HazirlayanKullaniciId }` geçildi | `VezneTestleri.Defterdeki_hareketin_sahibi_TAHSIL_EDENDIR_hazirlayan_degil` | ✔ |
| M-V10 | Vezneleme `ToplamTl = 0m` yaptı (damga tahsile kaydı) | `VezneTestleri.Toplam_VEZNELEME_aninda_damgalanir_tahsilde_degil` · `Fis_ac_veznele_tahsil_et_…` · `Kuyruk_kisisel_veri_dondurmez_…` · `HttpVezneTestleri.Tezgahtar_fis_acabilir_ve_vezneye_gonderebilir` (4 kırmızı) | ✔ |
| **M-V11 (ek)** | Tahsildeki `IyimserKilitDogrula` çağrısı silindi | `VezneTestleri.Eskimis_damga_ile_tahsil_409_verir` | ✔ |
| **M-V12 (ek)** | PATCH'teki `SahiplikDogrula` çağrısı silindi | `VezneTestleri.Baska_tezgahtarin_fisi_PATCH_edilemez_ama_MUDUR_edebilir` | ✔ |

**Test açığı bulunmadı:** her mutasyon en az bir testi kırdı (M-V1 hariç; o kod bu pakette yok).

---

## 7. denetim.sh

```
dotnet/denetim.sh
  ✓ Vezne/ altında defter yazımı (0 olmalı)    0
  ✓ Vezne → SatisFisi çağrısı (>=1)          1
  ✓ Pages: duzenlenenId kalıbı                   1 (tavan 1)
  ✓ Pages/*.razor dosya sayısı                   31 (tavan 31)
  ✓ Pages içinde istemci record tanımı          122 (tavan 122)
  ✓ Migrations: beyaz liste dışı 0 baytlık .cs 0
  ✓ Migrations: BuildTargetModel'siz Designer      0
  ✓ Kaynak ağacı: beyaz liste dışı .mutorj    0
DENETİM TEMİZ.
```

Yol boyunca yakalanan ve düzeltilen üç ihlal (gizlenmedi):
`duzenlenenId` ikinci kalıbı (değişken `cekmeceFisId` olarak adlandırıldı),
sayfa tavanı, Pages içindeki istemci record tavanı (modeller Core'a taşındı).

---

## 8. Canlı doğrulama (localhost:5198, gerçek veri)

Kiracı **PAKET19-TEST** açıldı; kur kaydı 2. denemede geldi
(`KUR=01M2QDG4SP8AWFBWA6YFETXKPB`), iki kullanıcı kuruldu:
**t1** (TEZGAHTAR) · **m1** (MUDUR). Stok `POST /islemler/alis` (HAVALE) ile 50 adet.

| # | Adım | Beklenen | Gerçekleşen | SQL / gövde kanıtı |
|---|---|---|---|---|
| 1 | t1 → `POST /vezne/fisler` | 200, durum ACIK | **200**, `"durum":"ACIK"` | `id=01M2QDGZM1QRWH35H7XS0Z6T21` |
| 2 | t1 → `POST …/veznele` | 200, VEZNEDE, toplam damgalı | **200**, `durum=VEZNEDE toplamTl=5000.00` | — |
| 3 | kasa yetkisi kaldırılmış t1 → `POST …/tahsil` | 403 | **403** `"Bu ekran için yetkiniz yok (kasa)"` | `SELECT COUNT(*) FROM hareketler … → 1` (yalnız alış) |
| 4 | m1 → `POST …/tahsil` (NAKIT 2.000 + KART 3.000) | 200, kasaya 5.000, fis_id'li defter satırı | **200**, `hareketFisId=01M2QDHB1HJ3GF5BCQW1W8CWRY` | `hareketler WHERE fis_id=@f → 1` · `kasa_hareketleri (tutar>0) → 2 satır, SUM=5000.00` · `vezne_fisleri → TAHSIL, hareket_fis_id dolu, tahsil_eden dolu` |
| 5 | Aynı fişe **ikinci** `/tahsil` | **409**, defterde YENİ SATIR YOK | **409** `"Yalnız vezneye gönderilmiş fiş tahsil edilebilir (bu fişin durumu: TAHSIL)."` | `COUNT(*) WHERE fis_id=@f → 1` · `COUNT(*) hareketler → 2` (alış + satış) |
| 6 | R11 — hareketin sahibi | tahsil eden ≠ hazırlayan | **doğrulandı** | `h.kullanici_id=01M2QDGSV4ABQ0RNT6YHPMPGS5` = `v.tahsil_eden…` ≠ `v.hazirlayan…=01M2QDGSJTHZM14QPJ7TX5SC8S` |
| 7 | `PATCH /admin/vezne {zorunlu:true}` (SAHİP) | 200 | **200** `{"zorunlu":true}`; `GET /admin/vezne` → `{"zorunlu":true}` | `kiraci_ayarlari → vezne_zorunlu = true` |
| 8 | Aynı uç, m1 (MUDUR) | 403 | **403** `"Bu işlem için gerekli rol: SAHIP"` | — |
| 9 | Dört göz: t1 kendi fişini tahsil | 400 | **400** `"Kendi hazırladığınız fişi tahsil edemezsiniz…"` | — |
| 10 | Aynı fişi m1 tahsil | 200 | **200** | — |
| 11 | Faz A geriye uyumluluk: t1 → `POST /islemler/satis` | 200 (R1 Faz B) | **200** | — |
| 12 | Tevsik: 30.001 TL **NAKİT** bacaklı tahsil | 400 | **400** (459 s. VUK §4.1.2) | tahsil sonrası `GET /vezne/fisler/{id}` → `durum=VEZNEDE` (**R9: fiş geri alındı**) |
| 13 | Aynı fiş, **HAVALE** | 200 | **200** | — |
| 14 | İptal: gerekçe "kısa" | 400 | **400** | — |
| 15 | İptal: 10+ karakter gerekçe | 200, kayıt SİLİNMEZ | **200** | `vezne_fisleri → IPTAL 1 satır (duruyor)` |
| 16 | TAHSİL edilmiş fişi iptal | 409 | **409** | — |
| 17 | ACIK fişte `PATCH` (kalemler toptan) | 200, toplam yeniden hesaplanır | **200**, `toplam=2450.00` (2×1200+50) | — |
| 18 | VEZNEDE fişte `PATCH` | 409 | **409** | — |
| 19 | Gün kapanışı sonrası tahsil | 400 (gün kilidi) | **400** `"…kapanmış güne hareket yazılamaz…"` | tahsil sonrası `durum=VEZNEDE` |
| 20 | `GET /vezne/kuyruk?durum=TUMU` | sınır + ayar + kırılım | **200** `azamiLimit=200, dahaFazlasiVar=false, vezneZorunlu=true, toplam=5`, kırılım IPTAL 1 / TAHSIL 3 / VEZNEDE 1 | — |
| 21 | Denetim izi | tahsil + iptal + ayar izi | **düştü** | `denetim_kayitlari → VezneFisi 25, KiraciAyari 2` |

**Temizlik:** API PID ile durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**.
`tenant_id` kolonu olan **43 tabloda** PAKET19-TEST satırları silindi; `tenants`
**9 → 8** (başlangıç değeri). `vezne_fisleri / vezne_fis_kalemleri / kiraci_ayarlari`
→ **0 / 0 / 0**. `QA-TEST-` önekli **3 kalıcı QA kiracısına dokunulmadı**.
**5199 portuna hiç dokunulmadı.**

---

## 9. DoD sözleşmesi (MÜHÜR 2 + DoD EK 1/2)

| Varlık | Düzenleme yolu | Pasife alma / iptal | Yetki | Denetim izi | Ekran eylemi |
|---|---|---|---|---|---|
| `VezneFisi` | `PATCH /vezne/fisler/{id}` — **yalnız ACIK**; gövdede yoksa KORU, null ise SİL; kalemler toptan değişir | `POST …/iptal` (gerekçe ≥10) — **hard DELETE YOK** | `[ModulYetkisi("islem")]` + `[RolGerekli(4 rol)]` + servis içi `SahiplikDogrula` | `DenetimServisi.YazmaKaydet` **çağrılır** (tahsil + iptal) + `DenetimYazmaDinleyicisi` | `#vz-duzenle-{id}` (Cekmece) · `#vz-veznele-{id}` · `#vz-iptal-{id}` (ModalPencere onayı) |
| `VezneFisKalemi` | Fişin PATCH ucundan (sil + yaz) — **tek tek kalem ucu AÇILMAZ** | Fişle birlikte | aynı | fiş üzerinden | Cekmece içinde satır alanları |
| `KiraciAyari` (vezne_zorunlu) | `PATCH /admin/vezne` | Anahtar `false`'a çekilir (silinmez) | `[RolGerekli("SAHIP")]` (sınıf düzeyi) | `YazmaKaydet` **çağrılır** | `/isletme` → `#i-vezne-zorunlu` |

**Salt okunur olanlar ve gerekçesi:** VEZNEDE/TAHSIL/IPTAL fiş → ekranda
`DegismezIpucu` (`#degismez-ipucu-VezneFisi`) + alternatif yol metni
("Para iade edilecekse İşlemler ekranındaki satış iadesi yolu kullanılır").
**Finansal varlıkta hard DELETE yok:** vezne modülü `hareketler`/`kasa_hareketleri`
satırı ne yazar ne siler (denetim.sh sayacı = 0). Storno taklidi **yapılmadı**.

**DoD EK 1 — her düğme:** `/vezne` ekranındaki her `<button>` ya istek atar ya da
`disabled` + sebep taşır (`#vz-tahsil-kilit`, `#vz-f-kilit`, `#vz-iptal-kilit`,
`cekmece-vezne-fisi-kilit`). bUnit testleri üçünü de ölçüyor.
**DoD EK 1 — tek düzenleme kalıbı:** çok alanlı düzenleme **Cekmece**,
yıkıcı onay **ModalPencere**; `duzenlenenId` kalıbı **açılmadı** (denetim.sh sayacı 1'de kaldı).
**DoD EK 1 — sessiz form yasağı:** boş kalemle Kaydet → istek atılmaz, `#vz-f-kalem-hata`.

---

## 10. Koşul karşılama tablosu (Birleşik koşullar + UX-S21…S33)

| Koşul | Durum | Kanıt |
|---|---|---|
| Yetki katmanı: tahsil ayrı controller (`kasa`) | ✅ | `VezneTahsilController`; canlı adım 3 (403) |
| R1 kapısı `SatisServisi` içinde | ⛔ **Faz B** | Sapma S2 |
| Kapı fail-closed "rol ∉ {SAHIP, MUDUR}" | ⛔ **Faz B** (kapı yazılmadı) | Sapma S2 |
| 19 ile 46 aynı dalgada sevk edilmez | ✅ | Paket 46 bu dalgada yok |
| Yarış/atomluk: ön kontrol + iyimser kilit, ExecuteUpdate YOK | ✅ | Sapma S6; M-V11; canlı adım 5 (409) |
| `vezne_fis_kalemleri.adet = decimal(12,3)` | ✅ | Migration `adet decimal(12,3)` |
| `tenants.vezne_zorunlu` RED, ayrı IKiraciVarlik tablo | ✅ | `kiraci_ayarlari`; canlı adım 7/21 |
| `vezneUzerinden` bayrağı DTO/HTTP gövdesinde yok | ✅ | Böyle bir alan hiç yazılmadı (Faz B'ye kaldı) |
| Sahiplik kontrolü sunucuda + "başka tezgâhtarın fişi → 403" testi | ✅ | `Baska_tezgahtarin_fisi_PATCH_edilemez_ama_MUDUR_edebilir`; M-V12 |
| Tahsil/iptal `YazmaKaydet` ile gerekçeli iz, fiş silinmez | ✅ | Canlı adım 21 (`VezneFisi` 25 iz) |
| QA: "kasa yetkili TEZGAHTAR + ayrım kapalı → 200" ve "kasa kaldırılınca 403" | ✅ | `Kasa_yetkisi_OLAN_tezgahtar_…` · `Kasa_yetkisi_KALDIRILAN_tezgahtar_…`; canlı adım 3 |
| M-V4 ölçülebilir (tek katman) | ✅ | Test uygulama kapısını atlar, indeksi ölçer; M-V4 kırmızı |
| Atomluk testi (fiş güncellemesi kaybolursa) | ✅ | `Defter_yazildiktan_sonra_fis_bagi_kaybolsa_bile_…` |
| Canlı adım 7 tek kod: **409** + `SELECT COUNT(*)` | ✅ | Canlı adım 5 |
| Kuyrukta cariAd dışında kişisel veri yok; yabancı kiracı/şube 404 | ✅ | `Kuyruk_kisisel_veri_dondurmez…`, `Baska_kiracinin_fis_kimligiyle_tahsil_404` (DB + HTTP) |
| Vezne.razor yoklaması zamanlayıcısız, elle yenile | ✅ | `Elle_yenile_dugmesi_kuyrugu_YENIDEN_ceker` |
| denetim.sh'a mutlak sayı yazılmadı | ✅ | Sapma S9/S11 |
| UX-S21 sistemin bildiği alan sorulmaz | ✅ | Kur `/kur/guncel`'den, şube oturumdan (gövdeden alınmaz), `#vz-f-kur-rozet` |
| UX-S22 tek seçenek → seçici gizlenir | ✅ | `Tek_kasada_kasa_secici_CIZILMEZ…` + `Iki_kasada_secici_CIZILIR` |
| UX-S23 soyut kaydın etkisi aynı ekranda | ✅ | Fiş kaydedilince kuyruk yenilenir; damgalı tutar satırda görünür |
| UX-S24 bağımlı alanın bağını ekran kurar | ✅ | `Kalanı ata`; VERESIYE bacağında kasa seçici kapanır |
| UX-S25 oluşturulabilen kayıt düzeltilebilir ya da gerekçe yazar | ✅ | Cekmece + `DegismezIpucu` |
| UX-S26 oluşturma = düzenleme alan kümesi, tek dizi | ✅ | Aynı `FormKalemi`; `Araclar` tek dizi |
| UX-S27 kaydet sonrası sonuç görünür | ✅ | `Tahsil_TEK_POST_uretir_ve_defter_fisi_basari_bandina_yazilir` (banda `f-42`) |
| UX-S28 hata "kayıt yok"a çevrilmez | ✅ | `catch` yalnız `hata` yazar, `liste` boşaltılmaz |
| UX-S29 sunucu sınırı ekranda | ✅ | `#vz-sinir-uyarisi` testi |
| UX-S30 kilitli düğme sebebini söyler | ✅ | `#vz-tahsil-kilit` (3 farklı sebep) |
| UX-S31 tek ad/tek dil/tek kaynak | ✅ | `Ham_durum_kodu_ekrana_basilmaz_sozlukten_cevrilir` |
| UX-S32 para birimi tutarla taşınır | ✅ | Her tutar `Bicim.Para(x, "TRY")` |
| UX-S33 dönemden bağımsız kart | ✅ | Ekranda dönem seçici yok — kart yok |
| Ücretsiz pilot: fiyat/teklif metni üretilmedi | ✅ | Rapor ve ekranda fiyatlandırma metni yok |

---

## 11. Açık kalanlar

1. **Faz B borcu (bu paketin doğrudan devamı):** R1 doğrudan satış kapısı
   (`VezneKapisi.DogrudanSatisaIzinVar`, `SatisServisi` içinde, fail-closed
   "rol ∉ {SAHIP, MUDUR}"), `vezneUzerinden` iç parametresi, `Islemler.razor`
   "Vezneye gönder" akışı ve **M-V1 mutasyon kanıtı**.
2. **`UX1C1EkranTestleri.Pos_gecersiz_kalemde_EKLE_…` kararsızlığı** — tam süitin
   1. koşumunda kırmızı, yalıtılmış koşumda ve 2. tam koşumda yeşil. Bu paketle
   ilişkisi yok (`Islemler.razor`'a dokunulmadı); paralel bUnit fikstür paylaşımı
   şüphesi. Ayrı bir kararsızlık avı işi.
3. **Tahsil atomluğu tam değil:** `SatisFisi` dış transaction kabul etmiyor. 3. adım
   (defter fişini fişe bağlama) kaybolursa fiş `TAHSIL` + `hareket_fis_id = NULL`
   kalır; ekranda `mutabakatGerekli` rozetiyle görünür ama **otomatik onarım yok**.
   Tam atomluk `SatisFisi` imzasının genişletilmesini gerektirir (spesifikasyonun
   açık sorusu; mevcut kodu değiştirmek ayrı karar).
4. **Stok rezervasyonu yok** (Faz C adayı): müşteri veznede beklerken aynı tek parça
   mal başka tezgâhtar tarafından satılabilir; tahsilde "yetersiz stok" 400'ü gelir.
5. **Şube düzeyinde vezne ayarı** yok (kiracı düzeyi seçildi); paket 46 şube
   görünürlüğü geldiğinde indirilebilir.
6. **Sıra/çağrı numarası yok** (insan-okur fiş no) — spesifikasyonun açık sorusu.
7. **İkon paylaşımı:** `/vezne` ile `/kasalar` aynı `wallet` sembolünü kullanıyor
   (sprite büyütülmedi). UX-2 görsel turunda ayrıştırılabilir.

---

## 12. Kurucuya / mali müşavire notlar

1. **Görev ayrılığı (dört göz) bir YASAL ZORUNLULUK OLARAK KODLANMADI.** Perakende
   kuyumcu için bunu zorunlu kılan bir Türk mevzuat hükmü bu oturumda
   araştırılmadı (spesifikasyonda **VARSAYIM** işaretli). Bu yüzden kural
   **yapılandırmadan** okunur ve **varsayılanı KAPALI**dır; tek kişilik dükkân
   hiçbir şekilde kilitlenmez. Mali müşavir "iç kontrol / görev ayrılığı" için bir
   zorunluluk bildirirse ayar varsayılanı açılabilir — karar kurucunundur.
2. **Tevsik (459 s. VUK, 30.000 TL nakit haddi) ve MASAK eşikleri bu pakette
   DEĞİŞTİRİLMEDİ.** Vezne yolu yeni bir mevzuat kuralı getirmez; yalnız mevcut
   kapıların **tahsil anında** çalıştığı canlıda kanıtlandı (adım 12/13).
3. **Kasa farkı artık kime sorulacağı belli:** deftere yazılan hareketin sahibi
   tahsil eden kullanıcıdır; fiyat/indirim sorusu hazırlayana sorulur. İkisi de
   kalıcı olarak saklanıyor (canlı adım 6).
4. **COMMIT YAPILMADI**; tüm değişiklikler çalışma ağacındadır.
