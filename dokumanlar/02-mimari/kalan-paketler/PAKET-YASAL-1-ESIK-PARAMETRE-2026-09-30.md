# PAKET YASAL-1 — Tarihli program parametreleri tablosu (eşiklerin zaman boyutu)

> Tarih: 2026-09-30 · Hazırlayan: Tamer Bey koordinasyonu (şartname ajanı; KOD YAZILMADI) · Durum: ŞARTNAME — ekip oyu bekliyor
> Kapattığı çelişki: **(1)** "eşiklerin zaman boyutu yok, program parametreleri tablosu yok; had değişirse geçmiş işlemler yeni hadle yorumlanır" (MIHENK-PLAN-RAPORU-2026-09-28 §3a). Ek olarak **çelişki (7)** kontrol sonucu § 12'dedir.
> Önkoşul: yok. **YASAL-2, YASAL-3, YASAL-4 bu paketin servisini kullanır → önce YASAL-1.**

---

## 1. Amaç

Kurucu önceliği: **"Pusula kadar sade + devletin tarif ettiği her kural otomatik ve görünür."**

Devletin rakamları (tevsik haddi, MASAK eşiği, gider pusulası süresi, stopaj oranı, bildirim süreleri, resmî tatiller) **bir tarihten itibaren** geçerlidir. Program bu rakamları **geçerlilik başlangıcıyla** tutmalı ve:
1. **Bugünkü işlemi** bugünkü rakamla,
2. **Geçmiş bir işlemi** (iade, taksit, geç fark edilen ihlal, rapor, risk penceresi, veri göçü) **o işlemin günündeki** rakamla değerlendirmeli,
3. **İleri tarihli** bir değişikliği (ör. 1 Ocak'tan itibaren yeni had) kod ve yayın beklemeden, **gününde kendiliğinden** uygulamalı ve birkaç gün önceden **tek cümleyle** haber vermelidir.

Sade yüz: tezgâhtar rakam görmez. Rakam yalnız sınır aşılınca uyarı cümlesinde ve SAHİP'in "Yasal sınırlar" sayfasında (salt okunur) görünür.

---

## 2. Mevcut durum (kod kanıtı)

| Kanıt | Ne gösteriyor |
|---|---|
| `dotnet/Kuyumcu.Api/Program.cs:74-78` | `MasakEsikleri` açılışta `appsettings:Masak`'tan **bir kez** okunup **singleton** yapılıyor. |
| `Program.cs:97-101` | `TevsikEsikleri` aynı: tek değer, singleton. |
| `Program.cs:146-147`, `:165-166`, `:68-72`, `:83-87` | `HurdaAyarlari`, `KdvAyarlari`, `TakvimAyarlari`, `MasakRiskAyarlari` — hepsi aynı desen. |
| `dotnet/Kuyumcu.Api/appsettings.json:51-61` | `Masak`: `KimlikEsigi 185000`, `AmacBeyaniEsigi 200000`, `BeyanFormuEsigi 2000000.01`, `EkBelgeEsigi 20000000.01`, `SupheliBildirimIsGunu 10`, `SaklamaYili 8` — **tarih alanı yok**. |
| `appsettings.json:90-94` | `Tevsik`: `Had 30000`, `Etkin true` — tarih yok. |
| `appsettings.json:122-126` | `Hurda`: `EsnafMuafStopajOran 0.02`, `NihaiTuketiciStopajOran 0.0`, `PusulaDuzenlemeGunu 7` — tarih yok. |
| `appsettings.json:159-162` | `Takvim.ResmiTatiller: []` — **dinî bayramlar hiç girilmemiş**; `Common/ResmiTatiller.cs:74-80` bu yılları "eksik" sayıyor, MASAK 10 iş günü süresi bayramları iş günü sayıyor. |
| `Modules/Tevsik/TevsikKurali.cs:38` | `Had = 30_000m` — kodda varsayılan (yapılandırma yoksa). |
| `Modules/Masak/MasakKuralMotoru.cs:24` | `KimlikEsigi = 185_000m` — aynı. |
| `Modules/Masak/MasakKuralMotoru.cs:197` | `SaklamaBitisi(t) => t.AddYears(SaklamaYili)` — tek süre. |
| `Modules/Masak/RiskServisi.cs:248`, `:256` | Risk skoru 90 günlük pencerede **bugünkü** `KimlikEsigi` ile geçmiş işlemleri sınıflıyor. |
| `Modules/Tevsik/TevsikController.cs:46` | Önizleme ucu `kapi.Esikler.Had` — tek değer. |
| `dokumanlar/02-mimari/adr/0005-mevzuat-esikleri-yapilandirmadan.md` | "Mevzuat değişince kod değişmez" — ama **tarih boyutu yok**; değişiklik yeniden başlatma ister ve geçmişi de yeni rakamla okur. |
| Kiracı boyutu | Eşikler kiracı bazında değil (doğru — ulusal rakamlar); ekrandan değişmiyor. |

**Sonuç:** Had 1 Ocak'ta değişirse (1) sunucunun o gece yeniden başlatılması gerekir; (2) 31 Aralık işlemine yapılan 2 Ocak iadesi, geç fark edilen ihlal taraması (YASAL-2 TB-16), taksit kuralı (TB-15), risk penceresi ve raporlar **yeni** rakamla yorumlanır → yanlış "ihlal" ya da gözden kaçan ihlal.

---

## 3. Mevzuat (birincil kaynak)

> Kural: her satırda birincil URL + ≤15 kelime alıntı. Teyitsizler § 3.2'de ayrıdır.

> Doğrulama: 30.09.2026, birincil metinler okunarak (Resmî Gazete, mevzuat.gov.tr, GİB, MASAK). İndirilen metinler oturum çalışma alanında; bu belgeye yalnız URL + kısa alıntı alındı.

### 3.1 Doğrulanan tarihli değerler (tohum satırları)
| Anahtar | Değer | Geçerlilik | Dayanak · birincil URL | Alıntı (≤15 kelime) |
|---|---|---|---|---|
| `TEVSIK_HADDI_TL` | **7.000** | **01.01.2016 – 29.11.2024** | 459 s. VUK GT, RG 24.12.2015/29572 · https://www.resmigazete.gov.tr/eskiler/2015/12/20151224-6.htm | "7.000 TL'yi aşan tutardaki her türlü tahsilat ve ödemelerini" · "Bu Tebliğ 1/1/2016 tarihinde yürürlüğe girer." |
| `TEVSIK_HADDI_TL` | **30.000** | **30.11.2024 –** | 575 s. VUK GT, RG 30.11.2024/32738 · https://www.resmigazete.gov.tr/eskiler/2024/11/20241130-10.htm | "“7.000” ibareleri “30.000” şeklinde" · (md.5) "Bu Tebliğ yayımı tarihinde yürürlüğe girer." |
| (ara dönem) | — | 2016–2024 | Aradaki 459 değişiklikleri (2016, 2017, 540 s. 2022, 572 s. 2024) hadde dokunmadı | — |
| `TEVSIK_BILDIRIM_IS_GUNU` | **5** | 02.08.2024 – (7524 s. K. md.13, RG 02.08.2024/32620) | VUK mük. md.355 · https://www.mevzuat.gov.tr/mevzuatmetin/1.4.213.pdf | "ödemeyi takip eden beş iş günü içerisinde kendiliğinden idareye bildirmesi halinde" |
| `MASAK_KIMLIK_ESIGI_TL` | 20.000 → 75.000 → 85.000 → **185.000** | 75.000: RG 24.02.2021 (3580 s. CK) · 85.000: RG 16.06.2022 (5739 s. CK) · **185.000: RG 14.01.2023/32073 (6702 s. CK)** – | Tedbirler Yön. md.5/1-b (konsolide dipnot) · https://www.mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf | "yüzseksenbeşbin TL … veya üzerinde olduğunda" · dipnot: "“seksenbeşbin” ibaresi “yüzseksenbeşbin” şeklinde değiştirilmiştir" |
| `MASAK_SIB_IS_GUNU` | **10** | (yönetmelik metni) | Tedbirler Yön. md.28/2 · aynı URL | "şüphenin oluştuğu tarihten itibaren en geç on iş günü içinde" |
| `MASAK_SAKLAMA_YIL` | **8** | (yönetmelik metni) | Tedbirler Yön. md.46 · aynı URL | "son işlem tarihinden itibaren sekiz yıl süre ile muhafaza etmek" (⚠ başlangıç = **son işlem**; bkz. YASAL-3) |
| `GIDER_PUSULASI_GUN` | **7** | (7338 s. K. ile değişik md.234) | VUK md.234 · https://www.mevzuat.gov.tr/mevzuatmetin/1.4.213.pdf | "azami yedi gün içinde düzenlenir" · "düzenlenmeyen gider pusulası hiç düzenlenmemiş sayılır" |
| `STOPAJ_ESNAF_MUAF_HURDA_ORAN` | **0,02** | 03.02.2009 – (ilk metin) | GVK md.94/13 + 2009/14592 BKK (RG 03.02.2009/27130), 13. bent · https://www.resmigazete.gov.tr/eskiler/2009/02/20090203-3.htm | "b) Hurda mal alımları için % 2" |
| `FATURA_HADDI_TL` | 9.900 (2025) · **12.000** (2026) | 01.01.2025 – 31.12.2025 · 01.01.2026 – | VUK md.232; 577 s. GT (RG 30.12.2024/32768 2. Mük.) · https://www.resmigazete.gov.tr/eskiler/2024/12/20241230M2-14-1.pdf ; 588 s. GT (RG **31.12.2025**/33124 5. Mük.) · https://www.resmigazete.gov.tr/eskiler/2025/12/20251231M5-31.pdf | "MADDE 232 Fatura kullanma mecburiyeti 12.000" |
| `KDV_OZEL_MATRAH_KODU` (öneri değeri) | 805 — **KAPALI** | — | GİB UBL-TR Kod Listeleri (özel matrah listesi) · ebelge.gib.gov.tr/dosyalar/kilavuzlar/UBLTR_1.2.1_Kilavuzlar.zip | "805 Altından Mamül veya Altın İçeren Ziynet Eşyaları İle Sikke Altınların Teslimi" |

**Düzeltme notları (birincil kaynaktan çıkan):**
- 30.000 TL haddi **30.11.2024**'te yürürlüğe girdi — **01.01.2025 değil**. 30.11.2024 ile 31.12.2024 arasındaki işlemler 30.000 ile yorumlanır.
- `mevzuat-gereksinimleri.md:58` (V-04) "fatura haddi çelişkili: 12.000 / 36.000 / 3 M / 5 M" → VUK 232 **fatura kullanma** haddi 2026 için **12.000 TL** olarak DOĞRULANDI. 3 M / 5 M e-Fatura **geçiş** hadleridir (ayrı konu, bu tabloya girmez). Kuralın **kodlanması** bu paketin kapsamı dışında; değer tabloya `DOGRULANDI` ama kural `KAPALI` olarak girer.
- 588 s. Tebliğ'in RG tarihi **31.12.2025**'tir (mevzuat.gov.tr dipnotundaki 30/12/2025 hatalı).

### 3.2 Teyit edilemeyenler (ürün bunları "TEYİTSİZ" damgasıyla tutar)
| Anahtar | Sorun | Ürün davranışı |
|---|---|---|
| `MASAK_AMAC_BEYANI_ESIGI_TL` (200.000) | Tedbirler Yönetmeliği'nde bu rakam bulunamadı | TEYITSIZ; bugünkü (tutucu) davranış korunur |
| `MASAK_BEYAN_FORMU_ESIGI_TL` (2 M) / `MASAK_EK_BELGE_ESIGI_TL` (20 M) | HMB sitesinde bulunan Nakit İşlem Beyan Formu tebliğ metni (https://ms.hmb.gov.tr/uploads/sites/12/2025/08/SAK-Genel-Teblig-SIRA-NO-31-6203d6d5cd7cf747.pdf) yükümlülüğü "finansal kuruluşlar"a veriyor; kuyumcu (md.4/1-k) bu tanımın dışında. RG'deki nihai metin ve sıra numarası **DOĞRULANAMADI** | TEYITSIZ — mali müşavir teyidi (YASAL-3 K-4) |
| `STOPAJ_ESNAF_MUAF_HURDA_ORAN` 2009 sonrası | 2009/14592 BKK'nın 13. bendinde sonradan değişiklik olup olmadığı **DOĞRULANAMADI** (yalnız ilk metin okundu) | Değer tohumlanır, `teyit=TEYITSIZ` |
| `STOPAJ_NIHAI_TUKETICI_ORAN` (0) | GVK md.94'te mükellef olmayan gerçek kişiden hurda/ziynet alımına özgü bent yok; md.94/18-20 yetkisiyle çıkmış bir karar olup olmadığı **DOĞRULANAMADI** | 0, `TEYITSIZ` |
| `STOPAJ_BEYAN_GUNU` (26) | Kanunda beyan "ertesi ayın yirmiüçüncü günü" (GVK md.98), ödeme "yirmialtıncı günü" (md.119); 26 yalnız GİB süreler belgesinde (https://cdn.gib.gov.tr/api/gibportal-file/file/getFileResources?objectKey=arsiv%2Fyardim-kaynaklar%2Fyararli-bilgiler%2FBeyannameVermeveOdemeSureleri.pdf — "İzleyen ayın 26 ncı günü saat 23.59'a kadar verilir."); dayanak tebliğ **DOĞRULANAMADI** | 26, `TEYITSIZ` |
| TB-15'te hangi günün haddi (işlem mi ödeme mi) | 459 §4.1.2 "işlemin toplam tutarı" der, hangi tarihin haddi sorusunu açıkça cevaplamaz | Öneri: işlem günü — mali müşavir teyidi |
| 6702 s. CK'nın RG orijinal metni | Yalnız mevzuat.gov.tr konsolide dipnotundan okundu | DOGRULANDI (konsolide) — orijinal RG metni ayrıca okunmalı |
| `RESMI_TATIL` dinî bayram günleri 2026–2027 | Diyanet/RG ilanı bu pakette okunmadı | Kuruluma yıllık elle girilir; boşsa Panel uyarısı |

---

## 4. Kapsam

### 4.1 Veri modeli — `yasal_parametreler` (YENİ tablo, sistem geneli)
Ulusal rakamlardır → **kiracıya bağlı DEĞİL** (`IKiraciVarlik` uygulamaz; `denetim.sh BEKLENEN_VARLIK` değişmez). Kiracı süzgecinin dışında kaldığı için okuma yalnız `YasalParametreServisi` üzerinden yapılır; yazma yalnız migration ve sistem ucu (§ 4.3).

| Kolon | Tür | Not |
|---|---|---|
| `id` | CHAR(36) PK | ULID |
| `anahtar` | VARCHAR(60) | ör. `TEVSIK_HADDI_TL` (§ 4.2 listesi, beyaz liste) |
| `deger_sayi` | DECIMAL(20,6) NULL | sayısal değerler |
| `deger_metin` | VARCHAR(200) NULL | kod/tarih listesi (ör. özel matrah kodu, tatil günü) |
| `birim` | VARCHAR(20) | `TL`, `GUN`, `IS_GUNU`, `YIL`, `ORAN`, `KOD`, `TARIH` |
| `gecerlilik_baslangic` | DATE | **yerel (Europe/Istanbul) gün**, dahil |
| `gecerlilik_bitis` | DATE NULL | hariç; NULL = açık uçlu. Sunucu yazarken bir öncekini kapatır (aralık çakışması 0 — CHECK testi) |
| `dayanak` | VARCHAR(300) | "575 s. VUK GT, RG 30.11.2024/32738" |
| `kaynak_url` | VARCHAR(500) | birincil URL |
| `teyit` | VARCHAR(12) | `DOGRULANDI` \| `TEYITSIZ` \| `KAPALI` |
| `aciklama_sade` | VARCHAR(300) | ekranda gösterilecek tek cümle ("Nakitte günlük sınır") |
| `olusturan` | VARCHAR(60) | `migration:<ad>` ya da sistem kullanıcısı |
| `olusturuldu` | DATETIME | |

İndeks: UNIQUE (`anahtar`, `gecerlilik_baslangic`). **Append-only:** güncelleme yalnız `gecerlilik_bitis` alanını bir kez doldurabilir (ADR-0004 ruhu; `denetim.sh`'a sayaç: bu tabloda `ExecuteUpdate/Remove` = 0).

Migration: `2026093xxxxxxx_YasalParametreler` — tablo + § 3.1'deki **doğrulanmış geçmiş** satırlarla tohumlama (yalnız DOĞRULANDI olanlar; teyitsizler `teyit=TEYITSIZ` ve bugünkü davranışı birebir koruyan değerle).

### 4.2 Anahtar listesi (ilk sürüm)
| Anahtar | Bugünkü değer (appsettings) | Kullanan |
|---|---|---|
| `TEVSIK_HADDI_TL` | 30.000 | TevsikKurali, TevsikController, YASAL-2 |
| `TEVSIK_BILDIRIM_IS_GUNU` | 5 (YENİ) | YASAL-2 TB-16 |
| `MASAK_KIMLIK_ESIGI_TL` | 185.000 | MasakKuralMotoru, RiskServisi |
| `MASAK_AMAC_BEYANI_ESIGI_TL` | 200.000 | MasakKuralMotoru — **dayanağı bu belgede bulunamadı → TEYITSIZ** (bkz. § 3.2) |
| `MASAK_BEYAN_FORMU_ESIGI_TL` | 2.000.000,01 | TEYITSIZ (M-06) |
| `MASAK_EK_BELGE_ESIGI_TL` | 20.000.000,01 | TEYITSIZ (M-06) |
| `MASAK_SIB_IS_GUNU` | 10 | MasakServisi |
| `MASAK_SAKLAMA_YIL` | 8 | MasakKuralMotoru, YASAL-3 |
| `GIDER_PUSULASI_GUN` | 7 | GiderPusulasiKapisi, YASAL-4 |
| `STOPAJ_ESNAF_MUAF_HURDA_ORAN` | 0,02 | GiderPusulasiKapisi — **TEYITSIZ** (bkz. YASAL-4 § 3) |
| `STOPAJ_NIHAI_TUKETICI_ORAN` | 0 | GiderPusulasiKapisi |
| `STOPAJ_BEYAN_GUNU` | 26 (YENİ) | YASAL-4 stopaj borcu — ayın günü |
| `KDV_GENEL_ORAN` | 0,20 | KdvAyarlari |
| `KDV_OZEL_MATRAH_KODU` | boş (`KAPALI`) | KdvAyarlari — K-10 teyidi |
| `RESMI_TATIL` | (yıl başına satırlar, `deger_metin` = tarih) | ResmiTatiller — dinî bayramlar buraya taşınır |
| `FATURA_HADDI_TL` | yok → 9.900 (2025), 12.000 (2026) | V-04 — değer DOĞRULANDI (§ 3.1); kural bu pakette **kodlanmaz** (`KAPALI`) |
| `KMTS_KOD_DESENI` | boş (`KAPALI`) | YASAL-4 § 4.4 — resmî biçim yayımlanınca doldurulur |

Politika (mevzuat olmayan) ayarları — risk puanları, dosya sınırları, ön-dolgu iskontosu — **bu tabloya girmez**, appsettings'te kalır (karışmasın; "yasal" etiketi yalnız mevzuata).

### 4.3 Servis ve uçlar
- **`Kuyumcu.Api/Common/YasalParametreServisi.cs` (YENİ, singleton + bellek önbelleği):**
  - `decimal Sayi(string anahtar, DateOnly yerelGun)` / `string Metin(...)` / `ParametreSatiri Satir(...)`.
  - Değer bulunamazsa **fail-closed**: `YasalParametreYokException` → işlem 500 değil **503 + "Yasal sınır tanımlı değil; destek ile görüşün"** (işlem yapılmaz, sessiz varsayılan YOK). Bu, "tablo boş → kod varsayılanı 30.000'e düş" yolunu kapatır.
  - `DateOnly IslemGunu(DateTime utc)` → `Zaman.UtcdenYerele` (tevsik/MASAK'la aynı yerel gün kuralı, K5).
  - Önbellek: açılışta tüm tablo; her gece 00:00 yerel ve sistem ucu yazınca tazelenir. Önbellekli değer ile DB farkı `GET /sistem/yasal-parametreler/saglik` ile ölçülür.
- **Mevcut ayar sınıfları kaldırılmaz, İNCELTİLİR:** `TevsikEsikleri.Had`, `MasakEsikleri.KimlikEsigi` vb. **tarih alan** bir çağrıya dönüşür: `Had(DateOnly gun)`. Tüm çağrı noktaları (grep `Esikler.Had`, `KimlikEsigi`, `PusulaDuzenlemeGunu`, `EsnafMuafStopajOran`, `SaklamaYili`, `SupheliBildirimIsGunu`) işlem gününü geçirir. Derleyici zorlaması: tarihsiz özellik **silinir** (unutulan çağrı derleme hatası olur).
- **appsettings bölümleri** yalnız **tohumlama/geçersiz kılma** için kalır; açılışta appsettings değeri ile tablonun **bugünkü** değeri farklıysa sunucu **açılmaz** (UretimAcilisDenetimi deseni) — iki kaynak sessizce ayrışmasın.
- **Uçlar:**
  - `GET /api/v1/yasal-parametreler?tarih=YYYY-MM-DD` — SAHİP/MÜDÜR/MUHASEBE; o gün geçerli tüm satırlar (sade açıklama + dayanak + teyit).
  - `GET /api/v1/yasal-parametreler/gecmis?anahtar=` — tarihçe.
  - `GET /api/v1/yasal-parametreler/yaklasan` — önümüzdeki 30 günde başlayacak değişiklikler (Panel bandı için; tüm roller, kişisel veri yok).
  - `POST /api/v1/sistem/yasal-parametreler` — **yalnız SistemAnahtari** (JWT ile erişilemez; `SistemAnahtariKapisi`, F-2-min deseni). Gövde: anahtar, değer, başlangıç, dayanak, kaynak_url, teyit. Geçmiş tarihli başlangıç **reddedilir** (409) — geçmiş yeniden yazılamaz; geçmiş düzeltmesi yalnız migration + denetim kaydıyla.
  - Mevcut `GET /masak/esikler` (`MasakModulu.cs:355-356`) ve `GET /tevsik/gunluk` (`TevsikController.cs:42-43`) aynı servisten okur (bugün + opsiyonel `tarih`).

### 4.4 "Geçmiş işlem eski hadle" kuralı — uygulanacak yerler
| Yer | Hangi gün |
|---|---|
| Yeni işlem kapıları (tevsik, MASAK, gider pusulası) | İşlemin sunucu anı (bugün) |
| İade/storno tevsik kontrolü (`SatisModulu.cs:1960`, `:2076`) | İade anı (yeni nakit akışı bugündür) — **değişmez**; ama iadenin kaynağının "haddi aştı mı" sorusu kaynak işlemin günüyle |
| TB-15 taksit (YASAL-2) | **Kaynak işlemin günü** (işlem tutarı o günün haddiyle kıyaslanır) — ⚠ mali müşavir teyidi (§ 3.2) |
| TB-16 geç ihlal taraması (YASAL-2) | İhlal gününün haddi; 5 iş günü o günün takvimiyle |
| MASAK saklama bitişi (YASAL-3) | Kayıt anındaki saklama yılı |
| Risk structuring bandı (`RiskServisi.cs:248-256`) | Her işlemin kendi günündeki `MASAK_KIMLIK_ESIGI_TL` |
| Uyum/eşik raporları ("eşik üstü işlemler") | Satır bazında işlemin günü |
| Veri göçü (P-47 içe aktarım) geçmiş tarihli satırlar | Satırın tarihi |
| Gider pusulası süre takibi (YASAL-4) | Teslim günü |

### 4.5 Ekranlar — ekranda nasıl görünür
| Yer | Görünüm | Tür |
|---|---|---|
| Tezgâh (satış/alış/hurda) | Değişiklik yok; sınır aşılınca mevcut tek cümlelik bant, rakam tablodan gelir | engel (mevcut) |
| Panel | Yeni değer 7 gün içinde başlıyorsa mavi bant: "1 Ocak'tan itibaren nakitte günlük sınır 30.000 TL yerine X TL." (yalnız SAHİP/MÜDÜR) | otomatik bilgi |
| Panel | Tablo bu yıl için bayram günü içermiyorsa SAHİP'e sarı bant: "Bu yılın bayram tatilleri girilmedi; yasal süreler 1–2 gün erken hesaplanabilir." | uyarı |
| Ayarlar → **"Yasal sınırlar"** (YENİ, salt okunur, SAHİP/MÜDÜR/MUHASEBE) | Tablo: sade ad · bugünkü değer · ne zamandan beri · dayanak (katlanır) · "Doğrulandı / Mali müşavir teyidi bekliyor" rozeti · "Geçmiş" katlanır listesi | bilgi |
| Yardım | Her satırın uzun açıklaması (jargon yalnız burada) | bilgi |

Basit modda "Yasal sınırlar" sayfası menüde **yok** (Tüm ekranlar'dan açılır); Panel bandı Basit'te de görünür (tek cümle).

---

## 5. Kapsam dışı
- Tabloyu **kuyumcunun** ekrandan düzenlemesi (ulusal rakamlar; yanlış giriş ceza doğurur). Yalnız sistem ucu + migration.
- Kiracı bazında "daha sıkı" had (ör. dükkânın kendi 20.000 TL iç sınırı) — ayrı politika paketi.
- Fatura haddi (V-04) ve MASAK beyan kademelerinin (M-06) **açılması** — teyit gelene kadar `KAPALI/TEYITSIZ` satır olarak durur.
- Resmî Gazete'den otomatik çekme (API yok).

---

## 6. Kabul ölçütleri
| # | Ölçüt | Kanıt |
|---|---|---|
| K-1 | Tohum tablosunda `TEVSIK_HADDI_TL`: `Sayi(…, 2024-11-29)=7000`, `Sayi(…, 2024-11-30)=30000`, `Sayi(…, 2015-12-31)` → **tanımsız (503)** | saf test |
| K-2 | Sınır: 2024-11-29 23:59 yerel işlem 7.000 hadle; 2024-11-30 00:00 yerel işlem 30.000 hadle (UTC 20:59/21:00 geçişi) | saf test |
| K-3 | Tablo boşken tevsik kapısı çağrılırsa işlem **yazılmaz** ve 503 döner (kod varsayılanına düşmez) | HTTP testi + `hareketler` sayımı 0 |
| K-4 | İleri tarihli satır (yarın başlayan 40.000) eklenir → bugün 30.000,01 nakit **400**; saat yerel 00:00'ı geçince (test saati) 35.000 nakit **200** — **yeniden başlatma yok** | HTTP testi (saat soyutlaması `Zaman`) |
| K-5 | Geçmiş tarihli başlangıçla `POST /sistem/yasal-parametreler` → 409; JWT ile → 401/403 | HTTP testi |
| K-6 | Aralık çakışması: aynı anahtara çakışan iki açık uçlu satır yazılamaz | DB testi |
| K-7 | appsettings `Tevsik:Had` ≠ tablonun bugünkü değeri → sunucu açılmaz, günlükte tek satır sebep | açılış testi |
| K-8 | `grep -rn "Esikler.Had\b\|KimlikEsigi\b" Modules` → tarihsiz kullanım 0 | denetim.sh yeni sayaç `YASAL_PARAM_TARIHSIZ -eq 0` |
| K-9 | `GET /yasal-parametreler/yaklasan` 7 gün içindeki değişikliği döner; Panel bandı çizilir (bUnit) ve 8 gün kala çizilmez | HTTP + bUnit |
| K-10 | "Yasal sınırlar" ekranı 1280/375 · Basit/Pro · açık/koyu 8 görüntü, taşma 0 | gerçek tarayıcı |
| K-11 | Tohum migration'ı sonrası `SELECT COUNT(*) FROM yasal_parametreler WHERE teyit='DOGRULANDI'` = § 3.1 satır sayısı; her satırda `kaynak_url` dolu | canlı SQL |
| K-12 | Mevcut tüm tevsik/MASAK sınır testleri (29.999,99/30.000/30.000,01; 184.999,99/185.000) değişmeden yeşil | `dotnet test` |
| K-13 | Risk skorunda 2024 tarihli 150.000 TL işlem, o gün eşik 185.000 ise "eşiğe yakın" bandında sayılır; eşik sonradan değişse de sayım değişmez | DB testi |

---

## 7. Mutasyon önerileri
1. `Sayi()` içinde `gecerlilik_bitis` karşılaştırması `<` yerine `<=` → K-2 KIRMIZI.
2. Gün hesabı UTC ile yapılsın (yerel değil) → K-2 KIRMIZI.
3. Değer yokken `30000m` dönülsün → K-3 KIRMIZI.
4. Önbellek gece tazelenmesin → K-4 KIRMIZI.
5. Sistem ucunda geçmiş tarih kontrolü silinsin → K-5 KIRMIZI.
6. Açılış eşitlik denetimi silinsin → K-7 KIRMIZI.
7. RiskServisi bugünkü eşikle okusun → K-13 KIRMIZI.

---

## 8. Riskler
- **Yanlış geçmiş veri:** tohumlanan geçmiş rakam yanlışsa geçmiş işlemler yanlış yorumlanır. Azaltım: yalnız § 3.1'de birincil kaynağı olan satırlar DOĞRULANDI tohumlanır; kaynağı olmayan geçmiş aralık **yoktur** (o güne ait sorgu 503 verir, uydurulmaz). Bu, 2016 öncesi göç verisinde tevsik taramasını durdurur — kabul.
- **Tek sunucu varsayımı:** önbellek birden çok API örneğinde tutarsız kalabilir → tazeleme gece + yazma sonrası; ölçüm ucu var.
- **Kapsam şişmesi:** 11 çağrı noktası (grep) tarih parametresi alacak; SATIŞ şeridinin dosyalarına dokunur → çakışma planı § 10.
- **ADR-0005 değişir:** "appsettings'ten okunur" → "tarihli tablodan okunur; appsettings tohum ve eşitlik denetimi". ADR güncellemesi paketle birlikte.
- **Bayram verisi:** `RESMI_TATIL` yıllık elle girilir (Diyanet ilanı); girilmezse süreler erken dolar (kuyumcu lehine hata değil ama gürültü) → Panel uyarısı + kurucuya yıllık hatırlatma bildirimi.

---

## 9. Tahmini süre (en çok 2 tur)
- Tur 1: tablo + migration + servis + 11 çağrı noktası + uçlar + testler **~4–5 saat**; ekran + Panel bandı **~1,5 saat**.
- Tur 2: düzeltme + tarayıcı kanıtı **~2 saat**.
- Toplam **~1 iş günü** (tahmin; ölçülmedi).

---

## 10. Şerit dosya alanı önerisi
**YASAL-1 yazar:**
- `dotnet/Kuyumcu.Api/Common/YasalParametreServisi.cs` (YENİ), `Data/YasalParametre.cs` (YENİ), `Data/KuyumcuDbContext.cs` (yalnız DbSet + indeks), `Data/Migrations/*_YasalParametreler.cs` (YENİ)
- `dotnet/Kuyumcu.Api/Modules/Tevsik/TevsikKurali.cs`, `Tevsik/TevsikController.cs` (TevsikKapisi.cs'e yalnız imza değişimi — gövdesi YASAL-2'nin)
- `dotnet/Kuyumcu.Api/Modules/Masak/MasakKuralMotoru.cs`, `Masak/RiskServisi.cs` (yalnız eşik okuma satırları), `Masak/MasakModulu.cs` (yalnız `esikler` ucu)
- `dotnet/Kuyumcu.Api/Modules/Hurda/GiderPusulasiKapisi.cs` (yalnız `HurdaAyarlari` okuma), `Modules/Fatura/KdvAyarlari.cs` (yalnız okuma)
- `dotnet/Kuyumcu.Api/Common/ResmiTatiller.cs`, `Program.cs` (yalnız kayıtlar), `Modules/Sistem/SistemModulu.cs` (yalnız yeni uç)
- `dotnet/Kuyumcu.Api/appsettings.json` (yalnız `_not` metinleri)
- `dotnet/Kuyumcu.Web/Pages/YasalSinirlar.razor` (YENİ), `Pages/Panel.razor` (yalnız yeni bant bileşeni çağrısı — 1 satır), `Bilesenler/YasalDegisiklikBandi.razor` (YENİ)
- `dotnet/denetim.sh` (yeni `YASAL_PARAM_*` bloğu), `dokumanlar/02-mimari/adr/0005-…md`
- Testler: `YasalParametreTestleri.cs`, `YasalParametreHttpTestleri.cs` (YENİ)
**Çakışma:** `SatisModulu.cs`, `CariModulu.cs` çağrı noktaları → tarih parametresi eklemek için **tek satırlık** değişiklik; SATIŞ şeridi açıksa kapıda birleştirilir. `TevsikKapisi.cs`/`MasakKapisi.cs` gövdeleri ŞUBE-KAPSAM (ŞK-A) ve YASAL-2/3'e aittir → sıra: **YASAL-1 → ŞK-A → YASAL-2 → YASAL-3**.

---

## 11. Kurucu / mali müşavir kararı gereken noktalar
1. TB-15 taksitte hangi günün haddi: **işlem (sözleşme) günü** mü, **ödeme günü** mü? (Öneri: işlem günü; teyit.)
2. `MASAK_AMAC_BEYANI_ESIGI_TL` = 200.000 TL rakamının dayanağı (bugün kodda "işlem amacı zorunlu" başlangıcı) — birincil kaynak bulunamadı (§ 3.2).
3. Tablonun yıllık bakımı (bayramlar, yeni tebliğ) kimin görevi — ByGatech destek süreci mi, kuyumcu mu? (Öneri: ByGatech; sistem ucu yalnız bizde.)

---

## 12. Ek — Çelişki (7) "has 3 hane" kontrolü (30.09 ağacı)
- **Büyük ölçüde kapandı:** `Kuyumcu.Web/Servisler/Bicim.cs:81` `Has(n) => n.ToString("F4")`; `Bilesenler/Fis/FisModelleri.cs:292-293` `HasYuvarla(…,4)` + `"#,0.00##"` (0,0003 kaybolmaz); `Bicim.cs:93-96` `KarHasGram` Pro'da `N4`.
- **KALAN 1 NOKTA:** `Kuyumcu.Web/Bilesenler/Fis/FisBakiyeRozeti.cs:28-30` `HasGram(...) => Math.Abs(gram).ToString("N3", Tr) + " g has"` — fişteki **müşteri bakiye rozeti** has bakiyesini hâlâ **3 haneyle** yazıyor (`:43` çağrısı). 0,0004 has bakiyeli müşteri "0,000 g has" görür.
- Öneri: yasal paket değil, **BICIM-2** (kuyrukta) kapsamına tek satırlık düzeltme + bUnit testi (0,0004 → "0,0004"). Bu şartnamenin kapsamına alınmadı.
