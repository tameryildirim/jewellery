# Tarayıcı QA turu — TUR 3 (2026-09-17, uygulama içi tarayıcı, http://localhost:5199)

> Kurucu talimatı: "ekran testlerini tıklayarak benim tarayıcımdan yap, hiçbir ayrıntıyı atlama". Yöntem her sayfada: (1) sayfaya git, (2) konsol/ağ hatalarını oku, (3) erişilebilirlik ağacını oku (başlık, boş durum, tablo, düğmeler), (4) ekran görüntüsü, (5) her modal/formu aç, doğrulama mesajlarını tetikle, `QA-TEST-` önekli kayıt oluştur → düzenle → (varsa) pasife al → aktife al, (6) karanlık tema + dar ekran (375px), (7) basit/profesyonel mod anahtarı. Bulgular önceliklendirilip düzeltme paketine döner.
>
> **Tur 1 (`QA-TARAYICI-TURU-2026-09-16.md`) ve tur 2 (`...-tur2.md`) dosyaları tarihsel tabandır, bu turda DEĞİŞTİRİLMEMİŞTİR.**
>
> **Tur 3 tamamlandı.** Kiracı: `QA-TEST-3 Kuyumculuk` (`01M2NZ97WE0PC3EGQG0NZX3W74`) — tamamen boş bir kiracıda, veri sıfırdan üretilerek koşuldu. 4 paralel grup, **29 sayfa** (tur 1/2'nin 27 rotası + bu turda ilk kez taranan `/siparisler` ve `/bildirimler`).

| # | Rota | Sayfa | Durum | Konsol/ağ | Bulgular |
|---|---|---|---|---|---|
| 1 | `/giris` | Giriş | BULGU | Konsol temiz. Boş submit ve `gecersiz-eposta`da istek gitmedi (istemci doğrulaması, `noValidate=true`); yanlış parola → POST `auth/giris` **404**. Mobil 375: 375=375. | 2 bulgu. Başarısız girişte "Kullanıcı bulunamadı" + 404 → kullanıcı varlığı ifşası (ORTA, HATA); sol tanıtım metni yüzük illüstrasyonuna biniyor (DÜŞÜK). |
| 2 | `/` | Panel | BULGU | Yalnız jetonsuz ilk yüklemedeki 401'ler; sonra `auth/ben`, `kur/guncel`, `hubs/kur` 200. POST `kur/guncelle` 200. Mobil 375: 375=375, hamburger `aria-label="Menüyü aç"`. | 2 bulgu (0 KRİTİK). "Kuru güncelle" başarı bandı yok (DÜŞÜK); 7 günlük satış grafiği veri yokken boş kutu, boş durum metni yok (DÜŞÜK). |
| 3 | `/islemler` | İşlemler (POS) | BULGU | POST `islemler/satis` 400 (kredi limiti, mesaj gösterildi) → 200 (fiş `01M2QBPQ09RVQH1RW82QTZSRS2`); POST `satis-iade` 200. Mobil 375: 375=375, sekme grubu 39px tek satır. | 5 bulgu (0 KRİTİK). Ürün listesinde stok adedi yok (ORTA); kiracıda kasa olmadığı için nakit/kart satış hiç yapılamıyor, yönlendirme yok (ORTA); "Son satışlar" aslında iade adaylarını gösteriyor (ORTA); eski hata mesajı formda asılı kalıyor (DÜŞÜK); iade formunda cari ön dolmuyor (DÜŞÜK). |
| 4 | `/cariler` | Cariler | BULGU | POST 201, PATCH 200 ×3, DELETE 200, aktife al 200. 4xx/5xx yok. Mobil 375: **scrollWidth 787 > 375**. | 3 bulgu (0 KRİTİK). Dar ekranda tablo sayfayı taşırıyor (ORTA, GÖRSEL); satır içi düzenlemede Enter kaydetmiyor (ORTA); geçersiz satır içi değerde düzenleyici kapanıyor, değer kayboluyor, hata metni asılı kalıyor (ORTA). |
| 5 | `/urunler` | Ürünler | **HATA** | **crit:** `DeserializeUnableToConvertValue … Urunler+StokVeri Path: $.subeStoklar[0].adet` → `FormatInt32`. GET `urunler/{id}/stok` 200, gövde `adet: 5.000`. Mobil 375: **845 > 375**. | 4 bulgu (**2 KRİTİK**). Stok modalı ondalık `adet`i çözemiyor, 5 adetlik stok "Stok yok" görünüyor (**KRİTİK**); bu istisna Blazor renderer'ı düşürüyor, sonraki tüm kaydetmeler sessizce çalışmıyor (**KRİTİK**); listede stok adedi sütunu yok (ORTA); dar ekranda taşma (ORTA). |
| 6 | `/urunler/agac` | Ürün ağacı | BULGU | POST `urun-kategorileri` 201, PATCH 200; POST `reyonlar` 201, PATCH 200 ×2 (pasif/aktif). 4xx/5xx yok. Mobil 375: **582 > 375**. | 1 bulgu (0 KRİTİK). Dar ekranda reyon tablosu sayfayı taşırıyor (ORTA, GÖRSEL). Başlık kırılması kapandı. |
| 7 | `/urunler/etiketler` | Etiketler | BULGU | Sayfa kaynakları dışında istek yok, 4xx/5xx yok. `window.print` 1 kez çağrıldı, 2 etiket + barkod SVG üretildi. Mobil 375: 375=375. | 1 bulgu (0 KRİTİK). `kur/guncel` oturum boyunca 65 kez çağrılıyor (DÜŞÜK, PERFORMANS). |
| 8 | `/fiyatlandirma` | Fiyat listeleri | BULGU | GET/POST/PATCH `fiyat/listeler` ve `fiyat/kampanyalar` 200. Sayfa kaynaklı 4xx/5xx yok. Mobil: temiz. | 3 bulgu (0 KRİTİK). Has marjı −5 % ve işçilik çarpanı 500 % sınırsız kabul ediliyor (**YÜKSEK**, HATA); tek/varsayılan listede "Pasife al" sessizce devre dışı, sebebi yazmıyor (ORTA); kampanya pasife alma onaysız (ORTA). |
| 9 | `/kasalar` | Kasalar | BULGU | POST `kasalar` 200 ×2, PATCH 200, POST hareket 200 ×3, DELETE 200, aktife al 200. 4xx/5xx yok. Mobil: temiz. | 3 bulgu (0 KRİTİK). Hareket modalı önceki kasadaki "Gider" seçimini hatırlıyor → yeni kasada yanlış yönde kayıt (**YÜKSEK**, HATA); elle girilen hareket hiçbir şekilde düzeltilemiyor (Storno kalıcı disabled) (**YÜKSEK**, DÜZENLENEBİLİRLİK); hareket açıklaması hiçbir ekranda görünmüyor (ORTA). |
| 10 | `/banka` | Banka | BULGU | GET/POST `bankalar` 200, POST dekont 200, PATCH 200. Geçersiz IBAN/negatif tutarda istek gitmedi. Mobil 375: **729 > 375**. | 3 bulgu (0 KRİTİK). "Pasife al" disabled ama kırmızı/etkin görünüyor ve tooltip "kasa" diyor (ORTA); dekont "Bekliyor" durumu elle mutabık işaretlenemiyor (ORTA, DÜZENLENEBİLİRLİK); dar ekranda hareket tablosu taşıyor (ORTA). |
| 11 | `/kur` | Kur | BULGU | GET `kur/guncel`/`gecmis`/`pariteler`/`kaynaklar` 200, POST `kur/manuel` 201, POST `kur/guncelle` 200, `hubs/kur` 200. Kur geçmişi artık DOLU. Mobil: temiz. | 2 bulgu (0 KRİTİK). Manuel kur kaydı düzeltilemiyor/silinemiyor (**YÜKSEK**, DÜZENLENEBİLİRLİK); kaynak paritesinde alış > satış çelişkisi uyarısız (DÜŞÜK, HATA). |
| 12 | `/emanet` | Emanet | BULGU | GET `emanet/{cariId}` 200, POST `emanet/giris` 200, gerekçeli storno 200. Gram 0 ve 4 karakter gerekçede istek gitmedi. Mobil: temiz. | 3 bulgu (0 KRİTİK). Storno ters kaydı TÜR sütununda "Giriş" yazıyor (ORTA, HATA); ham gram/ayar/taş geri okunamıyor (ORTA); bakiye 0 iken "İade Et" sebepsiz devre dışı (DÜŞÜK). |
| 13 | `/sayim` | Sayım | BULGU | POST `sayim` 200, POST `sayim/{id}/say` 200 ×2 (ürün KODU ile), POST `iptal` 200. Negatif adet/boş adda istek gitmedi. Mobil: temiz. | 3 bulgu (0 KRİTİK). Satır içi "Sayılan" düzenlemesi Enter ve odak kaybında sessizce kaydetmiyor — barkod okuyucu Enter gönderdiği için tezgâhta veri kaybı (**YÜKSEK**); düzenleme moduna girince mevcut değer kayboluyor (ORTA); boş listede kural kutusu var, boş durum metni yok (DÜŞÜK). |
| 14 | `/faturalar` | Faturalar | BULGU | POST `faturalar` 200 ×2, PATCH `durum` 200 ×2 (onaylı), filtre 200. Tek hata: GET `islemler/fis/{elle girilen id}` 404 (UI "Fiş bulunamadı" gösterdi). Masaüstü 1265=1265; mobil 375: **1180 > 375**. | 4 bulgu (0 KRİTİK). TASLAK fatura ve kalemleri düzenlenemiyor, düzeltme numara serisini yakıyor (**YÜKSEK**); "Fişten Fatura Kes" 26 haneli ULID'yi elle yazdırıyor (**YÜKSEK**); Ödendi durumundan geri dönüş yok (ORTA); dar ekranda ağır taşma, 1280'de sütunlar kırılıyor (ORTA). |
| 15 | `/gider-pusulalari` | Gider pusulaları | **HATA** | GET liste/`sure-takibi`/`{id}` 200; filtreler 200. **POST `islemler/hurda-alis` 400** ("Satıcı tipi seçin: NIHAI_TUKETICI…"). Mobil: 375=375. | 4 bulgu (**2 KRİTİK**). Hurda Alış formunda satıcı kimliği alanları yok → arayüzden gider pusulası ÜRETİLEMİYOR (**KRİTİK**, HATA); kayıt hiçbir şekilde düzeltilemiyor, storno yolu yok, detaydaki "Kaydet" yanıltıcı (**KRİTİK**); "Hurda alımı gir" bağlantısı yanlış sekmeye (Satış) düşüyor (ORTA); boş formda Kaydet sessiz (ORTA). |
| 16 | `/gider-pusulalari/{Id}/belge` | Gider pusulası belgesi | BULGU | Sayfaya ait hata yok. GET `.../belge` 200. Başlık `GP-2026-000001 — Gider Pusulaları`. Karanlık temada belge beyaz kâğıt olarak kalıyor (bilinçli). | 1 bulgu (0 KRİTİK). İşletme adresi ve vergi dairesi/no olmadan yazdırılabiliyor, uyarı var ama Yazdır engellenmiyor (ORTA). **Turun en yüksek görsel puanı (5/5).** |
| 17 | `/uyum` | Uyum (tevsik/MASAK) | BULGU | `masak/ozet`, `esikler`, `kimlik-tespitleri`, `ara`, `supheli` 200; POST `supheli` 201; PATCH `gonderildi` 200. 4xx/5xx yok. Mobil: temiz. | 3 bulgu (0 KRİTİK). Gönderim referansı boşken Kaydet sessizce etkisiz (ORTA); GONDERILDI kaydına "Kapat" yolu kalmıyor (ORTA); Saklama Süresi sekmesi kart yerine düz metin (DÜŞÜK, GÖRSEL). |
| 18 | `/raporlar` | Raporlar | **HATA** | **Konsol:** `<path> attribute d: Expected number, "… 1 1 110,0 20,0 Z"` (pasta grafik, ORTAK bileşen). `raporlar/aylik`, `gunluk`, `cari-yaslandirma`, `stok-degeri`, `pos-fatura-mutabakati` 200. Mobil: temiz. | 4 bulgu (0 KRİTİK, 2 sayısal tutarsızlık). Stok Dağılımı pasta grafiği Türkçe ondalık virgülüyle üretildiği için hiç çizilmiyor (**YÜKSEK**, HATA); aylık özet veresiye/tahsilat 0 gösteriyor, aynı sayfadaki VERESİYE satış ve ₺150 tahsilatla çelişiyor (**YÜKSEK**, HATA); sayı biçimi karışık (DÜŞÜK); "KÂR" ve "NAKİT FARKI" kartları aynı rakamı iki anlamla veriyor (DÜŞÜK). |
| 19 | `/raporlar/kar-zarar` | Kâr/zarar | BULGU | `kar-zarar`, `kar-zarar/urun`, `stok-degerlemesi` 200; "Geçen ay" boş durumu doğru. Ters tarih aralığında **istek gitmedi** (istemci doğrulaması). Mobil: temiz. | 3 bulgu (0 KRİTİK). HAS hasılatı aylık özetle uyuşmuyor: 0,5917 g vs 11,45 has — aynı tek satış, ~19 kat fark (**YÜKSEK**, HATA); ürün adı ile "maliyet eksik" rozeti bitişik (DÜŞÜK, ERİŞİLEBİLİRLİK); ilk açılış 5-6 sn "Yükleniyor..." (DÜŞÜK, PERFORMANS). **Görsel 5/5.** |
| 20 | `/raporlar/vade-takibi` | Vade takibi | BULGU | `vade-takibi?ufukGun=7/60` 200; bilinçli `-1` → 400 (mesaj ekranda). Mobil: temiz. | 2 bulgu (0 KRİTİK). Ekran veri varken doğrulanamadı: ₺4.000 VERESİYE satışa rağmen açık alacak/cari bakiye/yaşlandırma hepsi 0 (ORTA, HATA — raporlar arası tutarsızlık); negatif ufuk değeri hâlâ sunucuya gidiyor (DÜŞÜK). |
| 21 | `/denetim-izi` | Denetim izi | BULGU | `denetim/ozet`, `denetim?limit=100&offset=0`, filtreler **tek istek** 200. Ters tarih aralığında istek gönderilmedi. Mobil: temiz. | 4 bulgu (0 KRİTİK). Tam TCKN gösteren gider pusulası detay/belge okumaları "kişisel veri erişimi" olarak işaretlenmiyor (**YÜKSEK**, HATA — KVKK); denetim izi otomatik kur kayıtlarıyla doluyor (1.877 kayıt/1 gün), aynı saniyede çift snapshot (ORTA, PERFORMANS); "QA TestSAHIP" bitişik (DÜŞÜK); sayfalama/dışa aktarım yok (DÜŞÜK). |
| 22 | `/siparisler` | Siparişler *(ilk kez tarandı)* | **HATA** | GET `siparisler?limit=200` 200, POST 200 (`S-2026-000001`), GET `{id}` 200, PATCH 200, POST `gecis` 200 ×2. 4xx/5xx yok; ortak pasta grafik hatası konsolda. | 4 bulgu (**1 KRİTİK**). Düzenle-kaydet kalem verisini bozuyor: ürün bağlantısı, ayar ve gram sessizce siliniyor (**KRİTİK**, HATA); eksik formda "Siparişi oluştur" sessiz (ORTA); kalem tablosu girdilerinin erişilebilir adı yok (DÜŞÜK); adet `1.000` biçiminde number girdisi (DÜŞÜK). |
| 23 | `/bildirimler` | Bildirimler *(ilk kez tarandı)* | BULGU | `bildirimler?durum=ACIK/TUMU` 200, `bildirimler/sayi` 200, `isler` 200 — **aynı istek 2 kez**. 4xx/5xx yok. 1280: 1265=1265. | 5 bulgu (0 KRİTİK). "Tümü" sekmesinde kapalı-bildirim metinleri gösteriliyor (ORTA, kod kanıtlı); zamanlanmış iş ayarları arayüzden düzenlenemiyor (ORTA, DÜZENLENEBİLİRLİK); filtre değişiminde çift istek (DÜŞÜK); `kur/guncel` onlarca kez (DÜŞÜK); "Şimdi çalıştır" bugün koşmuş işte sonucu önceden söylemiyor (DÜŞÜK). |
| 24 | `/isletme` | İşletme | BULGU (kısmi) | GET `admin/tenant` 200 ×2, `subeler` 200, `yetkiler/benim` 200. Sonra **oturum düştü:** `kur/guncel` 401 → `auth/yenile` 401 → `auth/cikis` 204 → `/giris`. | 5 bulgu (0 KRİTİK). Oturum bitişi SESSİZ: açık form uyarısız kapanıyor, veri kayboluyor, dönüş adresi korunmuyor (**YÜKSEK**); pasif şube aktife alınamıyor (**YÜKSEK**, DÜZENLENEBİLİRLİK); kimlik telefonunda doğrulama yok, 'abc' kabul (ORTA, HATA); kur kaynağı/maliyet yöntemi hiç belirlenmemişken bile seçilemiyor (ORTA); başlık bloğu iki kez tekrarlanıyor (DÜŞÜK). **Tur 2'nin KRİTİK'i "işletme adı düzenlenemiyor" KAPANDI.** |
| 25 | `/kilavuz` | Kılavuz | BULGU (kısmi) | `/kilavuz.html` HTTP 200, 50.313 bayt, 18 bölüm, konsol/CSP ihlali **YOK** (iframe kaldırılmış; `X-Frame-Options: DENY` ve `frame-ancestors none` gevşetilmemiş). `/kilavuz` rotası oturum bittiği için `/giris`e düştü. | 2 bulgu (0 KRİTİK). "Uygulamayı başlatma" bölümü son kullanıcıya değil geliştiriciye yazılmış ve YANLIŞ adresi veriyor (`localhost:5000`, gerçekte 5199) (**YÜKSEK**); `/kilavuz` oturum istiyor ama `/kilavuz.html` oturumsuz açılıyor (DÜŞÜK). **Tur 1/2'nin KRİTİK CSP bulgusu fiilen çözülmüş (rota canlı doğrulanamadı).** |
| 26 | `/veri-disa-aktarim` | Veri dışa aktarım | ÖLÇÜLEMEDİ | Bu turda bu rotaya ait istek yok. Oturum bitişi kanıtı: `auth/yenile` 401 → `auth/cikis` 204 → `/giris`. | 1 bulgu. Rota canlı doğrulanamadı — önceki 4 kalem KAPANDI sayılamaz, yalnız kod kanıtı var (**YÜKSEK**, HATA/süreç). Puanlar tur 2'den TAŞINDI. |
| 27 | `/kullanicilar` | Kullanıcılar | ÖLÇÜLEMEDİ | Bu turda bu rotaya ait istek yok (oturum bitti). | 2 bulgu. Rota canlı doğrulanamadı (**YÜKSEK**); kullanıcı kendi rolünü/erişim durumunu değiştiremiyor — kasıtlı kilit, gerekçeli (ORTA, DÜZENLENEBİLİRLİK). Puanlar tur 2'den TAŞINDI. |
| 28 | `/yetkiler` | Yetkiler | ÖLÇÜLEMEDİ | Bu turda bu rotaya ait istek yok (oturum bitti). | 1 bulgu. Rota canlı doğrulanamadı (**YÜKSEK**); kirli-durum ve matris genişliği kodda düzeltilmiş görünüyor, canlı kanıt yok. Puanlar tur 2'den TAŞINDI. |
| 29 | `/ayarlar` | Ayarlar (tema, mod) | ÖLÇÜLEMEDİ | Bu turda bu rotaya ait istek yok (oturum bitti). | 1 bulgu. Rota canlı doğrulanamadı (**YÜKSEK**); profil düzenleme düğmesi ve şifre alanı kilidi kodda var, canlı kanıt yok. Puanlar tur 2'den TAŞINDI. |

## Çapraz kontroller
- [x] **Karanlık tema tüm sayfalarda okunaklı** — body bg `rgb(11,18,32)`, color `rgb(232,238,246)`; gezilen tüm rotalarda tutarlı. Gider pusulası belgesi karanlık temada da bilinçli olarak beyaz kâğıt kalıyor. Tur 1'deki `/giris` ve `/kilavuz` istisnaları kapandı.
- [ ] **Dar ekran 375px: yatay kaydırma yok — KISMEN.** 29 sayfanın ölçülebilenlerinde 4 rota hâlâ taşıyor: `/cariler` 787, `/urunler` 845, `/urunler/agac` 582, `/banka` 729, `/faturalar` 1180 (5 rota). Geri kalan tüm ölçülen rotalarda 375 = 375 — tur 2'de 27/27 taşıyordu, artık tabloların kendisi taşıyor (kabuk/çekmece düzeltmesi tuttu).
- [x] **Basit / profesyonel mod anahtarı** — çalışıyor ve artık içerik de sadeleşiyor: Basit modda nav 25 → 8 bağlantı, Panel metni 432 → 388 karakter, grafik ve Bugün Özet kalkıyor, açıklama metni çıkıyor. (Tur 1/2'nin "içerik sadeleşmiyor" bulgusu KAPANDI.)
- [ ] **Oturum süresi dolunca temiz yönlendirme — BAŞARISIZ (yeni biçimde).** Tur 2'deki 3× yenile + "Beklenmeyen hata" bandı kapandı; yerine **sessiz** davranış geçti: `auth/yenile` 401 → `auth/cikis` 204 → `/giris`, bant yok, açık form verisi kayıp, dönüş adresi (`?donus=`) korunmuyor. Ayrıca refresh token rotasyonu yüzünden grup 4'ün 4 rotası hiç ölçülemedi.
- [x] **Her kaydet akışında başarı bandı — BÜYÜK ÖLÇÜDE GEÇTİ.** "Kaydedildi." / "Hareket kaydedildi!" / "Manuel kur kaydedildi…" / "Kayıt yeniden aktif edildi." bantları cariler, ürünler, ağaç, fiyatlandırma, kasalar, banka, kur, emanet, sayım, faturalar, uyum ekranlarında doğrulandı. **İstisna:** Panel "Kuru güncelle" (bant yok).
- [x] **Sessiz etkisizlik (geçersiz formda hiçbir şey olmaması) — büyük ölçüde kapandı.** 15+ ekranda Türkçe alan altı doğrulama + "Form eksik ya da hatalı — işaretli alanları düzeltin." bandı var ve istek hiç gitmiyor. **Kalan istisnalar:** `/islemler` hurda alış boş form, `/siparisler` boş form, `/uyum` boş gönderim referansı, `/bildirimler` yok.
- [x] **Escape ile modal kapatma** — ortak `ModalPencere` düzeltmesi `/cariler`, `/urunler`, `/urunler/agac`, `/kasalar`, `/faturalar`, `/gider-pusulalari` üzerinde doğrulandı.
- [ ] **Düzenlenebilirlik (kurucu hedefi: "her veri girişi düzenlenebilir") — KISMEN.** Cari (modal + satır içi), ürün, kategori, reyon, satış kalemi, sipariş (taslak), kasa/banka hesabı, işletme kimliği düzenlenebiliyor; pasife alınan kayıtlar geri alınabiliyor. **Kalan 25 kalem** aşağıda listelendi; en ağırları gider pusulası (hiçbir düzeltme yolu yok), kasa hareketi ve manuel kur.

## Tur 2 → Tur 3 karşılaştırması

### Önceki bulguların akıbeti (takip edilen 148 kalem)
| Durum | Sayı | Oran |
|---|---|---|
| **KAPANDI** | **89** | %60,1 |
| **KISMEN** | **38** | %25,7 |
| **AÇIK** | **21** | %14,2 |

Grup bazında:

| Grup | Takip edilen | Kapandı | Kısmen | Açık |
|---|---|---|---|---|
| 1 (`/giris` … `/urunler/etiketler`) | 37 | 29 | 3 | 5 |
| 2 (`/fiyatlandirma` … `/faturalar`) | 44 | 28 | 8 | 8 |
| 3 (`/gider-pusulalari` … `/siparisler`) | 39 | 29 | 3 | 7 |
| 4 (`/bildirimler` … `/ayarlar`) | 28 | 3 | 24 | 1 |

> **Uyarı (Madde 1):** grup 4'ün 24 "KISMEN" kaleminin çoğu **canlı doğrulanamadığı için** kapanmış sayılmadı (oturum süresi doldu, yalnız kaynak kod kanıtı var). Gerçek kapanma oranı bu yüzden olduğundan düşük görünüyor; grup 4 uzun ömürlü QA jetonuyla yeniden koşulmalıdır.

### Sayı karşılaştırması
| Ölçüt | Tur 2 | Tur 3 | Fark |
|---|---|---|---|
| Ölçülen sayfa | 27 | **29** (2 yeni rota: `/siparisler`, `/bildirimler`) | +2 |
| Toplam bulgu | 149 | **81** | **−68** |
| KRİTİK | 11 | **5** | **−6** |
| YÜKSEK | 31 | **18** | −13 |
| ORTA | 63 | **34** | −29 |
| DÜŞÜK | 44 | **24** | −20 |
| Yeni bulgu | 33 | **52** | +19 |
| Kapanan önceki kalem | 12 | **89** | +77 |
| Ortalama görsel puanı | 3,85 | **4,00** | **+0,15** |
| Ortalama kullanıcı dostu puanı | 3,15 | **3,48** | **+0,33** |
| Düzenlenemeyen veri girişi kalemi | 30 | **25** | **−5** |

### Düzenlenemeyen giriş listesi farkı (30 → 25)
**Listeden düşenler (artık düzenlenebilir / geri alınabilir):** satış fişi kaleminin adet-fiyatı (satır içi), cari (modal + satır içi + aktife al), ürün (Gerçek Milyem/Birim dâhil) ve açılış stoğu, ürün kategorisi, **reyon** (tur 2 KRİTİK'i), fiyat listesi + kampanya, kasa kartı (döviz/şube kuralıyla), banka hesabı (pasife al), **işletme kimliği** (ad/adres/telefon/vergi dairesi/vergi no — tur 2 KRİTİK'i), pasif kasa/ürün/cari/reyonun aktife alınması.

**Listede kalanlar + bu turda eklenenler (25 kalem):** aşağıdaki "Düzenlenemeyen veri girişleri" tablosu. Yeni eklenenler: sipariş türü/cari (taslakta bile yok), TASLAK dışı sipariş, zamanlanmış iş ayarları ve bildirim eşikleri, dekont "Bekliyor" durumu, pasif şubenin aktife alınması, kullanıcının kendi rolü (kasıtlı kilit).

## Bulgu özeti — TUR 3 (2026-09-17)

### Sayılar
| Ölçüt | Değer |
|---|---|
| Gezilen sayfa | **29** (4 paralel grup: 7+7+8+7) — bunların **4'ü canlı ölçülemedi** (`/veri-disa-aktarim`, `/kullanicilar`, `/yetkiler`, `/ayarlar`), 3'ü kısmi (`/isletme`, `/kilavuz`, `/bildirimler`) |
| Toplam bulgu | **81** |
| KRİTİK | **5** |
| YÜKSEK | **18** |
| ORTA | **34** |
| DÜŞÜK | **24** |
| Yeni bulgu | **52** |
| Kapanan önceki kalem | **89** · Kısmen **38** · Açık **21** (takip edilen 148 kalem) |
| Ortalama görsel puanı | **4,00 / 5** (toplam 116/145) |
| Ortalama kullanıcı dostu puanı | **3,48 / 5** (toplam 101/145) |
| Düzenlenemeyen veri girişi kalemi | **25** |
| Kendiliğinden oluşan hata | `/urunler` Stok modalı **JsonException + renderer ölümü** (yeniden üretildi) · ORTAK pasta grafik `<path> d` kültür hatası (`/raporlar`, `/denetim-izi`, `/siparisler` …) · `/bildirimler` çift istek · oturum bitişinde sessiz çıkış. Diğer tüm 4xx'ler kasıtlı geçersiz denemelerden. |

En düşük puanlı sayfalar: `/urunler` (4/2), `/gider-pusulalari` (4/2), `/islemler` (4/3), `/faturalar` (3/3), `/raporlar` (3/3). En yüksek: `/gider-pusulalari/{id}/belge` (5/4), `/raporlar/kar-zarar` (5/4).

### KRİTİK bulgular (5) — rota · kanıt · öneri
1. **`/urunler` — Stok modalı API'nin ondalık `adet` değerini çözemiyor; gerçek stok gizleniyor.** Kanıt: GET `/api/v1/urunler/01M2QBHBXABYA0DWX735R5EMAB/stok` 200, gövde `{"toplamAdet":5.000,"subeStoklar":[{"adet":5.000,…}]}`; `Kuyumcu.Web.Pages.Urunler+StokVeri.adet` **int** beklediği için `DeserializeUnableToConvertValue … FormatInt32` (konsol crit) ve modal "Şube Stokları — Stok yok" diyor. Ürünün gerçekte 5 adet stoğu var. → `StokVeri.adet`'i `decimal` yap (ya da API'de tamsayı serileştir) + JSON okuma hatasını kullanıcıya hata bandıyla bildir.
2. **`/urunler` — Stok modalı hatasından sonra sayfadaki tüm kaydetmeler sessizce çalışmıyor (Blazor renderer ölüyor).** Kanıt: modal açıldıktan sonra "Ürünü Düzenle"de ad değiştirilip Kaydet 2 kez tıklandı → **hiç PATCH gitmedi**, hata çıkmadı, modal açık kaldı. Sayfa yenilendikten sonra aynı işlem PATCH 200 + "Kaydedildi." verdi. Kullanıcı veri kaybettiğini fark etmez. → Bileşen düzeyinde `ErrorBoundary`; yakalanmayan istisnada görünür hata + "Sayfayı yenile" eylemi, sessiz ölü durum bırakma.
3. **`/gider-pusulalari` (+`/islemler` Hurda Alış) — arayüzden gider pusulası ÜRETİLEMİYOR.** Kanıt: Hurda Alış formunun tüm alanları Cari / Ödeme Türü / Kasa / Gram / Ayar / Has Gram Fiyatı / Açıklama; profesyonel modda da satıcı tipi–ad soyad–TCKN–adres YOK (`body.innerText` "TCKN" içermiyor). Geçerli veriyle Kaydet → POST `islemler/hurda-alis` **400**: "Hurda alışında satıcı kimliği zorunludur (VUK md.234)… Satıcı tipi seçin: NIHAI_TUKETICI, ESNAF_MUAF, MUKELLEF". OpenAPI'de `HurdaSaticiDto` var, arayüz doldurmuyor (test verisi ancak API'den POST edilerek üretilebildi: `GP-2026-000001`). → Forma satıcı bloğu ekle (tip + ad soyad + TCKN + adres + mal açıklaması, tipe göre zorunluluk), alanlar dolmadan Kaydet açılmasın.
4. **`/gider-pusulalari` — kayıt hiçbir şekilde düzeltilemiyor; detaydaki "Kaydet" yanıltıcı.** Kanıt: detay dialog düğmeleri `['×','🖨 Yazdır','Kaydet','İptal']`, düzenlenebilir alan sayısı **0**; ekranın kendi metni: "Hurda alımı için sistemde ters kayıt (storno) yolu BULUNMUYOR: bu kayıt bugün sistem içinden hiçbir şekilde düzeltilemiyor." VUK belgesi; yanlış TCKN/adres kalıcı. → VUK'un izin verdiği düzeltme yolunu (iptal + yeni pusula, ters hurda alış) sisteme ekle; alanı olmayan "Kaydet" düğmesini gösterme.
5. **`/siparisler` — Düzenle-kaydet kalem verisini bozuyor: ürün bağlantısı, ayar ve gram siliniyor.** Kanıt: `S-2026-000001` kalemi başta "ÜRÜN: QA-TEST 22 Ayar Bilezik T3 Dz | AYAR: K22 | GRAM: 12.5 | ADET: 1"; yalnız açıklama ve söz verilen gün değiştirilip PATCH `/siparisler/01M2QE3GA701CQ71KSM9KFG4N5` 200 sonrası kalem "ÜRÜN: — | SERBEST TANIM: QA-TEST 22 Ayar Bilezik T3 Dz | AYAR: — | GRAM: — | ADET: 1" oldu. Kullanıcı bu alanlara dokunmadı, veri **sessizce** kayboldu. → Düzenleme formunda kalemi `urunId`/`ayar`/`gram` ile birlikte yükleyip geri gönder; ürün seçili kalemi serbest tanıma çevirme.

### YÜKSEK bulgular (18) — özet
| # | Rota | Bulgu | Tür |
|---|---|---|---|
| 1 | `/fiyatlandirma` | Has marjı −5 % ve işçilik çarpanı 500 % sınırsız kabul ediliyor (POST 200) — POS'ta maliyet altı fiyat üretir | HATA |
| 2 | `/kasalar` | Hareket modalı önceki kasadaki "Gider" seçimini hatırlıyor → yeni EUR kasasında gizlice −100,00 € çıkış kaydı oluştu | HATA |
| 3 | `/kasalar` | Elle girilen kasa hareketi hiç düzeltilemiyor (Storno kalıcı `disabled`), açıklama da görünmediği için gerekçe okunamıyor | DÜZENLENEBİLİRLİK |
| 4 | `/kur` | Manuel kur kaydı düzeltilemiyor/silinemiyor; yanlış kur ona bağlanan işlemlerle kalıcı | DÜZENLENEBİLİRLİK |
| 5 | `/sayim` | Satır içi "Sayılan" Enter/odak kaybında sessizce kaydetmiyor — barkod okuyucu Enter gönderir, tezgâhta veri kaybı | KULLANILABİLİRLİK |
| 6 | `/faturalar` | TASLAK fatura ve kalemleri düzenlenemiyor; düzeltme yolu numara serisini yakıyor | DÜZENLENEBİLİRLİK |
| 7 | `/faturalar` | "Fişten Fatura Kes" 26 haneli ULID'yi elle yazdırıyor, seçici yok (Önizle → 404) | KULLANILABİLİRLİK |
| 8 | `/raporlar` | Stok Dağılımı pasta grafiği tr-TR ondalık virgülüyle üretildiği için hiç çizilmiyor (ortak bileşen, birçok sayfada konsol hatası) | HATA |
| 9 | `/raporlar` | Aylık özet veresiye/tahsilat 0 gösteriyor; aynı sayfadaki VERESİYE satış ve ₺150 tahsilatla çelişiyor | HATA |
| 10 | `/raporlar/kar-zarar` | HAS hasılatı 0,5917 g vs aylık 11,45 has — aynı tek satış için ~19 kat fark | HATA |
| 11 | `/denetim-izi` | Tam TCKN gösteren gider pusulası okumaları "kişisel veri erişimi" (hassas) olarak işaretlenmiyor — KVKK raporlaması eksik | HATA |
| 12 | `/isletme` | Oturum bitişi SESSİZ: açık form uyarısız kapanıyor, girilen veri kayboluyor, dönüş adresi korunmuyor | KULLANILABİLİRLİK |
| 13 | `/isletme` | Pasife alınan şube arayüzden aktife alınamıyor (gerekçe metni var, eylem yok) | DÜZENLENEBİLİRLİK |
| 14 | `/kilavuz` | "Uygulamayı başlatma" bölümü geliştiriciye yazılmış ve YANLIŞ adres veriyor (`localhost:5000`, gerçekte 5199); esnafa Docker/MySQL anlatılıyor | KULLANILABİLİRLİK |
| 15 | `/veri-disa-aktarim` | Rota canlı doğrulanamadı — QA oturumu süre doldu, bulgular kapanmış sayılamaz | SÜREÇ/HATA |
| 16 | `/kullanicilar` | Rota canlı doğrulanamadı — aynı sebep | SÜREÇ/HATA |
| 17 | `/yetkiler` | Rota canlı doğrulanamadı — aynı sebep | SÜREÇ/HATA |
| 18 | `/ayarlar` | Rota canlı doğrulanamadı — aynı sebep | SÜREÇ/HATA |

### Düzenlenemeyen veri girişleri — TUR 3 (25 kalem, 29 sayfanın 13'ünde)
| # | Rota | Düzenlenemeyen kayıt / alan | Gerekçe ekranda var mı? |
|---|---|---|---|
| 1 | `/islemler` | Satış fişi (tarih, ödeme türü, kalem) — kayıttan sonra; İade / Düzelt (ters kayıt) sunuluyor | Evet (kabul edilebilir muhasebe davranışı) |
| 2 | `/urunler` | Ürün şube stoğu — Stok modalı salt okunur; üstelik modal bozuk olduğu için stok hiç görülemiyor | Evet (ama modal çalışmıyor) |
| 3 | `/kasalar` | Kasa hareketi (tutar/tür/açıklama) — yalnız `disabled` Storno | Evet |
| 4 | `/kasalar` | Kasa hareketi açıklaması geçmişte hiç gösterilmiyor | Hayır |
| 5 | `/banka` | Banka dekontu (yön/tutar/karşı taraf/açıklama/durum) | Evet |
| 6 | `/banka` | Dekont "Bekliyor" durumu — mutabakat işareti konulamıyor | Hayır |
| 7 | `/kur` | Manuel kur kaydı — satırda hiç eylem yok | Hayır |
| 8 | `/emanet` | Emanet giriş hareketi (ayar/gram/taş/açıklama) — yalnız Storno | Evet |
| 9 | `/emanet` | Emanet hareketinin ham gram/ayar/taş değerleri hiç geri okunamıyor | Hayır |
| 10 | `/sayim` | Sayım oturumu adı/kapsamı/şubesi | Evet |
| 11 | `/sayim` | Sayılmış satırı "sayılmadı"ya geri alma | Evet |
| 12 | `/faturalar` | TASLAK fatura (cari/tür/KDV/toplam/açıklama/kalemler) | Evet |
| 13 | `/faturalar` | Fatura kalemi (miktar/fiyat/işçilik/indirim) | Evet |
| 14 | `/faturalar` | "Ödendi" durumu geri alınamıyor (yalnız İptal Et) | Evet |
| 15 | `/gider-pusulalari` | Gider pusulası kaydının tamamı — storno yolu da YOK | Evet (ama çözüm yok) |
| 16 | `/uyum` | Şüpheli işlem bildirimi (tutar/gerekçe/işlem kimliği) | Evet |
| 17 | `/uyum` | Kimlik tespiti kayıtları (bu kiracıda kayıt olmadığı için doğrulanamadı) | — |
| 18 | `/siparisler` | Sipariş türü ve cari — TASLAK'ta bile düzenleme formunda yok | Hayır |
| 19 | `/siparisler` | TASLAK dışı sipariş (Onaylandı/Atölyede/Hazır/Teslim/İptal) — hiçbir alan | Evet |
| 20 | `/bildirimler` | Zamanlanmış iş ayarları (çalışma saati/eşik/etkinlik) | Hayır |
| 21 | `/bildirimler` | Bildirim eşikleri (MASAK süresi, vade hatırlatma günü, saklama süresi) | Hayır |
| 22 | `/isletme` | Kur kaynağı ve maliyet yöntemi — hiç belirlenmemişken bile seçilemiyor | Evet |
| 23 | `/isletme` | Pasif şubeyi aktife alma | Evet (eylem yok) |
| 24 | `/isletme` | Abonelik planı değişimi — bu turda doğrulanamadı (oturum bitti) | — |
| 25 | `/kullanicilar` | Kullanıcının kendi rolü ve kendi aktiflik durumu (kasıtlı kilit) | Evet |

### Görsel / kullanıcı dostu ortalamaları
| Grup | Sayfa | Görsel toplam | Görsel ort. | Kullanıcı dostu toplam | K.dostu ort. |
|---|---|---|---|---|---|
| 1 | 7 | 28/35 | 4,00 | 25/35 | 3,57 |
| 2 | 7 | 27/35 | 3,86 | 24/35 | 3,43 |
| 3 | 8 | 33/40 | 4,13 | 28/40 | 3,50 |
| 4 | 7 | 28/35 | 4,00 | 24/35 | 3,43 |
| **Toplam** | **29** | **116/145** | **4,00** | **101/145** | **3,48** |

> Not: grup 4'ün 4 rotasının puanları tur 2'den TAŞINDI (canlı ölçülemedi); ortalamalar bu yüzden bir sonraki turda yeniden ölçülmelidir.

### Düzeltme paketine giren maddeler (öncelik sırasıyla)
**P0 — KRİTİK (sevk engeli):**
1. `StokVeri.adet` → `decimal` (veya API'de tamsayı serileştirme) + JSON hata bandı — `/urunler`.
2. Bileşen düzeyinde `ErrorBoundary` + görünür hata/"Sayfayı yenile" — sessiz renderer ölümü yasak (tüm sayfalar).
3. Hurda Alış formuna satıcı kimliği bloğu (tip/ad soyad/TCKN/adres/mal açıklaması) — `/islemler` + `/gider-pusulalari`.
4. Gider pusulası düzeltme yolu (iptal + yeni pusula ya da ters hurda alış) + alanı olmayan "Kaydet" düğmesinin kaldırılması.
5. Sipariş düzenlemesinde kalem `urunId`/`ayar`/`gram` korunması — `/siparisler`.

**P1 — YÜKSEK:**
6. Ortak pasta grafik bileşeninde `CultureInfo.InvariantCulture` ile SVG koordinat biçimleme (tek düzeltme, çok sayfa).
7. Rapor tutarlılığı: aylık veresiye/tahsilat toplamları + HAS hasılatı tek kaynaktan; veresiye satışın cari bakiyesine ve vade takibine düştüğünü doğrulayan uçtan uca test.
8. Kasa hareketi modalının her açılışta sıfırlanması + başlıkta kasa adı + başarı bandında yön.
9. Satır içi düzenleyicilerde **Enter = kaydet, Escape = iptal** (ortak davranış) — `/sayim`, `/cariler`.
10. Elle kasa hareketi ve manuel kur için gerekçeli storno/düzeltme akışı; kasa geçmişine AÇIKLAMA sütunu.
11. TASLAK faturanın tam düzenlenebilir olması; kilit yalnız "Kesildi" ve sonrası için.
12. Fiş seçici (tarih/cari/tutar ile aranabilir) + `/islemler`'den "faturaya dönüştür" kısayolu.
13. Fiyat listesinde has marjı 0–100 / işçilik 0–200 aralık denetimi (istemci **ve** sunucu).
14. Tam TCKN gösteren okumaların `hassas=true` ile denetim izine yazılması (KVKK).
15. Oturum bitişinde "Oturumunuz sona erdi" bandı + `/giris?donus=<rota>` + açık form taslağının saklanması.
16. Pasif şubeye "Aktife Al" (PATCH `aktif=true`).
17. Kılavuzun "Uygulamayı başlatma" bölümünün müşteri diline çevrilmesi ve port bilgisinin yayın yapılandırmasından alınması.

**P2 — ORTA (seçilmiş):** dar ekran kart görünümü (`/cariler`, `/urunler`, `/urunler/agac`, `/banka`, `/faturalar`); devre dışı düğmeler için tek stil + sebep metni standardı (`/fiyatlandirma`, `/banka`, `/emanet`); ürün listesine ve satış seçeneğine stok adedi; kiracı kurulumunda varsayılan kasa; "Son satışlar" listesinin gerçek hareketlerden beslenmesi; emanet storno ters kaydının "Storno (çıkış)" etiketlenmesi; dekont mutabakat işareti; GONDERILDI MASAK kaydına gerekçeli "Kapat"; zamanlanmış iş ayarları diyaloğu; `/bildirimler` "Tümü" sekmesi metin koşulu (`durum == "KAPALI"`).

**P3 — DÜŞÜK:** `kur/guncel` önbelleği (65 istek/oturum); denetim izine kur snapshot'larının yazılmaması + aynı saniyedeki mükerrer kayıt; sayı biçimi tekleştirme (has gram virgül); rozet/etiket bitişikliği (`QA TestSAHIP`, `…Dzmaliyet eksik`, `TaslakKalemler`); boş durum metinleri (Panel grafiği, `/sayim` listesi); erişilebilir ad eksikleri (`/siparisler` kalem girdileri).

### Süreç bulgusu (Madde 1 — gizlenmedi)
QA fikstüründeki erişim jetonu ömrü ~15 dakika ve refresh token **rotasyonlu/tek kullanımlık** olduğu için tur ortasında paralel gruplar birbirinin jetonunu geçersizleştirdi. Grup 1 ve 3 yeniden giriş yapıp devam edebildi; **grup 4'ün 4 rotası (`/veri-disa-aktarim`, `/kullanicilar`, `/yetkiler`, `/ayarlar`) hiç ölçülemedi** ve bu rotaların önceki bulguları KAPANDI olarak işaretlenmedi. Öneri: fikstüre **grup başına ayrı refresh token çifti** ya da en az 2 saatlik erişim jetonu; orkestratör turu başlatmadan hemen önce jetonu yenilesin.

### Temizlik
**Fikstür kiracısı KORUNDU** (`QA-TEST-3 Kuyumculuk`, `01M2NZ97WE0PC3EGQG0NZX3W74`) — sonraki turlar ve kurucuya gösterilecek gerçekçi örnekler için bilinçli olarak silinmedi. `SELECT COUNT(*) FROM tenants` → **8**. Bu turda oluşturulan QA-TEST verileri (cari, ürün, kategori, reyon, fiyat listesi, kampanya, kasalar, banka hesabı + dekont, faturalar, sipariş, gider pusulası `GP-2026-000001`, MASAK bildirimi) kiracıda durmaktadır. Kaynak kod değiştirilmedi, git komutu çalıştırılmadı, 5198 portuna dokunulmadı.
