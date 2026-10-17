# Faturalog ve muadilleri — banka/fatura/cari otomasyonu ve kuyumcu programları araştırması (17.10.2026)

> **Kurucunun isteği (17.10):** Mihenk hem Faturalog'u hem "girdi-çıktı, veresiye" programlarını aratmayacak; ekranlar sade, eksik yok; otomatikleşebilen her şey otomatik (borçtan düşme, müşteri kaydı, IBAN'dan para gelince), karar müşterinin olduğu yerde sorulacak. Tüm muadiller istendi; "Faturalog en doğrusu ama".
>
> **Kapsam:** Yalnız araştırma. Kod değişmedi, mimari önerilmedi (bir sonraki adım). Proje kodu taranmadı; bağlam yalnız şu belgelerden alındı: `musteri-gorusmesi-2026-10-16.md` (EK-2 F1–F12, EK-3), `PAKET-PLANI-MUSTERI-2026-10-16.md` (M1–M11), `sadelestirme/PUSULA-REFERANS-2026-09-23.md`, `mevzuat-gereksinimleri.md`.
>
> **Yöntem:** Beş paralel web araştırması (Faturalog derin · muadiller · kuyumcu programları · mevzuat birincil kaynak · arayüz/otomasyon kalıpları) + sentez. **Hiçbir yerde hesap açılmadı, form doldurulmadı, demo istenmedi.** Tüm erişimler **17.10.2026**.
>
> **İşaretler:** **K** = birincil metin/sayfa okundu · **O** = ikincil kaynak ya da arama özeti · **D** = doğrulanamadı. **[Ü]** = üretici beyanı · **[B]** = bağımsız kaynak. Köşeli parantezli kodlar (ör. [F3]) § 9 Kaynakça'ya bağlanır.
>
> **Güvenlik notu:** İki sayfada (faturalog.com ana sayfası ve alomaliye.com'daki bir yazı) metnin sonunda okuyucu yapay zekâya yönelik gömülü not bulundu. Talimat olarak uygulanmadı; içerik yalnız veri olarak okundu.

---

## 1. Yönetici özeti — en önemli 10 bulgu

1. **Faturalog küçük bir arayüz şirketi; kendisi ne GİB entegratörü ne de TCMB lisanslı açık bankacılık kuruluşu.** Unvan FATURALOG BİLİŞİM HİZMETLERİ A.Ş. (Erdemli/Mersin adresli). LinkedIn'e göre 2019 kuruluşlu, 2–10 çalışanlı [F4][F22]. e-Fatura'yı **EDM Bilişim** altyapısıyla kesiyor; EDM GİB listesinde var, Faturalog yok [F4][F3][F18][B]. 28 bankayı "entegre" sayıyor [F1][Ü]. Ancak TCMB'nin ödeme kuruluşu ve elektronik para kuruluşu listelerinde yok [F19][F20][B]. Banka bağlantı yöntemi hiçbir sayfada yazmıyor (**D**).
2. **Faturalog'un çekirdek akışını uçtan uca beyan eden muadiller kuyumcuya yönelik ürünler.** Akış şu: banka hareketi → cari otomatik → kurala göre otomatik fatura. Bu akışı **Milyem PLUS**, **SarrafPro**, **Terminal Gold**, **KuyumHub**, **Dezy** ve **Hasfiyat** (blog beyanı) sunduğunu söylüyor [P5][P17][P4][P6][M6][P2][Ü]. Genel ön muhasebe ürünleri (Paraşüt, KolayBi', BizimHesap, Logo İşbaşı) bankadan **yeni fatura kesmiyor**; yalnız hareketi cariyle eşleştirip **açık faturayı kapatıyor** [M1a][M1b][M4][Ü]. Mihenk'in gerçek rakip alanı burası: kuyumcu yazılımı ile Faturalog işlevinin birleşimi. Bu alan dolmaya başladı.
3. **Faturalog'un sayfaları "otomatik mi, onaylı mı" sorusunda kendi içinde çelişiyor.** Otomatik fatura sayfası akışı onaysız anlatıyor [F10]. Toplu fatura sayfasında faturalar kesilmeden önce onaya düşüyor [F13]. Kural tanım ekranı hiç gösterilmiyor (**D**). Müşterinin gerçekte ne kullandığı ancak kendi ekranından öğrenilebilir.
4. **MASAK kimlik tespiti eşiği 7 Ekim 2026'da 185.000 TL'den 370.000 TL'ye çıktı.** Elektronik transfer eşiği de 15.000 TL'den 30.000 TL'ye çıktı. Kaynak: RG 07.10.2026/33393, CB Kararı 11845 [L13][L14][K]. Projedeki `mevzuat-gereksinimleri.md` M-01 hâlâ **185.000 TL** yazıyor ve "2026'da değişiklik bulunamadı" diyor. Bu satırın güncelliği kaybolmuş. Rakiplerde de karışıklık var: KuyumSmart 370.000, KuyumHub 185.000 TL yazıyor [P3][P6][Ü].
5. **Para havalesinde gönderenin TCKN/VKN'si mevzuatça garanti değil.** MASAK Yönetmeliği md.24'e göre 30.000 TL ve üstü transferde gönderenin adı ve hesap numarası zorunlu. Bunlara ek olarak adres, doğum bilgisi, müşteri no, TCKN, pasaport no ya da VKN'den **en az biri** gerekiyor. Altında yalnız ad ve hesap/referans numarası zorunlu [L14][K]. Banka şemalarındaki alanlar doğrulanamadı (**D**). Sonuç: otomatik cari eşleştirmenin birincil anahtarı pratikte **IBAN + ad**. TCKN yalnız gelirse kullanılabilir. Paraşüt de bu yüzden "IBAN'ı cariye bir kez öğret" kalıbını kullanıyor [M1c][K].
6. **Hiçbir rakip "bu para eski borç mu, yeni alışveriş mi, avans mı?" diye açıkça sormuyor (araştırmada bulunamadı).** Yerleşik varsayılan iki türlü. QuickBooks parayı en eski açık faturaya uyguluyor; kendi kılavuzu bunun her işletmeye uymadığını kabul ediyor [U8][K]. Xero ve Odoo ise parayı "bağlanmamış alacak" olarak bekletip sonra bağlatıyor [U9][U10][O]. QuickBooks topluluğunda "birden çok adayda sessizce en eskiyi seçiyor" şikâyeti var [U12][B]. Mihenk'te gram cinsinden veresiye de var; bu soruyu doğru sormak bir **fark** fırsatı. Bu bir çıkarımdır, doğrulanmış bulgu değildir.
7. **Kuyumcuda "havaleden otomatik fatura" yapısal olarak riskli.** KDVK 23/e özel matrahı için satılan ürünün has gramı gerekir. Has bedel, Borsa İstanbul son işgünü külçe kapanışı × ayar ile hesaplanır [L7][L8][K]. Banka hareketi ürünü taşımaz. Ayrıca her para girişi satış değildir: veresiye tahsilatı, avans, emanet, iade, kişisel transfer olabilir. Zaten faturalanmış bir satışın tahsilatını yeniden faturalamak **çift fatura** üretir. Bu yüzden "otomatik fatura" yalnız iki durumda güvenli: (a) hareket mevcut bir satış ya da siparişle eşleştiğinde, (b) kuyumcunun açıkça tanımladığı bir kural varsa (ör. "bu IBAN = toptan alıcı, her havale X ürünü satışı").
8. **2026'da e-Arşiv ve fatura haddi değişti.** 573 s. VUK GT ile e-Arşiv'e dahil olmayanlar 1.1.2026'dan itibaren faturalarını **tutara bakılmaksızın** GİB portalından e-Arşiv olarak düzenliyor. 589 s. tebliğ basit usul ve işletme esası için bunu 1.1.2027'ye öteledi [L3][L4][K]. Fatura düzenleme haddi 2026'da **12.000 TL** (588 s. VUK GT) [L5][K]. Kuyumcuya "3 kat" hükmü (514 s. VUK GT md.4) metin olarak duruyor; kaldırıldığına dair kayıt bulunamadı. 2026 için 12.000 × 3 = 36.000 TL bizim hesabımız; hiçbir kaynak bu rakamı yazmıyor (**K** metin / **D** yürürlük teyidi) [L6]. Kuyumcuya özel e-Fatura eşiği yok; genel 3 milyon TL brüt satış eşiği geçerli [L2][K].
9. **Yazılımın müşteri adına banka verisi çekmesinin hukuki zemini belirsiz.** Lisanslı yol (HBH) TCMB'nin ödeme kuruluşu ve elektronik para kuruluşu listelerinde 7 + 19 kuruluşta var (Logo Ödeme dahil). Bu sayı otomatik metin çıkarımına dayanıyor; liste gözle teyit edilmeli [M18][M19][O]. TCMB Yönetmeliği md.61/2'ye göre HBH sağlayıcısı müşterinin kimlik doğrulama bilgisini isteyemez [L17][K]. "Müşterinin kurumsal web servis kullanıcısıyla çekme" yolunun lisans gerektirmediğini söyleyen bir kaynak bulunamadı (**D**). 6493 md.12/2-ğ teknik hizmet istisnası olası bir dayanak. md.28'e göre izinsiz faaliyetin cezası 1–3 yıl hapis [L16][K]. **Avukat görüşü şart.**
10. **Arayüzde en iyi kalıp üç kademeli güven + "neden" etiketi + geri alma.** Yüksek güvende otomatik ya da "hazır" toplu onay, orta güvende öneri, düşük güvende öneri yok, kullanıcı arar (QuickBooks rozetleri, Xero JAX). Otomatik işlenen her kayıt neden işlendiğini söylüyor ("Kural", "Hafıza", "Eşleşme"). Büyük tutarlar her zaman insana düşüyor [U1][U2][U4][K]. Google PAIR ve Microsoft HAX rehberleri otomasyonun kullanıcının yönlendirmesiyle adım adım artırılmasını öneriyor [U14][U15][K]. NN/g onay penceresi yerine geri almayı öneriyor [U16][K]. Kuyumcu tarafında Pusula'nın "ödeme bacağı fiş satırıdır, FARK 0,00" kalıbı ve OkCredit'in iki büyük düğmesi ("Veresiye ver / Ödeme al") en sade örnekler [PUSULA-REF][U20][O].

---

## 2. Ürün envanteri

Fiyatlar üreticinin sitesinden (KDV durumu belirtildiği gibi); "?" = sitede yok ya da doğrulanamadı.

### 2.1 Banka → cari → fatura otomasyonu (Faturalog ve muadilleri)

| # | Ürün (firma) | Kategori | Hedef | Fiyat (başlangıç) | Kaynak |
|---|---|---|---|---|---|
| 1 | **Faturalog** (Faturalog Bilişim Hizmetleri A.Ş.) | e-Fatura arayüzü + banka→fatura otomasyonu | KOBİ, e-ticaret; 11 kuyumcu logosu | 100 kontör 250 ₺ … 1.000 kontör 1.500 ₺ | [F1][F3] |
| 2 | **Paraşüt** | Bulut ön muhasebe | KOBİ, serbest çalışan | e-Fatura + Ön Muhasebe aylık 940 ₺ + KDV; kontör 50 adet 210 ₺ | [M1d] |
| 3 | **BizimHesap** (Infotera) | Bulut ön muhasebe | KOBİ, perakende, saha | Temel aylık 870 ₺ + KDV | [M2a][M2b] |
| 4 | **KolayBi'** (ENKO) | Bulut ön muhasebe | KOBİ, esnaf | Ofis aylık 900 ₺ (KDV dahil); KolayBi Banka yıllık 2.400 ₺ | [M3a][M3b] |
| 5 | **Logo İşbaşı** | Bulut ön muhasebe | KOBİ | Ön Muhasebe aylık 463 ₺ + KDV | [M4] |
| 6 | **Milyem PLUS** (Milyem Plus Yazılım Tek. A.Ş.) | Kuyumcu finans otomasyonu | Kuyumcu | ? | [P5] |
| 7 | **SarrafPro** | Kuyumcu "resmileştirme" + banka eşleştirme | Kuyumcu, sarraf | Teklif usulü | [P17] |
| 8 | **Dezy** (Dezy Yazılım Ltd.) | Kuyumcu POS/ERP + e-ticaret | Kuyumcu | İlk 3 ay ücretsiz; TL tutarı ? | [M6] |
| 9 | **Faturam Gold** (App Store, bireysel geliştirici) | Kuyumcu e-Fatura uygulaması | Kuyumcu | Uygulama içi kontör | [M7] |
| 10 | **Aposkal** | Ön muhasebe + mali müşavir | KOBİ, esnaf | Esnaf aylık 180 ₺ + KDV | [M8] |
| 11 | **Netadam Net-X** | Ön muhasebe/ERP | KOBİ | ? | [M9] |
| 12 | **Opak E-Ekstre** | ERP modülü | KOBİ | ? | [M10] |
| 13 | **Hesapmix** (XBEX) | Bulut ön muhasebe | KOBİ | ? | [M11] |
| 14 | **Fiscus AI** | Ön muhasebe (yapay zekâ) | KOBİ | ? (14 gün deneme) | [M12] |
| 15 | **Bilsoft** | Ön muhasebe | KOBİ | ? | [M13] |
| 16 | **Faturaport** | e-Fatura + ön muhasebe (banka entegrasyonu yok) | Esnaf | yıllık 1.000 ₺ | [M14] |
| 17 | **Cariofis** | e-Fatura (banka entegrasyonu yok) | Esnaf | 500 kontör 1.200 ₺ | [M15] |
| 18 | **Mikro** (Run/Jump/Fly) + e-Mutabakat | Masaüstü ERP | KOBİ–orta | ? | [M16a][M16b] |
| 19 | **Logo ERP/Netsis** + Logo Ödeme (HBH lisanslı) | ERP | Orta–büyük | ? | [M17][M18] |
| 20 | **Luca** (TÜRMOB) | Mali müşavir muhasebesi; banka bazı bankalardan gün sonu aktarım | SMMM | ? | [M20] |
| 21 | **Mysoft e-Aktarım** | SMMM banka aktarımı | Mali müşavir | SMMM'ye ücretsiz | [M21] |
| 22 | **Rahat Aktarım** | SMMM ekstre çözümleme + VKN'den otomatik cari | Mali müşavir | 1 ekstre satırı = 1 kontör | [M22] |
| 23 | **Finrota NetEkstre** (Eçözüm, 2001) | Nakit yönetimi / ekstre otomasyonu | Orta–büyük | ? | [M23a][M23b] |
| 24 | **Bulut Tahsilat** | Nakit yönetimi, bayi tahsilatı | Orta–büyük, bayi ağı | 4 paket, TL ? | [M24] |
| 25 | **Vomsis** ("lisanslama sürecinde" beyanı) | Hesap hareketi, POS rapor | Orta | ? | [M25] |
| 26 | **Nebim V3 + BulutTahsilat** | Perakende ERP | Zincir perakende (kuyumculuk sektör listesinde yok) | ? | [M26][P-NEB] |

