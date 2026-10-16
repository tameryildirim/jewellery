// ═════════════════════════════════════════════════════════════════════════════
// YÜK TESTİ — TB-12 ("Yük ve ölçeklenme davranışı ölçülmedi")
//
// AMAÇ: "hızlıdır" demek yerine ÖLÇMEK. Rakamlar bu makinenin (geliştirme
// dizüstü, Docker MySQL) rakamlarıdır; üretim kapasitesi İDDİA EDİLMEZ.
//
// ⚠ ORAN SINIRI GEVŞETİLMEZ. Üretimde kullanıcı başına 100 istek/dk vardır;
// test de o sınır altında koşar. Sınırı kapatıp "şu kadar RPS" demek,
// üretimde olmayan bir sayı üretmek olurdu (Anayasa md.1).
// Bu yüzden senaryo ÇOK KULLANICILIDIR: her sanal kullanıcı kendi jetonunu
// taşır, tıpkı bir dükkândaki ayrı personel gibi.
//
// Çalıştırma:
//   k6 run yuk-testi/k6-senaryo.js
//   TABAN=http://localhost:5199 KULLANICI=10 k6 run yuk-testi/k6-senaryo.js
// ═════════════════════════════════════════════════════════════════════════════

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

const TABAN = __ENV.TABAN || 'http://localhost:5199';
const API = `${TABAN}/api/v1`;
const KULLANICI = parseInt(__ENV.KULLANICI || '10', 10);

// ─────────────────────────────────────────────────────────────────────────────
// P-25 / R-6 — ÖLÇEK MODU (50 bin ürün + 3 yıl hareket)
//
// POS modu "boş dükkânda hız" ölçer; ölçek modu "DOLU dükkânda hız" ölçer ve
// plan § P-25'in R-6 koşuludur. Fark kasıtlıdır: B-8 sunucuda toplamaya
// geçmeden koşulan bir yük testi yanıltıcı olurdu (plan risk satırı).
//
// Kurulum ÖNCEDEN yapılır (SQL ile 50k ürün / 180k hareket tohumlanır) ve
// hazır bir JETON verilir; k6 burada kiracı AÇMAZ — açsaydı ölçtüğü şey boş
// bir kiracı olurdu.
//
//   MOD=olcek TABAN=http://localhost:5198 JETON=<erişim jetonu> \
//     k6 run --summary-export ham/yuk-testi-<tarih>.json yuk-testi/k6-senaryo.js
//
// ⚠ 5199 KURUCUNUN inceleme sunucusudur; ölçek koşumu 5198'de yapılır.
// ─────────────────────────────────────────────────────────────────────────────
const MOD = __ENV.MOD || 'pos';
const JETON = __ENV.JETON || '';
const OLCEK = MOD === 'olcek';

if (OLCEK && !JETON) {
    throw new Error('MOD=olcek için JETON zorunludur (kiracı önceden tohumlanır).');
}

const okumaSure = new Trend('okuma_sure', true);
const satisSure = new Trend('satis_sure', true);
const oranSiniri = new Rate('oran_siniri_429');
// 429 BAŞARISIZLIK DEĞİLDİR — tasarlanmış davranıştır. Gerçek hata 5xx'tir.
const sunucuHatasi = new Rate('sunucu_hatasi_5xx');
// P-25 / R-6 — ölçek modunda rapor uçları AYRI ölçülür: POS okuması ile rapor
// okuması aynı torbaya konursa 50 bin ürünün etkisi ortalamada kaybolur.
const raporSure = new Trend('rapor_sure', true);

