# PAKET UX-1 — Faz A uygulama raporu

> Paket: UX-1 "Her veri girilen yer düzeltilebilir" — **Faz A** (politika haritası + GerekceAlani + DegismezIpucu)
> Tarih: 2026-09-16 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) rolü, Opus · Dalga 1
> Durum: **BİTTİ** (Faz A kapsamı). Faz B/C/D bu pakette AÇILMADI.

---

## 1. Yapılanlar

### Sunucu — tek doğruluk kaynağı
`dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs` (YENİ)

- `Politikalar` — üç damga: `DUZENLENEBILIR` · `STORNO` · `SALT_OKUNUR`.
- `GerekceAnahtarlari` — 13 anahtar (SERBEST, DEFTER, DENETIM, MASAK, SISTEM, TURETILMIS, KAPANIS, SAYIM, DISA_AKTARIM, BELGE, FATURA, UC_YOK, STORNO_YOK). **Haritada çıplak metin yok**, yalnız anahtar.
- `VarlikPolitikasi` kaydı: `Varlik · Politika · GerekceAnahtari · HedefUc · AlternatifYol`.
- `DuzeltmePolitikasi.Varliklar` — **32 varlık** (KuyumcuDbContext'teki HER DbSet).
- `DuzeltmePolitikasi.HareketTurleri` — **14 hareket türü** (`Sabitler.HareketTurleri`'nin tamamı).
- `MetaController` — `GET /api/v1/meta/duzeltme-politikalari`, sınıf düzeyinde `[Authorize]` (Mehmet K8). Kiracıya özel veri döndürmez.

### İstemci
- `dotnet/Kuyumcu.Web/Servisler/DuzeltmePolitikaServisi.cs` (YENİ) — haritayı **oturumda bir kez** çeker ve önbellekler (Bertan koşul 4 / TB-09). Sayfa başına çağrı yok.
- `dotnet/Kuyumcu.Web/Bilesenler/GerekceAlani.razor` (YENİ) — etiket + `<textarea>` + canlı sayaç + `ChildContent(bool gecerli)` ile Kaydet kilidi. `Id`, `AsgariUzunluk` (varsayılan **20**), `AzamiUzunluk` (1000), `Satir`, `Etiket` parametre.
- `dotnet/Kuyumcu.Web/Bilesenler/DegismezIpucu.razor` (YENİ) — `#degismez-ipucu-{Varlik}`, `role="note"`, kilit ikonu + gerekçe + (varsa) alternatif yol. **Hiç `<button>` üretmez** — devre dışı düğme de çizilmez.
- `dotnet/Kuyumcu.Web/Program.cs` — `DuzeltmePolitikaServisi` DI kaydı.
- `dotnet/Kuyumcu.Web/wwwroot/css/app.css` — `.degismez-ipucu` ve `.gerekce-alani` kuralları. **Sabit renk yok**, yalnız `var(--kenarlik)`, `var(--uyari-vurgu)`, `var(--yuzey-2)`, `var(--metin-guclu)`, `var(--metin-soluk)`, `var(--yazi-sm)`, `var(--yaricap-sm)`. Satır içi `style` yok.

### Sözlük
`dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — `SozlukDuzeltme` sınıfı (TR + EN, 20 alan) + `Sozluk.Duzeltme` + `GerekceMetni(anahtar)` çözücü.

### Mevcut sayfalar
**SIFIR DOM DEĞİŞİKLİĞİ** (Bertan koşul 2). Hiçbir `Pages/*.razor` dosyasına dokunulmadı; mevcut element id'leri aynen duruyor. Mevcut dört gerekçe alanı (Kasalar `#gun-aciklama`, Cariler `#dz-gerekce`, VeriDisaAktarim `#da-gerekce`, Uyum `#sb-gerekce`) **bu fazda sarılmadı** (Bertan koşul 3).

---

## 2. Spesifikasyondan sapmalar ve gerekçeleri

| # | Sapma | Gerekçe |
|---|---|---|
| S1 | **`Reyon` haritada `SALT_OKUNUR` / `UC_YOK` damgalı** (spesifikasyonun hedefi `DUZENLENEBILIR`). | Faz A'da reyon PATCH/DELETE ucu **yok** (`KategoriModulu.cs` ReyonController yalnız GET+POST). Canlıda doğrulandı: `PATCH /api/v1/reyonlar/{id}` → **405**. Harita bugünün gerçeğini söyler; Faz B ucu açtığında satır `DUZENLENEBILIR`'e döner. Anayasa md.1: olmayan uç "var" diye damgalanmaz. |
| S2 | **`DuzeltAkisi.razor` yazılmadı.** | Bertan koşul 2 — Faz C'ye ertelendi (nihai iç mantığı paket 48'e bağlı). |
| S3 | **Faz B/C işleri (reyon PATCH/DELETE, tenant PATCH, kasa DTO genişletme, banka DELETE, fiyat rol kapısı K1, ekran bağlamaları) yapılmadı.** | Görev tanımı "Faz A". Bu kalemler Faz B/C'ye aittir; Mehmet K1 (fiyat uçlarına `[RolGerekli]`) **Faz C'den önce Faz B'de** yapılmalı — açık kalan olarak taşındı. |
| S4 | **Migration YOK.** | Faz A yalnız kod + bileşen; şema değişikliği yok. `tenants` kimlik kolonları (`IsletmeKimlikBilgileri`) Faz B'nin işi. |
| S5 | `Fatura` varlığı `STORNO` damgalı (spesifikasyon ayrı damga demiyordu). | `PATCH /faturalar/{id}/durum` var ama tutar/kalem yerinde düzeltilmez. Açık soru 4 (fatura IPTAL storno mudur) **mali müşavir teyidi** bekliyor — bkz. §9. |
| S6 | `HttpDuzeltmePolitikaTestleri.cs` spesifikasyonun test listesinde yoktu; eklendi. | `[Authorize]` bir denetleyici niteliğidir; saf test görmez (Mehmet K8'in kanıtı gerçek HTTP ister). |
| S7 | Ek mutasyon kanıtı M-UX1-A1 ve M-UX1-A2 eklendi. | Aşkın K5 (yansıma testi) ve Mehmet K8 (dinamik çağrı yasağı) testlerinin gerçekten ısırdığını kanıtlamak için. |

### Spesifikasyonun "mevcut durum" iddialarının koda karşı doğrulaması
- ✅ `ReyonController` yalnız GET+POST — **doğru** (canlı 405).
- ✅ `Fiyat` PATCH uçları var, ekran yok — **doğru**.
- ✅ `Tenant`'ta adres/vergi_dairesi/vergi_no/telefon kolonu yok — **doğru**.
- ❌ "Paket 33 YARIM / `YazmaKaydet` YOK / `HareketTurleri`'nde SATIS_IADE-ALIS_IADE-CARI_DUZELTME eksik / Cari PATCH `CariModulu.cs:614`" — **hepsi BAYAT** (Mehmet K11 + Aşkın K1 ile aynı bulgu). Kodda: `CariModulu.cs:178` CariDuzeltmeDto, `:555` Duzeltme servisi, `:746` POST ucu, `:726` Cari PATCH; `Entities.cs:24-33` üç tür ekli; `DenetimIzi.cs` `YazmaKaydet` mevcut. Harita bu **güncel** gerçeğe göre kuruldu (`CARI_DUZELTME` hareket türü haritada, hedefi `api/v1/cari/{id}/duzeltme`).
- ⚠ `.mutorj` artıkları (Bertan koşul 6): `Fiyatlandirma` ve `VadeTakibi` `.mutorj` dosyaları `.razor` ile **birebir aynı**; `Islemler`, `Sayim`, `Urunler` **FARKLI** — ama fark, `.mutorj`'un paket 21/paket 4 öncesi eski bir yedek olmasından geliyor (`.razor` daha zengin), yarım kalmış bir mutasyon değil. Faz A bu dosyaların **hiçbirine dokunmadı**, dolayısıyla koşul tetiklenmedi. Faz C'den önce Kurucu'ya eskale edilmeli (silme yasağı gereği dosyalar yerinde bırakıldı).

### Mevzuat / "DOGRULANMALI" hükümleri
Spesifikasyonda "Mevzuat dayanağı: (yok)". Faz A'da hiçbir mevzuat hükmü koda **sabit** yazılmadı; VUK md.219 ve ADR-0004 yalnız **gerekçe metni** olarak (sözlükte, TR+EN) görünür — davranışı belirleyen bir eşik/oran/had yok. Yapılandırmadan okunması gereken bir hüküm bu fazda doğmadı.

---

## 3. Derleme

```
$ dotnet build Kuyumcu.sln --nologo
Oluşturma başarılı oldu.
    0 Hata
    1 Uyarı   ← xUnit1031, Kuyumcu.Tests/RotaBaslikTestleri.cs(155,67) — ÖNCEDEN VARDI, UX-1 ile ilgisiz
```
Son mutasyondan sonra temiz yeniden derleme yapıldı: **0 hata**.

## 4. Migration

**gerekmedi** — Faz A yalnız kod + bileşendir, şema değişikliği yoktur. `kuyumcu_net` üzerinde migration çalıştırılmadı; migration sayısı 22'de kaldı.

## 5. Testler

| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Yeni saf + bUnit (`~DuzeltmePolitikasiTestleri\|~DuzeltmeEkranTestleri`) | 26 | 26 | 0 | 0 |
| Yeni HTTP (`~HttpDuzeltmePolitikaTestleri`) | 2 | 2 | 0 | 0 |
| **Tam süit** (`--logger trx`) | **1107** | **1107** | **0** | **0** |

TRX sayaçları: `total="1107" executed="1107" passed="1107" failed="0" error="0" timeout="0" aborted="0"`.
Önce: 1079 · Sonra: 1107 · **Yeni test: 28** (18 saf, 8 bUnit, 2 HTTP).

Yeni test dosyaları:
- `Kuyumcu.Tests/DuzeltmePolitikasiTestleri.cs` — envanter tamlığı (DbSet yansıması), uydurma varlık yok, üç damga, defter türleri/varlıkları DUZENLENEBILIR değil, denetim/MASAK/dışa aktarım SALT_OKUNUR, SALT_OKUNUR'a HedefUc verilmez, **HedefUc gerçek uca işaret eder (yansıma: `[Route]`+`[HttpPatch/Put/Delete]`, storno için `[HttpPost]` dahil)**, defter tablolarına yazma ucu açılmamıştır, gerekçe anahtarlarının TR+EN karşılığı vardır, **ekran HedefUc'u dinamik çağırmaz** (kaynak taraması).
- `Kuyumcu.Tests/DuzeltmeEkranTestleri.cs` — GerekceAlani 19/20 kilidi, boşluk saymaz, `AsgariUzunluk=300` (Aşkın K2), sayaç/etiket/maxlength, `DegerChanged`; DegismezIpucu pozitif+negatif eş (Aşkın K4), alternatif yol, alternatif yoksa satır çizilmez. **Ekran sözleşmesi politika haritasından sürülür** — testin içine sabit politika yazılmadı.
- `Kuyumcu.Tests/HttpDuzeltmePolitikaTestleri.cs` — anonim 401, TEZGAHTAR 200 + camelCase sözleşme + kiracı verisi sızmaz.

## 6. Mutasyon tablosu

| Kod | Değişiklik | Kırmızıya dönen testler | Geri alındı (cmp) |
|---|---|---|---|
| **M-UX1-01** | `DuzeltmePolitikasi.cs`: `S("DenetimKaydi", …Denetim)` → `D("DenetimKaydi", "api/v1/kullanicilar/{id}")` | `DuzeltmePolitikasiTestleri.Denetim_izi_salt_okunurdur`, `DuzeltmePolitikasiTestleri.Defter_varliklari_DUZENLENEBILIR_DEGILDIR`, `DuzeltmeEkranTestleri.Denetim_satirinda_duzenle_yoktur_ipucu_vardir`, `HttpDuzeltmePolitikaTestleri.TEZGAHTAR_haritayi_okuyabilir_ve_sozlesme_camelCase_tir` | `cmp` = AYNI |
| **M-UX1-05** | `GerekceAlani.razor`: `AsgariUzunluk … = 20` → `= 0` | `DuzeltmeEkranTestleri.Gerekce_kisa_iken_kaydet_kapali`, `DuzeltmeEkranTestleri.Gerekce_bosluklari_SAYILMAZ` | `cmp` = AYNI |
| **M-UX1-07** | `DegismezIpucu.razor`: kilit `<span>` → `<button class="dugme dugme-ikincil" disabled="@(false)">Düzenle</button>` (⚠ dizge tuzağı için `@(false)` kullanıldı) | `DuzeltmeEkranTestleri.Denetim_satirinda_duzenle_yoktur_ipucu_vardir`, `DuzeltmeEkranTestleri.Degismez_ipucu_ALTERNATIF_yolu_gosterir` | `cmp` = AYNI |
| **M-UX1-A1** (ek) | `D("Kasa", "api/v1/kasalar/{id}")` → `"api/v1/kasalar/{id}/para-birimi"` (olmayan uç) | `DuzeltmePolitikasiTestleri.Duzenlenebilir_varligin_HedefUc_u_GERCEK_bir_yazma_ucudur` | `cmp` = AYNI |
| **M-UX1-A2** (ek) | `DuzeltmePolitikaServisi.cs`'e `Uygula(satir, govde) => api.PatchAsync<object>(satir.HedefUc!, govde)` eklendi | `DuzeltmePolitikasiTestleri.Ekran_HedefUc_u_DINAMIK_CAGIRMAZ` | `cmp` = AYNI |

**Kapsam dışı mutasyonlar:** M-UX1-02 (reyon şube kapısı), M-UX1-03 (kasa hareket kapısı), M-UX1-04 (tenant PATCH koru/sil) **Faz B**'ye; M-UX1-06 (ürün tam kart) **Faz C**'ye aittir. Bu fazda o kod yollarının hiçbiri yazılmadı, dolayısıyla mutasyonları da koşulmadı. Test açığı bulunmadı: her Faz A mutasyonu en az bir testi kırdı.

## 7. Denetim çıktısı

```
═══ Kiracı izolasyonu denetimi ═══
  ✓ IgnoreQueryFilters (yasak)                     0
  ✓ new KuyumcuDbContext (yalnız fabrika)          1
  ✓ Find/FindAsync (yasak)                         0
  ✓ ExecuteUpdate/Delete (beyaz liste 4)           4
  ✓ Hareketler.Add (yalnız HareketFabrikasi)       1
  ✓ VeresiyeKapanisi (yalnız Etiketten)            2
  ✓ IKiraciVarlik uygulayan varlık                31
  ✓ HasQueryFilter                                 2

═══ Güvenlik yüzeyi ═══
  ✓ oran sınırı / HTTPS yönlendirme / sıkıştırma / sağlık ucu / CORS beyaz liste

DENETİM TEMİZ.
```

## 8. Canlı doğrulama (localhost:5198 — 5199'a dokunulmadı)

Test kiracısı: **PAKETUX-1-TEST** (`tenantId=01M2MGC6TCBX1RQSTSNYY4C1V0`), `POST /auth/kayit` → **201**.

| Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| `GET /api/v1/meta/duzeltme-politikalari` **tokensiz** | 401 | **401** | gövde: `{"message":"Token bulunamadı","error":"Unauthorized","statusCode":401}` |
| `GET /api/v1/meta/duzeltme-politikalari` **SAHIP token** | 200 + camelCase | **200** | `varliklar=32`, `hareketTurleri=14`; `DenetimKaydi=SALT_OKUNUR/DENETIM`; `SATIS=STORNO/api/v1/islemler/satis-iade`; `Reyon=SALT_OKUNUR/UC_YOK` |
| SALT_OKUNUR satırda `hedefUc` | `null` | **hiç yok** | perl taraması: "SALT_OKUNUR-AMA-UC-VAR" satırı üretilmedi |
| Hareket türlerinde DUZENLENEBILIR | yok | **yok** | perl taraması: "DUZENLENEBILIR-HAREKET" satırı üretilmedi |
| Yanıt kiracı verisi taşıyor mu | hayır | **hayır** | `grep -c "$TENANT_ID" pol.json` → `0` |
| `PATCH /api/v1/reyonlar/{id}` (harita `UC_YOK` diyor) | uç yok | **405** | harita dürüst — S1 sapmasının kanıtı |
| `PATCH /api/v1/kasalar/{id}` (harita `DUZENLENEBILIR` diyor) | uç var | **400** (doğrulama hatası, 405 değil) | gövde: `{"message":["Kasa adı en az 2 karakter olmalıdır"],…}` — uç gerçekten var |
| Meta ucu deftere/denetime yazıyor mu | hayır | **hayır** | 3 çağrı öncesi/sonrası `denetim_kayitlari` = **13 → 13**; `hareketler` = **0** |

Temizlik:
- API süreci (PID 41965) durduruldu; `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**.
- Test kiracısı `information_schema`'dan çıkarılan **31 tenant_id'li tablodan** + `tenants`'tan silindi.
- `SELECT COUNT(*) FROM tenants` → **5** (başlangıç değeri 5). `WHERE ad LIKE 'PAKETUX-1-TEST%'` → **0**. `kullanicilar`/`denetim_kayitlari` (o tenant) → **0**.

SQL (parola değişkenden, ekrana basılmadı):
```sql
SELECT COUNT(*) FROM tenants;                                  -- 5
SELECT COUNT(*) FROM tenants WHERE ad LIKE 'PAKETUX-1-TEST%';  -- 0
SELECT COUNT(*) FROM hareketler          WHERE tenant_id='…';  -- 0
SELECT COUNT(*) FROM denetim_kayitlari   WHERE tenant_id='…';  -- 0
```

## 9. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` içinden **bu pakete ait** olanlar:

**YENİ**
- `dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs`
- `dotnet/Kuyumcu.Web/Bilesenler/DegismezIpucu.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/GerekceAlani.razor`
- `dotnet/Kuyumcu.Web/Servisler/DuzeltmePolitikaServisi.cs`
- `dotnet/Kuyumcu.Tests/DuzeltmePolitikasiTestleri.cs`
- `dotnet/Kuyumcu.Tests/DuzeltmeEkranTestleri.cs`
- `dotnet/Kuyumcu.Tests/HttpDuzeltmePolitikaTestleri.cs`

**DEĞİŞİKLİK**
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `dotnet/Kuyumcu.Web/Program.cs`
- `dotnet/Kuyumcu.Web/wwwroot/css/app.css`

(Listedeki diğer `M`/`??` satırları önceki paketlerden gelir; UX-1 onlara dokunmadı. **Commit yapılmadı.**)

## 10. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Bertan K2 — Faz A yalnız GerekceAlani + DegismezIpucu + DuzeltmePolitikasi + testler; DuzeltAkisi Faz C'ye; sayfalara sıfır DOM değişikliği | **KARŞILANDI** | Hiçbir `Pages/*.razor` değişmedi; `DuzeltAkisi.razor` yazılmadı |
| Bertan K3 — mevcut 4 gerekçe alanı bu fazda sarılmaz | **KARŞILANDI** | Kasalar/Cariler/VeriDisaAktarim/Uyum dosyalarına dokunulmadı |
| Bertan K4 — sayfalara yeni API çağrısı eklenmez; politika tek istemci servisinden, oturumda bir kez | **KARŞILANDI** | `DuzeltmePolitikaServisi.Yukle()` tek `cekim` Task'ıyla önbellekler; sayfa çağrısı 0 |
| Bertan K6 — `.mutorj` yedekleri diff'lenir | **KARŞILANDI (uyarıyla)** | Fiyatlandirma/VadeTakibi AYNI; Islemler/Sayim/Urunler FARKLI ama eski paket yedeği. Faz A bu dosyalara dokunmadı → iş durdurulmadı, §2'de eskale notu düşüldü |
| Aşkın K2 — `AsgariUzunluk` parametredir, varsayılan 20, 300 desteklenir | **KARŞILANDI** | `Asgari_uzunluk_PARAMETREDIR_300_de_uygulanir` (299 kapalı / 300 açık) |
| Aşkın K3 — M-UX1-05 izolasyonu: diğer kapatma sebepleri sağlanır, 19/20 aynı testte | **KARŞILANDI** | `Alan()` kurucusunda `kaydediliyor=false`, `tutar=100` sabit; mutasyon 2 test kırdı |
| Aşkın K4 — negatif iddia pozitif eşle çiftlenir | **KARŞILANDI** | `#degismez-ipucu-DenetimKaydi` VAR + `role=note` + sözlük metni; `button` YOK |
| Aşkın K5 — HedefUc testi YANSIMAYLA | **KARŞILANDI** | `Uclar()` ControllerBase + `[Route]` + `[HttpPatch/Put/Delete/Post]`; `Yansima_uc_envanteri_BOS_DEGILDIR` yansımanın kendisini korur; M-UX1-A1 testin ısırdığını kanıtlar |
| Mehmet K8 — `/meta/…` `[Authorize]`; HedefUc yalnız yol dizgesi, istemci dinamik çağırmaz | **KARŞILANDI** | Canlı 401/200; `Ekran_HedefUc_u_DINAMIK_CAGIRMAZ` kaynak taraması + M-UX1-A2 |
| K-UX-2 — defter tablolarına PATCH/DELETE ucu açılmaz | **KARŞILANDI** | `Defter_tablolarina_PATCH_veya_DELETE_ucu_ACILMAMISTIR`; `ExecuteUpdate/Delete` beyaz listesi 4'te kaldı |
| Politika üç değerden biridir (DUZENLENEBILIR/STORNO/SALT_OKUNUR) | **KARŞILANDI** | `Politika_degerleri_UC_TANEDIR` |
| Sozlukler TR+EN, çıplak metin yok | **KARŞILANDI** | `Gerekce_anahtarlarinin_TR_ve_EN_karsiligi_VARDIR` (TR≠EN de sınanır) |
| Yeni CSS'te sabit renk yok, belirteç `var()`; satır içi style yok | **KARŞILANDI** | app.css eki yalnız `var(--…)`; `TasarimBelirtecTestleri` ham hex tavanı yeşil (tam süit) |
| Mevcut element id'leri değişmez | **KARŞILANDI** | Hiçbir mevcut sayfa/id'ye dokunulmadı |
| Migration prosedürü | **GEREKMEDİ** | Şema değişikliği yok |
| Test kiracısı silinir, tenants sayısı döner | **KARŞILANDI** | 5 → 6 → **5** |
| COMMIT YOK | **KARŞILANDI** | Çalışma ağacında kalındı |

## 11. Açık kalanlar

1. **Faz B (sıradaki iş):** Reyon PATCH/DELETE, `PATCH /admin/tenant` (+`IsletmeKimlikBilgileri` migration), KasaGuncelleDto genişletme (Bertan koşul 1: önce `Ad` `[Required]` kaldırılıp "gövdede yoksa koru" desenine geçirilecek), Banka DELETE. Bunlar açıldıkça harita satırları `SALT_OKUNUR/UC_YOK` → `DUZENLENEBILIR` olur ve mevcut testler bunu otomatik zorlar.
2. **Mehmet K1 — BLOKLAYICI, Faz C'den ÖNCE:** `FiyatModulu.cs:227-238` sınıf düzeyinde yalnız `[ApiController, Authorize]`; `ListeOlustur/ListeGuncelle/KampanyaOlustur/KampanyaGuncelle` üzerinde `[RolGerekli]` **YOK** → bugün TEZGAHTAR fiyat marjını değiştirebilir. Bu pakette harita `FiyatListesi`/`Kampanya`'yı DUZENLENEBILIR damgaladı ama **ekran bağlanmadı**, dolayısıyla açık genişlemedi. Faz B'de `[RolGerekli("SAHIP","MUDUR")]` + TEZGAHTAR 403 HTTP testi gerekir.
3. **`.mutorj` artıkları:** `Islemler/Sayim/Urunler .razor.mutorj` dosyaları karşılık gelen `.razor` ile farklı (eski paket yedeği). Silme yasağı gereği yerinde bırakıldı — Kurucu'nun elle temizlemesi gerekir. Faz C bu dosyalara dokunmadan önce tekrar diff alınmalı.
4. **`DuzeltAkisi.razor`** (Faz C) ve **paket 48 storno motoru** (Faz D). Bugün `STORNO_YOK` anahtarlı 9 hareket türü ekranda düğme değil **gerekçe** gösterecek.
5. **Mevcut dört gerekçe alanının bileşene sarılması** (Bertan koşul 3'ün ikinci adımı) — her sarmada ilgili bUnit testi değişmeden yeşil kalmalı.
6. **Gün kapanışı fark gerekçesine asgari uzunluk gelecek mi?** `Kasalar.razor #gun-aciklama` bugün `<input>` ve asgari 0; `KasaModulu.cs` `MinLength(20)` yalnız *yeniden açma* gerekçesine ait. Aşkın K2: "ayrı karar, varsayılmaz" — **karar bekliyor**.
7. **Denetim izine YAZMA kaydı** açık sorusu: `DenetimServisi.YazmaKaydet` **mevcuttur** (`Data/DenetimIzi.cs`), paket 33 onu kullanıyor. Faz A'da yazma yok, dolayısıyla çağrılmadı; Faz B'de tenant kimlik değişimi Mehmet K3 gereği `YazmaKaydet(…, hassas:true)` ile aynı transaction'da kaydedilecek.

## 12. Kurucuya / mali müşavire notlar

- **Mali müşavire (Mustafa üzerinden):** *Fatura iptali bir storno sayılır mı?* Bugün `PATCH /faturalar/{id}/durum` ile `IPTAL` yazılıyor ama ters kayıt üretilmiyor. Harita `Fatura`'yı ihtiyatlı biçimde `STORNO` damgaladı (yerinde düzeltme kapalı). Muhasebe açısından "durum=IPTAL" yeterli mi, yoksa ters kayıt şart mı — **teyit gerekir**. Cevap "ters kayıt şart" ise paket 48'in kapsamına fatura da girer.
- **Mali müşavire:** *Emanet giriş/çıkış için düzeltme yolu.* Bugün tek çare ters yönde ikinci hareket ve bu ekranda anlatılmıyor. Harita `EMANET_GIRIS`/`EMANET_CIKIS`'ı `STORNO_YOK` damgaladı (kullanıcıya "storno paket 48 ile gelecek" denir). Emanetin paralel defterde has bacağında çalışması nedeniyle storno kapsamına girip girmediği **karar bekliyor**.
- **Kurucuya:** `dotnet/Kuyumcu.Web/Pages/*.razor.mutorj` (5 dosya) ve `dotnet/Kuyumcu.Api/**/*.mutorj` (7 dosya) artıkları duruyor; hook `rm`'i blokladığı için silinemedi. Üçü (`Islemler`, `Sayim`, `Urunler`) güncel `.razor`'dan eski. **Elle silinmesi** öneriliyor.
- **Kurucuya:** MASAK kayıtlarına düzeltme yolu **açılmadı** (spesifikasyon "mevzuat gerekçesi araştırılmadan açılmaz" diyor). Harita hepsini `SALT_OKUNUR` damgaladı — bu ihtiyatlı (fail-closed) seçimdir.
