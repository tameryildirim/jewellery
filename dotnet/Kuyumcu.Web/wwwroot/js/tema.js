// ═══════════════════════════════════════════════════════════════════════════
// TEMA UYGULAYICI — paket UX-2 Faz A1 (Mehmet K1/K2)
//
// Neden ayrı dosya ve adlandırılmış fonksiyon:
//   • CSP `script-src 'self' 'wasm-unsafe-eval'` (Kuyumcu.Api/Program.cs).
//     'unsafe-inline' YOK → index.html'e <script> BLOĞU YAZILAMAZ.
//     'wasm-unsafe-eval' JS eval()'ı KAPSAMAZ → eval KULLANILAMAZ.
//   • Bu yüzden C# tarafı `kuyumcuTema.uygula(mod)` adlı fonksiyonu çağırır;
//     dizge kod üretimi yoktur.
//
// İlk boyama (FOUC): bu dosya blazor.webassembly.js'ten ÖNCE yüklenir ve
// sonundaki `uygula(oku())` çağrısıyla tercih daha Blazor açılmadan uygulanır.
//
// KESIF-P1 · KIYAS TEMA-TEK: localStorage 'kuyumcu-tema' bir ÖNBELLEKTİR,
// doğruluk kaynağı DEĞİLDİR. Tek kaynak kullanıcı satırıdır
// (/kullanici/gorunum); Blazor açılışta ve her oturum değişiminde onu okuyup
// buradaki uygula()'yı çağırır (Program.cs → KullaniciGorunumServisi). Ekranda
// cihaza özel ikinci bir tema seçici YOKTUR.
//
// ⚠ "sistem" seçilirse öznitelik KALDIRILIR. `data-tema=""` YAZILMAZ:
//    boş dizge de bir öznitelik değeridir ve
//    `:root:not([data-tema="aydinlik"])` eşleşse bile
//    `:root[data-tema="karanlik"]` eşleşmez — ama asıl tuzak, gelecekte
//    `[data-tema]` varlığına bakan bir kuralın medya sorgusunu bastırmasıdır.
// ═══════════════════════════════════════════════════════════════════════════
(function () {
    'use strict';

    var ANAHTAR = 'kuyumcu-tema';
    var GECERLI = ['aydinlik', 'karanlik'];   // beyaz liste

    function gecerliMi(mod) {
        return GECERLI.indexOf(mod) !== -1;
    }

    /** Temayı uygular ve tercihi saklar. Beyaz liste dışı her değer = "sistem". */
    function uygula(mod) {
        var kok = document.documentElement;
        if (!gecerliMi(mod)) {
            kok.removeAttribute('data-tema');
            try { localStorage.removeItem(ANAHTAR); } catch (e) { /* özel sekme */ }
            return 'sistem';
        }
        kok.setAttribute('data-tema', mod);
        try { localStorage.setItem(ANAHTAR, mod); } catch (e) { /* özel sekme */ }
        return mod;
    }

    /** Saklı tercihi döner; yoksa ya da bozuksa "sistem". */
    function oku() {
        var deger = null;
        try { deger = localStorage.getItem(ANAHTAR); } catch (e) { deger = null; }
        return gecerliMi(deger) ? deger : 'sistem';
    }

    window.kuyumcuTema = { uygula: uygula, oku: oku };

    uygula(oku());   // ilk boyamadan önce
})();

// ═══════════════════════════════════════════════════════════════════════════
// ARAYÜZ MODU UYGULAYICI — paket UX-2 Faz A2
//
// belirtecler.css'teki html[data-mod="basit"|"profesyonel"] blokları YALNIZ
// ÖLÇÜ belirteci değiştirir (renk ve kontrast sözleşmesi sabittir).
//
// ⚠ MOD BİR TERCİHTİR, YETKİ DEĞİLDİR. Değer SUNUCUDAN gelir
//   (/auth/ben → etkinArayuzModu); burada yalnız BEYAZ LİSTE yazılır.
//   Beyaz liste dışı her değerde öznitelik KALDIRILIR (tema.js'teki
//   "boş dizge yazma" tuzağının aynısı).
// ⚠ Yeni <script> etiketi EKLENMEZ: index.html betik sayısı sayaca bağlıdır
//   (denetim.sh). Bu yüzden ayrı dosya değil, aynı dosyada ikinci ad alanı.
// ═══════════════════════════════════════════════════════════════════════════
(function () {
    'use strict';

    var GECERLI = ['basit', 'profesyonel'];   // beyaz liste (küçük harf)

    /** html[data-mod] özniteliğini yazar. Beyaz liste dışı = öznitelik yok. */
    function uygula(mod) {
        var kok = document.documentElement;
        var n = (mod || '').toString().toLowerCase();
        if (GECERLI.indexOf(n) === -1) {
            kok.removeAttribute('data-mod');
            return '';
        }
        kok.setAttribute('data-mod', n);
        return n;
    }

    window.kuyumcuMod = { uygula: uygula };
})();

