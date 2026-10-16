# Eksik Özellik Denetimi: Mihenk

**Tarih:** 02.10.2026 · **Soran:** Kurucu Ramazan Akkocan · **Hazırlayan:** Tamer Bey (salt okuma denetimi)

**Soru:** "Sıradaki işler bitince, eklemediğimiz bir özellik kalıyor mu?"

**Nasıl bakıldı:** Plan, kıyas, kılavuz, kurul ve keşif belgelerindeki her söz tek tek çıkarıldı. Her biri kodda arandı ve satır numarasıyla kanıtlandı. Yasa iddiaları yalnız mevzuat.gov.tr'deki resmî metinlerden okundu. Rakip bilgisi rakiplerin kendi sayfalarından alındı.

**Bu denetimde neler yapılmadı:** Hiçbir dosya değiştirilmedi. Sunucu ya da tarayıcı açılmadı, veritabanına dokunulmadı. Bu yüzden "VAR" demek "kodda ekranı ve arka ucu var" demektir. Ekranda çalıştığı bu denetimde ölçülmedi.

**Kısaltmalar:** Api = `dotnet/Kuyumcu.Api/Modules/` · Web = `dotnet/Kuyumcu.Web/` · D9 = `dokumanlar/09-uygulama-kayitlari/`

**İş büyüklüğü:**
- **S:** yaklaşık 1 paket (yarım gün)
- **M:** 2–3 paket (1–2 gün)
- **L:** 4 paket ve üstü

---

## (A) Özet

Sıradaki işlerin (TOPLU-4, PARA-DEMO-2, TOPLU-5, OLCEK-YUK, T-LINUX, T-WIN) neredeyse tamamı görünüm düzeltmesi, para hatası düzeltmesi, yük testi ya da kurulum provası. Yeni özellik getirmiyorlar. Hepsi bitince **vaat edilmiş ama yapılmamış ya da yarım kalmış 29 özellik** kalıyor; ayrıca **4 özellik kurucu ya da mali müşavir kararını bekliyor**.

**Satışı doğrudan engelleyecek 2 eksik var:**
1. **Alış-satış fiyat modeli.** Çeyrek, yarım, tam ve ata satış fiyatı canlı kotasyondan gelmiyor. Gram altında ayrı alış fiyatı ve ayrı satış fiyatı yok. Kodun kendisi bunu "Faz 1'de açılacak" diye bekletiyor.
2. **KDV özet raporu.** Mali müşavirin ilk isteyeceği rapor bu ve henüz yok.

**Kodu hazır olup ekranı olmayan 6 iş var:** satın alma ve mal kabul, irsaliye, cari ekstresi yazdırma, emanet belgesi yazdırma, siparişe kapora ve usta bağlama, sipariş süreç şeridi. Bunlar en ucuz kazanç.

**Resmî metinden doğrulanan iki yasal konu:**
- Kuyum Ticareti Hakkında Yönetmelik md.10/3, kuyumcunun "emanet alamaz" olduğunu söylüyor. Bu, Emanet modülünü ve "müşterinin altın hesabı" dilini ilgilendiriyor. Konu zaten K-12 ile hukuki görüş bekliyor; metin artık birincil kaynaktan doğrulandı.
- Fiyat Etiketi Yönetmeliği md.6/3, vitrindeki fiyatlarda işçiliğin dahil olup olmadığının ayrıca yazılmasını istiyor. Vitrin panosunda bu ibare yok.

**Plana hiç girmemiş 10 fikir de çıktı.** En önemlisi şu: külçe altın satışındaki KDV istisnası (KDV Kanunu md.17/4-g) için programda ayrı bir yol yok ve bu soru mali müşavir soru listesinde de yer almıyor.

---

## (B) Eksik: vaat edilmiş ama yok ya da yarım

**Önem:** Satış engeli · Önemli · İyi olur. **İş:** S / M / L.

### B1. Kodu hazır, ekranı ya da bağlantısı yok (en ucuz kazanç)

