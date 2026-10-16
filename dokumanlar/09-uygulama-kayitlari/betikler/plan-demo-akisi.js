export const meta = {
  name: 'mihenk-demo-akisi',
  description: 'Cumartesi musteri demosu icin ekran sirasi, anlatim metni, gereken veri ve S-3 oncelik listesi',
  phases: [
    { title: 'Arastirma', detail: 'Pusula referansi, rakip demolari, kuyumcu gunluk akisi, SaaS demo yontemi' },
    { title: 'Senaryo', detail: 'Ekran sirasi + anlatim + gereken veri + S-3 oncelik listesi' },
  ],
}

const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery'
const SDIZIN = KOK + '/dokumanlar/02-mimari/sadelestirme'

const ORTAK = `Dil: TÜRKÇE. Ürün adı **MİHENK** (kurucu kararı 23 Eylül; "Kuyumcu ERP" adı ölüdür,
belgelerde MİHENK yaz). Firma ByGatech A.Ş.

BAĞLAM: Kurucu CUMARTESİ SABAHI GERÇEK BİR MÜŞTERİYE DEMO YAPACAK. Kendi sözleri:
"ana hatlarıyla sık kullanılan tüm ekranları göstereceğim, sadeleştirilmiş ekranlar ile olacak
şekilde." Ayrıca: "demo için hangi ekranlar sırasıyla ne olacak bilemem, onu sen Pusula'dan ve
diğerlerinden araştır, kendin bul."

⚠ BU MÜŞTERİ BİZİ DAHA ÖNCE GÖRDÜ VE ELEŞTİRDİ. Eleştirileri (plan § 1-2'de ölçülmüş):
menü karmaşık, satış ekranı karışık, renkler iyi değil. Rakip olarak PUSULA'yı beğeniyor.
Demo bu üç eleştiriye CEVAP vermeli.

OKUNACAK KAYNAKLAR (repoda, tekrar keşfetme):
 · ${KOK}/PLAN-SADELESTIRME-PUSULA-2026-09-23.md — § 1 müşterinin dört cümlesi, § 4 kabul ölçütleri,
   § 5 iki yüz tek gövde, § 5B kullanıcı profili (ilkokul/lise mezunu tezgâhtar), § 6 S-dalgaları
 · ${SDIZIN}/PUSULA-REFERANS-2026-09-23.md — Pusula'nın ekranları, 6 görsel + demo kare şeridi
 · ${KOK}/PUSULA-KIYAS-RAPORU-2026-09-21.md — madde madde kıyas
 · ${KOK}/PAZAR-ARASTIRMASI-2026-09-16.md — Türkiye kuyumcu yazılımı pazarı
 · ${KOK}/dokumanlar/09-uygulama-kayitlari/ham/qa-turu-8.json — 46 rotanın güncel durumu,
   görsel/kullanıcı dostu puanları, bulgular (hangi ekran demoya HAZIR DEĞİL, oradan görürsün)
 · ${KOK}/dotnet/Kuyumcu.Web/Layout/MenuTanimi.cs (38 öğe) ve Servisler/BasitMenu.cs (8 yol)

ANAYASA MADDE 1: uydurma yok. "Pusula şöyle yapıyor" diyorsan KAYNAĞINI göster; kaynakta yoksa
"DOĞRULANAMADI" yaz. Demoda gösterilecek her ekran için o ekranın BUGÜNKÜ durumunu
qa-turu-8.json'dan kontrol et — kritik bulgusu olan ekranı "hazır" sayma.`

phase('Arastirma')

const SEMA = {
  type: 'object',
  properties: {
    mercek: { type: 'string' },
    bulgular: { type: 'array', items: { type: 'object', properties: {
      baslik: { type: 'string' }, kanit: { type: 'string' }, demoya_etkisi: { type: 'string' },
    }, required: ['baslik', 'kanit', 'demoya_etkisi'] } },
    dogrulanamayan: { type: 'array', items: { type: 'string' } },
  },
  required: ['mercek', 'bulgular', 'dogrulanamayan'],
}

