# Paket PWA-1 — PWA kabuğu asgarisi — manifest, ikonlar, ana ekrana ekle, kabuk önbelleği service worker

> Katman: Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tahmin: Faz A 1–1,5 gün · Faz B 0,5–1 gün · toplam 1,5–2,5 gün (paket 44'ün 2,5–3,5 gününden küçültülmüş: çevrimdışı göstergesi, kısayollar ve sürüm uyuşmazlığı işi kapsam dışı). İkon kaynağı yoksa +0 gün (192 ile yayın); gerçek cihaz/HTTPS kanıtı bu tahmine DAHİL DEĞİLDİR. gün · Önkoşullar: Teknik önkoşul YOK — paket bağımsız sevk edilebilir (K44-7: 43 ile teknik bağımlılık yoktur, ayak izleri yalnız index.html ve Ayarlar.razor'da kesişir)., TİCARİ SIRA ÖNERİSİ: paket 43 / UX-2-A2 (375px dar ekran kabuğu) SONRASI. Telefona kurulan ama dar ekranda kırık duran kabuk, kurulmamış olandan kötü izlenim bırakır., MÜHÜR 2 (tek elden): 43 → 44/PWA-1 sırası index.html paylaşımlı olduğu için tek uygulayıcıda (Bertan) birleştirilir; paralel çalışılmaz., Paket 45 (MFA) ile temas: kurulu uygulama oturum kilidi taşımaz; JWT localStorage'da kalır. Bu pakette DEĞİŞMEZ, sevk raporuna not düşülür. · Kaynak: katman mimarı Yeni paketler (hazırlık kurulu tur 1, mühür 2) · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** KOSULLU KABUL · Dalga 7
> ⚠ **VETO:** YENİ: BAŞ MİMAR (Bertan) merceği: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu, önkoşul sırası, paket boyutu (sevk edilebilirlik), dosya çakışması, ADR uyumu. Üç pakette dosya:satır kanıtı bizzat doğrulandı (Entities.cs Tenant bloğu, Program.cs CSP + statik boru hattı, denetim.sh yüzeyi, wwwroot/index.html, AdminModulu plan yazma yolu, AuthServisi.Kayit). NOT (Madde 1): görevde "4 paket" denmesine rağmen bana ulaşan tasarım metni 3 pakettir (F-8, S-1, PWA-1); PWA-1 'mevzuat' bölümünün ortasında kesilmiş, 4. paket nesnesi hiç başlamamıştır. Olmayan tasarıma oy uydurulmadı.: İTİRAZ: 'Teknik önkoşul YOK' ifadesi eksiktir. Barındırılan yayında (Kuyumcu.Api.csproj:22 → Kuyumcu.Web ProjectReference) service-worker-assets.js'in üretildiği DOĞRULANMAMIŞTIR; üretilmiyorsa karma listesi ve güncelleme akışı ölü doğar. 'dotnet publish -c Release' kanıtı alınmadan Faz A başlamamalıdır.

## Hedef
Uygulamayı telefonda "ana ekrana ekle" ile kurulabilir hâle getirmek: W3C manifest + ikonlar + CSP uyumlu (satır içi script YOK) service worker yalnız uygulama kabuğunu önbelleğe alır. Kiracı verisi ve /api yanıtı HİÇBİR koşulda önbelleğe girmez. Paket 44'ün küçültülmüş, tek elden sevk edilebilir asgarisi.

## Kodda bugünkü durum
PWA altyapısı sıfır. wwwroot içeriği: css, favicon.png (32x32), icon-192.png (PNG 192x192, doğrulandı ama hiçbir dosyada referansı yok — yetim), index.html, js, kilavuz.html. index.html:1-39'da manifest/apple-touch-icon/SW kaydı yok; js/tema.js, dil.js, suruklenebilir.js, indir.js, _framework/blazor.webassembly.js yüklenir; index.html:29-31 "Satır içi <script> YASAK" der. CSP Kuyumcu.Api/Program.cs:281-291 sabit; :283 script-src 'self' 'wasm-unsafe-eval' (unsafe-inline YOK), manifest-src/worker-src hiç yazılmamış. Statikler Program.cs:316-317 (UseBlazorFrameworkFiles + UseStaticFiles, StaticFileOptions YOK), SPA geri dönüşü :327 MapFallbackToFile("index.html"). deploy/nginx-kuyumcu.conf:61-68'de .webmanifest MIME yok, SW için no-cache yok, /assets/ 7 gün immutable. deploy/iis-web.config staticContent clientCache 7 gün (DOĞRULANMALI: dosya bu oturumda açılmadı, paket 44 spesifikasyonu §Kodda bugünkü durum böyle diyor). Mimari kararlar K44-1…K44-8: dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md:281-288.

## Kapsam
**İçinde:**
- manifest.webmanifest (id/start_url/scope/display standalone/lang tr/name/short_name/theme_color/background_color/icons) + index.html <link rel=manifest>
- iOS: apple-touch-icon + apple-mobile-web-app-capable + apple-mobile-web-app-title meta'ları; iki media'lı theme-color satırı
- İkonlar: mevcut icon-192.png yeniden kullanılır; 512 + 512-maskable kaynak görsel varsa eklenir (yoksa manifest yalnız var olan ikonu bildirir — bkz. açık sorular)
- Service worker: Microsoft Blazor WASM mekanizması (csproj ServiceWorkerAssetsManifest + ServiceWorker öğesi); dev sürümü boş, yayın sürümü karmalı kabuk önbelleği
- SW kaydı wwwroot/js/pwa.js içinde adlandırılmış kuyumcuPwa.baslat() ile (tema.js deseni) — satır içi script EKLENMEZ (CSP)
- Reddetme listesi fetch işleyicisinin İLK satırı: /api/, /hubs/, /saglik, /swagger ve Authorization taşıyan hiçbir istek önbelleğe girmez/önbellekten yanıtlanmaz
- CSP'ye manifest-src 'self' + worker-src 'self' eklenmesi (başka hiçbir yönerge gevşetilmez)
- no-cache başlığı + application/manifest+json MIME: Kuyumcu.Api StaticFileOptions, deploy/nginx-kuyumcu.conf, deploy/iis-web.config
- denetim.sh'a ayrı JS/HTML bloğu: eval/new Function = 0, reddetme listesi ≥1, index.html satır içi script gövdesi = 0
- Faz B: Ayarlar ekranında 'Ana ekrana ekle' düğmesi (beforeinstallprompt) + 'Yeni sürüm hazır — Şimdi yenile' bandı

