// Hazırlık kurulu JSON çıktısı → HAZIRLIK-KURULU-<tur>.md
const fs = require('fs'); const path = require('path');
const [,, girdi, hedefDizin] = process.argv;
if (!girdi) { console.error('kullanım: node kurul-render.js <kurul.json> [dizin]'); process.exit(2); }
const j = JSON.parse(fs.readFileSync(girdi, 'utf8')); const r = j.result || j; const m = r.muhur; const D = r.degerlendirmeler || [];
const tur = r.tur || 1; const L = [];
L.push(`# Satışa hazırlık kurulu — tur ${tur} (2026-09-16)`); L.push('');
L.push(`> 7 mercek (Eray satış · Köprü teslimat · Aşkın QA · Mehmet güvenlik/uyum · Bertan mimari · Mustafa FinOps · Ramazan-ajan strateji) + Tamer Bey mührü. Kural: HAZIR yalnız herkes hazır ve ortalama ≥ 8. Ham veri: \`ham/hazirlik-kurulu-${tur}.json\`.`); L.push('');
L.push(`## KARAR: ${m.karar} — ortalama ${m.ortalama_puan}/10`); L.push(''); L.push(m.ozet); L.push('');
L.push('| Mercek | Puan | Hazır | Engel sayısı |'); L.push('|---|---|---|---|');
for (const d of D) L.push(`| ${d.mercek.split(' — ')[0]} | ${d.puan_0_10} | ${d.hazir ? 'EVET' : 'HAYIR'} | ${d.engeller.length} |`);
L.push(''); L.push('## Öncelikli engeller (tekilleştirilmiş, mühür)'); L.push(''); L.push('| # | Engel | Mercekler | Ciddiyet | Çözüm paketi | Dalga |'); L.push('|---|---|---|---|---|---|');
m.engeller_oncelikli.forEach((e, i) => L.push(`| ${i + 1} | ${e.baslik.replace(/\|/g, '/')} | ${e.mercekler.join(', ')} | ${e.ciddiyet} | ${e.cozum_paketi.replace(/\|/g, '/')} | ${e.dalga} |`));
L.push(''); L.push('## Satışa hazırlık asgarisi (mühür)'); L.push(''); m.satisa_hazirlik_asgarisi.forEach(x => L.push('- ' + x));
L.push(''); L.push('## Sonraki adımlar'); L.push(''); m.sonraki_adimlar.forEach(x => L.push('- ' + x));
L.push(''); L.push('## Mercek değerlendirmeleri'); L.push('');
for (const d of D) {
  L.push(`### ${d.mercek} — ${d.puan_0_10}/10 — ${d.hazir ? 'HAZIR' : 'HAZIR DEĞİL'}`); L.push('');
  L.push('**Gerçekçi örnek:** ' + d.gercekci_ornek); L.push('');
  L.push('Güçlü yanlar:'); d.guclu_yanlar.forEach(x => L.push('- ' + x)); L.push('');
  L.push('Engeller:'); d.engeller.forEach(e => L.push(`- **[${e.ciddiyet}] ${e.baslik}** — ${e.kanit} → ${e.cozum_paketi}${e.tahmin ? ' (' + e.tahmin + ')' : ''}`)); L.push('');
  if (d.notlar && d.notlar.length) { L.push('Notlar:'); d.notlar.forEach(x => L.push('- ' + x)); L.push(''); }
}
const cikti = path.join(hedefDizin || path.dirname(girdi) + '/..', `HAZIRLIK-KURULU-${tur}.md`);
fs.writeFileSync(cikti, L.join('\n')); console.log('yazıldı: ' + cikti + ' (' + L.length + ' satır) — KARAR: ' + m.karar + ' ort ' + m.ortalama_puan);
