#!/usr/bin/env node
// ═══════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-4A — TUVAL ↔ UYGULAMA BİREBİR KARŞILAŞTIRMA ARACI (kalıcı)
//
// Kurucu (09.10): "birebir tasarım neyse aynısını istisnasız olarak istiyorum".
// Bu araç "gözle benziyor" yerine SAYIYLA karşılaştırır:
//   (1) Tuval (*.dc.html) statik sayfaya çevrilir: support.js ve x-dc betiği
//       atılır, <helmet> içeriği <head>'e taşınır, Google Fonts bağlantısı
//       uygulamanın KENDİ Inter dosyalarıyla değiştirilir (aynı yazı tipiyle
//       ölçülsün diye; uygulama CSP'si Google Fonts'a izin vermez).
//   (2) Tuval ve uygulama AYNI viewport'ta başsız Chrome'da açılır.
//   (3) Eşleme tablosundaki her öğe çifti için getComputedStyle (font-size,
//       font-weight, letter-spacing [em], line-height, renk, aile, büyük harf)
//       ve getBoundingClientRect (x, y, w, h) çıkarılır.
//   (4) Tolerans: yazı BİREBİR (boy ±0,5 px · ağırlık eşit · harf aralığı
//       ±0,01em · renk eşit · aile eşit · satır yüksekliği ±0,5 px);
//       konum/boyut ±4 px. Metin farklıysa (veri) genişlik farkı "veri"
//       gerekçesiyle bilgi satırına düşer; konum yine sınanır. "hiza":"sag"
//       öğede sol kenar yerine SAĞ kenar sınanır (sağa yaslı veri).
//   (5) Tuval | uygulama yan yana görüntü + fark (difference) görüntüsü.
//
// KULLANIM:
//   node tuval-kiyas.mjs <esleme.json> [--cdp 9701] [--profil <dizin>] [--etiket koyu]
//
// esleme.json:
//   { "tuval": "<.dc.html yolu>", "uygulama": "http://localhost:5161/",
//     "giris": "http://localhost:5161/__giris"   (isteğe bağlı; profil oturumsuzsa),
//     "genislik": 1440, "yukseklik": 900,
//     "tema": "karanlik" | "aydinlik"            (uygulamada html[data-tema]),
//     "hazir": "<uygulamada beklenecek seçici>",
//     "font": { "latin": "<woff2>", "latin-ext": "<woff2>" },
//     "cikti": "<dizin>",
//     "ogeler": [ { "ad": "menü Bugün", "tuval": ".nav a.on", "uygulama": "#yildiz-menu-bugun",
//                   "hiza": "sol"|"sag", "dikey": "ust"|"alt"|"yok", "yatay": "veri",
//                   "ek": ["background-color", ...], "yazisiz": false } ] }
//   "adimlar": [...] (isteğe bağlı, YILDIZ-4B) ölçümden önce uygulamada çalışır — bkz. adim().
//   YILDIZ-4C: öğede "konum":"kalip" (tuvalde çizilmemiş hâl → en yakın tuval kalıbı: yazı+renk birebir, yer bilgi;
//   "boy":"veri" h'yi de bilgiye alır) · üst düzeyde "tuvalAcik": true (Y_* tuvalinin .y.acik belirteçleri).
//   "dikey":"alt" alt kenarı sınar (alta yaslı liste); "yok" y'yi yalnız bilgi yazar
//   (konumu satır SAYISINA bağlı öğe — sayı veridir).
//   YILDIZ-4D: öğede "onek": "Toptancı ·" → İKİ tarafın metni de bu önekle başlamalı (veri farkı sayılmaz;
//   tuvaldeki sabit sözcük uygulamada yoksa FARK). Üst düzeyde "tuvaldeYok": [{"ad", "uygulama": "<seçici>"} |
//   {"ad", "metin": "<düzenli ifade>"}] → tuvalde OLMAYAN öğe uygulamada GÖRÜNÜRSE FARK (ör. "+ Süzgeç").
//   YILDIZ-4D: üst düzeyde "navKaynak": "<Amblem_sol(.._acik).dc.html>" → ekran tuvalinin <nav>'ı amblem tuvalinin
//   <nav>'ıyla değiştirilir (kurucu kararı 9 Ekim § 2a: amblem + ByGatech HER ekranda menünün başında; 4. tur ekran
//   tuvalleri bu karardan ÖNCE çizildi). Seçili menü öğesi ekran tuvalindekiyle aynı href'e taşınır; amblemin
//   helmet dışı <style>'ı (animasyon) eklenir. Kullanıldığında tablo başlığına yazılır.
//   Seçici sonuna "::metin" eklenirse öğenin İLK doğrudan metin düğümünün kutusu
//   ölçülür (tuvalde "113.011" gibi ayrı elemanı olmayan parça). Yazı özellikleri
//   yine öğenin kendisinden okunur.
//
// ÇIKTI (<cikti>/<etiket>-*): tablo.md · tablo.json · tuval.png · uygulama.png ·
//   yan-yana.png (tuval | uygulama) · fark.png (difference). Çıkış kodu: tüm
//   öğeler tolerans içindeyse 0, değilse 1, öğe bulunamazsa 2.
// ═══════════════════════════════════════════════════════════════════════════
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { baslat } from './cdp.mjs';

