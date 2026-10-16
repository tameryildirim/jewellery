// ═════════════════════════════════════════════════════════════════════════════
// PAKET RAPOR-4 tur 2 — ürün bazlı kâr (/raporlar/kar-zarar/urun) DEĞİŞKEN aralık/şube/sayfa yükü.
// Hasım bulgusu (ORTA): k6-agir-rapor.js aynı 3 kiracıyla AYNI 3 yıllık aralığı tekrar tekrar istiyor →
// aralık toplamı önbelleğine (anahtar = ay aralığı + şube + pencere) hep isabet eder, en iyi durumu ölçer.
// Bu senaryo her istekte aralığın başını (30–1095 gün önce) ve sonunu (0–20 gün önce) rastgele seçer,
// şube süzgecini yarı yarıya açar, sayfayı (ofset 0/50/100) değiştirir — ay kovası önbelleği paylaşılır
// ama aralık toplamı ve canlı parça çoğunlukla YENİDEN kurulur (gerçek kullanıma yakın en kötü durum).
// Ayrıca her turda bir "sabit 3 yıl" isteği (karşılaştırma için aynı koşulda).
//   YUK_SIFRE=... TABAN=http://localhost:5160 KIRACI_DOSYASI=... SURE=2m OZET=... k6 run -q k6-kar-degisken.js
// Varsayılan parola YOK; tohum (seed) sabit verilebilir: TOHUM=42.
// ═════════════════════════════════════════════════════════════════════════════
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Counter } from 'k6/metrics';
import { SharedArray } from 'k6/data';

const API = `${__ENV.TABAN || 'http://localhost:5160'}/api/v1`;
const SIFRE = __ENV.YUK_SIFRE;
if (!SIFRE) throw new Error("YUK_SIFRE ortam değişkeni zorunlu (varsayılan parola yok)");
const kiracilar = new SharedArray('k', () => JSON.parse(open(__ENV.KIRACI_DOSYASI)).kiracilar
  .sort((a, b) => a.no.localeCompare(b.no)).slice(0, 3));
const t = {
  degisken: new Trend('uc_kar_degisken', true),
  sabit: new Trend('uc_kar_sabit_3yil', true),
  vade: new Trend('uc_vade_takibi', true),
};
const s429 = new Counter('sayi_429');

export const options = {
  scenarios: { degisken: { executor: 'constant-vus', vus: kiracilar.length * 2, duration: __ENV.SURE || '2m' } },
  thresholds: { 'http_req_failed': ['rate<0.01'], 'sayi_429': ['count<1'] },
  summaryTrendStats: ['avg', 'med', 'p(95)', 'p(99)', 'max', 'count'],
};

// Deterministik sözde rastgele (VU başına ayrı akış) — koşumlar tekrarlanabilir.
let durum = null;
function rnd() {
  if (durum === null) durum = (parseInt(__ENV.TOHUM || '42', 10) * 7919 + __VU * 104729) >>> 0;
  durum = (durum * 1664525 + 1013904223) >>> 0;
  return durum / 4294967296;
}
const gun = (n) => new Date(Date.now() - n * 864e5).toISOString().slice(0, 10);

let h = null;
export default function () {
  const k = kiracilar[Math.floor((__VU - 1) / 2) % kiracilar.length];
  const temel = { 'Content-Type': 'application/json', 'X-Forwarded-For': k.xff };
  if (!h) {
    const g = http.post(`${API}/auth/giris`, JSON.stringify({ email: k.kullanicilar[(__VU - 1) % 2], sifre: SIFRE }), { headers: temel });
    if (g.status !== 200) { sleep(5); return; }
    h = { ...temel, Authorization: `Bearer ${g.json('accessToken')}` };
  }
  const ok = r => { if (r.status === 429) s429.add(1); return r.status === 200; };

  const sonGun = Math.floor(rnd() * 21);                    // bitiş: 0–20 gün önce
  const basGun = 30 + Math.floor(rnd() * 1066);             // başlangıç: 30–1095 gün önce
  const sube = rnd() < 0.5 && k.subeId ? `&subeId=${k.subeId}` : '';
  const ofset = [0, 50, 100][Math.floor(rnd() * 3)];
  const d = http.get(`${API}/raporlar/kar-zarar/urun?baslangic=${gun(basGun)}&bitis=${gun(sonGun)}&limit=50&ofset=${ofset}${sube}`,
    { headers: h, tags: { ad: 'kar_degisken' } });
  t.degisken.add(d.timings.duration); check(d, { degisken: ok });
  sleep(1);
  const v = http.get(`${API}/raporlar/vade-takibi?ufukGun=30`, { headers: h }); t.vade.add(v.timings.duration); check(v, { vade: ok });
  sleep(1);
  const s = http.get(`${API}/raporlar/kar-zarar/urun?baslangic=${gun(1095)}&bitis=${gun(0)}&limit=50`, { headers: h, tags: { ad: 'kar_sabit' } });
  t.sabit.add(s.timings.duration); check(s, { sabit: ok });
  sleep(1);
}

export function handleSummary(d) {
  const v = (m, a) => (d.metrics[m] ? d.metrics[m].values[a] : null);
  const o = { istek: v('http_reqs', 'count'), hata: v('http_req_failed', 'rate'), sayi429: v('sayi_429', 'count') || 0 };
  for (const m of ['uc_kar_degisken', 'uc_kar_sabit_3yil', 'uc_vade_takibi'])
    o[m] = { n: v(m, 'count'), p50: v(m, 'med'), p95: v(m, 'p(95)'), p99: v(m, 'p(99)'), max: v(m, 'max') };
  const out = { stdout: JSON.stringify(o) + '\n' };
  if (__ENV.OZET) out[__ENV.OZET] = JSON.stringify(o, null, 1);
  return out;
}
