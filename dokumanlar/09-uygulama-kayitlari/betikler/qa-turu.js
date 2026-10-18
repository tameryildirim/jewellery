export const meta = { name: 'kuyumcu-qa-turu', description: '42/42 rota tarayici QA turu (rota listesi @page direktiflerinden URETILIR, elle yazilmaz) (uygulama ici tarayici bolmesi, 5199 QA sunucusu): fikstur kiracisi API ile kurulur, 4 sayfa grubu sirayla tiklanir (duzenlenebilirlik + kullanici dostu + konsol/ag hatalari), ozet cizelgeye yazilir, fikstur silinir', phases: [{ title: 'Fikstür' }, { title: 'Tur' }, { title: 'Özet', model: 'opus' }] }

// ═══ P-43-ORTALAMA-BASLA — İKİ DOSYADA BİREBİR AYNI BLOK (elle ayrıştırma) ═══
// Bu blok `betikler/qa-turu.js` ile `betikler/qa-ozet.mjs` içinde BAYT BAYT
// AYNIDIR ve `duman-testi.sh::test_ortalama_blogu_iki_dosyada_ayni` bunu ölçer.
// Neden kopya: qa-turu.js koşum betiği sandbox'ında çalışır (gövdesi bir
// fonksiyona sarılır, `return` içerir) → node ile ÇALIŞTIRILAMAZ, `import`/
// `require` YOKTUR. CLI bu yüzden qa-ozet.mjs'de; sapma raporda yazılıdır.
//
// TUR 8 GERÇEĞİ (ölçüldü, iddia değil): yayın anlık görüntüsü eksik dosya
// servis ettiği için grup 1'in ALTI rotası (/cikti/{Tur}/{Id}, /kasalar,
// /sayim, /banka, /pos/tahsilatlar, /yetkiler) HİÇ önyüklenmedi ("Failed to
// start platform") ve hepsi 1/1 puanlandı. O altı 1'lik puan ORTALAMAYA
// KARIŞTI: tur 8 ortalaması 3,17 / 2,74 çıktı. Altı rota çıkarılınca gerçek
// ortalama 3,50 / 3,00. Yani "tur 7'den düştü" iddiası GÜRÜLTÜYDÜ.
//
// KURAL: bir grubun TÜM rotaları AYNI ÖNYÜKLEME hatasıyla ENGELLENDİ dönerse
// o grup PUAN ORTALAMASINA KATILMAZ; ayrı bir "ölçülemedi" kalemi olur.
// BULGULAR KAYBOLMAZ — bulgu/kritik/düzenlenemeyen sayımları TÜM sayfalardan
// yapılmaya devam eder; ayrılan yalnız ortalamadır.
//
// ⚠ Ölçülemeyen rota "temiz" demek DEĞİLDİR (Anayasa md.1). Ortalamadan
//   çıkarmak onu aklamaz; aracın bozuk olduğunu söyler. O rotalar duman kapısı
//   (duman-testi.sh) yeşile döndükten sonra YENİDEN taranmalıdır.

/** Tarayıcı konsolunda "uygulama hiç açılmadı" anlamına gelen imzalar. */
const ONYUKLEME_IMZALARI = [
  'Failed to start platform',
  'mono_download_assets',
  'integrity checks failed',
  '_framework',
]

/** Sayfa gerçekten ENGELLENDİ mi (durum alanı) — büyük/küçük harf duyarsız. */
function P43_engellendiMi(p) { return /ENGELLEN/i.test(String((p && p.durum) || '')) }

/** Sayfanın konsol/ağ kanıtında bulunan önyükleme imzaları. */
function P43_imzalar(p) {
  const m = String((p && p.konsol_ag) || '').toLowerCase()
  return ONYUKLEME_IMZALARI.filter(i => m.includes(i.toLowerCase()))
}

/**
 * Grupları ikiye ayırır. Bir grup ancak (a) sayfası varsa, (b) sayfalarının
 * HEPSİ ENGELLENDİ ise ve (c) hepsinde ORTAK bir önyükleme imzası varsa
 * "ölçülemedi" sayılır. Tek tük engellenen sayfa grubu diskalifiye ETMEZ
 * (tur 8 grup 8: 4 sayfanın 1'i engelliydi → o grup ortalamada KALIR).
 */
function gruplariAyir(sonuclar) {
  const olculemedi = []
  const olculebilen = []
  for (const g of (sonuclar || [])) {
    const sayfalar = g.sayfalar || []
    const hepsiEngelli = sayfalar.length > 0 && sayfalar.every(P43_engellendiMi)
    const ortak = hepsiEngelli
      ? ONYUKLEME_IMZALARI.filter(i => sayfalar.every(p => P43_imzalar(p).includes(i)))
      : []
    if (hepsiEngelli && ortak.length > 0) {
      olculemedi.push({ grup: g.grup, sayfa_sayisi: sayfalar.length, imza: ortak[0], rotalar: sayfalar.map(p => p.rota) })
    } else {
      olculebilen.push(g)
    }
  }
  return { olculemedi, olculebilen }
}

/** Türkçe ondalık (virgül) — rapor metinleriyle aynı biçim. */
function tr2(x) { return (x === null || x === undefined) ? '—' : Number(x).toFixed(2).replace('.', ',') }

function P43_ortala(sayfalar, f) {
  const d = sayfalar.map(f).filter(x => typeof x === 'number')
  return d.length ? +(d.reduce((a, b) => a + b, 0) / d.length).toFixed(2) : null
}

/**
 * İki ortalamayı birden hesaplar. Sayımlar (bulgu/kritik/düzenlenemeyen)
 * HER ZAMAN tüm sayfalardan yapılır; ayrılan yalnız puan ortalamasıdır.
 */
