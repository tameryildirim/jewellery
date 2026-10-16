# Tarayıcı QA turu — TUR 5 (2026-09-18, uygulama içi tarayıcı, http://localhost:5199)

> Kurucu talimatı: "ekran testlerini tıklayarak benim tarayıcımdan yap, hiçbir ayrıntıyı atlama". Yöntem her sayfada: (1) sayfaya git, (2) konsol/ağ hatalarını oku, (3) erişilebilirlik ağacını oku (başlık, boş durum, tablo, düğmeler), (4) ekran görüntüsü, (5) her modal/formu aç, doğrulama mesajlarını tetikle, `QA-TEST-` önekli kayıt oluştur → düzenle → (varsa) pasife al → aktife al, (6) karanlık tema + dar ekran (375px), (7) tur 4 bulgularının tek tek yeniden denenmesi.
>
> **Tur 1 (`QA-TARAYICI-TURU-2026-09-16.md`), tur 2, tur 3 ve tur 4 dosyaları tarihsel tabandır; bu turda DEĞİŞTİRİLMEMİŞTİR.** Bu dosya tur 1'in başlık + 27 satırlık tablo iskeletinden türetilmiş, bu turda ilk kez ya da yeniden taranan 12 rota eklenerek **39 satıra** genişletilmiştir.
>
> **Tur 5 tamamlandı (tam koşu).** Kiracı: `QA-TEST-3 Kuyumculuk` (`01M2NZ97WE0PC3EGQG0NZX3W74`) — fikstür kiracısı korunarak sürdürüldü. 5 paralel grup, **39 sayfa**.
>
> **ANAYASA MD.1 — YAPILMAYANLAR GİZLENMİYOR:**
> - Fikstür dosyası (`scratchpad/qa-fikstur.json`) tur 4 kapanışında boşaltıldığı için **0 bayt** geldi; beş grubun beşi de bunu raporladı. Turlar ya tarayıcı bölmesinde zaten duran canlı oturumla ya da `POST /api/v1/auth/yenile` ile sürdürüldü. Jeton/parola değeri hiçbir çıktıya yazılmadı. **Sonraki tur için risk:** tarayıcıda canlı oturum yoksa mevcut izin ayarlarıyla hiçbir rota test edilemez.
> - `/kurulum` → "Açılışı uygula" ve "Kurulumu mühürle" geri alınamaz olduğu için **bilerek çalıştırılmadı**; mühür sonrası düzenlenebilirlik doğrulanmadı.
> - `/kullanicilar` yeni kullanıcı kaydı, `/ayarlar` şifre değiştirme ve iki aşamalı giriş kurulumu **parola gerektirdiği için başlatılmadı**.
> - `/banka` "Ekstre Yükle" CSV akışı (dosya yükleme aracı yok), `/pos/tahsilatlar` eşleştirme/storno akışı (kiracıda hiç POS tahsilatı yok), `/bildirimler` bildirim kapatma/geri alma (açık bildirim üretilemedi) **doğrulanamadı**.
> - `/gider-pusulalari/{Id}/belge` bu turda **ölçülmedi** (tabloda öyle işaretli).
> - **ÖLÇÜM TUZAĞI (grup 4 kaydı):** `resize_window` 1280×800 öykünmesi açıkken bölme daha darsa sayfa ölçekleniyor ve `ref` ile yapılan tıklamalar yanlış öğeye düşüyor. Tur 4'ün tıklama temelli "çalışmıyor" bulguları bu tuzağa düşmüş olabilir; tur 5'te tüm tıklama testleri öykünme KAPALI yapıldı.

