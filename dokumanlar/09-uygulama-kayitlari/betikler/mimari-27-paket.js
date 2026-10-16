export const meta = {
  name: 'kuyumcu-kalan-27-mimari',
  description: 'Kalan 27 paket icin katman bazli mimari (Opus mimarlar), uc mercekli oylama, dalga/cakisma sentezi',
  phases: [
    { title: 'Tasarim', detail: '6 katman mimari (Opus): paket basina uygulanabilir spesifikasyon', model: 'opus' },
    { title: 'Oylama', detail: 'Bertan (mimari, Opus) · Mehmet (guvenlik) · Askin (test) mercekleri: EVET/HAYIR/KOSULLU' },
    { title: 'Sentez', detail: 'Dalgalar, dosya cakisma haritasi, kesisen kararlar, ADR onerileri', model: 'opus' },
  ],
}

const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery'
const KATMANLAR = args.katmanlar

const PAKET_SPEC = {
  type: 'object',
  properties: {
    no: { type: 'integer' },
    ad: { type: 'string' },
    hedef: { type: 'string', description: 'Perakende kuyumcu icin bu paket neyi cozer, 2-4 cumle' },
    mevcut_durum_kodda: { type: 'string', description: 'Kodda bugun ne var (dosya:satir kanitli)' },
    kapsam_ici: { type: 'array', items: { type: 'string' } },
    kapsam_disi: { type: 'array', items: { type: 'string' } },
    veri_modeli: { type: 'array', items: { type: 'object', properties: {
      tablo: { type: 'string' }, degisiklik: { type: 'string' }, kolonlar: { type: 'string' }, indeksler: { type: 'string' }, migration_adi: { type: 'string' } },
      required: ['tablo', 'degisiklik'] } },
    uclar: { type: 'array', items: { type: 'object', properties: {
      fiil: { type: 'string' }, yol: { type: 'string' }, roller: { type: 'string' }, aciklama: { type: 'string' }, kapilar: { type: 'string' } },
      required: ['fiil', 'yol', 'aciklama'] } },
    servis_ve_dosyalar: { type: 'array', items: { type: 'object', properties: {
      dosya: { type: 'string' }, islem: { type: 'string', description: 'YENI | DEGISTIR' }, icerik: { type: 'string' } },
      required: ['dosya', 'islem', 'icerik'] } },
    ekranlar: { type: 'array', items: { type: 'object', properties: {
      rota: { type: 'string' }, dosya: { type: 'string' }, ozet: { type: 'string' }, menu_yetki: { type: 'string' } },
      required: ['rota', 'dosya', 'ozet'] } },
    is_kurallari: { type: 'array', items: { type: 'string' }, description: 'Fail-closed kurallar, isaret sozlesmeleri, degismezler' },
    testler: { type: 'array', items: { type: 'object', properties: {
      dosya: { type: 'string' }, tur: { type: 'string', description: 'DB | HTTP | bUnit | saf' }, senaryolar: { type: 'array', items: { type: 'string' } } },
      required: ['dosya', 'tur', 'senaryolar'] } },
    mutasyon_kanitlari: { type: 'array', items: { type: 'string' }, description: 'Kural bozulunca hangi test adiyla kirmiziya donmeli' },
    canli_dogrulama: { type: 'array', items: { type: 'string' } },
    onkosul_paketler: { type: 'array', items: { type: 'integer' } },
    dosya_ayak_izi: { type: 'array', items: { type: 'string' }, description: 'DEGISTIRILECEK mevcut dosyalar (cakisma analizi icin tam yol)' },
    mevzuat: { type: 'array', items: { type: 'object', properties: {
      kaynak: { type: 'string' }, guven: { type: 'string', description: 'DOGRULANDI | DOGRULANMALI | VARSAYIM' }, not: { type: 'string' } },
      required: ['kaynak', 'guven', 'not'] } },
    riskler: { type: 'array', items: { type: 'string' } },
    acik_sorular: { type: 'array', items: { type: 'string' } },
    tahmin_gun: { type: 'string' },
  },
  required: ['no', 'ad', 'hedef', 'mevcut_durum_kodda', 'kapsam_ici', 'kapsam_disi', 'veri_modeli', 'uclar', 'servis_ve_dosyalar', 'ekranlar', 'is_kurallari', 'testler', 'mutasyon_kanitlari', 'canli_dogrulama', 'onkosul_paketler', 'dosya_ayak_izi', 'mevzuat', 'riskler', 'tahmin_gun'],
}

