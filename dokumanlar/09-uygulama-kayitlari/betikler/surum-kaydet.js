// SÜRÜM MÜHRÜ — TEK KAYNAK: dokumanlar/09-uygulama-kayitlari/ham/surum.json
//
// P-01 (2026-09-18) mührü: sürümün tek kaynağı ham/surum.json'dur. v1-surum-muhru.js'in
// ürettiği SURUM-MUHRU-V1-2026-09-16.md ve ana belgedeki § MÜHÜR 2-4 satırları TARİHSEL
// TASLAKTIR; çeliştiklerinde surum.json (ve onun beslendiği PLAN-TEK-SEFER-2026-09-18 § 2/§ 5)
// geçerlidir. Bu betik artık dosyayı SIFIRDAN YAZMAZ — mevcut mühürle BİRLEŞTİRİR:
//   * yeni paket satırı eklenir,
//   * mevcut satırın sürümü ancak --uzerine-yaz ile değiştirilir (gerekçe zorunlu),
//   * {1.0, 1.1, KAPSAM-DIŞI} kümesi dışında bir değer (ör. "karar bekliyor") YAZILMAZ
//     → fail-closed, çıkış kodu 3. (denetim.sh: SURUM_KARAR_BEKLEYEN = 0 cırcır sayacı.)
// Kullanım: node surum-kaydet.js <v1-cikti.json> [--uzerine-yaz]
const fs = require('fs'); const path = require('path');
const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari';
const KUME = ['1.0', '1.1', 'KAPSAM-DIŞI'];
const arg = process.argv.slice(2); const girdi = arg.find(a => !a.startsWith('--'));
const uzerineYaz = arg.includes('--uzerine-yaz');
if (!girdi) { console.error('kullanım: node surum-kaydet.js <v1-cikti.json> [--uzerine-yaz]'); process.exit(2); }
const o = JSON.parse(fs.readFileSync(girdi, 'utf8')); const R = o.result || o; const v = R.v1 || R;
const HEDEF = path.join(KOK, 'ham/surum.json');
const m = fs.existsSync(HEDEF) ? JSON.parse(fs.readFileSync(HEDEF, 'utf8')) : {};
const red = []; const eklenen = []; const degisen = []; const korunan = [];
for (const s of (v.surumler || [])) {
  const k = String(s.paket_no);
  if (!KUME.includes(s.surum)) { red.push(`${k} → "${s.surum}"`); continue; }
  if (!m[k]) { m[k] = { surum: s.surum, gerekce: s.gerekce }; eklenen.push(k); }
  else if (m[k].surum !== s.surum) {
    if (uzerineYaz) { m[k] = { surum: s.surum, gerekce: s.gerekce }; degisen.push(`${k}: ${s.surum}`); }
    else korunan.push(`${k} (mühürlü ${m[k].surum} ≠ gelen ${s.surum})`);
  }
}
if (red.length) {
  console.error('REDDEDİLDİ — küme dışı sürüm (yalnız ' + KUME.join(' / ') + '): ' + red.join(' · '));
  console.error('Karar bekleyen satır surum.json\'a YAZILMAZ; kararı önce ekip oyuyla kapat (PLAN § 5).');
  process.exit(3);
}
fs.writeFileSync(HEDEF, JSON.stringify(m, null, 1));
const say = (x) => Object.values(m).filter(s => s.surum === x).length;
console.log(`ham/surum.json: ${Object.keys(m).length} paket — 1.0: ${say('1.0')} · 1.1: ${say('1.1')} · KAPSAM-DIŞI: ${say('KAPSAM-DIŞI')} · küme dışı: ${Object.values(m).filter(s => !KUME.includes(s.surum)).length}`);
console.log(`eklenen ${eklenen.length} · üzerine yazılan ${degisen.length}${degisen.length ? ' [' + degisen.join(', ') + ']' : ''} · mühür korundu ${korunan.length}${korunan.length ? ' [' + korunan.join(', ') + ']' : ''}`);
if (R.mehmet) console.log('Mehmet onayı: ' + R.mehmet.onay + ' · eksik ' + (R.mehmet.eksikler || []).length + ' · veto ' + (R.mehmet.veto || []).length);
