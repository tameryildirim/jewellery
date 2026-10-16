// ═══════════════════════════════════════════════════════════════════════════
// P-37 — ARAÇ ŞERİDİ HİZA ÖLÇÜMÜ (tarayıcı konsolunda koşar)
// Kurucu 21 Eylül 16:35: "sağda maden yazan yer ile yanında olması gereken şey
// aynı hizada mı?" — bu betik o soruyu SAYIYLA yanıtlar. "Düzelttim" beyanı
// ölçümsüz kabul EDİLMEZ (Anayasa md.1).
//
// Ölçülenler (her serbest ekran için):
//   1) araç şeridindeki kontrollerin üst kenar farkı (px)   → hedef ≤ 2
//   2) kontrollerin yükseklik farkı (px)                     → hedef ≤ 2
//   3) kesik metin sayısı (scrollWidth > clientWidth)        → hedef 0
//   4) açık dropdown panelinin komşu kontrollerle kesişimi   → hedef 0
//   5) main.ana-icerik yatay taşması (scrollWidth-clientWidth)→ hedef 0
// ═══════════════════════════════════════════════════════════════════════════
window.p37Olc = function () {
  const serit = document.querySelector('.sayfa-basi');
  const sonuc = { rota: location.pathname, seritVar: !!serit };

  const ana = document.querySelector('main.ana-icerik');
  sonuc.tasma = ana ? Math.max(0, ana.scrollWidth - ana.clientWidth) : null;

  if (serit) {
    // Şeritteki KONTROLLER: girdi, seçici tetiği ve eylem düğmeleri.
    const kontroller = [...serit.querySelectorAll(
      '.sayfa-basi-filtre .girdi, .sayfa-basi-filtre .secici-tetik, ' +
      '.sayfa-basi-filtre input, .sayfa-basi-eylem button, .sayfa-basi-eylem a')]
      .filter(el => el.offsetParent !== null);
    const r = kontroller.map(el => ({ t: el.getBoundingClientRect().top,
                                      h: el.getBoundingClientRect().height,
                                      id: el.id || el.tagName }));
    sonuc.kontrolSayisi = r.length;
    // ⚠ SATIR SATIR ölçülür: şerit dar ekranda satır kaydırabilir; iki ayrı
    //   satırdaki kontrolün üst kenarı doğal olarak farklıdır, bu KUSUR DEĞİL.
    //   Kusur, AYNI satırdaki kontrollerin kaymasıdır (kurucunun gördüğü şey).
    const satirlar = {};
    r.forEach(x => { const k = Math.round(x.t / 12); (satirlar[k] = satirlar[k] || []).push(x); });
    let ustFark = 0, yukFark = 0;
    Object.values(satirlar).forEach(g => {
      const t = g.map(x => x.t), h = g.map(x => x.h);
      ustFark = Math.max(ustFark, Math.max(...t) - Math.min(...t));
      yukFark = Math.max(yukFark, Math.max(...h) - Math.min(...h));
    });
    sonuc.satirSayisi = Object.keys(satirlar).length;
    sonuc.ustFark = +ustFark.toFixed(2);
    sonuc.yukseklikFark = +yukFark.toFixed(2);
    sonuc.detay = r.map(x => x.id + ':' + x.t.toFixed(0) + '/' + x.h.toFixed(0));

    // Kesik metin: GÖRÜNÜR etiketler + düğme metinleri (1px alt-piksel toleransı).
    // ⚠ `.gorsel-gizli` etiketler HARİÇ: onlar ekran okuyucu içindir ve
    //   tasarım gereği 1px'e kırpılır — kesik saymak YANLIŞ ölçüm olurdu.
    const metinler = [...serit.querySelectorAll(
      '.alan-etiket, .sayfa-basi-filtre label, .sayfa-basi-eylem button, .sayfa-basi-eylem a')]
      .filter(el => !el.classList.contains('gorsel-gizli'));
    sonuc.kesik = metinler.filter(el => el.scrollWidth - el.clientWidth > 1)
                          .map(el => el.textContent.trim().slice(0, 30));
    sonuc.kesikSayisi = sonuc.kesik.length;
  }
  return sonuc;
};

// Açık dropdown panelinin KOMŞU KONTROLLERLE kesişim alanı.
window.p37Dropdown = function () {
  const tetik = document.querySelector('.sayfa-basi-filtre .secici-tetik');
  if (!tetik) return { yok: true };
  tetik.click();
  return new Promise(r => setTimeout(() => {
    const panel = document.querySelector('.sayfa-basi-filtre .secici-liste-kap');
    if (!panel) { r({ panelYok: true }); return; }
    const p = panel.getBoundingClientRect();
    const komsular = [...document.querySelectorAll(
      '.sayfa-basi-filtre .girdi, .sayfa-basi-filtre .secici-tetik, .sayfa-basi-eylem button')]
      .filter(el => el !== tetik && el.offsetParent !== null);
    let alan = 0;
    for (const el of komsular) {
      const k = el.getBoundingClientRect();
      const w = Math.max(0, Math.min(p.right, k.right) - Math.max(p.left, k.left));
      const h = Math.max(0, Math.min(p.bottom, k.bottom) - Math.max(p.top, k.top));
      alan += w * h;
    }
    r({ kesisimAlani: +alan.toFixed(1), panelUst: +p.top.toFixed(1), tetikAlt: +tetik.getBoundingClientRect().bottom.toFixed(1) });
  }, 250));
};
