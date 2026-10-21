// Keşif: rotaya git, sırayla seçicilere DOM click(), her adımdan sonra görünür düğme/girdi dök + ekran görüntüsü.
// Kullanım: node kesif-akis.mjs <hedef> <rota> <ad> <sel1> [sel2 ...]
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, '');
const [HEDEF, rota, ad, ...seciciler] = process.argv.slice(2); const bek = ms => new Promise(r => setTimeout(r, ms));
const b = await baslat(9976, S + '/profil-kesif'); await b.boyut(1440, 900);
await b.git(HEDEF + '/__giris', 6000); await b.ev(`Blazor.navigateTo(${JSON.stringify(rota)}); 1`); await bek(3500);
const dok = async (i) => { const d = await b.ev(`(()=>{const g=e=>e.getBoundingClientRect().width>0; const t=e=>e.innerText.trim().replace(/\\s+/g,' ').slice(0,45);
  return {yol: location.pathname, dugmeler: [...document.querySelectorAll('main button, main a.btn, main summary, [role=dialog] button, aside button')].filter(g).map(e=>(e.id?'#'+e.id+' ':'')+t(e)).filter(x=>x.trim()).slice(0,40),
    girdiler: [...document.querySelectorAll('main input, main textarea, main select, aside input, [role=dialog] input')].filter(g).map(e=>(e.id?'#'+e.id:'(idsiz)')+'['+e.type+']').slice(0,40)}})()`);
  console.log(`\n=== ${ad} adım ${i} · ${d.yol}`); console.log('DÜĞME: ' + d.dugmeler.join(' | ')); console.log('GİRDİ: ' + d.girdiler.join(' | ')); await b.foto(`${S}/kesif-${ad}-${i}.png`); };
await dok(0);
for (const [i, sel] of seciciler.entries()) { const ok = await b.ev(`(()=>{const e=document.querySelector(${JSON.stringify(sel)}); if(!e) return false; e.scrollIntoView({block:'center'}); e.click(); return true})()`); if (!ok) console.log('YOK:', sel); await bek(2200); await dok(i + 1); }
b.kapat(); process.exit(0);
