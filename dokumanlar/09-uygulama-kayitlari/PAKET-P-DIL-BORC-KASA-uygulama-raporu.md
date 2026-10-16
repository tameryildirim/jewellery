# PAKET P-DIL-BORC-KASA — uygulama raporu, TUR 2 (11.10.2026 · şerit t5 · API 5166 · vekil 5167 · CDP 9512–9513)

Kapsam: ekran denetimi 10.10 (`EKRAN-DENETIM-KULLANICI-GOZU-2026-10-10.md`) Y7, Y8, Y18, O21.
Rol: Taşkın (backend + sözlük) ve Ertuğ (ekran). COMMIT YOK. Tur 1 raporunun içeriği § 10'da özetlendi; bu sürüm onun yerini alır.

## 0. Tur 2'de kapatılan bulgular

| Bulgu (doğrulayıcı / tasarım hakemi) | Durum | Ne yapıldı | Kanıt |
|---|---|---|---|
| KRİTİK: Kasalar (Yıldız) etiket ile Kart/Havale/Toplam satırı üst üste biniyordu (K9=1, 8/8) | KAPANDI | Kök neden: Yıldız ızgarası `#kasa-liste .kart-govde > p.metin-kucuk` öğelerinin hepsini `ayrim` alanına koyuyordu. Bu yüzden yeni `#kasa-buyuk-etiket-*` ile `#kasa-ayrim-*` aynı hücreye düştü. Etiket artık kendi alanında (`etiket`), büyük rakamın üstünde duruyor. Ayrım satırı solda ikinci satırda. ≤767 px'te tek sütun. | Yıldız `/kasalar` "Tüm ayrıntılar" 8/8 kombinasyon: K9 = 0. Görüntü: `yildiz-karanlik-BASIT-1280-kasalar-ayrinti.png` |
| KRİTİK + ORTA: Y18 emanet has diliyle gösteriliyordu ("11,4500 has") | KAPANDI | Şema: `hareketler` tablosuna `emanet_gram`, `emanet_tas` ve `emanet_ayar_id` eklendi (migration `EmanetBirakildigiGibi`). Giriş, iade ve storno bu izi yazıyor. Ekranlar bırakıldığı gibi gösteriyor: "22 ayar · 12,50 g". Has yalnız ikincil (PRO). İade ayar ve gramla giriliyor. **Hesap değişmedi**: bakiye ve kapılar yine has üzerinden işliyor. | § 3, § 5, § 7 |
| ORTA: O21 kalan terimler | DEVİR | Sahiplerine iş kalemi olarak yazıldı (§ 9). | — |
| DÜŞÜK: Tezgâh "Kart · Ana Kasa" | DEVİR | YILDIZ-TEZGAH-ODEME (t7) dosyası (`YildizTezgahVerisi.cs:12-13`, ödeme kaydı). Dokunulmadı (§ 9). | — |
| DÜŞÜK: `BorcDiliSozlugu.Durum()` üretimde kullanılmıyordu | KAPANDI | İşaret kuralı tek kaynağa alındı: `BorcDiliSozlugu.Yon(bakiye, hane)`, yuvarlanmış değerden yön seçer. Yeni overload `Durum(bakiye, hane, biçim, dil)` eklendi. Dört çağrı yeri bunları çağırıyor: Tezgâh üst satırı `YildizTezgahBicim.MusteriDurumu` → `Durum`; `FisBakiyeRozeti` → `Yon`; `FisProYanSutun` → `Yon`; `Cariler.YonMetni/YonRengi` → `Yon`. | Mutasyon M10-1 (çağrı yeri) → 8 kırmızı |
| DÜŞÜK: Yıldız Müşteriler 375 px'te "1" ile "BİZ BORÇLUYUZ" ayrı satıra düşüyordu | KAPANDI | Üst satırın her " · " parçası `span.yz-ust-parca` içinde ve `white-space: nowrap`. TextContent değişmedi. | `yildiz-aydinlik-PRO-375-_cariler.png`: "6 MÜŞTERİ · 2 BİZE BORÇLU · / 1 BİZ BORÇLUYUZ" |
| DÜŞÜK: Platin Tezgâh rozeti işareti yuvarlamadan seçiyordu (0,0004 g has → "bize borçlu: 0,000 g has") | KAPANDI | Rozet yönü gösterim hassasiyetinden seçiliyor: has 3 hane, para 2 hane. | PDBK_18: 0,0004 g → "Borç yok"; 0,0005 g → "bize borçlu: 0,001 g has" |
| DÜŞÜK: Spesifikasyonda "Birleşik koşullar" bölümü yok | NOT | Doğrulandı: bölüm yok. Kabul, görev istemindeki EK KOŞULLAR'a ve Y7/Y8/Y18/O21 çözüm satırlarına göre ölçüldü. Orkestratör kapı kaydına yazmalı. | — |

