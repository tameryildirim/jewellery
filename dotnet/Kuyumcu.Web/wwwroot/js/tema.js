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
// ⚠ VARSAYILAN = "altin" / "yogun": öznitelik hiç yazılmaz, mevcut kiracının
//   ekranı bu paketten sonra HİÇ değişmez.
// ═══════════════════════════════════════════════════════════════════════════
(function () {
    'use strict';

    // ⚠ S1-a: 8 renk şeması (belirtecler.css'te tanımlı) + iki ESKİ kod.
    //   "kurumsal" → Platin Mavi, "zumrut" → yeni Zümrüt: kiraci_ayarlari'ndaki
    //   kayıtlı değerler geçersizleşmesin diye beyaz listede KALDILAR.
    //   Beyaz liste dışı değer YAZILMAZ; öznitelik kaldırılır (varsayılan = Platin).
    var SEMALAR = ['platin', 'zumrut', 'grafit', 'bordo', 'okyanus', 'menekse',
                   'zeytin', 'fildisi', 'kurumsal', 'yildiz'];
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

    // ⚠ PAKET YILDIZ-AMBLEM-3 (ekip oyu 10.10): GİRİŞ ÖNCESİ ŞEMA = BU CİHAZDA EN SON KULLANILAN ŞEMA.
    //   Oturum yokken /gorunum okunamaz (kimlik ister) ve kiracı bilinmeden kiracının şeması bilinemez;
    //   bu yüzden sunucudan gelen her şema uygulamasında (oturum açılınca / şema değişince) değer bu
    //   cihaza 'mihenk-son-sema' olarak yazılır ve giriş sayfası ilk boyamadan önce onu okur (FOUC yok).
    //   Yalnız GÖRÜNÜM ÖNBELLEĞİDİR — doğruluk kaynağı sunucudur; veri/yetki/güvenlik taşımaz.
    //   Hiç giriş yapılmamış cihazda anahtar yoktur → varsayılan şema (Platin). Çıkışta anahtar KALIR.
    //   Beyaz liste dışı değer ne yazılır ne uygulanır.
    var SON_SEMA = 'mihenk-son-sema';

    /** Bu cihazda en son kullanılan şema (beyaz listede değilse ya da yoksa ''). */
    function sonSema() {
        var d = null;
        try { d = localStorage.getItem(SON_SEMA); } catch (e) { d = null; }   // özel sekme
        d = (d || '').toString().toLowerCase();
        return SEMALAR.indexOf(d) !== -1 ? d : '';
    }

    /** Renk şemasını uygular. Dönen değer GERÇEKTEN uygulanan şemadır. */
    function sema(deger) {
        var s = yaz('data-sema', deger, SEMALAR);
        try {
            if (s) localStorage.setItem(SON_SEMA, s); else localStorage.removeItem(SON_SEMA);
        } catch (e) { /* özel sekme */ }
        return s || 'altin';
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
        return { sema: sema(g.sema), olcek: olcek(g.yaziOlcegi) };
    }

    window.kuyumcuGorunum = { uygula: uygula, sema: sema, olcek: olcek, sonSema: sonSema };

    // İlk boyamadan önce (YILDIZ-AMBLEM-3): son kullanılan şema köke yazılır — oturumsuz /giris o şemayla
    // çizilir; oturum varsa sunucu yanıtı gelince kuyumcuGorunum.uygula aynı özniteliği yeniden yazar.
    yaz('data-sema', sonSema(), SEMALAR);
})();
