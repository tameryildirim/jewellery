// kurul<tur>-args.json içindeki "<… DOLDUR …>" yer tutucularını gerçek ölçümlerle doldurur.
// Kullanım: node betikler/kurul-args-doldur.js <tur> <kapi-dizini> ["dalga durumu metni"]
// Kaynaklar: ham/qa-turu-<tur>.json (qa-turu-kaydet.js çıktısı), <kapi-dizini>/test.log, CLAUDE.md Durum satırı (damgala.js)
const fs = require('fs'); const path = require('path');
const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari';
const [,, turS, kapi, dalgaMetni] = process.argv; const tur = +turS;
if (!tur || !kapi) { console.error('kullanım: node kurul-args-doldur.js <tur> <kapi-dizini> ["dalga durumu"]'); process.exit(2); }
const af = path.join(KOK, 'betikler', `kurul${tur}-args.json`); const a = JSON.parse(fs.readFileSync(af, 'utf8'));
const q = JSON.parse(fs.readFileSync(path.join(KOK, 'ham', `qa-turu-${tur}.json`), 'utf8'));
const R = Object.values(q.rotalar); const n = R.length;
const say = (f) => R.reduce((t, r) => t + f(r), 0);
const bulgu = say(r => r.bulgular.length), kritik = say(r => r.bulgular.filter(b => /KRIT/i.test(b.ciddiyet)).length), duz = say(r => r.duzenlenemeyen.length);
const onceki = R.flatMap(r => r.onceki_bulgular || []);
const kap = onceki.filter(o => /KAPANDI/i.test(o.durum)).length, kis = onceki.filter(o => /KISMEN/i.test(o.durum)).length, acik = onceki.length - kap - kis;
const ort = (f) => (say(f) / n).toFixed(2); const gorsel = ort(r => r.gorsel), dostu = ort(r => r.kullanici_dostu);
const testLog = fs.readFileSync(path.join(kapi, 'test.log'), 'utf8'); const m = testLog.match(/Başarısız:\s*(\d+),\s*Başarılı:\s*(\d+)/);
if (!m) { console.error('test.log içinde özet satırı yok'); process.exit(1); }
const durum = (fs.readFileSync(path.join(KOK, '../../CLAUDE.md'), 'utf8').match(/^Durum \([^)]*\): ([^\n]*)/m) || [])[1] || '(damga satırı yok)';
const dosyalar = fs.readdirSync(kapi).join(', ');
a.kanitlar.qa_turu_cizelgesi = `${KOK}/QA-TARAYICI-TURU-2026-09-16-tur${tur}.md (tur ${tur}: ${n} sayfa, ${bulgu} bulgu, ${kritik} kritik, ${duz} düzenlenemeyen veri girişi; önceki tur bulguları KAPANDI ${kap} / KISMEN ${kis} / AÇIK ${acik}; ort görsel ${gorsel}/5, kullanıcı dostu ${dostu}/5 — dar tarayıcı bölmesinde ölçüldü) + ${KOK}/ham/qa-turu-${tur}.json (rota bazlı ham bulgular)`;
a.kanitlar.kapi_kayitlari = `${kapi}/ (${dosyalar}): tam süit ${m[2]}/${m[2]}, ${m[1]} başarısız; CLAUDE.md damgası: ${durum}`;
if (dalgaMetni) a.kanitlar.dalga_durumu = dalgaMetni; else a.kanitlar.dalga_durumu = String(a.kanitlar.dalga_durumu).replace(/^<[^:]*:\s*/, '').replace(/>$/, '');
for (const [k, v] of Object.entries(a.kanitlar)) if (/DOLDUR/.test(String(v))) console.log('UYARI: hâlâ yer tutucu: ' + k);
fs.writeFileSync(af, JSON.stringify(a, null, 1));
console.log(`kurul${tur}-args.json dolduruldu: ${n} sayfa · ${bulgu} bulgu (${kritik} kritik) · düzenlenemeyen ${duz} · önceki KAPANDI ${kap}/KISMEN ${kis}/AÇIK ${acik} · görsel ${gorsel} / dostu ${dostu} · test ${m[2]}`);
