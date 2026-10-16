# PAKET YILDIZ-4B — uygulama raporu (09.10.2026)

## TUR 2 (09.10 ~15:00–16:10) — tasarım hakeminin 4 KRİTİK bulgusu kapatıldı

Kapsam yalnız görünüm (hasımsız paket): para/veri/yasal kural, gövde, POST yolu DEĞİŞMEDİ (Y4B_23 gövde birebir testi yeşil). COMMIT YOK · migration YOK · mevzuat hükmü YOK.

| # | Bulgu (hakem) | Yapılan | Kanıt (gerçek başsız Chrome, 1440×900) |
|---|---|---|---|
| 1 | Barkod alanı açılışta odak alıyor; `.yz-alan:focus-within` kutu çerçevesi (outline 1px) tuvalde yok | Yalnız Yıldız: `.yz .yz-alan:focus-within { outline: 0; border-bottom-color: var(--metin); }` (tuval `.alan input{outline:0}`; odak göstergesi tuval kalıbında: alt çizgi `--p` → `--m`) | koyu PRO: `aktif=islem-barkod · outline=none 0px · alt çizgi rgb(243,242,239)=--metin`; odaksız `rgb(201,198,190)=--kenarlik`. Açık BASİT: odakta `rgb(20,20,20)`, odaksız `rgb(138,135,128)` |
| 2 | Kaydet hatası ve satış sonrası bant ekran dışında (y=916) | Yıldız tezgâhında hata, son fiş, yazdır bağlantısı, yerleşim/taksit uyarısı **Fark bloğunun not satırı kalıbında** (`#yildiz-tezgah-hata` --kotu rol=alert; `#yildiz-tezgah-son-fis`, `#yildiz-tezgah-yazdir` b2 altı çizili). Hata çizilince odak oraya taşınır (`kuyumcuOdak.alana`, kaydırır). Özgün bantlar (`#hata-bandi`, `#basari-bandi`, `#islem-yazdir-sar`, …, `#islem-yasal-kapi`) Yıldız işlem kabında `display:none` (DOM'da kalır; id'ler değişmedi) | Stok aşan fiş (925 Gümüş Kolye ×6, stok 2): koyu PRO ve açık BASİT `#yildiz-tezgah-hata x=996 y=683 h=65 ekranda=true`, `activeElement=yildiz-tezgah-hata`, scrollY=0, sayfa 900, Kaydet y=804 ekranda. 375 koyu BASIT: hata y=589 ekranda (odakla kaydı). Başarılı satış: `#yildiz-tezgah-son-fis y=708 "Son fiş: Satış · 14:55 · 6.908,72 ₺"`, `#yildiz-tezgah-yazdir y=736` href `/cikti/SATIS_FISI/01M4G89AG1MMVZXN7EF7A1JY90?yazdir=1` |
| 3 | "Değiştir" ve yasal eşik eski kalıpla sayfa üstünde; Kaydet y=986; kimlik formu fişin altında; Kaydet kilitliyken ✓ | (a) Müşteri satırı (sayfanın FisMusteriSatiri parçası, TEK tanım `MusteriSatiriParcasi`) Yıldız'da sol sütunda **"Değiştir"e bağlı mutlak konumlu katman** (`#yildiz-tezgah-musteri`, akışı itmez), seçici tuval `.alan` kalıbında (alt çizgi, 22 px/300, ok yok, kutu yok), seçenekler `.sat` kalıbında; bakiye rozeti gizli (aynı bilgi üst satırda). (b) Yasal kapı bandı Yıldız'da çizilmez; cümle Fark not satırında (BASİT'te — özgünde bant yalnız BASİT'te). (c) MASAK bloğu (TEK tanım `MasakParcasi`, kapı AYNI) Yıldız'da **"+ Kimlik bilgisi"** hapıyla açılan **çekmecede** (`Cekmece Varlik="yildiz-kimlik"`). (d) `FarkTamam` artık `Gonderilebilir` ister: Kaydet kilitliyken ✓ yok | 332.823,07 ₺ fiş, koyu PRO: Peşin eşik üstü → katman kendiliğinden açık `x=64 y=168 w=852`, Kaydet `y=804 ekranda`, sayfa 900, `#islem-yasal-kapi` yok, ✓ 0. Gerçek müşteri seçilince katman kapanır, `#yildiz-tezgah-kimlik y=736`, Kaydet `y=804 disabled`, not "Kimlik ve işlem amacı eksik: …", ✓ 0, akışta `.kart--masak` 0, sayfa 900. Hap → çekmece `x=960 w=480 h=900`, içinde MASAK formu 1. Açık BASİT aynı ölçüler |
| 4 | Tasarım notu satırları ekranda (`#yildiz-tum-not`, `#yildiz-musteri-not`) | İki satır ve sözlük girdileri (`YildizEkranSozlugu.Not`, `YildizMusteriSozlugu.Not`) kaldırıldı | DOM'da yok (Y4B_13/14). Kıyas eşlemesinden "alt not" öğesi çıkarıldı (Müşteriler 28→27, Tüm ekranlar 68→67) |

