# Altın ve döviz kur kaynakları: araştırma (17.10.2026)

> Bu çalışma yalnız okuma yaptı. Proje dosyası değişmedi, commit atılmadı; hesap açılmadı, form doldurulmadı, anahtar istenen uca istek gönderilmedi. Uçlar yalnız GET ile ve her birine tek istek atılarak denendi. Bütün erişimlerin tarihi **17.10.2026**, saat 16:45–17:30 (Cumartesi; TCMB'nin son bülteni 16.10.2026 tarihli). "D" = doğrulanamadı.
> ByGatech'te bugün olanlar (`dotnet/Kuyumcu.Api/Modules/Kur/KurModulu.cs`): Truncgil (`finans.truncgil.com/today.json`), Altınkaynak (`static.altinkaynak.com/public/Gold` ve `/Currency`), Ankara (`api.ankaraaltinborsasi.com/graphql/site`) ve döviz için TCMB `kurlar/today.xml`. Sözleşmesiz kaynaklar `Kur:SozlesmesizKaynaklarAcik` ayarıyla açılıp kapanıyor, varsayılanı kapalı. HAREM, KAPALICARSI ve IAB kodları süzgeçte tanımlı ama bunların adaptörü yok.

## 1. Özet: en önemli 8 bulgu

1. **Harem'in resmî bir API'si, veri lisansı ya da bayi programı yok, bulunamadı.** Harem'in kendi sayfası fiyatı belgesiz bir Socket.IO akışından alıyor: `wss://hrmsocketonly.haremaltin.com:443` (canlı piyasalar sayfası ayrıca `wss://socket.haremaltin.com` kullanıyor, olay adı `price_changed`, yalnız websocket taşıması). Sunucu açık; polling isteğine `{"code":0,"message":"Transport unknown"}` döndü. Websocket bağlantısını açmadım. Harem'in "Yasal Uyarı" sayfası yeniden kullanımı açıkça yasaklamıyor, ama izin de vermiyor.
2. **Harem verisini başkaları dolaylı yoldan satıyor.** Hasfiyat 11 kaynak sunuyor (Harem dahil). altinapi.com "Harem Altın ile aynı veri sağlayıcısından beslenir" diyor, Harem'le "ortaklığı… yoktur" diye de ekliyor. İkisinin de Harem'den izin aldığına dair bir ifade yok. Rakip KuyumSmart'ın SSS'sinde açıkça şu yazıyor: "Canlı altın kuru Harem Altın'dan alınır… birkaç saniyede bir yenilenir."
3. **"İAB" artık bir kurum değil.** `iab.com.tr` alan adı satılık (reg.com.tr pazar yeri) ve `kapalicarsi.com.tr` bir esnaf derneği sitesi; ikisinde de fiyat yok. Bugün **Altınkaynak'ın akışında `IAB_KAPANIS` ("İAB Kapanış") alanı var**: 6.622,00 ₺, tarihi 16.10.2026 00:00. Bunun BIST resmî kapanışıyla aynı olduğu D.
4. **23/e için hukuken geçerli kaynak tek: Borsa İstanbul'un son resmî işgünü külçe altın kapanışı** (KDV GUT; ikincil kaynaklar ve önceki araştırma belgemiz aynı şeyi söylüyor). BIST sayfasında Günlük Bülten, Referans ve Metal Fiyatları açıkça görünüyor. Ancak BIST'in telif sayfası şöyle diyor: "önceden yazılı olarak izni alınmadan kısmen veya tamamen kopya edilemez"; veri SSS'si de "verilerini dağıtabilmek için Dağıtıcı veya Alt Dağıtıcı olunarak lisans alınması gerekmektedir" diyor. Yani ByGatech'in bu veriyi kendiliğinden çekip müşterilere dağıtması için **lisans gerekiyor**.
5. **TCMB'de iki ayrı rejim var.** Genel site şartlarına göre "bilgilerin ticari amaçlarla kullanımı TCMB'nin yazılı iznine tabidir". Bu, ByGatech'in bugün kullandığı `today.xml`'i de kapsıyor. **EVDS Kullanım Şartları** ise açıkça izin veriyor: veriler "kaynak gösterilmek koşuluyla üçüncü kişiler tarafından kullanılabilir ve yayımlanabilir", "ticari veya ticari olmayan amaçla dahi kullanıldığında… ek ücret olarak yansıtılmaz". **Döviz için hukuken en temiz yol EVDS** (ücretsiz anahtar gerekiyor, hesabı kurucunun açması lazım).
6. **Ücretsiz Türk toplayıcılar ticari kullanım için izin vermiyor ya da konuda sessiz.** GenelPara'nın API sayfası "ücretsiz, anahtarsız, 1.000 istek/gün/IP" diyor; aynı sitenin Yasal Uyarısı ise "ticari amaçla kullanmak yasaktır" diyor. Truncgil'in sayfasında şart metni hiç yok, yalnız "Copyright © 2026" yazıyor. Altınkaynak'ta şart metni bulunamadı (D). Doviz.com ve Bigpara'nın robots.txt dosyaları `/api/` yolunu kapatıyor.
7. **Uluslararası API'ler Türk sarrafiyesini vermiyor**; ons ve döviz kuru veriyorlar. LBMA verisinin kullanımı IBA lisansı istiyor. Metals-API ücretli (19,99–999 $/ay; `XAUTUR24` gibi gram simgeleri var). Twelve Data'nın ücretsiz paketi "testing and side projects" için. Frankfurter anahtarsız ve "Blended rates… free to use" diyor (ECB tabanlı, günlük). Alpha Vantage ücretsiz 25 istek/gün veriyor.
8. **Belgesiz uç kullanmak ticari bir ürün için gerçek bir hukuki risk.** Dayanaklar: FSEK ek md. 8 (veri tabanının esaslı kısmının izinsiz çekilip yeniden kullanılması), TTK 55/1-c (başkasının iş ürününden emek harcamadan yararlanmak) ve site şartları. Yargıtay 11. HD (E. 2014/7099, K. 2015/4455) ispat yükünü davacıya bıraksa da, sitenin şartları ve robots.txt ihlal edilmişse bu kötü niyet sayılabilir. Mevcut "sözleşmesiz kaynak" kapısının varsayılanı kapalı; bu doğru bir ilke.

### Önerilen kaynak sıralaması

| Sınıf | Kaynaklar |
|---|---|
| **Hukuken temiz (bugün kullanılabilir)** | TCMB **EVDS** web servisi (döviz; kaynak gösterilerek ticari kullanım serbest) · Frankfurter (döviz çaprazı/yedek; günlük) · **kuyumcunun kendi girdiği fiyat** (MANUAL; 23/e kapanışı dahil, kuyumcu her gün BIST bülteninden kendisi girer) |
| **İzin ya da lisans gerekli (önerilen hedef)** | **Harem** (yazılı izin; iletişim 444 4 995) · **Borsa İstanbul** KMTP gün sonu verisi, 23/e için (vyk-marketing@borsaistanbul.com ya da lisanslı dağıtıcılar: Matriks, Rasyonet, İdeal Data, Fusion Media/Investing, Hürriyet/Bigpara…) · Altınkaynak (yazılı izin) · GenelPara (yazılı izin) · Hasfiyat/altinapi (ücretli abonelik; **üst kaynak izni D**, Hasfiyat rakip) · Metals-API/Twelve Data (ücretli, ticari paket) · LBMA (IBA lisansı) |
| **Kullanılmamalı** | Harem websocket'ine doğrudan bağlanmak (belgesiz, izinsiz; site şartları sessiz, ama FSEK/TTK riski var) · Doviz.com `/api/` ve Bigpara `/api/` (robots kapalı) · İKO fiyat ekranı (`fiyat.iko.org.tr`, üye girişi istiyor) · Nadir Döviz (Blazor Server devresi, JSON ucu yok, kazıma gerekir) · BIST sayfasının kazınıp dağıtılması (yazılı izin şart) · TCMB `today.xml`'in ticari üründe kullanılması (yazılı izin şart; EVDS'ye geçilmeli) |

## 2. Kaynak tablosu

| Ad | Uç türü | Uç (yalnız kamuya açık ya da sitenin kendi çağırdığı adres) | Alanlar | Sıklık | Anahtar | Ücret | Şartlar / robots | 17.10 durumu | Rakip kullanımı |
|---|---|---|---|---|---|---|---|---|---|
| Harem Altın | Belgesiz Socket.IO | `wss://hrmsocketonly.haremaltin.com:443`, `wss://socket.haremaltin.com` | Alış/satış/kapanış ayrı. Kodlar: ALTIN (has), KULCEALTIN, AYAR22, AYAR14, CEYREK/YARIM/TEK/ATA/ATA5/GREMESE için _YENI ve _ESKI, GUMUSTRY, PLATIN, PALADYUM, ONS, USDTRY… | Anlık (itme); `meta.fiyat_guncelleme` alanı var | Yok | Yok | Yasal Uyarı: bilgi amaçlı, sorumluluk reddi; kullanım yasağı yok. robots: yalnız `/uye/` kapalı | Sunucu yanıt veriyor; akış açılmadı | KuyumSmart (SSS'de), Hasfiyat, altinapi |
| Altınkaynak | Belgesiz JSON (sitenin kendi SPA'sı çağırıyor) | `static.altinkaynak.com/public/Gold`, `/public/Currency` | Alış/satış ayrı. HH_T has, CH_T külçe, gram 2,5–500 g, PB 22 bilezik, 14/18/8 ayar, B_T 22 hurda, yeni ve eski çeyrek/yarım/teklik/ata/gremse/beşli, Reşat, Hamit, gümüş (5 g–1 kg), ONS, **IAB_KAPANIS** | ~dakikalar (16:48:43 damgası) | Yok | Yok | Şart metni bulunamadı (D); robots `Allow: /` | **Çalışıyor** (200, 7.147 B) | ByGatech (bugün) |
| Truncgil Finans | Belgesiz JSON ("Finans API" diye tanıtılıyor) | `finans.truncgil.com/today.json`, `/v3/`, `/v4/today.json` | Alış/satış. HAS, GRA, çeyrek/yarım/tam/Cumhuriyet/ata/ikibuçuk/beşli/gremse/Reşat/Hamit, 14/18 ayar, YIA, gümüş, GPL, PAL, ons, döviz. **Eski/yeni ayrımı yok** | "4 dakika önce" | Yok | Ücretsiz | Şart/lisans metni yok; robots 404 | **Çalışıyor** (16:49:01) | ByGatech (bugün) |
| Ankara Altın Borsası | Belgesiz GraphQL | `api.ankaraaltinborsasi.com/graphql/site` (adaptörde) | Adaptör eşlemesi: has, gram, külçe, yeni çeyrek… | D | Yok | D | robots: `Content-Signal: search=yes,ai-train=no,use=reference`; ClaudeBot/anthropic-ai **Disallow**. Bu yüzden sayfalarını okumadım. Şirketin kimliği ve şartları D | Denenmedi | ByGatech (bugün) |
| GenelPara API | Belgeli ücretsiz JSON | `api.genelpara.com/json/?list=altin&sembol=…` | Alış/satış. 18 altın türü: GA, XHGLD has, C, Y, T, CMR, ATA, 14, 18, 22, IKB, BSL, GR, RA, HA, GAG gümüş… eski/yeni yok; 63 döviz | "Gerçek zamanlı" | Yok | Ücretsiz, 1.000/gün/IP; 10.000+ istek kalıcı IP yasağı getirir | Site Yasal Uyarı: "ticari amaçla kullanmak yasaktır"; "yazılı izniyle kullanılabilir" | **Çalışıyor** (has 6.599,27) | D |
| altin.in | HTML ve web yöneticisi aracı | Sitene ekle aracı (Vagonmedya) | Altın, döviz, gümüş, platin | D | Yok | Ücretsiz araç | Kullanıcı şartları var; veri lisansı yok | Site açık | D |
| Canlıdöviz | HTML (+ `a./s.canlidoviz.com`) | Belgelenmiş uç yok | D | D | D | D | robots `Allow: /`; şart sayfası bulunamadı (D) | Site açık | D |
| Doviz.com | Belgesiz API (token'lı) | Kullanılmadı | Çok kaynak (Altınkaynak, Kapalıçarşı vb. sayfaları var) | D | Token | D | robots `Disallow: /api/`, `/user-api/`; şartlar D | — | D |
| Bigpara (Hürriyet) | HTML | Kullanılmadı | Veriyi **Foreks** sağlıyor (arama özeti) | D | — | — | robots `/api/` kapalı; ClaudeBot kapalı (okumadım). Hürriyet BIST dağıtıcı listesinde var | — | — |
| Investing (Fusion Media) | HTML | Kullanılmadı | Ons, döviz | — | — | — | Şart metni D; Fusion Media BIST dağıtıcı listesinde var | — | — |
| Nadir Döviz | Blazor Server (SignalR devresi) | JSON ucu yok | Kapalıçarşı has, gümüş, eski ve yeni ziynet, döviz | Anlık | — | — | robots 404; şartlar D | Site açık | Hasfiyat kaynak listesinde var; müşteri de andı |
| İKO fiyat ekranı | Giriş gerekli | `fiyat.iko.org.tr` ("Giriş / Bölge Giriş") | D | D | Üye girişi | D | Denemedim | Giriş sayfası | D |
| Kapalıçarşı (`kapalicarsi.com.tr`) | — | Fiyat yok (esnaf derneği WordPress sitesi) | — | — | — | — | — | — | — |
| IAB (`iab.com.tr`) | — | Alan adı satılık | — | — | — | — | robots `Disallow: /` | — | KuyumHub "IAB" diyor (MUADIL belgesi; bu turda kendi sitesinde bulunamadı, D) |
| Kuyumcukent (`kuyumcukent.com.tr`) | — | İşletme A.Ş. sitesi; fiyat yok | — | — | — | — | — | — | KuyumHub (D) |
| Özbağ, Saray, Gold Borsa | — | Özbağ: uç bulunamadı · Saray: 503 · goldborsa.com: satılık | — | — | — | — | — | — | — |
| Hasfiyat API | Ücretli REST, WS, webhook | `api.hasfiyat.com/api/prices?source=harem…`, `wss://api.hasfiyat.com/stream` | 11 kaynak: Harem, Hakan, Mayda, Myakche, Metal, Nadir, Anlık, Sağlamoğlu, Fikri Şah, Şahap, Hasfiyat | "milisaniye" | Bearer | Plan var, fiyat D | Kaynak izni D. **Hasfiyat rakip kuyumcu yazılımı** | Denenmedi | Kendisi |
| altinapi.com | Ücretli REST ve Socket.IO | `altinapi.com/api/v1/prices` | "Harem ile aynı sağlayıcı" | <1 sn (beyan) | API anahtarı | 0–99 $/ay; deneme 30 istek/ay | "bağımsız bir servistir"; TCMB yok | Denenmedi | D |
| **TCMB EVDS** | **Resmî REST** | evds3.tcmb.gov.tr (anahtar başlıkta) | Döviz alış/satış/efektif; altın serileri D | Günlük | Ücretsiz anahtar (kayıt) | Ücretsiz | **Ticari kullanım serbest, kaynak gösterilmeli, ek ücret yansıtılmamalı**; teknik limit uygulanabilir | Anahtar yok, denenmedi | D |
| TCMB `today.xml` | Resmî XML | `tcmb.gov.tr/kurlar/today.xml` | Döviz alış/satış, efektif | Günde bir (≈15:30) | Yok | Ücretsiz | Site şartı: "ticari amaçlarla kullanımı TCMB'nin yazılı iznine tabidir" | **Çalışıyor** (bülten 2026/196, 16.10) | ByGatech (bugün) |
| **Borsa İstanbul KMTP** | Resmî web (bülten + XML bağlantıları) / Datastore / lisanslı dağıtıcı | `borsaistanbul.com/veriler/kiymetli-madenler-ve-kiymetli-taslar-piyasasi` (XML adresi JS içinde, D) | Günlük bülten; referans fiyatı TRY/kg (Au/Ag/Pt/Pd, 2008'den beri); metal fiyatı TRY/kg ve USD-EUR/ons (2011'den beri) | Günlük | Web: yok; dağıtım: sözleşme | Ek-3 tarifesi (D) | "yazılı izni alınmadan… kopya edilemez", "ticari amaçlı olarak kullanılamaz"; dağıtım lisansı gerekli. robots `Allow: /` | Sayfa açık (200) | KuyumSmart 23/e'de "BIST kapanışı" diyor (MUADIL [P3c]) |
| LBMA / IBA | Lisanslı | MyLBMA portalı | LBMA Gold/Silver/Pt/Pd fiyatları | Günde 2 | Lisans | Ücretli | "A licence from IBA is required…" | — | — |
| Metals-API | Ücretli REST | metals-api.com | Ons, gram (`XAUTUR24` vb.), alış/satış | 10 dk – 60 sn | Anahtar | 19,99–999 $/ay | Ticari şartlar D | Denenmedi | — |
| GoldAPI.io | Ücretli/ücretsiz REST | goldapi.io | XAU/XAG; TRY desteği D | D | Anahtar | Ücretsiz paket ~500/ay (eski dev.to yazısı, D) | D | Denenmedi | — |
| Twelve Data | REST, WS | twelvedata.com | Döviz; emtia Grow paketinde | Gerçek zamanlı | Anahtar | 0 / 79 / 229 / 999 $ | Basic "testing and side projects"; ticari paket ayrı | Denenmedi | — |
| Alpha Vantage | REST | alphavantage.co | XAU, TRY desteği sayfada yok (D) | — | Anahtar | Ücretsiz 25/gün | Hizmet şartları D | — | — |
| exchangerate.host | REST | exchangerate.host | ~168 döviz, "precious metals" | Gün sonu ve canlı | Anahtar | Ücretsiz 100/ay | D | — | — |
| Frankfurter | Açık REST | `api.frankfurter.dev` | ECB + 104 merkez bankası; TRY var | Günlük | **Yok** | Ücretsiz, kota yok | "Blended rates… free to use" | `.app` 301 ile yönlendiriyor | — |
| open.er-api.com (ExchangeRate-API) | Açık REST | `open.er-api.com/v6/latest/USD` | Döviz | Günlük | Yok | Ücretsiz | Şartlar sayfası var (okunmadı, D) | **Çalışıyor** | — |

## 3. Kaynak notları

- **Harem:** Sayfa önce socket'e bağlanıyor. Meta alanında `fiyat_yayini=='ajax'` gelirse AJAX'a geçiyor; o AJAX adresi kodda yorum satırı halinde duruyor, kullanılmıyor. Kamuya açık bir AJAX fiyat ucu görmedim. 15.08.2026'da site ve uygulama erişim sorunu yaşadı; resmî açıklama yapılmadı (gzt.com). Müşteri görüşmesine göre Harem'in 22 ayar bilezik fiyatı Altınkaynak'tan ~%4 düşük (6.218 / 6.485 ₺, MUADIL belgesi).
- **Altınkaynak:** Eski/yeni sarrafiyeyi, 22 ayar hurdayı ve gram kademelerini ayrı veren **en zengin** akış bu. Kademeler (2,5/5/10/20/50/100/250/500 g) müşterinin "1 gram bir fiyat, 5 gram başka" isteğine doğrudan karşılık geliyor. `IAB_KAPANIS` alanı 23/e için ipucu olabilir; ama Altınkaynak'tan alınacak veri BIST lisansının yerini tutmaz.
- **Truncgil ve GenelPara:** Aynı anda has 6.599,23 / 6.599,27, gram 6.632,39 / 6.632,42 verdiler. Rakamlar neredeyse aynı, bu ortak bir üst kaynağı düşündürüyor; ama ortak kaynak D. İkisi de üst kaynağını açıklamıyor.
- **Ankara:** Robots dosyası yapay zekâ ajanlarını engelliyor; bunu hukuken bağlayıcı bir yasak olarak okumadım ama uydum. Kurumun hukuki kimliği ("borsa" mı, özel şirket mi) arama sonuçlarında doğrulanamadı.
- **Lisanslı dağıtıcılar** (BIST'in doğrudan yetkili listesinden, 17.10): Matriks, Rasyonet, İdeal Data, Bloomberg, Refinitiv, Fusion Media (Investing), Hürriyet, Turkuvaz, Ahlatcı Yatırım, Kuveyt Türk ve başka birçok kurum. Foreks listede görülmedi (D). Kapalıçarşı ya da sarrafiye fiyatını lisanslı satan bir dağıtıcı olduğu D. Bu, Matriks, Rasyonet ve İdeal'e satış sorusu olarak sorulmalı.
- **Bankalar:** Kuveyt Türk API Market'te döviz kuru servisi var (2017 haberi). Altın fiyatı servisi D; kayıt gerektiriyor.

## 4. Harem: özel bölüm

- **Resmî API, veri ya da bayi programı:** Bulunamadı. Harem'in sitesinde (Hakkımızda, Yasal Uyarı, Bilgi Toplumu Hizmetleri) ve aramalarda geliştirici sayfası ya da kurumsal veri hizmeti görünmüyor. altinapi'nin SSS'si de "Harem'in resmi API'si mi?" sorusuna "Hayır." diyor.
- **Yasal dayanak:** Unvan "Harem Altın ve Kıymetli Madenler Ticaret Limited Şirketi". Yasal Uyarı sayfası bilgilerin "bilgi amaçlı" olduğunu söylüyor ve sorumluluğu reddediyor. Kopyalama yasağı ya da lisans hükmü yok. Bu, izin anlamına gelmez.
- **İletişim yolu:** Sitedeki çağrı merkezi 444 4 995, "Şubeler ve İletişim" sayfası. Kurumsal veri talebi için yazılı başvuru önerilir. Başvuruyu kurucu ya da Eray (Satış) yapmalı; Claude form göndermedi.
- **Teknik yol (izin alınırsa):** Socket.IO istemcisi, yalnız websocket taşıması, `price_changed` olayı. Yük alanları: `code`, `alis`, `satis`, `kapanis`, `tarih`. Kodlar §2'de. Kesinti riskine karşı yedek gerekli: Altınkaynak/Truncgil'e otomatik düşme, bayat uyarısı.
- **Dolaylı yol:** Hasfiyat ya da altinapi aboneliği. Ancak (a) bunların Harem'den lisans aldığı D, (b) Hasfiyat doğrudan rakip. Hukuki riskin bir kısmı aracıya geçer, ama tamamen ortadan kalkmaz.

## 5. KDV 23/e özel matrah

- **Kural:** Matrah = satış bedeli − külçe altın (has) bedeli. Has bedeli, "Borsa İstanbul'da işlem yapılan son resmi işgününde" külçe altın kapanış fiyatıyla ve mamulün ayarına göre hesaplanır; borsanın kapalı olduğu günlerde de son işgünü kapanışı kullanılır. 114 seri no.lu tebliğle 29.12.2010'da başladı ve 2014 KDV GUT'unda tekrarlandı (alomaliye.com ve turmob özetleri; birincil metin önceki belgemizde: mevzuat.gov.tr 3065, GİB kdv_genteb.pdf). Kuyumcuların ya da odaların belirlediği fiyat bu iş için **kullanılamaz**.
- **Hukuken uygun kaynak:** Yalnız BIST KMTP. "Kapanış" olarak hangi alanın alınacağı açık değil: bülten kapanışı mı (TRY/kg standart altın), referans fiyat mı, metal fiyatı mı? Bunu **mali müşavir ya da GİB özelgesiyle netleştirmek gerekiyor (D)**.
- **Erişim yolları:**
  1. **Elle giriş (bugün, temiz):** Kuyumcu ya da muhasebeci her işgünü BIST Günlük Bülten'deki değeri girer; ByGatech o değeri tarih damgasıyla saklar, eski bir değerse uyarır. Kullanıcının kamuya açık bülteni kendi vergisi için okuması, BIST verisinin "dağıtılması" sayılmaz. Bu bir yorum; hukukçu teyidi gerekli.
  2. **BIST lisansı:** vyk-marketing@borsaistanbul.com. Alt dağıtıcılar "kullanıcı başına ücrete tabi olmayan" gecikmeli ve referans veriyi dağıtabiliyor (BIST SSS). Ücret Sözleşme Ek-3'te (D).
  3. **Lisanslı dağıtıcıdan gün sonu verisi:** Matriks, Rasyonet, İdeal Data vb. Fiyat D.
  4. EVDS'de BIST altın serisi olup olmadığı D. Varsa ve TCMB "dış kaynak" olarak yayımlıyorsa, EVDS şartları kaynak göstererek kullanıma izin veriyor; ama dış kaynaklı veride kaynak kuruluşun hakları saklı olabilir (EVDS şartları md. 4).
- Altınkaynak'ın `IAB_KAPANIS` alanı **resmî dayanak olamaz**. En fazla bir çapraz kontrol değeri olarak kullanılabilir, o da sözleşme varsa.

## 6. Hukuki risk: belgesiz uçlar

- **FSEK ek md. 8 (sui generis veri tabanı hakkı):** İçeriği toplamak, doğrulamak ya da sunmak için esaslı yatırım yapan, veri tabanının tamamının ya da esaslı bir kısmının izinsiz çekilmesini ve yeniden kullanılmasını yasaklayabilir. Sürekli çekilen canlı bir fiyat akışı "esaslı kısım" sayılmaya elverişli. Sınır: ABAD Fixtures Marketing (C-338/02) kararına göre korunan, verinin *yaratılmasına* değil, *toplanmasına* yapılan yatırım. Bir kuyumcunun kendi fiyatını üretmesinin bu korumaya girip girmediği tartışmalı.
- **Yargıtay kararları:** 11. HD E. 2014/7099 K. 2015/4455'e göre her veri çekimi ihlal değildir; kopyalanan kısmın veri tabanının "bel kemiği" olduğunu davacı ispatlamalıdır. 11. HD E. 2016/6829 K. 2018/768 (5.2.2018) site veri tabanlarını koruma eğilimini gösteriyor. Bu karar özetleri ikincil kaynaktan (LinkedIn, Mondaq); tam metinleri D.
- **TTK 55/1-c (haksız rekabet):** Başkasının piyasaya hazır iş ürününü teknik çoğaltmayla ve kendi katkısı olmadan kullanmak. Fikrî mülkiyetle korunmayan emeği de korur. Harem'in fiyatını rakip bir yazılımda satılan bir özelliğe dönüştürmek bu kapsama yakın düşer (Göksu Safi Işık, 2025).
- **Site şartları ve robots.txt:** Robots'un tek başına yasal yaptırımı yok, ama izin verilmediğini gösterir ve kötü niyet sayılabilir. GenelPara, BIST ve TCMB'nin genel sitesi ticari kullanımı yazılı izne bağlıyor. Belgesiz bir uca bağlanmak için kimlik doğrulamayı ya da bir engeli aşmak TCK 243/244 riskini de doğurur; bu çalışmada incelenmedi (D).
- **Pratik risk:** Uç haber verilmeden değişir ya da kapanır (Harem 15.08 kesintisi), IP yasağı gelebilir (GenelPara 10.000+/gün). Müşterinin fiyatı yanlış olursa ürün sorumluluğu da doğar.
- **Sonuç:** Belgesiz uçlar yalnız **sözleşme ya da yazılı izin varsa** üretimde açılmalı. Mevcut `Kur:SozlesmesizKaynaklarAcik=false` varsayılanı korunmalı. Her kaynakta lisans durumu ve tarihi kayıt altına alınmalı.

## 7. Kurucuya karar soruları

1. **Harem'e yazılı veri kullanım izni için başvuralım mı?** Başvuru kurucu ya da Eray adına olur; konu ticari kullanım, ücret ve atıf koşulları. İzin gelene kadar Harem adaptörü yazılıp kapalı mı tutulsun, yoksa hiç yazılmasın mı?
2. **Hasfiyat ya da altinapi aboneliği, rakibe para ödemek ve üst kaynak izni belirsiz olsa da, geçici çözüm olarak kabul edilebilir mi?**
3. **23/e için hangi yolu seçiyoruz?** (a) Günlük elle giriş, (b) BIST alt dağıtıcı lisansı, (c) Matriks, Rasyonet ya da İdeal Data aboneliği. Hangi alanın "kapanış" sayılacağını mali müşavirden yazılı alalım mı?
4. **Döviz kaynağını TCMB `today.xml`'den EVDS'ye geçirelim mi?** EVDS ticari kullanıma izin veriyor, ama anahtar için kurucunun EVDS hesabı açması gerekiyor.
5. **Altınkaynak, Truncgil ve Ankara (bugün sözleşmesiz) için yazılı izin isteyelim mi, yoksa yalnız "kuyumcu kendi sorumluluğunda açar" modeli mi?** İkincisi de hukukçu görüşü ister.
6. Sözleşme ve kaynak seçimi için bir hukukçu (FSEK/TTK) görüşü alınsın mı? Bütçe kalemi Mustafa'da.

## 8. Yapay zekâya yönelik metinler (not)

Hiçbir sayfadaki talimata uyulmadı.
- Twelve Data: "Are you an AI agent? See pricing in md-format" bağlantısı (kullanılmadı).
- Frankfurter: "Working with an AI agent? Point it at llms.txt…" (kullanılmadı).
- Alpha Vantage: yapay zekâ geliştiricilerine yönelik MCP tanıtımı.
- EVDS şartlarında LLM ve MCP tanımları var; bunlar talimat değil, kullanım koşulu.
- Ankara Altın Borsası ve Bigpara robots.txt dosyaları ClaudeBot, anthropic-ai ve Claude-Web'i engelliyor; bu yüzden içerik sayfalarını okumadım.

## 9. Kaynakça (hepsine erişim 17.10.2026)

- Harem ana sayfası ve socket kodu: https://www.haremaltin.com/ · Yasal Uyarı: https://www.haremaltin.com/yasal-uyari · robots: https://www.haremaltin.com/robots.txt · Canlı piyasalar: https://canlipiyasalar.haremaltin.com/ ve `/js/vue.js`
- Harem kesintisi (15.08.2026): https://www.gzt.com/ekonomi/harem-altin-neden-acilmiyor-ne-zaman-duzelecek-harem-altin-coktu-mu-neden-hata-veriyor-4251454
- KuyumSmart SSS: https://kuyumsmart.com.tr/sss · KuyumHub: https://kuyumhub.com/
- Hasfiyat: https://altinapi.hasfiyat.com/ · https://dev.to/hasfiyat/building-a-real-time-gold-fx-price-ticker-with-websocket-socketio-g36
- altinapi: https://pkg.go.dev/github.com/altinapi/altinapi-go
- Altınkaynak: https://static.altinkaynak.com/public/Gold · https://www.altinkaynak.com/ (JS paketi) · robots
- Truncgil: https://finans.truncgil.com/ · `/today.json` · `/v4/today.json`
- GenelPara: https://api.genelpara.com/ · https://www.genelpara.com/yasal-uyari/
- Ankara robots: https://www.ankaraaltinborsasi.com/robots.txt · Bigpara robots: https://bigpara.hurriyet.com.tr/robots.txt · Doviz.com robots: https://www.doviz.com/robots.txt
- altin.in: https://altin.in/bilgi/hakkimizda · Nadir: https://www.nadirdoviz.com/ · İKO: https://fiyat.iko.org.tr/ · iab.com.tr · kapalicarsi.com.tr · kuyumcukent.com.tr
- TCMB EVDS Kullanım Şartları: https://evds3.tcmb.gov.tr/igmevdsms-dis/documents/showDocument?docId=18 · TCMB site şartları: https://www.tcmb.gov.tr/wps/wcm/connect/TR/TCMB+TR/Bottom+Menu/Diger/Kullanim+Sartlari · https://www.tcmb.gov.tr/kurlar/today.xml
- BIST KMTP verileri: https://borsaistanbul.com/veriler/kiymetli-madenler-ve-kiymetli-taslar-piyasasi · Veri dağıtım SSS: https://www.borsaistanbul.com/sss/veri-dagitim-ve-endeks-lisanslama · Veri ürünleri: https://www.borsaistanbul.com/en/data/data-dissemination/market-data-products · Dağıtıcılar: https://borsaistanbul.com/veriler/veri-yayini/veri-dagitici-kuruluslar · Telif: https://borsaistanbul.com/telif-hakki-ve-cekince-ihbari
- 23/e ikincil kaynaklar: https://www.alomaliye.com/2014/09/09/kulce-ve-tane-bicimindeki-altin-teslimlerine-iliskin-kdv-indirimi/ · https://www.lexpera.com.tr/mevzuat/ozelgeler/39044742-kdv-30-2200-sayili-ozelge-1 · birincil metin: `dokumanlar/01-gereksinimler/musteri-gorusmesi-2026-10-16.md` § 23/e (mevzuat.gov.tr 3065, GİB kdv_genteb.pdf)
- LBMA: https://www.lbma.org.uk/prices-and-data/precious-metal-prices · Metals-API: https://metals-api.com/pricing · Twelve Data: https://twelvedata.com/pricing · Frankfurter: https://frankfurter.dev/docs/ · exchangerate.host: https://exchangerate.host/ · Alpha Vantage: https://www.alphavantage.co/support/ · GoldAPI (ücretsiz paket, eski kaynak): https://dev.to/aershov24/gold-prices-api-how-to-get-real-time-historical-gold-silver-json-prices-from-goldapi-io-in-30-secs-iad · open.er-api: https://open.er-api.com/v6/latest/USD
- Hukuk: https://www.goksusafiisik.av.tr/tr/publications/2025-summer-issue/web-scraping-eyleminin-haksiz-rekabet-acisindan-degerlendirilmesi?id=510 · https://www.mondaq.com/turkey/copyright/1304748/web-kaz%C4%B1ma-web-scraping-ve-internet-sitelerine-y%C3%B6nelik-koruma · https://dergipark.org.tr/tr/download/article-file/628136
- Kuveyt Türk API: https://webrazzi.com/2017/11/27/kuveyt-turk-api/ · https://developer.kuveytturk.com.tr

*Geçici inceleme dosyaları (HTML/JS/JSON) şu dizinde: `/private/tmp/claude-501/-Users-ramazanakkocan-ByGatech-yerel-jewellery/4c95c907-27bb-460c-90cb-06e16b05f6ef/scratchpad/kur/`. Proje ağacına hiçbir şey yazılmadı.*
