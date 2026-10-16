# PAKET 1 — Fiş / işlem grubu + kalem modeli · FAZ A · uygulama raporu

> Tarih: 2026-09-16 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) rolü · Dalga 1
> Durum: **BİTTİ (yalnız FAZ A)** · Derleme 0 hata · 1180/1180 test yeşil · `denetim.sh` TEMİZ
> COMMIT YOK — değişiklikler çalışma ağacında.

## 1. Yapılanlar

### Veri modeli
- **YENİ tablo `fatura_kalemleri`** (`dotnet/Kuyumcu.Api/Data/FaturaKalemi.cs`, `IKiraciVarlik`).
  Ürün kartından **KOPYA** taşır (ayar, milyem, gram, has gram, taş ibaresi, birim); belge
  dondurulur. İndeksler: `(tenant_id, fatura_id)`, `(tenant_id, hareket_id)`.
- **`faturalar`** tablosuna iki kolon: `kalem_kaynagi` (BASLIK|KALEM, DB varsayılanı `BASLIK`)
  ve `has_deger_kaynagi` (HAREKET|KALEM|ELLE, DB varsayılanı `ELLE`).
  Bertan'ın koşulu gereği **tek migration**, `has_deger_kaynagi` **üç değerli** açıldı ki
  paket 38 kolonu yeniden migrate etmek zorunda kalmasın.

### Tek motor (Bertan kısmi vetosunun çözümü)
- `Modules/Fatura/FaturaKdvHesabi.cs` **YAZILMADI**.
- **YENİ** `Modules/Fatura/OzelMatrahMotoru.cs` — saf sınıf (DB/ağ/saat yok).
  İmza paket 38 belgesindeki tanıma uyumlu: girdi `(hasGrami, hasBirimFiyat)` bacak listesi +
  toplam + kdvOran; çıktı `OzelMatrahSonuc(HasGram, HasDeger, Matrah, KdvTutar, AraToplam, Uyari)`.
  Her bacak KENDİ kur fiyatıyla çarpılır (tek kur varsayılmaz), yuvarlama
  `Math.Round(…, 2, MidpointRounding.AwayFromZero)`. Paket 1 onu **kalem bazına** yayar;
  paket 38 aynı sınıfı hareket bazına uygulayacak.

### Uçlar
| Fiil | Yol | Durum |
|---|---|---|
| POST | `/api/v1/faturalar` | DEĞİŞTİ (geriye uyumlu): opsiyonel `kalemler` |
| GET | `/api/v1/faturalar/{id}/kalemler` | YENİ (`[ModulYetkisi("fatura")]`, okuma) |
| GET | `/api/v1/islemler/fis/{fisId}` | YENİ (`[ModulYetkisi("islem")]`, okuma) |

- `FaturaServisi.Olustur` ikiye ayrıldı: kalem gönderilmeyen istek **bugünkü gövdeden**
  (satır satır dokunulmadan) geçer; kalemli istek `KalemYolu`'na sapar.
- Damga mantığı (`fişin TÜM kalemleri`) `DamgalanacakBul` metoduna **çıkarıldı** ve iki yol da
  onu çağırır — kopyalanmadı (spesifikasyonun "yeniden kullanılır, kopyalanmaz" şartı).
- `FaturaKalemDto`'da **has bedeli alanı YOKTUR**: kuyum KDV'sinde matrah dışı tutar istemciden
  alınsaydı yanlış beyan kapısı açılırdı. Has bedeli ürün kartı + kur kaydından sunucuda türetilir.
- Sınırlar (kalem ≤ 200, miktar > 0, birim fiyat ≥ 0, açıklama ≤ 300) **servis içinde elle**
  doğrulanır (Mehmet K1-2: DataAnnotations yalnız controller yolunda çalışır).

### Ekran (Ertuğ)
`Kuyumcu.Web/Pages/Faturalar.razor`:
- Yeni fatura modalında **kalem tablosu** (`#fk-ekle`, `#fk-urun-{ix}`, `#fk-miktar-{ix}`,
  `#fk-fiyat-{ix}`, `#fk-iscilik-{ix}`, `#fk-indirim-{ix}`, `#fk-sil-{ix}`, `#fk-toplam`),
  kalem toplamı fatura toplamına eşit değilse **Kaydet KAPALI** (`#fk-kaydet`) + `HataBandi`.
- Liste satırında `#fk-goster-{ix}` → kalem detay modalı (`#fd-kalem-tablosu`, `#fd-toplam`),
  kalemsiz faturada boş tablo yerine **"bu fatura başlık düzeyindedir"** açıklaması (`BosDurum`).
- Kalemli faturada listede "Kalemler" rozeti. Mevcut hiçbir element id'si değişmedi;
  yeni id'ler `fk-` / `fd-` önekli. Yeni CSS **yazılmadı** (mevcut sınıflar kullanıldı) →
  sabit renk/satır içi stil sorunu doğmadı.
- `Sozlukler.cs`: 18 yeni anahtar **TR + EN** birlikte.

### Düzeltme politikası (UX-S1)
`DuzeltmePolitikasi`'na `S("FaturaKalemi", Fatura, "api/v1/faturalar/{id}/durum")` eklendi:
fatura kalemi **SALT_OKUNUR**; düzeltme yolu faturanın kendisidir (IPTAL + yeni fatura).
`DuzeltmePolitikasiTestleri` (harita tamlığı) yeşil kaldı.

## 2. Sapmalar ve gerekçeleri

