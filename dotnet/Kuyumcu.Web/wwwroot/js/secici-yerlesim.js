// ═══════════════════════════════════════════════════════════════════════════
// AÇILIR PANEL YERLEŞİMİ — paket P-39 · K6
//
// ÖLÇÜLDÜ (TASARIM-DENETIMI-2026-09-21.md § 8.2): 5 vakada seçici paneli
// GÖRÜŞ ALANININ TAMAMEN ALTINDA açılıyordu — `/isletme` @1024'te üç panel
// (136 / 376 / 471 px aşağıda), `/hurda` ödeme türü (1280'de 249, 1024'te
// 407 px). Kullanıcı açısından "seçiciye bastım, hiçbir şey olmadı".
//
// TEK MEKANİZMA: panel açıldığında yer ölçülür; ALTTA yer yoksa ve ÜSTTE
// varsa `.yukari` sınıfı konur (CSS panel'i tetiğin üstüne alır). Her iki
// durumda da panel görüş alanına kaydırılır. İkinci bir yerleşim deseni
// AÇILMAZ — Secici.razor tek çağırandır.
//
// KATMAN-1 (tur 3, 2026-09-25): KIRPAN ATA. Görüş alanı yeterli olsa da panel
// `overflow ≠ visible` bir atanın (ör. Satış PRO `.islem-pro-kalem-kart`,
// overflow:hidden, 339–647) İÇİNDEYSE atanın kutusu dışında kalan kısmı
// KESİLİYORDU (doğrulayıcı ölçümü: #islem-urun listesi 114–441'de, yalnız
// 339 altı görünür; arama kutusu ve ilk öğeler görünmez). Karar artık
// "panel açıldığı yerde kırpan atanın kutusuna sığar mı?" sorusunu da sorar;
// sığmıyorsa panel `.sabit` olur: position:fixed ile kırpan atadan KURTULUR
// (css/katman.css), konumu tetiğe göre belirteçle (--secici-ust/-alt/-sol/
// -gen) verilir; sayfa/kap kaydırılınca tetikle birlikte yeniden konumlanır.
//
// ⚠ Anayasa md.1: ölçüm başarısızsa (öğe yok) sessizce "oldu" denmez, false
//   döner; çağıran davranışı değiştirmez, eski (aşağı açılan) yerleşim kalır.
// ⚠ CSP: script-src 'self' — satır içi <script> ve eval() YOK.
// ═══════════════════════════════════════════════════════════════════════════
(function () {
    'use strict';

    var PAY = 8;   // tetik ile katlama arasında bırakılan asgari boşluk (px)
    var ASGARI_ALT = 240;   // KATMAN-1: altta bundan az yer varsa ve üstte daha çok varsa yukarı aç
    var ARALIK = 4;   // tetik ile panel arası (= var(--bosluk-1, .25rem))

    function kirparMi(s) {
        if (!s) return false;
        var x = s.overflowX || s.overflow || 'visible';
        var y = s.overflowY || s.overflow || 'visible';
        return x !== 'visible' || y !== 'visible';
    }

    // Panelin atalarından overflow ≠ visible olanların kutularının KESİŞİMİ.
    // Kırpan ata yoksa (ya da ölçüm yapılamıyorsa) null.
    function kirpanKutu(panel) {
        var gcs = window.getComputedStyle;
        if (typeof gcs !== 'function') return null;
        var kutu = null;
        var el = panel.parentElement;
        while (el && el !== document.body && el !== document.documentElement) {
            if (kirparMi(gcs.call(window, el))) {
                var r = el.getBoundingClientRect();
                kutu = kutu
                    ? { top: Math.max(kutu.top, r.top), bottom: Math.min(kutu.bottom, r.bottom),
                        left: Math.max(kutu.left, r.left), right: Math.min(kutu.right, r.right) }
                    : { top: r.top, bottom: r.bottom, left: r.left, right: r.right };
            }
            el = el.parentElement;
        }
        return kutu;
    }

    // `.sabit` panelin konumu: tetiğe göre, görüş alanına kenetli (8 px pay).
    function sabitKonumla(panel, tetik, yukari, gen) {
        var t = tetik.getBoundingClientRect();
        var gorus = window.innerHeight || document.documentElement.clientHeight;
        var genis = window.innerWidth || document.documentElement.clientWidth;
        var w = Math.min(gen, genis - 2 * PAY);
        var sol = t.left;
        if (sol + w > genis - PAY) sol = t.right - w;          // sağa taşıyorsa sağ kenarlar hizalanır
        sol = Math.max(PAY, Math.min(sol, genis - PAY - w));    // yine de ekran içinde kalır
        var azami = Math.max(0, Math.floor(yukari ? t.top - ARALIK - PAY : gorus - t.bottom - ARALIK - PAY));
        panel.style.setProperty('--secici-gen', w + 'px');
        panel.style.setProperty('--secici-sol', sol + 'px');
        panel.style.setProperty('--secici-azami-boy', azami + 'px');
        if (yukari) {
            panel.style.removeProperty('--secici-ust');
            panel.style.setProperty('--secici-alt', (gorus - t.top + ARALIK) + 'px');
        } else {
            panel.style.removeProperty('--secici-alt');
            panel.style.setProperty('--secici-ust', (t.bottom + ARALIK) + 'px');
        }
        // Kapsayan blok düzeltmesi: dönüşümlü (transform) bir ata fixed'in
        // referansını değiştirirse ölçülen fark belirtece eklenir.
        var p = panel.getBoundingClientRect();
        var dx = sol - p.left;
        if (Math.abs(dx) > 0.5) panel.style.setProperty('--secici-sol', (sol + dx) + 'px');
        if (yukari) {
            var dyA = p.bottom - (t.top - ARALIK);
            if (Math.abs(dyA) > 0.5) panel.style.setProperty('--secici-alt', (gorus - t.top + ARALIK + dyA) + 'px');
        } else {
            var dyU = (t.bottom + ARALIK) - p.top;
            if (Math.abs(dyU) > 0.5) panel.style.setProperty('--secici-ust', (t.bottom + ARALIK + dyU) + 'px');
        }
    }

    // Açık `.sabit` paneller: sayfa/kap kaydırılınca ya da pencere boyu
    // değişince tetikle birlikte yeniden konumlanır. Panelin KENDİ iç
    // kaydırması yeniden yerleştirmez (katman.js K1_12 dersi: scrollTop sıfırlanmasın).
    var sabitler = [];
    var dinleniyor = false;

    function hepsiniYenile(e) {
        var hedef = e && e.target;
        sabitler = sabitler.filter(function (k) {
            var panel = document.getElementById(k.panelId);
            var tetik = document.getElementById(k.tetikId);
            if (!panel || !tetik || panel !== k.panel || !panel.classList.contains('sabit')) return false;
            if (hedef && hedef !== document && typeof panel.contains === 'function' && panel.contains(hedef)) return true;
            sabitKonumla(panel, tetik, k.yukari, k.gen);
            return true;
        });
    }

    function dinle() {
        if (dinleniyor || typeof window.addEventListener !== 'function') return;
        dinleniyor = true;
        window.addEventListener('scroll', hepsiniYenile, true);
        window.addEventListener('resize', hepsiniYenile);
    }

    function yerlestir(panelId, tetikId) {
        var panel = document.getElementById(panelId);
        var tetik = document.getElementById(tetikId);
        if (!panel || !tetik) return false;

        panel.classList.remove('yukari');
        panel.classList.remove('sola');
        panel.classList.remove('kaydir');
        panel.classList.remove('sabit');
        panel.style.removeProperty('--secici-kaydir');
        panel.style.removeProperty('--secici-azami-boy');
        panel.style.removeProperty('--secici-ust');
        panel.style.removeProperty('--secici-alt');
        panel.style.removeProperty('--secici-sol');
        panel.style.removeProperty('--secici-gen');
        panel.removeAttribute('data-katman-azami');
        sabitler = sabitler.filter(function (k) { return k.panelId !== panelId; });

        var t = tetik.getBoundingClientRect();
        var p = panel.getBoundingClientRect();
        var gorus = window.innerHeight || document.documentElement.clientHeight;

        // Görsel denetim D-14 (21.10): Yıldız formlarında (.yz-duzen) liste AKIŞ İÇİNDEDİR (position: relative — tuval:
        // açılan liste altındaki alanları iter). Orada yukarı açma uygulanamaz; /gider "Personel" listesi görüş alanının
        // 52 px altına taşıyordu. Akış içi listede panel en çok görüş alanı boyunda olur ve AÇILDIKTAN SONRA kendisi
        // görüş alanına kaydırılır (tetik üstte görünür kalır).
        var konum = typeof window.getComputedStyle === 'function' ? window.getComputedStyle(panel).position : '';
        if (konum === 'relative' || konum === 'static') {
            panel.style.setProperty('--secici-azami-boy', Math.max(0, Math.floor(gorus - t.height - 2 * PAY - ARALIK)) + 'px');
            panel.setAttribute('data-katman-azami', '');
            window.requestAnimationFrame(function () {
                if (typeof panel.scrollIntoView === 'function') panel.scrollIntoView({ block: 'nearest', inline: 'nearest' });
            });
            return true;
        }

        var altYer = gorus - t.bottom - PAY;
        var ustYer = t.top - PAY;

        // Altta sığmıyor VE üstte daha çok yer varsa panel YUKARI açılır.
        // KATMAN-1: uzak (sunucudan dolan) liste yerleşimden SONRA büyüyordu (tur: 960 px
        // #islem-cari-liste-kap alt kenarı ekranın 4 px dışına). Karar yalnız o anki
        // boya değil, altta kalan alanın asgarisine (ASGARI_ALT) de bakar; seçilen
        // taraftaki kalan alan panelin AZAMİ boyu olur → liste büyüse de ekran dışına
        // çıkmaz, kendi içinde kayar (belirteç --secici-azami-boy, css/katman.css).
        var yukari = (p.height > altYer || altYer < ASGARI_ALT) && ustYer > altYer;
        if (yukari) {
            panel.classList.add('yukari');
        }
        var azami = Math.max(0, Math.floor(yukari ? ustYer : altYer));
        panel.style.setProperty('--secici-azami-boy', azami + 'px');
        panel.setAttribute('data-katman-azami', '');

        // KATMAN-1 tur 3: KIRPAN ATA. Panelin açılacağı kutu (liste sonradan
        // büyüyebileceği için en az ASGARI_ALT boyunda varsayılır) kırpan atanın
        // kutusuna sığmıyorsa panel `.sabit` olur ve atadan kurtulur.
        var kirpan = kirpanKutu(panel);
        if (kirpan) {
            var h = Math.min(Math.max(p.height, ASGARI_ALT), azami);
            var ust = yukari ? t.top - ARALIK - h : t.bottom + ARALIK;
            var alt = ust + h;
            var kesik = ust < kirpan.top - 1 || alt > kirpan.bottom + 1
                || p.left < kirpan.left - 1 || p.right > kirpan.right + 1;
            if (kesik) {
                panel.classList.add('sabit');
                sabitKonumla(panel, tetik, yukari, p.width);
                sabitler.push({ panelId: panelId, tetikId: tetikId, panel: panel, yukari: yukari, gen: p.width });
                dinle();
                if (typeof tetik.scrollIntoView === 'function') {
                    tetik.scrollIntoView({ block: 'nearest', inline: 'nearest' });
                }
                return true;
            }
        }

        // PAKET S1-i: panel artık tetikten GENİŞ olabilir (min 20rem). Sağ kenardan
        // taşıyorsa tetiğin SAĞ kenarına hizalanır (sola doğru açılır). Ölçüm
        // yeniden alınır: `.yukari` yüksekliği değiştirmez ama genişlik içeriğe bağlı.
        var genis = window.innerWidth || document.documentElement.clientWidth;
        var p2 = panel.getBoundingClientRect();
        if (p2.right > genis - PAY && t.right - p2.width >= PAY) {
            panel.classList.add('sola');
        }

        // Hâlâ taşıyorsa (akış içi şerit paneli `.sola`dan etkilenmez; ölçüldü:
        // /uyum 375 px'te 88 px) panel ekran içine KAYDIRILIR, sol kenar PAY'ın
        // altına inmez. Değer bir CSS belirtecidir; satır içi düzen kuralı yazılmaz.
        var p3 = panel.getBoundingClientRect();
        var tasma = p3.right - (genis - PAY);
        if (tasma > 0) {
            var kaydir = Math.min(tasma, Math.max(0, p3.left - PAY));
            if (kaydir > 0) {
                panel.style.setProperty('--secici-kaydir', (-kaydir) + 'px');
                panel.classList.add('kaydir');
            }
        }

        // Yön ne olursa olsun panel görüş alanına getirilir (tetik de görünsün).
        if (typeof tetik.scrollIntoView === 'function') {
            tetik.scrollIntoView({ block: 'nearest', inline: 'nearest' });
        }
        return true;
    }

    window.kuyumcuSecici = { yerlestir: yerlestir };
})();
