# PAKET B-3: PEŞİN MÜŞTERİ SİSTEM CARİSİ (D2), FAIL-CLOSED. Uygulama raporu

Rol: Taşkın (backend) ve Ertuğ (frontend). Dalga S-B, 2026-09-23.
Durum: **BİTTİ (paket kapısı, 7/7)**. Tam süitte 7 kırmızı test var. Hiçbiri bu paketin değil. Sahipleri § 10'da yazılı.

Kaynaklar:
- `KARARLAR-D1-D11-2026-09-23.md`, D2 kararı: KOŞULLU, K1–K5.
- `PLAN-SADELESTIRME-PUSULA-2026-09-23.md`: § 3 md.6, S-2 madde 2.3 ve 2.4.
- Tasarım tuvali `tuval/project/Tezgah.dc.html`: `Müşteri [Peşin Müşteri ▾]` · "Borcu yok".

---

## 0. Spesifikasyonu koda karşı doğrulama (sapmalar ve gerekçeleri)

| İddia / madde | Kodda bulunan | Uyarlama |
|---|---|---|
| "`cariler.sistem_carisi` canlıda yok" | Doğru. Kodda grep 0 eşleşme. Canlıda ayrı bir `SHOW COLUMNS` ölçümü YAPILMADI; kanıt dolaylıdır: `AddColumn` migration'ı `kuyumcu_net` üzerinde hatasız uygulandı (kolon var olsaydı 1060 hatası verirdi). | Migration eklendi (§ 2). |
| Plan'daki "Birleşik koşullar" bölümü | Plan'da bu adda bir bölüm **YOK**. | Bağlayıcı koşullar olarak D2 kararının K1–K5 koşulları ile plan § 3 (sınırlar) ve S-2 madde 2.3/2.4 alındı. |
| K3: "Fark ≠ 0 kaydedilemez" kuralı `OdemeSepeti`'nde yazılacak | `OdemeSepeti.Coz` zaten **tam eşitlik** istiyordu: bacak toplamı ≠ fiş tutarı ise 400. Karma fişte açık kalan fark yalnız VERESİYE bacağıyla kapatılabiliyordu. | K3, Peşin Müşteri için "veresiye bacağı yasak" olarak yazıldı. Tam eşitlik kuralı bu kuralla birlikte "fark = 0" anlamına gelir. Bayrak **sunucuda ölçülür** (`sistemCarisi:` parametresi). Vezne dosyası değişmedi: `Coz`'a yeni parametre isteğe bağlı eklendi. Vezne yolu SaveChanges bakiye kilidiyle korunur (aşağıda). |
| "Kiracı tohumlama (mevcut ADR-0027 modül-başı tohumlama mekanizması)" | ADR-0027 deseni "gerekiyorsa tohumla" biçiminde tembel çalışır (`YetkiModulu.TohumlaGerekiyorsa`). | Kart ilk ihtiyaç anında açılır: `GET /api/v1/cari/sistem-carisi`. Migration mevcut kiracılara satır **yazmaz**: ULID sunucuda üretilir, mevcut veri de değişmez. Tekliği veritabanı kısıtı sağlar. |
| D2 K5: gevşeme "uyum belgesine" yazılır | Emsal (K4-D, evrak girişi): gevşeme `MasakKapisi.cs` içindeki "bilinen eksik ölçüm" notuna yazılmış. | Aynı desen izlendi. `MasakKapisi.DogrulaKumulatif` özetine ve `TevsikKapisi` KAPSAM KAYDI'nın 5. maddesine yazıldı. Ayrıca § 8'e yazıldı. |
| Canlı port | Görev metninin genel kısmı 5198 diyor, paket kısmı 5193 diyor. | Pakete özgü talimat esas alındı: **5193**. 5194–5200 portlarına dokunulmadı. |

**Mevzuat:** Bu paket yeni bir yasal hüküm **yazmadı**. Eşik sayıları mevcut yapılandırmadan okunur: MASAK eşiği `MasakEsikleri.KimlikEsigi`, tevsik eşiği `TevsikEsikleri.Had`. Karşılaştırma operatörleri mevcut kurallardan alındı: MASAK için `>=` (`motor.Gereksinim`), tevsik için `>` (`TevsikKurali`). Peşin Müşteri reddi bir iç politikadır ve yalnız sıkılaştırır: yasal eşiğe ulaşan işlemi kimliksiz kartta hiç kabul etmez. Bu yüzden WebSearch ile birincil kaynak araştırması gerekmedi. Mali müşavir notu § 11'de.

---

## 1. Yapılanlar

### Sunucu (Taşkın)
- **`Data/Entities.cs`:** `Cari.SistemCarisi` alanı eklendi (`sistem_carisi`, bool, NOT NULL, varsayılan 0).
- **`Modules/Cari/SistemCarisi.cs` (yeni):**
  - `MiAsync(db, tenant, cariId)` metodu. Kiracı şartını elle de yazar.
  - `GetirVeyaOlustur` metodu tohumlamayı yapar ve `DenetimServisi.YazmaKaydet` ile iz bırakır. Yarışta kısıt ikinci kaydı reddeder, kazanan kart okunur. Kazanan bulunamazsa hata yeniden fırlatılır.
  - Sunucu mesajları burada tanımlıdır. Hiçbiri eşik sayısı taşımaz. Eşik yalnız `EsikMesaji` çalışırken yapılandırmadan okunur.
  - `SistemCarisiDegistirilemez` istisnası: `ApiException(400)`.
