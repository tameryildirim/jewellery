# Kuyumcu ERP — Piyasa Araştırması ve Satılabilirlik Değerlendirmesi

**Tarih:** 2026-09-11 · **İsteyen:** Kurucu · **Yöntem:** piyasa sıfırdan tarandı, proje ölçüldü (tahmin yok).
**Karar dosyası değil — değerlendirmedir.** Resmî git/no-git kararı ekip oyuyla mühürlenmeli.

---

## 0. KISA CEVAP

**Bugünkü hâliyle satılamaz.** Teknik çekirdek sağlam ve sektöre özgü en zor kısım (has altın muhasebesi) doğru çözülmüş; ama **yasal zorunlulukların hiçbiri yok** ve pazar sandığımızdan çok kalabalık.

Bitmiş ürünün kabaca **%40'ı** elimizde. Eksik %60'ın büyük kısmı "güzel olurdu" değil, **"olmadan satılamaz"** cinsinden.

---

## 1. İLK ARAŞTIRMAMIN HATASI — düzeltiyorum

`arastirmalar/KUYUMCU-YAZILIM-FIRSAT-2026-09-07.md` bu işi *"nadir ama en önemli programlardan biri"* diye çerçeveledi ve rakip olarak esas M&L ile birkaç küçük bulut oyuncusunu saydı.

**Bu yanlıştı.** Bu turda tek oturumda bulunan, kuyumcuya yazılım satan firmalar:

M&L Yazılım · Kuyumcum · Kuyum360 · Ariyen · ZarifSoft · Gramino ERP · KuyumPOS · KuvvemERP · KuyumBase · SarrafPro · Dijigold · Kuyumsoft · Dara (Atom Barkod) · Expert Barkod · 360 Jewelry · Bakiyem · Dezy · Kuyumcu App

**On sekiz oyuncu.** Pazar "nadir" değil, **doygun ve rekabetçi.** Bu, projenin değersiz olduğu anlamına gelmez — ama "boş alan var, girip kapalım" varsayımı çöktü. Farklılaşma zorunlu.

**Ayrıca:** yapmayı konuştuğumuz TV vitrin panosu **zaten rakipte var.** ZarifSoft kendi sayfasında birebir şunu yazıyor: *"Vitrin ekranı (TV modu) için canlı kur gösterimi."* Pano bir **fark yaratıcı değil, eksik kapatıcıdır.** Faz A kararı geçerli ama beklentiyi buna göre ayarlamak lazım.

---

## 2. PAZAR — doğrulanmış rakamlar

| Ölçü | Değer | Kaynak |
|---|---|---|
| Perakende kuyumcu mağazası | **~35.000** | İstanbul Kuyumcular Odası |
| Üretici | ~5.000 | İKO |
| Sektör iş hacmi | ~10 milyar $/yıl | Dünya Gazetesi |
| İstihdam | ~250.000 kişi | İKO |
| M&L müşterisi | **~7.000 firma**, 81 il, 42 ülke, 1993'ten beri | mlyazilim.com |

M&L tek başına pazarın **~%20'sini** tutuyor ve 33 yıllık. Sektörde yerleşiklik ve güven çok ağır basıyor.

**Fiyatlandırma:** taranan rakiplerin **hiçbiri** web sitesinde fiyat yayınlamıyor (M&L, Kuyumcum, ZarifSoft, Gramino, KuyumPOS — hepsi "iletişime geçin"). Bu sektörde **gizli fiyatlandırma norm.** Bizim fiyat stratejimiz için dışarıdan referans alınamaz; pilot görüşmesinde öğrenilecek.

---

## 3. YASAL ZORUNLULUKLAR — 2026'nın asıl hikâyesi

