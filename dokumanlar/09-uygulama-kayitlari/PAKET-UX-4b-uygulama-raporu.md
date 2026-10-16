# PAKET UX-4b — Mantık ve tutarlılık düzeltmeleri (madde 6 · 7 · 8 · 9)

> Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) · Dalga 2 · 2026-09-16
> Kapsam: YALNIZ madde 6, 7, 8, 9 (sıra 6 → 7 → 8 → 9). Madde 1-5 UX-4a'da bitti; 10-14 UX-4c'nin.
> Spesifikasyon: `dokumanlar/02-mimari/kalan-paketler/PAKET-UX-4-mantik-ve-tutarlilik-duzeltmeleri.md`
> **COMMIT YOK** — çalışma ağacında bırakıldı.

## 1. Durum özeti

| Kapı | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln --nologo` | **0 hata · 0 uyarı** |
| Tam süit (`dotnet test` + trx) | **1283 / 1283 yeşil · 0 başarısız** (önce 1245; 7(d) düzeltmesiyle 1279 → 1283) |
| `dotnet/denetim.sh` | **DENETİM TEMİZ** |
| Mutasyon kanıtı | **21 mutasyon → 21'i de** adlandırılmış testi kırmızıya çevirdi (test açığı YOK) |
| Canlı doğrulama (5198) | 12 + **1 (7(d) turu)** senaryo, beklenen HTTP kodları + SQL kanıtı |
| Migration | **gerekmedi** (şema değişikliği yok — gerekçe § 4) |
| Test kiracısı | `PAKET-UX-4b-TEST` ve 7(d) turunun 3 adet `PAKETUX-4b-TEST` kiracısı silindi; `tenants` 6 → 9 → **6** (QA-TEST-1'e dokunulmadı) |

> **7(d) eki (2026-09-16, ikinci tur):** hasım doğrulayıcının [KRITIK] bulgusu — spesifikasyonun
> "Kurucu ek bulgusu — madde 7(d)" alt bölümü ilk turda **ne uygulanmış ne de sapma olarak beyan edilmişti**.
> Bu tur kapsamında uygulandı, test + mutasyon + canlı kanıtla kapatıldı (§ 3 madde 7(d), § 6 M7-6…M7-9, § 8 C13).

---

## 2. Spesifikasyonun koda karşı doğrulanması (Adım 1)

| Spesifikasyon iddiası | Kodda bugün | Sonuç |
|---|---|---|
| `UrunGuncelleDto.Konsinye` `bool` (nullable değil), temizleme bloğu koşulsuz | Doğrulandı — `UrunModulu.cs` içinde `dto.Konsinye != mevcut.Konsinye \|\| …` bloğu sahibi/bedeli null'lıyordu | Hedefe sadık uygulandı |
| POS'ta `hasFiyati` yalnız atanıyor, hiç okunmuyor | Doğrulandı (`Islemler.razor` `private decimal hasFiyati;` yalnız `KurYansit`'te yazılıyordu) | Uygulandı |
| Ana kasa seçicisi tüm kasaları listeliyor, bacak kasaları süzülüyor | Doğrulandı (`BacakKasalari` süzüyor, `#islem-kasa` süzmüyordu) | Uygulandı |
| Manuel kur modalında GBP alanı yok; sunucu `ManuelKurDto.GbpTry` kabul ediyor | Doğrulandı — sunucu tarafı HAZIR, iş tamamen ekran tarafında | Uygulandı |
| `TahsilatGirisDto`'da `KasaId` yok; `TahsilatGir` kasaya hiç yazmıyor | Doğrulandı | Uygulandı |
| Yeni cari modalı 3 tür, düzenleme modalı 5 tür; sunucu 5'ini kabul ediyor | Doğrulandı (`CariOlusturDto.Tur` regex'i 5 değer) | Tek diziden üretilir hâle getirildi |

**Sapma 1 — "Birleşik koşullar" bölümü spesifikasyonda YOK.** Görev metni bu bölümü bağlayıcı sayıyor; dosyada böyle bir başlık bulunmuyor (başlıklar: Mühürlü kararlar · Sentez özeti · Hemen düzeltilecekler · Kanıtlar · Genel kurallar · UX-1 Faz B/C · Riskler · Tüm bulgular). Varsayım yapılmadı (Anayasa md.1): bağlayıcı kabul edilen küme **§ Mühürlü kararlar (Q1) + görevdeki EK KOŞULLAR (Q1 seçenek B, Mehmet koşul 2/3/4) + § Genel kurallar (UX-S21…S33)** oldu.

**Sapma 2 — HAVALE bacağı.** Belgedeki Q1 kararı "HAVALE banka hesabına yazar" diyor. Görevdeki **Mehmet koşul 2 (veto niteliğinde)** bu cümleyi geçersiz kılıyor: tahsilatta da HAVALE **kasaya** yazılır, satış konvansiyonuyla aynı (`SatisModulu` "Kasa ID — NAKIT/KART/HAVALE için zorunlu"). **Banka bacağı bu pakette AÇILMADI** (paket 41/32 alanı). Kanıt: `HAVALE_tahsilati_da_KASAYA_yazilir_satisla_ayni` testi banka hareketinin **boş** olduğunu da doğruluyor.

**Sapma 3 — element id önekleri.** Spesifikasyon tablosu yeni cari modalı için `#c-vergi-no` / `#c-fiyat-listesi` öneriyor. O modaldaki mevcut id öneki `cari-` (`#cari-ad`, `#cari-tur`, `#cari-adres`). Aynı modalda ikinci bir önek açmamak için yeni id'ler **`cari-vergino`, `cari-limit`, `cari-vade`, `cari-fiyat-listesi`** oldu (UX-S31 "tek ad, tek kaynak"). Mevcut hiçbir id değişmedi.

