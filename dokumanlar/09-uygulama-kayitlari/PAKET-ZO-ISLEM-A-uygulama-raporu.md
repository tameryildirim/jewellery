# PAKET ZO-ISLEM-A — Hurda · POS tahsilat · Banka "zorunlu önce" + Hurda "şube bağlamı gerekli" hatası


## 00000000000000000000. 17. TUR — denetim.sh P-23 ODEME_KALEMSIZ_YOL (KRİTİK): 0,00 TL'lik, ödeme kalemi olmayan satış kaydedilebiliyordu — 30.09.2026 00:00–00:20 (t7 · API 5170 · `KUYUMCU_TEST_DB=kuyumcu_test_t7`)
Durum beyanı: **BITTI** (bu turun dosya alanı `Kuyumcu.Api/Modules/Satis/*` + testler; Web tarafına DOKUNULMADI). Çalışma dizini `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r20/`. Migration YOK. Yeni varlık YOK. Yeni uç YOK. Yasal kapılar (MASAK, tevsik, gider pusulası, hediye/KDVK md.27) DEĞİŞMEDİ.

### 17.1 Kök neden (ÖLÇÜLDÜ)
- Canlı sayaç sorgusu (denetim.sh P-23 ile birebir) `kuyumcu_net`'te **2 satır** döndürdü, ikisi de `TEST-KESIF Kuyumculuk` (01M3QFYWC18SPG25CTE5YCAS1H) kiracısında: `01M3QG1P6EVK9HM5CVRTCBKWXJ` SATIS **KART 0.00** (29.09 21:10) ve `01M3QQEA86CZ0ADP59MXZTBVEX` SATIS **NAKIT 0.00** (29.09 23:19). Tüm veritabanında ödeme etiketli + |tutar| = 0 olan başka satır YOK (tür×etiket dökümü: yalnız bu 2 SATIS).
- Mekanizma: tek etiketli yollar hareketi `NAKIT/KART/HAVALE/VERESIYE` ile etiketliyor; kalem fabrikası (`Data/OdemeKalemFabrikasi.Turet`) yalnız `|TlTutari| > 0` iken kalem doğuruyor. `SatisOlusturDto.BirimFiyatTl [Range(0,…)]` 0'ı kabul ettiği için 0 TL "KART satışı" yazıldı, kalemi doğmadı → sayaç kırmızı.
- Aynı delik başka yollarda da açıktı (kodda ölçüldü): fark 0 takas (`NAKIT`/`VERESIYE` etiketli 0 TL), 0 TL alış (`VERESIYE` etiketli), bedelsiz iade (elle 0 tutar ya da hediye satırının iadesi), 0 fiyatlı hurda alış/satış. Çok bacaklı satış fişi `KARMA` etiketli olduğu ve sepet bacak > 0 istediği için sayaca girmiyordu; yine de %100 iskontolu fiş 0 TL'ye düşüp belirsiz "bacak toplamı" hatası veriyordu.

### 17.2 Düzeltme — kuralın TEK kaynağı `OdemeTutarKapisi` (`Modules/Satis/OdemeSepeti.cs` sonu)
Kural, denetim.sh P-23 sayacının koddaki karşılığıdır: **ödeme etiketi taşıyan satır tutarsız olamaz.**
| Yol | Karar | Gerekçe |
|---|---|---|
| Tek satış `POST /islemler/satis` | 0 TL → **400** "Satış tutarı 0 TL olamaz: ödemesi olmayan işlem kaydedilmez. Birim fiyatı girin; hediye verilecekse satış fişinde bedelli bir kalemin yanına 'Hediye' olarak ekleyin." (alan `birimFiyatTl`); ödeme türü boş/geçersiz → **400** (servis içinde de; DTO regex'i yalnız controller yolunda koşar) | 0 TL satış bedelsiz teslimdir; kod tabanındaki mevcut hediye kuralı (KDVK md.27 emsal bedel — `Tek_basina_hediye_REDDEDILIR`) ile aynı gerekçe; yeni mevzuat hükmü YAZILMADI |
| Satış fişi `POST /islemler/satis-fisi` | iskonto sonrası fiş toplamı 0 → **400** "Satış fişi tutarı 0 TL olamaz…" (alan `kalemler`); ödemesiz fiş → **400** alan mesajı "Fiş en az bir ödeme bacağı içermelidir" (eskiden anlamsız "Odemeler en az 1 karakter olmalıdır") | Hediye akışı BOZULMADI: hediye kalem bedelli kalemin yanında 0 TL kalır, fiş `KARMA` etiketlidir, kalemi bacaktan doğar |
| Hurda alış / hurda satış | 0 TL → **400** (alan `birimFiyatTlHasGram`) | 0 TL'lik gider pusulası / ramat çıkışı anlamsız; fiyat kurdan otomatik gelir |
| Alış / alış fişi kalemi | 0 TL **KABUL**, ama ödeme etiketi **null** (kasa/cari satırı zaten yazılmıyordu) | Bedelsiz mal girişi meşrudur ve doğrulayıcı testi `Dogrulayici/L1bSinirTestleri.D1_sifir_birim_fiyat_iki_kalem_kasa_DEGISMEZ_stok_artar_tutar_0` bunu SÖZLEŞME olarak koruyor (ilk sürümde 400 verdim → bu test kırmızıya döndü → kararı "etiket yok" olarak değiştirdim) |
| Takas | fark 0 → etiket **null** (kasa/cari satırı zaten yazılmıyordu) | Para el değiştirmedi; eşit takas yasaklanamaz |
| Satış iadesi / alış iadesi | tutar 0 (bedelsiz — ör. hediye satırının iadesi ya da elle 0) → etiket **null**, kasa/POS/cari satırı **yazılmaz**; paralı iade AYNEN | Hediye malın iadesi engellenemez; 0 TL kasa/POS satırı üretmek defteri kirletirdi |
Değmeyenler (bilerek): `Degisim` (P21) — SATIS bacağı tam tutarla kalem doğuruyor, sayaca girmiyor; `Iscilik` DTO'su zaten `> 0`. Kalem fabrikası (`Data/`) bu şeridin dosya alanı dışında olduğu için DEĞİŞTİRİLMEDİ; kural fabrikadan önce, işlem yolunda uygulanıyor.

### 17.3 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln` → **0 hata, 0 uyarı** (son temiz derleme `r20/build6.log`: 0 hata; tek "uyarı" MSB3026 = başka şeridin koşan testi `Kuyumcu.Tests.dll`'yi kilitlediği için kopyalama yeniden denemesi, derleme başarılı).
- Yeni testler: `Kuyumcu.Tests/ZOISLEMAOdemeTutarKapisiTestleri.cs` — **20 test** (saf 7: `OdemeTutarKapisiSafTestleri.*`; DB 11: `ZOP23_sifir_TL_satis_400_ve_hicbir_satir_YAZILMAZ` ×3 (KART/NAKIT/VERESIYE), `ZOP23_odeme_turu_BOS_satis_servis_yolunda_da_400`, `ZOP23_normal_satis_GECER_ve_kalemi_dogar`, `ZOP23_yuzde_100_iskontolu_satis_fisi_400`, `ZOP23_bedelsiz_alis_ve_alis_fisi_kalemi_KAYDEDILIR_etiket_TASIMAZ`, `ZOP23_sifir_fiyatli_hurda_alisi_ve_satisi_400`, `ZOP23_fark_0_takas_KAYDEDILIR_odeme_etiketi_TASIMAZ`, `ZOP23_bedelsiz_iade_KAYDEDILIR_etiket_ve_kasa_satiri_YOK`, `ZOP23_parali_iade_ETIKETINI_ve_kasa_satirini_KORUR`; HTTP 2: `ZOP23h_sifir_TL_KART_satis_400_mesaj_ve_alan_govdede`, `ZOP23h_odemesiz_satis_fisi_400_Turkce_alan_mesaji` — gövde JSON olarak ayrıştırılır). Her DB testi denetim.sh sorgusunun test içi karşılığını (`KalemsizYol` = 0) da ölçer.
- İlgili süitler (filtre: Satis|Iade|Takas|Hurda|Alis|P23|Fis|Vezne|Pos|Masak|Tevsik|FiyatListesi|Kasa|NakitPayi|Storno|ZOISLEMA|Degisim|Kampanya|Iskonto|L1b|OdemeTutarKapisi): **2392/2392 geçti** (`r20/ilgili2.log`). Son koşum (yeni + SatisFisi + L1b): 79/79.
- Tam süit: görev metni bu tur için "Tam süiti yalnız doğrulayıcı koşar" dediği için **koşulmadı** (başlatılan koşum bu talimat gereği durduruldu; sonuç üretmedi).
- Mutasyonlar (`r20/mut.sh`: kaynak yedeği → perl tek eşleşme → filtreli test → geri kopya → `cmp`):

| Kod | Değişiklik | Kırmızı testler | Geri alındı |
|---|---|---|---|
| M70 | `SifirOlamaz` gövdesi `if (false && ParaYok(..))` | `SifirOlamaz_Turkce_sebep_ve_ALAN_tasir`, `ZOP23_sifir_TL_satis_…` ×3, `ZOP23_sifir_fiyatli_hurda_…`, `ZOP23_yuzde_100_…`, (ilk sürümdeki alış testi) — 7 | cmp AYNI |
| M71 | tek satışta `SifirOlamaz` çağrısı devre dışı | `ZOP23_sifir_TL_satis_400_…` (KART, NAKIT, VERESIYE) | cmp AYNI |
| M72 | `Etiket` her zaman etiketi döndürür | `Etiket_para_yoksa_NULL_varsa_AYNEN`, `ZOP23_bedelsiz_iade_…`, `ZOP23_fark_0_takas_…` | cmp AYNI |
| M73 | satış iadesinde kasa bekçisi `&& !ParaYok(iadeTutar)` kaldırıldı | `ZOP23_bedelsiz_iade_KAYDEDILIR_etiket_ve_kasa_satiri_YOK` | cmp AYNI |
| M74 | tek satışta `EtiketGecerli` devre dışı | `ZOP23_odeme_turu_BOS_satis_servis_yolunda_da_400` (ilk koşum başka şeridin Web derleme kilidi MSB4018 ile derlenemedi → yeniden koşuldu) | cmp AYNI |
| M75 | satış fişinde `SifirOlamaz(fisToplam…)` devre dışı | `ZOP23_yuzde_100_iskontolu_satis_fisi_400` | cmp AYNI |
| M76 | alış kaleminde `Etiket(toplamTl, …)` kaldırıldı | `ZOP23_bedelsiz_alis_ve_alis_fisi_kalemi_KAYDEDILIR_etiket_TASIMAZ` | cmp AYNI |
| M77 | `Odemeler` Türkçe hata mesajı kaldırıldı | `ZOP23h_odemesiz_satis_fisi_400_Turkce_alan_mesaji` | cmp AYNI |
Son mutasyondan sonra temiz `dotnet build` → 0 hata.
- `bash ./denetim.sh` → **DENETİM BAŞARISIZ**, iki ✗ (ikisi de bu turun kodundan DEĞİL):
  - `P-23: ODEME_KALEMSIZ_YOL (beklenen 0) 2` — sayaç GEÇMİŞİ ölçer; sayılan 2 satır kod düzeltmesinden ÖNCE `TEST-KESIF Kuyumculuk` kiracısına yazılmış 0 TL satışlardır (yukarıda). Bu satırlar başka bir iş kolunun (keşif testi) kiracısındadır; finansal tabloda hard DELETE yasak, storno o kiracının sahibinin işi → orkestratöre (kiracıyı açan keşif turu kapanınca kiracı temizlenirse sayaç 0'a döner). Yeni yazım kapandı: canlıda aynı istek artık 400 (aşağıda), bu turun test kiracısında sayaç 0.
  - `P-36b: GIRDI_AILESI_DISI_BICIM 1` — `Kuyumcu.Web/Pages/KarZarar.razor:384` `0m.ToString("N2", GramKultur)`; KarZarar ORTAK-1 şeridinin dosyasıdır (bu turda aynı anda değişti) → sahibi ORTAK-1.
  - P-23'ün diğer sayaçları ✓ (`YENI_YOL 0`, `FABRIKA_KAPISI 1`, `FIS_TOPLAM_FARKI 0`, `CALISMA_ZAMANI_KAPISI 1`).

### 17.4 Canlı doğrulama — HTTP + SQL (API 5170, gerçek ağaç `--no-build`, `kuyumcu_net`)
Kiracı `PAKETZO-ISLEM-A-TEST` 01M3QT2W4RFWZJX8Q5K5BW5EBB (sistem anahtarı rastgele, chmod 600, yalnız süreç ortamında; parolalar yazılmadı). Kurulum: kasa +300.000, müşteri Ayşe, tedarikçi, ürün 22 ayar bilezik, 3 adet HAVALE alış (120.000), 10 g hurda alış. Yanıtlar okundu (`r20/canli3.out`). (Önceki iki kurulum denemesi — stoksuz ürün ve MASAK eşiğine takılan alış — kiracı açıp yarıda kaldı; ikisi de aşağıda silindi.)

| Adım | Beklenen | Gerçek |
|---|---|---|
| A) 0 TL KART satış | 400 | **400** "Satış tutarı 0 TL olamaz: ödemesi olmayan işlem kaydedilmez. Birim fiyatı girin; hediye verilecekse…" alan `birimFiyatTl` |
| B) 0 TL NAKIT satış | 400 | **400** aynı mesaj |
| C) ödeme türü yok | 400 | **400** alan `odemeTuru` "Ödeme türü zorunludur" |
| D) %100 iskontolu fiş (KART 3.000 bacaklı) | 400 | **400** "Satış fişi tutarı 0 TL olamaz… Kalem fiyatlarını ya da iskontoyu düzeltin…" alan `kalemler` |
| E) ödemesiz fiş | 400 anlaşılır | **400** alan `odemeler` "Fiş en az bir ödeme bacağı içermelidir" |
| F) normal KART satış 45.000 | 200 | **200** `tlTutari 45000`, `odemeTuru KART` |
| G) normal fiş 3.000 NAKIT | 200 | **200** `fisId …`, `toplamTl 3000`, `nakitToplam 3000` |
| H) 0 fiyat hurda satış | 400 | **400** "Hurda satışı tutarı 0 TL olamaz…" alan `birimFiyatTlHasGram` |
| I) 0 fiyat hurda alış | 400 | **400** "Hurda alışı tutarı 0 TL olamaz…" |
| J) bedelsiz alış (tedarikçiden numune) | 200, etiket yok | **200** `tlTutari 0`, `odemeTuru null` |
| K) fark 0 takas (45.800 ↔ 45.800) | 200, etiket yok | **200** `farkTl 0` |

SQL (`r20/sql.out`):
```
tur        odeme_turu  tl_tutari   kalem  kalemler             kasa_satiri
ALIS       HAVALE      -120000.00  1      HAVALE:120000.00     1
HURDA_ALIS NAKIT       -4580.00    1      NAKIT:4580.00        1
SATIS      KART        45000.00    1      KART:45000.00        1
SATIS      KARMA       3000.00     1      NAKIT:3000.00        1
ALIS       NULL        0.00        0      NULL                 0
TAKAS      NULL        0.00        0      NULL                 0
kiraci_kalemsiz (P-23 sorgusu, bu kiracı) = 0 · sifir_tl_satis = 0
```

### 17.5 Gerçek tarayıcı
Bu tur ekrana dokunmadı (razor/css/bileşen/sözlük değişmedi; yalnız sunucu kuralı ve sunucu hata metni). Hata metni ekrana mevcut hata bandı/alan-altı yoluyla gelir; o yollar önceki turlarda ölçüldü. Gerçek tarayıcı ölçümü bu tur için UYGULANMADI — gerekçe: ekran dosyası değişmedi.

### 17.6 DoD sözleşmesi (bu tur)
Yeni varlık/kayıt türü yok, yeni uç yok. Finansal satırlara hard DELETE yok; geçmişteki 2 kalemsiz satır silinmedi (storno sahibin işi). Denetim kaydı yolları değişmedi.

### 17.7 Temizlik
API 5170 PID ile durduruldu (`lsof :5170` boş). Bu turda açılan 3 test kiracısı (`01M3QSJ5WGRYXACCMV0GF3QME4`, `01M3QT1YX42JG525NBSK6778P9`, `01M3QT2W4RFWZJX8Q5K5BW5EBB`) `kiraci-temizle.sh --uygula` ile silindi; `PAKETZO-ISLEM-A%` kiracı sayısı **0**. `tenants` = **16** (tur başında 14; aradaki fark başka şeritlerin açık kiracıları: `PAKETSATIS-3-TEST t24`, `PAKETKATMAN-1b-TEST Olcum11` — bu şeridin DEĞİL). Hakem kiracısı 01M3JG487JCP11FMBAFRMR4X4P listede yok. Proje ağacına geçici dosya bırakılmadı (mutasyon yedekleri `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r20/`).

### 17.8 Değişen dosyalar (bu tur) · açık kalanlar
- `dotnet/Kuyumcu.Api/Modules/Satis/OdemeSepeti.cs` — `OdemeTutarKapisi` (ParaYok · SifirOlamaz · EtiketGecerli · Etiket).
- `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs` — Satis / SatisFisi / HurdaAlis / HurdaSatis kapıları; AlisKalemiYaz · Takas · SatisIade · AlisIade etiket/kasa bekçileri; `SatisFisiDto.Odemeler` Türkçe mesajı.
- `dotnet/Kuyumcu.Tests/ZOISLEMAOdemeTutarKapisiTestleri.cs` (yeni, 20 test).
Açık kalanlar: (1) P-23 canlı sayacı `TEST-KESIF Kuyumculuk` kiracısındaki 2 geçmiş satır yüzünden ✗ — sahibi keşif turu/orkestratör (kiracı silinince ya da satırlar storno edilince 0); (2) P-36b ✗ `KarZarar.razor:384` — ORTAK-1; (3) tam süit bu tur doğrulayıcıya bırakıldı (görev talimatı); (4) Web tarafında satış ekranı 0 fiyatla "Kaydet"i önden kilitlemiyorsa sunucunun 400 bandı görünür — ekran kilidi SATIS-3 şeridinin (Islemler*) işi.

## 0000000000000000000. 16. TUR — önceki doğrulamada kalan 3 bulgu: Hurda MASAK çıkmazı (KRİTİK) · Dekont Tutar "0" (YÜKSEK) · POS "Öneri getir" birebir tutanı önermiyor (YÜKSEK) — 29.09.2026 14:36–15:10 (t7 · API 5170 · vekil 5171 · CDP 9803–9808 · `KUYUMCU_TEST_DB=kuyumcu_test_t7`)
Durum beyanı: **BITTI** (paralel şerit kapısının 7 maddesi aşağıda). Çalışma dizini `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r19/`. Migration YOK. Yeni varlık YOK, yeni DOM kimliği YOK. Uç değişikliği tek: `GET /masak/gereksinim` isteğe bağlı `cariId` parametresi aldı (geriye uyumlu). Yasal kapılar (MASAK kapısı, tevsik, gider pusulası) DEĞİŞMEDİ; kayıt aynı `MasakKapisi.DogrulaKumulatif` ile doğrulanıyor, yalnız ekran artık aynı ölçüyle soruyor. Devir notu 3 (POS elle eşleştirme `cift_sira`, harf duyarlılığı, `OtoEslestirOnerisi` kalanı, `ElleSec` kısmi seçimi) önceki turlarda kapanmıştı; testleri bu turun tam süitinde yeşil.

### 0000000000000000000.1 Spesifikasyonu koda karşı doğrulama (kalan bulgular hâlâ açık mıydı?)
| Bulgu | Koddaki durum (okundu) | Sonuç |
|---|---|---|
| Hurda MASAK çıkmazı | `Hurda.razor` `MasakTazele` gereksinimi yalnız tutarla soruyordu (`Gereksinim(t)`); sunucu `SatisModulu.HurdaSatis` → `MasakKapisi.DogrulaKumulatif(cariId)` ile **günlük toplamı** ölçüyor. 8. turdaki düzeltme yalnız "eşik altı reddinde en az kimlik eşiğiyle sor" diyordu → kümülatif kademe (amaç/açıklama/fon) hiç çizilmiyordu; tüm alanlar doluyken gelen MASAK reddi de yalnız genel not gösteriyordu. | AÇIKTI → düzeltildi |
| Dekont Tutar "0" | `Banka.razor` `private decimal dekontTutar;` + `dekontTutar = v ?? 0m` + açılışta `dekontTutar = 0` → `SayisalAlan.OnParametersSet` kutuya "0" yazıyordu. | AÇIKTI → düzeltildi |
| POS öneri birebir | `PosModulu.OtoEslestirOnerisi` yalnız (cihaz, valör günü) kümesinin TAMAMINI deniyordu; aynı gün 1.470 + 26.950 varken 26.950'lik yatış için küme 28.420 → `bulundu:false`. | AÇIKTI → düzeltildi |

### 0000000000000000000.2 Bulgu → kök neden → düzeltme
| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| [KRİTİK] Hurda ramat çıkışında MASAK döngüsü (ekran YALNIZ_KIMLIK soruyor, sunucu kümülatifle amaç/açıklama/fon istiyor; pencere kapanıyor, sebep yok) | **Sunucu** (`Modules/Masak/MasakModulu.cs`): `MasakServisi.Gereksinim(tutar, cariId, tenantId)` — cari verilirse kademe `motor.Gereksinim(|MasakKapisi.GunlukIslemToplam(...)| + |tutar|)`; yani kaydın geçtiği kapıyla **aynı fonksiyon, aynı ölçü** (tek kaynak). Yanıt yalnız kademe taşır (toplam/tutar dönmez); başka kiracının carisi → toplam 0 (kiracı koşulu `GunlukIslemToplam` içinde elle de yazılı); Peşin Müşteri kartı → 0 (tek işlem kuralı). Uç `[HerkeseAcik]` kaldı (TEZGÂHTAR satış ekranı da çağırır). **İstemci** (`UyumServisi.Gereksinim(tutar, cariId = null)`): `&cariId=` ekler. **Hurda.razor**: `MasakSorulmali` (tutar eşikte ∨ MASAK reddi ∨ ramatçı seçili) → gereksinim (tutar, ramatçı) anahtarıyla sorulur; `MasakGerekli` eşik altında da sunucu `kimlikGerekli` derse alanları açar; ramatçı değişince (`CariSecildi → TevsikTazele → MasakTazele`) yeniden sorulur. Ramatçı seçiliyken sorgu tutarı işlemin kendi tutarıdır (toplamı sunucu ekler); müşterisiz zorla açılışta eski `max(tutar, kimlik eşiği)` kuralı korunur. `MasakAlanlariniAc(e)`: ekrandaki MASAK alanlarının HEPSİ doluyken sunucu yine MASAK'la reddederse (ör. uyruk kodu) sunucunun cümlesi `#hrd-hata` bandına yazılır — sessiz döngü kalmaz. | DB `ZO29z_MASAK_gereksinimi_MUSTERIYLE…` · bUnit `ZO29z_hurda_MASAK_gereksinimi_RAMATCIYLA…`, `ZO29z_hurda_esik_alti_RAMATCI_toplami…`, `ZO29z_hurda_MASAK_alanlari_doluyken_ret_SEBEBI…` · M135–M138 · canlı Z (ekrandan kayıt, SQL `masak_beyanlari.kademe = AMAC_BEYANI`) |
| (aynı sınıf, Banka) Dekontta müşteri seçiliyken günlük toplam eşiği aşıyorsa eşik altında ekran alan açmıyordu | `Banka.razor`: `MasakSorulmali`/`MasakGerekli` aynı kalıp; müşteri seçici `@bind` yerine `DegerChanged="DekontCariDegisti"` → gereksinim (tutar, müşteri) ile yeniden sorulur. Müşterisiz eşik altında sorgu YOK (eski davranış). | bUnit `ZO29z_banka_dekont_MUSTERI_secilince…` · M140 |
| [YÜKSEK, K18/K20] Dekont Tutar kutusu "0" ile açılıyor, "250000" → "0250000" | `dekontTutar` → `decimal?` (açılışta `null`), gövde ve MASAK bileşeni `DekontTutarTl => dekontTutar ?? 0m`; boş kutu `Pozitif` ile alan altı mesajı verir, POST gitmez. Hurda'nın Para kutularıyla aynı davranış. | bUnit `ZO29z_banka_dekont_tutar_BOS_acilir…` · M139 · canlı B (değer `""` → yazınca `250000` → odak dışı `250.000`) |
| [YÜKSEK] POS "Öneri getir" 26.950 ₺ yatış ↔ 26.950 ₺ net tahsilatı önermiyor ("tutmuyor") | `PosModulu.OtoEslestirOnerisi`: gün kümesi tutmazsa kalanı `hareketKalan`a (hareketin eşleşmemiş tutarı) tolerans içinde BİREBİR eşit TEK tahsilat aranır; **birden çok eşit aday varsa öneri YOK** (hangisinin yattığı bilinemez — varsayım yapılmaz, Anayasa md.1). Öneri yine hiçbir şey yazmaz; aynı `PosYatmasiKapisi` ve cihaz-hesap kısıtı geçerli. | DB `ZO29z_oneri_gun_kumesi_tutmasa_da_BIREBIR…`, `ZO29z_oneri_BIREBIR_iki_aday…BOS_doner`, `ZO29z_oneri_BIREBIR_kismi_eslesmis_harekette_KALANA_bakar` · M141–M143 · canlı HTTP + ekran P |

Hakem notu "tezgâhtar yanlış satırı işaretlerse kısmi eşleştirme sessizce kabul ediliyor": kısmi eşleştirme sunucunun bilinçli kuralı (devir notu 3 "sunucu kısmiyi destekliyor"); ekran seçilen ve yatan toplamı 20 px kalın satırda yan yana yazıyor ("Seçilen 26.950 ₺ · bankaya yatan 26.950 ₺"), yanlış seçim artık önerinin doğru satırı işaretlemesiyle önleniyor. Kural değiştirilmedi.

### 0000000000000000000.3 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo --no-incremental` (son mutasyondan sonra, `r19/build-son.log`) → **0 Hata, 1 Uyarı** (`EtiketEkranTestleri.cs(145) xUnit1031` → ORTAK-1, bu paketin değil).
- Yeni testler (+9): `ZOISLEMA6Testleri.cs` (DB, 4): `ZO29z_MASAK_gereksinimi_MUSTERIYLE_gunluk_toplam_uzerinden_kapiyla_ayni`, `ZO29z_oneri_gun_kumesi_tutmasa_da_BIREBIR_tutan_TEK_tahsilati_onerir`, `ZO29z_oneri_BIREBIR_iki_aday_varsa_varsayim_yapmaz_BOS_doner`, `ZO29z_oneri_BIREBIR_kismi_eslesmis_harekette_KALANA_bakar`; `ZOISLEMAEkranTestleri.cs` (bUnit, 5): `ZO29z_hurda_MASAK_gereksinimi_RAMATCIYLA_sorulur_kumulatif_kademe_alanlari_cizilir`, `ZO29z_hurda_esik_alti_RAMATCI_toplami_kimlik_istiyorsa_alanlar_KAYITTAN_ONCE_acilir`, `ZO29z_hurda_MASAK_alanlari_doluyken_ret_SEBEBI_bantta_gorunur`, `ZO29z_banka_dekont_tutar_BOS_acilir_yazilan_sifirla_birlesmez`, `ZO29z_banka_dekont_MUSTERI_secilince_gereksinim_musteriyle_sorulur`. Filtreli: **9/9**; Banka + ZO29z + ZOISLEMA6 + MigrationHazirlik **37/37** (`r19/t3.log`).
- Değiştirilen mevcut test (gerekçeli): `BankaEkranTestleri` (paket 41) — sahte `/masak/gereksinim` her tutara (1.000 dahil) `AMAC_BEYANI` dönüyordu (gerçekçi değil); tutara göre gerçekçi yapıldı (kimlik eşiği altı → `YOK`). `Esik_altinda_MASAK_alanlari_GORUNMEZ` testinin "gereksinim hiç sorulmaz" satırı yeni kurala çevrildi: müşteri seçiliyken sorgu MÜŞTERİYLE yapılır (`cariId=c1`), alanlar yine GÖRÜNMEZ (sunucu YOK derse). Müşterisiz eşik altı "sorgu yok" kuralı yeni `ZO29z_banka_dekont_MUSTERI…` testinde korunuyor.
- Mutasyon (`r19/mut.sh`: yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/r19/<kod>-<dosya>`, `perl -0777` tek eşleşme, derle, filtreli test, geri kopyala, `cmp`; çıktı `r19/mutlar.out`). **Hiçbir testi kırmayan mutasyon: 0.**

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M135 | `MasakModulu`: `motor.Gereksinim(|onceki| + |tutar|)` → `motor.Gereksinim(|tutar|)` | `ZOISLEMA6Testleri.ZO29z_MASAK_gereksinimi_MUSTERIYLE_gunluk_toplam_uzerinden_kapiyla_ayni` | AYNI |
| M136 | `Hurda.razor`: `Gereksinim(t, c)` → `Gereksinim(t, null)` | `ZO29z_hurda_MASAK_gereksinimi_RAMATCIYLA…`, `ZO29z_hurda_esik_alti_RAMATCI_toplami…` | AYNI |
| M137 | `Hurda.razor` `MasakGerekli`: `|| masakGereksinim?.KimlikGerekli == true` silindi | `ZO29z_hurda_esik_alti_RAMATCI_toplami…` | AYNI |
| M138 | `Hurda.razor` `MasakAlanlariniAc`: dolu alanda sunucu cümlesi → `null` | `ZO29z_hurda_MASAK_alanlari_doluyken_ret_SEBEBI_bantta_gorunur` | AYNI |
| M139 | `Banka.razor`: `Deger="@dekontTutar"` → `Deger="@(dekontTutar ?? 0m)"` | `ZO29z_banka_dekont_tutar_BOS_acilir_yazilan_sifirla_birlesmez` | AYNI |
| M140 | `Banka.razor`: `Uyum.Gereksinim(t, c)` → `Uyum.Gereksinim(t)` | `ZO29z_banka_dekont_MUSTERI_secilince…`, `BankaEkranTestleri.Esik_altinda_MASAK_alanlari_GORUNMEZ` | AYNI |
| M141 | `PosModulu`: `birebir.Count == 1` → `>= 1` | `ZO29z_oneri_BIREBIR_iki_aday_varsa_varsayim_yapmaz_BOS_doner` | AYNI |
| M142 | `PosModulu`: birebir dalı `if (false && …)` | `ZO29z_oneri_gun_kumesi_tutmasa_da_BIREBIR…`, `ZO29z_oneri_BIREBIR_kismi…` | AYNI |
| M143 | `PosModulu`: birebir karşılaştırmada `hareketKalan` → `hareket.Tutar` | `ZO29z_oneri_BIREBIR_kismi_eslesmis_harekette_KALANA_bakar` | AYNI |

- `bash ./denetim.sh` (son kod, `r19/denetim.log`) → **DENETİM TEMİZ**.

### 0000000000000000000.4 Tam süit (paralel şerit kapısı madde 7)
Gerçek ağaçta, `KUYUMCU_TEST_DB=kuyumcu_test_t7`, TRX `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-r19.trx`, günlük `r19/tam.log`: **7198 test, 7196 geçti, 2 kırmızı**:
- `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` → **ORTAK-1** (13.–15. turlarda da aynı).
- `DugmeIkiTestleri.D2_11_ekranlar_favori_yildizi_DINLENIRKEN_gorunur_ve_kenar_tasir` → **DUGME-2** (`css/dugme.css` koşum sırasında 14:59'da değişti; bu paket CSS'e dokunmadı).
- Ara koşumda (`r19/t2.log`) görülen `MigrationHazirlikTestleri.EksikMigrationliSemadaBekleyenListesiDolar` ("Unknown database 'kuyumcu_t1_eski'", başka şeridin paylaşılan veritabanı adı) ve `SATIS3EkranTestleri.Arama_harf_harf…DEBOUNCE` tam süitte yeşil (kararsız, sahipleri başka şerit).
- Bu paketin testleri (ZOISLEMA* + Dogrulayici.ZOISLEMA*) tam süitte **260/260 yeşil**. **FARK (bu paketin yeni kırmızısı) = 0.**

### 0000000000000000000.5 Canlı doğrulama — HTTP + SQL + EKRAN
Ortam: API 5170 (gerçek ağaç, `--no-build`); kiracı `PAKETZO-ISLEM-A-TEST` 01M3PGB5JWX6KJYJ43TGMEGMB9; sistem anahtarı rastgele, chmod 600, yalnız süreç ortamında; parolalar chmod 600 fikstürde; yanıtlar okundu (`r19/kur.out`, `kur2.out`, `canli.out`, `sql1.out`, `sen/s1.log`, `sen/s2.log`). Kurulum: SAHİP + şubeli MÜDÜR, kasa (+50.000 ₺), müşteri Ayşe, ramatçı Abdurrahman, POS cihazı (Garanti, %2), 1.500 + 27.500 ₺ işçilik KART (net 1.470 + 26.950, aynı valör günü), POS hesabına 26.950 ve 1.470 ₺ GELEN, ramatçıdan Ziraat'e bugün 150.000 ₺ havale (müşterili dekont), 5 × hurda alışı (45,8 has g). Hepsi 200/201 (ilk betikte POS cihazı yanıtı `{cihaz:{…}}` sarmalı olduğu için betik durdu; `kur2.py` kalan adımları tamamladı).

| Adım | Beklenen | Gerçek |
|---|---|---|
| `GET /masak/gereksinim?tutar=65633.4` | YOK | **200** `YOK` |
| aynı `&cariId=<ramatçı>` (150.000 + 65.633 = 215.633) | AMAC_BEYANI | **200** `AMAC_BEYANI`, amacGerekli true |
| `tutar=185000` / `&cariId=<ramatçı>` | YALNIZ_KIMLIK / AMAC_BEYANI | **200** `YALNIZ_KIMLIK` / **200** `AMAC_BEYANI` (bulgudaki çelişkinin sunucu tarafı kapandı) |
| `tutar=65633.4&cariId=<Ayşe>` (günlük 29.000 KART) | YOK | **200** `YOK` |
| `cariId=` başka kiracının hakem kiracısı kimliği | tek işlem kuralı | **200** `YOK` (toplam okunmadı) |
| `POST /pos/oto-eslestir-onerisi` 26.950 hareketi | bulundu, tek satır 26.950 | **200** `bulundu:true`, `toplam 26950`, satır `…X4V1PNCMY3MY47RT80` 26.950 (eskiden `bulundu:false`) |
| aynı, 1.470 hareketi | bulundu, 1.470 | **200** `bulundu:true`, satır `…VRA7CX0EWCFP3PDZ01` 1.470 |
| **EKRAN Z** (MÜDÜR, BASİT aydınlık 1440): has 10 (65.637 ₺, eşik altı) | müşterisiz MASAK alanı yok; ramatçı seçilince amaç+açıklama alanları KAYITTAN ÖNCE | müşterisiz `{masak:false}`; "Abdurrahman" seçilince `{masak:true, belge, amac, aciklama:true, fon:false}`; Havale seçildi, alanlar dolduruldu → kilit yok → onay "…Tutar: 65.637 ₺ · Tahsil edilen: 65.637 ₺ (Havale)…" → bant "Hurda satışı kaydedildi: Altın 10,000 g · 65.637 ₺ · Kalan hurda: 35,800 g"; hata bandı / onay hatası / MASAK notu YOK (tek denemede kayıt, döngü yok) |
| SQL (Z) | HURDA_SATIS + kimlik tespiti + beyan AMAC | `hareketler` 01M3PGEBSBMK5ZXYQ92VWWX8M2 `HURDA_SATIS has -10 tl 65637.00 cari=ramatçı`; `masak_kimlik_tespitleri` 1 satır (65637.00, TC_KIMLIK, kaynak HAREKET); `masak_beyanlari.kademe = AMAC_BEYANI`, `islem_amaci`/`aciklama` dolu |
| **EKRAN Z** PRO karanlık 375 | aynı | aynı akış, bant "…65.606,30 ₺ · Kalan hurda: 25,800 g" |
| **EKRAN B** dekont (BASİT aydınlık 1440 ve PRO karanlık 375) | kutu boş, "250000" → "250000" | açılış değeri `""`; yazınca `250000`; odak dışı `250.000` (iki boyutta aynı) |
| **EKRAN P** POS (BASİT aydınlık 1440 ve PRO karanlık 375): +26.950 ₺ seçildi → Öneri getir | 26.950 satırı işaretli | "Net: 26.950 ₺ · 1"; `pos-elle-…X4V1…` işaretli, 1.470 satırı işaretsiz; "Seçilen 26.950 ₺ · bankaya yatan 26.950 ₺" |

### 0000000000000000000.6 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum MÜDÜR, taze profil)
- Senaryo: `r19/sen/senaryo.mjs` (CDP 9803) × {BASİT aydınlık 1440, PRO karanlık 375}; ölçüm matrisi `r19/sen/sahne.mjs` + `kos.sh` (CDP 9804/9806/9807/9808): /pos/tahsilatlar, /hurda, /banka (+ hurda hata durumu) × {1440,1280,960,375} × {BASİT,PRO} × {aydınlık,karanlık} = **64 ölçüm** (`sen/sonuc.jsonl`), 16/16 koşum rc=0, mod 64/64 istenen modda.

| Ölçü | Sonuç |
|---|---|
| yatay, K1, K3_tasan, K3_kirik, K5_TRY, K7, K8, K9_binen, K9b, K10_elipsis, K10b, K11, K12, K13 | **64/64 sıfır** |
| Açılış 1440 (K4_boyKat) | 3 ekran + hata durumu × BASİT/PRO × aydınlık/karanlık → **16/16 = 1 ekran** |
| ham "şube bağlamı" / "şube atanmamış" | 0 / 0 |

- `K2_kartSagYuzde` bilgi alanıdır (kart sağ kenarı %), bulgu değildir. `dugme-olc` ve `tutarlilik.mjs` bu turda YENİDEN KOŞULMADI: açılışta yeni öğe/düğme yok (MASAK alanları yalnız sunucu isteyince çizilir; dekont kutusu yalnız boş açılıyor). Son ölçümleri § 00000000000000000.5'te.
- Görüntüler (Read ile baktım), kopyaları `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/ZO-ISLEM-A-r19-*.png` (6 dosya): `Z1-ramatci-secilince-MASAK-alanlari_BASIT_aydinlik_1440` (belge, TCKN, ad soyad, uyruk, İşlem Amacı*, Beyan Açıklaması* — eşik altı tutarda, kayıttan önce), `Z4-kaydedildi_PRO_karanlik_375` (yeşil bant, taşma yok), `B1-dekont-acilis_PRO_karanlik_375` (Tutar kutusu boş), `B2-dekont-250000_BASIT_aydinlik_1440` ("250.000", Kaydet gerekçeli kilitli: "Kaydetmek için kimlik bilgilerini doldurun."), `P1-oneri-birebir_BASIT_aydinlik_1440` ve `_PRO_karanlik_375` (26.950 satırı işaretli).
- Gözlem (gerekçeli, dokunulmadı): MASAK kutusunun başlığı "Kimlik bilgisi gerekli (tutar yüksek)" eşik altında (kümülatif nedenle) de aynı; `MasakFormu` ortak bileşen ve metin paket 41'in sözlüğünde — kurala dokunmadan "(bugünkü işlemlerle birlikte)" eki önerisi açık kalanlarda. POS "Net: 26.950 ₺ · 1" satırı önceki turların metni.

### 0000000000000000000.7 Sapmalar ve gerekçeleri
- **Dosya alanı dışı tek dokunuş**: `Kuyumcu.Api/Modules/Masak/MasakModulu.cs` (+`Kuyumcu.Web/Servisler/UyumServisi.cs`). Gerekçe: hurda kaydını doğrulayan kural (`DogrulaKumulatif`) cariyi ve günlük toplamı kullanıyor; ekranın aynı cevabı alabilmesinin tek doğru yolu gereksinim ucuna aynı ölçüyü koymak (Bertan/Taşkın gözü: kural sunucuda, ekran yansıtır; istemcide toplam hesaplamak ikinci kaynak olurdu, sunucu mesajını ayrıştırmak kırılgan). Değişiklik ekleyici ve geriye uyumlu (parametresiz çağrı aynı yanıtı döner); eşzamanlı şeritlerin hiçbirinin alanında değil. **Mehmet (güvenlik) notu**: uç `[HerkeseAcik]` (kiracı içi her rol) — cari verilince yanıt yalnız KADEME döner, toplam dönmez; başka kiracı carisi 0 sayılır. Kiracı içi bir kullanıcının "bu müşteri bugün eşiğe yaklaştı mı" bilgisini kademe olarak görebilmesi, aynı kullanıcının zaten satış ekranında gördüğü bilgidir; yine de kurucu/Mehmet onayına açık kalanlarda.
- `BankaEkranTestleri` (paket 41) sahte yanıtı gerçekçi yapıldı ve bir satırı yeni kurala çevrildi (§ .3 gerekçesi).
- Mevzuat: yeni hüküm YOK. Eşikler yine `MasakEsikleri` yapılandırmasından; kümülatif (bağlantılı işlem) ölçüsü zaten mevcut `GunlukIslemToplam` (paket 41 / B-3) — yeniden yazılmadı, çağrıldı.

### 0000000000000000000.8 DoD sözleşmesi (bu tur)
Yeni varlık/kayıt türü YOK. Hurda satışı ve MASAK kimlik tespiti/beyanı değişmez (append-only) kayıtlardır; ekrandaki `DegismezIpucu` (`ZO.BankaMasakKilit`) ve düzeltme yolları değişmedi. Dekont: mevcut düzenleme/ters kayıt yolları değişmedi. POS önerisi hiçbir şey yazmaz; eşleştirme geri alma yolu (`eslesmeler/{id}/geri-al`) değişmedi. Yeni sözlük metni YOK; CSS değişmedi; Razor özniteliklerinde `{ }` lambda yok.

### 0000000000000000000.9 Temizlik
- API 5170 ve vekil 5171 durduruldu (`lsof` dinleyen 0); Chrome süreçleri kapandı (`pgrep` 0).
- Kiracı 01M3PGB5JWX6KJYJ43TGMEGMB9, `tenant_id` sütunu olan 70 tablodan ve `tenants`'tan `r19/sil.sql` ile silindi: önce **412** satır, sonra **0** (`r19/say.sql`); `PAKETZO-ISLEM%` ve hakem kiracısı 01M3JG487JCP11FMBAFRMR4X4P sayısı **0**.
- `tenants`: başlangıç **15**, son **15**. QA-TEST kiracılarına dokunulmadı.
- Disk: hook `rm`'yi engellediği için `r19/sen/profil-*` Chrome profilleri (ve önceki turun `r18/agac`, `r18/sen/profil-*`) kurucu/orkestratör tarafından silinmeli. Proje ağacında .bak/.tmp/.mutorj yok (yedekler `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/r19/`).

### 0000000000000000000.10 Değişen dosyalar (bu tur) · koşul karşılama · açık kalanlar
- Değişen dosyalar:
  - `dotnet/Kuyumcu.Api/Modules/Masak/MasakModulu.cs` (`Gereksinim(tutar, cariId, tenantId)` + uç `cariId` parametresi)
  - `dotnet/Kuyumcu.Api/Modules/Pos/PosModulu.cs` (`OtoEslestirOnerisi` birebir tek aday)
  - `dotnet/Kuyumcu.Web/Servisler/UyumServisi.cs` (`Gereksinim(tutar, cariId = null)`)
  - `dotnet/Kuyumcu.Web/Pages/Hurda.razor` (MASAK gereksinimi ramatçıyla; dolu alanda ret sebebi bantta)
  - `dotnet/Kuyumcu.Web/Pages/Banka.razor` (dekont tutarı `decimal?`; gereksinim müşteriyle)
  - `dotnet/Kuyumcu.Tests/ZOISLEMA6Testleri.cs` (yeni, 4 DB testi), `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (+5 bUnit), `dotnet/Kuyumcu.Tests/BankaEkranTestleri.cs` (sahte yanıt gerçekçi)
- Koşul karşılama: (1) derleme 0 hata ✓ · (2) paket testleri 0 kırmızı (9/9, 37/37, tam süitte 260/260) ✓ · (3) denetim.sh TEMİZ ✓ · (4) 9 mutasyon, hepsi adlı testte kırmızı, cmp AYNI ✓ · (5) canlı HTTP + SQL + ekran ✓ · (6) test kiracısı silindi, tenants 15→15 ✓ · (7) tam süit farkı 0 ✓.
- Açık kalanlar (sahibiyle): ORTAK-1 `O8_basit_metinler_jargon…` · DUGME-2 `D2_11_ekranlar_favori_yildizi…` (dugme.css koşum sırasında değişti) · `MigrationHazirlikTestleri` paylaşılan `kuyumcu_t1_eski` yarışı (t1 şeridi) · SATIS-3 DEBOUNCE kararsızlığı · `EtiketEkranTestleri` xUnit1031 uyarısı (ORTAK-1) · **Mehmet/kurucu**: `/masak/gereksinim?cariId=` kademe bilgisinin kiracı içi tüm rollere açık olması (§ .7) · MASAK kutusu başlığı eşik altı kümülatif durumda "(tutar yüksek)" diyor — `MasakFormu` ortak bileşen/sözlük, metin önerisi paket 41 sahibine · Satış/iade/takas ekranlarının MASAK gereksinimi de müşterisiz soruluyorsa aynı sınıf hata orada da olabilir — yeni uç parametresi hazır, ilgili şerit (SATIS-3) `Uyum.Gereksinim(tutar, cariId)` ile bağlamalı · önceki turların açık kalanları değişmedi.

---

## 000000000000000000. 15. DOĞRULAMA — doğrulayıcı KRİTİK: has 6 haneden fazla ondalıkta onaydaki tutar ≠ kaydedilen tutar, ramatçı boşken 400 — 29.09.2026 13:17–14:00 (t7 · API 5170 · vekil 5171 · CDP 9803–9809 · `KUYUMCU_TEST_DB=kuyumcu_test_t7`)
Durum beyanı: **BITTI** (paralel şerit kapısının 7 maddesi aşağıda). Çalışma dizini `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r18/`. Migration YOK. Yeni uç, varlık, DOM kimliği YOK. POST gövdesinin biçimi, yetki, yasal kapılar (MASAK, tevsik, gider pusulası) DEĞİŞMEDİ. Dokunulan sayfa yalnız `Hurda.razor` (paketin kendi sayfası; gerekçe: doğrulayıcı KRİTİK). Önceki turlarda kapanan devir notu 3 bulguları (POS elle eşleştirme çıkmazı `cift_sira`, harf duyarlılığı, `OtoEslestirOnerisi` kalanı, `ElleSec` kısmi seçimi) § 000000000'da; bu turda o koda DOKUNULMADI, testleri tam süitte yeşil.

### 000000000000000000.1 Bulgu → kök neden → düzeltme
| Bulgu | Kök neden (kodda okundu, canlıda ölçüldü) | Düzeltme |
|---|---|---|
| [KRİTİK] has `1,0000005` → onay "Tutar 6.800,00", sunucu 6.800,01 hesaplıyor → tahsil 0,01 eksik → ramatçı boşken 400 (`Dogrulayici.ZOISLEMASinirHurdaOnayKayitTestleri.ZOSO_has_7_hane_…` KIRMIZI) | Sunucu `SatisModulu.HurdaSatis` (satır 1316–1323) hası `Yuvarla6` (AwayFromZero) ile yuvarlayıp TL'yi yuvarlanmış hasla hesaplıyor; ekran `Tutar => Math.Round(KabulHas * fiyat, 2)` ham hası çarpıyordu. Canlı kanıt (5170, `r18/canli.out`): has 1.0000005, fiyat 6.565,55, ham çarpım 6.565,55 tahsil → **400** "Tahsil edilmeyen 0,01 TL için ramatçı carisi zorunludur…" | İki yol değerlendirildi: (a) ekranda `Has6` ile yuvarlayıp onayda yuvarlanmış değeri göstermek, (b) alanı sunucunun kabul ettiği 6 haneyle sınırlamak. **Seçilen (b)**: `Kaydet()` içinde `hasGram` ve `aliciKabulHas` için `x != Math.Round(x, 6)` ise alan altı mesaj `ZO.HurdaHasHaneSiniri` ("Has gram en çok 6 ondalık hane alır (ör. 1,000001). Fazla haneyi silin." / EN karşılığı) → onay açılmaz, POST gitmez. Gerekçe: (a) önce kurgulandı ve kodda doğru çalıştı, ama kullanıcının yazdığı 1,0000005 ile kaydedilen 1,000001 farklı olur (sessiz yuvarlama). (b)'de yazılan = onaylanan = gönderilen = kaydedilen, istemci-sunucu arasında ikinci bir yuvarlama kuralı yaşamaz. Doğrulayıcının testi iki yolu da kabul ediyor ("ekran reddetti: kabul edilebilir"). (a) denemesinde onay sonrası pencere kapandığı için doğrulayıcı testi `#hrd-onay-hedef`'i bulamadı; bu da (b)'yi destekledi. |
| Sahte sunucu açığı (ZO29u sınıfının `SatisYaniti` `Round6`yı atlıyordu) | `ZOISLEMAHurdaTahsilTestleri.SatisYaniti` has ve kabulü ham kullanıyordu | Sahte sunucu da sunucu kuralına getirildi: `Math.Round(has/kabul, 6, AwayFromZero)` sonra `Math.Round(kabul × fiyat, 2, AwayFromZero)`. |

Sayısal kenar durumları (orkestratör 13:16 tablosu), bu turda ölçüldü:

| Girdi (has) | Ekran | Gövde | Sunucu / SQL |
|---|---|---|---|
| `0,0001` | onay 0,68 ₺ (fiyat 6.800) | 0.0001 | kayıt 0,68 (`ZOSO_has_4_hane_0_0001…` yeşil) |
| `1,0000005` (7 hane) | alan hatası, onay YOK, POST YOK | — | — (canlı ekran F; bUnit `ZO29y_has_7_hane…`, `ZOSO_has_7_hane…`) |
| alıcı kabul `1,9999995` | alan hatası, POST YOK | — | — (canlı ekran H; `ZO29y_alici_kabul_7_hane…`) |
| `1,000001` (6 hane), ramatçı boş | onay "Tutar 6.565,78 ₺", tahsil 6.565,78 | hasGrami 1.000001 | **200**, SQL `hareketler.tl_tutari 6565.78`, `nakit_tutar 6565.78`, `has_grami -1.000001`, `cari_id NULL`, `kasa_hareketleri.tutar 6565.78` |
| `1.500` (belirsiz) | alan hatası, POST YOK | — | — (`ZOSO_sifir_eksi_bos_belirsiz…("1.500")` yeşil) |
| `1,5` | onay 10.200 ₺ (fiyat 6.800) | 1.5 | kayıt 10.200 (`ZOSO_turkce_1_5…` yeşil) |

### 000000000000000000.2 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra, `r18/build-son.log`) → **0 Uyarı, 0 Hata**. (Ara derlemelerde görülen tek uyarı `EtiketEkranTestleri.cs(145) xUnit1031` → ORTAK-1, bu paketin değil.)
- Yeni testler (`Kuyumcu.Tests/ZOISLEMAHurdaTahsilTestleri.cs`, +3): `ZO29y_has_7_hane_ALAN_HATASI_onay_yok_POST_yok`, `ZO29y_alici_kabul_7_hane_ALAN_HATASI_POST_yok`, `ZO29y_has_6_hane_ONAY_GOVDE_KAYIT_ayni_ramatci_bos`.
- Doğrulayıcı testi `ZOSO_has_7_hane_onaydaki_tutar_sunucunun_kaydettigi_tutardir` (DEĞİŞTİRİLMEDİ): KIRMIZI (`r18/t0.log`, "Tahsil edilmeyen 0.01 TL…") → **YEŞİL**.
- Filtreli: `ZOISLEMASinirHurda|ZOISLEMAHurdaTahsil` **45/45** üç ardışık koşumda (`t4-1..3.log`; üçünde de yalnız o sırada dosyada duran geçici hata ayıklama testim kırmızıydı, sonra silindi — `tahsil.cs.ok` ile geri yüklendi, dll'de yok, kanıt `b3.log` sonrası `grep -a -c ZZDEBUG` = 0).
- Kararsızlık notu: `ZOSO_has_7_hane…` bir kez (`t2.log`) kırmızı çıktı, sonra tek başına ve üç sınıf koşumunda yeşil. Kök neden ölçüldü: doğrulayıcının sahte sunucusu `/masak/esikler` için `{}` döner → `KimlikEsigi 0` = "bilinmiyor" → her tutarda MASAK gereksinimi sorulur; test `Gir` sonrasında beklemeden `#hrd-kaydet`'e tıklar, gereksinim yanıtı gelmeden düğme o an `disabled` (`MasakYukleniyorSebep`) olabilir → tıklama etkisiz. Doğrulayıcı testi değiştirilmedi; sahibine not (açık kalanlar).
- Mutasyon (`r18/mut.sh`: yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/r18/<kod>-Hurda.razor`, `perl -0777` TEK eşleşme (önce `grep -c` = 1), derle, filtreli test, geri kopyala, `cmp`). **Hiçbir testi kırmayan mutasyon: 0.**

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M131 | `if (hasGram is { } hh && hh != Math.Round(hh, 6))` → `if (false && …)` | `ZOSO_has_7_hane_onaydaki_tutar_sunucunun_kaydettigi_tutardir`, `ZO29y_has_7_hane_ALAN_HATASI_onay_yok_POST_yok` | AYNI |
| M132 | kabul kuralı `else if (aliciKabulHas … != Math.Round(kh, 6))` → `else if (false && …)` | `ZO29y_alici_kabul_7_hane_ALAN_HATASI_POST_yok` | AYNI |
| M133 | `Math.Round(hh, 6)` → `Math.Round(hh, 7)` (sınır gevşetilir) | `ZOSO_has_7_hane_…`, `ZO29y_has_7_hane_…` | AYNI |
| M134 | mesaj `ZO.HurdaHasHaneSiniri` → `Dil.T.Kabuk.SifirdanBuyuk` (yanlış sebep) | `ZO29y_has_7_hane_ALAN_HATASI_onay_yok_POST_yok` | AYNI |

- `bash ./denetim.sh` (son kod) → **DENETİM TEMİZ** (`r18/denetim.log`).

### 000000000000000000.3 Tam süit (paralel şerit kapısı madde 7)
YALITILMIŞ kopya ağaçta koşuldu (`r18/agac`, `rsync` .git/bin/obj/png/pdf hariç, temiz derleme 0 hata; `KUYUMCU_TEST_DB=kuyumcu_test_t7`). TRX `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-r18.trx`, günlük `r18/tam.log`: **7169 test, 4 kırmızı**:
- `YayinVarlikDumanTestleri.MenuTanimi_StatikBaglantilarinKarsiligiVar` — "favicon.png → wwwroot dosyası YOK": kopya ağaçta png'ler bilerek hariç tutuldu (disk %98). Gerçek ağaçta yeniden koşuldu → **yeşil** (`r18/tekrar.log`).
- `SATIS3EkranTestleri.Arama_harf_harf_yazilinca_DEBOUNCE…` ve `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter_DOGRUDAN…` → **SATIS-3**; gerçek ağaçta yeniden koşuldu → **yeşil** (kararsız, zamanlayıcı/debounce).
- `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` ("Expected 15, Actual 16", `OrtakGorunumSozlugu` alan sayısı) → **ORTAK-1**; devir listesinde ve 13./14. turda aynı.
- Bu paketin testleri (ZOISLEMA* + Dogrulayici.ZOISLEMA*) tam süitte **244/244 yeşil**. **FARK (bu paketin yeni kırmızısı) = 0.**

### 000000000000000000.4 Canlı doğrulama — HTTP + SQL + EKRAN
Ortam: API 5170 (gerçek ağaç, `--no-build`); kiracı `PAKETZO-ISLEM-A-TEST` 01M3PC3BXQ43PJ1BWQTE1E1DNW; sistem anahtarı rastgele, chmod 600, yalnız süreç ortamında; parolalar chmod 600 fikstürde; yanıtlar okundu (`r18/kur.out`, `kur2.out`, `canli.out`, `sen/senaryo.log`, `sql1.out`, `sql2.out`).

| Adım | Beklenen | Gerçek |
|---|---|---|
| Kurulum (SAHİP + şubeli MÜDÜR, kasa, kur, kasaya 50.000 ₺ giriş, 3 × hurda alışı K22 10 g) | 2xx | davet 201 · kayıt 201 · kullanıcı 201 · kasa 200 · kasa giriş 200 · hurda alışı 200 ×3 (ilk deneme kasa boş olduğu için 400 ×3 "bakiyesi yetersiz", kasa girişinden sonra 200) · bakiye 27,48 has g |
| `POST /islemler/hurda-satis` has 1.0000005, tahsil = ham çarpım 6.565,55, cari yok | 400 (sunucu Yuvarla6 → 6.565,56) | **400** "Tahsil edilmeyen 0,01 TL için ramatçı carisi zorunludur…" — kök nedenin sunucu tarafı kanıtı |
| aynı, has 1.000001, tahsil 6.565,56 | 200 | **200** toplamTl 6565.56 · kalanTl 0 · SQL `tl_tutari 6565.56`, kasa 6565.56 |
| **EKRAN F** (MÜDÜR, BASİT aydınlık 1440): has `1,0000005` → Kaydet | alan hatası, onay yok | `#alan-hata-hrd-has` "Has gram en çok 6 ondalık hane alır (ör. 1,000001). Fazla haneyi silin." · kutu `1,0000005`, `aria-invalid=true` · onay **açılmadı** |
| **EKRAN H**: has 2, "Daha fazla" → alıcı kabul `1,9999995` → Kaydet | alan hatası, onay yok | `#alan-hata-hrd-kabul-has` aynı cümle · onay **açılmadı** |
| **EKRAN G**: has `1,000001`, ramatçı ve tahsil boş → Kaydet → Kaydet (onay) | onay = bant = SQL | onay "… Tutar: **6.565,78 ₺** · Tahsil edilen: 6.565,78 ₺ (Nakit)…" · bant "Hurda satışı kaydedildi: Altın 1,000001 g · **6.565,78 ₺**" · onay hatası yok · SQL 01M3PC6PDJB92NK51X3NR8FJEG `has_grami -1.000001`, `tl_tutari 6565.78`, `nakit_tutar 6565.78`, `cari_id NULL`; `kasa_hareketleri.tutar` **6565.78** |

### 000000000000000000.5 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum MÜDÜR, taze profil)
- `r18/sen/sahne.mjs` + `kos.sh`: `/hurda` açılışı ve hata durumu (`1,0000005` + Kaydet) × {1440,1280,960,375} × {BASİT,PRO} × {aydınlık,karanlık} = **32 ölçüm** (`sen/sonuc.jsonl`). PRO aydınlık 960'ta mod düğmesine ilk koşumda tıklanamadı (mod=BASIT ölçüldü); o kombinasyon CDP 9809'da yeniden koşuldu → mod=PRO, sonuç aynı.

| Ölçü | Sonuç |
|---|---|
| yatay, K1, K3_tasan, K3_kirik, K5_TRY, K7, K8, K9_binen, K9b, K10_elipsis, K10b, K11, K12, K13 | **32/32 sıfır** |
| Açılış 1440 (K4_boyKat) | BASİT/PRO × aydınlık/karanlık → **4/4 = 1 ekran**; hata durumunda da 1 |
| ham "şube bağlamı" / "şube atanmamış" | 0 / 0 |

- `dugme-olc` ve `tutarlilik.mjs` bu turda YENİDEN KOŞULMADI: değişiklik açılışta yeni öğe ya da düğme eklemiyor, yalnız hata durumunda alan altına mevcut `AlanHatasi` satırı çıkıyor. Son ölçümleri § 00000000000000000.5'te (ortasız 0, silik 0, kırpık 0, mod farkı 0).
- Görüntüler (Read ile baktım), kopyaları `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/ZO-ISLEM-A-r18-hurda-*.png` (5 dosya):
  - `F1-has-7-hane`: "Bizim kaydımız" kırmızı, kutuda 1,0000005, altında 6 hane cümlesi; Kaydet birincil, onay yok.
  - `H1-kabul-7-hane`: alıcı kabul kutusu kırmızı, aynı cümle.
  - `G1-onay`: onay penceresi, has 1,000001 g, Tutar = Tahsil edilen = 6.565,78 ₺.
  - `G2-kaydedildi`: bant 6.565,78 ₺.
  - `hane-hata-PRO-karanlik-375`: 375 px'te hata cümlesi iki satıra sarılıyor, taşma yok.
- Gözlem (gerekçeli): hata durumunda, Kaydet'ten önce canlı "Tutar" ham hasla hesaplanır (F'de 6.565,77 ₺; sunucu 6,5 hane için 6.565,78 derdi). Kayıt bu değerle ASLA gitmez: Kaydet onay açmaz, kutu kırmızıdır. Kutu 6 haneye indirildiğinde gösterilen, onaylanan ve kaydedilen tutar aynıdır (G).

### 000000000000000000.6 Sapmalar ve gerekçeleri
- İstemcide sessiz yuvarlama yerine alan sınırı seçildi (§ .1 gerekçesi). Kural yeni bir mevzuat hükmü değil, sunucunun mevcut hassasiyetinin (`Yuvarla6`) ekrandaki karşılığıdır; sabit sayı "6" sunucudaki `Yuvarla6` ile aynıdır.
- Doğrulayıcı testi DEĞİŞTİRİLMEDİ (kararsızlık notu § .2).
- Mevzuat: yeni hüküm yok; MASAK/tevsik/gider pusulası kapıları değişmedi.

### 000000000000000000.7 DoD sözleşmesi (bu tur)
Yeni varlık, uç, düğme YOK. Hurda satışı finansal kayıttır: değişmez; kilit gerekçesi ve düzeltme yolu (tahlil farkını düzelt, müşteri düzeltme fişi) değişmedi. Yeni metin `HurdaHasHaneSiniri` `Sozlukler.ZoIslem.cs` içinde TR+EN; konumsal record, eksik anahtar derleme hatası verir. CSS değişmedi.

### 000000000000000000.8 Temizlik
- API 5170 ve vekil 5171 durduruldu (`lsof` dinleyen 0); Chrome profilleri kapandı (`pgrep` 0).
- Kiracı 01M3PC3BXQ43PJ1BWQTE1E1DNW, `tenant_id` sütunu olan 70 tablodan ve `tenants`'tan `r18/sil.sql` ile silindi: önce 348 satır, sonra **0** (`r18/say.sql`); `PAKETZO-ISLEM%` ve hakem kiracısı 01M3JG487JCP11FMBAFRMR4X4P sayısı **0**.
- `tenants`: başlangıç **15**, son **14**. Bu paketin açtığı tek kiracı silindi (+1 −1); eksilen 1 başka şeridin kendi kiracısını silmesinden geliyor (kalan paket kiracıları: PAKETQ10-4, PAKETT-S2, PAKET-ORTAK-1, PAKETZO-BELGE). QA-TEST kiracılarına dokunulmadı.
- Disk: hook `rm`'yi engellediği için `r18/agac` (yalıtılmış kopya ağaç, ~884 MB) ve `r18/sen/profil-*` kurucu/orkestratör tarafından silinmeli. Proje ağacında .bak/.tmp/.mutorj yok (yedekler `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/r18/`).

### 000000000000000000.9 Değişen dosyalar (bu tur) · açık kalanlar
- Değişen dosyalar:
  - `dotnet/Kuyumcu.Web/Pages/Hurda.razor` (`Kaydet()`: has ve alıcı kabul için 6 hane kuralı, 2 blok)
  - `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoIslem.cs` (+1 anahtar `HurdaHasHaneSiniri`, TR+EN)
  - `dotnet/Kuyumcu.Tests/ZOISLEMAHurdaTahsilTestleri.cs` (+3 test; sahte sunucu Yuvarla6)
- Açık kalanlar (sahibiyle): ORTAK-1 `O8_basit_metinler_jargon…` (tam süit) · SATIS-3 `SATIS3EkranTestleri` Arama DEBOUNCE / Barkod dizisi (kararsız, yeniden koşumda yeşil) · `EtiketEkranTestleri` xUnit1031 uyarısı (ORTAK-1) · doğrulayıcı testi `ZOSO_has_7_hane…` MASAK gereksinim yanıtını beklemeden tıklıyor (kararsızlık, § .2; sahibi doğrulayıcı) · 14. turun ORTA kararı (kur eşitlemesinde sabit eşik yok, fark onayda tutar olarak yazılır) kurucu/mimar kararına açık · önceki turların açık kalanları değişmedi (banka hareket yanıtında `eslesmemisKalan` → Taşkın/Bertan; /hurda menü grubu → kurucu/mimar; KenarCubugu grubu belgeyi uzatır → KATMAN; `KayitSecici` etiket eki aralığı ortak bileşende → ORTAK-1/OLCEK-1).

---

## 00000000000000000. 14. DOĞRULAMA — doğrulayıcı KRİTİK (belirsiz "1.500" tahsil TAMAMI sayılıyordu) · ORTA (kur eşitlemesinin tutar sınırı yok) · DÜŞÜK ×2 — 29.09.2026 (t7 · API 5170 · vekil 5171 · CDP 9803–9810 · `KUYUMCU_TEST_DB=kuyumcu_test_t7`)
Durum beyanı: **BITTI** (paralel şerit kapısının 7 maddesi aşağıda). Çalışma dizini `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r17/`. Migration YOK. Yeni uç, varlık, DOM kimliği YOK. POST gövdesinin biçimi, yetki ve yasal kapılar (MASAK, tevsik, gider pusulası) DEĞİŞMEDİ. Dokunulan sayfa yalnız `Hurda.razor` (paketin kendi sayfası; gerekçe: doğrulayıcı KRİTİK). Ortak bileşenlere (SayisalAlan, GecersizAlanKapisi, KayitSecici) DOKUNULMADI; SATIS-3'ün 29.09 t17 ortak kapısı (`Bilesenler/Form/GecersizAlanKapisi`) yalnız KULLANILDI.

### 00000000000000000.1 Bulgu → kök neden → düzeltme
| Bulgu | Kök neden (kodda okundu) | Düzeltme |
|---|---|---|
| [KRİTİK] "Tahsil edilen"e `1.500` → alan kırmızı ama onay açılıyor, TAMAMI kaydediliyor | `SayisalAlan` belirsiz metinde yalnız iç hata (`icHata`) yazıyor, çağırana bir şey bildirmiyordu; `tahsilEdilen` null kalıyor, `TahsilCoz` null'ı "tamamı" sayıyor; `Kaydet()` `form.Temizle()` ile başlayıp formda hata bırakmıyordu → `form.Gecerli` true | Hurda formu (`#hrd-form-alan`) ortak `GecersizAlanKapisi` ile `CascadingValue` içine alındı: formdaki her Sayi/Para/Gram kutusu anlaşılamayan metinde kendini kapıya yazar, çağırana null bildirir, kur yansıması kullanıcının metnini EZMEZ. `Kaydet()` başta `SayiAlanlari` (hrd-has, hrd-kabul-has, hrd-fiyat, hrd-tahsil) için kapıdaki kaydı forma HATA olarak işler → onay açılmaz, POST gitmez; kutunun kendi mesajı ("Ondalık için virgül kullanın: 1,5 ya da 1500") altında kalır. "Tamamı" artık yalnız alan GERÇEKTEN boşken. Ayrıca kutu anlaşılamayan metin taşırken "+ Detay" bölümü kapatılamaz (`DetayZorunluAcik`), kapalı bölüm gizli değer taşımaz. |
| [ORTA] Kur eşitlemesinin tutar üst sınırı yok (6.800 yazılır, kur 7.500'e çıkar, 7.500 kaydedilir) | `TahsilCoz` görülmüş her Tutar'ı eşitliyor; onay yalnız "eşitlendi" diyordu | Onay özeti eşitlenen FARKI tutar olarak yazar: "Yazdığınız 6.800 ₺, kaydedilecek 7.500 ₺ (fark +700 ₺). Müşteriden alınan para farklıysa Vazgeç deyip tutarı düzeltin." (`Sozlukler.ZoIslem.cs` `HurdaTahsilKurFarki` TR+EN, `Hurda.razor` `KurFarkiNotu`). Sabit eşik KONMADI (gerekçe § .6). |
| [DÜŞÜK] PRO aydınlık 1440 açılış 1,1 ekran | Doğrulayıcının `tur2/foto/hurda__PRO_aydinlik_1440.png` görüntüsü 1440×946: fark İÇERİKTE değil, kenar çubuğunda ("Para ve banka" grubu açık, menü belgeyi uzatıyor). Aynı ölçümde PRO karanlık (grup kapalı) = 1,0 | Kod değişikliği yok (KenarCubugu = KATMAN şeridi). Bugünkü ağaçta yeniden ölçüldü: 1440 × 3 rota × 2 mod × 2 tema = **12/12 = 1 ekran**; ayrıca `/hurda` PRO aydınlık 1440'ta grup aç/kapa ile belge 900 px, içerik alt kenarı 815 px (`sen/k4.mjs`). |
| [DÜŞÜK] "Ramatçı / rafineri carisi(isteğe bağlı)" bitişik | `KayitSecici` etiketi metni ve eki arasında boşluk üretmiyor (Razor boşluk kırpar); `AlanKabugu` ise `.alan-etiket { gap }` kullanıyor | Ortak bileşene dokunmadan `css/zo-islem.css`: `#hrd-cari-sarmal > label > .metin-soluk, #pos-eslestir-hareket-sarmal > label > .metin-soluk { margin-left: var(--bosluk-2) }`. Canlıda ölçüldü: `marginLeft 8px`, metin ile ek arası **8 px** (D1 görüntüsü). |

### 00000000000000000.2 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra, `r17/build5.log`) → **0 Uyarı, 0 Hata**. (Ara derlemede 1 uyarı görüldü: `EtiketEkranTestleri.cs(145) xUnit1031` → bu paketin değil, UrunEtiket sahibi ORTAK-1.)
- Yeni testler (`Kuyumcu.Tests/ZOISLEMAHurdaTahsilTestleri.cs`, +5):
  - `ZO29w_belirsiz_1_500_tahsil_alan_hatasi_KALIR_onay_yok_POST_yok_bolum_kapanmaz` (kur yenilense de metin ezilmez, yine engel)
  - `ZO29w_belirsiz_duzeltilince_kayit_acilir_ve_YAZILAN_tahsil_gider`
  - `ZO29w_belirsiz_birim_fiyat_1_500_eski_kur_fiyati_GITMEZ_onay_yok`
  - `ZO29x_kur_esitlemesi_onayda_YAZILAN_KAYDEDILECEK_ve_FARK_tutar_olarak_yazar`
  - `ZO29x_kur_dususunde_fark_EKSI_isaretle_yazilir`
- Doğrulayıcı testi `Dogrulayici.ZOISLEMASinirHurdaTahsilKurTestleri.ZOSK_belirsiz_1_500_tahsil_reddedilir_POST_yok` (DEĞİŞTİRİLMEDİ): KIRMIZI → **YEŞİL**. `ZOSK_buyuk_kur_sicramasi_…` (gözlem testi, değiştirilmedi) yeşil kalır.
- Güncellenen kendi testim: `ZOISLEMAEkranTestleri.ZO_hurda_detay_ekle_ile_alanlar_GORUNUR_ve_POSTa_girer` "6.800" yazıp 6800 bekliyordu. "6.800" P-78'e göre BELİRSİZDİR; eski test tam da kapatılan hata yüzünden yeşildi (belirsiz → null → tamamı = 1 × 6.800). Artık "6800" yazıyor; niyet (detay alanları POST'a girer) aynı.
- Filtreli koşumlar: `ZOISLEMAHurdaTahsil|ZOISLEMASinirHurdaTahsilKur` **21/21** (`t1.log`); geniş filtre (`ZOISLEMA|Hurda|Q102|P20|SADE1|Pos|Banka|ZOIslem|SayisalAlan|Gecersiz`) 843 testte 2 kırmızı (`t2.log`): kendi `ZO_hurda_detay_ekle_…` (yukarıda düzeltildi) ve `PosBarkodTestleri.Fiyat_SUNUCU_teklifinden_gelir_kur_kaydiyla_istenir` (SATIS-3, devir listesinde kararsız); ikisi yeniden koşuldu → `ZOISLEMAEkranTestleri|PosBarkodTestleri` **83/83** (`t3.log`).
- Mutasyon (`r17/mut.sh`: yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/r17/<kod>-Hurda.razor`, `perl -0777` TEK eşleşme (önce sayıldı = 1), derle, filtreli test, geri kopyala, `cmp`). **Hiçbir testi kırmayan mutasyon: 0.**

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M126 | `if (gecersizAlanlar.OnekleVar(id))` → `if (false && …)` | `ZOSK_belirsiz_1_500_tahsil_reddedilir_POST_yok`, `ZO29w_belirsiz_1_500_…`, `ZO29w_belirsiz_duzeltilince_…` | AYNI |
| M127 | `<CascadingValue Value="gecersizAlanlar" …>` → `Name="yok"` (kapı basamaklanmaz) | `ZOSK_belirsiz_1_500_…`, `ZO29w_belirsiz_birim_fiyat_1_500_…`, `ZO29w_belirsiz_duzeltilince_…`, `ZO29w_belirsiz_1_500_…` | AYNI |
| M128 | `DetayZorunluAcik` içindeki `|| DetayAlanlari.Any(gecersizAlanlar.OnekleVar)` → `|| false` | `ZO29w_belirsiz_1_500_…_bolum_kapanmaz` | AYNI |
| M129 | onay notundan `KurFarkiNotu(tutar)` çıkarıldı | `ZO29x_kur_esitlemesi_…`, `ZO29x_kur_dususunde_…` | AYNI |
| M130 | fark işareti `(fark > 0m ? "+" : "")` → `""` | `ZO29x_kur_esitlemesi_…` | AYNI |

- `bash ./denetim.sh` (son kod) → **DENETİM TEMİZ** (`r17/denetim.log`).

### 00000000000000000.3 Tam süit (paralel şerit kapısı madde 7)
- İLK koşum gerçek ağaçta (`r17/tam.log`, 7142 test, 19 kırmızı) GEÇERSİZ SAYILDI: koşum sırasında SATIS-3 t17 şeridi `SayisalAlan.cs` ve `Islemler.razor` üzerinde mutasyon koşuyordu (yedekler `mut-yedek/SATIS-3-t17/M-S3-T17-1…13.orj`, `SayisalAlan.cs` 12:37:29'da değişti, test bin'i 12:36–12:37'de başka derlemeyle yenilendi). Kırmızıların 17'si SATIS-3/S26 T17 "1.500" testleri, 2'si benim `ZO29w/ZO29x` testlerim (ortak kutu mutasyonluyken).
- Bu yüzden tam süit YALITILMIŞ kopya ağaçta koşuldu: SATIS-3'ün tüm `.orj` yedeklerinin kopyadaki dosyalarla birebir aynı olduğu `cmp` ile doğrulandıktan sonra `rsync` (`r17/agac`), temiz derleme (0 hata), `KUYUMCU_TEST_DB=kuyumcu_test_t7`. TRX `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-r17b.trx`, günlük `r17/tam2.log`: 7142 test, 117 kırmızı; bunların 114'ü kopyada depo kökü dosyalarının (deploy/, docs/, dokumanlar/) olmamasından (Yayin*, PD3, OlcumKapisi, Kilavuz …). Kök dosyalar eklenip bu 16 sınıf yeniden koşuldu (`r17/tam2-tekrar.log`, 247 test): **3 kırmızı**:
  - `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` → **ORTAK-1** (devir listesinde ve 13. turda aynı)
  - `S26DigerIslemlerTestleri.T17_Siparis_kapora_tus_tus_1_500_Cevir_PASIF_onay_ACILMAZ_POST_YOK` → **SATIS-3** (t17 şeridinin bugün yazdığı test; FisDigerIslemler/Sipariş, bu paketin dosyası değil)
  - `MigrationHazirlikTestleri.EksikMigrationliSemadaBekleyenListesiDolar` → paylaşılan `kuyumcu_t1_eski` veritabanı yarışı (ilk koşumda YEŞİL; kararsız, sahibi orkestratör/Batuhan)
- Bu paketin testleri (ZOISLEMA* + Dogrulayici.ZOISLEMA*, 232 test) tam süitte **232/232 yeşil**. **FARK (bu paketin yeni kırmızısı) = 0.**

### 00000000000000000.4 Canlı doğrulama — HTTP + SQL + EKRAN
Ortam: API 5170 (önce gerçek ağaçtan, matris için yalıtılmış kopyadan `--no-build`); kiracı `PAKETZO-ISLEM-A-TEST` 01M3P856K1BB56TGRSJDRZ749K; sistem anahtarı rastgele, chmod 600, yalnız süreç ortamında; parolalar chmod 600 fikstürde; yanıtlar okundu (`r17/kur.out`, `kur2.out`, `canli.out`, `sen/senaryo2.out`, `sql0.out`, `sql1.out`).

| Adım | Beklenen | Gerçek |
|---|---|---|
| Kurulum (SAHİP; MÜDÜR şubeli/şubesiz, TEZGÂHTAR şubeli/şubesiz; kasa, cari, kur, banka, POS, 2 × KART işçilik, 2 × POS yatma, 5 × hurda alışı K22 10 g) | 2xx | davet 201 · kayıt 201 · kullanıcı 201 ×4 · diğerleri 200 · hurda bakiyesi 45,8 has g (ilk betik POS cihaz yanıtının `cihaz.id` sarmalında düştü; kalanı `kur2.py` ile tamamlandı) |
| `POST /islemler/hurda-satis` tahsil 400 < toplam 500, cari yok | 400 | **400** "Tahsil edilmeyen 100 TL için ramatçı carisi zorunludur…" |
| aynı, tahsil 600 > 500 | 400 | **400** "Ödenen tutar işlem tutarını aşamaz. İşlem: 500,0, ödenen: 600" |
| dondurulmuş fiyat 6.559,21 (güncel 6.559,28), kabul 1,23 | 200, toplam = 1,23 × gönderilen | **200** toplamTl 8067.83 · SQL `tl_tutari 8067.83`, kasa 8067.83 |
| **EKRAN D (MÜDÜR, BASİT aydınlık 1440):** has 1; Tahsil edilen = `1.500` | detay kapatılamaz; Kaydet → alan hatası kalır, onay yok, POST yok | `#hrd-detay-gizle` YOK (Kaydet'ten önce de) · Kaydet → `#alan-hata-hrd-tahsil` "Ondalık için virgül kullanın: 1,5 ya da 1500" · kutu `1.500`, `aria-invalid=true` · onay penceresi **açılmadı** |
| D, kur oynadı (6.558,65 → 6.564,16), yeniden Kaydet | metin ezilmez, yine engel | kutu hâlâ `1.500` · onay **açılmadı** · hata aynı |
| SQL (D sonrası) | yeni HURDA_SATIS yok | yalnız C satırı (`sql0.out` = `sql1.out`'un ilk satırı) |
| **EKRAN E:** tahsil = ekrandaki Tutar 6.564,16; kur oynadı → 6.565,07; Kaydet | onayda yazılan / kaydedilecek / işaretli fark | "… Kur değiştiği için tahsil edilen tutar güncel tutara eşitlendi. **Yazdığınız 6.564,16 ₺, kaydedilecek 6.565,07 ₺ (fark +0,91 ₺).** Müşteriden alınan para farklıysa Vazgeç deyip tutarı düzeltin." |
| E onaylandı | bant = onay = SQL | bant "Hurda satışı kaydedildi: Altın 1,000 g · **6.565,07 ₺**" · SQL `hareketler` 01M3P8C8ZY048PAAEHZX9AWP1X `tl_tutari 6565.07`, `nakit_tutar 6565.07`, NAKIT, cari NULL · `kasa_hareketleri.tutar` **6565.07** |

### 00000000000000000.5 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum MÜDÜR, taze profil)
- `r17/sen/sahne.mjs` + `kos.sh`: {1440,1280,960,375} × {BASİT,PRO} × {aydınlık,karanlık} × 3 rota (/pos/tahsilatlar, /hurda, /banka) = **48 ölçüm** (`sen/sonuc.jsonl`, `sen/ozet.txt`). Mod uyumsuzluğu 0.

| Ölçü | Sonuç |
|---|---|
| yatay, K1, K3_tasan, K3_kirik, K5_TRY, K7, K8, K9_binen, K9b, K10_elipsis, K10b, K11, K12, K13 | **48/48 sıfır** |
| Açılış 1440 (K4_boyKat) | 3 rota × 2 mod × 2 tema → **12/12 = 1 ekran** (PRO aydınlık /hurda dahil) |
| ham "şube bağlamı" / "şube atanmamış" metni | 0 / 0 |
| `dugme-olc` (CDP 9809, 3 rota × 2 mod × 2 tema) | **ortasız 0 · silik 0 · kırpık 0** |
| `tutarlilik.mjs` 1440 (CDP 9810) | **mod farkı 0**, düğme sınıf grubu 6 |

- Görüntüler (Read ile baktım), kopyaları `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/ZO-ISLEM-A-r17-hurda-*.png` (6 dosya):
  - `D1-belirsiz-1500`: "Tahsil edilen (TL)" kırmızı, kutuda `1.500`, altında virgül cümlesi; ramatçı etiketi "(kayıtlı müşteri)  (isteğe bağlı)" aralıklı.
  - `D2-kur-sonrasi-engel`: kur sonrası kutu ve hata aynı, onay yok.
  - `E1-onay-fark`: onayda yazılan/kaydedilecek/fark cümlesi.
  - `E2-kaydedildi`: bant 6.565,07 ₺.
  - `PRO-aydinlik-1440`, `PRO-karanlik-375`: açılış.

### 00000000000000000.6 Sapmalar ve gerekçeleri
- ORTA için sabit göreli eşik KONMADI. Seçenek (b) uygulandı: fark tutar olarak onayda. Gerekçe: keyfi bir yüzde ya gerçek kısmi tahsili "tamamı" sayar ya da kuruş oynamasında yeniden ramatçı istetir (13. turun kapattığı hata). İstemcide yapılandırmadan okunan eşik altyapısı da yok. Eşitleme yalnız ekranda GÖRÜLMÜŞ bir Tutar yazıldığında olur, elle fiyatta kapalıdır ve artık kullanıcı farkı tutar olarak görüp Vazgeç diyebilir. Doğrulayıcının gözlem testi (`ZOSK_buyuk_kur_sicramasi_…`) bu davranışı sabitliyor ve yeşil.
- DÜŞÜK etiket aralığı ortak `KayitSecici`'de değil paketin kendi CSS'inde çözüldü: ortak bileşen başka şeritlerin ekranlarında da kullanılıyor ve dosya alanım dışında. Ortak düzeltme önerisi aşağıda.
- Mevzuat: yeni hüküm yok; tevsik/MASAK/gider pusulası kapıları değişmedi.

### 00000000000000000.7 DoD sözleşmesi (bu tur)
Yeni varlık, uç, düğme YOK. Hurda satışı finansal kayıttır: değişmez; kilit gerekçesi ve düzeltme yolu değişmedi. Yeni metin (`HurdaTahsilKurFarki`) `Sozlukler.ZoIslem.cs` içinde TR+EN; konumsal record, eksik anahtar derleme hatası verir. Yeni CSS yalnız belirteç (`var(--bosluk-2)`), sabit renk yok.

### 00000000000000000.8 Temizlik
- API 5170 ve vekil 5171 durduruldu (dinleyen süreç 0); Chrome profilleri kapandı.
- Kiracı 01M3P856K1BB56TGRSJDRZ749K, `tenant_id` sütunu olan 70 tablodan ve `tenants`'tan `r17/sil.sql` ile silindi: önce 450 satır, sonra **0** (`r17/say.sql`); `PAKETZO-ISLEM%` kiracı sayısı **0**. Hakem kiracısı 01M3JG487JCP11FMBAFRMR4X4P yok (0).
- `tenants`: başlangıç **15**, son **16**; fark bu paketin değil: aralıkta `PAKETZO-BELGE-TEST` (09:49) açıldı. QA-TEST kiracılarına dokunulmadı.
- Disk: hook `rm`'yi engellediği için `r17/agac` (yalıtılmış kopya ağaç, ~850 MB), `r17/sen/profil-*`, `r17/arac/profil-*` ve r16 kalıntıları kurucu/orkestratör tarafından silinmeli. Proje ağacında .bak/.tmp/.mutorj yok (yedekler `/private/tmp/kuyumcu-serit/` altında).

### 00000000000000000.9 Değişen dosyalar (bu tur) · açık kalanlar
- Değişen dosyalar:
  - `dotnet/Kuyumcu.Web/Pages/Hurda.razor` (`CascadingValue gecersizAlanlar`, `SayiAlanlari`, `Kaydet` kapı kontrolü, `DetayZorunluAcik`, `KurFarkiNotu`)
  - `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoIslem.cs` (+1 anahtar TR+EN)
  - `dotnet/Kuyumcu.Web/wwwroot/css/zo-islem.css` (+1 kural)
  - `dotnet/Kuyumcu.Tests/ZOISLEMAHurdaTahsilTestleri.cs` (+5 test)
  - `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (1 test girdisi "6.800" → "6800", gerekçe § .2)
- Açık kalanlar (sahibiyle): ORTAK-1 O8 (tam süit) · SATIS-3 `S26…T17_Siparis_kapora_…` (t17 sürüyor) ve PosBarkod kararsız · `MigrationHazirlik…EksikMigrationli…` paylaşılan DB yarışı (orkestratör/Batuhan) · `EtiketEkranTestleri` xUnit1031 uyarısı (ORTAK-1) · `KayitSecici` etiket eki aralığı ORTAK bileşende de düzeltilmeli (sahibi ORTAK-1/OLCEK-1; bu paket yalnız kendi sayfalarında çözdü) · kenar çubuğu grubu açıkken belgeyi uzatma (doğrulayıcının 1,1 ölçümü) → KATMAN · önceki turların açık kalanları değişmedi (banka hareket yanıtında `eslesmemisKalan` → Taşkın/Bertan; /hurda menü grubu → kurucu/mimar).

---

## 0000000000000000. 13. DOĞRULAMA — tasarım hakemi 2 KRİTİK (Hurda): ramatçı hatası yalnız alan adıydı · onaylanan tutar ≠ kaydedilen tutar — 29.09.2026 (t7 · API 5170 · vekil 5171 · CDP 9803/9804/9809/9810 · `KUYUMCU_TEST_DB=kuyumcu_test_t7`)
Durum beyanı: **BITTI** (paralel şerit kapısının 7 maddesi aşağıda). Çalışma dizini `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r16/`. Migration YOK. Yeni uç, varlık ya da DOM kimliği YOK. POST gövdesi, yetki ve yasal kapılar (MASAK, tevsik, gider pusulası) DEĞİŞMEDİ. Dokunulan sayfa yalnız `Hurda.razor` (paketin kendi sayfası; gerekçe: tasarım hakemi KRİTİK ×2).

### 0000000000000000.1 Kök neden (ÖLÇÜLDÜ, tahmin değil)
- **KRİTİK 2 (onay 80.744,09 ≠ kayıt 80.743,23):** hakemin günlüğü okundu (`ZO-ISLEM-A-hakem5/api-5170.log`, `a6.json`, `a7.json`). `h11` onay görüntüsü `a6.json` koşusundan geliyor ve bu koşu pencereyi **açık bırakıp bitiyor** (onaylamadan). `h12/h13` kaydı ise `a7.json` ile açılan **ayrı bir tarayıcı oturumunda**, dakikalar sonra, o anki kurla yeni açılan bir pencerenin onayı. Sunucu günlüğünde bu arada yalnız **bir** ekran POST'u var: `HURDA_SATIS: kayıt 12.5 / kabul 12.3 … = 80743.23 TL`. Yani karşılaştırılan iki tutar iki ayrı pencereye ait; aynı pencerede onay ile kayıt arasında fark yok. Gövde `Kaydet()` anında dondurulur (Q10-2 v2; `Gonder` alanlardan yeniden kurmaz) ve sunucu (`SatisModulu.HurdaSatis`) `toplamTl = Yuvarla2(kabul × dto.BirimFiyatTlHasGram)` ile YALNIZ gönderilen fiyatı kullanır. Bu tur aynı zinciri TEK oturumda, pencere açıkken kur değişirken canlıda kanıtladım (aşağıda): **onay = bant = `hareketler.tl_tutari` = `kasa_hareketleri.tutar`**.
- **KRİTİK 1 (ramatçı):** `Hurda.razor` `Kaydet()` eksik tahsilde `form.Ekle("hrd-cari", RamatciMetni)` diyordu, yani mesaj alanın etiketiydi. Kur 10 sn'de bir yenilendiği için tezgâhtarın yazdığı Tutar gönderimde eskiyebiliyordu (hakem: 80.709,77 < 80.714). Canlıda iki yönü de gördüm: kur düşünce yazılan tutar yeni Tutar'ı AŞIYORDU (80.721,83 > 80.687,14 → sunucu 400 "Ödenen tutar işlem tutarını aşamaz"), kur yükselince eksik kalıyordu. Etiket her durumda "(isteğe bağlı)" kalıyordu.

### 0000000000000000.2 Düzeltme
| Bulgu | Düzeltme (`Hurda.razor`, `Sozlukler.ZoIslem.cs` TR+EN) |
|---|---|
| Mesaj yalnız alan adı | Tek kural `TahsilKurallari()`: eksikte `HurdaRamatciGerekli` → "Tahsil edilmeyen {eksik} için ramatçıyı seçin ya da tahsil edilen tutarı işlem tutarına ({tutar}) eşitleyin." Tahsil tutarı aşarsa (önceden sunucuya gidip 400 dönüyordu) `hrd-tahsil` altında `HurdaTahsilAsim` → "Tahsil edilen tutar işlem tutarını ({tutar}) aşamaz; {fazla} fazla yazıldı." Tutarlar `Bicim.Para`. |
| Kuruş farkında ramatçı isteniyordu | `GorulenTutarKaydet()`: kur değişmeden hemen önce ekrandaki (kabul has, Tutar) çifti hatırlanır. `TahsilCoz()`: yazılan tahsil bu has miktarı için EKRANDA GÖRÜNMÜŞ bir Tutar ise kastı "tamamı" sayılır; ramatçı istenmez, tahsil güncel Tutar'a eşitlenir. Onay penceresi bunu açıkça yazar: "Kur değiştiği için tahsil edilen tutar güncel tutara eşitlendi." Kullanıcı eşitlenmiş tutarı görerek onaylar. Elle fiyatta kural kapalıdır. Görülmemiş bir tutar (gerçek kısmi tahsil) eşitlenmez. |
| Etiket "(isteğe bağlı)" kalıyordu | `KayitSecici Zorunlu="@RamatciZorunlu"`: tahsil eksikken etiketten "(isteğe bağlı)" kalkar. |
| (bu turda buldum) Mesaj bayat kalıyordu | İlk canlı koşuda `C` görüntüsünde mesaj "(80.710,02 ₺)" derken form 80.721,83 gösteriyordu (kur tıklamadan sonra değişmişti). Mesaj görünürken kur, tahsil, ramatçı, fiyat ya da gram değişirse (`KurYansit`, `TevsikTazele`) kural yeniden hesaplanır; koşul kalkınca mesaj da kalkar. |
| Onay ≠ kayıt | Kod zaten doğruydu (yukarıdaki kök neden). Yeni test ve mutasyon (M123) ile zincir kilitlendi: onay özeti = POST gövdesi = sunucunun döndürdüğü `toplamTl` = bant. |

### 0000000000000000.3 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra, `build5.log`) → **0 Uyarı, 0 Hata**.
- Yeni test dosyası `Kuyumcu.Tests/ZOISLEMAHurdaTahsilTestleri.cs` (7 test). Sahte sunucu `hurda-satis` yanıtını GÖVDEDEN, sunucu formülüyle hesaplar (sabit yanıt bu zinciri sınayamazdı):
  - `ZO29t_eksik_tahsilde_ramatci_mesaji_CUMLE_eksik_ve_islem_tutariyla_etiket_zorunlu_olur`
  - `ZO29t_kur_oynamasi_GORULEN_tutar_yazildiysa_ramatci_istenmez_guncel_tutara_esitlenir_onayda_yazar`
  - `ZO29t_hic_GORULMEMIS_eksik_tutar_esitlenmez_ramatci_istenir`
  - `ZO29t_elle_fiyatta_kur_esitlemesi_KAPALI_eski_tutar_ramatci_ister`
  - `ZO29t_tahsil_tutari_asarsa_alan_alti_CUMLE_POST_yok`
  - `ZO29u_ONAYDAKI_tutar_gonderilen_govde_ve_bant_AYNI_kur_pencere_acikken_degisse_de`
  - `ZO29v_mesaj_gorunurken_kur_degisirse_tutar_BAYAT_kalmaz_ramatci_secilince_kalkar`
- Güncellenen test (bu paketin kendi testi, doğrulayıcı testi DEĞİL): `ZOISLEMAEkranTestleri.ZO_hurda_detay_alani_HATALIYSA_bolum_acik_kalir_ve_mesaj_gorunur`. Eski hâli "tahsil boşaltılsa da ramatçı hatası durur" diyordu, yani bayat mesajı sabitliyordu. Yeni hâli: tahsil hâlâ eksikken hata tutarı güncel olarak durur ve bölüm kapatılamaz (asıl niyet korunuyor). Tahsil boşaltılınca (= tamamı) koşul kalkar, mesaj kalkar, bölüm kapatılabilir.
- Filtreli koşum (`ZOISLEMA|Hurda|Q102|P20|SADE1|Pos|Banka|ZOIslem`) → **705/705** (`t6.log`). Önceki koşumda (`t5.log`) `PosBarkodTestleri.Fiyat_SUNUCU_teklifinden_gelir_kur_kaydiyla_istenir` bir kez kırmızıydı. Bu test SATIS-3'ün ve devir notunda kararsız olarak kayıtlı; kod değişmeden tekrar koşunca yeşildi.
- Mutasyon (`r16/mut.sh`). Yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/r16/<kod>-Hurda.razor` altına alındı, `perl -0777 -pi` ile TEK eşleşme uygulandı (önceden `grep -c` = 1), derleme ve filtreli test koşuldu, yedek geri kopyalandı, `cmp` ile karşılaştırıldı. **Hiçbir testi kırmayan mutasyon: 0.**

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M116 | `gorulenTutarlar.Contains((KabulHas, t))` → `false` | `ZO29t_kur_oynamasi_GORULEN_…` (\*) | AYNI |
| M117 | `!fiyatElle &&` kaldırıldı | `ZO29t_elle_fiyatta_kur_esitlemesi_KAPALI_…` | AYNI |
| M118 | `RamatciZorunlu => false` | `ZO29t_eksik_tahsilde_ramatci_mesaji_CUMLE_…` | AYNI |
| M119 / M119b | `if (tahsil > Tutar)` → `if (false)` (b: tek kurala taşındıktan sonra) | `ZO29t_tahsil_tutari_asarsa_alan_alti_CUMLE_POST_yok` | AYNI / AYNI |
| M120 / M120b | cümle mesajı → `RamatciMetni` (eski davranış) | `ZO29t_eksik_…`, `ZO29t_hic_GORULMEMIS_…`, `ZO29t_elle_fiyatta_…` (+ b: `ZO29v_…`) | AYNI / AYNI |
| M121 | `KurYansit` içindeki `GorulenTutarKaydet();` silindi | `ZO29t_kur_oynamasi_GORULEN_…` | AYNI |
| M122 | onay özetindeki eşitleme notu → `""` | `ZO29t_kur_oynamasi_GORULEN_…` | AYNI |
| M123 | `Gonder` gövdeyi o anki fiyat/Tutar ile yeniden kurar (`govde with {…}`) | `ZO29u_ONAYDAKI_…`, `Q102HurdaOnayEkranTestleri.Onay_acikken_kur_degisirse_POST_ONAYLANAN_…`, `Dogrulayici.Q102EkranSinirTestleri.DQ102v2_onay_acikken_kur_degisirse_POST_onaylanan_fiyati_tasir` | AYNI |
| M124 | `TevsikTazele` içindeki yeniden hesaplama silindi | `ZO29v_mesaj_gorunurken_…` | AYNI |
| M125 | `KurYansit` içindeki yeniden hesaplama silindi | `ZO29v_mesaj_gorunurken_…` | AYNI |

(\*) M116'nın İLK koşusu yeşil çıktı (7 sn; artımlı Razor derlemesi değişikliği almadı). Aynı mutasyon yeniden koşulunca `ZO29t_kur_oynamasi_GORULEN_…` kırmızıya döndü. İki koşunun günlüğü de `r16/mut-M116.log` (son koşu). Sonraki bütün mutasyonlar derlemenin değişikliği aldığını kırmızıyla gösterdi.
- `bash ./denetim.sh` (son kod) → **DENETİM TEMİZ** (`denetim2.log`). Bu turun ilk denetiminde görülen P-39 `TABLO_SARMALAYICISIZ 1` (`IslemDefteri.razor`, ZO-BELGE) o şerit tarafından kapatılmış.

### 0000000000000000.4 Canlı doğrulama — HTTP + SQL + EKRAN
Ortam: API 5170; kiracı `PAKETZO-ISLEM-A-TEST` 01M3P4N4JVNQFRZSEM3PMVN6DK; sistem anahtarı rastgele üretildi, chmod 600, yalnız süreç ortamında; parolalar chmod 600 fikstürde; yanıtlar okundu: `r16/canli.out`, `r16/sen/senaryo.out`, `senaryo2.out`, `sql.out`, `sql2.out`.

| Adım | Beklenen | Gerçek |
|---|---|---|
| Kurulum (SAHİP; MÜDÜR şubeli/şubesiz, TEZGÂHTAR şubeli/şubesiz; kasa, cari, kur, banka, POS, 5 × hurda alışı K22 10 g) | 2xx | davet 201 · kayıt 201 · kullanıcı 201 ×4 · diğerleri 200 · hurda bakiyesi 45,8 has g |
| `POST /islemler/hurda-satis` tahsil 400 < toplam 500, cari yok | 400 | **400** "Tahsil edilmeyen 100 TL için ramatçı carisi zorunludur…" (sunucu kapısı aynı; ekran artık bunu gönderimden ÖNCE cümleyle söylüyor) |
| aynı, tahsil 600 > toplam 500 | 400 | **400** "Ödenen tutar işlem tutarını aşamaz. İşlem: 500,0, ödenen: 600" (ekran artık göndermeden `hrd-tahsil` altında söylüyor) |
| güncel kurdan FARKLI (dondurulmuş) fiyat 6.563,72 (güncel 6.563,79), kabul 1,23 | 200, toplam = 1,23 × gönderilen | **200** toplamTl 8073.38 = 1,23 × 6.563,72 · SQL `tl_tutari 8073.38`, kasa 8073.38 |
| **EKRAN 1 (tek oturum, MÜDÜR, BASİT aydınlık 1440):** 12,5 / kabul 12,3 / Havale; tahsil = ekrandaki Tutar 80.721,83; kur oynadı → 80.687,14 (yazılan tutar ARTIK FAZLA) | ramatçı istenmez, eşitlenir, onay = kayıt | etiket "(isteğe bağlı)" · ramatçı hatası **yok** · onay "Tutar: 80.687,14 ₺ · Tahsil edilen: 80.687,14 ₺ (Havale) · … · Kur değiştiği için tahsil edilen tutar güncel tutara eşitlendi." · pencere açıkken kur 80.662,79'a oynadı, özet **aynı kaldı** · bant "Hurda satışı kaydedildi: Altın 12,500 g · **80.687,14 ₺**" · SQL `hareketler.tl_tutari` **80687.14**, `kasa_hareketleri.tutar` **80687.14** |
| **EKRAN 2 (tek oturum, son kodla):** eksik tahsil (Tutar − 1.000) → Kaydet | cümle mesajı, etiket zorunlu, POST yok | "Tahsil edilmeyen **1.000 ₺** için ramatçıyı seçin ya da tahsil edilen tutarı işlem tutarına (**80.677,91 ₺**) eşitleyin." · etiket "(isteğe bağlı)" YOK · onay penceresi açılmadı |
| aynı mesaj görünürken kur oynadı (80.677,91 → 80.691,32) | mesaj güncel tutarla | "Tahsil edilmeyen **1.013,41 ₺** … (**80.691,32 ₺**)" |
| tahsil = Tutar + 500 → Kaydet | `hrd-tahsil` altında cümle | "Tahsil edilen tutar işlem tutarını (80.691,32 ₺) aşamaz; 500 ₺ fazla yazıldı." |
| tahsil = ekrandaki 80.691,32; kur oynadı → 80.695,87 (yazılan tutar ARTIK EKSİK; hakemin h10 yönü) | ramatçı istenmez; onay = kayıt | ramatçı hatası **yok** · onay "Tutar: 80.695,87 ₺ · Tahsil edilen: 80.695,87 ₺ … eşitlendi." · pencere açıkken kur 80.684,43'e oynadı, özet aynı · bant **80.695,87 ₺** · SQL `tl_tutari` **80695.87**, kasa **80695.87** |

SQL (`sql.out`, `sql2.out`): `HURDA_SATIS` satırları 8073.38 / 80687.14 / 80695.87 (HAVALE, cari NULL), her birinin `kasa_hareketleri.tutar` değeri aynı; `TAHLIL_FARKI` −0,02 / −0,2 has g.

### 0000000000000000.5 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum MÜDÜR, taze profil)
- `r16/sen/sahne.mjs` + `kos.sh`: {1440,1280,960,375} × {BASİT,PRO} × {aydınlık,karanlık} × 3 rota (/pos/tahsilatlar, /hurda, /banka) = **48 ölçüm**. İki kombinasyon yarış yüzünden yeniden koşuldu (BASİT aydınlık 1440: `kuyumcuTema` henüz yüklenmemişti; PRO karanlık 960: mod tıklaması tutmamıştı). Son durumda mod uyumsuzluğu 0.

| Ölçü | Sonuç |
|---|---|
| yatay, K1, K3_tasan, K3_kirik, K5_TRY, K7, K8, K9_binen, K9b, K10_elipsis, K10b, K11, K12, K13 | **48/48 sıfır** |
| Açılış 1440 (K4_boyKat) | /pos/tahsilatlar, /hurda, /banka × 2 mod × 2 tema → **12/12 = 1 ekran** |
| `dugme-olc` (CDP 9809, 3 rota × 2 mod × 2 tema) | **ortasız 0 · silik 0 · kırpık 0** |
| `tutarlilik.mjs` 1440 (CDP 9810) | **mod farkı 0**, düğme sınıf grubu 6 |

- Görüntüler (Read ile baktım). Kopyaları `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/ZO-ISLEM-A-r16-hurda-*.png` altında (8 dosya):
  - `B-ramatci-cumle`: kırmızı cümle, etiket yıldızsız ve "(isteğe bağlı)" yok.
  - `B2-ramatci-guncel`: kur sonrası cümle güncel tutarla.
  - `C-tahsil-asim`: `hrd-tahsil` kırmızı, altında cümle.
  - `A1-kur-oynadi`: ramatçı "(isteğe bağlı)", hata yok.
  - `A2-onay`, `A3-onay-kur-oynadi`: pencere 80.695,87; arkadaki form 80.684,43; eşitleme notu.
  - `A4-kaydedildi`: bant 80.695,87 ₺.
  - `PRO-karanlik-375`.

### 0000000000000000.6 Tam süit (paralel şerit kapısı madde 7)
- Son kod ve son mutasyondan sonra koşuldu. TRX `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-r16b.trx`, günlük `r16/tam2.log`.
- Sonuç: **7101 test, 7100 geçti, 1 başarısız**: `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (beklenen 15, gerçek 16) → **ORTAK-1**. Devir listesinde ve § 000000000000000.5'te aynı test var.
- **FARK (bu paketin yeni kırmızısı) = 0.**

### 0000000000000000.7 DoD sözleşmesi (bu tur)
Yeni varlık, uç ve düğme YOK. Hurda satışı finansal kayıttır: değişmez, kilit gerekçesi ve düzeltme yolu (`Tahlil farkını düzelt`, müşteri düzeltme fişi) değişmedi. Yeni metinler `Sozlukler.ZoIslem.cs` içinde TR+EN; konumsal record olduğu için eksik anahtar derleme hatası verir. Yeni CSS yok.

### 0000000000000000.8 Sapmalar ve gerekçeleri
- Hakem iki çözüm önermişti: "kuruş farkında ramatçı istenmesin" ya da "tutar kilitlensin". Kilitleme (kurun formu dondurması) seçilmedi. Kilitlenirse tezgâhtar eski kurla satar ve kurun yenilendiğini görmez. Bunun yerine YALNIZ "ekranda görülmüş Tutar yazıldı" durumu tamamı sayılır ve onay penceresi eşitlemeyi açıkça yazar. Sabit bir kuruş eşiği konmadı: eşik sayısı keyfi olurdu ve gerçek bir kısmi tahsili yutabilirdi.
- KRİTİK 2 için kod değişikliği gerekmedi (kök neden ölçüm yöntemi). Yine de zincir yeni test ve M123 ile kilitlendi ve canlıda tek oturumda iki kez kanıtlandı.
- Mevzuat: yeni hüküm yok. Tevsik, MASAK ve gider pusulası kapıları değişmedi. `NakitTahsil` artık eşitlenmiş tahsili kullanıyor, yani tevsik önizlemesi gönderilecek değerle hesaplanıyor.

### 0000000000000000.9 Temizlik
- API 5170 ve vekil 5171 durduruldu (dinleyen süreç 0). Chrome profilleri kapatıldı.
- Kiracı 01M3P4N4JVNQFRZSEM3PMVN6DK, `tenant_id` sütunu olan 70 tablodan ve `tenants`'tan `r16/sil.sql` ile silindi. Silmeden önce 473 satırdı, sonra **0** (`r16/say.sql`); `PAKETZO-ISLEM%` kiracı sayısı **0**.
- Hakem kiracısı 01M3JG487JCP11FMBAFRMR4X4P mevcut değil (0).
- `tenants`: başlangıç **16**, son **15**. Bu paketin kiracısı açıldı ve silindi. Eksilen 1 kiracı aynı aralıkta başka bir şeridin kendi test kiracısını silmesinden geliyor. QA-TEST kiracılarına dokunulmadı.
- Disk: hook `rm`'yi engellediği için `r16/sen/profil-*`, `r16/arac/profil-*` ve `r16` çalışma dizinini kurucu ya da orkestratör silmeli. Proje ağacında .bak/.tmp/.mutorj yok.

### 0000000000000000.10 Değişen dosyalar (bu tur) · açık kalanlar
- Değişen dosyalar:
  - `dotnet/Kuyumcu.Web/Pages/Hurda.razor` (`TahsilKurallari`, `TahsilCoz`, `GorulenTutarKaydet`, `RamatciZorunlu`, onay notu, tazeleme)
  - `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoIslem.cs` (+3 anahtar TR+EN)
  - `dotnet/Kuyumcu.Tests/ZOISLEMAHurdaTahsilTestleri.cs` (yeni, 7 test)
  - `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (1 test güncellendi, gerekçe yukarıda)
- Açık kalanlar (sahibiyle): ORTAK-1 O8 (tam süit) · SATIS-3 PosBarkod kararsız · önceki turların açık kalanları değişmedi (§ 000000000000000.9: Üst bant kur 403 metni → KATMAN/ORTAK; şubesiz TEZGÂHTAR BASİT/PRO tercihi → KATMAN; banka hareket yanıtında `eslesmemisKalan` → Taşkın/Bertan; /hurda menü grubu → kurucu/mimar).

---

## 000000000000000. 12. DOĞRULAMA — tasarım hakemi 2 YÜKSEK: şubesiz oturumda POS ham sunucu metni (3 kez + "ödeme yok") · POS banka hareketi seçenekleri silik/ters hiyerarşi — 29.09.2026 (t7 · API 5170 · vekil 5171 · CDP 9803–9810 · `KUYUMCU_TEST_DB=kuyumcu_test_t7`)
Durum beyanı: **BITTI** (paralel şerit kapısının 7 maddesi aşağıda). Çalışma dizini `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r15/`. Migration YOK. Yeni uç, yeni varlık, yeni DOM kimliği YOK (yalnız sınıf `zo-pos-eslestir-izgara`). Yetki, yasal kapılar ve POST gövdeleri DEĞİŞMEDİ. Devir notu 3'teki eşleştirme çıkmazı ve harf duyarlılığı § 000000000'da kapanmıştı; bu turda o mantığa dokunulmadı.

### 000000000000000.1 Bulgu → düzeltme
| Bulgu | Kök neden (ölçüldü) | Düzeltme | Kanıt |
|---|---|---|---|
| [YÜKSEK] Şubesi olmayan TEZGÂHTAR POS'ta ham "Şube bağlamı çözülemedi…" metnini 3 kez görüyordu (sayfa bandı, arama kutusu altı ve üst bant). Veri okunamadığı hâlde takvim "ödeme yok" diyordu. | Küresel şube çözümleyicisi (`KuyumcuDbContext`) 403 dönüyor. Bunu canlıda ölçtüm: `/pos/valor-takvimi`, `/pos/tahsilatlar`, `/pos/yatma-adaylari` ve `/hurda/bakiye` dördü de 403. `PosTahsilatlar.Yukle` `ex.Message`'ı olduğu gibi basıyordu. Hata olunca `takvim` boş liste kalıyor, bu yüzden boş durum çiziliyordu (UX-S28 ihlali). | `PosTahsilatlar.razor`: yeni `veriGeldi` bayrağı eklendi. Veri okunamadıysa takvim, "Kartlı tahsilatlar", eşleştirme kartı ve kilit kutusu hiç çizilmiyor. Şubesiz oturumda 403 gelirse Hurda ile AYNI sade cümle yazılıyor (`SadeSozlugu.HurdaSubeYok`; SAHİP için `ZO.HurdaSubeYokSahip`), yalnız bantta ve TEK kez. Başka her hata sunucu metniyle kalıyor. | bUnit `ZO29q_…` (×4) · M109, M110 · canlı ekran (aşağıda) |
| [YÜKSEK, aynı sınıf, canlıda buldum] Şubeli TEZGÂHTAR `/pos/tahsilatlar` isteğinden 200 alıyor, ama eşleştirme uçları `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` → 403 "gerekli rol". Kart çizildiği için arama kutusu ham rol metniyle düşüyordu. | Menü girdisi tüm rollere açık (`MenuTanimi.cs:127`, `[]`). | Eşleştirme kartı yalnız SAHİP, MÜDÜR ve MUHASEBE rolünde çiziliyor (`EslestirmeYetkisi`). Rol henüz bilinmiyorsa kart gizlenmiyor; yetki kapısı sunucuda duruyor (fail-closed). | bUnit `ZO29s_…` (×3) · M114 · canlı: tezgahtar kart yok, arama hatası 0 |
| [YÜKSEK] POS banka hareketi seçenekleri zeminden ayırt edilmiyordu (dugme-olc zemin 1,00 / kenar 1,00 ×8). Hiyerarşi de tersti: tutar 12 px gri, banka adı 18 px kalındı; başta gereksiz "Banka hareketi" çipi vardı. | Ortak `KayitSecici` sonuçları düz satır olarak çiziyor (app.css `.secici-sonuc { border:0; background:transparent }`). Sunucu sonucun ADINA hesap adını, tutarı ise alt bilgiye koyuyordu. | (1) Sunucu (`PosModulu.YatmaAdaylari`): sonucun adı artık TUTAR (`+26.950 ₺`). Alt bilgi "hesap · tarih · karşı taraf" oldu; kısmen eşleşmiş harekette sonuna `kalan …` ekleniyor. Yanıt şekli değişmedi; sözlükte `PosAdayAlt` → `PosAdayKalan` (TR+EN). (2) `zo-islem.css`, yalnız `#pos-eslestir-hareket-sarmal` kapsamında (ortak bileşene dokunulmadı): her seçenek kenarlı (`--kenarlik`), zeminli (`--yuzey`), 10 px köşeli bir satır-düğme; tutar 20 px/700, alt bilgi 15 px; seçili kutuda ve sonuçlarda tür çipi gizli. (3) 1101 px ve üstünde seçici 2 sütun, "Öneri getir" ile birincil düğme sağda alt alta. Bu olmadan açılış 1440'ta 1,1 ekrandı (1015/900 px, ölçüldü); bu düzenle 1 ekran. | bUnit `ZO29r_…`, `ZO29k_…` (servis + HTTP) · M111, M112, M113, M115 · dugme-olc silik 0 · CDP ölçümü: kenar kontrastı 3,65 (aydınlık) / 3,61 (karanlık), ad 20 px, alt 15 px, çip görünür 0 |

### 000000000000000.2 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra) → **0 Hata**, 1 Uyarı. Uyarı başkasının dosyasında: `Kuyumcu.Tests/EtiketEkranTestleri.cs(145,47) xUnit1031`, bu paketin dosyası değil.
- Yeni ya da güncellenen testler:
  - Yeni: `ZO29q_pos_subesiz_403_sade_cumle_TEK_kez_kartlar_cizilmez` (TEZGAHTAR/MUDUR/SAHIP), `ZO29q_pos_subeli_oturumda_BASKA_hata_sunucu_metniyle_kalir_bos_durum_yalan_soylemez`, `ZO29r_pos_hareket_secenekleri_satir_dugme_TUTAR_birincil_cip_yok`, `ZO29s_pos_eslestirme_karti_yalniz_yetkili_rolde` (TEZGAHTAR/MUHASEBE/MUDUR).
  - Güncellenen: `ZO29k_…` servis ve HTTP testleri (ad = tutar; alt bilgi hesap ile başlar, kalan yalnız kısmi harekette). Sahte API gövdeleri gerçek yanıt biçimine çekildi.
- Doğrulayıcının `ZOS12_yatma_adaylari_kalan_0_01…` testine DOKUNULMADI. İlk tasarım kalanı ada taşıyınca bu test kırmızıya döndü; tasarım, "kalan alt bilgide" sözleşmesine uyacak şekilde düzeltildi ve test yeşile döndü.
- Filtreli koşum (`ZOISLEMA|Pos|Hurda|SADE1|Banka`) → **630/630** (`r15/t5.log`).
- Mutasyon (`r15/mut.sh`). Yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/r15/<kod>-<dosya>` altına alındı, `perl -0777 -pi` tek eşleşmeyle uygulandı (önceden `grep -c` = 1 doğrulandı), derleme ve filtreli test koşuldu, yedek geri kopyalandı, `cmp` ile karşılaştırıldı. Hiçbir testi kırmayan mutasyon: **0**.

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M109 | `else if (veriGeldi)` → `else` | `ZO29q_…` ×3 + `ZO29q_pos_subeli_…` | AYNI |
| M110 | şubesiz 403 → `hata = ex.Message` | `ZO29q_…` (SAHIP, MUDUR, TEZGAHTAR) | AYNI |
| M111 | `if (kalan < s.Tutar)` → `if (true)` (PosModulu) | `ZO29k_yatma_adaylari_yalniz_POS_hesabindaki_ACIK_hareketler_hesap_adi_ve_KALAN_ile` | AYNI |
| M112 | seçenek kenarı `1px solid var(--kenarlik)` → `0` | `ZO29r_…` | AYNI |
| M113 | çip `display: none` → `inline-flex` | `ZO29r_…` | AYNI |
| M114 | `EslestirmeYetkisi`'ne TEZGAHTAR eklendi | `ZO29s_…(TEZGAHTAR)` | AYNI |
| M115 | geniş ekran 2 sütun kuralı silindi | `ZO29r_…` | AYNI |

M113'ün ilk koşusunda derleme, başka şeridin yarım işi yüzünden kırmızıydı: `IslemDefteri.razor(301)` `ZoBelgeMetinleri.YazdirKayit` yok (ZO-BELGE). Ağaç yeniden derlenince M113 tekrar koşuldu ve yine kırmızı → geri alındı → cmp AYNI.
- `bash ./denetim.sh` → **tek ✗: `P-39: TABLO_SARMALAYICISIZ 1`**. Bu sayaç bu paketin değil. Kaynağı `Kuyumcu.Web/Pages/IslemDefteri.razor:306` (ZO-BELGE şeridinin eşzamanlı işi). Aynı betikle hangi dosyadan geldiği ölçüldü. Bu paketin sayaçlarının hepsi ✓.

### 000000000000000.3 Canlı doğrulama — HTTP + SQL + EKRAN
Ortam: API 5170; kiracı `PAKETZO-ISLEM-A-TEST` 01M3NMKBVSMKHN4MEFZR58RH52; sistem anahtarı rastgele, chmod 600, yalnız süreç ortamında; parolalar chmod 600 fikstürde; yanıtlar okundu: `r15/canli.out`, `r15/sql.out`.

| Adım | Beklenen | Gerçek |
|---|---|---|
| Kurulum (SAHİP): davet, kayıt, 4 kullanıcı (MÜDÜR şubeli/şubesiz, TEZGÂHTAR şubeli/şubesiz), kasa, cari, kur, Garanti POS hesabı, POS cihazı, işçilik KART 1.500 + 27.500, POS yatması 26.950 + 1.470 | 2xx | davet 201 · kayıt 201 · kullanıcı 201 ×4 · diğerleri **200** |
| Rol × şube: `/pos/valor-takvimi`, `/pos/tahsilatlar`, `/pos/yatma-adaylari`, `/hurda/bakiye` | şubesiz TEZGÂHTAR 403 ×4; şubeli TEZGÂHTAR aday 403 (rol); diğerleri 200 | SAHİP, MÜDÜR, şubesiz MÜDÜR: **200 ×4** · şubeli TEZGÂHTAR: 200/200/**403** "gerekli rol: SAHIP veya MUDUR veya MUHASEBE"/200 · şubesiz TEZGÂHTAR: **403 ×4** "Şube bağlamı çözülemedi…" |
| `GET /pos/yatma-adaylari?q=&dil=tr` | ad = tutar | **200** `("+1.470 ₺", "Garanti BBVA · POS Tahsilat Hesabı · 29.09.2026 · Garanti POS yatan")`, `("+26.950 ₺", "… · Garanti POS yatan")` |
| `POST /pos/eslestir` 26.950 hareketi ↔ 26.950 tahsilat, **10.000 kısmi** | 200 | **200** "Eşleştirme tamamlandı", eslesenToplam 10000 |
| Aday listesi tekrar (tr / en) | kısmi harekette "kalan 16.950 ₺" alt bilgide | **200** `… · kalan 16.950 ₺` / `… · remaining 16.950 ₺`; ad `+26.950 ₺` |

SQL (`r15/sql.out`):
- `pos_yatma_eslesmeleri`: 1 satır, 10.000, geri alınan 0.
- `banka_hareketleri`: 1.470 BEKLIYOR, 26.950 ESLESTI.
- `pos_tahsilatlari`: 1.470 yatan 0 BEKLIYOR; 26.950 yatan 10.000 KISMI.
- `kullanicilar`: MÜDÜR ve TEZGÂHTAR birer şubeli, birer şubesiz.

Ekrandaki "kalan 16.950 ₺" ve valör takviminin "Yatan 10.000 ₺ · Kalan 18.420 ₺" satırı bu SQL ile birebir tutuyor.

### 000000000000000.4 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, taze profil)
- **MÜDÜR (şubeli)**, `r15/sen/sahne.mjs` + `kos.sh`: 16 kombinasyon ({1440,1280,960,375} × {BASİT,PRO} × {aydınlık,karanlık}) × 3 rota (/pos/tahsilatlar, /hurda, /banka) = **48 ölçüm**. 16/16 rc=0; mod uyumsuzluğu 0.

| Ölçü | Sonuç |
|---|---|
| yatay kaydırma, K1, K3_tasan, K3_kirik, K5_TRY, K7, K8, K9_binen, K9b, K10, K10b, K11, K12, K13 | **48/48 sıfır** |
| Açılış (1440, K4_boyKat): /pos/tahsilatlar, /hurda, /banka × 2 mod × 2 tema | **12/12 = 1** ekran (POS düzeltmeden önce 1,1 idi) |
| POS seçenekleri (16 kombinasyon × 2 seçenek) | kenar kontrastı 3,65 / 3,61 · ad 20 px · alt 15 px · çip görünür **0** |
| `dugme-olc` (kopya, CDP 9809; 3 rota × 2 mod × 2 tema; seçenek düğmeleri listede) | **ortasız 0 · silik 0 · kırpık 0** (hakem ölçümünde silik 8) |
| `tutarlilik.mjs` 1440 (CDP 9810) | **mod farkı 0**, düğme sınıf grubu 6 |

- **Rol oturumları** (/pos/tahsilatlar + /hurda; 1440 BASİT aydınlık ve 375 PRO karanlık):

| Oturum | Ham "şube bağlamı" | Sade cümle | "ödeme yok" | Eşleştirme kartı | "Arama yapılamadı" | K3 / yatay |
|---|---|---|---|---|---|---|
| şubesiz TEZGÂHTAR — POS | **0** | **1** (bantta, tek) | **yok** | **yok** | **0** | 0 / 0 |
| şubesiz TEZGÂHTAR — Hurda | 0 | 1 | — | — | 0 | 0 / 0 |
| şubeli TEZGÂHTAR — POS | 0 | 0 | yok | **yok** (rol) | **0** | 0 / 0 |
| şubesiz MÜDÜR (tek şube) — POS/Hurda | 0 | 0 | yok | var | 0 | 0 / 0 |

- Görüntüler (Read ile baktım), kopyaları `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/ZO-ISLEM-A-r15-*.png` (7 dosya):
  - `mudur__pos-tahsilatlar_BASIT_aydinlik_1440`: iki satır-düğme; büyük "+1.470 ₺", "+26.950 ₺"; sağda Öneri getir + birincil düğme.
  - `…_PRO_karanlik_1440` ve `…_PRO_karanlik_375`: 375'te tek sütun, seçenekler kartlı.
  - `mudur__pos-secili_BASIT_aydinlik_1440`: seçili kutuda tutar büyük, çip yok; elle liste 1.470 / 16.950.
  - `tezgahtar0__pos-tahsilatlar_BASIT_aydinlik_1440`: sayfada yalnız süzgeç ve TEK sade cümle; takvim, "ödeme yok" ve eşleştirme kartı yok.
  - `tezgahtar0__hurda_…` ve `tezgahtar__pos-tahsilatlar_…`.

### 000000000000000.5 Tam süit (paralel şerit kapısı madde 7)
- Koşum son değişiklikten ve son mutasyondan sonra yapıldı. TRX `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-r15.trx`, günlük `r15/tam.log`.
- Sonuç: **7049 test, 7047 geçti, 2 başarısız.**
  - `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` → ORTAK-1. Devir listesinde ve § 00000000000000.5'te aynı test var.
  - `P39TasarimKokleriTestleri.K5_Her_tablo_KENDI_kaydirma_kabindadir` → koleksiyon `["IslemDefteri.razor@19280"]` → ZO-BELGE şeridinin eşzamanlı işi. denetim P-39 ile aynı kaynak; bu paket o dosyaya dokunmadı.
- **FARK (bu paketin yeni kırmızısı) = 0.**

### 000000000000000.6 DoD sözleşmesi (bu tur)
Yeni varlık, yeni uç ve yeni düğme YOK. `/pos/yatma-adaylari` yanıtının şekli aynı; yalnız ad ile alt bilginin içeriği yer değiştirdi (sahibi bu paket, 29.09'da açılmıştı). Eşleştirme kartının rol koşulu sunucudaki `[RolGerekli]` ile TAM eşleşiyor. Düzenleme ve geri alma yolları değişmedi.

### 000000000000000.7 Sapmalar ve gerekçeleri · dokunulan sayfalar
- Hakemin önerisi "tutar birincil, banka adı ikincil" idi. Kalan tutar ikincil satırın SONUNDA kaldı, çünkü doğrulayıcı testi `ZOS12` "kalan … alt bilgide" sözleşmesini sabitliyor ve doğrulayıcı testleri değiştirilmez. Tutar yine birincil.
- Tür çipi CSS ile yalnız bu ekranın sarmalında gizlendi. Ortak `KayitSecici` başka şeritlerin alanı ve başka ekranlarda çip bilgi taşıyor.
- Şubeli TEZGÂHTAR için kart gizleme hakem bulgusunda yoktu. Aynı sınıf hata canlıda ölçüldü (ham "gerekli rol" metni), bu yüzden kapatıldı.
- Dokunulan sayfa: `PosTahsilatlar.razor` (paketin kendi sayfası; gerekçe: tasarım hakemi YÜKSEK ×2). Hurda.razor ve Banka.razor'a bu turda dokunulmadı.
- Mevzuat: yeni hüküm yok.

### 000000000000000.8 Temizlik
- API 5170 ve vekil 5171 durduruldu (dinleyen süreç 0). Chrome profilleri her koşudan sonra kapatıldı.
- Kiracı 01M3NMKBVSMKHN4MEFZR58RH52, `tenant_id` sütunu olan 70 tablodan `r15/sil.sql` ile silindi. Silmeden önce 405 satırdı, yeniden sayımda **0** (`r15/say.sql`); `PAKETZO-ISLEM%` kiracı sayısı **0**.
- `tenants`: başlangıç **14**, son **16**. Bu paketin kiracısı açıldı ve tamamen silindi. Artan 2 kiracı, aynı aralıkta açılan başka şeritlerin test kiracıları: `PAKETOLCEK-1-TEST Dogrulama`, `PAKETZO-BELGE-TEST Kuyumculuk`, `PAKETSATIS-3-TEST Dogrulama` (üçü de yeni). QA-TEST kiracılarına dokunulmadı.
- Disk: hook `rm`'yi engellediği için `r15/sen/profil-*`, `r15/arac/profil-*` ve `r15` çalışma dizinini kurucu ya da orkestratör silmeli.

### 000000000000000.9 Değişen dosyalar (bu tur) · açık kalanlar
- Değişen dosyalar:
  - `dotnet/Kuyumcu.Web/Pages/PosTahsilatlar.razor` (`veriGeldi`, şubesiz 403 sade cümle, `EslestirmeYetkisi`, ızgara sınıfı)
  - `dotnet/Kuyumcu.Web/wwwroot/css/zo-islem.css` (+ seçici satır-düğme ve geniş ekran 2 sütun bloğu)
  - `dotnet/Kuyumcu.Api/Modules/Pos/PosModulu.cs` (`YatmaAdaylari` ad/alt bilgi)
  - `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoIslem.cs` (`PosAdayAlt` → `PosAdayKalan`, TR+EN)
  - `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (+4 test yöntemi, sahte gövdeler)
  - `dotnet/Kuyumcu.Tests/ZOISLEMA5Testleri.cs` (ZO29k güncellendi)
- Açık kalanlar (sahibiyle):
  - (1) **Üst bant** (`Layout/UstBar.razor:195-197` `.kur-hata` + "Kur alınamadı"): şubesiz TEZGÂHTAR'da kur servisinin 403 metnini ham basıyor → **KATMAN/ORTAK** (Layout, bu paketin alanı dışında). POS ve Hurda sayfa gövdesinde ham metin 0.
  - (2) Şubesiz TEZGÂHTAR'da BASİT/PRO seçimi uygulanmıyor: ölçümde "Basit" tıklandı, ekran PRO kaldı. Tercih kaydı büyük olasılıkla aynı şube 403'üne takılıyor; ölçülmedi → **KATMAN** (MenuTercihServisi / Api/Modules/Tercih).
  - (3) `denetim.sh` P-39 `TABLO_SARMALAYICISIZ 1` = `IslemDefteri.razor:306` → **ZO-BELGE**.
  - (4) Önceki turların açık kalanları değişmedi: ORTAK-1 O8 · SATIS-3 PosBarkod kararsız · KATMAN POS menüsü çift seçili / SatirEylemleri 375 · Banka hareket yanıtında `eslesmemisKalan` yok → Taşkın/Bertan · /hurda menü grubu kurucu/mimar kararı.

---

## 00000000000000. 11. DOĞRULAMA — tasarım hakemi k3 KRİTİK: Hurda defteri dar pencerede kırpılıyordu (K13) — 29.09.2026 (t7 · API 5170 · vekil 5171 · CDP 9803/9804/9806–9808 · `KUYUMCU_TEST_DB=kuyumcu_test_t7`)
Durum beyanı: **BITTI** (aşağıdaki 7 madde). Bu bölüm § 0000000000000'ı günceller. Çalışma dizini `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r14/`. Migration YOK; sunucu ucu/gövdesi, yetki ve yasal kapılar (MASAK, tevsik, gider pusulası) DEĞİŞMEDİ. Mevcut DOM kimlikleri korundu (`hrd-defter`, `hrd-satir-{id}`, `hrd-fark-{id}`, `hrd-duzelt-{id}`, `hrd-gecmis-ac`); yeni kimlik yok (yalnız sınıf + `data-etiket`).
Devir notu 3'teki POS elle eşleştirme çıkmazı (KRİTİK), harf duyarlılığı (ORTA) ve üç DÜŞÜK bulgu § 000000000 (6. doğrulama) turunda kapandı; bu turda o koda DOKUNULMADI, testleri (ZOS4 dahil) tam süitte koşuldu.

### 00000000000000.1 Bulgu → düzeltme
| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| [KRİTİK] Hurda defteri akordeon açıkken 1024 709/674, 960 623/610, 375 823/317 (sw/cw); 375'te HAS GRAM ve TUTAR ekran dışı, 960'ta "Tahlil farkını düzelt" kesik; Banka/POS tabloları kart olurken Hurda olmuyordu | `Hurda.razor`: tabloya `zo-kart-tablo`; hücrelere Banka/POS ile AYNI kart sınıfları (`zo-k-bas` tarih, `zo-k-durum` tür, `zo-k-ad` maden, `zo-k-gram`, `zo-k-tutar`, `zo-k-fark`, `zo-k-eylem`) ve etiket (`data-etiket`: Has gram, Tutar; tahlil farkı etiketi YALNIZ hurda satışında — boş hücre etiket basmaz). `zo-islem.css`: `@media (max-width: 1100px)` bloğu — başlık gizli, satır iki sütunlu ızgara kart; tarih · tür üstte, maden, has gram · tutar etiketiyle, tahlil farkı, düzelt düğmesi en altta tam satır ve SARABİLİR (`white-space: normal`); ≤480 px düğme tam genişlik. Eşik 1100 (hakemin "en az ≤1024"ü; Banka/POS ile aynı eşik). Hiçbir hücre silinmedi; renk yalnız belirteç, satır içi stil yok. | bUnit `ZO29p_hurda_defteri_dar_ekranda_KART_kalibi` · M104–M108 · CDP ölçümü akordeon AÇIK (aşağıda) |
| [süreç] "tur.mjs akordeonu açmadığı için K13=0 ölçtü" | Bu turun sahnesi (`r14/sen/sahne.mjs`) `/hurda`'yı iki kez ölçer: açılış ve `#hrd-gecmis-ac` tıklanıp akordeon AÇIKKEN; ek olarak defter kabının sw/cw'si, kabın/ekranın dışına taşan hücre listesi ve her düzelt düğmesinin sığıp sığmadığı ölçülür. | `r14/sen/sonuc.jsonl` |

### 00000000000000.2 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra) → **0 Hata, 0 Uyarı** (`r14/build2.log`).
- Yeni test (1): `ZO29p_hurda_defteri_dar_ekranda_KART_kalibi` (DOM sınıfları + etiketler + alış satırında etiketsiz fark hücresi + CSS medya bloğu).
- Filtreli (`ZOISLEMA|P20b|Q102|Hurda`) → **348/348** (`r14/t1.log`).
- Mutasyon (`r14/mut.sh`: yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/r14/<kod>-<dosya>` → `perl -0777 -pi` tek eşleşme (önceden `grep -c` = 1 doğrulandı) → derle → `--filter ZOISLEMAEkranTestleri` → geri kopya → `cmp`). Hiçbir testi kırmayan mutasyon: **0**.

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M104 | `class="sag zo-k-gram"` → `class="sag"` | `ZO29p_…` (67/68) | AYNI |
| M105 | fark etiketi koşulsuz (`data-etiket="@TahlilFarkiMetni"`) | `ZO29p_…` | AYNI |
| M106 | eylem düğmesi `white-space: normal` → `nowrap` | `ZO29p_…` | AYNI |
| M107 | `#hrd-defter > thead { display: none; }` → `table-header-group` | `ZO29p_…` | AYNI |
| M108 | `<td class="zo-k-eylem">` → `<td>` | `ZO29p_…` | AYNI |

- `bash ./denetim.sh` (son derlemeden sonra) → **DENETİM TEMİZ** (`r14/denetim.log`).

### 00000000000000.3 Canlı doğrulama — HTTP + SQL + EKRAN (5170; kiracı `PAKETZO-ISLEM-A-TEST` 01M3NGZK37EGPMM9FWAX8J4EF8; sistem anahtarı rastgele, chmod 600, yalnız süreç ortamında; parolalar chmod 600 fikstürde; yanıtlar okundu)
| Adım | Beklenen | Gerçek |
|---|---|---|
| Kurulum (SAHİP): kasa, kur, Ziraat hesabı + GELEN 3.000, POS cihazı, işçilik KART 1.500 | 200 | davet 201 · kayıt 201 · kasa/banka/dekont/POS/işçilik **200** |
| MÜDÜR hurda alışı ×5 (kasa boşken) | 400 kasa yetersiz | **400** "'Ana Kasa' kasasının bakiyesi yetersiz: 1500.00 TRY var, 4580.00 TRY isteniyor…" (gerçek kural; nakit eklendi) |
| MÜDÜR ramat çıkışı hurda yokken | 400 | **400** "Bu şubede yeterli hurda yok: elde 0 has gram…" |
| İşçilik NAKİT 30.000, MÜDÜR hurda alışı ×3 | 200 | **200** ×3 |
| MÜDÜR ramat çıkışı 1 has, alıcı 0,95, tahsil 475 (cari yok) | 400 | **400** "Tahsil edilmeyen 25 TL için ramatçı carisi zorunludur…" |
| MÜDÜR ramat çıkışı 1 has (alıcı 0,95) ve 2 has, tamamı tahsil | 200 | **200** ×2 (toplam 500 / 1.000, kalan 0) |
| `GET /hurda/defter?limit=100` MÜDÜR (şubesi var) | 5 satır | **200** 2 × HURDA_SATIS (−2 / −1 has), 3 × HURDA_ALIS (9,16 has, −4.580) |
| Ekran /hurda akordeon AÇIK, 16 kombinasyon + 1024 ×2 | kart ≤1100, taşan hücre 0, düzelt sığar | aşağıdaki tablo |

SQL (`r14/sql.out`, `hareketler` bu kiracı): `HURDA_ALIS 3 satır Σhas 27,48 ΣTL −13.740 · 1 şube`; `HURDA_SATIS 2 satır Σhas −3,00 ΣTL 1.500 · 1 şube` — ekrandaki 5 kart ile birebir.

### 00000000000000.4 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum MÜDÜR, taze profil)
Matris (`r14/sen/sahne.mjs` + `kos.sh`, ham `r14/sen/sonuc.jsonl`): 16 kombinasyon ({1440,1280,960,375} × {BASİT,PRO} × {aydınlık,karanlık}) × 4 sahne (hurda-acilis, hurda-defter-acik, pos-acilis, banka-acilis) = **64 ölçüm**, 16/16 rc=0, mod uyumsuz 0. Ek: 1024×768 BASİT aydınlık + PRO karanlık.

| Ölçü | Sonuç |
|---|---|
| yatay kaydırma, K1, K3_tasan, K3_kirik, K5_TRY, K7, K8, K9_binen, K9b, K10, K10b, K11, K12, K13 | **64/64 sıfır** (1024 ek ölçümde de K3/yatay 0) |
| Açılış (1440, K4_boyKat) — /hurda, /pos/tahsilatlar, /banka | 12/12 **1** ekran |
| BASİT↔PRO (`tutarlilik.mjs`, 3 rota, 1440) | mod farkı **0**, düğme sınıf grubu 6 |

Hurda defteri akordeon AÇIK (`hurda-defter-acik`):
| Genişlik | .tablo-sar sw/cw (önce → sonra) | Kart | Kabın/ekranın dışındaki dolu hücre | "Tahlil farkını düzelt" |
|---|---|---|---|---|
| 1440 (×4) | 1090/1090 → 1090/1090 | tablo | 0 | 2/2 sığar, tek satır (34 px) |
| 1280 (×4) | 930/930 → 930/930 | tablo | 0 | 2/2 sığar |
| 1024 (×2) | 709/674 → **674/674** | kart | 0 | 2/2 sığar |
| 960 (×4) | 623/610 → **610/610** | kart | 0 | 2/2 sığar |
| 375 (×4) | 823/317 → **317/317** | kart | 0 (has gram + tutar görünür) | 2/2 sığar, tam genişlik (44 px) |

Görüntüler (Read ile bakıldı): `r14/sen/foto/hurda-defter-acik_BASIT_aydinlik_375.png` (her kayıt kart: tarih · tür, Altın, Has gram / Tutar etiketli, tahlil farkı, tam genişlik düzelt), `hurda-defter-acik_PRO_karanlik_960.png` (kart, düzelt kesilmiyor), `hurda-defter-acik_BASIT_aydinlik_1280.png` (tablo, 7 sütun, düğme sığıyor). Kopyalar `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/ZO-ISLEM-A-r14-*.png` (5 dosya: 375 BASİT/PRO karanlık, 960 PRO karanlık, 1024 BASİT, 1280 BASİT).

### 00000000000000.5 Tam süit (paralel şerit kapısı madde 7)
- Koşum (son değişiklik ve son mutasyondan sonra, TRX `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-r14.trx`, `r14/tam.log`): **7009 test, 7008 geçti, 1 başarısız** — yalnız `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1; devir listesinde ve § 0000000000000.5'te aynı).
- **FARK (bu paketin yeni kırmızısı) = 0.**

### 00000000000000.6 DoD sözleşmesi (bu tur)
Yeni varlık, yeni uç, yeni düğme YOK. Hurda satışı düzeltme yolu (`hrd-duzelt-{id}` → tahlil farkı düzeltme çekmecesi) değişmedi ve artık her genişlikte görünür; alış satırı değişmez, gerekçe `DegismezIpucu Varlik="HurdaSatisi"` kutusunda (değişmedi).

### 00000000000000.7 Sapmalar ve gerekçeleri · dokunulan sayfalar
- Eşik 1100 px (hakem "en az ≤1024" dedi): aynı paketteki Banka hareketleri ve POS defteriyle aynı eşik — tek kalıp, tek ölçü; 1100–1280 arası tablo kabına sığıyor (1280'de 930/930).
- Dokunulan sayfa: `Hurda.razor` (paketin kendi sayfası; gerekçe: tasarım hakemi k3 KRİTİK).
- Mevzuat: yeni hüküm yok.

### 00000000000000.8 Temizlik
API 5170 ve vekil 5171 durduruldu (dinleyen 0); `pgrep -f ZO-ISLEM-A/r14` 0 (Chrome dahil). Kiracı 01M3NGZK37EGPMM9FWAX8J4EF8 70 `tenant_id`'li tablodan `r14/sil.sql` ile silindi; yeniden sayım **0** (`r14/say.sql`); `PAKETZO-ISLEM%` kiracı **0**. `tenants`: başlangıç **16** → son **15** — bu paketin kiracısı açılıp tamamen silindi; eksilen 1 kiracı başka şeridin (aynı aralıkta kendi kiracısını silen şerit; `PAKETZO-BELGE-TEST` yerinde duruyor). QA-TEST kiracılarına dokunulmadı. Disk: `rm` hook'la yasak → `r14/sen/profil-*` (Chrome profilleri) ve `r14` çalışma dizini kurucu/orkestratör tarafından silinmeli.

### 00000000000000.9 Değişen dosyalar (bu tur) · açık kalanlar
- `dotnet/Kuyumcu.Web/Pages/Hurda.razor` (defter tablosu: sınıf + `data-etiket`, yorum) · `dotnet/Kuyumcu.Web/wwwroot/css/zo-islem.css` (+ Hurda defteri kart bloğu, ~35 satır) · `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (+1 test).
- Açık kalanlar (sahibiyle): § 0000000000000.9'daki maddeler değişmedi (ORTAK-1 O8 · SATIS-3 PosBarkod kararsız · KATMAN POS menüsü çift seçili / SatirEylemleri 375 · Banka hareket yanıtında `eslesmemisKalan` yok → Taşkın/Bertan · PosModulu ret iletisinde ham ondalık · sikisma.js pencere perdesi · /hurda menü grubu kurucu/mimar kararı).

---

## 0000000000000. 10. DOĞRULAMA — tasarım hakemi t1: 3 YÜKSEK (POS bayat seçim · Banka talimat satırı/POS satırı eylemi · Dekont Kaydet ekran dışı) — 29.09.2026 04:05–05:10 (t7 · API 5170 · vekil 5171 · CDP 9803–9805 · `KUYUMCU_TEST_DB=kuyumcu_test_t7`)
Durum beyanı: **BITTI**. Bu bölüm § 000000000000'ı günceller (geçerli durum budur). Çalışma dizini `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r13/`. Migration YOK, sunucu ucu/gövdesi DEĞİŞMEDİ (Banka sayfası mevcut `GET /pos/cihazlar` ucunu okur). Yetki ve yasal kapılar (MASAK, tevsik, gider pusulası) DEĞİŞMEDİ. Mevcut DOM kimlikleri korundu (`pos-eslestir-hareket`, `pos-oto-oneri`, `bh-bekliyor-not`, `dekont-kaydet`, `dekont-kaydet-sebep`, `bh-eslestir-{id}` …); yeni kimlik `bh-pos-{id}`.

### 0000000000000.1 Bulgu → düzeltme
| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| [YÜKSEK] POS: eşleştirmeden sonra seçili hareket eski "kalan"ıyla kalıyor, yeniden eşleştirmeye çağırıyor; "Öneri getir" ara adımı | `PosTahsilatlar.razor`: (a) `BankaHareketSecildi` artık async — seçimle birlikte `OneriGetir()` OTOMATİK çalışır (düğme yenilemek için durur, kimlik aynı); (b) `EslestirUygula` başarıda `bankaHareketId = ""` (seçim temizlenir, bayat kart ve bayat kalan gider) ve yatmanın yeni kalanı banda eklenir: kalan 0 → `ZO.PosTamamiEslesti` "Bu yatmanın tamamı eşleşti.", kalan > 0 → `ZO.PosKalanVar` "Bu yatmada eşleşmemiş {0} kaldı; yeniden seçip eşleştirebilirsiniz." (TR+EN). (c) Canlıda yakalanan ikinci kusur: seçim temizlenince gözatma listesi YENİDEN GELMİYORDU (kullanıcı yazmadan seçtiyse `metin ""` = `sonAranan ""` → arama tetiklenmiyor; "yeniden seçip eşleştirin" çıkmazdı). `Bilesenler/KayitSecici.razor` `OnParametersSet` dış temizlemede `sonAranan = null` (kendi `Temizle()`'si ile AYNI) → liste sunucudan TAZE gelir. | bUnit `ZO29m` · `ZO28o` (güncellendi: bant sonuna "· Bu yatmanın tamamı eşleşti.") · M95, M96, M97, M102 · canlı ekran E1–E4, F0–F8 |
| [YÜKSEK] Banka: tablonun altında "'Bekliyor' = … 'Müşteriyle eşleştir' düğmesiyle eşleştirin" talimat satırı (K17); POS yatması satırında "Müşteriyle eşleştir" | `Banka.razor`: `#bh-bekliyor-not` aynı kimlikle kapalı "Ayrıntı" katlanının (`details#bh-degismez-ayrinti`) İÇİNE taşındı (görünür akışta değil; ipucu olarak durur). POS cihazlarının hesapları `GET /pos/cihazlar` → `bankaHesapId` kümesi (açılışta bir kez; okunamazsa küme boş kalır → hiçbir satırda POS eylemi çıkmaz, tahmin yok; P-11: catch içinde veri boşaltılmaz). POS hesabına GELEN, müşteriye bağlanmamış, BEKLİYOR satırın BİRİNCİL eylemi `bh-pos-{id}` "POS tahsilatıyla eşleştir" → `/pos/tahsilatlar`; "Müşteriyle eşleştir" "⋯" menüsünde kalır (aynı hesaba müşteri havalesi de gelebilir; karar sunucuda — `PosYatmasiKapisi`). | bUnit `ZO29n` ×2 · M98, M99, M101b, M101c · canlı 16/16 `G26 = "POS tahsilatıyla eşleştir ⋯"`, `bekliyorNotGorunur false`, `bekliyorNotKatlanda true` |
| [YÜKSEK] Dekont: 185 bin üstünde Kaydet ekran dışında (top 1909 / 900), tutar kutusu gruplanmıyor | Pencere altbilgisi ortak `ModalPencere`'nin SABİT altlığına (`Altlik`, P-36 — kodda vardı, hiç kullanılmamıştı) taşındı: kilit sebebi + Vazgeç/Kaydet; gövde kayar, altlık sabit. `zo-islem.css`: `.modal-alt .zo-alt-sebep` / `.zo-alt-dugmeler` tam satır (belirteçsiz renk yok). Gruplama: ortak `Para` bileşeni (`SayisalAlan.OdakGitti` → `Bicim.ParaGirdi`) zaten grupluyor — bUnit'te ve GERÇEK Chrome'da (CDP `Input.insertText` + gerçek blur) 16/16 `250.000`. Hakemin '250000' ölçümü alanın odaktan çıkmadığı (ya da odak olayı üretmeyen yazma) ölçümdür; ortak bileşende değişiklik gerekmedi, OLCEK/BICIM şeridine bildirim gerekmez. | bUnit `ZO29o` · M100 · canlı 16/16 `kaydet.gorunur true`, `sebep.gorunur true`, `kaydetAltlikta true`; 1440: Kaydet alt kenarı 843 / 900, 375: 759 / 812 |

### 0000000000000.2 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo --no-incremental` (son değişiklikten ve son mutasyondan sonra) → **0 Hata, 1 Uyarı** (`EtiketEkranTestleri.cs(145)` xUnit1031 — başka şeridin dosyası).
- Yeni testler (4): `ZO29m_pos_hareket_secilince_ONERI_otomatik_eslestirince_secim_TEMIZLENIR_kalan_banda`, `ZO29n_banka_POS_hesabi_satiri_POS_ile_eslestir_birincil_bekliyor_notu_katlanda`, `ZO29n_banka_POS_hesabi_OLMAYAN_satirda_POS_eylemi_yok_musteriyle_eslestir_birincil`, `ZO29o_dekont_kaydet_ve_sebep_SABIT_altlikta_tutar_odak_disinda_gruplanir`. Güncellenen (gerekçeli): `ZO28o_…` (bant artık yatmanın yeni kalanını da yazar).
- Filtreli (`ZOISLEMA|Pos|Banka|Q91|Q102|P20Ekran|Sozluk|SADE1|ModalPencere|MasakFormu|ZOS`) → **657/657**; yeni 5 test 5 ardışık koşumda 5/5.
- Mutasyon (`r13/mut.sh`: yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/r13/<kod>-<dosya>` → `perl -0777 -pi` tek eşleşme → derle → `--filter ZOISLEMAEkranTestleri` → `cp` ile geri → `cmp`; günlükler `r13/mut/`). Hiçbir testi kırmayan mutasyon: **0** (M101 ve M103 açık buldu → test güçlendirildi / ölü mekanizma kaldırıldı, aşağıda).

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M95 | başarıda `bankaHareketId = ""` silindi (seçim kalır) | `ZO29m_…` | AYNI |
| M96 | seçimde otomatik öneri `if (false)` | `ZO29m_…` | AYNI |
| M97 | bant sonuna kalan ekleme `&& false` | `ZO28o_…`, `ZO29m_…` | AYNI |
| M98 | POS eylemi `if (false && …)` | `ZO29n_banka_POS_hesabi_satiri_…` | AYNI |
| M99 | POS hesabı süzgeci silindi (her GELEN satır POS) | `ZO29n_banka_POS_hesabi_OLMAYAN_…` | AYNI |
| M100 | dekont `Altlik` kaldırıldı (düğmeler gövdeye döner) | `ZO29o_…` | AYNI |
| M101 | talimat satırı katlanın DIŞINA ikinci kez (aynı id) | — (YEŞİL kaldı → açık) | AYNI |
| M101b / M101c | aynı (id'li / id'siz) — test güçlendirildikten sonra | `ZO29n_banka_POS_hesabi_satiri_…` / aynı | AYNI / AYNI |
| M102 | `KayitSecici` dış temizlemede `sonAranan = null` silindi | `ZO29m_…` (liste yeniden gelmez) | AYNI |
| M103 | `posHesapOkunamadi` bayrağı hiç kurulmaz | — (YEŞİL: bayrak ölü mekanizma, küme zaten boş) → bayrak KALDIRILDI | AYNI |

- `bash ./denetim.sh` → ilk koşum **BAŞARISIZ**: `P-11: DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48 > tavan 47` (benim `catch { posHesaplari = []; }`) → catch veri boşaltmaz hâle getirildi → **47 (tavan 47) · DENETİM TEMİZ** (`r13/denetim3.log`, son derlemeden sonra).

### 0000000000000.3 Canlı doğrulama — HTTP + SQL + EKRAN (5170; kiracı `PAKETZO-ISLEM-A-TEST` 01M3NCA6N39QWH3H1D7V3ZJ7V7; sistem anahtarı rastgele, chmod 600, yalnız süreç ortamında; yanıtlar okundu — `r13/canli.out`, `r13/sql.out`, `r13/sen/ekran.log`, `ekran2.log`)
Kurulum (`r13/kur.py`, `senaryo.py`, `ek.py`): Garanti BBVA · POS Hesabı + Ziraat · Vadesiz; "Garanti POS" cihazı Garanti hesabına bağlı; KART işçilik 12.000 / 15.000 / 20.000 → tahsilat net 11.760 (t1) / 14.700 (t2) / 19.600 (t3); Garanti GELEN 26.460 "Garanti POS yatan" (G26), 30.000 "Garanti POS yatan 2" (G30), müşteri havalesi G11C; Ziraat Z26, Z11C.

| Adım (ekrandan, 1440 BASİT, ŞUBESİZ MÜDÜR) | Beklenen | Gerçek |
|---|---|---|
| L0 aday listesi (yazmadan) | G26 kalan 26.460 | `Garanti BBVA · POS Hesabı … +26.460 ₺ · kalan 26.460 ₺` |
| S1 G26 seçilir — "Öneri getir"e BASILMAZ | öneri kendiliğinden, iki satır işaretli | `elle:true`, 2 işaretli, "Seçilen 26.460 ₺ · bankaya yatan 26.460 ₺" |
| t2 kaldırılır → Eşleştir | kısmi; seçim temiz; kalan banda | bant **"Eşleştirme tamamlandı · 11.760 ₺ · 1 tahsilat · Bu yatmada eşleşmemiş 14.700 ₺ kaldı; yeniden seçip eşleştirebilirsiniz."**; `secili:null`, `elle:false`, kilit "Önce bankaya yatan tutarı seçin.", Öneri getir kilitli (E2) |
| (düzeltme ÖNCESİ) liste | taze liste | `[]` — **çıkmaz yakalandı** → `KayitSecici` düzeltmesi (M102) |
| F0 (düzeltme sonrası, yeni API) | G30 30.000 + G26 kalan 14.700 | ikisi de listede, hesap adı + kalan |
| F1–F3 G30 seç → yalnız t3 → Eşleştir | kalan 10.400 banda | bant "… 19.600 ₺ · 1 tahsilat · Bu yatmada eşleşmemiş 10.400 ₺ kaldı; …" (F3) |
| F4 liste (yazmadan, TAZE) | G30 kalan 10.400, G26 kalan 14.700 | `… +30.000 ₺ · kalan 10.400 ₺`, `… +26.460 ₺ · kalan 14.700 ₺` (F4 görüntüsü) |
| F5–F7 G26 seç (öneri otomatik t2) → Eşleştir | tamamı eşleşti | öneri t2 işaretli, "Seçilen 14.700 ₺ · bankaya yatan 14.700 ₺"; bant **"… 14.700 ₺ · 1 tahsilat · Bu yatmanın tamamı eşleşti."** |
| F8 liste | G26 düşer | yalnız G30 (kalan 10.400) |
| HTTP A `GET /pos/cihazlar` MÜDÜR / MUHASEBE / anonim | 200 / 200 / 401, cihazın hesabı Garanti | **200** `[('Garanti POS', True)]` / **200** aynı / **401** |
| HTTP B adaylar | yalnız G30 kalan 10.400 | **200** `[G30 '… +30.000 ₺ · kalan 10.400 ₺']` |
| HTTP E tamamı eşleşmiş G26 + t3 1 TL | 400 | **400** "Eşleşen toplam banka hareketi tutarını aşamaz …" |
| Banka ekranı (yeni BEKLİYOR "Garanti POS yatan 3" 5.000, 375 BASİT) | satırda "POS tahsilatıyla eşleştir" | `POS tahsilatıyla eşleştir ⋯`, talimat satırı görünmez (katlanda) |

SQL (`r13/sql.out`): `pos_yatma_eslesmeleri` 3 satır, hepsi Garanti hareketlerinde, `geri_alindi 0`: 11.760 + 14.700 → "Garanti POS yatan" 26.460 (ESLESTI), 19.600 → "Garanti POS yatan 2" 30.000 (ESLESTI, kalan 10.400). `pos_tahsilatlari`: 11.760/11.760 YATTI, 14.700/14.700 YATTI, 19.600/19.600 YATTI. Ziraat 26.460 BEKLIYOR (dokunulmadı), müşteri havaleleri ESLESTI + cari bağlı (değişmedi).

### 0000000000000.4 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, taze profil)
Matris (`r13/sen/sahne.mjs` + `kos.sh`, ham `r13/sen/sonuc.jsonl`): 16 kombinasyon ({1440,1280,960,375} × {BASİT,PRO} × {aydınlık,karanlık}) × 4 sahne (banka-garanti-hareket, dekont-250bin, pos-secim-otomatik-oneri, hurda-acilis) = **64 ölçüm**, 16/16 rc=0, mod uyumsuz 0; son değişiklikten sonra PRO karanlık 1440 ve (yeni BEKLİYOR satırla) BASİT 375 yinelendi.

| Ölçü | Sonuç (64) |
|---|---|
| yatay kaydırma, K1, K3_tasan, K3_kirik, K5_TRY, K7, K8, K10, K10b, K11, K12, K13, K9b | **0** |
| K9_binen | 48 sahnede **0**; `dekont-250bin` 16/16'da 6 — hepsi AÇIK PENCERENİN altında kalan sayfa öğesi ⟂ pencere öğesi çifti (ör. `#sayfa-basi-banka-baslik ⟂ #modal-… "Dekont Gir"`, `p.stat-deger "76.440 ₺" ⟂ "Tutar"`): perde arkası, görsel binme değil (sikisma.js pencere perdesini ayırmıyor). Pencere İÇİ, kaydırma kabının kırptığı kısmı hesaba katan ölçüm (`ekran.mjs` M 1440 / M 375): **`kirpmaDuyarliBinen: []`** ×2 |
| dekont-250bin | 16/16 tutar `250.000`, Kaydet ve kilit sebebi görünür, Kaydet altlıkta |
| banka-garanti-hareket | 16/16 G26 birincil eylem "POS tahsilatıyla eşleştir", G11C (müşteri havalesi, Eşleşti) "Faturaya Dönüştür"; talimat satırı görünmez |
| pos-secim-otomatik-oneri | 16/16 seçimden sonra (düğmeye basmadan) elle liste açık, 2 işaretli, Eşleştir açık |
| BASİT↔PRO (`tutarlilik.mjs`, /hurda,/pos/tahsilatlar,/banka, 1440) | mod farkı **0**, düğme sınıf grubu 6 |

Görüntüler (Read ile bakıldı): `r13/sen/foto/banka-garanti-hareket_BASIT_aydinlik_1440.png` (TUTAR sütunu, "POS tahsilatıyla eşleştir", talimat satırı yok), `dekont-250bin_BASIT_aydinlik_1440.png` (250.000; Vazgeç/Kaydet + sebep sabit altlıkta), `dekont-250bin_PRO_karanlik_375.png`, `F4-liste-taze-kalanlar_BASIT_1440.png` (bant kalanı yazıyor, liste taze kalanlarla), `banka-garanti-hareket_BASIT_aydinlik_375.png`. Kopyalar `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/ZO-ISLEM-A-r13-*.png` (9 dosya).

### 0000000000000.5 Tam süit (paralel şerit kapısı madde 7)
- Koşum 1 (ilk düzeltmelerden sonra, TRX `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-r13.trx`, `r13/tam.log`): **6971 test, 6970 geçti, 1 başarısız** — `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1; devir listesinde).
- Koşum 2 (`KayitSecici` düzeltmesinden sonra, `r13/test5.log`): 6971 / 6970 / 1 — yine yalnız O8.
- Koşum 3 (SON değişiklik — P-11 düzeltmesi — sonrası, TRX `tam-r13b.trx`, `r13/tam2.log`): **6976 test, 6975 geçti, 1 başarısız** — yalnız O8.
- **FARK (bu paketin yeni kırmızısı) = 0.** Filtreli koşumlarda bir kez görülen `PosBarkodTestleri.Fiyat_SUNUCU_teklifinden_gelir_kur_kaydiyla_istenir` tek başına 3/3 yeşil ve üç tam süitte yeşil — devir listesindeki kararsız (SATIS-3).

### 0000000000000.6 DoD sözleşmesi (bu tur)
Yeni varlık YOK, yeni uç YOK. `PosYatmaEslesme` değişmedi (append-only; geri alma "Eşleşmeyi geri al"). Yeni düğme `bh-pos-{id}` gerçek gezinme yapar (`/pos/tahsilatlar`); POS eşleştirme ve Öneri getir düğmeleri istek atar ya da gerekçeli kilitlidir ("Önce bankaya yatan tutarı seçin."). Dekont Kaydet kilitliyken sebebi sabit altlıkta görünür (UX-S30). Başarı bandı sunucunun sonucunu + yatmanın yeni kalanını yazar (UX-S27).

### 0000000000000.7 Sapmalar ve gerekçeleri · dokunulan sayfalar
- `Bilesenler/KayitSecici.razor` (ortak, hiçbir eşzamanlı şeridin listesinde değil): tek satır — dış temizlemede `sonAranan = null`, bileşenin kendi `Temizle()` davranışıyla birebir; gözatılamaz türlerde etkisiz (metin "" < 2 karakter → arama yok). Canlıda bulunan çıkmazın kök nedeni buydu.
- Banka "POS tahsilatıyla eşleştir" POS ekranına götürür, hareketi orada önceden seçmez (POS sayfasında sorgu parametresi + seçici ön seçimi daha büyük ayak izi; seçici artık yazmadan listelediği için tek tık).
- Kısmen eşleşmiş POS yatması Banka ekranında sunucu durumu gereği "Eşleşti" görünür ve POS eylemi çıkmaz (Banka hareket yanıtında kalan yok); kalan POS ekranının listesinde görünür → açık kalan.
- Dokunulan sayfalar: `PosTahsilatlar.razor`, `Banka.razor` (paketin sayfaları; gerekçe: tasarım hakemi t1 YÜKSEK ×3).
- Mevzuat: yeni hüküm yok.

### 0000000000000.8 Temizlik
API 5170 ve vekil 5171 durduruldu (dinleyen 0); `pgrep -f ZO-ISLEM-A/r13` 0 (Chrome dahil). Kiracı 01M3NCA6N39QWH3H1D7V3ZJ7V7 70 `tenant_id`'li tablodan `r13/sil.sql` ile silindi; yeniden sayım **0**; `PAKETZO-ISLEM%` kiracı **0** (hakem kiracısı 01M3JG487JCP11FMBAFRMR4X4P de yok). `tenants`: başlangıç **16** → son **15** — fark bu paketin değil: aynı aralıkta başka şeritler kendi kiracılarını sildi/açtı (SATIS-3-HAKEM7, eski ZO-BELGE, OLCEK-1P gitti; yeni ZO-BELGE, OLCEK-1 geldi); bu paketin kiracısı açılıp tamamen silindi. QA-TEST kiracılarına dokunulmadı. Disk: `rm` hook'la yasak → `r13/sen/profil-*` ve `r13` çalışma ağacı kurucu/orkestratör tarafından silinmeli.

### 0000000000000.9 Değişen dosyalar (bu tur) · açık kalanlar
- `dotnet/Kuyumcu.Web/Pages/PosTahsilatlar.razor` (otomatik öneri, seçim temizleme, kalan banda) · `dotnet/Kuyumcu.Web/Pages/Banka.razor` (talimat satırı katlana, POS satırı eylemi + `/pos/cihazlar` okuma, dekont sabit altlık) · `dotnet/Kuyumcu.Web/Bilesenler/KayitSecici.razor` (`sonAranan = null`) · `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoIslem.cs` (+`PosTamamiEslesti`, `PosKalanVar`, `BankaPosEslestir` TR+EN) · `dotnet/Kuyumcu.Web/wwwroot/css/zo-islem.css` (+2 kural) · `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (+4 test, 1 güncelleme).
- Açık kalanlar (sahibiyle): ORTAK-1 `OrtakGorunumSaflik.O8` · SATIS-3 `PosBarkod` kararsız · KATMAN: POS menüsünde iki satır birlikte seçili (F4 görüntüsünde "POS cihazları" + "POS alacakları"), `SatirEylemleri` 375 menü konumu · Banka hareket yanıtı kalan taşımadığı için kısmen eşleşmiş POS yatması Banka ekranında "Eşleşti" (Taşkın/Bertan kararı: yanıt sözleşmesine `eslesmemisKalan` eklenmesi) · sunucu ret metni "hareket 26460,00" ham ondalık (PosModulu iletisi; tr-TR biçim) · sikisma.js açık pencerede perde arkası öğeleri "binen" sayıyor (araç sahibine) · § 00000000.8'deki menü grubu maddeleri değişmedi.

---

## 000000000000. 9. DOĞRULAMA — TASARIM HAKEMİ t3 KRİTİK: POS tahsilatı ilgisiz hesaptaki müşteri havalesiyle eşleşiyordu — 29.09.2026 02:40–03:45 (t7 · API 5170 · vekil 5171 · CDP 9803/9806–9808 · `KUYUMCU_TEST_DB=kuyumcu_test_t7`)
Durum beyanı: **BITTI**. Bu bölüm § 00000000000'ı günceller (geçerli durum budur). Çalışma dizini `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r12/`. Migration YOK (şema değişmedi). Yeni uç: `GET /api/v1/pos/yatma-adaylari` (salt okuma, `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]`, `[ModulYetkisi("pos")]` denetleyiciden). Yasal kapılar (MASAK/tevsik/gider pusulası) DEĞİŞMEDİ.

### 000000000000.1 Bulgu → düzeltme (Bertan/Taşkın gözü: kural SUNUCUDA, ekran yalnız yansıtır)
| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| [KRİTİK] Garanti POS cihazının tahsilatı Ziraat hesabındaki müşteri havalesiyle eşleşti ("Yattı") | `PosModulu.cs` iki kapı, TEK kural, üç yol (aday listesi, öneri, eşleştirme): **`PosYatmasiKapisi`** — hareket GELEN olmalı; müşteriye bağlı (`cari_id`) → 400 "Bu banka hareketi bir müşteriyle eşleşmiş; POS yatması olarak kullanılamaz"; faturaya bağlı / durum BEKLIYOR·ESLESTI dışı → 400; virman bacağı (`karsi_bacak_id`) → 400; ters kayıt ya da ters kaydı yapılmış (`storno_kaynak_id`) → 400; hesabında hiç POS cihazı yoksa → 400 "Bu banka hesabına bağlı POS cihazı yok…". **`CihazHesapKapisi`** (her satır) — tahsilatın cihazının `banka_hesap_id`'si tanımsızsa → 400 "<cihaz> cihazının parasının yattığı banka hesabı tanımlı değil; POS Cihazları ekranından hesabı seçin" (fail-closed: hangi hesaba yattığı bilinmeden kapanış yok); hareketin hesabından farklıysa → 400 "<cihaz> cihazının parası başka bir banka hesabına yatar…"; para birimi farklıysa → 400. `OtoEslestirOnerisi` aynı `PosYatmasiKapisi`'ndan geçer, adayları yalnız hareketin hesabına bağlı cihazların tahsilatlarıyla sınırlar ve yanıtta `uygunCihazlar` döner. | DB `ZO29g` ×2, `ZO29h` ×2, `ZO29i`, `ZO29j` · M84–M88, M91, M92 · canlı B–G |
| Seçici (genel `/arama`) POS dışı hareketleri de getiriyordu; sonuçta hesap adı yoktu | Yeni `GET /pos/yatma-adaylari?q=&limit=&dil=` → aynı `AramaYaniti` sözleşmesi: YALNIZ POS cihazlarının hesaplarına GELEN, müşteriye/faturaya bağlanmamış, virman/ters kayıt olmayan ve eşleşmemiş KALANI > 0 olan hareketler; `Ad` = "<Banka> · <Hesap adı>", `AltBilgi` = "gg.aa.yyyy · <karşı taraf> +<tutar> · kalan <kalan>" (`Sozlukler.ZoIslem` `PosAdayAlt` TR+EN; para `AramaServisi.EkranPara` = Web `Bicim.Para` ile aynı kural). Sorgu boşken gözatma (en yeni adaylar — tezgâhtar 2 karakter yazmak zorunda değil); sorgu: hesap adı / karşı taraf / referans / açıklama / tutar öneki. Sınır `limit` (≤ 50) + `dahaVar` (UX-S29). `KayitSecici.razor`'a isteğe bağlı iki parametre: `AramaYolu` (null → eski `/arama` davranışı AYNEN) ve `Gozat`. `PosTahsilatlar.razor` seçiciyi bu uca bağlar; elle seçim listesi `uygunCihazlar` ile süzülür (başka hesaba yatan cihazın satırı listede yok). | DB `ZO29k`, HTTP `ZO29k_…_MUHASEBE_200_camelCase_TEZGAHTAR_403`, bUnit `ZO29l` · M89, M90, M93, M94 · canlı A, G, H · ekran matrisi |

### 000000000000.2 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo --no-incremental` (son mutasyondan sonra) → **0 Hata, 1 Uyarı** (`EtiketEkranTestleri.cs(145)` xUnit1031 — başka şeridin dosyası); denetim düzeltmesinden sonra artımlı derleme 0 hata.
- Yeni testler (9): `ZOISLEMA5PosHesapTestleri` — `ZO29g_POS_cihazi_olmayan_hesaptaki_musteri_havalesi_POS_tahsilatini_KAPATAMAZ`, `ZO29g_baska_cihazin_hesabina_yatan_para_bu_cihazin_tahsilatini_KAPATAMAZ`, `ZO29h_musteriyle_eslesmis_hareket_POS_hesabinda_olsa_da_400`, `ZO29h_ters_kaydi_yapilmis_ve_virman_hareketi_400`, `ZO29i_cihazin_hesabi_TANIMSIZSA_eslesmez_400`, `ZO29j_oneri_YALNIZ_hareketin_hesabindaki_cihazlari_sunar`, `ZO29k_yatma_adaylari_yalniz_POS_hesabindaki_ACIK_hareketler_hesap_adi_ve_KALAN_ile`; `ZOISLEMA5HttpTestleri.ZO29k_yatma_adaylari_ucu_MUHASEBE_200_camelCase_TEZGAHTAR_403`; bUnit `ZOISLEMAEkranTestleri.ZO29l_pos_secici_YALNIZ_yatma_adaylari_hesap_adi_kalan_elle_liste_hesabin_cihazlari`. Güncellenen: `ZO28b_pos_eslestir_AYRI_ara_dugmesi_yok…` ve `PosHareketSecili` yardımcısı (sahte uç `/arama` → `/pos/yatma-adaylari`), `PosEkranTestleri` sahte API'sine yeni uç için gerçekçi yanıt ('{}' değil).
- Fikstür (`PosYardim.cs`, gerekçeli): yeni kural cihazın hesabını zorunlu kıldığı için `CihazYaz` cihazı varsayılan olarak kiracının "Garanti · Ana" fikstür hesabına bağlar (`hesapsiz: true` / `bankaHesapId:` ile değiştirilir), `BankaHareketiYaz` aynı hesaba yazar (`hesapId:`, `cariId:` parametreleri eklendi), `HesapYaz` yeni. Eski POS testleri (Paket 31/P-22/S3f/ZO4) davranış değişmeden yeşil.
- Filtreli: `ZOISLEMA|Pos|P22|S3f|Arama|SADE1|Banka|Q102|P20Ekran|Sozluk` → **849/849** (son değişiklikten sonra yinelendi: 849/849).
- Mutasyon (`r12/mut.sh`: yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/r12/<kod>-<dosya>` → `perl -0777 -pi` tek eşleşme → derle → `--filter ZOISLEMA5|ZO29l|PosEslestirme|ZOISLEMA4` → `cp` ile geri → `cmp`; özet `r12/mut/ozet.txt`). Hiçbir testi kırmayan mutasyon: **0**.

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M84 | müşteriye bağlı hareket kapısı `if (false)` | `ZO29h_musteriyle_eslesmis_hareket_POS_hesabinda_olsa_da_400` | AYNI |
| M85 | cihaz hesabı ≠ hareket hesabı kapısı kapalı | `ZO29g_baska_cihazin_hesabina_yatan_para_…` | AYNI |
| M86 | "hesapta POS cihazı yok" kapısı kapalı | `ZO29g_POS_cihazi_olmayan_hesaptaki_musteri_havalesi_…` | AYNI |
| M87 | cihaz hesabı tanımsız kapısı kapalı | `ZO29i_cihazin_hesabi_TANIMSIZSA_eslesmez_400` | AYNI |
| M88 | öneride cihaz-hesap süzgeci silindi | `ZO29j_oneri_YALNIZ_hareketin_hesabindaki_cihazlari_sunar` | AYNI |
| M89 | adaylarda `CariId == null` süzgeci silindi | `ZO29k_yatma_adaylari_…` | AYNI |
| M90 | adaylarda POS hesabı süzgeci silindi | `ZO29k_yatma_adaylari_…` | AYNI |
| M91 | virman kapısı kapalı | `ZO29h_ters_kaydi_yapilmis_ve_virman_hareketi_400` | AYNI |
| M92 | "ters kaydı yapılmış" sorgusu `|| false` | `ZO29h_ters_kaydi_yapilmis_ve_virman_hareketi_400` | AYNI |
| M93 | sayfada `AramaYolu` silindi (genel /arama'ya döner) | `ZO29l_pos_secici_YALNIZ_yatma_adaylari_…` | AYNI |
| M94 | sayfada elle liste `uygunCihazlar` süzgeci silindi | `ZO29l_pos_secici_YALNIZ_yatma_adaylari_…` | AYNI |

- `bash ./denetim.sh` → ilk koşum **KIRMIZI**: `Sınırsız ToListAsync (Modules · B-8) 149 (tavan 145 — ARTIŞ)` — benim dört yeni sorgum (POS hesap kimlikleri, hesap adı eşleşmesi, uygun cihazlar, hesap adı sözlüğü) `Take`'siz idi → `Take(AzamiSatir)` / `Take(al)` eklendi → **145 (tavan 145) · DENETİM TEMİZ** (`r12/denetim2.log`).

### 000000000000.3 Canlı doğrulama — HTTP + SQL (5170; kiracı `PAKETZO-ISLEM-A-TEST` 01M3N7T8P1SCG9EYZKB87MYPZN; sistem anahtarı rastgele, yalnız süreç ortamında, chmod 600; parolalar chmod 600 fikstürde; yanıtlar okundu — `r12/canli.out`, `r12/canli2.out`, `r12/sql.out`)
Kurulum: 2 hesap (Garanti BBVA · POS Hesabı, TC Ziraat Bankası · Vadesiz), "Garanti POS" cihazı Garanti hesabına bağlı, KART işçilik 12.000 / 15.000 → tahsilat net 11.760 / 14.700; dekontlar: Garanti GELEN 26.460 "Garanti POS yatan" (G26), Garanti GELEN 11.760 Mehmet Kaya (müşteriye bağlı, G11C), Ziraat GELEN 26.460 "Garanti POS yatan" (hakemin gördüğü ÖZDEŞ satır, Z26), Ziraat GELEN 11.760 Mehmet Kaya (müşteriye bağlı, Z11C).

| Adım | Beklenen | Gerçek |
|---|---|---|
| A `GET /pos/yatma-adaylari?q=` (MÜDÜR) | yalnız G26, hesap adı + kalan | **200** `[G26 'Garanti BBVA · POS Hesabı' '29.09.2026 · Garanti POS yatan +26.460 ₺ · kalan 26.460 ₺']` |
| A2 MUHASEBE `q=26` / A3 `q=Ziraat` / A4 anonim | G26 / boş / 401 | **200** [G26] / **200** [] / **401** |
| B Z26 + t1 | 400 | **400** "Bu banka hesabına bağlı POS cihazı yok; POS yatması yalnız cihazın hesabına gelen parayla eşleşir" |
| C G11C (POS hesabında müşteri havalesi) + t1 | 400 | **400** "Bu banka hareketi bir müşteriyle eşleşmiş; POS yatması olarak kullanılamaz" |
| D Z11C (hakemin olayı: Ziraat müşteri havalesi) + t1 | 400 | **400** aynı ileti |
| E öneri Z26 | 400 | **400** "…bağlı POS cihazı yok…" |
| F öneri G26 | 200, iki tahsilat, uygunCihazlar = [Garanti POS] | **200** `bulundu:true, toplam 26460, uygunCihazlar:[…RDRD9], satirlar: 11760 + 14700` |
| G0 Ziraat hesabına "Ziraat POS" cihazı → G adaylar | Z26 ve G26 AYRI hesap adlarıyla | **200** `[Z26 'Türkiye Cumhuriyeti Ziraat Bankası · Vadesiz' …, G26 'Garanti BBVA · POS Hesabı' …]` |
| G Z26 + t1 (Garanti cihazının tahsilatı) | 400 | **400** "Garanti POS cihazının parası başka bir banka hesabına yatar; seçilen banka hareketi bu tahsilatı kapatamaz" |
| EKRAN (1440 BASİT): Garanti sonucu seç → Öneri getir → t2 işaretini kaldır → Eşleştir | yalnız t1 11.760 | liste 2 satır (Garanti cihazının iki tahsilatı), "Seçilen 11.760 ₺ · bankaya yatan 26.460 ₺", bant **"Eşleştirme tamamlandı · 11.760 ₺ · 1 tahsilat"** (`E1`, `E2`) |
| H2 adaylar `q=Garanti` | G26 kalan 14.700 | **200** G26 "kalan 14.700 ₺" (Z26 de listede: karşı taraf adı "Garanti POS yatan" metin eşleşmesi — hesap adı Ziraat olarak AYRI yazılı) |
| H3 G26 + t2 14.700 → H4 adaylar | 200 → G26 listeden düşer | **200** `eslesenToplam 14700` → yalnız Z26 kaldı |
| I tahsilatlar | ikisi YATTI | `(14700,14700,YATTI), (11760,11760,YATTI)` |

SQL (`r12/sql.out`): `pos_yatma_eslesmeleri` → 2 satır, ikisi de **Garanti BBVA** "Garanti POS yatan" 26460 hareketinde (11760 + 14700, geri_alindi 0); Ziraat hareketlerinde ve müşteri havalelerinde **0 eşleşme**. `banka_hareketleri`: Garanti 26460 ESLESTI (cari yok), Ziraat 26460 BEKLIYOR (dokunulmadı), iki müşteri havalesi ESLESTI + cari bağlı (değişmedi). `denetim_kayitlari`: PosYatmaEslesme EKLEME 4, PosTahsilat GUNCELLEME 2, PosCihaz EKLEME 4.

### 000000000000.4 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum ŞUBESİZ MÜDÜR, taze profil)
Matris (`r12/sen/sahne.mjs` + `kos.sh`, ham `r12/sen/sonuc.jsonl`, özet `r12/sen/ozet.py`): 16 kombinasyon ({1440,1280,960,375} × {BASİT,PRO} × {aydınlık,karanlık}) × 5 sahne (pos-secici-acilis, pos-secici-26, pos-oneri-garanti, hurda-acilis, banka-acilis) = **80 ölçüm**, 16/16 rc=0, mod uyumsuz 0.

| Ölçü | Sonuç (80) |
|---|---|
| yatay kaydırma, K1, K3_tasan, K3_kirik, K5_TRY, K7, K8, K9_binen, K10, K11 | **0** |
| pos-secici-acilis / -26 | 16/16: yazmadan liste gelir; 2 sonuç, ikisi de hesap adıyla ("Türkiye Cumhuriyeti Ziraat Bankası · Vadesiz …", "Garanti BBVA · POS Hesabı …") ve "kalan 26.460 ₺" |
| pos-oneri-garanti | 16/16 elle listede yalnız Garanti cihazının 2 tahsilatı |
| BASİT↔PRO (`tutarlilik.mjs`, /hurda,/pos/tahsilatlar,/banka, 1440) | mod farkı **0**, düğme sınıf grubu 6 |

Görüntüler (Read ile bakıldı): `r12/sen/foto/pos-secici-26_BASIT_aydinlik_1440.png`, `pos-secici-26_PRO_karanlik_375.png`, `E1-garanti-oneri-t1_BASIT_1440.png`; kopyalar `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/ZO-ISLEM-A-r12-*.png` (6 dosya).

### 000000000000.5 Tam süit (paralel şerit kapısı madde 7)
- Koşum 1 (mutasyonlar ve canlı ölçümden önce, `KUYUMCU_TEST_DB=kuyumcu_test_t7`, TRX `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-r12.trx`, günlük `r12/tam.log`): **6919 test, 6918 geçti, 1 başarısız** — `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1; devir listesinde).
- Koşum 2 (denetim için eklenen `Take` sınırlarından sonra; TRX `tam-r12b.trx`, günlük `r12/tam2.log`): **6925 test, 6918 geçti, 7 başarısız**: O8 (ORTAK-1) + **ZO-BELGE şeridine ait 6**: `Dogrulayici.ZOBELGESinirTestleri.ZOBS07_sayim_SIFIR_kabul_edilir_gercek_istek_0_gider`, `ZOBS11_sayim_Turkce_giris_dogru_sayiya_cevrilir` ×4 (koşum 1'de bu 6 test YOKTU: 6919 → 6925, doğrulayıcının yeni sınır testleri) ve `ZoBelgeTestleri.ZB37_Islem_defteri_BASIT_bes_sutun_PRO_on_dort` (Expected "/cikti/satis/h1", Actual "/cikti/SATIS_FISI/h1"; `Pages/IslemDefteri.razor` 03:07'de, `KasaRaporu.razor` 03:16'da ZO-BELGE şeridince değiştirildi — bu paketin dosyası değil, koşum 1'de yeşildi). Bu paketin iki koşum arasındaki tek değişikliği `PosModulu.cs`'deki dört `Take` sınırıdır.
- **FARK (bu paketin yeni kırmızısı) = 0.** `denetim.sh` → **DENETİM TEMİZ**.

### 000000000000.6 DoD sözleşmesi (bu tur)
Yeni varlık YOK; yeni uç salt okuma. `PosYatmaEslesme` davranışı aynı (append-only, geri alma "Eşleşmeyi geri al"). Reddedilen eşleştirme hiçbir satır yazmaz (testlerde `EslesmeSayisi == 0`, canlıda SQL). Her düğme istek atar ya da gerekçeli kilitli (değişmedi); sunucu reddi sayfa bandında Türkçe tek cümle.

### 000000000000.7 Sapmalar ve gerekçeleri
- `Bilesenler/KayitSecici.razor` ortak bileşendir (hiçbir eşzamanlı şeridin listesinde değil): yalnız iki İSTEĞE BAĞLI parametre eklendi; varsayılanlar eski davranışı birebir korur (Faturalar/Uyum/Hurda/GlobalArama çağrıları değişmedi; ilgili testler yeşil). Alternatif (genel `/arama`'ya POS türü eklemek) `Modules/Arama` + `AramaTurleri` sözleşmesini genişletirdi — daha büyük ayak izi.
- "Cihazın hesabı tanımsızsa eşleştirme yok" (fail-closed): hangi hesaba yattığı bilinmeyen tahsilatı herhangi bir hareketle kapatmak hakemin bulduğu hatanın ta kendisidir. Mevcut kiracılarda hesabı boş cihaz varsa POS Cihazları ekranından (mevcut PATCH ucu) hesap seçilmelidir → kurucuya not.
- Mevzuat: yeni hüküm yok.
- Dokunulan sayfalar: `PosTahsilatlar.razor` (paketin sayfası; gerekçe: tasarım hakemi t3 KRİTİK).

### 000000000000.8 Temizlik
API 5170 ve vekil 5171 durduruldu (dinleyen 0); `pgrep -f ZO-ISLEM-A/r12` 0 (Chrome dahil). Kiracı 01M3N7T8P1SCG9EYZKB87MYPZN 70 `tenant_id`'li tablodan `r12/sil.sql` ile silindi (ör. denetim 212, rol_yetkileri 72, banka_hareketleri 4, pos_yatma_eslesmeleri 2, pos_cihazlari 2, kiraci_davetleri 1, tenants 1); yeniden sayım **0**. `tenants`: başlangıç **15** → son **15**; `PAKETZO%` 0. QA-TEST kiracılarına dokunulmadı. Disk: `rm` yasak → `r12/sen/profil-*` ve `r12` çalışma ağacı kurucu/orkestratör tarafından silinmeli.

### 000000000000.9 Değişen dosyalar (bu tur) · açık kalanlar
- `dotnet/Kuyumcu.Api/Modules/Pos/PosModulu.cs` (iki kapı + `YatmaAdaylari` servis/uç + öneri süzgeci) · `dotnet/Kuyumcu.Web/Bilesenler/KayitSecici.razor` (+`AramaYolu`, `Gozat`) · `dotnet/Kuyumcu.Web/Pages/PosTahsilatlar.razor` (seçici kaynağı + elle liste süzgeci) · `dotnet/Kuyumcu.Web/Servisler/PosServisi.cs` (`PosOneri.UygunCihazlar`) · `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoIslem.cs` (+`PosAdayAlt` TR+EN) · `dotnet/Kuyumcu.Tests/ZOISLEMA5Testleri.cs` (yeni, 8 test) · `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (+1, 2 güncelleme) · `dotnet/Kuyumcu.Tests/PosYardim.cs` (fikstür) · `dotnet/Kuyumcu.Tests/PosEkranTestleri.cs` (sahte uç yanıtı). Mevcut id'ler korundu (`pos-eslestir-hareket` ve türevleri aynı).
- Açık kalanlar (sahibiyle): ORTAK-1 `OrtakGorunumSaflik.O8` (devir listesinde) · KATMAN `SatirEylemleri` 375 menü konumu · kurucuya: hesabı boş POS cihazları eşleştirilemez (bilinçli, fail-closed) — POS Cihazları ekranında hesap seçilmeli · `is_calismalari` zamanlanmış iş çakışması (paralel API'ler) → orkestratör/DevOps · § 00000000.8'deki KATMAN/menü grubu maddeleri değişmedi.

---

## 00000000000. 8. DOĞRULAMA — iptal reddi pencerede (ORTA) + tasarım hakemi 4 KRİTİK (Hurda MASAK çıkmazı · tahsil aracı çelişkisi · 12 px tutarlar · PRO geliştirici notu) — 29.09.2026 00:50–02:10 (t7 · API 5170 · vekil 5171 · CDP 9803/9806–9811 · `KUYUMCU_TEST_DB=kuyumcu_test_t7`)
Durum beyanı: **BITTI**. Bu bölüm § 0000000000'ı günceller (geçerli durum budur). Çalışma dizini `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r11/`. Sunucu uçları/gövdeleri, yetki ve yasal kapılar (MASAK, tevsik, gider pusulası) DEĞİŞMEDİ; migration YOK. Devir notu 3'teki POS maddeleri (eşleşme sırası, harf duyarlılığı, öneri/kısmi) § 000000000 ve § 0000000000'da kapanmıştı; bu turda yeniden açılmadı.

### 00000000000.1 Bulgu → düzeltme
| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| [ORTA] İptal onay penceresi sunucu reddini göstermiyordu (bant perdenin arkasında) | `PosTahsilatlar.razor`: pencereye `<HataBandi Id="pos-iptal-hata">`; `IptalUygula` catch'i `iptalHata`'ya yazar, pencere AÇIK kalır; `IptalKapat()` (Vazgeç/×) ret metnini de temizler; yeniden açılışta eski metin yok. Razor özniteliğinde `{ }` lambda yok. | bUnit `ZO29a` · M70, M71 · canlı: pencere açıkken t2 API ile eşleştirildi → "Tahsilatı iptal et" → `#pos-iptal-hata` "Banka yatmasıyla eşleşmiş tahsilat iptal edilemez; önce eşleşmeyi geri alın", pencerede ve en üstte (`elementFromPoint`), sayfa bandı boş; Vazgeç → ikisi de kapandı (`P1-iptal-reddi-pencerede_BASIT_1440.png`) |
| [KRİTİK] Hurda ≥ MASAK eşiği çıkmazı: formda MASAK alanı yoktu, ret ham alan yoluyla (`masak.aciklama`) görünüyordu | Banka dekontunun kalıbı taşındı (AYNI `MasakFormu` bileşeni, AYNI uçlar): eşik `/masak/esikler`'den (istemciye sayı gömülmez; `KimlikEsigi ≤ 0` = "bilinmiyor", karar sunucuda), gereksinim `/masak/gereksinim?tutar=` (tutar/kur değişince yenilenir; `UyumServisi` sayfaya enjekte edilmedi — aynı istemci kodu `new UyumServisi(Api)`, başka şeritlerin bUnit kurulumları bozulmasın diye). Tutar ≥ eşik: alanlar AÇILIŞTA (`#hrd-masak`), Kaydet gerekçeli kilitli (`Dil.T.Banka.MasakKilitSebep` / BASİT `ZO.MasakKilitSebepBasit`), gövdeye `masak` girer (`HurdaSatisIstegi.Masak`, sunucu `HurdaSatisDto.Masak` ile aynı ad). Eşik altında sunucu bağlantılı işlem toplamıyla reddederse (ileti "MASAK"): pencere kapanır, alanlar formda açılır, tek sade cümle `#hrd-masak-not` (`ZO.HurdaMasakGerekli`), gereksinim en az kimlik eşiğiyle sorulur, ham alan yolu basılmaz. Kayıt sonrası kişisel veri formda kalmaz. | bUnit `ZO29c`, `ZO29d` · M75/M75b, M76/M76b, M77/M77b · canlı HTTP (a) ve EKRANDAN ≥ 185.000 ₺ kayıt (§ .4) · SQL `masak_kimlik_tespitleri` 1 satır |
| [KRİTİK] "Tahsil aracı (isteğe bağlı)" + Nakit seçili + "Bu tutar nakit alınamaz" çelişkisi (K18) | Nakit kapalıyken (sunucu tevsik önizlemesi ya da sunucu reddi): alan ZORUNLU (`*`, "isteğe bağlı" yok), Nakit radyosu pasif ve seçili GELMEZ, cümle alanın altında TEK hata (`#hrd-nakit-uyari`, `hata-metni alan-hata`, role=alert), Kaydet gerekçeli kilitli ("Kaydetmek için tahsil aracını seçin."). Sunucu nakdi reddederse (bağlantılı toplamı yalnız sunucu bilir) alan pencere KAPANINCA açılır → ret metni sayfada tek kez. Kayıt bitince form açılış hâline döner (Nakit, "Daha fazla" kapalı). | bUnit `ZO29b`, `ZO28f` (yeniden yazıldı), `ZO28e_hurda_ret_metni_TEK_yerde` (son satırı: bant yerine alan altı), `ZO29f` · M72, M73, M74, M80, M81, M82, M83 · canlı `1 acilis` (§ .4) |
| [KRİTİK] Ana tutarlar 12 px | `zo-islem.css` (kimlik seçicileri app.css `.metin-kucuk`'u ezer; kimlikler aynı): `#hrd-tutar > strong` ve `#pos-elle-toplam` `var(--yazi-rakam)` (20 px) 700; `#hrd-ozet`, fiyat özeti, `#hrd-has-sinir`, `#hrd-tahlil-fark`, bakiye kartı Giren/Çıkan `var(--yazi-govde)` (18 px). Sabit renk yok. | bUnit `ZO29e` · M79 · canlı computed style 96/96 ölçümde `tutarPx 20px/700`, fiyat/sınır/kart 18 px; POS "Seçilen 1.250 ₺ · bankaya yatan 1.250 ₺" 1440 ve 375'te `20px/700` |
| [KRİTİK] PRO Hurda defterinde geliştirici notu ("bu sürümde stornolanmaz… ters kayıt motoru… stok satırı… fark satırı") | Kilit kutusu iki modda AYNI tek cümle (`ZO.HurdaDefterKilitBasit`: değişmez + satırdaki "Tahlil farkını düzelt" + para için düzeltme fişi), alternatif bölüm yok; düzeltme çekmecesi notu `ZO.HurdaDuzeltCekmeceNot` ("Eski kayıt silinmez; girdiğiniz gram farkı kayda eklenir. Tahsil edilen para bu yolla değişmez."). Hub metni (`Hurda.DegismezNotu`) sözlükte durur, ekran çizmez. | bUnit `ZO28_hurda_defter_notu_PRO_modda_da_jargonsuz` (güçlendirildi: 7 yasak ifade + çekmece notu), `P20…DegismezIpucu…` (bkz. § .3) · M78 · canlı 16/16 `jargon []` |
| [DÜŞÜK] Mutasyon geri alımı `cp -p` + artımlı derleme bayat ikili | Bu turun betiği (`r11/mut.sh`) geri almada `cp` (mtime korunmaz) kullanır ve son mutasyondan sonra `dotnet build --no-incremental` koşuldu (0 hata). Kapı kaydına not: mutasyon betiklerinde geri alma sonrası derleme `--no-incremental` olmalı. | `r11/build-son.log` |
| [DÜŞÜK] Ortak `SatirEylemleri` menüsü 375'te satırın yanında açılmıyor | Bu paketin dosyası değil (KATMAN şeridi, `Bilesenler/SatirEylemleri.razor`) — dokunulmadı; açık kalanlara sahibiyle yazıldı. | — |

### 00000000000.2 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo --no-incremental` (son mutasyondan ve son değişiklikten sonra) → **0 Hata, 1 Uyarı** (`EtiketEkranTestleri.cs(145)` xUnit1031 — başka şeridin dosyası). İlk artımsız denemede `Kuyumcu.Tests.deps.json` başka bir şeridin eşzamanlı derlemesince kilitliydi (MSB4018 IOException) — 45 sn sonra yinelendi, yeşil.
- Filtreli: `ZOISLEMA|Q102|P20Ekran|Pos|MasakFormuEkran|ZOS|Sozluk` → **527/527**; son değişiklikten sonra `ZOISLEMA|P20Ekran|Q102` → **218/218**.
- Yeni testler (6): `ZO29a_pos_iptal_sunucu_reddi_PENCEREDE_gorunur_pencere_acik_kalir`, `ZO29b_hurda_nakit_kapali_TAHSIL_ARACI_zorunlu_nakit_secili_degil_kaydet_gerekceli_kilitli`, `ZO29c_hurda_MASAK_esik_ustunde_ALANLAR_acilista_kaydet_kilitli_govdeye_masak_girer`, `ZO29d_hurda_MASAK_reddi_ALANLARI_acar_ham_alan_yolu_basilmaz`, `ZO29e_tutar_ve_ozet_yazilari_GOVDE_boyu_toplam_KALIN`, `ZO29f_hurda_kayit_sonrasi_form_ACILIS_halinde_detay_kapali_nakit`. Güncellenen (gerekçeli): `ZO28_hurda_defter_notu_PRO_modda_da_jargonsuz`, `ZO28f_…` (Kaydet artık gerekçeli kilitli; ret yolu sunucu reddiyle sınanıyor), `ZO28e_hurda_ret_metni_TEK_yerde_…` (pencere kapanınca cümle alan altında), `P20EkranTestleri.Hurda_satisi_icin_DegismezIpucu_cizilir_ve_icinde_dugme_YOKTUR` ("stornolanmaz" geliştirici notu yerine kullanıcı cümlesi — tasarım hakeminin KRİTİK bulgusu bu metni kaldırmayı istedi).
- Mutasyon (`r11/mut.sh`: yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/r11/<kod>-<dosya>` → `perl -0777 -pi` tek eşleşme → derle → `--filter ZOISLEMAEkranTestleri|P20EkranTestleri` → `cp` ile geri → `cmp`; özet `r11/mut/ozet.txt`, günlükler `r11/mut/M*-test.log`):

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M70 | iptal penceresindeki `HataBandi` silindi | `ZO29a_pos_iptal_sunucu_reddi_PENCEREDE_…` | AYNI |
| M71 | iptal catch'i yine sayfa bandına (`hata`) yazar | `ZO29a_…` | AYNI |
| M72 | Nakit `Pasif: false` | `ZO29b_…` | AYNI |
| M73 | tahsil aracı `Zorunlu="false"` | `ZO29b_…` | AYNI |
| M74 | Kilit'ten "tahsil aracını seçin" satırı silindi | `ZO29b_…`, `ZO28f_…` | AYNI |
| M75 / M75b | gövdede `Masak: null` | `ZO29c_…` | AYNI / AYNI |
| M76 / M76b | MASAK reddi dalı `if (false …)` | `ZO29d_…` | AYNI / AYNI |
| M77 / M77b | MASAK eksik alan kilidi hep açık | `ZO29c_…`, `ZO29d_…` | AYNI / AYNI |
| M78 | PRO kilit kutusu eski hub metnine döner | `ZO28_hurda_defter_notu_PRO_…`, `P20…DegismezIpucu…` | AYNI |
| M79 | `#hrd-tutar > strong` gövde boyuna iner | `ZO29e_…` | AYNI |
| M80 | sunucu nakit reddi pencere kapanınca alanı açmaz | `ZO28e_hurda_ret_metni_TEK_yerde_…`, `ZO28f_…` | AYNI |
| M81 | alan altı nakit hatası çizilmez | `ZO29b_…`, `ZO28e_…ret_metni…`, `ZO28f_…`, `ZO28e_hurda_nakit_SINIRI_…` | AYNI |
| M82 | kayıt sonrası tahsil aracı sıfırlanmaz | `ZO29b_…` | AYNI |
| M83 | kayıt sonrası "Daha fazla" kapanmaz | `ZO29f_…` | AYNI |

  (M75b–M77b: `catch` bloklarının P-11 sayacı için yeniden düzenlenmesinden SONRA aynı mutasyonların yinelenmesi.) Hiçbir testi kırmayan mutasyon: 0.
- `bash ./denetim.sh` → **DENETİM TEMİZ**. İlk koşumda `P-11: DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 52 > 47` KIRMIZIYDI (benim yeni `catch` bloklarım: gereksinim/eşik null'a çekiliyor, MASAK reddinde pencere durumu catch içinde sıfırlanıyordu) → gereksinim sorgudan ÖNCE düşürülür, catch boş kalır (fail-closed aynı); pencereyi kapatıp alanları açan iş `MasakAlanlariniAc()` yöntemine alındı (veri düşmüyor, yalnız pencere kapanıyor) → Hurda 7 → 2 (önceki değer), toplam 47. ModalPencere 49/55 (yeni kullanım YOK), duzenlenenId 1/1.
- **Tam süit** — § .6'ya bakın.

### 00000000000.3 Sapmalar ve gerekçeleri
- `Kuyumcu.Contracts/Sayfalar/HurdaSozlesmeleri.cs`: `HurdaSatisIstegi`'ne isteğe bağlı son parametre `object? Masak = null` (Hurda sözleşmesi; sunucu DTO'su zaten `Masak` taşıyordu — uç/gövde şeması DEĞİŞMEDİ, istemci yalnız dolduruyor). `object`: `MasakFormu.MasakBilgi` Web projesinde; System.Text.Json çalışma anı tipiyle serileştirir (bUnit `ZO29c` gövdede `masak.belgeNo/adSoyad/belgeTuru`yu JSON ayrıştırarak sınar; canlıda SQL satırı).
- `P20EkranTestleri.cs` (eski P-20 paketinin Hurda ekran testi; eşzamanlı bir şeridin dosyası değil): "stornolanmaz" iddiası, tasarım hakeminin kaldırılmasını istediği geliştirici notunu kilitliyordu → yeni kullanıcı cümlesini ve "bu sürümde" yokluğunu sınar.
- Sunucu MASAK iletisindeki `(masak.aciklama)` SUNUCUDA değiştirilmedi: `UX4dServisTestleri` bu ifadeyi sözleşme olarak sınıyor (UX-4d md.2) ve `Modules/Masak` bu paketin alanı değil. Ekranda ham yol hiç görünmez: MASAK reddi pencereyi kapatıp sade cümle + alanlar gösterir (canlı `hamYol:false` 96/96 + akış).
- MASAK eşiği için Banka ile aynı istemci kuralı (tek sayı sunucudan, `Tutar ≥ KimlikEsigi`); sıfır/eksik eşik "bilinmiyor" sayılır — başka ekranların bUnit sahte API'si `/masak/esikler`'e `{}`/404 döndüğünde form yanlışlıkla açılmasın ve karar sunucuda kalsın (fail-closed sunucudur).
- Mevzuat: yeni hüküm yazılmadı; MASAK/tevsik eşikleri ve kuralları sunucudaki mevcut yapılandırmadan okunur (`/masak/esikler`, `/masak/gereksinim`, `/tevsik/gunluk`). Mevzuat kaynağı araştırması gerekmedi (hiçbir eşik/oran/kural koda yazılmadı).
- Dokunulan sayfalar (TEK DOKUNUŞ kaydı): `Hurda.razor`, `PosTahsilatlar.razor` — paketin kendi sayfaları; gerekçe: doğrulayıcı ORTA + tasarım hakemi 4 KRİTİK.

### 00000000000.4 Canlı doğrulama — HTTP + EKRAN (5170; kiracı 1 `PAKETZO-ISLEM-A-TEST` 01M3N2NG8145MMTNQJ2FGR5WKQ, kiracı 2 01M3N3G6BG9M5F9WW0BHJYF3PS; sistem anahtarı rastgele, yalnız süreç ortamında, chmod 600; parolalar chmod 600 fikstürde; yanıtlar okundu — `r11/canli.out`, `r11/sen/ekran.log`, `r11/sql.out`)
Kurulum: kasa · 2 cari · banka + 2 GELEN dekont · POS cihazı · KART işçilik 1.500 / 2.750 → tahsilat 1.470 / 2.695 · şubesiz MÜDÜR ile 5 × 10 g 22 ayar hurda alışı (200 ×5 — şube bağlamı hatası YOK) → has bakiye 45,800 g. (İlk alış denemesi 4.580 ₺'de kasa bakiyesi yetersiz 400 verdi — gerçek iş kuralı; fiyat düşürülerek yinelendi.)

| Adım | Beklenen | Gerçek |
|---|---|---|
| a) MÜDÜR `hurda-satis` 29,225 has g × 6.554,38 = 191.551,76 ₺, HAVALE, `masak` YOK | 400 MASAK | **400** "MASAK: 185.000 TL ve üstü işlemde kimlik tespiti zorunludur" |
| b) aynı tutar NAKİT + masak dolu | 400 tevsik | **400** "Tevsik zorunluluğu: … 30.000,00 TL haddini aşıyor…" |
| c) EKRAN (1440 BASİT): has 30 → tutar 196.656 ₺ | MASAK alanları açılışta, tahsil aracı zorunlu, Nakit pasif/seçili değil, Kaydet gerekçeli kilitli | `masak:true, odemeZorunlu:true, istege:false, nakitPasif:true, secili:null, uyari:"Bu tutar nakit alınamaz; Havale ya da Kart seçin.", kilit:"Kaydetmek için tahsil aracını seçin."`, `tutarPx 20px/700`, `hamYol:false` |
| d) Havale seçildi | kilit sebebi MASAK'a geçer | `kilit:"Kaydetmek için kimlik bilgilerini doldurun."` |
| e) T.C. no + ad soyad (gereksinim YALNIZ_KIMLIK: 4 alan) | Kaydet açılır | `kaydetKapali:false, kilit:null` |
| f) Kaydet → onay → Onayla | 200 + sonuç bandı | bant **"Hurda satışı kaydedildi: Altın 30,000 g · 196.656 ₺ · Kalan hurda: 15,800 g"**, MASAK alanları kapandı (kişisel veri formda yok) |
| g) PRO /hurda defter kilit kutusu + çekmece | tek kullanıcı cümlesi | "Kaydedilen hurda satışı değiştirilmez. Alıcıyla gram farkı çıkarsa satırdaki "Tahlil farkını düzelt" ile ekleyin; para farkı için müşteri düzeltme fişini kullanın." · çekmece "Eski kayıt silinmez; …" · jargon `[]` |
| h) POS iptal penceresi açıkken t2 API ile eşleştirildi (200) → "Tahsilatı iptal et" | ret pencere İÇİNDE, pencere açık | `pencereAcik:true, iptalHata:"Banka yatmasıyla eşleşmiş tahsilat iptal edilemez; önce eşleşmeyi geri alın", pencereIcinde:true, bandUstte:true, sayfaBandi:null`; Vazgeç → `pencere:false, hata:false` |
| i) (kiracı 2) "Daha fazla" elle açık + Havale → kayıt (1440 BASİT, 375 PRO karanlık) | form açılış hâline döner | önce `detay:true, secili:HAVALE` → sonra `detay:false, odeme:false` + bant "Hurda satışı kaydedildi: Altın 1,000 g · 6.554,30 ₺ · …" |

SQL (`r11/sql.out`, kiracı 1): `masak_kimlik_tespitleri` → 1 satır: hareket `HURDA_SATIS 196656.00 HAVALE`, `islem_tutari_tl 196656.00`, `belge_turu TC_KIMLIK`, belge_no/ad_soyad dolu (şifreli; değer basılmadı), `uyruk TUR`, `kaynak_tur HAREKET`. `hareketler`: HURDA_ALIS 5 · HURDA_SATIS 1 (196.656) · ISCILIK 2. `pos_tahsilatlari`: 1470 BEKLIYOR · 2695 YATTI (reddedilen iptal hiçbir şey değiştirmedi). `denetim_kayitlari`: EKLEME MasakKimlikTespiti 1 · Hareket 8 · PosYatmaEslesme 2 · PosTahsilat EKLEME 2 / GUNCELLEME 1. API günlüğündeki `is_calismalari … Duplicate entry` hataları başka kiracıların zamanlanmış işleri (aynı veritabanında birden çok şeridin API'si) — bu paketle ilgisiz.

### 00000000000.5 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum ŞUBESİZ MÜDÜR, taze profil)
**Matris** (`r11/sen/sahne3.mjs` + `kos.sh`, özet `r11/sen/ozet.py`, ham `r11/sen/sonuc.jsonl`): 16 kombinasyon ({1440,1280,960,375}×{BASİT,PRO}×{aydınlık,karanlık}) × 6 sahne (hurda-acilis, hurda-masak, hurda-defter, pos-acilis, pos-iptal-onay, banka-acilis) = **96 ölçüm**, 16/16 rc=0, mod uyumsuz 0. Ek: `pos-elle-toplam` (1440 BASİT aydınlık, 375 PRO karanlık), `hurda-havale-once/sonra` (1440 BASİT aydınlık, 375 PRO karanlık).

| Ölçü | Sonuç (96) |
|---|---|
| yatay kaydırma, K1, K3_tasan, K3_kirik, K5_TRY, K7, K8, K10, K11 | **0** |
| K9_binen | 84 — YALNIZ `pos-iptal-onay` (açık pencere) sahnesinde; örnekler "sayfa öğesi ⟂ #modal-…" (ör. `span.secim-ad "Kısmi yattı" ⟂ #modal-… "Tahsilatı iptal et"`) — önceki turlarla aynı yanlış pozitif sınıfı; pencere taşmıyor/kaymıyor (`panelTasar:false, panelKaydir:false`) |
| hurda-acilis / hurda-masak | 32/32 `tutarPx 20px/700`, fiyat/sınır/kart 18 px; hurda-masak 16/16 MASAK açık, tahsil aracı zorunlu, "isteğe bağlı" yok, seçili yok, kilit "Kaydetmek için tahsil aracını seçin.", `hamYol:false` (bu sahnede kalan bakiye 15,8 g < 30 g olduğundan alanın bakiye aşımı hatası da görünür — beklenen) |
| hurda-defter | 16/16 kutu tek cümle, jargon `[]` (BASİT ve PRO aynı) |
| BASİT↔PRO tutarlılık (`tutarlilik.mjs`, /hurda,/pos/tahsilatlar,/banka, 1440) | mod farkı **0**; düğme sınıfları iki modda aynı (`r11/sen/tutarlilik-*-1440.txt`) |

Görüntüler (Read ile bakıldı): `r11/sen/foto/H1-masak-nakit-kilit_BASIT_1440.png`, `H4-sonuc_BASIT_1440.png` (DÜZELTME ÖNCESİ: Havale seçimi "Daha fazla"yı açık tutuyordu → bu görüntü M82/M83 düzeltmelerini doğurdu), `hurda-havale-sonra_BASIT_aydinlik_1440.png` (düzeltme sonrası açılış hâli), `H5-defter-notu_PRO_1440.png`, `P1-iptal-reddi-pencerede_BASIT_1440.png`, `hurda-masak_BASIT_aydinlik_375.png`, `pos-elle-toplam_PRO_karanlik_375.png`. Kopyaları: `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/ZO-ISLEM-A-r11-*.png` (7 dosya).

### 00000000000.6 Tam süit (paralel şerit kapısı madde 7)
- Koşum 1 (`KUYUMCU_TEST_DB=kuyumcu_test_t7`, TRX `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-r11.trx`, günlük `r11/tam.log`): **6872 test, 6871 geçti, 1 başarısız**: `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (Expected 15 / Actual 16 — ORTAK-1; devir listesinde var).
- Koşum 2 (son değişiklik `detayAcik = false` + `ZO29f` sonrası): TRX `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-r11b.trx`, günlük `r11/tam2.log`: **6873 test, 6872 geçti, 1 başarısız** — yine yalnız `OrtakGorunumSaflik.O8` (ORTAK-1). Aynı koşumdan önce `denetim.sh` → **DENETİM TEMİZ** (`r11/denetim2.log`).
- **FARK (bu paketin yeni kırmızısı) = 0.**

### 00000000000.7 DoD sözleşmesi (bu tur)
Yeni varlık yok. `MasakKimlikTespiti` (append-only yükümlülük kaydı) Hurda ekranından da doğar: düzenleme yok, `DegismezIpucu` (`ZO.BankaMasakKilit`) gerekçe + doğru yol yazılı (Banka ile aynı); sunucu `masak.Kaydet` + `DenetimServisi` yazma kaydı (canlı: 1 EKLEME). `HurdaSatisi` değişmez; düzeltme yolu satırdaki "Tahlil farkını düzelt" (çekmece). `PosTahsilat` iptali yıkıcı onay penceresinde, ret metni pencerede; geri dönüş yolu "Eşleşmeyi geri al". Her düğme istek atar ya da disabled + görünür sebep (Kaydet: tahsil aracı / MASAK; iptal: gerekçe n/20).

### 00000000000.8 Temizlik
API (5170) ve vekil (5171) durduruldu, dinleyen 0; `pgrep -f ZO-ISLEM-A/r11` 0 (Chrome dahil). Kiracı 1 ve 2: 70 `tenant_id`'li tablodan `r11/sil.sql`, `r11/sil2.sql` ile silindi (satır sayıları okundu; ör. kiracı 1: denetim 237, rol_yetkileri 72, refresh 27, kur 19, masak_kimlik_tespitleri 1, pos_yatma_eslesmeleri 1, tenants 1), kendi davet satırları (`kiraci_davetleri`, `kullanan_tenant_id`) silindi (1+1); yeniden sayım **0 + 0**. `tenants where ad like 'PAKETZO-ISLEM-A%'` → **0**; hakem kiracısı `01M3JG487JCP11FMBAFRMR4X4P` artık yok (0). Toplam `tenants` başlangıç 18 → son 14: fark başka şeritlerin kendi kiracılarını silmesi (bu paketin net etkisi 0; QA-TEST kiracılarına dokunulmadı). Disk: `rm` yasak → `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r11/sen/profil-*` Chrome profilleri ve `r11` çalışma ağacı kurucu/orkestratör tarafından silinmeli.

### 00000000000.9 Değişen dosyalar (bu tur) · açık kalanlar
- `dotnet/Kuyumcu.Web/Pages/Hurda.razor` (+~145/−15) · `dotnet/Kuyumcu.Web/Pages/PosTahsilatlar.razor` (+13/−3) · `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoIslem.cs` (+3 anahtar TR+EN: `HurdaDuzeltCekmeceNot`, `HurdaTahsilSecinKilit`, `HurdaMasakGerekli`) · `dotnet/Kuyumcu.Web/wwwroot/css/zo-islem.css` (+14; belirteç, sabit renk yok) · `dotnet/Kuyumcu.Contracts/Sayfalar/HurdaSozlesmeleri.cs` (+`Masak`) · `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (+6 test, 3 güncelleme) · `dotnet/Kuyumcu.Tests/P20EkranTestleri.cs` (1 iddia). Mevcut id'ler korundu; yeni id'ler `pos-iptal-hata`, `hrd-masak`, `hrd-masak-not`.
- Açık kalanlar (sahibiyle): ORTAK-1 `OrtakGorunumSaflik.O8` · KATMAN `Bilesenler/SatirEylemleri.razor` 375'te menü konumu (doğrulayıcı DÜŞÜK) · orkestratör kapı kaydı: mutasyon geri alımında `cp -p` yerine `cp` + `--no-incremental` · `is_calismalari` zamanlanmış iş çakışması (aynı veritabanında paralel API'ler; "Duplicate entry") → orkestratör/DevOps · Sunucu MASAK iletisindeki `(masak.aciklama)` ifadesi `UX4dServisTestleri` sözleşmesinde; ekran çevirdiği için bu paketi engellemez, sunucu metni değişecekse sahibi UX-4d/Masak modülü · § 00000000.8'deki KATMAN/ortak bileşen/menü grubu maddeleri değişmedi.

---

## 0000000000. 7. DOĞRULAMA — POS EŞLEŞMESİNİ EKRANDAN GERİ ALMA (KRİTİK) + düğme yeri (DÜŞÜK) + etiketli sonuç (DÜŞÜK) — 28/29.09.2026 23:50–00:40 (t7 · API 5170 · vekil 5171 · CDP 9803/9806–9810 · `KUYUMCU_TEST_DB=kuyumcu_test_t7`)
Durum beyanı: **BITTI**. Bu bölüm § 000000000'ı günceller (geçerli durum budur). Çalışma dizini `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r10/`. Sunucu tarafına DOKUNULMADI (uç `POST /pos/eslesmeler/{id}/geri-al` [RolGerekli SAHIP, MUDUR] ve `GET /pos/tahsilatlar/{id}/eslesmeler` zaten vardı); migration YOK.

### 0000000000.1 Bulgu → düzeltme
| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| [KRİTİK] Yanlış eşleştirme ekrandan düzeltilemiyordu (servis `EslesmeGeriAl` hiç çağrılmıyordu; eşleşmiş satırda İptal kilitli → çıkmaz) | `PosTahsilatlar.razor`: bankaya eşleşmiş (yatan > 0, IPTAL değil) her satırın ortak "⋯" menüsüne **"Eşleşmeyi geri al"** (`#pos-t-geri-al-{id}`). Tıklanınca `GET /pos/tahsilatlar/{id}/eslesmeler` → yalnız ETKİN satırlar; ortak onay penceresi (`ModalPencere`, yıkıcı onay — düzenleme kalıbı değil): tek etkin eşleşmede seçici çizilmez, salt okunur satır (UX-S22, `#pos-geri-al-secim-tek`), birden çoksa radyo (`#pos-geri-al-secim`, öneriyle/elle alt bilgisi); gerekçe ≥ 20 (`#pos-geri-al-gerekce`, alan altı "Gerekçe en az 20 karakter olmalı (n/20)."); kilit sebebi görünür (`#pos-geri-al-kilit`, alan hatasıyla aynı cümle tekrar edilmez); `POST /pos/eslesmeler/{id}/geri-al`; başarı bandı "Eşleşme geri alındı · 500 ₺" (sunucu mesajı + geri alınan tutar, UX-S27); hata bandı `#pos-geri-al-hata` (liste okunamazsa "geri alınacak eşleşme yok" DENMEZ, UX-S28). Yetki: SAHİP/MÜDÜR dışında eylem kilitli + menüde sebep `#pos-t-geri-al-kilit-{id}` (sunucunun `[RolGerekli]`'siyle aynı). İptal kilidi eşleşmiş satırda doğru yolu söyler (`PosIptalKilitEslesmis`: "önce bu menüden Eşleşmeyi geri al'ı kullanın"); `PosKilitNotu` üç yolu tek cümlede söyler. Geri dönüş yolu (DoD EK 4): geri alınan çift aynı ekranda yeniden eşleştirilir (§ 000000000 `cift_sira`). | bUnit `ZO28n_*` (4 test) · canlı HTTP + SQL (§ .4) · ekrandan eşleştir → geri al → yeniden eşleştir (§ .5) |
| [DÜŞÜK] Eşleştir düğmesi elle seçim listesinin ÜSTÜNDEydi | Düğme tek `RenderFragment` (`EslestirDugmesi`), tek yerde çizilir: öneri yokken üstteki ızgarada, öneri/elle liste açıkken listenin ve "Seçilen … · bankaya yatan …" özetinin ALTINDA (`.zo-pos-elle-eylem`, sağa yaslı; ≤ 640 px tam genişlik). Kimlik `pos-eslestir-uygula` DEĞİŞMEDİ, kopya yok. | `ZO28o_*` (DOM sırası pos-elle → pos-elle-toplam → pos-eslestir-uygula) · tarayıcı 16/16 `altta=true`, tek kopya |
| [DÜŞÜK] Başarı bandında etiketsiz sayı ("· 1") | `ZO.PosEslestiSonuc` "{0} · {1} · {2} tahsilat" / EN "{2} payment(s)" | canlı "Eşleştirme tamamlandı · 500 ₺ · 1 tahsilat" · `ZO28o_*` |
| (bu turda bulunan) 375 px'te pencerenin "Eşleşmeyi geri al" düğmesi iki satıra kırılıyordu (K3_kirik 4/16) | `.satir.zo-onay-dugmeler`: yazı `nowrap`; ≤ 480 px düğmeler alt alta tam genişlik (eylem üstte, Vazgeç altta; DOM sırası aynı) | yeniden ölçüm K3_kirik 0 · `ZO28o_*` CSS kuralı |
| (bu turda bulunan, kendi testim) `ZO28e_hurda_ret_metni_*` / `ZO28e_hurda_nakit_*` "459" alt dizesini TÜM işarette arıyordu; rastgele modal kimliği (GUID onaltılık) "459" içerince kırmızı (geniş koşumda 1/460) | "459 s." + "Tebliğ" aranır (mevzuat atfının kendisi) | filtreli 460/460, ekran 56/56 ×3 |
| [DÜŞÜK] Tam süitte başka şeritlerin kırmızıları | Orkestratör kapısına (aşağıda § .7) | — |

### 0000000000.2 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo` (son mutasyondan ve son değişiklikten sonra) → **0 Hata**; tek uyarı `EtiketEkranTestleri.cs(145)` xUnit1031 (başka şeridin dosyası; artımlı derlemede görünüp kayboluyor — § 000000000'da da kayıtlı).
- Filtreli: `ZOISLEMAEkranTestleri` → **56/56** (üç ardışık koşum). Geniş: `ZOISLEMA|MasakFormuEkran|ZOS4|ZOS_pos|Sozluk|Pos` → **460/460** (TRX `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/filtre-r10.trx`).
- Yeni testler (5): `ZOISLEMAEkranTestleri.ZO28n_pos_eslesmis_satirda_GERI_AL_menude_gerekceli_onay_POSTa_girer`, `ZO28n_pos_iki_etkin_eslesmede_SECIM_zorunlu_secilen_POSTa_girer`, `ZO28n_pos_MUHASEBE_geri_al_KILITLI_sebep_yazili`, `ZO28n_pos_eslesme_listesi_HATASI_bos_durum_degil`, `ZO28o_pos_eslestir_dugmesi_LISTENIN_ALTINDA_sonuc_bandi_ETIKETLI`. Sahte API: `/pos/tahsilatlar/t3/eslesmeler` gerçekçi gövde (etkin + geri alınmış satır), `/geri-al` gerçek yanıt biçimi; 500 yanıtı ayrı test.
- Mutasyon (betik `r10/mut.sh`: yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/r10/PosTahsilatlar.razor` → `perl -0777 -pi` tek eşleşme → derle → `--filter ZOISLEMAEkranTestleri` → geri kopyala → `cmp`; günlükler `r10/M6x-test.log`):

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M60 | `GeriAlinabilirMi` → `false` (eylem hiç çizilmez) | `ZO28n_pos_eslesmis_satirda_GERI_AL_…`, `ZO28n_pos_iki_etkin_…`, `ZO28n_pos_eslesme_listesi_HATASI_…`, `ZO28n_pos_MUHASEBE_…` | AYNI |
| M61 | yetki `SAHIP or MUDUR` → `+ MUHASEBE` | `ZO28n_pos_MUHASEBE_geri_al_KILITLI_sebep_yazili` | AYNI |
| M62 | etkin süzgeci `.Where(x => !x.GeriAlindi)` kaldırıldı | `ZO28n_pos_eslesmis_satirda_GERI_AL_…` | AYNI |
| M63 | gerekçe kilidi `< 20` → `< 0` | `ZO28n_pos_eslesmis_satirda_GERI_AL_…` | AYNI |
| M64 | sonuç bandı etiketsiz eski biçime | `ZO28o_pos_eslestir_dugmesi_LISTENIN_ALTINDA_sonuc_bandi_ETIKETLI` | AYNI |
| M65 | üstteki düğme her zaman çizilir (`@if (true)`) | `ZO28o_…` | AYNI |
| M66 | iptal kilit sebebi eşleşmiş satırda eski metin | `ZO28n_pos_eslesmis_satirda_GERI_AL_…` | AYNI |
| M67 | kilit satırı kısa gerekçede de çizilir (tekrar) | `ZO28n_pos_eslesmis_satirda_GERI_AL_…` | AYNI |

  M50–M59 geçerli (ilgili satırlar değişmedi).
- **Tam süit** (`KUYUMCU_TEST_DB=kuyumcu_test_t7`, son değişiklikten sonra; TRX `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-r10b.trx`, günlük `r10/tam2.log`): **6842 test, 6839 geçti, 3 başarısız**: `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1; devir listesinde var) · `RaporEkranTestleri.IslemDefteri_acilista_sunucuya_limitli_istek_atar` ve `RaporEkranTestleri.IslemDefteri_sayfa_ileri_geri_ofseti_gunceller` ("limit=10" bulunamadı; `RaporEkranTestleri.cs` koşum sırasında 00:24'te başka şerit tarafından değiştirildi; IslemDefteri* ZO-BELGE alanı). Önceki tam koşum (`r10/tam.log`, iki küçük UI düzeltmesinden önce): 6825/6824, yalnız O8. **FARK (bu paketin yeni kırmızısı) = 0** — değişen dosyalarım yalnız PosTahsilatlar/ZoIslem sözlüğü/zo-islem.css/ZOISLEMA testleri.
- `bash ./denetim.sh` → **DENETİM TEMİZ** (ModalPencere 49/55 — yeni kullanım YIKICI ONAY [geri alma], düzenleme modalı değil; duzenlenenId 1/1; P-11 veri düşüren catch 47/47: ilk sürümde `catch { …; etkinEslesmeler = []; }` sayacı 48 yaptı, liste try öncesi boşaltılacak biçimde düzeltildi).

### 0000000000.3 DoD sözleşmesi (bu tur)
Yeni varlık yok. `PosYatmaEslesme` (append-only, finansal): düzenleme/silme YOK; düzeltme yolu artık EKRANDA: "⋯ → Eşleşmeyi geri al" (gerekçe ≥ 20, SAHİP/MÜDÜR, sunucu `DenetimServisi.YazmaKaydet` çağırır — canlıda 2 geri alma = 2 GUNCELLEME + 2 yazma kaydı) → aynı ekranda yeniden eşleştirme (geri dönüş yolu). `PosTahsilat` salt okunur (satıştan türer): `DegismezIpucu` üç düzeltme yolunu yazar; iptal eşleşmiş satırda gerekçeli kilitli. Her düğme istek atar ya da disabled + görünür sebep taşır (bUnit ile).

### 0000000000.4 Canlı doğrulama — HTTP (5170; kiracı `PAKETZO-ISLEM-A-TEST` 01M3MXDVJ0YPJE7WZF8K6AYJSX; sistem anahtarı rastgele, yalnız süreç ortamında, chmod 600; parolalar chmod 600 fikstürde; yanıtlar okundu — `r10/senaryo.out`)
Kurulum: kasa · cari · banka · GELEN dekont 3.000 / 500 / 1.250 · POS cihazı · KART işçilik 1.500 ve 2.750 → tahsilat net 1.470 ve 2.695 BEKLIYOR.

| Adım | Beklenen | Gerçek |
|---|---|---|
| a) MÜDÜR eşleştir t1 1.000 | 200 | **200** "Eşleştirme tamamlandı", satirSayisi 1 |
| b) MUHASEBE `GET …/eslesmeler` | 200, 1 etkin | **200** `[(1000.0, False)]` |
| c) MUHASEBE geri-al | 403 | **403** "Bu işlem için gerekli rol: SAHIP veya MUDUR" |
| d) MÜDÜR geri-al gerekçe "kisa" | 400 | **400** "Gerekçe en az 20 karakter olmalıdır" |
| e) MÜDÜR geri-al | 200 | **200** "Eşleşme geri alındı" → t1 `yatan 0 · BEKLIYOR` |
| f) aynı eşleşmeyi tekrar geri-al | 400 | **400** "Eşleşme zaten geri alınmış" |
| g) aynı çift yeniden 1.470 (dönüş yolu) | 200 | **200** → t1 `1470 · 1470 · YATTI` |

SQL (`r10/sql.out`): `pos_yatma_eslesmeleri` → t1: `sira 1 · 1000 · geri_alindi 1 · gerekçe dolu` / `sira 2 · 1470 · 0`; t2: `sira 1 · 500 · 1 · "Yanlis banka hareketiyle eslestirildi, ekrand…"` (EKRANDAN yazılan gerekçe) / `sira 2 · 500 · 0` → iz silinmedi. `pos_tahsilatlari`: 2695/500 KISMI · 1470/1470 YATTI. `banka_hareketleri`: 3000 ESLESTI · 500 ESLESTI · 1250 BEKLIYOR. `denetim_kayitlari` (PosYatmaEslesme): EKLEME 10 (4 eşleşme satırı + 4 eşleştirme yazma kaydı + 2 geri alma yazma kaydı) · GUNCELLEME 2 (`GeriAlindi, GeriAlmaGerekcesi`). API günlüğünde `pos_yatma` için `Duplicate entry` **0**.

### 0000000000.5 Ekrandan akış + gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum ŞUBESİZ MÜDÜR, taze profil)
**Akış** (`r10/sen/ekran-geri-al.mjs`, 1440 PRO; günlük `r10/sen/ekran.log`):
1. "Yapi" → Yapi Kredi POS yatan 500 seçildi → Öneri getir → t2 işaretlendi → sıra (px): liste 730 · özet 828 · **düğme 857** (altta, tek kopya, açık) → Eşleştir → bant **"Eşleştirme tamamlandı · 500 ₺ · 1 tahsilat"**.
2. Defter → t2 "⋯" → menü: `Eşleşmeyi geri al` (açık) · `Tahsilatı iptal et` (kilitli) + "Bankaya eşleşmiş tahsilat iptal edilemez; önce bu menüden Eşleşmeyi geri al'ı kullanın."
3. Eşleşmeyi geri al → pencere: tek satır "28.09.2026 · 500 ₺" (seçici yok), kilit "(0/20)"; "kisa" → alan altı "(4/20)", düğme kilitli; geçerli gerekçe → açık → Onayla → bant **"Eşleşme geri alındı · 500 ₺"**, t2 BEKLIYOR, valör YATAN 1.970 → 1.470.
4. Dönüş yolu: aynı hareket + t2 yeniden eşleştirildi → bant "Eşleştirme tamamlandı · 500 ₺ · 1 tahsilat".
5. t1 (1 geri alınmış + 1 etkin): pencere yalnız etkini gösterir ("28.09.2026 · 1.470 ₺"); Vazgeç → kapandı, istek yok.

Görüntüler (Read ile bakıldı): `r10/sen/foto/A-elle-dugme-altta_PRO_1440.png`, `A-eslestir-sonra_PRO_1440.png`, `B-menu-geri-al_PRO_1440.png`, `C-geri-al-kisa-gerekce_PRO_1440.png` (bu görüntü "aynı cümle iki kez" kusurunu gösterdi → düzeltildi), `C-geri-al-onay_PRO_1440.png`, `D-geri-al-sonra_PRO_1440.png`, `E-yeniden-eslestir_PRO_1440.png`.

**Matris** (`r10/sen/sahne2.mjs` + `kos.sh`, özet `r10/sen/ozet.txt`, ham `r10/sen/sonuc.jsonl`): 16 kombinasyon ({1440,1280,960,375}×{BASİT,PRO}×{aydınlık,karanlık}) × 6 sahne (hurda-dolu, hurda-nakit-siniri, pos-acilis, pos-elle-dugme-altta, pos-geri-al-menu, pos-geri-al-onay) = **96 ölçüm**, 16/16 rc=0 (ilk koşumda PRO-karanlık-1280 `kuyumcuTema` yüklenmeden düştü, ölçüm yazmamıştı; tek başına yeniden koşuldu rc=0), mod uyumsuz 0. Ölçüm son derlemeyle (düğme istifi + tekrar düzeltmesi) YENİDEN BAŞLATILAN API'de alındı.

| Ölçü | Sonuç (96) |
|---|---|
| yatay kaydırma, K1, K3_tasan, **K3_kirik** (ilk koşum 4 → düzeltme sonrası 0), K5_TRY, K7, K8, K9b, K10, K10b, K11, K12, K13 | **0** |
| K9_binen | 180 — yalnız açık katman sahneleri (pos-geri-al-menu 16/16, pos-geri-al-onay 16/16): örneklerin tamamı "sayfa öğesi ⟂ pencere/menü öğesi" (ör. `td "28.09.2026" ⟂ #pos-t-geri-al-…`, `h3 "Valör Takvimi" ⟂ #modal-…`); pencere içi çakışma YOK — § 000000000 ile aynı yanlış pozitif sınıfı |
| pos-elle-dugme-altta | 16/16 düğme özetin altında, tek kopya, açık (375'te tam genişlik 317 px) |
| pos-geri-al-menu | 16/16 menü açık, eylem açık ve görünür alanda |
| pos-geri-al-onay | 16/16 tek satır, alan altı hata, tekrar yok, onay kilitli, Vazgeç solda (375'te altta), pencere taşmaz/kaymaz |

Görüntüler (Read ile bakıldı): `pos-geri-al-onay_BASIT_karanlik_375.png` (düğmeler alt alta, kırılma yok), `pos-elle-dugme-altta_BASIT_aydinlik_375.png`, `pos-geri-al-menu_PRO_aydinlik_1280.png`.

### 0000000000.6 Temizlik
API (5170) ve vekil (5171) durduruldu, dinleyen 0; `pgrep -f ZO-ISLEM-A/r10` 0 (Chrome dahil). Kiracı `01M3MXDVJ0YPJE7WZF8K6AYJSX`: 70 `tenant_id`'li tablodan `r10/sil.sql` ile silindi (satırlar okundu: denetim 293, rol_yetkileri 72, kur 46, refresh 42, … pos_yatma_eslesmeleri 4, tenants 1); yeniden sayım **0**. Kendi davet satırım (`01M3MXDVBENHX71FFBFN3NXB6F`) silindi (ROW_COUNT 1). `tenants where ad like 'PAKETZO-ISLEM-A%'` → **0**. Toplam `tenants` başlangıç 17 → son 18: fark başka şeritlerin bu sırada açtığı kiracılar (`PAKETZO-BELGE-TEST Kuyumculuk` 21:19, `PAKETSATIS-3-TEST Dogrulayici` 21:13 — dokunulmadı); bu paketin net etkisi 0. QA-TEST kiracılarına dokunulmadı. Disk: `rm` yasak → `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r10/sen/profil-*` ve önceki turların profilleri kurucu/orkestratör tarafından silinmeli.

### 0000000000.7 Değişen dosyalar (bu tur) · açık kalanlar
- `dotnet/Kuyumcu.Web/Pages/PosTahsilatlar.razor` (+165/−15; `@inject OturumServisi`; geri alma eylemi + penceresi; düğme yeri; etiketli sonuç) · `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoIslem.cs` (+13 anahtar TR+EN; `PosKilitNotu` metni) · `dotnet/Kuyumcu.Web/wwwroot/css/zo-islem.css` (+10: `.zo-pos-elle-eylem`, `.zo-onay-dugmeler`; sabit renk yok) · `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (+5 test, 2 kararsız iddia sağlamlaştırıldı). Dokunulan sayfa: `PosTahsilatlar.razor` (paketin kendi sayfası; gerekçe: doğrulayıcı KRİTİK + 2 DÜŞÜK). Sunucu/uç/gövde/yetki DEĞİŞMEDİ; mevcut id'ler korundu, yeni id'ler `pos-geri-al-*`, `pos-t-geri-al-*`.
- Açık kalanlar (sahibiyle): ORTAK-1 `OrtakGorunumSaflik.O8` · ZO-BELGE `RaporEkranTestleri.IslemDefteri_acilista_sunucuya_limitli_istek_atar` + `IslemDefteri_sayfa_ileri_geri_ofseti_gunceller` (test dosyası koşum sırasında değişti) · SATIS-3 `T10_Hurda_had_ustu_NAKITTE` (önceki turda yük altında zaman aşımı; bu koşumda yeşil) · `kiraci-temizle.sh` zsh koruması (§ 000000000.10) · kiracı sayısı kapı ölçüsü öneki/kimliğiyle (paralel şeritler toplamı oynatıyor) · § 00000000.8'deki KATMAN/ortak bileşen/menü grubu maddeleri değişmedi.

---

## 000000000. 6. DOĞRULAMA — POS ELLE EŞLEŞTİRME ÇIKMAZI (KRİTİK) + harf duyarlılığı (ORTA) + öneri/ekran kısmi eşleştirme (DÜŞÜK) — 28.09.2026 22:55–23:40 (t7 · API 5170 · vekil 5171 · CDP 9803/9806–9809 · `KUYUMCU_TEST_DB=kuyumcu_test_t7`)
Durum beyanı: **BITTI**. Bu bölüm § 00000000'ı günceller (geçerli durum budur). Çalışma dizini `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r9/`.

### 000000000.1 Karar (Bertan/Taşkın gözü) — neden "eşleşme sırası"
Kök neden (doğrulayıcının ölçtüğü): `pos_yatma_eslesmeleri` üzerindeki tekil indeks `(tenant_id, pos_tahsilat_id, banka_hareket_id)` geri alınmış satırı da sayıyordu → aynı çift ne geri almadan sonra ne de ikinci kısmi tutarla eşleşebiliyordu (409 "Bu kayıt zaten var").
Değerlendirilen üç yol:
1. **Tekillik yalnız etkin satıra** — MySQL'de kısmi (filtreli) indeks yok; üretilmiş kolon hilesi gerekir ve yine de *aynı çiftin ikinci kısmi eşleşmesini* kilitler. Tek başına yetmez.
2. **Kısmi eşleştirme mevcut satırı büyütsün** — ADR-0004 (eşleşme append-only: silinmez, geri alınır) ile çelişir: satırın tutarı değişir, geri alma iki olayı birlikte geri alır, denetim izi "hangi tutar ne zaman" sorusunu kaybeder. Ayrıca geri alınan çiftin yeniden eşleşmesini çözmez.
3. **SEÇİLEN: her eşleştirme ayrı bir olay satırı + çift içi sıra (`cift_sira`)**. Tekil indeks `(tenant_id, pos_tahsilat_id, banka_hareket_id, cift_sira)` oldu. Sıra = o çiftin TÜM satırları (geri alınmışlar dahil) + 1. Böylece geri alınan satır iz olarak kalır, aynı çift yeniden ve kısmi ek tutarla eşleşir, her olay tek tek geri alınabilir; **yarış koruması korunur**: aynı çifte aynı anda gelen iki istek aynı sırayı hesaplar, veritabanı ikincisini reddeder (eski indeksin verdiği koruma birebir). Net/hareket aşımı zaten serviste toplamla denetleniyor (değişmedi).

### 000000000.2 Bulgu → düzeltme
| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| [KRİTİK] Elle eşleştirme çıkmazı (geri alınan çift / ikinci kısmi → 409) | `Entities.cs` `PosYatmaEslesme.CiftSira` (`cift_sira` int, varsayılan 1) · `KuyumcuDbContext.cs` tekil indeks sırayı içerir · `PosModulu.Eslestir` sırayı `CountAsync(tenant, tahsilat, hareket)+1` ile yazar · migration `20260928195825_ZOIslemPosEslesmeCiftSira` | Canlı ve SQL aşağıda; `ZO28j`, `ZO28k`; doğrulayıcının `ZOS4_geri_alinan_cift_yeniden_eslestirilebilir` ve `ZOS4_ayni_cift_ikinci_kismi_istek_500_uretmez` testleri önce KIRMIZI (r9/once.log: 3 başarısız / 18), şimdi yeşil |
| [ORTA] `GroupBy(PosTahsilatId)` harf duyarlı, MySQL duyarsız → `{t, t.lower()}` 409 | `GroupBy(s => s.PosTahsilatId, StringComparer.OrdinalIgnoreCase)` | Doğrulayıcının `ZOS4_ayni_tahsilat_buyuk_kucuk_harf_farkli_kimlikle_400` testi (DEĞİŞTİRİLMEDİ) önce kırmızı, şimdi yeşil; canlı g) → 400 |
| [DÜŞÜK] `OtoEslestirOnerisi` kısmi eşleşmiş harekette `hareket.Tutar` kullanıyordu | küme `hareketKalan`a eşitlenir (`hareketKalan > 0m && Math.Abs(toplam - hareketKalan) <= tolerans`) | `ZO28l_oneri_kismi_eslesmis_harekette_KALANA_esitlenen_kumeyi_bulur` |
| [DÜŞÜK] `PosTahsilatlar.razor` `ElleSec` yalnız tam kalanla seçiyordu | hareketin boştaki kalanı satırın kalanından küçükse satır o kadar (kısmi) seçilir; satır altına `#pos-elle-kismi-{id}` açıklaması (Sozlukler.ZoIslem `PosElleKismi` TR+EN). Hareket kalanı 0 ise eski davranış (tam kalan + "fazla" kilidi) | `ZO28m_pos_elle_hareket_kalani_KUCUKSE_kismi_tutarla_eslestirir` (yeni) · `ZO28e_pos_elle_secim_FAZLAYSA_kilitli_sebep_yazili` fikstürü hareketKalan 500→0 (kalan>0'da artık kısmi seçim olduğu için "fazla" durumu kalan 0'da sınanır; gerekçe testin yorumunda) · tarayıcı: 16/16 kombinasyonda kısmi cümle, ekrandan POST |
| [DÜŞÜK] Rapor "500" diyordu, gerçek 409 | § 00000000 başlığı ve satırı + `PosModulu.cs` yorumu düzeltildi | — |

### 000000000.3 Migration
`20260928195825_ZOIslemPosEslesmeCiftSira` — `dotnet build` yeşil sonrası `dotnet ef migrations add` (build'li; `--no-build` kullanılmadı). Up(): eski tekil indeksi düşür → `cift_sira int NOT NULL DEFAULT 1` ekle (mevcut satırlar kendi çiftinin ilk olayıdır; eski indeks çift başına tek satıra izin veriyordu) → `(tenant_id, pos_tahsilat_id, banka_hareket_id, cift_sira)` tekil indeks. `.cs` 1,8 KB, `.Designer.cs` 236 KB (0 bayt değil; snapshot `cift_sira` içeriyor). `kuyumcu_net`'e uygulandı: `__EFMigrationsHistory` son satır `20260928195825_ZOIslemPosEslesmeCiftSira`; `show index` → tekil indeks 4 kolon. Not: Down() eski 3 kolonlu tekil indeksi geri kurar — aynı çiftte birden çok olay satırı oluşmuşsa Down başarısız olur (append-only veri silinmeden geri dönülemez; bilinçli).

### 000000000.4 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo` (son mutasyondan ve son yorum düzeltmesinden sonra) → **0 Hata, 0 Uyarı** (`r9/build-son.log`). (Ara derlemelerde görülen tek uyarı `EtiketEkranTestleri.cs(145)` xUnit1031 — başka şeridin dosyası.)
- Filtreli: `--filter 'FullyQualifiedName~ZOISLEMA|FullyQualifiedName~MasakFormuEkran|FullyQualifiedName~ZOS4|FullyQualifiedName~ZOS_pos'` → **149/149** (TRX `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/filtre-r9.trx`). Geniş POS/şema kapsamı `Pos|Eslest|Migration|Sema|Indeks|Sozluk` → **413/413**.
- Yeni testler (4): `ZOISLEMA4PosTestleri.ZO28j_geri_alinan_cift_yeniden_ve_ikinci_kismi_tutarla_eslesir_iz_kalir`, `ZO28k_ayni_cift_ayni_sira_iki_kez_yazilamaz`, `ZO28l_oneri_kismi_eslesmis_harekette_KALANA_esitlenen_kumeyi_bulur`, `ZOISLEMAEkranTestleri.ZO28m_pos_elle_hareket_kalani_KUCUKSE_kismi_tutarla_eslestirir`.
- Mutasyon (yedekler `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/r9/`, betik `r9/mut.sh`, günlükler `r9/M5x-test.log`):

| Kod | Değişiklik | Kırmızıya dönen test(ler) | cmp |
|---|---|---|---|
| M56 | sıra sayımına `&& !e.GeriAlindi` eklendi (geri alınanı saymaz) | `ZO28j_geri_alinan_cift_yeniden_ve_ikinci_kismi_tutarla_eslesir_iz_kalir`, `ZOS4_geri_alinan_cift_yeniden_eslestirilebilir` | AYNI |
| M57 | `GroupBy(…, StringComparer.OrdinalIgnoreCase)` → `GroupBy(s => s.PosTahsilatId)` | `ZOS4_ayni_tahsilat_buyuk_kucuk_harf_farkli_kimlikle_400` | AYNI |
| M58 | öneri `Math.Abs(toplam - hareketKalan)` → `Math.Abs(toplam - hareket.Tutar)` | `ZO28l_oneri_kismi_eslesmis_harekette_KALANA_esitlenen_kumeyi_bulur` | AYNI |
| M59 | `ElleSec` kısmi seçim → `secilenler[t.Id] = t.KalanTutar;` | `ZO28m_pos_elle_hareket_kalani_KUCUKSE_kismi_tutarla_eslestirir` | AYNI |

  M50–M55 geçerli (dosyalarının ilgili satırları değişmedi).
- **Tam süit** (`KUYUMCU_TEST_DB=kuyumcu_test_t7`, TRX `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-r9.trx`, günlük `r9/tam.log`): **6792 test, 6791 geçti, 1 başarısız**: `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1; devir listesinde de var). **FARK (bu paketin yeni kırmızısı) = 0.**
- `bash ./denetim.sh` → **DENETİM TEMİZ** (ModalPencere 48/55, duzenlenenId 1/1).

### 000000000.5 Canlı doğrulama (5170; kiracı 1 `PAKETZO-ISLEM-A-TEST` 01M3MT07EG15ECPNMYJPCPADVC; sistem anahtarı rastgele, yalnız süreç ortamında, chmod 600; yanıtlar okundu — `r9/devam.out`, `r9/sql.out`)
Kurulum: kasa 200 · cari 201 · banka 200 · dekont GELEN 3.000 200 · POS cihazı 200 · KART işçilik 1.500 200 → tahsilat net 1.470 BEKLIYOR. Tüm eşleştirmeler şubesiz MÜDÜR ile:

| Adım | Beklenen | Gerçek |
|---|---|---|
| a) eşleştir 1.000 | 200 | **200** "Eşleştirme tamamlandı" |
| b) AYNI ÇİFT ikinci kısmi 470 | 200 (eskiden 409) | **200** |
| c) `eslesmeler/{id}/geri-al` (1.000) | 200 | **200** "Eşleşme geri alındı" |
| d) AYNI ÇİFT yeniden 1.000 | 200 (eskiden 409) | **200** |
| e) kalan iki etkin satırı geri al | 200 ×2 | **200 ×2** |
| f) AYNI ÇİFT 1.470 (doğrulayıcının 409 aldığı istek) | 200 | **200** |
| g) `{t:1},{t.lower():1}` | 400 | **400** "Aynı tahsilat bir eşleştirmede yalnız bir kez yer alabilir…" |
| h) net aşımı +1 | 400 | **400** "Eşleşen tutar tahsilatın net tutarını aşamaz (net 1470,00, yatan 1470,00)" |

SQL (`pos_yatma_eslesmeleri`, kiracı 1): `cift_sira 1 · 1000.00 · geri_alindi 1 · gerekçe dolu` / `2 · 470.00 · 1` / `3 · 1000.00 · 1` / `4 · 1470.00 · 0` → **iz silinmedi, dört olay, bir etkin**. `pos_tahsilatlari` → `1470.00 / 1470.00 YATTI`; `banka_hareketleri` → `3000.00 ESLESTI`. `denetim_kayitlari` (varlik_turu `PosYatmaEslesme`): 4 eşleştirme + 3 geri alma kaydı (EKLEME + GUNCELLEME `GeriAlindi, GeriAlmaGerekcesi`). API günlüğünde `pos_yatma` için `Duplicate entry` **0**.

### 000000000.6 Ekrandan eşleştirme + gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum ŞUBESİZ MÜDÜR, kiracı 2 01M3MT55N18RQMSQHQDFBAWDC5, dolu veri, taze profil)
- Veri: r8 senaryosunun aynısı + ek GELEN dekont 500 ₺ "Yapi Kredi POS yatan" (tahsilat kalanları 2.695 ve 1.470 ₺'den küçük → kısmi durum).
- **Ekrandan POST** (`r9/sen/ekran-eslestir.mjs`, 1440 PRO): hareket "Kredi" → seç → Öneri getir (küme yok) → ilk tahsilatı işaretle → satır altı "Bu tahsilattan 500 ₺ eşleşecek (bankaya yatanın kalanı kadar); geri kalanı açık kalır." · "Seçilen 500 ₺ · bankaya yatan 500 ₺" · düğme açık → **Banka yatmasıyla eşleştir** → başarı bandı "Eşleştirme tamamlandı · 500 ₺ · 1"; Valör takvimi YATAN 0 → 500 ₺, KALAN 4.165 → 3.665 ₺. SQL (`r9/sql-ekran.out`): `cift_sira 1 · 500.00 · geri_alindi 0 · net 2695.00 · yatan 500.00 · KISMI · hareket 500.00 ESLESTI`.
- Matris (`r9/sen/sonuc.jsonl`, özet `r9/sen/ozet.txt`): 16 kombinasyon ({1440,1280,960,375}×{BASİT,PRO}×{aydınlık,karanlık}) × 14 sahne (13 eski + yeni `pos-elle-kismi`) = **224 ölçüm**; 16/16 rc=0 (ilk koşumda PRO-aydınlık-1440 `kuyumcuTema` yüklenmeden, PRO-aydınlık-1280 giriş hız sınırı (GIRIS-YOK) ile düştü; ikisi tek tek yeniden koşuldu, rc=0; düşen koşumlar ölçüm yazmamıştı), mod uyumsuz 0.

| Ölçü | Sonuç (224) |
|---|---|
| yatay kaydırma, K1, K3_tasan, K3_kirik, K5_TRY, K7, K8, K9b, K10, K10b, K11, K12, K13 | **0** |
| K9_binen | 466 — yalnız açık pencere sahneleri (pos-iptal-onay, banka-dekont-masak/pasaport/diger-kod, banka-yeni-hesap; her biri 16/16): örtü altı sayfa ⟂ pencere yanlış pozitifi, § 00000000 ile aynı sınıf |
| pos-elle-kismi | 16/16: kısmi cümle 1, "Seçilen 500 ₺ · bankaya yatan 500 ₺", düğme açık, kilit metni yok |

Görüntüler (Read ile bakıldı): `r9/sen/foto/ekran-eslestir-once_PRO_1440.png`, `ekran-eslestir-sonra_PRO_1440.png` (başarı bandı + valör 500/3.665), `pos-elle-kismi_BASIT_aydinlik_1440.png`, `pos-elle-kismi_BASIT_karanlik_375.png` (tek sütun, taşma yok).

### 000000000.7 Temizlik
API (5170) ve vekil (5171) durduruldu, dinleyen 0; `pgrep -f ZO-ISLEM-A/r9` 0 (Chrome dahil). Kiracılar: kendi iki kiracım + orkestratörün devrettiği hakem kiracısı `01M3JG487JCP11FMBAFRMR4X4P` (PAKETZO-ISLEM-A-HAKEM-TEST). **Olay:** `kiraci-temizle.sh` zsh betiği yanlışlıkla `bash` ile çağrıldı → `${=TABLOLAR}: bad substitution`, betik yalnız `tenants` satırlarını sildi, 27 tabloda 3.102 yetim satır kaldı (70 tablo taramasıyla ölçüldü). Aynı üç kimlik için 69 tablodan `delete … where tenant_id in (…)` SQL dosyasıyla (`r9/sil.sql`, satır sayıları okundu: toplam 3.102) silindi; yeniden tarama **0**. Paketin 4 davet satırı (`01M3MT07CD28…`, `01M3MT55MDAC…`, hakem `ZOHAKEM-aaf3618bca81`, yetim `01M3MFSC04J9…` "PAKETZO-ISLEM-A-TEST Hakem2") silindi (ROW_COUNT 4). `tenants where ad like 'PAKETZO-ISLEM-A%'` → **0**; toplam `tenants` **17** (başlangıç 18 − hakem kiracısı 1; kendi iki kiracım açılıp silindi). QA-TEST ve başka şeritlerin kiracılarına dokunulmadı.
Disk: `rm` hook'la yasak → kendi Chrome profillerim (`r9/sen/profil-*`, `r8/sen/profil-*` vb.) ve `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/` kurucu/orkestratör tarafından silinmeli (ben silemem).

### 000000000.8 Değişen dosyalar (bu tur)
`dotnet/Kuyumcu.Api/Data/Entities.cs` (CiftSira) · `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` (tekil indeks) · `dotnet/Kuyumcu.Api/Data/Migrations/20260928195825_ZOIslemPosEslesmeCiftSira.cs` + `.Designer.cs` (yeni) + `KuyumcuDbContextModelSnapshot.cs` · `dotnet/Kuyumcu.Api/Modules/Pos/PosModulu.cs` · `dotnet/Kuyumcu.Web/Pages/PosTahsilatlar.razor` · `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoIslem.cs` (`PosElleKismi`) · `dotnet/Kuyumcu.Tests/ZOISLEMA4Testleri.cs` (+3) · `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (+1, 1 fikstür). Doğrulayıcı testi `Dogrulayici/ZOISLEMASinirTestleri.cs` DEĞİŞTİRİLMEDİ. Dokunulan sayfa: `PosTahsilatlar.razor` (bu paketin kendi sayfası; gerekçe: doğrulayıcı DÜŞÜK bulgusu). Paketin tüm dosyaları § 00000000.6 + bu liste.

### 000000000.9 DoD sözleşmesi (bu tur)
Yeni varlık yok; `PosYatmaEslesme`'ye kolon eklendi. Düzenleme yolu değişmedi: eşleşme append-only, düzeltme `POST /pos/eslesmeler/{id}/geri-al` (gerekçe ≥ 20, MUDUR+, denetim kaydı `DenetimServisi.YazmaKaydet`) ve ardından yeniden eşleştirme — bu turdan itibaren gerçekten mümkün. Finansal satır hard DELETE yok. Ekran: kilit gerekçesi `DegismezIpucu` aynen.

### 000000000.10 Açık kalanlar (sahibiyle)
- Tam süit başkasına ait: ORTAK-1 `OrtakGorunumSaflik.O8` (15↔16).
- `kiraci-temizle.sh` bash ile çağrılınca sessizce yalnız `tenants`'ı siliyor (`${=…}` zsh sözdizimi, shebang/koruma yok) → betik sahibi/orkestratör: başına `#!/bin/zsh` + `[ -n "$ZSH_VERSION" ] || exit 2` önerilir (dosya alanım dışı, dokunulmadı).
- Kiracı sayısı kapı ölçüsü: toplam yerine ad öneki/kimlik (§ 00000000.1) → orkestratör.
- § 00000000.8'deki KATMAN/ortak bileşen/menü grubu maddeleri değişmedi.

---

## 00000000. 5. DOĞRULAMA BULGULARI (POS çift satır 409 [eski metinde yanlışlıkla "500"] · filtre sayısı · dosya listesi · kiracı sayısı çelişkisi) — 28.09.2026 22:16–22:40 (t7 · API 5170 · vekil 5171 · CDP 9803/9806–9808 · `KUYUMCU_TEST_DB=kuyumcu_test_t7`)
Durum beyanı: **BITTI** (tarihçe; geçerli durum § 000000000). Bu bölüm § 0000000'ı günceller. Ekran kodu değişmedi; yalnız sunucuda tek kural + tek test eklendi. Çalışma dizini `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r8/`.

### 00000000.1 Bulgu → düzeltme
| Bulgu | Düzeltme / kanıt |
|---|---|
| [DÜŞÜK] POS `Eslestir`: aynı tahsilat tek istekte iki satırla gelince 400 yerine **409** "Bu kayıt zaten var" (tekil indeks `DbUpdateException` → MySQL 1062 → genel işleyicide 409; bu satır önce yanlışlıkla "500" yazıyordu, 9. turda düzeltildi — § 000000000) | `PosModulu.cs` `Eslestir` başında: `dto.Satirlar.GroupBy(s => s.PosTahsilatId).Any(g => g.Count() > 1)` → **400** "Aynı tahsilat bir eşleştirmede yalnız bir kez yer alabilir; tutarı tek satırda toplayın". Yeni test `ZOISLEMA4PosTestleri.ZO28i_ayni_tahsilat_IKI_SATIRLA_gelirse_400_ve_hic_yazmaz` (100+200 < net, yani eski kodda indeks hatasına düşen durum; `BadRequestException` + eşleşme satırı 0). Doğrulayıcının `ZOS_pos_ayni_tahsilat_iki_satirla_net_tutari_asamaz` testi de yeşil. Canlı: çift satır → 400 (yukarıdaki metin), tek satır 300 → 200; API günlüğünde `pos_yatma` için `Duplicate entry` **0** (günlükteki 114 `Duplicate entry` satırının hepsi `is_calismalari` arka plan işi — paylaşılan DB'de birden çok API'nin zamanlayıcı yarışı, bu paketle ilgisiz). |
| [DÜŞÜK] "205/205" yeniden üretilemedi | 205 sayısı r7'de filtre metni kaydedilmeden koşulmuş bir koşumdan (`r7/t3.log`) geldi; bugün hiçbir kayıtlı filtre 205 vermiyor (`ZO28|ZOISLEMA4|MasakFormuEkran` = 90). **O sayı geçersiz sayılsın.** Bu turun ölçüsü, tam metinle: `--filter 'FullyQualifiedName~ZOISLEMA|FullyQualifiedName~MasakFormuEkran'` → **134/134** (133 + 1 yeni), TRX `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/filtre-r8.trx`; birleşik `'FullyQualifiedName~ZO28|FullyQualifiedName~ZOISLEMA|FullyQualifiedName~MasakFormuEkran'` → 134/134, TRX `filtre-r8-birlesik.trx`. |
| [DÜŞÜK] Değişen dosya listesi eksik | Paketin 25.09–28.09 arasında dokunduğu TÜM dosyalar aşağıda § 00000000.6'da (izlenmeyenler dahil). |
| [DÜŞÜK] Hakem kiracısı kalıntısı | `01M3JG487JCP11FMBAFRMR4X4P PAKETZO-ISLEM-A-HAKEM-TEST` hâlâ DB'de (22:21 ölçümü). Bu kiracıyı ben açmadım → kural gereği SİLMEDİM; orkestratör kapısında `kiraci-temizle.sh 01M3JG487JCP11FMBAFRMR4X4P --uygula`. Ayrıca yetim davet satırı `01M3MFSC04J9M3NKM98MX77909` ("PAKETZO-ISLEM-A-TEST Hakem2…", kiracısı zaten yok) ve `ZOHAKEM-aaf3618bca81` (yukarıdaki hakem kiracısının daveti) — ikisi de hakeme ait, dokunulmadı. |
| [DÜŞÜK] Paralel şeritte mutasyon kararsızlığı | Bu turdaki bütün filtreli koşumlar, mutasyon ve tam süit `KUYUMCU_TEST_DB=kuyumcu_test_t7` ile. |
| [KRİTİK] Kiracı sayısı çelişkisi (18→18 / 20→17) | Kök neden ölçüldü: `tenants` toplamı paralel şeritlerce sürekli değişiyor, bu yüzden iki ajanın farklı anlarda aldığı toplamlar birbirini tutmaz. Bu turdaki ölçüm zaman çizelgesi: 22:21:08 **19** (listesi `r8/tenants-baslangic.txt`) → 22:21:38 benim kiracım açıldı → 22:23 benim silmemden sonra `kiraci-temizle.sh` "kalan 19" → 22:22:38'de (UTC 19:22:38) OLCEK-1 şeridi `PAKET-OLCEK-1M-TEST` açtı → 22:24 **20** → ikinci kiracım açılıp silindi → 22:31 **19** (arada başka şeritlerin ekleme/silmesi). **Tek ölçü önerisi (doğrulayıcıyla aynı):** toplam değil, paketin KENDİ kiracı kimlikleri: `select count(*) from tenants where ad like 'PAKETZO-ISLEM-A-TEST%'` → başta 0, sonda **0**; kendi kimliklerim için tenant_id kolonlu 70 tabloda **0** satır; hakem kiracısı (`…-HAKEM-TEST`) ayrı ve açık kalem. |

### 00000000.2 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra, 22:20) → **0 Hata, 0 Uyarı** (`r8/build2.log`). İlk derlemede kendi testimde `CountAsync` using eksikti (CS1061) → `Count` ile düzeltildi.
- Filtreli: yukarıdaki 134/134.
- Mutasyon (yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/M55/PosModulu.cs`, günlük `r8/m55-test.log`):

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M55 | `PosModulu.Eslestir` çift satır koşulu `.Any(g => g.Count() > 1)` → `.Any(g => false)` | `ZOISLEMA4PosTestleri.ZO28i_ayni_tahsilat_IKI_SATIRLA_gelirse_400_ve_hic_yazmaz` (ZOISLEMA 115 testte yalnız bu 1 kırmızı) | AYNI |

  M50–M54 (§ 0000000.4) geçerli; o dosyalar bu turda değişmedi.
- **Tam süit** (`KUYUMCU_TEST_DB=kuyumcu_test_t7`, TRX `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-r8.trx`, günlük `r8/tam.log`): **6771 test, 6770 geçti, 1 başarısız**: `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (beklenen 15, gerçek 16 — ORTAK-1, önceki turun listesinde de var). **FARK (bu paketin yeni kırmızısı) = 0.** (Toplam test sayısı 6785 → 6771: başka şeritlerin test dosyası değişiklikleri.)
- `bash ./denetim.sh` → **DENETİM TEMİZ** (ModalPencere 48/55, duzenlenenId 1/1, SATIR_ICI_STYLE 72, SABIT_RENK 0, HAM_INPUT 32/42).

### 00000000.3 Canlı doğrulama (5170; iki test kiracısı `PAKETZO-ISLEM-A-TEST` 01M3MQEDE2408S0N0QZQ5E1ZTF ve 01M3MQJZMVRS1T24737MCB386N; sistem anahtarı rastgele, yalnız süreç ortamında; parolalar chmod 600 fikstürde; yanıtlar okundu — `r8/devam.out`, `r8/senaryo.out`, `r8/devam2.out`, `r8/ek7.out`)
- Kiracı 1: davet 201 · kayıt 201 · giriş 200 · şubesiz MÜDÜR 201 · kasa 200 · cari 201 · banka 200 · dekont 3.000 200 · POS cihazı 200 · KART işçilik 1.500 200 → tahsilat net 1.470 BEKLIYOR.
- `POST /pos/eslestir` (MÜDÜR) aynı tahsilat {100, 200} → **400** "Aynı tahsilat bir eşleştirmede yalnız bir kez yer alabilir; tutarı tek satırda toplayın"; {300} → **200** "Eşleştirme tamamlandı · 300 · 1 satır".
- SQL: `pos_yatma_eslesmeleri` → 1 satır `300.00, otomatik 0, geri_alindi 0`; `pos_tahsilatlari` → `1470.00 KISMI`; `banka_hareketleri` → `ESLESTI`.
- Kiracı 2 (tarayıcı matrisi verisi, r7 senaryosunun aynısı): tevsik önizlemesi 39.395,22 → `kalanNakitHakki 0` / 1.000 → 30.000; `hurda-satis` 6 g NAKIT → 400 tevsik, 1 g HAVALE → 200; MASAK uyruk `ZZQ` → 400, `rks` → 200; `eslestir` 2.695→1.470 hareket → 400, 1.470 → 200.

### 00000000.4 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum ŞUBESİZ MÜDÜR, dolu veri, taze profil) — güncel ağaçta yeniden koşuldu
`r8/sen/sonuc.jsonl`, özet `r8/sen/ozet.txt`: 16 kombinasyon ({1440,1280,960,375}×{BASİT,PRO}×{aydınlık,karanlık}) × 13 sahne = **208 ölçüm**, 16/16 rc=0, mod uyumsuz 0.

| Ölçü | Sonuç (208) |
|---|---|
| yatay kaydırma, K1, K3_tasan, **K3_kirik**, K5_TRY, K7, K8, K9b, K10, K10b, K11, K12, K13 | **0** |
| K9_binen | 478 — yalnız açık pencere sahneleri (pos-iptal-onay, banka-dekont-masak/pasaport/diger-kod, banka-yeni-hesap; her biri 16/16): örtü altındaki sayfa ⟂ pencere yanlış pozitifi, § 0000000 ile birebir aynı |
| banka hareket kabı (scroll/client) | 1440 1088/1088 · 1280 928/928 · 960 608/608 · 375 315/315; 9/9 satırda tutar, durum, eylem görünür, ad kesik 0 |
| POS defter kabı | 1440 1090/1090 · 1280 930/930 · 960 610/610 · 375 317/317; 2/2 |
| hurda nakit sınırı | 16/16 tahsil aracı açılışta görünür, "Daha fazla" kapalı, uyarı "Bu tutar nakit alınamaz; Havale ya da Kart seçin." |
| POS elle | 16/16 öneri-yok cümlesi 1 kez, işaret kutusu 1 |

Görüntüler (Read ile bakıldı): `r8/sen/foto/banka-hareket_BASIT_karanlik_375.png` (9 hareket kart, tutar/durum/eylem görünür), `pos-elle_PRO_aydinlik_1440.png`, `hurda-nakit-siniri_BASIT_aydinlik_1440.png`. K3 tek ölçü: 208'de 0, gerekçeli istisna yok (§ 0000000.6 ile aynı).

### 00000000.5 Temizlik
API (5170) ve vekil (5171) durduruldu, dinleyen 0; `pgrep -f ZO-ISLEM-A/r8` 0 (Chrome profilleri dahil). İki kiracı `kiraci-temizle.sh --uygula` ile silindi (196 satır ve 410 satır + her birinde tenants satırı); ikinci çağrılar "Kiracı YOK (bağlantı sınandı)"; 70 tablo taramasında kendi kimliklerim için **0** satır; kendi iki davet satırım silindi (ROW_COUNT 1 + 1). `tenants where ad like 'PAKETZO-ISLEM-A-TEST%'` → **0**. Toplam `tenants` bitişte (22:31) **19** — paralel şeritler yüzünden toplam, paket ölçüsü değildir (bkz. § 00000000.1 KRİTİK satırı). QA-TEST ve başka şeritlerin PAKET* kiracılarına dokunulmadı.

### 00000000.6 Paketin dokunduğu TÜM dosyalar (25.09–28.09; git'te izlenmeyenler dahil)
- API: `dotnet/Kuyumcu.Api/Modules/Hurda/HurdaStokKapisi.cs` (OturumSubesiCoz) · `Modules/Hurda/HurdaModulu.cs` · `Modules/Hurda/TahlilFarkiServisi.cs` (OturumSubesiCoz) · `Modules/Satis/SatisModulu.cs` (HurdaAlis/HurdaSatis şube satırları; paylaşılan dosya — 27.09 21:59 değişikliği bu paketin değil) · `Modules/Arama/AramaModulu.cs` (BANKA_HAREKET araması) · `Modules/Masak/MasakKapisi.cs` (uyruk ISO) · `Modules/Pos/PosModulu.cs` (`hareketKalan` öneri yanıtı; **bu tur** çift satır 400).
- Core: `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoIslem.cs` (yeni; TR+EN; `UlkeListesi.IsoKodlari` + `KodGecerli`, `RKS` dahil).
- Web: `dotnet/Kuyumcu.Web/Pages/Hurda.razor` · `Pages/Banka.razor` · `Pages/PosTahsilatlar.razor` · `Bilesenler/MasakFormu.razor` (ortak bileşen, hakem bulgusu gerekçesiyle) · `Bilesenler/KayitSecici.razor` (`AraDugmesiYok`, `BosMesaj`) · `wwwroot/css/zo-islem.css` (yeni) · `wwwroot/index.html` (1 link).
- Testler: yeni `ZOISLEMAEkranTestleri.cs`, `ZOISLEMASubeTestleri.cs`, `ZOISLEMAAramaTestleri.cs`, `ZOISLEMA4Testleri.cs`, `ZoTestYardim.cs`; güncellenen `EkranTestleri.cs` (MasakFormuEkranTestleri), `P20bEkranTestleri.cs`, `P20EkranTestleri.cs`, `Q102HurdaOnayEkranTestleri.cs`, `Dogrulayici/Q102SinirTestleri.cs`, `SADE1EkranTestleri.cs`, `PosEkranTestleri.cs`, `BankaEkranTestleri.cs`, `P12EkranTestleri.cs`, `Q91BankaEkranTestleri.cs`, `UX1C2EkranTestleri.cs`, `P19VirmanBacagiEkranTestleri.cs`.
- **Bu turda değişen:** `dotnet/Kuyumcu.Api/Modules/Pos/PosModulu.cs` (+6 satır) · `dotnet/Kuyumcu.Tests/ZOISLEMA4Testleri.cs` (+1 test). Ekran dosyası değişmedi. Migration YOK (gerekmedi).
- Not: `Modules/Hurda/`, `Modules/Pos/`, `Modules/Masak/`, `Pages/Hurda.razor`, `Pages/PosTahsilatlar.razor`, `Sozlukler.ZoIslem.cs`, `zo-islem.css` git'te izlenmiyor (`??`) — dosya düzeyinde iz yalnız bu listeyle sürülür; orkestratör kapı kaydına bu liste yazılmalı.

### 00000000.7 DoD sözleşmesi (bu tur)
Yeni varlık/uç YOK. `POST /pos/eslestir` sözleşmesi daraldı (çift satır 400); geri dönüş yolu `eslesmeler/{id}/geri-al` aynen. Finansal değişmez (net aşılmaz) artık servis kuralıyla, indekse bırakılmadan korunur.

### 00000000.8 Açık kalanlar (sahibiyle)
- Tam süit başkasına ait: ORTAK-1 `OrtakGorunumSaflik.O8` (15↔16).
- Hakem kiracısı `01M3JG487JCP11FMBAFRMR4X4P` + iki hakem daveti → orkestratör kapısı (`kiraci-temizle.sh`).
- Kiracı sayısı kapı ölçüsü: toplam yerine paket kimliği/ad öneki ile sayılmalı (doğrulayıcı istemine eklenmeli) → orkestratör.
- POS PRO menüsünde "POS cihazları" ve "POS alacakları" birlikte seçili görünüyor (görüntü `pos-elle_PRO_aydinlik_1440.png`) → KATMAN (önceki turdan). GlobalArama 375, KenarCubugu, ModalPencere 375 → KATMAN/ortak bileşen.
- BASİT "Tahsil aracı (isteğe bağlı)" etiketi nakit sınırı aşılınca da "isteğe bağlı" diyor (ortak Secim) → tasarım hakemi.
- Kosova `RKS` kaynağı (Cloudflare) → Mehmet (06); SubeBelirle fallback → Bertan/Taşkın; /hurda menü grubu → kurucu/mimar (değişmedi).

---

## 0000000. 4. DOĞRULAMA BULGULARININ KAPATILMASI + YENİDEN ÖLÇÜM — 28.09.2026 20:50–21:45 (t7 · API 5170 · vekil 5171 · CDP 9803/9806–9811)
Durum beyanı: **BITTI** (tarihçe; geçerli durum § 00000000). Bu bölüm § 000000'ı günceller (geçerli durum budur). Kod yeniden yazılmadı.

### 0000000.1 Devralma
- 20:34–20:43 arasında yarıda kesilen bir koşum (`r6/`) ağaçta şu işleri UYGULAMIŞ bıraktı (betikler `r6/tablo.py`, `r6/hurda.py`, `r6/pos.py`; hepsi ağaçta grep ile doğrulandı): banka hareketleri + POS "Kartlı tahsilatlar" 1100 px altında KART (`zo-kart-tablo`, `zo-k-*` sınıfları, `data-etiket`); Hurda nakit sınırı ön uyarısı (sunucu `GET /tevsik/gunluk` önizlemesiyle); POS elle eşleştirme (satır işaretleme, `Satirlar` gövdesi); sunucu tarafı MASAK uyruk ISO denetimi (`MasakKapisi`, `UlkeListesi.KodGecerli`); Kosova `RKS`. Yarım mutasyon yoktu (r6'da mutasyon betiği yok). Son r6 test günlüğünde 1 kırmızı vardı (`ZO28d…` 256↔257) — test dosyası sonradan 257'ye güncellenmişti.
- Bu turda bulunan ve düzeltilen test kusurları: `ZO28e_hurda_nakit_SINIRI…` 10 g giriyordu, fikstür bakiyesi 9,16 g → bakiye aşımı yüzünden onay açılmıyordu (test hatası; 9 g / `fisToplamTl=61200` yapıldı). `ZOISLEMAEkranTestleri.cs` 1043/1047 xUnit2013 uyarıları → `Assert.Single`.

### 0000000.2 Bulgu → düzeltme
| Bulgu | Düzeltme | Tarayıcı kanıtı (16 kombinasyon: {1440,1280,960,375}×{BASİT,PRO}×{aydınlık,karanlık}) |
|---|---|---|
| [ORTA] Banka hareket tablosu 375'te TUTAR ancak kaydırınca görünüyor | `#bh-hareket-tablo` 1100 px altında kart (başlık gizli; tarih·yön, karşı taraf+açıklama, büyük TUTAR·durum, eylemler); POS valör ile aynı kalıp | `banka-hareket`: 375 kap 315/315, 960 608/608 (kaydırma YOK); 9/9 satırda TUTAR, durum, eylem görünür; ad kesik 0 |
| [KRİTİK] Banka + POS listeleri 960/375'te tutar/eylem göstermiyor | aynı kart kalıbı `#pos-t-defter-tablo`'ya; valör kartında cihaz adı 600 px altında kendi satırında | `pos-defter-kart`: 375 317/317, 960 610/610; 2/2 tutar+durum+eylem görünür, ad kesik 0 |
| [KRİTİK] POS kısmi yatışta "elle eşleştirin" ama yol yok, mesaj iki kez | öneri yoksa TEK cümle "Bu tutar bir günün toplamıyla tutmuyor. Bankanın yatırdığı tahsilatları aşağıdan işaretleyin." + işaretlenebilir tahsilat listesi; seçilen toplam kalanı aşarsa düğme kilitli ve sebep yazılı; gövde seçilen satırlardan | `pos-elle` 16/16: öneri-yok metni 1 kez, "elle eşleştirin" 0, işaret kutusu 1 (kalan tek tahsilat). Canlı: `eslestir` elle 1470 → 200, fazla 2695→1470 → 400 |
| [KRİTİK] Hurda tevsik duvarı önceden söylenmiyor, ret iki kez | tutar sunucu önizlemesine göre nakit alınamazsa tahsil aracı AÇILIŞTA görünür + alanın altında tek cümle "Bu tutar nakit alınamaz; Havale ya da Kart seçin."; eşik SAYISI istemcide yok (sunucu `/tevsik/gunluk`); ret BASİT'te mevzuat atfı olmadan; **bu turda ek:** BASİT'te ret cümlesi alan uyarısıyla aynıysa pencere kapanınca banda ikinci kez yazılmaz | `hurda-nakit-siniri` 16/16: tahsil aracı görünür, "Daha fazla" kapalı, uyarı metni doğru; Havale seçilince uyarı kalkar. `ret.mjs` (4 kombinasyon): BASİT pencere → metin pencerede, kapatınca sayfada **1** kez, mevzuat atfı yok; PRO'da tam sunucu metni tek yerde |
| [DÜŞÜK] 375'te hesap rozeti iki satır | 600 px altında sekmede ad ve bakiye alt alta | `sekmeAd` 16/16: "Garanti BBVA — Vadesiz TL" 1 satır |
| [DÜŞÜK] Kosova pasaport kodu | `UlkeListesi.IsoKodlari` + `RKS` (257 kod). Kaynak: AB Konseyi PRADO belge kodu `RKS-AO-03001` (PRADO kodları ICAO ülke kodunu önek alır). ⚠ Bu turda sayfa Cloudflare engeli nedeniyle yeniden açılamadı (WebFetch 403, curl → bot sınaması) — kaynak önceki koşumun kaydıdır; `XKX` pasaportta doğrulanamadığı için EKLENMEDİ. Mevzuat hükmü değil, yazım denetimi. | canlı: MASAK dekont uyruk `rks` → 200 |
| [DÜŞÜK] Uyruk ISO denetimi yalnız istemcide | `MasakKapisi` aynı `UlkeListesi.KodGecerli`'yi çağırır → 400 "MASAK: uyruk kodu geçersiz ('ZZQ'); pasaporttaki 3 harfli ülke kodunu yazın" | canlı ZZQ → 400; testler `ZOISLEMA4MasakSafTestleri.*` |

### 0000000.3 Dosya alanı dışı (DUR-VE-YAZ kaydı)
- `Kuyumcu.Api/Modules/Masak/MasakKapisi.cs` — doğrulayıcı bulgusu açıkça sunucu tarafını istedi; en küçük değişiklik (tek koşul). Sahibi Mehmet/Taşkın → orkestratör kapı kaydına.
- `Kuyumcu.Tests/P20bEkranTestleri.cs` — `Defterde_ham_tur_kodu_BASILMAZ…` Hurda sayfasını sınar; `Find` → `WaitForElement` (tam süit yükünde yarış; tek başına 3/3 yeşildi). Assert içeriği değişmedi.
- Banka.razor yorum bloğu kısaltıldı: `P39TasarimKokleriTestleri.K5_Her_tablo_KENDI_kaydirma_kabindadir` `<table>`'dan geriye 600 karakterde sarmalayıcı arıyor; uzun yorum onu dışarı itmişti.

### 0000000.4 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra) → **0 Hata, 0 Uyarı** (21:2x); son koşumda 1 uyarı `EtiketEkranTestleri.cs(145)` xUnit1031 — bu paketin değil. (Arada bir derleme `Emanet.razor(298) CS0103 'Modu'` ile kırıldı — EMANET-1 şeridinin yarım işi; 60 sn sonra yeşil.)
- Yeni test: `ZOISLEMAEkranTestleri.ZO28f_hurda_ret_ALAN_UYARISIYLA_ayniysa_banda_tekrar_yazilmaz`; düzeltilen: `ZO28e_hurda_nakit_SINIRI…` (fikstür), `P20b…Defterde_ham_tur_kodu…` (bekleme).
- Filtreli `ZOISLEMA|MasakFormuEkran`: **205/205**.
- Mutasyonlar (betik `r7/mut.sh`+`r7/mutlar.sh`, yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/M50…M54/`, günlük `r7/mut-r7.log`, her birinde derleme 0 Hata):

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M50 | Hurda `OnayKapat` tekrar koşulu silindi | `ZO28f_hurda_ret_ALAN_UYARISIYLA…` | AYNI |
| M51 | `NakitKapali` → hep false | `ZO28e_hurda_nakit_SINIRI…`, `ZO28f…` | AYNI |
| M52 | POS `EslestirilebilirMi` `Fazla == 0m` silindi | `ZO28e_pos_elle_secim_FAZLAYSA_kilitli_sebep_yazili` | AYNI |
| M53 | `MasakKapisi` ISO koşulu `if (false)` | `ZOISLEMA4MasakSafTestleri.ZO28e_esik_ustunde_ISO_DISI_uyruk_400` (ZZQ, TR, xkx) | AYNI |
| M54 | CSS kart kuralı (thead gizleme) silindi | `ZO28e_banka_ve_pos_defteri_dar_ekranda_KART_kalibi` | AYNI |

- **Tam süit** (`KUYUMCU_TEST_DB=kuyumcu_test_t7`, `trx-pZO-ISLEM-A/tam-28h.trx`): **6785 test, 6777 geçti, 8 başarısız — hiçbiri bu paketin dosyasına bağlı değil:** `OrtakGorunumSaflik.O8` (ORTAK-1, önceki turlardan) · `EmanetSadeTestleri.E18`, `S35EmanetEkranTestleri.S35_14/S35_15`, `UX1C2EkranTestleri.Emanet_COK_BACAKLI…` (EMANET-1 — Emanet.razor/EmanetModulu koşum sırasında değişiyordu; tek başına koşumda bu dördü yeşil, yerine `E41` kırmızı → şerit yarım işi) · `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA…` (SATIS-3 alanı), `MigrationHazirlikTestleri.EksikMigrationli…`, `YayinAcilisHttpTestleri.HazirUcu…` (tek başına yeşil → kararsız, paylaşılan DB/migration; ağaçta başka şeridin `Data/Migrations` işi var). İlk tam koşumdaki bu pakete ait 2 kırmızı (`P20b…Defterde…`, `P39…K5…`) düzeltildi, ikinci koşumda yeşil. **FARK (bu paketin yeni kırmızısı) = 0.**
- `bash ./denetim.sh` → **DENETİM TEMİZ** (ModalPencere 48/55, duzenlenenId 1/1, SATIR_ICI_STYLE 72, SABIT_RENK 0, HAM_INPUT 32/42).

### 0000000.5 Canlı doğrulama (5170, test kiracısı `PAKETZO-ISLEM-A-TEST` 01M3MJGPYPCFF1T3MPGBKFK99J; sistem anahtarı rastgele üretilip yalnız süreç ortamına verildi; parolalar chmod 600 fikstürde; yanıtlar okundu — `r7/senaryo.out`, `r7/devam.out`, `r7/ek7.out`)
- Hazırlık: davet 201 · kayıt 201 · giriş 200 · MÜDÜR (şubesiz) 201 · kasa/kasa giriş 200 · cari 201 · kur 200 · `hurda-alis` (şubesiz MÜDÜR) 200 has 9,16 · `hurda-satis` 2,5 g 200 · `/hurda/bakiye` 200 6,66 g · banka 200 · 7 dekont 200 · POS cihazı 200 · 2 KART işçilik 200 · valör takvimi 200 (brüt 4.250, komisyon 85, net 4.165) · ikinci hesap "Garanti BBVA / Vadesiz TL" 200.
- `GET /tevsik/gunluk?fisToplamTl=39436.56` → 200 `kalanNakitHakki 0, islemTutariKuraliDevrede true`; `…=1000` → 200 `kalanNakitHakki 30000, …false` (eşik sunucudan).
- `hurda-satis` 6 g NAKIT → **400** "Tevsik zorunluluğu: … 39.436,56 TL, 30.000,00 TL haddini aşıyor…"; 1 g HAVALE → **200**.
- MASAK dekont uyruk `ZZQ` → **400** "MASAK: uyruk kodu geçersiz ('ZZQ')…"; `rks` → **200**.
- POS: `oto-eslestir-onerisi` (103.439,43) → 200 `bulundu:false`; 1.470 yatış: `eslestir` 2.695 satırıyla → **400** "Eşleşen toplam banka hareketi tutarını aşamaz…"; 1.470 satırıyla → **200** "Eşleştirme tamamlandı · 1470 · 1 satır"; sonra tahsilat durumları `(2695, BEKLIYOR)`, `(1470 kalan 0, YATTI)`.
- Arama (`tur=BANKA_HAREKET`) önceki turla aynı (q=103 → Garanti POS yatan; q=1.250 → 3 yorum; q=999 → []).

### 0000000.6 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum ŞUBESİZ MÜDÜR, dolu veri, taze profil)
- **Senaryo matrisi** (`r7/sen/sonuc.jsonl`, `sahne.mjs`+`kos2.sh`): 16 kombinasyon × 13 sahne = **208 ölçüm** (yeni sahneler: `hurda-nakit-siniri`, `pos-elle`, `pos-defter-kart`; `banka-hareket` artık Ziraat sekmesini seçip kart ölçer). İlk koşumda 3 kombinasyon giriş yarışıyla düştü (rc 1/3) → tekrar denemeli betikle hepsi rc=0. Mod uyumsuz 0.

| Ölçü | Sonuç (208) |
|---|---|
| yatay kaydırma | 0 |
| K1, K3_tasan, **K3_kirik**, K5, K7, K8, K9b, K10, K10b, K11, K12, K13 | **0** |
| K9_binen | 478 — YALNIZ açık pencere sahnelerinde (POS iptal onayı, dekont MASAK/PASAPORT/Diğer ülke, yeni hesap): örtü altı sayfa ⟂ pencere, ölçüm yanlış pozitifi (önceki turlarla aynı). Pencere dışı sahnelerde 0 |
| banka hareket kabı (scroll/client) | 1440 1088/1088 · 1280 928/928 · 960 **608/608** · 375 **315/315** (önceki tur 852/608 ve 979/315) |
| POS defter kabı | 1440 1090/1090 · 1280 930/930 · 960 610/610 · 375 317/317 |

- **K3 tek ölçü, tek karar:** uygulayıcı ve doğrulayıcı aynı `sikisma.js` K3_kirik'i ölçer → 208 ölçümde **0**; gerekçeli istisna YOK.
- **tur.mjs** (`r7/tur`, `analiz.txt`): 48 sayfa, 164 katman, hata 0; tek bulgu GlobalArama 375 SERBEST-YÜZEN (KATMAN) — önceki turla birebir aynı (`diff` boş). **Yeni bulgu 0.**
- **tutarlilik.mjs 1440**: mod farkı 0, `tutarlilik-fark-1440.txt` 0 bayt, düğme sınıf grubu 6. **dugme-olc**: ortasız 0 silik 0 kırpık 0.
- **Açılış 1440×900** (`r7/acilis/acilis.log`): hurda 0,85/0,88 · POS 0,85/0,88 · banka 0,55/0,55; birincil düğme her ekranda 1.
- Görüntüler (Read ile bakıldı): `ham/TUVAL-KARSILASTIRMA/ZO-ISLEM-A-28g-banka-hareket-ONCE-SONRA-375.png` (önce: tablo kesik, TUTAR yok; sonra: her hareket kart, tutar büyük, durum ve eylem görünür), `…-960.png`, `ZO-ISLEM-A-28g-hurda-nakit-siniri-ONCE-SONRA-1440.png`, `ZO-ISLEM-A-28g-pos-elle-eslestir-ONCE-SONRA-1440.png`; ayrıca `r7/sen/foto/hurda-ret-pencere_BASIT_karanlik_375.png`, `pos-defter-kart_BASIT_karanlik_375.png` (208 + 8 görüntü `r7/sen/foto/`).
- Bölge tablosu ek satırları: "Banka hareketleri <1100 px — tuvalde yok · kart · GEREKÇELİ (tutar/eylem görünürlüğü; satır içi düzenleme düğmeleri `--dokunma-hedefi` yüksekliğini korur, bu yüzden kart satırları ~48 px)"; "POS elle eşleştirme listesi — tuvalde yok · öneri bulunamayınca açılır · GEREKÇELİ (sunucu `Satirlar` alanı, hakem KRİTİK)".

### 0000000.7 Temizlik
API (5170) ve vekil (5171) durduruldu: dinleyen 0; `pgrep -f ZO-ISLEM-A/r7` 0 (Chrome'lar dahil). `zsh kiraci-temizle.sh 01M3MJGPYPCFF1T3MPGBKFK99J --uygula` → ikinci çağrı "Kiracı YOK (bağlantı sınandı)"; information_schema'daki tenant_id kolonlu TÜM tablolarda bu kiracı için **0 satır**; `kiraci_davetleri` 1 satır silindi (ROW_COUNT 1). **tenants: başlangıç 18 → son 18.** QA-TEST ve başka şeritlerin PAKET* kiracılarına dokunulmadı.

### 0000000.8 Bu turda değişen dosyalar (r6 devri dahil)
`dotnet/Kuyumcu.Web/Pages/Hurda.razor` · `dotnet/Kuyumcu.Web/Pages/PosTahsilatlar.razor` · `dotnet/Kuyumcu.Web/Pages/Banka.razor` · `dotnet/Kuyumcu.Web/wwwroot/css/zo-islem.css` · `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoIslem.cs` · `dotnet/Kuyumcu.Api/Modules/Masak/MasakKapisi.cs` · `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` · `dotnet/Kuyumcu.Tests/ZOISLEMA4Testleri.cs` · `dotnet/Kuyumcu.Tests/EkranTestleri.cs` · `dotnet/Kuyumcu.Tests/P20bEkranTestleri.cs`. Dokunulan sayfalar: Hurda, PosTahsilatlar, Banka (aynı paketin hakem düzeltmesi — gerekçe: doğrulayıcı bulguları). Migration YOK (gerekmedi).

### 0000000.9 DoD sözleşmesi (bu tur)
- Yeni varlık/kayıt türü YOK. POS eşleştirmesi mevcut `POST /pos/eslestir` + `eslesmeler/{id}/geri-al` (geri dönüş yolu) ile; hurda satışı değişmez (düzeltme: tahlil farkı düzeltmesi / müşteri düzeltme fişi, DegismezIpucu); banka hareketi finansal kayıt — karşı taraf/açıklama satır içi düzenleme, tutar/yön storno ile. MASAK kimlik tespiti append-only.
- Tevsik: had ve kural SUNUCUDAN (`/tevsik/gunluk`, `TevsikKapisi`); istemciye eşik gömülmedi; son karar sunucuda (fail-closed sunucudadır).

### 0000000.10 Açık kalanlar (sahibiyle)
- Tam süit başkasına ait: ORTAK-1 `O8`; EMANET-1 (E18, S35_14, S35_15, UX1C2 Emanet, tek başına E41 — şerit yarım); SATIS-3 `AlisOdemeAraci…` ve migration testleri (kararsız, tek başına yeşil) → orkestratör.
- Kosova `RKS` kaynağı bu turda yeniden açılamadı (Cloudflare); Mehmet (06) kaynağı tarayıcıdan teyit etsin.
- GlobalArama 375 SERBEST-YÜZEN, KenarCubugu BASİT 1440 belge 933 px, POS menüsünde iki satır birlikte seçili → KATMAN. ModalPencere 375 iç kaydırma (ortak bileşen).
- BASİT'te "Tahsil aracı (isteğe bağlı)" etiketi nakit sınırı aşıldığında da "isteğe bağlı" yazıyor (ortak Secim etiketi) — karar tasarım hakemine.
- § 000000.10'daki diğer maddeler (GIDEN tutarda ASCII "-", SubeBelirle fallback → Bertan/Taşkın, /hurda menü grubu → kurucu/mimar, `kiraci-temizle.sh` bash kalkanı) değişmedi.

---

## 000000. ÇAPRAZ HAKEM ÇELİŞKİSİ (K3) + 3 DÜŞÜK BULGU + BAŞSIZ CHROME YENİDEN ÖLÇÜM — 28.09.2026 19:05–20:10 (t7 · API 5170 · vekil 5171 · CDP 9803/9806–9811)
Durum beyanı: **BITTI**. Bu bölüm § 00000'ı günceller (geçerli durum budur). Kod yeniden yazılmadı; yalnız devir notu 2'nin istediği dört iş yapıldı. Sıra: kod → derleme → canlı veri + tarayıcı ölçümleri (16 kombinasyon) → API/vekil durduruldu → kiracı silindi → mutasyon M42–M49 → temiz derleme → filtreli + tam süit → denetim.

### 000000.1 Devralma
- Ağaç § 00000 sonu ile aynıydı (yarım mutasyon yok: `mut-yedek/ZO-ISLEM-A/M35…M41/orijinal` ile Banka.razor / MasakFormu.razor / AramaModulu.cs cmp → son hâlim; 5170/5171 boş; tenants **19**).
- **Ölçüm tuzağı (Madde 1 — bulundu ve giderildi):** ilk sonda (prob.mjs) bir hata ile yarıda kaldı ve başlattığı başsız Chrome (PID 72485, 9811) kapanmadı; sonraki koşumlar yeni profil verseler de o AÇIK Chrome'a bağlanıp ESKİ CSS'i ölçtü (`min-width` 0px). Süreç öldürüldü; bütün betikler artık her koşumda `profil-<port>-<zaman>` ile TAZE profil açar ve koşum sonunda `pkill -f r5/sen/profil-<port>-` ile kendi Chrome'unu kapatır. Aşağıdaki bütün sayılar bu düzeltmeden SONRA alındı.

### 000000.2 Bulgu → düzeltme
| Bulgu | Karar / düzeltme | Kanıt |
|---|---|---|
| [KRİTİK — çapraz hakem] K3 çelişkisi: uygulayıcı K3_kırık 3 ("Garanti POS yatan" 1280/960), doğrulayıcı K3 0 | **TEK ÖLÇÜ, TEK KARAR: hücre sığdırıldı, gerekçe KALMADI.** Banka hareket tablosunda "Karşı taraf" ve "Açıklama" iki dar sütundu; ikisi **tek hücrede alt alta** oldu (ad üstte, açıklama soluk altta; başlık "KARŞI TARAF · AÇIKLAMA"). Hücre içindeki iki bloğa `min-width: 14rem` (24 karakterlik ad + satır içi düzenleme düğmesi iç boşluğu tek satır) — tablo otomatik yerleşiminde `td` min-width'i dar kapta uygulanmadığı ölçüldü, bu yüzden asgari genişlik içerik bloğuna verildi. Satır içi düzenleme (SatirIciDuzenle banka-dekont karsitaraf/aciklama), kimlikler, eylemler DEĞİŞMEDİ. Sütun 7→6. | Senaryo matrisi 160 ölçüm: **K3_tasan 0 · K3_kirik 0** (her genişlikte). Tablo kabı: 1440 1088=1088 · 1280 **928=928** (kaydırmasız) · 960 852>608 · 375 979>315 (kabında kayar, sayfa yatay 0). Sonda: 23 karakterlik "Kuyumcular Odası Aidatı" ve "Garanti POS yatan" 1440/1280/960/375'te **1 satır**; yalnız 32 karakterlik "Mehmet Kaya Kuyumculuk Ltd. Şti." 960'ta kelime arasından sarar (> 24 karakter, K3 tanımı dışı). Görüntü: `ham/TUVAL-KARSILASTIRMA/ZO-ISLEM-A-28e-banka-hareket-K3-ONCE-SONRA-1280.png`, `…-960.png` (Read ile bakıldı). Test: `ZO28_banka_hareket_tablosu_DAR_sutun_ve_eylemler_ortak_menude` (6 başlık, `zo-hucre-karsi`, iki blok, tutar 4. hücre, CSS 15rem kuralı). M47, M48 |
| [DÜŞÜK] BASİT MASAK "Belge No (TCKN / Pasaport)" kısaltması | `MasakFormu` Sade=true (BASİT çağıran) etiketi belge türüne göre kısaltmasız: "T.C. kimlik numarası" / "Pasaport numarası" / "Belge numarası" (EN: "Turkish ID number" / "Passport number" / "Document number"). PRO etiketi (HUB) değişmedi. Yeni metinler `Sozlukler.ZoIslem.cs` (HUB'a dokunulmadı). | tarayıcı 8 BASİT kombinasyon: "Pasaport numarası *"; 8 PRO: "Belge No (TCKN / Pasaport) *". `ZO28d_BASIT_belge_no_etiketi_TCKN_kisaltmasi_tasimaz_belge_turune_gore`; M43 |
| [DÜŞÜK] "Diğer ülke" kodu ISO doğrulaması yok | `UlkeListesi.IsoKodlari`: ISO 3166-1 alpha-3'ün TAMAMI (249) + pasaportta geçen ICAO Doc 9303 özel kodları (XXA vatansız, XXB/XXC mülteci, XXX belirsiz, UNO/UNA/UNK BM) = 256. **Kaynak/üretim (tahmin değil):** IANA tz veritabanı `/usr/share/zoneinfo/iso3166.tab` (ISO 3166-1 alpha-2, 249 satır) → .NET/ICU `RegionInfo.ThreeLetterISORegionName` (`dotnet fsi`, betik `r5/iso.fsx`); ICU'nun tanımadığı tek kod EH → ESH (Batı Sahra) elle. Listede olmayan kod: alan altında "Bu kod bir ülke kodu değil. Pasaporttaki 3 harfli kodu yazın (örnek: FRA)." ve `MasakFormu.Eksiksiz` false → Kaydet kilitli (sebep alanın altında, UX-S30). Boş uyruk geçerli sayılır (zorunluluğu sunucu söyler — § 00000.9 notu değişmedi). Mevzuat hükmü DEĞİL (yazım denetimi). | tarayıcı 16/16: ZZQ → hata metni görünür, FRA → hata yok (`kodHata null`); `ZO28d_diger_ulke_kodu_ISO_DISIYSA_alan_alti_hata_ve_kayit_kilitli`; M42, M46, M49; görüntü `ZO-ISLEM-A-28e-masak-diger-kod-BASIT-PRO-1440.png` |
| [DÜŞÜK] "0.050" binlik yorumu anlamsız önek ("0050") | Binlik grubu 0 ile başlayamaz: `TutarOnekiCoz` binlik deseni `^[1-9]\d{0,2}(\.\d{3})+$` (noktalı ve virgüllü dalda). "0.050" → yalnız ondalık yorum; `TutarOnekleri("0.050")` → **["0.05"]** (tek önek). Doğrulayıcının `ZOS3/ZOS4` veri satırları DEĞİŞMEDİ ve yeşil. | canlı `q=0.050` → 200 [Kuruş farkı · +0,05 ₺] (tek sonuç), `q=0,05` ve `q=0.05` aynı; `q=1.250` hâlâ iki yorum (Faiz +1,25 · Kira +1.250 · Elektrik −1.250,50). Testler: `ZO28b_tutar_oneki_cozumu("0.050"→"0.050", "0.050,00"→null, "012.000")`, `ZO28c_tutar_onekleri…("0.050"→"0.05", "0.055")`; M44, M45 |

### 000000.3 Dosya alanı dışı (DUR-VE-YAZ kaydı)
- `Kuyumcu.Web/Bilesenler/MasakFormu.razor` (§ 0000.3 gerekçesiyle; bulgular doğrudan bu bileşende). İşlemler (SATIS-3) çağıranında etki: yalnız "Diğer ülke" kutusuna ISO dışı kod yazılırsa Kaydet kilitlenir; BASİT etiket metni değişir. → orkestratör kapı kaydına.
- `Kuyumcu.Api/Modules/Arama/AramaModulu.cs` (yalnız tutar öneki).
- `Kuyumcu.Tests/EkranTestleri.cs` (yalnız `MasakFormuEkranTestleri` +2).
- DOKUNULMAYAN: app.css, Sozlukler HUB, ortak Form bileşenleri, SatirIciDuzenle, SatirEylemleri, Islemler*, Fis/*, Masak modülü (sunucu).

### 000000.4 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra) → **0 Hata, 1 Uyarı** (`EtiketEkranTestleri.cs(145)` xUnit1031 — bu paketin değil).
- Yeni testler **2 yöntem** + mevcut 2 teoriye 5 veri satırı + 1 testte yapı güncellemesi: `MasakFormuEkranTestleri.ZO28d_diger_ulke_kodu_ISO_DISIYSA_alan_alti_hata_ve_kayit_kilitli` · `MasakFormuEkranTestleri.ZO28d_BASIT_belge_no_etiketi_TCKN_kisaltmasi_tasimaz_belge_turune_gore` · `ZOISLEMAAramaSafTestleri.ZO28b_tutar_oneki_cozumu` (+3 veri) · `ZO28c_tutar_onekleri_belirsiz_yazimda_IKI_yorum` (+2 veri) · `ZOISLEMAEkranTestleri.ZO28_banka_hareket_tablosu…` (birleşik hücre).
- Filtreli: `ZOISLEMA|MasakFormuEkran|ZOISLEMASinir` **164/164**; geniş (`ZOISLEMA|MasakFormuEkran|Masak|Banka|Islemler|Arama|Pos|Hurda`) **852/852**.
- Mutasyonlar (betik `r5/mut.sh` + `r5/mutlar.sh`, yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/M42…M49/`, günlük `r5/mut-r5.log`; her birinde derleme 0 Hata; önce kuru koşuyla her ifadenin TEK yerde eşleştiği doğrulandı):

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M42 | `UlkeListesi.KodGecerli` → hep `true` | `ZO28d_diger_ulke_kodu_ISO_DISIYSA…` | AYNI |
| M43 | `BelgeNoEtiketi` BASİT'te de HUB etiketi | `ZO28d_BASIT_belge_no_etiketi…` | AYNI |
| M44 | binlik deseni `^[1-9]…` → eski `^\d{1,3}…` | `ZO28b_tutar_oneki_cozumu("0.050")`, `("012.000")`, `ZO28c_tutar_onekleri…("0.055")` | AYNI |
| M45 | "0.050" tek önek dönüşü silindi | `ZO28c_tutar_onekleri…("0.050")` | AYNI |
| M46 | `Eksiksiz` içindeki ISO denetimi silindi | `ZO28d_diger_ulke_kodu_ISO_DISIYSA…` | AYNI |
| M47 | Banka `td` `zo-hucre-karsi` sınıfı silindi | `ZO28_banka_hareket_tablosu_DAR_sutun…` | AYNI |
| M48 | CSS `td.zo-hucre-dar.zo-hucre-karsi { min-width: 15rem … }` silindi | `ZO28_banka_hareket_tablosu_DAR_sutun…` | AYNI |
| M49 | uyruk kodu kutusunun `Hata=` bağı silindi | `ZO28d_diger_ulke_kodu_ISO_DISIYSA…` | AYNI |

- **Tam süit** (`KUYUMCU_TEST_DB=kuyumcu_test_t7`, `/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-28e.trx`): **6715 test, 6713 geçti, 2 başarısız**; bu paketin testlerinin hepsi yeşil. Kırmızılar:
  - `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (15 ↔ 16) → **ORTAK-1** (§ 000/§ 0000/§ 00000'da da vardı).
  - `P07DayaniklilikTestleri.B10_esZamanli_5_istek_TEK_jeton_yenileme_uretir` (beklenen 1, gerçek 2) → **kararsız**: aynı ikili ile tek başına koşuldu → 5/5 YEŞİL. Eşzamanlı jeton yenileme testi; bu paketin dokunduğu hiçbir dosyaya (Banka/MasakFormu/Arama/ZoIslem sözlüğü/zo-islem.css) bağlı değil → sahibi Auth/P-07 (orkestratör).
  - § 00000'daki SATIS-3 kırmızıları (`KarmaOdemeEkranTestleri` ×2, `SATIS3EkranTestleri.T3_Takas…` ×2) bu koşumda YEŞİL.
  - **FARK (bu paketin yeni kırmızısı) = 0.**
- `bash ./denetim.sh` → **DENETİM TEMİZ.** Bu paketin sayaçları: ModalPencere 48/55 (artış 0), duzenlenenId 1/1, SATIR_ICI_STYLE 74 (değişmedi), SABIT_RENK 0, HAM_INPUT 32/42.

### 000000.5 Canlı doğrulama (5170, test kiracısı `PAKETZO-ISLEM-A-TEST` 01M3MCERXYG4P2WMVH1ZDTDAW7; sistem anahtarı rastgele üretilip yalnız süreç ortamına verildi; parolalar chmod 600 fikstürde, çıktıya yazılmadı; yanıt gövdeleri okundu)
- Hazırlık (`r5/senaryo.out`, `r5/devam.out`): davet 201 · kayıt 201 · giriş 200 · şubeler 200 · MÜDÜR (şubesiz) 201 · kasa 200 · kasa GİRİŞ 200 · cari 201 · kur 200 · `hurda-alis` (şubesiz MÜDÜR) 200 has 9,16 · `hurda-satis` 2,5 has 200 · `/hurda/bakiye` 200 6,66 g · banka 200 · dekontlar GELEN 2.456.789,45 (MASAK TC_KIMLIK/TUR) · GIDEN 1.250,50 · GELEN 103.439,43 · GELEN 1.250 · GELEN 1,25 · **GELEN 0,05 "Kuruş farkı" (yeni)** · **GIDEN 750 "Kuyumcular Odası Aidatı" (23 karakter, yeni)** hepsi 200 · POS cihazı 200 · 2 KART işçilik 200 · `/pos/tahsilatlar` toplamSatir 2 · `/pos/valor-takvimi` 200 (brüt 4.250, komisyon 85, net 4.165).
- Eşleştirme araması (`GET /arama?tur=BANKA_HAREKET`, MÜDÜR):
```
q=0.050       200 [('Kuruş farkı', '28.09.2026 · +0,05 ₺')]
q=0,05        200 [('Kuruş farkı', '28.09.2026 · +0,05 ₺')]
q=0.05        200 [('Kuruş farkı', '28.09.2026 · +0,05 ₺')]
q=1.250       200 [('Faiz', '… +1,25 ₺'), ('Kira geliri', '… +1.250 ₺'), ('Elektrik', '… −1.250,50 ₺')]
q=103         200 [('Garanti POS yatan', '28.09.2026 · +103.439,43 ₺')]
q=103.439,43  200 [('Garanti POS yatan', …)]   q=103439.43 200 [aynı]
q=1250,5      200 [('Elektrik', '… −1.250,50 ₺')]   q=1.250,00 200 [('Kira geliri', …)]
q=1.2,3       200 []   q=999 200 []   q=Garanti 200 [('Garanti POS yatan', …)]
```
- `GET /hurda/bakiye` rol × şube matrisi (`r5/matris.out`, `r5/iki.out`) — § 00000.5 ile BİREBİR: tek şube: SAHİP 200 · MÜDÜR şubesiz 200 · MUHASEBE şubesiz 200 · TEZGÂHTAR şubesiz 403 ("Şube bağlamı çözülemedi…", fail-closed) · MÜDÜR şubeli 200 · TEZGÂHTAR şubeli 200; iki aktif şube: MÜDÜR/MUHASEBE şubesiz 400 "Hurda işlemleri için şube bağlamı gerekli." (hurda-satis 400 "şube seçimi gerekli"); ikinci şube pasife alınınca MÜDÜR 200 (hurda-satis iş kuralına geçer: 400 "Tahsil edilmeyen 1550 TL için ramatçı carisi zorunludur…").

### 000000.6 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum ŞUBESİZ MÜDÜR, dolu veri, taze profil)
- **Senaryo matrisi** (`r5/sen/sonuc.jsonl`, betik `r5/sen/sahne.mjs` + `kos.sh`): **{1440, 1280, 960, 375} × {BASİT, PRO} × {aydınlık, karanlık} = 16 kombinasyon × 10 sahne = 160 ölçüm** (sahneler: hurda dolu · POS açılış · POS arama "103" · POS arama "999" · POS iptal onayı · banka hareketleri · dekont MASAK 1.250.000 · dekont PASAPORT · **dekont Diğer ülke + "ZZQ" (yeni)** · yeni hesap). 16 koşum rc=0; mod uyumsuz ölçüm 0 (her sonuç istenen modda).

| Ölçü | Sonuç (160 ölçüm) |
|---|---|
| yatay kaydırma | 0 |
| K1, K5, K7, K8, K9b, K10, K10b, K11, K12, K13 | 0 |
| **K3_tasan** | **0** |
| **K3_kirik** | **0** (§ 00000'daki 3 → 0; banka hareket 1280 ×4 ve 960 ×4 dahil) |
| K9_binen | 480 = yalnız açık pencere sahnelerinde (POS iptal onayı, dekont MASAK, PASAPORT, Diğer ülke, yeni hesap × 16): hepsi `arka plan öğesi ⟂ pencere içeriği` (örn. `p.stat-etiket "TOPLAM BAKİYE" ⟂ span.alan-etiket-metin`) — örtü altındaki sayfa ile pencerenin çakışması, ölçüm yanlış pozitifi (§ 0000.6/§ 00000.6 ile aynı); pencere dışı sahnelerde 0 |
| banka tablo kabı (scrollWidth/clientWidth) | 1440 1088/1088 · 1280 928/928 · 960 852/608 · 375 979/315 |
| Diğer ülke sahnesi | ZZQ → "Bu kod bir ülke kodu değil…" 16/16; FRA → hata yok 16/16; etiket BASİT "Pasaport numarası *" 8/8, PRO "Belge No (TCKN / Pasaport) *" 8/8 |

- **tur.mjs** (`r5/tur`, analiz `r5/tur/analiz.txt`; BASİT iki işçi birlikte, sonra PRO iki işçi birlikte — aynı kullanıcıda farklı mod eşzamanlı KOŞULMADI): 3 rota × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375} = **48 sayfa**, 164 katman, hata 0, geçersiz 0. analiz.py tek bulgu: GlobalArama 375 SERBEST-YÜZEN (3 rota; sahibi KATMAN — değişmedi). **Yeni bulgu 0.** ("eksik" satırı analiz.py'nin tam tur eşiğidir; 16 kombinasyonun 16'sı koşuldu, `kos.log` 4 × rc=0.)
- **tutarlilik.mjs 1440** (`r5/arac`): mod farkı **0**, `tutarlilik-fark-1440.txt` **0 bayt**, düğme sınıf grubu 6.
- **dugme-olc**: `ortasiz 0 silik 0 kirpik 0`.
- **Açılış 1440×900** (`r5/acilis/acilis.log`): hurda 0,85 / 0,88 · pos 0,85 / 0,88 · banka 0,55 / 0,55 (BASİT aydınlık / PRO karanlık); birincil düğme her ekranda 1 (değişmedi; BASİT'te belge 933 px = KenarCubugu, sahibi KATMAN).
- Görüntüler (Read ile bakıldı): `ham/TUVAL-KARSILASTIRMA/ZO-ISLEM-A-28e-banka-hareket-K3-ONCE-SONRA-1280.png` (önce: "Garanti POS yatan" ve "Mehmet Kaya Kuyumculuk Ltd. Şti." iki/üç satır; sonra: tek hücrede ad + açıklama, hepsi tek satır, TUTAR/DURUM/eylem görünür), `…-960.png` (960: kısa adlar tek satır, tablo kabında kayar), `ZO-ISLEM-A-28e-masak-diger-kod-BASIT-PRO-1440.png`. Senaryo görüntüleri `r5/sen/foto/` (160).
- Bölge tablosu ek satırı: "Banka hareket tablosu — tuvalde yok · karşı taraf + açıklama tek hücrede alt alta · GEREKÇELİ (K3 tek ölçü; satır yüksekliği ~108 px: iki satır içi düzenleme düğmesi `--dokunma-hedefi` asgari yüksekliğini korur — dokunma hedefi küçültülmedi)".

### 000000.7 Temizlik
API (5170) ve vekil (5171) durduruldu: dinleyen **0**; `pgrep -f ZO-ISLEM-A/r5` **0** (kendi Chrome'ları dahil). `zsh kiraci-temizle.sh 01M3MCERXYG4P2WMVH1ZDTDAW7 --uygula` (bu kez zsh ile) → ikinci çağrı "Kiracı YOK (bağlantı sınandı)"; information_schema'daki tenant_id kolonlu tüm tablolarda bu kiracı için **0 satır**; davet satırı `kiraci_davetleri` 1 silindi (ROW_COUNT 1, `kullanan_tenant_id` eşleşmesiyle). **tenants: başlangıç 19 → son 19.** `PAKETZO-ISLEM-A-HAKEM-TEST` ve QA-TEST kiracılarına DOKUNULMADI.

### 000000.8 Bu turda değişen dosyalar
`dotnet/Kuyumcu.Web/Pages/Banka.razor` (hareket tablosu: karşı taraf + açıklama tek hücre, başlık 6 sütun) · `dotnet/Kuyumcu.Web/wwwroot/css/zo-islem.css` (+3 kural) · `dotnet/Kuyumcu.Web/Bilesenler/MasakFormu.razor` (BASİT belge no etiketi, uyruk kodu hata bağı, `Eksiksiz` ISO denetimi) · `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoIslem.cs` (+4 alan TR+EN; `UlkeListesi.IsoKodlari` + `KodGecerli`) · `dotnet/Kuyumcu.Api/Modules/Arama/AramaModulu.cs` (binlik grubu 0 ile başlamaz; "0.050" tek önek) · `dotnet/Kuyumcu.Tests/EkranTestleri.cs` (+2) · `dotnet/Kuyumcu.Tests/ZOISLEMAAramaTestleri.cs` (+5 veri) · `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (tablo testi güncellendi). Dokunulan sayfa: Banka (aynı paketin hakem düzeltmesi). Hurda.razor, PosTahsilatlar.razor, Pos.razor DEĞİŞMEDİ. Migration YOK (gerekmedi).

### 000000.9 DoD sözleşmesi (bu tur)
- Yeni varlık / kayıt türü YOK. Banka hareketi finansal kayıt: karşı taraf / açıklama satır içi düzenleme (değişmedi, aynı kimlikler), tutar/yön storno ile. MASAK kimlik tespiti append-only (DegismezIpucu, değişmedi). Arama ucu salt okunur.
- **Mehmet (06) / Taşkın (03) notu:** uyruk kodu ISO denetimi şimdilik YALNIZ istemcide (MasakFormu). Sunucu `MasakBilgiDto.Uyruk` yalnız `[MaxLength(3)]`; aynı `UlkeListesi.KodGecerli` (Kuyumcu.Core, sunucudan da erişilebilir) `MasakKapisi`'nda çağrılmalı — Masak modülü bu paketin dosya alanı dışında, dokunulmadı. § 00000.9 uyruk zorunluluğu (mali müşavir) notu değişmedi.

### 000000.10 Açık kalanlar (sahibiyle)
- Tam süit: ORTAK-1 `O8` (15↔16); Auth/P-07 `B10_esZamanli_5_istek…` kararsız (tek başına 5/5 yeşil).
- Sunucu tarafı uyruk ISO denetimi → Mehmet/Taşkın (Masak modülü, § 000000.9).
- GlobalArama 375 SERBEST-YÜZEN, KenarCubugu BASİT 1440 belge 933 px → KATMAN (değişmedi). ModalPencere 375'te pencere içi kaydırma (ortak bileşen).
- Banka hareket tablosu 960'ta DURUM/eylem sütunları tablo kabında kaydırmayla görünür (sayfa kaymaz; § 0000'dan beri aynı karar).
- Banka listesinde GIDEN tutar ASCII "-" (POS aramasındaki "−" ile tek tip değil) — § 00000.10'dan devam.
- `kiraci-temizle.sh` bash kalkanı önerisi (§ 00000.7) → orkestratör.
- § 0000.10 / § 00000.10'daki diğer maddeler (FaturaAra biçimi, "İptal" vazgeç, SubeBelirle fallback → Bertan/Taşkın, /hurda menü grubu → kurucu/mimar) değişmedi.

---

## 00000. SON DOĞRULAMA (3. tur) KALAN BULGULARININ KAPATILMASI — 28.09.2026 16:55–17:45 (tarihçe; geçerli durum § 000000) (t7 · API 5170 · vekil 5171 · CDP 9803/9806–9809)
Durum beyanı: **BITTI**. Bu bölüm § 0000'ı günceller; § 0000'daki ölçümler bu turda GÜNCEL ağaçta yeniden koşuldu (aşağıda). Sıra: içerik değişikliği → derleme → mutasyon (M35–M41) → temiz derleme → canlı + tarayıcı ölçümleri → temizlik → tam süit + denetim.

### 00000.1 Devralma
- `Banka.razor` ve `AramaModulu.cs` mtime 16:51 = doğrulayıcının (dog-zoia mA/mB) mutasyon geri yüklemesi. İkisi de benim 16:22 son hâlimle BİREBİR (`cmp mut-yedek/ZO-ISLEM-A/M25/orijinal` ve `M32|M33/orijinal` → AYNI). Yarım mutasyon YOK. 5170/5171 boş, tenants **18**.
- Önceki "SON DOĞRULAMA (GEÇTİ)" bulgu listesi (IBAN'dan önce 'Daha fazla', TUTAR görünmüyor, Hurda geliştirici notu, POS kilit notu jargonu, 'Banka hareketi kimliği', kırmızı dolu yıkıcı düğmeler, açıklama satırları, native select'ler) § 000/§ 0000'da kapatılmıştı; koda karşı yeniden bakıldı: Banka yeni hesap sırası `hesap-iban` (300) → `hesap-sahibi` (315, açıcı arkasında); Hurda.razor'da K-13 metni yalnız razor YORUMUNDA; POS'ta "Düzeltme şöyle"/"Banka hareketi kimliği" 0 eşleşme.

### 00000.2 Bulgu → düzeltme
| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| [DÜŞÜK] Dekont "Yön" zorunlu ama "(isteğe bağlı)" yazıyor | `Banka.razor` `<Secim ElemanId="dekont-yon" … Zorunlu="true">` (ortak `Secim` bileşenine dokunulmadı; parametre zaten vardı). | tarayıcı: etiket "Yön *", `.alan-istege` 0, `aria-required="true"` (1440 BASİT aydınlık, 375 PRO karanlık + 7 kombinasyon senaryosu); `ZO28c_dekont_YON_zorunlu_isaretli_istege_bagli_yazmaz`; M35 |
| [DÜŞÜK] MASAK uyruk varsayılanı "TUR" (yabancı müşteri sessizce Türk) | **Fail-closed varsayılan**: uyruk "TUR" YALNIZ belge türü T.C. kimlik kartı iken çıkarılır (`MasakFormu.VarsayilanUyruk`). Pasaport / mavi kart / sürücü belgesi / vergi levhasında kullanıcı elle seçmediyse uyruk BOŞ gelir; seçici "Ülke seçin" der, altında "Müşterinin uyruğunu seçin." Elle seçilen uyruk belge türü değişince EZİLMEZ. Uyruğun YASAL zorunluluğu koda yazılmadı (mevzuat hükmü değil, yalnız yanlış veri üretmeyen varsayılan) → Mehmet / mali müşavir teyidi notu § 00000.9. Sunucu uyruk boşken kaydı kabul ediyor (değişmedi). | canlı: PASAPORT + uyruk yok → 200, `masak_kimlik_tespitleri` PASAPORT/NULL, TC_KIMLIK/TUR; tarayıcı 7 kombinasyon: belge "Pasaport" → uyruk "Ülke seçin", ipucu "Müşterinin uyruğunu seçin."; `ZO28c_uyruk_TUR_yalniz_TC_kimlikte_PASAPORTta_BOS_elle_secilen_EZILMEZ`; M36, M37, M41 |
| [DÜŞÜK] `MasakFormu.razor:17` satır içi style | `<h3 class="zo-masak-baslik">` + `zo-islem.css` `.zo-masak-baslik { margin: 0; }` (index.html zo-islem.css'i her sayfada yükler → İşlemler çağıranı da aynı görünür). | denetim `SATIR_ICI_STYLE` 75 → **74**; tarayıcı `style` null, `margin-top: 0px`; M40 |
| [DÜŞÜK] Tutar önekinde nokta yorumu tutarsız ("1.250" hep binlik, "1.2,3" → "12.3") | (1) Yeni `AramaServisi.TutarOnekleri`: TEK noktalı binlik yazım ("1.250") belirsizdir → binlik (1250) VE ondalık (1,25; kolon 2 ondalıklı olduğu için "1.25" öneki) yorumlarının İKİSİ de aranır (VEYA). Üçüncü ondalık 0 değilse ("1.255") ondalık yorum olamaz → tek önek. Çok noktalı ya da virgüllü yazım belirsiz değil. (2) Virgüllü yazımda noktalar yalnız geçerli binlik grubu olabilir: "1.2,3" → null (yalnız metin araması). `TutarOnekiCoz` imzası ve doğrulayıcının `ZOS3_TutarOnekiCoz_sinirlar` verileri DEĞİŞMEDİ. | canlı `q=1.250` → Faiz +1,25 ₺ · Kira geliri +1.250 ₺ · Elektrik −1.250,50 ₺ (önek eşleşmesi); `q=1.250,00` → yalnız Kira geliri +1.250 ₺; `q=1.2,3` → []; SQL `CAST(tutar AS CHAR)` 1.25 / 1250.00 / 1250.50; `ZO28c_belirsiz_1_250_IKI_yorumu_da_bulur_virgullu_yazim_TEK_yorum`, `ZO28c_tutar_onekleri_belirsiz_yazimda_IKI_yorum` (7 veri), `ZO28b_tutar_oneki_cozumu("1.2,3")`; M38, M39 |
| [DÜŞÜK] HTTP arama testinin beklenen değeri üretim koduyla | `ZO28b_banka_hareketi_TUTARLA_aranir…`: `"+103.439,43 ₺"` ve `"−1.250,50 ₺"` SABİT metin; yeni HTTP testi de sabit `"+1.250 ₺"`. (Kurucu "testte sabit para metni yazma" kuralı ekran testleri içindir; burada sunucu biçimi ile ekran biçiminin eşitliği ayrıca `ZO28b_sunucu_ekran_parasi_Bicim_Para_ile_BIREBIR` ile bağlı kalır — sabit metin ORAKIL, Bicim karşılaştırması SÖZLEŞME.) | filtreli 136/136 yeşil |
| [KRİTİK] HAKEM ÇELİŞKİSİ — K3 (banka hareketi) | **Sayı yeniden ölçüldü, iki rapor aynı gerçeği yazsın diye K3 İKİ ALT ÖLÇÜYLE yazılıyor.** `sikisma.js` K3'ü ikiye böler: `K3_tasan` (kabından taşan metin) ve `K3_kirik` (≤ 24 karakterlik kısa metnin iki satıra inmesi). Güncel ağaçta (senaryo matrisi, dolu veri, 72 ölçüm): **K3_tasan = 0 her ölçümde** (doğrulayıcının "K3 0"ı budur); **K3_kirik = 3 yalnız /banka hareket tablosu 1280 ve 960'ta** (1440 ve 375'te 0), üçü de AYNI fiziksel hücre (`td.zo-hucre-dar` → içindeki `span.satir-ici` → satır içi düzenleme düğmesi) "Garanti POS yatan" — üç kelimelik karşı taraf adı kelime arasından iki satıra sarıyor (kelime ortasından kırılma YOK). Tablo 1280'de `.tablo-sar` 928 = 928 (kaydırmasız), 960'ta 868 > 608 (kabında kayar, sayfa kaymaz). Gerekçe (değişmedi): bu sütun 28.09 sabah KRİTİK bulgusu (1440'ta TUTAR görünmüyordu) için bilerek sarmalı yapıldı; 1280'de asgari genişlik vermek tabloyu kaba sığmaz hâle getirip TUTAR/DURUM'u kaydırmaya iter. Veri hücresi, etiket değil. tur.mjs 48 sayfa taramasında (boş hareket kartı kapalı açılış) K3 = 0/0. | `r4/sen/sonuc.jsonl` (banka-hareket 1280 BASİT aydınlık/karanlık: tasan 0, kırık 3; 960 PRO aydınlık/BASİT karanlık: 0/3; 1440 ×2 ve 375 ×2: 0/0) |

### 00000.3 Dosya alanı dışı (DUR-VE-YAZ kaydı)
- `Kuyumcu.Web/Bilesenler/MasakFormu.razor` — § 0000.3'teki gerekçeyle (doğrulayıcı bulgusu doğrudan bu bileşende). Değişiklik: belge türü değişince uyruk varsayılanı, "Ülke seçin" yer tutucu, h3 sınıfı. İşlemler (SATIS-3) çağıranında POST gövdesi yalnız pasaportlu müşteride farklılaşır (uyruk "TUR" yerine boş/seçilen) → orkestratör kapı kaydına.
- `Kuyumcu.Api/Modules/Arama/AramaModulu.cs` — yalnız banka hareketi araması (§ 0000.3 ile aynı kapsam).
- `Kuyumcu.Tests/EkranTestleri.cs` — yalnız `MasakFormuEkranTestleri` sınıfına 1 test.
- DOKUNULMAYAN: app.css, Sozlukler HUB, Secim/Secici/AlanKabugu (ortak), Katlanir, Islemler*, Fis/*.

### 00000.4 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra, 17:24) → **0 Hata, 1 Uyarı** (`EtiketEkranTestleri.cs(145)` xUnit1031 — bu paketin değil). Ara not: 17:11–17:20 arasında SATIS-3'ün `Pages/Islemler.Odeme.cs` dosyası `using Kuyumcu.Web.Servisler` eksikliğiyle derlemeyi kırdı (CS0103 `Bicim` ×2); o şerit 17:20'de düzeltti, bu paket o dosyaya dokunmadı. 17:23'teki bir derleme başka şeridin eşzamanlı derlemesi yüzünden `GenerateWasmBootJson` dosya kilidiyle (MSB4018) düştü; API durduruldu, yeniden derlendi (YEŞİL), API yeniden başlatıldı — bütün tarayıcı ölçümleri bu yeşil derlemeden sonra.
- Yeni testler bu turda **4 yöntem** (+ 9 veri satırı): `ZOISLEMAEkranTestleri.ZO28c_dekont_YON_zorunlu_isaretli_istege_bagli_yazmaz` · `MasakFormuEkranTestleri.ZO28c_uyruk_TUR_yalniz_TC_kimlikte_PASAPORTta_BOS_elle_secilen_EZILMEZ` · `ZOISLEMAAramaHttpTestleri.ZO28c_belirsiz_1_250_IKI_yorumu_da_bulur_virgullu_yazim_TEK_yorum` (gerçek HTTP + MySQL) · `ZOISLEMAAramaSafTestleri.ZO28c_tutar_onekleri_belirsiz_yazimda_IKI_yorum` (7 veri) + `ZO28b_tutar_oneki_cozumu`'na 2 veri ("1.2,3" → null, "12.345,6"). HTTP testi sabit metne çevrildi.
- Filtreli: `ZOISLEMA|MasakFormuEkran|ZOISLEMASinir` **136/136**; geniş (`ZOISLEMA|MasakFormuEkran|Masak|Banka|Islemler|Arama|Pos|Hurda`) **820/821** — tek kırmızı `BasitModPosTestleri.Iki_kasa_varken_secici_GORUNUR` (`#islem-kasa` bulunamadı; SATIS-3'ün 17:19–17:20'de düzenlediği `Fis/FisOdemeKasa` alanı) → tam süitte (17:39) YEŞİL.
- Mutasyonlar (betik `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r4/mut.sh` + `mutlar.sh`, yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/M35…M41/`, günlük `r4/mut-r4.log`; her birinde derleme 0 Hata, geri yükleme `cmp` AYNI):

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M35 | Banka `dekont-yon` `Zorunlu="true"` silindi | `ZO28c_dekont_YON_zorunlu_isaretli_istege_bagli_yazmaz` | AYNI |
| M36 | `VarsayilanUyruk` her belge türünde "TUR" | `ZO28c_uyruk_TUR_yalniz_TC_kimlikte_PASAPORTta_BOS_elle_secilen_EZILMEZ` | AYNI |
| M37 | `if (!uyrukElle && !uyrukDiger)` → `if (!uyrukDiger)` (elle seçilen ezilir) | aynı test | AYNI |
| M38 | `TutarOnekleri` ikinci (ondalık) önek kaldırıldı | `ZO28c_tutar_onekleri…("1.250")`, `…("12.340")`, `ZO28c_belirsiz_1_250_IKI_yorumu…` (HTTP) | AYNI |
| M39 | Virgüllü yazımda binlik grup denetimi kapatıldı (`if (false && …)`) | `ZO28b_tutar_oneki_cozumu(girdi: "1.2,3", beklenen: null)` | AYNI |
| M40 | h3 sınıfı → eski `style="margin:0"` | `ZO28c_uyruk_TUR_yalniz_TC…` (style null + sınıf beklentisi) | AYNI |
| M41 | uyruk seçicisinden `YerTutucu` silindi ("Henüz kayıt seçilmedi." görünür) | `ZO28c_uyruk_TUR_yalniz_TC…` | AYNI |

- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-28d.trx`, 17:39→17:42): **6647 test, 6642 geçti, 5 başarısız**; bu paketin testlerinin hepsi yeşil. Kırmızıların hiçbiri bu paketin değil:
  - `KarmaOdemeEkranTestleri.Bacak_toplami_tutmuyorsa_KAYDET_KAPALI` ("birebir eşit olmalıdır" yok), `KarmaOdemeEkranTestleri.Had_asilinca_NAKIT_bacak_KAYDI_ENGELLER` ("NAKİT bacak eklenemez" yok), `SATIS3EkranTestleri.T3_Takas_yeni_urun_TEK_arama_kutusundan…(BASIT|PROFESYONEL)` → **SATIS-3** (Islemler.Odeme.cs / Fis/* 17:11–17:31 arası düzenleniyordu; bu paket dokunmadı).
  - `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (15 ↔ 16) → **ORTAK-1** (§ 0000 ve § 000'da da vardı).
  - § 0000'daki `.mutorj` iki kırmızısı ve EMANET-1 `X5` bu koşumda YEŞİL (sahipleri kapatmış).
  - **FARK (bu paketin yeni kırmızısı) = 0.**
- `bash ./denetim.sh` (17:37) → **DENETİM TEMİZ.** Bu paketin sayaçları: ModalPencere 48/55 (artış 0), duzenlenenId 1/1, SATIR_ICI_STYLE **74** (75'ten −1, tavan 143), SABIT_RENK 0, HAM_INPUT 32/42.

### 00000.5 Canlı doğrulama (5170, test kiracısı `PAKETZO-ISLEM-A-TEST` 01M3M5HEYJ2EA9Z8STP8D386SH; sistem anahtarı rastgele üretilip yalnız süreç ortamına verildi; parolalar chmod 600 fikstürde, çıktıya yazılmadı; yanıt gövdeleri okundu)
- Hazırlık: davet 201 · kayıt 201 · giriş 200 · şubeler 200 · MÜDÜR (şubesiz) 201 · kasa 200 · kasa GİRİŞ 60.000 200 · cari 201 · kur 200 · `hurda-alis` (şubesiz MÜDÜR) 200 has 9,16 · `hurda-satis` 2,5 has 200 · `GET /hurda/bakiye` 200 6,66 g · banka hesabı 200 · dekont GELEN 2.456.789,45 (MASAK TC_KIMLIK/TUR) 200 · GIDEN 1.250,50 200 · GELEN 103.439,43 200 · GELEN 1.250 200 · GELEN 1,25 200 · POS cihazı 200 (yanıt artık `{cihaz:{…}}` sarmalında — başka şeridin değişikliği; betik `GET /pos/cihazlar` ile devam etti) · 2 KART işçilik 200 · `/pos/tahsilatlar` toplamSatir 2 · `/pos/valor-takvimi` 200 (brüt 4.250, komisyon 85, net 4.165).
- **Pasaportlu MASAK dekontu**: `POST /bankalar/{id}/hareket` GELEN 1.250.000, masak `{belgeTuru: PASAPORT, …}` uyruk YOK → **200**; SQL `masak_kimlik_tespitleri`: `TC_KIMLIK TUR` · `PASAPORT NULL`.
- **Eşleştirme araması** (`GET /arama?tur=BANKA_HAREKET`, MÜDÜR jetonu):
```
q=103          200 [('Garanti POS yatan', '28.09.2026 · +103.439,43 ₺')]
q=103.439,43   200 [('Garanti POS yatan', '28.09.2026 · +103.439,43 ₺')]
q=103439.43    200 [('Garanti POS yatan', '28.09.2026 · +103.439,43 ₺')]
q=1250,5       200 [('Elektrik', '28.09.2026 · −1.250,50 ₺')]
q=1.250        200 [('Faiz', '28.09.2026 · +1,25 ₺'), ('Kira geliri', '28.09.2026 · +1.250 ₺'), ('Elektrik', '28.09.2026 · −1.250,50 ₺')]
q=1.250,00     200 [('Kira geliri', '28.09.2026 · +1.250 ₺')]
q=1.2,3        200 []
q=999          200 []
q=Garanti      200 [('Garanti POS yatan', '28.09.2026 · +103.439,43 ₺')]
```
- SQL (`banka_hareketleri`, bu kiracı, `yon tutar CAST(tutar AS CHAR) karsi_taraf_ad durum`): `GELEN 1.25 1.25 Faiz BEKLIYOR` · `GELEN 1250.00 1250.00 Kira geliri` · `GIDEN 1250.50 1250.50 Elektrik` · `GELEN 103439.43 103439.43 Garanti POS yatan` · `GELEN 2456789.45 … Mehmet Kaya Kuyumculuk` (+ pasaportlu 1.250.000).
- `GET /hurda/bakiye` rol × şube matrisi (güncel ağaçta yeniden koşuldu, `r4/matris.out` + `r4/iki.out`) — § 0000.5 ile BİREBİR: tek şube: SAHİP 200 · MÜDÜR şubesiz 200 · MUHASEBE şubesiz 200 · TEZGÂHTAR şubesiz 403 ("Şube bağlamı çözülemedi…", fail-closed) · MÜDÜR şubeli 200 · TEZGÂHTAR şubeli 200; iki aktif şube: MÜDÜR/MUHASEBE şubesiz 400 "Hurda işlemleri için şube bağlamı gerekli." (hurda-satis 400 "şube seçimi gerekli"); ikinci şube pasife alınınca MÜDÜR 200 (hurda-satis iş kuralı kapısına geçer: 400 "Tahsil edilmeyen 1550 TL için ramatçı carisi zorunludur").

### 00000.6 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum ŞUBESİZ MÜDÜR, dolu veri) — hepsi 17:24 yeşil derlemesinden SONRA
- **Senaryo matrisi** (`r4/sen/sonuc.jsonl`, 9 sahne × 8 kombinasyon = **72 ölçüm**: {BASİT aydınlık 1440, PRO aydınlık 1440, BASİT aydınlık 1280, BASİT karanlık 1280, PRO aydınlık 960, BASİT karanlık 960, BASİT aydınlık 375, PRO karanlık 375}; sahneler: hurda dolu · POS açılış · POS arama "103" · POS arama "999" · POS iptal onayı · banka hareketleri · banka dekont MASAK 1.250.000 · **banka dekont PASAPORT (yeni)** · banka yeni hesap):

| Ölçü | Sonuç |
|---|---|
| yatay kaydırma | 0 / 72 |
| K1, K5, K7, K8, K10, K11, K12, K13 | 0 / 72 |
| K3_tasan | 0 / 72 |
| K3_kirik | 3 yalnız banka hareket tablosunun göründüğü sahnelerde 1280 ×2 ve 960 ×2 (aynı "Garanti POS yatan" hücresi; § 00000.2 gerekçe); 1440 ve 375'te 0 |
| K9_binen | 6 yalnız açık pencere (modal) sahnelerinde: `arka plan öğesi ⟂ #modal-…` (arka plan ⟂ modal katmanı ölçüm yanlış pozitifi — § 0000.6/§ 000.6 ile aynı); sayfa ölçümünde 0 |
| dekont Yön | "Yön *", `.alan-istege` 0, `aria-required=true` (8/8) |
| PASAPORT sahnesi | belge "Pasaport", uyruk "Ülke seçin", ipucu "Müşterinin uyruğunu seçin.", h3 `style` null, `margin-top 0px` (8/8) |

- Ek sahne (`r4/sen2`): dekont penceresi üstü 1440 BASİT aydınlık ve 375 PRO karanlık → "Yön *", K3/K7/K10 0, yatay 0.
- **tur.mjs** (`r4/tur` + `r4/tur2` → birleşik `r4/tur3/analiz.txt`): 3 rota × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375} = **48 sayfa**, 158 katman, hata 0, geçersiz 0; 48 sayfanın hepsinde K1/K3/K5/K7/K8/K9/K10/K11/K12/K13 = 0, yatay 0. analiz.py tek bulgu: GlobalArama 375 SERBEST-YÜZEN (3 rota; sahibi KATMAN — değişmedi). **Yeni bulgu 0.** Not: ilk BASİT aydınlık koşusu (17:27–17:32) sırasında başka şeritler `olcek.css` (17:28) ve `tuval-satis.css` (17:31) dosyalarını değiştirdi; o koşuda 13 geçici bulgu (katlanır başlık rozeti kırığı, 13 px yazı) çıktı. Aynı kombinasyon 17:34'te TEK başına yeniden koşuldu → 12 sayfa 0 bulgu; birleşik analiz o yeniden koşuyu kullanır (ilk koşu `r4/tur/analiz.txt` iz olarak durur).
- **tutarlilik.mjs 1440** (`r4/arac`): BASİT↔PRO eleman farkı **0** (`tutarlilik-fark-1440.txt` 0 bayt), mod farkı 0, düğme sınıf grubu 6.
- **dugme-olc**: `ortasiz 0 silik 0 kirpik 0`.
- **Açılış 1440×900** (`r4/acilis/acilis.log`): hurda 0,85 / 0,88 · pos 0,85 / 0,88 · banka 0,55 / 0,55 (BASİT aydınlık / PRO karanlık); birincil düğme her ekranda 1.
- Görüntüler (Read ile bakıldı) `ham/TUVAL-KARSILASTIRMA/`: `ZO-ISLEM-A-28d-masak-uyruk-ONCE-SONRA-1440.png` (önce: uyruk hep "Türkiye"; sonra: Pasaport → "Ülke seçin" + ipucu), `ZO-ISLEM-A-28d-dekont-yon-zorunlu-1440-375.png` ("Yön *" 1440 aydınlık ve 375 karanlık). Senaryo görüntüleri `r4/sen/foto/` (72).
- Bölge tablosu ek satırı: "MASAK uyruk seçicisi — tuvalde yok · belge türüne bağlı varsayılan · GEREKÇELİ (yasal kapı, özellik silinmez; yanlış veri üretmeyen varsayılan)".

### 00000.7 Temizlik
API (PID 72292 → yeniden başlatılan süreç) ve vekil durduruldu: 5170/5171 dinleyen **0**; `pgrep -f ZO-ISLEM-A/r4/` **0**. `kiraci-temizle.sh 01M3M5HEYJ2EA9Z8STP8D386SH --uygula` **yanlışlıkla bash ile** çağrıldı: betik zsh'e özgü `${=TABLOLAR}` kullandığı için tablo döngüsü çalışmadı, yalnız `tenants` satırı silindi (ikinci çağrı "Kiracı YOK" dedi) ve information_schema taraması 70 tabloda **734 artık satır** gösterdi (Madde 1: gizlenmedi). Aynı silme mantığı zsh ile koşuldu → tenant_id kolonlu **70 tabloda kalan 0**; davet satırı `kiraci_davetleri` 1 silindi (ROW_COUNT 1). tenants: başlangıç 18 → şimdi 19; fark başka şeritlerin bu süre içinde açtığı kiracılar (`PAKETSATIS-3-TEST` 14:06Z, `PAKETEMANET-1-TEST Hakem` 14:29Z) ve başka bir şeridin kendi silmesi — bu paketin katkısı 0. `PAKETZO-ISLEM-A-HAKEM-TEST` ve QA-TEST kiracılarına DOKUNULMADI. (Orkestratöre not: `kiraci-temizle.sh` bash ile çağrılınca sessizce yarım siler — başına `[ -n "$ZSH_VERSION" ] || exec zsh "$0" "$@"` gibi bir kalkan önerilir; betik bu paketin alanında değil, değiştirilmedi.)

### 00000.8 Bu turda değişen dosyalar
`dotnet/Kuyumcu.Web/Pages/Banka.razor` (dekont-yon Zorunlu) · `dotnet/Kuyumcu.Web/Bilesenler/MasakFormu.razor` (uyruk varsayılanı, yer tutucu, h3 sınıfı) · `dotnet/Kuyumcu.Api/Modules/Arama/AramaModulu.cs` (`TutarOnekleri`, virgüllü yazım denetimi) · `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoIslem.cs` (+2 alan TR+EN: `MasakUyrukSecin`, `MasakUyrukYerTutucu`) · `dotnet/Kuyumcu.Web/wwwroot/css/zo-islem.css` (+1 kural) · `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (+1) · `dotnet/Kuyumcu.Tests/ZOISLEMAAramaTestleri.cs` (+2 yöntem, sabit metin, +2 veri) · `dotnet/Kuyumcu.Tests/EkranTestleri.cs` (MasakFormuEkranTestleri +1). Dokunulan sayfa: Banka (aynı paketin doğrulayıcı düzeltmesi). Hurda.razor ve PosTahsilatlar.razor bu turda DEĞİŞMEDİ.

### 00000.9 DoD sözleşmesi (bu tur) + notlar
- Yeni varlık / kayıt türü YOK, migration YOK (gerekmedi). Arama ucu salt okunur (GET). MASAK kimlik tespiti append-only yükümlülük kaydı (DegismezIpucu tek cümle, § 0000.11); banka hareketi finansal — storno + karşı taraf/açıklama satır içi düzenleme (değişmedi).
- **Mehmet (06 Güvenlik) + mali müşavir teyidi**: yabancı uyruklu gerçek kişide uyruk bilgisinin MASAK kimlik tespitinde ZORUNLU olup olmadığı (Suç Gelirlerinin Aklanmasının ve Terörün Finansmanının Önlenmesine Dair Tedbirler Hakkında Yönetmelik, kimlik tespiti maddesi) bu turda birincil kaynaktan DOĞRULANMADI → koda zorunluluk yazılmadı; yalnız yanlış veri üretmeyen varsayılan (boş) uygulandı. Zorunlu çıkarsa kural sunucuda (`MasakKapisi`) yapılandırmadan okunmalı, istemciye gömülmemeli.

### 00000.10 Açık kalanlar (sahibiyle)
- Tam süit: SATIS-3 (`KarmaOdemeEkranTestleri` ×2, `SATIS3EkranTestleri.T3_Takas…` ×2 — şerit düzenleme ortasında), ORTAK-1 (`O8`).
- GlobalArama 375 SERBEST-YÜZEN → KATMAN (değişmedi). ModalPencere 375'te sayfa başlığı pencere üstünde görünüyor (ortak bileşen; § 0000 ile aynı).
- Banka hareket listesinde GIDEN tutar hâlâ ASCII "-" ile ("−" değil) — POS aramasındaki "−" ile tek tip değil; görsel tutarlılık, bir sonraki Banka dokunuşunda (kural değil).
- `kiraci-temizle.sh` bash kalkanı → orkestratör (§ 00000.7).
- § 0000.10'daki diğer maddeler (FaturaAra "0.00 TRY" biçimi → ZO-BELGE/arama sahibi; Pos.razor/BankaEkstre.razor "İptal" vazgeç; uyruk seçicisinde "(isteğe bağlı)" soneki ortak Secici çizmiyor; SubeBelirle "fallback yok" → Bertan/Taşkın; /hurda menü grubu → kurucu/mimar) değişmedi.

---

## 0000. 2. TASARIM HAKEMİ TURU BULGULARININ KAPATILMASI — 28.09.2026 15:32–16:31 (tarihçe; geçerli durum § 00000) (t7 · API 5170 · vekil 5171 · CDP 9803/9806/9807/9808/9809)
Durum beyanı: **BITTI**. Bu bölüm § 000'ı günceller. Aşağıdaki bütün ölçümler son İÇERİK değişikliğinden SONRA alındı (sıra: § 0000.7).

### 0000.1 Devralma
- Ağaç § 000'daki gibiydi; tek fark `Hurda.razor` mtime 14:58 — doğrulayıcının (dv-zoa) mutasyon geri yüklemesi: `dv-zoa/mut.log` M1/M5/DV1 hepsi `cmp=AYNI`. Yarım mutasyon YOK. `mut-yedek/ZO-ISLEM-A/M15…M22` yedekleri yerinde.
- 5170/5171 boştu; tenants başlangıç **18**.

### 0000.2 Bulgu → düzeltme
| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| [KRİTİK] POS eşleştirmede banka yatması ters işaretle ve ham biçimde ("-103439.43 TRY"); tutarla arama çalışmıyor | `AramaModulu.BankaHareketAra`: yön `GELEN` → "+", `GIDEN` → "−" (U+2212); alt bilgi `EkranPara` (Web `Bicim.Para` ile BİREBİR aynı kural, testle bağlı); sayısal sorgu `TutarOnekiCoz` ile veritabanı biçimine çevrilip tutarın BAŞINDAN eşleşir (`EF.Functions.Like(CAST(tutar AS char), '103%')`), metin aramasıyla VEYA'lanır. `KayitSecici` ortak bileşenine iki İSTEĞE BAĞLI parametre (varsayılan davranış AYNI): `AraDugmesiYok` (ayrı "Ara" düğmesi çizilmez — arama zaten yazdıkça çalışıyordu) ve `BosMesaj`; POS: "Bu tutarda banka hareketi bulunamadı." | canlı `q=103` / `103.439,43` / `103439.43` → "Garanti POS yatan · 28.09.2026 · +103.439,43 ₺"; `1250,5` → "−1.250,50 ₺"; `999` → []; tarayıcıda 7 kombinasyonda "+103.439,43 ₺", "Ara" düğmesi yok, 999'da mesaj; M23, M24, M25, M27 |
| [KRİTİK] "Tahsilatı iptal et" onayında vazgeç düğmesi "İptal"; üç farklı ad ve iki farklı düzen | POS iptal onayı ortak OnayKipi düzeninde: `.satir` içinde SOLDA `btn-ikincil esit-pay` "Vazgeç", SAĞDA `btn-tehlike esit-pay` eylem (kimlikler `pos-iptal-vazgec`/`pos-iptal-onayla` DEĞİŞMEDİ). Banka'nın beş penceresi (Yeni Hesap, Dekont Gir, Faturaya dönüştür, Kullanımdan kaldır, Storno) "İptal" → "Vazgeç" (`ZoIslemSozlugu.Vazgec` TR "Vazgeç" / EN "Cancel"); hepsi zaten ikincil-önce sıradaydı. Hurda OnayKipi kullanıyor (değişmedi). | tarayıcı 7 kombinasyon: vazgeç solda (1440: x 488 < 726), metin "Vazgeç"; dekont ve yeni hesap penceresinde "İptal" adlı düğme 0; M26, M32 |
| [KRİTİK] POS Valör Takvimi 960/375'te NET/YATAN/KALAN görünmüyor | 1100 px altında satır KART olur: üstte tarih · cihaz, altında 24 px kalın KALAN, sonra Banka ve diğer tutarlar kendi etiketiyle (`data-etiket` + `::before`). Hiçbir sütun silinmedi. Kimlik seçici `#pos-t-valor` — ilk denemede sınıf seçicisi olcek.css'in `html[data-mod] .ana-icerik .veri-tablosu > tbody > tr > td` kuralına yeniliyordu (gerçek tarayıcıda KALAN büyümüyor, satır boyu boşluk bırakıyordu) → düzeltildi. Tablo yine `.tablo-sar` içinde (P-39). | 960: KALAN x 309–919 görünür, `.tablo-sar` 642=642; 375: 29–346, 349=349; 1440/1280 tablo aynen (1122=1122, 962=962); görüntü `ZO-ISLEM-A-28c-pos-valor-ONCE-SONRA-960-375.png`; M30 |
| [KRİTİK] Banka dekontu MASAK bölümü: mevzuat anlatısı + "Düzeltme şöyle yapılır" tekrarı + ham ISO kodu + BASİT'te "MASAK eşiği" + aynı uyarı iki kez | (1) Kilit kutusu TEK cümle: "Kimlik bilgisi kaydedildikten sonra değiştirilemez; yanlışsa yeni işlemle doğrusunu kaydedin." (AlternatifYol kaldırıldı). (2) Uyruk: Türkiye varsayılanlı ülke SEÇİCİSİ (32 ülke, ad görünür, gövdeye ISO alpha-3 KOD gider; listede yoksa "Diğer ülke" 3 harfli kod kutusunu açar — giriş engellenmez). (3) `MasakFormu` ortak bileşenine İSTEĞE BAĞLI `Sade` parametresi (varsayılan AYNI → İşlemler etkilenmez): Banka BASİT'te başlık "Kimlik bilgisi gerekli (tutar yüksek)", rozet ve açıklama tekrarı yok. (4) MASAK kapısı açıkken müşteri alanının altındaki ipucu çizilmez (uyarı kutusu söylüyor); BASİT'te ipucu jargonsuz. (5) BASİT kilit sebebi "Kaydetmek için kimlik bilgilerini doldurun." (6) 3. tur gerçek tarayıcının bulduğu: 512 px pencerede üç sütunlu MASAK alanları etiketi kırıp kutuları kaydırıyordu (K7 1, "T.C. kimlik kartı" üç noktalı K10 1) → yalnız bu pencerede tek sütun (`.zo-masak-dikey`). | tarayıcı: BASİT'te başlık sade, "eşiğ" 0, "Düzeltme şöyle" 0, uyruk "Türkiye", cari ipucu yok, uyarı kutusu 1; K7 0, K10 0 (7 kombinasyon); görüntü `ZO-ISLEM-A-28c-banka-masak-ONCE-SONRA.png`; M28, M29, M33, M34 |
| [DÜŞÜK] POS başlığı menü adından farklı ("POS Hakedişleri" ↔ "POS alacakları") | `SayfaBasi` ve `<PageTitle>` → `MenuAdlari.Icin(Dil.T).PosAlacaklari.Ad` (Hurda ile aynı desen) | tarayıcı: başlık "POS alacakları" = üst bar "POS alacakları"; M31 |
| [DÜŞÜK] /hurda PRO 1440 "Daha fazla" açık görüntü (ölçüm aracı) | Üretim kusuru kanıtı yok (§ doğrulayıcı notu); bu turda 7 kombinasyonda senaryo + 48 sayfalık tur: hurda açılışta detay kapalı. | tur 48 sayfa K = 0 |
| [DÜŞÜK] `MasakFormu.razor` dosya alanı dışı | Bu turda da değişti (uyruk seçici + isteğe bağlı `Sade`). Gerekçe: hakem KRİTİK bulgusu doğrudan bu bileşende. Varsayılan davranış korunur; İşlemler (SATIS-3) çağıranında yalnız uyruk ham koddan ülke seçicisine döner (POST gövdesi yine "TUR"). → orkestratör kapı kaydına. | MASAK testleri (MasakFormuEkran*, Masak* filtre ve tam süit) yeşil |
| [DÜŞÜK] `PAKETZO-ISLEM-A-HAKEM-TEST` kiracısı | Hakemin artığı; uygulayıcı DOKUNMADI (görev kuralı: yalnız kendi test kiracısı). → hakem/orkestratör `kiraci-temizle.sh`. | tenants listesinde duruyor |

### 0000.3 Dosya alanı dışına çıkılan yerler (DUR-VE-YAZ kaydı)
- `Kuyumcu.Api/Modules/Arama/AramaModulu.cs` — yalnız `BankaHareketAra` + iki yardımcı (`TutarOnekiCoz`, `EkranPara`); başka türlerin araması DEĞİŞMEDİ (Fatura alt bilgisi hâlâ "0.00 TRY" biçiminde → sahibine not). Eşzamanlı şerit listesinde bu dosyanın sahibi yok.
- `Kuyumcu.Web/Bilesenler/KayitSecici.razor` — iki isteğe bağlı parametre; varsayılan çizim birebir aynı (KayitSecici/Secici/Arama testleri yeşil).
- `Kuyumcu.Web/Bilesenler/MasakFormu.razor` — yukarıda.
- `Kuyumcu.Tests/EkranTestleri.cs` — yalnız bu paketin `ZO28_uyruk…` testi yeniden yazıldı + 2 yeni test (MasakFormuEkranTestleri sınıfı).
- DOKUNULMAYAN: app.css, Sozlukler HUB, Katlanir, SatirEylemleri, Secici, OnayKipi, ModalPencere, Pos.razor, BankaEkstre.razor (ikisinde hâlâ "İptal" vazgeç düğmesi var — paket rotaları dışında; not).

### 0000.4 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo --no-incremental` (son mutasyondan sonra, 16:24) → **0 Hata, 1 Uyarı** (`EtiketEkranTestleri.cs(145)` xUnit1031 — bu paketin değil).
- Yeni testler **13** (+1 yeniden yazılan): `ZOISLEMAEkranTestleri` 6 (`ZO28b_pos_basligi_MENU_adiyla_ayni_BASITte_hakedis_yok`, `ZO28b_pos_iptal_onayi_VAZGEC_solda_kirmizi_eylem_sagda`, `ZO28b_pos_valor_DAR_ekranda_KART_KALAN_etiketli_ve_css_kurali`, `ZO28b_pos_eslestir_AYRI_ara_dugmesi_yok_yazinca_arar_bos_sonucta_tutar_mesaji`, `ZO28b_banka_pencerelerinde_vazgec_dugmesi_VAZGEC`, `ZO28b_banka_MASAK_kilit_TEK_cumle_uyari_TEK_yerde_BASITte_jargonsuz`) · `ZOISLEMAAramaHttpTestleri` 1 (gerçek HTTP + MySQL) · `ZOISLEMAAramaSafTestleri` 2 teori (11 + 5 veri) · `MasakFormuEkranTestleri` 2 (`ZO28b_uyruk_DIGER…`, `ZO28b_Sade_modda…`) + `ZO28_uyruk…` yeniden yazıldı.
- Filtreli: `ZOISLEMA|MasakFormuEkran` **106/106**; geniş filtre (ZOISLEMA|Pos|Banka|Masak|KayitSecici|Arama|SADE1|P39|MenuMimarisi|EkranTestleri|Secici|OnayKipi) **2004/2004** (değişiklik ortasında; son hâl tam süitte).
- Mutasyonlar (betik `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/r3/mut.sh` + `mutlar.sh`, yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/M23…M34/`, günlük `r3/mut-r3.log`; her birinde derleme 0 Hata, filtre 39 testte YALNIZ adı yazılı 1 test kırmızı, geri yükleme `cmp` AYNI):

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M23 | AramaModulu: işaret `Yon == "GELEN"` → eski `"GIRIS"` | `ZOISLEMAAramaHttpTestleri.ZO28b_banka_hareketi_TUTARLA_aranir_GELEN_arti_GIDEN_eksi_ekran_bicimi` | AYNI |
| M24 | AramaModulu: tutar önek koşulu silindi | aynı HTTP testi | AYNI |
| M25 | EkranPara: TRY tam lira `N0` → `N2` | `ZOISLEMAAramaSafTestleri.ZO28b_sunucu_ekran_parasi_Bicim_Para_ile_BIREBIR(57550, TRY)` | AYNI |
| M26 | POS iptal vazgeç metni → `Dil.T.Ortak.Iptal` | `ZO28b_pos_iptal_onayi_VAZGEC_solda_kirmizi_eylem_sagda` | AYNI |
| M27 | KayitSecici `@if (!AraDugmesiYok)` → `@if (true)` | `ZO28b_pos_eslestir_AYRI_ara_dugmesi_yok_yazinca_arar_bos_sonucta_tutar_mesaji` | AYNI |
| M28 | Banka MASAK kilidine AlternatifYol geri | `ZO28b_banka_MASAK_kilit_TEK_cumle_uyari_TEK_yerde_BASITte_jargonsuz` | AYNI |
| M29 | MasakFormu: seçilen ülke yerine hep "TUR" | `MasakFormuEkranTestleri.ZO28b_uyruk_DIGER_ulke_kod_kutusunu_acar_ve_KOD_modele_buyuk_harfle_gider` | AYNI |
| M30 | zo-islem.css valör kartı `display: grid` → `table-row` | `ZO28b_pos_valor_DAR_ekranda_KART_KALAN_etiketli_ve_css_kurali` | AYNI |
| M31 | POS başlığı → `Dil.T.Pos.TahsilatBaslik` | `ZO28b_pos_basligi_MENU_adiyla_ayni_BASITte_hakedis_yok` | AYNI |
| M32 | Banka pencerelerinde vazgeç → "İptal" | `ZO28b_banka_pencerelerinde_vazgec_dugmesi_VAZGEC` | AYNI |
| M33 | MASAK açıkken cari ipucu yine çizilir (`@if (true)`) | `ZO28b_banka_MASAK_kilit_TEK_cumle_…` | AYNI |
| M34 | `.zo-masak-dikey` tek sütun kuralı boşaltıldı | `ZO28b_banka_MASAK_kilit_TEK_cumle_…` | AYNI |

- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-28c.trx`, 16:25→16:29): **6589 test, 6585 geçti, 4 başarısız**; bu paketin testlerinin hepsi yeşil (tam süitte `ZOISLEMA*` + `MasakFormuEkran*` başarısız 0). Kırmızıların hiçbiri bu paketin değil:
  - `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (beklenen 15, gerçek 16) → **ORTAK-1** (§ 000'da da vardı).
  - `Dogrulayici.EMANET1SinirTestleri.X5_Karma_madene_gecince_varsayilan_o_madenin_neti` ("1387,5" ↔ "11,45") → **EMANET-1**.
  - `MigrationHijyeniTestleri.Mutorj_beyaz_listesi_curumez` ("Beyaz liste kalemi artık yok: Kuyumcu.Api/Data/HareketFabrikasi.cs.mutorj") ve `OlcumKapisiTestleri.denetim_sh_razor_dizinlerini_include_suzgecsiz_TARAMAZ` (P02_MUTORJ_TAVAN 16 ↔ gerçek `.mutorj` artığı 0): ağaçtaki 16 `.mutorj` dosyası 14:47 (doğrulayıcı trx'inde bu iki test GEÇTİ) ile 16:25 arasında başka biri tarafından silinmiş (`find Kuyumcu.Web Kuyumcu.Api -name "*.mutorj*"` → 0). Bu paket dosya silmedi, `.mutorj` üretmedi (yedekler /private/tmp altında) → **kurucu/orkestratör** (KURUCUYA-KARARLAR md.4 silme kalemi; beyaz liste + tavan güncellenmeli).
  - **FARK (bu paketin yeni kırmızısı) = 0.**
- `bash ./denetim.sh`: 16:10 koşumu **iki ✗ — ikisi de EMANET-1'in** `Pages/EmanetHamKap.razor` dosyası (`Pages/*.razor dosya sayısı 48 (tavan 47)`, `P-37c SAYFA_BASI_KAPSAM acik=1`; `grep -L "<SayfaBasi" Pages/*.razor` → Ayarlar, Ekranlar, Panel [muaf] + EmanetHamKap). `denetim.sh` 16:29'da başka bir şerit tarafından güncellendi; tam süitten sonra yeniden koşuldu (`r3/denetim2.log`) → **DENETİM TEMİZ.** (✓ 291). Bu paketin sayaçları ✓: ModalPencere 48/55 (artış 0), duzenlenenId 1/1, SATIR_ICI_STYLE 75 (tavan 143), SABIT_RENK 0, HAM_INPUT 32/42, P-39 hepsi ✓, P-20 HURDA_STOK_KACAK_YOL 0, P-20b 0.

### 0000.5 Canlı doğrulama (5170, test kiracısı `PAKETZO-ISLEM-A-TEST` 01M3M1115F93R9V0WK5AHVY9YP; sistem anahtarı rastgele üretilip yalnız süreç ortamına verildi, parolalar chmod 600 fikstürde, çıktıya yazılmadı; yanıt gövdeleri okundu)
- Hazırlık: davet 201 · kayıt 201 · giriş 200 · şubeler 200 · MÜDÜR (şubesiz) 201 · kasa 200 · kasa GİRİŞ 60.000 200 · cari 201 · kur 200 · `hurda-alis` (satıcı kimlikli, şubesiz MÜDÜR) 200 has 9,16 · `hurda-satis` 2,5 has 200 · `GET /hurda/bakiye` 200 6,66 g · banka hesabı 200 · dekont GELEN 2.456.789,45 (MASAK bilgisiyle) 200 · GIDEN 1.250,50 200 · GELEN 103.439,43 200 · POS cihazı 200 · 2 KART işçilik 200 · `/pos/tahsilatlar` toplamSatir 2 · `/pos/valor-takvimi` 200 (brüt 4.250, komisyon 85, net 4.165, kalan 4.165).
- **Eşleştirme araması** (`GET /arama?tur=BANKA_HAREKET`, MÜDÜR jetonu):
```
q=103          200 [('Garanti POS yatan', '28.09.2026 · +103.439,43 ₺')]
q=103.439,43   200 [('Garanti POS yatan', '28.09.2026 · +103.439,43 ₺')]
q=103439.43    200 [('Garanti POS yatan', '28.09.2026 · +103.439,43 ₺')]
q=1250,5       200 [('Elektrik', '28.09.2026 · −1.250,50 ₺')]
q=999          200 []
q=Garanti      200 [('Garanti POS yatan', '28.09.2026 · +103.439,43 ₺')]
```
- SQL (`banka_hareketleri`, bu kiracı): `GIDEN 1250.50 '1250.50' Elektrik BEKLIYOR` · `GELEN 103439.43 '103439.43' Garanti POS yatan BEKLIYOR` · `GELEN 2456789.45 … BEKLIYOR` (CAST(tutar AS CHAR) = aramanın eşleştiği metin); `masak_kimlik_tespitleri` 1 satır.
- `GET /hurda/bakiye` rol × şube matrisi (güncel ağaç, yeniden koşuldu):

| Kullanıcı | Tek aktif şube | 2 aktif şube | 2. şube pasife alınınca |
|---|---|---|---|
| SAHİP (şubeli) | 200 · Merkez · 6,66 | — | — |
| MÜDÜR (şubesiz) | **200** · Merkez · 6,66 | **400** "Hurda işlemleri için şube bağlamı gerekli." (hurda-satis 400 "şube seçimi gerekli") | **200** · Merkez |
| MUHASEBE (şubesiz) | **200** | **400** | — |
| TEZGÂHTAR (şubesiz) | **403** "Şube bağlamı çözülemedi…" (fail-closed) | — | — |
| MÜDÜR (şubeli) | 200 | — | — |
| TEZGÂHTAR (şubeli) | 200 | — | — |
(Pasif sonrası hurda-satis 400 "Tahsil edilmeyen 1550 TL için ramatçı carisi zorunludur" — kasa verilmedi; şube kapısı geçildi, iş kuralı kapısı çalıştı.)

### 0000.6 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum ŞUBESİZ MÜDÜR, dolu veri)
- **Senaryo matrisi** (`r3/sen/sonuc.jsonl`, 8 sahne × 7 kombinasyon = 56 ölçüm: {BASİT aydınlık 1440, PRO aydınlık 1440, BASİT karanlık 1280, PRO karanlık 375, BASİT aydınlık 375, PRO aydınlık 960, BASİT karanlık 960}; sahneler: hurda dolu · POS açılış · POS arama "103" · POS arama "999" · POS iptal onayı · banka hareketleri · banka dekont MASAK 1.250.000 · banka yeni hesap):
  - Her ölçümde yatay kaydırma 0, K1/K5/K7/K8/K10/K11/K12/K13 = 0, mod doğrulandı.
  - K9 6 yalnız pencere (modal) sahnelerinde: örnekler `arka plan öğesi ⟂ #modal-…` (arka plan ⟂ açık modal katmanı — § 000.6 ile aynı ölçüm yanlış pozitifi; sayfa ölçümünde 0).
  - K3_kırık 3 yalnız banka hareketleri 1280 ve 960'ta: `td.zo-hucre-dar "Garanti POS yatan"` (üç kelimelik karşı taraf adı dar sütunda iki satıra sarıyor). GEREKÇELİ: bu sütun 28.09 sabah KRİTİK bulgusu (TUTAR görünmüyor) için bilerek sarmalı yapıldı; 1280'de tablo 928=928, sütuna asgari genişlik vermek TUTAR'ı yeniden kabın dışına iter. Etiket değil veri hücresi; bu turda yeni eklenen test verisiyle ortaya çıktı (kod değişmedi).
- **tur.mjs** (`r3/tur`): 3 rota × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375} = **48 sayfa**, 164 katman, hata 0, geçersiz 0; 48 sayfanın hepsinde K1/K3/K5/K7/K8/K9/K10/K11/K12/K13 = 0, yatay 0. analiz.py tek bulgu: GlobalArama 375 SERBEST-YÜZEN (3 rota; sahibi KATMAN — 25/27/28.09 ile aynı). **Yeni bulgu 0.**
- **tutarlilik.mjs 1440**: BASİT↔PRO eleman farkı **0** (`r3/arac/tutarlilik-fark-1440.txt` 0 bayt).
- **dugme-olc**: `ortasiz 0 silik 0 kirpik 0`.
- **Açılış 1440×900** (`r3/acilis/acilis.log`): hurda 0,85 / 0,88 · pos 0,85 / 0,88 · banka 0,55 / 0,55 (BASİT aydınlık / PRO karanlık); birincil düğme her ekranda 1 (hurda "Ramat çıkışını kaydet", pos "Banka yatmasıyla eşleştir", banka "+ Yeni Hesap").
- Görüntüler (Read ile bakıldı) `ham/TUVAL-KARSILASTIRMA/`: `ZO-ISLEM-A-28c-pos-arama-ONCE-SONRA-1440.png` (önce "-103439.43 TRY", sonra "+103.439,43 ₺", "Ara" düğmesi yok), `ZO-ISLEM-A-28c-pos-iptal-ONCE-SONRA-1440.png` (önce "Tahsilatı iptal et | İptal" gri bantta, sonra "Vazgeç | Tahsilatı iptal et" eşit pay), `ZO-ISLEM-A-28c-pos-valor-ONCE-SONRA-960-375.png` (önce KOMİSYON'da kesik, sonra kart + büyük KALAN), `ZO-ISLEM-A-28c-banka-masak-ONCE-SONRA.png`, `ZO-ISLEM-A-28c-pos-1440-BASIT-PRO.png` (başlık "POS alacakları").
- Bölge tablosu (§ 8 ve § 000.6'ya ek): "POS valör, dar ekran — tuvalde yok (tuval 1440) · kart yığını · GEREKÇELİ (375'te tuval yok, makul yığın)"; "Onay penceresi düğmeleri — tuvalde yok · ortak OnayKipi düzeni · AYNI (P-65)"; "MASAK formu — tuvalde yok · pencerede tek sütun · GEREKÇELİ (özellik silinmez, yasal kapı)".

### 0000.7 Zaman sırası
İçerik değişiklikleri 15:35–16:05 (sözlük, MasakFormu, Banka, PosTahsilatlar, KayitSecici, AramaModulu, zo-islem.css, testler) → derleme 0 hata → canlı API (15:48) + senaryo/arama/matris → ilk tarayıcı turu MASAK K7/K10 ve KALAN yazı boyu ezilmesini BULDU → `.zo-masak-dikey` (15:55, Banka.razor + css; API durduruldu, yeniden derlendi, yeniden başlatıldı) ve `#pos-t-valor` kimlik seçicisi (16:03, yalnız css) → test güncellemesi + derleme + 106/106 → **son ölçüm turu** (senaryo 7 kombinasyon + tur 48 sayfa + tutarlılık + düğme + açılış; 16:05–16:17) → API/vekil durduruldu, kiracı silindi → mutasyonlar 16:19–16:23 (dosya mtime'ları = geri yükleme + `touch`, içerik `cmp` AYNI) → `--no-incremental` temiz derleme 16:24 → tam süit + denetim.

### 0000.8 Temizlik
API ve vekil durduruldu (5170/5171 dinleyen 0), ölçüm Chrome'ları kapatıldı (`pgrep -f ZO-ISLEM-A/r3/` = 0). `kiraci-temizle.sh 01M3M1115F93R9V0WK5AHVY9YP --uygula` → ikinci çağrıda "Kiracı YOK (bağlantı sınandı)"; tenant_id kolonlu **70 tabloda kalan 0** (information_schema'dan üretilen UNION sorgusu, SUM=0); davet satırı `kiraci_davetleri` 1 silindi. tenants: başlangıç 18 → şimdi 19; fark başka şeritlerin bu süre içinde açtığı kiracılar (`PAKETSATIS-3-DOGRULAMA-TEST` 13:08Z, `PAKETEMANET-1-TEST DOG` 13:16Z) ve bir başka şeridin kendi silmesi — bu paketin katkısı 0. `PAKETZO-ISLEM-A-HAKEM-TEST` ve QA-TEST kiracılarına DOKUNULMADI.

### 0000.9 Bu turda değişen dosyalar
`dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoIslem.cs` (+9 alan TR+EN, `MasakUyrukIpucu` metni, yeni `UlkeListesi`), `dotnet/Kuyumcu.Web/Bilesenler/MasakFormu.razor` (uyruk seçici, `Sade`), `dotnet/Kuyumcu.Web/Bilesenler/KayitSecici.razor` (`AraDugmesiYok`, `BosMesaj`), `dotnet/Kuyumcu.Api/Modules/Arama/AramaModulu.cs` (banka hareketi araması), `dotnet/Kuyumcu.Web/Pages/PosTahsilatlar.razor`, `dotnet/Kuyumcu.Web/Pages/Banka.razor`, `dotnet/Kuyumcu.Web/wwwroot/css/zo-islem.css` (+2 blok), `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (+6), `dotnet/Kuyumcu.Tests/ZOISLEMAAramaTestleri.cs` (yeni), `dotnet/Kuyumcu.Tests/EkranTestleri.cs` (MasakFormuEkranTestleri: 1 yeniden, +2). Dokunulan sayfalar: PosTahsilatlar, Banka (aynı paketin hakem düzeltmesi, yeni dalga değil). Hurda.razor bu turda DEĞİŞMEDİ.

### 0000.11 DoD sözleşmesi (bu tur)
Yeni varlık / kayıt türü YOK, migration YOK (gerekmedi). Eşleştirme araması salt okunur bir uçtur (GET). Banka hareketi (finansal) düzenlenemez — doğru yol storno (48-A motoru, "⋯ → Storno" + ortak onay) ve karşı taraf / açıklama satır içi düzenleme; MASAK kimlik tespiti append-only yükümlülük kaydıdır — DegismezIpucu tek cümleyle gerekçe + düzeltme yolu. POS tahsilat defteri satırı: gerekçeli iptal ortak onayla, vazgeç "Vazgeç". Her düğme istek atar ya da kilitli + sebep (UX-S30); ayrı "Ara" düğmesi kaldırıldı çünkü arama yazdıkça çalışır.

### 0000.10 Açık kalanlar (sahibiyle)
- Tam süit: ORTAK-1 (`O8`), EMANET-1 (`X5_Karma_madene…`), `.mutorj` silinmesi → kurucu/orkestratör (`Mutorj_beyaz_listesi_curumez`, `denetim_sh_razor_dizinlerini_include_suzgecsiz_TARAMAZ`: P02_MUTORJ_TAVAN 16 → 0 ve beyaz liste güncellenmeli).
- denetim: 16:10'da EMANET-1'in `EmanetHamKap.razor` ✗×2; 16:29 `denetim.sh` güncellemesinden sonra TEMİZ.
- GlobalArama 375 SERBEST-YÜZEN → KATMAN (değişmedi).
- `AramaModulu.FaturaAra` alt bilgisi hâlâ "0.00 TRY" biçiminde (K5 sınıfı) → arama ucunun sahibi / ZO-BELGE (fatura seçicisi).
- `Pos.razor` (POS cihazları) ve `BankaEkstre.razor` pencerelerinde vazgeç düğmesi hâlâ "İptal" → bu paketin rotaları dışında; bir sonraki dokunuşta aynı `ZoIslemSozlugu.Vazgec`.
- `MasakFormu` uyruk seçicisi İşlemler'de (SATIS-3) de görünür; `Sade` yalnız Banka'da açık — SATIS-3 BASİT akışında da açmak isterse tek parametre.
- Uyruk seçicisindeki "(isteğe bağlı)" etiketi: ortak `Secici` bu soneki çizmiyor (diğer MASAK alanlarında var) → ortak bileşen sahibi.
- Genel satış/alış `SubeBelirle` "fallback yok" kararı → Bertan/Taşkın; /hurda menü grubu çelişkisi → kurucu/mimar (değişmedi).

---

## 000. SON DOĞRULAMA BULGULARININ KAPATILMASI — 28.09.2026 13:48–14:55 (tarihçe; geçerli durum § 0000) (t7 · API 5170 · vekil 5171 · CDP 9803–9810)
Durum beyanı: **BITTI**. Bu bölüm § 00'ı günceller; aşağıdaki bütün ölçümler son dosya değişikliğinden SONRA alındı (sıra: § 000.6).

### 000.1 Devralma — ağaçta ne vardı (yeniden yazılmadı)
- `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A` yeniden başlatmada silinmişti; ağaç kodla denetlendi. 28.09 sabah koşumu (07:00–11:25, raporu yazılmadan kesilmiş) doğrulayıcı bulgularının çoğunu kodlamış ve `ZO28_*` testlerini yazmıştı: POS kilit kutusu tek cümle, eşleştirme etiketi "Bankaya yatan tutar" + görünür kilit sebebi, yıkıcı eylemler (`Tahsilatı iptal et`, `Kullanımdan kaldır`) ortak `SatirEylemleri` "⋯" menüsünde ikincil görünümde ve bakiyeli hesapta sebep görünür, üç sayfada başlık altı açıklama satırı kaldırıldı, Hurda'da K-13 geliştirici notu yerine tek sade ipucu, native select'ler ortak `Form/Secim` (radyo) oldu (Hurda tahsil aracı, POS durum, dekont yön, döviz), MASAK "Uyruk (TUR)" etiketi sade + ipucu, banka hareket tablosu dar sütun + tutar tek satır.
- **Yarım kalmış tek iş bulundu:** `Banka.razor` Yeni Hesap formunda IBAN, "Daha fazla (hesap sahibi)" açıcısının ALTINDA kalmıştı (yorum "IBAN ve Döviz'in altında" diyor, kod tersini yapıyordu) → [ORTA] bulgu hâlâ açıktı. Düzeltildi: sıra Banka Adı* → Hesap Adı* → IBAN* → Döviz → Daha fazla → Kaydet (düzenleme çekmecesi zaten doğruydu). Canlı ölçüm (top px, 1440): 278 · 365 · 452 · 539 · 603 · 671.
- Ağaçta yarım mutasyon YOK: ilk derleme 0 hata 0 uyarı, `ZOISLEMA|P39TasarimKokleri` 89/89.

### 000.2 Bu oturumun ek düzeltmeleri (gerçek tarayıcının bulduğu)
1. **Hurda defteri kilit kutusu (BASİT)** — POS kutusuyla aynı kusur: "Düzeltme şöyle yapılır:" başlığı + iki paragraf + "stok satırı / fark satırı" iç terimleri. BASİT'te artık TEK sade cümle, doğru yol satırdaki düğmenin ADIYLA: "Kaydedilen hurda satışı değiştirilmez. Alıcıyla gram farkı çıkarsa satırdaki "Tahlil farkını düzelt" ile ekleyin; para farkı için müşteri düzeltme fişini kullanın." (`Sozlukler.ZoIslem.cs` TR+EN `HurdaDefterKilitBasit`; `Hurda.razor` BASİT'te `AlternatifYol=null`). PRO metni (stornolanmaz + düzeltme yolu) AYNEN — `P20EkranTestleri` sözleşmesi korunur.
2. **/banka seçili hesap sekmesi** dolu mavi zeminle "+ Yeni Hesap" birincil düğmesinin ikizi görünüyordu (tek birincil düğme). `zo-islem.css`: `.zo-banka-sekmeler > .sekme.secili { background: var(--vurgu-yuzey); border-color: var(--vurgu); color: var(--vurgu); }`. İlk denemede kenar `--vurgu-kenar` idi → dugme-olc aydınlıkta "silik" (kenar 1,35 < 1,6) ölçtü → kenar `--vurgu` yapıldı → silik 0. app.css'e dokunulmadı.
3. Yeni testler (2): `ZO28_hurda_defter_kilidi_BASIT_modda_TEK_cumle_ve_dugme_adi`, `ZO28_banka_secili_hesap_sekmesi_DOLU_vurgu_degil_yumusak_belirtec`.

### 000.3 Bulgu → durum
| Doğrulayıcı / hakem bulgusu | Durum | Kanıt |
|---|---|---|
| [ORTA] Banka Yeni Hesap: isteğe bağlı açıcı IBAN'ın üstünde | KAPANDI (bu oturum) | canlı sıra 278<365<452<539<603<671 (1440/1280/960/375 hepsi); `ZO28_banka_hesap_sahibi_acicisi_IBAN_ve_DOVIZden_SONRA_gelir`; M15 |
| [KRİTİK] Banka hareket listesinde TUTAR görünmüyor (1440) | KAPANDI | 1440: `.tablo-sar` scrollWidth 1088 = clientWidth 1088; TUTAR hücresi x 963–1101 "+2.456.789,45 ₺" görünür; 1280: 928=928; 960/375 tablo kendi kabında kayar, sayfa yatay 0; M18 |
| [KRİTİK] Hurda: geliştirici notu (K-13, "bacağı", "DEĞİLDİR") | KAPANDI (sabah) + defter kutusu sadeleşti (bu oturum) | `ZO28_hurda_detayda_TEK_sade_ipucu…`, `ZO28_hurda_defter_kilidi_BASIT…`; M20, M22; görüntü hurda BASIT 1440 |
| [KRİTİK] POS kilit notu jargon + tekrar | KAPANDI (sabah) | kutu tek cümle "Bu tahsilat satıştan gelir; yanlışsa satırdaki İptal ile ya da satışı iade ederek düzeltin."; M16 |
| [YÜKSEK] POS eşleştirme "Banka hareketi kimliği", sessiz "Öneri getir" | KAPANDI (sabah) | etiket "Bankaya yatan tutar"; boşken iki düğme kilitli ve sebep ekranda "Önce bankaya yatan tutarı seçin." (canlı 1440/1280/960/375); M17. "Ara" düğmesi ortak KayitSecici'nin (DoD EK md.1 gereği) — bu paketin dosyası değil |
| [YÜKSEK] Yıkıcı eylemler kırmızı dolu birincil | KAPANDI (sabah) | ekranda birincil düğme sayısı canlı: hurda 1, pos 1, banka 1; M19 |
| [YÜKSEK] Başlık altı açıklama satırı (K17), BASİT "pasife" | KAPANDI (sabah) | `ZO28_uc_sayfada_SAYFA_ACIKLAMASI_yok…` |
| [YÜKSEK] Native select'ler (K22), "TC KIMLIK", "Uyruk (TUR)" | KAPANDI (sabah) | Hurda tahsil aracı / POS durum / dekont yön / döviz = `Form/Secim` radyo; müşteri ve belge türü = ortak özel `Secici` (native değil); belge türü sözlükten "T.C. kimlik kartı"; Uyruk etiketi + ipucu |

### 000.4 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo --no-incremental` (son mutasyondan sonra, 14:35) → **0 Hata, 1 Uyarı** (`EtiketEkranTestleri.cs(145)` xUnit1031 — bu paketin değil).
- Filtreli: `ZOISLEMA` **59/59**; ilgili geniş filtre (ZOISLEMA|P39|P20|Q102|SADE1|Banka|Pos) 510 testte 509 + tek kırmızı `PosBarkodTestleri.Fiyat_SUNUCU_teklifinden…` → tek başına 7/7 yeşil (kararsız, SATIS-3).
- Mutasyonlar (betik `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/mut.sh`, yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/M15…M22/`, günlük `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/mut28.log`; her birinde derleme 0 hata, geri yükleme `cmp` AYNI + `touch`, sonda `--no-incremental` temiz derleme):

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| M15 | Banka.razor: IBAN bloğu açıcının altına geri | `ZO28_banka_hesap_sahibi_acicisi_IBAN_ve_DOVIZden_SONRA_gelir` | AYNI |
| M16 | POS kilit Gerekce → `Dil.T.Pos.DefterGerekce` | `ZO28_pos_kilit_TEK_cumle_etiket_sade_eslestir_kilidinin_SEBEBI_gorunur` | AYNI |
| M17 | `EslestirKilit` boş hareket → null (sessiz) | `ZO28_pos_kilit_TEK_cumle_…_SEBEBI_gorunur` | AYNI |
| M18 | karşı taraf `<td class="zo-hucre-dar">` → `<td>` | `ZO28_banka_hareket_tablosu_DAR_sutun_ve_eylemler_ortak_menude` | AYNI |
| M19 | bakiyeli hesap kilit sebebi `@if (false)` | `ZO28_banka_KALDIR_tehlike_dolu_degil_menude_bakiyeliyse_SEBEP_yazili` | AYNI |
| M20 | Hurda BASİT `AlternatifYol` geri | `ZO28_hurda_defter_kilidi_BASIT_modda_TEK_cumle_ve_dugme_adi` | AYNI |
| M21 | seçili sekme zemini `--vurgu-yuzey` → `--vurgu` (dolu) | `ZO28_banka_secili_hesap_sekmesi_DOLU_vurgu_degil_yumusak_belirtec` | AYNI |
| M22 | Hurda BASİT gerekçe → eski `HO.DegismezNotuBasit` | `ZO28_hurda_defter_kilidi_BASIT_modda_TEK_cumle_ve_dugme_adi` | AYNI |
Her mutasyonda filtre 59 testte yalnız adı yazılı 1 test kırmızı (58 yeşil).
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-28b.trx`, 14:36→): **6526 test, 6524 geçti, 2 başarısız**, hiçbiri bu paketin değil:
  - `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` — beklenen 15, gerçek 16: `Sozlukler.OrtakGorunum.cs` 14:16'da ORTAK-1 şeridince 16. alan eklendi, test 14:17'de değişti (şerit ortasında) → **ORTAK-1**.
  - `Dogrulayici.EMANET1SinirTestleri.V1_Iade_belirsiz_1_500…` — emanet çıkış POST'u → **EMANET-1**.
  - **FARK (bu paketin yeni kırmızısı) = 0.**
- `bash ./denetim.sh` → **DENETİM BAŞARISIZ, tek ✗**: `P-37c SAYFA_BASI_KAPSAM acik=1` → neden `Pages/EmanetHamKap.razor` (14:36'da EMANET-1 şeridince oluşturulan yeni dosya, `<SayfaBasi>`/muafiyet yok) → **EMANET-1**. Bu paketin sayaçları ✓: ModalPencere 48/55, duzenlenenId 1/1, SATIR_ICI_STYLE 75 (tavan 143), SABIT_RENK 0, P-39 TABLO_SARMALAYICISIZ 0, P-20 HURDA_STOK_KACAK_YOL 0, P-20b HURDA_FIRE_SIZINTISI 0.

### 000.5 Canlı doğrulama (5170, test kiracısı `PAKETZO-ISLEM-A-TEST` 01M3KTCR241S5ESYQ4XJN86HED; sistem anahtarı rastgele üretilip yalnız süreç ortamına verildi, parolalar chmod 600 fikstürde, çıktıya yazılmadı)
Hazırlık (yanıt gövdeleri okundu): kasa 200 · kasa GİRİŞ 60.000 200 · cari 201 · kur 200 · banka hesabı 200 · dekont GİDEN 1.250,50 200 · GELEN 8.000 200 · GELEN 2.456.789,45 ilk deneme **400 MASAK** ("185.000 TL ve üstü işlemde kimlik tespiti zorunludur" — yasal kapı çalışıyor), MASAK bilgisiyle 200 · POS cihazı 200 · 2 KART işçilik 200 → `/pos/tahsilatlar` toplamSatir 2.
Hurda (şubesiz MÜDÜR): ilk `hurda-alis` **400 gider pusulası** ("satıcı kimliği zorunludur (VUK md.234)" — yasal kapı çalışıyor); satıcıyla **200** has 9,16 · `hurda-satis` 2,5 has **200** · `GET /hurda/bakiye` **200** 6,66 g.

`GET /hurda/bakiye` rol × şube matrisi:
| Kullanıcı | Tek aktif şube | 2 aktif şube | 2. şube pasife alınınca (`DELETE /subeler/{id}` 200 "Şube deaktif edildi") |
|---|---|---|---|
| SAHİP (şubeli) | 200 · Merkez · 6,66 | — | — |
| MÜDÜR (şubesiz) | **200** · Merkez | **400** "Hurda işlemleri için şube bağlamı gerekli." (hurda-satis 400 "şube seçimi gerekli") | **200** · Merkez |
| MUHASEBE (şubesiz) | **200** | **400** | — |
| TEZGÂHTAR (şubesiz) | **403** "Şube bağlamı çözülemedi…" (fail-closed) | — | — |
| MÜDÜR (şubeli) | 200 | — | — |
| TEZGÂHTAR (şubeli) | 200 | — | — |
(Pasif sonrası hurda-satis 400 "Tahsil edilmeyen 1550 TL için ramatçı carisi zorunludur" — kasa verilmemişti; şube kapısı geçildi, iş kuralı kapısı çalıştı.)
SQL (`hareketler ⋈ subeler ⋈ kullanicilar`, tür · şube · has · tl · rol · kullanıcı şubesiz?):
```
HURDA_ALIS   Merkez Şube   9.160000  -25648.00  MUDUR  1
HURDA_SATIS  Merkez Şube  -2.500000    7750.00  MUDUR  1
subeler: Merkez Şube aktif=1 · Ikinci Sube aktif=0
kullanicilar: SAHIP şubeli 1 · MUDUR şubesiz 1 / şubeli 1 · MUHASEBE şubesiz 1 · TEZGAHTAR şubesiz 1 / şubeli 1
```
Tarayıcıda şubesiz MÜDÜR oturumu: kullanıcı kartı "Müdür · Merkez Şube", /hurda bakiyesi ve formu açık (orkestratörün SADE-1 hata görüntüsündeki durum kapandı).

### 000.6 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum ŞUBESİZ MÜDÜR, dolu veri)
- **tur.mjs** son hâl (`/private/tmp/kuyumcu-serit/ZO-ISLEM-A/tur28b`, 14:05–14:18): 3 rota × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375} = **48 sayfa**, 152 katman, hata 0, geçersiz 0; her sayfada K1/K3/K5/K9/K10/K11/K12/K13 = 0, yatay kaydırma 0, mod doğrulandı. analiz.py tek bulgu: GlobalArama 375 SERBEST-YÜZEN (3 rota; sahibi KATMAN — 25/27/28.09 ile aynı). **Yeni bulgu 0.** Sekme CSS'i sonrası /banka yeniden turlandı (`tur28c`, 16 sayfa): aynı sonuç.
- **tutarlilik.mjs 1440**: BASİT↔PRO eleman farkı **0** (`arac28/tutarlilik-fark-1440.txt` boş).
- **dugme-olc** (kopya, CDP 9808): 12 kombinasyon, **ortasız 0 · silik 0 · kırpık 0** (ilk ölçümde sekme kenarı silik 2 → düzeltildi → 0).
- **Açılış 1440×900** (ana alan içerik alt kenarı / görünüm): hurda 0,85 / 0,88 · pos 0,85 / 0,88 · banka 0,55 / 0,55 (BASİT aydınlık / PRO karanlık); ekran başına birincil düğme 1 (hurda "Ramat çıkışını kaydet", pos "Banka yatmasıyla eşleştir", banka "+ Yeni Hesap"). (Tur'un K4 değeri /pos'ta 1,1–1,3: belge yüksekliği kenar çubuğunun açık "Para ve banka" grubundan — KATMAN açığı; ana alan 0,85.)
- **Senaryo matrisi** (`sen28b/sonuc.jsonl`; hurda: has 1,5 + Daha fazla + kabul 1,45 + defter açık; POS defteri açık; banka hareketleri açık; Yeni Hesap modalı) × {1440 BASİT aydınlık, 1440 PRO aydınlık, 1280 BASİT karanlık, 375 PRO karanlık, 960 PRO aydınlık, 960 BASİT karanlık} = 24 ölçüm: K sayaçları 0, yatay 0. Tek istisna: modal senaryosunda K9 6 — örnekler `p.stat-deger ⟂ alan etiketi` (arka plan ⟂ açık modal katmanı; ölçüm yanlış pozitifi, sayfa ölçümünde 0).
- Görüntüler (Read ile bakıldı) `ham/TUVAL-KARSILASTIRMA/`: `ZO-ISLEM-A-28b-banka-hareket-1440-BASIT-PRO.png` (TUTAR ve Durum sütunu görünür), `ZO-ISLEM-A-28b-hurda-dolu-1440-BASIT-PRO.png` (defter kutusu tek cümle), `ZO-ISLEM-A-28b-pos-dolu-1440-BASIT-PRO.png`, `ZO-ISLEM-A-28b-banka-yeni-hesap-375-960.png` (IBAN → Döviz → Daha fazla), `ZO-ISLEM-A-28b-banka-1440-tuval-biz.png` (tuval Tezgah desen dili ↔ /banka açılış), `ZO-ISLEM-A-28b-banka-ONCE-SONRA.png`.
- Bölge tablosu § 8 ile aynı; ek satırlar: "Seçili hesap sekmesi — tuvalde seçili öğe yumuşak vurgu (menü) · bizde yumuşak zemin + vurgu kenarı · AYNI"; "Kilit kutuları — tuvalde yok · tek sade cümle · GEREKÇELİ (UX-S25 özellik silinmez)".
- Zaman sırası (son İÇERİK değişikliği → ölçüm): Banka.razor (IBAN) ~13:50 · Hurda.razor + Sozlukler.ZoIslem.cs 14:09 → derleme + API yeniden başlatma → tur28b (foto 14:12…14:18) · senaryo sen28b · zo-islem.css kenar düzeltmesi ~14:23 (statik dosya, derleme gerektirmez) → açılış ölçümü + görüntü · tur28c + dugme-olc → canlı iki-şube → mutasyonlar (dosya mtime'ları 14:32–14:34 = mutasyon geri yüklemesi + `touch`; içerik `cmp` ile birebir AYNI) → `--no-incremental` temiz derleme 14:35 → tam süit + denetim 14:36–14:5x.

### 000.7 Temizlik
API (PID 88004/87999), vekil (PID 72771) ve ölçüm Chrome'ları durduruldu; 5170/5171 boş. `kiraci-temizle.sh 01M3KTCR241S5ESYQ4XJN86HED --uygula`; sonradan arka plan işinin yazdığı 1 `kur_snapshots` satırı ayrıca silindi → tenant_id kolonlu tüm tablolarda kalan **0**. Ayrıca bu paketin 28.09 sabah koşumundan (07:56) kalan aynı adlı `PAKETZO-ISLEM-A-TEST` kiracısı (01M3KG7W4HVQ1FDTMRCK54E48Z, 795 satır) aynı betikle silindi → kalan 0. İki davet satırı silindi (row_count 2). tenants: başlangıç 20 → şimdi 17 (−1 benim, −1 bu paketin sabah artığı, −1 başka şeritlerin kendi temizliği). `PAKETZO-ISLEM-A-HAKEM-TEST` (doğrulayıcının) ve QA-TEST kiracılarına DOKUNULMADI.

### 000.8 Bu oturumda değişen dosyalar
`dotnet/Kuyumcu.Web/Pages/Banka.razor` (IBAN sırası), `dotnet/Kuyumcu.Web/Pages/Hurda.razor` (BASİT kilit kutusu), `dotnet/Kuyumcu.Core/I18n/Sozlukler.ZoIslem.cs` (+1 alan TR+EN), `dotnet/Kuyumcu.Web/wwwroot/css/zo-islem.css` (+1 kural), `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (+2 test). Sabah koşumunun dokunduğu (bu oturum korudu): PosTahsilatlar.razor, Banka.razor, Hurda.razor, zo-islem.css, Sozlukler.ZoIslem.cs, `Bilesenler/MasakFormu.razor` (yalnız Uyruk etiketi/ipucu — ortak bileşen; ZO-ISLEM-A gerekçesi hakem bulgusu), ZOISLEMAEkranTestleri.cs. Dokunulan sayfalar: Hurda, Banka, PosTahsilatlar (aynı paketin doğrulayıcı düzeltmesi, yeni dalga değil). DOKUNULMAYAN: app.css, Sozlukler HUB, Katlanir, SatirEylemleri, KayitSecici, Secici, SubeSecici.

### 000.9 Açık kalanlar (sahibiyle)
- Tam süit: ORTAK-1 (`O8` — sözlük 16. alan, şerit ortasında), EMANET-1 (`V1_Iade_belirsiz…`); kararsız `PosBarkodTestleri` (SATIS-3, tek başına yeşil). denetim P-37c ✗ → EMANET-1 (`EmanetHamKap.razor`).
- GlobalArama 375 SERBEST-YÜZEN; POS menüsünde "POS cihazları" ve "POS alacakları" birlikte seçili; kenar çubuğu açık grupla belgeyi uzatıyor → KATMAN.
- POS eşleştirme seçicisindeki ayrı "Ara" düğmesi ortak `KayitSecici` bileşeninin (her düğme istek atar kuralı) → ortak bileşen sahibi.
- Genel satış/alış `SubeBelirle` "fallback yok" kararı → Bertan/Taşkın. /hurda menü grubu çelişkisi → kurucu/mimar.

---

## 00. DOĞRULAYICI BULGULARININ KAPATILMASI — 28.09.2026 00:00–00:50 (tarihçe; geçerli durum § 000) (t7 · API 5170 · vekil 5171 · CDP 9801–9816)
Durum beyanı: **BITTI**. Aşağıdaki bütün ölçümler **son dosya değişikliğinden SONRA** alındı (sıra kanıtı § 00.4). 27.09 22:24 trx'i ve 22:29 denetim günlüğü artık kanıt DEĞİLDİR (doğrulayıcının haklı "sıra hatası" bulgusu); § 0.4 ve § 0.6'daki beyanlar bu bölümle geçersizleşir.

### 00.1 Bulgu → düzeltme
| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| [KRİTİK] P-39 TABLO_SARMALAYICISIZ 2 + `P39TasarimKokleriTestleri.K5` kırmızı (sarmalayıcı `tablo-sar zo-baslik-tek-satir`) | PosTahsilatlar.razor: iki sarmalayıcı yine TAM `class="tablo-sar"`; tek satır sınıfı TABLOYA: `<table class="veri-tablosu zo-baslik-tek-satir">`. zo-islem.css: `.veri-tablosu.zo-baslik-tek-satir th { white-space: nowrap; }`. Test `ZO_pos_tablolari_baslik_TEK_SATIR…` sarmalayıcı sınıfının TAM `tablo-sar` olduğunu ve tabloların sınıfı taşıdığını sınar. | denetim `✓ P-39: TABLO_SARMALAYICISIZ 0` · `DENETİM TEMİZ.` · K5 yeşil (filtre 74/74 ve tam süit) |
| [KRİTİK] Rapor son ağacı ölçmüyordu | Son değişiklikten sonra: derleme → denetim → tam süit → canlı → tarayıcı yeniden koşuldu. | § 00.4 zaman sırası |
| [ORTA] Şubesiz tüm-şube rolü hurda ALIŞI 400, ramat çıkışı 200 (UX-S22 iki cevap) | `SatisModulu.HurdaAlis` başına `/hurda` bakiyesi ve `HurdaSatis` ile AYNI tek kural: `islem = await Hurda.HurdaStokKapisi.OturumSubesiCoz(db, islem);` (tek satır; 2+ aktif şubede ve şubeye kilitli rolde 400 kalır — fail-closed). Genel satış/alış `SubeBelirle` "fallback yok" kuralına DOKUNULMADI (o karar Bertan/Taşkın'da kalır); yalnız hurda defterinin iki ucu tek cevaba bağlandı. Yeni DB testi `ZO_subesiz_mudur_HURDA_ALISI_tek_subede_o_subeye_iki_subede_400`. | canlı § 00.3 [1]–[3] + SQL |
| [DÜŞÜK] POS defteri rozeti sınıra değince kısmi sayı | `Katlanir Sayi="@(sayfa?.ToplamSatir)"` — sunucunun toplamı (API: `toplamSatir` = filtreli COUNT, `sinirDegdi = offset + satirlar.Count < toplamSatir`). Akordeon kapalıyken de gerçek toplam görünür; açılınca `pos-t-sinir` uyarısı. Yeni test `ZO_pos_defter_rozeti_SINIRA_degince_sunucu_TOPLAMINI_gosterir` (toplam 250, dönen 1 → rozet "250"). | canlı [7]: `limit=1` → `toplamSatir 2, sinirDegdi True, satir 1` |
| [DÜŞÜK] SatisModulu.cs mtime 27.09 21:59 | Bu paketin 27.09 akşamına kadar dosyadaki TEK değişikliği HurdaSatis satırıdır (1284); 21:59 değişikliği bu paketin değil — sahibini orkestratör kapı kaydına yazar. 28.09'da bu paket dosyaya HurdaAlis satırını ekledi (1137, yorum 3 satır). | `grep -n OturumSubesiCoz` → 1137, 1284 |

### 00.2 Derleme · testler · mutasyon · denetim
- `dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra, 00:18) → **0 Hata, 1 Uyarı** (`EtiketEkranTestleri.cs(145)` xUnit1031 — bu paketin değil).
- Filtreli: `ZOISLEMA|P39TasarimKokleri` → **74/74** (ZOISLEMA 31 = 29 + 2 yeni).
- Mutasyonlar (betik `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/mut.sh`, yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/`, `--no-incremental`, her birinde `cmp` AYNI; günlük `r27b/mutlar.log`):

| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M11b | sınıf tablodan sarmalayıcıya geri (`<div class="tablo-sar zo-baslik-tek-satir"><table class="veri-tablosu">`) | `P39TasarimKokleriTestleri.K5_Her_tablo_KENDI_kaydirma_kabindadir` + `ZO_pos_tablolari_baslik_TEK_SATIR_sinifini_ve_nowrap_kuralini_tasir` |
| M12b | zo-islem.css `nowrap` → `normal` | `ZO_pos_tablolari_baslik_TEK_SATIR_sinifini_ve_nowrap_kuralini_tasir` |
| M13 | rozet `ToplamSatir` → `Kayitlar.Count` | `ZO_pos_defter_rozeti_SINIRA_degince_sunucu_TOPLAMINI_gosterir` |
| M14 | HurdaAlis'teki `OturumSubesiCoz` satırı silindi | `ZO_subesiz_mudur_HURDA_ALISI_tek_subede_o_subeye_iki_subede_400` |
(M1, M3, M4, M5, M8, M9, M10 27.09'da güncel ağaçta kırmızı verdi — § 0.5; o noktaların kodu 28.09'da değişmedi.) Not: M12b/M13 koşularında ilk derleme denemesi eşzamanlı şeritlerin dosya kilidi yüzünden düştü; betik 45 sn bekleyip yeniden derledi, ikinci deneme 0 Hata.
- `bash ./denetim.sh` (00:19, rc=0) → **DENETİM TEMİZ.** `✓ P-39: TABLO_SARMALAYICISIZ 0` · `✓ P-18: MOBIL_TABLO_SARMALAYICISIZ 0` · `✓ SABIT_RENK 0` · `✓ P-20 HURDA_STOK_KACAK_YOL 0` · `✓ P-20b HURDA_FIRE_SIZINTISI 0`. (İlk deneme `zsh ./denetim.sh` → "DENETİM KOŞULMADI — bu betik BASH içindir", rc=2; bash ile yeniden koşuldu.)
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-28.trx`, 00:19→00:39): **6403 test, 6395 geçti, 8 başarısız**. Tek başına yeniden koşum (`r27b/kirmizi.log`): 25 testte 3 kırmızı kaldı:
  - `OlcekTekTestleri.OL14_tablo_satiri_ve_liste_yazisi_moddan_bagimsiz_TEK_belirtec` — beklenen `var(--yazi-kontrol)`, gerçek `calc(var(--yazi-govde) * 15 / 18)`; `css/olcek.css` koşum SIRASINDA 00:43'te OLCEK-1 şeridince değiştirildi → **OLCEK-1**.
  - `S24EskiAltinEkranTestleri` ×2 — Islemler / `Bilesenler/Fis` → **SATIS-3** (27.09'da da kırmızıydı).
  - Kararsız (tek başına YEŞİL): `PosBarkodTestleri` ×2 (Islemler barkod — SATIS-3), `SATIS3EkranTestleri.ARANIRKEN…`, `S1jMenuAramaTestleri…("İ")` (KenarCubugu), `BildirimEkranTestleri.Her_dugme…` (Bildirimler).
  - **FARK (bu paketin yeni kırmızısı) = 0**: 8 testin hiçbiri Hurda/PosTahsilatlar/Banka sayfalarını ya da hurda API'sini çizmez/çağırmaz; `P39…K5` bu koşumda yeşil.

### 00.3 Canlı doğrulama (5170, yeni test kiracısı `PAKETZO-ISLEM-A-TEST` 01M3JBX3AGMXKPH6YER3YT9E15; SAHİP + şubesiz MÜDÜR, parolalar chmod 600 fikstürde, çıktıya yazılmadı; betikler `r27b/canli*.py`)
MÜDÜR oluşturma 201, `subeId=None`; SQL `kullanicilar.sube_id` = NULL. Kur 200, kasa GİRİŞ 60.000 200, cari 201.
| # | İstek (şubesiz MÜDÜR) | Beklenen | Gerçek |
|---|---|---|---|
| 1 | POST /islemler/hurda-alis 10 g K22, tek aktif şube | 200 (önce 400) | **200**, `hasGrami 9.16`, gider pusulası üretildi |
| 2 | 2. şube açıldıktan sonra aynı istek | 400 (fail-closed) | **400** "Hurda alış için şube seçimi gerekli" |
| 2b | GET /hurda/bakiye iki şubede | 400 | **400** "Hurda işlemleri için şube bağlamı gerekli." |
| 3 | 2. şube pasif (`DELETE /subeler/{id}` 200 "Şube deaktif edildi") → hurda-alis 10 g | 200 | ilk deneme **400 tevsik** ("aynı gün aynı kişiyle nakit toplam 51.296 TL, 30.000 TL haddini aşıyor" — yasal kapı çalışıyor, şube kapısı geçildi); 1 g ile **200**, `hasGrami 0.916` |
| 4 | POST /islemler/hurda-satis 2,5 has | 200 | **200**, `kalanHurdaHasGram 6.66` |
| 5 | GET /hurda/bakiye | 200 | **200**, subeId = Merkez, hasGram 6,66 (sonra 7,576) |
| 6 | GET /pos/tahsilatlar (SAHİP, 2 KART işçilik sonrası) | 200 | **200** toplamSatir 2, sinirDegdi False, 2 satır |
| 7 | GET /pos/tahsilatlar?limit=1 | toplam ≠ dönen | **200** toplamSatir 2, sinirDegdi True, 1 satır |
SQL (`hareketler ⋈ subeler ⋈ kullanicilar`, tür · şube · has_grami · tl_tutari · rol · kullanıcı şubesiz?):
```
HURDA_ALIS   Merkez Şube   9.160000  -25648.00  MUDUR  1
HURDA_SATIS  Merkez Şube  -2.500000    7750.00  MUDUR  1
HURDA_ALIS   Merkez Şube   0.916000   -2564.80  MUDUR  1
subeler: Merkez Şube aktif=1 · Ikinci Sube aktif=0
```
Senaryo verisi: banka hesabı 200 + 3 dekont (GELEN/GIDEN) 200 + POS cihazı 200 + 2 KART işçilik 200.

### 00.4 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum ŞUBESİZ MÜDÜR)
- **tur.mjs** (`r27b/tur`): 3 rota × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375} = **48 sayfa**, 180 katman, hata 0, geçersiz 0. analiz.py: tek bulgu GlobalArama 375 SERBEST-YÜZEN (3 rotada; sahibi KATMAN — 27.09 ile aynı). **Yeni bulgu 0.** (Not: analiz'in "bağlı açılır yazı boyları" dağılımında bu turda 16px ×12 da görüldü — 27.09'da yalnız 18px; açılır liste bileşeni OLCEK-1/DUGME-2 alanı, bulgu olarak işaretlenmedi.)
- **tutarlilik.mjs 1440** (`r27b/araclar`): `mod farki 0`. **dugme-olc**: `ortasiz 0 silik 0 kirpik 0`.
- **Açılış 1440×900** (ana alan içerik alt kenarı / görünüm): hurda 0,93 / 0,96 · pos 0,86 / 0,89 (rozet "2", defter kapalı) · banka 0,58 / 0,58 (BASİT / PRO) → hepsi ≤ 1,0.
- **Senaryo matrisi** (`r27b/senaryo/sonuc.jsonl`, dolu veri, hurda detay açık + defter açık, POS defteri açık, banka hareketleri açık) × {1440 BASİT aydınlık, 1440 PRO aydınlık, 1280 BASİT karanlık, 375 PRO karanlık, **960 PRO aydınlık, 960 BASİT karanlık**} = 18 sayfa: K1…K13 = **0**, yatay kaydırma **0**, mod doğrulandı. /pos/tahsilatlar ek ölçümü her genişlikte: sarmalayıcılar `["tablo-sar","tablo-sar"]`, başlık `white-space: nowrap`, başlık yüksekliği 35 px (375'te 42 px — tek satır), rozet "2".
- Görüntüler (Read ile bakıldı) `ham/TUVAL-KARSILASTIRMA/`: `ZO-ISLEM-A-28-pos-tahsilatlar-960-senaryo.png` (960 PRO aydınlık ↔ 960 BASİT karanlık: başlıklar tek satır, tablo kendi kabında kayıyor), `ZO-ISLEM-A-28-pos-tahsilatlar-1440-acilis-BASIT-PRO.png` (açılış: defter kapalı, rozet 2), `ZO-ISLEM-A-28-senaryo-375-PRO-karanlik.png`.
- Zaman sırası (son değişiklik → ölçüm): PosTahsilatlar.razor 00:09:28 · SatisModulu.cs 00:14:05 (M14 geri yükleme) · zo-islem.css 23:59:40 → derleme 00:18:09 → denetim 00:19:51 → tam süit trx 00:39:06 → canlı + tarayıcı 00:20–00:48.

### 00.5 Temizlik
API (PID 14138/14100), vekil (PID 18943) ve ölçüm Chrome'u (PID 70778) durduruldu; 5170/5171 boş. `kiraci-temizle.sh 01M3JBX3AGMXKPH6YER3YT9E15 --uygula`; davet satırı silindi (row_count 1); tenant_id kolonlu TÜM tablolarda bu kiracıya ait kalan satır **0**; `tenants where id=…` **0**. tenants: başlangıç 20 → şimdi 16 (fark başka şeritlerin kendi kiracılarını silmesi; QA-TEST kiracılarına dokunulmadı).

### 00.6 28.09 değişen dosyalar (bu paketin)
`dotnet/Kuyumcu.Web/Pages/PosTahsilatlar.razor` (sınıf tabloya, rozet toplamı), `dotnet/Kuyumcu.Web/wwwroot/css/zo-islem.css` (seçici), `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs` (HurdaAlis tek satır + yorum), `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (1 test güncellendi, 1 yeni), `dotnet/Kuyumcu.Tests/ZOISLEMASubeTestleri.cs` (1 yeni). Dokunulan sayfa: PosTahsilatlar.razor — aynı paketin doğrulayıcı düzeltmesi (yeni dalga değil). SatisModulu.cs eşzamanlı SATIS şeritleriyle paylaşılan dosyadır; değişiklik HurdaAlis'in ilk satırıyla sınırlı.
Not: `HurdaStokKapisi.cs` (23:34:45) ve `Hurda.razor` (23:35:57) mtime'ı bu oturumun düzenlemesi değil (bu oturumda bu iki dosyaya yazılmadı); içerikleri ZOISLEMA testleriyle yeşil.

### 00.7 Açık kalanlar (28.09, sahibiyle)
- Tam süit: OLCEK-1 (`OL14` — olcek.css eşzamanlı değişiklik), SATIS-3 (`S24EskiAltin` ×2; kararsız `PosBarkod` ×2, `SATIS3…ARANIRKEN`), KATMAN (`S1jMenuArama` kararsız), ORTAK-1 (`Bildirim…Her_dugme` kararsız).
- Genel satış/alış/iade/takas `SubeBelirle` "fallback yok" (şubesiz tüm-şube rolü) → Bertan/Taşkın genel kural kararı; hurda iki ucu bu pakette tek kurala bağlandı.
- GlobalArama 375 SERBEST-YÜZEN, KenarCubugu BASİT 1440 belge 933 px, POS menü iki satır seçili → KATMAN. ModalPencere 375 dekont alt düğmeleri → ortak bileşen. /hurda menü grubu çelişkisi → kurucu/mimar.

---

## 0. DEVRALMA VE YENİDEN ÖLÇÜM — 27.09.2026 (t7 · API 5170 · vekil 5171 · CDP 9801–9816)
Durum beyanı (27.09): BITTI — ⚠ doğrulayıcı 27.09 bu beyanı ölçüm sırası nedeniyle reddetti; geçerli durum § 00.

### 0.1 Ağaç denetimi (yarım mutasyon var mı?)
- `/private/tmp/kuyumcu-serit/mut-yedek` yeniden başlatmada silinmişti; bu yüzden 10 mutasyon noktasının hepsi koda bakılarak tek tek denetlendi (`HurdaStokKapisi.OturumSubesiCoz` `== 1` ve `&& s.Aktif`, `SatisModulu.HurdaSatis` çözüm satırı, `Hurda.razor` `DetayGorunur`/`DetayHatali`, `Banka.razor` `MasakGerekli`/hesap sahibi koşulu, `PosTahsilatlar.razor` `BaslangictaAcik`). Hepsi özgün hâlinde; yarım mutasyon YOK. 25.09 işi korunarak devam edildi, yeniden yazılmadı.
- İlk derleme `dotnet build Kuyumcu.sln` → 0 hata 0 uyarı; ZOISLEMA 28/28, ilgili filtre 546/546.

### 0.2 27.09'da yapılan ek değişiklikler (gerçek tarayıcı ölçümünün bulduğu 2 kusur)
1. **/pos/tahsilatlar 960 px — K3 kırık başlık** ("VALÖR TARİHİ", "CİHAZ ADI" iki satıra kırılıyordu; 25.09'da tablo boştu, bu yüzden görülmemişti — bu kez canlı veri ile valörlü tahsilat vardı). Çözüm: iki `.tablo-sar` kabına `zo-baslik-tek-satir` sınıfı (PosTahsilatlar.razor, id değişmedi) + `css/zo-islem.css` `.zo-baslik-tek-satir .veri-tablosu th { white-space: nowrap; }`; dar ekranda tablo kendi kabında kayar (sayfa yatay kaydırma 0 ölçüldü). Yeni test: `ZO_pos_tablolari_baslik_TEK_SATIR_sinifini_ve_nowrap_kuralini_tasir`. İkinci tur: K3 = 0.
2. **/hurda bakiye kutusu dikey iç boşluğu 0** (CDP ölçümü: `.dugme { padding-block:0 }` → "Altın" etiketi kutu kenarına 1 px yapışıyordu). Çözüm: `css/zo-islem.css` `#hrd-bakiye-listesi .hurda-bakiye-kart { padding: 12px 16px; height: auto; }` (ölçüm sonrası: padding 12px 16px). app.css'e dokunulmadı.

### 0.3 Derleme
Son mutasyondan sonra `dotnet build Kuyumcu.sln --nologo --no-incremental` → **0 hata**; uyarılar: `EtiketEkranTestleri.cs(145)` xUnit1031 (bu paketin değil) + MSB3026 kopyalama yeniden denemesi (çalışan API DLL'i kilitliyordu, kopya tamamlandı).
Not: eşzamanlı şeritler aynı ağacı derlediği için üç kez "dosya kilitli" (MSB4018 WebCil) ve bir kez başka şeridin yarım `Fis/FisOdemeBacaklari.razor` (CS0117/CS1061) derleme hatası görüldü; bunlar beklenip yeniden denendi, hiçbiri bu paketin dosyasında değildi.

### 0.4 Testler (27.09)
- ZOISLEMA: **29/29** (28 + 1 yeni). İlgili filtre (ZOISLEMA, SADE1, P20*, Q102*, Banka, Pos, P12, Q91, UX1C2, P19): son koşum 550 testte 549 geçti; tek kırmızı `S24EskiAltinEkranTestleri.S24_Fark_Sifir…` (filtreye "Pos" alt dizgesiyle girdi; Islemler/Fis — SATIS-3 şeridi, bu paketin dosyasına dokunmuyor).
- **Tam süit** (`/private/tmp/kuyumcu-serit/trx-pZO-ISLEM-A/tam-27.trx`): 6333 test, **6323 geçti, 10 başarısız — hiçbiri bu paketin değil**:
  - `P10PosBacagiEkranTestleri` ×4 ve `S24EskiAltinEkranTestleri` ×2 → Islemler/`Bilesenler/Fis/FisOdemeBacaklari.razor` (SATIS-3 şeridi; dosya koşum sırasında 22:10'da değişti, aynı saatte derleme hatası da veriyordu). Tek başına yeniden koşumda hâlâ kırmızıydılar; son filtreli koşumda P10 yeşile döndü, S24 ×1 kırmızı.
  - `OrtakGorunumSaflik.O1_index_ortak_cssi_EN_SONDA_yukler`, `OlcekTekTestleri.OL5_index_olcek_cssi…`, `OrtakGorunumCekmeceTestleri.C2…`, `DovizBacagiEkranTestleri.Kasa_listesi…` → tek başına yeniden koşumda **yeşil** (kararsız: `wwwroot/index.html` koşum sırasında 22:19'da başka şerit tarafından düzenlendi).
  - Devir notundaki 3 kırmızı (EMANET-1 S5/S8, MigrationHazirlik) bu koşumda **yeşil**.
  - FARK (bu paketin ürettiği yeni kırmızı) = **0**: kırmızıların hiçbiri Hurda/Banka/PosTahsilatlar/hurda API'sine dokunan test değil.

### 0.5 Mutasyon kanıtları — 27.09 güncel ağaçta YENİDEN koşuldu (betik `/private/tmp/kuyumcu-serit/ZO-ISLEM-A/mut.sh`, yedek `/private/tmp/kuyumcu-serit/mut-yedek/ZO-ISLEM-A/`, her mutasyondan sonra `cmp` AYNI, derleme `--no-incremental`)
| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M1 | `aktifSubeler.Count == 1` → `>= 1` | ZO_subesiz_mudur_IKI_aktif_subede_400_kalir_fail_closed |
| M3 | SatisModulu.HurdaSatis içindeki `OturumSubesiCoz` satırı silindi | ZO_subesiz_mudur_tek_subede_bakiye_okur_ve_ramat_cikisi_O_SUBEYE_yazilir |
| M4 | `&& s.Aktif` silindi | ZO_pasif_sube_SAYILMAZ_tek_aktif_sube_secilir |
| M5 | `DetayGorunur => !detayAcik …` | 4 test: acilista_YALNIZ_zorunlu…, detay_ekle_ile…, KAPATILAMAZ…, HATALIYSA… |
| M8 | dekont detayında `\|\| MasakGerekli` silindi | ZO_banka_dekont_MASAK_esiginde_detay_KENDILIGINDEN_acilir |
| M9 | POS `BaslangictaAcik="@defterAcik"` silindi | ZO_pos_defteri_KAPALI_acilinca_iptal_dugmesi_gorunur_filtrede_acik_kalir |
| M10 | hesap sahibi koşulu → `@if (true)` | ZO_banka_yeni_hesap_sahibi_KAPALI_ekle_ile_acilir_ve_POSTa_girer |
| M11 (yeni) | `tablo-sar zo-baslik-tek-satir` → `tablo-sar` | ZO_pos_tablolari_baslik_TEK_SATIR_sinifini_ve_nowrap_kuralini_tasir |
| M12 (yeni) | zo-islem.css `nowrap` → `normal` | ZO_pos_tablolari_baslik_TEK_SATIR_sinifini_ve_nowrap_kuralini_tasir |

Ölçülen tuzak (Madde 1): ilk M5 koşumunda M4'ün mutasyonlu `Kuyumcu.Api.dll`'i artımlı derlemede kalmıştı (M5'te ek olarak "pasif şube" testi kırmızı göründü; kaynak `cmp` ile AYNI olduğu hâlde). `--no-incremental` ile yeniden derlenince 28/28 yeşil; betik bundan sonra hep `--no-incremental` derledi ve M5 temiz koşumda yalnız kendi 4 testini kırdı.

### 0.6 denetim.sh
**DENETİM TEMİZ** (rc=0). Bu paketin sayaçları: ModalPencere 48/55, duzenlenenId 1/1, GUVENLIK_ELLE_IZOLASYON 0, SATIR_ICI_STYLE 90 (tavan 143), SABIT_RENK 0, HURDA_STOK_KACAK_YOL 0, HURDA_FIRE_SIZINTISI 0. (25.09'daki P-39 ve P-23 ✗'leri kapanmış.)

### 0.7 Canlı doğrulama (5170, test kiracısı `PAKETZO-ISLEM-A-TEST` 01M3J4RJZB3TQWRJZ66AY1CK0K, davetle kayıt; parolalar chmod 600 fikstürde, çıktıya yazılmadı)
Hazırlık (hepsi yanıt gövdesi okunarak): kasa oluştur 200 → kasa GIRIS 60.000 200 → cari 201 → hurda alış 10 g K22 (ilk deneme 25 g: 400 tevsik haddi 30.000 TL — yasal kapı çalışıyor; ikinci: 400 kasa yetersiz — kasa kapısı çalışıyor) 200, has 9,16.

`GET /hurda/bakiye` matrisi:
| Kullanıcı | Tek aktif şube | 2 aktif şube | 2. şube pasife alınınca (`DELETE /subeler/{id}` 200 "Şube deaktif edildi") |
|---|---|---|---|
| SAHİP (şubeli) | 200 · Merkez · 9,16 g | 200 | 200 |
| MÜDÜR (şubesiz) | **200** · Merkez | 400 "Hurda işlemleri için şube bağlamı gerekli." | **200** |
| MUHASEBE (şubesiz) | **200** | 400 | 200 |
| TEZGÂHTAR (şubesiz) | 403 "Şube bağlamı çözülemedi…" (fail-closed) | 403 | 403 |
| MÜDÜR (şubeli) | — | — | 200 |
| TEZGÂHTAR (şubeli) | — | — | 200 |

`POST /islemler/hurda-satis` şubesiz MÜDÜR, tek şube, 2,5 has × 3.100 → **200**, `kalanHurdaHasGram 6,66`, `kasaBakiyesi 42.102`.
SQL (`hareketler` ⋈ `subeler` ⋈ `kullanicilar`):
```
HURDA_ALIS   Merkez Şube   9.160000  -25648.00 NAKIT SAHIP  kullanici_subesiz=0
HURDA_SATIS  Merkez Şube  -2.500000    7750.00 NAKIT MUDUR  kullanici_subesiz=1
```
Senaryo verisi: banka hesabı 200 + 3 dekont (GELEN/GIDEN; ilk denemede GIRIS/CIKIS → 400 "yalnız GELEN veya GIDEN", doğru yön sözlüğüyle 200) + POS cihazı 200 + 2 KART işçilik 200 → `/pos/tahsilatlar` 2 satır, valör takvimi dolu.

### 0.8 Gerçek tarayıcı (başsız Chrome + CDP, vekil 5171, oturum ŞUBESİZ MÜDÜR — hatanın yaşandığı kullanıcı tipi)
- **tur.mjs** (`/private/tmp/kuyumcu-serit/ZO-ISLEM-A/tur2`): 3 rota × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375} = **48 sayfa**, 180 katman, hata 0, geçersiz 0. analiz.py: K1/K3/K5/K9/K9b/K10/K10b/K11/K12/K13/yatay = **0** her sayfada; kalan tek bulgu GlobalArama 375 SERBEST-YÜZEN (3 rotada, sahibi KATMAN; orkestratör tabanında 82 ekranda aynı). **Yeni bulgu 0.** (İlk tur `tur/` K3 kırık başlığı yakaladı → 0.2/1 ile düzeltildi → ikinci tur temiz.) analiz'in "eksik" satırı 42-rotalık tam tur eşiğine göredir; 16 kombinasyonun 16'sı × 3 rota ölçüldü.
- **tutarlilik.mjs 1440** (kopya, CDP 9811): BASİT↔PRO eleman farkı **0**.
- **dugme-olc** (kopya, CDP 9812): ortasız 0 · silik 0 · kırpık 0.
- **Açılış yüksekliği 1440×900** (içerik alt kenarı / görünüm): hurda 0,92 / 0,95 · pos 0,86 / 0,89 · banka 0,58 / 0,58 (BASİT / PRO) → hepsi ≤ 1,0. (BASİT'te belge 933 px: taşan öğe `KenarCubugu` grup başlıkları `#kenar-grup-yonetim` b=974 — KATMAN şeridinin alanı; ana alan sığıyor.)
- **Senaryo matrisi** (`/private/tmp/kuyumcu-serit/ZO-ISLEM-A/senaryo/sonuc.jsonl`; hurda: has 1,5 + "Daha fazla" + kabul 1,45 + defter açık; POS: valör dolu + defter açık; banka: 3 hareket açık) × {1440 BASİT aydınlık, 1440 PRO aydınlık, 1280 BASİT karanlık, 375 PRO karanlık} = 12 sayfa: sikisma K sayaçları **0**, yatay kaydırma **0**, mod doğrulandı (aria-pressed).
- Görüntüler (Read ile bakıldı) `ham/TUVAL-KARSILASTIRMA/`: `ZO-ISLEM-A-27-{hurda,pos-tahsilatlar,banka}-1440.png` (tuval deseni ↔ biz), `ZO-ISLEM-A-27-{…}-ONCE-SONRA.png` (25.09 ↔ 27.09), `ZO-ISLEM-A-27-senaryo-{1440-BASIT-aydinlik,1440-PRO-aydinlik,1280-BASIT-karanlik,375-PRO-karanlik}.png`.
- Bölge tablosu 25.09'daki ile aynı (aşağıda § 8); ek satır: "POS tablo başlıkları — tuvalde tablo başlığı tek satır · bizde tek satır (27.09 düzeltmesi) · AYNI".

### 0.9 Temizlik
API (PID 88221) ve vekil (PID 34982) durduruldu; 5170/5171 boş. `kiraci-temizle.sh 01M3J4RJZB3TQWRJZ66AY1CK0K --uygula` → 659 satır + tenants 1. Sonradan başka bir sürecin yazdığı 1 `kur_snapshots` satırı ayrıca silindi; tenant_id kolonlu tüm tablolarda kalan **0**. Davet satırı silindi (row_count 1). tenants: başlangıç 18 → şimdi 20; fark bu koşum sırasında başka şeritlerin açtığı `PAKETSATIS-3-TEST`/`PAKETKATMAN-1b-TEST Dogrulayici2`/`PAKETOLCEK-1-TEST Dogrulayici` kiracılarıdır; benim kiracım yok (count 0). QA-TEST kiracılarına dokunulmadı.

### 0.10 27.09 değişen dosyalar (bu paketin)
`dotnet/Kuyumcu.Web/Pages/PosTahsilatlar.razor` (2 sınıf), `dotnet/Kuyumcu.Web/wwwroot/css/zo-islem.css` (2 kural), `dotnet/Kuyumcu.Tests/ZOISLEMAEkranTestleri.cs` (1 test). 25.09 listesi (§ 10) aynen geçerli.

### 0.11 Açık kalanlar (27.09, sahibiyle)
- Tam süit kırmızıları: SATIS-3 (P10PosBacagi ×4, S24EskiAltin ×2 — `Fis/FisOdemeBacaklari.razor`); kararsız ×4 (index.html eşzamanlı düzenleme — ORTAK-1/OLCEK-1).
- KenarCubugu BASİT 1440'ta belgeyi 933 px'e uzatıyor; /pos/tahsilatlar'da "POS cihazları" ve "POS alacakları" menü satırları birlikte seçili görünüyor (önek eşleşmesi) → KATMAN.
- GlobalArama 375 SERBEST-YÜZEN → KATMAN. ModalPencere 375 dekont alt düğmeleri (ortak bileşen). /hurda menü grubu çelişkisi → kurucu/mimar. Şubesiz tüm-şube rolü satış/alış `SubeBelirle` "fallback yok" → Bertan/Taşkın kararı.

---

## 25.09.2026 İLK UYGULAMA KAYDI (tarihçe; 27.09'da yeniden ölçüldü, yukarıdaki § 0 geçerlidir)

Tarih: 25.09.2026 · Şerit: t7 (test DB `kuyumcu_test_t7`, canlı API 5170, vekil 5171, CDP 9801–9820) · Rol: Taşkın (backend) + Ertuğ (frontend)
Durum beyanı: **BITTI** (paket kapısının 7 maddesi sağlandı; tam süitteki 3 kırmızı başka şeritlere ait, aşağıda sahipleriyle).

## 1. Kök neden ölçümü — "Müdür · Merkez Şube" ama /hurda "şube bağlamı gerekli"
Tahmin yok, ölçüldü:
- `kuyumcu_net.kullanicilar`: şubesi atanmamış MÜDÜR kullanıcıları gerçekten var (QA-TEST-FRONTEND, PAKET-ORTAK-1-TEST; ikincisinde kiracının TEK şubesi var).
- İstemci: `Bilesenler/SubeSecici.razor` `KartMetni` kiracıda tek şube varsa o şubenin adını yazıyor (UX-S22) → kart "Müdür · Merkez Şube".
- Sunucu: `/hurda/*` şubeyi yalnız JWT `subeId` claim'inden (`kullanicilar.sube_id`) alıyor, yoksa 400 veriyor (`SubeSuzgeci.OturumSubesinde`). Aynı ekranda aynı soruya iki cevap çıkıyordu.
- Düzeltme (en küçük değişiklik, yalnız hurda alanı): `HurdaStokKapisi.OturumSubesiCoz(db, islem)`. Oturumun şubesi varsa o kullanılır. Yoksa, rol TÜM-ŞUBE rolüyse (`SubeYetkisi.TumSubeler`: SAHIP/MUDUR/MUHASEBE) ve kiracıda **tam olarak bir aktif şube** varsa işlem o şubede yürür. Diğer durumlarda (çok şube, şubeye kilitli rol) oturum olduğu gibi kalır ve sunucu 400/403 döner (fail-closed). Bu çözüm `/hurda/bakiye`, `/hurda/defter`, `POST /islemler/hurda-satis` ve `tahlil-duzeltme` yollarında çağrılıyor.
- Gerçekten şubesiz olan oturumda ekran TEK kısa sebebi ve ne yapılacağını yazıyor (UX-S30). Sunucunun iç terimli metni ("şube bağlamı") ekrana basılmıyor. Sebep bir kez yazılıyor: form notu, düğme yanı ve bakiye kartı aynı metni kopyalamıyor. SAHİP için ayrı metin var: "Kullanıcılar ekranından kendinize şube atayın".

**Canlı matris (5170, test kiracısı, `GET /hurda/bakiye`):**
| Kullanıcı | Tek aktif şube | 2 aktif şube | 2. şube pasife alınınca |
|---|---|---|---|
| SAHİP (şubeli) | 200, sube=Merkez, 22,9 g | 200 | 200 |
| MÜDÜR (şubesiz) | **200**, sube=Merkez | 400 "Hurda işlemleri için şube bağlamı gerekli." | **200** (pasif şube sayılmıyor) |
| MUHASEBE (şubesiz) | 200 | 400 | 200 |
| TEZGÂHTAR (şubesiz) | 403 (küresel çözümleyici; çözülmez) | 403 | 403 |
| MÜDÜR (şubeli) | 200 | 200 | 200 |

`POST /islemler/hurda-satis` (şubesiz MÜDÜR, tek şube) → 200, kalan 20,4 g. SQL: `HURDA_SATIS sube_id=01M3C8PTDHH5K679NHFJYJFJVB (Merkez) has −2,5 tl 7750 NAKIT rol=MUDUR`.

## 2. Ekranlar — zorunlu önce (kurucu 25.09 11:35)
- **/hurda**
  - Açılışta yalnız bu öğeler var: bakiye kartı · "Bizim kaydımız" (tek zorunlu alan) · tek satırlık özet (kasa · kurdan birim fiyat · türetilen tutar) · "Daha fazla (alıcının kabulü, fiyat, ramatçı, tahsilat)" · Kaydet · kapalı "Hurda defteri".
  - İsteğe bağlı alanlar kapalıyken DOM'da yok ve gövdeye yalnız varsayılanları gidiyor: kabul=null, cari=null, NAKIT, tahsil=tutar.
  - Değer girilmişse ya da bir alan hatalıysa bölüm kapatılamıyor.
  - Fire gerekçesi paragraf olarak değil, katlanır ipucu olarak duruyor (`hrd-fire-notu` aynı kimlikle).
  - Defter tablosu tanımsız `.tablo` sınıfından standart `.veri-tablosu`na geçti; Düzelt düğmesi artık kırpılmıyor.
- **/pos/tahsilatlar**: Envanter "değişiklik gerekmiyor" diyordu. Kurucu kuralı gereği yalnız defter (Kartlı tahsilatlar) ortak `Katlanir` içinde kapalı geliyor. Filtre değişince açık/kapalı durumu korunuyor. "Bu kayıt değiştirilemez" notu açılışta kaldı.
- **/banka**
  - Hesap hareketleri kapalı akordeonda. Hata bandı akordeonun dışında.
  - Yeni hesap ve düzenle formlarında "Hesap sahibi" alanı "Daha fazla (hesap sahibi)" arkasında. Düzenlemede kayıtlı değer varsa alan açık geliyor.
  - Dekont formunda açılışta yalnız Yön + Tutar var. Tarih, müşteri, karşı taraf ve açıklama "Daha fazla" arkasında. MASAK eşiğinde bölüm kendiliğinden açılıyor.
  - Emojiler (🏛️ ✏️ 🧾) yerine `Ikon` bileşeni kullanıldı.
  - BASİT modda "cari"/"kümülatif" yerine "Müşteri"/"Seçilmedi" yazıyor.
  - Tarihteki yıldız kaldırıldı: alan bugünle dolu geliyor, boşaltılırsa alan altında mesaj çıkıyor.
- "Daha fazla" satırı tuvaldeki `Musteriler.dc.html` "Daha fazla (…)" satırının ölçüleriyle yapıldı: 14/16 iç boşluk, 9 px köşe, gövde zemini, 16 px yazı, sağda ok.
- Değişmeyenler: sunucu uçları ve gövdeleri, yasal kapılar (gider pusulası, MASAK, onay penceresi) ve mevcut DOM kimlikleri.
  - İstisna: `pos-t-defter-baslik`. Başlık artık Katlanir'ın `pos-t-defter-kart-ac` düğmesinde; kök kimlik `pos-t-defter-kart` korundu. Ortak bileşen başlığa kimlik almadığı için bu fark gerekçeli.

## 3. Sapmalar ve gerekçeleri
1. Şube çözümü `SubeSuzgeci` (tüm modüller) yerine yalnız hurda yollarına kondu. Satış ve alış `SubeBelirle` ("Nest ile birebir, fallback yok") DEĞİŞMEDİ. Bu yüzden şubesiz MÜDÜR tek şubede satış yapamıyor → açık kalan.
2. Tuvalde /hurda, /banka ve /pos/tahsilatlar ekranları YOK. Karşılaştırma tuvalin desen diliyle yapıldı ("Daha fazla" satırı, kart dili).
3. Kontrast için tuvalden gerekçeli fark: "Daha fazla" ve akordeon başlıklarının kenarı `--kenarlik` (≥3:1, WCAG 1.4.11). `dugme-olc` tuvaldeki `--ayrac` kenarını 1,38 ölçüp "silik" dedi (<1,6).
4. Ortak `Katlanir.razor`, SATIS-2 şeridine ait olduğu için ona dokunulmadı. Görünüm yalnız `.zo-gecmis` kapsamında `css/zo-islem.css` ile ayarlandı.
5. Menü riski: `/hurda` kodda (MenuTanimi.cs) PARA VE BANKA grubunda, görev tarifinde GÜNLÜK İŞ altında. Tek taraflı taşınmadı → kurucuya/mimara not.

## 4. Migration
Gerekmedi (şema değişikliği yok).

## 5. Testler
- Yeni: **28 test**.
  - `ZOISLEMAEkranTestleri` (bUnit): 15 Fact + 1 Theory×4. Kapsam: açılışta gizlenen alan DOM'da yok, "+ ekle" ile görünüyor ve POST'a giriyor, kapalıyken yalnız varsayılanlar gidiyor, hatalı ya da dolu bölüm kapanmıyor, akordeonlar, şubesiz 4 rol × 400/403 için tek sebep, BASİT banka metinleri.
  - `ZOISLEMASubeTestleri` (DB, t7): 6 Fact + 1 Theory×3. Kapsam: rol × şubeli/şubesiz × tek/çok/pasif şube, kiracı yalıtımı, çıkışın doğru şubeye yazılması.
  - Yardımcı: `ZoTestYardim.cs`.
- Güncellenen eski testler (açılış artık kapalı): P20b/P20/Q102 (+Dogrulayici) `Ekran()` yardımcıları "Daha fazla" ve defteri açıyor. BankaEkran, P12, Q91, UX1C2 ve P19 hareket akordeonunu/dekont detayını açıyor. PosEkran defteri açıyor. SADE1: Hurda şubesiz testi (tek sebep, bant) ve POS başlık kimliği.
- Filtreli son koşum (temiz build sonrası): **262/262 geçti** (ZOISLEMA, SADE1, P20*, Q102*, Banka, Pos, P12, Q91, UX1C2, P19).
- Tam süit (`trx-pZO-ISLEM-A`): **6304 test, 6301 geçti, 3 başarısız**. Hiçbiri bu paketin değil:
  - `Dogrulayici.EMANET1SinirTestleri.S5…`, `…S8…` → sahibi T8 EMANET-1
  - `MigrationHazirlikTestleri.EksikMigrationliSemadaBekleyenListesiDolar` → bu paket migration içermiyor; sahibi migration yazan şerit veya kararsızlık, orkestratör kapısında
  - Bir önceki tam koşumda görülen 50 kırmızının hepsi ZO-BELGE ekranlarına (Fatura, Evrak, Rapor, Kasa raporu, Denetim izi, İçe aktarım) ve EMANET-1'e aitti; son koşumda kapanmışlardı.
- Derleme: 0 hata. Tek uyarı `EtiketEkranTestleri.cs(145)` xUnit1031 (bu paketin değil); bu paketin 3 analizör uyarısı giderildi.

## 6. Mutasyon kanıtları (kaynak yedeği `mut-yedek/ZO-ISLEM-A`, geri alınınca cmp AYNI)
| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M1 | `aktifSubeler.Count == 1` → `>= 1` | ZO_subesiz_mudur_IKI_aktif_subede_400_kalir_fail_closed |
| M2 | tüm-şube rol kontrolü silindi | ZO_subesiz_TEZGAHTAR_tek_subede_bile_COZULMEZ_400 |
| M3 | HurdaSatis içindeki çözüm satırı silindi | ZO_subesiz_mudur_tek_subede_bakiye_okur_ve_ramat_cikisi_O_SUBEYE_yazilir |
| M4 | `&& s.Aktif` silindi | ZO_pasif_sube_SAYILMAZ_tek_aktif_sube_secilir |
| M5 | `DetayGorunur => !detayAcik …` | 4 test (acilista_YALNIZ_zorunlu…, detay_ekle…, KAPATILAMAZ…, HATALIYSA…) |
| M6 | `DetayHatali` koşulu silindi | ZO_hurda_detay_alani_HATALIYSA_bolum_acik_kalir… (ilk koşumda test açığı çıktı; test güçlendirildi, sonra kırmızı) |
| M7 | 400/403 şube metni → ham sunucu metni | ZO_hurda_subesiz…(MUDUR/SAHIP/MUHASEBE/TEZGAHTAR) + SADE1 Hurda_sube_baglami… (5 kırmızı; son sürüm elle yeniden koşuldu) |
| M8 | dekont detayında `MasakGerekli` silindi | ZO_banka_dekont_MASAK_esiginde_detay_KENDILIGINDEN_acilir |
| M9 | POS `BaslangictaAcik="@defterAcik"` silindi | ZO_pos_defteri_…filtrede_acik_kalir (ilk koşumda açık çıktı; sahte API'ye gerçek ağ gecikmesi eklendi, sonra kırmızı) |
| M10 | hesap sahibi `@if (true)` | ZO_banka_yeni_hesap_sahibi_KAPALI_ekle_ile_acilir_ve_POSTa_girer |

Not: `shutil.copy2` eski mtime'ı taşıdığı için artımlı derleme mutasyonlu DLL'i bırakıyordu. Bu ölçüldü (mutasyon sonrası 2 sahte kırmızı). Betiğe `os.utime` eklendi ve son mutasyondan sonra `--no-incremental` TEMİZ build yapıldı (0 hata).

## 7. denetim.sh
DENETİM BAŞARISIZ, 2 ✗; hiçbiri bu paketin değil:
- `P-39 TABLO_SARMALAYICISIZ 5`: Faturalar, Evraklar, IslemDefteri, KasaRaporu → ZO-BELGE
- `P-23 ODEME_KALEMSIZ_YOL 3`: canlı veri, `PAKETSATIS-2-TEST` kiracısının 3 KARMA satışı → SATIS-2

Bu paketi ilgilendiren sayaçlar ✓: ModalPencere 48/55, duzenlenenId 1/1, GUVENLIK_ELLE_IZOLASYON 0, SABIT_RENK 0, SATIR_ICI_STYLE 93 (tavan 143), HURDA_STOK_KACAK_YOL 0, HURDA_FIRE_SIZINTISI 0.

## 8. Gerçek tarayıcı ölçümü (başsız Chrome + CDP, vekil 5171, şubesiz MÜDÜR oturumu)
- **tur.mjs**: 3 rota × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375} = 48 sayfa, 168 katman. Tüm sayfalarda K1, K3, K5, K9, K9b, K10, K10b, K11, K12, K13 ve yatay kaydırma **0**. analiz.py 3 "bulgu" gösteriyor: GlobalArama 375 SERBEST-YÜZEN; orkestratör tabanı TAM-TUR-v3'te 82 ekranda aynı, sahibi KATMAN. **Yeni bulgu 0.**
- **tutarlilik.mjs 1440**: BASİT↔PRO farkı **0**. Taban `MOD-TUTARLILIK-FARK-1440` /pos/tahsilatlar için 8 fark içeriyordu.
- **dugme-olc**: ortasız 0 · silik 0 · kırpık 0. İlk ölçümde 16 silik vardı; kenar belirteciyle düzeltildi.
- **Açılış yüksekliği** (ana alan / görünüm, 1440×900): hurda 1,00 · pos 1,00 · banka 1,00, BASİT ve PRO. PRO hurda 1,03 ölçülmüştü; özet tek satıra alındı.
- **Senaryo matrisi** (CDP ile doldurulmuş; `sahne.jsonl`, 1440 BASİT/PRO aydınlık, 1280 BASİT karanlık, 375 PRO karanlık, 960 PRO): hurda dolu (has 1,5 + kabul 1,45 + defter açık), onay penceresi, POS defteri açık, banka hareketler, dekont, dekont detay, yeni hesap. Sayfa ölçümlerinde bütün K sayaçları 0. Modal senaryolarında K9 2–6: CDP ile örnek okundu, hepsi arka plan ⟂ `#modal-…` (açık katman). Sayfa ölçümünde K9 0.
- Görüntüler, Read ile bakıldı (`ham/TUVAL-KARSILASTIRMA/`):
  - `ZO-ISLEM-A-hurda-960-ONCE-SONRA.png` (orkestratörün SADE-1 görüntüsü ↔ aynı kullanıcı tipi, çalışıyor)
  - `ZO-ISLEM-A-{hurda,banka,pos-tahsilatlar}-1280-ONCE-SONRA.png`
  - `ZO-ISLEM-A-{hurda,banka,pos-tahsilatlar}-1440.png` (tuval ↔ biz)
  - `ZO-ISLEM-A-senaryo-1440-PRO.png`, `-senaryo-375-PRO-karanlik.png`, `-senaryo-1280-BASIT-karanlik.png`

**Bölge tablosu (tuval deseni ↔ biz):**
| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| İsteğe bağlı alanlar | "Daha fazla (…)" tam genişlik satır, ok sağda | aynı ölçü ve metin kalıbı | AYNI |
| "Daha fazla" kenarı | #d7dce4 (ayraç) | `--kenarlik` | GEREKÇELİ FARK (≥3:1 kontrast) |
| Geçmiş listeleri | tuvalde bu ekranlar yok; kurucu kuralı "kapalı akordeon" | ortak Katlanir, kapalı, sayı rozetli | GEREKÇELİ FARK (kurucu 25.09) |
| Birincil düğme | tek, mavi | tek (Ramat çıkışını kaydet · Yeni Hesap) | AYNI |
| İkon | ince çizgi | Ikon bileşeni; banka emojileri kaldırıldı | AYNI |
| Tuvalde olmayan (fire ipucu, değişmezlik notu, onay penceresi) | yok | korundu (ipucu ya da katlanır) | GEREKÇELİ (özellik silinmez) |

## 9. DoD sözleşmesi
Yeni varlık ya da kayıt türü yok. Mevcut kayıtların düzenleme yolları değişmedi:
- Hurda satışı: DegismezIpucu + tahlil farkı düzeltme çekmecesi
- Banka hesabı: PATCH (çekmece) ve pasife al / aktife al
- Dekont: satır içi açıklama/karşı taraf, tarih ve eşleştirme çekmeceleri, storno
- POS tahsilatı: gerekçeli iptal + DegismezIpucu

Yeni düğmelerin hepsi istek atmayan ama işlevli `@onclick` taşıyor ("ölü düğme" testi geçti).

## 10. Değişen dosyalar
- API:
  - `Modules/Hurda/HurdaStokKapisi.cs` (OturumSubesiCoz)
  - `Modules/Hurda/HurdaModulu.cs`
  - `Modules/Hurda/TahlilFarkiServisi.cs`
  - `Modules/Satis/SatisModulu.cs` (HurdaSatis içinde 2 satır)
- Core: `I18n/Sozlukler.ZoIslem.cs` (yeni, TR+EN)
- Web:
  - `Pages/Hurda.razor`
  - `Pages/Banka.razor`
  - `Pages/PosTahsilatlar.razor`
  - `wwwroot/css/zo-islem.css` (yeni)
  - `wwwroot/index.html` (1 link)
- Testler:
  - Yeni: `ZOISLEMAEkranTestleri.cs`, `ZOISLEMASubeTestleri.cs`, `ZoTestYardim.cs`
  - Güncellenen: `P20bEkranTestleri`, `P20EkranTestleri`, `Q102HurdaOnayEkranTestleri`, `Dogrulayici/Q102SinirTestleri`, `SADE1EkranTestleri`, `PosEkranTestleri`, `BankaEkranTestleri`, `P12EkranTestleri`, `Q91BankaEkranTestleri`, `UX1C2EkranTestleri`, `P19VirmanBacagiEkranTestleri`
- Dokunulan sayfalar (tek dokunuş): Hurda, Banka, PosTahsilatlar. Bu paketin kapsamı bu üç sayfa.
- DOKUNULMAYANLAR: app.css, Sozlukler HUB, Katlanir, SubeSecici, AuthModulu.

## 11. Canlı doğrulama sonrası temizlik
- API ve vekil durduruldu; 5170 ve 5171 boş.
- Test kiracısı `PAKETZO-ISLEM-A-TEST` (01M3C8PTDHK4F68CAE7XNM20FJ) `kiraci-temizle.sh --uygula` ile 1076 satır + tenants satırıyla silindi. tenant_id kolonlu tüm tablolarda kalan satır 0.
- Davet satırı silindi.
- tenants 17 → 16 (başlangıç 17 benim kiracımı da içeriyordu). QA-TEST kiracılarına dokunulmadı.

## 12. Açık kalanlar (sahibiyle)
- Şubesiz tüm-şube rolü tek şubeli kiracıda satış ve alış yapamıyor (`SatisModulu.SubeBelirle`, "fallback yok" kararı). Aynı kural tüm modüllere uygulanacaksa karar mimara/Taşkın'a ait (Bertan).
- Tam süitteki 3 kırmızı: EMANET-1 (T8) ×2, MigrationHazirlik ×1. denetim: P-39 ×5 (ZO-BELGE), P-23 ×3 (SATIS-2 test kiracısı).
- GlobalArama 375 SERBEST-YÜZEN (KATMAN).
- ModalPencere 375'te dekont formunun alt düğmeleri gövde içinde kayıyor (ortak bileşen).
- /hurda menü grubu çelişkisi: kurucuya/mimara.

## 13. Kurucuya ve mali müşavire notlar
Mevzuat hükmü değişmedi (gider pusulası, MASAK, tevsik kapıları aynen duruyor). Yeni eşik veya oran yazılmadı.