const KATMAN_SONUC = {
  type: 'object',
  properties: {
    katman: { type: 'string' },
    ortak_kararlar: { type: 'array', items: { type: 'string' }, description: 'Katman genelinde gecerli tasarim kararlari ve gerekceleri' },
    paketler: { type: 'array', items: PAKET_SPEC },
    okunan_dosyalar: { type: 'array', items: { type: 'string' } },
  },
  required: ['katman', 'ortak_kararlar', 'paketler'],
}

const ORTAK = `
BAĞLAYICI KURALLAR:
- Proje kökü: ${KOK}. Bu görev SALT OKUNURDUR: hiçbir dosya oluşturma/değiştirme/silme; git checkout/stash/reset/rm/mv çalıştırma; dotnet build/test/ef ÇALIŞTIRMA (paralelde başka bir ajan paket 33'ü derliyor; eşzamanlı derleme obj/ çakıştırır).
- Sır/parola/bağlantı dizesi değerini ASLA yazma.
- Türkçe yaz. Kod gerçeğine dayan: her 'mevcut durum' iddiasına dosya:satır kanıtı ekle. Tahmin etme; okumadıysan 'okunmadı' de.
- MEVZUAT UYDURMA: bir yasal kural yazacaksan kaynağı adıyla ver ve güven düzeyini DOGRULANDI/DOGRULANMALI/VARSAYIM diye işaretle. Devir belgesindeki A7 araştırması (tevsik 30.000 TL nakit haddi ve işlem tutarı kuralı; MASAK 185.000 TL eşiği ve bağlantılı işlem kümülatifi; hurda stopajı vatandaşta yok; döviz alım-satımı yetkisiz kuyumcuya yasak; kampanya ≤92 gün FEY md.11/3) doğrulanmış sayılır.
- KAPSAM: müşteri PERAKENDE kuyumcu dükkânı (tek dükkân + 2–3 şube). İmalat/atölye (Katman 1) KAPSAM DIŞI. e-Fatura entegrasyonu K1 sözleşmesi gelmeden mock ile yapılmaz (ADR-0012). Döviz alım-satımı ucu açılmaz (PAKET-15 karar). Veri içe aktarımı ADR-0014 ile yasak (paket 47 için seçenek sun, oylama karar verir).
- MİMARİ DEĞİŞMEZLER (ihlal eden tasarım reddedilir): her yeni varlık IKiraciVarlik + tenant_id + (tenant_id, …) indeks; global kiracı filtresi (KuyumcuDbContext.HasQueryFilter) ve KiraciYazmaDenetcisi; IgnoreQueryFilters/Find/FindAsync yasak (denetim.sh); deftere yazan TEK yol HareketFabrikasi.Ekle(db, OdemeIzi, taslak) (Hareketler.Add başka yerde yasak); kur_snapshot_id zorunlu; kasa para birimi kapısı (K6); gün kilidi (GunKapanisiKapisi via IslemDogrulayici.KasaDogrula(..., an)); tevsik kapısı (TevsikKapisi.Dogrula) ve MASAK kümülatifi (MasakKapisi.DogrulaKumulatif) para el değiştiren her yeni akışta; vade/limit (VadeKapisi) borç doğuran her akışta; denetim izi append-only (ADR-0004); alan şifreleme kimlik alanlarında (ADR-0003); rol denetimi [RolGerekli] TAM eşleşme + [ModulYetkisi]; DTO'larda DataAnnotations yalnız controller yolunda çalışır, servis içi elle kontrol; Sozlukler.cs TR+EN; ekran testleri bUnit (sahte API deseni CariEkranTestleri.cs); DB testleri gerçek MySQL kuyumcu_test (ADR-0011); her kural için MUTASYON KANITI; migration adı 'YYYYMMDDHHMMSS_Ad'; belge üretimi istemcide @media print (ADR-0007); rapor ölçeklenmesi (TB-19: sınırsız ToListAsync yasak, sunucuda toplama).
- ÖNCE OKU: ${KOK}/PARITE-IS-LISTESI-MUHURLU-2026-09-14.md (kendi paketlerinin tanımı, rakip paritesi ve kabul ölçütleri), ${KOK}/SONRAKI-OTURUM-PROMPT.md (§1 durum, §3, A5–A17 ilgili bölümler, §6 tuzaklar), ${KOK}/dokumanlar/02-mimari/adr/README.md ve ilgili ADR'ler, ${KOK}/dokumanlar/02-mimari/mimari-tanimi.md, ${KOK}/dokumanlar/08-degerlendirme/teknik-borc-envanteri.md (ilgili TB'ler). Sonra dokunacağın kodu oku: ${KOK}/dotnet/Kuyumcu.Api/Data/Entities.cs, KuyumcuDbContext.cs, HareketFabrikasi.cs, ilgili Modules/*, ilgili Kuyumcu.Web/Pages/*.razor, Kuyumcu.Core/I18n/Sozlukler.cs (yapı), Kuyumcu.Tests/TestOrtami.cs + HttpOrtami.cs + bir ekran testi (desen).
- ÇIKTI HEDEFİ: her paket için, bu bağlamı hiç görmemiş bir Opus uygulayıcı ajanın 'dosya dosya' uygulayabileceği, kanıta dayalı, KENDİ BAŞINA YETERLİ spesifikasyon. Belirsiz bıraktığın her şey 'acik_sorular'a; uydurma yok.
- Paketleri küçük ve sevk edilebilir tut: gerekiyorsa paket içi Faz A/B ayır (A tek başına sevk edilebilir). Var olan mekanizmayı yeniden yazma; genişlet. Her paket için 'dosya_ayak_izi' EKSİKSİZ olsun (Entities.cs, KuyumcuDbContext.cs, Sozlukler.cs, KenarCubugu.razor, UstBar.razor, Program.cs, ModulKayit.cs, denetim.sh gibi ortak dosyalar dahil) — sıralama/çakışma planı buna dayanacak.
`

