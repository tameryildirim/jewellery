// PAKET BICIM-2 — SAYI/PARA BİÇİM DENETİMİ (tarayıcı içi ölçer; tur.mjs/ekstra.js'e DOKUNMAZ).
// Kurucu 25.09: "solda parasal format var, sağda inputta yok — tüm formatları gözden geçir".
// Kullanım: sayfaya enjekte et → window.__bicim() → { B_toplam, B_ortak, B_sayfa, ihlaller:[...] }
// Tek kaynak kurallar Kuyumcu.Web/Servisler/Bicim.cs:
//   para  : "50.000 ₺" / "63.363,16 ₺" — binlik NOKTA, ondalık VİRGÜL, ₺ SONDA
//   gram  : "12,500 g" (P-57: 3 hane, binlik YOK)   ·   has: "77,6014 has" (4 hane, binlik YOK)
//   yüzde : "%12,5"  ·  saat "09:02"  ·  tarih tr-TR "25.09.2026"
// İhlal türleri:
//   GRUPSUZ   ≥4 haneli tam kısım binliksiz ("50000", "63363,16")  — gram/has HARİÇ (P-57)
//   NOKTA_ONDALIK  İngilizce ondalık ("15.00", "41.95")
//   TL_ONDE   "₺1.234" (simge önde)       ·   TRY   "TRY" kalıbı sayı yanında
//   HAS_HANE  has değeri 4 hane değil     ·   GRAM_HANE  gram değeri 3 hane değil (gram KUTUSU: en az 3 hane)
// HARİÇ: tarih, saat, yıl (1900–2099 tek başına), telefon, TCKN/VKN, IBAN, barkod
// (≥9 hane tam sayı), fiş/belge no (harf/tire bitişik), sürüm numarası, URL.
// Kaynak sınıfı: "ortak" = Bilesenler/Form SayisalAlan ailesi (input.girdi.sayi in .alan-girdi-satir),
// "sayfa" = sayfa dosyasının kendi metni/ham <input>'u (sahibi sayfa şeridi).
(() => {
  const vis = e => { if (!e) return false; const r = e.getBoundingClientRect(); const s = getComputedStyle(e); return r.width > 0 && r.height > 0 && s.visibility !== 'hidden' && s.display !== 'none'; };
  const yol = e => { const p = []; for (let x = e; x && x !== document.body && p.length < 4; x = x.parentElement) { if (x.id) { p.unshift('#' + x.id); break; } p.unshift(x.tagName.toLowerCase() + (typeof x.className === 'string' && x.className.trim() ? '.' + x.className.trim().split(/\s+/)[0] : '')); } return p.join('>'); };
  const kimlikliAta = e => { for (let x = e; x && x !== document.body; x = x.parentElement) if (x.id) return x.id; return null; };
  const HARIC_KIMLIK = /(tel|telefon|iban|barkod|vergi|vkn|tckn|kimlik-no|seri|evrak-no|fis-no|belge-no|posta|sifre|yil|saat|surum|versiyon)/i;
  // Sayı belirteci: önünde/sonunda harf, tire, eğik çizgi, iki nokta, alt çizgi yok.
  const SAYI = /(^|[^\p{L}\p{N}_\-\/:.,#])([−-]?\d[\d.,]*\d|[−-]?\d)(?![\p{L}\p{N}_\-\/:])/gu;
  const ihlaller = [];
  const baglam = e => (e.value !== undefined && e.tagName === 'INPUT' ? '[value] ' + e.value : (e.innerText || e.textContent || '')).replace(/\s+/g, ' ').trim().slice(0, 90);
  const denetle = (metin, e, kaynak, girdi) => {
    const t = metin.replace(/ | /g, ' ');
    if (/https?:\/\//.test(t)) return;
    // TRY kalıbı
    if (/\bTRY\s?[−-]?\d|\d\s?TRY\b/.test(t)) ihlaller.push({ tur: 'TRY', ornek: t.trim().slice(0, 60), yer: yol(e), kimlik: kimlikliAta(e), kaynak, girdi, baglam: baglam(e) });
    if (/(^|[^\d\s,.])\s*₺\s?[−-]?\d/.test(t)) ihlaller.push({ tur: 'TL_ONDE', ornek: t.trim().slice(0, 60), yer: yol(e), kimlik: kimlikliAta(e), kaynak, girdi, baglam: baglam(e) });
    SAYI.lastIndex = 0; let m;
    while ((m = SAYI.exec(t))) {
      const tok = m[2].replace(/^[−-]/, ''); const son = t.slice(m.index + m[0].length, m.index + m[0].length + 5); const on = t.slice(Math.max(0, m.index + m[1].length - 4), m.index + m[1].length);
      if (/[A-Z]{2,}\s?$/.test(on)) continue;                  // kısaltma + sürüm ("WCAG 2.1")
      // tarih / saat / sürüm: iki ve daha çok nokta gruplu ama 3'lü değil
      if (/^\d{1,2}\.\d{1,2}\.\d{2,4}$/.test(tok) || /^\d{4}-\d{2}-\d{2}$/.test(tok)) continue;
      const gram = /^\s?g\b/.test(son) || (girdi === 'gram');
      const has = /^\s?has\b/i.test(son);
      if (has) { if (!/^\d+,\d{4}$/.test(tok)) ihlaller.push({ tur: 'HAS_HANE', ornek: tok + ' has', yer: yol(e), kimlik: kimlikliAta(e), kaynak, girdi, baglam: baglam(e) }); continue; }
      if (/^\d+\.\d{1,2}$/.test(tok) && !/^\d{1,2}\.\d{2}$/.test(tok) || /^\d+\.\d{4,}$/.test(tok)) { ihlaller.push({ tur: 'NOKTA_ONDALIK', ornek: tok, yer: yol(e), kimlik: kimlikliAta(e), kaynak, girdi, baglam: baglam(e) }); continue; }
      if (/^\d{1,2}\.\d{2}$/.test(tok) && /[₺$€£]|TL/.test(son)) { ihlaller.push({ tur: 'NOKTA_ONDALIK', ornek: tok, yer: yol(e), kimlik: kimlikliAta(e), kaynak, girdi, baglam: baglam(e) }); continue; }
      if (gram) { if (/\./.test(tok) || (girdi === 'gram' ? !/^\d+,\d{3,}$/.test(tok) : (/,/.test(tok) && !/^\d+,\d{3}$/.test(tok)))) ihlaller.push({ tur: 'GRAM_HANE', ornek: tok + ' g', yer: yol(e), kimlik: kimlikliAta(e), kaynak, girdi, baglam: baglam(e) }); continue; }
      const tam = tok.split(',')[0];
      if (/\./.test(tam)) continue;                               // gruplu (ya da tarih)
      if (tam.length < 4) continue;
      if (/,/.test(tok) === false) {
        if (tam.length >= 9) continue;                            // barkod / TCKN / telefon / IBAN parçası
        if (/^(19|20)\d\d$/.test(tam) && !/^\s?[₺$€£]/.test(son) && !/[₺$€£]\s?$/.test(on)) continue;   // yıl
        if (/^0\d{3,}$/.test(tam)) continue;                      // telefon parçası / sıfırla başlayan kod
      }
      ihlaller.push({ tur: 'GRUPSUZ', ornek: tok, yer: yol(e), kimlik: kimlikliAta(e), kaynak, girdi, baglam: baglam(e) });
    }
  };
  window.__bicim = () => {
    ihlaller.length = 0;
    const kok = document.querySelector('main, .icerik, #icerik, article') || document.body;
    const ust = document.querySelector('header, .ust-bar');
    // 1) Görünür metin düğümleri (girdiler hariç)
    for (const alan of [kok, ust].filter(Boolean)) {
      const w = document.createTreeWalker(alan, NodeFilter.SHOW_TEXT, { acceptNode: n => n.nodeValue.trim() && /\d/.test(n.nodeValue) ? NodeFilter.FILTER_ACCEPT : NodeFilter.FILTER_REJECT });
      let n; while ((n = w.nextNode())) {
        const e = n.parentElement; if (!e || !vis(e)) continue;
        if (e.closest('script, style, code, pre, kbd, [data-bicim-haric], input, textarea, select, option')) continue;
        const id = kimlikliAta(e) || ''; if (HARIC_KIMLIK.test(id) || HARIC_KIMLIK.test(e.className || '')) continue;
        // Metin, satırdaki komşularla birleşik okunmalı ("63363,16" + " ₺" ayrı düğüm olabilir)
        denetle(n.nodeValue + ' ' + (n.nextSibling?.textContent || e.nextSibling?.textContent || '').slice(0, 6), e, 'sayfa', null);
      }
    }
    // 2) Girdi değerleri (odak DIŞI) — boş olmayan, görünür metin/sayı kutuları
    for (const i of kok.querySelectorAll('input')) {
      if (!vis(i) || i === document.activeElement) continue;
      const tip = (i.type || 'text').toLowerCase();
      if (!['text', 'number', ''].includes(tip)) continue;
      const v = (i.value || '').trim(); if (!v || !/\d/.test(v)) continue;
      if (HARIC_KIMLIK.test(i.id || '') || HARIC_KIMLIK.test(i.name || '') || HARIC_KIMLIK.test(i.getAttribute('autocomplete') || '')) continue;
      if (i.getAttribute('inputmode') === 'tel' || i.getAttribute('inputmode') === 'numeric' && v.length >= 9) continue;
      const ortak = i.classList.contains('sayi') && !!i.closest('.alan-girdi-satir');
      const sonEk = i.parentElement?.querySelector('.alan-son-ek')?.textContent?.trim() || '';
      const girdi = ortak ? (sonEk === 'g' ? 'gram' : (/[₺$€£]|USD|EUR|GBP/.test(sonEk) ? 'para' : 'sayi')) : ('ham-' + tip);
      denetle(v + (girdi === 'gram' ? ' g' : ''), i, ortak ? 'ortak' : 'sayfa', girdi);
    }
    const tekil = []; const gor = new Set();
    for (const x of ihlaller) { const k = x.tur + '|' + x.ornek + '|' + x.yer; if (!gor.has(k)) { gor.add(k); tekil.push(x); } }
    return { B_toplam: tekil.length, B_ortak: tekil.filter(x => x.kaynak === 'ortak').length, B_sayfa: tekil.filter(x => x.kaynak === 'sayfa').length, ihlaller: tekil.slice(0, 60) };
  };
})();