### Tur 2 tuval-kiyas (r5, son kodla — 12 koşum)
| Ekran | koyu·BASİT | açık·BASİT | koyu·PRO | açık·PRO | taşma |
|---|---|---|---|---|---|
| Tezgâh | 36/36 | 36/36 | 36/36 | 36/36 | 0 |
| Müşteriler | 27/27 | 27/27 | 27/27 | 27/27 | 0 |
| Tüm ekranlar | 66/67 | 66/67 | 66/67 | 66/67 | 0 |
Tüm ekranlardaki tek FARK `main` yüksekliği: tuval 1856,5 ↔ uygulama 1792,2 px — kaldırılan tasarım notu satırının yeri (bulgu 4'ün doğrudan sonucu; yazı ölçüleri AYNI). Tablolar `ham/yildiz-4b/r5-*-tablo.md`, yan yana `r5-*-yan-yana.png`. Tezgâh kıyası barkod odağı bırakılmış (blur) hâlde ölçer; odak hâli yukarıdaki satır 1'de ayrıca ölçüldü.

### Tur 2 sıkışma ölçümü (sikisma-olc.js) — /islemler, /cariler, /ekranlar × 1440·1280·375 × BASİT·PRO × açık·koyu (36)
K1 0 · K3 0/0 · K5 0 · K6 0 · K9 0 · K10 0 · K11 0 · K12 0 · yatay 0 (hepsi). Bilinen ve tur 1'deki gerekçesi aynı kalanlar: K7 `/cariler` 1440/1280 = 1 (tuvalin kendi `align-items:flex-end` başlık satırı), K8 BASİT `/ekranlar` = 1 ("Kâr-zarar" — ölçerin `[a-z]+-[a-z]+` kalıbı â'yı tanımıyor), K4 tren `/ekranlar` 2,1–5,3 ekran × 6 bölüm (tuval 39 kare). Çıktılar `ham/yildiz-4b/tur2/olc-{pro,basit}-t2.out`.

### Tur 2 — 8 eski şema FARK 0
Kiracı şeması geçici platin; `arac/fark.mjs`: **BASİT 48/48 FARK 0, PRO 48/48 FARK 0** (3 karşılaştırma ısınma nedeniyle yeniden çekildi, ikinci çekimde eşit). Şema sonra yildiz'e alındı. Razor tarafı: başka şemada özgün fiş (Y4B_21) — müşteri satırı ve MASAK parçası özgün yolda AYNI işaretleme (tek tanım).

### Tur 2 görüntüler (Read ile bakıldı: 4)
`ham/yildiz-4b/tur2/s2-koyu-pro-1.png` (stok hatası Fark bloğunda), `s4-koyu-pro-1.png` (müşteri katmanı, ilk hâli kutulu seçiciydi → kimlik ağırlıklı kurallarla düzeltildi, ikinci çekim tuval .alan kalıbında), `s2-acik-basit-1.png` (açık BASİT; görülen "yasal kapı" genel cümlesi → hata satırı varken yazılmıyor, düzeltildi ve yeniden ölçüldü). Diğer senaryo görüntüleri aynı dizinde (s1, s3, s4 adımları, 375).

### Tur 2 testleri
- Yeni (Yildiz4BTestleri): Y4B_40 (odak CSS), Y4B_41 (hata Fark bloğunda + rol/alert + tabindex + odak çağrısı + genel yasal cümle yok; BASİT/PRO), Y4B_42 (son fiş + yazdır Fark bloğunda; BASİT/PRO), Y4B_43 (özgün bantlar gizli), Y4B_44 (Değiştir katmanı, sayfa üstüne açılmaz), Y4B_45 (kimlik çekmecede, ✓ kilitliyken yok) = 6 test / 8 koşum. Y4B_13/14 güncellendi (not satırları YOK). Sahte API: 400 yanıtı ve eşik parametresi, POST yanıtına `hareketId`.
- `Yildiz4B` 20 → **28/28**; `Yildiz|Ekranlar|BasitModPos|CariEkran|T5_Islemler` **260/260**.
- Islemler.razor satır tavanı (T5, ≤1400): 1398 → **1397**.

### Tur 2 mutasyon (kaynak `mut-yedek/` → perl tek eşleşme → `Yildiz4B` → geri kopya → `cmp` AYNI)
| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M10-01 | alan odağı yeniden kutu çerçevesi | Y4B_40 |
| M11-01 | hata satırı çizilmez | Y4B_41 (BASİT, PRO) |
| M12-01 | odak yanlış öğeye | Y4B_41 (BASİT, PRO) |
| M13-01 | son fiş satırı düşer | Y4B_42 (BASİT, PRO) |
| M14-01 | yasal kapı bandı Yıldız'da görünür | Y4B_43 |
| M15-01 | müşteri satırı yine sayfa üstünde | Y4B_24, Y4B_44 |
| M16-01 | MASAK bloğu yine fişin altında | Y4B_45 |
| M17-01 | kilitliyken ✓ | Y4B_45 |
| M18-01 | Tüm ekranlar notu geri | Y4B_14 |
| M19-01 | çekmece kendiliğinden açık | Y4B_45 |
| M20-01 | hata varken genel yasal cümle | Y4B_41 (BASİT) |
| M21-01 | Müşteriler notu geri | Y4B_13 |
12/12 kırmızı, hepsi geri alındı `cmp` AYNI; ardından temiz `dotnet build Kuyumcu.sln` **0 hata 0 uyarı**.

