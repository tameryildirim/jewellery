# Tarayıcı QA turu — TUR 8 (uygulama içi tarayıcı, http://localhost:5199)

> Yöntem tur 1 ile aynı: (1) rotaya git, (2) konsol/ağ hatalarını oku, (3) erişilebilirlik ağacını oku, (4) her kip/formu aç, doğrulama mesajlarını tetikle, `QA-TEST-` önekli kayıt oluştur → düzenle → (varsa) pasife al/storno, (5) karanlık tema + 375px, (6) grafik (getBBox) ve mobil taşma ölçümü. Sayılar betikle ölçülmüştür (tur 7'de ajan sayımı hatalıydı), anlatı ve önceliklendirme bu belgeye aittir.

**Tur 8 kapsamı: 46 rota / 8 paralel grup.** Aşağıdaki tablo tur 8 sonuçlarıdır. Tur 1 dosyasına dokunulmamıştır (tarihsel taban).

| # | Rota | Durum | Konsol/ağ | Bulgular (kısa) |
|---|---|---|---|---|
| 1 | `/cikti/{Tur}/{Id}` | ENGELLENDİ | 2× `_framework` .wasm 404 + SRI; "Failed to start platform" | Uygulama hiç önyüklenmedi (KRİTİK). Tur 7'nin 4 bulgusu doğrulanamadı, ihtiyatla AÇIK. |
| 2 | `/kasalar` | ENGELLENDİ | Aynı önyükleme çökmesi | Rota test edilemedi; 8 eski bulgu doğrulanamadı. |
| 3 | `/sayim` | ENGELLENDİ | Aynı; hiç API çağrısı yok | 9 eski bulgu doğrulanamadı (13× `kur/guncel` ölçülemedi). |
| 4 | `/banka` | ENGELLENDİ | Aynı; curl ile de 404/0 bayt | 6 eski bulgu doğrulanamadı. |
| 5 | `/pos/tahsilatlar` | ENGELLENDİ | Aynı | 8 eski bulgu doğrulanamadı. |
| 6 | `/yetkiler` | ENGELLENDİ | Aynı | 3 eski bulgu doğrulanamadı; 22 sütunlu matris ölçümü tekrarlanamadı. |
| 7 | `/kullanicilar` | BULGU | 4xx/5xx yok; POST 201, PATCH 200, DELETE 200 | Diyalog açılışta hata metinleriyle geliyor; pasife alma/aktifleştirme asimetrik; arama/filtre yok; kilit metni "Sahipliği Devret"i söylemiyor. |
| 8 | `/kilavuz` | HATA | `GET /kilavuz.html` → **404, 0 bayt** | **KRİTİK gerileme:** kılavuzun tamamı yayında yok, düğme ve 14 bağlantı ölü; liste elle yazıldığı için sağlıklı görünüyor. |
| 9 | `/cariler` | BULGU | 4xx/5xx yok | Satırda 8 düğme, satır yüksekliği 305 px (YÜKSEK); pasif caride tüm eylemler açık; durum değişiminde bant yok; aria-label eksik. |
| 10 | `/pos` | BULGU | 4xx/5xx yok; POST 200 (201 değil) | Komisyon oranı giriş/gösterim birimi farklı (100 kat risk); doğrulama yardım metnini tekrarlıyor; taksit oranları yalnız düzenlemede; cihaz sayacı pasifleri sayıyor. |
| 11 | `/veri-disa-aktarim` | BULGU | 4xx/5xx yok | İki çelişik açıklama; 6 ham kapsam anahtarı; arşiv parolası açık metin; yeniden indirme yok; sayfa başlığı yok. |
| 12 | `/urunler` | BULGU | 4xx/5xx yok; POST 201 | Açıklama alanının etiketi sayfa alt başlığı (erişilebilirlik); stok adedi sütunu yok; stok kipinde ham ULID; metin bitişiklikleri; durum süzgeci yok. |
| 13 | `/urunler/agac` | BULGU | 4xx/5xx yok | Düzeltilen alanda hata işareti kalıyor; 3 aynı adlı "+ Yeni Kategori"; düzenle kipinde kayıt adı yok; H1 yok. |
| 14 | `/raporlar/kar-zarar` | HATA | 4xx/5xx yok; aynı URL 2× çağrılıyor | **DEFTER HAS GRAMI altın+gümüşü topluyor (YÜKSEK)**; "Karşılaştır" seçimi sessizce etkisiz (YÜKSEK); defter has gramı açıklamasıyla çelişiyor; hiç grafik yok. |
| 15 | `/fiyatlandirma` | BULGU | 4xx/5xx yok | Pasife alma onaysız; hata metni düzeltmeden sonra kalıyor; riskli değer onayı gereksiz isteniyor; `<form>` yok (Enter çalışmıyor); kip role=dialog değil. |
| 16 | `/kurulum` | HATA | Prova 200 (iş kuralı reddi gövdede) | **KRİTİK:** geri dönülemez "Kurulumu mühürle" açılış provasıyla aynı gerekçe alanına bağlı, onay kipi yok. Ayrıca kilit gerekçesi yanlış; kabul edilmeyen döviz kasası listede; "Eksik" adımlara bağlantı yok. |
| 17 | `/evraklar/takvim` | BULGU | Kasıtlı 400 (366 gün sınırı) | Adı takvim ama ızgara yok; 366 gün sınırı ancak sunucudan öğreniliyor; "Düzeltme şöyle yapılır" cümlesi yarım; boş durum olmayan düğmeyi tarif ediyor. |
| 18 | `/islemler` | BULGU | Kasıtlı 400 (tevsik), sonra 200 | Başarı bandı fiş numarasıyla ~5 sn'de siliniyor (YÜKSEK); ürün listesinde stok yok; "(isteğe bağlı)" birim fiyat fiilen zorunlu; ULID fiş no; nokta/virgül karışık. |
| 19 | `/gider-pusulalari/{Id}/belge` | BULGU | 200; kasıtlı 404 denemesi | Belgeden düzeltme/iptal yolu yok; eksik zorunlu alanla Yazdır engellenmiyor; PDF yok; "← Geri" düğme; belgenin etrafında uygulama kabuğu. |
| 20 | `/vitrin` | BULGU | PATCH 400 (kasıtlı), 200, POST 200 | Özel pano kalemi eklenemiyor; "Varsayılana dön" onaysız; bant eski adı kullanıyor; kiosk/tam ekran kipi yok. **Tur 7'nin KRİTİK marj bulgusu KAPANDI.** |
| 21 | `/veri-ice-aktarim` | BULGU | Kasıtlı 400, sonra 200'ler | Uygulama sonrası prova özeti eskimiş kalıyor; başlık 2×, "Ne aktarılacak" 3×; ham API yolu; "44/20 karakter" sayacı; 0 KB dosya boyutu. |
| 22 | `/siparisler/{Id}` | BULGU | POST 200; kasıtlı PATCH 400, sonra 200 | **KRİTİK:** taslakta bile cari/tür/şube/tahmini tutar düzenlenemiyor. **YÜKSEK:** düzenleme formunda ürün seçici hiç yok. Ayar serbest metin; dönüş bağlantısı yok. |
| 23 | `/vezne` | HATA | POST 200; kasıtlı PATCH 400, sonra 200 | **YÜKSEK:** kuyruk "Hazırlanıyor" fişleri ₺0,00 gösteriyor (detayda 3.200 ₺). Süzgeç yüzünden kayıt kaybolmuş görünüyor; eski doğrulama mesajı kalıyor; ULID. |
| 24 | `/raporlar` | HATA | 4xx/5xx yok | **YÜKSEK:** Cari Yaşlandırma has gram bakiyesini hiç göstermiyor (-16,946 has gizli). Gün sonu farkında düzeltme bağlantısı yok; dönemden bağımsız notu eksik; 3 farklı sayı biçimi. |
| 25 | `/raporlar/satis-analizi` | BULGU | 4xx/5xx yok (ters aralıkta kasıtlı 400) | Sayfa açıklaması Rapor Merkezi metni; kart yalnız GİRİŞ'i gösteriyor; ters aralıkta eski tablo kalıyor; grafik/dışa aktarım yok. |
| 26 | `/yedekler` | HATA | 4xx/5xx yok | **KRİTİK:** talep gerekçesi düzenlenemiyor. **YÜKSEK:** RPO taahhüdü 22 kat aşılmış (7985/360 dk) ve gösterge nötr. Doğrula eylemi yok; hata nedeni yok; başarı bandı yok; ham enum. |
| 27 | `/gider-pusulalari` | HATA | PATCH 200; ters aralık 200 (engellenmiyor) | **KRİTİK:** listede Düzenle yok, düzenleme Detay kipinde gizli. **YÜKSEK:** "Hurda alımı gir" bağlantısı Satış sekmesini açıyor. Çelişkili "değiştirilemez" metni; kayıt sonrası bant yok. |
| 28 | `/faturalar` | HATA | Kasıtlı 400'ler + kuyum PATCH 400 | **KRİTİK×2:** kuyum (özel matrah) taslağı "Düzenle" ile hiç kaydedilemiyor; düzenleme kipinde Cari listesi 0 seçenek. **YÜKSEK:** negatif tutar istemcide engellenmiyor. Bant yok; çelişkili iptal metni; KDV oranı etiketi tutarsız. |
| 29 | `/evraklar` | HATA | POST 201, PATCH 200 | **YÜKSEK:** negatif tutarda mesaj yanlış ("Bu alan zorunludur"). Evrak türü kilidi gerekçesiz; kilit hangi alan demiyor; süzgeç boş durumu çelişkili; ISO tarih. **890px taşması KAPANDI.** |
| 30 | `/denetim-izi` | BULGU | 4xx/5xx yok | **YÜKSEK (performans):** kayıtların %78,8'i otomatik kur anlık görüntüsü. 54 türün 17'si ham kod; "QA FrontendSAHIP"; 0 kayıtlı listede 🔐; sayfalama/dışa aktarım yok. |
| 31 | `/ayarlar` | BULGU | 4xx/5xx yok; PATCH 200 | Konsolda 51 Blazor WASM hatası (ORTA); EN arayüzde rol çevrilmiyor. **"Ortam: Geliştirme" bulgusu KAPANDI.** |
| 32 | `/raporlar/vade-takibi` | BULGU | Kasıtlı 400 (ufuk -1) | **GERİLEME:** ufuk alanı type=text oldu, min kayboldu; -1 yine sunucuya gidiyor. Sıfır bakiyeli cari "gecikmiş" tablosunda; geçersiz süzgeçte tüm kartlar kayboluyor; aria-pressed yok. |
| 33 | `/emanet` | BULGU | 4xx/5xx yok; POST/PATCH 200 | Storno ters kaydı yeşil "Giriş" rozetiyle (ORTA); ham gram/ayar/taş geri okunamıyor; Enter kaydetmiyor; devre dışı düğmelerin sebebi yok. **Açıklama kaybı (KRİTİK) KAPANDI.** |
| 34 | `/raporlar/islem-defteri` | HATA | Kasıtlı 400 (ters aralık) | Açıklama yanlış (Rapor Merkezi metni); **YÖN sütunu işaretle çelişiyor**; süzgeçte olmayan türler (EVRAK_GIRIS); ham enum; geçersiz aralıkta eski veri kalıyor. |
| 35 | `/bildirimler` | BULGU | 4xx/5xx yok; PATCH/POST 200 | "Şimdi çalıştır" bandı -600 px yukarıda, görünmüyor; **`kur/guncel` 154 kez çağrıldı** (ORTA, performans); sekme değişince açıklama güncellenmiyor; Yenile sessiz. **H1 eklendi (KAPANDI).** |
| 36 | `/urunler/etiketler` | BULGU | 4xx/5xx yok | Sonuçsuz aramada boş durum yok; liste boşalsa da 30 etiket seçili kalıyor (yanlış basım riski); etiket işi kaydedilemiyor; "abc" için yanıltıcı mesaj. Barkod SVG'leri sağlam çiziliyor. |
| 37 | `/uyum` | BULGU | 4xx/5xx yok; POST başarılı | **YÜKSEK:** "İşlem tutarı (isteğe bağlı)" fiilen zorunlu, boşken Kaydet hiç açılmıyor ve hata basılmıyor. Başarı bantlarında role=alert yok; "değiştirilemez" bloğu her satırda; saklama sayıları kart dilinde değil. |
| 38 | `/kur` | BULGU | 4xx/5xx yok; `/api/v1/kur` 107 istek | Geçmiş otomatik yoklamayla doluyor, sayfalama yok; 100 "İptal et" düğmesinin tamamı pasif ve gerekçesiz; ham kaynak adları; "MANUALGeçersiz"; iptal kipinde "Storno gerekçesi" etiketi; **107 istek (ORTA, performans)**. |
| 39 | `/siparisler` | BULGU | Kasıtlı 400 (ayar "22"), sonra 200 | Oluşturmada başarı bandı yok; ayar serbest metin; **taslakta cari/tür/şube düzenlenemiyor**; kalem girdilerinde erişilebilir ad yok; role=alert/odak yok; adet step=0.001. |
| 40 | `/gider` | HATA | Kategori istekleri 200; gider kaydı 2× reddedildi | **KRİTİK:** nakit kasa eksi bakiyede (-₺738.298,80) olduğu için kiracıda HİÇBİR gider kaydedilemiyor, çıkış yolu gösterilmiyor. Ham tutar biçimi; kasa bakiyesi formda yok; sekme değişiminde eski bantlar; tarih süzgeci yok. |
| 41–46 | Grup 8 rotaları + grup 7'nin son 2 rotası | ÖLÇÜLDÜ (betik sayımında) | — | **GİRDİ KESİLDİ** — bu 6 rotanın ayrıntılı grup çıktısı özet ajanına ulaşmadı; sayılar betik ölçümünde (46 rota / 225 bulgu) içeridedir, satır ayrıntıları bu tabloda yazılamadı. |

## Çapraz kontroller
- [x] **Karanlık tema okunaklı** — ölçülen tüm rotalarda gövde `rgb(11,18,32)` / `rgb(232,238,246)`; vurgulu rozetler koyu zemin + açık metin. **B-32 gerilemesi (açık zemin üstünde açık metin) hiçbir rotada görülmedi.**
- [x] **Dar ekran 375px: sayfa gövdesinde yatay kaydırma YOK** — ölçülen her rotada `document.documentElement.scrollWidth = 375` (örnekleme yapılmadı). Geniş tablolar yalnız kendi `overflow-x:auto` kabında kayıyor. `/evraklar` 890px taşması (tur 7: 1208) da kapandı. *(Grup 1'in 375 ölçümü hata kabuğuna aittir, rotaları temsil etmez.)*
- [x] **Grafik ölçümü (P-24, getBBox)** — 46 sayfa ölçüldü, 48 SVG değerlendirildi: **çizilmeyen grafik 0**, dar çizilen 3. Gerçek veri grafiği yalnız `/raporlar` (46 parçalı sütun + 4 dilimli pasta) ve `/urunler/etiketler` (44–50 parçalı barkodlar). Diğer sayfalardaki onlarca SVG ikondur (parça 0).
- [ ] **Her kaydet akışında başarı bandı — BAŞARISIZ.** `/faturalar` (oluştur/düzenle/iptal), `/siparisler` (oluştur), `/yedekler` (talep), `/gider-pusulalari` (PATCH) hiç bant basmıyor; `/cariler` durum değişiminde sessiz; `/uyum` ve `/kur` bantları `role=alert` taşımıyor.
- [ ] **Yayın hattı bütünlüğü — BAŞARISIZ (KRİTİK).** `qa-yayin` anlık görüntüsünde `blazor.boot.json`un istediği 2 `.wasm` (Microsoft.Extensions.Logging.Abstractions, System.ComponentModel.Annotations) ve `kilavuz.html` yok. Grup 1 turu bu yüzden tamamen boş döndü.
- [x] **Gerçek ekran girişi** — grup 2, 3, 4, 5, 7'de `/giris` formundan gerçek giriş yapıldı (token enjeksiyonuna düşülmedi). Grup 6 öykünmeli token ile açtı ve bunu açıkça bildirdi; grup 1 uygulama açılmadığı için hiç giriş yapamadı.

## Tur 7 → Tur 8 karşılaştırması

| Ölçüt | Tur 7 | Tur 8 | Fark |
|---|---|---|---|
| Ölçülen rota | 41 (ajan sayımı; gerçek 44) | **46** | +2 |
| Toplam bulgu | 211 (ajan sayımı; gerçek 236) | **225** | −11 |
| KRİTİK | — | **17** | — |
| Düzenlenemeyen veri girişi (ham) | 18 (ajan sayımı; gerçek 35) | **38 ham / 32 gerçek** (6'sı ekranda gerekçeli, meşru) | −3 gerçek |
| Ortalama görsel puan | 3,20 | **3,17** | −0,03 |
| Ortalama kullanıcı dostu puan | 2,80 | **2,74** | −0,06 |

**Önceki tur bulgularının durumu (grup sonuçlarındaki `onceki_bulgular` alanından):**
- **KAPANDI (kanıtlı, 22):** `/cariler` pasife alma (eski KRİTİK), `/cariler` Enter ile kaydetme, `/cariler` zorunlu alan işareti, `/urunler` pasif ürün satışta seçilebiliyor (eski KRİTİK), `/urunler` 375px süzgeç kırpılması, `/urunler` zorunlu alan işareti, `/islemler` sepet kalemi düzenlenebilir oldu, `/islemler` pasif ürün süzgeci, `/kullanicilar` şube ön dolu, `/veri-disa-aktarim` onay kutusu erişilebilir adı, `/urunler/agac` sıra alanı sıralamayı etkiliyor, `/fiyatlandirma` sayısal biçim, `/fiyatlandirma` aria-invalid, `/evraklar/takvim` boş durum ayrımı, `/vitrin` marj doğrulaması (eski KRİTİK), `/vitrin` sessiz yanlış fiyat, `/vitrin` etiket ipucu, `/vezne` sayfa başlığı, `/denetim-izi` bitiş tarihi off-by-one, `/evraklar` 890px taşma, `/evraklar` düzenleme bandı, `/ayarlar` "Ortam: Üretim", `/emanet` açıklama kaydı (eski KRİTİK), `/bildirimler` H1, `/siparisler` H2, `/kur` tarih süzgeci etiketleri, `/uyum` Enter araması, `/uyum` GONDERILDI yolu, `/uyum` işlem kimliği alanı.
- **KISMEN (9):** `/kilavuz` dönüş bağlantısı ve bölüm sayısı (belge 404 olduğu için ölçülemedi), `/faturalar` taslak düzenleme (standart taslakta çalışıyor, kuyumda çalışmıyor), `/faturalar` tablo kırılması, `/raporlar` dönemden bağımsız notu, `/raporlar` negatif bakiye (veri yokluğu), `/raporlar/kar-zarar` maliyet rozeti, `/raporlar/satis-analizi` NAKİT kartı ve tarih etiketleri, `/raporlar/islem-defteri` tarih etiketleri, `/yedekler` "değiştirilemez" kutusu yığılması, `/siparisler` kalem etiketleri, `/ayarlar` yarım MFA kaydı, `/evraklar/takvim` ISO vade biçimi.
- **AÇIK kalan:** grup 1'in 6 rotasındaki **40 bulgu** uygulama açılmadığı için yeniden denenemedi ve ihtiyatla AÇIK bırakıldı (yeni tur sayımına eklenmedi — kanıtsız "açık" da bir iddiadır, Anayasa md.1). Kalan gruplarda ~90 bulgu kanıtla AÇIK.
- **GERİLEMELER (3):** (a) `/kilavuz.html` geçen tur 200 ve 60.885 bayttı → şimdi 404; (b) `qa-yayin` anlık görüntüsünden 2 `.wasm` kaybolmuş; (c) `/raporlar/vade-takibi` ufuk alanı `type=number`+`min=0` iken `type=text`'e düşmüş.
- **Yeni bulgu:** bu turda ilk kez yazılan bulgu sayısı **≈62** (tur 7'de olmayan rotalar `/gider`, `/kurulum` kalemleri ve grup 2/4/5/6/7'nin yeni kalemleri dâhil; grup 1 hiç yeni bulgu üretemedi, yalnız 1 engel bulgusu 6 kez tekrarlandı).

## Bulgu özeti — TUR 8

### Sayılar
| Ölçüt | Değer |
|---|---|
| Gezilen rota | **46** (8 paralel grup) |
| Toplam bulgu | **225** |
| KRİTİK | **17** |
| Düzenlenemeyen veri girişi (ham / gerçek) | **38 / 32** (6'sı ekranda gerekçeli ve meşru) |
| Ortalama görsel puan | **3,17 / 5** |
| Ortalama kullanıcı dostu puan | **2,74 / 5** |
| Grafik ölçülen sayfa | 46 · 48 SVG · çizilmeyen **0** · dar çizilen **3** |

En düşük puanlı sayfalar: grup 1'in 6 rotası (1/1 — uygulama açılmadı), `/kilavuz` (2/1), `/kurulum` (3/2), `/faturalar` (3/2), `/gider` (3/2), `/evraklar/takvim` (3/2). En yüksek: `/ayarlar` (4/4), `/vitrin` (4/4), `/urunler/agac` (4/4).

### KRİTİK ve YÜKSEK bulgular — rota · kanıt · öneri

**KRİTİK**
1. **Yayın anlık görüntüsü bozuk — uygulama hiç açılmıyor (tüm rotalar).** `GET /_framework/Microsoft.Extensions.Logging.Abstractions.wasm` → 404, `System.ComponentModel.Annotations.wasm` → 404; konsol "Failed to start platform"; `qa-yayin/wwwroot/_framework` içinde 59 `.wasm` var, bu ikisi fiziksel olarak yok; yanındaki `qa-yayin-yeni/.../_framework` 0 `.wasm` (yarım kopya). → Yayın betiğine bütünlük kapısı: yayından sonra `blazor.boot.json`daki her varlık için HTTP 200 + SHA-256 eşleşmesi doğrulansın; geçmeden `kuyumcu-qa` ayağa kalkmasın.
2. **`/kilavuz` — kullanım kılavuzunun tamamı 404.** Düğme + 14 bölüm bağlantısı `kilavuz.html`e gidiyor; `fetch` ve `curl` ile 404/0 bayt. Dosya projede var (`Kuyumcu.Web/wwwroot/kilavuz.html`), yayın çıktısında yok. Geçen tur 200/60.885 bayttı → gerileme. → `CopyToPublishDirectory` + yayın sonrası 200 doğrulaması.
3. **`/kurulum` — geri dönülemez "Kurulumu mühürle" tek tıkla açılıyor.** Aynı `#kur-gerekce` alanı hem "Açılışı uygula" hem "Kurulumu mühürle" ön koşulu; alana 29 karakter yazılınca (açılış satırı hiç eklenmemişken) `#kur-muhurle.disabled=false` oldu, onay kipi yok. → Mühür için ayrı gerekçe alanı + işletme adını yazdıran ikinci onay (type-to-confirm).
4. **`/gider` — nakit kasa eksi bakiyede olduğu için hiçbir gider kaydedilemiyor.** "Kasa bakiyesi yetersiz: -738298.80 TRY var, 1250.75 isteniyor." ve ikinci TRY kasasında "0.00 TRY var". Kuralın koruduğu değişmez mevcut veride zaten bozulmuş; kural yalnız yeni kaydı engelliyor, çıkış yolu gösterilmiyor. → Eksi bakiyeye düşme yolunu kapatın, mevcutlar için düzeltme akışı verin, hata mesajına "Kasaya giriş yap / Virman yap" bağlantıları koyun.
5. **`/faturalar` — kuyum (özel matrah) taslağı "Düzenle" ile hiç kaydedilemiyor.** Toplam orijinaline geri yazılıp yalnız açıklama değiştirildiğinde bile `PATCH .../01M322DK7G3Z1TT8PPD53486D2` → 400. Düğme eklendi, işlev bu türde gelmedi. → Kuyum faturasında toplamı salt okunur yapın ve PATCH gövdesine göndermeyin.
6. **`/faturalar` — düzenleme kipinde Cari listesi 0 seçenek.** `select#fd-duzenle-cari`: `options.length=0`, `selectedIndex=-1`; aynı anda "+ Yeni Fatura" kipi 8 seçenek taşıyor. → Listeyi aynı kaynaktan doldurun, mevcut cariyi seçili getirin.
7. **`/siparisler/{Id}` — taslakta bile cari, tür, şube ve tahmini tutar düzenlenemiyor.** Düzenle formu yalnız [gün, açıklama, kalem tanım/ayar/gram/adet]. Yanlış cariyle açılan sipariş iptal edilip yeniden açılmak zorunda. → Taslakta bu alanları da düzenlenebilir yapın, değişikliği durum geçmişine yazın.
8. **`/yedekler` — talep gerekçesi düzenlenemiyor.** Satır eylemleri yalnız `['Talebi iptal et']`. → BEKLİYOR durumundaki talebe "Düzenle" ekleyin.
9. **`/gider-pusulalari` — listede "Düzenle" yok, düzenleme Detay kipinde gizli.** Satır eylemleri `['Detay','🖨 Yazdır']`; düzenlenebilir iki alan ancak Detay açılınca görünüyor. → Satıra "Düzenle" ekleyin ya da kipi "Detay / Düzenle" adlandırın.
10. *(Grup 1'in 6 rotasında aynı engel bulgusu 6 ayrı KRİTİK olarak kaydedildi — kök neden 1 numaralı maddedir.)*

**YÜKSEK**
11. `/raporlar/kar-zarar` — **DEFTER HAS GRAMI altın (32,7604) + gümüş (11,6550) = 44,4154 g** olarak tek sayıda toplanıyor; API'nin `satis.toplamHas` alanı da yanıltıcı. → Maden kırılımlı gösterin; alan adını `altinToplamHas` yapın.
12. `/raporlar/kar-zarar` — "Karşılaştır" seçildiğinde istek `karsilastir=ONCEKI_DONEM` ile 200 dönüyor ve gövdede `onceki` + `degisimYuzde` DOLU geliyor, ama arayüz hiç kullanmıyor (tek bir delta/ok/yüzde yok). → KPI kartlarına delta basın.
13. `/raporlar` — **Cari Yaşlandırma has gram bakiyesini hiç göstermiyor**: API `hasGramBakiye: -16,946` derken ekran o satırı tüm kolonlarda ₺0,00 gösteriyor. Kuyumcunun asıl alacağı gizleniyor. → HAS GRAM kolonu ekleyin.
14. `/vezne` — kuyruk "Hazırlanıyor" fişlerin toplamını **₺0,00** gösteriyor (fiş ucu `toplamTl=2150`, düzenleme sonrası 3200). Vezne görevlisi tahsil edeceği tutarı listede göremiyor. → Kuyruk sorgusunda kalem toplamını hesaplayın.
15. `/islemler` — başarı bandı **fiş numarasıyla birlikte ~5 sn'de kayboluyor** ve "Son satışlar" tablosunda fiş no sütunu yok. → Bandı kullanıcı kapatana kadar bırakın veya fiş no sütunu ekleyin.
16. `/cariler` — satırda **8 eylem düğmesi**, satır yüksekliği 305 px, 1280×800'de yalnız 2 cari görünüyor. → 2 birincil eylem + "⋯" taşma menüsü.
17. `/siparisler/{Id}` — düzenleme formunda **ürün seçici hiç yok** (oluşturmada var); yanlış ürünle açılan kalem taslakta bile düzeltilemiyor.
18. `/gider-pusulalari` — **"Hurda alımı gir" bağlantısı `/islemler`in Satış sekmesini açıyor** (query/hash boş). → `/islemler?sekme=hurda-alis`.
19. `/faturalar` — Düzenle kipinde **negatif tutar istemcide engellenmiyor**, PATCH gidiyor ve 400 dönüyor (aynı ürünün Yeni kipi engelliyor).
20. `/evraklar` — negatif tutarda doğrulama mesajı **yanlış**: dolu alana "Bu alan zorunludur." diyor.
21. `/yedekler` — **RPO taahhüdü 22 kat aşılmış (7985 dk / 360 dk)**, son yedek 5 gün önce, gösterge nötr renkte ve hiçbir uyarı yok (tur 7'de 4687 dk idi — kötüleşiyor).
22. `/denetim-izi` — denetim izinin **%78,8'i otomatik kur anlık görüntüsü** (8437 kaydın 6648'i); ~10 sn'de bir yeni satır. → Kur anlık görüntülerini ayrı teknik günlüğe alın.
23. `/uyum` — **"İşlem tutarı (isteğe bağlı)" fiilen zorunlu**: boşken Kaydet hiç açılmıyor, alan düzeyinde hiçbir hata basılmıyor.

### Düzenlenemeyen veri girişleri (38 ham / 32 gerçek eksik)
Kurucu kuralı: *"her veri girişi düzenlenebilir olsun."* Aşağıda **tamamı** listelenmiştir; sonundaki *(gerekçeli)* işaretliler ekranda gerekçesi ve düzeltme yolu yazılı olduğu için meşru sayılmıştır (6 kalem).

1. `/kullanicilar` — kendi kullanıcısının Rol ve Erişim durumu; kilit metni "Sahipliği Devret"i söylemiyor
2. `/kullanicilar` — kullanıcı kaydı silinemiyor, yalnız pasife alınıyor *(gerekçeli)*
3. `/veri-disa-aktarim` — aktarım geçmişi satırı düzeltilemiyor, dosya yeniden indirilemiyor *(gerekçeli, KVKK md.11)*
4. `/veri-ice-aktarim` — içe aktarım geçmişi satırları ve prova sonucu *(gerekçeli, kanıt kaydı)*
5. `/gider-pusulalari/{Id}/belge` — pusulanın hiçbir alanı belge ekranından düzeltilemiyor ve iptal edilemiyor
6. `/gider-pusulalari` — satırda Düzenle yok; satıcı, TCKN, tutar, tarih hiçbir yerden düzeltilemiyor (tek yol iptal + yeni pusula)
7. `/vitrin` — pano kalem kataloğu: yeni kalem oluşturulamıyor/silinemiyor (22 sabit kalem)
8. `/siparisler/{Id}` — sipariş carisi (müşteri/atölye)
9. `/siparisler/{Id}` — sipariş türü (müşteri/atölye/tamir)
10. `/siparisler/{Id}` — şube ("oturumdan")
11. `/siparisler/{Id}` — tahmini tutar (detayda düz metin)
12. `/siparisler/{Id}` — kalem ürünü (düzenleme formunda alan yok)
13. `/vezne` — fiş türü (Satış/Tahsilat/İşçilik) *(gerekçeli, düzeltme yolu iptal)*
14. `/raporlar` — rapor ekranı salt okunur *(gerekçeli, türetilmiş bakiye)*
15. `/raporlar/vade-takibi` — rapor rakamları *(gerekçeli)*
16. `/raporlar/islem-defteri` — hareket dökümü satırları *(gerekçeli)*
17. `/yedekler` — yedek talebi gerekçesi (Düzenle yok, yalnız iptal)
18. `/faturalar` — kuyum (özel matrah) taslağı: Düzenle açılıyor ama hiçbir değişiklik kaydedilemiyor
19. `/faturalar` — düzenleme kipinde Cari açılır listesi boş; faturanın carisi ne gösteriliyor ne değiştirilebiliyor
20. `/faturalar` — fatura kalemleri salt okunur; kalem düzeltme/silme yolu yok
21. `/evraklar` — evrak türü (Çek/Senet) kilitli ve nedeni yazmıyor
22. `/evraklar` — tutar ve evrakı veren müşteri *(gerekçeli)*
23. `/ayarlar` — kendi rolü *(gerekçeli)*
24. `/ayarlar` — sistem künyesi salt okunur *(gerekçeli, doğru davranış)*
25. `/emanet` — gram, ayar, maden ve cari kilitli; yanlış gram girilirse tek yol storno + yeniden giriş *(gerekçeli)*
26. `/emanet` — storno satırı ve stornolanmış satır ikinci kez stornolanamıyor *(gerekçeli)*
27. `/bildirimler` — çalışma geçmişi satırları *(gerekçeli, koşum kanıtı)*
28. `/bildirimler` — kurulumda KAPALI bir iş arayüzden açılamıyor
29. `/urunler/etiketler` — etiket basım işi (adet + seçim + arama) kaydedilemiyor, sayfadan çıkınca sıfırlanıyor
30. `/uyum` — şüpheli işlem bildiriminin tutarı ve gerekçesi hiçbir durumda düzenlenemez (mevzuat gerekçeli biçimsel istisna)
31. `/kur` — manuel kur kaydı kaydedildikten sonra düzenlenemiyor, yalnız gerekçeli iptal var
32. `/siparisler` — sipariş başlığındaki CARİ, TÜR ve ŞUBE (oluşturmada soruluyor, taslakta düzenlenemiyor)
33. `/gider` — gider satırının tutarı (düzeltme yolu storno + yeni kayıt; bu turda hiç gider oluşturulamadığı için storno FİİLEN DENENEMEDİ)
34. `/gider` — virmanın tek bacağı *(gerekçeli, iki bacak birlikte döner)*
35. `/cikti/{Tur}/{Id}`, `/kasalar`, `/sayim`, `/banka` — tur 7'de kaydedilen düzenlenemez kalemler bu turda **doğrulanamadı** (uygulama açılmadı); listeden düşürülmedi, açık kabul edildi.

### Görsel / kullanıcı dostu ortalamaları
- Ortalama görsel: **3,17 / 5** (tur 7: 3,20)
- Ortalama kullanıcı dostu: **2,74 / 5** (tur 7: 2,80)
- İki ortalamanın da hafifçe gerilemesinin tek başına en büyük nedeni grup 1'in 6 rotasının 1/1 puan almasıdır (yayın hatası). Bu 6 rota dışarıda bırakılırsa görsel ≈3,5 / kullanıcı dostu ≈2,9 çıkar — yani ürünün kendisi gerilemedi, **ölçüm engellendi**.

### Düzeltme paketine giren maddeler (tur 8 çıktısı)
**D8-1 (kapı, önce bu):** Yayın bütünlüğü kapısı — `blazor.boot.json` varlıkları + `kilavuz.html` için HTTP 200 + karma doğrulaması; geçmeden QA sunucusu kalkmasın. *(KRİTİK 1, 2)*
**D8-2:** `/kurulum` mühür ayrımı — ayrı gerekçe alanı + type-to-confirm onayı. *(KRİTİK 3)*
**D8-3:** Kasa eksi bakiye düzeltme akışı + gider formunda bakiye gösterimi + eylem bağlantılı hata mesajı. *(KRİTİK 4)*
**D8-4:** Fatura düzenleme onarımı — kuyum taslağında toplamı salt okunur yap, cari listesini doldur, negatif tutarı istemcide engelle. *(KRİTİK 5, 6 + YÜKSEK 19)*
**D8-5:** Taslak kayıtlarda başlık alanlarının düzenlenebilirliği — sipariş cari/tür/şube/tutar + kalem ürün seçici, yedek talebi gerekçesi, gider pusulası satır Düzenle. *(KRİTİK 7, 8, 9 + YÜKSEK 17)*
**D8-6:** Rapor doğruluğu — has gram maden kırılımı, karşılaştırma deltalarının basılması, cari yaşlandırmada HAS GRAM kolonu, vezne kuyruğunda gerçek toplam, işlem defterinde YÖN türetimi. *(YÜKSEK 11–14)*
**D8-7:** Geri bildirim tekdüzeliği — her kaydet/durum değişiminde `role=status` bandı; başarı bandının kalıcılığı ve fiş no sütunu; bandın görünür alana kaydırılması. *(YÜKSEK 15 + çapraz kontrol)*
**D8-8:** Liste yoğunluğu — `/cariler` ve `/faturalar` satır eylemlerinin taşma menüsüne alınması. *(YÜKSEK 16)*
**D8-9:** Performans — `kur/guncel` yoklamasının tek paylaşılan servise/SignalR'a alınması (154 istek/tur) ve kur anlık görüntülerinin denetim izinden çıkarılması (%78,8). *(YÜKSEK 21, 22)*
**D8-10:** Operasyon uyarıları — RPO aşımının kırmızı gösterilmesi + "şimdi doğrula" eylemi. *(YÜKSEK 20)*
**D8-11 (sistematik, tek bileşenle çok ekran):** rozet/metin bitişiklikleri (`MANUALGeçersiz`, `QA FrontendSAHIP`, `...DUZENLENDIPasif`), ham enum → Türkçe sözlük (denetim izi 17, işlem defteri, yedekler), eksik `PageTitle`, düzeltilince kalan hata işaretleri, sekme/kip değişiminde mesaj kuyruğunun temizlenmesi, tarih/sayı biçiminin tek bir tr-TR biçimlendiricide toplanması.
**D8-12 (gerileme onarımı):** `/raporlar/vade-takibi` ufuk alanı `type=number min=0 max=365`'e geri döndürülsün.

> **GİRDİ KESİLDİ** — grup 7'nin son 2 rotası ile grup 8'in 6 rotasının ayrıntılı çıktısı bu özet ajanına ulaşmadı; sayılar betik ölçümünden alınmıştır, satır ayrıntıları eksiktir.
