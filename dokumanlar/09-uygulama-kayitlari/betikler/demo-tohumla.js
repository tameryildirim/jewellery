#!/usr/bin/env node
// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-30 (kimlik R-4) — DEMO KİRACISI TOHUMLAMA BETİĞİ
//
// NE YAPAR: temiz bir DEMO kiracısı açar ve içini YALNIZ ÜRÜNÜN KENDİ
// UÇLARINDAN geçerek doldurur. Tek satır bile doğrudan INSERT ile yazılmaz;
// her defter satırı sunucudaki HareketFabrikasi.Ekle kapısından geçer
// (plan § 1 md.8 · P-30 DoD).
//
// İZOLASYON (P-30 DoD · BK-16):
//   • Demo, QA-TEST-* fikstüründen TÜRETİLMEZ — sıfırdan kurulur.
//   • Adı DEMO- ile başlar; kiraci-temizle.sh bu adı SİLMEYİ REDDEDER
//     (betiğin silme ad kuralı PAKET*/TEST*/QA* ile sınırlıdır).
//   • Kurgusal adlar kullanılır (KVKK algısı) — gerçek müşteri adı yok.
//
// ⚠ DÜRÜSTLÜK NOTU (Anayasa md.1) — 3 AYLIK GEÇMİŞ:
//   Satış/alış/tahsilat uçlarının hiçbiri "işlem tarihi" ALMAZ; hareket tarihi
//   sunucuda o anki saattir. Bu yüzden bu betik geçmişe tarihli bir defter
//   YAZAMAZ ve YAZMIŞ GİBİ DE YAPMAZ: ürettiği geçmiş BUGÜNE tarihlidir ve
//   rapor JSON'unda "takvim: TEK_GUN" diye açıkça damgalanır. Geçmişe tarihli
//   tohumlama, uçlara birinci sınıf "işlem tarihi" eklenmesini gerektirir
//   (kapsam genişlemesi → PAKET-P-30 raporu § devredilenler).
//   Tarihi sonradan SQL ile kaydırmak YASAKTIR: denetim damga zinciri ve
//   mutabakat kayıtları o an yalan söylemeye başlar.
//
// PAKET P-D3 (2026-09-23): içerik DEMO-SENARYOSU-2026-09-26.md § 3'ün (V1–V17)
// istediği veridir — aşağıdaki SENARYO nesnesi. Plan yazılmadan ÖNCE, içerik
// yazıldıktan SONRA (sunucudan geri okunarak) sınanır; tutmayan madde = çıkış 1.
//   --plan-dogrula : ağ çağrısı yapmadan planı § 3'e karşı sınar (çıkış 0/1).
//   --sema/--olcek : kurucunun seçtiği renk şeması / yazı ölçeği (V15).
//
// KULLANIM:
//   KUYUMCU_SISTEM_ANAHTARI=<anahtar> node demo-tohumla.js \
//       [--api http://localhost:5198] [--ad "DEMO-VITRIN Kuyumculuk"] \
//       [--prova-kiraci] [--rapor <yol>] [--prova --kosan <ajan-kimligi>]
//   --prova-kiraci : gerçek demo yerine SİLİNEBİLİR bir prova kiracısı kurar
//                    (ad PAKET/TEST/QA ile başlamalı; dalga kapısında kullanılır).
//   --kosan        : ZORUNLU (yalnız --prova modunda). Provayı koşan AJANIN
//                    kimliği. BK-45 "iki AYRI ajan" koşulu ancak koşanın adı
//                    kayda geçerse denetlenebilir; aynı ajanın iki koşumu
//                    kanıt DEĞİLDİR ve denetim.sh bunu artık KIRMIZI verir.
// ═══════════════════════════════════════════════════════════════════════════

const fs = require('node:fs');
const path = require('node:path');

const KOK = path.resolve(__dirname, '..', '..', '..');           // proje kökü
const HAM = path.join(__dirname, '..', 'ham');                    // rapor dizini

function argOku(ad, varsayilan = null) {
  const i = process.argv.indexOf(ad);
  return i >= 0 && process.argv[i + 1] && !process.argv[i + 1].startsWith('--')
    ? process.argv[i + 1] : varsayilan;
}
const BAYRAK = (ad) => process.argv.includes(ad);

const API = (argOku('--api') || process.env.KUYUMCU_API || 'http://localhost:5198').replace(/\/$/, '');
const PROVA = BAYRAK('--prova-kiraci');
const AD = argOku('--ad') || (PROVA ? `PAKET-P-30-TEST Kuyumculuk` : 'DEMO-VITRIN Kuyumculuk');
const ANAHTAR = process.env.KUYUMCU_SISTEM_ANAHTARI || '';
// BK-45: provayı KİMİN koştuğu kayda geçmezse "iki ayrı ajan" koşulu
// denetlenemez. Bu yüzden --prova modunda --kosan (ya da KUYUMCU_PROVA_KOSAN)
// ZORUNLUDUR; boşsa betik en başta durur (fail-closed).
const KOSAN = (argOku('--kosan') || process.env.KUYUMCU_PROVA_KOSAN || '').trim();
const OPERATOR = process.env.KUYUMCU_SISTEM_OPERATOR || 'p30-demo';

// ── İZOLASYON KAPILARI (fail-closed) ───────────────────────────────────────
function adiDogrula(ad) {
  if (/^QA-TEST-/i.test(ad))
    throw new Error(`REDDEDİLDİ: '${ad}' kalıcı QA fikstürüdür. Demo ondan TÜRETİLMEZ (P-30 DoD).`);
  if (PROVA) {
    if (!/^(PAKET|TEST|QA)/i.test(ad))
      throw new Error(`REDDEDİLDİ: prova kiracısı adı PAKET/TEST/QA ile başlamalı (gelen: '${ad}') — `
        + 'aksi hâlde kiraci-temizle.sh onu silemez ve çöp kiracı kalır.');
  } else if (!/^DEMO-/.test(ad)) {
    throw new Error(`REDDEDİLDİ: demo kiracısının adı 'DEMO-' ile başlamalıdır (gelen: '${ad}').`);
  }
  return ad;
}

// ── HTTP — sessiz başarısızlık YOK ─────────────────────────────────────────
async function istek(metot, yol, { govde, jeton, sistem } = {}) {
  const basliklar = { 'Content-Type': 'application/json' };
  if (jeton) basliklar['Authorization'] = `Bearer ${jeton}`;
  if (sistem) { basliklar['X-Sistem-Anahtari'] = ANAHTAR; basliklar['X-Sistem-Operator'] = OPERATOR; }
  const c = await fetch(`${API}/api/v1${yol}`, {
    method: metot, headers: basliklar,
    body: govde === undefined ? undefined : JSON.stringify(govde),
  });
  const metin = await c.text();
  let json = null;
  try { json = metin ? JSON.parse(metin) : null; } catch { /* metin gövde */ }
  if (!c.ok) {
    // Anayasa md.1: gerçek durum kodu + gövdenin başı raporlanır, yutulmaz.
    throw new Error(`${metot} ${yol} → HTTP ${c.status}: ${metin.slice(0, 300)}`);
  }
  return json;
}

