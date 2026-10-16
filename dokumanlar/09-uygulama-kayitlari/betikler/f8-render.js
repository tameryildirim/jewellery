// F-8 çıktısı → ../02-mimari/F-8-BIRIM-EKONOMI-2026-09-16.md + ham/f8-birim-ekonomi.json (tek gerçek kaynak: MD tablosu JSON'dan türer)
const fs = require('fs'); const path = require('path');
const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari';
const [,, girdi] = process.argv; if (!girdi) { console.error('kullanım: node f8-render.js <cikti.json>'); process.exit(2); }
const j = JSON.parse(fs.readFileSync(girdi, 'utf8')); const r = j.result || j; const s = r.sentez; const T = r.taramalar || [];
const kalemler = s.maliyet_tablosu.map(k => ({ ad: k.ad, sinif: k.sinif, tutar: k.tutar, para_birimi: k.para_birimi, donem: k.donem, kiraci_basi_aylik_tl: k.kiraci_basi_aylik_tl, alt_olasi_ust: k.alt_olasi_ust, kaynak: k.kaynak, tarih: k.tarih, guven: k.guven }));
fs.writeFileSync(path.join(KOK, 'ham/f8-birim-ekonomi.json'), JSON.stringify({ uretim: '2026-09-16', kalemler, kiraci_basi_maliyet: s.kiraci_basi_maliyet, fiyat_kademeleri_onerisi: s.fiyat_kademeleri_onerisi, rakip_kiyasi: s.rakip_kiyasi, dogrulanmali: s.dogrulanmali }, null, 1));
const tahmin = kalemler.filter(k => /TAHM|OLCULMEDI/i.test(k.guven)); const kaynakli = kalemler.filter(k => !/TAHM|OLCULMEDI/i.test(k.guven));
const L = [];
L.push('# F-8 — Birim ekonomi ve fiyatlandırma dosyası (2026-09-16, Faz A; ÖNERİ — fiyat ilanı DEĞİL)'); L.push('');
L.push('> Mustafa (09 FinOps) + Eray (08 Satış) araştırması; tek gerçek kaynak `09-uygulama-kayitlari/ham/f8-birim-ekonomi.json` (bu tablo ondan türer). MÜHÜR 2: bu dosya dolmadan hiçbir kanalda rakamlı fiyat açıklanmaz; kurucu onayı olmadan ilan yok. Sözleşme numarası/temsilci bilgisi yazılmaz. TAHMİN satırları toplamdan ayrı gösterilir.'); L.push('');
L.push('## Özet'); L.push(''); L.push(s.ozet); L.push('');
L.push(`## Maliyet tablosu — kaynaklı satırlar (${kaynakli.length})`); L.push(''); L.push('| Kalem | Sınıf | Tutar | Dönem | Kiracı başı aylık TL | Belirsizlik (alt/olası/üst) | Kaynak | Tarih | Güven |'); L.push('|---|---|---|---|---|---|---|---|---|');
for (const k of kaynakli) L.push(`| ${k.ad} | ${k.sinif} | ${k.tutar} ${k.para_birimi} | ${k.donem} | ${k.kiraci_basi_aylik_tl} | ${k.alt_olasi_ust} | ${k.kaynak} | ${k.tarih} | ${k.guven} |`);
L.push(''); L.push(`## Maliyet tablosu — TAHMİN / ÖLÇÜLMEDİ satırları (${tahmin.length}; toplamdan ayrı)`); L.push(''); L.push('| Kalem | Sınıf | Tutar | Dönem | Kiracı başı aylık TL | Belirsizlik | Dayanak | Güven |'); L.push('|---|---|---|---|---|---|---|---|');
for (const k of tahmin) L.push(`| ${k.ad} | ${k.sinif} | ${k.tutar} ${k.para_birimi} | ${k.donem} | ${k.kiraci_basi_aylik_tl} | ${k.alt_olasi_ust} | ${k.kaynak} | ${k.guven} |`);
L.push(''); L.push('## Kiracı başı maliyet (10 / 50 / 100 kiracı)'); L.push(''); L.push(s.kiraci_basi_maliyet); L.push('');
L.push('## Fiyat kademeleri ÖNERİSİ (ilan değil; kurucu onayı gerekir)'); L.push(''); L.push('| Kademe | Kapsam | Aylık TL önerisi | Gerekçe |'); L.push('|---|---|---|---|');
for (const k of s.fiyat_kademeleri_onerisi) L.push(`| ${k.kademe} | ${k.kapsam.replace(/\|/g, '/')} | ${k.aylik_tl_onerisi} | ${k.gerekce.replace(/\|/g, '/')} |`);
L.push(''); L.push('## Rakip kıyası (yalnız birincil kaynak)'); L.push(''); L.push('| Rakip | Fiyat | Kanıt | Güven |'); L.push('|---|---|---|---|');
for (const k of s.rakip_kiyasi) L.push(`| ${k.rakip} | ${k.fiyat} | ${k.kanit} | ${k.guven} |`);
L.push(''); L.push('## Kurucunun tasarruf hedefi — dürüst okuma'); L.push(''); L.push(s.tasarruf_hedefi_durust); L.push('');
L.push('## Entegratör önerisi'); L.push(''); L.push(s.entegrator_onerisi); L.push('');
L.push('## Kurucuya imza/para kalemleri'); L.push(''); s.kurucuya_kalemler.forEach(x => L.push('- ' + x)); L.push('');
L.push('## DOĞRULANMALI'); L.push(''); s.dogrulanmali.forEach(x => L.push('- ' + x)); L.push('');
L.push('## Riskler'); L.push(''); s.riskler.forEach(x => L.push('- ' + x)); L.push('');
L.push('## Araştırma kaynakları'); L.push('');
for (const t of T) { for (const k of t.kaynaklar) L.push(`- [${k.baslik}](${k.url}) — güven: ${k.guven}`); }
const cikti = path.join(KOK, '../02-mimari/F-8-BIRIM-EKONOMI-2026-09-16.md');
fs.writeFileSync(cikti, L.join('\n')); console.log(`yazıldı: ${cikti} (${L.length} satır; kaynaklı ${kaynakli.length}, tahmin ${tahmin.length})`);
