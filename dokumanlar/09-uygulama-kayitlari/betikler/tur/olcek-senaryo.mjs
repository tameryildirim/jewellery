// PAKET OLCEK-1 v6 — DOLU FORM / KATMAN senaryolarında BASİT↔PRO ölçü farkı (tasarım hakemi 28.09).
// tutarlilik.mjs yalnız ekranın AÇILIŞ durumunu ölçer; bu betik aynı id'leri iş yapılmış
// durumda ölçer: /islemler ürün eklendi + ödeme bölündü (+ 7 haneli tutar), /cariler Düzenle
// çekmecesi açık, /emanet "Emanet Al" açık. Önbellek kapalı (eski CSS okunmaz).
// Kullanım: TUR_HEDEF=http://localhost:5187 TUR_CDP=9960 node olcek-senaryo.mjs <genislik> <cikti-dizini> [tema]
// Çıktı: <dizin>/senaryo-<g>-<tema>.json (ham) + fark satırları stdout'a; ekran görüntüleri <dizin>/<Mod>-<g>-<tema>-<durum>.png
import { baslat } from './cdp.mjs';
import { writeFileSync, mkdirSync } from 'node:fs';
const HEDEF = process.env.TUR_HEDEF || 'http://localhost:5187';
const W = +(process.argv[2] || 1440), OUT = process.argv[3] || '.', TEMA = process.argv[4] || 'aydinlik';
const H = W < 500 ? 812 : 900; mkdirSync(OUT, { recursive: true });
const bek = ms => new Promise(r => setTimeout(r, ms));
const OLC = `(() => {
  const vis = e => { const r = e.getBoundingClientRect(); const s = getComputedStyle(e); return r.width > 1 && r.height > 1 && s.visibility !== 'hidden' && s.display !== 'none'; };
  const ad = e => e.id ? '#' + e.id : e.tagName.toLowerCase() + '"' + (e.innerText || e.value || e.getAttribute('aria-label') || '').trim().replace(/\\s+/g, ' ').slice(0, 20) + '"';
  const kok = [...document.querySelectorAll('main, .cekmece, [role=dialog]')];
  const els = new Set(); kok.forEach(k => k.querySelectorAll('button, a.btn, input:not([type=hidden]):not([type=checkbox]):not([type=radio]), select, textarea, .secici-tetik, summary').forEach(e => els.add(e)));
  const o = {}; for (const e of els) { if (!vis(e)) continue; const s = getComputedStyle(e); const r = e.getBoundingClientRect();
    o[ad(e)] = { h: Math.round(r.height), w: Math.round(r.width), fs: s.fontSize, fw: s.fontWeight, pad: [s.paddingTop, s.paddingRight, s.paddingBottom, s.paddingLeft].join(' '), rad: s.borderTopLeftRadius,
      dugme: e.tagName === 'BUTTON' || e.tagName === 'SUMMARY' || e.classList.contains('btn') }; }
  const kesik = [...document.querySelectorAll('main input, .cekmece input')].filter(vis).filter(e => e.scrollWidth > e.clientWidth + 1).map(e => ad(e) + ' ' + e.clientWidth + '<' + e.scrollWidth + ' "' + e.value + '"');
  return { o, kesik, yatay: document.documentElement.scrollWidth > innerWidth + 1 }; })()`;
const sonuc = {};
const b = await baslat(+(process.env.TUR_CDP || 9960), OUT + '/profil-senaryo');
await b.cmd('Network.enable'); await b.cmd('Network.setCacheDisabled', { cacheDisabled: true }); await b.cmd('Network.setBypassServiceWorker', { bypass: true });
await b.boyut(W, H); await b.git(HEDEF + '/__giris', 7000);
const tik = async sel => { const r = await b.ev(`(()=>{const e=document.querySelector(${JSON.stringify(sel)}); if(!e) return null; e.scrollIntoView({block:'center'}); const q=e.getBoundingClientRect(); return {x:q.x+q.width/2,y:q.y+q.height/2}})()`); if (!r) return 'YOK ' + sel; await bek(300);
  for (const type of ['mouseMoved', 'mousePressed', 'mouseReleased']) await b.cmd('Input.dispatchMouseEvent', { type, x: r.x, y: r.y, button: 'left', clickCount: 1 }); return 'ok ' + sel; };
