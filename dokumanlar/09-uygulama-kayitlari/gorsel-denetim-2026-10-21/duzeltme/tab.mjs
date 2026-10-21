// Gerçek Tab basışlarıyla odak sırası + "İçeriğe geç" Enter → odak main. node tab.mjs <vekil> <rota> <n>
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, ''); const bek = ms => new Promise(r => setTimeout(r, ms));
const [V, R, N] = process.argv.slice(2);
const b = await baslat(9986, S + '/profil-sahip'); await b.git(V + '/__giris', 6000); await b.boyut(1280, 900);
await b.git(V + R, 5000);
const tus = async k => { for (const t of ['keyDown', 'keyUp']) await b.cmd('Input.dispatchKeyEvent', { type: t, key: k, code: k, windowsVirtualKeyCode: k === 'Tab' ? 9 : 13, ...(t==='keyDown'&&k==='Enter'?{text:'\r'}:{}) }); await bek(150); };
const ad = () => b.ev(`(()=>{const e=document.activeElement; const r=e.getBoundingClientRect(); return (e.id||e.className||e.tagName).toString().slice(0,32)+' y'+Math.round(r.top)+(e.matches(':focus-visible')?' fv':'')})()`);
const sira = []; for (let i = 0; i < +N; i++) { await tus('Tab'); sira.push(await ad()); }
console.log(R, JSON.stringify(sira));
await b.git(V + R, 5000); await tus('Tab'); const ilk = await ad(); await b.foto(`${S}/foto/O14-icerige-gec.png`); await tus('Enter'); await bek(300);
console.log('ilk:', ilk, '· Enter sonrası:', await ad(), '· rota:', await b.ev('location.pathname'));
await tus('Tab'); console.log('sonraki Tab:', await ad());
b.kapat();