**Sapma 5 (ikinci tur, düzeltildi) — kapsam beyanı eksikti.** Spesifikasyonun "Hemen düzeltilecekler" tablosundan sonra gelen
**"### Kurucu ek bulgusu — madde 7(d) (2026-09-16 13:58, ekran görüntüsü; UX-4b kapsamında BAĞLAYICI)"** alt bölümü ilk turda
**ne uygulandı ne de sapma olarak yazıldı**; koşul tablosunda madde 7 yalnız (a)(b)(c) kanıtıyla "KARŞILANDI" göründü.
Anayasa md.1 gereği bu eksik kapsam burada kayda geçirildi ve bu turda **uygulandı** (§ 3 madde 7(d), § 6 M7-6…M7-9, § 8 C13).
Aynı alt bölümün sayı biçimi cümlesi ("TRY6,821.11") spesifikasyonun kendi ifadesiyle **UX-2-A2 kabuk işidir**, bu pakette
bilinçli olarak **dokunulmadı**. Kod doğrulaması: `SatisFisiDto` (SatisModulu.cs:230-251) kök seviyede `KasaId` **taşımıyor** →
spesifikasyonun "ölü alan" iddiası doğrulandı.

**Sapma 4 — mevzuat/eşik yapılandırmadan.** "Kasasız tahsilat" raporunun tarih eşiği kodda SABİT YAZILMADI: sorgu parametresi (`?esik=`) ya da yapılandırma anahtarı `Tahsilat:KasaZorunluBaslangic`. İkisi de yoksa uç **400** döner (fail-closed) — eşik tahmin edilmez. Bkz. § 11 kurucuya/mali müşavire notlar.

---

## 3. Yapılanlar

### Madde 6 — Konsinye verisinin sessiz silinmesi durduruldu
- `UrunGuncelleDto.Konsinye` → **`bool?`** (XML yorumunda neden yazıldı).
- `UrunServisi.Guncelle`: konsinye üçlüsü **yalnız gövdede geldiyse** değerlendirilir
  (`if (dto.Konsinye is not null || dto.KonsinyeCariId is not null || dto.KonsinyeBirimBedel is not null)`),
  gelmediyse mevcut kayıt olduğu gibi korunur (UX-S26).
  `KonsinyeKapisi.Dogrula` ve `CariDogrula` çağrıları **aynen** korundu — ikinci bir kapı yazılmadı.
- `Urunler.razor`: düzenleme modalına konsinye kutusu (`#d-konsinye`) + sahibi (`#d-kons-cari`) + bedeli (`#d-kons-bedel`);
  `UrunTamKart` kaydı artık konsinye üçlüsünü **okuyor**; PATCH gövdesi üçlüyü taşıyor;
  Kaydet düğmesi konsinyede sahibi/bedeli boşken kilitli (yeni ürün modalındaki kuralın aynısı).

### Madde 7 — POS'ta türetilebilen üç alan bağlandı
- (a) `KurYansit`: `hasFiyati` artık **okunuyor** → hurda ve takas has gram fiyatı canlı kurdan doluyor;
  `#hurda-fiyat-kaynak` / `#takas-eski-fiyat-kaynak` rozetleri **"Kurdan geldi" / "Elle girildi"** diyor (UX-S21).
  Elle girilen değerin üzerine kur güncellemesi **yazmaz**; fiş kaydedilince bayraklar sıfırlanır ve fiyat yeniden kurdan gelir.
  ⚠ `@bind:after` yerine açık `@onchange` kullanıldı: değer ve kaynak bayrağı **aynı olayda** yazılsın, rozet ile alan asla ayrışmasın.
- (b) Tek TL kasası varsa ana kasa **otomatik seçilir ve kilitlenir**; yanında `#islem-kasa-tek` sebebini yazar (UX-S22).
- (c) Ana kasa seçicisi **TRY ile süzülür** (`AnaKasalar`). Bu sunucudaki K6 kapısını tekrarlamaz, imkânsız seçeneği ekrandan kaldırır.
- (d) **(ikinci tur — kurucu ek bulgusu)** Karma ödeme açıkken üst `#islem-kasa` seçicisi **hiç çizilmez**:
  dal koşulu `else if (tur != "TRANSFER")` → `else if (tur != "TRANSFER" && !KarmaAcik)`.
  Gerekçe koda da yazıldı: `SatisFisiDto`'da **kök seviyede `KasaId` alanı YOKTUR**
  (`SatisModulu.cs:230-251`) — üstteki seçici ölü alandı, kullanıcıya aynı soruyu iki kez soruyordu (UX-S22 ihlali).
  · Bacak kasa seçicisi, bacağın para birimine uyan **tek kasa** varsa otomatik atanır (`BacakKasaSabitle`,
  `BacakEkle` / `AracDegisti` / `BirimDegisti` içinde) ve **kilitlenir**; altına `#islem-bacak-{ix}-kasa-tek`
  salt okunur satırı yazılır (mevcut `metin-soluk metin-kucuk` sınıfları, yeni CSS yok, satır içi `style` yok).
  · Karma **kapatılınca** üst seçici geri gelir ve **son bacağın kasası** ön değer olur (`KarmaDegisti`);
  ön değer yalnız ana seçicide gerçekten bulunan bir TL kasasıysa uygulanır (fail-closed), aksi hâlde alan boş kalır.
  · `@bind:after="TevsikOnizle"` → `KarmaDegisti` (tevsik önizlemesi **aynen** çağrılmaya devam eder).
  · Mevcut element id'leri değişmedi; yeni id `islem-bacak-{ix}-kasa-tek` (`<önek>-<alan>` kuralı).

### Madde 8 — Kur ekranı
- Manuel kur modalına **GBP** alanı (`#m-gbp`), gövdede `gbpTry`, modal açılışında mevcut GBP ile ön-doldurma.
- "Şimdi çek" düğmesi (`#kur-simdi-cek`) sürerken **"Çekiliyor..."** der ve kilitlidir (UX-S30);
  hem "Şimdi çek" hem manuel kayıt sonrası **BasariBandi** ("Kur güncellendi", 3 sn) görünür (UX-S27).
  Bant tek bir `BasariGoster` metodundan beslenir — kopya metot yazılmadı.
