export const meta = { name: 'kuyumcu-oy-tamamla', description: 'Oylanmamis/kesik oy almis paketler icin tam metinli yeniden oylama: paket basina 3 mercek (belgeyi diskten okur) + muhur (karar satirlari) — salt okunur', phases: [{ title: 'Oylama' }, { title: 'Mühür', model: 'opus' }] }
const PAKETLER = args.paketler   // [{no, belge, ad}]
const DALGA_PLANI = args.dalga_plani || ''
const OY = { type: 'object', properties: { mercek: { type: 'string' }, oylar: { type: 'array', items: { type: 'object', properties: { paket_no: { type: 'string' }, oy: { type: 'string' }, kosullar: { type: 'array', items: { type: 'string' } }, gerekce: { type: 'string' }, kanit: { type: 'string' } }, required: ['paket_no', 'oy', 'kosullar', 'gerekce'] } }, capraz_bulgular: { type: 'array', items: { type: 'string' } }, veto: { type: 'array', items: { type: 'object', properties: { paket_no: { type: 'string' }, neden: { type: 'string' } }, required: ['paket_no', 'neden'] } } }, required: ['mercek', 'oylar', 'capraz_bulgular', 'veto'] }
const MERCEKLER = [
  { ad: 'Bertan', model: 'opus', rol: 'BAŞ MİMAR merceği: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu, önkoşul sırası, paket boyutu, dosya çakışması (tek-el kuralı dosya bazında), ADR uyumu. Belgedeki dosya:satır kanıtlarından en az 3 tanesini kaynakta kendin doğrula.' },
  { ad: 'Mehmet', model: 'sonnet', rol: 'GÜVENLİK merceği (veto yetkin var): kiracı izolasyonu, rol/yetki, fail-closed kapılar, sır yönetimi, yedek şifreleme, dış entegrasyon (entegratör kimliği, mali mühür, UBL), telemetri/hata paketinde kişisel veri, MFA kapısı (45-API tamamlanmadan üretim yayını yok), KVKK. Mevzuat/güvenlik kapısını atlayan her akışa VETO.' },
  { ad: 'Aşkın', model: 'sonnet', rol: 'QA merceği (fail-closed kalite kapısı): testler kuralı gerçekten sınıyor mu (mutasyon kanıtı test adıyla), DB/HTTP/bUnit dengesi, kararsızlık riski, canlı doğrulama ölçülebilir mi (HTTP kodu + SQL), kurulum/yedek/e-belge paketlerinde kanıt nasıl üretilir (gerçek prova), test kiracısı temizliği.' },
]
phase('Oylama')
const oylar = []
for (const p of PAKETLER) {
  const s = await parallel(MERCEKLER.map(m => () => agent(`Türkçe yaz. Salt okunur (dosya değiştirme/derleme/test YOK; ≤ 12 araç çağrısı). SEN: ${m.ad} — ${m.rol}
GÖREV — YENİDEN OYLAMA (önceki turda bu paketin tasarım metni merceklere KESİK ulaştı; şimdi tam metni oku): PAKET ${p.no} — ${p.ad}. Belgeyi OKU: ${p.belge} (tamamı: hedef, kodda bugünkü durum, kapsam, veri modeli, uçlar, servis ve dosyalar, ekranlar, iş kuralları, testler, mutasyon kanıtları, canlı doğrulama, riskler, açık sorular). Dalga planı bağlamı: ${DALGA_PLANI.slice(0, 900)}
Oyla: EVET / HAYIR / KOSULLU; koşullar ≤ 8 (her biri ≤ 260 karakter, dosya:satır kanıtıyla), gerekçe, kanıt; çapraz bulgular (diğer paketlerle çelişki: T-1/T-3 dizin tekliği, Program.cs tek-el, sır disiplini); veto (yalnız Mehmet bağlayıcı). paket_no alanına aynen "${p.no}" yaz. ÇIKTI SINIRI ≤ 1.500 kelime; StructuredOutput tek seferde.`, { label: `oy:${m.ad}:${p.no}`, phase: 'Oylama', model: m.model, effort: 'medium', schema: OY })))
  for (const o of s.filter(Boolean)) { const v = oylar.find(x => x.mercek === o.mercek); if (v) { v.oylar.push(...o.oylar); v.capraz_bulgular.push(...o.capraz_bulgular); v.veto.push(...o.veto) } else oylar.push(o) }
  log(`oylandı: ${p.no}`)
}
phase('Mühür')
const KARAR = { type: 'object', properties: { karar_tablosu: { type: 'array', items: { type: 'object', properties: { paket_no: { type: 'string' }, ad: { type: 'string' }, sonuc: { type: 'string' }, birlesik_kosullar: { type: 'array', items: { type: 'string' } }, dalga: { type: 'integer' }, tahmin_gun: { type: 'string' }, onkosullar: { type: 'array', items: { type: 'string' } } }, required: ['paket_no', 'ad', 'sonuc', 'birlesik_kosullar', 'dalga', 'tahmin_gun', 'onkosullar'] } }, ozet: { type: 'string' } }, required: ['karar_tablosu', 'ozet'] }
const muhur = await agent(`Türkçe yaz. Araç çağrısı gerekmez (≤ 3). Tamer Bey adına MÜHÜR: ${PAKETLER.map(p => p.no).join(', ')} için karar tablosu — sonuc (Mehmet vetosu → RED ya da "KOSULLU KABUL (veto koşulu)"; 3 EVET → EVET; KOSULLU varsa "KOSULLU KABUL"), birlesik_kosullar (üç merceği birleştir, tekle, çelişkiyi çöz; ≤ 10 madde, ≤ 240 karakter; veto ilk madde "VETO:"), dalga (plan: ${DALGA_PLANI.slice(0, 700)}), tahmin_gun (kısa), onkosullar. ozet ≤ 600 karakter. ÇIKTI SINIRI ≤ 2.000 kelime; StructuredOutput tek seferde.
PAKETLER: ${JSON.stringify(PAKETLER.map(p => ({ no: p.no, ad: p.ad, dalga_onerisi: p.dalga_onerisi })))}
OYLAR: ${JSON.stringify(oylar).slice(0, 60000)}`, { label: 'muhur', phase: 'Mühür', model: 'opus', effort: 'medium', schema: KARAR })
return { oylar, karar_tablosu: muhur ? muhur.karar_tablosu : [], ozet: muhur ? muhur.ozet : '' }