// ═══ PAKET P-D3 — CUMARTESİ DEMOSUNUN İSTEDİĞİ VERİ (DEMO-SENARYOSU § 3) ═══
// Tek kaynak: dokumanlar/02-mimari/sadelestirme/DEMO-SENARYOSU-2026-09-26.md
// bölüm 3 "GEREKEN VERİ" (V1–V17). Aşağıdaki SENARYO nesnesi o listenin
// makinece okunur karşılığıdır; senaryoPlaniniDogrula() onu tohumlamadan ÖNCE,
// senaryoOlcumunuDegerlendir() ise tohumlamadan SONRA sunucudan geri okunan
// değerlerle sınar. İkisi de FAIL-CLOSED: bir V maddesi tutmazsa betik
// sıfırdan farklı kodla çıkar, "TOHUMLANDI" yazmaz (Anayasa md.1).
//
// Kurgusal adlar (KVKK algısı). Telefonlar '0532 000 00 xx' kalıbında.
// Stok kodları dükkân kodu gibidir; "DEMO-" öneki ÜRÜN KODUNDA YOKTUR (V7/B9).
// Ekrana düşen hiçbir alanda "QA", "TEST", "DUZ", "DUZENLENDI" geçmez (V16).
// ⚠ ALTIN FİYATI SABİT YAZILMAZ: altın ürünlerin alış/satış birim fiyatı
//   sunucunun kendi fiyat teklifinden (/fiyat/teklif: canlı has kuru × liste
//   marjı) okunur. Gümüş için kotasyon YOKTUR (sunucu 'KOTASYON_YOK' der), bu
//   yüzden gümüş ürüne dükkânın kendi etiket fiyatı (sabitFiyat, kurgusal) ürünün
//   kendi ucundan (/fiyat/listeler/{id}/urun-fiyatlari) girilir — ekrandaki
//   'fiyat yok' kör noktasını demoda bırakmamak için.
const SENARYO = {
  kaynak: 'dokumanlar/02-mimari/sadelestirme/DEMO-SENARYOSU-2026-09-26.md § 3',
  // V3 — TL kasa, açılış bakiyesi ≥ 50.000 TL ve ASLA NEGATİF DEĞİL.
  kasaAcilisTl: 150000,
  kasaAsgariTl: 50000,
  // V9 — geçmiş satış tutar aralığı ve nakit kapısı (tevsik eşiği sunucudadır;
  //      buradaki 30.000 yalnız DEMO VERİSİNİN kendine koyduğu tavandır ve
  //      senaryo metnindeki "hiçbiri 30.000 ₺ üstü nakit değil" satırıdır).
  satisTutarAlt: 2000,
  satisTutarUst: 25000,
  nakitTavanTl: 30000,
  // V14 — vitrinde görünür bırakılacak kalemler (7–10).
  vitrinKalemleri: ['HAS', 'GRAM', 'CEYREK_YENI', 'YARIM_YENI', 'TAM_YENI', 'ATA_YENI', 'BILEZIK_22', 'AYAR_14'],
  vitrinAlt: 7,
  vitrinUst: 10,
  // Varsayılan perakende fiyat listesi (yeni kiracıda YOK). Marj kurgusal dükkân
  // kararıdır, mevzuat değildir; kurucu /fiyatlandirma ekranından değiştirir.
  liste: { ad: 'Perakende', tur: 'PERAKENDE', hasMarjYuzde: 4, iscilikCarpanYuzde: 100 },
  // TOPLU-3 (uzun demo bulgusu 5): fişte "işletme bilgisi eksik" kutusu çıkmasın diye
  // işletme kartı KURGUSAL değerlerle doldurulur (ürünün kendi ucundan: PATCH
  // /admin/tenant/kimlik). Vergi no bilerek GERÇEK OLAMAYACAK bir değerdir
  // (0000000000 — kontrol hanesi tutmaz, hiçbir mükellefe ait olamaz); telefon
  // carilerdeki kurgusal 0532000010x dizisinden. Unvan DEĞİŞTİRİLMEZ (kiracı adı).
  // TOPLU-5 (hakem ORTA 02.10 05:57 + ekip oyu): fişte yer tutucu gibi duran "(örnek adres)" /
  // "Örnek Vergi Dairesi" kalktı → KURGUSAL ama DOĞAL adres (kapı no + mahalle + ilçe/il; sokak adı
  // kurgusal); vergi no kurgusal olduğu BELLİ kalır (0000000000). 'DEMO' filigranı YOK (ekip oyu).
  // Yalnız demo tohumu: ürün kodu, yasal kural ya da belge biçimi DEĞİŞMEZ.
  isletme: {
    adres: 'Gümüşhane Sokak No: 12, Kuyumcular Çarşısı, Merkez / Ankara',
    vergiDairesi: 'Merkez Vergi Dairesi',
    vergiNo: '0000000000',
    telefon: '05320000100',
  },
  // V15 — dükkân görünümü (SAHİP). Kurucu seçimini --sema/--olcek ile ezer.
  sema: 'platin',
  yaziOlcegi: '18',
  // V4 — 5 müşteri + 1 toptancı. 'rol' betik içi etikettir, sunucuya gitmez.
  cariler: [
    { rol: 'M1', tur: 'MUSTERI', ad: 'Derya Yıldızlı', telefon: '05320000101' },
    { rol: 'M2', tur: 'MUSTERI', ad: 'Tuna Akarsu', telefon: '05320000102' },
    { rol: 'M3', tur: 'MUSTERI', ad: 'Selin Ergüven', telefon: '05320000103' },
    { rol: 'M4', tur: 'MUSTERI', ad: 'Kemal Ardıç', telefon: '05320000104' },
    // V12 — emanet bakiyesi 0,0000 kalacak müşteri: HİÇBİR hareketi yok.
    { rol: 'EMANET_BOS', tur: 'MUSTERI', ad: 'Nermin Çalışkan', telefon: '05320000105' },
    { rol: 'TOPTANCI', tur: 'TEDARIKCI', ad: 'Anadolu Döküm Toptan Kuyum', telefon: '05320000106' },
  ],
  // V5/V6/V7/V8 — 12 ürün: 4 ziynet · 3 takı (22) · 2 taşlı (14) · 2 gümüş · 1 gram altın.
  // 'acilisAdet' = ürün kartında girilen AÇILIŞ STOĞU (UX-4d md.1: deftere
  // SAYIM_DUZELTME düşer, alış SAYILMAZ). Neden alış değil: 12 ürünün stoğu
  // bugüne tarihli ~1,2 milyon TL'lik "alış" olarak yazılınca Panel'in "Net kasa"
  // kartı (satış − alış − hurda) KIRMIZI ve EKSİ ~1 milyon görünüyordu (ölçüldü,
  // P-D3 prova 3). Dükkân programa mevcut malıyla geçer; bu gerçeğin kendisidir.
  // Satılanlar + alışlar hesaba katılınca her üründe ≥ 3 kalır.
  urunler: [
    { k: 'CYR', tip: 'ZIYNET', kod: 'ZYN-CYR-22', ad: '22 Ayar Çeyrek Altın', ayarId: 'K22', gramAgirlik: 1.75, pariteKodu: 'CEYREK_YENI', acilisAdet: 6, grup: 'ziynet' },
    { k: 'YRM', tip: 'ZIYNET', kod: 'ZYN-YRM-22', ad: '22 Ayar Yarım Altın', ayarId: 'K22', gramAgirlik: 3.5, pariteKodu: 'YARIM_YENI', acilisAdet: 4, grup: 'ziynet' },
    { k: 'TAM', tip: 'ZIYNET', kod: 'ZYN-TAM-22', ad: '22 Ayar Tam Altın', ayarId: 'K22', gramAgirlik: 7.0, pariteKodu: 'TAM_YENI', acilisAdet: 3, grup: 'ziynet' },
    { k: 'ATA', tip: 'ZIYNET', kod: 'ZYN-ATA-22', ad: '22 Ayar Ata Lira', ayarId: 'K22', gramAgirlik: 7.2, pariteKodu: 'ATA_YENI', acilisAdet: 3, grup: 'ziynet' },
    { k: 'BLZ', tip: 'ZIYNET', kod: 'BLZ-22-015', ad: '22 Ayar Burma Bilezik', ayarId: 'K22', gramAgirlik: 15.2, acilisAdet: 3, grup: 'taki22' },
    { k: 'ZNC', tip: 'ZIYNET', kod: 'ZNC-22-008', ad: '22 Ayar Zincir Kolye', ayarId: 'K22', gramAgirlik: 8.0, acilisAdet: 3, grup: 'taki22' },
    { k: 'KNY', tip: 'ZIYNET', kod: 'KNY-22-010', ad: '22 Ayar Künye', ayarId: 'K22', gramAgirlik: 10.0, acilisAdet: 3, grup: 'taki22' },
    { k: 'YZK', tip: 'ZIYNET', kod: 'YZK-14-003', ad: '14 Ayar Zirkon Taşlı Yüzük', ayarId: 'K14', gramAgirlik: 3.2, tasAgirlik: 0.3, tasKokeni: 'SENTETIK', acilisAdet: 4, grup: 'tasli14' },
    { k: 'KLY', tip: 'ZIYNET', kod: 'KLY-14-004', ad: '14 Ayar Zirkon Taşlı Kolye', ayarId: 'K14', gramAgirlik: 4.1, tasAgirlik: 0.4, tasKokeni: 'SENTETIK', acilisAdet: 4, grup: 'tasli14' },
    { k: 'GKL', tip: 'GUMUS', kod: 'GMS-KLY-925', ad: '925 Gümüş Kolye', ayarId: 'G925', gramAgirlik: 12.0, acilisAdet: 3, grup: 'gumus', sabitFiyat: 2400 },
    { k: 'GBL', tip: 'GUMUS', kod: 'GMS-BLK-925', ad: '925 Gümüş Bileklik', ayarId: 'G925', gramAgirlik: 18.0, acilisAdet: 2, grup: 'gumus', sabitFiyat: 3200 },
    { k: 'GRM', tip: 'KULCE', kod: 'GRM-24-001', ad: '24 Ayar Gram Altın', ayarId: 'K24', gramAgirlik: 1.0, pariteKodu: 'GRAM', acilisAdet: 5, grup: 'gram24' },
  ],
  // V9/V10 — 9 satış. Nakit satışlar müşteri başına günlük 30.000 TL'nin altında
  // kalacak biçimde dağıtılır; 2 veresiye (V10) açık bırakılır.
  satislar: [
    { urun: 'CYR', cari: 'M1', odeme: 'NAKIT' },
    { urun: 'GRM', cari: 'M2', odeme: 'NAKIT' },
    { urun: 'YZK', cari: 'M3', odeme: 'KART' },
    { urun: 'CYR', cari: 'M3', odeme: 'NAKIT' },
    { urun: 'KLY', cari: 'M4', odeme: 'VERESIYE', vadeGun: 30 },
    { urun: 'GBL', cari: 'M1', odeme: 'NAKIT' },
    { urun: 'GRM', cari: 'M4', odeme: 'NAKIT' },
    { urun: 'YRM', cari: 'M2', odeme: 'VERESIYE', vadeGun: 15 },
    { urun: 'CYR', cari: 'M4', odeme: 'KART' },
  ],
  // Toptancıdan KÜÇÜK bir vadeli alış (kasadan nakit çıkmaz): toptancı cari
  // kartında bakiye görünsün, "Günlük alış" kartı sıfır kalmasın — ama Panel'in
  // net kartını eksiye düşürmesin (senaryoOlcumunuDegerlendir PANEL_NET ölçer).
  alislar: [{ urun: 'GBL', adet: 2, cari: 'TOPTANCI', vadeGun: 30 }],
  // V10 — veresiyenin KISMİ tahsilatı: borç açık kalır.
  tahsilat: { cari: 'M4', oran: 0.3 },
  // V11 — mevcut emanet kaydı (EMANET_BOS dışındaki bir müşteri).
  emanet: { cari: 'M1', ayarId: 'K22', gramAgirlik: 10.0, tasAgirlik: 0 },
  // Adım 3 (<30.000 TL) ve adım 4 (>30.000 TL, tevsik kapısı) için gerekli ürünler.
  ucuzUrun: 'CYR',
  pahaliUrun: 'BLZ',
  stokAsgari: 3,
};