- **`Modules/Masak/MasakKapisi.cs`:**
  - K1: `GunlukIslemToplam` Peşin kart için 0 döner.
  - K2: `DogrulaKumulatif`, Peşin kartta tutar MASAK kimlik eşiğine ulaşırsa 400 döner. Tutar tevsik haddini aşarsa da 400 döner. Kimlik bloğu verilmiş olsa bile işlem geçmez.
  - İsteğe bağlı yeni kurucu parametresi `TevsikEsikleri?`. Verilmezse varsayılan eşikler kullanılır. "Eşik yoksa kontrol yok" diye bir yol yazılmadı.
  - `SatisServisi` ve `CariServisi` yapılandırılmış `tevsikKurali.Esikler` değerini geçirir.
- **`Modules/Tevsik/TevsikKapisi.cs`:**
  - K1: `GunlukNakitToplam` Peşin kart için 0 döner.
  - K2: `Dogrula` metodu `max(nakit, fiş toplamı)` değeri hadde ulaşırsa 400 döner. Kural kapalıyken (`Etkin=false`) de uygulanır.
  - K5: KAPSAM KAYDI 5. madde.
- **`Modules/Satis/OdemeSepeti.cs`:** K3 için `Coz(..., bool sistemCarisi = false)`. Değer doğruysa VERESİYE bacağı 400 alır.
- **`Modules/Satis/SatisModulu.cs`:** Karma fiş bayrağı sunucuda ölçer. Masak kapısı tevsik eşiklerini alır.
- **`Modules/Cari/CariModulu.cs`:**
  - Yeni uç `GET /api/v1/cari/sistem-carisi`. Dönüş tipi `PesinMusteriKarti` (Contracts); anonim nesne değil.
  - Liste ve detay yanıtlarına `sistemCarisi` alanı eklendi.
  - `DeaktifEt` (DELETE) Peşin kartta 400 döner.
  - PATCH `aktif:false` Peşin kartta 400 döner.
  - PATCH ile kimlik alanı yazmak (vergiNo, kimlikTuru, dogumTarihi, uyruk) 400 döner.
  - PEP beyanını `true` yapmak 400 döner.
  - Ad ve not düzenlenebilir. PATCH "gövdede yoksa koru" sözleşmesi korunur.
- **`Data/KuyumcuDbContext.cs`:** `SistemCarisiniKoru` hem `SaveChanges` hem `SaveChangesAsync` içinde çalışır. Servisi atlayan her yolu keser:
  - Sistem carisi silinemez ve pasife alınamaz.
  - Bayrağı kaldırılamaz. Var olan bir cari sonradan sistem carisine çevrilemez.
  - Bakiyesi (TL, has, döviz) sıfırdan farklı olamaz.

  Bu kilit sayesinde Vezne, emanet, evrak ve tek etiketli VERESİYE yollarının hepsi Peşin karta borç yazamaz. Bunun için o dosyalara dokunmak gerekmedi.
- **`Kuyumcu.Contracts/Sayfalar/PesinMusteriSozlesmeleri.cs` (yeni):** `PesinMusteriKarti` tipi. Sunucu imzasında durduğu için kilitlidir, SozlesmeKapsam cırcırı artmadı.
  - Mevcut `CariVeri` (Islemler ve Cariler) tiplerine `SistemCarisi` alanı eklendi. Yeni istemci tipi açılmadı.

### Ekran (Ertuğ)
- **Sözlük:** `Kuyumcu.Core/I18n/PesinMusteriSozlugu.cs` (yeni). TR+EN metinler konumsal record içinde: eksik anahtar derleme hatası verir. İçinde eşik sayısı yok. Ortak `Sozlukler.cs` dosyasına dokunulmadı.
- **`Pages/Islemler.razor`:**
  - Satışta müşteri boşsa Peşin Müşteri ön seçilir. Kartın kimliği sunucudan gelir.
  - Satış dışı türe geçilince ön seçim kaldırılır; alışta satıcının kimliği sorulur.
  - Kaydetme sonrası sıfırlamada kart yeniden ön seçilir.
  - Kart seçiliyken `#islem-pesin-notu` gösterilir. Not sayı içermez.
  - Kart okunamazsa POS açık kalır ve `#islem-pesin-hata` bandı çıkar (UX-S28).
  - POST yolu değişmedi (plan § 3 md.5).
- **`Pages/Cariler.razor`:**
  - Peşin kart satırında "Pasife Al" düğmesi çizilmez. Yerine `#c-sistem-carisi-{id}` kilit rozeti çıkar; gerekçe `title` içindedir.
  - Düzenle çekmecesinde `#c-pasife-al` yerine `DegismezIpucu` (`#degismez-ipucu-SistemCarisi`) gösterilir: gerekçe ve doğru yol (UX-S25).
  - Düzenle düğmesi yerinde kalır; ad değiştirilebilir.
- **Dokunulan sayfalar (DoD EK 2, TEK DOKUNUŞ):** `Pages/Islemler.razor`, `Pages/Cariler.razor`.
- **Sahte API:** yeni bUnit testlerinde `/cari/sistem-carisi` ucu gerçekçi yanıt döner. `'{}'` varsayılanı kullanılmaz.

