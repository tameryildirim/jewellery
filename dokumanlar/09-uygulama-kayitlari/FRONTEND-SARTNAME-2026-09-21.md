# FRONTEND ŞARTNAMESİ — Kuyumcu ERP · 2026-09-21

> **Ajan:** Ertuğ (03 Frontend) · **Tur:** 1 — **SALT OKUNUR**.
> Bu turda **hiçbir kaynak dosya değiştirilmedi**. Sebep: arka planda P-22 uygulama
> dalgası aynı ağaca yazıyor; iki el aynı anda yazarsa çakışır. `dotnet build` /
> `dotnet test` **koşulmadı** (koşan dalganın derlemesi bozulmasın diye). git/commit/rm/mv yok.
> Bu belge **envanter + şartname**dir; uygulama dalga kapısından sonra **tur 2**'de yapılacak.
>
> **Makine okunur envanter:** `scratchpad/frontend-envanter.json` (264 KB, sayfa → alanlar /
> dropdownlar / düğmeler / tablo / bulgular).

---

## § 0. YÖNTEM VE KAPSAM (neyi ölçtüm, neyi ölçemedim)

**Kaynak taraması — TAM.** `dotnet/Kuyumcu.Web` altındaki **83 `.razor` dosyası** okundu:
46 sayfa (`Pages/`), 4 yerleşim (`Layout/`), 33 bileşen (`Bilesenler/`) + `wwwroot/css/app.css`
(1237 satır), `wwwroot/css/belirtecler.css` (375 satır), `Servisler/Bicim.cs`.

Ayrıştırma bir **tırnak/`@(...)` farkında etiket tarayıcısıyla** yapıldı (betik:
`scratchpad/fe/tara2.py`). Bu önemli: naif regex, Razor lambda'sındaki `=>` okunu etiket sonu
sanıp `<button @onclick='() => X()' class="dugme…">` düğmesini "sınıfsız" diye yanlış raporluyordu
(ilk taramada 49 yanlış pozitif vardı, doğru sayı **15**). Rapordaki tüm sayılar düzeltilmiş
tarayıcıdan gelir. Razor yorumu (`@* … *@`) içindeki 2 öğe sayımdan düşüldü.

**Sayılar:** 365 girdi alanı · 176 dropdown · 418 düğme · 82 tablo · 458 `<th>` · 506 `<label>`.

**Tarayıcı turu — KISITLI (dürüst kayıt).**
* Sunucu: `http://localhost:5199` **ayakta**, kendi sekmemde (`tab-4`) çalışıldı, başka sekmeye
  dokunulmadı, 5198'e dokunulmadı.
* **GİRİŞ YAPILAMADI.** `scratchpad/qa-fikstur.json` **0 bayt** (kimlik bilgisi yok);
  `localStorage`/`sessionStorage`/çerez **boş** (ölçüldü). Ayrıca ajan güvenlik kuralı gereği
  parola alanına değer yazmam. `/panel` ve `/urunler` denendi → ikisi de `/giris`'e yönlendirdi.
* Dolayısıyla tarayıcıda **yalnız 2 ekran** ölçülebildi: `/giris` (iki sekmesi ayrı ayrı) ve
  statik `/kilavuz.html`. **Kimlik korumalı 44 ekran = ÖLÇÜLMEDİ (giriş yapılamadı).**
* ⚠ 5199'daki yayın **DALGA 4 anlık görüntüsüdür**; ağaçtaki en yeni ekranlar (ör. `/gider`)
  orada olmayabilir. **Kaynak envanteri 46 sayfayı kapsar, tarayıcı turu yalnız 2 ekranı.**

> ⚠ **BU BÖLÜM 1. GEÇİŞİN KAYDIDIR VE AŞILDI.** Fikstür sağlandıktan sonra 2. geçişte
> **39 rota daha** ölçüldü (`main.ana-icerik` ölçütüyle, 375/768/1440). Güncel ve bağlayıcı
> ölçüm dökümü **§ 7**'dir; hâlâ ölçülemeyen 6 rota § 7.4'tedir. `/gider` ve `/hurda`'nın
> yayında olmadığı **doğrulandı** (§ 7.4).

---

## § 1. BULUNAN TUTARSIZLIKLAR

Toplam **749 bulgu** · 620'si SERBEST dosyalarda, 129'u KİLİTLİ dosyalarda
(kilitli olanlar "→ kilitli" diye işaretli, düzeltme ilgili P-xx paketine devredilir).

### 1.1 DÜĞMELER — aynı işi yapan **üç ayrı sınıf sistemi** (en büyük tutarsızlık)

`app.css` iki neredeyse aynı düğme sistemi tanımlıyor:
* `.btn` + `.btn-birincil/-ikincil/-tehlike/-kucuk/-buyuk/-tam` → `app.css:163-189`
* `.dugme` + `.dugme-birincil/-ikincil/-tehlike/-kucuk` → `app.css:197-212`

Gövdeleri satır satır aynı (`inline-flex; gap:.5rem; font-size:.875rem; padding:.5rem 1rem;
border-radius:var(--radius-md)`), **tek fark**: çıplak `.dugme` kendine zemin+kenarlık veriyor
(ikincil gibi görünür), çıplak `.btn` şeffaf kalıyor. Yani aynı görev iki adla, üstelik
çıplak hâlleri **farklı** görünüyor.

| Sistem | Düğme | Dosya | Örnek |
|---|---:|---:|---|
| `.btn …` | 247 | 27 | `Pages/Banka.razor`, `Pages/Kasalar.razor`, `Pages/Ayarlar.razor` → kilitli |
| `.dugme …` | 94 | 26 | `Pages/Uyum.razor`, `Pages/Evraklar.razor`, `Pages/Gider.razor` |
| üçüncü sistem (`.btn`/`.dugme` yok) | 62 | 20 | `class="ikincil"` **28 kez** — `Pages/Vezne.razor:135`, `Pages/SiparisDetay.razor:120`, `Bilesenler/KabukHatasi.razor:32` → kilitli |
| **hiç sınıf yok** (ham tarayıcı düğmesi) | 15 | 5 | `Layout/KenarCubugu.razor:61` (Çıkış Yap), `Pages/Vezne.razor:51,154,204,343,425`, `Pages/SiparisDetay.razor:75,135,209,304,332` → kilitli değil / `Pages/Siparisler.razor:70,240` → kilitli |

**Aynı dosyada iki sistem birden:** `Layout/UstBar.razor`, `Pages/Cariler.razor`,
`Pages/Urunler.razor` (3 dosya).

**Diğer düğme bulguları**
* `type` özniteliği olmayan düğme: **273/418**. — *Dürüst not:* uygulamada yalnız **4 adet
  `<form>`** var ve bunların **hiçbirinde** `type`'sız düğme yok (ölçüldü: 0/273). Yani bu
  **işlevsel risk değil, yalnız tutarlılık borcudur**; düşük öncelikli.
* Aynı iş için farklı etiket: "Kaydet" (24 yer) · "Ekle" (`Pages/Gider.razor:196`) ·
  "Onayla" (9 yer) · "Evet" (`Pages/Kurulum.razor:375`) hepsi *kaydetme/onaylama* eylemi.
  "İptal" 17 yer + modal kapatma için ayrıca `&times;` (`Bilesenler/Cekmece.razor:28`).
* Yükleniyor durumu **var ama iki biçimde**: `disabled="@…"` 234 yerde, metin değişimi
  (`@(Kaydediliyor ? … : …)`) 93 yerde, `<Yukleniyor>` bileşeni 63 yerde. `aria-busy`
  **hiçbir yerde yok (0)**.
* `disabled` gerekçesi yazılı olan kalıp **kuralda var ama yaygın değil**:
  `Dil.T.Yonetim.KilitSebebi` (`Pages/Giris.razor:137`) ve `Bilesenler/KayitSecici.razor`
  başlığındaki "HER DÜĞME YA İSTEK ATAR YA DISABLED + SEBEP (DoD EK md.1 / UX-S30)" sözleşmesi.
  Buna karşılık 234 `disabled` kullanımının kalanında gerekçe metni yok.

### 1.2 DROPDOWN'LAR — boş seçenek metni **dört farklı dil**

