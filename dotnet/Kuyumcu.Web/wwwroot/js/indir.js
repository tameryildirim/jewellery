// Tarayıcıya dosya indirtme — Blazor WASM'dan bayt dizisi alır.
// Kullanım: window.kuyumcuIndir(dosyaAdi, base64Veri, mimeTuru)
window.kuyumcuIndir = function (dosyaAdi, base64Veri, mimeTuru) {
    const ikili = atob(base64Veri);
    const bayt = new Uint8Array(ikili.length);
    for (let i = 0; i < ikili.length; i++) bayt[i] = ikili.charCodeAt(i);

    const blob = new Blob([bayt], { type: mimeTuru || 'application/octet-stream' });
    const url = URL.createObjectURL(blob);
    const bag = document.createElement('a');
    bag.href = url;
    bag.download = dosyaAdi;
    document.body.appendChild(bag);
    bag.click();
    document.body.removeChild(bag);
    // Bellekte şifreli arşiv tutulmasın.
    setTimeout(() => URL.revokeObjectURL(url), 1000);
};

// Panoya kopyala — parola alanı için. Başarısızsa false döner (izin reddi vb.).
window.kuyumcuPanoyaKopyala = async function (metin) {
    try {
        await navigator.clipboard.writeText(metin);
        return true;
    } catch {
        return false;
    }
};