| Sapma | Gerekçe |
|---|---|
| **POST `/faturalar/fisten` YAZILMADI** | Aşkın'ın bağlayıcı koşulu: `hareketler` tablosunda **adet kolonu yok**, kalemin miktarı defterden türetilemez. Kod gerçeği doğrulandı (`Hareket` sınıfında adet yok). Faz A = kalemli fatura (istemci `kalemler` gönderir) + kalem okuma ucu + geriye uyum. |
| **FAZ B işleri (fis_id'nin ALIS/HURDA_ALIS/TAKAS/ISCILIK'e yayılması, POS-fatura mutabakatı doğrulaması) yapılmadı** | Bertan: "FAZ B, FAZ A'dan AYRI SEVK EDİLECEK". Orkestratör talimatı: yalnız Faz A. |
| `GET /islemler/fis/{fisId}` Faz A'ya alındı | Orkestratörün EK KOŞUL'u ("yalnız GET /islemler/fis/{fisId} okuma ucu"). Fiş için ayrı tablo AÇILMADI. |
| `has_deger_kaynagi` kolonu bu pakette açıldı | Bertan: faturalar tablosuna kolon ekleyen migration TEK olacak, kolon üç değerli doğacak. Sıra 38→1 yerine 1 önce sevk edildiği için kolon burada doğdu; paket 38 kalan 3 kolonunu (ozel_matrah_kodu, has_gram, has_birim_fiyat) ekleyecek. |
| Ekranda "Fişten fatura kes" akışı yok | `/faturalar/fisten` kapsam dışı. `Islemler.razor`'a "Fişi görüntüle" bağlantısı da eklenmedi: fiş **belgesi** paket 5'in işidir; bu pakette yalnız veri ucu verildi (spesifikasyon: "Paket 5'in fiş belgesi bunu tüketir"). |
| `RaporModulu.cs` değiştirilmedi | Spesifikasyon "önce ÖLÇÜLÜR, gerekmiyorsa dokunulmaz" diyor: mutabakat `fatura_no` damgasına bakıyor, kalemli faturada damga aynen vuruluyor (canlı kanıt: fişin İKİ hareketi de `FTR-2026-000001`). Mevcut mutabakat/rapor testleri değişmeden yeşil. |
| M-F11 (hediye kalemi) mutasyonu yok | Hediye kalemi kuralı `/faturalar/fisten` yolunun kuralıdır; o uç Faz A'da yok. Yerine ek mutasyon M1-11 (kalem hareket sorgusundan kiracı şartının kaldırılması) uygulandı. |

**Mevzuat işaretleri:** KDVK md.23/(e) ve VUK md.230 spesifikasyonda **DOGRULANMALI**. Bu paket
kuralı değiştirmedi, hesabın yerini değiştirdi (elle giriş → sunucuda türetme). Varsayılan
`KdvTuru` **STANDART** kaldı; KUYUM bilinçli seçim. 23/e neden kodu (805) **yazılmadı**,
kolon açılmadı — paket 38'in işi. e-Fatura XML / sağlayıcı soyutlaması üretilmedi (ADR-0012).

## 3. Migration

- Ad: `20260916081339_FaturaKalemleri` (+ `.Designer.cs` + `KuyumcuDbContextModelSnapshot.cs`).
- `dotnet build` yeşilken, **`--no-build` kullanılmadan** üretildi. İlk üretimde varsayılan
  değerler `""` geldiği görüldü → model'e `HasDefaultValue("BASLIK"/"ELLE")` eklenip migration
  `remove` + yeniden `add` ile üretildi (kaynak: `TasarimZamaniFabrika` `KUYUMCU_BAGLANTI`
  ortam değişkenini okur).
- `kuyumcu_net`'e uygulandı:
  ```
  Applying migration '20260916081339_FaturaKalemleri'. Done.
  kalem_kaynagi      varchar(10) NO  BASLIK
  has_deger_kaynagi  varchar(10) NO  ELLE
  mevcut 8 fatura → kalem_kaynagi=BASLIK, has_deger_kaynagi=ELLE (geri doldurma gerekmedi)
  ```

## 4. Testler

| Koşum | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| `FaturaKdvTestleri` [saf] | 10 | 10 | 0 |
| `FaturaKalemTestleri` [DB] | 17 | 17 | 0 |
| `FisOkumaTestleri` [DB] | 5 | 5 | 0 |
| `FaturaEkranTestleri` [bUnit] | 6 | 6 | 0 |
| Regresyon (DuzeltmePolitikasi + Rapor/Fatura + Mutabakat) | 33 | 33 | 0 |
| **TAM SÜİT** (`--logger trx`) | **1180** | **1180** | **0** |

trx sayacı: `total="1180" passed="1180" failed="0"`.
**Yeni test sayısı: 38.** Süit öncesi: 1142 → sonrası: 1180.

## 5. Mutasyon kanıtları

