// ═════════════════════════════════════════════════════════════════════════════
// PWA KÖPRÜSÜ — PAKET-44 / PWA-1 (20.10). tema.js deseni: IIFE, adlandırılmış fonksiyonlar, eval YOK (CSP).
//   kuyumcuPwa.baslat(dotNetRef): service worker'ı kaydeder; yeni sürüm hazır olunca .NET'e GuncellemeHazir(),
//   "ana ekrana ekle" istemi yakalanınca KurulumMumkun(true/false), ağ durumu değişince AgDurumu(bool) bildirir.
//   kuyumcuPwa.kur(): saklı kurulum istemini açar (yoksa false). kuyumcuPwa.yenile(): bekleyen worker'a devral der.
// ═════════════════════════════════════════════════════════════════════════════
(function () {
    'use strict';

    var kayit = null, bekleyenIstem = null, dotNet = null;

    function bildir(ad, deger) {
        if (!dotNet) return;
        try { dotNet.invokeMethodAsync(ad, deger === undefined ? null : deger); } catch (e) { /* .NET yoksa sessiz */ }
    }

    function baslat(ref) {
        dotNet = ref;
        if (!('serviceWorker' in navigator)) return false;
        navigator.serviceWorker.register('service-worker.js').then(function (r) {
            kayit = r;
            if (r.waiting && navigator.serviceWorker.controller) bildir('GuncellemeHazir');
            r.addEventListener('updatefound', function () {
                var yeni = r.installing;
                if (!yeni) return;
                yeni.addEventListener('statechange', function () {
                    if (yeni.state === 'installed' && navigator.serviceWorker.controller) bildir('GuncellemeHazir');
                });
            });
        }).catch(function () { /* kayıt olmadı: uygulama normal çalışır */ });
        navigator.serviceWorker.addEventListener('controllerchange', function () { window.location.reload(); });
        window.addEventListener('beforeinstallprompt', function (e) { e.preventDefault(); bekleyenIstem = e; bildir('KurulumMumkun', true); });
        window.addEventListener('appinstalled', function () { bekleyenIstem = null; bildir('KurulumMumkun', false); });
        window.addEventListener('online', function () { bildir('AgDurumu', true); });
        window.addEventListener('offline', function () { bildir('AgDurumu', false); });
        if (!navigator.onLine) bildir('AgDurumu', false);
        return true;
    }

    function kur() {
        if (!bekleyenIstem) return Promise.resolve(false);
        var istem = bekleyenIstem; bekleyenIstem = null;
        istem.prompt();
        return istem.userChoice.then(function (s) { return s && s.outcome === 'accepted'; }).catch(function () { return false; });
    }

    function yenile() {
        if (kayit && kayit.waiting) { kayit.waiting.postMessage({ tur: 'HEMEN_DEVRAL' }); return true; }
        window.location.reload();
        return false;
    }

    function cevrimdisiMi() { return !navigator.onLine; }

    window.kuyumcuPwa = { baslat: baslat, kur: kur, yenile: yenile, cevrimdisiMi: cevrimdisiMi };
})();
