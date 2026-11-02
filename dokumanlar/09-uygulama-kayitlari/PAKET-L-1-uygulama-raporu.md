# PAKET L-1 — Uygulama raporu: çok kalemli nakit satışta tevsik açığı kapatıldı

> Dalga SATIŞ SON · Taşkın (backend) + Ertuğ (frontend) · 25.09.2026
> Yalıtım: `KUYUMCU_TEST_DB=kuyumcu_test_t2`, canlı API 5162, vekil 5163. Bu paket commit atmadı.

## 1. Özet

**Sorun.** Kasiyer iki ürünü nakit satınca ekran her ürün için ayrı bir istek atıyordu. T-S1 v3'te canlıda şu görüldü: Peşin Müşteri'ye 42.065,89 ₺'lik satış iki ayrı istekle gitti (28.711,64 ve 13.354,25). İkisi de kabul edildi. Sunucunun tevsik kapısı (459/575 s. VUK GT) fiş toplamına bakar. Ama fiş bölündüğü için toplamı hiç görmedi, bu yüzden yasal sınır aşıldı.

**Çözüm.** İki ve daha çok ürünlü satış artık her modda (BASİT ve PROFESYONEL) tek bir istekle `POST /islemler/satis-fisi` ucuna gider. Bu uç tevsik ve MASAK kapılarını zaten fişin brüt toplamıyla çağırıyordu. Tek ürünlü satış eskisi gibi `/islemler/satis` yolunda kalır. Kaydetme `Kaydet()` içinde tek bir istekle yapılır; fiş bileşenleri kendileri istek atmaz.

**Sonuç (canlı).** Aynı vaka (Peşin + 2 kalem nakit, 42.065,89 ₺) şimdi **tek istek** olarak gidiyor. Sunucu bunu **400** ile reddediyor ve Müşteri alanının altında "kimlik gerekiyor" bandı çıkıyor. SQL'de **0** hareket yazıldı.

## 2. Önce ölçülenler (ADIM 1 — spesifikasyon koda karşı)

| İddia | Ölçüm | Sonuç |
|---|---|---|
| `Islemler.razor` çok kalemli SATIŞ'ı kalem başına POST ediyor | `else if (tur is "SATIS" or "ALIS")` dalında `foreach (var k in kalemler) … PostAsync("/islemler/satis"…)` ("API kalem başına tek işlem kaydeder — sırayla gönder") | DOĞRU (satır ~1197) |
| satis-fisi tek bacaklı çok kalemli fişi kabul ediyor mu | `SatisFisiDto` → `Kalemler` (≥1), `Odemeler` (≥1). Hediye/iskonto dalı zaten tek bacakla bu uca gidiyordu | EVET; gövde değişmeden kabul ediliyor |
| Kapılar fiş BRÜT toplamıyla mı çağrılıyor | `SatisServisi.SatisFisi`: `masak.DogrulaKumulatif(…, fisToplam, …)` ve `tevsik.Dogrula(…, sepet.NakitToplam, now, islemToplamTutari: fisToplam)`. Peşin Müşteri'de `TevsikKapisi.Dogrula` şunu ölçer: `olcut = max(nakit, islemToplam) > Had` → fail-closed, `AlanIle("cariId")` | EVET — sunucuda kural değişikliği gerekmedi |
| satis-fisi tek kalemli uçla aynı gövdeyi taşıyor mu | **HAYIR — `ReyonId` yoktu.** Tek kalemli uç `reyonId` alıyor ve seçilen reyondan düşüyor; fiş ucu bunu bilmiyordu. Çok kalemli satış fiş ucuna taşınınca PRO'da seçilen reyon sessizce yok sayılırdı (stok yanlış reyondan düşerdi) | **SAPMA → en küçük sunucu değişikliği** (§ 3.1) |
| Alış için fiş ucu var mı | `SatisModulu` içinde yalnız `POST /islemler/alis` (tek kalem) var; `alis-fisi` yok | YOK → alış dalına dokunulmadı (§ 7) |
| Cari iskontosu | Fiş ucu `fisIskontoYuzde` boşsa **carinin kartındaki oranı** uygular. Bacak toplamı fiş toplamına TAM eşit olmak zorunda. Çok kalemli düz satış boş oranla gönderilseydi, iskontolu bir caride bacak (ekran toplamı) ile sunucu toplamı tutmaz ve satış 400 alırdı. Eski yol (`/islemler/satis`) carinin oranını hiç uygulamıyordu | Davranış eşitliği için düz fişte `fisIskontoYuzde = 0` (§ 3.2) |

## 3. Yapılanlar

