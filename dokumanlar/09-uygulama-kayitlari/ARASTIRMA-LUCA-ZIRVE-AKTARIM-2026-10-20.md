# Araştırma — Luca ve Zirve'ye fatura / muhasebe fişi aktarımı (2026-10-20)

> Yöntem: yalnız herkese açık sayfalar (luca.com.tr PDF'leri, lucayazilim.freshdesk.com destek makaleleri ve ekran görüntüleri, blog.zirveyazilim.net ve oradan indirilen resmi örnek Excel dosyaları, Paraşüt/Atlas Aktarım sayfaları, üçüncü taraf bloglar). Hesap açılmadı, form doldurulmadı, giriş yapılmadı. Doğrulanamayan her şey **TEYİTSİZ** işaretli. İndirilen ham dosyalar: `scratchpad/indirilen/` (FATURA-EXCEL-ORNEK.xls, Banka-Ornek-Excel-Dosyasi.xlsx, Luca ekran görüntüleri).

## ÖZET (5 satır)
1. **Luca — muhasebe fişi (Excel Veri Aktarımı):** resmi, herkese açık, kolonları ekran görüntüsünden doğrulandı: `Fiş No; Fiş Tarihi; Hesap Kodu; Evrak No; Evrak Tarihi; Açıklama; Borç; Alacak; Miktar; Döviz; Kur` — ayraç `;` (Türkçe), tarih `GG.AA.YYYY`, tutar `1.234,56`; hesap kodları Luca'da önceden açık olmalı; tek seferde en çok 50 fiş, mahsupta 400 satır. **Güvenle yazılabilir: EVET (dar riskle).**
2. **Luca — fatura listesi (Excel Fatura Aktarımı):** CSV `;` ile, sabit şablon yok; mali müşavir Luca içinde kendi "kural tablosu"nu tanımlar (TARİH/FT.NO/AÇIKLAMA/TOPLAM/MATRAH8/KDV8/MATRAH18/KDV18 örneği). **KISMEN.**
3. **Luca — API / e-Fatura XML:** Luca MMP'ye herkese açık API yok; Luca Net web servisi var ama belge TESMER başvurusu + gizlilik sözleşmesiyle verilir. UBL-TR XML'i doğrudan MMP'ye yükleme ekranı resmi kaynakta yok; e-belgeler TÜRMOB/İşNet entegratör ya da Nettecap/BelgeApp üzerinden "Akıllı Entegrasyon Noktası"na akar. **HAYIR (doğrudan); e-Fatura zaten entegratörden Luca'ya gidiyor.**
4. **Zirve — fatura Excel (Genel Muhasebe > Fatura Excel):** resmi örnek dosya indirildi ve okundu: `Tarih | Türü | Cari Adı/Açıklama | Evrak Numarası | Vergi/T.C Kimlik No | matrah%18 | kdv%18 | matrah %1 | KDV%1 | Para Birimi | Döviz Tutarı | Stok Kodu | Miktar` (.xls, kolon sırası serbest — kullanıcı F5 tanım tablosunda eşler). **EVET.** Zirve muhasebe fişi (borç/alacak satırlı) Excel aktarımı resmi kaynakta yok; XML fatura aktarımı = **Express Aktarım, UBL-TR e-Fatura/e-Arşiv XML veya ZIP** (sürükle-bırak). **KISMEN** (şema belgelenmemiş ama UBL-TR olduğu birden çok kaynakta).
5. **Ortak dil:** mali müşavirler fiilen (a) e-Fatura/e-Arşiv **UBL-TR XML'i entegratörden indirip** Express Aktarım / AEN ile muhasebeleştiriyor, (b) Paraşüt'ün ücretsiz **Atlas Aktarım**ı Luca'ya doğrudan, Zirve/Mikro'ya Excel-XML çıktısıyla aktarıyor. GİB e-Defter (XBRL-GL) bir *çıktı* standardıdır, program-arası fiş girişi için kullanılmaz; "muavin Excel" diye ortak bir standart yok. **Mihenk için en sağlam yol: Luca fiş CSV + Zirve fatura Excel + (zaten var olan) e-Fatura UBL-TR XML indirme.**

---

## 1. LUCA (TÜRMOB / TESMER)

### 1a. Muhasebe fişi — "Excel Veri Aktarımı" (resmi, herkese açık)
Kaynak: Luca destek makalesi *01.02.09 Excel Veri Aktarımı* — https://lucayazilim.freshdesk.com/support/solutions/articles/67000267580-01-02-09-excel-veri-aktar-m- (ekran görüntüleri: S3 ekleri 67001112516–67001112519; yerelde `scratchpad/indirilen/luca-img/veri-2..5.png`).

**Menü yolu:** Muhasebe > Fiş İşlemleri > Excel Veri Aktarımı (ekran başlığı "FİŞ AKTARIMI"). Logo→Luca makalesi de aynı menüyü "Fiş CSV dosyalarını … Excel Veri Aktarımı menüsünden yükleyin" diye anıyor — https://lucayazilim.freshdesk.com/support/solutions/articles/67000689613-logo-program-ndan-veri-aktar-m-i%C5%9Flemi

**Ekrandaki alanlar (ekran görüntüsü veri-2.png):** `Ayraç: "; (Türkçe)"` seçicisi · `Şablon İndir` · `Dosya Seç` · doğrulama kodu (captcha) · `Yükle`. Üst liste: Fiş No / Fiş Tarihi / Durum / Borç / Alacak / Bakiye. Alt liste: Hesap Kodu / Evrak No / Evrak Tarihi / Açıklama / Borç(TL) / Alacak(TL) / Miktar. Uyarı metni: "Muhasebeleşmeyen kayıtlarınız 24 saat içinde silinecektir".

**Şablon kolonları (ekran görüntüsü veri-3.png, resmi şablonun Excel görünümü, A→K):**

| A | B | C | D | E | F | G | H | I | J | K |
|---|---|---|---|---|---|---|---|---|---|---|
| Fiş No | Fiş Tarihi | Hesap Kodu | Evrak No | Evrak Tarihi | Açıklama | Borç | Alacak | Miktar | Döviz | Kur |

Örnek satırlar (resmi görüntüden birebir):
```
Fiş No;Fiş Tarihi;Hesap Kodu;Evrak No;Evrak Tarihi;Açıklama;Borç;Alacak;Miktar;Döviz;Kur
1;01.01.2020;100.001;;01.01.2020;AÇILIŞ FİŞİ;0;6.101,00;0;;
1;01.01.2020;120.004;;01.01.2020;AÇILIŞ FİŞİ;4.337,29;0;0;;
1;01.01.2020;391.018;;01.01.2020;AÇILIŞ FİŞİ;0;646,37;0;;
```
Mihenk satış faturası için (örnek, kendi üretimimiz — kolon kuralı yukarıdaki resmi şablon):
```
Fiş No;Fiş Tarihi;Hesap Kodu;Evrak No;Evrak Tarihi;Açıklama;Borç;Alacak;Miktar;Döviz;Kur
1;15.10.2026;120.001;MHK2026000000012;15.10.2026;Satış faturası MHK2026000000012;12.000,00;0;0;;
1;15.10.2026;600.001;MHK2026000000012;15.10.2026;Satış faturası MHK2026000000012;0;10.000,00;0;;
1;15.10.2026;391.020;MHK2026000000012;15.10.2026;Hesaplanan KDV %20;0;2.000,00;0;;
```

**Kurallar (makale metninden, birebir):**
- "Muhasebe Fişinin aktarılması için zorunlu olarak Fiş No, Fiş Tarihi, Hesap Kodu, Borç ve Alacak bilgileri doldurulması gerekmektedir."
- "Fiş No ve Fiş Tarihi alanlarına yazılan numara ve tarihe göre fişi ayrıştırmaktadır. … Fiş tarihinde ya da numarasında farklılık olursa ayrı muhasebe fişine kayıt etmektedir."
- "aynı anda maksimum 50 adet fiş yüklenebilir"; mahsup fişinde en çok 400 satır; açılış/kapanış fişinde satır sınırı yok.
- "fişin detayında ki hesap kodlarının aynısı sistemde açılmış olması gerekmektedir. Hesap Kodu sistemde yok ise … 'hesap kodu bulunamadı' uyarısı" (F10 ile anında açılabilir).
- Fiş Kes penceresi: Fiş Açıklaması, Fiş Tipi (Mahsup / Açılış Fişi / Kapanış Fişi), Fiş Kodu, Belge Türü.
- **KDV satırı kuralı:** ayrı satır olarak 391/191 hesabına yazılır (fiş ekranındaki "Fiş Detay" penceresi hesap bazlı; KDV için özel alan yok). Luca'nın kendi taslak fişlerinde fatura başına 1 fiş ya da dönem toplu fiş yapılabiliyor (Korkmaz Digital: "e-defter standardında 1 fiş 1 fatura" — https://korkmazdigital.com/en-kolay-yontemiyle-5-dakikada-luca-veri-aktarimi-yapin/). **Öneri: fatura başına 1 fiş (Fiş No = sıra, Evrak No = fatura no).**

**Biçim:**
- Ayraç: **`;`** (ekranda "; (Türkçe)" seçeneği; Logo→Luca makalesi de çıktı dosyasını "CSV" diye anar). Uzantı: **.csv** (Logo→Luca makalesi); Şablon İndir'in .xls mi .csv mi verdiği **TEYİTSİZ** — ayraç seçicisi olması CSV'yi gösterir.
- Tarih: **GG.AA.YYYY** (01.01.2020) — görüntüden; ekran gösteriminde 01/01/2020.
- Tutar: **Türkçe** — binlik `.`, ondalık `,` (6.101,00; 250,5). Boş değer `0`. Miktar 0.
- Kodlama: **TEYİTSİZ** (belirtilmemiş; Türkçe karakterli "AÇILIŞ FİŞİ" örneği var; Luca web/Java tabanlı → UTF-8 ve Windows-1254 ikisi de olası; her ikisiyle test edilmeli).
- Hesap kodu biçimi: Tek Düzen, alt hesap noktalı (`100.001`, `191.108.18`); Luca'da önce tanımlı olmalı.
- Doğrulama kodu (captcha) zorunlu → **otomasyonla yüklenemez; mali müşavir elle yükler.**

Destekleyen broşür (luca.com.tr PDF): "Excelden Yevmiye Fişlerinin Aktarılması", "Excelden Ticari Yazılımlarda Kaydedilen Fatura Tutar, KDV, Matrah Tutarlarının Aktarılması", "Fişler toplu olarak CSV formatında rapor olarak alınıp saklanabilir" — https://luca.com.tr/Upload///oLM61jCc/e1fN/WSYSXoZgK85bJoG.pdf ; ayrıca https://www.turmobeimza.com.tr/upload/docs/genel_brosur_luca.pdf

Üçüncü taraf "Luca formatı" (Fisle blog — resmi değil, kolonları farklı: Tarih/Hesap Kodu/Hesap Adı/Borç/Alacak/Açıklama; örnek 15.03.2026 · 258.40) — https://www.fisle.co/blog/luca-formati-nedir — **resmi şablonla uyuşmuyor, kullanılmamalı.**
Yeşil Mavi Yazılım "LucaExcelFis(CSV).pdf" — Cloudflare engeli, okunamadı → **TEYİTSİZ**: https://destek.yesilmaviyazilim.com.tr/bilgilendirme/LucaExcelFis(CSV).pdf

### 1b. Fatura listesi — "Excel Fatura Aktarımı" (resmi)
Kaynak: *01.02.11 Excel Fatura Aktarımı* — https://lucayazilim.freshdesk.com/support/solutions/articles/67000271379-01-02-11-excel-fatura-aktar-m- (görüntüler `fatura-2..5.png`).
- Ekran: "Fatura Aktarım İşlemleri" — Aktarım Şablonu / Tanım* / **Dosya (CSV)*** / Ayraç **"; (Noktalı Virgül, Türkçe)"**.
- Dosya: "Excel'den CSV (Virgülle Ayrılmış) olarak kaydedilir"; en çok **400 satır, 15 sütun**, sütun başına 15 kural, 20 şablon.
- Sabit kolon adı yok: mali müşavir her sütun başlığına tıklayıp türünü seçer (Diğer / Açıklama / Evrak No / Evrak Tarihi / Tutar). Örnek şablon "EDAK Matrahın İçinde 8-18 KDV": `TARIH | FT.NO | TUR | ACIKLAMA | BEDEL | KDV | TOPLAM | MATRAH8 | KDV8 | MATRAH18 | KDV18` (01.01.2019 · 67 · 150 · 22 · 172).
- "TOPLAM, MATRAH8, KDV8, MATRAH18 ve KDV18 sütunlarının türü mutlaka Tutar olmalıdır." Formüllerde ondalık **nokta** (0.18).
- Hesap belirleme: Sabit / Sezgisel / Hesap kodunu sütundan al / Diğer sütunda belirlenen / **VKN-TCKN ile** (cari alt hesapta kayıtlı VKN eşleşirse otomatik). Ba/Bs işaretleri, belge türü FT-Fatura, stok hareketi seçenekleri var.
- Hazır örnek şablon listesi: ÜLKER, SELÇUK ECZA, HEDEF ECZA, EDAK ECZA, DİLEK ECZA, AKMAN, **Luca_Gider_Fatura_Listesi, Luca_Gelir_Fatura_Listesi**, EDAK Matrahın İçinde 8-18 KDV, SELÇUK ECZA KDVDEN MATRAHA, BURSA ECZA DEPOSU (hepsi ayraç `;`). "Luca_Gelir_Fatura_Listesi" şablonunun kolonları görüntüde yok → **TEYİTSİZ**.

Mihenk için önerilen gelir listesi CSV (kendi kolon adlarımız; mali müşavir bir kez kural tanımlar):
```
TARIH;FT.NO;VKN;CARI;ACIKLAMA;MATRAH0;MATRAH1;KDV1;MATRAH10;KDV10;MATRAH20;KDV20;TOPLAM
15.10.2026;MHK2026000000012;1234567890;AYŞE KUYUMCULUK LTD;22 ayar bilezik;0;0;0;0;0;10.000,00;2.000,00;12.000,00
```
(Tutar yazımı Türkçe `1.234,56`; KDV oranları 2026 mevzuatına göre 0/1/10/20; kuyumda kül/has altın istisnaları için MATRAH0 kolonu.)

### 1c. e-Fatura / e-Arşiv XML (UBL-TR) Luca'ya yükleme
- Resmi Luca MMP makalelerinde **"XML dosyası yükle" ekranı yok**; gelen/giden e-belgeler **Akıllı Entegrasyon Noktası (AEN)** altında entegratörden çekilir: Muhasebe > Akıllı Entegrasyon Noktası > İşnet Alış/Satış Faturaları Entegrasyon ("İşnet'ten Getir"), TÜRMOB Entegratör, Nettecap gelir-gider ("Nettecap'ten Getir"). XML yalnız **indirme** yönünde ("Seçilenleri İndir: HTML/JPG/XML") — https://lucayazilim.freshdesk.com/support/solutions/articles/67000726662-i%CC%87%C5%9Fnet-al-%C5%9F-sat-%C5%9F-faturalar-entegrasyon-ve-muhasebele%C5%9Ftirme-i%CC%87%C5%9Flemleri ; https://lucayazilim.freshdesk.com/support/solutions/articles/67000724022-aen-ak-ll-entegrasyon-noktas-muhasebele%C5%9Ftirme-i%CC%87%C5%9Flemleri-ek-%C3%B6zellikler
- Broşür: "İŞNET NetteCap ve BelgeAPP aracılığı ile … XML formattaki toplu faturalar … muhasebeleştirilebilir" (luca.com.tr PDF yukarıda) → XML toplu yükleme **Nettecap/BelgeApp (İşNet) üzerinden**, Luca'ya doğrudan değil.
- TÜRMOB e-Belge portalı: "Eski entegratörün XML faturaları, en fazla 300 adetlik zip dosyalarıyla portala yüklenebilir" (arşiv amaçlı) — https://lucayazilim.freshdesk.com/support/solutions/articles/67000549601-e-belge-sso
- Luca Net (KOBİ ticari) tarafında: Gelen e-Fatura → "LucaNet'e Yükle" (entegratör listesinden; dosyadan değil) — https://lucayazilim.freshdesk.com/support/solutions/articles/67000715093-lucanette-gelen-e-fatura
- Üçüncü taraf iddiası (PDFXMLPro, satıcı): "harici XML dosyaları e-Fatura aktarım ekranından yüklenir" — https://pdfxmlpro.com/luca-e-fatura-xml-aktarma → **TEYİTSİZ** (resmi makaleyle çelişiyor).
- Pratik sonuç: **Mihenk'in kestiği e-Fatura/e-Arşiv zaten entegratörde (Nilvera vb.)**; mali müşavir Luca AEN'den çeker. Mihenk tarafında ek iş: yok. Entegratör→Luca otomatik köprüsü: Faturaport "Luca entegrasyonu" (yıllık 1.000 TL+KDV, Luca'ya ödenir) — https://faturaport.com/blog/e-donusum/faturaport-a-luca-entegrasyonu-eklendi ; NES/Nettecap "Luca Aktarım Ayarları" — https://nes.com.tr/luca-fatura-aktarimi

### 1d. Luca API / web servis
- Luca MMP: herkese açık API dokümanı **yok**. Broşür "Web Servis API Aktarımı" ve "LUCA NET WEB SERVİS AKTARIMLARI: Cari, Stok, Fatura, İrsaliye ve Muhasebe Fişi Aktarımları" diyor (Luca Net KOBİ paketi) — https://www.luca.com.tr/Upload///g8gcvmgB/iEBJ/h0hYcgDT8xmwpIN.pdf
- Koşul: "Entegrasyon sağlamak isteyen programlar, web servis hizmeti başvurusunda bulunmalıdır … gizlilik sözleşmesi ile birlikte Web servis hizmeti ücretsiz" — TESMER portalı üzerinden online sözleşme — https://lucayazilim.freshdesk.com/support/solutions/articles/67000549601-e-belge-sso
- "TÜRMOB servis anahtarı (API key)" yalnız GİB e-beyanname/DBS gönderimi, Ba-Bs, e-Arşiv sorgulama içindir; dışarıdan Luca'ya veri yazmaz — https://lucayazilim.freshdesk.com/support/solutions/articles/67000686858-t%C3%BCrmob-servi%CC%87s-anahtari-alma-api%CC%87-key-sorgulama-i%CC%87%C5%9Flemleri%CC%87 ; https://www.karadumanmalimusavirlik.com/lucada-api-ile-defter-beyana-gonderim-yapmak/
- Üçüncü taraflar (Rapitek "REST+OAuth2", BankaEkstreFiş "Luca portalına oturum açıp fişleri portal üzerinden oluşturur") → Luca'ya **ekran/oturum otomasyonu veya özel sözleşmeyle** yazıyorlar; belge herkese açık değil → **TEYİTSİZ**. https://rapitek.com/luca-entegrasyonu/ ; https://www.bankacevirici.com/egitim/banka-islemleri-ekstreden-muhasebeye
- Luca'ya toplu geçiş (Logo/Mikro'dan): **Luca Asistan** masaüstü uygulaması Logo XML'ini hesap planı CSV + fiş CSV'ye çevirir, sonra Excel Veri Aktarımı ile yüklenir (yukarıdaki Logo makalesi). Büyük göçler "Luca Aktarım ekibi" (aktarim@luca.com.tr) tarafından, RAR ile Luca Dosya Transfer uygulaması üzerinden — https://lucayazilim.freshdesk.com/support/solutions/articles/67000697852-luca-dosya-transfer-uygulamas-
- Hesap planı: Kişisel > Yardımcı Programlar > Hesap Planı Excel Aktarma şablonu → "XML Oluştur" makrosu → Muhasebe > Hesap Planı > Hesap Planı XML Aktar; cari bilgileri (adres, VKN/TCKN) için ayrı CSV — https://lucayazilim.freshdesk.com/support/solutions/articles/67000267449-01-01-04-hesap-plan-xml-csv-excel-aktar (XML şeması makalede yok → TEYİTSİZ).

GitHub: "luca" aramaları yalnız ilgisiz projeler (brandon-rhodes/luca YAML muhasebe, Luca AI) döndürdü; Türk Luca için açık kaynak aktarım deposu **bulunamadı**.

---

## 2. ZİRVE (Zirve Yazılım)

### 2a. Fatura Excel aktarımı — Genel Muhasebe > "Fatura Excel" (resmi, örnek dosya indirildi)
Kaynak: https://blog.zirveyazilim.net/zirve-masaustu-fatura-excel-aktarim ; örnek dosya https://blog.zirveyazilim.net/wp-content/uploads/2023/02/FATURA-EXCEL-ORNEK.xls (xlrd ile okundu; Sayfa1 51 satır × 13 sütun, Code page 1254, .xls BIFF).

**Sayfa1 başlıkları (birebir):**
`Tarih | Türü | Cari Adı/Açıklama | Evrak Numarası | Vergi/T.C Kimlik No | matrah%18 | kdv%18 | matrah %1 | KDV%1 | Para Birimi | Döviz Tutarı | Stok Kodu | Miktar`

Örnek satırlar (dosyadan birebir):
```
Tarih       | Türü           | Cari Adı/Açıklama | Evrak Numarası   | Vergi/T.C Kimlik No | matrah%18 | kdv%18 | matrah %1 | KDV%1 | Para Birimi | Döviz Tutarı | Stok Kodu | Miktar
08.02.2023  | Satış Faturası | Zirve Akademi     | GİB2023000000001 | 11111111111         | 533,60    | 96,05  | 100       | 1     |             |              |           |
16.02.2023  | Satış Faturası | Zirve Akademi     | GİB2023000000002 | 11111111111         | 524,83    | 94,47  | 100       | 1     |             |              |           |
```
(Dikkat: resmi örnek dosya VKN alanına 11111111111 yazmış — bizim iş kuralı kataloğumuz bunu geçersiz sayar; Mihenk gerçek VKN/TCKN yazar.) Sayfa2 aynı verinin 9 sütunlu kısa hâli: `Tarih | Türü | Cari adı | evrak no | vkn | matrah%18 | kdv%18 | matrah %1 | KDV%1`.

**Biçim:**
- Uzantı: **.xls** (resmi örnek). Blog: sayfa adı (ör. Sayfa1) tanım tablosuna yazılır. .xlsx kabulü belirtilmemiş → **TEYİTSİZ** (banka örneği .xlsx olduğundan büyük olasılıkla kabul).
- Tarih: gerçek Excel tarih hücresi (`dd/mm/yyyy;@` biçimli). Blog uyarısı: "Kayıtlar gelmiyorsa tarih alanı '/' ile ayrılmış olabilir; '/' → '.' ile değiştirin" ⇒ metin tarihte **GG.AA.YYYY** beklenir.
- Tutar: sayı hücresi (`#,##0.00`); metin verilecekse Türkçe `533,60`. Banka blogu: "1000 ayırıcısı nokta kullan" ayarı, aksi hâlde 8.500 → 85 okunabilir.
- Kolon sırası serbest: kullanıcı **F5 tanım tablosunda** "sıra no" ile eşler (Evrak tarihi sıra no, evrak no, açıklama/cari, VKN, matrah sütunu + bağlı KDV sütunu + hesap kodu). Örnek: matrah → 600.18 alacak, KDV → 391.18; kapatılacak hesap 120 ana + alt hesabı otomatik aç veya sabit kod. "Excel'de hesap kodu veya KDV oranı sütunu varsa kodları tabloya yazmayın, ilgili sıra no'yu doldurun."
- Zorunlu: Tarih, Cari Adı/Açıklama, VKN/TCKN, Matrah, KDV; Evrak no isteğe bağlı; döviz/stok için ek kolonlar.
- Fişe aktarım: Alt+S dosya seç → F3 fişe aktar → F2 kaydet. Fiş kesme biçimi (günlük/aylık tek fiş) ekranda seçilir.
- Kodlama: ikili Excel (BIFF, cp1254) — CSV değil; UTF-8 sorunu yok.

Mihenk satış için önerilen satır (aynı başlıklar, 2026 KDV oranları için başlıkları `matrah%20 | kdv%20 | matrah%10 | KDV%10 | matrah%0` yapmak serbest — Zirve başlığa değil sıra no'ya bakar):
```
15.10.2026 | Satış Faturası | AYŞE KUYUMCULUK LTD | MHK2026000000012 | 1234567890 | 10.000,00 | 2.000,00 | 0 | 0 | | | BLZ22-001 | 1
```

### 2b. Banka Excel (ilgili, aynı mantık) — https://blog.zirveyazilim.net/zirve-masaustunde-banka-excel-aktarim ; örnek https://blog.zirveyazilim.net/wp-content/uploads/2023/03/Banka-Ornek-Excel-Dosyasi.xlsx
Başlıklar: `Tarih | Fiş No | İşlem Açıklaması | Borç | Alacak | (boş) | İşlem Tutarı` (tek sütunda alacak eksi işaretli: -8500). Kolon numaraları tanım tablosuna girilir.

### 2c. Muhasebe fişi (borç/alacak satırlı) Excel aktarımı
Resmi blog/kılavuzda ayrı bir "fiş Excel" ekranı **yok**; Genel Muhasebe kılavuzunda yalnız Hızlı Fiş (F12), Seri Fiş (Ctrl+F, şablona göre tek satır giriş), Üstat Fiş, Banka Excel, Fatura Excel var — https://blog.zirveyazilim.net/zirve-genel-muhasebe-kullanim-kilavuzu . Ticari modülde Servis > Veri Aktarımı > Diğer Programlar > "Excel Dosyalarından" ile **stok kartı ve açılış fişi** aktarılır (kolon numarası eşlemeli, 1000 satır üstü parçalı) — https://blog.zirveyazilim.net/ticari-modulde-excel-ile-stok-kart-ve-acilis-fisi-aktarimi-nasil-yapilir . Program-arası göç için "Kayıt Aktar" (61 program, SQL/.bak/Excel/metin) — D'Cloud (üçüncü taraf) https://dcloudsoftware.com/tr/cozumler/zirve-entegrasyon → **TEYİTSİZ**. Sonuç: Zirve'ye yevmiye fişi değil, **fatura listesi** ver; fiş Zirve'de üretilir.

### 2d. XML fatura aktarımı — Express Aktarım
- Zirve Masaüstü > Express Aktarım > Yeni Stoksuz Express Aktarım > **"XML'den Fatura Aktar"**: tek ZIP, bir/birden çok XML, sürükle-bırak — https://blog.zirveyazilim.net/ekspres-aktarim-ile-xmlden-fatura-aktarimi-nasil-yapilir
- Ürün sayfası: "XML dosyaları tek tek veya toplu … sıkıştırılmış dosya desteği"; kaynaklar Paraşüt, Bizmu, Bizim Hesap, Zirve Portal, e-Portal, GİB e-Belge (e-Arşiv/e-SMM), Zirve Drive; "hangi entegratörü kullandığınızın önemi yok" — https://www.zirveyazilim.net/express-fatura-aktarim/
- Modül kılavuzu: Ayarlar > Express Aktarım Ayarları (Bizim Hesap API key, Paraşüt kullanıcı/şifre, GİB e-Belge şifre) · Muhasebeleştirme Ayarları (alış/satış matrah-vergi-tevkifat-iade hesapları, "Akıllı Muhasebe Ayarları": fatura türü/açıklama kelimesine göre otomatik hesap) · Faturaları İndir → Muhasebeleştir → "Fişe Git"; "Diğer İşlemler: XML görüntüleme" — https://blog.zirveyazilim.net/yenilenen-express-aktarim-stoksuz-modulu-nasil-kullanilir
- XML şeması resmi sayfada adlandırılmamış; kaynakların tamamı e-Fatura/e-Arşiv belgesi olduğundan ve Paraşüt'ün dışa verdiği de "e-Fatura ve e-Arşiv XML" olduğundan **UBL-TR 1.2** olduğu kuvvetle olası (PDFXMLPro satıcı iddiası "Zirve … UBL-TR uyumlu" https://pdfxmlpro.com/ ) → **KISMEN TEYİTLİ**.
- Mihenk'in Nilvera'dan indirdiği/ürettiği UBL-TR XML'ler (giden e-Arşiv/e-Fatura) ZIP'lenip mali müşavire verilirse Zirve bunu doğrudan muhasebeleştirir.

### 2e. Zirve API / e-fatura entegrasyonu
- Herkese açık, üretici yayınlı API **bulunamadı** (zirveyazilim.net, blog, GitHub). D'Cloud: "Zirve'nin herkese açık API'si yok; Nova 2.0'daki API Key GİB e-Beyanname anahtarıdır" (üçüncü taraf) → **TEYİTSİZ ama tutarlı**.
- Piyasa köprüleri: Facit (entegratör portalından çekip Zirve'ye fiş) https://facit.app/zirve/ ; Korkmaz Digital "Online Veri Aktarımı" (Zirve Dönüşüm, EDM vb. entegratörlerden) https://korkmazdigital.com/zirve-donusum-e-faturalarinizi-zirve-veri-aktarimi-yapin/ ; Faturaport Zirve entegrasyonu (yıllık 1.000 TL+KDV) https://faturaport.com/blog/e-donusum/faturaport-a-muhasebe-programlari-entegrasyonu-eklendi
- Zirve'nin kendi e-Dönüşüm (entegratör) ürünü var; mükellef Zirve Dönüşüm kullanıyorsa Express Aktarım "Zirve Portal" kaynağı ile çekiyor — Mihenk için geçerli değil (entegratör Nilvera).
- İşletme defteri (2. sınıf) için: Express Aktarım > e-Arşiv Fatura (İnteraktif) Aktar / İnternet V.D. e-Arşiv Fatura Aktar (GİB'den çeker) — https://halilibrahimyurdunmali.wordpress.com/2023/12/23/zirve-ile-express-aktarim-isletme-defteri/

GitHub: "zirve" muhasebe/XML/Express Aktarım araması → açık kaynak depo **yok**.

---

## 3. Mali müşavirlerin fiilen kullandığı ortak yol
1. **e-Belge XML (UBL-TR) → muhasebe programı.** Her iki programın da "fatura indir → hesap kodu şablonu → muhasebeleştir" akışı var: Luca AEN (TÜRMOB/İşNet entegratör, Nettecap), Zirve Express Aktarım (XML/ZIP + portallar). Bu yüzden ön muhasebe programları Luca/Zirve'ye *fiş* değil, **fatura** veriyor.
2. **Paraşüt → Luca/Zirve/Mikro:** Paraşüt'ün ücretsiz **Atlas Aktarım** masaüstü programı: Luca'ya "tek tuşla", aylık/parçalı, bilanço + işletme defteri, banka hareketi (bilanço); Zirve Müşavir / Zirve Nova / Mikro Müşavir'e **Excel veya XML çıktısı** ve Express Aktarım modülüyle. Dosya biçimi/kolonları sayfada yok → **TEYİTSİZ** — https://atlasaktarim.com/ ; https://www.parasut.com/mali-musavir-kanali . Paraşüt'ün kendi "Veri Aktarımı" ise e-Fatura/e-Arşiv **XML'lerinin ZIP'i** (yalnız .zip, ≤1 GB) — https://www.parasut.com/kullanim-kilavuzu/kolay-veri-aktarimi-nasil-yapilir-kullanim-kilavuzu
3. **Logo → Luca:** Logo "Veri aktarım dışarı" XML'i → Luca Asistan "LOGO XML" → hesap planı CSV + fiş CSV → Luca Excel Veri Aktarımı (resmi makale, § 1d). Logo'nun kendi girdi aracı ExcelTrans'tır (Logo'ya giriş; Luca ile ilgisi yok) — https://docs.logo.com.tr/download/attachments/50668190/ExcelTrans%20Tan%C4%B1t%C4%B1m%20Dok%C3%BCman%C4%B1.pdf
4. **Mikro:** üçüncü taraf araçlar "mikrov14-15fislistesi.xml" ile fiş aktarıyor (faturacevirici.com; sayfa 500 döndürdü) → **TEYİTSİZ**.
5. **GİB e-Defter (XBRL-GL 2.1):** yevmiye/kebir *çıktı* paketi; muhasebe programları üretir, GİB'e gönderir. Program-arası fiş *girişi* için hiçbir kaynak bunu kullanmıyor; Luca/Zirve'de "e-Defter XML yükle" ekranı yok. TÜRMOB "Elektronik Muhasebe Fişi Kılavuzu" fişlerin XML/PDF/TXT/CSV/JSON saklanmasını düzenler, kolon standardı koymaz — https://www.turmob.org.tr/arsiv/mbs/resmigazete/-Elektronik_Muhasebe_Fisi_Kilavuzu_30042020.pdf . "Muavin Excel" ortak standardı **yok**; muavin her programda farklı rapor çıktısıdır.
6. **Defter-Beyan (işletme defteri mükellefi):** GİB'in "Perakende Satış Fişlerini Toplu Ekleme" ve "Toplu Gider" Excel şablonları var (2. sınıf kuyumcu için ilgili olabilir) — https://www.defterbeyan.gov.tr/documents/kilavuz/Perakende%20Sat%C4%B1%C5%9F%20Fi%C5%9Flerini%20Toplu%20Bir%20%C5%9Eekilde%20Ekleme%20K%C4%B1lavuzu.pdf ; https://www.defterbeyan.gov.tr/documents/kilavuz/toplu-gider-kullanim-klavuzu.pdf (kolonları bu turda açılmadı → TEYİTSİZ).

---

## 4. Biçim özet tablosu

| Biçim | Ayraç | Uzantı | Tarih | Ondalık | Kodlama | Kolonlar | Kaynak türü |
|---|---|---|---|---|---|---|---|
| Luca fiş (Excel Veri Aktarımı) | `;` | .csv (Şablon İndir çıktısı TEYİTSİZ) | 01.01.2020 | `,` (binlik `.`) | TEYİTSİZ (UTF-8/1254 test) | Fiş No;Fiş Tarihi;Hesap Kodu;Evrak No;Evrak Tarihi;Açıklama;Borç;Alacak;Miktar;Döviz;Kur | Resmi ekran görüntüsü |
| Luca fatura listesi (Excel Fatura Aktarımı) | `;` | .csv | 01.01.2019 | `,` ; formüllerde `.` | TEYİTSİZ | Serbest, ≤15 sütun, ≤400 satır; kullanıcı kural tanımlar | Resmi makale |
| Luca hesap planı | XML (makro üretir) / CSV (cari) | .xml / .csv | — | — | — | Şablon Excel'de; şema TEYİTSİZ | Resmi makale |
| Zirve fatura Excel | hücre tabanlı | .xls (resmi örnek; .xlsx TEYİTSİZ) | Excel tarih / GG.AA.YYYY | Excel sayı / `,` | BIFF cp1254 | Tarih, Türü, Cari Adı/Açıklama, Evrak Numarası, Vergi/T.C Kimlik No, matrah%18, kdv%18, matrah %1, KDV%1, Para Birimi, Döviz Tutarı, Stok Kodu, Miktar (sıra serbest) | Resmi örnek dosya |
| Zirve banka Excel | hücre tabanlı | .xlsx | Excel tarih | Excel sayı | — | Tarih, Fiş No, İşlem Açıklaması, Borç, Alacak, İşlem Tutarı | Resmi örnek dosya |
| Zirve XML (Express Aktarım) | XML/ZIP | .xml / .zip | UBL | UBL | UTF-8 | UBL-TR e-Fatura/e-Arşiv (KISMEN TEYİTLİ) | Resmi blog + satıcı |
| Luca XML e-belge | — | — | — | — | — | Doğrudan yükleme ekranı yok; entegratör/AEN | Resmi makaleler |

## 5. Sonuç — "Örnek dosya olmadan güvenle yazılabilir mi?"

| Hedef | Karar | Gerekçe / kalan risk |
|---|---|---|
| **Luca muhasebe fişi CSV** | **EVET** | Kolon adları, sırası, tarih ve tutar biçimi resmi ekran görüntüsünden; ayraç `;`. Kalan risk: kodlama (UTF-8 BOM'lu + Windows-1254 iki seçenek sun), Şablon İndir'in gerçek uzantısı, 50 fiş/400 satır sınırına göre dosya bölme. Captcha nedeniyle yükleme elle. |
| **Luca fatura listesi CSV** | **KISMEN** | Sabit şablon yok; mali müşavir Luca'da kuralı bir kez tanımlar. Kolon adlarımızı belgeleyip örnek CSV vermemiz yeter. |
| **Luca API / web servis** | **HAYIR** | Belge yalnız TESMER sözleşmesiyle; Luca MMP için zaten yok. Kurucu kararı gerekir (başvuru). |
| **Luca'ya UBL-TR XML** | **HAYIR (gerek de yok)** | Doğrudan yükleme ekranı resmi kaynakta yok; e-belge entegratörden AEN'e akıyor. |
| **Zirve fatura Excel (.xls)** | **EVET** | Resmi örnek dosya elde; kolon sırası serbest, mali müşavir F5'te eşler. Kalan risk: .xlsx kabulü (xls üretmek en güvenlisi), KDV oranı başlıkları 2026'ya göre. |
| **Zirve yevmiye fişi Excel** | **HAYIR** | Böyle bir resmi ekran yok; Zirve fişi fatura Excel'inden kendisi üretir. |
| **Zirve XML (Express Aktarım)** | **KISMEN → fiilen EVET** | Şema adı resmi sayfada yok ama girdiler e-Fatura/e-Arşiv XML; Mihenk'in Nilvera'dan aldığı UBL-TR XML'i ZIP'lemek yeterli. |
| **Zirve API** | **HAYIR** | Herkese açık API yok. |

**Mihenk için önerilen paket (ekip oyu için):** (1) `Luca fişi (.csv ;)` dışa aktarımı — fatura başına 1 fiş, hesap kodu eşleme tablosu ayarlardan (120/600/391 + kuyum istisnaları), 50 fiş/dosya bölme, UTF-8 BOM ve 1254 seçeneği; (2) `Zirve fatura listesi (.xls)` — resmi başlıklarla, sayfa adı "Sayfa1"; (3) `e-Belge XML ZIP` — dönemin giden UBL-TR XML'leri (Zirve Express Aktarım ve genel amaç); (4) mevcut genel CSV korunur. Doğrulama: Luca/Zirve demo hesabı olmadan son kanıt **mali müşavirin bir kez yüklemesi**; bu, dış bağımlılık olarak açık kalır.