| Kod | Değişiklik | Kırmızıya dönen test | Geri alındı (cmp) |
|---|---|---|---|
| M1-1 | Toplam eşitlik kapısı ±1 TL toleranslı | `FaturaKalemTestleri.Toplam_bir_kurus_sapinca_400_doner_ve_HICBIR_satir_yazilmaz` | ✓ |
| M1-2 | KUYUM'da has bedeli dto'dan okunur | `…Kuyum_kalemli_faturada_dto_HasDeger_YOKSAYILIR_kalemlerden_toplanir` | ✓ |
| M1-3 | Kur kaydı yoksa has bedeli 0 yazılır | `…Kuyum_kalemli_faturada_kur_kaydi_yoksa_400` | ✓ |
| M1-4 | Taş ibaresi kaleme yazılmaz (null) | `…Tas_ibaresi_kaleme_METIN_olarak_kopyalanir_ve_urun_degisse_de_DEGISMEZ` | ✓ |
| M1-5 | Damga yalnız verilen harekete vurulur | `…Kalemli_faturada_fisin_TUM_hareketleri_damgalanir` | ✓ |
| M1-6 | KDV kalemlerden değil belge toplamından hesaplanır | `…Uc_kalemli_faturada_KDV_kalemlerden_TOPLANIR_belge_uzerinden_degil` (+ `…dto_HasDeger_YOKSAYILIR`) | ✓ |
| M1-7 | "zaten faturalandırılmış" kontrolü kaldırılır | `…Ayni_fisten_ikinci_kalemli_fatura_400_doner` | ✓ |
| M1-8 | 200 kalem sınırı kaldırılır | `…Ikiyuzbir_kalem_400_doner` | ✓ |
| M1-9 | Kalemsiz yol da KalemYolu'na sokulur | `…Kalemsiz_istek_BUGUNKU_yolu_alir_kalem_kaynagi_BASLIK`, `…Kalemsiz_faturada_okuma_ucu_BOS_LISTE_ve_BASLIK_doner` | ✓ |
| M1-10 | Ekranda toplam uyuşmazlık kapısı kaldırılır | `FaturaEkranTestleri.Kalem_toplami_fatura_toplamiyla_UYUSMAYINCA_Kaydet_KAPALI` | ✓ |
| M1-11 | Kalem hareket sorgusundan kiracı şartı kaldırılır | `FaturaKalemTestleri.Yabanci_kiracinin_hareketine_baglanma_denemesi_404` | ✓ |
| M1-12 | Fiş okuma ucundan kiracı şartı kaldırılır | `FisOkumaTestleri.Yabanci_kiracinin_fis_id_si_404` | ✓ |

**İlk denemede yakalanan test açığı (Anayasa md.1):** M1-3'ün ilk koşumunda hiçbir test kırmızıya
dönmedi — çünkü servis kapısı kaldırılınca motor **aynı anlamda** ama farklı metinle 400 veriyordu
(çok katmanlı koruma tuzağı). Test, servis katmanının **kendi mesajını** (`"kalem için kur kaydı
bulunamadı"`) arayacak biçimde güçlendirildi ve mutasyon tekrarlandı → KIRMIZI.
Aynı şekilde M1-6'nın ilk denemesi de etkisiz çıktı (motorun kalem içi yuvarlaması hesabı
değiştirmiyordu); yeni bir DB testi (`Uc_kalemli_faturada_KDV_kalemlerden_TOPLANIR…`, 3 × 10,01 TL
→ 5,01 vs 5,00) yazılıp mutasyon servis toplamına taşındı → KIRMIZI.

Mutasyon sonrası tüm kaynaklar `cmp` ile birebir doğrulandı, ardından **temiz `dotnet build`**
(0 hata, 1 uyarı — `RotaBaslikTestleri.cs:156` xUnit1031, paket öncesinden var).

## 6. Denetim

```
═══ Kiracı izolasyonu denetimi ═══
  ✓ IgnoreQueryFilters (yasak)                     0
  ✓ new KuyumcuDbContext (yalnız fabrika)          1
  ✓ Find/FindAsync (yasak)                         0
  ✓ ExecuteUpdate/Delete (beyaz liste 4)           4
  ✓ Hareketler.Add (yalnız HareketFabrikasi)       1      ← fatura kalemi defter DEĞİL
  ✓ VeresiyeKapanisi (yalnız Etiketten)            2
  ✓ IKiraciVarlik uygulayan varlık                32
  ✓ HasQueryFilter                                 2
═══ Güvenlik yüzeyi ═══  (hepsi ✓)
DENETİM TEMİZ.            (çıkış 0)
```

`./yetim-tara.sh` → **çıkış 2**: `hareketler.cari_id` (1 satır) ve `kasa_hareketleri.kasa_id`
(1 satır) kiracı sızıntısı. **Bu paketten DEĞİL**: ilgili satırların tarihi `2026-09-09 21:19:12`
(bu oturumdan bir hafta önce, geliştirme veritabanındaki eski deneme verisi). Kanıt SQL'i §7'de.
Sessizce düzeltilmedi; §9'da Mehmet'e/Kurucu'ya not düşüldü.

## 7. Canlı doğrulama (localhost:5198 — 5199'a dokunulmadı)

Kiracı: `PAKET1-TEST` (`01M2MNAT…`). Kur kaydı beklendi (`/kur/guncel` → `altinHasFiyati` geldi).

