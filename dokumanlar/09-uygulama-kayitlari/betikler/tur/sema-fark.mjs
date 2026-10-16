#!/usr/bin/env node
// ═══════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-4C — ESKİ 8 ŞEMA PİKSEL FARKI (kalıcı; YILDIZ-1/4B fark betiklerinin yerine)
//
// Soru: "Yıldız" eklemesi (tema-yildiz.css + Yıldız koşullu razor dalları) öteki 8 şemada
// bir pikseli bile değiştirdi mi? Yöntem (4B arac/fark.mjs ile AYNI, kalıcılaştırıldı):
//   • Kiracının şeması ÖNCEDEN Yıldız DIŞINA alınmış olmalı (PATCH /gorunum {"sema":"platin"}):
//     böylece Yıldız kabuğu/bileşenleri hiç çizilmez — betik bunu her rotada DOĞRULAR
//     (.yildiz-kabuk ya da .yz varsa çıkış 3).
//   • Her rota × 8 şema × açık/koyu: html[data-sema/data-tema] değiştirilir, tema-yildiz.css
//     AÇIK → KAPALI → AÇIK üç görüntü alınır (Inter @font-face kuralları ayrı <style>'a kopyalanır
//     ki yazı tipi farkı ölçüme karışmasın). İlk görüntü ISINMA'dır (sayfa ilk saniyelerde zamanla
//     değişebilir); KAPALI ↔ ikinci AÇIK sha256 eşit olmalı. Fark çıkarsa karşılaştırma BİR kez
//     yeniden çekilir (ısınma kanıtı yazılır), yine farklıysa FARK sayılır.
//
// KULLANIM:
//   node sema-fark.mjs <etiket> [--cdp 9719] [--profil <dizin>] [--vekil http://localhost:5161]
//                      [--rotalar /,/islemler,/cariler,/ekranlar] [--genislik 1440] [--yukseklik 900]
// ÇIKTI: satır başına FARK/YENIDEN kaydı + "<etiket>: N karşılaştırma · FARK k". Çıkış kodu FARK>0 ise 1.
// ═══════════════════════════════════════════════════════════════════════════
import { createHash } from 'node:crypto';
import { baslat } from './cdp.mjs';

const arg = (ad, v = null) => { const i = process.argv.indexOf(ad); return i > 0 && process.argv[i + 1] ? process.argv[i + 1] : v; };
const ETIKET = process.argv[2] ?? 'fark';
const CDP = Number(arg('--cdp', '9719'));
const PROFIL = arg('--profil', '/private/tmp/kuyumcu-serit/sema-fark-profil');
const VEKIL = arg('--vekil', 'http://localhost:5161');
const ROTALAR = arg('--rotalar', '/,/islemler,/cariler,/ekranlar').split(',');
const W = Number(arg('--genislik', '1440')), H = Number(arg('--yukseklik', '900'));
const SEMALAR = ['platin', 'zumrut', 'grafit', 'bordo', 'okyanus', 'menekse', 'zeytin', 'fildisi'];
const bek = ms => new Promise(r => setTimeout(r, ms));

const b = await baslat(CDP, PROFIL);
let fark = 0, top = 0;
try {
  await b.boyut(W, H);
  await b.git(VEKIL + '/__giris', 4000);
  for (const rota of ROTALAR) {
    await b.git(VEKIL + rota, 6000);
    const kabuk = await b.ev(`!!document.querySelector('.yildiz-kabuk')+'/'+!!document.querySelector('.yz')`);
    if (kabuk !== 'false/false') { console.log('HATA: Yıldız kabuğu hâlâ çiziliyor (kiracı şeması Yıldız DIŞINA alınmalı)', rota, kabuk); process.exit(3); }
    for (const sema of SEMALAR) for (const tema of ['aydinlik', 'karanlik']) {
      let sha = [];
      for (let deneme = 0; deneme < 2 && (deneme === 0 || sha[1] !== sha[2]); deneme++) {
        if (deneme) console.log('YENIDEN', ETIKET, rota, sema, tema, sha.join(' '));
        sha = [];
        for (const kapali of [false, true, false]) {
          await b.ev(`document.documentElement.setAttribute('data-sema','${sema}');document.documentElement.setAttribute('data-tema','${tema}');document.activeElement && document.activeElement.blur();(()=>{const ys=[...document.styleSheets].find(s=>s.href&&s.href.includes('tema-yildiz'));if(!ys) throw new Error('tema-yildiz.css yok');if(!document.getElementById('yz-fontlar')){const st=document.createElement('style');st.id='yz-fontlar';st.textContent=[...ys.cssRules].filter(r=>r.constructor.name==='CSSFontFaceRule').map(r=>r.cssText).join('\\n');document.head.appendChild(st);}ys.disabled=${kapali};return 1})()`);
          await bek(350);
          const r = await b.cmd('Page.captureScreenshot', { format: 'png' });
          sha.push(createHash('sha256').update(r.data).digest('hex').slice(0, 12));
        }
      }
      top++;
      if (sha[1] !== sha[2]) { fark++; console.log('FARK', ETIKET, rota, sema, tema, sha.join(' ')); }
    }
  }
  console.log(`${ETIKET}: ${top} karşılaştırma · FARK ${fark}`);
  process.exitCode = fark ? 1 : 0;
} finally { b.kapat(); }
