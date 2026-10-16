export const meta = {
  name: 'kuyumcu-piyasa-arastirmasi-2026-09',
  description: 'Kuyumcu ERP icin piyasa/rakip/mevzuat/arayuz/teknoloji arastirmasi (5 paralel Opus arastirmaci + sentez): daha iyi ne yapabiliriz',
  phases: [
    { title: 'Arastirma', detail: '5 paralel arastirmaci: rakipler, kuyumcunun aci noktalari, mevzuat 2026, arayuz trendleri, entegrasyon/teknoloji', model: 'opus' },
    { title: 'Sentez', detail: 'Onceliklendirilmis oneri listesi + kurucuya sabah ozeti', model: 'opus' },
  ],
}
const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery'
const BULGU = {
  type: 'object',
  properties: {
    konu: { type: 'string' },
    bulgular: { type: 'array', items: { type: 'object', properties: {
      baslik: { type: 'string' }, detay: { type: 'string' }, kaynak_url: { type: 'string' }, kaynak_tarih: { type: 'string' }, guven: { type: 'string', description: 'YUKSEK | ORTA | DUSUK' } },
      required: ['baslik', 'detay', 'kaynak_url', 'guven'] } },
    firsatlar: { type: 'array', items: { type: 'object', properties: {
      oneri: { type: 'string' }, gerekce: { type: 'string' }, etki: { type: 'string', description: 'YUKSEK | ORTA | DUSUK' }, caba: { type: 'string', description: 'gun tahmini' }, bizde_var_mi: { type: 'string' } },
      required: ['oneri', 'gerekce', 'etki', 'caba', 'bizde_var_mi'] } },
    acik_sorular: { type: 'array', items: { type: 'string' } },
    kaynak_sayisi: { type: 'integer' },
  },
  required: ['konu', 'bulgular', 'firsatlar', 'acik_sorular', 'kaynak_sayisi'],
}
const ORTAK = `
KURALLAR: Türkçe yaz. Web araması ve sayfa okuma (WebSearch/WebFetch) KULLAN; her bulguya URL + tarih + güven düzeyi ekle. UYDURMA YOK: doğrulayamadığın rakamı 'DUSUK güven' işaretle ya da yazma. Rakip fiyatları/özellikleri sitesinden/demo sayfasından doğrula; 'söylenti' ise öyle de. Mevzuatta yalnız birincil kaynak (Resmî Gazete, GİB, MASAK, Ticaret Bakanlığı, TCMB) 'YUKSEK'; haber/blog 'ORTA'; forum 'DUSUK'.
BİZİM ÜRÜN (karşılaştırma tabanı): ${KOK}/SONRAKI-OTURUM-PROMPT.md §1-§3 ve A5–A17 (yapılanlar), ${KOK}/PARITE-IS-LISTESI-MUHURLU-2026-09-14.md (48 paket; Katman 1 imalat kapsam dışı), ${KOK}/PAZAR-ARASTIRMASI-2026-09-13.md (önceki araştırma — TEKRARLAMA, üzerine koy), ${KOK}/dokumanlar/02-mimari/kalan-paketler/ (UX-1/2/3 tasarımları). Müşteri: PERAKENDE kuyumcu dükkânı (tek dükkân + 2–3 şube), Türkiye. Yığın: .NET 8 + Blazor WASM + MySQL, saf CSS, npm yok.
Proje dosyalarını DEĞİŞTİRME (salt okunur). Sır yazma. Derleme/test koşma.
Çıktın orkestratör için ham veridir: somut, kaynaklı, önceliklendirilebilir.
`
phase('Arastirma')
const KONULAR = [
  { key: 'rakipler', prompt: 'RAKİPLER: Türkiye kuyumcu yazılımları — M&L Yazılım (Kuyumcu Programı), CetaSoft Kuyum, Kuyumcum, Gramino, Dezy, KuyumPOS, Altın Yazılım, Jewelmaster/JewelERP, Sarraf programları, Logo/Mikro/Nebim kuyum dikeyleri, yurt dışı (Jewel360, JMS Jewelry Management, Lightspeed Jewelry). Her biri için: özellik listesi (has-cari, etiket/barkod, KMTS, e-fatura, mobil, bulut, POS, sadakat, e-ticaret entegrasyonu, WhatsApp), fiyat modeli (aylık/yıllık/lisans), demo var mı, müşteri şikâyetleri (Şikayetvar, Google yorumları, forumlar), 2025–2026 yenilikleri. Sonuç: özellik matrisi + "bizde eksik olan, rakiplerin çoğunda olan" listesi.' },
  { key: 'aci_noktalari', prompt: 'KUYUMCUNUN GÜNLÜK ACI NOKTALARI: esnaf/kuyumcu forumları, Ekşi Sözlük, YouTube (kuyumcu programı tanıtım/eğitim videoları ve yorumları), kuyumcu odaları/dernek siteleri (İKO, Kuyumcukent, TÜRKÇEKAM), Google Play/App Store kuyumcu uygulama yorumları, "kuyumcu programı tavsiye" aramaları. Neyi en çok istiyorlar: has-cari mutabakatı, etiket basımı, sayım, çırak/tezgâhtar yetkisi, telefondan bakiye görme, müşteriye WhatsApp ekstre, altın fiyatı otomatik güncelleme, kâr hesabı, fire/ramat (imalat — kapsam dışı ama not et), taksit/veresiye takibi, emanet, vitrin panosu. Sonuç: en çok tekrar eden 15 istek, her biri için "bizde var / kısmen / yok" ve kaynak.' },
  { key: 'mevzuat', prompt: 'MEVZUAT 2026 (yalnız birincil kaynaklardan doğrula): (1) MASAK — kıymetli maden/taş ticareti yükümlüleri için 2025–2026 değişiklikleri (kimlik tespiti eşiği, şüpheli işlem, MASAK Online, uyum görevlisi, cezalar); (2) Kıymetli Maden ve Taş Ticareti: 29 Nisan 2026 yetki belgesi/yönetmelik, KMTS karekod perakende takvimi; (3) GİB: e-Fatura/e-Arşiv 2026 hadleri, 7 günlük fatura süresi, tevsik zorunluluğu (30.000 TL) ve 2026 güncellemesi, kuyum KDV özel matrah (23/e, 805 kodu), gider pusulası/hurda; (4) Fiyat Etiketi Yönetmeliği kuyum hükümleri; (5) TCMB/Hazine döviz mevzuatı (32 sayılı karar) kuyumcuya etkisi; (6) e-Defter/e-İrsaliye 2026. Sonuç: her konu için hüküm, tarih, kaynak URL, güven, bize etkisi (kodda ne değişmeli), ve devir belgesindeki mevcut kabullerle (A7 araştırması) çelişki var mı.' },
  { key: 'arayuz', prompt: 'ARAYÜZ VE DENEYİM TRENDLERİ 2025–2026 (modern, sade, şık): perakende POS/ERP arayüz örnekleri (Shopify POS, Square, Lightspeed Retail, Toast, Zoho Inventory, Odoo 17/18 POS, Lightspeed Jewelry), tasarım sistemleri (Material 3, Fluent 2, Radix/shadcn estetiği, Apple HIG), tipografi/renk/boşluk ilkeleri, karanlık tema, veri yoğun tablolar için desenler, mobil-first POS, erişilebilirlik (WCAG 2.2), "basit mod / uzman mod" örnekleri (progressive disclosure), onboarding/boş durum tasarımı, altın/kuyum sektörüne özgü görsel dil örnekleri. Her öneriyi Blazor WASM + saf CSS (npm yok, CSP sıkı: dış font/CDN yok) ile uygulanabilir somut adımlara çevir: belirteç değerleri (renk hex, aralık, yarıçap), bileşen listesi, sayfa düzeni önerileri (Panel, Satış, Cariler, Ürünler, Giriş). Kaynaklarla.' },
  { key: 'teknoloji', prompt: 'ENTEGRASYON VE TEKNOLOJİ FIRSATLARI: (1) altın/döviz kur kaynakları (Truncgil, Harem Altın, Kapalıçarşı canlı, BİST kıymetli madenler, TCMB) — API/lisans/ücret/güvenilirlik; (2) e-Fatura özel entegratörleri (Foriba/Sovos, Uyumsoft, Logo e-Fatura, Mikro, EDM, İzibiz, Digital Planet) — API biçimi, ücret, kuyum sektörü deneyimi; (3) WhatsApp Business API ile ekstre/fiş gönderimi — sağlayıcılar ve maliyet; (4) banka POS/sanal POS ve banka ekstresi (açık bankacılık, MT940/CSV) entegrasyonu; (5) donanım: barkod/etiket yazıcı (Zebra ZPL, TSC), terazi entegrasyonu, RFID sayım, karekod okuyucu; (6) AI destekli özellikler: fotoğraftan ürün kartı/etiket, sesli satış, anomali/şüpheli işlem tespiti, mağaza asistanı (Claude API); (7) PWA/mobil ve çevrimdışı; (8) çok kiracılı bulut barındırma maliyeti (Türkiye veri yerleşimi, KVKK). Her madde: fizibilite, maliyet, bize etkisi, önerilen paket.' },
]
const bulgular = (await parallel(KONULAR.map(k => () => agent(`${ORTAK}\nGÖREV — ${k.prompt}`, { label: `arastir:${k.key}`, phase: 'Arastirma', model: 'opus', effort: 'high', schema: BULGU })
  .then(r => ({ key: k.key, sonuc: r }))))).filter(Boolean)