| Adım | Beklenen | Gerçek |
|---|---|---|
| Kiracı + cari + 2 ürün (biri taş ibareli) + kasa + 2 alış | 200 | 200 |
| 2 kalemli fiş (`/islemler/satis-fisi`, KART 15.000) | 200 | 200, `fisId=01M2MNE3…` |
| `GET /islemler/fis/{fisId}` | kalemler + bacaklar defterle birebir | `kalemSayisi=2`, `toplamTl=15000`, kalemler (9.000 / 6.000), bacak `KART 15.000`, `kur.altinHasFiyati=6773,10`, `cariAd` var — telefon/adres/vergiNo YOK |
| Kalemli fatura (`POST /faturalar`, hareketId + 2 kalem) | 200, 2 satır, damga | 200 · `FTR-2026-000001` · `kalem_kaynagi=KALEM`, `has_deger_kaynagi=KALEM` |
| SQL: kalem toplamları = başlık | eşit | `toplam 15000,00 = Σsatir_toplam`, `kdv 2500,00 = Σkdv_tutar`, `has 0,00 = Σhas_deger`, kalem=2 |
| SQL: fişin hareketleri | ikisi de aynı fatura_no | `FTR-2026-000001 → 2 satır` |
| Aynı fişten ikinci fatura | 400 + mesaj | 400 · `Bu fişin kalemleri zaten FTR-2026-000001 ile faturalandırılmış` |
| Toplam 1 kuruş sapık | 400, satır sayısı DEĞİŞMEZ | 400 · `Kalem toplamı 15000.00 TL, gönderilen toplam 15000.01 TL — fark 0.01 TL. Kalemli faturada tolerans yoktur.` · sonra `faturalar=1, fatura_kalemleri=2` (değişmedi) |
| Kalemsiz (eski usul) fatura | 200, BASLIK, eski hesap | 200 · `FTR-2026-000002` · `kdvTutar=2000`, `araToplam=10000`, `kalemKaynagi=BASLIK`, `hasDegerKaynagi=ELLE` |
| KUYUM kalemli fatura (70.000 TL) | 200, has bedeli sunucudan | 200 · `FTR-2026-000003` · `hasDeger=61.990,57`, `kdvTutar=1.334,90`, `araToplam=68.665,10` |
| Has bedeli **perl ile bağımsız** hesap | birebir | `perl: has=61990.57 matrah=6674.53 kdv=1334.90 \| SQL: 61990.57 / 6674.53 / 1334.90 → EŞİT=EVET` (9,160000 g × 6.767,53 TL) |
| `GET /faturalar/{id}/kalemler` | sıra + ibare | 200 · `KALEM`, `[(1,'Burma Bilezik',None,9000),(2,'Tek Taş Yüzük','sentetik taşlıdır',6000)]` |
| Ürün kartının taş ibaresi değiştirildi | kalem DEĞİŞMEZ | ürün kartı → `laboratuvar üretimi taş içerir`; kalem hâlâ `sentetik taşlıdır` |
| 201 kalem | 400 | 400 · `Bir faturada en çok 200 kalem olabilir; 201 kalem gönderildi` |
| API durduruldu, test kiracısı silindi | tenants eski sayıya döner | `lsof :5198` boş · tenant_id'li 31 tablodan silindi · `tenants=5` (başlangıç 5) · `fatura_kalemleri=0` |

Kullanılan SQL (örnek):
```sql
select f.fatura_no, f.kalem_kaynagi, f.has_deger_kaynagi, f.toplam, f.kdv_tutar, f.has_deger,
       count(k.id), sum(k.satir_toplam), sum(k.kdv_tutar), sum(k.has_deger)
  from faturalar f left join fatura_kalemleri k on k.fatura_id=f.id
 where f.tenant_id='…' group by f.id;
select fatura_no, count(*) from hareketler where tenant_id='…' and fis_id='…' group by fatura_no;
-- sızıntının bu pakete ait OLMADIĞININ kanıtı:
select h.tarih from hareketler h join cariler c on c.id=h.cari_id
 where c.tenant_id <> h.tenant_id;      -- → 2026-09-09 21:19:12
```

## 8. Değişen dosyalar (`git status --porcelain`, bin/obj hariç)

