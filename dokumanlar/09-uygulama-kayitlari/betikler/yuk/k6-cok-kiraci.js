// ═════════════════════════════════════════════════════════════════════════════
// PAKET OLCEK-YUK — ÇOK KİRACILI TEZGÂH YÜKÜ (10 / 50 / 100 kiracı)
//
// Kaynak: HAZIRLIK-KURULU-5.md madde 13. Önceki araç (dotnet/yuk-testi/k6-senaryo.js,
// R-6/P-25) tek kiracı · 10 VU · 55 sn idi; bu betik onu çok kiracıya genişletir.
//
// PROFİL (gerçekçi tezgâh karışımı) — kiracı başına KULLANICI_BASINA eşzamanlı personel
// (varsayılan 2: SAHIP + MUDUR, her biri kendi hesabıyla). Her sanal kullanıcı:
//   giriş (oturum başında 1 kez) → döngü: panel · kur · ürün arama · fiyat teklifi ·
//   satış fişi (her 2 turda 1) · cari arama · kasa hareketleri · rapor (her 4 turda 1)
//   her istek arası 2–5 sn düşünme süresi (insan tezgâhtar; ölçülen ~14–18 istek/dk/kişi).
// Her kiracı kendi hattından gelir (X-Forwarded-For) — dükkânlar ayrı IP'dir.
//
// ⚠ ORAN SINIRI GEVŞETİLMEZ. 429 ayrıca sayılır; normal kullanımda hedef 0'dır.
// ⚠ http_req_failed 429'u da hata sayar (k6 varsayılanı: 4xx/5xx) — bu bilerek
//   korunur: kabul eşiği "failed < %1" 429'u da içerir.
//
// Çalıştırma:
//   KIRACI_DOSYASI=/private/tmp/kuyumcu-serit/OLCEK-YUK/kiracilar.json KIRACI=10 \
//   SURE=5m TABAN=http://localhost:5166 k6 run --summary-export ozet-10.json k6-cok-kiraci.js
// ═════════════════════════════════════════════════════════════════════════════
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate, Counter } from 'k6/metrics';
import { SharedArray } from 'k6/data';

const TABAN = __ENV.TABAN || 'http://localhost:5166';
const API = `${TABAN}/api/v1`;
const KIRACI = parseInt(__ENV.KIRACI || '10', 10);
const KB = parseInt(__ENV.KULLANICI_BASINA || '2', 10);
const SURE = __ENV.SURE || '5m';
const RAMPA = __ENV.RAMPA || '60s';
// PAKET OLCEK-YUK t2 (doğrulayıcı DÜŞÜK): varsayılan parola YOK — YUK_SIFRE zorunlu, yoksa betik durur.
const SIFRE = __ENV.YUK_SIFRE;
if (!SIFRE) throw new Error("YUK_SIFRE ortam değişkeni zorunlu (varsayılan parola yok)");

const kiracilar = new SharedArray('kiracilar', () =>
  JSON.parse(open(__ENV.KIRACI_DOSYASI)).kiracilar
    .sort((a, b) => a.no.localeCompare(b.no)).slice(0, KIRACI));

const VU = kiracilar.length * KB;
const oran429 = new Rate('oran_siniri_429');
const sayi429 = new Counter('sayi_429');
const hata5xx = new Rate('sunucu_hatasi_5xx');
const uc = {};
for (const ad of ['giris', 'panel', 'kur', 'urun_arama', 'fiyat_teklif', 'satis_fisi', 'cari_arama', 'kasa_hareket', 'rapor',
  'r_gunluk', 'r_aylik', 'r_stok_yaslandirma', 'r_stok_degeri', 'r_islem_defteri'])
  uc[ad] = new Trend(`uc_${ad}`, true);

export const options = {
  discardResponseBodies: false,
  scenarios: {
    tezgah: {
      executor: 'ramping-vus', startVUs: 0,
      stages: [{ duration: RAMPA, target: VU }, { duration: SURE, target: VU }, { duration: '10s', target: 0 }],
      gracefulRampDown: '10s',
    },
  },
  thresholds: {
    'http_req_duration{faz:kararli}': ['p(95)<800'],
    'http_req_failed{faz:kararli}': ['rate<0.01'],
    'sayi_429': ['count<1'],
    'sunucu_hatasi_5xx': ['rate<0.001'],
  },
  summaryTrendStats: ['avg', 'med', 'p(90)', 'p(95)', 'p(99)', 'max', 'count'],
};

const BASLANGIC = Date.now();
const rampaMs = (() => { const m = /^(\d+)(s|m)$/.exec(RAMPA); return m ? (+m[1]) * (m[2] === 'm' ? 60000 : 1000) : 60000; })();

function olc(ad, yanit) {
  uc[ad].add(yanit.timings.duration);
  oran429.add(yanit.status === 429);
  if (yanit.status === 429) sayi429.add(1);
  hata5xx.add(yanit.status >= 500);
  return yanit;
}
function etiket(ad) { return { ad, faz: (Date.now() - BASLANGIC) > rampaMs ? 'kararli' : 'rampa' }; }
const dus = () => sleep(2 + Math.random() * 3);

