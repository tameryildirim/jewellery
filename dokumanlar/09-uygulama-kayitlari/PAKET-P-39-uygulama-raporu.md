# PAKET P-39 — TASARIM DENETİMİ KÖK NEDENLERİ (8 kök · 43 ekran)

**Dalga:** 6 · **Sürüm:** 1.0 · **Tarih:** 2026-09-21 · **Önkoşul:** P-37c
**Kaynak ölçüm:** `dokumanlar/09-uygulama-kayitlari/TASARIM-DENETIMI-2026-09-21.md` (+ ham veri `scratchpad/tasarim-denetimi.json`)
**Kurucunun tetiklediği madde:** *"adet kısmında ne yazarsam altta da o görünüyor"* → K7.

> **Anayasa md. 1 uyarısı — bu rapor iki oturum ayağının toplamıdır.** Paketin
> ilk ayağı (21 Eylül 22:04-22:26) kesintiye uğradı; kod, testler ve
> `denetim.sh` sayaçları o ayakta yazılmıştı, **rapor ve canlı ölçüm eksikti**.
> İkinci ayak (22:27-23:4x) eksikleri tamamladı, **canlı ölçümü baştan koştu**,
> ölçümün gösterdiği **üç artık kusuru** ve **ölçümün ortaya çıkardığı iki
> gerilemeyi** kapattı.
>
> **Üçüncü ayak (23:45-…) = hasım doğrulayıcının 5 bulgusunun kapanışı.** En ağırı
> KRİTİK'ti: rapor "hiçbir `Pages/*.razor` açılmadı" diyordu, **oysa 6 sayfa
> değişmişti**. Doğrulayıcı haklıdır; beyan § 1.1'de düzeltildi, § 7 ve § 8
> yeniden yazıldı, kapı kaydı eklendi. Bulguların tamamının kapanış tablosu
> **§ 11**'dedir; o bölümdeki derleme/denetim/süit sayıları bu ayakta taze
> koşulmuştur, § 6'daki piksel sayıları ikinci ayağa aittir (§ 11.2 açıklar).

---

## 1. Yapılanlar — kök neden başına

| Kök | Ne yapıldı | Nerede (TEK yer) |
|---|---|---|
| **K1** Üst bar 1024'te belgeyi yatay kaydırıyor (43 rota, 91-160 px) | Sarma kuralı `@media (max-width:768px)` hapsinden çıkarıldı; `.ust-bar`, `.ust-sag`, `.mod-anahtar` her genişlikte sarar, `min-width:0`. Taşma `overflow-x:hidden` ile **gizlenmedi**. | `wwwroot/css/app.css` § K1 |
| **K2** Satır etikete göre hizalanıyor (28 kesin kusur / 8 kalıp / 6 ekran) | Alan hücresi **üç raylı ızgara** [etiket·kontrol·alt]; satır kapsayıcıları `subgrid` ile ortak raya oturur. Bu ayakta üç boşluk daha kapandı: (a) **ikinci alan ailesi** `.secici-alan` da aynı üç raya alındı, (b) `.arama-kutu` kontrol rayına, (c) alt ızgaranın **kendi satır boşluğu** rayları kaydırıyordu → `row-gap: inherit`. | `app.css` § K2 · `Bilesenler/Form/AlanKabugu.razor` |
| **K3** 375'te 443/455 (%97,4) dokunma hedefi 44 px altı | `≤480px` bloğunda düğme ailesinin tamamına (çıplak `button`, `summary`, `[role=button]`, `.btn`, `.pill`, `.dil-secici button` dâhil) asgari kutu; belirteç telefonda 2,75rem. Metin içi bağlantı kapsam dışı (gerekçe CSS'te yazılı). | `app.css` § K3 · `belirtecler.css` |
| **K4** 15 farklı düğme yüksekliği / 5 kart yarıçapı | Düğme ölçeği **iki belirtece** (`--dugme-yuksekligi`, `--dugme-yuksekligi-kucuk`), kart yarıçapı tek belirtece indirildi; giriş ekranının paralel `.btn/.pill` ailesi de kapsama alındı. | `belirtecler.css` § K4 · `app.css` |
| **K5** Tablo taşması (14+ ekran), `/hurda` @375 ana içerik 58 px | Ölü `tablo-sarmal` sınıfı yerine tek sarmalayıcı `.tablo-sar`; `.ana-icerik { min-width:0 }`. Bu ayakta ek: **dosya seçici girdi** kapsayıcısını aşıyordu (`/veri-ice-aktarim` @375, 72 px) → `input[type="file"].girdi { max-width:100% }`. | `app.css` § K5 |
| **K6** Panel görüş alanının ALTINDA açılıyor (5 vaka) | `js/secici-yerlesim.js`: yer yoksa `.yukari` sınıfı → panel tetiğin üstünde açılır; panel görüş alanından uzun olamaz (`max-height: min(24rem,70vh)`, liste kendi içinde kayar). | `wwwroot/js/secici-yerlesim.js` · `app.css` § K6 |
| **K7** Önizleme girdinin kendisini tekrar ediyor (8 alan / 5 ekran) — **kurucunun bulduğu madde** | Ölçüt **ortak taban sınıfa** kondu: önizleme yalnız **bilgi ekliyorsa** çizilir (`BicimBilgiEkliyor`), `aria-describedby` zinciri de buna bakar. Birimsiz `Sayi`'da ve `Para`'da çizilmez; `Gram`'da yalnız **has karşılığı** kalır. | `Bilesenler/Form/SayisalAlan.cs` · `Sayi.razor` · `Para.razor` · `Gram.razor` |
| **K8** Metin kutusu 34,5 px ↔ seçici 32 px (31 kayıt / 10 grup) | `.girdi:not(.girdi-cok-satir), .secim, .secici-tetik, .secici-ara` tek belirteçten yükseklik alır, `padding-block:0`, geri düşüş değeri yazılı. | `app.css` § K8 |