Kaynak bulunamayanlar: Paym.es, Colendi, Muhasebe.app, Smartbooks, Defterbeyan, Hesapix (alan adı çözülmedi), "Luca Koobi". Entegratörler (Nilvera, İzibiz, QNB eSolutions, Turkcell e-Şirket, Türk Telekom, Kolaysoft, Digital Planet, EDM, Sovos) için "banka hareketinden fatura" özelliğine dair kaynak bulunamadı (**D**). QNB eSolutions'ın Ödüyo'yu satın aldığı ve faturaya ödeme bağlantısı eklediği bilgisi ikincil kaynaktan geliyor (**O**). Papel, TCMB listesinde **faaliyet izni iptal edilenler** arasında [M19][B].

### 2.2 Kuyumcu programları

| # | Ürün (firma) | Platform | Hedef | Fiyat | Kaynak |
|---|---|---|---|---|---|
| 1 | **Pusula Perakende/Toptan/Üretim, PARS** · **PusulaX** (Pusula + Kivi) | Masaüstü + mobil | Perakende, toptan, atölye | ? | [P1a][P1b][P1c][PUSULA-REF] |
| 2 | **Hasfiyat** (+ altinapi.hasfiyat.com) | Web, Windows, iOS, Android | Perakende; fiyat panosu | Bronz 1.250 / Silver 3.250 / Gold 5.250 ₺ aylık (KDV dahil) | [P2a][P2b][P2c] |
| 3 | **KuyumSmart** | Tarayıcı (PWA) | Perakende | Aylık 100 USD + KDV; yıllık 1.100 USD | [P3a][P3b][P3c] |
| 4 | **Terminal Gold / KuvvemERP** (Kuvve Teknoloji) | Bulut + iOS/Android | Perakende, zincir, toptan | Lisans 4.000 / 6.000 / 9.000 USD; "Terminal Fatura" yıllık 900 USD | [P4a][P4b][P4c] |
| 5 | **Milyem PLUS** | Web | Perakende | ? | [P5] |
| 6 | **KuyumHub** (Fluxverys, Bilkent TEKMER) | iOS, Android, web, masaüstü + mali müşavir paneli | Perakende, zincir | Şube başına yıllık 50.000 / 100.000 ₺ | [P6a][P6b] |
| 7 | **Ariyen** (Codeyen) | Masaüstü + mobil + pano + web + e-ticaret | Perakende | Paket var, rakam görünmüyor | [P7a][P7b] |
| 8 | **CetaSoft Kuyum** (İzmir) | Windows | Perakende, toptan | ? | [P8a][P8b] |
| 9 | **M&L Yazılım** (1993) | Masaüstü + iPad | İmalat, toptan, perakende | ? | [P9] |
| 10 | **Kuyum Plus** (Konasoft) | Bulut + iOS/Android | Perakende, toptan | ? | [P10] |
| 11 | **Kuyumcum** (Bambu Teknoloji) | iPad/iPhone/Android | Perakende, toptan | ? | [P11] |
| 12 | **KuyumERP** (SkySoft) | Bulut | Atölye, üretici, toptan | ? | [P12] |
| 13 | **Gramino** (bireysel geliştirici) | Web | Tek şube, veresiye/emanet ağırlıklı | ? | [P13] |
| 14 | **Bakiyem** (Antalya) | Mobil öncelikli | Perakende, atölye | Aylık 4,99–64,99 USD | [P14] |
| 15 | **ZarifSoft** (Ankara) | Bulut | Perakende | ? | [P15] |
| 16 | **E-Yaz Milyem** (Eskişehir) | Masaüstü | Perakende | ? | [P16] |
| 17 | **SarrafPro** | Web + mobil | Kuyumcu, sarraf | Teklif | [P17] |
| 18 | **Meadify** (Mead Teknoloji) | Bulut | Mağaza + e-ticaret | ? | [P18] |
| 19 | **Dezy** | Bulut + mobil | Kuyumcu e-ticaret/POS | 3 ay ücretsiz | [M6][P19] |
| 20 | **Platin360** | e-ticaret modülü | Kuyumcu e-ticaret | ? | [P20] |
| 21 | **Dijimo** (Konya) | Bulut | Perakende, toptan | ? | [P21] |
| 22 | **Kuyumsoft "Kuyumex"** | Masaüstü + iOS | Altın/döviz | ? | [P22] |
| 23 | **KuyumPOS** (yalnız arama özeti, **O**) | ? | Perakende | Ücretsiz paket var | [P23] |
| 24 | **Kuyumcu Programı** (Esmeva) | Android/iOS | Perakende | ? | [P24] |
| 25 | **MacGold / MskGold** (MacroSoft; eski) | Windows | Perakende | ? | [P25] |
| 26 | **Diamags** (SCI Bilişim) | iOS | ? | ? | [P26] |

Bulunamayanlar ya da kuyumcu ürünü olmadığı anlaşılanlar:
- Bulunamadı: "Kuyumcu Takip" (alan adı çözülmüyor), Vega Kuyumcu, Altın Kasa, KuyumNet, Goldtakip, AltınPOS.
- Yalnız başka sitelerde adı geçenler: IOSSGOLD, Dijigold, ONEPiR.
- Kuyumcu ürünü değil: Goldsoft (doküman yazılımı). Akınsoft Wolvox'ta kuyumcu modülü bulunamadı. Nebim'in sektör listesinde kuyumculuk yok.
- Logo/Netsis'te "Has Altın Uygulaması Desteği" doküman sayfasının adı görüldü ama sayfa açılmadı (**D**).

---

## 3. Özellik matrisi

✓ = sayfada açıkça var · ✗ = kaynak açıkça yok diyor ya da hiç listelenmemiş · ? = doğrulanamadı. Neredeyse tamamı **üretici beyanı**dır. "Var" demek "çalışıyor" demek değildir.

### 3.1 Blok A — banka/fatura/cari otomasyonu

| Özellik | Faturalog | Paraşüt | BizimHesap | KolayBi' | İşbaşı | Milyem+ | SarrafPro | Dezy | Netadam | Rahat Akt. | Finrota | Bulut Tahs. |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Banka hareketi otomatik çekme | ✓ (yöntem ?) | ✓ web servis + şube formu | ✓ | ✓ | ✓ şube başvurusu | ✓ salt-okur | ✓ | ✓ + PDF ekstre | ✓ API/MT940 | ✓ 4 banka API + Excel/PDF | ✓ | ✓ SOAP/JSON |
| Banka sayısı (beyan) | 28 | 14 (sayfa "13" diyor) | 19 | 22–25+ | 17 (SSS "tamamlanmadı" diyor) | 20+ | ? | 30 | 18 | 4 API | 29 (2023) | 25 logo |
| Excel/MT940/PDF ekstre yükleme | ? | ✓ Excel | ? | ? | ? | ? | ? | ✓ PDF | ✓ MT940 | ✓ | ? | ? |
| Eşleştirme ölçütü | "kural" (ayrıntı ?) | IBAN, TCKN | ? | ? | ? | IBAN + ad + geçmiş | TCKN (blog) | tutar + tarih + IBAN | IBAN, unvan, açıklamadaki fatura no, tutar, geçmiş | VKN/TCKN | işlem kodu kuralı | bayi ödeme kodu + öğrenme |
| Onay kuyruğu | çelişkili | ✓ ilk eşleşmede | ? | ? | ? | ? | ? | ✓ | ✓ belirsizde sor | ? | ? | ? |
| Otomatik cari açma | ✓ ("AI Hub") | ✗ (elle "yeni cari") | ? | ? | ? | ? | ? | ✗ | ? | ✓ VKN'den | ✓ | ? |
| Eşleşen tahsilatla açık borcu kapatma | ? | ✓ | ? | ✓ | ✓ | ✓ (cariye yazar) | ✓ | ✓ | ? | — | ✓ fiş | ✓ fiş |
| Banka hareketinden **yeni** fatura | ✓ | ✗ | ? | ? | ? | ✓ (özel matrahlı) | ✓ e-Fatura + gider pusulası | ✓ toplu, onaylı | ✗ | ✗ | ✗ | ✗ |
| e-Fatura/e-Arşiv mükellef ayrımı | ✓ (toplu fatura yazısı) | ✓ | ? | ? | ? | ? | ? | ✓ VKN sorgusu | ? | — | — | — |
| Gelen e-fatura yönetimi | ✓ onayla/reddet | ✓ | ? | ? | ✓ | ? | ? | ? | ? | — | — | — |
| Toplu fatura | ✓ Excel / grup | ? | ? | ? | ? | ? | ✓ | ✓ | ? | — | — | — |
| Tekrarlayan/zamanlı fatura | ✓ | ? | ? | ? | ? | ? | ? | ? | ? | — | — | — |
| Cari mutabakat e-postası | ✓ tek tık | ✓ Kolaymutabakat | ? | ✗ | ? | ? | ? | ? | ? | — | — | — |
| Tahsilat hatırlatma | ? | ✓ e-posta, vade günü | SMS paketi ? | "otomatik takip" (kanal ?) | ? | ? | ? | ? | ? | — | — | ✓ SMS tahsilat |
| Risk limiti | ✓ | ? | ? | ? | ? | ? | ? | ? | ? | — | — | — |
| MASAK | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✓ | ✗ | ✗ | — | — | — |
| KDV 23/e özel matrah | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ? | ✓ (805) | ✗ | — | — | — |
| Has/gram stok | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ gram değeri | ? | ? | ✗ | — | — | — |
| Pazaryeri | ✓ (SSS) | ? | ✓ | ✓ | ? | ? | ? | ✓ | ? | — | — | — |
| Mobil uygulama | ✗ (rozetler boş, mağazada yok) | ✓ iOS 4,6 | ✓ iOS 3,9 | ✓ iOS 3,4 | ✓ iOS 4,3 | web | ✓ | ✓ | ? | — | — | — |
| Kontör modeli | ✓ | ✓ | ✓ | paket içi | paket içi | ? | ? | ? | ? | ✓ | ? | paket |

Kaynaklar: Faturalog [F1][F3][F8][F10]–[F13][F23]; Paraşüt [M1a]–[M1h]; BizimHesap [M2a]–[M2d]; KolayBi' [M3a]–[M3c]; İşbaşı [M4]; Milyem PLUS [P5]; SarrafPro [P17a][P17b]; Dezy [M6]; Netadam [M9]; Rahat Aktarım [M22]; Finrota [M23a][M23b]; Bulut Tahsilat [M24].

### 3.2 Blok B — kuyumcu programları

Kısaltmalar: KS = KuyumSmart, TG = Terminal Gold/Kuvvem, MP = Milyem PLUS, KH = KuyumHub, KP = Kuyum Plus, KE = KuyumERP, SP = SarrafPro.

| Özellik | Pusula/X | Hasfiyat | KS | TG | MP | KH | Ariyen | CetaSoft | M&L | KP | Kuyumcum | KE | Gramino | SP |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Has/milyem stok | ? | ✓ | ✓ | ✓ | ? | ✓ | ✓ | ✓ | ? | ✓ | ? | ✓ | ✓ | ? |
| Ayar bazlı stok | ✓ (barkodsuz stok ayar kolonu, PUSULA-REF) | ? | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ? | ✓ | ? | ? | ✓ | ? |
| Barkod / RFID | ✓ / ✓ | ✓ / ? | ✓ / ✓ | ✓ / ✓ | ? | ? | ✓ / ✓ | ✓ / ? | ✓ / ✓ | ✓ / ✓ | ✓ / ✓ | ✓ / ? | ? | ? |
| Sarrafiye (adet) | ✓ (stokta ÇEYREK ZİYNET, ATA ALTIN; PUSULA-REF) | ? | ✓ 14 tür | ? | ? | ? | ✓ | ✓ | ? | ? | ? | ? | ✓ | ? |
| Hurda alış | ✓ (video; fişte "14 HURDASI" bacağı) | ? | ✓ | ✓ | ? | ✓ (7 gün kuralı) | ✓ | ✓ | ? | ✓ | ? | ✓ | ✓ | ? |
| Gider pusulası | ? | ✓ | ✗ otomatik değil | ? | ✓ (menü) | ✓ | ✓ | ✓ (ek lisans) | ? | ? | ? | ? | ? | ✓ otomatik |
| Müşteri emaneti | ? | ? | ✓ gram+ayar | ? | ? | ? | ✓ | ✓ | ? | ? | ✓ rapor | ? | ✓ | ? |
| Konsinye | ? | ? | ✗ | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? |
| Cari TL | ✓ | ✓ | ✓ | ✓ | ✓ | ? | ✓ | ✓ | ? | ✓ | ✓ | ✓ | ✓ | ✓ |
| Cari gram/has (veresiye gram) | ✓ (Açık Pozisyon HAS satırı) | ? | ✓ | ✓ | ? | ✓ | ? | ✓ | ? | ? | ? | ✓ | ? | ? |
| Açık pozisyon (has borç/alacak) | ✓ (ana ekran kutucuğu) | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? |
| Fişte ödeme bacakları (nakit + kart + eski altın + açık hesap) | ✓ | ? | ✓ satış+takas+hurda tek fiş | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? |
| Canlı fiyat kaynağı | ? | ✓ çok kaynak | ✓ Harem | ✓ (5 dk, adı yok) | ? | ✓ IAB/BIST/Kuyumcukent | ✓ | ✓ "3 kaynak" | ✓ kur | ? | ✓ | ? | ✓ | ? |
| TV fiyat panosu | ? | ✓ | ✓ | ? | ? | ? | ✓ | ? | ? | ? | ? | ? | ? | ? |
| e-Fatura/e-Arşiv | ✓ (Kivi) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ (ek lisans) | ? | ✓ | ✓ | ? | ? | ✓ |
| Alıcıya göre belge türünü otomatik seçme | ? | ? | ✓ | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? |
| KDV 23/e özel matrah | ? | ? | ✓ BIST kapanışıyla | ✓ | ✓ | ✓ | ? | ? | ? | ? | ? | ? | ? | ? |
| Banka hareketi otomatik | ✓ (Kivi, 6 banka) | ✓ (blog) | ✗ | ✓ 19 banka | ✓ 20+ | ✓ 28 | ✓ | ? | ? | ? | ✓ (ayrıntısız) | ? | ? | ✓ |
| Havaleden otomatik fatura | ? | ✓ toplu (blog) | ✗ | ? | ✓ | ? | ? | ? | ? | ? | ? | ? | ? | ✓ |
| Kart komisyonu / taksit otomatik | ✓ | ? | ✓ | ✓ | ? | ? | ? | ✓ | ? | ? | ? | ? | ✓ POS kontrol | ? |
| MASAK kimlik / tarama | ? | ✓ risk paneli | ✓ NFC, 370 bin | ✓ | ✓ | ✓ NFC, 185 bin | ✓ | ? | ? | ✓ | ? | ? | ? | ✓ |
| ÖKC/POS cihazı | ? | ? | ? | ? | ? | ? | ? | ✓ (ek lisans) | ? | ✓ | ? | ? | ? | ? |
| Hatırlatma | SMS/e-posta (şüpheli hareket) | ? | WhatsApp fiş | SMS paketi | ? | ? | ? | ✓ vadesi geçene SMS | ? | WhatsApp | ? | ? | ? | ? |
| Mali müşavir paneli / aktarım | ? | ? | ? | ? | ✓ | ✓ | ✓ muhasebe | ✓ resmi muhasebe paketi | ? | ? | ? | ? | ? | ✓ Zirve, Mikro konnektörü |
| Fiyat açık mı | ✗ | ✓ | ✓ | ✓ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |

Kaynaklar: § 2.2'deki kodlar. Pusula satırları için ayrıca [PUSULA-REF].

### 3.3 Matristen çıkan farklılaştırıcılar ve dikkat noktaları

- **Faturalog'a özgü (kuyumcu programlarında az görülen):**
  - otomatik cari açma ("AI Hub"),
  - tek tık cari mutabakat e-postası ve yanıt takibi,
  - risk limiti,
  - tekrarlayan/zamanlı fatura,
  - Excel'den toplu fatura.

  [F11][F13]
- **Kuyumcu programlarına özgü (Faturalog'da hiç yok):**
  - has/milyem,
  - sarrafiye,
  - hurda ve gider pusulası,
  - emanet,
  - 23/e özel matrah,
  - MASAK,
  - TV fiyat panosu,
  - RFID sayım,
  - fişte ödeme bacakları.

  Faturalog'un ürün kartında yalnız KDV, birim fiyat ve GTİP kodu var [F14][K].
- **Kimsenin açıkça göstermediği:**
  - konsinye,
  - "para geldi, eski borç mu, yeni mi, avans mı" sorusu,
  - gram veresiyeye TL tahsilatın hangi kurla düşüleceği.
- **Rakiplerin kendi metinlerindeki tutarsızlıklar:**
  - Paraşüt banka sayısında 13 ile 14 arasında çelişiyor.
  - İşbaşı "17 banka" diyor ama SSS'si entegrasyonun "henüz tamamlanmamış" olduğunu söylüyor.
  - Ariyen'in deneme süresi bir yerde 10 gün, başka bir yerde 2 ay.
  - Faturalog'da ücretsiz deneme bir sayfada var, öbüründe yok.
  - MASAK eşiği rakipler arasında farklı.

  Bu tutarsızlıklar pazarlama beyanlarına temkinle bakmak gerektiğini gösteriyor.
- **Yeni bulgu (Pusula kartını tamamlar):** PusulaX'te banka para girişinin cariyle eşleştirilmesi, sanal POS hareketinin bakiyeye işlenmesi ve e-Fatura işi **Kivi** iş ortaklığıyla geliyor. Sayılan bankalar: Akbank, Ziraat, Garanti, İş, Yapı Kredi, QNB [P1b][Ü]. Ayrıca SarrafPro'nun "Konnektör" modülü PUSULANET ile bağlanıyor [P17a][Ü]. Pusula kullanıcıları banka ve e-belge işini üçüncü taraf ürünle tamamlıyor olabilir. Müşterinin "Faturalog + diğer program" ikilisi bu kalıba uyuyor (**çıkarım**).

---

## 4. Faturalog — derin inceleme

> Bağımsız kaynak çok az. Bağımsız olarak doğrulananlar: GİB entegratör listesi, TCMB listeleri, IJS fuarı, LinkedIn kaydı, uygulama mağazası araması. Geri kalan her şey **üretici beyanı**. "Tüm özellikler" ancak müşterinin ekranından ya da kurucunun talep edeceği bir demodan kesinleşir.

### 4.1 Künye

- **Unvan ve kayıt:** FATURALOG BİLİŞİM HİZMETLERİ A.Ş. · Alata Mah. Hacı Ömer Var Cad. No:75/22 Erdemli/Mersin · VKN 3350740428 · MERSİS 03350740428000001. Kaynak KVKK ve gizlilik metinleri, v2.0, 2025 [F4][F5][Ü].
  - **Çelişki:** Site altbilgisi yalnız "İstanbul, Türkiye" yazıyor.
  - Metinde KEP adresi yerine "[KEP adresi eklenecek]" yer tutucusu duruyor.
  - VKN ve MERSİS bağımsız kaynakta bulunamadı; Ticaret Sicil Gazetesi'ne bakılmadı (**D**).
- **LinkedIn:** "Fintech yazılım platformu", kuruluş 2019, 2–10 çalışan, İstanbul Jewelry Show Nisan 2026, Salon 2 Stand 2P100 [F22][B platform / Ü içerik].
- **IJS fuarı:** 1–4 Nisan 2026 IJS'nin "Better Stands" Bronz listesinde "FATURALOG BILISIM HIZMETLERI A.S." adı geçiyor. Kuyumcu fuarına katılım bağımsız kaynakla doğrulandı [F21][B].
- **Kuyumcu müşteriler:** Ana sayfada 11 kuyumcu logosu/adı var [F1][Ü]. Biri "Liva Erdemli"; şirket adresi de Erdemli. Bu yerel bir başlangıca işaret edebilir (**D**, çıkarım). Kuyumcuya özel sayfa, vaka çalışması ya da özellik yok.
- **Sertifikalar:** ISO 27001 ve 9001 rozetleri yalnız görsel olarak duruyor. Belgeyi veren kuruluş ve belge numarası yok (**D**) [F1].
- **"Binlerce işletme" iddiası** doğrulanamadı [F7].

### 4.2 Site haritası

`sitemap.xml` 23 adres içeriyor [F2][K]:
- **Modül sayfaları:** açık bankacılık, cari, e-Fatura yönetimi, otomatik fatura, toplu fatura (iki sayfa), ürün yönetimi.
- **Tanıtım:** `/panel/` (örnek veriyle çizilmiş bir panel görseli).
- **Kurumsal:** fiyatlandırma, SSS, hakkımızda, iletişim.
- **Hukuki:** KVKK, gizlilik, çerez, hesap silme.
- **Blog:** 3 tarihsiz yazı; görsel yolları Aralık 2025'i gösteriyor.

Yok olanlar: ayrı kullanım koşulları sayfası, ayrı yardım merkezi (altbilgideki "Yardım/Destek Merkezi" bağlantıları iletişim sayfasına gidiyor), rakip kıyas sayfası.

Giriş portalı e-posta ya da telefon ve parola ile açılıyor; "Kayıt Ol" bağlantısı var [F17]. Bu araştırmada kayıt **olunmadı**.

### 4.3 Akışlar (üretici beyanı)

| Akış | Ne söylüyor | Belirsiz kalan | Kaynak |
|---|---|---|---|
| **Banka (açık bankacılık)** | TL, döviz ve kredi kartı hesapları tek panelde; "saniyeler içinde / anlık"; banka başına ayrı şifre/token gerekmiyor; Merkez Bankası ve BDDK düzenlemelerine atıf; 28 banka sayılıyor | Aracı kuruluşun adı, yöntem (HBH aracısı mı, banka web servisi mi), ekstre yükleme | [F1][F9][F16b] |
| **Otomatik fatura** | Gelen işlem sınıflandırılır → karşı taraf bulunur → cari açılır → "belirlenen kurallara göre" fatura kesilir → e-posta ile gider | Kural editörü, onay adımı, KDV mantığı, mükellef sorgusunun bu akışta yapılıp yapılmadığı | [F10] |
| **Cari / "AI Hub"** | Transferdeki kişi bilgisiyle cari anında açılır, eksik bilgi tamamlanır; bakiye, vade, tek tık mutabakat e-postası (yanıtlar izlenir), risk limiti | Yapay zekâ modeli ve doğruluk oranı verilmiyor; "eksik bilgi tamamlama"nın kaynağı yok | [F11] |
| **e-Fatura yönetimi** | Gelen faturayı tek tıkla onayla/reddet; vergi ve tevkifat otomatik dolar; 10 yıl "akıllı arşiv" | — | [F12] |
| **Toplu fatura** | Excel'den ya da kayıtlı müşteri grubundan; faturalar **önce onaya düşer**; zamanlanmış gönderim; abonelik faturası; alıcının e-Fatura/e-Arşiv durumu kontrol edilir | Adet limiti | [F13][F16a] |
| **Ürün** | Tek tek ya da toplu ürün kartı, satışta stok düşer, KDV, birim fiyat, GTİP | Altın, ayar, gram, has, işçilik **yok** | [F14] |
| **Pazaryeri** | SSS: Trendyol, Hepsiburada, N11, Amazon, kendi e-ticaret sitesi | Modül sayfalarında geçmiyor | [F8] |
| **Muhasebe programı** | Resmi beyan yok; Luca ve Zirve yalnız bir müşteri yorumunda geçiyor | Entegrasyonun varlığı | [F7] |
| **Panel (görsel tanıtım)** | Görseldeki örnekler: "bugün 12 otomatik fatura", "1.200 kontör kaldı", Enpara/Akbank/TEB bakiyeleri, hızlı işlemler (toplu fatura, **gider pusulası**, ürün ekleme), "FLF2025…" fatura numaraları | Gider pusulasının modül olarak var olduğu yalnız bu görselde geçiyor | [F15] |

**Çelişki:** Otomatik fatura sayfası akışı onaysız anlatıyor [F10]; toplu fatura sayfası onaylı anlatıyor [F13]. Müşterinin hangi kipte çalıştığı **sorulmalı**.

### 4.4 Fiyat ve kontör

[F3][Ü]

| Paket | Liste fiyatı | Satış fiyatı | Kontör başı | Destek |
|---|---|---|---|---|
| 100 kontör | 300 ₺ | 250 ₺ | 2,50 ₺ | Standart |
| 250 kontör ("en çok tercih edilen") | 650 ₺ | 500 ₺ | 2,00 ₺ | Özel tasarım |
| 500 kontör | 1.200 ₺ | 900 ₺ | 1,80 ₺ | Öncelikli |
| 1.000 kontör | 1.900 ₺ | 1.500 ₺ | 1,50 ₺ | 7/24 VIP |

- Paketlere dahil olanlar:
  - 12 taksit,
  - gelen ve giden fatura için 10 yıl saklama,
  - ücretsiz kurulum (panel, API bağlantıları, şablon),
  - komisyonsuz kullanım, işlem başına ek ücret yok,
  - süresi dolmayan, devreden kontör,
  - davette iki tarafa 250'şer kontör.
- KDV'nin dahil olup olmadığı yazmıyor (**D**).
- Müşteri yorumunda geçen 5.000 kontörlük paket listede yok.
- Ücretsiz deneme konusunda çelişki var: fiyat sayfası SSS'si demo görüşmesi öneriyor, SSS sayfası ise süresi belirtilmemiş ve kart istemeyen bir deneme olduğunu söylüyor [F3][F8].
- Kıyas için diğer ürünlerin kontör fiyatları:
  - Paraşüt: 100 adet 400 ₺ [M1d]
  - BizimHesap: 1.000 adet 1.800 ₺ [M2b]
  - Cariofis: 500 adet 1.200 ₺ [M15]

  Faturalog'un kontörü kıyaslananlar arasında en ucuzlardan.

### 4.5 Mobil, yorum, basın

- **Mobil:**
  - App Store ve Google Play rozetleri `#` adresine gidiyor.
  - Google Play'de "faturalog" araması sonuç vermiyor.
  - App Store'da da uygulama bulunamadı [F1][F23][B].
  - Hesap silme politikası mağaza kurallarına atıf yapıyor. Silme 30 günde yapılıyor. Destek kayıtları 3 yıl, hesap bilgileri sözleşme süresi + 10 yıl saklanıyor [F6]. Bu, uygulamanın hazırlıkta olabileceğine işaret ediyor ama **doğrulanamadı**.
  - Muadil araştırması "App Store ve Google Play var" diye not etmişti. Bunun dayanağı yalnız rozetler; mağazada ürün yok. **Sonuç: mobil uygulama yayında değil ya da bulunamıyor.**
- **Yorum ve şikâyet:**
  - Şikâyetvar'daki `/faturalog` sayfası HTTP 410 döndürüyor, aramada kayıt yok [F24][B].
  - Ekşi Sözlük 403 verdi.
  - YouTube'da video bulunamadı.
  - Sitedeki sosyal medya simgeleri `#` adresine gidiyor.
  - Basın, yatırım ve ödül haberi bulunamadı. Aramada çıkan "Faturalab" farklı bir şirket.
  - **Bağımsız kullanıcı görüşü yok.**
- **Çerez:** Üçüncü taraf reklam ve izleme çerezi kullanılmadığı beyan ediliyor [F25].

### 4.6 Mihenk için Faturalog'dan çıkan dersler (EK-2'yi düzeltir/derinleştirir)

| EK-2 kalemi | Düzeltme / derinleştirme |
|---|---|
| F1 Açık bankacılık | 28 banka **beyanı**. Yöntem bilinmiyor. Faturalog TCMB'de lisanslı değil, yani bir aracı ya da web servis kullanıyor olmalı. Müşterinin anlattığı "bankaya evrak götürüyoruz" yolu, Paraşüt ve İşbaşı'nın şube başvurusu yöntemiyle örtüşüyor [M1b][M4]. |
| F2 Otomatik fatura | Sayfalar çelişkili (onaysız / onaylı). Kuyumcu için havaleden fatura ürün ve has bilgisi ister (§ 5.2 satır 5). |
| F3 "AI Hub" | Yalnız pazarlama metni; model ve doğruluk verisi yok. Eşdeğer davranış rakiplerde kural tabanlı: Paraşüt'te IBAN öğretme, Rahat Aktarım'da VKN'den cari, Odoo'da Partner Mapping [M1c][M22][U6]. |
| F4/F5 Gelen fatura, mükellef ayrımı | Doğrulandı (üretici beyanı). Teknik altyapı **EDM Bilişim** [F4][F18]. |
| F6 Toplu fatura | Excel ve grup seçimiyle, onaylı. Zamanlı ve abonelik faturası var. |
| F8 10 yıl saklama | Mevzuatta e-Arşiv için ayrı bir 10 yıl hükmü yok. 10 yıl TTK 82'den, 5 yıl VUK 253'ten geliyor (§ 6.10). |
| F9 Luca/Zirve | Yalnız bir müşteri yorumunda geçiyor; resmi entegrasyon beyanı yok. |
| F10 Kontör | Tam tablo § 4.4. |
| F12 Mobil | Mağazada yok. |
| **Yeni** | Tek tık mutabakat e-postası + yanıt takibi; cari risk limiti; tekrarlayan fatura; gider pusulası (yalnız panel görselinde) [F11][F13][F15]. |

---

## 5. Otomasyon haritası (raporun kalbi)

### 5.1 Karar ilkeleri (kaynaklı)

1. **Kesin anahtarla eşleşme → otomatik, ama geri alınabilir ve "neden" etiketli.** Örnekler:
   - Xero JAX yalnız yüksek güvende otomatik mutabakat yapıyor; her kayıtta Match/Rule/Memory/Prediction etiketi var ve özellik banka hesabı başına açılıp kapanıyor [U4][K].
   - QuickBooks otomatik işlediği satıra "AUTO", kuralın tanıdığı satıra "RULE" rozeti koyuyor [U3][K].
2. **Büyük tutar her zaman insana.** QuickBooks "Ready to post" belirlenen tutar eşiğinin üstünü toplu onaydan çıkarıyor [U2][K]. Kuyumcuda tutarlar büyük olduğu için bu kalıp daha da önemli.
3. **Otomasyon kullanıcının yönlendirmesiyle adım adım artmalı.** Güven yüksekse ya da hata riski düşükse daha çok otomatikleştirilmeli [U14][K].
4. **Onay penceresi değil geri alma.** Fazla uyarı alışkanlık körlüğü yaratır. Onay yalnız ciddi ya da geri dönüşsüz işlerde ve olağandışı tutarlarda istenmeli [U16][K].
5. **Mevzuatın insana bıraktığı karar otomatikleştirilmez.** MASAK şüpheli işlem bildirimi buna örnek: şüphe oluştuktan sonra 10 iş günü süre var [L14][K]. Belgesiz işlemi kolaylaştırmak da bu kapsamda (EK-3 madde 3).
6. **Müşterinin niyeti veride yoksa sorulur.** Para girişinin amacı (borç, alışveriş, avans, emanet) banka mesajında yapısal olarak yer almıyor. Açıklama alanı serbest metin ve bankadan bankaya standart değil [M27][B].

Tablodaki kademe işaretleri:
- **OTO** = sistem yapar; satırda "neden" etiketi ve geri al düğmesi bulunur.
- **ÖNERİ** = sistem hazırlar, tek dokunuşla onaylanır; toplu onay verilebilir.
- **SOR** = karar kullanıcınındır; sistem seçenekleri sıralar ama hiçbirini önceden seçmez.

### 5.2 Akış akış harita

| # | İş akışı | Rakipler ne yapıyor | **OTO** (güvenli) | **ÖNERİ + onay** | **SOR** (mutlaka) ve nedeni |
|---|---|---|---|---|---|
| 1 | **Para girişi kaydı** (banka hareketinin sisteme düşmesi) | Herkes otomatik çekiyor: Faturalog "anlık", Terminal Gold "1 dk", Milyem+ "anında", Luca bazı bankalarda gün sonu [F9][P4a][P5][M20]. QuickBooks'ta bağlantı kopunca mükerrer hareket şikâyeti var [U13] | Hareketi çek, **banka referansıyla tekilleştir**, salt-okur bağlan (Milyem+ ve KuyumHub "salt-okur" diyor [P5][P6a]) | — | Hiçbiri. Not: personel rolüne göre görünürlük ayrı bir karar (EK-3 madde 6) |
| 2 | **Gönderenin cariyle eşleşmesi** | Paraşüt: IBAN ya da TCKN kayıtlıysa otomatik, değilse ilk seferde kullanıcı eşler ve IBAN cariye yazılır [M1c]. Milyem+: IBAN + ad + geçmiş [P5]. Netadam: belirsizse kullanıcıya sorar [M9]. Odoo: açıklamadan müşteri bulur [U6]. QuickBooks: birden çok adayda en eskiyi seçer (şikâyet) [U12] | **Tek** aday ve kesin anahtar: daha önce onaylanmış IBAN ya da gelen TCKN/VKN cari kaydıyla birebir aynı | Ad benzerliği tek adaya işaret ediyorsa; açıklamada cari kodu ya da fiş no geçiyorsa | **Birden çok aday** ya da ad tutup IBAN yeni ise. Neden: yanlış cariye yazılan para hem borcu hem MASAK izini bozar. TCKN mesajda garanti değil (MASAK md.24 [L14]) |
| 3 | **Yeni müşteri (cari) açma** — kurucu kararı: "bulamazsa otomatik kaydetsin" (PAKET-PLANI M8) | Faturalog "anında açar" [F11]. Rahat Aktarım VKN'den otomatik cari açar [M22]. Paraşüt otomatik açmıyor, elle açılıyor [M1e]. Dezy'de bu özellik yok [M6] | Eşleşme yoksa **"bankadan gelen"** etiketli cari açılır. İçinde yalnız bankanın verdiği ad, IBAN ve (geldiyse) TCKN/VKN bulunur. Uydurma alan yazılmaz | Muhtemel aynı kişi için **birleştirme önerisi** (aynı ad, farklı IBAN; ortak hesap) | **Ad tek başına** yetmez, iki farklı kişi aynı adı taşıyabilir. Bankadan gelen cari MASAK kimlik tespiti yapılmış sayılmaz (md.5, 370.000 TL eşiği [L14]). KVKK dayanağı: md.5/2-ç (hukuki yükümlülük: VUK, 459 tevsik) + c (sözleşmenin ifası) en güçlü dayanaklar. Pazarlama amacına genişletilirse açık rıza gerekir. Veri ilgili kişiden değil bankadan geldiği için aydınlatma ilk iletişimde yapılmalı ([L15][K]; Aydınlatma Tebliği **O**) |
| 4 | **Borç düşme (veresiye kapama)** | Paraşüt, İşbaşı, KolayBi' eşleşen carinin açık faturasını kapatıyor [M1c][M4][M3a]. QuickBooks en eski açık faturaya uyguluyor; ayar kapatılabiliyor [U8]. Xero fazla ödemeyi "overpayment/prepayment" olarak tutuyor [U9]. Odoo ödemeyi "outstanding" olarak bekletip yeni faturada bant gösteriyor [U10]. KolayBi''de "ödeme düştü, fatura eşleşmedi" şikâyeti var [M3c] | Carinin **tek** açık TL borcu var ve gelen tutar ona **tam eşit** | Birden çok açık borç: önerilen dağıtım en eskiden yeniye (QuickBooks varsayılanı), kuyumcu tek dokunuşla değiştirebilir. Küçük eksik ödeme için tolerans (Odoo kalıbı [U6]) | **(a)** Açık borç yok ya da gelen tutar borçtan fazla. Neden: para avans, yeni alışveriş, emanet, iade ya da kişisel transfer olabilir; niyet veride yok. **(b) Gram cinsinden veresiye** (has/22 ayar borcu). TL tahsilatın hangi kurla (bugünkü satış, alış, anlaşılan kur) kaç grama düşeceği müşteriyle yapılan anlaşmaya bağlı ve kur riski taşıyor. Rakiplerin hiçbirinde bu sorunun nasıl çözüldüğü bulunamadı (**D**). **(c)** Kısmi ödemede "kalanı açık bırak / indirim yap" kararı (QuickBooks "Leave as underpayment" [U8]) |
| 5 | **Banka hareketinden fatura kesme** | Faturalog otomatik (onay çelişkili) [F10][F13]. Milyem+ otomatik, sayfaya göre 4,2 sn [P5]. SarrafPro ve Hasfiyat toplu [P17b][P2c]. Dezy toplu ama kullanıcı **onaylıyor** [M6]. Genel ön muhasebe ürünleri kesmiyor [M1e] | Yalnız hareket **mevcut bir satış ya da siparişle** eşleştiyse ve o satışın belgesi henüz kesilmediyse. Hareket faturayı değil satışı tamamlar | Kuyumcunun tanımladığı **kural** varsa, örneğin: "Bu IBAN toptan alıcı, havale = X ürün satışı, e-Fatura". Kural önizlemesi ve ilk N işlemin onayı (PAIR: "kademeli artır" [U14]) | **Eşleşen satış yoksa her durumda.** Nedenler: 23/e özel matrahı için has gramı gerekir, havale ürünü taşımaz [L7][L8]. Zaten faturalanmış satışın tahsilatını yeniden faturalamak çift fatura ve çift KDV beyanı demek. Kişisel ya da ortak transfer satış değildir |
| 6 | **e-Fatura / e-Arşiv seçimi** | Faturalog (toplu yazısında), Paraşüt, KuyumSmart, Dezy alıcının kaydına bakıp otomatik seçiyor [F16a][M1f][P3c][M6] | **Tam otomatik.** Alıcı e-Fatura'ya kayıtlıysa e-Fatura, değilse e-Arşiv (509 § IV.2.1 [L2][K]) | — | Mükellef sorgusu yanıt vermezse belge kesilmez, sıraya alınır; tahminle tür seçilmez |
| 7 | **Fiş mi fatura mı** (belge türü) | Müşteri: "fişe gerek yok, direkt fatura" (görüşme madde 5). KuyumSmart ve Milyem+ satıştan e-Arşiv kesiyor [P3c][P5] | Eşik üstü satışta faturayı **zorunlu** kıl; eşik yasal parametredir. Fatura haddi 2026'da 12.000 TL [L5]. Kuyumcuda 3 kat (514 md.4) → 36.000 TL; bu bizim hesabımız, yürürlük teyitli değil [L6] | Kuyumcunun Ayarlar'daki varsayılanı (ör. "her satış e-Arşiv") her satışta önceden seçili gelir | Eşik altında müşteri fatura isterse fatura zorunlu (VUK 232). ÖKC ilişkisi mali müşavire sorulmalı: muafiyet şartlarını taşımayan kuyumcu ÖKC kullanmaya devam eder [L20][L21] (§ 6.11) |
| 8 | **KDV özel matrah** | KuyumSmart: BIST son işgünü kapanışı [P3c]. Dezy: 805 kodu + has düşümü [M6]. Terminal Gold, Milyem+, KuyumHub "özel matrah" diyor [P4a][P5][P6a] | Matrah = satış − (has gram × BIST son resmî işgünü külçe kapanışı); fatura tipi OZELMATRAH, kod 805 (gümüşte 808) [L8][L9] | Hesaplanan "binde" oranı gösterge olarak (EK-4) | Kuyumcunun **sabit "binde"** ile matrahı elle belirlemesi: yalnız mali müşavir onaylı, kilitli seçenek (PAKET-PLANI notu). Pırlantalı üründe taş bedeli → mali müşavir |
| 9 | **Stok düşme** | Faturalog satış faturasıyla düşüyor [F14]. Kuyumcu programlarında satışla düşüyor | Satış/fiş kaydında otomatik. **Banka hareketi stoğu değiştirmez** | Sayım farkı için düzeltme önerisi (RFID sayımı: KuyumSmart, Terminal Gold [P3a][P4a]) | Fire/tolerans dışı gram farkı. Neden: re'sen tarhiyat riski (görüşme EK § G) |
| 10 | **Fiyat ve kur** | Terminal Gold 5 dk'da bir; KuyumSmart Harem canlı; Platin360'ta "devre kesici" ani dalgalanmada güncellemeyi durduruyor [P4a][P3b][P20] | Kaynaktan çekme, saat damgası, **bayat uyarısı** | Ani sıçramada güncellemeyi durdurup onay bekleme (Platin360 kalıbı) | Kuyumcunun kendi fiyatı ve ofseti: müşteri sözü, "elle müdahale şart" (görüşme madde 1). Lisanssız kaynak kullanımı kurucu kararı (M10) |
| 11 | **Gün sonu** | Milyem+: gün sonu ciro + MASAK taraması [P5]. Pusula: Açık Pozisyon kutucuğu [PUSULA-REF]. Paraşüt: ekstre ile program bakiyesi farkı sıfırlanınca "Bakiyeyi sabitle" [U7] | Özet hesabı: ciro, kasa, banka, has pozisyonu | **Gün sonu toplu e-Arşiv listesi** (Faturalog toplu faturası gibi onaya düşer [F13]); eşleşmemiş hareketlerin listesi | Kasa sayım farkı: kuyumcu açıklama girer, sistem farkı kendiliğinden kapatmaz |
| 12 | **MASAK kimlik tespiti** | KuyumSmart ve KuyumHub NFC ile kimlik okuyor; eşikler farklı (370 bin / 185 bin) [P3a][P6a]. SarrafPro TCKN tespiti + MASAK sorgusu [P17b] | Eşik kontrolü: **370.000 TL**, birbiriyle bağlantılı işlemler toplanır (md.5) [L14]. Yaptırım listesi taraması | NFC/kimlik okuyucudan alan doldurma | Kimlik doğrulamanın tamamlanması (kişi ve belge karşısında). **Şüpheli işlem bildirimi asla otomatik değil**: şüphe insan kararıdır, süre 10 iş günü (md.28/2) [L14] |
| 13 | **459 tevsik** (banka/PTT zorunluluğu) | Mevcut Mihenk kapısı (mevzuat belgesi V-03). Rakiplerde bulunamadı | 30.000 TL üstü nakit tahsilat/ödemeyi **durdur** [L12][K]. Aynı gün aynı cari için kümülatif hesap | — | Yabancıyla işlem istisnası: faturaya pasaport no gerekiyor (575 h bendi [L12]). Kısım kısım ödeme (mevzuat belgesi V-03b) |
| 14 | **Emanet** | KuyumSmart emaneti gram + ayarla cariye bağlıyor [P3a]. Ariyen, CetaSoft, Gramino emanet takibi yapıyor [P7a][P8a][P13] | — | — | **Her zaman sor.** Banka parası ya da teslim alınan altın emanet olarak otomatik işaretlenmez. Neden: emanet satış ve borçtan farklı hukuki ilişkidir ve kaydı müşterinin beyanına dayanır |
| 15 | **Hurda alış + gider pusulası** | SarrafPro her alım için otomatik gider pusulası hazırlıyor [P17b]. KuyumHub 7 gün kuralını izliyor [P6a]. Faturalog panelinde gider pusulası kısayolu var [F15]. KuyumSmart pusula kesmiyor [P3a] | Mükellef olmayan kişiden alımda **taslak** gider pusulası ve 7 gün sayacı (VUK 234) [L10]. Bedel 7 gün içinde bankadan ödendiyse "banka belgesi pusula yerine geçer" notu (234/5) [L10] | e-Gider Pusulası gönderimi (573 ile ıslak imzasız doğrulama imkânı, kapsam GİB kılavuzuna bağlı [L3]) | Stopaj: mükellef olmayanın zati eşya olarak sattığı hurda altında tevkifat yok (özelge, **O** [L11]). Yine de mali müşavire sorulmalı. Hurdanın rafineriye satışında KDV istisnası **D** |
| 16 | **Mutabakat** | Faturalog: tek tık cari mutabakat e-postası, yanıtlar izleniyor [F11]. Paraşüt: Kolaymutabakat [M1g]. Mikro: e-Mutabakat (KEP + e-imza) [M16b]. Paraşüt banka ekstresi mutabakatında fark sıfırlanınca bakiyeyi sabitliyor [U7] | Banka bakiyesi farkının hesaplanması | Mutabakat mektubunun hazırlanması | **Gönderim** müşterinin onayıyla olur. Dış muhataba giden ileti olduğu için kullanıcı gönderir |
| 17 | **Hatırlatma ve tahsilat takibi** | Paraşüt: vade gününde e-posta, aynı günün faturaları tek e-postada [U17]. Xero: vadeden 7, 14, 21 gün sonra [U18]. CetaSoft: vadesi geçene SMS [P8a]. OkCredit: WhatsApp ve SMS, ödeme bağlantılı [U20]. Chaser: ödeme gelince hatırlatma kendiliğinden durur [U19] | Vade takvimi, geciken listesi, ödeme gelince hatırlatmanın durması | Hatırlatma planı **müşteri başına açılır** (varsayılan kapalı); mesaj metni önizlenir | İlk gönderim ve kanal seçimi. Neden: ticari ilişki hassas (kuyumcuda tanıdık müşteri). Ayrıca SMS/WhatsApp ile borç hatırlatmasının ticari elektronik ileti (6563 / İYS) kapsamında olup olmadığı **araştırılmadı (D)** → açık soru |
| 18 | **Kart/POS komisyonu, taksit, blokaj** | Terminal Gold, PusulaX/Kivi, CetaSoft, KuyumSmart otomatik işliyor [P4a][P1b][P8a][P3a] | Otomatik (sözleşmedeki oran tablosundan) | — | Oran tablosunun ilk girişi |
| 19 | **Gelen e-fatura** (toptancı işçilik faturası) | Faturalog: onayla/reddet, vergi otomatik dolar [F12] | Gelen faturayı listeye düşür | Alış kaydına ve işçilik belgesine eşleştirme önerisi (M9 bağlantısı) | Reddetme kararı. Neden: ticari itiraz kullanıcı kararıdır |
| 20 | **e-Belge saklama** | Faturalog 10 yıl beyanı [F3] | Silinmez arşiv, en uzun süre: 10 yıl (TTK 82) [L19] | — | — |

### 5.3 "Para geldi" senaryosu — karar ağacı (tasarım değil, araştırmadan çıkan soru sırası)

1. Hareket daha önce görüldü mü (banka referansı)? → Evet ise **atla** (QuickBooks mükerrer dersi [U13]).
2. Gönderen tek ve kesin biçimde tanındı mı (onaylı IBAN ya da birebir TCKN/VKN)? → Hayır ise:
   - aday yoksa **"bankadan gelen" cari aç** (kurucu kararı),
   - birden çok aday varsa **SOR**.
3. Bu carinin hareketle eşleşen **satış ya da siparişi** var mı? → Varsa satışı tamamla. Belgesi kesilmemişse belge türünü 5.2 satır 6–7'ye göre kes (**OTO**, kuyumcu varsayılanı "otomatik kes" ise).
4. Açık **TL** borcu var mı?
   - Tek borç ve tam tutar → **OTO** düş.
   - Birden çok borç → **ÖNERİ** (en eskiden).
   - Fazla tutar → **SOR**: kalan avans mı, alışveriş mi, emanet mi?
5. Açık **gram** borcu var mı? → **SOR**: hangi kurla kaç gram düşülecek? Sistem kuru önerir ama seçmez.
6. Hiçbiri yoksa → **SOR**, seçenekler eylem adıyla sıralanır: "Yeni satış" · "Avans/ön ödeme" · "Emanet" · "Satış dışı (kişisel/ortak/iade)".
7. Hepsinde: tutar MASAK eşiğine yakınsa ya da bağlantılı işlemlerle aşıyorsa kimlik tespiti uyarısı.

---

## 6. Mevzuat çerçevesi (birincil kaynak)

> Mevzuat kalemleri koda girmeden önce mali müşavir ve hukukçu tarafından son kez teyit edilmeli. Aşağıdakiler bilgi amaçlıdır, hukuki görüş değildir.

### 6.1 e-Fatura ve e-Arşiv zorunluluğu — **K**

- **Genel e-Fatura eşiği:** 2022 ve sonraki yıllar için brüt satış hasılatı **3 milyon TL** (509 § IV.1.4(a)(1), 535 ile değişik) [L2][L1].
  - Geçiş, eşiği aşan yılı izleyen 7. ayın başında: 2025 hasılatı eşiği aşan **1.7.2026**'ya kadar geçer.
  - 2026'da bu eşiği değiştiren bir tebliğ bulunamadı.
  - İnternetten satış yapanlar ile gayrimenkul ve motorlu taşıt işlerinde eşik 500 bin TL.
- **Kuyumcuya özel e-Fatura bendi yok.** 509 IV.1.4'ün 1–9. bentlerinde kıymetli maden ya da kuyum geçmiyor. Kıymetli maden yalnız IV.10.1'deki e-Döviz/Kıymetli Maden Alım-Satım Belgesi'nde geçiyor ve bu, döviz büfesi yetkisi olanlar için [L2].
- **e-Arşiv:** 573 s. tebliğ (RG 12.11.2024/32720) ile e-Arşiv'e dahil olmayanlar faturalarını GİB portalından e-Arşiv olarak düzenler [L3][L4]:
  - 2025'te vergiler dahil 3.000 TL'yi aşan faturalar,
  - **1.1.2026'dan itibaren tutara bakılmaksızın** tüm faturalar.
  - 589 s. tebliğ (RG 31.12.2025/33124 5. Mük.) basit usul ve işletme esası için tarihleri bir yıl öteledi. Bilanço esasındaki kuyumcu için 2026 kuralı geçerli.
- **Alıcıya göre tür:** e-Fatura mükellefi kayıtlı alıcıya e-Fatura, diğerlerine e-Arşiv düzenler (509 § IV.2.1) [L2].
- **İnternet satışı:** e-Arşiv elektronik iletilir ve ek bilgi şartı vardır (IV.2.4.5) [L2].
- **Düzeltme:** Görüşme belgesi EK § E'deki "509 sayılı tebliğ 1/1/2026" ifadesi aslında **573 ile değişik 509 IV.2.4.3** hükmüdür.

### 6.2 Fatura düzenleme haddi — **K** (514 yürürlüğü **D**)

- VUK 232 haddi **2026'da 12.000 TL**. Kaynak 588 s. VUK GT (RG 31.12.2025/33124 5. Mük.), yeniden değerleme oranı %25,49 [L5].
- Aynı listedeki diğer 2026 tutarları: 353/1 fatura vermeme cezası 17.000 TL; 355 tevsike uymama yıllık üst sınırı 35.000.000 TL.
- **514 s. VUK GT md.4** (RG 4.1.2020/30998): İşlenmiş kıymetli maden ve taş ile bunlardan mamul eşya satışında had, işlem yılı haddinin **3 katı** [L6]. Kuyumcunun başka faaliyetleri için normal had geçerli.
  - Kaldırıldığına dair kayıt bulunamadı.
  - 2026 için 36.000 TL bizim hesabımız; hiçbir kaynak bu rakamı yazmıyor.
- `mevzuat-gereksinimleri.md` § 8'deki "dört çelişen rakam" (12.000 / 36.000 / 3.000.000 / 5.000.000) böyle ayrışıyor:
  - 12.000 TL: genel fatura haddi.
  - 36.000 TL: kuyumcuya 3 kat (hesap).
  - 3.000.000 TL: e-Fatura geçiş eşiği.
  - 5.000.000 TL: 2018–2020 dönemlerinin eski e-Fatura eşiği.

  Bunlar farklı kavramlar, birbiriyle çelişmiyor.

### 6.3 KDV — **K**

- **KDVK 23/e:** Ziynet eşyası ve sikke altında matrah, külçe altın bedeli düşüldükten sonra kalan tutar [L7].
- **KDVK 17/4-g:** Külçe altın ve gümüş teslimi istisna. Aynı bentte metal hurda ve atık teslimi de istisna [L7].
- **KDV GUT III/A-4.2.1:** Has bedel = **Borsa İstanbul son resmî işgünü külçe altın kapanışı × ayar**. Borsada işlem olmayan günde de son işgünü kapanışı esas alınır [L8].
  - Hurda dahil külçeden imalat yapanlar aldıkları işçilik üzerinden ayrıca KDV hesaplar.
  - Gümüşte aynı yöntem 4.2.2'de.
- **UBL-TR kod listesi v1.27** [L9]: fatura tipi `OZELMATRAH`, kod **805** (altın ziynet ve sikke), **808** (gümüş), istisna kodu **230** (17/4-g). 16.03.2026 tarihli UBL-TR 1.2.1 güncel listesi açılamadı; kodların değişmediği **D** [L22][O].
- **"Binde X" sabit oranı mevzuatta yok.** EK-4 bulgusu korunuyor.

### 6.4 Hurda alış ve gider pusulası

- **VUK 234 (K)** [L10]:
  - Belge düzenleme zorunluluğu olmayandan alımda gider pusulası düzenlenir.
  - Süre malın tesliminden itibaren 7 gün; süresinde düzenlenmeyen pusula hiç düzenlenmemiş sayılır.
  - 234/5: Bedel bu süre içinde banka, ödeme kuruluşu ya da PTT ile ödenirse, bunların belgesi pusula yerine geçer.
  - 573 ile e-Gider Pusulasında ıslak imzasız doğrulama imkânı getirildi; kapsamı GİB kılavuzuna bağlı [L3].
- **Stopaj (O):** Muğla VDB özelgesine (30.06.2022) göre mükellef olmayanın zati eşya olarak sattığı hurda altına yapılan ödemede GVK 94 kapsamında tevkifat **yok** [L11]. Hurda toplayıcısı muaf esnafa %2 tevkifat ayrı bir durum.
- **KDV (D):** Kuyumcunun hurdayı rafineriye satışının "metal hurda" istisnasına mı, külçe istisnasına mı girdiği netleşmedi; özelge gerekli.

### 6.5 MASAK — **K**

- **Yükümlülük:** Tedbirler Yönetmeliği md.4/1-k kıymetli maden, taş ve mücevher alım satımı yapanları yükümlü sayar [L14].
- **7 Ekim 2026 değişikliği** (RG 07.10.2026/33393, CB Kararı 11845, yayımı tarihinde yürürlükte) [L13]:
  - md.5/1-b kimlik tespiti: 185.000 TL → **370.000 TL**.
  - md.5/1-c elektronik transfer: 15.000 → **30.000 TL**.
  - md.24 ve 24/A eşikleri: **30.000 TL**.
  - md.16: SMS OTP ya da internet kanalıyla doğrulamada imza örneği alınmayabilir; yalnız "finansal kuruluşlar" için.
  - mevzuat.gov.tr derlenmiş metni bu değişiklikleri işlemiş [L14].
- **Tutardan bağımsız kimlik tespiti:** sürekli iş ilişkisi, şüpheli işlem, mevcut bilgilerin doğruluğundan şüphe. Bağlantılı işlemler toplanır.
- **Şüpheli işlem bildirimi:** şüphe oluştuktan sonra en geç **10 iş günü** (md.28/2).
- **Saklama:** **8 yıl** (md.46).
- **Kıymetli madene özel nakit sınırı** bulunamadı (**D**).
- **Projeye etkisi:** `mevzuat-gereksinimleri.md` M-01 (185.000 TL) ve "2026'da değişiklik bulunamadı" notu güncel değil. Kod tarafının (`MasakEsikleri.KimlikEsigi`) yapılandırmadan okunduğu yazıyor. Değerin güncellenmesi ayrı bir iş; burada hiçbir şey değiştirilmedi.

### 6.6 459 s. VUK GT (tevsik) — **K**

- Had **30.000 TL** (575 s. tebliğ, RG 30.11.2024/32738; eski 7.000 TL) [L12].
- Yabancılar ve Türkiye'de yerleşik olmayanlar kapsam dışı; faturaya pasaport no yazılması şart (h bendi).
- Kuyuma özel hüküm yok.
- 2026'da tutarın değiştiğine dair kayıt bulunamadı.

### 6.7 KVKK — **K** (yorum kısmı hukuki değerlendirme)

- **md.3/d:** Belirlenebilir kişiye ilişkin her bilgi kişisel veridir; gönderen adı ve IBAN bu tanıma girer (tanımdan çıkarım) [L15].
- **md.5/2:** Açık rıza gerektirmeyen dayanaklar: a kanunda öngörülme, c sözleşme, ç hukuki yükümlülük, f meşru menfaat [L15].
  - Bankadan cari açmak için en güçlü dayanak ç (VUK kayıt düzeni, 459, MASAK); yanında c (alacağın kapatılması).
  - Pazarlamaya genişletilirse açık rıza gerekir.
- **md.10 aydınlatma:** Veri elde edilirken yapılır. Veri bankadan geldiğinde Aydınlatma Tebliği'nin "ilk iletişim / makul süre" kuralı uygulanır (Tebliğ okunmadı, **O**).
- **VERBİS (O):** 2023/1154 kararıyla yıllık 50 çalışandan ve 100 milyon TL bilançodan az olanlar istisna. 2025/2393 kararıyla bilanço esasına göre defter tutmayanlarda yalnız çalışan sayısına bakılır [L23].

### 6.8 Açık bankacılık ve veri çekme yolları

- **6493 (K)** [L16]:
  - md.12/1-f ödeme emri başlatma hizmeti (ÖBH), md.12/1-g hesap bilgisi hizmeti (HBH).
  - md.13/2: Banka ve ödeme hizmeti sağlayıcısı dışındakiler ödeme hizmeti sunamaz.
  - md.12/2-ğ: Fon sahibi olmayan teknik hizmet sağlayıcının veri işleme ve BT hizmeti ödeme hizmeti sayılmaz.
  - md.28/1: İzinsiz faaliyetin cezası 1–3 yıl hapis ve 5.000 güne kadar adli para cezası.
- **TCMB Yönetmeliği (RG 1.12.2021/31676, K)** [L17]:
  - Yalnız HBH için yapılan başvuruda sermaye şartı yok (md.11/17).
  - HBH sağlayıcısı kimlik doğrulama bilgisi dahil hassas veri isteyemez ve veriyi amaç dışı kullanamaz (md.61/2).
  - Altyapıyı BKM sağlar (GEÇİT, md.59).
  - Çıkarım: müşterinin bankacılık parolasını alıp ekran kazımak bu modelle çakışıyor.
- **BDDK Bilgi Sistemleri Yönetmeliği (RG 15.3.2020/31069, K)** [L18]:
  - Açık bankacılık servisleri tanımı müşteri adına hareket eden tarafların API, web servis ya da dosya transferiyle erişimini kapsıyor (md.3/a).
  - Bu servislerde tek bileşenli kimlik doğrulamaya şartlı izin var (md.41).
- **Üç yol:**
  - (a) Mihenk'in kendisinin HBH lisansı alması.
  - (b) Lisanslı bir HBH ile iş birliği. Listede 7 ödeme kuruluşu (Logo Ödeme dahil) ve 19 elektronik para kuruluşu var. Sütun eşlemesi otomatik çıkarımla yapıldı; gözle teyit gerekli [M18][M19].
  - (c) Müşterinin şubede imzaladığı başvuruyla bankanın kurumsal web servisini kullanmak. Paraşüt ve İşbaşı bu yolu kullanıyor [M1b][M4]. Bunun **lisans gerektirmediğini söyleyen kaynak bulunamadı (D).**
- **Banka ücretleri:** Paraşüt kılavuzuna göre İş Bankası hesap başına yıllık 295 ₺ ya da aylık 25 ₺ alıyor (BSMV hariç); Vakıf ve Yapı Kredi ücret alabiliyor [M1b][K].
- **Çekilen veri açısından:** Bağımsız bir mali yazı (Alomaliye, 2.1.2026) her banka için ayrı talimat gerektiğini ve açıklama alanlarının standart olmadığını söylüyor [M27][B]. Pazarlamadaki "açık bankacılık" ifadesi lisanslı HBH anlamına gelmiyor: Faturalog, Finrota ve Bulut Tahsilat TCMB listelerinde yok [M18][M19].

### 6.9 Banka hareketinde gönderen kimliği

- **Mevzuat (K):** MASAK md.24'e göre 30.000 TL ve üstü transferde ad ve hesap no zorunlu. Ek olarak adres, doğum bilgisi, müşteri no, TCKN, pasaport no ya da VKN'den **biri** gerekiyor. Altında yalnız ad ve hesap/referans no zorunlu [L14].
- **Teknik (D):** Doğrulanamayanlar:
  - TCMB EFT ve FAST mesaj formatları kamuya açık değil.
  - BKM ÖHVPS hareket şemasına erişilemedi.
  - Kuveyt Türk ve VakıfBank API sayfaları 404 verdi.
  - Ziraat WSDL bağlantısı reddedildi.
  - "SenderIdentityNumber" ya da "KarsiTarafVKN" gibi alanların varlığı.
- **Rakip beyanları (Ü):** Paraşüt IBAN ya da TCKN ile eşleştirdiğini [M1c], SarrafPro ödemedeki TCKN'yi "otomatik tespit" ettiğini [P17b] söylüyor. Hangi bankadan bu alanın geldiği belirtilmiyor.
- **Yol:** Her bankanın sandbox'ından gerçek yanıt almak (Garanti, İş, Kuveyt Türk sandbox'ları görüşme EK § F'de not edilmişti).

### 6.10 Saklama — **K**

| Kaynak | Süre | Başlangıç |
|---|---|---|
| VUK 253/254 | 5 yıl | İzleyen takvim yılı |
| TTK 82/5 | **10 yıl** | Takvim yılı sonu |
| MASAK md.46 | 8 yıl | Belge tarihi / son işlem |

- 509 § VI e-Belge için ayrı bir süre koymuyor, "kanuni süreler" diyor. Saklama yurt içinde yapılır.
- Pratikte en uzun süre: **10 yıl** [L1][L19][L14][L2].

### 6.11 ÖKC — **K** (483'ün sonraki değişiklikleri **D**)

- **Zorunluluk:** 3100 s. Kanun md.1'e göre nihai tüketiciye satış yapan 1. ve 2. sınıf tüccar ÖKC kullanır [L20]. 557 s. tebliğ yeni nesil ÖKC ve kartla ödeme zorunluluğunu getirdi [L21].
- **Muafiyet (483 s. tebliğ md.6)** [L24]. Şartlar:
  - 10 milyon TL hasılat, 10 milyon TL aktif, 1 milyon TL özsermaye kriterlerinden en az ikisi,
  - 20'den fazla ÖKC ya da faturalama sistemi,
  - e-Fatura, e-Arşiv ve e-Defter kaydı,
  - tüm satışları e-Arşiv ile belgeleyeceğini bildirme.
- **509 § V.5.2:** Muaf olanlar ile Güvenli Mobil Ödeme (507) kullananlar, 232 tutarına kadar perakende satışta "NİHAİ TÜKETİCİ" yazılı e-Arşiv düzenleyebilir. Bu e-Arşiv ÖKC fişi yerine geçer.
- **Sonuç:** Tipik kuyumcu ÖKC'ye devam eder. Müşterinin "fiş iptal, direkt fatura" isteği, eşik altında ÖKC'nin yerine e-Arşiv kesmeyi gerektiriyor. Bunun her kuyumcu için mümkün olup olmadığı **mali müşavire sorulmalı** (§ 8).

---

## 7. Arayüz kalıpları — "kullanıcıyı yormayan" iyi ve kötü örnekler

### 7.1 İyi kalıplar (somut kaynaklı)

| # | Kalıp | Nerede | Kaynak |
|---|---|---|---|
| 1 | **Üç kademeli güven:** yeşil rozet (yüksek), mavi rozet (orta), düşükte öneri yok, kullanıcı "Bul" ile arar. Yeni firmada temiz eşleşme bile birkaç kez kabul edilene kadar mavi kalır | QuickBooks | [U1] |
| 2 | **"Hazır olanlar" toplu onay:** yalnız bütün kontrolleri geçenler işaretli gelir, tek "İşle" basılır. Eşik üstü tutar her zaman kuyrukta kalır. Bekleyen satır nedenini yazar ("kategori tahmini düşük güvenli") | QuickBooks Ready to post | [U2] |
| 3 | **Neden etiketi:** otomatik kayıtta "Eşleşme / Kural / Hafıza / Tahmin" | Xero JAX; QuickBooks RULE/AUTO | [U4][U3] |
| 4 | **Otomatik yapılanların gözden geçirme sayfası** ("Review Reconciled") | Xero | [U4] |
| 5 | **Otomasyonun hesap ya da kural başına açılması** (varsayılan kullanıcının seçimi) | Xero (hesap), QuickBooks (kural) | [U4][U3] |
| 6 | **Bir kez öğret, hep tanı:** ilk eşleşmede IBAN cariye yazılır, sonrakiler otomatik | Paraşüt | [M1c] |
| 7 | **Açıklamadan müşteri bulma** ve ilk tutan kuralın kazanması | Odoo Partner Mapping, mutabakat modelleri | [U6] |
| 8 | **Tutar tutmazsa iş bırakmama:** fark için tek tık yeni kayıt (Zoho), eksik ödeme toleransı (Odoo), "eksik olarak bırak" (QuickBooks) | Zoho, Odoo, QuickBooks | [U5][U6][U8] |
| 9 | **Bağlanmamış alacak görünür kalır:** aynı müşteriye yeni fatura onaylanınca mavi bant çıkar ve "Ekle" ile bağlanır | Odoo | [U10] |
| 10 | **Tek hedefli özet bandı:** ekstre bakiyesi, program bakiyesi, fark; fark 0 olunca "Bakiyeyi sabitle". Öneri "Eşleştir"e basıp onaylamakla iki dokunuşta biter | Paraşüt banka mutabakatı | [U7] |
| 11 | **Ödeme bacağı fiş satırıdır:** nakit, kart, eski altın (gramıyla), açık hesap aynı ızgarada; çıkışlar = girişler, FARK 0,00 olunca Kaydet | PusulaX satış ekranı | [PUSULA-REF] |
| 12 | **İki büyük düğme:** müşteri ekranında "Veresiye ver" ve "Ödeme al"; hatırlatma WhatsApp/SMS ile, müşterinin uygulama indirmesine gerek yok | OkCredit (arama özeti, **O**) | [U20] |
| 13 | **Hatırlatma ödeme gelince kendiliğinden durur;** aynı gün vadesi gelenler tek mesajda; müşteri ya da fatura başına kapatılabilir; asgari tutar eşiği | Chaser, Paraşüt, Xero | [U19][U17][U18] |
| 14 | **Onay penceresi yerine geri alma;** düğmede "Evet/Hayır" değil eylem adı ("Borçtan düş", "Avans olarak kaydet") | NN/g | [U16] |
| 15 | **Güven bilgisi kategorik ve eylem odaklı;** sayısal yüzde gösterilmez. Düşük güvende birkaç aday sunulur | Google PAIR | [U14] |
| 16 | **Kolay reddetme, verimli düzeltme, şüphede kapsamı daralt, nedenini açıkla** (G8, G9, G10, G11) | Microsoft HAX | [U15] |
| 17 | **Barkod okut → büyük fiyat + "stokta" rozeti + fotoğraf** | PusulaApp fiyat okuma, KuyumSmart | [PUSULA-REF][P3a] |
| 18 | **Ani piyasa sıçramasında fiyat güncellemesini durduran "devre kesici"** | Platin360 | [P20] |
| 19 | **Sabah bakılan tek sayı:** Açık Pozisyon (has borç/alacak) ana ekranda kutucuk | PusulaX | [PUSULA-REF] |

### 7.2 Kötü kalıplar (topluluk/şikâyet kaynaklı)

| # | Kalıp | Nerede | Kaynak |
|---|---|---|---|
| 1 | Birden çok adayda **sessizce en eskiyi** seçmek (ödeme olmasa bile) | QuickBooks; ekip "mevcut işleyiş" dedi | [U12] |
| 2 | Yapay zekânın **eğitilememesi**, kullanıcı düzeltmesinden öğrenmemesi | QuickBooks | [U21] |
| 3 | Yanlış kuralın **1000'den fazla** kaydı işlemesi; geri alma sayfa sayfa | QuickBooks (31.3.2026) | [U22] |
| 4 | **Kapatılamayan** otomatik güncelleme | Wave (**O**) | [U23] |
| 5 | **Her satıra ayrı OK** tıklaması (2023–2025); yanlış OK'in yeni kayıt doğurması; önceden işaretli önerinin sonraki işte seçili kalması | Xero | [U11][U24] |
| 6 | Benzer adlı iki geri alma: "Unmatch" kaydı korur, "Uncategorize" **siler** | Zoho | [U5] |
| 7 | Bağlantı kopunca **mükerrer hareket** | QuickBooks (**O**) | [U13] |
| 8 | Ödeme düştü ama **fatura eşleşmedi** (Şikâyetvar başlığı) | KolayBi' | [M3c] |
| 9 | Dağıtılmamış avans ya da alacak için **hatırlatma yok** | Xero (kullanıcı isteği) | [U9] |
| 10 | Sekmeli belge alanı, 12 px yoğun ızgara, 13 işlem düğmesi | PusulaX | [PUSULA-REF] |
| 11 | Ürün adı yerine **ürün kodu** gösteren satış satırı | PusulaX | [PUSULA-REF] |
| 12 | Pazarlama ile gerçeğin ayrışması: boş mağaza rozetleri, belge numarasız ISO rozeti, çelişen deneme süresi | Faturalog | [F1][F3][F8] |

### 7.3 Onay kuyruğu / öneri listesi tasarımlarında ortak öğeler (rakiplerden derlenen)

Rakip ürünlerde onay kuyruğu satırı şu öğelerden oluşuyor:
- Hareket satırı: tarih · gönderen · tutar · açıklama.
- **Tek** önerilen eylem; alternatifler katlanır ("Consider", QuickBooks).
- Neden etiketi.
- Satır başında tek dokunuşla onay.
- Üstte "hazır olanların hepsi" toplu onayı.
- Eşik üstü satırların toplu onaydan otomatik çıkması.
- Kuyrukta bekleme nedeninin yazılması.
- İşlenmişler sekmesinde "geri al".
- "Bunu kural yap" kısayolu.

Bu öğelerin hepsi QuickBooks, Xero ve Zoho'da birlikte bulunuyor [U1][U2][U3][U4][U5]. Türkiye'deki ürünlerde (Paraşüt hariç) adım adım yardım dokümanı bulunamadı. BizimHesap'ın eşleştirme ekranı doğrulanamadı (**D**).

---

## 8. Açık sorular (araştırmayla kapanmayanlar)

### 8.1 Müşteriye (kuyumcu)

1. Faturalog'u hangi kipte kullanıyorsunuz: para gelince fatura **kendiliğinden** mi kesiliyor, **onayınızla** mı? Bir ekran görüntüsü ya da 10 dakikalık gösterim mümkün mü? (Sitedeki sayfalar çelişiyor.)
2. Havaleyle gelen paranın kaçı yeni satış, kaçı veresiye tahsilatı, kaçı avans ya da emanet? Faturalog bunları nasıl ayırıyor? Çift fatura yaşadınız mı?
3. Gram (has/22 ayar) cinsinden veresiyeniz var mı? TL ödeme geldiğinde hangi kurla kaç gram düşüyorsunuz: o günkü satış, alış ya da anlaştığınız kur?
4. Hangi bankalarla çalışıyorsunuz? Faturalog için şubeye talimat ya da başvuru formu imzaladınız mı? Banka ücret alıyor mu?
5. Banka hareketinde gönderenin TC kimlik numarası ekranda görünüyor mu (bankaya göre)?
6. Müşterinin "otomatik kaydet" isteği yalnız ad ve IBAN ile açılan cari için de geçerli mi? Kimlik vermeyen müşterinin kaydı nasıl olmalı?
7. Faturalog yanında kullandığınız "girdi-çıktı/veresiye" programı hangisi (Pusula mı, başka mı)? Pusula ise klasik mi, PusulaX mi?
8. Borç hatırlatmasını SMS ya da WhatsApp ile kendiniz mi yapıyorsunuz? Otomatik gitsin mi, yoksa siz mi göndermek istersiniz?
9. Faturalog kontör paketiniz ve aylık fatura adediniz nedir (maliyet kıyası için)?

### 8.2 Kurucuya

1. **Banka verisi çekme yolu:** (a) lisanslı HBH ile iş birliği, (b) müşterinin kurumsal web servisi, (c) ekstre yükleme. Hukuk görüşü alınana kadar hangisi? (6493 md.28 cezai risk.)
2. Faturalog'dan ya da müşteriden demo ekranı istenmesi (Claude hesap açmaz).
3. Rakip kümesi: Milyem PLUS, KuyumHub, Terminal Gold, SarrafPro, KuyumSmart doğrudan rakip. Bunlarla kıyas yapılsın mı? (Demo talebi kurucu kararı.)
4. `mevzuat-gereksinimleri.md` M-01'in 370.000 TL'ye güncellenmesi ve kodda eşik değerinin yapılandırmadan değiştirilmesi (ayrı paket; bu araştırmada dokunulmadı).

### 8.3 Mali müşavire

1. 2026'da e-Arşiv tutara bakılmaksızın zorunluyken, kuyumcunun ÖKC fişiyle yetinebileceği sınır 12.000 TL mi, 514 ile 36.000 TL mi? 514 md.4 yürürlükte mi?
2. ÖKC muafiyeti olmayan kuyumcu eşik altı her satışta ÖKC yerine yalnız e-Arşiv kesebilir mi, yoksa ikisi birlikte mi gerekir? (Müşterinin "fiş iptal" isteği.)
3. Hurdanın rafineriye ya da toptancıya satışında KDV: 17/4-g metal hurda istisnası mı, külçe istisnası mı, vergiye tabi mi?
4. Bireyden hurda alımında e-Gider Pusulası kullanılacak mı? 234/5 banka belgesi uygulaması nasıl yürür?
5. Mükellef olmayandan hurda altın alımında stopaj gerçekten yok mu? (Özelge ikincil kaynaktan okundu.)
6. UBL-TR 1.2.1 (16.03.2026) sürümünde OZELMATRAH ve 805 kodu değişti mi?
7. Gram cinsinden veresiyenin TL tahsilatla kapanmasında kur farkı nasıl kaydedilir?
8. Havale ile gelen ve satışla eşleşmeyen paranın (avans) faturası ne zaman kesilir? (Avans faturası uygulaması.)
9. Luca/Zirve aktarım biçimi (örnek dosya).

### 8.4 Hukukçuya

1. Müşterinin kendi kurumsal web servis kullanıcısıyla, Mihenk'in onun adına veri çekmesi HBH lisansı gerektirir mi (6493 md.12/2-ğ teknik hizmet istisnası)?
2. Bankadan gelen kişisel veriyle otomatik cari açmada aydınlatma metni ve "ilk iletişim" zamanı.
3. SMS/WhatsApp ile borç hatırlatması 6563 sayılı Kanun ve İYS kapsamında ticari elektronik ileti sayılır mı? (Bu araştırmada incelenmedi.)

### 8.5 Doğrulanamayan kalemler (toplu)

**Faturalog**
- Banka bağlantı yöntemi ve aracısı; 28 bankanın gerçekten bağlı olup olmadığı.
- Kural editörü; otomatik ve onaylı akışın hangisi olduğu.
- Otomatik akışta mükellef sorgusu ve KDV.
- Deneme süresi; fiyatlara KDV dahil mi.
- Muhasebe ve pazaryeri entegrasyonlarının çalışıp çalışmadığı.
- Mobil uygulama; ISO belgeleri; kuruluş ve sicil kaydı.
- Bağımsız kullanıcı yorumu.

**Muadiller**
- BizimHesap ve KolayBi': eşleştirme ölçütü, otomatik cari ve fatura, hatırlatma kanalı, bağlantı yöntemi.
- İşbaşı: banka listesi ve tutarlılığı.
- Milyem PLUS, Dezy, Hasfiyat: fiyat ve teknik yöntem.
- Mikro, Dia, Zirve, ETA, Sentez, Uyumsoft: ürün içi eşleştirme.
- Entegratörlerin banka→fatura özelliği.
- Finrota ve Bulut Tahsilat fiyatları.

**Kuyumcu programları**
- Konsinye ve açık pozisyon raporu (Pusula dışında).
- Entegratör adları (Meadify ve Diamags dışında).
- Bağımsız şikâyet verisi: Şikâyetvar araması 410 verdi, yalnız CetaSoft'ta 1 kayıt var.
- Bulunamayan 10'dan fazla ad (§ 2.2).
- KuyumPOS özellikleri (yalnız arama özeti).

**Mevzuat**
- 514 md.4'ün 2026'da yürürlükte olduğu.
- UBL-TR 1.2.1 kodları.
- Hurda KDV istisnası.
- EFT/FAST mesaj alanları ve banka API hareket alanları.
- Web servis yolunun lisans durumu.
- Aydınlatma Tebliği ayrıntısı.
- 483'ün sonraki değişiklikleri.
- 6563/İYS kapsamı.

**Arayüz**
- BizimHesap eşleştirme ekranı.
- KhataBook düğme adları.
- FreeAgent, Wave, Holded resmi sayfaları (403 ya da açılmadı).
- QuickBooks otomatik işlemenin varsayılan değeri.
- Odoo'da geri alma.
- Paraşüt'te avans sorusu.
- Türkiye'de WhatsApp ile otomatik veresiye hatırlatan doğrulanmış uygulama.

---

## 9. Kaynakça (tüm erişimler 17.10.2026)

### Proje içi
- [PUSULA-REF] `dokumanlar/02-mimari/sadelestirme/PUSULA-REFERANS-2026-09-23.md` (pusulax.com görselleri, YouTube @pusulayazilim)
- `dokumanlar/01-gereksinimler/musteri-gorusmesi-2026-10-16.md` · `dokumanlar/02-mimari/PAKET-PLANI-MUSTERI-2026-10-16.md` · `dokumanlar/01-gereksinimler/mevzuat-gereksinimleri.md`

### Faturalog
- [F1] https://faturalog.com/ [Ü]
- [F2] https://faturalog.com/sitemap.xml · https://faturalog.com/robots.txt [Ü]
- [F3] https://faturalog.com/fiyatlandirma/ [Ü]
- [F4] https://faturalog.com/kvkk/ [Ü]
- [F5] https://faturalog.com/gizlilik-politikasi/ [Ü]
- [F6] https://faturalog.com/hesap-silme-politikasi/ [Ü]
- [F7] https://faturalog.com/hakkimizda/ [Ü]
- [F8] https://faturalog.com/sikca-sorulan-sorular/ [Ü]
- [F9] https://faturalog.com/acik-bankacilik/ [Ü]
- [F10] https://faturalog.com/otomatik-fatura-modulu/ [Ü]
- [F11] https://faturalog.com/cari-modulu/ [Ü]
- [F12] https://faturalog.com/e-fatura-yonetimi/ [Ü]
- [F13] https://faturalog.com/toplu-fatura/ [Ü]
- [F14] https://faturalog.com/urun-yonetimi/ [Ü]
- [F15] https://faturalog.com/panel/ [Ü]
- [F16] Blog yazıları [Ü]:
  - https://faturalog.com/blog/
  - [F16a] https://faturalog.com/faturalog-toplu-fatura-modulu/
  - [F16b] https://faturalog.com/bankalar-arasi-mekik-dokumaya-son/
  - https://faturalog.com/muhasebede-yapay-zeka-donemi/
- [F17] https://portal.faturalog.com/ [Ü]
- [F18] https://ebelge.gib.gov.tr/efaturaozelentegratorlerlistesi.html [B]
- [F19] https://www.tcmb.gov.tr/wps/wcm/connect/TR/TCMB+TR/Main+Menu/Temel+Faaliyetler/Odeme+Hizmetleri/Odeme+Kuruluslari [B]
- [F20] https://www.tcmb.gov.tr/wps/wcm/connect/TR/TCMB+TR/Main+Menu/Temel+Faaliyetler/Odeme+Hizmetleri/Elektronik+Para+Kuruluslari [B]
- [F21] https://www.istanbuljewelryshow.com/overview/sustainability/better-stands/ [B]
- [F22] https://www.linkedin.com/company/faturalog [B platform]
- [F23] https://play.google.com/store/search?q=faturalog&c=apps [B]
- [F24] https://www.sikayetvar.com/faturalog (410) · https://www.sikayetvar.com/arama?q=faturalog [B]
- [F25] https://faturalog.com/cerez-politikasi/ · https://faturalog.com/iletisim/ [Ü]
- Okunamayanlar: https://eksisozluk.com/?q=faturalog (403), https://www.youtube.com/results?search_query=faturalog (içerik dönmedi)

### Muadiller (banka/fatura/cari)
- [M1a] https://www.parasut.com/banka-entegrasyonu
- [M1b] https://www.parasut.com/kullanim-kilavuzu/banka-hesaplarinin-baglanmasi
- [M1c] https://www.parasut.com/kullanim-kilavuzu/is-bankasi-entegrasyonu
- [M1d] https://www.parasut.com/on-muhasebe-fiyatlari
- [M1e] https://www.parasut.com/kullanim-kilavuzu/banka-mutabakati-yapmak
- [M1f] https://www.parasut.com/e-fatura
- [M1g] https://www.parasut.com/kullanim-kilavuzu/kolaymutabakat-entegrasyonu-nedir
- [M1h] Paraşüt hatırlatma, tahsilat ve kullanıcı yorumları:
  - https://www.parasut.com/kullanim-kilavuzu/tahsilat-odeme-hatirlatmalari
  - https://www.parasut.com/kullanim-kilavuzu/musteriden-tahsilat-talep-etmek
  - https://apps.apple.com/tr/app/id976151321
  - https://www.sikayetvar.com/parasut
- [M2a] https://www.bizimhesap.com · https://bizimhesap.com/destek · https://bizimhesap.com/ayricaliklar
- [M2b] https://bizimhesap.com/fiyatlar
- [M2c] https://albaraka.com.tr/en/sme/cash-management/e-services-and-accounting-integrations/bizim-hesap-preaccounting
- [M2d] https://apps.apple.com/tr/app/id1193203589 · https://www.sikayetvar.com/bizimhesap
- [M3a] https://www.kolaybi.com/online-banka-entegrasyonu · https://www.kolaybi.com/en/online-bank-integration · https://www.kolaybi.com
- [M3b] https://www.kolaybi.com/fiyatlar · https://apps.apple.com/tr/app/id1613344055
- [M3c] https://www.sikayetvar.com/kolaybi
- [M4] Logo İşbaşı:
  - https://isbasi.com/banka-entegrasyonu
  - https://www.isbasi.com
  - https://www.isbasi.com/entegrasyonlar
  - https://apps.apple.com/tr/app/id1383770557
  - https://www.sikayetvar.com/logo-isbasi
- [M6] https://dezyapp.com/ · https://dezyapp.com/kuyumcu-e-fatura-programi/ · https://dezyapp.com/pricing/
- [M7] https://apps.apple.com/tr/app/id1629936369
- [M8] https://www.aposkal.com
- [M9] https://www.netadam.net/on-muhasebede-banka-entegrasyonu/
- [M10] https://opakyazilim.com/blog/banka-entegrasyonu-ile-otomatik-tahsilat-eslestirme
- [M11] https://hesapmix.com/ozellikler/banka-entegrasyonu
- [M12] https://fiscusai.org
- [M13] https://bilsoft.com/en/entegrasyonlar/banka
- [M14] https://faturaport.com
- [M15] https://www.cariofis.com/ozellikler/e-fatura
- [M16a] https://www.mikro.com.tr · https://www.mikro.com.tr/mikro-run/
- [M16b] https://www.mikro.com.tr/e-mutabakat/
- [M17] Logo ERP banka entegrasyonları (banka siteleri):
  - https://www.garantibbva.com.tr/tr/kobi/nakit_yonetimi/entegrasyon-cozumlerimiz.page
  - https://www.yapikredi.com.tr/kurumsal/nakit-yonetimi/e-cozumler-ve-operasyonel-hizmetler/
  - https://albaraka.com.tr/en/uyumsoft-accounting-integration.aspx
- [M18] https://www.tcmb.gov.tr/wps/wcm/connect/TR/TCMB+TR/Main+Menu/Temel+Faaliyetler/Odeme+Hizmetleri/Odeme+Kuruluslari · https://www.forbes.com.tr/ekonomi/tcmb-den-logo-odeme-hizmetleri-ne-faaliyet-izni
- [M19] https://www.tcmb.gov.tr/wps/wcm/connect/TR/TCMB+TR/Main+Menu/Temel+Faaliyetler/Odeme+Hizmetleri/Elektronik+Para+Kuruluslari
- [M20] Luca aktarımı (Yapı Kredi / İş Bankası beyanları; arama özeti, **O**)
- [M21] https://mysoft.com.tr/e-aktarim
- [M22] https://rahataktarim.com/
- [M23a] https://finrota.com/en/netekstre-nedir · https://finrota.com/en/about-us
- [M23b] https://www.finrota.com/en/blog/online-banka-entegrasyonu-nedir-api-bankaciligi · https://www.finrota.com/en/news/third-quarter-2023-open-banking-data
- [M24] https://www.buluttahsilat.com · https://www.buluttahsilat.com/tr/onlinebanka · https://www.buluttahsilat.com/tr/sss
- [M25] https://www.vomsis.com
- [M26] https://www.nebim.com.tr/en/nebim-v3-cloudcollection-integration
- [M27] https://www.alomaliye.com/2026/01/02/banka-ekstreleri-sorunsali/ [B]
- Diğer: https://www.mukellef.co · https://www.ziraatbank.com.tr/tr/ticari/z-donusum/e-donusum (EDM'nin Ziraat e-dönüşüm altyapısı; arama özeti)

### Kuyumcu programları
- [P1a] https://www.pusulanet.net
- [P1b] https://pusulax.com
- [P1c] YouTube @pusulayazilim ve videolar:
  - https://www.youtube.com/@pusulayazilim
  - https://www.youtube.com/watch?v=hqWhqmHL6zU
  - https://www.youtube.com/watch?v=7bMvmlLHW1g
  - https://www.youtube.com/watch?v=QqdizqqQozc
- [P2a] https://www.hasfiyat.com
- [P2b] https://www.hasfiyat.com/premium
- [P2c] Hasfiyat hakkında diğer kaynaklar:
  - https://hasfiyat.com/blog/en-iyi-kuyumcu-yazilimlari-ve-fiyat-ekranlari-karsilastirmasi
  - https://www.capterra.com/p/10044507/Hasfiyat/
  - https://www.yatirimx.com.tr/ekonomi/kuyumcu-yazilimi-ve-canli-altin-verisi-hasfiyat-ile-kuyumculukta-dijital-donusum/26158
- [P3a] https://kuyumsmart.com.tr/ · https://kuyumsmart.com.tr/ozellikler
- [P3b] https://kuyumsmart.com.tr/sss
- [P3c] https://kuyumsmart.com.tr/canli-altin-fiyatlandirma/kuyumcu-e-fatura-ozel-matrah
- [P4a] https://terminal.gold/ · https://terminal.gold/kuyumcu-yazilimi
- [P4b] https://terminal.gold/fiyatlandirma
- [P4c] https://kuvvem.com/sektor/kuyumcu-magaza-programi/
- [P5] https://milyemplus.com/
- [P6a] https://kuyumhub.com/
- [P6b] https://kuyumhub.com/blog/en-iyi-kuyumcu-mobil-uygulamasi.html
- [P7a] https://ariyen.net · https://ariyen.net/fiyatlar
- [P7b] https://ariyenkuyumcu.com/blog/kuyumcular-hangi-programi-kullaniyor
- [P8a] https://www.cetasoft.com.tr/kuyum/yap.php · https://apps.apple.com/us/app/id6751603719
- [P8b] https://www.sikayetvar.com/cetasoft
- [P9] https://www.mlyazilim.com · https://www.youtube.com/watch?v=_N_BDsrFAhI
- [P10] https://www.kuyumplus.com
- [P11] https://kuyumcum.net/ · https://apps.apple.com/us/app/kuyumcum/id1474947143 · https://www.youtube.com/watch?v=Tc79KWBK6nI
- [P12] https://www.kuyumerp.com/
- [P13] https://graminoerp.com.tr/
- [P14] https://bakiyem.net/tr
- [P15] https://zarifsoft.com/kuyumcu-muhasebe-programi/
- [P16] https://e-yaz.com.tr/kuyumculuk-yazilimi/
- [P17a] https://sarraf.pro
- [P17b] https://sarraf.pro/blog/otomatik-gider-pusulasi-ve-muhasebe-entegrasyonu
- [P18] https://www.meadify.com/kuyumcu-programi/
- [P19] https://dezyapp.com/kuyumcu-e-ticaret-entegrasyonu/
- [P20] https://www.platin360.com/kuyumcu-e-ticaret-modulu-anlik-kur-takibinden-otomatik-fiyatlamaya-kapsamli-rehber/
- [P21] https://dijimo.com.tr/cozumler/kuyumcu-programi.html
- [P22] https://www.kuyumsoft.com · https://apps.apple.com/us/app/id1211629132
- [P23] https://kuyumpos.com/ (sayfa okunamadı; arama özeti)
- [P24] https://kuyumcuprogrami.com/kuyumcularin-kullandigi-program.html
- [P25] https://www.tamindir.com/windows/macgold/
- [P26] https://apps.apple.com/pe/app/diamags/id6478971649
- [P-NEB] https://www.nebim.com.tr/i/assets/images/v3_brosur/Nebim_V3_BR17_02_TekSayfa.pdf
- Kontrol amaçlı açılanlar:
  - https://www.goldsoft.com.tr (kuyumcu yazılımı değil)
  - https://www.akinsoft.com.tr/medya-merkezi/request?q=mini-ada-atasay-kuyumculuk-bayii-toplantisinda-7112
  - https://play.google.com/store/search?q=kuyumcu%20program&c=apps&hl=tr
  - https://github.com/emrullahkara/DC-Kuyumcu-takip (404)
  - https://www.gercektaraf.com.tr/kuyumcuda-fatura-mi-gider-pusulasi-mi-belge-duzeni-2026 (ikincil)

### Mevzuat (birincil, aksi belirtilmedikçe)
- [L1] https://www.turmob.org.tr/arsiv/mbs/resmigazete/31727-2.pdf (535 s. VUK GT, RG tıpkıbasımı) · https://www.mevzuat.gov.tr/mevzuatmetin/1.4.213.pdf (VUK 232, 234, 253)
- [L2] https://cdn.gib.gov.tr/api/gibportal-file/file/getFile?objectKey=MEVZUAT_TEBLIGLER%2FUNIVERSAL%2F2026%2FMEVZUAT_TEBLIGLER_2026_VukTeb509_Guncel.pdf (509 güncel) · https://www.resmigazete.gov.tr/eskiler/2019/10/20191019-5.pdf (509 asıl)
- [L3] https://ismmmo.org.tr/dosya/5123/Mevzuat-Dosya/12112024-VUK-573-sayili-tebligi.pdf (573; RG 32720 tıpkıbasımı)
- [L4] https://www.resmigazete.gov.tr/eskiler/2025/12/20251231M5-32.pdf (589) · https://www.alomaliye.com/2025/12/31/vergi-usul-kanunu-genel-tebligi-sira-no-589/ (ikincil)
- [L5] https://www.resmigazete.gov.tr/eskiler/2025/12/20251231M5-31.pdf (588, 2026 tutarları)
- [L6] https://www.resmigazete.gov.tr/eskiler/2020/01/20200104-4.htm (514)
- [L7] https://www.mevzuat.gov.tr/mevzuatmetin/1.5.3065.pdf (KDVK 17/4-g, 23/e)
- [L8] https://www.mevzuat.gov.tr/MevzuatMetin/yonetmelik/9.5.19631.pdf (KDV GUT)
- [L9] https://ebelge.gib.gov.tr/dosyalar/kilavuzlar/UBL-TR_Kod_Listeleri-V_1_27.pdf
- [L10] https://www.mevzuat.gov.tr/mevzuatmetin/1.4.213.pdf (VUK 234)
- [L11] https://www.muhasebenews.com/?p=133234 (hurda altın stopaj özelgesi; ikincil)
- [L12] https://www.resmigazete.gov.tr/eskiler/2024/11/20241130-10.htm (575 / 459)
- [L13] https://www.resmigazete.gov.tr/eskiler/2026/10/20261007-9.pdf (MASAK değişikliği 07.10.2026)
- [L14] https://www.mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf (Tedbirler Yönetmeliği, güncel)
- [L15] https://www.mevzuat.gov.tr/mevzuatmetin/1.5.6698.pdf (KVKK)
- [L16] https://www.mevzuat.gov.tr/mevzuatmetin/1.5.6493.pdf (6493)
- [L17] https://www.resmigazete.gov.tr/eskiler/2021/12/20211201-1.htm (TCMB Ödeme Hizmetleri Yönetmeliği)
- [L18] https://www.resmigazete.gov.tr/eskiler/2020/03/20200315-10.htm (BDDK Bilgi Sistemleri Yönetmeliği)
- [L19] https://www.mevzuat.gov.tr/mevzuatmetin/1.5.6102.pdf (TTK 82)
- [L20] https://www.mevzuat.gov.tr/mevzuatmetin/1.5.3100.pdf (3100)
- [L21] https://ynokc.gib.gov.tr/UploadedFiles/Files/557_VUK_GT.pdf
- [L22] https://www.pwc.com.tr/tr/hizmetlerimiz/vergi/dolayli-vergi/bultenler/e-donusum-bultenleri/2026/ubl-tr-1-2-1-paketi-guncellenmistir.html (ikincil)
- [L23] https://www.erdem-erdem.av.tr/bilgi-bankasi/verbis-kayit-yukumlulugune-iliskin-istisna-kriteri-degistirildi (ikincil)
- [L24] https://ynokc.gib.gov.tr/UploadedFiles/Files/483_SIRA_NOLU_VERGI_USUL_KANUNU_GENEL_TEBLIGI_guncel_25052018.pdf
- Ek ikincil: https://www.forbes.com.tr/fintek/tcmb-den-hepsipay-ve-param-a-acik-bankacilik-izni · https://www.alomaliye.com/2026/03/17/acik-bankacilik-hizmetlerinin-yeni-ozelliklerine-iliskin-basin-duyurusu/

### Arayüz ve otomasyon kalıpları
- [U1] https://quickbooks.intuit.com/learn-support/en-global/help-article/bank-transactions/ai-suggestions-help-match-categorise-bank/L8FHOh4AD_ROW_en
- [U2] https://quickbooks.intuit.com/learn-support/en-us/help-article/account-management/use-ready-post-quickbooks-online/L9jzjf8Xe_US_en_US
- [U3] https://quickbooks.intuit.com/learn-support/en-us/banking/how-to-set-and-use-banking-rules-for-downloaded-transactions/00/262188 · https://quickbooks.intuit.com/learn-support/en-global/help-article/bank-registers/unmatch-downloaded-bank-transactions-move-another/L71oaBb86_ROW_en
- [U4] https://www.xero.com/uk/campaign/auto-bank-reconciliation/ · https://blog.xero.com/product-updates/automatic-bank-reconciliation-jax-beta/
- [U5] https://www.zoho.com/books/help/banking/match-categorize.html
- [U6] https://www.odoo.com/documentation/18.0/applications/finance/accounting/bank/reconciliation.html · https://www.odoo.com/documentation/18.0/applications/finance/accounting/bank/reconciliation_models.html
- [U7] https://www.parasut.com/kullanim-kilavuzu/banka-mutabakati-yapmak
- [U8] https://quickbooks.intuit.com/learn-support/en-us/help-article/accounting-bookkeeping/resolve-common-issues-applying-payment-towards/L2UMXN8tt_US_en_US · https://quickbooks.intuit.com/learn-support/en-au/help-article/customer-refunds-credits/create-apply-adjustment-notes-delayed-credits/L5kne9EiI_AU_en_AU
- [U9] https://evolutionplatform.poolwerx.com.au/support/solutions/articles/13000109082-xero-record-a-prepayment · https://productideas.xero.com/forums/939198-for-small-businesses/suggestions/50006298-reconciliation-overpayment-allocation-reminder
- [U10] https://www.odoo.com/documentation/saas-17.4/applications/finance/accounting/payments.html
- [U11] https://productideas.xero.com/forums/939198/suggestions/44988634
- [U12] https://quickbooks.intuit.com/community/banking-4/are-there-settings-to-improve-my-bank-rec-if-there-are-a-few-options-qb-always-seems-to-pick-the-wrong-one-the-oldest-one-even-if-it-isn-t-a-payment-23384
- [U13] https://quickbooks.intuit.com/learn-support/en-us/banking/re-duplicate-transactions-from-bank-feed/01/1165446/highlight/true
- [U14] https://pair.withgoogle.com/chapter/explainability-trust/
- [U15] https://www.microsoft.com/en-us/haxtoolkit/library/
- [U16] https://www.nngroup.com/articles/confirmation-dialog/ · https://www.nngroup.com/articles/user-control-and-freedom/
- [U17] https://www.parasut.com/kullanim-kilavuzu/musterinize-odeme-hatirlatma
- [U18] https://blog.accountingprose.com/never-miss-a-payment-with-xeros-automatic-invoice-reminders · https://quickbooks.intuit.com/learn-support/en-us/help-article/invoicing/send-invoice-reminders-automatically-manually/L84cQjpxo_US_en_US
- [U19] https://www.chaserhq.com/credit-control-accounts-receivables-software
- [U20] https://okcredit.in/llms.txt · https://okcredit.in/blog/what-is-okcredit-and-how-to-use-okcredit (arama özeti)
- [U21] https://quickbooks.intuit.com/community/banking-4/can-we-adjust-or-train-the-bank-feed-matching-ai-or-programming-so-it-better-matches-24473
- [U22] https://quickbooks.intuit.com/community/transactions-28/is-there-a-way-to-go-back-from-a-1000-transactions-somehow-ending-up-in-the-chart-of-accounts-via-a-rule-to-them-showing-up-again-in-the-bank-transactions-120407
- [U23] https://support.waveapps.com/hc/en-us/articles/4988928647188 (403; arama özeti)
- [U24] https://productideas.xero.com/forums/967136-banking-chart-of-accounts/suggestions/51662221-clear-suggested-matches-after-creating-a-prepaymen · https://productideas.xero.com/forums/967136-banking-chart-of-accounts/suggestions/51146524-restrict-reconciliation-from-initial-screen
- Diğer kalıp kaynakları:
  - https://support.freeagent.com/hc/en-gb/articles/115001586004
  - https://support.freeagent.com/hc/en-gb/articles/360014732520
  - https://www.holded.com/es/programa-tesoreria/reglas-conciliacion
  - https://qonto.com/fr/accountants/bank-reconciliation
  - https://communityhub.sage.com/cfs-file/__key/communityserver-discussions-components-files/524/Sage-50-Accounts-_2D00_-Bank-Feeds-and-Rules-_2D00_-202302.pdf
  - https://help.1stformations.co.uk/accounting-service/using-xeros-find-and-match-function
  - https://evolutionplatform.poolwerx.com.au/support/solutions/articles/13000109471-xero-record-a-part-payment-on-an-invoice
  - https://fhpaccounting.co.uk/xero-bank-reconciliation-like-a-pro-master-rules-cash-coding-and-error-prevention-techniques/
  - https://apps.apple.com/us/app/id6463430967
  - https://www.tamindir.com/indir/yaz-tahtaya-al-haftaya-veresiye/amp/
  - https://www.similarweb.com/app/google/com.vaibhavkalpe.android.khatabook/kuwait/

---

*Bu belge araştırmadır; mimari ve paket kararları bir sonraki adımda verilecek. Üretici beyanları "var" olarak işaretlendi ama hiçbiri çalışırken denenmedi.*
