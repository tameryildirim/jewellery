# Pusula'ya Göre Neredeyiz — Menü, Ekran, Alan ve Fiyat Kıyası

**Kime:** Kurucu Ramazan Akkocan · **Hazırlayan:** Tamer Bey koordinasyonunda ByGatech ekibi
**Tarih:** 21 Eylül 2026 · **Kapsam:** Pusula Yazılım + PusulaX/KiviERP ekosistemi ↔ Kuyumcu ERP'nin bugünkü hâli
**Neden:** Müşteri Pusula'yı *"basit ve işe yarar"* buldu. O cümlenin teknik karşılığı arandı.

---

## 1. Tek cümlelik cevap

**Pusula'nın sattığı şey özellik değil, çevirisizlik.** Kuyumcunun kafasındaki *"bir alışveriş = bir fiş; ne verdim, ne aldım"* modelini tek ekrana birebir çizmiş. Biz o çeviri yükünü tezgâhta hâlâ kullanıcıya bindiriyoruz — ama aradaki fark **kütle farkı değil, kablo farkı**: kaybettiğimiz maddelerin çoğunda sunucu tarafı zaten yazılmış, ekrana bağlanmamış.

---

## 2. Pusula kim — ilk kez doğrulanan firma gerçekleri

| | |
|---|---|
| Kuruluş | **2002** (24 yıllık) |
| Kullanıcı tabanı | kendi beyanıyla **"4.000 üzeri şirket"**, 17 ülke |
| Ekip | **15 kişi** (Ekibimiz sayfasında isim isim) |
| Adres | Cağaloğlu/Fatih, İstanbul; aynı adreste kardeş firma Bilkar Bilgisayar |
| Satış | Bayi ağı + yerinde ziyaret. **Ücretsiz deneme sürümü yok** |
| Destek | Sitede **15 destek personelinin adı ve cep numarası** + Ankara/İzmir bölge bayileri + uzaktan yardım |
| Ürünler | Perakende · Toptan · Üretim · Pırlanta Üretim · Taş Stok-Cari · Barsade · PARS (mobil) · Anlık Fiyat (kur) |

Bu dar bir ürün değil. Sitesi ise neredeyse boş: 7 yazılım ürününün detay sayfasının **hepsi "İçerik Hazırlanıyor"**, Referanslar sayfası boş, Video Galeri'de tek video gömülü değil. Gerçek kanıt üç yerden geldi: pusulax.com'daki 5 gerçek ekran görüntüsü, YouTube'daki 36 ders videosunun başlık+süre verisi, ve online mağazadaki ürün açıklamaları.

---

## 3. MENÜ BASİTLİĞİ — özellikle istenen kıyas

### Pusula'nın menüsü

Sol kenarda **7 ana başlık**, üstünde bir **"Menü Ara"** kutusu, altında "Destek Talebi Gönder":

> Tanımlamalar · Bilgi Girişleri · Stok Raporları · Cari Raporlar · Transfer · Fatura · Ayarlar

"Bilgi Girişleri" açıldığında altındaki 9 ekran görünüyor: Cari Hesap İşlemleri · Döviz Kurları Giriş · Ürün Barkodlama · Üretim Ekranı · Sayım İşlemleri · Fiyat Sorgulama · Kur Ekranı · Sertifika · Tamir.

**Perakende'nin tüm ana menüsünü anlatan eğitim videosu: 1 dakika 52 saniye.**

### Bizim menümüz (21 Eylül)

| | |
|---|---|
| Ana grup | **6** — Günlük İş (5) · Müşteri ve Stok (6) · Para ve Banka (10) · Belge ve Uyum (3) · Dökümler (6) · Yönetim (8) |
| Gruplu bağlantı | 38 + 2 alt blok (Kılavuz, Ayarlar) = ekranda **40** |
| Rol başına | SAHİP 38 · MÜDÜR 32 · MUHASEBE 30 · **TEZGÂHTAR 17** |
| Basit mod | 8 rota (tezgâhtarda fiilen 6) |
| Arama | Menünün en üstünde kalıcı arama kutusu + **Ctrl/Cmd+K** |
| Ek | Kullanıcı bazında favoriler, sürükle-bırak sıralama, Alt+↑/↓ |