- Kur yazma yollarının rol kapıları (`PATCH /kur/kaynak`) **değişmedi**.

### Madde 9 — Cari tahsilatına kasa bacağı
**Sunucu (`CariModulu.cs`)**
- `TahsilatGirisDto.KasaId` eklendi + `IValidatableObject`: TL tutarı > 0 iken kasa **zorunlu** (controller yolunda 400, alan adı `kasaId`).
- `CariServisi.TahsilatGir` içinde **servis içi elle kontrol** (DTO doğrulaması servis çağrılarında çalışmaz).
- Kasa doğrulaması **mevcut tek mekanizmadan**: `IslemDogrulayici.KasaDogrula(db, dto.KasaId!, islem.TenantId, "TRY", now)`
  → K6 para birimi kapısı **ve** `GunKapanisiKapisi.Dogrula` gün kilidi bu tek çağrının içinden gelir. Yeni kasa doğrulama kodu YAZILMADI.
- `masak.DogrulaKumulatif` → kasa kapısı → `tevsik.Dogrula` sırası **transaction ÖNCESİ** korundu.
- Kasa bacağı **aynı transaction** içinde: `kasa.Bakiye += tlTutari` + `KasaHareket` (**işaretli**, `Math.Abs` yok), `HareketId` ile cari hareketine bağlı.
- Deftere yazan tek yol `HareketFabrikasi.Ekle` — `Hareketler.Add` sayısı **1** kaldı (denetim.sh TEMİZ).
- Yalnız has gram tahsilatında (TL = 0) kasa **istenmez**.
- Yeni uç: `GET /api/v1/cari/kasasiz-tahsilatlar?esik=&limit=` — `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]`,
  yalnız **tarih eşiğiyle** (`Tur=TAHSILAT ∧ OdemeTuru ∈ {NAKIT,KART,HAVALE} ∧ Tarih < eşik`).
  **Kasa tahmini yok, geri doldurma yok, eski kayıtlar değiştirilmez (append-only).**

**Ekran (`Cariler.razor`)**
- Tahsilat modalında TL tutarı > 0 iken kasa seçici (`#th-kasa`), tek TL kasasında **otomatik + kilitli** (`#th-kasa-tek`);
  seçici TRY ile süzülür; kasa seçilmeden Kaydet kilitli ve sebebi yazılı (`#th-kasa-gerekli`, UX-S30).
- Yeni cari modalı düzenleme modalıyla **alan kümesi olarak eşitlendi**: vergi no, kredi limiti, varsayılan vade, fiyat listesi.
- Cari türü seçenekleri **tek diziden** (`CariTurleri`) üretiliyor — iki modalda beş seçenek (UX-S26/S31).

---

## 4. Migration

**Gerekmedi.** Bu pakette hiçbir yeni varlık/kolon doğmadı:
- `TahsilatGirisDto.KasaId` bir **DTO** alanıdır; `hareketler` tablosunda kasa_id kolonu **açılmadı** (Bertan/Mehmet koşulu: "yeni bir bayrak kolonu eklenmesin", kasasız tahsilat işareti türetilmiş kalır).
- Kasa bacağı mevcut `kasa_hareketleri` tablosuna yazar.
- `UrunGuncelleDto.Konsinye` yalnızca C# tipini değiştirdi (`bool` → `bool?`); `urunler.konsinye` kolonu aynı.
- GBP zaten `kur_snapshots.gbp_try` olarak vardı.

`dotnet ef migrations add` **çalıştırılmadı**; `kuyumcu_net` şeması değişmedi (22 migration aynen duruyor).

---

## 5. Test sayıları

| | Önce | Sonra |
|---|---|---|
| Toplam | 1245 | **1283** |
| Başarısız | 0 | **0** |
| Atlanan | 0 | 0 |

**Yeni test sayısı: 38** (ilk tur 34 + 7(d) turu 4)
- `Kuyumcu.Tests/TahsilatKasaBacagiTestleri.cs` — 12 (DB/servis)
- `Kuyumcu.Tests/HttpTahsilatKasaTestleri.cs` — 6 (gerçek HTTP boru hattı)
- `Kuyumcu.Tests/UX4bEkranTestleri.cs` — **20** (bUnit; 16 ilk tur + 4 adet 7(d) testi)

**7(d) turunun dört testi (hepsi mutasyonla kanıtlı):**
1. `Karma_acikken_ust_kasa_secicisi_CIZILMEZ` — karma kapalıyken `#islem-kasa` **tek**, açıkken `FindAll("#islem-kasa")` **boş** (`#islem-kasa-tek` de yok)
2. `Karma_kapaninca_ust_secici_GERI_gelir_ve_son_bacak_kasasi_on_degerdir` — iki TL kasalı fikstürde bacakta `ks3` seçilir, karma kapanınca `#islem-kasa` değeri `ks3`
3. `Karma_bacak_kasasi_TEK_TL_kasasinda_otomatik_ve_KILITLIDIR` — `#islem-bacak-0-kasa` değeri `ks1` + `disabled` + `#islem-bacak-0-kasa-tek` "otomatik" der
4. `Karma_fis_govdesinde_YALNIZ_BACAK_kasasi_gider` — `POST /islemler/satis-fisi` gövdesinde kök `kasaId` **yok**, `odemeler[0].kasaId = ks1`

**Mehmet koşul 4'ün dört adlı testi (hepsi mutasyonla kanıtlı):**
1. `KasaIdsiz_NAKIT_tahsilat_REDDEDILIR` (+ HTTP `KasaIdsiz_NAKIT_tahsilat_400_doner`)
2. `Kapali_gune_tahsilat_GUN_KILIDINE_takilir`
3. `Doviz_kasasina_TL_tahsilat_REDDEDILIR` (+ HTTP karşılığı)
4. `Basarili_NAKIT_tahsilat_kasayi_ARTIRIR_cariyi_DUSURUR`

