# PAKET S2-3: PEŞİN MÜŞTERİ BANDI + BAKİYE ROZETİ (fiş üst satırı). Uygulama raporu

Rol: Taşkın (backend) ve Ertuğ (frontend). Dalga S2 (Tezgâh fişi, Pusula formülü). Tarih: 2026-09-24 01:40.
Durum: **BİTTİ (paket kapısı, 7/7).** Tam süitte 4 kırmızı test var. Hiçbiri bu paketin değil; sahipleri § 10'da yazılı.

Spesifikasyon: `PLAN-SADELESTIRME-PUSULA-2026-09-23.md` § S-2, madde 2.4 (Peşin Müşteri) ve 2.6 (bakiye rozeti, Pusula F4). Taslak: `sadelestirme/tuval/project/Tezgah.dc.html`. Taslakta müşteri kutusunun içinde "Borcu yok" yazan bir rozet var.

---

## 0. Spesifikasyonu koda karşı doğrulama

| İddia / koşul | Kodda bulunan | Uyarlama |
|---|---|---|
| "B-3 kapıdan geçmiş olmalı" | `ORKESTRATOR-DURUM.md`, 2026-09-23 23:15 kaydı: **"B-3 (Peşin Müşteri) GEÇTİ"**. | Peşin Müşteri kısmı yapıldı. |
| 2.4 "satışta Peşin Müşteri ön seçili" | B-3 bunu `Islemler.razor` içinde **zaten yazmış** (`PesinOnSec`, B3E_01..04 testleri). | Yeniden yazılmadı. Bloğun işaretlemesi yeni bileşene taşındı; davranış aynı kaldı ve B3E testleri yeşil. |
| 2.4 "tevsik/MASAK eşiğinde SUNUCU 400" | B-3'te var: `MasakKapisi.DogrulaKumulatif` (kimlik ve tevsik dalları) ve `TevsikKapisi.Dogrula`. | Değişmedi. Yalnız red **alan işareti** taşıyacak hale geldi (aşağıda). |
| 2.4 "ekranda 'gerçek müşteri seçin' bandı" | Yoktu. Sunucunun 400 mesajı genel hata bandına düşüyordu (B-3 § 10 "Basit mod bandı"). | **Yazıldı.** Detay aşağıda. |
| 2.6 "Cari seçilince HAS + TL bakiyesi rozet (veri zaten yükleniyor)" | Kısmen doğru. `GET /cari?limit=200&aktif=true` yanıtı `tlBakiye`, `hasGramBakiye` ve döviz alanlarını taşıyor (`CariModulu.cs` satır 588-594). Ancak istemci tipi `Islemler.CariVeri` bu alanları **okumuyordu**: yalnız `(Id, Ad, SistemCarisi)` alanları vardı. | Tipe sunucuyla aynı adda ve tipte 5 alan eklendi (decimal). Yeni istek ya da yeni tip açılmadı. |
| "Birleşik koşullar" bölümü | Plan'da bu adda bir bölüm yok (B-3 raporu da aynı şeyi yazmıştı). | Bağlayıcı koşul olarak plan § 3 (sınırlar), § 5B (jargon 0, etiket ≤ 3 kelime) ve görev metnindeki EK KOŞULLAR alındı. |
| `Islemler.razor` ≤ 1.400 satır (S21 testi) | Paket öncesinde 1.397 satırdı; yalnız 3 satır pay kalmıştı (S2-2 § 10 A-1). | Müşteri bloğu `Bilesenler/Fis/FisMusteriSatiri.razor` bileşenine taşındı. Sonuç: **1.392 satır.** |

### Bandı açan sinyal: neden alan işareti?
Bandın koşulunu istemci **tahmin edemez**. Eşik sayısı istemciye gömülmez (plan § 3 md.6; B-3 `ESIK_ISTEMCIDE` cırcırı). Her 400'e "gerçek müşteri seçin" demek de yanlış olurdu: stok yetersizliği ya da fark ≠ 0 gibi başka sebeplerde kullanıcıyı yanlış yere yönlendirirdi. Sunucu mesajının metnini aramak da kırılgandır.

Çözüm, var olan P-05 / UX-4d "alan altı hata" sözleşmesini 400'e genişletmek oldu:
- Peşin eşik reddi artık gövdesinde `alanlar: { cariId: [mesaj] }` taşır.
- İstemcinin `ApiHata.Alanlar` ayrıştırıcısı bu sözleşmeyi zaten okuyordu. Değişmedi.
- Ekran bandı şu üç koşulun hepsi sağlanınca açılır: `400`, `alanlar` içinde `cariId` var ve seçili müşteri Peşin Müşteri.

İstisna tipi **değişmedi**. Red `BadRequestException` olarak kalıyor, çünkü mevcut B-3 testleri `Assert.ThrowsAsync<BadRequestException>` ile tam tip eşleşmesi arıyor. İşaret `Exception.Data` üzerinden taşınıyor (`AlanIsareti.AlanIle`). İşaretsiz istisnaların gövdesi birebir aynı kaldı (S23H_03 testi ve canlı adım 9 bunu kanıtlıyor). Nest alan sırası da korundu: message, alanlar?, error, statusCode, istekKimligi.