| Yükümlülük | Somut kural | Projede |
|---|---|---|
| MASAK kimlik tespiti | **185.000 TL** ve üstü işlemde kimlik zorunlu | **YOK** |
| MASAK işlem beyanı | 200.000–2.000.000 TL: amaç + **en az 20 karakter** açıklama | **YOK** |
| MASAK beyan formu | 2.000.001–20.000.000 TL: nakit işlem beyan formu; 20 M+ ek belge | **YOK** |
| Şüpheli işlem bildirimi | Tutardan bağımsız, **10 iş günü** içinde | **YOK** |
| Belge saklama | **~8 yıl** denetime hazır | **YOK** |
| KMTS | **Nisan 2026'dan itibaren zorunlu**, güvenlik etiketi/takip | **YOK** |
| e-Fatura / e-Arşiv | GİB entegrasyonu | **YOK** (Fatura modülü tamamen dahili, 4 uç, GİB bağlantısı sıfır) |

Kod taraması: `masak`, `kmts`, `kimlik tespit`, `şüpheli`, `gib`, `efatura`, `earsiv` kelimelerinin **hiçbiri** projede geçmiyor — **0 dosya.**

**Bu kalemler pazarlama özelliği değil, satış önkoşuludur.** e-Fatura'sı olmayan bir kuyumcu programı bugün kurulamaz; MASAK'sız olan da kuyumcuyu cezaya açık bırakır.

Acı ironi: **ilk araştırmamızın "2026 fırsat penceresi" dediği şey tam olarak bunlardı — ve proje bunların hiçbirini içermiyor.**

---

## 4. PROJENİN GERÇEK ÖLÇÜSÜ

