# Tarayıcı QA turu — 2026-09-16 (uygulama içi tarayıcı, http://localhost:5199) — TUR 10 (2026-09-24)

> Kurucu talimatı: "ekran testlerini tıklayarak benim tarayıcımdan yap, hiçbir ayrıntıyı atlama". Chrome eklentisi bağlı olmadığı için uygulama içi tarayıcı bölmesi kullanılır; giriş parolasını kurucu kendisi yazar (ekip parola girmez). Yöntem her sayfada: (1) sayfaya git, (2) konsol/ağ hatalarını oku, (3) erişilebilirlik ağacını oku (başlık, boş durum, tablo, düğmeler), (4) ekran görüntüsü `ekran/<rota>.png`, (5) her modal/formu aç, doğrulama mesajlarını tetikle, `QA-TEST-` önekli kayıt oluştur → düzenle → (varsa) pasife al, (6) karanlık tema + dar ekran (375px), (7) basit/profesyonel mod anahtarı. Bulgular önceliklendirilip düzeltme paketine (UX-4 devamı) döner.

**Tur 10 (2026-09-24).** 8 paralel grup, 47 sayfa (47 ayrı rota). İskelet tur 1'in 27 satırlık tablosundan kopyalandı (tur 1 dosyası tarihsel taban, dokunulmadı); tur 1'de olmayan 20 rota 28–47. satırlara eklendi (tur 9 ile aynı sıra).

> **Oturum:** tur 9'daki oturum engeli bu turda **çözüldü** — her grup kendi sekmesinde `http://localhost:5179/__giris` ile sunucu taraflı oturum açtı (grup 1'de bir kez `/giris`'e düştü, 1 denemede geri açıldı). 47 rotanın 47'si oturumlu ölçüldü. Tek istisna: `/giris` rotası, oturum zaten açık olduğu için anında `/`'a yönlendi; **kimliksiz giriş formu bu turda ölçülmedi** (önceki 5 bulgusu ölçülemedi = açık).
>
> **Her sayfada tekrarlayan kabuk hatası (tek kök, tek madde):** CSP `connect-src 'self'` hava durumu widget'ının `api.open-meteo.com` isteğini engelliyor (2 konsol hatası) + `ws://localhost:5179/hubs/kur` SignalR bağlantısı her navigasyonda 3–5 kez `failed`/404 ile düşüp yeniden deniyor (ağ kaydı 89→187 büyüyor). Aşağıdaki "Konsol/ağ" sütununda "kabuk gürültüsü" diye anılır; rotaya özgü 4xx/5xx yoksa satır **OK** sayılır.

