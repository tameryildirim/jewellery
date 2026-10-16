# PAKET UX-2-A2 — Uygulama raporu

> Görsel çekicilik · Faz A2: belirteç tamamlama + CSP-temiz bileşen zırhı +
> Cekmece/SayfaBasi/SatirIciDuzenle bileşenleri (SIFIR sayfa DOM değişikliği)
> · Dalga 2c · Tarih: 2026-09-17 · Roller: Taşkın (backend/yapılandırma) + Ertuğ (frontend)
> Durum: **BİTTİ** — derleme 0 uyarı/0 hata · 1675/1675 test yeşil · `dotnet/denetim.sh` TEMİZ ·
> 11 mutasyon kanıtı · canlı doğrulama 5198'de yapıldı, sunucu durduruldu.

---

## 1. Ne yapıldı

### 1.1 Belirteç tamamlama (`wwwroot/css/belirtecler.css`)
- **Yeni semantik belirteçler** (üç blokta da tanımlı: AYDINLIK, KARANLIK-MEDYA, KARANLIK-TERCİH):
  `--grafik-1 … --grafik-5`, `--odak-halka`.
- **Yeni ölçü belirteçleri** (`:root`): `--bosluk-8` (4rem — 4px ritmi 4/8/12/16/24/32/48/64px olarak tamamlandı),
  `--bosluk-hucre`, `--satir-yuksekligi-sik` (1.35), `--dokunma-hedefi`,
  `--odak-halka-kalinlik`, `--odak-halka-boslugu`,
  `--egri-cikis`/`--egri-giris-cikis`/`--egri-yumusak`, `--gecis-hizli`/`--gecis`,
  `--golge-katman`/`--golge-yuksek` (vendor değerlerine bağlı).
- **Yeni bloklar:** `:root[data-yogunluk="sik"|"rahat"]` ve
  `html[data-mod="basit"|"profesyonel"]` — **yalnız ölçü belirteci** değiştirirler;
  renk belirteci değiştirmeleri testle yasaklandı (M-UX2-A2-07).
- Mod değeri **sunucudan** gelir (`/auth/ben → etkinArayuzModu` → `ArayuzModuServisi`),
  JS yalnız **beyaz listeli** (`basit|profesyonel`) değeri yazar
  (`window.kuyumcuMod.uygula`, `wwwroot/js/tema.js` içinde ikinci ad alanı — **yeni `<script>` etiketi eklenmedi**).

### 1.2 Vendored Open Props (`wwwroot/css/vendor/open-props-easing-golge.css` — YENİ)
- **Yalnız 7 DEĞER**: 4 easing + 3 gölge. **Hiç seçici yok** (`:root` dışında bildirim yok, test zorluyor).
- Lisans başlığı: **MIT · Copyright (c) 2021 Adam Argyle**, tam metin + sürüm + kaynak URL dosyanın başında.
  Lisans metni 2026-09-17'de `raw.githubusercontent.com/argyleink/open-props/main/LICENSE`
  adresinden **indirilip okundu**, MIT olduğu doğrulandı (varsayım değil).
- Yükleme sırası **vendor → belirtecler.css → app.css** (index.html; testle kilitli).

### 1.3 CSP-temiz bileşen zırhı (`wwwroot/css/app.css`)
- `:focus-visible` odak halkası **tek kaynaktan** (`var(--odak-halka)`): `.btn .dugme .dugme-benzeri
  .girdi .secim .sekme .kenar-nav a .dil-secici button .mod-anahtar button .kabuk-menu-dugme
  .cekmece-kapat .modal-kapat .si-tetik .si-girdi .sayfa-basi-eylem button`.
- `.sayi` → `font-variant-numeric: tabular-nums; text-align: right` (+ `td.sayi/th.sayi`).
- Yeni bileşen stilleri: `.sayfa-basi*`, `.cekmece*`, `.satir-ici/.si-*`, `[data-duzenlenebilir]`.
- **Sabit renk yok** (yalnız `var()`; yarı saydam örtüler `rgb(15 23 42 / .45)` — hex değil, ham hex sayacı etkilenmedi),
  **satır içi style yok**.