function ortalamaButunlugu(sonuclar) {
  const { olculemedi, olculebilen } = gruplariAyir(sonuclar)
  const tum = (sonuclar || []).flatMap(g => g.sayfalar || [])
  const olc = olculebilen.flatMap(g => g.sayfalar || [])
  return {
    olculemeyen_gruplar: olculemedi,
    olculemeyen_rota_sayisi: olculemedi.reduce((t, g) => t + g.sayfa_sayisi, 0),
    tum_sayfa_sayisi: tum.length,
    olculebilen_sayfa_sayisi: olc.length,
    ortalama_gorsel_tum: P43_ortala(tum, p => p.gorsel_puan_1_5),
    ortalama_kullanici_dostu_tum: P43_ortala(tum, p => p.kullanici_dostu_puan_1_5),
    ortalama_gorsel_olculebilen: P43_ortala(olc, p => p.gorsel_puan_1_5),
    ortalama_kullanici_dostu_olculebilen: P43_ortala(olc, p => p.kullanici_dostu_puan_1_5),
  }
}

/** --ozet çıktısının metni (saf: yan etkisiz). */
function ozetMetni(tur, sonuclar) {
  const b = ortalamaButunlugu(sonuclar)
  const s = []
  s.push(`QA ORTALAMA BÜTÜNLÜĞÜ (tur ${tur})`)
  s.push(`  tüm rotalar         : n=${b.tum_sayfa_sayisi} · görsel ${tr2(b.ortalama_gorsel_tum)}/5 · kullanıcı dostu ${tr2(b.ortalama_kullanici_dostu_tum)}/5`)
  s.push(`  ölçülebilen rotalar : n=${b.olculebilen_sayfa_sayisi} · görsel ${tr2(b.ortalama_gorsel_olculebilen)}/5 · kullanıcı dostu ${tr2(b.ortalama_kullanici_dostu_olculebilen)}/5`)
  if (b.olculemeyen_gruplar.length === 0) {
    s.push('  ölçülemedi: YOK — iki ortalama aynıdır.')
  } else {
    for (const g of b.olculemeyen_gruplar) {
      s.push(`  ölçülemedi (ortalamaya KATILMADI): grup ${g.grup} · ${g.sayfa_sayisi} rota · imza "${g.imza}"`)
      for (const r of g.rotalar) s.push(`      - ${r}`)
    }
    s.push('  ⚠ ölçülemeyen rotaların BULGULARI kaybolmadı; yalnız PUAN ortalamasından ayrıldı.')
    s.push('  ⚠ "ölçülemedi" TEMİZ demek değildir — duman kapısı yeşile dönünce bu rotalar YENİDEN taranır.')
  }
  return s.join('\n')
}
// ═══ P-43-ORTALAMA-BITIS ═════════════════════════════════════════════════════

const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery'
const SCRATCH = '/private/tmp/claude-501/-Users-ramazanakkocan-Desktop-ByGatech-projeler-jewellery/f8597a15-bda0-4765-a57f-77f2b6380580/scratchpad'
const URL = args.url || 'http://localhost:5199'
const TUR = args.tur || 1
// ⚠ KOŞUM BETİĞİ SANDBOX'I (2026-09-21'de ölçüldü, varsayılmadı): Date.now() / new Date()
//   YASAK (resume önbelleğini kırar) ve require() DE YOK (ReferenceError). Tur 7 iki kez
//   bu yüzden milisaniyeler içinde düştü. Bu yüzden dışarıdan ölçülen her şey args ile gelir:
//     args.t0          → turun başlangıcı (epoch sn; `date +%s`)
//     args.rotalar     → @page listesi (`grep -rhE '^@page' Pages`)
//     args.icloud_acik → K-20 iCloud eşitlemesi açık mı (taban geçerliliği, BK-42)
//   Süre ölçümü (BK-39) betikten değil, Bash'i olan 'krono' ajanından yazılır — mekanizma
//   otomatik kalır, kimse elle hatırlamak zorunda değildir.
const KRONO_T0 = Number(args.t0)
if (!KRONO_T0) throw new Error('args.t0 (epoch sn) verilmedi — süre ölçülemez, tur BAŞLAMADI (BK-39).')
// K-20 (iCloud eşitlemesi) ölçüm tabanını geçersiz kılar (BK-42). ÖLÇÜLMEDİYSE KÖTÜMSER.
// ⚠ Tanım BURADA: 2026-09-21'de tur 7'nin 10 ajanı (84 dk, 1,8 M jeton) bitmişken betik
//   son satırda 'ICLOUD_ACIK is not defined' ile düştü — tanım, yerine 'krono' ajanı konan
//   bloğun içinde kalmıştı. Sonuç önbellekten kurtarıldı; ders bu satırda duruyor.
const ICLOUD_ACIK = args.icloud_acik !== false

// ═══ B-1 · ROTA KAPSAMI: 42/42, FAIL-CLOSED (PLAN § 2 P-02) ═══════════════════
// TUR 6 GERCEGI: 42 rotanin 39'u tarandi, 3'u SESSIZCE atlandi — hepsi
// parametreli: /gider-pusulalari/{Id}/belge · /siparisler/{Id} ·
// /banka/ekstre/{IthalatId}. Sebep: grup listesi ELLE yaziliydi ve yeni sayfa
// eklendikce geride kaliyordu. Artik liste @page direktiflerinden URETILIR:
// yeni bir sayfa eklendigi anda tura girer, kimse listeyi guncellemeyi
// unutamaz. Kapsam 42 degilse TUR BASLAMAZ (fail-closed, Anayasa md.1).
const ROTA_KAPSAM_BEKLENEN = 53   // D1-19: /kvkk (52 → 53) · PAKET EKSIK-KUCUK: /vitrin/ekran (51 → 52) · PAKET EKSIK-B1-SATINALMA: /satin-alma + /irsaliyeler (49 → 51) · PAKET AUTH-SIFIRLA: /sifre-sifirla (48 → 49) · PAKET YASAL-1: /yasal-sinirlar (47 → 48) · PAKET S1-d: /ekranlar eklendi (46 → 47) · P-20: /hurda (45 → 46)
// Liste hâlâ ELLE YAZILMAZ: orkestratör turu başlatmadan önce
//   grep -rhE '^@page' dotnet/Kuyumcu.Web/Pages --include='*.razor'
// çıktısını args.rotalar olarak geçirir; aşağıdaki sayı kapısı fail-closed durur.
const SABLONLAR = (Array.isArray(args.rotalar) ? args.rotalar : [])
  .map(x => String(x).replace(/^@page\s+"/, '').replace(/"\s*$/, '').trim())
  .filter(Boolean)
