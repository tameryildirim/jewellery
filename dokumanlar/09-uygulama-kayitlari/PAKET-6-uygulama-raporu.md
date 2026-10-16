# Paket 6 — Bildirim kanalı + zamanlanmış iş çatısı (Faz A) — uygulama raporu

> Dalga 2 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) rolü · Tarih: 2026-09-16
> Kapsam: **YALNIZ FAZ A**. TEVSIK_KURTARMA (Faz B), /bildirimler/is-calismalari ayrı
> ekranı ve WhatsApp derin bağlantısı YAZILMADI.

## 1. Durum özeti

| Ölçüt | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln --nologo` | **0 hata · 0 uyarı** |
| Tam süit | **1590 test · 1590 geçti · 0 başarısız · 0 atlanan** |
| Yeni test | **49** (öncesi 1541 → sonrası 1590) |
| `dotnet/denetim.sh` | **DENETİM TEMİZ** |
| Migration | `20260916181735_BildirimVeIsCatisi` — kuyumcu_net'e uygulandı (`__EFMigrationsHistory` = **32** satır) |
| Kiracı sayısı | başlangıç **7** → son **7** (test kiracısı silindi) |
| Mutasyon kanıtı | 10/10 — her biri adı verilen testi kırmızıya çevirdi |

## 2. Yapılanlar

### Veri modeli
- **YENİ** `Kuyumcu.Api/Data/ZamanlanmisIs.cs` — `IsCalismasi` ve `Bildirim` varlıkları +
  `IsDurum` / `BildirimOnem` sabitleri. `Entities.cs`'e **dokunulmadı** (GunKapanisi deseni;
  paket 1/38 ile yaz-seri çakışması kesildi).
- `KuyumcuDbContext`: 2 DbSet + 5 indeks. İki UNIQUE indeks kuralı **veritabanına** taşır:
  `(tenant_id, is_kodu, calisma_anahtari)` ve `(tenant_id, tekillik_anahtari)`.
  "Önce SELECT et" yok → yarış koşulu kapalı.
- Migration Up() okundu ve doğrulandı: iki tablo, 5 indeks, ikisi `unique: true`.
  Canlıda `SHOW INDEX FROM is_calismalari` ile UNIQUE varlığı görüldü.

### Çatı
- `Modules/Isler/IZamanlanmisIs.cs` — arayüz + `BildirimTaslagi` + `IsCikti` + `IsBaglami`
  + `IsAnahtarlari` (Gunluk / Elle / Tekillik — saf, testlenebilir).
- `Modules/Isler/IsCalistirici.cs` — hak talebi (claim) → iş → tamamla. Bildirim ekleme
  tek tek, tekillik çakışması sayılır.
- `Modules/Isler/IsZamanlayici.cs` — BackgroundService. `Isler:Etkin=false` ise hiç koşmaz.
  `Isler:TurDakika` 1–1440 arasına sıkıştırılır. **İş mantığı yok** (K0-5).
- `Modules/Isler/IBildirimKanali.cs` — arayüz + tek uygulama `UygulamaIciKanal` (ADR-0016).
- Üç Faz A işi: `VadeHatirlatmaIsi`, `MasakSibSuresiIsi`, `SaklamaOnizlemeIsi`.
- `Modules/Isler/IslerModulu.cs` — `BildirimServisi` + `BildirimController` + `IslerController`.

### Uçlar
| Fiil | Yol | Kapılar | Canlı doğrulanan kod |
|---|---|---|---|
| GET | `/api/v1/bildirimler?durum=&limit=` | `[ModulYetkisi("bildirim")]` + `[RolGerekli(4 rol)]` + gorunur_roller süzmesi | 200 / 401 / 403 |
| GET | `/api/v1/bildirimler/sayi` | `[ModulYetkisi("bildirim")]` | 200 |
| POST | `/api/v1/bildirimler/{id}/kapat` | `[ModulYetkisi]` + `[RolGerekli(4 rol)]` + `DenetimServisi.YazmaKaydet` | 200 / 400 / 404 |
| GET | `/api/v1/isler` | `[ModulYetkisi]` + `[RolGerekli("SAHIP","MUDUR")]` | 200 |
| POST | `/api/v1/isler/{kod}/calistir` | aynı + `OranSiniri.IsCalistir` (3/dk) + denetim izi | 200 / 403 / 404 / 429 |

### Ekran
- **YENİ** `Kuyumcu.Web/Pages/Bildirimler.razor` (`/bildirimler`) — Açık/Kapalı/Tümü sekmesi,
  önem rozeti, hedef rota bağlantısı, Kapat (onay modalı + not), HataBandi/BasariBandi,
  BosDurum, sınır uyarısı, kapalı pencere notu, ProAlan içinde iş paneli + çalışma geçmişi.
- `Layout/UstBar.razor` — 🔔 rozeti (60 sn yoklama). 0 ise DOM'da yok; 403/401'de de yok;
  başka hatada "?" rozeti + başlıkta sebep. `#ust-mod-anahtar` ve tema seçici bozulmadı (bUnit).
