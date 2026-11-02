# Araştırma — Sabit IP istemeyen, kendi kendine başlanabilen banka hesap hareketi API'leri · 27.10.2026

> Önceki belgeler: `ARASTIRMA-BANKA-DOGRUDAN-ENTEGRASYON-2026-10-18.md` (hukuk, başvuru yolu) ve `ARASTIRMA-BANKA-API-TEKNIK-2026-10-19.md` (tel biçimi). Bu belge yalnız **yeni bilgiyi** ve şu soruyu cevaplıyor: hangi banka **sabit IP istemiyor** ve Garanti gibi **yazılım firmasının kendi kendine başlayabileceği** bir yol sunuyor?
> Yöntem: Hesap açılmadı, giriş yapılmadı, form doldurulmadı. Yalnız herkese açık sayfalar okundu. Kuveyt Türk, Albaraka ve Ziraat Katılım portalları JavaScript ile çalıştığı için içerikleri okunamadı. Bunların arka uç uçlarını doğrudan sorgulama denemesi izin katmanında reddedildi ve yapılmadı.
> "DOĞRULANAMADI" = resmi kaynakta bulunamadı. Tahmin yapılmadı.

## 0. En önemli yeni bulgular

1. **Yönetmelik, IP kısıtını neredeyse kuralı hâline getiriyor.** BDDK Bilgi Sistemleri Yönetmeliği m.41'e göre açık bankacılık servislerinde **tek bileşenli** kimlik doğrulamaya (yalnız kullanıcı/şifre) üç şartla izin veriliyor: uçtan uca güvenli iletişim, telafi edici ek kontroller ve **müşterinin bağlantı kaynaklarının ek kısıtlamalara tabi tutulması**. Kullanıcı/şifreli SOAP servislerinin (Ziraat, Vakıf, QNB, Akbank, Kuveyt, Albaraka, İş Posmatik) çoğunda bankanın IP sorması bu yüzden beklenir. Her banka için bu ayrı ayrı **DOĞRULANAMADI**. Kaynak: https://www.alomaliye.com/2020/03/15/bankalarin-bilgi-sistemleri-ve-elektronik-bankacilik (RG 15.03.2020/31069, m.41).
2. **Akbank kendi sayfasında IP'yi "istenildiğinde" diyor.** Kendi ifadesi: "İstenildiğinde erişime açık olarak tanımlı terminal kontrolleri ile güvenli". Paraşüt ise kılavuzunda IP alanına kendi iki IP'sinin yazılmasını istiyor. Bu bir **çelişki**, zorunluluk kesinleşmedi.
3. **İş Bankası API Portal yolu bizim için uygun değil.** Katalog (Nisan 2024) şunları söylüyor:
   - Hesap Hareketleri API'si **"Çok Yakında"** durumunda.
   - Kullanıcı yalnız "Türkiye'de yerleşik ticari tüzel kişiliğe sahip Bankamız müşterileri".
   - API sözleşmesi şart.
   - UAT için **işletme adına OV/EV SSL sertifikası** ve SSL karşılıklı kimlik doğrulama (mTLS) gerekiyor.
   - IP'den söz edilmiyor.

   Yani API'yi yazılım firması değil, **kuyumcunun kendisi** tüketiyor.
4. **Yapı Kredi API portalında "Account Transaction List" var** (`https://api.yapikredi.com.tr/api/currentAccounts/account/v1/`). Ancak kimlik modeli, müşteri onayı, gerçek ortama geçiş ve kimlerin kullanabileceği portal sayfalarında yazmıyor. Bu yüzden Garanti benzeri bir yol **DOĞRULANAMADI**.
5. **Kuveyt Türk (ve aynı altyapıdaki Ziraat Katılım) OAuth + müşteri girişi modeli kullanıyor.** Müşteri müşteri numarası ve internet şube şifresiyle giriyor, SMS şifresi giriyor, sonra dış uygulamanın erişeceği veriyi onaylıyor. IP'den söz edilmiyor. Ancak hesap hareketi API'sinin lisanssız bir yazılım firmasına gerçek ortamda açılıp açılmadığı **DOĞRULANAMADI**.
6. **QNB'de başvuru kuyumcunun kendisinde.** Online Ekstre API başvurusu Tüzel İnternet Bankacılığı'ndan, ıslak imzasız yapılıyor. Kimlik bilgileri kullanıcı/şifre ve WSDL. IP'den söz edilmiyor (QNB bülteni 21.10.2024 ve Paraşüt). Garanti'deki gibi OAuth/consent yok.
7. **Lisanslı aracı (HBH/AIS) yolu sabit IP derdini tümden kaldırıyor.** Bankaya aracı bağlanıyor, ByGatech aracının REST API'sini çağırıyor. Bu yol bireysel hesapları da kapsıyor. Fiyatlar:
   - Kobaküs: Silver ₺3.700/ay (2.000 hareket), Gold ₺7.800/ay (10.000 hareket). API erişimi +₺1.500/ay. KDV hariç, 12 ay taahhüt.
   - Finrota NetEkstre KOBİ: ₺1.900/ay'dan başlıyor. API ise yalnız "Kurumsal" pakette ve fiyatı "sorunuz".