**Dışında:**
- Çevrimdışı YAZMA: defter yazma yolu HareketFabrikasi.Ekle kur snapshot, gün kilidi, tevsik ve MASAK kümülatifi ister; istemcide hesaplanamaz (K44-1). Kuyruk YOK.
- API/kiracı verisinin önbelleğe alınması — her biçimiyle yasak (K44-2)
- 375px dar ekran kabuğu, kart modu, mobil yerleşim → paket 43 / UX-2-A2
- Push bildirimi, arka plan eşitleme (Background Sync), periodic sync
- manifest shortcuts (rota adları 43/46 ile oynayabilir; yanlış kısayol kurulu uygulamada kalıcıdır)
- Çevrimdışı göstergesi/rozetler, sürüm uyuşmazlığı tespit ucu (sunucu-istemci sürüm karşılaştırması)
- Kurulu uygulamada oturum kilidi / JWT saklama değişikliği → paket 45 (MFA) ile değerlendirilir
- Splash ekranı özelleştirme, App Store/Play paketleme (TWA), yeni ikon/logo tasarımı

## Veri modeli
| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |
|---|---|---|---|---|
| — | YENİ TABLO YOK, MIGRATION YOK. Paket tamamen statik varlık + boru hattı başlığı + istemci JS düzeyindedir; hiçbir IKiraciVarlik varlığı eklenmez/değişmez, DenetimKaydi şeması değişmez. | — | — | — |

