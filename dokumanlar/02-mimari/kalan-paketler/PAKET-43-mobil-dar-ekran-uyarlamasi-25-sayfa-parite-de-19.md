# Paket 43 — Mobil / dar ekran uyarlaması (25 sayfa; PARITE'de '19 sayfa')

> Katman: Katman 7 — Platform · Tahmin: 5-8 gün (Faz A 2-3 · Faz B 2-3 · Faz C 1-2) — PARITE tahminiyle uyumlu gün · Önkoşullar: yok · Kaynak: katman mimarı K7-43 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** EVET · Dalga 8

## Hedef
Perakende kuyumcu tezgâhta ve tezgâh dışında telefondan çalışır: patron dükkândan uzaktayken kasa durumunu, cari bakiyeyi ve günlük satışı telefonda okuyabilmeli; tezgâhtar 375 px'lik bir ekranda menüye ulaşıp ürün/cari arayabilmeli. Bugün 375 px'te kenar çubuğu 240 px yer kaplıyor, uygulama yatay kayıyor ve altı sütunlu tablolar okunamıyor. Paket bunu üç fazda kapatır: kabuk (çekmece), patron üçlüsü + öncelikli tablolar, kalan sayfalar ve dokunma hedefleri.

## Kodda bugünkü durum
Kabuk: AnaYerlesim.razor <div class="uygulama"> içinde <KenarCubugu/> + .icerik-sutunu; kenar çubuğu app.css:56 'width: 240px; flex-shrink: 0' ile SABİTTİR ve hiçbir medya sorgusu onu gizlemez (grep: app.css'te .kenar-cubugu yalnız @media print bloğunda display:none, app.css:434). Hamburger/çekmece DOM'u YOKTUR (KenarCubugu.razor'da buton yok, UstBar.razor'da yalnız h1 + .ust-sag var). Dar ekran temel katmanı UX-2 A1 ile geldi: app.css:481 @media(max-width:768px) form ızgaralarını tek sütuna indirir, modal/kart/ana içerik dolgusunu kısar; app.css:492 @media(max-width:480px) .stat-izgara'yı tek sütun yapar; app.css:129 @media(max-width:720px) kur bandını ve son sorguyu gizler. app.css:478 civarındaki yorum 375 px yatay kaydırmanın A2'ye kadar KAPANMADIĞINI açıkça yazar. Tablolar: 27 Pages dosyasından 22'sinde <table> var, 21'i .tablo-sar (app.css:221 overflow-x:auto) içinde; tek istisna PusulaBelgesi.razor (2 tablo, sarmalayıcısız — kâğıt belge, ADR-0007). Hiçbir <td>'de data-etiket yok (grep boş). Örnek tablo deseni Cariler.razor:42-64 (6 sütun, son sütun .satir-eylemler düğmeleri). Panel.razor:51-55 dört StatKart taşır (GunlukSatis, GunlukAlis, NetHasGram, NetKasa). Yerleşim durumu servisi yok (Program.cs:13-19'da OturumServisi, DilServisi, TemaServisi, ApiIstemci, CanliKurServisi, YetkiServisi, UyumServisi). index.html:6 viewport meta'sı doğru (width=device-width, initial-scale=1.0) — orada düzeltilecek bir şey yok.

## Kapsam
**İçinde:**
- FAZ A (tek başına sevk edilebilir, 2-3 gün): YerlesimDurumu servisi; UstBar'a hamburger düğmesi (yalnız ≤768px görünür); KenarCubugu'nun ≤768px'te sabit konumlu çekmeceye dönmesi + kapanış düğmesi; tıklanınca kapanan yarı saydam örtü; rota değişiminde ve Esc'te otomatik kapanma; ≥769px'te davranışın BUGÜNKÜ hâliyle bit bit aynı kalması; üst bar kırılımının 720 → 768 px'e taşınması.
- FAZ B (2-3 gün): patron üçlüsü dar ekran okunabilirliği — Panel (stat kartları + grafik sarmalayıcı), Kasalar, Cariler; `.veri-tablosu.kart-mod` CSS'i ve Cariler/Kasalar/Islemler/Faturalar/VadeTakibi/Urunler/Banka tablolarının <td>'lerine `data-etiket` eklenmesi (7 sayfa).
- FAZ C (1-2 gün): kalan tablo sayfaları (DenetimIzi, Emanet, Fiyatlandirma, GiderPusulalari, Isletme, KarZarar, Kullanicilar, Kur, Raporlar, Sayim, UrunAgaci, UrunEtiketleri, Uyum, VeriDisaAktarim, Yetkiler) için kart modu ya da 'yatay kaydırılabilir kalır' kararının TEK TEK kaydı; birincil eylem düğmeleri ve sekmeler için ≥44x44 CSS dokunma hedefi; modal'ın ≤480px'te tam ekran açılması; envanter testinin tüm sayfaları kapsayacak şekilde açılması.
- Sozlukler.cs'e TR+EN yeni metinler: MenuyuAc, MenuyuKapat (aria-label'lar).
- Ölçüm betiği/QA turu: 375x667, 390x844, 414x896, 768x1024, 1024x768 genişliklerinde 25 sayfanın taşma ölçümü.

