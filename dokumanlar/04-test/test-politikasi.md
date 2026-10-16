# Test Politikası (Test Policy)

| | |
|---|---|
| **Belge kimliği** | KYM-TP-001 |
| **Sürüm / Tarih** | 1.0 · 2026-09-13 |
| **Düzenleyen** | ByGatech A.Ş. |
| **Onay makamı** | Kurucu |
| **Durum** | GÖZDEN GEÇİRİLDİ |
| **Dayanak** | **ISO/IEC/IEEE 29119-3:2021** §6 — Organizational test documentation |

> ⚠ Bu belge **IEEE 829-2008**'e dayanmaz. O standart **Superseded** durumdadır ve yerine ISO/IEC/IEEE 29119 serisi geçmiştir.

---

## 1. Amaç ve kapsam

Bu politika, ByGatech A.Ş.'nin Kuyumcu ERP ürününde testin **neden**, **nasıl** ve **hangi kanıtla** yapıldığını tanımlar. Ürünün tüm bileşenlerini kapsar: API, Blazor istemci, veri katmanı, yasal uyum kuralları.

---

## 2. Temel ilke: yeşil test kanıt değildir

Bu politikanın ayırt edici maddesi budur ve **ölçülmüş bir gerçeğe** dayanır:

> Bu projede **4 gerçek hata**, yüzlerce yeşil testten geçtikten sonra ancak canlı sunucuda gerçek veriyle çalışırken ortaya çıktı.

| # | Hata | Belirti |
|---|---|---|
| 1 | `SistemKapsami` çocuk kapsamı `HttpContext` tarafından eziliyordu | Giriş ucu HTTP **500** |
| 2 | `MapControllers().RequireRateLimiting` uç niteliklerini eziyordu | **429 hiç dönmüyordu** |
| 3 | `KasaGuncelle`'de `Math.Abs` | Kasa bakiyesi −49.576 ↔ defter +199.576 |
| 4 | Stopaj kasadan düşülmüyordu | Pusula net 61.042 TL, kasadan çıkan 62.288 TL |

Buna ek olarak, Blazor'da **eşleşmeyen bileşen parametresi** `dotnet build` tarafından **0 hata** ile geçirildi; hata yalnız çalışma anında ortaya çıkıyordu.

**Sonuç:** "Testler geçti" cümlesi bu projede tek başına bir tamamlanma kanıtı olarak **kabul edilmez**.

---

## 3. Kanıt hiyerarşisi

Bir işin "bitti" sayılması için gereken kanıtlar, güçten zayıfa:

| # | Kanıt türü | Neyi kanıtlar |
|---|---|---|
| 1 | **Mutasyon kanıtı** | Korumanın gerçekten koruduğunu. Koruma kapatıldığında test **kırmızıya dönmeli** |
| 2 | **Canlı doğrulama** | Gerçek sunucuda, gerçek veriyle çalıştığını |
| 3 | **HTTP entegrasyon testi** | Middleware sırası, auth, oran sınırı, JSON sözleşmesi |
| 4 | **Özdeşlik (invariant) testi** | Modüller arası tutarlılık (bakiye ↔ defter, belge ↔ kasa) |
| 5 | **Servis / birim testi** | İş kuralının kendisini |
| 6 | Derleme başarısı | Neredeyse hiçbir şeyi |

**Kırmızıya dönmeyen test, test değildir.** Kapsam sayısını şişiren ölü koddur.

---

## 4. Test seviyeleri ve araçlar

| Seviye | Araç | Kapsam | Karar |
|---|---|---|---|
| Birim / servis | xunit | İş kuralları, hesaplar, yasal eşikler | — |
| Veri katmanı | xunit + **gerçek MySQL 8.4** | Filtreler, interceptor'lar, şifreleme | [ADR-0011](../02-mimari/adr/0011-testler-gercek-mysql.md) |
| HTTP boru hattı | `Microsoft.AspNetCore.Mvc.Testing` | Middleware, auth, oran sınırı, sözleşme | PLAN-2 Adım 0a |
| Bileşen (UI) | **bUnit** | Render, koşullu görünürlük, rol gizleme | PLAN-2 Adım 0b |
| Uçtan uca (tarayıcı) | Playwright — **yalnız 3 duman senaryosu** | Giriş→pano, bir satış, bir MASAK bildirimi | Aşkın: 3'ün ötesine genişletme **reddedildi** |
| Yük | k6 | Başarım | ⬜ planlandı |

