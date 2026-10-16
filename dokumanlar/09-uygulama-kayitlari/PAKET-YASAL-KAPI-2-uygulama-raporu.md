# PAKET YASAL-KAPI-2 — uygulama raporu (Taşkın + Ertuğ, 10.10.2026 · t3 · 5164/5165 · CDP 9610)

Tasarım hakemi KRİTİK (YASAL-KAPI-GERI kapanışı): /cariler Tahsilat çekmecesinde 250.000 ₺ nakit girilince sunucu MASAK 400 dönüyordu ("masak.aciklama 20 karakter…"). Çekmecede kimlik ve amaç alanı yoktu, tezgâhtar çıkışsız kalıyordu. Tutar 25.000'e inince eski hata kutusu da yerinde duruyordu. Aynı pakette görünüm hataları da vardı: ödeme tablosunda tutar kesiliyordu, 375 px'te "Kart" dikey kırılıyordu, aynı uyarı birden çok yerde çıkıyordu ve Kaydet kilitliyken "Fark yok, kaydedebilirsiniz" yazıyordu.

**Durum (tur 2, 10.10 16:20): BITTI.** Doğrulayıcının tur 1 bulguları kapatıldı (§0). Paket kapısının yedi maddesi tur 2 koduyla yeniden ölçüldü. Tam süitte bu paketten yeni kırmızı yok (§0.6).

## 0. Tur 2 — doğrulayıcı bulgularının kapatılması

### 0.1 [KRİTİK] PRO /islemler'de aynı MASAK eşiği 4 yerde → kapandı
**Sebep:** `Islemler.YasalKapi2.cs` yalnız BASİT kilit metnini sadeleştirmişti. PRO'da eşik 4 görünür blokta çıkıyordu: `#masak-baslik`, `#masak-kural`, `#islem-kaydet-kilit` ve `#islem-pro-amac-etiket`. Tur 1 raporundaki "kilit sebebi eşiği hiçbir modda tekrarlamıyor" cümlesi /islemler PRO için yanlıştı. Düzeltildi, bkz. §2 Sapmalar.

