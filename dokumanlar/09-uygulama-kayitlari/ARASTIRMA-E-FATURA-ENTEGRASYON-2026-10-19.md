# e-Fatura / e-Arşiv'i arka planda otomatik göndermek: araştırma

**Tarih:** 19 Ekim 2026 · **Kapsam:** yalnız herkese açık sayfalar okundu. Hesap açılmadı, form doldurulmadı, giriş yapılmadı. Kod değiştirilmedi.
**Önceki çalışma:** `ARASTIRMA-MASAK-EFATURA-BAGLANTI-2026-10-14.md` § B (UBL-TR, ÖZELMATRAH 805/808/230, 12.000 TL fatura haddi). Burada tekrar edilmedi, yalnız atıf yapıldı.

| İşaret | Anlamı |
|---|---|
| ✅ | Birincil metin (GİB tebliği, kılavuzu, listesi) okundu |
| 🟦 | Firmanın kendi sitesi / belgesi (üretici beyanı) |
| 🟡 TEYİTSİZ | İkincil kaynak, eksik ya da yorum; teyit gerekir |
| ❌ | Aranıp birincil kaynakta bulunamadı |

---

## Sade özet (6 satır)

1. **Fatura iki yoldan GİB'e gider.** Alıcı e-Fatura'ya kayıtlıysa fatura GİB üzerinden alıcının kutusuna gider (e-Fatura). Kayıtlı değilse fatura alıcıya e-posta ya da kâğıtla verilir, GİB'e ertesi gün sonuna kadar rapor edilir (e-Arşiv). ✅
2. **1 Ocak 2026'dan beri** bilanço esasına göre defter tutan her kuyumcu, e-Fatura'ya kayıtlı olmasa bile **tutarı ne olursa olsun** her faturayı e-Arşiv kesmek zorunda. Basit usul ve işletme defteri tutanlar için tarih **1 Ocak 2027**. Kuyumcuya özel ayrı bir kural yok. ✅
3. **Bir yazılımın yasal ve gerçekçi tek yolu, GİB izinli bir özel entegratörün API'si.** GİB portalını taklit eden kütüphaneler resmî bir kanal değil ve her imzada SMS onayı istiyor. ByGatech'in kendisinin entegratör olması ise ISO belgeleri, bağımsız denetim, ikinci şehirde felaket merkezi ve bir yıllık test istiyor. Bu yollar uygun değil. ✅
4. **GİB listesinde bugün 93 entegratör var.** Listede TÜRMOB'a bağlı **TÜRMOB-TESMER (Luca e-Entegratör)** de yer alıyor. Ancak Luca'nın yazılımlara açık bir API belgesi bulunamadı. ✅ liste / ❌ API belgesi
5. **Belgesi herkese açık, test ortamı olan entegratörler:** Nilvera (REST, `apitest.nilvera.com`) ve BirFatura (REST, Swagger, ücretsiz test hesabı). İzibiz SOAP kullanıyor ve GitHub'da resmî örnek kodu var. Diğerlerinin belgesi sözleşmeden sonra veriliyor. 🟦
6. **Öneri:** Mihenk'e entegratörden bağımsız bir bağlantı katmanı yazılsın; ilk bağlanacak entegratör Nilvera olsun, İzibiz yedek olsun. Kuyumcu entegratörle kendisi sözleşme yapar; tüzel kişiyse mali mühür, şahıs işletmesiyse e-imza alır ve API anahtarını Mihenk'e girer. Gerisini (kayıtlı mı sorgusu, gönderim, durum takibi, e-posta, iptal) Mihenk arka planda yapar.

---

## 1. Faturalar nereye gidiyor?

### 1.1 Üç kullanım yöntemi (509 Sıra No'lu VUK Genel Tebliği V.1)

| Yöntem | Nasıl çalışır | Kaynak | Durum |
|---|---|---|---|
| **GİB Portal** | GİB'in web ekranında elle kesilir. GİB, portalda hangi belgelerin kesilebileceğini sınırlayabilir | 509 V.1.1 | ✅ |
| **Özel Entegratör** | GİB izinli firmanın sistemi belgeyi oluşturur, mali mühürle onaylar, zaman damgası basar, alıcıya iletir ve raporu GİB'e gönderir. Sistem Türkiye'de olmak zorunda | 509 V.1.2 | ✅ |
| **Doğrudan Entegrasyon** | Mükellefin kendi sistemi GİB'e bağlanır. GİB faaliyet konusu, hasılat, sermaye büyüklüğü, belge sayısı gibi şartlar belirler. Başvurudan sonra bir yıl içinde bitirilmesi gerekir | 509 V.1.3 (573 ile değişik) | ✅ |

Kaynak: [509 Sıra No'lu Tebliğ, güncel hâli (515, 526, 535, 550, 573 işlenmiş), GİB](https://cdn.gib.gov.tr/api/gibportal-file/file/getFile?objectKey=MEVZUAT_TEBLIGLER%2FUNIVERSAL%2F2026%2FMEVZUAT_TEBLIGLER_2026_VukTeb509_Guncel.pdf) · 589 değişikliği: [alomaliye.com, RG 31.12.2025/33124 5. Mük.](https://www.alomaliye.com/2025/12/31/vergi-usul-kanunu-genel-tebligi-sira-no-589/) (589'un metni ayrıca GİB PDF'inde IV.2.4.3'e işlenmiş) ✅