// ── TUR, AĞACI DEĞİL YAYINI ÖLÇER (2026-09-21) ───────────────────────────────
// Ölçülen şey 5199'daki YAYIN ANLIK GÖRÜNTÜSÜDÜR; ağaç turun ortasında ilerleyip
// yeni sayfa kazanabilir (P-19 /gider'i ekledi: 44 → 45) ve o sayfa YAYINDA YOKTUR.
// Sabit sayıyı körü körüne dayatmak, bitmiş bir turu çöpe attırıyordu. Kural:
// yayın sayısı ağaçtan FARKLIYSA gerekçe ZORUNLUDUR, fark günlüğe ve tur sonucuna
// yazılır. Gerekçesiz sapma = eskisi gibi fail-closed. Sessiz atlama yine imkânsız:
// aşağıdaki ATLANAN kapısı listedeki HER rotanın bir gruba girmesini şart koşar.
const YAYIN_BEKLENEN = Number(args.yayin_rota_sayisi) || ROTA_KAPSAM_BEKLENEN
const KAPSAM_SAPMASI = YAYIN_BEKLENEN !== ROTA_KAPSAM_BEKLENEN
if (KAPSAM_SAPMASI && !String(args.yayin_gerekce || '').trim()) {
  throw new Error(`rota kapsamı: yayın ${YAYIN_BEKLENEN}, ağaç ${ROTA_KAPSAM_BEKLENEN} — ` +
    `fark GEREKÇESİZ. args.yayin_gerekce ile hangi yayının ölçüldüğünü yazın. Tur BAŞLAMADI.`)
}
if (KAPSAM_SAPMASI) {
  log(`⚠ KAPSAM SAPMASI: tur ${YAYIN_BEKLENEN} rotalı YAYINI ölçüyor, ağaçta ${ROTA_KAPSAM_BEKLENEN} rota var` +
    ` — gerekçe: ${String(args.yayin_gerekce).trim()}`)
}
if (SABLONLAR.length !== YAYIN_BEKLENEN) {
  throw new Error(`rota kapsamı: ${SABLONLAR.length} @page bulundu, ${YAYIN_BEKLENEN} bekleniyordu. ` +
    `Sayfa eklendi/silindiyse ROTA_KAPSAM_BEKLENEN, dotnet/denetim.sh (P02-1) ve ` +
    `Kuyumcu.Web/Layout/RotaBasliklari.cs birlikte güncellenir. Tur BAŞLAMADI.`)
}
// args.gruplar VERILMISSE sirasi korunur, EKSIKLERI otomatik eklenir (sessiz
// atlama imkansiz). Verilmemisse 6'sarli gruplara bolunur.
const ELLE = Array.isArray(args.gruplar) ? args.gruplar.map(g => g.slice()) : []
const KAPSANAN = new Set(ELLE.flat())
const EKSIK = SABLONLAR.filter(x => !KAPSANAN.has(x))
const GRUPLAR = ELLE.length ? ELLE.slice() : []
for (let i = 0; i < EKSIK.length; i += 6) GRUPLAR.push(EKSIK.slice(i, i + 6))
const TUM = new Set(GRUPLAR.flat())
const ATLANAN = SABLONLAR.filter(x => !TUM.has(x))
if (ATLANAN.length) throw new Error('rota kapsamı eksik, tur BAŞLAMADI: ' + ATLANAN.join(', '))
log(`rota kapsamı: ${SABLONLAR.length}/${ROTA_KAPSAM_BEKLENEN} · ${GRUPLAR.length} grup · elle verilen ${ELLE.flat().length}, otomatik eklenen ${EKSIK.length}`)
const CIZELGE_1 = `${KOK}/dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16.md`
const CIZELGE = TUR > 1 ? `${KOK}/dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16-tur${TUR}.md` : CIZELGE_1
const ONCEKI = args.onceki_bulgular || (TUR > 1 ? `${KOK}/dokumanlar/09-uygulama-kayitlari/ham/qa-turu-${TUR - 1}.json` : '')
const FIKSTUR = args.fikstur_dosyasi || `${SCRATCH}/qa-fikstur.json`
const TEMIZLE = args.temizle === true
const ORTAK = `Türkçe yaz. Anayasa md.1: hata gizleme yok, "oldu" ancak kanıtla. KIRMIZI ÇİZGİLER: proje kaynak kodunu DEĞİŞTİRME; git komutu yok; COMMIT yok; sır/parola/token değerini ASLA çıktına veya rapora yazma (dosyada kalır); 5198 portuna dokunma; derleme/test koşma (bu tur salt kullanım testidir). Tarayıcı araçları: ToolSearch ile "select:mcp__Claude_Browser__navigate,mcp__Claude_Browser__read_page,mcp__Claude_Browser__find,mcp__Claude_Browser__computer,mcp__Claude_Browser__form_input,mcp__Claude_Browser__javascript_tool,mcp__Claude_Browser__read_console_messages,mcp__Claude_Browser__read_network_requests,mcp__Claude_Browser__get_page_text,mcp__Claude_Browser__resize_window,mcp__Claude_Browser__tabs_context,mcp__Claude_Browser__browser_batch" yükle (tek çağrıda). Bölme kapalıysa navigate ile ${URL} açılır. Ekran okumada read_page/find/get_page_text tercih et; ekran görüntüsünü (computer screenshot, scale 0.5) sayfa başına en fazla 2 kez al (görsel kalite yargısı için). ADIM EKONOMİSİ: browser_batch ile adımları birleştir.`
const OTURUM = `OTURUM AÇMA — ÖYKÜNME KAPALI, GERÇEK GİRİŞ (B-1): ÖNCE ${FIKSTUR} dosyasındaki e-posta ve şifre ile ${URL}/giris EKRANINDAN giriş yap (form_input ile alanları doldur, Giriş düğmesine tıkla, üst barda kullanıcı adı görünene dek bekle). Giriş ekranının kendisi de bir rotadır ve bu yolla GERÇEKTEN test edilmiş olur. Parola değerini rapora YAZMA. Ekran girişi başarısız olursa bu BİR BULGUDUR (KRİTİK, /giris): bulgu olarak yaz, genel_notlar'a 'ÖYKÜNMELİ TUR: ekran girişi çalışmadı, token enjeksiyonuna düşüldü' satırını ekle ve ancak ondan sonra yedek yola geç. YEDEK YOL (parola yazılmaz): ${FIKSTUR} dosyasını Read ile oku (accessToken, refreshToken, anahtarlar). javascript_tool ile localStorage'a yaz: localStorage.setItem('<accessAnahtar>', accessToken); localStorage.setItem('<refreshAnahtar>', refreshToken); sonra navigate ${URL}/ → üst barda kullanıcı adı görünmeli (read_page ile doğrula; görünmüyorsa 401 ağ isteği var mı bak; token süresi dolmuşsa curl ile POST ${URL}/api/v1/auth/yenile {refreshToken} ile yenile, dosyayı güncelle, tekrar dene). Token değerlerini rapora YAZMA.`
phase('Fikstür')
const FIK = { type: 'object', properties: { tenant_id: { type: 'string' }, isletme_adi: { type: 'string' }, dosya: { type: 'string' }, tohum: { type: 'array', items: { type: 'string' } }, sorunlar: { type: 'array', items: { type: 'string' } } }, required: ['tenant_id', 'isletme_adi', 'dosya', 'tohum', 'sorunlar'] }
const HAZIR = args.fikstur_hazir
const fikstur = HAZIR ? (log('fikstür hazır kabul edildi: ' + HAZIR.isletme_adi + ' (' + HAZIR.tenant_id + ') — ajan atlandı'), { tenant_id: HAZIR.tenant_id, isletme_adi: HAZIR.isletme_adi, dosya: FIKSTUR, tohum: HAZIR.tohum || ['(önceki koşumda kuruldu)'], sorunlar: HAZIR.sorunlar || [] }) : await agent(`${ORTAK}
GÖREV — QA FİKSTÜRÜ (API ile, tarayıcı gerekmez). ÖNCE: ${FIKSTUR} dosyası VARSA ve içindeki refreshToken ile POST ${URL}/api/v1/auth/yenile 200 dönüyorsa (dosyayı yeni tokenlarla güncelle) fikstür YENİDEN KULLANILIR — yeni kiracı AÇMA, tohum verisini yalnız eksikse tamamla ve doğrudan (5)e geç. İKİNCİ ŞANS (yenileme 401 ise — refresh token döndürülmüş/iptal edilmiş olabilir): dosyadaki email + sifre ile curl POST ${URL}/api/v1/auth/giris dene (değerleri ekrana basma); 200 dönerse yeni access/refresh ile dosyayı güncelle, AYNI kiracıyı yeniden kullan (kurucuya gerçekçi örnekler için tek zengin fikstür kiracısı gerekir) ve (5)e geç. Dosya yoksa ya da giriş de başarısızsa: (1) ${KOK}/dotnet/Kuyumcu.Api/Modules/Auth/AuthModulu.cs → KayitDto alanları ve şifre kuralı; ${KOK}/dotnet/Kuyumcu.Web/Servisler/OturumServisi.cs → localStorage anahtar sabitleri (AccessAnahtar/RefreshAnahtar değerleri). (2) curl ile POST ${URL}/api/v1/auth/kayit: işletme adı "QA-TEST-${TUR} Kuyumculuk", ad/soyad "QA Test", e-posta qa-test-${TUR}@bygatech.test, şifre: 'openssl rand -base64 18' ile ÜRET (ekrana basma; kurala uymuyorsa harf+rakam ekle). Yanıttaki tenantId/accessToken/refreshToken'ı ve anahtar adlarını ${FIKSTUR} dosyasına JSON olarak yaz ({tenantId, email, sifre, accessToken, refreshToken, accessAnahtar, refreshAnahtar}); chmod 600. (3) GET ${URL}/api/v1/auth/ben → 200 doğrula. (4) GERÇEKÇİ TOHUM VERİSİ (Modules/*Modulu.cs uçlarından; ADIM EKONOMİSİ ≤ 45 araç çağrısı; kur için /kur/guncel dolana dek 10-20 sn bekle): 2 şube? (varsa tek şube yeter), kasalar: "Ana Kasa" TRY + "Döviz Kasası" USD; 8 ürün (22 ayar burma bilezik 15,20 g; 14 ayar alyans çifti 8,40 g; 22 ayar çeyrek altın; gram altın 24 ayar; 925 gümüş kolye; 18 ayar pırlanta yüzük 0,25 ct sertifikalı; 22 ayar set; hurda 22 ayar 12 g), 5 cari (2 müşteri, 1 toptancı, 1 atölye, 1 kurumsal — vergi no/limit/vade dolu), fiyat listesi (varsayılan, marj %8 + işçilik), 1 kampanya; işlemler: 3 satış (nakit, kart, veresiye), 1 alış (toptancıdan), 1 hurda alış, 1 tahsilat, 1 emanet girişi, 1 banka hareketi, 1 gider pusulası, 1 fatura (KUYUM), 1 sayım açık (kapatma). Her POST'un HTTP kodunu doğrula; başarısızları "sorunlar"a yaz (bunlar da bulgudur). (5) Dönüş: tenant_id, dosya yolu, tohum listesi (ne oluşturuldu), sorunlar. Kimlik değerlerini yazma.`, { label: 'fikstur', phase: 'Fikstür', effort: 'medium', schema: FIK })
if (!fikstur) throw new Error('fikstür kurulamadı')
log(`fikstür: ${fikstur.isletme_adi} · tohum ${fikstur.tohum.length} · sorun ${fikstur.sorunlar.length}`)
phase('Tur')
const BULGU = { type: 'object', properties: { grup: { type: 'integer' }, sayfalar: { type: 'array', items: { type: 'object', properties: { rota: { type: 'string' }, durum: { type: 'string' }, konsol_ag: { type: 'string' }, mobil_scroll_width: { type: 'integer' }, grafik_bbox: { type: 'array', items: { type: 'object', properties: { secici: { type: 'string' }, parca: { type: 'integer' }, en_genis_px: { type: 'number' }, svg_genislik_px: { type: 'number' }, oran_yuzde: { type: 'number' } }, required: ['secici', 'parca', 'en_genis_px', 'svg_genislik_px', 'oran_yuzde'] } }, gorsel_puan_1_5: { type: 'integer' }, kullanici_dostu_puan_1_5: { type: 'integer' }, bulgular: { type: 'array', items: { type: 'object', properties: { baslik: { type: 'string' }, ciddiyet: { type: 'string' }, tur: { type: 'string' }, kanit: { type: 'string' }, oneri: { type: 'string' } }, required: ['baslik', 'ciddiyet', 'tur', 'kanit', 'oneri'] } }, duzenlenemeyen_veri_girisleri: { type: 'array', items: { type: 'string' } }, onceki_bulgular: { type: 'array', items: { type: 'object', properties: { baslik: { type: 'string' }, durum: { type: 'string' }, kanit: { type: 'string' } }, required: ['baslik', 'durum', 'kanit'] } } }, required: ['rota', 'durum', 'konsol_ag', 'mobil_scroll_width', 'gorsel_puan_1_5', 'kullanici_dostu_puan_1_5', 'bulgular', 'duzenlenemeyen_veri_girisleri'] } }, genel_notlar: { type: 'array', items: { type: 'string' } } }, required: ['grup', 'sayfalar', 'genel_notlar'] }
const sonuclar = []
for (let i = 0; i < GRUPLAR.length; i++) {
  const g = GRUPLAR[i]
  const s = await agent(`${ORTAK}
${OTURUM}
GÖREV — TARAYICI TURU grup ${i + 1}/${GRUPLAR.length}: şu rotaları sırayla tıklayarak test et: ${g.join(', ')}. ${ONCEKI ? `ÖNCEKİ TUR BULGULARI: ${ONCEKI} dosyasını Read ile oku (rotalar → bulgular, duzenlenemeyen); KENDİ rotaların için listelenen HER bulguyu ve her düzenlenemeyen girişi YENİDEN DENE ve sayfanın onceki_bulgular alanına {baslik, durum: KAPANDI/ACIK/KISMEN, kanit} yaz; ACIK/KISMEN olanlar bu turun bulgular listesine de girer (aynı başlık). Yeni bulgular ayrıca yazılır. ` : ''}HER SAYFADA: (a) navigate → 2 sn bekle → read_console_messages(onlyErrors) + read_network_requests (4xx/5xx; 401'ler dâhil) → read_page (başlık, boş durum, tablo, düğmeler); (b) "Yeni/Ekle" akışı: modal/formu aç, alanları gerçekçi değerle doldur (form_input), önce GEÇERSİZ değerle kaydet → doğrulama mesajı çıkıyor mu, sonra geçerli → başarı bandı + satır listede; (c) "Düzenle" akışı: oluşturduğun kaydı düzenle → kaydet → değişiklik listede; DÜZENLE YOKSA "duzenlenemeyen_veri_girisleri"ne yaz (KRİTİK bulgu: kurucu her veri girişinin düzenlenebilir olmasını istiyor); (d) pasife al/sil/storno varsa dene ve geri al; (e) filtre/arama/sayfalama varsa dene; (f) karanlık tema (resize_window colorScheme dark: vurgulu satır/uyarı kutusu OKUNABİLİR mi — açık sarı zemin üstünde açık metin = KRİTİK B-32 gerilemesi) ve dar ekran: resize_window {width:375,height:812} → javascript_tool ile TAM SAYIYI oku: document.documentElement.scrollWidth. **PAKET P-18 SAYACI — MOBIL_YATAY_KAYDIRMA:** bu sayı 375'ten BÜYÜKSE o rota için KRİTİK bulgu yaz (başlık tam olarak 'MOBIL_YATAY_KAYDIRMA: <rota> <sayı> px') ve kanıta taşan öğeyi ekle ([...document.querySelectorAll('body *')].filter(e=>e.getBoundingClientRect().right>375.5 && getComputedStyle(e).position!=='fixed').slice(0,5).map(e=>e.tagName+'.'+e.className)). Sayıyı HER ZAMAN sayfanın mobil_scroll_width alanına yaz — 'sığdı' demek yetmez, ölçüm yazılır (Anayasa md.1). Örnekleme YOK: her rota tek tek ölçülür (BK-24) — sonra resize_window {width:1280,height:800,colorScheme:"light"} (preset desktop DEĞİL: tarayıcı bölmesi dar olabilir, görsel puan 1280 genişlikte verilir (MÜHÜR 5 KOŞUL 12); sayfaya ilk girişte de bu boyutu uygula); (g) **PAKET P-24 SAYACI — GRAFİK KANITI getBBox İLE (B-28):** sayfada <svg> varsa javascript_tool ile ÖLÇ (path dizgesine BAKMA — 'd' özniteliği dolu görünüp genişliği 0 olan pasta dilimi bu üründe bulundu):
  [...document.querySelectorAll('svg')].map(sv=>{const w=sv.getBoundingClientRect().width;const ps=[...sv.querySelectorAll('path,rect,circle')];const en=ps.reduce((m,e)=>{let b=0;try{b=e.getBBox().width}catch(_){}
  return Math.max(m,b)},0);return {secici:(sv.id||sv.getAttribute('class')||'svg'), parca:ps.length, en_genis_px:+en.toFixed(1), svg_genislik_px:+w.toFixed(1), oran_yuzde:+(w?en/w*100:0).toFixed(1)}})
  Sonucu sayfanın grafik_bbox alanına OLDUĞU GİBİ yaz (svg yoksa boş dizi). parca > 0 olduğu hâlde en_genis_px === 0 ise KRİTİK bulgu yaz: başlık tam olarak 'GRAFIK_CIZILMIYOR: <rota> <secici> 0 px'. Tek kategorili pastada oran_yuzde < 50 ise YÜKSEK bulgu yaz ('tek kategori %100 çizilmiyor'). Ölçümü ATLAMA; sayfa grafik içermiyorsa boş dizi yazılır, 'baktım' demek yetmez (Anayasa md.1); (h) ekran görüntüsü (scale 0.5) ile görsel kalite 1-5 ve kullanıcı dostu 1-5 puanla (gerekçe: tıklama sayısı, sistemin bildiği alanın tekrar sorulması, sessiz etkisizlik, geri bildirim). Her bulgu: baslik, ciddiyet (KRITIK/YUKSEK/ORTA/DUSUK), tur (DUZENLENEBILIRLIK/HATA/KULLANILABILIRLIK/GORSEL/PERFORMANS/ERISILEBILIRLIK), kanit (rota + DOM/konsol/ağ), oneri. Tıklanan hiçbir şeyi atlama; QA-TEST verisi dışında bir şey silme. ADIM EKONOMİSİ: sayfa başına ≤ ${ONCEKI ? 18 : 14} araç çağrısı (browser_batch kullan).`, { label: 'tur:' + (i + 1), phase: 'Tur', effort: 'medium', schema: BULGU })
  if (s) sonuclar.push(s)
  log(`tur: ${sonuclar.length}/${GRUPLAR.length} grup · bulgu ${sonuclar.flatMap(x => x.sayfalar).flatMap(p => p.bulgular).length}`)
}
// ═══ SAYILAR AJANA BIRAKILMAZ, BETİK SAYAR (2026-09-21) ══════════════════════
// TUR 7 GERÇEĞİ: özet ajanı 44 rotayı 41, 236 bulguyu 211, 35 düzenlenemeyen
// girişi 18 saydı. Kritik sayısı doğruydu ama kurucuya giden ANA RAKAMLAR
// yanlıştı ve ben o yanlış rakamı rapor ettim. Kök neden, tur 4'te ısıran hatanın
// aynısı: ajana büyük bir JSON verip "say" demek. Artık sayılar BURADA, veriden
// hesaplanır; ajan yalnız ANLATIR. Ajan kendi saydığını yazarsa betiğin sayısı
// KAZANIR (aşağıda ozet nesnesinin üzerine yazılıyor).
const TUM_SAYFA = sonuclar.flatMap(x => x.sayfalar)
const TUM_BULGU = TUM_SAYFA.flatMap(p => p.bulgular || [])
const KRITIK = TUM_BULGU.filter(b => /KRIT/i.test(String(b.ciddiyet || ''))).length
const DUZ_HAM = TUM_SAYFA.flatMap(p => p.duzenlenemeyen_veri_girisleri || [])
// "tasarım gereği / bilinçli salt okunur" diye GEREKÇESİ YAZILMIŞ olanlar ayrılır:
const GEREKCELI = DUZ_HAM.filter(x => /tasarım gereği|bilinçli|salt okunur olmalı|kritik sayılmadı/i.test(String(x)))
const DUZ_GERCEK = DUZ_HAM.length - GEREKCELI.length
// ═══ B-28 · GRAFİK KANITI SAYILIR (P-24) ════════════════════════════════════
// bUnit `d="M..."` dizgesini görüp "çizildi" diyordu; tarayıcıda dilimin
// genişliği 0'dı. Kanıt artık getBBox genişliğidir ve sayım BETİKTE yapılır.
const TUM_BBOX = TUM_SAYFA.flatMap(p => p.grafik_bbox || [])
const BBOX_OLCULEN = TUM_SAYFA.filter(p => Array.isArray(p.grafik_bbox)).length
const BBOX_CIZILMEYEN = TUM_BBOX.filter(g => (g.parca || 0) > 0 && (g.en_genis_px || 0) === 0).length
const BBOX_DAR = TUM_BBOX.filter(g => (g.en_genis_px || 0) > 0 && (g.oran_yuzde || 0) < 50).length
const ORT = (f) => { const d = TUM_SAYFA.map(f).filter(x => typeof x === 'number'); return d.length ? +(d.reduce((a, b) => a + b, 0) / d.length).toFixed(2) : null }
const P43_HAM = ortalamaButunlugu(sonuclar)
const P43_BUTUNLUK = {
  olculemeyen_gruplar: P43_HAM.olculemeyen_gruplar,
  olculemeyen_rota_sayisi: P43_HAM.olculemeyen_rota_sayisi,
  olculebilen_sayfa_sayisi: P43_HAM.olculebilen_sayfa_sayisi,
  ortalama_gorsel_olculebilen: P43_HAM.ortalama_gorsel_olculebilen,
  ortalama_kullanici_dostu_olculebilen: P43_HAM.ortalama_kullanici_dostu_olculebilen,
}
const OLCUM = {
  sayfa_sayisi: TUM_SAYFA.length, bulgu_sayisi: TUM_BULGU.length, kritik: KRITIK,
  duzenlenemeyen_ham: DUZ_HAM.length, duzenlenemeyen_gerekceli: GEREKCELI.length, duzenlenemeyen_gercek: DUZ_GERCEK,
  ortalama_gorsel: ORT(p => p.gorsel_puan_1_5), ortalama_kullanici_dostu: ORT(p => p.kullanici_dostu_puan_1_5),
  grafik_olculen_sayfa: BBOX_OLCULEN, grafik_svg_sayisi: TUM_BBOX.length,
  grafik_cizilmeyen: BBOX_CIZILMEYEN, grafik_dar_cizilen: BBOX_DAR,
  // ═══ P-43 · ORTALAMA BÜTÜNLÜĞÜ ═══ Tamamı önyükleme hatasıyla ENGELLENDİ
  // dönen grup PUAN ortalamasına katılmaz; iki ortalama da rapora yazılır.
  // ortalama_gorsel / ortalama_kullanici_dostu ESKİ ANLAMINI KORUR (tüm rotalar);
  // yeni alanlar ölçülebilen rotaların ortalamasını ve ayrılan grupları taşır.
  ...P43_BUTUNLUK,
}
log(`ÖLÇÜM (betik saydı, ajan değil): sayfa ${OLCUM.sayfa_sayisi} · bulgu ${OLCUM.bulgu_sayisi} · kritik ${OLCUM.kritik} · düzenlenemeyen ${OLCUM.duzenlenemeyen_gercek} gerçek + ${OLCUM.duzenlenemeyen_gerekceli} gerekçeli · görsel ${OLCUM.ortalama_gorsel} · kullanıcı dostu ${OLCUM.ortalama_kullanici_dostu}` +
  ` · grafik(getBBox): ${OLCUM.grafik_svg_sayisi} svg, çizilmeyen ${OLCUM.grafik_cizilmeyen}, dar ${OLCUM.grafik_dar_cizilen}`)