const YASAK_KELIME = /\b(QA|TEST|DUZ|DUZENLENDI)\b/i;

/** EAN-13 denetim hanesi (etiket önizlemesinde gerçek barkod çizilsin). */
function ean13(govde12) {
  const t = [...govde12].reduce((s, c, i) => s + Number(c) * (i % 2 ? 3 : 1), 0);
  return govde12 + ((10 - (t % 10)) % 10);
}

/**
 * SAF — tohumlamadan ÖNCE planın V1–V17 sınırlarını sınar. Eksik listesi döner
 * (boş = geçer). Hiçbir ağ çağrısı yapmaz; testler bu fonksiyonu doğrudan koşar.
 */
function senaryoPlaniniDogrula(p) {
  const e = [];
  // TOPLU-3: fişin "düzenleyen" tarafı (A4Belge.KimlikEksikMi ile aynı kural:
  // adres + (vergi dairesi ya da vergi no)) demo planında dolu olmalı.
  if (!p.isletme || !String(p.isletme.adres || '').trim()
      || !(String(p.isletme.vergiDairesi || '').trim() || String(p.isletme.vergiNo || '').trim()))
    e.push('ISLETME: işletme kartı (adres + vergi dairesi/no) planda eksik');
  const musteriler = p.cariler.filter((c) => c.tur === 'MUSTERI');
  const toptanci = p.cariler.filter((c) => c.tur === 'TEDARIKCI');
  if (musteriler.length < 5) e.push(`V4: müşteri ${musteriler.length} < 5`);
  if (toptanci.length < 1) e.push('V4: toptancı yok');
  if (p.cariler.length < 6) e.push(`V4: cari ${p.cariler.length} < 6`);
  if (p.urunler.length < 12) e.push(`V5: ürün ${p.urunler.length} < 12`);
  const say = (g) => p.urunler.filter((u) => u.grup === g).length;
  if (say('ziynet') < 4) e.push('V6: 22 ayar ziynet < 4');
  if (say('taki22') < 3) e.push('V6: 22 ayar takı < 3');
  if (say('tasli14') < 2) e.push('V6: 14 ayar taşlı < 2');
  if (say('gumus') < 2) e.push('V6: 925 gümüş < 2');
  if (say('gram24') < 1) e.push('V6: 24 ayar gram altın < 1');
  const kodlar = new Set();
  for (const u of p.urunler) {
    if (/^DEMO-/i.test(u.kod)) e.push(`V7: ürün kodu DEMO- önekli: ${u.kod}`);
    if (kodlar.has(u.kod)) e.push(`V7: ürün kodu tekrarlı: ${u.kod}`);
    kodlar.add(u.kod);
  }
  for (const m of [...p.cariler.map((c) => c.ad), ...p.urunler.map((u) => u.ad), ...p.urunler.map((u) => u.kod)])
    if (YASAK_KELIME.test(m)) e.push(`V16: yasak kelime: ${m}`);
  // V8 — satılanlar düşüldükten sonra her üründe ≥ stokAsgari
  for (const u of p.urunler) {
    const satilan = p.satislar.filter((s) => s.urun === u.k).length;
    const alinan = (p.alislar || []).filter((a) => a.urun === u.k).reduce((t, a) => t + a.adet, 0);
    const kalan = (u.acilisAdet || 0) + alinan - satilan;
    if (kalan < p.stokAsgari) e.push(`V8: ${u.kod} kalan stok ${kalan} < ${p.stokAsgari}`);
  }
  for (const a of p.alislar || []) {
    if (!p.urunler.some((u) => u.k === a.urun)) e.push(`alış: bilinmeyen ürün ${a.urun}`);
    if (!p.cariler.some((c) => c.rol === a.cari && c.tur === 'TEDARIKCI')) e.push(`alış: tedarikçi olmayan cari ${a.cari}`);
  }
  if (p.satislar.length < 8) e.push(`V9: satış ${p.satislar.length} < 8`);
  const veresiye = p.satislar.filter((s) => s.odeme === 'VERESIYE');
  if (veresiye.length < 2) e.push(`V10: açık veresiye ${veresiye.length} < 2`);
  if (p.tahsilat && !(p.tahsilat.oran > 0 && p.tahsilat.oran < 1))
    e.push('V10: tahsilat veresiyeyi kapatmamalı (0 < oran < 1)');
  for (const s of p.satislar) {
    if (!p.urunler.some((u) => u.k === s.urun)) e.push(`V9: satışta bilinmeyen ürün ${s.urun}`);
    if (!p.cariler.some((c) => c.rol === s.cari && c.tur === 'MUSTERI')) e.push(`V9: satışta bilinmeyen müşteri ${s.cari}`);
    if (s.cari === 'EMANET_BOS') e.push('V12: emanet için ayrılan müşteriye satış yazılamaz');
  }
  if (!(p.kasaAcilisTl >= p.kasaAsgariTl && p.kasaAsgariTl >= 50000))
    e.push(`V3: kasa açılışı ${p.kasaAcilisTl} < ${p.kasaAsgariTl} (asgari 50.000)`);
  if (!p.emanet || p.emanet.cari === 'EMANET_BOS') e.push('V11: mevcut emanet kaydı EMANET_BOS dışındaki müşteride olmalı');
  if (!p.cariler.some((c) => c.rol === 'EMANET_BOS')) e.push('V12: emanet bakiyesi sıfır müşteri ayrılmamış');
  const v = p.vitrinKalemleri.length;
  if (v < p.vitrinAlt || v > p.vitrinUst) e.push(`V14: vitrin kalemi ${v} ∉ [${p.vitrinAlt},${p.vitrinUst}]`);
  if (!p.sema || !p.yaziOlcegi) e.push('V15: renk şeması/yazı ölçeği seçilmemiş');
  if (!p.urunler.some((u) => u.k === p.ucuzUrun)) e.push('Adım 3: ucuz ürün tanımsız');
  if (!p.urunler.some((u) => u.k === p.pahaliUrun)) e.push('Adım 4: pahalı ürün tanımsız');
  return e;
}

/**
 * SAF — tohumlamadan SONRA sunucudan GERİ OKUNAN değerleri sınar.
 * `o` alanları: kasaBakiyeleri[], cariSayisi, musteriSayisi, urunSayisi,
 * stoklar{kod:adet}, satisSayisi, nakitSatisTutarlari[], satisTutarlari[],
 * acikVeresiye, emanetBakiye, emanetBosBakiye, kurId, sema, yaziOlcegi,
 * vitrinAktif, yasakKelimeli[], demoOnekliKod[], ucuzFiyat, pahaliFiyat, tevsikEsigi.
 * Döner: [{ v, beklenen, gercek, tamam }]. Bilinmeyen (undefined) değer TAMAM DEĞİLDİR.
 */
