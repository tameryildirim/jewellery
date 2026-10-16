// ═══════════════════════════════════════════════════════════════════════════
// P-37c — AÇILIR PANEL ÖRTÜŞME ÖLÇÜMÜ (tarayıcı konsolunda koşar)
//
// P-37 doğrulayıcısı, "kesişim 0" iddiasının ÖLÇÜLMEMİŞ olduğunu gösterdi ve
// şerit ikinci satıra sardığında 1280px'te 5056 px² örtüşme ÖLÇTÜ. Bu betik o
// sayıyı yeniden üretilebilir kılar: HER seçici tek tek açılır, açık panelin
// TÜM komşu kontrollerle kesişim alanı toplanır.
//
// ⚠ ESKİ BETİĞİN AÇIĞI (p37-hiza-olcum.js → p37Dropdown): yalnız İLK seçiciyi
//   açıyordu. Sarma örtüşmesi ise çoğu zaman İKİNCİ/ÜÇÜNCÜ seçicide doğar.
//   Burada hepsi taranır ve en kötü değer raporlanır.
//
// Hedef: kesisimToplam === 0 (1280 / 1024 / 900 / 375 px).
//
// Kullanım (konsol):
//   await p37cOlc()                → o anki genişlikte ölçüm
// ═══════════════════════════════════════════════════════════════════════════
window.p37cOlc = async function () {
  const bekle = (ms) => new Promise((r) => setTimeout(r, ms));

  const kesisim = (a, b) => {
    const g = Math.max(0, Math.min(a.right, b.right) - Math.max(a.left, b.left));
    const y = Math.max(0, Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top));
    return g * y;
  };

  const serit = document.querySelector('.sayfa-basi');
  const sonuc = {
    rota: location.pathname,
    genislik: window.innerWidth,
    seritVar: !!serit,
    tetikSayisi: 0,
    kesisimToplam: 0,
    enKotuTetik: null,
    enKotuAlan: 0,
    tasma: 0,
    detay: [],
  };
  if (!serit) return sonuc;

  const ana = document.querySelector('main.ana-icerik');
  sonuc.tasma = ana ? Math.max(0, ana.scrollWidth - ana.clientWidth) : 0;

  const tetikler = [...serit.querySelectorAll('.sayfa-basi-filtre .secici-tetik')]
    .filter((el) => el.offsetParent !== null && !el.disabled);
  sonuc.tetikSayisi = tetikler.length;

  for (const tetik of tetikler) {
    tetik.click();
    await bekle(250);

    const panel = serit.querySelector('.sayfa-basi-filtre .secici-liste-kap');
    if (!panel) {
      sonuc.detay.push({ tetik: tetik.id || '(kimliksiz)', panelYok: true });
      continue;
    }
    const p = panel.getBoundingClientRect();

    // Komşu = şeritteki DİĞER her görünür kontrol (süzgeç + eylem yuvası).
    const komsular = [...serit.querySelectorAll(
      '.sayfa-basi-filtre .girdi, .sayfa-basi-filtre .secici-tetik, ' +
      '.sayfa-basi-filtre input, .sayfa-basi-filtre select, ' +
      '.sayfa-basi-eylem button, .sayfa-basi-eylem a')]
      .filter((el) => el !== tetik && el.offsetParent !== null && !panel.contains(el));

    let alan = 0;
    const binenler = [];
    for (const el of komsular) {
      const a = kesisim(p, el.getBoundingClientRect());
      if (a > 0) binenler.push((el.id || el.tagName) + ':' + a.toFixed(1));
      alan += a;
    }

    sonuc.kesisimToplam += alan;
    if (alan > sonuc.enKotuAlan) {
      sonuc.enKotuAlan = alan;
      sonuc.enKotuTetik = tetik.id || '(kimliksiz)';
    }
    sonuc.detay.push({
      tetik: tetik.id || '(kimliksiz)',
      kesisim: +alan.toFixed(1),
      binenler,
      panelUst: +p.top.toFixed(1),
      panelAlt: +p.bottom.toFixed(1),
      tetikAlt: +tetik.getBoundingClientRect().bottom.toFixed(1),
    });

    // Kapat: perdeye tıkla (değeri DEĞİŞTİRMEDEN kapanır).
    const perde = document.querySelector('.secici-perde');
    if (perde) perde.click(); else tetik.click();
    await bekle(150);
  }

  sonuc.kesisimToplam = +sonuc.kesisimToplam.toFixed(1);
  sonuc.enKotuAlan = +sonuc.enKotuAlan.toFixed(1);
  return sonuc;
};
