# Rakiplere göre neredeyiz? — Kurucu raporu (2026-09-16 16:50, Tamer Bey)

> **Kapsam dışı (elimizde olmayan işler, sıralamaya GİRMEDİ):** e-Fatura/e-Arşiv entegratör sözleşmesi ve mali mühür, yaptırım listesi veri aboneliği, kur verisi ticari sözleşmesi, alan adı/TLS, bulut hesabı. Bunlar `KURUCUYA-KARARLAR-2026-09-16.md`'de imza/para kalemi olarak duruyor.
>
> **Madde 1 uyarısı (dürüstlük):** Rakip yetenekleri firmaların **kendi pazarlama metnidir** — hiçbir rakip ürünün içi görülmedi (`PAZAR-ARASTIRMASI-2026-09-16.md` § Rakip matrisi, 13 firma, 152 kaynak). Bizim satırlarımız ise **ölçülmüştür** (1312 test, 27 sayfalık tarayıcı QA turu ×2, 7 mercekli hazırlık kurulu). Yani aşağıdaki sıralama "beyan ↔ kanıt" karşılaştırmasıdır; rakiplerde deneme hesabı açılırsa beyan kanıta döner (bkz. § 6).

## 1. Kısa cevap

| Soru | Cevap |
|---|---|
| Bugün, mevcut ağaçla (dalga 2a bitti, 15:52 kapısı) | 13 Türkiye rakibi arasında **satış vitrininde ~9-10. sıra** (mobil yok, kurulum/yedek/veri göçü yok, kaydedilen veri düzeltme boşlukları, kullanıcı dostu puan 3,1/5). |
| Uyum çekirdeği ve mühendislik kalitesi bakımından | **İlk 3** — gider pusulası VUK 234, tevsik haddi kapısı, MASAK eşiği, KDV 23/e özel matrah motoru, sentetik taş kapısı, append-only denetim izi, kiracı izolasyonu sayaçları, 1312 test. Bu kalite iddiasını yayımlayan rakip yok. |
| Dalga 2b + 2c bitince (bugün-yarın; düzenlenebilirlik, storno, MFA, Türkçe mesajlar, kılavuz, kabuk + mobil çekmece, Esc/odak) | Tahminen **5-6. sıra** (Dezy, KuyumHub, Terminal Gold, M&L, Pusula'nın arkasında; CetaSoft/KuyumBase ile başa baş). |
| 1.0 asgarisi bitince (dalga 3: yayın paketi T-1, veri göçü 47, açılış sihirbazı T-2, yedek T-3, ticari paket S-1) | **Tier 2'nin üst yarısı (3-4.)** — e-Fatura hariç. Fiyatla değil kapsam + sözleşmesizlik + veri sahipliği ile konumlanırız. |

Sıralama takım değerlendirmesidir (kurul tur 1'in 7 merceği + pazar ve arayüz araştırmaları); tur 2 kurulu da (QA turu 2 sonrası) HAZIR DEĞİL, 3,0/10 dedi — engeller aynı (düzeltilebilirlik/storno, raporlar, mobil kabuk, kurulum/yedek/göç, e-Belge, MFA); sıralama değişmedi. Belge: `dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-2.md`.

## 2. Rakip katmanları (elimizde olmayan işler hariç)

| Katman | Firmalar | Neden orada |
|---|---|---|
| **1 — Olgun pazar liderleri** | M&L Yazılım (~7.000 firma / 42 ülke; MLPerakende 94.686 TL peşin), Pusula (~3.000 firma; üretim + toptan + perakende, PARS mobil) | En geniş modül seti (personel hakediş, katalog, tamir-sipariş, RFID, banka/POS cihazı). Zayıf: masaüstü/lisans modeli, mobil patron ekranı zayıf, Şikayetvar'da "ömür boyu denildi, güncelleme ücreti istendi" kaydı (M&L, 05.12.2025). |
| **2 — Modern bulut, güçlü pazarlama** | Dezy (500/1.000/2.500 TL/ay, 3 ay ücretsiz, 6 pazaryeri, mobil, 6 saatte bir yedek, ücretsiz veri aktarımı, 7/24 WhatsApp), KuyumHub (AI asistan, 18 banka açık bankacılık, MASAK + yaptırım taraması, has paralel defter, "mevcut ERP'nin yanında çalışır"), Terminal Gold (19 banka, özel matrah faturası, F2/F3/F12 kısayolları, "her işletmeye izole veritabanı", gerçek ekran görüntüsü yayımlıyor), CetaSoft (9 modül, sadakat/parapuan, tamir, personel prim; 40.000 TL + KDV cihaz başı), KuyumBase (e-ticaret + kargo, stok devir/yaşlandırma analizi), Milyem Plus (havale → eşleşme → e-Fatura zinciri, 17+ banka) | Modern ekranlar, mobil, entegrasyon genişliği. Zayıf: fiyat çoğunda gizli; uyum derinliği yüzeysel (ŞİB formu, 10 iş günü, tevsik yok); kalite/izolasyon kanıtı yayımlamıyorlar. |
| **3 — Niş / küçük** | Kuyumcum.net (30-40 $/kullanıcı/ay + kurulum), KuvvemERP, ZarifSoft (vitrin/ekran modu), Gramino (tek ekran alış/satış/tahsilat, ters ibraz), Expert Barkod (modül başına ücret) | Dar kapsam ya da düşük kurumsal olgunluk (Gramino'nun iletişimi Gmail + Instagram). |
| **Biz — Kuyumcu ERP (ücretsiz pilot)** | Bugün: Katman 2'nin alt ucu (işlev) / Katman 1 (uyum + kalite) / son sıra (mobil, teslimat) | Aşağıdaki boyut tablosu. |

## 3. Boyut boyut sıralama

| Boyut | Bizde bugün (kanıt) | Rakiplerde (beyan) | Sıramız (13 firma içinde) | Ne zaman değişir |
|---|---|---|---|---|
| **Çekirdek dükkân işi** — has-cari çift defter, POS (karma ödeme, hurda/takas), çift birim stok, fiyat listesi + kampanya + ürün görünümü, emanet, sayım, kasa/banka, gider pusulası, pırlanta 4C | VAR ve ölçülü: 1312 test, dalga 2a ile fiyat listesi düzenleme + POS çift kasa hatası kapandı; QA turu 2'de fiyatlandırma kritiği KAPANDI. Eksik: tamir/sipariş (3-A, 2b'de sırada), vezne (19), reyon (25), çek/senet (29), personel prim (1.1), sadakat (1.1), RFID/terazi (yok) | M&L/CetaSoft/Pusula geniş; Dezy/KuyumHub modern; Jewel360 tamir + ekspertiz çıtası | **6-7.** | 2b (3-A) → 3 (19, 25, 20, 27, 34, 35) sonra **4-5.** |
| **Uyum (bizim elimizdeki kısım)** — MASAK 185.000 eşiği + kimlik tespiti + ŞİB kaydı, tevsik 30.000 kapısı, gider pusulası VUK 234 belgesi, KDV 23/e özel matrah motoru (1-A), sentetik taş/köken kapısı, append-only denetim izi, KVKK dışa aktarım | VAR; ama ekranda kusurlu: MASAK "Gönderildi/Kapat" düğmeleri istek atmıyordu, kimlik tespiti düzenlenemiyor (UX-4d md. 10 + UX-1-C3, 2b/2c'de) | KuyumHub (MASAK + NFC + yaptırım + 8 yıl saklama), Milyem Plus (risk taraması), Terminal Gold (özel matrah), Dezy ("805 otomatik" iddiası) — hiçbiri doğrulanmadı | **1-3.** | 2b (38-A + UX-4d) ile ekran tarafı da kapanır |
| **Arayüz / kullanıcı dostu** | Ölçüldü: tur 1 görsel 3,85/5 · kullanıcı dostu 3,07/5; tur 2 (27 sayfa) görsel 3,85 / kullanıcı dostu 3,15 (dar bölmede ölçüldü). Artı: belirteç sistemi + karanlık tema (rakiplerde nadir), Basit/Profesyonel mod, Panel 3 kart, POS tek ekran. Eksi: 375 px'te 27/27 sayfa taşıyor, Escape modalı kapatmıyor, sessiz formlar, İngilizce doğrulama mesajları, "kaydedilen veri düzeltilemiyor" boşlukları, emoji ikonlar | Dezy 4/5 (profil seçimiyle sadeleşme, kasa+kur+gün paneli), Terminal Gold (tek ekran + F-tuşları + gerçek ekran görüntüleri), Jewel360 (Capterra kullanım kolaylığı 5,0 — kendi sayfasından, taraflı), Kuyumcum.net (yetkiyle sadeleşme) | **9-11.** | 2b (Türkçe mesajlar, onay modalları, kılavuz) + **2c** (kabuk + ≤768 px çekmece menü, Cekmece/SayfaBasi/SatirIciDuzenle, Esc/odak, 27 sayfa tek dokunuş, SVG ikon seti) → **5-6.**; UX-2-B (5 şablon) → **3-4.** |
| **Mobil / çoklu platform** | YOK (PWA-1 dalga 7; 375 px kabuk 2c'de) | Hemen hepsinde iOS/Android; Dezy + KuyumHub + Terminal Gold + CetaSoft + Pusula + Kuyumcum.net | **Son.** | 2c kabuk ile "telefonda kullanılabilir", PWA-1 ile "ana ekrana eklenir" |
| **Çoklu şube / kullanıcı / yetki / izolasyon** | Şube, 4 rol, modül yetkisi, kiracı izolasyonu sayaçları (denetim.sh), yetki hiyerarşisi; MFA yok (45-API 2b'de), sistem yöneticisi kapısı yok (F-2-min dalga 3) | Terminal Gold "izole DB", Pusula/M&L şube; MFA hiçbirinin sayfasında geçmiyor | **3-5.** | 2b (45-API) → **1-2.** |
| **Teslimat & işletim** — kurulum, yedek, destek, veri göçü, kılavuz, sürüm/hata kimliği | YOK sayılır: yayın paketi yok (T-1), yedek otomasyonu yok (T-3), içe aktarım yok (47), açılış bakiyeleri yok (T-2), kılavuz boştu (UX-4d md. 14), hata kimliği yok (T-4a), destek SLA yok (S-1) | Dezy (30 dk kurulum, 6 saatte bir yedek + 30 gün geri alma, ücretsiz veri aktarımı, 7/24 WhatsApp), M&L (ML Akademi), Pusula (uzak destek) | **Son.** | Dalga 3 (T-1, T-2, T-3, 47, T-4a) + S-1 → **orta** |
| **Fiyat / ticari model** | Ücretsiz pilot; öneri 1.200-1.500 TL/ay + KDV (F-8, ilan değil); yazılım lisansı 0 TL; kullanıcı başı ücret yok; veri müşteride (dışa aktarım) | Dezy 500-2.500 TL/ay; Kuyumcum.net 30 $/kullanıcı/ay; M&L 94.686 TL peşin; CetaSoft 40.000 TL + KDV cihaz başı; Terminal Gold 900 $/yıl'dan | Fiyatla **Dezy'yi geçemeyiz** (F-8 dürüst okuma); Mikro Jump'a karşı ~%77-86, Kuyumcum.net'e karşı 3 kullanıcıda ~%73 tasarruf (kaynaklı) | S-1 + F-8 Faz B (rakip fiyat teyidi) |
| **Mühendislik kalitesi / güven** | 1312/1312 test + mutasyon kanıtı + denetim TEMİZ her pakette; append-only denetim izi (ADR-0004); kiracı sızıntısı kapıları; veri dışa aktarım (kilitlenme yok) | Hiçbir rakip test/izolasyon kanıtı yayımlamıyor; şikâyet kayıtları: CetaSoft lisans mağduriyeti (04.10.2024), M&L güncelleme ücreti | **1.** (kanıtlı iddia bakımından) | Satış hikâyesinin omurgası; T-4a hata kimliği + T-3 yedek tatbikatı ile müşteriye görünür olur |

## 4. Bizde var / yok — rakiplerin sattığı ve elimizde olan işler

| Özellik | Durum | Nerede / ne zaman |
|---|---|---|
| Has-cari çift defter, "TL'de kâr HAS'ta zarar" uyarısı | VAR (ölçülü) | — |
| POS karma ödeme, hurda/takas, tek kasa otomatik, çift kasa hatası | VAR (UX-4b, 1283 test) | — |
| Fiyat listesi + kampanya + liste × ürün görünümü, toplu marj | VAR (UX-4a + 49) | — |
| Çift birim stok (adet + gram), sayım oturumu, etiket/barkod | VAR | — |
| Gider pusulası VUK 234 belgesi, tevsik kapısı, MASAK eşiği/ŞİB | VAR; ekran düğmeleri 2b'de tamir | UX-4d md. 10, UX-1-C3 |
| KDV 23/e özel matrah motoru | VAR (1-A); genişleme + önizleme | 38-A (2b) |
| Kaydedilen her verinin düzeltilebilmesi (kurucu şartı) | KISMEN — QA turu 2: 21 sayfada 21 düzenlenemeyen giriş (grup 1-2) | UX-1-B (2b, API) + UX-1-C1..C4 (2c, 27 sayfa) |
| Storno / denetim izli düzeltme motoru | YOK | 48-A (2b) |
| Türkçe, alan altı doğrulama mesajları; onay modalları | YOK → 2b | UX-4d md. 8, 13 |
| Raporların kendi içinde tutarlılığı (gün sonu farkı, aylık has) | HATALI → 2b | UX-4d md. 11-12; 34/35 (dalga 3) |
| Kılavuz / uygulama içi yardım | BOŞTU → 2b | UX-4d md. 14; K14 kılavuz galerisi (2c) |
| MFA (iki aşamalı giriş) | YOK → 2b | 45-API (2b), 45-UI (2c) |
| Bildirim + zamanlanmış işler (vade hatırlatma vb.) | YOK → 2b | 6-A |
| Sipariş / tamir-servis takibi (M&L, CetaSoft, Jewel360 satıyor) | YOK → 2b | 3-A |
| Mobil kabuk (≤768 px çekmece), PWA | YOK | UX-2-A2 (2c), PWA-1 (dalga 7) |
| Vezne modeli, reyon/vitrin stoğu, platin/paladyum, çoklu birim cari | YOK → dalga 3 | 19, 25, 20, 27 |
| Kurulum paketi, yedek otomasyonu, veri içe aktarım, açılış bakiyeleri | YOK → dalga 3 | T-1, T-3, 47-A, T-2 |
| Banka/açık bankacılık eşleştirme (KuyumHub, Milyem Plus, Terminal Gold) | YOK → dalga 4 (MT940/CSV) | 32, 41 |
| Vitrin/fiyat panosu (ZarifSoft, SarrafPano kategorisi) | Fiyat motoru VAR, yayın ucu/ekran yok | 1.1 (pazar araştırması 1 numaralı öneri) |
| Sadakat/parapuan, personel prim, çek/senet, RFID, terazi, pazaryeri | YOK | 1.1 ve sonrası (29 dalga 4; diğerleri kuyruk) |

## 5. Arayüz: rakiplerden alınacak dersler ve bizim cevabımız

Bugün elimizdeki kanıt (arayüz araştırması, 4 tarama × 10 ürün; rakip sayfaları ve yayımlanmış ekran görüntüleri):

| Rakip deseni | Bizde | Plan |
|---|---|---|
| Dezy — kurulumda profil seçimi, ekran seti sadeleşir; panelde kasa + canlı altın + gün özeti | Basit/Profesyonel mod VAR; Panel 3 kart VAR | İlk girişte mod seçimi (UX-3-B, dalga 3) |
| Terminal Gold — tek ekranda barkod → kur → işçilik → fiş; F-tuşu kısayolları; altın pozisyonu her ekranda | POS tek ekran VAR; kısayol yok; üst şeritte has pozisyonu yok | UX-2-A2 kabuk (üst şerit) + KomutPaleti/kısayollar (UX-2-B) |
| Notion/Odoo — satır yanında çekmecede düzenleme, satır içi düzenleme, Enter kaydet / Esc iptal | YOK (modal + Esc çalışmıyor) | Cekmece + SatirIciDuzenle (2c, mühür K3) |
| Shopify — boş durumda tek belirgin eylem; sınır uyarıları | BosDurum VAR, CTA yuvası yok; sunucu sınırı uyarısı yok | UX-1-C (UX-S29/S30 sözleşmesi) |
| Jewel360 — süreç şeridi (alındı → işlemde → hazır → teslim) | YOK | Emanet/sipariş AdimGostergesi (2c/UX-2-B) |
| Lightspeed — rehberli ilk giriş, bağlam duyarlı "?" | Kılavuz boştu | UX-4d md. 14 + K14 galerisi + ilk giriş kontrol listesi (T-2) |
| Karanlık tema | VAR (rakiplerin çoğunda yok) | Korunur, ölçülür (UX-2-D) |

### 5b. Canlı bakış — 17:01, uygulama içi tarayıcıyla 6 rakip sitesi (metin + yayımlanmış ekran envanteri)

Tarayıcı bölmesi bu sırada gizli olduğu için **ekran görüntüsü alınamadı** (bölme açıldığında aynı sayfalar ekran görüntüsüyle tekrar gezilip buraya eklenecek); sayfa metinleri ve yayımladıkları ekran görselleri okundu. Aşağıdakiler firmaların kendi sayfalarındaki beyanlardır.

| Rakip (sayfa) | Sayfada ne gösteriyor | Bizim için ders / karşılık |
|---|---|---|
| **Dezy** (dezyapp.com) | "Yıl sonuna kadar ücretsiz"; "Gerçek uygulama ekranları (demo hesap)"; özellik matrisi: canlı kurlu POS, **tek fişte karma ödeme (nakit·kart·altın·hurda) — Yeni**, ayar bazlı stok, etiket tasarımcısı, PVC sertifika kartı, ürün foto/video, **offline stok sayımı**, çoklu şube + şubeler arası kasa/varlık transferi, **çalışan kasası + zorunlu gün sonu kapanışı — Yeni**, çalışan performansı, **kara liste & risk seviyesi**, müşteri mobil uygulaması + PDF katalog + ekstre, banka ekstresinden e-fatura, günlük servet özeti, 6 pazaryeri, Becky AI, AI sesli borç hatırlatma, "ücretsiz özellik talebi" | Karma ödeme bizde VAR (paket 14 + UX-4b çift kasa düzeltmesi); çalışan kasası/gün sonu kapanışı = bizde gün kilidi VAR + vezne (19, dalga 3); kara liste/risk = 40-A (dalga 4); servet özeti = Panel has pozisyonu (VAR); offline sayım + müşteri uygulaması + PDF katalog + AI = 1.1 ve sonrası. Pazarlama dersi: gerçek demo ekranları ve "ücretsiz özellik talebi" vaadi — S-1 paketine girecek. |
| **Terminal Gold** (terminal.gold/kuyumcu-yazilimi) | "Pazarlama değil, gerçek ekranlar" — Satış / Stok / e-Fatura sekmeleriyle canlı ekran görüntüleri (müşteri verisi maskeli); hızlı satış "barkoddan okut → kuru uygula → işçiliği ekle → fişi kes"; 19 banka canlı entegrasyonu; izole veritabanı + SSL + günlük yedek; "veri aktarımı dâhil, kurulum anında, taahhüt yok"; 50+ modül iddiası | Satış akışı bizde tek ekran (VAR); banka canlı entegrasyonu bizde yok (32/41 MT940/CSV, dalga 4); yedek/kurulum vaadi = T-1/T-3 (dalga 3). Ders: satış sayfasında maskeli GERÇEK ekranlar yayımlamak — QA fikstür kiracısı bunun için hazır. |
| **KuyumHub** (kuyumhub.com) | Açılışta canlı kur şeridi (has, gram, çeyrek, yarım, tam, USD, EUR); "Özet / Satış / Banka / Kimlik & MASAK / Yapay Zeka" ekran karuseli (demo hesap, maskeli); 5 adımlı akış: satış → NFC kimlik + MASAK/yaptırım taraması → gider pusulası VUK 234 → e-Fatura (has istisnası) → banka eşleşmesi; "mevcut programınızın (Pusula, Nebim, Logo, Mikro, Netsis, ETA, M&L) üstüne kurulur, 15 dk"; personel prim/hedef; 14 gün ücretsiz; iOS/Android yerel uygulama | Uyum zinciri (kimlik → MASAK → gider pusulası → özel matrah) bizde VAR (e-Fatura hariç) ama tek akış olarak SUNULMUYOR — ders: uyum adımlarını satışın içinde ardışık göstermek (UX-2-B süreç şeridi). Canlı kur şeridi = üst şerit (UX-2-A2). "Mevcut programın yanında çalışır" konumlandırması bizim veri göçü (47) argümanımızın rakibi. |
| **Kuyumcum.net** (Özellikler) | Tablet-öncelikli, "tezgâh üstü kullanım, personel müşteriyle konuşurken sade tasarım"; mağazaya erişim yalnız kayıtlı/onaylı cihazlardan; 180 para birimi; mağaza pozisyon raporu ("alış-satışta kâr, pozisyon dönüştürmede zarar edilirse günün emeği zayi olur") | Cihaz kaydı fikri = MFA (45-API) + T-1 nginx IP kısıtı ile kısmen; pozisyon raporu = bizde "TL'de kâr, HAS'ta zarar" uyarısı VAR. Ders: tezgâh üstü sade ekran = UX-3 basit mod + POS ≥44 px hedefler (UX-2-B). |
| **KuyumBase** (özellikler) | iOS/Android/Web, **offline mod + otomatik senkron**, "Nisan 2026 KMTS'ye tam uyum" (pazar araştırmasında iki kez çürük bulundu), bandrol/seri no, e-ticaret + kargo + SEO, Paraşüt çift yönlü senkron, 15 sn canlı fiyat + **fiyat kilitleme** + fiyat geçmişi grafikleri + push bildirim, 4 ödeme altyapısı, QR ödeme linki, CRM segmentasyon; 14 gün ücretsiz, kart yok | Fiyat kilitleme (belirli süre fiyat sabitleme) bizde yok → küçük ve değerli: fiyat listesi "kilit" alanı (1.1 adayı); Paraşüt senkronu = 42-A muhasebe köprüsü (dalga 5). Offline = PWA-1. |
| **Gramino** (graminoerp.com.tr) | Tek ana menü ekran görüntüsü (1440×900: canlı kur + günlük özet); "Excel'den kurtarın, tek ekrandan yönetin"; alış/satış + açık hesap + tahsilat tek ekran; kasa-para/ziynet/gram/hurda/bilezik ayrı kartlar; **işlem geçmişi düzenleme/iptal + ters ibraz kaydı**; personel bazında satılan gram; "karmaşık menüler arasında kaybolmazsınız" | Ters ibraz = bizim 48-A storno (dalga 2b'de sırada); personel gram raporu = 34/35 (dalga 3). Ders: ürün sayfasında "dükkânın 8 acı noktası → çözüm" anlatımı (S-1). |

**Ortak gözlem:** altı rakibin dördü (Dezy, Terminal Gold, KuyumHub, Gramino) sitesinde **gerçek ürün ekranı** gösteriyor; hepsi canlı kuru açılış ekranının ortasına koyuyor; üçü (Dezy, KuyumHub, KuyumBase) **kartsız ücretsiz deneme** veriyor. Bizde bunların hiçbiri dış dünyaya açık değil (ürün ücretsiz pilot, sitesi yok) — bu, S-1 ticari paketinin ilk üç maddesi olmalı.

Ürünlerin **içi** yalnız deneme hesabıyla görülür (Dezy yıl sonuna kadar ücretsiz, KuyumHub 14 gün, KuyumBase 14 gün): hesap açmak dış taahhüt olduğu için ekip açmaz — kararı `KURUCUYA-KARARLAR-2026-09-16.md` md. 12'ye yazdık.

## 6. Dürüst uyarılar

1. Rakip satırları beyandır; deneme hesabı açılmadan "Dezy'de var, bizde yok" cümleleri kanıt değildir.
2. Fiyatla Dezy'yi silemeyiz (Dezy 500 TL/ay taban; bizim önerimiz 1.200 TL/ay). Kurucunun "silme oranı" hedefi tek rakama inmez; rakibe göre değişir (F-8 § dürüst okuma).
3. Hazırlık kurulu tur 1: **HAZIR DEĞİL, 3,14/10**. Tur 2 bugün QA turu 2 sonrası; HAZIR ancak 7 mercek hazır + ortalama ≥ 8 deyince.
4. QA turu 2 (27 sayfa, tamamlandı): 149 bulgu, 11 kritik, 30 düzenlenemeyen veri girişi; önceki 147 bulgudan 12'si kapandı, 13'ü kısmen, 122 açık — çünkü düzenlenebilirlik işi (UX-1-B/C, 48-A) 2b/2c'de sırada. Beklenen ara durum, gerileme değil. Yeni yüksek hatalar (ürün Stok modalı, uyum belge no araması — ikisi de "API yanıt şekli ↔ istemci modeli" uyumsuzluğu) dalga 2c'ye işlendi.

## 7. Bana verdiğiniz hal ile şimdiki hal — ekran ve kabiliyet farkı

**Karşılaştırma noktaları.** "Verdiğiniz hal" = 2026-09-16 12:30 orkestratör kapısı (talimatınızın hemen sonrası, dalga 1 bitmiş hali). "Şimdiki hal" = 2026-09-17 sabahı (dalga 2a + 2b bitti, kapı 22:11; S-2 elden kapandı). Ölçümler kapı kayıtlarından ve iki tarayıcı QA turundan alınmıştır; tahmin yoktur.

### 7.1 Sayılarla

| Ölçüm | Verdiğiniz hal (16 Eylül 12:30) | Şimdi (17 Eylül) | Fark |
|---|---|---|---|
| Otomatik test | 1.193 | 1.650 | +457 |
| Veritabanı göçü (migration) | 25 | 33 | +8 |
| Ekran (Blazor sayfası) | 27 | 30 | +3 |
| Sevk edilmiş paket | 6 (dalga 1) | 18 (dalga 1 + 2a + 2b + S-2) | +12 |
| Tarayıcı QA turu | henüz yapılmamıştı | 2 tur tamamlandı, 3. tur sürüyor | — |
| Kapı durumu | build 0/0 · denetim temiz | build 0/0 · denetim temiz | değişmedi (korundu) |

### 7.2 Ekranlarda ne değişti

**Yeni ekranlar (3)**

| Ekran | Ne yapar |
|---|---|
| Siparişler | Müşteri siparişi, dış atölyeye verilen iş ve tamir-servis takibi; altı durumlu akış, her durum değişikliği iz bırakır |
| Sipariş detayı | Tek siparişin kalemleri, durum geçmişi, söz verilen gün, iptal onayı |
| Bildirimler | Vade hatırlatmaları, MASAK 10 iş günü uyarısı, saklama süresi önizlemesi; üst barda okunmamış rozeti |

**Düzeltilen ekranlar (sizin gördüğünüz sorunlar dâhil)**

| Ekran | Önce | Şimdi |
|---|---|---|
| POS (İşlemler) | Karma ödemede kasa iki kez soruluyordu (sizin ekran görüntünüzle bildirdiğiniz sorun) | Üst kasa seçici karma ödemede hiç çizilmiyor; bacak kasası tek seçenekte otomatik ve kilitli |
| Fiyatlandırma | Kaydedilen liste ve kampanya düzeltilemiyordu | Satırda Düzenle / Pasife al / Aktifleştir, kaydedildi bandı; liste × ürün görünümü, toplu marj önizlemesi |
| Ürünler | Şube stokları penceresi her açılışta çöküyordu | Pencere gerçek veriyle açılıyor; konsinye alanı düzenlemeye eklendi; ürün kartından açılış stoğu girilebiliyor |
| Cariler | Yeni cari formu, düzenleme formundan daha az alan istiyordu | İki form aynı alan kümesini taşıyor |
| Kılavuz | Sayfa bomboştu (kendi güvenlik başlığımız kendi sayfamızı engelliyordu) | Çerçeve kaldırıldı, içerik uygulama içinde gösteriliyor |
| Form mesajları | "The Email field is not a valid e-mail address." gibi İngilizce ve teknik | Türkçe, alan adıyla; sunucu hangi alanda ne hata olduğunu ayrı ayrı döndürüyor |
| Kur | Manuel kur girilse de geçmiş boş görünüyordu | Geçmiş aynı günü de kapsıyor; sözleşmesiz fiyat kaynakları kapatılabilir hale geldi |
| Faturalar | Kuyum faturasında has bedeli elle giriliyordu | İşlem seçilince has bedeli defterden türetiliyor, tutmazsa kayıt yazılmıyor |
| Emanet, Sayım, Kullanıcılar | Arama yok, kapanış sonucu görünmüyor, şube atanamıyor | Cari araması, kapanış özeti ve iptal onayı, kullanıcıya şube atama |

**Ölçülen puanlar (aynı yöntem, aynı 27 sayfa)**

| Ölçüm | QA turu 1 (16 Eylül 13:20) | QA turu 2 (16 Eylül 17:08) |
|---|---|---|
| Görsel | 3,85 / 5 | 3,85 / 5 |
| Kullanıcı dostu | 3,07 / 5 | 3,15 / 5 |
| Kritik bulgu | 12 | 11 |
| Düzenlenemeyen veri girişi | 29 | 30 |
| Toplam bulgu | 133 | 149 |

**Bu tablonun dürüst okuması.** Ekranlarda somut düzelmeler var ama puanlar neredeyse aynı kaldı ve bulgu sayısı arttı. Üç nedeni var, üçü de kayıtlı: (1) ikinci turda daha çok akış denendi ve iki yeni ekran eklendi, yani ölçüm yüzeyi büyüdü; (2) "kaydedilen veriyi düzeltme" işinin sunucu tarafı bitti, ekranlara bağlanması sıradaki dalgada (bu yüzden düzenlenemeyen kalem sayısı düşmedi); (3) dar ekran kabuğu (telefon görünümü) henüz yazılmadı, 27 sayfanın tamamı telefonda yatay kayıyor. Bu üç iş dalga 2c'nin içeriğidir.

### 7.3 Kabiliyette ne değişti

**Yeni yetenekler**

| Yetenek | Ne işe yarar | Durum |
|---|---|---|
| Kuyum KDV özel matrahı | Has altın bedeli matrah dışı kalır, KDV yalnız işçilik ve kâr üzerinden hesaplanır; işlem seçilince rakam defterden gelir, tutmazsa fatura yazılmaz | Çalışıyor, canlıda doğrulandı |
| Storno (denetim izli düzeltme) | Yanlış kayıt silinmez; ters kayıt gerekçeyle yazılır, kim yaptı izi kalır, aynı kayıt iki kez storno edilemez | Çalışıyor (satış, kasa, banka, emanet, fatura durumu); takas, hurda alışı ve transfer henüz kapsam dışı ve açıkça reddediliyor |
| İki aşamalı giriş | Parolanın yanında telefon uygulamasından altı haneli kod; kurtarma kodları; yanlış denemede kilit | Sunucu tarafı bitti, ekranı sıradaki dalgada |
| Bildirim ve zamanlanmış işler | Vade yaklaşınca, MASAK süresi dolarken, saklama süresi geçerken uygulama kendi uyarır | Çalışıyor |
| Sipariş ve tamir takibi | Müşteri siparişi, dış atölye işi ve tamir; söz verilen gün, gecikme rozeti | Çalışıyor; defteri etkilemiyor (kanıtlandı) |
| Kaydedilen veriyi düzeltme uçları | Reyon, işletme kimlik bilgileri, kasa, banka hesabı düzeltilebilir hale geldi | Sunucu tarafı bitti, ekran bağlama sıradaki dalgada |
| Fiyat listesi = ürün görünümü | Listeye göre her ürünün fiyatı, toplu marj uygulaması, tekil geçersiz kılma | Çalışıyor |
| Açılış stoğu | Ürün kartından ilk stok girilebiliyor (önce satış "stok yok" diye reddediyordu) | Çalışıyor |

**Belge ve karar tarafı (sizin için üretilenler)**

- Rakip kıyas raporu (bu belge), birim ekonomi dosyası, iki hazırlık kurulu değerlendirmesi, iki tarayıcı QA turu çizelgesi.
- Sürüm mührü taslağı: hangi paket 1.0'a, hangisi 1.1'e — 50 paket tek tabloda.
- Üretim yayını ve yedekleme için taslak yapılandırmalar (kod değil, uygulayıcıya girdi).
- Karar listeniz 12 maddeden 15 maddeye çıktı; hepsi imza, para ya da geri dönülemez veri gerektiren işler.

### 7.4 Değişmeyenler (dürüstlük)

- **Satışa hazırlık kararı hâlâ "hazır değil"**: iki kurul turunda da (3,14 ve 3,0 üzerinden 10) aynı sonuç çıktı.
- **e-Fatura hattı yok**: yazılım tarafı hazırlanıyor, entegratör sözleşmesi ve mali mühür sizde.
- **Telefonda kullanım yok**: dar ekran kabuğu sıradaki dalgada.
- **Kurulum, yedek, veri taşıma yok**: üçü de 3. dalgada.
- **Fiyat ilan edilmedi**: ürün ücretsiz pilot; rakamlı fiyat için birim ekonomi dosyasının ikinci yarısı ve sizin onayınız gerekiyor.

## Kaynaklar
- `PAZAR-ARASTIRMASI-2026-09-16.md` (13 rakip, 152 kaynak; § Rakip matrisi)
- `dokumanlar/02-mimari/ARAYUZ-ARASTIRMASI-2026-09-16.md` (benchmark 14 ürün; § Mühür K1-K14)
- `dokumanlar/02-mimari/F-8-BIRIM-EKONOMI-2026-09-16.md` (fiyat kıyası, kaynaklı)
- `dokumanlar/09-uygulama-kayitlari/HAZIRLIK-KURULU-1.md` (7 mercek, 20 engel, § Ramazan — rakip parite)
- `dokumanlar/09-uygulama-kayitlari/QA-TARAYICI-TURU-2026-09-16.md` (+ tur 2 çizelgesi hazırlanıyor), `ham/qa-turu-1.json`
- `dokumanlar/09-uygulama-kayitlari/ORKESTRATOR-DURUM.md` (kapı kayıtları: 15:52, 1312/1312)