---

## 1. Yapılanlar

### Sunucu (Taşkın)
- **`Kuyumcu.Api/Common/ApiException.cs`:**
  - Yeni `AlanIsareti` statik sınıfı eklendi (`AlanIle<TEx>(alan)` ve `Alan(ex)`).
  - `HataYakalayici` içindeki `ApiException` dalı işaret varsa `alanlar` basıyor. İşaret yoksa `null` geçiyor ve gövde değişmiyor.
- **`Kuyumcu.Api/Modules/Masak/MasakKapisi.cs`:** Peşin kartın iki red satırına (MASAK kimlik ve tevsik) `.AlanIle("cariId")` eklendi.
- **`Kuyumcu.Api/Modules/Tevsik/TevsikKapisi.cs`:** Peşin red satırına `.AlanIle("cariId")` eklendi.
- Eşik sayıları, karşılaştırma operatörleri ve mesajlar **değişmedi**. Hepsi B-3'teki haliyle yapılandırmadan okunuyor.

### Sözleşme
- **`Kuyumcu.Contracts/Sayfalar/IslemlerSozlesmeleri.cs`:** `CariVeri` kaydına `TlBakiye`, `HasGramBakiye`, `UsdBakiye`, `EurBakiye`, `GbpBakiye` alanları eklendi. Hepsi `decimal`, varsayılanı 0; sunucudaki ad ve tiple aynı. Yeni tip açılmadı ve record tavanı 3/3'te kaldı.

### Ekran (Ertuğ)
- **Yeni `Bilesenler/Fis/FisMusteriSatiri.razor`** (fiş üst satırı).
  - `Islemler.razor`'daki müşteri bloğu buraya **birebir** taşındı. Taşınan id'ler aynı kaldı: `islem-cari`, `islem-cari-pasif-notu`, `islem-pesin-notu`, `islem-pesin-hata`.
  - Eklenenler:
    - `#islem-cari-bakiye` rozeti. Mevcut `.rozet` sınıfları kullanılıyor: yeşil "Borcu yok", sarı borç, mavi alacak. Rozetin `title` özniteliği "Müşterinin bugünkü durumu" (UX-S33).
    - `#islem-pesin-esik` bandı (`bant bant-uyari`, `role=alert`). Bant iki eylem taşır:
      - `#islem-pesin-esik-sec` "Müşteri seç" düğmesi: Peşin ön seçimini kaldırır, istek atmaz.
      - `#islem-pesin-esik-yeni` "Yeni müşteri" bağlantısı: `/cariler` sayfasını açar.
  - Bileşen istek atmaz ve ApiIstemci enjekte etmez (plan § 3 md.5). Bu kural S21 yapı testiyle ve denetim sayacıyla kontrol ediliyor.
- **Yeni `Bilesenler/Fis/FisBakiyeRozeti.cs`** (saf sınıf).
  - İşaret kuralı `Cariler.razor` ile aynıdır: artı değer "müşteri bize borçlu", eksi değer "müşterinin alacağı".
  - Rozet sırasıyla has gram, TL ve (sıfır değilse) USD, EUR, GBP gösterir.
  - Her tutar kendi birimiyle yazılır: `Bicim.Para` ve "g has" (UX-S32). Borç ve alacak aynı anda varsa ikisi ayrı ayrı yazılır.
- **`Pages/Islemler.razor`:**
  - Blok yerine tek bileşen çağrısı geldi.
  - `pesinKimlikGerekli` bayrağı eklendi. Kurulduğu tek yer `Kaydet()` catch dalıdır: `400`, `Alanlar["cariId"]` ve seçili müşteri Peşin.
  - Bayrak şu durumlarda iner:
    - her yeni `Kaydet()` denemesinde,
    - müşteri değişince (`MusteriDegisti`),
    - işlem türü değişince (`TurSec`),
    - "Müşteri seç" tıklanınca.
  - `CarileriTazele()` metodu eklendi. Başarılı kayıttan sonra cari listesi yeniden çekilir, böylece açık hesap satışından sonra rozet eski bakiyeyi göstermez. Tazeleme hatası yutulmaz, hata bandına yazılır (UX-S28).
  - `limit=200` ile cari isteği yapılan yer tek kaldı: açılış da aynı metodu çağırıyor. P-15 `DUZ_SELECT_LIMIT` sayacı arttırılmadı (§ 5).
  - **Tek POST yolu** değişmedi: `Kaydet()` yerinde.