**Çözüm:** Eşiğin tek yeri PRO "Yasal kapılar" paneli (YASAL-PARAM-EKRAN tur 3'ün "kademe söylensin" kararı orada yaşar):
- `#islem-pro-kimlik-etiket`: "370.000 ₺ ve üstünde kimlik (MASAK)".
- `#islem-pro-amac-etiket`: "200.000 ₺ ve üstünde işlem amacıyla kimlik — teyit bekleniyor".
- PRO Kaydet kilidi eşik söylemez, neyin eksik olduğunu söyler. Yeni sözlük anahtarları (TR+EN, `YasalKapi2Sozlugu`):
  - `MasakKilitProAmac`: "Kaydetmek için kimlik ve işlem amacını doldurun; kademe Yasal kapılar panelinde."
  - `MasakKilitProKimlik`: aynı cümlenin yalnız kimlik hâli.
- `MasakFormu` yeni parametre `EsikPanelde` (varsayılan false): true iken `#masak-kural` eşik cümlesini tekrarlamaz, yalnız saklama notunu taşır ("Bilgiler şifreli saklanır."). /islemler bunu panel çizildiğinde açar (`MasakEsikPanelde = PRO && !YildizTezgahMi`). Panel olmayan ekranlar (Cariler, Banka, Hurda) değişmedi.
- Panel çizilmeyen PRO (Yıldız tezgâh) ve gereksinim gelmemişken eski kademe cümlesi kalır, çünkü eşiği söyleyen başka yer yoktur.

**Ölçüm (başsız Chrome, CDP 9610, vekil 5165, API 5164; platin; `pYASAL-KAPI-2/esikid.js` yalnız görünür blokları sayar):**

| Bileşim | /islemler 220.000 — eşik blokları | Kilit sebebi | `#masak-kural` |
|---|---|---|---|
| PRO açık 1280 | 200.000: `#masak-baslik`, `#islem-pro-amac-etiket` · 370.000: `#islem-pro-kimlik-etiket` | "Kaydetmek için kimlik ve işlem amacını doldurun; kademe Yasal kapılar panelinde." (eşik 0) | "Bilgiler şifreli saklanır." |
| PRO açık 375 | aynı (2 + 1) | aynı (eşik 0) | aynı |
| PRO koyu 1280 | aynı (2 + 1) | aynı (eşik 0) | aynı |
| PRO koyu 375 | aynı (2 + 1) | aynı (eşik 0) | aynı |
| BASİT açık/koyu × 1280/375 (4) | 200.000: yalnız `#masak-baslik` | "Kaydetmek için kimlik bilgilerini doldurun." | yok (Sade) |

PRO koyu 1280/375 ilk koşumda kalem eklenemedi ("Önce ürün seçin", ölçüm geçersiz). İki bileşim yeniden koşuldu, sonuç tabloda (`-tekrar` satırları). Hedefe ulaşıldı: form başlığı + PRO panel; kilit sebebinde 0.

**bUnit:** `YK2_11_PRO_islemler_220000_esik_baslik_ve_panelde_kilit_sebebinde_ESIK_YOK` (PRO /islemler: 200.000 tam 2 blok = başlık + panel; 370.000 tam 1 blok = panel kimlik satırı; kilit metninde sayı yok; kural = saklama notu). `YK2_12_BASIT_islemler_220000_esik_tek_yerde_kilit_sade` (BASİT: 1 blok).

**Başkasının testlerinde güncellenen beklentiler** (doğrulayıcının "kademe PRO yan panelde; tek yer orası olsun" kararı). Her birinde kademe iddiası panele taşındı, kilit iddiası "eşik sayısı yok" oldu:
- `YasalParamTur3Testleri` Tur3_…KIMLIK…: panel `#islem-pro-kimlik` kırmızı + MASAK.
- `YasalParamTur3Testleri` Tur3_…AMAC…: `#islem-pro-amac` kırmızı, `#islem-pro-kimlik` yeşil.
- `SATIS3EkranTestleri` T19 (3 iddia).
- `SATIS3T24YasalMesajTestleri` T24 (PRO dalı).
- `Toplu5Testleri` T5_PRO_degisim: MASAK atfı `#islem-pro-kimlik-etiket`'te, kural = saklama notu.
- `Toplu5KaynakTestleri.T5_Islemler_razor…`: çağrı satırına `EsikPanelde="@MasakEsikPanelde"` eklendi; Islemler.razor 1.397 satır, tavan 1.400.

### 0.2 [ORTA] Avans al/iade ve değişimde sunucuda MASAK kapısı yok → bu paketin alanı dışında, ayrı sunucu paketi
Doğrulayıcı da paketin alanı dışında olduğunu yazdı. İstemciye sahte kural konmadı. §11'deki kurucu/mali müşavir sorusu aynen geçerli. Ayrı paket önerisi: "SatisModulu.P21 AvansAl (:492) ve Degisim (:718) için MasakKapisi.DogrulaKumulatif; P21 DTO'larına Masak alanı; mevzuat kuralı gereği önce birincil kaynak (MASAK Tedbirler Yönetmeliği md.5/1-b, bağlantılı işlem) ve mali müşavir teyidi." Orkestratör kuyruğuna (§13).

### 0.3 [DÜŞÜK] denetim.sh GIRDI-2 TEST_GECERSIZ_FIKSTUR=1 → başka paketin dosyası, dokunulmadı
`Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` `Iban = "TR200000000000000000000002"`. Dosya 9 Ekim 15:38 tarihli, sahibi MASAK-ESZAMAN doğrulayıcısı. Görev sınırı gereği değiştirilmedi. Orkestratör kapısına not (§13).

### 0.4 [DÜŞÜK] Her tuş vuruşunda /masak/gereksinim → kapandı (gecikmeli soru)
`ParaMasakKapisi.SoruGecikmesi` (sayfa kurucusu `Olustur` → 250 ms). Soru bu süre bekler; arada tutar yine değişirse eski soru hiç gönderilmez (nesil sayacı). Beklerken `Gereksinim` null kalır, kilit "Kimlik formu hazırlanıyor…" olur (fail-closed, güvenlik kaybı yok). Doğrudan kurulan kapı (bUnit saf testleri) 0 ms ile eski davranışta kalır.
- **Gerçek Chrome:** "2 → 25 → 250 → 2500 → 25000 → 250000" 60 ms arayla yazıldı. `/masak/gereksinim` GET sayısı 16 bileşimin 16'sında **1** (eskiden 6). Form açık. 25.000'e inince form, ret ve kilit yok.
- **Testler:** `YK2_S09_tus_vurusu_bekleme_icinde_yeni_tutar_gelirse_eski_soru_HIC_gonderilmez` (saf) ve `YK2_14_tahsilat_tutari_tus_tus_yazilinca_gereksinim_TEK_kez_sorulur` (bUnit /cariler).

### 0.5 Ek: tur 1'in kendi uyarısı giderildi
`YasalKapi2EkranTestleri.esikYok` hiç atanmıyordu (CS0649). Alan artık `YK2_13_esik_alinamazsa_sunucu_reddi_cikissiz_kalmaz_ham_metin_yok_tutar_inince_kalkar` testinde kullanılıyor. Senaryo: eşik okunamaz, sunucu reddeder, bantta `MasakEsikYok` görünür, ham metin çıkmaz, tutar inince bant kalkar. Build 0 hata, 0 uyarı (çözüm geneli, `build-final.log`).

### 0.6 Tur 2 kapı ölçüleri
| Madde | Sonuç |
|---|---|
| Build | Son `dotnet build Kuyumcu.sln` → **0 Hata · 0 Uyarı** (`build-final.log`, artımlı). Tam derlemede görülen 6 uyarının hepsi başka şeritlerin dosyalarında (§13); bu paketin uyarısı 0 (tur 1'in CS0649'u giderildi). Ara koşumlarda başka şeritten 2 geçici hata görüldü (`IslemBantGosterim.cs:66`, ISLEM-BANT, 15:22 düzenlemesi). 90 sn sonra kendiliğinden yeşile döndü; o dosyaya dokunulmadı. |
| Filtreli testler | YK2_* 28/28 ✓ · etkilenen geniş küme (YK2, Tur3, T19, T24, T5_PRO_degisim, Toplu5 kaynak) 116 geçti, 1 atlandı, 0 başarısız. |
| Tam süit (tur başına 1; `KUYUMCU_TEST_DB=kuyumcu_test_t3`; trx `/private/tmp/kuyumcu-serit/trx-pYASAL-KAPI-2-t2/_Mac_2026-10-10_16_02_12.trx`) | **12.169 toplam · 12.155 geçti · 11 başarısız · 3 atlandı.** Kırmızıların hiçbiri bu paketin değil (aşağıda). **Yeni kırmızı: 0.** |
| denetim.sh (bash) | Yalnız iki ✗: **P-30** (KABUL'de hariç) ve **GIRDI-2 TEST_GECERSIZ_FIKSTUR** (MASAKESZAMANSinirTestleri.cs:187, başka paket, §0.3). Bu paketin sayaçları ✓. |
| Mutasyon (tur 2) | 5/5 kırmızı, cmp AYNI (tablo aşağıda). |
| Canlı API + SQL | Yeni kiracıyla yeniden koşuldu (§0.7). |
| Test kiracısı | `PAKETYASAL-KAPI-2-TEST c2fbf2` (01M4JXHSSF5H1KWXET31VTRXHY) `zsh kiraci-temizle.sh … --uygula` ile silindi (553 satır + tenants 1). Davet satırı silindi (kalan 0). |

**Tam süit kırmızıları ve sahipleri:**
- `RaporDortTestleri.RD_KH01`, `RD_KH02`: RAPOR-DORT. Tur 1'de de vardı.
- `S24EskiAltinEkranTestleri.S24_Satici_Kimligi…`, `S24_Fark_Sifir…`: tek başına koşumda da kırmızı. Gerçek metin "Eski altın: Fiyat doğruysa bunu yalnız dükkân sahibi ya da müdür onaylayabilir…". Bu ISLEM-BANT(-2) bant onay kilidi; o şeridin yarım işi.
- `Q102EkranSinir` ×2, `Q102T3EkranSinir` ×2, `ZOS11_hurda_ekran_turkce…`, `ZOSR18…`, `Yasal4HttpTestleri.Y4A_tur4_eszamanli…`: tam süitte zaman aşımı. Tek başına yeniden koşumda (`kirmizi-tekrar.log`) **geçti**, yük altında kararsız. Hurda/kur bant ekranları ISLEM-BANT ve HURDA-ONAY şeritlerinin alanı. MASAK kapısıyla ilgileri yok.

**Mutasyon tablosu (tur 2):** yöntem kopya → `perl -0777` tek eşleşme → filtreli test → geri kopya → `cmp`.

| Kod | Değişiklik | Kırmızı testler | cmp |
|---|---|---|---|
| T2-1 | `Islemler.YasalKapi2.cs` PRO dalı `MasakEsikPanelde ?` → `false ?` (eski eşikli kilit metni) | YK2_11, Tur3_…KIMLIK…, Tur3_…AMAC…, T19_iki_kalem…, T19_tek_kalem… (+ YK2_06 bu koşumda kırmızı, yük kararsızlığı; tek başına yeşil) | AYNI |
| T2-2 | `MasakFormu.razor` `EsikPanelde ?` → `false ?` (kural satırı eşiği yine tekrarlar) | YK2_11, Toplu5IslemTestleri.T5_PRO_degisim… | AYNI |
| T2-3 | `Islemler.razor` `EsikPanelde="@MasakEsikPanelde"` silindi | YK2_11, T5_PRO_degisim…, Toplu5KaynakTestleri.T5_Islemler_razor… | AYNI |
| T2-4 | `ParaMasakKapisi.cs` `if (n != nesil) return true;` (gecikme sonrası nesil koruması) silindi | YK2_S09 | AYNI |
| T2-5 | `ParaMasakKapisi.Olustur` `SoruGecikmesi = 250 ms` silindi | YK2_14 | AYNI |

T2-1'in ilk koşumu kırmızı vermedi. O koşumun build'i 3 dk 38 sn sürdü ve başka şeridin eşzamanlı build'iyle yarıştı: ölçülen dll mutasyonsuz koddu. Aynı mutasyon tekrarlandı (T2-1b, build 28 sn) ve yukarıdaki kırmızıları verdi. Son mutasyondan sonra çözüm geneli temiz build 0/0.

### 0.7 Canlı doğrulama (tur 2, API 5164, kuyumcu_net)
Yeni kiracıda `canli.py` yeniden koşuldu: 42 adım, 36'sı beklenenle aynı. Ayrılan 6 adım tur 1'dekinin aynısı ve betik beklentisinin eseri:
- DövizTahsilat'ın 5 adımı nakit tevsik kapısında (30.000 ₺) MASAK'tan önce 400 döner.
- DövizVeresiye 199.999,99 USD kur çevriminde 200.000,09 ₺ olur, yani eşik üstü; 400 doğru.

SQL kanıtı:
- Kiracıdaki `masak_beyanlari`: 10 satır.
- `TAHSILAT` 5 hareket / 3 beyanlı, `VERESIYE` 5 / 3, `DOVIZ_VERESIYE` 5 / 4, `DOVIZ_TAHSILAT` 1 / 0.
- Eşik altı (199.999,99 ve aşağı-25.000) hareketlerde beyan 0. Eşik üstü beyanlı her hareketin beyanı 1.

Tur 2'de sunucu kodu değişmedi. Bu koşum yalnız aynı sözleşmenin yeni ağaçta sürdüğünü gösterir. API (kendi PID'i ve çocuğu), vekil ve Chrome durduruldu; `lsof` 5164, 5165 ve 9610'da dinleyen yok. `tenants` sayısı 22. Başlangıçta 23'tü; arada başka bir şerit kendi kiracısını sildi, bu paketin net etkisi 0.

### 0.8 Tur 2 ölçüm matrisi (değişen ekranlar; 16 bileşim × 3 adım = 48 satır + 6 tekrar satırı; ham `pYASAL-KAPI-2/olcum-t2.tsv`)
- **/islemler 220.000, platin (8 bileşim):** K1 0, K3 0/0, K3c 0, K10c 0, K5 (TRY) 0, K9 0, yatay 0. Eşik blokları yukarıdaki tabloda.
- **/cariler tahsilat, tuş tuş 250.000 → 25.000 (16 bileşim):** BASİT'te eşik yalnız `#masak-baslik`. PRO'da `#masak-baslik` (200.000) + `#masak-kural` (370.000 ve 200.000 kademeleri tek cümlede, teyit notuyla; panel yok). Ölçer blok başına ilk eşiği sayar.
  - K1, K3c, K10c, K5 ve yatay 0; gereksinim GET 1; form açılıp kapanıyor.
  - K9=6/4/2 ve Yıldız 375'teki K3 kırık=1 tur 1'deki değerlerle birebir aynı (`olcum.tsv`). K9, çekmecenin arkasında kalan sayfa öğeleridir; çekmece içi örtüşme tur 1'de 0 ölçüldü. Bu turda Cariler razor/CSS'ine dokunulmadı.
- **/islemler, Yıldız (8 bileşim):** Yıldız tezgâhında betik kalem ekleyemiyor (tur 1'de de "kaydetKilitli=-"). Yıldız tezgâh YILDIZ-4C-2 şeridinin alanı. MasakFormu orada `EsikPanelde=false` ile eski davranışta kalır.
- **Görüntüler (Read ile bakılan 4):**
  - `foto/t2-platin-PROFESYONEL-aydinlik-1280-islemler.png`: Yasal kapılar paneli iki kademe satırıyla.
  - `foto/t2-platin-PROFESYONEL-karanlik-375-tekrar-islemler.png`: 375 koyu, Kart tek satır.
  - `foto/t2-platin-PRO-karanlik-1280-masak.png`: form başlığında eşik; kural satırında yalnız "Bilgiler şifreli saklanır."
  - `foto/t2-platin-PRO-karanlik-1280-kaydet.png`: kilit sebebinde sayı yok.

### 0.9 Tur 2 değişen dosyalar
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.YasalKapi2.cs` (+2 anahtar TR+EN)
- `dotnet/Kuyumcu.Web/Bilesenler/MasakFormu.razor` (+`EsikPanelde`)
- `dotnet/Kuyumcu.Web/Bilesenler/ParaMasakKapisi.cs` (+`SoruGecikmesi`)
- `dotnet/Kuyumcu.Web/Pages/Islemler.YasalKapi2.cs` (PRO kilit metni + `MasakEsikPanelde`)
- `dotnet/Kuyumcu.Web/Pages/Islemler.razor` (tek öznitelik; gerekçe: aynı paketin ikinci turu, aynı satır)
- Testler:
  - `dotnet/Kuyumcu.Tests/YasalKapi2IslemlerProTestleri.cs` (yeni)
  - `YasalKapi2Testleri.cs` (+S09, +YK2_13, +YK2_14)
  - `YasalParamTur3Testleri.cs`, `SATIS3EkranTestleri.cs`, `SATIS3T24YasalMesajTestleri.cs`, `Toplu5Testleri.cs` (beklenti güncellemesi)

---

**Tur 1 durumu (tarihçe):** BITTI beyan edilmişti. Doğrulayıcı PRO /islemler eşik tekrarı (KRİTİK) yüzünden geçirmedi; tur 2 bunu kapattı.

## 1. Önce: eski kodda gerçek Chrome'da yeniden üretim

Kiracı `PAKETYASAL-KAPI-2-TEST`, 26 karakterlik kasa adı `Merkez Şube Ana Nakit Kasa`.

| # | Senaryo (eski kod) | Sonuç |
|---|---|---|
| Ö1 | /cariler Tahsilat 250.000 → Kaydet → 25.000 | Kimlik formu çıkmadı. Bantta ham metin kaldı: "MASAK: … MASAK beyan açıklaması (masak.aciklama) …". Tutar 25.000'e inince de bant DURDU. |
| Ö2 | /islemler BASİT 1280, 220.000 ₺, müşteri seçili | ALDIM tablosu kabından 27 px taştı, tutar hücresi sağdan kesildi. Yeni ölçer: `K10c=1 td.sayi "220.000 ₺"` |
| Ö3 | /islemler BASİT 375, aynı fiş | "Kart" 12 px'lik hücrede 4 satıra kırıldı. Yeni ölçer: `K3c=1 td.fis-tuval-ad "Kart"` (eski K3 ölçümü yakalamamıştı) |
| Ö4 | /islemler BASİT, Kaydet kilitli | `#islem-fark-durum` = "Fark yok, kaydedebilirsiniz." |
| Ö5 | /islemler BASİT, kimlik formu açık | Eşik cümlesi ("200.000 ₺ ve üstünde") 2 blokta geçiyordu: form başlığı ve Kaydet kilit sebebi. |

## 2. Yapılanlar

**Ortak bileşen `Bilesenler/ParaMasakKapisi.cs` (yeni).** Satış dışı her para formu bu tek kuralı kullanır:
- (a) **Eşik üstünde ortak MasakFormu açılır.** Eşik `/masak/esikler` ucundan gelir (`KimlikIstenenTutar`). Müşteri biliniyorsa, eşiğin altında da `/masak/gereksinim?tutar&cariId` ile bugünkü bağlantılı toplam sorulur. Kaydet kilitlenir ve sebebini yazar: "Kaydetmek için kimlik bilgilerini doldurun.". Eşik sayısını yalnız form başlığı söyler.
- (b) **Yeniden değerlendirme.** Tutar ya da müşteri değişince sunucuya yeniden sorulur. Geç gelen eski yanıt yeni tutarın üstüne yazılmaz (nesil sayacı). Sunucu reddi `YasalKapiIzi` olarak tutulur. Tutar reddin ölçüsünün altına inerse ya da müşteri değişirse form, kilit ve ret cümlesi kalkar; tutar geri çıkınca hepsi geri gelir. Girilen beyan silinmez. Gövdeye `masak` yalnız kapı açıkken gider.
- (c) **Ham metin basılmaz.** MASAK reddi formu açar. Form eksikse ne yapılacağını kilit sebebi söyler. Form tamken gelen ret, alan anahtarlarından arındırılmış tek cümleyle yazılır: "Kimlik bilgileri kabul edilmedi: …". `SadeMetin`, `(masak.xxx)` anahtarlarını ve 409 cümlesine gömülü `(MASAK: …)` parçasını çıkarır.
- **Fail-closed.** Gereksinim alınamazsa Kaydet kilitli kalır ve sebebi yazılır. Eşik alınamazsa ve sunucu reddederse çıkışsız kalınmaz: "kimlik formu açılamadı, sayfayı yenileyin" yazılır.

**Bağlanan formlar:**
- **Cariler** (`Cariler.razor` + yeni `Cariler.YasalKapi2.cs`): Tahsilat çekmecesi, Veresiye penceresi, Döviz tahsilat ve Döviz veresiye. Döviz formlarında ölçü, kurdan türetilen TL karşılığıdır (ekrandaki salt okunur satırla aynı hesap). Veresiye penceresine kilit sebebi satırı eklendi (`#vr-kaydet-kilit`, UX-S30). Yeni id'ler: `#th-masak`, `#th-masak-ret`, `#vr-masak`, `#vr-masak-ret`, `#vr-kaydet-kilit`, `#dv-masak`, `#dv-masak-ret`, `#dt-masak`, `#dt-masak-ret`.
- **Siparişten satış** (`Islemler.Diger.cs`, `FisDigerSiparis.razor`, `FisDigerModelleri.cs`): sunucu `SiparisSatisaCevirDto.Masak` alanını kabul ediyordu ama ekran göndermiyordu, bu yüzden eşik üstündeki teslim çıkışsızdı. MasakFormu, eylem şeridindeki kilit (`EkSebep`) ve ret izi eklendi.
- **Banka dekontu (müşterili)** (`Banka.razor`): kimlik formu zaten vardı. Eklenenler: sunucu reddi iz deseniyle tutuluyor, ret cümlesi tutar inince kalkıyor ve ham anahtar basılmıyor. **Dekont eşleştirme:** sunucunun 409 cümlesindeki gömülü ham MASAK parçası basılmıyor.
- **Evrak nakit tahsili** (`Evraklar.razor`): sunucuda MASAK kapısı yok, yalnız tevsik var. Ret metni sadeleştirildi ve hedef (kasa ↔ banka) değişince eski ret kalkıyor.

**Görünüm:**
- **`wwwroot/css/yasal.css`:** ALDIM tek ödeme satırında ödeme türü ve tutar tek satırda kalır; kasa adı sözcük aralarından sarar. 375 px'te ızgara sırası `auto minmax(0,1fr) auto` oldu. Sabit renk yok.
- **`FisVerdimAldim.razor`:** yeni `KaydetKilitli` parametresi. Kilitliyken fark satırı "Fark yok." der. `Islemler.razor` satır 76'ya tek öznitelik eklendi.
- **`Islemler.YasalKapi2.cs` (yeni) + `Islemler.razor`'da tek ifade değişikliği:** BASİT'te kimlik formu açıkken Kaydet kilidi eşiği tekrarlamaz, `ZO.MasakKilitSebepBasit` yazar. PRO'nun kademe cümlesi DEĞİŞMEDİ.
- **Ölçer `betikler/sikisma-olc.js`:** iki yeni sayaç eklendi.
  - **K3c kelimeKirik:** tek kelimesi birden çok satıra bölünen metni sayar, öğenin çocuk sayısına bakmaz.
  - **K10c kesikDeger:** kaydırma ya da kırpma kabının kenarında yarım kalan sayıyı ve değeri sığmayan girdi kutusunu sayar.
  - İkisi de eski kodda kusuru yakaladı (Ö2, Ö3), yeni kodda 0.

**Sözlük `Core/I18n/Sozlukler.YasalKapi2.cs` (TR+EN):** MasakHazirlaniyor, MasakSorguHata, MasakEsikYok, MasakKabulEdilmedi, FarkYok, VeresiyeTutarGirin.

### Tarama tablosu (sınıf: MASAK/tevsik eşiğine takılabilen para formları)

| Ekran / form | Sunucu kapısı (kod) | Yön | Eşik | (a) MasakFormu | (b) yeniden değerlendirme | (c) ham metin yok |
|---|---|---|---|---|---|---|
| Cariler · Tahsilat (TL) | MASAK `TahsilatGirHareket` + tevsik | giriş | sunucudan (200.000 / 370.000) | EVET (yeni) | EVET (iz + gereksinim) | EVET |
| Cariler · Veresiye | MASAK `VeresiyeGir` | borç | aynı | EVET (yeni) | EVET | EVET |
| Cariler · Döviz tahsilat | MASAK `DovizTahsilatGir` + tevsik (efektif nakit) | giriş | TL karşılığı | EVET (yeni) | EVET | EVET |
| Cariler · Döviz veresiye | MASAK `DovizVeresiyeGir` | borç | TL karşılığı | EVET (yeni) | EVET | EVET |
| Siparişten satış (Diğer ▾) | MASAK `SatisFisi` (Masak alanı) | giriş | fiş toplamı | EVET (yeni) | EVET | EVET |
| Banka dekontu (müşterili) | MASAK `HareketEkleIc` | giriş/çıkış | aynı | vardı (Paket 41) | EVET (ret izi yeni) | EVET (yeni) |
| Banka · dekont eşleştirme | MASAK → 409 (storno yolu, bilinçli) | — | dekont günü | YOK (sunucu tasarımı: sonradan kimlik alınamaz) | — | EVET (gömülü ham parça yeni çıkarıldı) |
| Evrak nakit tahsili | yalnız tevsik (`TahsilEdildi`) | giriş | — | YOK (sunucuda MASAK kapısı yok) | hedef değişince ret kalkar (yeni) | EVET |
| Hurda alış/satış | MASAK + tevsik | — | — | vardı (YASAL-KAPI-GERI) | vardı (`masakZorla` izi) | vardı |
| Sipariş kapora (SiparisDetay) | MASAK `TahsilatGirHareket` | giriş | — | vardı (KAPORA-ESZAMAN) | vardı | vardı. **DOKUNULMADI:** KAPORA-EKRAN şeridi |
| Avans al / iade (Diğer ▾) | yalnız tevsik (`AvansAl` / `AvansIade`) | giriş/çıkış | — | YOK (sunucuda MASAK kapısı yok, §11) | tevsik reddi kısa cümle (vardı, t22) | — |
| Değişim (Diğer ▾) | yalnız tevsik (`Degisim`) | — | — | YOK (sunucuda MASAK yok) | vardı (t22) | — |
| Emanet giriş/çıkış | MASAK/tevsik kapısı yok | — | — | — | — | — |
| Satış / takas / iade (Islemler) | MASAK + tevsik | — | — | vardı | vardı (YASAL-KAPI-GERI) | vardı |

### Sapmalar ve gerekçeleri
- **Avans, değişim, emanet ve evrak tahsili:** spesifikasyon "her birinde MasakFormu" diyordu. Ama bu uçlarda sunucuda MASAK kapısı YOK: gövdede `masak` alanı yok ve 400 dönmüyor. Bu ekranlara form koymak sahte bir yasal kural yazmak olurdu (MEVZUAT KURALI). Bunun yerine tarama tablosuna "kapı yok" yazıldı ve konu kurucu / mali müşavire soru olarak düşüldü (§11).
- **Dekont eşleştirmede form yok:** sunucu bilinçli olarak 409 dönüyor ("sonradan kimlik alınamaz", BankaModulu yorumu). Burada yalnız (c) uygulandı.
- **PRO'da eşik sayacı 2:** BASİT'te 1. PRO'da ortak MasakFormu'nun `#masak-kural` satırı iki kademeyi birlikte açıklıyor (370.000 kimlik / 200.000 amaç). Bu ayrıntı PRO'ya ait, satış, banka ve hurdada da aynı bileşen kullanılıyor. Değişmedi. ~~Kilit sebebi ise eşiği hiçbir modda tekrarlamıyor~~ — **tur 2 düzeltmesi:** bu cümle /islemler PRO için YANLIŞTI (ölçüm: 200.000 ₺ 3 blok, 370.000 ₺ 2 blok; YK2_01 yalnız /cariler'i sınıyordu). Tur 2'de PRO kilit sebebi eşik söylemiyor, /islemler PRO'da kural satırı yalnız saklama notu; eşik form başlığı + PRO "Yasal kapılar" paneli (§0.1, YK2_11). /cariler PRO'da (panel yok) `#masak-kural` iki kademeli kuralı (370.000 kimlik — MASAK; 200.000 amaç — mali müşavir teyidi bekleniyor) söyleyen TEK yer olarak kalır; 200.000 sayısı başlıkta ve bu kural cümlesinde geçer (YK2_01 PRO = 2, doğrulayıcının tur 1'de kabul ettiği değer). Ölçüm aracı (`esikid.js`, `tekrar.js`) blok başına ilk eşiği saydığı için tabloda kural bloğu 370.000 altında görünür — açık not, §13.
- **Satış BASİT üst bandı korundu:** "Bu işlem yasal bir kapıya takıldı; gereken alanlar açıldı." (SATIS-3 t24 kararı). Bu bant eşik söylemiyor, alanların neden açıldığını söylüyor. Değiştirilmedi.

