# PAKET 31 — POS cihazı takibi + komisyon + valör · UYGULAMA RAPORU

> Tarih: 2026-09-18 · Dalga 4 · Faz A + Faz B uygulandı · Durum: **BİTTİ**
> Derleme 0 hata / 0 uyarı · `dotnet test` **2806/2806 yeşil, 0 başarısız**
> `dotnet/denetim.sh` **TEMİZ** · 53 migration (2'si bu paket) · canlı doğrulama 5198'de yapıldı

---

## 1. Yapılanlar

### Veri modeli (Faz A)
`pos_cihazlari`, `pos_tahsilatlari` — dördü de `IKiraciVarlik`, global `HasQueryFilter`
otomatik bağlandı, indeksler `KuyumcuDbContext.OnModelCreating`'e kullanıcı sorgusu
gerekçesiyle tek tek yazıldı.

### Veri modeli (Faz B)
`pos_taksit_oranlari`, `pos_yatma_eslesmeleri` (+ `(tenant, tahsilat, banka_hareketi)`
BENZERSİZ indeks: aynı çift iki kez eşleşemez, yarışta veritabanı reddeder).

### Servis ve dosyalar
| Dosya | Ne yapıldı |
|---|---|
| **YENİ** `Modules/Pos/PosAyarlari.cs` | `CihazZorunlu(false)` · `ValorIsGunu(false)` · `IadeKomisyonIadeEdilir(false)` · `AzamiValorGun(90)` · `OtoEslestirToleransKurus(0)`. Hiçbir oran/valör sayısı koda gömülmedi. |
| **YENİ** `Modules/Pos/PosDefteri.cs` | `pos_tahsilatlari`'na yazan **TEK yol**. `Ekle` (komisyon + valör snapshot), `ValorHesapla`, `Ozetle` (yatan+durum türetimi). `SaveChanges` ÇAĞIRMAZ. |
| **YENİ** `Modules/Pos/PosModulu.cs` | 5 DTO + `PosServisi` (13 metot) + `PosController` (13 uç). `CihazCoz` ve `TaksitOraniCoz` satış yolundan çağrılan statiklerdir. |
| `Data/Entities.cs` | 4 varlık + `PosTahsilatDurumu` sabitleri. `Kasa`/`KasaHareket` **değişmedi**. |
| `Data/KuyumcuDbContext.cs` | 4 DbSet + 11 indeks. |
| `Modules/Satis/SatisModulu.cs` | `OdemeBacagiDto` ve 4 DTO'ya `PosCihazId` + `PosTaksitAdedi`; `KasaGuncelle` artık kasa hareketi Id'sini **döndürür** (bakiye aritmetiği DEĞİŞMEDİ); `PosDefterineYaz` yardımcısı; **6 çağrı noktası** bağlandı. |
| `Modules/Rapor/RaporModulu.cs` | `PosFaturaMutabakati` yanıtına `pos` bloğu + "valörü geçti, yatma görünmüyor" uyarısı. Mevcut `tahsilat` kırılımı **değişmedi**. |
| `Modules/Banka/BankaModulu.cs` | `HareketListele`'ye `posEslesmemis=true` süzgeci (GELEN + BEKLIYOR). `HareketEkle` **değişmedi**. |
| `Modules/Disaaktarim/Kaynaklar.cs` | 3 yeni CSV kaynağı (`pos-cihazlari`, `pos-tahsilatlari`, `pos-eslesmeleri`), kişisel veri yok. |
| `Modules/Meta/DuzeltmePolitikasi.cs` | 4 varlığın düzeltme damgası (2 DÜZENLENEBİLİR + 2 SALT_OKUNUR/DEFTER). |
| `Modules/ModulKayit.cs` · `Program.cs` · `appsettings*.json` | DI + `Pos` yapılandırma bölümü (`_not`: "bu eşikler mevzuat değil, banka üye işyeri sözleşmesidir"). |
| `Kuyumcu.Core/I18n/Sozlukler.cs` | `SozlukPos` (55 anahtar) **TR + EN**, `SozlukNav.Pos` / `.PosTahsilat`. Çıplak metin yok. |
| `Kuyumcu.Web/Servisler/PosServisi.cs` | Tiplenmiş istemci + **tüm** yanıt record'ları (Pages altında yeni record YOK). |
| `Kuyumcu.Web/Pages/Pos.razor` · `PosTahsilatlar.razor` | İki yeni ekran; `ErrorBoundary` sayfa kökünde. |
| `Kuyumcu.Web/Layout/KenarCubugu.razor` · `RotaBasliklari.cs` | Menü + üst bar başlıkları. |
| `dotnet/denetim.sh` | Yeni sayaç: `PosTahsilatlari.Add` = 1. |
| **YENİ** `dokumanlar/02-mimari/adr/0017-pos-alacak-defteri.md` | Yan defter kararı. |

### Uçlar (13)
`POST/GET/GET{id}/PATCH{id}/POST{id}/pasife-al` cihaz · `POST/PATCH` taksit oranı ·
`GET /pos/tahsilatlar` (sayfalı) · `GET /pos/valor-takvimi` (sunucuda toplanmış) ·
`GET /pos/tahsilatlar/{id}/eslesmeler` · `POST /pos/tahsilatlar/{id}/iptal` ·
`POST /pos/eslestir` · `POST /pos/oto-eslestir-onerisi` · `POST /pos/eslesmeler/{id}/geri-al`.
Hepsi `[ModulYetkisi("kasa")]`; yazma uçlarında `[RolGerekli]` TAM eşleşme.

---

## 2. DoD sözleşmesi (zorunlu bölüm)

| Varlık | Düzenleme yolu | Ekranda | Gerekçe |
|---|---|---|---|
| `pos_cihazlari` | `PATCH /pos/cihazlar/{id}` (kısmi) + `POST .../pasife-al` + `PATCH {aktif:true}` ile geri açma | `/pos` → **Düzenle** (Cekmece) · **Pasife al** (onay modalı) · **Aktifleştir** | DÜZENLENEBİLİR |
| `pos_taksit_oranlari` | `POST` ekle + `PATCH .../taksit-oranlari/{oranId}` | `/pos` çekmecesi içinde `SatirIciDuzenle` | DÜZENLENEBİLİR |
| `pos_tahsilatlari` | **Düzenlenemez.** Tek yol `POST .../iptal` (gerekçe ≥20, servis içinde ELLE kontrol) | `/pos/tahsilatlar` → satırda **Tahsilatı iptal et** + sayfada `DegismezIpucu` | SALT OKUNUR — satır satıştan TÜRETİLİR; düzeltme yolu chargeback/iptal ya da ilgili satışın iadesi |
| `pos_yatma_eslesmeleri` | **Silinmez.** `POST /pos/eslesmeler/{id}/geri-al` (gerekçe ≥20) | `/pos/tahsilatlar` eşleştirme bloğu | APPEND-ONLY (ADR-0004) |

Finansal varlıkta **hard DELETE yok**: hiçbir POS ucunda `Remove/RemoveRange/ExecuteDelete` yoktur.
Denetim kaydı: `DenetimServisi.YazmaKaydet` 7 noktada **çağrılır** (yeniden yazılmadı).

**Düzenlenemeyen kalem sayısı = 0**: `pos_tahsilatlari` ve `pos_yatma_eslesmeleri`
için gerekçe + doğru düzeltme yolu ekranda YAZILIDIR ve uçları düğmeyle açıktır.

---

## 3. Derleme · test · denetim

```
dotnet build Kuyumcu.sln --nologo   → 0 Hata, 0 Uyarı
dotnet test  (tam süit)             → Başarılı: 2806, Başarısız: 0, Atlanan: 0
dotnet/denetim.sh                   → DENETİM TEMİZ
```

Test sayısı **sonra 2806**. ⚠ DÜRÜST ÖLÇÜM (Madde 1): paket öncesi toplam bu
oturumda **ölçülmedi** (ilk tam süit koşumu değişikliklerden sonra yapıldı), bu
yüzden "önce" sayısı tahmin edilmiyor. Kesin olan: bu pakette **49 yeni test**
yazıldı (aşağıdaki tablo) ve ayrıca `RotaBaslikTestleri` teorisine iki yeni rota
vakası (`/pos`, `/pos/tahsilatlar`) eklendi.

| Dosya | Tip | Test |
|---|---|---|
| `PosCihazTestleri.cs` | DB | 8 |
| `PosTahsilatTestleri.cs` | DB | 18 |
| `PosEslestirmeTestleri.cs` | DB | 11 |
| `PosEkranTestleri.cs` | bUnit | 6 |
| `HttpPosTestleri.cs` | HTTP | 6 |
| `PosYardim.cs` | (yardımcı) | — |

### Güncellenen mevcut kayıtlar (kaçınılmaz, hepsi gerekçeli)
- `RotaBaslikTestleri`: 40 → **42** rota (iki yeni sayfa) + `RotaBasliklari.Harita`'ya iki başlık.
- `IkonTestleri`: kenar çubuğu rota listesine `/pos`, `/pos/tahsilatlar`.
- `DisaAktarimTestleri`: kaynak sayısı 6 → **9**.
- `DuzeltmePolitikasiTestleri`: 4 yeni varlığın damgası eklendi (test kendisi değişmedi, **harita** dolduruldu).
- `denetim.sh` tavanları: ModalPencere 51 → **53** (ikisi de yıkıcı onay), `Pages/*.razor` 40 → **42**.
  `IgnoreQueryFilters=0`, `ExecuteUpdate/Delete beyaz liste`, `Hareketler.Add=1`
  sayaçları **DEĞİŞMEDİ**.

---

## 4. Mutasyon kanıtları

| Kod | Mutasyon | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M31-1 | `PosDefteri`: `komisyon = 0m` | `KartSatisi_PosTahsilatiYazar_BrutKomisyonNet` (+3 türev) | `cmp` birebir |
| M31-2 | `ValorHesapla`: `AddDays(valorGun)` → `AddDays(0)` | `KartSatisi_ValorTarihiHesaplanir_TakvimGunu`, `ValorIsGunu_Acikken_HaftasonuAtlanir` | `cmp` birebir |
| M31-3 | `CihazCoz`: belirsizlikte BadRequest yerine `adaylar[0]` | `KartBacagi_BirdenCokAktifCihaz_Reddedilir` | `cmp` birebir |
| M31-4 | `Eslestir`: iki aşım kontrolü `if(false)` | `Eslesme_NetTutariAsamaz`, `Eslesme_BankaHareketiTutariniAsamaz` | `cmp` birebir |
| M31-5 | `Ozetle`: durum türetimi yerine sabit `YATTI` | `KismiEslesme_DurumKISMI`, `EslesmeGeriAl_SatirSilmez_OzetiDuzeltir` | `cmp` birebir |
| M31-6 | Liste/takvim sorgularından elle `TenantId ==` kaldırıldı | `PosTahsilati_ElleTenantSarti_FiltresizBaglamda_DaKorur` | `cmp` birebir |
| M31-6b | `CihazCoz`'dan elle `TenantId ==` kaldırıldı | `Cihaz_ElleTenantSarti_YabanciCihaz_FiltresizBaglamda_404` | `cmp` birebir |
| M31-7 | Cihaz–kasa para birimi kontrolü kaldırıldı | `Cihaz_KasaParaBirimiUyusmazsa_Reddedilir` | `cmp` birebir |
| M31-8 | `ValorTakvimi`: `ToListAsync` + bellekte `GroupBy` | `ValorTakvimi_SunucudaToplanir_SatirDonmez` | `cmp` birebir |
| M31-9 | `CihazGuncelle`: PATCH tam gövde üzerine yazsın | `CihazPatch_GonderilmeyenAlanlari_KORUR` | `cmp` birebir |
| M31-E | **Karar E (KasaModulu)** | **UYGULANMADI — DEVREDİLDİ** (aşağı bak) | — |

### ⚠ Bulunan TEST AÇIĞI ve kapatılması (Madde 1)
M31-6/6b ilk denemede **hiçbir testi kırmadı**: global `HasQueryFilter` ve şube
eşleşmesi mutasyonu maskeliyordu (Aşkın'ın "çok katmanlı koruma mutasyonu
yakalamaz" uyarısı gerçekleşti). İki **tek katman** testi yazıldı:
- `PosTahsilati_ElleTenantSarti_FiltresizBaglamda_DaKorur` — servis **filtresiz**
  (`SistemKiraciBaglami`) bağlamla kurulur, böylece yalnız elle `TenantId` şartı ölçülür.
- `Cihaz_ElleTenantSarti_YabanciCihaz_FiltresizBaglamda_404` — fikstür, A kiracısına
  ait ama B'nin **şube ve kasa kimliğini taşıyan** yapay bir cihaz yazar; şube
  savunması da devre dışı kalır.
Mutasyon tekrarlandı → **ikisi de kırmızı**. Son mutasyondan sonra `cmp` birebir + TEMİZ derleme.

---

## 5. Migration

| Ad | İçerik | Uygulama |
|---|---|---|
| `20260918015302_PosCihazVeTahsilat` | `pos_cihazlari` + `pos_tahsilatlari` (2 CreateTable) | `kuyumcu_net`'e uygulandı |
| `20260918015343_PosEslestirmeVeTaksit` | `pos_taksit_oranlari` + `pos_yatma_eslesmeleri` | `kuyumcu_net`'e uygulandı |

`dotnet ef database update` → `Done.` · `show tables like 'pos%'` → 4 tablo.
İki dilimin **ayrı** migration'a çıkması için Faz B varlıkları migration 1 üretilirken
geçici olarak `mb.Ignore<T>()` ile modelden çıkarıldı, sonra geri alındı; `--no-build`
**kullanılmadı**, her adımda derleme yeşildi. Migration adları spesifikasyondaki
`20260916*` / `20260917*` yerine **20260918** ile başlar (üretildikleri gün; EF damgayı
kendi koyar — sapma, zararsız).

---

## 6. Canlı doğrulama (localhost:5198, test kiracısı `PAKET31-TEST`)

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | `POST /pos/cihazlar` (Garanti POS, 0.0185, valör 1) | 200 + camelCase `id` | **200**, `id=01M2S4KY7F…` | yanıt gövdesi okundu |
| 2 | İkinci cihaz (Akbank POS) | 200 | **200** | — |
| 3 | `POST /islemler/satis` KART, `posCihazId` **YOK** | 400 "POS cihazı belirtilmeli" | **400** — `"POS cihazı belirtilmeli: bu şubede birden çok aktif cihaz var"` | fail-closed doğrulandı |
| 4 | `POST /islemler/satis` KART 10.000 + cihaz | 200 | **200** | — |
| 5 | `GET /pos/tahsilatlar` | brüt 10000 / komisyon 185,00 / net 9815,00 / valör bugün+1 | **brut 10000.0 · oran 0.0185 · komisyon 185.0 · net 9815.0 · valör 2026-09-18T21:00:00Z (= 19.09 TR günü başı) · BEKLIYOR** | JSON |
| 6 | `GET /kasalar/{id}` | önceki bakiye + 10.000 (brüt) | önce **0,00** → sonra **10.000,00** | POS defteri bakiyeyi ETKİLEMEDİ |
| 7 | `GET /pos/valor-takvimi` | ilgili günde net 9815,00 | **net 9815.00 · kalan 9815.00** | JSON |
| 8 | `PATCH /pos/cihazlar/{id}` yalnız `ad` | oran/valör/kasa/banka hesabı DEĞİŞMEZ | ÖNCE `Garanti POS · 0.0185 · 1 · <kasa> · NULL` → SONRA `Garanti POS 2 · 0.0185 · 1 · <kasa> · NULL` | SQL öncesi/sonrası |
| 9 | 5.000 TL GELEN dekont + `POST /pos/eslestir` | KISMI | **200** · `yatan 5000.0 · kalan 4815.0 · KISMI` | JSON |
| 10 | `POST /pos/eslesmeler/{id}/geri-al` | satır SİLİNMEZ, BEKLIYOR'a döner | **200** · SQL: `eslesen_tutar 5000.00 · geri_alindi 1 · gerekçe dolu`; tahsilat `BEKLIYOR/0.00`; banka hareketi `BEKLIYOR` | SQL |
| 11 | 9.815 TL dekont + `POST /pos/oto-eslestir-onerisi` | küme önerilir, hiçbir şey yazılmaz | **200** `{"bulundu":true,"toplam":9815.00,…}` | JSON |
| 12 | `POST /pos/eslestir` (öneri) | YATTI | **yatan 9815.0 · kalan 0.0 · YATTI** | JSON |
| 13 | Eşleşmiş tahsilatı iptal | 400 | **400** "Banka yatmasıyla eşleşmiş tahsilat iptal edilemez" | JSON |
| 14 | İptal gerekçesi "kisa" | 400 | **400** "Gerekçe en az 20 karakter olmalıdır" | JSON |
| 15 | `GET /raporlar/pos-fatura-mutabakati?tarih=2026-09-18` | pos bloğu doğru | `{"adet":1,"brutKart":10000.0,"komisyon":185.0,"beklenenNet":9815.0,"yatan":9815.0,"valoruGecmisBekleyenAdet":0}` · mevcut `tahsilat` kırılımı aynen | JSON |
| 16 | `POST /pos/cihazlar/{id}/pasife-al` (açık alacak kalmadı) | 200 | **200** `aktif=false` | JSON |

### ⚠ Birleşik koşul 2 — KASA SEMANTİĞİ ÖLÇÜMÜ (düzeltilmedi, ölçüldü)
Aynı kasaya **1 kart (10.000) + 1 nakit (3.000)** satış girildi; gün kapanışında
**gerçek nakit 3.000** beyan edildi:

```
POST /kasalar/{id}/gun-kapanisi  {"sayilanNakit":3000}   → HTTP 200
{"defterBakiye":13000.00,"sayilanNakit":3000,"fark":-10000.00,"kilitli":true}
```

**Fark = −10.000,00 TL = kart tahsilatının tamamı.** Kart tahsilatı kasaya brüt
girdiği, gün kapanışı ise sayılan **NAKDİ** karşılaştırdığı için kart satışı olan
her günde bu fark **kaçınılmazdır**. Bu paket davranışı **değiştirmedi** (Karar A);
`Kasa`'ya `tur(NAKIT|POS|BANKA)` kolonu eklenmedi, kapanış formülüne dokunulmadı —
konu **paket 30**'undur, kapsam donduruldu, **DEVREDİLDİ**.

### Temizlik
API PID `kill` ile durduruldu, `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**.
5199 portuna **dokunulmadı**. `information_schema`'dan `tenant_id` kolonu olan
**58 tablo** tek tek silindi + kiracı + geçici davet satırı.
`select count(*) from tenants` → **8** (başlangıçtaki değer). `pos_cihazlari`,
`pos_tahsilatlari`, `pos_yatma_eslesmeleri` → **0**.
`QA-TEST-` önekli kiracılara **dokunulmadı**.

---

## 7. Sapmalar ve gerekçeleri

1. **Karar E uygulanmadı (talimat gereği).** `KasaModulu.cs` dosyasına **tek satır**
   dokunulmadı, `KasaKomisyonTestleri.cs` açılmadı, `ElleHareket_*` iki test
   yazılmadı. **DEVREDİLDİ** — ayrı mühür ister. Sonucu: elle kasa hareketinde
   komisyon kusuru (bakiyeye net, harekete brüt) **devam ediyor** — bilinen, açık kusur.
2. **ADR numarası 0016 → 0017.** 0016 reyon yerleşimi ADR'ına aitti (kullanımda).
3. **Migration damgaları `20260918*`.** Spesifikasyon `20260916*`/`20260917*` demişti;
   EF damgayı üretim anına göre koyar. İki ayrı migration koşulu **karşılandı**.
4. **`AlisOlusturDto`'ya `PosCihazId` eklenmedi.** Kartla yapılan **alış ödemesi**
   (para bizden çıkar) POS cihazından tahsilat değildir; POS defteri bir **tahsilat**
   defteridir. `AlisIade` (para kasaya girer) `IadeDto` üzerinden **kapsamdadır**.
   6 nokta denetim testi bu sınırı açıkça yazar ve ölçer.
5. **`PosTaksitAdedi` alanı eklendi** (spesifikasyon yalnız `PosCihazId` diyordu).
   Taksit oranı tablosu (Faz B) girdi olmadan hiç kullanılamazdı.
6. **denetim.sh `ExecuteUpdate/Delete` beyaz listesi 5'tir, 4 değil** (görev metni 4 diyor).
   Ölçüm: `denetim.sh:53` bugün `5` yazıyor ve paket 32 ile böyle bırakılmış. POS
   uçlarında `ExecuteUpdate/ExecuteDelete` **yoktur**, sayı **artmadı**.
7. **Son migration 20260916060026 değil**; depoda 51 migration vardı (spesifikasyon
   metni eskimişti). Şimdi 53.
8. **bUnit `Cekmece` kilit sebebi** için sözlüğe 3 anahtar eklendi
   (`AdCokKisa`, `TumDurumlar`, `BankaHareketId`) — çıplak metin yasağı gereği.

---

## 8. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Karar E uygulanmadı, DEVREDİLDİ | ✅ | `KasaModulu.cs` git farkı yok; rapor § 7.1 |
| İki sevk dilimi + iki ayrı migration | ✅ | § 5 |
| `PosTahsilatlari.Add(` tüm ağaçta 1 + denetim.sh sayacı | ✅ | `denetim.sh` TEMİZ |
| `IgnoreQueryFilters=0`, `Hareketler.Add=1`, ExecuteUpdate sayısı sabit | ✅ | denetim çıktısı |
| 6 KART çağrı noktası DB denetim testi (CihazZorunlu=true) | ✅ | `KartBacagi_AltiCagriNoktasinin_HEPSI_PosaYazar` |
| `KartBacagi_TekCihaz_OtomatikSecilir` | ✅ | test yeşil |
| Kiracı sertliği + mutasyon kanıtı | ✅ | M31-6 / M31-6b (tek katman testleriyle) |
| PATCH gönderilmeyen alanı korur + canlı SQL karşılaştırma | ✅ | M31-9 + canlı adım 8 |
| Finansal varlıkta hard DELETE yok | ✅ | POS uçlarında `Remove/ExecuteDelete` 0 |
| `yatan_tutar`/`durum` yalnız `Ozetle`'den türetilir | ✅ | M31-5 |
| Valör takvimi sunucuda toplanır (SQL'de GROUP BY aranır) | ✅ | `ValorTakvimi_SunucudaToplanir_SatirDonmez` + M31-8 |
| Sayfalı liste + `Zaman.YerelGunAraligiUtc` + sınır uyarısı | ✅ | `TahsilatListele`, `sinirDegdi`, ekranda `SinirUyarisi` |
| InvariantCulture (takvim + CSV) test içinde doğrulanır | ✅ | `ValorTakvimi_SayilariInvariantCultureIleBicimlenir` |
| Kasa semantiği canlı ölçüldü, düzeltilmedi | ✅ | § 6, fark −10.000,00 |
| POS yalnız TRY, döviz ucu yok | ✅ | `Cihaz_KasaParaBirimiUyusmazsa_Reddedilir` |
| 5464 sayılı kanun için koda kural / ekrana uyarı KOYULMADI | ✅ | grep: "5464" 0 satır |
| Tevsik kart bacağına uygulanmaz, MASAK dokunulmadı | ✅ | `SatisModulu.cs:915-917` değişmedi |
| Pages'te yeni istemci record yok, tip tek yerde | ✅ | `denetim.sh` tavanı 129'da kaldı |
| Her düğme istek atar ya da disabled+sebep | ✅ | `denetim.sh` ölü düğme sayacı 71'de kaldı; bUnit 3 test |
| Tek düzenleme kalıbı (Cekmece) · ModalPencere yalnız yıkıcı onay | ✅ | § 2 + denetim gerekçesi |
| Test kiracısı silindi, tenants başlangıç değerine döndü | ✅ | 8 → 8 |
| 5199'a dokunulmadı, COMMIT yok | ✅ | git ağacı kirli bırakıldı, commit yok |

---

## 9. Açık kalanlar

1. **Karar E** — `KasaModulu.HareketGir` komisyon kusuru sürüyor (bakiyeye net,
   harekete brüt). Tamer Bey'in ayrı mührünü bekliyor.
2. **Kasa semantiği / gün kapanışı farkı** — ölçüldü (−10.000,00), düzeltilmedi.
   Paket 30 ile birlikte oylanmalı. Kuyumcu demoda bu farkı görebilir.
3. **Geriye dönük POS alacağı** — paket 31 öncesi kart kasa hareketleri için satır
   üretilmez. "Geçmiş POS alacağım nerede" sorusu **açık**; tek meşru yol elle giriş
   (ADR-0014 içe aktarım yasağıyla çelişmez) ve kapsam dışı bırakıldı.
4. **Resmî tatil takvimi yok** — `ValorIsGunu` yalnız haftasonu atlar; bayramda
   valör yanlış hesaplanır. Ekranda bu sınır **yazılıdır** (`Pos.TatilUyarisi`).
5. **Chargeback muhasebesi** — iptal edilen tahsilat bugün yalnız POS defterinde
   kalır; cari bakiyeye/satış hareketine **dokunmaz**. Paket 33 düzeltme fişiyle
   bağlanması ayrı karardır.
6. **Cihazın kasa bağı** düzenlenemez (yalnız oluşturmada verilir) — ekranda
   `DegismezIpucu` ile gerekçesi yazılı; gerekirse ayrı kalem.
7. **Pre-existing uyarı**: `EkstreEkranTestleri.cs` / `EkstreIceAktarmaTestleri.cs`
   içinde 5 adet `xUnit2031` uyarısı **paket 32'den devralındı**, bu pakette
   üretilmedi ve dokunulmadı.

## 10. Kurucuya / mali müşavire notlar

- **Komisyon oranı ve valör günü MEVZUAT DEĞİLDİR.** Bankayla yapılan üye işyeri
  sözleşmesinden gelir ve kuyumcudan kuyumcuya değişir. Koda **hiçbir sayı
  gömülmedi**; her cihazda kullanıcıdan alınır.
- **5464 sayılı Banka/Kredi Kartları Kanunu** (POS'tan nakit kullandırma yasağı)
  bu oturumda **DOĞRULANMADI**. Koda kural, ekrana uyarı metni **konmadı**.
  Doğrulanırsa ayrı bir uyum paketine (Katman 6) girer.
- **İadede komisyon iade edilmez** varsayılanı (`IadeKomisyonIadeEdilir=false`)
  yaygın teamüle dayanır ama **doğrulanmış bir hüküm değildir**; sözleşmesi farklı
  olan kuyumcu ayarı açar.
- **Kart satışı olan günde kasa sayım farkı** işletmenin gerçek bir sorusudur
  (§ 6). Muhasebe kararı verilene kadar kuyumcuya "kart kasası ayrı tutulsun"
  denebilir; kalıcı çözüm paket 30'dadır.