function senaryoOlcumunuDegerlendir(o, p = SENARYO) {
  const s = [];
  const ekle = (v, beklenen, gercek, tamam) => s.push({ v, beklenen, gercek: gercek ?? null, tamam: tamam === true });
  const kb = o.kasaBakiyeleri || [];
  ekle('V3', `her kasa > 0 ve en az biri ≥ ${p.kasaAsgariTl}`, kb,
    kb.length > 0 && kb.every((b) => b > 0) && kb.some((b) => b >= p.kasaAsgariTl));
  ekle('V4', '≥ 6 cari (≥ 5 müşteri)', `${o.cariSayisi}/${o.musteriSayisi}`, o.cariSayisi >= 6 && o.musteriSayisi >= 5);
  ekle('V5', '≥ 12 ürün', o.urunSayisi, o.urunSayisi >= 12);
  const stok = Object.values(o.stoklar || {});
  ekle('V8', `her üründe ≥ ${p.stokAsgari}`, o.stoklar, stok.length >= 12 && stok.every((a) => a >= p.stokAsgari));
  const tut = o.satisTutarlari || [];
  ekle('V9', `≥ 8 satış, her biri ${p.satisTutarAlt}–${p.satisTutarUst}, nakit ≤ ${p.nakitTavanTl}`, tut,
    o.satisSayisi >= 8 && tut.length >= 8 && tut.every((t) => t >= p.satisTutarAlt && t <= p.satisTutarUst)
    && (o.nakitSatisTutarlari || []).every((t) => t <= p.nakitTavanTl));
  ekle('V10', '≥ 2 açık veresiye', o.acikVeresiye, o.acikVeresiye >= 2);
  ekle('V11', 'mevcut emanet bakiyesi > 0', o.emanetBakiye, o.emanetBakiye > 0);
  ekle('V12', 'emanet için ayrılan müşteride bakiye 0', o.emanetBosBakiye, o.emanetBosBakiye === 0);
  ekle('V13', 'güncel kur dolu', o.kurId, typeof o.kurId === 'string' && o.kurId.length > 0);
  ekle('V14', `vitrinde ${p.vitrinAlt}–${p.vitrinUst} kalem`, o.vitrinAktif, o.vitrinAktif >= p.vitrinAlt && o.vitrinAktif <= p.vitrinUst);
  ekle('V15', `şema ${p.sema} · ölçek ${p.yaziOlcegi}`, `${o.sema}/${o.yaziOlcegi}`, o.sema === p.sema && o.yaziOlcegi === p.yaziOlcegi);
  ekle('V16', 'ekrana düşen adlarda QA/TEST/DUZ yok', o.yasakKelimeli, Array.isArray(o.yasakKelimeli) && o.yasakKelimeli.length === 0);
  ekle('V7', 'ürün kodunda DEMO- öneki yok', o.demoOnekliKod, Array.isArray(o.demoOnekliKod) && o.demoOnekliKod.length === 0);
  ekle('ADIM3', `ucuz ürünün satış fiyatı < ${o.tevsikEsigi ?? '?'}`, o.ucuzFiyat,
    typeof o.tevsikEsigi === 'number' && o.ucuzFiyat > 0 && o.ucuzFiyat < o.tevsikEsigi);
  // Panel "Net kasa" kartı = günlük satış − alış − hurda; eksi ise KIRMIZI çizilir.
  ekle('PANEL_NET', 'Panel net kartı ≥ 0 (kırmızı eksi yok)', o.panelNetTl,
    typeof o.panelNetTl === 'number' && o.panelNetTl >= 0);
  ekle('ADIM4', `pahalı ürünün satış fiyatı > ${o.tevsikEsigi ?? '?'} (tevsik kapısı tetiklenir)`, o.pahaliFiyat,
    typeof o.tevsikEsigi === 'number' && o.pahaliFiyat > o.tevsikEsigi);
  return s;
}

// kodEki: aynı kiracıya İKİNCİ kez tohumlarken stok kodu çakışmasın diye
// eklenen ek (boş = temiz kiracı, kodlar okunur kalır).

// ═══════════════════════════════════════════════════════════════════════════
// 15 DAKİKALIK GÖSTERİM PROVASI (--prova)
//
// NE ÖLÇER: docs/RUNBOOK-DEMO.md'deki gösterim adımlarının ARKASINDAKİ uçları
// sırayla çağırır, her adımın süresini ve HTTP durumunu yazar.
// KRİTİK tanımı: adımın ucu 2xx dönmediyse ya da gösterime konu olan liste
// BEKLENEN veriyi vermiyorsa (boş/çelişkili özet) o adım kritiktir.
//
// ⚠ DÜRÜSTLÜK SINIRI (Anayasa md.1): bu prova, İNSANIN ekranda yaptığı
//   gösterimin yerine GEÇMEZ; ekranların çizim süresini ölçmez. Ölçtüğü şey
//   "gösterimin dayandığı veri yolu iki turda da kritik hata vermeden ve
//   bütçe içinde cevap veriyor mu" sorusudur. Betiğin ölçtüğü süre, insan
//   gösteriminin ALT SINIRIDIR; runbook her adıma ayrıca insan dakikası yazar.
// ═══════════════════════════════════════════════════════════════════════════
const PROVA_ADIMLARI = [
  { no: 1,  rota: '/giris',              ad: 'Giriş + kısıt beyanı',  yol: '/genel/kisitlar', anonim: true, dk: 1 },
  { no: 2,  rota: '/',                   ad: 'Karşılama paneli',      yol: '/pano/guncel',    dk: 2 },
  { no: 3,  rota: '/kur',                ad: 'Canlı kur',             yol: '/kur/guncel',     dk: 1 },
  { no: 4,  rota: '/urunler',            ad: 'Stok listesi',          yol: '/urunler',        dk: 2, doluOlmali: true },
  { no: 5,  rota: '/cariler',            ad: 'Cari listesi',          yol: '/cari',           dk: 2, doluOlmali: true },
  { no: 6,  rota: '/islemler',           ad: 'Satış geçmişi',         yol: '/islemler/iade-adaylari?tur=SATIS&limit=50', dk: 2, doluOlmali: true },
  { no: 7,  rota: '/kasalar',            ad: 'Kasa durumu',           yol: '/kasalar',        dk: 1, doluOlmali: true },
  { no: 8,  rota: '/raporlar/vade-takibi', ad: 'Vade takibi',           yol: '/raporlar/vade-takibi', dk: 2 },
  { no: 9,  rota: '/raporlar',           ad: 'Raporlar',              yol: '/raporlar/gunluk',      dk: 1 },
  { no: 10, rota: '/denetim-izi',       ad: 'Denetim izi',           yol: '/denetim?limit=20',     dk: 1 },
];

// ═══ TEK KAYNAK KAPISI — prova "rota" etiketi UYGULAMANIN @page LİSTESİNDEN
//     doğrulanır (PAKET P-D1 / D-c, 2026-09-23) ════════════════════════════
// ÖLÇÜLMÜŞ ARIZA: adım 8'in etiketi '/vade-takibi' yazıyordu; böyle bir @page
// YOKTUR (gerçeği '/raporlar/vade-takibi'). 'yol' alanı (API ucu) doğru
// olduğu için prova YEŞİL dönüyor, rapora ve runbook'a YANLIŞ rota adı
// giriyordu — Panel'deki ölü bağlantıyla AYNI SINIF hata. RUNBOOK § 3.1 bunu
// 23 Eylül'de yakaladı ama betikte düzelmemişti.
// KURAL: 'rota' elle yazılır ama ELLE DOĞRULANMAZ — bu kapı onu @page
// direktiflerine karşı sınar ve uymuyorsa PROVA BAŞLAMAZ (fail-closed).
// Not: 'yol' bir API ucudur, 'rota' bir ekran adresidir; ikisi aynı şey
// değildir (ör. adım 9: rota '/raporlar', yol '/raporlar/gunluk'). Bu yüzden
// tek kaynak "rota = yol" değil, "rota ∈ @page kümesi" demektir.
const SAYFA_DIZINI = path.join(KOK, 'dotnet', 'Kuyumcu.Web', 'Pages');

function uygulamaRotalari() {
  const dosyalar = fs.readdirSync(SAYFA_DIZINI).filter((f) => f.endsWith('.razor'));
  const rotalar = new Set();
  for (const d of dosyalar) {
    const metin = fs.readFileSync(path.join(SAYFA_DIZINI, d), 'utf8');
    for (const satir of metin.split(/\r?\n/)) {
      const m = /^@page\s+"([^"]+)"/.exec(satir);
      if (m) rotalar.add(m[1].replace(/\{([A-Za-z]+)(:[a-z]+)?\}/g, '{$1}'));
    }
  }
  return rotalar;
}

function provaRotalariniDogrula() {
  const gercek = uygulamaRotalari();
  if (gercek.size < 40)
    throw new Error(`REDDEDİLDİ: @page ölçümü boş/eksik döndü (${gercek.size} rota) — `
      + `kapı oyunlanmış ya da kaynak ağacı bulunamadı: ${SAYFA_DIZINI}`);
  const olu = PROVA_ADIMLARI
    .map((a) => a.rota)
    .filter((r) => !gercek.has(r.replace(/\?.*$/, '')));
  if (olu.length)
    throw new Error(`REDDEDİLDİ: prova tablosunda ÖLÜ ROTA etiketi var: ${olu.join(', ')}. `
      + `Bu adresler uygulamanın @page listesinde YOK (${gercek.size} rota tarandı). `
      + `Doğru rotayı Pages/*.razor'dan al; 'yol' (API ucu) ile 'rota' (ekran adresi) karıştırılmaz.`);
  return gercek.size;
}