## 3. Migration
Gerekmedi (şema değişikliği yok, sunucu kodu değişmedi).

## 4. Testler

Yeni test sayısı **23 koşum**, hepsi `Kuyumcu.Tests/YasalKapi2Testleri.cs` dosyasında:

**Saf (11 koşum):**
- S01 sınır tablosu (4 satır): 199.999,99 / 200.000 / 250.000 / 370.000 için form, gövde ve kilit.
- S02 yukarı-aşağı-yukarı: iz ve beyan korunur.
- S03 bugünkü toplam: eşik altında da form açılır, müşteri değişince yeniden sorulur.
- S04 form tamken ret: tek sade cümle, tutar inince kalkar, geri çıkınca gelir.
- S05 `SadeMetin`: gerçek 409 cümlesiyle.
- S06 geç gelen eski yanıt yazılmaz.
- S07 eşik alınamazsa ret çıkışsız kalmaz.
- S08 sözlük TR+EN.

**bUnit (12 koşum):**
- YK2_01 (BASİT 1 / PRO 2): Tahsilat 250.000 → form, kilit, eşik sayacı, dokunulmadan hata yok, ham metin yok.
- YK2_02: beyanlı POST gövdesinde `masak` var (JSON ayrıştırılarak). 199.999,99'da gövdede `masak` yok.
- YK2_03: 250.000 → 25.000 → 370.000, beyan korunur.
- YK2_04: sunucu reddi (bugünkü toplam) → alanlar açılır, "masak.aciklama" ve "şu an 0" yok; tutar inince form, ret ve kilit kalkar.
- YK2_05: veresiye (200.000 / 199.999,99).
- YK2_06: döviz tahsilat (TL karşılığı 340.000 → 34.000).
- YK2_07: döviz veresiye.
- YK2_08 (2 koşum): fark satırı kilitliyken "Fark yok."
- YK2_09: siparişten satış (250.000 → 199.999,99).
- YK2_10: banka dekontu reddi.