### 1.2 Akış: kim neyi gönderir, kim neyi raporlar

| Konu | e-Fatura (alıcı kayıtlı) | e-Arşiv (alıcı kayıtlı değil: birey ya da kayıtsız şirket) |
|---|---|---|
| Fatura nereye gider | Satıcı → (entegratör) → **GİB** → alıcının entegratörü / posta kutusu. GİB sistemi dışından gönderilen belge e-Fatura sayılmaz (509 V.5.1) ✅ | Alıcıya **doğrudan** gider; alıcı isterse kâğıt, isterse elektronik (internet dahil her türlü elektronik ortam). Elektronik gönderildiyse kâğıda basmak gerekmez (509 V.5.2) ✅ |
| GİB'e ne gider | Faturanın kendisi (zarf) | **e-Arşiv Raporu:** günlük dönemler hâlinde, **en geç izleyen günün sonuna kadar**, mali mühür ya da e-imza ve zaman damgasıyla. Bunu entegratör yapar ([e-Arşiv Teknik Kılavuzu V.1.19, 14.10.2026, § 5](https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/e-Arsiv_Teknik_Kilavuzu_V.1.19.pdf); V.1.17'de de aynı) ✅ |
| Kim raporlar | Entegratör | Entegratör (portalda kesilen faturayı GİB zaten görür) ✅ |
| Saat bilgisi | — | e-Arşiv faturada tarihin yanında **saat ve dakika** zorunlu (509 V.5.2) ✅ |
| Belgedeki ibare | GİB amblemi, "e-Fatura" | GİB amblemi, "e-Arşiv Fatura"; elektronik iletildiyse altında "e-Arşiv izni kapsamında elektronik ortamda iletilmiştir." ibaresi (509 V.3) ✅ |
| Alıcıya e-posta / SMS | — | Tebliğ "her türlü elektronik araç ve ortam" diyor. Entegratörler e-posta gönderimini hizmete katıyor (Logo: 1 e-posta kontöre dahil 🟦). SMS ile gönderime özel bir hüküm aranmadı/bulunamadı | ✅ e-posta yolu açık · 🟡 SMS |
| İnternetten satış | — | İnternetten satılan malın e-Arşiv faturası **elektronik** gönderilmek zorunda (509 IV.2.4.5) ✅ |