async function prova(eposta, sifre) {
  if (!KOSAN)
    throw new Error('REDDEDİLDİ: --kosan <ajan-kimligi> zorunludur (BK-45). '
      + 'Provayı koşan ajan kayda geçmezse "iki AYRI ajan" koşulu denetlenemez.');
  const rotaSayisi = provaRotalariniDogrula();   // D-c: ölü rota etiketi ile prova BAŞLAMAZ
  const t0 = Date.now();
  const basladiIso = new Date().toISOString();
  const giris = await istek('POST', '/auth/giris', { govde: { email: eposta, sifre } });
  const jeton = giris.accessToken;
  if (!jeton) throw new Error('Prova: giriş yapılamadı (accessToken yok).');

  const adimlar = [];
  let kritik = 0;
  for (const a of PROVA_ADIMLARI) {
    const t = Date.now();
    const adimBasladi = new Date().toISOString();   // BK-45: her adımın başlangıç damgası
    const basliklar = a.anonim ? {} : { Authorization: `Bearer ${jeton}` };
    const c = await fetch(`${API}/api/v1${a.yol}`, { headers: basliklar });
    const metin = await c.text();
    let veri = null;
    try { veri = metin ? JSON.parse(metin) : null; } catch { /* metin */ }

    const dizi = Array.isArray(veri) ? veri : (Array.isArray(veri?.kayitlar) ? veri.kayitlar : null);
    const bos = a.doluOlmali === true && (dizi === null || dizi.length === 0);
    const kritikMi = !c.ok || bos;
    if (kritikMi) kritik++;

    adimlar.push({
      no: a.no, rota: a.rota, adim: a.ad, yol: a.yol, durum: c.status,
      basladi: adimBasladi, bitti: new Date().toISOString(),
      ms: Date.now() - t, insanDk: a.dk,
      kritik: kritikMi ? 1 : 0,
      not: kritikMi ? (!c.ok ? `HTTP ${c.status}: ${metin.slice(0, 160)}` : 'beklenen liste BOŞ döndü') : '',
    });
  }

  const veriYoluMs = Date.now() - t0;
  const insanDkToplam = PROVA_ADIMLARI.reduce((t, a) => t + a.dk, 0);
  return {
    tur: 'GOSTERIM_PROVASI',
    rota_tek_kaynak: { agac_rota_sayisi: rotaSayisi, olu_rota_etiketi: 0 },  // P-D1/D-c kanıtı
    kosan: KOSAN,                       // BK-45: iki tur İKİ AYRI kosan ister
    kiraci: AD,
    rotalar: PROVA_ADIMLARI.map((a) => a.rota),
    adimlar,
    kritik,
    // İnsan gösterim bütçesi (runbook) ile ölçülen veri yolu AYRI raporlanır;
    // ikisi birbirinin yerine geçmez (Anayasa md.1).
    insanDkToplam,
    insanDkTavan: 20,                   // BK-45: > 20 dk ise paket KAPANMAZ
    insanDkAsildi: insanDkToplam > 20 ? 1 : 0,
    insanSuresiOlculdu: false,          // betik insanın ekrandaki süresini ÖLÇMEZ
    veriYoluMs,
    veriYoluDk: +(veriYoluMs / 60000).toFixed(4),
    basladi: basladiIso,
    bitti: new Date().toISOString(),
    olculdu: new Date().toISOString(),
  };
}

// ═══ TOPLU-3 (demo videosu bulgusu 4) — İLK KURU BEKLEME ══════════════════════
// Kur zamanlayıcısı yeni kiracının ilk kurunu Kur:GuncellemeSaniye (60 sn) aralıkla
// yazar; eski 40 sn'lik bekleme ilk turu kaçırıp 404 ile düşüyordu. Yoklama en çok
// KUR_BEKLEME_AZAMI_SN saniye sürer (zamanlayıcı aralığının 2,5 katı). 404 BEKLENEN
// ara durumdur; başka her hata olduğu gibi yükselir (Anayasa md.1 — yutulmaz).
// `getir` fetch benzeri bir işlevdir ({ ok, status, json(), text() }) — test sahte verir.
const KUR_BEKLEME_AZAMI_SN = 150;
async function kurBekle(getir, { azamiSn = KUR_BEKLEME_AZAMI_SN, aralikMs = 2000, bekle } = {}) {
  const uyu = bekle || ((ms) => new Promise((r) => setTimeout(r, ms)));
  const bitis = azamiSn * 1000;
  let sonDurum = null;
  for (let gecen = 0; gecen <= bitis; gecen += aralikMs) {
    const c = await getir();
    sonDurum = c.status;
    if (c.ok) {
      const k = await c.json();
      if (k && k.id) return k;
    } else if (c.status !== 404) {
      throw new Error(`GET /kur/guncel → HTTP ${c.status}: ${(await c.text()).slice(0, 300)}`);
    }
    await uyu(aralikMs);
  }
  throw new Error(`${azamiSn} saniyede /kur/guncel kur vermedi (son durum ${sonDurum}) — `
    + 'kur olmadan defter yazılmaz (sessizce devam EDİLMEZ). V13: kaynak gelmiyorsa '
    + 'kurucu /kur ekranından ELLE kur girer, sonra betik --eposta ile yeniden koşulur.');
}

