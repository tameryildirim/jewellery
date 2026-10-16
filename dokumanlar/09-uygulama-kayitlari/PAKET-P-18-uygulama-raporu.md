# PAKET P-18 — MOBİL PARİTE + TEMA BELİRTEÇLERİ + STICKY SÜTUN

**Tarih:** 2026-09-21 · **Dalga:** DALGA 4 · **Sürüm:** 1.0
**Roller:** Taşkın (backend) + Ertuğ (frontend) · **Kimlikler:** B-9 · B-30 · B-32
**Plan bloğu:** `PLAN-TEK-SEFER-2026-09-18.md` satır 441-458 · **Bağlı koşul:** BK-24
**Migration:** HAYIR (plan böyle diyor; şema değişikliği gerekmedi — doğrulandı)

---

## § 0 — ÖNKOŞUL ÖLÇÜMÜ (varsayım yok)

| Önkoşul | Diskte | Kanıt |
|---|---|---|
| P-16 | ✅ VAR | `dokumanlar/09-uygulama-kayitlari/PAKET-P-16-uygulama-raporu.md` |
| P-01 | ✅ VAR | `PAKET-P-01-uygulama-raporu.md` · `denetim.sh` `# >>> P-01` bloğu canlı |
| R-13 kararı (P-01) | ✅ mühürlü | `denetim.sh` P-01 bloğu R-13 sayacını taşıyor |
| `Bilesenler/KartListe.razor` | ❌ YOKTU → **bu pakette açıldı** | `test -e` |
| `betikler/qa-turu.js` · `hazirlik-kurulu.js` | ✅ VAR | `test -e` |
| Tek el zinciri `Pages/Islemler.razor` | P-16 kapanmış, P-18 tek el | P-16 raporu §, kapı kaydı |

**Kapsam daraltması gerekmedi** — her önkoşul yerindeydi.

---

## § 1 — SPESİFİKASYONUN KODA KARŞI DOĞRULANMASI (sapmalar)

Plan DoD'u **"bugün: cariler 996, urunler 859, isletme 729, banka 789, islemler 571"** diyordu.
**BU RAKAMLAR BAYATTI.** 2026-09-21'de gerçek Chrome'da (CDP, 375×812, oturum açık) tek tek ölçtüm:

| Plan iddiası | Ölçülen gerçek | Sapma |
|---|---|---|
| cariler 996 | **592** | Plan 404 px fazla söylüyordu |
| urunler 859 | **600** | 259 px fazla |
| isletme 729 | **412** *(taşma kaynağı farklı — aşağıda)* | 317 px fazla |
| banka 789 | **588** | 201 px fazla |
| islemler 571 | **603** | 32 px eksik |
| “42 rota” | **44 rota** | P-16/P-17 iki ekran ekledi (`Vitrin`, `PusulaBelgesi`) |

**En önemli sapma — TEŞHİS YANLIŞTI.** Plan taşmayı *tablolara* bağlıyor ve düzeltmeyi
"yalnız tablo sarmalayıcı" olarak tarif ediyordu. Ölçüm bunu **çürüttü**: tablolar zaten
`.tablo-sar` içinde hapsedilmişti. **Suçlu ÜST BARDI.**

```
44/44 rotada taşan öğeler (ölçüm çıktısı, P-18 öncesi):
  div.ust-sag@582…630 | button.kur-guncelle-btn@400…448 |
  div.mod-anahtar@582…630 | div.sube-secici@378…416
```

`.ust-bar{display:flex; justify-content:space-between}` sarmıyordu, `h1` küçülmüyordu,
`.ust-sag`/`.sube-secici` `flex-wrap` taşımıyordu → kabuğun min-content genişliği
565-814 px'e çıkıyor ve **her rotayı birden** taşırıyordu.

**Spesifikasyonun HEDEFİNE sadık kalındı** (42/42 → gerçekte 44/44 rota `scrollWidth ≤ 375`),
**yöntemi düzeltildi** (PLAN § 1 md.1 "mekanizma önce"): düzeltme tek tek sayfalara değil
**kabuğa** yazıldı. Bu, plan bloğu ile bu koşullar arasındaki tek çelişkidir ve plan
üstündür kuralı gereği planın **DoD'u** (≤375) korunmuş, **dosya gerekçesi** güncellenmiştir.

**İkinci ölçülmüş teşhis (planda yoktu):** `/isletme` 412 px'te takılı kaldı. Sebep
`.gorunmez{position:absolute}` (ekran okuyucu metni): `.tablo-sar` **konumsuz** olduğu için
onun kapsayıcı bloğu değildi ve öğe `overflow` kırpmasından **kaçıyordu**.
`.tablo-sar{position:relative}` bunu kapattı.

**Yasal/mevzuat hükmü yok** — bu paket tamamen arayüz katmanıdır; koda sabitlenen bir
mevzuat varsayımı üretilmedi.

---

## § 2 — KİMLİK KAPANIŞ TABLOSU

