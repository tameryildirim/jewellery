# PAKET P-44 — Kasa negatif bakiye kapısı (tek merkez · `KasaBakiyeKapisi`)

**Tarih:** 2026-09-23 · **Rol:** Taşkın (backend) · **Dalga:** PARA VE SAYI DOĞRULUĞU (tur 8 triyajı · cumartesi kümesi)
**Durum:** BİTTİ (denetim.sh hariç — aşağıda § 8'de gerçek çıktısıyla raporlandı)
**Gerçek süre:** ~1 saat 5 dakika (triyaj tahmini 2,0 saat — tahmin YÜKSEK çıktı, kalibrasyon için not edildi)

---

## 1. ÖNCE ÖLÇÜM — triyajın doğrulayamadığı (a) maddesi KAPANDI

Triyaj: *"fikstür kasasının −786.498,80 TL'ye HANGİ işlemlerle düştüğü ÖLÇÜLMEDİ"*.
Kod yazmadan önce ölçüldü (`kuyumcu_net`, kasa `01M3227XT7DXVZKS9HC54MBYAN`):

| Ölçüm | Değer |
|---|---|
| Kasa bakiyesi (bugün) | **−738.298,80 TL** (triyajdaki −786.498,80 değil — aradan işlem geçmiş) |
| `SUM(kasa_hareketleri.tutar)` | **−738.298,80 TL** → **defter tutarlı**, bakiye defterden türetilebiliyor |
| Çıkışlar | 5 satır · −883.048,80 TL |
| Girişler | 5 satır · +144.750,00 TL |

Çıkışların kaynak hareket türleri (`hareketler` JOIN):

| Tür | Adet | Tutar |
|---|---|---|
| `ALIS` | 4 | −858.500,00 TL |
| `HURDA_ALIS` | 1 | −24.548,80 TL |
| `TAHSILAT` (giriş) | 1 | +50.000,00 TL |
| `SATIS` (giriş) | 4 | +94.750,00 TL |

**Sonuç:** sızıntının tek kaynağı `SatisModulu.KasaGuncelle`'nin **negatif delta** çağrılarıdır
(alış ve hurda alımı). Elle kasa çıkışı bu kasada hiç kullanılmamış. Yani kapı doğru yere kondu;
"kapı eklenir ama asıl yol gözden kaçar" riski gerçekleşmedi. Bu bir **ölçümdür, tahmin değildir**.

Ayrıca negatif bakiyeli 4 kasa var; biri **`QA-TEST G2 EUR Kasa` (−100,00 EUR)** — QA fikstürüdür,
**dokunulmadı**.

---

## 2. Spesifikasyon ↔ kod doğrulaması (dosya ayak izi)

| Dosya | Spesifikasyon | Gerçek |
|---|---|---|
| `Modules/Kasa/KasaBakiyeKapisi.cs` | YENİ | ✅ oluşturuldu |
| `Modules/Kasa/KasaModulu.cs` | var | ✅ (`HareketGir` kapısızdı, kapı eklendi) |
| `Modules/Gider/GiderModulu.cs` | var | ✅ (kopya kontrol 280–285 → tek çağrı) |
| `Modules/Gider/VirmanModulu.cs` | var | ✅ (kopya kontrol 130–134 → tek çağrı) |
| `Modules/Satis/SatisModulu.cs` | var | ✅ (`KasaGuncelle` kapısızdı, kapı eklendi) |
| `Modules/Satis/StornoServisi.cs` | var | ✅ (3 noktada gerekçeli istisna işareti) |
| `Kuyumcu.Tests/KasaBakiyeKapisiTestleri.cs` | YENİ | ✅ 7 test |

Satır numaraları spesifikasyondakiyle **birebir tuttu** (GiderModulu 280, VirmanModulu 130).

---

## 3. Yapılanlar

### 3.1 Yeni: `KasaBakiyeKapisi`
- `Dogrula(Data.Kasa kasa, decimal isaretliDelta)` — `kasa.Bakiye + isaretliDelta < 0m` → `BadRequestException`.
- **Pozitif delta hiçbir zaman engellenmez** (`isaretliDelta >= 0m` → erken çıkış). Gerekçe: canlıda
  ZATEN eksiye düşmüş kasalar var; "bakiye+delta<0" koşulu kayıtsız uygulansaydı o kasalara
  **para GİRİŞİ de** reddedilir, kiracı kasasını bir daha dolduramazdı.
- Mesaj **tek yerden** (`Mesaj(...)`): kasa adı · mevcut bakiye · istenen tutar · para birimi ·
  "Nakit kasa eksiye düşemez."
- `StornoIcinAtlanir(decimal)` — gerekçeli istisnanın **görünür** işareti (sessiz atlama yok;
  `grep KasaBakiyeKapisi` ile sayılabilir).
- Banka hesapları **kapsam dışı** (KMH gerekçesi sınıf dokümantasyonunda korundu).

### 3.2 Bağlanan yollar

| Yol | Önce | Sonra |
|---|---|---|
| `GiderModulu` gider ödemesi | elle kopya kontrol | `Kasa.KasaBakiyeKapisi.Dogrula(kasa, -tutar)` |
| `VirmanModulu` kasa→X virman | elle kopya kontrol | `Dogrula(kaynak.Ad, kaynak.Bakiye, kaynak.ParaBirimi, -tutar)` |
| `KasaModulu.HareketGir` (elle çıkış) | **KAPISIZ** | `KasaBakiyeKapisi.Dogrula(kasa, netTutar)` |
| `SatisModulu.KasaGuncelle` (alış/hurda/iade/para üstü — 862, 979, 1512, 1691, 2067) | **KAPISIZ** | `Dogrula(kasa, tutarDelta)` — tek yerde, tüm çağrıları kapsar |
| `StornoServisi` × 3 (210, 436, 756) | kapısız | `StornoIcinAtlanir(...)` — **gerekçeli istisna** |
| `AcilisServisi.KasaAcilisi` | **KAPISIZ** | `Dogrula(kasa, satir.TlTutar)` — § 4'teki SAPMA |

---

## 4. SAPMALAR ve gerekçeleri

**S1 — İkinci `Dogrula` aşırı yüklemesi eklendi (spesifikasyon tek imza öngörüyordu).**
`VirmanModulu` çağrı noktasında elde `Data.Kasa` yok, `VirmanTaraf` projeksiyonu var. Seçenekler:
(a) ikinci bir DB okuması eklemek, (b) aşırı yükleme. (b) seçildi — mesaj yine **tek** özel
metottan üretiliyor, yani "tek merkez" koşulu bozulmadı.

**S2 — Mesaj metni BİRLEŞTİRİLDİ (spesifikasyon "mesaj metni korunur" diyordu).**
Spesifikasyon hem "mesaj tek yerden" hem "mesaj metni korunur" diyor; bu ikisi çelişiyordu, çünkü
`GiderModulu` ve `VirmanModulu` mesajları ZATEN farklıydı (Gider'de "Nakit kasa eksiye düşemez."
cümlesi var, Virman'da yok; ikisinde de kasa adı yoktu). Tek merkez kuralı üstün tutuldu: tek mesaj,
Gider'in tam bilgisi + **kasa adı** (spesifikasyonun kendi alan listesi kasa adını istiyor).
Eski metne bağlı hiçbir test yok (`grep "Kasa bakiyesi yetersiz" Kuyumcu.Tests` → 0 eşleşme).

**S3 (EK BULGU — spesifikasyon dışı) — `AcilisServisi` açılış GERİ ALMA yolu kapısızdı ve kapatıldı.**
Kabul koşulu "bakiyeyi azaltan hiçbir yazma yolu kapısız kalmaz" diyor. Ölçüm:
`AcilisServisi.GeriAl` (satır 274) `TlTutar = -kalem.TlTutar` ile `KasaAcilisi(..., tersKayit: true)`
çağırıyor; doğrulama hattındaki "negatif açılış reddedilir" kontrolü (satır 505) bu yolda
**koşmuyor**. Yani 10.000 TL açılış girilip harcandıktan sonra açılış geri alınınca kasa eksiye
düşüyordu. Kapı eklendi. Storno istisnasına dahil EDİLMEDİ: açılış devri bir defter hareketinin
tersi değil, bir **başlangıç iddiasının** geri alınmasıdır; harcanmış parayı geri almanın doğru
yolu önce harcamaları geri almaktır ve hata mesajı bunu söyler. Tam süit bu değişiklikle yeşil
(`AcilisBakiyeTestleri` dahil).

**S4 — Mevzuat/yasal hüküm kodlanmadı.** Bu pakette yapılandırmadan okunması gereken bir mevzuat
hükmü YOK; kural bir muhasebe değişmezidir ("çekmecede olmayan para ödenemez"), eşik içermez.

---

## 5. Migration

**GEREKMEDİ.** Şema değişikliği yok; yeni varlık/kolon/indeks yok. `dotnet ef` çalıştırılmadı.
Migration sayısı: **66** (değişmedi).

---

## 6. Derleme

```
dotnet build Kuyumcu.sln --nologo   →  0 Uyarı · 0 Hata
```
(son mutasyon geri alındıktan SONRA tekrar koşuldu — yine 0/0.)

---

## 7. Testler

| Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| `KasaBakiyeKapisiTestleri` (filtreli) | 7 | 7 | 0 | 0 |
| **TAM SÜİT** (`Kuyumcu.Tests`) | **3967** | **3967** | **0** | **0** |

Önce: 3960 test · Sonra: **3967** (+7 yeni).

### 7.1 Yeni testler (7)
1. `Elle_kasa_cikisi_bakiyeyi_negatife_dusuremez_400`
2. `Elle_kasa_cikisi_bakiye_kadarsa_200_ve_bakiye_sifir` (sınır: bakiyenin tamamı çekilebilir)
3. `Hurda_alimi_kasa_bakiyesini_asarsa_400`
4. `Satis_iadesi_kasa_bakiyesini_asarsa_400`
5. `Storno_negatif_bakiyede_bile_200_doner` (gerekçeli istisna)
6. `Banka_hesabi_kapiya_tabi_degil_200` (KMH — kapsam dışı)
7. `Kasa_hareket_toplami_her_zaman_bakiyeye_esit` (değişmez: reddedilen istek deftere DE bakiyeye DE dokunmaz)

### 7.2 ⚠ ORTAM KARARSIZLIĞI — GİZLENMEDİ (Anayasa md.1)
İlk tam süit koşumunda **1 kırmızı** vardı:
`YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner` →
`MySqlException: Unknown database 'kuyumcu_t1_hazir'`.
- Bu test P-44'ün dokunduğu hiçbir dosyayı kullanmıyor.
- Yalıtılmış koşumda **10/10 yeşil**.
- İkinci tam süit koşumunda **3967/3967 yeşil**.
- Sebep ölçüldü: aynı çalışma ağacında **paralel bir `dotnet test` süreci** var
  (`ps -ax | grep Kuyumcu.Tests` → 4 süreç) ve paylaşılan test şemalarını
  (`kuyumcu_test`, `kuyumcu_t1_hazir`) düşürüp yeniden kuruyor. Mutasyon koşumlarından birinde de
  aynı yarış `Can't create database 'kuyumcu_test'; database exists` hatasıyla 7 testi birden
  düşürdü, tekrar koşumda düzeldi.
- **Kalan risk:** çok ajanlı koşumda test veritabanı paylaşımlı; `KUYUMCU_TEST_DB` ajan başına
  ayrılmadıkça bu yarış tekrar eder. Açık kalem olarak kaydedildi (§ 11).

---

## 8. `dotnet/denetim.sh`

**DENETİM TEMİZ DEĞİL — 4 ihlal var ve gizlenmiyor.** Gerçek çıktı:

```
✗ P-36:  HAM_SELECT           33 > tavan 30 — ham <select> EKLENDİ, ortak Secici kullanılmalı
✗ P-36:  HAM_INPUT            43 > tavan 42 — ortak Form bileşeni kullanılmalı
✗ P-36c: HAM_SELECT_SERBEST    3 — serbest ekranda ham <select>
✗ P-36c: HAM_INPUT_SERBEST     1 — serbest ekranda ham <input>
DENETİM BAŞARISIZ — yukarıdaki ihlaller giderilmeli.
```

**Bu ihlaller P-44'ten DOĞMADI.** Kanıt:
- Dördü de `Kuyumcu.Web/Pages/*.razor` sayaçları. P-44 **hiçbir `.razor` dosyasına dokunmadı**
  (§ 10 değişen dosya listesi: yalnız `.cs`).
- Bugün 12:00'dan sonra değişmiş razor dosyaları: `Giris.razor`, `SiparisDetay.razor`,
  `Yedekler.razor` — bunlar paralel yürüyen başka paketlerin (P-51/P-56 ailesi) dosyaları.
- Bu oturum başlamadan önceki kapı kaydı (ORKESTRATOR-DURUM, 2026-09-22 06:04) "denetim TEMİZ"
  diyordu; arada yalnız paralel ajanların razor değişiklikleri var.

**Karar:** ihlal P-44'ün kapsamı dışında (tek dokunuş kuralı gereği o razor sayfalarına bu paketten
dokunulmadı). Orkestratöre bildirilir; ilgili paket sahibi kapatmalıdır.

---

## 9. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/P-44/` altına kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli
değişiklik → filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile **birebir aynı** kanıtlandı.

| Kod | Değişiklik | Kırmızıya dönen testler | Geri alım |
|---|---|---|---|
| **M44-1** | `Dogrula(Data.Kasa, decimal)` gövdesi → `return;` | `Elle_kasa_cikisi_bakiyeyi_negatife_dusuremez_400` · `Hurda_alimi_kasa_bakiyesini_asarsa_400` · `Satis_iadesi_kasa_bakiyesini_asarsa_400` · (+ bonus `Kasa_hareket_toplami_her_zaman_bakiyeye_esit`) → **4 kırmızı** | `cmp` AYNI |
| **M44-2** | `< 0m` → `< -1000000m` | `Elle_kasa_cikisi_bakiyeyi_negatife_dusuremez_400` (+ 3 bonus) → **4 kırmızı** | `cmp` AYNI |
| **M44-3** | `< 0m` → `<= 0m` | `Elle_kasa_cikisi_bakiye_kadarsa_200_ve_bakiye_sifir` → **1 kırmızı** | `cmp` AYNI |
| **M44-4** | `StornoServisi:435` `StornoIcinAtlanir(tersTutar)` → `Dogrula(kasa, tersTutar)` | `Storno_negatif_bakiyede_bile_200_doner` → **1 kırmızı** | `cmp` AYNI |

**Test açığı bulunmadı** — dört mutasyonun dördü de en az bir testi kırdı, hiçbiri sessiz geçmedi.
Son mutasyondan sonra `dotnet build Kuyumcu.sln` → **0 Uyarı · 0 Hata**.

---

## 10. Canlı doğrulama (localhost:5198 · `kuyumcu_net`)

Test kiracısı: **`PAKETP-44-TEST`** (`01M374RTAQAXEH34CGCQD2JGK3`) — iş sonunda tamamen silindi.
5199 portuna **dokunulmadı** (koşum sırasında ve sonrasında 2 dinleyicisiyle ayakta).

| # | İstek | Beklenen | Gerçek | Yanıt gövdesi (özet) |
|---|---|---|---|---|
| 1 | `POST /api/v1/kasalar` | 200 | **200** | `bakiye: 0.00` |
| 2 | `POST /kasalar/{id}/hareket` `GIRIS 1000` | 200 | **200** | `bakiye: 1000.00` |
| 3 | `POST /kasalar/{id}/hareket` `CIKIS 1000.01` | **400** | **400** | `'P44 Kasa' kasasının bakiyesi yetersiz: 1000.00 TRY var, 1000.01 TRY isteniyor. Nakit kasa eksiye düşemez.` |
| 4 | `POST /kasalar/{id}/hareket` `CIKIS 1000` | 200 | **200** | `bakiye: 0.00` (sınır: tamamı çekilebiliyor) |
| 5 | `POST /kasalar/{id}/hareket` `GIRIS 500` | 200 | **200** | `bakiye: 500.00` |
| 6 | `POST /api/v1/islemler/hurda-alis` 10 g K22 × 2.800 TL/has (=25.648,00 TL) | **400** | **400** | `'P44 Kasa' kasasının bakiyesi yetersiz: 500.00 TRY var, 25648.00 TRY isteniyor. Nakit kasa eksiye düşemez.` |

**SQL kanıtı (reddedilen istekler hiçbir şey yazmadı):**

| Sorgu | Sonuç |
|---|---|
| `SELECT bakiye FROM kasalar WHERE id=…` | `500.00` |
| `SELECT SUM(tutar) FROM kasa_hareketleri WHERE kasa_id=…` | `500.00` → **defter = bakiye** |
| `SELECT COUNT(*) FROM kasa_hareketleri WHERE kasa_id=…` | `3` (yalnız başarılı 3 hareket) |
| `SELECT COUNT(*) FROM hareketler WHERE tenant_id=… AND tur='HURDA_ALIS'` | `0` (reddedilen hurda alımı deftere düşmedi) |

**Temizlik:**
- API PID durduruldu · `lsof -nP -iTCP:5198 -sTCP:LISTEN` → **boş**.
- `tenant_id` kolonu olan **70 tablonun tamamından** kiracı satırları silindi; doğrulama sorgusu
  (her tabloda `COUNT(*)>0 HAVING`) → **hiç satır dönmedi**.
- Canlı doğrulama için eklenen `kiraci_davetleri` satırı silindi.
- `tenants` sayısı: **başlangıç 9 → son 9** ✅
- `QA-TEST-` önekli kiracılara ve `QA-TEST G2 EUR Kasa` (−100 EUR) fikstürüne **dokunulmadı**.

---

## 11. DoD sözleşmesi

P-44 **yeni varlık/kayıt türü OLUŞTURMAZ** — yeni tablo, yeni DTO, yeni uç, yeni ekran yoktur.
Eklenen tek şey bir **doğrulama kapısıdır** (saf, durumsuz `static` sınıf). Bu nedenle:

| Kalem | Durum | Gerekçe / kanıt |
|---|---|---|
| Yeni varlık → PATCH ucu | **gerekmedi** | Yeni varlık yok (migration yok, `Entities.cs` değişmedi) |
| Yeni varlık → DELETE/pasifleştirme | **gerekmedi** | Yeni varlık yok |
| `[ModulYetkisi]`/`[RolGerekli]` | **değişmedi** | Yeni uç yok; kapı MEVCUT uçların içinde çalışır, o uçların yetkileri aynen korunur |
| Denetim kaydı | **değişmedi** | Kapı bir yazma değil bir REDdir; reddedilen istek zaten hiçbir kayıt yazmaz (canlı SQL kanıtı § 10) |
| Ekranda Düzenle/Pasife al | **gerekmedi** | Yeni kayıt türü yok |
| Düğme davranışı (DoD EK 1) | **değişmedi** | Hiçbir `.razor` dosyasına dokunulmadı |
| Tek düzenleme kalıbı (DoD EK 1/2) | **değişmedi** | `Pages/` altında `ModalPencere` düzenleme kullanımı **0 artış** (razor dosyası açılmadı) |

**Düzenlenebilirlik tablosu (etkilenen kayıtlar):**

| Kayıt | Düzeltme yolu | Değişmezlik gerekçesi |
|---|---|---|
| Kasa hareketi (elle) | `POST /kasalar/{id}/hareket/{hid}/storno` — **kapıya tabi değil**, negatif bakiyede bile çalışır | Append-only defter; hareket silinmez, tersi yazılır |
| Satış/alış/hurda/iade kasa bacağı | `StornoServisi` (48-A motoru) | Aynı |
| Kasa bakiyesi | Doğrudan yazılamaz; yalnız hareketlerden türer | `Kasa_hareket_toplami_her_zaman_bakiyeye_esit` testi bu değişmezi sabitler |

**Tek dokunuş (DoD EK 2):** Bu dalgada **hiçbir razor sayfası açılmadı** → "Dokunulan sayfalar: YOK".
Storno taklidi ("ters iade") üretilmedi; storno yalnız mevcut `StornoServisi` motorudur.

---

## 12. Koşul karşılama tablosu

| Kabul koşulu | Durum | Kanıt |
|---|---|---|
| `kasa.Bakiye` azaltan her yazma yolu kapılı ya da açık storno bayraklı | ✅ | § 13 grep dökümü |
| Elle kasa çıkışı negatife düşüremez → 400 | ✅ | Canlı #3 · test 1 |
| Hurda alımı / iade / para üstü negatife düşüremez → 400 | ✅ | Canlı #6 · test 3, 4 |
| Storno negatif bakiyeye rağmen 200 | ✅ | Test 5 · M44-4 mutasyonu |
| Banka kapsam dışı | ✅ | Test 6 |
| `dotnet test` 0 başarısız | ✅ | 3967/3967 |
| `dotnet/denetim.sh` TEMİZ | ❌ | § 8 — 4 razor ihlali, **P-44 kaynaklı değil** |
| Test listesindeki 7 testin hepsi | ✅ | § 7.1 |
| Mutasyon listesindeki 4 maddenin hepsi | ✅ | § 9 |

---

## 13. `kasa.Bakiye` kapı dökümü (kabul koşulunun ölçümü)

| Satır | Azaltabilir mi? | Kapı |
|---|---|---|
| `KasaModulu.cs:287` (`+= netTutar`) | EVET (KasaCikis) | ✅ `Dogrula` (284) |
| `SatisModulu.cs:2333` (`+ tutarDelta`) | EVET (alış/hurda/iade/para üstü) | ✅ `Dogrula` (2330) |
| `GiderModulu.cs:288` (`- tutar`) | EVET | ✅ `Dogrula` (281) |
| `VirmanModulu.cs:312` (`+ isaretliTutar`) | EVET (kaynak bacağı) | ✅ `Dogrula` (132, kaynak kasa ise) |
| `AcilisServisi.cs:355` (`+ satir.TlTutar`) | EVET (geri alma) | ✅ `Dogrula` (351) — **S3 ek bulgu** |
| `StornoServisi.cs:210` | EVET | 🟡 `StornoIcinAtlanir` — gerekçeli istisna |
| `StornoServisi.cs:436` | EVET | 🟡 `StornoIcinAtlanir` — gerekçeli istisna |
| `StornoServisi.cs:756` | EVET | 🟡 `StornoIcinAtlanir` — gerekçeli istisna |
| `EvrakModulu.cs:519` (`+ e.Tutar`) | HAYIR | `dto.Tutar <= 0` evrak oluşturmada reddedilir (satır 230) |
| `CariModulu.cs:988` (`+ tlTutari`) | HAYIR | `kasa` yalnız `tlTutari > 0` iken dolu (satır 951) |
| `CariModulu.cs:1402` (`+ dto.Tutar`) | HAYIR | `dto.Tutar <= 0` reddedilir (satır 1354) |

---

## 14. Değişen dosyalar

```
dotnet/Kuyumcu.Api/Modules/Kasa/KasaBakiyeKapisi.cs      (YENİ)
dotnet/Kuyumcu.Api/Modules/Kasa/KasaModulu.cs
dotnet/Kuyumcu.Api/Modules/Gider/GiderModulu.cs
dotnet/Kuyumcu.Api/Modules/Gider/VirmanModulu.cs
dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs
dotnet/Kuyumcu.Api/Modules/Satis/StornoServisi.cs
dotnet/Kuyumcu.Api/Modules/Kurulum/AcilisServisi.cs      (SAPMA S3)
dotnet/Kuyumcu.Tests/KasaBakiyeKapisiTestleri.cs         (YENİ)
```
Commit YAPILMADI (kurucu kuralı). `.razor` dosyası değiştirilmedi.

---

## 15. Açık kalanlar

1. **`denetim.sh` 4 razor ihlali** (P-36 / P-36c) — P-44 kaynaklı değil, paralel paketlerin razor
   değişikliklerinden. Orkestratör ilgili pakete yönlendirmeli.
2. **Test veritabanı yarışı** — çok ajanlı koşumda `kuyumcu_test` / `kuyumcu_t1_hazir` paylaşımlı;
   ajan başına `KUYUMCU_TEST_DB` verilmedikçe sahte kırmızılar tekrar eder.
3. **Canlıdaki 3 negatif kasa temizlenmedi** (−738.298,80 · −176.388,20 · −144.996,00 TL).
   P-44 **yeni** sızıntıyı kapatır; **mevcut** negatif bakiyeleri düzeltmez. Çıkış yolu
   **P-50**'nin işidir. ⚠ **Uyarı:** kapı devrede olduğu için bu üç kiracıda artık HİÇBİR nakit
   çıkışı yapılamaz (gider, alış, iade dahil) — demo bu kiracılarda hâlâ duvara toslar.
   P-50 gelene kadar düzeltmenin tek yolu storno ya da kasaya giriş yapmaktır.
4. **`QA-TEST G2 EUR Kasa` −100,00 EUR** — QA fikstürü, dokunulmadı; kalıcı fikstür politikası
   gereği kurucunun kararı.

---

## 16. Kurucuya / mali müşavire notlar

1. **Banka hesaplarında negatif bakiye hâlâ SERBEST.** Gerekçe kodda korundu: KMH (kredili mevduat)
   negatif bakiyeye izin verir. **Karar sizindir:** hesap bazında KMH limiti tanımlanacak mı, yoksa
   banka da kasa gibi sıfır altına kapatılsın mı? Şu an sınır **yok** — canlı doğrulamada banka
   hesabı 100,00 TL'yken 5.000,00 TL virman yapıldı ve bakiye −4.900,00 TL'ye düştü (bilinçli).
2. **Açılış geri alma artık reddedilebilir** (S3). Harcanmış bir açılış devrini geri almak isteyen
   kiracı önce harcamaları storno etmelidir. Mali müşavir bu sıralamayı onaylamalı.
3. **Mevcut negatif kasalar kilitlendi** (§ 15/3). Bu kiracılarda işlem yapılacaksa düzeltme kararı
   (storno mu, düzeltme fişi mi, kasaya giriş mi) mali müşavir onayıyla verilmelidir — P-44 bunu
   kendi başına seçmez.