async function icerikDoldur(jeton, giris, olc, kodEki = '') {
  // P-D3: plan tohumlamadan ÖNCE sınanır — V listesi tutmayan plan hiç yazılmaz.
  const planEksik = senaryoPlaniniDogrula(SENARYO);
  if (planEksik.length)
    throw new Error(`REDDEDİLDİ: demo planı DEMO-SENARYOSU § 3'ü karşılamıyor: ${planEksik.join(' · ')}`);
  const sema = argOku('--sema') || SENARYO.sema;
  const olcek = argOku('--olcek') || SENARYO.yaziOlcegi;

  // 4) Kur kaydı — satış/alış bu kimliğe bağlanır (yeni kiracıda gecikmeli gelebilir).
  // ⚠ TUZAK: yeni kiracıda kur arka plan işiyle gelir; ilk saniyelerde uç 404
  //   ("henüz kur verisi yok") döner. 404 BEKLENEN bir ara durumdur, beklenir;
  //   başka her hata olduğu gibi yükselir (Anayasa md.1 — yutulmaz).
  const kur = await olc('kur', () => kurBekle(
    () => fetch(`${API}/api/v1/kur/guncel`, { headers: { Authorization: `Bearer ${jeton}` } })));

  // 5) Kasa (oturumun şubesinden) — V3: açılış bakiyesi POZİTİF ve ≥ asgari.
  const kasa = await olc('kasalar', async () => {
    const liste = await istek('GET', '/kasalar', { jeton });
    const mevcut = (liste || []).find((k) => k.aktif !== false && (k.paraBirimi || 'TRY') === 'TRY');
    if (mevcut) return mevcut;
    // Yeni kiracıda kasa gelmiyorsa demo kendi kasasını ÜRÜNÜN UCUNDAN açar
    // (doğrudan INSERT değil) — aksi hâlde nakit bacağı yazılamaz.
    const subeId = giris.kullanici?.subeId
      || (await istek('GET', '/subeler', { jeton }))?.[0]?.id;
    if (!subeId) throw new Error('Şube bulunamadı — kasa açılamaz (sessizce devam EDİLMEZ).');
    return await istek('POST', '/kasalar', {
      jeton, govde: { ad: 'Ana Kasa', subeId, paraBirimi: 'TRY' },
    });
  });
  await olc('kasa-acilis', async () => {
    const guncel = await istek('GET', `/kasalar/${kasa.id}`, { jeton });
    const bakiye = Number(guncel?.bakiye ?? guncel?.kasa?.bakiye ?? 0);
    if (bakiye >= SENARYO.kasaAcilisTl) return;
    // PARA-DEMO-3 (hakem YÜKSEK, 02.10): açılış devri ELLE "Gelir" (kasa hareketi) olarak YAZILMAZ —
    // gün sonu "Tahsilat" onu yeni para sayıp 150.000 ₺ şişiriyordu. Ürünün kendi açılış yolu
    // (Kurulum › açılış: prova → uygula, SAHIP) kullanılır: ACILIS hareketine bağlı kasa bacağı yazar,
    // gün sonu onu "Açılış devri (tahsilata girmez)" satırına ayırır. Hata yutulmaz (Anayasa md.1).
    const satirlar = [{ hedefId: kasa.id, tlTutar: +(SENARYO.kasaAcilisTl - bakiye).toFixed(2), hasGram: 0 }];
    const prova = await istek('POST', '/kurulum/acilis/prova', { jeton, govde: { tur: 'KASA', satirlar } });
    if (!prova?.provaJetonu) throw new Error('Açılış provası jeton döndürmedi — açılış devri yazılamadı.');
    await istek('POST', '/kurulum/acilis/uygula', {
      jeton,
      govde: { tur: 'KASA', satirlar, provaJetonu: prova.provaJetonu,
               gerekce: 'Açılış devri — önceki günden devreden nakit', kurSnapshotId: kur.id },
    });
  });

  // 6) Cariler (kurgusal) — rol → sunucu kaydı
  const cari = {};
  await olc('cariler', async () => {
    for (const c of SENARYO.cariler) {
      const { rol, ...govde } = c;
      cari[rol] = await istek('POST', '/cari', { jeton, govde });
    }
  });

  // 7) Ürünler — dükkân kodu + EAN-13 barkod (etiket önizlemesinde gerçek barkod)
  const urun = {};
  const barkodTohum = (kodEki.replace(/\D/g, '') || '0').slice(-4).padStart(4, '0');
  await olc('urunler', async () => {
    let i = 0;
    for (const u of SENARYO.urunler) {
      i++;
      const { k, acilisAdet, grup, sabitFiyat, ...govde } = u;
      urun[k] = await istek('POST', '/urunler', {
        jeton,
        govde: { ...govde, kod: u.kod + kodEki, barkod: ean13(`869${barkodTohum}${String(i).padStart(5, '0')}`),
                 acilisStogu: { adet: acilisAdet } },
      });
    }
  });

  // 8) Fiyat: gümüşte kotasyon yok → dükkânın etiket fiyatı varsayılan listeye;
  //    sonra her ürünün teklifi SUNUCUDAN okunur (altın fiyatı koda gömülmez).
  const teklif = {};
  await olc('fiyatlar', async () => {
    const listeler = await istek('GET', '/fiyat/listeler', { jeton });
    let liste = (listeler || []).find((l) => l.varsayilan && l.aktif !== false);
    // Yeni kiracı fiyat listesiz doğar → POS hiçbir ürüne otomatik fiyat veremez
    // (teklif 'LISTE_YOK'). Demo, ürünün kendi ucundan varsayılan perakende
    // listesini açar; marj kurgusal dükkân kararıdır (SENARYO.liste).
    if (!liste)
      liste = await istek('POST', '/fiyat/listeler', { jeton, govde: { ...SENARYO.liste, varsayilan: true, aktif: true } });
    if (!liste?.id) throw new Error('Varsayılan fiyat listesi açılamadı — teklif alınamaz (sessizce devam EDİLMEZ).');
    for (const u of SENARYO.urunler.filter((x) => x.sabitFiyat)) {
      await istek('POST', `/fiyat/listeler/${liste.id}/urun-fiyatlari`, {
        jeton, govde: { urunId: urun[u.k].id, sabitFiyat: u.sabitFiyat, not: 'Dükkân etiket fiyatı' },
      });
    }
    for (const u of SENARYO.urunler) {
      const t = await istek('GET', `/fiyat/teklif?urunId=${urun[u.k].id}&kurSnapshotId=${kur.id}`, { jeton });
      if (!t?.teklifVar || !(t.satisFiyati > 0))
        throw new Error(`${u.kod}: sunucu fiyat teklifi vermedi (${t?.sebep || 'yanıt boş'}) — demo fiyatsız ürünle kurulmaz.`);
      teklif[u.k] = t;
    }
  });

  // 9) Küçük vadeli alış — toptancıdan (kasadan nakit çıkmaz; borç cariye yazılır).
  //    Her satır HareketFabrikasi üzerinden. Birim fiyat = teklifin has değeri
  //    (altın) / etiket fiyatının %60'ı (gümüş, kurgusal maliyet).
  await olc('alislar', async () => {
    for (const a of SENARYO.alislar) {
      const u = SENARYO.urunler.find((x) => x.k === a.urun);
      const t = teklif[a.urun];
      const birim = u.sabitFiyat ? +(u.sabitFiyat * 0.6).toFixed(2) : +Number(t.hasDegeri || t.satisFiyati).toFixed(2);
      await istek('POST', '/islemler/alis', {
        jeton,
        govde: {
          urunId: urun[a.urun].id, adet: a.adet, birimFiyatTl: birim,
          cariId: cari[a.cari].id, kurSnapshotId: kur.id, vadeGun: a.vadeGun ?? null,
          aciklama: 'Toptancıdan mal girişi',
        },
      });
    }
  });

  // 10) Satışlar — nakit + kart + veresiye karışık (V9/V10)
  const fisler = [];
  await olc('satislar', async () => {
    for (const s of SENARYO.satislar) {
      const t = teklif[s.urun];
      const nakitMi = s.odeme === 'NAKIT' || s.odeme === 'KART';
      const yanit = await istek('POST', '/islemler/satis', {
        jeton,
        govde: {
          urunId: urun[s.urun].id, adet: 1, birimFiyatTl: t.satisFiyati,
          odemeTuru: s.odeme, kasaId: nakitMi ? kasa.id : null,
          cariId: cari[s.cari].id, kurSnapshotId: kur.id, vadeGun: s.vadeGun ?? null,
          aciklama: 'Tezgâh satışı',
        },
      });
      fisler.push({ odeme: s.odeme, tutar: t.satisFiyati, cari: s.cari, yanit });
    }
  });

  // 11) Kısmi tahsilat — veresiye açık kalır (V10)
  await olc('tahsilat', async () => {
    const ver = fisler.find((f) => f.cari === SENARYO.tahsilat.cari && f.odeme === 'VERESIYE');
    if (!ver) throw new Error('Tahsilat için veresiye satış bulunamadı.');
    await istek('POST', `/cari/${cari[SENARYO.tahsilat.cari].id}/tahsilat`, {
      jeton,
      govde: { tlTutari: Math.round(ver.tutar * SENARYO.tahsilat.oran), odemeTuru: 'NAKIT', kasaId: kasa.id,
               kurSnapshotId: kur.id, aciklama: 'Veresiye ara ödemesi' },
    });
  });

  // 12) Mevcut emanet kaydı (V11) — EMANET_BOS müşteriye DOKUNULMAZ (V12)
  await olc('emanet', async () => {
    const e = SENARYO.emanet;
    await istek('POST', '/emanet/giris', {
      jeton,
      govde: { cariId: cari[e.cari].id, ayarId: e.ayarId, gramAgirlik: e.gramAgirlik, tasAgirlik: e.tasAgirlik,
               aciklama: 'Bayram için bırakılan bilezikler', kurSnapshotId: kur.id },
    });
  });

  // 13) Vitrin panosu — 7–10 kalem görünür (V14)
  await olc('vitrin', async () => {
    const ayarlar = await istek('GET', '/pano/ayarlar', { jeton });
    const kalemler = Array.isArray(ayarlar) ? ayarlar : (ayarlar?.kalemler || ayarlar?.ayarlar || []);
    if (!kalemler.length) throw new Error('/pano/ayarlar kalem listesi boş döndü — vitrin ayarlanamaz.');
    for (const k of kalemler) {
      const kod = k.kod || k.pariteKodu;
      const olmali = SENARYO.vitrinKalemleri.includes(kod);
      if (Boolean(k.aktif) !== olmali)
        await istek('PATCH', `/pano/ayarlar/${kod}`, { jeton, govde: { aktif: olmali } });
    }
  });

  // 13b) TOPLU-3 — işletme kartı (kurgusal): fişin düzenleyen tarafı dolu çıksın.
  //      Yanıt GERİ OKUNUR; sunucu alanı yazmadıysa betik durur (sessiz geçiş yok).
  await olc('isletme', async () => {
    const t = await istek('PATCH', '/admin/tenant/kimlik', { jeton, govde: { ...SENARYO.isletme } });
    if (!t || !t.adres || !(t.vergiDairesi || t.vergiNo))
      throw new Error(`işletme kartı yazılamadı (yanıt: ${JSON.stringify(t).slice(0, 200)})`);
  });

  // 14) Dükkân görünümü (V15) — yalnız SAHİP; kurucu --sema/--olcek ile seçer
  await olc('gorunum', () => istek('PATCH', '/gorunum', { jeton, govde: { sema, yaziOlcegi: olcek } }));

  // 15) GERİ OKUMA — V maddeleri sunucudan ölçülür, fail-closed değerlendirilir.
  const olcum = await olc('senaryo-olcum', () => senaryoOlc(jeton, { kur, cari, urun, teklif, fisler }));
  const sonuc = senaryoOlcumunuDegerlendir(olcum, { ...SENARYO, sema, yaziOlcegi: olcek });
  const eksik = sonuc.filter((x) => !x.tamam);
  const rapor = {
    sayimlar: { cari: Object.keys(cari).length, urun: Object.keys(urun).length, satis: fisler.length },
    senaryo: { kaynak: SENARYO.kaynak, maddeler: sonuc, eksik: eksik.length, olcum },
  };
  if (eksik.length) {
    const hata = new Error(`SENARYO KARŞILANMADI (${eksik.length}): `
      + eksik.map((x) => `${x.v} beklenen ${x.beklenen}, gerçek ${JSON.stringify(x.gercek)}`).join(' · '));
    hata.rapor = rapor;
    throw hata;
  }
  return rapor;
}

