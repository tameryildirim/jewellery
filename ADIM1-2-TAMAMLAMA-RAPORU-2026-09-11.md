# Adım 1 + 2 — Tamamlama Raporu

**Tarih:** 2026-09-11 · **Kapsam:** `PLAN-YASAL-ZORUNLULUKLAR-2026-09-11.md` Adım 1 ve 2, `ADIM1-KARAR-2026-09-11.md` mührüne göre.
**Commit atılmadı** (Kurucu kuralı). **Bütün sayılar ölçüldü, hiçbiri tahmin değil.**

---

## 1. KABUL ÖLÇÜTLERİ — kanıt tablosu

| # | Ölçüt | Sonuç | Kanıt |
|---|---|---|---|
| K1 | Tüm testler yeşil | ✅ | **137/137** başarılı (124 eski + 13 yeni), 0 başarısız |
| K2 | `HasQueryFilter` tüm kiracı varlıklarında | ✅ | 15 varlık `IKiraciVarlik` uyguluyor; filtre döngüyle hepsine kuruluyor |
| K3 | M1 — yabancı kasa çıplak sorguyla görünmez | ✅ | 4 test; mutasyonda kırmızı |
| K4 | M2 — yabancı kiracıya `Add()` reddedilir | ✅ | 2 test; mutasyonda kırmızı |
| K5 | M3 — kiracı taşıma reddedilir | ✅ | 2 test; mutasyonda kırmızı |
| K6 | Faz 0 açığı (99.999 / 77.777 TL) servis atlanarak da engellenir | ✅ | 2 test; mutasyonda kırmızı |
| K7 | `IgnoreQueryFilters` = 0, `new KuyumcuDbContext` yalnız fabrikada | ✅ | Aşağıda §4 |
| K8 | Giriş ucunda gerçek 429 | ✅ | Aşağıda §3 |
| K9 | `tenant_id` indeksleri mevcut | ✅ | 15/15 tablo |
| K10 | Giriş/kayıt/refresh akışları çalışıyor | ✅ | Aşağıda §3 |

---

## 2. MUTASYON KANITI — filtrenin çalıştığının tek kanıtı

