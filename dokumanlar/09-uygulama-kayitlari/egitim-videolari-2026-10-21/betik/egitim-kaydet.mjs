// Y-01 eğitim videosu kaydı: başsız Chrome + CDP screencast, vekil üzerinden parolasız giriş, Yıldız kabuğu (Blazor.navigateTo).
// Kullanım: node egitim-kaydet.mjs <plan-id> [--prova]   (ortam: HEDEF=http://localhost:5181)
// Çıktı: kayit-<id>/kayit.json + kareler/ (video-yap.py ile aynı şema). Adım süresi = klip net süresi + 1,3 sn.
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'node:fs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, '');
const ID = process.argv[2]; const PROVA = process.argv.includes('--prova');
const HEDEF = process.env.HEDEF || 'http://localhost:5181';
const plan = JSON.parse(readFileSync(`${S}/plan-${ID}.json`, 'utf8'));
const KLIP = `/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/demo-kayit-2026-10-01-uzun/seslendirme/klipler-egitim-${ID}.json`;
const CIKTI = `${S}/${PROVA ? 'prova' : 'kayit'}-${ID}`; mkdirSync(CIKTI + '/kareler', { recursive: true });
const bek = ms => new Promise(r => setTimeout(r, ms));
const sureler = {}; let gercekSure = false;
if (existsSync(KLIP)) { const k = JSON.parse(readFileSync(KLIP, 'utf8')); if (k.length === plan.adimlar.length) { for (const x of k) sureler[x.id] = x.net; gercekSure = true; } }
for (const a of plan.adimlar) if (!(a.id in sureler)) sureler[a.id] = a.cumleler.reduce((t, c) => t + c[1].split(/\s+/).length, 0) / 2.1 + 0.32 * (a.cumleler.length - 1);
if (!PROVA && !gercekSure && !process.env.ZORLA) { console.log('DUR: klip süreleri yok (klipler-egitim-' + ID + '.json); önce seslendir.sh'); process.exit(2); }
const KAPLAMA = readFileSync(S + '/kaplama.js', 'utf8'); const H = PROVA ? 0.3 : 1;
const b = await baslat(9962 + (PROVA ? 10 : 0), S + '/profil-kayit'); await b.boyut(1440, 900);
const EK_CSS = `(()=>{const s=document.createElement('style');s.id='kk-ek';s.textContent='#kk-alt{top:0!important;bottom:auto!important;left:0!important;right:0!important;border-radius:0!important;border-left:none!important;border-bottom:5px solid #f2c230!important;padding:9px 22px 10px!important} #kk-alt .b{font-size:20px!important} #kk-alt .s{font-size:15px!important} #kk-etiket{top:82px!important;bottom:auto!important}';document.documentElement.appendChild(s)})()`;
const KAPLAMA2 = KAPLAMA + ';' + EK_CSS;
await b.cmd('Page.addScriptToEvaluateOnNewDocument', { source: `window.open = () => null; window.print = () => null; document.addEventListener('DOMContentLoaded', () => { ${KAPLAMA2} });` });
await b.cmd('Network.enable'); const sorunlar = []; const kareler = []; let kayitta = false;
const bosalt = setInterval(() => {
  for (const d of b.olay.splice(0, b.olay.length)) {
    if (d.method === 'Page.screencastFrame') {
      b.cmd('Page.screencastFrameAck', { sessionId: d.params.sessionId }).catch(() => {});
      if (!kayitta) continue;
      const ad = String(kareler.length).padStart(6, '0') + '.jpg'; writeFileSync(`${CIKTI}/kareler/${ad}`, Buffer.from(d.params.data, 'base64'));
      kareler.push({ ad, ts: d.params.metadata.timestamp, w: d.params.metadata.deviceWidth, h: d.params.metadata.deviceHeight });
    } else if (d.method === 'Network.responseReceived' && d.params.response.status >= 400 && !/__giris|favicon/.test(d.params.response.url)) {
      const sorun = { tip: 'http', durum: d.params.response.status, url: d.params.response.url.slice(0, 120), govde: '' }; sorunlar.push(sorun);
      setTimeout(() => b.cmd('Network.getResponseBody', { requestId: d.params.requestId }).then(r => { sorun.govde = String(r.body).slice(0, 300); }).catch(() => {}), 300);
    }
  }
}, 40);
const simdi = () => Date.now() / 1000;
const kap = (fn, ...a) => b.ev(`window.__kap && window.__kap.${fn}(${a.map(x => JSON.stringify(x)).join(',')}); 1`).catch(() => {});
const imlecGit = async (x, y, ms = 420) => { await kap('imlec', x, y); b.cmd('Input.dispatchMouseEvent', { type: 'mouseMoved', x, y }).catch(() => {}); await bek(ms * H); };
const kutu = sel => b.ev(`(()=>{const e=document.querySelector(${JSON.stringify(sel)}); if(!e) return null; const r=e.getBoundingClientRect(); if(r.width===0) return null; return {x:Math.round(r.left+r.width/2),y:Math.round(r.top+r.height/2),ust:r.top,alt:r.bottom}})()`);
const tiklaSel = async (sel) => {
  let k = await kutu(sel); for (let i = 0; i < 12 && !k; i++) { await bek(250); k = await kutu(sel); }   // Blazor çizimi için en çok 3 sn bekle
  if (!k) { sorunlar.push({ tip: 'bulunamadi', sel }); console.log('  BULUNAMADI', sel); return false; }
  if (k.ust < 60 || k.alt > 860) { await b.ev(`document.querySelector(${JSON.stringify(sel)}).scrollIntoView({block:'center'}); 1`); await bek(400); k = await kutu(sel); }
  await imlecGit(k.x, k.y); await kap('tik', k.x, k.y);
  // Girdi: CDP fare tıklaması (odak + imleç). Düğme/bağlantı: yalnız DOM click() — CDP tıklaması Blazor düğmelerinde
  // düşebiliyor (satış kaydı prova'da yazılmadı); ikisini birden yapmak açılır listeleri iki kez tetikleyip kapatıyordu.
  const girdi = await b.ev(`(()=>{const e=document.querySelector(${JSON.stringify(sel)}); return !!e && (e instanceof HTMLInputElement || e instanceof HTMLTextAreaElement)})()`);
  if (girdi) { await b.cmd('Input.dispatchMouseEvent', { type: 'mousePressed', x: k.x, y: k.y, button: 'left', buttons: 1, clickCount: 1 }); await b.cmd('Input.dispatchMouseEvent', { type: 'mouseReleased', x: k.x, y: k.y, button: 'left', buttons: 0, clickCount: 1 }); }
  else await b.ev(`(()=>{const e=document.querySelector(${JSON.stringify(sel)}); if(e.focus) e.focus(); e.click(); return 1})()`).catch(() => {});
  await bek(300 * H); return true;
};
const yaz = async (metin, ms = 110) => { for (const c of metin) { await b.cmd('Input.insertText', { text: c }); await bek(ms * H); } };
const tus = async (key) => { const kod = { Enter: 13, Escape: 27, Tab: 9 }[key] || 0; await b.cmd('Input.dispatchKeyEvent', { type: 'rawKeyDown', key, code: key, windowsVirtualKeyCode: kod }); if (key === 'Enter') await b.cmd('Input.dispatchKeyEvent', { type: 'char', text: '\r', key, code: key, windowsVirtualKeyCode: kod }); await b.cmd('Input.dispatchKeyEvent', { type: 'keyUp', key, code: key, windowsVirtualKeyCode: kod }); };
const yukBekle = async () => { for (let i = 0; i < 40; i++) { const y = await b.ev(`[...document.querySelectorAll('.donen-halka, .yukleniyor-kutu, [aria-busy=true]')].some(e=>e.getBoundingClientRect().width>0)`).catch(() => false); if (!y) return; await bek(150); } };
const git = async (rota) => { await b.ev(`Blazor.navigateTo(${JSON.stringify(rota)}); 1`); await bek(900 * H); await yukBekle(); await bek(600 * H); await b.ev('window.scrollTo(0,0); 1'); };
const sec = async (sel) => { await tiklaSel(sel); await b.ev(`(()=>{const e=document.querySelector(${JSON.stringify(sel)}); if(e&&e.select) e.select(); return 1})()`); };
const kopyala = async (kaynak, hedef) => { const m = (await b.ev(`document.querySelector(${JSON.stringify(kaynak)})?.textContent || ''`)).match(/[\d.]+,\d{2}/); if (!m) { sorunlar.push({ tip: 'kopyala', kaynak }); return; } await sec(hedef); await yaz(m[0].replace(/\./g, '')); };
const EYLEM = {
  git: e => git(e.rota),
  tik: e => tiklaSel(e.sel),
  tikVarsa: async e => { if (await kutu(e.sel)) await tiklaSel(e.sel); },
  sec: e => sec(e.sel),
  kopyala: e => kopyala(e.kaynak, e.hedef),
  yaz: e => yaz(e.metin),
  tus: e => tus(e.key),
  etiket: e => kap('etiket', e.metin),
  bekle: e => bek(e.ms * H),
  imlec: e => imlecGit(e.x, e.y, 700),
  imlecSel: async e => { const k = await kutu(e.sel); if (k) { if (k.ust < 60 || k.alt > 860) { await b.ev(`document.querySelector(${JSON.stringify(e.sel)}).scrollIntoView({block:'center'}); 1`); await bek(400); } const k2 = await kutu(e.sel); await imlecGit(k2.x, k2.y, 700); } else sorunlar.push({ tip: 'imlec-bulunamadi', sel: e.sel }); },
  kaydir: e => b.ev(`window.scrollTo({top:${e.top},behavior:'smooth'}); 1`),
  kart: e => kap('kart', e.ust || '', e.b || '', e.s || ''),
  imlecMetin: async e => { const sel = await b.ev(`(()=>{const adaylar=[...document.querySelectorAll('main *')].filter(x=>x.children.length===0 && x.getBoundingClientRect().width>0 && x.textContent.includes(${JSON.stringify(e.metin)})); const k=adaylar[0]; if(!k) return null; if(!k.id) k.id='kk-gecici-'+Math.random().toString(36).slice(2,8); return '#'+k.id})()`); if (!sel) { sorunlar.push({ tip: 'metin-bulunamadi', metin: e.metin }); console.log('  METİN YOK', e.metin); return; } await b.ev(`document.querySelector(${JSON.stringify(sel)}).scrollIntoView({block:'center',behavior:'smooth'}); 1`); await bek(700); const k = await kutu(sel); if (k) await imlecGit(k.x, k.y, 700); },
  kartKapat: () => kap('kart', '', '', ''),
  tikMetin: async e => { const sel = await b.ev(`(()=>{const k=[...document.querySelectorAll(${JSON.stringify(e.kap || 'button')})].find(x=>x.getBoundingClientRect().width>0 && x.innerText.trim().replace(/\\s+/g,' ')===${JSON.stringify(e.metin)}); if(!k) return null; if(!k.id) k.id='kk-gecici-'+Math.random().toString(36).slice(2,8); return '#'+k.id})()`); if (!sel) { sorunlar.push({ tip: 'metin-bulunamadi', metin: e.metin }); console.log('  METİN YOK', e.metin); return; } await tiklaSel(sel); },
  yazSel: async e => { await tiklaSel(e.sel); await b.ev(`(()=>{const x=document.querySelector(${JSON.stringify(e.sel)}); if(x&&x.select) x.select(); return 1})()`); await yaz(e.metin); },
};
// ---- hazırlık: vekil girişi (localStorage jetonları), açık tema, Profesyonel mod
await b.git(HEDEF + '/__giris', 6000); await b.ev(KAPLAMA2);
if (process.env.KAP_ONEK !== undefined) await b.cmd('Page.addScriptToEvaluateOnNewDocument', { source: `window.__kapOnek = ${JSON.stringify(process.env.KAP_ONEK)};` });
if (process.env.KAP_ONEK !== undefined) await b.ev(`window.__kapOnek = ${JSON.stringify(process.env.KAP_ONEK)}; 1`);
for (let i = 0; i < 40 && !(await b.ev(`!!document.querySelector('#yildiz-ust')`)); i++) await bek(300);
console.log('hazırlık: kabuk =', await b.ev(`!!document.querySelector('#yildiz-ust')`), '· yol', await b.ev('location.pathname'));
await kap('kart', plan.acilisUst || 'MİHENK · EĞİTİM', plan.baslik, plan.alt);
kayitta = true; if (!PROVA) { await b.cmd('Page.startScreencast', { format: 'jpeg', quality: 88, everyNthFrame: 1 }); await bek(500); }
const T0 = simdi(); const adimlar = []; const bolumler = [{ grup: 'Açılış', bas: 0 }];
await bek(2600 * H); await kap('kart', '', '', '');
for (const [i, a] of plan.adimlar.entries()) {
  const sure = sureler[a.id] * H; const t0 = simdi(); bolumler.push({ grup: a.baslik, bas: +(t0 - T0).toFixed(2) });
  await kap('altyazi', `${plan.baslik} · ${a.sira}/${a.toplam} — ${a.baslik}`, a.sorun);
  let hata = '';
  for (const e of a.eylemler) { try { await EYLEM[e.t](e); } catch (x) { hata += String(x).slice(0, 120) + ' '; sorunlar.push({ tip: 'adim', id: a.id, eylem: e.t, hata: String(x).slice(0, 160) }); } }
  if (!(await b.ev('!!window.__kap'))) { await b.ev(KAPLAMA2); await kap('altyazi', `${plan.baslik} · ${a.sira}/${a.toplam} — ${a.baslik}`, a.sorun); }
  const zE = simdi() - t0; const kalan = (sure + 1.3 * H) - zE; if (kalan > 0) await bek(kalan * 1000);
  const bant = await b.ev(`(()=>{const x=document.querySelector('#blazor-error-ui');return !!x&&getComputedStyle(x).display!=='none'})()`); if (bant) sorunlar.push({ tip: 'hata-bandi', id: a.id });
  // Çevrimdışı bandı (PWA): OS ağ olayından gelir; kayıtta görünürse kayıt geçersiz sayılır (uydurma bastırma YOK, yeniden çekilir).
  if (await b.ev(`!!document.querySelector('#pwa-cevrimdisi')`)) sorunlar.push({ tip: 'cevrimdisi-bandi', id: a.id });
  const yer = await b.ev('location.pathname');
  if (PROVA) await b.foto(`${CIKTI}/${String(i).padStart(2, '0')}-${a.id}.png`);
  adimlar.push({ id: a.id, grup: plan.baslik, sira: a.sira, toplam: a.toplam, baslik: a.baslik, bas: +(t0 - T0).toFixed(2), son: +(simdi() - T0).toFixed(2), klip: +sureler[a.id].toFixed(2), yer, hata });
  console.log(String(i).padStart(2, '0'), a.id.padEnd(10), 'bas', (t0 - T0).toFixed(1).padStart(6), 'süre', (simdi() - t0).toFixed(1).padStart(5), 'klip', sureler[a.id].toFixed(1).padStart(5), yer, hata);
  await kap('etiket', '');
}
await kap('altyazi', '', ''); await kap('kart', plan.acilisUst || 'MİHENK · EĞİTİM', plan.baslik, plan.kapanisAlt || 'Bitti · Kılavuzdan yeniden izleyebilirsiniz'); await bek(3000 * H); await kap('kart', '', '', '');
const SON = simdi() - T0; if (!PROVA) await b.cmd('Page.stopScreencast'); await bek(400); kayitta = false; clearInterval(bosalt);
writeFileSync(`${CIKTI}/kayit.json`, JSON.stringify({ T0, sure: +SON.toFixed(2), gercekSure, adimlar, bolumler, kareler, sorunlar }, null, 1));
console.log('BİTTİ', ID, 'süre', SON.toFixed(1), 'sn · kare', kareler.length, '· sorun', sorunlar.length); for (const s of sorunlar) console.log('  SORUN', JSON.stringify(s));
b.kapat(); process.exit(0);
