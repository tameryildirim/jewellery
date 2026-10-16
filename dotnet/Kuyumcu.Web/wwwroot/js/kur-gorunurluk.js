// ═══════════════════════════════════════════════════════════════════════════
// KUR YOKLAMASI — SEKME GÖRÜNÜRLÜĞÜ (PAKET P-70)
//
// CanliKurServisi sekme gizliyken /kur/guncel yoklamasını duraklatır. Tarayıcı
// "visibilitychange" olayını C#'a iletmek için ince köprü.
//
// ES MODÜLÜ olarak `import("./js/kur-gorunurluk.js")` ile yüklenir; index.html'e
// <script> EKLENMEZ (denetim.sh "index.html <script> sayısı" sabiti değişmez).
// Kendi kökümüzden gelir, eval kullanmaz, CSP'de yeni istisna GEREKTİRMEZ.
// ═══════════════════════════════════════════════════════════════════════════

let dinleyici = null;

/** Görünürlük değişince C# tarafındaki GorunurlukDegisti(bool) çağrılır. */
export function izle(dotnetRef) {
    birak();
    dinleyici = () => {
        dotnetRef.invokeMethodAsync('GorunurlukDegisti', !document.hidden);
    };
    document.addEventListener('visibilitychange', dinleyici);
    // Açılışta sekme zaten gizliyse (arka planda açılan sekme) hemen bildir.
    if (document.hidden) {
        dinleyici();
    }
}

/** Aboneliği bırakır (servis kapanırken). */
export function birak() {
    if (dinleyici) {
        document.removeEventListener('visibilitychange', dinleyici);
        dinleyici = null;
    }
}
