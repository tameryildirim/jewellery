# Paket 44 — PWA kabuğu (manifest, service worker, ana ekrana ekleme)

> Katman: Katman 7 — Platform · Tahmin: Faz A 1,5–2 gün · Faz B 1–1,5 gün · toplam 2,5–3,5 gün (PARITE tahmini 2–3 gün; fark, yayın çıktısıyla gerçek doğrulama ve dağıtım dosyalarının (nginx/IIS) düzeltilmesinden gelir) gün · Önkoşullar: yok · Kaynak: katman mimarı K7-44 · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 7

## Hedef
Perakende kuyumcu dükkânında tezgâhtaki tablet ve sahibin telefonu uygulamayı tarayıcı adres çubuğu olmadan, ana ekrandaki bir ikondan açabilsin; uygulama açılışı ağ beklemeden gelsin. Vitrin/depo gibi kapsama zayıf noktalarda uygulama beyaz ekranla ölmesin, 'çevrimdışısınız' diyen anlaşılır bir kabuk göstersin. Yeni sürüm yayınlandığında kullanıcı eski sürümde kilitli kalmasın, uygulama içi bir bantla yenilensin. Veri çevrimdışı ÇALIŞMAZ ve bu bilinçli bir sınırdır (K44-1).

## Kodda bugünkü durum
PWA altyapısı SIFIR. wwwroot'ta manifest yok, service worker yok (ls wwwroot: css, favicon.png, icon-192.png, index.html, js, kilavuz.html). index.html:1-45 hiçbir manifest/apple-touch-icon bağlantısı ve hiçbir SW kaydı içermez; yalnız tema.js, dil.js, suruklenebilir.js, indir.js, blazor.webassembly.js yüklenir. Kuyumcu.Web.csproj:1-20'de ne ServiceWorkerAssetsManifest özelliği ne ServiceWorker öğesi vardır. wwwroot/icon-192.png fiziksel olarak vardır (PNG 192x192, doğrulandı) ama hiçbir dosyada referansı yoktur — yetim. favicon.png 32x32'dir. CSP Kuyumcu.Api/Program.cs:281-291'de sabittir; script-src'de 'unsafe-inline' YOKTUR (283) ve manifest-src/worker-src yönergeleri hiç yazılmamıştır. Statik dosyalar Program.cs:316-317 (UseBlazorFrameworkFiles + UseStaticFiles) ile hiçbir OnPrepareResponse/StaticFileOptions olmadan sunulur; SPA geri dönüşü Program.cs:327 MapFallbackToFile("index.html"). Kuyumcu.Api.csproj:22 Kuyumcu.Web'i proje referansı olarak alır — WASM varlıkları API sürecinden sunulur, yani test ana bilgisayarı da bu dosyaları görebilir. deploy/nginx-kuyumcu.conf'ta .webmanifest MIME yok, sw için no-store yok; deploy/iis-web.config staticContent clientCache 7 gündür.

## Kapsam
**İçinde:**
- W3C Web App Manifest (`wwwroot/manifest.webmanifest`) + index.html bağlantısı + iOS apple-touch-icon/başlık meta'ları
- Kurulabilirlik için ikon seti: 192, 512 ve maskable 512 (mevcut icon-192.png yeniden kullanılır)
- Blazor WASM hazır PWA mekanizması: csproj ServiceWorkerAssetsManifest + ServiceWorker öğesi; dev'de geçiş SW, yayında kabuk SW
- SW'nin `/api/`, `/hubs/`, `/saglik`, `/swagger` için ağ-dışı yol açmaması (reddetme listesi)
- CSP'ye manifest-src 'self' + worker-src 'self' eklenmesi
- index.html / service-worker.js / service-worker-assets.js için no-cache başlığı (Kuyumcu.Api StaticFileOptions + nginx + IIS)
- Faz B: yeni sürüm bandı, çevrimdışı göstergesi, Ayarlar'da 'Ana ekrana ekle' düğmesi (beforeinstallprompt)
- Sözlüklere TR+EN metinler; dosya seviyesi + HTTP + bUnit testleri; mutasyon kanıtları

