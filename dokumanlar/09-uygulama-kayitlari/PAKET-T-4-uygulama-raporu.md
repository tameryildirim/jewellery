# Paket T-4 — Faz A (T-4a) uygulama raporu

> Tarih: 2026-09-18 · Dalga 3 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Model: Opus
> Kapsam: **YALNIZ Faz A / T-4a**. Faz B (T-4b: destek paketi, `destek_paketleri` tablosu, `/destek` sayfası) bu dalgada YAZILMADI (dalga 6).
> Durum: **BİTTİ** — `dotnet build Kuyumcu.sln` 0 uyarı / 0 hata · **2452/2452 test yeşil** · `dotnet/denetim.sh` **TEMİZ** · canlı doğrulama 5198'de gerçek veriyle yapıldı · test kiracısı silindi (`tenants` 8 → 8).

---

## 1. Yapılanlar

### 1.1 Korelasyon ara katmanı (YENİ)
`dotnet/Kuyumcu.Api/Common/IstekKimligi.cs`
- Her isteğe **ULID** (26 karakter, mevcut `Common/Ulid.cs` üreticisi) verilir; `HttpContext.Items["istek_kimligi"]`, `ctx.TraceIdentifier`, `X-Istek-Kimligi` yanıt başlığı ve ILogger kapsamı **aynı** değeri taşır.
- İstemcinin gönderdiği `X-Istek-Kimligi` **okunmaz** (günlük zehirlenmesi); kimliği her zaman sunucu üretir ve giden başlığı ezer.
- Okuma tek yardımcıyla: `HttpContext.IstekKimligi()`. İkinci üretici/okuyucu açılmadı.
- `Program.cs`: `app.UseMiddleware<IstekKimligiAraci>()` **UseForwardedHeaders'tan sonra, `UseMiddleware<HataYakalayici>()`'dan önce** — tek kayıt noktası.

### 1.2 Hata gövdelerinde kimlik
`dotnet/Kuyumcu.Api/Common/ApiException.cs` — `HataYakalayici`'nın **dört dalı** da ortak `Yaz(ctx, kod, mesaj, hataAdi)` yardımcısından geçiyor:

| Dal | Alan kümesi (önce) | Alan kümesi (sonra) |
|---|---|---|
| ApiException | message · error · statusCode | + `istekKimligi` |
| BadHttpRequest (413/400) | message · error · statusCode | + `istekKimligi` |
| DbUpdateConcurrency (409) | message · error · statusCode | + `istekKimligi` |
| Beklenmeyen (500) | message · statusCode (**error YOK**) | + `istekKimligi` (**error hâlâ YOK**) |

`HataYakalayici`'dan GEÇMEYEN iki yazma noktasına da kimlik eklendi (destek kodu her hata için elde olsun):
- doğrulama 400'ü (`Program.cs` `InvalidModelStateResponseFactory`),
- 401 (`JwtBearerEvents.OnChallenge`).

### 1.3 Sürüm/derleme/migration künyesi (YENİ)
`dotnet/Kuyumcu.Api/Modules/Meta/SurumModulu.cs` — `SurumServisi` + `SurumController`:
- `GET /api/v1/meta/surum` — `[Authorize]`, her rol. Gövde: `surum · derlemeZamani · derlemeZamaniKaynagi · ortam · migrationSayisi · sonMigration · bekleyenMigrationSayisi`. 60 sn `IMemoryCache` (anahtar kiracıya göre AYRIŞMAZ — künyede kiracı verisi yok, şema tektir).
- `GET /api/v1/meta/surum/ayrinti` — `[RolGerekli("SAHIP")]`, uygulanmış + bekleyen migration listeleri, `uyari` (bekleyen varsa dolu), **yalnız bool** `kapilar` özeti, `DenetimServisi.OkumaKaydet(..., "Surum", ...)`.
- Migration bilgisi **yalnız** `GetAppliedMigrationsAsync` (__EFMigrationsHistory) + `GetPendingMigrationsAsync` (assembly) ile okunur; dosya sayımı YOK.
- Fail-closed: sürüm ya da derleme damgası okunamazsa "bilinmiyor" dönmez → 500.
- Sızdırmaz: sır, bağlantı dizesi, makine adı, IP, dosya yolu yok; `ortam` yalnız `Production` | `Development`.

