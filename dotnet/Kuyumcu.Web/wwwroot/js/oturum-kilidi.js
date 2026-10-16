// EMANET-1 keşif bulgusu B006 — SEKMELER ARASI TEK UÇUŞLU JETON YENİLEME.
//
// Aynı kullanıcı iki/üç sekme açınca her sekme yenileme jetonunu açılışta
// localStorage'dan okuyup bellekte tutuyordu. Erişim süresi dolunca sekmeler
// AYNI eski jetonla /auth/yenile çağırıyor; ikinci çağrı sunucuda "iptal edilmiş
// jetonun yeniden kullanımı" (RFC 9700 §4.14.2) sayılıp TÜM aile iptal ediliyor
// ve bütün sekmeler oturumdan düşüyordu.
//
// Çözüm istemcide: yenileme, tarayıcı genelinde TEK kilit altında yapılır
// (navigator.locks; yoksa localStorage kilidi). Kilidi alan sekme jetonu
// localStorage'dan TAZE okur: başka sekme zaten yenilemişse onun çiftini
// benimser, sunucuya eski jetonla GİTMEZ. Sunucudaki rotasyon ve çalıntı
// tespiti GEVŞETİLMEZ.
(function () {
    var bekleyen = {};
    var sayac = 0;
    var DEPO_KILIT = "kuyumcu-yenile-kilit";
    var AZAMI_MS = 15000;          // kilit asla bundan uzun tutulmaz (sekme çökse bile)

    function kimlik() {
        sayac += 1;
        return "k" + Date.now().toString(36) + "-" + Math.random().toString(36).slice(2) + "-" + sayac;
    }

    function uyu(ms) { return new Promise(function (r) { setTimeout(r, ms); }); }

    // navigator.locks yoksa (güvenli olmayan http kökeni) en iyi çaba localStorage kilidi.
    async function depoKilidiAl(id) {
        var bitis = Date.now() + AZAMI_MS;
        while (Date.now() < bitis) {
            var simdi = Date.now();
            var ham = null;
            try { ham = localStorage.getItem(DEPO_KILIT); } catch (e) { return false; }
            var mevcut = null;
            try { mevcut = ham ? JSON.parse(ham) : null; } catch (e) { mevcut = null; }
            if (!mevcut || mevcut.t + AZAMI_MS < simdi) {
                localStorage.setItem(DEPO_KILIT, JSON.stringify({ id: id, t: simdi }));
                await uyu(30);
                var geri = null;
                try { geri = JSON.parse(localStorage.getItem(DEPO_KILIT) || "null"); } catch (e) { geri = null; }
                if (geri && geri.id === id) return true;
            }
            await uyu(60 + Math.floor(Math.random() * 60));
        }
        return false;
    }

    window.kuyumcuOturumKilidi = {
        /** Kilidi alır; alınınca kimlik döner. Kilit yoksa null (çağıran kilitsiz sürer). */
        al: function (ad) {
            var id = kimlik();
            if (navigator.locks && navigator.locks.request) {
                return new Promise(function (hazir) {
                    navigator.locks.request(ad, function () {
                        return new Promise(function (birak) {
                            var zamanlayici = setTimeout(function () { delete bekleyen[id]; birak(); }, AZAMI_MS);
                            bekleyen[id] = function () { clearTimeout(zamanlayici); birak(); };
                            hazir(id);
                        });
                    }).catch(function () { hazir(null); });
                });
            }
            return depoKilidiAl(id).then(function (tamam) {
                if (!tamam) return null;
                bekleyen[id] = function () {
                    try {
                        var geri = JSON.parse(localStorage.getItem(DEPO_KILIT) || "null");
                        if (geri && geri.id === id) localStorage.removeItem(DEPO_KILIT);
                    } catch (e) { /* depo yok: bırakılacak kilit de yok */ }
                };
                return id;
            });
        },
        birak: function (id) {
            var f = bekleyen[id];
            delete bekleyen[id];
            if (f) f();
        }
    };
})();
