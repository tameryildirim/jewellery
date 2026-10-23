// Görsel denetim 2 düzeltmeleri + gölge katmanı — tarayıcı doğrulaması. node dogrula-3.mjs <sahipVekil> <tezgahtarVekil>
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
import { writeFileSync, mkdirSync } from 'node:fs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, ''); mkdirSync(`${S}/foto/kanit`, { recursive: true });
const [SAHIP, TEZ] = process.argv.slice(2); const bek = ms => new Promise(r => setTimeout(r, ms));
const sonuc = []; const yaz = (id, gecti, olcum) => { sonuc.push({ id, gecti, olcum }); console.log((gecti ? 'GEÇTİ ' : 'KALDI ') + id + ' · ' + JSON.stringify(olcum)); };
const foto = async (b, ad) => b.foto(`${S}/foto/kanit/${ad}.png`);
let b = await baslat(9970, S + '/profil-sahip'); await b.git(SAHIP + '/__giris', 6000);
const tam = async (r, w, h, ms = 4500) => { await b.boyut(w, h); await b.git(SAHIP + r, ms); };
const tema = async t => { await b.ev(`window.kuyumcuTema.uygula(${JSON.stringify(t)}); 1`); await bek(300); };
// ── Gölge: açılır liste (açık + koyu)
for (const t of ['aydinlik', 'karanlik']) {
  await tam('/gider', 1280, 800); await tema(t);
  const o = await b.ev(`(async()=>{const tt=document.getElementById('gd-kasa'); tt.click(); await new Promise(r=>setTimeout(r,700)); const k=document.getElementById('gd-kasa-liste-kap'); const s=getComputedStyle(k); const zemin=getComputedStyle(document.body).backgroundColor; const op=k.querySelector('[role=option]'); const r=op.getBoundingClientRect(); return {golge:s.boxShadow!=='none', kenar:s.borderColor, bg:s.backgroundColor, zemin, farkli:s.backgroundColor!==zemin, op:[r.left+r.width/2, r.top+r.height/2]}})()`);
  await b.cmd('Input.dispatchMouseEvent', { type: 'mouseMoved', x: o.op[0], y: o.op[1] }); await bek(200);
  const h = await b.ev(`(()=>{const op=document.querySelector('#gd-kasa-liste-kap [role=option]:hover'); return op? getComputedStyle(op).backgroundColor : 'hover yok'})()`);
  yaz(`gölge liste ${t}`, o.golge && o.farkli && h !== 'hover yok' && h !== 'rgba(0, 0, 0, 0)', { ...o, hoverZemin: h }); await foto(b, `golge-liste-${t}`);
  await b.ev(`document.getElementById('gd-kasa').click(); 1`); await bek(300);
}
// ── Gölge: pencere + perde
await tam('/cariler', 1280, 800); await tema('aydinlik');
{ const o = await b.ev(`(async()=>{[...document.querySelectorAll('main button, main a')].find(e=>/^Yeni müşteri/.test(e.innerText.trim())).click(); await new Promise(r=>setTimeout(r,1200)); const a=document.querySelector('.modal-arka'); const p=document.querySelector('.modal-panel'); return {perde:a&&getComputedStyle(a).backgroundColor, golge:p&&getComputedStyle(p).boxShadow!=='none'}})()`); yaz('gölge pencere + perde', o.perde && o.perde !== 'rgba(0, 0, 0, 0)' && o.golge, o); await foto(b, 'golge-pencere'); }
// ── /kurulum liste görüş alanında
await tam('/kurulum', 1280, 900);
{ const o = await b.ev(`(async()=>{const d=[...document.querySelectorAll('details')].find(x=>/Açılış bakiyesi/.test(x.querySelector('summary')?.innerText||'')); if(d&&!d.open){d.querySelector('summary').click(); await new Promise(r=>setTimeout(r,800));} const t=document.getElementById('kur-acilis-tur'); t.scrollIntoView({block:'center'}); await new Promise(r=>setTimeout(r,300)); t.click(); await new Promise(r=>setTimeout(r,900)); const k=document.getElementById('kur-acilis-tur-liste-kap'); const r=k.getBoundingClientRect(); const u=document.elementFromPoint(r.left+r.width/2, Math.min(r.bottom-10, innerHeight-1)); return {ust:Math.round(r.top), alt:Math.round(r.bottom), vh:innerHeight, dibiGorunur:!!(u&&k.contains(u))}})()`); yaz('/kurulum açılır liste görüş alanında', o.alt <= o.vh && o.ust >= 0 && o.dibiGorunur, o); await foto(b, 'kurulum-liste'); }
// ── /islemler kısa ekran: yapışkan blok "+ İndirim"i örtmez
for (const [w, h] of [[1024, 768], [1280, 800]]) { await tam('/islemler', w, h);
  const o = await b.ev(`(()=>{const a=document.getElementById('yildiz-tezgah-alt').getBoundingClientRect(); const e=document.getElementById('yildiz-tezgah-indirim').getBoundingClientRect(); const u=document.elementFromPoint(e.left+e.width/2, e.top+e.height/2); return {altUst:Math.round(a.top), indirimAlt:Math.round(e.bottom), ustte:u&&u.id}})()`); yaz(`/islemler ${w}×${h} + İndirim örtülmez`, o.altUst >= o.indirimAlt && o.ustte === 'yildiz-tezgah-indirim', o); if (w === 1024) await foto(b, 'islemler-1024x768'); }
