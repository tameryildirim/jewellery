# Rakiplerin en iyisi bizde — özellik eşitleme planı

**Tarih:** 15 Ekim 2026 · **Hazırlayan:** Tamer Bey (4 paralel araştırma ajanı + kod taraması) · **Kapsam:** yalnız okuma; kod değişmedi.
**Kurucu hedefi:** *"Tüm benzer ürünlerin en iyi özelliklerini istiyorum; onlarda olup bizde yok diye bir şey istemiyorum."*

**Kaynak notu:**
- Rakip bilgileri firmaların **kendi beyanıdır** (site, uygulama mağazası, video başlığı); ürünlerini biz denemedik.
- "Bizde" sütunu ise **koddan** doğrulandı; dosya yolu verildi.
- Mevzuat iddiaları rakip sitesinden alınmadı.

---

## 1. Özet

| | |
|---|---|
| Taranan program | **36** (Türkiye 25, yurt dışı 11). Bunların 10'u bu turda ilk kez bulundu: Softnorm, Ariyen, Kuyum Plus, Naron, Dijimo, Kuyumex, Goldist, Platin360, Hasfiyat, kuyumcuprogrami.com |
| Bizim program bugün | 54 ekran · 436 sunucu ucu · 43 modül |
| Rakiplerde görülen özellik başlığı | **~95** (aynı işi yapanlar birleştirildi) |
| Bizde **VAR** (yaklaşık sayım) | ~45 |
| Bizde **KISMİ** | ~12 |
| Bizde **YOK** | ~38 |
| Kapatmak için iş paketi | **~80 paket**, 3 dalga (30 + 20 + 30) |
| Çalışma süresi (benim temponla: paket başı 6–10 saat) | **480–800 saat** |
| Takvim (dış beklemelerle) | iyimser **6 hafta** · gerçekçi **2,5 ay** · kötü **4 ay** |

**Kısaca:** Tezgâh ve uyum çekirdeğimiz rakiplerin çoğundan derin. Eksiğimiz üç yerde toplanıyor:
1. **Dışarıya bağlanma:** e-Fatura, SMS ve WhatsApp, banka, POS, pazaryeri.
2. **Donanım:** terazi, RFID, termal fiş, etiket yazıcı, kimlik çipi.
3. **Telefon:** uygulama, çevrimdışı çalışma, müşteri uygulaması.

---

## 2. Bizde olup rakiplerin çoğunda olmayanlar (satışta kullanılacak)

| Özellik | Bizde (kanıt) | Rakipte |
|---|---|---|
| Tevsik kapısı: 30.000 TL üstü nakdi **durdurur**, ihlal kaydı ve dilekçe hazırlar | `Modules/Tevsik/*` | KuyumHub yalnız uyarıyor; diğerlerinde yok |
| Gider pusulası: VUK 234 belgesi, 7 gün sayacı, iptal ve yeniden düzenleme | `Modules/Hurda/HurdaModulu.cs:625` | Milyem Plus, SarrafPro var; tezgâh programlarında yok |
| Şüpheli işlem bildirimi için **10 iş günü** sayacı, ek beyan, referans saklama | `Modules/Masak/*`, `Isler/MasakSibSuresiIsi.cs` | KuyumHub "form hazır"; süre takibi kimsede yok |
| Yaptırım listesi: eşleşen kişiyle işlem **durur** | `Modules/Masak/YaptirimServisi.cs` | KuyumHub, SarrafPro, Terminal Gold |
| Stopaj dönem borcu, tahlil farkı düzeltmesi | `Hurda/StopajModulu.cs`, `TahlilFarkiServisi.cs` | Yok |
| Tarihli yasal sınırlar: değişiklik günü yeni değer kendiliğinden başlar, geçmiş eski sınırla kalır | `Modules/Yasal/*` | Yok. Kivi ve KuyumHub sitelerinde hâlâ 185.000 TL yazıyor |
| Sentetik/laboratuvar taş kapısı (RG 24.06.2026) | `Urun/TasKokeni.cs` | Yok |
| Silinemez denetim izi + ters kayıtla düzeltme | `Modules/Denetim/*`, `StornoServisi.cs` | Terminal Gold (ters kayıt), Kuyumex (log) |
| KVKK: maskeli ve AES-256 şifreli dışa aktarım, süre dolunca anonimleştirme | `Disaaktarim/*`, `SaklamaSuresiIsi.cs` | Yok (yalnız "KVKK uyumlu" beyanı) |
| İki aşamalı giriş (MFA) + rol × modül yetkisi | `Auth/MfaModulu.cs`, `Yetki/*` | SarrafPro, KuyumBase (biyometrik) |
| FIFO/LIFO parti maliyeti | `Maliyet/MaliyetMotoru.cs` | CetaSoft ve Kuyum Plus yalnız ağırlıklı ortalama |
| Vezne kuyruğu (tezgâhtar para tutmaz) | `Vezne/*` | M&L, CetaSoft |
| Reyon yerleşimi, konsinye, emanet has defteri | `Yerlesim/*`, `KonsinyeKapisi.cs`, `Emanet/*` | Kısmen M&L, Terminal Gold |
| Kur TV/vitrin ekranı (canlı) | `Pages/VitrinEkrani.razor` | M&L, SarrafPro, Hasfiyat, ZarifSoft |