## (a) Özet tablo

Sütun kısaltmaları: **Portal+sandbox** = herkese açık geliştirici portalı, kendi kendine kayıt ve sandbox. **Kimlik** = kimlik modeli. **Bireysel** = bireysel (şahsi) hesapla kullanım.

| Banka | Hesap hareketi API/servisi | Portal+sandbox | Kimlik | Sabit IP | Bireysel | Kaynak |
|---|---|---|---|---|---|---|
| **Garanti BBVA** (referans) | EHÖ Account Transactions | **Var.** Kayıt ücretsiz, sandbox var, gerçek ortama geçiş talep ile | OAuth2 client_credentials + müşterinin internet şubesinde verdiği consentId | Portalda **isteğe bağlı** (kurucunun gözlemi). Ana sayfa IP'den söz etmiyor | DOĞRULANAMADI | [1] [2] |
| **İş Bankası** | (a) API Portal "Hesap Hareketleri": **Çok Yakında**. (b) Şube web servisi (Posmatik) | Portal ve sandbox var, ama gerçek ortam için sözleşme + OV/EV SSL + iş birimi onayı gerekiyor | (a) OAuth + mTLS, token 20 dk. (b) Kullanıcı/şifre | (a) Belirtilmemiş (VPN yok, mTLS var). (b) Belirtilmemiş | (a) **Hayır**, yalnız ticari tüzel kişi. (b) DOĞRULANAMADI | [3] [4] [5] |
| **Yapı Kredi** | Portal: "Account Transaction List". Şube: Online Hesap Özeti web servisi | Portal ve kayıt var. Sandbox "Simulation Data" bölümüyle var gibi görünüyor, kimlik ayrıntısı yazmıyor | Portal DOĞRULANAMADI. Şube: kullanıcı/şifre + firma kodu | DOĞRULANAMADI | DOĞRULANAMADI | [6] [7] [5] |
| **Akbank** | Elektronik/Online Ekstre web servisi (son 3 ay). API portalında hesap API'si yok | Yok | Kullanıcı/şifre + URF | **Çelişkili.** Banka: terminal kontrolü "istenildiğinde". Paraşüt: IP alanı dolduruluyor | DOĞRULANAMADI | [8] [5] |
| **Ziraat** | Web Servis Online Hesap Hareketleri (HEK_NKYWS) | Yok | Kullanıcı/şifre (şube) | Belirtilmemiş | DOĞRULANAMADI | [9] [5] |
| **VakıfBank** | Şube web servisi (SOnlineEkstreServis). API portalında yalnız HGS, Vinov, gümrük ve para transferi görünüyor | Portal var, hesap hareketi API'si görünmüyor | Kullanıcı/şifre + müşteri no | Belirtilmemiş | DOĞRULANAMADI | [10] [5] |
| **Halkbank** | Web Servis Hizmetleri (XML), elektronik ekstre | Yok | DOĞRULANAMADI | DOĞRULANAMADI | DOĞRULANAMADI | [11] |
| **QNB** | Online Ekstre API (SOAP) | "QNB API Developer Portal" bültende geçiyor, adresi DOĞRULANAMADI. Başvuru Tüzel İnternet Bankacılığı'ndan, ıslak imzasız | Kullanıcı/şifre + WSDL | Belirtilmemiş | Bülten "tüzel" diyor | [12] [5] |
| **Enpara** | QNB ile aynı servis ("3. Şablon") | Yok. Başvuru sirketim@enpara.com adresine e-postayla | Kullanıcı/şifre + WSDL | Belirtilmemiş | Enpara Şirketim (şirket) | [5] |
| **DenizBank** | API Portal: GetAccountTransactionList, GetCorporateAccountTransactionList | Var, test ortamı var | AppKey + JWS imza | **ZORUNLU.** Erişim, müşterinin statik IP numaralarına yapılacak yetkilendirmeden sonra açılıyor | DOĞRULANAMADI | [13] [14] |
| **Kuveyt Türk** | API Market: hesap bilgisi ve hareketleri | Portal var. Sandbox ve gerçek ortama geçiş koşulları DOĞRULANAMADI (sayfa JS) | OAuth yetkilendirme kodu: müşteri no + şifre + SMS + veri onayı. Ayrıca şube SOAP (BOA) | Belirtilmemiş | Banka sayfası "bireysel ve tüzel tüm müşteriler" diyor; açık bankacılık sayfası, ücretsiz | [15] [16] [17] |
| **TEB** | Şube web servisi (CepteTEB dahil) | Yok | Kullanıcı/şifre + firma adı + firma anahtarı | Belirtilmemiş | DOĞRULANAMADI | [5] |
| **ING** | Şube web servisi | Yok | Kullanıcı/şifre | Belirtilmemiş | DOĞRULANAMADI | [5] |
| **Albaraka** | Şube web servisi (HesapBilgileriService). "Api Market" portalı var | Portal içeriği okunamadı | Kullanıcı/şifre + müşteri no | Belirtilmemiş | DOĞRULANAMADI | [5] [18] |
| **Türkiye Finans** | DOĞRULANAMADI (yalnız aracı listelerinde geçiyor) | DOĞRULANAMADI | DOĞRULANAMADI | DOĞRULANAMADI | DOĞRULANAMADI | [19] |
| **Vakıf Katılım** | Bakiye İzleme / Hesap Hareketleri Web Servisi | Yok. "Sistemsel tanım" için şubeye gidiliyor. Liste: SAP, Logo, Mikro, Zirve, Paraşüt, Uyumsoft, Bulut Tahsilat; diğerleri talep üzerine | DOĞRULANAMADI | Belirtilmemiş | Sayfa KOBİ/ticari/kurumsal diyor | [20] |
| **Ziraat Katılım** | API Market (Kuveyt ile aynı altyapı) | Portal var, içerik okunamadı | DOĞRULANAMADI | DOĞRULANAMADI | DOĞRULANAMADI | [21] |
| **Şekerbank** | DOĞRULANAMADI (Finrota listesinde var) | — | — | — | — | [19] |
| **Fibabanka** | Şube web servisi | Yok | Kullanıcı/şifre | Belirtilmemiş | DOĞRULANAMADI | [5] |
| **Odeabank** | DOĞRULANAMADI (Finrota listesinde var) | — | — | — | — | [19] |