- **Sözlük:** Yeni `Kuyumcu.Core/I18n/Sozlukler.FisMusteri.cs` dosyası (`FisMusteriSozlugu`) açıldı. TR ve EN metinler konumsal record içinde, bu yüzden eksik anahtar derleme hatası verir. Metinlerde rakam yok. Etiketler en çok 3 kelime: "Borcu yok", "Borcu", "Alacağı", "Müşteri seç", "Yeni müşteri". Ortak `Sozlukler.cs` dosyasına dokunulmadı.
- **CSS:** Yeni CSS yok. Mevcut belirteçli sınıflar kullanıldı (`.rozet-*`, `.bant-uyari`, `.rozet-satir`). Satır içi style ve sabit renk eklenmedi.
- **Dokunulan sayfalar (DoD EK 2, tek dokunuş):** `Pages/Islemler.razor`. Bu sayfa S2 şeridinin dosyasıdır; S2-1 ve S2-2'nin devamıdır, aynı dalganın sıralı paketidir.

### Denetim (`dotnet/denetim.sh`, yeni blok `# >>> S2-3`)
| Sayaç | Ölçüm | Neyi yakalar |
|---|---|---|
| `S2-3: ALAN_ISARETI_EKSIK` = 0 | MasakKapisi içinde en az 2, TevsikKapisi içinde en az 1 `AlanIle("cariId")` çağrısı; ara katmanda `AlanIsareti.Alan(ex)` | İşaretin sökülmesi (bandın bir daha açılmaması) |
| `S2-3: FIS_BILESENI_ISTEK` = 0 | `FisMusteriSatiri.razor` ve `FisBakiyeRozeti.cs` içinde enjeksiyon ya da `*.XxxAsync` istek çağrısı. Yorumlar sayılmaz. | Tek POST yolunun delinmesi |
| `S2-3: SOZLUK_SAYI` = 0 | `Sozlukler.FisMusteri.cs` metin satırlarında rakam | Eşik sayısının istemciye sızması |

---

## 2. Migration
**Gerekmedi.** Şema değişikliği yok. `cariler.sistem_carisi` B-3'te geldi; bakiye alanları mevcut `cari_bakiyeler` tablosundan okunuyor.

---

## 3. Test sayıları

| Koşum | Toplam | Başarılı | Başarısız | Kırmızılar |
|---|---|---|---|---|
| Paket öncesi tam süit (00:49, `kuyumcu_test_x`) | **4638** | 4633 | 5 | `S32MusterilerEkranTestleri.S32_08…`, `S3eKurKaynagiEkranTestleri` ×2, `UX4cEkranTestleri.M10…`, `YayinAcilisHttpTestleri.HazirUcu…` |
| S2-3 filtreli (`~S23`) | 20 | 20 | 0 | — |
| İlgili aileler (`~PesinMusteri`, `~S21`, `~S22`, `~B4AltinBacagi`, `~Sozlesme`, `~Masak`, `~Tevsik`, `~Karma`, `~BasitMod` …) | 455 | 455 | 0 | — |
| POS aileleri, son derlemeden sonra (`~S23`, `~PesinMusteri`, `~S21`, `~S22`, `~B4AltinBacagi`, `~Islem`, `~Pos`, `~Karma`, `~BasitMod`, `~Barkod`, `~Teklif`, `~Iade`) | 485 | 485 | 0 | — |
| **Son tam süit** (01:36, trx `scratchpad/trx-pS2-3/_Mac_2026-09-24_01_36_16.trx`) | **4670** | 4666 | 4 | § 10'da. Hiçbiri S2-3'ün değil |

**Yeni testler (20):**
- `S23FisMusteriEkranTestleri` (bUnit, 12 test):
  - S23E_01: rozet metni cari bakiyesiyle **aynı** (plan kabulü).
  - S23E_02: Peşin seçiliyken rozet "Borcu yok" ve yeşil.
  - S23E_03: alacak, karışık yön ve döviz birimleri.
  - S23E_04: müşteri boşken rozet yok.
  - S23E_05: alan işaretli 400 bandı açar; sunucu mesajı hata bandında kalır; bantta rakam yok.
  - S23E_06: "Müşteri seç" ön seçimi kaldırır ve istek atmaz.
  - S23E_07: alan işaretsiz 400 bandı açmaz.
  - S23E_08: gerçek caride alan işaretli 400 bandı açmaz.
  - S23E_09: kayıttan sonra rozet taze listeden okunur.
  - S23E_10: bant yalnız son denemeye aittir.
  - S23E_11: müşteri değişince bant iner.
  - S23E_12: işlem türü değişince bant iner.
- Aynı sınıfta saf testler (2):
  - S23_01: sözlükte TR ve EN var, boş anahtar yok, rakam yok, etiketler ≤ 3 kelime.
  - S23_02: rozetin yön ve birim kuralları.
- `S23PesinBandiHttpTestleri` (HTTP, 6 test):
  - S23H_01: karma fişte MASAK eşiği üstü → 400 ve `alanlar.cariId` (mesajla aynı; `error` = "Bad Request").
  - S23H_02: tek kalem satışta tevsik eşiği üstü → 400, `alanlar.cariId` var ve deftere satır yazılmadı.
  - S23H_03: alan işaretsiz red (fark ≠ 0) → gövdede `alanlar` yok.
  - S23H_04: gerçek caride Peşin reddi yok.
  - S23H_05: `/cari` yanıtı istemci tipine bakiyeyle ayrışıyor ve rozet metni aynı bakiyeden üretiliyor.
  - S23H_06: `TevsikKapisi` doğrudan çağrıldığında da red `cariId` işaretli.

