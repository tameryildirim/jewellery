// PAKET-44 / PWA-1 (20.10) — GELİŞTİRME service worker'ı.
// Hiçbir şey önbelleğe ALMAZ ve fetch olayını DİNLEMEZ: geliştirmede önbellek, "kod değişti ama ekran değişmedi"
// saatlerini yakar. Yayında (dotnet publish) bu dosyanın yerine service-worker.published.js konur (csproj ServiceWorker).
self.addEventListener('install', function () { self.skipWaiting(); });
self.addEventListener('activate', function (e) { e.waitUntil(self.clients.claim()); });
