// Mimari iş akışı JSON çıktısını Markdown belgelerine döker.
// Kullanım: node mimari-render.js <workflow-output.json> <hedef-dizin> [<ux-output.json>]
const fs = require('fs');
const path = require('path');
const [,, girdi, hedef, uxGirdi] = process.argv;
if (!girdi || !hedef) { console.error('kullanım: node mimari-render.js <json> <dizin> [<ux-json>]'); process.exit(2); }
const veri = JSON.parse(fs.readFileSync(girdi, 'utf8'));
const r = veri.result || veri;
fs.mkdirSync(hedef, { recursive: true });

const slug = s => s.toLowerCase()
  .replace(/ç/g, 'c').replace(/ğ/g, 'g').replace(/ı/g, 'i').replace(/ö/g, 'o').replace(/ş/g, 's').replace(/ü/g, 'u')
  .replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '').slice(0, 48);
const liste = (arr, f) => (arr && arr.length) ? arr.map(x => '- ' + (f ? f(x) : x)).join('\n') : '- (yok)';
const kod = s => '`' + s + '`';
const pad = n => String(n).padStart(2, '0');

// Oylar: paket_no → [{mercek, oy, kosullar, gerekce}]
const oyHaritasi = {};
for (const o of (r.oylar || [])) for (const oy of (o.oylar || [])) {
  const k = String(oy.paket_no);
  (oyHaritasi[k] = oyHaritasi[k] || []).push({ mercek: o.mercek, ...oy });
}
const vetolar = {};
for (const o of (r.oylar || [])) for (const v of (o.veto || [])) (vetolar[String(v.paket_no)] = vetolar[String(v.paket_no)] || []).push(`${o.mercek}: ${v.neden}`);
const kararlar = {};
for (const k of ((r.sentez && r.sentez.karar_tablosu) || [])) kararlar[String(k.paket_no)] = k;
// Sürüm (V-1 sürüm mührü): ham/surum.json = { paket_no: { surum, gerekce } } — yoksa sütun '-' kalır
const surumYolu = path.join(path.dirname(girdi), 'surum.json');
const surumler = fs.existsSync(surumYolu) ? JSON.parse(fs.readFileSync(surumYolu, 'utf8')) : {};
// Dalga (MÜHÜR 5 sıraları): ham/dalga.json = { paket_no: { dalga } } — yoksa sentezdeki dalga kalır
const dalgaYolu = path.join(path.dirname(girdi), 'dalga.json');
const dalgalar = fs.existsSync(dalgaYolu) ? JSON.parse(fs.readFileSync(dalgaYolu, 'utf8')) : {};
const dalgaOf = (no, varsayilan) => { const d = dalgalar[String(no)]; return d ? d.dalga : varsayilan; };
const surumOf = (no) => { const s = surumler[String(no)] || surumler[String(no).replace(/-A$/, '')] ; return s ? s.surum : '-'; };