Test sayısı 4638'den 4670'e çıktı. S2-3'ün payı +20; kalan +12 başka şeritlerin yeni testleri. Hiçbir test silinmedi.

---

## 4. Mutasyon kanıtları

Yöntem:
1. Dosya `scratchpad/mut-yedek/S2-3/` altına kopyalandı.
2. Python ile tek eşleşmeli değişiklik yapıldı. Eşleşme sayısı 1 değilse betik durur.
3. `dotnet build` ile derlendi (0 Hata), ardından filtreli test koşuldu.
4. Yedek geri kopyalandı ve `cmp` ile birebir aynı olduğu kanıtlandı.

Betik: `scratchpad/pS2-3/mut.sh`. Son mutasyondan sonra temiz derleme alındı: **0 Uyarı, 0 Hata** (`build-son2.log`).

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M-S23-1 | Ara katman işareti okumaz (`string? alan = null;`) | S23H_01, S23H_02 | cmp=AYNI |
| M-S23-2 | MasakKapisi, MASAK kimlik reddinden `.AlanIle` silindi | S23H_01 | cmp=AYNI |
| M-S23-3 | MasakKapisi, tevsik reddinden `.AlanIle` silindi | S23H_02 | cmp=AYNI |
| M-S23-4 | TevsikKapisi, Peşin reddinden `.AlanIle` silindi | **İlk koşumda hayatta kaldı** (bkz. alttaki not) → S23H_06 eklendi → S23H_06 kırmızı | cmp=AYNI |
| M-S23-5 | Bant koşulundan `Alanlar.ContainsKey("cariId")` silindi | S23E_07 | cmp=AYNI |
| M-S23-6 | Bant koşulundan `cariId == pesinCariId` silindi | S23E_08 | cmp=AYNI |
| M-S23-7 | `Kaydet()` başındaki bayrak sıfırlaması silindi | S23E_10 | cmp=AYNI |
| M-S23-8 | `MusteriDegisti` sıfırlaması silindi | S23E_11 | cmp=AYNI |
| M-S23-9 | `TurSec` sıfırlaması silindi | S23E_12 | cmp=AYNI |
| M-S23-10 | "Müşteri seç" `cariId`'yi boşaltmıyor | S23E_06 | cmp=AYNI |
| M-S23-11 | Kayıt sonrası `CarileriTazele()` çağrısı silindi | S23E_09 | cmp=AYNI |
| M-S23-12 | Rozet TL parçası yanlış alanı okuyor (`UsdBakiye`) | S23E_01, S23E_03, S23E_09, S23_02, S23H_05 | cmp=AYNI |
| M-S23-13 | Borç yönü ters çevrildi (`> 0` yerine `< 0`) | S23E_01, S23E_03, S23E_09, S23_02 | cmp=AYNI |
| M-S23-14 | "Borcu yok" koşulu alacağı yok sayıyor | S23E_03, S23_02 | cmp=AYNI |
| M-S23-15 | Bileşen bandı hiç çizmiyor (`@if (false)`) | S23E_05, S23E_06, S23E_10, S23E_11, S23E_12 | cmp=AYNI |
| M-S23-16 | (denetim) MasakKapisi işareti söküldü | `✗ S2-3: ALAN_ISARETI_EKSIK 1 (masak=1 ≥2 …)` | cmp=AYNI |
| M-S23-17 | (denetim) Fiş bileşenine `@inject ApiIstemci Api` eklendi | `✗ S2-3: FIS_BILESENI_ISTEK 1` | cmp=AYNI |
| M-S23-18 | (denetim) Sözlüğe "185.000 TL" yazıldı | `✗ S2-3: SOZLUK_SAYI 1` | cmp=AYNI |

**M-S23-4 notu (test açığı kapatıldı):** Satış yolunda MASAK kapısının tevsik dalı, TevsikKapisi'nin Peşin dalından **önce** patlar. Bu yüzden HTTP testleri TevsikKapisi'nin işaretini görmüyordu. S23H_06 eklendi: kapıyı doğrudan çağırır (nakit payı haddin altında, fiş toplamı üstünde) ve işareti ölçer. Mutasyon tekrarlandı ve kırmızı döndü.

**M-S23-17 notu (sayaç düzeltildi):** Sayacın ilk sürümü yorumdaki "ApiIstemci bilmez" cümlesini de sayıyordu ve taban ölçümü 2 çıktı. Ölçüt kod desenine (enjeksiyon ya da `.XxxAsync` çağrısı) daraltıldı. Taban 0, mutasyonla 1.

