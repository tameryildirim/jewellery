// /islemler 375: barkod kutusuna yazıp arama sonuçlarını ölçer.
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, ''); const bek = ms => new Promise(r => setTimeout(r, ms));
const [V, W, METIN, FOTO] = process.argv.slice(2);
const b = await baslat(9984, S + '/profil-sahip'); await b.git(V + '/__giris', 6000); await b.boyut(+W, 812);
await b.ev(`Blazor.navigateTo('/islemler'); 1`); await bek(4000);
await b.ev(`(()=>{const i=document.querySelector('#islem-barkod'); i.focus(); i.value=${JSON.stringify(METIN)}; i.dispatchEvent(new Event('input',{bubbles:true})); return 1})()`); await bek(2000);
console.log(JSON.stringify(await b.ev(`[...document.querySelectorAll('.fis-arama-liste li, .fis-arama-liste [role=option]')].slice(0,5).map(li=>{const k=[...li.querySelectorAll('*')].filter(e=>e.children.length===0&&e.scrollWidth>e.clientWidth+1).map(e=>e.className+':'+e.textContent.trim().slice(0,40)); return {t:li.innerText.replace(/\\n/g,' | ').slice(0,90), kesik:k, html:li.innerHTML.slice(0,300)}})`), null, 1));
if (FOTO) await b.foto(FOTO);
b.kapat();