function paketBelgesi(p, katmanAd, kaynak) {
  const no = String(p.no);
  const karar = kararlar[no];
  const oylar = oyHaritasi[no] || [];
  const dosyaAdi = `PAKET-${/^\d+$/.test(no) ? pad(no) : no}-${slug(p.ad)}.md`;
  const md = `# Paket ${no} — ${p.ad}

> Katman: ${katmanAd} · Tahmin: ${p.tahmin_gun || '-'} gün · Önkoşullar: ${(p.onkosul_paketler || []).join(', ') || 'yok'} · Kaynak: ${kaynak} · Tarih: 2026-09-15
> **Karar (Tamer Bey mührü):** ${karar ? `${karar.sonuc} · Dalga ${karar.dalga}` : 'sentez bekleniyor'}
${vetolar[no] ? `> ⚠ **VETO:** ${vetolar[no].join(' | ')}\n` : ''}
## Hedef
${p.hedef}

## Kodda bugünkü durum
${p.mevcut_durum_kodda}

## Kapsam
**İçinde:**
${liste(p.kapsam_ici)}

**Dışında:**
${liste(p.kapsam_disi)}

## Veri modeli
${(p.veri_modeli && p.veri_modeli.length) ? '| Tablo | Değişiklik | Kolonlar | İndeksler | Migration |\n|---|---|---|---|---|\n' + p.veri_modeli.map(v => `| ${v.tablo} | ${v.degisiklik} | ${v.kolonlar || '-'} | ${v.indeksler || '-'} | ${v.migration_adi || '-'} |`).join('\n') : '- Şema değişikliği yok'}

## Uçlar
${(p.uclar && p.uclar.length) ? '| Fiil | Yol | Roller | Açıklama | Kapılar |\n|---|---|---|---|---|\n' + p.uclar.map(u => `| ${u.fiil} | ${kod(u.yol)} | ${u.roller || 'Authorize'} | ${u.aciklama} | ${u.kapilar || '-'} |`).join('\n') : '- Yeni uç yok'}

## Servis ve dosyalar
${liste(p.servis_ve_dosyalar, d => `**${d.islem}** ${kod(d.dosya)} — ${d.icerik}`)}

## Ekranlar
${liste(p.ekranlar, e => `${kod(e.rota)} · ${kod(e.dosya)} — ${e.ozet}${e.menu_yetki ? ` (menü/yetki: ${e.menu_yetki})` : ''}`)}

## İş kuralları (fail-closed)
${liste(p.is_kurallari)}

## Testler
${liste(p.testler, t => `${kod(t.dosya)} [${t.tur}]\n${(t.senaryolar || []).map(s => '  - ' + s).join('\n')}`)}

## Mutasyon kanıtları
${liste(p.mutasyon_kanitlari)}

## Canlı doğrulama
${liste(p.canli_dogrulama)}

## Dosya ayak izi (çakışma planı için)
${liste(p.dosya_ayak_izi, d => kod(d))}
${p.fazlar && p.fazlar.length ? '\n## Fazlar\n' + liste(p.fazlar, f => `**${f.ad}** (${f.ne_zaman}): ${f.icerik}`) : ''}
## Mevzuat dayanağı
${liste(p.mevzuat, m => `${m.kaynak} — **${m.guven}** — ${m.not}`)}

## Riskler
${liste(p.riskler)}

## Açık sorular
${liste(p.acik_sorular)}

## Oylama
${liste(oylar, o => `**${o.mercek}: ${o.oy}** — ${o.gerekce}${(o.kosullar && o.kosullar.length) ? '\n  - Koşullar: ' + o.kosullar.join(' · ') : ''}`)}
${karar ? `\n## Birleşik koşullar (uygulayıcı için bağlayıcı)\n${liste(karar.birlesik_kosullar)}` : ''}
`;
  fs.writeFileSync(path.join(hedef, dosyaAdi), md);
  return { no, ad: p.ad, dosya: dosyaAdi, katman: katmanAd, karar, tahmin: p.tahmin_gun, onkosul: p.onkosul_paketler || [], ayakIzi: p.dosya_ayak_izi || [] };
}

const yazilan = [];
for (const t of (r.tasarimlar || [])) {
  const s = t.sonuc || {};
  for (const p of (s.paketler || [])) yazilan.push(paketBelgesi(p, s.katman || t.katman, `katman mimarı ${t.katman}`));
}
let ux = null;
if (uxGirdi && fs.existsSync(uxGirdi)) {
  const u = JSON.parse(fs.readFileSync(uxGirdi, 'utf8'));
  ux = u.result || u;
  for (const o of (ux.oylar || [])) for (const oy of (o.oylar || [])) { const k = String(oy.paket_no); (oyHaritasi[k] = oyHaritasi[k] || []).push({ mercek: o.mercek, ...oy }); }
  for (const o of (ux.oylar || [])) for (const v of (o.veto || [])) (vetolar[String(v.paket_no)] = vetolar[String(v.paket_no)] || []).push(`${o.mercek}: ${v.neden}`);
  for (const p of ((ux.tasarim && ux.tasarim.paketler) || [])) yazilan.push(paketBelgesi(p, 'Katman 8 — Deneyim (UX)', 'UX mimarı'));
}

// Ana belge
const s = r.sentez || {};
const katmanKararlari = (r.tasarimlar || []).map(t => `### ${(t.sonuc && t.sonuc.katman) || t.katman}\n${liste((t.sonuc && t.sonuc.ortak_kararlar) || [])}`).join('\n\n');
const ana = `# Kalan paketler mimarisi — ${yazilan.length} paket (2026-09-15)

> Kaynak: 6 katman mimarı (Opus) + Katman 8 deneyim mimarı, üç mercekli oylama (Bertan · Mehmet · Aşkın), sentez. Kurucu kararı: Katman 1 (imalat/atölye, paket 7–13) KAPSAM DIŞI. Her paketin ayrıntılı spesifikasyonu \`kalan-paketler/\` altında.

## Özet
${s.ozet || '(sentez yok)'}

**Toplam tahmin:** ${s.toplam_tahmin_gun || '-'} iş günü

## Karar tablosu
| # | Paket | Sonuç | Dalga | Sürüm | Tahmin | Önkoşul | Belge |
|---|---|---|---|---|---|---|---|
${yazilan.sort((a, b) => (Number(a.no) || 999) - (Number(b.no) || 999)).map(y => `| ${y.no} | ${y.ad} | ${y.karar ? y.karar.sonuc : '-'} | ${dalgaOf(y.no, y.karar ? y.karar.dalga : '-')} | ${surumOf(y.no)} | ${y.tahmin || '-'} | ${y.onkosul.join(', ') || '-'} | [${y.dosya}](kalan-paketler/${y.dosya}) |`).join('\n')}

## Uygulama dalgaları (yaz-seri yasası: dalga içinde SERİ)
${liste(s.dalgalar, d => `**Dalga ${d.no}** — paketler ${d.paketler.join(', ')} · sıra: ${d.uygulama_sirasi.join(' → ')}\n  ${d.gerekce}${(d.paralel_guvenli_ciftler && d.paralel_guvenli_ciftler.length) ? '\n  Paralel güvenli çiftler: ' + d.paralel_guvenli_ciftler.join('; ') : ''}`)}

## Dosya çakışma haritası
${(s.cakisma_haritasi && s.cakisma_haritasi.length) ? '| Dosya | Paketler |\n|---|---|\n' + s.cakisma_haritasi.map(c => `| ${kod(c.dosya)} | ${c.paketler.join(', ')} |`).join('\n') : '- (yok)'}

## Kesişen kararlar ve ADR önerileri
${liste(s.kesisen_kararlar, k => `**${k.baslik}** — ${k.karar}\n  Gerekçe: ${k.gerekce}${k.adr_onerisi ? '\n  ADR önerisi: ' + k.adr_onerisi : ''}`)}

## Katman kararları
${katmanKararlari}
${ux ? `\n## Katman 8 — Deneyim kararları\n${liste(ux.tasarim && ux.tasarim.ortak_kararlar)}\n\n### Yeni paket uygulayıcıları için UX sözleşmesi\n${liste(ux.tasarim && ux.tasarim.uygulama_kurallari_yeni_paketler_icin)}\n\n### Sayfa envanteri (düzenlenebilirlik · basit mod)\n| Sayfa | Veri girişleri | Düzenleme var mı | Politika | Basit modda | Görsel notlar |\n|---|---|---|---|---|---|\n${((ux.tasarim && ux.tasarim.sayfa_envanteri) || []).map(e => `| ${e.sayfa} | ${e.veri_girisleri} | ${e.duzenleme_var_mi} | ${e.politika} | ${e.basit_modda} | ${e.gorsel_notlar || '-'} |`).join('\n')}` : ''}

## Kurucuya kalanlar (yalnız imza/para/dış taahhüt)
${liste(s.kurucuya_kalanlar)}

## Riskler
${liste(s.riskler)}

## Mercek çapraz bulguları
${(r.oylar || []).concat((ux && ux.oylar) || []).map(o => `### ${o.mercek}\n${liste(o.capraz_bulgular)}${(o.veto && o.veto.length) ? '\n**Veto:** ' + o.veto.map(v => `${v.paket_no}: ${v.neden}`).join(' | ') : ''}`).join('\n\n')}
`;
fs.writeFileSync(path.join(path.dirname(hedef), 'MIMARI-KALAN-PAKETLER-2026-09-15.md'), ana);
console.log(`yazıldı: ${yazilan.length} paket belgesi + ana belge`);
console.log(yazilan.map(y => `${y.no}\t${y.karar ? y.karar.sonuc + ' d' + y.karar.dalga : '-'}\t${y.dosya}`).join('\n'));