| Kimlik | Durum | Kanıt |
|---|---|---|
| **B-9** — Mobil parite, 42/42 rotada 375 px | **KAPANDI** | Gerçek Chrome/CDP, **44/44 rota tek tek**, aydınlık **ve** karanlık = **88 ölçüm, 0 taşma** (§ 6 tablosu · `olcumler/P-18-mobil-375-aydinlik.json`, `-karanlik.json`) · `app.css:1155-1180` · test `B9_Ust_bar_DAR_EKRANDA_sarar_ve_baslik_kuculebilir` · sayaç `P-18: MOBIL_YATAY_KAYDIRMA 0 / 88` |
| **B-30** — `/yetkiler` ROL sütunu sticky değil | **KAPANDI** | `Pages/Yetkiler.razor:45` `<KartListe … YapiskanIlkSutun="true">` · `app.css:1185-1192` · **canlı kanıt:** `scrollLeft=300` sonrası ROL başlığı `left` **13 → 13** (kaymadı), zemin aydınlık `rgb(248,250,252)` / karanlık `rgb(27,38,52)` · test `B30_Yetkiler_ROL_sutunu_YAPISKAN_isaretlidir` + `B30_Yapiskan_ilk_sutun_KartListe_uzerinden_soezlesmeye_baglidir` |
| **B-32** — Karanlık temada satır vurgusu okunamıyor | **KAPANDI** | `.ozet-kutu--amber` ham palet (`--amber-50`) → `--uyari-yuzey`; `app.css:840` · **canlı ölçüm:** aydınlık `rgb(253,246,231)` + metin `rgb(30,41,59)`, karanlık `rgb(42,35,19)` + metin `rgb(232,238,246)` (daha önce her iki temada da `rgb(255,251,235)` idi) · razor sabit renk **31 → 14** · test `B32_Vurgu_yuzeyleri_…` + `B32_Razor_dosyalarinda_SABIT_RENK_tavani_asilmaz` · sayaç `TEMA_HAM_PALET_VURGU 0` |

**Devredilen yok.** Üç kimlik de bu pakette kapandı.

---

## § 3 — YAPILANLAR

### 3.1 Mekanizma: kabuk (B-9)
`app.css`'e işaretli P-18 bloğu eklendi. `≤768px`: `.ust-bar{flex-wrap:wrap}`,
`.ust-bar h1{flex:1 1 8rem;min-width:0;overflow-wrap:anywhere}`, `.ust-sag{flex-wrap:wrap}`,
`.sube-secici{flex-wrap:wrap;max-width:100%}`, `.mod-anahtar{flex-wrap:wrap}`,
`.kenar-cubugu{max-width:88vw}`. `≤480px`: mod anahtarı küçülür, şube etiketi tam satır olur.
**Geniş ekran görünümü değişmedi** (kurallar yalnız medya sorgusunda).
Ayrıca `.tablo-sar{position:relative}` — kaydırma kutusu mutlak konumlu torununu da hapseder.

### 3.2 Yeni bileşen: `Bilesenler/KartListe.razor` (B-9 · B-30)
Veri tablosunun **TEK** sarmalayıcısı. Sağladıkları:
- `.tablo-sar` içine alır (taşma hapsi),
- `role="region"` + `tabindex="0"` + `aria-label` → **kaydırılabilir bölge klavyeyle erişilebilir** (WCAG 2.1.1),
- `aria-describedby` ile "yatay kaydırılabilir" ipucu (UX-S29: sınır ekranda söylenir; ipucu **yalnız** ≤768px'te görünür),
- `YapiskanIlkSutun` ile B-30 yapışkan ilk sütun,
- `Id` verilirse `<önek>-sar` / `<önek>-ipucu` türetir; verilmezse **boş `id` basmaz**.
**Mevcut element id'leri değişmedi**; yalnız `yetki-matris-sar`, `cari-liste-sar` gibi **yeni** id'ler eklendi (`<önek>-<alan>` sözleşmesi).
9 tablo 6 ekranda (`Cariler`, `Urunler`×3, `Isletme`, `Banka`, `Islemler`×2, `Yetkiler`) bu bileşene alındı; elle `<div class="tablo-sar">` **kalmadı**.

### 3.3 Tema belirteçleri (B-32)
| Yer | Önce | Sonra |
|---|---|---|
| `app.css .ozet-kutu--amber` | `var(--amber-50)` / `--amber-200` (ham palet, temayla DEĞİŞMEZ) | `var(--uyari-yuzey)` / `var(--uyari-kenar)` |
| `Pages/Islemler.razor` `<tr style="background:var(--amber-50)">` | satır içi ham palet | `class="satir-vurgu"` |
| `Pages/Islemler.razor` `style="background:var(--slate-50)"` | satır içi ham palet | `class="kart kart--soluk"` |
| `Pages/Cariler.razor` `style="…border-bottom:1px solid var(--slate-100)"` | satır içi ham palet | `class="satir-arasi liste-satiri"` |
| `Bilesenler/MasakFormu.razor` `style="border-left:…var(--amber-500);background:var(--amber-50)"` | satır içi ham palet | `class="kart kart--masak"` |
| `Bilesenler/PastaGrafik.razor` 6 sabit hex + 3 satır içi style | `#b45309 …` | `var(--grafik-1…5)` + `--bilgi`; `.grafik-pasta`, `.grafik-lejant*` sınıfları |
| `Bilesenler/CubukGrafik.razor` 3 sabit hex | `#059669`, `#b45309`, `#64748b` | `var(--grafik-3)`, `var(--grafik-1)`, `var(--metin-soluk)` |
| `Bilesenler/AlanGrafik.razor` 4 sabit hex | `#f59e0b`, `#b45309`, `#64748b` | `var(--vurgu)`, `var(--metin-soluk)` |
| `Pages/Raporlar.razor` 4 satır içi `color:#…` | yaşlandırma sütunları | `.yas-0-30 / -31-60 / -61-90 / -90-usta` (belirteç) |

Sonuç: **razor sabit renk 31 → 14**, **satır içi style 154 → 143**.
Kalan 14'ün tamamı `Pages/Giris.razor`'un tanıtım panelidir; o panel `belirtecler.css`'te
**bilinçli olarak** her iki temada da koyudur (`--giris-panel-*`), yani tema değişkeni değildir.

### 3.4 Sözlük (TR + EN)
`SozlukKabuk`'a `TabloBolgesi` ve `TabloKaydirIpucu` eklendi (`required init` → anahtar eksikse
**derleme hatası**; çıplak metin yolu kapalı). TR ve EN karşılıkları yazıldı.