let oturum = null;
export default function () {
  const vu = __VU - 1;
  const k = kiracilar[Math.floor(vu / KB) % kiracilar.length];
  const eposta = k.kullanicilar[vu % KB];
  const temel = { 'Content-Type': 'application/json', 'X-Forwarded-For': k.xff };

  if (!oturum) {
    const g = olc('giris', http.post(`${API}/auth/giris`, JSON.stringify({ email: eposta, sifre: SIFRE }), { headers: temel, tags: etiket('giris') }));
    if (!check(g, { 'giriş 200': r => r.status === 200 })) { sleep(5); return; }
    oturum = { h: { ...temel, Authorization: `Bearer ${g.json('accessToken')}` }, tur: 0 };
    dus();
  }
  const h = oturum.h;
  const tur = oturum.tur++;
  const kabul = r => r.status === 200 || r.status === 201;

  check(olc('panel', http.get(`${API}/pano/guncel`, { headers: h, tags: etiket('panel') })), { panel: kabul }); dus();
  const kurY = olc('kur', http.get(`${API}/kur/guncel`, { headers: h, tags: etiket('kur') }));
  check(kurY, { kur: kabul });
  const kurId = kurY.status === 200 ? (kurY.json('id') || k.kurId) : k.kurId;
  dus();
  const aranan = `#${Math.floor(Math.random() * 2000)}`;
  check(olc('urun_arama', http.get(`${API}/urunler?q=${encodeURIComponent(aranan)}&limit=50`, { headers: h, tags: etiket('urun_arama') })), { 'ürün arama': kabul });
  dus();
  const urunId = k.urunler[tur % k.urunler.length];
  check(olc('fiyat_teklif', http.get(`${API}/fiyat/teklif?urunId=${urunId}&kurSnapshotId=${kurId}`, { headers: h, tags: etiket('fiyat_teklif') })), { 'fiyat teklifi': kabul });
  dus();
  if (tur % 2 === 0) {
    const tutar = 3000 + (tur % 10) * 100;
    const s = olc('satis_fisi', http.post(`${API}/islemler/satis-fisi`, JSON.stringify({
      kalemler: [{ urunId, adet: 1, birimFiyatTl: tutar }],
      odemeler: [{ arac: 'NAKIT', tutarTl: tutar, kasaId: k.kasaId }],
      kurSnapshotId: kurId, aciklama: 'OLCEK-YUK tezgâh satışı',
    }), { headers: h, tags: etiket('satis_fisi') }));
    check(s, { 'satış fişi': kabul });
    dus();
  }
  check(olc('cari_arama', http.get(`${API}/cari?q=${encodeURIComponent('Müşteri')}&limit=50`, { headers: h, tags: etiket('cari_arama') })), { 'cari arama': kabul });
  dus();
  check(olc('kasa_hareket', http.get(`${API}/kasalar/${k.kasaId}/hareketler?limit=50`, { headers: h, tags: etiket('kasa_hareket') })), { 'kasa hareketleri': kabul });
  dus();
  if (tur % 4 === 1) {
    const raporlar = ['raporlar/gunluk', 'raporlar/aylik', 'raporlar/stok-yaslandirma', 'raporlar/stok-degeri', 'raporlar/islem-defteri?limit=100&ofset=0'];
    const r = raporlar[Math.floor(tur / 4) % raporlar.length];
    const ry = olc('rapor', http.get(`${API}/${r}`, { headers: h, tags: etiket('rapor') }));
    uc['r_' + r.split('?')[0].replace('raporlar/', '').replace(/-/g, '_')].add(ry.timings.duration);
    check(ry, { rapor: kabul });
    dus();
  }
}

export function handleSummary(d) {
  const m = d.metrics;
  const v = (ad, a) => (m[ad] && m[ad].values[a] !== undefined ? m[ad].values[a] : null);
  const satir = { kiraci: kiracilar.length, vu: VU, sure: SURE,
    istek: v('http_reqs', 'count'), rps: v('http_reqs', 'rate'),
    p50: v('http_req_duration{faz:kararli}', 'med'), p95: v('http_req_duration{faz:kararli}', 'p(95)'),
    p99: v('http_req_duration{faz:kararli}', 'p(99)'), hata: v('http_req_failed{faz:kararli}', 'rate'),
    sayi429: v('sayi_429', 'count') || 0, oran5xx: v('sunucu_hatasi_5xx', 'rate'), uclar: {} };
  for (const ad of Object.keys(uc)) satir.uclar[ad] = { n: v(`uc_${ad}`, 'count'), p50: v(`uc_${ad}`, 'med'), p95: v(`uc_${ad}`, 'p(95)'), p99: v(`uc_${ad}`, 'p(99)') };
  const out = { stdout: JSON.stringify(satir) + '\n' };
  if (__ENV.OZET) out[__ENV.OZET] = JSON.stringify(satir, null, 1);
  return out;
}
