// CLS ölçümü (tam sayfa yükleme, 375×812). node cls.mjs <vekil> rota...
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, ''); const bek = ms => new Promise(r => setTimeout(r, ms));
const [V, ...R] = process.argv.slice(2);
const b = await baslat(9988, S + '/profil-sahip'); await b.git(V + '/__giris', 6000); await b.boyut(375, 812);
await b.cmd('Page.addScriptToEvaluateOnNewDocument', { source: `window.__cls=[];new PerformanceObserver(l=>{for(const e of l.getEntries()){if(e.hadRecentInput)continue;window.__cls.push({v:e.value,t:Math.round(e.startTime),k:(e.sources||[]).map(s=>{const n=s.node;return n&&n.nodeType===1?((n.id?'#'+n.id:'')+'.'+String(n.className).split(' ')[0]+' '+Math.round(s.previousRect.top)+'→'+Math.round(s.currentRect.top)):'?'}).slice(0,3)})}}).observe({type:'layout-shift',buffered:true});` });
for (const r of R) { await b.git(V + r, 6000);
  const c = await b.ev('window.__cls'); const top = c.reduce((a, e) => a + e.v, 0);
  console.log(r, top.toFixed(3), JSON.stringify(c.filter(e => e.v > 0.01).map(e => [e.v.toFixed(3), e.t, e.k]))); }
b.kapat();