Başkasının testinde güncellenen beklenti: `SATIS3T24YasalMesajTestleri.cs` T24 (BASİT). Kimlik formu açıkken Kaydet ipucu artık `ZO.MasakKilitSebepBasit`. PRO beklentisi aynen kaldı. Bu değişiklik MK7 mutasyonunda kırmızıya döndü.

| Koşum | Sonuç |
|---|---|
| `~YasalKapi2` (son kod) | 23/23 |
| `~YasalKapi2 \| ~BankaEkran \| ~T24 \| ~Banka` (son kod, Banka düzeltmesinden sonra) | 234 geçti, 0 başarısız, 1 atlandı |
| Geniş ilgili küme (SATIS3, YasalParam, CariEkran, Tevsik, ParaDemo, SonKalanlar, Toplu2/5, UX4b, S32, TM1, KesifP2, Banka, Evrak, Siparis, YasalKapi, ZOISLEMA, Masak, Kapora, Diger) | 2.598 test. Tek kırmızı T24(BASIT) çıktı, beklenti güncellendi, yeniden koşumda yeşil. |
| **Tam süit** (tur başına bir kez; `KUYUMCU_TEST_DB=kuyumcu_test_t3`; trx `/private/tmp/kuyumcu-serit/trx-pYASAL-KAPI-2/_Mac_2026-10-10_14_32_22.trx`) | **12.102 toplam, 12.097 geçti, 2 başarısız, 3 atlandı.** Kırmızılar: `RaporDortTestleri.RD_KH01`, `RD_KH02` ("Rapor dönemi en çok 400 gün olabilir"). Sahibi RAPOR-DORT; AKTIF-KOSUM 13:29'da açık olarak kayıtlı. **Bu paketten yeni kırmızı: 0.** |

