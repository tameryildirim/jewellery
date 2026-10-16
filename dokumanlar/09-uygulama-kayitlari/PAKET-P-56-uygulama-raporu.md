# PAKET P-56 — /yedekler: BEKLİYOR talebinin gerekçesi düzenlenebilir

> Şerit: **SERİT B — YEDEK + KUR + DENETİM İZİ** (wf_f5213225-aa4) · izole test vt `kuyumcu_test_q` · canlı port **5187**
> Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Koşum: 2026-09-24 00:55–01:30
> Kaynak şartname: `dokumanlar/09-uygulama-kayitlari/TRIYAJ-TUR8-2026-09-23.md` (P-56, KRİTİK, 2,0 saat tahmin)

---

## 0. BU KOŞUMUN NİTELİĞİ — YENİDEN DOĞRULAMA (Anayasa md.1: gizlenmiyor)

Ağaçta P-56'nın kodu **zaten vardı**: 23.09 16:12'de önceki bir koşum (eski şerit kurgusu, `kuyumcu_test_b` / 5196)
uygulamış ve rapor yazmıştı; orkestratör kaydı (`ORKESTRATOR-DURUM.md:539`) *"P-56 uygulayıcı BITTI dedi, doğrulanmadı"*
diyor. O koşumun VT'si (`_b`) sonradan backend VT'siyle çakıştığı için şerit `_q`/5187 ile yeniden kuruldu.

Bu koşumda **kod yeniden yazılmadı** (şartname: "varsa yeniden yazma"). Yapılan: mevcut uygulamanın şartnameye karşı
okunması + yedi kapı maddesinin **bu ağaçta, bu VT'de, bu portta** yeniden ölçülmesi. `YedekModulu.cs` ve
`Yedekler.razor` 23.09 23:17'de başka bir pakette de değişmiş (P-13/B-48 iptal gerekçesi vb.); P-56 kısmı okunarak
bütün olduğu doğrulandı.

## 1. İLK İŞ — "DOĞRULANAMADI" kaleminin ÖLÇÜMÜ

- `test -e`: üç ayak izi dosyası **VAR** (`Yedekler.razor` 547 st · `YedekModulu.cs` 579 st · `YedekEkranTestleri.cs` 368 st).
- Yedek modülündeki uçlar (`Modules/Yedek/YedekModulu.cs` controller): `GET ozet` · `GET /` · `GET tatbikatlar` ·
  `GET talepler` · `POST talep` · `PATCH talep/{id}` (iptal) · **`PATCH talep/{id}/gerekce`** (P-56).
- Gerekçe güncelleme ucu **VAR** (`YedekModulu.cs:576-578` → `YedekServisi.TalepGerekceGuncelle`, `:436-499`).
  Mevcut `PATCH talep/{id}` her çağrıda talebi İPTAL ettiği için gerekçe oraya eklenmemiş, ayrı uç açılmış — doğru karar
  (aksi hâlde "gerekçemi düzelteyim" diyenin talebi iptal olurdu).

## 2. UYGULAMANIN ŞARTNAMEYE KARŞI OKUNMASI

### 2.1 Sunucu — `YedekModulu.cs`
- `YedekGerekceGuncelleDto` (`:169-200`): `…Gonderildi` üç durumlu PATCH deseni.
- `TalepGerekceGuncelle`: `FirstOrDefaultAsync` (Find YOK) → **durum kapısı** `Durum != BEKLIYOR` → 400 (`:444`) →
  alan gövdede yoksa **KORU** (`degisti:false`, iz yazılmaz) → servis içi elle **20 karakter** kontrolü (`:465-468`) →
  aynı değer ise değişiklik yok → `DenetimServisi.YazmaKaydet` **çağrılır** (eski + yeni, `hassas:true`, `:484-485`).
- Yetki: sınıf düzeyinde `[Authorize] [RolGerekli("SAHIP")] [Yetki.ModulYetkisi("yedek")]` (`:543-550`).

### 2.2 Ekran — `Yedekler.razor`
- Gerekçe hücresi (`:152-168`): `t.Durum == "BEKLIYOR"` ise **`SatirIciDuzenle`** (tek alanlı kalıp, DoD EK md.2),
  değilse salt okunur metin. İşlenmiş satırda eylem sütununda mevcut **`DegismezIpucu`** (`:194`).
