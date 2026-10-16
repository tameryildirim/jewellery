export const meta = { name: 'kuyumcu-dalga-yeniden-oylama', description: 'Kurucunun yeni onceliklerine (her veri girisi duzenlenebilir, en guzel arayuz, satisa hazirlik) gore dalga sirasinin yeniden oylanmasi: 4 mercek + muhur (salt okunur)', phases: [{ title: 'Oylama' }, { title: 'Mühür', model: 'opus' }] }
const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery'
const ANA = `${KOK}/dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md`
const DURUM = `${KOK}/dokumanlar/09-uygulama-kayitlari/ORKESTRATOR-DURUM.md`
const ONERI = args.oneri
const OY = { type: 'object', properties: { mercek: { type: 'string' }, oy: { type: 'string' }, onerilen_dalgalar: { type: 'array', items: { type: 'object', properties: { no: { type: 'string' }, sira: { type: 'array', items: { type: 'string' } }, gerekce: { type: 'string' } }, required: ['no', 'sira', 'gerekce'] } }, kosullar: { type: 'array', items: { type: 'string' } }, gerekce: { type: 'string' }, veto: { type: 'array', items: { type: 'string' } } }, required: ['mercek', 'oy', 'onerilen_dalgalar', 'kosullar', 'gerekce', 'veto'] }
const ORTAK = `Türkçe yaz. Salt okunur: dosya değiştirme/derleme/test YOK. OKU (yalnız bu bölümler, kısa): (1) ${ANA} → "## Uygulama dalgaları" ve "## Dosya çakışma haritası" bölümleri (sed -n ile), (2) ${DURUM} → "## Kurucunun bağlayıcı talimatları" (madde 1-11) ve son 10 satır. ADIM EKONOMİSİ: ≤ 8 araç çağrısı. ÇIKTI SINIRI: ≤ 2.000 kelime; onerilen_dalgalar en fazla 8 dalga; her dizge ≤ 400 karakter; StructuredOutput tek seferde.
KURUCUNUN YENİ TALİMATI (2026-09-16 11:50, bağlayıcı): "Satışa hazır diyene kadar devam; ekip acımasızca değerlendirsin; her sayfada veri girişi olan her yer DÜZENLENEBİLİR olmalı; kullanıcı dostu ekranlar; piyasadaki en güzel arayüz." Mühürlü sentez dalga planı: dalga 2 = UX-4, 49, 38-A, 6-A, 3-A · 3 = 19, 25, 20, 27, UX-1-B, UX-2-A2 · 4 = 29, 31, 41, 32, UX-3-B · 5 = 42-A, 5-A, 39-A, 40-A, UX-2-C · 6 = 34-A, 35-A, 37, 36, UX-1-C · 7 = 45, 46, 44, 47-A, 48-A · 8 = Faz B/C kuyruğu, UX-3-C, UX-2-B, 43, UX-2-D. Yaz-seri yasası (dalga içinde seri) ve dosya çakışma haritası aynen geçerli.
TAMER BEY'İN ÖNERİSİ (oylanan): ${ONERI}
Oyun: EVET / HAYIR / KOSULLU; kendi önerdiğin dalga sırasını (2a, 2b, 2c, 3…) ve koşullarını yaz. ${args.sorular || "Sorular: (a) UX-1-B/C ve UX-2-A2/B öne alınmalı mı? (b) UX-2-B tek dalga mı serpme mi? (c) 38-A ve 41 arayüzden önce mi? (d) Satışa hazırlık asgarisi?"}`
phase('Oylama')
const MERCEKLER = [
  { ad: 'Bertan — 02 Baş Mimar (çakışma, sevk edilebilirlik, yeniden iş maliyeti)', model: 'opus' },
  { ad: 'Mehmet — 06 Güvenlik/uyum (veto yetkili: mevzuat kapıları, MASAK/KDV takvimi, düzenleme uçlarında yetki+denetim izi)', model: 'sonnet' },
  { ad: 'Aşkın — 04 QA (test/kanıt yükü, tarayıcı turu, regresyon yüzeyi, kararsızlık)', model: 'sonnet' },
  { ad: 'Eray — 08 Satış (bir kuyumcu neyi görünce satın alır: arayüz, hız, düzenleme rahatlığı, uyum güvencesi; satışa hazırlık asgarisi)', model: 'opus' },
]
const oylar = (await parallel(MERCEKLER.map(m => () => agent(`${ORTAK}\nSEN: ${m.ad}. Yalnız kendi merceğinden oy ver; mercek alanına adını yaz.`, { label: 'oy:' + m.ad.split(' ')[0], phase: 'Oylama', model: m.model, effort: 'medium', schema: OY })))).filter(Boolean)
log(`oylar: ${oylar.map(o => o.mercek.split(' ')[0] + '=' + o.oy).join(', ')}`)
phase('Mühür')
const MUHUR = { type: 'object', properties: { karar: { type: 'string' }, dalgalar: { type: 'array', items: { type: 'object', properties: { no: { type: 'string' }, sira: { type: 'array', items: { type: 'string' } }, gerekce: { type: 'string' } }, required: ['no', 'sira', 'gerekce'] } }, satisa_hazirlik_asgarisi: { type: 'array', items: { type: 'string' } }, kosullar: { type: 'array', items: { type: 'string' } }, celiskiler_ve_cozum: { type: 'array', items: { type: 'string' } }, ozet: { type: 'string' } }, required: ['karar', 'dalgalar', 'satisa_hazirlik_asgarisi', 'kosullar', 'celiskiler_ve_cozum', 'ozet'] }
const muhur = await agent(`Türkçe yaz. Salt okunur, araç çağrısı GEREKMEZ (gerekirse ≤ 3). Tamer Bey adına MÜHÜR: aşağıdaki 4 oyu birleştir; Mehmet'in vetosu bağlayıcı; çelişkileri çöz ve çözümü yaz; nihai dalga sırasını (2a, 2b, 2c, 3, 4, …; dalga içi SERİ; UX fazları paket-faz düzeyinde) ve satışa hazırlık ASGARİ paket kümesini ver. ÇIKTI SINIRI: ≤ 2.000 kelime, her dizge ≤ 400 karakter, StructuredOutput tek seferde.
KURUCU TALİMATI: her veri girişi düzenlenebilir · en güzel arayüz · satışa hazır diyene kadar devam. ÖNERİ: ${ONERI}
OYLAR: ${JSON.stringify(oylar).slice(0, 40000)}`, { label: 'muhur', phase: 'Mühür', model: 'opus', effort: 'medium', schema: MUHUR })
return { oylar, muhur }