| Ölçü | Değer |
|---|---|
| Kod | **18.772 satır** (C# + Razor, bin/obj hariç) |
| API modülü | 15 |
| HTTP ucu | **81** (34 GET, 26 POST, 15 PATCH, 5 DELETE, 1 PUT) |
| Blazor sayfası | 13 |
| Veritabanı tablosu | 16 |
| Migration | 3 (temiz kurulabiliyor) |
| Test | **79** (+ pano 34 dahil **124**), gerçek MySQL'e karşı |

Modül büyüklükleri: Kur 1103 · Satış 764 · Cari 538 · Ürün 437 · Kullanıcı 374 · Banka 369 · Auth 304 · Emanet 268 · Fatura 257 · Rapor 247 · Kasa 224 · Pano 205 · Yetki 186 · Şube 134 · Admin 90.

### Güçlü yanlar — gerçekten iyi olanlar

1. **Has altın paralel defteri.** `hareketler` hem TL hem has gram taşıyor, her işlem bir kur anlık görüntüsüne bağlı. Sektörün en zor ve en çok yanlış yapılan kısmı bu ve **doğru kurgulanmış.**
2. **Milyem/ayar alan modeli** K8–K24 ve G800–G950 ile tam; has gram 6 ondalık.
3. **124 test gerçek MySQL'e karşı** koşuyor. InMemory (transaction yok) ve SQLite (decimal hassasiyeti yok) gerekçesiyle reddedilmiş — bu olgun bir karar.
4. **Çok kiracılı yapı**, JWT + SHA-256 hash'li dönen refresh token, BCrypt parola.
5. **Canlı kur**: 3 kaynak (Altınkaynak, Ankara Altın Borsası, Truncgil), kanonik parite sözlüğü, kaynak seçimi, 10 sn tazeleme, SignalR push.
6. Faz 0'da **altı kritik hata kanıtlanıp düzeltildi** (kiracı sınırı ihlali, çift/üç kayıt, işaret çelişkisi, UTC gün sınırı, para birimi körlüğü).
7. `Kilavuz.razor` — kullanım kılavuzu sayfası var.

### Eksikler — rakiplerde var, bizde yok

Kod taramasıyla doğrulandı (0 dosya):

| Eksik | Kimde var |
|---|---|
| **e-Fatura / e-Arşiv** | ZarifSoft (açıkça yazıyor), sektörde standart |
| **MASAK uyum** | SarrafPro'nun tüm konumlandırması bunun üstüne |
| **KMTS** | Nisan 2026'dan beri zorunlu |
| **Barkod / RFID** | M&L (MLBarkod, MLAges-RFID), Ariyen, Kuyumcum, Dara, Expert Barkod |
| **Toptan modülü** | Kuyumcum'da ayrı modül |
| **Personel / prim** | Kuyumcum, Gramino |
| **Masraf / gider** | Kuyumcum (has altın cinsinden masraf raporu), Gramino |
| **İmalat / atölye** | M&L |
| **Tamirat takibi** | M&L |
| **Sertifika / katalog** | M&L (MLSertifika) |
| **Mobil uygulama** | M&L, Kuyum360, Kuyumcum, Bakiyem, Dezy |
| **Sayım (RFID'li)** | Kuyumcum |

### Teknik borç ve açık kusurlar

1. **Global kiracı sorgu filtresi YOK** — `HasQueryFilter` sayısı **0**. Mehmet'in Faz 0 vetosu hâlâ açık. Şu an izolasyon her sorguda elle `TenantId` yazılmasına bağlı; bir geliştirici unutursa veri sızar. Faz 0'da bu tam olarak kanıtlanmıştı.
2. **Oran sınırı YOK** — `AddRateLimiter` 0. Pano oturumsuz uç açacağı için bu kritikleşiyor.
3. **`UseHttpsRedirection` YOK**, **`AddResponseCompression` YOK** (Blazor 9,26 MB sıkıştırılmadan gidiyor), **`AddHealthChecks` YOK**.
4. **Panel'deki aylık grafik kalıcı olarak boş.** Doğrulandı: `Panel.razor:230` `aylik.Gunler` bekliyor, `RaporModulu.AylikOzet` (satır 106-115) dönüşünde **`gunler` alanı yok.** Müşteriye gösterilecek ilk ekranda çalışmayan bir grafik var.
5. Pano modülü 205 satır ama **0 HTTP ucu** — hesap motoru hazır, dışarı açılmadı (bilinçli; Faz A devam ediyor).

---

## 5. SATILABİLİRLİK — kalem kalem

| Kriter | Durum |
|---|---|
| Çekirdek muhasebe doğru mu? | **Evet** — sektörün en zor kısmı çözülmüş, testli |
| Yasal olarak kurulabilir mi? | **Hayır** — e-Fatura yok, MASAK yok, KMTS yok |
| Rakiplerle özellik paritesi? | **Hayır** — barkod, toptan, personel, masraf, imalat, tamir, mobil yok |
| Çok kiracılı SaaS olarak güvenli mi? | **Hayır** — global kiracı filtresi yok (açık veto), oran sınırı yok |
| Demoya çıkarılabilir mi? | **Kısmen** — boş grafik ve yanıltıcı "Net Kasa" kartı düzeltilirse evet |
| Pazarda yer var mı? | **Dar** — 18 rakip, lider %20 pay ve 33 yıl kıdemli |

---

## 6. NE YAPILIRSA SATILABİLİR

**Kapı 1 — yasal (bunlar olmadan satış konuşulamaz):**
1. e-Fatura / e-Arşiv entegrasyonu (özel entegratör üzerinden)
2. MASAK: 185.000 TL kimlik eşiği, kademeli beyan formları, 10 iş günü şüpheli işlem akışı, 8 yıl saklama
3. KMTS uyumu

**Kapı 2 — rekabet paritesi:**
4. Barkod (RFID sonraya bırakılabilir)
5. Masraf/gider ve personel modülleri — ikisi de küçük, hızlı kazanç
6. Toptan akışı

**Kapı 3 — güvenlik (Mehmet'in vetoları):**
7. EF Core global kiracı sorgu filtresi
8. Oran sınırı + HTTPS yönlendirme + sıkıştırma + sağlık ucu

**Kapı 4 — cila:**
9. Aylık grafiğin `gunler` alanı, "Net Kasa" kartı
10. Pano Faz A'nın kalanı

**Dürüst tahmin uyarısı:** bu listeye süre biçmedim. Geçmiş hızımızdan çarpan uydurmak Madde 1'e aykırı olurdu; istersen kalem kalem kırıp gerçekçi tahmin çıkarırım.

---

## 7. FARKLILAŞMA — nerede kazanabiliriz

Rakiplerin çoğu ya masaüstü-eski (M&L) ya da hafif bulut. İki boşluk göze çarpıyor:

1. **MASAK'ı gerçekten otomatikleştirmek.** SarrafPro bu konuda blog yazıyor; eşikleri, formları, 10 günlük sayacı ve 8 yıllık arşivi *uygulamanın içinde* çalıştıran bir ürün gördüğümüze dair kanıt bulamadım. Kuyumcunun asıl korkusu ceza — orası ödemeye razı olduğu yer.
2. **Has altın defterinin doğruluğunu ispatlanabilir kılmak.** Bizde 124 test ve paralel defter var. Rakiplerin hiçbiri test/denetlenebilirlik iddiasında bulunmuyor. Bu, "sizin programınız hesabı kaçırmıyor mu?" sorusuna verilecek tek somut cevap.

**⚠️ DÜZELTME (2026-09-11, Eray'ın araştırması):** 1. maddedeki "kimsede yok" tespiti **YANLIŞ ÇIKTI.** SarrafPro'nun tüm konumlandırması *"Banka, e-Fatura ve MASAK Uyum Platformu"*; ayrı "MASAK Sorgusu" özellik sayfası ve *"Şüpheli İşlem Bildirimi Artık Otomatik"* duyurusu var. KuyumBase de *"MASAK ve KMTS uyumu, şüpheli işlem takibi, MASAK/AML bildirimleri"* iddia ediyor. **"İlk biz yaparız" savunulamaz.** İddia en fazla "biz de yaparız, has altın defterimiz daha sağlam" olabilir. Kaynaklar: sarraf.pro/ozellikler/masak-sorgusu · kuyumbase.com/ozellikler. Sınır: pazarlama metni, demo doğrulaması yok — K3 daha da acil. Ayrıntı: `SIRADAKI-ADIM-KARAR-2026-09-11.md` §1.

---

## 8. SONUÇ

Elimizdeki şey **satılabilir bir ürün değil, üzerine ürün kurulabilecek sağlam bir çekirdek.** En zor kısım (has altın muhasebesi, çok kiracılılık, canlı kur) bitmiş ve testli durumda; ama ürünü satılabilir kılan yasal katman hiç başlamamış.

İki yol var:
- **Devam:** yukarıdaki 4 kapı kapatılır. Çekirdek harcanmaz, ama yol uzun ve pazar kalabalık.
- **Daralt:** tam ERP yarışından çekilip **"MASAK + has altın uyum"** ürününe odaklanılır, mevcut ERP'lerin yanına kurulur. Rekabet çok daha az, çekirdeğimizin güçlü olduğu yer tam burası.

Tavsiyem **ikincisini ciddiye almak.** Ama bu bir strateji kararıdır; ekip oyuna götürülmeli.

**Kurucu'ya kalanlar:** rakip demo erişimi, e-Fatura özel entegratör sözleşmesi, kaynak kullanım izinleri, pilot kuyumcu.

---

### Kaynaklar
- https://mlyazilim.com/ · https://www.mlyazilim.com/ml_gold_erp.html
- https://kuyumcum.net/TR/Ozellikler/
- https://zarifsoft.com/
- https://graminoerp.com.tr/
- https://kuyumpos.com/ (HTTP 403 — içerik alınamadı)
- https://kuvvem.com/sektor/kuyumcu-magaza-programi/
- https://sarraf.pro/blog/kuyumcular-hangi-islemleri-masak-a-bildirmek-zorunda-2026-guncel-kurallar
- https://ms.hmb.gov.tr/uploads/sites/7/2023/12/KMTSUYGULAMASINAILISKINUSULESASLAR.pdf
- https://www.iko.org.tr/sektor/turkiye-de-kuyumculuk
- https://www.dunya.com/kose-yazisi/kuyumculuk-sektoru-10-milyar-dolarlik-bir-sektor/356977
- https://kuyum360.com.tr/blog/en-iyi-kuyumcu-mobil-uygulamasi.html
- https://ariyenkuyumcu.com/blog/kuyumcular-hangi-programi-kullaniyor