- `GerekceKaydet` (`:484-513`): 20 karakter altında **istek atmaz**, `#yd-gerekce-hata-{id}` (role=alert) Türkçe mesaj;
  geçerliyse `PATCH …/gerekce`, yanıttaki `gerekce` başarı bandına yazılır (UX-S27), liste yeniden yüklenir; hata → hata bandı.
- Yeni ModalPencere yok, yeni `duzenlenenId` yok (denetim sayaçları aşağıda).

## 3. SAPMALAR VE GEREKÇELERİ

| # | Sapma | Gerekçe |
|---|---|---|
| S1 | Şartname "IPTAL/CALISIYOR/BITTI" diyor; kodda **CALISIYOR ve BITTI durumu YOK**. Gerçek küme `YedekTalepDurumu = BEKLIYOR / ISLENDI / IPTAL / HATA` (`Data/Entities.cs`). | Kapı "BEKLIYOR değilse kapalı" — hedef birebir korunuyor, hayalî durum adı yazılmadı. Test adı şartnamedeki gibi `CalisanTalepteDuzenleGorunmez`, fikstür `ISLENDI`. |
| S2 | Mevcut `PATCH talep/{id}` yerine **ayrı `…/gerekce`** ucu. | Mevcut uç iptal eder (§1). |
| S3 | PATCH'in **`null` = SİL** dalı yok → 400 ("gerekçe silinemez"). | Gerekçe zorunlu alan (`POST /talep` gerekçesiz açtırmıyor). Canlıda ölçüldü (§8 adım 5). |
| S4 | `Sozlukler.cs`'e **dokunulmadı** (şerit sınırı). Kısa gerekçe mesajı `Dil.T.Siparis.GerekceKisa` anahtarından geliyor. | Çıplak metin yok; ama anahtar başka modülün adı altında (UX-S31 "tek kaynak" borcu → P-58). |
| S5 | 20 karakter kuralı `SatirIciDuzenle` bileşenine parametre olarak değil, ebeveynde. | Ortak bileşen şerit sınırı dışında (P-61 ayak izi). Kabul koşulu testle kanıtlı. |

**Mevzuat hükmü:** yok. Paket eşik/vergi kuralı getirmiyor; 20 karakter kuralı sunucuda zaten vardı (`GerekceEnAz`),
istemci aynı sabiti ekran doğrulaması için kullanıyor (sunucu kuralı esastır). Web araştırması gerekmedi.

## 4. DoD SÖZLEŞMESİ

| Varlık | Düzenlenebilir mi | Uç / gerekçe |
|---|---|---|
| `YedekTalebi` (BEKLIYOR) — `Gerekce` | **EVET** | `PATCH /api/v1/yedekler/talep/{id}/gerekce` · ekranda satır içi Düzenle · denetim izi eski+yeni |
| `YedekTalebi` (BEKLIYOR) — pasife alma | **EVET (mevcut)** | `PATCH /api/v1/yedekler/talep/{id}` → IPTAL (ortak onay modalı). Hard DELETE yok |
| `YedekTalebi` (ISLENDI / IPTAL / HATA) | **HAYIR** | İşlenmiş talebin gerekçesi denetim izinin parçası; sunucu 400, ekranda `DegismezIpucu` |
| `YedekKaydi`, `YedekTatbikati` | HAYIR (kapsam dışı) | Makine üretimi koşu/tatbikat kayıtları |

Yetki: `[RolGerekli("SAHIP")]` + `[ModulYetkisi("yedek")]` (test `GerekceGuncelle_SahipOlmayan_403`). Denetim:
`YazmaKaydet` çağrılıyor, yeniden yazılmadı. Her düğme gerçek istek atar (SatirIciDuzenle Kaydet/İptal yalnız düzenleme
açıkken çizilir; kısa gerekçede Kaydet → alan altı mesaj, sessiz etkisizlik yok).

## 5. DERLEME

```
dotnet build Kuyumcu.sln --nologo   (ilk ve son mutasyon sonrası)
    0 Uyarı
    0 Hata
```

## 6. MIGRATION

**Gerekmedi** — şema değişikliği yok (`yedek_talepleri.gerekce` mevcut sütun).

