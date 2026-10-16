export const meta = { name: 'kuyumcu-yeni-paket-tasarim', description: 'Kurul/QA sonucu dogan YENI paketler (T/F/S/PWA/E-BELGE...) icin paket basina Opus tasarimci (mimari semasi) + 3 mercek oy + muhur (karar tablosu); cikti mimari-birlesik.json ile birlestirilebilir', phases: [{ title: 'Tasarım', model: 'opus' }, { title: 'Oylama' }, { title: 'Mühür', model: 'opus' }] }
const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery'
const PAKETLER = args.paketler            // [{no, ad, tanim, kaynak, dalga_onerisi}]
const PARTI = args.parti || 3
const DALGA_PLANI = args.dalga_plani || ''
const KATMAN = args.katman || 'Yeni paketler (hazırlık kurulu tur 1)'
const PAKET_SPEC = { type: 'object', properties: {
  no: { type: 'string' }, ad: { type: 'string' }, hedef: { type: 'string' }, mevcut_durum_kodda: { type: 'string' },
  kapsam_ici: { type: 'array', items: { type: 'string' } }, kapsam_disi: { type: 'array', items: { type: 'string' } },
  veri_modeli: { type: 'array', items: { type: 'object', properties: { tablo: { type: 'string' }, degisiklik: { type: 'string' }, kolonlar: { type: 'string' }, indeksler: { type: 'string' }, migration_adi: { type: 'string' } }, required: ['tablo', 'degisiklik'] } },
  uclar: { type: 'array', items: { type: 'object', properties: { fiil: { type: 'string' }, yol: { type: 'string' }, roller: { type: 'string' }, aciklama: { type: 'string' }, kapilar: { type: 'string' } }, required: ['fiil', 'yol', 'aciklama'] } },
  servis_ve_dosyalar: { type: 'array', items: { type: 'object', properties: { dosya: { type: 'string' }, islem: { type: 'string' }, icerik: { type: 'string' } }, required: ['dosya', 'islem', 'icerik'] } },
  ekranlar: { type: 'array', items: { type: 'object', properties: { rota: { type: 'string' }, dosya: { type: 'string' }, ozet: { type: 'string' }, menu_yetki: { type: 'string' } }, required: ['rota', 'dosya', 'ozet'] } },
  is_kurallari: { type: 'array', items: { type: 'string' } },
  testler: { type: 'array', items: { type: 'object', properties: { dosya: { type: 'string' }, tur: { type: 'string' }, senaryolar: { type: 'array', items: { type: 'string' } } }, required: ['dosya', 'tur', 'senaryolar'] } },
  mutasyon_kanitlari: { type: 'array', items: { type: 'string' } }, canli_dogrulama: { type: 'array', items: { type: 'string' } },
  onkosul_paketler: { type: 'array', items: { type: 'string' } }, dosya_ayak_izi: { type: 'array', items: { type: 'string' } },
  fazlar: { type: 'array', items: { type: 'object', properties: { ad: { type: 'string' }, ne_zaman: { type: 'string' }, icerik: { type: 'string' } }, required: ['ad', 'ne_zaman', 'icerik'] } },
  mevzuat: { type: 'array', items: { type: 'object', properties: { kaynak: { type: 'string' }, guven: { type: 'string' }, not: { type: 'string' } }, required: ['kaynak', 'guven', 'not'] } },
  riskler: { type: 'array', items: { type: 'string' } }, acik_sorular: { type: 'array', items: { type: 'string' } }, tahmin_gun: { type: 'string' } },
  required: ['no', 'ad', 'hedef', 'mevcut_durum_kodda', 'kapsam_ici', 'kapsam_disi', 'veri_modeli', 'uclar', 'servis_ve_dosyalar', 'ekranlar', 'is_kurallari', 'testler', 'mutasyon_kanitlari', 'canli_dogrulama', 'onkosul_paketler', 'dosya_ayak_izi', 'fazlar', 'mevzuat', 'riskler', 'tahmin_gun'] }
