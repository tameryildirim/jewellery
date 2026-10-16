# PAKET P-08 — TUR 2: KALAN DoD BORCUNUN KAPATILMASI (2026-09-20)

> **Bu belge, `PAKET-P-08-uygulama-raporu.md`nin devamıdır ve onun § DÜZELTME bölümünde
> yazılı iki borcu kapatır:** (1) mutasyon turu koşulmamıştı, (2) canlı doğrulama kanıtı
> rapora geçmemişti. Ayrıca tam süit ve `denetim.sh` yeniden koşuldu.
>
> **Anayasa md.1 uyarısı:** bu turda **kaynak kodda tek satır değişiklik yapılmadı.**
> Mutasyonlar geçici olarak uygulandı, geri alındı ve `cmp` ile birebir doğrulandı.
> Paketin kodu 18-19 Eylül'de yazılmıştı; bu tur yalnız **kanıt** üretir.

---

## § 0 — Önkoşul ölçümü (P-05)

`dokumanlar/09-uygulama-kayitlari/PAKET-P-05-uygulama-raporu.md` diskte **VAR**.
`__EFMigrationsHistory`'de P-05 göçleri (`20260918131942_P05DogalAnahtarBenzersizlik`,
`20260918132020_P05CariVergiNoBenzersiz`) **uygulanmış** durumda. Önkoşul karşılanıyor;
kapsam daraltması gerekmedi.

---

## § 1 — Kapı kaydı (2026-09-20 22:25–22:55)

| Kontrol | Komut | Sonuç |
|---|---|---|
| Derleme | `dotnet build Kuyumcu.sln --nologo` | **0 Hata / 0 Uyarı** |
| Denetim (mutasyon öncesi) | `dotnet/denetim.sh` | **DENETİM TEMİZ** (P-08 bloğu 6/6 ✓) |
| Denetim (mutasyon sonrası) | `dotnet/denetim.sh` | **DENETİM TEMİZ** (116 ✓) |
| Tam süit | `dotnet test … --logger trx` | **3032 / 3032 geçti, 0 başarısız** · 40,65 sn |
| Migration (uygulanmış) | `SELECT COUNT(*) FROM __EFMigrationsHistory` | **53** |
| Migration dosyası (.cs) | `ls Migrations/*.cs` | **115** |
| Test kiracısı | `SELECT COUNT(*) FROM tenants WHERE ad LIKE 'PAKET%'` | **0** (kiracı toplamı **8**, başlangıçtaki değer) |
| 5199 | `lsof -nP -iTCP:5199 -sTCP:LISTEN` | **çalışıyor, dokunulmadı** (3 satır) |
| 5198 | `lsof -nP -iTCP:5198 -sTCP:LISTEN` | **0 satır** (kapatıldı) |

> **Süre notu:** önceki rapordaki 41 dk 12 sn'lik tam süit bellek açlığı kaynaklıydı.
> Bu turda aynı süit **40,65 saniye** sürdü — kök neden ortadan kalkmış.

---

## § 2 — Migration ve `Down()` TATBİKATI (PLAN § 1 ilke 10)

**Göç:** `20260918150025_HareketAdetPersonel`

| Dosya | Boyut |
|---|---|
| `20260918150025_HareketAdetPersonel.cs` | **1.502 bayt** (0 bayt DEĞİL) |
| `20260918150025_HareketAdetPersonel.Designer.cs` | **195.532 bayt** |

`Up()`: `hareketler.adet decimal(12,3) NULL` + `hareketler.personel_id varchar(36) NULL`
+ `IX_hareketler_tenant_id_personel_id_tarih (tenant_id, personel_id, tarih)`.
`Down()`: indeks + iki kolon düşürülür. **`fatura_id` kolonu açılmamıştır** (ADR-0018).

### `Down()` GERÇEKTEN KOŞTURULDU (bu turda, kuyumcu_net üzerinde)

