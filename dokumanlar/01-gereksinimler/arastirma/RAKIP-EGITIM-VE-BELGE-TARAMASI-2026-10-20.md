# Rakip eğitim videoları ve belge taraması — 20 Ekim 2026

> **Amaç:** Türkiye'deki kuyumcu yazılımlarının **eğitim videolarını** (YouTube kanalları, oynatma listeleri, video başlık/açıklamaları), **kullanım kılavuzlarını**, SSS/yardım merkezlerini, bloglarını ve **sürüm notlarını** tarayıp her firmadan özellik listesi ve "nasıl yapılıyor" ayrıntısı çıkarmak; ByGatech ile karşılaştırıp eksik listesi değil **kapatma listesi** vermek.
>
> **Yöntem:** 5 paralel tarama (20.10.2026). Yalnız herkese açık kaynaklar; **hiçbir yerde hesap açılmadı, form doldurulmadı, giriş yapılmadı, demo istenmedi.** Video içerikleri izlenmedi; yalnız başlık, açıklama, süre, tarih ve (varsa) bölüm zaman damgaları okundu. Bu yüzden ekran içi adım sayısı/kısayol yalnız yazılı belgede ya da video açıklamasında geçiyorsa verildi; aksi hâlde **TEYİTSİZ**. Sayfalardaki yapay zekâya yönelik gömülü metinler yalnız veri sayıldı.
>
> **ByGatech sütunu** koddan değil şu belgelerden dolduruldu: `RAKIP-OZELLIK-ESITLEME-PLANI-2026-10-15.md` § 2–3 (kod yollu "Bizde" sütunu), `MUADIL-ARASTIRMASI-2026-10-17.md` § 2.2/3.2, `DURUM-OTURUMLAR.md` (18–20.10 kayıtları: D1-01…07, 09…20, D2-12, D3-01, D3-11, 8 banka bağlayıcısı, e-Fatura/e-Arşiv Nilvera katmanı, fatura şablonu, Luca/Zirve aktarımı, M7/M8). Tek kod kontrolü: `Modules/EBelge/EBelgeModulu.cs` uçları yalnız ayar/gönderim/durum/PDF — **gelen e-fatura alma yok**.
>
> **İşaretler:** ✅ var · 🟡 kısmen · ❌ yok · ? emin değil · **[Ü]** üretici beyanı · **TEYİTSİZ** doğrulanamadı. Firma bölümlerindeki `[n]` o bölümün kaynak listesine bağlanır.

---

## 1. Özet (10 satır)

1. **Taranan firma: 24** (12 ana + 12 ek). Eğitim videosu olan firma **11**; gerçek bir "ekran başına video" seti yalnız **Pusula (36 video, 2016)**, **M&L (ML Akademi 88 video)**, **Hasfiyat (49 video, 1–2 dk, 2026)**, **E-Yaz Milyem (34+ video, 5 yıllık)**, **Softnorm (12 video, biri 30 dk "kısayol tuşları")** ve **KuyumSmart (bölümlü 4 video + 14 Shorts)**'ta var. Dezy, KuyumHub, Milyem Plus, Terminal Gold, KuyumBase, Gramino, Kuyum Plus (yalnız 10 sn klip): **eğitim videosu yok.**
2. **Yazılı kılavuz** yalnız **SarrafPro (kilavuz.sarraf.pro, ~60 sayfa)**'da; SSS derinliği **KuyumSmart (81 soru)**, **Dezy (33)**, **Bakiyem (23)**, **Hasfiyat (18 + haftalık sürüm notu)**. Diğerlerinde SSS ≤ 11; CetaSoft kılavuzu yalnız basılı/CD; Logo belgeleri (docs.logo.com.tr) bu ağdan açılamadı.
3. **Kısayol tuşu yayımlayan iki rakip:** Terminal Gold (**F2 ürün · F3 cari · F4 KARMA · F12 tahsilat**) ve CetaSoft (**F7 tahsilat · F8 peşin sat**, 2009 ekran görüntüleri). ByGatech'in F2/F4/F8/F9'u var; ama **ekranda kısayol yardımı + kısa video** hiçbir rakipte de yok → kolay fark alanı.
4. **Rakipte olup ByGatech'te OLMAYAN, dış bağımlılığı olmayan en önemliler:** (a) gişe kasası / personel **sabah zimmeti → akşam sayım → ana kasaya devir** akışı (Ariyen 7 adım); (b) **günlük servet özeti**: kasa+banka+stok+cari TL ve has çift kolon, **dünle fark**, riskli cari düşük ağırlıklı "tahsil edilebilir servet" (Dezy); (c) **prim simülasyonu "bu oranla geçen ay ne öderdim"** (Dezy — 15.10 planındaki tanımsız "dene-gör"ün karşılığı **bulundu**) + avans eşiği onayı + ay sonu maaş+prim−avans tek fiş; (d) banka **PDF ekstresinden** hareket okuma (Dezy VakıfBank/Ziraat, Hasfiyat e-posta/PDF/Excel); (e) **has fiyat kaynağı seçimi** (TCMB saatlik / BİST günlük / Harem-Nadir-Ahlatçı dakikalık, SarrafPro); (f) hurda alışında pazarlık payı aşılınca yönetici onayı (KuyumSmart); (g) stok durumu "yolda / rezerve / kayıp / tamirde" (Terminal Gold 11 durum); (h) paylaşılabilir, süreli canlı fiyat linki ve QR ürün sayfası/kiosk (SarrafPro, KuyumSmart); (i) **eğitim videosu + ekranda kısayol yardımı**.
5. **Dış bağımlılığı olanlar:** gelen e-fatura kutusu (Softnorm QNB, E-Yaz İzibiz, Faturalog, SarrafPro), e-İrsaliye/e-Gider Pusulası (KuyumHub, Hasfiyat, Softnorm, E-Yaz), WhatsApp Business şablon tetikleyicileri ve WhatsApp'tan kimlik görseli okuma (SarrafPro), NFC/OCR kimlik (KuyumHub, Kivi, KuyumSmart, SarrafPro, Hasfiyat), RFID seti (Zebra ZD621R + Chainway C5 fiilî standart: Pusula, M&L, Softnorm, KuyumSmart, Kuyum Plus, Terminal Gold, Ariyen), ÖKC (Kuyum Plus Pavo N86, Nebim Ingenico), e-ticaret fiyat gönderimi (Hasfiyat → WooCommerce/ikas/IdeaSoft/Ticimax), AI ürün görseli (KuyumSmart AI Studio, Dezy).
6. **Terazi entegrasyonu hiçbir kuyumcu programında yazılı değil** (yalnız Mikro Hızlı Satış genel terazi; Dezy paket listesinde ad var, ayrıntı yok). Emanet ekranı yalnız CetaSoft, Softnorm, KuyumSmart, Gramino, Ariyen, Dezy, Kuyum Plus'ta belgeli; Pusula, KuyumHub, M&L, SarrafPro, Terminal Gold'da belgesiz.
7. **ByGatech'te olup rakiplerin hiçbirinde belgelenmemiş olanlar:** tevsik haddi üstü nakdi **durduran** kapı; ŞİB **10 iş günü** sayacı + gizlilik; gider pusulası iptal/yeniden düzenleme ve 7 gün sayacı (KuyumHub'da da var); **tarihli yasal sınırlar** (rakiplerin yarısı hâlâ 185.000 TL yazıyor: KuyumHub, Kivi); sentetik taş kapısı; FIFO/LIFO parti maliyeti; vezne kuyruğu; stopaj dönem borcu; KVKK şifreli dışa aktarım + anonimleştirme; **bankalara doğrudan 8 bağlayıcı** (rakipler aracı/okuma API'si kullanıyor, hiçbiri banka adıyla uç belgelemiyor); dış API + canlı kur kanalı (Hasfiyat'ta da var); Luca **resmî şablon** fiş dosyası (rakipler "Luca/Logo dökümü" diyor, biçim vermiyor).
8. **Büyük ERP'ler kuyumcu çözümü değil:** Nebim V3 sektör listesinde kuyum yok (Reis, Permun genel perakende modülüyle; "Nebim Gold Çözüm Ortağı" Asist Yazılım IJS'de stant açıyor); Logo'da tek kuyum işlevi **Netsis Has Altın Uygulaması** (2021 belgesi) + Tiger banka hesabında kıymetli maden (2.83) + "Özel Matrah" e-fatura tipi; Mikro'da kuyum modülü yok (dövizli/tartılı stok + terazi). Akınsoft, DİA, Zirve, Luca: kuyumcu modülü bulunamadı.
9. **İsim tuzakları:** "Altın Pusula" kuyumcu ERP'si değil, tüketici altın fiyat uygulaması (Play Store); "Kuyumcu Pro" yalnız arşivlenmiş vitrin site + 3 günlük "KuyumPro Elite" kanalı (TEYİTSİZ); "Sarraf Otomasyon" adlı ürün yok (SarrafPro = Backhub; sarraf.io ayrı firma); **Milyem Plus ≠ E-Yaz Milyem**; "Meadify" bulunamadı; Kuyumcum'un YouTube kanalı Kuvve Teknoloji'ye yönleniyor (TEYİTSİZ).
10. **Önerilen sıra (§ 4):** önce dış bağımlılıksız 14 kalem (≈ 8 S + 5 M + 1 L, tahmini 60–100 saat), sonra entegratör/Meta/donanım bekleyen 9 kalem. En yüksek satış etkisi: eğitim videosu seti, gişe zimmeti, servet özeti, prim simülasyonu, banka PDF okuma.

---

## 2. Firma başına bölümler

### 2.1 Faturalog (faturalog.com)

**Kaynaklar (belge 15 · video 0):** [1] https://faturalog.com · [2] /otomatik-fatura-modulu/ · [3] /e-fatura-yonetimi/ · [4] /acik-bankacilik/ · [5] /cari-modulu/ · [6] /urun-yonetimi/ · [7] /toplu-fatura/ · [8] /fiyatlandirma · [9] /sikca-sorulan-sorular (6 soru) · [10] /blog (3 yazı) · [11] /faturalog-toplu-fatura-modulu/ · [12] /bankalar-arasi-mekik-dokumaya-son/ · [13] /muhasebede-yapay-zeka-donemi/ · [14] /hakkimizda · [15] https://portal.faturalog.com (giriş sayfası). YouTube kanalı yok; Play/App Store'da uygulama yok (ana sayfa mağaza düğmeleri boş `#`).

**Özellikler:** Kuyumcuya özel modül **YOK** (sitede kuyumcu/altın/has/özel matrah geçmiyor; yalnız logolar) [1][9]. Banka: tüm bankalar tek panel, bakiye canlı, hareket kronolojik, "anlık mutabakat" ile cariye otomatik eşleşme; örnek bankalar Akbank, Garanti, İş, YKB, Ziraat, Halk, Vakıf, QNB [4][9][12]. Fatura ("Otomatik Zeka"): hareket → kullanıcı kuralı → cari açılır → saniyeler içinde fatura → e-posta [2][13]; gelen e-faturalar panele düşer, tek tık kabul/ret; 10 yıl saklama [3]; Toplu Fatura: Excel/müşteri grubu, zamanlanmış ve tekrarlayan fatura, mükellef sorgusuyla senaryo seçimi [7][11]. Cari ("AI Hub"): gönderen bilgisinden otomatik cari, vade, tek tık mutabakat e-postası + yanıt takibi, risk limiti [5]. Ürün: KDV/birim fiyat/GTİP; satışla stok düşer [6]. E-ticaret: Trendyol, Hepsiburada, N11, Amazon, Etsy (yorum) [9][14]. Fiyat: kontör 1,50–2,50 ₺/fatura [8]. Satış/hurda/emanet/MASAK/etiket/personel/kampanya/kasa/şube/kur: yok.

**İş akışı ayrıntıları:** Toplu fatura 4 adım: siparişleri seç → "Fatura Oluştur" → veri aktarılır → mükellef sorgusu ile senaryo [11]. Ekran adları (görsel alt metni): Otomatik Zeka Arayüzü, Banka Entegrasyon Ekranı, Açık Bankacılık Arayüzü, Cari Yönetim Ekranı, Mutabakat Ekranı [2][4][5]. Kural tanımlama mantığı sayfada yok.

**TEYİTSİZ:** mobil uygulama; API belgesi; blog tarihleri; yardım merkezi (iletişime yönleniyor).

### 2.2 Pusula Yazılım · PusulaX · Kivi (pusulanet.net · pusulax.com · kivierp.com)