Bu paketin dokunduğu dosyalar (çalışma ağacı zaten başka paketlerden kirliydi):
- `dotnet/Kuyumcu.Api/Data/FaturaKalemi.cs` **(YENİ)**
- `dotnet/Kuyumcu.Api/Modules/Fatura/OzelMatrahMotoru.cs` **(YENİ)**
- `dotnet/Kuyumcu.Api/Data/Migrations/20260916081339_FaturaKalemleri.cs` **(YENİ)**
- `dotnet/Kuyumcu.Api/Data/Migrations/20260916081339_FaturaKalemleri.Designer.cs` **(YENİ)**
- `dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs` (güncellendi)
- `dotnet/Kuyumcu.Api/Data/Entities.cs`
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs`
- `dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`
- `dotnet/Kuyumcu.Web/Pages/Faturalar.razor`
- `dotnet/Kuyumcu.Tests/FaturaKdvTestleri.cs` **(YENİ)**
- `dotnet/Kuyumcu.Tests/FaturaKalemTestleri.cs` **(YENİ)**
- `dotnet/Kuyumcu.Tests/FisOkumaTestleri.cs` **(YENİ)**
- `dotnet/Kuyumcu.Tests/FaturaEkranTestleri.cs` **(YENİ)**

## 9. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Bertan: tek motor (OzelMatrahMotoru), paket 1 çağırır | ✅ | `FaturaKdvHesabi.cs` yok; `KalemYolu` → `OzelMatrahMotoru.Hesapla` |
| Bertan: faturalar kolonları TEK migration, `has_deger_kaynagi` üç değerli | ✅ | `20260916081339_FaturaKalemleri` |
| Bertan: Faz B ayrı sevk | ✅ | fis_id yayılımı ve /fisten yapılmadı |
| Bertan: geriye uyum regresyon testi, mevcut gövdeye dokunma | ✅ | `Kalemsiz_istek_BUGUNKU_yolu_alir…`; KDV gövdesi (satır 130-151) değişmedi |
| Mehmet K1-1: hareket kümesi `TenantId` şartıyla, yabancı 404 testi | ✅ | M1-11 · `Yabanci_kiracinin_hareketine_baglanma_denemesi_404` (filtresiz bağlam) |
| Mehmet K1-2: sınırlar servis içinde elle, "201 kalem 400" testi | ✅ | `Ikiyuzbir_kalem_400_doner` (servis yolu) + `Ikiyuz_kalem_SINIRDA_kabul_edilir` |
| Mehmet K1-4: tek motor | ✅ | yukarıda |
| Mehmet K1-5: fiş yanıtı yalnız cari ad + id | ✅ | `Fis_yaniti_cari_adini_tasir_ama_kisisel_iletisim_bilgisi_TASIMAZ` + canlı JSON |
| Mehmet: HareketFabrikasi tekeli, denetim.sh TEMİZ | ✅ | `Hareketler.Add = 1`, DENETİM TEMİZ |
| Aşkın: `/fisten` Faz A'dan çıkarıldı | ✅ | §2 |
| Aşkın: sabitler önceden perl ile hesaplandı | ✅ | `FaturaKdvTestleri` (3 × 10,01 → 5,01 vs 5,00; 65.000/61.542,93) |
| Aşkın: bUnit `disabled` varlığına bakar, `WaitForAssertion` | ✅ | `FaturaEkranTestleri` |
| Aşkın: sahte API'ye yeni uç yanıtı, `catch (ApiHata or JsonException)` | ✅ | `Faturalar.razor` `KalemleriAc`, sahte API `/kalemler` |
| Aşkın: mevcut `new MFatura.FaturaServisi(db)` çağrısı değişmeden derlenir | ✅ | ctor'a bağımlılık eklenmedi; `RaporFaturaTestleri` yeşil |
| UX-S1 düzenleme yolu | ✅ | `DuzeltmePolitikasi` → `FaturaKalemi` SALT_OKUNUR |
| UX-S8/S9 boş durum + bantlar · UX-S12 TR+EN · UX-S16 ekran mutasyonu · K-UX-7 id sözleşmesi | ✅ | §1 Ekran, M1-10 |
| e-Fatura XML / 805 kodu kapsam dışı | ✅ | hiçbir XML/sağlayıcı/kod yazılmadı |

## 10. Açık kalanlar

1. **FAZ B** (bu pakette yapılmadı): `POST /faturalar/fisten` — önkoşulu `hareketler.adet`
   kolonudur (paket 39/42 de istiyor); fis_id'nin ALIS/HURDA_ALIS/TAKAS/ISCILIK'e yayılması
   ("iki ayrı alış aynı fis_id'yi ALMAZ" testiyle); hediye kaleminin faturaya 0 bedel + indirim
   girmesi; POS-fatura mutabakatının kalemli faturayla doğrulanması.
2. **Paket 38** kalan 3 kolonu (`ozel_matrah_kodu`, `has_gram`, `has_birim_fiyat`) ve hareket
   bazlı türetimi ekleyecek; `OzelMatrahMotoru` zaten hazır ve imzası uyumludur.
3. **Fiş belgesi ekranı** (`Islemler.razor` "Fişi görüntüle" + yazdırma) paket 5'e bağlıdır;
   veri ucu hazır.
4. **Kalemli faturada birim gösterimi** ("miktar 2,5 GRAM" mi, ayrıca gram_agirlik mı basılacak)
   belge tasarımıyla (paket 5) kararlaşacak.

## 11. Kurucuya / mali müşavire notlar

- **KDVK md.23/(e)** (has bedelinin özel matrah olarak matrah dışı bırakılması) ve
  **VUK md.230** (faturada bulunması gereken bilgiler) birincil metinleri **doğrulanmadı**.
  Kod bu hükmü **yeni getirmiyor**; bugün de uygulanan hesabın yerini elle beyandan sunucu
  türetmesine taşıyor. `KdvTuru` varsayılanı **STANDART** bırakıldı: KUYUM bilinçli seçimdir.
  Mali müşavir teyidi (K2) alınana kadar bu tutuculuk korunmalıdır.
- **e-Fatura 805 özel matrah neden kodu** bu pakette **yazılmadı**, kolon açılmadı; arayüzde
  "bu kod doğrudur" iddiası taşıyan hiçbir metin yok.
- **Güvenlik bulgusu (bu paketten değil):** geliştirme veritabanı `kuyumcu_net`'te
  `hareketler.cari_id` (1) ve `kasa_hareketleri.kasa_id` (1) kiracı sızıntısı var; ilgili
  satırların tarihi **2026-09-09**. Mehmet (06 Güvenlik) ve Kurucu bilgilendirilmelidir;
  bu oturumda **düzeltilmedi** (izinsiz veri değişikliği yapılmaz).
- **Kirlilik (bu paketten değil):** `dotnet/Kuyumcu.Api/Modules/Satis/OdemeSepeti.cs.mutorj`
  (15 Eylül'den kalma mutasyon artığı) hâlâ ağaçta; silmek Kurucu'ya bırakıldı (hook `rm` bloklar).


---

# EK — 2026-09-16 (ikinci dalga): `POST /faturalar/fisten` eklendi (bağlayıcı sapma kapatıldı)

> Uygulayıcı: Taşkın (backend) + Ertuğ (frontend) rolü · Dalga 1 · COMMIT YOK.
> Durum: **BİTTİ (FAZ A tamam)** · Derleme **0 hata 0 uyarı** · **1193/1193 test yeşil** ·
> `dotnet/denetim.sh` **TEMİZ** · canlı 5198'de doğrulandı, test kiracısı silindi.

## E1. Kapatılan bulgu (KRİTİK)

Önceki turda `POST /faturalar/fisten` Faz A'dan **çıkarılmıştı**; gerekçe Aşkın'ın
"adet kolonu yok" koşuluydu. Oysa spesifikasyonun **§ Birleşik koşullar → ÇELİŞKİ ÇÖZÜMÜ**
maddesi bu çelişkiyi ters yönde mühürlemiş:

> "uç Faz A'da KALIR ama miktar hareketten TÜRETİLMEZ — kaynak ölçülene dek **adet=1
> sözleşmesi** teste yazılır."

Bu turda uç bağlayıcı çözüme göre eklendi. **Mühürsüz sapma kaldırıldı.**

- `FaturaServisi.FistenOlustur` + `POST /api/v1/faturalar/fisten`
  (`[ModulYetkisi("fatura")]` sınıf kapısı + `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]`).
- **ADET=1 SÖZLEŞMESİ** koda sabit (`FaturaServisi.FistenKalemMiktari = 1m`) ve XML yorumuyla
  gerekçeli: `hareketler` tablosunda adet kolonu YOKTUR; miktar **uydurulmaz**. Kaynak kolon
  (paket 39/42) gelince değişecek **tek şey** miktarın okunduğu satırdır.
  Kalemin bedeli hareketin TL tutarının ta kendisidir → fatura toplamı = fiş toplamı.
- **FİŞİN TÜM KALEMLERİ GİRER** (B-4): damga kümesi yeni `FisHareketleriBul` +
  ortak `DamgaKontrol` ile kurulur; mevcut `DamgalanacakBul` gövdesi ve **mesaj metinleri
  korundu**, kopyalanmadı.
- **HEDİYE KALEMİ**: belgede liste fiyatıyla **görünür**, indirim satırı `liste × adet`
  kadardır, **net bedeli 0**'dır; KDV 0. Defter sözleşmesi varsayılmadı, `SatisModulu.cs`
  (hediyede `tl_tutari=0`, `liste_fiyati`, `indirim_tutari = liste × adet`) **ölçüldü**.
  Defterde liste yoksa **0 yazılır, uydurulmaz**; hediye-dışı kalemde satır toplamı ≤ 0 → 400.
- **KUYUM**: has bedeli **defterden** türetilir (`|hareket.has_grami| × kur kaydındaki
  altin_has_fiyati`), tek motor `OzelMatrahMotoru` üzerinden; kur kaydı yoksa **fail-closed 400**.
  İstemci has bedeli **gönderemez** (DTO'da alan yok).
- **KİRACI**: hareket kümesi `h.TenantId == islem.TenantId && h.FisId == …`; yabancı kiracının
  fişi **404** ve mesajı `"Fiş bulunamadı"` olarak sınanır (aşağıdaki mutasyon notuna bakınız).
- **Sınırlar servis içinde elle**: fiş 200 kalemi aşarsa 400 (DataAnnotations servis yolunda
  çalışmaz — Mehmet K1-2).
- Ekran (Ertuğ): araç çubuğunda `#ff-ac` → **"Fişten Fatura Kes"** modalı:
  `#ff-fis` (fiş kimliği) → `#ff-onizle` **önizleme** (GET `/islemler/fis/{fisId}`, ikinci bir
  belge ucu AÇILMADI) → `#ff-kalem-tablosu` + `#ff-toplam` → `#ff-kaydet`.
  Önizleme yapılmadan ya da fiş zaten faturalıysa **Kaydet KAPALI**. Boş durum + `HataBandi` +
  `BasariBandi` var; yeni CSS yazılmadı (sabit renk/satır içi stil sorunu doğmadı);
  mevcut element id'leri **değişmedi**, yeni id'ler `ff-` önekli.
