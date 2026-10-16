# Tarayıcı QA turu — 2026-09-16 (uygulama içi tarayıcı, http://localhost:5199)

> Kurucu talimatı: "ekran testlerini tıklayarak benim tarayıcımdan yap, hiçbir ayrıntıyı atlama". Chrome eklentisi bağlı olmadığı için uygulama içi tarayıcı bölmesi kullanılır; giriş parolasını kurucu kendisi yazar (ekip parola girmez). Yöntem her sayfada: (1) sayfaya git, (2) konsol/ağ hatalarını oku, (3) erişilebilirlik ağacını oku (başlık, boş durum, tablo, düğmeler), (4) ekran görüntüsü `ekran/<rota>.png`, (5) her modal/formu aç, doğrulama mesajlarını tetikle, `QA-TEST-` önekli kayıt oluştur → düzenle → (varsa) pasife al, (6) karanlık tema + dar ekran (375px), (7) basit/profesyonel mod anahtarı. Bulgular önceliklendirilip düzeltme paketine (UX-4 devamı) döner.

**Tur 1 tamamlandı (2026-09-16).** 27 rota / 4 paralel grup. Aşağıdaki tablo tur 1 sonuçlarıdır.

| # | Rota | Sayfa | Durum | Konsol/ağ | Bulgular |
|---|---|---|---|---|---|
| 1 | `/giris` | Giriş | BULGU | Konsol temiz; tüm `_framework`/css/js 200/304. Boş ve geçersiz e-postada API'ye istek gitmedi (HTML5 tooltip engelledi). | 3 bulgu (0 KRİTİK). Doğrulama yalnız tarayıcı balonuna dayanıyor, uygulama içi Türkçe mesaj yok (ORTA); sol tanıtım metni yüzük illüstrasyonunun üstüne biniyor (DÜŞÜK); sayfa açık temada da koyu, tema tercihini yok sayıyor (DÜŞÜK). |
| 2 | `/` | Panel | BULGU | Konsol temiz. `auth/ben`, `kur/guncel`, `yetkiler/benim`, `raporlar/gunluk`, `raporlar/aylik`, `masak/ozet`, `gider-pusulalari/sure-takibi` 200; POST `kur/guncelle` 200; PATCH `kur/kaynak` 200; PATCH `kullanicilar/ben` 200. | 3 bulgu. Dar ekranda kenar çubuğu sabit, yatay taşma scrollWidth 516>375, KPI kartları kesik (YÜKSEK); kur güncelleme/kaynak değişiminde geri bildirim yok (DÜŞÜK); basit modda panel KPI'ları sadeleşmiyor (DÜŞÜK). |
| 3 | `/islemler` | İşlemler (POS) | BULGU | Yalnız kasıtlı geçersiz denemelerden 400. POST `islemler/satis` → 400 (stok yok), 400 (tevsik haddi), sonra 200. Diğer tüm GET'ler 200. | 6 bulgu (**1 KRİTİK**). Kaydedilen satış fişi listelenmiyor/düzenlenmiyor/storno yok (**KRİTİK**); boş formda "Satış Kaydet" ve geçersiz kalemde "Ekle" sessizce etkisiz (YÜKSEK); dar ekranda sekmeler dikey yığılıyor, scrollWidth 560>375 (YÜKSEK); ürün listesinde stok bilgisi yok (ORTA); kalem satırı (adet/fiyat) düzenlenemiyor (ORTA); tevsik uyarısı ödeme türü önermiyor (DÜŞÜK). |
| 4 | `/cariler` | Cariler | BULGU | Yalnız kasıtlı 400'ler. POST `cari` 400→201; GET/PATCH `cari/{id}` 200; DELETE `cari/{id}` 200; süzme istekleri 200. | 8 bulgu (**1 KRİTİK**). Pasife alınan cari listede "Aktif" gibi kalıyor, aktife alma yok (**KRİTİK**); "Pasife Al" onaysız tek tıkla (YÜKSEK); doğrulama mesajları İngilizce + teknik alan adı (YÜKSEK); Yeni Cari ile Düzenle formu farklı alan kümesi (ORTA); boş adla Kaydet sessiz, başarı bildirimi yok (ORTA); tür sütunu ham enum, filtre eksik (ORTA); satır başına 5 düğme yığılıyor (ORTA); telefon biçimi doğrulanmıyor (DÜŞÜK). |
| 5 | `/urunler` | Ürünler | BULGU | Yalnız kasıtlı 400'ler. POST `urunler` 201; GET/PATCH/DELETE `urunler/{id}` 200; süzmeler 200. | 7 bulgu (0 KRİTİK). Pasif ürünü aktife alma yolu yok (YÜKSEK); Düzenle formunda Gerçek Milyem/Birim/Konsinye alanları yok (YÜKSEK); boş/geçersiz (gram −5) kayıt sessizce etkisiz (YÜKSEK); iki ayrı "Kategori" alanı kafa karıştırıyor (ORTA); Escape modalı kapatmıyor (ORTA); sütun genişlikleri ve sayı biçimi tutarsız (ORTA); başarı geri bildirimi yok (DÜŞÜK). |
| 6 | `/urunler/agac` | Ürün ağacı | BULGU | GET `urun-kategorileri?sadeceAktif=false`, `reyonlar`, `subeler` 200; POST `urun-kategorileri` 201; PATCH x2 200; POST `reyonlar` 201. | 5 bulgu (**1 KRİTİK**). Reyon oluşturulduktan sonra düzenlenemiyor/pasife alınamıyor, satırda 0 düğme (**KRİTİK**); boş adla Kaydet sessiz (ORTA); dar ekranda başlık harf harf kırılıyor, scrollWidth 539>375 (ORTA); "Sıra" negatif değer kabul ediyor, −3 ile 201 (DÜŞÜK); kategori pasife almada toast yok (DÜŞÜK). |
| 7 | `/urunler/etiketler` | Etiketler | BULGU | GET `urunler?aktif=true&limit=500` 200; oturum boyunca 130+ tekrarlı `kur/guncel` 200. | 5 bulgu (0 KRİTİK). Adet=0 iken "Etiketleri Yazdır (0)" aktif ve boş sayfa yazdırıyor (ORTA); dar ekranda taşma scrollWidth 545>375 (ORTA); `kur/guncel` çok sık yineleniyor (DÜŞÜK, performans); arama sonrası seçim korunuyor mu doğrulanamadı (DÜŞÜK); sayfa başlığında "— Kuyumcu ERP" son eki yok (DÜŞÜK). |
| 8 | `/fiyatlandirma` | Fiyat listeleri | BULGU | Sayfa yüklemede 4xx/5xx yok. POST `fiyat/listeler` 200, POST `fiyat/kampanyalar` 200. | 5 bulgu (**1 KRİTİK**). Fiyat listesi ve kampanya satırları düzenlenemiyor/pasife alınamıyor — tbody'de hiç düğme yok (**KRİTİK**, kurucu şikâyeti doğrulandı); geçersiz girişte sessiz etkisizlik (YÜKSEK); dar ekranda taşma 537>375 (YÜKSEK); başarı geri bildirimi yok (ORTA); tabloda ham enum (DÜŞÜK). |
| 9 | `/kasalar` | Kasalar | BULGU | GET `kasalar` 200; POST (boş ad) 400 İngilizce mesaj; POST/PATCH/DELETE 200; POST `kasalar/{id}/hareket` 200. | 6 bulgu (**1 KRİTİK**). Kasa hareketi düzenlenemiyor/storno yok, geçmişte açıklama görünmüyor (**KRİTİK**); pasif kasa geri aktif edilemiyor, pasifte "+ Hareket" hâlâ tıklanabilir (YÜKSEK); dar ekranda taşma 534>375 (YÜKSEK); sunucu mesajı İngilizce "The Ad field is required." (ORTA); tutar 0'da sessiz etkisizlik (ORTA); düzenlemede döviz değiştirilemiyor (DÜŞÜK). |
| 10 | `/banka` | Banka | BULGU | GET `bankalar`, `bankalar/{id}/hareketler` 200; POST (IBAN 'TR12') 400 + Türkçe mesaj; POST/PATCH 200; POST hareket 200. | 5 bulgu (**1 KRİTİK**). Banka hareketi (dekont) düzenlenemiyor/silinemiyor, tek eylem "Faturaya Dönüştür" (**KRİTİK**); dar ekranda 7 sütunlu tablo taşıyor 841>375 (YÜKSEK); negatif tutarda sessiz etkisizlik (ORTA); hesap pasife alma/silme yok (ORTA); IBAN doğrulaması Türkçe ve doğru (olumlu kayıt). |
| 11 | `/kur` | Kur | HATA | GET `kur/guncel`, `kur/gecmis`, `kur/pariteler` 200; POST `kur/guncelle` 200; POST `kur/manuel` 201. **`kur/gecmis` gövdesi `[]`.** | 6 bulgu (0 KRİTİK, 1 doğrulanmış işlev hatası). Kur geçmişi manuel kayıttan sonra bile boş — bitiş tarihi gün başlangıcı sayılıyor olabilir (YÜKSEK, HATA); manuel kur kaydı düzeltilemiyor/iptal edilemiyor, kiracının güncel kuru MANUAL kaldı (YÜKSEK); dar ekranda taşma 499>375 (YÜKSEK); tarih filtresi arayüzde yok ama metin "Seçilen aralıkta" diyor (ORTA); "Şimdi çek" sonrası geri bildirim yok (ORTA); manuel kurda 0/negatif sessizce reddediliyor (ORTA). |
| 12 | `/emanet` | Emanet | BULGU | GET `cari`, `emanet/{cariId}` 200; POST `emanet/giris` 200; POST `emanet/{cari}/cikis` 200. Hata yok. | 5 bulgu (**1 KRİTİK**). Emanet giriş/çıkış hareketleri düzenlenemiyor, storno yok (**KRİTİK**); dar ekranda taşma 534>375 (YÜKSEK); hareket tablosu ham gram/ayar/taş göstermiyor, yalnız has (ORTA); gram 0'da sessiz etkisizlik (ORTA); müşteri seçilmeden düğmeler aktif görünüyor (DÜŞÜK). |
| 13 | `/sayim` | Sayım | BULGU | GET `sayim`, `subeler`, `reyonlar` 200; POST `sayim` 400 (aynı şubede açık oturum, mesaj gösterildi); POST `sayim/{id}/say` ürün koduyla 404 "Barkod bulunamadı", barkodla 200. | 5 bulgu (0 KRİTİK). "Barkod okut / ürün seç" etiketi var ama ürün seçme yolu yok, ürün kodu 404 veriyor (YÜKSEK); dar ekranda taşma 521>375 — tezgâh senaryosu için kritik (YÜKSEK); oturum adı/kapsamı düzenlenemiyor, listede şube sütunu yok (ORTA); boş adla "Oturumu Aç" sessiz (ORTA); sayılmış satırı "sayılmadı"ya geri alma yok (DÜŞÜK). |
| 14 | `/faturalar` | Faturalar | BULGU | GET `faturalar` 200; POST `faturalar` 200; GET `faturalar/{id}/kalemler` 200; PATCH `faturalar/{id}/durum` 200 (Kes ve İptal); durum filtresi 200. Hata yok. | 6 bulgu (**1 KRİTİK**). TASLAK fatura düzenlenemiyor — cari/KDV/kalem/tutar (**KRİTİK**); "Kes" ve "İptal Et" onaysız, geri alınamaz (YÜKSEK); kalem miktar birimi karışık "1.000 g ADET" (ORTA, HATA); geçersiz faturada sessiz ret (ORTA); 1024px'te bile yatay taşma, rozet/etiket yapışık "TaslakKalemler" (ORTA); Escape modalı kapatmıyor (DÜŞÜK). |
| 15 | `/gider-pusulalari` | Gider pusulaları | BULGU | Sayfaya ait hata yok. GET `gider-pusulalari?limit=100`, `/sure-takibi`, `/{id}` 200; tarih filtresi 200 (boş durum doğru). | 4 bulgu (**1 KRİTİK**). Gider pusulası hiç düzenlenemiyor, "+ Yeni" de yok — yalnız Detay/Yazdır (**KRİTİK**); dar ekranda üst çubuk taşıyor 552>375 (ORTA); stopaj üç yerde üç farklı gösterim (DÜŞÜK); dialog Escape ile kapanmıyor (DÜŞÜK). |
| 16 | `/gider-pusulalari/{Id}/belge` | Gider pusulası belgesi | BULGU | Sayfaya ait hata yok. GET `gider-pusulalari/{id}/belge` 200. | 3 bulgu (0 KRİTİK). Yalnız görüntüleme denetim izine "yazdırıldı" olarak düşüyor (DÜŞÜK, HATA); dar ekranda üst çubuk taşıyor 540>375 — taşan öğeler `.ust-sag`/`.mod-anahtar` (ORTA); belge içeriği tam ve doğru: VUK md.234, yazıyla tutar, imza/kaşe alanları (olumlu kayıt). |
| 17 | `/uyum` | Uyum (tevsik/MASAK) | HATA | `masak/ozet`, `masak/kimlik-tespitleri`, `kimlik-tespitleri/ara`, `masak/supheli` 200; POST `masak/supheli` 201. **"Gönderildi"/"Kapat" tıklamalarında HİÇ istek yok.** | 7 bulgu (**1 KRİTİK**). Kimlik tespiti ve şüpheli bildirim kayıtları düzenlenemiyor/silinemiyor (**KRİTİK**); "Gönderildi"/"Kapat" düğmeleri sessizce hiçbir şey yapmıyor — 10 iş günlük MASAK süresi izlenemez (YÜKSEK, HATA); belge no araması 0 sonuçta eski listeyi gösteriyor (ORTA, HATA); Kaydet disabled ama neden söylenmiyor (ORTA); dar ekranda taşma 520>375 (ORTA); not alanının etiketi yanlış metin/i18n karışması (DÜŞÜK); gerekçe kesiliyor, tam metin yolu yok (DÜŞÜK). |
| 18 | `/raporlar` | Raporlar | HATA | Sayfaya ait konsol hatası yok. `raporlar/aylik`, `cari-yaslandirma`, `stok-degeri`, `pos-fatura-mutabakati` 200; ay filtresi çalışıyor. | 5 bulgu (0 KRİTİK, 2 sayısal tutarsızlık). Gün sonu mutabakatı kendi içinde çelişiyor: SATIŞ FATURASI ₺68.000 vs Faturasız 4 · ₺173.200, FARK ₺105.200 (YÜKSEK, HATA); aylık "NET HAS GRAM 8,12" günlük paneldeki "53,04 has" ile uyumsuz — aylık toplam tek günden küçük olamaz (YÜKSEK, HATA); aynı ay için iki farklı kâr: −31.800 vs +20.200 (ORTA); dar ekranda taşma 542>375 (ORTA); sayı biçimi karışık, has noktalı TL virgüllü (DÜŞÜK). |
| 19 | `/raporlar/kar-zarar` | Kâr/zarar | BULGU | Bilinçli ters tarih için 2×400, Türkçe mesaj gösterildi. `kar-zarar`, `kar-zarar/urun`, `stok-degerlemesi` 200. | 4 bulgu (0 KRİTİK). Dar ekranda taşma 475>375 (ORTA); "Maliyeti bilinen: 5,000" adet para gibi biçimlenmiş (DÜŞÜK); geçersiz tarih aralığı sunucuya iki kez gönderiliyor (DÜŞÜK, performans); TL/HAS bacağı ayrımı + dönem kısayolları + ürün marjı iyi tasarım (olumlu kayıt). |
| 20 | `/raporlar/vade-takibi` | Vade takibi | BULGU | Bilinçli −1 için 1×400 → "Ufuk gün sayısı negatif olamaz". `vade-takibi?ufukGun=7/60` 200. | 4 bulgu (0 KRİTİK). Varsayılan 7 günlük ufukta 76.000 ₺ açık alacak üç kartta da ₺0,00 görünüyor, toplam açık alacak hiçbir kartta yok (ORTA); dar ekranda taşma 519>375 (ORTA); telefon `tel:` bağlantısı ve cari kart linki yok (DÜŞÜK); 7 gün varsayılanı gizli, hızlı seçim yok (DÜŞÜK). |
| 21 | `/denetim-izi` | Denetim izi | BULGU | `denetim/ozet`, `denetim?limit=100`, filtreler 200. **Her filtre değişiminde aynı istek 2 kez gidiyor.** | 6 bulgu (0 KRİTİK). Ters tarih aralığı sessizce kabul, özet 0'a düşüyor, mesaj yok (ORTA); dar ekranda taşma 541>375 (ORTA); filtre değişiminde çift istek (DÜŞÜK, performans); kullanıcı adı ile rol bitişik "QA TestSAHIP" (DÜŞÜK); onay kutusunun erişilebilir adı "on" (DÜŞÜK, erişilebilirlik); kayıt türü listesi ham entity adları (DÜŞÜK). |
| 22 | `/veri-disa-aktarim` | Veri dışa aktarım | BULGU | `disa-aktarim/kaynaklar`, `disa-aktarim/gecmis` 200. İlk açılışta süresi dolmuş token: 3× POST `auth/yenile` 401 → 3× `auth/cikis` 204 + Blazor hata bandı. Panel kaynaklı 2× SVG `<path> attribute d` konsol hatası. | 4 bulgu (0 KRİTİK). Süresi dolmuş tokenda 3× yenile/çıkış döngüsü ve "Beklenmeyen bir hata oluştu" bandı (YÜKSEK, HATA); gerekçe <20 karakterde düğme sessizce pasif, mesaj yok (ORTA); dar ekranda taşma 541>375 (ORTA); kişisel veri içeren kapsamlar varsayılan işaretli — KVKK (DÜŞÜK). **Not:** "Hazırla ve İndir" gerçek indirme politika gereği tıklanmadı. |
| 23 | `/kullanicilar` | Kullanıcılar | BULGU | GET `kullanicilar` 200; PATCH `kullanicilar/ben` 200 (düzenle + geri al). Yeni hata yok. | 4 bulgu (0 KRİTİK). Kullanıcı rolü ve aktif/pasif durumu düzenlenemiyor, satırda Pasife Al/Aktife Al yok (ORTA, DÜZENLENEBİLİRLİK); Yeni Kullanıcı formunda doğrulama mesajı yok, Kaydet sessizce pasif (ORTA); dar ekranda taşma 742>375 (ORTA); kendi satırında "(Hoş geldin)" etiketi anlamsız (DÜŞÜK). **Not:** kullanıcı oluşturma ve şifre değiştirme politika gereği test edilmedi. |
| 24 | `/yetkiler` | Yetkiler | BULGU | GET `yetkiler`, `yetkiler/benim` 200; PUT `yetkiler` 200 (2×). Kalıcılık yenilemeyle doğrulandı. Yeni hata yok. | 3 bulgu (0 KRİTİK). Matris masaüstünde bile karta sığmıyor, son sütun kesik — table.scrollWidth 883 > 734 (ORTA); dar ekranda taşma 534>375 (ORTA); kaydedilmemiş değişiklik uyarısı yok, Kaydet her zaman aktif (DÜŞÜK). Başarı bandı "Yetkiler güncellendi!" çalışıyor (olumlu). |
| 25 | `/isletme` | İşletme | BULGU | GET `admin/tenant`, `subeler` 200; PATCH `admin/tenant` 200 (2×); POST/GET/PATCH/DELETE `subeler` 200. Yeni hata yok. | 7 bulgu (**1 KRİTİK**). İşletme adı düzenlenemiyor, salt metin (**KRİTİK**); pasife alınan şube geri aktife alınamıyor (YÜKSEK); şube telefonu doğrulanmıyor, 'abc' 200 ile kabul (ORTA, HATA); plan/arayüz modu sessizce kaydediliyor (ORTA); Yeni Şube formunda doğrulama mesajı yok (ORTA); dar ekranda taşma 662>375 (ORTA); şube CRUD sonrası başarı mesajı yok (DÜŞÜK). |
| 26 | `/ayarlar` | Ayarlar (tema, mod) | BULGU | PATCH `kullanicilar/ben` 200 (3×); GET `auth/ben` 200. Tema localStorage `kuyumcu-tema`, ağ isteği yok. Yeni hata yok. | 3 bulgu (0 KRİTİK). Profil Ad Soyad/E-posta burada salt okunur, düzenleme yalnız `/kullanicilar`da — SAHİP olmayan kendi adını hiç düzenleyemeyebilir (ORTA, DÜZENLENEBİLİRLİK); dar ekranda taşma 529>375 (ORTA); tema/mod seçimi sessizce kaydediliyor (DÜŞÜK). **Not:** şifre değiştirme politika gereği test edilmedi. |
| 27 | `/kilavuz` | Kılavuz | HATA | Konsol: "Framing 'http://localhost:5199/' violates … frame-ancestors 'none'. The request has been blocked." HEAD `/kilavuz.html` 200 ama başlıklar `frame-ancestors 'none'` + `X-Frame-Options: DENY`. | 4 bulgu (**1 KRİTİK**). Sayfa tamamen boş: iframe CSP + X-Frame-Options ile bloke (**KRİTİK**, HATA); yüklenemediğinde kullanıcıya açıklama/yedek bağlantı yok (YÜKSEK); dar ekranda taşma 531>375 (ORTA); iframe arka planı karanlık temada sabit beyaz (DÜŞÜK). Puanlar: görsel 1/5, kullanıcı dostu 1/5 — turun en kötü sayfası. |

