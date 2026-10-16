# Test Planı (Test Plan)

| | |
|---|---|
| **Belge kimliği** | KYM-TPL-001 |
| **Sürüm / Tarih** | 1.0 · 2026-09-13 |
| **Düzenleyen** | ByGatech A.Ş. |
| **Onay makamı** | Aşkın (04 QA) |
| **Durum** | GÖZDEN GEÇİRİLDİ |
| **Dayanak** | **ISO/IEC/IEEE 29119-3:2021** §7 — Test management documentation |

---

## 1. Kapsam

**Test edilen:** Kuyumcu ERP — API (17 modül, 100 uç), Blazor istemci (14 sayfa), veri katmanı (21 tablo), yasal uyum kuralları (MASAK, VUK, KVKK).

**Kapsam dışı — ve gerekçeleri:**

| Kapsam dışı | Gerekçe |
|---|---|
| e-Fatura entegrasyonu | Entegratör test ortamı yok; mock ile test **anlamsız** ([ADR-0012](../02-mimari/adr/0012-efatura-sahte-entegrasyonla-yapilmaz.md)) |
| MASAK'a gerçek ŞİB gönderimi | MASAK'ın API'si ve test sunucusu **yok**; XSD yayımlanmamış |
| Gerçek veri hacminde başarım | Pilot verisi yok; sentetik yük testi planlandı |
| Tarayıcılar arası uyumluluk | Hedef konmadı |
| Erişilebilirlik (WCAG) | Hedef konmadı |

---

## 2. Test öğeleri ve risk önceliği

Risk = (hata olasılığı) × (hatanın bedeli). Bu üründe bedel **para kaybı** veya **idari para cezası**dır.

| Öncelik | Alan | Bedel | Mevcut kapsam |
|---|---|---|---|
| **1** | Kiracı izolasyonu | Başka işletmenin verisine erişim — ticari ve hukuki felaket | iki katman + mutasyon kanıtı |
| **1** | Para ve has hesabı | Yanlış bakiye, tutmayan defter | özdeşlik testleri |
| **1** | MASAK eşikleri | İdari para cezası | 27 test, **tam sınır** değerleriyle, dal %98,1 |
| **2** | Kimlik doğrulama | Hesap ele geçirme | 14 HTTP testi, `AuthServisi` %98 |
| **2** | Gider pusulası + stopaj | Vergi hatası | 23 test |
| **2** | Denetim izi | KVKK ihlali | append-only, mutasyon kanıtı |
| **3** | Raporlar, mutabakat | Yanlış karar | 19 test |
| **4** | Kullanıcı arayüzü | Kullanılabilirlik | 19 bUnit testi (14 sayfadan 2'si) |

---

## 3. Test tasarımı

### 3.1 Sınır değer analizi — yasal eşiklerde zorunlu

Mevzuat eşiklerinde "yaklaşık doğru" diye bir şey yoktur. Her eşik **tam sınırında** sınanır:

```
184.999,99 → kademe YOK        185.000,00 → kimlik ZORUNLU
199.999,99 → yalnız kimlik     200.000,00 → amaç beyanı
2.000.000,00 → amaç kademesi   2.000.000,01 → beyan formu
20.000.000,00 → beyan formu    20.000.000,01 → + ek belge
```
**Negatif tutarlar da kapsanır** — iade/ters kayıt yollarında eşik mantığı bozulmamalıdır.

### 3.2 Özdeşlik (invariant) testleri

Modül sınırında duran test, modül sınırını aşan hatayı göremez. Bu projede iki hata tam olarak böyle kaçtı.

| Özdeşlik | Neden |
|---|---|
| Kasa bakiyesi **=** kasa defteri toplamı (işaret dahil) | `Math.Abs` hatası bunu kırmıştı |
| Gider pusulası net tutarı **=** kasadan çıkan | Stopaj hatası bunu kırmıştı |
| Has borç **=** has alacak | Paralel defter tutarlılığı |

### 3.3 Mutasyon testi

Her koruma mekanizması **bilerek kırılarak** sınanır. Kırmızıya dönmeyen test sayılmaz.

### 3.4 Negatif ve saldırı testleri

- Başka kiracının kaydını okuma/yazma denemesi
- Çapraz kiracı referans enjeksiyonu (gövdede başka kiracının kimliği)
- Çalıntı refresh token yeniden kullanımı
- Rol yükseltme denemesi
- Oran sınırı aşımı
- Uydurma TCKN (checksum)
- Denetim izini değiştirme/silme denemesi

---

## 4. Test ortamı

| Bileşen | Yapılandırma |
|---|---|
| Veritabanı | MySQL 8.4 (Docker, `kuyumcu-mysql`), şema `kuyumcu_test` |
| Şema kurulumu | `Database.Migrate()` — **`EnsureCreated` değil** |
| Boru hattı | Üretimle **aynı** interceptor'lar: kiracı yazma denetçisi, denetim izi, MASAK hash |
| HTTP | `WebApplicationFactory<Program>` + TestServer |
| Ters vekil | `IStartupFilter` ile nginx taklidi (`RemoteIpAddress` = loopback) |
| Şifreleme | Sabit test anahtarı — gerçek AES-256-GCM çalışır |
| Kur zamanlayıcısı | **Kapalı** — dış ağa çıkmaz |

**Koruma:** `TestVeritabani`, `kuyumcu` veya `kuyumcu_net`'e bağlanmayı kod düzeyinde reddeder.

---

## 5. Giriş ve çıkış ölçütleri

**Test koşumuna giriş:** Kod derleniyor · MySQL kabı ayakta · migration güncel.

**Çıkış (kabul) ölçütleri:**
1. `dotnet test` → **0 başarısız**
2. `./denetim.sh` → **TEMİZ**
3. Yeni korumalar için **mutasyon kanıtı** raporlanmış
4. Para/has dokunan değişikliklerde **özdeşlik testi** yeşil
5. **Canlı doğrulama** çıktısı rapora eklenmiş
6. `dotnet list package --vulnerable` → **0**
7. Lisans kapısı → çıkış kodu 0

---

## 6. Askıya alma ve yeniden başlama

**Askıya alma:** Kiracı izolasyon testlerinden biri kırmızıysa · `denetim.sh` ihlal bulduysa · `yetim-tara.sh` **kiracı sızıntısı** raporladıysa (bu bir güvenlik olayıdır).

**Yeniden başlama:** Kök neden bulunup düzeltildiğinde ve **mutasyon kanıtı** üretildiğinde.

---

## 7. Bilinen kırılganlıklar

| Kırılganlık | Gerekçe / azaltma |
|---|---|
| Testler paylaşımlı MySQL şeması kullanır | Şema kurulumu süreç başına **bir kez** (`SemayiBirKezKur`). Bu bir kez kırıldı: ikinci fikstür `EnsureDeleted` ile 132 testi düşürdü |
| Oran sınırı testleri IP bölütüne duyarlı | Her test kendi `X-Forwarded-For` IP'siyle gelir (RFC 5737 aralığı). Oran sınırı testte **gevşetilmez** — sınanan davranış odur |
| Kayıt ucu saatte 3 istekle sınırlı | Kayıt **yalnız bir kez** sınanır; diğer testler kullanıcıyı doğrudan veritabanına yazar |

---

## 8. Zamanlama ve süre

**Süre tahmini verilmemiştir.** Geçmiş hızdan çarpan uydurmak Anayasa md.1'e aykırı sayılır. Test koşum süresi ölçülmüştür: **385 test ≈ 5 saniye**.