- `Sozlukler.cs`: 5 yeni anahtar **TR + EN** (`FistenKes`, `FisKimligi`, `Onizle`,
  `FisKalemSayisi`, `FisOnizlemeIpucu`).
- `OzelMatrahMotoru.Yuvarla6` `public` yapıldı (kalem has gramının aynı yuvarlamayı
  kullanması için); hesap gövdesi değişmedi.

## E2. Migration

**GEREKMEDİ.** Yeni kolon/tablo yok: `fatura_kalemleri` ve `faturalar.kalem_kaynagi` /
`has_deger_kaynagi` bu paketin ilk turunda `20260916081339_FaturaKalemleri` ile açılmış ve
`kuyumcu_net`'e uygulanmıştı. `dotnet ef` bu turda **çalıştırılmadı**.

## E3. Testler

| Koşum | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| `FaturaFistenTestleri` (YENİ, DB) | 9 | 9 | 0 |
| `FaturaEkranTestleri` (4 yeni bUnit dâhil) | 10 | 10 | 0 |
| **Tam süit (sonuç koşumu)** | **1193** | **1193** | **0** |

Yeni test sayısı: **13** (9 DB + 4 bUnit). Önce 1180 → sonra 1193.

**Kararsızlık kaydı (Anayasa md.1 — gizlenmedi):** tam süitin ilk koşumunda
`KarmaOdemeEkranTestleri.Satistan_alisa_gecince_karma_kipi_KAPANIR` **1 kez** kırmızı geldi
(`Assert.Empty() Failure: Collection was not empty`, `KarmaOdemeEkranTestleri.cs:257`).
Bu dosya bu paketin kapsamında DEĞİL ve bu turda değiştirilmedi. Yalıtılmış 3 koşum + tam
süitin ikinci koşumu **yeşil** → bilinen bUnit kararsızlığı
(`SONRAKI-OTURUM-PROMPT.md` §6'daki "adı yakalanamayan kararsız test" kaydının adı artık
belli). Orkestratöre/QA'ya not: bu test `WaitForAssertion` ile sağlamlaştırılmalı.

