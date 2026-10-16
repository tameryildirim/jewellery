// QA turu iş akışı çıktısı → ham/qa-turu-<tur>.json (rota bazlı bulgular; sonraki tur ajanları bunu okuyup her bulguyu yeniden dener)
const fs = require('fs'); const path = require('path');
const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari';
const [,, girdi] = process.argv; if (!girdi) { console.error('kullanım: node qa-turu-kaydet.js <cikti.json>'); process.exit(2); }
const o = JSON.parse(fs.readFileSync(girdi, 'utf8')); const R = o.result || o;
if (!R.sonuclar) { console.error('sonuclar yok'); process.exit(1); }
const rotalar = {};
for (const g of R.sonuclar) for (const s of g.sayfalar) rotalar[s.rota] = { grup: g.grup, durum: s.durum, konsol_ag: s.konsol_ag, gorsel: s.gorsel_puan_1_5, kullanici_dostu: s.kullanici_dostu_puan_1_5, duzenlenemeyen: s.duzenlenemeyen_veri_girisleri, bulgular: s.bulgular, onceki_bulgular: s.onceki_bulgular || [] };
const cikti = { tur: R.tur, uretim: '2026-09-16', fikstur: R.fikstur, genel_notlar: R.sonuclar.flatMap(g => g.genel_notlar.map(n => 'grup ' + g.grup + ': ' + n)), ozet: R.ozet, rotalar };
const hedef = path.join(KOK, 'ham', `qa-turu-${R.tur}.json`); fs.writeFileSync(hedef, JSON.stringify(cikti, null, 1));
const b = Object.values(rotalar).reduce((t, r) => t + r.bulgular.length, 0); const k = Object.values(rotalar).reduce((t, r) => t + r.bulgular.filter(x => /KRIT/i.test(x.ciddiyet)).length, 0);
console.log(`yazıldı: ${hedef} — ${Object.keys(rotalar).length} rota, ${b} bulgu (${k} kritik), düzenlenemeyen ${Object.values(rotalar).reduce((t, r) => t + r.duzenlenemeyen.length, 0)}`);
