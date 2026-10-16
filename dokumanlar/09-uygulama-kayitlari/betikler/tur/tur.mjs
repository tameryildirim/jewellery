// Kurucu 25.09: "her yeri tekrar adam gibi istisnasız olarak gez".
// Kullanım: node tur.mjs <isci_no> <mod BASIT|PRO> <tema aydinlik|karanlik> <genislikler virgüllü> <rotalar virgüllü>
import { baslat } from './cdp.mjs';
import { readFileSync, mkdirSync, appendFileSync } from 'node:fs';
const [isci, MOD, TEMA, GEN, ROT] = process.argv.slice(2);
const OUT = (process.env.TUR_OUT || 'sonuc').startsWith('/') ? process.env.TUR_OUT : process.cwd() + '/' + (process.env.TUR_OUT || 'sonuc'); mkdirSync(OUT + '/foto', { recursive: true });
const JS = readFileSync('sikisma.js', 'utf8'), EK = readFileSync('ekstra.js', 'utf8');
const HEDEF = process.env.TUR_HEDEF || 'http://localhost:5182';
const BOY = { 1440: 900, 1280: 800, 1024: 768, 960: 820, 768: 1024, 375: 812 };
const bek = ms => new Promise(r => setTimeout(r, ms));
const yaz = o => appendFileSync(`${OUT}/olcum-${isci}.jsonl`, JSON.stringify(o) + '\n');
const b = await baslat(+(process.env.TUR_CDP || 9400 + +isci), (process.env.TUR_PROFIL || OUT) + '/profil-' + isci); // profil ASLA proje agacina yazilmaz
await b.boyut(1440, 900);
const kur = async () => { await b.ev(JS); await b.ev(EK); };
const girisli = () => b.ev(`!!document.querySelector('#ust-mod-basit') && typeof window.kuyumcuTema==='object'`);
let gir = false;
for (let d = 0; d < 6 && !gir; d++) { await b.git(HEDEF + '/__giris', 7000); gir = await girisli(); if (!gir) { console.log('giris-bekle', d); await bek(20000); } }
if (!gir) { console.log('GIRIS-BASARISIZ'); yaz({ tip: 'hata', hata: 'giris-basarisiz', MOD, TEMA }); b.kapat(); process.exit(3); }
await kur();
// mod: sunucuda kullanıcıya yazılır — düğmeyle seç ve doğrula
const modSec = async () => b.ev(`(async()=>{const hedef=${JSON.stringify(MOD === 'PRO' ? 'Profesyonel' : 'Basit')};const x=[...document.querySelectorAll('button')].find(b=>b.innerText.trim()===hedef);if(!x)return 'dugme-yok';x.click();await new Promise(r=>setTimeout(r,1500));return x.classList.contains('secili')||x.getAttribute('aria-pressed')==='true'||x.getAttribute('aria-checked')==='true'?'ok':'dogrulanamadi:'+x.outerHTML.slice(0,160)})()`);
let ms = ''; for (let d = 0; d < 4; d++) { ms = await modSec(); if (ms === 'ok') break; await bek(3000); }
console.log('mod', MOD, ms); if (ms !== 'ok') { yaz({ tip: 'hata', hata: 'mod-secilemedi:' + ms, MOD, TEMA }); b.kapat(); process.exit(4); }
await b.ev(`window.kuyumcuTema.uygula(${JSON.stringify(TEMA)}); 1`);
const yukBekle = async () => { for (let i = 0; i < 40; i++) { const y = await b.ev(`[...document.querySelectorAll('.donen-halka, .yukleniyor-kutu, [aria-busy=true]')].some(e => e.getBoundingClientRect().width > 0)`); if (!y) break; await bek(500); } await bek(400); };
const guvenliAd = s => s.replace(/[^a-z0-9]+/gi, '_').replace(/^_|_$/g, '') || 'kok';
for (const w of GEN.split(',').map(Number)) {
  await b.boyut(w, BOY[w]);
  for (const rota of ROT.split(',')) {
    const etiket = `${guvenliAd(rota)}__${MOD}_${TEMA}_${w}`;
    try {
      const ok = await b.ev(`typeof window.__ek==='function' && typeof Blazor!=='undefined'`);
      if (!ok) { await b.git(HEDEF + rota, 5000); await kur(); await b.ev(`window.kuyumcuTema.uygula(${JSON.stringify(TEMA)}); 1`); }
      else { await b.ev(`Blazor.navigateTo(${JSON.stringify(rota)}); 1`); await bek(1200); }
      await yukBekle();
      await b.ev('window.scrollTo(0,0); 1');
      const yol = await b.ev('location.pathname');
      const olc = await b.ev('({...window.__olc(), ...window.__ek()})');
      const modGercek = await b.ev(`document.querySelector('#ust-mod-profesyonel')?.getAttribute('aria-pressed')==='true'?'PRO':(document.querySelector('#ust-mod-basit')?.getAttribute('aria-pressed')==='true'?'BASIT':'?')`); const gecerli = modGercek === MOD && yol !== '/giris';
      await b.foto(`${OUT}/foto/${etiket}.png`, true);
      yaz({ tip: 'sayfa', rota, yol, w, MOD, modGercek, gecerli, TEMA, foto: `${etiket}.png`, ...olc });
      // sekmeler
      const sekmeler = await b.ev(`[...document.querySelectorAll('[role=tab]')].filter(e=>e.getBoundingClientRect().width>0).map((e,i)=>{e.setAttribute('data-tur-sekme',i);return (e.innerText||'').trim().slice(0,30)})`);
      for (let i = 1; i < sekmeler.length && i < 12; i++) {
        await b.ev(`document.querySelector('[data-tur-sekme="${i}"]')?.click(); 1`); await bek(700); await yukBekle();
        const so = await b.ev('({...window.__olc(), ...window.__ek()})');
        const f = `${etiket}__sekme${i}.png`; await b.foto(`${OUT}/foto/${f}`, true);
        yaz({ tip: 'sekme', rota, w, MOD, TEMA, sekme: sekmeler[i], foto: f, ...so });
      }
      if (sekmeler.length > 1) { await b.ev(`document.querySelector('[data-tur-sekme="0"]')?.click(); 1`); await bek(600); }
      // açılır katmanlar
      const acicilar = await b.ev('window.__acicilar()');
      for (const a of acicilar) {
        await b.ev('window.scrollTo(0,0); window.__katmanOnce()');
        const var_ = await b.ev(`(()=>{const e=document.querySelector('[data-tur-acici="${a.i}"]'); if(!e||e.getBoundingClientRect().width===0) return false; e.scrollIntoView({block:'center'}); window.__onceScroll=scrollY; e.click(); return true})()`);
        if (!var_) continue;
        await bek(700); await yukBekle();
        const k = await b.ev(`window.__katmanOlc(${a.i})`);
        let f = null;
        if (k.acildi) { f = `${etiket}__acici${a.i}.png`; await b.foto(`${OUT}/foto/${f}`); }
        const hata = await b.ev(`(()=>{const x=document.querySelector('#blazor-error-ui');return x&&getComputedStyle(x).display!=='none'})()`);
        yaz({ tip: 'katman', rota, w, MOD, TEMA, acici: a.ad, aciciMetin: a.metin, foto: f, hataBandi: hata, ...k });
        await b.ev('window.__kapatHepsi()');
        const yol2 = await b.ev('location.pathname');
        if (hata || yol2 !== yol) { await b.git(HEDEF + rota, 5000); await kur(); await b.ev(`window.kuyumcuTema.uygula(${JSON.stringify(TEMA)}); 1`); await b.ev('window.__acicilar()'); }
      }
    } catch (e) { yaz({ tip: 'hata', rota, w, MOD, TEMA, hata: String(e).slice(0, 300) }); try { await b.git(HEDEF + rota, 5000); await kur(); } catch {} }
  }
}
console.log('bitti', isci); b.kapat();