### Denetim (`dotnet/denetim.sh`, yeni blok `# >>> B-3`)
| Sayaç | Ölçüm | Neyi yakalar |
|---|---|---|
| `B-3: KAPI_EKSIK` = 0 | MasakKapisi ve TevsikKapisi dosyalarının her birinde en az 2 `SistemCarisi.MiAsync` çağrısı | K1/K2 kapısının sökülmesi |
| `B-3: KORUMA_EKSIK` = 0 | İki SaveChanges koruması, servisteki DeaktifEt kapısı, karma fişin K3 bayrağı | K3/K4 kapısının sökülmesi |
| `B-3: ESIK_ISTEMCIDE` tavan 4 (cırcır) | İstemcide `185.000 / 30.000` biçimli sayı satırları. Taban 4 satırın hepsi B-3 öncesinden var. | Eşiğin istemciye taşınması |
| `B-3: ESIK_SABIT_SUNUCUDA` = 0 | `SistemCarisi.cs` içinde eşik sayısı | Eşiğin koda sabit yazılması |

---

## 2. Migration

- Ad: `20260923174110_B3PesinMusteriSistemCarisi`.
- İçerik: `AddColumn sistem_carisi tinyint(1) NOT NULL DEFAULT 0` ve ham SQL.
- Ham SQL şudur: `CREATE UNIQUE INDEX UQ_cariler_tenant_sistem_carisi ON cariler (tenant_id, ((CASE WHEN sistem_carisi = 1 THEN 1 END)))`. Desen P05 ile aynıdır: model bu indeksi bildirmez, bu yüzden model farkı doğmaz. `Down` metodu önce indeksi, sonra kolonu siler.
- Üretilen dosyalar okundu. `.cs` 790 bayt; ham SQL eklendikten sonra büyüdü. `.Designer.cs` 235.777 bayt. `Up()` yalnız bu kolonu ekliyor; başka şeridin bekleyen model farkı yoktu.
- `kuyumcu_net` üzerinde uygulandı. `Applying migration '20260923174110_B3PesinMusteriSistemCarisi'. Done.` `__EFMigrationsHistory` = 70.
- `SHOW INDEX` iki parça gösteriyor: `tenant_id` ve `(case when (sistem_carisi = 1) then 1 end)`.
- Test ile kanıt: B3_02 aynı kiracıya ikinci sistem carisi eklemeye çalışır ve `DbUpdateException` alır.
- ⚠ **Prosedür sapması (Anayasa md.1):** `dotnet ef database update` komutunu `dotnet build Kuyumcu.sln` ile **aynı komut satırında** çalıştırdım. O an çözüm derlemesi **kırmızıydı**: `Kuyumcu.Tests/RaporlarEkranTestleri.cs` 3 × CS0117 `YasSatiriBos` hatası verdi. Bu, PARA şeridinin yarım işiydi. `ef` komutu `Kuyumcu.Api` projesini kendisi derledi ve bu derleme yeşildi ("Build succeeded"). `--no-build` kullanılmadı. Migration yalnız bu paketin kolonunu içerdiği için sonuç etkilenmedi. Ama "çözüm yeşil olmadan ef çalıştırma" kuralı harfiyen karşılanmadı. Sonraki derlemeler 0/0.

---

## 3. Test sayıları (önce / sonra)

| Koşum | Toplam | Başarılı | Başarısız | Kırmızı |
|---|---|---|---|---|
| Paket öncesi tam süit (20:31, `kuyumcu_test_b`) | **4237** | 4236 | 1 | `IkonTestleri.KenarCubugu_menu_rotalari_ikon_gecisinden_SONRA_da_ayni` (S-1 şeridi) |
| B-3 filtreli (`~PesinMusteri`) | 27 | 27 | 0 | — |
| Sözleşme ailesi + B-3 (`~PesinMusteri\|~SozlesmeKapsam\|~SozlesmeSekil\|~SozlesmeYansima`) | 62 | 62 | 0 | — |
| Paket sonrası tam süit (21:15, trx `scratchpad/trx-pB-3`) | **4361** | 4354 | 7 | § 10: hiçbiri B-3'e ait değil |
| Son değişiklikten sonra B-3 filtreli | 27 | 27 | 0 | — |

Son değişiklik P-08 ve döviz sayacı uyumu içindi: sabit adı değişti, bakiye karşılaştırması yeniden yazıldı. Davranış değişmedi.

Test sayısı düşmedi: 4237 → 4361. Bu paketin payı +27. Kalanı başka şeritlerin yeni testleridir. Hiçbir test silinmedi.

**Yeni testler (27):**
- `PesinMusteriTestleri` (DB, 16 test): B3_01…B3_16. Kapsam: tohumlama, teklik kısıtı, eşik altı satış, MASAK ve tevsik reddi, kural kapalıyken red, doğrudan kapı çağrıları, K1 havuz dışı ve gerçek cari ile karşılaştırma, karma fişte VERESİYE, fark ≠ 0, saf `OdemeSepeti`, tek etiketli VERESİYE, cari ucundan VERESİYE, DELETE ve PATCH yasağı, PATCH sözleşmesi, SaveChanges koruması, sıradan cari davranışının korunması.
- `PesinMusteriHttpTestleri` (HTTP, 5 test): B3H_01 401 · B3H_02 TEZGÂHTAR 200, camelCase ve liste bayrağı · B3H_03 DELETE 400 · B3H_04 PATCH aktif:false 400 ve ad 200 · B3H_05 satış fişi eşik üstü 400, eşik altı başarılı.
- `PesinMusteriEkranTestleri` (bUnit, 6 test): B3E_01 ön seçim ve POST gövdesinde `cariId` · B3E_02 notta sayı yok · B3E_03 alışta ön seçim taşınmaz · B3E_04 hata bandı · B3E_05 satırda Pasife Al yok, rozet var · B3E_06 çekmecede DegismezIpucu.