### 3.5 Yol üstünde bulunan GERÇEK HATA (planda yoktu)
`Pages/Cariler.razor:18` `<SayfaBasi Anahtar="cariler" …>` **`Baslik` vermiyordu** →
`/cariler` sayfasının `<h2 id="sayfa-basi-cariler-baslik">` başlığı **BOŞ** çiziliyordu.
Derleyici bunu `warning RZ2012` ile söylüyordu ama yalnız `Kuyumcu.Web` yeniden derlendiğinde
görünüyordu. `Baslik="@Dil.T.Cari.Baslik"` eklendi; derleme **0 uyarı**ya döndü.

---

## § 4 — [KRİTİK OLAY] `dotnet/denetim.sh` sıfırlandı ve TAM KURTARILDI

**Anayasa md.1 gereği gizlenmiyor.** 05:53'te P-18 sayaç bloğunu eklemek için koştuğum
komut hatalıydı:

```
perl -0777 -pi -e 'my $b = do { local(@ARGV,$/) = ("…/p18-blok.sh"); <> }; s/…/$1$b/s' denetim.sh
```

`local(@ARGV, …)` perl'in `-i` işleyicisinin girdi listesini **değiştirdi**: sonuç
`p18-blok.sh`'a yazıldı, `denetim.sh` **0 bayta düştü**. Dosya git'te izlenmiyordu
(`fatal: path 'dotnet/denetim.sh' … not in 'HEAD'`) ve `denetim.sh.mutbak` de 0 baytlıktı.

**Kurtarma (kanıtlı):** perl çıktıyı `p18-blok.sh`'a yazdığı için o dosya
**orijinal `denetim.sh`'in TAMAMI + P-18 bloğu** oldu. Doğrulama:

| Kontrol | Beklenen (kayıp öncesi bu oturumda okunan) | Kurtarılan dosya |
|---|---|---|
| `>>> P-00 … <<< P-11` satır numaraları | 998 … 2424 | **birebir aynı** |
| `>>> P-12` / `<<< P-12` | 2426 / 2524 | **2426 / 2524** |
| `>>> P-17` / `<<< P-17` | 2990 / 3100 | **2990 / 3100** |
| `bash -n` | — | **SÖZDİZİMİ OK** |
| `./denetim.sh` | `DENETİM TEMİZ.` | **`DENETİM TEMİZ.` · exit 0 · 168 ✓** |
| P-12 + P-17 sayaç adları/değerleri | P-17 raporu § 14 | **12/12 birebir aynı** |
| Canlı sayaç testi (NK-R1) | `✗ ORTAK_SABLON_DISI_PRINT 1` | **aynen üretti** (§ 5, NK-R1) |