const MERCEKLER = [
  { key: 'pusula', baslik: 'PUSULA NASIL GÖSTERİYOR', detay: 'Repodaki PUSULA-REFERANS ve PUSULA-KIYAS belgelerini oku. Pusula kendi demosunda HANGİ EKRANLARI HANGİ SIRAYLA gösteriyor, ilk 60 saniyede ne gösteriyor, satışı kaç adımda yapıyor, neyi hiç göstermiyor. Müşterimizin Pusula\'da beğendiği somut şey ne (belgelerde yazıyorsa alıntıla). Bulamadığını DOĞRULANAMADI yaz.' },
  { key: 'rakipler', baslik: 'RAKİP DEMO VE SEKTÖR AKIŞI', detay: 'WebSearch/WebFetch yükle (ToolSearch: "select:WebSearch,WebFetch"). Türkiye kuyumcu yazılımlarının (Dezy, Gramino, Milyem, Kuyumex, CetaSoft, ZarifSoft, Terminal Gold, M&L) tanıtım videoları ve demo sayfaları hangi ekranları öne çıkarıyor — ekran sırasını çıkar. Ayrıca genel olarak: bir kuyumcu dükkânında günün akışı nedir (sabah kur girişi, gün içi satış/alış, akşam kasa kapanışı) — demo bu akışı takip ederse daha inandırıcı olur mu? Kaynak göster.' },
  { key: 'yontem', baslik: 'YAZILIM DEMOSU YÖNTEMİ', detay: 'WebSearch/WebFetch yükle. B2B yazılım demosunun kanıtlanmış kuralları: ilk kaç saniyede değer gösterilmeli, "tell-show-tell", demoyu neyle bitirmeli, kaç ekran fazla, müşterinin kendi verisini görmesinin etkisi, demoda YAPILMAMASI gerekenler (ayar ekranı gezmek, boş liste göstermek, hata çıkması). 15 dakikalık bir demo için ekran sayısı üst sınırı ne olmalı — sayı ver.' },
  { key: 'hazirlik', baslik: 'BİZİM EKRANLARIN DEMO HAZIRLIĞI', detay: 'ham/qa-turu-8.json dosyasını oku (46 rota, 225 bulgu, 17 kritik). Demoda gösterilmeye ADAY her ekran için: bugünkü görsel ve kullanıcı dostu puanı, açık KRİTİK bulgusu var mı, düzenlenemeyen veri girişi var mı. Çıktı: "demoya hazır" / "düzeltilirse hazır" / "demoda GÖSTERİLMEMELİ" üçlü sınıflaması, her biri gerekçeli. Ayrıca MenuTanimi.cs (38 öğe) ile BasitMenu.cs (8 yol) karşılaştır: basit modun 8 yolu demoyu taşıyor mu, eksik var mı.' },
]

const arastirma = (await parallel(MERCEKLER.map(m => () =>
  agent(ORTAK + '\n\nGÖREV — ARAŞTIRMA MERCEĞİ: ' + m.baslik + '\n\n' + m.detay + '\n\nADIM EKONOMİSİ: en çok 35 araç çağrısı.',
    { label: 'demo:' + m.key, phase: 'Arastirma', effort: 'medium', schema: SEMA })
))).filter(Boolean)

log('Demo araştırması bitti: ' + arastirma.length + '/4 mercek')

phase('Senaryo')