- `Layout/KenarCubugu.razor` — `Yetki.Erisebilir("bildirim")` dalında 🔔 Bildirimler.
- `Layout/RotaBasliklari.cs` — `["/bildirimler"]`.
- `Kuyumcu.Core/I18n/Sozlukler.cs` — `Nav.Bildirimler` + yeni `SozlukBildirim` bloğu, **TR ve EN birlikte**.
- `wwwroot/css/app.css` — `.bildirim-rozet*`, `.bildirim-kart*`, `.bant-uyari`, `.dugme-benzeri`.
  **Sabit renk yok**; tümü mevcut belirteçlerden (`var(--…)`).

### Modül-başı yetki tohumlama (ADR-0027 mekanizması — sahiplik notu)
Mimari belgesi mekanizmayı **paket 29**'a yazmıştı (ADR-0027). Paket 29 henüz yazılmadı ve
paket 6 yeni bir modül anahtarı (`bildirim`) getiriyor; eski tek marker
(`__yapilandirildi__`) yüzünden mevcut kiracılarda ekran/rozet **sessizce 403** olurdu.
Birleşik koşul 2'nin "ya burada kurulur" seçeneği uygulandı:

- `YetkiServisi.ModulMarker(modul)` → `__tohum:{modul}__`; her modül **kendi markeriyle**
  bir kez tohumlanır, mekanizma **idempotent**tir.
- `Moduller.TohumlamaOncesi` — mekanizmadan önce var olan 8 anahtar. **Eski kiracıda bu
  anahtarlar yeniden tohumlanmaz**: sahibin bilerek kaldırdığı yetki geri gelmez; yalnız
  markerleri geriye doldurulur.
- `Kaydet` artık modül markerlerini de yazar (aksi hâlde kayıt sonrası tohumlama yeniden
  koşup kaldırılan yetkiyi geri getirirdi).
- **Paket 3/29/31/32/45/46 bu mekanizmayı yalnız KULLANIR**: kendi anahtarını
  `Moduller.Tumu` + `Moduller.Varsayilan`'a ekler; `TohumlamaOncesi` **büyümez**.
- Regresyon testi: `BildirimHttpTestleri.Eski_marker_li_kiracida_bildirim_anahtari_TOHUMLANIR`.

## 3. Sapmalar ve gerekçeleri

| # | Spesifikasyon | Uygulanan | Gerekçe |
|---|---|---|---|
| S1 | `IZamanlanmisIs.Calistir(KuyumcuDbContext db, string tenantId, DateTime anUtc, CancellationToken)` | `Calistir(IsBaglami baglam, CancellationToken)` — bağlam `IServiceProvider` (kiracıya sabit kapsam), db, tenantId, anUtc taşır | SAKLAMA_ONIZLEME işi `SaklamaSuresiIsi`'ni (Scoped, Program.cs:47) **çağırmak zorunda**; db'den o servis türetilemez. Alternatif işin içinde `new DenetimServisi(...)` kurmaktı — ADR-0021 ("tek yazma denetim izi yolu") ihlali olurdu. |
| S2 | Dönüş `IReadOnlyList<BildirimTaslagi>` | `IsCikti(Taslaklar, IncelenenKayit)` | `is_calismalari.incelenen_kayit` dürüst doldurulsun diye; aksi hâlde sayaç ya 0 ya uydurma olurdu (md.1). |
| S3 | `Varsayilan` matrisinde **TEZGAHTAR dahil** üç role `bildirim` | TEZGAHTAR'a **varsayılan kapalı** (SAHIP/MUDUR/MUHASEBE açık) | Orkestratörün ek koşulu ("SAHIP/MUDUR/MUHASEBE'ye açılır, TEZGAHTAR'a değil") spesifikasyonun üzerinde ve daha yenidir. Uç `[RolGerekli]` tezgâhtarı **kabul eder**; sahibi yetki ekranından açabilir. Spesifikasyonun "TEZGAHTAR 200 alır ama MASAK bildirimini görmez" testi, testin içinde SAHİP eliyle yetki verilerek korundu — görünürlük süzmesi yine ölçülüyor. |
| S4 | Vade bildirimi gövdesinde müşteri | Gövdede **ad/telefon YOK**; kimlik yalnız `ilgili_varlik_id`, yönlendirme `/raporlar/vade-takibi` | Mehmet'in bağlayıcı koşulu: "bildirim başlık-gövdesinde TCKN/belge no/**ad** yok". UX bedeli açık kalemlere yazıldı. |
| S5 | `Isler` yapılandırması | İş başına `Etkin` anahtarı da eklendi (`Isler:Vade:Etkin` vb.) | Bir işin kapatılması tüm turu kapatmasın; `TevsikKurtarma:Etkin=false` ile aynı desen. |
| S6 | Elle koşum yanıtı | `atlandi=true` iken mesaj **"zaten koştu, yeni koşum yapılmadı"** | **Canlı doğrulamada yakalandı**: aynı dakika içindeki ikinci tetikleme koşmuyor ama yanıt "İş çalıştı: 0 yeni bildirim" diyordu — koşmamış işi koşmuş gibi raporlamak md.1 ihlalidir. Düzeltildi + test yazıldı (`Ayni_dakikada_ikinci_elle_tetikleme_ATLANDI_olarak_raporlanir`). |
| S7 | `/bildirimler` basit menüye | **Eklenmedi** | `BasitModMenuTestleri` basit menü uzunluğunu 7 olarak sabitliyor; liste paket UX-3'ün sahipliğinde. Rozet **her modda** görünür ve tıklanınca ekrana gider (mod bir kapı değildir), yani kullanıcı erişimsiz kalmıyor. |
| S8 | Kiracı sayısı "5'e döner" | **7'ye döner** | Spesifikasyon 5 diyordu; kodda/veride bugün 7 kiracı var (5 + `QA-TEST-1`, `QA-TEST-2` kalıcı QA fikstürü). Fikstürlere dokunulmadı. |
| S9 | denetim.sh `BEKLENEN_VARLIK 15 → 17` | 17 yapıldı; ölçülen gerçek değer **37** (karşılaştırma `-ge`) | Spesifikasyonun dediği yapıldı. `'>=' mi '=' mi` kararı bu pakette **verilmedi** (spesifikasyonun kendi notu). |

