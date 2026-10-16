// mimari-render.js ana belgeyi yeniden yazdıktan sonra: Ek UX sözleşmesi (UX-S21…S33) + UX-4/49 karar satırlarını geri ekler.
const fs = require('fs'); const path = require('path');
const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari';
const ana = path.join(KOK, '../02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md');
let md = fs.readFileSync(ana, 'utf8');
const ek = fs.readFileSync(path.join(KOK, 'ham/ux-ek-sozlesme.md'), 'utf8').trimEnd();
if (!md.includes('### Ek UX sözleşmesi')) {
  if (!md.includes('\n## Kurucuya kalanlar')) { console.error('"## Kurucuya kalanlar" başlığı yok'); process.exit(1); }
  md = md.replace('\n## Kurucuya kalanlar', '\n' + ek + '\n\n## Kurucuya kalanlar');
}
const satirlar = [
  '| UX-4 | Mantık ve tutarlılık düzeltmeleri (140 bulgu; 14 hemen düzeltme, üç alt dalgada: 4a madde 1-5 · 4b 6-9 · 4c 10-14) | MÜHÜRLÜ (4 karar oylandı, 2026-09-16 09:30) | 2 (baş) | belgede madde başına | 33 | [PAKET-UX-4-mantik-ve-tutarlilik-duzeltmeleri.md](kalan-paketler/PAKET-UX-4-mantik-ve-tutarlilik-duzeltmeleri.md) |',
  '| 49 | Fiyat listesi = ürün görünümü (toplu/tekil marj, geçersiz kılma, düzenleme) | MÜHÜRLÜ (2026-09-16 09:30) | 2 (UX-4a sonrası) | 4-6 | UX-4 madde 1-2 | [PAKET-49-fiyat-listesi-urun-gorunumu.md](kalan-paketler/PAKET-49-fiyat-listesi-urun-gorunumu.md) |'
];
if (!md.includes('| UX-4 |')) {
  if (!md.includes('\n\n## Uygulama dalgaları')) { console.error('"## Uygulama dalgaları" başlığı yok'); process.exit(1); }
  md = md.replace('\n\n## Uygulama dalgaları', '\n' + satirlar.join('\n') + '\n\n## Uygulama dalgaları');
}

// PLAN BÖLÜMLERİ (render her seferinde siler; buradan geri eklenir — sıra: ESKİ işareti → REVİZE → brif mühür eki → MÜHÜR 2 → MÜHÜR 3 yeni paketler)
md = md.replace('\n## Uygulama dalgaları (yaz-seri yasası: dalga içinde SERİ)\n', '\n## Uygulama dalgaları (yaz-seri yasası: dalga içinde SERİ) — ESKİ (2026-09-16 11:15 sentezi; MÜHÜR 2 ile DEĞİŞTİRİLDİ, yalnız tarihçe)\n');
const ekler = [
  ['## Uygulama dalgaları — REVİZE', 'ham/dalga-plani-revize.md', t => t.replace(/^## Uygulama dalgaları — REVİZE[^\n]*/m, '## Uygulama dalgaları — REVİZE (12:12 mührü; MÜHÜR 2 ile DEĞİŞTİRİLDİ, yalnız tarihçe)')],
  ['### Mühür eki (2026-09-16 12:40', 'ham/brif-muhur-eki.md', t => t],
  ['## Uygulama dalgaları — MÜHÜR 2', 'ham/dalga-plani-muhur2.md', t => t],
  ['## Yeni paketler — MÜHÜR 3', 'ham/yeni-paketler-ortak.md', t => t],
  ['### MÜHÜR 4 — kod öncesi açık kararlar (2026-09-16 14:56)', 'ham/muhur4.md', t => t],
  ['### MÜHÜR 5 — kurul tur 2 sonrası dalga planı revizyonu (2026-09-16 17:36)', 'ham/muhur5.md', t => t],
  ['### MÜHÜR 6 — 3-P taslakları (T-3 yedek betikleri, F-6/F-7 üretim profili) (2026-09-16)', 'ham/muhur6.md', t => t],
];
for (const [isaret, dosya, donustur] of ekler) {
  const yol = path.join(KOK, dosya);
  if (!md.includes(isaret) && fs.existsSync(yol)) { md += '\n' + donustur(fs.readFileSync(yol, 'utf8')).trimEnd() + '\n'; console.log('eklendi: ' + dosya); }
}

md = md.replace(/^> Kaynak: 6 katman mimarı/m, '> Kaynak: 6 katman mimarı (11 paket, 2026-09-15) + 16 paket için paket başına mimar (2026-09-16 sabahı)');
fs.writeFileSync(ana, md);
console.log('ana belge tamamlandı: ek sözleşme ' + (md.includes('### Ek UX sözleşmesi') ? 'var' : 'YOK') + ', UX-4/49 satırları ' + (md.includes('| UX-4 |') ? 'var' : 'YOK'));

// Paket belgesi ekleri (kendini onarır): ham/ek-PAKET-<no>.md → aynı önekli kalan-paketler belgesine, ilk başlık satırı yoksa sona eklenir (render sonrası kaybolan ekler için)
{
  const kp = path.join(KOK, '../02-mimari/kalan-paketler');
  for (const ekAd of fs.readdirSync(path.join(KOK, 'ham')).filter(f => /^ek-PAKET-.*\.md$/.test(f)).sort()) {
    const metin = fs.readFileSync(path.join(KOK, 'ham', ekAd), 'utf8').trimEnd();
    const baslik = metin.split('\n').find(s => s.startsWith('#'));
    const onek = ekAd.replace(/^ek-/, '').replace(/\.md$/, '').replace(/\.\d+$/, '');
    const hedefAd = fs.readdirSync(kp).find(f => f.startsWith(onek + '-') || f === onek + '.md');
    if (!hedefAd) { console.log('ek hedefi yok: ' + ekAd); continue; }
    const hf = path.join(kp, hedefAd); const mevcut = fs.readFileSync(hf, 'utf8');
    if (baslik && mevcut.includes(baslik)) { console.log('ek zaten var: ' + hedefAd); continue; }
    fs.writeFileSync(hf, mevcut.trimEnd() + '\n\n' + metin + '\n'); console.log('ek eklendi: ' + hedefAd + ' ← ' + ekAd);
  }
}
