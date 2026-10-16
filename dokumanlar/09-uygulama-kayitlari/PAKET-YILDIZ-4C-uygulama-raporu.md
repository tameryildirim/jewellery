# PAKET YILDIZ-4C — uygulama raporu · TUR 3 (09.10.2026 23:10–, "Tekrar dene")

**Durum: YARIM.** Tasarım hakeminin iki KRİTİK bulgusu kodda kapatıldı. bUnit testleri, 8 mutasyon, derleme ve denetim (veritabanı gerektirmeyen kısım) tamam.
Ama bu turun **gerçek Chrome ölçümü, canlı satış/SQL kanıtı ve tam süiti KOŞULAMADI**: Docker Desktop 23:26'dan beri motoru başlatamıyor (`docker ps` → "Docker Desktop is unable to start"), `kuyumcu-mysql` (3306) dinlemiyor.
Görev sınırı Docker/disk incelemesini yasaklıyor; bu yüzden müdahale edilmedi. Kurucu kuralına göre ekrana dokunan iş tarayıcıda ölçülmeden GEÇTİ sayılmaz → **YARIM**.

## 0. Bu turda kapatılan hakem bulguları
### [KRİTİK-1] Müşteri katmanı 30.000 ₺'de kendiliğinden açılıyor, kalemleri ve toplamı örtüyordu
- `Pages/Islemler.Yildiz.cs`: `YildizMusteriGorunur` artık **yalnız `yildizMusteriAcik`** ("Değiştir"). Önceden `pesinKimlikGerekli || PesinEsikUstu || pesinHata` da açıyordu.
- Kapı AYNI (veri/yasal davranışa dokunulmadı):
  - Eşik üstünde Kaydet kilitli kalır.
  - Sebebi Fark satırında yazar (`#yildiz-tezgah-not`, mevcut `KaydetKilitSebebi`: "… müşteri seçin").
  - Sunucu reddi hata satırında görünür.
  - Peşin Müşteri yükleme hatası katman kapalıyken Fark not satırına düşer (`Uyarilar`, UX-S28). Katman açıkken yalnız katmandaki `#islem-pesin-hata`'da görünür; iki kez yazılmaz.
- `tema-yildiz.css`: katman `position: absolute; z-index: 30` → **`position: static`**, yani akışta.
  - Kalemleri aşağı iter, örtmez. Genişte `flex: 0 1 auto; min-height: 0`, katman `max-height: 50vh` ile kendi içinde kayar. Toplam `flex-shrink: 0` olduğu için kesilmez.
  - Zemin opak (`var(--yuzey-govde)`). 375'te katman da akışta; altındaki kalem sızamaz.

### [KRİTİK-2] Karmada BASİT "SATAN" satırı yarıdan kesik; eski `.kart` ve `.btn` öğeleri görünüyor
- `Pages/Islemler.razor` `OdemeParcasi`: `FisSatici` Yıldız tezgâhında çizilmiyor (`&& !YildizTezgahMi`).
  - BASİT'te bu satır salt okunur bilgiydi: gövdeye `personelId` girmez, sunucu oturum kullanıcısını yazar. İşlev kaybı yok.
  - PRO'daki satıcı seçimi müşteri katmanındaki "+ Satıcı ve fiyat listesi" kalıbında kaldı.
- `Bilesenler/Fis/FisOdemeBacaklari.razor`: yeni `[Parameter] bool Yildiz`. Bu parametre yalnız çizimi değiştirir; kurallar, id'ler ve geri çağrılar AYNI.
  - Dış sarmalayıcı `kart fis-odeme-kart` → `yz-bacaklar`. Özgün kart başlığı ("Ödeme" + "+ Başka ödeme ekle") çizilmez; Yıldız'ın kendi hapı var.
  - Satır = tuval `.sat` kalıbı. Solda `.yz-bacak-ad` içinde araç seçicisi ve soluk "· Ana Kasa" eki var (`YildizKasaEki`). Kasa eki, kasa seçicisi çizilmiyorsa (değer otomatikse) yazılır. Sağda tutar, en sağda **.b2 "Çıkar"** (✕ ve `.btn` yok; aria-label "N. ödeme satırını çıkar").
  - "+ Taksit", "Kuru yeniden al" ve "Kalanı ata" düğmeleri `.btn` yerine `.yz-b2`.
  - PRO'daki "Fiş toplamı / Ödeme toplamı" satırları ve karma `FisParaUstu` (#islem-para-ustu-ac) Yıldız'da çizilmez. Toplam ve Fark tuvalin kendi bloklarında; para üstü "+ Para üstü" hapında.
  - "Kalan nakit hakkı" satırı yasal bilgi olduğu için korundu. Yalnız sınıfı tuvalin not satırı (`yz-s yz-tezgah-not`); belirsiz eşleşme tablosunda.
- Sayfa sarmalayıcısı `kart islem-fis-kart` → Yıldız'da `islem-fis-kart`. Önceden görünüm şeffaftı ama hakem ESKI sayacında 1 sayılıyordu.
- CSS:
  - Etiket gizleme seçicileri `>` yerine soy seçici oldu (araç seçicisi artık `.yz-bacak-ad` içinde).
  - ✕ için `left: -40px` ve kaydırma kutusunun `margin-left: -48px` kuralları kaldırıldı.
  - Dar ekranda satır ızgarası `minmax(0,1fr) 130px auto`.
- Öteki 8 şema AYNEN: `Yildiz=false` → eski markup birebir. Y4C_45'in platin kolu bunu sınıyor (kart, ✕ `.btn`, Satan var).

