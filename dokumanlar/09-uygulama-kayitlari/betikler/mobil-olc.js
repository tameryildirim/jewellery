// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-18 — 375 px MOBİL GENİŞLİK ÖLÇÜMÜ (gerçek Chrome, CDP; ÖRNEKLEME YOK)
//
// Aşkın koşulu (BK-24): 42/42 rota GERÇEK TARAYICIDA TEK TEK ölçülür.
// Kullanım:
//   1) API 5198de ayakta olsun (5199a DOKUNMA — o kurucunun inceleme sunucusu)
//   2) <calisma>/at.txt, rt.txt  : geçerli erişim/yenileme jetonları
//      <calisma>/rotalar.txt     : her satırda bir rota
//   3) node mobil-olc.js [aydinlik|karanlik] [cikti.json]
// Çıktı: her rota için document.scrollWidth + taşan ilk 6 öğe.
// GEÇME ÖLÇÜTÜ: 375_USTU = 0.
// ⚠ Jeton dosyaları ÇALIŞMA DİZİNİNDEDİR, depoya yazılmaz.
// ═══════════════════════════════════════════════════════════════════════════
const fs = require('fs'), http = require('http'), { spawn } = require('child_process');
const S = __dirname;
const TEMA = process.argv[2] || 'aydinlik';
const CIKTI = process.argv[3] || (S + '/olcum-' + TEMA + '.json');
const ROTALAR = fs.readFileSync(S + '/rotalar.txt', 'utf8').trim().split('\n')
  .map(r => r.replace('{IthalatId}', 'yok').replace('{Tur}', 'SATIS_FISI')
             .replace('{Id}', 'yok'));
const AT = fs.readFileSync(S + '/at.txt', 'utf8').trim();
const RT = fs.readFileSync(S + '/rt.txt', 'utf8').trim();

const get = u => new Promise((ok, no) => http.get(u, r => { let d = ''; r.on('data', c => d += c); r.on('end', () => ok(JSON.parse(d))); }).on('error', no));
const bekle = ms => new Promise(r => setTimeout(r, ms));

(async () => {
  const chrome = spawn('/Applications/Google Chrome.app/Contents/MacOS/Google Chrome', [
    '--headless=new', '--remote-debugging-port=9222', '--user-data-dir=' + S + '/chrome-profil',
    '--no-first-run', '--no-default-browser-check', '--disable-gpu', 'about:blank'
  ], { stdio: 'ignore' });
  let hedef = null;
  for (let i = 0; i < 40 && !hedef; i++) { await bekle(500); try { hedef = (await get('http://127.0.0.1:9222/json/list')).find(t => t.type === 'page'); } catch {} }
  if (!hedef) { console.error('Chrome açılmadı'); process.exit(1); }

  const ws = new WebSocket(hedef.webSocketDebuggerUrl);
  await new Promise(r => ws.onopen = r);
  let no = 0; const bekleyen = new Map();
  ws.onmessage = e => { const m = JSON.parse(e.data); if (m.id && bekleyen.has(m.id)) { bekleyen.get(m.id)(m); bekleyen.delete(m.id); } };
  const cdp = (method, params = {}) => new Promise(r => { const id = ++no; bekleyen.set(id, r); ws.send(JSON.stringify({ id, method, params })); });
  const js = async expr => { const r = await cdp('Runtime.evaluate', { expression: expr, awaitPromise: true, returnByValue: true }); return r.result?.result?.value; };

  await cdp('Page.enable'); await cdp('Runtime.enable');
  await cdp('Emulation.setDeviceMetricsOverride', { width: 375, height: 812, deviceScaleFactor: 1, mobile: true });
  if (TEMA === 'karanlik') await cdp('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-color-scheme', value: 'dark' }] });

  await cdp('Page.navigate', { url: 'http://localhost:5198/giris' }); await bekle(4000);
  await js(`localStorage.setItem('kuyumcu-access',${JSON.stringify(AT)});localStorage.setItem('kuyumcu-refresh',${JSON.stringify(RT)});'ok'`);
  await cdp('Page.navigate', { url: 'http://localhost:5198/' }); await bekle(9000);

  const OLC = `(() => { const d=document.documentElement;
    const tasan=[...document.querySelectorAll('body *')].filter(e=>{const r=e.getBoundingClientRect();return r.width>0&&r.right>375.5&&getComputedStyle(e).position!=='fixed';})
      .map(e=>e.tagName.toLowerCase()+'.'+(e.className&&e.className.baseVal===undefined?String(e.className).trim().split(/\\s+/).slice(0,2).join('.'):'')+'@'+Math.round(e.getBoundingClientRect().right))
      .slice(0,6);
    return {sw:d.scrollWidth, cw:d.clientWidth, bsw:document.body.scrollWidth, yol:location.pathname, tasan};})()`;

  const sonuc = [];
  for (const r of ROTALAR) {
    await js(`Blazor.navigateTo(${JSON.stringify(r)}, false); 'git'`);
    await bekle(1800);
    let m = await js(OLC);
    if (m && m.sw > 375) { await bekle(2200); m = await js(OLC); }
    sonuc.push({ rota: r, ...m });
    console.log(`${(m?.sw ?? '?')}\t${r}\t${m?.yol ?? ''}\t${(m?.tasan || []).join(' | ')}`);
  }
  fs.writeFileSync(CIKTI, JSON.stringify(sonuc, null, 1));
  const kotu = sonuc.filter(x => (x.sw || 0) > 375);
  console.log(`\nTOPLAM=${sonuc.length}  375_USTU=${kotu.length}`);
  ws.close(); chrome.kill(); process.exit(0);
})();
