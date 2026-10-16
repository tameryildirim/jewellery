# Rakiplerde var, bizde yok — KENDİ BAŞIMIZA yapabileceklerimiz (madde madde)

> **Kapsam (talimatınız):** mobil/telefon işleri ve dış izin-sözleşme-para gerektiren işler bu listeden çıkarıldı; hepsi en sonda § C'de ayrı ayrı sebebiyle duruyor.
> **Yöntem:** her satırın "bizdeki durum" sütunu koda bakılarak doğrulandı (grep/dosya:satır), rakip sütunu firmanın kendi sayfasındaki beyandır.
> **Süre sütunu:** § A'daki süreler mühürlü mimari tahminleridir; § B'deki süreler yeni tahmindir (±%40) ve ekip oyuyla kesinleşir.

---

## Kısa cevap

- **§ A — 27 madde:** zaten planda, sırası ve paketi belli. Kimseden izin, para ya da cihaz gerekmiyor. Toplam iş: kalan dalgalar (2c → 3 → 4 → 5 → 1.1).
- **§ B — 13 madde:** rakipte var, bizde yok, **planda da yok** — ama tamamen kendi elimizde. Toplam tahmin **22-30 gün**.
- **§ C — hariç tutulanlar:** 4 başlık (mobil · izin/sözleşme · donanım · kapsam dışı).
- **§ D — 7 madde:** rapor yazıldığında eksikti, **bu arada kapandı** (yanlışlıkla "eksik" saymayın).

---

# § A — Planda VAR, kendi başımıza yapıyoruz (27 madde)