Ön ölçüm: `SELECT COUNT(*) FROM hareketler WHERE adet IS NOT NULL` → **0**
(kendi test kiracım silindikten sonra; yani tatbikat **hiçbir veriyi imha etmedi**).
`QA-TEST-*` fikstürlerinde `adet` dolu satır yoktu.

| Adım | Komut | Sonuç |
|---|---|---|
| Geri alma | `dotnet ef database update 20260918132020_P05CariVergiNoBenzersiz --project Kuyumcu.Api --connection …` | `Reverting migration '20260918150025_HareketAdetPersonel'. Done.` |
| Şema kontrolü | `information_schema.COLUMNS … IN ('adet','personel_id')` | **0 kolon** (ikisi de düştü) |
| Tarihçe | `SELECT COUNT(*) FROM __EFMigrationsHistory` | **52** |
| Yeniden uygulama | `dotnet ef database update --project Kuyumcu.Api --connection …` | `Applying migration '20260918150025_HareketAdetPersonel'. Done.` |
| Şema kontrolü | aynı sorgu | `adet decimal(12,3)` · `personel_id varchar(36)` |
| Tarihçe | aynı sorgu | **53** (başlangıç değerine döndü) |
| İndeks | `information_schema.STATISTICS … 'IX_hareketler_tenant_id_personel_id_tarih'` | **3 satır** (üç kolonlu indeks yerinde) |

`--no-build` **kullanılmadı**; iki `dotnet ef` çağrısı da `Build succeeded.` satırıyla başladı.

---

## § 3 — MUTASYON KANITI (9 mutasyon, 9'u da KIRMIZI)

Yöntem: kaynak dosyalar
`/private/tmp/…/scratchpad/mut-yedek/P-08/` altına kopyalandı → `perl -0777 -pi -e` ile
tek eşleşmeli değişiklik → `dotnet test --filter FullyQualifiedName~P08Hareket` →
yedek geri kopyalandı → `cmp` ile birebir doğrulandı.
Filtre kümesi **19 test**tir; her satırdaki "Geçen" sütunu 19 − kırmızı sayısıdır.

| Kod | Dosya:kural | Değişiklik | KIRMIZI olan test(ler) | cmp |
|---|---|---|---|---|
| **M08-1** | `HareketFabrikasi.cs:94` — Y-32 tür kapısı | `if (…HareketTurleri…)` → `if (false)` | `TurKapisi_ListedeOlmayanTur_400_doner` · `TurKapisi_BosTur_400_doner` (2) | **BİREBİR** |
| **M08-2** | `HareketFabrikasi.cs:103` — Y-14 adet kapısı | `adet < 0m` → `adet < -999999m` | `AdetKapisi_NegatifAdet_400_doner` | **BİREBİR** |
| **M08-3** | `HareketFabrikasi.cs:110` — Y-13 personel kapısı | `&& string.IsNullOrWhiteSpace(...)` → `&& false` | `PersonelKapisi_BosDizge_400_doner` | **BİREBİR** |
| **M08-4** | `FaturaHareketBagi.cs:90` — KALEM otoritesi | `if (sonuc.ContainsKey(h.Id)) continue;` → `if (false) continue;` | `Bag_KALEM_damgayi_YENER_ve_hareket_TEK_KEZ_sayilir` | **BİREBİR** |
| **M08-5** | `FaturaHareketBagi.cs:160` — `Bagsiz` aday süzgeci | `A && !kalem` → `A \|\| !kalem` | `Bagsiz_kalemi_bagli_ama_damgasi_DUSMUS_hareketi_ADAY_SAYMAZ` | **BİREBİR** |
| **M08-6** | `FaturaHareketBagi.cs:133` — karşılıksız damga (fail-closed) | `if (!string.IsNullOrEmpty(h.FaturaNo))` → `if (false)` | `IlkBag_KARSILIKSIZ_damgayi_da_ENGELLER_fail_closed` | **BİREBİR** |
| **M08-7** | `FaturaHareketBagi.cs` — `GecerliFaturalar` IPTAL süzgeci | `&& f.Durum != "IPTAL"` kaldırıldı | `IlkBag_IPTAL_edilmis_faturayi_BAG_SAYMAZ` | **BİREBİR** |
| **M08-8** | `FaturaHareketBagi.cs:63` — fatura kümesi GENİŞLETİLMEZ | boş küme erken dönüşü, DB'nin tüm faturalarını çekmeyle değiştirildi | `Bag_kume_DISINDAKI_faturaya_BAGLANMAZ` · `IlkBag_IPTAL_edilmis_faturayi_BAG_SAYMAZ` (2) | **BİREBİR** |
| **M08-9** | `SatisRaporlari.cs:122` — Y-14 "bilinmiyor" kuralı | `adetBilinmiyor = h.Adet is null` → `= false` | `IslemDefteri_adeti_bilinmeyen_satirda_BILINMIYOR_der` | **BİREBİR** |

