// Keşif: vekil üzerinden giriş, her rotada görünür düğme/girdi metinlerini dök (kayıt planı için gerçek metinler).
// Kullanım: node kesif.mjs http://localhost:5181 /islemler /hurda ...
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, '');
const [HEDEF, ...rotalar] = process.argv.slice(2);
const bek = ms => new Promise(r => setTimeout(r, ms));
const b = await baslat(9971, S + '/profil-kesif'); await b.boyut(1440, 900);
await b.git(HEDEF + '/__giris', 6000);
for (const rota of rotalar) {
  await b.ev(`Blazor.navigateTo(${JSON.stringify(rota)}); 1`); await bek(3500);
  const d = await b.ev(`(()=>{const g=e=>e.getBoundingClientRect().width>0; const t=e=>e.innerText.trim().replace(/\\s+/g,' ').slice(0,60);
    return {yol: location.pathname, h1: document.querySelector('main h1, h1')?.innerText.trim(),
      dugmeler: [...document.querySelectorAll('main button, main a.btn, main summary')].filter(g).map(e=>(e.id?'#'+e.id+' ':'')+t(e)).filter(x=>x.trim()).slice(0,45),
      girdiler: [...document.querySelectorAll('main input, main textarea, main select')].filter(g).map(e=>(e.id?'#'+e.id:'')+'['+(e.type||e.tagName.toLowerCase())+']'+(e.placeholder?' ph='+e.placeholder:'')).slice(0,30)}})()`);
  console.log('\n=== ' + rota + ' → ' + d.yol + ' · ' + d.h1); console.log('DÜĞME: ' + d.dugmeler.join(' | ')); console.log('GİRDİ: ' + d.girdiler.join(' | '));
  await b.foto(`${S}/kesif-${rota.replace(/\W+/g, '_')}.png`);
}
b.kapat(); process.exit(0);
