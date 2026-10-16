# PAKET UX-2 — Faz A1 uygulama raporu

> **Kapsam:** Görsel çekicilik — belirteçler + karanlık tema + rota başlıkları (SIFIR DOM DEĞİŞİKLİĞİ)
> **Tarih:** 2026-09-16 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Dalga:** 0
> **Durum:** BİTTİ (Faz A1). Faz A2 (hamburger/çekmece), Faz B/C/D kapsam dışı.
> **Commit YOK** — çalışma ağacında.

---

## 1. Özet

| | |
|---|---|
| Derleme | `dotnet build Kuyumcu.sln --nologo` → **0 Hata · 0 Uyarı** |
| Testler | **971 → 1027** (+56 yeni), tam süit **1027/1027 yeşil**, 0 atlanan |
| Mevcut 971 test | **DEĞİŞTİRİLMEDİ**, hepsi yeşil (Ölçülebilir Kabul 4) |
| Migration | **GEREKMEDİ** (UX-2 veritabanına dokunmaz; tema tercihi localStorage'da) |
| denetim.sh | **DENETİM TEMİZ** |
| Mutasyon | 5/5 kırmızıya döndü, hepsi `cmp` ile birebir geri alındı (+1 ek mutasyon iki test açığı buldu ve kapattı) |
| Canlı | 5198'de doğrulandı, sunucu durduruldu; **5199'a dokunulmadı** |
| Test kiracısı | `PAKETUX-2-TEST Kuyumcu` açıldı ve **silindi**; `tenants` = 5 → 5 |

---

## 2. Yapılanlar

### 2.1 `wwwroot/css/belirtecler.css` (YENİ, 291 satır)
Üç katman:
1. **Ham palet** — `--amber-*`, `--slate-*`, `--yesil*`, `--kirmizi*`, `--mavi*`, `--sari*`.
   app.css'ten **taşındı**, isimler KALDIRILMADI (spesifikasyon şartı).
   kilavuz.html'in kullandığı kısa adlar (`--yesil`, `--kirmizi-bg`, `--gri-bg`, `--golge`,
   `--golge-lg`, `--r`, `--amber-600`) da buraya alındı → **üçüncü palet kaynağı kapandı**.
2. **Semantik katman** — `--yuzey`, `--yuzey-2`, `--yuzey-3`, `--yuzey-govde`, `--metin`,
   `--metin-guclu`, `--metin-soluk`, `--kenarlik`, `--ayrac`, `--vurgu*`, `--basari*`,
   `--uyari*`, `--tehlike*`, `--bilgi*`, `--kenar-*` (kenar çubuğu), `--golge*`.
3. **Ölçü** — `--bosluk-1..7`, `--yaricap*`, `--yazi-*`, `--satir-yuksekligi`, `--kalin*`.
   Geriye dönük takma adlar: `--govde`, `--radius-md/lg/xl`, `--r`.

Tema:
```css
:root { … aydınlık … color-scheme: light; }
@media (prefers-color-scheme: dark) { :root:not([data-tema="aydinlik"]) { … karanlık … } }
:root[data-tema="karanlik"] { … karanlık … }
:root[data-tema="aydinlik"] { color-scheme: light; }
```
`:not([data-tema="aydinlik"])` koruması sayesinde kullanıcı "Aydınlık" derse sistem
karanlıkken bile aydınlık kalır.

### 2.2 `wwwroot/css/app.css` (DEĞİŞİKLİK)
- **Hiçbir seçici adı, hiçbir seçicinin özgüllüğü değişmedi** (Bertan KOŞUL 4). Yalnız
  bildirim DEĞERLERİ `var(--semantik)` ile değiştirildi.