phase('Tasarim')
log('6 katman mimarı (Opus) başlatılıyor — 27 paket')

const tasarimlar = await parallel(KATMANLAR.map(k => () =>
  agent(`${ORTAK}
GÖREV — ${k.ad} MİMARI (Bertan'ın ekibinden bir mimar olarak konuş). Aşağıdaki paketlerin HER BİRİ için tam spesifikasyon üret; katman genelinde ortak kararları (paylaşılan altyapı, sıralama, tek tablo mu iki tablo mu gibi) 'ortak_kararlar'a yaz ve gerekçele.

PAKETLER (no · ad · PARITE tahmini · bugünkü kod durumu):
${k.paketler.map(p => `- ${p.no} · ${p.ad} · ${p.parite_gun} gün · ${p.durum}`).join('\n')}

ÖZEL NOTLAR:
- Paket 33 (varsa): A17 tasarımı sabittir, yeniden tasarlama; yalnız artık maddeleri ve A17 sonrası uyum noktalarını yaz.
- Paket 47 (varsa): ADR-0014 ile çelişir; en az üç seçenek (ADR korunur + yalnız açılış bakiyesi girişi · kapılı içe aktarım [her satır mevzuat kapılarından geçer] · ADR revizyonu) artı/eksi ve önerinle sun; kararı oylama verecek.
- Paket 6 (varsa): diğer paketlerin (28 hatırlatma, TB-16, ADR-0008 saklama) üzerine bineceği ortak çatıyı tanımla; e-posta/SMS sağlayıcı sözleşmesi yoksa 'uygulama içi bildirim + zamanlanmış iş' ile sınırla, dış kanalı arayüz olarak bırak.
- Paket 1 (varsa): fatura satır kalemi K1'den BAĞIMSIZ modellenmeli (e-Fatura XML üretimi kapsam dışı), KDV kuyum türü ve 805/23-e ile uyumlu; paket 38 bunun üstüne biner.
- Raporlar (34–37 varsa): PARITE'deki 19 rapor listesini birebir çıkar, hangisi zaten var hangisi yok tablo hâlinde; TB-19 ölçeklenme kuralı; rapor tarih aralıkları Zaman.YerelGunAraligiUtc ile (K5 tuzağı).
- Uyum (38–42 varsa): mevzuat dayanağını güven düzeyiyle işaretle; doğrulanmamış hükmü koda koyma, 'yapılandırmadan okunur, varsayılan KAPALI' tasarla.
- Platform (43–48 varsa): 45 TOTP için secret saklama (alan şifreleme ile), kurtarma kodları, brute-force oran sınırı; 46 şube görünürlüğü için hangi sorguların şube filtresi alacağı (tenant filtresi gibi global mi, açık parametre mi) gerekçeli; 43 için ölçülebilir kabul ölçütü (viewport genişlikleri, hangi sayfalar); 44 için Blazor WASM PWA'nın çevrimdışı vaadi (yalnız kabuk, veri değil).
`, { label: `tasarim:${k.kod}`, phase: 'Tasarim', model: 'opus', effort: 'high', schema: KATMAN_SONUC })
    .then(r => ({ katman: k.kod, sonuc: r }))))