Ara koşumda (20:53) `SozlesmeKapsamTestleri` 2 test kırmızıya döndü. Sebep bu paketti: ilk sürümde istemci `Api.GetAsync<CariVeri>` kullanıyordu ve bu kilitsiz bir tipti. Düzeltme: sunucu `PesinMusteriKarti` Contracts tipini döndürür, istemci aynı tipi okur. Sonuçta 62/62 yeşil ve tam süitte bu iki test yeşil.

---

## 4. Mutasyon kanıtları

Yöntem şöyle işledi:
1. Kaynak dosya `scratchpad/mut-yedek/B-3/<kod>.<dosya>` altına kopyalandı.
2. `perl -0777 -pi` ile tek eşleşmeli değişiklik yapıldı. Betik eşleşme sayısını da doğrular.
3. Proje derlendi ve süzgeçli testler koşuldu: `--filter FullyQualifiedName~PesinMusteri`.
4. Yedek geri kopyalandı ve `cmp` ile birebir aynı olduğu kanıtlandı.

Son mutasyondan sonra temiz derleme alındı: 0 Hata, 0 Uyarı.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M-B3-1 (a) | MASAK tarafı K2: kimlik eşiği kapısı `if (false)` | B3_04, B3_07 | cmp=AYNI |
| M-B3-2 (a) | MASAK tarafı K2: tevsik haddi kapısı `if (false)` | B3_05 | cmp=AYNI |
| M-B3-3 (a) | Tevsik tarafı K2 `if (false)` | B3_06 | cmp=AYNI |
| M-B3-4 | K1: `GunlukNakitToplam` kısa devresi silindi | B3_08 | cmp=AYNI |
| M-B3-5 | K1: `GunlukIslemToplam` kısa devresi silindi | B3_08 | cmp=AYNI |
| M-B3-6 (b) | Servis `DeaktifEt` kapısı silindi (silinebilir) | B3_13 | cmp=AYNI |
| M-B3-7 (b) | `SaveChangesAsync` koruma çağrısı silindi | B3_15, B3_11, B3_12 | cmp=AYNI (bakiye düzeltmesinden sonra yeniden koşuldu, aynı sonuç) |
| M-B3-8 | K3: `OdemeSepeti` veresiye yasağı `if (false)` | B3_09, B3_10 | cmp=AYNI |
| M-B3-9 | K3: satış fişi bayrağı `sistemCarisi: false` | B3_09 | cmp=AYNI |
| M-B3-10 (b) | PATCH `aktif:false` kapısı silindi | B3_13 | cmp=AYNI |
| M-B3-11 | PATCH kimlik alanı kapısı etkisiz | B3_14 | cmp=AYNI |
| M-B3-12 | Ekran ön seçimi `if (false)` | B3E_01, B3E_02, B3E_03 | cmp=AYNI |
| M-B3-13 | Cariler satırı `@if (false)`: Pasife Al düğmesi geri geldi | B3E_05, B3E_06 | cmp=AYNI |
| M-B3-14 (b) | SaveChanges "Deleted" koruması `if (false)` | B3_15 | cmp=AYNI (yeniden koşuldu) |
| M-B3-17 | SaveChanges bakiye kilidi `.All(x => true)` | B3_15, B3_11, B3_12 | cmp=AYNI |
| M-B3-15 (c) | Eşik istemciye taşındı: `Islemler.razor` içine `private const decimal PesinEsik = 185_000m;` | **denetim sayacı** `✗ B-3: ESIK_ISTEMCIDE 5 (tavan 4 — ARTIŞ)`, HATA=1 | cmp=AYNI; sonra ✓ 4 |
| M-B3-16 | Kapı sökümü: MasakKapisi K1 satırı silindi | **denetim sayacı** `✗ B-3: KAPI_EKSIK 1 (masak=1 · tevsik=2)` | cmp=AYNI |

Görevin istediği üç mutasyonun karşılığı şudur:
- (a) Eşik kapısı kaldırıldığında test kırmızı: M-B3-1/2/3.
- (b) Sistem carisi silinebilir yapıldığında test kırmızı: M-B3-6/7/10/14.
- (c) Eşik istemciye taşındığında denetim sayacı kırmızı: M-B3-15.

Hayatta kalan mutasyon yok.

Kayıt dışı kalan (açıkça): migration'daki UNIQUE indeks mutasyona sokulmadı. Bunun için veritabanını yeniden kurmak gerekirdi. Yerine B3_02 testi kısıtın canlı olduğunu kanıtlar.

---

## 5. Denetim (`bash ./denetim.sh`)

