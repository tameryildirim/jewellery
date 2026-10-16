# DURUM RAPORU — Kuyumcu ERP
**Tarih:** 2026-09-13 · **Hazırlayan:** Tamer Bey ekibi · **Talep:** Kurucu ("en baştan bakalım — ne verdim, ne ekledik, ne eklemedik, neden")

> **Kanıt kuralı (Anayasa md.1):** Bu rapordaki her rakam bu oturumda ölçüldü.
> Ölçüm anı: `git` taban commit `bbfdc95` ("first commit") ↔ çalışan ağaç.
> **Commit atılmadı** (Kurucu kuralı yürürlükte).

---

## 1. PROJEYİ BANA NE OLARAK VERDİN

Taban: `bbfdc95` — .NET 8 + Blazor WASM portu. Çalışıyordu, ama **uyum katmanı sıfırdı ve kiracı izolasyonu kanıtsızdı.**

| Ölçüt | Devraldığım hâl |
|---|---|
| C# dosyası | 34 |
| C# satırı | 6.774 |
| Blazor `.razor` satırı | 4.540 |
| API ucu (`[HttpX]`) | 78 |
| Modül klasörü | 15 (Admin, Auth, Banka, Cari, Emanet, Fatura, Kasa, Kullanici, Kur, Rapor, Satis, Sube, Urun, Yetki) |
| Veri tablosu (`DbSet`) | 16 |
| **Otomatik test** | **0 dosya, 0 test** |
| Migration | **0** (şema elle) |
| MASAK / KVKK / VUK | **yok** |

**Devraldığım hâlde kanıtlanmış 6 kritik hata vardı** (`DENETIM-RAPORU-2026-09-10.md`): kiracı sızıntısı (başka kuyumcunun kasasına 99.999 TL yazıldı, HTTP 200 döndü), çifte/üçlü kayıt, has altın işaret hatası.

---

## 2. BUGÜNKÜ HÂL — ölçüm

| Ölçüt | Devraldığım | Bugün | Fark |
|---|---|---|---|
| C# dosyası | 34 | **73** | +39 |
| C# satırı | 6.774 | **15.238** | **+8.464 (%125)** |
| API ucu | 78 | **98** | +20 |
| Modül | 15 | **18** (+Hurda, +Masak, +Pano) | +3 |
| Tablo | 16 | **21** | +5 |
| **Test** | **0** | **321 — hepsi yeşil** | +321 |
| Migration | 0 | **9** | +9 |
| CI denetimi | yok | **`denetim.sh` — TEMİZ** | yeni |

**Bu oturumdaki koşum çıktısı:**
```
Başarılı!  - Başarısız: 0, Başarılı: 321, Atlanan: 0, Toplam: 321, Süre: 3 s
DENETİM TEMİZ.
```

---

## 3. NE GİBİ KABİLİYETLER EKLEDİK

### 3.1 Güvenlik / veri izolasyonu
- **Global kiracı sorgu filtresi** — artık sorguda `TenantId` yazmak unutulsa bile başka kuyumcunun verisi görünmez.
- **Yazma denetçisi** — filtre yalnız okumayı korur; yazma tarafına ayrı `SaveChangesInterceptor` kondu.
- **Fail-closed tasarım** — bağlamsız `DbContext` doğrudan patlar; işaretlenmemiş bir varlık uygulamayı **açılmaktan** alıkoyar.
- **Mutasyon kanıtı** — filtre bilerek kapatıldı, 13 testin **11'i kırmızıya döndü**; geri alındı, yeşil. (Yeşil test tek başına bir şey kanıtlamaz.)
- **Oran sınırı** — giriş 5/dk, kayıt 3/saat, genel 100/dk. Gerçek **429 + `Retry-After: 60`** alındı.
- **HTTPS yönlendirme, br+gzip sıkıştırma, `/saglik` ucu, CORS beyaz listesi** (liste boşsa hiçbir çapraz origin yok).
- **Alan bazlı şifreleme** — AES-256-GCM + aranabilirlik için HMAC-SHA256 hash sütunu. Diskte TCKN düz metin **0**.
- **Denetim izi** — kim, ne zaman, neyi okudu/yazdı (KVKK md.12).

