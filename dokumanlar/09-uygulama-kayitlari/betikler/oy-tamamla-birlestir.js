// oy-tamamla çıktısını ham/yeni-paketler.json'a işler (ilgili paketlerin oylarını ve karar satırlarını değiştirir) → sonra yeni-paket-birlestir.js + render + ana-belge-ek.js
const fs = require('fs'); const path = require('path');
const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari';
const [,, girdi] = process.argv; if (!girdi) { console.error('kullanım: node oy-tamamla-birlestir.js <cikti.json>'); process.exit(2); }
const o = JSON.parse(fs.readFileSync(girdi, 'utf8')); const R = o.result || o;
const yf = path.join(KOK, 'ham/yeni-paketler.json'); const y = JSON.parse(fs.readFileSync(yf, 'utf8')); const Y = y.result || y;
const nolar = new Set(R.karar_tablosu.map(k => String(k.paket_no)));
for (const oy of Y.oylar) { oy.oylar = oy.oylar.filter(x => !nolar.has(String(x.paket_no))); oy.veto = (oy.veto || []).filter(x => !nolar.has(String(x.paket_no))); }
for (const oy of R.oylar) { const hedef = Y.oylar.find(x => x.mercek.split(' ')[0].toLowerCase() === oy.mercek.split(' ')[0].toLowerCase()); if (hedef) { hedef.oylar.push(...oy.oylar); hedef.veto.push(...oy.veto); hedef.capraz_bulgular.push(...oy.capraz_bulgular); } else Y.oylar.push(oy); }
Y.karar_tablosu = Y.karar_tablosu.filter(k => !nolar.has(String(k.paket_no))).concat(R.karar_tablosu);
Y.tasarimlar[0].sonuc.ortak_kararlar = Y.tasarimlar[0].sonuc.ortak_kararlar.map(k => k.startsWith('USUL MÜHRÜ') ? 'USUL MÜHRÜ KAPANDI (2. tur): T-4, F-2-min ve E-BELGE tam metinle yeniden oylandı; karar satırları teknik mühürdür. ' + (R.ozet || '') : k);
fs.writeFileSync(yf, JSON.stringify(y));
console.log('yeni-paketler.json güncellendi: ' + [...nolar].join(', ') + ' — karar tablosu ' + Y.karar_tablosu.length + ' satır');