### Neden InMemory veya SQLite değil

- **EF InMemory transaction desteklemez.** Para ve has altın hareketleri transaction içinde yürür; desteklenmeyen bir transaction sessizce yok sayılırsa testler **sahte güven** verir.
- **SQLite decimal hassasiyetini uygulamaz.** Has gram hesabı ondalık hassasiyete bağlıdır; SQLite'ta doğru görünen bir test MySQL'de yanlış sonuç verebilir.
- Test şeması **migration ile** kurulur, `EnsureCreated` ile değil — aksi hâlde test şeması üretimden sapar.

---

## 5. Zorunlu kalite kapıları

Her adım kapanışında, **istisnasız**:

| # | Kapı |
|---|---|
| **K1** | `dotnet test` → 0 başarısız · `./denetim.sh` → TEMİZ. Çıktı **yapıştırılmış**, özet değil |
| **K2** | Yeni her API ucu HTTP entegrasyon testiyle gelir: yetkisiz=401, yanlış rol=403, yanlış kiracı=404/boş, geçersiz gövde=400, geçerli=200 |
| **K3** | Her yeni uç için "B kiracısı A'nın kaydını göremez/yazamaz" testi — **istisnasız** |
| **K4** | Para/has dokunan her adımda **özdeşlik testi**: bakiye ↔ defter toplamı, has borç ↔ has alacak, belge net ↔ kasa hareketi — **işaret dahil** |
| **K5** | Yeni her kural için **mutasyon kanıtı**: kuralı boz → en az 1 test kırmızı, **isimleriyle raporla** |
| **K6** | Yeni her ekran için bUnit smoke; hesap gösteriyorsa biçimleme/yuvarlama testi |
| **K7** | Adım kapanışında **canlı sunucuda elle doğrulama çıktısı** |
| **K8** | Güvenlik vetosu ayrıdır; QA kapısından geçmek **yayın izni değildir** |

---

## 6. Geçirilmeyenler

1. "Testler yeşil" cümlesinin **tek kanıt** olarak sunulduğu hiçbir adım
2. Canlı sunucuda **bir kez bile** koşturulmamış hiçbir özellik
3. **Mutasyon kanıtı olmayan** hiçbir yeni koruma
4. Para/has dokunup **özdeşlik testi getirmeyen** hiçbir adım
5. **Kiracı izolasyon testi olmayan** hiçbir yeni uç
6. **Dolu veritabanına göç denenmeden** yapılacak hiçbir yayın
7. `SET foreign_key_checks=0` ile eklenen hiçbir kısıt
8. **Tek kullanıcılı testle** "yarış koşulu yok" iddiası
9. **Ölçülmemiş** performans rakamı
10. **Komut çıktısı yapıştırılmamış** hiçbir "bitti" raporu

---

## 7. Test verisi ve gizlilik

- Testler `kuyumcu_test` veritabanında çalışır. `TestVeritabani`, `kuyumcu` veya `kuyumcu_net`'e bağlanmayı **kod düzeyinde reddeder**.
- Gerçek müşteri verisi test ortamına **kopyalanmaz**.
- Test TCKN'leri checksum algoritmasından geçen **sentetik** numaralardır (ör. `10000000146`).
- Test şifreleme anahtarı sabittir ve **yalnız testte** kullanılır.

---

## 8. Sorumluluk

| Rol | Sorumluluk |
|---|---|
| **Aşkın** (04 QA) | Kalite kapısı — fail-closed. Kapıdan geçmeyen sevk edilmez |
| **Mehmet** (06 Güvenlik) | Güvenlik testleri ve yayın vetosu |
| Geliştirici | Kendi değişikliğinin K1–K7 kanıtını üretmek |
| **Tamer Bey** | Kapı ihlallerinin mühürlenmesi |
