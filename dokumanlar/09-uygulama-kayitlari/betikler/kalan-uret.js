// dalga-uygula.js'ten args'ı GÖMÜLÜ tek seferlik koşum betiği üretir (Workflow scriptPath ile; args gerekmez)
// kullanım: node betikler/kalan-uret.js <args.json> <cikti.js> <atlanacak-paketler>
// NOT: bu betik daha önce oturum geçici dizinindeydi ve oturum değişince kayboldu (2026-09-21).
//      Kalıcı olsun diye betikler/ altına alındı — sürümlenmemiş araç bırakma dersi (mut.sh ile aynı hata).
const fs = require('fs');
const [, , argsYol, cikti, atla] = process.argv;
const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/';
const a = JSON.parse(fs.readFileSync(argsYol, 'utf8'));
const atlaSet = new Set((atla || '').split(',').map(x => x.trim()).filter(Boolean));
a.paketler = a.paketler.filter(p => !atlaSet.has(String(p.no)));
let src = fs.readFileSync(KOK + 'dalga-uygula.js', 'utf8');
const sayim = (src.match(/\bargs\./g) || []).length;
src = src.replace(/\bargs\./g, 'GOMULU.');
// meta literalinin SONUNU süslü parantez dengesiyle bul
const bas = src.indexOf('export const meta'); if (bas < 0) throw new Error('meta yok');
let d = 0, i = src.indexOf('{', bas), son = -1;
for (let j = i; j < src.length; j++) { const c = src[j]; if (c === '{') d++; else if (c === '}') { d--; if (d === 0) { son = j; break; } } }
if (son < 0) throw new Error('meta kapanışı yok');
const nl = src.indexOf('\n', son);
src = src.slice(0, nl + 1) + 'const GOMULU = ' + JSON.stringify(a) + '\n' + src.slice(nl + 1);
fs.writeFileSync(cikti, src);
console.log('üretildi: ' + cikti + ' (' + src.length + ' bayt) · paketler: ' + a.paketler.map(p => p.no).join(' → ') + ' · args→GOMULU: ' + sayim);