Not: tam süit, Banka.razor'daki son küçük düzeltmeden önceki derlemeyle koştu. Düzeltme şuydu: `dekontRedIzi = null` catch bloğundan deneme başına taşındı (P-11 sayacı için). Düzeltmeden sonra ilgili 234 test ve MK11 yeniden koşuldu ve yeşil.

## 5. Mutasyon kanıtları

Yöntem: yedek `/private/tmp/kuyumcu-serit/mut-yedek/YASAL-KAPI-2/`, `perl -0777` ile tek eşleşmeli değişiklik, build, `--filter ~YasalKapi2|~T24`, yedeği geri kopyalama ve `touch`, son olarak `cmp`. Betik: `pYASAL-KAPI-2/mut.sh`.

| Kod | Değişiklik | Kırmızı testler | cmp |
|---|---|---|---|
| MK1 | `RedGecerli` ⇒ `redIzi is not null` (yapışkan ret) | YK2_S02, YK2_S04 | AYNI |
| MK2 | `Govde` ⇒ hep `Model` (eşik altında da beyan gider) | S01(199.999,99), S02, YK2_02, YK2_04, YK2_05, YK2_06, YK2_09 (7) | AYNI |
| MK3 | `EsikUstu` `>=` ⇒ `>` | S01(200.000) | AYNI |
| MK4 | Cariler Tahsilat'ta MASAK reddi yakalaması silindi (ham metin bantta) | YK2_04 | AYNI |
| MK5 | `SadeMetin` gömülü `(MASAK: …)` çıkarma satırı silindi | YK2_S05 | AYNI |
| MK6 | `FisVerdimAldim` kilitliyken de "kaydedebilirsiniz" | YK2_08(kilitli) | AYNI |
| MK7 | `MasakEksikMetniTekYer` BASİT dalı kapatıldı | T24(BASIT) | AYNI |
| MK8 | `Tazele` nesil koruması kaldırıldı | YK2_S06 | AYNI |
| MK9 | Tahsilat `KaydetAktif`'ten MASAK kilidi çıkarıldı | YK2_01×2, YK2_03, YK2_04 | AYNI |
| MK10 | Siparişten satış `EkSebep` = null | YK2_09 | AYNI |
| MK11 | Banka MASAK reddi yakalaması silindi (son kodla tekrarlandı) | YK2_10 | AYNI |

CSS düzeltmesi bUnit ile ölçülemez. Kanıtı gerçek Chrome'daki önce/sonra ölçümüdür: K10c 1→0 (1280), K3c 1→0 (375); §7.

Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: 0 uyarı, 0 hata. Kaynakta mutasyon izi yok (grep 0).

## 6. denetim.sh
`pYASAL-KAPI-2/denetim.log`: 315 ✓. İki ✗ var, ikisi de bu paketin değil:

- **P-30 DEMO_INSAN_SURESI:** bilinen, kapsam dışı.
- **GIRDI-2 TEST_GECERSIZ_FIKSTUR 1:** `Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` IBAN. Sahibi MASAK-ESZAMAN, YASAL-KAPI-GERI raporunda da aynı.

İlk koşumda **P-11 DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM** 48 > 47 çıkmıştı. Sebebi bu paketin Banka catch bloğundaki `dekontRedIzi = null;` satırıydı. Atama deneme başına taşındı ve sayaç 47'ye döndü (✓). Bu paketin sayaçları: B-3 ESIK_ISTEMCIDE 4 (tavan 4) ✓, ModalPencere kullanımı tavan altında ✓ (bu paket yeni ModalPencere açmadı), satır içi style eklenmedi ✓.

## 7. Gerçek tarayıcı ölçümü

**Ortam:** başsız Chrome (CDP 9610, profil `pYASAL-KAPI-2/chrome`), vekil 5165, API 5164. Bileşimler: platin + yıldız × BASİT + PRO × açık + koyu × 1280×800 + 375×812. Ölçer `sikisma-olc.js` (yeni K3c/K10c dahil), uyarı tekrar sayacı `pYASAL-KAPI-2/tekrar.js` ve çekmece içi örtüşme ölçüsü (kırpılan alan hesaba katılarak). Ham satırlar `pYASAL-KAPI-2/olcum.tsv` ve `olcum-banka.tsv` dosyalarında, 64 satır.

**Özet:**
- /cariler Tahsilat, 250.000 (32 ölçüm):
  - Form açık, Kaydet kilitli, sebep "Kaydetmek için kimlik bilgilerini doldurun.".
  - Dokunulmadan alan hatası 0, ham `masak.aciklama` 0.
  - K1/K3c/K10c/K5/K11/yatay hepsi 0, çekmece içi örtüşme 0.
  - Eşik cümlesi BASİT'te 1. PRO'da başlık + kural satırı (bkz. sapma).
- 25.000: form yok, kilit yok, eski hata yok.
- /islemler 220.000 (16 ölçüm): platinde ALDIM tutarı tam ("220.000 ₺", kırpma sağ 0), "Kart" tek satır, K3c=0, K10c=0, fark satırı kilitliyken "Fark yok.", eşik tekrarı 1.
- /banka dekont 250.000 / 25.000 (16 ölçüm, platin): hepsi 0. Form açılıp kapanıyor, ham metin yok.
- **Bugünkü toplam Chrome'da:** "Ali Veli" aynı gün 250.000 kartla tahsil edilmişti. 25.000 girilince form açıldı ve başlık "Kimlik ve işlem amacı gerekli — bu müşterinin bugünkü toplamı 275.000 ₺" oldu. Toplamı olmayan "Ayşe Yılmaz"da 25.000 → form yok.

**Gerekçeli sayaçlar (bu paketin değil):**
- /cariler K9 3–6: çekmece, perdenin altındaki sayfanın üstüne açılıyor (sayfa ↔ çekmece örtüşmesi). Çekmece içi örtüşme 0.
- **Yıldız 375:** `#cekmece-tahsilat-baslik "Tahsilat — Zeynep Kaya"` iki satır (K3 kırık 1, form açık da kapalı da aynı). /islemler'de YildizTezgah örtüşmeleri var: "220.000" ⟂ "FARK", "ÖDEME" ⟂ "0 ₺" (K9 4). Bunlar Bilesenler/Yildiz ve tema-yildiz.css alanı (YILDIZ-4C-2 / YILDIZ-TIPO). Bu paket o dosyalara dokunmadı.