### Mevzuat işaretleri
- **TEVSIK_KURTARMA (VUK mük. md.355/4) — DOGRULANMALI**: iş **yazılmadı**. Yapılandırma
  anahtarı `Isler:TevsikKurtarma:Etkin` **varsayılan false** olarak `appsettings.json`'a kondu ve
  `IsSenaryoTestleri` bunu zorluyor. Koda hiçbir süre/hüküm **sabit yazılmadı**.
- **MASAK 10 iş günü**: bu paket rakamı **yeniden hesaplamıyor**, `masak_supheli_bildirimler.son_bildirim_tarihi`
  alanını yalnız **okuyor**. Yeni mevzuat rakamı girmedi.
- **ADR-0008**: yalnız `Onizleme()` çağrılıyor; maskeleme (`Uygula`) iş ağacında **0 kez** geçiyor
  (denetim.sh sayacı + kaynak tarama testi).

## 4. DoD sözleşmesi (Bertan koşulu · MÜHÜR 2)

| Varlık | Düzenleme yolu | Uç | Kapı | Denetim izi | Ekranda |
|---|---|---|---|---|---|
| `Bildirim` | **Kapatma (damga)** — içerik değişmez | `POST /bildirimler/{id}/kapat` | `[ModulYetkisi("bildirim")]` + `[RolGerekli(SAHIP,MUDUR,MUHASEBE,TEZGAHTAR)]` | `DenetimServisi.YazmaKaydet` **çağrılır** (yeniden yazılmadı — ADR-0021) | Onay modalı + not alanı; kapalı satırda **DegismezIpucu** |
| `IsCalismasi` | **Salt okunur** — ölçüm kaydı | düzeltme ucu YOK; yeniden koşum `POST /isler/{kod}/calistir` | `[RolGerekli(SAHIP,MUDUR)]` + 3/dk | `IS_ELLE_CALISTIRMA` yazılır | "Şimdi çalıştır"; satır düzenlenemez |

**Neden salt okunur:** bildirimde kapatma "kim, ne zaman, hangi notla" damgasıdır — metni
sonradan değiştirilseydi damganın altı doldurulmuş olurdu. İş çalışması "iş koştu mu"
sorusunun kanıtıdır; elle düzeltilebilseydi cevap güvenilmez olurdu. İkisi de
`DuzeltmePolitikasi` haritasına **SALT_OKUNUR** damgasıyla, alternatif yoluyla birlikte eklendi
(`DuzeltmePolitikasiTestleri.Her_varlik_haritada_VARDIR` bunu zorluyor).

**Hard DELETE yok:** iki tabloda da `[HttpDelete]` yok; `Remove`/`ExecuteDelete` yok.
Bildirim **silinmez**, yalnız damgalanır; "N günden eski KAPALI bildirim listede gösterilmez"
kuralı `Isler:KapaliGosterimGun` (varsayılan 30) ile **Faz A'da** kondu ve ekranda yazıyor.

**DoD 4 kural:**
1. *Her düğme istek atar ya da disabled+gerekçe* → `BildirimEkranTestleri.Her_dugme_ya_istek_atar_ya_da_disabled_dir`
   (her buton tıklanır; ya istek çıkar ya modal açılır ya da `disabled` + `title` gerekçesi vardır).
2. *Tek düzenleme kalıbı* → yeni `duzenlenenId` kalıbı açılmadı; `ModalPencere` **yalnız**
   geri alınamaz eylemin onayı için kullanıldı.
3. *Tek dokunuş* → aşağıda.
4. *Sessiz form yasağı* → kapatma notu 500'ü aşarsa alan altında Türkçe mesaj; sunucu da
   elle kontrol ediyor (DataAnnotations yalnız controller yolunda çalışır).