const ORTAK = `Türkçe yaz. Salt okunur (kod yazma/derleme/test YOK; grep/sed -n ile kaynağa bak, ≤ 25 araç çağrısı). Proje: ${KOK} (.NET 8 API Modules/*Modulu.cs DTO+Servis+Controller tek dosya; Blazor WASM Kuyumcu.Web saf CSS/CSP sıkı; MySQL; EF migration; çok kiracılı IKiraciVarlik + HasQueryFilter + KiraciYazmaDenetcisi; tek defter yazma yolu HareketFabrikasi.Ekle; kapılar TevsikKapisi/MasakKapisi/GunKapanisiKapisi/VadeKapisi; [RolGerekli] tam eşleşme + [ModulYetkisi]; DenetimServisi append-only; alan şifreleme AES-GCM; testler xUnit gerçek MySQL + bUnit + HTTP WebApplicationFactory; dotnet/denetim.sh sayaçları). Mevcut belgeler: ${KOK}/dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md (§ Karar tablosu, REVİZE plan, Mühür eki), ${KOK}/dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-1.md (engeller), ${KOK}/PAZAR-ARASTIRMASI-2026-09-16.md, ${KOK}/deploy/ ve ${KOK}/docker-compose.yml (teslimat paketleri için). Anayasa md.1: iddiaları dosya:satır ile kanıtla; emin değilsen 'DOĞRULANMALI'. DoD sözleşmesi bağlayıcı: her yeni varlık PATCH/pasife alma + yetki + denetim kaydı + DegismezIpucu ile doğar; her düğme istek atar ya da disabled; tek düzenleme kalıbı (Cekmece/SatirIciDuzenle). ÇIKTI SINIRI: her dizi ≤ 12 madde, her dizge ≤ 320 karakter; toplam ≤ 3.500 kelime; StructuredOutput tek seferde.`
phase('Tasarım')
const tasarimlar = []
for (let i = 0; i < PAKETLER.length; i += PARTI) {
  const parti = PAKETLER.slice(i, i + PARTI)
  const s = await parallel(parti.map(p => () => agent(`${ORTAK}
GÖREV — PAKET TASARIMI (Bertan adına, uygulanabilir spesifikasyon): PAKET ${p.no} — ${p.ad}. Kurul tanımı/engel ("||" ile ayrılmış satırlar; içinde BAŞKA paketlere ait engel satırları da olabilir — yalnız bu paketin ad alanıyla örtüşen satırları ve "MÜHÜR 2:" notunu esas al, diğerlerini kapsam dışı bırak): ${p.tanim}. Kaynak: ${p.kaynak || 'hazırlık kurulu tur 1'}. Dalga önerisi: ${p.dalga_onerisi || '-'}.
Üret: hedef; kodda bugünkü durum (dosya:satır); kapsam içi/dışı; veri modeli (varsa; IKiraciVarlik + (tenant_id, …) indeks); uçlar (roller + kapılar); servis ve dosyalar (YENI/DEGISTIR, mevcut mekanizmayı yeniden kullan, ikinci yol açma); ekranlar (rota/dosya/menü yetkisi; UX sözleşmesi UX-S1…S33; basit/pro mod); iş kuralları (fail-closed); testler (DB/HTTP/bUnit/saf, senaryo adlarıyla); mutasyon kanıtları (test adıyla); canlı doğrulama (HTTP kodu + SQL); önkoşul paketler (dizge); dosya ayak izi (tam yol); fazlar (A/B; Faz A sevk edilebilir asgari); mevzuat (güven derecesiyle); riskler; açık sorular; tahmin_gun. 'no' alanına aynen "${p.no}" yaz.`, { label: 'tasarim:' + p.no, phase: 'Tasarım', model: 'opus', effort: 'medium', schema: PAKET_SPEC })))
  tasarimlar.push(...s.filter(Boolean))
  log(`tasarım: ${tasarimlar.length}/${PAKETLER.length}`)
}
phase('Oylama')
const OY = { type: 'object', properties: { mercek: { type: 'string' }, oylar: { type: 'array', items: { type: 'object', properties: { paket_no: { type: 'string' }, oy: { type: 'string' }, kosullar: { type: 'array', items: { type: 'string' } }, gerekce: { type: 'string' }, kanit: { type: 'string' } }, required: ['paket_no', 'oy', 'kosullar', 'gerekce'] } }, capraz_bulgular: { type: 'array', items: { type: 'string' } }, veto: { type: 'array', items: { type: 'object', properties: { paket_no: { type: 'string' }, neden: { type: 'string' } }, required: ['paket_no', 'neden'] } } }, required: ['mercek', 'oylar', 'capraz_bulgular', 'veto'] }
const MERCEKLER = [
  { ad: 'Bertan', model: 'opus', rol: 'BAŞ MİMAR merceği: tutarlılık, mevcut mekanizmanın yeniden kullanımı, veri modeli doğruluğu, önkoşul sırası, paket boyutu (sevk edilebilir mi), dosya çakışması, ADR uyumu. Rastgele 3 pakette dosya:satır kanıtını kendin doğrula.' },
  { ad: 'Mehmet', model: 'sonnet', rol: 'GÜVENLİK merceği (veto yetkin var): kiracı izolasyonu, rol/yetki, fail-closed kapılar, sır yönetimi (üretim sırları, TLS, 3306), yedek şifreleme, içe aktarım riski, abonelik/plan kapılarının yazma yolunu bozmaması, telemetri/hata paketinde kişisel veri, MFA koşulu. Mevzuat/güvenlik kapısını atlayan her akışa VETO.' },
  { ad: 'Aşkın', model: 'sonnet', rol: 'QA merceği (fail-closed kalite kapısı): testler kuralı gerçekten sınıyor mu (mutasyon kanıtı test adıyla), DB/HTTP/bUnit dengesi, kararsızlık riski, canlı doğrulama ölçülebilir mi, kurulum/yedek paketlerinde "kanıt" nasıl üretilir (gerçek publish + gerçek geri yükleme), test kiracısı temizliği.' },
]
const oylar = []
const OYPARTI = 5
for (let i = 0; i < tasarimlar.length; i += OYPARTI) {
  const parti = tasarimlar.slice(i, i + OYPARTI)
  const s = await parallel(MERCEKLER.map(m => () => agent(`Türkçe yaz. Salt okunur (≤ 10 araç çağrısı). SEN: ${m.ad} — ${m.rol}
GÖREV — OYLAMA: aşağıdaki ${parti.length} paket tasarımını kendi merceğinden oyla (EVET / HAYIR / KOSULLU), her paket için koşullar (≤ 6, her biri ≤ 260 karakter, dosya:satır kanıtıyla), gerekçe, kanıt; çapraz bulgular (paketler arası çelişki, çift mekanizma, eksik önkoşul); veto (yalnız Mehmet için bağlayıcı; diğerleri "itiraz" olarak yazar). ÇIKTI SINIRI ≤ 2.500 kelime; StructuredOutput tek seferde. Dalga planı bağlamı: ${DALGA_PLANI.slice(0, 1500)}
TASARIMLAR: ${JSON.stringify(parti).slice(0, 70000)}`, { label: `oy:${m.ad}:${i / OYPARTI + 1}`, phase: 'Oylama', model: m.model, effort: 'medium', schema: OY })))
  for (const o of s.filter(Boolean)) { const v = oylar.find(x => x.mercek === o.mercek); if (v) { v.oylar.push(...o.oylar); v.capraz_bulgular.push(...o.capraz_bulgular); v.veto.push(...o.veto) } else oylar.push(o) }
  log(`oylama: ${Math.min(i + OYPARTI, tasarimlar.length)}/${tasarimlar.length} paket`)
}
phase('Mühür')
const KARAR = { type: 'object', properties: { karar_tablosu: { type: 'array', items: { type: 'object', properties: { paket_no: { type: 'string' }, ad: { type: 'string' }, sonuc: { type: 'string' }, birlesik_kosullar: { type: 'array', items: { type: 'string' } }, dalga: { type: 'integer' }, tahmin_gun: { type: 'string' }, onkosullar: { type: 'array', items: { type: 'string' } } }, required: ['paket_no', 'ad', 'sonuc', 'birlesik_kosullar', 'dalga', 'tahmin_gun', 'onkosullar'] } }, ortak_kararlar: { type: 'array', items: { type: 'string' } }, ozet: { type: 'string' } }, required: ['karar_tablosu', 'ortak_kararlar', 'ozet'] }
const muhur = await agent(`Türkçe yaz. Araç çağrısı gerekmez (≤ 3). Tamer Bey adına MÜHÜR: ${tasarimlar.length} yeni paket için karar tablosu — sonuc (Mehmet vetosu → RED ya da "KOSULLU KABUL (veto koşulu)"; 3 EVET → EVET; KOSULLU varsa "KOSULLU KABUL"), birlesik_kosullar (üç merceği birleştir, tekle, çelişkiyi çöz; ≤ 10 madde, her biri ≤ 240 karakter; veto ilk madde "VETO:"), dalga (plan: ${DALGA_PLANI.slice(0, 1200)}; paket-faz olarak verilmişse Faz A'nın dalgası; sayı), tahmin_gun (kısa), onkosullar (dizge). ortak_kararlar: paketler arası ortak mekanizma kararları (≤ 8). ozet ≤ 800 karakter. ÇIKTI SINIRI ≤ 3.000 kelime; StructuredOutput tek seferde.
TASARIM ÖZETLERİ: ${JSON.stringify(tasarimlar.map(t => ({ no: t.no, ad: t.ad, onkosul: t.onkosul_paketler, tahmin: t.tahmin_gun, ayak: t.dosya_ayak_izi.slice(0, 8), fazlar: t.fazlar.map(f => f.ad) }))).slice(0, 20000)}
OYLAR: ${JSON.stringify(oylar).slice(0, 60000)}`, { label: 'muhur', phase: 'Mühür', model: 'opus', effort: 'medium', schema: KARAR })
return { tasarimlar: [{ katman: KATMAN, sonuc: { katman: KATMAN, okunan_dosyalar: [], ortak_kararlar: muhur ? muhur.ortak_kararlar : [], paketler: tasarimlar } }], oylar, karar_tablosu: muhur ? muhur.karar_tablosu : [], ozet: muhur ? muhur.ozet : '' }
