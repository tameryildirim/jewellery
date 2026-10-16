// ═══════════════════════════════════════════════════════════════════════════
// AÇILIR KATMAN — TEK KALIP (paket KATMAN-1, 2026-09-25)
//
// ÖLÇÜLDÜ (betikler/tur, başsız Chrome): `.satir-menu` popover'ları (SatirEylemleri
// "⋯", FisDigerIslemler "Diğer", Kasalar "⋯") top-layer'da UA varsayılanıyla
// (inset:0; margin:auto) GÖRÜŞ ALANININ ORTASINDA açılıyordu — açıcısına bağlı
// değil (tur: SERBEST-YUZEN). Kurucu 25.09: "sayfanın ortasında seçim çıkıyor".
//
// TEK MEKANİZMA: bağlı katman açıldığında açıcısının getBoundingClientRect'ine göre
//   · dikey: ALTINDA; altta sığmıyor ve üstte daha çok yer varsa ÜSTÜNDE (flip);
//     azami yükseklik = seçilen taraftaki kalan alan (katman kendi içinde kayar);
//   · yatay: açıcı ekranın sağ yarısındaysa sağ kenarlar, değilse sol kenarlar
//     hizalanır; sonra görüş alanına PAY (8 px) payla KISILIR (clamp);
//   · değerler CSS BELİRTECİ olarak yazılır (--katman-ust/--katman-sol/
//     --katman-azami-boy) — satır içi düzen kuralı değil; biçim css/katman.css'te.
//   · sayfa/kap kaydırılınca ve pencere boyu değişince yeniden konumlanır;
//     açıcı görüş alanından çıktıysa katman kapanır.
//   · açılınca odak ilk etkin öğeye geçer (Esc/dışarı tıklama yerli popover'dadır).
// Popover olmayan (Blazor'un koşullu çizdiği) bağlı katman `data-katman-acici`
// özniteliğiyle aynı yardımcıya bağlanır. (KATMAN-1b: kayıt arama paleti artık
// BAĞLI DEĞİLDİR — perdeli komut paletidir, css/katman.css § 4.)
// Bu yolun BUGÜN razor tüketicisi YOKTUR (grep 'data-katman-acici' *.razor → 0;
// KATMAN-1b 30.09 doğrulama bulgusu). BİLEREK KORUNUR, gerekçe: (1) popover olmayan,
// Blazor'un koşullu çizdiği bağlı katman (ör. satır içi öneri listesi) tek kalıba
// bu öznitelikle girer — ikinci bir konumlama kodu yazılmasın diye; (2) 0×0 / ekran
// dışı açıcı korumasının popover'sız kolu bu yolla sınanır (KatmanTekKalipTestleri
// K1_18(oge), K1_19, K1_22) — silinirse regresyon kalkanı yarıya iner. Gözlemci
// DOM'a eklenen her öğe için tek seçici sorgusu yapar (matches + querySelectorAll);
// tüketici yokken hiçbir öğeyi konumlamaz.
// Açıcı görünmezse (0×0) bağ KURULMAZ, varsa ÇÖZÜLÜR (gorunurKutu/bagiCoz).
//
// ⚠ Anayasa md.1: açıcı ya da katman bulunamazsa sessizce "oldu" denmez —
//   yerlestir() false döner, `data-katman` yazılmaz ve CSS eski davranışta kalır.
// ⚠ CSP: script-src 'self' — satır içi <script> ve eval() YOK.
// ═══════════════════════════════════════════════════════════════════════════
(function () {
    'use strict';

    var PAY = 8;       // görüş alanı kenarı ile katman arasında asgari boşluk (px)
    var ARALIK = 4;    // açıcı ile katman arasındaki boşluk (px)
    var BAGLI_POPOVER = '[popover].satir-menu';
    var BAGLI_OGE = '[data-katman-acici]';

    var aciciHaritasi = {};   // popover id → son açan öğe

    function gorusAlani() {
        return {
            w: window.innerWidth || document.documentElement.clientWidth,
            h: window.innerHeight || document.documentElement.clientHeight
        };
    }

    function aciciBul(katman) {
        if (!katman || !katman.id) return null;
        var a = aciciHaritasi[katman.id];
        if (a && a.isConnected) return a;
        var id = katman.id.replace(/"/g, '\\"');
        return document.querySelector('[popovertarget="' + id + '"]:not([popovertargetaction="hide"])');
    }

    // Saf hesap (birim testi ve ölçüm için dışarı açık): açıcı kutusu, katman boyu
    // ve görüş alanından konum üretir.
    function hesapla(a, kw, kh, g) {
        var altYer = g.h - a.bottom - ARALIK - PAY;
        var ustYer = a.top - ARALIK - PAY;
        var yukari = kh > altYer && ustYer > altYer;
        var azami = Math.max(0, Math.floor(yukari ? ustYer : altYer));
        var boy = Math.min(kh, azami);
        var ust = yukari ? a.top - ARALIK - boy : a.bottom + ARALIK;

        var genislik = Math.min(kw, g.w - 2 * PAY);
        var sagda = (a.left + a.right) / 2 > g.w / 2;
        var sol = sagda ? a.right - genislik : a.left;
        sol = Math.max(PAY, Math.min(sol, g.w - PAY - genislik));
        return { ust: Math.round(ust), sol: Math.round(sol), azami: azami, yukari: yukari };
    }

    function uygula(katman, sonuc) {
        katman.style.setProperty('--katman-ust', sonuc.ust + 'px');
        katman.style.setProperty('--katman-sol', sonuc.sol + 'px');
        katman.style.setProperty('--katman-azami-boy', sonuc.azami + 'px');
    }

    // İçerik boyu (kaydırılabilir kısım dahil) + dikey kenarlık; azami boy yerinde kalır.
    function dogalBoy(katman, kutu) {
        var sh = katman.scrollHeight, oh = katman.offsetHeight, ch = katman.clientHeight;
        if (typeof sh !== 'number' || typeof oh !== 'number' || typeof ch !== 'number') return kutu.height;
        return Math.max(kutu.height, sh + Math.max(0, oh - ch));
    }

    // Açıcı GÖRÜNMEZSE (display:none → kutu 0×0; ör. masaüstünde gizli #ust-kayit-ara)
    // katman ona BAĞLANAMAZ: 0×0 kutu (0,0)'dadır ve "ekranda" sayılırdı → katman sol üst
    // köşeye yapışıyordu (KATMAN-1 tur 3 doğrulaması, KRİTİK). Bağ çözülür (öznitelik ve
    // belirteçler silinir) ve katman kendi CSS düzeninde kalır.
    function gorunurKutu(a) {
        return !!a && a.width > 0 && a.height > 0;
    }
    function bagiCoz(katman) {
        if (typeof katman.removeAttribute === 'function') {
            katman.removeAttribute('data-katman');
            katman.removeAttribute('data-katman-yon');
        }
        ['--katman-ust', '--katman-sol', '--katman-azami-boy'].forEach(function (b) {
            if (katman.style && typeof katman.style.removeProperty === 'function') katman.style.removeProperty(b);
        });
    }

    function yerlestir(katman, acici) {
        if (!katman || !acici) return false;
        var a = acici.getBoundingClientRect();
        var g = gorusAlani();
        if (!gorunurKutu(a)) { bagiCoz(katman); return false; }
        // Görüş alanı dışındaki açıcıya da bağ KALMAZ (doğrulama 27.09): popover olmayan
        // data-katman-acici öğesinde eski data-katman + --katman-* belirteçleri kalıyordu.
        if (a.bottom < 0 || a.top > g.h) { bagiCoz(katman); return false; }
        // Öznitelik ÖNCE yazılır: bağlı kalıbın genişlik kuralları (css/katman.css) ölçüme girsin.
        katman.setAttribute('data-katman', 'bagli');
        // İki geçiş: ilk konumda (açıcının solunda) kullanılabilir genişlik dar olabilir →
        // satırlar fazla kırılır, boy büyük ölçülür. Yeni konumda DOĞAL boy yeniden
        // ölçülür ve hesap tekrarlanır (tur: 1440 PRO #c-menu yukarı açılınca açıcıdan
        // 26 px kopuk kalıyordu).
        // ⚠ DOĞAL boy azami boy KALDIRILMADAN ölçülür (dogalBoy: scrollHeight + kenarlık):
        //   kaldırmak kutuyu bir an büyütür ve tarayıcı scrollTop'u 0'a kısar — 375 px'te
        //   iç kaydırma hiç ilerlemiyordu (doğrulama 25.09: /cariler "⋯" st=0).
        var sonuc = null;
        for (var gecis = 0; gecis < 2; gecis++) {
            var k = katman.getBoundingClientRect();
            sonuc = hesapla(a, k.width, dogalBoy(katman, k), g);
            uygula(katman, sonuc);
        }
        katman.setAttribute('data-katman-yon', sonuc.yukari ? 'ust' : 'alt');
        return true;
    }

    function ilkOgeyeOdak(katman) {
        var oge = katman.querySelector('button:not([disabled]), [href], input:not([disabled]), [tabindex]:not([tabindex="-1"])');
        if (oge && typeof oge.focus === 'function') {
            try { oge.focus({ preventScroll: true }); } catch (e) { oge.focus(); }
        }
    }

    // Açıcıyı tıklama anında kaydet (aynı menüyü birden çok düğme açabilir).
    document.addEventListener('click', function (e) {
        var a = e.target && e.target.closest ? e.target.closest('[popovertarget]') : null;
        if (a && a.getAttribute('popovertargetaction') !== 'hide') {
            aciciHaritasi[a.getAttribute('popovertarget')] = a;
        }
    }, true);

    // beforetoggle: ilk karede bile açıcının altında dursun (ortada "parlama" yok);
    // toggle: gerçek boyla kesin yerleşim + odak.
    document.addEventListener('beforetoggle', function (e) {
        var k = e.target;
        if (!k || !k.matches || !k.matches(BAGLI_POPOVER) || e.newState !== 'open') return;
        var a = aciciBul(k);
        if (!a) return;
        var r = a.getBoundingClientRect();
        if (!gorunurKutu(r)) return;   // görünmez açıcıya ilk karede de bağlanılmaz
        k.style.setProperty('--katman-ust', Math.round(r.bottom + ARALIK) + 'px');
        k.style.setProperty('--katman-sol', Math.round(Math.max(PAY, r.left)) + 'px');
        k.setAttribute('data-katman', 'bagli');
    }, true);

    document.addEventListener('toggle', function (e) {
        var k = e.target;
        if (!k || !k.matches || !k.matches(BAGLI_POPOVER)) return;
        if (e.newState === 'open') {
            if (yerlestir(k, aciciBul(k))) ilkOgeyeOdak(k);
        }
    }, true);

    function acikBaglilar() {
        var liste = [];
        try { liste = Array.prototype.slice.call(document.querySelectorAll(BAGLI_POPOVER + ':popover-open')); } catch (e) { liste = []; }
        return liste.concat(Array.prototype.slice.call(document.querySelectorAll(BAGLI_OGE)));
    }

    function ogeAcicisi(k) {
        var id = k.getAttribute('data-katman-acici');
        return id ? document.getElementById(id) : aciciBul(k);
    }

    var bekleyen = false;
    function hepsiniYenile() {
        if (bekleyen) return;
        bekleyen = true;
        window.requestAnimationFrame(function () {
            bekleyen = false;
            acikBaglilar().forEach(function (k) {
                var a = ogeAcicisi(k);
                if (!a) return;
                var r = a.getBoundingClientRect();
                var g = gorusAlani();
                var disarida = r.bottom < 0 || r.top > g.h || r.width === 0;
                if (disarida && k.matches(BAGLI_POPOVER) && typeof k.hidePopover === 'function') {
                    try { k.hidePopover(); } catch (e) { /* zaten kapalı */ }
                    return;
                }
                yerlestir(k, a);
            });
        });
    }
    // Kaydırma yakalama evresinde dinlenir (iç kaplar da yakalansın) — ama olayın hedefi
    // bir bağlı katmanın KENDİSİ ya da içindeki bir öğeyse bu, kullanıcının menüyü
    // kaydırmasıdır: yeniden yerleştirme YAPILMAZ (aksi halde her tekerlek adımında
    // konum/azami boy yeniden yazılır ve iç kaydırma ilerlemez — doğrulama 25.09).
    function kaydirmada(e) {
        var h = e && e.target;
        if (h && h.nodeType === 1 && typeof h.closest === 'function' &&
            h.closest(BAGLI_POPOVER + ', ' + BAGLI_OGE)) return;
        hepsiniYenile();
    }
    window.addEventListener('scroll', kaydirmada, true);
    window.addEventListener('resize', hepsiniYenile);

    // Blazor'un koşullu çizdiği bağlı katmanlar (popover değil): DOM'a girince bağla.
    function yeniOgeleriBagla(kok) {
        var ogeler = [];
        if (kok.matches && kok.matches(BAGLI_OGE)) ogeler.push(kok);
        if (kok.querySelectorAll) ogeler = ogeler.concat(Array.prototype.slice.call(kok.querySelectorAll(BAGLI_OGE)));
        ogeler.forEach(function (k) { yerlestir(k, ogeAcicisi(k)); });
    }
    function gozlemciKur() {
        if (typeof MutationObserver !== 'function' || !document.body) return;
        new MutationObserver(function (kayitlar) {
            kayitlar.forEach(function (kayit) {
                Array.prototype.forEach.call(kayit.addedNodes, function (n) {
                    if (n.nodeType === 1) yeniOgeleriBagla(n);
                });
            });
        }).observe(document.body, { childList: true, subtree: true });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', gozlemciKur);
    else gozlemciKur();

    window.kuyumcuKatman = { yerlestir: yerlestir, hesapla: hesapla, PAY: PAY };
})();
