export const meta = { name: 'kuyumcu-hazirlik-kurulu', description: 'Satisa hazirlik kurulu: 7 mercek (Eray, Kopru, Askin, Mehmet, Bertan, Mustafa, Ramazan-ajan) acimasizca degerlendirir, muhur HAZIR/DEGIL + engel listesi uretir (salt okunur)', phases: [{ title: 'Değerlendirme' }, { title: 'Mühür', model: 'opus' }] }
const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery'
const TUR = args.tur || 1
const KANIT = args.kanitlar || {}
const ONCEKI = args.onceki || ''
const PARTI = args.parti || 4
const DEG = { type: 'object', properties: { mercek: { type: 'string' }, puan_0_10: { type: 'integer' }, hazir: { type: 'boolean' }, engeller: { type: 'array', items: { type: 'object', properties: { baslik: { type: 'string' }, kanit: { type: 'string' }, ciddiyet: { type: 'string' }, cozum_paketi: { type: 'string' }, tahmin: { type: 'string' } }, required: ['baslik', 'kanit', 'ciddiyet', 'cozum_paketi'] } }, guclu_yanlar: { type: 'array', items: { type: 'string' } }, gercekci_ornek: { type: 'string' }, notlar: { type: 'array', items: { type: 'string' } } }, required: ['mercek', 'puan_0_10', 'hazir', 'engeller', 'guclu_yanlar', 'gercekci_ornek', 'notlar'] }
const ORTAK = `Türkçe yaz. Salt okunur: dosya değiştirme YOK; derleme/test KOŞMA (kanıt dosyalarını oku; gerekirse 'dotnet test --list-tests' gibi hafif komutlar serbest). Kaynak koda bak (grep/sed -n ile, kısa). Kurucu talimatı: "acımasızca değerlendirin; satışa hazır değilse yeniden test ve yeniden değerlendirme". Ölçüt: Türkiye'de perakende kuyumcu dükkânına PARA KARŞILIĞI satılabilir mi? Rakipler: Dezy, KuyumHub, Mikro/Logo kuyumcu modülleri. Her engel için dosya:satır ya da ekran/rapor kanıtı; kanıtsız engel yazma (Anayasa md.1); emin değilsen 'DOĞRULANMALI' de. Her engel için çözüm paketi öner (mevcut paket numarası ya da yeni). ADIM EKONOMİSİ: ≤ 20 araç çağrısı. ÇIKTI SINIRI: ≤ 2.500 kelime; en fazla 12 engel; her dizge ≤ 500 karakter; StructuredOutput tek seferde.
Proje: ${KOK} (.NET 8 API + Blazor WASM + MySQL; 42 rota / 42 .razor sayfa — P-02 ile taze ölçüldü, "27" bayattı). KANIT DOSYALARI: ${JSON.stringify(KANIT)}. ${ONCEKI ? 'ÖNCEKİ TUR KARARI: ' + ONCEKI + ' — önceki engellerin kapanıp kapanmadığını AYRICA değerlendir.' : ''} Bu tur: ${TUR}.`
phase('Değerlendirme')
const MERCEKLER = [
  { ad: 'Eray — 08 Satış: bir kuyumcu demo görünce satın alır mı? İlk 10 dakika deneyimi, POS hızı, fiyat/has görünürlüğü, düzenleme rahatlığı, rakip parite, fiyat/değer; gerçekçi bir satış demosu senaryosu yaz', model: 'opus' },
  { ad: 'Köprü — 07 Teslimat: kurulum/onboarding (yeni kiracı açılışı, ürün/cari aktarımı, ilk gün), kılavuz/yardım, yedekleme-geri yükleme, destek süreci, güncelleme/yayın (deploy/nginx), veri göçü', model: 'opus' },
  { ad: 'Aşkın — 04 QA: test kanıtları (sayı, tür dengesi, mutasyon kanıtları, kararsızlık), tarayıcı turu bulguları, düzenlenemeyen veri girişi var mı (her sayfa), hata gösterimi, boş/yükleniyor durumları; MOBİL PARİTE (P-18 sayacı MOBIL_YATAY_KAYDIRMA): son QA turunun her rotası için mobil_scroll_width ≤ 375 mi — 375’i aşan TEK rota bile bu merceği hazir=false yapar (BK-24, örnekleme kabul edilmez); ayrıca karanlık temada okunamayan vurgu (B-32) ve yatayda kayan matriste yapışkan sütun (B-30)', model: 'sonnet' },
  { ad: 'Mehmet — 06 Güvenlik ve uyum (veto): kiracı izolasyonu, yetki, MFA, denetim izi, tevsik/MASAK/KVKK, KDV özel matrah, e-Arşiv zorunluluğu, sır yönetimi, yedek/şifreleme; canlı DB\'deki bilinen sızıntı satırları', model: 'sonnet' },
  { ad: 'Bertan — 02 Baş Mimar: teknik borç, migration sağlığı (sıfır baytlık dosyalar 8, .mutorj artığı 16 — tabanlar: ham/olcum-2026-09-18.json), ölçek (TB-19), tek yazma yolu, ikizlenmiş mekanizmalar, kalan paketlerin sevk sırası', model: 'opus' },
  { ad: 'Mustafa — 09 FinOps: barındırma/işletme maliyeti (MySQL, sunucu, yedek), lisans, kiracı başı maliyet, fiyatlandırma önerisi; kurucunun tasarruf hedefi (rakip programları memleketten silecek fiyat) dürüstçe', model: 'opus' },
  { ad: 'Ramazan — 01 Strateji: rakip parite matrisi (PAZAR-ARASTIRMASI-2026-09-16.md), satışa hazırlık asgarisi, sürümleme (1.0 kapsamı vs sonrası), takvim gerçekçiliği', model: 'opus' },
]
const sonuclar = []
for (let i = 0; i < MERCEKLER.length; i += PARTI) {
  const parti = MERCEKLER.slice(i, i + PARTI)
  const s = await parallel(parti.map(m => () => agent(`${ORTAK}\nSEN: ${m.ad}. Yalnız kendi merceğinden; mercek alanına adını yaz.`, { label: 'kurul:' + m.ad.split(' ')[0], phase: 'Değerlendirme', model: m.model, effort: 'medium', schema: DEG })))
  sonuclar.push(...s.filter(Boolean))
  log(`kurul: ${sonuclar.length}/${MERCEKLER.length} mercek`)
}
phase('Mühür')
const MUHUR = { type: 'object', properties: { karar: { type: 'string' }, ortalama_puan: { type: 'number' }, engeller_oncelikli: { type: 'array', items: { type: 'object', properties: { baslik: { type: 'string' }, mercekler: { type: 'array', items: { type: 'string' } }, ciddiyet: { type: 'string' }, cozum_paketi: { type: 'string' }, dalga: { type: 'string' } }, required: ['baslik', 'mercekler', 'ciddiyet', 'cozum_paketi', 'dalga'] } }, satisa_hazirlik_asgarisi: { type: 'array', items: { type: 'string' } }, sonraki_adimlar: { type: 'array', items: { type: 'string' } }, ozet: { type: 'string' } }, required: ['karar', 'ortalama_puan', 'engeller_oncelikli', 'satisa_hazirlik_asgarisi', 'sonraki_adimlar', 'ozet'] }
const muhur = await agent(`Türkçe yaz. Araç çağrısı gerekmez (≤ 3). Tamer Bey adına MÜHÜR (tur ${TUR}): 7 merceğin değerlendirmesini birleştir. Karar: "HAZIR" yalnız Mehmet dâhil her mercek hazir=true ve ortalama ≥ 8 ise; aksi hâlde "HAZIR DEĞİL". Engelleri tekilleştir, ciddiyete ve mercek sayısına göre sırala, her birine çözüm paketi + dalga ata. ÇIKTI SINIRI: ≤ 2.500 kelime; en fazla 20 engel; her dizge ≤ 500 karakter; StructuredOutput tek seferde.
DEĞERLENDİRMELER: ${JSON.stringify(sonuclar).slice(0, 80000)}`, { label: 'muhur', phase: 'Mühür', model: 'opus', effort: 'medium', schema: MUHUR })
return { tur: TUR, degerlendirmeler: sonuclar, muhur }
