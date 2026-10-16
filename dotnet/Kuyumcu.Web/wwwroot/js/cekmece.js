// ═══════════════════════════════════════════════════════════════════════════
// YÜZEN KATMAN ERİŞİLEBİLİRLİĞİ — paket UX-2 Faz A2 (K6 · DoD EK 2 · tur 2)
//
// TEK MEKANİZMA: Cekmece.razor, ModalPencere.razor ve dar ekrandaki kenar
// çubuğu çekmecesi AYNI yardımcıyı kullanır. İkinci bir odak/Esc deseni
// AÇILMAZ (QA turu 2 bulgusu: "Escape hiçbir modalı kapatmıyor" — sebep,
// panelin açılışta odağı ALMAMASIydı; tuş olayı panele hiç ulaşmıyordu).
//
// CSP: script-src 'self' 'wasm-unsafe-eval' → satır içi <script> ve eval()
// YASAK. Bu yüzden adlandırılmış fonksiyonlar ve window.kuyumcuCekmece.
// ═══════════════════════════════════════════════════════════════════════════
(function () {
    'use strict';

    var ODAKLANABILIR = [
        'a[href]', 'button:not([disabled])', 'input:not([disabled])',
        'select:not([disabled])', 'textarea:not([disabled])',
        '[tabindex]:not([tabindex="-1"])'
    ].join(',');

    function odaklanabilirler(panel) {
        if (!panel || !panel.querySelectorAll) return [];
        var hepsi = panel.querySelectorAll(ODAKLANABILIR);
        return Array.prototype.filter.call(hepsi, function (e) {
            return e.offsetParent !== null || e === document.activeElement;
        });
    }

    /**
     * Paneli açar: önceki odağı saklar, ilk odaklanabilir öğeye (yoksa panelin
     * kendisine) odaklanır ve Tab'ı panel içine hapseder.
     * @returns {boolean} odak gerçekten panele taşındıysa true
     */
    // ── ARKA PLAN KAYDIRMA KİLİDİ (P-36c) ───────────────────────────────
    // FRONTEND-POPUP ölçümü: pencere açıkken arkadaki sayfa kayıyordu; mobilde
    // kullanıcı pencereyi kaybediyordu. Kilit SAYAÇLIDIR: iç içe/ardışık iki
    // panel birbirinin kilidini açmaz (tek bayrak kullanılsaydı açardı).
    // ── PAKET ORTAK-1 (D): SAYFA ZIPLAMASI ────────────────────────────────
    // ÖLÇÜLDÜ (tam tur v3): /cariler 768 px'te "Düzenle" çekmecesi açılınca
    // sayfa −85…−782 px yukarı zıplıyordu. Sebep: dar ekranda panel tam
    // ekrandır ve arkadaki liste çizilmez → belge kısalır → tarayıcı kaydırma
    // konumunu kısalan belgeye kırpar. Kilit, açılış anındaki belge boyunu
    // --pencere-sabit-boy belirtecine yazar (ortak.css: body.pencere-sabit
    // { min-height }) ve konumu saklar; belge kısalamaz, konum KORUNUR.
    // Kapanışta konum geri verilir, sabitleme liste yeniden çizildikten SONRA
    // (bir sonraki kareden sonra) kaldırılır — kapanışta da zıplama olmaz.
    var SABIT_BIRAK_MS = 250;
    function sabitle() {
        var kok = document.documentElement;
        document.__kuyumcuKaydirma = { x: window.scrollX, y: window.scrollY };
        document.body.style.setProperty('--pencere-sabit-boy', kok.scrollHeight + 'px');
        document.body.classList.add('pencere-sabit');
    }
    function konumuKoru() {
        var k = document.__kuyumcuKaydirma;
        if (k && (Math.abs(window.scrollY - k.y) > 1 || Math.abs(window.scrollX - k.x) > 1)) {
            window.scrollTo(k.x, k.y);
        }
    }
    function sabitiBirak() {
        sayaciDogrula();   // ModalPencere de açıkken yok edilebilir: bayat sayaç sabiti tutmasın
        if ((document.__kuyumcuPencereSayisi || 0) !== 0) return;   // yeni pencere açıldı
        konumuKoru();
        document.body.classList.remove('pencere-sabit');
        document.body.style.removeProperty('--pencere-sabit-boy');
        document.__kuyumcuKaydirma = null;
    }
    // Çizimden ÖNCE sabitleme (Cekmece.razor OnParametersSet, eşzamanlı çağrı):
    // Blazor aynı çizimde arkadaki listeyi gizleyebilir; DOM değişmeden belge
    // boyu ve konum burada saklanır. kilitle() ikinci kez ölçmez.
    // ORTAK-1 (D) · ÖLÇÜLDÜ (tam tur, 24 kombinasyon): açık çekmeceden başka
    // sayfaya geçilince bileşen kapat() çağrılmadan yok ediliyor, sayaç 1'de
    // KALIYORDU → sonraki sayfada gövde kaydırma kilitli ve yeni çekmece
    // "ilk pencere" sayılmıyordu (sabitleme atlanıyor, sayfa zıplıyordu).
    // Sayaç, DOM'da açık pencere (aria-modal) yoksa bayattır: sıfırlanır.
    function sayaciDogrula() {
        if ((document.__kuyumcuPencereSayisi || 0) > 0 && !document.querySelector('[aria-modal="true"]')) {
            document.__kuyumcuPencereSayisi = 0;
            document.__kuyumcuOnSabit = false;
            document.body.classList.remove('pencere-acik');
        }
    }
    /** Bileşen açıkken yok edildi (sayfa değişti): kilidi bırakır. */
    function birak() {
        if ((document.__kuyumcuPencereSayisi || 0) > 0) kilitCoz();
    }
    function onSabitle() {
        sayaciDogrula();
        if ((document.__kuyumcuPencereSayisi || 0) !== 0 || document.__kuyumcuOnSabit) return;
        sabitle();
        document.__kuyumcuOnSabit = true;
        // Güvenlik: pencere hiç açılmazsa (ac çağrılmadı) sabit KALICI olmasın —
        // aksi hâlde sonraki sayfalar eski belge boyunda boş alanla uzar (ölçüldü:
        // SONRA turunda Panel K4 1.2 → 2.8 kat).
        setTimeout(function () {
            if (document.__kuyumcuOnSabit && !document.querySelector('[aria-modal="true"]')) {
                document.__kuyumcuOnSabit = false;
                sabitiBirak();
            }
        }, 1000);
    }
    /** @returns {boolean} bu kilit İLK pencereninse true (konum yalnız o an saklandı). */
    function kilitle() {
        var ilk = !document.__kuyumcuPencereSayisi;
        if (ilk && !document.__kuyumcuOnSabit) sabitle();
        document.__kuyumcuOnSabit = false;
        document.__kuyumcuPencereSayisi = (document.__kuyumcuPencereSayisi || 0) + 1;
        document.body.classList.add('pencere-acik');
        return ilk;
    }
    function kilitCoz() {
        var n = (document.__kuyumcuPencereSayisi || 1) - 1;
        document.__kuyumcuPencereSayisi = n < 0 ? 0 : n;
        if (document.__kuyumcuPencereSayisi === 0) {
            document.body.classList.remove('pencere-acik');
            konumuKoru();
            setTimeout(sabitiBirak, SABIT_BIRAK_MS);
        }
    }

    function ac(panel) {
        if (!panel) return false;
        var ilkPencere = kilitle();
        panel.__kuyumcuOncekiOdak = document.activeElement;
        var liste = odaklanabilirler(panel);
        var hedef = liste.length > 0 ? liste[0] : panel;
        // ORTAK-1 (D): odak sayfayı KAYDIRMAZ (preventScroll); panel sabit
        // konumludur, görünür olması için kaydırma gerekmez.
        try { hedef.focus({ preventScroll: true }); } catch (e) { return false; }
        // Konum YALNIZ ilk pencerenin açılışında saklanmıştır; ikinci (iç içe/
        // ardışık) pencere eski konuma geri SARMAZ (ölçüldü: açık çekmecede başka
        // satırın "Düzenle"si sayfayı saklı konuma çekiyordu).
        if (ilkPencere) konumuKoru();

        panel.__kuyumcuHapis = function (olay) {
            if (olay.key !== 'Tab') return;
            var o = odaklanabilirler(panel);
            if (o.length === 0) { olay.preventDefault(); return; }
            var ilk = o[0], son = o[o.length - 1];
            if (olay.shiftKey && document.activeElement === ilk) {
                olay.preventDefault(); son.focus();
            } else if (!olay.shiftKey && document.activeElement === son) {
                olay.preventDefault(); ilk.focus();
            }
        };
        panel.addEventListener('keydown', panel.__kuyumcuHapis);
        return document.activeElement === hedef;
    }

    /** Paneli kapatır: hapsi kaldırır ve odağı TETİKLEYİCİYE geri verir. */
    function kapat(panel) {
        if (!panel) return false;
        kilitCoz();
        if (panel.__kuyumcuHapis) {
            panel.removeEventListener('keydown', panel.__kuyumcuHapis);
            panel.__kuyumcuHapis = null;
        }
        var onceki = panel.__kuyumcuOncekiOdak;
        panel.__kuyumcuOncekiOdak = null;
        if (onceki && onceki.focus && document.contains(onceki)) {
            // ORTAK-1 (D): tetikleyiciye dönen odak sayfayı kaydırmaz; konum
            // kilit çözülürken saklanan konumdan geri verilir.
            try { onceki.focus({ preventScroll: true }); return true; } catch (e) { /* kaldırılmış öğe */ }
        }
        return false;
    }

    /**
     * UX-1-C0(d) — "odak İLK HATALI ALANA". Boş/geçersiz gönderimde ekran alan
     * altı mesajı çizer; okuyucu ve klavye kullanıcısı için odağın da oraya
     * gitmesi gerekir, yoksa mesaj sayfanın görünmeyen bir yerinde kalır.
     *
     * ⚠ İKİNCİ MEKANİZMA DEĞİL: aynı dosyada, aynı ad alanında, yeni <script>
     *   etiketi AÇMADAN (denetim.sh: index.html <script> sayısı sabit 6).
     * @returns {boolean} odak gerçekten taşındıysa true (testler bunu okur)
     */
    function alanaOdakla(id) {
        if (!id) return false;
        var e = document.getElementById(id);
        if (!e) return false;
        try {
            // URUN-FORM-2: modal içinde scrollIntoView ATA KAPLARI da kaydırıyordu
            // (overflow:hidden panel programatik kayar) → hata satırı sabit alt şeridin
            // arkasına düşüyordu. Modalda yalnız gövde kaydırılır; alanın altındaki hata
            // satırı da görünür bölgeye alınır (alan kabuğunun alt kenarı esas).
            var govde = e.closest ? e.closest('.modal-govde') : null;
            e.focus({ preventScroll: true });
            if (govde) {
                var panel = govde.closest('.modal-panel');
                if (panel) panel.scrollTop = 0;
                var kabuk = (e.closest && e.closest('.alan')) || e;
                // Hata satırı odak anında henüz çizilmemiş olabilir (PRO: kabuk sonradan uzar);
                // ölçüm hemen VE çizimden sonra (iki kare) tekrarlanır.
                var goster = function () {
                    var g = govde.getBoundingClientRect();
                    var k = kabuk.getBoundingClientRect();
                    var pay = 12;
                    if (k.bottom > g.bottom - pay) govde.scrollTop += k.bottom - (g.bottom - pay);
                    if (k.top < g.top + pay) govde.scrollTop -= (g.top + pay) - k.top;
                };
                goster();
                if (window.requestAnimationFrame)
                    window.requestAnimationFrame(function () { goster(); window.requestAnimationFrame(goster); });
                setTimeout(goster, 150);
            } else if (e.scrollIntoView) {
                e.scrollIntoView({ block: 'center' });
            }
        } catch (h) { return false; }
        return document.activeElement === e;
    }

    window.kuyumcuCekmece = { ac: ac, kapat: kapat, sabitle: onSabitle, birak: birak };
    window.kuyumcuOdak = { alana: alanaOdakla };
})();