- **Paket öncesi (20:36):** DENETİM BAŞARISIZ, tek ihlal `✗ P-08: HARETURU_DISI_SABIT 1 ≠ 0 — EVRAK_GIRIS listede yok`. Bu, başka şeridin işiydi.
- **Birinci son koşum (21:18):** iki ihlal çıktı ve **ikisi de bu paketindi**:
  - `✗ P-08: HARETURU_DISI_SABIT — MUSTERI listede yok`. Sebep: `SistemCarisi.cs` içindeki `const string Tur = "MUSTERI"`. P-08 deseni `Tur = "…"` biçimini hareket türü sabiti sayıyor. Düzeltme: sabitin adı `CariTuru` yapıldı.
  - `✗ Doviz kolonu atama 4 (beklenen 3)`. Sebep: SaveChanges korumasındaki `b.UsdBakiye == 0m` karşılaştırması. Sayacın deseni `UsdBakiye[ ]*[+-]?=` bunu atama sanıyor. Düzeltme: karşılaştırma dizi üzerinden `.All(x => x == 0m)` olarak yazıldı.
  - Düzeltmelerden sonra B-3 testleri 27/27 yeşil kaldı. M-B3-7, M-B3-14 ve yeni M-B3-17 yeniden koşuldu ve kırmızı döndü.
- **İkinci son koşum:** düzeltmelerden ve son temiz derlemeden sonra yapıldı. Sonuç: **DENETİM TEMİZ** (`DENETIM_EXIT=0`).

### 5a. Son denetim çıktısı (`scratchpad/pB-3/son-denetim2.log`)
```
  ✓ Doviz kolonu atama (yalnız DovizBakiye)       3
  ✓ P-08: HARETURU_DISI_SABIT                      0 (-eq 0)
── B-3: Peşin Müşteri sistem carisi (fail-closed) ──
  ✓ B-3: KAPI_EKSIK                                0 (masak=2 · tevsik=2 çağrı)
  ✓ B-3: KORUMA_EKSIK                              0
  ✓ B-3: ESIK_ISTEMCIDE                            4 (tavan 4)
  ✓ B-3: ESIK_SABIT_SUNUCUDA                       0
DENETİM TEMİZ.
```

---

## 6. Canlı doğrulama (5193, `kuyumcu_net`)

Sürücü: `scratchpad/pB-3/canli.sh` ve `canli.py`. Davet satırı doğrudan eklendi, parola rastgele üretildi. Hiçbir sır ekrana basılmadı.

İlk üç koşum betik fikstürü yüzünden eksik kaldı:
1. Yeni kiracının TL kasası yoktu. Satış 400 "kasa ID gerekli" döndü.
2. Alış, boş kasadan ödeme istedi ve 400 döndü.
3. Stok yoktu.

Sunucu üçünde de doğru biçimde reddetti. Dördüncü koşumda stok, sayım ucuyla (`PATCH /urunler/{id}/stok/{sube}`) açıldı. Kayıt oran sınırı saatte 3 istek olduğu için 5193 API'si bir kez yeniden başlatıldı (yalnız bu paketin PID'i).

Aşağıdaki tablo dördüncü koşumdur (kiracı `PAKETB3-TEST-CANLI4`). Yanıtlar okundu.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | POST /auth/kayit | 201 | 201 | jeton var |
| 2 | GET /kur/guncel | 200 + id | 200 | kur var |
| 3 | GET /cari/sistem-carisi (tohumlama) | 200 | 200 | ad=Peşin Müşteri, sistemCarisi=True, aktif=True |
| 4 | GET /cari/sistem-carisi (2. çağrı) | aynı id | 200 | ayni_id=True |
| 5–7 | cari ×2, ürün, kasa, sayım 10 | 201/200 | 201/201, 201, 200, 200 | fikstür |
| 8 | satış Peşin 10.000 NAKIT | 200 | **200** | kaydedildi |
| 9 | satış Peşin 25.000 NAKIT (aynı gün, K1) | 200 | **200** | kümülatif 35.000 sayılmadı; gerçek caride bu ikinci işlem tevsikte reddedilirdi (B3_08) |
| 10 | satış Peşin 186.000 HAVALE + tam kimlik | 400 | **400** | "Peşin Müşteri ile bu tutarda işlem yapılamaz: 186.000,00 TL, MASAK kimlik tespiti eşiğine (185.000,00 TL) ulaşıyor. Kimlik tespiti için gerçek müşteri seçin…" |
| 11 | satış Peşin 35.000 HAVALE | 400 | **400** | "… tevsik (459 s. VUK GT) eşiğine (30.000,00 TL) ulaşıyor …" |
| 12 | satış Peşin 1.000 VERESİYE | 400 | **400** | "Peşin Müşteri kartı borç ya da alacak taşıyamaz …" (SaveChanges kilidi) |
| 13 | fiş Peşin nakit + VERESİYE | 400 | **400** | "Peşin Müşteri'ye borç (veresiye / açık hesap) yazılamaz: fiş farkı sıfır olmalıdır …" |
| 14 | fiş Peşin fark ≠ 0 (6.000 / 10.000) | 400 | **400** | "Ödeme bacaklarının toplamı fiş tutarına eşit olmalıdır … (fark -4.000,00 TL)" |
| 15 | fiş Peşin nakit + kart + havale üçlü, fark 0 | 200 | **200** | kaydedildi |
| 16 | DELETE /cari/{peşin} | 400 | **400** | "… silinemez ve pasife alınamaz." |
| 17 | PATCH `{aktif:false}` | 400 | **400** | aynı mesaj |
| 18 | PATCH `{vergiNo}` | 400 | **400** | "… kimlik/vergi bilgisi … yazılamaz …" |
| 19 | PATCH `{ad:"Peşin Satış"}` | 200 | **200** | ad=Peşin Satış, sistemCarisi=True |
| 20 | PATCH ad geri | 200 | 200 | ad=Peşin Müşteri |
| 21 | GET /cari liste | sistemCarisi=true | 200 | sistemCarisi=True, tlBakiye=0.0 |
| 22 | Karşılaştırma: **gerçek** cari 186.000 HAVALE + kimlik | 200 | **200** | kural yalnız kartı daraltır, satışı yasaklamaz |