## Çapraz kontroller
- [x] Karanlık tema (prefers-color-scheme + data-tema) tüm sayfalarda okunaklı — 27/27 rotada uygulandı (body bg `rgb(11,18,32)`), kontrast sorunu yok. **İstisna:** `/kilavuz` iframe'i sabit beyaz; `/giris` açık temada da koyu kalıyor (tema tercihini yok sayıyor).
- [ ] **Dar ekran 375px: yatay kaydırma yok, menü daralıyor — BAŞARISIZ (27/27 rotada yatay kaydırma).** scrollWidth 475–1163 > 375; kenar çubuğu sabit kalıyor, ana içerik 207px'e sıkışıyor. Taşan öğeler grup-3 ölçümünde `DIV.ust-sag`, `DIV.mod-anahtar`. Tek düzeltme (Layout üst bar + kenar çubuğu çekmecesi) 27 sayfayı birden çözer — turun en yüksek getirili işi.
- [x] Basit / profesyonel mod anahtarı (#ust-mod-anahtar): menü daralıyor — çalışıyor (Basit modda menü 7 kaleme iniyor, PATCH `kullanicilar/ben` 200, kalıcı). **Eksik:** Panel içeriği basit modda sadeleşmiyor (aynı 4 KPI + grafik).
- [x] Oturum süresi dolunca 401 → giriş sayfasına yönlendirme — yönlendirme çalışıyor, **ama** temiz değil: süresi dolmuş tokenla açılışta 3× `auth/yenile` 401 + 3× `auth/cikis` 204 ve Blazor "Beklenmeyen bir hata oluştu." bandı (`/veri-disa-aktarim`, YÜKSEK). Ayrıca refresh token rotasyonu paralel gruplarda yarışa girdi (tek kullanımlık) — sonraki turlar için **grup başına ayrı token çifti** üretilmeli.
- [ ] **Her kaydet akışında başarı bandı — BAŞARISIZ.** Yalnız `/islemler` ("İşlem başarıyla kaydedildi!"), `/kullanicilar` ve `/yetkiler` ("güncellendi!") bant gösteriyor; cariler, ürünler, ağaç, fiyatlandırma, kasalar, banka, kur, emanet, faturalar, işletme, ayarlar sessiz. Sunucu hatası "kayıt yok" gibi gösterilmiyor (bu kısmı OK), ancak **geçersiz formda "sessiz etkisizlik"** 15+ ekranda tekrar ediyor.

## Bulgu özeti — TUR 1 (2026-09-16)

### Sayılar
| Ölçüt | Değer |
|---|---|
| Gezilen rota | **27** (4 paralel grup: 7+7+7+6) |
| Toplam bulgu | **133** |
| KRİTİK | **12** |
| YÜKSEK | 27 |
| ORTA | ~54 |
| DÜŞÜK | ~40 (4'ü olumlu kayıt) |
| Ortalama görsel puanı | **3,85 / 5** (toplam 104/135) |
| Ortalama kullanıcı dostu puanı | **3,07 / 5** (toplam 83/135) |
| Düzenlenemeyen veri girişi kalemi | **26** |
| Kendiliğinden oluşan konsol/ağ hatası | Yalnız `/kilavuz` CSP ihlali + Panel'de 2× SVG `path d` uyarısı; diğer tüm 4xx'ler kasıtlı geçersiz denemelerden |

En düşük puanlı sayfalar: `/kilavuz` (1/1), `/fiyatlandirma` (4/2), `/faturalar` (3/2), `/uyum` (4/2). En yüksek: `/gider-pusulalari/{id}/belge` (5/4), `/raporlar/kar-zarar` (5/4).

### KRİTİK bulgular (12) — rota · kanıt · öneri
1. **`/kilavuz` sayfası tamamen boş** — iframe `src="/kilavuz.html"`; konsol: "Framing … violates frame-ancestors 'none'"; yanıt başlıkları `frame-ancestors 'none'` + `X-Frame-Options: DENY`; `main` içinde hiç metin yok. → Kılavuzu iframe yerine Blazor sayfasında render et, ya da yalnız `/kilavuz.html` yolu için `frame-ancestors 'self'` + `SAMEORIGIN` istisnası (global ayarı bozmadan).
2. **`/islemler` — kaydedilen satış fişi listelenmiyor, düzenlenemiyor, storno yok** — "İşlem başarıyla kaydedildi!" sonrası fiş no/ID gösterilmiyor, sayfada hareket listesi ve Düzenle/Storno yok (satış `01M2MS26KW3S36C28EB1PPRFQW` UI'dan geri alınamadı). → "Bugünkü işlemler" tablosu + satır başına Görüntüle/Düzenle/Storno; bantta fiş numarası.
3. **`/cariler` — pasife alınan cari listede "Aktif" gibi kalıyor, aktife alma yok** — DELETE `cari/01M2MS4EMV9HSJKZTZ941ND90P` 200, GET `cari/{id}` → `aktif:false`, ama liste satırı rozetsiz duruyor. → Varsayılan aktif süzgeci + "Pasifleri göster" + "Aktife Al".
4. **`/urunler/agac` — reyon oluşturulduktan sonra düzenlenemiyor/pasife alınamıyor** — "QA-TEST Vitrin 1" satırında button/a/input sayısı 0, satır tıklanabilir değil. → PATCH `/reyonlar/{id}` + kategori benzeri Düzenle modalı.
5. **`/fiyatlandirma` — fiyat listesi ve kampanya satırları düzenlenemiyor/pasife alınamıyor** — her iki tablonun tbody'sinde hiç düğme yok (`btns:[]`). *(Kurucu şikâyeti doğrulandı.)* → Satır başına Düzenle (PATCH) + Aktif/Pasif anahtarı + varsayılan liste değiştirme akışı.
6. **`/kasalar` — kasa hareketi düzenlenemiyor/storno edilemiyor** — Geçmiş modalında tek düğme "×", açıklama sütunu yok; yanlış girilen 150 TL düzeltilemedi. → Geçmiş satırında Düzenle/Storno (ters kayıt) + açıklama sütunu.
7. **`/banka` — banka hareketi (dekont) düzenlenemiyor/silinemiyor** — satırdaki tek eylem "🧾 Faturaya Dönüştür"; "Bekliyor" durumu manuel değiştirilemiyor. → Düzenle + Sil/Storno + eşleştirme eylemi.
8. **`/emanet` — emanet giriş/çıkış hareketleri düzenlenemiyor, storno yok** — hareket tablosunda satır eylemi yok (`btns:[]`); düzeltmenin tek yolu ters iade girmek, o da "İade" olarak görünüyor (defter yanlış okunuyor). → Satıra Düzenle/Storno (ters hareket + açıklama).
9. **`/faturalar` — TASLAK fatura düzenlenemiyor** — FTR-2026-000002 taslak satırında yalnız "Kalemleri Göster / Kes / İptal Et"; taslak tanımı gereği düzenlenebilir olmalı. → Taslakta "Düzenle" (aynı modal, PUT), kesildikten sonra kilit.
10. **`/gider-pusulalari` — kayıt hiç düzenlenemiyor, "+ Yeni" de yok** — detay modalı salt metin (satıcı ad/adres/TCKN, gram, ayar, stopaj); satırda yalnız Detay/Yazdır. VUK belgesi, yanlış TCKN düzeltilemiyor. → Detayda "Düzenle" (denetim izli).
11. **`/uyum` — kimlik tespiti ve şüpheli işlem bildirimi kayıtları düzenlenemiyor/silinemiyor** — kimlik tespiti modalı salt okunur; QA-TEST bildirimi (250.000 ₺, ACIK) iptal/sil edilemedi. KVKK md.4/1-d doğruluk ilkesi. → ACIK durumda tutar/gerekçe düzenlenebilsin, yanlış bildirim iptal edilebilsin.
12. **`/isletme` — işletme adı düzenlenemiyor** — "İşletme adı: QA-TEST-1 Kuyumculuk" düz metin, textbox/Düzenle yok; PATCH `/admin/tenant` zaten mevcut. → Ad/vergi no/adres için Düzenle diyaloğu.

### YÜKSEK bulgular (27) — özet
- **Kabuk/duyarlılık (27 rota):** 375px'te yatay kaydırma, kenar çubuğu sabit — tek düzeltmeyle hepsi kapanır.
- **`/uyum`:** "Gönderildi" / "Kapat" düğmeleri hiçbir istek atmıyor (MASAK 10 iş günü izlenemez) — **işlev hatası**.
- **`/kur`:** kur geçmişi manuel kayıttan sonra bile boş (`[]`) — **işlev hatası**; manuel kur geri alınamıyor (kiracı kuru MANUAL kaldı).
- **`/raporlar`:** gün sonu mutabakatı çelişkili (₺105.200 açıklanmayan fark); aylık NET HAS GRAM 8,12 < günlük 53,04 — **sayısal hata**.
- **`/veri-disa-aktarim`:** süresi dolmuş tokenda 3× yenile/çıkış döngüsü + Blazor hata bandı.
- **Pasiften geri dönüş yok:** `/urunler`, `/kasalar`, `/isletme` (şube) — yanlış pasife alma kalıcı.
- **Onaysız yıkıcı eylem:** `/cariler` "Pasife Al" (onaysız), `/faturalar` "Kes"/"İptal Et" (onaysız, geri alınamaz).
- **Sessiz etkisizlik:** `/islemler`, `/fiyatlandirma`, `/urunler` — boş/geçersiz formda Kaydet hiçbir mesaj vermeden hiçbir şey yapmıyor.
- **Doğrulama mesajları İngilizce:** "The Email field is not a valid e-mail address.", "The field KrediLimitiTl must be between…", "The Ad field is required."
- **`/urunler`:** Düzenle formunda Gerçek Milyem / Birim / Konsinye alanları yok.
- **`/sayim`:** ürün kodu kabul edilmiyor (404), ürün seçme yolu yok — barkodu bilmeyen tezgâhtar hiçbir şey sayamıyor.
- **`/kilavuz`:** yüklenemediğinde kullanıcıya hiçbir açıklama/yedek bağlantı yok.

### Düzenlenemeyen veri girişleri (UX-1 ihlalleri, 26 kalem)
| # | Rota | Düzenlenemeyen kayıt / alan |
|---|---|---|
| 1 | `/islemler` | Satış fişi — kayıt sonrası görüntüle/düzenle/storno yolu yok |
| 2 | `/islemler` | Satış kalemi (adet / birim fiyat) — satır içi düzenleme yok, yalnız Sil |
| 3 | `/urunler` | Gerçek Milyem — düzenleme formunda yok |
| 4 | `/urunler` | Birim (Adet/Gram) — düzenleme formunda yok |
| 5 | `/urunler` | Konsinye bayrağı — düzenleme formunda yok |
| 6 | `/urunler` | Aktif/pasif — pasife alındıktan sonra geri alınamıyor |
| 7 | `/cariler` | Aktif/pasif — pasife alma geri alınamıyor, listede görünmüyor |
| 8 | `/urunler/agac` | Reyon (ad, şube, tip, aktif) — Düzenle/Pasife Al yok |
| 9 | `/fiyatlandirma` | Fiyat listesi (ad/tür/marj/çarpan/varsayılan) |
| 10 | `/fiyatlandirma` | Kampanya (ad/tarih/hedef/indirim) |
| 11 | `/kasalar` | Kasa hareketi (tutar/tür/açıklama) — Düzenle/Storno yok |
| 12 | `/kasalar` | Kasa dövizi — düzenleme modalında yok |
| 13 | `/kasalar` | Pasif kasa — geri aktif etme yok |
| 14 | `/banka` | Banka hareketi/dekont (yön/tutar/karşı taraf/açıklama/durum) |
| 15 | `/banka` | Banka hesabı — pasife alma/silme yok |
| 16 | `/kur` | Manuel kur kaydı — düzeltme/iptal yok |
| 17 | `/emanet` | Emanet giriş hareketi (ayar/gram/taş/açıklama) |
| 18 | `/emanet` | Emanet iade hareketi |
| 19 | `/sayim` | Sayım oturumu adı/kapsamı/şubesi |
| 20 | `/sayim` | Sayılmış satırı "sayılmadı"ya geri alma |
| 21 | `/faturalar` | Taslak fatura (cari/tür/KDV/toplam/açıklama/kalemler) |
| 22 | `/faturalar` | Fatura kalemi (miktar/fiyat/işçilik/indirim) |
| 23 | `/gider-pusulalari` | Gider pusulası (satıcı ad/TCKN/adres/mal cinsi/gram/ayar/brüt/stopaj/teslim tarihi) — yeni de yok |
| 24 | `/uyum` | Kimlik tespiti (ad, belge türü/no, uyruk, adres, tutar, beyan) — salt okunur |
| 25 | `/uyum` | Şüpheli işlem bildirimi (tutar/işlem kimliği/gerekçe) + durum düğmeleri çalışmıyor |
| 26 | `/kullanicilar` `/isletme` `/ayarlar` | Kullanıcı rolü & aktif/pasif; işletme adı; şube pasif→aktif; profil ad/e-posta (Ayarlar'da salt okunur) |

### Tekrarlayan kalıplar (tek düzeltme = çok sayfa)
1. **Kabuk duyarlılığı** — 27/27 rota, 375px yatay kaydırma. Kenar çubuğu çekmecesi + `.ust-sag`/`.mod-anahtar` sarmalama.
2. **Sessiz etkisizlik** — boş/geçersiz formda Kaydet/Ekle hiçbir mesaj vermeden etkisiz (15+ ekran). Ortak `EditForm` + `ValidationMessage` altyapısı.
3. **İngilizce DataAnnotations mesajları** — `ErrorMessage` Türkçeleştirmesi tek geçişte.
4. **Başarı bandı/toast yokluğu** — ortak `Toast` bileşeni, tüm CRUD sonrası.
5. **Escape modalı kapatmıyor** — `/urunler`, `/faturalar`, `/gider-pusulalari`, `/uyum`. Ortak dialog bileşenine keydown + odak tuzağı.
6. **Pasiften geri dönüş yok** — cari, ürün, kasa, şube. Ortak "Aktife Al" deseni.
7. **Ham enum gösterimi** — cari türü, fiyat listesi türü, denetim izi kayıt türleri. Ortak etiket sözlüğü.
8. **Sayı biçimi tutarsızlığı** — has gram noktalı ("8.1200"), TL virgüllü; "1.000 g ADET"; "5,000 adet". Tek tr-TR biçimleyici.
9. **Gereksiz/çift istek** — `kur/guncel` 130+ kez; denetim izi filtresinde çift istek; geçersiz tarih aralığında 2× 400.

### Düzeltme paketine giren maddeler (öncelik sırası)
| Paket | Kapsam | Gerekçe |
|---|---|---|
| **UX-4e — Kabuk duyarlılığı** | Kenar çubuğu <768px çekmece, üst bar sarmalama, tabloları kart görünümüne indirme | Tek düzeltme, 27 rota; çapraz kontrol başarısız |
| **UX-4f — Kılavuz KRİTİK** | `/kilavuz` iframe kaldır → Blazor render (ya da yol bazlı CSP istisnası) + hata durumunda yedek bağlantı | Sayfa tamamen kullanılamaz; güvenlik ayarı bozulmadan çözülmeli |
| **UX-4g — Düzenlenebilirlik dalgası 1 (KRİTİK)** | Reyon, fiyat listesi, kampanya, taslak fatura, işletme adı için Düzenle/PATCH | Kurucu şartı UX-1; 5 KRİTİK |
| **UX-4h — Düzenlenebilirlik dalgası 2 (mali kayıtlar)** | Satış fişi listesi + storno, kasa hareketi storno, banka dekontu düzenle/sil, emanet storno | 4 KRİTİK; mali defter düzeltilemiyor, denetim izli storno şart |
| **UX-4i — Uyum/VUK düzenlenebilirlik** | Gider pusulası düzenle, kimlik tespiti düzelt, şüpheli bildirim düzenle/iptal **ve "Gönderildi"/"Kapat" düğmelerinin işlev hatası** | 2 KRİTİK + 1 YÜKSEK işlev hatası; MASAK 10 iş günü ve KVKK md.4/1-d |
| **UX-4j — Doğrulama ve geri bildirim altyapısı** | Ortak EditForm/ValidationMessage, Türkçe ErrorMessage, Toast bandı, yıkıcı eylemlerde onay modalı, Escape ile modal kapatma | 5 tekrarlayan kalıp; "sessiz etkisizlik" 15+ ekranda |
| **UX-4k — Pasif/aktif yaşam döngüsü** | Cari, ürün, kasa, şube için "Aktife Al" + pasif rozeti + pasif süzgeci; pasif kayıtta yıkıcı düğmeleri gizle | 1 KRİTİK + 3 YÜKSEK; yanlış pasife alma kalıcı |
| **UX-4l — Rapor/veri doğruluğu (HATA)** | Gün sonu mutabakatı fark açıklaması, aylık NET HAS GRAM kaynağı, kur geçmişi bitiş tarihi gün sonu, iki farklı "kâr" adlandırması, fatura kalemi birim biçimi | Doğrulanmış sayısal/işlev hataları; regresyon testi ile |
| **UX-4m — Oturum/401 hijyeni** | Tek noktadan 401 yakalama (tek yenile → tek çıkış → /giris), "Oturumunuz sona erdi" mesajı, Blazor error boundary tetiklenmemesi | YÜKSEK; kullanıcıya jenerik hata bandı gösteriliyor |
| **UX-4n — Alan doğrulama ve giriş kalitesi** | Telefon biçimi (cari + şube), kategori "Sıra" negatif reddi, etiket adedi min=1, ürün stok bilgisi combobox'ta, sayım ürün kodu/ad ile arama | ORTA yoğunluklu ama veri kalitesini doğrudan etkiliyor |
| **UX-4o — Performans/gürültü** | `kur/guncel` tek merkezden 30-60 sn, denetim izi çift istek, geçersiz aralıkta istemci tarafı engelleme, Panel SVG `path d` uyarısı | DÜŞÜK; 136 istekten büyük kısmı gereksiz |
| **UX-4p — Cilalama** | Ham enum etiketleri, tr-TR sayı biçimleyici, `/giris` illüstrasyon çakışması, sütun genişlikleri, sayfa başlıkları, erişilebilir etiketler, "(Hoş geldin)" → "(siz)" | Görsel puanı 3,85 → hedef 4,5 |

### Tur 1 notları
- Kod değiştirilmedi, git komutu çalıştırılmadı, derleme/test koşulmadı, 5198 portuna dokunulmadı. Tüm gözlemler `http://localhost:5199` üzerinden.
- **Politika gereği yapılmayanlar (kurucunun elle doğrulamasına bırakıldı):** `/veri-disa-aktarim` "Hazırla ve İndir" gerçek dosya indirme; `/kullanicilar` yeni kullanıcı oluşturma (parola girişi); `/ayarlar` ve `/kullanicilar` şifre değiştirme.
- **Sonraki tur için:** her gruba **ayrı token çifti** üretilsin — tek kullanımlık refresh token rotasyonu paralel gruplarda yarışa girdi (grup 2 ve 4 yeniden giriş yapmak zorunda kaldı).
- **Temizlik:** fikstür kiracısı `QA-TEST-1 Kuyumculuk` (`01M2MRP101HYXTB07ZH95A3F6Z`) **korundu** — sonraki turlar ve kurucuya gerçekçi örnek için kalıcı. `SELECT COUNT(*) FROM tenants` → **6**. Kiracıda bırakılan QA-TEST verisi: satış `01M2MS26KW3S36C28EB1PPRFQW`, pasif cari `01M2MS4EMV9HSJKZTZ941ND90P`, pasif ürün `01M2MS8CJZ8SFTDQJJC9VT96N6`, kategori `01M2MSBB2DT8PEPCX5RJPJAWNX` (sıra −3), reyon "QA-TEST Vitrin 1", fiyat listesi + kampanya, pasif "QA-TEST Euro Kasası", Ana Kasa'ya +150 TL hareket, banka hesabı + 1.250 TL dekont, emanet ±has, **MANUAL kur 6.750 (kiracının güncel kuru MANUAL kaynaklı!)**, açık "QA Eylül Sayımı", fatura FTR-2026-000002 (iptal), MASAK şüpheli bildirim (ACIK, 250.000 ₺), pasif "QA-TEST Şube Kadıköy".

## Fikstür (API) bulguları — tur 1 (2026-09-16 12:36, fikstür ajanı: gerçekçi veri tohumlama)
Kiracı `QA-TEST-1 Kuyumculuk` (KALICI, sonraki turlarda yeniden kullanılır): 1 şube, 2 kasa (TRY/USD), fiyat listesi + kampanya, 8 ürün, 5 cari, banka hesabı + hareket, 3 alış (MASAK kimlik dâhil), 3 satış (nakit/kart/veresiye), hurda alış (gider pusulası otomatik), tahsilat, emanet, KUYUM fatura, açık sayım. 16 tohum adımı başarılı; 7 sorun (→ **paket UX-4d**, dalga 2b başı):
1. Ürün kartında açılış stoğu yolu yok — stoksuz ürün satılamıyor, önce alış gerekiyor. (YÜKSEK, kullanılabilirlik)
2. MASAK açıklama hata mesajı hangi alanı kastettiğini söylemiyor. (ORTA)
3. MASAK kimlik türü mesajı kabul edilen değerleri listelemiyor; DTO'da doğrulama yok. (ORTA)
4. MASAK kümülatif eşik mesajı tutarsız (ilk denemede toplam yok). (DÜŞÜK)
5. Hurda alışta NAKIT varsayılanı tevsik haddi yüzünden çoğu işlemde geçersiz. (ORTA, UX-S21)
6. Banka hareketi yön sözlüğü modüller arası tutarsız (GIRIS/CIKIS vs GELEN/GIDEN). (ORTA)
7. `/api/v1/*` altında bilinmeyen yol 200 HTML (SPA fallback) dönüyor. (ORTA, güvenlik/hijyen)
