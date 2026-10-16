// KEŞİF DÖNGÜSÜ SÜRÜCÜSÜ (kurucu 30.09: "döngüyü kur, farklı session'da kur, sana rapor versin").
// Tek seferlik keşif oturumunun (/private/tmp/kuyumcu-kesif/b/kesif.mjs, 30.09 00:20) kendi kendine
// yeten sürümü: kesif-olc ölçeri gömülü, başsız Chrome'u kendisi başlatır (CDP yoksa).
// Her rota: açılış ölçümü + (etk=1 ise) her seçici/select/radyo seçeneği, her güvenli açıcı/sekme/⋯/
// katlanır tıklanır, her adımdan sonra yeniden ölçülür. Kayıt/silme/gönderme düğmelerine BASILMAZ.
// Kullanım: node kesif.mjs <BASIT|PRO> <aydinlik|karanlik> <genislik> <etk 0|1> <rota1,rota2,...> [etiket]
// Ortam: KESIF_HEDEF (vekil, vars. http://localhost:5233) · KESIF_CDP (vars. 9952)
//        KESIF_PROFIL (vars. /private/tmp/kuyumcu-kesif/dongu/profil) · KESIF_OUT (vars. /private/tmp/kuyumcu-kesif/dongu/sonuc)
//        KESIF_ARACLAR (sikisma-olc.js + tur/ekstra.js dizini; vars. bu betiğin üst dizini)
//        KESIF_SINIR (rota başına eylem, 160) · KESIF_SURE (rota başına dk, 8)
// ⚠ Profil/çıktı ASLA proje ağacına yazılmaz. 5199/5200 ve şerit portları (5160-5198) kullanılmaz.
import { spawn } from 'node:child_process';
import { appendFileSync, mkdirSync, readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import os from 'node:os';
const BURASI = dirname(fileURLToPath(import.meta.url));
const HEDEF = process.env.KESIF_HEDEF || 'http://localhost:5233';
const CDP = +(process.env.KESIF_CDP || 9952);
const PROFIL = process.env.KESIF_PROFIL || '/private/tmp/kuyumcu-kesif/dongu/profil';
const OUT = process.env.KESIF_OUT || '/private/tmp/kuyumcu-kesif/dongu/sonuc';
const ARACLAR = process.env.KESIF_ARACLAR || join(BURASI, '..');
if (/\/ByGatech-yerel\/jewellery\//.test(PROFIL + '|' + OUT)) { console.error('REDDEDİLDİ: profil/çıktı proje ağacında olamaz'); process.exit(2); }
const CHROME = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
const bekle = ms => new Promise(r => setTimeout(r, ms));
// ── ölçer (tek seferlik oturumun kesif-olc.js'i, birebir)
const KESIF_OLC = String.raw`
// Keşif testi ek ölçer (30.09) — sikisma-olc.js + ekstra.js üstüne.
// K7x: aynı satırdaki ALAN kutuları (input/select/textarea/.secici-tetik/radyo grubu)
//      arasında etiket üstü / kutu üstü / kutu altı farkı (>3 px). Radyo grubu
//      K7'de kontrol sayılmıyordu; burada sayılır.
// KM : ham kod (BÜYÜK_ALT_ÇİZGİ), İngilizce/teknik sızıntı, kültürsüz para biçimi.
(() => {
  const vis = e => { if (!e) return false; const r = e.getBoundingClientRect(); const s = getComputedStyle(e); return r.width > 0 && r.height > 0 && s.visibility !== 'hidden' && s.display !== 'none' && +s.opacity !== 0; };
  const ad = e => (e.id ? '#' + e.id : e.tagName.toLowerCase() + (typeof e.className === 'string' && e.className.trim() ? '.' + e.className.trim().split(/\s+/)[0] : '')) + ' "' + (e.innerText || e.value || e.getAttribute('aria-label') || '').trim().replace(/\s+/g, ' ').slice(0, 28) + '"';
  const KONTROL = '.secici-tetik, input:not([type=checkbox]):not([type=radio]):not([type=hidden]), select, textarea, [role=radiogroup], .secim-grubu';
  const katmanKok = () => { const k = [...document.querySelectorAll('.cekmece, [role=dialog], dialog[open], .modal')].filter(vis); return k.length ? k[k.length - 1] : null; };
  window.__kok = () => katmanKok() || document.querySelector('main, .icerik, #icerik, article') || document.body;
  const etiketOf = (item, c) => {
    const adaylar = [...item.querySelectorAll('label, .alan-etiket, legend')].filter(vis).filter(l => !l.closest('.secim-secenek') && !l.classList.contains('secim-secenek'));
    const cr = c.getBoundingClientRect();
    const once = adaylar.filter(l => l.getBoundingClientRect().bottom <= cr.top + 4);
    return once.length ? once[once.length - 1] : null;
  };
  window.__k7x = (kok) => {
    kok = kok || window.__kok();
    const ctrls = [...kok.querySelectorAll(KONTROL)].filter(vis).filter(c => !c.closest('.secici-liste-kap, .secim-grubu input') && !(c.matches('input') && c.closest('.secici-liste-kap')));
    const kayit = new Map();
    for (const c of ctrls) {
      if (c.closest('[role=radiogroup]') && !c.matches('[role=radiogroup]')) continue;
      let item = c, P = c.parentElement;
      while (P && P !== document.body) {
        const s = getComputedStyle(P);
        const satir = (s.display.includes('flex') && !s.flexDirection.startsWith('column')) || s.display.includes('grid');
        if (satir && [...P.children].filter(vis).length >= 2) break;
        item = P; P = P.parentElement;
      }
      if (!P || P === document.body) continue;
      if (!kayit.has(P)) kayit.set(P, []);
      const grp = kayit.get(P);
      if (grp.some(g => g.item === item)) continue;
      grp.push({ item, c, l: etiketOf(item, c) });
    }
    const out = [];
    for (const [P, grp] of kayit) {
      if (grp.length < 2) continue;
      // aynı görsel satır: öğe kutularının üstü 24 px içinde
      const kume = [];
      for (const g of grp) { const t = g.item.getBoundingClientRect().top; const k = kume.find(x => Math.abs(x.t - t) < 24); if (k) k.u.push(g); else kume.push({ t, u: [g] }); }
      for (const k of kume) {
        if (k.u.length < 2) continue;
        const cr = k.u.map(g => g.c.getBoundingClientRect());
        const ust = cr.map(r => r.top), alt = cr.map(r => r.bottom);
        const et = k.u.filter(g => g.l).map(g => g.l.getBoundingClientRect().top);
        const kutuTip = k.u.map(g => g.c.matches('[role=radiogroup], .secim-grubu') ? 'radyo' : 'kutu');
        const ayniTip = kutuTip.every(t => t === kutuTip[0]);
        const fUst = Math.max(...ust) - Math.min(...ust), fAlt = Math.max(...alt) - Math.min(...alt), fEt = et.length >= 2 ? Math.max(...et) - Math.min(...et) : 0;
        const kusur = fUst > 3 || fEt > 3 || (ayniTip && fAlt > 3);
        if (kusur) {
          const min = Math.min(...ust);
          out.push({ sat: ad(P), fark: { kutuUst: Math.round(fUst), kutuAlt: Math.round(fAlt), etiketUst: Math.round(fEt) },
            alanlar: k.u.map((g, i) => ({ etiket: g.l ? (g.l.innerText || '').trim().slice(0, 30) : null, kontrol: ad(g.c), tip: kutuTip[i], dy: Math.round(cr[i].top - min), h: Math.round(cr[i].height) })) });
        }
      }
    }
    return out;
  };
  // Metin taraması: ham kod, İngilizce/teknik sızıntı, kültürsüz para
  window.__metin = (kok) => {
    kok = kok || document.body;
    const w = document.createTreeWalker(kok, NodeFilter.SHOW_TEXT); let n; const hamKod = new Set(), ing = new Set(), para = new Set();
    const ING = /\b(null|undefined|NaN|Infinity|Error|Exception|Loading\.\.\.|Invalid|required|is required|must be|true|false|Object|Submit|Cancel|Save|Delete|Search|Select\.\.\.|Unhandled|Bad Request|Not Found|Internal Server)\b|\{\d\}|\[object /;
    while ((n = w.nextNode())) {
      const p = n.parentElement; if (!p || !vis(p) || p.closest('script, style, code, pre, noscript, [aria-hidden=true], .sr-only, .gorsel-gizli, option')) continue;
      const t = n.textContent; if (!t.trim()) continue;
      (t.match(/\b[A-ZÇĞİÖŞÜ]{2,}(?:_[A-ZÇĞİÖŞÜ0-9]+)+\b/g) || []).forEach(x => hamKod.add(x));
      const m = t.match(ING); if (m) ing.add(m[0] + ' ← "' + t.trim().slice(0, 50) + '"');
      (t.match(/\b\d{1,3}(?:,\d{3})+\.\d{2}\b|\b\d{5,}(?:[.,]\d{1,2})?\s?(?:₺|TL)\b|₺\s?\d{5,}\b|\bTRY\s?\d/g) || []).forEach(x => para.add(x));
    }
    // yer tutucu / değer içinde de bak
    [...document.querySelectorAll('input, textarea')].filter(vis).forEach(i => { const v = (i.placeholder || '') + ' ' + (i.value || ''); (v.match(/\b[A-ZÇĞİÖŞÜ]{2,}(?:_[A-ZÇĞİÖŞÜ0-9]+)+\b/g) || []).forEach(x => hamKod.add(x + ' (kutu)')); });
    return { hamKod: [...hamKod].slice(0, 8), ingilizce: [...ing].slice(0, 6), hamPara: [...para].slice(0, 6) };
  };
  // K9x — üst üste binen yazı; açılır katmanın (liste/menü/popover/çekmece) altındaki içerikle çakışma SAYILMAZ
  const katmanda = e => { for (let p = e; p && p !== document.body; p = p.parentElement) { if (p.matches('.secici-liste-kap, [popover], [role=menu], [role=listbox], [role=dialog], .cekmece, dialog, .acilir, .acilir-menu, [role=tooltip]')) return p; const s = getComputedStyle(p); if ((s.position === 'absolute' || s.position === 'fixed') && (parseInt(s.zIndex) > 0)) return p; } return null; };
  window.__k9x = (kok) => {
    kok = kok || document.querySelector('main') || document.body;
    const yap = [...kok.querySelectorAll('*')].filter(vis).filter(e => e.children.length === 0 && (e.innerText || '').trim()).slice(0, 700).map(e => [e, e.getBoundingClientRect(), katmanda(e)]);
    const bin = [];
    for (let i = 0; i < yap.length && bin.length < 6; i++) for (let j = i + 1; j < yap.length; j++) { const [a, r, ka] = yap[i], [c, q, kc] = yap[j]; if (a.contains(c) || c.contains(a) || ka !== kc) continue; const ox = Math.min(r.right, q.right) - Math.max(r.left, q.left), oy = Math.min(r.bottom, q.bottom) - Math.max(r.top, q.top); if (ox > 4 && oy > 4) { bin.push(ad(a) + ' ⟂ ' + ad(c)); break; } }
    return bin;
  };
  window.__yukleniyor = () => [...document.querySelectorAll('.donen-halka, .yukleniyor-kutu, [aria-busy=true], .yukleniyor')].some(e => vis(e));
  window.__tamOlc = () => {
    let o = {}; try { o = window.__olc(); } catch (e) { o = { olcHata: String(e) }; }
    let e2 = {}; try { e2 = window.__ek(); } catch (e) { e2 = { ekHata: String(e) }; }
    const k7x = window.__k7x(); const kokEl = window.__kok();
    return { ...o, ...e2, K7x: k7x, K9x: window.__k9x(), katmanAcik: kokEl !== (document.querySelector('main, .icerik, #icerik, article') || document.body), metin: window.__metin(), yol: location.pathname };
  };
  return 'kesif-kuruldu';
})()
`;
// ── CDP bağlantısı: çalışan Chrome'a bağlan, yoksa başlat
async function bagla() {
  let chrome = null, ws;
  const bul = async () => { try { const l = await (await fetch(`http://127.0.0.1:${CDP}/json/list`)).json(); return l.find(x => x.type === 'page')?.webSocketDebuggerUrl; } catch { return null; } };
  ws = await bul();
  if (!ws) {
    mkdirSync(PROFIL, { recursive: true });
    chrome = spawn(CHROME, ['--headless=new', '--disable-gpu', '--hide-scrollbars', `--remote-debugging-port=${CDP}`, `--user-data-dir=${PROFIL}`, '--no-first-run', '--no-default-browser-check', '--lang=tr-TR', 'about:blank'], { stdio: 'ignore' });
    for (let i = 0; i < 60 && !ws; i++) { await bekle(250); ws = await bul(); }
    if (!ws) throw new Error('Chrome CDP ' + CDP + ' açılmadı');
  }
  const sock = new WebSocket(ws); await new Promise((r, j) => { sock.onopen = r; sock.onerror = j; });
  let id = 0; const bek = new Map(); const olay = [];
  sock.onmessage = m => { const d = JSON.parse(m.data); if (d.id && bek.has(d.id)) { const { r, j } = bek.get(d.id); bek.delete(d.id); d.error ? j(new Error(JSON.stringify(d.error))) : r(d.result); } else if (d.method) olay.push(d); };
  const cmd = (method, params = {}) => new Promise((r, j) => { const i = ++id; bek.set(i, { r, j }); sock.send(JSON.stringify({ id: i, method, params })); });
  await cmd('Page.enable'); await cmd('Runtime.enable'); await cmd('Network.enable'); await cmd('Log.enable');
  const ev = async (expr) => { const r = await cmd('Runtime.evaluate', { expression: expr, awaitPromise: true, returnByValue: true }); if (r.exceptionDetails) throw new Error(r.exceptionDetails.exception?.description || r.exceptionDetails.text); return r.result.value; };
  const boyut = (w, h) => cmd('Emulation.setDeviceMetricsOverride', { width: w, height: h, deviceScaleFactor: 1, mobile: w < 768 });
  const git = async (url, ms = 2500) => { await cmd('Page.navigate', { url }); await bekle(ms); };
  const foto = async (dosya, tam = false) => { const { writeFileSync } = await import('node:fs'); const r = await cmd('Page.captureScreenshot', { format: 'png', captureBeyondViewport: tam }); writeFileSync(dosya, Buffer.from(r.data, 'base64')); };
  const kur = async () => { await ev(readFileSync(join(ARACLAR, 'sikisma-olc.js'), 'utf8')); await ev(readFileSync(join(ARACLAR, 'tur', 'ekstra.js'), 'utf8')); await ev(KESIF_OLC); };
  const olaylar = () => { const o = olay.splice(0); const r = { konsol: [], ag: [], mutasyon: [] };
    for (const d of o) {
      if (d.method === 'Runtime.exceptionThrown') r.konsol.push('istisna: ' + (d.params.exceptionDetails.exception?.description || d.params.exceptionDetails.text || '').slice(0, 200));
      else if (d.method === 'Runtime.consoleAPICalled' && (d.params.type === 'error' || d.params.type === 'warning')) r.konsol.push(d.params.type + ': ' + d.params.args.map(a => a.value ?? a.description ?? '').join(' ').slice(0, 200));
      else if (d.method === 'Log.entryAdded' && d.params.entry.level === 'error') r.konsol.push('log: ' + (d.params.entry.text || '').slice(0, 200) + ' ' + (d.params.entry.url || '').slice(0, 80));
      else if (d.method === 'Network.responseReceived' && d.params.response.status >= 400) r.ag.push(d.params.response.status + ' ' + d.params.response.url.replace(/^https?:\/\/[^/]+/, '').slice(0, 120));
      else if (d.method === 'Network.requestWillBeSent' && /POST|PUT|PATCH|DELETE/.test(d.params.request.method) && d.params.request.url.includes('/api/')) r.mutasyon.push(d.params.request.method + ' ' + d.params.request.url.replace(/^https?:\/\/[^/]+/, '').slice(0, 120));
    }
    // jeton/erişim anahtarı hiçbir çıktıya yazılmaz
    const san = x => x.replace(/access_token=[^&\s'"]+/g, 'access_token=***').replace(/eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]+\.?[A-Za-z0-9_-]*/g, '<jwt>');
    r.konsol = r.konsol.map(san).filter(x => !/hubs\/kur|MONO_WASM: WebSocket|WebSocket connection|signalr|SignalR|Failed to start the connection|WebSocket failed/i.test(x)); r.ag = r.ag.map(san); r.mutasyon = r.mutasyon.map(san); return r; };
  const kapat = () => { try { sock.close(); } catch {} if (chrome) chrome.kill(); };
  return { cmd, ev, boyut, git, foto, kur, olaylar, kapat };
}
const [MOD, TEMA, WS, ETK, ROT, ETIKET = 'kosum'] = process.argv.slice(2);
const W = +WS, BOY = { 1440: 900, 1280: 800, 960: 820, 375: 812 }[W] || 900;
const yaz = (o) => appendFileSync(`${OUT}/${ETIKET}.jsonl`, JSON.stringify({ t: new Date().toISOString().slice(11, 19), MOD, TEMA, W, ...o }) + '\n');
mkdirSync(OUT + '/foto', { recursive: true });
const b = await bagla(); await b.boyut(W, BOY);
const yukKontrol = async () => { for (let i = 0; i < 20; i++) { const l = os.loadavg()[0]; if (l <= 20) return; if (i === 0) console.log('yük', l.toFixed(1), 'bekleniyor'); await bekle(30000); } console.log('yük hâlâ yüksek, devam'); };
const girisli = async () => { try { return await b.ev(`!!document.querySelector('#ust-mod-basit')`); } catch { return false; } };
const ayarla = async () => {
  await b.kur(); await b.ev(KESIF_FN);
  const hedef = MOD === 'PRO' ? '#ust-mod-profesyonel' : '#ust-mod-basit';
  for (let d = 0; d < 4; d++) {
    const ok = await b.ev(`document.querySelector('${hedef}')?.getAttribute('aria-pressed')==='true'`);
    if (ok) break; await b.ev(`document.querySelector('${hedef}')?.click(); 1`); await bekle(1500);
  }
  await b.ev(`window.kuyumcuTema && window.kuyumcuTema.uygula(${JSON.stringify(TEMA)}); 1`);
};
const yukBekle = async () => { const t0 = Date.now(); while (Date.now() - t0 < 10000) { let y = false; try { y = await b.ev('window.__yukleniyor ? window.__yukleniyor() : false'); } catch {} if (!y) return 0; await bekle(400); } return 1; };
const gitRota = async (rota, tam = false) => {
  const hazir = !tam && await b.ev(`typeof Blazor!=='undefined' && typeof window.__tamOlc==='function'`).catch(() => false);
  if (hazir) { await b.ev(`Blazor.navigateTo(${JSON.stringify(rota)}); 1`); await bekle(1300); }
  else { await b.git(HEDEF + rota, 5500); if (!(await girisli())) { await b.git(HEDEF + '/__giris', 7000); await b.git(HEDEF + rota, 5500); } await ayarla(); }
  const spin = await yukBekle(); await b.ev('window.scrollTo(0,0); 1'); return spin;
};
const guvenliAd = s => s.replace(/[^a-z0-9]+/gi, '_').replace(/^_|_$/g, '') || 'kok';
// ── kusur imzaları
function imzalar(m, o) {
  const s = [];
  (m.K7x || []).forEach(k => s.push(['K7x', `${k.alanlar.map(a => (a.etiket || a.kontrol) + '(' + a.tip + ',dy' + a.dy + ',h' + a.h + ')').join(' | ')} · kutuÜst±${k.fark.kutuUst} kutuAlt±${k.fark.kutuAlt} etiketÜst±${k.fark.etiketUst}`]));
  (m.ornek?.hiza || []).forEach(h => s.push(['K7', h]));
  (m.ornek?.tasan || []).forEach(h => s.push(['K3-tasan', h]));
  (m.ornek?.kirik || []).forEach(h => s.push(['K3-kirik', h]));
  (m.ornek?.elipsis || []).forEach(h => s.push(['K10', h]));
  (m.ek?.kes || []).forEach(h => s.push(['K10b', h]));
  (m.ek?.kirp || []).forEach(h => s.push(['K13-kirp', h]));
  (m.ek?.ezik || []).forEach(h => s.push(['K12-ezik', h]));
  (m.ek?.etiketBin || []).forEach(h => s.push(['K9b', h]));
  (m.K9x || []).forEach(h => s.push(['K9', h]));
  (m.ornek?.dugmeDikey || []).forEach(h => s.push(['K12-dikey', h]));
  if (m.yatay > 0) s.push(['yatay', `yatay kaydırma ${m.yatay} px`]);
  if (m.K1_gizliGorunen > 0) s.push(['K1', `[hidden] görünen ${m.K1_gizliGorunen}`]);
  if (m.K11_hataBandi) s.push(['K11', 'Blazor hata bandı görünür']);
  if (m.K5_TRY > 0) s.push(['K5', `ekranda 'TRY' ×${m.K5_TRY}`]);
  if (MOD === 'BASIT') (m.ornek?.jargon || []).forEach(h => s.push(['K8', h]));
  (m.metin?.hamKod || []).forEach(h => s.push(['ham-kod', h]));
  (m.metin?.ingilizce || []).forEach(h => s.push(['ingilizce', h]));
  (m.metin?.hamPara || []).forEach(h => s.push(['ham-para', h]));
  (o?.konsol || []).forEach(h => s.push(['konsol', h]));
  (o?.ag || []).forEach(h => s.push(['ag-4xx5xx', h]));
  return s;
}
// ── eylem keşfi (sayfada çalışır)
const KESIF_FN = `window.__kesif = (kokSel) => {
  const vis = e => { if (!e) return false; const r = e.getBoundingClientRect(); const s = getComputedStyle(e); return r.width > 0 && r.height > 0 && s.visibility !== 'hidden' && s.display !== 'none' && +s.opacity !== 0; };
  const YASAK = /kaydet|kaydı|sil\\b|silin|onayla|gönder|fatura kes|faturaya|öde\\b|ödeme yap|tahsil et|yazdır|çıkış|oturum|pasife|aktifleştir|aktif et|geri al|uygula|içe aktar|yedek al|geri yükle|devret|iptal et|vezneye|mutabakat|kilitle|sıfırla|onay|evet|başlat|bitir|tamamla|indir|dışa aktar|çalıştır|kes\\b|yükle|oluştur|aç ve|kapat gün|gün sonu|gönderildi|işaretle|varsayılan yap|şifre|parola|davet|e-?posta at|sms|whatsapp|paylaş|kopyala|ödendi|kapat$|temizle|satışı bitir|fişi kapat/i;
  const KATMAN = '.cekmece, [role=dialog], dialog[open], .modal, [popover], .secici-liste-kap';
  const kok = kokSel ? document.querySelector(kokSel) : (document.querySelector('main') || document.body);
  if (!kok || !vis(kok)) return null;
  const icinde = e => kok.contains(e) && !e.closest('nav, aside, .kenar-cubugu, .ust-bar, .secici-liste-kap') && !(e.closest('header') && !e.closest('main')) && (kokSel ? true : !(e.closest(KATMAN) && e.closest(KATMAN) !== kok));
  const out = []; window.__metinSay = {};
  const id = e => { if (!e.dataset.kz) e.dataset.kz = 'kz' + (window.__kzSay = (window.__kzSay || 0) + 1); return e.dataset.kz; };
  const metin = e => (e.innerText || e.getAttribute('aria-label') || e.title || '').trim().replace(/\\s+/g, ' ').slice(0, 50);
  const sekmeBenzeri = e => { const p = e.parentElement; if (!p) return false; const kar = [...p.children].filter(c => c.matches('button, a, [role=button]') && vis(c)); return kar.length >= 3 && kar.some(c => c.matches('.secili, .aktif, [aria-pressed=true], [aria-selected=true], [aria-current]')); };
  kok.querySelectorAll('.secici-tetik').forEach(e => { if (vis(e) && icinde(e) && !e.disabled) out.push({ tip: 'secici', k: 'secici:' + (e.id || metin(e)), sel: '[data-kz="' + id(e) + '"]', m: (e.closest('.secici-alan')?.querySelector('.alan-etiket')?.innerText || e.id || '').trim().replace(/\\s+/g,' ').slice(0, 30) }); });
  kok.querySelectorAll('select').forEach(e => { if (vis(e) && icinde(e) && !e.disabled) [...e.options].slice(0, 15).forEach((o, i) => out.push({ tip: 'select', k: 'select:' + (e.id || e.name || '') + '#' + o.value, sel: '[data-kz="' + id(e) + '"]', i, m: (e.labels?.[0]?.innerText || e.id || '').trim().slice(0, 30) + '=' + o.text.trim().slice(0, 30) })); });
  kok.querySelectorAll('[role=radiogroup]').forEach(g => { if (!vis(g) || !icinde(g)) return; [...g.querySelectorAll('input[type=radio]')].forEach(r => { if (r.disabled) return; const l = r.closest('label') || document.querySelector('label[for="' + r.id + '"]'); out.push({ tip: 'radyo', k: 'radyo:' + (g.id || g.getAttribute('aria-label')) + '#' + r.value, sel: '[data-kz="' + id(l || r) + '"]', m: (g.getAttribute('aria-label') || g.id) + '=' + metin(l || r) }); }); });
  kok.querySelectorAll('summary').forEach(e => { if (vis(e) && icinde(e)) out.push({ tip: 'katlanir', k: 'sum:' + metin(e), sel: '[data-kz="' + id(e) + '"]', m: metin(e) }); });
  kok.querySelectorAll('button, [role=button], [role=tab], [role=menuitem], a.dugme, a.btn').forEach(e => {
    if (!vis(e) || !icinde(e) || e.disabled || e.matches('.secici-tetik, summary')) return;
    if (e.type === 'submit') return;
    const t = metin(e); if (!t || YASAK.test(t) || YASAK.test(e.title || '') || YASAK.test(e.getAttribute('aria-label') || '')) return;
    if (e.tagName === 'A' && e.getAttribute('href') && !e.getAttribute('href').startsWith('#')) return;
    if (/^(tamam|vazgeç|kapat|×|✕|iptal|fişe dön)$/i.test(t)) return;
    const hedef = e.getAttribute('popovertarget') || e.getAttribute('aria-controls') || null;
    const sek = e.matches('[role=tab]') || sekmeBenzeri(e);
    const say = (window.__metinSay = window.__metinSay || {}); const mk = (kokSel || '') + t; if (!e.id || /-\d+$|\d{6,}/.test(e.id)) { say[mk] = (say[mk] || 0) + 1; if (say[mk] > 2) return; }
    out.push({ tip: sek ? 'sekme' : 'dugme', k: (sek ? 'sekme:' : 'dugme:') + (e.id || t), sel: '[data-kz="' + id(e) + '"]', m: t, hedef, acici: !!hedef || e.hasAttribute('aria-haspopup') || e.hasAttribute('aria-expanded') });
  });
  return out;
};
window.__durum = () => [...document.querySelectorAll('main .secili, main [aria-pressed=true], main [aria-selected=true]')].filter(e => e.getBoundingClientRect().width > 0 && e.matches('button, [role=tab], a')).map(e => (e.innerText || '').trim().slice(0, 20)).join('+');
window.__katmanlar = () => [...document.querySelectorAll('.cekmece, [role=dialog], dialog[open], .modal, :popover-open, [aria-modal=true]')].filter(e => { const r = e.getBoundingClientRect(); return r.width > 0 && r.height > 0 && getComputedStyle(e).display !== 'none'; }).map(e => { if (!e.dataset.kzk) e.dataset.kzk = 'k' + (window.__kzkSay = (window.__kzkSay || 0) + 1); return '[data-kzk="' + e.dataset.kzk + '"]'; });
1`;
const kapatKatman = async () => { await b.ev(`(async()=>{ const vis=e=>{if(!e)return false;const r=e.getBoundingClientRect();return r.width>0&&r.height>0&&getComputedStyle(e).display!=='none'}; document.querySelectorAll(':popover-open').forEach(p=>{try{p.hidePopover()}catch{}}); for(const t of [document.activeElement, document.body, document]) t?.dispatchEvent?.(new KeyboardEvent('keydown',{key:'Escape',bubbles:true})); await new Promise(r=>setTimeout(r,200)); const p=[...document.querySelectorAll('.perde, .cekmece-perde, .modal-perde, [id$=-perde], .secici-perde')].find(vis); if(p) p.click(); await new Promise(r=>setTimeout(r,200)); const k=[...document.querySelectorAll('.cekmece, [role=dialog], dialog[open], .modal')].filter(vis).pop(); if(k){const x=[...k.querySelectorAll('button')].find(b=>/^(kapat|vazgeç|×|✕|tamam)$/i.test((b.innerText||b.getAttribute('aria-label')||'').trim())); if(x) x.click();} return 1})()`).catch(() => {}); await bekle(300); };
async function eylemUygula(e) {
  if (e.tip === 'secici') {
    // aç, seçenekleri say; her seçenek ayrı eylem olarak kuyruğa eklenir (anahtar: secici#deger)
    await b.ev(`document.querySelector('${e.sel}')?.click(); 1`); await bekle(450);
    const sec = await b.ev(`(()=>{const t=document.querySelector('${e.sel}'); const kap=t&&(document.getElementById(t.id+'-liste-kap')||document.querySelector('.secici-liste-kap')); if(!kap) return []; return [...kap.querySelectorAll('[role=option]')].filter(o=>o.getBoundingClientRect().width>0).slice(0,15).map(o=>({d:o.dataset.deger, m:(o.innerText||'').trim().replace(/\\s+/g,' ').slice(0,40)}))})()`);
    return { secenekler: sec };
  }
  if (e.tip === 'seciciSecenek') {
    const ac = await b.ev(`(()=>{const t=document.querySelector('${e.sel}'); if(!t) return false; if(!t.classList.contains('acik')) t.click(); return true})()`); if (!ac) return { yok: true };
    await bekle(400);
    const ok = await b.ev(`(()=>{const t=document.querySelector('${e.sel}'); const kap=t&&(document.getElementById(t.id+'-liste-kap')||document.querySelector('.secici-liste-kap')); const o=kap&&[...kap.querySelectorAll('[role=option]')].find(o=>o.dataset.deger===${JSON.stringify(e.d)}); if(!o) return false; o.click(); return true})()`);
    return { ok };
  }
  if (e.tip === 'select') { const ok = await b.ev(`(()=>{const s=document.querySelector('${e.sel}'); if(!s) return false; s.selectedIndex=${e.i}; s.dispatchEvent(new Event('change',{bubbles:true})); s.dispatchEvent(new Event('input',{bubbles:true})); return true})()`); return { ok }; }
  const ok = await b.ev(`(()=>{const x=document.querySelector('${e.sel}'); if(!x) return false; x.scrollIntoView({block:'center'}); x.click(); return true})()`);
  return { ok };
}
const gorulen = new Set(existsSync(`${OUT}/${ETIKET}.imza`) ? [] : []);
async function olcVeKaydet(rota, adim, ekOlay) {
  let m; try { m = await b.ev('window.__tamOlc()'); } catch (e) { await b.kur().catch(() => {}); try { m = await b.ev('window.__tamOlc()'); } catch (e2) { yaz({ tip: 'olcHata', rota, adim, hata: String(e2).slice(0, 200) }); return; } }
  const o = b.olaylar(); if (ekOlay) { o.konsol.push(...ekOlay.konsol); o.ag.push(...ekOlay.ag); o.mutasyon.push(...ekOlay.mutasyon); }
  const imz = imzalar(m, o); const yeni = imz.filter(([tur, ay]) => { const k = `${rota}|${tur}|${ay}`; if (gorulen.has(k)) return false; gorulen.add(k); return true; });
  let foto = null;
  if (yeni.length) { foto = `${guvenliAd(rota)}__${MOD}_${TEMA}_${W}__${String(adim.n).padStart(3, '0')}.png`; await b.foto(`${OUT}/foto/${foto}`).catch(() => { foto = null; }); }
  yaz({ tip: 'olcum', rota, yol: m.yol, adim, yeni, imzaSay: imz.length, mutasyon: o.mutasyon, foto, K4: [m.K4_boyKat, m.K4_bolum], K2: m.K2_kartSagYuzde });
  return m;
}
for (const rota of ROT.split(',').filter(Boolean)) {
  await yukKontrol();
  const t0 = Date.now();
  let spin; try { spin = await gitRota(rota, true); } catch (e) { yaz({ tip: 'hata', rota, hata: String(e).slice(0, 200) }); continue; }
  const acilisOlay = b.olaylar();
  const yolAcilis = await b.ev('location.pathname').catch(() => '?');
  if (spin) acilisOlay.konsol.push('SPINNER: 10 sn sonra hâlâ yükleniyor');
  const m0 = await olcVeKaydet(rota, { n: 0, eylem: 'açılış', yolAcilis }, acilisOlay);
  await b.foto(`${OUT}/foto/${guvenliAd(rota)}__${MOD}_${TEMA}_${W}__acilis.png`, true).catch(() => {});
  if (ETK !== '1') continue;
  const ziyaret = new Set(); let n = 0; const sinir = +(process.env.KESIF_SINIR || 160), sure = +(process.env.KESIF_SURE || 8) * 60000;
  const zamanVar = () => n < sinir && Date.now() - t0 < sure;
  const hazirla = async () => { const ok = await b.ev('typeof window.__kesif==="function"').catch(() => false); if (!ok) { await b.kur(); await b.ev(KESIF_FN); } };
  const geriDon = async () => { try { await gitRota(rota, true); } catch {} await hazirla(); };
  // kokSel: katman seçicisi (null=main). yenidenAc: katman kapanırsa yeniden açar.
  async function dongu(kokSel, yenidenAc, derinlik, iz) {
    let yeniden = false;
    while (zamanVar()) {
      await hazirla();
      if (kokSel) { const acik = await b.ev(`!!document.querySelector('${kokSel}') && document.querySelector('${kokSel}').getBoundingClientRect().width>0`).catch(() => false); if (!acik) return 'durdu'; }
      const durum = await b.ev('window.__durum()').catch(() => '');
      const list = (await b.ev(`window.__kesif(${JSON.stringify(kokSel)})`).catch(() => null)) || [];
      const anah = e => kokSel ? iz + '>' + e.k : durum + '|' + e.k;
      // öncelik: seçici/select/radyo → katlanır/düğme → sekme (sekme durumu değiştirir)
      const sira = { secici: 0, select: 0, radyo: 0, katlanir: 1, dugme: 2, sekme: 3 };
      const e = list.filter(x => !ziyaret.has(anah(x))).sort((p, q) => sira[p.tip] - sira[q.tip])[0];
      if (!e) { if (!kokSel && !yeniden) { yeniden = true; await geriDon(); continue; } return 'bitti'; }
      yeniden = false;
      ziyaret.add(anah(e)); n++;
      const yolOnce = await b.ev('location.pathname').catch(() => '?');
      if (e.tip === 'secici') {
        const r = await eylemUygula(e); await bekle(300);
        await olcVeKaydet(rota, { n, eylem: `${iz}secici-ac: ${e.m}`, durum });
        await b.ev(`(()=>{const t=document.querySelector('${e.sel}'); const p=t&&document.getElementById(t.id+'-perde'); if(p) p.click(); else if(t&&t.classList.contains('acik')) t.click(); return 1})()`).catch(() => {}); await bekle(250);
        for (const s of r?.secenekler || []) {
          if (!zamanVar()) break;
          const k2 = anah(e) + '#' + s.d; if (ziyaret.has(k2)) continue; ziyaret.add(k2); n++;
          if (kokSel) { const acik = await b.ev(`!!document.querySelector('${kokSel}') && document.querySelector('${kokSel}').getBoundingClientRect().width>0`).catch(() => false); if (!acik) { ziyaret.delete(k2); return 'durdu'; } }
          const r2 = await eylemUygula({ tip: 'seciciSecenek', sel: e.sel, d: s.d });
          if (!r2?.ok) { yaz({ tip: 'erisilemedi', rota, e: `${e.m}=${s.m}` }); continue; }
          await bekle(650); const sp = await yukBekle();
          await olcVeKaydet(rota, { n, eylem: `${iz}${e.m}=${s.m}`, durum }, sp ? { konsol: ['SPINNER: seçim sonrası 10 sn'], ag: [], mutasyon: [] } : null);
          const y2 = await b.ev('location.pathname').catch(() => '?'); if (y2 !== yolOnce) { yaz({ tip: 'rotaDegisti', rota, e: s.m, yol: y2 }); await geriDon(); if (kokSel) return 'durdu'; }
        }
        continue;
      }
      const katOnce = await b.ev('window.__katmanlar()').catch(() => []);
      const r = await eylemUygula(e); if (!r?.ok) { yaz({ tip: 'erisilemedi', rota, e: e.m }); continue; }
      await bekle(700); const sp = await yukBekle();
      const m = await olcVeKaydet(rota, { n, eylem: `${iz}${e.tip}: ${e.m}`, durum }, sp ? { konsol: ['SPINNER: eylem sonrası 10 sn'], ag: [], mutasyon: [] } : null);
      const yol = await b.ev('location.pathname').catch(() => '?');
      if (m?.K11_hataBandi || yol !== yolOnce) { yaz({ tip: 'rotaDegisti', rota, e: e.m, yol }); await geriDon(); if (kokSel) return 'durdu'; continue; }
      // yeni katman açıldı mı?
      const katSonra = await b.ev('window.__katmanlar()').catch(() => []);
      const yeniKat = katSonra.filter(k => !katOnce.includes(k)).pop() || (e.hedef ? `#${e.hedef}` : null);
      const katAcik = yeniKat && await b.ev(`!!document.querySelector(${JSON.stringify(yeniKat)}) && document.querySelector(${JSON.stringify(yeniKat)}).getBoundingClientRect().width>0`).catch(() => false);
      if (katAcik && derinlik < 3) {
        const ac = async () => { if (yenidenAc) { const ok = await b.ev(`!!document.querySelector('${kokSel}') && document.querySelector('${kokSel}').getBoundingClientRect().width>0`).catch(() => false); if (!ok && !(await yenidenAc())) return false; } const v = await b.ev(`!!document.querySelector('${e.sel}')`).catch(() => false); if (!v) return false; await eylemUygula(e); await bekle(700); return b.ev(`!!document.querySelector(${JSON.stringify(yeniKat)}) && document.querySelector(${JSON.stringify(yeniKat)}).getBoundingClientRect().width>0`).catch(() => false); };
        const son = await dongu(yeniKat, ac, derinlik + 1, `${iz}${e.k.slice(0, 40)} > `);
        await kapatKatman();
        if (son === 'durdu') { ziyaret.delete(anah(e)); if (kokSel) return 'durdu'; }
      }
    }
  }
  await hazirla();
  await dongu(null, null, 0, '');
  await kapatKatman();
  yaz({ tip: 'rotaBitti', rota, eylemSay: n, sn: Math.round((Date.now() - t0) / 1000) });
  console.log(rota, MOD, TEMA, W, 'eylem', n, Math.round((Date.now() - t0) / 1000) + 's');
}
b.kapat(); process.exit(0);