**Veri kaybı: YOK.** Ara adımda kullandığım 04:16 tarihli `mut-yedek/P-17/denetim.sh.orig`
yedeği 5 satır eksikti (P-17 rev 1.1'in `${=VAR}` → bash dizisi düzeltmesi); o yol
**terk edildi**, tam kurtarma kullanıldı. Yine de o ara adımda P-17 rev 1.1 düzeltmesini
elle yeniden yazmış ve **NK-R1 ile canlı doğrulamıştım** — yani iki bağımsız yoldan aynı
sonuca varıldı.

**Alınan ders (P-18 bloğuna yorum olarak yazıldı):** `denetim.sh` **bash**'tir; `${=VAR}`
zsh sözdizimidir ve sayacı sessizce öldürür. P-18 bloğu bu yüzden **bash dizisi** kullanır.

> **KURUCUYA:** `dotnet/denetim.sh` **git'te izlenmiyor** (`.gitignore`/ilk commit dışı).
> 3.234 satırlık kalite kapısının tek kopyası çalışma ağacındadır. Bu paket onu bir kez
> kaybetti ve şans eseri kurtardı. **K-öneri:** `denetim.sh` sürüm kontrolüne alınsın
> (commit yasağı sizde; karar sizin).

---

## § 5 — MUTASYON KANITLARI

Yöntem: kaynak `scratchpad/mut-yedek/P-18/` altına kopyalandı → **tek eşleşmeli** değişiklik
(`node mut.js`, eşleşme ≠ 1 ise durur) → filtreli `dotnet test` + `./denetim.sh` → yedek geri
kopyalandı → `cmp` ile birebir doğrulandı. Proje ağacına `.bak/.tmp/.mutorj` **bırakılmadı**.

| Kod | Mutasyon | KIRMIZIYA DÖNEN TEST (adıyla) | denetim.sh | Geri alım |
|---|---|---|---|---|
| **M18-1** | `.ust-bar{flex-wrap:wrap}` → `nowrap` | `MobilPariteCssTestleri.B9_Ust_bar_DAR_EKRANDA_sarar_ve_baslik_kuculebilir` | `✗ MOBIL_KABUK_SARMA 4/5` · exit 1 | `cmp` AYNI |
| **M18-2** | `.tablo-sar{position:relative}` → `static` | `B9_Kaydirma_kutusu_MUTLAK_KONUMLU_torununu_da_hapseder` | `✗ MOBIL_KABUK_SARMA 4/5` · exit 1 | `cmp` AYNI |
| **M18-3** | `.ana-icerik{overflow-x:hidden}` eklendi (taşmayı gizleme) | `B9_Tasma_OVERFLOW_HIDDEN_ile_GIZLENMEZ` | `✗ MOBIL_TASMA_GIZLEME 1` · exit 1 | `cmp` AYNI |
| **M18-4** | `.ozet-kutu--amber` geri `var(--amber-50)` | `B32_Vurgu_yuzeyleri_HAM_PALET_degil_SEMANTIK_belirtecten_gelir` | `✗ TEMA_HAM_PALET_VURGU 1` · exit 1 | `cmp` AYNI |
| **M18-5** | `Yetkiler.razor` `YapiskanIlkSutun="true"` → `"false"` | `B30_Yetkiler_ROL_sutunu_YAPISKAN_isaretlidir` | `✗ YETKILER_ROL_YAPISKAN razor 0` · exit 1 | `cmp` AYNI |
| **M18-6** | `PastaGrafik` `var(--grafik-1)` → `#b45309` | `B32_Razor_dosyalarinda_SABIT_RENK_tavani_asilmaz` | `✗ SABIT_RENK 15 > tavan 14 · panel dışı 1` · exit 1 | `cmp` AYNI |
| **M18-7** | `Cariler.razor` KartListe dışına elle `<div class="tablo-sar">` | `B9_Veri_tablolari_TEK_sarmalayici_kalibini_kullanir` | `✗ MOBIL_TABLO_SARMALAYICISIZ 1` · exit 1 | `cmp` AYNI |
| **M18-8** | `KartListe.razor`'dan `tabindex="0"` kaldırıldı (derleme gerekti) | `KartListeTestleri.Tabloyu_KAYDIRILABILIR_ve_KLAVYEYLE_ERISILEBILIR_bolgeye_alir` | — (bileşen sözleşmesi) | `cmp` AYNI |
| **NK-R1** | `IslemDefteri.razor`'a beyaz liste dışı `window.print` (kurtarılan P-17 sayacı canlı mı?) | — | `↳ beyaz liste dışı: …IslemDefteri.razor` · `✗ P-17: ORTAK_SABLON_DISI_PRINT 1` · exit 1 | `cmp` AYNI |

**Test açığı bulunmadı:** her mutasyon en az bir testi VE/VEYA sayacı kırmızıya çevirdi.
Son mutasyondan sonra temiz `dotnet build`: **0 uyarı · 0 hata**.

---

## § 6 — CANLI DOĞRULAMA (localhost:5198 — 5199'a DOKUNULMADI)

Test kiracısı **PAKETP-18-TEST Kuyumculuk** (sistem daveti ile) açıldı, iş sonunda silindi.
Ölçüm aracı: **gerçek Google Chrome** (headless=new, CDP), `Emulation.setDeviceMetricsOverride`
375×812 mobil, `Emulation.setEmulatedMedia prefers-color-scheme`.
**Örnekleme YOK** (BK-24): her rota tek tek gezildi ve `document.documentElement.scrollWidth`
okundu. Betik depoya alındı: `dokumanlar/09-uygulama-kayitlari/betikler/mobil-olc.js`.

### 6.1 HTTP kanıtı
| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| 1 | `POST /api/v1/sistem/davetler` (gerekçesiz) | 400 | **400** `"Gerekçe zorunludur"` |
| 2 | `POST /api/v1/sistem/davetler` (gerekçe 20+) | 201 | **201** (jeton üretildi) |
| 3 | `POST /api/v1/auth/kayit` (davetsiz) | 400 | **400** `"E-posta zorunludur"` |
| 4 | `POST /api/v1/auth/kayit` (davetli) | 201 | **201** `tenantId, accessToken, refreshToken` |
| 5 | `POST /api/v1/auth/yenile` | 200 | **200** (rotasyon çalıştı) |
| 6 | `GET /api/v1/auth/ben` | 200 | **200** `id, tenantId, subeId, ad, soyad, email, rol, arayuzModu, etkinArayuzModu, aktif…` |
| 7 | `GET /css/app.css` | 200 `text/css` | **200 `text/css`** |
| 8 | `GET /yetkiler` | 200 `text/html` (SPA) | **200 `text/html`** |

### 6.2 B-30 canlı kanıt (`/yetkiler`, 375 px)
| Ölçüm | Aydınlık | Karanlık |
|---|---|---|
| `.kart-liste--yapiskan thead th:first-child` `position` | `sticky` | `sticky` |
| `left` | `0px` | `0px` |
| zemin (hesaplanmış) | `rgb(248,250,252)` | `rgb(27,38,52)` |
| `#yetki-matris-sar.scrollLeft = 300` sonrası ROL başlığının ekran `left`i | **13 → 13 (kaymadı)** | **13 → 13** |
| `#yetki-matris-ipucu` görünürlük | `block` · "Tablo yatay kaydırılabilir — parmağınızl…" | `block` |

### 6.3 B-32 canlı kanıt (hesaplanmış renk, gerçek tarayıcı)
| Sınıf | Aydınlık (zemin / metin) | Karanlık (zemin / metin) |
|---|---|---|
| `.ozet-kutu--amber` | `rgb(253,246,231)` / `rgb(30,41,59)` | **`rgb(42,35,19)` / `rgb(232,238,246)`** |
| `tr.satir-vurgu` | `rgb(255,251,235)` / `rgb(30,41,59)` | **`rgb(42,35,19)` / `rgb(232,238,246)`** |
| `.kart--masak` | `rgb(253,246,231)` / `rgb(30,41,59)` | **`rgb(42,35,19)` / `rgb(232,238,246)`** |
| `:root --vurgu-yuzey` | `#fffbeb` | **`#2a2313`** |

Planın işaret ettiği `rgb(255,251,235)` karanlık temada **artık çıkmıyor**.

### 6.4 B-9 — 44/44 ROTA, TEK TEK (BK-24)
| # | Rota | ÖNCE (px) | SONRA aydınlık | SONRA karanlık | ≤375 |
|---|---|---|---|---|---|
| 1 | `/` | **582** | 375 | 375 | ✅ |
| 2 | `/ayarlar` | **595** | 375 | 375 | ✅ |
| 3 | `/banka` | **588** | 375 | 375 | ✅ |
| 4 | `/banka/ekstre/yok` | **624** | 375 | 375 | ✅ |
| 5 | `/bildirimler` | **621** | 375 | 375 | ✅ |
| 6 | `/cariler` | **592** | 375 | 375 | ✅ |
| 7 | `/cikti/SATIS_FISI/yok` | **583** | 375 | 375 | ✅ |
| 8 | `/denetim-izi` | **607** | 375 | 375 | ✅ |
| 9 | `/emanet` | **600** | 375 | 375 | ✅ |
| 10 | `/evraklar` | **585** | 375 | 375 | ✅ |
| 11 | `/evraklar/takvim` | **600** | 375 | 375 | ✅ |
| 12 | `/faturalar` | **611** | 375 | 375 | ✅ |
| 13 | `/fiyatlandirma` | **603** | 375 | 375 | ✅ |
| 14 | `/gider-pusulalari` | **618** | 375 | 375 | ✅ |
| 15 | `/gider-pusulalari/yok/belge` | **606** | 375 | 375 | ✅ |
| 16 | `/giris` | **582** | 375 | 375 | ✅ |
| 17 | `/islemler` | **603** | 375 | 375 | ✅ |
| 18 | `/isletme` | **598** | 375 | 375 | ✅ |
| 19 | `/kasalar` | **598** | 375 | 375 | ✅ |
| 20 | `/kilavuz` | **597** | 375 | 375 | ✅ |
| 21 | `/kullanicilar` | **814** | 375 | 375 | ✅ |
| 22 | `/kur` | **565** | 375 | 375 | ✅ |
| 23 | `/kurulum` | **609** | 375 | 375 | ✅ |
| 24 | `/pos` | **609** | 375 | 375 | ✅ |
| 25 | `/pos/tahsilatlar` | **630** | 375 | 375 | ✅ |
| 26 | `/raporlar` | **607** | 375 | 375 | ✅ |
| 27 | `/raporlar/islem-defteri` | **606** | 375 | 375 | ✅ |
| 28 | `/raporlar/kar-zarar` | **581** | 375 | 375 | ✅ |
| 29 | `/raporlar/kasa` | **628** | 375 | 375 | ✅ |
| 30 | `/raporlar/satis-analizi` | **592** | 375 | 375 | ✅ |
| 31 | `/raporlar/vade-takibi` | **614** | 375 | 375 | ✅ |
| 32 | `/sayim` | **587** | 375 | 375 | ✅ |
| 33 | `/siparisler` | **615** | 375 | 375 | ✅ |
| 34 | `/siparisler/yok` | **615** | 375 | 375 | ✅ |
| 35 | `/urunler` | **600** | 375 | 375 | ✅ |
| 36 | `/urunler/agac` | **583** | 375 | 375 | ✅ |
| 37 | `/urunler/etiketler` | **610** | 375 | 375 | ✅ |
| 38 | `/uyum` | **585** | 375 | 375 | ✅ |
| 39 | `/veri-disa-aktarim` | **606** | 375 | 375 | ✅ |
| 40 | `/veri-ice-aktarim` | **606** | 375 | 375 | ✅ |
| 41 | `/vezne` | **587** | 375 | 375 | ✅ |
| 42 | `/vitrin` | **598** | 375 | 375 | ✅ |
| 43 | `/yedekler` | **609** | 375 | 375 | ✅ |
| 44 | `/yetkiler` | **600** | 375 | 375 | ✅ |

**ÖZET: 44 rota × 2 tema = 88 ölçüm · 375 px'i aşan: 0.**
Önce: 44/44 taşıyordu (en düşük 565, en yüksek 814 px).
Ham veri: `olcumler/P-18-mobil-375-ONCE.json`, `-aydinlik.json`, `-karanlik.json`.

> **Parametreli 4 rota** (`/banka/ekstre/{IthalatId}`, `/cikti/{Tur}/{Id}`,
> `/gider-pusulalari/{Id}/belge`, `/siparisler/{Id}`) yer tutucu kimlikle gezildi; bu
> ekranlar "kayıt bulunamadı" durumunda çizildi. **Dürüstlük notu:** dolu kayıtla çizilen
> hâlleri bu turda ölçülmedi; QA turunda gerçek fikstür kimliğiyle tazelenmelidir
> (`qa-turu.js` artık her rota için `mobil_scroll_width` istiyor).

### 6.5 Temizlik
- API PID durduruldu · `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **0 satır** · **5199 → 0 satır (hiç dokunulmadı)** · geçici 5197 dosya sunucusu da durduruldu (**0 satır**).
- Test kiracısı `information_schema`'dan `tenant_id` kolonu olan **58 tablonun tamamından** silindi + `kiraci_davetleri` satırı silindi.
- Doğrulama: `hareketler=0` · `kur_snapshots=0` · `kullanicilar(p18test)=0` · `kiraci_davetleri(p18test)=0` · **`tenants` = 8 (başlangıçtaki değer)** · `QA-TEST-%` kiracı sayısı **3 — dokunulmadı**.
- **Bilerek silinmedi:** `denetim_kayitlari` (tenant_id='SISTEM') satırları. Denetim izi **append-only**'dir (`P-09: DENETIM_APPEND_ONLY`); davet üretimi kaydını silmek o değişmezi ihlal ederdi. Toplam SISTEM izi: 18 satır.
- Geçici sır dosyaları (`at.txt`, `rt.txt`, `sk.txt`, `p18sifre.txt`, `kayit*.json`, `davet.json`, `yenile*.json`, `ben.json`) `printf '' >` ile boşaltıldı. Hiçbir jeton/parola değeri rapora yazılmadı.

---

## § 7 — DERLEME, TEST, DENETİM

| Ölçüt | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln --nologo` | **0 Uyarı · 0 Hata** (başlangıçta 1 uyarı vardı: `Cariler.razor` RZ2012 — § 3.5'te düzeltildi) |
| Tam süit `dotnet test … --logger trx` | **Toplam 3371 · Geçti 3371 · Başarısız 0 · Atlanan 0** (`trx-pP-18/…_2026-09-21_05_02_26.trx`) |
| Süit büyümesi | P-17 sonu **3357** → **3371** = **+14 yeni test** |
| `dotnet/denetim.sh` | **`DENETİM TEMİZ.` · exit 0 · 168 ✓** |
| Migration | **YOK** (plan: HAYIR; şema değişikliği gerekmedi) — `Migrations/*.cs` sayısı değişmedi |

### Yeni testler (14)
`Kuyumcu.Tests/MobilPariteTestleri.cs`
- `MobilPariteCssTestleri` (8): `B9_Ust_bar_DAR_EKRANDA_sarar_ve_baslik_kuculebilir` · `B9_Kaydirma_kutusu_MUTLAK_KONUMLU_torununu_da_hapseder` · `B9_Tasma_OVERFLOW_HIDDEN_ile_GIZLENMEZ` · `B32_Vurgu_yuzeyleri_HAM_PALET_degil_SEMANTIK_belirtecten_gelir` · `B32_Razor_dosyalarinda_SABIT_RENK_tavani_asilmaz` · `B30_Yapiskan_ilk_sutun_KartListe_uzerinden_soezlesmeye_baglidir` · `B30_Yetkiler_ROL_sutunu_YAPISKAN_isaretlidir` · `B9_Veri_tablolari_TEK_sarmalayici_kalibini_kullanir`
- `KartListeTestleri` (6, bUnit gerçek çizim): `Tabloyu_KAYDIRILABILIR_ve_KLAVYEYLE_ERISILEBILIR_bolgeye_alir` · `Ipucu_metni_SOZLUKTEN_gelir_ciplak_metin_yoktur` · `Baslik_verilirse_ERISILEBILIR_AD_olur_verilmezse_sozlukten_gelir` · `YAPISKAN_ILK_SUTUN_yalniz_istenince_isaretlenir` · `Id_verilmezse_BOS_id_ozniteligi_BASILMAZ` · `Satir_ici_STYLE_uretmez`

> **Testlerin sınırı, dürüstçe:** bu testler CSS kuralının **varlığını** kilitler, **piksel
> ölçmez**. 375 px iddiasının kanıtı § 6.4'teki gerçek tarayıcı tablosudur. Bu sınır test
> dosyasının başına yorum olarak da yazıldı.

---

## § 8 — MEKANİZMA: `denetim.sh` P-18 SAYAÇ BLOĞU

`# >>> P-18` … `# <<< P-18` işaretli blok (PLAN § 7 blok kuralı). **Tabanların hepsi
2026-09-21'de taze ölçüldü, hiçbiri belgeden kopyalanmadı** (PLAN § 1 md.2); her sayacın
ölçüm komutu yorum olarak yazıldı.

```
✓ P-18: MOBIL_OLCUM_BETIGI            mobil-olc.js (gerçek Chrome/CDP, tek tek)
✓ P-18: MOBIL_YATAY_KAYDIRMA          0 / 88 rota-ölçüm (aydınlık + karanlık, tek tek)
✓ P-18: MOBIL_KABUK_SARMA             5/5 (üst bar sarar · başlık küçülür · kutu hapseder)
✓ P-18: MOBIL_TASMA_GIZLEME           0 (taşma gizlenmedi, hapsedildi)
✓ P-18: MOBIL_TABLO_SARMALAYICISIZ    0 elle sarmalayıcı · 9 KartListe kullanımı
✓ P-18: SABIT_RENK                    14 (tavan 14 — cırcır; hepsi Giris paneli)
✓ P-18: TEMA_HAM_PALET_VURGU          0 (vurgu yüzeyi semantik belirteçten)
✓ P-18: YETKILER_ROL_YAPISKAN         razor 1 · css 3
```

**Cırcır (yalnız azalan) tavanlar — hiçbiri artırılmadı:**
- `P-18: SABIT_RENK` tavan **14** (taze ölçüm; P-18 öncesi 31).
- `P-11: SATIR_ICI_STYLE` tavanı **154 → 143** *indirildi* (P-16'nın 154'e indirdiği gibi). P-18 on bir satır içi style'ı sınıfa taşıdı; tavan gerçeğe çekildi.

**`MOBIL_YATAY_KAYDIRMA` neden dosyadan okunuyor — açık sınır:** 375 px'i kabuk betiği
ölçemez; gerçek tarayıcı ölçer (BK-24 zaten "gerçek tarayıcıda tek tek" diyor). `denetim.sh`
sayısal iddiayı **uydurmaz**, ölçüm dosyasından okur ve *ölçüm yoksa da KIRILIR*. Tazelik
QA turu kapısında zorlanır → `qa-turu.js` artık her rota için `mobil_scroll_width` **zorunlu
şema alanı** olarak istiyor ve 375'i aşan rota için `MOBIL_YATAY_KAYDIRMA: <rota> <px>`
başlıklı **KRİTİK** bulgu yazdırıyor; `hazirlik-kurulu.js`'te Aşkın merceği "375'i aşan tek
rota bile `hazir=false`" koşuluna bağlandı (plan § 7: bu iki betik P-18'in dosya listesindedir).

---

## § 9 — KOŞUL KARŞILAMA TABLOSU

| Koşul | Durum | Kanıt |
|---|---|---|
| Plan DoD: 42/42 rotada `scrollWidth ≤ 375`, gerçek tarayıcı, örnekleme yok | ✅ (44/44) | § 6.4, 88 ölçüm, `olcumler/*.json` |
| Plan DoD: karanlık temada vurgu **belirteçten**, sabit renk yok | ✅ | § 6.3 · `SABIT_RENK 31→14` · `TEMA_HAM_PALET_VURGU 0` |
| Plan DoD: `/yetkiler` ROL sütunu sticky | ✅ | § 6.2 canlı kaydırma kanıtı |
| Plan DoD: ölçüm kapsamı `Pages + Bilesenler + Layout` | ✅ | `SABIT_RENK` sayacı ve test bu üç klasörü tarar |
| Plan sayaçları: `MOBIL_YATAY_KAYDIRMA=0` · `SABIT_RENK` (azalan) | ✅ | § 8 (+4 ek mekanizma sayacı) |
| Plan dosyaları: `css/` · `KartListe.razor` (YENİ) · 5 sayfa · `Yetkiler` · `Fiyatlandirma` · `qa-turu.js` · `hazirlik-kurulu.js` | ⚠️ kısmen | `Fiyatlandirma.razor`'a **DOKUNULMADI** — ölçüm gösterdi ki oradaki `rgb(255,251,235)` `tr.satir-vurgu`'dan, yani `--vurgu-yuzey` belirtecinden geliyordu ve karanlıkta zaten `#2a2313` oluyor; dosyada düzeltilecek bir sabit renk **yoktu** (§ 1). Kapsam genişletmemek için dokunulmadı. |
| (a) her iş kuralı için hatayı yeniden üreten test | ✅ | 14 yeni test |
| (b) mutasyon kanıtı (adıyla kırmızı + `cmp`) | ✅ | § 5, 8 mutasyon + 1 kurtarma kontrolü |
| (c) 5198'de canlı doğrulama, 5199'a dokunma | ✅ | § 6; `lsof` 5199 → 0 satır |
| (d) `dotnet/denetim.sh` TEMİZ | ✅ | exit 0, 168 ✓ |
| (e) tam süit 0 başarısız | ✅ | 3371/3371 |
| (f) kendi test kiracısı silindi, `QA-TEST-*` korundu | ✅ | `tenants=8`, `QA-TEST-%=3` |
| (g) uygulama raporu | ✅ | bu dosya |
| Migration: HAYIR | ✅ | şema değişmedi |
| Mevcut element id'leri değişmez; yeni id `<önek>-<alan>` | ✅ | yalnız `*-sar`, `*-ipucu` eklendi |
| Pages içinde yeni istemci `record` açma | ✅ | 0 yeni record |
| Razor özniteliğinde `{ }` lambda yok · `disabled="false"` dizgesi yok | ✅ | KartListe'de lambda yok |
| Yeni CSS'te sabit renk yok, belirteç `var()` | ✅ | P-18 bloğunda 0 hex |
| Satır içi style yok | ✅ | `Satir_ici_STYLE_uretmez` testi · 154→143 |
| DoD EK 1(1): her düğme ya istek atar ya `disabled`+ipucu | ✅ (kapsam dışı) | P-18 **hiç düğme eklemedi**; yalnız düzen/renk |
| DoD EK 1(2): tek düzenleme kalıbı, yeni `ModalPencere` düzenlemesi yok | ✅ | 0 yeni ModalPencere kullanımı |
| DoD EK 2: TEK DOKUNUŞ — dokunulan sayfalar listesi | ✅ | § 10 |
| Ücretsiz pilot: fiyat/teklif/tahsilat metni üretilmedi | ✅ | bu pakette hiç ticari metin yok |
| COMMIT YOK / git komutu yok / `rm`-`mv` yok | ✅ | yalnız `git status --porcelain` okundu |
| Sır/parola/jeton değeri çıktıya yazılmadı | ✅ | tüm jetonlar dosyada kaldı, sonra boşaltıldı |

### DoD sözleşmesi (Bertan koşulu)
P-18 **hiçbir yeni varlık / kayıt türü / uç / DTO üretmemiştir**: ne migration, ne tablo, ne
API ucu, ne denetim kaydı. Tamamı sunum katmanıdır (CSS + bir sarmalayıcı bileşen + sözlük).
Bu yüzden PATCH/DELETE/`[RolGerekli]`/`DenetimServisi` maddeleri **konu dışıdır** — boş
geçilmiş değil, **uygulanamaz**. Dokunulan ekranlardaki veri girişlerinin düzenlenebilirliği
P-06/P-12/P-16'nın sözleşmesidir ve bu pakette **değiştirilmemiştir**; `KartListe` yalnız
sarar, hiçbir eylemi kaldırmaz (testler tablonun içeriğini olduğu gibi geçirdiğini ölçer).

---

## § 10 — DOKUNULAN SAYFALAR (DoD EK 2 · TEK DOKUNUŞ)

`Pages/Cariler.razor` · `Pages/Urunler.razor` · `Pages/Isletme.razor` · `Pages/Banka.razor` ·
`Pages/Islemler.razor` · `Pages/Yetkiler.razor` · `Pages/Raporlar.razor`
(`Raporlar.razor` planın P-18 dosya listesinde yoktu; **gerekçe:** B-32'nin dört satır içi
`color:#…` yaşlandırma rengi oradaydı ve kimlik kapanmadan paket geçemezdi. Kapsam
genişletilmedi — yalnız o dört sınıf değişti.)
`Pages/Fiyatlandirma.razor`: **dokunulmadı** (§ 9 gerekçesi).

Bileşen/Layout: `Bilesenler/KartListe.razor` (YENİ) · `PastaGrafik` · `CubukGrafik` ·
`AlanGrafik` · `MasakFormu`. `Layout/UstBar.razor`'a **dokunulmadı** — düzeltme tamamen CSS'tedir.

---

## § 11 — DEĞİŞEN DOSYALAR

```
M dotnet/Kuyumcu.Web/wwwroot/css/app.css                (P-18 bloğu + .ozet-kutu--amber + .tablo-sar)
M dotnet/Kuyumcu.Core/I18n/Sozlukler.cs                 (TabloBolgesi · TabloKaydirIpucu, TR+EN)
M dotnet/Kuyumcu.Web/Pages/Cariler.razor                (KartListe + Baslik hatası + liste-satiri)
M dotnet/Kuyumcu.Web/Pages/Urunler.razor                (3 × KartListe)
M dotnet/Kuyumcu.Web/Pages/Isletme.razor                (KartListe)
M dotnet/Kuyumcu.Web/Pages/Banka.razor                  (KartListe)
M dotnet/Kuyumcu.Web/Pages/Islemler.razor               (2 × KartListe + 2 ham palet → belirteç)
M dotnet/Kuyumcu.Web/Pages/Yetkiler.razor               (KartListe + YapiskanIlkSutun)
M dotnet/Kuyumcu.Web/Pages/Raporlar.razor               (4 satır içi renk → .yas-* sınıfı)
M dotnet/Kuyumcu.Web/Bilesenler/PastaGrafik.razor       (6 hex + 3 style → belirteç/sınıf)
M dotnet/Kuyumcu.Web/Bilesenler/CubukGrafik.razor       (3 hex → belirteç)
M dotnet/Kuyumcu.Web/Bilesenler/AlanGrafik.razor        (4 hex → belirteç)
M dotnet/Kuyumcu.Web/Bilesenler/MasakFormu.razor        (satır içi ham palet → .kart--masak)
M dotnet/denetim.sh                                      (P-18 bloğu + P-11 tavan 154→143 + § 4 kurtarma)
M dokumanlar/09-uygulama-kayitlari/betikler/qa-turu.js   (mobil_scroll_width şema alanı + KRİTİK kural)
M dokumanlar/09-uygulama-kayitlari/betikler/hazirlik-kurulu.js (Aşkın merceği MOBIL_YATAY_KAYDIRMA)
? dotnet/Kuyumcu.Web/Bilesenler/KartListe.razor          YENİ
? dotnet/Kuyumcu.Tests/MobilPariteTestleri.cs            YENİ (14 test)
? dokumanlar/09-uygulama-kayitlari/betikler/mobil-olc.js YENİ (CDP ölçüm betiği)
? dokumanlar/09-uygulama-kayitlari/olcumler/P-18-mobil-375-ONCE.json       YENİ
? dokumanlar/09-uygulama-kayitlari/olcumler/P-18-mobil-375-aydinlik.json   YENİ
? dokumanlar/09-uygulama-kayitlari/olcumler/P-18-mobil-375-karanlik.json   YENİ
? dokumanlar/09-uygulama-kayitlari/PAKET-P-18-uygulama-raporu.md           YENİ (bu dosya)
```
*(`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısından P-18'e ait olanlar süzüldü;
depodaki diğer `M`/`??` satırları önceki paketlerden gelmektedir, bu pakette dokunulmadı.)*

---

## § 12 — AÇIK KALANLAR / DEVREDİLENLER

1. **Parametreli 4 rota dolu kayıtla ölçülmedi** (§ 6.4 notu). → QA turu 2/3'te gerçek
   fikstür kimliğiyle tazelenmeli; `qa-turu.js` artık bunu zorunlu alan olarak istiyor.
2. **`MOBIL_YATAY_KAYDIRMA` anlık görüntüdür.** `denetim.sh` ölçüm dosyasını okur; dosya
   bayatlarsa sayaç yine `0` der. Tazelik yalnız QA turu kapısında zorlanır. Daha güçlüsü
   (kapıda zorunlu yeniden ölçüm) **P-24 / dalga 4 kapısına devredildi** — orada zaten
   `getBBox` işi için tarayıcı ayağa kalkıyor.
3. **`Pages/Giris.razor`'da 14 sabit renk** duruyor. Tanıtım paneli bilinçli olarak her iki
   temada da koyudur; yine de bu renkler `belirtecler.css`'e `--giris-*` belirteci olarak
   taşınabilir. Kapsam dışı → **1.1'e** (gerekçeli).
4. **Mobil "kart görünümü" (tabloların karta dönmesi) YAPILMADI.** `KartListe` bugün
   sarmalayıcıdır; gerçek kart düzeni DOM değişikliği ve her ekranda sütun önceliği kararı
   ister. Plan DoD'u (≤375 px) **sarmalayıcıyla karşılandı**; kart düzeni **D-11 / 1.1**'e
   bırakıldı. Bileşen adı bu gelecek iş için seçildi.
5. **`dotnet/denetim.sh` sürüm kontrolünde değil** (§ 4). Kurucu kalemi.

---

## § 13 — KURUCUYA / MALİ MÜŞAVİRE NOTLAR

- **Mali müşavire not YOK.** Bu paket mevzuat hükmü içermez; koda sabitlenmiş bir vergi/uyum
  varsayımı üretilmedi.
- **Kurucuya, 2 kalem:**
  1. **`dotnet/denetim.sh` git'te izlenmiyor.** 3.234 satırlık kalite kapısının tek kopyası
     çalışma ağacındadır ve bu pakette bir kez sıfırlandı (tam kurtarıldı, § 4). Sürüm
     kontrolüne alınması kararı sizde (commit yasağı sizin).
  2. **Süre:** tahmin 300 ajan-dk. Gerçekleşen ≈ 330 ajan-dk. Sapmanın tek sebebi § 4'teki
     `denetim.sh` olayının teşhis + kurtarma + doğrulaması (≈40 dk). Asıl iş tahminin
     altında bitti, çünkü ölçüm 44 ayrı sayfa düzeltmesi yerine **tek bir kabuk düzeltmesi**
     gerektiğini gösterdi.

---

## § 14 — ÖZET

Plan "42 rotayı tek tek 375 px'e sığdır" diyordu ve suçluyu tablolarda arıyordu. Gerçek
ölçüm suçlunun **üst bar** olduğunu gösterdi: **44/44 rota** 565-814 px taşıyordu. Tek bir
kabuk kuralı bloğu + `.tablo-sar{position:relative}` ile **44 rota × 2 tema = 88 ölçümün
tamamı 375 px**e indi. `/yetkiler` ROL sütunu canlıda kaydırmada donuk kalıyor, karanlık
temada vurgu yüzeyleri ham paletten değil semantik belirteçten geliyor, razor sabit renk
31 → 14, satır içi style 154 → 143. 14 yeni test, 8 mutasyon kanıtı, 8 yeni cırcır sayaç,
süit **3371/3371**, `DENETİM TEMİZ`, test kiracısı silindi (`tenants` 8).
