// ═════════════════════════════════════════════════════════════════════════════
// PAKET GIRDI-AILESI-2 — GİRDİ MASKESİ (kurucu 08.10: "telefon gibi sayısal alanlara
// harf yazılamaz — tuş anında + yapıştırma temizliği; tarih 08102026 → 08.10.2026,
// mantıksız yıl yazılamaz; IBAN yazılırken maske").
//
// TEK DOSYA, TEK KALIP: ortak girdi bileşenleri (Kuyumcu.Web/Bilesenler/Form/*)
// kutuya data-maske="<tür>" yazar; bu dosya belge düzeyinde (window, capture)
// dinler — sayfa başına kod yok, JS birlikte çalışma (interop) yok.
//   beforeinput : maskeden geçmeyecek tuş (harf, fazladan hane, pencere dışı yıl)
//                 HİÇ yazılmaz.
//   paste       : panodaki metin maskeden geçirilip yazılır (rakam dışı temizlenir).
//   input       : kutu maskeli biçime getirilir (gruplama, nokta, büyük harf);
//                 imleç anlamlı karakter sayısıyla korunur. Blazor'un kendi input
//                 dinleyicisi (kabarcık evresi) DEĞİŞMİŞ değeri okur.
// ⚠ Maske algoritmaları C# tarafında AYNEN vardır (Iban.Maskele, Telefon.Maskele,
//   Tarih.Maskele …) → JS'siz ortamda da aynı sonuç; ikisi aynı çıktıyı verdiği için
//   Blazor kutuyu yeniden yazmaz, imleç atlamaz.
// ⚠ Kural (mod-97, aralık, pencere) burada YOKTUR: kural Kuyumcu.Core'dadır; burada
//   yalnız "yazılabilir mi" vardır. CSP: satır içi betik yok, eval yok.
// ═════════════════════════════════════════════════════════════════════════════
(function () {
    'use strict';

    function rakamlar(s) { return (s || '').replace(/[^0-9]/g, ''); }

    function grupla(s, gruplar) {
        var parca = [], i = 0;
        for (var k = 0; k < gruplar.length && i < s.length; k++) {
            parca.push(s.substr(i, gruplar[k]));
            i += gruplar[k];
        }
        if (i < s.length) parca.push(s.substr(i));
        return parca.join(' ');
    }

    var IBAN_TR = 26, IBAN_EN_COK = 34, PASAPORT = 20, BARKOD = 50;

    // IBAN yapıştırma temizliği: "IBAN: TR33-…" ön ekini atar (Iban.BaslangicaKes ile AYNI).
    function baslangicaKes(v) {
        var s = v.toUpperCase().replace(/[^A-Z0-9]/g, '');
        function bas(i, yalnizTr) {
            return i + 4 <= s.length && /^[A-Z]{2}[0-9]{2}$/.test(s.substr(i, 4)) && (!yalnizTr || s.substr(i, 2) === 'TR');
        }
        if (bas(0, false)) return s;
        var i;
        for (i = 1; i + 4 <= s.length; i++) if (bas(i, true)) return s.substr(i);
        for (i = 1; i + 4 <= s.length; i++) if (bas(i, false)) return s.substr(i);
        return s;
    }

    var MASKE = {
        iban: function (v) {
            var b = '';
            var s = baslangicaKes(v || '');
            for (var i = 0; i < s.length; i++) {
                var c = s[i];
                var harf = c >= 'A' && c <= 'Z', rakam = c >= '0' && c <= '9';
                if (!harf && !rakam) continue;
                if (b.length < 2) {
                    if (harf) { b += c; continue; }
                    if (b.length === 0) b = 'TR'; else continue;
                }
                var tr = b[0] === 'T' && b[1] === 'R';
                var sinir = tr ? IBAN_TR : IBAN_EN_COK;
                if (b.length >= sinir) break;
                if (tr && b.length < 10 && !rakam) continue;
                if (!tr && b.length < 4 && !rakam) continue;
                b += c;
            }
            return grupla(b, [4, 4, 4, 4, 4, 4, 4, 4, 4]);
        },
        telefon: function (v) {
            var t = (v || '').replace(/^\s+/, '');
            // tur 3: yapıştırma öneki ("Tel: +90 …") — ilk rakamdan ÖNCE '+' varsa uluslararası (Telefon.Maskele ile AYNI)
            var ilk = t.search(/[0-9]/);
            var arti = (ilk < 0 ? t : t.substr(0, ilk)).indexOf('+') >= 0;
            var r = rakamlar(t);
            if (arti) {
                if (r.indexOf('90') === 0) {
                    var u = r.substr(2, 10);
                    return u.length === 0 ? '+90' : '+90 ' + grupla(u, [3, 3, 2, 2]);
                }
                return '+' + r.substr(0, 15);
            }
            // tur 3: "00" uluslararası önek — kırpılmaz ("0090" → TR gruplaması)
            if (r.length > 2 && r.indexOf('00') === 0 && r.charAt(2) !== '0') {
                var d = r.substr(2);
                if (d.indexOf('90') === 0) {
                    var n = d.substr(2, 10);
                    return n.length === 0 ? '0090' : '0090 ' + grupla(n, [3, 3, 2, 2]);
                }
                return '00' + d.substr(0, 15);
            }
            if (r.indexOf('0444') === 0 && r.length <= 8) return grupla(r, [4, 2, 2]);   // 444 tek numara (Telefon.Maskele ile AYNI)
            if (r.indexOf('444') === 0 && r.length <= 7) return grupla(r, [3, 2, 2]);
            if (r.charAt(0) === '0') return grupla(r.substr(0, 11), [4, 3, 2, 2]);
            return grupla(r.substr(0, 10), [3, 3, 2, 2]);
        },
        tarih: function (v) {
            var t = (v || '').trim();
            if (/^\d{4}-\d{2}-\d{2}$/.test(t)) t = t.substr(8, 2) + t.substr(5, 2) + t.substr(0, 4);
            var r = rakamlar(t).substr(0, 8);
            if (r.length <= 2) return r;
            if (r.length <= 4) return r.substr(0, 2) + '.' + r.substr(2);
            return r.substr(0, 2) + '.' + r.substr(2, 2) + '.' + r.substr(4);
        },
        rakam: function (v, el) { var n = +((el && el.getAttribute('data-hane')) || 64); return rakamlar(v).substr(0, n); },
        eposta: function (v) { return (v || '').replace(/[\s\u0000-\u001f\u007f]/g, ''); },
        pasaport: function (v) { return (v || '').toUpperCase().replace(/[^A-Z0-9]/g, '').substr(0, PASAPORT); },
        barkod: function (v) { return (v || '').toUpperCase().replace(/[^\x21-\x7e]/g, '').substr(0, BARKOD); },
        ad: function (v) { return (v || '').replace(/[\u0000-\u001f\u007f]/g, '').replace(/\s/g, ' ').replace(/^ +/, '').replace(/ {2,}/g, ' '); },
        sayi: function (v, el) {
            var eksi = el && el.getAttribute('data-eksi') === '1';
            var s = (v || '').trim();
            var basEksi = eksi && s.charAt(0) === '-';
            // GOC-C-2 VERI-2 (tasarım hakemi, VERİ): tam sayı alanında ayırıcı ARTIK SİLİNMEZ. Eskiden "1,5" → "15"
            // (yapıştırma ve input yolu; tuşta virgül engellenip ardından gelen 5 eklenince yine 15) — sessiz 10 kat.
            // Ayırıcı kalır → C# (SayisalAlan) "Küsuratlı yazılamaz" der, Kaydet kilitli. Tuştaki engel aşağıda (beforeinput).
            s = s.replace(/[^0-9.,]/g, '');
            return (basEksi ? '-' : '') + s;
        }
    };

    // Tarih öneki hâlâ geçerli bir güne tamamlanabilir mi? (gün 01–31, ay 01–12,
    // yıl pencerede: data-yil-en-az … data-yil-en-cok — 3000 ve 1899 HİÇ yazılamaz)
    function tarihOnekGecerli(maskeli, el) {
        var r = rakamlar(maskeli);
        if (r.length >= 1 && r[0] > '3') return false;
        if (r.length >= 2) { var g = +r.substr(0, 2); if (g < 1 || g > 31) return false; }
        if (r.length >= 3 && r[2] > '1') return false;
        if (r.length >= 4) { var a = +r.substr(2, 2); if (a < 1 || a > 12) return false; }
        if (r.length >= 5) {
            var enAz = +(el.getAttribute('data-yil-en-az') || 1900);
            var enCok = +(el.getAttribute('data-yil-en-cok') || 2100);
            var p = r.substr(4), k = p.length, carpan = Math.pow(10, 4 - k);
            var alt = +p * carpan, ust = alt + carpan - 1;
            if (ust < enAz || alt > enCok) return false;
        }
        return true;
    }

    function maskele(el, v) { var f = MASKE[el.getAttribute('data-maske')]; return f ? f(v, el) : v; }

    function maskeliKutu(e) {
        var el = e.target;
        return el && el.tagName === 'INPUT' && el.hasAttribute('data-maske') && MASKE[el.getAttribute('data-maske')] ? el : null;
    }

    // Anlamlı karakter (boşluk ve tarih noktası dışındaki) sayısıyla imleç konumu.
    function anlamliSay(s, son) {
        var n = 0;
        for (var i = 0; i < son && i < s.length; i++) if (s[i] !== ' ' && s[i] !== '.') n++;
        return n;
    }
    function konumBul(s, n) {
        if (n <= 0) return 0;
        var say = 0;
        for (var i = 0; i < s.length; i++) {
            if (s[i] !== ' ' && s[i] !== '.') say++;
            if (say === n) return i + 1;
        }
        return s.length;
    }

    // ── GOC-C-2 VERI-2: tam sayı alanında ondalık VİRGÜLÜ yazılmaz — ve ardından gelen rakamlar da ─────────
    // Eski akış: "1" "," (engellendi) "5" → kutu "15" (sessiz 10 kat). Şimdi virgül engellenince kutu "kesir kipi"ne
    // girer: kutunun SONUNA yazılan rakamlar (kesir haneleri) da yazılmaz ve kutunun altındaki ipucu
    // (<id>-kesir, bileşen çizer) görünür. Silme, ok tuşu, odak kaybı kipi kapatır. Kural C#'tadır; burada yalnız tuş.
    // GOC-C-2 tur 2 (hasım KRİTİK): "." tr-TR BİNLİK ayırıcıdır — engellenmez, kesir kipine SOKMAZ. "45.000" kutuda
    // "45.000" kalır ve C# FormAlani.SayiCoz'a bırakılır (= 45000; "1.500" Belirsiz, "1.5" küsurat → alan altı hata,
    // Kaydet kilitli). Eskiden "." de engellenip kesir kipi açılıyordu: "45.000" → "45" (sessiz 1000 kat küçülme).
    function tamSayiKutusu(el) { return el.getAttribute('data-maske') === 'sayi' && el.getAttribute('data-tam') === '1'; }
    function kesirIpucu(el, acik) {
        var ip = el.id ? document.getElementById(el.id + '-kesir') : null;
        if (ip) { if (acik) ip.removeAttribute('hidden'); else ip.setAttribute('hidden', ''); }
    }
    function kesirKipi(el, acik) {
        if (acik) el.setAttribute('data-kesir-engel', '1'); else el.removeAttribute('data-kesir-engel');
        kesirIpucu(el, acik);
    }
    window.addEventListener('keydown', function (e) {
        var el = maskeliKutu(e);
        if (el && el.hasAttribute('data-kesir-engel') && /^(Arrow|Home|End|Backspace|Delete|Escape)/.test(e.key || '')) kesirKipi(el, false);
    }, true);
    window.addEventListener('focusout', function (e) {
        var el = maskeliKutu(e);
        // Odak kaybında ENGEL kipi kapanır ama ipucu KALIR: kullanıcı "1,5" yazıp çıktıysa kutuda "1" olduğunu ve
        // nedenini görmeye devam eder (sessiz kırpma yok); kutuya yeniden yazınca ipucu kalkar.
        if (el && el.hasAttribute('data-kesir-engel')) el.removeAttribute('data-kesir-engel');
    }, true);

    // ── 1) tuş: maskeden geçmeyecek karakter hiç yazılmaz ─────────────────
    window.addEventListener('beforeinput', function (e) {
        var el = maskeliKutu(e);
        if (!el) return;
        if (tamSayiKutusu(el) && e.inputType && e.inputType.indexOf('delete') === 0) kesirKipi(el, false);
        if (e.inputType !== 'insertText' || e.data == null) return;   // silme/yapıştırma ayrı
        var v = el.value, bas = el.selectionStart, son = el.selectionEnd;
        if (bas == null) return;
        // TOPLU-7A: kutuda yalnız "0" varken (ör. ödeme tutarı 0) rakama basınca "099999" olmaz — "0" yer tutucudur, yerine geçilir.
        // (Seçili metnin üstüne yazma zaten değiştirir; burada imleç çökük ve kutu tam "0".) Virgül/sıfır tuşu etkilenmez.
        if (el.getAttribute('data-maske') === 'sayi' && v === '0' && bas === son && /^[1-9][0-9]*$/.test(e.data)) {
            e.preventDefault();
            el.value = e.data;
            try { el.setSelectionRange(e.data.length, e.data.length); } catch (x) { /* seçim desteklenmiyor */ }
            el.dispatchEvent(new Event('input', { bubbles: true }));
            return;
        }
        if (tamSayiKutusu(el)) {
            var sonda = son === v.length;
            if (e.data.indexOf(',') >= 0 || (el.hasAttribute('data-kesir-engel') && sonda && /^[0-9]+$/.test(e.data))) {
                e.preventDefault();
                el.setAttribute('data-maske-engel', String(Date.now()));
                kesirKipi(el, true);
                return;
            }
            kesirKipi(el, false);   // geçerli rakam: kip ve ipucu kapanır
        }
        var once = maskele(el, v.substring(0, bas) + v.substring(son));
        var aday = maskele(el, v.substring(0, bas) + e.data + v.substring(son));
        if (aday === once || (el.getAttribute('data-maske') === 'tarih' && !tarihOnekGecerli(aday, el))) {
            e.preventDefault();
            el.setAttribute('data-maske-engel', String(Date.now()));   // ölçüm/inceleme için iz
        }
    }, true);

    // ── 2) yapıştırma: pano temizlenip yazılır ────────────────────────────
    window.addEventListener('paste', function (e) {
        var el = maskeliKutu(e);
        if (!el || !e.clipboardData) return;
        e.preventDefault();
        var metin = e.clipboardData.getData('text') || '';
        var v = el.value, bas = el.selectionStart || 0, son = el.selectionEnd || bas;
        var on = v.substring(0, bas), arka = v.substring(son);
        if (el.getAttribute('data-maske') === 'tarih' && /^\s*\d{4}-\d{2}-\d{2}\s*$/.test(metin)) metin = MASKE.tarih(metin);
        var sonuc = maskele(el, on + metin + arka);
        if (el.getAttribute('data-maske') === 'tarih') {
            while (sonuc.length > 0 && !tarihOnekGecerli(sonuc, el)) sonuc = MASKE.tarih(rakamlar(sonuc).slice(0, -1));
        }
        var hedef = anlamliSay(maskele(el, on + metin), (maskele(el, on + metin)).length);
        el.value = sonuc;
        var k = konumBul(sonuc, hedef);
        try { el.setSelectionRange(k, k); } catch (x) { /* type=email seçim desteklemez */ }
        el.dispatchEvent(new Event('input', { bubbles: true }));
    }, true);

    // ── 3) her değişim: maskeli biçim (Blazor'un dinleyicisinden ÖNCE) ────
    window.addEventListener('input', function (e) {
        var el = maskeliKutu(e);
        if (!el) return;
        var v = el.value, m = maskele(el, v);
        if (m === v) return;
        var imlec = el.selectionStart == null ? v.length : el.selectionStart;
        var n = anlamliSay(v, imlec);
        // kapanış KRİTİK (08.10): rakamla başlanınca maske 'TR' önekini kendisi ekler;
        // imleç o iki harfi de geçmeli, yoksa 'T|R3'te takılır ve sonraki haneler engellenir.
        if (el.getAttribute('data-maske') === 'iban' && /^[0-9]/.test(v.toUpperCase().replace(/[^A-Z0-9]/g, '')) && m.substring(0, 2) === 'TR') n += 2;
        el.value = m;
        var k = konumBul(m, n);
        try { el.setSelectionRange(k, k); } catch (x) { /* type=email */ }
    }, true);

    window.girdiMaske = {
        /** Ölçüm/test için: verilen türün maskesi (C# Maskele ile aynı olmalı). */
        maskele: function (tur, v) { var f = MASKE[tur]; return f ? f(v, null) : v; }
    };
})();