### Ek (kendi alanım, başka şeridin yan etkisi)
- **Y4B_14 kırmızısı:** EKSIK-B1-SATINALMA (t5) menüye `/satin-alma` ve `/irsaliyeler` ekledi; Yıldız "Tüm ekranlar"da bu iki karenin ikonu boştu (`d=""`).
  - `YildizIkonlari.Kare`'ye menünün kendi seçtiği **tuval ikonları** eklendi: Ürünler ve Faturalar ikonu. Yeni çizim uydurulmadı; belirsiz eşleşme tablosunda.
- **denetim B-3 (ESIK_ISTEMCIDE):** ilk koşumda 5/4 çıktı. Sebep bu turun yorumundaki "30.000 ₺" metniydi; yorum "Peşin Müşteri haddi" olarak değiştirildi → 4.

## 1. Migration
Gerekmedi (şema değişikliği yok; API/DTO/servis değişmedi).

## 2. Testler
| Koşum | Toplam | Geçti | Kaldı |
|---|---|---|---|
| `FullyQualifiedName~Yildiz4C\|Yildiz4B\|Yildiz3\|Yildiz2T\|YildizAmblem` (son kod) | 120 | 120 | 0 |
| `FullyQualifiedName~Yildiz` (son kod) | 184 | 182 | 2 |
| Tam süit | — | — | **KOŞULAMADI** (MySQL yok) |

- Kırmızı 2: `Yildiz1HttpTestleri.Y1H1` / `Y1H2`. Hata: `MySqlConnector.MySqlException: Unable to connect to any of the specified MySQL hosts`. Docker yüzünden; bu paketin ürünü değil.
- **Yeni testler (Yildiz4CTestleri.cs):**
  - `Y4C_44` (BASİT + PRO): eşik üstünde katman kendiliğinden açılmaz; sebep Fark satırında; "Değiştir"le açılır; seçince kilit kalkar.
  - `Y4C_45` (BASİT + PRO): karmada eski kart/`.btn`/Satan yok; "Çıkar" .b2; "· Ana Kasa" eki; satır çıkarılır; platin karma özgün hâlinde.
  - `Y4C_46`: CSS — katman akışta ve opak; "Çıkar" grid 3. sütunda; ✕ ve −48 px kuralları yok.
  - `Y4C_47`: Peşin yükleme hatası katman kapalıyken not satırında, açıkken tek yerde.
- **Uyarlanan testler:**
  - `Y4B_44`: CSS `position: absolute` → `static`.
  - `Y4B_45`: katman artık kendiliğinden açılmadığı için gerçek müşteri "Değiştir"le seçiliyor.
  - `Y4C_41`: ✕ `left: -40px` yerine `.yz-bacak-sil` `grid-column: 3`.

## 3. Mutasyon (tur 3) — `r3/mut.py`
Yöntem: yedek `/private/tmp/kuyumcu-serit/mut-yedek/pYILDIZ-4C-r3/` → tek eşleşmeli değiştirme (sayı = 1 doğrulanır) → derleme → `--filter Yildiz` → yedek geri → `filecmp` birebir.
Her satırda görülen Y1H1/Y1H2 kırmızısı MySQL yokluğundandır; tabloda yalnız ayırt edici kırmızılar var.

