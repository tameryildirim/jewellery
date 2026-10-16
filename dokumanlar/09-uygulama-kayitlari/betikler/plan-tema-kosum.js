export const meta = {
  name: 'kuyumcu-tema-sistemi',
  description: '8 renk semasi x aydinlik/karanlik x 3 yazi boyutu: kurumsal SaaS arastirmasi + kontrast dogrulanmis belirtec sistemi + mockup',
  phases: [
    { title: 'Arastirma', detail: 'Kurumsal SaaS renk sistemleri: web arastirmasi, 4 paralel mercek' },
    { title: 'Palet', detail: '8 sema x aydinlik/karanlik, 13 semantik belirtec, kontrast olculur' },
    { title: 'Mockup', detail: '8 sema x 2 tema tek sayfada, gercek ekranla' },
  ],
}

const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery'
const PLAN = KOK + '/PLAN-SADELESTIRME-PUSULA-2026-09-23.md'
const SDIZIN = KOK + '/dokumanlar/02-mimari/sadelestirme'

const ORTAK = `Dil: TÜRKÇE. Kuyumcu ERP — Blazor WASM, saf CSS, CDN/npm YASAK (CSP).
BAĞLAYICI KAYNAK: ${PLAN} § 5A "Görsel yön — Sakin Lüks" ve § 5B "Kullanıcı profili".

⚠ PLANIN İÇİNDE ÇELİŞKİ VAR, ÇÖZÜMÜ ŞU: § 8.1 (ESKİ) "üçten biri: A Altın Klasik / B Platin Mavi /
C Zümrüt" diyor. § 5A (YENİ, 23 Eylül kurucu talimatı) ise şunu diyor ve BAĞLAYICI OLAN BUDUR:
  "ürün 8 renk şeması ile gelir (Platin Mavi · Zümrüt · Grafit · Bordo · Okyanus · Menekşe ·
   Zeytin · Fildişi = açık menü; ALTIN/AMBER YOK, kurucu ve müşteri beğenmedi; her şema 13
   semantik belirteçle tanımlı, açık menü için menuBaslik/menuAktifMetin belirteçleri ayrıca)
   × aydınlık/karanlık × 3 yazı boyutu (16/18/20 px)."
ALTIN/AMBER VURGU KULLANMA. "A/B/C" adlandırması ÖLÜDÜR.

KURUCU TALİMATI (23 Eylül, birebir): "en az 7-8 tane renk olacak, bana sorma, internetten araştır
kurumsal programlarda nasıl olmalı, koyu tema ile açık temadaki renkler uygulamaya göre de
farklılık gösterebilir." → Karar ekipte, kurucuya sorulmaz. Karanlık tema aydınlığın ters çevrilmişi
DEĞİLDİR; her şema için ayrı ayarlanır.

ÖLÇÜLEN MEVCUT DURUM (orkestratör, 23.09):
 · dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css — 520 satır, bugünkü belirteç isimleri BURADA.
 · Servisler/TemaServisi.cs — 19. satır, bugün yalnız açık/koyu (data-tema). data-sema YOK.
 · kullanicilar tablosunda tema/sema/yazi_boyutu kolonu YOK → tercih kalıcılığı migration ister.
 · app.css'te 73 ham font-size + 75 ham padding bildirimi var (S-1'de belirtece bağlanacak).
 · Sayfada vurgu rengi taşıyan öğe hedefi ≤ 3 (VURGU_OGE_MAX).

⚠ KOD YAZILMAZ. Bu koşum yalnız ${SDIZIN}/ altına yazar. Beş kod şeridi aynı ağaçta koşuyor;
hiçbir .cs/.razor/.css üretim dosyasına DOKUNMA. belirtecler.css YALNIZ OKUNUR.`

phase('Arastirma')

