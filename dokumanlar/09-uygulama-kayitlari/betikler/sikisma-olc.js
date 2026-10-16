// ═══════════════════════════════════════════════════════════════════════════
// KURUCU ŞİKÂYET KATALOĞU — GERÇEK TARAYICI ÖLÇÜMÜ (orkestratör, 24.09.2026)
//
// Kurucu: "ekranlar hakkında sana ne şikâyet ettiysem tüm sayfalarda kontrol edilsin".
// Her madde kurucunun bir şikâyetinden doğdu; hepsi computed style / geometri ile
// ölçülür (bUnit ile ÖLÇÜLEMEZ). Kabul: her rota × genişlik × modda HER sayaç 0
// (dar sütun ve boy için eşik aşılırsa ölçülmüş gerekçe).
//
//  K1 gizliGorunen   [hidden] taşıyıp görünen öğe            (menü, 24.09 06:55)
//  K2 darSutun       en sağdaki kart ana alanın %70'inden az  (Ayarlar, 08:30)
//  K3 tasan / kirik  (SATIS-3 doğrulama 28.09: tablo HÜCRESİ td de sayılır — "22 Ayar Burma Bilezik" 3 satırdı, K3 td ölçmüyordu)
//                    kutudan taşan yazı · kısa etiketin birden çok satıra
//                    bölünmesi (Range.getClientRects satır sayımı)   (Ayarlar, 08:30)
//  K4 tren           sayfa boyu > 2 ekran ve ≥ 4 bölüm            (Ayarlar, 08:40)
//  K5 TRY            ekranda görünen HER "TRY" (₺ olmalı) + "TRY6,754.91" kültürsüz para
//                                                              (üst bar 08:30; kurucu 10:50 "TRY yerine ₺")
//  K6 seciciDar      açılan seçici listesi < 320 px ya da seçenek > 2 satır
//                                                              (Satış ürün seçici, 10:20)
//  K7 hizasiz        yan yana form alanlarında etiket üstü / kutu altı farkı > 3 px
//                                                              (Satış Elle ekle, 10:20)
//  K8 jargon         BASİT modda kullanıcıya görünen jargon (Cari, Storno, Vezne,
//                    Mutabakat, Pasife al, Denetim izi)          (Satış "Cariler", 10:20)
//  K9 binen          üst üste binen yazı                        (Panel altın kartı, 08:45)
//  K10 elipsis       seçili değeri üç noktayla KESİLEN seçici/kutu (S1-i doğrulama:
//                    375 px /islemler "22 Ayar Burma Bilezik (K22)" 199/81 px)
//  K11 hataBandi    Blazor 'Beklenmeyen bir hata oluştu' bandı görünür (menü araması, 14:40)
//  K3c kelimeKirik  TEK KELİMESİ birden çok satıra (harf harf) bölünen metin — öğenin çocuk sayısından
//                    bağımsız (YASAL-KAPI-2 10.10: 375 /islemler ALDIM "Kart" 12 px hücrede 4 satır; K3 kirik
//                    yalnız ≤1 çocuklu öğeyi saydığı için kaçırdı)
//  K10c kesikDeger   sayı içeren yazı kaydırma/kırpma kabının KENARINDA kesiliyor (kısmen görünür) ya da
//                    girdi kutusunun değeri kutuya sığmıyor (YASAL-KAPI-2: 1280 ALDIM "220.000 ₺" → "220.00")
//  K12 dugmeDikey   tek satırlık düğme yazısı kutunun dikey ortasında değil
//  K15 etkilesimGorunurluk  açık katmanda (çekmece/modal/details/liste) overflow:hidden atasınca kırpılan ya da
//                    kaydırılamaz biçimde ekran dışında kalan metin/düğme + ilk görünümde ekran dışı Kaydet (YILDIZ-KATMAN 11.10)
//                    (/evraklar süzgeç düğmeleri dibe yapışık, CUMA-J 25.09)
//  +  yatay          yatay kaydırma (375 px'te de 0)
//
// Kullanım (javascript_tool): bu dosyanın TAMAMINI çalıştır → "kuruldu".
//   window.__olcTam()                → o anki sayfa (async; seçicileri açıp kapatır)
//   window.__taraTam(['/','/islemler'])→ SPA içinde rota rota (Blazor.navigateTo)
// ═══════════════════════════════════════════════════════════════════════════
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
    // K3c — tek kelime dikey kırılması (metin düğümü bazında; çocuk sayısı sınırı yok)
    const kelimeKirik = [];
    { const w = document.createTreeWalker(main, NodeFilter.SHOW_TEXT); let n;
      while ((n = w.nextNode())) { const t = n.textContent.trim(); if (!t || /\s/.test(t) || t.length < 2 || t.length > 24) continue;
        const p = n.parentElement; if (!p || !vis(p)) continue; const g = document.createRange(); g.selectNodeContents(n);
        if (new Set([...g.getClientRects()].filter(q => q.width > 0.5).map(q => Math.round(q.top))).size > 1) kelimeKirik.push(p); } }
    // K10c — kenarda kesilen sayı (kısmen görünür) + değeri sığmayan girdi kutusu
    const kesikDeger = hepsi.filter(e => e.children.length === 0 && /\d/.test(e.innerText || '')).filter(e => {
      const r = e.getBoundingClientRect();
      for (let p = e.parentElement; p && p !== document.body; p = p.parentElement) { const cs = getComputedStyle(p);
        if (cs.overflowX !== 'visible') { const pr = p.getBoundingClientRect();
          return (r.left < pr.right - 1 && r.right > pr.right + 1) || (r.right > pr.left + 1 && r.left < pr.left - 1); } }
      return false; })
      .concat([...main.querySelectorAll('input:not([type=checkbox]):not([type=radio]):not([type=hidden])')].filter(vis)
        .filter(e => e.value && getComputedStyle(e).textOverflow !== 'ellipsis' && e.scrollWidth > e.clientWidth + 1));
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
    const jargonRe = /(?<![\p{L}])(cari\p{L}*|storno|vezne|mutabakat|pasife al|denetim izi)(?![\p{L}])|\b[a-z]+(?:-[a-z]+){1,}\b(?=[^\/.]|$)/giu;
    const jargon = basit ? (main.innerText.match(jargonRe) || []) : [];
    // K12 — düğme metni dikeyde ortada değil (CUMA-J 4. tur, doğrulayıcı ORTA bulgusu:
    // /evraklar süzgeç düğmelerinde yazı kutunun dibine yapışıktı; K1..K11 yakalamıyordu).
    // Tek satırlık düğme/bağlantı-düğme: yazının dikey merkezi kutunun merkezinden
    // max(4 px, yüksekliğin %15'i) fazla sapıyorsa sayılır.
    const dugmeDikey = [...main.querySelectorAll('button, a.dugme, a.btn, .dugme, .btn')].filter(vis).filter(e => {
      const t = (e.innerText || '').trim(); if (!t || t.includes('\n')) return false;
      const k = e.getBoundingClientRect(); if (k.height < 20) return false;
      const rs = []; const w = document.createTreeWalker(e, NodeFilter.SHOW_TEXT); let n;
      while ((n = w.nextNode())) { if (!n.textContent.trim()) continue; const g = document.createRange(); g.selectNodeContents(n); [...g.getClientRects()].filter(q => q.width > 1).forEach(q => rs.push(q)); }
      if (!rs.length) return false;
      const ust = Math.min(...rs.map(q => q.top)), alt = Math.max(...rs.map(q => q.bottom));
      if (alt - ust > k.height * 0.8) return false; // çok satır/ikon: ölçülmez
      return Math.abs((ust + alt) / 2 - (k.top + k.bottom) / 2) > Math.max(4, k.height * 0.15);
    });
    // K15 — ETKİLEŞİM SONRASI GÖRÜNÜRLÜK (YILDIZ-KATMAN · denetim 10.10 "bu turun dersi"): açık katmandaki
    // (çekmece, modal, açık details, liste kutusu) metin/düğme; en yakın overflow:hidden|clip atasınca kırpılıyorsa
    // ya da hiçbir kaydırma kabında değilken ekran dışında kalıyorsa sayılır. overflow:auto|scroll kabı içinde
    // kaydırarak ulaşılabilen öğe sayılmaz. Katmanın birincil düğmesi (Kaydet) ilk görünümde ekran dışındaysa da sayılır.
    const k15 = [];
    {
      const KAT = [...document.querySelectorAll('.cekmece, .modal-panel, [role=dialog][aria-modal=true], details[open], [role=listbox], :popover-open')].filter(vis);
      const H = innerHeight, W = innerWidth;
      for (const k of KAT) {
        for (const e of [...k.querySelectorAll('button, a, input, select, textarea, label, h2, h3, p, td, .secici-tetik')].filter(vis)) {
          if (!(e.innerText || e.value || e.placeholder || '').trim()) continue;
          const er = e.getBoundingClientRect(); let kirp = false, kayar = false;
          for (let p = e.parentElement; p && p !== document.body; p = p.parentElement) {
            const ps = getComputedStyle(p); const ov = ps.overflowY + ' ' + ps.overflowX;
            if (/auto|scroll/.test(ov)) { kayar = true; break; }
            if (/hidden|clip/.test(ov)) { const pr = p.getBoundingClientRect(); kirp = er.bottom > pr.bottom + 1 || er.top < pr.top - 1 || er.right > pr.right + 1; break; }
          }
          if (kirp || (!kayar && (er.bottom > H + 1 || er.right > W + 1))) k15.push(e);
        }
        if (k.matches('.cekmece, .modal-panel, [role=dialog]')) for (const bd of [...k.querySelectorAll('.dugme-birincil, .btn-birincil, .yz-b1, [id$="-kaydet"]')].filter(vis)) { const br = bd.getBoundingClientRect(); if (br.bottom > H + 1 || br.top < 0) k15.push(bd); }
      }
    }
    return {
      K15_etkilesimGorunurluk: k15.length,
      rota: location.pathname + location.search, genislik: innerWidth, mod: basit ? 'BASIT' : 'PRO',
      K1_gizliGorunen: [...document.querySelectorAll('[hidden]')].filter(e => getComputedStyle(e).display !== 'none').length,
      K2_kartSagYuzde: kartSag,
      K3_tasan: tasan.length, K3_kirik: kirik.length, K3c_kelimeKirik: kelimeKirik.length, K10c_kesikDeger: kesikDeger.length,
      K4_boyKat: +(Math.max(document.documentElement.scrollHeight, main.scrollHeight) / innerHeight).toFixed(1),
      K4_bolum: [...main.querySelectorAll('h2, details > summary')].filter(vis).length,
      K5_TRY: (document.body.innerText.match(/\bTRY\b/g) || []).length,  // kurucu 24.09: 'TRY yerine ₺ işareti' — ekranda görünen HER 'TRY' sayılır
      K7_hizasiz: hiza.length, K8_jargon: jargon.length, K9_binen: bin.length,
      K11_hataBandi: (() => { const b = document.querySelector('#blazor-error-ui'); return b && getComputedStyle(b).display !== 'none' ? 1 : 0; })(),  // kurucu 24.09 14:40: aramada 'Beklenmeyen bir hata oluştu'
      K10_elipsis: elipsis.length,
      K12_dugmeDikey: dugmeDikey.length,
      yatay: document.documentElement.scrollWidth - document.documentElement.clientWidth,
      ornek: { tasan: tasan.slice(0, 3).map(ad), kirik: kirik.slice(0, 4).map(ad), kelimeKirik: kelimeKirik.slice(0, 4).map(ad), kesikDeger: kesikDeger.slice(0, 4).map(ad), hiza: hiza.slice(0, 3), jargon: [...new Set(jargon)].slice(0, 5), binen: bin.slice(0, 2), elipsis: elipsis.slice(0, 3).map(ad), dugmeDikey: dugmeDikey.slice(0, 3).map(ad) }
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
  window.__ozetTam = a => a.map(x => x.hata ? `${x.rota} HATA ${x.hata}` : [x.rota, x.mod, x.genislik, 'K1:' + x.K1_gizliGorunen, 'K2:' + x.K2_kartSagYuzde, 'K3:' + x.K3_tasan + '/' + x.K3_kirik, 'K3c:' + x.K3c_kelimeKirik, 'K10c:' + x.K10c_kesikDeger, 'K4:' + x.K4_boyKat + 'x' + x.K4_bolum, 'K5:' + x.K5_TRY, 'K6:' + x.K6_seciciDar + '/' + x.K6_seciciOlculen, 'K7:' + x.K7_hizasiz, 'K8:' + x.K8_jargon, 'K9:' + x.K9_binen, 'K11:' + x.K11_hataBandi, 'K10:' + x.K10_elipsis, 'K12:' + x.K12_dugmeDikey, 'Y:' + x.yatay, JSON.stringify(x.ornek)].join(' | ')).join('\n');
  return 'kuruldu';
})()