### 3.1 Sunucu (`Kuyumcu.Api/Modules/Satis/SatisModulu.cs`) — yeni uç yok, eklenenler isteğe bağlı
- `SatisFisiDto.ReyonId` (`[MaxLength(36)]`, isteğe bağlı) eklendi. Sözleşmesi tek kalemli uçla aynıdır: reyon verildiyse **yalnız o reyondan** düşülür (yetmezse 400); boşsa önce atanmamış stok tükenir.
- Fiş döngüsünde `StokDegistir(…, dto.ReyonId)` kullanılıyor. İlk yerleşim uyarısı `yerlesimUyarisi` olarak yanıta eklendi (UX-S27: uyarı kaybolmaz).
  - ⚠ Kendi hatamı düzelttim: ilk sürümde `yerlesimUyarisi ??= await StokDegistir(…)` yazmıştım. `??=` operatörü sağ tarafı yalnız sol taraf boşsa çalıştırır; yani ilk uyarıdan sonraki kalemlerin **stoğu hiç düşmezdi**. Çağrı artık her kalemde koşuyor; `??=` yalnız atamayı koruyor. Bu hatayı M-L1-7 mutasyonu ve L1H_10 testi kilitliyor.
- `ReyonId` gönderilmeyen bütün çağıranlarda (sipariş → satış dönüşümü, karma fiş) davranış aynı kaldı.
- **Değişmeyenler:** yasal kapılar (tevsik, MASAK, Peşin sınırı) — kural gevşetilmedi. Eşik yapılandırmadan okunuyor. Migration gerekmedi.

### 3.2 Ekran (`Pages/Islemler.razor`, `Pages/Islemler.Yazdir.cs`, `Bilesenler/Fis/FisYazdir.cs`)
- **Saf kural** `FisYazdir.FisUcuMu(kalemSayisi, ozelFis)` = `ozelFis || kalemSayisi > 1`. `ozelFis` karma, hediye ya da iskonto demektir. Kuralda eşik rakamı yok, yalnız kalem sayısına bakıyor.
- **`Islemler.FisUcunaGider`** (`tur == "SATIS" && FisUcuMu(…)`) iki şeyi birlikte besler:
  1. `Kaydet()` içindeki fiş dalını;
  2. "tek belge" kararını (`TekBelge`).
  Karar tek kuraldan okunduğu için etiket ile davranış ayrışamaz.
- **`Kaydet()` fiş dalı** eski hediye/iskonto dalının genişletilmiş hâlidir. Kalıp değişmedi: tek bacak `{arac: odemeTuru, tutarTl: ToplamTl, kasaId}`. Gövde `Govde()` ile kuruluyor, yani BASİT modun gizli alanları (vadeGun) gövdeye girmiyor. Eklenenler:
  - `reyonId = SecilenReyon`;
  - `fisIskontoYuzde = HediyeVar || IskontoVar ? fisIskontoYuzde : 0m` — düz fişe carinin oranı sessizce girmez. Hediye ve iskonto dalında gönderilen değer eskisiyle aynı;
  - yanıttan fiş no, iskonto notu ve `yerlesimUyarisi` okunuyor (UX-S27).
- **Tek kalem ve ALIS** eski döngüde kaldı. Yorumda şu yazıyor: "SATIS buraya yalnız TEK kalemle düşer (L-1). ALIS kalem başına: alış fiş ucu yok".
- **T-S1'in "N ayrı fiş" notu kaldırıldı.** Silinenler: `#islem-kaydet-cokfis` satırı ve `CokFisNotu` özelliği. Satışta her kayıt artık tek belge olduğu için etiket her zaman **"Kaydet ve yazdır"**, ve kayıttan sonra yazdırma sekmesi kendiliğinden açılır. `FisYazdir.TekBelge` imzası değişmedi; çağıran artık `FisUcunaGider` geçiriyor.
- `Islemler.razor`: 1387 → **1385** satır (≤1400). Yorumlarda eşik rakamı yok: denetim.sh `ESIK_ISTEMCIDE` ilk koşumda yorumumdaki "30.000"ı yakaladı, rakamı kaldırdım.

### 3.3 Sapmalar ve gerekçeleri
1. **Sunucuya `ReyonId` eklendi.** Görevde "gerekirse satis-fisi gövde kabulü" deniyordu. Eklenmeseydi, çok kalemli satış fiş ucuna taşınınca PRO'da seçilen reyon kaybolurdu. Bu, sessiz bir davranış değişikliği olurdu.
2. **"Aynı fiş KART ile → 200" kabulü Peşin Müşteri'de TUTMAZ, ve bu doğrudur.** B-3 (D2·K2) kuralı Peşin Müşteri'de fiş toplamı hadde ulaşan işlemi **hangi ödeme aracıyla olursa olsun** reddeder: `olcut = max(nakit, islemToplam)`. Kimliksiz kart bu işlemi yapamaz. Bu kural gevşetilmedi. KART kabulü bu yüzden **gerçek müşteriyle** (Ayse Deneme) ölçüldü: 200, tek fiş, iki kalem. Peşin + KART ise 400 verdi (L1H_02 ve canlı C1).
3. **Düz çok kalemli fişte `fisIskontoYuzde = 0`** (§ 2, son satır). Carinin oranı düz satışa eskiden de uygulanmıyordu. Oranı "boş" gönderseydim, iskontolu bir caride satış bacak eşitsizliğinden 400 alırdı.
4. **`personelId`** fiş dalında açıkça yazılı kaldı (`personelId = SaticiGovde`). Denetimdeki P-16 `DTO_FORM_ESLESMEZ` sayacı DTO alanının razor'da harfiyen görünmesini istiyor. İlk koşumda `Govde()`'ye bıraktığım için sayaç 1 verdi; düzelttim.