## 1. Spesifikasyonun koda karşı doğrulanması (sapmalar)

- **Y18 tam çözümü şema ister.** Tur 1'de doğrulandı:
  - `Hareket` varlığında gram ve ayar kolonu yoktu.
  - `EmanetGirisDto` AyarId ve GramAgirlik alıyordu ama saklamıyordu.
  - Tasarım hakemi KRİTİK verdiği ve görev "önce bunları kapat" dediği için migration prosedürü uygulandı.
  - DOSYA ALANI sapması bilinçlidir. Dışına çıkılan dosyalar:
    - `Entities.cs`, migration
    - `EmanetModulu.cs`, `StornoServisi.cs` (2 satır)
    - `CariModulu.cs` (liste yanıtına 1 alan)
    - `Contracts` (2 kayıt)
    - `Emanet.razor`, `Emanet.Yildiz.cs`, `YildizMusteriVerisi.cs`, `YildizMusteriler.razor`
    - `FisBakiyeRozeti.cs`, `FisProYanSutun.razor`, `YildizTezgahVerisi.cs` (bulgu emri)
  - Hepsi bulgu satırlarının istediği yerler. Gerekçe § 0'da.
- **Tuval Y_Emanet "Ne bıraktı" sütunu** "22 ayar · 12,5 g · taş 0,5 g" yazar. Bu yüzden brüt gram ve taş ayrı saklanır. Ayar başına kalan, net (brüt − taş) üzerinden hesaplanır, çünkü has netten hesaplanır.
- **Eski satırlar** (kolondan önce) NULL kalır. Bir kişide ayarı bilinmeyen has ≠ 0 ise ya da başka ayarla iade yapıldıysa (ayar kalanı eksiye düşer), özet `null` döner ve ekran eski has gösterimine düşer. Uydurma yok, Anayasa md.1.
- **Geriye uyum:** eski istemci (ör. `KesifP2HttpTestleri`) iade gövdesine `ayarId` koyup gram koymuyordu. Bu alan o gün yoktu ve yok sayılıyordu. Gram gönderilmedikçe eski yol aynen sürer. İlk denemede 400 veriyordu, düzeltildi.
- **İade has toleransı 0,001 g:** "tümünü iade et" kalan has'ı birebir gönderir. Bu değer satır satır yuvarlanmış has toplamıdır ve toplam gramdan hesaplanandan milyonda birkaç sapabilir. Emanet tam 0'a kapanır (canlı: Tuna 7,5 g → `birakildigiGibi: []`, `mevcutHasGramBakiye: 0`). Mevzuat değil, yuvarlama toleransı.

## 2. Yapılanlar (tur 2)

**Sunucu**
- `Data/Entities.cs` → `Hareket.EmanetGram` (decimal(15,3)?, işaretli), `EmanetTas` (decimal(15,3)?), `EmanetAyarId` (varchar(10)?).
- `Modules/Emanet/EmanetModulu.cs`:
  - Giriş izi yazar.
  - `EmanetCikisDto.AyarId` ve `[Gram] GramAgirlik` (isteğe bağlı çift) eklendi. Verilirse has sunucuda aynı fonksiyonla hesaplanır (`EmanetHas`; tek `HasHesap.ToHasGram` çağrı yeri, beyaz liste 4 korunur). Sapma 0,001'i aşarsa 400. Maden ayardan gelir.
  - `GET /emanet/{cariId}` yanıtına `birakildigiGibi` eklendi (altın, şube kapsamlı).
  - `EmanetKurali.BirakildigiGibi(...)`: tek gruplu sorgu (TB-19).
- `Modules/Satis/StornoServisi.cs`: storno satırı izi ters işaretle taşır (`EmanetGram`, `EmanetTas`, `EmanetAyarId`).
- `Modules/Cari/CariModulu.cs` `Listele`: her satıra `emanetBirakildigi` eklendi (tek ek sorgu).
- `Kuyumcu.Core/Domain/EmanetBirakildigi.cs` (yeni): saf kural `Ozetle` ve kayıtlar `EmanetIz` / `EmanetAyarSatiri`.
- `SunucuIletiSozlugu.Veri.cs`: 3 yeni sunucu iletisinin TR/EN çifti.

**Sözlük** (`Sozlukler.BorcDili.cs`)
- `BorcDiliSozlugu.Yon` ve `Durum(bakiye, hane, biçim, dil)`. Eski `Durum` da `Yon`'a bağlandı.
- `EmanetDiliSozlugu` (yeni): `Gram` ("12,50 g", sayı ile birim arası U+00A0), `AyarAdi` ("22 ayar" / "22K", U+00A0), `AyarGram`, `Satir` (taşlı), `Parcalar`, `Emanette`, `EmanetteDuz`, `IadeGram`, `IadeAyar`, `HasIkincil`.