**Dokunulan sayfalar (tek dokunuş kaydı):**
`Kuyumcu.Web/Pages/Bildirimler.razor` (YENİ), `Layout/UstBar.razor`, `Layout/KenarCubugu.razor`,
`Layout/RotaBasliklari.cs`, `wwwroot/css/app.css`.

## 5. Testler

| Dosya | Tür | Adet |
|---|---|---|
| `Kuyumcu.Tests/IsCatisiTestleri.cs` | DB | 6 |
| `Kuyumcu.Tests/IsSenaryoTestleri.cs` | DB | 9 |
| `Kuyumcu.Tests/BildirimHttpTestleri.cs` | HTTP | 14 |
| `Kuyumcu.Tests/BildirimEkranTestleri.cs` | bUnit | 11 |
| `Kuyumcu.Tests/IsCatisiSafTestleri.cs` | saf | 9 |
| **Toplam yeni** | | **49** |

Ayrıca mevcut iki test paket 6 yüzünden güncellendi (davranışları korunarak):
- `RotaBaslikTestleri.Yansima_TAM_27_rota_bulur` → `…_28_…` (yeni sayfa; testin amacı budur).
- `DuzeltmePolitikasiTestleri` kendiliğinden kapsadı (harita satırları eklendi).

Süit: **önce 1541 / sonra 1590** (49 yeni; `RotaBaslikTestleri` yeniden adlandırması sayıyı değiştirmez).

> **TEST BOŞLUĞU — DÜRÜSTÇE:** `HttpOrtami.cs` tüm `IHostedService`'leri kaldırır; bu yüzden
> **`IsZamanlayici`'nin kendi döngüsü hiçbir testte koşmuyor**. Döngü (uyanma aralığı, kiracı
> gezme, ikinci turun atlaması) **birim testle değil CANLIYLA doğrulandı** — §7 adım 8.
> Mutasyon kapsaması için döngünün kapsam seçimi ayrıca **kaynak tarama testiyle** ölçülüyor
> (`Zamanlayici_is_kosumunu_KIRACI_kapsaminda_yapar`); bunun bir kaynak testi olduğu kodda yazılıdır.

## 6. Mutasyon tablosu

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M6-1 | `is_calismalari` UNIQUE indeksi `unique: false` | `IsCatisiTestleri.Ayni_yerel_gunde_ikinci_kosum_ATLANIR_ve_tek_satir_kalir` | `cmp` birebir aynı |
| M6-2 | Hak talebi (claim) `SaveChanges`'i işten SONRAYA ertelendi | `IsCatisiTestleri.Ayni_yerel_gunde_ikinci_kosum_ATLANIR_ve_tek_satir_kalir` | `cmp` aynı |
| M6-3 | `bildirimler` UNIQUE indeksi `unique: false` | `IsCatisiTestleri.Iki_tur_ayni_taslagi_uretse_de_TEK_bildirim_olur` | `cmp` aynı |
| M6-4 | Tekillik anahtarına saat eklendi (`-E{HHmmssfff}`) | `IsCatisiSafTestleri.Tekillik_anahtari_kimlik_ve_yerel_gun_icerir_SAAT_icermez` **ve** `IsSenaryoTestleri.Ayni_gunde_iki_kosum_cari_basina_TEK_vade_bildirimi_birakir` | `cmp` aynı |
| M6-5 | `MasakSibSuresiIsi` `gorunur_roller` boş bırakıldı | `IsSenaryoTestleri.Suresi_yaklasan_SIB_bildirim_uretir_ve_KISISEL_VERI_TASIMAZ` **ve** `BildirimHttpTestleri.Is_ucundan_uretilen_MASAK_bildirimi_TEZGAHTARA_gorunmez` | `cmp` aynı |
| M6-6 | `SaklamaOnizlemeIsi`'nde `Onizleme` → `Uygula` | `IsSenaryoTestleri.Saklama_onizlemesi_bildirim_uretir_ama_MASKELEME_YAPMAZ`, `…_ayda_BIR_bildirim_uretir`, `IsCatisiSafTestleri.Isler_agacinda_Uygula_cagrisi_gecmez` **+ denetim.sh BAŞARISIZ** | `cmp` aynı |
| M6-7 | İkinci `IBildirimKanali` DI kaydı | `IsCatisiSafTestleri.DI_de_IBildirimKanali_uygulamasi_TAM_1_dir` **+ denetim.sh BAŞARISIZ** | `cmp` aynı |
| M6-8 | `Isler:TevsikKurtarma:Etkin` → `true` | `IsSenaryoTestleri.Tevsik_kurtarma_isi_bu_pakette_KAYITLI_DEGIL_ve_varsayilani_KAPALI` | `cmp` aynı |
| M6-9 | `IsZamanlayici`'de `kapsam.Kiraci(id)` → `kapsam.Sistem()` | `IsCatisiSafTestleri.Zamanlayici_is_kosumunu_KIRACI_kapsaminda_yapar` | `cmp` aynı |
| M6-10 | `RotaBasliklari`'ndan `/bildirimler` satırı silindi | `RotaBaslikTestleri.Tum_rotalar_baslik_tasir(rota:"/bildirimler")` + `…Ust_bar_GERCEK_rotada_gercek_basligi_cizer` | `cmp` aynı |