## 7. TESTLER (`KUYUMCU_TEST_DB=kuyumcu_test_q`)

| Kapsam | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| `FullyQualifiedName~Yedek` (başta) | 60 | 60 | 0 |
| P-56'nın 8 testi (adıyla) | 8 | 8 | 0 |
| `FullyQualifiedName~Yedek` (mutasyonlar geri alındıktan sonra) | 60 | 60 | 0 |
| **Tam süit** (`--logger trx --results-directory …/scratchpad/trx-pP-56`) | **4658** | **4655** | **3** (hiçbiri bu paketin değil — §7.2) |

### 7.1 Paketin testleri (8; bu koşumda yeni test yazılmadı — önceki koşumda eklenmişti)
bUnit `YedekEkranTestleri`: `BekleyenTalebinGerekcesiDuzenlenir` · `CalisanTalepteDuzenleGorunmez` · `KisaGerekceIstekGondermez`
HTTP `YedekHttpTestleri`: `GerekceDegisimiDenetimIzineYazilir` · `GerekceGuncelle_KisaGerekce_400` · `GerekceGuncelle_IslenmisTalep_400` ·
`GerekceGuncelle_AlanGonderilmedi_Korunur` · `GerekceGuncelle_SahipOlmayan_403`

### 7.2 Tam süitin 3 kırmızısı — sahipleriyle
| Test | Hata | Sahibi (ölçüm) |
|---|---|---|
| `S23FisMusteriEkranTestleri.S23E_12_tur_degisince_bant_iner_satisa_donunce_GERI_GELMEZ` | `WaitForFailedException` | **S-2 şeridi (S2-3)** — `Bilesenler/Fis/FisMusteriSatiri.razor` 01:00, `FisBakiyeRozeti.cs` 00:52'de değişiyordu |
| `S35EmanetEkranTestleri.S35_15_Ayrintidan_GERI_AL_ortak_storno_onayina_gider_ve_48A_ucuna_POST` | `Assert.Empty() Failure` | **S-3B şeridi (S3-5)** — `Pages/Emanet.razor` bu koşum sırasında değişti |
| `UX4cEkranTestleri.M10_Ayar_listesi_UC_EKRANDA_TEK_KAYNAKTAN_beslenir` | `Assert.Contains() Failure` (Emanet ayar seçenekleri) | **S-3B şeridi** — aynı `Pages/Emanet.razor` |

Bu paketin dosyaları (`Yedekler.razor`, `YedekModulu.cs`) koşum boyunca yedekle **cmp birebir aynı**; üç test de
Yedek dosyalarına referans vermiyor. Devir notunda liste verilmedi; fark ölçüsü sahiplikle yapıldı → **bu paketin ürettiği
yeni kırmızı = 0**.

## 8. MUTASYON KANITLARI

Yöntem: kaynak `scratchpad/mut-yedek/P-56/` altına kopyalandı → `perl -0777 -pi -e` tek eşleşme (eşleşme sayısı önceden
ölçüldü: 1/1/1/1) → filtreli test → yedek geri kopyalandı → `cmp`.

| Kod | Değişiklik | Kırmızıya dönen test | Geri alındı |
|---|---|---|---|
| **M56-1** | `Yedekler.razor:153` gerekçe hücresi `@if (t.Durum == "BEKLIYOR")` → `@if (true)` (ekran durum kapısı) | `YedekEkranTestleri.CalisanTalepteDuzenleGorunmez` (1/9 başarısız) | cmp AYNI |
| **M56-2** | `Yedekler.razor:487` `if (yeni.Length < GerekceEnAz)` → `if (false)` (istemci 20 karakter kuralı) | `YedekEkranTestleri.KisaGerekceIstekGondermez` (1/9) | cmp AYNI |
| **M56-3** | `YedekModulu.cs:484-485` `denetim.YazmaKaydet(... gerekçesi güncellendi ...)` silindi | `YedekHttpTestleri.GerekceDegisimiDenetimIzineYazilir` (1/12) | cmp AYNI |
| **M56-4** (ek) | `YedekModulu.cs:444` sunucu durum kapısı `kayit.Durum != Bekliyor` → `false` | `YedekHttpTestleri.GerekceGuncelle_IslenmisTalep_400` (1/12) | cmp AYNI |

