// Kurucu 25.09 12:20 (Faturalar): "yazının biri butonun üstünde diğeri altında, butonlar zeminle aynı renkte".
// Her rotada düğme/seçici/girdi: metin dikey ortalama sapması, düğme zemini/kenarlığı ile arka plan kontrastı, kırpılma.
import { baslat } from './cdp.mjs';
import { writeFileSync } from 'node:fs';
const HEDEF = process.env.TUR_HEDEF || 'http://localhost:5182';
const R = process.argv[2].split(','), MODLAR = (process.argv[3] || 'Basit,Profesyonel').split(','), TEMALAR = (process.argv[4] || 'aydinlik,karanlik').split(',');
const b = await baslat(9391, process.cwd() + '/profil-dugme');
await b.boyut(1440, 900);
await b.git(HEDEF + '/__giris', 7000);
const bek = ms => new Promise(r => setTimeout(r, ms));
const yuk = async () => { for (let i = 0; i < 40; i++) { if (!(await b.ev(`[...document.querySelectorAll('.donen-halka,.yukleniyor-kutu,[aria-busy=true]')].some(e=>e.getBoundingClientRect().width>0)`))) break; await bek(500); } await bek(500); };
const OLC = `(() => {
  const vis = e => { const r = e.getBoundingClientRect(); const s = getComputedStyle(e); return r.width > 0 && r.height > 0 && s.visibility !== 'hidden' && s.display !== 'none'; };
  const rgb = c => { const m = c.match(/[\\d.]+/g); return m ? m.map(Number) : [0,0,0,0]; };
  const L = ([r,g,bb]) => { const f = v => { v /= 255; return v <= .03928 ? v/12.92 : Math.pow((v+.055)/1.055, 2.4); }; return .2126*f(r)+.7152*f(g)+.0722*f(bb); };
  const kontrast = (a, c) => { const x = L(a), y = L(c); return (Math.max(x,y)+.05)/(Math.min(x,y)+.05); };
  const zemin = e => { for (let p = e.parentElement; p; p = p.parentElement) { const c = rgb(getComputedStyle(p).backgroundColor); if (c[3] === undefined || c[3] > .5) return c; } return [255,255,255]; };
  const ad = e => (e.id ? '#'+e.id : e.tagName.toLowerCase()+(typeof e.className==='string'&&e.className.trim()?'.'+e.className.trim().split(/\\s+/)[0]:''))+' "'+(e.innerText||e.value||'').trim().replace(/\\s+/g,' ').slice(0,26)+'"';
  const main = document.querySelector('main') || document.body; const W = innerWidth; const out = { ortasiz: [], silik: [], kirpik: [] };
  [...main.querySelectorAll('button, a.btn, select, .secici-tetik, input[type=button], input[type=submit]')].filter(vis).forEach(e => {
    const r = e.getBoundingClientRect(); const s = getComputedStyle(e);
    // metin kutusu: metin düğümlerinin satır dikdörtgenleri
    let top = 1e9, bot = -1e9; const w = document.createTreeWalker(e, NodeFilter.SHOW_TEXT); let n;
    while ((n = w.nextNode())) { if (!n.textContent.trim()) continue; const rg = document.createRange(); rg.selectNodeContents(n); for (const q of rg.getClientRects()) { if (q.width < 1) continue; top = Math.min(top, q.top); bot = Math.max(bot, q.bottom); } }
    if (e.tagName === 'SELECT') { top = null; }
    if (top !== null && top < 1e9) { const ic = r.top + parseFloat(s.borderTopWidth), dis = r.bottom - parseFloat(s.borderBottomWidth); const sap = ((top + bot) / 2) - ((ic + dis) / 2); if (Math.abs(sap) > 2.5 && r.height < 80) out.ortasiz.push(ad(e) + ' sapma=' + sap.toFixed(1) + 'px h=' + Math.round(r.height)); }
    if (e.tagName === 'SELECT' && r.height > parseFloat(s.fontSize) * 2.2 && s.appearance !== 'none' && s.webkitAppearance !== 'none') out.ortasiz.push(ad(e) + ' native-select h=' + Math.round(r.height));
    const bg = rgb(s.backgroundColor), z = zemin(e); const dolu = bg[3] === undefined || bg[3] > .5;
    const kb = parseFloat(s.borderTopWidth) > 0 ? rgb(s.borderTopColor) : null;
    const kz = dolu ? kontrast(bg, z) : 1; const kk = kb ? kontrast(kb, dolu ? bg : z) : 1; const kk2 = kb ? kontrast(kb, z) : 1;
    if (Math.max(kz, kk2) < 1.6 && !e.disabled) out.silik.push(ad(e) + ' zemin=' + kz.toFixed(2) + ' kenar=' + kk2.toFixed(2));
    if (r.right > W + 1 || r.left < -1) out.kirpik.push(ad(e) + ' sag=' + Math.round(r.right));
  });
  return out; })()`;
const sonuc = []; 
for (const M of MODLAR) {
  await b.ev(`(async()=>{const x=[...document.querySelectorAll('button')].find(b=>b.innerText.trim()===${JSON.stringify(M)}); if(x) x.click(); await new Promise(r=>setTimeout(r,1800)); return 1})()`);
  for (const T of TEMALAR) {
    for (const r of R) { await b.ev(`Blazor.navigateTo(${JSON.stringify(r)}); 1`); await bek(1200); await yuk(); await b.ev(`window.kuyumcuTema.uygula(${JSON.stringify(T)}); 1`); await bek(300); const o = await b.ev(OLC); sonuc.push({ r, M, T, ...o }); }
  }
}
writeFileSync('dugme-olc.json', JSON.stringify(sonuc));
const say = k => sonuc.reduce((t, x) => t + x[k].length, 0);
const satir = []; for (const x of sonuc) for (const k of ['ortasiz', 'silik', 'kirpik']) for (const v of x[k]) satir.push(`${x.r}\t${x.M}\t${x.T}\t${k}\t${v}`);
writeFileSync('dugme-olc.txt', satir.join('\n'));
console.log('ortasiz', say('ortasiz'), 'silik', say('silik'), 'kirpik', say('kirpik')); b.kapat();
