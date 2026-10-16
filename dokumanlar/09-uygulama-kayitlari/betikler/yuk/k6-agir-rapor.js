// ═════════════════════════════════════════════════════════════════════════════
// PAKET OLCEK-YUK — sınırsız liste düzeltmelerinin YÜK ALTINDA önce/sonra ölçümü.
// Büyük kiracılar (K001–K003: 50.000 ürün · 5.000 cari · 180.000 hareket / 3 yıl),
// kiracı başına 2 personel; her tur: vade takibi · ürün bazlı kâr (3 yıl) · fiş araması.
// Bu uçlar düzeltmeden önce kiracının TÜM cari / ürün kataloğunu belleğe alıyordu.
//   KIRACI_DOSYASI=... SURE=2m k6 run k6-agir-rapor.js
// ═════════════════════════════════════════════════════════════════════════════
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Counter } from 'k6/metrics';
import { SharedArray } from 'k6/data';

const API = `${__ENV.TABAN || 'http://localhost:5166'}/api/v1`;
// PAKET OLCEK-YUK t2 (doğrulayıcı DÜŞÜK): varsayılan parola YOK — YUK_SIFRE zorunlu, yoksa betik durur.
const SIFRE = __ENV.YUK_SIFRE;
if (!SIFRE) throw new Error("YUK_SIFRE ortam değişkeni zorunlu (varsayılan parola yok)");
const kiracilar = new SharedArray('k', () => JSON.parse(open(__ENV.KIRACI_DOSYASI)).kiracilar
  .sort((a, b) => a.no.localeCompare(b.no)).slice(0, 3));
const t = { vade: new Trend('uc_vade_takibi', true), kar: new Trend('uc_kar_zarar_urun', true), arama: new Trend('uc_fis_arama', true) };
const s429 = new Counter('sayi_429');

export const options = {
  scenarios: { agir: { executor: 'constant-vus', vus: kiracilar.length * 2, duration: __ENV.SURE || '2m' } },
  thresholds: { 'http_req_failed': ['rate<0.01'], 'sayi_429': ['count<1'] },
  summaryTrendStats: ['avg', 'med', 'p(95)', 'p(99)', 'max', 'count'],
};

let h = null;
const bugun = new Date().toISOString().slice(0, 10);
const ucYilOnce = new Date(Date.now() - 1095 * 864e5).toISOString().slice(0, 10);
export default function () {
  const k = kiracilar[Math.floor((__VU - 1) / 2) % kiracilar.length];
  const temel = { 'Content-Type': 'application/json', 'X-Forwarded-For': k.xff };
  if (!h) {
    const g = http.post(`${API}/auth/giris`, JSON.stringify({ email: k.kullanicilar[(__VU - 1) % 2], sifre: SIFRE }), { headers: temel });
    if (g.status !== 200) { sleep(5); return; }
    h = { ...temel, Authorization: `Bearer ${g.json('accessToken')}` };
  }
  const ok = r => { if (r.status === 429) s429.add(1); return r.status === 200; };
  const v = http.get(`${API}/raporlar/vade-takibi?ufukGun=30`, { headers: h }); t.vade.add(v.timings.duration); check(v, { vade: ok });
  sleep(1);
  const kz = http.get(`${API}/raporlar/kar-zarar/urun?baslangic=${ucYilOnce}&bitis=${bugun}&limit=50`, { headers: h }); t.kar.add(kz.timings.duration); check(kz, { kar: ok });
  sleep(1);
  const a = http.get(`${API}/arama?q=${encodeURIComponent('Müşteri')}&tur=FIS&limit=20`, { headers: h }); t.arama.add(a.timings.duration); check(a, { arama: ok });
  sleep(1);
}

export function handleSummary(d) {
  const v = (m, a) => (d.metrics[m] ? d.metrics[m].values[a] : null);
  const o = { istek: v('http_reqs', 'count'), hata: v('http_req_failed', 'rate'), sayi429: v('sayi_429', 'count') || 0 };
  for (const m of ['uc_vade_takibi', 'uc_kar_zarar_urun', 'uc_fis_arama'])
    o[m] = { n: v(m, 'count'), p50: v(m, 'med'), p95: v(m, 'p(95)'), p99: v(m, 'p(99)'), max: v(m, 'max') };
  const out = { stdout: JSON.stringify(o) + '\n' };
  if (__ENV.OZET) out[__ENV.OZET] = JSON.stringify(o, null, 1);
  return out;
}