const BURASI = dirname(fileURLToPath(import.meta.url));
const arg = (ad, v = null) => { const i = process.argv.indexOf(ad); return i > 0 && process.argv[i + 1] ? process.argv[i + 1] : v; };
const yapi = JSON.parse(readFileSync(process.argv[2], 'utf8'));
const CDP = Number(arg('--cdp', '9701'));
const PROFIL = arg('--profil', '/private/tmp/kuyumcu-serit/tuval-kiyas-profil');
const ETIKET = arg('--etiket', 'kiyas');
const W = yapi.genislik ?? 1440, H = yapi.yukseklik ?? 900;
const CIKTI = yapi.cikti; mkdirSync(CIKTI, { recursive: true });
const yol = (ad) => join(CIKTI, `${ETIKET}-${ad}`);
const bek = (ms) => new Promise(r => setTimeout(r, ms));

// ── (1) Tuvali statik sayfaya çevir ─────────────────────────────────────────
export function tuvaliCevir(ham, font) {
  const helmet = (ham.match(/<helmet>([\s\S]*?)<\/helmet>/) || [, ''])[1];
  const yuz = (ad, dosya, aralik) => `@font-face{font-family:"Inter";font-style:normal;font-weight:100 900;font-display:block;src:url("file://${dosya}") format("woff2");unicode-range:${aralik}}`;
  const fontCss = font ? `<style>${yuz('ext', font['latin-ext'], 'U+0100-02BA,U+02BD-02C5,U+02C7-02CC,U+02CE-02D7,U+02DD-02FF,U+0304,U+0308,U+0329,U+1D00-1DBF,U+1E00-1E9F,U+1EF2-1EFF,U+2020,U+20A0-20AB,U+20AD-20C4,U+2113,U+2C60-2C7F,U+A720-A7FF')}${yuz('lat', font.latin, 'U+0000-00FF,U+0131,U+0152-0153,U+02BB-02BC,U+02C6,U+02DA,U+02DC,U+0304,U+0308,U+0329,U+2000-206F,U+20AC,U+2122,U+2191,U+2193,U+2212,U+2215,U+FEFF,U+FFFD')}</style>` : '';
  const bas = helmet.replace(/<link[^>]+fonts\.googleapis\.com[^>]*>/g, fontCss);
  return ham
    .replace(/<script src="\.\/support\.js"><\/script>/, '')
    .replace(/<script type="text\/x-dc"[\s\S]*?<\/script>/, '')
    .replace(/<helmet>[\s\S]*?<\/helmet>/, '')
    .replace(/<\/?x-dc>/g, '')
    .replace('</head>', `${bas}\n</head>`);
}