**Bulunan iki test açığı ve kapatılması (mutasyon önce YAKALANMADI, test güçlendirildi):**
- M6-4 yalnız saf testi kırıyordu → `IsSenaryoTestleri.Ayni_gunde_iki_kosum_cari_basina_TEK_vade_bildirimi_birakir` eklendi.
- M6-5 yalnız DB testini kırıyordu → `BildirimHttpTestleri.Is_ucundan_uretilen_MASAK_bildirimi_TEZGAHTARA_gorunmez` eklendi.

Son mutasyondan sonra dokuz dosyanın tamamı `cmp` ile **birebir aynı** doğrulandı; ardından
temiz `dotnet build` → 0 hata 0 uyarı.

## 7. `dotnet/denetim.sh`

Eklenen sayaçlar:
- `Isler/ altında Uygula( (0 olmalı)` — ADR-0008 kenarından dolanma koruması
- `Isler/ defter yazımı (0 olmalı)` — `HareketFabrikasi` / `Hareketler.Add` / `KasaHareketleri.Add`
- `IBildirimKanali DI kaydı (1 olmalı)` — ADR-0016 dış kanal tekeli
- `BEKLENEN_VARLIK 15 → 17`

Son çıktı (tamamı):
```
═══ Kiracı izolasyonu denetimi ═══
  ✓ IgnoreQueryFilters (yasak)                     0
  ✓ new KuyumcuDbContext (yalnız fabrika)         1
  ✓ Find/FindAsync (yasak)                         0
  ✓ ExecuteUpdate/Delete (beyaz liste 4)           4
  ✓ Hareketler.Add (yalnız HareketFabrikasi)      1
  ✓ VeresiyeKapanisi (yalnız Etiketten)           2
  ✓ Isler/ altında Uygula( (0 olmalı)            0
  ✓ Isler/ defter yazımı (0 olmalı)             0
  ✓ IBildirimKanali DI kaydı (1 olmalı)          1
  ✓ IKiraciVarlik uygulayan varlık                37
  ✓ HasQueryFilter                                 2

═══ Güvenlik yüzeyi ═══
  ✓ oran sınırı / HTTPS / sıkıştırma / sağlık ucu   var
  ✓ ArayuzModu (yalnız 6 dosya)                   6
  ✓ ArayuzModu kapı yüzeyinde (0 olmalı)        0
  ✓ Finansal varlıkta [HttpDelete] (0 olmalı)    0
  ✓ CORS                                           beyaz liste

DENETİM TEMİZ.
```

## 8. Canlı doğrulama (localhost:5198, gerçek veri)

Test kiracısı: **PAKET6-TEST Kuyumculuk** (`01M2NQV79B9S332XGNZF293GGB`). API
`Isler__TurDakika=1` ile koştu. **5199'a dokunulmadı.**

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /auth/kayit` | 201 + tenantId | 201, `tenantId=01M2NQV79B…` |
| 2 | Veresiye satış + vade 3 gün geriye (SQL fikstürü) | `cari_bakiyeler.tl_bakiye=12000` | `01M2NQWGDV… 12000.00` |
| 3 | **Zamanlayıcı kendi turu** (elle tetikleme YOK) | KRITIK bildirim doğar | `GET /bildirimler` → 200, 1 satır: `onem=KRITIK`, `baslik="Vadesi geçmiş veresiye borcu"`, `hedefRota=/raporlar/vade-takibi`, `ilgiliVarlikId=01M2NQWGDV…` |
| 4 | `GET /bildirimler/sayi` | 1 | `{"sayi":1,"enYuksekOnem":"KRITIK"}` |
| 5 | `POST /isler/VADE_HATIRLATMA/calistir` (MUDUR) — tekillik | yeni bildirim yok | 200, `uretilenBildirim=0, atlananBildirim=1, incelenenKayit=1` |
| 6 | Aynı dakikada 2. elle tetikleme | dürüst "atlandı" | 200, `atlandi=true` (**ilk koşumda mesaj yanıltıcıydı → S6 düzeltmesi**) |
| 7 | Elle tetikleme 4. istek/dk | 429 | `TooManyRequests` (oran sınırı 3/dk **canlıda** doğrulandı) |
| 8 | **Zamanlayıcı ikinci/üçüncü tur** (API 2dk13sn ayakta, tur 1 dk) | günlük anahtarlı satır TEK kalır | `SELECT is_kodu, COUNT(*) … calisma_anahtari NOT LIKE '%-E%'` → `MASAK_SIB_SURESI 1 · SAKLAMA_ONIZLEME 1 · VADE_HATIRLATMA 1` |
| 9 | TEZGAHTAR `GET /bildirimler` | 403 | `{"message":"Bu ekran için yetkiniz yok (bildirim)","statusCode":403}` |
| 10 | TEZGAHTAR `POST /isler/…/calistir` | 403 | aynı gövde, HTTP 403 |
| 11 | Bilinmeyen iş kodu | 404 | `{"message":"Tanımlı iş bulunamadı: YOK_BOYLE","statusCode":404}` |
| 12 | `POST /bildirimler/{id}/kapat` | 200 | `{"kapatildi":"2026-09-16T18:33:31Z","mesaj":"Bildirim kapatıldı"}` |
| 13 | Aynı bildirimi tekrar kapat | 400 | `{"message":"Bildirim zaten kapatılmış","statusCode":400}` |
| 14 | Kapatma sonrası satır sayısı | DEĞİŞMEDİ | `SELECT COUNT(*), MAX(kapatildi IS NOT NULL) FROM bildirimler …` → `1  1` |
| 15 | `limit=100000` | en çok 100 | yanıtta `"limit":100` |
| 16 | Tarayıcı `/bildirimler` | rozet 1, kapatınca kaybolur | 🔔 1 (kritik renk) göründü; kapatınca yeniden yüklemede rozet **yok**; başarı bandı "Bildirim kapatıldı"; Kapalı sekmesinde DegismezIpucu ve 30 gün penceresi notu göründü |
| 17 | `#ust-mod-anahtar` + tema seçici | bozulmadı | Basit/Profesyonel düğmeleri yerinde (ekranda ve bUnit'te) |