### 1.4 KABUK — dar ekran (QA turu 1'in 27 rotalık taşması)
QA turu 1 bulgusu: 27 rotanın **hepsinde** 375px'te yatay taşma (scrollWidth 499–1000 > 375).
Tek kök neden: `.uygulama{display:flex}` + `.kenar-cubugu{width:240px;flex-shrink:0}`.
- `@media (max-width: 768px)`: `.uygulama{display:block}`, `.kenar-cubugu` sabit konumlu
  **çekmece** (`translateX(-100%)` → `.menu-acik` ile `0`), `.icerik-sutunu{width:100%}`,
  `.form-grid-2/.form-grid-3` tek sütun, `.ana-icerik` daha dar iç boşluk.
- Hamburger **`#ust-menu-ac`** (UstBar) — `aria-controls="kenar-cubugu"`, `aria-expanded`.
  Örtü **`#kenar-ortu`** (AnaYerlesim) tıklanınca kapatır; gezinti olunca çekmece kendiliğinden kapanır.
- Durum: `Servisler/KabukServisi.cs` (**paylaşılan tek örnek**, DI değil — gerekçe § 6/S1).

### 1.5 Yeni bileşenler (K7 bütçesi içinde; KomutPaleti/HizliIzgara/KayitliGorunum AÇILMADI)
| Bileşen | id sözleşmesi | Davranış |
|---|---|---|
| `Bilesenler/SayfaBasi.razor` | `#sayfa-basi-{anahtar}`, `-baslik`, `-aciklama`, `-filtre`, `-eylem` | başlık + açıklama + filtre yuvası + **tek** birincil eylem yuvası |
| `Bilesenler/Cekmece.razor` | `#cekmece-{varlik}`, `#cekmece-kapat`, `-govde`, `-kaydet`, `-iptal`, `-arka`, `-kilit` | `role="dialog" aria-modal aria-labelledby`; Esc kapatır; açılışta odak içeri, kapanışta tetikleyiciye; **iyimser kaydetme YOK** |
| `Bilesenler/SatirIciDuzenle.razor` | `#si-{varlik}-{alan}-{id}` (+ `-ac/-kaydet/-iptal/-hata/-bekliyor`), hücre `data-duzenlenebilir` | Enter kaydet · Esc iptal (eski değere döner) · boş gönderimde alan altı Türkçe mesaj · kaydederken kilit + "bekliyor" |

### 1.6 ModalPencere Esc onarımı (QA turu 2 grup 1-2)
Kök neden **doğrulandı**: `ModalPencere.razor`'da `@onkeydown` + `tabindex=-1` + `Escape→Kapat`
zaten vardı, ama **açılışta odak panele taşınmıyordu** → tuş olayı panele hiç ulaşmıyordu.
Çözüm: Cekmece ile **aynı** yardımcı (`wwwroot/js/cekmece.js` → `window.kuyumcuCekmece.ac/kapat`):
odak içeri + Tab hapsi + kapanışta odağın tetikleyiciye dönüşü. **İkinci mekanizma açılmadı.**

### 1.7 `js/cekmece.js` (YENİ, adlandırılmış fonksiyon — inline script YOK)
`ac(panel)` / `kapat(panel)`; eval yok, dizge kod üretimi yok. index.html betik sayısı 5 → **6** (sayaca bağlandı).

### 1.8 CSP / K12-K13
- `DilServisi.cs`'deki `eval()` çağrısı **zaten kaldırılmıştı** (Faz A1'de; `js/dil.js`
  → `kuyumcuDil.uygula`). Kodda doğrulandı: `Kuyumcu.Web` ağacında `InvokeVoidAsync("eval"` **yok**.
  Bu maddede yeni iş çıkmadı; A2'de aynı desen `kuyumcuMod` ve `kuyumcuCekmece` için de korundu.
