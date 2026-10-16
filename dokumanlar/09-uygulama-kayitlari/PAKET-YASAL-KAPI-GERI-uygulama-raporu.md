# PAKET YASAL-KAPI-GERI — uygulama raporu (Taşkın + Ertuğ, 09.10.2026 · t3 · 5164/5165)

Kurucu bulgusu (09.10): "toplam tutarı en başta 200.000 üzerine çıkardım sonra ürünlerden kaldırdım 200.000'in altına indirdim algılamadı".

**Durum: BITTI (tur 4, 10.10.2026).** Tur 1–3'te kod, testler, mutasyonlar, canlı kanıt ve Chrome ölçümü tamamlanmıştı. Yalnız test kiracısının silinmesi ve kapı kanıtları Docker çöküşü yüzünden açık kalmıştı. Tur 4'te Docker/MySQL ayaktaydı: eski kiracı silindi, kapı kanıtları yeniden üretildi, kapora düzeltmesi Chrome'da ölçüldü ve doğrulayıcının DÜŞÜK bulguları kapatıldı (§15). Tur 1–3 metni aşağıda tarihsel kayıt olarak duruyor.

## 1. Önce: kurucunun senaryosu gerçek Chrome'da yeniden üretildi (eski kod)

Ortam: başsız Chrome (betikler/tur/cdp.mjs), vekil 5165, API 5164, kiracı `PAKETYASAL-KAPI-GERI-TEST 1b9701`. Görüntüler `/private/tmp/kuyumcu-serit/YASAL-KAPI-GERI/chrome/foto/` altında.

| # | Senaryo (eski kod) | Sonuç |
|---|---|---|
| Ö1 | Yıldız BASIT, gerçek müşteri. Fiş 70.000 ₺ (bağlantılı toplam eşiğin altında) iken araya aynı müşteriye 30.000 ₺'lik satış girdi. Kaydet'e basılınca sunucu 400 MASAK döndü, ardından 50.000 ₺'lik kalem silindi; fiş 20.000 ₺ oldu, bağlantılı toplam 170.000 ₺ (eşiğin altında). | **HATA.** Form açık ve Kaydet KİLİTLİ kaldı, ekranda "Kimlik tespiti eksik: MASAK eşiği aşıldı …" yazıyordu (`once yapiskan-yildiz-basit-C-alti.png`). Sunucu bu fişi kabul ederdi. |
| Ö2 | Platin BASIT, Peşin Müşteri, 230.000 → kalem sil → 150.000 ₺ | İki tutarda da aynı cümle: "Bu tutarda Peşin Müşteri olmaz — müşteri seçin." Hangi eşiğin tuttuğu (200.000 kimlik → 30.000 tevsik) görünmüyordu, kurucunun "algılamadı" algısı da buradan (`pesin-platin-basit-*.png`). |
| Ö3 | Yıldız BASIT, Peşin, 150.000 ₺ | Aynı genel cümle (`pesin-yildiz-basit-A.png`). |
| Ö4 | Yıldız BASIT/PRO ve Platin BASIT/PRO, sunucu reddi olmadan 230.000 → 150.000 → 230.000 | Doğru çalışıyor: form kapandı, kilit kalktı, form geri geldi (`once-*-A/B/C`). Asıl hata reddin bıraktığı yapışkan durum ve Peşin cümlesiydi. |