| # | Özellik | Kaynak | Durum | Kanıt | Önem | İş |
|---|---|---|---|---|---|---|
| 1 | Satın alma siparişi ve mal kabul ekranı | EKRAN-ALAN-ILISKI-KIYAS:91 · KAPSAM-ENVANTERI Y-34 | KISMEN: sunucuda 8 uç var, ekranı ve menüsü yok | Api/Satinalma/SatinalmaModulu.cs:711-748 · Web'de yalnız VeriDisaAktarim.razor'da geçiyor | Önemli | M |
| 2 | İrsaliye (sevk ve alış) ekranı ve kâğıt çıktısı | PARITE-IS-LISTESI:120 · YAPABILECEKLERIMIZ:44 · Y-35 | KISMEN: sunucuda 10 uç var; ekranı yok, basılabilir belge türü yok | Api/Irsaliye/IrsaliyeModulu.cs:419-461 · Api/Cikti/CiktiModulu.cs:44-47 (yalnız 4 belge türü var) | Önemli | M |
| 3 | Cari ekstresini Cariler ekranından yazdırma | KULLANIM_KILAVUZU:220, 451 · YAPABILECEKLERIMIZ:63 | KISMEN: belge var, ekranda düğmesi yok; ancak adres elle yazılarak açılıyor | Api/Cikti/CiktiModulu.cs:47 · Web/Pages/Cariler.razor içinde "/cikti" 0 kez geçiyor | Önemli | S |
| 4 | Emanet teslim belgesini Emanet ekranından yazdırma | KULLANIM_KILAVUZU:175, 451 | KISMEN: yalnız İşlem Defteri'nde ve yalnız BASİT modda düğmesi var | Web/Pages/Emanet.razor içinde "/cikti" 0 kez · IslemDefteri.razor:224-228 | Önemli | S |
| 5 | Siparişe kapora bağlama ve usta seçme | EKRAN-ALAN-ILISKI-KIYAS:88, 258 | KISMEN: kapora "Diğer işlemler"den alınıp satışta düşülüyor, ama sipariş kartına bağlanmıyor; usta alanı ekranda yok | Api/Siparis/SiparisModulu.cs:59-72, 1035 (kapora ucu) · Web'de "/kapora" çağrısı yok · Web/Bilesenler/Fis/FisDigerSiparis.razor:58 | Önemli | S |
| 6 | Sipariş ve tamir süreç şeridi (alındı → işlemde → hazır → teslim) | YAPABILECEKLERIMIZ:42 · RAKIP-KIYAS:75 | KISMEN: şerit bileşeni yazılmış ama hiçbir sayfada kullanılmıyor | Web/Bilesenler/AdimGostergesi.razor · sayfalarda AdimGostergesi kullanımı: 0 | İyi olur | S |

### B2. Yarım kalmış

