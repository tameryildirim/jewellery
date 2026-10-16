# Teknik Öz-Değerlendirme (Technical Due Diligence Self-Assessment)

| | |
|---|---|
| **Belge kimliği** | KYM-DD-001 |
| **Sürüm / Tarih** | 1.0 · 2026-09-13 |
| **Düzenleyen** | ByGatech A.Ş. |
| **Onay makamı** | Kurucu (Ramazan Akkocan) |
| **Durum** | GÖZDEN GEÇİRİLDİ |

---

## 0. Bu belgenin amacı ve yöntemi

Bir yazılım projesi satın alınırken veya ortaklık görüşmesine girerken **teknik due diligence** yapılır: alıcı tarafın mühendisleri kod tabanını, mimariyi, güvenliği, süreçleri ve ekibi inceler.

Bu belge, o incelemeyi **kendi projemize kendimiz uyguladığımız** rapordur.

**Yöntem:** Black Duck, Sphere Inc., CTO on Demand ve PI Partners'ın kamuya açık teknik due diligence kontrol listeleri tarandı; 14 inceleme başlığı ve kırmızı bayrak listesi çıkarıldı. Her başlık bu projeye karşı **ölçüldü** — görüş bildirilmedi, komut çalıştırıldı.

**Neden kendimiz yazıyoruz:** Bir alıcının bulacağı bulgu, ondan önce bizim tarafımızdan yazılmış olursa zayıflık olmaktan çıkar, olgunluk göstergesine dönüşür. Gizlenmiş bir bulgu ise, bulunduğunda tüm belge setinin güvenilirliğini düşürür.

> **Kırmızı bayrak listesinin ilk maddesi şudur:** *"Hedef şirketin kod tabanına, mimari diyagramlara veya güvenlik denetim raporlarına erişim vermeye isteksizliği."* Bu belge o maddenin tam tersidir.

---

## 1. Özet tablo

| # | Başlık | Durum | Not |
|---|---|---|---|
| 1 | Süreç ve organizasyon olgunluğu | 🟡 Kısmi | Kararlar kayıtlı, CI yok |
| 2 | Mimari ve ölçeklenebilirlik | 🟡 Kısmi | Diyagram var, yük testi yok |
| 3 | Kod kalitesi ve teknik borç | 🟢 İyi | Envanter açık, otomatik denetim var |
| 4 | Test kapsamı ve teslimat hattı | 🟡 Kısmi | **367 test** güçlü, **CI/CD yok** |
| 5 | Güvenlik | 🔴 **Açık var** | **Repo geçmişinde sır**, pentest yok |
| 6 | Uyum / sertifikasyon | 🔴 Yok | Hiçbir sertifika yok |
| 7 | Lisans ve fikrî mülkiyet | 🔴 **Açık var** | **LICENSE yok, IP devri belgesiz** |
| 8 | Bağımlılıklar ve 3. taraf | 🟢 İyi | 18 paket, 0 zafiyet |
| 9 | Ekip ve kilit kişi riski | 🔴 **Açık var** | **Bus factor = 1** |
| 10 | Altyapı ve operasyon | 🔴 Yok | Yayın hattı, izleme, yedek yok |
| 11 | Dokümantasyon | 🟢 İyi | Bu belge seti |
| 12 | Veri ve yapay zekâ maruziyeti | 🟡 Kısmi | AI üretimi kod, kökeni kayıtlı |
| 13 | Yol haritası inandırıcılığı | 🟡 Kısmi | Plan var, süre tahmini yok |
| 14 | Ürün / pazar konumu | 🟡 Kısmi | Araştırma var, pilot yok |

**Genel:** 4 yeşil · 6 sarı · 4 kırmızı.

---

## 2. Başlık başlık bulgular

### 2.1 🔴 Güvenlik — repo geçmişinde sır

**Bulgu.** Sürüm kontrolüne alınmış `appsettings.Development.json` dosyası şunları içeriyor:
- JWT imzalama sırrı (`Jwt:Secret`)
- MySQL root parolası (bağlantı dizesi içinde)
- Alan şifreleme anahtarı (`Sifreleme:Anahtar`)