**Temizlik:** API durduruldu (`lsof -nP -iTCP:5198 -sTCP:LISTEN` → boş). Test kiracısı
`information_schema`'dan çekilen **37 tablodan** silindi + `tenants` satırı silindi.
Zamanlayıcının diğer kiracılarda açtığı `is_calismalari` satırları da silindi — **iki tablo da
bu oturumdan önce BOŞTU**, dolayısıyla silme onları tam olarak önceki hâline döndürdü.
`QA-TEST-1` / `QA-TEST-2` kiracılarına **dokunulmadı**: `rol_yetkileri` satır sayıları 22'de kaldı,
yeni marker/`bildirim` satırı **0**.

```
SELECT COUNT(*) FROM tenants;          → 7   (başlangıçtaki değer)
SELECT COUNT(*) FROM bildirimler;      → 0
SELECT COUNT(*) FROM is_calismalari;   → 0
SELECT COUNT(*) FROM rol_yetkileri WHERE modul='bildirim' OR modul LIKE '%tohum%'; → 0
```

## 9. Değişen dosyalar

**YENİ**
```
dotnet/Kuyumcu.Api/Data/ZamanlanmisIs.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260916181735_BildirimVeIsCatisi.cs
dotnet/Kuyumcu.Api/Data/Migrations/20260916181735_BildirimVeIsCatisi.Designer.cs
dotnet/Kuyumcu.Api/Modules/Isler/IZamanlanmisIs.cs
dotnet/Kuyumcu.Api/Modules/Isler/IBildirimKanali.cs
dotnet/Kuyumcu.Api/Modules/Isler/IsCalistirici.cs
dotnet/Kuyumcu.Api/Modules/Isler/IsZamanlayici.cs
dotnet/Kuyumcu.Api/Modules/Isler/IslerModulu.cs
dotnet/Kuyumcu.Api/Modules/Isler/Isler/VadeHatirlatmaIsi.cs
dotnet/Kuyumcu.Api/Modules/Isler/Isler/MasakSibSuresiIsi.cs
dotnet/Kuyumcu.Api/Modules/Isler/Isler/SaklamaOnizlemeIsi.cs
dotnet/Kuyumcu.Web/Pages/Bildirimler.razor
dotnet/Kuyumcu.Tests/IsCatisiTestleri.cs
dotnet/Kuyumcu.Tests/IsCatisiSafTestleri.cs
dotnet/Kuyumcu.Tests/IsSenaryoTestleri.cs
dotnet/Kuyumcu.Tests/BildirimHttpTestleri.cs
dotnet/Kuyumcu.Tests/BildirimEkranTestleri.cs
```
**DEĞİŞEN**
```
dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs            (2 DbSet + 5 indeks)
dotnet/Kuyumcu.Api/Data/SistemKapsami.cs               (izinli kullanım listesi 4 → 6)
dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs
dotnet/Kuyumcu.Api/Modules/ModulKayit.cs               (servis + kanal + 3 iş kaydı)
dotnet/Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs        (modül-başı tohumlama)
dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs  (2 yeni damga)
dotnet/Kuyumcu.Api/Common/OranSiniri.cs                (IsCalistir 3/dk)
dotnet/Kuyumcu.Api/Program.cs                          (AddHostedService<IsZamanlayici>)
dotnet/Kuyumcu.Api/appsettings.json                    ("Isler" bölümü)
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                  (Nav.Bildirimler + SozlukBildirim, TR+EN)
dotnet/Kuyumcu.Web/Layout/UstBar.razor                 (🔔 rozeti + 60 sn yoklama)
dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor            (menü bağlantısı)
dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs            (/bildirimler)
dotnet/Kuyumcu.Web/wwwroot/css/app.css                 (bildirim sınıfları, belirteçle)
dotnet/denetim.sh                                      (3 yeni sayaç + varlık 17)
dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs             (27 → 28)
```
> `git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısında bu dosyaların bir kısmı `??`
> görünür; depoda yalnız tek bir "first commit" olduğu için birçok dosya izlenmiyor.
> **COMMIT YAPILMADI.** Projede `.mutorj`/`.bak`/`.tmp` **bırakılmadı**; depodaki 16 adet
> `*.mutorj` önceki oturumlardan kalmadır (hiçbiri bu oturumda üretilmedi — `find -newermt` boş).

## 10. Koşul karşılama tablosu

| Koşul (birleşik / ek) | Durum | Kanıt |
|---|---|---|
| DbUpdateException **yalnız MySQL 1062** ise "zaten çalıştı" | ✅ | `IsCalistirici.BenzersizlikIhlali` + `Benzersizlik_DISI_veritabani_hatasi_ATLANDI_sayilmaz_HATALI_yazilir` |
| Diğer istisnalar HATALI kaydı, `hata_ozeti` ≤500 ve **sınıflandırılmış** | ✅ | `Siniflandir()` ham mesajı yazmaz; `Is_istisna_atarsa_durum_HATALI_ve_hata_ozeti_SIR_TASIMAZ` (parola dizesi sızmıyor) |
| Tohumlama mekanizması burada kurulur, idempotent, eski-marker testi | ✅ | `YetkiModulu` + `Eski_marker_li_kiracida_bildirim_anahtari_TOHUMLANIR` |
| Kiracı izolasyonu: iş başına ayrı DI scope + `SistemKapsami.Kiraci(id)` | ✅ | `IsZamanlayici.Tur` + M6-9 |
| `IgnoreQueryFilters` = 0 | ✅ | denetim.sh |
| denetim.sh sayaçları (Uygula=0, HareketFabrikasi=0, kanal=1) + varlık 17 | ✅ | §7 |
| Görünürlük süzmesi **sunucuda**; MASAK TEZGÂHTAR'a görünmez (HTTP testi) | ✅ | `BildirimServisi.Gorunur` + 2 HTTP testi |
| Elle tetikleme SAHIP/MUDUR + 3/dk + denetim izi | ✅ | `[RolGerekli]` + `EnableRateLimiting` + canlı 429 + `IS_ELLE_CALISTIRMA` |
| Bildirimler SİLİNMEZ; "N günden eski KAPALI gösterilmez" Faz A'da | ✅ | `Isler:KapaliGosterimGun=30`, ekranda yazıyor |
| Zamanlayıcı döngüsü **canlıyla** doğrulandı, raporda yazıldı | ✅ | §5 notu + §8 adım 3/8 |
| UstBar rozeti: bUnit'te ilk yükleme + elle yenile | ✅ | `Rozet_sayi_3_olunca_gorunur_ve_ELLE_YENILE_ile_guncellenir` (zamanlayıcı beklenmiyor) |
| `#ust-mod-anahtar` + tema seçici bozulmadı | ✅ | aynı testte + `TemaEkranTestleri` yeşil |
| UX-3: basit modda rozet görünür, iş çatısı ProAlan içinde | ✅ | UstBar mod koşulsuz; iş paneli `<ProAlan>` içinde |
| MÜHÜR 2: finansal hard DELETE yok, PATCH/DELETE kapıları, DoD 4 kural | ✅ | §4 |
| 9 Eylül sızıntı satırlarına dokunulmadı | ✅ | değişen dosya listesinde yok |
| TEVSIK_KURTARMA yazılmadı, varsayılan KAPALI | ✅ | M6-8 testi |
| Katman 8 UX-S21…S33 | ✅ (aşağıda) | |

