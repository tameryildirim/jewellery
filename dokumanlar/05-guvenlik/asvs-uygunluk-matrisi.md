# OWASP ASVS 5.0.0 Uygunluk Matrisi

| | |
|---|---|
| **Belge kimliği** | KYM-ASVS-001 |
| **Sürüm / Tarih** | 1.0 · 2026-09-13 |
| **Düzenleyen** | ByGatech A.Ş. |
| **Onay makamı** | Mehmet (06 Güvenlik) |
| **Durum** | GÖZDEN GEÇİRİLDİ |
| **Dayanak** | **OWASP ASVS 5.0.0** (30 Mayıs 2025) — CC BY-SA 4.0 |

---

## 0. Kapsam ve dürüstlük beyanı

Bu matris, ASVS 5.0.0'ın **17 bölümüne** karşı ürünün durumunu gösterir.

**Bu bir sertifikasyon değildir.** ASVS uygunluğu, bağımsız bir denetçinin her kontrol maddesini tek tek doğrulamasıyla iddia edilir. Burada yapılan **öz-değerlendirmedir**: her satır bizim ölçümümüze dayanır ve ölçüm yöntemi yazılıdır.

**Sızma testi yapılmamıştır.** Bu matris onun yerini tutmaz.

| İşaret | Anlamı |
|---|---|
| ✅ | Karşılanıyor, ölçümle kanıtlı |
| 🟡 | Kısmen karşılanıyor |
| ❌ | Karşılanmıyor |
| ➖ | Bu ürün için geçerli değil (gerekçesiyle) |

---

## 1. Bölüm bazında durum

### V1 — Encoding and Sanitization

| Durum | Kanıt |
|---|---|
| ✅ | **SQL enjeksiyonu:** Tüm veri erişimi EF Core üzerinden parametreli. `denetim.sh` ham SQL kullanımını denetliyor; `FromSqlRaw` kod tabanında yok. |
| ✅ | **Çıktı kodlaması:** Blazor varsayılan olarak HTML kodlar. `MarkupString` **8 yerde kullanılıyor** — hepsi `AlanGrafik.razor` ve `CubukGrafik.razor` içinde SVG üretimi için. Kullanıcı verisi olan tek alan (`Etiket`) `System.Net.WebUtility.HtmlEncode`'dan geçiyor; sayısal değerler `F1` + InvariantCulture ile biçimlendiriliyor. **XSS yolu yok** — ama bu, gözden kaçmaması gereken bir desendir: yeni bir `MarkupString` kullanımı eklenirse kodlama elle sağlanmalıdır. |
| 🟡 | **Girdi temizleme:** Serbest metin alanları (MASAK gerekçe, açıklama) uzunluk sınırlı ama içerik filtrelemesi yok. Depolanan veri yalnız kendi kiracısına gösterilir. |

### V2 — Validation and Business Logic

| Durum | Kanıt |
|---|---|
| ✅ | **İş kuralı doğrulaması sunucuda.** İstemci doğrulaması yalnız kullanıcıyı yönlendirir; `MasakKapisi` ve `GiderPusulasiKapisi` son sözü söyler. |
| ✅ | **Fail-closed:** Yasal eşik aşıldığında kimlik yoksa işlem **tamamlanamaz** — uyarıp geçmez. Canlı doğrulama: HTTP 400. |
| ✅ | **Sınır değer doğrulaması:** 27 test, eşiklerin tam sınırında, negatif tutarlar dahil. |
| ✅ | **TCKN checksum doğrulaması** — uydurma numara reddedilir. |
| ✅ | **Model doğrulaması:** `[Required]`, `[MaxLength]`, `[Range]`, `[EmailAddress]`; hata biçimi sözleşmeli (`{message[], error, statusCode}`). |

### V3 — Web Frontend Security