### 3.2 MASAK uyumu *(rakiplerde bulunmayan kısım)*
- 3 yeni tablo: kimlik tespiti, beyan, şüpheli işlem bildirimi. **Append-only** (8 yıl saklama).
- **Kural motoru** — saf fonksiyon, I/O yok: 185.000 TL kimlik eşiği, beyan kademeleri, ŞİB için **10 iş günü** sayacı. 36 test, **tam sınır** değerleriyle (184.999 / 185.000 / 200.000 / 2.000.000 / 2.000.000,01 …).
- **Eşikler yapılandırmadan okunur** — rakam değişince kod değişmez.
- **7 işlem akışına kapı** — eşik aşıldığında kimlik yoksa işlem **kapanmaz** (uyarıp geçmez).
- **Resmî tatil takvimi** — sabit 7 tatil kodda; dinî bayramlar yapılandırmadan (hicri takvim kayar, tahmin edilmez), girilmemiş yıl sessiz geçmez.
- **Saklama süresi işi** — silme değil **maskeleme**: kimlik alanları maskelenir, tutar/tarih/kademe korunur (MASAK 8 yıl ister ↔ KVKK ölçülülük ister).

### 3.3 Vergi / muhasebe
- **Gider pusulası (VUK md.234)** — hurda alışında zorunlu kimlik, TCKN checksum doğrulaması, **tutar yazıyla**, 7 günlük düzenleme süresi takibi, gecikme listesi.
- **Stopaj (GVK md.94)** — satıcı tipine göre kesinti; **kasadan net tutar çıkar** (canlı testte yakalanan hata).
- **Günlük POS–fatura mutabakatı** — kasıtlı uyumsuz veride fark yakalandı: 3 satış 75.000 ↔ 1 fatura 40.000 → fark 35.000 + 2 uyarı + faturasız işlem listesi.
- **KMTS opsiyonel kod alanı** — ürün ve harekette.

### 3.4 Altyapı
- 9 migration (şema artık kodda, elle değil) · 14 kiracı indeksi
- `denetim.sh` — CI denetimi; **kendisi mutasyonla sınandı** (bilerek `IgnoreQueryFilters` sokuldu → dosya+satırla yakaladı, exit 1)
- Blazor ilk yükleme: **19,59 MB → 7,53 MB (%61,5)** — üstelik bu Debug derlemesi, yani tavan değil

### 3.5 Canlı testte yakalanan 4 gerçek hata *(hepsi yeşil testten geçmişti)*
| Hata | Sonuç |
|---|---|
| `SistemKapsami` çocuk kapsamı `HttpContext` tarafından eziliyordu | Giriş HTTP 500 veriyordu → düzeltildi |
| `MapControllers().RequireRateLimiting` uç niteliklerini eziyordu | 429 hiç dönmüyordu → düzeltildi |
| `KasaGuncelle`'de `Math.Abs` (**devraldığım kodda vardı**) | Defter yönü kayboldu: bakiye −49.576 ↔ defter +199.576 → düzeltildi |
| Stopaj kasadan düşülmüyordu (**benim hatam**) | Pusula net 61.042 derken kasadan 62.288 çıkıyordu → düzeltildi |

---

## 4. PLAN — tik tik durum

`PLAN-YASAL-ZORUNLULUKLAR-2026-09-11.md`

| # | Adım | Durum |
|---|---|---|
| 0 | KMTS blokaj mı? | ✅ Bakanlık PDF'inde "kuyumcu" 0, "perakende" 0 kez → yükümlü rafineri/Darphane. Plandan çıktı |
| 1 | Global kiracı sorgu filtresi | ✅ |
| 1b | Elle `TenantId` şartlarının temizliği | ❌ **bilinçli yapılmadı** (§5) |
| 2 | Oran sınırı + taşıma güvenliği | ✅ |
| 3 | MASAK veri modeli | ✅ |
| 4 | MASAK kural motoru | ✅ |
| A | MASAK → işlem akışı *(oyla eklendi)* | ✅ |
| 5 | e-Fatura / e-Arşiv | 🔒 **başlanamadı — K1** (§5) |
| 6 | Hurda kimlik + gider pusulası | ✅ |
| 7 | POS–fatura mutabakatı | ✅ |
| 8 | KMTS opsiyonel alan | ✅ |
| 9 | Demo engelleri (boş grafik, yanıltıcı kart) | ✅ |

**9 adımın 8'i + oyla eklenen A bitti. Tek kilitli adım: 5.**

Planda yazmayıp sonradan kapatılanlar: CORS beyaz listesi ✅ · `denetim.sh` ✅ · pusula belgesi + tutar yazıyla ✅ · 7 gün süre takibi ✅ · resmî tatil takvimi ✅ · saklama maskelemesi ✅ · TCKN maskeleme + okuma izi ✅