**Mevcut testlerde yapılan uyarlamalar** (davranış değişikliğinin doğal sonucu, gizlenmedi):
- `VadeTakibiTestleri.cs` (5 çağrı), `MasakEntegrasyonTestleri.cs` (1), `NakitPayiYazmaTestleri.cs` (1): tahsilat DTO'suna `KasaId = k.KasaId` eklendi.
- `MasakBaglantiliIslemTestleri.cs` **değişmedi**: MASAK kapısı kasa kapısından ÖNCE çalışıyor, test hâlâ MASAK hatası bekliyor ve alıyor.
- `CariEkranTestleri.cs`: sahte API'ye gerçekçi `/kasalar` yanıtı eklendi (immutable kural: "`{}` varsayılanı → yeni uçlara gerçekçi yanıt"). 22/22 yeşil.

### ⚠ Kararsız (flaky) test tespiti ve düzeltmesi — Anayasa md.1
Mutasyon turunda `POS_elle_girilen_hurda_fiyati_ELLE_GIRILDI_rozeti_alir` **alakasız mutasyonlarda da** kırmızıya döndü.
Ölçüm: tek başına 5/5 yeşil, sınıf içinde 0/6. Sebep gizlenmedi, kök neden bulundu: canlı kur **asenkron** yansıyor;
test, kur yansımadan önce `Change` yapınca ölçüm iki render turu arasına düşüyordu.
Düzeltme: POS testleri önce `WaitForAssertion` ile kurun yansımasını bekliyor. Sonuç: 4 ardışık koşumda 16/16 yeşil.
Etkilenen mutasyonlar (M7-2, M7-3, M7-5) **düzeltmeden sonra yeniden koşuldu** ve tek temiz sinyal verdi.

---

## 6. Mutasyon tablosu

Yöntem: kaynak `…/scratchpad/mut-yedek/UX-4b/` (7(d) turu: `…/mut-yedek/UX-4b-7d/`) altına kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli değişiklik → `dotnet build` + filtreli `dotnet test` → yedek geri kopyalandı → `cmp` ile birebir aynılık kanıtlandı. **21/21 mutasyon kırmızı üretti.**

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (cmp) |
|---|---|---|---|
| M6-1 | `UrunModulu`: konsinye koruma koşulu kaldırıldı (`if (true)` + `dto.Konsinye ?? false`) | `Konsinye_alanlari_gonderilmeyen_PATCH_ile_KORUNUR` | birebir |
| M6-2 | `Urunler.razor`: PATCH gövdesinden konsinye üçlüsü silindi | `Urun_PATCH_govdesi_KONSINYE_ucluyu_tasir` | birebir |
| M6-3 | `Urunler.razor`: `dKonsinyeBedel = null` (tam kart okunmuyor) | `Urun_PATCH_govdesi_KONSINYE_ucluyu_tasir` · `Urun_duzenleme_modalinda_KONSINYE_alanlari_vardir` | birebir |
| M7-1 | `Islemler.razor`: hurda fiyatının kurdan doldurulması kapatıldı | `POS_hurda_has_gram_fiyati_KURDAN_gelir_ve_rozeti_soyler` · `POS_elle_girilen_…` | birebir |
| M7-2 | `HurdaFiyatGirildi`: `hurdaFiyatElle = false` | `POS_elle_girilen_hurda_fiyati_ELLE_GIRILDI_rozeti_alir` | birebir |
| M7-3 | `AnaKasalar` TRY süzmesi kaldırıldı | `POS_ana_kasa_secicisinde_DOVIZ_kasasi_YOKTUR` | birebir |
| M7-4 | `KasalariYukle`: tek kasada otomatik seçim kapatıldı | `POS_TEK_kasada_ana_kasa_otomatik_secilir_ve_KILITLENIR` | birebir |
| M7-5 | Takas fiyatının kurdan doldurulması kapatıldı | `POS_takas_eski_urun_fiyati_da_KURDAN_gelir` | birebir |
| M7-6 | `Islemler.razor`: dal koşulundan `&& !KarmaAcik` kaldırıldı (üst seçici karma açıkken yine çiziliyor) | `Karma_acikken_ust_kasa_secicisi_CIZILMEZ` | birebir |
| M7-7 | `BacakKasaSabitle` → `b.KasaId = string.Empty` (tek kasada otomatik atama yok) | `Karma_bacak_kasasi_TEK_TL_kasasinda_otomatik_ve_KILITLIDIR` · `Karma_fis_govdesinde_YALNIZ_BACAK_kasasi_gider` | birebir |
| M7-8 | Bacak kasa seçicisinden `disabled="@(BacakKasaTek(...) is not null)"` özniteliği silindi | `Karma_bacak_kasasi_TEK_TL_kasasinda_otomatik_ve_KILITLIDIR` | birebir |
| M7-9 | `KarmaDegisti`: son bacak kasasının ön değer yapılması `if (false)` ile kapatıldı | `Karma_kapaninca_ust_secici_GERI_gelir_ve_son_bacak_kasasi_on_degerdir` | birebir |
| M8-1 | `Kur.razor`: gövdeden `gbpTry` satırı silindi | `Manuel_kur_govdesinde_GBP_TASINIR` | birebir |
| M8-2 | `ManuelModalAc`: `mGbp = 0` (ön-doldurma yok) | `Manuel_kur_modali_MEVCUT_GBP_ile_acilir` | birebir |
| M8-3 | `SimdiCek`: `BasariGoster` çağrısı silindi | `Simdi_cek_sonrasi_BASARI_bandi_gorunur` | birebir |
| M9-1 | `TahsilatGir`: servis içi kasa zorunluluğu `if (false)` | `KasaIdsiz_NAKIT_tahsilat_REDDEDILIR` · `KasaIdsiz_KART_ve_HAVALE_tahsilati_da_REDDEDILIR` | birebir |
| M9-2 | `KasaDogrula(..., "TRY", now)` → `null` para birimi | `Doviz_kasasina_TL_tahsilat_REDDEDILIR` · HTTP `Doviz_kasasina_TL_tahsilat_400_doner` | birebir |
| M9-3 | `KasaDogrula(..., "TRY", now)` → `null` an (gün kilidi kapalı) | `Kapali_gune_tahsilat_GUN_KILIDINE_takilir` · `Basarisiz_tahsilat_HICBIR_bacagi_yazmaz` | birebir |
| M9-4 | Kasa bakiyesi artırımı kaldırıldı | `Basarili_NAKIT_tahsilat_kasayi_ARTIRIR_cariyi_DUSURUR` · `HAVALE_tahsilati_da_KASAYA_yazilir_satisla_ayni` · HTTP `KasaIdli_NAKIT_tahsilat_200_…` | birebir |
| M9-5 | Kasasız rapor filtresi `Tarih < sinir` → `< sinir.AddYears(10)` | `Kasasiz_tahsilat_raporu_YALNIZ_esik_oncesini_listeler` | birebir |
| M9-6 | `TahsilatGirisDto.Validate` → `if (false)` (DTO kapısı kapalı) | HTTP `KasaIdsiz_NAKIT_tahsilat_400_doner` | birebir |
| M9-7c | Kasasız rapor: eşik yoksa `DateTime.UtcNow` varsayılır (fail-closed delindi) | `Kasasiz_tahsilat_raporu_ESIKSIZ_uretilmez` · HTTP `…_ESIKSIZ_400_doner` | birebir |
| M9-8 | `Cariler.razor`: tahsilat gövdesinde `kasaId = null` | `Tahsilat_govdesinde_KASA_ID_gider_tek_kasada_otomatik` | birebir |
| M9-9 | `TahsilatAc`: tek kasada otomatik atama kapatıldı | `Tahsilat_govdesinde_KASA_ID_gider_tek_kasada_otomatik` | birebir |
| M9-10 | Yeni cari gövdesinden `vergiNo` silindi | `Yeni_cari_govdesi_vergiNo_ve_fiyatListesiId_TASIR` | birebir |
| M9-11 | `TahsilatKasalari` TRY süzmesi kaldırıldı | `Tahsilat_kasa_secicisinde_DOVIZ_kasasi_YOKTUR` | birebir |