const ARS = {
  type: 'object',
  properties: {
    mercek: { type: 'string' },
    bulgular: { type: 'array', items: { type: 'object', properties: {
      kaynak: { type: 'string', description: 'Ürün/sistem adı + URL' },
      ne_yapiyor: { type: 'string' },
      sayilar: { type: 'string', description: 'kaç ton, kaç belirteç, hangi kontrast eşiği' },
      bize_dersi: { type: 'string' },
    }, required: ['kaynak', 'ne_yapiyor', 'sayilar', 'bize_dersi'] } },
    karanlik_tema_kurallari: { type: 'array', items: { type: 'string' } },
    kacinilacaklar: { type: 'array', items: { type: 'string' } },
    dogrulanamayan: { type: 'array', items: { type: 'string' } },
  },
  required: ['mercek', 'bulgular', 'karanlik_tema_kurallari', 'kacinilacaklar', 'dogrulanamayan'],
}

const MERCEKLER = [
  { key: 'sistemler', baslik: 'OLGUN RENK SİSTEMLERİ', detay: 'Radix Colors, IBM Carbon, Atlassian Design System, Material 3, Shopify Polaris, Tailwind, Adobe Spectrum. Her biri için ÖLÇ: kaç ton kademesi (9? 10? 12?), nasıl adlandırılıyor, hangi kademe hangi işe (zemin / kart / kenarlık / hover / metin / vurgu), aydınlık-karanlık eşleme kuralı nedir. ÖZELLİKLE Radix Colors\'ın 12 adımlı ölçeği ve "app background / subtle background / UI element / border / solid / text" rol dağılımını ayrıntılı çıkar — bizim 13 semantik belirtecimiz buna oturacak.' },
  { key: 'karanlik', baslik: 'KARANLIK TEMA — AYRI BİR TASARIM', detay: 'Kurucu "koyu tema ile açık temadaki renkler uygulamaya göre farklılık gösterebilir" dedi. ARAŞTIR: karanlık temada neden doygunluk düşürülür, neden saf siyah (#000) kullanılmaz, yükselti (elevation) neden renkle değil açıklıkla anlatılır, karanlıkta kontrast neden farklı algılanır (halation/parlama), hangi renkler karanlıkta "yanar". Material 3 karanlık tema token eşlemesi ve Apple HIG karanlık mod kuralları birinci kaynak. Somut SAYI ver: aydınlıkta X doygunluk karanlıkta Y oluyor gibi.' },
  { key: 'kurumsal', baslik: 'KURUMSAL SaaS GÖRSEL DİLİ 2026', detay: 'Linear, Stripe, Supabase, Vercel, Notion, Retool gibi 2026 "şık kurumsal" referansları. ÖLÇ: kaç marka rengi kullanıyorlar, vurgu rengini NEREDE kullanıyorlar (bizim kuralımız: yalnız birincil düğme + aktif menü + odak halkası + KPI, sayfada en çok 3 öğe), nötr ölçekleri kaç kademe, kenarlık/gölge stratejisi. Ayrıca FİNANS/ERP yazılımları (QuickBooks, Xero, SAP Fiori, Odoo) nasıl renklendiriyor — bizim kullanıcımız tezgâhtar, ekranda rakam okuyacak.' },
  { key: 'erisilebilirlik', baslik: 'KONTRAST VE ERİŞİLEBİLİRLİK', detay: 'WCAG 2.2 AA/AAA eşikleri (metin 4,5:1, büyük metin 3:1, UI bileşeni ve grafik 3:1), APCA\'nın neden geldiği ve WCAG 2\'den farkı, odak halkası görünürlük kuralları. Renk körlüğü (döteranopi/protanopi, erkeklerin ~%8\'i) için renk TEK BAŞINA anlam taşımama kuralı. Bizim 4 durum rengimiz (başarı/uyarı/hata/bilgi) 8 şema × 2 temada da ayırt edilebilir olmalı — nasıl? Ayrıca 8 şemadan bazılarının durum rengiyle çakışması (yeşil şema + yeşil başarı rengi) nasıl çözülür.' },
]