### UX-S21…S33 karşılıkları
- **S21/S22** — ekranda kullanıcıdan istenen tek alan kapatma notudur (isteğe bağlı); durum,
  limit, kapalı penceresi ve önem **sunucudan** gelir, seçici kurulmaz.
- **S23** — "Şimdi çalıştır" sonrası etkisi **aynı ekranda** görünür (yeni bildirim listeye düşer,
  çalışma geçmişi tablosu güncellenir, banda `uretilenBildirim` yazılır).
- **S24** — durum sekmesi değişince liste yeniden çekilir (sessiz değil, istek atılır).
- **S25** — kapalı bildirimde **DegismezIpucu** (gerekçe + doğru yol); "uç var düğme yok" durumu yok.
- **S26** — bu pakette oluşturma formu yok (kayıtları iş üretir); kapatma tek formdur.
- **S27** — her yazma başarı bandıyla biter ve **POST yanıtının `mesaj` alanı** banda yazılır.
- **S28** — `catch` bloklarında liste **boşaltılmaz**; boş durum yalnız istek başarılıyken çıkar.
- **S29** — `azamiLimit` ve `dahaFazlasiVar` sunucudan gelir, ekranda uyarı bandı basılır.
- **S30** — kapalı işin düğmesi `disabled` + yanında/`title`'da sebebi yazar.
- **S31** — ham kod ekrana basılmaz; önem/durum metinleri sözlükten (TR+EN).
- **S32** — bu ekranda tutar kartı yok; bildirim gövdesindeki tutar işin ürettiği metindir
  ve `InvariantCulture` ile biçimlenir (kültür tuzağı).
