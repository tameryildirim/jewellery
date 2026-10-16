# Tehdit Modeli

| | |
|---|---|
| **Belge kimliği** | KYM-TM-001 |
| **Sürüm / Tarih** | 1.0 · 2026-09-13 |
| **Düzenleyen** | ByGatech A.Ş. |
| **Onay makamı** | Mehmet (06 Güvenlik) |
| **Durum** | GÖZDEN GEÇİRİLDİ |
| **Yöntem** | STRIDE + veri akışı analizi |

---

## 1. Korunan varlıklar ve değerleri

| Varlık | Neden değerli | İhlalin bedeli |
|---|---|---|
| **Müşteri kimlik verisi** (TCKN, pasaport, adres, doğum tarihi) | KVKK kapsamında kişisel veri; MASAK gereği 8 yıl saklanır | KVKK idari para cezası + itibar |
| **Kiracı verisi ayrımı** | Her kuyumcu ayrı bir işletme ve rakip olabilir | Ticari felaket; ürün satılamaz hâle gelir |
| **Mali kayıtlar** (kasa, cari, has altın) | İşletmenin defteri | Para kaybı, vergi hatası |
| **MASAK kayıtları** | Hukuki kanıt; append-only | Cezai sorumluluk |
| **Denetim izi** | KVKK md.12 ibraz yükümlülüğü | Denetimde savunmasızlık |
| **Kimlik doğrulama sırları** | Sisteme erişimin anahtarı | Tam ele geçirme |

---

## 2. Tehdit aktörleri

| Aktör | Yetenek | Motivasyon |
|---|---|---|
| **Kötü niyetli kiracı** | Meşru hesap sahibi; API'yi doğrudan çağırabilir | Rakip kuyumcunun müşteri listesi, fiyatları |
| **İçeriden personel** (tezgâhtar) | Sınırlı rolle meşru erişim | Müşteri verisi sızdırma, kayıt gizleme |
| **Dış saldırgan** | Kimlik doğrulaması yok | Kimlik verisi hırsızlığı, fidye |
| **Token hırsızı** | XSS veya cihaz erişimiyle token elde etmiş | Hesap devralma |
| **Dikkatsiz geliştirici** | Kod yazma yetkisi | Kasıtsız sızıntı |

> **Tasarım varsayımı:** En tehlikeli aktör dış saldırgan değil, **kötü niyetli kiracıdır**. Meşru bir hesabı vardır, sistemin iç yapısını bilir ve API'yi doğrudan çağırabilir. Kiracı izolasyonunun iki katmanlı ve mutasyonla kanıtlanmış olması bu varsayımdan gelir.

---

## 3. STRIDE analizi

### S — Spoofing (kimlik sahteciliği)

| Tehdit | Karşı önlem | Kanıt |
|---|---|---|
| Şifre tahmini / kaba kuvvet | Oran sınırı 5/dk (IP+e-posta bölütlü) | Gerçek 429 + `Retry-After` |
| Kullanıcı sayımı (enumeration) | Yanlış şifrede jenerik mesaj | Test |
| Sahte JWT | HMAC-SHA256 imza, `ValidateIssuerSigningKey` | — |
| **Ters vekil arkasında IP sahteciliği** | `KnownProxies` + `ForwardLimit = 1` — yalnız güvenilen vekilden gelen `X-Forwarded-For` kabul edilir | 2026-09-13'te eklendi |
| Çalıntı refresh token | Rotasyon + **yeniden kullanım tespiti**: tüm oturumlar kapanır | Test |
| **KALAN RİSK: MFA yok** | — | [TB-14](../08-degerlendirme/teknik-borc-envanteri.md) |

### T — Tampering (kurcalama)

| Tehdit | Karşı önlem | Kanıt |
|---|---|---|
| **Denetim izini değiştirme/silme** | Append-only, `SaveChanges` seviyesinde zorlanır; okuma ucu var, **silme ucu yok** | Mutasyon: 5 testin 4'ü · canlı DELETE → 405 |
| MASAK kaydını sonradan düzeltme | Append-only; düzeltme yeni kayıtla (`duzeltilen_id`) | — |
| Şifreli alanı kurcalama | AES-**GCM** kimliklendirilmiş şifreleme — kurcalanan metin çözülmez | — |
| **Çapraz kiracı referans enjeksiyonu** | Gövdeden gelen her `*_id` kiracıya karşı doğrulanır | 2026-09-13'te bulundu ve kapatıldı + mutasyon kanıtı |
| SQL enjeksiyonu | Tüm erişim EF parametreli; `FromSqlRaw` = 0 | Ölçüm |
| **KALAN RİSK:** Veritabanı seviyesinde referans bütünlüğü yok | Uygulama katmanı koruyor; `ExecuteUpdate/Delete` ve raw SQL bunu atlar | [ADR-0009](../02-mimari/adr/0009-veritabani-yabanci-anahtar-yok.md) |