⚠ **Ölçülen ortam arızaları (gizlenmiyor):**
1. **M56-1 ilk denemede YEŞİL kaldı.** Test mantığı doğruydu (tal2 için `#si-yedek-talep-gerekce-tal2-ac` oluşmalıydı);
   neden bayat ikili: o anda ağaçta **63 dotnet/MSBuild süreci** koşuyordu ve artımlı Razor derlemesi mutasyonu dll'e
   yansıtmadı (dll damgası 01:04:37, belirsiz). `dotnet build Kuyumcu.Web --no-incremental` ile tekrarlandı → KIRMIZI.
   Sonraki Razor mutasyonu (M56-2) baştan `--no-incremental` ile koşuldu.
2. **M56-3 ilk denemede ölçüm geçersiz:** `Kuyumcu.Tests.dll dosyası bulunamadı` — başka şeridin eşzamanlı derlemesi
   dosyayı o anda değiştiriyordu. Tekrarlandı.
3. M56-3 tekrarında `dotnet build Kuyumcu.Api --no-incremental` **rc=1** verdi:
   `Microsoft.NET.Sdk.WebAssembly.Browser.targets(272,5): error MSB4018 … ConvertDllsToWebCil` — Web projesinin
   WebCil dosya kilidi (paralel şerit). Ardından `dotnet test` Api'yi artımlı derleyip mutasyonu yakaladı (hedef test
   kırmızı). Son mutasyondan sonra kaynaklar `touch`landı, `dotnet build Kuyumcu.sln` → **0 uyarı 0 hata**,
   `~Yedek` 60/60 yeşil.

## 9. DENETİM (`dotnet/denetim.sh`)

```
rc=1
  ✗ P-15: DUZ_SELECT_LIMIT      ölçülen 13 · tablo 12 · açık 9 (tavan 9)
  ✗ S2-3: FIS_BILESENI_ISTEK    2 (fiş bileşeni istek atmaz)
DENETİM BAŞARISIZ — yukarıdaki ihlaller giderilmeli.
```
**Bu paketin sayaçları ✓:**
```
  ✓ Pages: ModalPencere kullanımı   52 (tavan 55)
  ✓ Pages: duzenlenenId kalıbı       1 (tavan 1)
  ✓ P-36: HAM_SELECT                29 (tavan 30)
  ✓ P-36: HAM_INPUT                 42 (tavan 42)
```
İki ihlal bu paketin DEĞİL (ölçüldü):
- `DUZ_SELECT_LIMIT`: `grep -rcE 'limit=200' Pages/*.razor` → `Yedekler.razor` **0**; noktalar Banka, Islemler(3),
  Vezne, Urunler, Faturalar(2), Kurulum, BankaEkstre, Evraklar(2), Siparisler. Sınıflandırılmamış 13. nokta
  Islemler/Faturalar/Siparisler'den birinde — **S-2 ya da SERİT A** (bu koşum sırasında `Islemler.razor` ve
  `Faturalar.razor` değişti).
- `FIS_BILESENI_ISTEK`: `Bilesenler/Fis/FisMusteriSatiri.razor` + `FisBakiyeRozeti.cs` — **S-2 şeridi (S2-3)**.

## 10. CANLI DOĞRULAMA (port 5187, `PAKETP-56-TEST Kuyumculuk`, tenant `01M385P4RY1E05BYF0SWGGQXA7`)

Kiracı: `kiraci_davetleri`'ne tek kullanımlık davet satırı (SHA-256 jeton özeti) eklendi → `POST /auth/kayit` → 200.

