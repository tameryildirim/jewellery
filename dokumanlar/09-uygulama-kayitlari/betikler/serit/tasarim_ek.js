// ═══ TASARIM HAKEMİ (kurucu 25.09 14:05: "front end'de de çalışsın, kontrol etsin, adam gibi tasarım istiyorum") ═══
const TASARIM = {
  type: 'object',
  properties: {
    paket_no: { type: 'string' },
    karar: { type: 'string', enum: ['GECTI', 'KALDI'] },
    gorulen_goruntu: { type: 'number', description: 'Read ile GERCEKTEN acilip bakilan ekran goruntusu sayisi' },
    olcumler: { type: 'array', items: { type: 'object', properties: { arac: { type: 'string' }, sonuc: { type: 'string' } }, required: ['arac', 'sonuc'] } },
    katalog: { type: 'array', items: { type: 'object', properties: { kod: { type: 'string' }, durum: { type: 'string', enum: ['UYGUN', 'IHLAL', 'KAPSAM_DISI'] }, kanit: { type: 'string' } }, required: ['kod', 'durum', 'kanit'] } },
    bulgular: { type: 'array', items: { type: 'object', properties: { baslik: { type: 'string' }, ciddiyet: { type: 'string', enum: ['KRITIK', 'YUKSEK', 'ORTA', 'DUSUK'] }, kanit: { type: 'string' }, oneri: { type: 'string' } }, required: ['baslik', 'ciddiyet', 'kanit'] } },
  },
  required: ['paket_no', 'karar', 'gorulen_goruntu', 'olcumler', 'katalog', 'bulgular'],
}
const tasarimPrompt = (p) => `${KURALLAR}
GÖREV — TASARIM HAKEMİ (kurucunun gözü; Opus). Paket ${p.no} "${p.ad}". Kurucu: "adam gibi tasarım istiyorum … bu kadar basit konular için bile test yapacaksam rezil olurum". Hedef kitle ilkokul/lise mezunu tezgâhtar; müşteri "çok karışık, Pusula sade" dedi.
⚠ Uygulayıcının ve doğrulayıcının raporlarını OKUMA. Yalnız: paketin tanımı ve koşulları (aşağıda), çalışan ürün, tuval, katalog.
ADIMLAR: (1) Paketin izolasyon satırındaki portlarda API'yi kendin başlat (dotnet run --no-build), kendi test kiracını gerçekçi veriyle tohumla (uzun adlar, 6-7 haneli ve eksi tutarlar, dolu tablolar), vekille parolasız aç (betikler/inceleme-vekili.py). Tarayıcı bölmesini KULLANMA — ${KOK}/dokumanlar/09-uygulama-kayitlari/betikler/tur/ araçları (BENIOKU.md; TUR_OUT ve TUR_PROFIL mutlak ${SCRATCH} altı, TUR_CDP kendi aralığın): tur.mjs, tutarlilik.mjs, dugme-olc.mjs, arama-dene.mjs — paketin rotalarında KOŞ. (2) SENARYO: ekranı tezgâhtar gibi KULLAN — CDP (betikler/tur/cdp.mjs) ile formu doldur: ürün ekle, ödeme ekle/çıkar, isteğe bağlı "+ … ekle"leri aç, yasal eşiği tetikle, hata üret; her durumda 1440×900 BASİT ve PRO + 375 ekran görüntüsü al. (3) En az 12 görüntüyü Read ile GÖR; tuval ${KOK}/dokumanlar/09-uygulama-kayitlari/ham/TUVAL-png/*.png ile karşılaştır. (4) Kurucu şikâyet kataloğu (${KOK}/dokumanlar/09-uygulama-kayitlari/ham/KURUCU-SIKAYET-KATALOGU.md — K1..K11 ve 25.09 tablosu K12..K23) paketin ekranlarında TEK TEK → 'katalog'. (5) Kurallar: açılışta yalnız zorunlular, geçmiş kapalı akordeon, BASİT↔PRO aynı ölçü, tek birincil düğme, açıklama/teknik metin satırı yok, para biçimi tabloda ve kutuda aynı, düğme yazısı ortada ve zeminden ayırt edilir, katman butona bağlı, jargon/İngilizce/ham kod yok, tuvaldeki tasarım notu kopyalanmamış.
KARAR: kurucuyu demoda utandıracak HER şey KRITIK. KRITIK ya da YUKSEK varsa KALDI. Kapsam dışı ama aynı ekranda gördüğün kusur "KAPSAM DIŞI:" önekiyle yazılır (paketi düşürmez). gorulen_goruntu < 12 ise KALDI. İş bitince kendi API sürecini ve test kiracını temizle.
PAKET TANIMI VE KOŞULLAR:
${(p.kosullar || []).map(k => '- ' + k).join('\n')}`
