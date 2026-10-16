# Tarayıcı QA turu — TUR 7 (2026-09-21, uygulama içi tarayıcı, http://localhost:5199)

> Yöntem (tur 1 ile aynı): (1) rotaya git, (2) konsol/ağ hatalarını oku, (3) erişilebilirlik ağacını oku, (4) ekran görüntüsü, (5) her modal/formu aç, doğrulama mesajlarını tetikle, `QA-TEST-` önekli kayıt oluştur → düzenle → (varsa) pasife al → geri al, (6) karanlık tema + dar ekran (375px), (7) basit/profesyonel mod anahtarı. Başlık ve tablo iskeleti tur 1 belgesinden kopyalanmıştır; **tur 1 dosyasına dokunulmamıştır (tarihsel taban).**
>
> **Oturum dürüstlüğü (Anayasa md.1):** yedi grubun yedisinde de öykünme YOK — `/giris` ekranından gerçek e-posta+parola ile giriş yapıldı (POST `/api/v1/auth/giris` 200). Parola/jeton değeri hiçbir yere yazılmadı.
>
> **GİRDİ KESİLDİ.** Bu tur 8 grup / 44 sayfa olarak planlandı; özet aşağıdaki **41 sayfanın** tam grup çıktısıyla üretildi. Grup 8 (3 sayfa) çıktısı bu özete hiç ulaşmadı ve grup 7'nin son sayfası (`/siparisler`) kesik geldi. Eksik sayfalar tabloda **ölçülmedi** olarak işaretlendi; hiçbir sayı tahmin edilmedi.