Plan S-2 madde 2.3'teki "nakit + eski altın + kart" üçlü bacak örneği bu pakette yok. Karma fişte ALTIN bacağı D3 kararıyla kurucuya bırakıldı. Üçlü bacak nakit + kart + havale ile kanıtlandı.

**SQL kanıtı** (`scratchpad/pB-3/canli-sql.txt`):
```
kiraci               ad             aktif  sistem_carisi
PAKETB3-TEST-CANLI4  Peşin Müşteri  1      1
-- kiracı başına sistem carisi sayısı: 4 test kiracısının her birinde 1
-- Peşin kartın defter satırları:
SATIS  NAKIT  10000.00  nakit 10000.00  veresiye 0.00
SATIS  NAKIT  25000.00  nakit 25000.00  veresiye 0.00
SATIS  KARMA  10000.00  nakit  5000.00  veresiye 0.00
-- cari_bakiyeler (Peşin): tl 0.00 · has 0.000000
-- denetim_kayitlari: EKLEME · SistemCarisi · "Peşin Müşteri sistem carisi tohumlandı (ilk ihtiyaç anında)"
-- masak_kimlik_tespitleri (Peşin): 0
```
Reddedilen işlemler (10–14, 16–18) deftere satır bırakmadı. Kartın bakiyesi 0.

**Temizlik:**
- 5193 API'si yalnız kendi PID'iyle durduruldu. `lsof -iTCP:5193 -sTCP:LISTEN` boş.
- Bu paketin açtığı 4 test kiracısı (`PAKETB3-TEST-CANLI`, `-CANLI2`, `-CANLI3`, `-CANLI4`) `tenant_id` kolonu taşıyan 70 tablonun tümünden silindi. Sonra `tenants` satırları ve `kiraci_davetleri` satırları (`isletme_adi LIKE 'PAKETB3-TEST-CANLI%'`) silindi.
- Kalan satır: 0. `tenants`: önce **10**, sonra **10**. Canlıdaki `cariler.sistem_carisi=1` sayısı: 0.
- `QA-TEST-` önekli 4 kiracıya dokunulmadı.

---

## 7. "Mevcut hâli bozulmasın" şartları

| # | Şart | Sonuç |
|---|---|---|
| 1 | `dotnet build Kuyumcu.sln` 0 hata | ✓ 0 Hata, 0 Uyarı (son derleme `son-build2.log`) |
| 2 | Tam süit ve test sayısı | Sayı 4237 → 4361, düşmedi ✓. 0 başarısız **ağaç ölçüsünde** sağlanmadı: 7 kırmızı var, hepsi başka şeridin (§ 10). PARALEL ŞERİT KAPISI'na göre bu paketin yeni kırmızısı 0 ✓ |
| 3 | `bash ./denetim.sh` | ✓ **DENETİM TEMİZ** (son koşum). Bu paketin yol açtığı 2 ihlal düzeltildi (§ 5). |
| 4 | Mevcut uç kaldırılmadı | `grep -rn "\[Http" Kuyumcu.Api --include=*.cs \| wc -l`: 369 → 373. B-3 payı +1 (`GET sistem-carisi`), kalan +3 Destek modülü (başka şerit). Envanterde silinen uç: **0** |
| 5 | DOM id değişmedi | Envanter id farkı: `/islemler` silinen=[] eklenen=[islem-pesin-hata, islem-pesin-notu]. `/cariler` statik id farkı yok; dinamik id 4 → 5 (`c-sistem-carisi-{id}`). `#c-pasife-al` ve `#c-pasife-al-{id}` sıradan cari için aynen duruyor (B3E_05). |
| 6 | Özellik kaybı 0 | Envanter (B-3 öncesi → sonrası) silinen öğe B-3 dosyalarında 0. Tek silinme `/giris` sayfasında (`altinGrad`, `parilti`, `tasGrad`). O sayfa S-1 şeridinin dosyası; B-3 dokunmadı. |
| + | Para yolu kapıları | Bu paket yeni bir para yolu açmadı. Mevcut satış yolu daraltıldı. `Hareketler.Add(` yalnız `HareketFabrikasi.cs` içinde; B-3 yeni kullanım eklemedi. |

---

## 8. Uyum kaydı: K1 gevşemesi ve karşılığı (K5)