> Not: M9-6, DTO kapısı ile servis içi kapıyı **ayırt edilebilir** kılmak için testin yanıt BİÇİMİNİ ölçmesini gerektirdi:
> model doğrulaması `message`'ı **dizi** döndürür, `BadRequestException` düz metin. Bu iddia eklenmeden M9-6 hiçbir testi kırmıyordu (test açığıydı) — açık kapatıldı.

Son mutasyondan sonra temiz `dotnet build`: **0 hata · 0 uyarı**.

---

## 7. Denetim çıktısı

```
═══ Kiracı izolasyonu denetimi ═══
  ✓ IgnoreQueryFilters (yasak)                     0
  ✓ new KuyumcuDbContext (yalnız fabrika)          1
  ✓ Find/FindAsync (yasak)                         0
  ✓ ExecuteUpdate/Delete (beyaz liste 4)           4
  ✓ Hareketler.Add (yalnız HareketFabrikasi)       1
  ✓ VeresiyeKapanisi (yalnız Etiketten)            2
  ✓ IKiraciVarlik uygulayan varlık                33
  ✓ HasQueryFilter                                 2

═══ Güvenlik yüzeyi ═══
  ✓ oran sınırı · HTTPS yönlendirme · sıkıştırma · sağlık ucu
  ✓ ArayuzModu (yalnız 6 dosya) 6 · ArayuzModu kapı yüzeyinde 0
  ✓ CORS beyaz liste

DENETİM TEMİZ.
```

---

## 8. Canlı doğrulama (localhost:5198, kiracı `PAKET-UX-4b-TEST`)

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| C1 | `POST /cari/{id}/tahsilat` — 1000 TL NAKİT, **kasaId yok** | 400 | **400** — `{"message":["Nakit/Kart/Havale tahsilat için kasa ID gerekli"],…}` (dizi → DTO kapısı) |
| C2 | Aynı uç, **USD kasası** + TL tutar | 400 | **400** — `Bu işlem TRY kasası gerektirir; 'UX4b USD Kasa' kasasının para birimi USD` |
| C3 | Aynı uç, 1500 TL NAKİT + TL kasası | 200 | **200** — cari kartı + bakiye döndü |
| C4 | 800 TL **HAVALE** + TL kasası | 200, kasaya yazar | **200** |
| C5 | `GET /cari/kasasiz-tahsilatlar` (eşiksiz) | 400 | **400** — "Eşik tahmin edilmez." |
| C6 | `…?esik=2030-01-01` | 200, adet 2, tutarlar POZİTİF | **200** — `adet:2`, `tlTutari: 800.00 / 1500.00` |
| C7 | `…?esik=2020-01-01` | 200, adet 0 | **200** — `adet:0`, `satirlar:[]` |
| C8 | Gün kapanışı sonrası tahsilat | 400 gün kilidi | **400** — `16.09.2026 günü bu kasa için KAPATILMIŞTIR … Kapanmış güne hareket yazılamaz` |
| C9 | Konsinye ürün oluştur | 201 | **201** |
| C10 | `PATCH /urunler/{id}` gövde = `{"ad":"…"}` (konsinye alanları YOK) | konsinye korunur | **200** + SQL: `konsinye=1`, sahibi ve `9500.00` **aynen duruyor** |
| C11 | `PATCH … {"konsinye":false}` | alanlar temizlenir | **200** + SQL: `konsinye=0`, sahibi `NULL`, bedel `NULL` |
| C12 | `POST /kur/manuel` `gbpTry:66.1234` → `GET /kur/guncel` | GbpTry döner | **201** → `kaynak MANUAL · gbpTry 66.1234 · altinHas 6900.0` |

