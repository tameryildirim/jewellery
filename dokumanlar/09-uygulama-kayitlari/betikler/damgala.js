// Kapı sonrası belge damgası: CLAUDE.md §3 ve SONRAKI §1 DURUM satırındaki test/migration sayılarını gerçek değerlerle günceller (Madde 1 belge kayması).
const fs = require('fs'); const path = require('path');
const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery';
const [,, testSayisi, etiket] = process.argv; if (!testSayisi) { console.error('kullanım: node damgala.js <test-sayısı> [etiket]'); process.exit(2); }
const migDir = path.join(KOK, 'dotnet/Kuyumcu.Api/Data/Migrations');
const mig = fs.readdirSync(migDir).filter(f => /^\d{14}_.*\.cs$/.test(f) && !f.endsWith('.Designer.cs') && fs.statSync(path.join(migDir, f)).size > 0).length;
const simdi = new Date(); const damga = `${simdi.getFullYear()}-${String(simdi.getMonth() + 1).padStart(2, '0')}-${String(simdi.getDate()).padStart(2, '0')} ${String(simdi.getHours()).padStart(2, '0')}:${String(simdi.getMinutes()).padStart(2, '0')}`;
function guncelle(dosya, rx, yeni) { let s = fs.readFileSync(dosya, 'utf8'); const oncesi = s; s = s.replace(rx, yeni); fs.writeFileSync(dosya, s); console.log(path.basename(dosya) + ': ' + (s === oncesi ? 'DEĞİŞMEDİ (işaret bulunamadı)' : 'damgalandı')); }
guncelle(path.join(KOK, 'CLAUDE.md'), /Durum \([^)]*\): \d+\/\d+ test yeşil · dotnet\/denetim\.sh TEMİZ · \d+ migration · 0 uyarı 0 hata\./, `Durum (${damga} kapısı${etiket ? ', ' + etiket : ''}): ${testSayisi}/${testSayisi} test yeşil · dotnet/denetim.sh TEMİZ · ${mig} migration · 0 uyarı 0 hata.`);
guncelle(path.join(KOK, 'SONRAKI-OTURUM-PROMPT.md'), /^DURUM \([^)]*\): \d+\/\d+ test yeşil[^\n]*$/m, `DURUM (${damga}, ${etiket || 'orkestratör kapısı'}): ${testSayisi}/${testSayisi} test yeşil (orkestratör kapısı, trx kayıtlı) · dotnet/denetim.sh TEMİZ · ${mig} migration · 0 uyarı 0 hata · commit yok. Kalan işin tek kaynağı: dokumanlar/09-uygulama-kayitlari/ORKESTRATOR-DURUM.md (§ A18 MÜHÜR 2 dalga planı).`);
console.log(`damga: ${damga} · test ${testSayisi} · migration ${mig}`);