- **Gevşeme:** kiracının "Peşin Müşteri" kartındaki satırlar MASAK bağlantılı işlem kümülatifine (`GunlukIslemToplam`) ve tevsik günlük nakit toplamına (`GunlukNakitToplam`) **girmez**. Gerekçe: kart bir kişi değildir. Havuzlansaydı günün kimliksiz müşterileri tek kişi sayılırdı ve eşik yanlış kişide patlardı.
- **Karşılık (fail-closed):** bu kartla yapılan işlem kendi başına MASAK kimlik eşiğine ulaşırsa ya da tevsik haddini aşarsa **reddedilir**. Ölçü, fiş toplamı ile nakit payının büyüğüdür. Kimlik bloğu verilmiş olsa da, tevsik kuralı kapalı olsa da red geçerlidir. Kart yalnız eşik altı işlem alır.
- **Kalan risk:** aynı kişi aynı gün bu kartla eşik altında birden çok işlem yaparsa bağlantılı toplam ölçülmez. Bu, bugünkü `cariId = null` yolunun sınırıyla aynıdır. Kişi kayıt altında olmadığı için ölçülemez.
- **Yazıldığı yerler:** `MasakKapisi.cs` içinde `DogrulaKumulatif` özeti; `TevsikKapisi.cs` KAPSAM KAYDI 5. madde; bu bölüm.

---

## 9. DoD sözleşmesi (düzenlenebilirlik)

| Varlık / kayıt | Uç | Ekran | Denetim izi | Salt okunur kısım ve gerekçe |
|---|---|---|---|---|
| Peşin Müşteri kartı (`cariler`, `sistem_carisi=1`) | `PATCH /api/v1/cari/{id}`: ad ve not düzenlenir, "gövdede yoksa koru" (B3_14). `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]`, `[ModulYetkisi("cari")]`. | Cariler → Düzenle (çekmece). Pasife al yerine `DegismezIpucu` ve kilit rozeti. | Tohumlama: `DenetimServisi.YazmaKaydet` (varlık `SistemCarisi`). PATCH: mevcut otomatik yazma izi. | **Silinemez / pasife alınamaz:** DELETE ve `aktif:false` 400 döner. Kart kimliksiz satışın tek yoludur; silinseydi POS ön seçimi ve kapı mantığı sahipsiz kalırdı. Doğru yol: kart yerinde kalır, adı değiştirilebilir. **Kimlik alanları ve PEP yazılamaz:** kimliği bilinen müşteri kendi kartını alır. |
| Kart bakiyesi (`cari_bakiyeler`) | Yok. Her zaman 0; SaveChanges kilidi. | Listede 0 görünür. | — | Finansal. Açık hesap yazılamaz (K3). Kimliksiz karta yazılan borç tahsil edilemez. |
| Peşin satış hareketleri (`hareketler`) | Mevcut storno yolu (48-A), değişmedi. | Mevcut işlem ekranları. | Mevcut. | Finansal kayıtta hard DELETE yok; B-3 bu yola dokunmadı. |
| `GET /api/v1/cari/sistem-carisi` | `[Authorize]`, `[ModulYetkisi("cari")]`; cari listesiyle aynı rol yüzeyi (tezgâhtar satış yapar). `[RolGerekli]` bilerek yok: liste ucuyla aynı. | POS ön seçimi. | Tohumlama anında. | Okuma ucudur. Kişisel veri taşımadığı için okuma izi düşmez. |

---

## 10. Açık kalanlar

**Tam süitteki kırmızılar** (21:15 koşumu, 7 test). Hiçbiri B-3 dosyalarına dokunmuyor. Devir notundaki tek kırmızı (`IkonTestleri…`) bu koşumda yeşildi.

| Test | Hata | Sahibi (dosyaya göre) |
|---|---|---|
| `S1eGirisTestleri.Her_dugme_ya_istek_atar_ya_da_gorunur_is_yapar` | `#giris-sifre-goster tıklanınca hiçbir şey olmadı` | S-1 şeridi (`Pages/Giris.razor`) |
| `S1eGirisTestleri.Goster_parolayi_GORUNUR_yapar_ve_GERI_gizler` | beklenen "text", gerçek "password" | S-1 şeridi (`Pages/Giris.razor`) |
| `S1cUstBarTestleri.S1c_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir` | CSS'te `overflow` bulundu | S-1 şeridi (`Layout/*`, css) |
| `RaporSetiTestleri.P48_01_TL_sifir_has_negatif_satir_CIKIS_doner` | CIKIS beklendi, GIRIS geldi | PARA şeridi, P-48 (`Modules/Rapor`) |
| `RaporSetiTestleri.P48_02_Zit_isaretli_satir_KARISIK_doner` | KARISIK beklendi, GIRIS geldi | PARA şeridi, P-48 |
| `RaporSetiTestleri.P48_03_Iki_bacak_sifirsa_yon_null_doner` | null beklendi, "GIRIS" geldi | PARA şeridi, P-48 |
| `RaporSetiTestleri.Islem_defteri_TL_isaretini_korur` | KARISIK beklendi, GIRIS geldi | PARA şeridi, P-48 |