**Dışında:**
- PWA manifest / service worker / ana ekrana ekleme — paket 44.
- Native mobil uygulama (mağaza yayını) — PARITE C sınıfı, izin/sözleşme gerektirir.
- El terminali / barkod tarayıcı ergonomisi — B sınıfı, cihazda doğrulanır.
- PusulaBelgesi.razor ve `.belge*` / `.urun-etiket*` / @media print blokları — kâğıt çıktısı dar ekran kurallarından MUAFTIR (belirtecler.css:21 ve app.css:12 ile zaten zorlanıyor).
- Giris.razor'un kendi iki panelli düzeni (app.css:301) — zaten dar ekranda tek panele düşüyor, dokunulmaz.
- Yeni API ucu, yeni varlık, yeni migration — yoktur.
- Gerçek cihaz/tarayıcı destek matrisi (PARITE B sınıfı).

## Veri modeli
- Şema değişikliği yok

## Uçlar
- Yeni uç yok

## Servis ve dosyalar
- **YENI** `dotnet/Kuyumcu.Web/Layout/YerlesimDurumu.cs` — Faz A. Scoped servis; TemaServisi/DilServisi deseni birebir: `public bool CekmeceAcik { get; private set; }`, `public event Action? Degisti`, `Ac()`, `Kapat()`, `Degistir()`. Her metot durumu değiştirdiyse Degisti'yi tetikler, değiştirmediyse TETİKLEMEZ (gereksiz render yok — TemaEkranTestleri'ndeki 'sistem seçilince hiç çağrı yapılmaz' titizliğinin karşılığı). Kalıcılık YOK: çekmece tercihi localStorage'a yazılmaz, her sayfa yüklemesinde kapalı başlar.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Program.cs` — Faz A. `builder.Services.AddScoped<YerlesimDurumu>();` satırı 15-19 aralığına, yorumuyla (// dar ekran çekmecesi (paket 43)).
- **DEGISTIR** `dotnet/Kuyumcu.Web/Layout/UstBar.razor` — Faz A. <header class="ust-bar"> içinde h1'den ÖNCE: `<button class="menu-dugmesi" id="menu-ac" aria-label="@Dil.T.Nav.MenuyuAc" aria-expanded="@Yerlesim.CekmeceAcik.ToString().ToLowerInvariant()" aria-controls="kenar-cubugu" @onclick="Yerlesim.Degistir">☰</button>`. YerlesimDurumu inject edilir, Degisti olayına abone olunur ve Dispose'da bırakılır (dosyadaki mevcut IDisposable deseni). Kur bandı/son sorgu JSX'i DEĞİŞMEZ.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor` — Faz A. <aside> etiketine `id="kenar-cubugu"` ve `class="kenar-cubugu @(Yerlesim.CekmeceAcik ? "acik" : null)"` eklenir. .kenar-logo içine kapanış düğmesi: `<button class="cekmece-kapat" id="menu-kapat" aria-label="@Dil.T.Nav.MenuyuKapat" @onclick="Yerlesim.Kapat">✕</button>`. NavLink listesi, yetki koşulları ve .kenar-alt HİÇ DEĞİŞMEZ (yetki mantığına dokunulmaz). OnInitialized'a `Nav.LocationChanged += (_, _) => Yerlesim.Kapat();` ve Yerlesim.Degisti aboneliği eklenir, Dispose'da bırakılır.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Layout/AnaYerlesim.razor` — Faz A. <div class="uygulama"> içine KenarCubugu'ndan SONRA örtü: `@if (Yerlesim.CekmeceAcik) { <div class="cekmece-orta" @onclick="Yerlesim.Kapat"></div> }`. Dış div'e `@onkeydown="EscKapat" tabindex="-1"` yerine: örtüye `@onkeydown` yerine basit çözüm — `<div class="uygulama" @onkeyup="EscKapat">` ve `private void EscKapat(KeyboardEventArgs e) { if (e.Key == "Escape") Yerlesim.Kapat(); }`. Oturum/yönlendirme mantığı (OnInitializedAsync, KontrolEt) DEĞİŞMEZ.
- **DEGISTIR** `dotnet/Kuyumcu.Web/wwwroot/css/app.css` — Faz A: (1) .menu-dugmesi ve .cekmece-kapat varsayılan `display: none`, ≤768px'te `display: inline-flex; min-width: 2.75rem; min-height: 2.75rem` (44 px dokunma hedefi); renkler yalnız var() ile (belirtecler.css). (2) @media (max-width: 768px) bloğuna: `.kenar-cubugu { position: fixed; inset-block: 0; inset-inline-start: 0; z-index: 60; transform: translateX(-100%); transition: transform .2s; }` ve `.kenar-cubugu.acik { transform: none; }`, `.cekmece-orta { position: fixed; inset: 0; z-index: 50; background: rgb(0 0 0 / .45); }`. (3) `@media (prefers-reduced-motion: reduce) { .kenar-cubugu { transition: none; } }` — app.css:43'teki mevcut desenin devamı. (4) app.css:129'daki 720px eşiği 768px yapılır. Faz B: `.veri-tablosu.kart-mod` kuralları @media(max-width:768px) içinde — thead gizlenir, tr kart olur (border + radius + margin-block), td flex satır olur ve `td::before { content: attr(data-etiket); color: var(--metin-soluk); font-size: var(--yazi-sm); }` başlığı basar; `td[data-etiket=""]::before` hiç basmaz (eylem hücresi). Faz C: `.sekme, .dugme, .satir-eylemler button { min-height: 2.75rem }` ve ≤480px'te `.modal-panel { margin: 0; max-height: 100vh; border-radius: 0 }`. ⚠ Var olan seçicilerin özgüllüğü ve sırası KORUNUR; @media print bloğuna dokunulmaz.
- **DEGISTIR** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — Faz A. SozlukNav'a (satır 50) iki required alan: MenuyuAc / MenuyuKapat; TR ve EN sözlüklerinin ikisinde de (satır 1120 ve 2054 civarındaki Nav = new() blokları) doldurulur. TR: "Menüyü aç" / "Menüyü kapat"; EN: "Open menu" / "Close menu".
- **DEGISTIR** `dotnet/Kuyumcu.Web/Pages/Cariler.razor · Kasalar.razor · Islemler.razor · Faturalar.razor · VadeTakibi.razor · Urunler.razor · Banka.razor` — Faz B. Yalnız iki şey: <table class="veri-tablosu"> → <table class="veri-tablosu kart-mod">, ve her <td>'ye ilgili <th>'nin sözlük anahtarıyla `data-etiket="@Dil.T..."` (eylem hücresine `data-etiket=""`). Veri, filtre, çağrı, yetki mantığına DOKUNULMAZ — diff yalnız öznitelik eklemesi olmalı.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Pages/Panel.razor` — Faz B. Patron üçlüsü dar ekranda ilk ekranda olsun: stat kartlarının sırası GunlukSatis → NetKasa → NetHasGram → GunlukAlis olacak biçimde yeniden dizilir (satır 51-55) ve grafikler `.grafik-sar` (app.css:320) içinde kaldıkları doğrulanır. Yeni veri çağrısı yok.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Pages/DenetimIzi · Emanet · Fiyatlandirma · GiderPusulalari · Isletme · KarZarar · Kullanicilar · Kur · Raporlar · Sayim · UrunAgaci · UrunEtiketleri · Uyum · VeriDisaAktarim · Yetkiler (.razor)` — Faz C. Her tablo için TEK TEK karar: 4 sütuna kadar olanlar kart moduna alınmaz (zaten sığar), 5+ sütunlular kart moduna alınır; alınmayan her tablo için dosyanın başındaki yorum bloğuna gerekçe satırı ('N sütun, 375 px'te sığıyor / sayısal ızgara okunurluğu kart modunda bozuluyor'). Karar envanteri testle sayılır (bkz. testler).

## Ekranlar
- `tüm rotalar (ortak kabuk)` · `dotnet/Kuyumcu.Web/Layout/AnaYerlesim.razor + UstBar.razor + KenarCubugu.razor` — ≤768px: kenar çubuğu ekrandan çıkar, üst barda hamburger belirir; tıklanınca çekmece soldan girer, örtü/Esc/rota değişimi kapatır. ≥769px: bugünkü sabit 240 px kenar çubuğu, hamburger gizli. (menü/yetki: Menü içeriği ve yetki koşulları DEĞİŞMEZ (KenarCubugu.razor'daki Yetki.Erisebilir / Rol kontrolleri aynen kalır); çekmece yalnız görünürlük biçimini değiştirir, yetkiyi değiştirmez.)
- `/` · `dotnet/Kuyumcu.Web/Pages/Panel.razor` — Patron üçlüsü (günlük satış, net kasa, net has) 375 px'te ilk ekranda, tek sütun stat ızgarasında; grafikler kendi kaydırılabilir kutusunda. (menü/yetki: değişmez)
- `/cariler, /kasalar, /islemler, /faturalar, /raporlar/vade-takibi, /urunler, /banka` · `ilgili Pages/*.razor` — ≤768px'te tablo satırları karta dönüşür; her hücre `data-etiket` ile başlığını yanında taşır; eylem düğmeleri kartın altında sarar (mevcut .satir-eylemler, app.css:457 civarı). (menü/yetki: değişmez)

## İş kuralları (fail-closed)
- Çekmece durumu SUNUCUYA GİTMEZ ve kalıcı değildir: yetki, kiracı, rol kararlarıyla hiçbir ilişkisi yoktur. Menüde bir bağlantının görünmesi hâlâ yalnız KenarCubugu'ndaki yetki koşuluna bağlıdır ve son sözü sunucudaki [RolGerekli]/[ModulYetkisi] söyler.
- Geniş ekran davranışı DEĞİŞMEZ: ≥769px'te üretilen DOM ve hesaplanan yerleşim paket öncesiyle aynı olmalıdır (hamburger `display:none`, kenar çubuğu akışta 240 px).
- Çekmece açıkken arka plan içeriği kaydırılmaz ve örtü tıklaması olayı yutar; ancak `position: fixed` ile body kilidi JS gerektirmediğinden body'ye sınıf eklenmez — örtü `inset: 0` ile tüm dokunuşları alır.
- Kâğıt muafiyeti: `.belge*`, `.urun-etiket*` ve `@media print` blokları dar ekran kurallarından etkilenmez; çekmece ve örtü @media print'te `display: none` (app.css:434 listesine `.cekmece-orta` eklenir).
- Dokunma hedefi: birincil eylem düğmeleri, sekmeler ve menü düğmeleri ≤768px'te en az 44x44 CSS px (WCAG 2.1 AAA 2.5.5'in ölçüsü; AA 2.5.8 24x24'tür — biz 44'ü hedefliyoruz, gerekçe kuyumcunun tezgâhta eldivenli/hızlı dokunuşu).
- i18n değişmezi: hamburger ve kapatma düğmelerinin aria-label'ları Sozlukler üzerinden gelir; Razor'a çıplak metin yazılmaz (Sozlukler.cs başlığındaki KURAL).
- CSP değişmezi: yeni satır içi <script> ya da eval YOK; paket 43 hiç JS dosyası eklemez.

## Testler
- `dotnet/Kuyumcu.Tests/CekmeceEkranTestleri.cs` [bUnit]
  - Varsayılanda çekmece KAPALI: AnaYerlesim çizilince .cekmece-orta DOM'da yok ve #menu-ac aria-expanded=false.
  - #menu-ac tıklanınca YerlesimDurumu.CekmeceAcik true, aside sınıfında 'acik' var, aria-expanded=true.
  - #menu-kapat tıklanınca kapanır; örtüye tıklanınca kapanır.
  - Rota değişince (NavigationManager.NavigateTo) çekmece kapanır — açıkken menüden bir bağlantıya basan kullanıcı örtünün altında kalmaz.
  - Esc tuşu (@onkeyup, Key="Escape") kapatır; başka bir tuş kapatmaz.
  - Kapalıyken Kapat() çağrısı Degisti olayını TETİKLEMEZ (gereksiz render yok).
  - Hamburger'in aria-label'ı seçili dile göre değişir (TR/EN), çıplak metin değil.
- `dotnet/Kuyumcu.Tests/DarEkranCssTestleri.cs` [saf]
  - CssKaynak (TasarimBelirtecTestleri.cs'teki ayrıştırıcı yeniden kullanılır) ile: app.css'te `.kenar-cubugu` için @media (max-width: 768px) altında `position: fixed` ve `transform: translateX(-100%)` bildirimleri VAR; `.kenar-cubugu.acik` için `transform: none` VAR.
  - 768 px dışında üçüncü bir kenar-çubuğu kırılımı YOK (eşik envanteri: app.css'teki max-width değerleri kümesi {768, 480} ∪ print; 720 px ARTIK YOK).
  - `prefers-reduced-motion: reduce` altında .kenar-cubugu geçişi kapatılmış.
  - @media print bloğunun display:none listesinde `.cekmece-orta` var.
  - Dar ekran bloğundaki hiçbir bildirim ham hex/rgb renk kullanmaz — yalnız var() (UX-2 A1 belirteç kuralının devamı).
  - .menu-dugmesi ve .cekmece-kapat için min-width/min-height ≥ 2.75rem.
- `dotnet/Kuyumcu.Tests/DarEkranEnvanterTestleri.cs` [saf]
  - Pages/*.razor taranır: <table içeren her dosya ya `.tablo-sar` sarmalayıcısı taşır ya da muafiyet listesinde (PusulaBelgesi) — muafiyet listesi testte SABİT ve gerekçelidir; yeni bir sayfa listeye kendiliğinden giremez.
  - `kart-mod` sınıfı taşıyan her tablonun TÜM <td>'lerinde data-etiket özniteliği var (biri eksikse test kırmızı) — 'yarım dönüştürülmüş tablo' imkânsız.
  - data-etiket değerleri `@Dil.T.` ile başlar (çıplak Türkçe metin yasak) ya da boş dizgedir (eylem hücresi).
  - Kapsam sayımı: Pages/*.razor sayısı ile paket kapsam listesi (25 + 2 muaf) tutarlı; yeni sayfa eklenince test kırmızı olur ve uyarlamayı zorunlu kılar.
  - index.html'de viewport meta'sı width=device-width içerir ve user-scalable=no / maximum-scale YOKTUR (yakınlaştırma engellenemez — WCAG 1.4.4).
- `dotnet/Kuyumcu.Tests/CariEkranTestleri.cs (mevcut) + KasaEkranTestleri` [bUnit]
  - Faz B'de dokunulan sayfaların MEVCUT testleri (çağrı/uç/gövde iddiaları) aynen yeşil kalmalı — data-etiket eklemesinin davranışı değiştirmediğinin kanıtı; yeni test eklenmez, regresyon kapısı olarak koşulur.

## Mutasyon kanıtları
- M-43-01: `.kenar-cubugu.acik { transform: none }` kuralı silinir → DarEkranCssTestleri 'acik sınıfı çekmeceyi içeri alır' KIRMIZI.
- M-43-02: KenarCubugu'nda Nav.LocationChanged aboneliği kaldırılır → CekmeceEkranTestleri 'rota değişince kapanır' KIRMIZI.
- M-43-03: #menu-ac'ın aria-expanded bağlaması sabit "false" yapılır → CekmeceEkranTestleri aria-expanded senaryosu KIRMIZI.
- M-43-04: Cariler.razor'daki bir <td>'den data-etiket silinir → DarEkranEnvanterTestleri 'kart-mod tüm td' KIRMIZI.
- M-43-05: app.css'teki 720 px eşiği geri getirilir → DarEkranCssTestleri eşik envanteri KIRMIZI.
- M-43-06: aria-label Sozlukler yerine çıplak "Menü" yazılır → CekmeceEkranTestleri dil senaryosu KIRMIZI.
- M-43-07: index.html viewport'a `user-scalable=no` eklenir → DarEkranEnvanterTestleri KIRMIZI.
- M-43-08: YerlesimDurumu.Kapat() koşulsuz Degisti tetikler → 'kapalıyken tetiklenmez' KIRMIZI.

## Canlı doğrulama
- API 5199'da yeniden başlatılır; tarayıcı 375x667'ye ayarlanır (resize_window mobile preset) ve SAHIP rolüyle giriş yapılır.
- 25 sayfanın her birinde iki ölçüm okunur ve raporlanır: `document.documentElement.scrollWidth - clientWidth` ve `document.querySelector('main.ana-icerik').scrollWidth - clientWidth`. KABUL: ikisi de ≤ 1 px. Ölçülen sayılar rapora TEK TEK yazılır (gözle 'düzgün duruyor' kanıt sayılmaz).
- Aynı ölçüm 390x844, 414x896, 768x1024 ve 1024x768'te tekrarlanır; 1024x768'te kenar çubuğunun hâlâ akışta ve hamburger'in gizli olduğu doğrulanır (geniş ekran regresyonu yok).
- Çekmece elle sürülür: hamburger → menü açılır → bir bağlantıya tıklanır → çekmece kapanır ve doğru sayfa açılır; örtüye tıklama ve Esc ayrı ayrı denenir.
- Karanlık tema + TR/EN dillerinde 375 px'te Panel, Cariler, Kasalar ekran görüntüsü alınır (UX-2 tema regresyonu kontrolü).
- Yazdırma önizlemesi (Cmd+P) bir gider pusulasında açılır: çekmece/örtü kâğıda çıkmıyor, belge düzeni bozulmamış.
- Test kiracısı kullanıldıysa doğrulama sonunda silinir.

## Dosya ayak izi (çakışma planı için)
- `dotnet/Kuyumcu.Web/Layout/AnaYerlesim.razor`
- `dotnet/Kuyumcu.Web/Layout/UstBar.razor`
- `dotnet/Kuyumcu.Web/Layout/KenarCubugu.razor`
- `dotnet/Kuyumcu.Web/Program.cs`
- `dotnet/Kuyumcu.Web/wwwroot/css/app.css`
- `dotnet/Kuyumcu.Web/wwwroot/index.html (yalnız doğrulanır; değişiklik beklenmiyor)`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `dotnet/Kuyumcu.Web/Pages/Panel.razor`
- `dotnet/Kuyumcu.Web/Pages/Cariler.razor`
- `dotnet/Kuyumcu.Web/Pages/Kasalar.razor`
- `dotnet/Kuyumcu.Web/Pages/Islemler.razor`
- `dotnet/Kuyumcu.Web/Pages/Faturalar.razor`
- `dotnet/Kuyumcu.Web/Pages/VadeTakibi.razor`
- `dotnet/Kuyumcu.Web/Pages/Urunler.razor`
- `dotnet/Kuyumcu.Web/Pages/Banka.razor`
- `dotnet/Kuyumcu.Web/Pages/DenetimIzi.razor`
- `dotnet/Kuyumcu.Web/Pages/Emanet.razor`
- `dotnet/Kuyumcu.Web/Pages/Fiyatlandirma.razor`
- `dotnet/Kuyumcu.Web/Pages/GiderPusulalari.razor`
- `dotnet/Kuyumcu.Web/Pages/Isletme.razor`
- `dotnet/Kuyumcu.Web/Pages/KarZarar.razor`
- `dotnet/Kuyumcu.Web/Pages/Kullanicilar.razor`
- `dotnet/Kuyumcu.Web/Pages/Kur.razor`
- `dotnet/Kuyumcu.Web/Pages/Raporlar.razor`
- `dotnet/Kuyumcu.Web/Pages/Sayim.razor`
- `dotnet/Kuyumcu.Web/Pages/UrunAgaci.razor`
- `dotnet/Kuyumcu.Web/Pages/UrunEtiketleri.razor`
- `dotnet/Kuyumcu.Web/Pages/Uyum.razor`
- `dotnet/Kuyumcu.Web/Pages/VeriDisaAktarim.razor`
- `dotnet/Kuyumcu.Web/Pages/Yetkiler.razor`
- `YENİ: dotnet/Kuyumcu.Web/Layout/YerlesimDurumu.cs`
- `YENİ: dotnet/Kuyumcu.Tests/CekmeceEkranTestleri.cs`
- `YENİ: dotnet/Kuyumcu.Tests/DarEkranCssTestleri.cs`
- `YENİ: dotnet/Kuyumcu.Tests/DarEkranEnvanterTestleri.cs`
- `DOKUNULMAZ: Data/Entities.cs, Data/KuyumcuDbContext.cs, HareketFabrikasi.cs, Modules/*, ModulKayit.cs, denetim.sh, Migrations/* (paket 43'ün sunucu ayak izi YOKTUR)`
- `DOKUNULMAZ: Pages/PusulaBelgesi.razor, Pages/Giris.razor, Pages/Kilavuz.razor (muaf/ayrı düzen), wwwroot/css/belirtecler.css (yeni belirteç gerekmiyor; gerekirse yalnız --kirilim-dar eklenir)`

## Mevzuat dayanağı
- WCAG 2.1 SC 1.4.4 Resize Text / SC 1.4.10 Reflow (W3C Öneri, 2018) — **DOGRULANDI** — Reflow ölçütü 320 CSS px genişlikte iki yönlü kaydırma olmamasını ister; bizim 375 px hedefimiz bundan gevşektir, yani ölçütün kendisi değil pratik hedefimizdir. Yakınlaştırmayı engelleyen viewport meta'sı (user-scalable=no) 1.4.4'ü ihlal eder — testle yasaklandı. Bu bir YASA değil, kabul edilmiş erişilebilirlik standardıdır.
- WCAG 2.1 SC 2.5.5 Target Size (AAA) / WCAG 2.2 SC 2.5.8 (AA, 24x24) — **DOGRULANDI** — 44x44 seçimi AAA ölçütüdür, zorunluluk değil, bizim tercihimizdir; AA sınırı 24x24'tür. Kod yorumunda böyle yazılır, 'mevzuat gereği' denmez.
- Türk mevzuatı — **VARSAYIM** — Paket 43'ün mali/uyum mevzuatıyla HİÇBİR bağı yoktur: para el değiştiren akış, defter, belge ya da eşik içermez. Tevsik/MASAK/vade kapıları bu pakette ne çağrılır ne değiştirilir.

## Riskler
- Faz B/C'de 22 sayfaya öznitelik eklemek, aynı dosyalara dokunan başka paketlerle (34-37 raporlar, 38 KDV, UX-1 düzenlenebilir alanlar) çakışır. AZALTMA: 43 Faz B/C, ilgili sayfayı değiştiren iş paketinden SONRA sıraya alınır; Faz A hiçbir Pages dosyasına dokunmadığı için her zaman paralel koşulabilir.
- `transform` ile gizlenen kenar çubuğu DOM'da kalır: ekran okuyucu kapalı çekmecedeki bağlantıları okuyabilir. AZALTMA kararı Faz A'da: kapalıyken `visibility: hidden` ile transition sonunda gizlenir (`transition: transform .2s, visibility .2s`) — CSS ile çözülür, JS gerekmez. Tam odak tuzağı (focus trap) bu pakette YOKTUR ve bu bilinçli sınırdır.
- Kart moduna dönen sayısal tabloların (KarZarar, Raporlar) sütun hizası bozulup okunurluğu DÜŞÜREBİLİR. AZALTMA: Faz C'de sayfa sayfa karar, kart'a almamak meşru bir sonuç ve gerekçesi dosyaya yazılır.
- 375 px ölçümü tarayıcıyla elle yapılır; otomatik görsel regresyon (Playwright) depoda YOK. Bu yüzden 'yatay kaydırma yok' iddiası her sürümde elle doğrulanmak zorunda. AZALTMA: DarEkranCssTestleri + envanter testi yapısal bozulmayı yakalar; piksel ölçümü canlı doğrulama turunda raporlanır.
- app.css:129'daki eşiği 720→768 taşımak, 721-768 px arası tabletlerde kur bandını GİZLER (bugün görünüyor). Bilinçli kayıp; oylamada itiraz gelirse eşik 768 yerine ayrı bırakılır.

## Açık sorular
- PARITE '19 sayfa' diyor, kodda 27 Pages dosyası var. 19 rakamının kaynağı bulunamadı (belge içinde dökümü yok) — kapsamı 25 sayfa olarak sabitledim; oylama başka bir kapsam isterse düzeltilir.
- Hedef cihaz listesi doğrulanmadı: kurucu/müşteri hangi telefonlarda çalışacağını söylemedi. 375/390/414 px seçimi yaygın iPhone genişliklerinden VARSAYIMLA türetildi; gerçek cihaz matrisi PARITE'de B sınıfıdır.
- Tablet (768-1024) için kenar çubuğu daraltılmış ikon modu istenir mi? Bu pakette YOK; ayrı bir UX maddesi olabilir.
- Çekmece açıkken odak tuzağı ve `inert` desteği bir sonraki erişilebilirlik paketine mi bırakılsın, Faz C'ye mi alınsın — karar oylamaya bırakıldı.
- Paket 44 (PWA) ile sıralama: 43 Faz A'nın 44'ten önce gelmesini öneriyorum; sıralamayı orkestratör onaylamalı.

## Oylama
- **BAŞ MİMAR (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, sevk edilebilirlik, TB-19, ADR uyumu. Kod kanıtı rastgele 5+ pakette elle doğrulandı (19, 20, 25, 27/29, 3/48): grep 'vezne' → 0 satır (K2-19 doğru); SatisController'da satis/satis-fisi uçlarında [RolGerekli] YOK, alis/hurda/transfer'de VAR (SatisModulu.cs:1722-1745 doğru); denetim.sh:53 ExecuteUpdate=4, :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 (alt sınır, -ge :70); cari_bakiyeler yalnız tl_bakiye+has_gram_bakiye (Entities.cs:150-151 doğru); Hareket.ParaBirimi var (Entities.cs:285 doğru); stoklar (tenant,sube,urun)+adet decimal(12,3) (Entities.cs:263-273); DenetimKaydi'nda sube_id/kasa_id YOK (DenetimIzi.cs:26-51 doğru); Ayarlar.Maden'de Celik/Rodyum YOK (Ayarlar.cs:17-20 doğru); FiyatMotoru.cs:48 `urun.AyarId.StartsWith('G')` (spec :49 demiş, ±1 sapma); kur_snapshots'ta gümüş/platin/paladyum kolonu YOK (Entities.cs:105-118 doğru); reyonlar tablosu var ve kendi yorumunda kapsam sınırı yazılı (UrunKategorisi.cs:47-55 doğru); BelgeSahipTuru.Siparis tanımlı + BelgeModulu.cs:93 reddediyor (doğru). Kodda IKiraciVarlik uygulayan varlık sayısı bugün 31 — denetim.sh değişkeni 15 olduğu için '-ge' ile geçiyor; paketlerin bu sayı hakkındaki iddiaları tutarsız (bkz. çapraz bulgu 3).: EVET** — Sunucu ayak izi SIFIR; hiçbir mimari değişmeze, kapıya, deftere dokunmuyor. Faz A tek başına sevk edilebilir ve geniş ekran davranışını bit bit koruyor. Çekmecenin JS'siz, saf Blazor durumu + CSS ile kurulması CSP yüzeyini büyütmüyor; kâğıt muafiyetinin (UX-2 KOŞUL 5) genişletilerek korunması doğru. 'Yatay kaydırma yok' iddiasının hem belge hem <main> ölçümüyle sayıyla kanıtlanması Madde 1'e uygun.
  - Koşullar: Faz A'da kapalı çekmecenin DOM'da odaklanabilir kalması CSS ile kapatılmalı (visibility gizleme, transition sonunda) — spec risk olarak yazmış, Faz A kabul ölçütüdür. · Kapsam sayısının PARITE'deki '19 sayfa' yerine 25 olarak düzeltilmesi belgeye işlensin. · Faz B/C, ilgili Pages dosyasına dokunan iş paketinden SONRA sıraya alınsın (Faz A sunucu ayak izi olmadığı için her zaman paralel koşulabilir).
- **Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: EVET** — Salt istemci; sunucu ayak izi yok; yetki mantığına dokunmuyor; yeni JS/CSP yüzeyi açmıyor. Güvenlik açısından temiz.
  - Koşullar: Kapalı çekmece transition sonunda visibility:hidden (riskte yazılı) — ekran okuyucu ve sekme odağı kapalı menüye ulaşmasın. · index.html'e satır içi script eklenmez; CSP değişmez — DarEkranEnvanterTestleri ile zorlansın (spec böyle).
- **Aşkın — QA / fail-closed kalite kapısı. Ölçütler: her mutasyon kanıtı bir test ADIYLA eşleşiyor mu ve tek katmanı ölçüyor mu (çok katmanlı koruma mutasyonu yakalamaz); DB/HTTP/bUnit dengesi; kararsızlık riski (ateşle-unut, yoklama, WaitForAssertion — mevcut ekran testlerinde WaitForAssertion deseni var: CariEkranTestleri.cs 5, BelgeYukleyiciEkranTestleri.cs 10); canlı doğrulama adımlarında beklenen HTTP kodu + SQL kanıtı; test kiracısı temizliği; kültür tuzağı (TestOrtami.cs/HttpOrtami.cs'de CultureInfo ayarı YOK → tr-TR/invariant davranışı testte açıkça sabitlenmeli); Kod gerçeği doğrulamaları: denetim.sh:66 BEKLENEN_VARLIK=15 ve :70 '-ge'; YetkiModulu.cs:22-34 TEZGAHTAR varsayılanı 'kasa' İÇERİR; YetkiModulu.cs:99-118 TohumlaGerekiyorsa tek marker ile bir kez çalışır; HttpOrtami.cs:78 RemoveAll<IHostedService>; HurdaModulu.cs:67 'yazdırıldı' metni; Bilesenler/ altında BelgeYerlesimi.razor ve wwwroot/css/belge.css YOK; SatisModulu.cs satis/satis-fisi uçlarında RolGerekli yok; MasakKapisi.cs:149 KapiyaTabiTurler ve VadeKapisi.cs:157 TAHSILAT formülü spesifikasyonlarla uyumlu; son migration 20260916060026_BelgeDeposu.: EVET** — Sunucu ayak izi sıfır, mimari değişmez etkilenmiyor; mutasyonlar (M-43-01..08) saf/bUnit testlerle bire bir; kabul ölçütü gözle değil ölçülebilir sayıyla (scrollWidth ≤ clientWidth+1) tanımlı; kâğıt muafiyeti ve viewport yakınlaştırma yasağı testle bekçileniyor; kapsam sayısı belgedeki hatayı (19 vs 27) düzeltiyor.
  - Koşullar: Öneri: CekmeceEkranTestleri 'rota değişince kapanır' bUnit FakeNavigationManager.NavigateTo ile LocationChanged'i tetikler — WaitForAssertion kullanılsın (Degisti olayı senkron değilse kararsız olur). Canlı doğrulama piksel ölçümleri raporda sayfa başına tablo hâlinde yazılsın (25 sayfa × 5 genişlik).

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- Faz A kabul ölçütü (Bertan+Mehmet ortak): kapalı çekmece DOM'da odaklanabilir kalmasın — transition sonunda visibility:hidden (transform .2s, visibility .2s), JS gerekmez; ekran okuyucu ve sekme odağı kapalı menüye ulaşmasın.
- index.html'e satır içi script eklenmez, CSP değişmez — DarEkranEnvanterTestleri ile zorlansın (Mehmet).
- Kapsam PARITE'deki '19 sayfa' yerine 25 sayfa olarak belgeye işlensin; sunucu ayak izi SIFIR (Entities.cs, KuyumcuDbContext.cs, Modules/*, denetim.sh, Migrations DOKUNULMAZ).
- Sıra kuralı: Faz B/C ilgili Pages dosyasına dokunan iş paketinden (34-37, 38, UX-1) SONRA; Faz A hiçbir Pages dosyasına dokunmadığı için her zaman önce/paralel koşulabilir. Dalga planı 43'ü son dalgaya koyarak bunu sağlar.
- Kart moduna dönen sayısal tablolarda (KarZarar, Raporlar) okunurluk düşerse kart'a almamak meşru sonuçtur; Faz C'de sayfa sayfa karar ve gerekçe dosyaya yazılır.
- app.css:129 eşiğinin 720→768 taşınması 721-768 px tabletlerde kur bandını gizler — bilinçli kayıp; itiraz gelirse eşik ayrı bırakılır.
- CekmeceEkranTestleri 'rota değişince kapanır' bUnit FakeNavigationManager.NavigateTo ile WaitForAssertion kullansın (LocationChanged senkron değilse kararsız olur).
- Kabul ölçütü sayıyla: scrollWidth ≤ clientWidth+1; canlı doğrulama piksel ölçümleri 25 sayfa × 5 genişlik tablosu hâlinde raporlansın (Playwright yok, elle ölçüm).
- Odak tuzağı (focus trap) ve inert bu pakette YOKTUR — bilinçli sınır; ayrı erişilebilirlik kalemine bırakılır.
- Paket 44 ile sıra: 43 Faz A, 44'ten ÖNCE gelsin (kırık dar ekran kabuğu telefona kurdurulmaz).
