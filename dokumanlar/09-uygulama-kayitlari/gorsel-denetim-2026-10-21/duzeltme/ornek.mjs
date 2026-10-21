// Yükleme sırasında ifade örnekler. node ornek.mjs <vekil> <rota> <ifade> [gen]
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, ''); const bek = ms => new Promise(r => setTimeout(r, ms));
const [V, R, IF, W] = process.argv.slice(2);
const b = await baslat(9989, S + '/profil-sahip'); await b.git(V + '/__giris', 6000); await b.boyut(+(W||375), 812);
b.cmd('Page.navigate', { url: V + R }); let son = '';
for (let i = 0; i < +(process.env.ADIM||40); i++) { await bek(100); try { const v = JSON.stringify(await b.ev(IF)); if (v !== son) { console.log(i * 100, v); son = v; } } catch {} }
b.kapat();
