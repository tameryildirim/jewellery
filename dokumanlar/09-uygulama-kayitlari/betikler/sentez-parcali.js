export const meta = { name: 'kuyumcu-mimari-sentez-parcali', description: 'Parcali sentez: once dalga plani + kesisen kararlar (1 Opus), sonra paket gruplari icin karar tablosu (Opus, 3er parti) — tek ajanli sentez cikti sinirinda kesildigi icin', phases: [{ title: 'Dalga plani', model: 'opus' }, { title: 'Karar tablosu', model: 'opus' }] }
const GIRDI = args.girdi
const GRUPLAR = args.gruplar
const PARTI = args.parti || 3
const ORTAK = `Türkçe yaz. Salt okunur görev: dosya değiştirme, derleme, test, canlı çağrı YOK. Kaynak koda bakma — mimarlar ve üç mercek (Bertan mimari · Mehmet güvenlik/veto · Aşkın QA) dosya:satır kanıtıyla zaten baktı; sen yalnız SENTEZ yaparsın. Tek girdi dosyası (jq ile oku, Read ile tamamını AÇMA — büyük): ${GIRDI}
Yapısı: {paketler:[{no,ad,katman,onkosul_paketler,tahmin_gun,dosya_ayak_izi,fazlar:[{ad,ne_zaman}],riskler,acik_sorular}], oylar:[{mercek,oylar:[{paket_no,oy,kosullar,gerekce}],veto:[{paket_no,neden}],capraz_bulgular}], ortak_kararlar:[{katman,ortak_kararlar}], ux_uygulama_kurallari:[...]}. paket_no ve no dizgedir ("4", "UX-1").
Bağlam: paket 33 ve UX-2 Faz A1 BİTTİ (dalga 0). DALGA 1 şu an uygulanıyor ve SABİTTİR: 4-A → UX-1-A → UX-3-A → 1-A. Kurucu şikâyeti üzerine mühürlenen iki ek paket dalga 2'nin BAŞINA sabittir: UX-4 (mantık/tutarlılık düzeltmeleri, 14 madde: fiyat listesi varsayılan bayrağı, düzenle/pasife al, tahsilata kasa bacağı vb.) ve 49 (fiyat listesi ürün görünümü: fiyat_listesi_urun_fiyatlari tablosu, önizleme/toplu uygula). Katman 1 (paket 7–13) kapsam dışı. Şirket yasası yaz-seri: aynı dosyaya dokunan paketler aynı anda uygulanmaz; dalga içinde SERİ sıra; ortak dosyalar (Entities.cs, KuyumcuDbContext.cs, Sozlukler.cs, KenarCubugu.razor, UstBar.razor, Program.cs, ModulKayit.cs, app.css, denetim.sh) → paralel DEĞİL; migration üreten paketler kesinlikle seri. Mehmet'in vetoları bağlayıcıdır (47: seçenek 3 yasak → seçenek 2 kapılı/kuru provalı içe aktarım; 45: MFA challenge jetonu farklı audience + HTTP testi; 32: toplu onay 41'den sonra; 1↔38: tek OzelMatrahMotoru; 40: Faz B KVKK; 39: kabul ölçütü; 41: dekont tarihi vs sunucu saati). Öncelik: küçük altyapı (6-A, 5-A, 3-A) → günlük dükkân işi (19, 25, 27, 29, 31, 32, 20) → uyum (38 ÖNE: piyasa araştırması 2026 e-Arşiv/KDV 23-e otomatiğini ACİL diyor; 39, 40-A, 41, 42) → raporlar (34–37) → platform (44, 45, 46, 43, 47, 48); UX-1 B/C/D, UX-2 A2/B/C/D, UX-3 B/C fazları araya serpilir; önkoşul her zaman önce. ADIM EKONOMİSİ: en fazla 6 araç çağrısı; her jq çıktısını tek seferde al; düşünmeyi kısa tut ve çıktıyı tek parçada yaz.`
const PLAN = { type: 'object', properties: {
  ozet: { type: 'string' },
  dalgalar: { type: 'array', items: { type: 'object', properties: { no: { type: 'integer' }, paketler: { type: 'array', items: { type: 'string' } }, gerekce: { type: 'string' }, uygulama_sirasi: { type: 'array', items: { type: 'string' } }, paralel_guvenli_ciftler: { type: 'array', items: { type: 'string' } } }, required: ['no', 'paketler', 'gerekce', 'uygulama_sirasi'] } },
  cakisma_haritasi: { type: 'array', items: { type: 'object', properties: { dosya: { type: 'string' }, paketler: { type: 'array', items: { type: 'string' } } }, required: ['dosya', 'paketler'] } },
  kesisen_kararlar: { type: 'array', items: { type: 'object', properties: { baslik: { type: 'string' }, karar: { type: 'string' }, gerekce: { type: 'string' }, adr_onerisi: { type: 'string' } }, required: ['baslik', 'karar', 'gerekce'] } },
  kurucuya_kalanlar: { type: 'array', items: { type: 'string' } }, toplam_tahmin_gun: { type: 'string' }, riskler: { type: 'array', items: { type: 'string' } } },
  required: ['ozet', 'dalgalar', 'cakisma_haritasi', 'kesisen_kararlar', 'kurucuya_kalanlar', 'toplam_tahmin_gun', 'riskler'] }