### R — Repudiation (inkâr)

| Tehdit | Karşı önlem | Kanıt |
|---|---|---|
| "Ben o kaydı görmedim" | Okuma izi (KVKK md.12) — kim, ne zaman, hangi kayıt | Canlı doğrulama |
| "Ben o değişikliği yapmadım" | Yazma izi otomatik — `SaveChanges` seviyesinde, **unutulamaz** | — |
| "İz sonradan uydurulmuş" | Append-only + IP + user-agent kaydı | Mutasyon kanıtı |
| **KALAN RİSK:** Doğrudan veritabanı erişimi olan biri izi değiştirebilir | Uygulama katmanı korumasının doğal sınırı; veritabanı yetkilendirmesiyle ele alınmalı | Belgelenmiş |

### I — Information Disclosure (bilgi ifşası)

| Tehdit | Karşı önlem | Kanıt |
|---|---|---|
| **Başka kiracının verisini okuma** | Global sorgu filtresi + fail-closed bağlam | Mutasyon: 13 testin 11'i |
| Veritabanı yedeği ele geçerse kimlik verisi | AES-256-GCM alan şifrelemesi | Diskte düz metin TCKN = 0 |
| Listede tam TCKN görünmesi | Maskeleme (son 4 hane); tam değer yalnız tekil çağrıda + iz | — |
| Yanıtta şifre hash'i sızması | Açık alan seçimi | Test |
| Hata mesajında yığın izi | `HataYakalayici` | — |
| XSS ile veri okuma | Blazor varsayılan kodlama + **CSP** (`script-src`'de `unsafe-inline` yok) | 7 test |
| Referrer üzerinden URL sızması | `Referrer-Policy: strict-origin-when-cross-origin` | Test |
| **⛔ EN KRİTİK: Sürüm kontrolünde sır** | — | [TB-01](../08-degerlendirme/teknik-borc-envanteri.md) — **Kurucu kalemi** |

### D — Denial of Service (hizmet engelleme)

| Tehdit | Karşı önlem | Kanıt |
|---|---|---|
| Kaba kuvvet / script istismarı | Oran sınırı: giriş 5/dk, kayıt 3/saat, genel 100/dk | Test |
| **Tek kullanıcının herkesi kilitlemesi** | Bölütleme: giriş IP+e-posta, genel kullanıcı başına | Tasarım |
| **(Düzeltildi)** Ters vekil arkasında tüm kullanıcıların tek kovaya düşmesi | `UseForwardedHeaders` | 2026-09-13 |
| Büyük gövde | Model doğrulaması, `MaxLength` | — |
| **KALAN RİSK:** Tek MySQL örneği, replika yok | — | [TB-12](../08-degerlendirme/teknik-borc-envanteri.md) |
| **KALAN RİSK:** Yük davranışı ölçülmedi | — | TB-12 |

### E — Elevation of Privilege (yetki yükseltme)

| Tehdit | Karşı önlem | Kanıt |
|---|---|---|
| Tezgâhtarın yönetim ucunu çağırması | `RolGerekli` + `ModulYetkisi`; **API son sözü söyler**, ekran gizlemesi tek kapı değildir | Canlı: MÜDÜR → 403 |
| Denetim izine erişim | Yalnız SAHİP, sınıf düzeyinde | Test + canlı |
| Token'da rol değiştirme | İmza doğrulaması | — |
| Kiracı kimliğini değiştirme | `tenantId` claim'i token'da imzalı | — |

---

## 4. Güven sınırları

```
[ İnternet ]
     │  ① TLS
     ▼
[ nginx ters vekil ]  ── güven sınırı 1 ──
     │  ② X-Forwarded-For (YALNIZ güvenilen vekilden kabul edilir)
     ▼
[ ASP.NET Core ]      ── güven sınırı 2: kimlik doğrulama ──
     │  ③ JWT → AktifKullanici (tenantId, rol)
     ▼
[ Yetkilendirme ]     ── güven sınırı 3: rol + modül yetkisi ──
     │  ④
     ▼
[ Kiracı bağlamı ]    ── güven sınırı 4: KİRACI İZOLASYONU ──
     │  ⑤ sorgu filtresi (okuma) + yazma denetçisi (yazma)
     ▼
[ MySQL ]             ── güven sınırı 5: ⚠ BUGÜN ZAYIF ──
        referans bütünlüğü YOK (ADR-0009 ile karara bağlandı)
```

**En kritik sınır 4'tür.** Oradaki ihlal, ürünün varlık sebebini ortadan kaldırır. İki katmanla korunur ve **mutasyonla kanıtlanmıştır**.

**Sınır 5 bugün zayıftır.** Uygulama katmanını atlayan yollar (`ExecuteUpdate/Delete` — **5** beyaz listeli kullanım (P-30b ölçümü 2026-09-23), raw SQL, migration betiği, doğrudan DB erişimi) için veritabanı seviyesinde koruma yoktur.

---

## 5. Doğrulanmış saldırı senaryoları

Aşağıdakiler test edilmiş, sonucu bilinen senaryolardır — varsayım değil.

| # | Senaryo | Sonuç |
|---|---|---|
| 1 | A kiracısı B'nin cari kaydını ister | **404** — kayıt yokmuş gibi |
| 2 | A kiracısı liste çeker | Yalnız kendi kayıtları |
| 3 | Sorgu filtresi devre dışı bırakılırsa | 13 testin **11'i kırmızı** |
| 4 | A, B'nin kimlik tespitini kendi bildirimine bağlar | **`NotFoundException`** — hiçbir kayıt yazılmaz |
| 5 | Saldırgan çalınan refresh token'ı kullanır | **401** + kullanıcının **tüm** oturumları kapanır |
| 6 | 6 ardışık hatalı giriş | **429** + `Retry-After: 60` |
| 7 | Tezgâhtar denetim izini ister | **403** |
| 8 | Denetim kaydı silinmeye çalışılır | **405** (uç yok) · servis seviyesinde **istisna** |
| 9 | Denetim kaydı güncellenmeye çalışılır | `DenetimIziDegistirilemez` — tüm `SaveChanges` düşer |
| 10 | Uydurma TCKN ile hurda alışı | **400** — checksum reddi |
| 11 | Kimliksiz, eşik üstü işlem | **400** — işlem tamamlanmaz |
| 12 | Token'sız API çağrısı | **401** + güvenlik başlıkları yine gönderilir |

---

## 6. Kabul edilen riskler

| Risk | Neden kabul edildi | Şart |
|---|---|---|
| Doğrudan veritabanı erişimi izi değiştirebilir | Uygulama katmanı korumasının doğal sınırı | Veritabanı yetkilendirmesi operasyon sorumluluğu |
| MFA yok | Pilot öncesi; tek kullanıcılı küçük işletme | Backlog'da **yazılı** |
| Sızma testi yapılmadı | Dış hizmet, bütçe kalemi | Kurucu kalemi |
| Yük davranışı bilinmiyor | Pilot verisi yok | Sentetik yük testi planlandı |
| `ExecuteUpdate/Delete` denetçiyi atlar | **5** kullanım (Auth ×4 + Kur ×1), **beyaz listede**, `denetim.sh` yeni eklemeyi kırmızıya düşürür; sayı `OlcumKapisiTestleri` ile gerçek çağrı sayısına kilitli | Sayım dondurulmuş (P-30b, 2026-09-23) |

---

## 7. Açık ve kapatılması gereken

| # | Açık | Sahibi |
|---|---|---|
| 1 | **⛔ Sürüm kontrolündeki sırlar** — rotasyon + geçmiş temizliği | **Kurucu** |
| 2 | Veritabanı referans bütünlüğü (ADR-0009 Dalga 1-4) | Geliştirme |
| 3 | `kuyumcu_net`'teki 2 satır kiracı sızıntısı kalıntısı | Kurucu kararı (yedek sonrası) |
| 4 | Boşta kalma kilidi | PLAN-2 Adım 1 |
| 5 | MFA | Backlog |
| 6 | İzleme/uyarı | TB-03 |
| 7 | Sızma testi | Dış hizmet |
