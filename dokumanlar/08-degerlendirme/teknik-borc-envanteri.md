# Teknik Borç Envanteri

| | |
|---|---|
| **Belge kimliği** | KYM-TB-001 |
| **Sürüm / Tarih** | 1.0 · 2026-09-13 |
| **Düzenleyen** | ByGatech A.Ş. |
| **Onay makamı** | Kurucu |
| **Durum** | GÖZDEN GEÇİRİLDİ |

---

## 0. Yöntem

Teknik due diligence kontrol listelerinde istenen şey "teknik borç yok" beyanı değil, **envanterlenmiş ve fiyatlandırılmış borç listesidir**. Gizlenen borç bulunduğunda tüm raporun güvenilirliğini düşürür; listelenen borç olgunluk göstergesidir.

Her kalem için: **ne**, **nasıl ölçüldü**, **etkisi**, **kapatma maliyeti**, **kimin işi**.

Maliyet birimi olarak adam-gün **verilmemiştir** — bu projede geçmiş hızdan çarpan uydurmak Anayasa md.1'e aykırı sayılmıştır. Yerine **görece büyüklük** (küçük / orta / büyük) ve **bağımlılık** verilir.

---

## 1. Özet

| # | Borç | Önem | Kapatan |
|---|---|---|---|
| TB-01 | Sürüm kontrolünde sır | 🔴 Kritik | Kurucu |
| TB-02 | `bin/` ve `obj/` sürüm kontrolünde (1.408 dosya) | 🔴 Yüksek | Geliştirme |
| TB-03 | CI/CD hattı yok | 🔴 Yüksek | Geliştirme |
| TB-04 | Veritabanında yabancı anahtar yok | 🟠 Orta-yüksek | Geliştirme (ADR-0009) |
| TB-05 | `hareketler.kur_snapshot_id` indekssiz | 🟠 Orta | Geliştirme |
| TB-06 | Kur snapshot temizliğinde TOCTOU penceresi | 🟠 Orta | Geliştirme |
| TB-07 | UI test kapsamı %7,2 | 🟠 Orta | QA |
| TB-08 | Dal kapsamı %29,5 | 🟠 Orta | QA |
| TB-09 | Sayfa içi doğrudan API çağrısı deseni | 🟡 Düşük-orta | Mimari |
| TB-10 | 8 fonksiyon karmaşıklık eşiğinin üstünde | 🟡 Düşük | Geliştirme |
| TB-11 | MASAK kuralının istemcide ikizlenmesi | 🟡 Düşük | Geliştirme |
| TB-12 | ~~Yük ve ölçeklenme davranışı ölçülmedi~~ ✅ **kapandı 2026-09-14** | — | Mimari |
| TB-13 | Runbook / operasyon belgesi yok | 🟠 Orta | Operasyon |
| TB-14 | MFA yok | 🟡 Düşük (bugün) | Güvenlik |
| TB-15 | Tevsik kapısı "kısım kısım ödeme"yi görmüyor | 🟠 Orta | Geliştirme |
| TB-16 | Tevsikte "5 iş günü" bildirim akışı yok | 🟡 Düşük | Geliştirme |
| TB-17 | Fatura satır kalemi yok — taş ibaresi faturaya basılamıyor | 🟡 Düşük-orta | Mimari |
| TB-18 | Sayım düzeltmesi deftere yazılmıyor (**devralınan borç**) | 🟠 Orta | Geliştirme |
| TB-19 | Bazı raporlar varlık sayısıyla ölçeklenen sorgu yapıyor (**devralınan borç**) | 🟡 Düşük | Geliştirme |

---

## 2. Kalemler

### TB-01 🔴 Sürüm kontrolünde sır

**Ölçüm**
```bash
git show bbfdc95:dotnet/Kuyumcu.Api/appsettings.Development.json | grep -iE 'secret|password|anahtar'
```
JWT imzalama sırrı, MySQL root parolası ve alan şifreleme anahtarı **ilk commit'te** mevcut.