**Bileşen tarafındaki çift koruma kaldırıldı:** Bileşenin `KimlikGerekli && PesinSecili` koşulu, sayfadaki `cariId == pesinCariId` koşulunun kopyasıydı. Bu kopya M-S23-6 mutasyonunu maskeliyordu. Kural tek yerde (sayfada) bırakıldı, mutasyon kırmızı döndü.

Hayatta kalan mutasyon yok.

---

## 5. Denetim (`bash ./denetim.sh`)
- **İlk son koşum:** DENETİM BAŞARISIZ. Tek ihlal şuydu: `✗ P-15: DUZ_SELECT_LIMIT ölçülen 13 · tablo 12`. **Bu paketin hatasıydı:** `CarileriTazele` metodu `limit=200` adresini ikinci kez yazdı. Düzeltme: açılış da `CarileriTazele()` metodunu çağırıyor, adres tek yerde duruyor. Yorumdaki kopya da silindi.
- **Son koşum (`scratchpad/pS2-3/denetim2.log`, EXIT=0):**
```
  ✓ P-15: DUZ_SELECT_LIMIT                         açık 9 (tavan 9) · toplam 12 = tablo 12
  ✓ Pages+Bilesenler+Layout record tanımı        3 (tavan 3)
  ✓ Pages: @onclick'siz ve disabled'sız <button>  70 (tavan 71)
── S2-3: Peşin Müşteri bandı + bakiye rozeti ──
  ✓ S2-3: ALAN_ISARETI_EKSIK                       0 (masak=2 · tevsik=1)
  ✓ S2-3: FIS_BILESENI_ISTEK                       0
  ✓ S2-3: SOZLUK_SAYI                              0
DENETİM TEMİZ.
```
B-3 sayaçları da yeşil. `ESIK_ISTEMCIDE` 4 (tavan 4): bu paket istemciye eşik sayısı eklemedi.

---

## 6. Canlı doğrulama (5192, `kuyumcu_net`)

- API, derlenmiş `bin` kopyasından çalıştırıldı (`scratchpad/pS2-3/api-bin`, PID 94016, `--urls http://localhost:5192`). Böylece paralel şeritlerin derlemesi çalışan süreci bozmadı.
- Sürücü: `scratchpad/pS2-3/canli.sh` ve `canli.py`. Davet satırı doğrudan eklendi, parola rastgele üretildi, hiçbir sır basılmadı.
- Yanıt gövdeleri okundu. Aşağıdaki tablo `canli-cikti.txt` dosyasındandır. Kiracı: `PAKETS23-TEST-CANLI`.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | POST /auth/kayit | 201 | 201 | jeton var |
| 2 | GET /kur/guncel | 200 + id | 200 | kur var |
| 3 | GET /cari/sistem-carisi (ön seçim kartı) | 200 | 200 | ad=Peşin Müşteri, sistemCarisi=True |
| 4 | POST /cari + POST /urunler + kasa + stok 20 | 201/201, 200, 200 | 201/201, 200, 200 | fikstür |
| 6 | **Peşin 186.000 TL HAVALE, tek ödeme (MASAK eşiği üstü)** | **400 + alanlar.cariId** | **400** | `alanlar.cariId=VAR (mesajla aynı)` · "Peşin Müşteri ile bu tutarda işlem yapılamaz: 186.000,00 TL, MASAK kimlik tespiti eşiğine (185.000,00 TL) ulaşıyor. Kimlik tespiti için gerçek müşteri seçin …" |
| 7 | Peşin 186.000 TL karma fiş (havale + kart) | 400 + alanlar.cariId | **400** | `alanlar.cariId=VAR (mesajla aynı)` · aynı mesaj |
| 8 | Peşin 35.000 TL HAVALE (tevsik haddi üstü) | 400 + alanlar.cariId | **400** | `alanlar.cariId=VAR (mesajla aynı)` · "… tevsik (459 s. VUK GT) eşiğine (30.000,00 TL) ulaşıyor …" |
| 9 | Peşin fark ≠ 0 (alan işaretsiz red) | 400, alanlar yok | **400** | `alanlar.cariId=YOK` · "Ödeme bacaklarının toplamı fiş tutarına eşit olmalıdır …" |
| 10 | Peşin 10.000 TL NAKİT (eşik altı) | 200 | **200** | kaydedildi |
| 11 | **Gerçek** cari 186.000 TL HAVALE, kimlik bilgisi yok | 400, alanlar.cariId yok | **400** | `alanlar.cariId=YOK` · "MASAK: 185.000 TL ve üstü işlemde kimlik tespiti zorunludur …". Band gerçek caride açılmaz. |
| 12 | Gerçek cari 12.500 TL VERESİYE | 200 | **200** | bakiye oluştu |
| 13 | GET /cari (rozetin okuduğu liste) | 200 | 200 | gerçek cari: tlBakiye=12500.0, hasGramBakiye=0.0 · Peşin: 0.0 / 0.0 |

