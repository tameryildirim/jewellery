# PAKET YASAL-PARAM-2 — Uygulama raporu

**Rol:** Taşkın (backend) + Ertuğ (frontend) · **Tarih:** 08.10.2026 13:30–15:20 · **Şerit:** t5 (test DB `kuyumcu_test_t5`, API 5166, vekil 5167, CDP 9512–9514)
**Kaynak:** `ham/TOPLU-TASARIM-KUYRUGU-2026-09-29.md` § "08.10 07:10 — YASAL-PARAM kalanları" (3 madde). Önceki paket: `PAKET-YASAL-PARAM-EKRAN-uygulama-raporu.md`.
**Durum:** BİTTİ (paket kapısının 7 maddesi sağlandı; tam süitteki 7 kırmızı başka şeritlere ait — § 11).

## 1. Spesifikasyon ↔ kod (mevcut durum doğrulaması)

| Kuyruk maddesi | Koddaki gerçek durum (doğrulandı) |
|---|---|
| ORTA · aynı gün eşik değişince o sabahki işlem yeni eşikle değerlendiriliyor | DOĞRU. `RiskServisi.KimlikEsigiDonemleri` → `YasalParametreServisi.Donemler` (gün çözünürlüğü, `Zaman.YerelGunBasiUtc`). Bugünden geçerli satırın başlangıcı yalnız `DateOnly`; kayıt anı hiçbir yerde dönem sınırı değildi. Onay metni ("bugün daha önce tamamlanmış işlemler yeniden değerlendirilmez") risk raporu için yanlıştı. |
| DÜŞÜK · bugünden geçerli yanlış giriş aynı gün düzeltilemiyor (409) | DOĞRU. `Dogrula`: `ayni.Any(p => p.GecerlilikBaslangic >= bas)` → 409; ayrıca DB'de `UNIQUE (anahtar, gecerlilik_baslangic)` aynı güne ikinci satırı yasaklıyordu. Ekrandaki ipucu "yarından geçerli doğru değeri yazın" diyordu. |
| DÜŞÜK · "geçmiş tarih 400" testi maskelenmiş | DOĞRU. `YasalParamHttpTestleri.RG_tarihi_yok_400_…_gecmis_400_…` geçmiş durumunu RG tarihi = BUGÜN ile sınıyordu; "RG tarihi > başlangıç" kuralı da `alan: baslangic` ile 400 verdiği için geçmiş kuralı silinse test yeşil kalıyordu. |

