// Çekmece seçicilerinin yazı/satır ölçüsü. node cekmece.mjs <vekil> rota|açıcıMetni ...
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, ''); const bek = ms => new Promise(r => setTimeout(r, ms));
const [V, ...R] = process.argv.slice(2);
const b = await baslat(9987, S + '/profil-sahip'); await b.git(V + '/__giris', 6000); await b.boyut(1280, 900);
for (const x of R) { const [rota, acici] = x.split('|');
  await b.ev(`Blazor.navigateTo(${JSON.stringify(rota)}); 1`); await bek(3500);
  await b.ev(`(()=>{const d=[...document.querySelectorAll('button,a')].find(e=>e.innerText.trim().startsWith(${JSON.stringify(acici)})); d&&d.click(); return !!d})()`); await bek(1500);
  const o = await b.ev(`[...document.querySelectorAll('.cekmece .secici-tetik, .modal-panel .secici-tetik')].filter(e=>e.offsetParent).map(e=>{const m=e.querySelector('.secici-metin')||e; const s=getComputedStyle(m); return e.id+' '+s.fontSize+'/'+s.fontWeight+' h'+Math.round(e.getBoundingClientRect().height)})`);
  console.log(rota, acici, JSON.stringify(o));
  // aç ve ilk satırı ölç
  const l = await b.ev(`(async()=>{const t=document.querySelector('.cekmece .secici-tetik, .modal-panel .secici-tetik'); if(!t) return null; t.click(); await new Promise(r=>setTimeout(r,2500)); const li=[...document.querySelectorAll('.secici-liste li, [role=option], .secici-secenek')].filter(e=>e.offsetParent); return li.slice(0,2).map(e=>getComputedStyle(e).fontSize+' h'+Math.round(e.getBoundingClientRect().height)+' '+e.innerText.slice(0,20)+" "+e.className)})()`);
  console.log('  liste:', JSON.stringify(l)); }
b.kapat();