---

## 3. Ana karşılaştırma tablosu

İşaretler:
- **Bizde:** ✅ var · 🟡 kısmi · ❌ yok.
- **Paket:** kapatacak iş paketi (§ 5).
- **Boy:** S ≈ 1 paket · M ≈ 2 paket · L ≈ 4+ paket.

### 3.1 Satış / tezgâh

| Özellik | Kimde | Bizde | Paket · Boy |
|---|---|---|---|
| Karma ödeme (nakit, kart, havale, veresiye, döviz, altın) | Dezy, Pusula, Terminal Gold | ✅ `Satis/OdemeSepeti.cs` | — |
| Hurda/takas + satış tek kayıtta ("KARMA") | Terminal Gold, Dezy | ✅ `SatisModulu.cs:3108` | — |
| F tuşlarıyla, fare kullanmadan satış | Terminal Gold, Dezy, Gramino | ✅ F2/F4/F8/F9 | — |
| **İşlem bekletme** (müşteriyi beklet, araya başkasını al) | Kuyumcum.net | ❌ | D1-03 · S |
| **Kur/fiyat kilidi** (satışta ya da kaporada kur dondurma) | Dezy, KuyumBase, WinGold, Daysum | ❌ | D1-04 · M |
| **Eşikli işlem onayı** (büyük iskonto, zararına satış, yüksek tutar patronun telefonuna düşer) | Dezy, KuyumHub | ❌ | D1-05 · M |
| Personel başına iskonto sınırı | Expert Barkod, Dezy | ❌ | D1-05 içinde |
| **Kuyumcunun kendi taksitli/senetli satışı** | Dezy, Pusula | 🟡 yalnız kart taksiti ve çek-senet | D1-06 · M |
| Termal fiş (80 mm) | ZarifSoft, Dezy | ❌ yalnız A4 | D1-01 · S |
| **Fatura PDF / yazdırma** | Hepsi | ❌ fatura için çıktı yok | D1-01 · S |
| Kaparo, iade, değişim, şubeler arası transfer | Çoğu | ✅ | — |
| Kamerayla barkod okuma (telefon) | Kivi, Dezy | ❌ | D3 (mobil) |
| Ödeme linki / QR ile ödeme | KuyumBase, Kivi, Dezy | ❌ | D2-07 · M |

### 3.2 Alış / hurda / atölye

| Özellik | Kimde | Bizde | Paket · Boy |
|---|---|---|---|
| Hurda alımı, ayar/milyem, has hesabı, gider pusulası | Çoğu | ✅ | — |
| Hurda türleri (külçe, eski bilezik, döküntü, çil) ve analiz raporu | Dezy, KuyumHub | 🟡 tür alanı var, analiz raporu yok | D1-15 · S |
| **Hurda alımına fotoğraf** ve terazi ekranı fotoğrafı | Jewel360, Dezy | ❌ yalnız ürüne fotoğraf | D1-12 · S |
| **Eritme, fire, takoz, kalafat, hurdalaştırma** | CetaSoft, KuvvemERP, Expert Barkod, M&L | ❌ "bu sürümde eritme firesi kaydedilmez" | D1-08 · M |
| Atölye iş emri (döküm → montaj → cila, usta, fire, aşama fotoğrafı) | M&L, Dezy, Pusula | ❌ (H-5 kapsam dışı); dış atölye siparişi var | D3-09 · L |
| Pırlanta üretim reçetesi | Terminal Gold, M&L, Pusula | ❌ | D3-09 içinde |