const yaz = async (sel, t) => { const v = await b.ev(`(()=>{const e=document.querySelector(${JSON.stringify(sel)}); if(!e) return 0; e.focus(); if(e.select) e.select(); return 1})()`); if (!v) return 'YOK ' + sel; await b.cmd('Input.insertText', { text: t }); return 'ok ' + sel; };
const git = async r => { await b.ev(`Blazor.navigateTo(${JSON.stringify(r)}); 1`); await bek(3500); };
for (const M of ['Basit', 'Profesyonel']) {
  await b.ev(`(async()=>{const x=[...document.querySelectorAll('button')].find(b=>b.innerText.trim()===${JSON.stringify(M)}); x&&x.click(); await new Promise(r=>setTimeout(r,1800)); return 1})()`);
  await b.ev(`window.kuyumcuTema.uygula(${JSON.stringify(TEMA)}); 1`);
  const kay = async (durum, adimlar) => { const o = await b.ev(OLC); await b.foto(`${OUT}/${M}-${W}-${TEMA}-${durum}.png`); (sonuc[durum] ??= {})[M] = { ...o, adimlar }; };
  await git('/islemler'); const a1 = [await yaz('#islem-barkod', 'Bilezik')]; await bek(2200);
  a1.push(await tik('#islem-barkod-oneri-0')); await bek(2200);
  a1.push(await tik(M === 'Basit' ? '#islem-odeme-bol' : '#islem-pro-karma')); await bek(1800);
  await b.ev('window.scrollTo(0,0);1'); await kay('islemler-fis-dolu', a1);
  const a2 = [await yaz('#islem-bacak-0-tutar', '1234567,89')]; await bek(800); await kay('islemler-tutar-7hane', a2);
  await git('/cariler'); const id = await b.ev(`(()=>{const e=document.querySelector('[id^=c-duzenle-]'); return e?e.id:''})()`);
  const a3 = [id ? await tik('#' + id) : 'YOK c-duzenle']; await bek(2000); await kay('cariler-cekmece', a3);
  await git('/emanet'); const a4 = [await tik('#e-giris-ac')]; await bek(2000); await kay('emanet-al-acik', a4);
}
b.kapat();
writeFileSync(`${OUT}/senaryo-${W}-${TEMA}.json`, JSON.stringify(sonuc, null, 1));
let fark = 0; const boylar = {};
for (const [d, m] of Object.entries(sonuc)) {
  const B = m.Basit?.o || {}, P = m.Profesyonel?.o || {};
  for (const [M, x] of Object.entries(m)) { for (const [k, v] of Object.entries(x.o)) if (v.dugme) (boylar[`${d} ${M}`] ??= new Set()).add(v.h);
    console.log(`${d}\t${M}\tadim: ${x.adimlar.join(' | ')}\tkesik: ${JSON.stringify(x.kesik)}\tyatay: ${x.yatay}`); }
  for (const k of Object.keys(B)) { if (!P[k]) continue; const a = B[k], c = P[k]; const f = [];
    for (const p of ['h', 'fs', 'fw', 'pad', 'rad']) if (String(a[p]) !== String(c[p]) && !(p === 'h' && Math.abs(a.h - c.h) <= 1)) f.push(`${p} ${a[p]}↔${c[p]}`);
    if (f.length) { fark++; console.log(`FARK\t${d}\t${k}\t${f.join(' · ')}`); } }
}
for (const [k, s] of Object.entries(boylar)) console.log(`BOYLAR\t${k}\t${[...s].sort((x, y) => x - y).join(',')}`);
console.log(`TOPLAM_FARK\t${fark}`);