Rota × genişlik × mod ölçüm tablosu:

| Bileşim (şema-mod-tema-genişlik) | Rota | Adım | K1 | K3 taşan/kırık | K3c | K10c | K5 | K11 | yatay | Durum |
|---|---|---|---|---|---|---|---|---|---|---|
| platin-BASIT-aydinlik-1280 | /cariler#tahsilat | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={Peşin-adı:1,"200.000 ₺":1} |
| platin-BASIT-aydinlik-1280 | /cariler#tahsilat | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={Peşin-adı:1} |
| platin-BASIT-aydinlik-1280 | /islemler | 220000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | kaydetKilitli=true" aldimTutar=220.000 odeme=["Kart:1","Kart:1"] fark="Fark yok. / kaydetKilitli=true" eşikTekrar={"200.000 ₺":1} |
| platin-BASIT-aydinlik-375 | /cariler#tahsilat | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={Peşin-adı:1,"200.000 ₺":1} |
| platin-BASIT-aydinlik-375 | /cariler#tahsilat | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={Peşin-adı:1} |
| platin-BASIT-aydinlik-375 | /islemler | 220000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | kaydetKilitli=true" aldimTutar=220.000 odeme=["Kart:1","Kart:1"] fark="Fark yok. / kaydetKilitli=true" eşikTekrar={"200.000 ₺":1} |
| platin-BASIT-karanlik-1280 | /cariler#tahsilat | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={Peşin-adı:1,"200.000 ₺":1} |
| platin-BASIT-karanlik-1280 | /cariler#tahsilat | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={Peşin-adı:1} |
| platin-BASIT-karanlik-1280 | /islemler | 220000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | kaydetKilitli=true" aldimTutar=220.000 odeme=["Kart:1","Kart:1"] fark="Fark yok. / kaydetKilitli=true" eşikTekrar={"200.000 ₺":1} |
| platin-BASIT-karanlik-375 | /cariler#tahsilat | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={Peşin-adı:1,"200.000 ₺":1} |
| platin-BASIT-karanlik-375 | /cariler#tahsilat | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={Peşin-adı:1} |
| platin-BASIT-karanlik-375 | /islemler | 220000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | kaydetKilitli=true" aldimTutar=220.000 odeme=["Kart:1","Kart:1"] fark="Fark yok. / kaydetKilitli=true" eşikTekrar={"200.000 ₺":1} |
| platin-PROFESYONEL-aydinlik-1280 | /cariler#tahsilat | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={Peşin-adı:1,"200.000 ₺":1,"370.000 ₺":1} |
| platin-PROFESYONEL-aydinlik-1280 | /cariler#tahsilat | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={Peşin-adı:1} |
| platin-PROFESYONEL-aydinlik-1280 | /islemler | 220000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | kaydetKilitli=true" aldimTutar=undefined odeme=[] fark="- / kaydetKilitli=true" eşikTekrar={"200.000 ₺":3,"370.000 ₺":2} |
| platin-PROFESYONEL-aydinlik-375 | /cariler#tahsilat | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={Peşin-adı:1,"200.000 ₺":1,"370.000 ₺":1} |
| platin-PROFESYONEL-aydinlik-375 | /cariler#tahsilat | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={Peşin-adı:1} |
| platin-PROFESYONEL-aydinlik-375 | /islemler | 220000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | kaydetKilitli=true" aldimTutar=undefined odeme=[] fark="- / kaydetKilitli=true" eşikTekrar={"200.000 ₺":3,"370.000 ₺":2} |
| platin-PROFESYONEL-karanlik-1280 | /cariler#tahsilat | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={Peşin-adı:1,"200.000 ₺":1,"370.000 ₺":1} |
| platin-PROFESYONEL-karanlik-1280 | /cariler#tahsilat | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={Peşin-adı:1} |
| platin-PROFESYONEL-karanlik-1280 | /islemler | 220000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | kaydetKilitli=true" aldimTutar=undefined odeme=[] fark="- / kaydetKilitli=true" eşikTekrar={"200.000 ₺":3,"370.000 ₺":2} |
| platin-PROFESYONEL-karanlik-375 | /cariler#tahsilat | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={Peşin-adı:1,"200.000 ₺":1,"370.000 ₺":1} |
| platin-PROFESYONEL-karanlik-375 | /cariler#tahsilat | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={Peşin-adı:1} |
| platin-PROFESYONEL-karanlik-375 | /islemler | 220000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | kaydetKilitli=true" aldimTutar=undefined odeme=[] fark="- / kaydetKilitli=true" eşikTekrar={"200.000 ₺":3,"370.000 ₺":2} |
| yildiz-BASIT-aydinlik-1280 | /cariler#tahsilat | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={"200.000 ₺":1} |
| yildiz-BASIT-aydinlik-1280 | /cariler#tahsilat | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={} |
| yildiz-BASIT-aydinlik-1280 | /islemler | 220000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | kaydetKilitli=-" aldimTutar=undefined odeme=[] fark="- / kaydetKilitli=-" eşikTekrar={"200.000 ₺":1,Peşin-adı:1} |
| yildiz-BASIT-aydinlik-375 | /cariler#tahsilat | 250000 | 0 | 0/1 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={"200.000 ₺":1} |
| yildiz-BASIT-aydinlik-375 | /cariler#tahsilat | 25000 | 0 | 0/1 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={} |
| yildiz-BASIT-aydinlik-375 | /islemler | 220000 | 0 | 0/1 | 0 | 0 | 0 | 0 | 0 | kaydetKilitli=-" aldimTutar=undefined odeme=[] fark="- / kaydetKilitli=-" eşikTekrar={"200.000 ₺":1,Peşin-adı:1} |
| yildiz-BASIT-karanlik-1280 | /cariler#tahsilat | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={"200.000 ₺":1} |
| yildiz-BASIT-karanlik-1280 | /cariler#tahsilat | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={} |
| yildiz-BASIT-karanlik-1280 | /islemler | 220000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | kaydetKilitli=-" aldimTutar=undefined odeme=[] fark="- / kaydetKilitli=-" eşikTekrar={"200.000 ₺":1,Peşin-adı:1} |
| yildiz-BASIT-karanlik-375 | /cariler#tahsilat | 250000 | 0 | 0/1 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={"200.000 ₺":1} |
| yildiz-BASIT-karanlik-375 | /cariler#tahsilat | 25000 | 0 | 0/1 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={} |
| yildiz-BASIT-karanlik-375 | /islemler | 220000 | 0 | 0/1 | 0 | 0 | 0 | 0 | 0 | kaydetKilitli=-" aldimTutar=undefined odeme=[] fark="- / kaydetKilitli=-" eşikTekrar={"200.000 ₺":1,Peşin-adı:1} |
| yildiz-PROFESYONEL-aydinlik-1280 | /cariler#tahsilat | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={"200.000 ₺":1,"370.000 ₺":1} |
| yildiz-PROFESYONEL-aydinlik-1280 | /cariler#tahsilat | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={} |
| yildiz-PROFESYONEL-aydinlik-1280 | /islemler | 220000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | kaydetKilitli=-" aldimTutar=undefined odeme=[] fark="- / kaydetKilitli=-" eşikTekrar={"200.000 ₺":1,Peşin-adı:1} |
| yildiz-PROFESYONEL-aydinlik-375 | /cariler#tahsilat | 250000 | 0 | 0/1 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={"200.000 ₺":1,"370.000 ₺":1} |
| yildiz-PROFESYONEL-aydinlik-375 | /cariler#tahsilat | 25000 | 0 | 0/1 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={} |
| yildiz-PROFESYONEL-aydinlik-375 | /islemler | 220000 | 0 | 0/1 | 0 | 0 | 0 | 0 | 0 | kaydetKilitli=-" aldimTutar=undefined odeme=[] fark="- / kaydetKilitli=-" eşikTekrar={"200.000 ₺":1,Peşin-adı:1} |
| yildiz-PROFESYONEL-karanlik-1280 | /cariler#tahsilat | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={"200.000 ₺":1,"370.000 ₺":1} |
| yildiz-PROFESYONEL-karanlik-1280 | /cariler#tahsilat | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={} |
| yildiz-PROFESYONEL-karanlik-1280 | /islemler | 220000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | kaydetKilitli=-" aldimTutar=undefined odeme=[] fark="- / kaydetKilitli=-" eşikTekrar={"200.000 ₺":1,Peşin-adı:1} |
| yildiz-PROFESYONEL-karanlik-375 | /cariler#tahsilat | 250000 | 0 | 0/1 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={"200.000 ₺":1,"370.000 ₺":1} |
| yildiz-PROFESYONEL-karanlik-375 | /cariler#tahsilat | 25000 | 0 | 0/1 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false bosHata=0 ham=false cekmeceIciBinen=0 eşikTekrar={} |
| yildiz-PROFESYONEL-karanlik-375 | /islemler | 220000 | 0 | 0/1 | 0 | 0 | 0 | 0 | 0 | kaydetKilitli=-" aldimTutar=undefined odeme=[] fark="- / kaydetKilitli=-" eşikTekrar={"200.000 ₺":1,Peşin-adı:1} |
| platin-BASIT-aydinlik-1280 | /banka#dekont | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true ham=false eşikTekrar={"200.000 ₺":1} |
| platin-BASIT-aydinlik-1280 | /banka#dekont | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false ham=false eşikTekrar={} |
| platin-BASIT-aydinlik-375 | /banka#dekont | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true ham=false eşikTekrar={"200.000 ₺":1} |
| platin-BASIT-aydinlik-375 | /banka#dekont | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false ham=false eşikTekrar={} |
| platin-BASIT-karanlik-1280 | /banka#dekont | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true ham=false eşikTekrar={"200.000 ₺":1} |
| platin-BASIT-karanlik-1280 | /banka#dekont | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false ham=false eşikTekrar={} |
| platin-BASIT-karanlik-375 | /banka#dekont | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true ham=false eşikTekrar={"200.000 ₺":1} |
| platin-BASIT-karanlik-375 | /banka#dekont | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false ham=false eşikTekrar={} |
| platin-PROFESYONEL-aydinlik-1280 | /banka#dekont | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true ham=false eşikTekrar={"200.000 ₺":1,"370.000 ₺":1} |
| platin-PROFESYONEL-aydinlik-1280 | /banka#dekont | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false ham=false eşikTekrar={} |
| platin-PROFESYONEL-aydinlik-375 | /banka#dekont | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true ham=false eşikTekrar={"200.000 ₺":1,"370.000 ₺":1} |
| platin-PROFESYONEL-aydinlik-375 | /banka#dekont | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false ham=false eşikTekrar={} |
| platin-PROFESYONEL-karanlik-1280 | /banka#dekont | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true ham=false eşikTekrar={"200.000 ₺":1,"370.000 ₺":1} |
| platin-PROFESYONEL-karanlik-1280 | /banka#dekont | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false ham=false eşikTekrar={} |
| platin-PROFESYONEL-karanlik-375 | /banka#dekont | 250000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=true kaydetKilitli=true ham=false eşikTekrar={"200.000 ₺":1,"370.000 ₺":1} |
| platin-PROFESYONEL-karanlik-375 | /banka#dekont | 25000 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | form=false kaydetKilitli=false ham=false eşikTekrar={} |