Yeşil test tek başına hiçbir şey ispatlamaz. Koruma **geçici olarak kapatıldı** (`OnModelCreating`'deki filtre döngüsü atlandı + yazma denetçisi boru hattından çıkarıldı) ve testler tekrar koşuldu:

```
KORUMA KAPALI:
  Başarısız! - Başarısız: 11, Başarılı: 2, Toplam: 13

  Başarısız M1_YabanciKasaCiplakSorguyla_Gorunmez
  Başarısız M1_YabanciCariVeUrunDeGorunmez
  Başarısız M1_ListelemeYalnizKendiKiracisiniDondurur
  Başarısız M1_TenantTablosuKendisiDeFiltrelenir
  Başarısız M2_BaskaKiraciyaKayitEklenemez
  Başarısız M2_TenantIdBossaAktifKiraciylaDamgalanir
  Başarısız M3_KaydinKiracisiDegistirilemez
  Başarısız M3_SistemBaglamiyla_CekilenYabanciKayit_KiraciBaglamindaKaydedilemez
  Başarısız Faz0_KasaSizintisi_ServisAtlanarakDaEngellenir
  Başarısız Faz0_CariSizintisi_ServisAtlanarakDaEngellenir
  Başarısız SistemBaglami_TenantIdVerilmemisKaydiReddeder

KORUMA AÇIK (geri alındıktan sonra):
  Başarılı! - Başarısız: 0, Başarılı: 137, Toplam: 137
```

Yeşil kalan 2 test (`SistemBaglami_TumKiracilariGorur`, `BaglamsizDbContext_Kurulamaz`) koruma olmadan da geçmesi gerekenlerdir — doğru davranış.

Mutasyon geri alındı; `grep -c MUTASYON` = **0**.

---

## 3. CANLI DOĞRULAMA (gerçek sunucu, gerçek HTTP)

### Kiracı izolasyonu — canlı
Taze bir kiracı açıldı (`/auth/kayit` → HTTP 201), JWT alındı, uçlar çağrıldı:

| | Veritabanında TOPLAM | Yeni kiracının GÖRDÜĞÜ |
|---|---|---|
| cari | **19** | **0** |
| ürün | **29** | **0** |
| kasa | **7** | **0** |
| şube | 6 | 1 *(kendi "Merkez Şube"si)* |

### Kimlik akışları
```
POST /auth/kayit   -> 201
POST /auth/giris   -> 200
GET  /auth/ben     -> 200   (istek bağlamında, Mehmet'in şartı)
GET  /saglik       -> 200
```

### Oran sınırı — gerçek 429
```
deneme 1 -> 401 · 2 -> 401 · 3 -> 401 · 4 -> 401 · 5 -> 401
deneme 6 -> 429 · 7 -> 429 · 8 -> 429

HTTP/1.1 429 Too Many Requests
Retry-After: 60
{"message":"Çok fazla istek gönderildi, lütfen biraz bekleyin",
 "error":"TooManyRequests","statusCode":429}
```

### Sıkıştırma
`_framework/blazor.boot.json` → `Content-Encoding: gzip` (önceden sıkıştırma hiç yoktu).

**Test kiracısı temizlendi** — geliştirme veritabanı kayıt sayısı 3 kiracıyla eski hâline döndü.

---

## 4. CI DENETİMLERİ (yorum satırları hariç, yalnız kod)

| Denetim | Beklenen | Ölçülen |
|---|---|---|
| `IgnoreQueryFilters` | 0 | **0** ✅ |
| `new KuyumcuDbContext` | 1 (yalnız `TasarimZamaniFabrika`) | **1** ✅ |
| `.Find(` / `FindAsync(` | 0 | **0** ✅ |
| `ExecuteUpdate/Delete` | 4 (beyaz liste) | **4** ✅ — Auth ×3, Kur ×1, hepsi sistem kapsamında |
| `IKiraciVarlik` uygulayan varlık | 15 | **15** ✅ |

---

## 5. CANLI TESTTE YAKALANAN İKİ GERÇEK HATA

Madde 1 gereği: ikisi de testlerden **geçmişti**, yalnız gerçek sunucuda ortaya çıktı.

### Hata 1 — Giriş 500 veriyordu (ürünü tamamen kırardı)
`SistemKapsami` bir HTTP isteğinin **içinde** yeni kapsam açıyor (ör. `AuthServisi.Giris`). O çocuk kapsamda `IHttpContextAccessor` isteği hâlâ görüyor. İlk yazımımda `HttpContext`'e öncelik verilmişti → sistem bağlamı eziliyor, kiracı çözülemiyor, ctor fail-closed patlıyordu:

```
System.InvalidOperationException: Kiracı bağlamı çözülemedi
    POST /api/v1/auth/giris  ->  500
```

**Düzeltme:** açıkça kurulan kapsam `HttpContext`'ten **önceliklidir**. Gerekçe koda yorum olarak işlendi.
**Not:** Fail-closed tasarım burada işe yaradı — hata sessizce yanlış veri döndürmek yerine gürültüyle patladı.

### Hata 2 — 429 hiç dönmüyordu (oran sınırı etkisizdi)
`MapControllers().RequireRateLimiting(Genel)` uca kural olarak eklendiğinde, action üzerindeki `[EnableRateLimiting(Giris)]` sıkı politikasını **eziyordu**. Giriş ucu 5/dk yerine 100/dk ile çalışıyordu; 8 ardışık hatalı denemede 429 dönmedi.

**Düzeltme:** genel limit `GlobalLimiter` olarak kuruldu — uç politikasıyla **zincirlenir, ezmez**. Ayrıca `UseRateLimiter()` kimlik doğrulamadan **sonraya** alındı ki genel limit kullanıcı başına bölütlenebilsin.

---

## 6. DEĞİŞEN DOSYALAR

**Yeni (5):** `Data/KiraciBaglami.cs` · `Data/SistemKapsami.cs` · `Data/KiraciYazmaDenetcisi.cs` · `Common/OranSiniri.cs` · `Tests/KiraciFiltreTestleri.cs`
**Yeni migration:** `20260911133733_KiraciIndeksleri` — 14 indeks
**Değişen (8):** `Data/KuyumcuDbContext.cs` · `Data/Entities.cs` (15 sınıfa işaretçi) · `Data/TasarimZamaniFabrika.cs` · `Program.cs` · `Modules/Auth/AuthModulu.cs` · `Modules/Kur/KurModulu.cs` · `Modules/Urun/UrunModulu.cs` · `Tests/TestOrtami.cs` + 4 test dosyası (çağrı noktaları)

### Mimari — iki katman
- **Okuma:** global sorgu filtresi. Kiracı modeli ile sistem modeli **ayrı önbelleklenir** → kiracı sorgusunda saf `tenant_id = @p` çıkar, OR yok, indeks kullanılır.
- **Yazma:** `SaveChangesInterceptor`. Filtre yalnız SELECT'i korur; `Add()` ve kiracı taşıma yollarını **yalnız denetçi** kapatır.
- **Fail-closed:** bağlamsız DbContext ctor'da patlar; işaretçisiz yeni varlık eklenirse uygulama **açılmaz**.
- **Kaçış yolu:** `IgnoreQueryFilters` yerine tek sınıf (`SistemKapsami`), 4 maddelik beyaz liste, grep'le denetlenebilir.

---

## 7. MEHMET'İN VETOSU

| # | Şart | Durum |
|---|---|---|
| 1 | Global query filter var | ✅ |
| 2 | `IKiraciBaglami` + dar, denetlenebilir sistem kapsamı | ✅ |
| 3 | En az 3 mutasyon testi | ✅ (11 test mutasyonda kırmızı) |
| 4 | `StokSayimGuncelle`'ye `SubeDogrula` | ✅ |
| 5 | Rate limit + gerçek 429 | ✅ |
| 6 | Tüm testler yeşil | ✅ (137/137) |

**Altı şartın altısı da kanıtlandı.** Vetonun kaldırılması Mehmet'in yazılı onayına sunulur — kararı ben veremem.

---

## 8. AÇIK KALANLAR (dürüstçe)

1. **68 elle `TenantId ==` şartı duruyor** — bilinçli (ADIM1-KARAR §3/Ç2). Temizlik **Adım 1b**. Bertan 73, Taşkın 68 saydı; fark kapatılmadı, o turda tek tek sayılacak.
2. **`ExecuteUpdate/Delete` yazma denetçisine uğramaz.** Mevcut 4 kullanım sistem kapsamında, sorun yok; ama **yeni bir kullanım eklenirse koruma çalışmaz.** CI grep'i bunun için var, henüz CI'a bağlanmadı.
3. **CORS hâlâ tamamen açık** (`AllowAnyOrigin`). Mehmet'in ek notu; bu pakette kapsam dışı bırakıldı.
4. **Denetim izi (audit log) yok.** Mehmet "MASAK verisinden ÖNCE kapanmalı" dedi → **Adım 3'ün ön şartı**.
5. **İndeks performans kazancı ölçülmedi.** İndeksler kuruldu ama öncesi/sonrası sorgu süresi karşılaştırması yapılmadı; mevcut veri hacmi zaten küçük olduğundan anlamlı ölçüm pilotta yapılabilir.
6. **Blazor boyut kazancı tam ölçülmedi** — gzip'in açıldığı doğrulandı, 9,26 MB'ın kaça düştüğü uçtan uca ölçülmedi.

---

## 9. PLAN İLERLEMESİ

| Adım | Konu | Durum |
|---|---|---|
| 1 | Global kiracı filtresi | ✅ **bitti** (kanıt: bu rapor) |
| 2 | Oran sınırı + taşıma | ✅ **bitti** (kanıt: §3) |
| 3 | MASAK veri modeli | ⬜ sırada — ön şartı: denetim izi (§8/4) |
| 4 | MASAK kural motoru | ⬜ |
| 5 | e-Fatura / e-Arşiv | 🔒 Kurucu K1 + K2 bekliyor |
| 6 | Hurda kimlik + gider pusulası | ⬜ |
| 7 | POS–fatura mutabakatı | ⬜ |
| 8 | KMTS opsiyonel alan | ⬜ |
| 9 | Demo engelleri | ⬜ |

---

## EK — 2026-09-12: §8'deki iki ölçüm açığı kapatıldı

### §8/6 Blazor boyut kazancı — ÖLÇÜLDÜ
Canlı sunucudan, `blazor.boot.json`'daki her kaynak tek tek çekildi (sıkıştırmasız ve `Accept-Encoding: gzip, br` ile):

| Grup | Dosya | Sıkıştırmasız | Sıkıştırmalı | Üretimde |
|---|---|---|---|---|
| assembly | 201 | **19,59 MB** | **7,53 MB** | yüklenir |
| pdb | 2 | 0,23 MB | 0,15 MB | yok (Debug artığı) |

**Kazanç %61,5.** `Content-Encoding: gzip` + `Vary: Content-Encoding` başlıkları doğrulandı.

**Bertan'ın 9,26 MB / 61 dosya ölçümüyle fark neden?** Bu ölçüm **Debug** derlemesinde yapıldı — Debug'da IL trimming/linker çalışmaz, tüm bağımlılıklar ayrı assembly olarak iner (201 dosya). Release + trimming ile dosya sayısı ve boyut belirgin düşer. **Yani 7,53 MB tavan değil, Debug'ın en kötü hâlidir.** Release ölçümü yayın hazırlığında yapılmalı; bu, `wasm-tools` iş yükünün kurulmasını gerektiriyor (ADIM1-KARAR §2'de zaten kurulu olmadığı ölçülmüştü).