## E4. Mutasyon kanıtları (bu tur)

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M1F5 | Fişten faturada damga yalnız **ilk** harekete vurulur | `Iki_kalemli_fisten_fatura_iki_kalem_yazar_ve_IKI_hareket_de_ayni_fatura_no_alir` | `cmp` AYNI |
| M1F7 | "zaten faturalandırılmış" kontrolü kaldırılır | `Ayni_fisten_ikinci_fatura_400_…`, `Fisin_bir_kalemi_elle_damgalanmissa_…`, `FaturaKalemTestleri.Ayni_fisten_ikinci_kalemli_fatura_400_doner` | `cmp` AYNI |
| M1F11 | Hediye kaleminde indirim satırı yazılmaz | `Hediyeli_fisten_faturada_hediye_kalemi_0_bedel_ve_indirim_liste_kadar_girer` | `cmp` AYNI |
| M1A1 | `FistenKalemMiktari = 2` (adet sözleşmesi bozulur) | `Fisten_faturada_kalem_miktari_BIRDIR_adet_sozlesmesi` + 4 test | `cmp` AYNI |
| M1F12A | Fiş sorgusundan `TenantId` şartı kaldırılır | `Yabanci_kiracinin_fis_id_si_404` | `cmp` AYNI |
| M1F3A | Has bacağının birim fiyatı kur yerine 1 TL yazılır | `KUYUM_fisten_faturada_has_bedeli_defterden_turetilir_istemci_gonderemez` | `cmp` AYNI |
| M1F8A | 200 kalem sınırı kaldırılır | `Ikiyuzbir_kalemli_fisten_fatura_400` | `cmp` AYNI |
| M1UX1 | Ekranda Kaydet kapısı kaldırılır | `Onizleme_YAPILMADAN_fisten_Kaydet_KAPALI`, `Zaten_faturalanmis_fiste_Kaydet_KAPALI` | `cmp` AYNI |