const TABLO = { type: 'object', properties: { karar_tablosu: { type: 'array', items: { type: 'object', properties: { paket_no: { type: 'string' }, ad: { type: 'string' }, sonuc: { type: 'string' }, birlesik_kosullar: { type: 'array', items: { type: 'string' } }, dalga: { type: 'integer' }, tahmin_gun: { type: 'string' }, onkosullar: { type: 'array', items: { type: 'string' } } }, required: ['paket_no', 'ad', 'sonuc', 'birlesik_kosullar', 'dalga', 'tahmin_gun', 'onkosullar'] } } }, required: ['karar_tablosu'] }

phase('Dalga planı')
const plan = await agent(`${ORTAK}
Önerilen jq komutları (sırayla, hepsi tek satır): (1) jq -c '.paketler[] | {no,ad,katman,onkosul_paketler,tahmin_gun,dosya_ayak_izi,fazlar}' ${GIRDI}  (2) jq -c '.oylar[] | {mercek, veto, oylar: [.oylar[] | {paket_no,oy}]}' ${GIRDI}  (3) jq -c '.ortak_kararlar' ${GIRDI}  (4) jq -c '[.oylar[] | .capraz_bulgular]' ${GIRDI}  (5) yalnız gerekirse: jq -c '.paketler[] | {no, riskler, acik_sorular}' ${GIRDI}
GÖREV — DALGA PLANI (Tamer Bey mührü için): (1) dalgalar: 2'den başla (dalga 2 = UX-4 → 49 → …); önkoşul grafiği + DOSYA ÇAKIŞMASI + öncelik sırasına göre; her dalga için paketler (paket-faz düzeyinde yazılabilir: "38-A", "UX-1-B"; bir paketin Faz B/C'si sonraki dalgada olabilir), uygulama_sirasi (SERİ), gerekce (≤ 400 karakter), paralel_guvenli_ciftler (yalnız kesinlikle ayrık dosya ayak izli çiftler; yoksa boş). (2) cakisma_haritasi: yalnız 2+ paketin dokunduğu dosyalar (dosya_ayak_izi kesişimi), en fazla 40 satır. (3) kesisen_kararlar: paketler arası çelişki/ortak mekanizma kararları (+ADR önerisi numarası 0016'dan başlayarak; adr_onerisi ≤ 120 karakter), en fazla 12; 47 için ADR-0014 çelişkisini oylara göre karara bağla (veto: seçenek 2). (4) kurucuya_kalanlar: yalnız imza/para/dış taahhüt (e-Fatura entegratör sözleşmesi, sertifika, KVKK aydınlatma metni onayı gibi), ≤ 10. (5) toplam_tahmin_gun (dalga 2+ toplamı, aralık). (6) riskler ≤ 12 (her biri ≤ 200 karakter). (7) ozet ≤ 1200 karakter.
ÇIKTI SINIRI (bağlayıcı): toplam yanıt 5.000 kelimeyi geçmesin; uzun gerekçe yazma; StructuredOutput'u tek seferde, kesintisiz ver.`, { label: 'dalga-plani', phase: 'Dalga planı', model: 'opus', effort: 'medium', schema: PLAN })
if (!plan) throw new Error('dalga planı üretilemedi')
log(`dalga planı hazır: ${plan.dalgalar.length} dalga, ${plan.cakisma_haritasi.length} çakışan dosya, ${plan.kesisen_kararlar.length} kesişen karar`)