**Hangi senaryo, nasıl seçilir?** Alıcının vergi numarası GİB'in kayıtlı kullanıcı listesinde varsa e-Fatura (TEMELFATURA ya da TICARIFATURA), yoksa EARSIVFATURA. Yazılım her satışta bu sorguyu yapar (entegratör API'leri "mükellef sorgula" hizmeti sunuyor 🟦). GİB'in kayıtlı kullanıcı listesi kılavuzunun yeni sürümü **16.11.2026'ya kadar** uygulanmalı; bu işi entegratör yapar ([e-Belge duyuruları, 30.09.2026](https://ebelge.gib.gov.tr/duyurular.html)) ✅

### 1.3 Zorunluluk sınırları (2026)

| Kural | Değer | Kaynak | Durum |
|---|---|---|---|
| e-Fatura'ya geçiş haddi | 2022 ve sonrası brüt satış hasılatı **3 milyon TL** ve üzeri. Geçiş, izleyen yılın 7. ayının başında (2025 hasılatı aşan → 1.7.2026) | 509 IV.1.4/a-1, IV.1.5/a | ✅ |
| İnternetten satış | **500 bin TL** ve üzeri → e-Fatura ve e-Arşiv birlikte | 509 IV.1.4/a-4, IV.2.4.2 | ✅ |
| e-Fatura'ya kayıtlı olanlar | e-Arşiv'e de geçmek zorunda. Kayıtlı alıcıya e-Fatura, diğerlerine e-Arşiv kesilir | 509 IV.2.4.1 | ✅ |
| **e-Fatura'ya kayıtlı olmayanlar** | 2025'te vergiler dahil 3.000 TL üstü faturalar; **1.1.2026'dan itibaren tutarına bakılmaksızın tüm faturalar e-Arşiv**. Basit usul ve işletme defteri tutanlar için bu tarih **1.1.2027** (2026 boyunca 3.000 TL sınırı geçerli) | 509 IV.2.4.3 (573 ve 589 ile değişik) | ✅ |
| Kâğıt kesilirse | Her kâğıt fatura için VUK 353. madde cezası; kesen ve (nihai tüketici değilse) alan için ayrı ayrı | 509 IV.2.4.3 | ✅ |
| **Kuyumcuya özel kural** | **Yok.** Zorunlu sektörler: EPDK lisanslıları, ÖTV (III) listesi, sebze-meyve, sağlık/SGK, gayrimenkul ve taşıt, otel, şarj. Kuyum bu listede değil. "Kıymetli maden" ifadesi yalnız e-Döviz ve Kıymetli Maden Alım/Satım Belgesinde geçiyor (döviz büfeleri ve aracı kurumlar) | 509 IV.1.4/a (1–9), V.5.10 | ✅ |
| GİB'in tek tek zorunlu tutma yetkisi | GİB, riskli bulduğu mükellefe sektöre ve ciroya bakmadan yazıyla zorunluluk getirebilir (en az 3 ay süre verir) | 509 IV.1.4/ğ, IV.2.4.4 | ✅ |
| Fatura düzenleme haddi 2026 | 12.000 TL (bu tutarın altında ÖKC fişi yeterli; üstünde fatura zorunlu) | önceki araştırma B4.1 (588 Sıra No) | ✅ |

**Kuyumcu açısından anlamı:** Bilanço esasına göre defter tutan bir kuyumcunun bugün kestiği **her fatura e-Arşiv ya da e-Fatura olmak zorunda**. Ziynet satışlarının çoğu 12.000 TL'yi geçtiği için neredeyse her satış fatura gerektiriyor. Bu yüzden e-belge bağlantısı satış için ön koşul. ✅ (metin) · 🟡 (her kuyumcunun defter türü mali müşavirle teyit edilmeli)

### 1.4 İptal ve itiraz

| Konu | Kural | Kaynak | Durum |
|---|---|---|---|
| Yasal dayanak | TTK 21/2: fatura alan kişi **8 gün** içinde itiraz etmezse içeriği kabul etmiş sayılır | Kılavuzlar § 2 | ✅ |
| e-Fatura, TİCARİ senaryo | Alıcı, faturanın kendisine iletildiği tarihten itibaren **8 gün** içinde sistem içinden "Ret" yanıtıyla iptal eder, ya da harici yolla itiraz eder | [e-Fatura İptal/İtiraz Kılavuzu V.1.2](https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/e-Fatura_Iptal_Ihtar_Itiraz_Bildirim_Kilavuzu_V_1.2.pdf) | ✅ |
| e-Fatura, TEMEL senaryo | Sistem içinden ret yok. **8 gün** içinde harici yollarla itiraz edilir: noter, taahhütlü mektup, telgraf ya da KEP. İptal talebi GİB'in iptal portalından açılır ve karşı tarafça onaylanır | aynı kılavuz · [GİB Fatura İptal Portalı](https://portal.efatura.gov.tr/FaturaIptal/) | ✅ |
| e-Arşiv iptali | 8 günlük süre, faturanın **alıcıya iletildiği** tarihten başlar. İptal her durumda 8 gün içinde yapılmalı. Entegratör kullanan, iptal talebini GİB'e entegratör üzerinden gönderir; alıcı kayıtlı e-Belge kullanıcısıysa portaldan onaylar ya da reddeder. Karşı tarafın onaylama zorunluluğu yok | [e-Arşiv Uygulamaları İptal/İtiraz Kılavuzu V.1.1, Ocak 2025](https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/e-Arsiv_Uygulamalari_Iptal_Ihtar_Itiraz_Bildirim_Kilavuzu_V.1.1.pdf) § 2–3 | ✅ |
| İtirazın kabul tarihi | Kabul işlemi faturanın ait olduğu ayı izleyen ayın **15'inci** günü sonuna kadar yapılırsa Ba/Bs ve KDV beyanına yansır (eski sürümde 20'nci gün) | e-Fatura kılavuzu V.1.2 · e-Arşiv kılavuzu V.1.1 | ✅ |
| Nihai tüketiciye (bireye) kesilen e-Arşiv'in iptali | Bireyin GİB'de kullanıcısı yok. İptal, e-Arşiv raporundaki `faturaIptal` alanıyla bildiriliyor (Teknik Kılavuz § 3.3.2.24). Bireye kesilen faturada süre ve onay akışı kılavuzda ayrı yazılmamış | Teknik Kılavuz V.1.17 | 🟡 TEYİTSİZ |
| Süre geçtikten sonra | Uygulamada alıcı iade faturası keser. Bu kural blog kaynaklarından; birincil metinde aranmadı | [parasut.com](https://www.parasut.com/blog/e-fatura-nasil-iptal-edilir) | 🟡 TEYİTSİZ |

**Mihenk için:** "Faturayı iptal et" düğmesi, faturanın iletim tarihinden itibaren **8 gün** açık kalmalı. Sonrasında düğme yerine "İade faturası kes" çıkmalı.

---

## 2. Bir yazılım şirketi faturayı GİB'e nasıl gönderir?

### (a) Özel entegratörün API'si (önerilen yol)

- Kuyumcu entegratörle sözleşme yapar. Entegratör GİB kaydını kendisi yapar: anlaştığı mükellefin bilgisini GİB'e elektronik olarak bildirir, onay gelince hizmete başlar (509 V.1.4). ✅
- e-Arşiv'i entegratör üzerinden kullanacak mükellefin **GİB'e ayrıca başvurmasına gerek yok** ([ebelge.gib.gov.tr/earsivbasvuru.html](https://ebelge.gib.gov.tr/earsivbasvuru.html)). ✅
- Mükellef, belgelerinin **entegratörün mali mührü ve zaman damgasıyla** onaylanmasını isteyebilir (509 V.1.2 ve V.9). Böylece her faturada kuyumcunun kart takması gerekmez. ✅
- Entegratör, müşterisinin ticari sırrını başka amaçla kullanamaz ve üçüncü kişilerle paylaşamaz (509 V.1.2). ✅
- Mihenk API anahtarıyla faturayı gönderir, durumunu sorgular, PDF'ini alır. Belge biçimi her entegratörde UBL-TR 1.2.1; API'nin şekli ise entegratöre göre değişiyor (§ 4). ✅ biçim / 🟦 API

### (b) GİB e-Arşiv Portal / e-Fatura Portal

- **Resmî bir yazılım API'si var mı?** Portalın web servisi var, ama **yalnız GİB'den izin almış özel entegratörlere** açık. İzin için yazılı başvuru, BİS raporu ve GİB'e demo sunumu gerekiyor. Faturalar GİB'e **anlık** iletilir: *"Başkanlık sistemlerine iletilmeyen e-Arşiv Faturalar hiç düzenlenmemiş sayılır."* Sistem çevrimdışı çalışamaz. ([e-Arşiv Fatura Portali Özel Entegratör Entegrasyon Kılavuzu, Eylül 2021, § 2–3.1](https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/e-Arsiv_Fatura_Portali_Entegrasyon_Kilavuzu.pdf)) ✅
- **Topluluk kütüphaneleri** (ör. [mlevent/fatura](https://github.com/mlevent/fatura), furkankadioglu/efatura ve fatura.js'den türeyenler) portala kullanıcı kodu ve şifreyle giriş yapıp portalın kendi ekran çağrılarını kullanıyor. Resmî ürün değiller. README'ye göre imzalamak için **portalda kayıtlı telefona gelen SMS kodu** gerekiyor ve sorumluluk kullanıcıda. 🟦
  - Arka planda otomatik göndermeye engel: her imzada bir insan SMS kodu girmek zorunda.
  - Kuyumcunun portal şifresini yazılımda saklamak gerekir.
  - GİB ekranı değişince kütüphane sessizce bozulur.
- **Hukuki durum:** Portalın üçüncü taraf yazılımla kullanılmasını **açıkça yasaklayan** bir GİB metni bulunamadı ❌. Ancak 509, yazılım entegrasyonu için yalnız özel entegratör ve doğrudan entegrasyon yollarını tanımlıyor. Portal ise *"entegre etmek konusunda yeterli alt yapıya sahip olmayan kullanıcılar"* için ve elle kullanım amacıyla açılmış (509 V.1.1) ✅. **Sonuç:** gri alan; satılan bir ürünün dayanağı yapılamaz. 🟡 (avukat görüşü alınmadı)

### (c) Doğrudan entegrasyon

- Doğrudan entegrasyonu **kuyumcunun kendisi** (mükellef) yapar. ByGatech kuyumcu adına GİB'e bağlanamaz; her kuyumcunun ayrı başvurusu gerekir. ✅ (509 V.1.3)
- Gereken: GİB'in belirlediği ölçek şartları (573 ile eklendi: faaliyet, hasılat, aktif ya da öz sermaye, belge sayısı), BİS raporu, test hesabıyla bir yıl içinde test, kendi mali mühür ya da HSM. e-Arşiv için doğrudan entegrasyonda TÜRKAK onaylı ISO 27001, ISO 22301 ve ISO 20000 belgeleri isteniyor ([e-Arşiv Başvuru Kılavuzu V.1.8, 22.05.2026](https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/e-ArsivBasvuruKilavuzu.V.1.8.pdf)). ✅
- Sayısal ölçek eşikleri (ör. "X milyon TL hasılat") bu turda bulunamadı. ❌
- **Kuyumcu için uygun değil.**

### (d) ByGatech'in kendisinin özel entegratör olması

| Şart | Ayrıntı | Kaynak | Durum |
|---|---|---|---|
| Başvuru | Dilekçe ve Özel Entegrasyon BİS Raporu; e-Fatura ve e-Arşiv için **ayrı ayrı** izin | 509 V.1.2 · e-Arşiv Başvuru Kılavuzu V.1.8 § 3 | ✅ |
| ISO belgeleri | TÜRKAK onaylı **ISO 27001**, **ISO 22301**, **ISO 20000**. Başvuruda en az biri olmalı, diğerleri için temin takvimi verilmeli | [Özel Entegrasyon Kılavuzu v1.14, 29.06.2026](https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/e-Fatura_Uygulamasi_Ozel_Entegrasyon_Kilavuzu_v1.14.pdf) · e-Arşiv Başvuru Kılavuzu V.1.8 | ✅ |
| Bağımsız bilgi sistemleri denetimi (ÖEBSD) | Başvuru dosyasına denetim görüşü ve raporu eklenmeli. Denetimi bankacılık ve sermaye piyasası mevzuatına göre yetkili bir bağımsız denetim kuruluşu yapar. Rapor en çok 2 yıl geçerli; olumsuz görüş faaliyetin durdurulması demek | [ÖEBSD Kılavuzu, Kasım 2019](https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/e-BELGE_OZEL_ENTEGRATORLERI_BILGI_SISTEMLERI_DENETIMI_KILAVUZU.pdf) | ✅ |
| Altyapı | **Başka bir ilde felaket kurtarma merkezi** (veritabanı en çok 30 dakika gecikmeli; 6 saat içinde geçiş), 7/24 iş sürekliliği, FIPS 140-2 Düzey 3 ya da EAL4+ donanım, yılda en az bir sızma testi, kayıtların 10 yıl saklanması, ITIL uyumlu yönetim | ÖEBSD Kılavuzu § 8 · Özel Entegrasyon Kılavuzu v1.14 | ✅ |
| Personel | Görev ayrılığı zorunlu (ikiz görev yok), ITIL sertifikalı personel; personel değişiklikleri 15 gün içinde GİB'e bildirilir | ÖEBSD § 12 · v1.14 | ✅ |
| Süre | Entegrasyon testleri, çalışmalara başlandıktan sonra **bir yıl içinde** bitmeli; ardından GİB izin yazısı | v1.14 § 2 · 509 V.1.3 | ✅ |
| Sermaye / şirket türü | GİB kılavuzlarında sermaye şartı bulunamadı. Listede limited şirketler de var (Nilvera Ltd., Bizofis Ltd., Nox Ltd.), yani anonim şirket şartı görünmüyor | Kılavuzlar · [GİB listesi](https://ebelge.gib.gov.tr/efaturaozelentegratorlerlistesi.html) | ❌ sermaye şartı bulunamadı · ✅ limited şirketler listede |
| TSE / TÜBİTAK denetimi | Denetim TSE ya da TÜBİTAK tarafından değil, yetkili bağımsız denetim firmasınca yapılıyor. Mali mühür TÜBİTAK Kamu SM'den alınıyor | ÖEBSD · 509 V.9 | ✅ |
| Maliyet | ISO belgeleri, denetim, ikinci veri merkezi, HSM ve personel maliyeti için birincil bir rakam yok | — | 🟡 TEYİTSİZ (bilinmiyor) |

**Sonuç:** En az 1–2 yıl sürer, kalıcı bir altyapı ve uyum ekibi ister. Mihenk'in satış takvimine uymaz. İleride ölçek oluşursa ayrı bir iş kararı olarak ele alınabilir.

---

## 3. TÜRMOB ve GİB'in özel entegratör listesi

- **GİB'in güncel listesi:** [ebelge.gib.gov.tr/efaturaozelentegratorlerlistesi.html](https://ebelge.gib.gov.tr/efaturaozelentegratorlerlistesi.html). 19.10.2026'da çekildi; **93 firma** sayıldı. Liste 421 Sıra No'lu Tebliğ kapsamındaki özel entegratörleri gösteriyor; hangi belge türü için izinli oldukları listede yazmıyor. 14.10'daki araştırmada "~105" diye geçen sayı yaklaşıktı; bugün tablo satır satır sayıldı. ✅
- **TÜRMOB:** Listenin 60. sırasında **"TÜRMOB-TESMER Eğitim Yayın ve Yazılım Hizmetleri İktisadi İşletmesi"** (Ankara) var. İletişim adresi `entegratordestek@luca.com.tr`; yani Luca e-Entegratör bu işletmenin ürünü. ✅
  - Luca'nın tanıtımı "LUCA bir TÜRMOB · TESMER projesidir" diyor ([turmobeimza.com.tr broşürü](https://www.turmobeimza.com.tr/upload/docs/genel_brosur_luca.pdf)). 🟦
  - "TÜRMOB Luca e-Entegratör" mobil uygulaması ücretsiz; belgeler kontörle kesiliyor ([App Store](https://apps.apple.com/eg/app/t%C3%BCrmob-luca-e-entegrat%C3%B6r/id6448848501)). 🟦
  - **Dış yazılımlara açık bir API ya da web servis belgesi bulunamadı.** ❌
  - **Kontör fiyatı bulunamadı.** ❌
  - Bir karşılaştırma sitesi, Luca'nın e-dönüşüm altyapısında İŞNET'in kullanıldığını yazıyor; TÜRMOB–İŞNET iş birliği haberi de var ([dunya.com](https://www.dunya.com/sirketler/turmob-ve-isnet-isbirligi-anlasmasi-imzaladi-haberi-244130)). TESMER'in listede kendi adıyla yer aldığı ise kesin. 🟡 TEYİTSİZ (hangi altyapının hangi dönemde kullanıldığı)
  - **Mihenk açısından anlamı:** Kuyumcunun mali müşaviri Luca kullanıyorsa faturaların Luca'ya akması önemli bir kolaylık. Ancak bu, gönderim kanalı olarak değil, **muhasebeye aktarım** olarak ele alınmalı. Luca'nın API'si yoksa bunun yolu, kuyumcunun hangi entegratörü kullandığına bakmadan mali müşavirin gelen e-faturaları ve e-Arşiv'leri GİB'den çekmesidir. 🟡

**Listeden bilinen adlar (tam liste GİB sayfasında):** Uyumsoft (2 şirket), Logo, İzibiz, QNB eSolutions, İŞNET, EDM, Sovos (FIT), Turkcell e-Şirket (ePlatform), Superonline, Türk Telekom, Mikro, Kolaysoft, Zirve, Doğan e-Dönüşüm, Avalara (Inposia), Mysoft, BirFatura, Nilvera, Trendyol e-Faturam (DSM), TÜRMOB-TESMER, TÜRKKEP, Sabancı Dx, Veriban, Pavo, Token, Worldline, Toshiba GCS, Hızlı Bilişim, Faturadijital (Cevizsoft) ve diğerleri. ✅

---

## 4. Yazılım firmalarına API açan entegratörler

| Entegratör | GİB listesi | API türü | Herkese açık belge / test ortamı | Fiyat modeli (kaynaktan) | Kaynak |
|---|---|---|---|---|---|
| **Nilvera** | ✅ (Kayseri, Ltd.) | **REST** (OpenAPI 3), Bearer anahtar | Belge herkese açık; test adresi `apitest.nilvera.com`. Anahtar Nilvera portalından üretiliyor, yani hesap gerekiyor | Kontör paketi. Bir bayi sayfasında: 1.000 kontör 2.375 TL, 10.000 kontör 13.000 TL, KDV hariç, 24 ay geçerli (Nilvera'nın kendi listesi değil) | [developer.nilvera.com](https://developer.nilvera.com) 🟦 · [ideasoft — Nilvera](https://www.ideasoft.com.tr/eticaret-cozum-merkezi/earsiv-efatura-cozumleri/nilvera/) 🟡 |
| **BirFatura** | ✅ (Ankara, A.Ş.) | **REST**, Swagger | Geliştirici sitesi ve ücretsiz test (sandbox) hesabı. API sayfası daha çok sipariş → fatura akışını anlatıyor; doğrudan UBL gönderimi var mı, belge okunmadan belli değil | Kontör: 500 kontör 400 TL; 50.000 kontörde birim fiyat 0,47 TL; "kontörlerin süresi yok" | [birfatura.com — API](https://birfatura.com/birfatura-api-entegrasyonu/) · [kontör fiyatları](https://birfatura.com/e-fatura-kontor-fiyatlari-e-fatura-fiyatlari/) 🟦 · 🟡 (UBL gönderimi) |
| **İzibiz** | ✅ | Web servis (belge "SOAP" demiyor; topluluk SOAP diye anlatıyor) | Resmî GitHub örneği (`izibiz/ws-client-dotnet`): kimlik doğrulama, e-Fatura, e-Arşiv, e-İrsaliye ve mutabakat. Test servis ayarları projede var | Güncel liste bulunamadı | [github.com/izibiz/ws-client-dotnet](https://github.com/izibiz/ws-client-dotnet) 🟦 · ❌ fiyat |
| **Logo (e-Logo)** | ✅ | Belgede yazmıyor (Connect lisansı ayrı satılıyor) | Herkese açık API belgesi bulunamadı | **Fiyat listesi herkese açık (25.09.2026'dan itibaren):** 150 kontör 2.250 TL (15,00 TL/adet) … 10.000 kontör 66.100 TL (6,61 TL) … 1.000.000 kontör 2.120.000 TL (2,12 TL). KDV hariç, süresiz. 1 e-Fatura = 1 kontör, 1 e-Arşiv = 1 kontör (1 e-posta dahil). Mükellef sorgusu: VKN ile 2, TCKN ile 3 kontör | [e-Logo fiyat listesi PDF](https://cdn.logo.com.tr/files/logocomtr//Uploads/Documents/elogo-ecozumler-fiyat-listesi.pdf) 🟦 · ❌ API belgesi |
| **QNB eSolutions (eFinans)** | ✅ | Bağlayıcı (connector) ve web servis; topluluk istemcileri SOAP | Resmî açık belge bulunamadı | Bulunamadı | [e-invoice.app](https://www.e-invoice.app/vendors/qnb-efinans) 🟡 · ❌ |
| **Sovos (FIT / Foriba)** | ✅ | SOAP (topluluk paketlerinde GetUblList, GetEnvelopeStatus vb.) | İngilizce süreç belgesi açık; web servis belgesi müşteriye veriliyor | Bulunamadı | [docs.sovos.com — Türkiye](https://docs.sovos.com/indirect-tax/indirect-tax-products/einvoicing/compliance-network/country-setup-guides/turkey/supported-business-processes) 🟦 · [ahmeti/sovos](https://packagist.org/packages/ahmeti/sovos) 🟡 |
| **Uyumsoft** | ✅ (2 şirket) | Web servis | Açık belge ya da test adresi bulunamadı | Bulunamadı | ❌ |
| **EDM** | ✅ | GİB kılavuzuna göre SOAP | Bilgiler EDM'den alınıyor; açık belge bulunamadı | Bulunamadı | ❌ |
| **Mysoft** | ✅ | — | Yalnız topluluk PHP paketi var (resmî değil) | Bulunamadı | [packagist phpdev/mysoft](https://packagist.org/packages/phpdev/mysoft) 🟡 |
| **Kolaysoft** | ✅ | — | Bulunamadı | Bulunamadı | ❌ |
| **TÜRMOB-TESMER (Luca)** | ✅ | — | Bulunamadı | Kontörlü; birim fiyat bulunamadı | § 3 |

**Mali mühür (kuyumcu tüzel kişiyse):** TÜBİTAK Kamu SM'den alınır, 3 yıl geçerli. Fiyat sayfası ([mm.kamusm.gov.tr/fiyatlandirma.jsp](https://mm.kamusm.gov.tr/fiyatlandirma.jsp)) tutarları sayfa yüklendikten sonra getiriyor; tutar okunamadı. Kamu SM 01.01.2026 ve 01.08.2026'da fiyatları güncellediğini duyurdu. İkincil kaynaklarda 2.040 TL (KDV dahil) ile 2.500 TL + KDV arası rakamlar var. 🟡 TEYİTSİZ

**Bayi / çözüm ortağı programları:** BirFatura'nın "İş Ortaklığı" sayfası var ve entegre eden yazılımları kendi listesinde gösteriyor 🟦. Nilvera, İzibiz, Uyumsoft ve Turkcell e-Şirket için yazılım evlerine yönelik herkese açık bir program sayfası bulunamadı ❌. Bayilik koşulları satış görüşmesiyle netleşir.

**Rakiplerin kullandığı entegratör:**
- **Faturalog:** Sitesinde entegratör adı, "GİB onaylı" ya da "iş ortağı" ifadesi yok. "Çözüm ortaklarımız" altındaki logolar kuyumcu işletmelerine ait. Faturalog GİB listesinde de **yok**. Yani arkada bir entegratör kullanıyor, ama hangisi olduğu bilinmiyor ([faturalog.com](https://faturalog.com/)). 🟡 TEYİTSİZ. Kontör fiyatı 1,50–2,50 TL (14.10 araştırması, faturalog.com/fiyatlandirma 🟦).
- **Pusula / Kivi:** 14.10 araştırması: Kivi GİB listesinde yok; arkadaki entegratör yazılmıyor. 🟡
- Diğer kuyumcu programları için kaynak bulunamadı. ❌

---

## 5. Öneri: Mihenk için en hızlı ve hukuken temiz yol

### 5.1 Karar

**Yol (a): özel entegratör API'si. Kuyumcu entegratörle kendisi sözleşme yapar, Mihenk API ile gönderir.**

- **Hukuken temiz:** Fatura 509'da tanımlı bir yolla gider. Raporlama, saklama (10 yıl), mühür ve zaman damgası entegratörde kalır. ByGatech "fatura düzenleyen" ya da "aracı" konumuna girmez; yazılım sağlayıcısı olarak kalır. Kuyumcunun portal şifresi ya da e-imzası ByGatech'te durmaz.
- **Hızlı:** GİB'e ByGatech adına başvuru yok. Tek iş, Mihenk'te bir bağlantı katmanı yazmak.
- **Entegratör seçimi (ekip oyu: Bertan, Taşkın, Mehmet):**
  1. **Nilvera ilk bağlantı:** REST, açık belge, test adresi, kontör modeli.
  2. **İzibiz ikinci bağlantı:** büyük ve yerleşik, resmî örnek kodu var. Kuyumcu zaten İzibiz kullanıyorsa ona da bağlanılır.
  3. Kuyumcu başka bir entegratörle çalışıyorsa (Logo, QNB, Uyumsoft, Luca…), o entegratörün belgesi sözleşmeyle alınır ve aynı katmana yeni bir bağlantı olarak eklenir.
  - Seçim **üretici beyanına** dayanıyor (🟦). Canlıya geçmeden önce entegratörün test ortamında ÖZELMATRAH (805/808) ve 230 istisna kodlarıyla gerçek bir kuyum faturasının kabul edildiği kanıtlanmalı.

### 5.2 Kuyumcudan istenenler

| # | Ne | Neden | Kaynak |
|---|---|---|---|
| 1 | Bir entegratörle sözleşme (çoğunda çevrim içi) | Entegratör GİB kaydını yapar; e-Arşiv için GİB'e ayrıca başvuru gerekmez | 509 V.1.4 · earsivbasvuru ✅ |
| 2 | **Tüzel kişiyse mali mühür** (Kamu SM, 3 yıllık). **Şahıs işletmesiyse e-imza (NES)** | e-Fatura kaydı ve mühür için. Entegratörün mührüyle imzalama da istenebilir; ancak kaydın kendisi için kuyumcunun mührü ya da e-imzası gerekip gerekmediği entegratöre göre değişebilir | 509 V.9 ✅ · ikincil kaynaklar 🟡 |
| 3 | Entegratör panelinden üretilen **API anahtarı** (ya da kullanıcı ve şifre), Mihenk ayarlarına girilir | Mihenk'in kuyumcu adına gönderebilmesi için | 🟦 |
| 4 | Mali müşavir teyidi: defter türü (bilanço / işletme / basit usul) ve e-Fatura'ya kayıtlı olup olmadığı | Zorunluluk tarihi buna bağlı (1.1.2026 ya da 1.1.2027) | 509 IV.2.4.3 ✅ |
| 5 | Fatura numarası ön eki (3 harf) ve logo | Entegratör numaralandırması | 🟡 (önceki araştırma B3.6) |

### 5.3 Bizim bir kerelik işlerimiz

| # | İş | Not |
|---|---|---|
| E-1 | **Entegratörden bağımsız bağlantı arayüzü:** gönder · durum sorgula · PDF/HTML al · mükellef sorgula · iptal et · gelen faturaları listele | İlk bağlantı Nilvera, ikinci İzibiz |
| E-2 | API anahtarının şifreli saklanması (işletme başına) | 14.10 araştırması § 3 notu: entegratör anahtarını şifreli saklamak doğru ve olağan |
| E-3 | UBL-TR 1.2.1 üretimi: ETTN, 16 haneli numara, senaryo seçimi (e-Fatura TEMEL/TİCARİ ya da EARSIV), satır bazında **ÖZELMATRAH 805/808** ve **230 istisna** kodu, saat-dakika | Önceki araştırma E-3; GİB'in 27.07.2026 ve 16.03.2026 paket güncellemeleri ve sicil/faaliyet koduna göre KDV oranı kontrolü (ertelenmişti) izlenmeli |
| E-4 | **Otomatik yönlendirme:** her satışta alıcının vergi numarası ya da TCKN'si sorgulanır; kayıtlıysa e-Fatura, değilse e-Arşiv; nihai tüketicide e-posta varsa elektronik gönderim | Logo'da TCKN sorgusu 3 kontör; sonuç önbelleğe alınmalı |
| E-5 | **Giden kutusu ve yeniden deneme:** "kuyrukta / gönderildi / GİB kabul / hata / ret" durumları, hatanın Türkçe gösterimi. Gönderilemeyen fatura "kesildi" sayılmaz, ekranda açıkça görünür | Kılavuz: GİB'e ulaşmayan e-Arşiv düzenlenmemiş sayılır |
| E-6 | **İptal:** iletimden sonraki 8 gün içinde düğme açık; sonra "iade faturası" akışı. Ticari faturada alıcının ret yanıtının izlenmesi | § 1.4 |
| E-7 | Test ortamında uçtan uca kanıt: e-Arşiv bireye, e-Arşiv kayıtsız şirkete, e-Fatura TEMEL, e-Fatura TİCARİ (ret dahil), iptal; ÖZELMATRAH satırları | Gerçek tarayıcı kuralı ve iş kuralları kataloğu |
| E-8 | Kurulum ekranı: "Entegratör seç → anahtarı yapıştır → bağlantıyı dene" (tek birincil düğme) | Sade ve şık ilke |

**Yapılmayacaklar:** GİB portalını taklit eden bağlantı (gri alan ve SMS onayı) · doğrudan entegrasyon · ByGatech'in entegratör olması.

### 5.4 Açık kalanlar

| Konu | Durum | Kim çözer |
|---|---|---|
| Nilvera ve İzibiz'de yazılım evi / bayi koşulları, toplu (çok işletmeli) anahtar yönetimi | ❌ açık sayfada yok | Kurucu satış görüşmesi (dış bağımlılık) |
| Luca e-Entegratör API'si ve kontör fiyatı | ❌ | TESMER'e yazılı soru |
| Faturalog'un ve müşterinin bugünkü entegratörü | 🟡 | Müşteriye sorulur (14.10 soru listesi madde 3) |
| Bireye kesilen e-Arşiv'in iptal süresi | 🟡 | Mali müşavir / entegratör destek |
| Mali mühür 2026 tutarı | 🟡 | Kamu SM sayfası (tarayıcıda) |
| Doğrudan entegrasyonun sayısal ölçek eşikleri | ❌ | Gerekmiyor (yol seçilmedi) |
| Portal kazımanın hukuki durumu | 🟡 | Gerekirse hukukçu (yol seçilmedi) |

---

## 6. Kaynak listesi (okunanlar)

**Birincil (GİB):**
- 509 Sıra No'lu VUK Genel Tebliği, güncel hâli (2026): https://cdn.gib.gov.tr/api/gibportal-file/file/getFile?objectKey=MEVZUAT_TEBLIGLER%2FUNIVERSAL%2F2026%2FMEVZUAT_TEBLIGLER_2026_VukTeb509_Guncel.pdf
- 589 Sıra No'lu Tebliğ (RG 31.12.2025/33124 5. Mük.): https://www.alomaliye.com/2025/12/31/vergi-usul-kanunu-genel-tebligi-sira-no-589/
- Özel entegratör listesi: https://ebelge.gib.gov.tr/efaturaozelentegratorlerlistesi.html
- e-Belge duyuruları: https://ebelge.gib.gov.tr/duyurular.html
- e-Arşiv Teknik Kılavuzu V.1.19 (14.10.2026): https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/e-Arsiv_Teknik_Kilavuzu_V.1.19.pdf
- e-Arşiv Fatura Portali Özel Entegratör Entegrasyon Kılavuzu (2021): https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/e-Arsiv_Fatura_Portali_Entegrasyon_Kilavuzu.pdf
- e-Arşiv Başvuru Kılavuzu V.1.8 (22.05.2026): https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/e-ArsivBasvuruKilavuzu.V.1.8.pdf
- e-Arşiv başvuru sayfası: https://ebelge.gib.gov.tr/earsivbasvuru.html
- e-Fatura Özel Entegrasyon Kılavuzu v1.14 (29.06.2026): https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/e-Fatura_Uygulamasi_Ozel_Entegrasyon_Kilavuzu_v1.14.pdf
- ÖEBSD Kılavuzu (2019): https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/e-BELGE_OZEL_ENTEGRATORLERI_BILGI_SISTEMLERI_DENETIMI_KILAVUZU.pdf
- e-Fatura İptal/İhtar/İtiraz Kılavuzu V.1.2: https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/e-Fatura_Iptal_Ihtar_Itiraz_Bildirim_Kilavuzu_V_1.2.pdf
- e-Arşiv Uygulamaları İptal/İhtar/İtiraz Kılavuzu V.1.1 (Ocak 2025): https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/e-Arsiv_Uygulamalari_Iptal_Ihtar_Itiraz_Bildirim_Kilavuzu_V.1.1.pdf

**Üretici / ikincil:** developer.nilvera.com · github.com/izibiz/ws-client-dotnet · birfatura.com (API, kontör) · e-Logo fiyat listesi (cdn.logo.com.tr) · docs.sovos.com · github.com/mlevent/fatura · faturalog.com · mm.kamusm.gov.tr · turmobeimza.com.tr · App Store (Luca e-Entegratör) · dunya.com · ideasoft.com.tr · e-invoice.app · parasut.com