### 1.1 Dokunulan sayfalar (DoD EK 2 — tek dokunuş) — **DÜZELTME (Anayasa md. 1)**

> ⚠ **Bu raporun önceki sürümü "hiçbir `Pages/*.razor` dosyası açılmadı" diyordu. BU BEYAN YANLIŞTI.**
> Hasım doğrulayıcı 6 sayfanın P-39 ayağı içinde (21 Eylül 22:10-22:18) değiştiğini kanıtladı ve haklıdır.
> Beyan burada düzeltilmiştir; kod değişikliğinin kendisi teknik olarak doğrudur, gizlenen bir başarısızlık
> yoktur — **gerçeğe uymayan bir özet vardı ve kaldırılmıştır.**
>
> **Kök sebep:** K5'in yeni `denetim.sh` sayacı `TABLO_SARMALAYICISIZ` fail-closed'dur ve `Pages/` altındaki
> HER `<table>`'ın `class="tablo-sar"` ile sarılmasını zorunlu kılar. Sayaç kurulduğu anda sarmalayıcısı
> olmayan sayfaların açılması **kaçınılmaz** hâle geldi; uygulayıcı bunu yaptı ama § 1 özetini güncellemedi.

**Kanıt:** değişiklik öncesi yedekler `scratchpad/mut-yedek/P-39/` altında duruyor; aşağıdaki tablo
`diff -w <yedek> <çalışma ağacı>` çıktısının **tamamıdır** (boşluk/girinti farkı hariç başka değişiklik yoktur).

| # | Sayfa | P-39'un yaptığı değişikliğin TAMAMI (`diff -w`) | İkinci dokunuş gerekçesi (DoD EK 2) |
|---|---|---|---|
| 1 | `Pages/Pos.razor` | +1 `<div class="tablo-sar">` sarmalayıcı (2 satır) | K5 · `TABLO_SARMALAYICISIZ=0` fail-closed sayacı; mekanik sarmalayıcı, mantık değişmedi |
| 2 | `Pages/Kurulum.razor` | +2 `<div class="tablo-sar">` sarmalayıcı (4 satır) | aynı — K5 sayacı |
| 3 | `Pages/Vezne.razor` | +2 `<div class="tablo-sar">` sarmalayıcı (4 satır) | aynı — K5 sayacı |
| 4 | `Pages/Raporlar.razor` | +1 `<div class="tablo-sar">` sarmalayıcı (2 satır) | aynı — K5 sayacı |
| 5 | `Pages/Hurda.razor` | `class="tablo-sarmal"` → `class="tablo-sar"` (1 satır, ölü sınıf adı) | K5 · ölü sarmalayıcı sınıfının tekilleştirilmesi (M39-5 mutasyonuyla bağlı) |
| 6 | `Pages/Islemler.razor` | **3 adet** `style="align-items:flex-end"` satır içi stil SİLİNDİ | K2 · "satır içi style yok" değişmezi + `FORM_SATIR_HIZA_SAPMASI=0` sayacı; hizalama artık `AlanKabugu` üç raylı ızgarasından gelir, satır içi stil ölü kalmıştı |

**Kapı kaydı:** 6 sayfanın altısı da bu dalgada başka paketlerce de açılmıştır (aynı oturum ayağı), bu
nedenle her biri **ikinci dokunuş** sayılır. Ortak gerekçe tek cümleyle: *"P-39'un fail-closed K5/K2
sayaçları `Pages/` altındaki işaretçileri zorunlu kılar; dokunuş ekran başına CSS/mantık değil, ortak
sınıf adının uygulanmasıdır."* Hiçbirinde iş kuralı, id, uç çağrısı ya da düzenleme kalıbı değişmedi
(`diff -w` çıktısı yukarıda birebir listelendi). Ekran başına CSS **yazılmadı**.

Paketin geri kalanı kabuk/ortak bileşen düzeyindedir: 3 CSS/JS dosyası + 4 form bileşeni + 1 kabuk.

---

## 2. Sapmalar ve gerekçeleri (Anayasa md. 1)

