// ═════════════════════════════════════════════════════════════════════════════
// PAKET OLCEK-YUK — çok kiracılı yük testi için kiracıları ÜRÜNÜN UÇLARIYLA kurar.
//
// Her kiracı: sistem daveti → kayıt (SAHIP) → giriş → elle kur → kasa →
// N_URUN şablon ürün (açılış stoğuyla) → N_CARI cari → 1 MUDUR kullanıcı →
// birkaç gerçek satış fişi (hacim büyütmesi SQL klonlamayla, yuk-tohum.sql).
//
// ⚠ Kayıt ucu IP başına 3/saat, davet 20/saat, giriş IP+e-posta başına 5/dk.
//   Ölçülen ortamda her kiracı AYRI bir dükkândır ve ayrı bir hattan gelir;
//   bunu X-Forwarded-For ile taklit ederiz (sunucu loopback'i bilinen vekil sayar,
//   Program.cs ForwardedHeaders). Oran sınırı GEVŞETİLMEZ.
//
// Sır yok: sistem anahtarı dosyadan okunur (chmod 600), çıktıya basılmaz.
// Parola sabit bir TEST parolasıdır; kimlik dosyası (cikti) chmod 600 yazılır.
//
// Kullanım:
//   TABAN=http://localhost:5166 ANAHTAR_DOSYASI=/private/tmp/.../sistem.anahtar \
//   KIRACI=100 BASLA=1 CIKTI=/private/tmp/kuyumcu-serit/OLCEK-YUK/kiracilar.json \
//   node yuk-kiraci-kur.mjs
// ═════════════════════════════════════════════════════════════════════════════
import fs from 'node:fs';

const TABAN = process.env.TABAN || 'http://localhost:5166';
const API = `${TABAN}/api/v1`;
const KIRACI = parseInt(process.env.KIRACI || '100', 10);
const BASLA = parseInt(process.env.BASLA || '1', 10);
const PARALEL = parseInt(process.env.PARALEL || '5', 10);
const CIKTI = process.env.CIKTI || '/private/tmp/kuyumcu-serit/OLCEK-YUK/kiracilar.json';
// PAKET OLCEK-YUK t2 (doğrulayıcı DÜŞÜK): varsayılan parola YOK — YUK_SIFRE zorunlu, yoksa betik durur.
const SIFRE = process.env.YUK_SIFRE;
if (!SIFRE) { console.error("YUK_SIFRE ortam değişkeni zorunlu (varsayılan parola yok)"); process.exit(2); }
const ANAHTAR = fs.readFileSync(process.env.ANAHTAR_DOSYASI, 'utf8').trim();
const ONEK = 'PAKET-OLCEK-YUK-TEST';

const uyu = ms => new Promise(r => setTimeout(r, ms));
const ip = i => `10.66.${Math.floor(i / 250)}.${(i % 250) + 1}`;

async function istek(metot, yol, { govde, jeton, sistem, xff } = {}) {
  const h = { 'Content-Type': 'application/json' };
  if (jeton) h.Authorization = `Bearer ${jeton}`;
  if (sistem) { h['X-Sistem-Anahtari'] = ANAHTAR; h['X-Sistem-Operator'] = 'olcek-yuk'; }
  if (xff) h['X-Forwarded-For'] = xff;
  for (let deneme = 0; ; deneme++) {
    const c = await fetch(`${API}${yol}`, { method: metot, headers: h, body: govde === undefined ? undefined : JSON.stringify(govde) });
    const metin = await c.text();
    if (c.status === 429 && deneme < 3) { await uyu(1000 * (parseInt(c.headers.get('retry-after') || '5', 10))); continue; }
    if (!c.ok) throw new Error(`${metot} ${yol} → HTTP ${c.status}: ${metin.slice(0, 240)}`);
    return metin ? JSON.parse(metin) : null;
  }
}

async function kurBekle(jeton, xff) {
  // Yeni kiracıda kur arka plan işiyle gelir; gelmezse ELLE kur yazılır.
  try {
    return await istek('POST', '/kur/manuel', { jeton, xff, govde: { altinHasFiyati: 6827.20, gramAltinFiyati: 6861.51, usdTry: 41.2, eurTry: 48.1 } });
  } catch (e) {
    throw new Error(`elle kur yazılamadı: ${e.message}`);
  }
}