### 1.4 Sürüm damgasının kaynağı
`dotnet/Kuyumcu.Api/Kuyumcu.Api.csproj`: `<Version>1.0.0</Version>` (sürümü **yalnız T-1 publish** artırır; kodda ikinci sürüm sabiti yok) + **Release derlemesinde** `AssemblyMetadata("DerlemeZamani")` damgası. `yayin-al.sh`'ye dokunulmadı (ikinci yayın yolu açılmadı).

### 1.5 İstemci
- `Kuyumcu.Web/Servisler/ApiIstemci.cs`: `ApiHata.IstekKimligi` (init-only; **kurucu imzası değişmedi**). `HataFirlat` kimliği önce **başlıktan**, yoksa gövdeden okur.
- `Kuyumcu.Web/Bilesenler/HataBandi.razor`: opsiyonel `Kimlik` parametresi → "Destek kodu" rozeti + salt-okunur **seçilebilir** alan + `#hata-kimlik-kopyala`. Kimlik yoksa rozet **hiç çizilmez**.
- `Kuyumcu.Web/Servisler/SurumServisi.cs` (YENİ): künye oturumda **bir kez** çekilir; hatalı çekim önbelleklenmez, istisna yukarı çıkar (UX-S28).
- `Kuyumcu.Web/Pages/Ayarlar.razor`: **Sistem künyesi** kartı (`#ayar-kunye`) — sürüm, derleme zamanı + kaynağı, ortam, migration sayısı/son adı, şema güncel (yeşil) / bekleyen (kırmızı) rozeti, `#ayar-kunye-kopyala`, salt-okunur `#ayar-kunye-satir`, SAHIP + pro modda `#ayar-kunye-ayrinti` düğmesi ve listesi, `HataBandi Kimlik=...`.
- `Kuyumcu.Core/I18n/Sozlukler.cs`: **TR + EN** `SozlukKunye` (21 anahtar). Ekranda çıplak metin yok.

### 1.6 Günlük korelasyonu (canlı ölçümle eklendi)
`appsettings.json` · `appsettings.Development.json` · `appsettings.Production.json` (+ `.ornek` ikizi): `Logging:Console:IncludeScopes = true`.
**Gerekçe (Madde 1, canlıda ölçüldü):** kapsam yazılmadan kullanıcı destek kodunu okuyor ama sunucu günlüğünde **aranamıyordu** (`grep <kimlik> api.log` → 0 satır). Ayrıca kapsam sözlük olarak verilince konsol biçimlendiricisi `System.Collections.Generic.Dictionary\`2[...]` basıyordu; **biçimli kapsam** (`BeginScope("IstekKimligi:{IstekKimligi}", kimlik)`) kullanıldı → hem grep hem yapısal alan çalışıyor.

---

## 2. Sapmalar ve gerekçeleri

| # | Spesifikasyon | Uygulanan | Gerekçe |
|---|---|---|---|
| S1 | `wwwroot/js/pano.js` (YENİ) | Mevcut `wwwroot/js/indir.js` içindeki `window.kuyumcuPanoyaKopyala` yeniden kullanıldı | İkinci kopyalama yolu açmamak (Bertan: mevcut mekanizmanın yeniden kullanımı) + `denetim.sh` "index.html `<script>` sayısı sabit 6" kapısı. CSP değişmedi: `script-src 'self' 'wasm-unsafe-eval'`, `unsafe-inline` EKLENMEDİ; kod hâlâ ayrı .js dosyasında. |
| S2 | Sürüm `Assembly.GetEntryAssembly()` | `typeof(SurumServisi).Assembly` | Test/WebApplicationFactory koşumunda giriş assembly'si **testhost**'tur; uç ürünün sürümü yerine test altyapısının sürümünü beyan ederdi (yanlış künye = yanlış destek kararı). |
| S3 | Derleme damgası her derlemede gömülü | Damga **yalnız Release**; Debug'da assembly dosyasının yazılma anı, `derlemeZamaniKaynagi` alanıyla **açıkça** ("damga" / "dosya") bildirilir | Her Debug derlemesinde damga değişince AssemblyInfo yeniden üretilir ve API + Tests her seferinde yeniden derlenirdi. Sessiz varsayım yok: kaynak yanıtta yazıyor, sabit değer yok. |
| S4 | Ortam adı | `IsProduction() ? "Production" : "Development"` | Mühür koşulu 10: ortam yalnız iki dizge; özel ortam adı ("Testing", "Staging-3") dağıtım topolojisini sızdırır. |
| S5 | (spesifikasyonda yok) | Doğrulama 400 ve 401 gövdelerine de `istekKimligi` | Bu iki gövde `HataYakalayici`'dan geçmez; kimlik verilmezse kullanıcı en sık gördüğü iki hatada destek kodu alamazdı. |
| S6 | (spesifikasyonda yok) | `Logging:Console:IncludeScopes` | Bkz. §1.6 — kimlik günlükte aranamıyordu; risk listesindeki "paketin asıl faydası kaybolur" maddesi canlıda gerçekleşmişti. |
| S7 | `denetim.sh` BEKLENEN_VARLIK 15→16 | **Dokunulmadı** | Faz A şema değişikliği içermez; sayaç dalga 6'da tek elden güncellenecek (birleşik koşul). |

