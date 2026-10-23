import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, ''); const bek = ms => new Promise(r => setTimeout(r, ms));
const b = await baslat(9997, S + '/profil-sahip'); await b.git(process.argv[2] + '/__giris', 6000); await b.boyut(375, 812);
await b.cmd('Page.addScriptToEvaluateOnNewDocument', { source: `window.__cls=[];new PerformanceObserver(l=>{for(const e of l.getEntries()){if(e.hadRecentInput)continue;window.__cls.push({v:+e.value.toFixed(3),t:Math.round(e.startTime),k:(e.sources||[]).map(s=>{const n=s.node;return n&&n.nodeType===1?((n.id?'#'+n.id:'')+'.'+String(n.className).split(' ')[0]+' '+Math.round(s.previousRect.top)+'→'+Math.round(s.currentRect.top)):'?'}).slice(0,3)})}}).observe({type:'layout-shift',buffered:true});` });
for (const r of ['/gider', '/banka', '/']) { await b.git(process.argv[2] + r, 6000); const c = await b.ev('window.__cls'); console.log(r, c.reduce((a, e) => a + e.v, 0).toFixed(3), JSON.stringify(c.filter(e => e.v > 0.02))); }
b.kapat();