**Bandın görünürlüğü:** Tarayıcıda oturum açıp adım adım denenmedi, çünkü parola girmek bu ajana yasaktır. Bunun yerine şu iki kanıt birleşiyor:
- (a) Canlı 400 gövdesi, bUnit testlerinin kullandığı gövdeyle **aynı şekli** taşıyor: `message`, `alanlar.cariId[0]` = message, `error` = "Bad Request", `statusCode` = 400 (adım 6-8).
- (b) Bu gövde gerçek `ApiIstemci` ayrıştırıcısından geçtiğinde `#islem-pesin-esik` bandı çiziliyor (S23E_05). Alan işaretsiz ya da gerçek caride gelen gövde bandı açmıyor (S23E_07, S23E_08).

Kurucunun 5199 inceleme sunucusunda elle bakılabilir.

**SQL kanıtı** (`scratchpad/pS2-3/canli-sql.txt`):
```
kiraci               ad             sistem_carisi  tl_bakiye  has_gram_bakiye
PAKETS23-TEST-CANLI  Peşin Müşteri  1              0.00       0.000000
PAKETS23-TEST-CANLI  S23 Müşteri    0              12500.00   0.000000
-- defter (hareketler):
Peşin Müşteri  SATIS  NAKIT     10000.00
S23 Müşteri    SATIS  VERESIYE  12500.00
```
Reddedilen işlemler (6, 7, 8, 9, 11) deftere satır bırakmadı. API'deki `tlBakiye` (12500.0), `cari_bakiyeler.tl_bakiye` (12500.00) ile aynı. Rozet de bu değeri okuyor.

**Temizlik:**
- 5192 API'si yalnız kendi PID'iyle (`kill 94016`) durduruldu. `lsof -iTCP:5192 -sTCP:LISTEN` boş döndü.
- `PAKETS23-TEST-CANLI` kiracısı silindi: önce `tenant_id` kolonu taşıyan 70 tablonun tümünden (`FOREIGN_KEY_CHECKS=0` oturumunda), sonra `tenants` ve `kiraci_davetleri` satırından.
- Kalan satır: **0**. `tenants` önce **10**, sonra **10**. `QA-TEST-` önekli 4 kiracıya dokunulmadı.
- Not: API açılışında günlükte `is_calismalari` yinelenen anahtar hataları (`MASAK_SIB_SURESI`, `SAKLAMA_ONIZLEME`, `VADE_HATIRLATMA`, 2026-09-24) görüldü. Sebep, aynı günün arka plan işlerinin başka portlardaki API'lerce zaten koşulmuş olması. Bu paketle ilgisi yok ve doğrulamayı etkilemedi.

---

## 7. DoD sözleşmesi (düzenlenebilirlik)

Bu paket **yeni varlık ya da kayıt türü açmadı**.

| Öğe | Düzenleme yolu | Gerekçe |
|---|---|---|
| Bakiye rozeti (`#islem-cari-bakiye`) | Salt okunur **gösterimdir**. | Bakiye defterden türetilir ve elle düzeltilemez (finansal). Doğru düzeltme yolu mevcut tahsilat, düzeltme fişi ve storno (48-A) yollarıdır. Peşin kartın bakiyesi her zaman 0 (B-3 SaveChanges kilidi). |
| "Gerçek müşteri seçin" bandı | Bant bir sonuç bildirimidir. İki eylemi gerçek iş yapar: "Müşteri seç" seçimi boşaltır, "Yeni müşteri" `/cariler` sayfasını açar. | DoD EK (1) karşılandı: her düğme ya iş yapıyor ya da istek atıyor. Bunlar S23E_06 ve S23E_05 testleriyle kanıtlandı. |
| Peşin Müşteri kartı | B-3'te tanımlandı: PATCH (ad, not), DELETE ya da pasife alma 400, DegismezIpucu. | Değişmedi. |

---

