// ═══════════════════════════════════════════════════════════════════════════
// DİL UYGULAYICI — paket UX-2 Faz A1 (Mehmet K1)
//
// DilServisi.cs eskiden `js.InvokeVoidAsync("eval", "document.documentElement.lang = '…'")`
// çağırıyordu. CSP `script-src 'self' 'wasm-unsafe-eval'` JS eval()'ına İZİN
// VERMEZ (wasm-unsafe-eval yalnız WebAssembly derlemesini kapsar) → o satır
// tarayıcıda sessizce düşüyordu ve <html lang> hiç güncellenmiyordu.
// Tema için aynı deseni kopyalamak yerine eval tamamen kaldırıldı.
// ═══════════════════════════════════════════════════════════════════════════
(function () {
    'use strict';

    var GECERLI = ['tr', 'en'];   // beyaz liste

    /** <html lang> özniteliğini ayarlar. Beyaz liste dışı değer yok sayılır. */
    function uygula(dil) {
        if (GECERLI.indexOf(dil) === -1) return document.documentElement.lang;
        document.documentElement.lang = dil;
        return dil;
    }

    window.kuyumcuDil = { uygula: uygula };

    // PAKET DIL-EKRAN: kayıtlı dil sayfa AÇILIRKEN uygulanır. Eskiden <html lang> yalnız dil
    // DÜĞMESİNE basılınca değişiyordu; EN tercihli kullanıcıda lang="tr" kalıyor, CSS
    // `text-transform: uppercase` "Daily work" yazısını Türkçe kurallarla "DAİLY WORK" (noktalı İ)
    // basıyordu. Anahtar DilServisi.Anahtar ile AYNIDIR ("kuyumcu-dil"); depo kapalıysa sessizce geçilir.
    try {
        var kayitli = window.localStorage.getItem('kuyumcu-dil');
        if (kayitli) uygula(kayitli);
    } catch (e) { /* depo erişilemez: varsayılan lang="tr" kalır */ }
})();