**Test açığı bulunmadı:** dokuz mutasyonun hepsi en az bir testi ADIYLA kırdı; hiçbir testi
güçlendirmek gerekmedi. Son mutasyondan sonra `dotnet build Kuyumcu.sln` → **0 Hata / 0 Uyarı**.

---

## § 4 — CANLI DOĞRULAMA (5198 · **5199'a DOKUNULMADI**)

### § 4.0 — ÖNCE BİR BULGU: 5198'de ESKİ BİR SÜREÇ DURUYORDU (md.1)

İlk canlı turda `nohup dotnet run … --urls 5198` **bağlanamadı ve kapandı**
(`api-5198.log` son satırı: `Application is shutting down...`, `Now listening` satırı **yok**).
İstekleri, önceki bir oturumdan kalmış **eski kodlu** bir süreç (PID 99291) karşıladı ve
`SATIS` satırlarına `adet` **NULL** yazdı. Bu, ürün hatası **değil**, ölçüm ortamı hatasıydı;
saklanmadı, çünkü aynı hata bir sonraki ajanı da yanıltabilirdi.

**Ders / kural önerisi:** canlı tur başlamadan `lsof -nP -iTCP:5198 -sTCP:LISTEN` boş
olmalı ve başlatılan sürecin log'unda **`Now listening on: http://localhost:5198`** satırı
görülmeden hiçbir ölçüm kanıt sayılmamalı.

Eski süreç durduruldu, güncel ikili yeniden başlatıldı (`Now listening` doğrulandı) ve
tüm ölçümler **tekrarlandı**. İkilinin güncelliği ayrıca doğrulandı: `Kuyumcu.Api.dll`
içinde `"Tanımsız hareket türü"` ve `"PersonelId boş dizge olamaz"` dizgeleri **mevcut**.

### § 4.1 — Adımlar (yanıt gövdeleri OKUNDU; `-o /dev/null` kullanılmadı)

Test kiracısı: **`PAKETP-08-TEST Kuyumculuk`** (`01M304VGS4881HQT4QWK9NPC03`).
Davet jetonu yerel üretildi, DB'ye **yalnız SHA-256 özeti** yazıldı; hiçbir sır rapora girmedi.