**Mevzuat/varsayım:** Faz A'da mevzuata bağlı bir eşik/oran koda yazılmadı (KVKK/VUK maddeleri Faz B'nin destek paketi içeriğini bağlar). Kurucuya/mali müşavire not gerektiren bir hüküm doğmadı — bkz. §10.

---

## 3. Migration

**Gerekmedi.** Faz A şema değişikliği içermez (spesifikasyon: "Faz A (T-4a) için şema değişikliği YOKTUR"). `dotnet ef` çalıştırılmadı. Uygulanmış migration bilgisi çalışma anında `__EFMigrationsHistory`'den okunur; canlıda **42** (SQL ile birebir doğrulandı, §7).

---

## 4. Testler

| Kapsam | Önce | Sonra | Fark |
|---|---|---|---|
| Tam süit (`dotnet test`) | 2427 | **2452** | **+25 yeni test** |
| Başarısız | 0 | **0** | — |
| Atlanan | 0 | 0 | — |

Yeni dosyalar:
- `Kuyumcu.Tests/IstekKimligiTestleri.cs` — HTTP (7) + `HataKimligiDallariTestleri` izole dal testleri (4)
- `Kuyumcu.Tests/SurumUcuTestleri.cs` — HTTP + gerçek MySQL (6)
- `Kuyumcu.Tests/SurumEkranTestleri.cs` — bUnit (8)

Mevcut iki test dosyasına yalnız **DI kaydı** eklendi (`Services.AddScoped<SurumServisi>()`): `TemaEkranTestleri.cs`, `UX1C4EkranTestleri.cs` — Ayarlar sayfası yeni servisi enjekte ettiği için zorunlu.

**Kararsızlık kanıtı (gizlenmedi):** ilk tam süit koşumunda `UX1C1EkranTestleri.Pos_gecersiz_kalemde_EKLE_sessizce_gecmez_ALAN_ALTI_mesaj_verir` kırmızı oldu (`#alan-hata-islem-adet` bulunamadı). Bu test **T-4 kapsamındaki hiçbir dosyaya dokunmuyor** ve `Click()` sonrası `WaitForAssertion` kullanmıyor (yarış). Tek başına koşumda 35/35 yeşil; sonraki **üç** tam süit koşumunda 2452/2452 yeşil. Mevcut kararsız test olarak raporlanıyor; düzeltmesi T-4 kapsamı dışıdır (açık kalanlar §9).

Son koşum TRX sayacı:
```
<Counters total="2452" executed="2452" passed="2452" failed="0" error="0" .../>
```

---