**Ekran görüntüleri** `/private/tmp/kuyumcu-serit/pYASAL-KAPI-2/foto/` altında, 49 dosya: her bileşim için `-cariler-250000`, `-cariler-25000` ve `-islemler`, ayrıca `platin-BASIT-aydinlik-1280-aldim-yakin.png`. Read ile gözle incelenen 4 görüntü:
- `platin-BASIT-aydinlik-1280-cariler-250000.png`: çekmecede ortak form, başlıkta tek eşik, altlıkta tek kilit sebebi.
- `yildiz-PROFESYONEL-karanlik-375-cariler-250000.png`: Yıldız koyu 375'te form ve kural satırı okunuyor, taşma yok.
- `platin-BASIT-karanlik-375-islemler.png`: "Kart | Merkez Şube Ana Nakit Kasa (2 satır) | 220.000 ₺".
- `platin-BASIT-aydinlik-1280-aldim-yakin.png`: tutar tam.

## 8. Canlı doğrulama (API 5164 + SQL, kuyumcu_net)

Kiracı `PAKETYASAL-KAPI-2-TEST` (01M4JP2XQ4JR452FFAF91S0YYT). Betikler `pYASAL-KAPI-2/canli/canli.py` ve `banka.py`, sonuçlar `canli-tahsilat.json`, `canli-sonuc.json` ve `canli-banka.json`. Her eşik denemesi yeni bir müşteriyle yapıldı. Yanıtlar okundu.

| Form | 199.999,99 masaksız | 200.000 masaksız / beyanlı | 250.000 masaksız / beyanlı | 370.000 masaksız / beyanlı | aynı müşteri 250.000 → 25.000 → 250.000 |
|---|---|---|---|---|---|
| Cari tahsilat (KART) | 200, SQL beyan 0 | 400 MASAK / 200, beyan 1 | 400 / 200, beyan 1 | 400 ("370.000 … kimlik") / 200, beyan 1 | 400 → 200 (beyan 0) → 400 (bugünkü toplam 275.000) |
| Veresiye | 200, beyan 0 | 400 / 200, beyan 1 | 400 / 200, beyan 1 | 400 / 200, beyan 1 | 400 → 200 (beyan 0) → 400 |
| Döviz veresiye (USD 49,29) | 4.057,62 USD = 200.000,09 TL → 400 (eşik üstü; betiğin kur yuvarlaması), beyanla 200, beyan 1 | 400 / 200, beyan 1 | 400 / 200, beyan 1 | 400 / 200, beyan 1 | 400 → 200 (beyan 0) → 400 |
| Döviz tahsilat (efektif) | 400 **tevsik** (30.000 TL nakit sınırı) | 400 tevsik / 400 tevsik | aynı | aynı | 400 tevsik → 200 (24.999,89 TL) → 400 tevsik |
| Banka dekontu (müşterili) | 200, beyan 0 | 400 MASAK / 200, beyan 1 (AMAC_BEYANI) | 400 / 200, beyan 1 | 400 / 200, beyan 1 | — |

**Not (döviz tahsilat):** efektif tahsilat sunucuda nakit sayılıyor, bu yüzden 30.000 TL karşılığını aşan her tutarda tevsik kapısı MASAK'tan önce reddediyor. MASAK formu bu formda sunucuya ancak tevsik sınırının altında ulaşabilir; o tutarda da kimlik gerekmiyor. Ekran bugünkü haliyle MASAK formunu açıyor ve tevsik reddini sunucu metniyle gösteriyor. Konu §11'de soru olarak duruyor.

**UI üzerinden SQL:** Chrome'da Tahsilat çekmecesinde "Ali Veli" 250.000 KART kimlik ve amaç doldurularak kaydedildi. SQL: `TAHSILAT -250000.00 KART beyan=1 kademe=AMAC_BEYANI`.

