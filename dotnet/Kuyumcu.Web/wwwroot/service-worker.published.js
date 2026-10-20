// PAKET-44 / PWA-1 (20.10) — YAYIN service worker'ı: uygulama KABUĞU çevrimdışı açılır; veri ASLA önbelleğe girmez.
//   • Yalnız karmalı kabuk varlıkları (service-worker-assets.js listesi; .html .js .css .wasm .dll .png .svg .woff .woff2 .json).
//   • REDDETME LİSTESİ (kısa devre): /api/ /hubs/ /saglik /swagger yolları, Authorization başlıklı istekler,
//     GET dışı istekler → dokunulmaz, önbelleğe YAZILMAZ (kümülatif mevzuat kapıları, kur tazeliği delinmez).
//   • navigate istekleri → önbellekteki index.html (SPA); API 404'ü maskelenmez (reddetme listesi önce).
//   • Yeni sürüm: install'da yeni önbellek kurulur; sayfa "HEMEN_DEVRAL" mesajı yollayınca skipWaiting.
self.importScripts('./service-worker-assets.js');

const ONEK = 'mihenk-kabuk-';
const ONBELLEK = ONEK + self.assetsManifest.version;
const UZANTILAR = [/\.dll$/, /\.pdb$/, /\.wasm$/, /\.html$/, /\.js$/, /\.css$/, /\.json$/, /\.woff2?$/, /\.png$/, /\.svg$/, /\.ico$/];
const DISLA = [/^service-worker\.js$/, /^service-worker\.published\.js$/, /appsettings.*\.json$/];
const RED_YOLLARI = ['/api/', '/hubs/', '/saglik', '/swagger'];

self.addEventListener('install', function (e) {
    e.waitUntil((async function () {
        const varliklar = self.assetsManifest.assets
            .filter(function (a) { return UZANTILAR.some(function (r) { return r.test(a.url); }); })
            .filter(function (a) { return !DISLA.some(function (r) { return r.test(a.url); }); })
            .map(function (a) { return new Request(a.url, { integrity: a.hash, cache: 'no-cache' }); });
        const c = await caches.open(ONBELLEK);
        await c.addAll(varliklar);
    })());
});

self.addEventListener('activate', function (e) {
    e.waitUntil((async function () {
        const adlar = await caches.keys();
        await Promise.all(adlar.filter(function (a) { return a.startsWith(ONEK) && a !== ONBELLEK; }).map(function (a) { return caches.delete(a); }));
        await self.clients.claim();
    })());
});

self.addEventListener('message', function (e) {
    if (e.data && e.data.tur === 'HEMEN_DEVRAL') self.skipWaiting();
});

self.addEventListener('fetch', function (e) {
    const istek = e.request;
    if (istek.method !== 'GET') return;
    if (istek.headers.has('Authorization')) return;
    const url = new URL(istek.url);
    if (url.origin !== self.location.origin) return;
    if (RED_YOLLARI.some(function (y) { return url.pathname.startsWith(y); })) return;
    e.respondWith((async function () {
        const c = await caches.open(ONBELLEK);
        if (istek.mode === 'navigate') {
            const kabuk = await c.match('index.html');
            if (kabuk) return kabuk;
        }
        const onbellekte = await c.match(istek);
        if (onbellekte) return onbellekte;
        return fetch(istek);
    })());
});