## 5. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/T-4/` altına kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli değişiklik → filtreli test → yedek geri kopyalandı → `cmp` ile birebir aynılık kanıtlandı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M-T4-1 | `IstekKimligiAraci`'de `X-Istek-Kimligi` yazma satırı silindi | `Basarili_istek_X_Istek_Kimligi_basligi_dondurur` (+5 kimlik testi) — 6 kırmızı / 1 yeşil | `cmp` AYNI |
| M-T4-2 | 500 dalı ortak `Yaz()` yerine kimliksiz gövde yazar | **yalnız** `Beklenmeyen_hata_500_govdesinde_kimlik_var` (404/413/409 testleri YEŞİL kaldı — §A10 izolasyonu) | `cmp` AYNI |
| M-T4-3 | Ara katman istemcinin `X-Istek-Kimligi` başlığını kabul eder | `Istemcinin_gonderdigi_X_Istek_Kimligi_yok_sayilir_sunucu_kendi_uretir` | `cmp` AYNI |
| M-T4-4 | `SurumController.Ayrinti`'den `[RolGerekli("SAHIP")]` kaldırıldı | `Ayrinti_ucu_TEZGAHTAR_icin_403` | `cmp` AYNI |
| M-T4-5 | `MigrationSayisi` sabit `22` döndürüldü | `Surum_ucu_200_doner_ve_migration_sayisi_EFMigrationsHistory_ile_ayni` (spesifikasyondaki ad Aşkın koşulu gereği satır 90 ile eşitlendi) | `cmp` AYNI |
| M-T4-6 | `HataBandi`'da rozet koşulu `@if (true)` yapıldı | `HataBandi_kimlik_yokken_rozet_hic_cizilmez` | `cmp` AYNI |

Faz B'ye ait mutasyon kanıtları (maskeleme allow-list, jeton tek kullanımlık, HasQueryFilter, Destek.razor disabled, DuzeltmePolitikasi girdisi) **bu pakette kapsam dışıdır** — dalga 6'da uygulanacak.
Son mutasyondan sonra: `dotnet build Kuyumcu.sln --nologo` → **0 uyarı / 0 hata**.

---

## 6. Denetim betiği

```
✓ IgnoreQueryFilters (yasak)                     0
✓ Find/FindAsync (yasak)                         0
✓ ExecuteUpdate/Delete (beyaz liste 5)           5
✓ Hareketler.Add (yalnız HareketFabrikasi)       1
✓ IKiraciVarlik uygulayan varlık                50
✓ Finansal varlıkta [HttpDelete] (0 olmalı)      0
✓ index.html <script> sayısı (sabit 6)           6
✓ Pages: @onclick'siz ve disabled'sız <button>  71 (tavan 71)   ← ARTMADI
✓ Pages: ModalPencere kullanımı                 47 (tavan 47)   ← ARTMADI
✓ Pages: duzenlenenId kalıbı                     1 (tavan 1)    ← ARTMADI
DENETİM TEMİZ.
```
Not: ilk koşumda `@onclick'siz düğme` sayacı 72 (tavan 71) verdi — künye ayrıntı düğmesinin `<button` satırında `@onclick` yoktu (çok satırlı etiket). Düğme tek satıra alındı, sayaç 71'e döndü. **Gizlenmedi, düzeltildi.**

---

## 7. Canlı doğrulama (localhost:5198, gerçek veri)

