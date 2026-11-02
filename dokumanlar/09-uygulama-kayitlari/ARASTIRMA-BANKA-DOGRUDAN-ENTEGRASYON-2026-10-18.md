# Araştırma — Bankalarla doğrudan entegrasyon (lisanssız hesap hareketi çekme) · 18.10.2026

## Özet (5 satır)
1. **Teknik olarak mümkün ve yaygın:** Türk bankalarının neredeyse hepsi kurumsal/ticari müşterisine "online hesap ekstresi / hesap hareketleri web servisi" veriyor. İşletme şubeye form verir (ya da internet şubesinden başvurur), banka **işletmeye** web servis kullanıcı adı/şifre (+ müşteri no / firma kodu / WSDL) verir; Paraşüt, Logo, Mikro bu yolla çalışıyor.
2. **Hukuk:** 6493 m.12(1)(g) "onayla, bir veya daha fazla hesaba ilişkin **konsolide** bilgilerin çevrim içi platformda sunulması" = Hesap Bilgisi Hizmeti (HBH), TCMB izni gerektirir. "Müşterinin kendi kanalı" (banka–işletme sözleşmesi, müşteri adına hareket eden yazılım) BDDK yönetmeliğinde açık bankacılık servisi olarak tanımlı. Ancak ERP üreticisinin bu kanalı kullanmasının HBH sayılmadığını açıkça söyleyen bir TCMB metni bulamadım → **TEYİTSİZ, hukuk görüşü şart.**
3. **Piyasa uygulaması:** Paraşüt 14 banka için resmi kılavuz yayımlıyor (şube formu + bankanın verdiği kullanıcı/şifre). Bankalar Paraşüt'ün IP adreslerini tanımlıyor. Nebim ise aracı (Finrota NetEkstre) üzerinden gidiyor. Lisansız yazılımlar bu modeli yıllardır bankaların bilgisiyle kullanıyor.
4. **Başlangıç bankaları (öneri):** Garanti BBVA (geliştirici portalı + sandbox), İş Bankası (sandbox portalı + şube web servisi), QNB/Enpara (aynı servis, portal + internet şubesinden başvuru), ardından Akbank ve Ziraat (sandbox yok, şube yolu, ama müşteri tabanı büyük).
5. **İşletmeden istenecekler:** bankaya imzalı web servis başvurusu, bankanın verdiği servis kimlik bilgileri, IBAN listesi, ByGatech sunucusunun statik IP'sinin bankaya bildirilmesi, banka ücretinin kabulü. İnternet şubesi parolası **asla** istenmez (ekran kazıma yok).

---

## 1. Bankaların kurumsal hesap hareketleri ürünleri

> "Doğrulandı" = bankanın kendi sayfasında ya da Paraşüt'ün resmi kılavuzunda görüldü. Diğerleri kaynağıyla işaretli.

