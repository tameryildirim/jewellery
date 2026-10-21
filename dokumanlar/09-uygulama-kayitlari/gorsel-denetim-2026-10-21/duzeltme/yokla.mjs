// Ara yoklama: verilen rotalarda JS ifadesi çalıştırır. node yokla.mjs <vekil> <genişlik> <ifade> rota...
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, ''); const bek = ms => new Promise(r => setTimeout(r, ms));
const [V, W, IF, ...R] = process.argv.slice(2);
const b = await baslat(9983, S + '/profil-sahip'); await b.git(V + '/__giris', 6000); await b.boyut(+W, 900);
for (const r of R) { await b.ev(`Blazor.navigateTo(${JSON.stringify(r)}); 1`); await bek(3500); console.log(r, JSON.stringify(await b.ev(IF))); }
b.kapat();