## 4. DoD sözleşmesi (yeni varlık / kayıt türü)

| Varlık | Durum |
|---|---|
| Yeni varlık / tablo | YOK — migration gerekmedi. |
| `SatisFisiDto.ReyonId` | Yeni **istek alanı**; yeni kayıt türü değil. Yazılan satırlar mevcut `hareketler` / `stok_yerlesimleri` satırlarıdır. |
| Çok kalemli satış fişi (`hareketler`, ortak `fis_id`) | Finansal kayıttır. Hard DELETE YASAK, PATCH yok. Düzeltme yolu mevcut 48-A storno motorudur (`POST /islemler/{hareketId}/storno`, SAHIP/MUDUR). Fiş ekranı değişmezliği ve düzeltme yolunu mevcut `DegismezIpucu` ile anlatıyor. Bu paket bu yolu değiştirmedi. Yetki: satış fişi ucu mevcut satışla aynı (`[ModulYetkisi]` sınıf düzeyinde); denetim kaydı mevcut yazma yolunda. |
| Düğmeler | Yeni düğme yok. "Kaydet ve yazdır" gerçek istek atar. Kilitliyken sebep yazar (`#islem-kaydet-kilit`, mevcut). |

## 5. Testler

**Yeni: 15 test yöntemi, 18 koşum.**
- `Kuyumcu.Tests/L1TekFisEkranTestleri.cs` (bUnit, kendi sahte API'si, gerçekçi `/islemler/fis/` yanıtı):
  - `FisUcuMu_iki_ve_ustu_kalem_ya_da_ozel_fis_EVET_tek_duz_kalem_HAYIR` (saf)
  - `Iki_kalemli_satis_TEK_satis_fisi_istegi_kalem_basi_satis_SIFIR` (BASIT + PROFESYONEL): satis-fisi 1, satis 0; gövde ayrıştırılarak sınanır — 2 kalem, tek NAKIT bacak, tutar 40.000, kasa, `fisIskontoYuzde=0`, `reyonId` anahtarı
  - `Iki_kalemli_fis_TEK_belge_Kaydet_ve_yazdir_kayittan_SONRA_yazdirma_sekmesi_acilir`
  - `Sunucu_tevsikle_reddederse_hata_GORUNUR_basari_YOK_ikinci_istek_YOK` (400 + `alanlar.cariId`; kalem başına yedek yol YOK; fiş kaybolmaz)
  - `Tek_kalemli_satis_bugunku_satis_ucunda_kalir_davranis_DEGISMEDI`
- `Kuyumcu.Tests/L1TevsikFisHttpTestleri.cs` (gerçek HTTP boru hattı, MySQL; **had sunucudan `/tevsik/gunluk` ile okunur, testte sabit rakam yok**):
  - L1H_01 Peşin + 2 kalem NAKİT, had üstü: 400, `tevsik`, `alanlar.cariId`, 0 hareket
  - L1H_02 Peşin + KART had üstü: 400 (B-3 gevşemez)
  - L1H_03 Gerçek cari + KART had üstü: 200, `kalemSayisi=2`, iki satır ortak `fis_id`
  - L1H_04 Gerçek cari + NAKİT 2 × 0,6·had: 400 (§4.1.2 fiş toplamı)
  - L1H_05 **Finansal sınırlar**, Theory: had −0,01 → 200 · had → 200 · had +0,01 → 400
  - L1H_06 **Kuruş yuvarlama**: birim = had/2 + 0,005 → kalem başına yukarı yuvarlanır (AwayFromZero) → had + 0,02 → 400
  - L1H_07 Kuruş: birim = had/2 + 0,004 → toplam tam had → 200
  - L1H_08 **3 kalem**: toplam üstü 400; altı 200, üç satır tek fiş
  - L1H_09 `reyonId`: boş reyon → 400 ve 0 hareket; dolu reyon → yalnız oradan düşer
  - L1H_10 Reyonsuz fişte atanmamış stok yetmezse yanıtta `yerlesimUyarisi` döner; 3 kalemin hepsi düşer; reyon verilince uyarı null
- **Güncellenen:** `TS1TuvalSatisTestleri.Cok_kalemli_duz_satista_etiket_KAYDET_…` → `Cok_kalemli_satis_L1_TEK_fis_etiket_KAYDET_VE_YAZDIR_kalem_basi_POST_YOK`. Eski test T-S1'deki açığı "davranış" olarak belgeliyordu; artık yeni davranışı sınıyor.

**Filtreli koşumlar:**

| Kapsam | Toplam | Geçen | Kalan |
|---|---|---|---|
| L1TekFis + L1TevsikFisHttp | 17 (L1H_10 öncesi), sonra 18 | tümü | 0 |
| L1 + TS1TuvalSatis + TS1Sinir (son derleme) | 76 | 76 | 0 |
| `Pages.Islemler`'i çizen 25 test sınıfı | 329 | 329 | 0 |

**Tam süit:** § 9'da.

## 6. Mutasyon kanıtları

Yöntem: kopya `scratchpad/mut-yedek/L-1/` altına alındı → `perl -0777 -pi` ile tek eşleşme değiştirildi → filtreli koşum → yedek geri kopyalandı → `cmp` ile birebir aynı olduğu doğrulandı. Son mutasyondan sonra temiz `dotnet build`: 0 uyarı, 0 hata.

| Kod | Değişiklik | Kırmızıya dönen testler | Geri alındı |
|---|---|---|---|
| M-L1-1 | `FisUcuMu` → `ozelFis` (çok kalemli dal eski kalem başı POST'a döner) | L1TekFis: `Iki_kalemli_satis…` (BASIT, PRO), `Iki_kalemli_fis_TEK_belge…`, `Sunucu_tevsikle_reddederse…`, `FisUcuMu…`; `TS1…Cok_kalemli_satis_L1_TEK_fis…` (6) | cmp AYNI |
| M-L1-2 | razor dalı `FisUcunaGider` → `(HediyeVar \|\| IskontoVar)` | `Iki_kalemli_satis…` (BASIT, PRO), `Iki_kalemli_fis_TEK_belge…`, `Sunucu_tevsikle…`, `TS1…Cok_kalemli_satis_L1…` (5) | cmp AYNI |
| M-L1-3 | `TekBelge` çağrısı eski `karma\|\|hediye\|\|iskonto`'ya | `Iki_kalemli_fis_TEK_belge…`, `TS1…Cok_kalemli_satis_L1…` (2) | cmp AYNI |
| M-L1-4 | `fisIskontoYuzde = … : 0m` → `fisIskontoYuzde` | `Iki_kalemli_satis…` (BASIT, PRO) (2) | cmp AYNI |
| M-L1-5 | Sunucu: `StokDegistir(…, dto.ReyonId)` → reyonsuz | L1H_09, L1H_10 (2) | cmp AYNI |
| M-L1-6 | Sunucu: yanıttan `yerlesimUyarisi` silindi | L1H_10 (1) | cmp AYNI |
| M-L1-7 | Sunucu: `yerlesimUyarisi ??= await StokDegistir(…)` (kısa devre, sonraki kalemin stoğu düşmez) | L1H_10 (1) — bu mutasyon ilk koşumda testi kırmadı; test 3 kalem ve 97 adetle güçlendirildi, sonra kırmızı oldu | cmp AYNI |

## 7. ALIŞ dalı (kapsam madde 3)

Alış için fiş ucu **yok**. Yalnız `POST /islemler/alis` (tek kalem) var. Yeni uç açmak yasak olduğu için alış dalı kalem başına kaldı. Risk şöyle:
- **Gerçek tedarikçi carisiyle** yapılan nakit alışta kapı günlük kümülatif toplama bakar (`GunlukNakitToplam`, aynı cari ve aynı yerel gün). Kalem başına bölmek toplamı düşürmez; bölünmüş istekler kümülatif toplamda yakalanır.
- **Peşin Müşteri (sistem carisi)** ile alışta kümülatif toplam yok. Kalem başına bölme orada da haddi deler.

→ **Orkestratöre:** ayrı paket gerekiyor: "alış fişi ucu (`/islemler/alis-fisi`) + ekranda çok kalemli ALIS'ın tek POST'a geçmesi". Bu paketin sınırı dışında.

## 8. Canlı doğrulama (5162, vekil 5163)

**Kurulum.**
- Kendi API'm `--urls http://localhost:5162` ile açıldı. Sistem anahtarı rastgele üretildi, chmod 600 dosyadan okundu, hiçbir çıktıya yazılmadı.
- Davet `POST /sistem/davetler` 201, kayıt `POST /auth/kayit` 201. Kiracı: `PAKETL-1-TEST canli` (`01M3B53JYQ20HS4PHVDGAVMHAZ`).
- API ile kurulanlar: kasa (200), tedarikçi ve müşteri carileri (201), 5 ürün (201), stok için 5 alış (200).
- `/tevsik/gunluk` → `{"had":30000.0,"kuralEtkin":true}`.

**API (ekranın L-1 gövdesiyle aynı şekil):**

| Adım | Beklenen | Gerçek |
|---|---|---|
| A Peşin + 2 kalem NAKİT 28.711,64 + 13.354,25 | 400, tevsik, alan cariId, 0 hareket | **400** "Peşin Müşteri ile bu tutarda işlem yapılamaz: 42.065,89 TL, tevsik (459 s. VUK GT) eşiğine (30.000,00 TL) ulaşıyor…", `alanlar.cariId` |
| B1 Peşin 2 kalem NAKİT 29.999,99 | 200 tek fiş | **200** fisId `…BFWN`, kalemSayisi 2 |
| B2 Peşin 2 kalem NAKİT 30.000,00 | 200 (tam had serbest) | **200** fisId `…ZKQ` |
| B3 Peşin 2 kalem NAKİT 30.000,01 | 400 | **400** tevsik |
| B4 Peşin 3 kalem NAKİT 30.000,02 | 400 | **400** tevsik |
| C1 Peşin 2 kalem KART 42.065,89 | 400 (B-3) | **400** |
| C2 Ayse Deneme 2 kalem KART 42.065,89 | 200 tek fiş iki kalem | **200** fisId `…E4G`, nakitToplam 0 |
| D Peşin TEK kalem `/islemler/satis` 5.000 | 200, eski yol | **200** `tur=SATIS`, fisId null |

**Gerçek tarayıcı (kendi sekmem, `http://localhost:5163/__giris` → `/islemler`):**

| Adım | Beklenen | Gerçek (ağ kaydı + SQL) |
|---|---|---|
| BASİT 1440, Peşin, Yüzük 13.354,25 + Kolye 28.711,64, Nakit, "Kaydet ve yazdır" | Tek istek, red, band, 0 hareket | Ağ kaydı: **tek** `POST /api/v1/islemler/satis-fisi → 400`, `/islemler/satis` 0. Müşteri alanının altında "Bu tutarda satış için müşterinin kimliği gerekiyor…" bandı ve [Müşteri seç] [Yeni müşteri] düğmeleri. SQL `SATIS` satır sayısı değişmedi (7 → 7) |
| Aynı fiş, müşteri Ayse Deneme, ödeme Kart | 200, tek fiş iki kalem, yazdırma açılır | `POST satis-fisi → 200` → `GET /islemler/fis/01M3B5AZHCM0DHMF0M804V8X21 → 200`. Sekme yazdırma sayfasına geçti (başlık "SATIŞ FİŞİ — ByGatech"). SQL: `fis 01M3B5AZ…X21 · 2 kalem · 42065.89 · nakit 0`; `odeme_kalemleri: KART 42065.89` |
| BASİT, Peşin, Küpe 12.000 + Kolye Ucu 9.000, Nakit | 200 tek fiş, yazdırma sekmesi | `POST satis-fisi → 200` (1 istek). `window.open("/cikti/SATIS_FISI/01M3B5RH10QE9EEB1R84N9AQZ9?yazdir=1","_blank","noopener")` çağrıldı. Bant "Fiş yeni sekmede yazdırmaya açıldı…". SQL: iki satır, ortak `fis_id 01M3B5RH0XTC2VQZDR3W0VGPXK`, 12000 + 9000 |
| BASİT tek kalem (Küpe 7.000) | Eski yol | `POST /islemler/satis → 200`, satis-fisi 0. Yazdırma yine açıldı |
| PRO, Küpe 8.000 + Kolye Ucu 6.000 | Tek fiş | `POST satis-fisi → 200` (1 istek), fis `01M3B5T3R0XD109WTACTPC03MC` (2 kalem, 14.000) |

**Tarayıcı notu (Anayasa md.1).** Pencere-açma çağrısı ilk seferde gerçekten yazdırma sayfasını açtı. Gömülü tarayıcı yeni sekme açmak yerine aynı sekmeyi kullandı ve `print()` diyaloğu sekmeyi kilitledi; o sekmeyi kapattım. Sonraki koşumlarda `window.open` çağrısı kaydedilerek ölçüldü (adres ve hedef yukarıda).

**Yerleşim ölçümü** (`betikler/sikisma-olc.js`, değişen ekran `/islemler`, fişte 2 kalem, etiket "Kaydet ve yazdır", `#islem-kaydet-cokfis` yok):

| Genişlik | Mod | Tema | K1 gizli | K3 taşan/kırık | K5 TRY | K6 seçici | K7 hiza | K8 jargon | K9 binen | K10 | K11 | Yatay | K2 kart sağ % |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1440 | BASİT | açık | 0 | 0/0 | 0 | 0/2 | 0 | 0 | **1\*** | 0 | 0 | 0 | 98 |
| 1280 | BASİT | açık | 0 | 0/0 | 0 | 0/2 | 0 | 0 | **1\*** | 0 | 0 | 0 | 98 |
| 1280 | BASİT | koyu | 0 | 0/0 | 0 | 0/2 | 0 | 0 | **1\*** | 0 | 0 | 0 | 98 |
| 375 | BASİT | açık | 0 | 0/0 | 0 | 0/2 | 0 | 0 | 0 | 0 | 0 | 0 | 97 |
| 375 | BASİT | koyu | 0 | 0/0 | 0 | 0/2 | 0 | 0 | 0 | 0 | 0 | 0 | 97 |
| 1440 | PRO | açık | 0 | 0/0 | 0 | 0/4 | 0 | 0 | 0 | 0 | 0 | 0 | 98 |
| 1280 | PRO | açık | 0 | 0/0 | 0 | 0/4 | 0 | 0 | 0 | 0 | 0 | 0 | 98 |
| 1280 | PRO | koyu | 0 | 0/0 | 0 | 0/4 | 0 | 0 | 0 | 0 | 0 | 0 | 98 |
| 375 | PRO | açık | 0 | 0/0 | 0 | 0/4 | 0 | 0 | 0 | 0 | 0 | 0 | 97 |
| 375 | PRO | koyu | 0 | 0/0 | 0 | 0/4 | 0 | 0 | 0 | 0 | 0 | 0 | 97 |

Koyu tema `prefers-color-scheme: dark` öykünmesiyle ölçüldü; gövde zemini `rgb(14, 18, 25)`.

\* **K9 = 1 bu paketin ürünü DEĞİL.** Üst üste binen iki öğe şunlar: `FisKalemGirisi`'nin barkod hata bandı ("Ürün seçildi ancak fiyatı otomatik hesaplanamadı…") ve `#islem-elle-ac` "Elle ekle". Bu band yalnız fiyat listesi tanımsız ürün okutulunca çıkıyor. Test kiracısında fiyat listesi yok, bu yüzden görüldü. Bu paket `FisKalemGirisi.razor`'a ve `tuval-satis.css`'e dokunmadı. Aynı çakışma L-1 işleminden önceki ilk ekran görüntüsünde de var. Sahibi: T-SATIS / T-S1 şeridi (§ 11).

**Ekran görüntüleri** (oturumda alındı, 1440 × 900, açık tema):
1. BASİT boş satış ekranı;
2. BASİT, 2 kalem, tevsik reddi — Müşteri altında kimlik bandı, fiş korunmuş;
3. PRO, 2 kalem tablosu;
4. BASİT, kayıttan sonra 2 kalemli yeni fiş, "Fiş yazdır" bağlantısı ve "Fiş yeni sekmede yazdırmaya açıldı" metni.

375 px ve kaydırılmış durumlarda gömülü bölmenin görüntü yakalaması boş kare döndürdü (çizim yakalama sorunu). O kombinasyonların kanıtı yukarıdaki sayısal ölçümdür. Tarayıcı bölmesi bir süre gizlendi; ölçüm JS ile sürdü.

**Kapanış.**
- API (dotnet run PID 88022 ve dinleyen süreç 88025) ve vekil (88562) `kill` ile durduruldu. `lsof` 5162 ve 5163 için boş.
- Kiracı temizliği: `kiraci-temizle.sh 01M3B53JYQ20HS4PHVDGAVMHAZ --uygula` 308 satır ve 1 kiracı sildi. Yeniden sorgu "Kiracı YOK" dedi. `information_schema`'daki tenant_id'li **70 tablonun toplamı 0**. `kiraci_davetleri` satırı silindi (1 → 0).
- **tenants: başta 12, sonda 12.**
- Sır içeren geçici dosyalar (`sistem.key`, `fikstur.json`, `canli-durum.json`) `printf ""` ile boşaltıldı.
- API günlüğünde 150 `Duplicate entry … is_calismalari` hatası var. Bunlar zamanlanmış günlük işlerin (MASAK_SIB_SURESI, SAKLAMA_ONIZLEME, VADE_HATIRLATMA) aynı veritabanına bağlı birden çok şerit API'sinde çakışmasından doğuyor ve bu paketle ilgisiz. Satış isteklerinde hata yok.

## 9. Derleme · denetim · tam süit

- `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı, 0 Hata** (son derleme).
- `dotnet/denetim.sh` → **DENETİM TEMİZ.**
  - İlk koşumda iki ✗ çıktı, ikisi de benimdi ve düzeltildi: P-16 `DTO_FORM_ESLESMEZ` (1; `personelId` harfiyen) ve B-3 `ESIK_ISTEMCIDE` (5 > tavan 4; yorumdaki "30.000").
  - Kendi sayaçlarım: `S2-3 FIS_BILESENI_ISTEK 0`, `P-16 DTO_FORM_ESLESMEZ 0`, `B-3 ESIK_ISTEMCIDE ≤ 4`.
- **Tam süit** (`--logger trx --results-directory scratchpad/trx-pL-1`): **5891 test · 5890 geçti · 1 kaldı.** Son koşum son derlemeyle yapıldı (0 uyarı, 0 hata). Kalan test: `Dogrulayici.TS2SinirTestleriTur2.Kampanya_indirimli_teklifte_iscilik_indirim_oncesi_500_YAZILMAZ` ("beklenen null ya da 400, gerçek 500"). Bu test T-S2 doğrulayıcısının 25.09 04:33 tur-3 ekidir. Saf bir fonksiyonu sınıyor: `FisProKalemKarti.TeklifIscilik`. Bu paket o dosyaya ve fonksiyona DOKUNMADI. Aynı kırmızı L-1 değişikliklerinin ilk tam koşumunda da vardı (5891 / 1). **Bu paketin ürettiği yeni kırmızı: 0.** Sahibi: T-S2b PRO şeridi (eşzamanlı). Yeni testler: 15 yöntem / 18 koşum; tam süit 5891 bunları içeriyor (paket öncesi yaklaşık 5873).

## 10. Değişen dosyalar (yalnız bu paket)

- `dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs` — `SatisFisiDto.ReyonId`, fiş döngüsünde reyonlu stok düşümü, yanıtta `yerlesimUyarisi`
- `dotnet/Kuyumcu.Web/Pages/Islemler.razor` — fiş dalı `FisUcunaGider`, çok fiş notu kaldırıldı (1385 satır)
- `dotnet/Kuyumcu.Web/Pages/Islemler.Yazdir.cs` — `FisUcunaGider`, `TekBelge` çağrısı, `CokFisNotu` silindi, başlık notu
- `dotnet/Kuyumcu.Web/Bilesenler/Fis/FisYazdir.cs` — `FisUcuMu` saf kuralı
- `dotnet/Kuyumcu.Tests/L1TekFisEkranTestleri.cs` (yeni)
- `dotnet/Kuyumcu.Tests/L1TevsikFisHttpTestleri.cs` (yeni)
- `dotnet/Kuyumcu.Tests/TS1TuvalSatisTestleri.cs` — bir test yeni davranışa çevrildi
- `dokumanlar/09-uygulama-kayitlari/PAKET-L-1-uygulama-raporu.md` (bu rapor)

**Dokunulan sayfalar:** `Pages/Islemler.razor` (satış). Gerekçe: L-1 KRİTİK yasal açık. Görevin dosya alanı bu sayfayı açıkça veriyor.

**Dokunulmayanlar:** Sözlük (`Kuyumcu.Core/I18n/Sozlukler.SatisTuval.cs`) değişmedi. `KaydetTek` ve `CokFisNotu` anahtarları artık satışta çizilmiyor. `KaydetTek`, `KaydetEtiketi` içinde tek belge olmayan hâl için emniyet olarak duruyor. Çekirdek sözlük alanım dışında olduğu için anahtarları silmedim; not § 11'de.

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| (1) ≥2 kalem SATIŞ her modda tek fiş `/islemler/satis-fisi` | KARŞILANDI | bUnit BASIT + PRO; canlı ağ kaydı (BASİT red, BASİT 200, PRO 200) |
| Tek kalem eski yolda | KARŞILANDI | `Tek_kalemli…` bUnit; canlı D ve tarayıcı tek kalem |
| `Kaydet()` tek POST yolu, fiş bileşeni istek atmıyor | KARŞILANDI | denetim `FIS_BILESENI_ISTEK 0` |
| (2) Önce ölç: satis-fisi kabul ediyor mu, kapılar brüt toplamla mı | KARŞILANDI | § 2; tek sunucu değişikliği ReyonId |
| (3) Alış: fiş ucu var mı | ÖLÇÜLDÜ: YOK | § 7 → ayrı paket önerisi |
| (4) "N ayrı fiş" notu kalktı, "Kaydet ve yazdır" + otomatik yazdırma | KARŞILANDI | bUnit `Iki_kalemli_fis_TEK_belge…`; canlı `window.open` ve yazdırma sayfası |
| (5) Sunucu ek savunma ÖNERİSİ (uygulama değil) | YAZILDI | § 12, S-L1 |
| KABUL: Peşin + 2 kalem nakit ≥ had → tek istek, red, SQL 0 | KARŞILANDI | canlı A ve tarayıcı |
| KABUL: aynı fiş KART → 200 tek fiş | KARŞILANDI (gerçek cari) · SAPMA (Peşin'de B-3 gereği 400) | C1 ve C2, § 3.3-2 |
| KABUL: 2 kalem nakit < had → 200 tek fiş | KARŞILANDI | B1, tarayıcı 21.000 ve 14.000 |
| bUnit: satis 0, satis-fisi 1 | KARŞILANDI | L1TekFis |
| Sınırlar 29.999,99 / 30.000,00 / 30.000,01; kuruş; 3+ kalem | KARŞILANDI | L1H_05–08; canlı B1–B4 |
| Gerçek tarayıcı BASİT 1440: tek fiş, yazdırma açılır | KARŞILANDI | § 8 |
| Mutasyon: çok kalemli dal eski POST'a → kırmızı | KARŞILANDI | M-L1-1, M-L1-2 |
| Eşik istemciye gömülmedi, kural gevşetilmedi | KARŞILANDI | denetim `ESIK_ISTEMCIDE` tavan içinde; kapı kodu değişmedi |
| Test kiracısı silindi, tenants eski değerde | KARŞILANDI | 12 → 12, 70 tabloda 0 satır |

## 12. Açık kalanlar ve notlar

**Kurucuya / mali müşavire**
- **S-L1 (sunucu ek savunma önerisi — UYGULANMADI).** Aynı kasiyer kısa sürede Peşin Müşteri'ye art arda eşik altı fişler kesebilir. Örnek: 5 dakikada 2 × 20.000 ₺ nakit, ayrı fişler. Ekran fişi artık bölmüyor. Ama kasiyer iki ayrı fiş keserse ya da bir API istemcisi `/islemler/satis`'i art arda çağırırsa kapı her fişi ayrı ölçer; Peşin Müşteri'de kümülatif toplam yoktur (TevsikKapisi "HÂLÂ AÇIK" md.2 ve md.5). Soru: 459/575 s. VUK GT bakımından "aynı alıcıya aynı gün bölünmüş nakit tahsilat", kimliği bilinmeyen alıcıda nasıl tespit edilir? Kasiyer + zaman penceresi temelli bir uyarı ya da ret mevzuatça gerekli ya da uygun mu? Dayanak birincil kaynaktan doğrulanmadığı için koda HİÇBİR kural yazılmadı. **DOĞRULANAMADI — mali müşavir teyidi gerekir** (MALİ-MÜŞAVİR-SORULARI'na S-L1 olarak eklenmesi önerilir).
- Tevsik haddi 30.000 ₺ sunucu yapılandırmasından geliyor (575 s. Tebliğ, RG 30.11.2024/32738; S-6'da doğrulanmış). Bu paket rakamı istemciye yazmadı.

**Orkestratöre / sahiplerine**
- **ALIŞ fiş ucu** (§ 7): ayrı paket. Sahibi satış/tevsik şeridi.
- **K9 çakışma:** BASİT ≥1280'de barkod fiyat-yok bandı "Elle ekle" düğmesinin üstüne biniyor (`FisKalemGirisi.razor` + `tuval-satis.css`). Sahibi T-SATIS / T-S1. L-1 öncesinden var.
- **Hediye/iskonto fiş dalında cari oranı (önceden var olan risk):** Hediye var ama iskonto girilmemiş bir fişte `fisIskontoYuzde` boş gidiyor. Sunucu carinin kartındaki oranı uyguluyor. Ekranın bacak tutarı iskontosuz kalıyor ve bacak eşitliği yüzünden satış 400 alıyor. Bu paket o dalın gönderdiği değeri DEĞİŞTİRMEDİ (davranış eşitliği). Sahibi P-16 / T-SATIS.
- **Sözlük:** `SatisTuvalSozlugu.CokFisNotu` artık hiçbir ekranda çizilmiyor, `KaydetTek` yalnız emniyet dalında. Sahibi T-SATIS / Çekirdek sözlük; silme kararı onlarda.
- **"Son 3 satış" kartı** (`FisSonSatislar`) fişi değil hareketleri listeliyor. Çok kalemli tek fiş orada 2 satır olarak görünüyor. L-1'den sonra çok kalemli fiş daha sık olacak, bu görünüm yanıltıcı olabilir. UX önerisi, sahibi T-SATIS.
- **Tam süitteki başkasına ait kırmızı:** `TS2SinirTestleriTur2.Kampanya_indirimli_teklifte_iscilik_indirim_oncesi_500_YAZILMAZ`. Sahibi T-S2b PRO şeridi (`FisProKalemKarti.TeklifIscilik`; kampanya indirimli teklifte işçilik indirim öncesi 500 yazılıyor).