### Dürüst kıyas

**Üst başlık sayısında biz daha azız: 6'ya 7.** Pusula'nın 7 dalının altında en az 27 yaprak var. Yani "Pusula 7, biz 38" kıyası elma-armut — bunu hasım turu çürüttü.

**Gerçek fark iki yerde:**
1. **Bizim gruplarımız katlanmıyor** — 6 başlığın altındaki 38 bağlantı hep açık duruyor. Pusula'da dallar kapalı başlıyor, ekranda 7 satır görünüyor.
2. **Pusula menüyü küçültmüyor, menüyü BAYPAS ediyor.** Günlük işin tamamı tek ekrandaki 13 düğmede; kullanıcı menüye gün boyu dönmüyor.

18 Eylül raporundaki "36 bağlantılık düz liste, gruplama yok" bulgusu **kapandı** — menü yeniden kuruldu, arama ve favoriler eklendi. Bu boyutta artık Pusula ile **başa başız**.

---

## 4. Pusula'nın satış ekranı — basitliğin asıl formülü

Ekran görüntüsünden okunan hâliyle:

- **Yalnızca 3 giriş alanı:** Hesap Adı (varsayılan "PEŞİN MÜŞTERİ") · Tarih · Para Birimi
- **13 büyük düğme:** AÇIK HESAP · BARKODLU SATIŞ · MAHSUP · NAKİT ÖDEME · NAKİT TAHSİLAT · BARKODSUZ SATIŞ · BARKODLU İADE · BARKODSUZ ALIŞ · BANKA · GİDEN HAVALE · AÇIKLAMA · İSKONTO · KOMİSYON
- **Tek grid:** GİRİŞLER ve ÇIKIŞLAR aynı tabloda. İki taraf eşitlenince altta fark **"0.00"** yazıyor, Kaydet'e basılıyor.
- Kalem kolonları: Ürün Kodu · Ayar · Miktar · Adet · Tutar · Döviz · Ödenecek Tutar
- Müşteri adının altında **bakiye rozeti** (HAS cinsinden, kırmızı)
- Fiyatlama: **Toplam Maliyet × 1,40 = Satış · × 2 = Etiket**

Sipariş→irsaliye→fatura→tahsilat zinciri yok. Kuyumcunun kâğıt fişi ekrana taşınmış. Uçtan uca satış dersi **3 dakika 54 saniye**; 36 dersin medyanı **74 saniye**, toplam eğitim **~45 dakika**.

Pusula'nın kendi pazarlama cümlesi de bunu söylüyor: *"Satış, Tahsilat ve İade aynı ekranda"* ve *"Karmaşaya değil, sadeliğe odaklanın."*

---

## 5. Pusula Perakende'nin işlem envanteri (27 ders videosu)

Döviz tanımlama · Ana menü · Altın ürün barkodlama (adet işçilikli) · Altın ürün barkodlama (gram) · Özel ürün barkodlama · Satış işlemi genel · Nakit satış · Nakit ödeme · Nakit tahsilat · Kredi kartı satış · Satış + hurda tahsilat + açık hesap · Altın giriş · **Hurda çıkış** · **Hurda alış** · Ziynet satış · Ziynet alış · Hesap tanımlama · Barkodsuz altın iade · Özel ürün alış (pırlanta/saat/gümüş/çelik) · **Döviz alış** · **Döviz satış** · Toptancıya barkodlu iade · **Müşteri ürün değişimi** · Satış iadesi · **Hurdadan vitrine ürün çıkarma** · **Barkodlu ürünü hurdaya atma** · Yedekleme