**7(d) turu (ikinci canlı koşum, kiracı `PAKETUX-4b-TEST`):**

| # | Adım | Beklenen | Gerçek |
|---|---|---|---|
| C13 | `POST /islemler/satis-fisi` — gövde kökünde **kasaId YOK**, yalnız `odemeler[0].kasaId` (12.000 TL NAKİT) | 200 + kasa hareketi | **200** — `{"fisId":"01M2N156F068BZX1D2088G4MXX","toplamTl":12000,"kalemSayisi":1,"nakitToplam":12000,"odemeler":[{"arac":"NAKIT","tutarTl":12000,"kasaId":"01M2N14HKR6F9KCVP4WNRWN67S"}]}` |

Gövde kök anahtarları (`jq keys`): `["cariId","kalemler","kurSnapshotId","odemeler"]` — **kasaId yok**.
Hazırlık: `POST /auth/kayit` 201 · `POST /kasalar` 200 · `POST /islemler/alis` 200 (stok; ilk denemede fiş
`400 "Bu şubede ürün stoğu bulunamadı"` döndü — gizlenmedi, stok açılıp tekrarlandı).

**SQL kanıtı (C13 sonrası, `kuyumcu_net`, kasa_hareketleri ⋈ hareketler):**
```
-18000.00  TRY  NAKIT  ALIS   -18000.00  fis_id=NULL
 12000.00  TRY  NAKIT  SATIS   12000.00  fis_id=01M2N156F068BZX1D2088G4MXX
kasalar: UX4b7d TL Kasa  TRY  bakiye=-6000.00   (-18000 alış + 12000 fiş bacağı)
```
→ Karma/fiş ucunun kasa bacağı **yalnız bacak kasaId'si ile** yazıldı; üst seçicinin kaldırılması hiçbir
sunucu sözleşmesini değiştirmedi (`SatisFisiDto`'da zaten kök `KasaId` yok).

**SQL kanıtı (C3 + C4 sonrası, `kuyumcu_net`):**
```
kasa           UX4b TL Kasa   TRY   1500.00        (C3 sonrası; C4 ile 2300.00)
kasa_hareket   1500.00  TRY  NAKIT  TAHSILAT  h.tl_tutari=-1500.00
cari_bakiye    -1500.00   has 0.000000
```
→ Kasa hareketi **işaretli (+1500)**, `hareket_id` ile TAHSILAT hareketine bağlı; deftere negatif yazılan ödeme kasaya pozitif giriyor; cari bakiyesi düşüyor. **Tek transaction.**

**Temizlik:** API PID durduruldu, `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**. 5199 (kurucunun inceleme sunucusu) hiç ellenmedi.
Test kiracısı `information_schema`'dan çıkarılan 32 `tenant_id`'li tablodan + `tenants`'tan silindi.
`select count(*) from tenants` → **6** (başlangıçtaki değer). `QA-TEST-1 Kuyumculuk` kiracısına **dokunulmadı**.

**7(d) turu temizliği:** 5198 PID `kill` ile durduruldu, `lsof -nP -iTCP:5198 -sTCP:LISTEN` **boş**.
3 adet `PAKETUX-4b-TEST` kiracısı (biri ilk betik denemesinden) `information_schema`'dan çıkarılan
**33** `tenant_id` kolonlu tablodan + `tenants`'tan silindi; `select count(*) from tenants` → **6**
(9 → 6, başlangıç değeri). Kalıntı denetimi: `hareketler / kullanicilar / urunler / kasalar` içinde
`tenant_id like '01M2N1%'` → **0 / 0 / 0 / 0**. `QA-TEST-1 Kuyumculuk` yerinde. 5199'a dokunulmadı.

---

## 9. DoD sözleşmesi (Bertan koşulu — dalga 2b'den itibaren bağlayıcı)

**Bu pakette YENİ varlık / yeni kayıt türü DOĞMADI.** Sözleşme her değişen kayıt için aşağıdaki gibi karşılanır:

| Kayıt / alan | Düzenleme yolu | Yetki kapısı | Denetim | Ekran eylemi |
|---|---|---|---|---|
| `urunler` konsinye üçlüsü (Konsinye · KonsinyeCariId · KonsinyeBirimBedel) | **PATCH `/urunler/{id}`** — gönderilmeyen alan KORUNUR, `{"konsinye":false}` ile temizlenir | `[ModulYetkisi("urun")]` + mevcut `[RolGerekli]` (değişmedi) | `DenetimYazmaDinleyicisi` (yeniden yazılmadı) | Ürün düzenleme modalı: kutu + sahibi + bedeli (`#d-konsinye`, `#d-kons-cari`, `#d-kons-bedel`) |
| `kur_snapshots.gbp_try` | Yeni manuel kur kaydıyla (kur kayıtları **değişmez**; UX-1'de `DegismezIpucu` ile gerekçelendirilmiştir) | `POST /kur/manuel` mevcut kapı | mevcut | Manuel kur modalı `#m-gbp` |
| `hareketler` (TAHSILAT) | **SALT OKUNUR — hard DELETE YASAK.** Düzeltme yolu: ters hareket / `POST /cari/{id}/duzeltme` (gerekçeli düzeltme fişi, ≥20 karakter) | `[ModulYetkisi("cari")]`; düzeltme fişi `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` | mevcut denetim izi | Cariler ekranında "Düzeltme" düğmesi (rolle çizilir) |
| `kasa_hareketleri` (tahsilat bacağı) | **SALT OKUNUR — hard DELETE YASAK.** Düzeltme: kasa hareketi girişi / gün kapanışı yeniden açma (20-1000 karakter gerekçe) | `[ModulYetkisi("kasa")]` + mevcut `[RolGerekli]` | mevcut | Kasalar ekranı |
| `GET /cari/kasasiz-tahsilatlar` | **Salt okunur rapor** — kayıt üretmez, bu yüzden düzenleme yolu gerekmez | `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` | okuma; yazma yok | (bu pakette uç düzeyinde; ekran bölümü UX-4c/paket 34 alanı — § 10) |
| `cariler` (yeni modalda açılan alanlar) | **PATCH `/cari/{id}`** — düzenleme modalı zaten tüm alanları taşıyor | `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` | mevcut | Düzenle / Pasife al |

**Neden salt okunur:** finansal varlıklarda (hareketler, kasa_hareketleri) hard DELETE **yasaktır**; defter append-only'dir ve bakiye defterden yeniden türetilebilir olmak zorundadır. Düzeltme, ters hareket veya gerekçeli düzeltme fişiyle yapılır.

---

## 10. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Kapsam yalnız madde 6-9, sıra 6→7→8→9 | ✅ | Bu rapor § 3; madde 1-5 ve 10-14'e dokunulmadı |
| Q1 seçenek B — KasaId NAKIT/KART/HAVALE için **sunucuda** zorunlu | ✅ | DTO `IValidatableObject` + servis içi kontrol; M9-1 / M9-6 |
| VERESIYE/mahsupta boş (yalnız has gram → kasa istenmez) | ✅ | `Sadece_HAS_GRAM_tahsilatinda_kasa_ISTENMEZ` |
| Kasa doğrulaması **aynen** `IslemDogrulayici.KasaDogrula(db, kasaId, tenant, "TRY", now)` | ✅ | Kod tek satır; yeni kasa doğrulama kodu yok; M9-2 / M9-3 |
| `tevsik.Dogrula` + `masak.DogrulaKumulatif` sırası (tx ÖNCESİ) korunur | ✅ | `TahsilatGir` gövdesi; `MasakBaglantiliIslemTestleri` değişmeden yeşil |
| Kasa hareketi yalnız tek yazma yolundan; `Hareketler.Add` = 1 | ✅ | denetim.sh "Hareketler.Add (yalnız HareketFabrikasi) 1" |
| Mehmet koşul 2 — HAVALE **kasaya** yazılır, banka bacağı açılmaz | ✅ | `HAVALE_tahsilati_da_KASAYA_yazilir_satisla_ayni` (banka hareketi boş); § 2 Sapma 2 |
| Mehmet koşul 3 — kasasız tahsilat raporu yalnız tarih eşiğiyle, tahmin/geri doldurma yok | ✅ | `GET /cari/kasasiz-tahsilatlar`; M9-5 / M9-7c; C5-C7 |
| Mehmet koşul 4 — 4 adlı test, mutasyonla kanıtlı | ✅ | § 5 ve § 6 |
| Madde 9 ekran — kasa seçici, tek kasada otomatik + kilitli (UX-S22) | ✅ | `#th-kasa`, `#th-kasa-tek`; M9-9 |
| Yeni cari modalı düzenleme modalıyla eşit (vergiNo, limit, vade, liste, 5 tür) | ✅ | `Yeni_cari_modali_duzenleme_modaliyla_AYNI_alan_kumesini_tasir`; canlı C: `POST /cari` alanları kabul etti |
| Madde 6 — `Konsinye` `bool?`, yalnız gönderildiyse değişir; mutasyon kırmızı; canlı SQL kanıtı | ✅ | M6-1; C10/C11 SQL |
| Madde 7 (a-b-c) — kurdan doldurma + rozet; tek kasa otomatik+salt okunur; TRY süzmesi | ✅ | M7-1…M7-5 |
| Madde 7(d) — karma açıkken üst kasa seçicisi çizilmez; bacak kasası tek kasada otomatik+kilitli; karma kapanınca son bacak kasası ön değer | ✅ | `Karma_acikken_ust_kasa_secicisi_CIZILMEZ` · `Karma_bacak_kasasi_TEK_TL_kasasinda_otomatik_ve_KILITLIDIR` · `Karma_kapaninca_ust_secici_GERI_gelir_…` · `Karma_fis_govdesinde_YALNIZ_BACAK_kasasi_gider`; M7-6…M7-9; C13 |
| Madde 8 — `#m-gbp`, `gbpTry`, ön-doldurma, "Şimdi çek" yükleniyor + BasariBandi | ✅ | M8-1…M8-3; C12 |
| UX-S21…S33 bağlayıcı | ✅ | S21 (rozet), S22 (tek seçenek gizlenir/kilitlenir), S26 (oluşturma=düzenleme alan kümesi), S27 (başarı bandı), S30 (kilitli düğme sebebi), S31 (tek dizi/tek kaynak), S32 (tutar+para birimi kasa seçicide) |
| Yeni CSS'te sabit renk yok · satır içi `style=""` yok · mevcut id'ler değişmedi | ✅ | Yeni işaretlemede yalnız mevcut sınıflar (`rozet`, `rozet-mavi`, `rozet-amber`, `metin-soluk`, `metin-kucuk`) kullanıldı; yeni CSS yazılmadı |
| Sozlukler TR + EN, çıplak metin yok | ✅ | `Islem.TekKasaOtomatik/KurdanGeldi/ElleGirildi`, `Kur.Cekiliyor/KurGuncellendi` — ikisi de iki dilde |
| bUnit sahte API `{}` varsayılanı → gerçekçi yanıt | ✅ | `CariEkranTestleri` `/kasalar` eklendi; `UX4bEkranTestleri` tüm yeni uçlara gerçekçi yanıt verir |
| COMMIT yok | ✅ | `git log` tek commit (`bbfdc95`), çalışma ağacı kirli bırakıldı |
| Test kiracısı silindi, tenants başlangıç değerine döndü | ✅ | 6 → 6; QA-TEST-1'e dokunulmadı |
| 5199'a dokunulmadı | ✅ | Yalnız 5198 kullanıldı, PID ile durduruldu |

---

## 11. Açık kalanlar / kurucuya ve mali müşavire notlar

1. **`Tahsilat:KasaZorunluBaslangic` yapılandırılmadı (bilinçli).** Kasasız tahsilat raporu çalışması için bu anahtarın
   *kasa bacağının canlıya çıktığı an* ile doldurulması gerekir. Değer **tahmin edilmedi** (Anayasa md.1) ve varsayılan **KAPALI**
   bırakıldı: anahtar yoksa uç 400 döner. **Kurucu/mali müşavir kararı gerekir.**
2. **Geçmiş tahsilatların kasa bacağı YOK ve geriye dönük üretilmeyecek.** `hareketler` tablosunda kasa_id kolonu hiç olmadı;
   kasa uydurmak denetimde savunulamaz. Sevk sonrası rapor tutarsızlığı bu raporla **görünür** kılınır, düzeltilmez.
   Mali müşavirin kararı: bu farklar mutabakat/düzeltme fişiyle mi kapatılacak, yoksa açılış farkı olarak mı bırakılacak?
3. **HAVALE tahsilatı şu anda KASAYA yazılıyor** (satış konvansiyonuyla aynı). Muhasebe açısından doğrusu banka hesabıdır;
   banka bacağı **paket 41/32**'de her iki modüle (Satış + Cari) **aynı pakette** gelmelidir. O güne kadar havale tahsilatları
   kasa defterinde görünür. Mali müşavire bildirilmesi gereken bilinçli geçici durumdur.
4. **"Kasasız tahsilat" ekranı yok** — uç hazır, ekran bölümü UX-4c / paket 34 (rapor seti) alanına bırakıldı.
   Bu pakette raporun **ekrandan** görülebilmesi kapsam dışıydı.
5. **Kasa seçicileri yalnız TRY listeler.** Döviz tahsilatı (USD/EUR kasasına doğrudan tahsilat) **bu pakette yoktur**;
   çok-birimli cari bakiyesi paket 27'nin konusudur.
6. **`TahsilatGirisDto.KasaId` sözleşme kırıcıdır.** Kasa göndermeyen eski istemciler 400 alır. Bu, mühürlü Q1-B kararının
   bilinçli sonucudur (Bertan'ın geriye dönük uyum itirazı oylamada azınlıkta kaldı). Dış istemci varsa sürüm notuna yazılmalıdır.
7. **Çalışma ağacında eski `.mutorj` artıkları var** (`HareketFabrikasi.cs.mutorj`, `SatisModulu.cs.mutorj`, `Islemler.razor.mutorj` vb.).
   Bu oturumda **üretilmedi** ve bu oturumda **silinmedi** (silme kurucuya bırakılır; TurkReport hook `rm` bloklar).
8. **7(d) ilk turda atlanmıştı — Anayasa md.1 kaydı.** Spesifikasyonun "Hemen düzeltilecekler" tablosundan sonra gelen
   "Kurucu ek bulgusu — madde 7(d)" alt bölümü ilk turda ne uygulandı ne de sapma olarak beyan edildi; koşul tablosunda
   madde 7 yalnız a-b-c kanıtıyla "KARŞILANDI" işaretlenmişti. Bu tur kapsamda tamamlandı ve kanıtlandı.
   **Kapsam dışı bırakılan tek şey** aynı alt bölümdeki sayı biçimi sorunudur ("TRY6,821.11"): spesifikasyon bunu
   açıkça **UX-2-A2 kabuk işi** sayıp "burada dokunulmaz" dediği için bu pakette değiştirilmedi.
9. **7(d) için tarayıcı doğrulaması yapılmadı** — kanıt bUnit DOM iddiaları + mutasyon + sunucu sözleşmesi (`SatisFisiDto`'da
   kök `KasaId` yok) + C13 canlı fiş turudur. Kurucunun ekran görüntüsündeki görsel doğrulama, 5199 (inceleme sunucusu)
   yeni yayın anlık görüntüsüyle tazelendikten sonra yapılabilir; 5199'a bu oturumda **dokunulmadı**.
10. **Bilinen tek derleme uyarısı** (`xUnit1031`, `RotaBaslikTestleri.cs:156`) bu paketten önce de vardı; temiz artımlı derlemede 0 uyarı görünür.
   Bu pakette **yeni uyarı üretilmedi**.

---

## 12. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısı depodaki tek commit yüzünden yüzlerce satır döndürüyor;
**bu pakette elle dokunulan dosyalar** şunlardır:

**Sunucu**
- `dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Urun/UrunModulu.cs`

**Sözlük**
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`

**Ekran**
- `dotnet/Kuyumcu.Web/Pages/Cariler.razor`
- `dotnet/Kuyumcu.Web/Pages/Islemler.razor`
- `dotnet/Kuyumcu.Web/Pages/Kur.razor`
- `dotnet/Kuyumcu.Web/Pages/Urunler.razor`

**Test (yeni)**
- `dotnet/Kuyumcu.Tests/TahsilatKasaBacagiTestleri.cs`
- `dotnet/Kuyumcu.Tests/HttpTahsilatKasaTestleri.cs`
- `dotnet/Kuyumcu.Tests/UX4bEkranTestleri.cs`

**7(d) turunda dokunulanlar:** `dotnet/Kuyumcu.Web/Pages/Islemler.razor` · `dotnet/Kuyumcu.Tests/UX4bEkranTestleri.cs` · bu rapor.

**Test (uyarlanan)**
- `dotnet/Kuyumcu.Tests/CariEkranTestleri.cs`
- `dotnet/Kuyumcu.Tests/MasakEntegrasyonTestleri.cs`
- `dotnet/Kuyumcu.Tests/NakitPayiYazmaTestleri.cs`
- `dotnet/Kuyumcu.Tests/VadeTakibiTestleri.cs`

**Belge (yeni)**
- `dokumanlar/09-uygulama-kayitlari/PAKET-UX-4b-uygulama-raporu.md`

Mimari belgeye, paket spesifikasyonuna ve `SONRAKI-OTURUM-PROMPT.md`'ye **dokunulmadı**.