```bash
git show bbfdc95:dotnet/Kuyumcu.Api/appsettings.Development.json | grep -iE 'secret|password|anahtar'
```

**Bağlam.** Bu sırlar **ilk commit'te** bulunuyor; yani ByGatech'e devredilen kod tabanında zaten mevcuttu. ByGatech tarafından eklenmemiştir. Ancak bu, bulgunun ciddiyetini azaltmaz.

**Neden kritik.** Dosyayı düzeltmek **yetmez**. Git geçmişi sırrı saklamaya devam eder; `git log -p` ile herkes tarafından okunabilir. Depo herhangi bir zamanda paylaşıldıysa (bayi, taşeron, yedek) sırlar ifşa olmuş sayılmalıdır.

**Gereken (iki adım, sırayla):**
1. **Sırları döndür** — JWT sırrı, veritabanı parolası ve şifreleme anahtarı değiştirilmeli. *(Şifreleme anahtarı değişirse mevcut şifreli veri için anahtar rotasyonu gerekir; `sifre_surumu` alanı bu amaçla vardır.)*
2. **Geçmişi temizle** — `git filter-repo` veya BFG ile. Bu işlem **geçmişi yeniden yazar** ve Kurucu kararı gerektirir.

**Durum:** ⛔ **Açık.** Kurucu kalemi.

---

### 2.2 🔴 Lisans ve fikrî mülkiyet

| Ölçüm | Sonuç |
|---|---|
| `LICENSE` / `LICENCE` dosyası | **Yok** |
| Katkı verenlerin IP devir sözleşmesi | Belge yok |
| Bağımlılık lisans envanteri | Bu belgede (§2.8) ilk kez çıkarıldı |

**Neden kritik.** Kırmızı bayrak listesinde doğrudan geçiyor: *"Kod sahipliğinin belirsizliği; taşeron işinde devir sözleşmesi olmaması."* Kod tabanı devralınmıştır ve devrin hukuki belgesi bu sette yoktur.

**Gereken:** Devir sözleşmesi veya IP devir belgesi; ürün lisansının seçilmesi (tescilli/ticari muhtemelen doğru seçim).

**Durum:** ⛔ **Açık.** Kurucu kalemi (hukuki).

---

### 2.3 🔴 Ekip ve kilit kişi riski

```
git shortlog -sn --all
     1  Tamer Yıldırım
```

**Bus factor = 1.** Sürüm kontrolünde tek katkı veren görünüyor.

**Hafifletici etkenler:** Bu belge seti, 12 ADR ve kod içi gerekçe yorumları, bilginin kişiden belgeye taşınması için yazılmıştır. Yeni bir geliştirici `dokumanlar/` ve `02-mimari/adr/` üzerinden tasarım gerekçelerine ulaşabilir.

**Kalan risk:** Operasyonel bilgi (yayın, veritabanı yönetimi, ortam kurulumu) belgelenmemiştir.

**Durum:** ⚠ **Açık.**

---

### 2.4 🔴 Altyapı, operasyon ve teslimat hattı

| Ölçüm | Sonuç |
|---|---|
| CI/CD yapılandırması (`.github/workflows`, `.gitlab-ci.yml`, `Jenkinsfile`) | **Yok** |
| Otomatik yayın (deployment) | Yok |
| İzleme / uyarı (monitoring, alerting) | Yok |
| Yedekleme politikası ve **test edilmiş geri yükleme** | Yok |
| Felaket kurtarma RTO/RPO hedefi | Tanımlı değil |
| Altyapı kodu (IaC) | Yok |

**Bağlam.** Ürün henüz pilot aşamasına gelmemiştir; üretim ortamı yoktur. Bu, eksikliği açıklar ama ortadan kaldırmaz.

**Mevcut güçlü nokta:** `dotnet/denetim.sh` — CI'da çalıştırılmaya hazır bir denetim betiği vardır ve **kendisi mutasyonla sınanmıştır** (bilerek ihlal sokulduğunda dosya ve satır numarasıyla yakaladı, çıkış kodu 1 verdi). Bir CI hattına bağlanması küçük bir iştir.

**Durum:** ⛔ **Açık.**

---

### 2.5 🔴 Uyum ve sertifikasyon

