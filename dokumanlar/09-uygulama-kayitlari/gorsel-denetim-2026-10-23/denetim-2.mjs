// ═══════════════════════════════════════════════════════════════════════════
// GÖRSEL DENETİM 2 (23.10) — 21.10 denetiminin düzeltmeler sonrası YENİDEN koşumu (kurucu: "ekran görüntülerine
// bakarak yazı boyutları, dropdownlar vs ne varsa test et tek tek"). Başsız Chrome + CDP; hiçbir kayıt yazılmaz
// (yalnız açılır listeler açılıp Escape ile kapatılır; "Kaydet" hiç tıklanmaz). Arayüz modu (Basit/Pro) sunucuda
// PATCH /kullanicilar/ben ile değiştirilir ve bitişte başlangıç değerine DÖNDÜRÜLÜR; tema yalnız tarayıcı önbelleği.
// Kullanım: node denetim-2.mjs <sahipVekil> <tezgahtarVekil>
// Çıktı: ham/*.jsonl · foto/matris/*.png|jpg · foto/acilir/* · foto/klavye/*
// ═══════════════════════════════════════════════════════════════════════════
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
import { readFileSync, writeFileSync, appendFileSync, mkdirSync } from 'node:fs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, '');
for (const d of ['ham', 'foto/matris', 'foto/acilir', 'foto/klavye']) mkdirSync(`${S}/${d}`, { recursive: true });
const [SAHIP, TEZ] = process.argv.slice(2); const bek = ms => new Promise(r => setTimeout(r, ms));
const OLC = readFileSync('/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/sikisma-olc.js', 'utf8');
const kaydet = (dosya, o) => appendFileSync(`${S}/ham/${dosya}.jsonl`, JSON.stringify(o) + '\n');
const ad = r => (r.replace(/^\//, '').replace(/\W+/g, '_') || 'kok');

const ROTALAR = process.env.ROTA ? process.env.ROTA.split(',') : ['/', '/islemler', '/cariler', '/kasalar', '/ekranlar', '/urunler', '/urunler/agac', '/urunler/etiketler', '/emanet', '/kur',
  '/pos', '/pos/tahsilatlar', '/vezne', '/gise', '/banka', '/gider', '/hurda', '/faturalar', '/e-belgeler', '/gider-pusulalari',
  '/raporlar', '/raporlar/kar-zarar', '/raporlar/vade-takibi', '/raporlar/islem-defteri', '/raporlar/satis-analizi', '/raporlar/kasa',
  '/fiyatlandirma', '/sayim', '/satin-alma', '/irsaliyeler', '/kullanicilar', '/yetkiler', '/isletme', '/ayarlar', '/kilavuz', '/uyum',
  '/kvkk', '/denetim-izi', '/veri-disa-aktarim', '/veri-ice-aktarim', '/kurulum', '/bildirimler', '/evraklar', '/evraklar/takvim',
  '/siparisler', '/yedekler', '/vitrin', '/vitrin/ekran', '/yasal-sinirlar'];

// ── Sayfa içi ölçerler (tek seferde enjekte) ───────────────────────────────
const EK = `
window.__cls = []; new PerformanceObserver(l => { for (const e of l.getEntries()) if (!e.hadRecentInput) window.__cls.push(e.value); }).observe({ type: 'layout-shift', buffered: true });
window.__gorunur = e => { const r = e.getBoundingClientRect(); if (r.width < 1 || r.height < 1) return false; const s = getComputedStyle(e); return s.visibility !== 'hidden' && s.display !== 'none' && +s.opacity > 0.05; };
window.__renk = c => { const m = c.match(/[\\d.]+/g); if (!m) return null; const v = m.slice(0, 3).map(Number); const a = m[3] !== undefined ? +m[3] : 1; return { v, a }; };
window.__zemin = e => { let x = e; while (x && x !== document.documentElement) { const c = __renk(getComputedStyle(x).backgroundColor); if (c && c.a > 0.9) return c.v; x = x.parentElement; } const h = __renk(getComputedStyle(document.body).backgroundColor); return h && h.a > 0 ? h.v : [255, 255, 255]; };
window.__L = v => { const m = v.map(x => { x /= 255; return x <= .03928 ? x / 12.92 : ((x + .055) / 1.055) ** 2.4; }); return .2126 * m[0] + .7152 * m[1] + .0722 * m[2]; };
window.__K = (a, b) => { const x = __L(a), y = __L(b); return (Math.max(x, y) + .05) / (Math.min(x, y) + .05); };
window.__metinler = () => [...document.querySelectorAll('body *')].filter(e => __gorunur(e) && [...e.childNodes].some(n => n.nodeType === 3 && n.textContent.trim()));
window.__tipo = () => {
  const OLCEK = new Set([12, 14, 16, 18, 22, 28, 40]); const KAHRAMAN = /yz-ince|yildiz-bugun-rakam|yildiz-bugun-kurus|yz-ozet-rakam|yz-sayim-rakam|yz-tezgah-tam|yz-tezgah-kurus|yz-tezgah-fark|panel-soru-sayi|stat-deger|amb|yz-sayi/;
  const boy = {}, disi = {}, agirlik = {}; let n = 0;
  for (const e of __metinler()) { const s = getComputedStyle(e); const fs = Math.round(parseFloat(s.fontSize) * 10) / 10; n++; boy[fs] = (boy[fs] || 0) + 1; agirlik[s.fontWeight] = (agirlik[s.fontWeight] || 0) + 1;
    if (!OLCEK.has(fs) && !KAHRAMAN.test(e.className) && !e.closest('svg') && !e.closest('.belge')) { const k = fs + ' ' + e.tagName.toLowerCase() + '.' + String(e.className).split(' ')[0]; disi[k] = (disi[k] || 0) + 1; } }
  const h1 = [...document.querySelectorAll('h1')].map(h => ({ g: __gorunur(h), fs: getComputedStyle(h).fontSize, fw: getComputedStyle(h).fontWeight, t: h.textContent.trim().slice(0, 40) }));
  return { n, boySayisi: Object.keys(boy).length, boy, agirlik, olcekDisi: Object.entries(disi).sort((a, b) => b[1] - a[1]).slice(0, 8), h1 };
};
window.__kontrast = () => { const ih = []; for (const e of __metinler()) { const s = getComputedStyle(e); const c = __renk(s.color); if (!c) continue; const fs = parseFloat(s.fontSize), b = +s.fontWeight >= 700; const esik = fs >= 24 || (fs >= 18.66 && b) ? 3 : 4.5; const k = __K(c.v, __zemin(e)); if (k < esik) ih.push({ k: +k.toFixed(2), esik, t: e.textContent.trim().slice(0, 30), c: String(e.className).split(' ')[0] }); } return { sayi: ih.length, ornek: ih.slice(0, 5) }; };
window.__ciftId = () => { const c = {}; document.querySelectorAll('[id]').forEach(e => c[e.id] = (c[e.id] || 0) + 1); return Object.keys(c).filter(k => c[k] > 1); };
window.__dugmeBoy = () => { const k = [...document.querySelectorAll('button, a[href], [role=button]')].filter(__gorunur).filter(e => { const r = e.getBoundingClientRect(); return (r.width < 24 || r.height < 24) && !e.closest('p, td, li'); }); return { sayi: k.length, ornek: k.slice(0, 4).map(e => (e.id || e.className || e.tagName).toString().slice(0, 30) + ' ' + Math.round(e.getBoundingClientRect().width) + '×' + Math.round(e.getBoundingClientRect().height)) }; };
window.__ozet = async () => { const x = await window.__olcTam(); return { ...x, cls: +window.__cls.reduce((a, b) => a + b, 0).toFixed(3), tipo: __tipo(), kontrast: __kontrast(), ciftId: __ciftId(), kucukHedef: __dugmeBoy(), sayfaBoy: document.documentElement.scrollHeight }; };
`;
const enjekte = async b => { await b.ev(OLC + '; 1'); await b.ev(EK + '; 1'); };
const git = async (b, r, ms = 3200) => { await b.ev(`window.__cls = []; Blazor.navigateTo(${JSON.stringify(r)}); 1`); await bek(ms); };

// ── Konsol ve ağ hataları ─────────────────────────────────────────────────
const dinle = b => { const hata = []; b.cmd('Network.enable'); b.cmd('Runtime.enable'); const t = setInterval(() => { for (const d of b.olay.splice(0)) { if (d.method === 'Network.responseReceived' && d.params.response.status >= 400) hata.push('HTTP ' + d.params.response.status + ' ' + d.params.response.url.replace(/^.*\/api\/v1/, '').slice(0, 60)); if (d.method === 'Runtime.exceptionThrown') hata.push('JS ' + (d.params.exceptionDetails.exception?.description || d.params.exceptionDetails.text).slice(0, 80)); if (d.method === 'Runtime.consoleAPICalled' && d.params.type === 'error') hata.push('console ' + d.params.args.map(a => a.value || a.description || '').join(' ').slice(0, 80)); } }, 50); return { hata, dur: () => clearInterval(t), al: () => hata.splice(0) }; };

// ── Mod / tema ────────────────────────────────────────────────────────────
const modOku = b => b.ev(`(async()=>{const t=localStorage.getItem('kuyumcu-access'); const r=await fetch('/api/v1/auth/ben',{headers:{Authorization:'Bearer '+t}}); const j=await r.json(); return {tercih:j.arayuzModu??null, etkin:j.etkinArayuzModu??null}})()`);
const modYaz = async (b, mod) => { await b.ev(`(async()=>{const t=localStorage.getItem('kuyumcu-access'); const r=await fetch('/api/v1/kullanicilar/ben',{method:'PATCH',headers:{Authorization:'Bearer '+t,'Content-Type':'application/json'},body:JSON.stringify({arayuzModu:${JSON.stringify(mod)}})}); return r.status})()`); await b.cmd('Page.reload'); await bek(5000); await enjekte(b); };
const tema = async (b, t) => { await b.ev(`window.kuyumcuTema.uygula(${JSON.stringify(t)}); 1`); await bek(300); };

// ── Dinamik kimlikli rotalar ─────────────────────────────────────────────
const dinamik = async b => b.ev(`(async()=>{const t=localStorage.getItem('kuyumcu-access'); const H={headers:{Authorization:'Bearer '+t}}; const r=[]; try{const s=await (await fetch('/api/v1/siparisler',H)).json(); const L=Array.isArray(s)?s:(s.satirlar||s.siparisler||[]); if(L[0]) r.push('/siparisler/'+L[0].id)}catch{} try{const p=await (await fetch('/api/v1/gider-pusulalari',H)).json(); const L=Array.isArray(p)?p:(p.satirlar||p.pusulalar||[]); if(L[0]) r.push('/gider-pusulalari/'+L[0].id+'/belge')}catch{} try{const f=await (await fetch('/api/v1/faturalar?limit=5',H)).json(); const L=Array.isArray(f)?f:(f.satirlar||f.faturalar||[]); if(L[0]) r.push('/cikti/FATURA/'+L[0].id)}catch{} return r})()`);

// ── Matris: rota × durum ─────────────────────────────────────────────────
let KOK = '';
async function matris(b, kim, rotalar, durumlar, ag) {
  for (const d of durumlar) {
    await b.boyut(d.w, d.h); if (d.mod) await modYaz(b, d.mod); await tema(b, d.tema);
    for (const r of rotalar) {
      ag.al(); let o;
      try { await b.git(KOK + r, 4500); await enjekte(b); o = await b.ev('window.__ozet()'); } catch (e) { o = { hata: String(e).slice(0, 120) }; }
      const png = d.w === 1280 || d.w === 375;
      const dosya = `${S}/foto/matris/${ad(r)}__${kim}_${d.ad}.${png ? 'png' : 'jpg'}`;
      try { const s = await b.cmd('Page.captureScreenshot', png ? { format: 'png', captureBeyondViewport: true } : { format: 'jpeg', quality: 70, captureBeyondViewport: true }); writeFileSync(dosya, Buffer.from(s.data, 'base64')); } catch {}
      kaydet('matris-' + kim, { kim, rota: r, durum: d.ad, w: d.w, zaman: new Date().toISOString(), olc: o, ag: ag.al(), foto: dosya.replace(S + '/', '') });
      console.log(kim, d.ad, r, o.hata ? 'HATA ' + o.hata : `K3 ${o.K3_tasan}/${o.K3_kirik} K9 ${o.K9_binen} Y ${o.yatay} cls ${o.cls} boy ${o.tipo.boySayisi} disi ${o.tipo.olcekDisi.length} kontrast ${o.kontrast.sayi}`);
    }
  }
}

// ── Açılır listeler ──────────────────────────────────────────────────────
const ACILIR = `(async () => {
  const bekle = ms => new Promise(r => setTimeout(r, ms)); const sonuc = [];
  const olc = async (t, katman) => {
    const once = t.querySelector('.secici-metin')?.textContent.trim() ?? ''; const rota = location.pathname;
    t.scrollIntoView({ block: 'center' }); await bekle(150); t.click(); await bekle(700);
    const kap = document.getElementById(t.id + '-liste-kap') || t.closest('.secici-kap')?.querySelector('.secici-liste-kap');
    const acik = !!kap && __gorunur(kap);
    let r = null, tasma = null, ortulu = null, secenek = 0, satirBoy = null, enUzunSatir = 1, kesik = 0, kaydirma = false;
    if (acik) { const k = kap.getBoundingClientRect(); r = [Math.round(k.left), Math.round(k.top), Math.round(k.width), Math.round(k.height)];
      tasma = { sag: Math.max(0, Math.round(k.right - innerWidth)), sol: Math.max(0, Math.round(-k.left)), alt: Math.max(0, Math.round(k.bottom - innerHeight)), ust: Math.max(0, Math.round(-k.top)) };
      const n = [[k.left + 4, k.top + 4], [k.right - 4, k.top + 4], [k.left + k.width / 2, k.top + k.height / 2], [k.left + 4, k.bottom - 4], [k.right - 4, k.bottom - 4]].filter(([x, y]) => x >= 0 && y >= 0 && x < innerWidth && y < innerHeight);
      ortulu = n.filter(([x, y]) => { const u = document.elementFromPoint(x, y); return u && !kap.contains(u) && !t.contains(u); }).length;
      const ops = [...kap.querySelectorAll('[role=option]')]; secenek = ops.length;
      if (ops[0]) { satirBoy = Math.round(ops[0].getBoundingClientRect().height); const lh = parseFloat(getComputedStyle(ops[0]).lineHeight) || 20; enUzunSatir = Math.max(...ops.map(o => Math.round(o.getBoundingClientRect().height / lh))); kesik = ops.filter(o => o.scrollWidth > o.clientWidth + 1).length; }
      kaydirma = kap.scrollHeight > kap.clientHeight + 1; }
    const m = t.querySelector('.secici-metin'); const ms = m ? getComputedStyle(m) : null;
    const g = t.closest('form, .cekmece, .modal-panel, main')?.querySelector('input.girdi, textarea.girdi'); const gs = g ? getComputedStyle(g) : null;
    const h1 = [...document.querySelectorAll('h1')].find(__gorunur); const hs = h1 ? getComputedStyle(h1) : null;
    const ariaAcik = t.getAttribute('aria-expanded');
    if (acik) { t.click(); await bekle(350); }   // tetik yeniden tıklanınca liste kapanır (çekmece açık kalır)
    let hala = kap && __gorunur(kap); if (hala) { t.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true })); await bekle(350); hala = kap && __gorunur(kap); }
    const sonra = t.querySelector('.secici-metin')?.textContent.trim() ?? '';
    sonuc.push({ rota, katman, id: t.id, deger: once.slice(0, 30), acildi: acik, kutu: r, tasma, ortulu, secenek, satirBoy, enUzunSatir, kesik, kaydirma, ariaAcik, ariaKapali: t.getAttribute('aria-expanded'), kapandi: !hala, degerKorundu: once === sonra, rotaAyni: location.pathname === rota,
      seciliFs: ms && parseFloat(ms.fontSize), seciliFw: ms && ms.fontWeight, girdiFs: gs && parseFloat(gs.fontSize), h1Fs: hs && parseFloat(hs.fontSize), baslikGibi: !!(ms && hs && Math.abs(parseFloat(ms.fontSize) - parseFloat(hs.fontSize)) < 1) });
  };
  const tetikler = () => [...document.querySelectorAll('.secici-tetik')].filter(e => __gorunur(e) && !e.disabled);
  for (const t of tetikler()) { try { await olc(t, 'sayfa'); } catch (e) { sonuc.push({ rota: location.pathname, id: t.id, hata: String(e).slice(0, 80) }); } }
  // Çekmece/pencere açıcıları: "+ …", "Yeni …", "Düzenle", "Elle gir" — sırayla aç, içindeki seçicileri ölç, kapat.
  const acicilar = [...document.querySelectorAll('main button, main a.yz-b1, main a.yz-ekle')].filter(e => __gorunur(e) && /^(\\+ |Yeni |Elle gir|Düzenle$|Hareket ekle|Tahsilat al|Ödeme al)/.test(e.innerText.trim())).slice(0, 4);
  for (const a of acicilar) { const ad = a.innerText.trim().slice(0, 24); const onceki = new Set(tetikler()); const once = onceki.size; try { a.click(); await bekle(1500);
      const ic = tetikler().filter(t => !onceki.has(t)); for (const t of ic) { try { await olc(t, 'acici:' + ad); } catch (e) { sonuc.push({ rota: location.pathname, id: t.id, katman: ad, hata: String(e).slice(0, 80) }); } }
      if (!ic.length) sonuc.push({ rota: location.pathname, katman: 'acici:' + ad, secicisiz: true, yeniTetik: tetikler().length - once });
      const kapat = [...document.querySelectorAll('.cekmece .cekmece-kapat, .modal-panel [id$=kapat], .modal-panel .modal-kapat, [role=dialog] button[aria-label*="apat"], button[id$="-vazgec"], button[id$="-iptal"]')].find(__gorunur); if (kapat) kapat.click(); else document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true })); await bekle(800);
      if (tetikler().some(t => !onceki.has(t))) { document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true })); await bekle(600); }
    } catch (e) { sonuc.push({ rota: location.pathname, katman: 'acici:' + ad, hata: String(e).slice(0, 80) }); } }
  return sonuc;
})()`;
async function acilir(b, kim, rotalar, durumlar) {
  for (const d of durumlar) { await b.boyut(d.w, d.h); if (d.mod) await modYaz(b, d.mod); await tema(b, d.tema);
    for (const r of rotalar) { try { await git(b, r); const s = await b.ev(ACILIR); for (const x of s) kaydet('acilir-' + kim, { kim, durum: d.ad, ...x });
        const acik = s.filter(x => x.acildi).length; console.log('acilir', kim, d.ad, r, `${acik}/${s.length}`, s.filter(x => x.tasma && (x.tasma.alt || x.tasma.sag || x.tasma.sol || x.tasma.ust)).length ? 'TAŞMA' : '', s.filter(x => x.ortulu).length ? 'ÖRTÜLÜ' : '', s.filter(x => x.baslikGibi).length ? 'BAŞLIK-GİBİ' : '');
        // ilk seçiciyi açık fotoğrafla
        const ilk = await b.ev(`(()=>{const t=[...document.querySelectorAll('.secici-tetik')].find(e=>__gorunur(e)&&!e.disabled); if(!t) return false; t.scrollIntoView({block:'center'}); t.click(); return true})()`); if (ilk) { await bek(600); await b.foto(`${S}/foto/acilir/${ad(r)}__${kim}_${d.ad}.png`); await b.ev(`document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true})); 1`); }
      } catch (e) { kaydet('acilir-' + kim, { kim, durum: d.ad, rota: r, hata: String(e).slice(0, 120) }); } } }
}

// ── Klavye turu ──────────────────────────────────────────────────────────
async function klavye(b, kim, rotalar) {
  await b.boyut(1280, 900); await b.cmd('Emulation.setFocusEmulationEnabled', { enabled: true });
  const tus = async () => { for (const t of ['keyDown', 'keyUp']) await b.cmd('Input.dispatchKeyEvent', { type: t, key: 'Tab', code: 'Tab', windowsVirtualKeyCode: 9 }); await bek(90); };
  for (const r of rotalar) { try { await b.git(KOK + r, 4000); await enjekte(b); const sira = []; let gorunmez = 0, ilk = null;
      await b.ev(`window.__snapZ = x => { const s = getComputedStyle(x); return s.outlineStyle + s.outlineWidth + s.outlineColor + '|' + s.boxShadow + '|' + s.borderColor + s.borderBottomColor + '|' + s.backgroundColor + '|' + s.color + '|' + s.textDecorationLine; }; window.__snap = e => [e, e.parentElement, e.parentElement?.parentElement].filter(Boolean).map(__snapZ).join('#'); window.__onceki = null; 1`);
      for (let i = 0; i < 30; i++) { await tus();
        // Odak görünürlüğü: ÖNCEKİ odaklı öğenin odaklıyken alınan stili ile şimdi (odak gidince) stili karşılaştırılır — gerçek Tab, programatik focus() yok.
        const o = await b.ev(`(()=>{const e=document.activeElement; let onceFark=null; if(window.__onceki){const p=window.__onceki; onceFark={id:p.id, fark:p.odakli!==__snap(p.e), video:p.e.tagName==='VIDEO'}} if(!e||e===document.body){window.__onceki=null; return {son:true, onceFark}} const q=e.getBoundingClientRect(); window.__onceki={e, id:(e.id||String(e.className).split(' ')[0]||e.tagName).slice(0,28), odakli:__snap(e)}; return {id:window.__onceki.id, ekranda:q.bottom>0&&q.top<innerHeight, onceFark}})()`);
        if (o.onceFark) { if (!o.onceFark.fark && !o.onceFark.video) { gorunmez++; sira[sira.length - 1] += '!'; } }
        if (o.son) break; if (i === 0) ilk = o.id; sira.push(o.id); }
      kaydet('klavye-' + kim, { kim, rota: r, ilk, adim: sira.length, gorunmez, sira }); console.log('klavye', r, 'ilk', ilk, 'görünmez', gorunmez + '/' + sira.length);
      if (gorunmez) await b.foto(`${S}/foto/klavye/${ad(r)}__${kim}.png`); } catch (e) { kaydet('klavye-' + kim, { kim, rota: r, hata: String(e).slice(0, 100) }); } }
}

// ── Koşu ─────────────────────────────────────────────────────────────────
const DURUM_SAHIP = [{ ad: 'PRO_aydinlik_1280', w: 1280, h: 800, mod: 'PROFESYONEL', tema: 'aydinlik' }, { ad: 'BASIT_karanlik_375', w: 375, h: 812, mod: 'BASIT', tema: 'karanlik' },
  { ad: 'PRO_aydinlik_1024', w: 1024, h: 768, mod: 'PROFESYONEL', tema: 'aydinlik' }, { ad: 'BASIT_aydinlik_320', w: 320, h: 700, mod: 'BASIT', tema: 'aydinlik' }, { ad: 'PRO_karanlik_812x375', w: 812, h: 375, mod: 'PROFESYONEL', tema: 'karanlik' }];
KOK = SAHIP; let b = await baslat(9995, S + '/profil-sahip'); await b.git(SAHIP + '/__giris', 6000); await enjekte(b);
const basMod = await modOku(b); kaydet('tercih', { an: 'baslangic', kim: 'sahip', ...basMod }); console.log('sahip başlangıç modu', JSON.stringify(basMod));
const ag = dinle(b);
const ekRota = await dinamik(b); console.log('dinamik rotalar', ekRota);
const R = process.env.ROTA ? ROTALAR : [...ROTALAR, ...ekRota];
const HIZLI = !!process.env.HIZLI;
await matris(b, 'sahip', R, HIZLI ? DURUM_SAHIP.slice(0, 2) : DURUM_SAHIP, ag);
await acilir(b, 'sahip', R, [{ ad: 'PRO_aydinlik_1280', w: 1280, h: 800, mod: 'PROFESYONEL', tema: 'aydinlik' }, { ad: 'BASIT_karanlik_375', w: 375, h: 812, mod: 'BASIT', tema: 'karanlik' }]);
await modYaz(b, 'PROFESYONEL'); await tema(b, 'aydinlik'); await klavye(b, 'sahip', R);
// EN dili (yalnız 1280 Pro açık; localStorage anahtarı bitişte silinir)
await b.ev(`localStorage.setItem('kuyumcu-dil','en'); 1`); await b.cmd('Page.reload'); await bek(5000); await enjekte(b);
for (const r of R) { try { await git(b, r); const o = await b.ev(`(()=>{const t=__metinler().map(e=>e.textContent.trim()).filter(t=>t.length>3); const tr=t.filter(x=>/[çğışöüÇĞİŞÖÜ]/.test(x)&&!/\\d/.test(x)).slice(0,6); const x=window.__olc(); return {lang:document.documentElement.lang, h1:document.querySelector('h1')?.textContent.trim().slice(0,30), tasan:x.K3_tasan, yatay:x.yatay, turkce:tr.length, ornek:tr}})()`); kaydet('dil-sahip', { rota: r, ...o }); const s = await b.cmd('Page.captureScreenshot', { format: 'jpeg', quality: 60 }); writeFileSync(`${S}/foto/matris/${ad(r)}__sahip_EN_1280.jpg`, Buffer.from(s.data, 'base64')); console.log('EN', r, o.h1, 'türkçe', o.turkce, 'taşan', o.tasan); } catch (e) { kaydet('dil-sahip', { rota: r, hata: String(e).slice(0, 80) }); } }
await b.ev(`localStorage.removeItem('kuyumcu-dil'); 1`); await b.cmd('Page.reload'); await bek(4000);
// Geri alma
await modYaz(b, basMod.tercih); await tema(b, 'aydinlik'); const sonMod = await modOku(b); kaydet('tercih', { an: 'bitis', kim: 'sahip', ...sonMod }); console.log('sahip bitiş modu', JSON.stringify(sonMod), 'dil', await b.ev(`localStorage.getItem('kuyumcu-dil')`));
ag.dur(); b.kapat();

// Tezgâhtar: modu DEĞİŞTİRİLMEZ (devralınmış); 1280 açık + 375 koyu matris.
KOK = TEZ; b = await baslat(9996, S + '/profil-tez'); await b.git(TEZ + '/__giris', 6000); await enjekte(b); const ag2 = dinle(b);
await matris(b, 'tezgahtar', R, [{ ad: 'KORU_aydinlik_1280', w: 1280, h: 800, tema: 'aydinlik' }, { ad: 'KORU_karanlik_375', w: 375, h: 812, tema: 'karanlik' }], ag2);
await tema(b, 'aydinlik'); ag2.dur(); b.kapat();
console.log('BİTTİ');