// ── 1024 özet ızgaraları
await tam('/hurda', 1024, 768, 7000); { const o = await b.ev(`(()=>{const ad=document.querySelector('[id^=yildiz-hurda-ad-]'); if(!ad) return {adSatir:-1, ekleBoy:-1, not:'satır yok'}; const lh=parseFloat(getComputedStyle(ad).lineHeight)||20; const ek=document.getElementById('yildiz-hurda-ayrinti'); return {adSatir:Math.round(ad.getBoundingClientRect().height/lh), ekleBoy:Math.round(ek.getBoundingClientRect().height)}})()`); yaz('/hurda 1024 ad ≤2 satır, ekle tek satır', o.adSatir <= 2 && o.ekleBoy <= 30, o); await foto(b, 'hurda-1024'); }
await tam('/kasalar', 1024, 768); { const o = await b.ev(`(()=>{const n=document.getElementById('yildiz-kasalar-ad-nakit'); const ek=document.getElementById('yildiz-kasalar-ayrinti'); return {tasan:n.scrollWidth>n.clientWidth+1, ekleBoy:Math.round(ek.getBoundingClientRect().height)}})()`); yaz('/kasalar 1024 ad taşmaz, ekle tek satır', !o.tasan && o.ekleBoy <= 30, o); }
// ── /gider seçici odağı (gerçek Tab) + CLS
await b.boyut(1280, 900); await b.cmd('Emulation.setFocusEmulationEnabled', { enabled: true }); await b.git(SAHIP + '/gider', 5000);
{ const tus = async () => { for (const t of ['keyDown', 'keyUp']) await b.cmd('Input.dispatchKeyEvent', { type: t, key: 'Tab', code: 'Tab', windowsVirtualKeyCode: 9 }); await bek(120); };
  await b.ev(`window.__Z=x=>{const s=getComputedStyle(x);return s.borderBottomColor+'|'+s.boxShadow}; window.__snap=e=>{const a=[];let x=e;for(let i=0;i<6&&x;i++){a.push(__Z(x));x=x.parentElement}return a.join('#')}; window.__o=null; 1`);
  const gorunmez = [], sira = [];
  for (let i = 0; i < 9; i++) { await tus(); const o = await b.ev(`(()=>{const e=document.activeElement; let f=null; if(window.__o){f={id:window.__o.id, fark:window.__o.s!==__snap(window.__o.e)}} if(!e||e===document.body){window.__o=null;return {son:true,f}} window.__o={e,id:e.id||e.className.toString().split(' ')[0],s:__snap(e)}; return {id:window.__o.id,f}})()`); if (o.f) { sira.push(o.f.id); if (!o.f.fark) gorunmez.push(o.f.id); } if (o.son) break; }
  yaz('/gider klavye odağı seçicilerde görünür', !gorunmez.some(x => x.startsWith('gd-')), { sira, gorunmez }); }
await b.cmd('Page.addScriptToEvaluateOnNewDocument', { source: `window.__cls=[];new PerformanceObserver(l=>{for(const e of l.getEntries()){if(!e.hadRecentInput)window.__cls.push(e.value)}}).observe({type:'layout-shift',buffered:true});` });
for (const [w, h] of [[375, 812], [1280, 800]]) { await tam('/gider', w, h, 6000); const c = await b.ev('window.__cls.reduce((a,b)=>a+b,0)'); yaz(`/gider ${w} CLS < 0,1`, c < 0.1, { cls: +c.toFixed(3) }); }
b.kapat();
// ── Tezgâhtar: 403 isteği 0
b = await baslat(9971, S + '/profil-tez'); await b.git(TEZ + '/__giris', 6000); await b.boyut(1280, 800); await b.cmd('Network.enable');
const R = ['/', '/islemler', '/cariler', '/kasalar', '/kur', '/banka', '/faturalar', '/vitrin', '/evraklar', '/kullanicilar', '/bildirimler', '/raporlar', '/gider', '/hurda', '/urunler'];
const red = {}; const bosalt = setInterval(() => { for (const d of b.olay.splice(0)) if (d.method === 'Network.responseReceived' && d.params.response.status === 403) { const u = d.params.response.url.replace(/^.*\/api\/v1/, '').split('?')[0]; red[u] = (red[u] || 0) + 1; } }, 50);
for (const r of R) { await b.git(TEZ + r, 5000); }
await bek(1000); clearInterval(bosalt);
yaz('tezgâhtar 15 ekranda 403 isteği', Object.keys(red).length === 0, red);
b.kapat();
writeFileSync(`${S}/dogrula-3.json`, JSON.stringify(sonuc, null, 1));
console.log(`TOPLAM ${sonuc.filter(x => x.gecti).length}/${sonuc.length}`);