const arastirma = (await parallel(MERCEKLER.map(m => () =>
  agent(`${ORTAK}

GÖREV — ARAŞTIRMA MERCEĞİ: ${m.baslik}

${m.detay}

YÖNTEM:
1. WebSearch ve WebFetch araçlarını ToolSearch ile yükle (query: "select:WebSearch,WebFetch").
2. BİRİNCİL kaynakları oku (tasarım sistemlerinin kendi dokümanları), blog özetlerini değil.
3. Her bulguda SAYI olsun: kaç kademe, hangi hex, hangi oran. "Modern ve şık" gibi cümle YAZMA.
4. Doğrulayamadığın iddiayı dogrulanamayan listesine yaz, gizleme.
ADIM EKONOMİSİ: en çok 30 araç çağrısı.`,
    { label: 'arastirma:' + m.key, phase: 'Arastirma', effort: 'medium', schema: ARS })
))).filter(Boolean)

log('Araştırma bitti: ' + arastirma.length + '/4 mercek · toplam ' + arastirma.reduce((t, a) => t + (a.bulgular || []).length, 0) + ' bulgu')

phase('Palet')

const palet = await agent(`${ORTAK}

GÖREV — 8 ŞEMA × AYDINLIK/KARANLIK BELİRTEÇ SİSTEMİ (Ertuğ frontend + Bertan mimari).

Araştırma çıktısı (4 mercek):
${JSON.stringify(arastirma, null, 1)}

YAPACAKLARIN:
1. ${KOK}/dotnet/Kuyumcu.Web/wwwroot/css/belirtecler.css dosyasını OKU (520 satır). Bugünkü
   belirteç ADLARINI koru — S-1 bu paleti doğrudan uygulayabilsin, yeniden adlandırma yapma.
2. 13 SEMANTİK BELİRTEÇ setini tanımla (§ 5A'nın istediği), artı açık menü için menuBaslik ve
   menuAktifMetin. Her belirtecin ROLÜ tek cümleyle yazılı olsun (Radix'in rol dağılımı örnek).
3. SEKİZ ŞEMA: Platin Mavi · Zümrüt · Grafit · Bordo · Okyanus · Menekşe · Zeytin · Fildişi.
   Her biri için AYDINLIK ve KARANLIK değerleri AYRI AYRI belirle — karanlık, aydınlığın ters
   çevrilmişi DEĞİL (araştırmadaki doygunluk/yükselti kurallarını uygula).
4. 4 DURUM RENGİ (başarı/uyarı/hata/bilgi) her şema × tema için çakışmasız olsun. Zümrüt ve
   Zeytin şemalarında yeşil başarı rengiyle çakışma riskini AÇIKÇA çöz ve nasıl çözdüğünü yaz.
5. KONTRASTI KENDİN HESAPLA (WCAG 2.2 nispi parlaklık formülü) ve HER çifti raporla:
   gövde metni/zemin, ikincil metin/zemin, birincil düğme metni/düğme, kenarlık/zemin (3:1),
   odak halkası/zemin (3:1), menü metni/menü zemini, her durum rengi/zemini.
   8 şema × 2 tema × en az 7 çift = en az 112 ölçüm. AA GEÇMEYEN DEĞER BIRAKMA — geçmiyorsa
   değeri değiştir ve yeniden ölç. Tabloyu rapora yaz.
6. 3 YAZI BOYUTU (16/18/20 px) için --yazi-govde kademelerini ver.
7. Çıktı: (a) ${SDIZIN}/TEMA-SISTEMI-2026-09-23.md — gerekçe, araştırma kaynakları, kontrast
   tablosu, her şemanın karakteri tek cümle; (b) ${SDIZIN}/belirtecler-8sema.css — S-1'in
   doğrudan alabileceği, :root[data-sema="..."][data-tema="..."] blokları hâlinde SAF CSS.
   Bu dosya ÜRETİME KOPYALANMAZ, S-1 paketi taşıyacak.
8. Ekip varsayılanını KARARA BAĞLA (kurucuya sorma — 23 Eylül talimatı). Planın önerisi Platin
   Mavi; araştırma başka diyorsa gerekçesiyle değiştir.
9. BACKEND NOTU: kullanicilar tablosunda tema/sema/yazi_boyutu kolonu YOK. Tercih kalıcılığı için
   gereken migration ve uç değişikliğini tek paragrafla tarif et (kod yazma).`,
  { label: 'palet:8sema', phase: 'Palet', effort: 'high', schema: {
    type: 'object',
    properties: {
      rapor: { type: 'string' }, css_dosyasi: { type: 'string' },
      belirtec_sayisi: { type: 'number' }, sema_sayisi: { type: 'number' },
      kontrast_olcumu: { type: 'number' }, aa_gecen: { type: 'number' },
      varsayilan_sema: { type: 'string' }, varsayilan_gerekce: { type: 'string' },
      semalar: { type: 'array', items: { type: 'object', properties: {
        ad: { type: 'string' }, karakter: { type: 'string' },
        aydinlik_vurgu: { type: 'string' }, karanlik_vurgu: { type: 'string' },
      }, required: ['ad', 'karakter', 'aydinlik_vurgu', 'karanlik_vurgu'] } },
      backend_notu: { type: 'string' },
      cozulen_catismalar: { type: 'array', items: { type: 'string' } },
    },
    required: ['rapor', 'css_dosyasi', 'belirtec_sayisi', 'sema_sayisi', 'kontrast_olcumu', 'aa_gecen', 'varsayilan_sema', 'varsayilan_gerekce', 'semalar', 'backend_notu', 'cozulen_catismalar'],
  } })