**Kaynaklar (video 36 + Kivi 7 kuyum videosu · belge 16):** YouTube @pusulayazilim (649 abone, 36 video; açıklamalar başlıkla aynı, adım/kısayol metni yok). Perakende ders seti (hepsi 2016, 45 sn–4 dk): Satış İşlemi Genel (PGPMDU4tBtU, 3:54) · Altın Giriş (Ks-jlevX6KE) · **Satış + Hurda Tahsilat + Açık Hesap** (tFXxTQ5tu9o, 4:23) · Nakit Satış (AnHyL4o4LUo) · Kredi Kartı (zuBqQYFdu5k) · Ziynet Satış (r1yLPNMyD24) · Ziynet Alış (hmHjAeBmSMo) · Hurda Alış (HCI-7TZdaG8) · Hurda Çıkış (dgnU_1_Xu8E) · **Hurdadan Vitrine Ürün Çıkma** (JHkJfgwwCLA) · **Barkodlu Ürünü Hurdaya Çıkma** (rTETKJ4bTq0) · Barkodlama Gram (EBPp-pYUdX8) / Adet-İşçilikli (5Lqs_2iClDs) / Özel Ürün (VY_IvKzbpIM) · Özel Ürün Alış pırlanta-saat-gümüş-çelik (Ea_lGET03SI) · Hesap Tanımlama (UEyfwG9C8dY) · Nakit Tahsilat (wqUcFZJlWWg) / Ödeme (5E2juj5lD-U) · Kart ve havale ile tahsilat (7bMvmlLHW1g, 2023) · Satış İadesi (xkaEoueU8bI) · Barkodsuz Altın İade (zzEm_OfU5rs) · Ürün Değişimi (IpMwcGAOhmk) · Toptancıya Barkodlu İade (DvxXijQT7xM) · Döviz Alış/Satış/Tanımlama (QmODe3t5Sbk, 6HPLmNS-Z04, KDsRKrgZZlI) · Ana Menü (6TVzY6Swy60) · Yedekleme (_2KMPO-JIPU) · Pars Ziynet (hqWhqmHL6zU, lkPT_F1FRPo, FC7GMpGMaT8) · RFID Nethom Swing-U (QqdizqqQozc) · Zebra ZD621R kalibrasyon (_8fYJGwBJKY, 2022) · **PusulaX Demo** (3k-tpHf-5C0, 10:51, 16.02.2026). Belgeler: [B1] pusulanet.net · [B3] /Galeri/Video-Detay/15/21 Perakende dersleri · [B4] /15/22 Toptan dersleri (5 başlık, video gömülü değil) · [B5] /15/23 Üretim dersleri (7 başlık) · [B6] /Home/sss (3 soru, 2'si "hazırlanıyor") · [B7] blog (2 yazı: EveryWAN el terminali, RDP port) · [B8] /Cozumler/Detay/7/36 fiyat: Perakende/Toptan/Üretim **1.500 USD**, Pırlanta Üretim 1.800 USD · [B11] pusulax.com · [B12] kivierp.com · [B13] /kuyum-superapp · [B14] /fiyatlar (JEWELLERY 22.680 ₺/yıl) · [B15] /bilgi-merkezi (13 yazı; /blog/kuyum-masak) · Kivi videoları: Banka Hareketlerinden Altın Faturası (WFprQGkTZ5w) · İşçilikli Fatura Özel Matrah (nZ_a7N155yE) · İşçiliksiz Altın-Gümüş Faturası (5H-EvL5ImIY) · Cep POS (p-bg4tCYRvc) · Ekstreden Fatura (LUbihDEr_20) · YTB KDV İstisnası (Vc9SvEhuQbU, 18.10.2026).

**Özellikler:** Satış: nakit/kart/havale/açık hesap/ziynet/döviz ayrı ekranlar; PusulaX tek satış ekranında tüm ödemeler + risk/bakiye [B11]. Alış/hurda: hurda alış/çıkış, hurdadan vitrine, barkodludan hurdaya, ziynet alış, özel ürün alış; üretimde ramat/fire/ayar düşürme [B5]. Emanet: belge yok. Cari: açık hesap, tahsilat/ödeme, toptan çanta takibi, geciken listesi [B4]. Banka/POS: perakendede POS komisyon takibi [B1]; PusulaX+Kivi: banka hareketi cariyle eşleşme, sanal POS [B11]; Kivi açık bankacılık [B13]. e-Belge: Pusula'da yok → Kivi: **805/808 (işçilikli), 230 (külçe) otomatik**, 1567 "has kesme", SMS/WhatsApp ile fatura, pazaryeri siparişinden e-fatura [B13]. MASAK: Kivi kamera/NFC kimlik, dijital imza; eşik hâlâ **185.000 TL** yazıyor [B13][B15]. Donanım: gram/adet barkodlama, RFID Nethom, Zebra ZD621R, EveryWAN el terminali, cihaz satış sitesi. Mobil: PARS (uzaktan yönetim, şüpheli harekette bildirim), PusulaApp (favori işlemler, barkod okutup fiyat) [B1][B11]. E-ticaret: Kivi pazaryeri. Atölye: Üretim programı (atölye bölümleri, bölüm stok/fire, ramat, katkı), Pırlanta Üretim [B5][B8]. Kasa: toptan çoklu kasa, döviz kasası. Şube: PusulaX çoklu şube [B11]. Muhasebe: Kivi mali müşavir entegrasyonu [B14]. Kur: Kivi BIST/Harem/spot/Kapalıçarşı [B13].

**İş akışı ayrıntıları:** Ana Menü → Altın Giriş · Satış (Nakit/Kart/Ziynet) · Hurda Alış/Çıkış · Ziynet Alış · Özel Ürün Alış · Döviz Alış/Satış · Hesap Tanımlama · Nakit Tahsilat/Ödeme · İade (3 tür) · Değişim · Barkodlama (3 tür) · Yedekleme — **ekran başına bir video, her biri 1–4 dk.** Satış fişinde hurda ödeme aracı + kalan açık hesaba (tek akış, 4:23). Hurda döngüsü: Hurda Alış → Hurda Çıkış (toptancıya/ramat) ↔ Hurdadan Vitrine. PusulaX "Ürün Üretim Ekranı": ürün + taş/maden + barkodlu etiket + maliyet/satış fiyatı tek ekranda [B11]. Kivi: banka hareketi → açıklamada altın ifadesi → "akıllı işlem" fatura taslağı → 805/808/230 → GİB; Cep POS linkiyle tahsilat+fatura aynı akışta.

**TEYİTSİZ:** ekran içi adım/F tuşu; emanet; prim; gün sonu kasa; Kivi /dokumanlar boş şablon; Kuyum SuperApp fiyatı iki sayfada farklı (18.900 ₺+KDV vs 22.680 ₺).

### 2.3 Dezy (dezyapp.com)

**Kaynaklar (belge 24 · video 0):** [D1] dezyapp.com (30 banka listesi) · [D2] /features/ · [D3] /pricing/ · [D4] /sss/ (33 soru) · [D5] /blog/ (≈37 yazı; 2. sayfa 404) · [D7] /kuyumcu-pos-sistemi/ · [D8] /kuyumcu-kasa-ve-altin-yonetimi/ · [D9] /kuyumcu-veresiye-takip-programi/ · [D10] /kuyumcu-e-fatura-programi/ · [D11] /kuyumcu-atolye-programi/ · [D12] /kuyumcu-personel-prim-takibi/ · [D13] /kuyumcu-barkod-etiket-sistemi/ · [D14] /kuyumcu-e-ticaret-entegrasyonu/ · [D15] /kuyumcu-yapay-zeka-ozellikleri/ · [D16] /altin-takip-sistemi/ · [D17] /kuyumcu-stok-takip-programi/ · [D18] /kuyumcu-muhasebe-programi/ · [D19] /mucevher-yonetim-sistemi/ · [D20] /bilezik-takip-programi/ · [D22] blog: e-fatura/özel matrah rehberi (09.06.2026), mobil tabletten satış (09.06), günlük servet takibi (10.06), stok yönetimi (21.09) · [D23] App Store id6748637560 (geliştirici "sezgin paksoy", **v1.3.7 18.10.2026**) · [D24] Play com.paksoygrp.dezymobile. YouTube kanalı yok; SSS "video eğitim" diyor, bağlantı yok.

**Özellikler:** Satış (Tezgâh): alış+satış aynı fiş; barkod → canlı kur; kazanç has gram; karma ödeme (nakit/kart/havale/kapora/veresiye/altın/hurda); eşik üstü satış patron telefonuna onay; v1.3.7 ödeme seçimi tek ekran, zararlı kalem "zarar" etiketi [D7][D23]. Hurda: hurda havuzu, ölçüm-ayar-has-kur müşteriye gösterilip dökümlü fiş; tahlil farkı düzeltme kaydı [D4][D20]. Emanet: veresiye aynı ayar altınla kapanırsa **fazlası emanete** [D9]. Cari: TL ve has ayrı; WhatsApp hesap özeti; kara liste/risk; **riskli cari düşük ağırlıkla "tahsil edilebilir servet"** [D9][D22]. Banka: 30 banka salt okuma [D1]; banka PDF ekstresinden (VakıfBank, Ziraat) toplu e-fatura [D10]. e-Belge: dahil GİB entegratörü; VKN sorgusu; **23/e: has gram × canlı kur matrahtan düşülür, KDV yalnız işçilikten, 805**; iptal/durum içeride; günlük e-arşiv raporu; telefondan fatura (1.3.7) [D10][D22][D23]. MASAK: v1.3.7 "eşik üstü işlemde kimlik formu" [D23]. Rapor: **günlük servet özeti (kasa+banka+stok+cari, TL ve has çift kolon, dünle fark)**, toptancı açık pozisyon, altın pozisyonlu bilanço, çalışan karnesi [D8][D18][D22]. Etiket: sürükle-bırak tasarımcı, Code128/EAN-13/QR, **PVC sertifika kartı + QR doğrulama**, rehberli sayım (satış sürerken), RFID "çok yakında" [D13][D17]. Mobil: iOS/Android, onay/ret, diğer şube stoku + rezerve talebi, fiyat tahtası, biyometrik giriş, **Becky** sesli komut [D22][D24]. E-ticaret: Trendyol/Hepsiburada/Amazon/Pazarama/İdefix/Shopify; gece AI ilan açma, sabah özet; hakediş/yoldaki para [D14]. Personel: **prim otomatik (satış kârı + hurda has gramı; iadede geri alınır; veresiyede tahsilata bağlı), avans eşiği onayı, ay sonu maaş+prim−avans tek fiş, "bu oranla geçen ay ne öderdim" simülasyonu** [D12]. Kampanya: VIP segment, doğum günü/yıldönümü WhatsApp [D19]. Atölye: iş emri (beklemede/üretimde/hazır/teslim), fire gram bazlı, fason has bakiyesi, külçe vault, ayar-milyem çevirici [D11]. Kasa: nakit+ziynet+hurda+döviz tek kasa; çalışan kasası; **kör sayım, 30 dk önce hatırlatma, gerekçeyle yeniden açma, fiş fotoğrafı** [D8]. Şube: sınırsız; transfer; konsolide [D2]. Muhasebe: Excel/XML; çift defter (TL + has) [D18]. Kur: TCMB + piyasa, manuel kilit; FIFO/LIFO/ortalama [D4][D16]. Fiyat: 2 paket (tutar yok), 3 ay ücretsiz; 6 saatte yedek; Windows/macOS masaüstü çevrimdışı çalışır [D3][D4].

**İş akışı ayrıntıları:** Satış: barkod → canlı fiyat → aynı fişe alış satırı → ödemeyi böl → kalan anlık → kasa+stok. Eşik üstü → push → tek dokunuş onay/ret, **sessiz saat** tanımlanabilir. Banka PDF'den fatura 4 adım: PDF yükle → EFT satırları ve IBAN eşleşir → müşteri/ürün onayla → toplu kes. Gün sonu: kör sayım → fark varsa patron onayı → kapanış. Stok rehberi 7 adım (sınıf → ölçüm standardı → tekil kimlik vitrine çıkmadan → işçiliği altından ayır → hareket türü → sayım farkına sebep zorunlu → gün sonu kıyas). Ekran adları: Tezgâh, Mobil satış ekranı, Onay ekranı, Öz varlık, Pano, Müşteri kartı, Etiket tasarımcısı, Rehberli stok sayımı, Çalışan kartı/avans/hesap fişi/prim/karne, E-ticaret panosu, Fiyat tahtası. Kısayol listesi yok.

**TEYİTSİZ:** paket tutarları; "50+ mağaza"; SMS; sadakat puanı; tartı ayrıntısı; "Çantacı özellikleri" sayfası açılmadı.

### 2.4 KuyumHub (kuyumhub.com — Fluxverys, Ankara)

**Kaynaklar (belge 13 + 19 blog · video 0):** [1] kuyumhub.com · [2] /ozellikler · [3] /kuyumhub-nedir.html (28 banka listesi, SSS) · [4] /fiyatlandirma.html · [5] /kuyumcu-mobil-uygulama.html · [6] /blog (19 yazı) · [7] /blog/kuyumcu-magaza-takibi-rehberi.html · [8] /blog/en-iyi-kuyumcu-mobil-uygulamasi.html · [9] /blog/kuyumcu-banka-hareketleri-takibi.html · [10] /blog/kuyumcu-kasa-sayim-rehberi.html · [11] /blog/kuyumcuda-taklas-rehberi.html · [12] /mevzuat/kuyumcu-2026-yukumlulukleri.html · [13] /mevzuat/kuyumcu-masak-uyum-rehberi.html · [14] portallar magazam.kuyumhub.com, muhasebe.kuyumhub.com (açılmadı). YouTube kanalı ve mağaza URL'si bulunamadı.

**Özellikler:** Mimari: mevcut masaüstü programın **üzerine** kurulur; "KuyumHub Agent" veriyi buluta taşır [2][3]. Satış: 8 metrikli ana ekran; ciro/indirim sapması bildirimi; 30.000 TL üstü fişler işaretli [4][5][12]. Gider pusulası: numaralı, **7 gün sayacı**, dekont bağlama, hurda alımında nakit ödemeye izin vermez, özel matraha tek tık, e-Gider Pusulası [2][12]. **Taklas**: eski altın has → yeni ürün → net has farkı → iki kalemlik defter kaydı otomatik [11]. Cari: TL ve has ayrı; "mevduat ve altın birikim hesapları" (Profesyonel) [4]. Banka: 28 banka okuma; faturalı/faturasız ayrımı; açıkta kalan tahsilat; kimliksiz büyük havale listesi [3][9]. e-Belge: mağazanın kendi entegratörüyle; özel matrah otomatik [2]. MASAK: **185.000 TL** (güncel değil); NFC + OCR + dijital imza; BM/OFAC günlük; PEP; risk skoru; parçalı işlem; MASAK Online STR formu hazır; 8 yıl saklama [2][13]. Rapor: hareketsiz stok, devir hızı, uykuda müşteri; Türkçe doğal dille rapor [2][4]. Yapay zekâ: sesli/yazılı soru → grafik + kaynak fiş [2]. Mobil: Face ID, push, has bakiyesi 22/18/14, iPad "tezgâhüstü kasa modu"; çevrimdışı satış iki sayfada çelişkili [5][7][8]. Personel: sabit/kademeli/hedef prim, vardiya, roller [2]. Kasa: nakit/POS/havale/has ayrı; vardiya kapanışı fark ekranı; sayım fişi PDF [7][10]. Şube: sınırsız; sevk "transit" [2][10]. Muhasebe: mali müşavir portalı; Luca/Logo/Excel [1][3]. Fiyat: Esnaf 4.166 ₺/ay, Profesyonel 8.333 ₺/ay (şube başı), 14 gün deneme [4]. E-ticaret, sadakat, SMS, etiket yazıcı, RFID, terazi: belirtilmemiş.

**İş akışı ayrıntıları:** Kurulum 3 adım (demo → uzaktan bağlantı → mağazadan indir). Banka eşleştirme: gönderen adı + tutar ↔ müşterinin son satış/faturaları; hurda ödemesi bankadansa dekont gider pusulasına [9]. Günlük kapanış: TL kasa → gram-has (vitrin+kasa+atölye) → POS/banka/çek → sayım fişi imza; kabul edilebilir fark ±50–100 TL, 0,01–0,05 g/parça [10]. Ekran adı/menü/kısayol yok.

### 2.5 CetaSoft Kuyum (cetasoft.com.tr/kuyum — İzmir, Windows masaüstü)

**Kaynaklar (belge 18 + 9 ekran görüntüsü · video 3):** [1] /kuyum/ · [2] /kuyum/yap.php · [3] /kuyum/arabirim.php → Kuyumcu01…09.gif (2009 tarihli, KDV %18) · [4] /kuyum/modul.php · [5] modparapuan · [6] modposvezne · [7] modenvanter · [8] modpatron · [9] modmobilizleme · [10] modbarkod · [11] modmuhasebe · [12] modtoptan · [13] /kuyum/indir.php (deneme exe; kılavuz yalnız basılı/CD) · [14] /kuyum/sms.php (NetGSM) · [15] /kuyum/satinal.php (Standart 40.000 ₺+KDV/PC, ek modül 15.000 ₺, e-Fatura 10.000 ₺) · [16] /efatura.php (TÜRKKEP 4 adım; fiyat [15] ile çelişkili) · [17] /rfid.php · [19] YouTube UCWjYade83GMuRm3eJW3jgzw (2 video): [20] N0hUdHMwCEM "POS Tablet Uygulaması Tanıtım" (5:46, 2024) · [21] dsh1NLo9P74 "**Üzerinde sarrafiye altını bulunan ürünleri barkodlama**" (3:53, 2023; iki ayardan oluşan mal tek barkod) · [22] D6gMgrBA4dw RFID (3. kişi).

**Özellikler:** Satış: "Alışveriş" ekranı (Perakende Satışlar / Kredi Kartı ile Satışlar / Perakende Alışlar sekmeleri; alanlar Tarih, Stok Kodu, Gram, Adet, Peşin Alınan, Müşteri, Ödeme tarihi, Kalan borç, Satıcı Personel) [3]; "**Alışveriş Föyü**": SATILANLAR + TAHSİL EDİLENLER, birden çok föy açık, geri çağırıp düzeltme [2][3]; kart blokaj/komisyon otomatik; sipariş → satış; POS tablet (2024) [20]. Hurda: takoz, kalafat, hurdalaştırma; "Gider Pusulası ile Mal Girişi" sekmesi [2][3]. **Emanet**: cari ekranında "Emanet Bakiye" tablosu (S.Kodu, Alınacak, Verilecek, Fark), "Emanet Kaydet" [3]. Cari: VERDİM/ALDIM; ödeme tipi "MAL İLE"; bakiye TL/gr/$/€; yaşlandırma; vadesi geçene SMS [2][3]. Banka: hesap/kart takibi, entegrasyon yok. e-Belge: Resmi Muhasebe (fatura, gider pusulası, irsaliye, ÖKC); TÜRKKEP; ekranda İşçilik KDV matrahı alanları; "Karşılığını gider pusulasından düş" [3][11][16]. MASAK: hiçbir sayfada yok. Rapor: öz sermaye geçmiş tarihli, maliyet (hareketli ortalama), K/Z, fotoğraflı rapor; Envanter Hareket İzleme [2][7]. Donanım: barkod modülü (etiket tasarımı, sayım, garanti sertifikası, katalog), RFID (1 dk 2.000 ürün iddiası), terazi yok [10][17]. Mobil: Mobil İzleme (şube özeti), POS Tablet [9][20]. Personel: prim (satış/kâr kotası), maaş, satış engelleme, hanutçu hakediş [2]. Sadakat: **ParaPuan** plastik kart, mal türüne göre oran, özel gün toplu SMS [5]. SMS: NetGSM [14]. Atölye: tamir takibi, atölye carileri; imalat yok [2]. Kasa/vezne: Kasa Hesapları (TL/USD/EUR devreden), POS/Vezne: çoklu tezgâhtar, tezgâhtar başına mal+nakit, vezneler arası transfer [3][6]. Şube: sınırsız şirket, virman, Patron/Şube İzleme [2][8]. Muhasebe aktarımı: yok. Kur: "Kurlar" ekranı (has, 22 bilezik, 22/18/14/8, has gümüş, 8 döviz, Ons, Borsa; Pariteler/Ziynet/SPD/TCMB düğmeleri; 3 kaynak) [3]. Toptan: alış/ödeme/iade/virman; bakiye Has/TL işçilik/Milyem işçilik/USD/EUR/CHF; mutabakat; çantacı [12].

**İş akışı ayrıntıları (tek somut arayüz kanıtı):** Ana pencere: üst menü *Dosya · Stoklar · Hesaplar · Alışveriş · Toptan · Muhasebe · Barkod · Sertifika · ParaPuan · Transfer · Araçlar · Bilgiler · İletişim · Görünüm · Yardım*; sol büyük simgeler *Tanımlamalar · Hesaplar · Resmi Muhasebe · Bilgiler · Kontroller · Rapor/Analiz · Araçlar · Ayarlar*; sağ panel "Stok Kodu Özellikleri" (Tür, Stok gr/adet, Milyem, Hası, Has fiyatı, İşçilik) + Kurlar + Gün Özeti + Cari Hesaplar; MDI sekmeler. **Satış fişi 3 adım:** stok kodu (ör. "Ç" = çeyrek) → "LİSTEYE EKLE" → **F7 TAHSİLAT** (Tip M = mal/hurda, N = nakit, K = kart) ya da **F8 Peşin Sat** → "ALIŞVERİŞİ GERÇEKLEŞTİR"; sağda Satış/Tahsilat/Kalan ve =HAS =TL =USD =EURO; alt bar İskonto · Hesabı Yazdır · Fatura Kes · Yeni Föy. Malla ödeme: cari ekranında ödeme tipi "MAL İLE" + stok kodu + miktar. e-Fatura devreye alma 4 adım (e-imza → TÜRKKEP → kontör → lisans).

**TEYİTSİZ:** güncel arayüz (ekranlar 2009); kılavuz çevrimiçi yok; RFID'nin hangi modülle çalıştığı; e-Fatura fiyatı çelişkili.

### 2.6 M&L Yazılım (mlyazilim.com — 1993, Kuyumcukent)

**Kaynaklar (belge 25 · video ≥30 tanıtım + ML Akademi 88 eğitim bağlantısı):** [2] /kuyumculuk_programlari.html · [3] ml_perakende · [4] ml_imalat · [5] ml_gold_erp · [6] ml_diamond · [7] ml_jewel · [8] ml_set · [9] ml_siparis · [10] rfid_sistemi · [11] ml_ages_rfid_programi · [12] sertifika_sistemleri · [13] barkod_etiket_sistemleri · [14] mobil_programlar · [15] ml_ipad_jewel · [17] ml_kurpano · [18] ml_e_ticaret · [19] ml_web_rapor · [20] ml_jewelnet · [21] /haberler.html (sürüm 12 → 24; eski sürüm desteği Mart 2025'te bitti) · [22] ml_akademi · [24] **https://www.mlakademi.com/training_videos.html (88 video, 7 bölüm)** · [25] mlkur.com · [26] YouTube UCR4dy7BIvKLBu4z0Krt6CMA (530 abone). Tanıtım/demo: "Yeni Kuyumculuk Muhasebesi Modülü Çok Yakında" (y_cMKLsy1fE, 08.10.2026: MASAK, banka, e-Fatura, CRM, çoklu şube) · "ML IPad Peşin İşlemler Demo" (rk7jlUyrBHg, 31.08.2026) · IPad Sipariş (LJH6C3FK8_c) · Barkod Etiketleme (Bf4uuWQg4FQ) · IPad Jewel Saha Satış (4bz7Ca4xQOI) · ML Perakende (NrPXRHbqR7s) · Koala Dongle (VnMVhTwE3j8) · **Yeni ML Jewel ERP Ön Bilgilendirme** (gsDR1fLHmz4, 09.05.2026) · İmalat (Y6FPIXoyxeI) · MLAGES RFID etiket takma (UcgAbwoGN8k) · 2025/2026 özeti (iojZwaQgmk0) · Kur Pano (PACoZfck-Mk) · E-Ticaret (qNWUhqfbYcU) · Web Rapor (YnnVklI3od0) · MLJewel Tanıtımı (_N_BDsrFAhI, 2020, 11.305 izl.) · Ages IJS "3 dakikada 1000 ürün" (T9F5rSJN7Uw) · mljewel.net B2B (epqsKDVtDQw). **ML Akademi serisi [24]:** Tanımlamalar (15: Şirket, Kur Girişleri bMcWqsNglUs, Altın Stokları 0ywXRlVsGx0, Hurda, Nakit, Ziynet, Gümüş, Platin, Firma Kartları, Kullanıcı, Adetli, Değerli Taş, Diğer, Pırlanta…) · Atölye İşlemleri (17: Altın/İşçilik/Hurda/Taş Çıkışı-Girişi, Bölüme Çıkış, Atölye Kasası, Hurda Ayarlama) · Günlük İşlemler–Altın (13: Genel Anlatım Gigx1Of6tUU, Peşin İşlemler 6puQ1dUuyHQ, Alacaklandırma/Borçlandırma, Altın Alış/Satış -4h6bb-kpns, Bedelli Giriş/Çıkış, İade, İşçilik) · Hurda (6) · Mamul (7) · Nakit/Döviz (4) · Stok (8) · Raporlar (19).

**Özellikler:** Ürün ailesi (masaüstü, sürüm 24): MLJewel, MLPerakende, MLDiamond, MLİmalat (+MLAtölye), MLGold ERP, MLGümüş, MLSipariş, MLSet, MLCenter; Mayıs 2026 "yeni arayüzlü ML Jewel ERP", Ekim 2026 "Kuyumculuk Muhasebesi Modülü (MASAK, banka, e-Fatura, CRM, çoklu şube)" duyuruldu — **yayınlanmadı**. Satış: Fatura-İrsaliye tek ekran; iPad Peşin İşlemler (cari → ürün/ziynet → hızlı fiyat → ödeme hurda/nakit/ziynet/kart → fiş); iPad Jewel saha/fuar. Hurda: giriş/alış/çıkış/satış/bedelli + **Hurda Ayarlama**, Mamul/Stok Dönüşü. Emanet: belirtilmemiş. Cari: TL/EUR/USD/HAS. Banka/MASAK/e-belge: mevcut üründe yok, yeni modülde vaat; "MLe-fatura/e-arşiv/e-irsaliye" adı var, entegratör yok [21]. Rapor: 19 standart; MLWebRapor. Donanım: Zebra, 1.250'lik kuyum etiket ruloları, MLAges-RFID (Zebra RFID yazıcı, UHF okuyucu, "3 dk 1.000 ürün", gün sonu otomatik envanter), MLSertifika (Evolis/Zebra kart yazıcı, Ministudyo fotoğraf ünitesi), Koala dongle; terazi yok. Mobil: yalnız iPad (Jewel, Peşin, Sipariş, Sayım); Android yok. E-ticaret: milyem/dolar esaslı fiyat, tek kur girişiyle toplu güncelleme, n11/Hepsiburada/Trendyol; MLJewel.net B2B; Web Katalog. Personel: hakediş (maaş, avans, mesai, prim). Atölye: tezgâh/döküm/ocak/patlatma/cila bölümleri bazında altın-fire-ramat; Gold ERP üretim reçetesi; sipariş → üretim emri → teslim. Kur panosu: MLKurpano (web/mobil/TV, **oda/dernek ortak ekranı + reklam alanı**). Eğitim: ML Akademi sınıf/birebir, seviye tespit + sınav + sertifika; ilk eğitim ücretsiz.

**İş akışı ayrıntıları:** Program mantığı: her değer türü ayrı "stok" (Altın, Hurda, Nakit, Ziynet, Gümüş, Platin, Adetli, Taş, Pırlanta) ve her stok için aynı şablon **Giriş / Çıkış / Alış / Satış + Bedelli Giriş/Çıkış + Dönüş/İade**. Kurulum sırası: Şirket → Kur → stok tanımları → Firma Kartları → Kullanıcı. Atölye: altın "bölüme" çıkar, işçilik/fire ile geri girer; Atölye İşlem Kontrol Raporu ile mutabakat. Kısayol/menü yolu yok.

**TEYİTSİZ:** eğitim videolarının açıklama/süresi (bot doğrulaması); fiyat yok; 10 oynatma listesinin başlıkları.

### 2.7 Milyem Plus (milyemplus.com — Milyem Plus Yazılım Teknolojileri A.Ş.)

**Kaynaklar (belge 30 · video 1 (3. taraf)):** [1] milyemplus.com · [2] /kuyumcu-otomatik-fatura/ · [3] /blog/ (26 yazı, 10.06–20.10.2026) · [4] …/kuyumcularda-muesteri-havalesi-nasil-otomatik-eslestirilir/ · [6] …/kuyumcular-icin-oezel-matrah-nedir…/ · [7] …/kuyumcularda-rfid-teknolojisi-nedir/ · [8] /privacy/ · [9] YouTube Seg-BYXS9yE "Milyem Plus Uşak'taydı" (Uşak Havadis, 9:56, SMMM Odası semineri) · app.milyemplus.com (girilmedi). Resmî YouTube, mağaza kaydı, kılavuz, sürüm notu: **yok**. E-Yaz "Milyem" ile ilgisi yok.

**Özellikler:** Banka: 20+ banka salt okuma; IBAN + gönderen adıyla eşleşme, "%99,2 güven skoru"; eksik açıklama/çift kayıt/yüksek tutarda manuel onay [1][2][4]. Fatura: belge tipi + senaryo (Özel matrah) + ürün + gramaj + işçilik; GİB "4,2 sn"; istisna faturası; "dakikada 100+ fatura"; mali müşavir aynı panel [1][2]. MASAK: taranan/eşik üstü/bildirilen sayaçları, denetim izi [1]. Cari: havale eşleşince bakiye güncellenir [1]. Rapor: haftalık ciro, tahsilat dağılımı [1]. Satış/hurda/emanet/etiket/RFID/mobil/e-ticaret/SMS/atölye/kasa/şube: ürün belgesi yok (bloglar kavramsal). Fiyat yok; deneme kartsız.

**İş akışı ayrıntıları:** 5 adım: bankaya bağlan → hareketler gelsin → eşleşsin → fatura oluşsun → rapor. Panel yan menüsü: Genel Bakış · Banka Hareketleri · Faturalar · Cari Hesaplar · Stok · MASAK · Gider Pusulası · Raporlar; üst göstergeler: Bugünkü tahsilat, Kesilen fatura, Eşleşme oranı (%98,4), MASAK durumu [1]. Özel matrah örneği: satış 100.000, külçe 80.000 → matrah 20.000; 17/4-g (külçe) ile 23/e (ziynet) ayrımı kalem bazında [6].

**TEYİTSİZ:** entegratör ve banka API sağlayıcısı adı; mobil uygulama; "470+ mali müşavir" (sayfadan sayfaya 190/360/470).

### 2.8 Softnorm (softnorm.com — Markanorm, Bursa)

**Kaynaklar (belge 14 · video 12):** [11] softnorm.com (#sss, fiyatlar) · [12] /kuyumcu-programi/ · [13] /kuyumcu-barkod-yazilimi/ · [14] /rfid-kuyumcu-sayim/ · [15] /kuyumcu-fatura-programi/ (Pro+) · [16] /kuyumcu-e-fatura/ · [17] /kuyumcu-masak/ · [18] /vaka-calismalari/ · [19] /egitimler/ (12 video dizini) · [20] /blog/ (9 yazı) · [22] YouTube @softnormdestek1578 (61 abone, 12 video, **açıklamalar boş**): SOFTNORM 1 Programa Genel Bakış (DDke6E7Unkg, **22:09**) · 2 Yedekleme (5IQvxw9PMts) · 3 Ürün Giriş (vC7br0AvhH8, 12:31) · **4 Kısayol Tuşları ile Hızlı Kullanım (ZpKIflZ5sGQ, 30:47)** · Eğitim 1 Alışveriş Ekranı (JEVw-7VwzNw, 8:34) · 2 Stok Listesi · 3 **Ürün Dönüşüm Ekranı** (2H8e5DCTcPA) · 4 Ürün İade (Xk4ELOhwMwM) · 5 Borçlu Cari (xNmFSoKRqbs) · 6 **Emanet İşlemleri** (qTVvsxHLS1k) · Softnorm Mobile Tablet (nSjIHwrjzjc, 12:03) · Zebra ribon takma (VoUARefT8XM, 4,8 B izl.).

**Özellikler:** Masaüstü (Dongle) + tablet; paketler Standart (barkod+stok+kasa+cari) 1.250 $, RFID 5.250 $, Pro+ 500 $ (e-fatura/gider pusulası/banka/MASAK/yapay zekâ) [11][12]. Satış: Alışveriş Ekranı tek ekranda satış+alış; kısayol tuşları (liste TEYİTSİZ). Hurda: Ürün Dönüşüm Ekranı; Pro+ hurda alımında otomatik gider pusulası [15]. Emanet: ayrı ekran [22]. Cari: borçlu cari; toptancı ayrı [12]. Banka: Pro+ QNB altyapısıyla; gelen ödeme → fatura, giden → gider pusulası [15]. e-Belge: e-Fatura/e-Arşiv/e-İrsaliye/e-Gider Pusulası/e-Defter; **Gelen kutusu** (QNB eSolutions, kalan yasal süre, tek tık kabul/ret); durum akışı Taslak → Kuyrukta → Gönderildi → GİB → Ulaştı → Yanıt; UBL dışa aktarım; kontör bakiyesi üst çubukta, gönderilemeyende kontör iadesi [16]. MASAK: belge kaydedilirken karşı taraf taranır, belge yanında "MASAK: eşleşme yok" [17]. Rapor: Patron ekranları, anlık özsermaye [12]. Donanım: Zebra ZD220T, Konftel ürün kamerası, Ultra 5 etiket; RFID: ZD621R + Chainway C5 + 2.000 etiket, sayım 5 dk (vaka: 6–8 saatten 5–10 dk) [13][14][18]. Mobil: tablet uygulaması (mağaza kaydı yok). Personel: isteğe bağlı prim. Yapay zekâ: Pro+ asistan ekranı. Şube: "şubeli uygun". E-ticaret/SMS/atölye/muhasebe aktarımı/terazi: yok.

**İş akışı ayrıntıları:** Eğitim sırası: Genel bakış → Ürün giriş → Alışveriş → Stok listesi → Dönüşüm → İade → Borçlu cari → Emanet. Pro+ günlük akış: banka hareketi → cari eşleşir → e-Fatura/gider pusulası → MASAK kaydı. Demo yalnız AnyDesk ile (Dongle).

### 2.9 "Kuyumcu Pro" — TEYİTSİZ

kuyumcupro.com DNS'te çözülmüyor; Wayback 16.11.2025 kopyası (http://web.archive.org/web/20251116100547/https://kuyumcupro.com/) "AI destekli stok, 360° CRM, PWA offline, Docker, E2E şifreleme" sloganlı vitrin site; **firma adı yok**, satış/hurda/e-belge/MASAK ekranı yok. Ayrı aday: YouTube @KuyumProElite (1 abone, 2 video, 3 gün önce; eX8e1APWe8c "Satış, Stok ve Hesap Yönetimi" 0:54). CetaSoft ve Esmeva'da bu adla ürün yok. **Gerçek ürün/müşteri TEYİTSİZ.**

### 2.10 "Altın Pusula" — kuyumcu ERP'si değil

Tek eşleşme Google Play **"Altın Pusula"** (com.altinpusula.app): tüketici altın fiyat/portföy/alarm uygulaması (https://play.google.com/store/apps/details?id=com.altinpusula.app&hl=tr); altinpusula.com erişilemez. Pusula Yazılım'la bağı yok. Kuyumcu işletmesi için stok/satış/fatura işlevi yok.

### 2.11 SarrafPro (sarraf.pro — Backhub Yazılım A.Ş., Kuyumcukent)

**Kaynaklar (belge 40+ · video 10 (resmî 8 genel + Kuyum Haber)):** [31] sarraf.pro · [32] /moduller (+ /banka, /resmilestirme, /envanter, /ticaret, /esnaf, /denetim-raporlama, /interaktif, /konnektor) · [33] /uyum/masak, /uyum/musteri-tani-kyc, /uyum/kimlik-dogrulama, /uyum/musteri-sicil-yonetimi · [34] App Store id6754468183 (v1.6, sürüm notları 1.0–1.6), Play com.ai.sarraf · [35] **https://kilavuz.sarraf.pro** (~60 sayfa; /llms.txt dizini; .md ile okunur): Sarraf 101, Banka Akış, Otomasyon, Hızlı Fatura (POS), Gider Pusulası, Uyum, Envanter, İnteraktif, Raporlama, Mahsup, Socket, Könnektör, Konfigürasyon, Uygulamalar, Güncellemeler, Sözlük · [36] …/konnektor/pusula-yazilim.md · [37] /blog (59 yazı, 11.2024–07.10.2026) · [38] /akademi (6), /sss (60+ başlık, çoğu yanıtsız), /bankalar (35 banka), /pazaryeri · [39] /kuyumcu.md, /moduller/admin/fatura-ayarlari.md · [40] /guncellemeler/readme.md (V.1.2.68 19.08.2025, V.2.0.01 14.09.2025) · [42] YouTube @sarrafpro (5 abone, 8 genel video); Kuyum Haber: 4QBSadkZfaI, J_A9rr3Sb1I "Tek Tuşla MASAK Sorgusu" (19:49), GfzOd27DzMs "Bütün İşlemler Tek Ekranda" (GEN 1); Nilvera: fdBf8r2SFtI "SarrafPro x Nilvera". "Sarraf Otomasyon" adlı başka ürün yok; sarraf.io ayrı firma.

**Özellikler:** Banka: 35 banka; "Banka Akış" ekranı (tüm hesaplara gelen ödemeler canlı; OFAC/MASAK taraması; AI riskli hareket uyarısı ve satışı durdurma; personel notu; "hangi ürün satıldı" işaretleme; buradan fatura) [35]; Mahsup: TCKN/VKN uyuşmazlığında hareketteki doğru numara otomatik (1.2.68) [40]. e-Belge: hızlı satış faturası (bekleyen satışlar), ekstre satışları (Excel toplu), gider pusulası; Hızlı Fatura/POS akışı: terminal/API → standardize → müşteri eşle → kural kontrolü → arşiv; AI açıklama analiziyle ürün önerisi; istisna/özel matrah otomatik; **Fatura Ayarları: has fiyat kaynağı TCMB (saatlik, varsayılan) / BİST (günlük) / Harem-Nadir-Ahlatçı-Altınkaynak-Hakan (dakikalık)** [32][35][39]. MASAK/uyum 5 adım: kimlik oku → NVİ doğrulama → malvarlığı dondurma listesi → TCKN–banka hareketi loglama → kimlik+dekont+imza çıktısı; **WhatsApp Business grubuna atılan kimlik görselleri AI ile okunur**; QR kimlik, NFC pasaport, e-Devlet ikametgâh; müşteri tanı formu, risk skoru, ŞİB taslağı + uyum görevlisine bildirim; müşteri sicil notları tüm şubelerde [33][35][37]. Rapor: Mutabakat/Uyum/Mali, gecelik bakiye, kullanıcı işlem izi, Operasyonel patron paneli [32][35]. Mobil: iPhone/iPad/**Apple Watch**/Android widget'ları, çevrimdışı görüntüleme, TR/EN/AR [34]. E-ticaret: ikas; pazaryeri fiyatı kur değişince kanal bazlı marjla otomatik [32][38]. Personel: rol/şube bazlı erişim [37]. WhatsApp Business API: şablon + tetikleyici (rapor hazır, fatura onayı, yaptırım sonucu, hesap hareketi) [35]. Şube: Şube/Birim; Esnaf modülü toptancı → bayi banka/müşteri/e-belge paylaşımı [32][35]. Muhasebe: Könnektör/Socket **Zirve, Zirve Müşavir, Mikro, AIFASOFT ERPGOLD, PUSULANET**; Mahsup Excel kural ekranı [35]. **Paylaşılabilir Fiyat Ekranı** (canlı/statik, link, geçerlilik süresi, arşiv); Topluluk/dernek canlı kur [35]. Barkod/etiket/kasa/emanet/atölye: yok. Fiyat yok (modül bazlı).

**İş akışı ayrıntıları:** Resmileştirme: kaynak seç → müşteri+kimlik → kalemler → belge kontrol → fatura/gider pusulası; eksik varsa **beklemeye**. Esnaf: bayi tanımla (VKN+telefon) → paket → gelen hareketi kural/tekil/paylaş → bayi tek ekran → bekliyor/düzeltme/kabul/ret. Sarraf 101: hesap → müşteri → kimlik+uyum → banka/POS verisi → rapor. Online İşlem Merkezi: etiketleme, zaman damgası, **sık işlemleri kısayolla kaydet**. Eşik notları: 200 bin–2 Mn amaç seçimi (≥20 karakter), 2–20 Mn Nakit İşlem Beyan Formu, >20 Mn belgeli [38].

**TEYİTSİZ:** Nilvera'nın entegratör olduğu (video başlığından çıkarım); kılavuzun Hareketler/Dekont/Gelen e-Fatura/Pozisyon sayfaları boş; fiyat; "binlerce denetim sorunsuz".

### 2.12 E-Yaz Milyem / Expert Barkod (e-yaz.com.tr · expertbarkod.com — E-Yaz Yazılım A.Ş., Eskişehir)

**Kaynaklar (belge 6 · video 34+):** [43] https://e-yaz.com.tr/kuyumculuk-yazilimi/ (34 video bağlantılı özellik sayfası; site spam "hacklink" içeriyor — güvenlik işareti) · [44] /kuyumculuk-erp-yazilimi-fiyatlari/ + expertbarkod.com/kuyumculuk-yazilimi-fiyatlari/ (Temel/Uzman/Pro/Üretim, ömür boyu lisans, eğitim saatleri: Kuyumculuk 10 s, Hızlı Satış 7 s, Döviz 5 s, Tamir 3 s; ek kullanıcı +%20/%15; rakam yok) · [45] /e-donusum-modulu/, /mobil-yazilim/ (KOBİERP Mini) · [47] expertbarkod.com/kuyumculuk-yazilimi/, bilisimeskisehir.com · [48] YouTube E-Yaz Yazılım A.Ş. UCEsyEQ05hpPa0rYSHyiCAJA (1,79 B abone, 528 video); @milyemkuyumculukyazilimi (36 video, E-Pazar/E-Dönüşüm serisi); Milyem eğitim serisi (5 yıl önce, "Hızlı Satış Ekranı" odaklı): Altın Stokları (1CqdTqnovcU) · Barkodlu Satış (Qv6G_iCrfJc, 2:03) · Ziynet Alış-Satış (hgscUdfxVTM) · Kredi Kartı ile Tahsilat (79jE-ww30sE) · **Altın ile Tahsilat** (2kOd26qhcgE) · **Hurda ile Tahsilat** (oEn0FMwX6GE) · Cari Hesap Oluşturma (krU3O5r2Rmw, "iki farklı yol") · Cari Seçimi (36epRumzAWY) · Döviz Kur Girişleri (O-yDLN1yays) · Hızlı Kur ve Fiyat Değişimi (FGk8g5z5sKQ) · Barkodlama (EHKccCDPs-Y) · Giriş Ekranı (HNsEW2_ZhzQ) · İade (ey0Xohiq3rs) · Ürün Teşhiri (1U1D_c3YOQo) · Kayıt Düzenleme (he9hgRk9600) · stok kartları (Hurda l5g0sBL4bQ8, Ziynet, Pırlanta, Taş, Gümüş, Platin, **Nakit** PteGb_W8cT0, Diğer, Set) · tanımlar (Kasa, Depo, Şube, Döviz, Banko, Tamirat Durum 2RWYad_tWkQ, Meta, Firma Tip, Taş Özellik, Mamül Cins, **İkram**) · yetki (Firma Tipi İşlem Yetkileri LEZRk-VDRHg, Kullanıcı Hakları Lt6JOBQGRM8 8:10, İşlem Parametreleri) · E-Dönüşüm: Gelen e-Faturanın aktarılması (4:51) · E-Pazar: Pazaryeri Ayarları (13:15), Stok Gönderme (11:20), Sipariş Aktarımı (9:04).

**Özellikler:** Satış: **Hızlı Satış Ekranı**: cari seç → barkod okut → "Satışlar/Ödemeler" penceresi → tahsilat "Alışlar/Tahsilatlar" penceresine; ziynet alış/satış düğmeleri; teşhirden satışa; satışta kâr görünür [43][47]. Hurda/altın ile tahsilat (milyem, has tutarı → altın stoku oluşur). Cari: firma tipi bazlı işlem yetkisi; çek/senet. Banka: Banka ve Kasa modülü; kart tahsilatı Diğer Tahsilat > Kredi Kartı > POS hesabı; otomatik entegrasyon yok. e-Belge: e-Fatura/e-Arşiv/e-İrsaliye/e-Defter/e-Müstahsil/e-SMM; entegratör **İzibiz**; **gelen e-faturayı cari eşleşmesiyle tek tuşla aktarma**, eksik stok kartı için hızlı kart açma [45][48]. MASAK: yok. Rapor: has bilançosu, karşılaştırmalı bilanço, Web-Rapor, Yönetici Konsolu. Donanım: barkod/sertifika/katalog, RF-ID sayım; terazi genel barkod sayfasında. Mobil: KOBİERP Mini (kuyuma özgü ekran yok). E-ticaret: E-Pazar 6 pazaryeri + IdeaSoft. Personel: **metaya/işleme/cari grubuna göre yetki, zararına satış izni, kullanıcı başına azami iskonto**, şube erişimi; mesaj panosu/ajanda [43][47]. Atölye: Kuyumcu Üretim, Pırlanta Üretim, Tamir (Tamirat Durum tanımları). Şube: şube tanımı, kârlılık. Muhasebe: T-Set ML / KOBİERP Muhasebe otomatik fiş.

**İş akışı ayrıntıları:** Tanımlar → stok kartı türleri (Altın/Hurda/Ziynet/Pırlanta/Taş/Gümüş/Platin/**Nakit**/Diğer/Set) + Kasa/Depo/Şube/Döviz/Banko/Tamirat Durum/Meta/Firma Tip/Taş/Mamül/İkram; Yetki → Firma Tipi İşlem Yetkileri, Kullanıcı Hakları, İşlem Parametreleri. e-Dönüşüm: Firma Tanımları > GİB alanı → ayarlar → fatura kapanınca mükellefse e-Fatura, değilse e-Arşiv.

**TEYİTSİZ:** video açıklamaları (bot engeli); kılavuz yok; Milyem360 kanalının bağı; özel matrah desteği; emanet.

### 2.13 Terminal Gold / KuvvemERP (terminal.gold · kuvvem.com — Kuvve Teknoloji, Kartal)

**Kaynaklar (belge 18 · video 0 eğitim):** [1] terminal.gold (ürün turu 2:14 mp4; 19 banka; MASAK 370.000) · [2] **/ozellikler** · [3] /entegrasyonlar (19 banka adı; GİB doğrudan iddiası; iyzico/PayU/Param; Trendyol+Hepsiburada; muhasebe aktarımı **Logo Tiger/GO, Mikro Fly/Jump, ETA SQL, Netsis Wings, Luca Net** — hesap planı eşleştirme bir kez; REST API + OAuth2 + webhook + sandbox) · [4] /altin-esasli-muhasebe (TL + HAS çift eksen, 45 hareket tipi) · [5] /vergi-denetimine-hazir-kuyumcu (kayıt silinmez, iptal = ters kayıt; MASAK uyarır engellemez; 12.000 TL üstü TCKN + e-Arşiv) · [6] /rfid-kuyumcu-sayim (C5 ile ~500 parça ~15 dk; etiket kartona, metal UHF'i bozar) · [7] /mobil-uygulama (36 ekran; çevrimdışı 3–7 gün; App Store id6761362828, Play gold.terminal.mobile) · [8] /fiyatlandirma (Terminal Fatura 900 USD/yıl; Gold 4.000 + 400/yıl; Diamond 6.000; e-Gold 9.000; ek kullanıcı 50 USD/yıl; SMS 150 USD/5.000; veri aktarımı ücretsiz 3–7 gün) · [9] /kuyumcu-programi · [10] /terminal-fatura · [11] /sarraf-programi · [12] /terminal-diamond · [13] /pirlanta-uretim-programi · [14] /pirlanta-programi · [15] /e-ticaret · [16] /hakkimizda ("Kuyumcu Akademisi" 2026 sonu) · [17] kuvvem.com (+ /sektor/kuyumcu-magaza-programi/, katalog.pdf — kuyum iddiaları ürün belgeleriyle doğrulanmıyor) · [18] YouTube @KuvveTeknoloji (1 abone, 2 KuvvemERP videosu). **Eğitim videosu, kılavuz, SSS yok.**

**Özellikler:** Satış: **F2 ürün · F3 cari · F4 KARMA · F12 tahsilat**; KARMA (eski altın al + yeni sat, fark TL ya da has); fiyat modeli sabit/gram/yüzde/has/manuel; işçilik 4 tip; POS komisyonu iç/dış; altın fiyatı 5 dk önbellek; net pozisyon göstergesi [2]. Hurda: KARMA ile; gider pusulası belgesi yok. Emanet/konsinye: pırlanta sayfasında emanet/kapora/taksit tek ekran; konsinye verilen/alınan stok durumu [2][14]. Cari: 6 tip; TL/USD/EUR/GBP/has; 8 işlem tipi; kredi limiti; mutabakat formu; vade hatırlatma. Banka: 19 banka, 1 dk; IBAN/isim/geçmiş eşleme; "eşleştirme bekliyor" kuyruğu; havale Bekliyor→Yapılıyor→Tamamlandı. e-Belge: UBL; taslak/kuyruk/onay/hata 60 sn; özel matrah + istisna; birden fazla havale tek faturada [11]. MASAK: 370.000 üstü TCKN + yaptırım + KYC; uyarır engellemez. Rapor: dashboard; Diamond: bilanço, prim analizi, zararına satışlar, personel defteri. Donanım: RFID (ZD621, C5, 71×28 etiket), Sunlux karekod, Bluetooth UHF; terazi yok. Mobil: 36 ekran, çevrimdışı, push. E-ticaret: vitrin, pazaryeri, B2B bayi, Yurtiçi/Aras. Personel: kalem bazlı yetki. Kampanya: vitrin indirim; SMS/e-posta (Diamond). İletişim: tamir tamamlanınca SMS/WhatsApp; ödeme linki. Atölye: iş emri Alındı→Başladı→Tamamlandı→Teslim; pırlanta reçete. Kasa: 5 araç tipi; açılış/kapanış sayımı. Şube: transfer "**yolda**" durumu, şube mutabakatı.

**İş akışı ayrıntıları:** Stok hiyerarşisi Grup→Kategori→Stok Kartı→Parti→Stok Ögesi; ayar 333/585/750/916/999; **11 stok durumu** (mevcut, satıldı, konsinye verilen/alınan, transfer yolda, iade, kayıp, tamir, rezerve, pasif, tüketildi); "ayrıştırma" (toplu stoğu tek ürüne). Gün sonu: eşleşmeyen 1–2 banka hareketini onayla ~5 dk. Banka kurulumu: kurumsal internet bankacılığı + API yetkisi ~30 dk. Tamir teslimde tahsilat, cari kapanır.

**TEYİTSİZ:** "GİB'e doğrudan, entegratörsüz" ile "kontör entegratöre ayrı" çelişiyor; Gold paket içeriği tabloyla çelişiyor; kuruluş yılı/müşteri sayısı; bağımsız kullanıcı yorumu yok.

### 2.14 Nebim V3 (nebim.com.tr)

**Kaynaklar (belge 15 · video kanal 2 + 18 kayıt):** [1] nebim.com.tr (sektörler: Hazır Giyim, Halı/Mobilya/Züccaciye, Üretim, Optik, Ticari — **kuyum yok**) · [2] Nebim_V3_BR20.pdf broşür (kuyum/altın/ayar/gram geçmiyor) · [3] Reis Kuyumculuk vakası (5 mağaza; "gramlar ve işçilikler devreye giriyor", maliyet özel) · [5][6] "Nebim Gold Çözüm Ortağı Asist Yazılım" IJS 2024 ve 2025 stantları · [7] asistyazilim.com.tr (referans: Aja Jewels, Fiyonk Kuyumculuk) · [8] Permun Saat vakası (seri no, 4 para birimi) · [9] /nebim-v3-pos-ve-arka-ofis · [10] /nebim-v3-mobil-magaza (PAX A6650, Zebra/Bixolon yazıcı) · [11] /egitim-takvimi (2026 WebCast: 10 eğitim) · [13] NebimV3_Ver_25_10_1.pdf (altın/özel matrah geçmiyor; Sovos entegrasyonu bitti) · [16] YouTube UC05dm586N9cU4mJ3yO4XNtA (Nebim Planet 2026, V3 26.4 webinarı qmxBryc84io, Finrota açık bankacılık) · [17] 3. taraf "NEBİM MAĞAZACILIK EĞİTİM" UCO4LzNir1ze7rebTz16sJxw (Peşin Satış piK18W2xS3I, İade cnCjjGutmGw/rYSmD8tUZ3I, Fatura Yeniden Dök xRU9be3AcRs, Mağaza Hareket Özeti OPSGGFijz0Y, Merkeze İade, Mağazalar Arası Transfer Z5ZHBG2oKlI, POS'tan e-Arşiv + ÖKC bilgi fişi JSESXhtNp1E, Ingenico iDE280 ayarları, "ÖKC manuel tahsilat" Y6BMt0oxXm0) · [18] oynatma listesi PLwXAIImAJm3h4ou58OdlYW7aIvrWZzW-m, Guided Sales (oh4qCRJaqW4), Taksitli Satış webinarı (-icXui2UoLQ), Paylink (jkVU5933oo0).

**Özellikler:** Genel perakende: dokunmatik POS, kısayol tuşları, işlem askıya alma, tek fişte çoklu ödeme, Multinet/Pluxee, PAX/Verifone, taksitli satış, reyon fişi, uzaktan satış linki [9][10]; Finrota açık bankacılık [16]; e-Fatura/e-Arşiv/e-İrsaliye/e-Defter/YN ÖKC [2]; veri ambarı/Power BI; Mobil Mağaza (stok sorgu, mal kabul, transfer, çevrimdışı sayım, etiket, NFC ödeme); pazaryeri/omnichannel; indirim kampanyaları + Hopi/Zubizu/Tax Free; toplu mesaj/SMS; KVKK/ETK izinleri kasada. **Kuyuma özgü:** hurda, emanet, has cari, altın kuru fiyatlama, özel matrah (23/e), MASAK belgesi **bulunamadı**; RFID/terazi yok.

**İş akışı ayrıntıları:** İade: "Tümünü İade", "Yeniden Dök" menüleri; nakit iade ve kredi çekiyle iade ayrı akış. ÖKC arızasında manuel tahsilat → sonra V3 POS'ta fatura kaydı zorunlu. Mobil Mağaza: barkod → sepet → "Alışverişi Tamamla" → PAX temassız → e-Arşiv e-posta; "İndirim Kampanyası Uygula" düğmesi (25.10.1).

**TEYİTSİZ:** "Nebim Gold Çözüm Ortağı" kademe adı mı kuyum uzmanlığı mı; video açıklamaları okunamadı.

### 2.15 Logo Yazılım (Tiger / GO / Netsis)

**Kaynaklar (belge 9 · video 6):** [1] docs.logo.com.tr/pages/viewpage.action?pageId=50690625 "**Netsis Has Altın Uygulaması Desteği**" ve [2] PDF (04.03.2021; Netsis 3 Standard/Enterprise, Wings) — **docs.logo.com.tr bu ağdan çözülmedi; içerik arama özetlerinden, TEYİTSİZ** · [3] logohizmetmerkezi.com …/yeni-surum-2-83-00-00.html (06.03.2023: banka hesaplarında kıymetli maden — Altın/Gümüş/Platin/Paladyum döviz türü; video frxWdsyc_ds) · [4] …/logo-magaza-satis-ekrani.html (e-Fatura Tipi "Özel Matrah": tütün, altın, gümüş…; F9 → e-Fatura Bilgileri; KDV 0 ise muafiyet sebebi zorunlu) · [5] …/logo-tiger-3-ihracat-faturasi-menusu.html · [6] pageId=113246282 Start 3 2.85 malzeme kartı "Özel matrah uygulansın" (TEYİTSİZ) · [7] pageId=34214424 Ba-Bs (TEYİTSİZ) · [9] logo.com.tr iş ortaklığı (kuyumcu dikey çözümü bulunamadı) · [10] YouTube "Logo Destek" @LogoSupport (3.576 video): "Logo Netsis Altın/Gümüş Döviz Tipi Desteği" (3w9ZGiKWH4I), "Banka hesaplarında kıymetli maden işlemleri nasıl yapılır?" (frxWdsyc_ds), "Logo Netsis Gümüş Uygulaması Geliştirmeleri (9.0.55)" (QaxaSQFM3-c), "Mal Bazında KDV" (rqCaARX5me0) · muhasebetr.com/yazarlarimiz/omerselimkoralturk/010 (805 kodu; "işçilik satırı e-Arşiv kurallarıyla çelişir" tek yazar görüşü).

**Özellikler:** Netsis Has Altın: parametre "KDV Hesaplarında Has Altın Sistemi" → Fatura Modülü > Satış Parametreleri > **Fatura Ek 1**; Döviz Takibi'nde "Has Altın" döviz tipi; günlük has kuru kur girişinden; stok kartında çevrim + döviz tipi = Has Altın; işçilik KDV'si işçilik oranıyla faturaya [1][2]. Gümüş uygulaması (9.0.55) [10]. Tiger: banka hesabında kıymetli maden hareketleri (2.83) [3]; stokta gram/ayar takibi yok. e-Belge: Özel Matrah ve İstisna fatura tipleri; malzeme kartı "özel matrah uygulansın" [4][6]. MASAK, hurda, gider pusulası, emanet, etiket/RFID, terazi, sadakat, atölye: kuyuma özel belge yok. Kuyumcu programları (Terminal Gold, KuyumHub) Logo'ya fiş aktarır.

**İş akışı ayrıntıları:** Netsis: has sistemi açık → fatura satırında stok kartının döviz tipi Has Altın → tutar gram × günlük has kuru; ayar farkı işçiliğine KDV. Logo mağaza satış: cari kartı LogoConnect "e-Fatura Kullanıcısı" → F9 → Senaryo → e-Fatura Tipi "Özel Matrah" → kaydet [4].

### 2.16 Mikro Yazılım (mikro.com.tr)

**Kaynaklar (belge 7 · video ~20):** [1] mikro.com.tr (sektörler: Perakende, İnşaat, Kırtasiye, Kimya, Lojistik, Metal, Mobilya, Otomotiv — **kuyum yok**) · [2] /mikro-jump/ (ondalıklı/tartılı ürün, dövizli stok, banka ekstre, Online Hesabım, Patron Ekranı) · [3] /mikro-hizli-satis/ (barkod, etiket tasarımı, **terazi entegrasyonu**, Promosyon Yönetimi, YN yazarkasa, çevrimdışı) · [4] /altin-faturasinin-muhasebe-kaydi-nasil-yapilir/ (2018 blog; 950/951 fason altın, has→22 ayar çevrim 1.000×0,995/0,916) · [5] /mikro-musavir/ (kuyumcuda enflasyon muhasebesi SSS, TEYİTSİZ) · [6] akademikro.com.tr (9 kategori, kuyum yok) · [7] buluo.mikro.com.tr (JS, açılamadı) · [8] YouTube @MikroYazlm1 (e-Portal e-Fatura 14.10.2026 vXrFvMe7qEU; Hızlı Satış BrcZMu8m-30; Online Hesabım V17 banka serisi PKgMo98oMts, f-UuL7YpJDQ; eski eğitim FAmPHE-8FJI, E9WDBK_H_Yw, begabarCyNQ, vkZ1Vbrj2kI) — **kuyum/altın videosu yok**.

**Özellikler:** Kuyumcu modülü **yok**. Hızlı satış, iskonto katmanları, ÖKC, çevrimdışı, Promosyon, terazi, etiket [3]; dövizli stok (has "dövizli stok" olarak tanımlanabilir — Mikro belgesi yok, yorum) [2]; banka: başvuru formu → Online Hesabım → manuel/otomatik aktarım [8]; e-Belge tam set; Patron Ekranı mobil. Alış/hurda, emanet, MASAK, gider pusulası, altın cari, RFID: yok.

### 2.17 Hasfiyat (hasfiyat.com — Gaziantep)

**Kaynaklar (belge: SSS 18 + 3 rehber + sürüm notları 5 + haber 9 · video 49):** [1] hasfiyat.com · [2] /premium (Bronz 1.250 / Silver 3.250 / Gold 5.250 ₺ ay KDV dahil) · [3] /destek (18 SSS) · [4] **/surum-notlari** (29.09, 03.10, 07.10, 12.10 sürümleri) · [5] /kuyumcu-fatura-nasil-kesilir (4 adım) · [6] /kuyumcu-alis-satis-islemleri (4 adım) · [7] /altin-fiyatlarini-otomatik-guncelleme (formül) · [8] /uygulamalar (App Store id6754001228, Play com.hasfiyat, Windows, Android TV, LG webOS, Samsung Tizen) · [10] YouTube @hasfiyat UCzh633rl1oJxJNvnRfeulKg (**49 video**, 22'si listelendi): cLuKovMEyQE "Altın Fiyat Ekranı, Toplu e-Fatura ve Muhasebe Tek Panelde" (12:12, kurucu anlatımı) · TlLurSyLWu8 "Alış-Satış Modülü Nasıl Kullanılır?" (5:15) · rBapRfH1F7Y "Fiyat Ayarları ve Formüllü Fiyatlandırma" (5:58) · NLfrtwH7lQc "Fatura Nasıl Kesilir? Tek Tıkla Toplu e-Fatura" (1:39) · BC6gZqj9QwA "MASAK: Risk Paneli" (1:55) · cYCHDAJIEso "Banka Hesapları Yönetimi" (2:21) · rF-4pcCJIYs "e-Fatura Entegrasyonu Nasıl Kurulur?" (1:11) · qivs3iYckkk "e-Fatura/e-Arşiv Ayarları" (1:55) · 6ADiU0kFaOE "Gider Pusulası Ayarları" (1:33) · TycN8DSkbZI "Kesilen Pusulaları Görüntüleme" · jAF6Nh5q4yc "Fiyat Ekranı Tema Ayarları" · rN66CwzzGoI "QR Kod ile Hızlı Giriş" · Şifre, Abonelik, Web İstatistikleri, Özel Alan Adı, Geçmiş Kurlar, Bildirim Ayarları, Destek · [11] altinapi.hasfiyat.com (REST + WebSocket) · [12] Trustpilot 4,0/5.

**Özellikler:** Fiyat panosu ana ürün (Harem/Nadir/Hakan WebSocket; formüllü emtia; TV/Android TV/webOS/Tizen; tema) [3][7][8]. Alış-satış: gram+işçilik → canlı fiyatla tutar; termal fiş 58/80 mm; iskonto; açık bakiyeyi cariye aktarma (29.09) [2][6][4]. Cari: tek bakiye; emanet ve VKN takibi (29.09) [4]. Banka: **e-posta/PDF/Excel'den hareket okuma**, TC eşleşmesi, otomatik cari; "Telegram makbuz takibi" (12.10) [3][4]. e-Belge: tekli/**toplu** kesim; entegratörler GİB portal, EDM, Uyumsoft, İzibiz, SüperEntegratör, İşnet, QNB eSolutions, Hızlı Bilişim, ICE; e-Gider pusulası otomatik [5][10]. MASAK: risk paneli; eşik **185.000 → 370.000** (12.10 sürümü, 7 Ekim RG) [4]. Rapor: günlük analiz, "Kuyumcu Defteri" gün sonu (03.10) [4]. Donanım: barkod sayım, Zebra doğrudan baskı (03.10); RFID/terazi yok [4]. Mobil: iOS/Android/Windows/Web; QR ile giriş [8]. E-ticaret: WooCommerce, ikas, IdeaSoft, Ticimax'a fiyat gönderimi; özel alan adlı canlı fiyat sitesi [3]. İletişim: sipariş durumu SMS, ödeme/teslim hatırlatma, QR müşteri formu, OCR kimlik [3][10]. Personel: alt kullanıcı 3/10/sınırsız. Sadakat/atölye/muhasebe aktarımı/şube: yok.

**İş akışı ayrıntıları:** Fatura 4 adım (hesap+modül → müşteri+kalem → belge tipi+entegratör → kes). Banka → TC eşleşme → MASAK risk paneli → e-Fatura. Formül: gram × ayar milyemi × has + işçilik (sabit/oransal; sıfırsa satır yok) + kâr; yuvarlama formülde; alış/satış ayrı marj. 07.10 sürümü: Alış-Satış ekranına **günlük SMS ekran kilidi**, klavye gezinimi, **fiyat teklif fişi**, kasa↔stok↔cari transfer. 12.10: toplu kasa girişi, bölünmüş ödeme.

**TEYİTSİZ:** 27 video yüklenmedi; F tuşu yok; haber URL'leri yok.

### 2.18 KuyumSmart (kuyumsmart.com.tr — Kuyumcukent)

**Kaynaklar (belge: SSS **81** + rehber 16 + 4 video sayfası + sözlük · video 18):** [1] kuyumsmart.com.tr · [2] /ozellikler · [3] /sss (81 soru, 11 bölüm) · [4] /rehber (16: /rehber/kuyumcuda-masak-kimlik-tespiti, hurda-altin-nasil-hesaplanir, kuyumcuda-gider-pusulasi, kuyumcu-barkod-sistemi-nasil-kurulur, excelden-kuyumcu-programina-gecis, kmts-nedir…) · [5] /fiyatlandirma ($100+KDV/mağaza/ay; yıllık $1.100), /donanim (Zebra ZD621R $1.350, Chainway C5 $1.100, UHF etiket $340/2.000, NETUM C750 $70), /qr-urun-katalogu, /ai-studio · [6] /videolar/* (bölüm zaman damgalı) · [7] YouTube @kuyumsmart (18 video): 6AwJyp53kzU "Barkodla Canlı Altın Fiyatı" (1:44; bölümler 0:00 barkod, 0:28 fiyat, 1:00 satış+stok, 1:12 e-fatura) · qMryJt_8DRM "RFID Stok Sayımı" (2:05; 0:15 tanımlı/tanımsız, 0:29 sayım başlat, 1:10 rapor, 1:31 eksik/fazla) · mqHWI2ULMK4 "RFID Etiketi Nasıl Basılır?" (0:45) · 9AexWL03Bw4 "AI Studio" (4:14) · 14 Shorts (E-Fatura tek akış, Satış/Stok raporları, Çok şube, Canlı kur, RFID, Program değiştir veri kaybetme…).

**Özellikler:** Satış: takas/karma tek fiş; "verilenler/alınanlar" iki taraflı fiş; kart taksiti POS tanımına kadar, komisyon ve brüt/net ayrı [2][3]. Alış/hurda: ayar başına alış/satış milyemi; önerilen fiyat; **pazarlık payı aşılırsa yönetici onayı** [3-#34]. Emanet: gram+ayar cariye bağlı [3-#46]. Tamir: kayıt → atölye → teslim [3-#45]. Cari: TL/döviz/has altın/has gümüş; yaşlandırma 0-30/31-60/61-90/90+; risk limiti uyarısı [2]. Banka: entegrasyon yok [2]. e-Belge: alıcıya göre tip otomatik; **805/808/230 otomatik**; gider pusulası fişten; kontör [3-#47–49]. MASAK: **370.000** satış ekranında; NFC/OCR; yasaklı liste + PEP; bağlantılı işlem; **eşik aşımında kimlik+imza olmadan fiş kapanmaz; liste eşleşmesinde yönetici onayı, liste adı satıcıya gösterilmez; şüpheli işlem notu yalnız sahibe**; 8 yıl [3-#50–58]. Rapor: şube/personel/dönem/kategori/metal; has gram veya adet; yıl-önceki yıl; "Varlıklarım" [2][3]. Donanım: **kamera ile QR/barkod**; RFID 5–15 dk; terazi yok [3]. Mobil: PWA [3-#13]. E-ticaret: QR ürün sayfası, online katalog, **kiosk modu**, kendi alan adında canlı fiyat sitesi; sepet yok [2][3]. Personel: 3 rol; personel maliyet/kâr görmez; **hesap başına tek oturum** [2]. Kasa: gün açılış/kapanış aynı sayım; nakit/sarrafiye/hurda/gram altın/gümüş kalem kalem; **boş satır = sayılmadı; kasa eksiye düşemez**; silme yok → iptal [3-#39–41]. Şube: transfer; depo da mağaza ($100/ay). Muhasebe: Excel/ekstre. Fiyat panosu: Harem; 14 sarrafiye; tam ekran. Ek: geri dönüşüm kutusu 30 gün; AI Studio 100 kredi/$20. Sadakat/SMS: yok.

**İş akışı ayrıntıları:** Fiyat formülü açık: (işçilik milyemi + ayar milyemi) × net gram × has kuru × (1+kâr); örnek 10 g 22 ayar, 60 milyem, 6.900 ₺, %6 → 71.384,64 ₺ [3-#19]. **Kur gelmezse satış ekranı uyarır, fiyat elle girilir** [3-#25]. Satış kaydı değiştirilemez; iptal ters kayıt [3-#58]. Kâr marjı kategori bazında, ürün bazında ezilebilir.

### 2.19 Ariyen (ariyen.net · ariyenkuyumcu.com — Codeyen)

**Kaynaklar (belge: blog 16 + 7 + rehber 6 + hesaplayıcı 7 · video 5):** [1] ariyen.net (Core, Mobile, CMD, Pano, Web, Muhasebe, Shop) · [2] /sistem · [3] /fiyatlar (rakam yok; 10 gün deneme) · [4] /blog (…/kuyumcuda-gise-kasasi-ve-personel-zimmeti, …/rfid-ile-kuyumcu-stok-sayimi, …/masak-yukumlulukleri-kuyumcu-kimlik-tespiti, …/has-altin-kasa-takibi-tl-degil-gram…) · [5] /altin-hesaplama (7 hesaplayıcı: çeyrek, has-milyem, işçilik, kâr-zarar, döviz-altın, **zekât**, gümüş) · [6] ariyenkuyumcu.com blog (7) · [7] status.ariyen.net · [8] YouTube @codeyencom (5 video): TC3wQs07YY0 "RFID, Masaüstü, Mobile ve Web" (1:56) + 4 Shorts (CMD Stok Sayımı 4Jmio73ykcU…).

**Özellikler:** Core (Windows): alım-satım, sarrafiye, hurda, döviz, gümüş; barkodsuz gram ürün; canlı fiyatla anlık maliyet/kâr; fiyat 15 sn, geri sayım görünür [2]. Cari + emanet; **gişe kasası ve personel zimmeti** [1][2][4]. Banka: **10 banka** otomatik; cari eşleme; para gelince mobil bildirim [2]. e-Belge: BirFatura ve Paraşüt; toplu fatura, fatura birleştirme; gider pusulası imzalı PDF + QR (mobilden); gelen fatura listesi; faturada satış kanalı [2]. MASAK: liste sorgusu, ŞİB formu [2]. Rapor: Web'de TL ve has kâr, şube kıyas. Donanım: CMD Android el terminali RFID; örnek sayım 1.203 üründe 1.184 okundu, 0:46 [2]. Mobil: iOS + Android APK; PIN. E-ticaret: Ariyen Shop (kendi alan adı, canlı kur × marj, PayTR/iyzico). Personel: rol, cihaz lisansı, **2FA**. Pano: Android TV. Şube: Pro/Full. Sadakat/SMS/atölye/terazi: yok.

**İş akışı ayrıntıları:** **Gişe zimmeti 7 adım:** sabah yönetici sarrafiye adet + ürün parça + açılış nakdi zimmetler → personel telefondan onaylar → gün içi takviye talebi (kendi talebini onaylayamaz) → satışta ürün zimmetten düşer, nakit/hurda gişeye girer → akşam sayım beklenen vs sayılan, kalem bazlı fark → onaylı gişe ana kasaya → vardiya devri sayımla; iptal: ürün gişeye döner. Ekran adı: Mobile "**Gişe Kasam**" [4]. RFID sayım 4 adım (terminalde vitrin/kasa/şube seç → gezdir → eksik/fazla/yer değişmiş → kamerayla incele, onayla). Kurulum: kayıt → 1 dk'da deneme → Core kur → 1 saat uzaktan eğitim, ilk satış birlikte.

**TEYİTSİZ:** fiyat; deneme süresi (10 vs 60 gün); banka adları.

### 2.20 Kuyum Plus (kuyumplus.com — Konasoft A.Ş.)

**Kaynaklar (belge 12 modül sayfası; blog 0 "hazırlanıyor"; kılavuz yok · video 9 (≤10 sn klip + Shorts)):** [1] kuyumplus.com · [2] /kuyumplus/stok-takibi (Hızlı İşlem), /perakende-modul, /toptan-modul, /satis-yonetimi, /barkodlama-modul, /egitim (Sayım/RFID), /cari-modul, /masraf-modul, /efatura, /raporlama, /teknik-destek (Tamir), /envanter-modul (WhatsApp) — slug'lar içerikle uyuşmuyor · [3] /cihazlar (RK02 RFID etiket, Zebra ZD621R, Chainway C5, **Pavo N86 Android ÖKC**) · [6] App Store id6801691502 (v1.2.0 03.10), Play com.konasoft.kuyumplus · [7] YouTube @KuyumPlus (9: "Yetkileri Belirleyin", "Tamir Takibi", MASAK Shorts…).

**Özellikler:** Satış: dokunmatik hızlı satış; çoklu sepet; bölünmüş/taksitli; dövizli; 58/80 mm termal [2]. Hızlı İşlem: döviz ve ziynet tek ekran; ağırlıklı ortalama maliyet. Toptan: "açık bağlama". Emanet cariye bağlı; dövizli ekstre; geciken vade hatırlatıcı; risk limiti; **Ayarlar > Otomasyon** ekstre e-postası planlama [2]. e-Belge: satış/tahsilat/iade sonrası otomatik; onaylı veya tam otomatik mod; entegratör adı yok. MASAK/KVKK: "uyumlu" ifadesi. Donanım: RFID + ÖKC; terazi yok. Mobil: iOS/Android. **WhatsApp modülü:** sipariş/tamir durum, hazır/teslim, satış-ödeme bildirimleri; gelen mesajlar ekrana düşer; gün sonu özeti WhatsApp/e-posta [2]. Tamir: atölye yönlendirme, aşama, tahmini teslim tarihi otomatik. Kasa: TL/USD/EUR/altın aynı kasada; gişe. Şube/depo/gişe. E-ticaret/sadakat/muhasebe/pano: yok.

**İş akışı ayrıntıları:** Hızlı İşlem 8 adım (tip → döviz/ziynet → tutar → nakit/havale → müşteri bağla → onay → rapor → gün sonu özet); Perakende 6 adım; WhatsApp 6 adım (telefon → kayıt → otomatik mesaj → güncellemeler → teslim → yanıtlar ekrana).

### 2.21 Kuyumcum (kuyumcum.net — Bambu Teknoloji)

**Kaynaklar (belge: SSS 11 + 10 modül + App Store sürüm notları 4 · video 1):** [1] kuyumcum.net · [2] /TR/Moduller/ · [3] /TR/Destek/ · [4] /TR/Fiyatlar/ (Standart $30 / Barkodlu $35 / RFID'li $40 kullanıcı/ay) · [5] App Store id1474947143 (2.6.4 25.09 … 2.6.7), Play tr.com.pardosoft.goldminer · [7] YouTube Tc79KWBK6nI "Kuyumcum Nedir?" (kanal sayfası @KuvveTeknoloji'ye yönlendi — TEYİTSİZ).

**Özellikler:** Perakende: tek/çok ürün, alış+satış aynı işlem; müşteriyi döviz ya da ziynet üzerinden borçlandırma; **İşlem Bekletme**; Hızlı İşlem [2]. Cari: hesap dönüştürme; **fişte işlem öncesi/sonrası bakiye** (2.6.6) [5]. Toptan: atölye/çantacı. e-Fatura: "Otomatik Fatura sayfası" (2.6.7). Rapor: pozisyon, K/Z, bilanço, **silinen kayıtlar raporu**; masraflar has altın olarak. Donanım: barkod + RFID (10–20 dk); sayım Excel'i 3 sayfa (2.6.7); Zebra/Netum/Sewoo. Mobil: **yalnız iPad ve 7,2"+ Android tablet** [3]. 180 para birimi. Şube: merkezi + transfer. MASAK/e-ticaret/SMS/atölye/pano/terazi: yok.

**İş akışı ayrıntıları:** Ekranlar (görsel dosya adları): Perakende işlem 1-2-3, Perakende işlem ödeme, İşlem detay, Bekleyen işlemler, Hızlı işlem satış/alış. 21 gün deneme.

### 2.22 ZarifSoft (zarifsoft.com — Ankara)

**Kaynaklar (belge: sayfa içi SSS + /egitim/ + /urunler/ · video 2):** [1] zarifsoft.com · [2] /urunler/ (ribonlu etiket yazıcı, 2D okuyucu, İDATA P1 tablet) · [3] /egitim/ (1 eğitim videosu: stok/ürün giriş, satış-kasa, kur-fiyat, rapor-cari) · [5] YouTube @zarifsoft: 9K3QJAZGmBM "**ZARİFSOFT EĞİTİM VİDEOSU**" (**20:59**, açıklama boş), -kvlb2WT7KU tanıtım (2:05).

**Özellikler:** Anlık kur (çoklu kaynak, manuel); gram bazlı stok 14/18/22, QR/barkod, kritik stok uyarısı; satış/kasa, cari, e-Fatura/e-Arşiv, çoklu ödeme, dövizli satış; vitrin TV ekranı; bulut, mobil uyumlu; termal fiş yazıcı, dijital fiyat tabelası [1][2]. Hurda, emanet, banka, MASAK, RFID, terazi, şube, personel, WhatsApp, atölye, e-ticaret, muhasebe: yok. 8 gün ücretsiz.

### 2.23 KuyumBase (kuyumbase.com)

**Kaynaklar (belge: SSS 6 + özellikler + blog 52 · video 0):** [1] kuyumbase.com, /ozellikler · [2] /sss · [3] /blog (52; çoğu tüketici içeriği) · [4] /fiyatlandirma (İşletme / E-Ticaret; rakam yok; 14 gün) · [5] App Store id6762566833, Play com.kuyumbase.app.

**Özellikler:** POS: çoklu ödeme, **PayTR, iyzico, Garanti BBVA, Vakıfbank sanal POS**, QR ödeme linki; hurda ve takas; seri no/ayar stok; CRM segmentasyon; **Paraşüt çift yönlü** (e-Fatura/e-Arşiv Paraşüt üzerinden); **KMTS** (bandrol, "Nisan 2026" ifadesi güncel değil), TCKN KYC, biyometrik; fiyat 15 sn, **fiyat kilitleme**, mağazaya özel marj, fiyat geçmişi grafiği; online mağaza + sayfa oluşturucu + 9 koşullu indirim motoru + Geliver/Basit Kargo etiketi; mobil **çevrimdışı mod**; 3/10 kullanıcı [1][2][4]. RFID, terazi, pano, emanet, banka, SMS, atölye, şube: yok. Adım/ekran bilgisi yok.

### 2.24 Gramino ERP (graminoerp.com.tr)

**Kaynaklar:** [1] tek sayfa (11 modül; SSS 7 soru, cevapsız); Instagram'a yönlendirme; video/blog/kılavuz/mağaza **yok**.

**Özellikler:** Perakende: alış/satış/açık hesap/tahsilat tek ekran; ürün kartları nakit/takı/gram altın/hurda/bilezik ayrı, **22 ayar bilezik ayrı panel**; emanet & cari (piyasa değeriyle bakiye); Günlük İcmal (kasa + POS/Visa denetim); İşlem Geçmişi düzenle/iptal/ters kayıt; **personel başına satılan gram**; CRM son işlem; rol bazlı kullanıcı; web [1]. Fatura, MASAK, barkod, mobil, e-ticaret, şube: yok. Olgunluk TEYİTSİZ.

### 2.25 Kısa ek: Bakiyem · Dijimo · Kuyumex · Meadify

- **Bakiyem** (bakiyem.net; SSS 23 https://bakiyem.net/tr/sss, blog 3, YouTube yok): **Triple-Book** (has / nakit / değer paralel defter), atölye kartında hedef-gerçekleşen ağırlıkla fire, saha personeli/çantacı, onay paneli, NISABA AI (yakında); fiyat Deneme 0 / Esnaf $9,99 / Kuyumcu $29,99 / İşletme $59,99 / Kurumsal $99,99 ay (sayfada ikinci rakam seti, hangisi geçerli belirsiz); TR/EN; CSV.
- **Dijimo** (dijimo.com.tr/cozumler/kuyumcu-programi.html; blog/SSS/YouTube yok): has cari, hurda, pırlanta sertifika, barkod/RFID; **WhatsApp + Instagram DM + web formu tek gelen kutusu**, özel sipariş **kaporadan teslime pipeline**, Meta onaylı "sipariş hazır" şablonu, segment hedefli bayram kampanyası, AI asistan (niyet/aciliyet); deneme 14/7 gün (çelişkili).
- **Kuyumex / Kuyumsoft** (kuyumsoft.com): döviz işlemine anında e-Belge (mysoft hesabı); kasaya alış/kasadan satış/hesapta alış-satış; **silinen/değiştirilen işlemin ilk hâli ve zamanı görüntülenir**; blog/SSS/YouTube yok.
- **Meadify:** bulunamadı (TEYİTSİZ / muhtemelen yanlış ad).
- **sarraf.io** (ZNOSoft): banka entegrasyonu, barkodlu perakende, pazaryeri "çok yakında" — SarrafPro ile bağı yok.

---

## 3. Büyük karşılaştırma tablosu

Sütun kısaltmaları: **MH** ByGatech · **PU** Pusula/PusulaX(+Kivi) · **DZ** Dezy · **KH** KuyumHub · **CS** CetaSoft · **ML** M&L · **MP** Milyem Plus · **SN** Softnorm · **SP** SarrafPro · **EY** E-Yaz Milyem/Expert · **TG** Terminal Gold · **HF** Hasfiyat · **KS** KuyumSmart · **AR** Ariyen · **KP** Kuyum Plus · **KM** Kuyumcum · **KB** KuyumBase · **ERP** Nebim/Logo/Mikro (en iyisi). Değerler üretici beyanıdır; ByGatech sütunu önceki belgeler + DURUM defterinden.

### 3.1 Eğitim ve belge

| Özellik | MH | PU | DZ | KH | CS | ML | MP | SN | SP | EY | TG | HF | KS | AR | KP | KM | KB | ERP |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Ekran başına kısa eğitim videosu | ❌ (demo seslendirme hazırlanıyor) | ✅ 27 | ❌ | ❌ | 🟡 2 | ✅ 88 | ❌ | ✅ 12 | ❌ (3. taraf) | ✅ 34+ | ❌ | ✅ 49 | ✅ 4+14 | 🟡 1+4 | ❌ klip | 🟡 1 | ❌ | ✅ (POS, 3. taraf) |
| Yazılı kullanım kılavuzu (çevrimiçi) | ✅ Kılavuz ekranı | ❌ | ❌ | ❌ | ❌ CD | ❌ | ❌ | ❌ | ✅ ~60 sayfa | ❌ | ❌ | 🟡 3 rehber | 🟡 16 rehber | 🟡 blog | ❌ | ❌ | ❌ | ✅ (Logo docs, erişilemedi) |
| SSS (soru sayısı) | ? | 3 | 33 | 🟡 | ❌ | ❌ | 🟡 | 🟡 | 60+ yanıtsız | ❌ | ❌ | 18 | **81** | ❌ | 🟡 | 11 | 6 | ✅ |
| Sürüm notları herkese açık | ❌ | ❌ | 🟡 App Store | ❌ | ❌ | 🟡 haber | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ haftalık | ❌ | ❌ | 🟡 App Store | ✅ App Store | ❌ | ✅ PDF |
| Kısayol tuşları yayımlı | 🟡 F2/F4/F8/F9 (kodda; belge?) | ❌ | ❌ "var" | ❌ | ✅ F7/F8 | ❌ | ❌ | 🟡 30 dk video | 🟡 kısayol kaydet | ❌ | ✅ F2/F3/F4/F12 | 🟡 klavye gezinimi | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Fiyat herkese açık | — | ✅ | ❌ | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ |

### 3.2 Satış / tezgâh

| Özellik | MH | PU | DZ | KH | CS | ML | MP | SN | SP | EY | TG | HF | KS | AR | KP | KM | KB | ERP |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Karma ödeme tek fiş (nakit+kart+altın+hurda+veresiye) | ✅ | ✅ | ✅ | ? | ✅ F7 M/N/K | ✅ iPad | ? | ✅ | ❌ | ✅ | ✅ | 🟡 bölünmüş | ✅ | ✅ | ✅ | ✅ | ✅ | 🟡 (altın yok) |
| Alış+satış (taklas/KARMA) aynı fiş | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ? | ✅ | ❌ | ✅ | ✅ F4 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| İşlem bekletme | ✅ | ? | ? | ? | ✅ çoklu föy | ? | ? | ? | ❌ | ? | ? | ? | ? | ? | 🟡 çoklu sepet | ✅ | ? | ✅ askıya al |
| Kur/fiyat kilidi | ✅ | ? | ✅ | ? | ? | ? | ? | ? | ? | ? | 🟡 manuel | ? | ? | ? | ? | ? | ✅ | ❌ |
| Eşikli onay (büyük iskonto/zararına/yüksek tutar) | ✅ | ? | ✅ push | ✅ bildirim | ? | ? | ? | ? | 🟡 AI durdurma | ✅ zararına izin | ? | ? | ✅ hurda pazarlık | ? | ? | ? | ? | ? |
| Kullanıcı başına azami iskonto | ✅ | ? | ? | ? | ? | ? | ? | ? | ? | ✅ | ? | ? | ? | ? | ? | ? | ? | ✅ |
| Kendi taksitli/senetli satışı | ✅ | 🟡 | ✅ | ? | ✅ vade | ? | ? | ? | ❌ | ✅ çek/senet | ✅ | ? | ? | ? | ✅ | ? | ? | ✅ |
| Termal fiş 58/80 mm | ✅ | ? | ✅ | ? | ? | ✅ iPad fiş | ? | ✅ | ❌ | ✅ bilgi fişi | ✅ | ✅ | ? | ? | ✅ | ? | ? | ✅ |
| Fiyat teklif fişi | ❌ | ? | ? | ? | ? | ? | ? | ? | ? | ✅ Teklif modülü | ? | ✅ | ? | ? | ? | ? | ? | ✅ |
| Kur gelmezse satış ekranı uyarır | ✅ (3 dk tazelik kapısı) | ? | ? | ? | ? | ? | ? | ? | ? | ? | 🟡 5 dk önbellek | ? | ✅ | ✅ geri sayım | ? | ? | ? | — |
| Satırda "zarar" etiketi | 🟡 onay | ? | ✅ | ? | ? | ? | ? | ? | ? | ✅ | ✅ rapor | ? | ? | ? | ? | ? | ? | ? |
| Kamerayla barkod (telefon) | ❌ | 🟡 PusulaApp | ✅ | ? | ? | ? | ? | ? | ? | ✅ Mini | ✅ | ? | ✅ | ? | ✅ | ? | ? | ✅ |

### 3.3 Alış / hurda / atölye

| Özellik | MH | PU | DZ | KH | CS | ML | MP | SN | SP | EY | TG | HF | KS | AR | KP | KM | KB | ERP |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Hurda alış + has hesabı + gider pusulası | ✅ | ✅ (GP ?) | ✅ | ✅ 7 gün | ✅ | ✅ | ✅ | ✅ Pro+ | ✅ bankadan | ✅ | 🟡 | ✅ e-GP | ✅ | ✅ PDF+QR | ✅ | 🟡 | ✅ | ❌ |
| Hurda fotoğrafı | ✅ | ? | ✅ | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? | ❌ |
| Hurdadan vitrine / barkodludan hurdaya | ❌ (D1-08 K-13) | ✅ | ✅ iade→hurda | 🟡 fire | ✅ hurdalaştırma | ✅ dönüş | ? | ✅ dönüşüm | ❌ | ? | 🟡 ayrıştırma | ? | ? | ? | ✅ külçe döngüsü | ? | ? | ❌ |
| Eritme/fire/takoz/ramat | ❌ (K-13) | ✅ üretim | ✅ | 🟡 | ✅ | ✅ | ? | ? | ❌ | ✅ üretim | ? | ? | ? | ? | ? | ? | ? | ❌ |
| Çok ayarlı ürün tek barkod (sarrafiyeli ürün) | 🟡 set | ? | ? | ? | ✅ video | ? | ? | ? | ❌ | ✅ set | ? | ? | ? | ? | ? | ? | ? | ❌ |
| Atölye iş emri (aşama, usta, fire) | ❌ (H-5) | ✅ üretim prog. | ✅ | ❌ rol | 🟡 tamir | ✅ bölümler | ❌ | ❌ | ❌ | ✅ | ✅ | ❌ | 🟡 tamir | ❌ | 🟡 tamir | 🟡 çantacı | ❌ | ❌ |
| Tamir takibi + müşteriye mesaj | 🟡 sipariş var, mesaj yok | ? | ✅ | ? | ✅ kart | ✅ | ? | ? | ❌ | ✅ durum tanımı | ✅ SMS/WA | ✅ SMS | ✅ | ? | ✅ WA | ? | ? | ❌ |

### 3.4 Stok / etiket / donanım

| Özellik | MH | PU | DZ | KH | CS | ML | MP | SN | SP | EY | TG | HF | KS | AR | KP | KM | KB | ERP |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Etiket tasarımcısı + QR + ZPL/Zebra | ✅ | ✅ Zebra | ✅ | ? | ✅ | ✅ Zebra | ❌ | ✅ Zebra | ❌ | ✅ | ✅ | ✅ Zebra | ✅ ZD621R | ✅ | ✅ | ✅ | 🟡 | ✅ |
| RFID sayım | ❌ | ✅ | 🟡 yakında | ? | ✅ | ✅ Ages | ❌ blog | ✅ 5 dk | ❌ | ✅ | ✅ | ❌ | ✅ | ✅ CMD | ✅ | ✅ | ❌ | ❌ |
| Terazi bağlantısı | ❌ | ❌ | 🟡 ad var | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 🟡 genel | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ Mikro |
| PVC sertifika / garanti kartı | ✅ belge (kart yazıcı ?) | ? | ✅ QR doğrulama | ? | ✅ | ✅ Evolis | ❌ | ? | ❌ | ✅ | ✅ | ? | ? | ? | ? | ? | ? | ❌ |
| Stok durumları (yolda/rezerve/kayıp/tamirde) | 🟡 transfer var, "yolda" ? | ? | ✅ rezerve | ✅ transit | ? | ? | ? | ? | ? | ? | ✅ 11 durum | ? | ? | ? | ? | ? | ? | ✅ |
| Eşik altı / hareketsiz stok bildirimi | ✅ | ? | ✅ | ✅ | ✅ | ? | ? | ? | ❌ | ? | ✅ | ? | ? | ? | ? | ? | ? | ✅ |
| Ürün fotoğrafı / AI görsel | 🟡 foto var, AI yok | ? | ✅ AI | ? | ✅ | ✅ Ministudyo | ❌ | ✅ Konftel | ❌ | ✅ teşhir | ? | ? | ✅ AI Studio | ? | ? | ? | ✅ blog | ❌ |
| ÖKC / yazarkasa | ❌ | ? | ? | ? | ✅ | ? | ❌ | ❌ | ❌ | 🟡 genel | ✅ fiş | ❌ | ❌ | ❌ | ✅ Pavo N86 | ❌ | ❌ | ✅ |
| Rakip programdan veri göçü | 🟡 akıllı içe aktarım | ? | ✅ | ✅ ajan | ? | ? | ? | ? | ✅ konnektör | ? | ✅ ücretsiz 3–7 gün | ? | ✅ "veri kaybetme" | ✅ | ? | ? | ? | — |

### 3.5 Cari / kasa / banka

| Özellik | MH | PU | DZ | KH | CS | ML | MP | SN | SP | EY | TG | HF | KS | AR | KP | KM | KB | ERP |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Has/gram cari + TL/döviz | ✅ | ✅ | ✅ | ✅ | ✅ 4 kur | ✅ | 🟡 | ✅ | 🟡 | ✅ | ✅ | 🟡 tek | ✅ | ✅ | ✅ | ✅ | ? | ❌ (Netsis has döviz) |
| Emanet defteri | ✅ | ? | ✅ | ? | ✅ tablo | ? | ? | ✅ ekran | ❌ | ? | 🟡 | 🟡 | ✅ | ✅ | ✅ | ? | ❌ | ❌ |
| Veresiyenin altınla kapanması, fazlası emanete | ✅ | ? | ✅ | ? | ✅ "MAL İLE" | ? | ? | ? | ❌ | ✅ | ? | ? | ? | ? | ? | ? | ? | ❌ |
| Hesap dönüştürme (TL↔has↔döviz) | ✅ | ? | ? | ? | ? | ? | ? | ? | ❌ | ? | ? | ? | ? | ? | ? | ✅ | ? | ❌ |
| Fişte işlem öncesi/sonrası bakiye | ? | ? | ? | ? | ✅ kalan | ? | ? | ? | ❌ | ? | ? | ? | ? | ? | ? | ✅ 2.6.6 | ? | ✅ |
| Mutabakat formu | ✅ | ? | ? | ? | ✅ toptan | ? | ? | ? | ✅ rapor | ? | ✅ | ? | ? | ? | ? | ? | ? | ✅ e-Mutabakat |
| Risk/kara liste | ✅ | 🟡 PusulaX | ✅ | ? | ? | ? | ? | ? | ✅ sicil | ? | ✅ limit | ? | ✅ limit | ? | ✅ limit | ? | ? | ✅ |
| Kör kasa sayımı + çalışan kasası | ✅ | ? | ✅ | 🟡 vardiya | ✅ vezne | 🟡 atölye kasası | ❌ | 🟡 | ❌ | ✅ kasa | ✅ açılış/kapanış | 🟡 Defter | ✅ | ✅ gişe | ✅ gişe | ? | ❌ | ✅ |
| **Sabah zimmet → akşam sayım → ana kasaya devir (gişe)** | 🟡 çalışan kasası var, zimmet onayı yok | ? | 🟡 devir onaylı | ? | ✅ tezgâhtar başına mal+nakit | ? | ❌ | ❌ | ❌ | ? | ? | ❌ | ❌ | ✅ 7 adım | 🟡 | ? | ❌ | ❌ |
| **Günlük servet özeti (TL+has, dünle fark)** | 🟡 patron paneli/altın pozisyonu; dünle fark ? | 🟡 açık pozisyon | ✅ | 🟡 | ✅ öz sermaye geçmiş tarih | ✅ Web Rapor | ❌ | ✅ özsermaye | ✅ gecelik bakiye | ✅ has bilanço | ✅ net pozisyon | ❌ | ✅ Varlıklarım | ✅ | ? | ✅ pozisyon | ❌ | ❌ |
| Banka canlı bağlantı | 🟡 8 bağlayıcı (canlıda denenmedi) | ✅ Kivi 6 | ✅ 30 | ✅ 28 | ❌ | ❌ (vaat) | ✅ 20+ | ✅ Pro+ QNB | ✅ 35 | ❌ | ✅ 19 | 🟡 e-posta/PDF | ❌ | ✅ 10 | ❌ | 🟡 | ❌ | ✅ Finrota/Mikro |
| Banka PDF/e-posta ekstresi okuma | ❌ (CSV var) | ? | ✅ PDF | ? | ❌ | ❌ | ❌ | ? | 🟡 Excel | ❌ | ? | ✅ | ❌ | ? | ❌ | ? | ❌ | ✅ ekstre |
| Havaleden müşteri bul/aç + borçtan düş | ✅ | ✅ Kivi | ✅ | ✅ | ❌ | ❌ | ✅ %99,2 skor | ✅ | ✅ AI + mahsup | ❌ | ✅ kuyruk | ✅ TC | ❌ | ✅ | ❌ | ? | ❌ | 🟡 |
| Para gelince bildirim | 🟡 uygulama içi | ✅ PARS | ✅ | ✅ | ❌ | ❌ | ? | ? | ✅ WA | ❌ | ✅ | ? | ❌ | ✅ mobil | ? | ? | ❌ | ❌ |
| Sanal POS / ödeme linki | ❌ | ✅ Kivi CepPOS | ? | ? | ❌ | ❌ | ❌ | ❌ | ✅ POS | ❌ | ✅ iyzico/PayU/Param | ❌ | ❌ | ✅ Shop | ❌ | ❌ | ✅ 4 altyapı | ✅ Paylink |

### 3.6 Fatura / muhasebe / uyum

| Özellik | MH | PU | DZ | KH | CS | ML | MP | SN | SP | EY | TG | HF | KS | AR | KP | KM | KB | ERP |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| e-Fatura/e-Arşiv gönderimi | 🟡 Nilvera katmanı, test hesabı yok | ✅ Kivi | ✅ dahil | ✅ kendi entegratörü | ✅ TÜRKKEP | 🟡 vaat | ✅ | ✅ QNB | ✅ | ✅ İzibiz | ✅ | ✅ 9 entegratör | ✅ | ✅ BirFatura/Paraşüt | ✅ | 🟡 | ✅ Paraşüt | ✅ |
| Özel matrah 805/808/230 otomatik | 🟡 motor var, kod K-10 teyit | ✅ Kivi | ✅ 805 | ✅ | 🟡 işçilik KDV | ? | ✅ senaryo | ? | ✅ | ? | ✅ | ? | ✅ | ? | ? | ? | ? | 🟡 tip var, kod ? |
| Gelen e-fatura kutusu | ❌ | ? | ? | ? | ? | ? | ? | ✅ | 🟡 | ✅ | ? | ? | ? | ✅ liste | ? | ? | 🟡 Paraşüt | ✅ |
| e-İrsaliye / e-Gider Pusulası / e-Defter | ❌ | ✅ Kivi e-İrs. | ❌ yol haritası | ✅ e-GP | 🟡 | 🟡 ad | ? | ✅ hepsi | ? | ✅ hepsi | ✅ e-İrs. | ✅ e-GP | ? | ? | ? | ? | ? | ✅ |
| Toplu fatura (çok havale → fatura) | ✅ | ✅ Kivi | ✅ | ? | ❌ | ❌ | ✅ | ? | ✅ Excel | ❌ | ✅ | ✅ | ? | ✅ | ? | ? | ? | ✅ |
| Fatura PDF/yazdır + fatura şablonu | ✅ UBL-TR→HTML | ? | ✅ | ? | ✅ | ? | ? | ? | ✅ | ✅ | ✅ | ? | ? | ? | ? | ? | ? | ✅ |
| Has fiyat kaynağı seçimi (TCMB/BİST/Harem…) | 🟡 Harem izni bekliyor; kaynak seçimi ? | ✅ Kivi 4 kaynak | ✅ TCMB+piyasa | ✅ IAB/BIST/Kuyumcukent | ✅ 3 kaynak | ? | ? | ? | ✅ TCMB/BİST/5 kaynak | ❌ elle | 🟡 | ✅ 3 kaynak | ✅ Harem | ✅ | ? | ✅ | ✅ | ❌ |
| Mali müşavir salt okunur hesabı | ✅ | ? | ? | ✅ portal | ❌ | ❌ | ✅ aynı panel | ? | ✅ | ? | ? | ? | ❌ | ✅ Muhasebe | ? | ? | ? | — |
| Luca/Zirve/Logo/Mikro aktarımı | ✅ Luca fiş CSV + Zirve xlsx; Logo/Mikro ❌ | ❌ | 🟡 Excel/XML | ✅ Luca/Logo (biçim ?) | ❌ | ❌ | 🟡 | ❌ | ✅ Zirve/Mikro/ERPGOLD konnektör | ✅ T-Set/KOBİERP | ✅ Logo/Mikro/ETA/Netsis/Luca | ❌ | 🟡 Excel | 🟡 Paraşüt | ❌ | ❌ | ✅ Paraşüt | — |
| Hesap planı eşleştirme ekranı | 🟡 600/391 yapılandırmadan | ❌ | ❌ | ? | ❌ | ❌ | ? | ❌ | ✅ Mahsup Excel kural | ✅ | ✅ bir kez | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | — |
| MASAK eşik (güncel 370.000) | ✅ tarihli | ❌ 185.000 (Kivi) | ✅ form (1.3.7) | ❌ 185.000 | ❌ yok | ❌ (vaat) | ✅ sayaç | 🟡 yaptırım | ✅ 200 bin/2 Mn/20 Mn notları | ❌ | ✅ 370.000 | ✅ 370.000 | ✅ 370.000 | ✅ | 🟡 "uyumlu" | ❌ | 🟡 KMTS | ❌ |
| NFC/OCR kimlik okuma | ❌ (D3-02) | ✅ Kivi | ? | ✅ | ❌ | ❌ | ? | ❌ | ✅ + WhatsApp görsel | ❌ | 🟡 KYC form | ✅ OCR | ✅ | ? | ? | ❌ | ✅ biyometrik | ❌ |
| Yaptırım listesi günlük + ŞİB hazırlık | ✅ + 10 iş günü | 🟡 | ? | ✅ STR formu | ❌ | ❌ | 🟡 | ✅ belge yanında | ✅ ŞİB taslağı | ❌ | ✅ | ✅ risk paneli | ✅ liste adı gizli | ✅ ŞİB formu | ? | ❌ | ? | ❌ |
| Eşik aşımında kimlik+imza olmadan fiş kapanmaz | ✅ | ? | ? | ? | ❌ | ❌ | ? | ? | ✅ beklemeye | ❌ | ❌ uyarır | ? | ✅ | ? | ? | ❌ | ? | ❌ |
| Tevsik haddi (30.000) nakdi durdurma | ✅ | ❌ | ❌ | 🟡 işaretler | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 🟡 12.000 TCKN | ❌ | ❌ | ❌ | ❌ | ❌ | 🟡 "nakit yasağı" iddiası | ❌ |
| Silinemez kayıt + ters kayıt + "ilk hâli görüntüle" | ✅ | ? | ? | ? | ? | ? | ✅ iz | ? | ✅ iz | ✅ Kayıt Düzenleme | ✅ | ? | ✅ | ? | ? | ✅ silinen raporu | ✅ denetim | ✅ |
| KVKK aydınlatma/rıza + şifreli dışa aktarım | ✅ | ? | 🟡 beyan | ? | ❌ | ❌ | 🟡 privacy | ❌ | ✅ | ❌ | 🟡 | ? | ✅ 8 yıl | ? | 🟡 | ❌ | ✅ TCKN şifreleme | ✅ izinler |

### 3.7 Müşteri / personel / platform

| Özellik | MH | PU | DZ | KH | CS | ML | MP | SN | SP | EY | TG | HF | KS | AR | KP | KM | KB | ERP |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| SMS / WhatsApp / e-posta kanalı | ❌ (D2-10) | ✅ Kivi fatura | ✅ WA | 🟡 destek | ✅ NetGSM | 🟡 PDF katalog | ❌ | ❌ | ✅ WA API şablon | ❌ | ✅ | ✅ SMS | 🟡 WA paylaş | ❌ | ✅ WA modülü | ❌ | ❌ | ✅ |
| Doğum günü / yıldönümü mesajı | ❌ | ❌ | ✅ | ❌ | ✅ özel gün SMS | ❌ | ❌ | ❌ | ❌ | ❌ | 🟡 Diamond | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ kampanya |
| Sadakat puanı / kart | ✅ (mali etkisiz) | ❌ | ❌ | 🟡 rapor | ✅ ParaPuan kart | ❌ | ❌ | ❌ | ❌ | 🟡 "İkram" | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 🟡 indirim motoru | ✅ Hopi |
| Uykuda müşteri / segment | ✅ | ❌ | ✅ VIP | ✅ | 🟡 gruplama | ❌ | ❌ | ❌ | ✅ sicil | ❌ | 🟡 CRM | ❌ | ❌ | ❌ | ❌ | 🟡 son işlem | ✅ | ✅ |
| Tek gelen kutusu (WA+Instagram+web) | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 🟡 WA | ❌ | ❌ | ❌ | ❌ | ❌ | 🟡 WA | ❌ | ❌ | ❌ |
| Prim/hedef | ✅ | ? | ✅ | ✅ 3 model | ✅ kota | ✅ hakediş | ❌ | 🟡 | ❌ | ✅ | ✅ analiz | ❌ | ❌ | ❌ | ❌ | 🟡 gram | ❌ | ❌ |
| **Prim simülasyonu ("bu oranla geçen ay ne öderdim")** | ❌ (D1-20 "dene-gör" = bu) | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Avans eşiği onayı + ay sonu maaş+prim−avans tek fiş | ❌ | ❌ | ✅ | ❌ | 🟡 maaş | ✅ avans/mesai | ❌ | ❌ | ❌ | 🟡 personel modülü | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | 🟡 |
| MFA / 2FA | ✅ | ❌ | 🟡 biyometrik | 🟡 Face ID | ❌ | ❌ dongle | ❌ | ❌ dongle | ? | ❌ | ? | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ biyometrik | ? |
| Hesap başına tek oturum | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? | ? | ✅ | ? | ? | ? | ? | ? |
| Yerel mobil uygulama | ❌ PWA ✅ | ✅ PARS/App | ✅ | ✅ | 🟡 izleme + tablet | 🟡 iPad | 🟡 ? | 🟡 tablet | ✅ + Watch | 🟡 Mini | ✅ 36 ekran | ✅ | ❌ PWA | ✅ | ✅ | 🟡 tablet | ✅ | ✅ |
| Çevrimdışı satış | ❌ (D3-03) | ? | ✅ masaüstü | 🟡 çelişkili | ✅ masaüstü | ✅ masaüstü | ❌ | ✅ masaüstü | 🟡 görüntüleme | ✅ masaüstü | ✅ 3–7 gün | ❌ | ❌ | ✅ Core | ? | ? | ✅ | ✅ |
| Çoklu şube + transfer | ✅ | ✅ | ✅ | ✅ | ✅ | 🟡 Center | 🟡 | 🟡 | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |
| Dış API / WebSocket kur | ✅ | ❌ | 🟡 MCP | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ Socket | ❌ | ✅ REST+webhook | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Vitrin/TV kur panosu | ✅ | ❌ | ✅ fiyat tahtası | ❌ | ❌ | ✅ Kurpano + oda | ❌ | 🟡 oda birliği | ✅ paylaşılabilir link | ❌ | ✅ | ✅ ana ürün | ✅ | ✅ Android TV | ❌ | ❌ | ❌ | ❌ |
| Dışa açık canlı fiyat sayfası / QR ürün / kiosk | ❌ | ❌ | 🟡 müşteri uyg. | ❌ | ❌ | ✅ web katalog | ❌ | ❌ | ✅ link + süre | 🟡 katalog | ✅ vitrin | ✅ alan adı | ✅ QR + kiosk | ✅ Shop | ❌ | ❌ | ✅ | ❌ |
| Web mağazası / pazaryeri | ❌ | 🟡 Kivi | ✅ 6 | ❌ | ❌ | ✅ 3 + B2B | ❌ | ❌ | ✅ ikas | ✅ 6 + IdeaSoft | ✅ 2 + B2B | 🟡 fiyat gönderimi | 🟡 katalog | ✅ Shop | ❌ | ❌ | ✅ | ✅ |
| Yapay zekâ asistanı | ❌ | ❌ | ✅ Becky | ✅ sesli/grafik | ❌ | ❌ | ❌ | ✅ Pro+ | ✅ açıklama analizi | ❌ | ❌ | ❌ | 🟡 AI Studio | ❌ | ❌ | ❌ | ❌ | 🟡 Nebim AI |
| Zekât hesabı | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ hesaplayıcı | ❌ | ❌ | ❌ | ❌ |
| Altın birikim hesabı | ❌ (D3-07) | ❌ | ❌ | ✅ Profesyonel | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

---

## 4. ByGatech'e eklenecekler

Boy: **S** ≈ 1 paket (≤ 1 gün) · **M** ≈ 2 paket · **L** ≈ 4+ paket. Sıra: satış etkisi × kolaylık. 15.10 planında zaten açık olan kalemler (D2/D3) burada tekrar edilmedi; yalnız bu taramanın yeni bulguları ve tanımı netleşenler var.

### 4.1 Dış bağımlılığı olmayan — hemen yapılabilir

| # | İş | Boy | Kaynak (rakip) | Not |
|---|---|---|---|---|
| Y-01 | **Ekran başına 1–2 dk eğitim videosu seti** (Hasfiyat/Pusula modeli: satış, hurda, emanet, banka, fatura, MASAK, kasa kapanışı, etiket, rapor) + bölüm zaman damgaları (KuyumSmart modeli); Kılavuz ekranından bağlantı | M (içerik) | Hasfiyat 49 video (§ 2.17), Pusula 27 (§ 2.2), KuyumSmart bölümlü (§ 2.18), ML Akademi 88 (§ 2.6) | Demo seslendirme hattı var (onaylı ses). Hiçbir rakipte "adım sayısı + kısayol" birlikte yok → sesli anlatımda söylenir. |
| Y-02 | **Ekranda kısayol yardımı** (Tezgâh'ta "?" ya da F1 ile F2/F4/F8/F9 listesi; kılavuzda tablo) | S | Terminal Gold F2/F3/F4/F12 (§ 2.13), CetaSoft F7/F8 (§ 2.5), Softnorm 30 dk kısayol videosu (§ 2.8) | Kodda var, belgede/ekranda yok. |
| Y-03 | **Gişe zimmeti akışı**: sabah yönetici sarrafiye adet + ürün + açılış nakdi zimmetler → tezgâhtar onaylar → takviye talebi (kendi talebini onaylayamaz) → akşam beklenen/sayılan kalem bazlı fark → onaylı gişe ana kasaya → vardiya devri sayımla | M | Ariyen 7 adım "Gişe Kasam" (§ 2.19), CetaSoft tezgâhtar başına mal+nakit (§ 2.5), KuyumHub vardiya fark ekranı (§ 2.4) | ByGatech'te çalışan kasası + kör sayım + vezne var; eksik: **ürün/sarrafiye zimmeti ve iki taraflı onay**. |
| Y-04 | **Günlük servet özeti**: kasa + banka + stok + cari, TL ve has çift kolon, **dünle fark**, riskli cari düşük ağırlıklı "tahsil edilebilir servet"; geçmiş tarihli öz sermaye | S | Dezy "Öz varlık" (§ 2.3), CetaSoft geçmiş tarihli öz sermaye (§ 2.5), SarrafPro gecelik bakiye (§ 2.11), KuyumSmart "Varlıklarım" (§ 2.18) | Patron paneli + altın pozisyonu + KarZarar var; "dünle fark" ve risk ağırlığı yok (?). |
| Y-05 | **Prim simülasyonu** "bu oranla geçen ay ne öderdim" + avans eşiği patron onayı + ay sonu maaş+prim−avans tek fiş + iade/iptalde prim geri alma kuralı (var mı? kontrol) | S+S | Dezy personel prim sayfası (§ 2.3 [D12]) | **15.10 planındaki tanımsız "dene-gör" (D1-20) bu.** 20.10'da "tanım yok" diye kapatıldı; tanım bulundu, yeniden açılmalı. |
| Y-06 | **Banka PDF/Excel ekstresi okuma** (VakıfBank, Ziraat PDF; genel Excel) → mevcut CSV eşleştirme hattına | M | Dezy 4 adım (§ 2.3 [D10]), Hasfiyat e-posta/PDF/Excel (§ 2.17) | Yarı dış: pilot işletmeden örnek PDF gerekir (gizli veri maskeli). Yapı Kredi/Halkbank/Deniz/Enpara için (API yok) tek yol bu. |
| Y-07 | **Has fiyat kaynağı seçimi** fatura/fiyat ayarında: TCMB (saatlik, varsayılan), BİST (günlük), serbest piyasa (dakikalık); seçim denetim izine | S | SarrafPro Fatura Ayarları (§ 2.11 [39]), Kivi 4 kaynak, CetaSoft 3 kaynak | Harem izni dış; TCMB herkese açık → en azından TCMB + elle. |
| Y-08 | **Hurda alışında pazarlık payı aşılınca yönetici onayı** (ayar başına alış milyemi + izin verilen sapma) — mevcut satış onayı altyapısına alış tarafı | S | KuyumSmart SSS #34 (§ 2.18) | D1-05 onay bandı yalnız satışta. |
| Y-09 | **Stok durumları**: transfer "yolda" (karşı şube kabul edene kadar), rezerve, kayıp, tamirde, konsinye verilen/alınan — Ürünler listesinde süzgeç | S | Terminal Gold 11 durum (§ 2.13), KuyumHub "transit" (§ 2.4), Dezy rezerve talebi (§ 2.3) | Transfer ve konsinye var; "yolda" ve "rezerve" durumu ? → doğrula. |
| Y-10 | **Fişte işlem öncesi/sonrası bakiye** satırı (TL + has) + müşteri diliyle ("3 çeyrek, 11 eski tam") | S | Kuyumcum 2.6.6 (§ 2.21), CetaSoft "Kalan borcu" (§ 2.5) | D1-09 müşteri dili ekstre var; fişte önce/sonra ? |
| Y-11 | **Fiyat teklif fişi** (satışa dönüşmeyen teklif; süreli; kur kilidiyle) | S | Hasfiyat 07.10 sürümü (§ 2.17), E-Yaz Teklif modülü (§ 2.12) | Kur kilidi (D1-04) temeli hazır. |
| Y-12 | **Kasa sayımı kuralları**: boş satır = "sayılmadı" (0 değil); kasa eksiye düşemez; gün açılış/kapanış aynı sayım şablonu | S | KuyumSmart SSS #39–41 (§ 2.18) | Kör sayım var; bu iki kural ? |
| Y-13 | **Yaptırım eşleşmesinde liste adının tezgâhtara gösterilmemesi** (yalnız "yönetici onayı gerekli"), şüpheli işlem notunun yalnız sahibe görünmesi | S | KuyumSmart SSS #50–58 (§ 2.18) | D1-18 gizlilik salt okunur hesap için var; tezgâhtar rolü için ? |
| Y-14 | **Dışa açık, süreli canlı fiyat linki** (vitrin ekranının paylaşılabilir, geçerlilik süreli sürümü) + **QR ürün sayfası / kiosk modu** (satın alma yok) | S + M | SarrafPro Paylaşılabilir Fiyat Ekranı (§ 2.11), KuyumSmart QR katalog + kiosk (§ 2.18), Hasfiyat alan adlı site (§ 2.17) | Vitrin ekranı + dış API var; anonim süreli link yok. |
| Y-15 | **Hesap planı eşleştirme ekranı** (600/391 alt hesapları, mali müşavir kodları bir kez) | S | Terminal Gold "bir kez eşleştirme" (§ 2.13), SarrafPro Mahsup (§ 2.11) | Luca/Zirve dosyası var; alt hesaplar yapılandırmadan → ekrana. |
| Y-16 | **Rakip programdan veri göçü şablonları** (Pusula/CetaSoft/M&L/Softnorm Excel dökümleri için sütun tanıma kuralları) | M | Terminal Gold "veri aktarımı ücretsiz 3–7 gün", KuyumSmart "program değiştir, veri kaybetme" (§ 2.18) | Akıllı içe aktarım (D1-16) var; rakip örnek dosyası müşteriden (yarı dış). |
| Y-17 | **Bildirimlerde sessiz saat** + fiş/gider fotoğrafı | S | Dezy (§ 2.3 [D8][D22]) | Gider modülü var; fotoğraf ? |
| Y-18 | **Hurdadan vitrine / barkodludan hurdaya** (ürünü hurda stoğuna düşürme ve tersi) | M | Pusula 2 video (§ 2.2), CetaSoft hurdalaştırma, Softnorm Ürün Dönüşüm Ekranı | **Kurucu kararı K-13 bekliyor** (D1-08, HURDA_FIRE_SIZINTISI kilidi). Rakiplerin tamamında var; karar verilmeden kodlanmaz. |

Toplam: 11 S + 6 M + 1 karar bekleyen; tahmini **60–100 saat** (paket başı 6–10 saat ölçüsüyle).

### 4.2 Dış bağımlılığı olan

| # | İş | Boy | Dış bağımlılık | Kaynak (rakip) |
|---|---|---|---|---|
| Y-20 | **Gelen e-fatura kutusu** (kabul/ret, kalan yasal süre, cari eşleşmesiyle alış faturasına/stok kartına aktarma) | M | Nilvera test hesabı (K-1) | Softnorm (§ 2.8 [16]), E-Yaz (§ 2.12), Faturalog (§ 2.1), SarrafPro |
| Y-21 | **e-Gider Pusulası + e-İrsaliye** (gider pusulası zaten var; e-belgeye çevrim) | M | Entegratör | KuyumHub, Hasfiyat, Softnorm, E-Yaz, Kivi |
| Y-22 | Belge durum akışı görünürlüğü: Taslak → Kuyrukta → Gönderildi → GİB → Ulaştı → Yanıt; kontör bakiyesi üst çubukta; gönderilemeyende kontör iadesi | S | Entegratör (kontör API'si) | Softnorm (§ 2.8), Terminal Gold 60 sn senkron |
| Y-23 | **WhatsApp Business API**: şablon + tetikleyici (fatura kesildi, ödeme alındı, tamir hazır, doğum günü); WhatsApp'tan gelen kimlik görselini AI ile okuma | L | Meta onayı (K-2) + AI servisi | SarrafPro (§ 2.11), Kuyum Plus WA modülü (§ 2.20), Dezy, Dijimo tek gelen kutusu |
| Y-24 | **NFC/OCR kimlik** + e-Devlet ikametgâh barkodu + NVİ doğrulama | L | Yerel mobil uygulama (D3-02), NVİ erişimi (TEYİTSİZ) | SarrafPro, KuyumHub, Kivi, KuyumSmart, Hasfiyat OCR |
| Y-25 | **RFID seti** (Zebra ZD621R + Chainway C5 + 71×28 kartona etiket; sayım eksik/fazla/yer değişmiş) | L | Donanım ortağı (K-4) | Softnorm vaka 6–8 saat → 5 dk (§ 2.8), Terminal Gold (§ 2.13), Ariyen 1.203/0:46 (§ 2.19) |
| Y-26 | **ÖKC** (Pavo N86 Android) | L | Cihaz SDK (K-4) | Kuyum Plus (§ 2.20), Nebim Ingenico (§ 2.14) |
| Y-27 | **E-ticaret fiyat gönderimi** (WooCommerce, ikas, IdeaSoft, Ticimax) — tam pazaryeri yerine önce yalnız fiyat/stok | M | Platform API anahtarları | Hasfiyat (§ 2.17), SarrafPro ikas (§ 2.11) |
| Y-28 | **AI ürün görseli** (tek fotoğraftan sahne/varyasyon) ve ürün metni | M | AI servisi | KuyumSmart AI Studio (§ 2.18), Dezy (§ 2.3) |
| Y-29 | **Oda/dernek ortak kur ekranı** + reklam alanı | M | Oda/dernek anlaşması | M&L Kurpano (§ 2.6), Softnorm Bursa odası (§ 2.8), SarrafPro Topluluk |

### 4.3 Yapılmayacaklar / dikkat

- **185.000 TL** eşiği (KuyumHub, Kivi), **KMTS "Nisan 2026"** ve "nakit yasağı" (KuyumBase): güncel değil/teyitsiz; ByGatech tarihli sınırla yönetiyor — rakip metni taklit edilmez.
- "GİB'e entegratörsüz doğrudan" (Terminal Gold) kendi sayfasıyla çelişiyor; "Online MASAK" (Kivi) pazarlama dili.
- E-Yaz sitesinde spam bağlantılar var; Kuyumcu Pro ve Altın Pusula kuyumcu ERP'si değil — rakip listesinden çıkarılmalı.
- Video izlenmediği için ekran içi adım sayıları ve kısayollar TEYİTSİZ; kurucu isterse 6 video (Softnorm kısayol 30 dk, Pusula satış+hurda 4:23, CetaSoft barkodlama 3:53, Hasfiyat alış-satış 5:15, KuyumSmart RFID 2:05, ZarifSoft 21 dk) izlenerek doğrulanabilir.

---

## 5. Kaynak toplamı

| Firma | Video | Belge | Kılavuz |
|---|---|---|---|
| Faturalog | 0 | 15 | ❌ |
| Pusula/PusulaX/Kivi | 36 + 7 | 16 | ❌ |
| Dezy | 0 | 24 | ❌ (SSS 33) |
| KuyumHub | 0 | 13 + 19 blog | ❌ |
| CetaSoft | 3 | 18 + 9 ekran | CD |
| M&L | ≥30 + 88 akademi | 25 | ❌ |
| Milyem Plus | 1 (3. taraf) | 30 | ❌ |
| Softnorm | 12 | 14 | ❌ |
| SarrafPro | 10 (8 genel) | 40+ | ✅ ~60 sayfa |
| E-Yaz Milyem / Expert | 34+ | 6 | ❌ |
| Terminal Gold / Kuvvem | 0 (2 Kuvvem) | 18 | ❌ |
| Nebim | 18 (çoğu 3. taraf) | 15 | ✅ sürüm PDF |
| Logo | 6 | 9 (docs erişilemedi) | ✅ (erişilemedi) |
| Mikro | ~20 (kuyum yok) | 7 | 🟡 |
| Hasfiyat | 49 | 18 SSS + 3 rehber + 5 sürüm | 🟡 |
| KuyumSmart | 18 | 81 SSS + 16 rehber | 🟡 |
| Ariyen | 5 | 23 blog + 7 hesaplayıcı | ❌ |
| Kuyum Plus | 9 (≤10 sn) | 12 | ❌ |
| Kuyumcum | 1 | 11 SSS + 4 sürüm | ❌ |
| ZarifSoft | 2 (biri 21 dk) | 3 | ❌ |
| KuyumBase | 0 | 6 SSS + 52 blog | ❌ |
| Gramino | 0 | 1 | ❌ |
| Bakiyem / Dijimo / Kuyumex | 0 | 23 SSS / 1 / 1 | ❌ |

Tüm erişimler 20.10.2026. Hiçbir rakip ürünü denenmedi; tüm özellikler üretici beyanıdır.