1. **"Para'da önizleme ÇIKAR" koşulundan sapıldı.** Görev metni birimsiz `Sayi`'da çıkmamasını, `Gram` ve `Para`'da çıkmasını istiyordu. Ölçüt `ONIZLEME_MUKERRER = 0` ile çelişiyor: `750000` yazan kutunun altına `₺750.000,00` yazmak **aynı rakam dizisidir** ve denetçinin `girdi-alti-tekrar` ölçütüne göre mükerrerdir (denetim § 5.1 bu kaydı `/cariler` Kredi Limiti'nde kusur saymıştı). **Karar:** ölçüt sayısaldır — biçimli metnin rakamları girdininkini içeriyorsa önizleme çizilmez. Para birimi bilgisi kaybolmaz, kutunun sağındaki `-birim` rozetinde durur (UX-S32 korunur, testle bağlandı). `Gram`'da önizleme **has karşılığı varsa** kalır (girdide olmayan bilgi).
2. **`denetim.sh` sayaçları tarayıcı ölçümü değil, MEKANİZMA ölçümüdür.** Görev "§ 10 ölçüm betiği `denetim.sh`a taşınır" diyor; `denetim.sh` bir kabuk betiğidir, tarayıcı açamaz. 8 sayaç **kusuru doğuran mekanizmanın kaynakta durduğunu** fail-closed doğrular (ör. `UST_BAR_TASMA_1024` = sarma kuralının medya sorgusu dışında olması). Piksel tarafı bu raporun § 6 canlı ölçümüdür ve betikleri sürümlenmiştir. **Bu ayrım § 6'daki sayı farklarının da sebebidir** (aşağıda açıkça yazıldı).
3. **Ölçüm kapsamı denetçininkinden dar.** Denetçi 43 rota + 25 açılan form = 68 anahtar ölçtü. Bu ayak **41 statik rota** (`@page` direktiflerinden üretildi; parametreli 5 rota ve `/giris` hariç) × 4 genişlik = **164 satır** + **13 açılan form/çekmece durumu** × 4 genişlik = **52 satır** ölçtü. `/vade-takvimi` diye bir rota **yoktur** (denetimdeki ad `/evraklar/takvim`; bu rota bu ayakta 4 genişlikte de **doldu**, `bos=false`).
4. **Ölçüm başsız Chrome + CDP ile koşuldu** (uygulama içi tarayıcı bölmesi yerine). Sebep: oturum jetonunu tarayıcıya taşımak gerekiyordu; uygulamanın CSP'si (`connect-src 'self'`, `script-src 'self'`) dış kaynaktan jeton enjeksiyonunu engelliyor, jetonu araç çağrısına yazmak ise sır yasağını çiğnerdi. Çözüm: jeton **yalnız Node sürecinde** okundu (`qa-fikstur.json`), CDP `Runtime.evaluate` ile `localStorage`a yazıldı. **Hiçbir jeton/parola değeri çıktıya yazılmadı.**
5. **Mutasyon kanıtlarının bir kısmı (M39-1…8) ilk ayakta üretildi**; bu ayakta o dosyaların bir bölümü daha da iyileştirildiği için `cmp` karşılaştırması ilk ayağın yedeğiyle birebir gelmiyor. Bu ayak, **değişen her kural için yeni mutasyon** koştu (M39-9…12 + M39-7b), hepsinde **taze yedek + `cmp` birebir aynı** kanıtı vardır (§ 5).

---

## 3. Veri modeli / migration

**Gerekmedi.** Paket yalnız sunum katmanıdır (CSS/JS + 5 Blazor bileşeni + 6 sayfada mekanik sarmalayıcı). Yeni varlık, yeni uç, yeni DTO yok; `dotnet ef` çalıştırılmadı.

**DÜZELTME (md. 1):** önceki sürüm "migration sayısı değişmedi (56)" diyordu — **taban sayı yanlıştı.**
Ölçüm: `[Migration]` öznitelikli Designer dosyası = **61**, `kuyumcu_net.__EFMigrationsHistory` = **61**.
P-39 migration eklemedi (doğru), yalnız rakam hatalıydı: **56 → 61**.

**`ORKESTRATOR-DURUM.md` DEĞİŞTİRİLMEDİ — ve sebebi bir düzeltmedir (md. 1).** Doğrulayıcı oradaki
"56 migration" rakamının da 61 yapılmasını istedi; **ölçtüm, o rakam yanlış değil.** `ORKESTRATOR-DURUM.md`
satır 204 bir **tarihsel kapı kaydıdır** (DALGA 4, 21 Eylül 05:15) ve o an gerçekten 56'ydı:

| Ölçüm | Sayı |
|---|---|
| `Migrations/*.Designer.cs` toplam | 65 |
| bunlardan **0 baytlık** (15 Eylül artığı, § 9.7) | 4 |
| `[Migration]` öznitelikli gerçek migration = 65 − 4 | **61** |
| mtime'ı 21 Eylül 05:15'ten **eski** Designer dosyası | 60 → gerçek: 60 − 4 = **56** |
| 05:15'ten sonra eklenen (P-19b, P-16, P-20b, P-21, P-22, P-37 …) | 5 → 56 + 5 = **61** |
| `kuyumcu_net.__EFMigrationsHistory` | **61** (tutarlı) |

Yani **tarihsel kayıt doğru, yalnız bu raporun "güncel" diye yazdığı rakam eskiydi.** Geçmiş kaydı
geriye dönük değiştirmek md. 1'e aykırı olurdu; düzeltme buraya yazıldı. Orkestratöre not: **güncel
durum satırlarında** (CLAUDE.md § 3 dâhil) "56 migration" ifadesi artık **61** olmalıdır.

## 4. Testler

| Koşum | Toplam | Geçti | Kaldı | Not |
|---|---|---|---|---|
| P-39 süzgeci (`FullyQualifiedName~P39TasarimKokleri`) | 25 | 25 | 0 | 8 kökün tamamı + 4 artık kural |
| Girdi ailesi + P-39 (mutasyon koşumu) | 48 | 42/48 (mutasyon altında) | — | bkz. § 5 |
| **Tam süit (önce, bu ayağın başı)** | **3715** | 3715 | 0 | ilk ayağın kodu, 21 P-39 testi |
| **Tam süit (sonra)** | **3719** | **3719** | **0** | `trx`: `scratchpad/trx-pP-39/` |

**Yeni test sayısı: 25** (21 ilk ayak + 4 bu ayak). Pakete girmeden önceki taban **3694**.
Derleme: `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı, 0 Hata**.
`dotnet/denetim.sh` → **DENETİM TEMİZ** (8 yeni P-39 sayacı dâhil).

Yeni testler (bu ayak): `K2_Kabuksuz_ogeler_de_KONTROL_rayina_oturur`, `K2_IKINCI_ALAN_AILESI_de_ayni_uc_raya_oturur`, `K2_Alt_izgara_bosluğu_UST_IZGARADAN_gelir`, `K5_Dosya_secici_girdi_kapsayicisini_ASMAZ`.

## 5. Mutasyon kanıtı

| Kod | Değişiklik | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| M39-1 | K1 sarma kuralı medya sorgusuna geri konur | `K1_Ust_bar_sarmasi_MEDYA_SORGUSU_DISINDA_tabanda_durur` | ilk ayak |
| M39-2 | `AlanKabugu` üç raylı yapısı bozulur | `K2_Alan_hucresi_UC_RAYLI_cizilir_etiket_kontrol_alt` | ilk ayak |
| M39-3 | ≤480 px asgari dokunma kutusu kaldırılır | `K3_Telefonda_dokunma_hedefi_44_pikselin_ALTINA_INEMEZ` | ilk ayak |
| M39-4 | Üçüncü düğme yüksekliği açılır | `K4_Dugme_yuksekligi_IKI_degerden_gelir` | ilk ayak |
| M39-5 / 5b | Ölü `tablo-sarmal` geri gelir / sarmalayıcı kaldırılır | `K5_Olu_sarmalayici_sinifi_tablo_sarmal_KULLANILMAZ`, `K5_Her_tablo_KENDI_kaydirma_kabindadir` | ilk ayak |
| M39-6 | Panel `.yukari` mekanizması kaldırılır | `K6_Panel_yer_yoksa_YUKARI_acilir_ve_tek_mekanizmadir` | ilk ayak |
| M39-7 | Önizleme koşulsuz çizilir (razor) | `K7_Girdinin_kendi_degerini_tekrar_eden_onizleme_CIZILMEZ` | ilk ayak |
| **M39-7b** | `BicimBilgiEkliyor` daima `true` (ölçüt iptal) | **6 test kırmızı**: `K7_Para_onizlemesi…`, `K7_Onizleme_yoksa_aria…`, `G8_Para_birimi_tutarla_birlikte_gosterilir`, `G9_Gram_has_karsiligi_uydurulmaz` (+2) | **cmp aynı** |
| M39-8 | Kontrol ailesine sabit piksel yükseklik yazılır | `K8_Kontrol_ailesinde_SABIT_piksel_yukseklik_bildirimi_YOK` | ilk ayak |
| **M39-9** | `.secici-alan` ızgaradan esneğe döner (ikinci aile dışarı) | `K2_IKINCI_ALAN_AILESI_de_ayni_uc_raya_oturur` | **cmp aynı** |
| **M39-10** | Alt ızgara `row-gap: inherit` → sabit `.3rem` | `K2_Alt_izgara_bosluğu_UST_IZGARADAN_gelir` | **cmp aynı** |
| **M39-11** | Sarmalayıcı kuralı `:only-child` → `*` | `K2_Kabuksuz_ogeler_de_KONTROL_rayina_oturur` | **cmp aynı** |
| **M39-12** | `input[type=file].girdi` `max-width: none` | `K5_Dosya_secici_girdi_kapsayicisini_ASMAZ` | **cmp aynı** |

Son mutasyondan sonra `dotnet build` **0 Uyarı / 0 Hata**; tam süit 3719/3719.

## 6. CANLI ÖLÇÜM (zorunlu) — önce / sonra

**Yöntem:** denetçinin § 10 betiği (aynı eşikler: hiza > 2 px, yükseklik > 2 px, dokunma < 44 px), başsız Chrome + CDP, `http://localhost:5198` (Debug), QA fikstür kiracısı (`QA-TEST-FRONTEND`), genişlikler 1440×900 / 1280×900 / 1024×820 / 375×812. Betikler: `scratchpad/p39-olcum.js` (ölçüt), `p39-kosum.mjs` (41 rota × 4), `p39-form-kosum.mjs` (13 açılan form × 4). Ham çıktı: `scratchpad/p39-olcum-sonra.json`, `p39-olcum-form.json`, `p39-olcum-artik.json`.

### 6.1 Toplam

| | ÖNCE (denetçi, 5199, 267 satır) | SONRA (bu ayak, 5198, 164 rota satırı) |
|---|---|---|
| Kusurlu satır | **141** | **34** |
| Temiz satır | 126 | **130** |

*(Satır tabanları birebir aynı küme değildir: denetçi 43 rota + 25 açılan form, bu ayak 41 rota. Açılan form durumları ayrıca ölçüldü — § 6.3.)*

### 6.2 Kök başına ölçülen değer

| Kök | ÖNCE (ölçülen) | SONRA (ölçülen) | Durum |
|---|---|---|---|
| **K1** belge yatay kayması @1024 | **67 satır / TÜM sayfalar, 91-160 px** | **0 satır** (4 genişlikte de 0 px) | **KAPANDI** |
| **K2** hiza kademe A | **28 kayıt / 8 kalıp / 6 ekran** (rota satırlarında 12) | **10 satır / 1 kalıp** (9 × 4 px filtre şeridi + 1 × 8,5 px `/emanet` @375) | büyük ölçüde kapandı, **artık var** (§ 8) |
| **K3** dokunma < 44 px @375 | **443 / 455 = %97,4** | **83 / 439 = %18,9** | kök kapandı, artık: metin içi bağlantı + onay kutusu |
| **K4** ayrı düğme yüksekliği (sistem geneli) | **15** | **10** @1440-1280-1024 · **3** @375 | iyileşti, hedefe (≤3) **ulaşılmadı** (§ 8) |
| **K5** ana içerik yatay kayması | `/hurda` @375 **58 px** | **0** (4 genişlikte de) | **KAPANDI** |
| K5' viewport'u aşan öğe | **68** | **31** (24 @375 · 6 @1024 · 1 @1280 · 2 @1440, tamamı `.tablo-sar` içinde kayan tablo) | iyileşti |
| **K6** panel tamamen görüş dışı | **5 vaka** | **0** (`/isletme`, `/hurda`, `/islemler`, `/urunler`, `/cariler` panelleri tek tek açıldı) | **KAPANDI** |
| **K7** mükerrer önizleme (`girdi-alti-tekrar`) | **240 kayıt / 89 satır / 8 alan / 5 ekran** | **1 kayıt / 1 satır** (`/raporlar/kasa`, tablo gövdesinde aynı tutarın geçmesi — girdi altı önizleme değil) | **KAPANDI** |
| **K8** yükseklik tutarsızlığı (kademe A) | **31 kayıt / 23 satır** | **0 satır** (4 genişlikte de) | **KAPANDI** |
| Kesik girdi (rota satırlarında) | 6 kayıt / 3 satır | **0** | kapandı (form içinde 2 kaldı — § 8) |
| Boş/açılmayan sayfa | 1 (`/vade-takvimi`) | **0** — böyle bir rota yok; `/evraklar/takvim` doldu | düzeltme (§ 2.3) |

### 6.3 Açılan form/çekmece durumları (denetimin en kötü ekranları)

| Anahtar | ÖNCE (denetçi) | SONRA |
|---|---|---|
| `/islemler # Ekle` | **41 px** hiza kusuru | **0** |
| `/islemler` (ürün seçili) | 19 px (kurucunun bulduğu) + mükerrer birim fiyat | **0 / 0** |
| `/cariler # + Yeni Cari` | 22 px (`fieldset#cari-p16`) | **0** |
| `/cariler # ✏️ Düzenle` | 22 px ×2 + mükerrer kredi limiti | **hiza 0 · mükerrer 0**, kesik girdi **2 kaldı** |
| `/urunler # + Yeni Ürün` / `# Düzenle` | yükseklik tutarsızlığı 4 genişlikte + mükerrer işçilik | **0 / 0** |
| `/urunler/etiketler` | 20,5 px, 3 genişlikte | **0** |
| `/emanet # + Emanet Al` | 10,2 px, 4 genişlikte | **0** @≥769 px · **8,5 px @375 kaldı** |
| `/faturalar # Yeni/Düzenle` | 4,5 px yükseklik + mükerrer toplam | **0 / 0** |
| `/hurda`, `/fiyatlandirma` | mükerrer birim fiyat / `'100'`–`'100'` | **0** |

### 6.4 Ölçüm sırasında bulunan ve KAPATILAN iki gerileme (md. 1)

1. **`/cariler # Düzenle` 21 px** — ilk artık düzeltmesi (`.alan` sarmalayıcısındaki her çocuğu kontrol rayına koymak) `<div class="alan"><Secici/></div>` kalıbında etiketi de aşağı itti. Kural daraltıldı (`:not(:has(> .secici-alan))` + `.alan > .secici-alan` ray aktarımı) → **0**.
2. **`/veri-ice-aktarim` @375 ana içerik 72 px** — aynı kural çok çocuklu sarmalayıcıda çocukları **yan yana sütunlara** dağıtıyordu. `:only-child` ile daraltıldı + dosya girdisine `max-width:100%` → **0**.

Her iki gerileme de birer testle bağlandı (M39-11, M39-12).

### 6.5 ÖLÇÜLEMEDİ

- **Ekran görüntüsü:** alınmadı (denetimde de alınamamıştı); bu paket piksel ölçümüne dayanır, görsel yargı içermez.
- **375 px'te açılan form durumlarının bir kısmı:** `/islemler # Ekle` ve `/cariler # Düzenle` düğmeleri 375 px'te bazı koşumlarda görünmedi (`dugme-bulunamadi`) — o satırlar kapalı hâliyle ölçüldü, uydurulmadı.
- **Bir kerelik oluşturucu kilitlenmesi:** ilk koşumda `/gider-pusulalari`de Chrome oluşturucusu kilitlendi. Sebep ölçüm betiğinin `MessageChannel` ile dönen bekleme döngüsüydü (ana ipliği aç bırakıyor); `setTimeout`a çevrildi, tekrar etmedi ve sayfa sonraki 4 koşumun hepsinde sorunsuz ölçüldü. **Sayfa kusuru değildir.**

## 7. Değişen dosyalar (P-39 kapsamı)

```
dotnet/Kuyumcu.Web/wwwroot/css/app.css
dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css
dotnet/Kuyumcu.Web/wwwroot/js/secici-yerlesim.js
dotnet/Kuyumcu.Web/Bilesenler/Form/AlanKabugu.razor
dotnet/Kuyumcu.Web/Bilesenler/Form/SayisalAlan.cs
dotnet/Kuyumcu.Web/Bilesenler/Form/Sayi.razor
dotnet/Kuyumcu.Web/Bilesenler/Form/Para.razor
dotnet/Kuyumcu.Web/Bilesenler/Form/Gram.razor
dotnet/Kuyumcu.Web/Bilesenler/Secici.razor
dotnet/Kuyumcu.Web/Pages/Pos.razor                     (K5 · +1 tablo-sar)
dotnet/Kuyumcu.Web/Pages/Kurulum.razor                 (K5 · +2 tablo-sar)
dotnet/Kuyumcu.Web/Pages/Vezne.razor                   (K5 · +2 tablo-sar)
dotnet/Kuyumcu.Web/Pages/Raporlar.razor                (K5 · +1 tablo-sar)
dotnet/Kuyumcu.Web/Pages/Hurda.razor                   (K5 · tablo-sarmal → tablo-sar)
dotnet/Kuyumcu.Web/Pages/Islemler.razor                (K2 · 3 satır içi align-items:flex-end silindi)
dotnet/Kuyumcu.Tests/P39TasarimKokleriTestleri.cs      (yeni)
dotnet/Kuyumcu.Tests/GirdiAilesiBilesenTestleri.cs     (K7 beklentileri)
dotnet/Kuyumcu.Tests/TasarimBelirtecTestleri.cs        (JS dosya sayacı 7→8)
dotnet/denetim.sh                                      (8 yeni sayaç)
```
`git status --porcelain` çalışma ağacında başka paketlerin değişikliklerini de gösterir; yukarıdaki liste
bu paketin dokunduğu dosyalardır. Sayfa satırları `scratchpad/mut-yedek/P-39/` altındaki değişiklik-öncesi
yedeklerle `diff -w` karşılaştırmasından üretilmiştir (önceki sürümdeki `grep -rl "P-39"` yöntemi, sayfalara
yorum satırı eklenmediği için 6 sayfayı **kaçırmıştı** — § 1.1 düzeltmesinin teknik sebebi budur).
**Commit yapılmadı.**

## 8. DoD sözleşmesi

Bu paket **yeni varlık/kayıt türü açmaz**: veri modeli, uç, DTO ve migration yoktur. Sözleşme-DoD'nin varlık maddeleri (PATCH / DELETE / `[RolGerekli]` / denetim kaydı) **kapsam dışıdır — düzenlenebilirlik yüzeyi değişmedi**, mevcut her formun Düzenle/Pasife al yolu olduğu gibi durur. Paketin doğrudan ilgilendiği DoD maddeleri:

| Madde | Karşılanma |
|---|---|
| DoD EK md. 1 — her düğme ya istek atar ya `disabled` + sebep | Değişmedi; K3 yalnız düğmenin **kutusunu** büyüttü, davranışına dokunmadı |
| DoD EK md. 2 — tek düzenleme kalıbı | **DÜZELTİLDİ:** `Pages/` altında **6 dosya açıldı** (§ 1.1, gerekçeleri kapı kaydında). Değişikliklerin tamamı sarmalayıcı `<div>` / ölü satır içi stil silme; **yeni `duzenlenenId` kalıbı yok**, `Pages/` altında yeni `ModalPencere` düzenleme kullanımı **0 artış** (denetim.sh sayaçları sabit, DENETİM TEMİZ) |
| DoD EK md. 3 — alan altı Türkçe hata | `AlanKabugu` üçüncü rayı hatayı taşımaya devam ediyor (`K2_Alan_alti_iceriginin_TAMAMI_ucuncu_raydadir`) |
| UX-S25 / UX-S30 | Kilit sebebi ve ipucu rayı korundu; `aria-describedby` zinciri önizleme kalkınca da doğru (test) |
| UX-S32 | Para birimi tutarla birlikte taşınıyor: önizleme kalksa da `-birim` rozeti kalır (test) |

## 9. Açık kalanlar

1. **K2 artığı — filtre şeridinde `.arama-kutu` 4 px** (`/cariler`, `/urunler`, `/denetim-izi`; 3 genişlik × 3 ekran = 9 satır). Sebep: arama kutusunun **etiket rayı yok** (`label.gorsel-gizli`), ve filtre şeridi bir `subgrid` kapsayıcısı değil. Çözüm önerisi: ya şerit de üç raylı ızgaraya alınır, ya `Arama` bileşenine yer kaplayan (görsel olarak gizli) etiket rayı verilir. **Bu pakette kapatılmadı** — kapsayıcı sınıfı ekrandan ekrana değişiyor, ölçülmeden yapılacak genel kural yeni gerileme riski taşıyor.
2. **K2 artığı — `/emanet` @375, 8,5 px.** `≤768 px`de satır bilinçli olarak esnek/sarmalı bırakıldı (mobil sarma davranışı korunsun diye). Telefon hizası ayrı bir karar ister.
3. **K4 hedefi tutmadı: 10 ayrı düğme yüksekliği** (tavan ≤3 idi; taban 15). Kalanlar ikon düğmeleri, rozet benzeri `.pill`'ler ve tablo içi küçük eylemler. `denetim.sh` sayacı **kaynak** tarafında 2'de kilitli; **canlı** tarafta 10. Bu fark raporun § 2.2'sinde açıklandı ve kapatılması için ayrı bir paket gerekir.
4. **`/cariler # Düzenle` iki kesik girdi** (`duz-eposta`, `duz-unvan`) duruyor. Denetimin § 3.1 bulgusu; 8 kökün içinde değildi.
5. **`.secici-alan` ikinci alan ailesi hâlâ ikinci ailedir.** CSS ile aynı raya oturtuldu ama `Secici` kendi kabuğunu çizmeye devam ediyor. Kalıcı çözüm `Secici`nin `AlanKabugu`ya geçmesidir (bileşen değişikliği, ayrı paket).
6. **Artık dosya — KURUCUYA SİLME LİSTESİ (20 dosya, hook `rm`'i bloklar).** Doğrulama turunun bulgusu; hepsi **P-39'dan ÖNCEKİ** paketlerin ayak izidir (mtime 21 Eylül 21:40-21:45), derlemeyi ve testleri bozmuyorlar. `find dotnet \( -name '*.mutorj' -o -name '*.bak' -o -name '*.mutbak' \) -newer dotnet/Kuyumcu.sln` çıktısı:

   ```
   dotnet/Kuyumcu.Api/Data/HareketFabrikasi.cs.mutorj
   dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatModulu.cs.mutorj
   dotnet/Kuyumcu.Api/Modules/Fiyat/FiyatMotoru.cs.mutorj
   dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs.mutorj
   dotnet/Kuyumcu.Api/Modules/Satis/OdemeSepeti.cs.mutorj
   dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs.mutorj
   dotnet/Kuyumcu.Api/Modules/Sayim/SayimModulu.cs.mutorj
   dotnet/Kuyumcu.Api/Modules/Urun/BelgeModulu.cs.mutorj
   dotnet/Kuyumcu.Api/Modules/Urun/PirlantaKapisi.cs.mutorj
   dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs.mutorj
   dotnet/Kuyumcu.Api/Modules/Vade/VadeKapisi.cs.mutorj
   dotnet/Kuyumcu.Tests/AramaHttpTestleri.cs.bak
   dotnet/Kuyumcu.Tests/MadenDefteriTestleri.cs.bak
   dotnet/Kuyumcu.Tests/P21CiroRucuTestleri.cs.bak
   dotnet/Kuyumcu.Web/Pages/Fiyatlandirma.razor.mutorj
   dotnet/Kuyumcu.Web/Pages/Islemler.razor.mutorj
   dotnet/Kuyumcu.Web/Pages/Sayim.razor.mutorj
   dotnet/Kuyumcu.Web/Pages/Urunler.razor.mutorj
   dotnet/Kuyumcu.Web/Pages/VadeTakibi.razor.mutorj
   dotnet/denetim.sh.mutbak   (0 bayt)
   ```

   **Sayaç önerisi (bir sonraki pakete):** `denetim.sh` bugün yalnız `.bak` beyaz listesini denetliyor, `.mutorj`/`.mutbak` denetlemiyor. `ARTIK_DOSYA=0` sayacı eklenmeli — **ama bu pakette eklenmedi**: sayaç fail-closed olduğu için 20 dosya silinmeden eklenmesi `denetim.sh`ı kırar, silme yetkisi ise kurucudadır. Doğru sıra: kurucu siler → sonraki paket sayacı kurar.

7. **Sıfır baytlık migration dosyaları (8 adet, 15 Eylül, P-39 öncesi).** `find dotnet/Kuyumcu.Api/Data/Migrations -name '*.cs' -empty`:
   `20260915131748_FiyatListesiVeKampanya{,.Designer}.cs`, `20260915132123_…`, `20260915132303_…`, `20260915132645_…`.
   Derlemeyi bozmuyorlar ve DB geçmişiyle tutarsızlık yaratmıyorlar (Designer `[Migration]` sayısı **61** = `__EFMigrationsHistory` **61**). Kurucu silsin; kapı kontrol listesine **"sıfır baytlık migration = 0"** şartı kalıcı eklensin.

8. **K4 canlı/kaynak farkı açık kalan olarak kuyruğa alındı** (doğrulayıcı ORTA bulgusu, § 9.3 ile aynı madde): kaynak sayacı 2'de kilitli, canlı ölçüm 10. Kalanlar ikon düğmeleri, `.pill` rozetleri ve tablo içi küçük eylemler. Gizlenmiş değil, ayrı paket gerekir.

## 10. Kurucuya / mali müşavire notlar

- **Kurucunun bildirdiği madde kapandı:** adet kutusuna yazılan sayı artık altta tekrarlanmıyor. Düzeltme **tek bileşende** yapıldı (`SayisalAlan`), 5 ekran birden düzeldi; `/fiyatlandirma`daki `'100'`/`'100'` tekrarı da aynı kökten gitti. Ölçüm: 240 mükerrer kayıt → 1 (o da tablo gövdesinde, girdi altı önizleme değil).
- **En yüksek getirili madde de kapandı:** 1024 px'te **43 ekranın tamamı** yatay kayıyordu, artık **hiçbiri kaymıyor** ("Profesyonel" düğmesi de ekrana girdi).
- **Mali/mevzuat hükmü yok** — bu paket görsel katmandır, mali müşavir notu gerekmedi.
- **GÜVENLİK BİLDİRİMİ (md. 1, gizlenmiyor) — KÖK NEDEN BULUNDU:** ölçüm sırasında MySQL root parolası bir komut çıktısında ekrana geldi (oturum kaydında durur). Parola kullanılmadı, hiçbir dosyaya yazılmadı. **Kök neden:** `docker-compose.yml`de `MYSQL_ROOT_PASSWORD` **iki satırda** geçiyor — satır 3 bir **yorum satırı** (parola açıkça yazılı), satır 14 gerçek ortam değişkeni. Runbook'taki `grep MYSQL_ROOT_PASSWORD … | head -1` reçetesi **yorum satırını seçiyor**, yanlış değer veriyor (`ERROR 1045 … Access denied`) ve doğrulama sırasında parolanın görünür olmasına yol açıyor. **Üç düzeltme istenir (kurucu):**
  1. `kuyumcu-mysql` root parolası **döndürülsün**;
  2. `docker-compose.yml` satır 3'teki **yorumdan parola çıkarılsın**;
  3. runbook reçetesi `grep MYSQL_ROOT_PASSWORD docker-compose.yml | grep -v '^\s*#' | head -1 | awk '{print $2}'` olarak düzeltilsin. *(Bu ayak doğru reçeteyi kullandı; parola değişkende kaldı, ekrana basılmadı.)*
- **Test kiracısı:** bu paket **yeni kiracı açmadı**; kalıcı QA fikstürü (`QA-TEST-FRONTEND`) kullanıldı, verisine dokunulmadı. `tenants` sayısı **9** (başlangıçtaki değer). 5198 ve yardımcı 5297 portları kapatıldı; **5199'a dokunulmadı** (çalışmaya devam ediyor).

---

## 11. DOĞRULAMA TURU 2 — hasım doğrulayıcının bulgularının kapanışı (2026-09-21, üçüncü ayak)

Hasım doğrulayıcı 5 bulgu bıraktı. Hepsi bağımsız olarak **yeniden ölçüldü** ve doğrulandı; hiçbiri reddedilmedi.

| # | Bulgu | Bağımsız doğrulama | Kapanış |
|---|---|---|---|
| 1 | **[KRİTİK]** Rapor "hiçbir `Pages/*.razor` açılmadı" diyor, 6 sayfa değişti | **DOĞRU.** `scratchpad/mut-yedek/P-39/` altında tam o 6 sayfanın değişiklik-öncesi yedeği duruyor (Pos·Kurulum·Islemler·Vezne·Raporlar·Hurda, mtime 22:10-22:18); `diff -w` her birinde yalnız sarmalayıcı/ölü-stil farkı gösteriyor | **KAPANDI** — § 1.1 düzeltme + kapı kaydı, § 7 dosya listesi, § 8 DoD satırı yeniden yazıldı. Beyan artık gerçeğe uyuyor |
| 2 | **[ORTA]** 19 mutasyon/yedek artığı | **DOĞRU** (+ `denetim.sh.mutbak` = 20). Hepsi 21 Eylül 21:40-21:45, P-39 öncesi | **AKTARILDI** — § 9.6'da tam silme listesi + sayaç önerisi; silme kurucuda (hook `rm`'i bloklar) |
| 3 | **[ORTA]** K4 canlıda 10 yükseklik (hedef ≤3) | Kaynak sayacı `DUGME_YUKSEKLIK_CESIDI = 2` (tavan 2) — bu ayakta yeniden koşuldu, **DENETİM TEMİZ**. Canlı ölçüm bu ayakta tekrarlanmadı (API 5198 açılmadı) | **AÇIK KALAN** — § 9.3 + § 9.8; gizlenmiyor, ayrı paket |
| 4 | **[ORTA]** MySQL parolası oturum kaydına düştü; kök sebep `docker-compose.yml` yorum satırı | **DOĞRU.** `MYSQL_ROOT_PASSWORD` iki satırda; `grep … \| head -1` yorumu seçiyor | **KAPANDI (bildirim)** — § 10'a kök neden + 3 maddelik düzeltme istemi yazıldı; bu ayak doğru reçeteyi kullandı, hiçbir değer ekrana gelmedi |
| 5 | **[DÜŞÜK]** migration 56 yazılmış, gerçek 61 · 8 sıfır baytlık migration dosyası | **KISMEN DOĞRU.** Raporun rakamı gerçekten eskiydi (Designer `[Migration]` = **61**, `__EFMigrationsHistory` = **61**; `-empty` = **8**). **Ama** `ORKESTRATOR-DURUM.md`deki 56, 21 Eylül 05:15 kapısının **tarihsel** kaydıdır ve o an doğruydu (aritmetiği § 3'te) | **KAPANDI** — § 3 düzeltildi (56→61) + aritmetik kanıt; tarihsel kayıt **bilerek değiştirilmedi** (geriye dönük değiştirmek md. 1'e aykırı); 8 boş dosya § 9.7'de kurucuya |

### 11.1 Bu ayakta yeniden koşulan kapılar (taze kanıt)

| Kapı | Komut | Sonuç |
|---|---|---|
| Derleme | `dotnet build Kuyumcu.sln --nologo` | **0 Uyarı, 0 Hata** |
| Denetim | `bash dotnet/denetim.sh` | **DENETİM TEMİZ** — 8 P-39 sayacının tamamı yeşil (`UST_BAR_TASMA_1024 0` · `FORM_SATIR_HIZA_SAPMASI 0` · `DOKUNMA_HEDEFI_KUCUK 0` · `DUGME_YUKSEKLIK_CESIDI 2` (tavan 2) · `TABLO_SARMALAYICISIZ 0` · `PANEL_VIEWPORT_DISI 0` · `ONIZLEME_MUKERRER 0` · `KONTROL_YUKSEKLIK_SAPMASI 0`) |
| Tam süit | `dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo --logger trx` | **Başarısız: 0 · Başarılı: 3719 · Atlanan: 0 · Toplam: 3719** (54 s) · trx: `scratchpad/trx-pP-39b/` |
| Migration | Designer `[Migration]` / `__EFMigrationsHistory` | **61 / 61** (tutarlı) |
| Kiracı | `SELECT COUNT(*) FROM tenants` | **9** (başlangıç değeri) · `ad LIKE 'PAKET%TEST%'` → **0** · `QA-TEST-*` fikstürlerine dokunulmadı |
| Port | `lsof -nP -iTCP:5198 -sTCP:LISTEN` | **boş** · 5199'a dokunulmadı |
| **Kurucunun maddesi (K7)** | `dotnet test --filter FullyQualifiedName~K7` | **4/4 yeşil**: `K7_Girdinin_kendi_degerini_tekrar_eden_onizleme_CIZILMEZ` · `K7_Bilgi_EKLEYEN_onizleme_KORUNUR` · `K7_Para_onizlemesi_de_tekrar_sayilir_birim_yaninda_kalir` · `K7_Onizleme_yoksa_aria_describedby_de_ONU_GOSTERMEZ`. Kaynak kontrolü: `Bilesenler/Form/SayisalAlan.cs:80-93` (`BicimBilgiEkliyor` → `OnizlemeGorunur`) ve `Sayi.razor:22` koşullu; düzeltme **ortak bileşende**, ekran başına değil |

### 11.2 Canlı piksel ölçümü — NEDEN TEKRARLANMADI ve § 6 rakamları hâlâ geçerli mi (md. 1, tahmin yok)

**Bu ayakta canlı ölçüm tekrarlanmadı ve uydurulmadı.** § 6'daki sayılar ikinci ayağın ölçümüdür
(`scratchpad/p39-olcum-sonra.json`, 21 Eylül **23:30**). Geçerliliği şöyle denetlendi:

- Bu üçüncü ayak **yalnız `.md` dosyalarına** dokundu; § 7 listesindeki kod dosyalarının hiçbirini açmadı.
- Ölçümden sonra iki kod dosyasının mtime'ı ilerlemiş görünüyor (`app.css` ve `Bilesenler/Form/SayisalAlan.cs`, **23:40**). Bu, mutasyon turunun **geri yükleme** adımıdır: her ikisi de `scratchpad/mut-yedek/P-39b/` altındaki yedekle `cmp` ile **birebir aynıdır** (bayt farkı 0) — yani mtime değişti, **içerik değişmedi**.
- **Açıkça yazılan tek boşluk:** o yedekler 23:33-23:34'te alınmıştır, ölçümden **3 dakika sonra**. 23:30 ile 23:33 arasında dosyaların değişmediğini mtime ile *kanıtlayamıyorum*; mutasyon akışı (yedek al → boz → test → geri yükle → `cmp`) bunu gerektirir ama **doğrulanmış değildir, varsayım olarak sunulmuyor**.
- Diğer 9 kod dosyasının mtime'ı **22:10-22:23**, yani ölçümün tamamen öncesinde.

**Sonuç:** § 6 rakamları bu ayakta **yeniden üretilmedi**; taşınabilir kanıt `denetim.sh`ın 8 fail-closed
kaynak sayacı ve 3719 testtir (ikisi de bu ayakta taze koşuldu). Piksel tarafının mutlak tazeliği isteniyorsa
ölçüm betiği (`scratchpad/p39-olcum.js` + `p39-kosum.mjs`) tek komutla yeniden koşulabilir — orkestratör
kapısına bırakıldı.

---

*Rapor: 2026-09-21 · Taşkın (backend) + Ertuğ (frontend) rolü · Opus uygulayıcı.*
*Üçüncü ayak (doğrulama turu 2 kapanışı): 2026-09-21 — beyan düzeltmeleri + kapıların taze koşumu.*
