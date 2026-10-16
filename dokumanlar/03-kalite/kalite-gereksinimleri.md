# Kalite Gereksinimleri

| | |
|---|---|
| **Belge kimliği** | KYM-KG-001 |
| **Sürüm / Tarih** | 1.0 · 2026-09-13 |
| **Düzenleyen** | ByGatech A.Ş. |
| **Onay makamı** | Kurucu |
| **Durum** | GÖZDEN GEÇİRİLDİ |
| **Dayanak** | **ISO/IEC 25010:2023** — SQuaRE, Product quality model |

---

## 0. Neden 2023 sürümü

ISO/IEC 25010 **Kasım 2023'te revize edildi** ve 2011 sürümünü iptal etti. Değişiklikler yüzeysel değildir:

| 2011 | 2023 |
|---|---|
| 8 karakteristik | **9 karakteristik** |
| — | **Safety** (yeni) |
| Usability | **Interaction capability** |
| Portability | **Flexibility** |
| Maturity (alt) | **Faultlessness** |
| User interface aesthetics (alt) | **User engagement** |
| Accessibility (alt) | **inclusivity** + **user assistance** olarak ikiye ayrıldı |

Başlık da değişti: *"System and software quality models"* → *"**Product** quality model"*.

2011 sürümüne dayanan bir kalite belgesi bugün **eskimiş** sayılır. Bu belge 2023'ün 9 karakteristiğini kullanır.

> **Kapsam notu (Anayasa md.1):** Alt karakteristiklerin **tam listesi** standardın ücretli metnindedir ve doğrulanamamıştır. Bu belgede yalnızca resmî önsözden doğrulanabilen değişiklikler ve karakteristik adları kullanılmıştır. Doğrulanmamış alt karakteristik adı **uydurulmamıştır**.

---

## 1. Ölçüm ilkesi

Her kalite gereksinimi için üç şey yazılır: **hedef**, **ölçüm yöntemi (çalıştırılabilir komut)**, **bugünkü değer**.

Ölçülemeyen bir hedef, hedef değildir. Ölçülmemiş bir alan "ölçülmedi" diye yazılır; tahmini değer verilmez.

---

## 2. Karakteristik bazında gereksinimler

### 2.1 Functional suitability — İşlevsel uygunluk

| Gereksinim | Hedef | Ölçüm | Bugün |
|---|---|---|---|
| **KG-F1** Yasal eşik doğruluğu | MASAK eşiklerinde **tam sınır** davranışı doğru | `MasakKuralTestleri` — 184.999,99 / 185.000 / 199.999,99 / 2.000.000 / 2.000.000,01 / 20.000.000 / 20.000.000,01, negatif tutarlar dahil | ✅ 36 test, dal kapsamı %98,1 |
| **KG-F2** Has altın hesabı | `(gram − taş) × milyem / 1000` ondalık hassasiyetle | `HasHesapTestleri` | ✅ %100 satır, %100 dal |
| **KG-F3** Kuyum KDV özel matrahı | Has altın bedeli matrah dışı | `RaporFaturaTestleri` | ✅ |
| **KG-F4** Muhasebe özdeşliği | Kasa bakiyesi = kasa defteri toplamı (**işaret dahil**) | `MuhasebeTestleri` | ✅ Regresyon testi var — bu bir kez kırıldı (`Math.Abs`) |
| **KG-F5** Belge net tutarı = kasa çıkışı | Gider pusulasındaki net, kasadan çıkanla aynı | `GiderPusulasiTestleri` | ✅ Regresyon testi var — bu da bir kez kırıldı (stopaj) |

---

### 2.2 Performance efficiency — Başarım verimliliği

| Gereksinim | Hedef | Ölçüm | Bugün |
|---|---|---|---|
| **KG-P1** İlk yükleme boyutu | ≤ 3 MB (sıkıştırılmış, ilk açılış) | `dotnet publish -c Release` + `blazor.boot.json`'daki kaynakların `.br` toplamı | ✅ **2,42 MB** |
| **KG-P2** Kiracı sorgusu indeks kullanımı | `tenant_id` öncü sütun | `information_schema.STATISTICS` | ✅ 54 indeks; `IX_*_tenant_id_*` deseni |
| **KG-P3** p95 yanıt süresi | **Hedef konmadı** | k6 yük testi | ⬜ **ÖLÇÜLMEDİ** |
| **KG-P4** 10× yükte ilk kırılan | **Belirlenmedi** | yük testi | ⬜ **ÖLÇÜLMEDİ** |