| Durum | Kanıt |
|---|---|
| ✅ | **CORS beyaz liste.** Liste boşsa **hiçbir** çapraz origin'e izin verilmez (fail-closed). Eskiden `AllowAnyOrigin` idi. |
| ✅ | **CSRF:** JWT `Authorization` başlığında taşınır, çerezde değil → klasik CSRF yüzeyi yok. |
| ✅ | **Content Security Policy gönderiliyor** (2026-09-13'te eklendi): `default-src 'self'` · `object-src 'none'` · `frame-ancestors 'none'` · `base-uri 'self'` · `form-action 'self'`. Blazor WASM için `'wasm-unsafe-eval'` zorunludur; **`script-src`'ye `'unsafe-inline'` VERİLMEZ** ve bu testle bekçileniyor. |
| ✅ | **Güvenlik başlıkları gönderiliyor:** `X-Content-Type-Options: nosniff` · `X-Frame-Options: DENY` · `Referrer-Policy: strict-origin-when-cross-origin` · `Permissions-Policy` (kamera/mikrofon/konum/ödeme kapalı). 7 testle bekçileniyor; 401 yanıtlarında da gönderildiği ayrıca sınanıyor. |

### V4 — API and Web Service

| Durum | Kanıt |
|---|---|
| ✅ | **Oran sınırı:** giriş 5/dk (IP+e-posta), yenileme 10/dk, kayıt 3/saat, genel 100/dk. Gerçek **429 + `Retry-After`** HTTP testiyle kanıtlı. |
| ✅ | **Gerçek istemci IP'si:** `UseForwardedHeaders` + güvenilir vekil listesi. Körü körüne `X-Forwarded-For` güvenilmiyor (`ForwardLimit = 1`, `KnownProxies`). |
| ✅ | **Tutarlı hata sözleşmesi** — bilgi sızdırmayan mesajlar. |
| ✅ | **HTTP sözleşmesi testli:** 401/403/400/200/429 gerçek istekle doğrulanmış. |
| 🟡 | **API sürümleme:** `/api/v1` var; sürüm geçiş politikası yazılı değil. |

### V5 — File Handling

| Durum | Kanıt |
|---|---|
| ➖ | **Dosya yükleme ucu YOK.** Ölçüldü: kod tabanında `IFormFile` kullanımı bulunmuyor. Bu saldırı yüzeyi mevcut değil. |

### V6 — Authentication

| Durum | Kanıt |
|---|---|
| ✅ | **Şifre saklama:** BCrypt. Testle kanıtlı: düz metin yok, `$2` imzası doğrulanıyor. |
| ✅ | **Kullanıcı sayımı engeli:** Yanlış şifrede mesaj "E-posta veya şifre hatalı" — hangi alanın yanlış olduğu sızdırılmaz. |
| ✅ | **Kaba kuvvet koruması:** giriş 5/dk, IP+e-posta bölütlü. |
| ✅ | **Pasif kullanıcı giriş yapamaz** (testli). |
| ❌ | **Çok etkenli kimlik doğrulama (MFA) YOK** — [TB-14](../08-degerlendirme/teknik-borc-envanteri.md). |
| 🟡 | **Şifre politikası:** 8-72 karakter. Sözlük/ihlal listesi kontrolü yok. |

### V7 — Session Management

| Durum | Kanıt |
|---|---|
| ✅ | **Access token kısa ömürlü:** 15 dakika. |
| ✅ | **Refresh token rotasyonu:** Her yenilemede eski **iptal edilir** (testli). |
| ✅ | **Çalıntı token tespiti:** İptal edilmiş token yeniden kullanılırsa kullanıcının **TÜM** tokenleri iptal edilir. Testle kanıtlı — meşru token da geçersizleşir. |
| ✅ | **Refresh token veritabanında hash'li** (SHA-256), düz metin yok (testli). |
| ✅ | **Çıkış token'ı geçersiz kılar** (testli). |
| ❌ | **Boşta kalma kilidi (idle lock) YOK** — Mehmet'in koşulu, PLAN-2 Adım 1. |

### V8 — Authorization

| Durum | Kanıt |
|---|---|
| ✅ | **Rol matrisi:** SAHİP / MÜDÜR / MUHASEBE / TEZGÂHTAR. `RolGerekli` + `ModulYetkisi`. |
| ✅ | **Kiracı izolasyonu — iki katman:** okuma (global sorgu filtresi) + yazma (`SaveChangesInterceptor`). Mutasyonla kanıtlı: filtre kapatılınca 13 testin 11'i kırmızı. |
| ✅ | **Fail-closed:** Bağlamsız `DbContext` patlar; işaretlenmemiş varlık uygulamayı **açtırmaz**. |
| ✅ | **Denetim izi yalnız SAHİP** — canlı: MÜDÜR → **403**. |
| ✅ | **Çapraz kiracı referans enjeksiyonu kapatıldı** (2026-09-13) + mutasyon kanıtı. |
| 🟡 | **Nesne düzeyi yetkilendirme:** 36 `*_id` ilişkisinin **tamamı** için "uygulama kapısı var mı" denetimi henüz çıkarılmadı (Mehmet'in K4'ü). Veritabanı seviyesi koruma [ADR-0009](../02-mimari/adr/0009-veritabani-yabanci-anahtar-yok.md) ile karara bağlandı. |

### V9 — Self-contained Tokens

| Durum | Kanıt |
|---|---|
| ✅ | **HMAC-SHA256 imza**, `ValidateIssuerSigningKey = true`, `ValidateLifetime = true`, `ClockSkew = 30 sn`. |
| ✅ | **Sır yoksa uygulama açılmaz** (fail-closed). |
| ✅ | **Token'da hassas veri yok:** yalnız `sub`, `tenantId`, `subeId`, `rol`. |
| ❌ | **Sır sürüm kontrolünde** — [TB-01](../08-degerlendirme/teknik-borc-envanteri.md). **En kritik açık.** |
| 🟡 | **Anahtar rotasyonu:** Alan şifrelemesi için `sifre_surumu` var; JWT sırrı için rotasyon mekanizması yok. |

### V10 — OAuth and OIDC

| Durum | Kanıt |
|---|---|
| ➖ | **Kullanılmıyor.** Kimlik doğrulama kendi JWT'siyle yapılır; dış kimlik sağlayıcı yok. |

### V11 — Cryptography

| Durum | Kanıt |
|---|---|
| ✅ | **AES-256-GCM** alan şifrelemesi — kimliklendirilmiş şifreleme (bütünlük dahil). |
| ✅ | **Aranabilirlik HMAC-SHA256 ile** — deterministik hash, tek yönlü. |
| ✅ | **Diskte düz metin TCKN = 0** (ham SQL ile doğrulandı). |
| ✅ | **Rastgelelik:** `RandomNumberGenerator` (kriptografik). |
| ✅ | **Anahtar 32 bayt doğrulanıyor**, yoksa uygulama açılmaz. |
| 🟡 | **Anahtar yönetimi:** ortam değişkeni / yapılandırma. HSM veya anahtar kasası yok. |

### V12 — Secure Communication

| Durum | Kanıt |
|---|---|
| ✅ | **HTTPS yönlendirme** (üretimde). |
| ✅ | **Veritabanı bağlantısı yerel**, dışa açık değil. |
| ✅ | **HSTS** üretimde etkin (`UseHsts`, 2026-09-13'te eklendi). Geliştirmede bilinçli olarak kapalı — localhost'u kalıcı HTTPS'e kilitlemek tarayıcıda zor temizlenen bir duruma yol açar. |
| 🟡 | **TLS sürümü ve şifre takımları** ters vekilde yapılandırılır; bu belgede doğrulanmadı. |

### V13 — Configuration

| Durum | Kanıt |
|---|---|
| ✅ | **Fail-closed yapılandırma:** JWT sırrı, şifreleme anahtarı, bağlantı dizesi yoksa **uygulama açılmaz**. |
| ✅ | **Mevzuat eşikleri yapılandırmadan** — kod değişmeden güncellenir. |
| ✅ | **Swagger yalnız geliştirmede.** |
| ✅ | **Bağımlılık zafiyeti: 0** (`dotnet list package --vulnerable`). |
| ✅ | **Lisans kapısı:** 128 paket, 0 copyleft, fail-closed kontrol. |
| ✅ | **SBOM üretiliyor:** CycloneDX 1.7, 128 bileşen. |
| ❌ | **Sırlar sürüm kontrolünde** — TB-01. |
| ❌ | **Derleme çıktısı sürüm kontrolünde** (1.408 dosya) — TB-02. |

### V14 — Data Protection

| Durum | Kanıt |
|---|---|
| ✅ | **Kişisel veri şifreli** (AES-256-GCM). |
| ✅ | **Listede maskeleme:** TCKN son 4 hane; tam değer yalnız tekil görüntülemede. |
| ✅ | **Okuma izi (KVKK md.12):** Hassas kayıt görüntülemeleri kaydedilir. |
| ✅ | **Saklama süresi yönetimi:** Silme değil maskeleme; tutar/tarih/kademe korunur. |
| ✅ | **Yanıtta şifre/hash sızıntısı yok** (testli). |
| ✅ | **Denetim izinde DEĞER yazılmaz**, yalnız alan adı — kişisel veri ikinci kez çoğaltılmaz. |
| 🟡 | **Veri dışa aktarımı yok** — taşınabilirlik hakkı henüz karşılanmıyor (PLAN-2 Adım 4). |

### V15 — Secure Coding and Architecture

| Durum | Kanıt |
|---|---|
| ✅ | **Mimari kurallar otomatik denetleniyor:** `denetim.sh` — 11 kural, kendisi mutasyonla sınanmış. |
| ✅ | **Tek kaçış yolu:** `IgnoreQueryFilters` yasak (0), yerine grep'lenebilir `SistemKapsami`. |
| ✅ | **Kararlar kayıtlı:** 12 ADR. |
| ✅ | **Ortalama siklomatik karmaşıklık 3,0**; eşiği aşan %2. |
| ❌ | **Veritabanı referans bütünlüğü yok** — ADR-0009 ile karara bağlandı, uygulanmadı. |

### V16 — Security Logging and Error Handling

| Durum | Kanıt |
|---|---|
| ✅ | **Denetim izi append-only** — kod tarafından zorlanıyor, mutasyonla kanıtlı. Silme ucu yok (canlı: **405**). |
| ✅ | **Kiracı ihlali girişimleri kaydediliyor** (`REDDEDILDI` eylemi). |
| ✅ | **Denetim izi okunabiliyor** — SAHİP'e özel uç ve ekran (KVKK md.11 talebine cevap verilebilir). |
| ✅ | **Hata mesajları yığın izi sızdırmıyor** (`HataYakalayici`). |
| 🟡 | **İzleme/uyarı (monitoring/alerting) yok** — [TB-03](../08-degerlendirme/teknik-borc-envanteri.md). |

### V17 — WebRTC

| Durum | Kanıt |
|---|---|
| ➖ | **Kullanılmıyor.** |

---

## 2. Özet

| Durum | Bölüm sayısı |
|---|---|
| ✅ Ağırlıklı karşılanıyor | **12** (V1, V2, **V3**, V4, V6*, V7*, V8, V11, **V12**, V14, V15*, V16) |
| 🟡 Kısmi | **1** (V9) |
| ❌ Belirgin açık | **1** (V13) |
| ➖ Geçerli değil | **3** (V5, V10, V17) |

\* Bölüm içinde en az bir ❌ maddesi var.

### Kapatılması gereken açıklar, öncelik sırasıyla

| # | Açık | Bölüm | Sahibi |
|---|---|---|---|
| 1 | **Sürüm kontrolündeki sırlar** (JWT, DB parolası, şifreleme anahtarı) | V9, V13 | **Kurucu** |
| 2 | Derleme çıktısının sürüm kontrolünden çıkarılması | V13 | Kurucu kararı |
| 3 | Boşta kalma kilidi | V7 | PLAN-2 Adım 1 |
| 4 | Veritabanı referans bütünlüğü | V15 | ADR-0009 |
| 5 | MFA | V6 | Backlog |
| 6 | Sızma testi | tümü | Dış hizmet |

### Bu değerlendirmede kapatılanlar (2026-09-13)

| Açık | Durum |
|---|---|
| Güvenlik başlıkları yok (V3) | ✅ Kapatıldı — 5 başlık + 7 bekçi testi |
| CSP yok (V3) | ✅ Kapatıldı — `script-src`'de `unsafe-inline` yok, testle bekçili |
| HSTS yok (V12) | ✅ Kapatıldı (üretimde) |
| Ters vekil arkasında gerçek IP (V4) | ✅ Kapatıldı — `UseForwardedHeaders` + güvenilir vekil listesi |
| Çapraz kiracı referans enjeksiyonu (V8) | ✅ Kapatıldı + mutasyon kanıtı |

Bu beş madde, öz-değerlendirmenin **kendi bulduğu ve aynı gün kapattığı** açıklardır.