async function kiraciKur(i) {
  const no = String(i).padStart(3, '0');
  const ad = `${ONEK}-K${no}`;
  const xff = ip(i);
  const damga = Date.now().toString(36);
  const davet = await istek('POST', '/sistem/davetler', { sistem: true, xff,
    govde: { isletmeAdi: ad, denemeGun: 30, gecerlilikSaat: 2, not: 'OLCEK-YUK yük testi', gerekce: 'OLCEK-YUK çok kiracılı yük testi kiracısı (geçici, iş sonunda silinir)' } });
  const davetJetonu = davet.jeton || davet.davetJetonu || davet.token;
  const sahipEposta = `olcekyuk-k${no}-${damga}@test.local`;
  await istek('POST', '/auth/kayit', { xff, govde: { isletmeAdi: ad, ad: 'Yuk', soyad: `Sahip${no}`, email: sahipEposta, sifre: SIFRE, davetJetonu } });
  const g = await istek('POST', '/auth/giris', { xff, govde: { email: sahipEposta, sifre: SIFRE } });
  const jeton = g.accessToken;
  const tenantId = g.kullanici?.tenantId;
  const subeId = g.kullanici?.subeId || (await istek('GET', '/subeler', { jeton, xff }))?.[0]?.id;
  if (!jeton || !tenantId || !subeId) throw new Error(`${ad}: giriş/şube eksik`);
  const kur = await kurBekle(jeton, xff);
  let kasalar = await istek('GET', '/kasalar', { jeton, xff });
  let kasa = (kasalar || []).find(k => (k.paraBirimi || 'TRY') === 'TRY');
  if (!kasa) kasa = await istek('POST', '/kasalar', { jeton, xff, govde: { ad: 'Ana Kasa', subeId, paraBirimi: 'TRY' } });

  const urunler = [];
  const sablon = [
    ['ZIYNET', 'K22', 1.75, 'Çeyrek'], ['ZIYNET', 'K22', 15.2, 'Burma Bilezik'], ['ZIYNET', 'K22', 8.0, 'Zincir Kolye'],
    ['ZIYNET', 'K14', 3.2, 'Taşlı Yüzük'], ['KULCE', 'K24', 1.0, 'Gram Altın'],
  ];
  for (let u = 0; u < sablon.length; u++) {
    const [tip, ayarId, gram, uad] = sablon[u];
    const r = await istek('POST', '/urunler', { jeton, xff, govde: {
      tip, kod: `S${no}-${u}`, ad: `${uad} ${no}-${u}`, ayarId, gramAgirlik: gram, iscilikTl: 300,
      acilisStogu: { adet: 100000 } } });
    urunler.push(r.id);
  }
  const cariler = [];
  for (let c = 0; c < 3; c++) {
    const r = await istek('POST', '/cari', { jeton, xff, govde: { tur: 'MUSTERI', ad: `Müşteri ${no}-${c}`, telefon: `0532${no}${String(c).padStart(4, '0')}` } });
    cariler.push(r.id);
  }
  const mudurEposta = `olcekyuk-k${no}-m-${damga}@test.local`;
  await istek('POST', '/kullanicilar', { jeton, xff, govde: { ad: 'Yuk', soyad: `Mudur${no}`, email: mudurEposta, sifre: SIFRE, rol: 'MUDUR', subeId } });

  // Gerçek satış fişleri (klon şablonu): 2 nakit fiş.
  const fisler = [];
  for (let s = 0; s < 2; s++) {
    const r = await istek('POST', '/islemler/satis-fisi', { jeton, xff, govde: {
      kalemler: [{ urunId: urunler[s], adet: 1, birimFiyatTl: 4000 }],
      odemeler: [{ arac: 'NAKIT', tutarTl: 4000, kasaId: kasa.id }],
      kurSnapshotId: kur.id, aciklama: 'OLCEK-YUK şablon satış' } });
    fisler.push(r?.fisId || r?.id || null);
  }
  return { no, ad, tenantId, subeId, kasaId: kasa.id, kurId: kur.id, urunler, cariler, xff,
    kullanicilar: [sahipEposta, mudurEposta] };
}

const sonuc = fs.existsSync(CIKTI) ? JSON.parse(fs.readFileSync(CIKTI, 'utf8')) : { kiracilar: [] };
const varolan = new Set(sonuc.kiracilar.map(k => k.no));
const sira = [];
for (let i = BASLA; i < BASLA + KIRACI; i++) if (!varolan.has(String(i).padStart(3, '0'))) sira.push(i);
let hata = 0;
const t0 = Date.now();
while (sira.length) {
  const dilim = sira.splice(0, PARALEL);
  const r = await Promise.allSettled(dilim.map(kiraciKur));
  for (const x of r) {
    if (x.status === 'fulfilled') sonuc.kiracilar.push(x.value);
    else { hata++; console.error(`HATA: ${x.reason.message}`); }
  }
  fs.writeFileSync(CIKTI, JSON.stringify(sonuc, null, 1), { mode: 0o600 });
}
console.log(`kiracı=${sonuc.kiracilar.length} hata=${hata} süre=${((Date.now() - t0) / 1000).toFixed(1)} sn`);
process.exit(hata ? 1 : 0);
