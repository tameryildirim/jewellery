export const meta = { name: 'kuyumcu-brif-oylama', description: 'Bir tasarim/karar belgesinin 5 mercekle (Bertan, Ertug, Mehmet, Askin, Eray) oylanmasi ve Tamer Bey muhru (salt okunur)', phases: [{ title: 'Oylama' }, { title: 'Mühür', model: 'opus' }] }
const BELGE = args.belge
const KONU = args.konu
const SORULAR = args.sorular || []
const EK = args.ek_belgeler || []
const OY = { type: 'object', properties: { mercek: { type: 'string' }, oy: { type: 'string' }, kosullar: { type: 'array', items: { type: 'string' } }, cevaplar: { type: 'array', items: { type: 'string' } }, gerekce: { type: 'string' }, veto: { type: 'array', items: { type: 'string' } } }, required: ['mercek', 'oy', 'kosullar', 'cevaplar', 'gerekce', 'veto'] }
const ORTAK = `Türkçe yaz. Salt okunur: dosya değiştirme/derleme/test YOK (grep/sed -n ile kaynağa bakmak serbest, ≤ 10 araç çağrısı). OKU: ${BELGE}${EK.length ? ' ve ' + EK.join(', ') : ''}. Her iddianı dosya:satır ya da belge bölümüyle kanıtla; emin değilsen 'DOĞRULANMALI' de. ÇIKTI SINIRI: ≤ 1.800 kelime; en fazla 8 koşul, her dizge ≤ 400 karakter; StructuredOutput tek seferde.
KONU: ${KONU}
SORULAR (her birine kısa cevap): ${SORULAR.map((s, i) => `(${i + 1}) ${s}`).join(' ')}
Oy: EVET / HAYIR / KOSULLU; veto yalnız Mehmet'te bağlayıcıdır, diğerleri 'itiraz' yazar.`
phase('Oylama')
const MERCEKLER = [
  { ad: 'Bertan — 02 Baş Mimar (mevcut mekanizmanın yeniden kullanımı, çift mekanizma açılmaması, DOM/id sözleşmesi, çakışma, sevk edilebilirlik)', model: 'opus' },
  { ad: 'Ertuğ — 03 Frontend (Blazor/CSS uygulanabilirlik, CSP, bileşen tasarımı, görsel kalite, erişilebilirlik, performans)', model: 'opus' },
  { ad: 'Mehmet — 06 Güvenlik (veto): CSP/inline style/JS-eval, XSS, yetki kapılarının arayüzde gevşememesi, parasal kayıtta iyimserlik yasağı, denetim izi', model: 'sonnet' },
  { ad: 'Aşkın — 04 QA: bUnit id sözleşmesi, ölçülebilir kabul ölçütleri, görsel regresyon, kararsızlık, test yükü', model: 'sonnet' },
  { ad: 'Eray — 08 Satış: kuyumcu gözüyle "en güzel ve en kolay" mı, demo etkisi, rakip kıyası (Dezy/Nebim/KuyumHub), tezgâh gerçekleri', model: 'opus' },
]
const oylar = (await parallel(MERCEKLER.map(m => () => agent(`${ORTAK}\nSEN: ${m.ad}. Yalnız kendi merceğinden; mercek alanına adını yaz.`, { label: 'oy:' + m.ad.split(' ')[0], phase: 'Oylama', model: m.model, effort: 'medium', schema: OY })))).filter(Boolean)
log(`oylar: ${oylar.map(o => o.mercek.split(' ')[0] + '=' + o.oy).join(', ')}`)
phase('Mühür')
const MUHUR = { type: 'object', properties: { karar: { type: 'string' }, birlesik_kosullar: { type: 'array', items: { type: 'string' } }, cevaplar: { type: 'array', items: { type: 'string' } }, celiskiler_ve_cozum: { type: 'array', items: { type: 'string' } }, ozet: { type: 'string' } }, required: ['karar', 'birlesik_kosullar', 'cevaplar', 'celiskiler_ve_cozum', 'ozet'] }
const muhur = await agent(`Türkçe yaz. Araç çağrısı gerekmez (≤ 3). Tamer Bey adına MÜHÜR: 5 oyu birleştir (Mehmet vetosu bağlayıcı), koşulları tekilleştir ve çelişkileri çözerek yaz, sorulara nihai cevap ver. Karar: EVET / KOŞULLU KABUL / RED. ÇIKTI SINIRI: ≤ 1.800 kelime; en fazla 14 birleşik koşul; her dizge ≤ 450 karakter; StructuredOutput tek seferde.
KONU: ${KONU}
SORULAR: ${SORULAR.join(' | ')}
OYLAR: ${JSON.stringify(oylar).slice(0, 40000)}`, { label: 'muhur', phase: 'Mühür', model: 'opus', effort: 'medium', schema: MUHUR })
return { oylar, muhur }
