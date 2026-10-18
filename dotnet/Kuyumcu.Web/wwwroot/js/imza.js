// D1-13 (18.10) — ekranda imza: parmak / kalem / fareyle tuvale çizim (Pointer Events).
// Kendi kökümüzden, eval yok, satır içi yok. Çizim yalnız tuvalde durur; PNG'yi Blazor isteyince alır.
// Kullanım: kuyumcuImza.baslat(id) · kuyumcuImza.temizle(id) · kuyumcuImza.bosMu(id) · kuyumcuImza.png(id) → base64 (önek yok)
window.kuyumcuImza = (function () {
    const durum = {};

    function ayarla(t) {
        const oran = window.devicePixelRatio || 1;
        const k = t.getBoundingClientRect();
        t.width = Math.round(k.width * oran);
        t.height = Math.round(k.height * oran);
        const c = t.getContext('2d');
        c.setTransform(oran, 0, 0, oran, 0, 0);
        c.lineWidth = 2.2;
        c.lineCap = 'round';
        c.lineJoin = 'round';
        c.strokeStyle = '#111';
        return c;
    }

    function baslat(id) {
        const t = document.getElementById(id);
        if (!t) return false;
        const d = { c: ayarla(t), cizim: false, bos: true };
        durum[id] = d;
        const nokta = (e) => { const k = t.getBoundingClientRect(); return [e.clientX - k.left, e.clientY - k.top]; };
        t.addEventListener('pointerdown', (e) => {
            e.preventDefault(); t.setPointerCapture(e.pointerId);
            d.cizim = true; const [x, y] = nokta(e); d.c.beginPath(); d.c.moveTo(x, y);
        });
        t.addEventListener('pointermove', (e) => {
            if (!d.cizim) return; e.preventDefault();
            const [x, y] = nokta(e); d.c.lineTo(x, y); d.c.stroke(); d.bos = false;
        });
        const bitir = () => { d.cizim = false; };
        t.addEventListener('pointerup', bitir);
        t.addEventListener('pointercancel', bitir);
        return true;
    }

    function temizle(id) {
        const t = document.getElementById(id); const d = durum[id];
        if (!t || !d) return;
        d.c.clearRect(0, 0, t.width, t.height); d.bos = true;
    }

    function bosMu(id) { const d = durum[id]; return !d || d.bos; }

    function png(id) {
        const t = document.getElementById(id);
        if (!t || bosMu(id)) return null;
        // Beyaz zemin: kâğıda basılan imza saydam arka planla koyu temada kaybolmasın.
        const k = document.createElement('canvas'); k.width = t.width; k.height = t.height;
        const c = k.getContext('2d'); c.fillStyle = '#fff'; c.fillRect(0, 0, k.width, k.height); c.drawImage(t, 0, 0);
        return k.toDataURL('image/png').split(',')[1];
    }

    return { baslat, temizle, bosMu, png };
})();