**Listede hiç geçmeyenler:** emanet · MASAK · KMTS · milyem terimi · e-Fatura · sipariş · bulut/abonelik.

---

## 6. Bizim 21 Eylül hâlimiz — 18 Eylül'den bu yana ne değişti

| | 18 Eylül | 21 Eylül |
|---|---|---|
| Ekran | 42 | **46** |
| Menü | 38 bağlantı, düz liste | **6 grup + arama + favoriler** |
| Migration | 49 | **65 dosya** (CLAUDE.md "61" diyor — belge geride) |
| API ucu | 273 | **332** |
| Tablo | 62 | **66** |
| Foreign key | 0 | **hâlâ 0** |

**Yeni 4 ekran, 18 Eylül'de "yok" dediklerimin tam karşılığı:** `/gider` (Gider ve Virman) · `/hurda` (Hurda ve Ramat) · `/vitrin` (Vitrin Panosu) · `/cikti/{Tur}/{Id}` (Belge/yazdırma).

**Satış için tıklama sayımız** (basit mod, tek kasa, nakit, barkod okutucuyla): **5 tık**. Elle ürün seçilirse 7.

---

## 7. Nerede eşitlendik, nerede kaybediyoruz, nerede öndeyiz

### Eşitlendiğimiz yer — kabuk ve menü
6 gruplu menü · kalıcı arama + Ctrl+K · favoriler · her ekranın tepesinde canlı kur şeridi · satış kaleminde Ayar·Milyem·Gram·Has kolonlarının ürün kartından otomatik gelmesi · müşteri kartında 2 zorunlu alan (Pusula'nın 3'üne karşı). **Pusula'nın beş sadelik kararının beşi bizde de var.**

### Kaybettiğimiz yer — hepsi tezgâhta, çoğu "kablo" işi

| Konu | Durum |
|---|---|
| **Müşteri zorunlu** | Satışta cari seçmeden kaydedilemiyor; "peşin müşteri" carisi yok. Pusula "PEŞİN MÜŞTERİ" ile açılıyor. Tezgâhtaki en büyük tek sürtünme |
| **Karma fişte altın bacağı yok** | Ödeme bacakları NAKİT·KART·HAVALE·VERESİYE·DÖVİZ. Nakit + eski altın — kuyumcudaki en sık ödeme — tek fişte girilemiyor. Pusula'nın vitrin fişi birebir bunu yapıyor |
| **Basit mod karma ödemeyi kapatıyor** | Karma ödeme anahtarı basit modda hiç çizilmiyor; tevsik haddi aşılınca nakit tamamen kapanıyor → 35.000 TL'lik bilezik 30.000 nakit + 5.000 kart olarak yazılamıyor |
| **P-21'in altı işlemi ekranda yok** | Rezerv · avans/kapora · **değişim** · konsinye çıkış/iade · sipariş→satış · ciro/rücu — sunucuda **1.285 satır + 14 uç hazır**, Web ağacında adı **sıfır kez** geçiyor |
| **Satıcı personel** | `hareketler.personel_id` kolonu ve indeksi var, rapor okuyor — ama hiçbir ekran yazmıyor; satış analizi her satırda "satan bilinmiyor" diyor |
| **Kaydet sonrası yazdır yok** | Belge motoru ve 4 belge türü hazır, tek giriş İşlem Defteri raporu |
| **Açık Pozisyon yok** | Pusula'nın patronu ekrana bağlayan tek sayısı. Kodda "Pozisyon" kelimesi 0 kez geçiyor; panoda yerinde hava durumu var |
| **Cari bakiyesi satışta görünmüyor** | "Bu adama veresiye verilir mi" için ekran değiştirmek gerekiyor |
| Barkod kutusuna otomatik odak · para üstü · e-Fatura · PWA | Hiçbiri yok |