| # | Adım | Beklenen | Gerçek (yanıt okundu) |
|---|---|---|---|
| 1 | `POST /yedekler/talep` (gerekçe 38 krk) | 201 | **201** `{"id":"01M385PG6H02WSVS5EA69E6HB2","durum":"BEKLIYOR",…}` |
| 2 | `PATCH …/gerekce` `{"gerekce":"kisa"}` | 400 alan zarfı | **400** `{"alanlar":{"gerekce":["Gerekçe en az 20 karakter olmalıdır"]}}` |
| 3 | `PATCH …/gerekce` geçerli | 200, degisti:true, BEKLIYOR | **200** `{"durum":"BEKLIYOR","gerekce":"mali musavir talebiyle ek yedek aliniyor","degisti":true}` |
| 4 | `PATCH …/gerekce` `{}` | 200, korunur | **200** `{"degisti":false,"mesaj":"Gövdede gerekçe alanı yok; kayıt korundu."}` |
| 5 | `PATCH …/gerekce` `{"gerekce":null}` | 400 (silinemez) | **400** "Gerekçe en az 20 karakter olmalıdır; gerekçe silinemez." |
| 6 | `GET /yedekler/talepler` | yeni gerekçe listede | **200** `"gerekce":"mali musavir talebiyle ek yedek aliniyor","durum":"BEKLIYOR"` |
| 7 | `PATCH talep/{id}` iptal → `…/gerekce` | 200 → 400 | **200** `durum:IPTAL` → **400** "(mevcut durum: IPTAL)" |
| 8 | 2. talep `POST` → SQL ile `ISLENDI` → `…/gerekce` | 400 | **201** → **400** "(mevcut durum: ISLENDI)" |

Adım 8'de talep durumu, harici yedek betiğinin test talebini işleyip diske dosya üretmemesi için doğrudan SQL ile
(`WHERE id=… AND tenant_id=<test> AND durum='BEKLIYOR'`, ROW_COUNT=1) ISLENDI'ye çekildi — kuyruğu API içinde tüketen
C# kodu yok.

**SQL kanıtı** (`kuyumcu_net`):
```
SELECT gerekce,durum,iptal_gerekce FROM yedek_talepleri WHERE id='01M385PG6H02WSVS5EA69E6HB2';
mali musavir talebiyle ek yedek aliniyor | IPTAL | canli dogrulama sonunda talep iptal ediliyor

SELECT eylem,hassas,detay FROM denetim_kayitlari WHERE varlik_id='01M385PG6H02WSVS5EA69E6HB2' ORDER BY tarih,id;
EKLEME      1  elle yedek talebi açıldı · gerekçe: aylik denetim oncesi elle yedek aliniyor
EKLEME      0  NULL
EKLEME      1  yedek talebi gerekçesi güncellendi · eski: aylik denetim oncesi elle yedek aliniyor · yeni: mali musavir talebiyle ek yedek aliniyor
GUNCELLEME  0  Gerekce
EKLEME      1  yedek talebi iptal edildi · gerekçe: canli dogrulama sonunda talep iptal ediliyor
GUNCELLEME  0  Durum, IptalGerekce, Islendi
```

**Temizlik:** API (dotnet run PID 50039, dinleyen 50044) `kill` → `lsof -nP -iTCP:5187 -sTCP:LISTEN` **boş**.
`tenant_id` sütunu olan **70 tablodan** test kiracısının satırları silindi; davet satırı ve `tenants` satırı silindi.
Son ölçüm: `COUNT(*) tenants` = **10** (başlangıç 10) · `PAKETP-56%` = 0 · davet = 0 · yedek_talepleri/denetim_kayitlari/
kullanicilar (test tenant) = 0/0/0. `QA-TEST-*` kiracılarına dokunulmadı. Jeton dosyası `printf ''` ile boşaltıldı.

## 11. DEĞİŞEN DOSYALAR

**Bu koşumda kaynak dosya değişmedi** (tüm mutasyonlar geri alındı, `cmp` AYNI). Yalnız bu rapor yeniden yazıldı.
P-56'nın ağaçtaki ayak izi (önceki koşum):
```
dotnet/Kuyumcu.Api/Modules/Yedek/YedekModulu.cs        (git: ?? — dizin izlenmiyor)
dotnet/Kuyumcu.Web/Pages/Yedekler.razor                (git: ??)
dotnet/Kuyumcu.Tests/YedekEkranTestleri.cs
dotnet/Kuyumcu.Tests/YedekHttpTestleri.cs
dokumanlar/09-uygulama-kayitlari/PAKET-P-56-uygulama-raporu.md
```
`git status --porcelain | grep -v -E "/(bin|obj)/"` bu depoda tüm ağacı değişik gösteriyor (tek `first commit`), paket
ayrımı için kullanışsız. **Dokunulan sayfalar (TEK DOKUNUŞ):** `Pages/Yedekler.razor` (P-56 → P-76 → P-77 → P-58 →
P-59 → P-61 → P-62 zincirinin ilki). Proje ağacında bu paketten `.mutorj/.bak/.tmp` artığı yok (ağaçtaki
`AramaHttpTestleri.cs.bak`, `MadenDefteriTestleri.cs.bak`, `P21CiroRucuTestleri.cs.bak` bu paketin değil).