Test kiracısı: **PAKETT-4-TEST Kuyumculuk** (`pakett4@test.local`), davetle açıldı. QA-TEST-* kiracılarına dokunulmadı.

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET /saglik` | 200 + `X-Istek-Kimligi` | **200** · `X-Istek-Kimligi: 01M2RM8F03JZYK13MCGJWRTXSC` |
| 2 | `GET /api/v1/meta/surum` (SAHIP) | 200 + migrationSayisi | **200** · `{"surum":"1.0.0","derlemeZamani":"2026-09-17T21:24:02.0276735Z","derlemeZamaniKaynagi":"dosya","ortam":"Development","migrationSayisi":42,"sonMigration":"20260917204558_YedekOtomasyonu","bekleyenMigrationSayisi":0}` |
| 3 | SQL `SELECT COUNT(*) FROM __EFMigrationsHistory;` | ucun değeriyle **birebir** | **42 = 42** ✓ |
| 4 | `GET /api/v1/cari/YOK-888` (SAHIP) | 404 · başlık = gövde kimliği | **404** · başlık `01M2RMC5839457QKQ0GPW2D4GW` · gövde `"istekKimligi":"01M2RMC5839457QKQ0GPW2D4GW"` ✓ |
| 5 | Sunucu günlüğünde aynı kimlik | grep eşleşmesi | `IstekKimligi:01M2RMC5839457QKQ0GPW2D4GW => Kuyumcu.Api.Modules.Cari.CariController.Getir` ✓ (IncludeScopes eklenmeden ÖNCE 0 eşleşme — §1.6) |
| 6 | `POST /api/v1/sistem/davetler` gövdesi eksik | 400 + kimlik | **400** · `{"message":["Gerekçe zorunludur"],"alanlar":{...},"error":"Bad Request","statusCode":400,"istekKimligi":"01M2RM97KKFKEFEMT0AYM00X76"}` |
| 7 | `GET /api/v1/meta/surum` (token yok) | 401 + kimlik | **401** · `{"message":"Token bulunamadı","error":"Unauthorized","statusCode":401,"istekKimligi":"01M2RMCT67E351X56SCVJ3MD69"}` + aynı değer başlıkta |
| 8 | `GET /api/v1/meta/surum/ayrinti` (TEZGAHTAR) | 403 | **403** · `{"message":"Bu işlem için gerekli rol: SAHIP","error":"Forbidden","statusCode":403,"istekKimligi":"01M2RMCK5SGP4VPV0993ZWB8F2"}` |
| 9 | `GET /api/v1/meta/surum` (TEZGAHTAR) | 200 (künye her role açık) | **200** |
| 10 | `GET /api/v1/meta/surum/ayrinti` (SAHIP) | 200 + listeler | **200** · `uygulanmis` 42 ad, `bekleyen` boş, `uyari: null` |
| 11 | Sır negatif grep: `password\|server=\|user=\|secret\|/Users/\|.dll\|localhost\|3306\|<makine adı>` | **boş** | **TEMİZ (eşleşme yok)** ✓ |
| 12 | SQL denetim kaydı | OKUMA · Surum | `OKUMA  Surum  sürüm ayrıntısı` ✓ |

**500 dalı:** canlıda kasıtlı 500 üretmek için ürüne sahte bir hata ucu eklenmedi (üretim yüzeyi açılmaz). 500 gövdesindeki kimlik, `HataYakalayici`'yı doğrudan çalıştıran izole testle ve M-T4-2 mutasyonuyla kanıtlandı; dört dal da **aynı** `Yaz()` yardımcısını kullanıyor.
**Tarayıcı turu:** Ayarlar künye kartı ve destek kodu rozeti bUnit ile kanıtlandı (8 test); 5199'daki kurucu sunucusuna dokunulmadı.

**Temizlik:** API PID öldürüldü → `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş (0 satır)**. Test kiracısı `tenant_id` taşıyan **49 tablodan** ve `tenants`'tan silindi; test daveti `kiraci_davetleri`'nden silindi.
`SELECT COUNT(*) FROM tenants` → **8** (başlangıçtaki değer). Kalanlar: ByGatech Kuyumculuk, Demo Kuyumcu, Demo Kuyumculuk, Düzeltme Sonrası Kuyumculuk, **QA-TEST-1/2/3 Kuyumculuk** (dokunulmadı), Ramazan Deneme.

---

## 8. DoD sözleşmesi

Faz A **yeni varlık/kayıt türü doğurmaz** — şema değişikliği yoktur, `Entities.cs`/`KuyumcuDbContext.cs` açılmamıştır.

| Varlık / kayıt | Durum | Düzenleme yolu |
|---|---|---|
| (yeni varlık yok) | — | — |
| Sürüm künyesi (`/meta/surum`) | **Türetilmiş, salt okunur** | Kullanıcı verisi değildir; değeri yayın hattı belirler (T-1 publish). Ekranda giriş alanı olarak sunulmaz (UX-S21), kopyalanabilir. |
| İstek kimliği | **Salt okunur, sunucu üretir** | Kullanıcı girdisi değildir; istemciden gelen değer bilinçli olarak yok sayılır. |
| `destek_paketleri` | **Faz B (dalga 6)** | Orada SALT_OKUNUR + `DuzeltmePolitikasi` girdisi + `DegismezIpucu` ile doğacak. |