| # | Madde | Kimde var | Bizdeki durum (doğrulandı) | Paket | Ne zaman | Süre |
|---|---|---|---|---|---|---|
| 1 | Vezne modeli — tezgâhtar satar, vezne tahsil eder; çalışan kasası | Dezy ("çalışan kasası — Yeni"), Gramino | Kodda `vezne` **0 satır** | 19 | Dalga 3 | 3-3,5 gün |
| 2 | Reyon / vitrin bazlı stok bölümlemesi | M&L, Pusula, ZarifSoft | Reyon tablosu var, stok bölümlemesi yok | 25 | Dalga 3 | 3-4,5 gün |
| 3 | Platin, paladyum, rodyum, çelik + saflık tabloları | M&L, Pusula | `Ayarlar.Maden`'de sadece altın/gümüş | 20 | Dalga 3 | 3-5 gün |
| 4 | Çoklu birim cari (TL + has + USD/EUR/GBP) | Kuyumcum.net (180 para birimi) | Cari bakiye yalnız TL + has gram | 27 | Dalga 3 | 3-4 gün |
| 5 | Rapor setini 19 rapora tamamlama + personel bazında satılan gram | Gramino, M&L, CetaSoft | Rapor sayısı eksik, personel kırılımı yok | 34-A | Dalga 3 | 8-10 gün |
| 6 | Kâr-zarar derinleştirme + mağaza pozisyon raporu | Kuyumcum.net ("pozisyonda zarar edilirse günün emeği zayi") | "TL'de kâr, HAS'ta zarar" uyarısı var; üç kanıtlı hata açık | 35-A | Dalga 3 | 4-5 gün |
| 7 | Eski programdan veri taşıma (kapılı, kuru provalı içe aktarım) | Dezy (ücretsiz), Terminal Gold, KuyumHub | Yok | 47-A | Dalga 3 | 4-6 gün |
| 8 | Açılış bakiyeleri + işletme kuruluş sihirbazı | Dezy ("30 dk'da kurulum") | Yok | T-2 | Dalga 3 | 3-4 gün |
| 9 | Kurulum ve yayın paketi (tek komutla kurulan sürüm) | Dezy, Terminal Gold ("kurulum anında") | Yok | T-1 + F-6 | Dalga 3 | 3-4 gün |
| 10 | Otomatik yedek + **doğrulanmış** geri yükleme | Dezy (6 saatte bir, 30 gün geri alma), Terminal Gold (günlük) | Elle çalışan tek betik var, otomasyon yok | T-3 | Dalga 3 | 1,5-2 gün |
| 11 | Sürüm/derleme damgası + hata kimliği + maskeli destek paketi | (Destek kalitesi: M&L Akademi, Pusula uzak destek) | Yok — müşteri "hangi sürümdesin" sorusuna cevap veremiyor | T-4a/b | Dalga 3 | 1,5-2,5 gün |
| 12 | İlk girişte profil seçimi, ekran setinin sadeleşmesi | Dezy, Kuyumcum.net (yetkiyle sadeleşme) | Basit/Profesyonel mod altyapısı var, ilk giriş seçimi yok | UX-3-B | Dalga 3 | 10-14 gün (UX-3 tümü) |
| 13 | Çek / senet portföyü | M&L, CetaSoft sınıfı | Yok | 29-A | Dalga 4 | 6-7 gün |
| 14 | Banka/POS **cihazı** takibi + komisyon + valör | Pusula, M&L | Kasa komisyon kusuru açık, cihaz defteri yok | 31 | Dalga 4 | 4 gün |
| 15 | Banka ekstresini **dosyadan** okuyup IBAN/ünvandan cari eşleştirme | KuyumHub (18 banka), Milyem Plus (17+), Terminal Gold (19) | Yok. *Not: canlı banka bağlantısı izin ister, MT940/CSV dosyası istemez — biz dosya yolunu yapıyoruz* | 32 + 41 | Dalga 4 | 5-7 + 2-3 gün |
| 16 | Kara liste / risk seviyesi / PEP beyanı / eşleşme motoru | Dezy ("kara liste & risk seviyesi"), Milyem (risk taraması) | Yok. *Yaptırım listesi aboneliği hariç — motor bizde, liste dışarıdan* | 40-A | Dalga 4 | 4-6 gün |
| 17 | Sistem yöneticisi kapısı + kayıt ucu kilidi | Terminal Gold ("izole veritabanı" konumlandırması) | Kayıt ucu açık | F-2-min | Dalga 3 | 2,5 gün |
| 18 | Satır içi düzenleme + yan çekmece + Esc/odak + tek dokunuşla kaydet | Gramino ("işlem geçmişi düzenleme/iptal"), Notion/Odoo deseni | QA turu 2: 30 düzenlenemeyen veri girişi | UX-1-C1..C4 | **Dalga 2c (şu an)** | sürüyor |
| 19 | Üst şeritte canlı kur + has pozisyonu (her ekranda) | KuyumHub, Gramino, Terminal Gold | Panelde var, üst şeritte yok | UX-2-A2 | **Dalga 2c (şu an)** | sürüyor |
| 20 | F-tuşu kısayolları + komut paleti | Terminal Gold (F2/F3/F12) | Yalnız POS barkod alanında Enter yakalanıyor | UX-2-B | Dalga 2c/3 | ~1 gün |
| 21 | Süreç şeridi (alındı → işlemde → hazır → teslim) | Jewel360 | Sipariş durumları var, görsel şerit yok | UX-2-B | Dalga 2c/3 | ~1 gün |
| 22 | Etiket / belge şablon motoru ("etiket tasarımcısı") | Dezy | Barkod + etiket ibaresi var, şablon tasarımı yok | 5 | Dalga 5 | 5-7 gün |
| 23 | Kâğıt sevk irsaliyesi + ortak yazdırma yerleşimi | M&L, Pusula | Yok | 42 | Dalga 5 | 3-4 gün |
| 24 | Has altın defteri dökümü + belge-defter mutabakatı | KuyumHub ("has paralel defter") | Defter var, döküm/mutabakat ekranı yok | 39 | Dalga 5 | 3-4 gün |
| 25 | Patron paneli + pozisyon/net varlık ekranı ("günlük servet özeti") | Dezy, KuyumHub | Panel 3 kart var, patron görünümü yok | 36 + 37 | 1.1 | 2-3 + 2-3 gün |
| 26 | Şube bazlı görünürlük + yetki matrisi tamamlama | Pusula, M&L (çoklu şube) | Şube ve 4 rol var, matris eksik | 46 | 1.1 | 3,5-4,5 gün |
| 27 | Ticari paket: maskeli **gerçek ekran görüntüleri**, 15 dk demo, tanıtım kiracısı, SLA | Dezy, Terminal Gold, KuyumHub, Gramino — **dördü de gerçek ekran yayımlıyor** | Hiçbiri yok. *Tanıtım kiracısı ve ekranlar bizde; yalnız yayımlama kararı sizde* | S-1 | 1.1 | 4-5,5 gün |