**Etki.** Depo herhangi bir zamanda paylaşıldıysa sırlar ifşa olmuş sayılmalıdır. Alan şifreleme anahtarının ifşası, diskteki AES-256-GCM korumasını **tamamen** geçersiz kılar.

**Bağlam.** Devralınan kod tabanından gelir; ByGatech eklemedi. Bu, ciddiyetini azaltmaz.

**Kapatma.** İki adım, sırayla: (1) sırları döndür, (2) git geçmişini temizle (`git filter-repo` / BFG). İkincisi geçmişi yeniden yazar → **Kurucu kararı**. Şifreleme anahtarı değişirse `sifre_surumu` alanı üzerinden rotasyon gerekir.

**Büyüklük.** Rotasyon küçük · geçmiş temizliği orta (koordinasyon gerektirir).

---

### TB-02 🔴 Derleme çıktısı sürüm kontrolünde

**Ölçüm**
```bash
git ls-files | grep -cE '/(bin|obj)/'
→ 1408
```

**Etki.** Her derleme depoyu kirletir. "Hangi değişiklik neyi bozdu" sorusu cevaplanamaz hâle gelir — kod değişikliği ikili dosya gürültüsü içinde kaybolur. Ayrıca `bin/` içindeki `appsettings.json` kopyaları TB-01'i çoğaltır.

**Kapatma.** `.gitignore` düzenlemesi + `git rm --cached`. Dosya değişikliği küçük; ama **commit gerektirir** ve bu projede commit yasağı yürürlüktedir → Kurucu kararı.

**Büyüklük.** Küçük (teknik olarak), orta (karar olarak).

---

### TB-03 🔴 CI/CD hattı yok

**Ölçüm.** `.github/workflows`, `.gitlab-ci.yml`, `azure-pipelines.yml`, `Jenkinsfile` — **hiçbiri yok**.

**Etki.** Kalite kapıları elle çalıştırılıyor. Bir geliştirici koşmayı unutursa kapı yok demektir.

**Hafifletici.** Kapıların kendisi **hazır ve tekrarlanabilir**: `denetim.sh` (11 kural, kendisi mutasyonla sınanmış), `dotnet test` (385), `yetim-tara.sh`, `yedek-al-ve-dogrula.sh`, lisans kapısı. Bunları bir iş akışı dosyasına bağlamak küçük bir iştir.

**Büyüklük.** Küçük.

---

### TB-04 🟠 Veritabanında yabancı anahtar yok

**Ölçüm.** Enforce edilen FK: **0**. 36 mantıksal ilişki uygulama katmanında korunuyor.

**Etki.** Eşzamanlılıkta yetim kayıt riski; kiracı sınırı veritabanı seviyesinde zorlanmıyor.

**Durum.** **Karara bağlandı** — [ADR-0009](../02-mimari/adr/0009-veritabani-yabanci-anahtar-yok.md): kritik ilişkilerde kompozit `(id, tenant_id)` FK, RESTRICT, aşamalı. Dalga 0 (tarama) tamamlandı ve **2 satır kiracı sızıntısı** buldu (kendi sızma testimizin kalıntısı).

**Ön koşul.** Sızıntı satırlarının temizlenmesi — MySQL 8.4 `ADD FOREIGN KEY` kısıtı ihlal eden satır varsa hata verir.

**Büyüklük.** Orta-büyük (4 dalga).

---

### TB-05 🟠 `hareketler.kur_snapshot_id` indekssiz

**Ölçüm**
```sql
SELECT INDEX_NAME, GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX)
FROM information_schema.STATISTICS
WHERE TABLE_SCHEMA='kuyumcu_net' AND TABLE_NAME='hareketler' GROUP BY INDEX_NAME;
→ IX_hareketler_tenant_id_tarih (tenant_id,tarih)
→ PRIMARY (id)
```
`kur_snapshot_id` üzerinde indeks **yok**.