- `:root` palet bloğu kaldırıldı (belirtecler.css'e taşındı); buna karşılık `.dugme*` takma adları
  ve dar ekran blokları eklendi → dosya 468 → **496 satır**.
- **Kâğıt dışı ham hex: 71 → 0.** Kalan 25 hex'in tamamı `.belge*`, `.urun-etiket*` ve
  `@media print` bloklarındadır (TEMADAN MUAF — Bertan KOŞUL 5).
- `.urun-etiket`'teki `var(--kenarlik, #999)` → `#999`, `.belge`'deki `var(--slate-200)`/
  `var(--radius-md)` → ham değer. Bugünkü çıktı birebir aynı (belirteçler o gün tanımsızdı,
  fallback kullanılıyordu); fark: artık karanlık tema kâğıda **sızamaz**.
- **`.dugme*` takma adları eklendi** (Aşkın K1): `.dugme`, `.dugme-birincil`, `.dugme-ikincil`,
  `.dugme-tehlike`, `.dugme-kucuk`, `.dugme-satir`. Razor'daki sınıf adları DEĞİŞTİRİLMEDİ.
  → Uyum, Gider Pusulaları, Pusula Belgesi ve Denetim İzi ekranlarındaki 17 düğme bugüne
  kadar **stilsiz** çiziliyordu; artık `.btn` ailesiyle aynı görünüyor.
- **Dar ekran temel katmanı** (`@media (max-width:768px)` ve `480px`): form ızgaraları tek
  sütun, modal genişliği sınırlı, kart/modal iç boşlukları daraltıldı, `.belge-taraflar`/
  `.belge-imza` tek sütun. `.tablo-sar`a `max-width:100%`, `.grafik-sar`a `min-width:0`.

### 2.3 Karanlık tema altyapısı
| Dosya | İçerik |
|---|---|
| `wwwroot/js/tema.js` (YENİ) | `window.kuyumcuTema.uygula(mod)` / `.oku()`. Beyaz liste `['aydinlik','karanlik']`; dışındaki her değer → `removeAttribute('data-tema')` + localStorage temizliği. Dosya sonunda `uygula(oku())` → **FOUC yok**. `eval` YOK, satır içi `<script>` YOK. |
| `wwwroot/js/dil.js` (YENİ) | `window.kuyumcuDil.uygula(dil)`. **Mehmet K1 gereği** DilServisi'ndeki `eval` kaldırıldı (aşağıda "Bulunan hata"). |
| `Servisler/TemaServisi.cs` (YENİ) | `Mod` ∈ {sistem, aydinlik, karanlik}; beyaz liste C# tarafında da uygulanır (iki taraf da güvenmez). `kuyumcuTema.uygula` adlandırılmış çağrısı. |
| `Bilesenler/TemaSecici.razor` (YENİ) | `#tema-sistem` / `#tema-aydinlik` / `#tema-karanlik`, `aria-pressed`, DilSecici pill deseni. |
| `Pages/Ayarlar.razor` | `#ayar-tema` kartı eklendi (**UstBar/KenarCubugu'na DOM eklenmedi**). |
| `Program.cs` (Web) | `TemaServisi` DI kaydı + açılışta `YukleAsync()`. |
| `wwwroot/index.html` | `belirtecler.css` → `app.css` sırası; `<meta name="color-scheme" content="light dark">`; `tema.js` + `dil.js` **blazor.webassembly.js'ten ÖNCE**. |

### 2.4 Rota başlığı haritası
`Layout/RotaBasliklari.cs` (YENİ) — 27 rota şablonu → `Sozluk` seçici işlevi.
- UstBar'daki `switch` (12 rota) ve `_ => "Kuyumcu"` **geri dalı KALDIRILDI**.
- Eşleştirme **şablon** iledir: `/gider-pusulalari/{Id}/belge` artık eşleşiyor
  (eski kod ham `AbsolutePath` karşılaştırdığı için bu rota hiçbir zaman eşleşmiyordu).
- `Bul()` eşleşme yoksa `null` döner; `Baslik()` bunu `Nav.Bulunamadi` ("Sayfa bulunamadı")
  ile karşılar. Bu bir "geri dal" değildir: **gerçek** hiçbir rota buraya düşemez (test iki
  yönlü zorlar), yalnız var olmayan bir adres (Router'ın `NotFound` dalı) düşer.
- `UstBar.razor` DOM'u değişmedi; yalnız `SayfaBasligi` gövdesi tek satıra indi.

### 2.5 Sözlükler (TR + EN)
- Başlıktaki **"auto-generated / ELLE DÜZENLEMEYİN"** uyarısı düzeltildi (K-UX-10):
  üreteç `scripts/gen-i18n-cs.mjs` ve kaynağı `packages/core` depoda YOK; dosya elle düzenlenir.
- `SozlukNav` += `PusulaBelgesi`, `Giris`, `Bulunamadi` (TR/EN).
- `SozlukTema` (YENİ) = `Baslik`, `Aciklama`, `Sistem`, `Aydinlik`, `Karanlik` (TR/EN);
  `Sozluk.Tema` kökte açıldı.

### 2.6 kilavuz.html
- Gömülü `:root` paleti **silindi**, `<link rel="stylesheet" href="css/belirtecler.css" />` eklendi.
- Sayfa kabuğu kuralları (body, `.toc`, `main`, `section`, `h2/h3`, `code`, `strong`, `a.ic`,
  `.kutu.*`, `figcaption`, `footer`, `.anahtar`, `.ust-not`, `.ust-link`, adım numaraları)
  semantik belirteçlere bağlandı → kılavuz artık karanlık temayı izler.
- **Bilinçli istisna:** "ekran temsili" figürleri (`.ekran`, `.app`, `.stat`, `table.mini`,
  `.kurbar`, `.form`, `.btn*`) aydınlık palette bırakıldı — bunlar uygulamanın *resmidir*,
  ekran kabuğu değildir. Raporda açıkça yazıldı, sessiz bırakılmadı.

---

## 3. Bulunan gerçek hatalar (paket dışı, kanıtlı)

1. **`DilServisi.cs:27` CSP tarafından engelleniyordu.**
   `js.InvokeVoidAsync("eval", $"document.documentElement.lang = '{Dil}'")` —
   CSP `script-src 'self' 'wasm-unsafe-eval'` (Kuyumcu.Api/Program.cs) JS `eval()`'ına
   **izin vermez**; `'wasm-unsafe-eval'` yalnız WebAssembly derlemesini kapsar. Yani dil
   değiştirildiğinde `<html lang>` hiç güncellenmiyordu. Mehmet'in K1 koşulu bu düzeltmeye
   açıkça izin veriyor; `js/dil.js` + adlandırılmış çağrıya çevrildi.
2. **`.dugme*` sınıf ailesi CSS'te hiç tanımlı değildi** (Aşkın K1 doğrulandı):
   razor'da 17 kullanım, app.css'te 0 seçici. Uyum / Gider Pusulaları / Pusula Belgesi /
   Denetim İzi ekranlarındaki düğmeler tarayıcı varsayılanıyla çiziliyordu.
3. **`/gider-pusulalari/{Id}/belge` başlığı hiçbir zaman eşleşmiyordu** (Bertan doğrulandı).

---

## 4. Testler

### 4.1 Yeni test dosyaları (+56 test)

| Dosya | Test | Neyi tutar |
|---|---|---|
| `TasarimBelirtecTestleri.cs` | 12 | WCAG 2.1 kontrast (24 çift × 3 tema), belirteç eksiksizliği, iki karanlık bloğun eşitliği, ham hex ratchet, kâğıt muafiyeti, index.html yükleme sırası, tema.js eval yasağı, kilavuz.html palet yokluğu, dar ekran kuralları, `.dugme*` takma adları, kontrast hesabının kendi doğruluğu |
| `RotaBaslikTestleri.cs` | 33 | 27 rota (yansımayla) × başlık, iki yönlü harita kontrolü, parametreli rota, UstBar çizimi, TR→EN değişimi, bilinmeyen adres |
| `TemaEkranTestleri.cs` | 7 | `kuyumcuTema.uygula` çağrı **argümanı**, sistem/aydınlık/karanlık, beyaz liste dışı değer, Ayarlar'daki kart + şifre formunun bozulmadığı |
| `GrafikKulturTestleri.cs` | 4 | AlanGrafik/CubukGrafik SVG'sinde tr-TR ondalık virgülü yokluğu, polyline ayrıştırılabilirliği, eksen etiketinde HTML kaçışı (XSS) |

Ölçüm yöntemleri (gizlenmiş varsayım yok):
- **Kontrast:** WCAG 2.1'in kendi tanımı (sRGB doğrusallaştırma → göreli parlaklık →
  `(L1+0.05)/(L2+0.05)`). Hesabın kendisi de sınanıyor: siyah/beyaz = 21.00, `#777`/beyaz = 4.48.
- **`var()` zincirleri çözülür** (`--govde` → `--yuzey-govde` → `#f8fafc`).
- **Kapsam dışı (bilinçli):** alfa içeren değerler (`rgb(251 191 36 / .2)`), gradyanlar —
  altındaki zemin bilinmeden kontrast hesaplanamaz.
- **`--ayrac` kontrast kapsamı dışında:** dekoratif ayraç çizgisi; WCAG 1.4.11 salt dekoratif
  sınırları kapsamaz. Etkileşimli bileşen kenarı için `--kenarlik` var ve **≥3:1 ölçülüyor**
  (aydınlık 3.18:1, karanlık 3.49:1).
- **Ham hex ratchet:** `HamHexTavani = 0`. Sayım *bildirim* düzeyinde yapılır (yorumlar
  ayıklanır); kâğıt bağlamı (`.belge*`, `.urun-etiket*`, `@media print`) muaftır.

### 4.2 Koşumlar

| Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Başlangıç (dokunmadan önce) | 971 | 971 | 0 | 0 |
| `~TasarimBelirtec` | 12 | 12 | 0 | 0 |
| `~RotaBaslik` | 33 | 33 | 0 | 0 |
| `~Tema` | 11 | 11 | 0 | 0 |
| `~GrafikKultur` | 4 | 4 | 0 | 0 |
| **Tam süit (trx)** | **1027** | **1027** | **0** | **0** |

trx: `…/scratchpad/trx-pUX-2/_Mac_2026-09-16_00_49_04.trx` ve sonraki koşum.
Süre ~12 s. Kararsız test **görülmedi** (§6 Tuzaklar'daki adı yakalanamayan kırmızı bu
oturumda tekrarlamadı — çözülmüş sayılmaz, sadece görülmedi).

---

## 5. Mutasyon kanıtları

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| **M-UX2A1-01** | `belirtecler.css` `:root`tan `--metin-soluk: #475569;` **silindi** | `TasarimBelirtecTestleri.Kontrast_AA_saglanir` ("AYDINLIK: --metin-soluk/--yuzey → belirteç çözülemedi") · `…Her_semantik_belirtecin_AYDINLIK_ve_KARANLIK_karsiligi_VARDIR` | `cmp` birebir |
| **M-UX2A1-01b** | Karanlık **medya** bloğunda `--metin: #e8eef6` → `#3a4a5e` (kontrast 4.5 altına) | `…Kontrast_AA_saglanir` ("KARANLIK-MEDYA: --metin(#3a4a5e)/--yuzey(#16202e) = 1.81:1") · `…Iki_karanlik_blok_AYNI_belirtecleri_AYNI_degerle_tanimlar` | `cmp` birebir |
| **M-UX2A1-02** | `RotaBasliklari.Harita`dan `["/emanet"]` satırı silindi | `RotaBaslikTestleri.Tum_rotalar_baslik_tasir(rota: "/emanet")` ("'/emanet' rotası RotaBasliklari.Harita'da YOK") · `…Ust_bar_GERCEK_rotada_gercek_basligi_cizer` | `cmp` birebir |
| **M-UX2A1-03** | `@media print` içinde `background: #fff` → `var(--yuzey)` | `TasarimBelirtecTestleri.Print_ve_belge_bloklari_SEMANTIK_BELIRTECE_referans_vermez` ("@media print body, .ana-icerik, .belge-sayfa { background: var(--yuzey) } → --yuzey") | `cmp` birebir |
| **M-UX2A1-04** | `TemaServisi`: `uygula(Mod)` → `uygula(mod == Sistem ? "" : mod)` | `TemaEkranTestleri.Sisteme_donunce_SISTEM_argumani_gider_BOS_DIZGE_DEGIL` · `…Beyaz_liste_disi_deger_SISTEME_duser` | `cmp` birebir |
| **M-UX2A1-05** | `AlanGrafik.razor`: `CizgiNoktalari`'nden `InvariantCulture` kaldırıldı | `GrafikKulturTestleri.TURKCE_kulturde_AlanGrafik_SVG_sinde_ONDALIK_VIRGUL_YOKTUR` · `…polyline_noktalari_AYRISTIRILABILIR` | `cmp` birebir |

### İki mutasyon TEST AÇIĞI buldu — ikisi de kapatıldı (Anayasa md.1)
1. **M-UX2A1-01b ilk koşuda YEŞİL kaldı.** Sebep: karanlık tema CSS'te **iki blokta**
   tanımlı (medya sorgusu + `data-tema`) ve test ikisini tek sözlüğe indiriyordu → son
   yazan kazanıyor, ilkinin bozulması ölçülmüyordu. Düzeltme: `KaranlikMedya` ve
   `KaranlikTercih` ayrı tutuldu, **her ikisi ayrı ölçülüyor** ve yeni bir test
   (`Iki_karanlik_blok_AYNI_belirtecleri_AYNI_degerle_tanimlar`) ikisinin eşitliğini
   zorluyor. Mutasyon tekrarlandı → kırmızı.
2. **M-UX2A1-05 ilk koşuda yalnız BİR testi kırdı.** `SayilarNoktaliOlmali` yardımcısı
   `points` değerini hem boşluk hem virgülle bölüyordu; bozuk `"0,0 168,5"` de sayı gibi
   görünüyordu. Düzeltme: boşluk **nokta ayıracı**, virgül **x/y ayıracı** olarak ayrıldı ve
   ikiden fazla virgül parçası hata sayıldı. Mutasyon tekrarlandı → **iki test de** kırmızı.

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln` → 0 Hata / 0 Uyarı.

---

## 6. denetim.sh

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
  ✓ oran sınırı / HTTPS yönlendirme / sıkıştırma / sağlık ucu   var
  ✓ CORS                                           beyaz liste
DENETİM TEMİZ.
```
denetim.sh **değiştirilmedi** (yalnız `Kuyumcu.Api`'yi tarar; UX-2 Web/Core/Tests'e dokunur).
Spesifikasyonun "denetim.sh'a hex sayacı eklenir" maddesi **Faz D**'dir; ratchet şu an testte.

---

## 7. Canlı doğrulama (port 5198)

Sunucu: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Kuyumcu.Api --no-build
--no-launch-profile --urls http://localhost:5198` · PID 44784 · iş sonunda `kill` edildi,
`lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**. **5199 (kurucunun inceleme sunucusu) çalışır
durumda bırakıldı, dokunulmadı.**

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `GET /saglik` | 200 `Healthy` | **200**, gövde `Healthy` |
| 2 | `GET /` içeriği | `css/belirtecler.css` satır 11, `css/app.css` satır 12 | **doğru sıra** (belirteçler önce) |
| 3 | `GET /` içeriği | `js/tema.js` (32) < `_framework/blazor.webassembly.js` (36) | **doğru sıra** (FOUC yok) |
| 4 | `GET /` içeriği | `<meta name="color-scheme" content="light dark">` | **var** (satır 7) |
| 5 | `GET /css/belirtecler.css` | 200, `text/css` | **200**, 10.918 bayt, `prefers-color-scheme`/`data-tema` 9 eşleşme |
| 6 | `GET /css/app.css` | 200, `text/css` | **200**, 30.012 bayt |
| 7 | `GET /js/tema.js` | 200, `text/javascript` | **200**, 2.513 bayt |
| 8 | `GET /js/dil.js` | 200, `text/javascript` | **200**, 1.326 bayt |
| 9 | `GET /kilavuz.html` | belirtecler.css bağlı, gömülü palet YOK | **`<link … css/belirtecler.css>` var**, `--amber-50:` / `--slate-900:` **yok** |
| 10 | CSP başlığı — `/saglik`, `/css/belirtecler.css`, `/emanet`, `/gider-pusulalari/01JTEST/belge` | Program.cs'teki dizgeyle **birebir aynı**, `script-src`'de `'unsafe-inline'` YOK | **dördünde de aynı**, gevşetme yok |
| 11 | 27 rotanın tamamı `GET` | hepsi 200 (SPA fallback) | **27/27 → 200, 0 hata** |
| 12 | `POST /api/v1/auth/kayit` (PAKETUX-2-TEST Kuyumcu) | 201 | **201**, `tenantId` okundu |
| 13 | `POST /api/v1/auth/giris` | 200 + token | **200**, token uzunluk 275 |
| 14 | `GET /api/v1/auth/ben` | 200, rol SAHIP | **200**, `"rol":"SAHIP"` |
| 15 | `GET /api/v1/yetkiler/benim` | 200 | **200**, 8 modül |
| 16 | `GET /api/v1/kur/guncel` | 200 | **200**, `altinHasFiyati` 6717,91 |

**Temizlik SQL'i** (parola değişkenden okundu, ekrana basılmadı):
`information_schema`'dan `tenant_id` kolonu olan **31 tablo** bulundu, hepsinden
`DELETE … WHERE tenant_id='01M2KH29KK5PFRYRCGWS4HAWY4'`, sonra `tenants`'tan silindi.
- `SELECT COUNT(*) FROM tenants` → **5** (başlangıç: 5) ✔
- `SELECT COUNT(*) FROM tenants WHERE ad LIKE 'PAKETUX-2-TEST%'` → **0** ✔
- 31 tablonun hepsinde kalan satır → **YOK** ✔

### ⚠ ÖLÇÜLMEYEN (dürüstçe raporlanıyor)
- **375 / 768 / 1280 px yatay kaydırma ÖLÇÜLMEDİ.** Bu makinede Playwright/tarayıcı
  otomasyonu yok; Blazor WASM olduğu için `curl` ile DOM ölçülemez. Yapılan:
  Aşkın'ın K3'ündeki *kural varlığı* CSS düzeyinde teste bağlandı
  (`Dar_ekran_kurallari_ve_tasma_hapsi_VARDIR`): `.icerik-sutunu{min-width:0}`,
  `.tablo-sar{overflow-x:auto;max-width:100%}`, `.grafik-sar{min-width:0}`,
  `@media(max-width:768px)` form ızgarası tek sütun + modal genişliği.
- **375 px'te yatay kaydırma HÂLÂ VARDIR** ve bu bilinçlidir: `.kenar-cubugu` 240 px
  sabit genişlikte duruyor. Çekmece/hamburger **Faz A2**'nin işidir (Bertan KOŞUL 2:
  DOM eklemek Faz A1'in "sıfır DOM" sözünü bozar). Ölçülebilir Kabul 2 **A1 sonunda
  karşılanmamıştır**; A2'de karşılanacaktır.
- **Karanlık temanın gözle kontrolü yapılmadı** (işletim sistemi teması değiştirilip
  ekran görüntüsü alınmadı). Kontrast sayısal olarak ölçüldü; *görsel* onay kurucunun
  5199'daki tarayıcısında yenileme ile yapılabilir (kaynak wwwroot sunuluyor).
- **Yazdırma önizlemesi gözle kontrol edilmedi.** Kâğıt muafiyeti testle zorlandı
  (print/belge blokları semantik belirtece referans vermiyor) ve o bloklardaki hiçbir
  değer değiştirilmedi — ama gözle doğrulama Faz C'ye kalıyor.

---

## 8. Değişen dosyalar

**YENİ**
```
dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css
dotnet/Kuyumcu.Web/wwwroot/js/tema.js
dotnet/Kuyumcu.Web/wwwroot/js/dil.js
dotnet/Kuyumcu.Web/Servisler/TemaServisi.cs
dotnet/Kuyumcu.Web/Bilesenler/TemaSecici.razor
dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs
dotnet/Kuyumcu.Tests/TasarimBelirtecTestleri.cs
dotnet/Kuyumcu.Tests/RotaBaslikTestleri.cs
dotnet/Kuyumcu.Tests/TemaEkranTestleri.cs
dotnet/Kuyumcu.Tests/GrafikKulturTestleri.cs
```
**DEĞİŞİKLİK**
```
dotnet/Kuyumcu.Core/I18n/Sozlukler.cs
dotnet/Kuyumcu.Web/Layout/UstBar.razor
dotnet/Kuyumcu.Web/Pages/Ayarlar.razor
dotnet/Kuyumcu.Web/Program.cs
dotnet/Kuyumcu.Web/Servisler/DilServisi.cs
dotnet/Kuyumcu.Web/wwwroot/css/app.css
dotnet/Kuyumcu.Web/wwwroot/index.html
dotnet/Kuyumcu.Web/wwwroot/kilavuz.html
```
Mutasyon yedekleri scratchpad'dedir; proje ağacına **hiçbir** `.mutorj`/`.bak`/`.tmp`
bırakılmadı. (Önceki oturumlardan kalan 16 `*.mutorj` duruyor — bu pakette dokunulmadı.)

---

## 9. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Bertan K1 — kilavuz.html gömülü palet silinip belirtecler.css bağlanacak | ✅ | `Kilavuz_html_kendi_paletini_TASIMAZ_belirtecleri_baglar` + canlı GET |
| Bertan K2 — Faz A1 gerçekten sıfır DOM (çekmece A2'ye) | ✅ | Mevcut 971 test değiştirilmeden yeşil; KenarCubugu/UstBar DOM'u değişmedi |
| Bertan K3 — rota haritası iki yönlü, geri dal yok, şablon eşleştirme | ✅ | `Tum_rotalar_baslik_tasir` (yansıma), `Haritada_GERCEK_olmayan_anahtar_YOKTUR`, `Parametreli_rota_SABLONLA_eslesir` |
| Bertan K4 — hiçbir seçici adı/özgüllüğü değişmez | ✅ | app.css'te yalnız değerler değişti; `.dugme*` **yeni** seçici (var olan bir kuralı ezmiyor) |
| Bertan K5 — belge/etiket/print temadan muaf, TESTLE zorlanır | ✅ | `Print_ve_belge_bloklari_SEMANTIK_BELIRTECE_referans_vermez` + M-UX2A1-03 |
| Bertan K6 — BelgeYerlesimi | ⏭ Faz C | A1 kapsamı dışı |
| Mehmet K1 — eval yasak, adlandırılmış fonksiyon + beyaz liste; DilServisi de düzeltilir | ✅ | `tema.js`, `dil.js`, `Tema_betigi_EVAL_kullanmaz…` |
| Mehmet K2 — satır içi `<script>` yok | ✅ | `Index_html_… Assert.DoesNotContain("<script>")` + canlı CSP |
| Mehmet K3 — dış kaynak yok, CSP gevşetilmez | ✅ | Yalnız yerel `<link>`; canlı CSP başlığı birebir aynı |
| Mehmet K4 — MarkupString'e kullanıcı verisi encode'suz girmez | ✅ | `Eksen_etiketi_KULLANICI_GIRDISI_ise_HTML_KACISI_yapilir` |
| Mehmet K5 — data-tema sunucuya gitmez | ✅ | Yalnız localStorage; API ucu eklenmedi |
| Aşkın K1 — `.dugme*` takma adı eklenir, razor değişmez | ✅ | `Dugme_takma_adlari_TANIMLI_dir`; razor dosyaları değişmedi |
| Aşkın K2 — 18 test-bağımlı seçici korunur | ✅ | 971 test **değiştirilmeden** yeşil |
| Aşkın K3 — taşma ölçümü `<main>` üzerinde | ⚠ KISMEN | Kural varlığı testle; **gerçek piksel ölçümü YAPILMADI** (§7) |
| Aşkın K4 — kontrast ölçülebilir tanımla, var() çözülür, alfa kapsam dışı | ✅ | `Kontrast_AA_saglanir` + `Kontrast_hesabi_WCAG_referans_degerlerini_URETIR` |
| Aşkın K5 — rota listesi yansımadan | ✅ | `YansimayaGoreRotalar()` → `[Route]` öznitelikleri |
| Aşkın K6 — GrafikKulturTestleri tr-TR'de | ✅ | `GrafikKulturTestleri` + M-UX2A1-05 |
| Aşkın K7 — TemaEkran çağrı argümanını doğrular | ✅ | `TemaEkranTestleri` + M-UX2A1-04 |
| Aşkın K8 — ham hex ratchet | ✅ | `HamHexTavani = 0`, kâğıt muaf |
| Aşkın K9 — kilavuz.html kararı sessiz bırakılmaz | ✅ | §2.6 (mock figür istisnası açıkça yazıldı) |
| K-UX-10 / UX-S12 — Sozlukler TR+EN, auto-generated uyarısı düzeltilir | ✅ | Sozlukler.cs başlığı + `SozlukTema` + 3 `SozlukNav` anahtarı |
| Ölçülebilir Kabul 1 (kontrast AA) | ✅ | 24 çift × 3 tema, hepsi ≥ eşik |
| Ölçülebilir Kabul 2 (yatay kaydırma 0) | ❌ A1'de karşılanmadı | Kenar çubuğu 240 px sabit — **Faz A2** |
| Ölçülebilir Kabul 3 (27 sayfa aynı kabuk, hiçbiri "Kuyumcu") | ✅ başlık kısmı | `SayfaBasi` bileşeninin 27 sayfaya yerleşmesi **Faz B** |
| Ölçülebilir Kabul 4 (sıfır DOM kırığı, 957→971 test dokunulmaz) | ✅ | 971/971 değiştirilmeden yeşil |

---

## 10. Açık kalanlar

1. **Faz A2 — kenar çubuğu çekmecesi + hamburger.** 375 px'te yatay kaydırma bu iş
   bitmeden kapanmaz. Ölçülebilir Kabul 2 oraya ertelendi.
2. **375/768/1280 piksel ölçümü** araç yokluğundan yapılamadı; Playwright ya da kurucunun
   tarayıcısında `document.querySelector('.ana-icerik').scrollWidth > clientWidth` ile
   doğrulanmalı (K3: ölçüm `<main>` üzerinde, `.tablo-sar`/`.grafik-sar` beyaz listede).
3. **Karanlık temanın gözle onayı** (POS, Panel grafikleri, giriş ekranı, modaller) ve
   **yazdırma önizlemesi** yapılmadı — kurucunun 5199'daki tarayıcısında yenileme yeter.
4. **Grafiklerin belirteçlere bağlanması Faz B'de.** Şu an SVG renkleri hâlâ sabit hex
   (`#f59e0b`, `#b45309`, `#059669`, `#64748b`) ve karanlık temada da aynı kalıyor —
   okunabilir ama marka dilinden kopuk. Kültür kapısı (GrafikKulturTestleri) Faz B için
   şimdiden kuruldu.
5. **Satır içi `style="…"`** kullanımları (Panel, Islemler, Kasalar, Ayarlar, Kilavuz…)
   duruyor — Faz B'nin sayfa sayfa işi.
6. **`--ayrac` kontrast kapsamı dışında.** Kart/tablo ayraç çizgileri hâlâ açık gri
   (aydınlıkta slate-200). Bilinçli: WCAG 1.4.11 dekoratif sınırları kapsamaz. Etkileşimli
   kenarlar (`--kenarlik`) ≥3:1'e **çekildi** — girdi/seçim/ikincil düğme kenarları bugüne
   göre biraz daha koyu görünecek. Kurucu görsel olarak sevmezse tek satırlık belirteç
   değişikliğidir (ama test o zaman kırmızı olur; karar kayda geçmeli).
7. **`Yansima_TAM_27_rota_bulur`** sabit sayı taşır. Yeni sayfa eklendiğinde bilinçli
   olarak kırmızıya döner ve uygulayıcıyı haritaya satır eklemeye zorlar (UX-S13).

---

## 11. Kurucuya / mali müşavire notlar

- **Mevzuat hükmü koda sabit yazılmadı:** UX-2 hiçbir yasal eşik, oran veya süre içermez;
  tamamen istemci tarafı görsel katmandır. Bu pakette mali müşavire sorulacak bir hüküm **yok**.
- **Kâğıt çıktısı korundu:** gider pusulası (VUK md.234) ve ürün etiketi (md.11/(e) ibaresi)
  kurallarına **hiç dokunulmadı**; karanlık temanın kâğıda sızması ayrı bir testle
  engellendi. Yine de ilk gerçek yazdırmada gözle bakılması önerilir.
- **Kurucunun 5199'daki inceleme sunucusu** kaynak `wwwroot`'u sunuyor: tarayıcıda
  **yenileme** yapılınca yeni tema ve kılavuz görünür. Sunucuya dokunulmadı.
- **Kurucu kararı bekleyen tek görsel soru:** marka rengi. "Altın" bugün amber-700
  (`#b45309`) olarak korundu (AA gerekçesiyle koyulaştırılmış hâli). Gerçek bir marka rengi
  varsa belirteçler ona göre yeniden kurulur — tek dosya, bir kalemde.