---

# § B — Planda YOK ama kendi başımıza yapabiliriz (13 madde, 22-30 gün)

Bu maddeler bugün hiçbir pakette değil. Hepsi izin/para/cihaz gerektirmiyor. **Ekip oyuna sunulacak**, kabul edilenler 1.1 kuyruğuna girer.

| # | Madde | Kimde var | Bizdeki durum (doğrulandı) | Tahmin | Neden değerli |
|---|---|---|---|---|---|
| 1 | **Sadakat / parapuan** | CetaSoft, Dezy, KuyumBase (CRM) | Kodda `Sadakat`/`Puan` **0 satır** | 3-4 gün | Perakende dükkânın müşteri tutma aracı; satışta sorulan ilk üç sorudan biri |
| 2 | **Personel prim / hakediş** | M&L, CetaSoft, KuyumHub (hedef-prim) | Kodda `Prim` **0 satır** | 3-4 gün | 34-A personel raporu üstüne oturur; patronun en çok istediği modül |
| 3 | **Vitrin / fiyat panosu** (ikinci ekrana tabela) | ZarifSoft, SarrafPano kategorisi | Fiyat motoru VAR, yayın ucu ve ekran yok | 2-3 gün | Pazar araştırmasının 1 numaralı önerisi; dükkân camına asılan ekran |
| 4 | **Fiyat kilitleme** (belirli süre fiyat sabitleme) | KuyumBase | Fiyat listesi + kampanya VAR, kilit alanı yok | 1-1,5 gün | Küçük iş, büyük değer: müşteriye verilen fiyat kur oynayınca bozulmuyor |
| 5 | **Stok devir hızı + ölü stok (stok yaşlandırma) raporu** | KuyumBase | **Cari** yaşlandırma VAR (`RaporModulu.cs:387`), **stok** yaşlandırma YOK | 1,5-2 gün | "Hangi mal 6 aydır vitrinde duruyor" — kuyumcunun para bağlama sorusu |
| 6 | **PDF katalog + müşteriye gönderilebilir ekstre çıktısı** | Dezy (PDF katalog + ekstre) | Ekstre ucu VAR (`CariModulu.cs:864`), yazdırılabilir/PDF çıktısı yok | 2-3 gün | 42 belge yerleşimi üstüne oturur; gönderme kanalı (WhatsApp/SMS) hariç |
| 7 | **Bandrol / seri numarası takibi** | KuyumBase | Kodda `Bandrol`/`SeriNo` **0 satır** | 2-3 gün | Tekil takip; ileride KMTS tarafında da işe yarar |
| 8 | **Sertifika kartı basımı (pırlanta)** | Dezy ("PVC sertifika kartı") | 4C verisi VAR, kart çıktısı yok | 1-1,5 gün | Pırlanta satan dükkânda doğrudan satış argümanı |
| 9 | **Müşteri segmentasyonu** (son alışveriş, ciro dilimi, doğum günü listesi) | KuyumBase (CRM segmentasyon) | Yok | 2-3 gün | Liste üretmek bizde; mesaj göndermek izinli işlerden (§ C) |
| 10 | **Muhasebeciye aktarım dosyası** (muhasebe fişi CSV/XML) | KuyumBase (Paraşüt senkronu) | Dışa aktarım altyapısı VAR, muhasebe fişi biçimi yok | 2-3 gün | *Canlı Paraşüt senkronu izin ister; dosya çıktısı istemez.* Mali müşavir engelini kaldırır |
| 11 | **Ürün fotoğraf galerisi** | Dezy (ürün foto/video) | Belge deposu VAR (paket 4-A), ürün kartında galeri yok | 1-2 gün | Video hariç; fotoğraf altyapısı zaten duruyor |
| 12 | **Ekspertiz / değerleme fişi** | Jewel360 | Yok | 1,5-2 gün | Hurda alımında ve emanet-rehin işinde kullanılıyor |
| 13 | **Ürün sayfası anlatımı: "dükkânın 8 acı noktası → çözümü"** | Gramino deseni | Yok | 0,5 gün | S-1 içine girer; yazı işi, kod değil |

---