phase('Karar tablosu')
const dalgaOzeti = JSON.stringify(plan.dalgalar.map(d => ({ no: d.no, paketler: d.paketler, sira: d.uygulama_sirasi })))
const tablolar = []
for (let i = 0; i < GRUPLAR.length; i += PARTI) {
  const parti = GRUPLAR.slice(i, i + PARTI)
  const sonuc = await parallel(parti.map(g => () => {
    const jqListe = g.map(n => JSON.stringify(n)).join(',')
    return agent(`${ORTAK}
Önerilen jq komutları (2 çağrı yeter): (1) jq -c '.paketler[] | select(.no | IN(${jqListe}))' ${GIRDI}  (2) jq -c '.oylar[] | {mercek, veto: [.veto[] | select(.paket_no | IN(${jqListe}))], oylar: [.oylar[] | select(.paket_no | IN(${jqListe}))]}' ${GIRDI}
DALGA PLANI (Tamer Bey mühürledi; buna uy): ${dalgaOzeti}
GÖREV — KARAR TABLOSU yalnız şu paketler için: ${g.join(', ')} (hepsi için birer satır; başka paket yazma). Her satır: paket_no (dizge, aynen), ad, sonuc (kural: Mehmet vetosu → "RED" ya da veto koşula bağlanmışsa "KOSULLU KABUL (veto koşulu)"; üç EVET → "EVET"; en az bir KOSULLU → "KOSULLU KABUL"; HAYIR varsa gerekçesiyle KOSULLU/RED), birlesik_kosullar (üç merceğin koşullarını BİRLEŞTİR: aynı olanları tekle, çelişenleri çöz ve çözümü yaz, dosya:satır kanıtlarını koru; en fazla 10 madde, her madde ≤ 240 karakter; vetoyu ilk madde yap ve "VETO:" ile başlat), dalga (dalga planındaki atama; paket-faz olarak verilmişse Faz A'nın dalgası; planda yoksa önkoşullarına göre en erken uygun dalga), tahmin_gun (mimarın tahmini, kısaltılmış), onkosullar (paket numaraları dizge).
ÇIKTI SINIRI (bağlayıcı): toplam yanıt 3.000 kelimeyi geçmesin; StructuredOutput'u tek seferde, kesintisiz ver.`, { label: 'tablo:' + g.join('-'), phase: 'Karar tablosu', model: 'opus', effort: 'medium', schema: TABLO })
  }))
  tablolar.push(...sonuc.filter(Boolean))
  log(`karar tablosu: ${tablolar.length}/${GRUPLAR.length} grup tamam`)
}
const karar_tablosu = tablolar.flatMap(t => t.karar_tablosu)
const eksik = GRUPLAR.flat().filter(n => !karar_tablosu.some(k => k.paket_no === n))
if (eksik.length) log(`UYARI: karar tablosunda eksik paketler: ${eksik.join(', ')}`)
return { sentez: Object.assign({}, plan, { karar_tablosu }), eksik }