---

## 5. NE EKLEMEDİK — ve NEDEN

| Eklenmedi | Neden |
|---|---|
| **e-Fatura / e-Arşiv (Adım 5)** | **K1 yok** — özel entegratör sözleşmesi. Kabul ölçütü "entegratörün test ortamına **gerçek** fatura"; mock ile "oldu" denmez. Ayrıca fatura haddi için 4 çelişen rakam var (12.000 / 36.000 / 3M / 5M) → **K2** |
| **MASAK'a gerçek ŞİB gönderimi** | MASAK'ın **API'si yok, test sunucusu yok** — yalnız MASAK Online'a XML yükleme. **XSD kamuya açık değil (K6)**. Kayıt e-Devlet + noter işi (**K5**) |
| **Adım 1b — 98 elle `TenantId` şartının temizliği** | Sayım 68 değil **98** çıktı. Bertan'ın "iki bağımsız katman" gerekçesi **yanlıştı** (ikisi de aynı JWT claim'ini okuyor). Yine de yapılmamalı — sebebi başka: **sistem kapsamında filtre hiç kurulmuyor**, orada elle şart tek koruma. Ölçülmüş kazanç yok, regresyon riski var. `ADIM1-KARAR` EK'inde mühürlendi |
| **Stopaj vergi borcu muhasebesi** | Borç hesabı gerekiyor; oran **K2**'ye bağlı — **para hareketini** etkiler, tahmin edilemez |
| **KMTS biçim doğrulaması** | Şema yayınlanmadı. Alan var, biçim uydurulmadı |
| **KVKK aydınlatma metni** | Hukuki metin — mekanizma hazır, metni kurucu/danışman verir |
| **Saklama işinin zamanlayıcısı** | Kişisel veriyi geri dönülemez değiştiren iş, kuyumcunun haberi olmadan kendi kendine çalışmamalı. Önce önizleme, sonra onay |
| **UI otomatik testleri** | bUnit altyapı kararı alınmadı |
| **İndeks performans ölçümü** | Pilot veri hacmi gerekiyor (**K4**) |
| **Pano Faz A'nın kalanı** | Plan §3: bilinçli beklemede — ZarifSoft'ta zaten var, fark yaratmıyor + Mehmet'in yayın vetosu. Yazılan 34 test yerinde, çürümüyor |
| **Barkod/RFID, toptan, personel/prim, masraf, imalat, tamirat, sertifika, mobil** | Plan §3: yasal zorunluluk değil → kapsam dışı. **Satılabilirlik için eksik** ama bu planın konusu değil |

---

## 6. KURUCU KALEMLERİ — bende yok

| # | İş | Neyi kilitliyor |
|---|---|---|
| **K1** | e-Fatura özel entegratör sözleşmesi | **Adım 5 tamamen** |
| **K2** | Mali müşavir: fatura haddi çelişkisi + **hurda stopaj oranı** | Adım 5, stopaj muhasebesi |
| **K3** | 2-3 rakip demosu (M&L, ZarifSoft, Kuyumcum) | MASAK farklılaşma iddiasının doğrulanması |
| **K4** | Pilot kuyumcu erişimi | Kabul testleri, indeks ölçümü |
| **K5** | Pilotun MASAK Online kaydı (e-Devlet, Taahhüt Formu, noter) | Gerçek ŞİB gönderimi |
| **K6** | ŞİB XML şeması (XSD) | XML üretimi |

---

## 7. DÜRÜST HÜKÜM

**Uyum tarafı bitti; ürün tarafı bitmedi.** Devraldığım kod 6 kanıtlı hatayla, sıfır testle geliyordu; bugün 321 test, CI denetimi ve iki katmanlı kiracı izolasyonu var — ve izolasyonun **çalıştığı mutasyonla kanıtlandı**.

Ama `SATILABILIRLIK-DEGERLENDIRMESI-2026-09-11.md`'deki hüküm **hâlâ geçerli**: pazarda 18 rakip var, proje ~%40 tamamlanmış. Eksik olan yasal katman değil — **barkod, toptan, personel, imalat, mobil**. Bunlar plan §3'te bilinçli olarak dışarıda bırakıldı.

**K1 ve K2 gelmeden plan içinde yapılacak iş kalmadı.** Bundan sonrası ya senin teyitlerin, ya da planın dışına çıkma kararı.