// ── (3) Sayfa içi ölçüm (tuval ve uygulama aynı işlev) ──────────────────────
const OLCER = `(function(ogeler, taraf){
  const sayi = v => (v === 'normal' ? 'normal' : Math.round(parseFloat(v) * 100) / 100);
  return ogeler.map(o => {
    const sec = o[taraf]; if (!sec) return { ad: o.ad, yok: true };
    const [css, ek] = sec.split('::');
    const el = document.querySelector(css);
    if (!el) return { ad: o.ad, yok: true, secici: sec };
    let r = el.getBoundingClientRect(), metin = (el.innerText || el.textContent || '').trim();
    if (ek === 'metin') {
      // YILDIZ-4D: doğrudan metin düğümü yoksa (parça kendi <span>'ına alınmışsa) İLK alt metin düğümü ölçülür.
      const tw = document.createTreeWalker(el, NodeFilter.SHOW_TEXT); let ilk = null; for (let n; (n = tw.nextNode());) if (n.textContent.trim()) { ilk = n; break; }
      const tn = [...el.childNodes].find(n => n.nodeType === 3 && n.textContent.trim()) || ilk;
      // YILDIZ-4C: kutu yalnız GLİFLERİ kapsar (baş/son boşluk hariç) — "Nakit " ile "Nakit" aynı genişlikte ölçülür.
      if (tn) { const tx = tn.textContent, bas = tx.length - tx.trimStart().length, son = tx.trimEnd().length;
        const rg = document.createRange(); rg.setStart(tn, bas); rg.setEnd(tn, son); r = rg.getBoundingClientRect(); metin = tx.trim(); }
    }
    const cs = getComputedStyle(el), fs = parseFloat(cs.fontSize);
    const ls = cs.letterSpacing === 'normal' ? 0 : parseFloat(cs.letterSpacing) / fs;
    const ekler = {}; for (const p of (o.ek || [])) ekler[p] = cs.getPropertyValue(p);
    return { ad: o.ad, x: r.x, y: r.y, w: r.width, h: r.height, fs, fw: cs.fontWeight,
      ls: Math.round(ls * 1000) / 1000, lh: sayi(cs.lineHeight), renk: cs.color,
      aile: cs.fontFamily.split(',')[0].replace(/["']/g, '').trim(), tt: cs.textTransform,
      metin: metin.replace(/\\s+/g, ' ').slice(0, 70), ekler };
  });
})`;

function karsilastir(o, t, u) {
  const sat = { ad: o.ad, farklar: [], bilgi: [] };
  if (t.yok || u.yok) { sat.yok = (t.yok ? 'tuvalde ' : '') + (u.yok ? 'uygulamada ' : '') + 'yok'; return sat; }
  const f = (ad, tv, uv, ok) => { (ok ? sat.bilgi : sat.farklar).push({ ad, tuval: tv, uygulama: uv }); };
  const yazili = !o.yazisiz;
  if (yazili) {
    if (Math.abs(t.fs - u.fs) > 0.5) f('font-size', t.fs, u.fs);
    if (String(t.fw) !== String(u.fw)) f('font-weight', t.fw, u.fw);
    if (Math.abs(t.ls - u.ls) > 0.01) f('letter-spacing(em)', t.ls, u.ls);
    if (t.lh === 'normal' || u.lh === 'normal' ? t.lh !== u.lh : Math.abs(t.lh - u.lh) > 0.5) f('line-height', t.lh, u.lh);
    if (t.renk !== u.renk) f('color', t.renk, u.renk);
    if (t.aile !== u.aile) f('font-family', t.aile, u.aile);
    if (t.tt !== u.tt) f('text-transform', t.tt, u.tt);
  }
  for (const p of (o.ek || [])) if ((t.ekler[p] || '') !== (u.ekler[p] || '')) f(p, t.ekler[p], u.ekler[p]);
  const ayniMetin = t.metin === u.metin;
  if (o.onek) for (const [taraf, m] of [['tuval', t], ['uygulama', u]])
    if (!String(m.metin).startsWith(o.onek)) f(`metin öneki "${o.onek}" (${taraf})`, t.metin, u.metin);
  // YILDIZ-4C: "konum":"kalip" → tuvalde ÇİZİLMEMİŞ bir hâlin öğesi tuvaldeki EN YAKIN KALIPLA kıyaslanır: yazı ölçüleri
  // ve renk BİREBİR sınanır, yer (x/y/w) kalıbın tuvaldeki yeri olmadığından bilgi satırına düşer; h sınanır
  // ("boy":"veri" ise o da bilgi).
  if (o.konum === 'kalip') {
    sat.bilgi.push({ ad: 'yer (kalıp — tuvalde bu hâl yok)', tuval: `${r1(t.x)},${r1(t.y)} ${r1(t.w)}`, uygulama: `${r1(u.x)},${r1(u.y)} ${r1(u.w)}` });
    if (o.boy === 'veri') sat.bilgi.push({ ad: 'h (veri)', tuval: r1(t.h), uygulama: r1(u.h) });
    else if (Math.abs(t.h - u.h) > 4) f('h', r1(t.h), r1(u.h));
    if (!ayniMetin) sat.bilgi.push({ ad: 'metin (veri)', tuval: t.metin, uygulama: u.metin });
    return sat;
  }
  const sag = o.hiza === 'sag';
  const tx = sag ? t.x + t.w : t.x, ux = sag ? u.x + u.w : u.x;
  // "yatay":"veri" → x, kendinden ÖNCEKİ verinin genişliğine bağlı (ör. dev rakamın ardındaki kuruş): bilgi.
  if (o.yatay === 'veri') sat.bilgi.push({ ad: 'x (önceki veriye bağlı)', tuval: r1(tx), uygulama: r1(ux) });
  else if (Math.abs(tx - ux) > 4) f(sag ? 'sağ kenar' : 'x', r1(tx), r1(ux));
  const dk = o.dikey ?? 'ust';   // 'ust' = üst kenar · 'alt' = alt kenar (alta yaslı liste) · 'yok' = veri sayısına bağlı
  if (dk === 'alt') { if (Math.abs(t.y + t.h - u.y - u.h) > 4) f('alt kenar', r1(t.y + t.h), r1(u.y + u.h)); }
  else if (dk === 'ust') { if (Math.abs(t.y - u.y) > 4) f('y', r1(t.y), r1(u.y)); }
  else sat.bilgi.push({ ad: 'y (satır sayısına bağlı)', tuval: r1(t.y), uygulama: r1(u.y) });
  if (Math.abs(t.h - u.h) > 4) f('h', r1(t.h), r1(u.h));
  if (Math.abs(t.w - u.w) > 4) f('w', r1(t.w), r1(u.w), !ayniMetin);   // metin farklıysa genişlik = veri
  if (!ayniMetin) sat.bilgi.push({ ad: 'metin (veri)', tuval: t.metin, uygulama: u.metin });
  return sat;
}
const r1 = (v) => Math.round(v * 10) / 10;