| Kod | Mutasyon | Kırmızı test(ler) | Geri alındı |
|---|---|---|---|
| M17-01 | Katman yine eşikte kendiliğinden açılır (`\|\| PesinEsikUstu`) | Y4C_44 (BASİT+PRO) | cmp AYNI |
| M18-01 | Yıldız bacak sarmalayıcısına `kart` geri | Y4C_45 (BASİT+PRO) | cmp AYNI |
| M19-01 | "Çıkar" `.btn` | Y4C_45 (BASİT+PRO) | cmp AYNI |
| M20-01 | Satan satırı Yıldız'da yine çizilir | Y4C_45 (BASİT) | cmp AYNI |
| M21-01 | Katman CSS `position: absolute` | Y4B_44, Y4C_46 | cmp AYNI |
| M22-01 | Kasa eki hiç yazılmaz | Y4C_45 (BASİT+PRO) | cmp AYNI |
| M23-01 | Sayfa sarmalayıcısı Yıldız'da da `kart` | Y4C_45 (BASİT+PRO) | cmp AYNI |
| M24-01 | Peşin yükleme hatası not satırına düşmez | Y4C_47 | cmp AYNI |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln` → **0 uyarı, 0 hata**.
Not: ilk tur-3 derlemesinde test projesinde 6 uyarı görüldü (SUBEKAPSAMA, KesifP2, SonKalanlar, Yildiz2Testleri — xUnit/CS8601). Hiçbiri bu turun dosyası değil. Son derleme artımlı olduğu için yeniden sayılmadı.

## 4. Denetim (`bash dotnet/denetim.sh`, tur 3)
309 ✓. ✗ satırları:
- **P-30 DEMO_INSAN_SURESI:** bilinen, kapsam dışı.
- **GIRDI-2 TEST_GECERSIZ_FIKSTUR (1):** `MASAKESZAMANSinirTestleri.cs:187`, sahibi t5.
- **CARI_BAKIYE_AYNA_UYUSMAZ (canlı) — ÖLÇÜLEMEDİ:** kuyumcu-mysql yok. Denetimin `docker ps` alt süreci Docker yüzünden asılı kaldı; yalnız KENDİ denetim sürecimin (PID 62619) alt süreçleri durduruldu ki betik bitsin.
- **B-3 ESIK_ISTEMCIDE 5/4:** bu turun yorumu; düzeltildi, yeniden sayım 4.

Bu paketin sayaçları ✓.

## 5. Gerçek Chrome ölçümü — KOŞULAMADI
- Ölçüm aracı hazır: `/private/tmp/kuyumcu-serit/pYILDIZ-4C/r3/hal.mjs`. Hakem `hal.mjs`'nin kopyasıdır ve bütün hâlleri koşar (00-boş … 10-başarı). Hâl başına şunları yazar:
  - `PROB`: katman açık mı, katman/kalemler/toplam kutuları, toplamın ortasındaki öğe, "Satan" sayısı.
  - Yeni hâl: `08b-esik-musteri-acik`.
- 8 kombinasyon (BASİT/PRO × koyu/açık × 1440/375) API (5160) ve tohumlu kiracı ister; MySQL olmadan oturum açılamıyor.
- MySQL gelince sıra:
  1. `demo-tohumla.js --prova-kiraci --sema yildiz`
  2. Vekil 5161
  3. `node hal.mjs <MOD> <TEMA> <W> <CDP 9701-9708> [basari]`
  4. ESKI=0, PROB.musteriAcik=false (08-esik), toplamUstundeki=TOPLAM, satan=0 doğrulanır.
  5. sikisma-olc + tuval-kiyas + sema-fark.

## 6. Canlı satış + SQL — KOŞULAMADI
Sebep aynı. Satış davranışı kodda değişmedi: `Y4C_23` (POST gövdesi platinle birebir, BASİT + PRO) yeşil. Karma satır ekleme `Y4C_22` (özgünle aynı satırlar) yeşil.

## 7. Temizlik
- API 5160 (PID kayıtlı: `r3/api.pid`) bu turda başlatıldı ve yalnız kendi PID'iyle durduruldu.
- Bu turda test kiracısı **açılmadı** (MySQL yok); silinecek kiracı yok. tenants sayısı ölçülemedi.
- QA-TEST, 5199, 5200 ve 5241'e dokunulmadı.

## 8. Olay (Anayasa md.1 — gizlenmez)
- Tohum betiğinin anahtar biçimini anlamak için `head -c 20 …/pYILDIZ-4C/anahtar` çalıştırdım. Bu, ilk koşumda (16:02) üretilmiş **yerel sistem anahtarının ilk 20 karakterini** çıktıya düşürdü.
- Anahtar yalnız o koşumun (artık durmuş) 5160 API sürecine ortam değişkeniyle verilmişti; kalıcı yapılandırmada değil.
- Bu tur için yeni anahtar üretildi (`r3/anahtar`, 600); eski anahtar kullanılmıyor. Eski dosyanın silinmesi kurucuya/orkestratöre bırakıldı (rm kancası).

## 9. DoD sözleşmesi
Yeni varlık ya da uç yok; yalnız görünüm. Düzenleme yolları:
- Ödeme satırı: araç seçicisi, tutar, "Çıkar" (aynı `BacakSil`).
- Müşteri: "Değiştir".
- Kalem: `.k` satırı.
- Satan (PRO): "+ Satıcı ve fiyat listesi".

Finansal varlıklarda silme yok.

## 10. Belirsiz eşleşme (orkestratör kurucuya gösterir)
| Öğe | Uygulamada | Neden |
|---|---|---|
| Karma satırı çıkarma | Satırın sağ ucunda .b2 "Çıkar" | Tuvalde karmadan satır çıkarma çizilmemiş; kalem düzenlemesindeki "Çıkar" (.b2) kalıbı |
| "· Ana Kasa" eki (karma) | Araç seçicisinin yanında soluk 15 px | Tuval "Nakit · Ana Kasa" satırı |
| Kalan nakit hakkı (karma, nakit satırı varken) | Tuvalin not satırı (15 px soluk) | Yasal bilgi; gizlenmedi |
| Müşteri katmanı | Akışta, kalemleri iter | Tuvalde seçim hâli yok; hakemin iki seçeneğinden "aşağı itsin" |
| /satin-alma, /irsaliyeler kareleri | Tuvalin Ürünler / Faturalar ikonu | Tuvalden sonra eklenen ekranlar; menünün kendi ikon seçimi |
| "− Tek ödeme", 375 alt çubuk, kimlik çekmecesi | Önceki turla aynı | — |

## 11. Dokunulan sayfalar
- `Pages/Islemler.razor`: Yıldız koşullu üç nokta (sarmalayıcı sınıfı, Satan, `Yildiz=` parametresi).
- `Pages/Islemler.Yildiz.cs`.
- Gerekçe: hakem 4C KRİTİK-1/2. Bu sayfalar 4A/4B/4C tur 1'de de açılmıştı.

## 12. Değişen dosyalar (tur 3)
- `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisOdemeBacaklari.razor` — `Yildiz` dalı.
- `dotnet/Kuyumcu.Web/Pages/Islemler.razor`.
- `dotnet/Kuyumcu.Web/Pages/Islemler.Yildiz.cs`.
- `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css`.
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Yildiz.cs` — `OdemeSatiriCikar` TR+EN.
- `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizIkonlari.cs`.
- `dotnet/Kuyumcu.Tests/Yildiz4CTestleri.cs`, `Yildiz4BTestleri.cs`.