| # | Adım | Beklenen | Gerçek | SQL kanıtı |
|---|---|---|---|---|
| 0 | `GET /health` | 200 | **HTTP 200** | — |
| 1 | `POST /api/v1/auth/kayit` | 201 | **HTTP 201** | `tenants` 8 → 9 |
| 2 | `POST /api/v1/islemler/alis` adet=2 | 200 + `adet` deftere | **HTTP 200** · `"adet":2.0` | `hareketler.adet = 2.000` |
| 3 | `POST /api/v1/islemler/satis` adet=1 | 200 + `adet` deftere | **HTTP 200** · `"adet":1.0` | `hareketler.adet = 1.000` |
| 4 | `POST …/satis` (tekrar, 6000 TL) | 200 | **HTTP 200** · `"adet":1.0` | `adet = 1.000` |
| 5 | Tevsik sınırı üstü nakit alış (150.000 TL) | 400 | **HTTP 400** · 459 s. VUK metni | — |
| 6 | `GET /api/v1/faturalar/faturalanabilir-islemler` | satış aday | **HTTP 200** · 6 aday | — |
| 7 | `POST /api/v1/faturalar/fisten` (carisiz) | 400 | **HTTP 400** · "Fişte cari yok — fatura için cari verilmelidir" | — |
| 8 | `POST /api/v1/faturalar/fisten` (has > toplam) | 400 | **HTTP 400** · "Has altın değeri toplam tutardan küçük olmalıdır" | — |
| 9 | `POST /api/v1/faturalar/fisten` (geçerli) | 200 | **HTTP 200** · `FTR-2026-000001`, `kalemKaynagi:"KALEM"` | `fatura_kalemleri.hareket_id = 01M30551VRD0MA9XA419SJMW6B` |
| 10 | **AYNI hareket için İKİNCİ fatura** | 400 (fail-closed) | **HTTP 400** · "Bu hareket zaten FTR-2026-000001 ile faturalandırılmış" | — |
| 11 | Aday listesi (fatura sonrası) | faturalanan hareket **düşer** | **düştü** (7 aday, faturalanan yok) | — |
| 12 | `GET /api/v1/raporlar/islem-defteri` | yeni satır `adetBilinmiyor:false`, eski satır `true` | **HTTP 200** · aşağıdaki tablo | — |

### § 4.2 — Y-14 "bilinmiyor" kuralının CANLI ispatı

§ 4.0'daki eski sürecin yazdığı iki satır, tam da DoD'un tarif ettiği **"geçmiş satır"**
oldu ve rapor onları **varsayım üretmeden** işaretledi:

| Hareket | Tür | `adet` | `adetBilinmiyor` | `satanBilinmiyor` |
|---|---|---|---|---|
| `01M30551VRD0…` | SATIS | 1.0 | **false** | true |
| `01M3053CGV7R…` | ALIS | 2.0 | **false** | true |
| `01M304ZKWFWX…` | SATIS | **null** | **true** | true |
| `01M304XN6PEE…` | SATIS | **null** | **true** | true |

Adet hiçbir yerde `0` ile doldurulmadı, has gramdan **türetilmedi**.

### § 4.3 — Tür kapısının (Y-32) canlı yüzeyi YOKTUR — dürüstlük notu

`hareketler.Tur` değerini **kullanıcı gövdesinden alan hiçbir uç yoktur**
(`grep -rn 'Tur = dto\.'` → yalnız fatura/cari/fiyat-listesi türleri çıkar; hareket türü
her yerde sunucu sabiti). Bu yüzden tür kapısı için "geçersiz tür gönder → 400" biçiminde
bir **canlı HTTP adımı üretilemedi**; kapı, gelecekte yazılacak modülleri korur.
Kanıt zinciri: birim testleri (3 adet) + **M08-1 mutasyonu** + `denetim.sh` `HARETURU_DISI_SABIT`.
Uydurma bir uç açılmadı (kapsam kilidi).

### § 4.4 — Temizlik

- API PID durduruldu → `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır**.
- Test kiracısı, `tenant_id` kolonu olan **58 tablodan** + `kiraci_davetleri` satırı +
  `tenants` kaydı silindi.
- `SELECT COUNT(*) FROM tenants` → **8** (başlangıç 8) · `… WHERE ad LIKE 'PAKET%'` → **0**
  · `… WHERE ad LIKE 'QA-TEST-%'` → **3** (fikstürler **değiştirilmedi**).
- Jeton dosyaları `printf '' >` ile boşaltıldı.
- 5199 ve 5199'un yayın anlık görüntüsü **hiç açılmadı**.

---

## § 5 — DENETİM ÇIKTISI (P-08 bloğu)

```
═══ P-08 hareket türü sözleşmesi + fatura bağı ═══
  ✓ P-08: HARETURU_DISI_SABIT                      0 (-eq 0)
  ✓ P-08: TUR_KAPISI + ADET/PERSONEL KAPISI        HareketFabrikasi'nda kapı, 400 dalı
  ✓ P-08: HAREKETFABRIKASI_DISI_YAZMA              0 (-eq 0)
  ✓ P-08: FATURA_NO_SERBEST_METIN                  2 (azami 2, cırcır)
  ✓ P-08: BAG_BUTUNLUGU                            tek sorgu yolu + ADR-0018 + ters yön kolonu YOK
  ✓ P-08: PERSONEL_ANLAM_KILIDI + ADET             SATAN sabiti + nullable adet + indeks