export const options = {
    scenarios: {
        pos: {
            executor: 'ramping-vus',
            startVUs: 1,
            stages: [
                { duration: '15s', target: KULLANICI },   // sabah açılışı
                { duration: '30s', target: KULLANICI },   // gün içi sabit yük
                { duration: '10s', target: 0 },           // kapanış
            ],
            gracefulRampDown: '5s',
        },
    },
    thresholds: {
        // Tezgâhta bekleyen müşteri varken 500 ms üstü fark edilir.
        // ⚠ ÖLÇEK MODUNDA GEVŞETİLİR VE BU BİR AYIP DEĞİL, BİR ÖLÇÜDÜR (md.1):
        //   50 bin ürünlü kiracıda rapor uçları boş kiracıdaki kadar hızlı
        //   DEĞİLDİR; eşiği 500 ms'te bırakıp "geçti" demek uydurma olurdu,
        //   eşiği kaldırmak da ölçmemek olurdu. Ölçek eşiği ayrı ve yazılıdır.
        'okuma_sure': [OLCEK ? 'p(95)<1500' : 'p(95)<500'],
        'satis_sure': ['p(95)<1000'],
        // Rapor uçları (günlük özet, stok yaşlandırma, stok değeri) — B-8'in
        // sunucuda toplama işi tam da bu sayıyı düşürmek içindir.
        'rapor_sure': [OLCEK ? 'p(95)<3000' : 'p(95)<3000'],
        // ⚠ `http_req_failed` KULLANILMIYOR: 429'u hata sayar, oysa oran sınırı
        // ürünün bilerek yaptığı iştir. Eşik gerçek sunucu hatası üzerinedir.
        'sunucu_hatasi_5xx': ['rate<0.001'],
    },
};

function json(yanit) { try { return yanit.json(); } catch { return {}; } }

/**
 * P-25 / R-6 — DOLU kiracının bir turu: ürün listesi (sayfalı), ürün araması
 * ve B-8'in sunucuda toplamaya çevirdiği rapor uçları. 200 ve 429 dışındaki
 * her kod kayda geçer; 5xx gerçek hatadır.
 */
function olcekTuru(basliklar) {
    const kabul = r => r.status === 200 || r.status === 429;

    const liste = http.get(`${API}/urunler?limit=50`, { headers: basliklar, tags: { ad: 'urun_listesi' } });
    olc(liste, okumaSure);
    check(liste, { 'ölçek: ürün listesi': kabul });

    const arama = http.get(`${API}/urunler?arama=P25-04&limit=50`, { headers: basliklar, tags: { ad: 'urun_arama' } });
    olc(arama, okumaSure);
    check(arama, { 'ölçek: ürün araması': kabul });

    const uclar = [
        ['gunluk_ozet',      `${API}/raporlar/gunluk`],
        ['aylik_ozet',       `${API}/raporlar/aylik`],
        ['stok_yaslandirma', `${API}/raporlar/stok-yaslandirma`],
        ['stok_degeri',      `${API}/raporlar/stok-degeri`],
        ['islem_defteri',    `${API}/raporlar/islem-defteri?limit=100&ofset=0`],
    ];
    const u = uclar[__ITER % uclar.length];
    const rapor = http.get(u[1], { headers: basliklar, tags: { ad: u[0] } });
    olc(rapor, raporSure);
    check(rapor, { [`ölçek: ${u[0]}`]: kabul });

    sleep(1);
}

/** Yanıtı ölç: süre, 429 oranı ve GERÇEK hata (5xx) ayrı ayrı. */
function olc(yanit, sure) {
    sure.add(yanit.timings.duration);
    oranSiniri.add(yanit.status === 429);
    sunucuHatasi.add(yanit.status >= 500);
    return yanit;
}