### 3.3 Stok / etiket / donanım

| Özellik | Kimde | Bizde | Paket · Boy |
|---|---|---|---|
| Adet + gram, sayım, reyon, ürün fotoğrafı, 4C, sertifika | Çoğu | ✅ | — |
| **Etiket tasarımcısı** (sürükle-bırak), QR (gram, ayar, sertifika), etikette anlık fiyat | Dezy, ZarifSoft | 🟡 sabit Code128 etiket, tarayıcıdan yazdırma | D1-02 · M |
| Zebra/ZPL ile doğrudan etiket yazıcısı | M&L, CetaSoft, Pusula, Softnorm | ❌ | D1-02 içinde |
| Isıya ve suya dayanıklı kuyum etiketi (sarf malzemesi) | M&L, CetaSoft | — iş ortağı konusu | Karar K-4 |
| Plastik sertifika / garanti kartı / VIP müşteri kartı baskısı | M&L, Dezy, CetaSoft, Expert | ❌ | D1-13 · S |
| **Terazi bağlantısı** (0,001 g, tartı kaydı) | Naron, Dezy, Marg, Pusula (cihaz satışı) | ❌ | D2-09 · M |
| **RFID sayım** ("3 dakikada 1000 ürün") ve hazır set | M&L, Terminal Gold, Softnorm, Kuyumcum, Ariyen, Kuyum Plus, Expert | ❌ (H-6) | D3-05 · L |
| Eşik altı stok bildirimi, ölü/hareketsiz stok | KuyumHub, CetaSoft, ZarifSoft | 🟡 yaşlandırma/devir raporu var, bildirim yok | D1-15 · S |
| Toptancı barkodunu koruma, set oluşturma | Kuyumcum, Expert | ❌ | D1-16 · S |
| 360° görsel, ürün videosu, yapay zekâyla arka plan temizleme | Dezy, Valigara | ❌ | D3-08 · M |
| Yazarkasa / fiziki POS (ÖKC yerine geçen) | Kivi, CetaSoft (Pavo), Dezy | ❌ (H-2) | D2-08 · L |
| KMTS bandrol / seri no | KuyumBase | 🟡 kod alanı var (H-8) | Mevzuat bekliyor (§ 4) |

### 3.4 Cari / kasa / banka