"Belirtilmemiş" = kaynakta IP'den hiç söz edilmiyor. Bu "IP istenmez" anlamına **gelmez**. Bkz. § 0 madde 1, BDDK m.41.

## (b) Sabit IP istemeyen ve Garanti gibi kendi kendine başlanabilen bankalar (kaynakla doğrulanan)

**Yalnız Garanti BBVA.** Doğrulanan noktalar:
- Portal kaydı ücretsiz.
- Sandbox var.
- Gerçek ortama geçiş talep ile.
- Müşteri onayı internet şubesinden veriliyor.
- IP alanı isteğe bağlı (kurucunun portal gözlemi).

Kaynak: https://developers.garantibbva.com.tr/pages/Home_tr.html

Ölçütlerin bir kısmını karşılayan, ama tamamı doğrulanamayan bankalar:
- **Kuveyt Türk:** Portal + OAuth + müşteri onayı modeli var, IP'den söz edilmiyor. Gerçek ortamda lisanssız yazılım firmasına hesap hareketi açılıp açılmadığı ve sandbox **DOĞRULANAMADI**. Bir sonraki adım: developer.kuveytturk.com.tr'ye kayıt olup katalog ve "go-live" koşullarını okumak. Bu bir hesap açma işi; kurucu onayı gerekir.
- **Yapı Kredi:** Portal kataloğunda Account Transaction List var. Kimlik ve onay modeli **DOĞRULANAMADI**. Bir sonraki adım: portala kayıt (kurucu onayı gerekir).
- **QNB/Enpara:** Başvuru internet şubesinden kuyumcunun kendisinde ve IP'den söz edilmiyor, ama model kullanıcı/şifreli SOAP. Garanti gibi değil.