// YILDIZ-4B: uygulama ölçülmeden önce ekranı tuvaldeki hâle getiren adımlar (ör. fişe kalem ekle).
//   {"tikla": "<seçici>"} · {"yaz": {"secici": "<seçici>", "metin": "..."}} · {"tus": "Enter"} ·
//   {"ev": "<js ifadesi>"} · {"bekle": ms} · {"varsa": "<seçici>", ...} (seçici yoksa adım atlanır)
async function adim(b, a) {
  if (a.varsa && !(await b.ev(`!!document.querySelector(${JSON.stringify(a.varsa)})`))) return;
  if (a.tikla) await b.ev(`(()=>{const e=document.querySelector(${JSON.stringify(a.tikla)}); if(!e) throw new Error('yok: ${a.tikla.replace(/'/g, '')}'); e.click(); return 1;})()`);
  if (a.yaz) {
    await b.ev(`(()=>{const e=document.querySelector(${JSON.stringify(a.yaz.secici)}); e.focus(); e.select && e.select(); return 1;})()`);
    await b.cmd('Input.insertText', { text: a.yaz.metin });
  }
  if (a.tus) {
    const k = { key: a.tus, code: a.tus, windowsVirtualKeyCode: a.tus === 'Enter' ? 13 : 0 };
    await b.cmd('Input.dispatchKeyEvent', { type: 'keyDown', ...k, text: a.tus === 'Enter' ? '\r' : undefined });
    await b.cmd('Input.dispatchKeyEvent', { type: 'keyUp', ...k });
  }
  if (a.ev) await b.ev(a.ev);
  await bek(Number(a.bekle ?? 600));
}