**Temizlik:**
- API 5164 (kendi PID'im ve çocuğu), vekil 5165 ve Chrome 9610 durduruldu; `lsof` ile üç portta dinleyen yok.
- Kiracı `zsh kiraci-temizle.sh 01M4JP2XQ4JR452FFAF91S0YYT --uygula` ile silindi. Tenant_id kolonu olan tüm tablolarda bu kiracıya ait satır 0 (information_schema taraması). Kendi `kiraci_davetleri` satırım da silindi.
- `tenants` sayısı 22. Başlangıçta da 22'ydi; benim kiracım silindi, bu sırada başka şeridin açtığı `PAKETEKSIK-B1-SATINALMA-TEST dg` eklendi. Bu paketin net etkisi 0.

## 9. DoD sözleşmesi
- Yeni varlık ya da kayıt türü YOK. `masak_beyanlari` kaydı mevcut sunucu akışıyla (MasakKapisi FAZ 2) doğar. Düzeltme ve maskeleme yolları değişmedi.
- Finansal satırlara hard DELETE eklenmedi.
- Yeni düğme YOK. Var olan Kaydet düğmeleri ya istek atar ya da disabled olur ve sebebini yazar: `#th-kaydet-kilit`, `#vr-kaydet-kilit` (yeni), `#cekmece-doviz-*-kilit`, `#siparis-cevir-kilit`, `#dekont-kaydet-sebep`.
- Yeni ModalPencere düzenlemesi YOK. Veresiye var olan penceresinde kaldı.
- Boş formda dokunulmadan zorunluluk hatası çıkmıyor (Chrome: bosHata=0 ×32; bUnit YK2_01).

## 10. Dokunulan sayfalar (TEK DOKUNUŞ kaydı)
- Bu dalgada açıldı: `Cariler.razor` (+ yeni `Cariler.YasalKapi2.cs`), `Banka.razor`, `Evraklar.razor`, `Islemler.Diger.cs`.
- **İkinci dokunuş, gerekçeli:** `Islemler.razor` ve satış partial'ı (yeni `Islemler.YasalKapi2.cs`). YASAL-KAPI-GERI'de açılmıştı. Gerekçe: bu paketin GÖRÜNÜM maddeleri ("Kaydet kilitliyken 'Fark yok, kaydedebilirsiniz' yazmaz", "uyarı tek kez") aynı ekranın kusuru. Dokunuş en küçük tutuldu: satır 76'ya tek öznitelik (`KaydetKilitli`) ve satır 658'de tek ifade (`MasakEksikMetniTekYer`). Satır sayısı değişmedi.
- **Değiştirilmedi:** `Cariler.Yildiz.cs`, Bilesenler/Yildiz/*, tema-yildiz.css (YILDIZ-4C-2 / YILDIZ-TIPO), SiparisDetay.razor (KAPORA-EKRAN), SatınAlma/İrsaliye. `Hurda.razor` mevcut iz deseniyle zaten uyumluydu.

## 11. Kurucu / mali müşavir / Mehmet (güvenlik) notları
**Bu pakette yeni mevzuat hükmü yazılmadı.** Eşikler ve kademeler sunucudan (`/masak/esikler`, `/masak/gereksinim`, yasal parametre tablosu) okunuyor; istemciye sayı gömülmedi (B-3 ✓). Aşağıdaki sorular birincil kaynakla karara bağlanana kadar kod değiştirilmedi; bugünkü sunucu davranışı korunuyor.

1. **Avans (kapora) al / iade ve Değişim:** `/islemler/avans` ve `/islemler/degisim` uçlarında MASAK kapısı YOK, yalnız tevsik var. Aynı para sipariş kaporası yolundan (SiparisDetay → TahsilatGirHareket) geldiğinde MASAK uygulanıyor. Bu yüzden 200.000 ₺ üstü nakit avans kimliksiz alınabilir ve bu bir eşik bölme yolu olabilir. Sorular:
   - Bu işlemler Tedbirler Yönetmeliği md.5/1-b kapsamında mı?
   - Kapsamdaysa ölçü ne olmalı?
   Karar sunucu tarafında (Taşkın + Mehmet) birincil kaynakla verilmeli.
2. **Döviz efektif tahsilat:** tevsik nakit sınırı MASAK'tan önce reddediyor (§8). Efektif dövizin 459 s. VUK GT tevsik kapsamında sayılması bugünkü sunucu davranışı. Mali müşavir teyidi gerekiyor.
3. **Kademe kaydı:** 370.000 ₺ dekontta `masak_beyanlari.kademe = AMAC_BEYANI` yazıldı (sunucu mesajı "370.000 … kimlik"). Sunucu davranışı, bu pakette değişmedi. Kayıtta 370.000 kademesinin ayrıca görünmesi gerekiyorsa bu bir mevzuat sorusu.

## 12. Paket kapısı (paralel şerit kuralı)
1. Build 0 hata ✓. Son temiz derleme 0 uyarı; tam yeniden derlemede başkasının 4 test dosyasında 6 uyarı görüldü, §13.
2. Bu paketin testleri 23/23 ✓.
3. Bu paketin sayaçları ✓ (§6).
4. Mutasyon 11/11 kırmızı, cmp AYNI ✓.
5. Canlı API + SQL ve Chrome doğrulaması ✓.
6. Test kiracısı silindi ✓.
7. Tam süitte bu paketten yeni kırmızı 0 ✓.

## 13. Açık kalanlar (sahipleriyle)
- **Tur 2 ekleri:**
  - **Ayrı sunucu paketi (ORTA):** avans al/iade ve değişim farkına MASAK kümülatif kapısı (§0.2). Önce birincil kaynak + mali müşavir teyidi.
  - **ISLEM-BANT(-2):** `S24EskiAltinEkranTestleri` ×2 bant onay kilidi yüzünden kırmızı (tek başına da). Q102/ZOS11/ZOSR18/Y4A yük altında zaman aşımı, tek başına yeşil.
  - **Ölçer notu:** `tekrar.js`/`esikid.js` blok başına ilk eşiği sayıyor. İki kademeyi tek cümlede söyleyen `#masak-kural` (panel olmayan PRO ekranları: Cariler, Banka, Hurda, Kapora) 200.000'i ikinci kez taşır. Bunun kaldırılması ortak MasakFormu'nun bütün PRO ekranlarını etkiler; ayrı tasarım kararı gerekir (tasarım hakemi, aşağıdaki madde ile aynı).
- **RAPOR-DORT:** `RD_KH01`, `RD_KH02` (400 gün sınırı, tarih bağımlı).
- **MASAK-ESZAMAN:** denetim `GIRDI-2 TEST_GECERSIZ_FIKSTUR` (MASAKESZAMANSinirTestleri.cs:187 IBAN).
- **Bilinen:** denetim `P-30`.
- **YILDIZ-4C-2 / YILDIZ-TIPO:** Yıldız 375'te çekmece başlığı iki satır; YildizTezgah örtüşmeleri ("220.000" ⟂ "FARK", "ÖDEME" ⟂ "0 ₺").
- **Tam yeniden derlemede uyarılar (başka şeritler):**
  - `Dogrulayici/SUBEKAPSAMASinirTestleri.cs` CS8601
  - `KesifP2Testleri.cs` xUnit2029
  - `SonKalanlarTestleri.cs` xUnit2013
  - `Yildiz2Testleri.cs` xUnit1031
- **Tasarım hakemine:** PRO MasakFormu kural satırı eşiği başlıkla birlikte ikinci kez söylüyor (ortak bileşen). BASİT satış üst bandı (SATIS-3 t24) korunuyor. Değişiklik istenirse ayrı karar gerekir.

## 14. Değişen dosyalar
`git status --porcelain | grep -v -E "/(bin|obj)/"` süzgecinden bu paketin dosyaları (çoğu dizin depoda izlenmediği için `??` görünüyor):
- dotnet/Kuyumcu.Core/I18n/Sozlukler.YasalKapi2.cs (yeni)
- dotnet/Kuyumcu.Web/Bilesenler/ParaMasakKapisi.cs (yeni)
- dotnet/Kuyumcu.Web/Bilesenler/Fis/FisVerdimAldim.razor
- dotnet/Kuyumcu.Web/Bilesenler/Fis/FisDigerSiparis.razor
- dotnet/Kuyumcu.Web/Bilesenler/Fis/FisDigerModelleri.cs
- dotnet/Kuyumcu.Web/Pages/Cariler.razor
- dotnet/Kuyumcu.Web/Pages/Cariler.YasalKapi2.cs (yeni)
- dotnet/Kuyumcu.Web/Pages/Banka.razor
- dotnet/Kuyumcu.Web/Pages/Evraklar.razor
- dotnet/Kuyumcu.Web/Pages/Islemler.razor (2 satırda tek öznitelik / tek ifade)
- dotnet/Kuyumcu.Web/Pages/Islemler.Diger.cs
- dotnet/Kuyumcu.Web/Pages/Islemler.YasalKapi2.cs (yeni)
- dotnet/Kuyumcu.Web/wwwroot/css/yasal.css
- dotnet/Kuyumcu.Tests/YasalKapi2Testleri.cs (yeni)
- dotnet/Kuyumcu.Tests/SATIS3T24YasalMesajTestleri.cs (T24 BASİT beklentisi)
- dokumanlar/09-uygulama-kayitlari/betikler/sikisma-olc.js (K3c, K10c)

COMMIT YOK. Proje ağacına geçici dosya bırakılmadı; geçici dosyalar `/private/tmp/kuyumcu-serit/pYASAL-KAPI-2/` altında.