## (c) Aracı servis seçenekleri

| Seçenek | Ne veriyor | Sabit IP derdi | Bireysel hesap | Ücret | Lisans | Kaynak |
|---|---|---|---|---|---|---|
| **Kobaküs** | Tek REST API ile Türkiye, İngiltere ve AB'de 850+ banka. Ziraat, Halk, Vakıf, İş, Garanti, Akbank, YKB, QNB, Deniz, TEB, ING, Kuveyt, Albaraka, Türkiye Finans, Vakıf Katılım ve Ziraat Katılım listede. Onay müşterinin kendi bankasının ekranında, güçlü kimlik doğrulamayla, süreli ve geri alınabilir | **Kalkar** (bankaya aracı bağlanır) | **Evet** ("bireysel ve ticari hesaplar") | Sandbox ücretsiz. Silver ₺3.700/ay (2.000 hareket), Gold ₺7.800/ay (10.000 hareket), API +₺1.500/ay. KDV hariç, 12 ay taahhüt | Kendini "altyapı sağlayıcı" olarak tanımlıyor; HBH rolünü lisanslı ortak (banka ya da lisanslı kuruluş) üstleniyor. Ortağın adı DOĞRULANAMADI | [22] [23] |
| **Finrota NetEkstre** | 30+ banka (Akbank'tan Ziraat Katılım'a, Şekerbank, Odeabank, Fibabanka dahil). "Bankaların Açık Bankacılık API'leri" ve bankanın güvenli ekranından izin modeli | Kalkar | DOĞRULANAMADI (sayfa KOBİ/kurumsal hedefli) | KOBİ ₺1.900/ay'dan başlıyor. API yalnız "Kurumsal" pakette, fiyat "sorunuz" | TCMB lisansından söz edilmiyor (yalnız ISO27001, PCI DSS) | [19] |
| **Logo eLogo Online Hesap Özeti** | Logo ürünlerinin içinde çalışıyor. Banka formları eLogo sipariş portalından alınıyor | Banka tarafındaki kural aynı kalır | DOĞRULANAMADI | Aylık/yıllık paket, fiyat yayımlanmamış | — | [24] |
| **Paraşüt** | Kendi ürünü için 14 bankaya şube formu + kullanıcı/şifre ile bağlanıyor. Üçüncü yazılımlara API vermiyor | — (Akbank için kendi IP'leri) | — | İş Bankası: hesap başına aylık 25 TL / yıllık 295 TL + BSMV. Vakıf ve YKB "ücret talep edebilir" | — | [5] |
| **TCMB/BKM açık bankacılık (ÖHVPS/GEÇİT)** | Mart 2026'da v2.0.0 ile HBH kapsamı genişledi. 53 katılımcı var | — | — | — | Doğrudan bağlanmak TCMB HBH izni ister | [25] |

Not: Logo ve Paraşüt, ByGatech için aracı değil. Bunlar kendi ürünleri içinde çalışan rakip örnekleri.

## (d) Önerilen sıralama

1. **Garanti BBVA.** Uygulandı. Tek doğrulanmış sabit-IP'siz ve kendi kendine başlanabilen yol.
2. **Lisanslı aracı ile tek entegrasyon (Kobaküs öncelikli, Finrota ikinci).** Sabit IP'yi, bireysel hesap sorununu ve 16+ bankayı tek seferde çözüyor. Önceki belgedeki HBH lisans riskini de lisanslı ortağa devrediyor. Maliyeti ₺3.700 + ₺1.500/ay ve 12 ay taahhüt; kiracılara yansıtma hesabı Mustafa'ya (FinOps). Kurucu kararı gerekir: sözleşme ve ücret dış etkili.
3. **Kuveyt Türk ve Yapı Kredi portallarına kayıt olup koşulları okumak.** Kurucu onayıyla hesap açılmalı. Gerçek ortamda müşteri onaylı hesap hareketi açılıyorsa bunlar 2. ve 3. "Garanti benzeri" banka olur.
4. **QNB/Enpara ve Ziraat.** Kuyumcu internet şubesinden ya da şubeden kendisi başvurur, IP sorulursa aşağıdaki çözüm uygulanır.
5. **Sabit IP isteyenler için tek çözüm: bankaya giden çağrıları sabit IP'li küçük bir bulut çıkışı (vekil sunucu) üzerinden göndermek.** Bankaya giden IP, kurucunun bulunduğu yer değil, sunucunun çıkış adresidir. Bu yapılırsa Akbank, DenizBank ve IP soran diğer SOAP bankaları kurucu nerede olursa olsun çalışır. Bu bir mimari öneridir, kaynaklı bir iddia değildir; maliyeti ve hangi bulut kullanılacağı araştırılmadı.

## Kaynaklar
1. Garanti geliştirici portalı: https://developers.garantibbva.com.tr/pages/Home_tr.html
2. Garanti API Store: https://www.garantibbva.com.tr/en/corporate/digital-banking/garanti-bbva-api-store
3. İş Bankası API ürün kataloğu (Nisan 2024): https://gorsel.isbank.com.tr/Documents/api-urun-katalogu.pdf
4. İş Bankası API Portal: https://www.isbank.com.tr/api-portal
5. Paraşüt banka bağlama kılavuzu: https://www.parasut.com/kullanim-kilavuzu/banka-hesaplarinin-baglanmasi
6. Yapı Kredi API kataloğu: https://apiportal.yapikredi.com.tr/api-catalog
7. Yapı Kredi Account Transaction List: https://apiportal.yapikredi.com.tr/documentation/currentAccounts/accountTransactionList
8. Akbank Elektronik Ekstre: https://www.akbank.com/kurumsal/nakit-yonetimi/e-hizmetler/elektronik-ekstre
9. Ziraat hesap hareketleri entegrasyonu: https://www.ziraatbank.com.tr/tr/kurumsal/nakit-yonetimi/hesap-hareketleri-entegrasyonu
10. VakıfBank API Portal: https://www.vakifbank.com.tr/en/digital-banking/open-banking/vakifbank-api-portal
11. Halkbank raporlama sistemleri: https://www.halkbank.com.tr/tr/ticari-kurumsal/nakit-yonetimi/raporlama-sistemleri
12. QNB basın bülteni 21.10.2024: https://www.qnb.com.tr/medium/document-file-4086.vsf
13. DenizBank kayıt (statik IP): https://apiportal.denizbank.com/kaydol
14. DenizBank hesap API'leri: https://apiportal.denizbank.com/apidetay/Accounts
15. Kuveyt Türk API bankacılığı genel özellikler: https://www.kuveytturk.com.tr/en/digital-banking/other-applications/api-banking/general-features
16. Kuveyt Türk yardımcı güvenlik bilgisi (giriş akışı): https://www.kuveytturk.com.tr/en/digital-banking/other-applications/api-banking/auxiliary-security-information
17. Kuveyt Türk API (2017, OAuth ve hesap hareketleri): https://webrazzi.com/2017/11/27/kuveyt-turk-api/
18. Albaraka Api Market: https://developer.albarakaturk.com.tr/
19. Finrota NetEkstre KOBİ: https://finrota.com/en/products/netekstre-kobi
20. Vakıf Katılım web servisi: https://www.vakifkatilim.com.tr/en/for-me/payments-and-services/services/balance-monitoring-account-activities-web-service
21. Ziraat Katılım Api Market: https://developer.ziraatkatilim.com.tr/
22. Kobaküs hesap hareketleri: https://kobakus.com/en/hesap-hareketleri
23. Kobaküs fiyatlar: https://kobakus.com/fiyatlar
24. Logo Online Hesap Özeti: https://www.logo.com.tr/en/product/online-account-statement
25. TCMB açık bankacılık v2.0.0 duyurusu (17.03.2026): https://www.alomaliye.com/2026/03/17/acik-bankacilik-hizmetlerinin-yeni-ozelliklerine-iliskin-basin-duyurusu/amp/
- BDDK yönetmeliği m.41: https://www.alomaliye.com/2020/03/15/bankalarin-bilgi-sistemleri-ve-elektronik-bankacilik