**Etki.** `KurServisi.EskiKurlariTemizle`'deki `NOT EXISTS` alt sorgusu, her aday snapshot için **tüm `hareketler` tablosunu tarar**. Bugün 68 satırda görünmez; 1M satırda gece işini kilitler.

**Kapatma.** TB-04'ün Dalga 3'ü bunu **bedavaya** çözer: MySQL yabancı anahtar için gereken indeksi otomatik oluşturur. Ayrıca eklenmesi gerekmez.

**Büyüklük.** Küçük (veya TB-04 ile sıfır).

---

### TB-06 🟠 Kur snapshot'ında zaman-kontrol/zaman-kullanım (TOCTOU) penceresi

**Tespit.** Temizlik kodunun kendisi tek bir `DELETE` ifadesidir ve kendi içinde atomiktir — orada pencere **yoktur**.

Gerçek pencere akışın bütününde: istemci bir kur snapshot kimliği alır → `IslemDogrulayici.KurDogrula` varlığını doğrular → hareket yazılır. Bu üç adım arasında 90 günlük temizlik işi o snapshot'ı silerse, hareket **yetim referansla** yazılır ve bugün hiçbir mekanizma engellemez.

**Olasılık.** Düşük (yalnız 90 günden eski snapshot'lar silinir; taze işlem taze snapshot kullanır). **Ama sıfır değil** ve etkisi ağırdır: silinen snapshot, o işlemin **kur kanıtıdır**.

**Kapatma.** TB-04 Dalga 3 (FK) bu yolu yapısal olarak kapatır: FK varken snapshot silinemez.

**Büyüklük.** TB-04 ile sıfır; bağımsız çözülürse küçük.

---

### TB-07 🟠 UI test kapsamı %7,2

**Ölçüm.** `coverlet` → `Kuyumcu.Web` satır %7,2 · dal %6,6. 14 sayfadan 2'si test ediliyor (`MasakFormu`, `DenetimIzi`).

**Etki.** Blazor, eşleşmeyen bileşen parametresini **derlemede değil çalışma anında** patlatır. Bu gerçekten yaşandı: `dotnet build` 0 hata verirken 8 bUnit testi kırmızıya döndü.

**Hafifletici.** bUnit altyapısı kuruldu, desen çalışıyor. Kalan iş mekanik.

**Büyüklük.** Orta (12 sayfa).

---

### TB-08 🟠 Dal kapsamı %29,5

**Ölçüm.** Genel dal kapsamı %29,5 (satır %72,4). `Kuyumcu.Api` dal %49,9.

**Bağlam.** Uyum sınıflarında dal kapsamı yüksek: `MasakKuralMotoru` %98,1 · `KiraciYazmaDenetcisi` %97,5 · `AlanSifreleme` %100 · `TutarYazi` %100. Düşük ortalama büyük ölçüde `Kuyumcu.Web`'den (%6,6) ve hata yollarından gelir.

**Etki.** Koşul kombinasyonları sınanmamış. Satır kapsamı yüksek ama dal düşükse, "her satır bir kez çalıştı ama hiçbir `if` iki yönlü denenmedi" demektir.

**Büyüklük.** Orta.

---

### TB-09 🟡 Sayfa içi doğrudan API çağrısı

**Ölçüm.** `Islemler.razor` 733 satır, `Banka.razor` 540 satır. API çağrıları sayfa içinden doğrudan `ApiIstemci` ile, aradaki tiplenmiş servis katmanı olmadan yapılıyor.

**Etki.** Kopyala-yapıştır üretir; `JsonElement` ile yazılan ekran test edilemez.

**Durum.** Yeni ekranlar için servis katmanı şartı PLAN-2 Adım 1'de karara bağlandı. Mevcut sayfalar dokunulmadı.

**Büyüklük.** Orta (kademeli).

---

### TB-10 🟡 Karmaşıklık eşiğini aşan 8 fonksiyon

**Ölçüm** (`lizard`, 2026-09-13)
```
409 fonksiyon · ortalama CCN 3,0 · ortalama 13,4 satır
CCN > 15 olan: 8 fonksiyon (%2)
```

| Fonksiyon | CCN |
|---|---|
| `UrunServisi.Guncelle` | 33 |
| `RaporServisi.PosFaturaMutabakati` | 22 |
| `ApiIstemci` (hata işleme) | 22 |
| `AyarFiyati.Tahmin` | 19 |
| `KiraciYazmaDenetcisi.Denetle` | 18 |
| `MasakKuralMotoru.Dogrula` | 17 |
| `FaturaServisi.Olustur` | 17 |

**Değerlendirme.** Ortalama CCN 3,0 iyi bir rakamdır; %2 oran düşüktür. Listedekilerin bir kısmı (kural motoru, yazma denetçisi) **doğası gereği** dallı — bunlar %98 dal kapsamıyla test edilmiştir, yani karmaşıklık kontrol altındadır. `UrunServisi.Guncelle` (CCN 33) alan-alan PATCH deseninden gelir ve gerçek bir sadeleştirme adayıdır.

**⚠ Araç uyarısı.** `lizard`'ın C# ayrıştırıcısı **sınıf adlarını karıştırıyor** (örn. `ApiIstemci` kodunu `ApiHata::ApiHata` diye raporluyor, bir dosyayı `*global*` sayıyor). CCN rakamları gösterge niteliğindedir; sınıf atamaları güvenilir değildir.

**Büyüklük.** Küçük (1-2 fonksiyon).

---

### TB-11 🟡 MASAK kuralının istemcide ikizlenmesi

**Ölçüm.** `MasakFormu.Eksiksiz()` (satır 130-139), sunucudaki kural şeklini istemcide yeniden yazıyor. Eşikler sunucudan geliyor (`/masak/esikler`) ve sunucu son sözü söylüyor (`MasakKapisi`), ama **kuralın şekli** iki yerde yaşıyor.

**Etki.** Sunucu kuralı değişirse istemci **sessizce sapar** — kullanıcı formu eksik doldurur, sunucu reddeder, sebep görünmez.

**Kapatma.** İstemci `masak/gereksinim` ucuna bağlanacak (PLAN-2 Adım 1'de karara bağlandı).

**Büyüklük.** Küçük.

---

### TB-12 ✅ KAPANDI — Yük ve ölçeklenme davranışı ölçüldü

**Önceki durum.** `grep -rni "k6|artillery|jmeter|loadtest"` → 0 isabet. Performans hakkında tek bir ölçüm yoktu.

**Şimdi.** `dotnet/yuk-testi/k6-senaryo.js` — 10 sanal kullanıcı, 55 sn, POS deseni (ürün listesi + günlük rapor + her 5 turda bir satış).

**Ölçüm (2026-09-14, geliştirme dizüstü + Docker MySQL 8.4):**

| Metrik | Değer |
|---|---|
| Okuma p95 | **25,3 ms** |
| Satış (yazma) p95 | **31,8 ms** |
| Sürdürülen istek | **16,0 istek/sn** |
| **5xx sunucu hatası** | **0** (921 istekte) |
| 429 (oran sınırı) | %4,12 — *tasarlanmış davranış* |

**⚠ Bu rakamlar bu makinenin rakamlarıdır; üretim kapasitesi İDDİA EDİLMEZ.** Ölçümün amacı mutlak kapasite değil, (a) yük altında hata üretilmediğini ve (b) gecikmenin tezgâh hızının çok altında kaldığını göstermektir.

**Bilinçli karar — oran sınırı testte GEVŞETİLMEDİ.** Sınırı kapatıp daha yüksek bir RPS yazmak, üretimde var olmayan bir sayı üretmek olurdu. Senaryo bunun yerine **çok kullanıcılıdır**: her sanal kullanıcı kendi jetonunu taşır, tıpkı bir dükkândaki ayrı personel gibi. Ölçülen %4,12'lik 429 oranı, kullanıcı başına 100 istek/dk sınırının tam da belgelendiği yerde çalıştığını gösterir.

**`http_req_failed` eşiği kullanılmadı** — k6 429'u hata sayar, oysa oran sınırı ürünün bilerek yaptığı iştir. Eşik `sunucu_hatasi_5xx < %0,1` üzerine kuruldu; ölçülen 0'dır.

---

### TB-13 🟠 Runbook yok

**Etki.** Bus factor 1 riskinin operasyonel yüzü. Ortam kurulumu, yayın, veritabanı yönetimi, olay müdahalesi belgelenmemiştir.

**Hafifletici.** Tasarım bilgisi 12 ADR'de ve kod içi gerekçe yorumlarında kayıtlıdır. Eksik olan **işletme** bilgisidir.

**Büyüklük.** Küçük-orta.

---

### TB-14 🟡 MFA yok

**Ölçüm.** `grep -rni "mfa|totp|2fa"` → 0 isabet.

**Etki.** SAHİP hesabı tek parolayla tüm müşteri kimlik verisine erişir.

**Durum.** Mehmet tarafından backlog'a yazılı olarak alındı (PLAN-2 Adım 0 oylaması, koşul 6). Bu tur için şart değildir ama **unutulmuş bir eksik değildir**.

**Büyüklük.** Orta.

---

### TB-15 🟠 Tevsik kapısı "kısım kısım ödeme"yi görmüyor

**Ölçüm.** `TevsikKapisi.GunlukNakitToplam` yalnız **aynı yerel gün** içindeki nakit hareketleri toplar.

**Etki.** 459 s. VUK Genel Tebliği **§4.1.2** ayrı bir kural koyar: *işlem tutarı* haddi aşıyorsa, bedel farklı tarihlerde parçalı ödense bile **her bir** ödeme banka kanalıyla yapılmak zorundadır. Bugün 62.000 TL'lik bir alım üç güne bölünüp günde 20.000 TL nakit ödenirse kapı **hiçbirini durdurmaz** — oysa üçü de ihlaldir.

**Neden şimdi kapanmadı.** Kural, "işlem"in ne olduğuna dair bir kavrama dayanır: sistemde bugün nakit hareketleri var, ama onları tek bir **sözleşme/işlem**e bağlayan bir kimlik yok. Bu bir veri modeli işidir, kapı ayarı değil.

**Büyüklük.** Orta. Bir `islem_grubu_id` alanı ve ödeme planı kavramı gerekir.

---

### TB-16 🟡 Tevsik ihlalinde "5 iş günü" bildirim akışı yok

**Ölçüm.** `grep -rni "kendiliğinden|pişmanlık"` → tevsik bağlamında 0 isabet.

**Etki.** VUK mükerrer md.355/4 son cümlesi: aykırı ödemeyi **ödeyen taraf**, ödemeyi izleyen **beş iş günü** içinde kendiliğinden idareye bildirirse **kendisine ceza kesilmez**. Kapı ihlali normalde engellediği için bu yol nadiren gerekir; ancak kapı `Tevsik:Etkin=false` yapılmışsa veya ihlal sistem dışında gerçekleşmişse işletmenin elinde bir kurtarma yolu vardır ve ürün bunu hatırlatmaz.

**Büyüklük.** Küçük. Bir hatırlatma listesi + süre takibi (gider pusulası 7 gün takibiyle aynı desen).

---

### TB-17 🟡 Fatura satır kalemi modeli yok — taş ibaresi faturaya otomatik basılamıyor

**Ölçüm.** `faturalar` tablosu **başlık düzeyindedir**; satır kalemi tablosu yoktur, açıklama serbest metindir.

**Etki.** Kuyum Ticareti Yönetmeliği md.11/(e) ibarenin **faturada** da yer almasını ister. Ürün kartı, etiket ucu, vitrin listesi ve satış kapısı ibareyi taşır; **fatura taşımaz**. İbare `/urunler/{id}/etiket` ucundan alınıp faturaya elle yazılmak zorundadır.

**Neden şimdi kapanmadı.** Satır kalemi eklemek fatura modelini, KDV matrah hesabını (kuyum KDV'si has bedelini matrah dışı bırakır) ve e-Fatura UBL eşlemesini birlikte ilgilendirir. e-Fatura kalemi (ADR-0012) kurucuya bağlı olduğu için model tasarımı o adımla birlikte yapılmalıdır.

**Büyüklük.** Büyük.

---

### TB-18 🟠 Sayım düzeltmesi deftere yazılmıyor — DEVRALINAN BORÇ

**Kaynak.** Devir denetimi (2026-09-10) T3 kalemi. **Bu tur kapanmadı ve envantere de alınmamıştı** — bu kayıt o eksiği düzeltir.

**Ölçüm.** `UrunServisi.StokSayimGuncelle` stok satırını üzerine yazıyor; gövdesinde `Hareketler.Add` yok:
`sed -n '/StokSayimGuncelle/,/^    }/p' … | grep -c 'Hareketler.Add'` → **0**.
`SAYIM_DUZELTME` hareket türü `Entities.cs:22`'de tanımlı ama **hiçbir yerde kullanılmıyor**.

**Etki.** Append-only defter, stok gerçeğinin tek kaynağı olma özelliğini sayım anında kaybediyor: sayımla düşürülen 3 gram, defterden yeniden türetilemiyor. Fireyi/kaybı kimin ne zaman düştüğü de yazılmıyor (denetim izi yazma dinleyicisi entity değişikliğini yakalar ama iş anlamını değil).

**Neden bu turda kapanmadı.** Bu turun kapsamı PLAN-2 Adım 1–5'ti; sayım o listede yoktu. Kapatmak yeni bir hareket türü akışı + mutabakat testi ister.

**Büyüklük.** Orta.

---

### TB-19 🟡 Bazı raporlar varlık sayısıyla ölçeklenen sorgu yapıyor — DEVRALINAN BORÇ (büyük ölçüde hafifledi)

**Kaynak.** Devir denetimi T5: *"günlük özet, yaşlandırma ve stok değeri raporları filtresiz `ToListAsync()` yapıyor."*

**Bugünkü ölçüm — iddia artık kısmen yanlış.** `RaporModulu.cs`'deki 9 `ToListAsync` çağrısı okundu:
- **Tarihle sınırlı (sorun değil):** satır 44, 96, 165, 200 — hepsi `Tarih >= … && Tarih < …` ile gün/ay aralığına bağlı. Geçmiş biriktikçe büyümezler.
- **Varlık sayısıyla ölçeklenir (kalan borç):** satır 292 (`CariBakiyeler` — cari sayısı kadar), 348 (`Stoklar` — ürün×şube kadar), 53 (`Kasalar` — küçük).

**Etki.** Hareket geçmişiyle değil, **cari/ürün sayısıyla** büyür. 10.000 carili bir işletmede yaşlandırma raporu 10.000 satırı belleğe alır. Bugünkü hedef segment (tek dükkân, 2–3 şube) için ölçülen etki yok: yük testinde günlük rapor p95 **25 ms**.

**Büyüklük.** Düşük — ama kayıtta durmalı, çünkü segment büyürse ilk sızlayacak yer burasıdır.

---

## 3. Bilinçli olarak borç SAYILMAYANLAR

Aşağıdakiler eksik değil, **karara bağlanmış tercihtir**. Bir incelemede "neden yok?" diye sorulabileceği için burada gerekçeleriyle listelenir:

| Görünen eksik | Gerekçe |
|---|---|
| Sunucuda PDF üretimi yok | [ADR-0007](../02-mimari/adr/0007-belge-uretimi-istemcide.md) — yeni bağımlılık ve lisans yükü; tarayıcı yazdırma motoru yeterli |
| 98 adet elle `TenantId` şartı duruyor | [ADR-0010](../02-mimari/adr/0010-elle-tenant-sartlari-korunur.md) — sistem kapsamında filtre kurulmaz, orada tek koruma odur |
| Saklama süresi işi otomatik çalışmıyor | [ADR-0008](../02-mimari/adr/0008-saklama-suresi-maskeleme.md) — kişisel veriyi geri dönülemez değiştiren iş, işletmenin haberi olmadan çalışmamalı |
| KMTS biçim doğrulaması yok | [ADR-0006](../02-mimari/adr/0006-kmts-kapsam-disi.md) — şema yayımlanmadı; bilinmeyen biçim için doğrulama yazmak uydurma olur |
| e-Fatura modülü yok | [ADR-0012](../02-mimari/adr/0012-efatura-sahte-entegrasyonla-yapilmaz.md) — mock ile "tamamlandı" denmez |
| Navigation property yok | [ADR-0009](../02-mimari/adr/0009-veritabani-yabanci-anahtar-yok.md) — FK ile `Include` ayrı kararlardır |
| Testler gerçek MySQL istiyor | [ADR-0011](../02-mimari/adr/0011-testler-gercek-mysql.md) — InMemory transaction'ı, SQLite decimal hassasiyetini yok sayar |

---

## 4. Kapatma sırası

Bağımlılık ve etkiye göre:

1. **TB-01** (sır rotasyonu) — aktif güvenlik açığı
2. **TB-03** (CI) — kapılar hazır, bağlamak küçük iş, sinyali büyük
3. **TB-02** (bin/obj) — TB-01'i çoğaltıyor
4. **TB-13** (runbook) — bus factor
5. **TB-04** (FK) → **TB-05** ve **TB-06** bedavaya kapanır
6. **TB-07**, **TB-08** (test kapsamı)
7. **TB-09**, **TB-11** (PLAN-2 Adım 1 ile birlikte)
8. ~~**TB-12** (yük testi)~~ ✅ kapandı · **TB-14** (MFA), **TB-10** (sadeleştirme)
9. **TB-15** (kısım kısım ödeme) — veri modeli işi; **TB-16** (5 iş günü bildirimi) küçük ve bağımsız
10. **TB-17** (fatura satır kalemi) — e-Fatura kalemiyle (K1) birlikte tasarlanmalı
11. **TB-18** (sayım defteri) — append-only bütünlüğünü ilgilendirir, TB-19'dan önce
12. **TB-19** (rapor ölçeklenmesi) — bugünkü segmentte ölçülen etki yok

---

## 5. Veri borcu — koda değil, KAYDA ait

| # | Tespit | Etki | Kim kapatır |
|---|---|---|---|
| **VB-01** | `kuyumcu_net` içinde **6 taşlı ürünün** `tas_kokeni` alanı **NULL** | Bu ürünler [ADR-0013](../02-mimari/adr/0013-tas-kokeni-fail-closed.md) gereği **satılamaz** | İşletme. `GET /urunler?tasKategori=KOKEN_EKSIK` listeler; köken **tahmin edilmemiştir** |
| **VB-02** | `kuyumcu_net` içinde kiracı sızıntısı kalıntısı: `kasa_hareketleri` 99.999 TL ("Kurban Kasa"), `hareketler` 77.777 TL ("Kurban Musteri") | Faz 0 sızma testimizin artığı; **silinmedi** (Mehmet'in K3 şartı) | Kurucu kararı