### Tur 2 tam süit (tek koşum, son kodla; `trx-pYILDIZ-4B/tur2.trx`)
11827 toplam · 11651 geçti · **173 başarısız** · 3 atlandı. Yıldız testlerinin hepsi yeşil (Yildiz4B 28/28; tur 1'in tek kırmızısı OL6 artık yeşil). 173 kırmızının **hiçbiri bu paketin dosyalarına dayanmıyor**:
- 166'sı iletisinde "Bu has fiyatı güncel has fiyatına göre olağan aralığın dışında" / "Bu fiyat olağan aralığın dışında (500–50.000 TL)" — `Kuyumcu.Core/Domain/Girdi/FiyatBandi.cs`, `GirdiHatasi.cs`, `Sozlukler.Girdi.cs`, `Sozlukler.IslemBant.cs` (15:14–15:16) ve `Kuyumcu.Api/Modules/Satis/IslemFiyatKapisi.cs`, `SatisModulu.cs`, `OdemeSepeti.cs` (15:15) — eşzamanlı **KUR-BANT + DIL-SUNUCU şeridi (t2)**; bu paket Api/Core-Domain'e dokunmadı.
- 6'sı `Pages/Hurda` ekran testleri (`#hrd-onay-…` bekleniyor; aynı fiyat bandı istemci kapısı) — bu paket Hurda.razor'a dokunmadı.
- 1'i `S1fGorunumHttpTestleri.S1f_H5_Tezgahtar_eski_yolda_403_dar_ucta_200_alir` (Api yetki; aynı şeridin Api değişikliği dönemi) — bu paket Api'ye dokunmadı.
→ Bu paketin yeni kırmızısı **0**.

### Tur 2 canlı (API 5160, vekil 5161, CDP 9711–9715)
Kiracı `PAKETYILDIZ4B-TEST Tunalılar Kuyumculuk T2` (01M4G7QQT34ZFYQMSNNB0NC2KC; `demo-tohumla.js --prova-kiraci --sema yildiz`, 6 cari / 12 ürün / 9 satış). Kullanıcı adı `PATCH /kullanicilar/ben` 200.
| Adım | Beklenen | Gerçek |
|---|---|---|
| Yıldız tezgâhı, barkod 8690000000128, "Kaydet ve yazdır" (koyu PRO) | POST 200, son fiş + yazdır Fark bloğunda | `hareketler` yeni satır `01M4G89AG1MMVZXN7EF7A1JY90 · SATIS · adet 1.000 · tl_tutari 6908.72 · nakit 6908.72 · NAKIT · 24 Ayar Gram Altın`; SATIS 9→11 (biri 2.400 ₺'lik 925 Gümüş Kolye satışı); Ana Kasa 225748.02; yazdır href aynı hareket kimliği |
| 925 Gümüş Kolye ×6 (stok 2) Kaydet | sunucu 400, hata görünür + odak | "Satış: yetersiz satılabilir stok. Stok 2,000 adet, … talep 6 adet." `#yildiz-tezgah-hata` y=683, odakta |
| 332.823 ₺ fiş, Peşin | Kaydet kilitli + sebep, katman açık, Kaydet ekranda | "Bu tutarda Peşin Müşteri olmaz — müşteri seçin." · Kaydet y=804 · sayfa 900 |
Temizlik: API (dotnet run 41101 + uygulama 41111) ve vekil (41860) kendi PID'leriyle durduruldu; 5160/5161 dinleyen yok. `zsh kiraci-temizle.sh <id> --uygula` iki kiracı için: bu koşumun 01M4G7QQT34ZFYQMSNNB0NC2KC'si ve **bu paketin 11:43'te kesilen ilk koşumundan kalan** 01M4FXAC7YTDYSGWP5X4YACVV1 (`PAKETYILDIZ4B-TEST Tunalılar Kuyumculuk`; tur 1 raporu "silindi" diyordu, oysa duruyordu — hakem de bunu kullanmış). İkisi için 75 tenant_id tablosunda kalan satır **0**. tenants: başlangıç 24 → son **20** (bu paketin silmesi 2; diğer 2'si eşzamanlı şeritlerin kendi temizliği — bu koşumun komutları yalnız iki kimliği hedefledi). QA-TEST kiracılarına dokunulmadı; 5199/5200/5241 durdurulmadı.

### Tur 2 belirsiz eşleşme (orkestratör kurucuya gösterir)
| Konu | Uygulamada | Neden |
|---|---|---|
| Tasarım notu satırları (Tum4 "Fareyle üstüne…", Musteri4 "Satıra dokununca…") | KALDIRILDI | Paket koşulu + hakem; tuvalde var → "birebir" ile çakışma. Kurucu tuvaldeki hâliyle isterse geri konur (M18/M21 tek satır). Tüm ekranlar `main` boyu 64 px kısa |
| Barkod alanı odakta | Alt çizgi `--metin` (`--m`) | Hakem kararı; tuval odak hâli çizmemiş. Odaksız hâl tuvalle aynı (`--p`) |
| Müşteri katmanı | Sol sütunda, üst satırın altında katman; `.alan` + `.sat` kalıbı; PRO'da özet satırı ("Bugün · TL · satıcı · liste") da katmanda | Tuvalde müşteri seçimi çizilmemiş |
| Kimlik formu | Uygulamanın ortak çekmecesi (480 px sağ çekmece), içerik özgün MASAK formu | Tuvalde yok; hakemin önerdiği "çekmece kalıbı". Çekmece içi Yıldız'a özel biçimlenmedi |
| Hata / son fiş / yazdır | Fark bloğunda not satırı (15 px; hata --kotu) + "Fiş yazdır" b2 | Tuvalde sonuç hâli yok; en yakın kalıp tuvalin yasal not satırı |
| Sunucu 400'ünden sonra BASİT genel "Bu işlem yasal bir kapıya takıldı…" cümlesi | Hata satırı varken Yıldız'da yazılmaz (özel yasal cümle varsa yazılır) | Özgün BASİT bandı HER 400'de açılıyor (stok hatasında da) — `kapiSunucudanAcildi`; özgün davranış hasımlı pakete not |
| 375 px | Tek sütun; Fark + Kaydet uzun fişte kaydırma altında (2 kalemde Kaydet y≈1335) | Tuvalde telefon hâli yok; hata çizildiğinde odakla görünür alana kayar |

### Tur 2 dokunulan sayfalar
`Pages/Islemler.razor` (bu dalgada tur 1'de açılmıştı; tur 2 gerekçesi: tasarım hakeminin 3 KRİTİK bulgusu — müşteri satırı ve MASAK bloğu aynı sayfada tek tanımlı parçaya alındı, işaretleme değişmedi). Cariler/Ekranlar razor'a dokunulmadı (yalnız Yıldız bileşenleri).

### Tur 2 değişen dosyalar
`dotnet/Kuyumcu.Web/Pages/Islemler.razor`, `Pages/Islemler.Yildiz.cs`, `Bilesenler/Yildiz/YildizTezgah.razor`, `YildizTezgahVerisi.cs`, `YildizEkranlar.razor`, `YildizMusteriler.razor`, `wwwroot/css/tema-yildiz.css`, `dotnet/Kuyumcu.Core/I18n/Sozlukler.Yildiz.cs`, `dotnet/Kuyumcu.Tests/Yildiz4BTestleri.cs`, `ham/yildiz-4b/esleme/{musteri,tum}-{koyu,acik}.json`, `ham/yildiz-4b/tur2/*`, `ham/yildiz-4b/r5-*`.

### Tur 2 denetim
`dotnet/denetim.sh`: 316 ✓, 1 ✗ — yalnız **P-30 DEMO_INSAN_SURESI** (kronometreli demo ölçümü, bilinen, bu paketin dışında). Bu paketin sayaçları ✓.

---

# TUR 1 (önceki koşum — tarihçe)

Kapsam: Tezgâh (Satış; tuval `Tezgah4` koyu / `Tezgah4a` açık) + Müşteriler (`Musteri4`/`Musteri4a`) + Tüm ekranlar (`Tum4`/`Tum4a`), tuvalle BİREBİR. Hasımsız paket; kendi kanıtı bağlayıcı. Para/veri/yasal davranışa dokunulmadı (satış gövdesi Yıldız'da ve platin'de bayt bayt aynı — Y4B_23). COMMIT YOK. Migration gerekmedi. Mevzuat hükmü yazılmadı.

Not (süreklilik): bu paketin önceki bir koşumu (aynı gün ~12:06'da kesilmiş) Müşteriler + Tüm ekranlar bileşenlerini, `Islemler.Yildiz.cs` / `YildizTezgah.razor` iskeletini ve CSS'in ortak + Müşteriler + Tüm ekranlar bölümünü yazmış, ama Tezgâh'ı sayfaya bağlamamış, Tezgâh CSS'ini, testleri ve ölçümü yapmamıştı; `/private/tmp` geçici dizinleri silinmişti. Bu koşum o kodu okuyup derledi, eksikleri tamamladı ve HER ŞEYİ yeniden ölçtü.

## Sonuç — tuval-kiyas (kabulün özü)
`betikler/tur/tuval-kiyas.mjs`, 1440 px, aynı Inter dosyası, öğe başına {font-size, ağırlık, harf aralığı (em), satır yüksekliği, renk, aile, büyük harf} + {x, y, w, h} (+ öğeye özel: alt çizgi rengi, zemin, köşe yarıçapı, yükseklik, dolgu, aralık, ikon kalınlığı/rengi, opaklık). Tolerans: yazı birebir (±0,5 px · ağırlık eşit · ±0,01em · renk eşit), konum/boyut ±4 px.

| Ekran | Tuval | Öğe | koyu·BASİT | açık·BASİT | koyu·PRO | açık·PRO | yatay taşma |
|---|---|---|---|---|---|---|---|
| Tezgâh (`/islemler`) | Tezgah4 / 4a | 36 | 36/36 | 36/36 | 36/36 | 36/36 | 0 |
| Müşteriler (`/cariler`) | Musteri4 / 4a | 28 | 28/28 | 28/28 | 28/28 | 28/28 | 0 |
| Tüm ekranlar (`/ekranlar`) | Tum4 / 4a | 68 | 68/68 | 68/68 | 68/68 | 68/68 | 0 |

FARK 0 · bulunamayan 0 (12 koşum). Tablolar: `ham/yildiz-4b/r4-{tezgah,musteri,tum}-{koyu,acik}-{basit,pro}-tablo.md|json`; yan yana (tuval | uygulama) `…-yan-yana.png`, fark `…-fark.png`; eşleme tabloları `ham/yildiz-4b/esleme/*.json`.
Kuruş/₺ AYRI öğe ölçüldü (Tezgâh toplamı): tuval `,00 ₺` 36 px / 200 / soluk = uygulama aynı; tam kısım 72 px / 200 / −0,035em; Fark 56 px / 200 + `--iyi` onay işareti.
Tezgâh ölçümünde fiş tuvaldeki hâle getirildi (2 × Gram altın + 1 × Çeyrek barkodla — araca eklenen `adimlar`).

İlk koşumlarda (r1–r3) bulunan ve düzeltilen farklar: Tezgâh ana alanı 1120 px'e sıkışıyordu (`.islem-sayfa` max-width + PRO ızgarası → Yıldız'da tam genişlik, flex sütun); "Nakit· Ana Kasa" boşluğu (Razor boş metni atıyordu); kalem ayrıntısında gram (tuval "2 adet · **1,00 g**" = tanesinin gramı; has = satır toplamı); Müşteriler'de Peşin Müşteri satırı (tuvalde yok); Tüm ekranlar tuvalinde nav'ın 41 px'e ezilmesi (aşağıda).

Gözle (Read, 3 görüntü): Tezgâh koyu BASİT yan yana, Müşteriler açık PRO yan yana, Tezgâh 375 koyu BASİT. Fark yalnız VERİ (ürün adları "24 Ayar Gram Altın", tutarlar, tek ödeme satırı) + aşağıdaki belirsiz eşleşmeler. 375'te görülen barkod girdisi 40 px sol dolgusu (olcek.css telefon kuralı) düzeltildi (`#islem-barkod` Yıldız'da 2 px — tuval).

## Belirsiz eşleşme (orkestratör kurucuya gösterir)
| Tuval öğesi | Uygulamada | Neden / en yakın kalıp |
|---|---|---|
| Tum4 kutusu `height:1460px; overflow:hidden` içinde içerik taşıyor, nav 72 → 41 px eziliyor | Kıyasta tuvale `.y > .nav{flex-shrink:0}` eklendi (tablo başlığında yazılı) | Tuvalin çizim kusuru; Bugün/Tezgâh/Müşteriler tuvallerinde nav 72 px. Uygulamada nav 72 px. |
| "Gider ve virman" karesi | Yıldız karesinde "Gider ve virman" (tuval); menü/başlıkta "Gider ve para aktarımı" kalır | 03.10 ekip oyu menü adını değiştirmişti; tuval eski adı yazıyor. Kurucu "tuval birebir" → Yıldız karesinde tuval. Aynı ekran iki ad taşıyor (UX-S31 gerilimi) — kurucu kararı gerekir. |
| Müşteriler "7 müşteri" + 6 satır | Sayıda sistem carisi (Peşin Müşteri) VAR, listede YOK | Tuval 7 sayar, 6 satır çizer; TUNALILAR'da 6 gerçek + Peşin = 7. Yıldız'da Peşin Müşteri kartı listeden açılamaz (Satış'ta seçilir). |
| "Selin Ergüven — bugün alışveriş yaptı" | ÇİZİLMEDİ (boş) | Liste yanıtında carinin son işlem tarihi yok; uydurulmadı (Anayasa md.1). Hasımlı pakete devir: liste ucuna `sonIslemTarihi`. |
| "Anadolu Döküm — Toptancı · 0532…" | "0532…" (Toptancı öneki yok) | Uygulama öneki `Segment == Toptan` ise yazar (TOPTANCI sekmesiyle AYNI ölçüt); tohum verisinde Anadolu `TEDARIKCI` türü, segmenti boş → veri farkı. |
| Kemal "bize borçlu · 30 gün" | vadesi geçmişse "30 gün geçti" | YILDIZ-4A kararı korundu (geçmiş ↔ kalan ayrımı). |
| "+ SÜZGEÇ" sekmesi (Müşteriler) | tuvalde yok; tuval sekme kalıbında 4. öğe | Tür/pasif süzgeci (özellik silinmez); kapalı gelir. |
| Tezgâh "Nakit · Ana Kasa" + "Kart · Ana Kasa" iki satır | Tek ödemede tek satır; karma ödemede bacak başına satır | Veri: tuval karma ödeme gösteriyor; ölçümde tek ödeme (satır kalıbı aynı ölçüldü). |
| Tür ızgarası, müşteri seçimi, son satışlar, reyon/açıklama, PRO yan sütunu | Tuvalde yok → gizli/çizilmez. "Satış" kelimesi tür ızgarasını, "Değiştir" müşteri satırını açar; reyon/açıklama satıra dokununca açılan düzenleme alanında; son satışlar/storno yolu "Tüm ekranlar › İşlem defteri" | Tuval kalıbı (b2 altı çizili / satıra dokun). Yasal bantlar (MASAK formu, yasal kapı, nakit uyarısı, hata/başarı) GİZLENMEZ — gerektiğinde fişin altında. |
| Satış dışı türler (Alış, İade, Değişim…) | Yıldız renkleriyle özgün fiş | Tuvalde yalnız Satış çizildi. |
| 375 / 1280 px | Tezgâh tek sütun; Tüm ekranlar ızgarası 1360 altı 6, 1000 altı 4, 760 altı 3, 480 altı 2 sütun; Müşteriler satırı 2 sütun | Tuvalde dar hâl yok; kare adları kırılmasın diye (K3). |

## Yapılanlar
- **Islemler.razor** (TEK DOKUNUŞ — bu dalgada ilk kez; satır sayısı 1398, tavan korunur): Yıldız + SATIS'ta `<YildizTezgah B="@YildizBaglami" />`; özgün `FisVerdimAldim`/`FisProKalemKarti`, alt satır (Vazgeç/Kaydet), PRO ödeme satırı ve yan sütun Yıldız'da çizilmez; iki koşul satırı (`!YildizTezgahMi`) dışında özgün işaretleme AYNEN; `YildizAboneOl/AboneliktenCik` yaşam döngüsünde; fiş iskontosu satırı Yıldız PRO'da da kalem düzenleme alanında.
- **Islemler.Yildiz.cs**: bağlam (değerler sayfanın mevcut durumundan; eylemler mevcut yöntemler: `Kaydet`, `Vazgec`, `OdemeyiBol`, `TekOdemeyeDonAracla`, `AcikHesapKaydet`, `IndirimDegistir`); kalem düzenleme = kalem tablosu + reyon + açıklama; yasal satır sunucu haddiyle (sayı istemciye gömülmez — B-3 sayacı).
- **YildizTezgah.razor / YildizTezgahVerisi.cs**: tuval yerleşimi; ödeme adı metin düğümü "Nakit " (tuval); birim gram.
- **Cariler.Yildiz.cs**: `YildizGorunen` (tuval süzgeci, sistem carisi hariç).
- **YildizEkranlar.razor + Sozlukler.Yildiz.cs**: `YildizEkranSozlugu.KareAdi` ("/gider" → "Gider ve virman", TR).
- **tema-yildiz.css**: 4B bölümüne TEZGÂH kuralları + dar ekran (1360/1000/760/480) + `#islem-barkod` dolgusu. Tümü `html:root[data-sema="yildiz"]` kapsamlı, renk yalnız `var()`.
- **tuval-kiyas.mjs**: `adimlar` (ölçümden önce ekranı hazırla: yaz/tuş/tıkla/ev) ve `tuvalEkCss` (tuval kusuru düzeltmesi, tabloya yazılır).
- **Testler**: `Yildiz4BTestleri.cs` (yeni, 20 test koşumu). 4B'nin kırdığı eski Yıldız testleri kurucu kararıyla güncellendi: `Y1_06` (200 ağırlık `.yz` tuval sınıflarında serbest), `Y4A_32` (`Blok4A` 4B başlığında biter), `Y2_20/21` (Tüm ekranlar artık `a.yz-kare`).

## Testler
- Önce: `FullyQualifiedName~Yildiz` 104 (4A raporu). Bu koşum başında (önceki yarım 4B kodu ile) 5 Yıldız testi kırmızıydı (Y1_06, Y2_20, Y2_21, Y4A_32 + yazılan Y4B_31). Sonra: `Yildiz` 124/124; `Yildiz|Ekranlar|BasitModPos|CariEkran` **252/252**.
- Tam süit (KUYUMCU_TEST_DB=kuyumcu_test_t1, tek tur, son kodla; trx `/private/tmp/kuyumcu-serit/trx-pYILDIZ-4B`): **11790 toplam · 11787 geçti · 1 başarısız · 2 atlandı**. Tek kırmızı `OlcekTekTestleri.OL6_olcek_css_yalniz_olcu_yazar_sabit_renk_yok_ve_moddan_bagimsizdir` — `olcek.css`'te yeni `html[data-mod] :is(.ana-icerik, .cekmece) :is(.btn-kucuk…)` seçicisi; dosya 13:43'te başka şerit tarafından değiştirildi, bu paket olcek.css'e DOKUNMADI → **bu paketin yeni kırmızısı 0**.
- Derleme: `dotnet build Kuyumcu.sln` 0 hata; artımlı 0 uyarı, son tam derlemede 6 uyarı — hepsi başka test dosyalarında önceden var olan xUnit1031/CS8601 (KesifP2, SUBEKAPSAMASinir, SonKalanlar, Yildiz2:193/232 — bu satırlara dokunulmadı).

## Mutasyon (kaynak `mut-yedek/` → perl tek eşleşme → `Yildiz4BTestleri` → geri kopya → `cmp` AYNI)
| Kod | Değişiklik | Kırmızı test |
|---|---|---|
| M1-01 | `YildizTezgahMi`: `tur == "SATIS"` → `tur != ""` | Y4B_22 |
| M2-01 | kalem ayrıntısı gram = gram × adet | Y4B_01 |
| M3-01 | sistem carisi süzgeci kalktı | Y4B_04 |
| M4-01 | "Gider ve virman" kare adı kalktı | Y4B_05, Y4B_14 |
| M5-01 | Islemler alt satırı Yıldız'da da çiziliyor | Y4B_20 (BASİT, PRO) |
| M6-01 | toplam rakam 72 → 60 px | Y4B_30 |
| M7-01 | kuruş+₺ soluk sınıfı kalktı | Y4B_10 (+ Y4B_22 zamanlama kırılganlığı göründü → `WaitForAssertion` ile sağlamlaştırıldı) |
| M8-01 | barkod girişi Yıldız'da iki kez çiziliyor | Y4B_20 (BASİT) |
| M9-01 | "Kaydet ve yazdır" → Vazgeç'e bağlı | Y4B_23 (BASİT, PRO) |
Hepsi geri alındı, `cmp` AYNI; son mutasyondan sonra temiz `dotnet build Kuyumcu.sln` 0/0. M1-01'in ilk denemesi başka şeridin eşzamanlı derlemesiyle dosya kilidine (MSB4018) takıldı → betik yeniden denedi.

## Gerçek (başsız) Chrome — sikisma-olc.js (K1–K12 + yatay)
Rotalar `/islemler`, `/cariler`, `/ekranlar` × 1440·1280·375 × BASİT·PRO × açık·koyu = 36 ölçüm:
| Ölçü | Sonuç |
|---|---|
| K1 gizli görünen · K5 TRY · K6 · K9 binen · K10 · K11 · K12 · yatay | 0 (hepsi) |
| K3 taşan/kırık | 0 (ilk ölçümde Tüm ekranlar 1280'de 4–7, 375'te 21 kırık + "Kullanıcılar" taşan, Müşteriler 375'te sekme taşması → dar ekran ızgara/sekme kuralları; yeniden ölçümde 0) |
| K7 hizasız | `/cariler` 1440/1280'de 1 — başlık satırı (üst satır+64 px başlık ↔ arama ↔ hap, alt kenar ±14): TUVALİN KENDİSİ (`align-items:flex-end`), kıyas 28/28 AYNI |
| K8 jargon | BASİT `/ekranlar` 1 — "r-zarar": ölçerin `[a-z]+-[a-z]+` kalıbı "Kâr-zarar"daki â'yı tanımıyor (yanlış pozitif; tuval metni) |
| K4 tren | `/ekranlar` 2,1–5,4 ekran × 6 bölüm: tuval 39 kare/6 bölüm (tuval içeriği de 1460 px kutuyu aşıyor) |
| K2 dar sütun | Tüm ekranlar %95–96; Tezgâh/Müşteriler ölçülmedi (kart yok) |
Sonuçlar: `ham/yildiz-4b/olc-{BASIT,PRO}-dar.json`, `olc-PRO.json`; 375 koyu tam sayfa görüntüleri `olc-{BASIT,PRO}-375-karanlik-{islemler,cariler,ekranlar}.png`.

## 8 eski şema (FARK 0)
`arac/fark.mjs` (bu pakette yazıldı; YILDIZ-1 betiği /tmp temizliğinde silinmişti): kiracı şeması geçici olarak platin (özgün sayfa çizilsin — Yıldız kabuğu/bileşeni olmadığı doğrulanır), 8 şema × açık/koyu × {Satış, Müşteriler, Tüm ekranlar} × tema-yildiz.css KAPALI ↔ AÇIK sha256. Yöntem notu: ilk görüntü ısınma (atılır) — aç-kapa-aç denemesi, sayfanın ilk saniyelerde zamanla değiştiğini (CSS'ten bağımsız) gösterdi; `@font-face` korunur. Fark çıkan karşılaştırma bir kez yeniden çekilir. **BASİT 48/48 FARK 0, PRO 48/48 FARK 0** (`ham/yildiz-4b/fark-basit.out`, `fark-pro.out`; PRO'da 1 karşılaştırma yeniden çekildi, ikinci çekimde eşit). Razor tarafı: başka şemada Satış özgün fişi (Y4B_21 BASİT/PRO). Ölçümden sonra şema yildiz'e geri alındı.

## Canlı (API 5160, vekil 5161, CDP 9711-9715)
Kiracı `PAKETYILDIZ4B-TEST Tunalılar Kuyumculuk` (`betikler/demo-tohumla.js --prova-kiraci --sema yildiz`; TUNALILAR senaryosu: 6 cari, 12 ürün, 9 satış; sistem anahtarı bu API için üretilen rastgele değer, basılmadı). Kullanıcı adı `PATCH /kullanicilar/ben` ile "Hakan Tunalı" (200). `GET /gorunum` 200 `sema=yildiz`.
**Satış akışı (Yıldız tezgâhı, PRO, gerçek Chrome):** barkod `8690000000128` + Enter → 1 kalem "6.908,72 ₺", Kaydet kilitsiz → "Kaydet ve yazdır" tıklandı → ağ: `POST /islemler/satis` **200**, fiş boşaldı (kalem 0).
SQL: `hareketler` son satır → `SATIS · adet 1.000 · tl_tutari 6908.72 · nakit_tutar 6908.72 · NAKIT · has -1.000000 · 24 Ayar Gram Altın`; kiracıda SATIS sayısı 9 → **10**; `kasalar` Ana Kasa 223356.84.
Satış davranışının değişmediğinin birim kanıtı: Y4B_23 — aynı kalemle platin ve Yıldız'da gönderilen `POST /islemler/satis` gövdesi (istemci kimlikleri hariç) BİREBİR (BASİT ve PRO).
Temizlik: API ve vekil kendi PID'leriyle durduruldu (5160/5161 dinleyen yok). `zsh kiraci-temizle.sh <id> --uygula`; ardından 75 tenant_id tablosunda kiracının satırı **0**, `tenants` satırı 0; tenants sayısı başlangıç 25 → son 25. 5199/5200/5241'e dokunulmadı.

## DoD sözleşmesi
Yeni varlık/kayıt türü YOK (yalnız görünüm). Tezgâh'taki her düğme sayfanın mevcut yöntemine bağlıdır (tek POST yolu `Kaydet()`); kalem yokken kilitli düğmeler sebebini `title`'da söyler (UX-S30, Y4B_11). Kalem/ödeme satırına dokunmak özgün düzenleme alanlarını açar (kalem tablosu, indirim, reyon, açıklama; ödeme türü + kasa + karma bacaklar). Müşteriler'de satır → sayfanın kartı (tahsilat, ekstre, düzenleme, pasife alma — `YildizKartEylemleri`); Yeni müşteri → mevcut form. Boş durum yalnız başarılı boş yanıtta; hata bandı ayrı (UX-S28).

## Dokunulan sayfalar
`Pages/Islemler.razor` (bu dalgada ilk), `Pages/Cariler.razor` ve `Pages/Ekranlar.razor` (önceki yarım 4B koşumunda açılmış; bu koşum yalnız `Cariler.Yildiz.cs` ve bileşen/sözlük dosyalarına dokundu).

## Değişen dosyalar (bu paket)
- `dotnet/Kuyumcu.Web/Pages/Islemler.razor` (M), `Pages/Islemler.Yildiz.cs`, `Pages/Cariler.Yildiz.cs`, `Pages/Cariler.razor` (M), `Pages/Ekranlar.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizTezgah.razor`, `YildizTezgahVerisi.cs`, `YildizMusteriler.razor`, `YildizMusteriVerisi.cs`, `YildizEkranlar.razor`, `YildizKartEylemleri.razor`, `YildizIkonlari.cs`
- `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Yildiz.cs`
- `dotnet/Kuyumcu.Tests/Yildiz4BTestleri.cs` (yeni), `Yildiz1Testleri.cs`, `Yildiz2Testleri.cs`, `Yildiz4ATestleri.cs`
- `dokumanlar/09-uygulama-kayitlari/betikler/tur/tuval-kiyas.mjs`
- `dokumanlar/09-uygulama-kayitlari/ham/yildiz-4b/` (kanıtlar)
(Yıldız dosyalarının çoğu git'te `??` — önceki Yıldız paketlerinden.)

## Koşul karşılama
| Koşul | Durum |
|---|---|
| tuval-kiyas 3 ekran × koyu+açık tolerans içinde (BASİT+PRO) | ✓ 12 koşum, 0 FARK |
| kuruş/₺ ayrı öğe | ✓ |
| yan yana görüntüler, Read | ✓ (3 Read) |
| satış akışı canlı + SQL | ✓ POST 200, hareketler +1 |
| 375 taşma 0 | ✓ yatay 0, K3 0 |
| 8 eski şema FARK 0 | ✓ 96/96 |
| mutasyon ≥3 | ✓ 9 |
| build 0/0 | ✓ |
| denetim TEMİZ | 316 ✓, 1 ✗ yalnız P-30 DEMO_INSAN_SURESI (bilinen, kronometreli demo ölçümü). İlk koşudaki B-3 ESIK_ISTEMCIDE 5/4 bu paketin yorumundaki "30.000" idi → yorum sayısız yazıldı, ✓ |
| tam süit 0 yeni kırmızı | ✓ 11787/11790; tek kırmızı OL6 (olcek.css, başka şerit) |
| BASİT+PRO aynı tuval düzeni; mod anahtarı kullanıcı menüsünde | ✓ (4A'dan) |
| tuvalde olmayan öğe yok | ✓ (istisnalar "belirsiz eşleşme"de) |

## Açık kalanlar
- Tam süitte başkasına ait kırmızı: `OlcekTekTestleri.OL6` (olcek.css 13:43 değişikliği — sahibi olcek.css'i düzenleyen eşzamanlı şerit, büyük olasılıkla TOPLU/düğme ölçeği).
- denetim ✗ P-30 DEMO_INSAN_SURESI (bilinen).
- "bugün alışveriş yaptı" (son işlem tarihi) — hasımlı pakete devir (liste ucuna alan).
- "Gider ve virman" ↔ menü adı "Gider ve para aktarımı" — kurucu kararı.
- Satış dışı türlerin (Alış, İade, Değişim…) Yıldız tuvali yok — tuval gelirse aynı araçla.
- `sikisma-olc.js` K8 kalıbı ASCII dışı harfi tanımıyor ("Kâr-zarar" yanlış pozitif) — ölçer sahibine.

## Kurucuya / mali müşavire
Mevzuat konusu yok (nakit sınırı sayısı sunucudan, istemciye gömülmedi). Kurucuya: yukarıdaki "belirsiz eşleşme" tablosu.