### Önde olduğumuz yer
**Emanet** (Pusula'nın hiçbir sayfasında geçmiyor; bizde 630 satırlık ekran + 4 uç) · **MASAK kimlik tespiti** (185.000 TL eşiği) · **tevsik kapısı** · **gider pusulasının fail-closed kimlik tespiti** · KMTS · pırlanta 4C ve taş kökeni derinliği · reyonun stok düşüm kaynağı olması · **kur beslemesinin pakete dahil ve çok kaynaklı olması** · çok kiracılı web + MFA + denetim izi · rol bazlı menü daralması · basit/profesyonel mod.

Karşı tarafta Pusula'nın mobili dükkândaki yerel sunucuya IP ile bağlanıyor ve Google Play kaydı *"veriler şifrelenmiyor"* diyor.

---

## 8. FİYAT — Pusula + PusulaX/KiviERP toplamı ↔ biz

Pusula'nın fiyatları **USD bazlı** (mağazanın baz para birimi USD; TL yalnız gösterim). Lisans **tek seferlik, program başına** ve **USB dongle** ile kilitli.

### Pusula ekosisteminin 3 yıllık sepeti — tek şube, barkodlu perakende

| Kalem | Bedel | Tür |
|---|---|---|
| Pusula Perakende lisansı | 1.500 USD = **73.203 TL** | tek seferlik |
| Anlık Fiyat (canlı kur) | 177 USD/yıl × 3 = **25.914 TL** | yıllık abonelik — *ürün açıklaması "İçerik Hazırlanıyor", ne verdiği yazmıyor* |
| PusulaX ↔ KiviERP (e-Fatura + MASAK + banka) | 18.900 TL+KDV/yıl × 3 = **68.040 TL** | yıllık abonelik |
| Zebra ZD220 yazıcı + Eyoyo okuyucu | **20.740 TL** | donanım |
| Etiket ruloları (3 yıl, muhafazakâr) | ~**6.000 TL** | sarf |
| **Bakım anlaşması** | **ilan edilmemiş** | PusulaX SSS: uzaktan destek için **zorunlu** |
| Kurulum + eğitim | **ilan edilmemiş** | lisansa dahil değil |
| **3 yıl toplam (bilinenler)** | **≈ 193.900 TL** | + bakım + kurulum/eğitim |

### Bizim tarafımız — aynı 3 yıl

| Kalem | Bedel |
|---|---|
| Tek Şube kademesi (F-8 önerisi, 1.200–1.500 TL/ay) | 43.200 – 54.000 TL |
| Canlı kur | **0 TL** — pakete dahil |
| e-Fatura/e-Arşiv entegrasyonu | pakete dahil *(kodda henüz yok — bkz. § 10)* |
| MASAK + KMTS + tevsik + gider pusulası + denetim izi | **dahil, ayrı ürün gerekmiyor** |
| Kullanıcı / modül başına ücret | **0 TL** |
| Kurulum ücreti | 0 TL |
| Donanım + sarf (aynı) | 26.740 TL |
| **3 yıl toplam** | **≈ 69.940 – 80.740 TL** |

**Fark: ~113.000–124.000 TL — %58–64 tasarruf.** Üstelik Pusula'nın bakım anlaşması ve kurulum/eğitim bedeli ilan edilmediği için **onun lehine sıfır sayılarak**.

### Fiyatın arkasındaki stratejik gerçek

Pusula müşterisi bugün **iki firmadan üç ayrı sözleşme** taşıyor: Pusula lisansı + kur aboneliği + KiviERP uyum aboneliği. Dahası **KiviERP Kuyum SuperApp'in kendisi de** milyem/has hesabı, canlı kur, gram envanter ve 5549 kimlik doğrulaması yapıyor — yani Pusula'nın çekirdeğiyle örtüşüyor. Müşteri aynı işi iki kez satın alıyor.

Ek not: Pusula'nın yazılım fiyatı artık online satılmıyor — HTML'de fiyatı ve "Sepete Ekle"yi gizleyen CSS var, yerine **"Bilgi Almak İstiyorum"** konmuş. Yazılım tekrar teklife dönmüş.

---

## 9. Hasım turu — kendi ekibimizin çürüttükleri

24 iddia sınandı, **7'si çürüdü, 3'ü bizim lehimize düzeldi**:

| İddia | Gerçek |
|---|---|
| "115 ham input / 73 ham select kaldı" | **Yanlış** — gerçek 38/29. Rakam derlenmeyen 5 mutasyon artığını sayıyor; form göçü sanılandan ileride |
| "Emanet kodda hiç yok" | **Yanlış** — 630 satırlık çalışır ekran + 4 uç var. En güçlü kartımızı yanlışlıkla silmişiz |
| "Pusula 7 başlık, biz 38" | **Elma-armut** — bizde de 6 üst başlık var; Pusula'nın 7 dalının altında en az 27 yaprak |
| Depodaki 16 Eylül araştırması Pusula'ya e-Fatura ve bulut atfediyor | **Yanlış** — ana sitede iki kelime de geçmiyor; e-Fatura yalnız PusulaX'te, KiviERP ortaklığıyla |
| Pusula büyüklüğü "~3.000 firma" | **4.000 üzeri** (kendi beyanı) |
| API uç sayısı 287 / 325 | İkisi de yanlış — **332** |
| "Vitrin marjında doğrulama yok" | Sunucuda var; eksik olan ekran sınırı |

**Ayakta kalan en acı üç iddia:** P-21'in altı işleminin ekranda hiç olmaması · personel_id'nin hiçbir ekranca yazılmaması · kaydet sonrası yazdır bağlantısının olmaması.

---

## 10. Ne yapmalı — sıralı liste

**Bu listenin tamamı mevcut dosyalarda birkaç satır. Yeni modül, yeni tablo, yeni migration gerektirmiyor.**

| # | İş | Maliyet |
|---|---|---|
| 0 | **ACİL:** `/urunler?limit=200` ucuna `&aktif=true` ekle — pasife alınmış ürün şu an POS'ta seçilip satılabiliyor (bkz. § 11) | 1 karakter |
| 1 | Varsayılan **"Peşin Müşteri"** carisi + satışta ön-seçili gelmesi. Nakit satış 5 tıktan 3 tıka iner | yarım gün |
| 2 | **P-21'in altı düğmesi** — Rezerv · Avans · **Değişim** · Konsinye çıkış · Siparişten satış · Ciro/rücu. Sunucu hazır, yalnız ekran | 3–5 gün |
| 3 | Kaydet sonrası **"Fiş Yazdır"** bağlantısı (`/cikti/...` sayfası zaten var) | yarım gün |
| 4 | Barkod kutusuna **otomatik odak** (mekanizma projede mevcut) | 1 saat |
| 5 | Cari seçilince **bakiye rozeti** (HAS + TL) — veri ekranda zaten yüklü | yarım gün |
| 6 | Karma ödemeye **ALTIN bacağı** — önce bugünkü davranışı belgele, ekip oyuyla mühürle, sonra bir satır ekle | 1 gün + karar |
| 7 | Karma ödemeyi **basit modda aç** (`ZorunluGorunur` bağla) | bir parametre |
| 8 | Fişe **satıcı personel** alanı — kolon ve rapor hazır, yazan yok | 1 gün |
| 9 | Panoda hava durumunun yerine **"Açık Pozisyon"** kutucuğu | 2 gün |
| 10 | Nakit satışta **para üstü** önizlemesi | yarım gün |
| 11 | Hurda alışta tekrar eden satıcıyı hatırla — 8 alan 3 alana iner | yarım gün |
| 12 | Ana ekrandaki ölü bağlantı: `/islem-defteri` → `/raporlar/islem-defteri` | bir kelime |
| 13 | Basit mod çelişkisini kapat — tezgâhtar 8 rota yazan listeden fiilen 6 görüyor | bir karar |
| 14 | Kur şeridinde **HAS'ı öne al** (kuyumcu HAS'la düşünür) | iki blok yer değişimi |
| 15 | **Öğrenme süresi hedefi koy:** menü anlatımı < 2 dk, tam satış < 4 dk, toplam eğitim < 45 dk — Pusula'nın rakamları | sürekli ölçü |