const dolu = tasarimlar.filter(Boolean)
log(`Tasarım bitti: ${dolu.length}/${KATMANLAR.length} katman döndü; toplam paket spesifikasyonu: ${dolu.reduce((a, d) => a + (d.sonuc?.paketler?.length || 0), 0)}`)

phase('Oylama')
const OY = {
  type: 'object',
  properties: {
    mercek: { type: 'string' },
    oylar: { type: 'array', items: { type: 'object', properties: {
      paket_no: { type: 'integer' }, oy: { type: 'string', description: 'EVET | HAYIR | KOSULLU' },
      kosullar: { type: 'array', items: { type: 'string' } }, gerekce: { type: 'string' }, kanit: { type: 'string' } },
      required: ['paket_no', 'oy', 'kosullar', 'gerekce'] } },
    capraz_bulgular: { type: 'array', items: { type: 'string' }, description: 'Katmanlar arasi celiski, cift tanim, eksik onkosul' },
    veto: { type: 'array', items: { type: 'object', properties: { paket_no: { type: 'integer' }, neden: { type: 'string' } }, required: ['paket_no', 'neden'] } },
  },
  required: ['mercek', 'oylar', 'capraz_bulgular', 'veto'],
}
const tasarimJson = JSON.stringify(dolu, null, 1)
const MERCEKLER = [
  { ad: 'Bertan', model: 'opus', rol: 'BAŞ MİMAR merceği: tutarlılık, mevcut mekanizmaların yeniden kullanımı, veri modeli doğruluğu (işaret sözleşmeleri, has/TL çift defter, kiracı indeksleri), önkoşul sırası, paket boyutu (sevk edilebilir mi), TB-19 ölçeklenme, ADR uyumu. Kodu kendin açıp spesifikasyonun dayandığı dosya:satır kanıtlarını rastgele 5 pakette DOĞRULA.' },
  { ad: 'Mehmet', model: undefined, rol: 'GÜVENLİK merceği (veto yetkin var): kiracı izolasyonu (IKiraciVarlik, filtre, yazma denetçisi, ExecuteUpdate/Delete beyaz listesi), rol/yetki (RolGerekli tam eşleşme, ModulYetkisi), fail-closed kapılar (tevsik, MASAK kümülatif, gün kilidi, kasa para birimi, vade/limit), alan şifreleme (kimlik/TCKN/TOTP secret), denetim izi append-only, oran sınırı, dosya yükleme (magic byte, boyut, tenant 404), içe aktarım riski (47), sır yönetimi. Mevzuat kapısını atlayan her akışa VETO.' },
  { ad: 'Aşkın', model: undefined, rol: 'QA merceği (fail-closed kalite kapısı): her paketin testleri gerçekten kuralı sınıyor mu (mutasyon kanıtı listesi test adıyla eşleşiyor mu), DB/HTTP/bUnit dengesi, kararsızlık riski (ateşle-unut, WaitForAssertion), canlı doğrulama adımları ölçülebilir mi (beklenen HTTP kodu + SQL kanıtı), test kiracısı temizliği, kültür tuzakları (invariant), mutasyonun yakalayamayacağı çok katmanlı korumalar. Kabul ölçütü belirsiz olan paket KOSULLU.' },
]
const oylar = await parallel(MERCEKLER.map(m => () =>
  agent(`${ORTAK}
GÖREV — OYLAMA. Sen ${m.ad}'sın. ${m.rol}
Aşağıda 6 katman mimarının ürettiği 27 paket spesifikasyonu var. HER paket için EVET / HAYIR / KOSULLU oyu ver; KOSULLU ise koşulları uygulayıcının doğrudan uygulayabileceği somutlukta yaz; HAYIR/VETO ise nedenini kanıtla. Katmanlar arası çelişkileri (aynı tablo iki farklı biçimde, eksik önkoşul, çift mekanizma) 'capraz_bulgular'a yaz. Şüpheliysen dosyayı kendin aç (salt okunur).

SPESİFİKASYONLAR (JSON):
${tasarimJson}
`, { label: `oy:${m.ad}`, phase: 'Oylama', model: m.model, effort: 'high', schema: OY })))

const oyDolu = oylar.filter(Boolean)
log(`Oylama bitti: ${oyDolu.length}/3 mercek döndü; veto sayısı: ${oyDolu.reduce((a, o) => a + (o.veto?.length || 0), 0)}`)