async function yanYana(b, a, c, cikti, mod) {
  const v = (p) => 'data:image/png;base64,' + readFileSync(p).toString('base64');
  const html = mod === 'fark'
    ? `<body style="margin:0;background:#000"><div style="position:relative;width:${W}px;height:${H}px"><img src="${v(a)}" style="position:absolute;inset:0"><img src="${v(c)}" style="position:absolute;inset:0;mix-blend-mode:difference"></div></body>`
    : `<body style="margin:0;background:#777;display:flex;gap:8px"><img src="${v(a)}" style="width:${W}px"><img src="${v(c)}" style="width:${W}px"></body>`;
  const gecici = join(CIKTI, `${ETIKET}-${mod}.html`);
  writeFileSync(gecici, html);
  await b.boyut(mod === 'fark' ? W : W * 2 + 8, H);
  await b.git('file://' + gecici, 1200);
  await b.foto(cikti);
  printfBosalt(gecici);
}
// YILDIZ-4D — ekran tuvalinin nav'ını amblem tuvalinin nav'ıyla değiştir (bkz. başlık "navKaynak").
export function navDegistir(ekran, amblem) {
  const navRe = /<nav class="nav"[\s\S]*?<\/nav>/;
  const eNav = (ekran.match(navRe) || [])[0], aNav = (amblem.match(navRe) || [])[0];
  if (!eNav || !aNav) throw new Error('navKaynak: <nav class="nav"> bulunamadı');
  // Seçili öğe METNİYLE eşlenir (açık tuvallerde href "…4a.dc.html", amblem tuvalinde "…4.dc.html").
  const secili = (eNav.match(/<a [^>]*class="on"[^>]*>([^<]+)<\/a>/) || [])[1];
  const menu = (n) => n.match(/<div style="display: flex; gap: 36px;[^"]*">[\s\S]*?<\/div>/)[0];
  let m = menu(aNav).replace(/ class="on" aria-current="page"/g, '');
  if (secili) m = m.replace(new RegExp(`<a href="([^"]+)">${secili}</a>`), `<a href="$1" class="on" aria-current="page">${secili}</a>`);
  const yeniNav = aNav.replace(menu(aNav), m);
  const helmet = (amblem.match(/<helmet>[\s\S]*?<\/helmet>/) || [''])[0];
  const disStil = amblem.replace(helmet, '').match(/<style>[\s\S]*?<\/style>/g) || [];
  return ekran.replace(navRe, yeniNav).replace('</head>', `${disStil.join('\n')}\n</head>`);
}
function printfBosalt(p) { try { writeFileSync(p, ''); } catch {} }