## 13. Koşul karşılama (tur 3)
| Koşul | Durum | Kanıt |
|---|---|---|
| Build 0/0 | ✓ | `r3/build-son.log` |
| Kendi testleri 0 başarısız | ✓ | 120/120. Yildiz 182/184; 2 kırmızı MySQL bağlantısı (Y1H) |
| Mutasyon ≥4, kırmızı, cmp AYNI | ✓ | 8/8 (`r3/mut.out`) |
| denetim (P-30 hariç) bu paket için TEMİZ | ✓ | B-3 düzeltildi; kalan ✗ t5 ve MySQL |
| Tam süitte yeni kırmızı yok | ✗ KOŞULAMADI | MySQL yok |
| Eski öğe Yıldız'da görünmez (gerçek Chrome ESKI=0) | ✗ ölçülemedi | bUnit Y4C_45 ✓ |
| Katman kendiliğinden açılmaz, kalemleri örtmez (Chrome) | ✗ ölçülemedi | bUnit Y4C_44 + CSS Y4C_46 ✓ |
| Satış davranışı aynı | Kısmi | Y4C_23 ✓; canlı SQL yok |
| 8 eski şema FARK 0 | ✗ ölçülemedi | bUnit platin kolu ✓ |
| Test kiracısı silindi | — | açılmadı |