// P-43: iki ortalama ve ayrılan grup(lar) tur günlüğüne AYRI AYRI basılır.
log(ozetMetni(TUR, sonuclar))
phase('Özet')
const OZET = { type: 'object', properties: { sayfa_sayisi: { type: 'integer' }, bulgu_sayisi: { type: 'integer' }, kritik: { type: 'integer' }, duzenlenemeyen: { type: 'array', items: { type: 'string' } }, en_onemli_10: { type: 'array', items: { type: 'string' } }, ortalama_gorsel: { type: 'number' }, ortalama_kullanici_dostu: { type: 'number' }, temizlik: { type: 'string' }, cizelge_dosyasi: { type: 'string' }, kapanan: { type: 'integer' }, acik_kalan: { type: 'integer' }, yeni_bulgu: { type: 'integer' } }, required: ['sayfa_sayisi', 'bulgu_sayisi', 'kritik', 'duzenlenemeyen', 'en_onemli_10', 'ortalama_gorsel', 'ortalama_kullanici_dostu', 'temizlik', 'cizelge_dosyasi'] }
const ozet = await agent(`Türkçe yaz. ⚠ SAYILAR ZATEN ÖLÇÜLDÜ — KENDİN SAYMA, BUNLARI KULLAN: ${JSON.stringify(OLCUM)}. ⚠ P-43 — İKİ ORTALAMA: ortalama_gorsel / ortalama_kullanici_dostu TÜM rotaların, ortalama_gorsel_olculebilen / ortalama_kullanici_dostu_olculebilen ise ÖLÇÜLEBİLEN rotaların ortalamasıdır. RAPORA İKİSİNİ DE YAZ ve hangisinin ne olduğunu söyle. olculemeyen_gruplar alanı boş değilse ayrı bir 'ÖLÇÜLEMEDİ' bölümü aç: hangi grup, hangi rotalar, hangi önyükleme imzası; o rotaların BULGULARININ KAYBOLMADIĞINI (sayımlara dâhil) ve 'ölçülemedi'nin TEMİZ demek OLMADIĞINI, duman kapısı yeşile dönünce yeniden taranacaklarını yaz. Ölçülemeyen tur varsa önceki turla ortalama KARŞILAŞTIRMASINI 'ölçülebilen' rakamlar üzerinden yap. (Tur 7'de ajan 44 rotayı 41, 236 bulguyu 211, 35 düzenlenemeyen girişi 18 saydı; bu yüzden sayım artık betikte. Anlatı, gruplama ve önceliklendirme SENİN işin; rakamlar DEĞİL. 'duzenlenemeyen' listesine TÜM gerçek eksikleri yaz, kısaltma yapma.) GÖREV — QA TURU ÖZETİ + TEMİZLİK (tur ${TUR}): ${TUR > 1 ? `(0) ${CIZELGE} dosyası YOKSA: ${CIZELGE_1} dosyasının başlığını ve 27 satırlık tablo iskeletini (rota sütunu dolu, sonuç sütunları boş) kopyalayarak oluştur — tur 1 dosyasına DOKUNMA (tarihsel taban). Sonra 'Tur ${TUR - 1} → Tur ${TUR} karşılaştırması' bölümü ekle: kapanan / açık kalan / kısmen bulgu sayıları (grup sonuçlarındaki onceki_bulgular alanından), düzenlenemeyen giriş listesi farkı, ortalama puan farkları, yeni bulgu sayısı. ` : ''}(1) ${CIZELGE} dosyasını oku ve tablo satırlarını grup sonuçlarına göre doldur (Durum: OK/BULGU/HATA; Konsol/ağ; Bulgular kısa), "Çapraz kontroller" kutularını işaretle, "Bulgu özeti" bölümüne tur ${TUR} için: sayılar, KRİTİK/YÜKSEK bulgular (rota + kanıt + öneri), düzenlenemeyen veri girişleri listesi, görsel/kullanıcı dostu ortalamaları, "düzeltme paketine giren maddeler". Dosyayı Edit/Write ile güncelle (yalnız bu dosya). (2) TEMİZLİK ${TEMIZLE ? "(İSTENDİ)" : "(BU TURDA YAPILMAZ — fikstür kiracısı KALICI: sonraki turlar ve kurucuya gerçekçi örnekler için tutulur; \"temizlik\" alanına \"kiracı korundu (tenants sayısı: N)\" yaz ve (2)yi ATLA, yalnız SELECT COUNT(*) FROM tenants sonucunu ver)"}: fikstür kiracısını (tenant_id ${fikstur.tenant_id}, işletme "${fikstur.isletme_adi}") tüm tablolardan sil: MySQL parolası 'P=$(awk -F": " "/^[[:space:]]*MYSQL_ROOT_PASSWORD:/ {print \\$2; exit}" ${KOK}/docker-compose.yml)' ile değişkene al (ekrana basma), 'docker exec -e MYSQL_PWD="$P" kuyumcu-mysql mysql -uroot kuyumcu_net' ile information_schema'dan tenant_id kolonu olan tabloları bul, FK sırasına dikkat ederek (önce çocuk tablolar; SET FOREIGN_KEY_CHECKS=0 kabul) DELETE ... WHERE tenant_id='${fikstur.tenant_id}'; sonra tenants satırı; SELECT COUNT(*) FROM tenants sonucunu yaz. ${FIKSTUR} dosyasını 'printf "" >' ile boşalt. Token/parola yazma. ADIM EKONOMİSİ ≤ 20 araç çağrısı.
GRUP SONUÇLARI (TAMAMI — ${sonuclar.length} grup, ${sonuclar.flatMap(x => x.sayfalar).length} sayfa; tüm sayıları BU veriden hesapla; 'ölçülmedi' yalnız burada gerçekten olmayan rota için yazılır; metin kesilmiş görünürse özetin sonuna 'GİRDİ KESİLDİ' yaz): ${JSON.stringify(sonuclar).slice(0, 220000)}`, { label: 'ozet', phase: 'Özet', model: 'opus', effort: 'medium', schema: OZET })
// ═══ BK-39 · TUR SÜRESİ ÖLÇÜLÜR, VARSAYILMAZ (Mustafa K-F2) ══════════════════
// "≈8 ajan-saat/tur" bir varsayımdı ve tüm dalga bütçeleri ona dayanıyordu.
// Artık her tur kendi süresini yazar; sonraki bütçeler bu dosyadan hesaplanır.
// ⚠ K-20 (iCloud eşitlemesi) açıkken ölçüm "geçersiz taban" damgası alır (BK-42).
// 2026-09-21 DEĞİŞİKLİĞİ: koşum betiğinde require() yok (sandbox), bu yüzden dosyayı
// betik değil, Bash'i olan bu ajan yazar. Mekanizma otomatik kalır: ajan turun
// parçasıdır, atlanamaz; yazdığı sayıyı da ekrana döndürür (kanıt).
const KRONO = { type: 'object', properties: { duvar_saati_sn: { type: 'integer' }, duvar_saati_saat: { type: 'number' }, taban_gecerli: { type: 'boolean' }, gecerli_ortalama_saat: { anyOf: [{ type: 'number' }, { type: 'null' }] }, dosya: { type: 'string' } }, required: ['duvar_saati_sn', 'duvar_saati_saat', 'taban_gecerli', 'dosya'] }
const SURE_DOSYASI = KOK + '/dokumanlar/09-uygulama-kayitlari/ham/qa-tur-suresi.json'
const krono = await agent(`Türkçe yaz. GÖREV — TUR SÜRESİ KAYDI (BK-39). Sadece Bash + Read/Write kullan, ≤ 6 araç çağrısı, başka hiçbir dosyaya dokunma.
(1) Şimdiki zamanı 'date +%s' ile al; turun başlangıcı ${KRONO_T0} (epoch sn). Geçen süre = şimdi - ${KRONO_T0} (saniye).
(2) ${SURE_DOSYASI} dosyasını oku (yoksa {"_aciklama":"QA turu süresi — ÖLÇÜLDÜ, varsayılmadı (PLAN BK-39). Dalga bütçeleri bu dosyadan hesaplanır.","olcumler":[]} ile başlat).
(3) olcumler dizisinden tur=${TUR} kaydını çıkar, yerine şunu ekle: {tur:${TUR}, rota_sayisi:${SABLONLAR.length}, grup_sayisi:${GRUPLAR.length}, duvar_saati_sn:<hesapladığın>, duvar_saati_saat:<sn/3600, 2 hane>, bitis:"<date -u +%Y-%m-%dT%H:%M:%SZ>", icloud_esitleme_acik:${ICLOUD_ACIK}, taban_gecerli:${!ICLOUD_ACIK}, damga:"${ICLOUD_ACIK ? 'GEÇERSİZ TABAN (K-20 açık — R15/BK-42)' : 'geçerli'}"}.
(4) gecerli_ortalama_saat = taban_gecerli:true olan ölçümlerin duvar_saati_saat ortalaması (2 hane); hiç yoksa null. varsayim_8_saat_yerine = bu ortalama, null ise "HENÜZ GEÇERLİ ÖLÇÜM YOK — K-20 kapanana dek 8 saat VARSAYIMI kullanılmaya devam eder (dürüstçe: ölçülmedi)".
(5) Dosyayı 2 boşluk girintili JSON olarak yaz ve yazdığın kaydı döndür. Sayıyı UYDURMA: 'date' çıktısına dayandır.`, { label: 'krono', phase: 'Özet', effort: 'low', schema: KRONO })
const KRONO_SN = krono ? krono.duvar_saati_sn : -1
log(krono
  ? `tur süresi: ${krono.duvar_saati_saat} saat (duvar saati) · ${ICLOUD_ACIK ? 'GEÇERSİZ TABAN (K-20 açık)' : 'geçerli taban'} → ham/qa-tur-suresi.json`
  : 'tur süresi YAZILAMADI (krono ajanı düştü) — ham/qa-tur-suresi.json bu tur için EKSİK (Madde 1: gizlenmiyor)')

const ozetKesin = Object.assign({}, ozet || {}, OLCUM)   // betiğin saydığı KAZANIR
return { tur: TUR, olcum: OLCUM, ozet_ajan_hali: ozet, rota_kapsami: `${SABLONLAR.length}/${YAYIN_BEKLENEN}`, agac_rota_sayisi: ROTA_KAPSAM_BEKLENEN, kapsam_sapmasi: KAPSAM_SAPMASI ? String(args.yayin_gerekce).trim() : null, sure_sn: KRONO_SN, taban_gecerli: !ICLOUD_ACIK, fikstur: { tenant_id: fikstur.tenant_id, sorunlar: fikstur.sorunlar }, sonuclar, ozet: ozetKesin }
