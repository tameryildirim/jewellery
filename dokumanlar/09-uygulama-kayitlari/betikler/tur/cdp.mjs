// Başsız Chrome + ham CDP sürücüsü (bağımlılıksız, Node 22 WebSocket).
import { spawn } from 'node:child_process';
import { mkdirSync } from 'node:fs';
const CHROME = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
export async function baslat(port, profil) {
  mkdirSync(profil, { recursive: true });
  const p = spawn(CHROME, ['--headless=new', '--disable-gpu', '--hide-scrollbars', `--remote-debugging-port=${port}`, `--user-data-dir=${profil}`, '--no-first-run', '--no-default-browser-check', '--lang=tr-TR', 'about:blank'], { stdio: 'ignore' });
  let ws;
  for (let i = 0; i < 50; i++) { try { const l = await (await fetch(`http://127.0.0.1:${port}/json/list`)).json(); const s = l.find(x => x.type === 'page'); if (s) { ws = s.webSocketDebuggerUrl; break; } } catch {} await new Promise(r => setTimeout(r, 200)); }
  const sock = new WebSocket(ws); await new Promise((r, j) => { sock.onopen = r; sock.onerror = j; });
  let id = 0; const bek = new Map(); const olay = [];
  sock.onmessage = m => { const d = JSON.parse(m.data); if (d.id && bek.has(d.id)) { const { r, j } = bek.get(d.id); bek.delete(d.id); d.error ? j(new Error(JSON.stringify(d.error))) : r(d.result); } else if (d.method) olay.push(d); };
  const cmd = (method, params = {}) => new Promise((r, j) => { const i = ++id; bek.set(i, { r, j }); sock.send(JSON.stringify({ id: i, method, params })); });
  await cmd('Page.enable'); await cmd('Runtime.enable');
  const ev = async (expr) => { const r = await cmd('Runtime.evaluate', { expression: expr, awaitPromise: true, returnByValue: true }); if (r.exceptionDetails) throw new Error(r.exceptionDetails.exception?.description || r.exceptionDetails.text); return r.result.value; };
  const boyut = (w, h) => cmd('Emulation.setDeviceMetricsOverride', { width: w, height: h, deviceScaleFactor: 1, mobile: w < 768 });
  const git = async (url, ms = 2500) => { await cmd('Page.navigate', { url }); await new Promise(r => setTimeout(r, ms)); };
  const foto = async (dosya, tam = false) => { const { writeFileSync } = await import('node:fs'); const r = await cmd('Page.captureScreenshot', { format: 'png', captureBeyondViewport: tam }); writeFileSync(dosya, Buffer.from(r.data, 'base64')); };
  const kapat = () => { try { sock.close(); } catch {} p.kill(); };
  return { cmd, ev, boyut, git, foto, kapat, olay };
}