log(`Araştırma bitti: ${bulgular.length}/5; kaynak ${bulgular.reduce((a, b) => a + (b.sonuc?.kaynak_sayisi || 0), 0)}`)

phase('Sentez')
const SENTEZ = {
  type: 'object',
  properties: {
    ozet: { type: 'string' },
    kurucuya_sabah_ozeti: { type: 'string', description: '10 cumle: en onemli 5 firsat + 3 risk' },
    oncelikli_oneriler: { type: 'array', items: { type: 'object', properties: {
      sira: { type: 'integer' }, oneri: { type: 'string' }, gerekce: { type: 'string' }, etki: { type: 'string' }, caba: { type: 'string' }, ilgili_paket: { type: 'string', description: 'mevcut paket no ya da YENI' }, kaynaklar: { type: 'array', items: { type: 'string' } } },
      required: ['sira', 'oneri', 'gerekce', 'etki', 'caba', 'ilgili_paket', 'kaynaklar'] } },
    rakip_matrisi: { type: 'array', items: { type: 'object', properties: { urun: { type: 'string' }, guclu: { type: 'string' }, zayif: { type: 'string' }, fiyat: { type: 'string' }, bizde_eksik: { type: 'string' }, kaynak: { type: 'string' } }, required: ['urun', 'guclu', 'zayif', 'fiyat', 'bizde_eksik', 'kaynak'] } },
    mevzuat_2026: { type: 'array', items: { type: 'object', properties: { konu: { type: 'string' }, hukum: { type: 'string' }, kaynak: { type: 'string' }, guven: { type: 'string' }, bize_etkisi: { type: 'string' }, mevcut_kabulle_celisiyor_mu: { type: 'string' } }, required: ['konu', 'hukum', 'kaynak', 'guven', 'bize_etkisi'] } },
    arayuz_onerileri: { type: 'array', items: { type: 'object', properties: { oneri: { type: 'string' }, neden: { type: 'string' }, nasil: { type: 'string', description: 'Blazor + saf CSS ile somut adim' }, sayfa: { type: 'string' } }, required: ['oneri', 'neden', 'nasil'] } },
    yeni_paket_onerileri: { type: 'array', items: { type: 'object', properties: { ad: { type: 'string' }, kapsam: { type: 'string' }, gun: { type: 'string' }, gerekce: { type: 'string' } }, required: ['ad', 'kapsam', 'gun', 'gerekce'] } },
    riskler: { type: 'array', items: { type: 'string' } },
  },
  required: ['ozet', 'kurucuya_sabah_ozeti', 'oncelikli_oneriler', 'rakip_matrisi', 'mevzuat_2026', 'arayuz_onerileri', 'yeni_paket_onerileri', 'riskler'],
}
const sentez = await agent(`${ORTAK}
GÖREV — SENTEZ (Eray satış/pazarlama + Ramazan strateji + Bertan mimari birlikte). 5 araştırmacının bulgularını birleştir: "Daha iyi ne yapabiliriz?" sorusuna ETKİ/ÇABA sıralı, kaynaklı, bizim paket listemize (48 + UX-1/2/3) eşlenmiş öneri listesi; rakip matrisi; mevzuat 2026 tablosu (mevcut kabullerle çelişki varsa açıkça); arayüz önerileri (Blazor + saf CSS ile uygulanabilir); yeni paket önerileri; riskler; kurucuya 10 cümlelik sabah özeti. Rakamları kaynağa bağla; çelişen kaynaklarda ikisini de yaz.
BULGULAR (JSON):
${JSON.stringify(bulgular, null, 1)}
`, { label: 'sentez', phase: 'Sentez', model: 'opus', effort: 'high', schema: SENTEZ })
return { bulgular, sentez }