| Özellik | Kimde | Bizde | Paket · Boy |
|---|---|---|---|
| Has-cari, TL/döviz bakiyesi, vade, kredi limiti, çek-senet, ekstre | Çoğu | ✅ | — |
| **Veresiyenin altınla kendiliğinden kapanması** (aynı ayar gelince borç düşer, fazlası emanete) | Dezy | ❌ | D1-07 · M |
| **Hesap dönüştürme** (dolar borcunu altına çevirme, "Bhav Cutting") | Kuyumcum.net, Marg | ❌ | D1-09 · S |
| Müşterinin anlayacağı ekstre ("11 eski tam · 3 çeyrek") | Dezy | 🟡 | D1-09 içinde |
| Cari **mutabakat formu** | Terminal Gold | ❌ (yalnız POS-fatura mutabakatı) | D1-10 · S |
| Risk rengi, kara liste (veresiye engeli) | Dezy | 🟡 kredi limiti var | D1-07 içinde |
| **Kör kasa sayımı**, çalışan başına kasa, kapanışa 30 dk kala bildirim | Dezy | 🟡 gün kapanışında sayılan nakit ve fark gerekçesi var | D1-11 · M |
| Kart blokajı ve komisyon (iç/dış) | CetaSoft, Terminal Gold | ✅ POS defteri, valör | — |
| Banka ekstresinden cari eşleştirme ve faturaya çevirme | Milyem Plus, SarrafPro, Kivi | ✅ CSV üzerinden | — |
| Bankaya özel ekstre biçimleri (MT940, banka CSV'leri) | — | 🟡 tek genel CSV | D2-05 · M |
| **Bankalara canlı bağlantı** (açık bankacılık, 19–30 banka, dakikada bir) | Kivi, Milyem Plus, Terminal Gold, KuyumHub, Dezy, SarrafPro | ❌ (H-4) | D2-06 · L + lisans |
| Havale gelince anlık bildirim | Terminal Gold | ❌ | D2-06 içinde |
| Sanal POS, ödeme linki (SMS/WhatsApp ile) | Kivi, KuyumBase | ❌ | D2-07 · M |

### 3.5 Fatura / muhasebe

| Özellik | Kimde | Bizde | Paket · Boy |
|---|---|---|---|
| Kuyum KDV özel matrah hesabı | Kivi, Terminal Gold, Milyem Plus | ✅ `OzelMatrahMotoru.cs` | — |
| **e-Fatura / e-Arşiv gönderimi** | M&L, Kivi, Dezy, KuyumBase, Expert ve 15+ program | ❌ (H-1) | D2-01…04 · L |
| 805/808/230 kodlarının otomatik konması, has kesme | Kivi | ❌ | D2-02 |
| Gelen havaleden saniyeler içinde otomatik fatura | Milyem Plus, SarrafPro, Kivi | 🟡 havaleden faturaya çevirme elle | D2-04 · S |
| Toplu fatura (çok havale → tek fatura) | Terminal Gold, Hasfiyat | ❌ | D2-04 içinde |
| Gelen e-faturayı içeri alıp stokla eşleme | SarrafPro, Terminal Gold | ❌ | D2-03 · M |
| e-İrsaliye, e-Defter | M&L, Expert | ❌ (irsaliye var, e-belge değil) | D3-12 · L (gerektiğinde) |
| Fatura ve ekstrenin SMS/WhatsApp ile gitmesi | Kivi, Marg | ❌ | D2-10 |
| **Mali müşavir paneli** (yalnız okuma) + Luca/Logo/Mikro dökümü | KuyumHub, Milyem Plus, Kivi, SarrafPro | 🟡 MUHASEBE rolü ve muhasebe fişi CSV'si var | D1-14 · M |
| Paraşüt / Zirve / Mikro bağlantısı | KuyumBase, SarrafPro, Dijimo | ❌ | D3-13 · M |

### 3.6 Uyum

| Özellik | Kimde | Bizde | Paket · Boy |
|---|---|---|---|
| Kimlik eşiği, işlem kapanmadan kimlik | Terminal Gold, KuyumHub | ✅ (eşik 370.000'e çekilecek) | D1-17 · S |
| **Kimlik kartını çipten (NFC) okuma** + fotoğraf | KuyumHub, Kivi | ❌ | D3-02 · L (telefon uygulaması gerekir) |
| İmzalı kimlik tespit belgesi | Kivi | ❌ | D1-13 içinde |
| Parçalı işlem / eşik bölme uyarısı | KuyumHub (Kivi "yakında") | ✅ eşik bölme sinyali `YaptirimServisi.cs:95` | — |
| Yaptırım listesinin her gün kendiliğinden güncellenmesi | KuyumHub | 🟡 elle yükleniyor | D1-18 · M |
| MASAK ve Maliye için hazır denetim raporu | SarrafPro | 🟡 denetim izi var, hazır rapor yok | D1-18 içinde |
| Personel kural uyarıları (peş peşe iade, mesai dışı satış) | KuyumHub | ❌ | D1-05 içinde |
| KVKK rıza kaydı | Terminal Gold | ❌ (aydınlatma metni yok, KV-05) | D1-19 · S |

### 3.7 Müşteri / pazarlama

| Özellik | Kimde | Bizde | Paket · Boy |
|---|---|---|---|
| **SMS / WhatsApp / e-posta gönderimi** | KuyumHub, Terminal Gold, CetaSoft, Kuyum Plus, SarrafPro, Dijimo, Marg | ❌ (H-3); yalnız uygulama içi bildirim | D2-10 · M |
| Doğum günü / yıldönümü hatırlatması, tek tıkla mesaj | KuyumHub, Dezy, Jewel360 | ❌ (doğum tarihi alanı yalnız kimlik için) | D2-11 · S |
| Tamir bitince otomatik mesaj; tamirde önce/sonra fotoğrafı | Terminal Gold, Kuyum Plus, Gem Logic, Dezy | 🟡 tamir siparişi var, mesaj ve fotoğraf yok | D2-11 · S |
| Vade hatırlatma SMS'i | KuyumHub | 🟡 uygulama içi var | D2-11 |
| **Sadakat / puan** (ParaPuan, kart) | CetaSoft, kuyumcuprogrami.com, Marg | ❌ | D2-12 · M |
| Müşteri segmentasyonu, uykuda müşteri listesi | KuyumBase, KuyumHub, Gramino | ❌ | D2-12 içinde |
| Dilek listesi, yüzük ölçüsü | Jewel360, Lightspeed | ❌ | D2-12 içinde |
| **Altın biriktirme planı** (aylık taksit, bonus, hatırlatma, dijital pasbuk) | Hindistan/Körfez (JewelFlow, Marg); KuyumHub "altın birikim hesabı" | ❌ (emanet defteri bunun temelini sağlar) | D3-07 · M |
| Kuyumcunun markasıyla **müşteri uygulaması** (canlı fiyatlı katalog, fiyat alarmı, bildirim, ödeme linki) | Dezy | ❌ | D3-06 · L |
| Müşteriye özel canlı fiyat sayfası / web | Hasfiyat, SarrafPro, M&L | 🟡 vitrin ekranı var, dış web yok | D3-06 içinde (önce S) |
| PDF katalog (WhatsApp'la) | M&L, Dezy, Expert | ❌ | D2-11 içinde |
| Tek gelen kutusu (WhatsApp, Instagram, web formu) | Dijimo | ❌ | D3-14 · M |

### 3.8 Rapor / patron / yapay zekâ

| Özellik | Kimde | Bizde | Paket · Boy |
|---|---|---|---|
| Patron paneli, altın pozisyonu, 16 rapor, kâr-zarar, şube kıyas | Çoğu | ✅ | — |
| **Personel prim / komisyon**, hedef, "dene-gör" | Dezy, KuyumHub, CetaSoft, M&L | ❌ (personel satış raporu var) | D1-20 · M |
| Patronun telefonuna anlık uyarı (şüpheli hareket, kasa farkı) | Pusula PARS, Dezy | ❌ | D3 (mobil) + D1-05 |
| **Yapay zekâ asistanı** (sesli ya da yazılı soru → grafikli cevap) | KuyumHub, Dezy (Becky), Softnorm | ❌ (yalnız aç/kapa bayrağı var) | D3-10 · M |
| Yapay zekâyla ürün metni ve sosyal medya görseli | Dezy, Valigara, Jewel360 | ❌ | D3-08 |
| Talep tahmini / otomatik yeniden sipariş | Valigara, Jewel360 | ❌ | D3-10 içinde |
| Zekât hesabı (has ağırlıktan) | Daysum | ❌ | D3-10 içinde · S |

### 3.9 Platform / teslimat

| Özellik | Kimde | Bizde | Paket · Boy |
|---|---|---|---|
| Çoklu şube, rol, MFA, bulut, yedek, kılavuz | Çoğu | ✅ | — |
| **Telefon/tablet uygulaması** (iOS, Android) | Hemen hepsi | ❌ (tarayıcıda duyarlı ekran) | D3-01 PWA · S; D3-02 yerel · L |
| **İnternet kesilince çalışma** (çevrimdışı satış, sonra eşitleme) | KuyumBase, KuvvemERP, Dezy, Terminal Gold | ❌ | D3-03 · L |
| Excel içe aktarımında sütunların kendiliğinden tanınması | Dezy, Kivi | 🟡 sabit CSV şablonu | D1-16 · S |
| **Rakip programdan veri göçü** (M&L, Pusula, CetaSoft vb.) | Terminal Gold ("veri aktarımı dahil"), Dezy | ❌ | D1-16 + müşteri örneği · M |
| Ücretsiz özellik talebi, WhatsApp destek | Dezy, Kivi | — iş modeli | Karar K-5 |
| Dış sistemlere API, WebSocket kur API'si | SarrafPro, Hasfiyat, Kivi | ❌ | D3-11 · M |
| Kuyumcular odası ortak kur ekranı + reklam alanı | M&L | ❌ | D3-06 içinde |

### 3.10 E-ticaret

| Özellik | Kimde | Bizde | Paket · Boy |
|---|---|---|---|
| Kendi web mağazası (stok ortak, canlı kurla fiyat) | KuyumBase, Terminal Gold, M&L, Hasfiyat | ❌ | D3-04 · L |
| Pazaryeri (Trendyol, Hepsiburada, Amazon, N11, Pazarama, İdefix, Shopify) | Dezy, KuvvemERP, M&L, Terminal Gold | ❌ | D3-04 · L |
| Ani kur dalgalanmasında fiyat "devre kesici" | Platin360 | ❌ | D3-04 içinde |
| Kargo etiketi, sipariş takibi | KuyumBase | ❌ | D3-04 içinde |
| B2B bayi portalı, toptan çanta takibi | M&L, KuvvemERP, Terminal Gold, CetaSoft | ❌ | D3-12 · L |

---

## 4. Taklit **edilmeyecekler** (yanlış, yasak ya da riskli iddialar)

| İddia | Kim | Neden yapmıyoruz |
|---|---|---|
| "MASAK'a otomatik bildirim", "Online MASAK entegrasyonu" | Pazarlama dili (Kivi bile "otomatik göndermiyoruz" diyor) | MASAK'ın kuyumcu için bir bağlantı arayüzü yok. Şifre paylaşımı md.29/2 ile yasak. Bildirimi açıklamanın cezası 5549 md.4/2 ve md.14/1'de (bkz. ARASTIRMA-MASAK-EFATURA). Biz "gönderim yardımcısı" yapıyoruz (D1-18) |
| "KMTS Nisan 2026, tüm kuyumcular" | KuyumBase | Eylülde birincil kaynakla çürütüldü (KM-02). Mevzuat çıkarsa bandrol işi açılır (H-8) |
| "Nakit yasağı, yalnız dijital ödeme" | KuyumBase | Birincil kaynakta yok. Geçerli kural VUK tevsik haddi (30.000 TL); bizde zaten durdurucu kapı var |
| 185.000 TL eşik | KuyumHub, Kivi blog, Terminal Gold | 7.10.2026'dan beri 370.000 TL. Biz tarihli yasal sınırla yönetiyoruz (D1-17) |
| Kuyumcuya özel olmayan genel ERP | KuvvemERP | Bizim üstünlüğümüz kuyuma özel derinlik |

---

## 5. Kapatma planı

Süreler benim çalışma süremdir. Ölçü: paket başına 6–10 saat (yazma + test + tarayıcıda deneme).

### Dalga 1 — Tezgâh ve uyum (dış bağımlılığı yok, hemen başlanabilir)

| Paket | İş | Boy |
|---|---|---|
| D1-01 | Fatura PDF/yazdırma + 80 mm termal fiş şablonu | S |
| D1-02 | Etiket tasarımcısı (sürükle-bırak), QR, anlık fiyat alanı, Zebra ZPL çıktısı | M |
| D1-03 | İşlem bekletme (park) | S |
| D1-04 | Kur/fiyat kilidi (satış, kapora, sipariş) | M |
| D1-05 | Eşikli işlem onayı, personel iskonto sınırı, personel kural uyarıları | M |
| D1-06 | Kuyumcunun kendi taksitli/senetli satışı (vade planı + çek-senet bağı) | M |
| D1-07 | Veresiyenin altınla kendiliğinden kapanması, fazlasının emanete geçmesi, risk rengi, kara liste | M |
| D1-08 | Eritme, fire, takoz, kalafat, hurdalaştırma | M |
| D1-09 | Hesap dönüştürme (döviz ↔ has ↔ TL), müşteri diliyle ekstre | S |
| D1-10 | Cari mutabakat formu | S |
| D1-11 | Kör kasa sayımı, çalışan kasası, kapanış hatırlatması | M |
| D1-12 | Hurda alımına fotoğraf (kamera/dosya) | S |
| D1-13 | Garanti kartı / sertifika / imzalı kimlik tespit belgesi baskısı (ekranda imza) | S |
| D1-14 | Mali müşavir rolü (yalnız okuma) + Luca/Logo/Mikro biçimli döküm | M |
| D1-15 | Eşik altı stok bildirimi, hareketsiz stok ve hurda analizi raporları | S |
| D1-16 | Akıllı içe aktarım (sütunların kendiliğinden tanınması), toptancı barkodunu koruma, set | S |
| D1-17 | 370.000 TL eşik, 12.000 TL fatura sınırı, MASAK metinleri | S |
| D1-18 | MASAK gönderim yardımcısı, bildirim gizliliği, yaptırım listesinin kendiliğinden güncellenmesi, denetim raporu | M+M |
| D1-19 | KVKK aydınlatma ve rıza kaydı (metin hukukçudan) | S |
| D1-20 | Personel prim/komisyon, hedef, "dene-gör" | M |

**Toplam ~30 paket → 180–300 saat.** Takvim: iyimser 3 hafta · gerçekçi 4–5 hafta.

### Dalga 2 — Dış bağlantılar (her biri bir sözleşme ya da hesap bekler)

| Paket | İş | Boy | Dış bağımlılık |
|---|---|---|---|
| D2-01…04 | e-Fatura/e-Arşiv: ayar, UBL-TR, 805/808/230, gönderim, durum, gelen fatura, iptal/iade, toplu fatura, havaleden otomatik fatura | L (~7 paket) | **Entegratör sözleşmesi + test hesabı** |
| D2-05 | Bankaya özel ekstre biçimleri (MT940 + 5 büyük banka) | M | Bankalardan örnek ekstre |
| D2-06 | Bankalara canlı bağlantı (açık bankacılık) | L | **Lisanslı aracı ya da TCMB lisansı** (TEYİTSİZ, araştırılacak) |
| D2-07 | Sanal POS / ödeme linki (iyzico, PayTR vb.) | M | Ödeme kuruluşu üye iş yeri hesabı |
| D2-08 | Fiziki POS / yazarkasa (ÖKC) bağlantısı | L | Cihaz üreticisinin SDK'sı (Pavo, Beko vb.) |
| D2-09 | Terazi bağlantısı (tarayıcıdan seri port) | M | Terazi modeli (AJ, Shimadzu vb.) |
| D2-10 | SMS / WhatsApp / e-posta kanalı | M | SMS firması + **WhatsApp Business onayı (Meta)** |
| D2-11 | Doğum günü, vade, tamir durumu mesajları; tamir fotoğrafı; PDF katalog | S+S | D2-10 |
| D2-12 | Sadakat/puan, segment, uykuda müşteri, dilek listesi, yüzük ölçüsü | M | — |

**Toplam ~20 paket → 120–200 saat** + dış beklemeler (sözleşmeler paralel yürürse 2–4 hafta).

### Dalga 3 — Telefon, donanım ve büyüme

| Paket | İş | Boy | Dış bağımlılık |
|---|---|---|---|
| D3-01 | PWA: ana ekrana eklenir, telefonda tam ekran | S | — |
| D3-02 | Yerel telefon uygulaması: patron ekranı, kamerayla barkod, **NFC ile kimlik kartı okuma**, anlık uyarılar | L | App Store / Google Play hesabı ve onayı |
| D3-03 | Çevrimdışı satış + eşitleme | L | — |
| D3-04 | Web mağazası + pazaryerleri + kargo + fiyat devre kesici | L | Pazaryeri satıcı API hesapları |
| D3-05 | RFID sayım + hazır donanım seti | L | Donanım ortağı (Zebra, Chainway) |
| D3-06 | Kuyumcunun markasıyla müşteri uygulaması ve canlı fiyat sayfası, oda kur ekranı | L | D3-02 |
| D3-07 | Altın biriktirme planı + dijital pasbuk | M | Mali müşavir/hukuk görüşü (mevduat sayılır mı? TEYİTSİZ) |
| D3-08 | 360° görsel, video, yapay zekâyla arka plan temizleme ve ürün metni | M | Yapay zekâ servisi |
| D3-09 | Atölye/imalat iş emri, pırlanta reçetesi (H-5 kalkar) | L | — |
| D3-10 | Yapay zekâ asistanı (soru → grafik), talep tahmini, zekât hesabı | M | Yapay zekâ servisi |
| D3-11 | Dış API, WebSocket kur yayını | M | — |
| D3-12 | B2B bayi portalı, toptan çanta takibi, e-İrsaliye | L | Entegratör |
| D3-13 | Paraşüt / Zirve / Mikro bağlantıları | M | Her birinin API hesabı |
| D3-14 | Tek gelen kutusu (WhatsApp + Instagram + web) | M | Meta onayı |

**Toplam ~30 paket → 180–300 saat** + mağaza onayı ve donanım.

### Genel takvim

| Senaryo | Süre | Varsayım |
|---|---|---|
| İyimser | **6 hafta** | Günde ~12 saat çalışma, sözleşmeler hemen başlar, kesinti yok |
| Gerçekçi | **2,5 ay** | Yeniden çalışma payı, internet kesintileri, dış hesaplar 2–3 hafta |
| Kötü | **4 ay** | Açık bankacılık lisansı, Meta onayı ve mağaza onayları uzar |

> Eylül dersi ("3 gün dersin 33 gün olur"): Bu rakamlar aralık olarak verildi. Bir dalga planından 1 günden fazla kayarsa sorulmadan, nedeniyle birlikte bildirilir.

---

## 6. Kurucudan beklenenler

| # | Karar / girdi | Ne zaman gerekli |
|---|---|---|
| K-1 | **e-Fatura entegratörü** (ve sözleşmeyi kuyumcunun mu, ByGatech'in mi yapacağı) | Dalga 2 başında |
| K-2 | **SMS firması + WhatsApp Business** başvurusu (Meta, ByGatech adına) | Dalga 2 |
| K-3 | **Ödeme kuruluşu** (sanal POS) ve **açık bankacılık aracısı** seçimi | Dalga 2 |
| K-4 | **Donanım ortağı**: Zebra yazıcı, RFID seti, terazi, kuyum etiketi. Satacak mıyız, önerecek miyiz? | Dalga 2–3 |
| K-5 | İş modeli: "ücretsiz özellik talebi" (Dezy gibi), deneme süresi, paketlere göre özellik dağılımı | Dalga 1 bitmeden |
| K-6 | App Store / Google Play geliştirici hesapları (ByGatech A.Ş. adına) | Dalga 3 başında |
| K-7 | Kapsam dışı beyanının (H-1…H-8) kaldırılması. Bugün giriş ekranında "hariç" diye yazıyor; her dalgada ilgili madde kalkar | Her dalga sonunda |
| K-8 | **Sıra onayı:** Dalga 1 → 2 → 3 mü, yoksa e-Fatura öne mi? Mali müşavir "bütün faturalar e-Arşiv" diye teyit ederse **e-Fatura öne alınmalı** (ARASTIRMA-MASAK-EFATURA § 1) | Hemen |

---

## 7. Kaynaklar (rakip beyanları — ana sayfalar)

- **Yerli (grup 1):**
  - M&L: mlyazilim.com (ml_perakende, rfid_sistemi, sertifika_sistemleri, ml_kurpano, ml_e_ticaret, ml_imalat).
  - Pusula: pusulanet.net, pusulax.com.
  - Kivi: kivierp.com (kuyum-superapp, kivibank, kivipay, uyum-ve-risk, fiyatlar).
  - Dezy: dezyapp.com (kuyumcu-pos-sistemi, kuyumcu-veresiye-takip-programi, kuyumcu-kasa-ve-altin-yonetimi, features/atolye, kuyumcu-musteri-uygulamasi, kuyumcu-personel-prim-takibi, pricing).
  - KuyumHub: kuyumhub.com (fiyatlandirma, kuyumcu-musteri-takip-uygulamasi, kuyumcu-personel-takip-uygulamasi).
  - Terminal Gold: terminal.gold (ozellikler, entegrasyonlar, fiyatlandirma, rfid-baslangic-seti).
  - CetaSoft: cetasoft.com.tr/kuyum (modparapuan, modposvezne, modenvanter, satinal).
- **Yerli (grup 2):**
  - kuyumbase.com, milyemplus.com, sarraf.pro (moduller), hasfiyat.com/premium, kuyumcum.net, kuvvem.com, zarifsoft.com, graminoerp.com.tr, expertbarkod.com.
  - softnorm.com, kuyumplus.com, naron.com.tr, ariyenkuyumcu.com, kuyumex.com, kuyumcuprogrami.com, dijimo.com.tr, goldist.com, platin360.com.
- **Yurt dışı:**
  - jewel360.com (+ knowledge.jewel360.com Metal Buy), edgeuser.com, valigara.com, lightspeedhq.com, apps.shopify.com/goldpricer.
  - mdmsolutions.biz/products/jewelflow, tallysolutions.com, auricsuite.com, wingoldnext.com, margsoftware.ae, daysum.net, gem-logic.com.
- **Bizim program:**
  - Koddan taranan dosyalar: `dotnet/Kuyumcu.Api/Modules/*`, `dotnet/Kuyumcu.Web/Pages/*`.
  - Kapsam dışı beyanı: `Kuyumcu.Contracts/Kisit/KisitSozlesmeleri.cs:44-59`.

**Erişilemeyenler:**
- YouTube başlıkları ve Google Play sayfaları kısmen alınamadı.
- M&L, KuyumBase, Milyem Plus ve SarrafPro fiyat yayımlamıyor.
- Bazı alan adları çözülmedi: terminalgold.com, dezy.com.tr, kuvvemerp.com.
