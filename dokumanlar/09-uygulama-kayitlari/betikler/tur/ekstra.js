(() => {
  const vis = e => { if (!e) return false; const r = e.getBoundingClientRect(); const s = getComputedStyle(e); return r.width > 0 && r.height > 0 && s.visibility !== 'hidden' && s.display !== 'none' && +s.opacity !== 0; };
  const ad = e => (e.id ? '#' + e.id : e.tagName.toLowerCase() + (typeof e.className === 'string' && e.className.trim() ? '.' + e.className.trim().split(/\s+/)[0] : '')) + ' "' + (e.innerText || e.value || e.getAttribute('aria-label') || '').trim().replace(/\s+/g, ' ').slice(0, 30) + '"';
  const bekle = ms => new Promise(z => setTimeout(z, ms));
  const anaAlan = () => document.querySelector('main, .icerik, #icerik, article') || document.body;
  // Ek ölçüler: K12 ezilmiş kutu · K13 kırpılan/taşan içerik · K10b kesilen değer (genel) · K14 yazı boyu tutarsızlığı
  window.__ek = () => {
    const main = anaAlan(); const W = innerWidth;
    const kutular = [...main.querySelectorAll('input:not([type=checkbox]):not([type=radio]):not([type=hidden]), select, textarea, .secici-tetik')].filter(vis);
    const ezik = kutular.filter(e => e.getBoundingClientRect().width < (W < 500 ? 60 : 90)).map(e => ad(e) + ' w=' + Math.round(e.getBoundingClientRect().width));
    const kaydirmaIci = e => { for (let p = e.parentElement; p && p !== document.body; p = p.parentElement) { const ox = getComputedStyle(p).overflowX; if ((ox === 'auto' || ox === 'scroll') && p.getBoundingClientRect().right <= W + 1) return true; } return false; };
    const kirp = [...main.querySelectorAll('*')].filter(vis).filter(e => { const r = e.getBoundingClientRect(); return r.right > W + 1 && r.width > 40 && r.left < W && !kaydirmaIci(e); }).filter((e, i, a) => !a.some(p => p !== e && p.contains(e))).map(e => ad(e) + ' sag=' + Math.round(e.getBoundingClientRect().right));
    const kes = [...main.querySelectorAll('input, select, .secici-tetik *, button, td, th, span, a, label')].filter(vis).filter(e => getComputedStyle(e).textOverflow === 'ellipsis' && e.scrollWidth > e.clientWidth + 1 && (e.innerText || e.value || '').trim()).map(ad);
    const etiketler = [...main.querySelectorAll('label, .alan-etiket')].filter(vis);
    const etiketBin = []; for (let i = 0; i < etiketler.length; i++) for (let j = i + 1; j < etiketler.length; j++) { const a = etiketler[i].getBoundingClientRect(), b = etiketler[j].getBoundingClientRect(); if (Math.min(a.right, b.right) - Math.max(a.left, b.left) > 2 && Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top) > 2 && !etiketler[i].contains(etiketler[j]) && !etiketler[j].contains(etiketler[i])) etiketBin.push(ad(etiketler[i]) + ' ⟂ ' + ad(etiketler[j])); }
    return { K12_ezik: ezik.length, K13_kirp: kirp.length, K10b_kesik: kes.length, K9b_etiketBin: etiketBin.length, ek: { ezik: ezik.slice(0, 4), kirp: kirp.slice(0, 4), kes: kes.slice(0, 4), etiketBin: etiketBin.slice(0, 3) } };
  };
  // Açıcılar: yalnız katman açan GÜVENLİ öğeler (kayıt/silme/gönderme ASLA)
  const YASAK = /kaydet|sil\b|silin|onayla|gönder|kes\b|fatura kes|öde\b|ödeme yap|tahsil et|yazdır|çıkış|oturumu kapat|pasife|aktifleştir|geri al|uygula|içe aktar|yedek al|geri yükle|devret|iptal et|vezneye|mutabakat|kapat\b|kilitle|sıfırla|onay|evet|başlat|bitir|tamamla|indir|dışa aktar|çalıştır/i;
  const GUVENLI = /^(\+\s*)?(yeni|diğer|daha fazla|düzenle|filtre|ayrıntı|detay|ekle|.*\bekle$|seçenekler|işlemler|göster|aç|tümü|menü|profil|bildirim)/i;
  window.__acicilar = () => {
    const main = document.body;
    const hepsi = [...main.querySelectorAll('[aria-haspopup], [popovertarget], [aria-expanded], summary, .secici-tetik, button, a[role=button]')].filter(vis);
    const sec = []; const gor = new Set();
    for (const e of hepsi) {
      const t = (e.innerText || e.getAttribute('aria-label') || e.title || '').trim().replace(/\s+/g, ' ');
      if (YASAK.test(t)) continue;
      if (e.closest('nav, aside, .kenar-cubugu') && !/ara|arama/i.test(t)) continue;
      if (e.type === 'submit' || e.closest('form') && e.matches('button:not([type=button])')) continue;
      const katmanli = e.matches('[aria-haspopup], [popovertarget], [aria-expanded], summary, .secici-tetik');
      if (!katmanli && !GUVENLI.test(t)) continue;
      const k = (e.id || '') + '|' + t.slice(0, 40); if (gor.has(k)) continue; gor.add(k);
      e.setAttribute('data-tur-acici', String(sec.length)); sec.push({ i: sec.length, ad: ad(e), metin: t.slice(0, 40) });
    }
    return sec.slice(0, 40);
  };
  const katmanlar = () => [...document.querySelectorAll(':popover-open, [role=dialog], [role=listbox], [role=menu], .cekmece, .secici-liste-kap, details[open] > :not(summary), .modal, dialog[open]')].filter(vis);
  window.__katmanOnce = () => { window.__onceki = new Set(katmanlar()); window.__onceScroll = scrollY; return window.__onceki.size; };
  window.__katmanOlc = (i) => {
    const acici = document.querySelector(`[data-tur-acici="${i}"]`);
    const yeni = katmanlar().filter(k => !window.__onceki.has(k));
    if (!yeni.length) return { acildi: false, sayfaKaydi: Math.round(scrollY - window.__onceScroll) };
    const k = yeni.sort((a, b) => b.getBoundingClientRect().width * b.getBoundingClientRect().height - a.getBoundingClientRect().width * a.getBoundingClientRect().height)[0];
    const r = k.getBoundingClientRect(); const ar = acici && vis(acici) ? acici.getBoundingClientRect() : null;
    const s = getComputedStyle(k);
    const tamEkran = r.width >= innerWidth * 0.9 && r.height >= innerHeight * 0.9;
    const kenarCekmece = (Math.abs(r.right - innerWidth) < 2 || r.left < 2) && r.height >= innerHeight * 0.9;
    const modal = !!document.querySelector('.perde, .modal-perde, ::backdrop') || k.matches('dialog[open], [aria-modal=true]');
    const bagli = ar ? (Math.abs(r.top - ar.bottom) < 24 || Math.abs(r.bottom - ar.top) < 24) && (r.left < ar.right + 8 && r.right > ar.left - 8) : false;
    const ogeler = [...k.querySelectorAll('button, [role=option], [role=menuitem], a, li, label, input')].filter(vis);
    const boylar = [...new Set(ogeler.map(o => getComputedStyle(o).fontSize))];
    const aileler = [...new Set(ogeler.map(o => getComputedStyle(o).fontFamily.split(',')[0].trim()))];
    const icTasan = [...k.querySelectorAll('*')].filter(vis).filter(e => e.children.length === 0 && (e.innerText || '').trim() && e.scrollWidth > e.clientWidth + 2 && getComputedStyle(e).overflowX === 'visible').length;
    return { acildi: true, katman: ad(k), sinif: k.className && String(k.className).slice(0, 60), rol: k.getAttribute('role'), kutu: [Math.round(r.left), Math.round(r.top), Math.round(r.width), Math.round(r.height)],
      ekranDisi: r.right > innerWidth + 1 || r.left < -1 || (r.bottom > innerHeight + 1 && s.overflowY !== 'auto' && s.overflowY !== 'scroll' && !tamEkran), bagli, tamEkran, kenarCekmece, modal,
      tur: tamEkran ? 'tam-ekran' : kenarCekmece ? 'cekmece' : bagli ? 'bagli-acilir' : 'SERBEST-YUZEN',
      sayfaKaydi: Math.round(scrollY - window.__onceScroll), yaziBoylari: boylar, aileler, govdeBoyu: getComputedStyle(document.body).fontSize, icTasan, ogeSayisi: ogeler.length,
      kose: s.borderRadius, golge: s.boxShadow !== 'none', zemin: s.backgroundColor };
  };
  window.__kapatHepsi = async () => { document.querySelectorAll(':popover-open').forEach(p => { try { p.hidePopover(); } catch {} }); document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true })); document.activeElement?.dispatchEvent?.(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true })); await bekle(200); const p = document.querySelector('.perde, .cekmece-perde, .modal-perde, [id$=-perde]'); if (p && vis(p)) p.click(); document.querySelectorAll('details[open]').forEach(d => d.open = false); await bekle(250); };
  return 'ek-kuruldu';
})()