# § C — Bu listeye ALMADIKLARIM (talimatınız gereği) ve sebepleri

**1. Mobil (sizin hariç tuttuğunuz)**
- Telefon uygulaması (iOS/Android) — Dezy, KuyumHub, KuyumBase, Terminal Gold, CetaSoft, Pusula, Kuyumcum.net
- Mobil el terminali (Pusula PARS) · push bildirim (KuyumBase) · çevrimdışı çalışma + senkron (Dezy offline sayım, KuyumBase)
- *Not: tablet ve 768 px tarafı zaten dalga 2c kabuğuyla geliyor; ayrı iş değil.*

**2. İzin / sözleşme / abonelik gerektirenler (kurucu kalemleri)**
| İş | Neden bizde bitmiyor |
|---|---|
| e-Fatura / e-Arşiv | Entegratör sözleşmesi + mali mühür (3-6 hafta, imza sizde) — yazılım tarafı hazırlanıyor |
| Canlı banka / açık bankacılık (19 banka) | Her bankayla ayrı sözleşme ve API anahtarı |
| Pazaryeri entegrasyonu (6 pazaryeri) | Pazaryeri mağaza hesabı + API anahtarı |
| Kargo entegrasyonu | Kargo firmasıyla sözleşme |
| Ödeme altyapısı / QR ödeme linki | Ödeme kuruluşu sözleşmesi |
| WhatsApp / SMS gönderimi | Gönderici adı başvurusu ve kontör (karar listesi md. 9) |
| Yaptırım listesi taraması | Veri sağlayıcı aboneliği (ücretli) |
| Canlı kur beslemesi | Veri kullanım izni (karar listesi md. 8) |
| Yapay zekâ asistanı (Dezy Becky, KuyumHub AI) | Dış servis anahtarı + aylık kullanım bedeli |

**3. Donanım gerektirenler** — RFID okuyucu (M&L, Kuyumcum.net) · elektronik terazi · NFC kimlik okuyucu (KuyumHub) · barkod el terminali. *Yazılımı yapılabilir; cihaz alımı sizin kararınız.*

**4. Kapsam dışı (kurucu kararı)** — atölye/üretim derinliği (Pusula), imalat fire takibi (KuvvemERP). Katman 1 baştan kapsam dışı bırakıldı.

---

# § D — Raporda "eksik" yazan ama BU ARADA KAPANAN 7 madde

Bunları tekrar istemeyin, bitti:

1. **Tamir / servis ve sipariş takibi** (M&L, CetaSoft, Jewel360 satıyor) — paket 3-A ile bitti, ekranı var.
2. **Ters ibraz / yanlış kaydı düzeltme** (Gramino satıyor) — 48-A storno motoru ile bitti.
3. **Şubeler arası stok transferi** (Dezy "kasa/varlık transferi") — zaten vardı (`POST /api/v1/islemler/transfer`).
4. **Zorunlu gün sonu kapanışı** (Dezy "Yeni" diye duyuruyor) — bizde gün kilidi olarak zaten vardı.
5. **İki aşamalı giriş (MFA)** — hiçbir rakibin sayfasında geçmiyor; bizde sunucu tarafı bitti, ekranı 2c'de.
6. **Cari yaşlandırma raporu** — var (0-30/31-60/61-90/90+ gün).
7. **Karanlık tema + kiracı izolasyon kanıtı** — bizde var, rakiplerin çoğunda yok.

---

# § E — Benim önerim: sıra

Kod tarafında sıra zaten mühürlü (2c → 3 → 4 → 5). § B'den **satışta en çok işe yarayacak dördü** öne alınmalı, çünkü ikisi birer günlük iş:

1. **Fiyat kilitleme** (1-1,5 gün) — en küçük iş, en görünür fayda.
2. **Stok devir / ölü stok raporu** (1,5-2 gün) — 34-A raporlarının içine girer, ayrı ekran açmaz.
3. **Personel prim** (3-4 gün) — 34-A personel kırılımının hemen ardına.
4. **Sadakat / parapuan** (3-4 gün) — rakiplerin üçünde birden var, bizde yok.

Kalan dokuz madde 1.1 kuyruğuna. **Karar ekip oyuyla alınacak, size sormuyorum** — bu belge yalnız "elimizde ne var" sorusunun cevabıdır.