## Uçlar
| Fiil | Yol | Roller | Açıklama | Kapılar |
|---|---|---|---|---|
| GET | `/manifest.webmanifest` | Anonim (kimlik gerekmez; kiracıdan bağımsız statik) | Statik dosya. Content-Type application/manifest+json (varsayılan sağlayıcıda YOK → ContentTypeProvider ile eklenir, aksi hâlde 404/oktet). Cache-Control: no-cache, must-revalidate. | Yok — kiracı/kapı yüzeyine dokunmaz |
| GET | `/service-worker.js` | Anonim | Statik dosya; dev sürümü hiçbir şey önbelleğe almaz, yayında service-worker.published.js ile değiştirilir. Cache-Control: no-cache, must-revalidate (yoksa güncelleme hiç ulaşmaz — K44-6). | Yok |
| GET | `/service-worker-assets.js · /index.html · /icon-192.png · /icon-512*.png` | Anonim | service-worker-assets.js ve index.html no-cache; ikonlar normal statik. _framework/* karmalı olduğu için UseBlazorFrameworkFiles DEĞİŞMEZ. | Yok |
| — | `YENİ API UCU YOK` | — | Hiçbir Modules/*Modulu.cs dosyasına uç eklenmez; [RolGerekli]/[ModulYetkisi] yüzeyi ve TevsikKapisi/MasakKapisi/GunKapanisiKapisi/VadeKapisi aynen kalır. | — |

## Servis ve dosyalar
- **YENI** `dotnet/Kuyumcu.Web/wwwroot/manifest.webmanifest` — Faz A. id/start_url/scope "/", display standalone, orientation any, lang tr, dir ltr, name "Kuyumcu ERP", short_name "Kuyumcu", TR description, categories business+finance. theme_color/background_color UYDURULMAZ: css/belirtecler.css'teki --vurgu ve --yuzey-govde ham hex'leri okunup yazılır, test bekçiler. shortcuts YOK.
- **YENI** `dotnet/Kuyumcu.Web/wwwroot/icon-512.png · icon-512-maskable.png` — Faz A, KAYNAK GÖRSEL VARSA. icon-192.png yeniden kullanılır; 192'den büyütme YASAK (bulanık ikon kurulu uygulamada kalıcıdır). Maskable'da içerik merkezde %80 daire içinde kalır. Kaynak yoksa manifest yalnız 192 bildirir ve rapora 'düşük kaliteli splash' notu düşülür.
- **YENI** `dotnet/Kuyumcu.Web/wwwroot/service-worker.js` — Faz A. GELİŞTİRME sürümü: hiçbir şey önbelleğe almaz, fetch'i respondWith'siz geçer. Başında NEDEN boş olduğu yorumla yazılır (dev önbelleği 'kod değişti, ekran değişmedi' saatleri yakar).
- **YENI** `dotnet/Kuyumcu.Web/wwwroot/service-worker.published.js` — Faz A. importScripts('./service-worker-assets.js'); install: beyaz liste uzantıları (.html .js .css .wasm .dll .png .svg .woff2 .json) integrity+cache:'no-cache' ile önbelleğe. activate: eski sürüm önbelleklerini siler (ad = önek + assetsManifest.version). fetch: reddetme listesi İLK satır; sonra navigate→index.html, diğer GET cache-first.
- **YENI** `dotnet/Kuyumcu.Web/wwwroot/js/pwa.js` — Faz A+B. tema.js deseni: IIFE, 'use strict', window.kuyumcuPwa = { baslat, kur, yenile }. eval/new Function YOK. baslat(): serviceWorker yoksa sessiz çık; register('/service-worker.js'); updatefound + controller varsa .NET'e GuncellemeHazir(). Faz B: beforeinstallprompt yakala/preventDefault/sakla → KurulumMumkun(true); kur() prompt(); yenile() postMessage HEMEN_DEVRAL + controllerchange→reload.
- **DEGISTIR** `dotnet/Kuyumcu.Web/wwwroot/index.html` — Faz A. <head>: link rel=manifest, apple-touch-icon 192, apple-mobile-web-app-capable/-title, iki media'lı theme-color. <body> sonunda blazor.webassembly.js'ten ÖNCE <script src="js/pwa.js"></script>. Satır 29-31'deki 'satır içi script YASAK' uyarısı korunur, kapsamı pwa.js'i içerecek şekilde genişletilir. MÜHÜR 2: bu dosyaya 43/44 ile birlikte TEK ELDEN dokunulur.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Kuyumcu.Web.csproj` — Faz A. <ServiceWorkerAssetsManifest>service-worker-assets.js</ServiceWorkerAssetsManifest> + <ServiceWorker Include="wwwroot\service-worker.js" PublishedContent="wwwroot\service-worker.published.js" />. Karma listesi ELLE tutulmaz (K44-3: sürüm kayması üretir).
- **DEGISTIR** `dotnet/Kuyumcu.Api/Program.cs` — Faz A, iki nokta. (1) CSP dizgesine (281-291) "manifest-src 'self'; worker-src 'self'; " eklenir — CSP3'te default-src'ye düşerler ama iOS/Safari mirası sürüm bağımlıdır (K44-5); başka yönerge gevşetilmez. (2) :317 UseStaticFiles → StaticFileOptions: service-worker.js/service-worker-assets.js/index.html/manifest.webmanifest için no-cache, must-revalidate + .webmanifest→application/manifest+json. :316 DEĞİŞMEZ.
- **DEGISTIR** `deploy/nginx-kuyumcu.conf` — Faz A. types { application/manifest+json webmanifest; } ve location = /service-worker.js | /service-worker-assets.js | /index.html için add_header Cache-Control "no-cache, must-revalidate". Mevcut 61-65'teki expires 7d/immutable bloğu /assets/ ile sınırlı olduğundan dokunulmaz.
- **DEGISTIR** `deploy/iis-web.config` — Faz A. staticContent'e .webmanifest mimeMap; SW dosyaları ve index.html için location path bazlı clientCache cacheControlMode="DisableCache". Bugünkü genel clientCache 7.00:00:00 SW'yi 7 gün dondurur → güncelleme hiç ulaşmaz (K44-6). DOĞRULANMALI: dosyanın bugünkü içeriği uygulayıcı tarafından okunsun.
- **DEGISTIR** `dotnet/denetim.sh` — Faz A. Betiğin denetim yüzeyi bugün yalnız Kuyumcu.Api/*.cs (satır 17 API=...). SONA AYRI blok eklenir: wwwroot/js/*.js + service-worker*.js içinde eval(/new Function( = 0; service-worker.published.js'te '/api/' reddetme dizgesi ≥1; index.html'de satır içi script gövdesi = 0. Mevcut sayaçlar (Hareketler.Add=1, ExecuteUpdate=4, IgnoreQueryFilters=0) DEĞİŞMEZ — kabul ölçütü.
- **DEGISTIR** `dotnet/Kuyumcu.Web/Pages/Ayarlar.razor (+ Kuyumcu.Core/I18n/Sozlukler.cs)` — Faz B. 'Ana ekrana ekle' düğmesi: kuyumcuPwa.kur() çağırır; beforeinstallprompt gelmediyse disabled + sebebi yazılı (UX-S30: 'Tarayıcınız kurulumu desteklemiyor ya da uygulama zaten kurulu'). 'Yeni sürüm hazır' bandı MainLayout'ta. Tüm metinler sözlükten (UX-S31), ham anahtar ekrana basılmaz.

## Ekranlar
- `— (Faz A ekransızdır)` · `dotnet/Kuyumcu.Web/wwwroot/index.html` — Faz A'da kullanıcıya görünen yeni ekran YOKTUR: kurulum tarayıcının kendi menüsünden ('Ana ekrana ekle' / 'Uygulamayı yükle') yapılır. Bu, Faz A'yı tek başına sevk edilebilir kılar; sessiz ve zararsızdır. (menü/yetki: Menü değişikliği yok)
- `/ayarlar` · `dotnet/Kuyumcu.Web/Pages/Ayarlar.razor` — Faz B. 'Uygulama' bölümüne tek düğme: 'Ana ekrana ekle' → kuyumcuPwa.kur(). DoD: düğme ya istek/JS çağrısı atar ya da disabled + sebep metni (UX-S30). Basit modda da görünür — kurulum bir tercihtir, yetki değildir (denetim.sh arayuz_modu ilkesi). Yeni veri girişi olmadığı için Cekmece/SatirIciDuzenle kalıbı devreye girmez. (menü/yetki: Mevcut Ayarlar menü yetkisi aynen; yeni [RolGerekli]/[ModulYetkisi] YOK)
- `(tüm rotalar)` · `dotnet/Kuyumcu.Web/Layout/MainLayout.razor` — Faz B. 'Yeni sürüm hazır — Şimdi yenile' bandı; yalnız SW güncellemesi hazır olduğunda çizilir, kullanıcı onayı olmadan skipWaiting YAPILMAZ (KURAL-44-8). Band kapatılamaz (bayat kabuk riski). Başlangıçta HİÇ çizilmez. (menü/yetki: Yetki yok — herkese görünür)

## İş kuralları (fail-closed)
- KURAL-PWA-1 (fail-closed): SW fetch işleyicisinin İLK satırı reddetme listesidir; yol /api/, /hubs/, /saglik, /swagger ile başlıyorsa VEYA istek Authorization taşıyorsa VEYA yanıt Vary: Authorization taşıyorsa `return;` — ne yanıtlanır ne yazılır. Sıra tersine dönerse kural ölür, bu yüzden testle bekçilenir.
- KURAL-PWA-2: Önbellekte kiracıya ait tek bayt yoktur; yalnız kimlik istemeyen, kiracıdan bağımsız statik kabuk varlıkları. Ortak cihazda oturum kapandıktan sonra veri sızma yolu açılmaz (ADR-0001 izolasyon yüzeyi genişlemez).
- KURAL-PWA-3: Çevrimdışı YAZMA yoktur. Navigate geri dönüşü /api/ yollarına verilmez — SPA fallback API 404'ünü maskelemesin.
- KURAL-PWA-4: Satır içi <script> eklenmez; kayıt adlandırılmış kuyumcuPwa.baslat() ile yapılır. CSP'de script-src 'unsafe-inline' AÇILMAZ; yalnız manifest-src 'self' ve worker-src 'self' eklenir.
- KURAL-PWA-5: service-worker.js, service-worker-assets.js, index.html, manifest.webmanifest no-cache, must-revalidate ile sunulur; aksi hâlde güncelleme akışı ölü koddur (IIS'teki 7 gün buna kasıtlı karşı örnektir).
- KURAL-PWA-6: postMessage işleyicisi yalnız {tur:'HEMEN_DEVRAL'} kabul eder ve aynı origin kontrolü yapar; başka mesaj türü sessizce yok sayılır.
- KURAL-PWA-7: SW kaydı başarısız olursa (eski tarayıcı, iOS özel sekme, HTTP) uygulama HİÇ bozulmaz — pwa.js sessizce çıkar, hiçbir ekran kırılmaz.
- KURAL-PWA-8: İkon uydurulmaz ve büyütülmez; manifest yalnız diskte fiziksel olarak var olan ve bildirdiği boyutta olan ikonları listeler (test bunu ölçer).

## Testler
- `dotnet/Kuyumcu.Tests/PwaVarlikTestleri.cs` [saf (kaynak taraması; TasarimBelirtecTestleri.cs:30-40 CssKaynak.Kok() ve YorumsuzKod yardımcıları yeniden kullanılır)]
  - Manifest_GecerliJson_ZorunluAlanlariTasir — id/start_url/scope/display/name/short_name/icons
  - Manifest_TemaRenkleri_BelirteclerCssIleAyni — theme_color=--vurgu, background_color=--yuzey-govde ham hex
  - Manifest_BildirilenHerIkon_DisktedeVarVeBildirilenBoyutta — PNG başlığından ölçülür, uydurma ikon yakalanır
  - IndexHtml_ManifestVeAppleTouchIcon_Baglantilari_Var
  - IndexHtml_SatirIciScriptGovdesi_Yok — mevcut CSP uyarısının kod karşılığı
  - IndexHtml_PwaJs_BlazorWebassemblyJsten_Once_Yuklenir
  - ServiceWorkerPublished_ReddetmeListesi_FetchIsleyicisinin_IlkSatirinda — /api/, /hubs/, /saglik, /swagger, Authorization
  - ServiceWorkerPublished_ApiYoluna_CachePut_Cagrisi_Yok
  - PwaJs_Eval_Ve_NewFunction_Icermez
  - Csproj_ServiceWorkerAssetsManifest_Ve_ServiceWorker_Ogesi_Tanimli
- `dotnet/Kuyumcu.Tests/HttpBoruHattiTestleri.cs (mevcut GuvenlikBaslikTestleri sınıfına ekleme, ~271-333)` [HTTP (WebApplicationFactory, HttpOrtami)]
  - Csp_ManifestSrc_Self_Icerir
  - Csp_WorkerSrc_Self_Icerir
  - Csp_ScriptSrc_UnsafeInline_Icermez — gevşetme regresyon bekçisi
  - Manifest_Ucu_ApplicationManifestJson_Doner — ÖNCE KOŞULSUN: 404 gelirse (test ana bilgisayarında wwwroot servis edilmiyorsa) test saf düzeye iner ve 'ölçülmedi' raporlanır, yanlış yeşil YOK
  - ServiceWorkerJs_NoCache_Basligi_Ile_Sunulur
  - IndexHtml_NoCache_Basligi_Ile_Sunulur
  - ApiYanitlari_CacheControl_NoStore_Tasir — DOĞRULANMALI: bugün var mı Program.cs okunarak saptanır, yoksa bu pakette eklenir (Mehmet koşulu)
- `dotnet/Kuyumcu.Tests/PwaEkranTestleri.cs` [bUnit (JSInterop STRICT mod — Loose hatayı yutar ve testi anlamsızlaştırır, Aşkın koşulu)]
  - Baslangicta_GuncellemeBandi_Cizilmez
  - GuncellemeHazir_Cagrilinca_Band_Cizilir_Ve_Kapatilamaz
  - SimdiYenile_Dugmesi_kuyumcuPwa_yenile_Cagirir (Faz B)
  - AnaEkranaEkle_Dugmesi_KurulumMumkunDegilse_Disabled_Ve_Sebebi_Yazili (UX-S30, Faz B)
  - AnaEkranaEkle_Dugmesi_kuyumcuPwa_kur_Cagirir (Faz B)
  - JsOrtami_Yokken_Ayarlar_Ekrani_Kirilmaz
- `dotnet/denetim.sh (betik testi — CI kapısı)` [betik]
  - Yeni JS/HTML bloğu eklendikten sonra çıkış kodu 0 (TEMİZ)
  - Mevcut sayaçlar değişmedi: Hareketler.Add=1, ExecuteUpdate=4, IgnoreQueryFilters=0, BEKLENEN_VARLIK -ge 15

## Mutasyon kanıtları
- M-PWA-01: service-worker.published.js'teki /api/ reddetme dalı silinir → PwaVarlikTestleri.ServiceWorkerPublished_ReddetmeListesi_FetchIsleyicisinin_IlkSatirinda KIRMIZI.
- M-PWA-02: Reddetme listesi fetch işleyicisinin sonuna taşınır (davranışsal olarak ölü) → aynı test KIRMIZI (sıra ölçülür, varlık değil).
- M-PWA-03: CSP'den manifest-src 'self' çıkarılır → HttpBoruHattiTestleri.Csp_ManifestSrc_Self_Icerir KIRMIZI.
- M-PWA-04: StaticFileOptions no-cache dalından service-worker.js çıkarılır → ServiceWorkerJs_NoCache_Basligi_Ile_Sunulur KIRMIZI.
- M-PWA-05: manifest.webmanifest'te theme_color elle başka bir hex'e çevrilir → Manifest_TemaRenkleri_BelirteclerCssIleAyni KIRMIZI.
- M-PWA-06: manifest'e diskte olmayan icon-512.png girdisi eklenir → Manifest_BildirilenHerIkon_DisktedeVarVeBildirilenBoyutta KIRMIZI.
- M-PWA-07: index.html'e satır içi <script>navigator.serviceWorker.register(...)</script> eklenir → IndexHtml_SatirIciScriptGovdesi_Yok KIRMIZI ve denetim.sh KİRLİ.
- M-PWA-08: pwa.js'te register çağrısı try/catch'siz bırakılıp serviceWorker yokluğu kontrolü silinir → JsOrtami_Yokken_Ayarlar_Ekrani_Kirilmaz KIRMIZI.
- M-PWA-09: postMessage işleyicisi her mesaj türünde skipWaiting() çağıracak şekilde değiştirilir → PwaVarlikTestleri 'yalnız HEMEN_DEVRAL kabul edilir' iddiası KIRMIZI (bu test PwaVarlikTestleri'ne eklenir).
- M-PWA-10: CSP'ye script-src 'unsafe-inline' eklenir → Csp_ScriptSrc_UnsafeInline_Icermez KIRMIZI.

## Canlı doğrulama
- dotnet publish -c Release ile YAYIN çıktısı alınır (service-worker.published.js yalnız yayında etkindir; dev sunucuda kanıt alınmaz).
- curl -i http://localhost:5198/manifest.webmanifest → 200 + Content-Type: application/manifest+json + Cache-Control: no-cache. (-o /dev/null KULLANILMAZ.)
- curl -i http://localhost:5198/service-worker.js → 200 + Cache-Control: no-cache, must-revalidate.
- curl -i http://localhost:5198/index.html → 200 + Cache-Control: no-cache; CSP başlığında manifest-src 'self' ve worker-src 'self' görünür, script-src'de unsafe-inline YOK.
- curl -i http://localhost:5198/icon-192.png → 200 image/png; manifest'te bildirilen her ikon için tekrarlanır.
- ZORUNLU SEVK KANITI: Chrome DevTools → Application → Cache Storage ekran görüntüsünde '/api/' ile başlayan girdi sayısı 0; Service Workers panelinde 'activated and is running'.
- Gerçek kiracıyla giriş yapılır, birkaç ekran gezilir, sonra Cache Storage yeniden bakılır → hâlâ 0 API girdisi (Authorization taşıyan istekler sızmamış).
- Çevrimdışı tatbikat: ağ kesilir, kurulu uygulama açılır → kabuk yüklenir, veri ekranları 'bağlantı yok' hatası verir (sessiz eski veri GÖSTERMEZ).
- SQL kanıtı: SELECT COUNT(*) FROM denetim_kayitlari WHERE ...> (paket başlangıç zamanı) → bu paket hiçbir yazma üretmediği için beklenen davranış değişmez; kiracı verisinde delta YOK.
- Test kiracısı kullanıldıysa doğrulama sonunda silinir.
- HTTPS/gerçek alan adı yoksa telefonda 'ana ekrana ekle' kanıtı ALINAMAZ → rapora 'yazıldı, cihazda denenmedi' diye açıkça yazılır (Madde 1, PARITE B dürüstlük kuralı). Uydurma ekran görüntüsü yok.

## Dosya ayak izi (çakışma planı için)
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/wwwroot/index.html (PAYLAŞIMLI: 43, 44, UX-2)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/wwwroot/manifest.webmanifest (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/wwwroot/service-worker.js (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/wwwroot/service-worker.published.js (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/wwwroot/js/pwa.js (YENİ)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/wwwroot/icon-512.png · icon-512-maskable.png (YENİ, kaynak görsel varsa)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Kuyumcu.Web.csproj`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Api/Program.cs (PAYLAŞIMLI: 43, 44, 45, 47, UX-3)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Web/Pages/Ayarlar.razor · Layout/MainLayout.razor (Faz B; 43/45 ile paylaşımlı)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/Kuyumcu.Core/I18n/Sozlukler.cs (Faz B metinleri; neredeyse tüm paketlerle paylaşımlı)`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dotnet/denetim.sh · dotnet/Kuyumcu.Tests/PwaVarlikTestleri.cs · PwaEkranTestleri.cs (YENİ) · HttpBoruHattiTestleri.cs`
- `/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/deploy/nginx-kuyumcu.conf · deploy/iis-web.config`

## Fazlar
- **Faz A — sevk edilebilir asgari (kurulabilirlik + kabuk önbelleği)** (Dalga 7, 43/UX-2-A2 sonrası; tek elden (MÜHÜR 2)): manifest.webmanifest + ikon bildirimi + index.html <head>/<script src=js/pwa.js> + csproj SW öğeleri + service-worker.js/.published.js (reddetme listesi ilk satır) + pwa.js kaydı + CSP manifest-src/worker-src + StaticFileOptions no-cache & .webmanifest MIME + nginx/IIS düzeltmeleri + denetim.sh bloğu + PwaVarlikTestleri + HTTP başlık testleri. Kullanıcıya görünen yeni ekran yok; tarayıcı menüsünden kurulur, sessiz ve zararsız.
- **Faz B — kullanıcıya görünen kurulum ve güncelleme akışı** (Faz A yeşil + canlı Cache Storage kanıtı alındıktan SONRA): Ayarlar'da 'Ana ekrana ekle' düğmesi (beforeinstallprompt, desteklenmiyorsa disabled + sebep — UX-S30), MainLayout'ta kapatılamaz 'Yeni sürüm hazır — Şimdi yenile' bandı (kullanıcı onayı olmadan skipWaiting YOK), PwaEkranTestleri (JSInterop Strict), sözlük metinleri. Faz A'sız anlamsızdır; Faz A Faz B'siz çalışır.
## Mevzuat dayanağı
- Çevrimdışı yazma yasağı — 7194/VUK tevsik haddi (30.000 TL) ve MASAK kümülatif eşiği (185.000 TL) kümülatif hesap ister — **YÜKSEK (kod kanıtı: tek yazma yolu HareketFabrikasi.Ekle; kapılar TevsikKapisi/MasakKapisi; mimari karar K44-1, MIMARI-KALAN-PAKETLER-2026-09-15.md:281)** — İstemcide kümülatif hesaplanamaz; çevrimdışı kuyruk eşiği sessizce delerdi. Bu yüzden çevrimdışı vaadi YALNIZ KABUK. Haddin güncel rakamı DOĞRULANMALI (bu oturumda mevzuat metni açılmadı).
- KVKK — ortak cihazda kişisel veri kalıntısı — **ORTA-YÜKSEK (ilke net; bu pakette kiracı verisi önbelleğe hiç girmediği için yeni yüzey açılmaz — K44-2)** — Kabuk varlıkları kimlik istemeyen statiklerdir. Ayrıca API yanıtlarında Cache-Control: no-store bulunup bulunmadığı DOĞRULANMALI; yoksa bu pakette eklenmesi Mehmet'in koşuludur.
- W3C Web App Manifest + Service Worker spesifikasyonları (hukuki mevzuat değil, teknik standart) — **YÜKSEK** — Manifest alanları ve SW yaşam döngüsü standarda göre yazılır; iOS'ta manifest-src'nin default-src'den miras alınması sürüm bağımlı olduğu için açıkça yazılır (K44-5).

## Riskler
- 512x512 kaynak görsel yoksa Android kurulumu düşük kaliteli splash verir. Azaltma: 192 ile yayınla + rapora dürüstçe yaz. Uydurma logo ÜRETİLMEZ, 192 büyütülmez.
- HTTPS/gerçek alan adı olmadan telefonda 'ana ekrana ekle' kanıtı alınamaz (deploy conf'unda örnek alan adı ornek.com var). Sonuç 'yazıldı, cihazda denenmedi' diye raporlanır.
- HTTP testleri WebApplicationFactory'de wwwroot'un servis edildiğini varsayar; DOĞRULANMADI. Uygulayıcı ilk iş manifest HTTP testini koşsun, 404 gelirse testler saf düzeye iner ve 'ölçülmedi' raporlanır — yanlış yeşil yok (Aşkın koşulu).
- SW davranışı kaynak taramasıyla ölçülür, çalışma zamanıyla değil. Bu bilinçli bir sınırdır; kapatıcı kanıt canlı Cache Storage ekran görüntüsüdür ve ZORUNLUdur.
- Bayat kabuk: kullanıcı 'Şimdi yenile'ye basmazsa eski WASM'la çalışır ve API ilerlemişse 400/404 alır. Azaltma: kapatılamaz band + no-cache. Sürüm uyuşmazlığı ucu bu pakette YOK.
- IIS'teki 7 günlük clientCache düzeltilmezse güncelleme hiç ulaşmaz ve saha müdahalesi gerektirir — bu, paketin en sinsi kusuru.
- Kurulu uygulamada oturum kilidi yok (JWT localStorage). Ortak tablette risk; paket 45 ile birlikte değerlendirilecek not sevk raporuna yazılır.
- index.html paylaşımlı dosyadır (43, 44, UX-2). Tek elden uygulanmazsa çakışma kaçınılmaz (MÜHÜR 2).
- Dev ortamında SW'nin boş olmaması hâlinde 'kod değişti, ekran değişmedi' saatleri yanar; dev sürümünün gerçekten boş olduğu testle sabitlenir.

## Açık sorular
- 512x512 ikonun KAYNAK GÖRSELİ nerede? Yüksek çözünürlüklü logo/vektör yoksa: (a) kurucudan kaynak iste, (b) geçici olarak yalnız 192 ile yayınla. Ekip oyuyla karar verilir; uydurma logo üretilmez.
- name/short_name metni 'Kuyumcu ERP' / 'Kuyumcu' kalsın mı, yoksa kiracı markası mı görünsün? Kiracı başına manifest üretmek statik dosya modelini kırar — bu pakette sabit ad önerilir, DOĞRULANMALI.
- API yanıtlarında bugün Cache-Control: no-store var mı? Program.cs'te başlık bloğunda (281-291) görülmedi; uygulayıcı doğrulasın, yoksa bu pakette eklensin (Mehmet koşulu).
- deploy/iis-web.config bu oturumda açılmadı; clientCache 7.00:00:00 iddiası paket 44 spesifikasyonundan devralındı — uygulayıcı dosyayı okuyup doğrulasın.
- Faz B bu pakette mi kalsın, yoksa ayrı sevk mi edilsin? Kurul tanımı 'ana ekrana ekle'yi istiyor; tarayıcı menüsü bunu Faz A'da zaten sağlıyor.
- Gerçek alan adı + TLS sertifikası ne zaman hazır olacak? Cihaz kanıtı buna bağlı (kurucu kalemi: dış/ücretli iş).

## Oylama
- **YENİ: BAŞ MİMAR (Bertan) merceği: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu, önkoşul sırası, paket boyutu (sevk edilebilirlik), dosya çakışması, ADR uyumu. Üç pakette dosya:satır kanıtı bizzat doğrulandı (Entities.cs Tenant bloğu, Program.cs CSP + statik boru hattı, denetim.sh yüzeyi, wwwroot/index.html, AdminModulu plan yazma yolu, AuthServisi.Kayit). NOT (Madde 1): görevde "4 paket" denmesine rağmen bana ulaşan tasarım metni 3 pakettir (F-8, S-1, PWA-1); PWA-1 'mevzuat' bölümünün ortasında kesilmiş, 4. paket nesnesi hiç başlamamıştır. Olmayan tasarıma oy uydurulmadı.: KOSULLU** — Tasarımın kodda anlattığı her nokta birebir doğru çıktı; reddetme listesini fetch'in ilk satırına koyup mutasyonla SIRAYI ölçmek (M-PWA-02) olgun bir çözüm. Veri modeline dokunmaması ve migration üretmemesi paketi gerçekten bağımsız kılıyor. İki gerçek risk: (1) barındırılan yayında ServiceWorkerAssetsManifest'in çalışacağı VARSAYILMIŞ, doğrulanmamış — yanlışsa karma listesi ve güncelleme akışı ölü doğar; (2) 512/maskable kaynak görsel yok, büyütme haklı olarak yasak ama sonuç 'düşük kaliteli kurulum' olarak rapora yazılmalı.
  - Koşullar: Faz A'nın İLK adımı 'dotnet publish -c Release' olsun: service-worker-assets.js'in barındırılan yayında (Kuyumcu.Api.csproj:22 → Kuyumcu.Web ProjectReference) üretildiği kanıtlanmadan başka dosyaya dokunulmasın. · İkon gerçeği: diskte yalnız icon-192.png var ve index.html'de hiç referansı yok. 512/maskable kaynak yoksa manifest tek ikon bildirir; 'Android kurulum/splash kalitesi düşük' notu sevk raporuna yazılsın, büyütme yapılmasın. · CSP tek elden: Program.cs:281-291 dizgesine yalnız manifest-src 'self' + worker-src 'self' eklenir; aynı turda 45/47 bu dizgeye dokunmaz. Csp_ScriptSrc_UnsafeInline_Icermez bekçisi zorunlu. · StaticFileOptions yalnız :317 UseStaticFiles'a bağlanır; :316 UseBlazorFrameworkFiles ve :327 MapFallbackToFile sırası korunur (karmalı _framework yolu etkilenmemeli). · denetim.sh'a eklenecek JS/HTML bloğu kod_say/denetle'yi YENİDEN KULLANMAZ (yüzey :17 ve --include='*.cs' ile .cs'e bağlı); ayrı fonksiyonla betiğin SONUNA eklenir, mevcut sayaçlar bozulmaz. · Sıra sabitlensin: Ayarlar.razor'a önce S-1 tanıtım şeridi (dalga 6), sonra PWA-1 Faz B 'Uygulama' bölümü (dalga 7); aynı dosyada paralel çalışma yok (MÜHÜR 2 tek el).
- **YENİ: Güvenlik merceğinden (Mehmet) üç tasarımı inceledim: kiracı izolasyonu, rol/yetki, fail-closed kapılar, sır yönetimi, yedek/TLS, içe aktarım riski ve MFA koşulu açısından. Program.cs:283 canlı kodda script-src 'self' 'wasm-unsafe-eval' olarak doğrulandı (unsafe-inline yok) — PWA-1'in CSP taahhüdünün mevcut temeli sağlam. docker-compose.yml:7'de MySQL root parolası düz metin olarak canlıda doğrulandı; hiçbir pakette bu ele alınmıyor (T-1/F-6/F-7'ye bırakılmış), ayrı takip gerekir.: EVET** — Paket kiracı/API verisini önbelleğe almıyor, çevrimdışı yazma yok, script-src'yi gevşetmiyor ve fail-closed reddetme listesini ilk satıra koyup mutasyon testleriyle bekçiliyor. Yeni HTTP ucu, yeni rol/yetki, yeni sır yok. İzolasyon yüzeyini genişletmiyor (ADR-0001 referansı doğru). Veto gerektirecek bir açık bulunmadı.
  - Koşullar: Service worker fetch reddetme listesinin gerçekten İLK satırda olduğu ve /api/, /hubs/, /saglik, /swagger, Authorization+Vary koşullarının TÜMÜNÜ kapsadığı, mutation testleri (M-PWA-01/02) CI'da kırmızı-yeşil doğrulanmadan yayına alınmamalı — bu, kiracı verisi sızıntısını önleyen TEK kontrol noktası. · CSP'ye eklenen manifest-src/worker-src 'self' dışında script-src'ye 'unsafe-inline' KESİNLİKLE eklenmemeli; Csp_ScriptSrc_UnsafeInline_Icermez testi canlı kodda doğrulandı (Program.cs:283 script-src 'self' 'wasm-unsafe-eval') ve regresyon bekçisi olarak kalmalı. · Yalnız dotnet publish -c Release çıktısı (service-worker.published.js) canlıya gider; dev sürümünün (boş SW) yanlışlıkla Production'a sızmadığı dağıtım betiğinde (Batuhan/DevOps) ayrıca doğrulanmalı, bu paket kendi başına garanti etmiyor. · postMessage işleyicisinin yalnız {tur:'HEMEN_DEVRAL'} kabul ettiği VE origin kontrolü yaptığı ayrı bir testle (bugünkü listede M-PWA-09 mutasyonu var ama pozitif test adı net değil) netleştirilmeli.
- **YENİ: Aşkın — QA/fail-closed kalite kapısı. Mercek: (a) her iş kuralının mutasyon kanıtlı bir testi var mı, (b) DB/HTTP/bUnit dengesi kurulu mu, (c) kararsızlık (flaky) riski, (d) canlı doğrulama ölçülebilir/somut mu, (e) kurulum/yedek paketlerinde "kanıt" gerçek publish+geri yükleme mi, (f) test kiracısı temizliği taahhüt edilmiş mi. Doğrulama: Program.cs:271-330 (CSP script-src 'self' 'wasm-unsafe-eval', unsafe-inline YOK — PWA-1'in iddiasıyla örtüşüyor), Kuyumcu.Api.csproj:9-17 (8 paket, F-8'in lisans-0 iddiasıyla örtüşüyor), Entities.cs:16,56 (Planlar dizisi ve Plan varsayılanı doğrulandı), AuthModulu.cs:87,323-326 (Kayit yolu doğrulandı, kota/deneme bitişi yok iddiası tutarlı), denetim.sh:1-60 (kalıp denetimi mekanizması gerçek ve betikte "ExecuteUpdate|ExecuteDelete beyaz liste 4" gibi sayaçlar var — S-1/PWA-1'in "denetim.sh'a blok eklenir" planı bu mekanizmaya uygun).: EVET** — Reddetme listesi (fetch işleyicisinin İLK satırı /api/, /hubs/, /saglik, /swagger, Authorization) somut ve mutasyon testleriyle (M-PWA-01/02) sıra dahil sınanıyor — bu, önbellek zehirlenmesi/veri sızıntısı riskine karşı güçlü bir QA kapısı. Canlı doğrulama adımları gerçekten ölçülebilir (curl -i başlık kontrolü, DevTools Cache Storage'da '/api/' girdi sayısı=0, çevrimdışı tatbikat). 'HTTPS olmadan cihazda ana ekrana ekle kanıtı alınamaz, yazıldı-denenmedi diye açık yazılır' ifadesi Madde 1 dürüstlüğüne tam uyuyor. Kod değişikliği + test dengesi (saf/HTTP/bUnit/betik) eksiksiz görünüyor.
  - Koşullar: ApiYanitlari_CacheControl_NoStore_Tasir testi 'DOĞRULANMALI: bugün var mı Program.cs okunarak saptanır' diye şartlı yazılmış; Program.cs:271-330 taramasında böyle bir başlık bulunmadı (yalnız CSP/X-Frame/HSTS var) — bu demek ki test muhtemelen bu pakette YENİ eklenecek davranışı da sınayacak; PR açıklamasında bu netleştirilsin. · service-worker.published.js sadece 'dotnet publish -c Release' ile etkinleşiyor; dev sunucusunda (5198/5199) kanıt alınamayacağı açıkça yazılmış — bu doğru ama canlı doğrulama adımlarının hangi ortamda koşacağı (ayrı bir Release build mi) SONRAKI-OTURUM-PROMPT'a not düşülmeli, yoksa 'kanıtladık' yanılgısı doğar.

## Birleşik koşullar (uygulayıcı için bağlayıcı)
- Faz A'nın İLK adımı 'dotnet publish -c Release': barındırılan yayında service-worker-assets.js üretildiği KANITLANMADAN başka dosyaya dokunulmaz (Bertan itirazı — 'teknik önkoşul YOK' ifadesi eksiktir).
- Reddetme listesi fetch işleyicisinin İLK satırıdır ve /api/, /hubs/, /saglik, /swagger, Authorization+Vary koşullarının TÜMÜNÜ kapsar; M-PWA-01/02 mutasyonları CI'da kırmızı-yeşil doğrulanmadan yayına alınmaz.
- CSP tek elden: Program.cs:281-291 dizgesine yalnız manifest-src 'self' + worker-src 'self' eklenir; script-src'ye 'unsafe-inline' asla eklenmez; Csp_ScriptSrc_UnsafeInline_Icermez bekçisi zorunlu.
- StaticFileOptions yalnız :317 UseStaticFiles'a bağlanır; :316 UseBlazorFrameworkFiles ve :327 MapFallbackToFile sırası korunur (karmalı _framework yolu etkilenmez).
- İkon gerçeği: diskte yalnız icon-192.png var ve index.html'de referansı yok. 512/maskable kaynak yoksa manifest tek ikon bildirir, büyütme yapılmaz, 'Android kurulum/splash kalitesi düşük' notu sevk raporuna yazılır.
- Yalnız service-worker.published.js canlıya gider; dev sürümünün Production'a sızmadığı dağıtım betiğinde (DevOps) ayrıca doğrulanır.
- postMessage işleyicisi yalnız {tur:'HEMEN_DEVRAL'} kabul eder ve origin kontrolü yapar; pozitif test adıyla netleştirilir.
- denetim.sh'a eklenecek JS/HTML bloğu kod_say/denetle'yi YENİDEN KULLANMAZ (yüzey .cs'e bağlı); ayrı fonksiyonla betiğin sonuna eklenir, mevcut sayaçlar bozulmaz.
- ApiYanitlari_CacheControl_NoStore testi: bugün Program.cs:271-330'da no-store başlığı YOK; bu davranışın bu pakette YENİ eklendiği PR açıklamasında netleştirilir.
- Sıra: Ayarlar.razor'a önce S-1 tanıtım şeridi (dalga 6), sonra PWA-1 Faz B (dalga 7); index.html ve Program.cs paylaşımlı olduğu için tek el (Bertan), paralel çalışma yok. 45 (MFA) ile temas: kurulu uygulama oturum kilidi taşımaz, bu pakette değişmez, rapora not düşülür.