DoD EK md.1 (ölü düğme yok): eklenen üç düğmenin **hepsi** etki üretir — `#ayar-kunye-kopyala` (pano + sonuç metni), `#ayar-kunye-ayrinti` (gerçek GET, `disabled="@ayrintiYukleniyor"`), `#hata-kimlik-kopyala` (pano; interop yoksa "kodu seçip elle kopyalayın" + seçilebilir salt-okunur alan). bUnit'te üçü de tıklanıp sonucu doğrulandı.
DoD EK md.2 (tek düzenleme kalıbı): yeni `ModalPencere`/`duzenlenenId` YOK (denetim sayaçları artmadı).
DoD EK md.4: yıkıcı eylem eklenmedi.

---

## 9. Koşul karşılama

| Koşul (birleşik / mühür) | Durum | Kanıt |
|---|---|---|
| Destek paketi diske yazılmaz | **Kapsam dışı (Faz B)** | Faz B yazılmadı |
| 500 dalına `error` EKLENMEZ | **KARŞILANDI** | `Yaz(..., hataAdi: null)`; test `Assert.False(govde.TryGetProperty("error", out _))` |
| 413/ApiException alan kümesi korunur | **KARŞILANDI** | `Govde_siniri_413_yanitinda_da_kimlik_var`, `ApiException_dalinda_alan_kumesi_korunur_ve_kimlik_eklenir`; mevcut `GovdeSiniriTestleri`/`HttpBoruHattiTestleri` yeşil |
| Migration sayısı yalnız __EFMigrationsHistory + assembly | **KARŞILANDI** | Kod + M-T4-5 + canlı 42=42 |
| `denetim.sh` BEKLENEN_VARLIK 16'ya çekilmesi dalga 6'da | **KARŞILANDI (dokunulmadı)** | denetim.sh değişmedi |
| CSP'ye `unsafe-inline` EKLENMEZ, kopyalama ayrı .js, sessiz ölmeyen düğme | **KARŞILANDI** | CSP satırı değişmedi; `indir.js`; `#hata-kimlik-elle` / `#ayar-kunye-elle` yedek yolu + testleri |
| İkinci limiter yolu yok | **KARŞILANDI** | Faz A'da oran sınırı değişikliği yok |
| IstekKimligiAraci UseForwardedHeaders sonrası, HataYakalayici öncesi | **KARŞILANDI** | `Program.cs` tek kayıt |
| Mutasyon test adı satır 90 ile eşitlendi | **KARŞILANDI** | M-T4-5 |
| `/meta/surum` `[Authorize]` kalır, sır/IP/makine/yol döndürmez, ortam iki dizge | **KARŞILANDI** | Canlı §7/7, §7/11 + `Surum_ucu_sir_veya_baglanti_dizesi_icermez` |
| Sürümü yalnız T-1 publish artırır | **KARŞILANDI** | csproj `<Version>`; `yayin-al.sh` değişmedi |
| Yaz-seri: `Sozlukler.cs`, `ApiIstemci.cs`, `HataBandi.razor` tek elden | **KARŞILANDI** | Üçü de bu pakette tek elden yazıldı |
| Kalıcı günlük sorusu Faz A sevkinden önce netleşsin | **KARŞILANDI** | T-1 `appsettings.Production.json` journald/docker json-file hattını tanımlıyor; kimliğin aranabilirliği `IncludeScopes` ile canlıda kanıtlandı |
| Anonim uç sorusu | **KAPANDI** | `/meta/surum` `[Authorize]`; `/saglik` + `/saglik/hazir` izleme için var |
| "Ayar özeti" yalnız bool kapı durumları | **KARŞILANDI** | `kapilar` yalnız 4 bool; test her alanın True/False olduğunu zorluyor |
| 60 sn önbellek–migration etkileşim notu | **KARŞILANDI** | `SurumUcuTestleri` başlık notu + kod yorumu (anahtar kiracıdan bağımsız) |
| Faz B kalemleri (429, 5 MB kırpma, zip sır testi, kiracı maskesi) | **Kapsam dışı (dalga 6)** | — |
| Sayısal iddia yasağı | **KARŞILANDI** | Tüm sayılar komut çıktısından (TRX, denetim.sh, SQL) |
| Tek dokunuş (dalga içi) | Dokunulan sayfa: **`Pages/Ayarlar.razor`** (bu dalgada T-4a tarafından) | Başka sayfa açılmadı |