- **S33** — dönem seçicisi yok; liste "şu an açık olanlar"dır, sekmede yazılıdır.

## 11. Açık kalanlar

1. **Faz B yazılmadı** (kapsam gereği): `TevsikKurtarmaIsi`, `/bildirimler/is-calismalari`
   ayrı ekranı, WhatsApp derin bağlantısı. İş çalışmaları **Faz A'da /bildirimler içindeki
   ProAlan panelinde** listeleniyor; ayrı ekran gerekmedi.
2. **Vade bildirimi müşteri adını taşımıyor** (kişisel veri koşulu). Kuyumcu "hangi müşteri"
   sorusunu ancak hedef ekrana giderek yanıtlıyor. Rozet/bildirim gürültüsüyle birlikte sahada
   ölçülmeli; gerekirse "ad yalnız SAHIP/MUDUR'a görünür" gibi rol bazlı bir kural **ekip oyuna**
   açılmalı. Bugün varsayım yapılmadı.
3. **Elle tetikleme dakika çözünürlüklü.** Aynı dakikadaki ikinci tetikleme koşmaz; yanıt
   bunu açıkça söylüyor ama arayüzde "1 dakika bekleyin" geri sayımı yok.
4. **Kapalı bildirim arşivi**: "30 günden eski kapalı bildirim listede gösterilmez" kondu, ama
   satırlar büyümeye devam ediyor. Arşiv görünümü / dışa aktarım kararı hâlâ **ekip oyunda**.
5. **`denetim.sh` varlık sayacı `-ge` mi `=` mi** — bu pakette karara bağlanmadı (spesifikasyonun
   kendi notu). Sayı 17'ye çıkarıldı, ölçülen gerçek 37.
6. **Rozet yoklaması 60 sn.** SignalR ile anlık besleme (Program.cs `/hubs/kur`) Faz A'da
   **seçilmedi** (kiracı bazlı grup yönetimi güvenlik yüzeyini büyütür). Karar açık.
7. **Zamanlayıcı "zaten çalışmış" kaydı `LogDebug`** seviyesindedir; üretim log seviyesi
   Information olduğu için canlıda log satırı **görünmedi** — atlama kanıtı SQL sayımıyla alındı.
   İstenirse seviye Information'a çekilebilir (gürültü bedeliyle).

## 12. Kurucuya / mali müşavire notlar

1. **VUK mükerrer md.355/4 (tevsik aykırılığını beş iş günü içinde kendiliğinden bildirme)
   birincil kaynaktan DOĞRULANMADI.** Bu yüzden ilgili iş **yazılmadı**; yapılandırma anahtarı
   `Isler:TevsikKurtarma:Etkin = false` olarak duruyor ve hiçbir davranışı değiştirmiyor.
   Hükmü mali müşavir teyit etmeden iş açılmamalı; açılırsa da **iş günü hesabı yapılmayacak**
   (üründe resmî tatil takvimi yok — `Takvim:ResmiTatiller` boş).
2. **MASAK 10 iş günü hesabı bu pakete ait değildir.** Paket 6 `son_bildirim_tarihi` alanını
   yalnız okur. O tarihin mevzuata uygunluğu MASAK paketinin sorumluluğundadır ve
   `Takvim:ResmiTatiller` **boş** olduğu sürece bayramlar iş günü sayılır → **süre erken dolar**
   (güvenli taraf, ama yıllık olarak doldurulması gerekir).
3. **Kuyum Ticareti Yönetmeliği yetki belgesi tadili (10 gün)** için hatırlatma **konulmadı**:
   üründe yetki belgesi tarihi tutan bir alan yok, hatırlatılacak veri yok. İstenirse önce
   işletme kartına belge tarihi alanı gerekir.
4. **ADR-0008 değişmedi**: saklama süresi dolmuş kişisel verinin maskelenmesi hâlâ **elle**
   tetiklenir. Paket 6 yalnız "süresi dolmuş kayıt var" hatırlatması üretir (ayda bir).
5. **Dış kanal (e-posta/SMS/WhatsApp) AÇILMADI** — sözleşme, KVKK aydınlatma metni ve gönderen
   kimliği gerektirir. Arayüz hazır, uygulama tek ve uygulama içidir; ikinci bir kanal
   eklenmesi denetim ve testten geçmeden mümkün değil (ADR-0016).

## 13. Sahiplik notu — ADR-0027

Mimari belgesindeki **ADR-0027 "Yetki tohumlama sahipliği paket 29'da"** kararı, paket 29
henüz uygulanmadığı için **fiilen paket 6'da** hayata geçti (birleşik koşul 2'nin açık verdiği
"ya burada kurulur" seçeneği). Sonraki paketler (3, 29, 31, 32, 45, 46) mekanizmayı **yeniden
kurmayacak**; yalnız kendi anahtarını `Moduller.Tumu` + `Moduller.Varsayilan`'a ekleyecek.
`Moduller.TohumlamaOncesi` dizisi **dondurulmuştur** ve büyütülmemelidir.
