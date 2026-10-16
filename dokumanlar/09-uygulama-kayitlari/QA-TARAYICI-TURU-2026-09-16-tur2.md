# Tarayıcı QA turu — TUR 2 — 2026-09-16 (uygulama içi tarayıcı, http://localhost:5199)

> Yöntem tur 1 ile aynı: (1) sayfaya git, (2) konsol/ağ hatalarını oku, (3) erişilebilirlik ağacını oku, (4) ekran görüntüsü, (5) her modal/formu aç, doğrulama mesajlarını tetikle, `QA-TEST-…T2/G2` önekli kayıt oluştur → düzenle → (varsa) pasife al, (6) karanlık tema + dar ekran (375px), (7) basit/profesyonel mod anahtarı. Kiracı: fikstür kiracısı `01M2N4TMYRFXBFZN1N5DTRM8HP` ("QA-TEST-2 Kuyumculuk", KALICI).
>
> **Tur 1 dosyası (`QA-TARAYICI-TURU-2026-09-16.md`) tarihsel tabandır, değiştirilmemiştir.**
>
> Not: ilk özet 90 KB kesilmiş girdiyle yazılmıştı; `qa-turu-2.json` ham kaydından tam veriyle düzeltildi (Madde 1). Bu turda **27 rotanın 27'si** ölçülmüştür (grup 1: 7, grup 2: 7, grup 3: 7, grup 4: 6).