Önceki paketin orkestratör düzeltmesi (`Dogrula`: `teyit == KAPALI → 400`) **korundu** (satıra dokunulmadı). Mevzuat eşik değerleri **değişmedi** (MASAK kimlik 370.000 07.10.2026'dan, öncesi 185.000); yalnız zaman çözünürlüğü ve düzeltme yolu eklendi → yeni mevzuat hükmü yazılmadı, birincil kaynak araştırması gerekmedi.

## 2. Yapılanlar

### 2.1 An sınırlı dönem (madde 1 — tercih edilen yol: AN sınırı)
- `yasal_parametreler.gecerlilik_baslangic_ani` (DATETIME(6), NULL) — yalnız **bugünden geçerli** yazılan satırda kayıt anı (UTC). NULL = başlangıç gününün yerel 00:00'ı → tohum ve ileri tarihli satırların davranışı **birebir aynı**.
- `YasalParametreSatiri.BaslangicAni` + `EtkinBaslangicUtc`; `YasalParametreServisi.DonemlerAn(anahtar, basUtc, bitUtc)`: satırları etkin başlangıç anına göre sıralar; bir satırın bitişi, kapanış günü sonraki satırın başlangıç günüyse **o satırın anıdır** (aynı gün değişiklik), değilse kapanış gününün 00:00'ı. Aralıkta değer olmayan an → **503 fail-closed** (eski davranış korunur).
- `RiskServisi.KimlikEsigiDonemleri` artık `DonemlerAn(…, yasal.SimdiUtc)` kullanır; işlem anı `Hareket.Tarih`. Sonuç: eşik öğlen değiştiyse o sabahki işlem **eski eşikle** değerlendirilir; onay penceresinin sözü artık doğru.
- Kapı (satış/MASAK işlem anı) gün düzeyinde zaten "o an geçerli satır"ı okuyordu — değişmedi.
- `Ekle` denetim izine yürürlük saati eklendi: "(BUGÜNDEN GEÇERLİ, 14:18:55 itibarıyla)".

### 2.2 Aynı gün düzeltme yolu (madde 2)
- **Uçlar** (`SistemController`, sistem anahtarı kapısı — JWT ile erişilemez):
  - `POST /api/v1/sistem/yasal-parametreler/{id}/duzeltme/onizleme` — HİÇBİR ŞEY YAZMAZ; yanlış → doğru, yanlışın yürürlük saati, onay kodu.
  - `POST /api/v1/sistem/yasal-parametreler/{id}/duzeltme` — 201.
- **Kurallar** (`DuzeltDogrula`, sıra: 404 → 409 → 403 → 400): satır yoksa 404; satır **bugün, bugünden geçerli (anlı) yazılmış ve hâlâ yürürlükte** değilse 409 (tohum, ileri tarihli, kapanmış satır düzeltilmez); **yalnız aynı operatör** (`olusturan == "sistem:"+X-Sistem-Operator`) → değilse **403** "Bu değeri başka bir yönetici girdi; aynı gün yalnız giren kişi düzeltebilir."; gerekçe ≥ 20 → 400 `gerekce`; değer ≥ 0 → 400 `degerSayi`; yanlış değerle aynı → 409; akla yatkınlık bugünden ÖNCEKİ değere göre (10 kat bandı) → 400; onay kodu yok → 400 `onayKodu`, uyuşmaz → 409.
- **Geçmişe etki yok / append-only:** yanlış satır silinmez, değeri değişmez; yalnız `gecerlilik_bitis` bir kez (bugün) dolar. Doğru değer YENİ satırdır: aynı başlangıç günü + kendi anı (düzeltme anı). `DonemlerAn` yanlış değeri yalnız [yazıldığı an, düzeltildiği an) aralığına uygular → o aralıktaki işlemler yanlış değerle (kapıda gerçekten uygulanan değerle) kayıtlı kalır.
- **Denetim izi** (`DenetimServisi.SistemYazmaKaydet`, yeniden yazılmadı): "yasal parametre AYNI GÜN DÜZELTİLDİ · anahtar · yanlış X (id) → doğru Y · yanlış değer HH:mm:ss–HH:mm:ss arasında geçerliydi, bu aralıktaki işlemler değişmez · operatör · gerekçe".
- Aynı gün ikinci "yeni değer" girişi 409 mesajı artık yolu söyler: "Bu sınıra bugün zaten bir değer yazıldı; yanlışsa "Bugün girilen değeri düzelt" bölümünden düzeltin."
- **Liste ucu** (`GET /yasal-parametreler`): satıra `baslangicSaati` (yerel HH:mm) ve `ayniGunDuzeltilebilir` eklendi (sözleşme `YasalSatirVeri`, varsayılanlı — eski istemci kırılmaz).
- **DB tekilliği:** `UNIQUE (anahtar, gecerlilik_baslangic)` → `UNIQUE (anahtar, gecerlilik_baslangic, gecerlilik_baslangic_ani)` + işlevsel `ux_yasal_param_gun_tekil (anahtar, gecerlilik_baslangic, CASE WHEN ani IS NULL THEN 1 END)` → anısız satırlarda (tohum/ileri tarihli) "gün başına tek satır" güvencesi **gevşemedi**; açık uçlu iki satır yasağı (`ux_yasal_param_acik_uc`) aynen.

### 2.3 Ekran (`Bilesenler/YasalParamFormu.razor` — yalnız `/yasal-sinirlar?yonetim=1`, ByGatech yönetimi)
- Formun sonunda, **yalnız düzeltilebilir satır varsa**, KAPALI katlanır "Bugün girilen değeri düzelt" (zorunlu önce kuralı). İçerik: açıklama; tek satır varsa seçici YOK, salt okunur satır "Sınır: değer (bugün 14:18 itibarıyla geçerli)" (UX-S22), birden çoksa seçici; Doğru değer · Adınız (ana formdaki ad önceden dolar) · Düzeltme gerekçesi · Sistem anahtarı (kaydedilmez, istekten sonra boşaltılır); "Düzeltmeyi önizle" (ikincil düğme — ekranda tek birincil korunur).
- Boş/geçersiz gönderim: alan altı Türkçe hata, istek atılmaz (aynı değer de istemcide yakalanır: "Doğru değer, düzeltilecek değerle aynı.").
- Onay: ortak `ModalPencere` (geri dönüşsüz işlem onayı, UX-S27) "Düzeltmeyi onayla": "yanlış 175.000 ₺ → doğru 180.000 ₺" + uyarı bandı "Yanlış değer bugün 14:18:56 itibarıyla geçerliydi; o saatten bu ana kadarki işlemler değişmez. Doğru değer onayladığınız andan geçerli olur." · Vazgeç / Onayla ve düzelt. Sunucu hatası (403 vb.) pencerede hata bandına yazılır (sessiz değil).
- Başarı bandı sunucunun döndürdüğünü yazar: "Düzeltildi: … 150.000 ₺ → 175.000 ₺, bugün 14:18:56 itibarıyla geçerli. 14:18:55–14:18:56 arasındaki işlemler değişmedi."; liste yeniden okunur.
- Kayıt onay penceresindeki `DegismezIpucu` metni düzeltildi: "Bugün başlayan değer kaydedildiği andan geçerlidir; bugün daha önceki işlemler önceki değerle kalır." / "Yanlış girdiyseniz bugün "Bugün girilen değeri düzelt" bölümünden gerekçeyle düzeltin (yalnız giren kişi)." (eskisi "yarından geçerli doğru değeri yazın" — artık yanlış yol).
- Sözlük: yeni `Kuyumcu.Core/I18n/Sozlukler.YasalParam2.cs` (`YasalDuzeltMetinleri`, TR+EN, konumsal record → eksik anahtar derleme hatası); `Sozlukler.YasalParam.cs`'te yalnız 2 metin (TR+EN) değişti. `Sozlukler.cs` HUB'a dokunulmadı.
- Yeni id'ler `ysy-dz-*`; mevcut id'ler değişmedi. Yeni CSS yok (mevcut `form-dikey`/`yasal-katlanir`/`bant` sınıfları), satır içi stil yok.

### 2.4 Maskelenmiş test (madde 3)
`YasalParamHttpTestleri.RG_tarihi_yok_400_RG_sayisi_yok_400_RG_tarihi_ileri_400_gecmis_400_gerekce_kisa_400_alanli`: geçmiş durumu ortak döngüden çıkarıldı; ayrı blokta RG tarihi = bugün−2, başlangıç = bugün−1 (yalnız geçmiş kuralı tetiklenir), hem önizleme hem kayıt yolunda `alan: baslangic` **ve** mesaj `StartsWith("Geçmiş tarihli yasal değer yazılamaz")` sınanıyor. Kanıt: M3-1 mutasyonu (geçmiş kuralı `if (false)`) bu testi artık KIRMIZI yapıyor (§ 6).

### 2.5 Sapmalar ve gerekçeleri
1. **Şema değişikliği gerekti** (kapsam "an sınırı" + "aynı gün düzeltme"): gün çözünürlüklü tek sütun ve `UNIQUE (anahtar, gün)` ile ikisi de yapılamaz. Seçenekler: (a) `olusturuldu`dan türetme — tohum satırlarının migration anına bağlanma riski, reddedildi; (b) yanlış satırı yerinde değiştirmek — append-only ilkesi ve "geçmiş işlemlere etki yok" ile çelişir, reddedildi. Seçilen: açık `gecerlilik_baslangic_ani` sütunu, yalnız bugünden geçerli satırda dolu.
2. **Dosya alanı dışı dokunuşlar** (zorunlu): `Data/YasalParametre.cs`, `Data/KuyumcuDbContext.cs` (indeks), migration üçlüsü, `Modules/Sistem/SistemModulu.cs` (2 uç), `Modules/Yasal/YasalParametreModulu.cs` (liste alanları), `Kuyumcu.Contracts/Sayfalar/YasalSinirlarSozlesmeleri.cs`. `UrunModulu.cs`'e dokunulmadı.
3. **Canlı doğrulama ayrı veritabanında** (`kuyumcu_yp2_t5`, sıfırdan 90 migration): MASAK kimlik eşiği ulusal satırdır; `kuyumcu_net`'te değiştirmek kurucunun 5199/5200 sunucularını ve tüm kiracıları etkilerdi. `kuyumcu_net`'e yalnız şema migration'ı uygulandı; MASAK satırı değişmedi (`masak370-01` 370000, bitiş NULL; `sistem:%` satır 0).
4. **Canlı adım beklentisi düzeltildi:** "Düzeltmeden sonra 160.000 kimliksiz veresiye → 200" beklenmişti; sunucu **400** verdi çünkü aynı cari aynı gün **bağlantılı toplam** 350.000 ≥ 175.000 (mevcut kümülatif kural). Mesaj "eşik 175.000 TL" diyor → düzeltilmiş değerin yürürlükte olduğunun kanıtı; kural değişmedi, beklenti yanlıştı.
5. Ölçüm ilk turunda PRO 1280'de "Önizle" düğmesi ile düzeltme katlanırı arasında boşluk 0 görüldü (görüntüde fark edildi) → katlanır formun `form-dikey` kabına alındı; yeniden ölçümde boşluk 16 px (formun diğer aralıklarıyla aynı, 1280 ve 375).

## 3. DoD sözleşmesi
| Varlık / kayıt | Düzenleme yolu | Salt okunur kısmı ve gerekçe |
|---|---|---|
| Yasal parametre (bugünden geçerli, bugün yazılmış, yürürlükte) | **YENİ:** `POST …/{id}/duzeltme/onizleme` + `POST …/{id}/duzeltme` (sistem anahtarı, yalnız giren operatör, gerekçe, onay kodu, denetim izi); ekranda "Bugün girilen değeri düzelt" | Yanlış satır silinmez/değiştirilmez (append-only, ulusal rakam); yalnız bitişi dolar. |
| Yasal parametre (ileri tarihli / tohum / dünkü) | Yeni değer girişi (`POST …/yasal-parametreler`, önceki paket) | Aynı gün düzeltme yolu yok → 409 + gerekçe mesajı; kuyumcu ekranında `DegismezIpucu` ("ByGatech günceller"). Pasife alma: ulusal rakam kaldırılamaz (KAPALI 400 — önceki paketin orkestratör kuralı korundu). |
Yeni varlık türü YOK (yalnız sütun). Kiracı-üstü tablo (IKiraciVarlik değil — gerekçeli beyaz liste, değişmedi).

## 4. Migration
`20261008104154_YasalParamAyniGunAn` (build yeşilken `dotnet ef migrations add`, `--no-build` kullanılmadı). Up: eski `IX_yasal_parametreler_anahtar_gecerlilik_baslangic` düşer · `gecerlilik_baslangic_ani datetime(6) NULL` · `UNIQUE (anahtar, gecerlilik_baslangic, gecerlilik_baslangic_ani)` · ham SQL `ux_yasal_param_gun_tekil`. Down tersini yapar. `.cs` 2,2 KB, `.Designer.cs` 259 KB (0 bayt değil).
`kuyumcu_net`: "Applying migration '20261008104154_YasalParamAyniGunAn'. Done." — `SHOW INDEX`: `ux_yasal_param_gun_tekil`, `IX_…_gecerlilik_b~` (3 sütun), `ux_yasal_param_acik_uc`, PRIMARY. Son iki migration: `20261008104154_YasalParamAyniGunAn`, `20261008090525_RaporDortOlusturulduIndeksi` (RAPOR-4 şeridi; anlık görüntü onunkini içeriyor).

## 5. Derleme ve denetim
- `dotnet build Kuyumcu.sln --nologo` (son kod): **0 Hata · 4 Uyarı** — dördü de başka şeritlerin test dosyalarında (`SUBEKAPSAMASinirTestleri.cs:49` CS8601, `KesifP2Testleri.cs:455` xUnit2029, `SonKalanlarTestleri.cs:587/818` xUnit2013); bu paketin dosyalarında uyarı 0.
- `bash dotnet/denetim.sh`: **2 ✗, ikisi de bu paketin değil** — P-30 DEMO_INSAN_SURESI (izinli istisna; kurucu kalemi) ve **P-18 MOBIL_TABLO_SARMALAYICISIZ "6 KartListe"** (sayaç ≥ 9 bekliyor; sabahki YASAL-PARAM-EKRAN denetiminde 10'du; bugün 13:55–14:21 arasında değişen `Pages/` dosyaları Banka/Kasalar/Hurda/Vezne/Gider/Faturalar = GOC-A şeridinin alanı; bu paket `Pages/` altında hiçbir dosyaya dokunmadı). Bu paketin sayaçları: `YASAL_PARAM_TARIHSIZ 0 ✓`, `YASAL_PARAM_YAZMA 0 ✓` (Remove/ExecuteUpdate/Delete yok), `YASAL_PARAM_OKUMA_DISI 0 ✓`.

## 6. Testler
**Yeni: 10 test** — `Kuyumcu.Tests/YasalParam2Testleri.cs`:
| Sınıf | Test | Tür |
|---|---|---|
| YasalParam2SafTestleri | `Ayni_gun_esik_degisimi_AN_siniri_1sn_once_ESKI_1sn_sonra_YENI_gun_basi_ESKI` (an−1 sn → 370.000; an ve an+1 sn → 150.000; gün başı → eski; iki dönem, sınır = an) | saf |
| | `Ayni_gun_duzeltme_yanlis_deger_YALNIZ_iki_an_arasinda_sonrasi_dogru_oncesi_onceki` | saf |
| | `Anisiz_tohum_satiri_GUN_BASI_siniri_korunur_bosluk_503` | saf |
| | `Ayni_gun_duzeltilebilir_yalniz_bugun_anli_ve_yururlukte_satir` | saf |
| YasalParam2RiskTestleri | `Risk_ogle_esik_dususu_SABAH_islemi_ESKI_esikle_1sn_sonrasi_YENI_esikle_degerlendirilir` (200.000 TL; an−1 sn ve an+1 sn → kimlikEksik 1; gün çözünürlüğünde 2) | DB |
| YasalParam2HttpTestleri | `Ayni_gun_duzeltme_ayni_operator_201_baska_operator_403_gerekce_400_onay_kodu_denetim_izi_gecmis_korunur` (409 ikinci yeni değer · 403 başka operatör önizleme+kayıt · JWT 401/403 · 400 gerekçe · 409 aynı değer · 400 onay kodu yok · 409 yanlış kod · 201 · kapı düzeltilmiş değeri okur · SQL: yanlış satır değeri aynı, bitiş bugün, doğru satır açık uçlu ve anı büyük · denetim izi tek satır · `DonemlerAn` iki an arası yanlış / öncesi önceki / sonrası doğru · kapanmış 409 · tohum 409 · yok 404) | HTTP |
| YasalParam2EkranTestleri | `Duzeltilebilir_satir_yoksa_bolum_YOK_varsa_KAPALI_katlanir_tek_satir_secicisiz` · `Bos_onizle_alan_alti_hatalar_istek_ATILMAZ` · `Onizle_onay_penceresi_yanlis_dogru_saat_kaydet_onay_kodu_basari_bandi` · `Baska_operator_403_onay_penceresinde_hata_bandi_sessiz_kalmaz` | bUnit |

**Değişen testler:** `YasalParamEkranTestleri.cs` — maskelenmiş geçmiş durumu yalıtıldı (§ 2.4); iki bUnit testinde ipucu beklentisi yeni (doğru) metne çevrildi (`Tur2_bugunden_gecerlide_ayni_gun_duzeltme_yolu_…`, `Tur3_onay_penceresi_kaynak_adresini_gosterir_metinler_sade`).
**Filtreli koşum (son kod):** YasalParam2 + YasalParamHttp + YasalParamEkran + YasalParametre + YasalParametreHttp + YasalSinirlarEkran + YasalParamTur3 → **81/81 Başarılı**.

## 7. Mutasyon kanıtları
Yöntem: yedek `/private/tmp/kuyumcu-serit/YASAL-PARAM-2/mut-yedek/` → `perl -0777 -pi` tek eşleşme → `dotnet build` 0 hata → filtreli test (45 test) → yedek geri + `touch` → `cmp` AYNI. Kayıtlar `mut-<kod>-*.log`, özet `mut-ozet.log`. Son mutasyondan sonra temiz derleme 0 hata.
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M1-1 | `EtkinBaslangicUtc` anı yok sayar (gün çözünürlüğüne dönüş) | Saf `Ayni_gun_esik_degisimi_AN_siniri…`, Saf `Ayni_gun_duzeltme_yanlis_deger…`, Risk `Risk_ogle_esik_dususu_SABAH…`, HTTP `Ayni_gun_duzeltme_…` (4) | AYNI |
| M1-2 | `DonemlerAn` aynı gün bitişini sonraki satırın anı yerine gün başına çeker | aynı 4 test | AYNI |
| M1-3 | `Ekle` bugünden geçerli satıra anı yazmaz | HTTP `Ayni_gun_duzeltme_…` | AYNI |
| M2-1 | Aynı operatör denetimi kapalı (`false &&`) | HTTP `Ayni_gun_duzeltme_…` (403 beklenirken geçti) | AYNI |
| M2-2 | Gerekçe asgarisi 0 | HTTP `Ayni_gun_duzeltme_…` | AYNI |
| M2-3 | Yanlış satırın bitişi doldurulmaz | HTTP `Ayni_gun_duzeltme_…` | AYNI |
| M2-4 | Denetim izi doğru satıra değil yanlış satıra yazılır | HTTP `Ayni_gun_duzeltme_…` | AYNI |
| M3-1 | Geçmiş başlangıç kuralı `if (false)` | `YasalParamHttpTestleri.RG_tarihi_yok_…_gecmis_400_…` (yalıtılan test — artık maskeli değil), `YasalParametreHttpTestleri.K5_gecmis_baslangic_400_…` | AYNI |
| M4-1 | Ekranda düzeltme gerekçe ön denetimi kapalı (`g < 0`) | bUnit `Bos_onizle_alan_alti_hatalar_istek_ATILMAZ` | AYNI |

## 8. Canlı doğrulama (API 5166, `kuyumcu_yp2_t5`, son kod; yanıtlar okundu — `canli-api.json`, `canli-sql.txt`)
Test kiracısı `PAKETYASALPARAM2-TEST` sistem davetiyle API'den kuruldu (parola rastgele, 600 izinli dosya; çıktıya basılmadı). Sistem anahtarı rastgele, yalnız süreç ortamında ve 600 izinli dosyada. (İlk koşum `/cariler` yanlış rota yüzünden risk adımlarını 404'le geçemedi; o kiracı da silindi, yasal satır geri yüklendi, API yeniden başlatıldı, betik `/cari` + veresiye ile yeniden koşuldu — aşağıdaki tablo ikinci koşumdur.)

| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /masak/esikler (önce) | 370.000 | 200 · 370000 |
| **SABAH işlemi:** veresiye 190.000 kimliksiz (14:18:55) | 200 | **200** |
| Risk (önce) | kimlikEksik 0 | 200 · toplamTl 190000 · kimlikEksik **0** |
| **ÖĞLE değişikliği** önizleme 370.000→150.000 | 200 | 200 · bugunMu true · SIKILASTIRMA |
| Kayıt (14:18:56) | 201 | **201** |
| Değişiklikten SONRA veresiye 160.000 kimliksiz | 400 | **400** "MASAK: 150.000 TL ve üstü işlemde kimlik tespiti zorunludur…" |
| **Risk (sonra): sabah işlemi ESKİ eşikle** | kimlikEksik 0 (gün çözünürlüğünde 1 olurdu) | 200 · kimlikEksik **0** |
| GET /yasal-parametreler kimlik satırı | düzeltilebilir | 150000 · baslangicSaati "14:18" · ayniGunDuzeltilebilir **true** |
| Aynı gün ikinci yeni değer | 409 | **409** "…"Bugün girilen değeri düzelt" bölümünden düzeltin." |
| Düzeltme önizleme / kayıt BAŞKA operatör | 403 | **403 / 403** "Bu değeri başka bir yönetici girdi; aynı gün yalnız giren kişi düzeltebilir." |
| Düzeltme kayıt SAHİP JWT | 403 | **403** "Sistem yönetimi anahtarı geçersiz." |
| Düzeltme önizleme gerekçe "kısa" | 400 | **400** `gerekce` |
| Düzeltme kayıt onay kodu yok | 400 | **400** `onayKodu` |
| Düzeltme önizleme (aynı operatör) | 200 | 200 · yanlış 150000 → 175000 · önceki 370000 · yanlisSaati 14:18:55 |
| Düzeltme kayıt | 201 | **201** · duzeltilenSatirId = yanlış satır · duzeltmeSaati 14:18:56 |
| Kapanmış yanlış satırı yeniden düzeltme | 409 | **409** |
| GET /masak/esikler (düzeltmeden sonra) | 175.000 | 200 · **175000** |
| Düzeltmeden sonra veresiye 160.000 kimliksiz | (beklenti 200 idi — § 2.5/4) | **400** "… eşik 175.000 TL; bugün kümülatif 350.000 TL" (bağlantılı toplam kuralı; düzeltilmiş eşik yürürlükte) |
| Risk (düzeltmeden sonra) | kimlikEksik 0 | 200 · kimlikEksik **0** |

**SQL kanıtı** (`kuyumcu_yp2_t5`):
```
id                          deger_sayi  bas         ani_utc                      bitis       olusturan
masak370-01                 370000      2026-10-07  NULL                         2026-10-08  migration:MasakKimlikEsigi370
01M4DKSQ9VV2M42WDSMVKWF04Y  150000      2026-10-08  2026-10-08 11:18:55.913967   2026-10-08  sistem:ramazan-yp2   ← yanlış (silinmedi, değeri aynı)
01M4DKSQR8NE9DDMJXWE88KX9Y  175000      2026-10-08  2026-10-08 11:18:56.505365   NULL        sistem:ramazan-yp2   ← doğru
hareketler: VERESIYE 190000.00 tarih 2026-10-08 11:18:55.179038 UTC  (< değişiklik anı 11:18:55.913967 → eski eşik)
denetim_kayitlari (varlik_turu=YasalParametre):
 01M4DKSQR8NE…  "yasal parametre AYNI GÜN DÜZELTİLDİ · MASAK_KIMLIK_ESIGI_TL · yanlış 150000 (01M4DKSQ9VV2…) → doğru 175000 · yanlış değer 14:18:55–14:18:56 arasında geçerliydi, bu aralıktaki işlemler değişmez · operatör: ramazan-yp2 · gerekçe: Resmî Gazete rakamı yanlış yazıldı; doğru değer 175.000 (test)."
 01M4DKSQ9VV2…  "yasal parametre eklendi · … eski 370000 → yeni 150000 · başlangıç 2026-10-08 (BUGÜNDEN GEÇERLİ, 14:18:55 itibarıyla) · yön SIKILASTIRMA · … operatör: ramazan-yp2 · kaynak RG 2026-10-08/99999 · önceki satır masak370-01 … kapandı · gerekçe: …"
```
**Temizlik:** API (PID 35759) ve vekil durduruldu; `lsof` 5166/5167 boş. İki test kiracısı `kiraci-temizle.sh`'nin `kuyumcu_yp2_t5` kopyasıyla silindi (57 + 108 satır; kalan kiracı **0** = başlangıç 0). Ayrı DB'deki MASAK satırı geri yüklendi (`sistem:%` satırlar silindi, `masak370-01` bitiş NULL). `kuyumcu_net`: bu pakette kiracı açılmadı; MASAK satırı değişmedi. (`kuyumcu_net` tenants başta 19, sonda 16 — fark başka şeritlerin kendi test kiracılarını silmesi; bu paket `kuyumcu_net`'e satır yazmadı. `QA-TEST-` kiracılarına dokunulmadı.)

## 9. Gerçek tarayıcı ölçümü (başsız Chrome, `cdp.mjs`, vekil 5167, CDP 9512; `sikisma.js` + `ekstra.js`; `olc/olcum.jsonl`, görüntüler `olc/*.png`)
Rota `/yasal-sinirlar?yonetim=1` (ByGatech yönetim kiracısı = test kiracısı, `Sistem__YonetimKiracilari__0`). İki durum: **bölüm** (katlanır açık, alanlar dolu) ve **onay** (Önizle → onay penceresi; KAYDEDİLMEDİ). Son kodla (boşluk düzeltmesinden sonra) yeniden ölçüldü:

| durum | mod | tema | w | gizli-görünen K1 | taşan K3 | kırık K3 | TRY K5 | binen K9 | modal dışı binen | yatay | ezik K12 | kırp K13 | kesik K10b | kart% K2 | onay açıldı |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| bölüm | BASİT | açık | 1280 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 98 | — |
| onay | BASİT | açık | 1280 | 0 | 0 | 0 | 0 | 6* | 0 | 0 | 0 | 0 | 0 | 98 | evet |
| bölüm | BASİT | açık | 375 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 97 | — |
| onay | BASİT | açık | 375 | 0 | 0 | 0 | 0 | 6* | 0 | 0 | 0 | 0 | 0 | 97 | evet |
| bölüm | BASİT | koyu | 1280 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 98 | — |
| onay | BASİT | koyu | 1280 | 0 | 0 | 0 | 0 | 6* | 0 | 0 | 0 | 0 | 0 | 98 | evet |
| bölüm | BASİT | koyu | 375 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 97 | — |
| onay | BASİT | koyu | 375 | 0 | 0 | 0 | 0 | 6* | 0 | 0 | 0 | 0 | 0 | 97 | evet |
| bölüm | PRO | açık | 1280 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 98 | — |
| onay | PRO | açık | 1280 | 0 | 0 | 0 | 0 | 6* | 0 | 0 | 0 | 0 | 0 | 98 | evet |
| bölüm | PRO | açık | 375 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 97 | — |
| onay | PRO | açık | 375 | 0 | 0 | 0 | 0 | 6* | 0 | 0 | 0 | 0 | 0 | 97 | evet |
| bölüm | PRO | koyu | 1280 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 98 | — |
| onay | PRO | koyu | 1280 | 0 | 0 | 0 | 0 | 6* | 0 | 0 | 0 | 0 | 0 | 98 | evet |
| bölüm | PRO | koyu | 375 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 97 | — |
| onay | PRO | koyu | 375 | 0 | 0 | 0 | 0 | 6* | 0 | 0 | 0 | 0 | 0 | 97 | evet |

\* 6 "binen"in tamamı modalın altındaki sayfa öğesiyle kesişmedir (`#modal-… ⟂ #ys-ad-tevsik…` vb.); modal dışı binen 0 — önceki paketin onay penceresi ölçümüyle birebir aynı (onay3-tablo: 6). Ek ölçüm (`bosluk.mjs`): "Önizle" düğmesi ile düzeltme katlanırı arası **16 px** (1280 ve 375), "Diğer bilgiler" ile "Önizle" arası da 16 px.
**Bakılan görüntüler (4):** `olc/bolum__BASIT_aydinlik_375.png` (katlanır açık; tek satır seçicisiz, saat "14:18", alanlar tam genişlik, taşma yok), `olc/onay__PRO_karanlik_375.png` (onay penceresi: yanlış 175.000 ₺ → doğru 180.000 ₺, amber uyarı bandı, Vazgeç/Onayla ve düzelt), `olc/bolum__PRO_karanlik_1280.png` (ilk turda Önizle–katlanır boşluğu 0 burada görüldü → düzeltildi, sayısal ölçümle kanıtlandı; görüntü sınırı 4 olduğundan son hâl yeniden Read edilmedi), `olc/onay__BASIT_aydinlik_1280.png` (onay penceresi ortada, sayfa kartları altta).
`tutarlilik.mjs` (BASİT↔PRO fark) koşulmadı: bölüm yalnız ByGatech yönetim formunda, iki modda aynı bileşen (mod koşulu yok).

## 10. Değişen dosyalar (bu paket; `git status` ağacın büyük kısmını `??` gösteriyor — liste elle)
- `dotnet/Kuyumcu.Api/Data/YasalParametre.cs` (sütun)
- `dotnet/Kuyumcu.Api/Data/KuyumcuDbContext.cs` (indeks)
- `dotnet/Kuyumcu.Api/Data/Migrations/20261008104154_YasalParamAyniGunAn.cs` (yeni) · `.Designer.cs` (yeni) · `KuyumcuDbContextModelSnapshot.cs`
- `dotnet/Kuyumcu.Api/Common/YasalParametreServisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Masak/RiskServisi.cs`
- `dotnet/Kuyumcu.Api/Modules/Sistem/SistemModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Yasal/YasalParametreModulu.cs`
- `dotnet/Kuyumcu.Contracts/Sayfalar/YasalSinirlarSozlesmeleri.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.YasalParam.cs` · `Sozlukler.YasalParam2.cs` (yeni)
- `dotnet/Kuyumcu.Web/Bilesenler/YasalParamFormu.razor`
- `dotnet/Kuyumcu.Tests/YasalParam2Testleri.cs` (yeni) · `dotnet/Kuyumcu.Tests/YasalParamEkranTestleri.cs`

**Dokunulan sayfalar (TEK DOKUNUŞ):** `Bilesenler/YasalParamFormu.razor` (YASAL-PARAM-EKRAN'ın bileşeni; ikinci dokunuş gerekçesi: kuyruk maddesi 2 "aynı gün düzeltme yolu" bu formun parçası). `Pages/` altında dosya açılmadı.

## 11. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| (1) Eşik dönemi an sınırlı; sabah işlemi eski eşikle | ✓ | Saf an±1 sn testi; Risk DB testi; canlı: sabah 190.000 → değişiklik sonrası risk kimlikEksik 0; SQL tarih 11:18:55.179 < an 11:18:55.914 |
| (2) Aynı gün düzeltme: yalnız aynı operatör + gerekçe + denetim izi, geçmişe etki yok | ✓ | HTTP testi; canlı 403/400/201/409; SQL (yanlış satır değeri aynı, yalnız bitiş); denetim izi satırı; `DonemlerAn` iki an arası yanlış değer |
| (3) Maskelenmiş "geçmiş 400" assert yalıtıldı | ✓ | Test yalıtıldı + M3-1 kırmızı |
| Sınır testleri (an öncesi/sonrası 1 sn) | ✓ | `Ayni_gun_esik_degisimi_AN_siniri_1sn_once…`, Risk testi ±1 sn |
| Yetki testi: başka operatör 403 | ✓ | HTTP testi + canlı 403/403; M2-1 kırmızı |
| Denetim izi SQL kanıtı | ✓ | § 8 |
| Canlı 5166: sabah işlemi + öğle değişimi → eski eşik | ✓ | § 8 |
| Mutasyon ≥ 2 kırmızı, cmp AYNI | ✓ | 9/9 mutasyon kırmızı, 9/9 cmp AYNI |
| Gerçek Chrome 1280+375 · BASİT+PRO · açık+koyu | ✓ | § 9, 16 ölçüm, 4 görüntü |
| Build 0 hata | ✓ | 0 Hata (4 uyarı başka şeritlerin dosyalarında) |
| denetim.sh TEMİZ (P-30 hariç) | ⚠ ağaç ölçüsü | Bu paketin sayaçları ✓; tek ek ✗ P-18 KartListe 6 (<9) — GOC-A şeridinin `Pages/` değişikliği (§ 5); paralel şerit kapısı gereği orkestratöre |
| Tam süit 0 YENİ kırmızı | ✓ | 11122 test: 11113 geçti, 7 başarısız, 2 atlandı; 7'nin hiçbiri bu paketin dosyalarına dokunmuyor (§ 12) |
| Test kiracısı silindi, sayı başa döndü | ✓ | `kuyumcu_yp2_t5` 0 → 0 |
| Commit yok, dosya silme yok, sır basılmadı | ✓ | — |

## 12. Açık kalanlar (sahibiyle)
- **Tam süit kırmızıları (bu paketin değil):**
  - `P54HttpTestleri.Miktar_SIFIR_400_Turkce_mesaj_ve_veri_DEGISMEZ` — yalnız koşumda da kırmızı (deterministik); `UrunModulu.cs` Range mesajı değişimi → **URUN-FORM-2** (aynı dalganın kardeş şeridi).
  - `RaporDortTestleri.RD_KH01/02/03/06` — `maliyetiBilinenAdet eski=226.000 yeni=218.000`; önbellek/ön-toplam → **RAPOR-4+KAR-HIZ (t1)**.
  - `S23FisMusteriEkranTestleri.S23E_04_musteri_bosaltilinca_rozet_CIZILMEZ` ve `S25YazdirParaUstuEkranTestleri.S25_E2_…` — tek başına koşumda **yeşil** (28 testlik filtrede yalnız P54 kırmızı); tam süitte kararsız → `Islemler.razor`/`Bilesenler/Fis` sahibi (SATIS/GOC şeridi), paylaşılan test DB etkisi.
- **denetim P-18** (KartListe 6 < 9) → GOC-A.
- **Kapı tarafı gün çözünürlüğü (bilgi):** `MasakKapisi` geç eşleştirmede `esikGunu` (dekont günü) ile gün düzeyinde okur; o gün öğlen değişiklik varsa sabahki dekontun geç eşleştirmesi yeni değeri alır. Kuyrukta istenmedi (madde risk raporu içindi); ihtiyaç olursa `DonemlerAn` ile aynı kalıp uygulanır.
- `dotnet/Kuyumcu.Web/Pages/Islemler.YasalParam.cs.tmp` proje ağacında duruyor (bu paketin değil; silme kurucuda).
- Uyarı: tam süiti yeniden başlatırken kendi süreç ağacımı durdurmak için geniş bir `pkill -f "testhost.*Kuyumcu.Tests"` komutu çalıştı; komut sonrası kalan tek testhost bu paketin süreciydi (14:19:27, ebeveyni bu paketin vstest'i) ve PID ile durduruldu. Başka şeridin testhost'una isabet ettiğine dair iz yok, ama paralel şeritlerde 14:5x civarı yarıda kesilmiş koşum görülürse sebep bu olabilir.

## 13. Kurucuya ve mali müşavire notlar
- Mevzuat değeri değişmedi; yeni hüküm yazılmadı. Teknik karar: bugünden geçerli değişiklik **kaydedildiği andan** geçerlidir; aynı gün daha önce tamamlanmış işlemler önceki değerle değerlendirilir (onay metni ile risk raporu artık aynı şeyi söylüyor).
- Aynı gün düzeltmede, yanlış değerin yürürlükte kaldığı birkaç dakika içindeki işlemler **o değerle kayıtlı kalır** (kapıda gerçekten uygulanan değer). Mali müşavir sorusu: yanlış girişin yürürlükte olduğu aralıkta kimlik alınmadan geçen bir işlem olursa (ör. yanlışlıkla gevşetilmiş eşik), sonradan kimlik tamamlama/raporlama yükümlülüğü nasıl ele alınmalı? Sistem bu aralığı denetim izinde saatleriyle verir; otomatik geriye dönük işlem yapılmadı (geçmiş yeniden yazılmaz).