const senaryo = await agent(ORTAK + `

GÖREV — CUMARTESİ DEMO SENARYOSU (Köprü teslimat + Eray satış + Aşkın QA merceği).

Araştırma çıktısı:
${JSON.stringify(arastirma, null, 1)}

ÜRET: ${SDIZIN}/DEMO-SENARYOSU-2026-09-26.md — kurucunun eline alıp uygulayacağı belge.

İÇİNDE OLACAKLAR:
1. **EKRAN SIRASI** — numaralı, her adımda: hangi rota, kaç saniye, kurucu ne yapacak (tık tık),
   ne söyleyecek (tek cümle, jargonsuz), müşteri neyi görecek. Toplam 15 dakikayı AŞMASIN.
   Sıralamanın GEREKÇESİNİ yaz — neden bu ekran önce, neden şu sonra; araştırmaya dayandır.
2. **MÜŞTERİNİN ÜÇ ELEŞTİRİSİNE CEVAP** — demonun hangi anı "menü karmaşık", hangi anı
   "satış ekranı karışık", hangi anı "renkler iyi değil" eleştirisini çürütüyor. Açıkça işaretle.
3. **GEREKEN VERİ** — demo kiracısında hangi kayıtlar bulunmalı: kaç müşteri, kaç ürün, kaç
   geçmiş satış, kasa bakiyesi ne olmalı (ASLA negatif olmasın), kur girilmiş olmalı mı.
   Bu liste P-30 (temiz demo kiracısı) paketine doğrudan girdi olacak — ölçülebilir yaz.
4. **S-3 ÖNCELİK SIRASI** — demoda gösterilecek ekranlar, sadeleştirmede HANGİ SIRAYLA
   yapılmalı. Plan § 6 S-3'te sıra "Panel → Cariler → Ürünler → Kasalar → Raporlar → Etiketler →
   Bildirimler" diyor; demo senaryosu başka sıra gerektiriyorsa DEĞİŞTİR ve gerekçelendir.
   Demoda GÖSTERİLMEYECEK ekranları da yaz — onlar S-4'e düşecek.
5. **GÖSTERİLMEYECEKLER** — demoda açılmaması gereken ekranlar ve nedeni (kritik bulgusu var,
   boş görünüyor, jargon dolu…). qa-turu-8.json'dan kanıtla.
6. **ARIZA PLANI** — demo sırasında bir şey ters giderse kurucu ne yapsın (hangi ekrana kaçsın,
   hangi cümleyi kursun). Yazılım demosu yöntem araştırmasından türet.
7. **PROVA LİSTESİ** — cuma akşamı orkestratörün baştan sona tıklayıp doğrulayacağı adımlar.

Ürün adı her yerde MİHENK. Kurucu ilkokul/lise mezunu tezgâhtara satıyor, müşteri de kuyumcu —
cümleler sade olsun, yazılım jargonu kullanma.`,
  { label: 'demo:senaryo', phase: 'Senaryo', effort: 'high', schema: {
    type: 'object',
    properties: {
      rapor: { type: 'string' },
      toplam_dakika: { type: 'number' },
      adimlar: { type: 'array', items: { type: 'object', properties: {
        sira: { type: 'number' }, rota: { type: 'string' }, saniye: { type: 'number' },
        kurucu_ne_yapar: { type: 'string' }, ne_soyler: { type: 'string' },
        neden_bu_sirada: { type: 'string' }, bugun_hazir_mi: { type: 'string' },
      }, required: ['sira', 'rota', 'saniye', 'kurucu_ne_yapar', 'ne_soyler', 'neden_bu_sirada', 'bugun_hazir_mi'] } },
      s3_oncelik: { type: 'array', items: { type: 'string' } },
      gosterilmeyecekler: { type: 'array', items: { type: 'string' } },
      gereken_veri: { type: 'array', items: { type: 'string' } },
      dogrulanamayan: { type: 'array', items: { type: 'string' } },
    },
    required: ['rapor', 'toplam_dakika', 'adimlar', 's3_oncelik', 'gosterilmeyecekler', 'gereken_veri', 'dogrulanamayan'],
  } })

log('Demo senaryosu: ' + (senaryo && senaryo.adimlar || []).length + ' adım · ' + (senaryo && senaryo.toplam_dakika) + ' dk')
return { arastirma, senaryo }