| # | Rota | Sayfa | Durum | Konsol/ağ | Bulgular |
|---|---|---|---|---|---|
| 1 | `/giris` | Giriş | BULGU | Yeni konsol hatası yok. Boş form ve `gecersiz-eposta` ile Giriş Yap → API'ye hiç istek gitmedi (HTML5 balonu engelledi). Şifre girilmedi (politika). | 2 bulgu (0 KRİTİK). Doğrulama yalnız tarayıcı balonuna dayanıyor, uygulama içi Türkçe mesaj yok (ORTA); sol tanıtım metni yüzük illüstrasyonunun üstüne biniyor (DÜŞÜK). **KAPANDI:** açık temada koyu kalma (light → `rgb(248,250,252)`). |
| 2 | `/` | Panel | BULGU | Yeni hata yok. `auth/ben`, `kur/guncel`, `yetkiler/benim`, `kur/kaynaklar`, `raporlar/gunluk`, `raporlar/aylik`, `masak/ozet`, `gider-pusulalari/sure-takibi` 200; POST `kur/guncelle` 200; PATCH `kur/kaynak` 200; PATCH `kullanicilar/ben` 200 (×2). | 3 bulgu (0 KRİTİK). Dar ekranda kenar çubuğu sabit, scrollWidth 516>375, hamburger yok (YÜKSEK); "Kuru güncelle" başarı bildirimi yok, kaynak değişiminde hata bandı var — tutarsız (DÜŞÜK); basit mod yalnız menüyü daraltıyor (DÜŞÜK). |
| 3 | `/islemler` | İşlemler (POS) | BULGU | Yeni hata yok. `cari`, `urunler`, `subeler`, `masak/esikler`, `kasalar` 200; POST `islemler/satis` 200; GET `islemler/iade-adaylari?tur=SATIS` 200; POST `islemler/satis-iade` 200. Boş formda ve adet=0/fiyat=−5'te hiç istek yok. | 5 bulgu (**1 KRİTİK**). Satış fişi listelenmiyor/düzenlenmiyor/storno yok — tek yol "Satış İadesi" ile ters kayıt (**KRİTİK**); boş formda Kaydet ve geçersiz kalemde Ekle sessizce etkisiz (YÜKSEK); dar ekranda sekmeler dikey yığılıyor, scrollWidth 537>375 (YÜKSEK); kalem satırı (adet/fiyat) düzenlenemiyor (ORTA); ürün listesinde stok bilgisi yok (ORTA). |
| 4 | `/cariler` | Cariler | BULGU | Yalnız kasıtlı 400. POST `cari` 400→201; GET/PATCH/DELETE `cari/{id}` 200; süzmeler 200. | 8 bulgu (**1 KRİTİK**). Pasife alınan cari listede "Aktif" gibi kalıyor, "Aktife Al" yok (**KRİTİK**); "Pasife Al" onaysız tek tıkla (YÜKSEK); doğrulama mesajları İngilizce/teknik (YÜKSEK); dar ekranda tablo taşıyor 939>375 (YÜKSEK); boş adla Kaydet sessiz + başarı bildirimi yok (ORTA); tür sütunu ham enum, filtre eksik (ORTA); Escape modalı kapatmıyor (ORTA, **yeni**); telefon biçimi doğrulanmıyor (DÜŞÜK). **KAPANDI:** Yeni Cari ↔ Düzenle alan farkı. |
| 5 | `/urunler` | Ürünler | HATA | **Konsol: "Stok" modalı her açılışta `NullReferenceException at Kuyumcu.Web.Pages.Urunler.<BuildRenderTree>b__0_9 → ModalPencere` (2 kez yeniden üretildi), hata bandı görünür oldu.** POST `urunler` 201; GET/PATCH/DELETE `urunler/{id}` 200; `urunler/{id}/stok` 200. | 9 bulgu (0 KRİTİK, 1 doğrulanmış çalışma zamanı hatası). **Stok modalı NRE fırlatıyor + "Beklenmeyen bir hata oluştu" bandı (YÜKSEK, YENİ)**; pasif ürünü aktife alma yok (YÜKSEK); Düzenle formunda Gerçek Milyem/Birim yok (YÜKSEK); boş/geçersiz kayıt sessizce etkisiz (YÜKSEK); dar ekranda tablo taşıyor 993>375 (YÜKSEK); Stok modalı salt okunur, stok girişi yok (ORTA, **yeni**); iki ayrı "Kategori" alanı (ORTA); Escape kapatmıyor (ORTA); başarı bildirimi yok (DÜŞÜK). **KISMEN:** Konsinye artık Düzenle'de. |
| 6 | `/urunler/agac` | Ürün ağacı | BULGU | Yeni hata yok. `urun-kategorileri?sadeceAktif=false`, `reyonlar`, `subeler` 200; POST `urun-kategorileri` 201; PATCH ×2 200; POST `reyonlar` 201. Boş adla Kaydet → istek yok. | 6 bulgu (**1 KRİTİK**). Reyon oluşturulduktan sonra düzenlenemiyor/pasife alınamıyor, satırda 0 düğme (**KRİTİK**); boş adla Kaydet sessiz (ORTA); Escape kapatmıyor (ORTA, **yeni**); dar ekranda başlık 4 satıra kırılıyor, 539>375 (ORTA); "Sıra" −3 kabul ediyor (DÜŞÜK); kategori pasif/aktif çalışıyor ama toast yok (DÜŞÜK). |
| 7 | `/urunler/etiketler` | Etiketler | BULGU | Yeni hata yok. `urunler?aktif=true&limit=500` 200; son 40 isteğin ~20'si tekrarlı `kur/guncel`. | 4 bulgu (0 KRİTİK). Adet=0 iken "Etiketleri Yazdır (0)" aktif ve boş yazdırıyor (ORTA); dar ekranda taşma 545>375 (ORTA); sayfa başlığında "— Kuyumcu ERP" yok (DÜŞÜK); `kur/guncel` çok sık yineleniyor (DÜŞÜK). **KAPANDI:** arama sırasında seçim korunuyor. |
| 8 | `/fiyatlandirma` | Fiyat listeleri | BULGU | Sayfa yüklemede 4xx/5xx yok. POST `fiyat/listeler` 200, PATCH ×3 200, POST `fiyat/kampanyalar` 200, PATCH ×3 200. Geçersiz gönderimlerde istek yok. | 5 bulgu (**0 KRİTİK — tur 1'in KRİTİK'i KAPANDI**). Geçersiz girişte sessiz etkisizlik (YÜKSEK); dar ekranda taşma 560>375 (YÜKSEK); "Pasife al" onaysız (ORTA); "kaydedildi" bandı kalıcı ve pasife almadan sonra da aynı yazı (DÜŞÜK); tabloda ham enum (DÜŞÜK). **KAPANDI:** satır Düzenle/Pasife al/Aktifleştir + başarı bandı geldi. |
| 9 | `/kasalar` | Kasalar | BULGU | GET `kasalar` 200; POST (boş ad) 400 İngilizce; POST/PATCH/DELETE 200; POST `kasalar/{id}/hareket` 200 ×2; GET `hareketler` 200. | 10 bulgu (**1 KRİTİK**). Kasa hareketi düzenlenemiyor/storno yok, açıklama sütunu yok (**KRİTİK**); pasif kasa aktife alınamıyor, pasifte "+ Hareket" aktif (YÜKSEK); dar ekranda taşma 556>375 (YÜKSEK); İngilizce "The Ad field is required." (ORTA); tutar 0'da sessiz etkisizlik (ORTA); **döviz kasasında tutarlar ₺ ile gösteriliyor (ORTA, yeni, HATA)**; **Ana Kasa −144.996 TL negatif bakiye uyarısız (ORTA, yeni)**; Escape kapatmıyor (ORTA, **yeni**); düzenlemede döviz değiştirilemiyor (DÜŞÜK); bakiye>0'da pasife alma kilidi + onay modalı (DÜŞÜK, **olumlu/yeni**). |
| 10 | `/banka` | Banka | BULGU | GET `bankalar`, `bankalar/{id}/hareketler` 200; POST (IBAN 'TR12') 400 + **Türkçe** mesaj; POST/PATCH 200; POST hareket 200. | 5 bulgu (**1 KRİTİK**). Dekont düzenlenemiyor/silinemiyor, tek eylem "Faturaya Dönüştür" (**KRİTİK**); dar ekranda tablo taşıyor 877>375 (YÜKSEK); negatif tutarda sessiz etkisizlik + toast yok (ORTA); hesap pasife alma/silme yok (ORTA); açılışta 2 sn "Yükleniyor…" + menü 3 kaleme daralıp geri açılıyor (DÜŞÜK, **yeni**). **KAPANDI:** IBAN doğrulaması Türkçe. |
| 11 | `/kur` | Kur | HATA | `kur/guncel`, `kur/pariteler` 200; **`kur/gecmis…&bitis=2026-09-16` gövdesi `[]`, curl ile `bitis=2026-09-17` → bugünün kayıtları geliyor (kök neden doğrulandı)**; POST `kur/guncelle` 200; POST `kur/manuel` 201. | 6 bulgu (0 KRİTİK, 1 doğrulanmış işlev hatası). **Kur geçmişi boş: `bitis` gün başı olarak yorumlanıyor, bugün dışlanıyor (YÜKSEK, HATA — kök neden bu turda bulundu)**; manuel kur kaydı düzeltilemiyor/silinemiyor (YÜKSEK); dar ekranda taşma 499>375 (YÜKSEK); tarih filtresi arayüzde yok (ORTA); manuel kurda 0/negatif sessiz ret + toast yok (ORTA); **sarrafiyede alış>satış satırı (Hamit Altın) uyarısız (DÜŞÜK, yeni)**. **KAPANDI:** "Şimdi çek" toast'ı geldi. |
| 12 | `/emanet` | Emanet | BULGU | GET `cari`, `emanet/{cariId}` 200; POST `emanet/giris` 200; POST `emanet/{cari}/cikis` 200. Fazla iadede istemci Türkçe mesajı, istek yok. | 5 bulgu (**1 KRİTİK**). Emanet giriş/çıkış hareketleri düzenlenemiyor, storno yok (**KRİTİK**); dar ekranda taşma 557>375 (YÜKSEK); tablo ham gram/ayar/taş göstermiyor, yalnız has (ORTA); gram 0'da sessiz etkisizlik + toast yok (ORTA); **müşteri arama kutusuna yazınca seçim "Müşteri seçin"e dönüyor ama alttaki veri eski müşterininki kalıyor (ORTA, yeni, HATA)**. **KAPANDI:** düğmeler müşteri seçilene kadar devre dışı. |
| 13 | `/sayim` | Sayım | BULGU | GET `sayim`, `subeler`, `reyonlar`, `urun-kategorileri/duz` 200; POST `sayim` 400 (aynı şubede açık oturum, Türkçe); POST `say` 404 (ürün kodu), 200 (barkod), 400 İngilizce (adet −1), 200. | 7 bulgu (0 KRİTİK). Ürün seçme yolu yok, ürün kodu 404 (YÜKSEK); dar ekranda taşma 521>375 (YÜKSEK); oturum adı/kapsamı düzenlenemiyor, şube sütunu yok (ORTA); boş adla "Oturumu Aç" sessiz (ORTA); **negatif adet 400 ama mesaj İngilizce/teknik (ORTA, yeni)**; sayılmış satır "sayılmadı"ya döndürülemiyor (DÜŞÜK); **sayım sonrası bant yok, odak barkoda dönmüyor (DÜŞÜK, yeni)**. **KAPANDI (kısmen):** Şube alanı artık ön dolu. |
| 14 | `/faturalar` | Faturalar | BULGU | GET `faturalar` 200; POST 200; GET `kalemler` 200; PATCH `durum` 200 ×2; durum filtreleri 200; **GET `islemler/fis/{id}` 404 (fikstürdeki 3 satış kimliği de curl ile 404)**. | 8 bulgu (**1 KRİTİK**). TASLAK fatura düzenlenemiyor (**KRİTİK**); "Kes"/"Ödendi İşaretle" onaysız, Ödendi geri alınamıyor (YÜKSEK); **"Fişten Fatura Kes" 26 haneli ULID istiyor, seçici yok, fikstür kimlikleri 404 (YÜKSEK, yeni)**; cari boşken Kaydet sessiz (ORTA); kalem birimi "1.000 g ADET" (ORTA, HATA); liste masaüstünde bile taşıyor 1099, mobilde 1141 (ORTA); başarı bandı yok (DÜŞÜK, **yeni**); Escape kapatmıyor (DÜŞÜK). **KISMEN:** kalem-toplam uyuşmazlığı için doğru uyarı + Kaydet kilidi geldi. |
| 15 | `/gider-pusulalari` | Gider pusulaları | BULGU | Sayfaya ait yeni hata yok. GET `gider-pusulalari?limit=100`, `/sure-takibi`, tarih filtresi 200 (boş durum doğru). | 5 bulgu (**1 KRİTİK**). Kayıt hiç düzenlenemiyor, "+ Yeni" de yok — yalnız Detay/Yazdır (**KRİTİK**); dar ekranda taşma 575>375 (ORTA); stopaj üç yerde üç gösterim (DÜŞÜK); Escape kapatmıyor (DÜŞÜK); **detayda "Satıcı tipi" ham enum `NIHAI_TUKETICI` (DÜŞÜK, yeni)**. |
| 16 | `/gider-pusulalari/{Id}/belge` | Gider pusulası belgesi | BULGU | Sayfaya ait hata yok. GET `/belge` 200. Başlık "GP-2026-000001 — Gider Pusulaları". | 2 bulgu (0 KRİTİK). Yalnız görüntüleme denetim izine "yazdırıldı" düşüyor (DÜŞÜK, HATA); dar ekranda üst çubuk taşıyor 540>375 (ORTA). **KAPANDI:** belge içeriği tam ve doğru (VUK md.234, yazıyla tutar, imza/kaşe). |
| 17 | `/uyum` | Uyum (tevsik/MASAK) | HATA | `masak/ozet`, `kimlik-tespitleri`, `masak/supheli` 200; POST `masak/supheli` 201; **GET `kimlik-tespitleri/ara?belgeNo=…` 200 ama istemci yanıtı çözemedi (alert)**; "Gönderildi"/"Kapat" tıklamalarında **hiç istek yok**. | 9 bulgu (**1 KRİTİK**). Kimlik tespiti ve şüpheli bildirim düzenlenemiyor/silinemiyor (**KRİTİK**); **tam belge no araması istemcide çöküyor: `DeserializeUnableToConvertValue` — API `{adet,kayitlar}` zarfı, istemci `List<…>` bekliyor (YÜKSEK, YENİ)**; "Gönderildi"/"Kapat" hiçbir istek atmıyor (YÜKSEK, HATA); kısmi belge no aramasında sessizlik (ORTA); Kaydet disabled ama neden söylenmiyor (ORTA); dar ekranda taşma 520>375 (ORTA); not alanının etiketi yanlış metin (DÜŞÜK); gerekçe kesiliyor (DÜŞÜK); Escape kapatmıyor (DÜŞÜK, **yeni**). |
| 18 | `/raporlar` | Raporlar | HATA | Sayfaya ait konsol hatası yok. `raporlar/aylik?yil=2026&ay=9/8`, `cari-yaslandirma`, `stok-degeri`, `pos-fatura-mutabakati` 200; ay filtresi çalışıyor. | 6 bulgu (0 KRİTİK, 3 sayısal tutarsızlık). Gün sonu mutabakatı çelişkili: SATIŞ FATURASI ₺142.500 vs "Faturalı 0 · ₺0,00", FARK ₺26.500; tahsilat 90.850 vs satış 169.000 (YÜKSEK, HATA); aylık "NET HAS GRAM" kartının anlamı belirsiz, panel/kâr-zarar ile uyumsuz (ORTA, HATA); iki farklı "kâr" (ORTA); **aylık API `veresiye.toplamTl=0` iken ekranda ₺98.300 VERESIYE satış (ORTA, yeni, HATA)**; dar ekranda taşma 542>375 (ORTA); sayı biçimi karışık (DÜŞÜK). |
| 19 | `/raporlar/kar-zarar` | Kâr/zarar | BULGU | Bilinçli ters tarih için 1×400 → "Bitiş tarihi başlangıçtan önce olamaz". `kar-zarar`, `/urun`, `stok-degerlemesi` 200; "Geçen ay" 200 (boş durum). | 5 bulgu (0 KRİTİK). **HAS hasılatı aylık API ile uyuşmuyor: 24,8404 g vs 23,657 has — aynı 4 satış (ORTA, yeni, HATA)**; dar ekranda taşma 515>375 (ORTA); "Maliyeti bilinen: 5,000" adet para gibi biçimli (DÜŞÜK); geçersiz aralık sunucuya gidiyor (DÜŞÜK); **ürün adı ile "maliyet eksik" rozeti bitişik okunuyor (DÜŞÜK, yeni, erişilebilirlik)**. **KAPANDI:** TL/HAS ayrımı + dönem kısayolları iyi tasarım. |
| 20 | `/raporlar/vade-takibi` | Vade takibi | BULGU | Bilinçli −1 için 1×400 → "Ufuk gün sayısı negatif olamaz". `vade-takibi?ufukGun=7/60` 200. | 4 bulgu (0 KRİTİK). Varsayılan 7 günlük ufukta ₺78.300 açık alacak üç kartta da ₺0,00 (ORTA); dar ekranda taşma 541>375 (ORTA); telefon `tel:` ve cari kart linki yok (DÜŞÜK); 7 gün varsayılanı gizli (DÜŞÜK). |
| 21 | `/denetim-izi` | Denetim izi | HATA | `denetim/ozet`, `denetim?limit=100`, filtreler 200. **Her filtre değişiminde aynı istek 2 kez ([3754]/[3755], [3758]/[3759]).** | 6 bulgu (0 KRİTİK). **Çift istek artık çift satır üretiyor: tbody 200 satır / 98 tekil, "Daha fazla göster (200 / 454)" tıklanmadan (YÜKSEK — tur 1'de DÜŞÜK/performanstı, bu turda veri bütünlüğü algısına dönüştü)**; ters tarih aralığı sessizce kabul (ORTA); dar ekranda taşma 541>375 (ORTA); "QA TestSAHIP" bitişik (DÜŞÜK); onay kutusunun erişilebilir adı "on" (DÜŞÜK); kayıt türü listesi ham entity adları (DÜŞÜK). |
| 22 | `/veri-disa-aktarim` | Veri dışa aktarım | BULGU | `disa-aktarim/kaynaklar`, `disa-aktarim/gecmis` 200; bu turda hiç 401 yok. Sayfaya ait yeni konsol hatası yok. | 4 bulgu (0 KRİTİK). Süresi dolmuş tokenda 3× yenile/çıkış döngüsü + Blazor hata bandı (YÜKSEK — **bu turda yeniden üretilemedi, kapanış kanıtı yok**); gerekçe <20 karakterde düğme sessizce pasif (ORTA); dar ekranda taşma 541>375 (ORTA); kişisel veri kapsamları varsayılan işaretli (DÜŞÜK). **Not:** "Hazırla ve İndir" politika gereği tıklanmadı. |
| 23 | `/kullanicilar` | Kullanıcılar | BULGU | Yeni konsol hatası yok. GET `kullanicilar` 200 (×3), PATCH `kullanicilar/ben` 200 (×2: Soyad "Test"→"Test QA-TEST-düzenlendi" ve geri alma; listede doğrulandı). 4xx/5xx yok. | 5 bulgu (0 KRİTİK). Yeni Kullanıcı formunda geçersiz e-posta/boş alanda doğrulama mesajı yok, Kaydet sessizce pasif (ORTA); listede "Pasife Al/Aktife Al" ve Düzenle'de rol/aktiflik alanı yok (ORTA); dar ekranda yatay kaydırma (ORTA); "(Hoş geldin)" etiketi anlamsız (DÜŞÜK); düzenleme sonrası başarı bildirimi yakalanamadı (DÜŞÜK, **yeni**). **KISMEN:** rol artık Yeni Kullanıcı'da seçilebiliyor, Düzenle'de yok. **Politika gereği:** Yeni Kullanıcı kaydı (şifre alanı zorunlu) yapılmadı. |
| 24 | `/yetkiler` | Yetkiler | BULGU | Yeni hata yok. GET `yetkiler` 200, GET `yetkiler/benim` 200, PUT `yetkiler` 200 (×4: Müdür→Kullanıcılar kaldır/geri al; sayfa yenilemesiyle kalıcılık doğrulandı, son durum `true`). 4xx/5xx yok. | 4 bulgu (0 KRİTİK). Yetki matrisi masaüstünde bile karta sığmıyor, KULLANICILAR sütunu yatay kaydırma istiyor (ORTA); dar ekranda yatay kaydırma (ORTA); kirli durum takibi yok, Kaydet değişiklik olmadan da aktif (DÜŞÜK); başarı bandı ~1 sn görünüp kayboluyor (DÜŞÜK, **yeni**). |
| 25 | `/isletme` | İşletme | BULGU | Yeni hata yok. GET `admin/tenant`, GET `subeler` 200; POST `subeler` 200; GET/PATCH/DELETE `subeler/{id}` 200 (şube oluştur → düzenle → onaylı pasife al). Plan değişiminde PATCH ağ listesinde görünmedi ama GET `admin/tenant` `plan=PRO` döndürdü (kalıcılık API ile doğrulandı). | 8 bulgu (**1 KRİTİK**). İşletme adı düzenlenemiyor, salt metin (**KRİTİK**); pasife alınan şube arayüzden aktife alınamıyor (YÜKSEK); şube telefonu doğrulanmıyor ("abc" kabul edildi) (ORTA); plan/varsayılan arayüz modu sessizce kaydediliyor (ORTA); Yeni Şube formunda doğrulama mesajı yok, Kaydet sessizce pasif (ORTA); kur kaynağı ve maliyet yöntemi API'de var, ekranda düzenlenemiyor (ORTA, **yeni**); dar ekranda yatay kaydırma (ORTA); şube ekleme/düzenleme/pasife alma sonrası başarı mesajı yok (DÜŞÜK). |
| 26 | `/ayarlar` | Ayarlar (tema, mod) | BULGU | Yeni hata yok. GET `auth/ben` 200; PATCH `kullanicilar/ben` 200 (×2: Basit → Profesyonel, `arayuzModu` doğrulandı). Tema `localStorage kuyumcu-tema='karanlik'` (ağ isteği yok), Sistem'e geri alındı. 4xx/5xx yok. | 4 bulgu (0 KRİTİK). Profil Ad Soyad/E-posta Ayarlar'da salt okunur, düzenleme yalnız `/kullanicilar`'da (ORTA); dar ekranda yatay kaydırma (ORTA); arayüz modu/tema sessizce kaydediliyor, geri bildirim yok (DÜŞÜK); "Şifreyi Güncelle" üç alan boşken de etkin (DÜŞÜK, **yeni**). **Politika gereği:** şifre değiştirme denenmedi. |
| 27 | `/kilavuz` | Kılavuz | HATA | **Konsol: 2× "Framing 'http://localhost:5199/' violates … `frame-ancestors 'none'`. The request has been blocked."** GET `/kilavuz.html` 200 [FAILED: `net::ERR_BLOCKED_BY_RESPONSE`]; HEAD başlıkları `frame-ancestors 'none'` + `X-Frame-Options: DENY`. Önceki turdan bu yana değişiklik yok. | 4 bulgu (**1 KRİTİK**). Sayfa tamamen boş: iframe içeriği CSP `frame-ancestors 'none'` + XFO DENY ile engelleniyor (**KRİTİK**); yüklenemediğinde kullanıcıya açıklama/yedek bağlantı yok (YÜKSEK); dar ekranda yatay kaydırma (ORTA); iframe arka planı karanlık temada sabit beyaz (DÜŞÜK). Puanlar: görsel 1/5, kullanıcı dostu 1/5 — turun en kötü sayfası. |

## Çapraz kontroller (tur 2)
- [x] **Karanlık tema** — 27/27 rotada uygulandı (`body` bg `rgb(11,18,32)`, metin `rgb(232,238,246)`), kontrast sorunu yok. `/giris` artık tema tercihine uyuyor (**tur 1 bulgusu kapandı**). İki bilinçli/bilinçsiz istisna: gider pusulası belgesi beyaz (yazdırılabilir, kabul edilebilir) ve `/kilavuz` iframe'i sabit beyaz (bulgu).
- [ ] **Dar ekran 375px: yatay kaydırma yok — BAŞARISIZ (26/27 rotada taşma bulgusu; yalnız `/giris`'te yok).** scrollWidth 499–1141 > 375; kenar çubuğu 240px sabit, hamburger/çekmece öğesi yok (`[class*=hamburger]`/menü `aria-label` DOM'da yok). Tur 1'e göre **hiç değişmedi** — hâlâ turun en yüksek getirili tek düzeltmesi.
- [x] **Basit / profesyonel mod anahtarı** — çalışıyor (PATCH `kullanicilar/ben` 200, menü 7 kaleme iniyor). **Eksik (değişmedi):** Panel içeriği basit modda sadeleşmiyor.
- [~] **Oturum/401 hijyeni** — bu turda grup 2 ve grup 3 fikstür tokenı süresi dolmuş bulup e-posta+şifre ile yeniden giriş yaptı; grup 3 süre dolumunda 3× `auth/yenile` 401 + 3× `auth/cikis` 204 + Blazor hata bandı desenini **yeniden gözlemledi**. `/veri-disa-aktarim` üzerinde yeniden üretilemedi. **Kapanmadı.**
- [ ] **Her kaydet akışında başarı bandı — BAŞARISIZ (kısmi ilerleme).** Geldi: `/fiyatlandirma` ("kaydedildi", ama bant kalıcı ve eylemden bağımsız aynı yazı), `/kur` ("güncellendi" toast, kendiliğinden kapanıyor — en iyi örnek), `/yetkiler` ("Yetkiler güncellendi!", ama ~1 sn görünüp kayboluyor). Hâlâ sessiz: cariler, ürünler, ağaç, kasalar, banka, emanet, sayım, faturalar, kullanıcılar, işletme/şube, ayarlar (tema/mod), panel "Kuru güncelle".
- [ ] **Escape ile modal kapatma — BAŞARISIZ.** Denendiği 7 rotanın 7'sinde başarısız: `/cariler`, `/urunler`, `/urunler/agac`, `/kasalar`, `/faturalar`, `/gider-pusulalari`, `/uyum`. Ortak `ModalPencere` bileşeninde tek düzeltme.
- [ ] **Geçersiz formda / eksik veride sessiz etkisizlik — BAŞARISIZ.** 17 rotada tekrarlandı: islemler, cariler, urunler, urunler/agac, fiyatlandirma, kasalar, banka, kur, emanet, sayim, faturalar, uyum, denetim-izi, veri-disa-aktarim, kullanicilar, isletme, ayarlar.

## Tur 1 → Tur 2 karşılaştırması

### Önceki bulguların durumu (grup sonuçlarındaki `onceki_bulgular` alanından)
| Grup | Takip edilen önceki kalem | KAPANDI | KISMEN | AÇIK |
|---|---|---|---|---|
| Grup 1 (giris, /, islemler, cariler, urunler, agac, etiketler) | 37 | 3 | 7 | 27 |
| Grup 2 (fiyatlandirma, kasalar, banka, kur, emanet, sayim, faturalar) | 52 | 7 | 2 | 43 |
| Grup 3 (gider-pusulalari, belge, uyum, raporlar, kar-zarar, vade-takibi, denetim-izi) | 33 | 2 | 2 | 29 |
| Grup 4 (veri-disa-aktarim, kullanicilar, yetkiler, isletme, ayarlar, kilavuz) | 25 | 0 | 2 | 23 |
| **Toplam (27 rota)** | **147** | **12** | **13** | **122** |

> Not (Madde 1): yukarıdaki sayım, `qa-turu-2.json` içindeki `onceki_bulgular` **kalem** sayısıdır (bazı kalemler hem bulgu hem "düzenlenemeyen giriş" olarak iki kez izlenmiştir; bu yüzden grupların genel notlarındaki "33 bulgu / 25 bulgu" ifadelerinden farklı olabilir). Oran: önceki kalemlerin **%8,2'si kapandı**, **%8,8'i kısmen düzeldi**, **%83,0'ü açık**.

**KAPANAN 12 kalem (kanıtla):** `/giris` tema tercihi · `/cariler` Yeni Cari↔Düzenle alan farkı · `/urunler/etiketler` arama sırasında seçim korunması · `/fiyatlandirma` satır Düzenle/Pasife al/Aktifleştir + fiyat listesi ve kampanya satır düzenlemesi + kayıt sonrası başarı bandı (toplam 4 kalem) · `/banka` IBAN Türkçe doğrulama · `/kur` "Şimdi çek" geri bildirimi · `/emanet` müşteri seçilmeden düğmelerin devre dışı olması · `/gider-pusulalari/{id}/belge` belge içeriğinin tamlığı (VUK md.234) · `/raporlar/kar-zarar` TL/HAS ayrımı + dönem kısayolları.

**KISMEN düzelen 13 kalem:** `/giris` illüstrasyon çakışması · `/` kur geri bildirimi (yalnız hata bandı geldi) · `/islemler` tevsik uyarısı (ödeme türü önerilmiyor) · `/cariler` 5 düğme yığını · `/urunler` Düzenle formu (Konsinye eklendi, Milyem/Birim yok) · `/urunler` iki "Kategori" alanı · `/urunler` tablo sütun genişlikleri/sayı biçimi · `/sayim` oturum adı/kapsamı + şube sütunu (şube ön dolumu geldi) · `/faturalar` geçersiz fatura reddi (kalem-toplam kuralı geldi, zorunlu alan sessizliği sürüyor) · `/uyum` belge no aramasında sessizlik · `/raporlar` NET HAS GRAM etiketi · `/veri-disa-aktarim` süresi dolmuş token döngüsü (bu turda yeniden üretilemedi) · `/kullanicilar` rol & aktif-pasif (rol artık Yeni Kullanıcı'da var, Düzenle'de yok).

### Düzenlenemeyen veri girişi listesi — fark
| | Tur 1 | Tur 2 |
|---|---|---|
| Kalem sayısı | 26 (27 rota) | **30** (27 rota, 16 rotaya dağılmış) |

- **Kapanan 3 kalem:** `/fiyatlandirma` fiyat listesi · `/fiyatlandirma` kampanya · `/urunler` Konsinye bayrağı.
- **Yeni 3 kalem:** `/urunler` şube stoğu (Stok modalı salt okunur, stok girişi yok) · `/faturalar` "Ödendi" durumu geri alınamıyor · `/isletme` kur kaynağı ve maliyet yöntemi (API'de var, ekranda kontrol yok).
- **Ayrıştırmadan gelen +4:** tur 1'in 26 numaralı satırı dört rotayı tek kalemde topluyordu (`/kullanicilar` `/isletme` `/ayarlar`); tur 2'de bunlar tek tek ölçülüp 5 ayrı kaleme ayrıldı (kullanıcı rolü · kullanıcı aktif/pasif · işletme adı · şube pasif→aktif · profil ad/e-posta). Yeni bir kusur değil, **aynı kusurun ayrıntılandırılmasıdır.**
- 26 − 3 (kapanan) + 4 (ayrıştırma) + 3 (yeni) = **30**. Diğer 23 kalem aynen sürüyor.

### Puan farkları
| Ölçüt | Tur 1 (27 sayfa) | Tur 2 (27 sayfa) | Fark |
|---|---|---|---|
| Ortalama görsel | 3,85 (104/135) | **3,85** (104/135) | **0,00** |
| Ortalama kullanıcı dostu | 3,07 (83/135) | **3,15** (85/135) | **+0,08** |

> Aynı 27 sayfalık taban üzerinde kıyas: **görsel puan hiç değişmedi** (kabuk duyarlılığına dokunulmadığı için beklenen sonuç), kullanıcı dostu puanı yalnız **+2 ham puan** kazandı (`/fiyatlandirma` 2→4 düzenlenebilirlik kazanımı, `/uyum` ve `/faturalar` puanlarının düşmesiyle kısmen dengelendi). Not: grup 1 tarayıcı bölmesi tur ortasında 604px'e daraldığı için görsel puanları bilinçli olarak tur 1 ile aynı tuttu.

### Yeni bulgu sayısı
**33 yeni bulgu** (`bulgular` içindeki başlığın, aynı rotanın `onceki_bulgular` başlıklarıyla benzerliği %60'ın altında kalanlar): grup 1'de 7, grup 2'de 15, grup 3'te 7, grup 4'te 4. Bunlardan **2'si YÜKSEK ve çalışma zamanı hatası**: `/urunler` Stok modalı NRE; `/uyum` belge no aramasının istemcide çökmesi. Ayrıca YÜKSEK yeni kalemler: `/kur` kur geçmişi `bitis` kök nedeni, `/faturalar` "Fişten Fatura Kes" seçici yokluğu, `/denetim-izi` çift isteğin çift satıra dönüşmesi (tur 1'de DÜŞÜK'tü — şiddet artışı).

## Bulgu özeti — TUR 2 (2026-09-16)

### Sayılar
| Ölçüt | Değer |
|---|---|
| Gezilen rota | **27 / 27** (4 paralel grup: 7+7+7+6) |
| Toplam bulgu | **149** |
| KRİTİK | **11** |
| YÜKSEK | **31** |
| ORTA | **63** |
| DÜŞÜK | **44** (1'i olumlu kayıt) |
| Yeni bulgu | **33** |
| Kapanan önceki kalem | **12** · Kısmen **13** · Açık **122** (takip edilen 147 kalem) |
| Ortalama görsel puanı | **3,85 / 5** (toplam 104/135) |
| Ortalama kullanıcı dostu puanı | **3,15 / 5** (toplam 85/135) |
| Düzenlenemeyen veri girişi kalemi | **30** |
| Kendiliğinden oluşan hata | `/urunler` Stok modalı NRE (2× yeniden üretildi) · `/uyum` arama deserializasyon istisnası · `/denetim-izi` çift istek · `/kilavuz` CSP `frame-ancestors` ihlali (2×); diğer 4xx'ler kasıtlı geçersiz denemelerden |

En düşük puanlı sayfalar: `/kilavuz` (1/1), `/uyum` (4/2), `/cariler` (3/3), `/urunler` (3/3), `/faturalar` (3/3). En yüksek: `/gider-pusulalari/{id}/belge` (5/4), `/raporlar/kar-zarar` (5/4).

### KRİTİK bulgular (11) — rota · kanıt · öneri
1. **`/islemler` — satış fişi listelenmiyor, düzenlenemiyor, storno yok.** Kanıt: kayıt sonrası fiş no/ID gösterilmedi, `main`'de hareket tablosu ve Düzenle/Storno yok; tek geri alma yolu "Satış İadesi" sekmesi (satış `01M2N56N8RH59JPAN02S2G8VMY` ancak tam ters kayıtla netlendi) — yanlış açıklama/ödeme türü düzeltilemiyor. → Form altına "Bugünkü işlemler" tablosu + Görüntüle/Düzenle/Storno; bantta fiş numarası.
2. **`/cariler` — pasife alınan cari listede "Aktif" gibi kalıyor, aktife alma yok.** Kanıt: DELETE `cari/01M2N5A7FE9X60H0FQBQRJVYV3` 200, GET → `aktif:false`; liste satırı 5 eylem düğmesiyle rozetsiz duruyor, "Aktife Al" yok, filtre yalnız Tümü/Müşteri/Tedarikçi. → Aktif süzgeci + "Pasifleri göster" + rozet + "Aktife Al".
3. **`/urunler/agac` — reyon düzenlenemiyor/pasife alınamıyor.** Kanıt: "QA-TEST Vitrin T2 | Kasa | Merkez Şube | 0 | Aktif" satırında button/a/input sayısı 0. → PATCH `/reyonlar/{id}` + kategori benzeri Düzenle modalı.
4. **`/kasalar` — kasa hareketi düzenlenemiyor/storno edilemiyor, geçmişte açıklama yok.** Kanıt: POST hareket 200 ("QA-TEST kasa hareketi G2", 150); Geçmiş modalı satırı "NAKIT 16.09.2026 ₺150,00", düğmeler `['×']`. → Geçmiş satırına Düzenle + Storno (gerekçe zorunlu) + açıklama sütunu.
5. **`/banka` — dekont düzenlenemiyor/silinemiyor.** Kanıt: 1.250 TL "QA-TEST dekont G2" POST 200; satırdaki tek eylem "🧾 Faturaya Dönüştür"; "Bekliyor" durumu elle değiştirilemiyor. → Düzenle + Sil/Storno + eşleştirme.
6. **`/emanet` — emanet giriş/çıkış hareketleri düzenlenemiyor, storno yok.** Kanıt: +11,1752 / −4,5800 has satırlarında `btns:[]`. Düzeltme yalnız ters iade ile mümkün, o da ekstrede "Çıkış" görünüyor. → Satıra Düzenle (aynı gün) + Storno; ekstrede düzeltme işareti.
7. **`/faturalar` — TASLAK fatura düzenlenemiyor.** Kanıt: FTR-2026-000002 (TASLAK) satır eylemleri "Kalemleri Göster | Kes | İptal Et"; Kalemler modalı salt okunur (`btns:['×']`). İptal edip yeniden girmek numara serisini tüketiyor. → Taslakta "Düzenle" (aynı modal, PUT/PATCH), Kesildi sonrası kilit.
8. **`/gider-pusulalari` — kayıt hiç düzenlenemiyor, "+ Yeni" de yok.** Kanıt: `main` düğmeleri yalnız "Detay" ve "🖨 Yazdır"; detay dialogu salt metin. VUK belgesi; yanlış TCKN/adres düzeltilemiyor. → Detayda "Düzenle" (denetim izli) + "Yeni".
9. **`/uyum` — kimlik tespiti ve şüpheli bildirim kayıtları düzenlenemiyor/silinemiyor.** Kanıt: kimlik tespiti dialogunda tek düğme "×"; QA-TEST bildirimi (250.000 ₺, ACIK) iptal/sil edilemedi; durum düğmeleri de istek atmıyor. KVKK md.4/1-d + MASAK 10 iş günü. → ACIK durumda tutar/gerekçe düzenleme + iptal; kimlik tespitinde düzeltme.

10. **`/isletme` — işletme adı düzenlenemiyor (salt metin).** Kanıt: işletme adı düz metin; satırda textbox/Düzenle yok. API alanları mevcut: `ad, plan, kurKaynagi, maliyetYontemi, varsayilanArayuzModu, aiEtkin`; PATCH `/admin/tenant` zaten çalışıyor (plan değişimi bu yolla kalıcı oldu). → İşletme kimliği için Düzenle diyaloğu (ad, ileride VKN/adres).
11. **`/kilavuz` — sayfa tamamen boş.** Kanıt: konsolda 2× "Framing … violates `frame-ancestors 'none'`"; GET `/kilavuz.html` `ERR_BLOCKED_BY_RESPONSE`; HEAD başlıkları `frame-ancestors 'none'` + `X-Frame-Options: DENY`. Kullanıcı hiçbir yardım içeriğine ulaşamıyor; tur 1'den bu yana değişiklik yok. → Kılavuzu Blazor bileşeni olarak doğrudan render et ya da yalnız `/kilavuz.html` yolu için `frame-ancestors 'self'` / `SAMEORIGIN` istisnası (global ayarı bozmadan) + "Yeni sekmede aç" yedek bağlantısı.

> **Kapanan KRİTİK (1):** `/fiyatlandirma` — fiyat listesi ve kampanya satırlarına Düzenle / Pasife al / Aktifleştir geldi, PATCH 200 ile doğrulandı.

### Düzenlenemeyen veri girişleri — TUR 2 (30 kalem, 27 rotanın 16'sında)
| # | Rota | Düzenlenemeyen kayıt / alan |
|---|---|---|
| 1 | `/islemler` | Satış fişi — kayıt sonrası görüntüle/düzenle yok; yalnız ters kayıt |
| 2 | `/islemler` | Satış kalemi (adet / birim fiyat) — satır içi düzenleme yok, yalnız Sil |
| 3 | `/cariler` | Aktif/pasif — pasife alma geri alınamıyor, listede görünmüyor |
| 4 | `/urunler` | Gerçek Milyem — düzenleme formunda yok |
| 5 | `/urunler` | Birim (Adet/Gram) — düzenleme formunda yok |
| 6 | `/urunler` | Aktif/pasif — pasiften geri alınamıyor |
| 7 | `/urunler` | Şube stoğu — Stok modalı salt okunur, stok girişi yok (**yeni**) |
| 8 | `/urunler/agac` | Reyon (ad, şube, tip, aktif) — Düzenle/Pasife Al yok |
| 9 | `/kasalar` | Kasa hareketi (tutar/tür/açıklama) — Düzenle/Storno yok |
| 10 | `/kasalar` | Kasa dövizi — düzenleme modalında yalnız "Kasa Adı" |
| 11 | `/kasalar` | Pasif kasa — "Aktife Al" yok |
| 12 | `/banka` | Banka hareketi/dekont (yön/tutar/karşı taraf/açıklama/durum) |
| 13 | `/banka` | Banka hesabı — pasife alma/silme yok |
| 14 | `/kur` | Manuel kur kaydı — düzeltme/iptal yok |
| 15 | `/emanet` | Emanet giriş hareketi (ayar/gram/taş/açıklama) |
| 16 | `/emanet` | Emanet iade hareketi |
| 17 | `/sayim` | Sayım oturumu adı/kapsamı/şubesi |
| 18 | `/sayim` | Sayılmış satırı "sayılmadı"ya geri alma |
| 19 | `/faturalar` | Taslak fatura (cari/tür/KDV/toplam/açıklama/kalemler) |
| 20 | `/faturalar` | Fatura kalemi (miktar/fiyat/işçilik/indirim) |
| 21 | `/faturalar` | "Ödendi" durumu — geri alınamıyor (İptal Et de kalkıyor) |
| 22 | `/gider-pusulalari` | Gider pusulası (satıcı ad/TCKN/adres/mal/gram/ayar/brüt/stopaj/teslim tarihi) — yeni de yok |
| 23 | `/uyum` | Kimlik tespiti (ad, belge türü/no, uyruk, adres, tutar, beyan) — salt okunur dialog, tek düğme × |
| 24 | `/uyum` | Şüpheli işlem bildirimi (tutar/işlem kimliği/gerekçe) — düzenle/sil/iptal yok, durum düğmeleri de çalışmıyor |
| 25 | `/kullanicilar` | Kullanıcı rolü — Düzenle diyaloğunda rol alanı yok (yalnız Yeni Kullanıcı'da var) |
| 26 | `/kullanicilar` | Kullanıcı aktif/pasif — DURUM sütunu var, değiştirme eylemi yok |
| 27 | `/isletme` | İşletme adı — salt metin, Düzenle yok (**KRİTİK**) |
| 28 | `/isletme` | Şube aktif/pasif — pasif → aktif yolu yok |
| 29 | `/isletme` | Kur kaynağı ve maliyet yöntemi — API'de var, ekranda kontrol yok (**yeni**) |
| 30 | `/ayarlar` | Profil Ad Soyad ve E-posta — Ayarlar'da salt okunur; yalnız `/kullanicilar`'da düzenlenebilir |

### Görsel / kullanıcı dostu puan dökümü (tur 2)
| Rota | Görsel | Kullanıcı dostu |
|---|---|---|
| `/giris` | 4 | 3 |
| `/` | 4 | 4 |
| `/islemler` | 4 | 3 |
| `/cariler` | 3 | 3 |
| `/urunler` | 3 | 3 |
| `/urunler/agac` | 4 | 3 |
| `/urunler/etiketler` | 4 | 4 |
| `/fiyatlandirma` | 4 | 4 |
| `/kasalar` | 4 | 3 |
| `/banka` | 4 | 3 |
| `/kur` | 4 | 3 |
| `/emanet` | 4 | 3 |
| `/sayim` | 4 | 3 |
| `/faturalar` | 3 | 3 |
| `/gider-pusulalari` | 4 | 3 |
| `/gider-pusulalari/{id}/belge` | 5 | 4 |
| `/uyum` | 4 | 2 |
| `/raporlar` | 4 | 3 |
| `/raporlar/kar-zarar` | 5 | 4 |
| `/raporlar/vade-takibi` | 4 | 3 |
| `/denetim-izi` | 4 | 3 |
| `/veri-disa-aktarim` | 4 | 3 |
| `/kullanicilar` | 4 | 3 |
| `/yetkiler` | 4 | 4 |
| `/isletme` | 4 | 3 |
| `/ayarlar` | 4 | 4 |
| `/kilavuz` | 1 | 1 |
| **Toplam (27 sayfa)** | **104 / 135** | **85 / 135** |
| **Ortalama (27 sayfa)** | **3,85** | **3,15** |

### Düzeltme paketine giren maddeler (tur 2 önceliği)
| Sıra | Paket | Kapsam | Gerekçe (tur 2 kanıtı) |
|---|---|---|---|
| 1 | **UX-4e — Kabuk duyarlılığı** (tur 1'den devir, hiç başlanmamış) | Kenar çubuğu <768px çekmece, üst bar (`ust-sag`, `mod-anahtar`) sarmalama, tabloları mobilde kart | 26/27 rotada taşma bulgusu, scrollWidth 499–1141; tek düzeltme 26 bulguyu birden kapatır |
| 2 | **UX-4q — `/urunler` Stok modalı NRE (YENİ)** | `Urunler.razor` `b__0_9` render fragment'inde null koruma + regresyon testi | Her açılışta `NullReferenceException` + kullanıcıya "Beklenmeyen bir hata oluştu" bandı; 2 kez yeniden üretildi |
| 3 | **UX-4r — `/uyum` arama sözleşme uyuşmazlığı (YENİ)** | İstemci servisinde `/kimlik-tespitleri/ara` yanıtını `{adet, kayitlar}` zarfıyla çöz; teknik istisnayı kullanıcıya gösterme | Arama %100 çöküyor, denetim izine "1 kayıt" yazılıyor ama kullanıcı sonuç görmüyor |
| 4 | **UX-4h — Düzenlenebilirlik dalgası 2 (mali kayıtlar)** | Satış fişi listesi + storno, kasa hareketi storno, banka dekontu düzenle/sil, emanet storno, taslak fatura düzenle | 5 KRİTİK; mali defter düzeltilemiyor, denetim izli storno şart |
| 5 | **UX-4g kalanı — reyon + cari + işletme kimliği** | PATCH `/reyonlar/{id}` + Düzenle modalı; cari pasif rozeti/süzgeci/"Aktife Al"; `/isletme` işletme adı Düzenle diyaloğu (PATCH `/admin/tenant` hazır) + kur kaynağı/maliyet yöntemi kontrolleri | 3 KRİTİK; fiyatlandırma ayağı kapandı, kalan üç ayak duruyor |
| 6 | **UX-4i — Uyum/VUK düzenlenebilirlik** | Gider pusulası düzenle + "Yeni"; kimlik tespiti düzelt; şüpheli bildirim düzenle/iptal **ve "Gönderildi"/"Kapat" işlev hatası** | 2 KRİTİK + 1 YÜKSEK; MASAK 10 iş günü, KVKK md.4/1-d |
| 7 | **UX-4j — Doğrulama & geri bildirim altyapısı** | Ortak EditForm/ValidationMessage, Türkçe `ErrorMessage`, Toast (kendiliğinden kapanan), yıkıcı eylemde onay modalı, **Escape ile modal kapatma + odak tuzağı** | Sessiz etkisizlik 13 ekranda; Escape 7/7 denemede başarısız; İngilizce mesajlar 3 rotada |
| 8 | **UX-4k — Pasif/aktif yaşam döngüsü** | Cari, ürün, kasa, şube, banka hesabı, kullanıcı için "Aktife Al" + rozet + süzgeç; `/kullanicilar` Düzenle'ye rol + Aktif anahtarı; pasif kayıtta yıkıcı/ekleme düğmelerini gizle | 1 KRİTİK + 4 YÜKSEK; pasif kasada "+ Hareket" hâlâ aktif, pasif şube (`QA-TEST Şube Kadıköy`) arayüzden geri alınamadı |
| 9 | **UX-4l — Rapor/veri doğruluğu (HATA)** | Kur geçmişi `bitis` gün sonu (**kök neden doğrulandı**), gün sonu mutabakatı, NET HAS GRAM etiketi, aylık veresiye alanı, kâr-zarar HAS hasılatı, fatura kalem birimi, döviz kasasında ₺ simgesi | 1 doğrulanmış API hatası + 5 sayısal tutarsızlık; regresyon testi ile |
| 10 | **UX-4s — `/denetim-izi` çift istek (şiddet arttı)** | Filtre değişimini tek tetikleyiciye indir, yeni sorguda listeyi sıfırla, yanıtı istek kimliğiyle eşle | 200 satır / 98 tekil + yanlış "200 / 454" sayacı → denetim izi çift kayıtlı görünüyor |
| 11 | **UX-4m — Oturum/401 hijyeni** | Tek noktadan 401 yakalama (tek yenile → tek çıkış → `/giris`), "Oturumunuz sona erdi" mesajı, error boundary tetiklenmemesi | Grup 3 turun başında deseni yeniden gözlemledi; kapanmadı |
| 12 | **UX-4n — Alan doğrulama & giriş kalitesi** | Telefon biçimi, "Sıra" negatif reddi, etiket adedi min=1, ürün stok bilgisi + açılış stoğu, sayım ürün kodu/ad araması, negatif bakiye uyarısı, **`/faturalar` fiş seçici (ULID yerine fiş no)** | ORTA yoğunluklu ama veri kalitesini ve tezgâh akışını doğrudan etkiliyor |
| 13 | **UX-4o — Performans/gürültü** | `kur/guncel` tek merkezden 30–60 sn, geçersiz aralıkta istemci engelleme, `/banka` açılış titremesi | Son 40 isteğin ~20'si `kur/guncel` |
| 14 | **UX-4p — Cilalama** | Ham enum etiketleri (cari türü, fiyat listesi türü, denetim türleri, satıcı tipi), tr-TR sayı biçimleyici, `/giris` illüstrasyon çakışması, sayfa başlıkları, erişilebilir etiketler ("on", "QA TestSAHIP", rozet bitişikliği) | Görsel 3,85 → hedef 4,5 |
| 15 | **UX-4f — `/kilavuz` KRİTİK** | iframe → Blazor render ya da yalnız `/kilavuz.html` için `frame-ancestors 'self'`/`SAMEORIGIN` istisnası + "Yeni sekmede aç" yedek bağlantısı + iframe temasını miras aldır | Tur 2'de yeniden doğrulandı: CSP ihlali 2×, `ERR_BLOCKED_BY_RESPONSE`, sayfa bomboş; turun en düşük puanlı sayfası (1/1) |
| 16 | **UX-4t — Yönetim ekranları (grup 4)** | `/ayarlar`'a "Profili düzenle"; "Şifreyi Güncelle" düğmesini alanlar doluyken etkinleştir; `/yetkiler` matrisinde rol sütununu yapışkan yap + kirli durum takibi + başarı bandını ≥3 sn göster; `/kullanicilar` Yeni Kullanıcı formunda alan bazlı doğrulama; `/isletme` şube telefonu biçim doğrulaması; `/veri-disa-aktarim` gerekçe için canlı karakter sayacı | Grup 4'ün 29 bulgusundan, KRİTİK/kabuk/pasif-aktif paketlerine girmeyen ORTA-DÜŞÜK kalanı; hepsi ölçülmüş kanıtlı kalemler |

### Tur 2 notları
- Kod değiştirilmedi, git komutu çalıştırılmadı, derleme/test koşulmadı, 5198 portuna dokunulmadı. Tüm gözlemler `http://localhost:5199`, kiracı `01M2N4TMYRFXBFZN1N5DTRM8HP`.
- **Politika gereği yapılmayanlar:** `/giris` şifre girişi; `/veri-disa-aktarim` "Hazırla ve İndir" gerçek dosya indirme; `/kullanicilar` Yeni Kullanıcı kaydı (şifre alanı zorunlu); `/ayarlar` şifre değiştirme. Token/parola değerleri hiçbir rapora yazılmadı.
- **Oturum:** tur 1'in dersine rağmen gruplar tek fikstür token çiftini paylaştı; grup 2 ve grup 3 süresi dolmuş token/geçersiz refresh nedeniyle e-posta+şifre ile yeniden giriş yapmak zorunda kaldı. **Tur 3 için grup başına ayrı kullanıcı/token çifti üretilsin.**
- **Madde 1 düzeltmesi:** bu özetin ilk sürümü 90 KB'de kesilmiş grup çıktılarıyla yazılmış, grup 4'ün 6 rotası (`/veri-disa-aktarim` hariç 5'i) "ÖLÇÜLMEDİ" sanılmış ve toplamlar eksik yazılmıştı. Bu bir **araç/girdi hatasıydı, ürün bulgusu değildi**; tüm sayılar `ham/qa-turu-2.json` kaydından yeniden hesaplandı. Ölçüm eksiği yoktur: 27/27 rota gezildi.
- **Temizlik:** fikstür kiracısı **korundu** (silinmedi) — sonraki turlar ve kurucuya gerçekçi örnek için kalıcı. `SELECT COUNT(*) FROM tenants` → **7**.
- **Kiracıda bırakılan QA-TEST verisi (tur 2):** satış `01M2N56N8RH59JPAN02S2G8VMY` + iadesi (netlenmiş) · pasif cari `01M2N5A7FE9X60H0FQBQRJVYV3` · pasif ürün `01M2N5D6TTQ6KZG3EHCJ29YQ84` · kategori `01M2N5JK90E7WQ1FP4KNZJZ9WT` (sıra 5) · reyon "QA-TEST Vitrin T2" (silinemez) · fiyat listesi `01M2N62DA6MKXSSXDH87KFD86D` · kampanya `01M2N63N8FCNF2BQ1PGN1BY74S` · pasif kasa `01M2N65V036DYTQEQGJY010KXQ` (EUR, ±150) · banka hesabı + dekont `01M2N69XR2BC7NYCGMXNXV19F4` · emanet net 6,5952 has · MANUAL kur 6.800 kaydı (güncel kaynak ALTINKAYNAK'a döndürüldü) · "QA Eylül Sayımı"nda 925 Gümüş Kolye sayılan=6 · fatura FTR-2026-000002 (ÖDENDİ, geri alınamaz) · MASAK şüpheli bildirim (ACIK, 250.000 ₺, silinemez) · **pasif şube "QA-TEST Şube Kadıköy (düzenlendi)"** (arayüzde aktife alma yok). Grup 4'ün geri alınan değişiklikleri (API ile doğrulandı): kullanıcı soyadı "Test", Müdür→Kullanıcılar yetkisi `true`, plan `BASLANGIC`, arayüz modu `PROFESYONEL`, tema Sistem. Ayrıca **Ana Kasa −144.996 TL** negatif bakiyede (fikstür alışlarından; bulgu olarak kaydedildi).
