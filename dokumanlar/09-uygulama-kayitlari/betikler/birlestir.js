// Parçalı sentez çıktısını üç tasarım/oy dosyasıyla birleştirir → ham/mimari-birlesik.json
// Kullanım: node betikler/birlestir.js <sentez-cikti.json>   (workflow task output ya da {sentez:{...}})
const fs = require('fs'); const path = require('path');
const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari';
const [,, sentezYolu] = process.argv;
if (!sentezYolu) { console.error('kullanım: node birlestir.js <sentez.json>'); process.exit(2); }
const oku = f => JSON.parse(fs.readFileSync(f, 'utf8'));
const a = oku(path.join(KOK, 'ham/mimari-kismi-K0-K5-K6.json')).result;
const b = oku(path.join(KOK, 'ham/mimari-eksik-16.json')).result;
const sRaw = oku(sentezYolu);
const sentez = (sRaw.result && sRaw.result.sentez) || sRaw.sentez || sRaw;
if (!sentez.karar_tablosu || !sentez.dalgalar) { console.error('sentez.karar_tablosu / dalgalar yok'); process.exit(1); }
const tasarimlar = a.tasarimlar.filter(t => t.sonuc).concat(b.tasarimlar.filter(t => t.sonuc));
const oylar = (a.oylar || []).concat(b.oylar || []);
const paketNolari = tasarimlar.flatMap(t => (t.sonuc.paketler || []).map(p => String(p.no)));
const kararNolari = sentez.karar_tablosu.map(k => String(k.paket_no));
const eksik = paketNolari.filter(n => !kararNolari.includes(n));
fs.writeFileSync(path.join(KOK, 'ham/mimari-birlesik.json'), JSON.stringify({ result: { tasarimlar, oylar, sentez } }));
console.log(`birleşik yazıldı: ${tasarimlar.length} tasarım girdisi (${paketNolari.length} paket), ${oylar.length} mercek, karar tablosu ${kararNolari.length} satır, ${sentez.dalgalar.length} dalga`);
if (eksik.length) console.log('UYARI karar tablosunda eksik paketler: ' + eksik.join(', '));
const uxEksik = ['UX-1', 'UX-2', 'UX-3'].filter(n => !kararNolari.includes(n));
if (uxEksik.length) console.log('UYARI UX kararı eksik: ' + uxEksik.join(', '));