## 8. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| B-3 kapıdan geçti mi? Ölç. | ✓ | ORKESTRATOR-DURUM 23:15 "B-3 GEÇTİ" |
| Satışta Peşin Müşteri ön seçili | ✓ | B-3'ten korundu: B3E_01..03 yeşil; S23 `Ekran()` her testte `pm1` seçili |
| Tevsik/MASAK eşiğinde sunucu 400 | ✓ | canlı 6, 7, 8 → 400 |
| Ekranda "gerçek müşteri seçin" bandı | ✓ | S23E_05 ve S23E_06; canlı gövde şekli aynı |
| Eşik sayısı istemciye gömülmez | ✓ | `SOZLUK_SAYI` 0 · B-3 `ESIK_ISTEMCIDE` 4/4 · bant metninde `\d` yok (S23E_05) · sözlük testi |
| Cari seçilince HAS + TL rozeti; veri zaten yüklü | ✓ | S23E_01; yeni istek yok, yalnız kayıttan sonra tazeleme |
| **KABUL:** canlı 185.000 TL üstü Peşin → 400 ve bant görünür | ✓ | canlı 6 (186.000) → 400 + `alanlar.cariId`; bant S23E_05 |
| **KABUL:** bUnit rozet metni cari bakiyesiyle aynı | ✓ | S23E_01 (`FisBakiyeRozeti.Olustur(c1)` == DOM metni; `Bicim.Tl(38900)`, "5,660 g has") ve S23H_05 (gerçek HTTP yanıtı) |
| Tek POST yolu; fiş bileşeni ApiIstemci bilmez | ✓ | `FIS_BILESENI_ISTEK` 0 · S21 yapı testi yeşil · S23E_06 POST sayısı değişmedi |
| Mevcut DOM id'leri değişmez | ✓ | Taşınan 4 id bileşende aynen duruyor (bu id'leri arayan B3E, S22 ve S21 testleri yeşil). Yeni id'ler `islem-` önekli: `islem-cari-bakiye`, `islem-pesin-esik`, `-sec`, `-yeni`. Not: git tabanı ilk commit olduğu için `git diff`, önceki paketlerin taşımalarını da "silinen id" gösteriyor. Ölçüt, render edilen DOM'daki id'lerdir. |
| Özellik silinmez | ✓ | Pasif cari notu, Peşin notu ve Peşin hata bandı aynen duruyor; pro ve basit modda aynı satır |
| Sabit renk, satır içi style, CDN yok | ✓ | Yeni CSS yok; yalnız mevcut belirteçli sınıflar |
| Jargon 0, etiket ≤ 3 kelime | ✓ | S23_01 testi; "bakiye", "cari", "tevsik" kelimeleri rozette ve bantta yok |
| UX-S28: hata "kayıt yok"a çevrilmez | ✓ | Sunucu mesajı `#hata-bandi` içinde kalıyor (S23E_05). Tazeleme hatası da banda yazılıyor. |
| UX-S30/S27 | ✓ | Bant sebebi yazıyor; başarılı kayıtta bant kapanıyor ve rozet tazeleniyor (S23E_09, S23E_10) |
| `Islemler.razor` ≤ 1.400 satır | ✓ | 1.392 satır (S21 testi yeşil) |
| İzolasyon: `KUYUMCU_TEST_DB=kuyumcu_test_x`, port 5192, `build-server shutdown` | ✓ | Tüm koşumlar bu ayarlarla yapıldı |
| Sözlük için ayrı dosya | ✓ | `Sozlukler.FisMusteri.cs` |
| Paket kapısı (7): yeni kırmızı üretmedi | ✓ | § 10 |

---

## 9. Dosya alanı sapması (gerekçeli)

Görevin S2 dosya alanı şudur: `Pages/Islemler.razor`, `Bilesenler/Fis/*`, `Servisler/BasitPos.cs`, `Bilesenler/BasitSatisAkisi.razor`. Bu paket alan dışında şu dosyalara da dokundu:

1. **`Kuyumcu.Api/Common/ApiException.cs`** ve **`Modules/Masak/MasakKapisi.cs`**, **`Modules/Tevsik/TevsikKapisi.cs`**. Bandın tek güvenilir sinyali sunucudan gelmek zorunda (§ 0). Alternatiflerin ikisi de kurala aykırıydı: istemcide eşik sayısı tutmak (yasak) ya da mesaj metnini aramak (kırılgan).
   - Değişiklik eklemelidir: iki dosyada 3 satırın sonuna `.AlanIle("cariId")` eklendi; ara katmana işaret okuma eklendi.
   - Eşzamanlı şeritlerin yasaklı alanı `Modules/Satis` ve `Modules/Cari` idi. Bunlara **dokunulmadı**. `Modules/Masak`, `Modules/Tevsik` ve `Common` hiçbir şeridin ilan edilmiş alanında değil.
   - B-4 (S-B şeridi) MasakKapisi'ne dokunursa birleşmede bu üç satır korunmalı. `S2-3: ALAN_ISARETI_EKSIK` sayacı sökülmeyi yakalar.
2. **`Kuyumcu.Contracts/Sayfalar/IslemlerSozlesmeleri.cs`**. `Islemler.razor`'ın kendi sözleşme dosyasıdır. Değişiklik: `CariVeri` tipine sunucuyla aynı 5 alan eklendi.
3. **`dotnet/denetim.sh`**. Dosyanın sonuna, özet satırından önce `S2-3` bloğu eklendi. Diğer bloklara dokunulmadı.

---

## 10. Açık kalanlar

**Son tam süitteki kırmızılar** (01:36; 4 test). Hiçbiri S2-3 dosyalarına dokunmuyor.

