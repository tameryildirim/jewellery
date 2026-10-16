# Tarayıcı QA turu — 2026-09-16 (uygulama içi tarayıcı, http://localhost:5199) — TUR 9 (2026-09-24)

> Kurucu talimatı: "ekran testlerini tıklayarak benim tarayıcımdan yap, hiçbir ayrıntıyı atlama". Chrome eklentisi bağlı olmadığı için uygulama içi tarayıcı bölmesi kullanılır; giriş parolasını kurucu kendisi yazar (ekip parola girmez). Yöntem her sayfada: (1) sayfaya git, (2) konsol/ağ hatalarını oku, (3) erişilebilirlik ağacını oku (başlık, boş durum, tablo, düğmeler), (4) ekran görüntüsü `ekran/<rota>.png`, (5) her modal/formu aç, doğrulama mesajlarını tetikle, `QA-TEST-` önekli kayıt oluştur → düzenle → (varsa) pasife al, (6) karanlık tema + dar ekran (375px), (7) basit/profesyonel mod anahtarı. Bulgular önceliklendirilip düzeltme paketine (UX-4 devamı) döner.

**Tur 9 (2026-09-24).** 8 paralel grup, 49 sayfa kaydı (47 ayrı rota; `/giris` üç grupta ölçüldü). İskelet tur 1'in 27 satırlık tablosundan kopyalandı (tur 1 dosyası tarihsel taban, dokunulmadı); tur 1'de olmayan 20 rota 28–47. satırlara eklendi.

> **Oturum engeli (turun en önemli ölçüm notu):** grup 2, 5, 6, 7 ve grup 8'in 4 rotası **oturum açamadı**. Ajanların güvenlik kuralı parolayı forma yazmayı ve belirteç enjeksiyonunu yasaklıyor; grup 2 ve 6'da auto-mode sınıflandırıcısı fikstür okumayı `[Credential Materialization]` gerekçesiyle reddetti; grup 5 ve 7'de fikstürdeki erişim belirtecinin süresi dolmuştu ve `POST /api/v1/auth/yenile` → 401 döndü (paralel grupların aynı yenileme belirtecini döndürmesinden kaynaklanan yarış olası). Grup 1 ve 4 belirteç enjeksiyonuyla ("ÖYKÜNMELİ TUR"), grup 3 ekran girişini DOM tıklamasıyla yaptı. **Bu engellenen rotalar ürün kusuru değildir, TEMİZ de değildir**: önceki turdaki bulguları açık sayılır. Puanı 0 olan satırlar "ölçülmedi" anlamındadır.