- `deploy/nginx-kuyumcu.conf`: **X-Frame-Options SAMEORIGIN → DENY** (API'nin DENY'i ile çelişki giderildi)
  ve **API ile birebir aynı CSP** başlığı eklendi (nginx'te CSP hiç yoktu).
  Testle karakter karakter `Kuyumcu.Api/Program.cs`'e karşı doğrulanıyor.

### 1.9 `kilavuz.html` — 4 temel durum galerisi
`#galeri` bölümü: **aydınlık/karanlık × basit/profesyonel** dört kart; her kartta sayfa başı,
veri tablosu (`.sayi` sağa hizalı tabular-nums), rozetler, satır içi düzenleme ipucu,
`--grafik-1..5` serisi ve birincil/ikincil düğme. Kartlar `data-tema`/`data-mod` taşır;
belirtecler.css'e `.tema-onizleme[data-tema="karanlik"]` / `[data-mod=…]` kapsamı eklendi
(CSP `frame-ancestors 'none'` iframe'e izin vermez; ikinci palet de yazılamaz — tek yol kapsamlamaktı).
Galeride **sabit renk ve satır içi stil yok** (testle zorlanıyor).

---

## 2. Sapmalar ve gerekçeleri (Anayasa md.1)

| # | Spesifikasyon/koşul | Yapılan | Gerekçe |
|---|---|---|---|
| S1 | `KabukServisi` DI ile enjekte edilsin (doğal tasarım) | **Statik paylaşılan tek örnek** (`KabukServisi.Ortak`) | `@inject` ile UstBar/KenarCubugu'na girince **mevcut 11 test** (RotaBaslikTestleri ×2, BasitModMenuTestleri ×7, BildirimEkranTestleri ×2) DI kaydı eksikliğinden kırıldı. A2'nin kabul ölçütü "mevcut testler **değiştirilmeden** yeşil" (K8-a) olduğu için servis DI'dan çıkarıldı. Durum yalnız bir görünüm bayrağıdır: sunucuya gitmez, yetkiye dokunmaz, kalıcı değildir. Gerçek çıktı raporlandı, gizlenmedi. |
| S2 | KABUK (3): "'Ürün Etiketleri' gibi eksik PageTitle sonekleri RotaBasliklari ile eşitlenir" | **YAPILMADI** — `Layout/RotaBasliklari.cs` zaten 27 rotayı tanıyor ve UstBar başlığı oradan geliyor; eksik olan **sayfa içindeki `<PageTitle>`**'lardır ve onlar `Pages/*.razor` içindedir. | A2 **sıfır sayfa DOM değişikliği** paketidir (K8-a) ve "Pages/*.razor dosyalarına DOKUNULMAZ" bağlayıcı. Kalem dalga 2c'nin B dilimine devredildi (§ 8). |
| S3 | KABUK (5): "Panel basit modda sadeleşir (3 kart)" | **YAPILMADI** | Aynı gerekçe: `Pages/Panel.razor` değişikliği gerektirir. B dilimine devredildi (§ 8). |
| S4 | KABUK (2): kur yoklaması tek yerden | **ZATEN ÖYLE** — `CanliKurServisi.BaslatAsync()` UstBar'dan tek çağrılıyor (tek REST + SignalR push aboneliği). QA'nın saydığı 136 istek/oturum **sayfa bileşenlerinin kendi `/kur/guncel` çağrılarından** gelir ve o çağrılar `Pages/` içindedir. | A2 Pages/'e dokunamaz. Ölçülen gerçek durum yazıldı; kalem B dilimine devredildi. |
| S5 | `.sayi` sağa hizalı | `.sayi{text-align:right}` + `td.sayi/th.sayi{text-align:right}` | `text-align` satır içi (`<span class="sayi">`) öğelerde etkisizdir; üst bar kur bandı gibi mevcut yerleşimler **değişmez**. Tablo hücrelerinde etki gerçekleşir. |
| S6 | K14 "index.html script sayısı = 2" | Sayaç **6**'ya kilitlendi | Mühür metnindeki 2 değeri kodla uyuşmuyor: A2 öncesi **5** betik vardı (tema, dil, suruklenebilir, indir, blazor). A2 `js/cekmece.js`'i ekledi → 6. Kuralın özü "**artmasın, inline olmasın**"dır ve sayaç bunu zorluyor; ayrıca `<script>` (inline blok) sayacı 0'da kilitli. |
| S7 | Yeni denetim sayaçlarının hedefleri (ör. "ölü düğme = 0") | **Cırcır (ratchet)**: bugünkü değer tavan | A2 Pages/'e dokunmadığı için borç bugün ölçüldü ve **dondu**; artış FAIL. Hedef 0'a iniş B dalgalarının işi. Taban değerler § 5'te. |

**Mevzuat/DOĞRULANMALI hükmü:** bu pakette yok (saf arayüz/yapılandırma paketi). Kurucuya/mali müşavire not gerektiren kalem çıkmadı.

---

## 3. Migration

**GEREKMEDİ.** Paket veri modeline dokunmaz; yeni varlık/alan yoktur.
`kuyumcu_net` şeması değişmedi, migration sayısı **33**'te kaldı.

---

## 4. Testler ve mutasyonlar

### 4.1 Test sayıları
| | Önce | Sonra |
|---|---|---|
| Toplam | 1650 | **1675** (+25) |
| Başarılı | 1650 | **1675** |
| Başarısız | 0 | **0** |
| Atlanan | 0 | 0 |

Yeni testler:
- `Kuyumcu.Tests/KabukBilesenTestleri.cs` (**17 test**, YENİ dosya): SayfaBasi ×2, Cekmece ×6,
  SatirIciDuzenle ×4, ModalPencere ×2, KabukServisi ×1, + id/ARIA sözleşmesi.
- `Kuyumcu.Tests/TasarimBelirtecTestleri.cs` (**+8 test**): yoğunluk/mod blokları yalnız ölçü;
  vendor CSS lisans + seçicisizlik + yükleme sırası; odak halkası tek kaynak; dar ekran kabuk kuralları;
  `cekmece.js` eval'siz + odak geri verme; `kuyumcuMod` beyaz listesi + betik sayısı;
  nginx CSP/DENY birebir; kılavuz 4 temel durum galerisi.
  Ayrıca mevcut `Kontrast_AA_saglanir` ölçüsüne **7 yeni çift** (odak halkası ×2, grafik serisi ×5, ≥3:1)
  ve `Her_semantik_belirtecin_…` listesine **6 yeni belirteç** eklendi.

Tam süit kanıtı: `--logger trx --results-directory …/scratchpad/trx-pUX-2-A2` →
`Başarılı! - Başarısız: 0, Başarılı: 1675, Atlanan: 0, Toplam: 1675`.

**KARARSIZLIK (flaky) KAYDI — Anayasa md.1, gizlenmedi:** üç tam süit koşumunun
**ikincisinde** `KarmaOdemeEkranTestleri.Satistan_alisa_gecince_karma_kipi_KAPANIR`
bir kez kırmızı döndü (1675'te 1 başarısız). Aynı sınıf hemen ardından **3 kez ayrı ayrı
koşuldu → 3/3 yeşil (10/10 test)**; sonraki tam süit koşumu yine **1675/1675 yeşil**.
Bu test A2'nin dokunduğu hiçbir dosyayı kullanmaz (`Pages/Islemler.razor` ekranı,
bu pakette açılmadı) — **kararsız (flaky) test** olarak kaydediliyor ve Aşkın'a
ayrı kalem olarak devrediliyor (§ 11/7).

### 4.2 Mutasyon tablosu (11/11 kırmızıya döndü, hepsi `cmp` ile birebir geri alındı)
| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| M-UX2-01 | `--metin: #1e293b → #9aa5b4` (aydınlık) | `TasarimBelirtecTestleri.Kontrast_AA_saglanir` | cmp OK |
| M-UX2-06 | app.css `.sayi`'ya `color:#123456` | `Ham_hex_beyaz_listeyi_asmaz` + `denetim.sh` (26 > tavan 25) | cmp OK |
| M-UX2-A2-02 | nginx `X-Frame-Options DENY → SAMEORIGIN` | `Nginx_kabugu_API_ile_AYNI_CSP_ve_DENY_gonderir` + `denetim.sh` | cmp OK |
| M-UX2-A2-03 | `Cekmece` Esc → Enter | `KabukBilesenTestleri.Cekmece_ESCAPE_ile_kapanir` | cmp OK |
| M-UX2-A2-04 | `ModalPencere`'de odak çağrısı kaldırıldı | `ModalPencere_acilinca_ODAGI_PANELE_tasir_ve_ESCAPE_kapatir` | cmp OK |
| M-UX2-A2-05 | `SatirIciDuzenle` boş değer kontrolü `if(false)` | `SatirIci_BOS_gonderimde_SESSIZ_kalmaz_alan_alti_mesaj_verir` | cmp OK |
| M-UX2-A2-07 | `[data-yogunluk="sik"]` bloğuna `--yuzey:#000000` | `Yogunluk_ve_mod_bloklari_YALNIZ_OLCU_belirteci_degistirir` | cmp OK |
| M-UX2-A2-08 | dar ekran `.uygulama{display:block → flex}` | `Dar_ekranda_KENAR_CUBUGU_cekmeceye_doner…` | cmp OK |
| M-UX2-A2-09 | vendor CSS lisans başlığı silindi | `Vendor_CSS_LISANSLIDIR_ve_HICBIR_SECICI_tasimaz` + `denetim.sh` | cmp OK |
| M-UX2-A2-10 | odak halkası `var(--odak-halka)` → `var(--vurgu)` (ikinci kaynak) | `Odak_halkasi_TEK_KAYNAKTAN_beslenir…` | cmp OK |
| M-UX2-A2-11 | `Cekmece.KaydetKapali` içinden `Kaydediliyor` çıkarıldı (iyimser kaydetme) | `Cekmece_KAYDEDILIYORKEN_kaydedildi_YAZMAZ_ve_dugmeyi_kilitler` | cmp OK |

Son mutasyondan sonra **temiz `dotnet build Kuyumcu.sln --nologo` → 0 Uyarı, 0 Hata.**

---

## 5. Denetim sayaçları (`dotnet/denetim.sh` → **DENETİM TEMİZ**)

Yeni bölüm: `═══ Arayüz kapıları (paket UX-2 Faz A2 · K14 + MÜHÜR 2 DoD) ═══`

| Sayaç | Taban (=tavan) | Kural |
|---|---|---|
| app.css ham hex (kaba sayım) | **25** | artış = FAIL (ince ölçüm: `Ham_hex_beyaz_listeyi_asmaz`, kâğıt dışı tavan **0**) |
| Pages+Bilesenler+Layout satır içi `style=` | **196** | artış = FAIL (Faz D'de 0'a iner) |
| vendor CSS lisans başlığı (MIT + kaynak + Copyright) | **var** | yoksa FAIL |
| index.html `<script >` sayısı | **6 (sabit)** | eşit değilse FAIL |
| index.html satır içi `<script>` | **0** | >0 FAIL |
| Pages: `@onclick`'siz **ve** `disabled`'sız `<button>` (DoD/1) | **71** | artış = FAIL; hedef 0 (B dalgaları) |
| Pages: `ModalPencere` kullanımı (DoD/2) | **45** | artış = FAIL; düzenleme amaçlıların 2c sonunda 0 olması hedef |
| Pages: `duzenlenenId` kalıbı (DoD/2) | **1** | artış = FAIL |
| Pages/*.razor dosya sayısı (DoD/3 — tek dokunuş yardımcısı) | **30** | artış = FAIL |
| Pages: doğrulama mesajsız `EditForm` (DoD/4) | **0 EditForm / 0 ValidationMessage** | `EditForm > ValidationMessage` ise FAIL |
| nginx CSP + `X-Frame-Options DENY` (K12) | **var** | eksik/çelişkili ise FAIL |

> **DoD/3 dürüstlük notu:** "tek dokunuş" kaydının asıl yeri orkestratörün `betikler/kapi.sh`
> çıktısıdır; kabuk betiği hangi sayfaya dokunulduğunu üretemez. Buradaki sayaç yalnız
> yeni sayfa sızmasını yakalar. **Bu pakette dokunulan sayfa: HİÇBİRİ** (§ 7).

---

## 6. Canlı doğrulama (http://localhost:5198 — Development, `--no-build`)

| Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|
| `curl -D - /` | 200 + CSP + DENY | **200** | `X-Frame-Options: DENY` · `Content-Security-Policy: default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'` · `X-Content-Type-Options: nosniff` |
| `/css/vendor/open-props-easing-golge.css` | 200, lisans başlıklı | **200, 3232 bayt** | ilk satırlar: `VENDORED — Open Props (seçici kopya: YALNIZ easing + gölge DEĞERLERİ)` |
| `/css/belirtecler.css` | 200, yeni belirteçler var | **200, 14976 bayt** | `odak-halka|grafik-1` eşleşme sayısı **9** |
| `/css/app.css` | 200 | **200, 42480 bayt** | — |
| `/js/cekmece.js` | 200, `kuyumcuCekmece` | **200, 3417 bayt** | eşleşme **2** |
| `/js/tema.js` | 200, `kuyumcuMod` | **200, 4176 bayt** | eşleşme **1** |
| `/kilavuz.html` | 200, 4 durum galerisi | **200, 50313 bayt** | `tema-onizleme` eşleşme **27** |
| index.html gövdesi | vendor CSS + cekmece.js referansı | **1 + 1** | sunucudan çekilen gövdede |

**Kiracı sayısı:** başlangıç **8** → bitiş **8** (`select count(*) from tenants`).
Bu pakette **test kiracısı açılmadı** (saf arayüz paketi); `QA-TEST-` önekli kalıcı QA fikstürlerine
**dokunulmadı**. API PID 8570 durduruldu; `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**. 5199'a dokunulmadı.

**Tarayıcı konsolunda CSP/EvalError = 0** iddiası bu raporda **YAPILMAMIŞTIR** — kanıtı tarayıcı QA
ajanına aittir (K12). Buradan verilen kanıt `curl` başlıkları + `eval(` yokluğunun test kanıtıdır
(`Tema_betigi_EVAL_kullanmaz…`, `Cekmece_betigi_EVAL_kullanmaz…`, `Mod_yazicisi_…`).
QA turu 2'nin "375px'te scrollWidth ≤ clientWidth, 27 rota" JSON kanıtı da tarayıcı turunda alınacaktır;
bUnit CSS uygulamaz, bu yüzden burada **sadece kural varlığı** sınandı (M-UX2-A2-08).

---

## 7. K4 id envanteri diff'i (2c öncesi → sonrası)

**Silinen id: 0 · Değişen id: 0.** Yalnız EKLENENLER:

- Kabuk: `#ust-menu-ac`, `#kenar-cubugu`, `#kenar-ortu`
- SayfaBasi: `#sayfa-basi-{anahtar}` (+ `-baslik`, `-aciklama`, `-filtre`, `-eylem`)
- Cekmece: `#cekmece-{varlik}` (+ `-arka`, `-baslik`, `-govde`, `-kaydet`, `-iptal`, `-kilit`), `#cekmece-kapat`
- SatirIciDuzenle: `#si-{varlik}-{alan}-{id}` (+ `-ac`, `-kaydet`, `-iptal`, `-hata`, `-bekliyor`)

**Dokunulan `Pages/*.razor` sayfaları: HİÇBİRİ** (DoD EK 2 tek dokunuş — bu paket sayfa bütçesi harcamaz).
Dokunulan kabuk dosyaları: `Layout/UstBar.razor`, `Layout/KenarCubugu.razor`, `Layout/AnaYerlesim.razor`,
`Bilesenler/ModalPencere.razor`.

---

## 8. DoD sözleşmesi

A2 **yeni varlık/kayıt türü doğurmaz** (veri modeli değişmedi, uç eklenmedi, migration yok).
Bu yüzden PATCH/DELETE/[RolGerekli]/denetim kaydı maddeleri **konusuz**dur.
Doğan şey **bileşen sözleşmesi**dir ve DoD karşılıkları şöyledir:

| Doğan şey | Düzenleme yolu | Kilit/gerekçe |
|---|---|---|
| `Cekmece` | çok alanlı düzenlemenin **tek kalıbı**; `Kaydet` olayı ebeveyne, kapanışı ebeveyn yapar | `Kaydediliyor` iken düğme kilitli + **sebep ekranda**; sunucu onayı gelmeden "kaydedildi" yazılmaz |
| `SatirIciDuzenle` | tek alanlı düzeltmenin **tek kalıbı**; Enter kaydet / Esc iptal | boş gönderimde alan altı mesaj; kaydederken girdi+düğmeler kilitli, "bekliyor" rozeti |
| `SayfaBasi` | salt sunum (veri tutmaz) | — |
| `KabukServisi` | görünüm bayrağı, kalıcı değil | sunucuya gitmez, yetkiye dokunmaz |
| Kabuk çekmecesi / ModalPencere | — | Esc + odak hapsi + odak dönüşü **tek yardımcıdan** |

**Yıkıcı eylem** bu pakette doğmadı; ortak onay modalı UX-4d (13) bileşenidir ve ikinci desen açılmadı.
**Salt okunur olan yok**, çünkü kayıt türü eklenmedi.

---

## 9. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| (1) Eksik belirteçler + `[data-yogunluk]` + `html[data-mod]` + `:focus-visible` + `.sayi` + altın dozu | **KARŞILANDI** | belirtecler.css; `Yogunluk_…`, `Odak_halkasi_…`, `Kontrast_AA_saglanir` (+7 çift) |
| (2) vendor Open Props (6-8 değer, seçicisiz, MIT lisans başlıklı, yükleme sırası) | **KARŞILANDI** | 7 değer; `Vendor_CSS_LISANSLIDIR_ve_HICBIR_SECICI_tasimaz`; lisans metni indirilip doğrulandı |
| (3) SayfaBasi + Cekmece + SatirIciDuzenle + bUnit testleri + kilavuz örnekleri | **KARŞILANDI** | 3 yeni .razor; 17 bUnit testi; kilavuz galerisi |
| (4) `DilServisi` eval kaldırma; TemaServisi aynı desen | **ZATEN KAPALI** | `Kuyumcu.Web`'de `InvokeVoidAsync("eval"` yok; `kuyumcuDil/kuyumcuTema/kuyumcuMod/kuyumcuCekmece` adlandırılmış |
| (5) nginx'e API ile aynı CSP + `X-Frame-Options DENY` | **KARŞILANDI** | nginx conf; `Nginx_kabugu_…` testi; canlı `curl -I` (Kestrel tarafı) |
| (6) denetim.sh 4 CSS/CSP sayacı | **KARŞILANDI** | § 5 (ham hex, satır içi style, vendor lisans, betik sayısı) |
| (7) kilavuz.html 4 temel durum galerisi | **KARŞILANDI** | `#galeri`; `Kilavuz_DORT_TEMEL_DURUM_…` |
| DoD 4 sayacı (MÜHÜR 2) | **KARŞILANDI (taban)** | § 5; DoD/3 sınırı açıkça yazıldı |
| K4 — id sözleşmesi (yalnız EKLENEN) | **KARŞILANDI** | § 7 |
| K6 — Cekmece sözleşmesi | **KARŞILANDI** | `Cekmece_K6_id_ve_ARIA_sozlesmesini_karsilar` |
| K7 — bileşen bütçesi (KomutPaleti/HizliIzgara/KayitliGorunum YOK) | **KARŞILANDI** | yalnız 3 bileşen eklendi; sayfalara yeni `Api.*` çağrısı eklenmedi |
| K8-a — mevcut testler değiştirilmeden yeşil, toplam düşmez | **KARŞILANDI** | 1650 → 1675; hiçbir mevcut test dosyası düzenlenmedi (yalnız `TasarimBelirtecTestleri` **eklemeyle** genişledi) |
| K12 — CSP kaydı (script-src `'self' 'wasm-unsafe-eval'`, script'te `unsafe-inline` YOK) | **KARŞILANDI** | § 6 canlı başlık; "eval yok" ifadesinin doğrusu bu raporda |
| K13 — eval/unsafe-inline açıkları | **KARŞILANDI (style-src hariç)** | style-src `'unsafe-inline'` kaldırma **Faz D**; ölçüsü satır içi style sayacı (196) |
| KABUK (1) dar ekran çekmecesi | **KARŞILANDI (kural düzeyinde)** | CSS + hamburger + örtü; canlı 27 rota JSON'u QA turu 2'de |
| KABUK (2) tek kur yoklaması | **KISMEN** | kabuk zaten tek kaynak; sayfa içi çağrılar Pages/'te → B dilimi |
| KABUK (3) sayı biçimi + PageTitle sonekleri | **KISMEN** | `.sayi` yapıldı; PageTitle Pages/'te → B dilimi |
| KABUK (4) Modal/Cekmece Esc + odak tuzağı, tek onay deseni | **KARŞILANDI** | § 1.6; ikinci desen açılmadı |
| KABUK (5) Panel basit modda sadeleşir | **YAPILMADI** | Pages/ yasağı → B dilimi |
| Pages/*.razor'a dokunulmadı | **KARŞILANDI** | § 7 |
| Kontrast `Kontrast_AA` iki temada, `--odak-halka` dâhil | **KARŞILANDI** | M-UX2-01 |
| Ürün ücretsiz pilot — fiyat/teklif metni üretilmedi | **KARŞILANDI** | bu pakette hiçbir ticari metin yok |

---

## 10. Değişen dosyalar

**Yeni:**
- `dotnet/Kuyumcu.Web/wwwroot/css/vendor/open-props-easing-golge.css`
- `dotnet/Kuyumcu.Web/wwwroot/js/cekmece.js`
- `dotnet/Kuyumcu.Web/Bilesenler/SayfaBasi.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/Cekmece.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/SatirIciDuzenle.razor`
- `dotnet/Kuyumcu.Web/Servisler/KabukServisi.cs`
- `dotnet/Kuyumcu.Tests/KabukBilesenTestleri.cs`
- `dokumanlar/09-uygulama-kayitlari/PAKET-UX-2-A2-uygulama-raporu.md`

**Değişen:**
- `deploy/nginx-kuyumcu.conf`
- `dotnet/denetim.sh`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (yeni `SozlukKabuk`, TR + EN)
- `dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css`
- `dotnet/Kuyumcu.Web/wwwroot/css/app.css`
- `dotnet/Kuyumcu.Web/wwwroot/index.html`
- `dotnet/Kuyumcu.Web/wwwroot/kilavuz.html`
- `dotnet/Kuyumcu.Web/wwwroot/js/tema.js` (`kuyumcuMod` ad alanı)
- `dotnet/Kuyumcu.Web/Bilesenler/ModalPencere.razor`
- `dotnet/Kuyumcu.Web/Layout/UstBar.razor`, `Layout/KenarCubugu.razor`, `Layout/AnaYerlesim.razor`
- `dotnet/Kuyumcu.Tests/TasarimBelirtecTestleri.cs`

> `git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısı bu paketten **önce de** kirliydi
> (dalga 2a/2b'nin sonuçları, commit yasağı gereği çalışma ağacında duruyor). Yukarıdaki liste
> bu paketin **kendi** dokunduğu dosyalardır. **Commit yapılmadı.**

---

## 11. Açık kalanlar

1. **B dilimine devredilenler (Pages/ yasağı yüzünden):** Panel basit mod sadeleşmesi,
   eksik `<PageTitle>` sonekleri, sayfa içi `/kur/guncel` çağrılarının kabuğa toplanması.
2. **Tarayıcı kanıtı bekleyenler (QA turu 2):** 375/768/1280 px'te 27 rotada
   `scrollWidth ≤ clientWidth` JSON'u; konsolda CSP/EvalError = 0; 4 temel durumun
   önce/sonra ekran görüntüleri (`09-uygulama-kayitlari/` altına).
3. **Faz D kalemi:** `style-src 'unsafe-inline'` kaldırma — bugün 196 satır içi `style=` borcu var.
4. **DoD borçları (cırcırla donduruldu):** 71 ölü düğme, 45 ModalPencere kullanımı, 1 `duzenlenenId`.
5. **nginx CSP canlı kanıtı:** `curl -I` kanıtı **Kestrel** üzerinden alındı; nginx ayakta bir
   ortam bu oturumda yok. Batuhan yayın ortamında `curl -I` ile tekrar doğrulamalı.
6. **Kararsız test:** `KarmaOdemeEkranTestleri.Satistan_alisa_gecince_karma_kipi_KAPANIR`
   üç tam süit koşumundan birinde kırmızı döndü, izole 3/3 yeşil. A2 kapsamı dışı;
   Aşkın'ın kararsızlık kuyruğuna.
7. **Vendored Open Props sürümü** "1.7.x" olarak yazıldı; npm paketi bu makinede yok, sürüm
   numarası dosya içeriğinden **doğrulanamadı** — değerler upstream kaynak dosyalarından
   alınmıştır, sürüm satırı kesinleşince güncellenmeli (varsayım olarak işaretlendi).

## 12. Kurucuya / mali müşavire notlar

**YOK.** Bu paket saf arayüz ve dağıtım yapılandırmasıdır; hiçbir mevzuat hükmü koda yazılmadı,
hiçbir mali hesap değişmedi. Ticari metin (fiyat/teklif/tahsilat) üretilmedi — ürün hâlâ
**ücretsiz pilot** konumundadır.