phase('Sentez')
const SENTEZ = {
  type: 'object',
  properties: {
    ozet: { type: 'string' },
    karar_tablosu: { type: 'array', items: { type: 'object', properties: {
      paket_no: { type: 'integer' }, ad: { type: 'string' }, sonuc: { type: 'string', description: 'KABUL | KOSULLU KABUL | RED | YENIDEN TANIMLANDI' },
      birlesik_kosullar: { type: 'array', items: { type: 'string' } }, dalga: { type: 'integer' }, tahmin_gun: { type: 'string' }, onkosullar: { type: 'array', items: { type: 'integer' } } },
      required: ['paket_no', 'ad', 'sonuc', 'birlesik_kosullar', 'dalga', 'tahmin_gun', 'onkosullar'] } },
    dalgalar: { type: 'array', items: { type: 'object', properties: {
      no: { type: 'integer' }, paketler: { type: 'array', items: { type: 'integer' } }, gerekce: { type: 'string' },
      uygulama_sirasi: { type: 'array', items: { type: 'integer' }, description: 'Dalga icinde SERI uygulama sirasi (yaz-seri yasasi; ortak dosyalar)' },
      paralel_guvenli_ciftler: { type: 'array', items: { type: 'string' } } },
      required: ['no', 'paketler', 'gerekce', 'uygulama_sirasi'] } },
    cakisma_haritasi: { type: 'array', items: { type: 'object', properties: { dosya: { type: 'string' }, paketler: { type: 'array', items: { type: 'integer' } } }, required: ['dosya', 'paketler'] } },
    kesisen_kararlar: { type: 'array', items: { type: 'object', properties: { baslik: { type: 'string' }, karar: { type: 'string' }, gerekce: { type: 'string' }, adr_onerisi: { type: 'string' } }, required: ['baslik', 'karar', 'gerekce'] } },
    kurucuya_kalanlar: { type: 'array', items: { type: 'string' }, description: 'Yalniz imza/para/dis taahhut/sertifika gerektirenler' },
    toplam_tahmin_gun: { type: 'string' },
    riskler: { type: 'array', items: { type: 'string' } },
  },
  required: ['ozet', 'karar_tablosu', 'dalgalar', 'cakisma_haritasi', 'kesisen_kararlar', 'kurucuya_kalanlar', 'toplam_tahmin_gun', 'riskler'],
}
const sentez = await agent(`${ORTAK}
GÖREV — SENTEZ (Tamer Bey adına mühür hazırlığı). Elinde 6 katman mimarının 27 paket spesifikasyonu ve 3 merceğin oyları var. Üret:
1) karar_tablosu: her paket için sonuç (Mehmet VETO'su varsa RED ya da koşullar sağlanana kadar KOSULLU; iki EVET + bir KOSULLU = KOSULLU KABUL; koşulları BİRLEŞTİR ve çelişenleri çöz, çözemediğini açıkça yaz), dalga numarası, önkoşullar.
2) dalgalar: önkoşul grafiğine ve DOSYA ÇAKIŞMASINA göre uygulama dalgaları. Şirket yasası 'yaz-seri': aynı dosyaya dokunan paketler AYNI ANDA uygulanmaz; her dalga içinde seri sıra ver; yalnız ayak izi tamamen ayrık çiftleri 'paralel_guvenli_ciftler'e yaz (Sozlukler.cs, Entities.cs, KuyumcuDbContext.cs, KenarCubugu.razor, Program.cs, ModulKayit.cs ortak dosyalardır — bunlara dokunan iki paket paralel DEĞİLDİR). Migration üreten paketler kesinlikle seri.
3) cakisma_haritasi: dosya → paket listesi (yalnız 2+ paketin dokunduğu dosyalar).
4) kesisen_kararlar: katmanlar arası ortak kararlar (ör. bildirim çatısı, belge deposu, fiş/kalem modeli, şube filtresi yaklaşımı, paket 47 kararı) + gerekiyorsa ADR önerisi (numara 0016'dan başlar, başlık + karar cümlesi).
5) kurucuya_kalanlar: yalnız imza/para/dış taahhüt/sertifika (K1, K5, K10, GS1 vb.).
6) toplam_tahmin_gun ve riskler.
Sıralama ilkesi: önce derleme/akış güvenliğini bozmayan küçük altyapı (6, 4, 1-Faz A), sonra günlük dükkân işini doğrudan etkileyenler (19, 25, 27, 29, 31, 32, 20), sonra rapor/uyum, en son platform; ama önkoşul grafiği her zaman öncedir.

TASARIMLAR (JSON):
${tasarimJson}

OYLAR (JSON):
${JSON.stringify(oyDolu, null, 1)}
`, { label: 'sentez', phase: 'Sentez', model: 'opus', effort: 'high', schema: SENTEZ })

return { tasarimlar: dolu, oylar: oyDolu, sentez }