async function ana() {
  const ham = readFileSync(yapi.tuval, 'utf8');
  const tuvalYolu = yol('tuval.html');
  let cevrik = tuvaliCevir(ham, yapi.font);
  // "tuvalEkCss" (YILDIZ-4B): tuvalin KENDİ çizim kusurunu gideren ek kural (ör. Tum4'te sabit 1460 px kutuda
  // taşan içerik nav'ı 72 → 41 px'e ezer). Kullanıldığında tablo başlığına yazılır (gizli düzeltme yok).
  if (yapi.tuvalEkCss) cevrik = cevrik.replace('</head>', `<style>${yapi.tuvalEkCss}</style>\n</head>`);
  if (yapi.navKaynak) cevrik = navDegistir(cevrik, readFileSync(yapi.navKaynak, 'utf8'));
  writeFileSync(tuvalYolu, cevrik);
  const b = await baslat(CDP, PROFIL);
  try {
    await b.boyut(W, H);
    await b.git('file://' + tuvalYolu, 2500);
    // YILDIZ-4C: "tuvalAcik": true → tuvalin KENDİ açık belirteçleri (.y.acik — Y_* dosyalarında tanımlı, yalnız koyu çizilmiş ekranlar için).
    if (yapi.tuvalAcik) await b.ev(`(document.querySelector('.y')?.classList.add('acik'), 1)`);
    await b.ev('document.fonts.ready.then(()=>1)');
    const tuval = await b.ev(`${OLCER}(${JSON.stringify(yapi.ogeler)}, 'tuval')`);
    await b.foto(yol('tuval.png'));

    if (yapi.giris) await b.git(yapi.giris, 4000);
    await b.git(yapi.uygulama, 4000);
    if (yapi.tema) await b.ev(`document.documentElement.setAttribute('data-tema', ${JSON.stringify(yapi.tema)}); 1`);
    if (yapi.hazir) {
      for (let i = 0; i < 60; i++) { if (await b.ev(`!!document.querySelector(${JSON.stringify(yapi.hazir)})`)) break; await bek(500); }
    }
    await bek(Number(yapi.bekle ?? 1500));
    for (const a of (yapi.adimlar || [])) await adim(b, a);
    await b.ev('document.fonts.ready.then(()=>1)');
    const uyg = await b.ev(`${OLCER}(${JSON.stringify(yapi.ogeler)}, 'uygulama')`);
    const tasma = await b.ev('document.documentElement.scrollWidth - document.documentElement.clientWidth');
    // YILDIZ-4D: tuvalde olmayan öğe uygulamada görünür mü (görünür = kutusu > 0, visibility/display açık)?
    const yasak = await b.ev(`(function(l){const g=e=>{const r=e.getBoundingClientRect(),s=getComputedStyle(e);return r.width>0&&r.height>0&&s.visibility!=='hidden'&&s.display!=='none'};
      const m=document.querySelector('main')||document.body; return l.map(y=>{ if(y.uygulama){const e=[...document.querySelectorAll(y.uygulama)].filter(g); return {ad:y.ad, gorunen:e.length, ornek:e[0]?(e[0].innerText||'').trim().slice(0,40):''};}
        const re=new RegExp(y.metin); const e=[...m.querySelectorAll('*')].filter(x=>x.children.length===0&&re.test(x.textContent||'')&&g(x)); return {ad:y.ad, gorunen:e.length, ornek:e[0]?e[0].textContent.trim().slice(0,40):''};});})(${JSON.stringify(yapi.tuvaldeYok || [])})`);
    await b.foto(yol('uygulama.png'));

    const satirlar = yapi.ogeler.map((o, i) => karsilastir(o, tuval[i], uyg[i]));
    const yok = satirlar.filter(s => s.yok).length;
    const farkli = satirlar.filter(s => !s.yok && s.farklar.length).length + yasak.filter(y => y.gorunen > 0).length;
    let md = `# Tuval kıyası — ${ETIKET}\n\nTuval: \`${yapi.tuval}\` · Uygulama: \`${yapi.uygulama}\` · ${W}×${H} · tema ${yapi.tema ?? '-'} · yatay taşma ${tasma} px${yapi.tuvalEkCss ? ` · tuval ek CSS: \`${yapi.tuvalEkCss}\`` : ''}${yapi.navKaynak ? ` · nav: \`${yapi.navKaynak.split('/').pop()}\` (§ 2a amblem kararı)` : ''}\n\n`;
    md += `Sonuç: ${satirlar.length + yasak.length} öğe · tolerans içinde ${satirlar.length + yasak.length - farkli - yok} · FARK ${farkli} · bulunamayan ${yok}\n\n`;
    md += `| # | Öğe | Ölçü | Tuval | Uygulama | Durum |\n|---|---|---|---|---|---|\n`;
    satirlar.forEach((s, i) => {
      const t = tuval[i], u = uyg[i];
      if (s.yok) { md += `| ${i + 1} | ${s.ad} | — | ${t.yok ? 'YOK' : t.metin} | ${u.yok ? 'YOK' : u.metin} | **BULUNAMADI** |\n`; return; }
      const ozet = (m) => `${m.fs}px/${m.fw}/${m.ls}em/lh ${m.lh}/${m.renk}/${m.aile} @ ${r1(m.x)},${r1(m.y)} ${r1(m.w)}×${r1(m.h)}`;
      md += `| ${i + 1} | ${s.ad} | özet | ${ozet(t)} | ${ozet(u)} | ${s.farklar.length ? '**FARK**' : 'AYNI'} |\n`;
      for (const f of s.farklar) md += `|  |  | ${f.ad} | ${f.tuval} | ${f.uygulama} | **FARK** |\n`;
      for (const f of s.bilgi) md += `|  |  | ${f.ad} | ${f.tuval} | ${f.uygulama} | bilgi |\n`;
    });
    if (yasak.length) {
      md += `\n## Tuvalde olmayan öğeler (uygulamada görünmemeli)\n\n| Öğe | Uygulamada görünen | Örnek | Durum |\n|---|---|---|---|\n`;
      for (const y of yasak) md += `| ${y.ad} | ${y.gorunen} | ${y.ornek} | ${y.gorunen ? '**FARK**' : 'AYNI (yok)'} |\n`;
    }
    writeFileSync(yol('tablo.md'), md);
    writeFileSync(yol('tablo.json'), JSON.stringify({ tuval, uygulama: uyg, satirlar, yasak, tasma }, null, 2));
    await yanYana(b, yol('tuval.png'), yol('uygulama.png'), yol('yan-yana.png'), 'yan');
    await yanYana(b, yol('tuval.png'), yol('uygulama.png'), yol('fark.png'), 'fark');
    console.log(`${ETIKET}: ${satirlar.length + yasak.length} öğe · AYNI ${satirlar.length + yasak.length - farkli - yok} · FARK ${farkli} · YOK ${yok} · taşma ${tasma} → ${yol('tablo.md')}`);
    process.exitCode = yok ? 2 : farkli ? 1 : 0;
  } finally { b.kapat(); }
}
if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) await ana();
export { BURASI };