/** Tohum sonrası sunucudan GERİ OKUMA — tek istisna: nakit satışların ödeme türü (yukarıda). */
async function senaryoOlc(jeton, { kur, cari, urun, teklif, fisler }) {
  const o = {};
  const kasalar = await istek('GET', '/kasalar', { jeton });
  o.kasaBakiyeleri = (kasalar || []).filter((k) => k.aktif !== false).map((k) => Number(k.bakiye));
  const cariler = await istek('GET', '/cari', { jeton });
  const cariDizi = Array.isArray(cariler) ? cariler : [];
  o.cariSayisi = cariDizi.length;
  o.musteriSayisi = cariDizi.filter((c) => c.tur === 'MUSTERI').length;
  o.acikVeresiye = cariDizi.filter((c) => c.tur === 'MUSTERI' && typeof c.tlBakiye === 'number' && c.tlBakiye > 0).length;
  const urunler = await istek('GET', '/urunler?aktif=true&limit=500', { jeton });
  const urunDizi = Array.isArray(urunler) ? urunler : [];
  o.urunSayisi = urunDizi.length;
  o.stoklar = {};
  for (const u of urunDizi) {
    const s = await istek('GET', `/urunler/${u.id}/stok`, { jeton });
    // Bilinmeyen biçim = null = V8 TUTMAZ (fail-closed; 0 varsayılmaz).
    o.stoklar[u.kod] = typeof s?.toplamAdet === 'number' ? s.toplamAdet : null;
  }
  const satislar = await istek('GET', '/islemler/iade-adaylari?tur=SATIS&limit=50', { jeton });
  const satisDizi = Array.isArray(satislar) ? satislar : [];
  o.satisSayisi = satisDizi.length;
  // Tutarlar SUNUCUNUN defterinden (tlTutari); ödeme türü sunucunun bu listesinde
  // yok, bu yüzden nakit sınırı betiğin GÖNDERDİĞİ ödeme türüyle eşlenir.
  o.satisTutarlari = satisDizi.map((x) => Number(x.tlTutari));
  o.nakitSatisTutarlari = fisler.filter((f) => f.odeme === 'NAKIT').map((f) => f.tutar);
  const em = await istek('GET', `/emanet/${cari[SENARYO.emanet.cari].id}`, { jeton });
  o.emanetBakiye = typeof em?.mevcutHasGramBakiye === 'number' ? em.mevcutHasGramBakiye : null;
  const bos = await istek('GET', `/emanet/${cari.EMANET_BOS.id}`, { jeton });
  o.emanetBosBakiye = typeof bos?.mevcutHasGramBakiye === 'number' ? bos.mevcutHasGramBakiye : null;
  const k = await istek('GET', '/kur/guncel', { jeton });
  o.kurId = k?.id || null;
  const g = await istek('GET', '/gorunum', { jeton });
  o.sema = g?.sema ?? null;
  o.yaziOlcegi = g?.yaziOlcegi ?? null;
  const pano = await istek('GET', '/pano/ayarlar', { jeton });
  const kalemler = Array.isArray(pano) ? pano : (pano?.kalemler || pano?.ayarlar || []);
  o.vitrinAktif = kalemler.filter((x) => x.aktif).length;
  const adlar = [...cariDizi.map((c) => c.ad), ...urunDizi.map((u) => u.ad), ...urunDizi.map((u) => u.kod),
    ...(kasalar || []).map((x) => x.ad)];
  o.yasakKelimeli = adlar.filter((a) => YASAK_KELIME.test(a || ''));
  o.demoOnekliKod = urunDizi.map((u) => u.kod).filter((kod) => /^DEMO-/i.test(kod || ''));
  const tev = await istek('GET', '/tevsik/gunluk', { jeton });
  o.tevsikEsigi = typeof tev?.had === 'number' ? tev.had : null;
  o.ucuzFiyat = teklif[SENARYO.ucuzUrun]?.satisFiyati ?? null;
  o.pahaliFiyat = teklif[SENARYO.pahaliUrun]?.satisFiyati ?? null;
  // Panel ile AYNI formül ve AYNI uç (Pages/Panel.razor NetKasaTl): satış − alış − hurda.
  const yerelBugun = new Date(Date.now() - new Date().getTimezoneOffset() * 60000).toISOString().slice(0, 10);
  const gun = await istek('GET', `/raporlar/gunluk?tarih=${yerelBugun}`, { jeton });
  const sayi = (x) => (typeof x === 'number' ? x : null);
  const [st, at, ht] = [sayi(gun?.satis?.toplamTl), sayi(gun?.alis?.toplamTl), sayi(gun?.hurda?.odenenTl)];
  o.panelNetTl = st === null || at === null || ht === null ? null : +(st - at - ht).toFixed(2);
  o.kurSnapshotId = kur.id;
  return o;
}

// ═══ PAKET YASAL-LISTE-DEMO — YASAL LİSTE (YAPTIRIM) ADIMI ══════════════════
// Panel'deki 'Yasal liste henüz yüklenmedi' bandı DOĞRU bir uyarıdır: liste
// kiracı-üstüdür (yaptirim_kayitlari) ve sistemde hiç yüklenmemişse her kiracı
// bunu görür. Bu adım listeyi YALNIZ birincil kaynaktan indirilmiş YEREL
// ÖNBELLEKTEN yükler (KUYUMCU_YAPTIRIM_ONBELLEK = POST /sistem/yaptirim-listesi
// gövdesi biçiminde JSON). UYDURMA/ÖRNEK LİSTE YOKTUR: önbellek yoksa adım
// YÜKLEMEZ, durumu rapora 'YUKLENMEDI' diye yazar ve bant demoda görünür kalır.
// Sistemde aynı ya da daha yeni tarihli liste varsa yeniden yüklemez (sunucu
// satırları tekilleştirmez; her yükleme yeni satır ekler).
// Konak listesi sunucudaki YaptirimServisi.BirincilKaynakKonaklari ile AYNIDIR.
const BIRINCIL_KONAKLAR = ['masak.hmb.gov.tr', 'ms.hmb.gov.tr', 'www.resmigazete.gov.tr', 'resmigazete.gov.tr',
  'scsanctions.un.org', 'main.un.org', 'www.un.org'];

/** Önbellek gövdesini sınar (saf, ağ yok). Boş dizi = yüklenebilir. */
function yasalListeOnbellekDogrula(g) {
  const e = [];
  if (!g || typeof g !== 'object') return ['gövde JSON nesnesi değil'];
  let u = null;
  try { u = new URL(String(g.kaynakUrl || '')); } catch { /* aşağıda */ }
  if (!u || u.protocol !== 'https:') e.push('kaynakUrl https:// ile başlamalı');
  else if (!BIRINCIL_KONAKLAR.includes(u.hostname.toLowerCase())) e.push(`kaynakUrl birincil kaynak değil (${u.hostname})`);
  if (!String(g.kaynak || '').trim()) e.push('kaynak (resmî liste adı) boş');
  if (!/^\d{4}-\d{2}-\d{2}$/.test(String(g.listeTarihi || ''))) e.push('listeTarihi YYYY-AA-GG değil');
  if (!Array.isArray(g.ekle) || g.ekle.length === 0) e.push('ekle boş — liste satırı yok');
  else g.ekle.forEach((s, i) => { if (!String(s?.adSoyad || '').trim()) e.push(`${i + 1}. satırda adSoyad boş`); });
  return e;
}

/**
 * Sunucu özetinden liste durumu (saf). Betik sunucunun listeEski bayrağına körü körüne
 * güvenmez: liste tarihi yoksa liste YOKTUR (fail-closed). Tarih yokken listeEski=false
 * gelirse sunucu ikilisi fail-open demektir (02.10'da güncel olmayan Debug ikilisinde
 * görüldü) → hata fırlatılır, demo "düzelmiş" gibi raporlanmaz (Anayasa md.1).
 */
function yasalListeDurumu(ozet) {
  if (!ozet || !ozet.listeTarihi) {
    if (ozet && ozet.listeEski === false)
      throw new Error('TUTARSIZ: sunucu liste tarihi yokken listeEski=false döndü (fail-open) — API ikilisini kaynaktan yeniden derleyin.');
    return 'YUKLENMEDI';
  }
  return ozet.listeEski === false ? 'SISTEMDE_GUNCEL' : 'ESKI';
}