**TEST AÇIĞI bulundu ve kapatıldı (Aşkın'ın "çok katmanlı koruma" tuzağı, iki kez):**
M1F12A ilk denemede **hiçbir testi kırmadı**. Sebep (1) test kiracıya sabitlenmiş bağlamda
koşuyordu → global `HasQueryFilter` zaten 404 üretiyordu; sebep (2) bağlam sisteme alınınca da
**"Cari bulunamadı"** kapısı 404 üretiyordu. Test `vt.OlusturSistem()` bağlamına taşındı **ve
hata mesajı `"Fiş bulunamadı"` olarak sınandı**; ancak ondan sonra mutasyon kırmızıya döndü.

Son mutasyondan sonra temiz derleme: **0 hata, 0 uyarı**.

## E5. Denetim

```
./denetim.sh → DENETİM TEMİZ
  Hareketler.Add (yalnız HareketFabrikasi)   1
  ExecuteUpdate/Delete (beyaz liste 4)       4
  IgnoreQueryFilters / Find / FindAsync      0 / 0
  IKiraciVarlik uygulayan varlık            32
```
Fatura kalemi **defter değildir**: `fatura_kalemleri` yazımı `Hareketler.Add` sayacını
değiştirmedi (1'de kaldı).

## E6. Canlı doğrulama (localhost:**5198** — 5199'a DOKUNULMADI)

Kiracı `PAKET1-TEST-FISTEN` (`01M2MQCYDBHAEMR5BC1ZFCVJVZ`), kur beklendi
(`altinHasFiyati = 6768,98`).

| Adım | Beklenen | Gerçek |
|---|---|---|
| Kiracı + cari + kasa + 2 ürün (biri taş ibareli) + 2 alış | 200 | 200 |
| 2 kalemli satış fişi (70.000 + 13.000, KART 83.000) | 200 + fisId | 200 · `fisId=01M2MQF81EEM4TX4FZ1B3X6XCG` |
| `POST /faturalar/fisten` (KUYUM, %20) | 200, kalemli fatura | 200 · `FTR-2026-000001` · `toplam=83000.00` `kdvTutar=1651.42` `hasDeger=73091.45` `kalemKaynagi=KALEM` `hasDegerKaynagi=KALEM` |
| SQL: başlık = kalem toplamları | eşit | `83000.00 = Σsatir_toplam` · `1651.42 = Σkdv_tutar` · `73091.45 = Σhas_deger` · kalem=2 |
| SQL: kalem alanları | miktar 1, ibare kopya | `(1,'Burma Bilezik',1.000,K22,9.160000,NULL,70000.00,6663.45,1332.69,62003.86)` · `(2,'Tek Tas Yuzuk',1.000,K14,1.638000,'sentetik taslidir',13000.00,1593.68,318.73,11087.59)` |
| SQL: fişin hareketleri | ikisi de aynı fatura_no | `FTR-2026-000001 → 2 satır` |
| Has bedeli **perl ile bağımsız** hesap | birebir | `perl: 9,16 × 6768,98 = 62003.86` · `1,638 × 6768,98 = 11087.59` → **SQL ile EŞİT** |
| Aynı fişten ikinci fatura | 400 + mesaj | 400 · `Bu fişin kalemleri zaten FTR-2026-000001 ile faturalandırılmış` |
| `GET /faturalar/{id}/kalemler` | sıra + ibare + KDV | 200 · `kalemKaynagi=KALEM`, kalem 1/2 alanları yukarıdaki SQL ile aynı |
| `GET /islemler/fis/{fisId}` | kalemler + kur + cari **adı** | 200 · `kalemSayisi=2`, `toplamTl=83000.00`, `kur.altinHasFiyati=6768,98`, `cariAd="Fisten Musteri"` — telefon/adres/vergiNo YOK |
| Hediyeli fişten fatura (STANDART) | hediye 0 bedel, toplamı büyütmez | 200 · `FTR-2026-000002` · `toplam=70000.00` · kalemler: `(1,70000.00,—,70000.00)` ve `(2,0.00,0.00,0.00,kdv 0.00)` |
| Kalemsiz fişten STANDART fatura (15.000) | eski hesapla aynı KDV | 200 · `FTR-2026-000003` · `kdvTutar=2500.00`, `araToplam=12500.00` |
| API durduruldu, test kiracısı silindi | tenants eski sayıya döner | `lsof :5198` **boş**, `lsof :5199` boş · tenant_id'li tüm tablolardan silindi · `tenants: 6 → 5` · `fatura_kalemleri(test kiracısı)=0` |

⚠ **Canlı hediye kaydında `liste_fiyati` NULL geldi** (kiracıda fiyat listesi tanımlı değil):
kalem 0 bedel + indirim `0.00` yazıldı — **uydurulmadı**. `indirim = liste × adet` sözleşmesi
DB testiyle (defterde liste dolu) kanıtlandı.

## E7. Bu turda değişen dosyalar

- `dotnet/Kuyumcu.Api/Modules/Fatura/FaturaModulu.cs` (FaturaFistenDto, FistenOlustur,
  FisHareketleriBul, DamgaKontrol, controller ucu)
- `dotnet/Kuyumcu.Api/Modules/Fatura/OzelMatrahMotoru.cs` (`Yuvarla6` public)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs` (5 anahtar TR+EN)
- `dotnet/Kuyumcu.Web/Pages/Faturalar.razor` (fişten fatura kes akışı)
- `dotnet/Kuyumcu.Tests/FaturaFistenTestleri.cs` **(YENİ)**
- `dotnet/Kuyumcu.Tests/FaturaEkranTestleri.cs` (sahte API `/islemler/fis/` yanıtı + 4 test)

## E8. Koşul karşılama (bu tur)

| Koşul | Durum | Kanıt |
|---|---|---|
| ÇELİŞKİ ÇÖZÜMÜ: uç Faz A'da kalır, adet=1 sözleşmesi teste yazılır | ✅ | `FistenKalemMiktari`, `Fisten_faturada_kalem_miktari_BIRDIR…`, M1A1 |
| Kapsam § İçinde 3. madde (POST /faturalar/fisten) | ✅ | canlı 200 + SQL |
| "FİŞİN TÜM KALEMLERİ GİRER" | ✅ | canlı `FTR-2026-000001 → 2 satır`, M1F5 |
| "HEDİYE KALEMİ 0 BEDEL + indirim" | ✅ | DB testi + canlı; M1F11 |
| Ekran "Fişten fatura kes" önizleme → Kaydet | ✅ | `#ff-ac/#ff-fis/#ff-onizle/#ff-kaydet`, 4 bUnit testi, M1UX1 |
| Bertan vetosu: tek motor (FaturaKdvHesabi YASAK) | ✅ | `/fisten` de `OzelMatrahMotoru.Hesapla` çağırır; `FaturaKdvHesabi.cs` yok |
| Mehmet K1-1 kiracı + 404 testi | ✅ | M1F12A (test açığı kapatıldıktan sonra kırmızı) |
| Mehmet K1-2 servis içi elle doğrulama | ✅ | 200 kalem sınırı + `Ikiyuzbir_kalemli_fisten_fatura_400`, M1F8A |
| HareketFabrikasi tekeli, denetim.sh TEMİZ | ✅ | `Hareketler.Add = 1`, DENETİM TEMİZ |
| Geriye uyum: kalemsiz başlık yolu bit bit aynı | ✅ | mevcut regresyon testleri + 1193/1193 |
| e-Fatura XML / 805 kodu kapsam dışı | ✅ | hiçbir XML/sağlayıcı/kolon/kod yazılmadı |
| Fiş için AYRI TABLO açılmadı | ✅ | fiş = aynı `fis_id`'yi taşıyan hareket kümesi |

## E9. Açık kalanlar (güncel)

1. **FAZ B** (ayrı sevk — Bertan'ın koşulu): `fis_id`'nin ALIS / HURDA_ALIS / TAKAS / ISCILIK
   akışlarına yayılması ("iki ayrı alış aynı fis_id'yi ALMAZ" testi + mutasyon) ve POS–fatura
   mutabakatının kalemli faturayla doğrulanması.
2. **`hareketler.adet`** kolonu (paket 39/42): geldiğinde `FistenKalemMiktari` yerine defterdeki
   adet okunacak — değişecek **tek satır** işaretlidir.
3. **Paket 38**: kalan 3 kolon (`ozel_matrah_kodu`, `has_gram`, `has_birim_fiyat`) ve 805 neden
   kodu; motor hazır.
4. **Kararsız test** `KarmaOdemeEkranTestleri.Satistan_alisa_gecince_karma_kipi_KAPANIR`
   (bu paketten değil) — QA sağlamlaştırmalı.
5. **Ağaç kirliliği (bu paketten değil):** 16 adet `*.mutorj` (15 Eylül) ve 4 adet sıfır baytlık
   `…_FiyatListesiVeKampanya` migration dosyası duruyor; `rm` hook tarafından bloklanıyor →
   **Kurucu silmeli**.
6. **Kiracı sızıntısı (bu paketten değil):** `kuyumcu_net`'te 2026-09-09 tarihli 2 satır;
   Mehmet (06) ayrı iş olarak ele almalı.

## E10. Kurucuya / mali müşavire notlar

- **KDVK md.23/(e)** ve **VUK md.230** birincil metinleri hâlâ **DOGRULANMADI**. Bu tur
  kuralı değiştirmedi; yalnız kuyum KDV'sinin has bedelini **elle beyandan defter türetmesine**
  taşıdı (sıkılaşma yönünde). `KdvTuru` varsayılanı **STANDART**; KUYUM bilinçli seçimdir.
  Mali müşavir teyidi (K2) alınmadan KUYUM varsayılan yapılmamalıdır.
- **Hediye/promosyon** (KDV GUT I/B-10.1, KDVK md.27): hediye kalemi faturada **görünür** hale
  geldi; emsal bedel/KDV tarhı gerekip gerekmediği **doğrulanmadı** — bu pakette hediye kalemi
  için KDV hesaplanmıyor (0). Mali müşavir teyidi gerekir.
- **e-Fatura 805 neden kodu** yazılmadı; arayüzde "e-Fatura hazır" iddiası taşıyan hiçbir metin yok.
