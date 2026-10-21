// Tipografi envanteri: görünür metin taşıyan öğelerin hesaplanan boy/ağırlık dağılımı ve kaynak sınıfları. node tipo.mjs <vekil> <gen> <json-çıktı>
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
import { writeFileSync } from 'node:fs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, ''); const bek = ms => new Promise(r => setTimeout(r, ms));
const [V, W, CIKTI] = process.argv.slice(2);
const R = ['/', '/islemler', '/cariler', '/urunler', '/kasalar', '/kur', '/gider', '/raporlar', '/raporlar/islem-defteri', '/raporlar/kar-zarar', '/sayim', '/emanet', '/siparisler', '/bildirimler', '/evraklar', '/evraklar/takvim', '/banka', '/faturalar', '/irsaliyeler', '/satin-alma', '/hurda', '/uyum', '/ayarlar', '/isletme', '/kullanicilar', '/vitrin', '/kilavuz', '/ekranlar', '/gider-pusulalari', '/urunler/etiketler'];
const b = await baslat(9991, S + '/profil-sahip'); await b.git(V + '/__giris', 6000); await b.boyut(+W, 900);
const top = {};
for (const r of R) { await b.ev(`Blazor.navigateTo(${JSON.stringify(r)}); 1`); await bek(3000);
  const o = await b.ev(`(()=>{const o=[]; document.querySelectorAll('body *').forEach(e=>{ if(![...e.childNodes].some(n=>n.nodeType===3&&n.textContent.trim())) return; const r=e.getBoundingClientRect(); if(r.width<1||r.height<1) return; const s=getComputedStyle(e); if(s.visibility==='hidden') return; o.push([s.fontSize, s.fontWeight, (e.tagName.toLowerCase()+'.'+String(e.className).split(' ').filter(Boolean).slice(0,2).join('.')).slice(0,50)]) }); return o})()`);
  for (const [fs, fw, k] of o) { const a = top[fs] ??= { n: 0, agirlik: {}, sinif: {}, rota: {} }; a.n++; a.agirlik[fw] = (a.agirlik[fw] || 0) + 1; a.sinif[k] = (a.sinif[k] || 0) + 1; a.rota[r] = 1; } }
const ozet = Object.entries(top).sort((x, y) => parseFloat(x[0]) - parseFloat(y[0])).map(([fs, a]) => ({ fs, n: a.n, agirlik: a.agirlik, rotaSay: Object.keys(a.rota).length, sinif: Object.entries(a.sinif).sort((x, y) => y[1] - x[1]).slice(0, 6) }));
writeFileSync(CIKTI, JSON.stringify(ozet, null, 1));
for (const z of ozet) console.log(z.fs.padEnd(8), String(z.n).padStart(5), JSON.stringify(z.agirlik), z.sinif.slice(0, 3).map(s => s[0] + '×' + s[1]).join(' , '));
b.kapat();
