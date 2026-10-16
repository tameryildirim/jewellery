// Yeni paket tasarım çıktısını ham/mimari-birlesik.json'a ekler (tasarimlar, oylar, sentez.karar_tablosu) — sonra mimari-render.js + ana-belge-ek.js koşulur.
const fs = require('fs'); const path = require('path');
const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari';
const [,, girdi] = process.argv; if (!girdi) { console.error('kullanım: node yeni-paket-birlestir.js <cikti.json>'); process.exit(2); }
const y = JSON.parse(fs.readFileSync(girdi, 'utf8')); const r = y.result || y;
const bf = path.join(KOK, 'ham/mimari-birlesik.json'); const b = JSON.parse(fs.readFileSync(bf, 'utf8')); const R = b.result;
const yeniNolar = new Set((r.karar_tablosu || []).map(k => String(k.paket_no)));
// aynı katman daha önce eklendiyse değiştir
R.tasarimlar = R.tasarimlar.filter(t => !(t.sonuc && t.sonuc.paketler && t.sonuc.paketler.some(p => yeniNolar.has(String(p.no)))));
R.tasarimlar.push(...(r.tasarimlar || []));
R.oylar = (R.oylar || []).filter(o => !(o.mercek || '').startsWith('YENİ:')).concat((r.oylar || []).map(o => Object.assign({}, o, { mercek: 'YENİ: ' + o.mercek })));
R.sentez.karar_tablosu = (R.sentez.karar_tablosu || []).filter(k => !yeniNolar.has(String(k.paket_no))).concat(r.karar_tablosu || []);
fs.writeFileSync(bf, JSON.stringify(b));
console.log(`birleşik güncellendi: +${(r.karar_tablosu || []).length} paket (${[...yeniNolar].join(', ')}); karar tablosu ${R.sentez.karar_tablosu.length} satır; oylar ${R.oylar.length}`);
