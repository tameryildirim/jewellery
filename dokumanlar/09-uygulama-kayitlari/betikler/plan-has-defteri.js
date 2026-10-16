export const meta = {
  name: 'mihenk-has-defteri-karari',
  description: 'Eski altinla odeme has defterine mi TL defterine mi islenir: mevzuat + sektor teamulu + rakip davranisi + kendi kodumuz',
  phases: [
    { title: 'Arastirma', detail: 'Mevzuat, sektor teamulu, rakipler, kendi kodumuz — 4 paralel mercek' },
    { title: 'Karar', detail: 'Ekip oyu, kaynakli karar, B-4 icin baglayici sartname' },
  ],
}

const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery'
const SDIZIN = KOK + '/dokumanlar/02-mimari/sadelestirme'

const ORTAK = `Dil: TÜRKÇE. Ürün MİHENK (ByGatech A.Ş.) — Türkiye'de kuyumcu dükkânı için ERP.

SORU (tek cümle): **Müşteri satışın bir kısmını eski altınıyla öderse (karma ödeme: nakit + eski altın),
bu altın bacağı HAS DEFTERİNE mi (gram bazlı has hesabı) yoksa TL DEFTERİNE mi (parasal tutar) işlenir?
Yoksa İKİSİNE BİRDEN mi, ve öyleyse hangi sırayla / hangi kur ile?**

Bu soru kurucuya soruldu, kurucu "bana neden soruyorsun, araştır" dedi. Karar EKİBİNDİR ve
KAYNAĞIYLA verilecektir. Anayasa Madde 1: uydurma yok. Mevzuat hükmünü "DOĞRULANDI" diye yazmak için
BİRİNCİL kaynak (mevzuat metni, GİB/TÜRMOB yayını, mali müşavir yayını) gerekir; yoksa "DOĞRULANAMADI" yaz.

NEDEN ÖNEMLİ: kuyumcudaki EN SIK ödeme biçimi budur ve bugün ürünümüzde ÇALIŞMIYOR
(Islemler.razor:1437 — "Sunucunun kabul ettiği bacak araçları — ALTIN YOKTUR"). B-4 paketi bunu yazacak
ama yanlış deftere işlerse kuyumcunun kârı ve pozisyonu YANLIŞ çıkar — para hatası.

REPODA OKUNACAKLAR (tekrar keşfetme):
 · ${KOK}/PAZAR-ARASTIRMASI-2026-09-16.md — Türkiye kuyumcu yazılımı pazarı, rakipler
 · ${KOK}/PUSULA-KIYAS-RAPORU-2026-09-21.md ve ${SDIZIN}/PUSULA-REFERANS-2026-09-23.md
 · ${KOK}/dotnet/Kuyumcu.Api/Data/Entities.cs — özellikle HasGrami yorumları (satır ~715, ~728, ~736)
 · ${KOK}/dotnet/Kuyumcu.Api/Data/HareketFabrikasi.cs — defterin TEK yazma yolu`

phase('Arastirma')

const SEMA = {
  type: 'object',
  properties: {
    mercek: { type: 'string' },
    bulgular: {
      type: 'array', items: {
        type: 'object', properties: {
          iddia: { type: 'string' },
          kaynak: { type: 'string', description: 'Birincil kaynak adi + URL veya dosya:satir' },
          dogrulandi: { type: 'boolean' },
          karara_etkisi: { type: 'string' },
        }, required: ['iddia', 'kaynak', 'dogrulandi', 'karara_etkisi'],
      },
    },
    dogrulanamayan: { type: 'array', items: { type: 'string' } },
  },
  required: ['mercek', 'bulgular', 'dogrulanamayan'],
}

