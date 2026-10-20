// Keşif: rotaya git, seçiciye tıkla, sonra görünür düğme/girdileri dök. Kullanım: node kesif-tik.mjs <hedef> <rota> <sel>
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, '');
const [HEDEF, rota, sel] = process.argv.slice(2); const bek = ms => new Promise(r => setTimeout(r, ms));
const b = await baslat(9975, S + '/profil-kesif'); await b.boyut(1440, 900);
await b.git(HEDEF + '/__giris', 6000); await b.ev(`Blazor.navigateTo(${JSON.stringify(rota)}); 1`); await bek(3000);
await b.ev(`document.querySelector(${JSON.stringify(sel)}).click(); 1`); await bek(1500);
const d = await b.ev(`(()=>{const g=e=>e.getBoundingClientRect().width>0; const t=e=>e.innerText.trim().replace(/\\s+/g,' ').slice(0,50);
  return {dugmeler: [...document.querySelectorAll('main button')].filter(g).map(e=>(e.id?'#'+e.id+' ':'')+t(e)).slice(0,30),
    girdiler: [...document.querySelectorAll('main input, main textarea')].filter(g).map(e=>(e.id?'#'+e.id:'(idsiz)')+'['+e.type+']'+(e.placeholder?' ph='+e.placeholder:'')).slice(0,20)}})()`);
console.log('DÜĞME: ' + d.dugmeler.join(' | ')); console.log('GİRDİ: ' + d.girdiler.join(' | '));
await b.foto(`${S}/kesif-tik.png`); b.kapat(); process.exit(0);