| Test | Hata | Paket öncesi (00:49) | Sahibi |
|---|---|---|---|
| `UX4cEkranTestleri.M10_Ayar_listesi_UC_EKRANDA_TEK_KAYNAKTAN_beslenir` | `Emanet` ekranı `e-ayar` seçeneklerinde "K995" yok | **vardı** | S3-B (`Pages/Emanet.razor`) |
| `UrunEkranTestleri.Stok_modali_GERCEK_yanit_seklinde_satirlari_cizer_hata_vermez` | Stok modalında "s1" bulunamadı | yoktu | S3-A (`Pages/Urunler.razor`, 01:29'da değişti) |
| `UrunEkranTestleri.Stok_modali_ONDALIK_adeti_cozer_ve_satiri_cizer` | aynı | yoktu | S3-A (`Pages/Urunler.razor`) |
| `YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner` | `Unknown database 'kuyumcu_t1_hazir'` (test VT altyapısı) | **vardı** | Yayın / altyapı (P-43/P-49 ailesi); eşzamanlı şeritlerin geçici VT'si |

**Paket kapısı madde (7):** son liste ile paket öncesi liste arasındaki fark `UrunEkranTestleri.Stok_modali_*` ×2 testidir. İkisi de `Urunler.razor` dosyasındadır. Bu dosya S3-A şeridinin alanıdır; S2-3 dosyaya dokunmadı ve dosya S2-3 koşarken 01:29'da değişti. Ara koşumda (01:26) aynı dosyadan 46 kırmızı vardı (`ArayuzModuServisi` kaydı yok); S3-A'nın yarım iziydi, son koşumda 2'ye indi. **S2-3'ün ürettiği yeni kırmızı: 0.**

Diğer açık kalanlar:
- **Tarayıcıda uçtan uca bant gözlemi** yapılmadı. Gerekçe: parola ile oturum açmak ajana yasak. Kanıt zinciri § 6'da. Kurucu 5199'da şu adımlarla bakabilir: Peşin Müşteri, 186.000 TL havale, Kaydet.
- **Peşin kartta veresiye reddi** (`OdemeSepeti`, `Modules/Satis`) alan işaretsiz kaldı, çünkü dosya S-B alanında. Ekran zaten "Açık hesap" düğmesini Peşin'de kilitliyor (S2-2), yani bu 400'e kullanıcı normal akışta ulaşmaz. İstenirse S-B şeridi `VeresiyeYasakMesaji` reddine `.AlanIle("cariId")` ekleyebilir. Tek satırdır.
- **Rozetin döviz satırı:** yalnız sıfırdan farklı döviz bakiyesi yazılır. Toplam 5 birim uzunsa rozet satırı kaydırır (`.rozet-satir` flex-wrap). Taslakta tek birimli örnek var; gerçek veride uzun satır görülürse S-1 (app.css) tarafına görsel iş düşer.
- Proje ağacında bu paketin olmayan bir yedek dosyası duruyor: `dotnet/denetim.sh.mutbak` (18 Eylül 16:49). Silme kararı kurucuya ya da orkestratöre bırakıldı.
- **Mevzuat:** Bu paket hiçbir mevzuat hükmü yazmadı ya da değiştirmedi. Eşikler, operatörler ve mesajlar B-3'teki haliyle yapılandırmadan okunuyor. Paket yalnız mevcut reddin **hangi form alanına ait olduğunu** işaretliyor. Bu yüzden birincil kaynak araştırması gerekmedi.

---

## 11. Kurucuya / mali müşavire notlar
1. Yeni bir yasal soru doğmadı. B-3'ün § 11 notu geçerli: "Tevsik haddini aşan ama tamamı kartla/havaleyle ödenen perakende satışta alıcının kimliği zorunlu mu?" Cevap gelene kadar Peşin kart tutucu tarafta kalıyor. Bant bu durumda tezgâhtarı gerçek müşteriye yönlendiriyor.
2. Bakiye rozeti **bugünkü** durumu gösterir. Dönem seçicisi yoktur, rozetin `title` özniteliğinde bu yazılıdır. Rozet, satış ekranının müşteri listesi (ilk 200 aktif müşteri) ile aynı veriden okunur; sınır mevcut `#islem-cari-pasif-notu` notunda yazılıdır.

---

## 12. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` ile ağaç ortak olduğu için yalnız S2-3'ün dokunduğu dosyalar listelendi.

Yeni:
- `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisMusteriSatiri.razor`
- `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisBakiyeRozeti.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.FisMusteri.cs`
- `dotnet/Kuyumcu.Tests/S23FisMusteriEkranTestleri.cs`
- `dotnet/Kuyumcu.Tests/S23PesinBandiHttpTestleri.cs`
- `dokumanlar/09-uygulama-kayitlari/PAKET-S2-3-uygulama-raporu.md` (bu dosya)

Değişen:
- `dotnet/Kuyumcu.Web/Pages/Islemler.razor` (1.397 → 1.392 satır)
- `dotnet/Kuyumcu.Api/Common/ApiException.cs` (git: ` M`)
- `dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs` (dizin git'te `??`)
- `dotnet/Kuyumcu.Api/Modules/Tevsik/TevsikKapisi.cs` (dizin git'te `??`)
- `dotnet/Kuyumcu.Contracts/Sayfalar/IslemlerSozlesmeleri.cs`
- `dotnet/denetim.sh` (`# >>> S2-3` bloğu)

Commit yapılmadı. Proje ağacında bu paketin bıraktığı `.bak`, `.tmp` ya da mutasyon kalıntısı yok; yedekler `scratchpad/mut-yedek/S2-3/` altında.