176 `<select>`; sınıf dağılımı: `.secim` 152, `.girdi` **23** (yanlış sınıf — girdi sınıfı
select'e verilmiş: `Pages/Evraklar.razor:50,217,259`, 3 serbest + 3 kilitli dosya).

| Boş seçenek | Adet | Kanıt |
|---|---:|---|
| **hiç boş seçenek yok** | 65 | `Bilesenler/BelgeYukleyici.razor:23`, `Bilesenler/MasakFormu.razor:24`, `Pages/Banka.razor:252` |
| `—` (tek tire, anlamsız) | 54 | `Bilesenler/FiyatListesiUrunPaneli.razor:47`, `Pages/Evraklar.razor:259,321` |
| `"… Seçin"` i18n anahtarı | ~30 | `Dil.T.Cari.CariSecin` (4), `Dil.T.Islem.KasaSecin` (4), `Dil.T.Urun.UrunSecin` (3), `Dil.T.Islem.SubeSecin` (2), `Dil.T.Kullanici.SubeSecin` (2) |
| `"Tümü"` (süzgeç anlamı) | ~11 | `Dil.T.Ortak.Tumu` (5), `Dil.T.Evrak.Tumu`, `Dil.T.Denetim.TumEylemler/TumVarliklar/TumSubeler` |
| melez | 2 | `— (@Dil.T.Fiyat.Varsayilan)` — `Pages/Cariler.razor:347,491` |

* **Arama/filtreleme yeteneği olan dropdown: 0.** Uzun listeler (ürün, cari) ham `<select>`.
  Ayrı bir `Bilesenler/KayitSecici.razor` (arama yapabilen seçici) **var** ama select'lerin
  yerine sistematik kullanılmıyor.
* Seçenek sıralaması hiçbir yerde açıkça sıralanmıyor (API'den gelen sıra kullanılıyor).

### 1.3 GİRDİLER

* **Tip dağılımı:** `text` (tipsiz) 144 · `number` 112 · `checkbox` 35 · `date` 31 ·
  `password` 11 · `email` 7 · `search` 7 · `tel` 2.
* **`number` girdilerinde ölçü birliği yok:** `step=0.01` 53 · `step=0.001` 27 · `step=0.0001` 8 ·
  `step=0.000001` 4 · `step=1` 5 · **step yok 13** (`Pages/Bildirimler.razor:142,152,163`).
  `min` yok: **29** (`Bilesenler/FiyatListesiUrunPaneli.razor:82,197,201`). `max` yok: 89/112.
  Yani *para* (2 hane), *gram* (3 hane), *milyem/has* (4-6 hane) ayrımı **dosyadan dosyaya değişiyor**,
  merkezî bir kural yok.
* **Etiket bağlanmamış alan: 43** (506 label'ın 468'inde `for=` var, geri kalanı boşta).
  Kanıt: `Bilesenler/GlobalArama.razor:43`, `Pages/Fiyatlandirma.razor:56,66,76`,
  `Pages/PosTahsilatlar.razor:25,32,33`, `Pages/Islemler.razor:110`,
  `Pages/Ayarlar.razor:232` → kilitli.
* **Placeholder etiket yerine kullanılmıyor** (iyi): yalnız 28 girdide placeholder var ve
  hepsinde ayrıca `<label>` da var (`Pages/Giris.razor:152` `"ornek@isletme.com"`,
  `Pages/Giris.razor:172` `"Altın Dünyası Kuyumculuk"`). **Ancak** placeholder'lar örnek değer,
  ipucu ve talimat arasında gidip geliyor — tek kalıp yok. Aynı dosyada iki şifre alanının
  placeholder'ı bile farklı kaynaktan: `Pages/Giris.razor:158` sabit `"••••••••"`,
  `Pages/Giris.razor:198` i18n `@Dil.T.Auth.SifrePlaceholder`. `Pages/Giris.razor:172`
  ise **çevrilmemiş sabit Türkçe metin** (i18n dışı).
* **Zorunluluk göstergesi YOK denecek kadar dağınık:** `aria-required` **0 kez**;
  `class="zorunlu"` **0 kez**; label sonunda `*` yalnız **2 kez** (`Pages/GiderPusulalari.razor`);
  HTML `required` 14 kez / 4 dosya; "Zorunlu" kelimesi 94 kez / 21 dosya ama **hata metni olarak**,
  alan etiketinde değil. Yani kullanıcı alanı **doldurmadan önce** neyin zorunlu olduğunu göremiyor.
* **`.girdi` sınıfı kullanımı iyi** (314/349), 35 girdide sınıf yok.
* **Hata gösterimi tutarlı ve iyi** (korunmalı): `<HataBandi>` 52 dosyada (yalnız
  `Pages/Kilavuz.razor` kullanmıyor), `<AlanHatasi>` 18 dosyada, `role="alert"` 37 kez,
  `.girdi.hatali` kırmızı kenarlık (`app.css:231`), `.alan-hata` alan altı metin (`app.css`).
  Boşluk: `AlanHatasi` yalnız 18 dosyada — 365 alanın çoğunda alan-altı hata yeri **yok**.

### 1.4 TABLOLAR

* **Sınıf üç başlı:** `veri-tablosu` 69 · `tablo` 9 · `belge-tablo` 4 (+`pano-tablo` ek sınıf).
  `app.css:259-266` yalnız `.veri-tablosu`'yu biçimlendiriyor; `.tablo` sınıfı için
  ayrı bir temel kural yok → `Pages/Vitrin.razor:57,98,125` tabloları farklı görünür.
* **Sayı hizası:** doğru araç **var** — `app.css:270-271`
  `.sayi { font-variant-numeric: tabular-nums; text-align: right; }`. Ama **458 `<th>`'nin
  yalnız 32'sinde** `class="sayi"` var. Tablosu olduğu hâlde `.sayi` sınıfını **hiç
  kullanmayan 34 dosya**: `Pages/Cariler.razor` (TL Bakiye, Has Gram sütunları sola dayalı),
  `Pages/Vezne.razor`, `Pages/Hurda.razor`, `Pages/Isletme.razor`, `Pages/Kullanicilar.razor`,
  `Pages/DenetimIzi.razor`, `Pages/Uyum.razor`, `Pages/Yetkiler.razor`,
  `Pages/GiderPusulalari.razor`, `Pages/PusulaBelgesi.razor` + `Pages/KasaRaporu.razor` → kilitli.
* **Para/gram biçimi:** merkezî `Servisler/Bicim.cs` **var ve iyi** (`Tl` → `C2` tr-TR = `₺` +
  binlik ayraç; `Para(n, paraBirimi)` → USD/EUR/GBP; `Gram` → `F3 g`; `Has` → `F4 has`;
  `Miktar(n, birim)`). `Bicim.Para` 73 kez / 15 dosya, `Bicim.Gram` 30 kez / 8 dosya.
  **Ama 46 yerde sayı biçimlendirilmeden basılıyor** (ham `271200.00` riski), 20 dosyada:
  `Pages/Vezne.razor:120,125,131,311` (`@k.Adet`, `@k.BirimFiyat`, `@k.Iscilik`, `@b.Tutar`),
  `Pages/Siparisler.razor:185,189,267` → kilitli, `Pages/VadeTakibi.razor:54,56,60,94`,
  `Pages/Cariler.razor:131,145` (`@c.TlBakiye`, `@c.HasGramBakiye`),
  `Pages/Banka.razor:88,90`, `Pages/Kasalar.razor:86,87`, `Pages/Evraklar.razor:88,90`,
  `Pages/Kurulum.razor:197,210`, `Pages/SiparisDetay.razor:116,119`,
  `Pages/Pos.razor:104-106` → kilitli, `Pages/KarZarar.razor:78,102,252` → kilitli,
  `Pages/SatisAnalizi.razor:109,156,206,212` → kilitli.
  Ayrıca `ToString("N2")` (`Pages/Panel.razor`) ve `ToString("N0")` ×4 (`Pages/KarZarar.razor`)
  `Bicim`'i **atlıyor** — para birimi simgesi düşüyor.
* **Yatay taşma koruması eksik:** `.tablo-sar { overflow-x:auto }` (`app.css:258`) **var**,
  ama **12 dosyada tablo bu sarmalayıcı olmadan** duruyor: `Pages/Banka.razor`,
  `Pages/Cariler.razor`, `Pages/Gider.razor`, `Pages/Islemler.razor`, `Pages/Isletme.razor`,
  `Pages/Kurulum.razor`, `Pages/Urunler.razor`, `Pages/Vezne.razor`, `Pages/Vitrin.razor`,
  `Pages/Yetkiler.razor`, `Pages/PusulaBelgesi.razor`, `Bilesenler/Yazdirma/A4Belge.razor`.
  `app.css:825` dar ekranda `.veri-tablosu { min-width: max-content }` diyor — sarmalayıcı
  yoksa bu **doğrudan sayfa taşması** demektir.
* **Boş durum iyi:** `<BosDurum>` 42 dosyada, 78 kez. Tablosu olup `<BosDurum>` kullanmayan
  6 dosya: `Pages/PusulaBelgesi.razor`, `Pages/UrunEtiketleri.razor`, `Pages/Yetkiler.razor`,
  `Pages/Isletme.razor`, `Bilesenler/Yazdirma/A4Belge.razor` + `Pages/Sayim.razor` → kilitli.
  Ayrıca 10 dosyada `<BosDurum>` yerine elle yazılmış "kayıt yok" metni var.

### 1.5 SABİT RENK / SABİT ÖLÇÜ

**Genel tablo iyi** — belirteç sistemi (`belirtecler.css`) gerçekten uygulanmış.
Satır içi `style="…"` içinde sabit renk/px/font kullanan **yalnız 7 yer** var:

| Dosya:satır | Ne |
|---|---|
| `Pages/Giris.razor:19` | `style="position:absolute;inset:0;…"` + SVG içinde ham hex (`#fcd34d`, `#f59e0b`, `#92400e`, `#dbeafe`, `#93c5fd`) — illüstrasyon, mazur görülebilir ama tema değişince uyumsuz |
| `Pages/Giris.razor:69` | `background:var(--amber-400)` (ham palet, semantik değil) + `font-size:1.5rem` |
| `Pages/Panel.razor` ×3 | `min-width:320px`, `min-width:260px` sabit px (`Panel.razor:88,101`) |
| `Pages/Raporlar.razor` ×2 → kilitli | `min-width:320px` (`Raporlar.razor:167,178`) |

Ayrıca **tek bir satır içi `display:grid`** var ve tam da o, § 2'deki kanıtlı bozulmayı
üretiyor: **`Pages/Giris.razor:175`**. Diğer **26 dosya** paylaşılan `.form-grid-2/-3` sınıfını
kullanıyor (`app.css:238` tanım, `app.css:569` ve `:824` altında 768px'te tek sütuna iniyor) —
yani kural var, bu tek yer kurala uymuyor.

**Kırılma noktaları tutarsız:** `app.css` içinde `480px` (3 kez), **`720px` (1 kez —
`app.css:137`, `.kur-bandi/.son-sorgu` gizleniyor)**, `768px` (5 kez), `1024px` (1 kez).
`720` tek başına kalmış, 768 olmalı.

### 1.6 ODAK VE DOKUNMA HEDEFİ (ölçülen değerler)

* **İki odak sistemi çakışıyor:** `app.css:230` `.girdi:focus, .secim:focus { outline: 2px solid
  var(--vurgu); border-color: transparent }` — **fareyle tıklayınca da** halka çiziyor ve
  kenarlığı siliyor. Buna karşılık `app.css:689-704` tek kaynak olarak `:focus-visible`
  grubunu tanımlıyor (`.btn, .dugme, .girdi, .secim, .sekme, …`). Aynı öğe için iki kural.
* **Dokunma hedefi belirteci var, kullanımı yok:** `--dokunma-hedefi: 2.75rem` (44px)
  `belirtecler.css:157`. `app.css` içinde **yalnız 4 yerde** kullanılıyor (`755`, `779`,
  `801-802`). Temel kontroller bunu **kullanmıyor**: `.btn`/`.dugme` `padding:.5rem 1rem` +
  `font-size:.875rem`, `.girdi`/`.secim` `padding:.5rem .75rem`.
  **Tarayıcıda ölçüldü** (`/giris`, 375px): `.girdi` yüksekliği **35px**, `.pill` sekmeler
  **33px**, dil değiştirme düğmeleri **32×23px** — üçü de 44px'in altında.
  Ayrıca `belirtecler.css:350` ve `:374` "sık/yoğun" modda bunu **2rem (32px)**'e düşürüyor.

---

## § 2. ÇÖZÜNÜRLÜK BOZULMALARI (ölçülen)

> **GÜNCELLEME (aynı gün, 2. geçiş):** fikstür sağlandıktan sonra kimlik korumalı ekranlar da
> ölçüldü. **Toplam ölçülen: 41 ekran** (`/giris` iki sekmesi + `/kilavuz.html` + kimlik
> gerektiren 39 rota) × 3 genişlik. Tam döküm **§ 7**'dedir; aşağıdaki tablo ilk geçişin
> (giriş yapılmadan ölçülen) kaydıdır ve değiştirilmemiştir.

Ölçüm yöntemi: her genişlikte `document.documentElement.clientWidth` (gerçek görünüm alanı) ile
`document.documentElement.scrollWidth` karşılaştırıldı; taşan öğeler
`getBoundingClientRect().right > clientWidth+1` ile listelendi. ⚠ § 7'de bu ölçüt
**yetersiz** bulundu: uygulama kabuğunda `.ana-icerik { overflow: auto }` (`app.css:122`)
olduğu için taşma `document`'ta değil `<main>` içinde oluşur — § 7 bu yüzden
`main.ana-icerik.scrollWidth` ile de ölçer.

| Sayfa | Genişlik | clientWidth | scrollWidth | Yatay kaydırma | Taşan öğe / not |
|---|---:|---:|---:|---|---|
| `/giris` — *Giriş Yap* sekmesi | 375 | 375 | **375** | yok | temiz · 6 dokunma hedefi < 40px |
| `/giris` — *Giriş Yap* sekmesi | 768 | 768 | **768** | yok | temiz |
| `/giris` — *Giriş Yap* sekmesi | 1440 | 1440 | **1440** | yok | temiz (sol marka paneli 1024px'ten sonra açılıyor) |
| **`/giris` — *İşletme Kur* sekmesi** | **375** | **375** | **420** | **VAR — 45px** | `DIV.giris-form-sutun` 420px · `DIV.giris-form-sar` 420px · `DIV.dil-secici` sağ kenarı 404px'te. Ekran görüntüsünde *Soyad* alanı ve *İşletme Adı* girdisinin sağı kesiliyor. |
| `/giris` — *İşletme Kur* sekmesi | 1440 | 1440 | **1440** | yok | temiz |
| `/kilavuz.html` (statik) | 375 | 375 | **375** | yok (sayfa kaymıyor) | ama `TABLE.mini` sağ kenarı **402px** → kapsayıcıyı 27px aşıyor ve **kırpılıyor**; son sütuna erişilemiyor. 43 dokunma hedefi < 40px. |
| `/kilavuz.html` | 768 | 753 | 753 | yok | temiz |
| `/kilavuz.html` | 1440 | 1425 | 1425 | yok | temiz |

**Kök neden — `/giris` İşletme Kur, 375px (kanıtlı):**
`Pages/Giris.razor:175` → `<div style="display:grid;grid-template-columns:1fr 1fr;gap:.75rem">`
(Ad / Soyad satırı). Izgara öğelerinin **otomatik asgari boyutu `min-content`**'tir; her
`<input class="girdi">` kendi doğal genişliğinin altına inemez. Tarayıcıda ölçülen hesaplanmış
değer: `grid-template-columns: 180px 180px` → 180+180+12 = **372px**, üstüne
`.giris-form-sar` dolgusu (`app.css:377`, `padding: 0 1.5rem 3rem` = 24+24) → **420px**.
İki düzeltme de tektir: ya `minmax(0,1fr)` / öğelere `min-width:0`, ya da zaten var olan
`.form-grid-2` sınıfını kullanmak (768px'te tek sütuna iniyor).

### İlk geçişte ölçülemeyenler — **ÇÖZÜLDÜ, bkz. § 7**

İlk geçişte 44 ekran `scratchpad/qa-fikstur.json` 0 bayt olduğu için ölçülememişti.
İkinci geçişte **39'u ölçüldü** (§ 7). Hâlâ ölçülemeyen **6 rota** ve sebepleri § 7.4'tedir:
`/hurda` ve `/gider` (5199 yayınında yok) + 4 parametreli rota (boş kiracıda gerçek kimlik yok).

---

## § 3. TEK TİP KURALLARI (ŞARTNAME) — bundan sonra HER ekranda geçerli

### 3.1 Etiket ve placeholder
1. Her girdi/dropdown/textarea'nın **görünür `<label for="…">`**'ı olacak. `aria-label` yalnız
   ikon-düğme ve arama kutusu gibi etiketi görsel olarak gereksiz olan yerlerde; o zaman da
   `id` zorunlu.
2. **Placeholder etiketin yerine geçmez.** Placeholder yalnız **örnek değer** için kullanılır ve
   `"örn. 1.250,00"` kalıbıyla yazılır. Talimat/ipucu placeholder'a değil, alanın **altına**
   `<span class="alan-ipucu">` ile yazılır.
3. Etiket metni **büyük harfle başlar, iki nokta yoktur**: `İşletme Adı` (✓), `işletme adı:` (✗).

### 3.2 Zorunluluk
4. Zorunlu alan etiketinde **`<span class="zorunlu" aria-hidden="true">*</span>`** ve girdide
   `aria-required="true"`. Her formun üstünde bir kez: `* işaretli alanlar zorunludur.`
5. İsteğe bağlı alan **işaretlenmez** (zorunlu olanı işaretlemek tek yönlüdür).

### 3.3 Hata
6. Hata metni **alanın hemen altında**, `<AlanHatasi Alan="…" Durum="form" />` ile, `.alan-hata`
   sınıfında (12px, `--tehlike`). Girdi `.hatali` alır.
7. Dil: **Türkçe, ikinci tekil şahıs yok, suçlayıcı değil.** Kalıp: `<Alan adı> <ne gerekiyor>.`
   Örnek: `Tutar 0'dan büyük olmalı.` / `E-posta geçerli değil.` `Hata: …` öneki yasak.
8. Sayfa/işlem düzeyi hata **`<HataBandi>`** ile, `role="alert"` taşıyarak, formun **üstünde**.
   Tarayıcının kendi balonu kapalı kalır (`novalidate` korunur).

### 3.4 Sayı / para / gram
9. **Ekrana basılan hiçbir sayı ham olmayacak.** Tek kaynak `Servisler/Bicim.cs`:
   * para → `Bicim.Para(tutar, paraBirimi)` (TRY'de `₺1.234,56`)
   * gram → `Bicim.Gram(g)` → `1,250 g`
   * has → `Bicim.Has(h)` → `0,9160 has`
   * miktar+birim → `Bicim.Miktar(n, birim)`
   * `ToString("N2"/"N0")` doğrudan **yasak** (para birimi düşer).
   * makine bağlamı (SVG, `input type=number`, sorgu dizesi, JSON) → yalnız `Bicim`'in makine
     biçimi; kullanıcıya gösterilen yerde asla.
10. **Sayısal sütun her zaman sağa dayalı** ve `.sayi` sınıfını alır (`th` ve `td` birlikte) —
    `tabular-nums` bu sınıftan gelir. Metin sütunu sola, durum/rozet sütunu ortaya.
11. `input type="number"` adım/asgari değeri **veri türüne göre sabittir**:
    | Tür | step | min | inputmode |
    |---|---|---|---|
    | Para (TL/döviz) | `0.01` | `0` | `decimal` |
    | Gram | `0.001` | `0` | `decimal` |
    | Milyem / has | `0.0001` | `0` | `decimal` |
    | Adet | `1` | `0` (veya `1`) | `numeric` |
    | Yüzde | `0.01` | `0`, `max="100"` | `decimal` |
    | Kur | `0.0001` | `0` | `decimal` |
12. Serbest metin girdisi **daima `maxlength`** taşır (ad/unvan 100, açıklama 500, kod 32).

### 3.5 Dropdown
13. Tek sınıf: **`.secim`** (`.girdi` bir select'e verilmez).
14. **Boş seçenek metni tek kalıp:**
    * Zorunlu seçim → `<option value="">Seçiniz</option>`
    * Süzgeç → `<option value="">Tümü</option>`
    * İsteğe bağlı alan → `<option value="">Belirtilmedi</option>`
    * **`—` tek başına yasak.**
15. Seçenekler **Türkçe alfabetik** sıralanır; anlamlı doğal sıra varsa (durum akışı, tarih)
    o sıra korunur ve bu bir yorumla belgelenir.
16. **10'dan fazla seçenek** beklenen yerde ham `<select>` değil, arama yapabilen
    `Bilesenler/KayitSecici.razor` kullanılır (ürün, cari, kullanıcı seçimleri).

### 3.6 Düğme
17. **Tek sistem: `.btn`.** `.dugme*` ve çıplak `.ikincil` **kapatılır** (geçiş için
    `app.css`'te takma ad verilir, kaynaktaki kullanımlar kademeli `.btn`'e çevrilir).
    Sınıfsız `<button>` kalmaz.
18. Hiyerarşi ve **ekran başına en fazla bir birincil**:
    * `btn btn-birincil` → ekranın ana eylemi (Kaydet / Onayla / Tahsil Et)
    * `btn btn-ikincil` → İptal, Geri, Yenile, ikincil eylemler
    * `btn btn-tehlike` → Sil, Storno, Pasife Al (geri dönüşü zor olan)
    * `btn btn-kucuk` → tablo satırı içi eylemler
19. **Adlandırma sözlüğü (tek dil):**
    | Eylem | Etiket |
    |---|---|
    | Yeni kayıt açma (modal açar) | `Yeni <Nesne>` |
    | Formu kaydetme (yeni ve düzenleme) | `Kaydet` |
    | Onay diyaloğunda kabul | `Onayla` |
    | Vazgeçme | `İptal` |
    | Silme | `Sil` |
    | Listeyi tazeleme | `Yenile` |
    | Satır içi detay | `Detay` |

    "Ekle", "Oluştur", "Evet", "Tamam" **kullanılmaz**.
20. Her düğmede `type="button"` (veya form içinde bilinçli `submit`) bulunur.
21. **Yükleniyor:** düğme metni değişmez; `disabled` + `aria-busy="true"` + düğme içinde
    `<Yukleniyor Kucuk="true" />`. Metin değiştirme kalıbı (`Kaydediliyor…`) tek biçime indirilir:
    ya hepsi metin değiştirir ya hiçbiri — **karar: hiçbiri, dönen gösterge kullanılır**
    (metin değişimi düğme genişliğini oynatıyor).
22. **`disabled` gerekçesi zorunlu:** pasif düğmenin yanında/altında
    `<span class="metin-soluk metin-kucuk">Kilit sebebi: …</span>` bulunur
    (`Pages/Giris.razor` kalıbı tüm ekranlara yayılır).

### 3.7 Tablo
23. Tek sınıf **`.veri-tablosu`**; `.tablo` ve `.pano-tablo` `.veri-tablosu`'nun değişkenine
    dönüştürülür. `.belge-tablo` (kâğıt çıktısı) ayrı kalır — kâğıt temadan muaftır.
24. **Her tablo `<div class="tablo-sar">` içinde**. İstisnasız (dar ekranda
    `min-width:max-content` var; sarmalayıcı yoksa sayfa taşar).
25. Başlık hizası gövde hizasıyla aynı (`.sayi` th + td birlikte).
26. **Boş durum:** her liste `<BosDurum>` kullanır. Metin kalıbı:
    `<Nesne> bulunamadı.` + tek satır ne yapılacağı + (varsa) birincil eylem düğmesi.
    Süzgeç sonucu boşsa: `Seçili süzgeçle <nesne> bulunamadı.` + `Süzgeçleri temizle`.

### 3.8 Odak, dokunma, kırılma noktaları
27. **Tek odak kuralı:** yalnız `:focus-visible`, `outline: var(--odak-halka-kalinlik) solid
    var(--odak-halka); outline-offset: var(--odak-halka-boslugu)`. `:focus` üzerine yazan
    kural (`app.css:230`) kaldırılır; `border-color: transparent` yapılmaz (kenarlığın
    kaybolması yer oynatıyor).
28. **En küçük dokunma hedefi 44×44px** (`--dokunma-hedefi`). `.btn`, `.dugme-benzeri`,
    `.girdi`, `.secim`, `.sekme`, ikon düğmeleri **`min-height: var(--dokunma-hedefi)`** alır.
    `btn-kucuk` yalnız masaüstünde küçülür; `≤768px`'te 44px'e döner.
    Yoğun modun 2rem (32px) değeri **yalnız masaüstünde** geçerli olur.
29. **Kırılma noktaları tam olarak üçtür: 375 (telefon) · 768 (tablet) · ≥1024 (masaüstü).**
    `720px` sorgusu 768'e taşınır. Yeni kırılma noktası açılmaz.
30. **Hiçbir ekranda yatay sayfa kaydırması olmayamaz**: her genişlikte
    `documentElement.scrollWidth <= clientWidth`. Geniş içerik (tablo, sekme şeridi) **kendi
    kabında** kaydırılır.
31. Sabit `px` genişlik/`min-width` satır içi `style`'da yasak; iki sütunlu form için **daima**
    `.form-grid-2` (768'de tek sütun). Serbest ızgara gerekirse
    `repeat(auto-fit, minmax(min(100%, 16rem), 1fr))`.
32. Renk ve ölçü **yalnız `belirtecler.css` semantik belirteçlerinden** gelir
    (`--vurgu`, `--metin`, `--yuzey`, `--bosluk-*`, `--yazi-*`). Ham palet (`--amber-400`) ve
    hex doğrudan kullanılmaz. **Kâğıt (`@media print`, `.belge*`, `.urun-etiket*`) muaftır.**

---

## § 4. UYGULAMA PLANI — İKİ LİSTE

### 4.1 SERBEST — 36 ekran (tur 2'de dokunulacak)

`Banka · BankaEkstre · Bildirimler · Cariler · Cikti · DenetimIzi · Emanet · EvrakTakvimi ·
Evraklar · Fiyatlandirma · Gider · GiderPusulalari · Giris · Hurda · IslemDefteri · Islemler ·
Isletme · Kasalar · Kullanicilar · Kur · Kurulum · Panel · PosTahsilatlar · PusulaBelgesi ·
SiparisDetay · UrunAgaci · UrunEtiketleri · Urunler · Uyum · VadeTakibi · VeriDisaAktarim ·
VeriIceAktarim · Vezne · Vitrin · Yedekler · Yetkiler`

Bu 36 ekranda **620 bulgu** var. En yüklü beş dosya:
`Pages/Urunler.razor` (58) · `Pages/Cariler.razor` (51) · `Pages/Islemler.razor` (47) ·
`Pages/Vezne.razor` (39) · `Pages/Banka.razor` (36).

Ortak parçalar da serbesttir ve **önce** bunlar düzeltilir:
`wwwroot/css/app.css`, `wwwroot/css/belirtecler.css`, `Layout/AnaYerlesim.razor`,
`Layout/UstBar.razor`, `Layout/KenarCubugu.razor`, `Layout/DilSecici.razor` ve
`Bilesenler/` altındaki **`HataBandi.razor` ve `KabukHatasi.razor` dışındaki 31 bileşen**.

### 4.2 KİLİTLİ — DOKUNMA (10 ekran + 2 bileşen)

`Ayarlar · Faturalar · KarZarar · KasaRaporu · Kilavuz · Pos · Raporlar · SatisAnalizi ·
Sayim · Siparisler` + `Bilesenler/HataBandi.razor` + `Bilesenler/KabukHatasi.razor`

Bu dosyalarda **129 bulgu** tespit edildi; hepsi `frontend-envanter.json` içinde
`"kilitli": true` ile işaretli. **Tur 2'de bu dosyalara yazılmayacak**; bulgular ilgili
P-xx paketine devredilir. Özeti (→ kilitli — P-xx'e devredildi):

* `Pages/Faturalar.razor` (31) — 4 tablo, `.sayi` kısmi, 10 select, boş seçenek karışık.
* `Pages/Siparisler.razor` (30) — 2 sınıfsız düğme (`:70`, `:240`), biçimsiz sayı `:185,189,267`.
* `Pages/Ayarlar.razor` (~20) — 20 düğme hepsi `.btn`, etiket bağlanmamış `:232`.
* `Pages/Raporlar.razor` — 7 tablo, 1'i `.tablo-sar`'sız, sabit `min-width:320px` ×2.
* `Pages/SatisAnalizi.razor` — 3 sınıfsız düğme (`:39,42,45`), 4 biçimsiz sayı.
* `Pages/KarZarar.razor` — `ToString("N0")` ×4 (para birimi düşüyor), biçimsiz `:78,102,252`.
* `Pages/Pos.razor` — 4 sınıfsız düğme, biçimsiz `:104-106`, 1 tablo sarmalayıcısız.
* `Pages/KasaRaporu.razor` — `.sayi` var, 1 sınıfsız düğme `:109`.
* `Pages/Sayim.razor` — tablosu var, `<BosDurum>` yok.
* `Pages/Kilavuz.razor` — `<HataBandi>` kullanmayan tek sayfa (statik `kilavuz.html` sarar);
  `kilavuz.html`'de 375px'te `TABLE.mini` kırpılması var (§ 2).
* `Bilesenler/KabukHatasi.razor:32` — `class="ikincil"` üçüncü sistem düğme.
* `Bilesenler/HataBandi.razor:29` — `.btn` sistemi; dokunulmayacak.

### 4.3 Çakışma önleme kuralı (tur 2 için)
`wwwroot/css/app.css` **tek ortak yazma noktasıdır** ve P-22 dalgası da oraya yazabilir.
Tur 2'ye başlamadan önce `app.css`'in dalga kapısı sonrası hâli yeniden okunacak; CSS
değişiklikleri dosyanın **sonuna tek blok** olarak ("UX-TEKTİP — 2026-09-21") eklenecek,
mevcut satırlar mümkün olduğunca **üzerine yazılmak yerine geçersiz kılınacak**. Razor
dosyalarında yalnız yukarıdaki SERBEST listeye dokunulacak.

---

## § 5. ÖNCELİK SIRASI (en çok ekranı tek hamlede düzelten önce)

| # | İş | Nerede | Etki |
|---:|---|---|---|
| 1 | **Dokunma hedefi + kontrol yüksekliği**: `.btn`, `.dugme`, `.girdi`, `.secim`, `.sekme`, `.pill`, ikon düğmelerine `min-height: var(--dokunma-hedefi)`; yoğun mod 32px'i yalnız masaüstüne kilitle | `app.css`, `belirtecler.css` | **46 ekranın hepsi** (ölçülen 3 ihlal: 35px girdi, 33px sekme, 32×23px dil düğmesi) |
| 2 | **Tek odak kuralı**: `app.css:230` `:focus` kuralını kaldır, yalnız `:focus-visible` grubu kalsın | `app.css` | **46 ekran** |
| 3 | **Tek düğme sistemi**: `.dugme*` → `.btn*` takma adı + çıplak `.ikincil` → `.btn.btn-ikincil`; sonra kaynakta kademeli değişim | `app.css` + 26 `.dugme` dosyası + 20 üçüncü-sistem dosyası + 5 sınıfsız dosya | **~45 ekran** (171 düğme) |
| 4 | **Kırılma noktası birleştirme**: `720px` → `768px`; `.form-grid-2` kuralını doğrula | `app.css:137` | **46 ekran** |
| 5 | **Tablo tek tipi**: `.tablo`/`.pano-tablo` → `.veri-tablosu` değişkeni; **12 dosyada** eksik `.tablo-sar` sarmalayıcısı | `app.css` + 12 dosya | **12 ekran** doğrudan taşma riski, görsel birlik **46 ekran** |
| 6 | **Sayı hizası**: `.sayi` sınıfını eksik 34 dosyanın `th`+`td`'lerine | 29 serbest + 5 kilitli dosya | **29 ekran** |
| 7 | **Biçimsiz sayı**: 46 yerde `Bicim.Para/Gram/Miktar`'a çevir; `ToString("N2"/"N0")` kaldır | 20 dosya (13 serbest) | **13 ekran** (para birimi/binlik ayraç kazanılır) |
| 8 | **Dropdown boş seçenek**: 65 eksik + 54 `—` → `Seçiniz`/`Tümü`/`Belirtilmedi`; `.girdi` verilmiş 23 select'i `.secim` yap | 21+11 dosya | **~30 ekran** (119 dropdown) |
| 9 | **Zorunluluk göstergesi**: `.zorunlu` yıldızı + `aria-required` + form başı açıklama | ortak `.alan` kalıbı + tüm formlar | **~35 ekran** |
| 10 | **Etiket bağlama**: 43 bağlanmamış alan | 11 serbest + 4 kilitli dosya | **11 ekran** |
| 11 | **number step/min tamamlama**: 13 `step` + 29 `min` eksiği tablo 11'e göre | 11 dosya | **11 ekran** |
| 12 | **`/giris` İşletme Kur 375px taşması**: `Giris.razor:175` satır içi ızgarayı `.form-grid-2`'ye çevir | 1 dosya | **1 ekran — tek kanıtlı canlı bozulma** |
| 13 | **`disabled` gerekçesi** kalıbını yayma; `aria-busy` ekleme | ortak düğme kalıbı | **~40 ekran** |
| 14 | **Boş durum**: `<BosDurum>` kullanmayan 6 dosya + elle yazılmış 10 metin | 16 dosya | **16 ekran** |
| 15 | **`kilavuz.html` `TABLE.mini` kırpılması** (375px) | `wwwroot/kilavuz.html` | 1 ekran |
| 16 | **Kilitli 10 ekranın 129 bulgusunun P-xx'e devri** (yazma yok, yalnız devir notu) | — | 10 ekran |

**Sıra mantığı:** 1-5 tek CSS dosyasına dokunup her ekranı düzeltir; 6-11 ekran ekran
yayılır; 12-15 tekil kanıtlı kusurlardır. Kod yazımı **oku-paralel / yaz-seri** kuralına
uyar: CSS tek elden, Razor dosyaları sırayla.

---

## § 6. KURUCUYA SEÇİM — renk şeması ve yazı tipi ölçeği

> Kurucu: *"renk ve yazı tipi benim işim."* Aşağıdaki oranlar **hesaplandı**
> (WCAG 2.1 bağıl parlaklık formülü, betik: `scratchpad/fe/kontrast.py`), tahmin değildir.
> Eşik: normal metin **≥ 4,5:1**; odak halkası / kenarlık / grafik nesnesi **≥ 3:1**.

### 6.1 Renk şemaları

#### A) **ALTIN KLASİK** — *mevcut palet* (öneri: bunu koru)
| Çift | Renkler | Oran | AA |
|---|---|---:|---|
| Gövde metni / yüzey | `#1e293b` / `#ffffff` | **14,63:1** | geçer |
| Soluk metin / yüzey | `#475569` / `#ffffff` | **7,58:1** | geçer |
| Soluk metin / tablo başlığı | `#475569` / `#f8fafc` | **7,24:1** | geçer |
| Vurgu bağlantı metni | `#b45309` / `#ffffff` | **5,02:1** | geçer |
| Birincil düğme yazısı | `#ffffff` / `#b45309` | **5,02:1** | geçer |
| Tehlike metni | `#b91c1c` / `#ffffff` | **6,47:1** | geçer |
| Tehlike düğme yazısı | `#ffffff` / `#dc2626` | **4,83:1** | geçer |
| Başarı metni | `#047857` / `#ffffff` | **5,48:1** | geçer |
| Uyarı metni | `#8a5206` / `#ffffff` | **6,38:1** | geçer |
| Odak halkası (≥3) | `#b45309` / `#ffffff` | **5,02:1** | geçer |
| Etkileşim kenarlığı (≥3) | `#8592a5` / `#ffffff` | **3,16:1** | geçer — **payı dar** |

*Karakter:* kuyumcu sektörüne doğrudan gönderme (altın/amber). *Zayıf nokta:* kenarlık
3,16:1 ile eşiği kıl payı geçiyor; `#7c8a9e`'ye koyulaştırılırsa pay rahatlar.

#### B) **PLATİN MAVİ** — kurumsal, nötr
| Çift | Renkler | Oran | AA |
|---|---|---:|---|
| Gövde metni | `#1f2937` / `#ffffff` | **14,68:1** | geçer |
| Soluk metin | `#4b5563` / `#ffffff` | **7,56:1** | geçer |
| Vurgu bağlantı metni | `#1d4ed8` / `#ffffff` | **6,70:1** | geçer |
| Birincil düğme yazısı | `#ffffff` / `#1d4ed8` | **6,70:1** | geçer |
| Tehlike metni | `#b91c1c` / `#ffffff` | **6,47:1** | geçer |
| Başarı metni | `#047857` / `#ffffff` | **5,48:1** | geçer |
| Odak halkası (≥3) | `#1d4ed8` / `#ffffff` | **6,70:1** | geçer |
| Kenarlık (≥3) | `#6b7280` / `#ffffff` | **4,83:1** | geçer — **rahat** |

*Karakter:* muhasebe/ERP yazılımlarının bildik dili; altın vurgusu kaybolur ama
kontrast payı en rahat olan şema.

#### C) **ZÜMRÜT KASA** — koyu yeşil, "kasa/güven"
| Çift | Renkler | Oran | AA |
|---|---|---:|---|
| Gövde metni | `#14532d` / `#ffffff` | **9,11:1** | geçer |
| Soluk metin | `#3f6212` / `#ffffff` | **7,08:1** | geçer |
| Vurgu bağlantı metni | `#065f46` / `#ffffff` | **7,68:1** | geçer |
| Birincil düğme yazısı | `#ffffff` / `#065f46` | **7,68:1** | geçer |
| Tehlike metni | `#b91c1c` / `#ffffff` | **6,47:1** | geçer |
| Odak halkası (≥3) | `#065f46` / `#ffffff` | **7,68:1** | geçer |
| Kenarlık (≥3) | `#6b7280` / `#ffffff` | **4,83:1** | geçer |

*Uyarı:* yeşil hem marka vurgusu hem "başarı/alacak" anlamı taşırsa **renk anlamı çakışır**
(Cariler ekranında alacak yeşil rozet). Seçilirse başarı rengi maviye kaydırılmalı.

#### D) **GECE ALTINI** — karanlık tema (mevcut karanlık paletin ölçümü)
| Çift | Renkler | Oran | AA |
|---|---|---:|---|
| Gövde metni | `#e2e8f0` / `#0f172a` | **14,48:1** | geçer |
| Soluk metin | `#94a3b8` / `#0f172a` | **6,96:1** | geçer |
| Soluk metin / yüzey-2 | `#94a3b8` / `#1e293b` | **5,71:1** | geçer |
| Vurgu bağlantı metni | `#fbbf24` / `#0f172a` | **10,69:1** | geçer |
| Birincil düğme yazısı | `#0f172a` / `#fbbf24` | **10,69:1** | geçer |
| Tehlike metni | `#f87171` / `#0f172a` | **6,45:1** | geçer |
| Başarı metni | `#34d399` / `#0f172a` | **9,29:1** | geçer |
| Odak halkası (≥3) | `#fbbf24` / `#0f172a` | **10,69:1** | geçer |
| Kenarlık (≥3) | `#64748b` / `#0f172a` | **3,75:1** | geçer |

D bir alternatif değil, **A/B/C'nin karanlık karşılığıdır** — hangisi seçilirse seçilsin
karanlık tema bu yapıda kalır (tema anahtarı zaten `belirtecler.css`'te çalışıyor).

**Ertuğ'un önerisi:** **A (Altın Klasik)** — sektöre uygun, zaten uygulanmış, tek dokunuş
kenarlığı `#8592a5` → `#7c8a9e` koyulaştırmak. B, kurucunun "kurumsal dursun" tercihi varsa
en güvenli ikinci seçenek. **Karar kurucunundur; seçilen şema `belirtecler.css`'in
2. katmanına (semantik) yazılır, ekranlara dokunulmaz.**

### 6.2 Yazı tipi ölçekleri

Yazı ailesi bugün: `"Segoe UI", system-ui, -apple-system, sans-serif` (sistem yazı tipi,
indirme yok — **hızlı**), sayılar için `ui-monospace, "SF Mono", Consolas, Menlo, monospace`.

| | **Ö1 — MEVCUT (yoğun)** | **Ö2 — OKUNAKLI** *(önerilen)* | **Ö3 — VERİ YOĞUN** |
|---|---|---|---|
| Gövde | 14px (`--yazi-govde`) | **16px** | 13px |
| xs / etiket-altı | .75rem (12px) | .8125rem (13px) | .6875rem (11px) |
| sm / tablo | .8125rem (13px) | .875rem (14px) | .75rem (12px) |
| md / kontrol | .875rem (14px) | **1rem (16px)** | .8125rem (13px) |
| lg | 1rem | 1.125rem | .9375rem |
| xl / kart başlığı | 1.125rem | 1.25rem | 1rem |
| 2xl / sayfa başlığı | 1.5rem | 1.75rem | 1.375rem |
| 3xl / giriş başlığı | 2.25rem | 2.5rem | 2rem |
| Satır yüksekliği | 1.5 (yoğun mod 1.35) | 1.55 (yoğun 1.4) | 1.4 (yoğun 1.3) |
| Sayı yazı tipi | monospace | **`tabular-nums` + gövde ailesi** | monospace |

* **Ö1** bugünkü hâl: tek ekrana çok satır sığar, ama 14px gövde + 12px yardım metni
  40 yaş üstü kullanıcıda zorlar (kuyumcu esnafı hedef kitle).
* **Ö2 (önerilen)**: mobil girdi yüksekliği doğal olarak 44px'e çıkar (kural 28 ile örtüşür) ve
  iOS'ta 16px altı girdilerde oluşan otomatik yakınlaştırma sorunu ortadan kalkar. Maliyet:
  tablolarda satır başına ~2px artış.
* **Ö3** yalnız "profesyonel/yoğun" modun ölçeği olarak tutulabilir; varsayılan olmamalı.

**Ertuğ'un önerisi:** varsayılan **Ö2**, "profesyonel mod" seçildiğinde **Ö1**.
Ö3 açılmaz (dokunma hedefi kuralıyla çelişir).

---

## § 7. ÇÖZÜNÜRLÜK TURU (TAM) — 2026-09-21, 2. geçiş

> **Salt okunur.** Hiçbir kaynak dosya değiştirilmedi, `dotnet build`/`test` koşulmadı,
> git komutu yok, kaydet/sil düğmesine basılmadı, 5198'e dokunulmadı. Kendi sekmem
> (`tab-4`), başka sekmeye dokunulmadı.

### 7.1 Oturum ve yöntem

* **Kimlik bilgisi girmedim.** Tarayıcı profilinde **zaten açık bir oturum** bulundu
  (`localStorage`: `kuyumcu-access`, `kuyumcu-refresh` — yalnız anahtar adları ve uzunlukları
  okundu, **değerleri okunmadı, hiçbir yere yazılmadı**). `/` açıldığında giriş ekranına
  yönlenmedi; ölçümler bu var olan oturumla yapıldı. Fikstür dosyasındaki parola/jeton
  değerleri kullanılmadı ve rapora yazılmadı.
* Kiracı **QA-TEST-FRONTEND**, verisi boş → listeler boş durumda ölçüldü (boş durum metinleri
  de § 7.3'te kayıtlı).
* Gezinti: sayfa yeniden yüklemeden `Blazor.navigateTo` + **rota eşleşme beklemesi**
  (`location.pathname === hedef` olana dek, en çok 3 sn) + yükleme göstergesinin kaybolmasını
  bekleme. Eşleşme doğrulaması olmadan yapılan ilk 1440 geçişinde 2 satır kaymıştı; kural
  eklenip o iki rota (`/`, `/ayarlar`) yeniden ölçüldü.
* **Ölçüm yeri — kritik:** `app.css:122` `.ana-icerik { flex:1; padding:1.5rem; overflow:auto }`.
  Bu yüzden taşma `document`'ta değil `<main>` içinde birikir. Kaynaktaki yorum (`app.css:563`)
  bunu zaten söylüyor: taşmanın `.tablo-sar` / `.grafik-sar` gibi **kaydırılabilir kutulara
  hapsedilmesi** hedeflenmiş. Bu yüzden her sayfada **iki** ölçü alındı:
  1. `documentElement.scrollWidth` vs `clientWidth` — sayfa düzeyi taşma,
  2. `main.ana-icerik.scrollWidth` vs `clientWidth` — **içerik alanı yatay kayması**
     (kullanıcının gerçekten gördüğü kaydırma çubuğu).
  Ayrıca her taşan öğenin ata zinciri tarandı: `overflow-x: auto|scroll` bir kap varsa
  "tasarım gereği hapsedilmiş", yoksa **bozulma** sayıldı.

**Kapsam: 39 rota × 3 genişlik (375 / 768 / 1440) = 117 ölçüm.**

### 7.2 Sonuç

| Genişlik | `document` taşması | `main` yatay kayması | Durum |
|---|---:|---:|---|
| **1440** | 0 / 39 | **0 / 39** | tamamı temiz (`clientWidth` 1425, içerik sütunu 1185) |
| **768** | 0 / 39 | **0 / 39** | tamamı temiz (`clientWidth` 768; dikey kaydırma çubuğu varken 753) |
| **375** | 0 / 39 | **6 / 39** | **6 ekranda bozulma** (aşağıda) |

**375 px'te bozulan 6 ekran — kök nedeniyle:**

| Sayfa | `main` cW | `main` sW | Taşma | Kök öğe | Kök neden (dosya:satır) |
|---|---:|---:|---:|---|---|
| `/urunler` | 375 | **612** | **237px** | `DIV.satir` içindeki arama girdisi + "Barkod okut" düğmesi + 2 `SELECT.secim`; satırın `min-content`'i 600px | **`app.css:387` `.satir { display:flex; align-items:center; gap:.75rem }` — `flex-wrap` YOK** · satır: `Pages/Urunler.razor:64` |
| `/islemler` | 375 | **479** | **104px** | 3 `BUTTON.sekme`; `DIV.sekme-grubu.sekme-serit` `scrollWidth` 661 | `app.css:579-585` `.sekme-serit`'e ≤768'de `overflow-x:auto` veriliyor **ama** ata zincirinde `min-width:0` yok → şerit kendi içinde kaymıyor, taşmayı `main`'e aktarıyor · `Pages/Islemler.razor:37` |
| `/ayarlar` | 375 | **423** | **48px** | `SPAN` @423; atası `DIV.satir-arasi` (`scrollWidth` 394) | **`app.css:390` `.satir-arasi { display:flex; justify-content:space-between }` — `flex-wrap` YOK** · `Pages/Ayarlar.razor:79, 83, 87, 162, 288` (aynı kalıp 5 kez) |
| `/emanet` | 375 | **411** | **36px** | `BUTTON.btn.btn-ikincil` @411; atası `DIV.satir` (`scrollWidth` 399) | `app.css:387` `.satir` wrap yok · `Pages/Emanet.razor:44` |
| `/vitrin` | 375 | **393** | **18px** | `TABLE.tablo` @393; atası `DIV.kart-govde` — **`.tablo-sar` sarmalayıcısı YOK** | § 1.4'teki "sarmalayıcısız 12 dosya" listesinin canlı kanıtı · `Pages/Vitrin.razor:57, 98, 125` |
| `/kur` | 375 | **389** | **14px** | `DIV.satir` @389; atası `DIV.kart-baslik.satir-arasi` | `app.css:387`/`:390` wrap yok · `Pages/Kur.razor:122, 242, 244` (tablolar `.tablo-sar` içinde ve **doğru** davranıyor) |

**Tek cümlelik özet: 6 bozulmanın 5'i iki CSS satırından kaynaklanıyor.**
`app.css`'teki diğer TÜM satır yardımcıları `flex-wrap: wrap` taşıyor —
`.dugme-satir:212`, `.yazdir-kutu:291`, `.sekme-grubu:336`, `.rozet-satir:473`,
`.satir-eylemler:543`, `.agac-satir:548`, `.bacak-satiri:552`, `.sayfa-basi-filtre:720`,
`.satir-ici:769` — yalnız **`.satir` (`:387`) ve `.satir-arasi` (`:390`)** taşımıyor.
6'ncısı (`/vitrin`) zaten § 1.4'te raporlanan eksik `.tablo-sar`.

**Önerilen düzeltme (tur 2, 4 dokunuş):**
1. `app.css:387` → `.satir { …; flex-wrap: wrap; }`
2. `app.css:390` → `.satir-arasi { …; flex-wrap: wrap; }`
3. `.sekme-serit`in ata kabına `min-width: 0` (şerit kendi içinde kaysın)
4. `Pages/Vitrin.razor:57, 98, 125` tablolarını `<div class="tablo-sar">` içine al
   *(bu 4 madde § 5'teki 5 ve 12 numaralı işlerin içinde; öncelik sırası değişmiyor)*

**375 px'te temiz ölçülen 33 rota:**
`/` · `/banka` · `/bildirimler` · `/cariler` · `/denetim-izi` · `/evraklar` ·
`/evraklar/takvim` · `/faturalar` · `/fiyatlandirma` · `/gider-pusulalari` · `/isletme` ·
`/kasalar` · `/kilavuz` · `/kullanicilar` · `/kurulum` · `/pos` · `/pos/tahsilatlar` ·
`/raporlar` · `/raporlar/islem-defteri` · `/raporlar/kar-zarar` · `/raporlar/kasa` ·
`/raporlar/satis-analizi` · `/raporlar/vade-takibi` · `/sayim` · `/siparisler` ·
`/urunler/agac` · `/urunler/etiketler` · `/uyum` · `/veri-disa-aktarim` ·
`/veri-ice-aktarim` · `/vezne` · `/yedekler` · `/yetkiler`

**Kesilen metin:** ayrı bir tarama yapıldı (yaprak öğede `scrollWidth > clientWidth` +
`overflow:hidden`). Gerçek kırpılma bulunmadı; tek tekrar eden eşleşme `SPAN.gorunmez`
(ekran okuyucu için gizlenen yardımcı metin, 1px genişlik) — **yanlış pozitif**, kusur değil.

**Dokunma hedefi (375 px, her sayfa için 40px'ten küçük etkileşimli öğe sayısı):**
39 rotanın **hepsinde** 46-91 arası. En kötü: `/kur` **91** · `/islemler` **65** ·
`/kilavuz` 60 · `/ayarlar` 57. En iyi: `/` 46. 1440'ta bile 47-135 (`/yetkiler` 135 —
yetki matrisi onay kutuları). Bu **çözünürlükten bağımsız**, § 1.6'daki kök nedenin
(kontrol yüksekliği 35px, `--dokunma-hedefi` belirtecinin kullanılmaması) doğrudan sonucudur
ve § 5'in **1 numaralı** işidir.

### 7.3 Boş durum metinleri (boş kiracıda ölçüldü)

**Metin GÖSTEREN 22 ekran** (kalıp tutarsızlığı var — bkz. § 3 kural 26):
`/cariler` "Cari bulunamadı" · `/urunler` "Ürün bulunamadı" · `/kasalar` "Kasa bulunamadı" ·
`/banka` "Banka hesabı bulunamadı" · `/faturalar` "Fatura bulunamadı" ·
`/islemler` "Bu kiracıda henüz satış kaydı yok" · `/vezne` "Bekleyen fiş yok" ·
`/evraklar` ve `/evraklar/takvim` "Portföyde evrak yok" · `/pos` "Henüz POS cihazı tanımlanmadı" ·
`/pos/tahsilatlar` "Bu aralıkta POS tahsilatı yok" · `/gider-pusulalari` "Bu aralıkta gider
pusulası yok." · `/bildirimler` "Bekleyen bildirim yok." · `/kurulum` "Henüz açılış satırı
eklemediniz." · `/raporlar`, `/raporlar/kasa`, `/raporlar/satis-analizi` "Bu dönemde kayıt yok." ·
`/urunler/agac` "Henüz kategori tanımlanmadı" · `/uyum` "Bu aralıkta kimlik tespiti kaydı yok." ·
`/veri-ice-aktarim` "Henüz içe aktarım yapılmadı." · `/yedekler` "Bekleyen ya da geçmiş yedek
talebi yok." · `/raporlar/islem-defteri` "Bu dönemde kayıt yok." (gövdede, `<BosDurum>` değil).

Gözlenen tutarsızlık: **nokta var/yok** (12 metin noktasız, 8 noktalı), **kip karışık**
("… bulunamadı" / "… yok" / "Henüz … tanımlanmadı" / "Henüz … eklemediniz"), ve
**hiçbirinde "ne yapmalıyım" yönlendirmesi yok.** § 3 kural 26'daki kalıp bunu kapatır.

**Boş durum metni GÖSTERMEYEN 16 ekran** (boş kiracıda kullanıcıya hiçbir şey söylemiyor —
**bulgudur**): `/siparisler` → kilitli · `/ayarlar` → kilitli · `/raporlar/kar-zarar` → kilitli ·
`/sayim` → kilitli · `/emanet` · `/denetim-izi` · `/fiyatlandirma` · `/isletme` ·
`/kullanicilar` · `/kur` · `/raporlar/vade-takibi` · `/urunler/etiketler` ·
`/veri-disa-aktarim` · `/vitrin` · `/yetkiler` · `/kilavuz`.
*(Not: bunların bir kısmı form/ayar ekranıdır ve liste içermez — orada boş durum beklenmez.
Liste/tablo içerip metin göstermeyenler: `/emanet`, `/denetim-izi`, `/fiyatlandirma`,
`/kullanicilar`, `/kur`, `/urunler/etiketler`, `/vitrin`, `/yetkiler` = **8 ekran**, § 1.4'teki
`<BosDurum>` eksiği listesiyle örtüşür.)*

### 7.4 HÂLÂ ÖLÇÜLMEYENLER — 6 rota

| Rota | Sayfa | Sebep |
|---|---|---|
| `/hurda` | `Pages/Hurda.razor` | **ÖLÇÜLMEDİ** — 5199 yayınında rota yok, "Sayfa bulunamadı" döndü. Kaynakta `@page "/hurda"` **var**; yayın DALGA 4 anlık görüntüsü olduğu için bu ekranı içermiyor. |
| `/gider` | `Pages/Gider.razor` | **ÖLÇÜLMEDİ** — aynı sebep, "Sayfa bulunamadı" döndü. Kaynakta `@page "/gider"` var. |
| `/banka/ekstre/{IthalatId}` | `BankaEkstre` | **ÖLÇÜLMEDİ** — parametreli rota; boş kiracıda gerçek ithalat kimliği yok, kayıt oluşturmak yasak (salt okunur tur). |
| `/cikti/{Tur}/{Id}` | `Cikti` | **ÖLÇÜLMEDİ** — parametreli rota, aynı sebep. |
| `/gider-pusulalari/{Id}/belge` | `PusulaBelgesi` | **ÖLÇÜLMEDİ** — parametreli rota, aynı sebep. |
| `/siparisler/{Id}` | `SiparisDetay` | **ÖLÇÜLMEDİ** — parametreli rota, aynı sebep. |

Bu 6 ekran için şu an yalnız **kaynaktan** çıkarılmış risk var (§ 1). `/gider` ve `/hurda`
yayın tazelenince, 4 parametreli rota ise veri girilmiş bir kiracıda ölçülebilir.

### 7.5 Ölçüm sırasında görülen, UI kusuru OLMAYAN şey

`/urunler` ekran görüntüsü alınırken üstte kırmızı bir bant vardı:
*"Sunucu şu anda çok yoğun (istek sınırı). 1 saniye sonra 'Yeniden dene' ile tekrar deneyin —
veriler silinmedi."* Bunu **benim yüküm** üretti (39 rota × 3 genişlik gezinti). Bant
doğru davranıyor: hatayı Türkçe, sakin ve veri kaybı olmadığını söyleyerek gösteriyor —
`<HataBandi>` kalıbının doğru çalıştığının kanıtıdır, bulgu değildir.

### 7.6 Kapsam dışı bıraktıklarım (diğer ajanların alanı)

* Açılır pencere / diyalog **içi** kaydırma çubukları → **ikinci ajan**. Ölçmedim.
* Düğme dili, menü araması, kurumsal renk paleti → **üçüncü ajan**. Bu geçişte ele almadım
  (§ 1.1 ve § 6 birinci geçişte yazılmıştı, değiştirilmedi).

---

## EK — kanıt dosyaları

* Makine okunur envanter: `scratchpad/frontend-envanter.json`
* Ayrıştırıcı ve özet betikleri (geçici): `scratchpad/fe/tara2.py`, `scratchpad/fe/son.py`,
  `scratchpad/fe/kontrast.py`
* Taranan kaynak kökü: `dotnet/Kuyumcu.Web/` (83 `.razor`, `wwwroot/css/app.css`,
  `wwwroot/css/belirtecler.css`, `Servisler/Bicim.cs`)
* Tarayıcı turu 1 (giriş yapılmadan): `/giris` (2 sekme × 3 genişlik) + `/kilavuz.html`
  (3 genişlik) → § 2.
* Tarayıcı turu 2 (var olan oturumla): **39 rota × 375/768/1440 = 117 ölçüm** → § 7.
  Ölçüm verisi `frontend-envanter.json` içinde **`cozunurluk`** anahtarı altında
  (sayfa · genişlik · `main` scrollWidth · taşan öğe · kök neden · boş durum metni ·
  dokunma hedefi sayısı). Ölçülemeyen 6 rota § 7.4'te sebebiyle listeli.