SOC 2, ISO 27001, KVKK VERBİS kaydı — **hiçbiri yok**.

**Not:** Ürünün KVKK **teknik** gereklerini karşılayan mekanizmaları vardır (erişim kaydı, şifreleme, saklama süresi yönetimi, maskeleme). Eksik olan **kurumsal sertifikasyon** ve **aydınlatma metni** gibi hukuki belgelerdir.

**Durum:** ⛔ **Açık.** Kısmen Kurucu kalemi.

---

### 2.6 🟢 Test kapsamı — güçlü yön

```
dotnet test  →  Başarısız: 0, Başarılı: 367, Atlanan: 0, Toplam: 367
./denetim.sh →  DENETİM TEMİZ.
```

| Test türü | Adet | Kapsam |
|---|---|---|
| Servis / iş kuralı | ~330 | MASAK kuralları, has hesabı, muhasebe, mutabakat, şifreleme |
| **HTTP boru hattı** | 17 | Gerçek 401/403/400/200, **429 + Retry-After**, JSON sözleşmesi, kiracı izolasyonu |
| **Bileşen (bUnit)** | 20 | Kademe görünürlüğü, eşik sınırları, rol gizleme |

**Ayırt edici nokta — mutasyon kanıtı.** Bu projede "test yeşil" tek başına kanıt sayılmaz. Koruyucu her mekanizma, **bilerek kırılarak** sınanmıştır:

| Koruma | Kapatıldığında |
|---|---|
| Kiracı sorgu filtresi | 13 testin **11'i kırmızı** |
| Denetim izi append-only | 5 testin **4'ü kırmızı** |
| CI denetim betiği | İhlali dosya+satırla yakaladı, exit 1 |
| Blazor bileşen parametresi | 8 test kırmızı — **derleme 0 hata veriyordu** |

**Bu ayrımın nedeni ölçülmüş bir gerçektir:** Bu projede **4 gerçek hata**, yüzlerce yeşil testten geçtikten sonra ancak canlı sunucuda gerçek veriyle çalışırken ortaya çıktı (bkz. §3).

**Yatırımcı kıyas ölçütü:** Taranan kontrol listelerinde *"kritik akışlarda tipik olarak %70+ coverage aranır"* deniyor. **Satır kapsamı (line coverage) bu projede ölçülmemiştir** — ölçülmediği için bir yüzde verilmeyecektir. Verilebilecek kanıt, mutasyon sonuçlarıdır.

**Durum:** ✅ Güçlü. Eksik: coverage ölçümü, yük testi.

---

### 2.7 🟢 Kod kalitesi ve teknik borç