### §8/2 CI denetimi — BAĞLANDI
`dotnet/denetim.sh` yazıldı; `dotnet test && ./denetim.sh` ile koşar, ihlalde çıkış kodu 1.

Denetlediği: `IgnoreQueryFilters` (0), `new KuyumcuDbContext` (yalnız fabrika), `Find/FindAsync` (0), `ExecuteUpdate/Delete` (beyaz liste 4), `IKiraciVarlik` kapsama, `HasQueryFilter` varlığı, oran sınırı / HTTPS / sıkıştırma / sağlık ucu, CORS'un açık olmaması. Yorum satırları elenir — "eskiden AllowAnyOrigin vardı" açıklaması ihlal sayılmaz.

**Betiğin kendisi mutasyonla sınandı:** `RaporModulu`'na bilerek `IgnoreQueryFilters` sokuldu → betik dosya:satır ile yakaladı, çıkış kodu 1; geri alınınca 0.

### §8/3 CORS — KAPATILDI
`AllowAnyOrigin` kaldırıldı. Artık `Cors:Origins` beyaz listesi; **liste boşsa hiçbir çapraz origin'e izin verilmez** (fail-closed). Üretimde Blazor ve API aynı süreçten servis edildiği için boş liste doğru varsayılandır; geliştirmede localhost portları tanımlı.
