#!/usr/bin/env node
// ═══════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-KATMAN — Yıldız KATMAN TARAMASI (kalıcı araç)
//
// Hakem 5A (KRİTİK): "eski tema çekmece/ModalPencere Yıldız'a sızıyor" — açılış hali tuvale
// uygundu, ama her AÇICI (Yeni/Düzenle/Sil/Geri ver/Barkod okut…) eski temanın yüzen kartını,
// lacivert perdesini, gölgesini getiriyordu. Mevcut ölçerler yalnız AÇILIŞ durumunu ölçtüğü
// için göremedi. Bu araç her ekranda güvenli açıcılara basar ve açılan katmanı ölçer.
//
// KURAL (Yıldız = tuval dili: kartsız, ince çizgi, sağ çekmece tek kalıp — yüzen kart yok):
//   TIP       : açılan modal/çekmece katmanı SAĞ ÇEKMECE olmalı (sağ kenara yapışık, boy ≥ %90
//               ekran; dar ekranda tam genişlik). Ortada yüzen panel = SERBEST-YUZEN (KIRMIZI).
//   PANEL     : zemin --yuzey-govde, köşe 0, gölge yok.
//   PERDE     : arka perde saydam (eski lacivert/siyah perde = KIRMIZI).
//   ESKI      : katman içinde görünen öğe: gölge taşıyan, köşesi > 4 px dolu kutu (kart), ya da
//               zemini Yıldız belirteci olmayan (--yuzey-govde/saydam; --metin yalnız b1 hapı) öğe.
//   AILE      : katman içi metnin ilk yazı ailesi Inter.
//   GORUNUR   : (etkileşim sonrası görünürlük, sikisma-olc K15 ile aynı kural) katman içinde
//               overflow:hidden atasınca kırpılan ya da kaydırılamaz biçimde ekran dışında kalan
//               metin/düğme + ilk görünümde ekran dışında kalan birincil (Kaydet) düğmesi.
//   UST-MENU  : #yildiz-menu içinde aria-current="page" taşıyan öğe sayısı = 1.
//
// KULLANIM:
//   node yildiz-katman-tara.mjs --mod BASIT|PRO [--cdp 9701] [--profil <dizin>] [--vekil http://localhost:5161]
//        [--cikti <dizin>] [--genislik 1440,375] [--temalar karanlik,aydinlik] [--rotalar /a,/b] [--acici-sinir 14]
//        [--foto '/urunler|Yeni ürün']   (o açıcı açıkken her genişlik/tema için ekran görüntüsü)
// ÇIKTI: <cikti>/katman-<MOD>.jsonl + özet; çıkış kodu KIRMIZI > 0 ise 1.
// Kayıt/gönderme/silme ONAYINA basılmaz: açılan onay penceresi Esc ile kapatılır.
// ═══════════════════════════════════════════════════════════════════════════
import { readFileSync, mkdirSync, appendFileSync, writeFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { baslat } from './cdp.mjs';

const BU = dirname(fileURLToPath(import.meta.url));
const arg = (ad, v = null) => { const i = process.argv.indexOf(ad); return i > 0 && process.argv[i + 1] ? process.argv[i + 1] : v; };
const MOD = arg('--mod', 'BASIT').toUpperCase();
const CDP = Number(arg('--cdp', '9701'));
const PROFIL = arg('--profil', '/private/tmp/kuyumcu-serit/pYILDIZ-KATMAN/profil');
const VEKIL = arg('--vekil', 'http://localhost:5161');
const CIKTI = arg('--cikti', '/private/tmp/kuyumcu-serit/pYILDIZ-KATMAN/katman'); mkdirSync(CIKTI, { recursive: true });
const GEN = arg('--genislik', '1440,375').split(',').map(Number);
const TEMALAR = arg('--temalar', 'karanlik,aydinlik').split(',');
const ROT_ARG = arg('--rotalar', null);
const ACICI_SINIR = Number(arg('--acici-sinir', '14'));
const FOTO = (arg('--foto', '') || '').split(';').filter(Boolean).map(x => x.split('|'));
const BOY = { 1440: 900, 1280: 800, 375: 812 };
const SIKISMA = readFileSync(join(BU, '..', 'sikisma-olc.js'), 'utf8');
const bek = ms => new Promise(r => setTimeout(r, ms));
const DOSYA = join(CIKTI, `katman-${MOD}.jsonl`); writeFileSync(DOSYA, '');
const yaz = o => appendFileSync(DOSYA, JSON.stringify(o) + '\n');

const OLCER = `(() => {
  const BELIRTEC = ['--metin','--metin-soluk','--kenarlik','--ayrac','--yuzey-govde','--basari','--tehlike'];
  const vis = e => { for (let p = e; p && p.nodeType === 1; p = p.parentElement) { const s = getComputedStyle(p); if (s.display === 'none' || s.visibility === 'hidden' || +s.opacity === 0) return false; } const r = e.getBoundingClientRect(); return r.width > 1 && r.height > 1; };
  const saydam = c => c === 'transparent' || /rgba\\([^)]*,\\s*0\\)$/.test(c);
  const tokenRgb = () => { const d = document.createElement('span'); document.body.appendChild(d); const m = {}; for (const t of BELIRTEC) { d.style.color = 'var(' + t + ')'; m[getComputedStyle(d).color] = t; } d.remove(); return m; };
  const B1 = '.dugme-birincil, .btn-birincil, .yildiz-b1, .yz-b1, [class~="b1"]';
  const ad = e => (e.id ? '#' + e.id : e.tagName.toLowerCase() + (typeof e.className === 'string' && e.className.trim() ? '.' + e.className.trim().split(/\\s+/)[0] : '')) + ' "' + (e.innerText || e.value || e.getAttribute('aria-label') || '').trim().replace(/\\s+/g, ' ').slice(0, 24) + '"';
  const KATMAN = '.modal-panel, .cekmece, [role=dialog][aria-modal=true]';
  window.__katmanlar = () => [...document.querySelectorAll(KATMAN)].filter(vis).filter((e, i, a) => !a.some(p => p !== e && p.contains(e)));
  window.__katmanImza = () => window.__katmanlar().map(e => (e.id || e.className)).join('|');
  window.__ustMenuSecili = () => [...document.querySelectorAll('#yildiz-menu a[aria-current=page]')].map(a => a.id);
  window.__katmanOlc = () => {
    const tok = tokenRgb(); const W = innerWidth, H = innerHeight;
    return window.__katmanlar().map(k => {
      const r = k.getBoundingClientRect(); const s = getComputedStyle(k);
      const sag = Math.abs(r.right - W) <= 2 && r.height >= H * 0.9 && r.top <= 2;
      const tip = sag ? 'SAG-CEKMECE' : 'SERBEST-YUZEN';
      const perdeE = k.previousElementSibling && /arka/.test(k.previousElementSibling.className) ? k.previousElementSibling : (k.parentElement && /arka/.test(k.parentElement.className) ? k.parentElement : null);
      const perde = perdeE ? getComputedStyle(perdeE).backgroundColor : 'yok';
      const panelZemin = tok[s.backgroundColor] || s.backgroundColor;
      const kirmizi = [];
      if (tip !== 'SAG-CEKMECE') kirmizi.push('tip');
      if (panelZemin !== '--yuzey-govde') kirmizi.push('panel-zemin ' + panelZemin);
      if (parseFloat(s.borderTopLeftRadius) > 0 || parseFloat(s.borderBottomLeftRadius) > 0) kirmizi.push('panel-kose ' + s.borderRadius);
      if (s.boxShadow !== 'none') kirmizi.push('panel-golge');
      if (perde !== 'yok' && !saydam(perde)) kirmizi.push('perde ' + perde);
      const eski = [];
      for (const e of [k, ...k.querySelectorAll('*')]) {
        if (e !== k && !vis(e)) continue;
        if (e.closest('.belge, .urun-etiket, svg')) continue;
        const es = getComputedStyle(e);
        if (e !== k && es.boxShadow !== 'none' && !/inset/.test(es.boxShadow)) { eski.push('golge ' + ad(e)); continue; }
        const bg = es.backgroundColor;
        if (e !== k && !saydam(bg)) {
          const t = tok[bg];
          if (!(t === '--yuzey-govde' || (t === '--metin' && e.matches(B1)))) { eski.push('zemin ' + (t || bg) + ' ' + ad(e)); continue; }
          if (t === '--yuzey-govde' && parseFloat(es.borderTopLeftRadius) > 4 && !e.matches('button, a, input, select, textarea, .secici-tetik')) { eski.push('kart ' + ad(e)); continue; }
        }
      }
      const aile = [...k.querySelectorAll('h1,h2,h3,label,button,input,p,span,td,th,a')].filter(vis).map(e => getComputedStyle(e).fontFamily.split(',')[0].replace(/"/g, '').trim()).filter(f => f !== 'Inter');
      // Etkileşim sonrası görünürlük (K15)
      const gor = [];
      const yaprak = [...k.querySelectorAll('button, a, input, select, textarea, label, h2, h3, p, span, td, .secici-tetik')].filter(vis).filter(e => (e.innerText || e.value || e.placeholder || '').trim());
      for (const e of yaprak) {
        const er = e.getBoundingClientRect();
        let kirp = null, kayar = false;
        for (let p = e.parentElement; p && p !== document.body; p = p.parentElement) {
          const ps = getComputedStyle(p); const ov = ps.overflowY + ' ' + ps.overflowX;
          if (/auto|scroll/.test(ov)) { kayar = true; break; }
          if (/hidden|clip/.test(ov)) { const pr = p.getBoundingClientRect(); if (er.bottom > pr.bottom + 1 || er.top < pr.top - 1 || er.right > pr.right + 1) kirp = p; break; }
          if (p === k) break;
        }
        if (kirp) gor.push('kirpik ' + ad(e));
        else if (!kayar && (er.bottom > H + 1 || er.right > W + 1)) gor.push('ekran-disi ' + ad(e));
      }
      const birincil = [...k.querySelectorAll(B1 + ', [id$="-kaydet"]')].filter(vis);
      for (const b of birincil) { const br = b.getBoundingClientRect(); if (br.bottom > H + 1 || br.top < 0) gor.push('birincil-gorunmez ' + ad(b)); }
      // DÜĞME (YILDIZ-KATMAN tur 2 — hakem KRİTİK "çekmeceden çekmeceye dört kalıp"): katmandaki her EYLEM SATIRI
      // (birincil/tehlike düğmesinin ebeveyni) tuval form kalıbında olmalı: b1 hap 56 px, dolgu 0 32, 16/500, yazı
      // genişliğinde (esnemez) · ikincil b2 altı çizili 56 px 16 px saydam, köşe 0 · b1 SOLDA, b2 sağında · ikincil metni "Vazgeç".
      const dugme = []; const dugmeKirmizi = [];
      const BIR = B1 + ', .btn-tehlike, .dugme-tehlike';
      const satirlar = new Set([...k.querySelectorAll(BIR)].filter(vis).filter(e => e.matches('button, a')).map(e => e.parentElement));
      for (const sat of satirlar) {
        const uyeler = [...sat.children].filter(e => e.matches('button, a') && vis(e));
        let b1x = null;
        for (const e of uyeler) {
          const es = getComputedStyle(e), er = e.getBoundingClientRect();
          const rg = document.createRange(); rg.selectNodeContents(e); const yaziW = rg.getBoundingClientRect().width;
          const rol = e.matches(BIR) ? 'b1' : 'b2';
          const zem = saydam(es.backgroundColor) ? 'saydam' : (tok[es.backgroundColor] || es.backgroundColor);
          const m = (e.innerText || '').trim().replace(/\\s+/g, ' ').slice(0, 20);
          const o = { rol, metin: m, sinif: (typeof e.className === 'string' ? e.className : '').slice(0, 40), w: Math.round(er.width), h: Math.round(er.height), x: Math.round(er.x),
            fazla: Math.round(er.width - yaziW), fs: es.fontSize, fw: es.fontWeight, pl: es.paddingLeft, pr: es.paddingRight, kose: es.borderTopLeftRadius, zem, alt: es.borderBottomWidth };
          dugme.push(o);
          const hata = [];
          if (o.h !== 56) hata.push('boy ' + o.h);
          if (o.fs !== '16px') hata.push('yazi ' + o.fs);
          if (rol === 'b1') {
            b1x = er.x;
            if (o.pl !== '32px' || o.pr !== '32px') hata.push('dolgu ' + o.pl + '/' + o.pr);
            if (o.fw !== '500') hata.push('agirlik ' + o.fw);
            if (parseFloat(o.kose) < 28) hata.push('kose ' + o.kose);
            if (!(zem === '--metin' || (zem === 'saydam' && e.matches('.btn-tehlike, .dugme-tehlike')))) hata.push('zemin ' + zem);
            if (o.fazla > 66) hata.push('esniyor +' + o.fazla);
          } else {
            if (zem !== 'saydam') hata.push('zemin ' + zem);
            if (parseFloat(o.kose) > 0) hata.push('kose ' + o.kose);
            if (o.alt !== '1px') hata.push('alt-cizgi ' + o.alt);
            if (o.fazla > 12) hata.push('esniyor +' + o.fazla);
            if (/^(iptal|vazgeç)$/i.test(m) && m !== 'Vazgeç') hata.push('metin ' + m);
          }
          if (hata.length) dugmeKirmizi.push(rol + ' "' + m + '" ' + hata.join(','));
        }
        for (const o of dugme) if (o.rol === 'b2' && b1x !== null && o.x < b1x && uyeler.length > 1) { dugmeKirmizi.push('sira b2 "' + o.metin + '" b1 solunda'); break; }
      }
      if (dugmeKirmizi.length) kirmizi.push('dugme ' + dugmeKirmizi.slice(0, 4).join(' | '));
      const h = k.querySelector('h2, h3'); const hs = h ? getComputedStyle(h) : null;
      return { katman: (k.id || k.className).slice(0, 40), tip, x: Math.round(r.x), w: Math.round(r.width), h: Math.round(r.height), panelZemin, kose: s.borderRadius, golge: s.boxShadow === 'none' ? 0 : 1, perde,
        baslik: hs ? (h.innerText.trim().slice(0, 30) + ' ' + hs.fontSize + '/' + hs.fontWeight + ' ' + hs.fontFamily.split(',')[0]) : '-',
        kirmizi, eski: eski.length, eskiOrnek: eski.slice(0, 5), aileDisi: aile.length, aileOrnek: [...new Set(aile)].slice(0, 3), gorunurluk: gor.length, gorOrnek: gor.slice(0, 4), dugme, dugmeKirmizi };
    });
  };
  // Açıcılar: katman açan güvenli öğeler + 5A/denetim açıcıları (Geri ver, Barkod okut, Sil/Pasife al → onay penceresi).
  const YASAK = /kaydet|onayla|gönder|fatura kes|öde\\b|ödeme yap|tahsil et|yazdır|çıkış|oturumu kapat|uygula|içe aktar|yedek al|geri yükle|devret|vezneye|kilitle|sıfırla|evet|başlat|bitir|tamamla|indir|dışa aktar|kapat ve|günü kapat/i;
  const ACAN = /^(\\+\\s*)?(yeni|düzenle|ayrıntı|detay|ekle|.*\\bekle$|geri ver|iade|barkod okut|emanet al|sil$|pasife al|kaldır|aktife al|değiştir|gör|incele)/i;
  window.__katmanAcicilar = () => {
    const ana = document.querySelector('main') || document.body; const sec = []; const gor = new Set();
    for (const e of [...ana.querySelectorAll('button, a[role=button], a.yz-b1, a.yz-b2, a.yz-b3, a.yz-ekle')].filter(vis)) {
      if (e.disabled) continue;
      const t = (e.innerText || e.getAttribute('aria-label') || e.title || '').trim().replace(/\\s+/g, ' ');
      if (!t || YASAK.test(t) || !ACAN.test(t)) continue;
      if (e.type === 'submit' && e.closest('form')) continue;   // form DIŞINDAKİ type'sız düğme varsayılan 'submit' görünür ama göndermez
      const k = t.slice(0, 30).toLowerCase(); if (gor.has(k)) continue; gor.add(k);
      e.setAttribute('data-katman-acici', String(sec.length)); sec.push({ i: sec.length, ad: ad(e), metin: t.slice(0, 30) });
    }
    return sec;
  };
  return 'katman-kuruldu';
})()`;

const b = await baslat(CDP, PROFIL);
await b.cmd('Network.enable'); await b.cmd('Network.setCacheDisabled', { cacheDisabled: true });
const kur = async () => { await b.ev(SIKISMA); await b.ev(OLCER); };
const temaUygula = t => b.ev(`window.kuyumcuTema.uygula(${JSON.stringify(t)}); document.documentElement.setAttribute('data-tema', ${JSON.stringify(t)}); 1`);
const yukBekle = async () => { for (let i = 0; i < 30; i++) { const y = await b.ev(`[...document.querySelectorAll('.donen-halka, .yukleniyor-kutu, [aria-busy=true]')].some(e => e.getBoundingClientRect().width > 0)`); if (!y) break; await bek(400); } await bek(300); };
const kapatHepsi = async () => { for (let i = 0; i < 3; i++) { const n = await b.ev(`(()=>{const k=window.__katmanlar(); if(!k.length) return 0; const x=k[k.length-1].querySelector('.cekmece-kapat, .modal-kapat'); if(x) x.click(); else k[k.length-1].dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true})); return k.length})()`); if (!n) break; await bek(400); } };
const IMZA = {};
const ozet = { mod: MOD, dugmeKirmizi: 0, ekran: 0, acici: 0, katman: 0, kirmizi: 0, tipYuzen: 0, eski: 0, aileDisi: 0, gorunurluk: 0, ustMenuSeciliYok: 0, ekranK15: 0, hata: 0 };
let rotalar = [];
try {
  await b.boyut(1440, 900);
  let gir = false;
  for (let d = 0; d < 4 && !gir; d++) { await b.git(VEKIL + '/__giris', 5000); await b.git(VEKIL + '/', 5000); gir = await b.ev(`document.documentElement.getAttribute('data-sema')==='yildiz' && !!document.querySelector('#yildiz-kullanici-ac')`).catch(() => false); if (!gir) await bek(30000); }
  if (!gir) { console.log('GIRIS/SEMA-BASARISIZ'); process.exit(3); }
  await b.ev(`document.querySelector('#yildiz-kullanici-ac').click(); 1`); await bek(500);
  await b.ev(`document.querySelector('#yildiz-mod-${MOD === 'PRO' ? 'profesyonel' : 'basit'}')?.click(); 1`); await bek(2500);
  await b.git(VEKIL + '/', 5000);
  const modGercek = await b.ev(`document.documentElement.getAttribute('data-mod')`);
  if (ROT_ARG) rotalar = ROT_ARG.split(',');
  else { await b.ev(`Blazor.navigateTo('/ekranlar'); 1`); await bek(2500); rotalar = await b.ev(`[...new Set([...document.querySelectorAll('#yildiz-tum a.yz-kare[href]')].map(a => new URL(a.href).pathname))]`); rotalar = ['/ekranlar', ...rotalar.filter(r => r !== '/ekranlar')]; }
  console.log('mod', MOD, '→', modGercek, '· rota', rotalar.length);
  yaz({ tip: 'kosum', MOD, modGercek, rotalar });
  for (const w of GEN) {
    await b.boyut(w, BOY[w] || 900); await b.git(VEKIL + '/', 5000); await kur();
    for (const rota of rotalar) {
      try {
        await b.ev(`Blazor.navigateTo(${JSON.stringify(rota)}); 1`); await bek(1200); await yukBekle(); await kur();
        const secili = await b.ev('window.__ustMenuSecili()');
        const k15e = await b.ev('window.__olc().K15_etkilesimGorunurluk ?? -1');
        ozet.ekran++; if (secili.length !== 1) ozet.ustMenuSeciliYok++; if (k15e > 0) ozet.ekranK15++;
        yaz({ tip: 'ekran', rota, w, secili, K15: k15e });
        const acicilar = (await b.ev('window.__katmanAcicilar()')).slice(0, ACICI_SINIR);
        for (const a of acicilar) {
          const onceImza = await b.ev('window.__katmanImza()');
          // Açıcı her seferinde METNİYLE yeniden bulunur (Blazor yeniden çizince öznitelik kaybolur).
          await b.ev('window.__katmanAcicilar(); 1');
          const var_ = await b.ev(`(()=>{const e=[...document.querySelectorAll('[data-katman-acici]')].find(x=>(x.innerText||x.getAttribute('aria-label')||x.title||'').trim().replace(/\\s+/g,' ').slice(0,30)===${JSON.stringify(a.metin)}); if(!e||e.getBoundingClientRect().width===0) return false; e.scrollIntoView({block:'center'}); e.click(); return true})()`);
          if (!var_) { yaz({ tip: 'acici-bulunamadi', rota, w, acici: a.metin }); continue; }
          ozet.acici++; await bek(700); await yukBekle(); await kur();
          const sonraImza = await b.ev('window.__katmanImza()');
          const yolS = await b.ev('location.pathname');
          if (sonraImza && sonraImza !== onceImza) {
            for (const t of TEMALAR) {
              await temaUygula(t); await bek(150);
              const olc = await b.ev('window.__katmanOlc()');
              const k15 = await b.ev('window.__olc().K15_etkilesimGorunurluk ?? -1');
              for (const o of olc) {
                ozet.katman++; if (o.kirmizi.length || o.eski || o.aileDisi || o.gorunurluk) ozet.kirmizi++;
                if (o.tip !== 'SAG-CEKMECE') ozet.tipYuzen++; if (o.dugmeKirmizi.length) ozet.dugmeKirmizi++; for (const d of o.dugme) { const im = d.rol + " " + d.h + "px " + d.fs + "/" + d.fw + " dolgu " + d.pl + " zemin " + d.zem + " kose " + (parseFloat(d.kose) >= 28 ? "hap" : d.kose); IMZA[im] = (IMZA[im] || 0) + 1; }
                ozet.eski += o.eski; ozet.aileDisi += o.aileDisi; ozet.gorunurluk += o.gorunurluk;
                yaz({ ...o, katmanTipi: o.tip, tip: 'katman', rota, w, tema: t, acici: a.metin, K15: k15 });
              }
              const f = FOTO.find(([r, m]) => r === rota && a.metin.startsWith(m));
              if (f) await b.foto(join(CIKTI, `foto-${MOD}-${w}-${t}-${rota.replace(/\W+/g, '_')}-${a.metin.replace(/\W+/g, '_').slice(0, 16)}.png`));
            }
            await temaUygula(TEMALAR[0]);
            await kapatHepsi();
          } else {
            yaz({ tip: 'acici-katmansiz', rota, w, acici: a.metin, yol: yolS, secili: await b.ev('window.__ustMenuSecili()') });
          }
          // Her açıcıdan sonra ekran SIFIRLANIR (aynı rotaya gitmek bileşeni yeniden kurmaz → önce başka ekrana).
          await b.ev(`Blazor.navigateTo('/ekranlar'); 1`); await bek(500);
          await b.ev(`Blazor.navigateTo(${JSON.stringify(rota)}); 1`); await bek(1200); await yukBekle(); await kur();
          await b.ev('window.__katmanAcicilar(); 1');
        }
      } catch (e) { ozet.hata++; yaz({ tip: 'hata', rota, w, hata: String(e).slice(0, 200) }); try { await b.git(VEKIL + rota, 5000); await kur(); } catch {} }
    }
  }
} finally { b.kapat(); }
yaz({ tip: "dugme-imza", IMZA });
console.log("DÜĞME İMZALARI (katman eylem satırları):"); for (const [k, v] of Object.entries(IMZA).sort((a, b) => b[1] - a[1])) console.log("  " + v + " × " + k);
yaz({ tip: 'ozet', ...ozet });
console.log(`YILDIZ-KATMAN ${MOD}: ekran ölçümü ${ozet.ekran} · açıcı ${ozet.acici} · katman ölçümü ${ozet.katman} · KIRMIZI katman ${ozet.kirmizi} (yüzen ${ozet.tipYuzen} · eski öğe ${ozet.eski} · Inter dışı ${ozet.aileDisi} · görünürlük ${ozet.gorunurluk}) · üst menü seçili yok ${ozet.ustMenuSeciliYok} · ekran K15 ${ozet.ekranK15} · düğme kalıbı dışı katman ${ozet.dugmeKirmizi} · hata ${ozet.hata}`);
process.exit(ozet.kirmizi + ozet.ustMenuSeciliYok > 0 ? 1 : 0);