const MERCEKLER = [
  { key: 'mevzuat', baslik: 'MEVZUAT', detay: 'WebSearch/WebFetch yükle (ToolSearch: "select:WebSearch,WebFetch"). ARAŞTIR: Vergi Usul Kanunu ve Tek Düzen Hesap Planı açısından kuyumcuda altın stoğu ve altın cinsinden alacak/borç nasıl kayda alınır; kıymetli maden işletmelerinde "has" (1000 ayar karşılığı) bazlı takip mevzuatta tanınıyor mu; 197 sayılı hesap ya da benzeri özel hesap kullanımı var mı; KDV açısından eski altın alımı (gider pusulası ile) nasıl belgelenir ve bu bir ALIŞ mı yoksa ödeme ARACI mı sayılır; MASAK/KMTS tarafında kıymetli maden alım-satımının kayıt yükümlülüğü. GİB, TÜRMOB, Resmî Gazete ve mali müşavir yayınlarını birincil kaynak say; forum cevabını sayma.' },
  { key: 'teamul', baslik: 'SEKTÖR TEAMÜLÜ', detay: 'WebSearch/WebFetch yükle. ARAŞTIR: Kapalıçarşı ve kuyumcu esnafının fiilî defter tutma biçimi — "has hesabı", "has cari", "gram cari", "milyem" kavramları. Kuyumcu müşterisine "bende 12 gram has alacağın var" mı der, "12.000 TL alacağın var" mı? Toptancı-perakendeci arasındaki hesap hangi birimle kapanır? Altın fiyatı gün içinde değişirken TL bazlı tutmanın kuyumcuya ne zararı var (kur farkı riski kimde kalır)? Kuyumcu dernekleri (İstanbul Kuyumcular Odası, JTR, Altın Madencileri vb.), sektör dergileri ve eğitim içerikleri kaynak olsun.' },
  { key: 'rakip', baslik: 'RAKİPLER NE YAPIYOR', detay: 'Önce repodaki PAZAR-ARASTIRMASI ve PUSULA belgelerini oku. Sonra WebSearch/WebFetch ile: Pusula, Dezy, Gramino, Milyem, Kuyumex, CetaSoft, ZarifSoft, M&L, Terminal Gold — bu programlar eski altınla ödemeyi nasıl işliyor; ekranlarında "has" kolonu var mı, cari bakiyesini gram mı TL mi gösteriyorlar; tanıtım/eğitim içeriklerinde bu akış nasıl anlatılıyor. PUSULA-REFERANS § 3\'te Pusula\'nın satış fişinde bacak olarak "14 HURDASI (2,55 g × 2.156,86 TL)" göründüğü yazıyor — bu bacağın hangi deftere düştüğünü kaynakla çıkarmaya çalış, çıkaramazsan DOĞRULANAMADI yaz.' },
  { key: 'kodumuz', baslik: 'BİZİM KODUMUZ BUGÜN NE YAPIYOR', detay: 'Web yok, KOD OKU. (a) HURDA_ALIS yolu bugün ne yapıyor: müşteriden eski altın alınınca hangi tabloya hangi satır düşüyor, has grami nasıl hesaplanıyor (gram × ayar/1000?), cari bakiyesi gram mı TL mi güncelleniyor, kasa hareketi oluşuyor mu. Dosya:satır ver. (b) cari_bakiyeler tablosunun kolonları: TL ve has gram AYRI mı tutuluyor? (c) HareketFabrikasi.cs defterin tek yazma yolu — hangi alanlar zorunlu. (d) TEK ödeme aracı olarak ALTIN seçilince (OdemeListesi, SATIS dalı) bugün ne oluyor — bu zaten çalışıyor, DAVRANIŞINI BELGELE; B-4 bunun karma bacak hâlini yazacak. (e) Entities.cs:715 ve :728 yorumlarını birebir alıntıla.' },
]

const arastirma = (await parallel(MERCEKLER.map(m => () =>
  agent(ORTAK + '\n\nGÖREV — MERCEK: ' + m.baslik + '\n\n' + m.detay + '\n\nADIM EKONOMİSİ: en çok 35 araç çağrısı. Her bulguda kaynak ZORUNLU.',
    { label: 'has:' + m.key, phase: 'Arastirma', effort: 'medium', schema: SEMA })
))).filter(Boolean)