async function yasalListeAdimi(jeton, olc) {
  const oku = () => istek('GET', '/masak/uyum-ozet', { jeton });
  const once = await olc('yasal-liste-oku', oku);
  const yol = (process.env.KUYUMCU_YAPTIRIM_ONBELLEK || '').trim();
  const sonuc = (durum, neden, ozet) => ({
    durum, neden, listeTarihi: ozet?.listeTarihi ?? null, listeEski: ozet?.listeEski ?? null,
  });
  const d0 = yasalListeDurumu(once);
  if (!yol) {
    console.error('UYARI: yasal liste önbelleği verilmedi (KUYUMCU_YAPTIRIM_ONBELLEK) — '
      + (d0 === 'SISTEMDE_GUNCEL' ? 'sistemdeki liste güncel.' : "Panel'de yasal liste bandı görünecek (doğru uyarı)."));
    return sonuc(d0, 'yerel önbellek yok (birincil kaynaktan indirilmiş liste gerekir)', once);
  }
  const g = JSON.parse(fs.readFileSync(yol, 'utf8'));
  const eksik = yasalListeOnbellekDogrula(g);
  if (eksik.length) throw new Error(`Yasal liste önbelleği reddedildi: ${eksik.join('; ')}`);
  if (once?.listeTarihi && once.listeTarihi >= g.listeTarihi)
    return sonuc('SISTEMDE_GUNCEL', `sistemdeki liste (${once.listeTarihi}) önbellekten eski değil — yeniden yüklenmedi`, once);
  const y = await olc('yasal-liste-yukle', () => istek('POST', '/sistem/yaptirim-listesi', { sistem: true, govde: g }));
  const sonra = await oku();
  if (yasalListeDurumu(sonra) !== 'SISTEMDE_GUNCEL') throw new Error(`Yasal liste yüklendi (${y?.eklenen} satır) ama hâlâ eski görünüyor: liste tarihi ${sonra?.listeTarihi}`);
  return { ...sonuc('YUKLENDI', `${y?.eklenen} satır · ${g.kaynakUrl}`, sonra), eklenen: y?.eklenen ?? null };
}

async function tohumla() {
  adiDogrula(AD);
  if (!ANAHTAR) throw new Error('KUYUMCU_SISTEM_ANAHTARI ortam değişkeni boş — davet üretilemez (uç fail-closed).');

  const adimlar = [];
  const olc = async (ad, f) => {
    const t0 = Date.now();
    const sonuc = await f();
    adimlar.push({ adim: ad, ms: Date.now() - t0 });
    return sonuc;
  };
  const t0 = Date.now();

  // 0) MEVCUT KİRACIYA TOHUMLAMA (--eposta ile): kayıt ucu IP başına 3/saat
  //    oran sınırlıdır; tekrar koşumlarda yeni kiracı açmak yerine var olanın
  //    içine yazılır. Yeni kiracı açmıyorsa davet de üretilmez.
  const mevcutEposta = argOku('--eposta');
  if (mevcutEposta) {
    const sifreM = argOku('--sifre') || 'DemoVitrin123!';
    const girisM = await olc('giris', () => istek('POST', '/auth/giris', {
      govde: { email: mevcutEposta, sifre: sifreM },
    }));
    if (!girisM.accessToken) throw new Error('Mevcut kiracıya giriş yapılamadı (accessToken yok).');
    const dolduM = await icerikDoldur(girisM.accessToken, girisM, olc, `-${Date.now().toString().slice(-5)}`);
    const yasalListeM = await yasalListeAdimi(girisM.accessToken, olc);
    return {
      kiraci: { ad: AD, id: girisM.kullanici?.tenantId || null, prova: PROVA, mod: 'MEVCUT_KIRACI' },
      takvim: 'TEK_GUN', sayimlar: dolduM.sayimlar, senaryo: dolduM.senaryo, yasalListe: yasalListeM, adimlar, toplamMs: Date.now() - t0,
    };
  }

  // 1) Davet (sistem anahtarı) — kayıt ucu davetsiz açılmaz (F-2-min).
  const davet = await olc('davet', () => istek('POST', '/sistem/davetler', {
    sistem: true, govde: { isletmeAdi: AD, denemeGun: 90, gecerlilikSaat: 2, not: 'P-30 demo tohumlama',
      gerekce: 'P-30 demo/prova kiracısı tohumlama daveti (gösterim hazırlığı)' },
  }));
  const jetonu = davet.jeton || davet.davetJetonu || davet.token;
  if (!jetonu) throw new Error(`Davet yanıtında jeton alanı yok: ${JSON.stringify(davet).slice(0, 200)}`);

  // 2) Kiracı + SAHİP kullanıcı
  // --sahip-eposta: kurucunun demo sabahı hatırlayacağı giriş adresi (isteğe bağlı).
  const eposta = argOku('--sahip-eposta') || `demo-${Date.now()}@ornek.local`;
  const sifre = 'DemoVitrin123!';
  await olc('kayit', () => istek('POST', '/auth/kayit', {
    govde: { isletmeAdi: AD, ad: 'Demo', soyad: 'Kuyumcu', email: eposta, sifre, davetJetonu: jetonu },
  }));

  // 3) Giriş
  const giris = await olc('giris', () => istek('POST', '/auth/giris', { govde: { email: eposta, sifre } }));
  const jeton = giris.accessToken;
  if (!jeton) throw new Error(`Giriş yanıtında accessToken yok: ${JSON.stringify(giris).slice(0, 200)}`);
  const tenantId = giris.kullanici?.tenantId || null;

  const doldu = await icerikDoldur(jeton, giris, olc);
  const yasalListe = await yasalListeAdimi(jeton, olc);   // YASAL-LISTE-DEMO

  return {
    // Prova turu bu kiracıya --eposta ile girer; e-posta rapora yazılmazsa
    // ikinci ajan giriş bilgisini veritabanından kazımak zorunda kalır.
    // (Parola YAZILMAZ — sır çıktıya düşmez; varsayılan betikte sabittir.)
    kiraci: { ad: AD, id: tenantId, prova: PROVA, eposta },
    takvim: 'TEK_GUN',   // ⚠ geçmişe tarihli DEĞİL — yukarıdaki dürüstlük notu
    sayimlar: doldu.sayimlar,
    senaryo: doldu.senaryo,     // P-D3: V1–V17 sunucudan geri okunmuş ölçüm
    yasalListe,                 // YASAL-LISTE-DEMO: YUKLENDI / SISTEMDE_GUNCEL / ESKI / YUKLENMEDI
    adimlar,
    toplamMs: Date.now() - t0,
  };
}

async function ana() {
  try {
    // P-D3: --plan-dogrula ağ çağrısı yapmadan planı DEMO-SENARYOSU § 3'e karşı sınar.
    if (BAYRAK('--plan-dogrula')) {
      const eksik = senaryoPlaniniDogrula(SENARYO);
      console.log(JSON.stringify({ plan: 'DEMO-SENARYOSU § 3', eksik }, null, 2));
      process.exit(eksik.length ? 1 : 0);
    }
    const sonuc = BAYRAK('--prova')
      ? await prova(argOku('--eposta'), argOku('--sifre') || 'DemoVitrin123!')
      : await tohumla();
    fs.mkdirSync(HAM, { recursive: true });
    // ⚠ Saniye çözünürlüğü YETMEZ: iki ardışık prova aynı saniyede biterse ikincisi
//   birincinin dosyasını EZER ve 'iki tur' kanıtı sessizce tek tura düşerdi.
const damga = new Date().toISOString().slice(0, 23).replace(/[:T.]/g, '-');
    const prv = sonuc.tur === 'GOSTERIM_PROVASI';
    const yol = argOku('--rapor')
      || path.join(HAM, prv ? `demo-prova-${damga}.json` : `demo-tohum-${damga}.json`);
    fs.writeFileSync(yol, JSON.stringify(sonuc, null, 2), 'utf8');
    if (prv) {
      console.log(`PROVA: kritik=${sonuc.kritik} · veri yolu ${sonuc.veriYoluMs} ms · `
        + `insan bütçesi ${sonuc.insanDkToplam} dk · rapor: ${yol}`);
      if (sonuc.kritik > 0) process.exit(2);
    } else {
      console.log(`TOHUMLANDI: ${sonuc.kiraci.ad} · ${sonuc.toplamMs} ms · rapor: ${yol}`);
      console.log(JSON.stringify(sonuc.sayimlar));
    }
  } catch (e) {
    console.error(`BAŞARISIZ: ${e.message}`);
    // Ölçüm yapılmışsa KANIT olarak yazılır — başarısızlık gizlenmez (Anayasa md.1).
    if (e.rapor) {
      const yolH = argOku('--rapor')
        || path.join(HAM, `demo-tohum-KARSILANMADI-${new Date().toISOString().slice(0, 23).replace(/[:T.]/g, '-')}.json`);
      try { fs.mkdirSync(path.dirname(yolH), { recursive: true }); fs.writeFileSync(yolH, JSON.stringify({ basarisiz: true, ...e.rapor }, null, 2), 'utf8'); console.error(`ölçüm: ${yolH}`); }
      catch (y) { console.error(`ölçüm yazılamadı: ${y.message}`); }
    }
    process.exit(1);
  }
}

// Testler saf fonksiyonları ağ çağrısı yapmadan kullanır (require); betik
// doğrudan koşulduğunda davranış DEĞİŞMEZ.
module.exports = { SENARYO, senaryoPlaniniDogrula, senaryoOlcumunuDegerlendir, ean13, kurBekle, KUR_BEKLEME_AZAMI_SN,
  BIRINCIL_KONAKLAR, yasalListeOnbellekDogrula, yasalListeDurumu };
if (require.main === module) ana();