DENETİM TEMİZ.
```

Sayaçlar `# >>> P-08` / `# <<< P-08` işaretli blok içinde (`denetim.sh:1874-2001`),
ölçüm komutları betiğin içine yorum olarak yazılı (BK-07). Tavan **artırılmadı**:
`FATURA_NO_SERBEST_METIN` cırcırı **2** (P-08 öncesi 7 → sonrası 2).

---

## § 6 — KİMLİK KAPANIŞ TABLOSU

| Kimlik | Durum | Kanıt |
|---|---|---|
| **Y-32** — `HareketTurleri` sözleşmesi kodda zorlansın | **KAPANDI** | `Data/HareketFabrikasi.cs:94` (kapı, `BadRequestException` → 400) · testler `TurKapisi_ListedeOlmayanTur_400_doner`, `TurKapisi_BosTur_400_doner`, `TurKapisi_ListedekiTum_turler_KABUL_edilir` · mutasyon **M08-1** · sayaç `HARETURU_DISI_SABIT = 0`. *Canlı HTTP yüzeyi yok — § 4.3.* |
| **Y-14** — `hareketler` tablosuna adet kolonu | **KAPANDI** | migration `20260918150025_HareketAdetPersonel` (Up **ve koşturulmuş Down**, § 2) · `Entities.cs:729` · kapı `HareketFabrikasi.cs:103` · rapor `SatisRaporlari.cs:122` · testler `AdetKapisi_NegatifAdet_400_doner`, `Adet_NULL_kalir_ve_0_ile_DOLDURULMAZ`, `Adet_yazildiysa_uc_ondalikla_korunur`, `IslemDefteri_adeti_bilinmeyen_satirda_BILINMIYOR_der`, `Satis_hareketi_adedi_DEFTERE_yazar_ve_iade_onu_TURETMEZ` · mutasyonlar **M08-2, M08-9** · canlı § 4.1 adım 2-4 ve § 4.2 |
| **Y-13** — fişe satıcı personel alanı | **KISMEN** | **Defter ayağı KAPANDI:** `hareketler.personel_id` kolonu + `(tenant, personel_id, tarih)` indeksi + `Sabitler.HareketPersonelRolu = "SATAN"` anlam kilidi (`Entities.cs:62`) + boş-dizge kapısı (`HareketFabrikasi.cs:110`) + rapor alanları `personelId`/`satanBilinmiyor` (`SatisRaporlari.cs:124-125`) · test `PersonelId_SATAN_anlamiyla_kilitlidir_ve_KullaniciId_den_AYRIDIR` · mutasyon **M08-3**. **AÇIK:** hiçbir uç bu kolonu **yazmıyor** (`grep -rn PersonelId` → yalnız kapı/indeks/rapor). Canlı turda tüm satırlarda `satanBilinmiyor = true`. **Devredildi: POS/fiş formu ayağı P-14/P-16'ya** (kapsam kilidi: bu pakette yeni ekran/DTO alanı açılmadı). Prim hesabı zaten D-2 (1.1). |
| **Y-25** — `fatura_no` serbest metnin emekliye ayrılması | **KAPANDI** | `Data/FaturaHareketBagi.cs` **tek sorgu yolu** (otorite `fatura_kalemleri.hareket_id`, damga yalnız bu dosya içinde `Kaynak="DAMGA"`) · ADR **`dokumanlar/02-mimari/adr/0018-fatura-hareket-bagi-tek-yon.md`** (BK-04 karşılandı) · `hareketler.fatura_id` kolonu **AÇILMADI** (`information_schema` doğrulandı) · testler `Bag_OTORITE_…`, `Bag_ESKI_SATIRDA_damga_…`, `Bag_KALEM_damgayi_YENER_…`, `IlkBag_IPTAL_…`, `Bagsiz_…`, `IlkBag_KARSILIKSIZ_…`, `IlkBag_kalemi_bagli_ama_DAMGASIZ_…`, `Bag_kume_DISINDAKI_…` · mutasyonlar **M08-4…M08-8** · sayaç `FATURA_NO_SERBEST_METIN = 2 (azami 2)` · canlı § 4.1 adım 9-11 |
| **B-52** — 34-A denetim zenginleştirme dilimi | **KAPANDI (bu paketin dilimi)** | Deftere yazan tek yol `HareketFabrikasi.Ekle` korunuyor; denetim kaydı `DenetimYazmaDinleyicisi` ile **otomatik** yazılır (yeniden yazılmadı, DoD sözleşmesi) — yeni `adet`/`personel_id` alanları da `Added` girişinin parçası olarak denetim izine girer · sayaç `HAREKETFABRIKASI_DISI_YAZMA = 0` · mutasyonla korunan kapılar M08-1…M08-3. *Denetim izi içeriğinin genişletilmesi (alan değerleri) ADR-0004 gereği bilinçli olarak YAPILMAZ — yalnız alan adları yazılır.* |

