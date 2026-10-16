#!/usr/bin/env node
// ═════════════════════════════════════════════════════════════════════════════
// P-43 — QA ORTALAMA BÜTÜNLÜĞÜ CLI (ölçüm aracının onarımı)
//
// KULLANIM:  node betikler/qa-ozet.mjs --ozet <ham/qa-turu-N.json>
//            (qa-turu.js koşum betii sandbox'ında çalışır ve node ile
//             çalıştırılamaz — gövdesi sarılır, `return` içerir. Hesap blou
//             iki dosyada BİREBİR AYNI tutulur ve duman testi bunu ÖLÇER.)
// ÇIKIŞ: 0 tamam · 2 'sonuclar' yok · 64 kullanım hatası
// ═════════════════════════════════════════════════════════════════════════════
import fs from 'node:fs'

// ═══ P-43-ORTALAMA-BASLA — İKİ DOSYADA BİREBİR AYNI BLOK (elle ayrıştırma) ═══
// Bu blok `betikler/qa-turu.js` ile `betikler/qa-ozet.mjs` içinde BAYT BAYT
// AYNIDIR ve `duman-testi.sh::test_ortalama_blogu_iki_dosyada_ayni` bunu ölçer.
// Neden kopya: qa-turu.js koşum betiği sandbox'ında çalışır (gövdesi bir
// fonksiyona sarılır, `return` içerir) → node ile ÇALIŞTIRILAMAZ, `import`/
// `require` YOKTUR. CLI bu yüzden qa-ozet.mjs'de; sapma raporda yazılıdır.
//
// TUR 8 GERÇEĞİ (ölçüldü, iddia değil): yayın anlık görüntüsü eksik dosya
// servis ettiği için grup 1'in ALTI rotası (/cikti/{Tur}/{Id}, /kasalar,
// /sayim, /banka, /pos/tahsilatlar, /yetkiler) HİÇ önyüklenmedi ("Failed to
// start platform") ve hepsi 1/1 puanlandı. O altı 1'lik puan ORTALAMAYA
// KARIŞTI: tur 8 ortalaması 3,17 / 2,74 çıktı. Altı rota çıkarılınca gerçek
// ortalama 3,50 / 3,00. Yani "tur 7'den düştü" iddiası GÜRÜLTÜYDÜ.
//
// KURAL: bir grubun TÜM rotaları AYNI ÖNYÜKLEME hatasıyla ENGELLENDİ dönerse
// o grup PUAN ORTALAMASINA KATILMAZ; ayrı bir "ölçülemedi" kalemi olur.
// BULGULAR KAYBOLMAZ — bulgu/kritik/düzenlenemeyen sayımları TÜM sayfalardan
// yapılmaya devam eder; ayrılan yalnız ortalamadır.
//
// ⚠ Ölçülemeyen rota "temiz" demek DEĞİLDİR (Anayasa md.1). Ortalamadan
//   çıkarmak onu aklamaz; aracın bozuk olduğunu söyler. O rotalar duman kapısı
//   (duman-testi.sh) yeşile döndükten sonra YENİDEN taranmalıdır.

/** Tarayıcı konsolunda "uygulama hiç açılmadı" anlamına gelen imzalar. */
const ONYUKLEME_IMZALARI = [
  'Failed to start platform',
  'mono_download_assets',
  'integrity checks failed',
  '_framework',
]

/** Sayfa gerçekten ENGELLENDİ mi (durum alanı) — büyük/küçük harf duyarsız. */
function P43_engellendiMi(p) { return /ENGELLEN/i.test(String((p && p.durum) || '')) }

/** Sayfanın konsol/ağ kanıtında bulunan önyükleme imzaları. */
function P43_imzalar(p) {
  const m = String((p && p.konsol_ag) || '').toLowerCase()
  return ONYUKLEME_IMZALARI.filter(i => m.includes(i.toLowerCase()))
}

/**
 * Grupları ikiye ayırır. Bir grup ancak (a) sayfası varsa, (b) sayfalarının
 * HEPSİ ENGELLENDİ ise ve (c) hepsinde ORTAK bir önyükleme imzası varsa
 * "ölçülemedi" sayılır. Tek tük engellenen sayfa grubu diskalifiye ETMEZ
 * (tur 8 grup 8: 4 sayfanın 1'i engelliydi → o grup ortalamada KALIR).
 */
