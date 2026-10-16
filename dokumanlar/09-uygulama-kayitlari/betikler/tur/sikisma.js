(() => {
  const vis = e => { const r = e.getBoundingClientRect(); const s = getComputedStyle(e); return r.width > 0 && r.height > 0 && s.visibility !== 'hidden' && s.display !== 'none'; };
  const ad = e => (e.id ? '#' + e.id : e.tagName.toLowerCase() + (typeof e.className === 'string' && e.className.trim() ? '.' + e.className.trim().split(/\s+/)[0] : '')) + ' "' + (e.innerText || e.value || '').trim().replace(/\s+/g, ' ').slice(0, 26) + '"';
  // Satır sayımı YALNIZ metin düğümlerinden (ikon/svg satır sayılmaz — 24.09 yanlış alarm düzeltmesi)
  const satir = e => { const tops = new Set(); const w = document.createTreeWalker(e, NodeFilter.SHOW_TEXT); let n; while ((n = w.nextNode())) { if (!n.textContent.trim()) continue; const r = document.createRange(); r.selectNodeContents(n); [...r.getClientRects()].filter(q => q.width > 1).forEach(q => tops.add(Math.round(q.top / 4))); } return tops.size; };
  const bekle = ms => new Promise(z => setTimeout(z, ms));
  const anaAlan = () => document.querySelector('main, .icerik, #icerik, article') || document.body;

  window.__olc = () => {
    const main = anaAlan(); const mr = main.getBoundingClientRect();
    const hepsi = [...main.querySelectorAll('*')].filter(vis);
    // K3
    const tasan = hepsi.filter(e => { const s = getComputedStyle(e); return e.children.length === 0 && (e.innerText || '').trim() && e.scrollWidth > e.clientWidth + 2 && s.overflowX === 'visible' && e.clientWidth > 0; });
    const kirik = [...main.querySelectorAll('button, label, h1, h2, h3, h4, span, a, th, td, summary, li')].filter(vis).filter(e => { const t = (e.innerText || '').trim(); return t && t.length <= 24 && !t.includes('\n') && e.children.length <= 1 && satir(e) > 1; });
    // K9
    const yap = hepsi.filter(e => e.children.length === 0 && (e.innerText || '').trim()).slice(0, 700).map(e => [e, e.getBoundingClientRect()]);
    const bin = [];
    for (let i = 0; i < yap.length && bin.length < 6; i++) for (let j = i + 1; j < yap.length; j++) { const [a, r] = yap[i], [b, q] = yap[j]; if (a.contains(b) || b.contains(a)) continue; const ox = Math.min(r.right, q.right) - Math.max(r.left, q.left), oy = Math.min(r.bottom, q.bottom) - Math.max(r.top, q.top); if (ox > 4 && oy > 4) { bin.push(ad(a) + ' ⟂ ' + ad(b)); break; } }
    // K2
    const kartlar = hepsi.filter(e => { const s = getComputedStyle(e); return parseFloat(s.borderTopWidth) > 0 && parseFloat(s.borderRadius) > 0 && e.getBoundingClientRect().height > 120; });
    const kartSag = kartlar.length ? Math.round(100 * (Math.max(...kartlar.map(e => e.getBoundingClientRect().right)) - mr.left) / Math.max(1, mr.width)) : null;
    // K7 — yan yana form alanları: aynı görsel satırdaki kutuların alt kenarı ve etiketlerin üst kenarı
    const kontrol = c => c.matches('input:not([type=checkbox]):not([type=radio]), select, textarea, .secici-tetik, .btn, button') ? c : c.querySelector('input:not([type=checkbox]):not([type=radio]):not([type=hidden]), select, textarea, .secici-tetik');
    const hiza = [];
    hepsi.filter(e => { const s = getComputedStyle(e); return (s.display.includes('flex') && !s.flexDirection.startsWith('column')) || s.display.includes('grid'); }).forEach(sat => {
      const cocuk = [...sat.children].filter(vis).map(c => [c, kontrol(c)]).filter(([, k]) => k && vis(k));
      if (cocuk.length < 2 || cocuk.filter(([c, k]) => k.tagName !== 'BUTTON' && !k.classList.contains('btn')).length < 1) return;
      // S1-i kapanış: sıra YALNIZ ilk çocuğa göre kurulmaz (sarmalı satırda ikinci
      //   görsel sıra — fiyat · Ekle — ölçülmüyordu). Her çocuk çapa olur; en kalabalık sıra seçilir.
      const topOf = k => k.getBoundingClientRect().top;
      const siralar = cocuk.map(([, a]) => cocuk.filter(([, k]) => Math.abs(topOf(k) - topOf(a)) < 60)).filter(s => s.length >= 2);
      if (!siralar.length) return;
      const ayniSira = siralar.reduce((en, s) => { const f = x => Math.max(...x.map(([, k]) => k.getBoundingClientRect().bottom)) - Math.min(...x.map(([, k]) => k.getBoundingClientRect().bottom)); return f(s) > f(en) ? s : en; });
      const altlar = ayniSira.map(([, k]) => k.getBoundingClientRect().bottom);
      const etiketler = ayniSira.map(([c]) => c.querySelector('label, .alan-etiket')).filter(Boolean).filter(vis).map(l => l.getBoundingClientRect().top);
      const altFark = Math.max(...altlar) - Math.min(...altlar);
      const etiketFark = etiketler.length >= 2 ? Math.max(...etiketler) - Math.min(...etiketler) : 0;
      if (altFark > 3 || etiketFark > 3) hiza.push(ad(sat) + ` kutuAlt±${Math.round(altFark)} etiketUst±${Math.round(etiketFark)}`);
    });
    // K10 — seçili değer (yer tutucu değil) text-overflow:ellipsis ile kesiliyor mu
    const elipsis = [...main.querySelectorAll('.secici-metin:not(.yer-tutucu), .si-tetik')].filter(vis).filter(e => (e.innerText || '').trim() && getComputedStyle(e).textOverflow === 'ellipsis' && e.scrollWidth > e.clientWidth + 1);
    // K8 — yalnız BASİT modda sayılır (profesyonelde "Cari" kalabilir, D10)
    const basit = [...document.querySelectorAll('button')].some(b => b.innerText.trim() === 'Basit' && (b.classList.contains('secili') || b.getAttribute('aria-pressed') === 'true' || b.getAttribute('aria-checked') === 'true'));
    // QA 10 (24.09): büyük/küçük harf duyarsız ('STORNO:' kaçıyordu) + kebab-case teknik kod adları (ör. 'cari-hareketleri')
    const jargonRe = /(?<![\p{L}])(cari|cariler|cariden|cariye|carinin|storno|vezne|mutabakat|pasife al|denetim izi)(?![\p{L}])|\b[a-z]+(?:-[a-z]+){1,}\b(?=[^\/.]|$)/giu;
    const jargon = basit ? (main.innerText.match(jargonRe) || []) : [];
    return {
      rota: location.pathname + location.search, genislik: innerWidth, mod: basit ? 'BASIT' : 'PRO',
      K1_gizliGorunen: [...document.querySelectorAll('[hidden]')].filter(e => getComputedStyle(e).display !== 'none').length,
      K2_kartSagYuzde: kartSag,
      K3_tasan: tasan.length, K3_kirik: kirik.length,
      K4_boyKat: +(Math.max(document.documentElement.scrollHeight, main.scrollHeight) / innerHeight).toFixed(1),
      K4_bolum: [...main.querySelectorAll('h2, details > summary')].filter(vis).length,
      K5_TRY: (document.body.innerText.match(/\bTRY\b/g) || []).length,  // kurucu 24.09: 'TRY yerine ₺ işareti' — ekranda görünen HER 'TRY' sayılır
      K7_hizasiz: hiza.length, K8_jargon: jargon.length, K9_binen: bin.length,
      K11_hataBandi: (() => { const b = document.querySelector('#blazor-error-ui'); return b && getComputedStyle(b).display !== 'none' ? 1 : 0; })(),  // kurucu 24.09 14:40: aramada 'Beklenmeyen bir hata oluştu'
      K10_elipsis: elipsis.length,
      yatay: document.documentElement.scrollWidth - document.documentElement.clientWidth,
      ornek: { tasan: tasan.slice(0, 3).map(ad), kirik: kirik.slice(0, 4).map(ad), hiza: hiza.slice(0, 3), jargon: [...new Set(jargon)].slice(0, 5), binen: bin.slice(0, 2), elipsis: elipsis.slice(0, 3).map(ad) }
    };
  };

  // K6 — seçicileri tek tek aç, panel genişliği ve en uzun seçeneğin satır sayısı
  window.__seciciOlc = async () => {
    const sonuc = [];
    const tetikler = [...anaAlan().querySelectorAll('.secici-tetik')].filter(vis).slice(0, 12);
    for (const t of tetikler) {
      t.click(); await bekle(250);
      const kap = document.getElementById(t.id + '-liste-kap') || document.querySelector('.secici-liste-kap');
      if (kap && vis(kap)) {
        const w = Math.round(kap.getBoundingClientRect().width);
        const secenekler = [...kap.querySelectorAll('[role=option]')].filter(vis).slice(0, 30);
        const enCok = secenekler.length ? Math.max(...secenekler.map(o => satir(o.querySelector('span:not(.secici-isaret)') || o))) : 0;
        const sagTasma = Math.max(0, Math.round(kap.getBoundingClientRect().right - innerWidth));
        if (w < 320 || enCok > 2 || sagTasma > 0) sonuc.push(`#${t.id} genislik=${w} enCokSatir=${enCok} sagTasma=${sagTasma}`);
      }
      const perde = document.getElementById(t.id + '-perde'); if (perde) perde.click(); else document.body.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
      await bekle(150);
    }
    return { olculen: tetikler.length, dar: sonuc };
  };

  window.__olcTam = async () => { const x = window.__olc(); const s = await window.__seciciOlc(); x.K6_seciciOlculen = s.olculen; x.K6_seciciDar = s.dar.length; x.ornek.secici = s.dar.slice(0, 3); return x; };
  window.__taraTam = async (rotalar, bekleMs = 2600) => { const s = []; for (const r of rotalar) { Blazor.navigateTo(r); await bekle(bekleMs); try { s.push(await window.__olcTam()); } catch (e) { s.push({ rota: r, hata: String(e) }); } } return s; };
  window.__ozetTam = a => a.map(x => x.hata ? `${x.rota} HATA ${x.hata}` : [x.rota, x.mod, x.genislik, 'K1:' + x.K1_gizliGorunen, 'K2:' + x.K2_kartSagYuzde, 'K3:' + x.K3_tasan + '/' + x.K3_kirik, 'K4:' + x.K4_boyKat + 'x' + x.K4_bolum, 'K5:' + x.K5_TRY, 'K6:' + x.K6_seciciDar + '/' + x.K6_seciciOlculen, 'K7:' + x.K7_hizasiz, 'K8:' + x.K8_jargon, 'K9:' + x.K9_binen, 'K11:' + x.K11_hataBandi, 'K10:' + x.K10_elipsis, 'Y:' + x.yatay, JSON.stringify(x.ornek)].join(' | ')).join('\n');
  return 'kuruldu';
})()