**Ekran**
- `Pages/Emanet.razor` (Platin; Yıldız'da "ayrıntı"/"Geri ver" aynı çekmeceyi açar):
  - Özet `#e-ozet-deger` "22 ayar · 12,50 g". Yeni `#e-ozet-has` yalnız PRO'da: "= 11,4500 has".
  - İade çekmecesi, ayar biliniyorsa:
    - "Elimizdeki emanet: Altın: 22 ayar · 12,50 g".
    - Tek ayarda seçici yok, `#c-ayar-sabit` (UX-S22). Çok ayarda `c-ayar` yalnız kalan ayarları listeler.
    - `c-gram` "Geri verilen gram" zorunlu, varsayılanı kalanın tamamı.
    - Has alanı (`c-has`) yok. Hesaplanan has yalnız PRO'da `#c-hesaplanan`.
    - Fazla gram alan altında `#c-gram-fazla` ve Kaydet kilitli.
    - POST gövdesi `ayarId`, `gramAgirlik`, `hasGrami` taşır.
  - Ayar bilinmiyorsa eski ekran AYNEN kalır. Eski id'lerin hiçbiri değişmedi.
- `Pages/Emanet.Yildiz.cs`: "Ne bıraktı" sütunu tuval kalıbında. Örnek: "22 ayar · 12,50 g · taş 0,50 g"; açıklama varsa önde.
- `Pages/Cariler.razor`:
  - Liste satırı `#cari-liste-emanette-*` "Emanette 22 ayar · 12,50 g".
  - Detay "Emanette" kartı aynı (ekstre has'ı ile tutuyorsa).
  - Yön rozeti `Yon` ile.
- `Bilesenler/Yildiz/YildizMusteriVerisi.cs`: durum "emanette 22 ayar 12,50 g".
- `Bilesenler/Yildiz/YildizMusteriler.razor`: üst satır parçaları bölünmez.
- `wwwroot/css/tema-yildiz.css` (P-DIL-BORC-KASA bloğu): Kasalar ızgara alanları ve üst satır parçası. Renk yok.

## 3. Migration
- Ad: `20261011003451_EmanetBirakildigiGibi`.
- Prosedür: `dotnet build Kuyumcu.sln` YEŞİL alındıktan sonra `dotnet ef migrations add` (build'li) koşuldu.
- `.cs` dosyası 1.469 bayt, `.Designer.cs` 260.230 bayt. `Up()` yalnız 3 `AddColumn` içeriyor (`emanet_ayar_id` varchar(10), `emanet_gram` ve `emanet_tas` decimal(15,3), hepsi nullable). Başka şeridin şema kayması yok.
- `dotnet ef database update --connection …(parola değişkenden)` sonucu: "Applying migration '20261011003451_EmanetBirakildigiGibi'. Done."
- kuyumcu_net doğrulaması: `__EFMigrationsHistory` son satırı `20261011003451_EmanetBirakildigiGibi`; `SHOW COLUMNS … LIKE 'emanet%'` → 3 kolon, YES/NULL.
- Yeni varlık yok; `hareketler` tablosu zaten kiracı süzgeçli ve indeksli.

## 4. Testler

`Kuyumcu.Tests/PDilBorcKasaTestleri.cs`: tur 1'den 14 koşum, tur 2'de 14 koşum eklendi, toplam **28 koşum**. Filtreli koşum (`~PDilBorcKasa`, doğrulayıcının `PDILBORCKASASinirTestleri` dahil): **45/45 Başarılı**.

| Test | Tür | Ne sınar |
|---|---|---|
| PDBK_14 | saf | ayar başına kalan net gram (iade, taş, iki ayar sırası, tam iade → boş) |
| PDBK_15 | saf | eski satır / ayarsız iade / başka ayarla iade → `null` (uydurma yok) |
| PDBK_16 | saf | emanet dili: "22 ayar · 12,50 g", taş, EN "22K", gümüş, satır kalıpları; "has" yok |
| PDBK_17 | saf | Yıldız "Ne bıraktı" hücresi (tuval kalıbı, açıklama önde, eski satır) |
| PDBK_18 | saf | tek işaret kuralı `Yon` sınırları; Platin rozeti 0,0004 → Borç yok, 0,0005 → 0,001; Tezgâh −0,004 ₺ → Borç yok |
| PDBK_19 | DB | giriş izi (12,5 · K22 · 11,45 has); `GET /emanet` `birakildigiGibi`; `GET /cari` `emanetBirakildigi`; has DEĞİŞMEDİ |
| PDBK_20 | DB | iade ayar+gramla (kalan 7,5 g); has uyuşmazsa 400 ve deftere yazılmaz; gram var ayar yok → 400; eski yol (yalnız has) çalışır, özet `null` |
| PDBK_21 | DB | storno izi ters işaretle (−12,5 / −0,5 / K22); özet boş |
| PDBK_22 (BASİT, PRO) | bUnit | özet "22 ayar · 12,50 g"; `#e-ozet-has` yalnız PRO |
| PDBK_23 | bUnit | iade çekmecesi: `#c-ayar-sabit`, `c-has` yok, POST `ayarId` K22 · `gramAgirlik` 12,5 · `hasGrami` 11,45 |
| PDBK_24 | bUnit | kalandan fazla gram: alan altı uyarı, Kaydet kilitli, POST yok |
| PDBK_25 | bUnit | ayar bilinmiyorsa eski has ekranı ve `c-has` korunur |
| PDBK_26 | saf | Yıldız müşteri satırı "emanette 22 ayar 12,50 g"; bilinmiyorsa eski metin |

İlgili sınıflar ayrıca koşuldu: Emanet, Cari, Storno, Fis, Yıldız, Kasa, TS2, S23, SATIS3, TM1, CiftBacak, P12, UX4c, S35, Müşteri, Tezgâh. Toplam 2974 test; ilk koşumda 4 kırmızı çıktı, ikisi bu paketindi:
- `TS2TuvalProTestleri.Musteri_karti_kucuk_bakiye…`: `FisProYanSutun` has'ı 2 haneye yeniden yuvarlıyordu. Değer çağıranda zaten yuvarlı, `Yon(…, 6)` yapıldı. Yeşil.
- `KesifP2HttpTestleri.CariEkstre…`: geriye uyum, § 1. Yeşil.
- `KarmaOdemeEkranTestleri.Karma_fis_TEK_POST…`: zamanlama. Tekrar koşumda yeşil.
- `RD_KH02`: başkasının, § 9.

**Tam süit** (son kodla, bir kez; `/private/tmp/kuyumcu-serit/trx-pP-DIL-BORC-KASA/_Mac_2026-10-11_04_31_02.trx`): **12623 test · 12618 geçti · 2 başarısız · 3 atlandı**.
- Kırmızılar: `RaporDortTestleri.RD_KH01` ve `RD_KH02`. İkisi de tur 1 devir listesinde var (rapor paketi, tarih bağımlı).
- **Yeni kırmızı = 0.**
- Tur 1'deki `Y5A_04` ve `Yasal4Http Mutabakat…` bu koşumda yeşil.

Test sayısı: tur 1 sonu 12569 → tur 2 sonu 12623. Bu paketin payı +14. Kalan fark eşzamanlı şeritlerin testleri.

## 5. Mutasyon kanıtları
Yöntem: `/private/tmp/kuyumcu-serit/P-DIL-BORC-KASA/mut.zsh`. Adımlar: yedek → `perl -0777` tek eşleşme → build (0 Hata) → `~PDilBorcKasa` → geri kopya → `cmp`.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M6-1 | `EmanetBirakildigi.Ozetle`: eksi kalan / tutarsız iz kontrolü kaldırıldı (`if (false)`) | PDBK_15 | AYNI |
| M7-1 | `EmanetModulu.Giris`: `EmanetGram = null` (iz yazılmıyor) | PDBK_19, PDBK_20, PDBK_21 | AYNI |
| M8-1 | `StornoServisi`: `EmanetGram = kaynak.EmanetGram` (işaret terslenmiyor) | PDBK_21 | AYNI |
| M9-1 | `EmanetModulu.Cikis`: tolerans `> 1000m` (has uyuşmazlığı geçiyor) | PDBK_20 | AYNI |
| M10-1 | **Çağrı yeri** `YildizTezgahBicim.MusteriDurumu`: `Durum(-c.TlBakiye, …)` | PDBK_02 + doğrulayıcının 7 sınır testi (`Kurus_yuvarlamasi_tezgah_durumu` ×4, `Eksi_bakiye…`, `Ingilizce_kalip…`, `Cok_buyuk_tutar…`) | AYNI |
| M11-1 | **Çağrı yeri** `FisBakiyeRozeti`: has yönü `Yon(…, 6)` (yuvarlamasız) | PDBK_18 | AYNI |
| M12-1 | `Emanet.razor`: `IadeBirakildigi => false` (iade has diline döner) | PDBK_23, PDBK_24 | AYNI |
| M13-1 | `BorcDiliSozlugu.Yon`: `Math.Sign(bakiye)` (yuvarlama yok) | PDBK_18 | AYNI |
| M14-1 | `Emanet.razor` özet: `if (OzetBirakildigi)` → `if (false)` | PDBK_22 (BASİT), PDBK_22 (PRO) | AYNI |

Tur 1'deki M1–M5 (5/5 kırmızı) geçerliliğini koruyor. Toplam 14 mutasyonun 14'ü kırmızı.

Son mutasyondan sonra temiz build alındı (`b7`): `dotnet build Kuyumcu.sln`, **0 Hata**. Tek uyarı başka şeridin dosyasında (`Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49,65) CS8601`). Bu paketin dosyalarında uyarı 0.

## 6. denetim.sh
`bash dotnet/denetim.sh` → 315 ✓, 2 ✗ (çıkış 1). Tur 1 ile aynı iki madde:
- `P-30 DEMO_INSAN_SURESI (BK-45)`: kabulde hariç, kurucu kalemi.
- `GIRDI-2 TEST_GECERSIZ_FIKSTUR 1`: `Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` IBAN fikstürü, başka şeridin.

Tur 2'nin ilk koşumunda bu paketin ürettiği 3 ✗ vardı, üçü de giderildi:
- `HasHesap.ToHasGram (beyaz liste 4)`: 5 → 4. Giriş ve iade tek `EmanetHas` çağrısından geçiyor.
- `DTO_DECIMAL_RANGESIZ`: 11 → 10. `[Gram]` eklendi.
- `SUNUCU_CIPLAK_TURKCE_ILETI`: 3 → 0. TR/EN çiftleri eklendi.

## 7. Canlı doğrulama (API 5166, sistem anahtarı ortam değişkeninde)
Kiracı: `PAKET-DILBORC2-TEST Kuyumculuk` = `01M4M7FDMFA9QHNVFK9WFK7CT5`. `demo-tohumla.js --prova-kiraci` ile ürün uçlarından tohumlandı (6 cari, 12 ürün, 9 satış; Derya'ya 10 g K22 emanet tohumda).

| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /auth/giris | 200 | 200 |
| GET /cari (Derya, tohum emaneti) | `emanetBirakildigi` K22 10 g | 200 · `emanetteHas 9.16 · [{"ayarId":"K22","gram":10,"has":9.16}]` |
| POST /emanet/giris Tuna 12,5 g K22 | 200, has 11,45 | 200 · `hasGrami 11.45` |
| GET /emanet/{tuna} | `birakildigiGibi` 12,5 g | 200 · `mevcutHasGramBakiye 11.45 · [{"ayarId":"K22","gram":12.5,"has":11.45}]` |
| POST cikis has 4 · K22 · 5 g (uyuşmaz) | 400, Türkçe ileti | 400 · "Has gram ayar ve gramla uyuşmuyor. Beklenen: 4.58, gönderilen: 4" |
| POST cikis has 4,58 · K22 · 5 g | 200 | 200 · `iadeEdilenHas 4.58` |
| GET /emanet/{tuna} | kalan 7,5 g | 200 · `6.87 has · [{"gram":7.5,"has":6.87}]` |
| POST tümünü iade 7,5 g · 6,87 has | 200, emanet 0 | 200 · sonra `mevcutHasGramBakiye 0 · birakildigiGibi []` |
| POST /emanet/giris Selin 12,5 g K22 | 200 | 200 (denetimdeki senaryo) |
| GET /kasalar · /kasalar/nakit-ayrimi | büyük rakam = nakit | `bakiye 216590.79` · `nakit 193744.00 · kart 22846.79 · havale 0` |

SQL kanıtı (`hareketler`, EMANET_*; `ad | tur | emanet_gram | emanet_tas | emanet_ayar_id | has_grami`):
```
Derya Yıldızlı  EMANET_GIRIS  10.000  NULL  K22   9.160000
Tuna Akarsu     EMANET_GIRIS  12.500  NULL  K22  11.450000
Tuna Akarsu     EMANET_CIKIS  -5.000  NULL  K22  -4.580000
Selin Ergüven   EMANET_GIRIS  12.500  NULL  K22  11.450000
Tuna Akarsu     EMANET_CIKIS  -7.500  NULL  K22  -6.870000
Σ Tuna 0.000 g / 0.000000 has · Derya 10.000 / 9.160000 · Selin 12.500 / 11.450000
```
Kasa SQL: `kasalar Ana Kasa 216590.79`; `kasa_hareketleri` gruplu: DIGER 150000.00 · NAKIT 43744.00 · KART 22846.79. Nakit = 193.744,00 = ekrandaki büyük rakam.

### Gerçek Chrome ölçümü
Başsız Chrome, `sikisma-olc.js`; ham veri `t2/olc2-ana.jsonl` ve `t2/olc2-son.jsonl`. Kapsam: platin + yıldız × açık + koyu × BASİT + PRO × 1280 + 375 = 8 kombinasyon her durum için. **Toplam 208 + 32 + 1 ölçüm.**

| Durum | Platin (8) | Yıldız (8) |
|---|---|---|
| `/` | taşan 0 · binen 0 · kırık 0 · TRY 0 · yatay 0 · [hidden] 0 | aynı, 0 |
| `/cariler` | 0 · satır "Emanette 22 ayar · 12,50 g" | 0 · üst satır parçaları bölünmez · "emanette 22 ayar 10,00 g" |
| `/kasalar` | 0 · büyük rakam 193.744 ₺ (nakit) | 0 |
| `/kasalar` Tüm ayrıntılar | — | **K9 = 0 (8/8)**; tur 1 KRİTİK kapandı |
| `/emanet` | 0 | 0 · "Ne bıraktı" sütununda "22 ayar · 12,50 g" |
| emanet müşteri seçili | 0 · "22 ayar · 12,50 g" (PRO: "= 11,4500 has") | 0 |
| emanet iade çekmecesi | çekmece içi binen **0**; sayfa K9 2–6 | çekmece içi binen **0**; sayfa K9 3–6 |
| emanet giriş çekmecesi (karşılaştırma, bu paket değiştirmedi) | sayfa K9 1–3, çekmece içi 0 | — |

**Çekmece K9 açıklaması.** Ölçer, açık çekmeceyi altında kalan sayfa öğeleriyle de çakışık sayıyor. Örnekler: `"Müşteri" etiketi ⟂ çekmecedeki "Ayar"`, `"Açıklama" ⟂ "Altın:"`. Katmanlar arası bu sayım iki kanıtla ayrıldı:
- Çekmecenin İÇİNDEKİ öğelerin birbiriyle çakışması ayrıca ölçüldü: 16/16 = 0.
- Bu paketin dokunmadığı giriş çekmecesi de aynı sayfa K9'unu veriyor (1–3).

**Yıldız tuval kıyası** (1440, `tuval-kiyas.mjs`):
- Müşteriler açık/koyu: 27/28 AYNI.
  - İlk koşumda 7 FARK çıktı. Sebep veriydi: test için Tuna'ya da emanet eklenmişti, "satır6 durum" iki satıra çıktı.
  - Tuna'nın emaneti tümüyle iade edilince tabloya dönüldü. Tek FARK "menü seçili" x +136 (menü sahibi şerit, tur 1'de de vardı).
- Tezgâh açık/koyu: 34/36 (menü ve ödeme1 kasa t7).
- Tüm ekranlar açık/koyu: 65/67 (menü ve `main` yüksekliği).
- Tur 1 ile birebir; bu paketin FARK'ı 0.

Gözle bakılan 4 görüntü (`/private/tmp/kuyumcu-serit/P-DIL-BORC-KASA/t2/foto/`):
1. `yildiz-karanlik-BASIT-1280-kasalar-ayrinti.png`: Ana Kasa satırında "Çekmecedeki nakit" etiketi 193.744 ₺'nin üstünde. "Kart 22.846,79 ₺ · Havale 0 ₺ · Toplam 216.590,79 ₺" solda ayrı satırda. Binme yok. (Üstteki "Biraz yoğunluk var" bandı API'nin geçici yoğunluk bildirimi; ölçüm hızından kaynaklı, bu paketin değil.)
2. `platin-aydinlik-BASIT-375-emanet-iade.png`: "Elimizdeki emanet · Altın: 22 ayar · 12,50 g" · Ayar "22 ayar" · "Geri verilen gram *" 12,500 g · Vazgeç/Kaydet. Has yok.
3. `yildiz-aydinlik-PRO-375-_cariler.png`: "6 MÜŞTERİ · 2 BİZE BORÇLU · / 1 BİZ BORÇLUYUZ" (parça bölünmedi) · "emanette 22 ayar 10,00 g".
4. `yildiz-karanlik-BASIT-1280-_emanet.png`: "Ne bıraktı" sütunu "22 ayar · 12,50 g", Has ikincil sütunda. Bu görüntüde "10,00 / g" kırılması görüldü. Ardından sayı–birim ve "22 ayar" U+00A0 ile bağlandı (yapısal güvence). /emanet yeniden ölçüldü: sayısal 8/8 = 0. Görüntü bütçesi (4) dolduğu için bu düzeltmeye ayrıca görüntüyle bakılmadı.

### Temizlik
- API (dotnet run + alt süreç 3515), vekil (5167) ve Chrome (9512/9513) durduruldu. 5166/5167 dinleyen yok.
- `zsh kiraci-temizle.sh 01M4M7FDMFA9QHNVFK9WFK7CT5 --uygula` → çıkış 0. SQL ile kiracı satırı 0 ve hareketler + cariler + kasalar = 0.
- **tenants 22 → 22.** QA-TEST-* ve diğer kiracılara dokunulmadı.

## 8. DoD sözleşmesi
- **Yeni kolonlar** (`emanet_gram`, `emanet_tas`, `emanet_ayar_id`) finansal defter satırındadır (`hareketler`). Düzenlenemezler: hard DELETE yasak, değişiklik yolu STORNO + yeni kayıt (48-A motoru; storno izi ters taşır, PDBK_21).
  - Ekranda gerekçe zaten var: Emanet ayrıntı çekmecesi kilitli alanları DegismezIpucu ile yazar.
  - P-12 PATCH ucu (`PATCH /emanet/hareketler/{id}`) yalnız açıklamayı düzeltir. Değişmedi; "gövdede yoksa koru, null ise sil" testi P12'de.
- **Yeni uç yok.** Var olan iki uç genişledi:
  - `POST /emanet/{cariId}/cikis`: isteğe bağlı `ayarId` + `gramAgirlik`; `[ModulYetkisi("emanet")]` aynı.
  - `GET /emanet/{cariId}` ve `GET /cari`: salt okunur alan eklendi.
- **Denetim kaydı:** yazma yolu değişmedi.
- **Form:** boş veya fazla gram alan altında Türkçe mesaj alır (`c-gram`). Kaydet sebebiyle kilitlenir (title = IadeFazlaUyari).
- **Dokunulan sayfalar (TEK DOKUNUŞ kaydı, tur 2):** `Pages/Emanet.razor`, `Pages/Emanet.Yildiz.cs`, `Bilesenler/Yildiz/YildizMusteriler.razor`, `Bilesenler/Fis/FisProYanSutun.razor`. Tur 1'den: `Pages/Kasalar.razor`, `Pages/Kasalar.Yildiz.cs`, `Pages/Cariler.razor` (tur 2'de aynı paketin düzeltmesi).

## 9. Açık kalanlar (sahipleriyle)
1. **Y8 Tezgâh "Kart · Ana Kasa"** → YILDIZ-TEZGAH-ODEME (t7). `YildizTezgahVerisi.cs` ödeme kaydında kart/havale satırına kasa adı yerine ödeme aracı yazılmalı.
2. **O21 kalan terimler** (orkestratör kapı kaydına iş kalemi olarak):
   - virman → P-FORM-GORUNURLUK (Gider)
   - "Oturum" → P-SAYIM
   - "sahte fark" → t7 (POS/Tezgâh)
   - ramat / konsinye / çek-senet devri → ürün/evrak sahipleri
   - altın pozisyonu → YILDIZ-KATMAN (Bugün alt şeridi)
3. Tam süitte başkasına ait kırmızılar: `RaporDortTestleri.RD_KH01`, `RD_KH02` (rapor paketi, tarih bağımlı; tur 1 devir listesinde).
4. denetim `GIRDI-2 TEST_GECERSIZ_FIKSTUR` (MASAKESZAMAN doğrulayıcı testi) ve `P-30` (kurucu kalemi).
5. Tuval kıyası "menü seçili" x +136 px: menü sahibi şerit (YILDIZ-KATMAN/AMBLEM).
6. Emanet "bırakıldığı gibi" yalnız ALTIN için özetleniyor. Gümüş/platin emanet eski has gösterimiyle kalıyor; veri izi yine yazılıyor.
7. Bu kolonlardan ÖNCE girilmiş emanetler (canlı demo verisi dahil) has ile görünmeye devam eder. Geri doldurma yapılmadı: gram ve ayar tarihten türetilemez, uydurulmaz.
8. Orkestratör: spesifikasyonda "Birleşik koşullar" bölümü yok; paket koşulları kapı kaydına yazılmalı.

## 10. Tur 1 özeti (geçerliliğini koruyor)
- **Y7:** borç/alacak tek kalıp `BorcDiliSozlugu`: "Müşteri bize borçlu: X ₺" / "Biz müşteriye borçluyuz: X ₺". Bugün, Müşteriler, Tezgâh üst satırı, cari kartı, Raporlar, Panel, Platin Tezgâh rozeti ve PRO yan sütun bunu kullanıyor. Metin taraması PDBK_07 = 0 aykırı.
- **Y8:** Kasa büyük rakamı = çekmecedeki nakit (`Kasalar.BuyukRakam`). Kart ve havale ayrı satırda adlarıyla. Ayrım okunamazsa "Kasa toplamı" diye etiketlenir.
- **O21:** BASİT modda "Bakiyeyi düzelt / Dövizle tahsilat al / Dövizle veresiye yaz"; PRO'da eski adlar.
- **Hesap:** işaret sözleşmesi ve `KasaNakitAyrimi` formülü değişmedi.

## 11. Kurucu / mali müşavir
- Mevzuat hükmü yok. Bu tur yalnız gösterim, emanetin bırakıldığı gibi izi ve yuvarlama toleransını kapsıyor.
- Has hesabı (`HasHesap.ToHasGram`, milyem) değişmedi. Mali müşavire soru yok.

## 12. Değişen dosyalar (bu paketinkiler; `git status --porcelain | grep -v -E "/(bin|obj)/"`)
- Sunucu:
  - `dotnet/Kuyumcu.Api/Data/Entities.cs`
  - `dotnet/Kuyumcu.Api/Data/Migrations/20261011003451_EmanetBirakildigiGibi.cs` ve `.Designer.cs`
  - `dotnet/Kuyumcu.Api/Data/Migrations/KuyumcuDbContextModelSnapshot.cs`
  - `dotnet/Kuyumcu.Api/Modules/Emanet/EmanetModulu.cs`
  - `dotnet/Kuyumcu.Api/Modules/Satis/StornoServisi.cs`
  - `dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs`
- Core:
  - `dotnet/Kuyumcu.Core/Domain/EmanetBirakildigi.cs` (yeni)
  - `dotnet/Kuyumcu.Core/I18n/Sozlukler.BorcDili.cs`
  - `dotnet/Kuyumcu.Core/I18n/SunucuIletiSozlugu.Veri.cs`
  - Tur 1: `Sozlukler.Yildiz.cs`, `Sozlukler.FisMusteri.cs`, `Sozlukler.SatisPro.cs`, `Sozlukler.ParaDemo2.cs`, `Sozlukler.Toplu3.cs`, `Sozlukler.Panel.cs`, `Sozlukler.KesifP2.cs`, `Sozlukler.cs`
- Sözleşme:
  - `dotnet/Kuyumcu.Contracts/Sayfalar/CarilerSozlesmeleri.cs`
  - `dotnet/Kuyumcu.Contracts/Sayfalar/EmanetSozlesmeleri.cs`
- Web:
  - `dotnet/Kuyumcu.Web/Pages/Emanet.razor`, `Emanet.Yildiz.cs`
  - `dotnet/Kuyumcu.Web/Pages/Cariler.razor`, `Kasalar.razor`, `Kasalar.Yildiz.cs`
  - `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizMusteriVerisi.cs`, `YildizMusteriler.razor`, `YildizTezgahVerisi.cs`
  - `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisBakiyeRozeti.cs`, `FisProYanSutun.razor`
  - `dotnet/Kuyumcu.Web/wwwroot/css/tema-yildiz.css`
- Test:
  - `dotnet/Kuyumcu.Tests/PDilBorcKasaTestleri.cs`
  - Tur 1: `Yildiz4ATestleri.cs`, `Yildiz4BTestleri.cs`, `S31PanelSadeTestleri.cs`, `Dogrulayici/TS2SinirTestleri.cs`, `ParaDemo2EkranTestleri.cs`

## 13. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Borç/alacak tek kalıp, tüm ekranlar (0 aykırı) | KARŞILANDI | PDBK_07 = 0. İşaret mantığı da tek kaynakta (`Yon`), 4 çağrı yeri bağlı (M10-1, M11-1) |
| Kasa büyük rakam = SQL nakit, kart/havale ayrı | KARŞILANDI | 193.744 = DIGER 150000 + NAKIT 43744; Yıldız ayrıntıda binme 0 |
| Emanet gram/ayar tek dil (Y18) | KARŞILANDI | "22 ayar · 12,50 g" Emanet özeti, iade çekmecesi, Müşteriler (Platin + Yıldız), Yıldız "Ne bıraktı"; has yalnız PRO ikincil. Canlı API + SQL |
| Gerçek Chrome platin + yıldız, BASİT/PRO, 1280 + 375, açık + koyu | KARŞILANDI | 241 ölçüm, 4 görüntü okundu |
| Mutasyon ≥ 3 kırmızı, cmp AYNI | KARŞILANDI | Tur 2: 9/9; toplam 14/14 |
| build 0/0 | KARŞILANDI (bu paketin dosyaları) | 0 Hata; tek uyarı başka şeridin dosyasında (CS8601 SUBEKAPSAMASinir) |
| denetim TEMİZ (P-30 hariç) | KENDİ SAYAÇLARI ✓ | Tek diğer ✗ GIRDI-2 TEST_GECERSIZ_FIKSTUR (başka şerit) |
| Tam süit 0 yeni kırmızı | KARŞILANDI | 2 kırmızı (RD_KH01/02), devir listesinde |
| Yıldız tuval FARK korunur | KARŞILANDI | Müşteriler 27/28, Tezgâh 34/36, Tüm 65/67 (tur 1 ile aynı) |
| Test kiracısı silindi, tenants başlangıç | KARŞILANDI | 22 → 22 |
| O21 | KISMEN (şerit sınırı) | Borç/kasa eylemleri yapıldı; kalanlar § 9.2 |