| # | Rota | Sayfa | Durum | Konsol/ağ | Bulgular |
|---|---|---|---|---|---|
| 1 | `/giris` | Giriş | BULGU | Bu sayfaya ait yeni konsol hatası yok. `POST auth/giris` → **404** (var olmayan kullanıcı denemesi); diğer tüm istekler 200/304. | 4 bulgu (0 KRİTİK). Başarısız girişte "Kullanıcı bulunamadı" + 404 ile kullanıcı varlığı ifşası (ORTA, HATA); şifre sıfırlama akışı hiç yok (ORTA); şifrede asgari uzunluk ve göz düğmesi yok (DÜŞÜK); sol tanıtım metni yüzük illüstrasyonunun üstüne biniyor (DÜŞÜK). |
| 2 | `/` | Panel | BULGU | Tüm rapor/kur/yetki/bildirim GET'leri 200; `hubs/kur/negotiate` 200. **TEK HATA:** `POST kur/guncelle` → 503. | 3 bulgu. "Kuru güncelle" daima 503 veriyor ama düğme herkese etkin (ORTA); kur hata bandının kapatma düğmesi yok, kalıcı (DÜŞÜK); negatif kasa bakiyeleri uyarısız listeleniyor (DÜŞÜK). |
| 3 | `/islemler` | İşlemler (POS) | BULGU | `POST islemler/satis` 400 (NAKİT+tevsik, 2×) → **200 (HAVALE ile gerçek satış kaydedildi)**. Başka hata yok. | 6 bulgu (0 KRİTİK). Ürün seçili değilken "Ekle" sessizce etkisiz (ORTA); liste fiyatı biliniyor ama alan ön doldurulmuyor (ORTA); ürün listesinde stok adedi yok (ORTA); dar ekran 571>375 (ORTA, YENİ); tevsik uyarısında Kaydet etkin kalıyor (DÜŞÜK); başarı mesajında ham ULID (DÜŞÜK). |
| 4 | `/vezne` | Vezne | BULGU | Kuyruk/fiş/gönder/iptal istekleri 200. 4xx/5xx yok. | 7 bulgu (0 KRİTİK). **Kuyrukta "Hazırlanıyor" fişler ₺0,00 gösteriyor, detayda ₺2.000 (YÜKSEK, HATA — API ile kanıtlandı, hata yalnız ACIK dalında)**; fiş açılınca süzgeç değişmediği için kayıt kaybolmuş görünüyor (ORTA); boş gerekçeli iptal sessizce etkisiz (ORTA); düzenleme penceresinde "İşçilik (TL)" alanı yok (ORTA); eski doğrulama mesajı kalıyor (DÜŞÜK); ham ULID (DÜŞÜK); gövde başlığı/açıklaması yok (DÜŞÜK). |
| 5 | `/cariler` | Cariler | BULGU | Liste/oluşturma/güncelleme 200 ("Kaydedildi." 2×). 4xx/5xx yok. | 6 bulgu (0 KRİTİK). **Pasif cariler satış ekranının müşteri listesinde seçilebiliyor (YÜKSEK, HATA, YENİ)**; **aktif cari pasife alınamıyor — "Pasife Al" yok (YÜKSEK, DÜZENLENEBİLİRLİK)**; pasif cariye tahsilat/veresiye açık (ORTA); satır içi düzenlemede Enter kaydetmiyor, değer kayboluyor (ORTA); dar ekran 996>375 (ORTA); satır eylemlerinin erişilebilir adları ayırt edilemiyor (DÜŞÜK). |
| 6 | `/urunler` | Ürünler | BULGU | Liste/stok/oluşturma/pasife alma/aktife alma 200. 4xx/5xx yok. | 5 bulgu (0 KRİTİK). Stok penceresinde şube adı yerine ham ULID (ORTA); listede stok adedi sütunu yok (ORTA); zorunlu alanlar formda işaretli değil (ORTA); dar ekran 859>375 (ORTA); stok penceresinde düğme ve hata metni bitişik (DÜŞÜK). |
| 7 | `/urunler/agac` | Ürün ağacı | BULGU | Kategori/reyon listeleri ve kategori oluşturma 200. 4xx/5xx yok. | 3 bulgu (0 KRİTİK). Kategori pasife alınamıyor/silinemiyor — aynı sayfadaki reyonlarda var (ORTA, DÜZENLENEBİLİRLİK); dar ekran 751>375 (ORTA); üç "+ Yeni Kategori" düğmesi ayırt edilemiyor (DÜŞÜK, erişilebilirlik). |
| 8 | `/urunler/etiketler` | Etiketler | BULGU | Ürün/etiket listesi 200. 4xx/5xx yok. Yazıcıya çıktı GÖNDERİLMEDİ. | 3 bulgu (0 KRİTİK). Sonuçsuz aramada boş durum mesajı yok (ORTA); arama boşalsa da seçim ve etiket önizlemeleri kalıyor — yanlış etiket basma riski (ORTA); etiket işi/adet kalıcı değil (DÜŞÜK). |
| 9 | `/fiyatlandirma` | Fiyat listeleri | BULGU | Liste/kampanya GET, POST, PATCH 200. Bu rotada yeni hata yok. | 6 bulgu (0 KRİTİK). **Karanlık temada `satir-vurgu` açık zeminde kalıyor, metin okunmuyor (YÜKSEK, GÖRSEL)**; alan düzeltilince hata mesajı kalıyor (ORTA); pasife alma onaysız (ORTA); yalnız ad değişirken risk onayı kaydı durduruyor (DÜŞÜK); sayılar nokta ondalıklı (DÜŞÜK); `<form>` yok, Enter kaydetmiyor (DÜŞÜK). |
| 10 | `/kasalar` | Kasalar | BULGU | Kasa oluşturma/düzenleme/hareket/storno 200. 4xx/5xx yok. | 3 bulgu (0 KRİTİK). Kasa kurma formunda şube seçilemiyor ama düzenlemede değişebiliyor (ORTA); devre dışı "Pasife Al" kart genişliğinde dolu kırmızı düğme (DÜŞÜK); "GIRIS" ↔ "Gelir" sözlük tutarsızlığı (DÜŞÜK). |
| 11 | `/banka` | Banka | BULGU | Hesap/hareket GET-POST 200; `PATCH bankalar/{id}` **400** (kasıtlı geçersiz IBAN) → sonra 200. | 6 bulgu (0 KRİTİK). Dekont "Bekliyor"dan çıkarılamıyor, mutabakat yapılamıyor (ORTA); devre dışı ipucu "kasa" diyor, sebep yalnız tooltip'te (ORTA); **dar ekran 789>375 — tur 4'e göre kötüleşme (751→789)** (ORTA); ekstre çekmecesi + dekont kipi üst üste, çift karartma (ORTA); bant içeriksiz "Hareket kaydedildi!" (DÜŞÜK); geçersiz IBAN istemcide yakalanmıyor (DÜŞÜK). |
| 12 | `/kur` | Kur | HATA | `kur/guncel` 200 (oturumda 130+ istek, biri **401**); `kur/gecmis`, `kur/pariteler` 200; **`POST kur/guncelle` 503**; `POST kur/manuel` 201. | 6 bulgu (0 KRİTİK, 3 YÜKSEK). **Kur geçmişinde "İptal et" hiçbir şey yapmıyor — yanlış manuel kur geri alınamıyor (YÜKSEK, HATA)**; **başarısız "Şimdi çek" sonrası güncel kur kartı ve ayar tablosu siliniyor (YÜKSEK, HATA)**; **kaynaklar kapalıyken "Şimdi çek" etkin, her tıklama garanti 503 (YÜKSEK)**; geçmiş otomatik yoklamayla doluyor, sayfalama yok (ORTA); `kur/guncel` aşırı tekrarı (ORTA, PERFORMANS); "MANUAL"/"ALTINKAYNAK" ham sabit (DÜŞÜK). |
| 13 | `/emanet` | Emanet | BULGU | Emanet giriş + gerekçeli storno 200. Konsol hatası yok. | 4 bulgu (0 KRİTİK). Storno ters kaydı TÜR sütununda "Giriş" görünüyor (ORTA, HATA); hareketten ham gram/ayar/taş geri okunamıyor (ORTA); devre dışı düğmelerin sebebi yazmıyor (DÜŞÜK); müşteri seçilmeden sayfa boş, yönlendirme yok (DÜŞÜK). |
| 14 | `/sayim` | Sayım | HATA | `sayim` GET/POST 200, `sayim/{id}/say` 200 (2×). **Enter denemelerinde HİÇ istek üretilmedi.** | 7 bulgu (0 KRİTİK, **4 YÜKSEK**). Satır içi "Sayılan"da Enter sessizce kaydetmiyor — ipucu metni kaydedeceğini vaat ediyor (YÜKSEK); düzenleme açılınca alan odaklanmıyor, yazılan değer kayboluyor (YÜKSEK); DURUM sütununda gerçek durum yerine düz metin "İptal Et" (YÜKSEK, HATA); zorunlu "Şube" boşken hiçbir alan işaretlenmiyor (YÜKSEK); satır dışarıdan güncellenince düzenleyici boş kalıyor (ORTA, HATA); "→" düğmesinin erişilebilir adı yok (DÜŞÜK); sayılan adet alanı `type=text` (DÜŞÜK). |
| 15 | `/faturalar` | Faturalar | HATA | `faturalar` GET/POST 200, durum süzgeci 200. **İlk (etkisiz) Kaydet basışında hiç istek üretilmedi.** | 5 bulgu (**1 KRİTİK**). **TASLAK fatura ve kalemleri düzenlenemiyor — düzeltme numara serisini yakıyor (KRİTİK, DÜZENLENEBİLİRLİK)**; dolu formda ilk "Kaydet" etkisiz, ikincide kaydediyor (YÜKSEK, HATA); "Fişten Fatura Kes" 26 haneli ULID'yi elle yazdırıyor (YÜKSEK); tablo 1280'de kırılıyor, 375'te 1178 taşma, rozetler bitişik (ORTA); kalemler kipi taslakta "kesilmiştir" diyor (DÜŞÜK). |
| 16 | `/siparisler` | Siparişler | BULGU | `POST siparisler` **400** (kasıtlı geçersiz ayar "22") → 200; `PATCH siparisler/{id}` 200. | 7 bulgu (0 KRİTİK). Durum geçişi onaysız, geri alınamıyor ve "Düzenle" düğmesini de yok ediyor (YÜKSEK, DÜZENLENEBİLİRLİK); "Ayar" serbest metin, hata ancak sunucudan 400 ile dönüyor (ORTA); düzenlemede cari/tür/şube değiştirilemiyor (ORTA); kalem girdilerinin erişilebilir adı yok (DÜŞÜK); doğrulama mesajında `role=alert` ve odak yok (DÜŞÜK); adet `step=0.001`, gram noktalı (DÜŞÜK); `main` içinde h1 yok (DÜŞÜK). |
| 17 | `/gider-pusulalari` | Gider pusulaları | HATA | Liste/süre-takibi/PATCH 200. Ters tarih aralığı **400 yerine 200** dönüyor. Detay açılışında aynı GET **iki kez**. | 4 bulgu (0 KRİTİK). **Boş adresle gider pusulası kaydedilebiliyor — VUK md.234 zorunlu unsuru doğrulanmıyor (YÜKSEK, HATA, YENİ)**; "Hurda alımı gir" derin bağlantısı Satış sekmesine düşüyor (ORTA); ters tarih aralığı istemcide engellenmiyor, uyarı da yok (DÜŞÜK); detayda çift getirme → denetim izine çift satır (DÜŞÜK, PERFORMANS). |
| 18 | `/gider-pusulalari/{Id}/belge` | Gider pusulası belgesi | ÖLÇÜLMEDİ | — | **Bu turda taranmadı** (5 grubun kapsamına girmedi). Tur 4 bulguları geçerliliğini koruyor sayılmalı; tur 6'da ölçülmeli. |
| 19 | `/uyum` | Uyum (tevsik/MASAK) | BULGU | MASAK özet/risk/kimlik/şüpheli GET 200; `POST masak/supheli` **404** (kasıtlı geçersiz işlem kimliği) → 201; `PATCH .../kapat` **200 (tur 4'ün kırık akışı KAPANDI)**. | 6 bulgu (0 KRİTİK). GONDERILDI bildiriminde hiçbir eylem kalmıyor, ekrandaki kuralla çelişiyor (ORTA, DÜZENLENEBİLİRLİK); "İşlem kimliği" serbest metin, 404'te alan düzeyinde hata yok (ORTA); kimlik tespiti aramasında Enter sessiz (DÜŞÜK); "Bu kayıt değiştirilemez" bloğu her satırda tekrar ediyor (DÜŞÜK); saklama süresi kartları sayfanın diliyle tutarsız (DÜŞÜK); yeni bildirimde başarı bandı görünmüyor (DÜŞÜK). |
| 20 | `/raporlar` | Raporlar | HATA | Aylık/katalog/yaşlandırma/stok/mutabakat GET 200; ay değişiminde 200. 4xx/5xx yok. | 4 bulgu (0 KRİTİK). **"Stok Dağılımı" pastası tek kategori %100 iken hiç çizilmiyor (YÜKSEK, HATA — `getBBox()` genişlik 0)**; ay değişince stok kartları dönemden bağımsız olduğunu söylemiyor (ORTA); sayı biçimi karışık (DÜŞÜK); katalogda "Satış Analizi" kartı yok, "Kâr-Zarar" iki kez (DÜŞÜK). |
| 21 | `/raporlar/kar-zarar` | Kâr/zarar | BULGU | `kar-zarar`, `kar-zarar/urun`, karşılaştırma 200. Ters tarihte hiç istek gitmiyor (doğru davranış). | 4 bulgu (0 KRİTİK). "Defter has gramı" kendi açıklamasıyla çelişiyor: 11,4500 g ↔ aylık 50,3800 has (ORTA, HATA); sekme başlığı "— Kuyumcu ERP" ekini kullanmıyor (DÜŞÜK); "maliyet eksik" rozeti ürün adına bitişik (DÜŞÜK); filtre değişiminde yinelenen istekler (DÜŞÜK, PERFORMANS). |
| 22 | `/raporlar/vade-takibi` | Vade takibi | BULGU | `vade-takibi?ufukGun=7/60` 200; kasıtlı `ufukGun=-1` → **400** (istek yine de gidiyor). | 3 bulgu (0 KRİTİK). Kiracıda açık alacak olmadığı için ekran dolu veriyle **doğrulanamadı** (ORTA, HATA — `cari-yaslandirma` toplamSatir=0 ile teyit); negatif ufuk değeri istemcide engellenmiyor (DÜŞÜK); hızlı ufuk düğmeleri `aria-pressed` bildirmiyor (DÜŞÜK). |
| 23 | `/raporlar/kasa` | Kasa defteri | BULGU | `kasa-defteri` 200; kasıtlı ters aralık **400**. | 5 bulgu (0 KRİTİK). Sayfa açıklaması yanlış — Rapor Merkezi metni (ORTA, HATA); yasal nitelikli defterde yazdırma/dışa aktarma yok (ORTA); süzgeçlerin görünür etiketi yok (ORTA); ters aralıkta eski tablo ekranda kalıyor (DÜŞÜK); hareketsiz günler "AÇIK (kapanış yapılmadı)" gürültüsü — 42 satırın ~36'sı (DÜŞÜK). |
| 24 | `/raporlar/islem-defteri` | İşlem defteri | BULGU | `islem-defteri` 200 (süzgeç + sayfalama); kasıtlı ters aralık **400**. | 6 bulgu (0 KRİTİK). Sayfa açıklaması yanlış (ORTA, HATA); tür süzgecinde `SAYIM_DUZELTME` yok ama listede var (ORTA, HATA); tür/ödeme ham enum (ORTA); `EMANET_CIKIS` satırı YÖN'de "Giriş" (ORTA, HATA); tarih süzgeçlerinde etiket yok, ters aralıkta eski tablo kalıyor (DÜŞÜK); "EMANET_GIRISStorno" bitişik (DÜŞÜK). |
| 25 | `/raporlar/satis-analizi` | Satış analizi | BULGU | `sube-karsilastirma` + sekme GET'leri 200; kasıtlı ters aralık **400**. | 4 bulgu (0 KRİTİK). Sayfa açıklaması yanlış (ORTA, HATA); NAKİT kartı TL tutarını farklı para birimlerinin hareket sayısıyla birleştiriyor, ₺20.036 çıkışı hiç göstermiyor (ORTA, HATA); yüzde nokta ondalıklı "50.0%" (DÜŞÜK); etiketsiz tarih süzgeci + ters aralıkta eski tablo (DÜŞÜK). |
| 26 | `/denetim-izi` | Denetim izi | BULGU | `denetim/ozet` ve `denetim/kayitlar` 200 (sayfalama dâhil). 4xx/5xx yok. | 7 bulgu (0 KRİTİK). Bitiş tarihi filtresi son günü kapsam dışı bırakıyor (ORTA, HATA, off-by-one); kayıtların %82'si otomatik kur anlık görüntüsü (ORTA); tür süzgecinde ham kod adları + yinelenen türler (ORTA); 0 kayıt dönen listeler "kişisel veri erişimi" sayılıyor (DÜŞÜK, HATA); çift getirme → aynı satır iki kez (DÜŞÜK, HATA); 5.500 kayıtta yalnız "Daha fazla göster", dışa aktarım yok (DÜŞÜK); "QA TestSAHIP" bitişik (DÜŞÜK). |
| 27 | `/evraklar` | Çek/senet portföyü | BULGU | `evraklar` GET 200, **POST 201**, `PATCH .../{id}` 200. Yeni 4xx/5xx yok. | 3 bulgu (0 KRİTİK). Süzgeç sonucu boşsa tablo tamamen kayboluyor, "kayıt yok" mesajı yok (ORTA); kilit metni hangi alanın eksik olduğunu söylemiyor (ORTA); ~890px'te başlık bloğu tek kelimelik satırlara sıkışıyor (ORTA). **Düzenleme uçtan uca çalıştı ve kalıcı.** |
| 28 | `/evraklar/takvim` | Vade takvimi | HATA | `evraklar/takvim?bas&bit` 200 (iki pencere). 4xx/5xx yok. | 5 bulgu (0 KRİTİK, **2 YÜKSEK**). **Portföyde ₺6.250,50 tutarında 2 evrak varken "Portföyde evrak yok" yazıyor (YÜKSEK, HATA)**; **gün bağlantısındaki `vadeBas/vadeBit` süzgeci hedef sayfada sessizce yok sayılıyor (YÜKSEK, HATA)**; "takvim" adına rağmen düz tablo, yalnız tarih metni tıklanabilir (ORTA); tarihler ISO biçiminde (DÜŞÜK); "Düzeltme şöyle yapılır:" satırı bağlantı etiketiyle bitiyor (DÜŞÜK). |
| 29 | `/pos` | POS cihazları | BULGU | `pos/cihazlar` GET/POST/PATCH ve `pasife-al` 200. Doğrulama hatalarında hiç istek gitmiyor. | 6 bulgu (0 KRİTİK). **"Bağlı Kasa" oluşturmadan sonra düzenlenemiyor ve gösterilen düzeltme yolu bu alanla ilgisiz (YÜKSEK, DÜZENLENEBİLİRLİK)**; doğrulama mesajları hatayı değil yardım metnini tekrarlıyor (ORTA); komisyon oran girilip yüzde gösteriliyor — 100 kat hata riski (ORTA); "CİHAZ SAYISI" pasifleri de sayıyor (DÜŞÜK); taksit oranları yalnız düzenlemede var (DÜŞÜK); oluşturma POST'u 201 yerine 200, bant bilgisiz (DÜŞÜK). |
| 30 | `/pos/tahsilatlar` | POS alacak defteri | BULGU | `pos/tahsilatlar` ve `valor-takvimi` 200; kasıtlı geçersiz kimlikle `oto-eslestir-onerisi` **404**. | 5 bulgu (0 KRİTİK). Aynı boş durum mesajı iki kez basılıyor (ORTA); eşleştirme hatası tıklanan düğmeden 685px yukarıda, ekran dışında (ORTA); tarih süzgeçlerinin erişilebilir adı yok (ORTA); sayfa üç farklı adla anılıyor (DÜŞÜK); açıklama ve "değiştirilemez" uyarısı aynı cümleyi tekrarlıyor (DÜŞÜK). **Eşleştirme/storno akışı KANITLANMADI (kiracıda tahsilat yok).** |
| 31 | `/kurulum` | Kuruluş sihirbazı | HATA | `kurulum/durum` 200, `acilis/prova` 200 (negatif ve döviz denemeleri). 4xx/5xx yok. | 5 bulgu (0 KRİTİK, 1 YÜKSEK). **Kuru prova NEGATİF açılış bakiyesini (−250 ₺) "geçerli, uygulanabilir" sayıyor (YÜKSEK, HATA — tur 4'ten beri AÇIK; mühür geri alınamadığı için kalıcı hasar riski)**; kabul edilmeyecek döviz kasası listede, hata ham kodla (`DOVIZ_HESAP_FAZ_B`) (ORTA); kilitli düğmenin gerekçesi yanıltıcı ("Kuru prova çalıştır" derken prova çalıştırılmıştı) (ORTA); "Eksik" adımlara gidiş bağlantısı yok (ORTA); `document.title` yok (DÜŞÜK). |
| 32 | `/veri-ice-aktarim` | Veri içe aktarım | BULGU | `ice-aktarim/prova` **400** (kasıtlı yanlış kolon) → 200; `uygula` 200; `geri-al` 200. | 5 bulgu (0 KRİTİK). Uygulandıktan sonra prova özeti eskimiş kalıyor ("Yazılan kayıt: 0") ve bu turda başarı bandı hiç çıkmadı (ORTA, HATA, KÖTÜLEŞME); başlık+açıklama iki kez (DÜŞÜK); hata mesajı ham API yolu gösteriyor (DÜŞÜK); "Düzeltme şöyle yapılır:" düğme adıyla bitiyor (DÜŞÜK); geri alma diyaloğunun başlığı eylemi adlandırmıyor (DÜŞÜK). |
| 33 | `/veri-disa-aktarim` | Veri dışa aktarım | BULGU | `kaynaklar`, `gecmis`, `POST disa-aktarim`, `.../indir` 200. Geçersiz gerekçede hiç istek yok. | 5 bulgu (0 KRİTİK). İki açıklama çelişiyor: "şifreli arşiv" ↔ "standart CSV" (ORTA); üç kapsam ham slug (`pos-cihazlari`…) (ORTA); geçmiş satırlarında hiçbir eylem yok — yeniden indirme de yok (ORTA, DÜZENLENEBİLİRLİK); arşiv parolası açık metin, göster/gizle yok (DÜŞÜK); eski satırlarda SATIR sütunu "—" (DÜŞÜK). |
| 34 | `/yedekler` | Yedekler | BULGU | `ozet`, liste, tatbikat, talepler 200; `POST yedekler/talep` **201**; `PATCH .../talep/{id}` 200. | 8 bulgu (0 KRİTİK). Başarısız/uyuşmayan koşular için hata nedeni gösterilmiyor (ORTA, HATA); doğrulanmamış yedek uyarısı var ama "şimdi doğrula" eylemi yok (ORTA); iptal diyaloğunda iki düğme de "iptal" kelimesiyle (ORTA); talep oluşturulunca hiçbir başarı geri bildirimi yok (ORTA); durumlar ham enum (ORTA); başlık iki kez (DÜŞÜK); "Bu kayıt değiştirilemez" kutusu her satırda (DÜŞÜK); RPO taahhüdü aşılmış (587/360 dk) ama vurgulanmıyor (DÜŞÜK). |
| 35 | `/kullanicilar` | Kullanıcılar | HATA | **`GET kullanicilar` → 429 (2×)**, ~45 sn sonra 200; `PATCH kullanicilar/ben` 200 (2×). Konsoldaki 104 hatanın tamamı 429. | 4 bulgu (0 KRİTİK, **2 YÜKSEK**). **429'da sayfa sessizce boş kalıyor: "Yeniden dene" yok, otomatik tekrar yok (YÜKSEK, HATA)**; **tek SAHİP olan kiracıda kendi rolü/erişimi hiçbir yoldan değiştirilemiyor — gösterilen yol uygulanamaz (YÜKSEK, DÜZENLENEBİLİRLİK)**; dar ekran 825>375 (ORTA); Yeni Kullanıcı diyaloğu açılır açılmaz hata metinleriyle geliyor (DÜŞÜK). |
| 36 | `/yetkiler` | Yetkiler | BULGU | `yetkiler`, `yetkiler/benim` 200 (bir kez 429); `PUT yetkiler` 200 (2×, değiştir + geri al). | 4 bulgu (0 KRİTİK). **Matriste ROL sütunu yapışkan değil: kutuya tıklanınca tablo sağa kayıyor, rol adları görünmez oluyor (YÜKSEK, GÖRSEL)**; matris ekranların yalnız bir bölümünü kapsıyor (12 sütun; Emanet/Kur/Vezne/POS/Sayım/Uyum/Yedekler/Denetim İzi yok) (ORTA); "SİPARİS" yazım hatası (DÜŞÜK); SAHİP satırında son 4 kutunun erişilebilir adı ham kod (DÜŞÜK). |
| 37 | `/isletme` | İşletme | HATA | `subeler` GET/POST/PATCH 200; kimlik PATCH 200. Bu sayfada JS istisnası yok. | 5 bulgu (0 KRİTİK, 1 YÜKSEK). **İşletme kimliği telefonu doğrulanmıyor: "abc" kaydediliyor — aynı alan şube formunda doğrulanıyor (YÜKSEK, HATA)**; kur kaynağı ve maliyet yöntemi hiç belirlenmemişken bile kilitli (ORTA, DÜZENLENEBİLİRLİK); dar ekran 729>375 (ORTA); başlık+açıklama iki kez (DÜŞÜK); Yeni Şube çekmecesi hata metniyle açılıyor, kilit sözcüğü farklı (DÜŞÜK). |
| 38 | `/ayarlar` | Ayarlar (tema, dil, mod) | BULGU | `auth/ben`, `mfa/durum`, `meta/surum`, `yetkiler/benim` 200; `PATCH kullanicilar/ben` 200 (2×). | 3 bulgu (0 KRİTİK). Yayın anlık görüntüsünde künye "Ortam: Geliştirme" gösteriyor (ORTA, HATA); yarım kalan iki aşamalı giriş kurulumu temizlenemiyor (ORTA, DÜZENLENEBİLİRLİK); İngilizce arayüzde rol çevrilmiyor ("Role SAHIP") (DÜŞÜK). Dil TR↔EN ve tema Sistem/Aydınlık/Karanlık uçtan uca çalıştı. |
| 39 | `/kilavuz` | Kılavuz | HATA | `GET /kilavuz` 200, `/kilavuz.html` 200 (50.313 bayt, **oturumsuz da 200**). CSP/iframe sorunu bu turda YOK. | 6 bulgu (0 KRİTİK, **2 YÜKSEK**). **"Uygulamayı başlatma" bölümü hâlâ `localhost:5000` + `start.bat` + Docker anlatıyor, 5199 hiç geçmiyor (YÜKSEK)**; **kılavuz ekranların büyük bölümünü (Vezne, Emanet, POS, Sayım, Uyum, Siparişler, Gider pusulaları, Yedekler, Veri aktarımı) hiç anlatmıyor (YÜKSEK)**; uygulamaya dönüş bağlantısı yok (ORTA); bölüm listesi 13/18 eksik (ORTA); içerik oturumsuz herkese açık (DÜŞÜK); rota sayfası neredeyse boş, açıklama iki kez (DÜŞÜK). |

## Çapraz kontroller
- [x] **Karanlık tema tüm sayfalarda okunaklı** — 39/39 rotada uygulanıyor (gövde `rgb(11,18,32)`, metin `rgb(232,238,246)`). **Tek istisna:** `/fiyatlandirma` `tr.satir-vurgu` koyu temada açık amber zeminde kalıp neredeyse beyaz metinle okunmaz oluyor (YÜKSEK).
- [ ] **Dar ekran 375px: yatay kaydırma yok — KISMEN BAŞARILI (tur 1'e göre büyük iyileşme).** 39 rotanın 31'i temiz (`scrollWidth == clientWidth == 375`). Taşan 8 rota: `/faturalar` 1178 · `/cariler` 996 · `/urunler` 859 · `/kullanicilar` 825 · `/banka` 789 (tur 4'te 751 — **kötüleşme**) · `/urunler/agac` 751 · `/isletme` 729 · `/islemler` 571 (**YENİ**). Hepsinin kaynağı tablo genişliği; tek bir "dar ekranda tabloyu karta çevir / overflow-x sarmalayıcı" düzeltmesi sekizini birden kapatır.
- [ ] **Basit / profesyonel mod anahtarı — BU TURDA ÖLÇÜLMEDİ** (grupların hiçbiri mod anahtarını ayrıca test etmedi). Tur 4 sonucu geçerli sayılmalı; tur 6'da yeniden ölçülmeli.
- [x] **Oturum yenileme / 401 davranışı** — `POST auth/yenile` 200 ile taze jeton alınabiliyor; süresi dolmuş jetonla 401 temiz dönüyor. **Ama** fikstür dosyası boş olduğu için üç grup yalnız tarayıcıda duran canlı oturum sayesinde çalışabildi (kırılgan). Ayrıca `/kullanicilar` **429**'da hiçbir kurtarma yolu sunmuyor.
- [ ] **Her kaydet akışında başarı bandı — HÂLÂ BAŞARISIZ.** İçerikli bant veren ekranlar: `/evraklar` ("…Müşterinin güncel TL bakiyesi: -₺6.250,50"), `/kasalar` ("Gelir ₺250,00 kaydedildi. Yeni bakiye: …"), `/kur`, `/emanet`, `/cariler`, `/urunler`, `/yetkiler`. Bant vermeyen ya da görünmeyen ekranlar: `/uyum` (201 sonrası bant yok), `/yedekler` (201 sonrası bant yok), `/veri-ice-aktarim` (uygula sonrası bant yok — **kötüleşme**), `/bildirimler` (bant ekran dışında, top = −155px), `/banka` (içeriksiz "Hareket kaydedildi!"), `/pos` (yalnız "Kaydedildi").
- [x] **Veri girişi düzenlenebilirliği (kurucu şartı) satır satır tarandı** — aşağıdaki "Düzenlenemeyen veri girişleri" bölümüne bakınız.

## Tur 4 → Tur 5 karşılaştırması

Bu bölümün tüm sayıları grup sonuçlarındaki `onceki_bulgular` alanlarından hesaplanmıştır.

| Ölçüt | Değer |
|---|---|
| Tur 4'ten devralınıp bu turda **tek tek yeniden denenen** bulgu | **148** |
| **KAPANDI** | **14** (%9,5) |
| **KISMEN** kapandı | **15** (%10,1) |
| **AÇIK** kaldı | **119** (%80,4) |
| Grup bazında kapanan | G1 **5** · G2 **0** · G3 **5** · G4 **1** · G5 **3** |
| Grup bazında kısmen | G1 3 · G2 1 · G3 5 · G4 2 · G5 4 |
| Grup bazında açık | G1 21 · G2 30 · G3 33 · G4 12 · G5 23 |

**Not (Md.1):** grup 4'ün dört rotasında (`/evraklar`, `/evraklar/takvim`, `/pos`, `/pos/tahsilatlar`) `onceki_bulgular` **boş** gönderildi; bu rotalar için tur 4 → tur 5 karşılaştırması yapılamadı. Bu yüzden karşılaştırma tabanı 148'dir, tur 4'ün toplam 149 bulgusu değil.

### KAPANAN 14 bulgu (kanıtlı)
1. `/islemler` — **tur 4'ün tek KRİTİK bulgusu kapandı:** reyon alanı artık boş "—" varsayılanlı gerçek açılır liste; kanıt olarak gerçek satış kaydedildi (`POST islemler/satis` 200, fiş no alındı).
2. `/islemler` — MASAK/tevsik kilidinin gerekçesi artık tam yazılıyor (₺30.000 haddi + "HAVALE/EFT veya kart seçin").
3. `/islemler` — eski hata mesajı formda kalmıyor; alan bazlı, anlık doğrulama.
4. `/islemler` — "Son satışlar" listesi artık gerçek satış listesi gibi davranıyor (iade/ters kayıt eylemleriyle).
5. `/urunler/agac` — isimsiz "0" düğmesi kalktı; `main` içindeki 14 düğmenin tamamının erişilebilir adı var.
6. `/uyum` — şüpheli bildirim "Kapat" akışı düzeldi: `PATCH masak/supheli/{id}/kapat` **200**, satır KAPATILDI; gerekçe ≥20 karakter doğrulaması çalışıyor.
7. `/gider-pusulalari` — hurda alış akışı tamam: `/islemler` "Hurda Alış" sekmesinde "Satıcı tipi" alanı var ve akış gerçek belge üretti (GP-2026-000001, TCKN + adres dolu).
8. `/gider-pusulalari` — detay modalı artık Escape ile kapanıyor.
9. `/denetim-izi` — özet kartları filtreyle senkron (filtre temizlenince 5.502'ye dönüyor).
10. `/raporlar/vade-takibi` — hızlı ufuk düğmeleri "secili" sınıfı alıyor.
11. `/yedekler` — başarı bandındaki ham ULID kaldırıldı (ancak bant tümüyle kayboldu → yeni bulgu).
12. `/yetkiler` — matris artık geç yüklenmiyor (4 sn içinde tam matris).
13. `/isletme` — pasif şube "Aktife Al" ile geri alınabiliyor (doğrulandı).
14. `/bildirimler` — filtre değişiminde çift istek kalktı (her sekme için tek istek).

### KISMEN kapanan 15 bulgu (öne çıkanlar)
- `/` — "Kuru güncelle" hâlâ 503, **ama** hata metni artık çözüm veriyor ("Kuru elle girin: Kur ekranı → Manuel kur").
- `/islemler` — kalem silinince ürün seçimi artık yanıltıcı görünmüyor; ancak "Ekle" sessiz etkisizliği sürüyor.
- `/cariler` — hücrede asılı hata metni gözlenmedi; **değer kaybı sürüyor**.
- `/gider-pusulalari` — Adres ve Mal açıklaması artık düzenlenebilir (PATCH 200, kalıcı); yasal çekirdek alanlar VUK md.234 gereği kilitli. Liste uçlarındaki çift getirme düzeldi, **detay ucunda sürüyor**.
- `/uyum` — 404'te artık "Hareket bulunamadı" bandı var; alan düzeyinde işaret yok. Kimlik tespiti aramasına statik açıklama eklenmiş; Enter hâlâ sessiz.
- `/kurulum` — döviz reddinde artık okunabilir Türkçe sebep var; ham kod ve seçilebilir döviz kasası duruyor. Kilit gerekçesi düğmenin altında gösteriliyor ama **yanlış gerekçe** yazıyor.
- `/siparisler` — doğrulama mesajı artık görünür alanda (top −56 → 523); `role=alert` ve odak taşıma yok.
- `/yetkiler` — açılıştaki kendiliğinden yatay kayma düzelmiş; kutuya tıklanınca yeniden kayıyor.
- `/kilavuz` — bölüm bağlantılarının `target` değeri `_blank` olmuş; QA bölmesi tek sekme kullandığı için gerçek davranış **kanıtlanamadı**.
- `/bildirimler` — "Şimdi çalıştır" sonrası metin netleşti ama bant ekran dışında kalıyor.

### KÖTÜLEŞENLER (gerileme)
1. `/banka` — dar ekran taşması **751 → 789 px**.
2. `/veri-ice-aktarim` — "1 kayıt uygulandı" başarı bandı **tamamen kayboldu**; uygulamanın tek göstergesi geçmiş satırı.
3. `/yedekler` — ULID kaldırılırken başarı bandı da kaldırıldı, yerine kullanıcıya dönük mesaj konmadı.
4. `/sayim` — DURUM hücresi artık düğme bile değil, **düz metin** "İptal Et".
5. `/emanet` — "sebep yazmıyor" kusuru artık `+ Emanet Al` düğmesinde de var.

### Düzenlenemeyen giriş listesi farkı (tur 4 → tur 5)
- **Tur 4:** 26+ kalem, ağırlıklı olarak "gerekçesiz kilit".
- **Tur 5:** **32 kalem** kayda geçti; ancak niteliği değişti — kalemlerin çoğunda ekranda artık **gerekçe + somut düzeltme yolu** yazıyor (`/islemler` satış fişi, `/vezne` VEZNEDE fişi, `/banka` dekont, `/kasalar` hareket, `/emanet` hareket, `/gider-pusulalari` yasal alanlar, `/pos/tahsilatlar`, `/denetim-izi`, `/yedekler` koşuları). Bu tur 4'e göre belirgin iyileşmedir.
- **Yeni kapanan:** `/gider-pusulalari` Adres + Mal açıklaması artık düzenlenebilir; `/isletme` pasif şube aktife alınabiliyor.
- **Yeni eklenen (tur 4'te yoktu):** `/pos` "Bağlı Kasa" düzenlemeden kaldırılmış ve yanlış düzeltme yolu gösteriyor; `/vezne` düzenleme penceresinde "İşçilik (TL)" alanı eksik; `/bildirimler` zamanlanmış iş ayarları hiç düzenlenemiyor ve gerekçe de yok; `/veri-disa-aktarim` geçmişinde hiçbir eylem yok; `/ayarlar` yarım MFA kurulumu temizlenemiyor.

### Ortalama puan farkları
| Ölçüt | Tur 4 | Tur 5 | Fark |
|---|---|---|---|
| Ortalama görsel puanı | 3,4 / 5 (yaklaşık, 35 sayfa) | **3,31 / 5** (129/39) | ≈ −0,1 |
| Ortalama kullanıcı dostu puanı | 3,0 / 5 (yaklaşık, 35 sayfa) | **2,92 / 5** (114/39) | ≈ −0,1 |
| Gezilen sayfa | 35 | **39** | +4 |

**Yorum:** puan ortalamalarındaki hafif düşüş kalite gerilemesi değil, **kapsam genişlemesi**dir — bu turda ilk kez / daha derin taranan `/pos/tahsilatlar` (2/2), `/yetkiler` (2/3), `/kurulum` (3/2), `/kilavuz` (3/2), `/evraklar/takvim` (3/2) gibi düşük puanlı ekranlar ortalamayı aşağı çekti. Aynı ekranların tur 4 puanları da benzer düzeydeydi.

### Yeni bulgu sayısı
- Toplam bulgu **193**; bunların **134'ü** tur 4'ten devralınan AÇIK (119) veya KISMEN (15) bulguların yeniden doğrulanmış hâlidir.
- **≈ 59 bulgu bu turda ilk kez kayda geçti.** Öne çıkan gerçek yeniler:
  1. `/cariler` → `/islemler` **pasif cariler satışta seçilebiliyor** (YÜKSEK).
  2. `/gider-pusulalari` **boş adresle kayıt** — VUK md.234 (YÜKSEK).
  3. `/islemler` dar ekran taşması 571>375 (ORTA, tur 4'te raporlanmamıştı).
  4. `/pos` "Bağlı Kasa" düzenlenemezliği ve yanlış düzeltme yolu (YÜKSEK).
  5. `/kullanicilar` 429'da boş ekran + kurtarma yolu yok (YÜKSEK).
  6. `/vezne` düzenleme penceresinde "İşçilik (TL)" eksikliği (ORTA).
  7. `/sayim` satır içi düzenleyicinin dışarıdan güncellemede boş kalması (ORTA).

## Bulgu özeti — TUR 5 (2026-09-18)

### Sayılar
| Ölçüt | Değer |
|---|---|
| Gezilen rota | **39** (5 paralel grup: 8+8+9+8+6) |
| Toplam bulgu | **193** |
| **KRİTİK** | **1** |
| YÜKSEK | **26** |
| ORTA | 87 |
| DÜŞÜK | 79 |
| Tur 4'ten devralınan bulgu | 148 → **14 kapandı · 15 kısmen · 119 açık** |
| Bu turda ilk kez kaydedilen bulgu | ≈ **59** |
| Ortalama görsel puanı | **3,31 / 5** (129/39) |
| Ortalama kullanıcı dostu puanı | **2,92 / 5** (114/39) |
| Düzenlenemeyen veri girişi kalemi | **32** (çoğunda gerekçe + düzeltme yolu ekranda yazılı) |
| Kendiliğinden oluşan konsol/ağ hatası | `POST kur/guncelle` **503** (`/` ve `/kur`), `GET kullanicilar` **429**, `kur/guncel` 130+ tekrar ve içinden bir **401**; diğer tüm 4xx'ler kasıtlı geçersiz denemelerden |

### KRİTİK bulgu (1)
- **`/faturalar` — TASLAK fatura ve kalemleri hiç düzenlenemiyor.** Kanıt: bu turda oluşturulan `FTR-2026-000005` "Taslak" durumundayken satır eylemleri yalnız `Kalemleri Göster · Kes · İptal Et`; "Düzenle" yok. Aynı durum FTR-2026-000004 ve -000002 için de geçerli. Düzeltmenin tek yolu iptal + yeni fatura, ve iptalin kendi metnine göre bile **"fatura numarası serisinde kullanılmış sayılır"** — yani her düzeltme bir numara yakıyor. **Öneri:** TASLAK'ta başlık, cari, KDV ve kalemler düzenlenebilir olsun; kilit yalnız "Kes" sonrası devreye girsin. Kurucunun "her veri girişi düzenlenebilir" kuralı burada doğrudan ihlal ediliyor.

### YÜKSEK bulgular (26 — rota + kanıt + öneri)
1. **`/vezne` kuyruk toplamı ACIK fişlerde ₺0,00 (HATA/PARA).** Kanıt: `GET vezne/kuyruk?durum=ACIK` → `toplamTl:0, kalemSayisi:1`; aynı fişin detayı `toplamTl:2000`. Fiş VEZNEDE'ye geçince liste doğru (₺3.000). Öneri: kuyruk toplamını detayla aynı kaynaktan hesapla; "ACIK fişin kuyruk toplamı = detay toplamı" gerileme testi ekle.
2. **`/cariler`→`/islemler` pasif cariler satışta seçilebiliyor (HATA, YENİ).** Kanıt: satış cari listesi 4 seçenek, `/cariler` yalnız 2 aktif; farkı oluşturan iki kayıt Pasif. Öneri: cari listelerini yalnız aktiflerle doldur; sunucuda da pasif cariye yeni hareket reddedilsin.
3. **`/cariler` aktif cari pasife alınamıyor (DÜZENLENEBİLİRLİK).** Kanıt: aktif satır eylemlerinde "Pasife Al" yok; yalnız pasifte "♻ Aktife al" var. Öneri: `/urunler`teki onaylı "🚫 Pasife Al" desenini caride de uygula.
4. **`/kur` geçmişteki "İptal et" hiçbir şey yapmıyor (HATA).** Kanıt: hem JS `.click()` hem gerçek fare tıklamasıyla hiçbir istek/kip/bant yok; girilen manuel kur (₺6.820,50) **geri alınamadı**. Öneri: onclick bağını ve yetkiyi doğrula; desteklenmiyorsa düğmeyi devre dışı bırakıp sebebini yaz.
5. **`/kur` başarısız "Şimdi çek" ekrandaki kur kartını ve ayar tablosunu siliyor (HATA).** Kanıt: 503 sonrası `main` içinde "K22" geçmiyor. Öneri: hatada son bilinen anlık görüntüyü koru, yanına "güncellenemedi" rozeti koy.
6. **`/kur` kaynaklar kapalıyken "Şimdi çek" etkin — garanti 503.** Öneri: düğmeyi devre dışı bırak, kullanıcıyı "Manuel Kur Gir"e yönlendir.
7. **`/sayim` satır içi "Sayılan"da Enter kaydetmiyor.** Kanıt: ipucu "Enter kaydeder" diyor; gerçek klavyeyle "3"+Enter → hiç istek yok, özet "0 / 2". Barkod okuyucu Enter gönderdiği için **sayım verisi sessizce kaybolur**. Öneri: `onkeydown`'da Enter'ı yakala; Esc/blur davranışını da test et.
8. **`/sayim` düzenleme açılınca alan odaklanmıyor, yazılan değer kayboluyor.** Kanıt: input eklendi ama `activeElement = BODY`, `input.value = ''`. Öneri: `FocusAsync` + mevcut değeri seçili getir.
9. **`/sayim` DURUM sütunu gerçek durumu göstermiyor; hücrede düz metin "İptal Et" (HATA).** Öneri: gerçek durum rozeti (AÇIK/IPTAL/KAPANDI), "İptal Et" ayrı EYLEMLER sütununda ve yalnız AÇIK oturumlarda.
10. **`/sayim` zorunlu "Şube" boşken hiçbir alan işaretlenmiyor.** Kanıt: `#sy-sube.className='secim'`, `aria-invalid=null`, tek mesaj "Form eksik ya da hatalı". Öneri: hata sınıfı + `aria-invalid` + alan altı mesaj + ilk hatalı alana odak.
11. **`/faturalar` dolu formda ilk "Kaydet" etkisiz, ikincide kaydediyor (HATA).** Kanıt: 1. tıklamada hiç POST yok ve eski hatalar ekranda; 2. tıklamada POST 200. Öneri: doğrulama durumunu tıklamadan önce tazele; "ilk tıklama doğrular" desenini kaldır.
12. **`/faturalar` "Fişten Fatura Kes" 26 haneli ULID'yi elle yazdırıyor.** Öneri: arama kutulu fiş seçici (son 50 fiş); ULID hiç görünmesin.
13. **`/siparisler` durum geçişi onaysız, geri alınamıyor ve "Düzenle"yi de yok ediyor (DÜZENLENEBİLİRLİK).** Kanıt: `confirm` sayacı 0; "Onaylandı" sonrası düğmeler `['Atölyede','Hazır','Siparişi iptal et']`. Öneri: ileri geçişi onay kipine bağla, geri geçişe izin ver, Onaylandı'da Düzenle açık kalsın.
14. **`/fiyatlandirma` karanlık temada `satir-vurgu` okunmuyor (GÖRSEL).** Kanıt: zemin `rgb(255,251,235)`, metin `rgb(232,238,246)`. Öneri: tema belirtecine bağlı ayrı token ya da `border-left` vurgusu.
15. **`/gider-pusulalari` boş adresle pusula kaydedilebiliyor (HATA, VUK md.234).** Kanıt: adres boşaltıldı → `PATCH` 200 + "Pusula güncellendi." Öneri: istemci + sunucu zorunluluk doğrulaması.
16. **`/raporlar` "Stok Dağılımı" pastası tek kategori %100 iken hiç çizilmiyor (HATA).** Kanıt: `path.getBBox()` genişlik 0 (yay başı = sonu). Öneri: tek dilimde `<circle>` çiz ya da bitişi 359,99°'ye çek.
17. **`/evraklar/takvim` "Portföyde evrak yok" diyor, oysa portföyde ₺6.250,50 tutarında 2 evrak var (HATA).** Öneri: metni "Seçili tarih aralığında vadesi gelen evrak yok" yap, portföy toplamını yine göster.
18. **`/evraklar/takvim` gün bağlantısındaki `vadeBas/vadeBit` hedef sayfada uygulanmıyor (HATA).** Kanıt: `/evraklar?vadeBas=…` sonrası tarih kutuları boş, tüm evraklar listeleniyor. Öneri: sorgu parametrelerini ilk yüklemede oku ve süzgeci rozetle göster.
19. **`/pos` "Bağlı Kasa" oluşturmadan sonra düzenlenemiyor ve gösterilen düzeltme yolu ilgisiz (DÜZENLENEBİLİRLİK).** Kanıt: düzenleme kipinde hiç `<select>` yok; metin POS tahsilatı iptalini anlatıyor. Öneri: alanı düzenlenebilir yap ya da bu alana özel doğru düzeltme yolu yaz.
20. **`/kurulum` kuru prova NEGATİF açılış bakiyesini geçerli sayıyor (HATA).** Kanıt: kasa −250 ₺ → `prova` 200, "Tüm satırlar geçerli — uygulanabilir", "Açılışı uygula" etkinleşti. Mühür geri alınamadığı için **kalıcı yanlış açılış devri** doğurur. Öneri: sunucu provasında kasa/banka için `< 0` satır hatası; istemcide `min=0`.
21. **`/kullanicilar` 429'da sayfa sessizce boş kalıyor (HATA).** Kanıt: `GET kullanicilar` 429 (2×), tabloda tek satır yok, "Yeniden dene" yok, otomatik tekrar yok (~45 sn). Öneri: "Yeniden dene" düğmesi + üstel geri çekilme + açık boş durum.
22. **`/kullanicilar` tek SAHİP kendi rolünü/erişimini hiçbir yoldan değiştiremiyor (DÜZENLENEBİLİRLİK).** Kanıt: Düzenle'de rol alanı yok, metin "Rolünüzü başka bir SAHİP değiştirebilir", listede tek kullanıcı → çıkışsız döngü. Öneri: "Yeni SAHİP ata ve devret" akışı.
23. **`/yetkiler` ROL sütunu yapışkan değil (GÖRSEL).** Kanıt: tıklama sonrası kabın `scrollLeft=252`, ilk hücre `left=13` (görünür alanın dışında). Öneri: `position: sticky; left: 0`.
24. **`/isletme` işletme kimliği telefonu doğrulanmıyor: "abc" kaydediliyor (HATA).** Kanıt: aynı değer şube formunda reddediliyor. Bu alan fatura/gider pusulasında basılıyor. Öneri: aynı kuralı iki formda da uygula (istemci + sunucu).
25. **`/kilavuz` "Uygulamayı başlatma" bölümü yanlış adresi ve geliştirici yordamını anlatıyor.** Kanıt: metinde `localhost:5000`, `start.bat`, Docker var; `5199` hiç geçmiyor. Öneri: son kullanıcı diline çevir, gerçek adresi yaz.
26. **`/kilavuz` ekranların büyük bölümünü hiç anlatmıyor.** Kanıt: 18 başlık arasında Vezne, Emanet, POS, Sayım, Uyum, Siparişler, Gider pusulaları, Yedekler, Veri aktarımı yok. "Satışa hazır" iddiası için bu belge açığı kapatılmalı.

### Düzenlenemeyen veri girişleri (32 kalem)
**A. Gerçek eksiklik (kurucu şartına aykırı, gerekçesiz ya da yanlış gerekçeli) — 13**
1. `/faturalar` — TASLAK faturanın hiçbir alanı (KRİTİK).
2. `/pos` — "Bağlı Kasa" (yanlış düzeltme yolu gösteriliyor).
3. `/cariler` — aktif cari pasife alınamıyor.
4. `/cariler` — satır içi düzenleme Enter ile kaydetmiyor, değer sessizce kayboluyor.
5. `/urunler/agac` — kategori pasife alınamıyor/silinemiyor (reyonlarda var).
6. `/vezne` — düzenleme penceresinde "İşçilik (TL)" alanı yok.
7. `/siparisler` — taslak siparişte cari/tür/şube değiştirilemiyor.
8. `/siparisler` — "Onaylandı" durumunda "Düzenle" düğmesi tamamen kayboluyor.
9. `/sayim` — oturum ADI açıldıktan sonra düzeltilemiyor (ad satır kümesini etkilemez).
10. `/kullanicilar` — kendi rolü/erişimi (tek SAHİP'te çıkışsız).
11. `/isletme` — kur kaynağı ve maliyet yöntemi hiç belirlenmemişken bile kilitli.
12. `/ayarlar` — yarım kalan iki aşamalı giriş kurulumu temizlenemiyor.
13. `/bildirimler` — zamanlanmış iş ayarları (saat/eşik/açık-kapalı) düzenlenemiyor ve gerekçe de gösterilmiyor.

**B. Gerekçeli ve düzeltme yolu ekranda yazılı (muhasebe/mevzuat kilidi; kabul edilebilir) — 19**
`/islemler` satış fişi (iade / ters kayıt yolu yazılı) · `/vezne` VEZNEDE fişi (gerekçeli iptal + yeni fiş) · `/banka` dekont (ters dekont; yine de açıklama/karşı taraf düzeltilebilmeli) · `/banka` dekont "Bekliyor" durumu · `/kasalar` kasa hareketi (storno) · `/emanet` hareketi (storno) · `/kur` geçmiş kaydı (iptal yolu **bozuk** — yukarıdaki YÜKSEK bulgu) · `/gider-pusulalari` yasal çekirdek alanlar (VUK md.234) · `/uyum` şüpheli bildirim tutar/gerekçe (MASAK) · `/uyum` GONDERILDI kaydında hiç eylem kalmaması (gerekçesiz — B'den A'ya geçmeye aday) · `/pos/tahsilatlar` satırları (satıştan türetilir) · 6 rapor ekranı (`/raporlar`, `/raporlar/kar-zarar`, `/raporlar/vade-takibi`, `/raporlar/kasa`, `/raporlar/islem-defteri`, `/raporlar/satis-analizi` — türetilmiş, düzenlenebilirlik beklenmez) · `/denetim-izi` (KVKK md.12, değişmezlik gerekli) · `/evraklar` tutar + veren müşteri · `/veri-ice-aktarim` uygulanmış aktarım (geri alma çalışıyor) · `/veri-disa-aktarim` geçmiş satırları (ama gerekçe metni ekranda YOK ve "yeniden indir" de yok) · `/yedekler` talep gerekçesi (iptal izi kalıyor) · `/yedekler` koşu/tatbikat tabloları · `/kurulum` mühür sonrası açılış (mühür test edilmedi).

### Görsel / kullanıcı dostu ortalamaları
- **Görsel: 3,31 / 5** (toplam 129 / 39 sayfa). Grup ortalamaları: G1 3,88 · G2 3,00 · G3 3,33 · G4 3,13 · G5 3,17.
- **Kullanıcı dostu: 2,92 / 5** (toplam 114 / 39 sayfa). Grup ortalamaları: G1 3,13 · G2 2,75 · G3 3,11 · G4 2,75 · G5 2,83.
- En düşük puanlı ekranlar: `/pos/tahsilatlar` (2/2), `/yetkiler` (2/3), `/kur` (3/2), `/faturalar` (2/2), `/sayim` (4/2), `/raporlar/kasa` (3/2), `/kurulum` (3/2), `/kilavuz` (3/2), `/evraklar/takvim` (3/2), `/kullanicilar` (3/2).
- En yüksek: `/islemler` (4/4), `/evraklar` (4/4), `/ayarlar` (4/4), `/raporlar/kar-zarar` (4/4), `/raporlar/vade-takibi` (4/4).
- **Kullanıcı dostuluğun başlıca düşmanları (tekrar eden desenler):** sessiz etkisizlik (Ekle, boş gerekçeli iptal, Enter, ilk Kaydet, kur "İptal et"), sistemin bildiği değerin tekrar sorulması (liste fiyatı), kaydın süzgeç yüzünden "kaybolmuş" görünmesi, ham ULID/enum gösterimi, görünmeyen ya da hiç olmayan başarı bandı.

### Düzeltme paketine giren maddeler (DP5)
1. **DP5-01 · `/faturalar` TASLAK düzenleme (KRİTİK).** Kes öncesi tam düzenlenebilirlik; iptal numara serisini yakmasın ya da bu açıkça anlatılsın.
2. **DP5-02 · Para hatası riski (YÜKSEK ×2).** `/vezne` kuyruk ACIK toplamı; `/kurulum` negatif açılış bakiyesi kapısı — ikisine de gerileme testi.
3. **DP5-03 · Sessiz etkisizlik tek elden (YÜKSEK ×6).** `/kur` "İptal et", `/sayim` Enter + odak, `/faturalar` ilk Kaydet, `/islemler` "Ekle", `/vezne` boş gerekçeli iptal, `/cariler` satır içi Enter. Kural: **hiçbir tıklama sessiz kalmaz** — ya iş yapar, ya devre dışıdır (sebebiyle), ya da doğrulama mesajı gösterir.
4. **DP5-04 · Pasif kayıt bütünlüğü (YÜKSEK ×2).** `/islemler` cari listesi yalnız aktifler; `/cariler` ve `/urunler/agac` "Pasife Al" deseni.
5. **DP5-05 · `/kur` dayanıklılığı (YÜKSEK ×2).** 503'te anlık görüntü ekranda kalsın; kaynak kapalıyken "Şimdi çek" devre dışı + sebep. Ayrıca `kur/guncel` tekilleştirmesi (130+ istek, 429'un ana kaynağı).
6. **DP5-06 · 429 dayanıklılığı (YÜKSEK).** Her liste ekranında "Yeniden dene" + üstel geri çekilme + açık boş durum.
7. **DP5-07 · Mevzuat doğrulaması (YÜKSEK ×2).** `/gider-pusulalari` adres zorunluluğu (VUK md.234); `/isletme` telefon doğrulaması (fatura/pusulada basılıyor).
8. **DP5-08 · `/sayim` liste durumu + zorunlu alan işareti (YÜKSEK ×2).**
9. **DP5-09 · `/evraklar/takvim` iki temel hata (YÜKSEK ×2).** Boş durum metni + derin bağlantı süzgeci.
10. **DP5-10 · Düzenlenebilirlik A listesi (13 kalem).** Öncelik: `/pos` bağlı kasa, `/vezne` işçilik, `/siparisler` cari/tür/şube + Onaylandı'da Düzenle, `/sayim` oturum adı, `/kullanicilar` SAHİP devri, `/isletme` ilk seçim, `/ayarlar` MFA sıfırlama, `/bildirimler` iş ayarları.
11. **DP5-11 · Dar ekran ortak düzeltmesi (ORTA, 8 rota).** `/faturalar` 1178 · `/cariler` 996 · `/urunler` 859 · `/kullanicilar` 825 · `/banka` 789 · `/urunler/agac` 751 · `/isletme` 729 · `/islemler` 571 — tek sarmalayıcı + <768px kart düzeni + gerileme testi.
12. **DP5-12 · Karanlık tema + matris (YÜKSEK ×2).** `/fiyatlandirma` `satir-vurgu` token; `/yetkiler` yapışkan rol sütunu.
13. **DP5-13 · Başarı bandı kalıbı (ORTA, 6 rota).** `/uyum`, `/yedekler`, `/veri-ice-aktarim`, `/bildirimler` (ekran dışı), `/banka` (içeriksiz), `/pos`. Kalıp: tutar + nesne + yeni durum, ham kimlik yok, görünür alanda.
14. **DP5-14 · Ham teknik değer gösterimi (ORTA).** ULID (`/islemler`, `/vezne` ×3, `/urunler` stok şube, `/faturalar` fiş seçici), ham enum (`/yedekler`, `/raporlar/islem-defteri`, `/denetim-izi`, `/kur`), `DOVIZ_HESAP_FAZ_B`, `/api/...` yolu, `pos-cihazlari` slugları.
15. **DP5-15 · Kopyala-yapıştır metin temizliği (ORTA).** "Sevk edilmiş raporların tam listesi" 3 rapor sayfasında; çift başlık `/veri-ice-aktarim`, `/veri-disa-aktarim`, `/yedekler`, `/isletme`, `/kilavuz`; "Düzeltme şöyle yapılır:" bileşeninin bozuk çıktısı; `/veri-disa-aktarim` çelişkili iki açıklama; kilit metninin tek kalıba ("Eksik olan: <alan>") indirilmesi.
16. **DP5-16 · Ters tarih / geçersiz süzgeç (ORTA, 5 rota).** Hatalı süzgeçte eski tablo geçersizleştirilsin; `/gider-pusulalari` sunucu 400 dönsün; `/raporlar/vade-takibi` negatif ufuk istemcide engellensin.
17. **DP5-17 · Rapor tutarlılığı (ORTA).** "Defter has gramı" ↔ "Net has gram hareketi"; NAKİT kartının para birimi kırılımı; stok kartlarına "dönemden bağımsız" notu; `SAYIM_DUZELTME` süzgeç seçeneği; `EMANET_CIKIS` ve `/emanet` storno yön etiketi; `/raporlar` katalog tekilleştirme.
18. **DP5-18 · `/denetim-izi` doğruluğu (ORTA).** Bitiş tarihi off-by-one; kur anlık görüntüleri denetim izine yazılmasın (%82); 0 kayıtlı listeler KVKK sayacına girmesin; çift getirme; sayfalama + CSV.
19. **DP5-19 · Sayı/dil biçimi tek kültür (ORTA/DÜŞÜK).** Gram tr-TR virgül, yüzde `%50,0`, tarih gg.aa.yyyy (`/evraklar`, `/evraklar/takvim`), `MANUAL` → "Elle girildi", EN'de rol çevirisi.
20. **DP5-20 · Onay ve geri alma (ORTA).** `/fiyatlandirma` pasife alma, `/siparisler` durum geçişi; `/yedekler` iptal diyaloğunda "Vazgeç" adlandırması.
21. **DP5-21 · Erişilebilirlik (DÜŞÜK, toplu).** İsimsiz/yinelenen düğmeler (`/urunler/agac` "+ Yeni Kategori" ×3, `/sayim` "→", `/cariler` satır eylemleri, `/siparisler` kalem girdileri, `/pos/tahsilatlar` tarih kutuları), bitişik rozetler ("QA TestSAHIP", "EMANET_GIRISStorno", "…DUZmaliyet eksik", "TaslakKalemler"), `main` içinde eksik h1 (`/siparisler`, `/bildirimler`, `/vezne`), `aria-pressed`/`role=alert` eksikleri, "SİPARİS" yazım hatası.
22. **DP5-22 · `/kilavuz` ve yayın künyesi (YÜKSEK + ORTA).** Başlatma bölümü 5199'a göre yeniden yazılsın, eksik 9 ekran belgelensin, bölüm listesi dosya başlıklarından üretilsin, "Uygulamaya dön" bağlantısı; `/ayarlar` künyesi yayın profilinde "Üretim" desin.
23. **DP5-23 · `/yedekler` operasyonel boşluklar (ORTA).** Hata nedeni sütunu, "şimdi doğrula" eylemi, uyuşmayan tablo adları, RPO aşımı rozeti.

## Temizlik
- **Fikstür kiracısı KORUNDU** (`01M2NZ97WE0PC3EGQG0NZX3W74` — "QA-TEST-3 Kuyumculuk"): sonraki turlar ve kurucuya gerçekçi örnek göstermek için kasıtlı olarak **silinmedi**. Bu turda hiçbir DELETE çalıştırılmadı. `SELECT COUNT(*) FROM tenants` → **8**.
- Test verisinin büyük bölümü test sırasında geri alındı: vezne fişi gerekçeyle iptal · kasa hareketi storno · emanet girişi storno · sayım oturumu iptal · içe aktarım geri alma (kayıtlar pasife alındı) · yedek talebi iptal · MASAK bildirimi kapatma · `FTR-2026-000005` ve `S-2026-000004` iptal · POS cihazı pasife alınıp yeniden aktifleştirildi · pasife alınan ürün aktife alındı · `QA-TEST G5 Sube` pasife alındı · yetki değişikliği geri alındı · kullanıcı soyadı, profil adı, işletme telefonu, gider pusulası adresi eski değerlerine geri yazıldı · dil TR, tema "Sistem".
- **Geri ALINAMAYAN tek değişiklik (Md.1):** `/kur` ekranındaki "İptal et" düğmesi çalışmadığı için girilen manuel kur **₺6.820,50 (MANUAL)** kiracının güncel kuru olarak duruyor (önceki değer ALTINKAYNAK ₺6.813,24). Dış kaynaklar kapalı olduğu için kendiliğinden de dönmeyecek.
- Silinemediği için duran kalıntılar: cari `QA-TEST Grup1 Tur5 Musteri DUZENLENDI`, ürün `QA-TEST G1 T5 Urun` (QAT5-001), kategori `QA-TEST G1 T5 Kategori`, satış fişi (₺45.000 HAVALE), fiyat listesi `QA-TEST G2 T6 Liste DUZ`, kasa `QA-TEST G2 T6 Kasa DUZ`, ₺750 banka dekontu, evrak `QA-TEST-G4T5-CEK-010` (₺1.250,50, portföyde), kapatılmış QA MASAK bildirimi, dışa aktarım kanıt satırı, pasif `QA-TEST G5 Sube`.
- Kaynak kod değiştirilmedi, git komutu çalıştırılmadı, commit yapılmadı, 5198 portuna dokunulmadı, derleme/test koşulmadı. Jeton/parola/arşiv parolası değeri hiçbir çıktıya yazılmadı.