export function setup() {
    // P-25 / R-6 — ölçek modunda kiracı ZATEN VAR ve DOLU; k6 yeni kiracı
    // açmaz, verilen jetonla dolu kiracıyı ölçer.
    // ⚠ JETON VİRGÜLLE ÇOĞALTILIR: oran sınırı KULLANICI başınadır. Tek jetonla
    //   10 sanal kullanıcı koşturmak isteklerin %92'sini 429'a düşürüyordu ve
    //   ölçülen şey ürünün hızı değil, oran sınırının hızı oluyordu (ilk koşum,
    //   md.1 — gizlenmedi, rapora yazıldı). Gerçek dükkânda 10 personelin 10
    //   ayrı hesabı olur; test de öyle koşar.
    if (OLCEK) {
        const jetonlar = JETON.split(',').map(x => x.trim()).filter(Boolean);
        return { jetonlar, urunler: [], kasaId: null, kurId: null, subeId: null };
    }

    const damga = Date.now();
    const baslik = { 'Content-Type': 'application/json' };

    // 1) Kiracı kur (kayıt ucu IP başına 3/saat — yalnız BİR kez çağrılır)
    const kayit = http.post(`${API}/auth/kayit`, JSON.stringify({
        isletmeAdi: `Yük Testi ${damga}`, ad: 'Yuk', soyad: 'Test',
        email: `yuk-${damga}@test.local`, sifre: 'Gucluparola1!',
    }), { headers: baslik });

    if (kayit.status !== 201 && kayit.status !== 200) {
        throw new Error(`Kiracı kurulamadı: ${kayit.status} ${kayit.body}`);
    }
    const sahip = json(kayit).accessToken;
    const yetkili = { ...baslik, Authorization: `Bearer ${sahip}` };

    // 2) Şube + kasa + kur + ürün
    const subeler = json(http.get(`${API}/subeler`, { headers: yetkili }));
    const subeId = (subeler[0] || subeler.subeler?.[0] || {}).id;

    const kasa = json(http.post(`${API}/kasalar`, JSON.stringify({
        subeId, ad: 'Yük Kasa', paraBirimi: 'TRY',
    }), { headers: yetkili }));

    const kur = json(http.post(`${API}/kur/manuel`, JSON.stringify({
        altinHasFiyati: 6827.20, gramAltinFiyati: 6861.51,
    }), { headers: yetkili }));

    const urunler = [];
    for (let i = 0; i < 5; i++) {
        const u = json(http.post(`${API}/urunler`, JSON.stringify({
            tip: 'ZIYNET', kod: `YUK-${damga}-${i}`, ad: `Yük ürünü ${i}`,
            ayarId: 'K22', gramAgirlik: 5, iscilikTl: 500,
        }), { headers: yetkili }));
        if (u.id) {
            http.patch(`${API}/urunler/${u.id}/stok/${subeId}`,
                JSON.stringify({ adet: 100000 }), { headers: yetkili });
            urunler.push(u.id);
        }
    }

    // 3) Sanal kullanıcılar — her biri kendi oran-sınırı bölümünde
    const jetonlar = [sahip];
    for (let i = 1; i < KULLANICI; i++) {
        const eposta = `yuk-${damga}-${i}@test.local`;
        http.post(`${API}/kullanicilar`, JSON.stringify({
            ad: `Personel${i}`, soyad: 'Yük', email: eposta,
            sifre: 'Gucluparola1!', rol: 'MUDUR', subeId,
        }), { headers: yetkili });

        const giris = http.post(`${API}/auth/giris`, JSON.stringify({
            email: eposta, sifre: 'Gucluparola1!',
        }), { headers: { ...baslik, 'X-Giris-Eposta': eposta } });
        const t = json(giris).accessToken;
        if (t) jetonlar.push(t);
    }

    return { jetonlar, urunler, kasaId: kasa.id, kurId: kur.id, subeId };
}

export default function (veri) {
    const jeton = veri.jetonlar[(__VU - 1) % veri.jetonlar.length];
    const basliklar = { 'Content-Type': 'application/json', Authorization: `Bearer ${jeton}` };

    if (OLCEK) { olcekTuru(basliklar); return; }

    // ── Okuma: POS'un en sık yaptığı iş ──────────────────────────────────
    const liste = http.get(`${API}/urunler?limit=50`, { headers: basliklar, tags: { ad: 'urun_listesi' } });
    olc(liste, okumaSure);
    check(liste, { 'ürün listesi 200': r => r.status === 200 || r.status === 429 });

    const rapor = http.get(`${API}/raporlar/gunluk`, { headers: basliklar, tags: { ad: 'gunluk_rapor' } });
    olc(rapor, okumaSure);

    // ── Yazma: her 5 turda bir satış (gerçek tezgâh oranı) ───────────────
    if (__ITER % 5 === 0 && veri.urunler.length > 0) {
        const urunId = veri.urunler[__ITER % veri.urunler.length];
        const satis = http.post(`${API}/islemler/satis`, JSON.stringify({
            urunId, adet: 1, birimFiyatTl: 5000,
            odemeTuru: 'NAKIT', kasaId: veri.kasaId, kurSnapshotId: veri.kurId,
        }), { headers: basliklar, tags: { ad: 'satis' } });
        olc(satis, satisSure);
        check(satis, { 'satış kabul': r => r.status < 500 });
    }

    sleep(1);
}