| # | Rota | Sayfa | Durum | Konsol/ağ | Bulgular |
|---|---|---|---|---|---|
| 1 | `/giris` | Giriş | BULGU | POST `auth/cikis` 204; geçersiz e-postada istek YOK (istemci doğrulaması); olmayan kullanıcı POST `auth/giris` → **401** (artık 404 değil); gerçek giriş 200. | 3 bulgu (0 KRİTİK). Şifre sıfırlama akışı hiç yok (ORTA); şifrede asgari uzunluk doğrulaması ve göz düğmesi yok (DÜŞÜK); sol tanıtım metni yüzük illüstrasyonuna biniyor (DÜŞÜK). |
| 2 | `/` | Panel | BULGU | 4xx/5xx yok. `kur/guncel`, `auth/ben`, `raporlar/gunluk`, `raporlar/aylik`, `bildirimler/sayi` 200; POST `hubs/kur/negotiate` 200, konsolda WebSocket hatası YOK. | 3 bulgu (0 KRİTİK). SignalR kanalı kurulu olmasına rağmen 10 sn'de bir `kur/guncel` yoklaması sürüyor (~360 istek/saat/sekme) (ORTA, PERFORMANS); "Kuru güncelle" geri bildirim vermiyor (DÜŞÜK); negatif kasa bakiyesi uyarısız listeleniyor (DÜŞÜK). |
| 3 | `/islemler` | İşlemler (POS) | BULGU | Açılış GET'leri 200. POST `islemler/satis` 400 (kasıtlı NAKİT tevsik denemesi) → 200 (HAVALE). Beklenmeyen 4xx/5xx yok. | 5 bulgu (0 KRİTİK, 1 YÜKSEK). Ürün listesi aktif süzgeci olmadan çekiliyor, pasif ürün sızıntısı riski sürüyor — kiracıda pasif ürün kalmadığı için bu turda yeniden üretilemedi (YÜKSEK, KISMEN); sepet kalemi adet/fiyat düzenlenemiyor (ORTA); ürün listesinde stok yok (ORTA); tevsik uyarısı varken Kaydet etkin (DÜŞÜK); başarı mesajında ham ULID (DÜŞÜK). |
| 4 | `/cariler` | Cariler | BULGU | 4xx/5xx yok: GET `cari` 200, oluşturma/güncelleme/satır içi telefon 200. | 6 bulgu (**1 KRİTİK**). Aktif cari arayüzden pasife alınamıyor — üçüncü turdur açık (**KRİTİK**); satır içi düzenlemede Enter sessizce kaydetmiyor (YÜKSEK); satırda 7–8 düğme, satır yüksekliği 329px, ekranda 2 satır — geçen tura göre kötüleşti (YÜKSEK); pasif cariye tahsilat/veresiye açık (ORTA); zorunlu alan işaretlenmiyor (ORTA); 6 eylem düğmesinde aria-label yok (DÜŞÜK). |
| 5 | `/urunler` | Ürünler | BULGU | 4xx/5xx yok; oluşturma, güncelleme, pasife alma, aktifleştirme 200. | 6 bulgu (**1 KRİTİK**). "Pasife Al" onayının "yeni satışta seçilemez" sözü tutulmuyor — pasif ürün `/islemler` listesinde seçilebilir (**KRİTİK**); 375px'te kategori/reyon süzgeçleri kırpılıyor, kaydırılamıyor (YÜKSEK); listede ADET sütunu yok (ORTA); stok penceresinde şube adı yerine ham ULID (ORTA); zorunlu alanlar işaretsiz (ORTA); düğme/hata metni bitişik (DÜŞÜK). |
| 6 | `/urunler/agac` | Ürün ağacı | BULGU | 0 yeni 4xx/5xx; kategori POST/PATCH/DELETE 200. | 3 bulgu (0 KRİTİK). Beş "+ Yeni Kategori" düğmesi ayırt edilemiyor (DÜŞÜK); Düzenle kipi hangi kaydı düzenlediğini söylemiyor (DÜŞÜK); "Sıra" değeri görünür etkisiz (DÜŞÜK). |
| 7 | `/urunler/etiketler` | Etiketler | BULGU | Bu rotaya ait 4xx/5xx yok; etiket önizlemeleri istemcide üretiliyor. | 3 bulgu (0 KRİTİK). Sonuçsuz aramada boş durum mesajı yok (ORTA); arama boşalsa da 36 etiketlik eski seçim ve önizleme ekranda kalıyor — yanlış etiket basma riski (ORTA); adet/seçim kalıcı değil, kaydedilebilir "etiket işi" yok (DÜŞÜK). |
| 8 | `/fiyatlandirma` | Fiyat listeleri | BULGU | Yeni 4xx/5xx yok; liste/pasif/aktif işlemleri 200. | 6 bulgu (0 KRİTİK). Alan düzeltilince hata mesajı ekranda kalıyor (ORTA); pasife alma onay sormuyor (ORTA); riskli değer onayı kayıt açılır açılmaz isteniyor (DÜŞÜK); sayısal alanlar nokta ondalıklı (DÜŞÜK); `<form>` yok, Enter kaydetmiyor (DÜŞÜK); aria-invalid yazılmıyor (DÜŞÜK). |
| 9 | `/kasalar` | Kasalar | BULGU | Uygulama kaynaklı 4xx/5xx yok: hareket 200, storno 200, hareketler 200. | 8 bulgu (0 KRİTİK). Kurma formunda şube seçilemiyor (ORTA); düzeltilen alanın hatası kalıyor (ORTA); eksi bakiyeli kasalar uyarısız (ORTA, HATA); storno ters kaydında kilitli ikinci "Storno" düğmesi (ORTA); dolu kırmızı "Pasife Al" kartı domine ediyor (DÜŞÜK); pasife almada onay var, aktifleştirmede yok (DÜŞÜK); geçmişte Gelir/Gider türü yazmıyor (DÜŞÜK); oluşturma ile güncelleme aynı mesajı veriyor (DÜŞÜK). |
| 10 | `/banka` | Banka | BULGU | PATCH `bankalar/{id}` 400 yalnız kasıtlı geçersiz IBAN testinden; diğer tüm istekler 200. **"Ekstre Yükle" dosya seçimi gerektirdiği için bu tur da denenemedi (gizlenmiyor).** | 6 bulgu (0 KRİTİK). Ekran "tarih doğrudan düzeltilebilir" diyor ama dekont tarihi ne girilebiliyor ne düzeltilebiliyor (YÜKSEK, DÜZENLENEBİLİRLİK); dekont "Bekliyor"dan çıkarılamıyor, mutabakat yapılamıyor (ORTA); "Pasife al" ipucu hâlâ "kasa" diyor (ORTA); satır içi Enter kaydetmiyor (ORTA); geçersiz IBAN istemcide yakalanmıyor (DÜŞÜK); satır içi kayıtta geri bildirim yok (DÜŞÜK). |
| 11 | `/kur` | Kur | BULGU | Yeni 4xx/5xx yok; `kur/guncel`, `kur/gecmis`, `kur/pariteler`, `kur/kaynaklar` 200; `hubs/kur/negotiate` 200. Manuel kur kaydedildi ve gerekçeli iptal edildi. | 6 bulgu (0 KRİTİK). Geçmiş otomatik yoklamalarla doluyor, sayfalama yok, manuel kayıt kayboluyor (ORTA); 100 "İptal et" düğmesinin 99'u sebepsiz devre dışı (ORTA); kaynak adları ham (`ALTINKAYNAK`/`MANUAL`) (DÜŞÜK); "MANUALGeçersiz" bitişik rozet (DÜŞÜK); iptal kipinin etiketi "Storno gerekçesi" (DÜŞÜK); tarih süzgeçlerinin id/etiketi yok (DÜŞÜK). |
| 12 | `/emanet` | Emanet | HATA | 4xx/5xx yok: GET `emanet/{cariId}` 200, PATCH `emanet/hareketler/{id}` 200. Has hesabı doğru (6,0 g × 0,916 = 5,496). | 5 bulgu (**1 KRİTİK**). "Emanet Al" kipindeki AÇIKLAMA sunucuya hiç gitmiyor — API'de `aciklama: null`, sessiz veri kaybı (**KRİTİK, HATA**); storno ters kaydı TÜR sütununda yeşil "Giriş" rozetiyle görünüyor (ORTA, HATA); ham gram/ayar/taş geri okunamıyor (ORTA); devre dışı düğmelerin sebebi yazmıyor (DÜŞÜK); müşteri seçilmeden sayfa boş ve yönlendirmesiz (DÜŞÜK). |
| 13 | `/sayim` | Sayım | HATA | POST `sayim` 400 yalnız kasıtlı testten (şubede açık oturum). Diğer istekler 200. **Yalnız bu sayfada GET `kur/guncel` 13 kez.** | 9 bulgu (0 KRİTİK, 2 YÜKSEK). Satır içi "Sayılan" düzenlemesinde Enter sessizce kaydetmiyor — barkod okuyucu yolu fiilen kapalı (YÜKSEK, HATA); DURUM sütununda düz metin "İptal Et" yazıyor, gerçek durum görünmüyor (YÜKSEK, HATA); hata bandı düzenlenen satırdan 391px uzakta (ORTA); hata bandı başarılı kayıt ve iptalden sonra da duruyor (ORTA); açık sayımı olan şube listede belirtilmiyor (ORTA); zorunlu "Şube" boşken alan işaretlenmiyor (ORTA); `kur/guncel` 13 kez (ORTA, PERFORMANS); "→" düğmesinde erişilebilir ad yok (DÜŞÜK); adet alanı type=text (DÜŞÜK). |
| 14 | `/faturalar` | Faturalar | HATA | 4xx/5xx yok; POST `faturalar` 200, `faturalar/{id}/kalemler` 200. **`arama?tur=FIS` 200 dönüyor ama boş sonuç veriyor.** | 5 bulgu (**1 KRİTİK**). TASLAK fatura ve kalemleri düzenlenemiyor; düzeltmenin tek yolu iptal ve numara serisini yakmak (**KRİTİK, DÜZENLENEBİLİRLİK**); "Fişten Fatura Kes" fiş araması hiçbir sonuç döndürmüyor, kip kullanılamaz (YÜKSEK, HATA); oluşturma/iptal sonrası başarı bandı yok (ORTA); 1280px'te tablo kırılıyor, "TaslakKalemler" bitişik (ORTA); Kalemler kipi taslakta "kesilmiştir" diyor (DÜŞÜK). |
| 15 | `/gider-pusulalari` | Gider pusulaları | HATA | 4xx/5xx yok; GET/PATCH 200. Ters tarih aralığı sunucuda da reddedilmiyor (200). | 5 bulgu (0 KRİTİK). "Hurda alımı gir" derin bağlantısı Satış sekmesini açıyor — üçüncü turdur aynı (YÜKSEK, HATA); aynı kipte hem "değiştirilemez" yazıyor hem iki alan düzenlenebiliyor (ORTA); PATCH 200 sonrası bant yok, değişiklik listede görünmüyor (ORTA); listede "Düzenle" yok, düzenleme "Detay" içinde gizli (ORTA); ters tarih aralığı engellenmiyor (DÜŞÜK). |
| 16 | `/gider-pusulalari/{Id}/belge` | Gider pusulası belgesi | BULGU | Geçerli kimlikte 0 hata; tek 4xx bilinçli negatif denemeden (404, ekranda düzgün karşılandı, Yazdır kilitlendi). | 3 bulgu (0 KRİTİK). Belge ekranından kaydı düzeltme/iptal yolu yok (ORTA); "← Geri" bağlantı değil düğme (DÜŞÜK); basılacak belgenin etrafında uygulama kabuğu duruyor (DÜŞÜK). **Turun en yüksek puanlı sayfası (5/4).** |
| 17 | `/uyum` | Uyum (tevsik/MASAK) | BULGU | 4xx/5xx YOK: `masak/ozet`, `masak/supheli`, `kimlik-tespitleri` 200; POST `masak/supheli` 201. Önceki turun 404'ü üretilemedi (serbest metin alanı arama seçicisine çevrilmiş). | 3 bulgu (0 KRİTİK). "Bu kayıt değiştirilemez" bloğu 5 satırda tekrar ediyor (DÜŞÜK); belge no aramasında Enter anında sonuç vermiyor (DÜŞÜK); Saklama Süresi sayıları kart dilinden sapıyor (DÜŞÜK). |
| 18 | `/raporlar` | Raporlar | BULGU | Sayfa isteklerinin tamamı 200/304; konsoldaki 404/400'ler önceki oturumdan kalma artıklar (kaynağı ayrıştırıldı). | 6 bulgu (0 KRİTİK). Ay filtresi değişince stok kartları dönemden bağımsız olduğunu söylemiyor (ORTA); cari yaşlandırmada negatif bakiye alacak gibi gösteriliyor (ORTA, HATA); "FARK VAR" diyor ama düzeltmeye bağlantı yok (ORTA); aynı ekranda üç farklı sayı biçimi (DÜŞÜK); katalogda Satış Analizi kartı yok, Kâr-Zarar iki kez (DÜŞÜK); Yıl/Ay seçicilerinin görünür etiketi yok (DÜŞÜK). |
| 19 | `/raporlar/kar-zarar` | Kâr/zarar | HATA | 0 yeni 4xx/5xx; **önceki turdaki ilk yükleme 401'i TEKRARLANMADI** (ilk denemede 200). Karşılaştırma değişince `kar-zarar/urun` aynı parametrelerle iki kez çağrılıyor. | 5 bulgu (0 KRİTİK). "Karşılaştır" seçimi sessizce etkisiz — istek `karsilastir=ONCEKI_DONEM` ile 200 dönüyor, ekranda tek bir karşılaştırma değeri yok (YÜKSEK, HATA); "Defter has gramı" aylık özetle çelişiyor (78,5012 g vs 89,9512 g, aynı 6 satış) (ORTA, HATA); ürün adı ile "maliyet eksik" rozeti bitişik (DÜŞÜK); yinelenen ürün çağrısı (DÜŞÜK); sayfa başlığı kalıbı bozuk (DÜŞÜK). |
| 20 | `/raporlar/vade-takibi` | Vade takibi | BULGU | `vade-takibi?ufukGun=7/60` 200; `?ufukGun=-1` → 400 (kasıtlı deneme). Bu turda GERÇEK VERİ var (₺221.200,00). | 4 bulgu (0 KRİTİK). Geciken/yaklaşan borcu SIFIR olan cari "Gecikmiş Alacaklar" tablosunda listeleniyor (ORTA); negatif ufuk hâlâ sunucuya gidiyor (DÜŞÜK); hızlı ufuk düğmeleri seçimi ekran okuyucuya bildirmiyor (DÜŞÜK); geçersiz ufukta ufuktan bağımsız kartlar da kayboluyor (DÜŞÜK). **Vadesi GEÇMİŞ senaryo hâlâ ölçülemedi.** |
| 21 | `/denetim-izi` | Denetim izi | HATA | Rotanın kendi istekleri 200 (`denetim/ozet` toplam 10413, `?bitis=2026-09-21` 9209). | 6 bulgu (0 KRİTİK). Bitiş tarihi son günü kapsam dışı bırakıyor — 1204 kayıt düşüyor (ORTA, HATA); kayıtların %76,9'u otomatik `KurSnapshot` (ORTA); ham kod adı sayısı 14 → **20'ye çıktı (GERİLEME)** (ORTA); 0 kayıt dönen listeler de "kişisel veri erişimi" işaretli (DÜŞÜK, HATA); 10.400 kayıtta yalnız 100'er "Daha fazla göster", dışa aktarım yok (DÜŞÜK); "QA TestSAHIP" bitişik (DÜŞÜK). |
| 22 | `/veri-disa-aktarim` | Veri dışa aktarım | BULGU | 4xx/5xx YOK: `kaynaklar`, `gecmis`, POST `disa-aktarim` ve `/indir` 200. Önceki turun token döngüsü tekrarlanmadı. | 7 bulgu (0 KRİTİK). İki açıklama çelişiyor ("şifreli arşiv" vs "standart CSV") (ORTA); üç kapsam ham slug (ORTA); geçmiş satırında hiçbir eylem yok, "yeniden hazırla" kaçış yolu yok (ORTA); kapsam onay kutularının erişilebilir adı "on" — MASKESİZ anahtarında kritik (ORTA); arşiv parolası düz metin (DÜŞÜK); eski satırlarda SATIR "—" (DÜŞÜK); sayfa başlığı yok (DÜŞÜK). |
| 23 | `/kullanicilar` | Kullanıcılar | BULGU | 4xx/5xx YOK: GET 200, POST 201, PATCH `/{id}` ve `/{id}/rol` 200. **"Sahipliği Devret" akışı doğrulandı ama GÜVENLİK GEREĞİ ONAYLANMADI (geri dönülemez).** | 5 bulgu (0 KRİTİK). Yeni Kullanıcı diyaloğu açılır açılmaz üç hata metniyle geliyor (DÜŞÜK); şubesiz kullanıcı oluşturulabiliyor, "—" açıklanmıyor (DÜŞÜK); pasife almada bant yok, aktifleştirmede onay yok (DÜŞÜK); arama/filtre yok (DÜŞÜK); kendi satırındaki kilit metni yeni "Sahipliği Devret" yolunu söylemiyor (DÜŞÜK). |
| 24 | `/yetkiler` | Yetkiler | BULGU | 4xx/5xx yok: GET `yetkiler` 200, PUT `yetkiler` 200 ×2. Değiştirilen yetki eski hâline döndürüldü ve ölçümle doğrulandı. | 3 bulgu (0 KRİTİK). 22 sütunluk matris 1280px'te sığmıyor (tablo scrollWidth 2119 / kap 990) (ORTA); matris 9 ekranı kapsamıyor (Ürün Ağacı, Etiketler, POS Hakedişleri, Vade Takvimi, Gider Pusulaları, Ayarlar, İşletme, Yetkiler, Kılavuz) (ORTA); "Eksik olan: değiştirilmiş bir yetki yok" ifadesi kafa karıştırıcı (DÜŞÜK). |
| 25 | `/isletme` | İşletme | **ölçülmedi** | **ölçülmedi** | **ölçülmedi** — grup 8 çıktısı bu özete ulaşmadı (GİRDİ KESİLDİ). Tahmin yapılmadı. |
| 26 | `/ayarlar` | Ayarlar (tema, mod) | BULGU | Bu rotada 4xx YOK: `auth/ben`, `mfa/durum`, `meta/surum` 200; POST `mfa/sifirla` 200; PATCH `kullanicilar/ben` 200 ×2 (değişiklik geri alındı). | 3 bulgu (0 KRİTİK). Yayın damgalı derlemede künye "Ortam: Geliştirme" gösteriyor (ORTA, HATA); EN arayüzde rol çevrilmiyor ("Role SAHIP") (DÜŞÜK); "Yarım kaydı temizle" başarı bandı üretmiyor (DÜŞÜK). |
| 27 | `/kilavuz` | Kılavuz | BULGU | Yüklemede 4xx/5xx YOK: `/kilavuz` 200, `/kilavuz.html` 200. (Tur 1'in CSP/iframe KRİTİĞİ kalıcı olarak kapalı.) | 4 bulgu (0 KRİTİK). Kılavuzda uygulamaya dönüş bağlantısı yok (ORTA); uygulama içi bölüm listesi 13/27 bölümü kapsıyor (ORTA); `kilavuz.html` oturumsuz herkese açık, 200 / 60.885 bayt (DÜŞÜK); rota sayfası neredeyse boş, açıklama iki kez (DÜŞÜK). |

## Ek rotalar — tur 1 iskeletinde olmayan, tur 7'de gezilen 15 sayfa

| # | Rota | Durum | Konsol/ağ | Bulgular |
|---|---|---|---|---|
| E1 | `/cikti/{Tur}/{Id}` | BULGU (ilk kez test edildi) | Sayfanın kendi istekleri temiz (belge 200); konsoldaki 2 adet 404 QA'nın kendi uç yoklamalarıdır. | 4 bulgu. Kalem tutarı ham "271200.00" basılıyor, aynı sayfada toplam doğru (YÜKSEK, GÖRSEL); müşteri fişinde has gram eksi işaretli ve ondalık tutarsız (ORTA); H2 H1'den önce ve başlık iki kez (DÜŞÜK); dar ekranda belge tablosu kendi içinde taşıyor (DÜŞÜK). |
| E2 | `/pos/tahsilatlar` | BULGU | 4xx/5xx yok; önceki turdaki 404'lü `oto-eslestir-onerisi` artık 200. | 8 bulgu. "Banka yatmasıyla eşleştir" hareket seçilse de kilitli, ipucu "elle eşleştirin" diyor ama o yol yok — çıkmaz (YÜKSEK); adaylar yön süzmüyor, POS girişine yalnız eksi hareketler öneriliyor (ORTA); tutarla arama sonuç vermiyor (ORTA); tarih kutularının etiketi yok (ORTA); H1 yok, açıklama iki kez, terminoloji karışık, karakter sayacı yok (4×DÜŞÜK). |
| E3 | `/pos` | BULGU | 4xx/5xx YOK; doğrulama hatalarında hiç istek üretilmiyor (istemci kilidi doğru çalışıyor). | 6 bulgu. Komisyon oran olarak giriliyor, yüzde olarak gösteriliyor — 100 kat hata riski (ORTA); doğrulama mesajı yardım metnini tekrarlıyor (ORTA); Taksit Oranları yalnız düzenlemede (DÜŞÜK); CİHAZ SAYISI pasifleri sayıyor (DÜŞÜK); başlık iki kez (DÜŞÜK); POST 201 yerine 200 (DÜŞÜK). |
| E4 | `/evraklar` | BULGU | GET 200, POST 201, PATCH 200; bu turda 4xx/5xx yok. | 7 bulgu. **Negatif tutarda doğrulama mesajı YANLIŞ: dolu alana "Bu alan zorunludur" diyor (YÜKSEK, HATA);** PATCH 200 sonrası başarı bandı yok (ORTA); kilit hangi alanın eksik olduğunu söylemiyor (ORTA); **890px'te yatay taşma 943 → 1208 px'e çıktı (GERİLEME, ORTA);** boş durum süzgeci yansıtmıyor (DÜŞÜK); evrak türü kilidi gerekçesiz (DÜŞÜK); ISO tarih (DÜŞÜK). |
| E5 | `/evraklar/takvim` | BULGU | İki takvim çağrısı da 200. | 4 bulgu. Adına rağmen takvim yok, satırlar tıklanabilir görünmüyor (ORTA); varsayılan aralık portföydeki hiçbir evrakı kapsamıyor, ekran boş açılıyor (ORTA); ISO tarih (DÜŞÜK); "Düzeltme şöyle yapılır:" cümlesi bağlantı etiketiyle bitiyor (DÜŞÜK). |
| E6 | `/kurulum` | BULGU | Yeni 4xx/5xx yok; POST `kurulum/acilis/prova` ×2 → 200. **"Açılışı uygula" ve "Kurulumu mühürle" geri dönülemez olduğu için KASITLI OLARAK çalıştırılmadı.** | 6 bulgu. Prova HATA döndüğünde kilit gerekçesi yanlış ("Kuru prova çalıştır") (ORTA); kabul edilmeyecek döviz kasası listede sunuluyor (ORTA); "Eksik" adımlara bağlantı yok (ORTA); sayfa başlığı yok (DÜŞÜK); H1 yok ve başlık iki kez (DÜŞÜK); adım satırı bitişik okunuyor (DÜŞÜK). |
| E7 | `/veri-ice-aktarim` | BULGU | POST `prova` 400 (kasıtlı geçersiz kolon) → 200; `uygula` 200; `geri-al` 200. Beklenmeyen 4xx/5xx yok. | 7 bulgu. Uygulandıktan sonra kuru prova özeti eskimiş kalıyor ("Yazılan kayıt: 0") (ORTA, HATA); başlık/açıklama iki kez (DÜŞÜK); hata metninde ham API yolu (DÜŞÜK); "Düzeltme şöyle yapılır:" düğme adıyla bitiyor (DÜŞÜK); geri alma diyaloğunun başlığı eylemi adlandırmıyor (DÜŞÜK); sayfa başlığı yok (DÜŞÜK); "37/20 karakter" sayacı asgariyi azami gibi gösteriyor (DÜŞÜK). |
| E8 | `/vitrin` | HATA (ilk kez test edildi) | `pano/guncel`, `pano/ayarlar` 200. **Sorun tersine: geçersiz veri 400 ALMIYOR, 200 ile kabul ediliyor.** | 4 bulgu (**1 KRİTİK**). Pano marjında doğrulama yok: −999.999 TL kabul edildi ve müşteriye dönük panoda Gram Altın alışı ₺0,00 oldu (**KRİTİK, HATA** — test sırasında marj 0'a çekilerek onarıldı); anormal fiyatta hiçbir uyarı yok (YÜKSEK); etiket boş bırakılabiliyor, ipucu yok (DÜŞÜK); panoya yeni/özel kalem eklenemiyor (ORTA). |
| E9 | `/raporlar/satis-analizi` | BULGU | `odeme-araci` 200, `personel` 200, ters aralıkta `personel` → 400 (istemci geçersizi yine de gönderiyor), `sube-karsilastirma` 200. | 6 bulgu. Sayfa açıklaması yanlış (Rapor Merkezi metni) (ORTA); NAKİT kartı TL tutarını farklı para birimlerinin hareket sayısıyla birleştiriyor, çıkışı hiç göstermiyor (ORTA, HATA); yüzde nokta ondalıklı (DÜŞÜK); tarih etiketi yok, ters aralıkta eski tablo kalıyor (DÜŞÜK); analiz sayfasında grafik yok (DÜŞÜK); dışa aktarma/yazdırma yok (DÜŞÜK). |
| E10 | `/raporlar/islem-defteri` | HATA | `islem-defteri` 200; ters aralık → 400 (kasıtlı). Başka 4xx/5xx yok. Rakamlar tutarlı (SATIS süzgeci 7 satır = ₺535.650,00 = özet GİRİŞ). | 6 bulgu. Sayfa açıklaması yanlış (ORTA, HATA); SAYIM_DUZELTME ve EVRAK_GIRIS listede var ama süzgeçte yok (ORTA, HATA); ham enum gösterimi (ORTA); **YÖN sütunu hem tür adıyla hem hareketin işaretiyle çelişiyor (EMANET_CIKIS → "Giriş", −5.496 g storno → "Giriş", EVRAK_GIRIS → "Çıkış") (ORTA, HATA);** tarih etiketi yok, ters aralıkta eski tablo kalıyor (DÜŞÜK); "EMANET_GIRISStorno" bitişik (DÜŞÜK). |
| E11 | `/siparisler` | BULGU (**GİRDİ KESİLDİ — sayfa çıktısı eksik geldi**) | POST `siparisler` 400 (kasıtlı geçersiz ayar "22") → 200 (S-2026-000006); güncelleme ve durum değişimi başarılı; başka 4xx/5xx yok. | En az 2 bulgu (kesik çıktıda görünen): Kalem "Ayar" alanı serbest metin, kabul edilen biçim yazmıyor, hata sunucudan 400 ile dönüyor (ORTA); sipariş düzenlemede cari, tür ve şube değiştirilemiyor (ORTA, DÜZENLENEBİLİRLİK). Kalan bulgular bu özete ulaşmadı. |
| E12 | `/siparisler/{Id}` | BULGU | GET 200, POST `gecis` 200 ×2, PATCH 400 (kasıtlı geçersiz ayar) → 200. Test sonunda durum eski hâline alındı. | 4 bulgu (**1 KRİTİK**). Cari, tür, şube ve tahmini tutar TASLAK durumunda bile düzenlenemiyor (**KRİTİK, DÜZENLENEBİLİRLİK**); Ayar alanı serbest metin, hatalı alan işaretlenmiyor (ORTA); main içinde başlık ve listeye dönüş bağlantısı yok (DÜŞÜK); gram nokta ondalıklı (DÜŞÜK). |
| E13 | `/vezne` | HATA | Kuyruk/fiş/iptal uçlarının hepsi 200; beklenmeyen 4xx/5xx yok. Test fişi gerekçeli iptal edildi. | 5 bulgu. Kuyruk ucu `toplamTl` hesaplamıyor: ekranda ₺0,00, detayda ₺2.150 (sonra ₺3.200) (YÜKSEK, HATA); fiş açılınca süzgeç değişmediği için kayıt kaybolmuş görünüyor (ORTA); eski doğrulama mesajı kalıyor (DÜŞÜK); ham ULID (DÜŞÜK); main içinde başlık yok (DÜŞÜK). |
| E14 | `/yedekler` | HATA | Sayfa istekleri 200, POST `yedekler/talep` 201, PATCH 200. Bu turda 4xx/5xx yok. | 9 bulgu. **RPO taahhüdü 13 kat aşılmış (4687 dk / 360 dk), gösterge nötr renkte ve son yedek 3 gün önce (YÜKSEK, HATA — DevOps'a eskale edilmeli);** "şimdi doğrula" eylemi yok (ORTA); başarısız koşu/uyuşmayan tatbikat için sebep yok (ORTA, HATA); talep sonrası bant yok (ORTA); iptal diyaloğunda iki düğme de "iptal" (ORTA); ham enum (ORTA); başlık iki kez (DÜŞÜK); "değiştirilemez" kutusu her satırda (DÜŞÜK); sayfa başlığı yok (DÜŞÜK). |
| E15 | `/bildirimler` | BULGU | 4xx/5xx yok; PATCH `isler/ayarlar` 200, POST `SAKLAMA_ONIZLEME/calistir` 200. Ayarlar "Varsayılana dön" ile geri alındı. | 5 bulgu. "Şimdi çalıştır" bandı görünmüyor (top −431 px; önceki tur −107 → **GERİLEME**) (ORTA); `kur/guncel` 80 istek (ORTA, PERFORMANS, ama 401 yok); "Tümü" sekmesinde kapalı bildirim metni kalıyor (ORTA); main içinde başlık yok (DÜŞÜK); "Yenile" sessiz (DÜŞÜK). |

## Çapraz kontroller — TUR 7
- [x] **Karanlık tema (B-32 gerilemesi) — TEMİZ.** 41/41 rotada gövde `rgb(11,18,32)` / metin `rgb(232,238,246)`; ölçülen vurgu örnekleri: hata bandı bg `rgb(42,20,20)` / fg `rgb(248,113,113)`, başarı bandı bg `rgb(16,41,31)` / fg `rgb(52,211,153)`, rozet-amber bg `rgb(42,35,19)` / fg `rgb(252,211,77)`. "Açık zemin üstünde açık metin" vakası hiçbir rotada bulunmadı.
- [x] **Dar ekran 375px (P-18 / BK-24) — 41/41 TEMİZ.** Örnekleme yapılmadı, her rota tek tek yeniden yüklenip ölçüldü: `document.documentElement.scrollWidth = 375`. Geniş tablolar (iç genişlik 669–1906 px) kendi kaydırma kabında kalıyor. Tur 1'in 27/27 başarısız kontrolü tamamen kapandı. **İki istisna kaydı (sayfa değil, öğe düzeyinde):** `/urunler` süzgeç satırı 375px'te kırpılıyor ve kaydırılamıyor; `/cikti/{Tur}/{Id}` belge tablosu kendi içinde kayıyor.
- [ ] **Ara genişlik (768–1024) — BAŞARISIZ ve GERİLEDİ.** `/evraklar` 890px'te `scrollWidth 1208 > clientWidth 890` (tur 6: 943). Diğer rotalar bu aralıkta adım bütçesi nedeniyle ölçülmedi — sonraki turda 768–1024 ayrıca taranmalı.
- [ ] **Her kaydet akışında başarı bandı — BAŞARISIZ (tekrarlayan kök neden).** Bant üretmeyenler: `/evraklar` güncelleme (PATCH 200), `/gider-pusulalari` güncelleme (PATCH 200), `/faturalar` oluşturma+iptal, `/yedekler` talep (POST 201), `/banka` satır içi kayıt, `/ayarlar` MFA temizleme, `/bildirimler` Yenile, `/` Kuru güncelle. Bant üretenler: `/evraklar` yeni kayıt, `/emanet`, `/islemler`, `/kasalar`, `/vezne`, `/ayarlar` profil. Aynı üründe iki farklı sözleşme var.
- [ ] **Satır içi düzenlemede Enter — BAŞARISIZ (tek bileşen, üç ekran).** `si-*` kimlikli satır içi düzenleyicide Enter hiçbir şey yapmıyor: `/sayim`, `/banka`, `/cariler`. Düğmenin kendi `title`'ı "Enter kaydeder" sözünü veriyor. `/sayim`'da kaydetme düğmesi de olmadığı için barkod okuyucu yolu fiilen kapalı.
- [x] Basit / profesyonel mod anahtarı — tüm rotalarda görünür ve çalışıyor (yalnız `/gider-pusulalari/{Id}/belge` gibi belge rotalarında istenmiyor, bulgu olarak yazıldı).
- [x] Oturum: 7 grubun 7'sinde gerçek ekran girişi; token enjeksiyonu yedek yoluna hiç düşülmedi. Önceki turun "3× yenile/çıkış döngüsü" ve `kar-zarar` ilk yükleme 401'i bu turda TEKRARLANMADI.

## Tur 6 → Tur 7 karşılaştırması

| Ölçüt | Değer |
|---|---|
| Tur 7'de yeniden denenen tur 6 bulgusu | **161** (grup 2–7 sayfalarının `onceki_bulgular` alanları; grup 1 sayfaları `/cikti` dışında önceki bulgu listesi taşımıyor) |
| **KAPANDI** | **20** (%12,4) |
| **KISMEN** | **12** (%7,5) |
| **AÇIK KALAN** | **129** (%80,1) |
| Tur 7'nin toplam bulgusu | 211 |
| Bunlardan **yeni bulgu** (önceki tur listesinde izi olmayan) | **~70** — 211 bulgudan 141'i önceki turun AÇIK/KISMEN kalemlerinin yeniden raporlanmasıdır; kalan ~70 bulgu grup 1'in izlenmeyen 38 bulgusu ile ilk kez test edilen `/cikti`, `/vitrin`, `/siparisler/{Id}` rotalarından gelir. (Tam eşleme yapılamadı çünkü grup 1 ve üç rota `onceki_bulgular` alanı taşımıyor — tahmin değil, ölçülen aralık: 70 ± grup 1'in tekrar payı.) |

**KAPANAN 20 bulgu (kanıtlı):** `/kullanicilar` SAHİP rolü verilemiyor → "Sahipliği Devret" akışı eklendi · `/kullanicilar` mobil taşma 891→375 · `/cariler` pasif cariler satış listesinden süzüldü · `/cariler` mobil 998→375 · `/urunler` mobil 875→375 · `/urunler/agac` kategori pasife alma eklendi · `/urunler/agac` mobil 752→375 · `/raporlar/kar-zarar` ilk yükleme 401'i · `/islemler` mobil 572→375 · `/vezne` Düzenle'ye "İşçilik (TL)" alanı eklendi · `/gider-pusulalari` detay kipi açıkken arka plan düğmelerinin sessiz etkisizliği · `/faturalar` ilk "Kaydet" basışının sessiz kalması · `/faturalar` mobil 1182→375 · `/denetim-izi` aynı saniyede yinelenen satır · `/ayarlar` yarım kalan MFA kurulumu temizlenemiyor · `/ayarlar` şifre bölümünde eski hata metninin kalması · `/bildirimler` zamanlanmış iş ayarları düzenlenemiyor → tam ayar formu eklendi · `/giris` 404 ile kullanıcı varlığı ifşası → 401 + genel mesaj · `/` WebSocket/`access_token` sorgu dizesi · `/uyum` GONDERILDI bildirimde yol yok → "Ek beyan düzenle" eklendi (+ `/uyum` serbest metin işlem kimliği arama seçicisine çevrildi).

**GERİLEYEN 4 nokta (Madde 1 — gizlenmedi):**
1. `/denetim-izi` kayıt türü süzgecinde çevrilmemiş ham ad **14 → 20** (yeni eklenen 6 varlık türü de çevrilmemiş).
2. `/bildirimler` "Şimdi çalıştır" bandının görünürlüğü **top −107 px → −431 px** (sayfa uzadı).
3. `/evraklar` 890px yatay taşma **943 → 1208 px**.
4. `/cariler` satır eylem yığını: düğmeler üç sıraya sarılıyordu, artık **satır yüksekliği 329 px** ve 1280×800'de ekranda yalnız **2 satır** görünüyor.
5. `/yedekler` RPO aşımı **753 dk → 4687 dk** (ürün kusuru değil, işletim: yedek zamanlayıcısı 3 gündür koşmamış görünüyor — ayrıca eskale edilmeli).

**Düzenlenemeyen giriş listesi farkı:** tur 7'de **47** düzenlenemeyen veri girişi kalemi kayda geçti (tur 1: 26). Artış kapsam genişlemesinden (41 rota vs 27) ve artık her ekranın kilit gerekçesinin tek tek denetlenmesinden geliyor. Bunların çoğunluğu **ekranda yazılı gerekçe + düzeltme yolu taşıyan meşru kilitler** (storno, VUK md.234, KVKK md.11/12, MASAK, append-only denetim izi). **Gerekçesiz ya da gerekçesi gerçeği yansıtmayan kalemler:** `/cariler` aktif→pasif yolu yok (KRİTİK) · `/faturalar` TASLAK başlık ve kalemleri (KRİTİK) · `/siparisler/{Id}` cari/tür/şube/tahmini tutar (KRİTİK) · `/banka` dekont tarihi (ekran "düzeltilebilir" diye söz veriyor) · `/banka` dekont durumu "Bekliyor" · `/pos/tahsilatlar` banka eşleştirmesi fiilen yapılamıyor · `/sayim` sayılan adet (Enter yolu kopuk) · `/islemler` sepet kalemi adet/fiyat · `/vitrin` yeni pano kalemi · `/yedekler` talep ve iptal gerekçesi · `/veri-ice-aktarim` aktarım gerekçesi · `/urunler/etiketler` etiket basım işi · `/kur` manuel kur kaydı.

**Ortalama puan farkları:** görsel **3,63 / 5** (tur 1: 3,85 → −0,22), kullanıcı dostu **3,00 / 5** (tur 1: 3,07 → −0,07). Düşüş gerçek bir gerileme değil, kapsam farkıdır: tur 1'de olmayan 15 ek rotanın büyük kısmı (vitrin 3/2, evraklar/takvim 3/2, satis-analizi 3/3, yedekler 3/3, veri-ice-aktarim 3/3) ortalamayı aşağı çekiyor. Tur 1'le ortak 26 rotada tablo düzelme yönünde (ör. `/kilavuz` 1/1 → 3/3, `/fiyatlandirma` 4/2 → 4/3, `/faturalar` 3/2 → 3/2 sabit, `/cariler` — → 2/2 en düşük).

## Bulgu özeti — TUR 7 (2026-09-21)

### Sayılar
| Ölçüt | Değer |
|---|---|
| Gezilen rota | **41** (planlanan 44'ün 41'i; grup 8 = 3 sayfa ölçülmedi, GİRDİ KESİLDİ) |
| Toplam bulgu | **211** |
| KRİTİK | **6** |
| YÜKSEK | 16 |
| ORTA | 86 |
| DÜŞÜK | 103 |
| Ortalama görsel puanı | **3,63 / 5** (149 / 205) |
| Ortalama kullanıcı dostu puanı | **3,00 / 5** (123 / 205) |
| Düzenlenemeyen veri girişi kalemi | **47** (13'ü gerekçesiz/yanıltıcı gerekçeli) |
| Kendiliğinden oluşan konsol/ağ hatası | **YOK.** Turda görülen tüm 4xx'ler (401 giriş denemesi, 400 tevsik/IBAN/ayar/prova/ufuk/ters tarih, 404 belge) kasıtlı negatif testlerdir; her biri ilgili sayfanın konsol/ağ satırında tek tek sahiplendirildi. Tek istisna: grup 3 ve 5'in konsolunda oturum başından kalma 3×404 + 2×400 tampon kaydı görüldü, **kaynağı bu turda doğrulanamadı — varsayım yapılmadı**, sonraki turda ilk yükleme izlenmeli. |

### KRİTİK bulgular (6)
1. **`/vitrin` — Pano marjında hiçbir doğrulama yok.** Kanıt: "Alış marjı (TL)" = −999999 → sunucu **200**, ayar tablosu `-₺999.999,00`, **müşteriye dönük panoda Gram Altın alışı ₺0,00**. Öneri: sunucuda marjı makul aralıkla sınırla, fiyat ≤ 0 doğuran marjı 400 ile reddet; arayüzde kaydetmeden önce "yeni pano fiyatı" önizlemesi. *(Test sırasında marj 0'a çekilerek onarıldı; kalıcı hasar yok.)*
2. **`/emanet` — "Emanet Al" kipindeki AÇIKLAMA sessizce yutuluyor.** Kanıt: `#e-aciklama` doluyken kayıt oluştu, GET `/api/v1/emanet/{cariId}` → `"aciklama": null`; aynı alan satır içi PATCH ile yazılabiliyor. Öneri: oluşturma isteğine `aciklama` alanını ekle + regresyon testi (form açıklaması = GET'teki açıklama).
3. **`/faturalar` — TASLAK fatura ve kalemleri düzenlenemiyor.** Kanıt: taslak satır eylemleri yalnız `['Kalemleri Göster','Kes','İptal Et']`; düzeltmenin tek yolu iptal, iptal kipi "fatura numarası serisinde kullanılmış sayılır" diyor. Öneri: kilidi yalnız KESILDI/ODENDI durumuna uygula, TASLAK'a tam Düzenle ver.
4. **`/siparisler/{Id}` — Taslak siparişte cari, tür, şube ve tahmini tutar düzenlenemiyor.** Kanıt: Düzenle formunun tüm alanları `[gun, aciklama, kalem-tanim-0, kalem-ayar-0, kalem-gram-0, kalem-adet-0]`; yanlış müşteriye açılan sipariş ancak iptal + yeniden açma ile düzeliyor. Öneri: taslakta bu dört alanı da düzenlenebilir yap, değişikliği denetim izine yaz.
5. **`/cariler` — Aktif cari arayüzden pasife alınamıyor (üçüncü tur).** Kanıt: 9 aktif satırın hiçbirinde "Pasife Al" yok, Düzenle diyaloğunda Durum alanı yok; `/urunler`, `/pos`, `/kullanicilar` aynı akışa sahip. Öneri: `/urunler` ile birebir aynı onaylı akışı ekle.
6. **`/urunler` — "Pasife Al" onayının "yeni satışta seçilemez" sözü tutulmuyor.** Kanıt: pasife alınan ürün hemen ardından `/islemler` ürün listesinde (13 seçenek) seçilebilir; aynı süzgeç cari için eklenmiş, ürün için eklenmemiş (`GET /api/v1/urunler?limit=200` aktif süzgeci taşımıyor). Öneri: ürün sorgusuna `aktif=true`, sunucuda pasif ürünle satışı reddet.

### YÜKSEK bulgular (16 — rota · kanıt özeti · öneri)
- `/sayim` satır içi Enter kaydetmiyor (POST yok, blur ile 200) → Enter'ı blur ile aynı kaydetme yoluna bağla; barkod okuyucu yolu bu.
- `/sayim` DURUM hücresinde düz metin "İptal Et" (5 iptal oturumunun hepsinde) → durum rozeti bas.
- `/cariler` satır içi Enter kaydetmiyor; `title` sözü tutulmuyor → tek bileşen düzeltmesi üç ekranı çözer.
- `/cariler` satırda 7–8 düğme, satır yüksekliği 329px, ekranda 2 satır → "⋯ İşlemler" menüsü.
- `/urunler` 375px'te kategori/reyon süzgeçleri kırpılıyor (kap `overflow-x: visible`, scrollWidth 840 / client 351) → flex-wrap.
- `/cikti/{Tur}/{Id}` kalem tutarı ham `271200.00` (aynı sayfada toplam `₺271.200,00`) → tek biçimlendirici.
- `/pos/tahsilatlar` eşleştirme düğmesi kilitli, ipucu "elle eşleştirin" diyor ama o yol yok → kısmi/elle eşleştirme aç ya da ipucunu gerçeğe uydur.
- `/banka` ekran "tarih doğrudan düzeltilebilir" diyor, tarih alanı hiç yok → tarih alanı + satır içi düzenleme ekle.
- `/raporlar/kar-zarar` "Karşılaştır" sessizce etkisiz (istek 200, ekranda tek delta yok) → delta bas ya da seçeneği kapat.
- `/evraklar` negatif tutarda "Bu alan zorunludur" (alan dolu) → alan bazlı mesaj.
- `/yedekler` RPO 4687 dk / taahhüt 360 dk, gösterge nötr renkte, son yedek 3 gün önce → kırmızı + "TAAHHÜT AŞILDI" rozeti **ve DevOps eskalasyonu**.
- `/gider-pusulalari` "Hurda alımı gir" Satış sekmesini açıyor (üçüncü tur) → `?sekme=HURDA_ALIM`.
- `/faturalar` "Fişten Fatura Kes" araması 200 dönüp boş sonuç veriyor, oysa 4 faturasız fiş var → FIS dizinine fiş no/cari/tarih ekle, boşken son 10 fişi listele.
- `/vezne` kuyruk ucu `toplamTl` hesaplamıyor: listede ₺0,00, detayda ₺2.150/₺3.200 → kuyruk sorgusunu detayla aynı hesaba bağla.
- `/islemler` ürün listesi aktif süzgeci olmadan çekiliyor (cari listesi `aktif=true` ile) → asimetriyi gider.
- `/vitrin` sıfır/anormal pano fiyatında hiçbir uyarı yok → makul sınır kontrolü + "fiyat şüpheli" rozeti.

### Düzenlenemeyen veri girişleri (47 kalem, özet)
Meşru (ekranda gerekçe + düzeltme yolu yazılı): kasa/banka hareketi tutar-yönü (storno), satış fişi (iade/ters kayıt), POS hakediş satırı, emanet gram/ayar/cari (storno), gider pusulası satıcı/kimlik/tutar (VUK md.234), dışa aktarım geçmişi (KVKK md.11), denetim izi (KVKK md.12), MASAK şüpheli bildirimi (ek beyan yolu var), sipariş no/durum geçmişi (append-only), SAHİP rolü yetkileri, kullanıcı silinemez (pasife alınır), rapor rakamları.
Gerekçesiz / gerekçesi gerçeği yansıtmayan **13 kalem**: `/cariler` aktif→pasif · `/faturalar` TASLAK başlık · `/faturalar` TASLAK kalemleri · `/siparisler/{Id}` cari-tür-şube-tahmini tutar · `/banka` dekont tarihi · `/banka` dekont durumu · `/pos/tahsilatlar` banka eşleştirmesi · `/sayim` sayılan adet (Enter) · `/islemler` sepet kalemi adet/fiyat · `/vitrin` yeni pano kalemi · `/yedekler` talep/iptal gerekçesi · `/veri-ice-aktarim` aktarım gerekçesi · `/urunler/etiketler` etiket basım işi (+ `/kur` manuel kur kaydı: doğrudan düzenleme yok, ama kipte yönlendirme var).

### Görsel / kullanıcı dostu ortalamaları
- Görsel: **3,63 / 5** (41 sayfa, 149 puan). En yüksek: `/gider-pusulalari/{Id}/belge` **5**. En düşük: `/cariler` **2**.
- Kullanıcı dostu: **3,00 / 5** (41 sayfa, 123 puan). En düşük ikili: `/sayim` **2**, `/cariler` **2**; ayrıca `/vitrin`, `/evraklar/takvim`, `/faturalar`, `/denetim-izi` **2**.
- En zayıf sayfalar (görsel/kullanıcı dostu): `/cariler` 2/2 · `/vitrin` 3/2 · `/evraklar/takvim` 3/2 · `/faturalar` 3/2 · `/sayim` 4/2 · `/denetim-izi` 3/2. En güçlüler: `/gider-pusulalari/{Id}/belge` 5/4 · `/ayarlar` 4/4 · `/yetkiler` 4/4 · `/` 4/4 · `/cikti/{Tur}/{Id}` 4/4.

### Düzeltme paketine giren maddeler (tur 7 çıkışı)
**P1 — veri bütünlüğü / sessiz hata (hemen):**
1. `/vitrin` pano marjı sunucu doğrulaması + fiyat önizlemesi (KRİTİK).
2. `/emanet` oluşturma isteğinde `aciklama` alanının kaybolması + regresyon testi (KRİTİK).
3. `/vezne` kuyruk ucunda `toplamTl` hesaplanması (YÜKSEK).
4. `/urunler`+`/islemler` pasif ürün süzgeci (istemci `aktif=true` + sunucu reddi) (KRİTİK).
5. `/denetim-izi` bitiş tarihi off-by-one (gün sonuna kaydır + birim test).
6. `/raporlar/islem-defteri` YÖN sütununun hareketin işaretinden türetilmesi + birim test.
7. `/raporlar/kar-zarar` "Defter has gramı" tanımının aylık özetle eşitlenmesi.

**P2 — kurucu kuralı "her veri girişi düzenlenebilir":**
8. `/cariler` "Pasife Al" + onay akışı (KRİTİK, üçüncü tur).
9. `/faturalar` TASLAK için tam Düzenle (başlık + kalemler) (KRİTİK).
10. `/siparisler/{Id}` taslakta cari/tür/şube/tahmini tutar (KRİTİK).
11. `/banka` dekont tarihi (giriş alanı + satır içi düzenleme) ve durum değiştirme/mutabakat eylemi.
12. `/islemler` sepet kaleminde adet/birim fiyat satır içi düzenleme.
13. `/pos/tahsilatlar` elle/kısmi eşleştirme yolu.

**P3 — tek bileşen, çok ekran (en yüksek getirili):**
14. **Satır içi düzenleyici (`si-*`): Enter = blur kaydetme yolu** → `/sayim`, `/banka`, `/cariler` birden düzelir.
15. **Mesaj kuyruğu "son durum kazanır"**: alan düzeltilince `hatali` sınıfı + bandı temizle → `/sayim`, `/kasalar`, `/fiyatlandirma`, `/vezne`.
16. **Tek "işlem sonucu bandı" sözleşmesi** (oluştur/güncelle/iptal hepsinde ≥4 sn, belge numarasıyla) → `/evraklar`, `/gider-pusulalari`, `/faturalar`, `/yedekler`, `/banka`, `/ayarlar`, `/`, `/bildirimler`.
17. **Zorunlu alan işaretleme sözleşmesi** (`hatali` + `aria-invalid` + "Bu alan zorunludur") → `/sayim`, `/evraklar`, `/cariler`, `/urunler`, `/siparisler`, `/fiyatlandirma`.
18. **Enum → Türkçe etiket sözlüğü** (eşlenmemiş tür kalınca derleme uyarısı) → `/denetim-izi` (20 ham ad), `/raporlar/islem-defteri`, `/yedekler`, `/kur`, `/veri-disa-aktarim`.
19. **Tek `tr-TR` sayı/tarih biçimlendiricisi** (has 4 hane, TL 2 hane, tarih gg.aa.yyyy) → `/raporlar`, `/cikti`, `/evraklar`, `/evraklar/takvim`, `/siparisler`, `/fiyatlandirma`, `/raporlar/satis-analizi`.
20. **Paylaşılan kur servisi**: SignalR bağlıyken HTTP yoklamasını durdur → `/` (10 sn aralık), `/sayim` (13 istek), `/bildirimler` (80 istek).
21. **PageTitle eksikleri**: `/veri-disa-aktarim`, `/veri-ice-aktarim`, `/yedekler`, `/kurulum`, `/raporlar/kar-zarar`.

**P4 — düzen ve erişilebilirlik:**
22. `/evraklar` 768–1024 aralığı taşması (GERİLEME) + sonraki turda tüm rotaların 890px taraması.
23. `/cariler` satır eylem yığını → "⋯ İşlemler" menüsü (GERİLEME).
24. `/urunler` 375px süzgeç kırpılması (flex-wrap).
25. `/yetkiler` matris ekseninin çevrilmesi (4 sütun × 22 satır) + matrisin rota kayıt tablosundan türetilmesi.
26. `/bildirimler` başarı bandının sabit/kaydırmalı gösterimi (GERİLEME).
27. `/gider-pusulalari` "Hurda alımı gir" derin bağlantısı (üçüncü tur).
28. `/faturalar` FIS arama dizini.
29. Tarih süzgeçlerine görünür etiket: `/pos/tahsilatlar`, `/raporlar`, `/raporlar/satis-analizi`, `/raporlar/islem-defteri`, `/kur`.
30. H1 / main başlığı eksikleri: `/pos/tahsilatlar`, `/kurulum`, `/vezne`, `/bildirimler`, `/siparisler/{Id}`, `/cikti` (H2→H1 sırası).

**İşletime eskalasyon (ürün kusuru değil):** `/yedekler` RPO 4687 dk — yedek zamanlayıcısı 18.09.2026 00:04'ten beri koşmamış görünüyor; Batuhan'a (DevOps) düşer.

## Temizlik
**BU TURDA TEMİZLİK YAPILMADI — fikstür kiracısı KALICI olarak korundu.** Sonraki turlar ve kurucuya gösterilecek gerçekçi örnekler için `tenant_id 01M2NZ97WE0PC3EGQG0NZX3W74` ("QA-TEST", qa-test-3 hesabı) silinmedi; hiçbir DELETE çalıştırılmadı.

`SELECT COUNT(*) FROM tenants;` → **8**

Fikstür dosyası (`scratchpad/qa-fikstur.json`) `printf '' >` ile boşaltıldı. Parola/jeton hiçbir yere yazılmadı.

**Turda bırakılan, geri alınamayan QA verisi (Madde 1 — gizlenmiyor):**
- `/islemler` HAVALE ile kaydedilen ₺45.000 QA satışı (fiş `01M30YMSVS42A3DFPJ12D6BAJW`) — muhasebe gereği silinemez, iade/ters kayıt gerekir.
- `/banka` ₺360'lık "QA-TEST G1 T7 dekont DUZ" kaydı (silme/storno yetkisi kurucuya bırakıldı).
- `/evraklar` QA-TEST-G5T7-CEK-030 (₺3.250,40) — arayüzde evrak silme/storno yok.
- `/urunler/agac` "QA-TEST G3 T7 Kategori DUZ", `/fiyatlandirma` "QA-TEST G3 T7 Liste DUZ" (ikisi de aktif, silme yolu yok).
- `/kullanicilar` "QA7DUZ Kullanici", `/cariler` "QA-TEST G2T7 Musteri DUZENLENDI", `/pos` "QA-TEST G2T7 POS DUZENLENDI", `/urunler` "QA-TEST G2T7 Urun DUZENLENDI", 1 dışa aktarım geçmiş kaydı.
- `/gider-pusulalari` GP-2026-000002'nin adres ve mal açıklaması test değerleriyle kaldı (eskisi: adres "Üsküdar / İstanbul", mal "22 ayar kırık bilezik").
- `/ayarlar` yarım kalmış MFA kurulum kaydı temizlendi (durum artık "Kapalı" — doğru son durum, yine de bildiriliyor).

Geri alınanlar: `/sayim` oturumu iptal · `/yetkiler` değişikliği eski hâline döndürüldü ve doğrulandı · `/kasalar` bakiyesi storno ile ₺0,00 · `/vitrin` marjı 0'a döndürüldü · `/vezne` fişi iptal · `/emanet` hareketi stornolandı · `/faturalar` FTR-2026-000008 iptal · `/yedekler` talebi iptal · `/veri-ice-aktarim` aktarımı geri alındı · `/siparisler` ve `/siparisler/{Id}` durumları eski hâline alındı · `/kur` manuel kaydı gerekçeli iptal · `/bildirimler` iş ayarları varsayılana döndürüldü · `/ayarlar` profil adı geri alındı.

Proje kaynak kodu değiştirilmedi, git komutu çalıştırılmadı, derleme/test koşulmadı, 5198 portuna dokunulmadı.

---

## § DÜZELTME — TUR 7 RAKAMLARI YANLIŞ SAYILMIŞTI (2026-09-21, orkestratör)

Bu çizelgedeki özet rakamları **özet ajanı** saydı ve **eksik saydı**. Ham veriden
(`ham/qa-turu-7.json`, 44 rotanın tamamı) yeniden hesaplandı:

| Ölçü | Ajanın dediği | **HAM VERİDEN ÖLÇÜLEN** |
|---|---|---|
| Taranan sayfa | 41 | **44** |
| Toplam bulgu | 211 | **236** |
| KRİTİK | 6 | **6** (doğruydu) |
| Düzenlenemeyen veri girişi | 18 | **35 gerçek eksik** (+15 tanesi "tasarım gereği" gerekçeli, ham toplam 50) |
| Ortalama görsel | 3,63 | **3,64** |
| Ortalama kullanıcı dostu | 3,00 | **2,98** |

### Turlar arası DÜRÜST karşılaştırma (hepsi ham veriden, aynı ölçüyle)

| | tur 5 | tur 6 | **tur 7** |
|---|---|---|---|
| Taranan rota | 39 | 39 | **44** |
| Bulgu | 193 | 203 | **236** |
| KRİTİK | 1 | 1 | **6** |
| Düzenlenemeyen (gerçek eksik) | 43 | 34 | **35** |

**Düzenlenemeyen giriş sayısı DÜŞMEDİ** — 34'ten 35'e çıktı (tur 6'da 39, tur 7'de 44 rota
tarandığı hâlde yatay kaldı). Daha önce "37 → 18 düştü" diye rapor edilen rakam **yanlıştı**.

### KRİTİK 1 → 6: bu bir çöküş DEĞİL, dördü zaten oradaydı

| Kritik | Tur 6'da | Ne oldu |
|---|---|---|
| `/faturalar` taslak fatura düzenlenemiyor | KRİTİK | **Hâlâ açık** — taşındı |
| `/cariler` aktif cari pasife alınamıyor | YÜKSEK | **Ciddiyet yükseldi** (üçüncü tur hayatta) |
| `/urunler` "pasife al" sözü tutulmuyor | YÜKSEK | **Ciddiyet yükseldi** |
| `/emanet` açıklama sessizce yutuluyor | yok | **GERÇEKTEN YENİ** |
| `/vitrin` pano marjında doğrulama yok | *taranmadı* | Yeni kapsam |
| `/siparisler/{Id}` taslak sipariş düzenlenemiyor | *taranmadı* | Yeni kapsam |

Tur 6 **beş rotayı sessizce atlamıştı** (`/cikti/{Tur}/{Id}` · `/gider-pusulalari/{Id}/belge` ·
`/vitrin` · `/siparisler/{Id}` · `/banka/ekstre/{IthalatId}`); betikteki yorum "3 rota" diyordu,
gerçek **5**. Tur 7 hepsini taradı — artıştaki iki kritik buradan geliyor.

### Kök neden ve kalıcı düzeltme

Kök neden, tur 4'te de ısıran hatanın aynısı: **ajana büyük bir JSON verip "say" demek.**
`betikler/qa-turu.js` düzeltildi — sayılar artık **betikte, ham veriden** hesaplanıyor
(`OLCUM` nesnesi), özet ajanına **dayatılıyor** ve ajan kendi sayısını yazsa bile betiğin
sayısı üzerine yazıyor. Ajan bundan sonra yalnız **anlatır**, saymaz.