---

## 10. Açık kalanlar / notlar

1. **Kararsız test (T-4 dışı):** `UX1C1EkranTestleri.Pos_gecersiz_kalemde_EKLE_sessizce_gecmez_ALAN_ALTI_mesaj_verir` — `Click()` sonrası `WaitForAssertion` yok, tam süitte nadiren kırmızı. Ayrı bir düzeltme kalemi olarak orkestratöre bırakıldı.
2. **Faz B (T-4b)** dalga 6'ya bırakıldı: `destek_paketleri` + migration, allow-list maskeleme, tek kullanımlık jeton, `/destek` sayfası, `DuzeltmePolitikasi` girdisi, `denetim.sh` varlık sayacı, oran sınırı kovası (saatte 3) ve ilgili testler/mutasyonlar.
3. **Sürüm numarası bugün `1.0.0`** (csproj). Gerçek sürüm artışı T-1 publish hattının kararıdır; T-4a sahte bir sürüm sabiti üretmez. Yayın betiği isterse `-p:InformationalVersion=` ile ezer.
4. **Geliştirme derlemesinde `derlemeZamaniKaynagi: "dosya"`** görünür (damga yalnız Release'te gömülür). Üretim yayınında `damga` beklenir; QA turunda bu alan gözle doğrulanmalı.
5. **TB-M1 (8 adet 0-baytlık migration + 336 baytlık `VadeTakibi.Designer.cs` + `.mutorj` artıkları)** duruyor; silme Kurucu kalemidir. T-4a sayımı dosyadan okumadığı için bu artıklar künyeyi **yanıltmıyor** (canlıda 42 = SQL 42).
6. **Kurucuya/mali müşavire not:** Faz A'da mevzuata bağlı hiçbir eşik/oran koda yazılmadı. KVKK md.4/12 yükümlülüğü asıl olarak Faz B'nin destek paketi içeriğini bağlar; o pakette allow-list maskeleme ve "kişisel veri yok" kuralı ile karşılanacak.
7. **Mehmet'e not:** `IncludeScopes` açıldı; günlüğe **yalnız** kimlik + çerçevenin kendi alanları düşüyor, istek gövdesi/sır/kişisel veri yazılmıyor (T-1 K14 notu geçerli).

---

## 11. Değişen dosyalar

**Yeni**
- `dotnet/Kuyumcu.Api/Common/IstekKimligi.cs`
- `dotnet/Kuyumcu.Api/Modules/Meta/SurumModulu.cs`
- `dotnet/Kuyumcu.Web/Servisler/SurumServisi.cs`
- `dotnet/Kuyumcu.Tests/IstekKimligiTestleri.cs`
- `dotnet/Kuyumcu.Tests/SurumUcuTestleri.cs`
- `dotnet/Kuyumcu.Tests/SurumEkranTestleri.cs`

**Değişen**
- `dotnet/Kuyumcu.Api/Common/ApiException.cs`
- `dotnet/Kuyumcu.Api/Program.cs`
- `dotnet/Kuyumcu.Api/Kuyumcu.Api.csproj`
- `dotnet/Kuyumcu.Api/Modules/ModulKayit.cs`
- `dotnet/Kuyumcu.Api/appsettings.json`
- `dotnet/Kuyumcu.Api/appsettings.Development.json`
- `dotnet/Kuyumcu.Api/appsettings.Production.json` (+ `.ornek` ikizi — `YayinDosyaSozlesmesiTestleri` birebir aynılık ister)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `dotnet/Kuyumcu.Web/Program.cs`
- `dotnet/Kuyumcu.Web/Servisler/ApiIstemci.cs`
- `dotnet/Kuyumcu.Web/Bilesenler/HataBandi.razor`
- `dotnet/Kuyumcu.Web/Pages/Ayarlar.razor`
- `dotnet/Kuyumcu.Tests/TemaEkranTestleri.cs` (yalnız DI kaydı)
- `dotnet/Kuyumcu.Tests/UX1C4EkranTestleri.cs` (yalnız DI kaydı)

**COMMIT YOK** — çalışma ağacında bırakıldı.