---

## § 7 — DoD SÖZLEŞMESİ (hangi kayıt nasıl düzeltilir)

| Varlık / kayıt | Düzenleme yolu | Gerekçe |
|---|---|---|
| `hareketler` satırı (`adet`, `personel_id` dâhil) | **SALT OKUNUR — PATCH/DELETE ucu YOK.** Düzeltme yolu: `StornoServisi` (tek storno motoru) ile ters kayıt | Finansal defter; ADR-0004 append-only + PLAN § 1 ilke 8. Ekranda `DegismezIpucu` ile "bu satır düzeltilmez, storno ile geri alınır" anlatılır |
| `fatura_kalemleri.hareket_id` bağı | Faturanın kendi iptal/storno yolundan; kalem tek başına düzenlenmez | Belge içeriği VUK md.253 boyunca sabit (FaturaKalemi.cs başlığı) |
| `hareketler.fatura_no` damgası | **Yeni yazılmaz, emekli.** Yalnız `FaturaHareketBagi` içinde geriye dönük okunur | ADR-0018 · sayaç `FATURA_NO_SERBEST_METIN` cırcırı |

**Bu pakette yeni varlık/kayıt türü DOĞMADI** (iki kolon eklendi), bu yüzden yeni
PATCH/DELETE ucu, yeni `[RolGerekli]`, yeni ekran eylemi gerekmedi. Hard DELETE eklenmedi.

---

