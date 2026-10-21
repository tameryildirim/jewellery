// Tipografi sonrası görüntüler. node foto-tipo.mjs <vekil>
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, ''); const bek = ms => new Promise(r => setTimeout(r, ms));
const [V] = process.argv.slice(2);
const b = await baslat(9992, S + '/profil-sahip'); await b.git(V + '/__giris', 6000);
for (const [w, h, tema] of [[1280, 800, 'aydinlik'], [375, 812, 'karanlik']]) {
  await b.boyut(w, h);
  for (const r of ['/cariler', '/kasalar', '/', '/evraklar/takvim', '/islemler']) {
    await b.ev(`Blazor.navigateTo(${JSON.stringify(r)}); 1`); await bek(3000); await b.ev(`window.kuyumcuTema?.uygula(${JSON.stringify(tema)}); 1`); await bek(400);
    const yatay = await b.ev('document.documentElement.scrollWidth > innerWidth');
    console.log(w, r, 'yatayKaydirma', yatay);
    await b.foto(`${S}/foto/T-${(r.replace(/\W/g, '') || 'kok')}-${w}-${tema}.png`);
  }
}
await b.ev(`window.kuyumcuTema?.uygula('aydinlik'); 1`);
b.kapat();
