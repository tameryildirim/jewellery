// Kaydırma zinciri: sayfa dibine in, iç kutunun üstünde tekerleği YUKARI çevir → pencere kaydırması azalmalı. node kaydirma-zinciri.mjs <vekil> <rota>
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, ''); const bek = ms => new Promise(r => setTimeout(r, ms));
const [V, R] = process.argv.slice(2);
const b = await baslat(9972, S + '/profil-sahip'); await b.git(V + '/__giris', 6000); await b.boyut(1280, 800); await b.git(V + R, 5000);
await b.ev(`document.querySelectorAll('details:not([open]) > summary').forEach(s=>{ if(/Daha fazla|ayrıntı|Tüm ekran/i.test(s.innerText)) s.click(); }); 1`); await bek(800);
if (process.env.MUTASYON) await b.ev(`document.querySelectorAll('.gk-ayrinti-govde, #kl-ekranlar').forEach(e=>e.style.overscrollBehavior='contain'); 1`);   // eski kural: kanıt için
const tekerlek = async (y, dy) => { await b.cmd('Input.dispatchMouseEvent', { type: 'mouseWheel', x: 640, y, deltaX: 0, deltaY: dy }); await bek(120); };
for (let i = 0; i < 40; i++) await tekerlek(500, 400);   // dibe in (iç kutu + sayfa)
const dip = await b.ev('({win:scrollY, ic:[...document.querySelectorAll("*")].filter(e=>/auto|scroll/.test(getComputedStyle(e).overflowY)&&e.scrollHeight>e.clientHeight+2&&e.getBoundingClientRect().height>0).map(e=>(e.id||e.className.toString().split(" ")[0])+" st"+e.scrollTop+"/"+(e.scrollHeight-e.clientHeight)+" top"+Math.round(e.getBoundingClientRect().top)+" h"+Math.round(e.getBoundingClientRect().height))})');
// iç kutunun üstünde yukarı çevir
const hedef = await b.ev('(()=>{const e=[...document.querySelectorAll("*")].find(e=>/auto|scroll/.test(getComputedStyle(e).overflowY)&&e.scrollHeight>e.clientHeight+2&&e.getBoundingClientRect().height>100&&e!==document.scrollingElement); if(!e) return null; const r=e.getBoundingClientRect(); return Math.round(Math.max(r.top+20, Math.min(r.bottom-20, 400)))})()');
for (let i = 0; i < 60; i++) await tekerlek(hedef ?? 400, -400);
const ust = await b.ev('scrollY');
console.log(R, JSON.stringify({ dip, hedefY: hedef, sonundaPencere: ust }), ust === 0 ? 'GEÇTİ (sayfanın başına çıkıldı)' : 'KALDI (tekerlek iç kutuda hapsoldu)');
await b.foto(`${S}/foto/kanit/kaydirma-${R.replace(/\W/g, '')}.png`);
b.kapat();