phase('Mockup')

const mockup = await agent(`${ORTAK}

GÖREV — 8 ŞEMA × 2 TEMA MOCKUP (kurucunun GÖRECEĞİ tek sayfa).

Palet çıktısı: ${JSON.stringify(palet, null, 1)}

${SDIZIN}/belirtecler-8sema.css dosyasını OKU ve ONUN değerleriyle çiz.

ÜRET: ${SDIZIN}/mockup-tema-8.html — tek dosya, SAF CSS, script YOK, CDN YOK, url()/@import YOK.
 · 8 şemanın her biri AYDINLIK ve KARANLIK hâliyle → 16 kart.
 · Her kartta AYNI örnek ekran: kenar çubuğu (7 satır) + üst bar + müşteri listesi (3 satır,
   sayılar sağa dayalı tabular) + vezne fişi kartı (VERDİM/ALDIM/FARK) + düğme ailesi (5 varyant)
   + 4 durum bandı (başarı/uyarı/hata/bilgi).
 · Gerçek kuyumcu verisiyle: "1,85 g has", "12.500,00 TL", "22 ayar", tarih tr-TR.
 · Her kartın altında o kombinasyonun EN DÜŞÜK kontrast oranı ve AA damgası yazsın.
 · En üstte üç yazı boyutunun (16/18/20) aynı satırda karşılaştırması.
 · 375 / 768 / 1024 / 1440 px genişliklerde bozulmasın.
 · En üstte tek paragraf: ürün 8 şema × aydınlık/karanlık × 3 boyutla geliyor, kullanıcı
   Ayarlar → Görünüm'den seçiyor, ekibin varsayılanı hangisi ve nedeni.

ESKİ MOCKUP'I SİLME ama raporda "mockup-S1.html ARTIK GEÇERSİZDİR (3 şema, altın vurgu —
plan § 5A'ya aykırı)" diye işaretle.`,
  { label: 'mockup:8sema', phase: 'Mockup', effort: 'medium', schema: {
    type: 'object',
    properties: {
      dosya: { type: 'string' }, kart_sayisi: { type: 'number' },
      en_dusuk_kontrast: { type: 'string' }, aa_gecmeyen: { type: 'number' }, notlar: { type: 'string' },
    },
    required: ['dosya', 'kart_sayisi', 'en_dusuk_kontrast', 'aa_gecmeyen', 'notlar'],
  } })

log('Tema sistemi bitti: ' + (palet && palet.sema_sayisi) + ' şema · ' + (palet && palet.aa_gecen) + '/' + (palet && palet.kontrast_olcumu) + ' AA · mockup ' + (mockup && mockup.kart_sayisi) + ' kart')
return { arastirma, palet, mockup }