| Banka | Ürün adı | Başvuru yolu | Kimlik bilgisi türü | Herkese açık belge / sandbox | Ücret |
|---|---|---|---|---|---|
| **Garanti BBVA** | Elektronik Hesap Özeti (EHÖ): Account Information + Account Transactions API'leri. Ayrıca MT940/942 dosyası (FTP/e-posta/SWIFT), LUCA ve Logo/Mikro paketleri | (a) Geliştirici portalı: uygulama aç → client id/secret. İşletme Garanti internet şubesinde "Başvur → Elektronik Hesap Özeti" ile client id'yi ve hesapları girer → consentId oluşur. (b) Paraşüt yolu: şubeye form → bankanın e-postayla verdiği kurum kodu | OAuth (client id + secret) + müşterinin internet şubesinde verdiği consent | **Var.** https://developers.garantibbva.com.tr: Account Transactions API ve mock verili sandbox. Canlıya geçiş için talep gerekir. İletişim: apistore@garantibbva.com.tr | Topluluk paketine göre aylık ~194 TL (**TEYİTSİZ**, tarihi belirsiz) |
| **İş Bankası** | API Portal'da "Hesap Hareketleri" API'si (Hesaplar kategorisi). Ayrıca şube üzerinden muhasebe yazılımı web servisi | (a) API Portal: hesap aç, test et, canlı için başvur. Katalog: yalnız "Türkiye'de yerleşik ticari tüzel kişiliğe sahip" müşteriler; UAT için OV/EV SSL sertifikası. (b) Şube formu → bankanın e-postayla verdiği kullanıcı adı/şifre | (a) OAuth + SSL sertifikası. (b) Kullanıcı/şifre | **Var.** developer.isbank.com.tr → developer.sandbox.isbank.com.tr. Portal 08.2025'te yenilendi (160 + 260 API) | Paraşüt kılavuzu: hesap başına yıllık 295 TL ya da aylık 25 TL + BSMV (doğrulandı, Paraşüt sayfası) |
| **QNB** | Online Ekstre API (21.10.2024'te yenilendi). Tüzel internet bankacılığından başvuru, ıslak imza yok | Tüzel İnternet Bankacılığı → Online Ekstre tanımı (basın bülteni). Paraşüt yolu: şubeye form, "3. Şablon" | Kullanıcı adı + şifre + WSDL adresi (Paraşüt) | QNB API Developer Portal (bültende geçiyor). developer.qnb.com.tr/landing şu an 404 döndü → URL **TEYİTSİZ** | Belirtilmemiş |
| **Enpara (QNB)** | QNB ile aynı servis ("3. Şablon") | Formu Enpara'da kayıtlı e-postadan sirketim@enpara.com adresine gönder | Kullanıcı adı + şifre + web servis adresi | QNB ile aynı | Belirtilmemiş |
| **Akbank** | Elektronik Ekstre (web servis, XML, son 3 ay geriye) + Logo yazılım entegrasyonu | Şubeye form → şube Ürün Referans Kodu (URF) verir → işletme Akbank Direkt'te "Web Servis Şifre İşlemleri → Hesap Ekstresi" kullanıcısı tanımlar, firma kodu = URF, **yazılımın IP adreslerini** ekler | Kullanıcı adı + şifre + IP kısıtı | Hesap API'si yok: apiportal.akbank.com yalnız kur, faiz ve ödeme planı API'leri sunuyor. Sandbox yok | Belirtilmemiş |
| **Yapı Kredi** | Online Hesap Özeti (gün içi, web servis), EHÖ/MT940 (SFTP), LOGO Connect, LUCA, e-posta ekstre | Yetki belgesi şubeden alınır, talep şubeye verilir | Kullanıcı adı + şifre + firma kodu (Paraşüt) | apiportal.yapikredi.com.tr var (OAuth). Hesap hareketi API'si olup olmadığı **TEYİTSİZ** | Şube Hizmet Komisyonları Tablosu. Paraşüt: "ücret talep edebilir" |
| **Ziraat Bankası** | "Web Servis Online Hesap Hareketleri Entegrasyonu" + MT940/942 (e-posta/FTP/SFTP/SWIFT) | Yalnız şube | Kullanıcı adı + şifre + IBAN (Paraşüt). Topluluk paketi: SOAP "HEK_NKYWS", kullanıcı/şifre + VKN/TCKN (**resmi değil**) | Herkese açık belge/sandbox bulunamadı | Belirtilmemiş |
| **VakıfBank** | Hesap hareketi web servisi (şube). API portalı (HGS, gümrük, para transferi) | Şubeye form → müşteri no + kullanıcı adı + şifre | Kullanıcı/şifre. Portal: OAuth authorization code | apiportal.vakifbank.com.tr var. Hesap hareketi API'si bulunamadı. Destek: apiportaldestek@vakifbank.com.tr | Paraşüt: "ücret talep edebilir" |
| **Halkbank** | Elektronik Ekstre (dosya), E-mail Ekstre, Web Servis Hizmetleri (XML mesajlaşma) | Sayfada başvuru adımı yok → şube (**TEYİTSİZ**) | Belirtilmemiş | Yok | "Ürün ve Hizmet Ücretleri" tablosu |
| **DenizBank** | API portalı: hesap hareketi görüntüleme, EFT/havale | Portalda kayıt + doküman + **statik IP** yetkilendirmesi | Bankanın verdiği kullanıcı kodu + şifre | apiportal.denizbank.com (test ortamı var). Paraşüt'te DenizBank "geçici kullanım dışı" | Belirtilmemiş |
| **Kuveyt Türk** | API Market (hesap bilgisi + hesap hareketleri, müşteri onaylı. 2017 kaynağı) | developer.kuveytturk.com.tr'de kayıt | apiKey/OAuth (topluluk listesi) | Portal var. Sayfa JS gerektirdiği için içerik okunamadı → kurumsal hareket API'si **TEYİTSİZ** | Belirtilmemiş |
| **Albaraka Türk** | MT940 + Logo, Mikro, Paraşüt, Uyumsoft, Bizim Hesap, Luca entegrasyonları. "Api Market" portalı | Şubeye form → e-postayla müşteri no + kullanıcı adı + şifre | Kullanıcı/şifre | developer.albarakaturk.com.tr (içerik okunamadı) | Belirtilmemiş |

Ortak desen (Paraşüt kılavuzunda 14 bankada aynı): **şubeye başvuru formu → banka, işletmeye özel web servis kullanıcısı verir → yazılıma girilir.** Bu kimlik bilgisi internet şubesi parolası değildir. Yalnız ekstre okuma yetkisi olan ayrı bir servis kullanıcısıdır.

## 2. Lisans gerekir mi? (6493 / TCMB / BDDK)

**Kanun metni (doğrulandı, mevzuat.gov.tr 6493 güncel metin):**
- m.12(1)(g): "Ödeme hizmeti kullanıcısının onayının alınması koşuluyla, ödeme hizmeti kullanıcısının ödeme hizmeti sağlayıcıları nezdinde bulunan bir veya daha fazla ödeme hesabına ilişkin konsolide edilmiş bilgilerin çevrim içi platformlarda sunulması hizmeti" = **Hesap Bilgisi Hizmeti (HBH)**, bir ödeme hizmeti.
- m.13(2): "Banka ve ödeme hizmeti sağlayıcısı dışındaki kişiler ödeme hizmeti sunamazlar." m.14: ödeme kuruluşu TCMB izniyle faaliyet gösterir.
- m.12(2)(ğ) istisnası: teknik hizmet sağlayanların "verinin işlenmesi, saklanması, güvenliğinin sağlanması…" hizmetleri, fonun sahibi olmadıkları sürece ödeme hizmeti sayılmaz. Bu istisnanın metni "ödeme hizmetlerinde provizyon işlemlerinin yapılmasını destekleyen" hizmetlerle sınırlı. ERP'ye uygulanıp uygulanmadığı **TEYİTSİZ**.
- Ocak 2025 itibarıyla m.12(1)(f)(g) kapsamında açık bankacılık izni olan 17 kuruluş var (Forbes Türkiye).

**BDDK "Bankaların Bilgi Sistemleri ve Elektronik Bankacılık Hizmetleri Hakkında Yönetmelik" (RG 15.03.2020/31069):**
- m.3(1)(a) açık bankacılık servisleri: "Müşterilerin ya da **müşteriler adına hareket eden tarafların** API, web servis, dosya transfer protokolü gibi yöntemlerle bankanın sunduğu finansal servislere uzaktan erişerek…" işlem yaptığı elektronik dağıtım kanalı.
- m.41: açık bankacılık servislerinde tek bileşenli kimlik doğrulamaya, telafi edici kontroller ve **bağlantı kaynağı kısıtı** (IP kısıtı) şartıyla izin verilir. Bankaların kullanıcı/şifre + IP kısıtı modeli buna uyuyor.
- m.10: müşteri sırrı, müşterinin açık talebi olmadan üçüncü kişiyle paylaşılmaz. Şube başvuru formu bu "açık talep" işlevini görüyor (yorum, **TEYİTSİZ**).

**İki model:**

| | Müşterinin kendi kanalı (Paraşüt/Logo modeli) | ÖHVPS / HBH (açık bankacılık lisansı) |
|---|---|---|
| Sözleşme | Banka ↔ işletme (şube formu) | Banka ↔ lisanslı HBH sağlayıcı (BKM GEÇİT standardı) |
| Kimlik | Bankanın işletmeye verdiği servis kullanıcısı | Müşterinin bankada SCA ile verdiği rıza + mTLS/OAuth |
| Kim bağlanır | İşletmenin yetkilendirdiği yazılım (IP kısıtlı) | Lisanslı kuruluş |
| Lisans | Yazılım firması için lisans istendiğine dair kaynak **bulunamadı** | TCMB izni zorunlu |

**Sonuç (dürüst sınır):** Paraşüt (lisanssız SaaS) 14 bankayla bu modeli bankaların resmi süreciyle uyguluyor. Akbank, Paraşüt'ün IP'lerini kendi formunda tanımlatıyor. Bu, bankaların modeli kabul ettiğini gösterir. Ancak "ERP üreticisinin işletme adına banka web servisini çağırıp birden çok bankanın hareketini tek ekranda göstermesi HBH değildir" diyen bir **TCMB görüşü ya da rehber cümlesi bulamadım.** m.12(1)(g)'deki "bir veya daha fazla hesap… konsolide… çevrim içi platform" ifadesi literal okunursa çok bankalı bir SaaS ekranına yakın düşüyor. → **TEYİTSİZ: avukat/TCMB görüşü alınmadan "lisans gerekmez" denmemeli.** Güvenli tasarım: hareketi muhasebe/cari eşleştirme için işlemek (ERP işlevi), "tüm bankalarınız tek ekranda" diye pazarlamamak (yorum, **TEYİTSİZ**).

## 3. Yerli ERP'ler nasıl yapıyor?

- **Paraşüt:** Doğrudan banka web servisi. Şube formu + bankanın verdiği kullanıcı/şifre (14 banka kılavuzu). Birden çok banka için ek paket ücreti. https://www.parasut.com/kullanim-kilavuzu/banka-hesaplarinin-baglanmasi
- **Logo:** Bankalarla doğrudan paketler (Garanti: Tiger2/GO; Akbank: Logo yazılım entegrasyonu; Yapı Kredi: LOGO Connect) + **eLogo Online Hesap Özeti** uygulaması (GO 3, Tiger 3, Netsis) + MT940 içe aktarma.
- **Mikro:** Bankaların paket entegrasyonları (Garanti: Jump/Fly; Albaraka; Vakıf Katılım listesinde Mikro).
- **Nebim V3:** Aracı üzerinden. **Finrota NetEkstre** (Türk Elektronik Para grubu, "açık bankacılık ürünü", 29 banka, 22 ERP) ve CloudCollection. Finrota'nın kendisinin HBH lisanslı olup olmadığı bulunamadı (**TEYİTSİZ**).
- **Kobaküs** gibi aracılar ise lisanslı kurum üzerinden AIS (rıza + SCA, FAPI/mTLS) kullanıyor ve banka parolası görmüyor.

## 4. Önerilen yol

**İlk dalga (belgesi/sandbox'ı açık):**
1. **Garanti BBVA:** portal + sandbox + OAuth + EHÖ consent. En temiz, belgeli API.
2. **İş Bankası:** sandbox portalı. Tüzel kişi değilse şube formu + kullanıcı/şifre yolu.
3. **QNB + Enpara:** tek servis, iki banka. Başvuru internet şubesinden, WSDL tabanlı.

**İkinci dalga (sandbox yok, şube yolu, yaygın):**
4. **Akbank:** web servis kullanıcısını işletme kendisi tanımlar, IP kısıtlı.
5. **Ziraat:** şube. Belge ancak başvurudan sonra gelir.

Not: Sandbox'sız bankalarda test için gerçek bir pilot işletmenin servis kullanıcısı gerekir.

**İşletmeden istenecekler:**
- Bankaya imzalı "hesap hareketleri web servisi" başvurusu (bankanın formu; Garanti'de EHÖ consent, QNB'de internet şubesi).
- Bankanın verdiği servis kimlik bilgileri (kullanıcı adı/şifre, müşteri no/firma kodu/URF, WSDL ya da client id + consent).
- Bağlanacak hesapların IBAN ve döviz listesi.
- ByGatech sunucusunun **sabit çıkış IP'sinin** banka formuna yazılması (Akbank, DenizBank zorunlu).
- Banka ücretinin kabulü (ör. İş: hesap başına 25 TL/ay + BSMV).
- **İstenmeyecek:** internet şubesi giriş parolası, SMS/OTP. Ekran kazıma yok.

**Açık kalanlar (kurucuya ve hukuka):**
- (a) HBH kapsamı için avukat/TCMB yazılı görüşü.
- (b) Pilot işletmenin bankası ve şirket türü (tüzel/şahıs). İş Bankası API yolu yalnız tüzel kişiye açık.
- (c) Sabit IP altyapısı.

## Kaynaklar
- 6493 sayılı Kanun güncel metin: https://www.mevzuat.gov.tr/mevzuatmetin/1.5.6493.pdf
- BDDK Bilgi Sistemleri ve E-Bankacılık Yönetmeliği (RG 31069) metni: https://www.alomaliye.com/2020/03/15/bankalarin-bilgi-sistemleri-ve-elektronik-bankacilik
- TCMB iş modeli rehberi değerlendirmesi: https://www.erdem-erdem.av.tr/bilgi-bankasi/odemeler-alaninda-sunulan-is-modellerinin-odeme-hizmeti-turleri-ile-iliskilendirilmesine-iliskin-tcmb-tarafindan-yayinlanan-rehber
- Açık bankacılık izinli 17 kuruluş: https://www.forbes.com.tr/fintek/tcmb-den-hepsipay-ve-param-a-acik-bankacilik-izni
- Paraşüt banka bağlama kılavuzu: https://www.parasut.com/kullanim-kilavuzu/banka-hesaplarinin-baglanmasi
- Garanti entegrasyon çözümleri: https://www.garantibbva.com.tr/isim-icin/diger-urunler/entegrasyon-cozumlerimiz · portal: https://developers.garantibbva.com.tr · topluluk paketi (EHÖ consent, ücret): https://packagist.org/packages/hasokeyk/garanti-bbva-php
- İş Bankası API portal: https://www.isbank.com.tr/api-portal · https://www.isbank.com.tr/bankamizi-taniyin/is-bankasi-api-portal-yenilendi · katalog: https://gorsel.isbank.com.tr/Documents/api-urun-katalogu.pdf
- QNB basın bülteni 21.10.2024: https://www.qnb.com.tr/medium/document-file-4086.vsf
- Akbank Elektronik Ekstre: https://www.akbank.com/kurumsal/nakit-yonetimi/e-hizmetler/elektronik-ekstre · API portal: https://apiportal.akbank.com
- Yapı Kredi e-çözümler: https://www.yapikredi.com.tr/kobi/nakit-yonetimi-ve-dis-ticaretin-finansmani/e-cozumler-ve-operasyonel-hizmetler
- Ziraat hesap hareketleri entegrasyonu: https://www.ziraatbank.com.tr/tr/kurumsal/nakit-yonetimi/hesap-hareketleri-entegrasyonu · topluluk SOAP paketi: https://packagist.org/packages/ravan/ziraat-bank
- VakıfBank API portal: https://www.vakifbank.com.tr/en/digital-banking/open-banking/vakifbank-api-portal
- Halkbank raporlama sistemleri: https://www.halkbank.com.tr/tr/ticari-kurumsal/nakit-yonetimi/raporlama-sistemleri
- DenizBank API portal: https://apiportal.denizbank.com/kaydol
- Kuveyt Türk API Market: https://developer.kuveytturk.com.tr · https://webrazzi.com/2017/11/27/kuveyt-turk-api/
- Albaraka e-hizmetler: https://www.albaraka.com.tr/en/sme/cash-management/e-services-and-accounting-integrations
- Vakıf Katılım web servisi: https://www.vakifkatilim.com.tr/en/other/help-center/for-me-detail/balance-monitoring-account-activities-web-service
- Banka API portalları listesi: https://github.com/3rt4nm4n/turkish-apis
- Logo eLogo Online Hesap Özeti: https://docs.logo.com.tr/download/attachments/83493963/EH%C3%96%20App%20Tan%C4%B1t%C4%B1m%20Bro%C5%9F%C3%BCr%C3%BC.pdf?api=v2
- Nebim–Finrota NetEkstre: https://www.nebim.com.tr/en/nebim-v3-finrota-netekstre-integration · Finrota: https://finrota.com/en/blog/netekstre-nedir · https://finrota.com/en/about-us
- Kobaküs (lisanslı AIS aracısı): https://kobakus.com/en/hesap-hareketleri