**Kök neden.** Sunucu reddinin (400) açtığı ekran durumları düz `bool` idi: `masakSunucudanGerekli` (MASAK formunu zorla açar), `kapiSunucudanAcildi` (BASİT'te "yasal kapı" bandı ve ödeme seçicisi), `pesinKimlikGerekli` (Peşin reddi bandı), ayrıca Hurda satışındaki `masakZorla`. Bu bayraklar yalnız Kaydet, müşteri değişimi ya da Vazgeç ile iniyordu. Kalem silme, adet azaltma, iskonto ya da hurda gramı azaltma ile tutar düşünce bayrak yerinde kalıyordu. `MasakGereksinimSor` da bayrak açıkken en az kimlik eşiğiyle yeniden sorduğu için ekran sunucudan farklı bir kural söylüyordu.

Tarama sırasında aynı sınıftan ikinci bir hata bulundu: nakit sınırı (tevsik) önizlemesi (`tevsikNakitKapali`) yalnız kalem ekleme, silme ve satır düzeltmesinde soruluyordu. Fiş iskontosu toplamı düşürünce önizleme bayat kalıyordu; ödeme otomatik Kart'ta ve "sınırı aştığı için Kart seçildi" notu ekranda kalıyordu. Bu hatayı bUnit testi YKG09 eski kodda kırmızıyla gösterdi.

## 2. Yapılanlar

- **`Bilesenler/Fis/YasalKapiIzi.cs` (yeni, tek kural):** sunucu reddi artık bir İZ olarak tutulur: reddin verildiği ölçü (mutlak tutar) ve müşteri. Tutar o ölçünün altına inerse ya da müşteri değişirse iz geçersiz olur. İz silinmez; tutar yeniden çıkınca durum geri gelir. Eşik sayısı yoktur, yalnız sunucunun reddettiği tutar hatırlanır.
- **`Pages/Islemler.YasalKapiGeri.cs` (yeni):** üç bayrak izle beslenen özellik oldu (`masakSunucudanGerekli`: ölçü MasakTutar; `kapiSunucudanAcildi` ve `pesinKimlikGerekli`: ölçü max(MasakTutar, TevsikTutar)). Mevcut `= true`, `= false` ve `|=` atamaları aynen çalışır. Ayrıca:
  - `YasalIzleriTazele()`: kapı izi geçersizleşince yalnız sunucunun kendi ret cümlesini (hata bandı) ve Peşin "aynı kişi mi?" sorusunu indirir.
  - `TevsikToplamTazele()`: satışta toplam son sorulandan farklıysa tevsik önizlemesini yeniden ister. Boş fişte istek atmaz (YASAL-2 sözleşmesi).
  - `PesinKilitMetni`: Peşin kilidi hangi eşiğin tuttuğunu söyler. Sıra sunucuyla aynıdır: önce MASAK, sonra tevsik.
- **`Core/I18n/Sozlukler.YasalKapiGeri.cs` (yeni, TR+EN):**
  - `PesinKimlikKilit`: "{0} ve üstünde kimlik gerekir — Peşin Müşteri olmaz, müşteri seçin."
  - `PesinTevsikKilit`: "{0} üstündeki işlem müşteri adına yazılır — Peşin Müşteri olmaz, müşteri seçin."
  - {0} sunucunun bildirdiği eşiktir (Bicim.Tl). Eşik bilinmiyorsa eski genel cümle kalır.
- **En küçük dokunuşlar:**
  - `Islemler.Odeme.cs`: alan satırı kaldırıldı, Peşin cümlesi `PesinKilitMetni` oldu.
  - `Islemler.razor`: iki alan satırı yorum oldu; OnAfterRenderAsync satırına `| YasalIzleriTazele() | await TevsikToplamTazele()` eklendi. Dosya 1.397 satır (S21 sınırı 1.400).
  - `Islemler.Pro.cs`: TevsikOnizle başına tek satır.
  - `Hurda.razor`: `masakZorla` izli özellik oldu.
- **Kaydet() ve POST gövdeleri değişmedi.** Gövdeye masak bloğu yalnız `MasakGerekli` iken gider (`MasakGovdesi` aynı). Girilen beyan verisi (`masak` modeli) silinmez: tutar yeniden çıkınca alanlar dolu gelir. Bunu YKG02 ve YKG07 doğruluyor.

### Sapmalar ve gerekçeleri
- Spesifikasyonun "mevcut durum" iddiası kısmen doğru çıktı. Sunucu reddi olmayan yolda ekran zaten yeniden hesaplıyordu (Ö4). Hata, reddin bıraktığı yapışkan durum ile Peşin cümlesinin tek tip olmasıydı. Hedefe sadık kalıp tüm sınıfı izle kapattım.
- Peşin cümlesinin değişmesi, başka paketlerin tek cümle iddia eden 4 testini (8 koşum) güncellemeyi gerektirdi:
  - `Masak370PesinEkranTestleri.cs:28`
  - `SATIS3T24YasalMesajTestleri.cs` (T24_had_USTU_KART ve T24_MASAK_kimlik testleri; `CelisenCift` ve `DoesNotContain` artık eşikli cümleleri de engel sayıyor)
  - `Dogrulayici/SATIS3T24SinirTestleri.cs:37`
  - Tek cümle değişmezi korundu, yalnız beklenen metin eşikli oldu. `Satis3Sozlugu.PesinTutarKilit` sabiti DEĞİŞMEDİ: eşik bilinmezken hâlâ kullanılıyor ve `DOG` sabit testi yeşil.
- `ZOISLEMAEkranTestleri` sınıfına `partial` eklendi (YKG06 o sınıfın hurda fikstürünü kullanıyor).

## 3. Migration
Gerekmedi (şema değişikliği yok).

## 4. Testler

Yeni test sayısı 21 koşum: 8 metot × 2 mod (16), YKG05'in 5 sınır satırı ve 2 saf test. Toplam metot sayısı 11 (9 ekran + 2 saf). Hepsi `Kuyumcu.Tests/YasalKapiGeriTestleri.cs` dosyasında.

| Test | Ne sınar |
|---|---|
| YKG_iz_yukari_asagi_yukari… / YKG_iz_musteri… (saf) | iz kuralı: tam ölçü, 1 kuruş altı, geri çıkış, müşteri değişimi, null |
| YKG01 (BASIT+PRO) | MASAK reddinden sonra kalem silinince form, kilit, BASİT bant ve hata bandı kalkar |
| YKG02 (BASIT+PRO) | aşağı ve yeniden yukarı: form geri gelir, yazılan beyan kaybolmaz, kilit sunucu kuralıyla aynı |
| YKG03 (BASIT+PRO) | eşik altında POST gövdesinde `masak` yok, beyan metni gövdede yok (JSON ayrıştırılarak) |
| YKG04 (BASIT+PRO) | Peşin yukarı-aşağı-yukarı: kilit cümlesi tevsik → kimlik → tevsik → kimlik |
| YKG05 (5 satır) | tam eşikler: 4.999,99 / 5.000 (had, sunucu `>`) / 5.000,01 / 14.999,99 / 15.000 (kimlik, `>=`) |
| YKG06 | Hurda satışı: MASAK reddi sonrası gram azalınca alanlar ve not kapanır, geri çıkınca gelir |
| YKG07 (BASIT+PRO) | iskonto ile aşağı: ret bandı ve form kalkar (hata bandını yalnız iz kuralı indirir), iskonto kalkınca geri gelir |
| YKG08 (BASIT+PRO) | Peşin sunucu reddi bandı (#islem-pesin-esik) tutar inince kalkar, çıkınca gelir |
| YKG09 | BASIT: nakit sınırı üstü otomatik Kart; iskonto ile sınır altına inince sunucuya yeni toplamla sorulur, Nakit döner |

Koşumlar (`KUYUMCU_TEST_DB=kuyumcu_test_t3`):

- Filtreli `~YKG`: 21/21 yeşil.
- Geniş filtre (SATIS/ZOISLEMA/Yildiz/Masak/Pesin/Yasal/Tevsik/Islem/Hurda/ParaDemo/Degisim/BasitMod/P16/S22/Tezgah/KESIF/Iade/Takas): 3.398 test. Kırmızılar:
  - **benim** iki YASAL2 testim çıktı ("kayıt sonrası sorgu ATILMAZ"); TevsikToplamTazele'ye boş fiş koruması eklendi ve ikisi yeşile döndü;
  - **başkasının** menü sayısı (Y2_02, S1j, Y4B_14), kararsız 2 test (Arama debounce, YasalParametre K1 tohum; yeniden koşumda yeşil).
- **Tam süit (tur başına bir kez):** 12.018 test, 12.007 geçti, 8 başarısız, 3 atlandı. trx: `/private/tmp/kuyumcu-serit/trx-pYASAL-KAPI-GERI/_Mac_2026-10-09_23_18_29.trx`. Kırmızıların **hiçbiri bu paketin değil**:
  - `SozlesmeKapsamTestleri.Kapsam_orani…` (58 ≠ 63) ve `Kilitsiz_tip_listesi…` (IrsaliyeDetay, IrsaliyeListesi, MalKabulSonucu, SatinAlmaDetay): sahibi SATINALMA (t5).
  - `P37cPanelVeKapsamTestleri.Her_ekran_ortak_arac_seridini…` (49 ≠ 51, yeni ekranlar): sahibi SATINALMA/KAPORA (t5).
  - `YayinAcilisHttpTestleri.HazirUcuBekleyenMigrationVarken503Doner`: MySQL bağlantısı ("Unable to connect to any of the specified MySQL hosts"); Docker'ın çökmesiyle aynı anda.
  - `Yildiz4BTestleri.Y4B_14` (39 kare), `Y4B_44` (CSS position static), `Y4B_45`, `Yildiz4CTestleri.Y4C_41`: YILDIZ-4C tur 3 süit sırasında Islemler.Yildiz.cs (23:15) ve test dosyalarını (23:19/23:24) değiştirdi; DLL'ler 23:23/23:25'te başka şeritçe yeniden derlendi. Temiz derlemeyle yeniden koşumda Y4B_44, Y4B_45 ve Y4C_41 yeşil.

## 5. Mutasyon kanıtları

Yöntem: yedek `/private/tmp/kuyumcu-serit/mut-yedek/YASAL-KAPI-GERI/`, ardından `perl -0777` ile tek eşleşmeli değişiklik, build, `--filter ~YKG`, yedeği geri kopyalama ve `touch`, son olarak `cmp`.

| Kod | Değişiklik | Kırmızı testler | cmp |
|---|---|---|---|
| M1 | `masakSunucudanGerekli` get ⇒ `masakRedIzi is not null` (eski yapışkan davranış) | YKG01 ×2, YKG02 ×2, YKG03 ×2, YKG07 ×2 (toplam 8; doğrulayıcının yeniden koşumuyla düzeltildi) | AYNI |
| M2 | `kapiSunucudanAcildi` get ⇒ yapışkan | YKG01(BASIT) | AYNI |
| M3 | `YasalIzleriTazele` ⇒ hemen `return false` | YKG07 ×2 | AYNI |
| M4 | `PesinKilitMetni` ⇒ hep eski genel cümle | YKG04 ×2, YKG05 (5000.01, 14999.99, 15000) | AYNI |
| M5 | `YasalKapiIzi.Gecerli` `>=` ⇒ `>` | 2 saf test, YKG01/02/03/07 ×2, YKG06 (11 kırmızı) | AYNI |
| M6 | Hurda `masakZorla` get ⇒ yapışkan | YKG06 | AYNI |
| M7 | `PesinMasakUstu` `>=` ⇒ `>` | YKG05(15000) | AYNI |
| M8 | `pesinKimlikGerekli` get ⇒ yapışkan | YKG08 ×2 | AYNI |
| M9 | `TevsikToplamTazele` ⇒ `return false` | YKG09 | AYNI |

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 uyarı, 0 hata. Kaynakta mutasyon izi yok (grep ile doğrulandı).

## 6. denetim.sh

`denetim2.log`: 313 ✓. ✗ satırlarının hiçbiri bu paketin değil:

- `P-30 DEMO_INSAN_SURESI`: kapsam dışı (bilinen).
- `P-36b SECICI_UZAK_KIP` (uzak=12 kayitsecici=5): başka şerit, bu paket Secici'ye dokunmadı.
- `GIRDI-2 TEST_GECERSIZ_FIKSTUR` 1 (`Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` IBAN): sahibi MASAK-ESZAMAN.
- `B-3 ESIK_ISTEMCIDE` 5 (tavan 4): bu paketin ilk yorumları sayıyı 8'e çıkarmıştı ("230.000/30.000" yorum metinleri); yorumlar sayısız yazıldı ve bu paketin payı 0. Kalan 5 satır: TevsikOneri.cs:8, Islemler.razor:516, FisOdemeKasa.razor:15 ve :85, **Islemler.Yildiz.cs:39** (YILDIZ-4C tur 3'ün yeni yorumu "30.000 ₺ eşiğinde"). Artış sahibi YILDIZ-4C.
- İlk koşumda görülen `@onclick'siz button` ve `duzenlenenId` ARTIŞları ikinci koşumda yoktu (başka şeritte geçici durum). Bu paket hiçbir razor işaretlemesi eklemedi.

## 7. Canlı doğrulama (API 5164 + SQL, kuyumcu_net)

`canli/canli.py`, sonuçlar `canli-sonuc.json`:

| Adım | Beklenen | Gerçek | Yanıt |
|---|---|---|---|
| 1 gerçek müşteri 230.000 masaksız (eşik üstü) | 400 | 400 | "MASAK: 200.000 TL ve üstü işlemde işlem amacı beyanıyla birlikte kimlik tespiti zorunludur …" |
| 2 aynı müşteri 150.000 masaksız (kalem silindi → eşik altı) | 200 | 200 | 01M4H4HGGRGE8DF7AR4P71WQ13 |
| 3 yeni müşteri 199.999,99 masaksız | 200 | 200 | 01M4H4HGWB6RWGFQYNPQMEPXEZ |
| 4 yeni müşteri 200.000,00 masaksız (tam eşik) | 400 | 400 | MASAK 200.000 … |
| 5 aynı müşteri 200.000,00 kimlik+amaç | 200 | 200 | 01M4H4HHD9SAZNKC2EZTCX7JEG |
| 6 yeni müşteri 370.000 masaksız | 400 | 400 | "MASAK: 370.000 TL ve üstü işlemde kimlik tespiti zorunludur …" |
| 7 aynı müşteri 370.000 kimlik+amaç | 200 | 200 | 01M4H4HHJP8AYAXYFCKRH9ZDW5 |
| 8 Peşin 250.000 | 400 | 400 | "… MASAK kimlik tespiti eşiğine (200.000,00 TL) ulaşıyor" |
| 9 Peşin 150.000 | 400 | 400 | "… tevsik (459 s. VUK GT) eşiğine (30.000,00 TL) ulaşıyor" |
| 10 Peşin 20.000 KART | 200 | 200 | 01M4H4HHNZV8TKVP4R1QGNAKQJ |

SQL (`hareketler` ve her hareketin `masak_beyanlari` satır sayısı):

```
01M4H4HGGRGE8DF7AR4P71WQ13  SATIS  150000.00  0  NULL          ← eşik altı: beyan YOK
01M4H4HGWB6RWGFQYNPQMEPXEZ  SATIS  199999.99  0  NULL
01M4H4HHD9SAZNKC2EZTCX7JEG  SATIS  200000.00  1  AMAC_BEYANI
01M4H4HHJP8AYAXYFCKRH9ZDW5  SATIS  370000.00  1  AMAC_BEYANI   ← (sunucu kademe adı; bu paketin konusu değil)
```

Sunucu ile ekran aynı kuralı söylüyor: Peşin'de 250.000'de kimlik (200.000), 150.000'de tevsik (30.000). Ekrandaki yeni cümleler bu iki eşiği aynı sırayla söylüyor (aşağıda).

## 8. Sonra: gerçek Chrome (düzeltilmiş kod)

| Şema × mod | Senaryo | A (fiş 70.000, ret öncesi) | B (sunucu reddi) | C (kalem silindi, fiş 20.000) |
|---|---|---|---|---|
| Yıldız BASIT | yapışkan red | uygun, Kaydet açık | form + "bugünkü toplamı 210.000 ₺" + kilit | **"Yasal kurallar uygun", Kaydet AÇIK, Nakit** |
| Yıldız PRO | yapışkan red | uygun | form + kilit | **uygun, Kaydet açık** |
| Platin BASIT | yapışkan red | Kaydet açık | yasal kapı bandı + masak formu + kilit | **Kaydet açık, bant ve form yok** |
| Platin PRO | yapışkan red | Kaydet açık | masak formu + kilit | **Kaydet açık** |
| Platin BASIT | Peşin 230.000 → 150.000 | — | "200.000 ₺ ve üstünde kimlik gerekir — Peşin Müşteri olmaz, müşteri seçin." | **"30.000 ₺ üstündeki işlem müşteri adına yazılır — Peşin Müşteri olmaz, müşteri seçin."** |
| Platin PRO | Peşin 230.000 → 150.000 | — | kimlik cümlesi | tevsik cümlesi |
| Yıldız BASIT | Peşin 250.000 / 150.000 | — | kimlik cümlesi | tevsik cümlesi |
| Platin BASIT | 40.000 (otomatik Kart) → iskonto %50 → 20.000 | Kart notu | — | **not yok, Nakit** |

Görüntüler: `foto/sonra-yapiskan-{yildiz,platin}-{BASIT,PROFESYONEL}-{B-red,C-alti}.png`, `sonra-pesin-*.png`, `sonra-iskonto-platin-basit-*.png`. Read ile bakılan (en çok 4): kesif1, once-yildiz-basit-A-ustu, pesin-yildiz-basit-A, **sonra-yapiskan-yildiz-BASIT-C-alti**. Sonuncusu fiş 20.000 ₺, "Yasal kurallar uygun · nakit 30.000 ₺ sınırının altında", Kaydet etkin, ödeme Nakit.

### Ölçüm tablosu (sikisma-olc.js `__olcTam`, `chrome/olcum.jsonl`; /islemler eşik üstü form açık + /hurda)

| şema | mod | tema | gen. | rota | K1 | K3 taşan/kırık | K5 TRY | K6 | K7 | K9 binen | K10 | K12 | yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| platin | BASIT/PRO | açık/koyu | 1280 | /islemler | 0 | 0/0 | 0 | 0/3 | 0 | 0 | 0 | 0 | 0 |
| platin | BASIT/PRO | açık/koyu | 375 | /islemler | 0 | 0/0 | 0 | 0/3 | 0 | 0 | 0 | 0 | 0 |
| platin + yıldız | BASIT/PRO | açık/koyu | 1280 + 375 | /hurda | 0 | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 |
| yıldız | BASIT/PRO | açık/koyu | 1280 | /islemler | 0 | 0/0 | 0 | 0/0 | 0 | **2** | 0 | 0 | 0 |
| yıldız | BASIT/PRO | açık/koyu | 375 | /islemler | 0 | **0/2** | 0 | 0/0 | 0 | **6** | 0 | 0 | 0 |

Toplam 32 ölçüm (16 /islemler, 16 /hurda). Yıldız /islemler'deki K9 ve K3 kırık bu paketin değil. Örnekler: "· borcu yok ⟂ 1 adet · 20,00 g", "Seç ⟂ 230.000,00 ₺", 375'te "Burma Bilezik" ve "Kart · Ykg Kasa" kırık. Ölçüm anında Yıldız müşteri katmanı açık kalıp kalem satırlarının üstüne biniyor. Aynı binme bu paketten ÖNCEKİ görüntüde de var (`pesin-yildiz-basit-A.png`). Sahibi YILDIZ-4C: tur 3'te `YildizMusteriGorunur` ve `.yz-tezgah-musteri {position: static}` üzerinde çalışıyor, bu paket bu dosyalara dokunmadı. Yan not: Yıldız'da ilk ekranda "Fiyat listesi tanımlı değil…" hata metni dar sütunda ürün seçicinin üstüne biniyor (`kesif1.png`); sahibi ISLEM-BANT/YILDIZ.

## 9. Tarama tablosu (ekran × eşik × yön)

| Ekran | 200.000 amaç / 370.000 kimlik (MASAK) | 30.000 nakit (tevsik) / ödeme zorlaması | Peşin kilidi | Aşağı yönde önceki durum | Sonrası |
|---|---|---|---|---|---|
| Satış (Islemler SATIS, platin + Yıldız) | sunucu reddi izi + istemci eşiği | önizleme her toplamda | eşikli cümle | ret sonrası yapışıyordu; iskontoda tevsik bayat kalıyordu; Peşin cümlesi tek tipti | KAPANDI (YKG01–05, 07–09, Chrome) |
| Alış (ALIS) | aynı sayfa ve izler (MasakTutar = fiş toplamı) | tevsik önizlemesi satışa özgü (sunucu da öyle) | aynı | ret sonrası yapışıyordu | KAPANDI (aynı kod yolu) |
| Hurda alımı (HURDA_ALIS, Fiş) | aynı izler (MasakTutar = has × fiyat) | NakitSiniriAsildi hesapla | aynı | ret sonrası yapışıyordu | KAPANDI (aynı kod yolu) |
| Takas / değişim | aynı izler (max(eski, yeni)) | TakasOdemeVarsayilani hesapla | aynı | ret sonrası yapışıyordu | KAPANDI (aynı kod yolu) |
| İade | aynı izler (iade tutarı) | hesapla | — | ret sonrası yapışıyordu | KAPANDI |
| Hurda satışı (/hurda) | `masakZorla` izi | tevsik zaten her tutarda soruluyor | — | ret sonrası yapışıyordu | KAPANDI (YKG06 + Chrome ölçümü) |
| Banka dekont (tahsilat) | her tutar değişiminde GereksinimTazele, bayrak yok | — | — | sorun yok | dokunulmadı |
| Diğer → Avans / Değişim | tevsikHaddi hesapla | hesapla | — | sorun yok | dokunulmadı |
| Emanet / Vezne / Cari tahsilat | ekranda eşik durumu yok (karar sunucuda) | — | — | — | dokunulmadı |
| Kapora (SiparisDetay) | `kaporaMasakZorla` artık reddin İZİ (tur 3) | — | — | ret sonrası yapışıyordu, eşik altında beyan gövdeye gidiyordu | KAPANDI tur 3 (YKGK01 ×2 mod, YKGK02 ×2 mod; MK1 kırmızı). KAPORA-EKRAN (t5) 22:21 BITTI beyan etti, dokunuş ondan sonra |

## 10. DoD sözleşmesi
Yeni varlık, kayıt türü ya da uç yok. Yeni durum yalnız ekran durumu (iz). Düzeltme yolu değişmedi. Düğme eklenmedi, kilitli Kaydet'in sebebi yine yazılı (UX-S30) ve artık hangi eşik olduğunu da söylüyor. Boş durum ve hata bandı kuralları değişmedi; hata bandı yalnız sunucunun kendi ret cümlesiyse ve fiş artık o reddin ölçüsünde değilse iner (UX-S28 ile uyumlu). Ücretsiz pilot: fiyat veya teklif metni yok.

## 11. Mevzuat
Yeni hüküm yazılmadı. Eşik sayıları sunucudan gelir (/masak/esikler, /tevsik/gunluk); karşılaştırma sırası (`MASAK ≥`, `tevsik >`) sunucudaki `MasakKapisi`/`TevsikKapisi`/`SistemCarisi.EsikMesaji` ile birebir ve canlıda doğrulandı (adım 8–9, YKG05).

## 12. Değişen dosyalar (bu paket)
- yeni: `dotnet/Kuyumcu.Web/Bilesenler/Fis/YasalKapiIzi.cs`
- yeni: `dotnet/Kuyumcu.Web/Pages/Islemler.YasalKapiGeri.cs`
- yeni: `dotnet/Kuyumcu.Core/I18n/Sozlukler.YasalKapiGeri.cs`
- yeni: `dotnet/Kuyumcu.Tests/YasalKapiGeriTestleri.cs`
- değişti (en küçük dokunuş):
  - `dotnet/Kuyumcu.Web/Pages/Islemler.Odeme.cs` (2 satır)
  - `Islemler.razor` (3 satır)
  - `Islemler.Pro.cs` (1 satır)
  - `Hurda.razor` (alan → özellik)
  - `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (`partial`)
  - `Masak370PesinEkranTestleri.cs`, `SATIS3T24YasalMesajTestleri.cs`, `Dogrulayici/SATIS3T24SinirTestleri.cs` (eşikli Peşin cümlesi beklentisi)
- Dokunulan sayfalar (TEK DOKUNUŞ): Islemler (yalnız durum hesabı; Yıldız dalının görsel kısmı yok), Hurda (yalnız durum hesabı).

## 13. Açık kalanlar
1. ~~Test kiracısı silinemedi~~ → **KAPANDI (tur 4)**: `01M4H0YQ4PPRTHRNVPMJT1ZF7Y` `kiraci-temizle.sh --uygula` ile silindi; 75 tabloda 0 satır kaldı (§15.1).
2. ~~Kapora `kaporaMasakZorla` yapışkan~~ → **KAPANDI (tur 3 kodu, tur 4 kanıtı)**: Hurda ile aynı iz kuralı uygulandı. YKGK01/YKGK02 ve MK1/MK2 mutasyonları var, 8 kombinasyonda Chrome ölçüldü (§15.3–15.5).
3. Tam süitteki başkasına ait kırmızılar ve sahipleri (tur 1 listesi; güncel liste §15.6):
   - SozlesmeKapsam ×2 ve P37c → SATINALMA/KAPORA (t5)
   - Y4B_14 ve menü 39 → menü sahibi şerit
   - YayinAcilisHttp → MySQL/Docker çöküşü
   - Y4B_44, Y4B_45, Y4C_41 → YILDIZ-4C (yeniden koşumda 3'ü yeşil)
4. Denetim sahipleri:
   - B-3 artışı (Islemler.Yildiz.cs:39) → YILDIZ-4C
   - GIRDI-2 fikstür → MASAK-ESZAMAN
   - P-36b → Secici sahibi
5. Yıldız müşteri katmanının kalem satırlarına binmesi (K9) ve "Fiyat listesi tanımlı değil" metninin dar sütunda binmesi → YILDIZ-4C / ISLEM-BANT.
6. Kurucunun aynı mesajdaki ikinci bulgusu (açılır listelerde eski tema yazı boyutu ve ailesi) bu paketin kapsamı değil; YILDIZ-TIPO paketinde.

## 14. Kurucu / mali müşavir
Yeni soru yok. Mevcut not: 200.000 amaç beyanı kademesi (M-2) mali müşavir teyidi bekliyor; bu paket eşiği yalnız sunucudan okuyor, değerine dokunmadı.

## 15. Tur 4 (10.10.2026): doğrulayıcı bulgularının kapatılması ve kapı kanıtları

Ortam: Docker `kuyumcu-mysql` Up, 3306 açık, disk 21 GB boş. API 5164, vekil 5165, CDP 9601/9602, test DB `kuyumcu_test_t3`. Tur 4'te yeni üretim kodu YAZILMADI. Tur 3'te uygulanan kapora iz kuralı ve sabit literal beklentiler yerinde doğrulandı; bu turda kanıtları yeniden üretildi.

### 15.1 [KRİTİK] Eski test kiracısı silindi
- `zsh betikler/kiraci-temizle.sh 01M4H0YQ4PPRTHRNVPMJT1ZF7Y --uygula` çalıştı. İkinci koşum: "Kiracı YOK … (bağlantı sınandı ve ÇALIŞIYOR — bu gerçekten 'yok' demektir)".
- SQL: `tenant_id` kolonu olan 75 tablonun UNION ALL sayımında satır toplamı **0**.
- tenants sayısı silmeden önce 22'ydi (bizimki dahil), sonra **21** oldu. Tur 1 raporundaki "23" o anın sayısıydı; aradaki farkı başka şeritler kendi kiracılarını silerek yarattı. Bu paketin kiracılarının iz sayısı 0'dır.

### 15.2 [KRİTİK] Kapı kanıtları yeniden üretildi
- **Derleme:** `dotnet build Kuyumcu.sln` sonucu **0 Uyarı, 0 Hata** (`build-t4-son.log`, son mutasyondan sonra temiz).
- **Filtreli testler:** `~YKG | ~SATIS3T24 | ~Masak370Pesin | ~KaporaEkran` filtresiyle **48/48 yeşil**. YKG ailesi 39 test (YKG01–09, YKGK01–02 ve saf testler).
- **denetim.sh** (`denetim-t4.log`): 315 ✓, 2 ✗, ikisi de bu paketin değil:
  - `P-30 DEMO_INSAN_SURESI`: kapsam dışı (bilinen).
  - `GIRDI-2 TEST_GECERSIZ_FIKSTUR` 1: `Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` IBAN, sahibi MASAK-ESZAMAN.
  - Tur 1'deki B-3 ve P-36b ✗'leri artık ✓. `ModalPencere` 48/55 ve `duzenlenenId` 1/1, artış yok.
- **Tam süit (tur başına bir kez):** 12.050 test, **12.040 geçti, 7 başarısız**, 3 atlandı. trx dizini `/private/tmp/kuyumcu-serit/trx-pYASAL-KAPI-GERI-t4/`. Kırmızıların hiçbiri bu paketin değil (§15.6).

### 15.3 Canlı API + SQL (5164, yeni kiracı `PAKETYASAL-KAPI-GERI-TEST 4eabc9`)
| Adım | Beklenen | Gerçek | Yanıt |
|---|---|---|---|
| 1 gerçek müşteri 230.000, MASAK yok (eşik üstü) | 400 | 400 | "MASAK: 200.000 TL ve üstü işlemde işlem amacı beyanıyla birlikte kimlik tespiti zorunludur …" |
| 2 aynı müşteri 150.000, MASAK yok (kalem silindi, eşik altı) | 200 | 200 | 01M4JK5J05XK42FEBEJT6J1T0H |
| 3 yeni müşteri 199.999,99, MASAK yok | 200 | 200 | 01M4JK5J3N38VKRGEXQ8SHJGNX |
| 4 yeni müşteri 200.000,00, MASAK yok (tam eşik) | 400 | 400 | MASAK 200.000 … |
| 5 aynı müşteri 200.000,00, kimlik + amaç | 200 | 200 | 01M4JK5J6HWWY1GAB79X9YJR42 |
| 6 yeni müşteri 370.000, MASAK yok | 400 | 400 | "MASAK: 370.000 TL ve üstü işlemde kimlik tespiti zorunludur …" |
| 7 aynı müşteri 370.000, kimlik + amaç | 200 | 200 | 01M4JK5J9XXBG4DNAAVX11KDWB |
| 8 Peşin 250.000 | 400 | 400 | "… MASAK kimlik tespiti eşiğine (200.000,00 TL) ulaşıyor" |
| 9 Peşin 150.000 | 400 | 400 | "… tevsik (459 s. VUK GT) eşiğine (30.000,00 TL) ulaşıyor" |
| 10 Peşin 20.000 KART | 200 | 200 | 01M4JK5JCASKZM0G4XXZ4CYM94 |
| K1 kapora 60.000 (müşterinin bugünkü toplamı 150.000 + 60.000 = 210.000) | 400 | 400 | "MASAK: 200.000 TL ve üstü … işlem amacı beyanıyla …" |
| K2 aynı siparişe kapora 10.000 (160.000, eşik altı), MASAK yok | 200 | 200 | sipariş S-2026-000001 |

SQL (`hareketler` ve her hareketin `masak_beyanlari` sayısı):
```
01M4JK5J05XK42FEBEJT6J1T0H  SATIS  150000.00  0  NULL          ← eşik altı: beyan YOK
01M4JK5J3N38VKRGEXQ8SHJGNX  SATIS  199999.99  0  NULL
01M4JK5J6HWWY1GAB79X9YJR42  SATIS  200000.00  1  AMAC_BEYANI
01M4JK5J9XXBG4DNAAVX11KDWB  SATIS  370000.00  1  AMAC_BEYANI
```

### 15.4 [ORTA] Kapora: gerçek Chrome (ret → aşağı → yukarı), 8 kombinasyon × 2 genişlik
Senaryo: müşterinin bugün 150.000 ₺ satışı var. "+ Kapora al" ile tutar 60.000 girilip Kaydet'e basılır (A). Sunucu 400 MASAK döner (B). Tutar 10.000'e indirilir (C). Tutar yeniden 60.000 yapılır (D). Betik `chrome/kapora.mjs`, çıktı `chrome/kapora-tum.log`.

| Durum | Sonuç (platin + yıldız × BASİT + PRO × açık + koyu × 1280 + 375, 32 satırın hepsi aynı) |
|---|---|
| A 60.000, ret öncesi | form yok, Kaydet AÇIK |
| B sunucu reddi | `#sipd-kapora-masak` GÖRÜNÜR, Kaydet KİLİTLİ, sebep "Kaydetmek için kimlik bilgilerini doldurun." |
| **C 10.000, eşik altı** | **form YOK, not YOK, Kaydet AÇIK, kilit sebebi YOK** (tur 1'de form yapışıyordu ve gövdeye masak gidiyordu) |
| D 60.000, geri yukarı | form yine görünür, Kaydet kilitli |

Ölçüm (`sikisma-olc.js __olcTam`, `chrome/olcum-kapora.jsonl`, 16 ölçüm, C durumunda):

| şema | mod | tema | gen. | K1 | K3 taşan/kırık | K5 TRY | K6 | K7 | K9 binen | K10 | K12 | yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| platin + yıldız | BASIT + PRO | açık + koyu | 1280 | 0 | 0/0 | 0 | 0/1 | 0 | 6 | 0 | 0 | 0 |
| platin + yıldız | BASIT + PRO | açık + koyu | 375 | 0 | 0/0 | 0 | 0/1 | 0 | 6 | 0 | 0 | 0 |

K9 = 6 bir ölçüm artefaktıdır. Binen örneklerin hepsi çekmece katmanı ile karartılmış arka sayfa arasında: "dd 'Müşteri siparişi' ⟂ #cekmece-kapat", "dd 'Kapora Tarayici' ⟂ #cekmece-siparis-kapora-baslik". Cekmece tasarımı gereği sayfanın üstüne açılır; çekmecenin kendi içinde binme yok. Görüntülerle doğrulandı.

Read ile bakılan görüntüler (3/4): `kapora-yildiz-BASIT-aydinlik-1280-B-red.png` (form + "bugünkü toplamı 210.000 ₺" + kilit), `kapora-yildiz-BASIT-aydinlik-1280-C-alti.png` (10.000, form yok, "Kaporayı al" etkin), `kapora-platin-PROFESYONEL-aydinlik-375-C-alti.png` (375'te aynı). Bütün görüntüler `chrome/foto/kapora-*` altında (15 dosya).

### 15.5 Mutasyonlar (tur 4; yedek `/private/tmp/kuyumcu-serit/mut-yedek/YASAL-KAPI-GERI-t4/`, perl tek eşleşme, geri kopya, `cmp`)
| Kod | Değişiklik | Kırmızı testler | cmp |
|---|---|---|---|
| MK1 | SiparisDetay `kaporaMasakZorla` get ⇒ `kaporaMasakRedIzi is not null` (eski yapışkan) | YKGK01, YKGK02 | AYNI |
| MK2 | kapora gövdesi `masak = KaporaMasakGerekli ? …` ⇒ `masak = kaporaMasakRedIzi is not null ? …` (eşik altında beyan gövdeye gider) | YKGK01 | AYNI |
| MS1 | sözlük `PesinKimlikKilit` metni "ve üstünde" ⇒ "ve üzerinde" (sabit kâhin kanıtı) | **DOG_T24_MASAK_esigi_TAM_ESIT ×2**, YKG04 ×2, YKG05(15000), MASAK370_Pesin ×2, T24_MASAK_kimlik_esigi_USTU ×2 (9 kırmızı) | AYNI |
| MS2 | sözlük `PesinTevsikKilit` metni "üstündeki" ⇒ "üzerindeki" | YKG04 ×2, YKG05(5000.01, 14999.99), T24_had_USTU_KART ×2 (6 kırmızı) | AYNI |

MS1 ve MS2, [DÜŞÜK] "sabit kâhin" bulgusunun kapandığını kanıtlıyor. `Dogrulayici/SATIS3T24SinirTestleri.cs:37` artık sabit `"10.000 ₺ ve üstünde kimlik gerekir — Peşin Müşteri olmaz, müşteri seçin."` bekliyor. YKG04/05 sabit `YkgKimlik15000`/`YkgTevsik5000` literallerini kullanıyor. `YkgKimlik(x)`/`YkgTevsik(x)` yardımcıları sözlükten OKUMAZ: metin sabit, yalnız tutar tr-TR biçimlenir. Sözlük değişince hepsi kırmızı oldu. Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0/0. Kaynakta mutasyon izi yok (grep).

[DÜŞÜK] M1 kırmızı listesi: §5 tablosu tur 3'te doğrulayıcının sonucuyla düzeltildi (YKG07 ×2 dahil, toplam 8).

### 15.6 Tam süit kırmızıları (7) ve sahipleri; bu paketin yeni kırmızısı yok
| Test | Sebep | Sahibi |
|---|---|---|
| `EksikB1SatinalmaEkranTestleri.EK02(MUHASEBE)` | filtreli yeniden koşumda YEŞİL (yük altında kararsız) | EKSIK-B1-SATINALMA (t5) |
| `Yildiz4CTestleri.Y4C_44(BASIT)` | "Secici 'islem-urun': 'u1' seçeneği listede yok. Listedekiler: []"; filtreli yeniden koşumda YEŞİL | YILDIZ-4C (t1) |
| `RaporDortTestleri.RD_KH01`, `RD_KH02` | "Rapor dönemi en çok 400 gün olabilir" (KarZararServisi.DonemDogrula). Test aralıkları `Bugun`/`ayBasi`'na göre üretiliyor, 10.10'da deterministik kırmızı. Bu paket Api koduna dokunmadı; 09.10 tam süitinde yeşildi | RAPOR-DORT |
| `SozlesmeKapsamTestleri.Kapsam_orani…`, `Kilitsiz_tip_listesi…` | tur 1'de de vardı | SATINALMA/KAPORA (t5) |
| `P37cPanelVeKapsamTestleri.Her_ekran_ortak_arac_seridini…` | tur 1'de de vardı | SATINALMA/KAPORA (t5) |

Tur 1 listesiyle fark: YayinAcilis (MySQL çöküşü), Y4B_14/44/45 ve Y4C_41 artık yeşil. Yeni gelenler EK02, Y4C_44 ve RD_KH01/02; dördü de başkasının alanında ve yukarıda gerekçelendirildi. **Bu paketin ürettiği kırmızı: 0.**

### 15.7 Temizlik
- API 5164 (PID 58505 ve çocuğu) ile vekil 5165 durduruldu. `lsof` ile 5164 ve 5165'te dinleyen yok.
- Yeni kiracı `01M4JK4T7DZRZNK6QMZHM8G38P` `kiraci-temizle.sh --uygula` ile silindi: 75 tabloda 0 satır. Eski kiracı da 0. Bu paketin `kiraci_davetleri` satırları (YKGDAV…, 2 adet) silindi.
- tenants şu an 24. Tur 4 başında 21'di; aradaki 3 kiracı başka şeritlerin: PAKETYILDIZ4C2-TEST, PAKETEKSIK-B1-SATINALMA-TEST, -TEST2. `PAKETYASAL%` adlı kiracı sayısı 0. QA-TEST kiracılarına dokunulmadı.
- 5199, 5200 ve 5241'e dokunulmadı. Commit yok.

### 15.8 Kalan küçük not (DÜŞÜK, kapsam dışı bırakıldı)
Kaporada kimlik formu eksiksiz doldurulduğu hâlde sunucu reddederse `kaporaHata = KG.MasakRet` bandı çıkar. Tutar inince bu bant (form değil) yerinde kalır. Kilit ve gövde doğru davranır: form kapanır, Kaydet açılır, gövdede masak gitmez. Yalnız bilgi bandı bayat kalır. Sahibi KAPORA-EKRAN. Önerilen düzeltme, Islemler'deki `YasalIzleriTazele` ile aynı: `KaporaTutarDegisti` içinde `if (!kaporaMasakZorla && kaporaHata == KG.MasakRet) kaporaHata = null;`.