| # | Özellik | Kaynak | Durum | Kanıt | Önem | İş |
|---|---|---|---|---|---|---|
| 7 | **Alış-satış fiyat modeli:** sarrafiye (çeyrek, yarım, tam, ata) satış fiyatı canlı kotasyondan gelsin; gram altında ayrı alış ve satış fiyatı olsun | Kodun kendi sözü: Api/Urun/UrunModulu.cs:366-372 ("alış/satış modeli gelince (Faz 1) yeniden açılacak") · rakip: e-yaz ziynet alış-satış | KISMEN: kotasyonlar ve vitrin panosunda alış/satış ayrımı var. Satış ekranı bunları kullanmıyor; kur kaydında tek has fiyatı var, alış fiyatı elle giriliyor | Api/Kur/KurModulu.cs:96-123 · Api/Pano/PanoFiyatServisi.cs:111, 159 · Data/Entities.cs:341 (yalnız AltinHasFiyati) · Satış ve Fiyat modüllerinde "çeyrek" araması: 0 | **Satış engeli** | M |
| 8 | Eksik raporlar: **KDV özeti**, kategori satış, stok devir, hurda özeti, iade özeti | PARITE-IS-LISTESI:107 · YAPABILECEKLERIMIZ:26 | KISMEN: 16 rapor var; bu 5 rapor "Faz B" diye bilerek bırakılmış | Api/Rapor/RaporKatalogu.cs:35-39 | KDV özeti **Satış engeli**, diğerleri Önemli | M |
| 9 | Her fişte "geri al": takas, hurda alışı ve transfer de geri alınabilsin; İşlem Defteri'nde düğme olsun | PLAN-SADELESTIRME:168 · RAKIP-KIYAS:161 · Kurucu kararı A19 | KISMEN: bu üç işlem türü geri alınmak istenince reddediliyor; İşlem Defteri'nde yalnız rozet var | Api/Satis/StornoServisi.cs:80 (ErtelenenTurler) · Web/Pages/IslemDefteri.razor:146-236 | Önemli (A19 kararına bağlı) | M |
| 10 | Vitrin panosu: dükkân ekranında tam ekran açılsın ve **"işçilik dahildir / hariçtir" ibaresi** olsun | KULLANIM_KILAVUZU:457 · DEMO-SENARYOSU:145 · Fiyat Etiketi Yönetmeliği md.6/3 (aşağıda) | KISMEN: pano var ama menünün içinde açılıyor; işçilik ibaresi yok | Web/Pages/Vitrin.razor · Web/Layout/AnaYerlesim.razor:77-78 (kabuksuz açılan tek ekran /ekranlar) | Önemli (yasal ibare) | S |
| 11 | Yeni işletme BASİT modda açılsın (oylanmış karar D1) | PLAN-SADELESTIRME:210 · KARARLAR-D1-D11:15 | KISMEN: varsayılan hâlâ PROFESYONEL | Data/Entities.cs:148 | Önemli | S |
| 12 | Müşteri değişince satış ekranı "fiyat listesi değişti, güncelleyeyim mi?" diye sorsun | KULLANIM_KILAVUZU:253 | YOK: kılavuz anlatıyor, kodda böyle bir davranış yok | Web/Pages/Islemler.razor:297 (MusteriDegisti yalnız kimlik durumunu sıfırlıyor) | Önemli (kılavuz yanlış bilgi veriyor) | S |
| 13 | Hurda alışında satıcının kimlik ve adres bilgisi hatırlansın | PUSULA-KIYAS:209 · PLAN-SADELESTIRME:169 | KISMEN: yalnız ad hatırlanıyor | Web/Bilesenler/Fis/FisHurdaAlis.razor:41-42, 84-87 | İyi olur | S |
| 14 | Açık pozisyonun TL / USD / EUR ayrımlı tablosu (Pusula'daki gibi) | PUSULA-KIYAS:123, 207 · PUSULA-REFERANS:27-33 | KISMEN: yalnız has gram pozisyonu var | Api/Pozisyon/PozisyonModulu.cs:13-41, 216 | İyi olur | M |

### B3. Hiç yok

| # | Özellik | Kaynak | Kanıt (ne arandı → sonuç) | Önem | İş |
|---|---|---|---|---|---|
| 15 | Has altın defteri dökümü ve belge ile defterin karşılaştırılması (paket 39) | PARITE-IS-LISTESI:117 · YAPABILECEKLERIMIZ:45 | "HasDefteri / defter-dokum": 0 | Önemli | M |
| 16 | Etiket ve belge şablonu seçme (paket 5) | PARITE-IS-LISTESI:52 · kalan-paketler/PAKET-05 | "EtiketSablon": 0; etiket sayfasının düzeni sabit | İyi olur | M |
| 17 | İlk girişte 3 soruluk karşılama ve uygulama içinde 7 adımlı tanıtım turu | PLAN-SADELESTIRME:314-315 | "IlkGiris / TurBaslat": 0 (yalnız kuruluş sihirbazı var) | İyi olur | M |
| 18 | Müşteri kartında vergi dairesi, IBAN, ikinci telefon, temsilci | EKRAN-ALAN-ILISKI-KIYAS:196 | Bu alanlar müşteri tablosunda yok (Data/Entities.cs:365-478); vergi dairesi yalnız işletmenin kendi kartında var (:158) | Önemli (toptancı ve şirket müşterisi için) | S |
| 19 | Ürün kartında birden çok taş satırı, marka ve model | EKRAN-ALAN-ILISKI-KIYAS:198, 252 | Tek taş alanı var | İyi olur | M |
| 20 | KVKK açık rıza ve aydınlatma kaydı | EKRAN-ALAN-ILISKI-KIYAS:261 | "riza / aydinlatma": yalnız bir yorum satırı | Önemli (hukuki teyit gerekir) | S |
| 21 | Kasiyer ve çırak gibi dar yetkili roller (paket 46, Faz A) | MIMARI tablo 46 | 4 rol var: SAHİP, MÜDÜR, TEZGÂHTAR, MUHASEBE (Data/Entities.cs:17) | İyi olur | M |
| 22 | Aboneliği "gecikmiş" ve "askıda" durumuna alma | HAZIRLIK-KURULU-5:30 | Api/Abonelik/AbonelikVarliklari.cs:102-103 (yalnız AKTİF ve SONLANDI) | Önemli (tahsilat) | S |
| 23 | Sahip ve müdür için iki aşamalı girişi zorunlu tutma ya da uyarı bandı | HAZIRLIK-KURULU-5:34 | "MfaZorunlu": 0 (iki aşamalı giriş isteğe bağlı olarak var) | Önemli (güvenlik) | S |
| 24 | Bakiyeyi yeniden hesaplama aracı: önce önizleme, sonra denetim izi bırakan düzeltme fişi | HAZIRLIK-KURULU-5:19 · KURUCU-KARAR-BEKLEYENLER md.8 | "yeniden-hesap": 0; uyuşmazlık yalnız bantla gösteriliyor (Cariler.razor:1057-1067) | Önemli (md.8 kararı gerekli) | M |
| 25 | Rakip programdan hazır göç profili (Dezy, Mikro, Logo, KuyumHub) | HAZIRLIK-KURULU-5:35 (GOC-1) | Api/Iceaktarim içinde "profil / Dezy / Mikro": 0 (rakibin örnek dosyası bekleniyor) | Önemli (satışta ilk sorulur) | M |
| 26 | İşlemlere geçmiş tarih girebilme (demoda 3 aylık geçmiş için) | RUNBOOK-DEMO:52-56 | Satış uçları işlem tarihi almıyor | İyi olur (demo) | M |
| 27 | Bankada kredili mevduat (KMH) limiti | KARARLAR-URUN K-24 madde 2 | "kmh / kredili": 0 | İyi olur | S |
| 28 | Gider pusulasında adresi eksik eski kayıtların listesi | KARARLAR-URUN K-25 madde 3 | "EksikBilgi": 0 | İyi olur | S |
| 29 | Panel'deki hava durumu kutusunu ayardan kapatma | DEMO-SENARYOSU:79 | Ayarlar.razor içinde "hava": 0 (demo konuşması bu ayar varmış gibi anlatıyor) | İyi olur | S |

### B4. Kodu hazır, karar bekliyor

| # | Özellik | Bekleyen karar | Bugünkü durum | Kanıt | Önem |
|---|---|---|---|---|---|
| 30 | Yasal yaptırım listesinin resmî kaynaktan yüklenmesi | md.11 / A13 (kurucu) | Yükleme yolu var; liste boş olduğu için Panel'deki "Yasal liste yüklenmedi" bandı kalkmıyor | Api/Masak/YaptirimServisi.cs:111-122 | **Satış engeli** (demoda görünüyor) |
| 31 | E-postayla parola sıfırlama | A20 (e-posta servisi seçimi) | Ekran var; üretimde gerçek e-posta gönderilmiyor | Api/Auth/ParolaSifirlamaModulu.cs:74, 103, 114-119 | Önemli |
| 32 | Tamamen altın cinsinden veresiye (TL tutarı 0) | A18 | Program şu an engelliyor | Api/Cari/CariModulu.cs:237 | Önemli |
| 33 | KDV özel matrah kodu (805) | A14 / S-3 (mali müşavir) | Kodda var, varsayılan olarak kapalı | Api/Fatura/KdvAyarlari.cs:42-58 | Önemli |

---

## (C) Planlı ve sırada (eksik sayılmaz)

Kaynak: D9/ORKESTRATOR-DURUM.md:1454-1476 · D9/ham/AKTIF-KOSUM (son satırlar, 02.10 09:27) · D9/ham/TOPLU-TASARIM-KUYRUGU-2026-09-29.md

| Paket | Ne getiriyor | Durum (02.10 09:27) |
|---|---|---|
| TOPLU-4 | 14 görünüm düzeltmesi. İçinde bir kritik madde var: Panel'deki "Kim borçlu?" satırı dar ekranda kırılıyor | Çalışıyor (internet kesintisinden sonra sürdürüldü) |
| PARA-DEMO-2 | 13 para ve kasa düzeltmesi: tek bacaklı kart ödemesi "karma" yazılıyor; gün kapatırken sahte fark çıkıyor; nakit çıkış kontrolü toplam bakiyeye bakıyor | Çalışıyor |
| PATCH-SOZLESME | Düzeltme uçlarının taraması, şube düzenleme, faturada has birikimi | Çalışıyor (09:27'de başladı) |
| TOPLU-5 | Cariler ve ekstre görünümü, fişteki "(örnek adres)" yazısı, yasal liste bandının dili | Bekliyor (üç şerit boşalınca başlayacak) |
| OLCEK-YUK | 10, 50 ve 100 işletmeyle yük testi. Özellik değil, kapasite kanıtı | Bekliyor (TOPLU-5'ten sonra, tek başına koşulacak) |
| T-LINUX | Temiz bir Linux sunucuda baştan sona kurulum provası | Kurucu kararı A4 bekleniyor (sunucu ve alan adı) |
| T-WIN | Temiz bir Windows'ta kurulum provası | Kurucu kararı A4 bekleniyor (deneme bilgisayarı) |

**Kuyrukta olup henüz pakete bağlanmamış işler:**
- Has ve gram yazımının tüm ekranlarda aynı olması (BICIM-TEK; Web/Servisler/Bicim.cs:81-91)
- SATIS-3'ün orta önemdeki maddeleri
- Rapor merkezindeki "Gün sonu" kartının sadeleştirilmesi
- Kâr-zararda sayı biçimi
- Dar ekranda Ürünler'deki satır düğmeleri
- Koyu temada silik sekmeler
- Liste, rapor ve yönetim ekranlarında "zorunlu önce" düzeni (yaklaşık 5 paket)
- Eski 31 iş (P-58..P-63 vb.)
- Kurul planındaki T-1 (yayın dilimi), T-3 (başka makinede geri yükleme tatbikatı), F-1, MIG-HIJYEN, PARCALA-1

---

## (D) Bilinçli olarak hariç tutulanlar

Kaynak: `docs/HARIC-LISTESI.md:35-42`. Bunlar eksik sayılmadı. Ama rakiplerde var ve satış görüşmesinde sorulur; aşağıda hangi rakipte görüldüğü yazıyor.

| Madde | Neden hariç | Rakipte var mı |
|---|---|---|
| H-1 e-Fatura / e-Arşiv / e-İrsaliye gönderimi | K-2: entegratör sözleşmesi, mali mühür | Var: Milyem Plus (milyemplus.com, "özel matrah senaryoları"), Terminal Gold (terminal.gold/kuyumcu-yazilimi) |
| H-2 Yazarkasa (ÖKC) | K-11: cihaz kararı | Bu turda rakip kaynağı doğrulanmadı |
| H-3 SMS / WhatsApp gönderimi | K-15: operatör sözleşmesi | Var: CetaSoft "SMS ve e-mail gönderim" (cetasoft.com.tr/kuyum/yap.php) |
| H-4 Bankayla canlı bağlantı | K-22: banka sözleşmesi | Bu turda rakip kaynağı doğrulanmadı (ekstre içe aktarma bizde var) |
| H-5 İmalat ve atölye (fire, ramat, eritme) | Perakende dükkân kararı | Var: Pusula "Atölye, Stok ve Fire Takibi" (pusulanet.net), KuyumERP (kuyumerp.com), CetaSoft "fire, ifraz, kalafat" |
| H-6 RFID sayım donanımı | K-21: donanım kararı | Var: e-yaz "RF-ID sayım" (e-yaz.com.tr/kuyumculuk-yazilimi) |
| H-7 Emanet parça kimliği | K-12: hukuki görüş | Emanet takibi var: CetaSoft "cari hesap ve emanet takibi". **Bkz. E-1 hukuki uyarısı** |
| H-8 Seri no / bandrol (KMTS) takibi | K-14: mevzuat teyidi | Bu turda rakip kaynağı doğrulanmadı |
| Rakip programın kendi dosya biçiminden göç | K-17: rakibin deneme hesabı yok | — |

**1.1 sürümüne bilerek ertelenenler.** Bunlar HARİÇ listesinde değil; kaynak D9/KAPSAM-ENVANTERI.md:159-171 ve KARARLAR-URUN-2026-09-21.md:

| Ertelenen | Rakipte görülen |
|---|---|
| Müşteri puanı ve sadakat | CetaSoft "müşteri sadakat ve parapuan" (cetasoft.com.tr/kuyum/modul.php) |
| Personel primi | CetaSoft "personel prim sistemi", ML Yazılım "personel hakediş" |
| Telefon uygulaması ve çevrimdışı çalışma | KuyumBase "offline mod", Dezy "çevrimdışı çalışma" (dezyapp.com/sss) |
| Pırlanta sertifika kartı basımı | e-yaz "sertifika basabilir" |
| E-ticaret | Dezy (Trendyol, Shopify), Kuvvem "e-ticaret modülü" |
| Mağaza taksit planı, teklif/proforma, hediye çeki, ekspertiz fişi, fiyat kilitleme, müşteri gruplama, patron paneli, döviz alım-satım ekranı, set birleştirme/bozma | Bu turda rakip kaynağı doğrulanmadı |

**Not: hariç listesi ve bazı belgeler eskimiş.**
- **Açılış stoku yanlışlıkla "hariç" yazıyor.** HARIC-LISTESI "açılış stoku" ve "CSV ile açılış satırları" için "HARİÇ, Faz B" diyor (docs/HARIC-LISTESI.md:59-60). Oysa ikisi de artık kodda var: Api/Iceaktarim/DefterKaynaklari.cs:157 (`acilis-bakiye`) ve :318 (`acilis-stok`). Liste müşteriye, var olan bir yeteneği "yok" diye söylüyor.
- **MIMARI.md başka bir ürünü anlatıyor.** `docs/MIMARI.md` eski NestJS/React/Expo mimarisini anlatıyor; ürün .NET/Blazor.
- **Kurulun istediği madde listeye eklenmemiş.** Kurul 5, "mobil uygulama yok" maddesinin hariç listesine eklenmesini istedi; eklenmemiş.
- **A11'de belge ile kod çelişiyor.** Belge "tutucu" diyor; kod ise devir bakiyesinden nakit tahsilatı "geçer, iz bırakır" diye uyguluyor (Api/Tevsik/TevsikKurali.cs:199-209).

---

## (E) Yeni fikirler: plana hiç girmemiş olanlar

| # | Fikir | Koddaki durum | Kaynak | Önem | İş |
|---|---|---|---|---|---|
| E-1 | **Emanet ve "altın hesabı" için hukuki uyarı.** Kuyum Ticareti Hakkında Yönetmelik md.10/3: *"Kuyum işletmesi, kuyum veya herhangi bir menkulü emanet alamaz ya da başka bir amaçla saklayamaz."* | Emanet modülü müşterinin altınını teslim alıp has bakiyesine yazıyor (Api/Emanet/EmanetModulu.cs:14-20). Demo dili cari has bakiyesini "müşterinin altın hesabı" diye sunuyor (Core/I18n/Sozlukler.ParaDemo.cs:95-98) | Resmî metin: mevzuat.gov.tr/MevzuatMetin/yonetmelik/7.5.38527.pdf (RG 14.04.2021/31454; değişiklik RG 24.12.2021/31699). K-12 olarak zaten hukuki görüş bekliyor; metin bu denetimde birincil kaynaktan doğrulandı | **Satış engeli olabilir** (hukuk kararı) | S (karar ve metin) |
| E-2 | **Külçe altın satışında KDV istisnası** (KDV Kanunu md.17/4-g: *"Külçe altın ve külçe gümüş teslimleri"*) | Programda ayrı bir istisna yolu yok; yalnız ziynet ve sikke için özel matrah var (Api/Fatura/FaturaModulu.cs:329-344). Mali müşavir soru listesinde bu soru yok | Resmî metin: mevzuat.gov.tr/mevzuatmetin/1.5.3065.pdf | **Satış engeli** (gram/külçe satan dükkân için mali risk) | S–M (önce mali müşavire soru) |
| E-3 | Ürün kartına girilen KDV oranı faturada kullanılmıyor | `KdvOrani` yalnız ürün kartında ve ürün modülünde geçiyor, fatura tarafında hiç okunmuyor (Api/Urun/UrunModulu.cs, Data/Entities.cs, Web/Pages/Urunler.razor) | Kod incelemesi | Önemli | S |
| E-4 | İşçiliği milyem cinsinden girme (toptancı ve atölye dili) | TL işçilik ve has hesabı var (Core/Domain/Ayarlar.cs:236, Data/Entities.cs:523); milyem işçilik yok | Kuvvem (kuvvem.com/sektor/kuyumcu-magaza-programi), CetaSoft | Önemli | M |
| E-5 | Muhasebe programlarına hazır aktarım biçimi (Luca, Logo, Mikro, Zirve) ve Excel dosyası | 12 ayrı CSV dosyası çıkıyor, muhasebe fişi de dahil (Api/Disaaktarim/Kaynaklar.cs:441-505); program biçimi ve Excel yok | e-yaz, CetaSoft "resmi muhasebe entegrasyonu", KuyumBase "Paraşüt entegrasyonu" | Önemli | M (her biçim için) |
| E-6 | İşletmenin kendi verisinin tam yedeğini indirebilmesi | Yedek talebi ve durum ekranı var; indirme bilerek yok (Api/Yedek/YedekModulu.cs:20-21) | Rakipler de yedeği kendileri alıyor (Terminal Gold "günlük yedek"); kuyumcunun güven sorusu | İyi olur | M |
| E-7 | Kur ve altın fiyatı geçmişi grafiği | Geçmiş tablo olarak var (Web/Pages/Kur.razor:350-440); grafik bileşeni hazır (Web/Bilesenler/AlanGrafik.razor) | KuyumBase "fiyat geçmişi grafikleri" (kuyumbase.com/ozellikler) | İyi olur | S |
| E-8 | Doğum günü ve evlilik yıldönümü hatırlatması (ekran içi; SMS hariç) | Doğum tarihi alanı var (Web/Pages/Cariler.razor:393); hatırlatma işi yok, yıldönümü alanı yok | 1.1'deki müşteri gruplamasıyla kısmen örtüşüyor | İyi olur | S |
| E-9 | Termal etiket yazıcısına doğrudan basım | Etiket tarayıcıdan basılıyor (Web/Pages/UrunEtiketleri.razor:192-233); termal yazıcı dili yok | ML Yazılım, Pusula "hızlı barkodlama", e-yaz "toplu barkod basımı" | İyi olur | M |
| E-10 | Altın günü (müşteri grubunun altın biriktirmesi) | Yok | Rakipte görülmedi; E-1 hukuki görüşüne bağlı | İyi olur | M |

Kodda zaten var olduğu görülen olağan ihtiyaçlar şunlar: ayar ve has dönüşümü, sipariş ve tamir takibi, şubeler arası transfer, barkodla sayım, gram, ayar ve milyem yazan etiket, kullanıcı rolleri, gün sonu, POS komisyonu, masraf, hurda alımı ve gider pusulası, fotoğraflı ürün kartı, kapora. Dar ekran düzeni de var ama telefon uygulaması yok.

**Doğrulanan yasal eşik:** Kimlik tespiti eşiği 185.000 TL (Tedbirler Hakkında Yönetmelik md.5/1-b; mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf). Koddaki değer de aynı: Api/Masak/MasakKuralMotoru.cs:24.

---

## (F) Önerilen sıralama: ilk 5 iş

1. **Alış-satış fiyat modeli (B-7, M).** Kuyumcunun tezgâhtaki ilk işi çeyrek ve gram altın alıp satmak; demoda ilk sorulacak şey de bu. Kotasyonlar ve vitrin panosu zaten alış/satış ayrımını biliyor, yalnız satış ekranına bağlanması gerekiyor.
2. **Mali müşavir paketi (B-8 KDV özeti, E-2, E-3, B-33).** Mali müşavir ikna olmadan dükkân programı değiştirmez. Önce E-2 (külçe istisnası) soru listesine eklenir; ardından KDV özet raporu ve ürünün KDV oranının faturaya bağlanması yapılır.
3. **Kodu hazır, ekranı eksik işler (B-1…B-6).** Cari ekstresi ve emanet belgesine yazdır düğmesi, siparişe kapora ve usta, satın alma ve irsaliye ekranları. Sunucu tarafı yazılmış; en az emekle en çok görünür kazanç burada.
4. **Küçük ama görünür düzeltmeler (B-10, B-11, B-12, D notu).** Vitrin panosuna tam ekran ve işçilik ibaresi (yasal), yeni işletmenin BASİT modda açılması, kılavuzdaki yanlış anlatımın düzeltilmesi, hariç listesinin güncellenmesi. Her biri yaklaşık 1 paket.
5. **Geri al'ı tamamlamak (B-9).** Takas, hurda alışı ve transfer de geri alınabilmeli. Hatalı fişi düzeltememek, dükkânın programa güvenini en hızlı bozan şey. Önce kurucunun A19 kararı gerekiyor.

**Kod dışı ama acil kararlar:**
- **K-12, emanet hukuki görüşü (E-1).** Emanet modülünün kaderini belirler.
- **md.11, yaptırım listesinin yüklenmesi (B-30).** Panel'deki bant demoda görünüyor.
- **A20, e-posta servisi (B-31).** Parola sıfırlama buna bağlı.

**Kaba toplam iş:** B ve E'deki kod işleri yaklaşık 19 adet S ve 19 adet M; L yok. Ölçülmüş hızla (şerit başına paket yaklaşık 1,5 saat, görünüm paketlerinde 6–10 saat) üç şerit paralel çalışırsa **ilk 5 iş için yaklaşık 4 gün** gerekir. Hepsi için 2–3 hafta gerekir. Bu bir tahmindir, ölçülmüş değildir.

---

**Okunan kaynaklar:** SONRAKI-OTURUM-PROMPT, PLAN-SADELESTIRME-PUSULA, PUSULA-KIYAS, EKRAN-ALAN-ILISKI-KIYAS, RAKIP-KIYAS, KISACA-RAKIPLER, YAPABILECEKLERIMIZ, PARITE-IS-LISTESI, PLAN-YASAL-ZORUNLULUKLAR, 02-mimari (MIMARI-KALAN-PAKETLER, sadelestirme/*, F-8, kalan-paketler başlıkları), docs (KULLANIM_KILAVUZU, HARIC-LISTESI, RUNBOOK-DEMO, MIMARI, URUN-ANLATIMI), D9 (HAZIRLIK-KURULU-1…5, KURUCU-VE-MALI-MUSAVIR-BEKLENENLER, KAPSAM-ENVANTERI, KARARLAR-URUN, ORKESTRATOR-DURUM), ham (KESIF-BULGULARI, KESIF-TRIYAJ, KESIF-KARARLAR, KURUCU-SIKAYET-KATALOGU, KURUCU-KARAR-BEKLEYENLER, KIYAS-BULGULARI, TOPLU-TASARIM-KUYRUGU, AKTIF-KOSUM, IS-KUYRUGU).

**Konum farkları:**
- DEMO-SENARYOSU, 09-uygulama-kayitlari altında değil, `dokumanlar/02-mimari/sadelestirme/` altında bulundu.
- MIHENK-PLAN-RAPORU-2026-09-28 yalnız PDF olarak var; okunmadı.

**Bu turda sınırlı kalanlar:**
- KESIF-BULGULARI'ndaki 91 madde satır satır yoklanmadı; triyaj ve P1–P8 raporlarının "açık kalanlar" bölümünden gidildi.
- Akınsoft, Kuyumsoft ve Netsis için kuyumcu özellik sayfası bulunamadı.
- Rakip özellikleri sayfa özetinden okundu, birebir alıntı değil.