| # | Rota | Sayfa | Durum | Konsol/ağ | Bulgular |
|---|---|---|---|---|---|
| 1 | `/giris` | Giriş | BULGU | Kabuk gürültüsü; oturum açık olduğu için `/`'a yönlendi, form render edilmedi. | 1 bulgu (DÜŞÜK, ölçüm notu): kimliksiz form bu oturumda ölçülemedi. Önceki 5 bulgu (göz ikonu/asgari uzunluk, şifremi unuttum, TR/EN `type=submit`, dönüş adresi, "HARİÇ (8)") **ÖLÇÜLEMEDİ** — açık sayılır. Görsel 4 / KD 4. |
| 2 | `/` | Panel | BULGU | Kabuk gürültüsü; tüm API 200. K1–K11 = 0. | 1 bulgu (ORTA): hava durumu widget'ı CSP'ye takılıp sessizce boş kalıyor. Önceki: 7 günlük grafik ezilmesi **KAPANDI** (grafik kaldırılmış); SignalR yoklama döngüsü ve eksi kasa bakiyesinin uyarı renksiz gösterilmesi (−₺728.188,36 normal renk) **AÇIK**; SRI çökmesi ve "Kuru güncelle" geri bildirimi ölçülemedi. Görsel 4 / KD 4. |
| 3 | `/islemler` | İşlemler (POS) | BULGU | Satış POST 200, iade-adayı POST 200; rotaya özgü hata yok. | 7 bulgu (0 KRİTİK, 2 YÜKSEK). Başarı bandı kayboluyor, fiş no hiçbir yerde yok (YÜKSEK); Ana Kasa −₺737.048,80 (YÜKSEK, HATA); stok adedi listede yok, "isteğe bağlı" fiyat zorunluya dönüşüyor, "Düzelt (ters kayıt)" açıklamasız pasif (ORTA); eski uyarılar kalıyor, iade formunda "peşin satış" metni (DÜŞÜK). Karanlık tema düğme kontrastı (~9:1) ve sepet tutar biçimi **KAPANDI**. Görsel 4 / KD 3. |
| 4 | `/cariler` | Cariler | BULGU | Arama istekleri 200 ve doğru sonuç; 4xx/5xx yok. | 2 bulgu (0 KRİTİK). Pasif müşteride Tahsilat/Veresiye/Düzeltme Fişi `⋯` menüsünden hâlâ tetiklenebiliyor (ORTA); mobilde tablo 975 px / 349 px, bakiye görünmüyor (DÜŞÜK). Telefonla arama, Türkçe harf-duyarsız arama, satır düğmesi aria-label **KAPANDI**. Görsel 4 / KD 4. |
| 5 | `/urunler` | Ürünler | BULGU | Liste 11 satır için 11 ayrı `GET urunler/{id}/stok` (N+1), hepsi 200. | 0 yeni bulgu; önceki 8'den 7'si AÇIK (4'ü bu turda yeniden test edilmedi): "Gram (isteğe bağlı)" 0'da reddediliyor, hata mesajı düzeltmeden sonra kalıyor, N+1; açıklama etiketi, kod zorunluluğu, stok penceresi metni, yardım cümlesi tekrarı test edilmedi. Düzenlemede '5.5' nokta ondalık **KAPANDI**. Görsel 4 / KD 3. |
| 6 | `/urunler/agac` | Ürün ağacı | BULGU | Kategori CRUD POST/PUT 200/201. | 1 bulgu (YÜKSEK, GÖRSEL): "REYON TİPİ", "YERLEŞİK ADET" başlıkları 1280 px'te iki satıra kırılıyor (K3=2). Tam CRUD (oluştur/düzenle/pasife al/aktife al) başarılı. Görsel 5 / KD 5. |
| 7 | `/urunler/etiketler` | Etiketler | OK | Kabuk gürültüsü dışında hata yok. | 0 bulgu. Tümünü seç (11), seçimi temizle, arama süzgeci çalışıyor; K1–K11 = 0. Görsel 5 / KD 5. |
| 8 | `/fiyatlandirma` | Fiyat listeleri | BULGU | Tüm çağrılar 200 (kayıt/güncelle/pasife al/aktifleştir). | 5 bulgu (1 YÜKSEK). Alan hata mesajı değer düzeltilince kalkmıyor, yalnız Kaydet'te temizleniyor (YÜKSEK); panel `<form>` değil / Enter yok, `role=dialog`/odak tuzağı/Esc yok (ORTA); 3 başlık dar sütunda kırılıyor (DÜŞÜK); 375 px taşma yok (bilgi). Basit menüde "Fiyat listeleri" bağlantısı **KAPANDI**; risk onayı senaryosu yeniden test edilmedi. Görsel 4 / KD 3. |
| 9 | `/kasalar` | Kasalar | OK | Hata yok; yalnız Düzenle penceresi açılıp iptal edildi. | 0 yeni bulgu. Eksi bakiye kırmızı kutu + "Eksi bakiye" rozeti **KAPANDI**; kasa şubesi/dövizi artık gerekçeli kilit + "Kasalar arası para aktar" yolu (**KISMEN** — işlemsiz kasada açık mı test edilmedi); "Storno" → "Geri al (düzeltme fişi)" (KISMEN); geçmişte Gelir/Gider yazmıyor (AÇIK); 5 madde ölçülemedi. Görsel 4 / KD 4. |
| 10 | `/banka` | Banka | BULGU | Geçersiz IBAN'da POST reddi; storno POST 200, bakiye anlık güncellendi. | 2 bulgu (0 KRİTİK). Geçersiz IBAN'da sunucu 400 sonrası bile alan `aria-invalid=false` kalıyor (ORTA); devre dışı "Kullanımdan kaldır" ipucu hâlâ "kasa" diyor (DÜŞÜK). **Tur 9'un 2 KRİTİK'i KAPANDI:** dekont tarihi girişte ve satırda düzenlenebiliyor; "Bekliyor" satırında "Müşteriyle eşleştir" var; storno sonrası bakiye güncelleniyor. Görsel 4 / KD 4. |
| 11 | `/kur` | Kur | OK | `POST kur/manuel` 201; 4xx/5xx yok. | 0 bulgu. Test için girilen manuel kur (₺6.750,55) sonradan "Şimdi çek" ile canlı kaynağa (₺6.704,77) geri döndürüldü (ajan açıkça raporladı). Görsel 4 / KD 4. |
| 12 | `/emanet` | Emanet | BULGU | Giriş POST 200, geri alma 200. | 2 bulgu (0 KRİTİK). Ayrıntıda ham gram/ayar geri okunamıyor, yalnız has gram (ORTA); BASİT modda "Cari" jargon sayacı müşteri adından tetiklendi — muhtemel yanlış pozitif (DÜŞÜK). Storno satırı "Geri alma kaydı" etiketi, devre dışı düğme sebebi, boş durum rehberliği **KAPANDI** (3). Görsel 4 / KD 4. |
| 13 | `/sayim` | Sayım | BULGU | Şube boşken POST istemcide durduruldu; PATCH 200. | 2 bulgu (0 KRİTİK). Satır içi "Sayılan" hata bandı sayfa tepesinde (top=22 px), giriş kapanıp değer kayboluyor (ORTA; metin iyileşti); mobilde tablo 513 px / 349 px (DÜŞÜK). Şube zorunluluk hatası artık doğru alanda (**KAPANDI** ×2). Açık: kilit kutusu "ad değiştirilemez" ama ad düzenlenebiliyor, şube seçicide açık sayım işareti yok, DURUM sütununda "İptal Et", `→` aria adı yok, `inputmode` yok. Görsel 4 / KD 3. |
| 14 | `/faturalar` | Faturalar | BULGU | Taslak POST 200, İptal 200. | 6 bulgu (0 KRİTİK). Basit modda KDV 23/e, K-10, `Kdv:OzelMatrahKodu` metni (ORTA); iptalde "Numara kesimde verilir", başlıklar 1280 px'te kırılıyor (K3=4), bant "İşlem başarılı —" boş, iptal bandında ULID, vazgeç düğmesinin adı "İptal" (DÜŞÜK). **Tur 9 KRİTİK'i KAPANDI:** kalemli taslakta satır ekle/sil/düzelt var; satırsız taslakta gerekçeli kilit. Görsel 4 / KD 3. |
| 15 | `/gider-pusulalari` | Gider pusulaları | BULGU | GET 200; ters tarih de 200 (uyarısız). | 2 bulgu (0 KRİTİK). Ters tarih aralığı uyarısız (DÜŞÜK); tablo başlıkları 1280 px'te kırılıyor (DÜŞÜK). "Hurda alımı gir" artık Hurda Alış sekmesini açıyor (**KAPANDI**). Görsel 4 / KD 4. |
| 16 | `/gider-pusulalari/{Id}/belge` | Gider pusulası belgesi | BULGU | Belge render, 4xx/5xx yok. Pano gizli, ekran görüntüsü alınamadı. | 0 yeni bulgu; önceki 6'nın 6'sı AÇIK (2'si yeniden test edilmedi): dar ekranda düğme yazısı alt kenara yapışık (bottom 381=381), Geri `<button>`, PDF yok, "Dayanak" boş satırı, eksik alanda Yazdır, iptal yönlendirmesi. Görsel 3 / KD 3. |
| 17 | `/uyum` | Uyum (tevsik/MASAK) | BULGU | Kabuk gürültüsü dışında hata yok. | 2 bulgu (0 KRİTİK). "İşlem tutarı (isteğe bağlı)" boşken Kaydet disabled (ORTA); "süresi geçmiş" / "süresi dolmuş" terim karışıklığı (DÜŞÜK). 2 önceki bulgu doğrulanamadı. Görsel 4 / KD 3. |
| 18 | `/raporlar` | Raporlar | OK | Tüm API 200. | 0 bulgu. Görsel 4 / KD 4. |
| 19 | `/raporlar/kar-zarar` | Kâr/zarar | BULGU | Rapor API 200. Pano gizli, ekran görüntüsü alınamadı. | 0 yeni bulgu; önceki 6 AÇIK (2'si yeniden test edilmedi): "Önceki dönem" karşılaştırması değer göstermiyor, Basit mod jargonu (HASILAT, DÖNEM KÂRI…), "GIDER" noktasız I, grafik parçalarında aria yok. Grafik: çubuk 8 parça (en geniş 52,5 / 418,5 px, %12,5 — dar), pasta 3 parça (%81,8). Görsel 4 / KD 3. |
| 20 | `/raporlar/vade-takibi` | Vade takibi | OK | `raporlar/vade-takibi` 200. | 0 bulgu (salt rapor, gerekçeli değişmezlik). Görsel 5 / KD 4. |
| 21 | `/denetim-izi` | Denetim izi | BULGU | `denetim/ozet`, `denetim` 200. | 1 bulgu (ORTA, GÖRSEL): Başlangıç/Bitiş tarih kutuları 15 px hizasız (K7=1). Not: "Denetim İzi" başlığı K8 regex'ine (büyük İ) takılmıyor — ölçüm aracı sınırı. Görsel 4 / KD 4. |
| 22 | `/veri-disa-aktarim` | Veri dışa aktarım | BULGU | `disa-aktarim/gecmis` 200. "Hazırla ve İndir" bilinçli olarak tıklanmadı (indirme onayı). | 1 bulgu (YÜKSEK): Kapsam kutularında 6 ham kod adı (`pos-cihazlari`, `pos-tahsilatlari`, `pos-eslesmeleri`, `satinalma`, `irsaliye`, `muhasebe-fisi`). Geçmiş satırı değişmezliği KVKK md.11 bilinçli tasarım olarak **KAPANDI**. Görsel 3 / KD 3. |
| 23 | `/kullanicilar` | Kullanıcılar | BULGU | `POST kullanicilar` 201, güncellemeler 200. | 1 bulgu (YÜKSEK, GÖRSEL): "İKİ AŞAMALI" başlığı 1280 px'te iki satıra kırılıyor (K3=1). Kendi rolünü değiştirememe ve silme yerine pasife alma gerekçeli bilinçli tasarım olarak **KAPANDI** (2). Tam CRUD + Erişimi Kapat/Aktifleştir başarılı. Görsel 5 / KD 5. |
| 24 | `/yetkiler` | Yetkiler | OK | Rotaya özgü hata yok. | 0 bulgu. Görsel 5 / KD 5. |
| 25 | `/isletme` | İşletme | BULGU | API 200. | 4 bulgu (**1 KRİTİK**). **İşletme kimliği modalı yalnız "İşletme adı" alanını içeriyor; adres, telefon, vergi dairesi, vergi no/TCKN hiçbir yerden düzenlenemiyor** (**KRİTİK**, DÜZENLENEBİLİRLİK); başlık/açıklama iki kez (ORTA); şube telefonu ham, tablo düğmeleri `type=submit` (DÜŞÜK). Görsel 3 / KD 2. |
| 26 | `/ayarlar` | Ayarlar (tema, mod) | OK | Hata yok; TR/EN geçişi sorunsuz. | 0 yeni bulgu. WASM önyükleme hatası ve EN'de "Role: SAHIP" **KAPANDI** ("Role Owner"). Rol ve sistem künyesi gerekçeli salt okunur. Görsel 4 / KD 4. |
| 27 | `/kilavuz` | Kılavuz | BULGU | Rotaya özgü hata yok. | 1 bulgu (YÜKSEK): BASİT modda ekran listesinde eski "Cariler" terimi (menüde "Müşteriler") — K8=1. Görsel 4 / KD 4. |
| 28 | `/cikti/{Tur}/{Id}` | Belge çıktısı | BULGU | `cikti`, `auth/ben`, `subeler`, `kur/guncel` 200; kabuk gürültüsü. | 2 bulgu (0 KRİTİK). Kabuk SignalR/CSP hatası (ORTA, PERFORMANS — tüm rotalar için tek madde); K-sayaçları temiz (DÜŞÜK, bilgi). **Tur 9 KRİTİK'i KAPANDI:** Profesyonel modda `[hidden]` görünen menü satırı 38 → 0. '45000.00' ve has gram işareti/ondalık **KAPANDI**; başlık sırası ve mobil üst bar (211 px) KISMEN; 26 karakterlik belge no AÇIK; mod düğmesi titremesi ölçülemedi. Görsel 4 / KD 4. |
| 29 | `/pos/tahsilatlar` | POS hakedişleri | BULGU | `pos/valor-takvimi`, `pos/tahsilatlar` 200 (ters aralık dahil). | 2 bulgu (0 KRİTİK). Ters tarih aralığı uyarısız (ORTA, HATA); terim dördüncü varyanta çıktı: "POS alacakları" / "POS Hakedişleri" / "POS alacak defteri" / "POS tahsilatı" (DÜŞÜK). Boş durum iki kez, eşleştir düğmeleri kilitli, "iptal ucu" jargonu AÇIK; veri olmadığı için 2 madde ölçülemedi. Görsel 4 / KD 3. |
| 30 | `/pos` | POS cihazları | BULGU | CRUD 200; `POST pos/cihazlar` 201 yerine 200. | 2 bulgu (0 KRİTİK). 4 başlık dar sütunda kırılıyor (DÜŞÜK); 375 px taşma yok (bilgi). Önceki: komisyon hesabı doğru ama ham ondalık girişi (0,025) riski **KISMEN**; taksit oranları yeni cihazda yok, CİHAZ SAYISI pasifleri sayıyor, "Taksit Oranları" iki kez, 200/201, Basit modda "valör / MEVZUAT DEĞİL" AÇIK. Görsel 4 / KD 3. |
| 31 | `/veri-ice-aktarim` | Veri içe aktarım | BULGU | Yüklemede 4xx/5xx yok. | 0 yeni bulgu; önceki 10'dan 8'i AÇIK (4'ü yeniden test edilmedi — gerçek aktarım riskli): başlık/açıklama iki kez, "Ne aktarılacak" üç kez, "43/20 karakter" sayacı, GEÇERLİ sütunu oran, `document.title` sayfa adı yok; onay metni konumu KISMEN; kasıtlı değişmezlik KAPANDI. Görsel 3 / KD 2 (turun en düşük KD'lerinden). |
| 32 | `/raporlar/satis-analizi` | Satış analizi | BULGU | Bilinçli ters tarih 400 ×2; yüklemede hata yok. | 3 bulgu (0 KRİTİK). Açıklama Rapor Merkezi metni (ORTA); ters aralıkta tablo eski veriyle kalıyor, dışa aktarma/yazdırma yok (DÜŞÜK). `aria-selected` **KAPANDI**. Grafik 6 parça, en geniş 70 / 899 px (%7,8 — dar). Görsel 4 / KD 3. |
| 33 | `/vezne` | Vezne | BULGU | Yeni fiş POST 200; bilinçli PATCH 400 (−5 adet). | 7 bulgu (0 KRİTİK). Yeni fiş süzgeç dışında kaybolmuş görünüyor, negatif fiyatta "En az bir kalem gerekli", Basit modda "tevsik, MASAK, gün kilidi" (ORTA); eski mesaj kalıyor, ULID, istemci doğrulaması yok, zorunlu alan "(isteğe bağlı)" (DÜŞÜK). Düzenlemede '1000.00' nokta ondalık **KAPANDI**. Görsel 4 / KD 3. |
| 34 | `/kurulum` | Kuruluş sihirbazı | BULGU | Yüklemede hata yok; prova AC-10 döndü. | 7 bulgu (0 KRİTİK). Prova hatalıyken "Eksik: Kuru prova çalıştır", Eksik adımlara bağlantı yok, K-24/KMH/AC-10/storno jargonu (ORTA); `document.title` sayfa-özel değil, "Kuruluş Sihirbazı" iki h2, bitişik sayaç, ham liste görünümü (DÜŞÜK). H1 ve negatif tutar istemci doğrulaması **KAPANDI** (KISMEN ×3). Görsel 3 / KD 3. |
| 35 | `/evraklar/takvim` | Vade takvimi | OK | `evraklar/takvim` 200. | 0 bulgu. Görsel 5 / KD 5. |
| 36 | `/siparisler/{Id}` | Sipariş detayı | OK | `siparisler/{id}` 200. | 0 bulgu. Test kaydı İptal durumda — "Bu kayıt değiştirilemez" bandı iş kuralı gereği doğru. Görsel 5 / KD 4. |
| 37 | `/raporlar/islem-defteri` | Hareket dökümü | OK | 200. | 0 bulgu (salt rapor). Görsel 5 / KD 4. |
| 38 | `/vitrin` | Vitrin panosu | BULGU | Hata yok. | 1 bulgu (DÜŞÜK): Sıra alanına 'abc' sessizce 0 oluyor, uyarı yok. Düzenle → kaydet → "Varsayılana dön" onayı uçtan uca çalışıyor, test verisi geri alındı. Görsel 5 / KD 4. |
| 39 | `/yedekler` | Yedekler | OK | Tümü 200; 1 SignalR `ERR_ABORTED` (yeniden bağlanma artefaktı). | 0 bulgu. Açık ve karanlık temada ekran görüntüsüyle okunaklı. Görsel 4 / KD 4. |
| 40 | `/gider` | Gider | BULGU | `POST giderler` 200, storno 200. | 2 bulgu (**1 KRİTİK**). **Storno sonrası "Toplam gider" net değil mutlak toplam: ₺125,50 gider + −₺125,50 storno = ₺251,00 gösteriliyor, doğrusu ₺0,00** (**KRİTİK**, HATA; kasa bakiyesi doğru döndü); BASİT modda "STORNO:" öneki — K8 regex'i büyük harfi yakalamıyor (YÜKSEK). Görsel 4 / KD 3. |
| 41 | `/raporlar/kasa` | Kasa raporu | OK | 200. | 0 bulgu. Görsel 4 / KD 3. |
| 42 | `/evraklar` | Evraklar | BULGU | PATCH 200 ×2 (değiştir + geri al). | 1 bulgu (ORTA, GÖRSEL): "EVRAK TÜRÜ", "EVRAKI VEREN MÜŞTERİ", "VADE GÜNÜ" başlıkları 1280 px'te kırılıyor (K3=3). Görsel 4 / KD 4. |
| 43 | `/ekranlar` | Tüm ekranlar | OK | Kabuk gürültüsü dışında hata yok. | 0 bulgu. Görsel 4 / KD 4. |
| 44 | `/hurda` | Hurda | BULGU | `POST islemler/hurda-satis` 200. | 4 bulgu (**1 KRİTİK**, 1 YÜKSEK). **Ramat çıkışı (stornosu olmayan hurda satışı) onay penceresi olmadan tek tıkla kaydediliyor** (**KRİTİK**); bakiye varken de "Bu madende hurda bakiyesi yok" (YÜKSEK, HATA); "Tahsilat kasası" zorunlu işareti yok/ön seçim yok, hata canlı temizlenmiyor (ORTA). Önceki 4 AÇIK, 2 doğrulanamadı. Görsel 4 / KD 3. |
| 45 | `/banka/ekstre/{IthalatId}` | Banka ekstresi | BULGU | Atla/geri-al 200. | 5 bulgu (0 KRİTİK, 2 YÜKSEK). Karşı taraf adı ile IBAN etiketi üst üste biniyor (K9=2, YÜKSEK); atlanan satır "Eşleşti" rozeti taşıyor (YÜKSEK, HATA); 7 kısa metin/başlık kırılıyor (ORTA); "İşlem başarılı" bandı eylemsiz, süzgeçte emir kipi "Atla" (DÜŞÜK). Önceki 4 AÇIK (yeni satırda bile tarih/tutar/açıklama düzenlenemiyor), 1 doğrulanamadı. Görsel 3 / KD 3. |
| 46 | `/siparisler` | Siparişler | BULGU | Oluştur/düzenle 200; iş kuralı hatası 200 gövdede. | 3 bulgu (0 KRİTİK). Aynı iş kuralı hatası iki DOM düğümünde tekrar (DÜŞÜK); kalem textbox'larında erişilebilir ad yok (DÜŞÜK); TASLAK siparişte tür/cari/şube düzenleme (DÜŞÜK, olumlu kayıt). **Tur 9 KRİTİK'i KAPANDI:** TASLAK'ta tür değiştirilip kaydedildi; oluşturma başarı bandı "Sipariş oluşturuldu: S-2026-000003" **KAPANDI**. Görsel 4 / KD 4. |
| 47 | `/bildirimler` | Bildirimler | BULGU | Kabuk WS hataları + 1 adet 400; ağ kaydı 89 → 187 (yeniden bağlanma döngüsü). | 2 bulgu (0 KRİTİK). SignalR sürekli yeniden bağlanıyor (ORTA, PERFORMANS); Yenile/sekme değişimi `aria-live` ile duyurulmuyor (DÜŞÜK). Sekme açıklaması **KAPANDI**; Yenile geri bildirimi KISMEN; sekmelerde `aria-selected` yok AÇIK. Görsel 4 / KD 3. |

## Çapraz kontroller
- [x] **Karanlık tema okunaklı** — B-32 gerilemesi yok. Karanlık temada birincil düğme kontrastı 2,95:1 → **~9:1** (`/islemler`, `/kurulum`) KAPANDI. `/yedekler`, `/ekranlar`, `/hurda`, `/emanet`, `/` ekran görüntüsüyle; pano gizli kalan rotalarda (`/ayarlar`, `/uyum`, grup 3'ün 2 rotası, grup 6'nın 5 rotası) DOM/WCAG kontrast hesabıyla (<4,5:1 bulunmadı) doğrulandı.
- [x] **Dar ekran 375px: sayfa yatay kaymıyor** — 47/47 rotada `document.scrollWidth=375`. **Ama** geniş tablolar kendi kutusunda kayıyor, önemli sütunlar ekran dışında: `/cariler` 975 px (bakiye), `/sayim` 513 px (Durum, `→`). `/cikti` mobil üst bar 230 → 211 px, "Şube kapsamı" artık bir kez.
- [x] **Basit / profesyonel mod — menü onarıldı.** Profesyonel modda `[hidden]` görünen satır 38 → **0** (BASİT ve PRO'da, 1280 ve 375 px). **Kalan:** Basit mod gövdeyi hâlâ sadeleştirmiyor — jargon `/vezne` (tevsik, MASAK), `/faturalar` (K-10, `Kdv:OzelMatrahKodu`), `/kurulum` (K-24, KMH, AC-10), `/pos` (valör, MEVZUAT DEĞİL), `/raporlar/kar-zarar` (HASILAT, DÖNEM KÂRI), `/gider` ("STORNO:"), `/kilavuz` ("Cariler"), `/veri-disa-aktarim` (kebab-case kod adları). K8 regex'i büyük harf duyarlı (STORNO, Denetim İzi kaçıyor) — ölçüm aracı düzeltilmeli. Mod düğmesi açılış titremesi ölçülmedi.
- [x] **Oturum** — `__giris` sunucu taraflı oturum her grupta çalıştı (tur 9 engeli çözüldü). 401 → `/giris` yönlendirmesi bu turda yeniden ölçülmedi (oturum düşmedi; grup 1'de bir düşme 1 denemede geri açıldı).
- [ ] **Her kaydet akışında başarı bandı — KISMEN.** Artık bant veren: sipariş oluşturma, banka storno, kullanıcı CRUD, vitrin, ürün ağacı. Eksikler: `/islemler` bandı kayboluyor ve fiş no yok; `/faturalar` "İşlem başarılı —" boş; `/banka/ekstre` "İşlem başarılı" eylemsiz; `/vezne`, `/faturalar` bantlarında ham ULID; eski doğrulama mesajları `/fiyatlandirma`, `/urunler`, `/vezne`, `/islemler`, `/hurda`'da düzeltmeden sonra kalıyor (ortak kök: `oninput` yeniden doğrulama yok).
- [ ] **Kabuk konsolu temiz — BAŞARISIZ (yeni çapraz kontrol).** 47/47 rotada open-meteo CSP ihlali + `/hubs/kur` WebSocket 404/failed döngüsü.

## Bulgu özeti — TUR 10 (2026-09-24)

### Sayılar (betik ölçümü — ajan saymadı)
| Ölçüt | Değer |
|---|---|
| Ölçülen sayfa | **47** (47 ayrı rota; 47'si ölçülebilir) |
| Toplam bulgu | **82** |
| KRİTİK | **3** |
| YÜKSEK | 11 (anlatı sayımı) |
| Düzenlenemeyen veri girişi (ham / gerekçeli / gerçek) | **20 / 3 / 17** |
| Ortalama görsel puan — TÜM rotalar | **4,06 / 5** |
| Ortalama kullanıcı dostu puan — TÜM rotalar | **3,62 / 5** |
| Ortalama görsel puan — ÖLÇÜLEBİLEN rotalar | **4,06 / 5** (47 ölçülebilen sayfa) |
| Ortalama kullanıcı dostu puan — ÖLÇÜLEBİLEN rotalar | **3,62 / 5** |
| Grafik ölçülen sayfa / SVG sayısı | 38 / 11 |
| Çizilmeyen grafik / dar çizilen | 0 / 2 (`/raporlar/kar-zarar` çubuk %12,5; `/raporlar/satis-analizi` %7,8) |
| Ölçülemeyen grup (önyükleme imzası) | yok (0 rota) |

**İki ortalamanın anlamı:** "TÜM rotalar" 47 sayfanın hepsinin ortalamasıdır; "ÖLÇÜLEBİLEN" yalnız önyükleme imzası sağlıklı (uygulamanın açıldığı) rotaların ortalamasıdır. Bu turda ölçülemeyen grup olmadığı için iki rakam eşit. Tur 9'dan farklı olarak oturum engeli de yok; bu rakamlar ürünün gerçek puanıdır.

**Kapsam notu:** grafik bbox 38 sayfada ölçüldü; grup 1'in 4 rotası (`/sayim`, `/kasalar`, `/banka`, `/cariler`) bütçe nedeniyle ölçülmedi (görsel olarak veri grafiği gözlenmedi, ölçülmedi olarak kayıtlı). Birçok önceki bulgu "yeniden test edilmedi / ölçülemedi" durumunda — bunlar kapandı sayılmaz (Anayasa md.1).

### KRİTİK bulgular (3) — rota · kanıt · öneri
1. **`/gider` — storno sonrası "Toplam gider" mutlak değer topluyor.** Kanıt: QA-TEST kasasına ₺125,50 gider girildi, "Geri al (düzeltme fişi)" ile stornolandı; kasa bakiyesi doğru biçimde ₺5.508,51'e döndü, ama "Toplam gider: ₺251,00" (125,50 + 125,50), doğrusu ₺0,00. Rapor rakamı yanlış — muhasebe güvenini doğrudan bozar. → Toplamı işaretli tutarlarla hesaplayın (storno satırı negatif); aynı toplama kalıbını kullanan diğer özetleri (kasa, rapor) tarayın; birim testi ekleyin.
2. **`/hurda` — ramat çıkışı (hurda satışı) onaysız, tek tıkla ve geri alınamaz.** Kanıt: 0,500 g Altın + Ana Kasa seçilip "Ramat çıkışını kaydet" → `POST /api/v1/islemler/hurda-satis` 200 anında, diyalog yok; sayfa metni "yerinde DEĞİŞMEZ ve bu sürümde stornolanmaz". → Kaydetmeden önce özet onay penceresi (maden, gram, tutar, kasa); orta vadede storno yolu.
3. **`/isletme` — işletme kimliğinde yalnız ad düzenlenebiliyor.** Kanıt: "İşletme kimliğini düzenle" diyaloğunda tek input `ikimlik-ad`; kartta görünen Adres, Telefon, Vergi dairesi, Vergi no/TCKN için hiçbir düzenleme kontrolü yok. Kurucunun "her veri girişi düzenlenebilir" kuralına doğrudan aykırı; fatura/pusula belgelerine basılan kimlik bilgisi düzeltilemiyor. → Modala bu dört alanı ekleyin; VKN/TCKN sağlaması (önceki açık bulgu) ile birlikte.

### YÜKSEK bulgular — rota · kanıt · öneri
1. `/islemler` — satış başarı bandı kayboluyor, fiş no hiçbir yerde kalmıyor (Son satışlar'da fiş no sütunu yok). → Kalıcı bant + okunur fiş no + "Fişi yazdır"; tabloya fiş no sütunu.
2. `/islemler` — Ana Kasa −₺737.048,80 ("nakit kasa eksiye düşemez" kuralına rağmen; panelde de uyarı rengi yok). → Eksiye düşüren hareket yolunu bulup aynı kapıyı uygulayın; mevcut bakiyeyi düzeltme fişiyle giderin.
3. `/hurda` — bakiye yeterliyken bile "Bu madende hurda bakiyesi yok"; alan düzeltilince mesaj kalkmıyor. → "Kalan hurdayı aşıyorsunuz (2,664 g mevcut)" + `oninput` yeniden doğrulama.
4. `/banka/ekstre/{IthalatId}` — karşı taraf adı ile "TR12 **** 0001" etiketi üst üste biniyor (K9=2). → Ayrı satır ya da yeterli boşluk.
5. `/banka/ekstre/{IthalatId}` — atlanan satır "Eşleşti" rozeti taşıyor. → Onaylandı / Atlandı / Yeni ayrı rozetler.
6. `/fiyatlandirma` — alan hata mesajı değer düzeltilince kalkmıyor, yalnız Kaydet'te. → `oninput`/`onchange` ile alan hatasını temizleyin (ortak form bileşeninde çözülürse `/urunler`, `/vezne`, `/islemler`, `/hurda` da kapanır).
7. `/gider` — BASİT modda "STORNO:" öneki; K8 sayacı büyük harf yüzünden kaçırıyor. → "Düzeltme:" gibi sade önek; jargon regex'ine `i` bayrağı.
8. `/veri-disa-aktarim` — 6 kapsam kutusu ham kod adıyla (`pos-cihazlari`, `satinalma`, `muhasebe-fisi`…). → Türkçe etiketler ("POS Cihazları", "Satınalma", "Muhasebe Fişi"…).
9. `/kilavuz` — BASİT modda "Cariler" (menüde "Müşteriler"). → Kılavuz metnini güncel menü adlarıyla eşleyin.
10. `/kullanicilar` — "İKİ AŞAMALI" başlığı 1280 px'te kırılıyor (K3=1). → `white-space:nowrap` ya da "2 Adımlı".
11. `/urunler/agac` — "REYON TİPİ", "YERLEŞİK ADET" başlıkları kırılıyor (K3=2). → Aynı çözüm; tablo başlıklarına genel `nowrap` kuralı `/fiyatlandirma`, `/pos`, `/faturalar`, `/gider-pusulalari`, `/evraklar`, `/banka/ekstre` kırılmalarını da kapatır.

### Düzenlenemeyen veri girişleri (20 ham / 3 gerekçeli / 17 gerçek)
Kurucu kuralı: *"her veri girişi düzenlenebilir olsun."* Betik gerekçeli sınıfını "tasarım gereği / bilinçli / salt okunur olmalı / kritik sayılmadı" ifadelerine göre ayırdı. Tamamı:

**Gerçek (betik sınıfı, 17):**
1. `/isletme` — İşletme adresi: "İşletme kimliğini düzenle" modalında alan yok.
2. `/isletme` — İşletme telefonu: aynı modalda alan yok.
3. `/isletme` — Vergi dairesi: aynı modalda alan yok.
4. `/isletme` — Vergi no / TCKN: aynı modalda alan yok.
5. `/gider` — Gider kaydı (kasa/kategori/tutar/açıklama): satır yerinde düzenlenemiyor; tek yol "Geri al (düzeltme fişi)" + ≥20 karakter gerekçe + yeni kayıt (ajan KRİTİK işaretledi).
6. `/hurda` — Hurda alış satırı: Eylemler hücresi tamamen boş; düzenleme, storno, düzeltme yok.
7. `/banka/ekstre/{IthalatId}` — Ekstre sahne satırı: "Yeni" durumdayken bile tarih, tutar, yön, açıklama, karşı taraf ünvanı/IBAN düzenlenemiyor (yalnız cari bağla / atla / onayla).
8. `/banka/ekstre/{IthalatId}` — Onaylanmış ekstre satırı: "Bu kayıt değiştirilemez" gerekçeli kilit; düzeltme yolu banka hareketinin stornosu.
9. `/kasalar` — Kasa → Şube ve Döviz (işlem geçmişi olan kasada): gerekçeli kilit + "Kasalar arası para aktar" yolu; işlemsiz yeni kasada açık olup olmadığı doğrulanmadı.
10. `/vezne` — Fiş türü (Satış/Tahsilat/İşçilik): düzenle kipinde salt metin; gerekçe + "iptal edip yenisini aç" yolu.
11. `/faturalar` — Kuyum taslağında Toplam (satırsız taslak): "Kuyum faturasında toplam buradan değişmez"; yol iptal + yeniden kes.
12. `/gider-pusulalari` — Satıcı adı, TCKN, brüt/stopaj/net tutar, teslim tarihi, son düzenleme tarihi: VUK md.234 gerekçeli kilit; yol "Pusulayı iptal et" + yeni hurda alışı (adres ve mal açıklaması düzenlenebiliyor).
13. `/uyum` — Şüpheli işlem bildiriminin tutarı ve gerekçesi: MASAK yükümlülük kaydı, gerekçeli; yol gerekçeli KAPAT + yeni kayıt.
14. `/raporlar/satis-analizi` — Rapor rakamları: "işlem kayıtlarından türetilir; rapor ekranında düzeltilmez" (salt rapor için beklenen).
15. `/emanet` — Storno satırı ve stornolanmış satır: ikinci kez stornolanamıyor (buton devre dışı, gerekçe ekranda).
16. `/ayarlar` — Profil › Rol (kendi rolü): "Rolünüzü başka bir SAHİP değiştirebilir" gerekçesi ekranda.
17. `/ayarlar` — Sistem künyesi (sürüm, derleme, şema adımı): salt okunur.

**Gerekçeli (betik sınıfı, 3):**
18. `/veri-disa-aktarim` — Aktarım geçmişi satırları (gerekçe/kapsam): KVKK md.11 bilinçli tasarım; yeni talep açılır.
19. `/hurda` — Hurda satış satırı: yerinde düzenlenemiyor/stornolanamıyor; yalnız "Tahlil farkını düzelt" (ekranda gerekçeli, bilinçli tasarım).
20. `/emanet` — Gram, ayar, maden ve cari: tasarım gereği kilitli, "düzeltme yolu stornodur".

*Anlatı notu (öncelik):* betiğin "gerçek" saydığı 17 kalemin 8–17. maddeleri ekranda gerekçe ve düzeltme yoluyla sunulan meşru kilitlerdir (mevzuat, kanıt kaydı, salt rapor, güvenlik); kurucu onaylarsa gerekçeli sınıfa alınabilir. **Eylem gerektiren gerçek eksikler 1–7'dir:** işletme kimliğinin 4 alanı (KRİTİK), gider kaydının yerinde düzenlenememesi, hurda alış satırında hiç eylem olmaması ve ekstre sahne satırının onaydan önce bile düzeltilememesi.

### Görsel / kullanıcı dostu ortalamaları
- TÜM rotalar: görsel **4,06 / 5**, kullanıcı dostu **3,62 / 5**.
- ÖLÇÜLEBİLEN rotalar (önyükleme imzası sağlıklı, 47/47): görsel **4,06 / 5**, kullanıcı dostu **3,62 / 5** — ölçülemeyen grup olmadığı için iki rakam aynı.
- Tur 9 ile (ölçülebilen üzerinden): görsel 1,33 → 4,06 (**+2,73**), kullanıcı dostu 1,12 → 3,62 (**+2,50**). Farkın büyük kısmı tur 9'daki oturum engelinin (≈29 kayıt 0 puan) kalkmasıdır; gerçek ürün eğilimi için tur 8 daha adil taban: görsel 3,17 → 4,06 (**+0,89**), kullanıcı dostu 2,74 → 3,62 (**+0,88**).
- En yüksek (5/5): `/urunler/agac`, `/urunler/etiketler`, `/kullanicilar`, `/yetkiler`, `/evraklar/takvim`. En düşük KD: `/isletme` (3/2), `/veri-ice-aktarim` (3/2); görsel 3: `/gider-pusulalari/{Id}/belge`, `/veri-disa-aktarim`, `/kurulum`, `/banka/ekstre`, `/isletme`, `/veri-ice-aktarim`.

### Düzeltme paketine giren maddeler (D10)
- **D10-1 (KRİTİK 1, hemen — veri doğruluğu):** `/gider` toplamını işaretli tutarla hesapla; storno içeren tüm özet toplamlarını tara; birim + canlı mutasyon kanıtı.
- **D10-2 (KRİTİK 2):** `/hurda` ramat çıkışına özet onay penceresi; "hurda bakiyesi yok" mesajını kalan gramla düzelt; kasa zorunlu işareti + ön seçim; `oninput` doğrulama. Hurda alış satırına düzeltme/storno yolu (düzenlenemeyen 6).
- **D10-3 (KRİTİK 3):** `/isletme` kimlik modalına adres, telefon, vergi dairesi, VKN/TCKN (+ sağlama); başlık tekrarı, telefon biçimi, `type=button`.
- **D10-4 (düzenlenebilirlik):** `/gider` kaydında yerinde düzenleme (ya da gerekçeli kilit + tek tık "düzelt ve yeniden gir" akışı); `/banka/ekstre` sahne satırını onaydan önce düzenlenebilir yap, "Atlandı" rozeti, eyleme özel bant, süzgeçte "Atlandı", ad/IBAN çakışması.
- **D10-5 (ortak form kalıbı):** alan hatasını `oninput` ile temizleme (`/fiyatlandirma`, `/urunler`, `/vezne`, `/islemler`, `/hurda`), sunucu 400 alan adını alana bağlama (`/banka` IBAN), "(isteğe bağlı)" etiketlerinin gerçek zorunlulukla eşlenmesi (`/vezne`, `/islemler`, `/urunler`, `/uyum`, `/kurulum`), sayısal alanlarda `inputmode` ve sessiz 0'a düşmeme (`/vitrin`, `/sayim`), ters tarih aralığı kontrolü (`/pos/tahsilatlar`, `/gider-pusulalari`, `/raporlar/satis-analizi`).
- **D10-6 (işlem geri bildirimi):** `/islemler` kalıcı bant + fiş no; ULID yerine okunur numara (`/vezne`, `/faturalar`, `/cikti`); boş/eylemsiz bantlar (`/faturalar`, `/banka/ekstre`); Ana Kasa eksi bakiye kök nedeni ve panelde uyarı rengi.
- **D10-7 (sadeleştirme / jargon):** Basit modda gövde jargonunu temizle (vezne, faturalar, kurulum, pos, kâr-zarar, gider "STORNO:", pos/tahsilatlar "iptal ucu"); `/veri-disa-aktarim` kod adları; `/kilavuz` "Cariler"; POS terminolojisini tek ada indir; K8 regex'ine `i` bayrağı.
- **D10-8 (görsel):** tablo başlıklarına genel `nowrap`/genişlik kuralı (9 rotada K3 kırılması); `/denetim-izi` tarih kutusu hizası; mobilde geniş tablolar yerine kart (`/cariler`, `/sayim`); dar çizilen grafikler (kâr-zarar, satış analizi).
- **D10-9 (kabuk):** `/hubs/kur` SignalR 404 kök nedeni (hub rotası/negotiate) ve yeniden bağlanma döngüsünü sınırla; hava durumu için CSP `connect-src` izni ya da widget'ı kaldır.
- **D10-10 (erişilebilirlik):** `/fiyatlandirma` gerçek `<form>` + `role=dialog`/Esc/odak; `/bildirimler` `aria-live` ve sekme `aria-selected`; `/siparisler` kalem aria-label; `document.title` (`/kurulum`, `/veri-ice-aktarim`); `/cikti` başlık sırası.
- **D10-0 (QA altyapısı):** kimliksiz `/giris` için oturumsuz ayrı sekme adımı; grup 1'in 4 rotasında grafik bbox; "yeniden test edilmedi" maddeler için adım bütçesi; POS tahsilatı içeren tohum veri.

## Tur 9 → Tur 10 karşılaştırması

| Ölçüt | Tur 9 | Tur 10 | Fark |
|---|---|---|---|
| Ölçülen sayfa kaydı | 49 (47 rota) | **47** (47 rota) | −2 kayıt (tur 9'da `/giris` 3 kez sayılmıştı) |
| Oturumsuz / engellenen rota | ≈29 | **0** | oturum engeli çözüldü |
| Toplam bulgu | 159 | **82** | −77 |
| KRİTİK | 5 | **3** | −2 (tur 9'un 5 KRİTİK'inin **5'i de KAPANDI**; 3 yeni KRİTİK) |
| Düzenlenemeyen (ham / gerekçeli / gerçek) | 12 / 2 / 10 | **20 / 3 / 17** | +8 / +1 / +7 (bkz. not) |
| Ortalama görsel (ölçülebilen) | 1,33 | **4,06** | +2,73 |
| Ortalama kullanıcı dostu (ölçülebilen) | 1,12 | **3,62** | +2,50 |

**Önceki bulguların durumu** (grup sonuçlarındaki `onceki_bulgular` alanından; toplam 187 kayıt):
- **KAPANDI: 36** (grup 1: 14, grup 2: 3, grup 3: 3, grup 4: 6, grup 7: 5, grup 8: 5). Öne çıkanlar: Profesyonel mod menü `[hidden]` (38 → 0), banka dekont tarihi (giriş + satır), "Bekliyor" → "Müşteriyle eşleştir", banka storno sonrası anlık bakiye, taslak fatura kalem düzenleme, TASLAK siparişte tür/cari/şube, sipariş oluşturma bandı, eksi kasa uyarısı, sayım Şube doğrulaması, cari telefon ve Türkçe harf araması, cari düğme aria-label'ları, çıktıda tr-TR tutar ve has gram, "Hurda alımı gir" derin bağlantısı, karanlık tema düğme kontrastı, vezne/ürün/sepet virgüllü ondalık, satış analizi `aria-selected`, emanet storno etiketi/boş durum/devre dışı sebebi, `/ayarlar` WASM ve "Role Owner", bildirim sekme açıklaması, Panel grafik ezilmesi (grafik kaldırıldı), 3 bilinçli tasarım teyidi (kullanıcı rolü, kullanıcı silme, dışa aktarım geçmişi).
- **AÇIK KALAN: 101** (grup 1: 15, grup 3: 37, grup 4: 31, grup 7: 10, grup 8: 8). Grup 3'ün 14'ü "açık (bu turda yeniden test edilmedi)" işaretli — kod değişmemiş görünüyor, açık sayıldı.
- **KISMEN: 14** (grup 1: 7, grup 3: 2, grup 4: 3, grup 7: 1, grup 8: 1) — ör. kasa şubesi gerekçeli kilit, POS terminolojisi, cari pasif menüsü, sayım satır hatası (metin düzeldi, konum değil), `/cikti` başlık sırası ve mobil üst bar, POS oran girişi, `/kurulum` title/H1/negatif tutar, `/uyum` terim, bildirim Yenile.
- **ÖLÇÜLEMEDİ / DOĞRULANAMADI: 36** (grup 1: 12, grup 4: 6, grup 7: 6, grup 8: 12 — `/giris`'in 5'i dahil). Açık sayılır, TEMİZ değildir.
- Grup 5 ve 6'nın 12 rotasında önceki tur (tur 9) kaydı yoktu — tur 9'da oturumsuz kalmışlardı; bu turda ilk kez tam ölçüldüler.

**Yeni bulgu:** bu turda ilk kez yazılan bulgu ≈**25** (82 bulgunun başlık eşleştirmesiyle önceki turda karşılığı olmayanlar; grup 1: 2, grup 2: 4, grup 3: 4, grup 4: 0, grup 5: 2, grup 6: 3, grup 7: 4, grup 8: 6). En önemlileri: 3 KRİTİK (`/gider` toplam, `/hurda` onaysız ramat, `/isletme` kimlik alanları), `/veri-disa-aktarim` kod adları, `/banka/ekstre` binen metin, `/gider` STORNO jargonu, kabuk SignalR/CSP gürültüsü, 4 rotada K3 başlık kırılması.

**Düzenlenemeyen giriş listesi farkı (tur 9 → tur 10):**
- *Kapananlar (6):* `/banka` dekont tarihi; `/banka` dekont durumu (Bekliyor); `/faturalar` taslak kalemleri; `/siparisler` TASLAK cari/tür/şube; `/kullanicilar` kendi rolü/erişimi (gerekçeli bilinçli tasarım, doğrulandı); `/kullanicilar` kayıt silme (pasife alma, bilinçli). Ayrıca `/veri-ice-aktarim` geçmişi kasıtlı değişmezlik olarak KAPANDI ve listeden çıktı.
- *Durum değiştiren:* `/kasalar` Kasa → Şube: "alan yok" → gerekçeli kilit + para aktarma yolu (KISMEN, hâlâ listede).
- *Süren (4):* `/vezne` fiş türü; `/faturalar` kuyum Toplam; `/gider-pusulalari` VUK md.234 alanları; `/veri-disa-aktarim` geçmişi.
- *Yeni (15):* `/isletme` adres, telefon, vergi dairesi, vergi no (**tamamen yeni, KRİTİK**); `/gider` kayıt; `/hurda` alış ve satış satırı; `/banka/ekstre` sahne ve onaylı satır; `/emanet` kilitli alanlar ve storno satırı; `/ayarlar` rol ve künye; `/uyum` şüpheli bildirim; `/raporlar/satis-analizi` rapor rakamları. Bunların çoğu (`/isletme` hariç) tur 8'de de listelenmiş, tur 9'da oturum engeli yüzünden ölçülemeyip listeden düşmüştü — artış ürün gerilemesi değil, ölçüm kapsamının geri gelmesidir.

## Temizlik
Bu turda yapılmadı — fikstür kiracısı (`01M31G7HX1T9M02F1C54S4YNM1`, "QA-TEST-FRONTEND Kuyumculuk") sonraki turlar ve kurucuya gerçekçi örnekler için **kalıcı** tutuluyor. `SELECT COUNT(*) FROM tenants` = **10** (fikstür kiracısı mevcut: 1). `qa-fikstur.json` boşaltılmadı (temizlik adımı bu turda atlandı).

Bu turda fikstür kiracısında kalan QA-TEST izleri (grup notlarından):
- Grup 3: `/fiyatlandirma` "QA-TEST G3 Fiyat Yeni" fiyat listesi ve `/pos` "QA-TEST G3 POS Yeni" cihazı (aktif; silinmedi).
- Grup 7: `/hurda` **gerçek hurda satışı 0,500 g Altın, ₺3.351,93, Ana Kasa** — sistem bu kaydı stornolayamıyor (KRİTİK 2'nin kanıtı); fikstür hurda bakiyesi buna göre azaldı.
- Grup 8: `/siparisler` S-2026-000003 (Ayşe Yılmaz, 14 Ayar Alyans Çifti, Taslak).
- Grup 1: `/sayim` "QA-TEST G1 T10 Sayim" oturumu; `/banka` 22.09.2026 +₺360 dekont stornolandı.
- Geri alınanlar: `/vezne` test fişi iptal, `/islemler` test satışı iade, `/faturalar` test taslağı iptal, `/gider` test gideri stornolandı (kasa ₺5.508,51'e döndü), `/evraklar` banka adı geri alındı, `/vitrin` varsayılana döndü, `/kur` manuel kur canlı kaynağa geri çekildi, `/kullanicilar` ve `/urunler/agac` test kayıtları aktif duruma geri alındı.