| Ölçüm | Değer |
|---|---|
| C# satırı (migration hariç) | 16.451 |
| Blazor `.razor` satırı | 5.072 (14 sayfa) |
| API ucu | 100 |
| Modül | 18 klasör (17'si uç içerir) |
| Veri tablosu | 21 · 262 sütun · 54 indeks |
| Otomatik mimari denetimi | `denetim.sh` — 11 kural |

`denetim.sh` şu kuralları her koşumda zorlar: `IgnoreQueryFilters` = 0 · elle `new KuyumcuDbContext` = 1 · `Find/FindAsync` = 0 · `ExecuteUpdate/Delete` beyaz listesi = 4 · `IKiraciVarlik` kapsaması · oran sınırı / HTTPS / sıkıştırma / sağlık ucu / CORS varlığı.

**Teknik borç envanteri** ayrı belgede: [`teknik-borc-envanteri.md`](teknik-borc-envanteri.md).

**Bilinen borçlar (özet):** derleme çıktısı sürüm kontrolünde (**1.408 dosya** `bin/`–`obj/`) · sayfa içi doğrudan API çağrısı deseni (en büyük sayfa 733 satır) · Release derlemesinde WASM boyutu ölçülmedi · UI kapsamı 14 sayfanın 2'siyle sınırlı.

**Durum:** ✅ İyi.

---

### 2.8 🟢 Bağımlılıklar

```
dotnet list package --vulnerable --include-transitive
→ Kuyumcu.Core / Kuyumcu.Api / Kuyumcu.Web / Kuyumcu.Tests: no vulnerable packages
```

**18 doğrudan paket.** Tamamı yaygın kullanımlı ve bakımlı: Microsoft resmî paketleri (ASP.NET Core, EF Core, SignalR), Pomelo MySQL sağlayıcısı, BCrypt.Net-Next, NUlid, Swashbuckle, xunit, bUnit.

**Şeffaflık notu:** 2026-09-13'te yapılan taramada bir zafiyet bulundu — `AngleSharp 1.1.2` (Orta, GHSA-pgww-w46g-26qg, mXSS). Bu paket, **aynı gün** eklenen bUnit 1.34.0'ın geçişli bağımlılığıydı. bUnit 2.11.3'e yükseltilerek **aynı gün** kapatıldı; 367 test yeşil kaldı.

Bu olay belgeye bilerek yazılmıştır: bir zafiyet taramasının değeri, temiz çıktığında değil, bir şey bulduğunda ne yapıldığıyla ölçülür.

**Eksik:** Resmî bir SBOM üretilmiyor; lisans envanteri henüz sistematik değil.

**Durum:** ✅ İyi.

---

### 2.9 🟡 Mimari ve ölçeklenebilirlik

**Var olanlar:**
- C4 modeli: System Context · Container · Component diyagramları
- ER diyagramları: genel + 4 konu alanı, **şemadan üretilen** (elle çizilmiş değil)
- 12 ADR — her kararın bağlamı, gerekçesi ve **olumsuz sonuçları** yazılı
- ISO/IEC/IEEE 42010:2022 uyumlu mimari tanımı

**Ölçülmemiş olanlar — dürüst liste:**

| Soru | Durum |
|---|---|
| 10× yükte önce ne kırılır? | **Ölçülmedi** |
| İndeks performansı gerçek veri hacminde? | **Ölçülmedi** (pilot verisi gerekiyor) |
| Eşzamanlı kullanıcı tavanı? | **Ölçülmedi** |
| Kullanıcı/işlem başına altyapı maliyeti? | **Ölçülmedi** |
| Release derlemesinde WASM boyutu? | **Ölçülmedi** (yalnız Debug: 19,59 → 7,53 MB) |

**Bilinen tekil arıza noktaları:** Tek MySQL örneği (replika yok) · tek uygulama süreci (yatay ölçekleme denenmedi) · kur sağlayıcısı kesintisi (birden çok kaynak tanımlı, devretme davranışı ölçülmedi).

**Açık mimari maddesi:** Veritabanında **enforce edilen yabancı anahtar kısıtı yoktur** (ölçüldü: 0). 39 mantıksal ilişkinin bütünlüğü tamamen uygulama katmanındadır. Bu devralınan bir durumdur; bilinçli bir tasarım tercihi mi yoksa ihmal mi olduğu **doğrulanamamıştır**. Bkz. [ADR-0009](../02-mimari/adr/0009-veritabani-yabanci-anahtar-yok.md) — karar bekleyen açık madde olarak kayıtlıdır.

**Durum:** 🟡 Kısmi.

---

### 2.10 🟡 Veri ve yapay zekâ maruziyeti

Kontrol listelerinde yeni bir başlık: *"AI üretimi kodun kökeni (provenance) kaydı"*.

**Şeffaf beyan:** Bu kod tabanının önemli bir bölümü, ByGatech A.Ş.'nin AI destekli geliştirme modeliyle üretilmiştir. Bu bir risk değil, şirketin çalışma modelidir — ancak inceleme sırasında sorulacağı için burada açıkça belirtilir.

**Kökeni izlenebilir kılan unsurlar:** Her önemli karar ADR olarak kayıtlı · kod içi gerekçe yorumları kararın **neden** alındığını taşıyor · her koruma mekanizması mutasyonla sınanmış (yani kodun çalıştığı kanıtlanmış, yalnızca "yazılmış" değil).

**Müşteri verisi ve AI:** Ürün **hiçbir AI servisine veri göndermez.** Ölçüm:

```bash
grep -rniE 'openai|anthropic|gemini|cognitive|huggingface' --include='*.cs' --include='*.razor' .
→ 0 eşleşme
```

`tenants.ai_etkin` alanı şemada mevcuttur ve bir yönetim ucu bu alanı **yazar** (`AdminModulu.cs:60`), kayıt sırasında `false` atanır. Ancak alanı **okuyup davranış değiştiren hiçbir kod yoktur** — yani bugün işlevsiz bir bayraktır.

**Dışa giden tüm ağ hedefleri (tam liste, ölçüldü):**

| Hedef | Amaç |
|---|---|
| `tcmb.gov.tr/kurlar/today.xml` | TCMB döviz kuru |
| `static.altinkaynak.com/public/Gold` · `/Currency` | Altın ve döviz kuru |
| `finans.truncgil.com/today.json` | Altın/döviz kuru |
| `ankaraaltinborsasi.com` · `api.ankaraaltinborsasi.com/graphql/site` | Altın kuru |

Bu dört sağlayıcı dışında **hiçbir dış çağrı yoktur**. Hiçbirine müşteri verisi gönderilmez; çağrılar tek yönlüdür (kur okuma).

Gelecekte bir AI özelliği eklenirse, veri paylaşımı için ayrı bir ADR ve KVKK değerlendirmesi gerekir.

**Durum:** 🟡 Kısmi — beyan var, resmî politika yok.

---

### 2.11 🟡 Yol haritası ve ürün/pazar

**Var:** İki sıralı plan belgesi, kabul ölçütleri kanıta bağlı · 4 kollu pazar araştırması (19 rakip, doğrulanmış fiyat aralığı, birincil kaynaklı pazar büyüklüğü: 19.905 perakende firma, İSO/TÜİK 2023).

**Yok:** Süre tahmini **bilinçli olarak verilmemiştir** — geçmiş hızdan çarpan uydurmak Anayasa md.1'e aykırı sayılmıştır · pilot müşteri yok · gelir doğrulaması yok.

**Dürüst konum:** Ürün, tek dükkân ve 2-3 şubeli perakende kuyumcu segmenti için yasal uyum tarafında hazırdır. Toptancı, imalatçı, e-Fatura mükellefi ve barkod/RFID isteyen segmentler **bugün kapsam dışıdır**.

**Durum:** 🟡 Kısmi.

---

## 3. Kalite kültürünün kanıtı: canlı testte yakalanan 4 hata

Bu bölüm bir zayıflık değil, bir **yöntem kanıtıdır**. Dördü de yüzlerce yeşil testten geçmişti; yalnızca canlı sunucuda gerçek veriyle ortaya çıktılar.

| # | Hata | Belirti | Kaynak |
|---|---|---|---|
| 1 | `SistemKapsami` çocuk kapsamı `HttpContext` tarafından eziliyordu | Giriş ucu **HTTP 500** | Kendi kodumuz |
| 2 | `MapControllers().RequireRateLimiting` uç niteliklerini eziyordu | **429 hiç dönmüyordu** | Kendi kodumuz |
| 3 | `KasaGuncelle`'de `Math.Abs` | Kasa defteri yön kaybı: bakiye −49.576 ↔ defter +199.576 | **Devralınan kod** |
| 4 | Stopaj kasadan düşülmüyordu | Pusula net 61.042 TL derken kasadan 62.288 TL çıkıyordu | Kendi kodumuz |

**Alınan yapısal ders — ve karşılığı:**
- Hata 1 ve 2 → **HTTP boru hattı testi** kuruldu (17 test). Servis testi middleware sırasını göremez.
- Hata 3 ve 4 → **özdeşlik (invariant) testleri**: bakiye ↔ defter toplamı, belge net ↔ kasa hareketi. Her biri tek başına doğruyken toplamda tutmuyordu.

Bu yüzden bu projede kalite kapısı şunu şart koşar: *"Testler geçti" tek kanıt olarak kabul edilmez; canlı sunucuda gerçek veriyle doğrulama zorunludur.*

---

## 4. Kırmızı bayrak kontrolü — madde madde

Taranan listelerdeki kırmızı bayrakların bu projeye karşı durumu:

| Kırmızı bayrak | Durum |
|---|---|
| Koda/diyagramlara erişim vermeye isteksizlik | ✅ Yok — bu belge seti tersini yapıyor |
| Otomatik test yok | ✅ Yok — 367 test |
| CI/CD yok, manuel deploy | ❌ **VAR** |
| Deploy'u yalnız bir kişi yapabiliyor | ❌ **VAR** (bus factor 1) |
| Yüksek karmaşıklık / sıkı bağlı monolit | 🟡 Ölçülmedi (siklomatik karmaşıklık) |
| 10× büyümede yeniden yazım gerektirir | 🟡 Ölçülmedi |
| Süregelen, planı olmayan rewrite | ✅ Yok |
| "O servisi sadece X anlıyor" | 🟡 Kısmen — ADR'ler ve yorumlar bunu azaltıyor |
| Yol haritası "bu hafta ne gelirse" | ✅ Yok — sıralı, kabul ölçütlü plan |
| Hiç güvenlik testi yapılmamış | ❌ **VAR** — pentest yok |
| Paylaşımlı admin hesapları | ✅ Yok — rol matrisi ve kişisel hesaplar |
| **Repo'da kimlik bilgisi** | ❌ **VAR** — §2.1 |
| Yamalanmamış zafiyetli bağımlılık | ✅ Yok — 0 zafiyet |
| Sertifika eksikliği | ❌ **VAR** |
| Kod sahipliği belirsizliği / IP devri yok | ❌ **VAR** |
| Copyleft lisans ihlali | ✅ Yok — bağımlılıklar izin verici lisanslı |
| Müşteri verisinin AI'ya gitmesi | ✅ Yok — hiçbir AI çağrısı yok |
| Mimari diyagram / runbook yokluğu | 🟡 Diyagram var, **runbook yok** |
| Test edilmemiş yedekler | ❌ **VAR** — yedek politikası yok |

**Sayım: 8 kırmızı bayrak açık, 9'u kapalı, 3'ü ölçülmedi.**

---

## 5. Öncelikli kapatma listesi

Etki ve maliyete göre sıralanmıştır. 1–4 arası, herhangi bir ciddi görüşme öncesinde kapatılmalıdır.

| Sıra | İş | Sahibi | Neden önce |
|---|---|---|---|
| 1 | **Sırları döndür** (JWT, DB parolası, şifreleme anahtarı) | Kurucu | Aktif güvenlik açığı |
| 2 | **Git geçmişini temizle** | Kurucu kararı | Sır geçmişte kalıyor |
| 3 | **LICENSE + IP devir belgesi** | Kurucu (hukuki) | Sahiplik sorusu her görüşmede çıkar |
| 4 | `bin/`–`obj/` sürüm kontrolünden çıkar (**1.408 dosya**) | Geliştirme | "Hangi değişiklik neyi bozdu" sorusunu engelliyor |
| 5 | **CI hattı kur** (`denetim.sh` + `dotnet test` zaten hazır) | Geliştirme | Küçük iş, büyük sinyal |
| 6 | ADR-0009 kararı (FK eklensin mi?) | Mimari | Açık madde olarak duruyor |
| 7 | Yedekleme + **test edilmiş geri yükleme** | Operasyon | Kırmızı bayrak |
| 8 | Satır kapsamı (coverage) ölçümü | QA | Yatırımcı sorar |
| 9 | Yük testi ve 10× senaryosu | Mimari | Pilot verisi gerekiyor |
| 10 | Sızma testi | Güvenlik | Dış hizmet |

---

## 6. Hüküm

**Bu proje, yasal uyum ve test disiplini tarafında güçlü; operasyon ve kurumsal süreç tarafında zayıftır.**

Devralındığında sıfır otomatik testi ve kanıtlanmış 6 kritik hatası olan bir kod tabanı, bugün 367 testli, iki katmanlı kiracı izolasyonu **mutasyonla kanıtlanmış**, kararları belgelenmiş bir ürüne dönüşmüştür.

Buna karşılık CI/CD, izleme, yedekleme, sertifikasyon ve IP belgelemesi **yoktur**. Bunların hiçbiri teknik olarak zor değildir; hiçbiri henüz yapılmamıştır.

**Bir alıcı bu projeye baktığında görmesi gereken şey, kusursuz bir sistem değil, kusurlarını kendi bulup yazan bir ekiptir.**
