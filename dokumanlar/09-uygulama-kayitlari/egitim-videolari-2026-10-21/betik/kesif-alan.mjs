// Keşif: rotaya git, seçicilere tıkla, sonra verilen alan kimliklerinin var/görünür olup olmadığını ve konumunu yaz; son alana kaydırıp görüntü al.
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, '');
const [HEDEF, rota, ad, tiklar, alanlar] = process.argv.slice(2); const bek = ms => new Promise(r => setTimeout(r, ms));
const b = await baslat(9977, S + '/profil-kesif'); await b.boyut(1440, 900);
await b.git(HEDEF + '/__giris', 6000); await b.ev(`Blazor.navigateTo(${JSON.stringify(rota)}); 1`); await bek(3500);
for (const sel of tiklar.split(',').filter(Boolean)) { const ok = await b.ev(`(()=>{const e=document.querySelector(${JSON.stringify(sel)}); if(!e) return false; e.scrollIntoView({block:'center'}); e.click(); return true})()`); console.log('tık', sel, ok); await bek(2000); }
for (const id of alanlar.split(',')) {
  const d = await b.ev(`(()=>{const e=document.getElementById(${JSON.stringify(id)}); if(!e) return null; e.scrollIntoView({block:'center'}); const r=e.getBoundingClientRect(); const et=e.closest('.alan, .form-alan, label, div')?.querySelector('label, .alan-etiket')?.innerText; return {x:Math.round(r.x),y:Math.round(r.y),w:Math.round(r.width),deger:e.value ?? e.innerText, etiket:(et||'').trim().slice(0,50)}})()`);
  console.log(id, JSON.stringify(d)); await bek(300);
}
await b.foto(`${S}/kesif-${ad}.png`); b.kapat(); process.exit(0);