> **KG-P1 için ölçüm uyarısı.** Yaygın hata `du -sh publish/wwwroot` ile tüm klasörü toplamaktır; bu, aynı içeriğin üç kopyasını (ham + `.br` + `.gz`) sayar ve gerçek indirme boyutunun ~2,5 katını verir. Doğru yol `blazor.boot.json`'da listelenen kaynakların `.br` boyutlarını toplamaktır. **Debug derlemesiyle ölçüm geçersizdir** — IL trimming yalnız Release'de uygulanır (Debug: 201 assembly, Release: 52).
>
> Bu projede daha önce **7,53 MB** rakamı raporlanmıştı; o ölçüm Debug derlemesine dayanıyordu ve **yanlıştı**. Gerçek rakam 2,42 MB'dir.

**Bilinen tekil arıza noktaları:** tek MySQL örneği (replika yok) · tek uygulama süreci · kur sağlayıcısı kesintisi (çoklu kaynak tanımlı, devretme ölçülmedi).

---

### 2.3 Compatibility — Uyumluluk

| Gereksinim | Hedef | Ölçüm | Bugün |
|---|---|---|---|
| **KG-C1** JSON sözleşmesi | HTTP yanıtları camelCase | `HttpBoruHattiTestleri.Yanit_camelCase_alan_adlari_kullanir` | ✅ |
| **KG-C2** Hata sözleşmesi | `{ message, error, statusCode }` — doğrulama hatasında `message` **dizi** | HTTP testleri | ✅ |
| **KG-C3** Kur sağlayıcı çeşitliliği | ≥ 2 bağımsız kaynak | `KurKaynakDefteri` | ✅ 4 kaynak (TCMB, Altınkaynak, Truncgil, Ankara Altın Borsası) |
| **KG-C4** e-Fatura (GİB) uyumu | UBL-TR | — | 🔒 **K1 bekliyor** (ADR-0012) |

---

### 2.4 Interaction capability — Etkileşim yeteneği
*(2011'deki "Usability" bu adla değiştirildi)*

| Gereksinim | Hedef | Ölçüm | Bugün |
|---|---|---|---|
| **KG-I1** Çok dillilik | tr + en, sözlük tabanlı | `Kuyumcu.Core/I18n/Sozlukler.cs` | ✅ |
| **KG-I2** Rol bazlı görünürlük | Yetkisiz kullanıcı ekranı **görmez** | bUnit + HTTP 403 testleri | ✅ |
| **KG-I3** Yasal alanların kademeli açılması | Tutar eşiğine göre alan açılır | `MasakFormuEkranTestleri` — sınır testleriyle | ✅ 12 test |
| **KG-I4** Erişilebilirlik (WCAG) | **Hedef konmadı** | — | ⬜ **ÖLÇÜLMEDİ** |

---

### 2.5 Reliability — Güvenilirlik
*(alt karakteristik "Maturity" → **"Faultlessness"** oldu)*

| Gereksinim | Hedef | Ölçüm | Bugün |
|---|---|---|---|
| **KG-R1** Test başarı oranı | %100 | `dotnet test` | ✅ **385/385** |
| **KG-R2** Satır kapsamı | ≥ %70 (yatırımcı kıyas ölçütü) | `coverlet` | ✅ **%72,4** (Api %80,9 · Core %100 · Web %7,2) |
| **KG-R3** Dal kapsamı | ≥ %60 | `coverlet` | ❌ **%29,5** — açık (TB-08) |
| **KG-R4** Korumaların mutasyon kanıtı | Her koruma kapatıldığında test **kırmızıya dönmeli** | Elle mutasyon | ✅ 4 koruma kanıtlandı |
| **KG-R5** Yedekten geri yükleme | Sağlama toplamları birebir | `yedek-al-ve-dogrula.sh` | ✅ **22/22 tablo** |
| **KG-R6** Referans bütünlüğü | Yetim kayıt = 0 | `yetim-tara.sh` | ✅ yetim 0 · ⛔ **kiracı sızıntısı 2** (kalıntı, ADR-0009) |

**Mutasyon kanıtı tablosu** — bu projede "test yeşil" tek başına kanıt sayılmaz:

| Koruma | Kapatıldığında |
|---|---|
| Kiracı sorgu filtresi | 13 testin **11'i** kırmızı |
| Denetim izi append-only | 5 testin **4'ü** kırmızı |
| MASAK kimlik tespiti doğrulaması | 4 testin **2'si** kırmızı |
| Blazor bileşen parametresi | 8 test kırmızı — **derleme 0 hata veriyordu** |
| CI denetim betiği | İhlali dosya+satırla yakaladı, çıkış kodu 1 |

---

### 2.6 Security — Güvenlik

| Gereksinim | Hedef | Ölçüm | Bugün |
|---|---|---|---|
| **KG-S1** Kiracı izolasyonu | Başka kiracının verisi **görünmez ve yazılamaz** | Mutasyon + HTTP testleri | ✅ iki katman |
| **KG-S2** Kimlik verisi şifreleme | TCKN/belge no diskte düz metin **0** | Ham SQL taraması | ✅ AES-256-GCM + HMAC |
| **KG-S3** Şifre saklama | Düz metin **yok**, BCrypt | `AuthTestleri` | ✅ |
| **KG-S4** Refresh token saklama | Veritabanında **hash**, düz metin yok | `AuthTestleri` | ✅ SHA-256 |
| **KG-S5** Çalıntı token tespiti | İptal edilmiş token yeniden kullanılırsa **tüm oturumlar kapanır** | `AuthTestleri` | ✅ |
| **KG-S6** Oran sınırı | Giriş 5/dk · kayıt 3/saat · genel 100/dk | HTTP testi — gerçek **429 + Retry-After** | ✅ |
| **KG-S7** Gerçek istemci IP'si | Ters vekil arkasında doğru IP | `UseForwardedHeaders` + güvenilir vekil listesi | ✅ (2026-09-13'te düzeltildi) |
| **KG-S8** Zafiyetli bağımlılık | **0** | `dotnet list package --vulnerable` | ✅ 0 |
| **KG-S9** Lisans uyumu | Copyleft **yok** | `nuget-license -a "MIT;Apache-2.0;..." -err` | ✅ 128 paket, 0 copyleft |
| **KG-S10** Denetim izi | Kişisel veri erişimi kayıtlı, **değiştirilemez** | Mutasyon testi | ✅ |
| **KG-S11** Sızma testi | — | dış hizmet | ⬜ **YAPILMADI** |
| **KG-S12** Çok etkenli kimlik doğrulama | — | — | ⬜ **YOK** (TB-14) |

