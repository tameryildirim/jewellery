// Arayüz araştırması JSON → Markdown belge
const fs = require('fs');
const [,, girdi, cikti] = process.argv;
const j = JSON.parse(fs.readFileSync(girdi, 'utf8')); const r = j.result || j; const b = r.brif; const T = r.taramalar || [];
const L = [];
L.push('# Arayüz araştırması ve tasarım brifi — Kuyumcu ERP (2026-09-16)');
L.push('');
L.push(`> Kaynak: 4 tarama açısı (kuyumcu yazılımları · sınıfının en iyisi iş uygulamaları · saf CSS tasarım sistemleri · tezgâh gerçekleri) + Opus tasarım brifi. Kaynak sayısı: ${b.kaynak_sayisi}. Ham veri: \`09-uygulama-kayitlari/ham/arayuz-arastirmasi.json\`. Kurucu hedefi: "piyasadaki en güzel, en kullanıcı dostu arayüz; veri girişi olan her yer düzenlenebilir; basit/profesyonel mod". Kısıtlar: saf CSS (vendored dosya olabilir; CDN/npm/inline script yok), Blazor bileşenleri, DOM id sözleşmesi korunur, belirtecler.css tabanı.`);
L.push('');
L.push('## Özet'); L.push(''); L.push(b.ozet); L.push('');
L.push('## Benchmark'); L.push(''); L.push('| Ürün | Kategori | Puan (1-5) | Alınacak ders |'); L.push('|---|---|---|---|');
for (const x of b.benchmark) L.push(`| ${x.urun} | ${x.kategori} | ${x.puan_1_5} | ${x.alinacak_ders.replace(/\|/g, '/')} |`);
L.push('');
L.push('## Görsel yön'); L.push('');
const g = b.gorsel_yon;
L.push(`- **Tipografi:** ${g.tipografi}`); L.push(`- **Renk:** ${g.renk}`); L.push(`- **Yoğunluk ve ızgara:** ${g.yogunluk_ve_izgara}`); L.push(`- **Karanlık tema:** ${g.karanlik_tema}`); L.push(`- **Mobil:** ${g.mobil}`);
L.push(''); L.push('**Bileşenler:**'); for (const x of g.bilesenler) L.push('- ' + x);
L.push(''); L.push('**Ekran şablonları:**'); for (const x of g.ekran_sablonlari) L.push('- ' + x);
L.push(''); L.push('## Düzenleme deseni (UX-1 ile uyum)'); L.push(''); L.push(b.duzenleme_deseni); L.push('');
L.push('## Basit / profesyonel mod görünümü'); L.push(''); L.push(b.basit_pro_mod); L.push('');
L.push('## CSS kiti kararı'); L.push(''); L.push(`**Karar:** ${b.css_kiti_karari.karar}`); L.push(''); L.push(`Gerekçe: ${b.css_kiti_karari.gerekce}`); L.push(''); L.push(`Alternatif: ${b.css_kiti_karari.alternatif}`); L.push('');
L.push('## UX-2 fazlarının yeniden tanımı'); L.push('');
for (const f of b.ux2_fazlari) { L.push(`### ${f.faz} (${f.tahmin_gun})`); L.push(''); L.push(f.kapsam); L.push(''); L.push('Kabul ölçütleri:'); for (const k of f.kabul_olcutleri) L.push('- ' + k); L.push(''); }
L.push('## Kullanıcı dostu ilkeler'); L.push(''); for (const x of b.kullanici_dostu_ilkeler) L.push('- ' + x); L.push('');
L.push('## Riskler'); L.push(''); for (const x of b.riskler) L.push('- ' + x); L.push('');
L.push('## Tarama ayrıntıları'); L.push('');
for (const t of T) {
  L.push(`### Açı: ${(t.notlar && t.notlar[0]) ? '' : ''}${t.urunler.length} ürün · ${t.desenler.length} desen · ${t.kaynaklar.length} kaynak`); L.push('');
  L.push('| Ürün/Kit | Hedef | Puan | Güçlü | Zayıf | Kanıt |'); L.push('|---|---|---|---|---|---|');
  for (const u of t.urunler) L.push(`| ${u.ad} | ${(u.hedef || '').replace(/\|/g, '/')} | ${u.puan_1_5} | ${u.arayuz_gucleri.slice(0, 3).join('; ').replace(/\|/g, '/')} | ${u.arayuz_zayifliklari.slice(0, 2).join('; ').replace(/\|/g, '/')} | ${u.kanit_url} |`);
  L.push(''); L.push('Desenler:'); for (const d of t.desenler) L.push(`- **${d.desen}** — ${d.aciklama} (örnek: ${d.ornek}) → Kuyumcu ERP: ${d.kuyumcu_erp_uygulamasi}`);
  L.push(''); L.push('Notlar:'); for (const n of t.notlar) L.push('- ' + n);
  L.push(''); L.push('Kaynaklar:'); for (const k of t.kaynaklar) L.push(`- [${k.baslik}](${k.url}) — güven: ${k.guven}${k.tarih_veya_surum ? ' — ' + k.tarih_veya_surum : ''}`);
  L.push('');
}
fs.writeFileSync(cikti, L.join('\n'));
console.log(`yazıldı: ${cikti} (${L.length} satır)`);
