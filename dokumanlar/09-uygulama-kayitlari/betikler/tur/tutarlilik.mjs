// Kurucu 25.09 11:45: "ikisi de aynı ekran, biri basit biri profesyonel, Emanet Al butonunun boyutu değişiyor".
// Her rotada görünür düğme/kutu/başlık ölçüleri BASIT ve PRO'da karşılaştırılır + uygulama geneli düğme sınıfı tutarlılığı.
import { baslat } from './cdp.mjs';
import { writeFileSync } from 'node:fs';
const HEDEF = process.env.TUR_HEDEF || 'http://localhost:5182';
const R = process.argv[2].split(',');
const W = +(process.argv[3] || 1440), H = W >= 1280 ? 900 : 820;
// OLCEK-1 v5 (27.09): CDP portu ve profil ortamdan ayarlanabilir (şerit izolasyonu); varsayılan 9371.
const b = await baslat(+(process.env.TUR_CDP || 9371), process.env.TUR_PROFIL || (process.cwd() + '/profil-tut'));
await b.boyut(W, H);
await b.git(HEDEF + '/__giris', 7000);
await b.ev(`window.kuyumcuTema.uygula('aydinlik'); 1`);
const bek = ms => new Promise(r => setTimeout(r, ms));
const yuk = async () => { for (let i = 0; i < 40; i++) { if (!(await b.ev(`[...document.querySelectorAll('.donen-halka,.yukleniyor-kutu,[aria-busy=true]')].some(e=>e.getBoundingClientRect().width>0)`))) break; await bek(500); } await bek(500); };
const OLC = `(() => { const vis = e => { const r = e.getBoundingClientRect(); const s = getComputedStyle(e); return r.width > 0 && r.height > 0 && s.visibility !== 'hidden' && s.display !== 'none'; };
  const main = document.querySelector('main') || document.body; const o = {};
  [...main.querySelectorAll('button, a.btn, input:not([type=hidden]), select, .secici-tetik, h1, h2, h3, label, th, td, .rozet, [class*=rozet], [class*=-rozet]')].filter(vis).forEach((e, i) => {
    const t = (e.innerText || e.value || e.placeholder || '').trim().replace(/\\s+/g, ' ').slice(0, 30);
    const k = e.id ? '#' + e.id : e.tagName.toLowerCase() + ':' + t; if (o[k]) return;
    const r = e.getBoundingClientRect(), s = getComputedStyle(e);
    o[k] = { h: Math.round(r.height), w: Math.round(r.width), fs: s.fontSize, fw: s.fontWeight, pad: s.paddingTop + ' ' + s.paddingLeft, rad: s.borderRadius, bg: s.backgroundColor, sinif: String(e.className).slice(0, 50), metin: t };
  }); return o; })()`;
const mod = async m => { const x = await b.ev(`(async()=>{const x=[...document.querySelectorAll('button')].find(b=>b.innerText.trim()===${JSON.stringify(m)}); if(!x) return 'yok'; x.click(); await new Promise(r=>setTimeout(r,1800)); return x.getAttribute('aria-pressed')})()`); return x; };
const sonuc = {};
for (const M of ['Basit', 'Profesyonel']) {
  console.log('mod', M, await mod(M));
  for (const r of R) { await b.ev(`Blazor.navigateTo(${JSON.stringify(r)}); 1`); await bek(1200); await yuk(); (sonuc[r] ??= {})[M] = await b.ev(OLC); }
}
writeFileSync(`tutarlilik-${W}.json`, JSON.stringify(sonuc));
// karşılaştır
const fark = []; const dugme = [];
for (const r of R) { const A = sonuc[r].Basit || {}, P = sonuc[r].Profesyonel || {};
  for (const k of Object.keys(A)) { if (!P[k]) continue; const a = A[k], p = P[k]; const d = [];
    if (Math.abs(a.h - p.h) > 2) d.push(`boy ${a.h}→${p.h}`); if (a.fs !== p.fs) d.push(`yazı ${a.fs}→${p.fs}`); if (a.pad !== p.pad) d.push(`dolgu ${a.pad}→${p.pad}`); if (a.rad !== p.rad) d.push(`köşe ${a.rad}→${p.rad}`); if (a.fw !== p.fw) d.push(`kalınlık ${a.fw}→${p.fw}`);
    if (d.length) fark.push(`${r} ${k} (${a.metin}): ${d.join(', ')}`); }
  for (const M of ['Basit', 'Profesyonel']) for (const [k, v] of Object.entries(sonuc[r][M] || {})) if (/^#|^button|^a:/.test(k) && /btn|dugme/.test(v.sinif) || k.startsWith('button')) dugme.push({ r, M, k, ...v }); }
writeFileSync(`tutarlilik-fark-${W}.txt`, fark.join('\n'));
// uygulama geneli: aynı düğme sınıfının boy/yazı dağılımı
const grup = {}; dugme.forEach(d => { const s = (d.sinif.match(/btn-[a-z-]+|btn\b/g) || ['(sinifsiz)']).sort().join(' '); const key = `${d.M} | ${s}`; (grup[key] ??= {}); const v = `h${d.h} fs${d.fs}`; (grup[key][v] ??= []).push(`${d.r} ${d.metin}`); });
const g = Object.entries(grup).map(([k, v]) => `${k}: ` + Object.entries(v).sort((a, b) => b[1].length - a[1].length).map(([x, l]) => `${x} ×${l.length}${l.length <= 3 ? ' [' + l.join('; ') + ']' : ''}`).join(' · '));
writeFileSync(`tutarlilik-dugme-${W}.txt`, g.join('\n'));
console.log('mod farki', fark.length, 'dugme sinif grubu', g.length); b.kapat();