### Kurucuya kalan iki karar

1. **e-Fatura entegratörü** — kodda sıfır satır. Pusula bile kendi yazmamış, KiviERP ortaklığıyla çıkarmış. Sözleşme + kontör = para kalemi, ekip kararıyla kapanmaz.
2. **Fiş başlığı tablosu** — bilinçli olarak yok (gerekçe: tevsik kümülatif toplamını bozar). Ama fiş iskontosu yüzdesi, fiş notu, para üstü, yuvarlama asılacak yer bulamıyor. Seçenek: `hareketler`'i bozmadan yalnız fiş üstverisi taşıyan yan tablo. Mevcut gerekçe ciddi, hafife alınmamalı.

---

## 11. Acil bulgu (Anayasa md. 1 gereği bildirilir)

QA turu 7'nin beş kritiğinden **biri hâlâ açık ve canlıda**: `Islemler.razor:1910` ürün listesini `aktif` süzgeci **olmadan** çekiyor. Sunucu bu süzgeci destekliyor ve cari tarafı bir satır yukarıda zaten `aktif=true` ile düzeltilmiş. **Pasife alınmış bir ürün bugün POS'ta seçilip satılabiliyor.** Düzeltme tek parametre.

---

## 12. Kanıt ve sınırlar

- **Kendi tarafımız:** tüm sayılar 21 Eylül'de dosyadan sayıldı, her bulgu `dosya:satır` kanıtlı. Kurucu talimatı gereği **hiçbir test/derleme çalıştırılmadı** — bu yüzden CLAUDE.md'deki "3636 test" iddiası bu raporla **doğrulanmamıştır**. Aynı belgedeki "61 migration" satırı bayat: dizinde 65 dosya var.
- **Pusula tarafı:** 5 gerçek ekran görüntüsü, 36 ders videosunun başlık+süre verisi, mağaza ürün açıklamaları ve firma sayfaları. **Videoların hiçbiri izlenmedi**, Pusula'nın veri modeli hiç görülmedi, *"bir satış Pusula'da kaç tıkla bitiyor"* sorusunun sayısal cevabı **yok**.
- **"Basit" algısı:** menü yüzeyinin küçüklüğü, öğrenme süresinin dakikayla ölçülebilmesi ve satış ekranındaki 3 giriş alanı **kanıtlandı**. Buna karşılık bu algı **bağımsız kullanıcı metniyle desteklenmiyor**: bulunabilen tek gerçek kullanıcı yorumu (Google Haritalar, 7 yıl önce) tam tersini söylüyor ve Pusula'nın App Store'daki 4 uygulamasının toplam oy sayısı 7.
- **Fiyat:** Pusula rakamları kendi mağazasından, KiviERP rakamı kendi fiyat sayfasından alındı. Bizim rakamlar **F-8 birim ekonomi dosyasındaki öneridir, ilan değildir** — mühür kalkmadan hiçbir kanalda fiyat açıklanamaz. F-8'de **destek maliyetimiz ölçülmemiş** durumda; gerçek marj o satır ölçülünce netleşir.
- Pusula'nın PARS mobil uygulaması pazarlandığı gibi değil: App Store'daki canlı ekran görüntüleri 2015 tasarımı bir **rapor görüntüleyici** ve listede "DenemeRapor", "StringDeneme" gibi geliştirici test kayıtları duruyor; Android sürümü Google Play'den kalkmış. PusulaX'in mobil ekranları ise **mockup** ("Merhaba, Roberto" yer tutucu adlarıyla).