**Dışında:**
- Çevrimdışı VERİ okuma/yazma, çevrimdışı işlem kuyruğu, arka plan senkronu (K44-1; kümülatif mevzuat kapılarını deler)
- Push bildirimi / Notification API (paket 6'nın bildirim çatısına aittir; bu pakette hiç istenmez, izin istemi açılmaz)
- App Store / Play Store yayını (PARITE C sınıfı: geliştirici hesabı gerektirir)
- Dar ekran yerleşim düzeltmeleri (paket 43)
- Yeni API ucu, yeni tablo, yeni migration — bu pakette YOKTUR

## Veri modeli
- Şema değişikliği yok

## Uçlar
- Yeni uç yok

## Servis ve dosyalar
- **YENI** `dotnet/Kuyumcu.Web/wwwroot/manifest.webmanifest` — Faz A. Alanlar: `id: "/"`, `start_url: "/"`, `scope: "/"`, `display: "standalone"`, `orientation: "any"`, `lang: "tr"`, `dir: "ltr"`, `name: "Kuyumcu ERP"`, `short_name: "Kuyumcu"`, `description` (TR), `background_color` ve `theme_color` — DEĞERLER UYDURULMAZ: belirtecler.css'teki `--yuzey-govde` (aydınlık) ve `--vurgu` ham hex karşılıkları okunup yazılır ve test bunu bekçiler. `icons`: 192 (any), 512 (any), 512 (maskable). `categories: ["business","finance"]`. Kısayol (shortcuts) YOK — rota adları paket 43/46 ile oynayabilir, yanlış kısayol kurulu uygulamada kalıcıdır.
- **YENI** `dotnet/Kuyumcu.Web/wwwroot/icon-512.png · icon-512-maskable.png` — Faz A. icon-192.png 192x192 olarak mevcuttur (doğrulandı) ve yeniden kullanılır. 512'lik görsel MEVCUT DEĞİLDİR ve 192'den büyütülmez (bulanık ikon kurulumda kalıcıdır). Kaynak vektör/yüksek çözünürlüklü görselin nereden geleceği acik_sorular'dadır. Maskable sürümde güvenli alan kuralı: içerik merkezde %80 dairenin içinde kalır.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Kuyumcu.Web.csproj` — Faz A. PropertyGroup'a `<ServiceWorkerAssetsManifest>service-worker-assets.js</ServiceWorkerAssetsManifest>`; yeni ItemGroup'a `<ServiceWorker Include="wwwroot\service-worker.js" PublishedContent="wwwroot\service-worker.published.js" />`. Bu iki satır, yayın (publish) sırasında karmalı varlık listesini üretip yayın SW'sini yerine koyar. SDK zaten Microsoft.NET.Sdk.BlazorWebAssembly'dir (csproj:1) — ek paket gerekmez.
- **YENI** `dotnet/Kuyumcu.Web/wwwroot/service-worker.js` — Faz A. GELİŞTİRME sürümü: hiçbir şey önbelleğe almaz, `fetch` olayını hiç dinlemez (ya da yalnız `event.respondWith` yapmadan geçer). Başında yorumla NEDEN boş olduğu yazılır: dev'de önbellek, 'kod değişti ama ekran değişmedi' saatlerini yakar.
- **YENI** `dotnet/Kuyumcu.Web/wwwroot/service-worker.published.js` — Faz A. `importScripts('./service-worker-assets.js')` ile karmalı listeyi alır. install: yalnız kabuk uzantıları (`.html .js .css .wasm .dll .pdb? hayır .png .svg .woff .woff2 .json(blazor.boot)`) beyaz listesinden geçen varlıkları `new Request(url,{integrity:hash, cache:'no-cache'})` ile önbelleğe alır; activate: kendi önbellek öneki dışındaki eski sürüm önbelleklerini siler. fetch: SADECE `event.request.method === 'GET'` VE `request.mode === 'navigate'` olan istekler için önbellekteki `index.html` döner; diğer tüm GET'ler için önbellekte varsa cache-first, yoksa ağ. REDDETME LİSTESİ (en başta, kısa devre): yol `/api/` `/hubs/` `/saglik` `/swagger` ile başlıyorsa VEYA istekte `Authorization` başlığı varsa VEYA yanıt `Vary: Authorization` taşıyorsa hiçbir şey yapılmaz (`return;` → tarayıcı doğal ağ yolunu kullanır) ve HİÇBİR KOŞULDA önbelleğe yazılmaz. Navigate geri dönüşü de bu yollar için verilmez (SPA fallback API 404'ünü maskelemesin). Sürüm damgası: `service-worker-assets.js` içeriğinden gelen `self.assetsManifest.version` önbellek adına katılır.
- **YENI** `dotnet/Kuyumcu.Web/wwwroot/js/pwa.js` — Faz A+B. tema.js deseni: IIFE, 'use strict', `window.kuyumcuPwa = { baslat, kur, yenile, cevrimdisiMi }`. eval/satır içi kod YOK (CSP). `baslat(dotNetRef)`: (1) `navigator.serviceWorker` yoksa sessizce çıkar (iOS özel sekme, eski tarayıcı); (2) `register('/service-worker.js')`; (3) `registration.onupdatefound` → yeni worker `installed` VE `navigator.serviceWorker.controller` varsa .NET'e `GuncellemeHazir()` geri çağrısı; (4) `beforeinstallprompt` olayını yakalar, `preventDefault()` ile saklar ve .NET'e `KurulumMumkun(true)`; `appinstalled` → `KurulumMumkun(false)`; (5) `online`/`offline` olaylarını .NET'e `AgDurumu(bool)` olarak iletir. `kur()`: saklı istemi `prompt()` eder, sonucu döner (istem yoksa false). `yenile()`: bekleyen worker'a `postMessage({tur:'HEMEN_DEVRAL'})` yollar, `controllerchange` olayında `location.reload()`. Yayın SW'sinde bu mesaj `skipWaiting()` çağırır.
- **DEGISTIR** `dotnet/Kuyumcu.Web/wwwroot/index.html` — Faz A. `<head>`e: `<link rel="manifest" href="manifest.webmanifest" />`, `<link rel="apple-touch-icon" sizes="192x192" href="icon-192.png" />`, `<meta name="apple-mobile-web-app-capable" content="yes">`, `<meta name="apple-mobile-web-app-title" content="Kuyumcu">`, `<meta name="theme-color">` (aydınlık/karanlık için iki media'lı satır). `<body>` sonuna, blazor.webassembly.js'ten ÖNCE `<script src="js/pwa.js"></script>`. SATIR İÇİ SCRIPT EKLENMEZ — index.html:28-30'daki uyarı aynen korunur ve kapsamı pwa.js'i de içerecek şekilde güncellenir.
- **YENI** `dotnet/Kuyumcu.Web/Servisler/PwaServisi.cs` — Faz B (iskeleti Faz A'da kurulur). TemaServisi deseni: `IJSRuntime` alır, `DotNetObjectReference<PwaServisi>` ile `kuyumcuPwa.baslat` çağırır. Özellikler: `bool GuncellemeVar`, `bool KurulabilirMi`, `bool Cevrimdisi`; olay `event Action? Degisti`. `[JSInvokable]` metotlar: `GuncellemeHazir()`, `KurulumMumkun(bool)`, `AgDurumu(bool)`. Metotlar: `Task KurAsync()` → `kuyumcuPwa.kur`; `Task YenileAsync()` → `kuyumcuPwa.yenile`. Tüm JS çağrıları try/catch: JS yoksa (bUnit, özel sekme) servis sessizce 'özellik yok' durumunda kalır — hiçbir ekran bu yüzden kırılmaz.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Program.cs` — Faz B. `builder.Services.AddScoped<PwaServisi>();` (mevcut AddScoped bloğuna, satır 13-19 hizasına) ve host kurulduktan sonra `await host.Services.GetRequiredService<PwaServisi>().BaslatAsync();` — mevcut oturum/dil/tema yükleme bloğunun (satır 25-29) DEVAMINA. Sıra önemlidir: tema/dil ilk boyamayı etkiler, PWA etkilemez.
- **YENI** `dotnet/Kuyumcu.Web/Bilesenler/PwaBandi.razor` — Faz B. İki durumlu tek bant (BasariBandi/HataBandi deseni): `GuncellemeVar` → 'Yeni sürüm hazır' + `#pwa-yenile` düğmesi; `Cevrimdisi` → 'Çevrimdışısınız — kayıt yapılamaz, görüntülenen veriler bağlantı kesilmeden öncesine aittir' (uyarı tonu, kapatılamaz). İkisi birden doğruysa çevrimdışı önce gösterilir. Metinler Sozlukler'den.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Layout/AnaYerlesim.razor` — Faz B. `<UstBar />` ile `<main class="ana-icerik">` arasına `<PwaBandi />`. Giriş sayfasında (GirisSayfasinda dalı, satır 7-9) bant GÖSTERİLMEZ — o dal `@Body` dışında bir şey çizmez ve öyle kalır.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Pages/Ayarlar.razor` — Faz B. Tema seçicinin (`#ayar-tema`) yanına 'Uygulama' kartı: `#ayar-pwa-kur` düğmesi (`PwaServisi.KurulabilirMi` false ise gizli değil, DEVRE DIŞI + açıklama: 'Bu tarayıcı ana ekrana eklemeyi desteklemiyor ya da uygulama zaten kurulu'), ve iOS için tek satırlık yönerge metni (Safari beforeinstallprompt desteklemez — Paylaş → Ana Ekrana Ekle).
- **DEGISTIR** `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` — Faz B. Yeni `public sealed class SozlukPwa` (Sozlukler.cs:1025 civarındaki SozlukBelge'den sonra) ve `Sozluk` bileşiminde `public required SozlukPwa Pwa { get; init; }` (satır 1046 sonrası blok). Alanlar: YeniSurumHazir, SimdiYenile, Cevrimdisi, CevrimdisiAciklama, UygulamaBasligi, AnaEkranaEkle, KurulumDesteklenmiyor, IosKurulumYonergesi. TR ve EN sözlük örneklerinin İKİSİNE de eklenir (dosyanın kuralı satır 10-11).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Program.cs` — Faz A. İki değişiklik. (1) CSP dizgesine (281-291) `manifest-src 'self'; ` ve `worker-src 'self'; ` eklenir; başka hiçbir yönerge gevşetilmez. (2) `app.UseStaticFiles()` (317) `StaticFileOptions{ OnPrepareResponse = ctx => { ... } }` ile değiştirilir: dosya adı `service-worker.js`, `service-worker-assets.js`, `index.html` veya `manifest.webmanifest` ise `Cache-Control: no-cache, must-revalidate` yazılır. `.webmanifest` için `ContentTypeProvider` ile `application/manifest+json` eşlemesi eklenir (varsayılan sağlayıcıda yoktur → bugün 404/oktet olarak düşer). `UseBlazorFrameworkFiles()` (316) DEĞİŞMEZ — _framework içeriği karmalı ve uzun önbelleğe uygundur.
- **DEGISTIR** `deploy/nginx-kuyumcu.conf` — Faz A. `types { application/manifest+json webmanifest; }` ve `location = /service-worker.js { add_header Cache-Control "no-cache, must-revalidate"; }` + aynısı `/service-worker-assets.js` ve `/index.html` için. Mevcut 64. satırdaki `expires 7d; Cache-Control public, immutable` bloğu `/assets/` ile sınırlı olduğu için dokunulmaz.
- **DEGISTIR** `deploy/iis-web.config` — Faz A. `staticContent` bloğuna `.webmanifest` mimeMap eklenir; SW ve index.html için `location path` bazlı `clientCache cacheControlMode="DisableCache"` kuralı yazılır. Bugünkü genel `clientCache 7.00:00:00` SW'yi 7 gün dondurur → güncelleme hiç ulaşmaz (K44-6).
- **DEGISTIR** `dotnet/denetim.sh` — Faz A. Mevcut denetim yüzeyi Kuyumcu.Api'nin *.cs dosyalarıdır (API="Kuyumcu.Api", satır 17). Sonuna AYRI bir blok eklenir (kiracı bloğunun sayımlarını bozmadan): wwwroot/js/*.js + service-worker*.js içinde `eval(`/`new Function(` = 0; `service-worker.published.js` içinde `/api/` reddetme listesi dizgesi ≥1; `index.html` içinde `<script>` açılışından sonra gelen satır içi gövde = 0. Yorum satırları elenir (betiğin kendi ilkesi, satır 12).

## Ekranlar
- `(yerleşim geneli, /giris hariç tüm rotalar)` · `dotnet/Kuyumcu.Web/Bilesenler/PwaBandi.razor + Layout/AnaYerlesim.razor` — Faz B. Üst bar altında ince bant: yeni sürüm hazır → 'Şimdi yenile'; çevrimdışı → kayıt yapılamayacağı uyarısı. (menü/yetki: Menü girdisi yok; rol denetimi yok (tüm oturumlarda görünür).)
- `/ayarlar` · `dotnet/Kuyumcu.Web/Pages/Ayarlar.razor` — Faz B. 'Uygulama' kartı: `#ayar-pwa-kur` ile ana ekrana ekleme; desteklenmiyorsa devre dışı + gerekçe; iOS yönergesi. (menü/yetki: Ayarlar sayfasının mevcut yetkisi aynen korunur; yeni [RolGerekli]/[ModulYetkisi] eklenmez.)

## İş kuralları (fail-closed)
- KURAL-44-1 (fail-closed, en kritik): service worker `/api/`, `/hubs/`, `/saglik`, `/swagger` ile başlayan hiçbir isteği önbelleğe YAZMAZ ve hiçbirine önbellekten yanıt VERMEZ. Bu yollar için navigate geri dönüşü de uygulanmaz.
- KURAL-44-2: `Authorization` başlığı taşıyan hiçbir istek/yanıt önbelleğe girmez (kiracı sızıntısının istemci tarafındaki tek olası yolu budur).
- KURAL-44-3: yalnız `method === 'GET'` isteklere dokunulur; POST/PATCH/DELETE hiç ele alınmaz (yazma yolu SW'den geçmez).
- KURAL-44-4: çevrimdışıyken uygulama YANLIŞ VERİ GÖSTERMEZ — kabuk çizilir, API çağrıları hata verir, bant çevrimdışı olduğunu söyler. 'Son bilinen değer' gösterimi yoktur (kur, bakiye ve has işareti eskimiş gösterilirse kullanıcı yanlış fiyat verir).
- KURAL-44-5: index.html'e satır içi `<script>` yazılamaz; kayıt/istem/yenile yalnız adlandırılmış `window.kuyumcuPwa.*` fonksiyonları üzerinden olur; `eval`/`new Function` yasak.
- KURAL-44-6: SW ve index.html no-cache ile sunulur; aksi halde güncelleme akışı ölü koddur.
- KURAL-44-7: manifest `scope` ve `start_url` `/` olmalıdır; alt yola daraltılırsa /giris yönlendirmesi (AnaYerlesim.razor:32-35, kimliksizken /giris'e atar) kurulu uygulamayı kapsam dışına çıkarır ve tarayıcı sekmesi açılır.
- KURAL-44-8: yeni sürüm kullanıcıya SORULMADAN uygulanmaz (`skipWaiting` yalnız kullanıcı 'Şimdi yenile' derse) — kasa fişi yazarken sayfanın altından uygulamanın değişmesi veri kaybıdır.

## Testler
- `dotnet/Kuyumcu.Tests/PwaVarlikTestleri.cs` [saf]
  - manifest.webmanifest geçerli JSON'dur ve zorunlu alanları taşır (name, short_name, start_url="/", scope="/", display="standalone", lang="tr", id)
  - manifest'teki her ikon dosyası diskte VARDIR ve PNG başlığından okunan gerçek boyut, beyan edilen `sizes` ile birebir eşleşir (192, 512, 512-maskable)
  - maskable purpose'lu en az bir 512 ikon vardır (Android adaptif ikon kırpması)
  - manifest theme_color/background_color değerleri belirtecler.css'teki ilgili ham hex ile aynıdır (uydurma renk bekçisi)
  - index.html manifest'e, apple-touch-icon'a ve js/pwa.js'e bağlantı verir; pwa.js blazor.webassembly.js'ten ÖNCE gelir
  - index.html'de satır içi script gövdesi YOKTUR (TasarimBelirtecTestleri.cs:52-60'taki YorumsuzKod yardımcısı yeniden kullanılır)
  - pwa.js ve service-worker*.js dosyalarında `eval(` / `new Function(` yoktur
  - service-worker.published.js reddetme listesi `/api/`, `/hubs/`, `/saglik`, `/swagger` dizgelerini içerir ve `request.method` kontrolü yapar
  - service-worker.js (dev) hiçbir `caches.open` çağrısı içermez
  - Kuyumcu.Web.csproj `ServiceWorkerAssetsManifest` özelliğini ve `PublishedContent` taşıyan `ServiceWorker` öğesini içerir
- `dotnet/Kuyumcu.Tests/HttpBoruHattiTestleri.cs` [HTTP]
  - Mevcut GuvenlikBaslikTestleri sınıfına (satır 271-333) ekleme: CSP `manifest-src 'self'` içerir
  - CSP `worker-src 'self'` içerir
  - CSP script-src hâlâ 'unsafe-inline' İÇERMEZ (mevcut test 309-323 aynen korunur — regresyon bekçisi)
  - GET /manifest.webmanifest → 200 ve Content-Type `application/manifest+json`
  - GET /service-worker.js → 200 ve Cache-Control `no-cache` içerir
  - GET /index.html → Cache-Control `no-cache` içerir
- `dotnet/Kuyumcu.Tests/PwaEkranTestleri.cs` [bUnit]
  - Başlangıçta bant çizilmez (güncelleme yok, çevrimdışı değil) — TemaEkranTestleri.cs:24-31 deseni, JSInterop.Loose + kuyumcuPwa.* sahtelenir
  - GuncellemeHazir() çağrılınca bant görünür ve `#pwa-yenile` düğmesi vardır
  - `#pwa-yenile` tıklanınca TEK bir `kuyumcuPwa.yenile` çağrısı yapılır (argümansız)
  - AgDurumu(false) → çevrimdışı metni görünür; AgDurumu(true) → kaybolur
  - Güncelleme + çevrimdışı birlikte → çevrimdışı metni gösterilir
  - Ayarlar: KurulabilirMi false iken `#ayar-pwa-kur` disabled; true olunca etkin ve tıklanınca `kuyumcuPwa.kur` bir kez çağrılır
  - JS ortamı yokmuş gibi (JSInterop çağrısı fırlatır) servis kurulumu ekranı KIRMAZ

## Mutasyon kanıtları
- M-44-01: service-worker.published.js'teki `/api/` reddetme dalı silinir → PwaVarlikTestleri 'reddetme listesi' testi KIRMIZI.
- M-44-02: manifest'te `icon-512.png` beyanı `sizes: "512x512"` bırakılıp dosya 192'lik ile değiştirilir → 'beyan edilen boyut gerçek boyuta eşittir' testi KIRMIZI.
- M-44-03: CSP'den `manifest-src 'self'` çıkarılır → HTTP CSP testi KIRMIZI.
- M-44-04: StaticFileOptions'taki no-cache dalından `service-worker.js` çıkarılır → 'SW no-cache ile sunulur' testi KIRMIZI.
- M-44-05: index.html'e satır içi `<script>console.log(1)</script>` eklenir → 'satır içi script yoktur' testi KIRMIZI (ve denetim.sh ihlal verir).
- M-44-06: `yenile()` butonu doğrudan `location.reload()` yapacak şekilde JS çağrısı kaldırılır → bUnit 'tek bir kuyumcuPwa.yenile çağrısı' testi KIRMIZI.
- M-44-07: manifest `scope` değeri `/panel` yapılır → manifest alan testi KIRMIZI (KURAL-44-7).

## Canlı doğrulama
- `dotnet publish Kuyumcu.Api -c Release` ile YAYIN çıktısı alınır (SW yalnız yayında etkindir — dev'de doğrulama yapılırsa YANLIŞ YEŞİL okunur; bu paketin en büyük tuzağıdır). Yayın çıktısı `--urls http://localhost:5199` ile koşturulur.
- ⚠ Chrome service worker'ı `localhost` için güvenli bağlam sayar; gerçek cihazda (telefon) HTTPS ZORUNLUDUR. Telefon doğrulaması yapılacaksa alan adı + sertifika gerekir — yoksa doğrulama 'yapılmadı' diye raporlanır, 'çalışıyor' denmez.
- Chrome DevTools → Application → Manifest: hata yok, 'Installability' yeşil; Service Workers: activated ve running; Cache Storage: içinde YALNIZ kabuk varlıkları var — `/api/` ile başlayan TEK BİR girdi bile olmamalı (K44-2 kanıtı, ekran görüntüsü alınır).
- Giriş yapılır, birkaç sayfa gezilir, sonra DevTools → Network → Offline. Beklenen: sayfa yenilendiğinde beyaz ekran DEĞİL kabuk gelir, çevrimdışı bandı görünür, API çağrıları hata verir, hiçbir eski bakiye/kur değeri gösterilmez.
- Çevrimdışıyken bir kayıt denenir → istek başarısız olur, hiçbir şey kuyruklanmaz, tekrar çevrimiçi olunca kendiliğinden gönderilmez (K44-1 kanıtı).
- CSS/metin küçük bir değişiklik yapılıp yeniden yayınlanır; açık sekmede 'Yeni sürüm hazır' bandı çıkar, 'Şimdi yenile' sonrası değişiklik görünür (güncelleme akışı kanıtı).
- `curl -I http://localhost:5199/service-worker.js` → `Cache-Control: no-cache`; `curl -I .../manifest.webmanifest` → `application/manifest+json`. (`-o /dev/null` KULLANILMAZ — §6 tuzağı.)
- Test kiracısı açıldıysa API durdurulup silinir (kur zamanlayıcısı §6 tuzağı).

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/wwwroot/index.html`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Kuyumcu.Web.csproj`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Program.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Layout/AnaYerlesim.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Ayarlar.razor`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/wwwroot/css/app.css`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Program.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Tests/HttpBoruHattiTestleri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/deploy/nginx-kuyumcu.conf`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/deploy/iis-web.config`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/02-mimari/mimari-tanimi.md`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/SONRAKI-OTURUM-PROMPT.md`
- `YENİ: dotnet/Kuyumcu.Web/wwwroot/manifest.webmanifest`
- `YENİ: dotnet/Kuyumcu.Web/wwwroot/service-worker.js`
- `YENİ: dotnet/Kuyumcu.Web/wwwroot/service-worker.published.js`
- `YENİ: dotnet/Kuyumcu.Web/wwwroot/js/pwa.js`
- `YENİ: dotnet/Kuyumcu.Web/wwwroot/icon-512.png`
- `YENİ: dotnet/Kuyumcu.Web/wwwroot/icon-512-maskable.png`
- `YENİ: dotnet/Kuyumcu.Web/Servisler/PwaServisi.cs`
- `YENİ: dotnet/Kuyumcu.Web/Bilesenler/PwaBandi.razor`
- `YENİ: dotnet/Kuyumcu.Tests/PwaVarlikTestleri.cs`
- `YENİ: dotnet/Kuyumcu.Tests/PwaEkranTestleri.cs`
- `YENİ: dokumanlar/02-mimari/adr/0016-pwa-cevrimdisi-yalniz-kabuk.md (ADR + README tablosuna satır)`
- `ÇAKIŞMA NOTU: index.html ve Ayarlar.razor paket 43 ile; Sozlukler.cs hemen her paketle; Kuyumcu.Api/Program.cs paket 45/46 ile; Layout/AnaYerlesim.razor paket 43 ve 46 ile kesişir.`

## Mevzuat dayanağı
- — — **DOGRULANDI** — Bu paketin mevzuat dayanağı YOKTUR ve olmamalıdır: hiçbir vergi/MASAK/tevsik kuralına dokunmaz. Mevzuatla tek teması NEGATİFTİR — çevrimdışı yazma kuyruğu açılmaması, tevsik (30.000 TL) ve MASAK (185.000 TL, bağlantılı işlem kümülatifi) eşiklerinin sunucuda kümülatif hesaplanma zorunluluğundandır (devir belgesi A7 araştırması, doğrulanmış sayılır).
- Blazor WASM PWA şablonu (ServiceWorkerAssetsManifest / ServiceWorker öğesi) — **DOGRULANMALI** — Mekanizmanın .NET 8.0.26 SDK'sında bu adlarla çalıştığı KODDA DOĞRULANMADI (bu görev salt okunurdu, build çalıştırılmadı). Uygulayıcı ajan ilk iş `dotnet publish` ile `service-worker-assets.js` üretimini kanıtlamalı; üretilmiyorsa şablonu `dotnet new blazorwasm --pwa` çıktısıyla karşılaştırmalı.

## Riskler
- EN BÜYÜK RİSK — YANLIŞ YEŞİL: SW yalnız yayın (publish) çıktısında etkindir; geliştirme sunucusunda 'çalışıyor' denirse iddia asılsız olur. Madde 1 gereği doğrulama YAYIN çıktısı üzerinden yapılır.
- Gerçek cihazda HTTPS zorunluluğu: telefonda 'ana ekrana ekle' kanıtı ancak sertifikalı alan adıyla alınabilir. Bugün deploy/nginx conf'unda örnek alan adı vardır (ornek.com); gerçek alan adı yoksa bu kanıt ALINAMAZ ve 'yazıldı, cihazda denenmedi' diye raporlanır (PARITE B sınıfı dürüstlük kuralı).
- Bayat kabuk: kullanıcı 'Şimdi yenile'ye basmazsa eski WASM'la çalışmaya devam eder; API sürümü ilerlemişse 400/404 alır. Azaltma: bandın kapatılamaz olması + KURAL-44-6 no-cache. Sunucu-istemci sürüm uyuşmazlığını tespit eden bir uç bu pakette YOKTUR (kapsam dışı).
- iOS/Safari sınırları: `beforeinstallprompt` desteklenmez, kurulum elle yapılır; Safari'nin SW önbelleği kullanılmayan uygulamalarda ~7 günde temizlenebilir. Bunlar tasarım kararı değil platform gerçeğidir, Ayarlar'daki yönerge metniyle dürüstçe söylenir.
- Ortak tezgâh tableti: kurulu uygulama ikonu 'oturum açık' izlenimi verir. JWT ve oturum davranışı DEĞİŞMEZ; bu pakette oturum süresi/kilit ekranı ele alınmaz (paket 45 TOTP ile birlikte değerlendirilmeli).
- denetim.sh'a JS/HTML yüzeyi eklenmesi, bugüne kadar yalnız Kuyumcu.Api/*.cs sayan betiğin sayaç mantığını bozabilir (satır 17-36). Yeni kontroller AYRI blok olarak, mevcut `denetle` sayımlarına dokunmadan eklenir.

## Açık sorular
- 512x512 ikonun KAYNAK GÖRSELİ nerede? wwwroot/icon-192.png 192x192'dir ve büyütülmeyecektir (K: bulanık ikon kurulu uygulamada kalıcıdır). Yüksek çözünürlüklü logo/vektör yoksa: (a) kurucudan kaynak istenir, (b) geçici olarak yalnız 192 ile yayınlanır (Android kurulabilir sayar ama splash düşük kalitelidir) — hangisi? Uydurma logo üretilmeyecektir.
- manifest `theme_color` için belirtecler.css'teki HANGİ belirteç esas alınacak (`--vurgu` mu, `--yuzey-ust` mü)? Karanlık temada iOS başlık çubuğu rengi tek değerdir; iki temada da kabul edilebilir tek renk seçimi tasarım kararıdır (Ertuğ).
- Gerçek bir HTTPS alan adı/ortam var mı? Yoksa cihaz doğrulaması yapılamaz ve paket 'sevk edildi' değil 'yazıldı, cihazda denenmedi' olarak kapanır — oylama bunu kabul ediyor mu?
- Paket 43'ten önce mi sonra mı sevk edilsin (K44-7 gerekçesiyle sonrası önerilir) — oylama.
- ADR-0016 açılsın mı? Öneri EVET: 'çevrimdışı vaadi yalnız kabuktur, veri kuyruğu yoktur' kararı ileride kesin olarak sorgulanacak bir karardır ve gerekçesi (kümülatif mevzuat eşikleri) kayda geçmelidir.

## Oylama
- **BAŞ MİMAR (Bertan) — tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, sevk edilebilirlik, TB-19, ADR uyumu. Kod kanıtı rastgele 5+ pakette elle doğrulandı (19, 20, 25, 27/29, 3/48): grep 'vezne' → 0 satır (K2-19 doğru); SatisController'da satis/satis-fisi uçlarında [RolGerekli] YOK, alis/hurda/transfer'de VAR (SatisModulu.cs:1722-1745 doğru); denetim.sh:53 ExecuteUpdate=4, :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 (alt sınır, -ge :70); cari_bakiyeler yalnız tl_bakiye+has_gram_bakiye (Entities.cs:150-151 doğru); Hareket.ParaBirimi var (Entities.cs:285 doğru); stoklar (tenant,sube,urun)+adet decimal(12,3) (Entities.cs:263-273); DenetimKaydi'nda sube_id/kasa_id YOK (DenetimIzi.cs:26-51 doğru); Ayarlar.Maden'de Celik/Rodyum YOK (Ayarlar.cs:17-20 doğru); FiyatMotoru.cs:48 `urun.AyarId.StartsWith('G')` (spec :49 demiş, ±1 sapma); kur_snapshots'ta gümüş/platin/paladyum kolonu YOK (Entities.cs:105-118 doğru); reyonlar tablosu var ve kendi yorumunda kapsam sınırı yazılı (UrunKategorisi.cs:47-55 doğru); BelgeSahipTuru.Siparis tanımlı + BelgeModulu.cs:93 reddediyor (doğru). Kodda IKiraciVarlik uygulayan varlık sayısı bugün 31 — denetim.sh değişkeni 15 olduğu için '-ge' ile geçiyor; paketlerin bu sayı hakkındaki iddiaları tutarsız (bkz. çapraz bulgu 3).: KOSULLU** — K44-1 ve K44-2 (API/hubs/saglik/swagger reddetme listesi, Authorization taşıyan hiçbir şey önbelleğe girmez) doğru ve bu ürünün mevzuat kapılarını koruyan tek savunma. Kayıt işleminin satır içi script yerine adlandırılmış pwa.js fonksiyonuyla yapılması mevcut CSP gerçeğine (script-src'de unsafe-inline yok) uygun. Önbellek başlıklarının (no-cache) ve IIS'teki 7 günlük clientCache kusurunun tasarımın parçası sayılması isabetli — aksi hâlde güncelleme akışı ölü kod olurdu.
  - Koşullar: İLK İŞ: `dotnet publish` ile ServiceWorkerAssetsManifest/ServiceWorker mekanizmasının bu SDK'da gerçekten service-worker-assets.js ürettiği KANITLANACAK. Spec bunu 'DOGRULANMALI' işaretlemiş; kanıt alınmadan kod yazılmaz. · HTTPS'li gerçek alan adı yoksa paket 'sevk edildi' değil 'yazıldı, cihazda denenmedi' olarak kapanır ve bu rapora açıkça yazılır (Madde 1). Bu kabul edilmiyorsa paket ertelenir. · K44-1 (çevrimdışı yalnız kabuk, yazma kuyruğu YOK) bir ADR'ye bağlanmalı — gerekçesi (tevsik/MASAK kümülatiflerinin sunucuda hesaplanma zorunluluğu) ileride kesin sorgulanacak bir karardır. ADR numarası için bkz. çapraz bulgu 1. · Paket 43 Faz A'dan SONRA sevk edilsin: dar ekranda kırık bir kabuğu telefona kurdurmak kurmamaktan kötüdür. · denetim.sh'a JS/HTML yüzeyi eklenirken mevcut `denetle` sayaçlarına dokunulmayacak, AYRI blok olarak eklenecek.
- **Mehmet — 06 Güvenlik. Kiracı izolasyonu (IKiraciVarlik/global filtre/KiraciYazmaDenetcisi/ExecuteUpdate beyaz listesi), rol-yetki (RolGerekli TAM eşleşme + ModulYetkisi), fail-closed mevzuat kapıları (tevsik, MASAK kümülatif, gün kilidi, K6 kasa para birimi, vade/limit), alan şifreleme (ADR-0003), denetim izi append-only (ADR-0004), oran sınırı, dosya yükleme yüzeyi, içe aktarım/zehirleme, sır yönetimi. Kod gerçeği doğrulandı (salt okunur): denetim.sh:43 IgnoreQueryFilters=0, :53 ExecuteUpdate/Delete beyaz liste=4 (AuthModulu 3 + KiraciYazmaDenetcisi/DenetimYazmaDinleyicisi), :58 Hareketler.Add=1, :63 VeresiyeKapanisi=2, :66 BEKLENEN_VARLIK=15 ve :70 karşılaştırma '-ge'. SatisModulu.cs:1722 'satis' ve :1744 'satis-fisi' uçlarında RolGerekli YOK (paket 19 iddiası doğru). DenetimController sınıf düzeyi [RolGerekli("SAHIP")] (DenetimModulu.cs:137). AdminController sınıf düzeyi SAHIP (AdminModulu.cs:71). Entities.cs:472-473 KarsiTarafAd/KarsiIban DÜZ METİN. YetkiModulu.cs:41 tek marker '__yapilandirildi__' (K4-F borcu doğru). IFormFile: 0. MarkupString gerçek kullanımı: UrunEtiketleri.razor:128 VE AlanGrafik.razor:37/49 (paket 5'in '=1' sayacı yanlış). OranSiniri politikaları: giris 5/dk, yenile 10/dk, kayit 3/sa, disa-aktarim 10/sa, belge 30/dk, genel 100/dk. Depoda 7 adet *.mutorj artık dosya grep sayaçlarını şişiriyor. NOT: görev metni '27 paket' diyor, gelen JSON'da 16 paket var (19,20,25,27,29,31,32,43,44,45,46,47,48,3,5,6); yalnız gelenler oylandı.: KOSULLU** — Çevrimdışı yazma kuyruğunun reddi mevzuat kümülatifleri için doğru karar. CSP yalnız 'self' ile genişliyor, satır içi script yok. Kiracı verisi önbelleğe girmemesi testle bekçileniyor. Tek eksik HTTP önbellek başlıklarının API tarafı.
  - Koşullar: KURAL-44-1/2 reddetme listesi (/api/, /hubs/, /saglik, /swagger, Authorization başlığı) SW'nin fetch işleyicisinin İLK satırı; PwaVarlikTestleri kaynak taraması + canlıda Cache Storage ekran görüntüsü ('/api' girdisi sıfır) ZORUNLU sevk kanıtı. · API yanıtlarına sunucuda 'Cache-Control: no-store' zaten var mı DOĞRULANSIN (Program.cs okunsun); yoksa bu pakette eklensin — SW dışı tarayıcı HTTP önbelleği de kiracı verisini ortak cihazda bırakabilir. · Yeni sürüm akışı kullanıcı onayı olmadan skipWaiting yapmaz (KURAL-44-8); `postMessage` işleyicisi yalnız 'HEMEN_DEVRAL' türünü kabul eder, origin kontrolü (event.source aynı origin). · Doğrulama YALNIZ publish çıktısında; HTTPS olmadan cihaz kanıtı 'yapılmadı' diye yazılır (Madde 1). · Ortak tablet riski: JWT localStorage'da kalıyor — bu pakette değişmez ama sevk raporuna 'kurulu uygulama oturum kilidi taşımaz, paket 45 ile birlikte değerlendirilecek' notu. · ADR-0016 numarası çakışıyor (bkz. çapraz bulgu).
- **Aşkın — QA / fail-closed kalite kapısı. Ölçütler: her mutasyon kanıtı bir test ADIYLA eşleşiyor mu ve tek katmanı ölçüyor mu (çok katmanlı koruma mutasyonu yakalamaz); DB/HTTP/bUnit dengesi; kararsızlık riski (ateşle-unut, yoklama, WaitForAssertion — mevcut ekran testlerinde WaitForAssertion deseni var: CariEkranTestleri.cs 5, BelgeYukleyiciEkranTestleri.cs 10); canlı doğrulama adımlarında beklenen HTTP kodu + SQL kanıtı; test kiracısı temizliği; kültür tuzağı (TestOrtami.cs/HttpOrtami.cs'de CultureInfo ayarı YOK → tr-TR/invariant davranışı testte açıkça sabitlenmeli); Kod gerçeği doğrulamaları: denetim.sh:66 BEKLENEN_VARLIK=15 ve :70 '-ge'; YetkiModulu.cs:22-34 TEZGAHTAR varsayılanı 'kasa' İÇERİR; YetkiModulu.cs:99-118 TohumlaGerekiyorsa tek marker ile bir kez çalışır; HttpOrtami.cs:78 RemoveAll<IHostedService>; HurdaModulu.cs:67 'yazdırıldı' metni; Bilesenler/ altında BelgeYerlesimi.razor ve wwwroot/css/belge.css YOK; SatisModulu.cs satis/satis-fisi uçlarında RolGerekli yok; MasakKapisi.cs:149 KapiyaTabiTurler ve VadeKapisi.cs:157 TAHSILAT formülü spesifikasyonlarla uyumlu; son migration 20260916060026_BelgeDeposu.: KOSULLU** — Kapsam doğru daraltılmış (çevrimdışı yalnız kabuk, mevzuat kümülatifleri gerekçe), mutasyonlar test adlarına bağlı. Ancak HTTP test iddiaları test ana bilgisayarında doğrulanmamış varsayıma dayanıyor ve SW davranışı testle değil kaynak taramasıyla ölçülüyor.
  - Koşullar: HTTP testleri 'GET /manifest.webmanifest → application/manifest+json' ve 'GET /service-worker.js → no-cache' WebApplicationFactory içinde Kuyumcu.Web wwwroot'unun servis edildiğini varsayıyor; HttpOrtami test ana bilgisayarında UseBlazorFrameworkFiles/UseStaticFiles'ın statik dosyaları bulduğu DOĞRULANMADI. Uygulayıcı ilk iş bu testi yazıp koşsun; 404 alırsa testler saf dosya düzeyine (PwaVarlikTestleri) indirilsin ve HTTP kanıtı canlı doğrulamaya taşınsın — 'yanlış yeşil' değil 'ölçülmedi' raporlansın. · service-worker.published.js yalnız yayında etkin: PwaVarlikTestleri reddetme listesi testi kaynak taramasıdır (davranış değil). Kabul ölçütü: Chrome DevTools Cache Storage ekran görüntüsünde '/api/' girdisi 0 — bu adım canlı doğrulamada zorunlu KANIT olarak işaretlensin, opsiyonel değil. · PwaEkranTestleri 'JS ortamı yokmuş gibi servis kurulumu ekranı kırmaz' JSInterop.Strict modda çağrı fırlatmasıyla ölçülsün; Loose mod hatayı yutar ve testi anlamsızlaştırır. · Cihaz doğrulaması HTTPS gerektiriyor; alan adı yoksa paket 'yazıldı, cihazda denenmedi' ile kapanır — bu durum kabul ölçütünde önceden yazılsın (Madde 1). · denetim.sh'a JS/HTML bloğu eklenirken mevcut sayaçların değişmediği (Hareketler.Add=1, ExecuteUpdate=4, IgnoreQueryFilters=0) kabul ölçütüne yazılsın.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- İLK İŞ (kod yazmadan): `dotnet publish` ile bu SDK'nın ServiceWorkerAssetsManifest/service-worker-assets.js ürettiği KANITLANSIN; kanıt alınmadan kod yazılmaz (Bertan).
- KURAL-44-1/2 reddetme listesi (/api/, /hubs/, /saglik, /swagger, Authorization başlıklı her istek) SW fetch işleyicisinin İLK satırı; kanıt ikili: PwaVarlikTestleri kaynak taraması + canlıda Cache Storage ekran görüntüsünde '/api/' girdisi 0 — bu adım ZORUNLU sevk kanıtı, opsiyonel değil (Mehmet+Aşkın).
- API yanıtlarında sunucu tarafı 'Cache-Control: no-store' var mı Program.cs okunarak doğrulansın; yoksa bu pakette eklensin (ortak cihazda tarayıcı HTTP önbelleği kiracı verisi bırakır).
- Yeni sürüm akışı kullanıcı onayı olmadan skipWaiting yapmaz (KURAL-44-8); postMessage yalnız 'HEMEN_DEVRAL' türünü kabul eder, event.source aynı origin kontrolü.
- Doğrulama YALNIZ yayın (publish) çıktısında; geliştirme sunucusunda 'çalışıyor' denmez. HTTPS'li gerçek alan adı yoksa paket 'sevk edildi' değil 'yazıldı, cihazda denenmedi' olarak kapanır ve bu kabul ölçütüne ÖNCEDEN yazılır (Madde 1, üç mercek ortak).
- ÇELİŞKİ ÇÖZÜMÜ — HTTP testleri: Aşkın WebApplicationFactory'de wwwroot servisinin doğrulanmadığını gösterdi. Çözüm: uygulayıcı ilk iş 'GET /manifest.webmanifest → application/manifest+json' ve 'GET /service-worker.js → no-cache' testini koşsun; 404 gelirse testler dosya düzeyine (PwaVarlikTestleri) inip HTTP kanıtı canlı doğrulamaya taşınır ve 'ölçülmedi' raporlanır — yanlış yeşil yok.
- PwaEkranTestleri JSInterop Strict modda ölçülsün (Loose mod hatayı yutup testi anlamsızlaştırır).
- denetim.sh'a JS/HTML yüzeyi AYRI blok olarak eklensin; mevcut sayaçlar (Hareketler.Add=1, ExecuteUpdate=4, IgnoreQueryFilters=0) değişmediği kabul ölçütüne yazılsın (satır 17-36 sayım mantığı korunur).
- K44-1 (çevrimdışı yalnız kabuk, yazma kuyruğu YOK) ADR'ye bağlansın; ADR-0016 numarası paket 31/32/45/47 ile çakışıyor — numara orkestratörce tekilleştirilsin (çapraz bulgu).
- Sıra ve kapsam dışı: 43 Faz A'dan SONRA sevk; JWT/oturum davranışı DEĞİŞMEZ — 'kurulu uygulama oturum kilidi taşımaz, paket 45 ile birlikte değerlendirilecek' notu sevk raporuna. 512x512 ikon kaynak görseli kurucudan istenir; uydurma logo üretilmez.