// ═══════════════════════════════════════════════════════════════════════════
// GÖRÜNÜM UYGULAYICI — renk şeması + yazı tipi ölçeği (paket P-36b)
//
// Kurucu talimatı 16: "renk ve yazı tipi benim işim." Seçim KİRACI bazındadır
// ve SUNUCUDAN gelir (/api/v1/gorunum → kiraci_ayarlari). Burada YALNIZ beyaz
// liste yazılır — tema.js ve mod.js'teki desenin birebir aynısı.
//
// ⚠ Yeni <script> etiketi EKLENMEZ: index.html betik sayısı sayaca bağlıdır.
//   Bu yüzden ayrı dosya değil, aynı dosyada üçüncü ad alanı.
// ⚠ Beyaz liste dışı her değerde öznitelik KALDIRILIR (boş dizge YAZILMAZ —
//   tema.js'in başındaki tuzağın aynısı).
// ⚠ TEMA TEMİZLİĞİ (kurucu 17.10): TEK ŞEMA "yildiz". data-sema="yildiz"
//   özniteliği KALICIDIR — ilk boyamadan önce yazılır ve sunucu ne derse
//   desin (eski istemci/bozuk değer) değişmez; tema-yildiz.css'in kapsamlı
//   seçicileri buna dayanır. Açık/koyu AYRI ad alanıdır (kuyumcuTema, data-tema).
// ═══════════════════════════════════════════════════════════════════════════
(function () {
    'use strict';

    var SEMA = 'yildiz';
    // ⚠ S1-a: "yogun" artık 16 px kademesini AÇIKÇA yazar (varsayılan 18 px);
    //   "okunakli" 20 px kademesidir. Eski kayıtlar çalışmaya devam eder.
    // ⚠ B-1: sunucu artık KANONİK punto kodu gönderir ('16' | '18' | '20') ve
    //   belirtecler.css bunları `:root[data-yazi="…"]` ile okur. Eski iki kod
    //   beyaz listede KALDI (data-olcek seçicileri CSS'te hâlâ duruyor).
    var OLCEKLER = ['yogun', 'okunakli'];
    var PUNTOLAR = ['16', '18', '20'];

    function yaz(ad, deger, gecerliler) {
        var kok = document.documentElement;
        var n = (deger || '').toString().toLowerCase();
        if (gecerliler.indexOf(n) === -1) { kok.removeAttribute(ad); return ''; }
        kok.setAttribute(ad, n);
        return n;
    }

    /** Renk şemasını uygular — tek şema; gelen değer ne olursa olsun "yildiz" yazılır. */
    function sema() {
        document.documentElement.setAttribute('data-sema', SEMA);
        return SEMA;
    }

    /**
     * Yazı boyutunu uygular.
     * ⚠ İKİ ÖZNİTELİK, TEK KARAR: kanonik punto kodu `data-yazi`ye, eski kod
     *   `data-olcek`e yazılır ve ÖTEKİ HER ZAMAN KALDIRILIR — ikisi bir arada
     *   kalıp çelişen iki blok uygulanamaz. Varsayılan ('18') hiçbir öznitelik
     *   yazmaz: kök zaten 18 px'tir.
     */
    function olcek(deger) {
        var kok = document.documentElement;
        var n = (deger || '').toString().toLowerCase();
        if (PUNTOLAR.indexOf(n) !== -1) {
            kok.removeAttribute('data-olcek');
            if (n === '18') { kok.removeAttribute('data-yazi'); return '18'; }
            kok.setAttribute('data-yazi', n);
            return n;
        }
        kok.removeAttribute('data-yazi');
        return yaz('data-olcek', deger, OLCEKLER) || '18';
    }

    /** İkisini birden — sunucudan gelen görünüm kaydı. */
    function uygula(g) {
        g = g || {};
        return { sema: sema(), olcek: olcek(g.yaziOlcegi) };
    }

    window.kuyumcuGorunum = { uygula: uygula, sema: sema, olcek: olcek };

    // İlk boyamadan önce: tek şema köke yazılır — oturumsuz /giris de Yıldız ile çizilir (FOUC yok).
    sema();
})();