log('Araştırma bitti: ' + arastirma.length + '/4 mercek')

phase('Karar')

const karar = await agent(ORTAK + `

GÖREV — KARAR (Bertan mimari + Mustafa FinOps + Mehmet güvenlik/uyum + Aşkın QA; Tamer Bey mühürler).

Dört merceğin çıktısı:
${JSON.stringify(arastirma, null, 1)}

ÜRET: ${SDIZIN}/KARAR-HAS-DEFTERI-2026-09-23.md ve yapılandırılmış çıktı.

KARARDA OLACAKLAR:
1. **CEVAP** — eski altın bacağı hangi deftere işlenir. Seçenekler: (A) yalnız has defteri (gram),
   (B) yalnız TL defteri, (C) İKİSİ BİRDEN (has gram + o anki kur ile TL karşılığı, hangisi asıl).
   Tek cümlelik net karar + hangi kaynağa dayandığı.
2. **KUR SORUSU** — TL karşılığı yazılacaksa hangi kur: işlem anındaki has fiyatı mı, gün sonu mu,
   dükkânın kendi alış kuru mu. Kaynakla.
3. **MEVCUT DAVRANIŞLA UYUM** — bugünkü HURDA_ALIS ve "tek ödeme olarak ALTIN" yolları ne yapıyor;
   kararımız onlarla TUTARLI mı. Tutarsızsa hangisinin değişeceğini ve NEDEN (mevcut davranış yanlışsa
   bunu açıkça söyle, ama "mevcut hali bozulmasın" şartı gereği değişimin göçü nasıl olacak, yaz).
4. **B-4 İÇİN BAĞLAYICI ŞARTNAME** — uygulayıcının doğrudan alacağı madde listesi: hangi tabloya ne
   yazılır, hangi kapılardan geçer, hangi testler yazılır, hangi mutasyonlar kırmızı dönmeli.
5. **AÇIK KALAN** — mali müşavir teyidi GEREKEN bir nokta kaldıysa ayrı başlıkta yaz; ama kararı
   ERTELEME. Ekip kararını verir, mali müşavir teyidi doğrulama olarak beklenir (K-10 ailesi).
6. **RİSK** — yanlış defter seçilirse kuyumcunun hangi rakamı yanlış çıkar (kâr, pozisyon, cari bakiye),
   ve bunu yakalayacak test hangisi.

Anayasa Madde 1: doğrulanamayan hiçbir mevzuat hükmünü koda sabit yazdırma; yapılandırmadan okunacak
ve varsayılanı TUTUCU olacak şekilde tarif et.`,
  { label: 'has:karar', phase: 'Karar', effort: 'high', schema: {
    type: 'object',
    properties: {
      rapor: { type: 'string' },
      karar: { type: 'string', enum: ['HAS_DEFTERI', 'TL_DEFTERI', 'IKISI_BIRDEN'] },
      karar_cumlesi: { type: 'string' },
      dayanak: { type: 'array', items: { type: 'string' } },
      kur_kurali: { type: 'string' },
      mevcut_davranisla_uyum: { type: 'string' },
      b4_sartname: { type: 'array', items: { type: 'string' } },
      test_listesi: { type: 'array', items: { type: 'string' } },
      mutasyon_listesi: { type: 'array', items: { type: 'string' } },
      mali_musavir_teyidi_gereken: { type: 'array', items: { type: 'string' } },
      dogrulanamayan: { type: 'array', items: { type: 'string' } },
    },
    required: ['rapor', 'karar', 'karar_cumlesi', 'dayanak', 'kur_kurali', 'mevcut_davranisla_uyum', 'b4_sartname', 'test_listesi', 'mutasyon_listesi', 'mali_musavir_teyidi_gereken', 'dogrulanamayan'],
  } })

log('Karar: ' + (karar && karar.karar) + ' — ' + (karar && karar.karar_cumlesi || '').slice(0, 120))
return { arastirma, karar }