## § 8 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| (a) Her iş kuralı için testi var | ✅ | 19 test, `Kuyumcu.Tests/P08HareketSozlesmeTestleri.cs` |
| (b) Mutasyon kanıtı | ✅ | § 3 — 9 mutasyon, 9'u kırmızı, 9'u `cmp` BİREBİR |
| (c) 5198'de canlı doğrulama | ✅ (tür kapısı hariç, § 4.3) | § 4 — HTTP kodları + SQL |
| (d) `dotnet/denetim.sh` TEMİZ | ✅ | § 5 |
| (e) Tam süit 0 başarısız | ✅ | 3032/3032 |
| (f) Test kiracısı silindi | ✅ | tenants 8, `PAKET%` 0, `QA-TEST-%` 3 dokunulmadı |
| (g) Uygulama raporu | ✅ | bu dosya |
| BK-04 (ADR yazılmadan bağ migration'ı yok) | ✅ | ADR-0018 mevcut; ayrıca **bağ migration'ı hiç açılmadı** |
| PLAN § 1 ilke 10 (`Down()` koşturulur) | ✅ | § 2 — geri alma + yeniden uygulama kanıtlı |
| Sayaç tabanı ölçüldü, kopyalanmadı | ✅ | `denetim.sh` P-08 bloğu canlı ölçüyor; bu turda 6/6 ✓ |
| Tavan artırılmadı | ✅ | `FATURA_NO_SERBEST_METIN` azami **2**, değişmedi |
| COMMIT YOK / git komutu yok | ✅ | yalnız `git status --porcelain` okundu |
| Kaynak kodda kalıcı değişiklik yok | ✅ | `cmp` × 9 BİREBİR; değişen tek dosya bu rapordur |
| 5199'a dokunulmadı | ✅ | 5199 boyunca çalışmaya devam etti |

---

## § 9 — DEĞİŞEN DOSYALAR (bu tur)

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısındaki değişiklikler
**bu turdan önce** de vardı (çalışma ağacı 18-19 Eylül'den beri kirli, commit yasak).
**Bu turun eklediği tek dosya:**

- `dokumanlar/09-uygulama-kayitlari/PAKET-P-08-uygulama-raporu-TUR-2-2026-09-20.md` (bu dosya)

Kaynak kod dosyaları (`HareketFabrikasi.cs`, `FaturaHareketBagi.cs`, `SatisRaporlari.cs`)
mutasyon turundan **birebir aynı** çıktı — `cmp` kanıtı § 3'te.

---

## § 10 — AÇIK KALANLAR

1. **Y-13'ün yazma ayağı yok.** `hareketler.personel_id` hiçbir uçtan doldurulmuyor;
   canlı turda tüm satırlar `satanBilinmiyor = true`. **P-14/P-16'ya devredildi.**
   Bu kapanmadan "satıcı bazlı satış raporu" **iddia edilemez**.
2. **Tür kapısının canlı HTTP kanıtı yok** (§ 4.3) — yüzey olmadığı için; uydurma uç açılmadı.
3. **5198'de başıboş süreç riski** (§ 4.0) — `kapi.sh`'a "canlı tur öncesi 5198 boş olmalı +
   `Now listening` satırı görülmeli" kontrolü eklenmesi önerilir. Bu, P-02'nin kapı betiği
   kapsamıdır; **bu pakette yapılmadı** (kapsam kilidi).
4. **`Pages/` altındaki `.mutorj` artıkları** ve `Data/HareketFabrikasi.cs.mutorj` hâlâ
   duruyor (silme K-4 ile kurucudadır; hook `rm`'i bloklar). Bu turda hiçbir yeni artık
   bırakılmadı.

---

## § 11 — KURUCUYA / MALİ MÜŞAVİRE NOT

- **Mali müşavire bir şey sorulmadı:** bu paket mevzuat hükmü koda sabitlemiyor.
  Canlı turda karşılaşılan 459 s. VUK tevsik kapısı (30.000 TL) **mevcut** bir kuraldır
  ve yapılandırmadan okunur (ADR-0005); bu pakette değiştirilmedi.
- **Kurucuya:** `Pages/` ve `Data/` altındaki `.mutorj` artıklarının silinmesi (K-4)
  hâlâ bekliyor; grep tabanlı sayaçları kirletiyorlar.
- **Kurucuya:** 5198 portunda önceki oturumlardan kalan süreçler ölçümleri sessizce
  bozabiliyor (§ 4.0). Oturum kapanışında bu portun boşaltılması iyi olur.

---

**Tahmin / gerçekleşen:** plan bu paket için 180 ajan-dk öngörmüştü; paketin **kodu**
önceki turda yazılmıştı. Bu **kanıt turu** ≈ 45 ajan-dk sürdü (mutasyon 9 koşum,
canlı tur, `Down()` tatbikatı, tam süit). Toplam sapma kapı kaydına yazılacaktır.