Diğer açık kalanlar:
- Ara koşumda (20:53) `UX1C4EkranTestleri`, `SurumEkranTestleri`, `GorunumEkranTestleri` (Ayarlar sayfası, NullReference) ve `S3aEkranTestleri` (P-31 dosyası) kırmızıydı. Son koşumda yeşil. Başka şeritlerin ara izleriydi.
- **S-2 (basit mod fişi) ile birleşme:** VERDİM/ALDIM + FARK ızgarası ve "Açık hesap olarak kaydet" düğmesi S-2'nin işidir. B-3, sunucu kuralını ve mevcut POS'a ön seçimi verdi. S-2 fişi Peşin kartta "Açık hesap" düğmesini kilitli çizmeli ve sebebini yazmalı (UX-S30). Kural sunucudadır; bu yalnız görsel iştir.
- **Basit mod bandı:** Eşik üstündeki red, sunucunun 400 mesajıyla mevcut hata bandına düşer (canlıda doğrulandı, adım 10–11). Plan 2.4'ün istediği "gerçek müşteri seçin" metni bu mesajın içindedir. Ayrı bir istemci ön kontrolü **bilerek yazılmadı**: yazılsaydı eşik sayısı istemciye taşınırdı.
- **UNIQUE indeks mutasyonu** koşulmadı. Kısıt B3_02 testiyle kanıtlı.
- **Prosedür sapması** (§ 2): `ef database update` çağrısı, çözüm derlemesi kırmızıyken aynı komut satırında çalıştı. Api derlemesi yeşildi.

---

## 11. Kurucuya / mali müşavire notlar

1. **Peşin Müşteri eşik politikası iç kuraldır, mevzuat yorumu değildir.** Kart, MASAK kimlik eşiğine ulaşan ya da tevsik haddini aşan işlemi kabul etmez. Yasal eşiklerin kendisi değişmedi ve yapılandırmadan okunur. Şu soru mali müşavire sorulmalı: "Tevsik haddini aşan ama tamamı kartla/havaleyle ödenen perakende satışta alıcının kimliği zorunlu mu?" Cevap "hayır" olursa bu sıkılaştırma gevşetilebilir. Bugün tutucu (fail-closed) tarafta duruyor.
2. Kart "MUSTERI" türünde, adı "Peşin Müşteri". Adı Cariler ekranından değiştirilebilir. Kart silinemez.
3. Mevcut kiracılarda kart **ilk satış ekranı açılışında** oluşur. Migration mevcut veriye dokunmaz. Demo kiracısında (DEMO-Vitrin) kart ilk POS açılışında görünecek. Cariler listesinde bir satır artacak (demo senaryosu § sayılarını etkileyebilir; DEMO şeridine not).

---

## 12. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, B-3'ün dokunduğu dosyalar)

Yeni:
- `dotnet/Kuyumcu.Api/Modules/Cari/SistemCarisi.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/20260923174110_B3PesinMusteriSistemCarisi.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/20260923174110_B3PesinMusteriSistemCarisi.Designer.cs`
- `dotnet/Kuyumcu.Core/I18n/PesinMusteriSozlugu.cs`
- `dotnet/Kuyumcu.Contracts/Sayfalar/PesinMusteriSozlesmeleri.cs`
- `dotnet/Kuyumcu.Tests/PesinMusteriTestleri.cs`
- `dotnet/Kuyumcu.Tests/PesinMusteriHttpTestleri.cs`
- `dotnet/Kuyumcu.Tests/PesinMusteriEkranTestleri.cs`
- `dokumanlar/09-uygulama-kayitlari/PAKET-B-3-uygulama-raporu.md`

Değişen:
- `dotnet/Kuyumcu.Api/Data/Entities.cs`
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs`
- `dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Tevsik/TevsikKapisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Satis/OdemeSepeti.cs`
- `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs`
- `dotnet/Kuyumcu.Contracts/Sayfalar/IslemlerSozlesmeleri.cs`
- `dotnet/Kuyumcu.Contracts/Sayfalar/CarilerSozlesmeleri.cs`
- `dotnet/Kuyumcu.Web/Pages/Islemler.razor`
- `dotnet/Kuyumcu.Web/Pages/Cariler.razor`
- `dotnet/denetim.sh` (yalnız `# >>> B-3` bloğu eklendi)

Dokunulmayan yasak alanlar: Modules/Kasa, Vezne, Maliyet, Rapor · Raporlar / Gider / SiparisDetay / Siparisler / Faturalar / Panel / Kurulum / Giris / Ekranlar · Layout/* · belirtecler.css · app.css · Sozlukler.cs · Sozlukler.Marka.cs · betikler/*.sh.

Commit yapılmadı.

---

## 13. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| K1: havuz dışı | ✓ | B3_08; M-B3-4/5 kırmızı; canlı adım 9 |
| K2: eşik üstü 400, fail-closed | ✓ | B3_04/05/06/07; B3H_05; M-B3-1/2/3; canlı 10–11 |
| K3: fark ≠ 0 / açık hesap yok | ✓ | B3_09/10/11/12; M-B3-7/8/9/17; canlı 12–14 |
| K4: tek / silinemez / pasife alınamaz | ✓ | B3_01/02/13/15; B3H_03/04; M-B3-6/10/14; canlı 16–17; UNIQUE indeks |
| K5: gevşeme yazılı | ✓ | § 8, MasakKapisi ve TevsikKapisi notları |
| Eşik istemciye gömülmez | ✓ | B3E_02; denetim ESIK_ISTEMCIDE; M-B3-15 |
| Satışta ön seçili (taslak) | ✓ | B3E_01/03 |
| Düzenleme yolu / DegismezIpucu (UX-S25) | ✓ | B3E_05/06; PATCH ad canlı 19 |
| Hata yutulmaz (UX-S28) | ✓ | B3E_04 |
| Paket kapısı 7 madde (paralel şerit) | ✓ | build 0/0 · 27/27 · B-3 sayaçları ✓ · 17 mutasyon · canlı 22/22 · kiracı silindi · yeni kırmızı 0 |