---

### 2.7 Maintainability — Sürdürülebilirlik

| Gereksinim | Hedef | Ölçüm | Bugün |
|---|---|---|---|
| **KG-M1** Ortalama siklomatik karmaşıklık | ≤ 5 | `lizard -l csharp` | ✅ **3,0** |
| **KG-M2** Karmaşıklık eşiğini aşan oran | ≤ %5 | `lizard --CCN 15` | ✅ **%2** (409 fonksiyondan 8'i) |
| **KG-M3** Mimari kuralların otomatik denetimi | İhlal → derleme kırmızı | `denetim.sh` — 11 kural | ✅ TEMİZ |
| **KG-M4** Mimari kararların kaydı | Her önemli karar ADR | `02-mimari/adr/` | ✅ 12 ADR |
| **KG-M5** Diyagramların güncelliği | Şemadan **üretilebilir** | `er-uret.py` | ✅ |
| **KG-M6** Ortalama fonksiyon uzunluğu | ≤ 20 satır | `lizard` | ✅ **13,4** |

---

### 2.8 Flexibility — Esneklik
*(2011'deki "Portability" bu adla değiştirildi; "scalability" alt karakteristik olarak eklendi)*

| Gereksinim | Hedef | Ölçüm | Bugün |
|---|---|---|---|
| **KG-E1** Mevzuat rakamı değişince **kod değişmez** | Tüm eşikler yapılandırmadan | `appsettings: Masak / Hurda / Takvim` | ✅ (ADR-0005) |
| **KG-E2** Dinî bayram tarihleri | Yapılandırmadan; girilmemiş yıl **sessiz geçilmez** | `ResmiTatiller.DiniBayramEksik()` | ✅ |
| **KG-E3** Kur sağlayıcısı eklenebilirliği | Adaptör deseni | `KurKaynakDefteri` | ✅ |
| **KG-E4** Yatay ölçekleme | — | — | ⬜ **DENENMEDİ** |
| **KG-E5** Veri dışa aktarımı (taşınabilirlik) | Müşteri verisini dışarı alabilmeli | — | ⬜ **YOK** — PLAN-2 Adım 4 |

---

### 2.9 Safety — Emniyet
*(**2023'te eklenen yeni karakteristik**)*

Bu karakteristik genelde can/mal güvenliği bağlamında düşünülür. Bu üründe karşılığı **mali ve hukuki emniyettir**: yanlış bir işlem para kaybına veya idari para cezasına yol açar.

| Gereksinim | Hedef | Ölçüm | Bugün |
|---|---|---|---|
| **KG-G1** Fail-closed açılış | Şifreleme anahtarı veya JWT sırrı yoksa **uygulama açılmaz** | `Program.cs` — `Build()` öncesi | ✅ |
| **KG-G2** Fail-closed kiracı bağlamı | Bağlamsız `DbContext` yapıcıda patlar | Ctor | ✅ |
| **KG-G3** Fail-closed varlık kapsaması | `IKiraciVarlik` uygulamayan varlık → **uygulama açılmaz** | Model doğrulaması | ✅ |
| **KG-G4** Yasal eşikte işlem **durur** | Kimlik eksikse işlem tamamlanamaz — uyarıp geçmez | `MasakKapisi` · canlı 400 | ✅ |
| **KG-G5** Belgesiz hurda alımı | Kimliksiz alış **reddedilir** | `GiderPusulasiKapisi` · canlı 400 | ✅ |
| **KG-G6** Süre aşımı uyarısı | ŞİB 10 iş günü · pusula 7 gün | Sayaç + gecikme listesi | ✅ |
| **KG-G7** Saklama süresi | Silme değil **maskeleme**; elle onay | `SaklamaSuresiIsi` | ✅ (ADR-0008) |
| **KG-G8** Nakit tevsik sınırı (30.000 TL) | Kümülatif kontrol, fail-closed | — | ⬜ **YOK** — PLAN-2 Adım 2 |

---

## 3. Özet tablo

| # | Karakteristik | Karşılanan | Açık | Ölçülmemiş |
|---|---|---|---|---|
| 1 | Functional suitability | 5 | 0 | 0 |
| 2 | Performance efficiency | 2 | 0 | **2** |
| 3 | Compatibility | 3 | 1 (K1) | 0 |
| 4 | Interaction capability | 3 | 0 | **1** |
| 5 | Reliability | 4 | **2** | 0 |
| 6 | Security | 10 | 0 | **2** |
| 7 | Maintainability | 6 | 0 | 0 |
| 8 | Flexibility | 3 | 1 | **1** |
| 9 | Safety | 7 | 1 | 0 |
| | **Toplam** | **43** | **5** | **6** |

**Okunuşu:** Güvenlik, sürdürülebilirlik, işlevsel uygunluk ve emniyet tarafında ölçülmüş ve karşılanmış durumdayız. Açıklar başarım (yük ölçülmedi), güvenilirlik (dal kapsamı) ve esneklik (dışa aktarım) alanlarında toplanıyor.

**Hiçbir satıra "muhtemelen karşılanıyor" yazılmamıştır.** Ölçülmeyen 6 kalem "ÖLÇÜLMEDİ" olarak durur ve kapatma planı [teknik borç envanterindedir](../08-degerlendirme/teknik-borc-envanteri.md).

---

## 4. Ölçümleri yeniden çalıştırma

```bash
# Testler ve kapsam
cd dotnet
dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --collect:"XPlat Code Coverage"

# Mimari kural denetimi
./denetim.sh

# Referans bütünlüğü
./yetim-tara.sh

# Yedek + geri yükleme doğrulaması
./yedek-al-ve-dogrula.sh

# Karmaşıklık
python3 -m lizard -l csharp Kuyumcu.Api Kuyumcu.Core Kuyumcu.Web \
    --exclude "*/bin/*" --exclude "*/obj/*" --exclude "*Migrations*"

# Bağımlılık zafiyeti ve lisans
dotnet list package --vulnerable --include-transitive
nuget-license -i Kuyumcu.sln -t -mapping ../dokumanlar/05-guvenlik/lisans-url-eslemesi.json \
    -a "MIT;Apache-2.0;BSD-3-Clause;MS-PL;BSD-2-Clause" -err

# İlk yükleme boyutu
dotnet publish Kuyumcu.Web/Kuyumcu.Web.csproj -c Release -o /tmp/yayin
# → /tmp/yayin/wwwroot/_framework/blazor.boot.json'daki kaynakların .br toplamı
```