## 14. Açık kalanlar
1. **(Engel) Docker Desktop / kuyumcu-mysql kapalı (23:26'dan beri).** Kurucu/orkestratör Docker'ı ayağa kaldırınca § 5–6 ve tam süit koşulmalı. Not: diskte 5,6 GB boş (%99); Docker'ın açılamamasıyla ilgisi doğrulanmadı (görev sınırı).
2. Önceki turun "Dünkü gün sonu" canlı kanıtı (SQL UPDATE izni) hâlâ açık.
3. (t5) GIRDI-2 `MASAKESZAMANSinirTestleri.cs:187`; (önceki tur) `P26TedarikTestleri.Y30_…` tam süit kırmızısı.
4. (Kabuk) uzun kullanıcı adında 1280 taşması; (ölçer) K8 "Kâr-zarar" yanlış pozitif — önceki turdan.

## 15. Kurucu / mali müşavir
Mevzuat hükmü yazılmadı. Eşik sayısı istemciye eklenmedi (B-3 = 4).

---
## EK — Önceki koşumun (tur 1–2, 22:46) raporu aşağıda AYNEN korunmuştur
# PAKET YILDIZ-4C — uygulama raporu (09.10.2026, "Tekrar dene" koşumu)

**Durum: YARIM.** Kod, testler, mutasyonlar, ölçümler ve canlı satışlar tamam. Ama kurucunun "tuvalden her sapma KALDI" kuralına göre iki madde açık kaldı (§ 9):
(1) **Karma ödeme hâlinde tuvalde olmayan eski öğeler görünüyor.** BASİT'te "SATAN" satırı, bacak satırlarında × işareti, "Bu müşterinin bugünkü kalan nakit hakkı" satırı çıkıyor; "· Ana Kasa" soluk eki ise yok. tuval-kiyas ölçtüğü 11 öğede FARK 0 verdi, ama bu öğeler eşleme tablosunda olmadığı için ölçüme girmedi; yan yana görüntüde görünüyorlar.
(2) **"Dünkü gün sonu" satırı bu koşumda canlı olarak yeniden kanıtlanamadı.** Satırı kurmak için kasa hareketini SQL ile düne almak gerekiyordu; izin sınıflandırıcısı bu UPDATE'i reddetti. Kanıt yalnız aynı paketin 16:31 ve 17:27 koşumundan var.

Bu koşum, aynı paketin 16:02–18:33 arasındaki ilk koşumunun devamıdır. O koşum kodu, testleri, 16 mutasyonu ve tam süiti bitirmiş, ama raporu yazmadan kesilmişti. Bu koşumda yapılanlar: son kodla yeniden derleme, filtreli testler, denetim, tam süit, yeni test kiracısıyla canlı satış ve SQL, 42 hâl tuval kıyası, sıkışma ölçümü, 8 şema piksel farkı. Kod değişikliği yapılmadı. Mutasyon yedekleri `cmp` ile güncel dosyalarla karşılaştırıldı: 14/14 AYNI. CSS'teki tek fark, AMBLEM şeridinin sonradan yaptığı belirteç taşıması.

## 1. Yapılanlar (ilk koşumun kodu; dosya alanı içinde, yalnız Yıldız koşullu dal)
- **Tezgâh etkileşim hâlleri**
  - Karmada özet satırları artık çizilmiyor; ödeme satırlarının kendisi tuvalin `.sat` kalıbında. 4B'deki KRİTİK "ödemeler iki kez" bulgusu kapandı.
  - "+ Başka ödeme" karmada yeni bacak ekliyor; özgün `BacakEkle` ile aynı kural (Y4C_22). Bacak tavanında düğme kilitleniyor ve sebebini yazıyor.
  - "+ İndirim" yalnız indirim alanını açıyor; kalem listesi tek (Y4C_11).
  - PRO'da kalem düzeltmesi tuvalin `.k` satır kalıbında; eski tablo ve "Sütunlar" yok (Y4C_10, Y4C_25).
  - Barkod yer tutucusu tuval metninde, "(F2)" yok (Y4C_21).
  - Müşteri katmanı "Seni bekleyen" satır kalıbında; süzgeç Türkçe ve seçili müşteri ilk sırada (Y4C_05, Y4C_13).
  - Müşteri değişince önceki kayıt hatası siliniyor ve `MusteriDegisti` çağrılıyor (Y4C_24).
  - Peşin art arda sorusu Fark bloğunda tek kez çıkıyor (Y4C_26).
  - Kaydet ve bantlar ekranda: genişte sabit, darda altta sabit (Y4C_42).
- **Bugün'deki iki satır** (yalnız okuma)
  - "Dünkü gün sonu kapatılmadı · Kasa · Kapat → /kasalar": `Rapor.KasaDefteri(dün)`; hareketli ve KAPALI olmayan kasa (Y4C_01, Y4C_30, Y4C_31).
  - "X'in emaneti · N g has — 30 günü aştı · Aç → /emanet": ALTIN emanet bakiyesinin sıfırdan artıya geçtiği gün. Liste sınıra değerse null döner, gün uydurulmaz. Eşik `EmanetEsikGun = 30` sabiti (Y4C_02, 03, 04, 32, 33).
  - Başka şemada ek istek atılmıyor (Y4C_34).
- **Müşteriler:** arama boşken yanlış boş durum yazmıyor; süzgeç "+ ekle" kalıbında; "Toptancı ·" alt satırı var (Y4C_14).
- **Tüm ekranlar:** tasarım notu metinleri ve "YETKİN OLANLAR" kalktı (4B tur 2'de yapılmıştı; bu pakette dokunulmadı).
- **CSS:** 4C bölümleri yalnız `html:root[data-sema="yildiz"]` kapsamında; sabit renk yok (Y4C_40–43).
- **Kalıcı araçlar:** `betikler/tur/tuval-kiyas.mjs` (genişletildi), `betikler/tur/sema-fark.mjs` (yeni).
- **Bu koşumda eklenen ölçüm betikleri:** `ham/yildiz-4c/olcum-r2/tasan.mjs` (taşan öğe bulucu) ve `kaydir.mjs` (375'te kaydırma sonrası erişim).

Veri, para ve yasal davranışa dokunulmadı: API, DTO, servis ve migration yok. Satış POST gövdesi Platin'le birebir (Y4C_23 BASİT ve PRO).

## 2. Migration
Gerekmedi (şema değişikliği yok).

## 3. Testler
| Koşum | Toplam | Geçti | Kaldı |
|---|---|---|---|
| `FullyQualifiedName~Yildiz4CTestleri` (26 yöntem) | 29 | 29 | 0 |
| `FullyQualifiedName~Yildiz` (4A/4B/4C/Amblem) | 170 | 170 | 0 |
| Tam süit (`trx-pYILDIZ-4C/tam2.trx`, son kodla, tek koşum) | 11969 (yürütülen 11966) | 11965 | 1 |

Tam süitteki tek kırmızı bu paketin değil:
- **Test:** `P26TedarikTestleri.Y30_ikinci_kapora_REDDEDILIR_ve_duzeltme_yolu_yazilidir`.
- **Hata:** sunucunun kapora iletisinde "storno" sözcüğü yok.
- **Sahibi:** KAPORA-EKRAN / t5 şeridi. Bu paket API'ye ve iletilere dokunmadı.
- İlk koşumun tam süitinde (18:33) bu test de kırmızıydı; o koşumdaki Y4B_31 (AMBLEM'in sabit renkleri) ve FaturaEkran Y4C_K12 kırmızıları bu koşumda yeşil.
- **Bu paketin yeni kırmızısı: 0.**

## 4. Mutasyon
Yöntem: kaynak `mut-yedek/` altına kopyalandı → perl ile tek eşleşmeli değişiklik → filtreli test (`Yildiz`) → yedek geri kopyalandı → `cmp`. Bu koşumda 14 kaynak dosya yedekle `cmp` edildi: hepsi AYNI. M12/M16 CSS yedekleri kendi aralarında AYNI; güncel dosyadan tek farkı AMBLEM'in eklediği belirteç bloğu.

Y4B_31 her satırda kırmızıydı. Bunun sebebi o saatteki AMBLEM ağacıydı, mutasyonla ilgisi yok; aşağıdaki tabloda yalnız ayırt edici kırmızılar var.

| Kod | Mutasyon | Kırmızı test(ler) | Geri alındı |
|---|---|---|---|
| M1-01 | Karmada da özet satırları çiz (`@if (true)`) | Y4B_12, Y4C_12, Y4C_20 (BASİT+PRO) | cmp AYNI |
| M2-01 | İndirim alanı yerine kalem parçası | Y4C_20 (BASİT+PRO) | cmp AYNI |
| M3-01 | Müşteri seçici yerine eski müşteri parçası | Y4B_24, Y4B_44, Y4B_45, Y4C_20, Y4C_23, Y4C_24 | cmp AYNI |
| M4-01 | Gün sonu: KAPALI durumu süzülmüyor | Y4C_01, Y4C_31 | cmp AYNI |
| M5-01 | Emanet eşiği `>` yerine `>=` | Y4C_03, Y4C_32 | cmp AYNI |
| M6-01 | Sınıra değen listede yine de başlangıç hesapla | Y4C_02 | cmp AYNI |
| M7-01 / M7-02 | "+ Başka ödeme" yeni bacak tutarı 0 | M7-01 açık bıraktı → test güçlendirildi → M7-02: Y4C_22 (BASİT+PRO) | cmp AYNI |
| M8-01 | Kalem düzeltmesinde 0 değere izin | Y4C_25 | cmp AYNI |
| M9-01 | Peşin sorusu Fark bloğuna geçmiyor | Y4C_26 | cmp AYNI |
| M10-01 | Müşteri değişince `MusteriDegisti` çağrılmıyor | Y4C_24 | cmp AYNI |
| M11-01 | Yıldız barkod yer tutucusu özgün metin | Y4C_21 | cmp AYNI |
| M12-01 | Karmada eski `.kart-baslik` gizleme kuralı silindi | Y4C_41 | cmp AYNI |
| M13-01 | Müşteriler arama-boş dalı kapalı | Y4C_14 | cmp AYNI |
| M14-01 | Başka şemada da Bugün ek yüklemesi | Y4C_34 | cmp AYNI |
| M15-01 | Türkçe iyelik eki tek kalıp | Y4C_04, Y4C_32 | cmp AYNI |
| M16-01 | Dar ekranda alt çubuk `fixed` yerine `sticky` | Y4C_42 | cmp AYNI |

Son mutasyondan sonra temiz derleme yapıldı: bu koşumda `dotnet build Kuyumcu.sln` → 0 uyarı, 0 hata.

## 5. Denetim (`dotnet/denetim.sh`, bu koşum)
315 ✓, 2 ✗ — ikisi de bu paketin dışında:
- **P-30 DEMO_INSAN_SURESI:** kronometreli demo ölçümü; bilinen ve kapsam dışı.
- **GIRDI-2 TEST_GECERSIZ_FIKSTUR:** `Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` (geçersiz IBAN fikstürü). Sahibi MASAK-ESZAMAN / t5 şeridi.

Bu paketin sayaçları ✓: satır içi style 24/196, ModalPencere 48/55, duzenlenenId 1/1, P-36 0.

## 6. Gerçek (başsız) Chrome ölçümü
Ortam: API 5160, vekil 5161, CDP 9714/9716–9719. Kiracı `PAKETYILDIZ4C-TEST Tunalılar Kuyumculuk` (01M4H0HHMD7A501020QPDV69J2), `demo-tohumla.js --prova-kiraci --sema yildiz` ile kuruldu.

### 6.1 Etkileşim hâlleri — tuval-kiyas (1440×900; koyu + açık; BASİT + PRO; 42 koşum)
Çıktı: `ham/yildiz-4c/olcum-r2/hal.out`. Tablolar ve yan yana görüntüler: `ham/yildiz-4c/hal/s1-*`.

| Hâl | BASİT koyu/açık | PRO koyu/açık |
|---|---|---|
| kalem düzeltme (PRO) | — | 7/7 AYNI · 7/7 AYNI |
| karma | 11/11 · 11/11 | 11/11 · 11/11 |
| indirim + para üstü | 7/7 · 7/7 | 7/7 · 7/7 |
| müşteri alanı | 4/4 · 4/4 | 4/4 · 4/4 |
| müşteri seçim satırları | 5/5 · 5/5 | 5/5 · 5/5 |
| Bugün iki satır | **4 AYNI, 3 YOK** · aynı | **4 AYNI, 3 YOK** · aynı |
| hata | 1/1 · 1/1 | 1/1 · 1/1 |
| başarı | 4/4 · 4/4 | 4/4 · 4/4 |
| Peşin art arda sorusu | 4/4 · 4/4 | 4/4 · 4/4 |
| kimlik (MASAK) | 5/5 · 5/5 | 5/5 · 5/5 |
| Müşteriler süzgeç | 2/2 · 2/2 | 2/2 · 2/2 |

Bütün koşumlarda FARK 0 ve taşma 0. Karma koyu BASİT'in ilk çekimi boş döndü (soğuk başlangıç), tek başına yeniden koşuldu: 11/11 AYNI.

Bugün'deki 3 YOK, "Dünkü gün sonu" satırının üç öğesi. Sebep: dünkü kasa hareketini kuracak SQL UPDATE izin sınıflandırıcısınca reddedildi (§ 9). Emanet satırının 4 öğesi canlı veriyle AYNI. Veri SQL ile kuruldu:
- `POST /emanet/giris` 200 (9,16 g has).
- Tohumdaki emanet girişi 31 gün geriye alındı: `EMANET_GIRIS 2026-09-08 …`.
- Fazla giriş `POST /emanet/{cari}/cikis` 200 ile geri alındı.

İlk koşumda (17:27, `hal-son.out`) dünkü kasa senaryosu da kurulmuştu ve Bugün 7/7 AYNI çıkmıştı (BASİT ve PRO, koyu ve açık). Satır metni o koşumun `bugun-sonra.out` dosyasında: "Dünkü gün sonu kapatılmadı Kasa Kapat | href=/kasalar | title=… Ana Kasa".

### 6.2 Eski öğe sayacı (ilk koşum, `g1/durum-*.out`, `yasal-PRO.out`, `kd.out`)
Ölçülen hâller: boş, 1 kalem, 2 kalem, karma, indirim + para üstü, müşteri, yasal eşik, kimlik çekmecesi, hata. Hepsinde `eski=0`, Kaydet ekranda (x=1085, y=804, 291×56). Ancak bu sayaç yalnız eski sınıfları (`.kart-baslik` vb.) sayar; Satan seçicisi, bacak ×'i ve nakit hakkı satırı bu sayaca girmiyor (§ 9).

### 6.3 Sıkışma (sikisma-olc.js) — `/`, `/islemler`, `/cariler`, `/ekranlar` × 1440, 1280, 375 × koyu + açık × BASİT + PRO (48 ölçüm)
Çıktılar: `ham/yildiz-4c/olc-{BASIT,PRO}-dar.json` ve `olcum-r2/olc-*.out`.
- **K1, K3, K5 (TRY), K10, K12, yatay kaydırma: hepsi 0.**
  - İlk ölçümde 1280'de 9 px, 375'te 32 px yatay taşma çıktı. `tasan.mjs` ile kaynağı bulundu: `#yildiz-kullanici` "HAKAN TUNALI K.".
  - Sebep fikstür verisiydi: ad alanına tam ad yazılmıştı. Ad ve soyad tuvaldeki gibi "Hakan / Tunalı" yapıldı ("HAKAN T.") ve yeniden ölçüldü: 0.
  - Not: başlığa AMBLEM'in "MİHENK" yazısı eklendikten sonra uzun kullanıcı adıyla 1280'de taşma oluyor. Kabuk 4A/AMBLEM alanında, açık kalanlarda.
- **K8 jargon "r-zarar" (/ekranlar, BASİT): yanlış pozitif.** Tuvaldeki metin birebir "Kâr-zarar". Ölçerin `\b[a-z]+-[a-z]+` deseni "â"dan sonra bölüyor.
- **K9 üst üste binme (/islemler, 375, BASİT + PRO): gerekçeli.** Dar ekranda Fark ve Kaydet alt çubuğu sabit; kabulün "Kaydet ekranda" maddesi bunu gerektiriyor. Kaydırma 0'dayken "+ Başka ödeme / + Para üstü" hapları çubuğun altında kalıyor. Kaydırınca erişiliyor: `kaydir.mjs` ile önce baska=[647–691], fark=[611–724]; sonra (scrollY 263) baska=[384–428], indirim=[440–484], fark=[611–724] — çakışma yok. Görüntü: `olcum-r2/375-kaydirilmis.png`.
- **K7 hiza (/cariler üst satırı ±14):** 4B'den kalan, tuvalle AYNI ölçüldü (bilgi).

### 6.4 8 eski şema piksel farkı (`betikler/tur/sema-fark.mjs`, bu koşum)
Kiracı geçici olarak platin şemaya alındı; ardından yildiz'e geri alındı (`fark-zincir.out`).
- BASİT: 64 karşılaştırma, **FARK 0**.
- PRO: 64 karşılaştırma, **FARK 0**.
- 3 karşılaştırma ısınma yüzünden yeniden çekildi; ikinci çekimde eşit.

### 6.5 Görüntüler (Read ile bakılan 4)
1. `hal/s1-karma-koyu-basit-yan-yana.png` (tuval | uygulama) — § 9'daki karma sapmaları burada görüldü.
2. `olc-BASIT-375-karanlik-islemler.png` — 375'te alt çubuk.
3. `olcum-r2/s-BASIT-3.png` — Peşin art arda sorusu Kaydet'i doğru biçimde durdurdu.
4. `olcum-r2/i2-PRO-5.png` — PRO açık temada indirim alanı tuvalin alan çizgisi kalıbında.

## 7. Canlı satış + SQL (bu koşum)
| Adım | Beklenen | Gerçek |
|---|---|---|
| BASİT karma (2 kalem; Nakit 5.000 + Kart) → Kaydet → Peşin sorusu → "Hayır" | Son fiş bandı + karma hareketler | "Son fiş: Satış · 22:42 · 18.045,66 ₺"; SQL: aynı `fis_id` ile 2 SATIS satırı (6932.64 ve 11113.02, KARMA, nakit 5000.00); kasa_hareketleri NAKIT 5000.00 + KART 13045.66 |
| PRO indirim %5 (1 kalem) → Kaydet → soru → "Hayır" | Bant + uygulanan iskonto | "Son fiş: … 10.557,37 ₺ · Uygulanan iskonto: 555,65 ₺"; SQL: tl_tutari 10557.37, indirim_tutari 555.65, liste_fiyati 11113.02, NAKIT |
| SATIS sayısı | Artış | 13 → 16 |
| Emanet satırı (31 gün) | Bugün'de satır | "Derya Yıldızlı'nın emaneti · 9,16 g has · 30 günü aştı · Aç" — tuval-kiyas 4/4 AYNI |
| Dünkü gün sonu | Bugün'de satır | Bu koşumda kurulamadı (SQL reddedildi); ilk koşumda var (16:31, 17:27) |

Temizlik:
- API (dotnet run 75807 ve alt süreci 75845) ve vekil (82430) kendi PID'leriyle durduruldu; 5160 ve 5161'de dinleyen yok.
- `zsh kiraci-temizle.sh 01M4H0HHMD7A501020QPDV69J2 --uygula` çalıştırıldı. 75 tenant_id tablosunda kalan satır 0; tenants 21 → 21.
- İlk koşumun kiracısı daha önce silinmişti (hiçbir veritabanında yok).
- QA-TEST, 5199, 5200 ve 5241'e dokunulmadı.

## 8. DoD sözleşmesi
Bu paket yeni varlık ya da uç üretmedi; yalnız görünüm. Düzenleme yolları özgün sayfadan geliyor:
- Kalem: Yıldız `.k` satırında düzeltme.
- Ödeme: bacak satırları.
- Müşteri: Değiştir.
- Bugün satırları: salt okunur; Kapat → /kasalar, Aç → /emanet.
Finansal varlıklarda silme yok.

## 9. Açık kalanlar
1. **(Bu paket, KALDI) Karma hâlde tuvalde olmayan öğeler.**
   - **Ne görünüyor:** BASİT'te "SATAN — Hakan Tunalı Kuyumcu" satırı (`FisSatici`, Islemler.razor `OdemeParcasi`); bacak satırlarında × (bacak silme); "Bu müşterinin bugünkü kalan nakit hakkı: 30.000 ₺" (`FisOdemeBacaklari.razor:203`). "Nakit · Ana Kasa" soluk kasa eki ise karma satırlarında yok; BASİT tek kasada çizilmiyor.
   - **Tuval kalıbı:** Satan için "+ ekle" hapı; kasa için `.sat` soluk ek; ×'in tuvalde karşılığı yok.
   - **Neden bu koşumda düzeltilmedi:** nakit hakkı satırı yasal (tevsik) gösterimi. Satan ve × ise özellik; gizlenirse işlev kaybolur. Paylaşılan Fis bileşenine Yıldız dalı açmak bu koşumun 2 tur sınırında yapılamadı.
   - **Öneri:** Satan → "+ Satan" b3 hapı. Bacak silme → satır sağına b2 "Kaldır". Nakit hakkı → hasımlı pakete devir (Fark bloğundaki "Yasal kurallar uygun · nakit 30.000 ₺ sınırının altında" ile aynı bilgiyi tekrar ediyor).
2. **(Bu paket) "Dünkü gün sonu" satırının bu koşumda canlı yeniden kanıtı.** `kasa_hareketleri` ve `hareketler.tarih` için 1 günlük UPDATE izin sınıflandırıcısınca reddedildi ("Modify Shared Resources"). Kanıt ilk koşumdan (16:31, 17:27) ve Y4C_01/30/31 testlerinden. Kurucu izin verirse aynı senaryo tek komutla kurulup ölçülebilir.
3. **(Başka şerit) Tam süit kırmızısı** `P26TedarikTestleri.Y30_ikinci_kapora_REDDEDILIR_ve_duzeltme_yolu_yazilidir` — KAPORA-EKRAN / t5.
4. **(Başka şerit) denetim GIRDI-2** `MASAKESZAMANSinirTestleri.cs:187` — MASAK-ESZAMAN / t5. **P-30** — kapsam dışı.
5. **(Kabuk, 4A/AMBLEM)** Uzun kullanıcı adında ("HAKAN TUNALI K.") 1280'de başlık 9 px taşıyor; "MİHENK" yazısı eklendikten sonra ortaya çıktı.
6. **(Ölçer)** K8 jargon deseni "Kâr-zarar"ı yanlışlıkla "r-zarar" diye yakalıyor (sikisma-olc.js; deseni Unicode harf sınırıyla düzeltmek gerekiyor).

## 10. Belirsiz eşleşme (orkestratör kurucuya gösterir)
| Öğe | Uygulamada | Neden |
|---|---|---|
| "− Tek ödeme" | Karmada üç hapın ardından b3 hap | Tuvalde karmadan dönüş çizilmemiş; en yakın kalıp b3 hapı |
| Satan / × / nakit hakkı (karma) | Özgün öğeler, Yıldız stiliyle | § 9.1 — tuval kalıbına taşınmalı |
| 375'te alt çubuk | Fark ve Kaydet sabit; içerik kaydırılarak erişiliyor | Tuvalde telefon hâli yok |
| Bugün emanet satırı | "X'in emaneti · N g has · 30 günü aştı · Aç" | Tuvaldeki satırla AYNI (4/4) |
| Kimlik (MASAK) | Ortak çekmece, içerik Yıldız düzeninde | Tuvalde yok; 5/5 AYNI (çekmece kalıbı) |

## 11. Dokunulan sayfalar
`Pages/Islemler.razor` (yalnız Yıldız dalı: barkod yer tutucusu), `Pages/Panel.razor` (Yıldız ek yüklemesi), `Pages/Cariler.razor` (Yıldız dalı). Bu sayfalar YILDIZ-4A/4B'de de açılmıştı. Gerekçe: 4B hakem bulguları ve paket kapsamı (etkileşim hâlleri, Bugün'deki iki satır).

## 12. Değişen dosyalar (ilk koşum; bu koşumda kaynak değişmedi)
- `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/`: `YildizTezgah.razor`, `YildizTezgahVerisi.cs`, `YildizKalemDuzen.razor`, `YildizMusteriSecici.razor`, `YildizMusteriVerisi.cs`, `YildizMusteriler.razor`, `YildizBugunVerisi.cs`, `YildizIkonlari.cs`.
- `dotnet/Kuyumcu.Web/Pages/`: `Islemler.Yildiz.cs`, `Islemler.razor`, `Panel.Yildiz.cs`, `Panel.razor`, `Cariler.Yildiz.cs`.
- `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css`.
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Yildiz.cs`.
- `dotnet/Kuyumcu.Tests/Yildiz4CTestleri.cs` (yeni), `Yildiz4BTestleri.cs`.
- `dokumanlar/09-uygulama-kayitlari/betikler/tur/tuval-kiyas.mjs`, `sema-fark.mjs` (yeni).
- Kanıt dizini: `ham/yildiz-4c/`.

## 13. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Build 0/0 | ✓ | bu koşum 0 uyarı, 0 hata |
| Kendi testleri 0 başarısız | ✓ | Y4C 29/29, Yildiz 170/170 |
| Mutasyon ≥4, kırmızı, cmp AYNI | ✓ | 16 (+M7-02) |
| denetim (P-30 hariç) TEMİZ | ✓ bu paket için | tek diğer ✗ t5'in |
| Tam süitte yeni kırmızı yok | ✓ | 1 kırmızı, başka şeridin |
| Etkileşim hâlleri tuval-kiyas tolerans içinde | ✓ (ölçülen öğeler) | 42 koşum FARK 0 |
| Eski form öğesi Yıldız'da görünmez | ✗ | § 9.1 karma |
| Kaydet ve bantlar ekranda (1440, 375) | ✓ | durum-*.out, 375 sabit çubuk |
| Satış davranışı aynı (POST testi + canlı SQL) | ✓ | Y4C_23, § 7 |
| Bugün'deki iki satır canlı | Kısmi | emanet ✓; dünkü gün sonu yalnız ilk koşumda |
| Müşteriler / Tüm ekranlar kalan bulgular 0 | ✓ | Y4C_14, suzgec 2/2 |
| 8 eski şema FARK 0 | ✓ | 64 + 64 |
| Test kiracısı silindi, tenants eski değerinde | ✓ | 21 → 21, kalan satır 0 |

## 14. Kurucu / mali müşavir
Bu paket mevzuat hükmü yazmadı. Emanet eşiği 30 gün, ekip oyuyla belirlenmiş bir **ekran** eşiği; yasal bir süre değil.
