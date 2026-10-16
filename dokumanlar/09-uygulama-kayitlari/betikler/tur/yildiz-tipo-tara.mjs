#!/usr/bin/env node
// ═══════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-TIPO — Yıldız TAM TİPOGRAFİ/ÖĞE TARAMASI (kalıcı araç)
//
// Kurucu (09.10): "yazı tipleri özellikle dropdownlarda eski temalardan kalma boyutları
// bile farklı geliyor tam bir kontrol sağlansın" · "birebir tasarım neyse aynısını".
//
// Yöntem: Yıldız seçili kiracıda menüdeki TÜM ekranlar (Tüm ekranlar sayfasındaki karolar)
// × genişlik × koyu/açık. Her ekranda görünen HER metin düğümü (+ girdi/select/textarea
// değeri ve yer tutucusu) için getComputedStyle font-family/size/weight/letter-spacing/color
// toplanır; ardından açılabilen her katman AÇILIR (Seçici paneli, '+ ekle' katlanırları,
// details, çekmece, modal, menü, bildirim — ekstra.js __acicilar: kayıt/silme/gönderme ASLA)
// ve YENİ görünen düğümler aynı kurala tabi tutulur.
//
// KURAL (tuval = tek doğruluk; ham/yildiz-tuval/kaynak/*.dc.html'den çıkarılan izinli küme):
//   aile        : ilk aile "Inter"
//   boy (px)    : 11 12 13 14 15 16 17 18 19 20 22 28 36 40 52 56 64 72 104 112
//   ağırlık     : 200 300 400 500
//   harf aralığı: (em) 0 −.005 −.01 −.035 .02 .04 .06 .08 .1 .14 .18 .22 .3  (±0,0006em — hesaplanmış px tam; −0,011em gövde aralığı −0,01em sayılmasın)
//   renk        : yalnız Yıldız belirteçleri --m --s --p --h --z --iyi --kotu (hesaplanmış rgb)
//   zemin       : metnin etkin zemini saydam ya da --yuzey-govde (--z); --metin (--m) dolgu yalnız b1 hapında (10.10)
//   Muaf: amblem (#yildiz-marka — ayrı paket, tuval Amblem_sol birebir), kâğıt (.belge*, .urun-etiket*).
//   Native <select>: seçenek listesini tarayıcı çizer → select ÖĞESİ ayrı sütunda ölçülür (native=1).
//
// KULLANIM:
//   node yildiz-tipo-tara.mjs --mod BASIT|PRO [--cdp 9981] [--profil <dizin>] [--vekil http://localhost:5189]
//        [--cikti <dizin>] [--genislik 1440,375] [--temalar karanlik,aydinlik] [--rotalar /a,/b] [--katman 1]
//        [--acici-sinir 20]
// ÇIKTI: <cikti>/tipo-<MOD>.jsonl (bulgu satırları) + özet satırı; çıkış kodu kural dışı > 0 ise 1.
// ═══════════════════════════════════════════════════════════════════════════
import { readFileSync, mkdirSync, appendFileSync, writeFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { baslat } from './cdp.mjs';

const BU = dirname(fileURLToPath(import.meta.url));
const arg = (ad, v = null) => { const i = process.argv.indexOf(ad); return i > 0 && process.argv[i + 1] ? process.argv[i + 1] : v; };
const MOD = (arg('--mod', 'BASIT')).toUpperCase();
const CDP = Number(arg('--cdp', '9981'));
const PROFIL = arg('--profil', '/private/tmp/kuyumcu-serit/yildiz-tipo-profil');
const VEKIL = arg('--vekil', 'http://localhost:5189');
const CIKTI = arg('--cikti', '/private/tmp/kuyumcu-serit/yildiz-tipo'); mkdirSync(CIKTI, { recursive: true });
const GEN = arg('--genislik', '1440,375').split(',').map(Number);
const TEMALAR = arg('--temalar', 'karanlik,aydinlik').split(',');
const ROT_ARG = arg('--rotalar', null);
const KATMAN = arg('--katman', '1') === '1';
const ACICI_SINIR = Number(arg('--acici-sinir', '20'));
const BOY = { 1440: 900, 1280: 800, 375: 812 };
const EK = readFileSync(join(BU, 'ekstra.js'), 'utf8');
const bek = ms => new Promise(r => setTimeout(r, ms));
const DOSYA = join(CIKTI, `tipo-${MOD}.jsonl`); writeFileSync(DOSYA, '');
const yaz = o => appendFileSync(DOSYA, JSON.stringify(o) + '\n');

// Sayfaya kurulan tarayıcı tarafı ölçer. window.__tipo(sadeceYeni) → {toplam, bulgular[]}
const OLCER = `(() => {
  const BOYLAR = [11,12,13,14,15,16,17,18,19,20,22,28,36,40,52,56,64,72,104,112];
  const AGIRLIK = ['200','300','400','500'];
  const ARALIK = [0,-0.005,-0.01,-0.035,0.02,0.04,0.06,0.08,0.1,0.14,0.18,0.22,0.3];
  // Kısa tuval adları (--m --s --p --h --z --iyi --kotu) :root'ta semantik adlarla eşlenir (not § 3): --metin --metin-soluk --kenarlik --ayrac --yuzey-govde --basari --tehlike.
  const BELIRTEC = ['--metin','--metin-soluk','--kenarlik','--ayrac','--yuzey-govde','--basari','--tehlike'];
  const gorunur = e => { for (let p = e; p && p.nodeType === 1; p = p.parentElement) { const s = getComputedStyle(p); if (s.display === 'none' || s.visibility === 'hidden' || +s.opacity === 0) return false; } const r = e.getBoundingClientRect(); return r.width > 1 && r.height > 1; };
  const muaf = e => !!e.closest('#yildiz-marka, .belge, [class^="belge-"], [class*=" belge-"], .urun-etiket, [class^="urun-etiket"], .sr-only, .gorsel-gizli, .ekran-okuyucu, #blazor-error-ui');
  // b1 = tuval tek hap birincil (dolgu --metin, yazı --yuzey-govde). Sekme/segment bu sınıfları taşısa bile b1 değildir.
  const B1 = '.dugme-birincil, .btn-birincil, .yildiz-b1, [class~="b1"], [class*="-b1 "], [class$="-b1"]';
  const SEKME = '.sekme, .sekmeler-sekme, .musteri-segment-oge, [role="tab"]';
  const saydam = c => c === 'transparent' || /rgba\\([^)]*,\\s*0\\)$/.test(c);
  const zemin = e => { for (let p = e; p && p.nodeType === 1; p = p.parentElement) { const c = getComputedStyle(p).backgroundColor; if (!saydam(c)) return { renk: c, sahip: p }; } return null; };
  const tokenRgb = () => { const d = document.createElement('span'); document.body.appendChild(d); const m = {}; for (const t of BELIRTEC) { d.style.color = 'var(' + t + ')'; m[getComputedStyle(d).color] = t; } d.remove(); return m; };
  const yol = e => { const p = []; for (let x = e; x && x !== document.body && p.length < 4; x = x.parentElement) { let s = x.tagName.toLowerCase(); if (x.id) { s += '#' + x.id; p.unshift(s); break; } const c = typeof x.className === 'string' ? x.className.trim().split(/\\s+/).filter(Boolean)[0] : (x.className && x.className.baseVal ? x.className.baseVal.split(' ')[0] : ''); if (c) s += '.' + c; p.unshift(s); } return p.join(' > '); };
  // Geçişler ölçüme karışmasın (tema değişince renk 0,2 sn geçişte ara değer verir): yalnız ölçüm sekmesinde kapatılır.
  if (!document.getElementById('tipo-gecis-yok')) { const st = document.createElement('style'); st.id = 'tipo-gecis-yok'; st.textContent = '*,*::before,*::after,*::placeholder{transition:none!important}'; document.head.appendChild(st); }
  window.__tipoGor = window.__tipoGor || new WeakSet();
  window.__tipoSifirla = () => { window.__tipoGor = new WeakSet(); return 1; };
  window.__tipo = (sadeceYeni, isaretle) => {
    const tok = tokenRgb();
    const ogeler = new Set();
    const w = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
    for (let n = w.nextNode(); n; n = w.nextNode()) { if (n.textContent.trim() && n.parentElement) ogeler.add(n.parentElement); }
    document.querySelectorAll('input:not([type=hidden]):not([type=checkbox]):not([type=radio]):not([type=range]):not([type=color]):not([type=file]), select, textarea').forEach(e => ogeler.add(e));
    const bulgular = []; let toplam = 0;
    for (const e of ogeler) {
      if (e.closest('svg') && !e.matches('svg text, svg tspan')) {}
      if (['SCRIPT','STYLE','NOSCRIPT','TEMPLATE','TITLE'].includes(e.tagName)) continue;
      if (muaf(e) || !gorunur(e)) continue;
      if (sadeceYeni && window.__tipoGor.has(e)) continue;
      if (isaretle) window.__tipoGor.add(e);
      toplam++;
      const s = getComputedStyle(e);
      const fs = parseFloat(s.fontSize); const fw = String(s.fontWeight);
      const ls = s.letterSpacing === 'normal' ? 0 : parseFloat(s.letterSpacing);
      const em = fs ? ls / fs : 0;
      const aile = s.fontFamily.split(',')[0].trim().replace(/^["']|["']$/g, '');
      const native = e.tagName === 'SELECT';
      const hatalar = [];
      if (aile !== 'Inter') hatalar.push(['aile', s.fontFamily.slice(0, 60)]);
      if (!BOYLAR.some(b => Math.abs(b - fs) < 0.26)) hatalar.push(['boy', s.fontSize]);
      if (!AGIRLIK.includes(fw)) hatalar.push(['agirlik', fw]);
      // letter-spacing em ise HESAPLANMIŞ px olarak kalıtılır (tuvalde de: 112 px rakamın −.035em aralığı yarı boy kuruşa −3,92 px iner) → ata boyuna göre de denenir.
      const atadanKalit = () => { for (let p = e.parentElement; p && p !== document.documentElement; p = p.parentElement) { const ps = getComputedStyle(p); const pl = ps.letterSpacing === "normal" ? 0 : parseFloat(ps.letterSpacing); if (Math.abs(pl - ls) > 0.01) return false; const pf = parseFloat(ps.fontSize); if (ARALIK.some(a => Math.abs(a - pl / pf) < 0.0006)) return true; } return false; };
      if (!ARALIK.some(a => Math.abs(a - em) < 0.0006) && !atadanKalit()) hatalar.push(['aralik', (Math.round(em * 1000) / 1000) + 'em (' + s.letterSpacing + ')']);
      if (!tok[s.color]) hatalar.push(['renk', s.color]);
      // ZEMİN (doğrulayıcı bulgusu 10.10: koyu Yıldız'da seçili sekme eski temanın dolu bloğu — yazı #000 = --yuzey-govde
      // olduğu için 'renk'ten geçiyordu): metnin ETKİN zemini (kendisi ya da ilk saydam olmayan atası) saydam/--yuzey-govde
      // olmalı; --metin dolgu yalnız b1 hapında (B1) serbest. Sekme/segment b1 SAYILMAZ.
      const z = zemin(e);
      if (z && tok[z.renk] !== '--yuzey-govde' && !(tok[z.renk] === '--metin' && z.sahip.matches(B1) && !z.sahip.matches(SEKME))) hatalar.push(['zemin', z.renk + ' @' + yol(z.sahip)]);
      if ((e.tagName === 'INPUT' || e.tagName === 'TEXTAREA') && e.placeholder && !e.value) { const ps = getComputedStyle(e, '::placeholder'); if (!tok[ps.color]) hatalar.push(['renk-yer-tutucu', ps.color]); }
      if (hatalar.length) bulgular.push({ s: yol(e), m: (e.tagName === 'INPUT' || e.tagName === 'TEXTAREA' ? (e.value || e.placeholder || '') : e.tagName === 'SELECT' ? (e.options[e.selectedIndex]?.text || '') : [...e.childNodes].filter(x => x.nodeType === 3).map(x => x.textContent).join('')).trim().replace(/\\s+/g, ' ').slice(0, 40), native: native ? 1 : 0, h: hatalar });
    }
    return { toplam, bulgular };
  };
  return 'tipo-kuruldu';
})()`;

const b = await baslat(CDP, PROFIL);
// Önbellek kapalı: statik CSS Cache-Control taşımaz (sezgisel önbellek) → düzeltmeden sonraki taramada eski CSS ölçülmesin.
await b.cmd('Network.enable'); await b.cmd('Network.setCacheDisabled', { cacheDisabled: true });
const kur = async () => { await b.ev(EK); await b.ev(OLCER); };
const temaUygula = t => b.ev(`window.kuyumcuTema.uygula(${JSON.stringify(t)}); document.documentElement.setAttribute('data-tema', ${JSON.stringify(t)}); 1`);
const yukBekle = async () => { for (let i = 0; i < 40; i++) { const y = await b.ev(`[...document.querySelectorAll('.donen-halka, .yukleniyor-kutu, [aria-busy=true]')].some(e => e.getBoundingClientRect().width > 0)`); if (!y) break; await bek(400); } await bek(350); };
const ozet = { mod: MOD, sayfa: 0, katman: 0, dugum: 0, kuralDisi: 0, nativeSelect: 0, nativeSelectKuralDisi: 0, ozellik: {}, hata: 0 };
let rotalar = [];
try {
  await b.boyut(1440, 900);
  let gir = false;
  for (let d = 0; d < 5 && !gir; d++) { await b.git(VEKIL + '/__giris', 5000); await b.git(VEKIL + '/', 5000); gir = await b.ev(`document.documentElement.getAttribute('data-sema')==='yildiz' && !!document.querySelector('#yildiz-kullanici-ac')`).catch(() => false); if (!gir) await bek(65000); }   // giriş sınırı 5/dk (429)
  if (!gir) { console.log('GIRIS/SEMA-BASARISIZ'); process.exit(3); }
  // Mod: Yıldız kullanıcı menüsünden seç ve doğrula (sunucuya yazılır).
  await b.ev(`document.querySelector('#yildiz-kullanici-ac').click(); 1`); await bek(500);
  await b.ev(`document.querySelector('#yildiz-mod-${MOD === 'PRO' ? 'profesyonel' : 'basit'}')?.click(); 1`); await bek(2500);
  await b.git(VEKIL + '/', 5000);
  const modGercek = await b.ev(`document.documentElement.getAttribute('data-mod')`);
  console.log('mod', MOD, '→ data-mod', modGercek);
  // Ekran listesi: Tüm ekranlar karoları (menüdeki TÜM ekranlar).
  if (ROT_ARG) rotalar = ROT_ARG.split(',');
  else {
    await b.ev(`Blazor.navigateTo('/ekranlar'); 1`); await bek(2500);
    rotalar = await b.ev(`[...new Set([...document.querySelectorAll('#yildiz-tum a.yz-kare[href]')].map(a => new URL(a.href).pathname))]`);
    rotalar = ['/ekranlar', ...rotalar.filter(r => r !== '/ekranlar')];
  }
  console.log('rota', rotalar.length);
  yaz({ tip: 'kosum', MOD, modGercek, rotalar });
  for (const w of GEN) {
    await b.boyut(w, BOY[w] || 900);
    await b.git(VEKIL + '/', 5000); await kur();
    for (const rota of rotalar) {
      try {
        await b.ev(`Blazor.navigateTo(${JSON.stringify(rota)}); 1`); await bek(1200); await yukBekle();
        await kur(); await b.ev('window.scrollTo(0,0); window.__tipoSifirla()');
        const yolG = await b.ev('location.pathname');
        // Sayfa: her tema
        for (const t of TEMALAR) {
          await temaUygula(t); await bek(150);
          const r = await b.ev(`window.__tipo(false, ${t === TEMALAR[0]})`);
          ozet.sayfa++; ozet.dugum += r.toplam;
          for (const f of r.bulgular) { ozet.kuralDisi++; if (f.native) ozet.nativeSelectKuralDisi++; for (const [o] of f.h) ozet.ozellik[o] = (ozet.ozellik[o] || 0) + 1; yaz({ tip: 'bulgu', yer: 'sayfa', rota, yolG, w, tema: t, ...f }); }
        }
        // Yan ölçü (bulgu sayılmaz): sayfa yatay kayıyor mu, satır içi kırpılan yazı (scrollWidth > clientWidth, overflow visible değil) var mı.
        const yan = await b.ev(`({yatay: document.documentElement.scrollWidth > innerWidth + 1, kesik: [...document.querySelectorAll(".ana-icerik *")].filter(e => e.children.length === 0 && (e.innerText||"").trim() && e.getBoundingClientRect().width > 0 && e.scrollWidth > e.clientWidth + 1 && getComputedStyle(e).overflowX !== "visible").length})`);
        ozet.yatay = (ozet.yatay || 0) + (yan.yatay ? 1 : 0); ozet.kesik = (ozet.kesik || 0) + yan.kesik; yaz({ tip: "yan", rota, w, ...yan });
        ozet.nativeSelect += await b.ev(`[...document.querySelectorAll('select')].filter(e=>e.getBoundingClientRect().width>1).length`);
        await temaUygula(TEMALAR[0]);
        if (!KATMAN) continue;
        const acicilar = (await b.ev('window.__acicilar()')).slice(0, ACICI_SINIR);
        for (const a of acicilar) {
          const var_ = await b.ev(`(()=>{const e=document.querySelector('[data-tur-acici="${a.i}"]'); if(!e||e.getBoundingClientRect().width===0) return false; e.scrollIntoView({block:'center'}); e.click(); return true})()`);
          if (!var_) continue;
          await bek(600); await yukBekle();
          for (const t of TEMALAR) {
            await temaUygula(t); await bek(120);
            const r = await b.ev(`window.__tipo(true, false)`);
            if (r.toplam) { ozet.katman++; ozet.dugum += r.toplam; }
            for (const f of r.bulgular) { ozet.kuralDisi++; if (f.native) ozet.nativeSelectKuralDisi++; for (const [o] of f.h) ozet.ozellik[o] = (ozet.ozellik[o] || 0) + 1; yaz({ tip: 'bulgu', yer: 'katman', acici: a.ad, rota, w, tema: t, ...f }); }
          }
          await temaUygula(TEMALAR[0]);
          await b.ev('window.__kapatHepsi()'); await bek(250);
          const yol2 = await b.ev('location.pathname');
          if (yol2 !== yolG) { await b.ev(`Blazor.navigateTo(${JSON.stringify(rota)}); 1`); await bek(1200); await yukBekle(); await kur(); await b.ev('window.__acicilar(); 1'); }
        }
      } catch (e) { ozet.hata++; yaz({ tip: 'hata', rota, w, hata: String(e).slice(0, 200) }); try { await b.git(VEKIL + rota, 5000); await kur(); } catch {} }
    }
  }
} finally { b.kapat(); }
yaz({ tip: 'ozet', ...ozet });
console.log(`YILDIZ-TIPO ${MOD}: ${rotalar.length} ekran · ${GEN.join('+')} · ${TEMALAR.join('+')} · sayfa ölçümü ${ozet.sayfa} · katman ${ozet.katman} · düğüm ${ozet.dugum} · KURAL DIŞI ${ozet.kuralDisi} (native select öğesi ${ozet.nativeSelectKuralDisi}/${ozet.nativeSelect}) · ${JSON.stringify(ozet.ozellik)} · hata ${ozet.hata} · yatay-kayan sayfa ${ozet.yatay || 0} · kesik yazı ${ozet.kesik || 0}`);
process.exit(ozet.kuralDisi > 0 ? 1 : 0);