## 12. KOŞUL KARŞILAMA

| Koşul | Durum | Kanıt |
|---|---|---|
| BEKLIYOR talepte Düzenle görünür, gerekçe kaydedilir (200) | ✓ | bUnit `BekleyenTalebinGerekcesiDuzenlenir` · canlı adım 3 (200) |
| İşlenmiş/iptal talepte Düzenle yok, kilit gerekçesi var | ✓ | bUnit `CalisanTalepteDuzenleGorunmez` · M56-1 · canlı 7/8 (400) |
| <20 karakter alan düzeyinde reddedilir, istek gitmez | ✓ | bUnit `KisaGerekceIstekGondermez` · M56-2 · sunucu da 400 (canlı 2) |
| Gerekçe değişimi denetim izinde eski+yeni | ✓ | HTTP `GerekceDegisimiDenetimIzineYazilir` · M56-3 · SQL §10 |
| Kapı (1) build 0 hata | ✓ | 0 uyarı 0 hata |
| Kapı (2) paket testleri 0 başarısız | ✓ | 8/8 · `~Yedek` 60/60 |
| Kapı (3) paket sayaçları ✓ | ✓ | ModalPencere 52/55 · duzenlenenId 1/1 · HAM_* ✓ |
| Kapı (4) mutasyon | ✓ | 4/4 kırmızı, cmp AYNI |
| Kapı (5) canlı | ✓ | §10, 8 adım |
| Kapı (6) test kiracısı silindi | ✓ | tenants 10 → 10 |
| Kapı (7) yeni kırmızı üretilmedi | ✓ | 3 kırmızı → S-2 / S-3B (§7.2) |

## 13. AÇIK KALANLAR / ORKESTRATÖRE NOT

1. **Başkasına ait tam süit kırmızıları:** `S23E_12` (S-2/S2-3) · `S35_15` ve `UX4c.M10` (S-3B, `Pages/Emanet.razor`).
2. **Başkasına ait denetim ihlalleri:** `P-15 DUZ_SELECT_LIMIT 13>12` (S-2 ya da SERİT A — Islemler/Faturalar/Siparisler) ·
   `S2-3 FIS_BILESENI_ISTEK 2` (S-2).
3. **Sözlük borcu (S4):** `Siparis.GerekceKisa` Yedek ekranında kullanılıyor → P-58'de `SozlukYedek.GerekceKisa` açılmalı.
4. **Ham durum kodu ekranda:** `Yedekler.razor:170` `<Rozet>@t.Durum</Rozet>` BEKLIYOR/ISLENDI'yi ham basıyor (UX-S31) — P-58 kapsamı.
5. **Denetim izinde eylem etiketi:** `DenetimServisi.YazmaKaydet` (`Data/DenetimIzi.cs:139-144`) eylemi sabit
   `DenetimEylemi.Ekleme` yazıyor; güncelleme satırı "EKLEME" etiketi taşıyor (otomatik dinleyici ayrıca doğru
   `GUNCELLEME · Gerekce` satırı düşüyor). Sistem geneli davranış; DoD "çağrılır, yeniden yazılmaz" dediği için
   dokunulmadı — eylem parametreli aşırı yükleme ayrı paket olarak değerlendirilmeli.
6. **Paralel şerit derleme tuzağı (bu koşumda yeniden ölçüldü):** artımlı Razor derlemesi mutasyonu dll'e yansıtmadı
   (M56-1), eşzamanlı derleme `Kuyumcu.Tests.dll`'i geçici olarak yok etti (M56-3), WebCil kilidi `--no-incremental`
   derlemeyi kırdı. Öneri: şerit başına `--artifacts-path`; o gelene dek Razor mutasyonları `--no-incremental` ile koşulmalı.
7. Kurucu / mali müşavir notu: **yok** (mevzuat hükmü yok).