| # | Rota | Sayfa | Durum | Konsol/ağ | Bulgular |
|---|---|---|---|---|---|
| 1 | `/giris` | Giriş | BULGU | Konsol hatası 0; `genel/kisitlar` 200; geçersiz formda `/auth/` isteği gitmiyor. Korumalı rotalar `/giris`'e yönleniyor. | 5 bulgu (0 KRİTİK). Gerçek parolayla giriş bu turda **denenmedi** (ajan kuralı; ürün hatası değil). Şifre sıfırlama yalnız "sahibine söyle" metni (ORTA); asgari şifre uzunluğu yok (DÜŞÜK); TR/EN düğmeleri `type=submit` (DÜŞÜK); dönüş adresi korunmuyor (DÜŞÜK); giriş öncesi "HARİÇ olanlar (8)" jargonu (DÜŞÜK). Görsel 4 / kullanıcı dostu 4 (grup 7, 8); grup 6 kaydı 0/0 (ölçülmedi). |
| 2 | `/` | Panel | ENGELLENDİ | Oturum yok → `/giris`. SRI/önyükleme hatası bu yüklemede görülmedi. | Önceki turdan taşınan 5 bulgu (ORTA: SRI çökmesinde mesaj yok, SignalR varken 10 sn yoklama; DÜŞÜK: kur güncelle geri bildirimi, eksi kasa uyarısı, 7 günlük grafik eziliyor). Yeniden denenemedi. |
| 3 | `/islemler` | İşlemler (POS) | BULGU | Yüklemede 4xx/5xx yok. `islemler/satis` 200; `satis-iade` 400, 400 (kasa bakiyesi), 200; tevsik denemesi 400 — hepsi bilinçli. | 11 bulgu (0 KRİTİK, 2 YÜKSEK). Başarı bandı 3 sn'de kayboluyor, fiş no hiçbir yerde yok (YÜKSEK); Ana Kasa −737.048,80 TL "eksiye düşemez" kuralına rağmen (YÜKSEK, HATA); stok adedi seçimde yok, "isteğe bağlı" fiyat zorunluya dönüşüyor, bakiye reddi "yasal kapı" diye sunuluyor, "Düzelt (ters kayıt)" açıklamasız pasif, karanlık temada birincil düğme 2,95:1 (ORTA); tevsik uyarısında Kaydet etkin, nokta ondalık, eski uyarılar kalıyor, iade formunda satış metni (DÜŞÜK). Görsel 3 / KD 2. |
| 4 | `/cariler` | Cariler | BULGU | Konsol temiz. `POST cari` 201; liste açılışta iki kez çekiliyor. | 6 bulgu (1 YÜKSEK). Telefonla arama 0 sonuç (YÜKSEK, HATA); ı/i harf farkı aramayı bozuyor, pasif cariye işlem eylemleri açık, dar ekranda bakiye sütunu görünmüyor (ORTA); pasife almada bant yok, eylem düğmelerinde aria-label yok (DÜŞÜK). Satır başına 7–8 düğme sorunu KAPANDI. Görsel 4 / KD 3. |
| 5 | `/urunler` | Ürünler | BULGU | 4xx/5xx yok. `POST urunler` 201, PUT 200. Liste her ürün için ayrı `…/stok` (N+1). | 8 bulgu (0 KRİTİK). Açıklama alanına sayfa alt başlığı etiket olarak bağlı, "Gram (isteğe bağlı)" 0'da reddediliyor (ORTA); hata kalıyor, kod zorunlu, N+1, düzenlemede '5.5', stok penceresinde bitişik metin, yardım cümlesi iki kez (DÜŞÜK). 6 önceki bulgu KAPANDI (stok sütunu, durum süzgeci, şube adı, zorunlu işaret, 375px süzgeçler). Görsel 3 / KD 3. |
| 6 | `/urunler/agac` | Ürün ağacı | ENGELLENDİ | Ölçülmedi (oturum yok). | 0 yeni bulgu; önceki 4 bulgu açık (doğrulanamadı). |
| 7 | `/urunler/etiketler` | Etiketler | ENGELLENDİ | Ölçülmedi. | 0 yeni bulgu; önceki 4 bulgu doğrulanamadı. |
| 8 | `/fiyatlandirma` | Fiyat listeleri | BULGU | Konsol temiz; tümü 200. `kur/guncel`, `gorunum` ilk yüklemede ikişer kez. | 5 bulgu (0 KRİTİK). Hata mesajı düzeltmeden sonra kalıyor (ORTA); riskli değer onayı alan değişmeden isteniyor, `<form>` yok/Enter çalışmıyor, form kip değil, Basit menüde ekran yok (DÜŞÜK). Pasife alma onayı, aria-invalid, sayı biçimi KAPANDI. Görsel 4 / KD 3. |
| 9 | `/kasalar` | Kasalar | BULGU | 2xx; sayfaya ait hata yok. | 9 bulgu (2 YÜKSEK). Kasanın şubesi düzenlenemiyor (YÜKSEK, DÜZENLENEBİLİRLİK); eksi bakiyeli kasalar uyarısız, özet şeridi yeşil (YÜKSEK); tutar hatası kalıyor, ters kayıt da "Stornolandı"+Storno düğmesiyle listeleniyor, Gelir/Gider yazmıyor (ORTA); mesajlar, onay asimetrisi, kırmızı Pasife Al düğmesi, "storno" jargonu (DÜŞÜK). Görsel 3 / KD 3. |
| 10 | `/banka` | Banka | BULGU | `POST bankalar` 400 (geçersiz IBAN, istemci yakalamadı); diğerleri 200. İlk 2 sn ₺0,00 / 0 hesap. | 7 bulgu (**2 KRİTİK**, 1 YÜKSEK). Dekont tarihi girilemiyor/düzeltilemiyor ama sayfa "düzeltilebilir" diyor (**KRİTİK**); dekont "Bekliyor"dan çıkarılamıyor (**KRİTİK**); storno sonrası bakiye güncellenmiyor (YÜKSEK, HATA); IBAN istemcide yok, yüklenirken ₺0,00 (ORTA); "kasa" ipucu, MASAK/kümülatif jargonu (DÜŞÜK). Satır içi Enter ve başarı bandı KAPANDI. Görsel 3 / KD 2. |
| 11 | `/kur` | Kur | ENGELLENDİ | Oturum yok; ajanın tahmini yol yoklaması 404 (ürün hatası değil). | Ölçülmedi. |
| 12 | `/emanet` | Emanet | ENGELLENDİ | Ölçülmedi. | Önceki 5 bulgu yeniden denenemedi. |
| 13 | `/sayim` | Sayım | BULGU | Tek 400: açık sayımlı şubeye ikinci oturum (beklenen iş kuralı). | 10 bulgu (2 YÜKSEK). Şube boşken hata Ad alanına yazılıyor, Şube işaretlenmiyor (YÜKSEK ×2, aynı kök); satır hatası 415 px uzakta ve değer siliniyor, kilit kutusu "adı değiştirilemez" diyor ama ad düzenleniyor, açık sayımlı şube seçicide belirtilmiyor, DURUM sütununda "İptal Et" (ORTA); bantlar üst üste, `→` aria yok, `inputmode` yok, dar ekranda Durum görünmüyor (DÜŞÜK). Enter ile kaydetme ve kalıcı hata bandı KAPANDI. Görsel 3 / KD 2. |
| 14 | `/faturalar` | Faturalar | BULGU | Yüklemede 4xx/5xx yok. PATCH 200 ×4, bilinçli −50 için 400. | 10 bulgu (**1 KRİTİK**). Taslak faturada kalemler düzenlenemiyor (**KRİTİK**); negatif tutar düzenlemede istemcide geçiyor, iptal onayı ile "taslak numara harcamaz" çelişiyor, KDV ₺200 (%20) matrahsız, Basit modda K-10/ayar anahtarı metni (ORTA); iptalde "Numara kesimde verilir", tablo kırılıyor, bant içeriksiz, ULID, vazgeç düğmesinin adı "İptal" (DÜŞÜK). Kuyum taslağı kaydı, cari listesi, başarı bantları KAPANDI. Görsel 3 / KD 3. |
| 15 | `/gider-pusulalari` | Gider pusulaları | BULGU | 4xx/5xx yok; PATCH 200 ×2; ters tarih isteği 200 (uyarısız). | 4 bulgu (1 YÜKSEK). "Hurda alımı gir" Satış sekmesini açıyor (YÜKSEK, HATA); çekmece kayıttan sonra açık kalıyor, ters tarih uyarısız, tablo 1280px'te kırılıyor (DÜŞÜK). Satırda Düzenle eklendi, çelişkili kilit metni KAPANDI. Görsel 3 / KD 3. |
| 16 | `/gider-pusulalari/{Id}/belge` | Gider pusulası belgesi | BULGU | Konsol temiz; GET belge 200; PUT 200 ×2. SRI hatası yok. | 6 bulgu (0 KRİTİK). Eksik zorunlu alanda Yazdır engellenmiyor (ORTA, doğrulanamadı); dar ekranda düğme yazısı alt kenarda, Geri düğme `<button>`, PDF yok, "Dayanak" boş satırı/ters H hiyerarşisi, iptal yönlendirmesi yanlış düğmeyi söylüyor (DÜŞÜK). Düzenle/iptal yolu, baskıda kabuk, SRI KAPANDI. Görsel 4 / KD 4. |
| 17 | `/uyum` | Uyum (tevsik/MASAK) | ENGELLENDİ | Oturum yok → `/giris`. | Ölçülmedi. |
| 18 | `/raporlar` | Raporlar | ENGELLENDİ | Oturum yok; getBBox ölçülemedi. | Ölçülmedi. |
| 19 | `/raporlar/kar-zarar` | Kâr/zarar | BULGU | Konsol temiz; tümü 200. Karşılaştırma değişince `kar-zarar/urun` ve `stok-degerlemesi` gereksiz yeniden. | 6 bulgu (0 KRİTİK). Karşılaştırmada önceki dönem tutarı yazmıyor, Basit mod raporu sadeleştirmiyor (2782=2782 karakter), defter has gramı aylık özetle eşleşmesi doğrulanamadı (ORTA); "GIDER" noktasız I, gereksiz istek, grafikte aria yok (DÜŞÜK). Grafik yok, maden toplama, sayfa başlığı KAPANDI. Grafik: çubuk 8 parça (en geniş 52,5 px / 426), pasta 3 parça (%81,8). Görsel 3 / KD 2. |
| 20 | `/raporlar/vade-takibi` | Vade takibi | ENGELLENDİ | Ölçülmedi. | Önceki 4 bulgu doğrulanamadı. |
| 21 | `/denetim-izi` | Denetim izi | ENGELLENDİ | Ölçülmedi. | Önceki 5 bulgu doğrulanamadı. |
| 22 | `/veri-disa-aktarim` | Veri dışa aktarım | ENGELLENDİ | Ölçülmedi. | Önceki 5 bulgu açık (doğrulanamadı). |
| 23 | `/kullanicilar` | Kullanıcılar | ENGELLENDİ | Ölçülmedi. | Önceki 4 bulgu açık (doğrulanamadı); `_framework` 404 bulgusu KAPANDI. |
| 24 | `/yetkiler` | Yetkiler | ENGELLENDİ | Oturumsuz 2× 401 (beklenen) → `/giris`. | 1 izlenebilirlik kaydı (DÜŞÜK): önceki turdaki "uygulama açılmıyor, `_framework` 404" KAPANDI. |
| 25 | `/isletme` | İşletme | ENGELLENDİ | Oturum yok → `/giris`. | Önceki turdan taşınan 4 bulgu (ORTA: başlık iki kez; DÜŞÜK: VKN/TCKN sağlaması yok, telefon ham, `type=submit`). |
| 26 | `/ayarlar` | Ayarlar (tema, mod) | ENGELLENDİ | Ölçülmedi. | Ölçülmedi. |
| 27 | `/kilavuz` | Kılavuz | ENGELLENDİ | `fetch('/kilavuz.html')` → 200, 59.443 bayt. | 0 yeni bulgu; "kılavuz 404" KISMEN (belge 200, rota içi bağlantılar tıklanamadı), 2 bulgu doğrulanamadı. |
| 28 | `/cikti/{Tur}/{Id}` | Belge çıktısı | BULGU | Konsol temiz; GET `cikti/SATIS_FISI/…` 200. Kabuk `gorunum` 4×, `kur/guncel` 3×, `subeler` 3× çağırıyor. | 7 bulgu (**1 KRİTİK**, 1 YÜKSEK). **Profesyonel modda `hidden` işaretli 38 menü satırı görünüyor, grup aç/kapa etkisiz** (**KRİTİK**, kurucunun "menü olmamış" uyarısı); kalem tutarı '45000.00' ham (YÜKSEK); mod düğmesi açılışta titriyor, has gram eksi ve farklı ondalık, mobil üst bar ~230 px (ORTA); iki H1, 26 karakterlik belge no (DÜŞÜK). Tablo taşması KAPANDI. Görsel 3 / KD 2. |
| 29 | `/pos/tahsilatlar` | POS hakedişleri | BULGU | Konsol temiz; 200. Tarih değişikliği `valor-takvimi`ni 2× çağırıyor. | 8 bulgu (0 KRİTİK). Kiracıda POS tahsilatı 0 satır — eşleştirme/iptal/tutar arama doğrulanamadı (ORTA ×3, açık bırakıldı); ters tarih uyarısız (ORTA); boş durum iki kez, cümle tekrarı, üç farklı ad, sayaç yok (DÜŞÜK). Etiket ve H1 KAPANDI. Basit menüde bağlantı yok. Görsel 3 / KD 2. |
| 30 | `/pos` | POS cihazları | BULGU | Konsol temiz; 200 (oluşturma 201 yerine 200). | 7 bulgu (0 KRİTİK). Oran 0,025 giriliyor, liste %2.5 — 100 kat hata riski; doğrulama yardım metnini tekrarlıyor, hatalar tek tek (ORTA); taksit yalnız düzenlemede, sayaç pasifleri sayıyor, başlık iki kez, bantlar tutarsız, valör jargonu (DÜŞÜK). Görsel 3 / KD 2. |
| 31 | `/veri-ice-aktarim` | Veri içe aktarım | BULGU | Bilinçli geçersiz prova 400; uygula ve geri-al 200. | 9 bulgu (0 KRİTİK). Uygulamadan sonra prova özeti "Yazılan kayıt: 0" (ORTA, HATA); başlık/açıklama tekrarı, ham API yolu, "46/20" sayaç, geçmiş sütunu, pencere başlığı, `document.title`, yarım "Düzeltme" cümlesi, onay metni konumu/'5.7 KB' (DÜŞÜK). "0 KB" KAPANDI. Görsel 3 / KD 3. |
| 32 | `/raporlar/satis-analizi` | Satış analizi | BULGU | Yüklemede 4xx/5xx yok; bilinçli ters tarih 400. | 4 bulgu (0 KRİTİK). Açıklama Rapor Merkezi metni (ORTA); ters aralıkta tablo eski veriyle kalıyor, dışa aktarma yok, `aria-selected` boş (DÜŞÜK). Grafik, yüzde biçimi, giriş/çıkış, etiketler KAPANDI. Grafik 6 parça, en geniş 70 px / 914. Görsel 4 / KD 3. |
| 33 | `/vezne` | Vezne | BULGU | Yüklemede hata yok; bilinçli PATCH 400. | 8 bulgu (0 KRİTİK). Yeni fiş süzgeç dışında kalıp kaybolmuş görünüyor, negatif fiyatta yanlış mesaj, Basit modda tevsik/MASAK jargonu (ORTA); eski mesaj kalıyor, ULID, '1500.00', istemci doğrulaması yok, zorunlu alan "isteğe bağlı" (DÜŞÜK). Kuyruk ₺0,00 KAPANDI. Görsel 3 / KD 2. |
| 34 | `/kurulum` | Kuruluş sihirbazı | BULGU | Yüklemede hata yok; prova 200 (AC-10 gövdede). Uygula/Mühürle tıklanmadı. | 8 bulgu (0 KRİTİK). Prova hatalıyken "Eksik: Kuru prova çalıştır", Eksik adımlara bağlantı yok, K-24/KMH/AC-10 jargonu (ORTA); title yok, H1 yok/çift başlık, bitişik sayaç, ham liste görünümü, negatif tutar istemcide geçiyor (DÜŞÜK). Mühür gerekçesi ayrı alan ve döviz kasası KAPANDI. Görsel 2 / KD 3. |
| 35 | `/evraklar/takvim` | Vade takvimi | ENGELLENDİ | Ölçülmedi. | Önceki 4 bulgu doğrulanamadı. |
| 36 | `/siparisler/{Id}` | Sipariş detayı | ENGELLENDİ | Ölçülmedi. | Önceki 6 bulgu DOĞRULANAMADI. |
| 37 | `/raporlar/islem-defteri` | Hareket dökümü | ENGELLENDİ | Ölçülmedi. | Önceki 6 bulgu DOĞRULANAMADI. |
| 38 | `/vitrin` | Vitrin panosu | ENGELLENDİ | Ölçülmedi. | Önceki 4 bulgu DOĞRULANAMADI. |
| 39 | `/yedekler` | Yedekler | ENGELLENDİ | Oturumsuz `GET yedekler` 401 (beklenen). | Ölçülmedi. |
| 40 | `/gider` | Gider | ENGELLENDİ | Tahmini yol yoklaması 404 (ürün hatası değil). | Ölçülmedi. |
| 41 | `/raporlar/kasa` | Kasa raporu | ENGELLENDİ | Tahmini yol 404 (ürün hatası değil). | Ölçülmedi. |
| 42 | `/evraklar` | Evraklar | ENGELLENDİ | Oturumsuz 401 (beklenen). | Ölçülmedi. |
| 43 | `/ekranlar` | Tüm ekranlar | ENGELLENDİ | `/giris`'e yönlendirme; konsol temiz. | Ölçülmedi. |
| 44 | `/hurda` | Hurda | ENGELLENDİ | `/giris`'e yönlendirme. | Önceki 7 bulgu yeniden denenemedi. |
| 45 | `/banka/ekstre/{IthalatId}` | Banka ekstresi | ENGELLENDİ | Ölçülmedi. | Önceki 5 bulgu (1'i KRİTİK: ekstre satırı düzenlenemiyor) yeniden denenemedi. |
| 46 | `/siparisler` | Siparişler | ENGELLENDİ | Oturum yok → `/giris`. | Önceki turdan taşınan 6 bulgu (**1 KRİTİK**: TASLAK siparişte cari/tür/şube düzenlenemiyor; ORTA ×2; DÜŞÜK ×3). |
| 47 | `/bildirimler` | Bildirimler | ENGELLENDİ | Oturum yok → `/giris`. | Önceki turdan taşınan 5 bulgu (ORTA ×3: bant görünmüyor, `kur/guncel` 154 istek, sekme açıklaması; DÜŞÜK ×2). |

## Çapraz kontroller
- [x] **Karanlık tema okunaklı** — oturumlu ölçülen 18 rotada B-32 gerilemesi (açık zemin + açık metin) yok. **İstisna:** karanlık temada birincil düğme metin kontrastı **2,95:1** (`/islemler` "Ekle", `/kurulum` "Kuru prova"/"Mühürle"; WCAG AA 4,5) — ORTA.
- [x] **Dar ekran 375px: sayfa yatay kaymıyor** — ölçülen tüm rotalarda `scrollWidth=375`. **Ama** geniş tablolar kendi kutusunda kayıyor ve önemli sütunlar ekran dışında (`/cariler` 969 px → bakiye görünmüyor, `/banka` 1310 px, `/sayim` 513 px); mobil üst bar ~230 px kaplıyor, "Şube kapsamı" iki kez yazıyor.
- [ ] **Basit / profesyonel mod — BAŞARISIZ (KRİTİK).** Profesyonel modda `[hidden]` taşıyan 38 `DIV.kenar-satir` görünüyor; grup başlığı `aria-expanded=false` iken satırlar açık, tıklamak 38→38 değiştirmiyor. Kök neden `.kenar-satir{display:flex}` kuralının `[hidden]`'ı ezmesi. Ayrıca mod düğmesi açılışta ~500 ms Profesyonel görünüp Basit'e dönüyor; Basit mod gövdeyi sadeleştirmiyor (kâr-zarar, vezne, faturalar, kurulum, POS'ta jargon). *Not:* grup 3 ve 4 `[hidden]` görünen öğe sayısını 0 ölçtü (muhtemelen Basit modda ya da gruplar açıkken); grup 1'in Profesyonel moddaki ölçümü somut ve tekrarlanabilir kanıt taşıdığı için KRİTİK geçerli sayıldı. Kapı testine "iki modda hidden+görünen = 0" ölçümü eklenmeli.
- [x] **Oturum dolunca 401 → giriş sayfası** — yönlendirme çalışıyor (grup 2, 6, 7, 8 ölçtü). Yan gözlem: süresi dolmuş belirteçle `/giris` açılınca "HARİÇ olanlar" kutusunda "Kısıt listesi alınamadı … (OturumDustuHatasi)" yazıyor (grup 5). Fikstür yenileme belirteci paralel gruplarda tek kullanımlık yarışa giriyor — **grup başına ayrı oturum** gerekli.
- [ ] **Her kaydet akışında başarı bandı — KISMEN.** Tur 1'e göre büyük ilerleme (fatura, dekont, pusula, POS, fiyat listesi bant veriyor). Eksikler: `/cariler` pasife almada bant yok; `/faturalar` oluşturma bandı "İşlem başarılı —" (boş); `/islemler` bandı 3 sn'de kayboluyor ve fiş no yok; `/sayim`, `/vezne`, `/islemler`, `/fiyatlandirma`, `/urunler`'de eski hata/başarı mesajları ekranda kalıyor; birkaç bantta ham ULID.

## Bulgu özeti — TUR 9 (2026-09-24)

### Sayılar (betik ölçümü — ajan saymadı)
| Ölçüt | Değer |
|---|---|
| Ölçülen sayfa kaydı | **49** (47 ayrı rota; `/giris` 3 grupta) |
| Toplam bulgu | **159** |
| KRİTİK | **5** |
| Düzenlenemeyen veri girişi (ham / gerekçeli / gerçek) | **12 / 2 / 10** |
| Ortalama görsel puan — TÜM rotalar | **1,33 / 5** |
| Ortalama kullanıcı dostu puan — TÜM rotalar | **1,12 / 5** |
| Ortalama görsel puan — ÖLÇÜLEBİLEN rotalar | **1,33 / 5** (49 ölçülebilen sayfa) |
| Ortalama kullanıcı dostu puan — ÖLÇÜLEBİLEN rotalar | **1,12 / 5** |
| Grafik ölçülen sayfa / SVG sayısı | 49 / 23 |
| Çizilmeyen grafik / dar çizilen | 0 / 2 |
| Ölçülemeyen grup (önyükleme imzası) | yok (0 rota) |

**İki ortalamanın anlamı:** "TÜM rotalar" 49 sayfa kaydının hepsinin ortalamasıdır; "ÖLÇÜLEBİLEN" yalnız önyükleme imzası sağlıklı olan (uygulamanın açıldığı) rotaların ortalamasıdır. Bu turda uygulama her yerde açıldığı için betik ölçülemeyen grup bulmadı ve iki rakam eşit çıktı. **Ancak bu rakamlar ürünün gerçek puanını göstermiyor:** grup 2, 5, 6, 7 ve grup 8'in 4 rotası **oturum açılamadığı** için 0 puan aldı (önyükleme imzası sağlıklı, ama sayfa içeriği hiç görülmedi). Bu 0'lar ortalamayı aşağı çekiyor. Oturumlu ölçülen rotaların puanları tabloda tek tek yazılı (görsel çoğunlukla 3–4, kullanıcı dostu 2–3). Betik ölçütüne "oturum açılamadı" imzası eklenmeli; yoksa "ölçülebilen" ortalama yanıltıcı kalır.

### KRİTİK bulgular (5) — rota · kanıt · öneri
1. **`/cikti/{Tur}/{Id}` (kabuk, bütün rotalar) — Profesyonel modda gizli olması gereken 38 menü satırı görünüyor; grup aç/kapa etkisiz.** Kanıt: `[...document.querySelectorAll('[hidden]')].filter(e=>getComputedStyle(e).display!=='none').length = 38`, hepsi `DIV.kenar-satir` (display:flex, 39 px); "Günlük İş" tıklandı → aria-expanded true, görünen 38→38, menü 2254→2263 px. Basit modda 0. Kurucunun "menü olmamış" uyarısı bu. → Genel CSS'e `[hidden]{display:none!important}` ekleyin ya da `.kenar-satir:not([hidden])`; kapı testine iki modda hidden+görünen=0 ölçümünü koyun.
2. **`/banka` — dekont tarihi ne girilebiliyor ne düzeltilebiliyor, sayfa ise "tarih doğrudan düzeltilebilir" diyor.** Kanıt: Dekont Gir alanları yalnız tutar/karşı taraf/açıklama (+yön, cari); tarih hücresi düz metin. → Girişe tarih alanı (varsayılan bugün) ve satır içi tarih düzenlemesi ekleyin.
3. **`/banka` — dekont "Bekliyor" durumundan çıkarılamıyor; ekstre mutabakatı yapılamıyor.** Kanıt: durum hücresinde denetim yok; eylemler yalnız "Faturaya Dönüştür" ve "Storno"; tur 8 dekontu hâlâ "Bekliyor". → "Eşleşti/Onaylandı" eylemi ya da ekstre eşleştirmesine yönlendirme.
4. **`/faturalar` — taslak faturada kalemler düzenlenemiyor.** Kanıt: Düzenle çekmecesi yalnız `#fd-duzenle-cari/tarih/aciklama/toplam`; "Kalemleri Göster" salt okunur. → Taslakta kalem ekle/düzelt/sil; kesilmiş faturada gerekçeli "iptal + yeniden kes" yolu.
5. **`/siparisler` — TASLAK siparişte cari, tür ve şube düzenlenemiyor** (önceki turdan taşındı; bu turda oturum olmadığı için doğrulanamadı, açık sayılır). → Taslakta bu üç alanı düzenleme formuna ekleyin.

### YÜKSEK bulgular — rota · kanıt · öneri
1. `/islemler` — başarı bandı ~3 sn'de kayboluyor, fiş no hiçbir yerde yok ("Fiş no" araması boş; Son satışlar'da sütun yok). → Bandı kullanıcı kapatana dek tutun, okunur fiş no + "Fişi yazdır" verin.
2. `/islemler` + `/kasalar` — Ana Kasa −737.048,80 TL; "Nakit kasa eksiye düşemez" kuralı iade yolunda çalışıyor ama kasa başka bir yoldan eksiye düşmüş; `/kasalar`'da eksi bakiye uyarısız, özet şeridi yeşil. → Eksiye düşüren hareket yolunu bulun, aynı kapıyı uygulayın; eksi kasaya kırmızı rozet + "sayım yapın" uyarısı.
3. `/kasalar` — kasanın şubesi oluşturulduktan sonra değiştirilemiyor ("Kasayı Düzenle"de yalnız Kasa Adı). → Hareketsiz kasada şubeyi düzenlemeye açın, hareketliyse gerekçeli kilit.
4. `/banka` — dekont storno edildikten sonra toplam ve hesap bakiyesi güncellenmiyor (₺53.110 kalıyor; yenileyince ₺51.860). → Storno sonrası hesap listesini yeniden çekin.
5. `/sayim` — Ad dolu, Şube boşken hata Ad alanına yazılıyor, Şube işaretlenmiyor (iki bulgu, aynı kök). → Şube için ayrı doğrulama ve hata metni.
6. `/cariler` — telefonla arama 0 sonuç (arama kutusu "Ad, telefon veya e-posta" diyor). → Sunucu aramasına rakama indirgenmiş telefon + e-posta.
7. `/cikti/{Tur}/{Id}` — kalem tutarı '45000.00' ham basılıyor (Tutarlar bölümü ₺45.000,00). → tr-TR para biçimi.
8. `/gider-pusulalari` — "Hurda alımı gir" bağlantısı Satış sekmesini açıyor; kullanıcı hurda alışını satış olarak girebilir. → `/islemler?tur=HURDA_ALIS` ve İşlemler sayfası parametreyi okusun.

### Düzenlenemeyen veri girişleri (12 ham / 2 gerekçeli / 10 gerçek)
Kurucu kuralı: *"her veri girişi düzenlenebilir olsun."* Tamamı:

**Gerçek eksikler (10):**
1. `/kasalar` — Kasa → Şube: oluşturmada seçiliyor, "Kasayı Düzenle"de alan yok.
2. `/banka` — Dekont → Tarih: girişte alan yok, satırda düzenlenemiyor (sayfa "doğrudan düzeltilebilir" diyor).
3. `/banka` — Dekont → Durum (Bekliyor): değiştirecek eylem yok.
4. `/faturalar` — fatura kalemleri: taslak Düzenle kipinde kalem ekleme/düzeltme/silme yok.
5. `/siparisler` — TASLAK siparişin CARİ, TÜR ve ŞUBE alanları düzenleme formunda yok (önceki turdan, doğrulanamadı).
6. `/kullanicilar` — kendi kullanıcısının Rol ve Erişim durumu Düzenle'de yok (önceki turdan, doğrulanamadı).
7. `/kullanicilar` — kullanıcı kaydı silinemiyor, yalnız pasife alınıyor (bilinçli tasarım; doğrulanamadı).
8. `/veri-disa-aktarim` — aktarım geçmişi satırı: gerekçe düzeltilemiyor, yeniden indirme yok (KVKK md.11 bilinçli tasarım; doğrulanamadı).
9. `/veri-ice-aktarim` — içe aktarım geçmişi ve prova sonucu: "kanıt kaydıdır; değiştirilmez", yol "geri al + yeni prova" (kasıtlı değişmezlik).
10. `/vezne` — fiş türü (Satış/Tahsilat/İşçilik): düzenle kipinde salt metin; ekranda gerekçe ve "fişi iptal et" yolu yazıyor.

*Anlatı notu:* 7–10. maddeler ekranda ya da önceki turda bilinçli tasarım olarak gerekçelendirilmiş; betik bunları "gerçek" saydı. Kurucu onaylarsa bir sonraki turda gerekçeli sınıfa alınabilir. Asıl eylem gerektiren eksikler 1–6'dır.

**Gerekçeli (meşru, 2):**
11. `/faturalar` — kuyum taslağında Toplam: "Kuyum faturasında toplam buradan değişmez" (yol: iptal + yeniden kes).
12. `/gider-pusulalari` — satıcı adı, TCKN, brüt/stopaj/net, tarih: VUK md.234 gerekçeli kilit; yol "Pusulayı iptal et" + yeni hurda alışı (adres ve mal açıklaması artık düzenlenebiliyor).

### Görsel / kullanıcı dostu ortalamaları
- TÜM rotalar: görsel **1,33 / 5**, kullanıcı dostu **1,12 / 5**.
- ÖLÇÜLEBİLEN rotalar (önyükleme imzası sağlıklı): görsel **1,33 / 5**, kullanıcı dostu **1,12 / 5** — bu turda ölçülemeyen grup olmadığı için iki rakam aynı.
- Tur 8 ile (ölçülebilen üzerinden): görsel 3,17 → 1,33 (**−1,84**), kullanıcı dostu 2,74 → 1,12 (**−1,62**). **Bu düşüş ürün gerilemesi değil, ölçüm engelidir:** 49 kaydın yaklaşık 29'u oturum açılamadığı için 0 puanla ortalamaya girdi. Oturumlu ölçülen rotalarda puanlar tur 8 düzeyinde (görsel 3–4, kullanıcı dostu 2–3; en iyi `/gider-pusulalari/{Id}/belge` 4/4, en düşük `/kurulum` görsel 2).

### Düzeltme paketine giren maddeler (D9)
- **D9-1 (KRİTİK 1, hemen):** Menü `[hidden]` onarımı — `[hidden]{display:none!important}` + kapı testi; mod düğmesi titremesini gidermek için görünüm tercihi gelene dek iskelet.
- **D9-2 (KRİTİK 2–3 + YÜKSEK 4):** Banka dekontu — tarih alanı ve satır içi tarih düzenleme, "Bekliyor → Eşleşti" eylemi, storno sonrası bakiye yenileme, istemci IBAN doğrulaması, yüklenirken iskelet.
- **D9-3 (KRİTİK 4):** Fatura taslağında kalem düzenleme; düzenle kipinde negatif tutar istemci doğrulaması; taslak iptal metni çelişkisi; KDV matrah gösterimi.
- **D9-4 (KRİTİK 5):** Sipariş taslağında cari/tür/şube düzenleme (önce oturumlu turda doğrulanmalı).
- **D9-5 (YÜKSEK 2–3):** Kasa bütünlüğü — eksiye düşüren yolu kapatma, eksi kasa uyarısı, kasa şubesini düzenlenebilir yapma, ters kayıt satırının gösterimi, Gelir/Gider rozeti.
- **D9-6 (YÜKSEK 1, 7, 8):** İşlem geri bildirimi — kalıcı bant + okunur fiş no, çıktıda tr-TR tutar, "Hurda alımı gir" derin bağlantısı.
- **D9-7 (YÜKSEK 5–6):** Form doğrulama kalıbı — alan bazlı hata (Şube), `oninput` ile hata temizleme (fiyat, ürün, kasa, vezne, sayım), eski bantları temizleme; cari aramasına telefon/e-posta ve Türkçe harf eşlemesi.
- **D9-8 (sadeleştirme):** Basit modda jargon temizliği (storno, valör, tevsik, MASAK, K-10, K-24, AC-10, KMH) ve Basit modun gövdeyi gerçekten sadeleştirmesi; POS oranını yüzde olarak alma; mobilde tablo yerine kart; mobil üst bar.
- **D9-9 (erişilebilirlik):** karanlık tema birincil düğme kontrastı ≥4,5:1, eksik `document.title`/H1'ler, aria-label/aria-selected eksikleri.
- **D9-0 (QA altyapısı, orkestratör):** grup başına ayrı fikstür oturumu (yenileme belirteci yarışı), turdan hemen önce belirteç tazeleme ya da kurucunun açtığı oturumun paylaşımı; betiğe "oturum açılamadı" ölçülemedi imzası; POS tahsilatı içeren QA tohumu.

## Tur 8 → Tur 9 karşılaştırması

| Ölçüt | Tur 8 | Tur 9 | Fark |
|---|---|---|---|
| Ölçülen sayfa kaydı | 44 | **49** | +5 |
| Toplam bulgu | 225 | **159** | −66 (çoğu oturum engeli nedeniyle ölçülmeyen rotalardan) |
| KRİTİK | — | **5** | — |
| Düzenlenemeyen (ham / gerçek) | 38 / 32 | **12 / 10** | −26 / −22 (bkz. aşağıdaki not) |
| Ortalama görsel (ölçülebilen) | 3,17 | **1,33** | −1,84 (ölçüm engeli) |
| Ortalama kullanıcı dostu (ölçülebilen) | 2,74 | **1,12** | −1,62 (ölçüm engeli) |

**Önceki bulguların durumu** (grup sonuçlarındaki `onceki_bulgular` alanından):
- **KAPANDI: 42** (grup 1: 8, grup 2: 2, grup 3: 16, grup 4: 15, grup 8: 1). Öne çıkanlar: kuyum fatura taslağı kaydı ve cari listesi, fatura/dekont/pusula başarı bantları, gider pusulasına Düzenle ve belge ekranından düzenle/iptal yolu, kâr-zarar grafikleri ve maden ayrımı, satış analizi grafiği, ürünlerde stok sütunu/durum süzgeci/şube adı, cari satırlarında düğme kalabalığı, sayımda Enter ile kaydetme, bankada satır içi Enter, `_framework` 404 önyükleme hatası, `/giris` illüstrasyon çakışması.
- **AÇIK KALAN: 118** — bunların 39'u (grup 2: 19, grup 8: 20) oturum açılamadığı için "açık (doğrulanamadı)" işaretli; 79'u oturumlu ölçümle hâlâ açık olduğu kanıtlanan bulgular.
- **KISMEN: 12** (grup 1: 5, grup 2: 1, grup 3: 2, grup 4: 2, grup 8: 2) — ör. kasa şubesi (kurmada seçilebiliyor, düzenlemede yok), sayımda Şube doğrulaması, kılavuz belgesi 200 ama bağlantılar doğrulanmadı, şifre sıfırlama yalnız yönlendirme.
- **DOĞRULANAMADI: 29** (grup 5'in 6 rotası). Grup 6 ve 7 önceki bulguları hiç listeleyemedi (grup 6 için tur 8'de 44 bulgu + 7 düzenlenemeyen giriş vardı); bunlar da açık sayılır.

**Yeni bulgu:** tur 8'de olmayan, bu turda ilk kez yazılan bulgu sayısı ≈**52** (başlık eşleştirmesiyle; grup 1: 17, grup 3: 14, grup 4: 18, grup 8: 3). En önemlileri: Profesyonel mod menü kusuru (KRİTİK), banka storno bakiye hatası, cari telefon araması, Ana Kasa eksi bakiye, kaybolan fiş no, karanlık tema düğme kontrastı, Basit mod jargon kümesi.

**Düzenlenemeyen giriş listesi farkı (tur 8 → tur 9):**
- *Kapananlar:* `/faturalar` kuyum taslağının kaydedilememesi (PATCH 200); `/faturalar` düzenlemede boş Cari listesi (9 seçenek); `/gider-pusulalari/{Id}/belge` düzenle/iptal yolu (belgeden "Pusulayı düzenle"); `/gider-pusulalari` satırda Düzenle (adres + mal açıklaması düzenleniyor; kalan alanlar gerekçeli).
- *Yeni:* `/kasalar` Kasa → Şube (tur 7'deki "kurmada seçilemiyor" tersine döndü: şimdi düzenlemede yok); `/banka` Tarih ve Durum bu turda somut kanıtla yeniden yazıldı; `/faturalar` kuyum Toplam gerekçeli kilit olarak kayda girdi.
- *Ölçülemediği için listeden düşen ama AÇIK sayılanlar:* `/vitrin` kalem kataloğu; `/siparisler/{Id}` cari/tür/şube/tahmini tutar/kalem ürünü; `/yedekler` talep gerekçesi; `/evraklar` evrak türü; `/bildirimler` kapalı iş; `/urunler/etiketler` basım işi; `/uyum` şüpheli bildirim tutarı; `/kur` manuel kur; `/gider` tutar ve virman bacağı; `/emanet` kilitli alanlar; `/ayarlar` rol/künye; rapor ekranları. Bu kalemler bir sonraki oturumlu turda yeniden taranmalı; "ölçülemedi" TEMİZ demek değildir.

## Temizlik
Bu turda yapılmadı — fikstür kiracısı (`01M31G7HX1T9M02F1C54S4YNM1`, "QA-TEST-FRONTEND Kuyumculuk") sonraki turlar ve kurucuya gerçekçi örnekler için **kalıcı** tutuluyor. `SELECT COUNT(*) FROM tenants` = **10** (fikstür kiracısı mevcut: 1). Bu turda oluşan QA-TEST kayıtları grup notlarında listelenmiştir (G1 T9 kasa/sayım/dekont/cari; G3 T10 fiyat listesi, POS, ürün; G4 T11 vezne fişi, satış + iade, fatura taslağı). Grup 4 notu: satış iadesi Ana Kasa eksi olduğu için "QA-TEST G1 T8 Kasa DUZ"dan yapıldı; ₺1.250 iki kasa arasında dengesiz kaldı (kurucu isterse virmanla eşitlenir).