function gruplariAyir(sonuclar) {
  const olculemedi = []
  const olculebilen = []
  for (const g of (sonuclar || [])) {
    const sayfalar = g.sayfalar || []
    const hepsiEngelli = sayfalar.length > 0 && sayfalar.every(P43_engellendiMi)
    const ortak = hepsiEngelli
      ? ONYUKLEME_IMZALARI.filter(i => sayfalar.every(p => P43_imzalar(p).includes(i)))
      : []
    if (hepsiEngelli && ortak.length > 0) {
      olculemedi.push({ grup: g.grup, sayfa_sayisi: sayfalar.length, imza: ortak[0], rotalar: sayfalar.map(p => p.rota) })
    } else {
      olculebilen.push(g)
    }
  }
  return { olculemedi, olculebilen }
}

/** Türkçe ondalık (virgül) — rapor metinleriyle aynı biçim. */
function tr2(x) { return (x === null || x === undefined) ? '—' : Number(x).toFixed(2).replace('.', ',') }

function P43_ortala(sayfalar, f) {
  const d = sayfalar.map(f).filter(x => typeof x === 'number')
  return d.length ? +(d.reduce((a, b) => a + b, 0) / d.length).toFixed(2) : null
}

/**
 * İki ortalamayı birden hesaplar. Sayımlar (bulgu/kritik/düzenlenemeyen)
 * HER ZAMAN tüm sayfalardan yapılır; ayrılan yalnız puan ortalamasıdır.
 */
function ortalamaButunlugu(sonuclar) {
  const { olculemedi, olculebilen } = gruplariAyir(sonuclar)
  const tum = (sonuclar || []).flatMap(g => g.sayfalar || [])
  const olc = olculebilen.flatMap(g => g.sayfalar || [])
  return {
    olculemeyen_gruplar: olculemedi,
    olculemeyen_rota_sayisi: olculemedi.reduce((t, g) => t + g.sayfa_sayisi, 0),
    tum_sayfa_sayisi: tum.length,
    olculebilen_sayfa_sayisi: olc.length,
    ortalama_gorsel_tum: P43_ortala(tum, p => p.gorsel_puan_1_5),
    ortalama_kullanici_dostu_tum: P43_ortala(tum, p => p.kullanici_dostu_puan_1_5),
    ortalama_gorsel_olculebilen: P43_ortala(olc, p => p.gorsel_puan_1_5),
    ortalama_kullanici_dostu_olculebilen: P43_ortala(olc, p => p.kullanici_dostu_puan_1_5),
  }
}

/** --ozet çıktısının metni (saf: yan etkisiz). */
function ozetMetni(tur, sonuclar) {
  const b = ortalamaButunlugu(sonuclar)
  const s = []
  s.push(`QA ORTALAMA BÜTÜNLÜĞÜ (tur ${tur})`)
  s.push(`  tüm rotalar         : n=${b.tum_sayfa_sayisi} · görsel ${tr2(b.ortalama_gorsel_tum)}/5 · kullanıcı dostu ${tr2(b.ortalama_kullanici_dostu_tum)}/5`)
  s.push(`  ölçülebilen rotalar : n=${b.olculebilen_sayfa_sayisi} · görsel ${tr2(b.ortalama_gorsel_olculebilen)}/5 · kullanıcı dostu ${tr2(b.ortalama_kullanici_dostu_olculebilen)}/5`)
  if (b.olculemeyen_gruplar.length === 0) {
    s.push('  ölçülemedi: YOK — iki ortalama aynıdır.')
  } else {
    for (const g of b.olculemeyen_gruplar) {
      s.push(`  ölçülemedi (ortalamaya KATILMADI): grup ${g.grup} · ${g.sayfa_sayisi} rota · imza "${g.imza}"`)
      for (const r of g.rotalar) s.push(`      - ${r}`)
    }
    s.push('  ⚠ ölçülemeyen rotaların BULGULARI kaybolmadı; yalnız PUAN ortalamasından ayrıldı.')
    s.push('  ⚠ "ölçülemedi" TEMİZ demek değildir — duman kapısı yeşile dönünce bu rotalar YENİDEN taranır.')
  }
  return s.join('\n')
}
// ═══ P-43-ORTALAMA-BITIS ═════════════════════════════════════════════════════

export { ONYUKLEME_IMZALARI, gruplariAyir, ortalamaButunlugu, ozetMetni, tr2 }

const A = process.argv.slice(2)
const i = A.indexOf('--ozet')
if (i === -1) {
  console.error('Kullanım: node qa-ozet.mjs --ozet <ham/qa-turu-N.json>')
  process.exit(64)
}
const yol = A[i + 1]
if (!yol) { console.error('✗ --ozet için dosya yolu verilmedi'); process.exit(64) }
const veri = JSON.parse(fs.readFileSync(yol, 'utf8'))
const sonuclar = veri.sonuclar || []
if (!sonuclar.length) {
  console.error(`✗ ${yol} içinde 'sonuclar' yok — ortalama hesaplanamaz (fail-closed).`)
  process.exit(2)
}
console.log(ozetMetni(veri.tur ?? '?', sonuclar))
