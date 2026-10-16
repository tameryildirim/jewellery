# PAKET T-K1 — PARA BİÇİMİ TUVALE ('57.550 ₺'), kur şeridi HAS/GRAM tam sayı, USD/EUR 2 hane — uygulama raporu

Tarih: 2026-09-24 20:54–21:35 · Rol: Taşkın (backend) + Ertuğ (frontend) · Dalga: TUVAL — KABUK (v2 · vt t1 · 5160/5161)
Durum beyanı: **BİTTİ** (paralel şerit kapısının 7 maddesi sağlandı; tam süitte kalan kırmızılar T-K1 dosyalarında değil, ayrıntı § 7).

## 1. Başlangıç durumu ve spesifikasyon doğrulaması
- Oturum açıldığında `Bicim.cs`, `UstBar.razor` (kur şeridi bölümü), `A4Belge.razor`, `tuval-kabuk.css`, `TK1ParaBicimiTestleri.cs` ve `S1cUstBarTestleri.cs` içinde bu paketin önceki (raporu yazılmamış) koşumundan kalan T-K1 kodu vardı. Hiçbiri körü körüne kabul edilmedi: derleme, filtreli testler, 9 mutasyon, tam süit, canlı API ve gerçek tarayıcı ölçümü bu oturumda **yeniden** koşuldu; eksik bulunan tek nokta (saat metni tuvalden 9 px sağda) düzeltildi (§ 2.5).
- Spesifikasyon (tuval `Panel.dc.html`, kur şeridi): `HAS 6.877 ₺ · GRAM 22 6.301 ₺ · USD 41,95 ₺ · 09:02` — etiket 14 px `#5a6577` büyük harf, değer 19 px 600, saat 14 px `#8a94a6`. Koddaki "mevcut durum" (₺ başta, `C2`/`N2` iki hane) T-K1 öncesi için doğruydu; hedef aynen uygulandı.
- Mevzuat: "kâğıt belgede kuruş iki hane" bir BİÇİM kararıdır (VUK belge tutarı kuruş taşır); koda yeni bir eşik/oran/yasal hüküm yazılmadı. Mevzuat hükmü sabitlenmediği için birincil kaynak araştırması gerekmedi.

## 2. Yapılanlar
### 2.1 `Kuyumcu.Web/Servisler/Bicim.cs` (tek merkez)
| Metot | Davranış | Örnek |
|---|---|---|
| `Tl(n)` | simge SONDA, NBSP (U+00A0) ile; kuruş SIFIR ise yazılmaz, değilse 2 hane; `AwayFromZero` yuvarlama | 57550 → `57.550 ₺` · 45000,5 → `45.000,50 ₺` · 99,999 → `100 ₺` |
| eksi | U+2212 `−` (tuvaldeki gibi) | −22000 → `−22.000 ₺`; −0,004 → `0 ₺` (işaretsiz sıfır) |
| `TlKurus(n)` | kuruş HER ZAMAN 2 hane | 45000 → `45.000,00 ₺`, 41,95 → `41,95 ₺` |
| `TlTam(n)` | tam liraya yuvarlar (kur şeridi HAS/GRAM) | 6876,60 → `6.877 ₺` |
| `BelgePara(n, pb)` | KÂĞIT: TRY → `TlKurus`, döviz → `Para` | 45000 TRY → `45.000,00 ₺` |
| `Para(n, pb)` | TRY → `Tl` (ekran); USD/EUR/GBP DEĞİŞMEDİ | 1250 USD → `1.250,00 $` |
| `Saat(t)` | `HH':'mm` InvariantCulture (fi-FI'de bile `09:02`) | |
| `Girdi(n)` | DEĞİŞMEDİ (P-78, gruplamasız `45000,00`) | |

**Eksi işareti kararı (U+2212):** Türkçe ekran okuyucular tireyi (U+002D) çoğunlukla "kısa çizgi" okur ya da atlar; U+2212 "eksi" okunur ve rakam genişliğinde olduğu için sütun hizası kaymaz. Kopyalama kaybı yok: `₺` soneki yüzünden bu metin zaten hesap tablosunda sayı olarak ayrışmaz; makineye giden her sayı `Bicim.Makine` (tire + nokta) ile üretilir (API gövdesi, CSV, SVG değişmedi).

### 2.2 `Layout/UstBar.razor` — YALNIZ kur şeridi biçimi
`#ust-kur-has` ve `#ust-kur-gram22` → `Bicim.TlTam`, `#ust-kur-usd` / `#ust-kur-eur` → `Bicim.TlKurus`, saat → `Bicim.Saat`; `.kur-bandi` title ipucu tam değeri kuruşuyla (`TlKurus`) taşır. Mevcut id'ler aynen korundu. (UstBar'daki diğer değişiklikler T-K2 şeridinindir, bu paketin kapsamı değil.)

### 2.3 Kâğıt çıktılar — kuruş her zaman 2 hane
- `Bilesenler/Yazdirma/A4Belge.razor` (Cikti.razor'un tek şablonu: satış fişi, cari ekstresi vb.): kalem hücresi ve tutar satırı `Bicim.BelgePara` → `45.000,00 ₺`. **Sapma/gerekçe:** A4Belge dosya alanı listesinde adıyla yoktu, ama KAPSAM maddesi "KAGIT CIKTILAR (Cikti.razor, A4Belge…) kurusu HER ZAMAN 2 hane" diyor ve hiçbir eşzamanlı şeridin listesinde değil; yalnız iki biçim çağrısı değişti.
- Gider pusulası (`PusulaBelgesi.razor`, `GiderPusulalari.razor`) ve `Uyum.razor` zaten `Bicim.BelgeParasi` (N2, sembol sonda) kullanıyordu — değişmedi, zaten 2 hane.
- `Bilesenler/Fis/*` ekrandaki POS fişi formudur (kâğıt değil) → ekran biçimi (`Tl`); T-SATIS şeridinin alanı, dokunulmadı.

### 2.4 Testler (sabit para metni yerine Bicim çağrısı)
- `Kuyumcu.Tests/TK1ParaBicimiTestleri.cs` (yeni, 26 test durumu): biçim SÖZLEŞMESİNİN kendisini sınar (bu yüzden beklenenler bilerek literal): Tl sıfır/kuruşlu/yuvarlama/eksi/−0, U+2212 ≠ tire ve ₺ başta değil, TlKurus, TlTam, Para dövizi değişmez, Girdi değişmez, Saat kültürden bağımsız, BelgePara ekrandan AYRIŞIR, A4Belge kalem hücresi ve tutar satırı kuruşu 2 hane basar.
- `S1cUstBarTestleri.cs`: `TK1_kur_seridi_has_gram_TAM_lira_usd_eur_IKI_hane_ipucu_kurusuyla` (yeni) + mevcut şerit testlerinin beklenenleri `Bicim.TlTam/TlKurus(..)` ile üretilir.
- Bicim kullanan diğer testlerde beklenen değer `Bicim.Tl(..)` ile üretilir (`GiderEkranTestleri` eksi U+2212, `RaporEkranTestleri`, `P57*`, `TS1TuvalSatisTestleri` yorum/üretim).

### 2.5 `wwwroot/css/tuval-kabuk.css` (app.css'e dokunulmadı)
- Saat `var(--metin-soluk)`, 14 px (PRO 13 px) — önceki koşumdan.
- **Bu oturumda eklendi (tarayıcı ölçümü sonucu):** yetkili kullanıcıda saat "kuru şimdi sor" DÜĞMESİDİR; dolgu+kenarlık (bosluk-2 + 1 px) metni tuvaldeki x=812'den 9 px sağa itiyordu (ölçüm: kutu 812, metin 821). Tıklama alanı küçülmesin diye dolgu korunur, kutu `margin-left: calc(-1 * (var(--bosluk-2) + 1px))` ile sola çekilir → ölçüm: kutu 803, **metin 812** (tuvalle aynı). Sabit renk yok, belirteç var.

## 3. Migration
Gerekmedi (şema değişikliği yok).

## 4. Derleme ve test koşumları
| Koşum | Sonuç |
|---|---|
| `dotnet build Kuyumcu.sln` (oturum başı) | 0 uyarı 0 hata |
| Filtre `TK1|S1cUstBar` | 49/49 geçti |
| Filtre `TK1|S1cUstBar|BicimTestleri|Q94Cikti` (mutasyon tabanı) | 54/54 geçti |
| Tam süit #1 (20:56, kendi değişikliklerim ağaçta) | **5436/5436 geçti**, 0 başarısız |
| Tam süit #2 (21:07, mutasyonlardan sonra) | 5452 · 5450 geçti · 2 başarısız (`S21FisYapiTestleri.Islemler_razor_1400_satiri_asmaz`, `BasitModPosTestleri.YASAL_KAPI_tevsik_basit_modda_odeme_turunu_ACAR`) |
| Tam süit #3 (21:31, CSS düzeltmesinden sonra, trx `scratchpad/trx-pT-K1/_Mac_2026-09-24_21_31_46.trx`) | 5464 · 5462 geçti · 2 başarısız (`Q102HurdaOnayEkranTestleri.Onaylaninca_POST_gider_bant_yazilir_ve_pencere_kapanir` — bUnit WaitFor zaman aşımı; `BasitModPosTestleri.YASAL_KAPI_tevsik…`) |
| İki kırmızının tek başına yeniden koşumu | `Q102HurdaOnay*` + `YASAL_KAPI_tevsik` → **13/13 geçti** |
| Son temiz derleme (21:30) | 0 hata (1 uyarı: `EtiketEkranTestleri.cs(145)` xUnit1031 — başka şeridin dosyası) |

Test sayısı artışı (5436 → 5464) eşzamanlı şeritlerin yeni testleridir. T-K1'e ait yeni test: 26 (TK1ParaBicimiTestleri) + 1 (S1cUstBar TK1) = **27**.

## 5. Mutasyon kanıtları (yöntem: yedek → `perl -0777 -pi` tek eşleşme → filtreli test → geri kopya → `cmp`)
| Kod | Değişiklik | Kırmızıya dönen testler | Geri alındı |
|---|---|---|---|
| M1-1 | `Tl`: sıfır kuruş kırpması kaldırıldı (hep N2) | `TK1_Tl_isaret_sonda_sifir_kurus_yazilmaz` (57550, 0, 860, 99.999, −22000, −0.004), `TK1_BelgePara_TRY_kurusu_HER_ZAMAN_yazar_ekrandan_AYRISIR`, `TK1_Para_TRY_ekran_bicimi_doviz_DEGISMEZ` (8 kırmızı) | cmp aynı |
| M1-2 | eksi U+2212 → tire `-` | `TK1_Tl_eksi_isareti_U2212_tire_DEGIL_ve_simge_basta_DEGIL`, `TK1_Tl_isaret…`(−22000, −125.5), `TK1_TlKurus_kurus_HER_ZAMAN_iki_hane`(−10) (4) | cmp aynı |
| M1-3 | `BelgePara` TRY → ekran `Tl` | `TK1_BelgePara_TRY…`, `TK1_A4Belge_kalem_hucresi_kurusu_iki_hane_basar`, `TK1_A4Belge_tutar_satiri_kurusu_iki_hane_basar` (3) | cmp aynı |
| M1-4 | UstBar HAS `TlTam` → `TlKurus` | `S1cUstBarTestleri.TK1_kur_seridi_has_gram_TAM_lira_usd_eur_IKI_hane_ipucu_kurusuyla`, `Basit_modda_serit_TUVAL_SIRASIYLA_has_gram22_usd_cizer_EUR_yok` (2) | cmp aynı |
| M1-5 | UstBar USD `TlKurus` → `TlTam` | aynı iki S1cUstBar testi (2) | cmp aynı |
| M1-6 | UstBar GRAM 22 `TlTam` → `TlKurus` | `TK1_kur_seridi…`, `Gram22_SUNUCUNUN_ayar_fiyatindan_gelir_istemcide_HESAPLANMAZ` (2) | cmp aynı |
| M1-7 | A4Belge kalem hücresi `BelgePara` → `Para` | `TK1_A4Belge_kalem_hucresi_kurusu_iki_hane_basar` (1) | cmp aynı |
| M1-8 | `Saat` kültürsüz → `ToString("HH:mm")` (kültürlü) | `TK1_Saat_HH_mm_iki_nokta_kulturden_BAGIMSIZ` (1) | cmp aynı |
| M1-9 | `Para` USD `N2` → `N0` | `TK1_Para_TRY_ekran_bicimi_doviz_DEGISMEZ`, `TK1_BelgePara_TRY…`, `TK1_A4Belge_tutar_satiri…` (3) | cmp aynı |

Not (Madde 1): M1-8'in ilk koşumu, eşzamanlı şeridin aynı `obj/` üzerindeki derlemesi yüzünden bayat DLL ile koştu (M1-7'nin kırmızısı görüldü); açık `dotnet build` sonrası yeniden koşuldu ve doğru test kırmızıya döndü. Son mutasyondan sonra `dotnet build Kuyumcu.sln` bir kez başka sürecin kilitlediği `Kuyumcu.Web.pdb` yüzünden 1 hata verdi; 20 sn sonra yeniden: **0 uyarı 0 hata**.

## 6. denetim.sh
`bash dotnet/denetim.sh` → **DENETİM TEMİZ.** (21:00 ve CSS düzeltmesinden sonra 21:27, ikisi de rc=0).

## 7. Paralel şerit kapısı — madde (7) "yeni kırmızı üretmedi"
Tam süit #3'teki iki kırmızı:
| Test | Dosya/sahip | Kanıt T-K1'den olmadığına |
|---|---|---|
| `BasitModPosTestleri.YASAL_KAPI_tevsik_basit_modda_odeme_turunu_ACAR` | `Pages/Islemler.razor` — **T-SATIS** | `#islem-odeme` kapı kapalıyken çiziliyor (Assert.Empty); biçimle ilgisi yok; Islemler.razor 21:02'de değişti; tek başına 21:34'te geçti. Süit #1'de (20:56) yeşildi. |
| `Q102HurdaOnayEkranTestleri.Onaylaninca_POST_gider_bant_yazilir_ve_pencere_kapanir` | Hurda — **Q10-A** | bUnit WaitFor zaman aşımı (yük altında); beklenen metin `8,660 g` (gram, T-K1 dokunmadı); tek başına geçti. |
Süit #2'deki `S21FisYapiTestleri.Islemler_razor_1400_satiri_asmaz` (Islemler.razor 1401 satır) de **T-SATIS**'e aittir; süit #3'te yeşil (dosya 1400 satıra indi).
Devir notunda kırmızı listesi verilmediği için karşılaştırma tabanı süit #1'dir (5436/5436 yeşil, T-K1 kodu ağaçta): FARK = 0 T-K1 kaynaklı kırmızı.

## 8. Canlı doğrulama (API 5160 · vekil 5161 · kuyumcu_net)
Test kiracısı `PAKETT-K1-TEST Kuyumculuk` (`01M3A8ZWHPY8SE9FT03EYWXC1B`) `betikler/demo-tohumla.js --prova-kiraci` ile ürün uçlarından kuruldu (sistem anahtarı yalnız bu API sürecine verilen tek kullanımlık rastgele değer; ekrana yazılmadı, iş sonunda dosyası boşaltıldı): 6 cari, 12 ürün, 9 satış.
| İstek | HTTP | Yanıttan okunan |
|---|---|---|
| POST /api/v1/sistem/davetler → /auth/kayit (betik) | 200/201 | TOHUMLANDI (24,5 sn) |
| POST /api/v1/auth/giris | 200 | accessToken var |
| GET /api/v1/kur/guncel | 200 | altinHasFiyati 6699.98 · gramAltinFiyati 6733.14 · usdTry 48.947 · eurTry 55.668 · kaynak ALTINKAYNAK · K22 gramFiyati 6137.18 |
| GET /api/v1/auth/ben (aynı saniyede 3. istek) | **429** | oran sınırı (auth uçları) — beklenen koruma; mod bilgisi tarayıcıdan `data-mod` ile okundu |
Ekrandaki karşılığı (aynı anda): `HAS 6.700 ₺ · GRAM 22 6.137 ₺ · USD 48,95 ₺ · EUR 55,67 ₺` → 6699,98→6.700 (tam), 6137,18→6.137, 48,947→48,95, 55,668→55,67: **API değeri ile ekran biçimi birebir tutarlı**.
SQL (kiracı süzgeçli): `hareketler` SATIS `01M3A90KDV9DZHFBMD15W0RX4B` tl_tutari **3200.00** TRY → `/cikti/SATIS_FISI/01M3A90KDV9DZHFBMD15W0RX4B` A4 belgesinde **`3.200,00 ₺`** (kâğıt: kuruş 2 hane), aynı anda ekran şeridi `6.708 ₺` (tam) — ayrım canlıda doğrulandı.
Panel'de eksi tutar: `−3.840 ₺`, karakter kodu **U+2212** (tarayıcıda `charCodeAt`).
İngilizce arayüz (EN): `FINE 6.710 ₺ · GRAM 22K 6.147 ₺ · USD 48,95 ₺ · EUR 55,68 ₺` — `TRY` 0, `1,234.56` kalıbı 0 (kurucunun 24.09 "TRY6,754.91" şikâyeti kapalı).

Temizlik: API (5160) ve vekil (5161) durduruldu, `lsof` iki port için boş. `betikler/kiraci-temizle.sh 01M3A8ZWHPY8SE9FT03EYWXC1B --uygula` → SİLİNDİ; 70 tenant_id'li tablo toplamı **0 satır**; `kiraci_davetleri`nde PAKETT-K1 satırı 2 → **0** (biri önceki koşumdan). tenants: başlangıç **13** → kiracım silindikten sonra 14 = 13 + başka şeridin 17:59 UTC'de açtığı `PAKETT-M1-TEST` (benim başlangıç sayımından sonra); benden önce var olan kiracı sayısı yine **13**. QA-TEST-* ve DEMO-* kiracılarına dokunulmadı.

## 9. Gerçek tarayıcı ölçümü
Araç: `betikler/sikisma-olc.js` (K1–K11 + yatay, sekmeye aynen yüklendi) + T-K1 ek sayaçları (`__tk1`: şerit değerleri, şerit öğesi kırık/taşan, `₺` ÖNEKLİ sayı, tireli eksi TL, `1,234.56` kalıbı, TRY). İki araç: (a) tarayıcı bölmesinde bu şeridin 5161 sekmesi (`tab-50` — sekme sınırı dolu olduğu için yeni sekme açılamadı; bu sekme zaten 5161'e, yani bu şeride aitti), (b) başsız Chrome + CDP (`scratchpad/cek.mjs`) ile 12 ekran görüntüsü.

| Rota | Genişlik | Mod | Tema | K1 | K2 | K3 taşan/kırık | K5 TRY | K6 | K7 | K8 | K9 | K10 | K11 | Yatay | Şerit (HAS/G22/USD/EUR) | şerit kırık/taşan | ₺ önek | tire-eksi | en-US |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| / | 1440 | BASİT | açık | 0 | 98 | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 6.700/6.137/48,95/— | 0/0 | 0 | 0 | 0 |
| /islemler | 1440 | BASİT | açık | 0 | 98 | 0/0 | 0 | 0/2 | 0 | 0 | 0 | 0 | 0 | 0 | aynı | 0/0 | 0 | 0 | 0 |
| /kasalar | 1440 | BASİT | açık | 0 | 98 | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | aynı | 0/0 | 0 | 0 | 0 |
| /raporlar | 1440 | BASİT | açık | 0 | 98 | 0/0 | 0 | 0/0 | 0 | **1** ("cari") | 0 | 0 | 0 | 0 | aynı | 0/0 | 0 | 0 | 0 |
| /ayarlar | 1440 | BASİT | açık | 0 | 98 | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | aynı | 0/0 | 0 | 0 | 0 |
| / · /islemler · /kasalar | 1440 | BASİT | koyu | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6.711/6.147/48,95/— | 0/0 | 0 | 0 | 0 |
| / · /islemler · /kasalar | 1440 | PRO | açık | 0 | 98·83·98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6.711/6.147/48,95/55,68 | 0/0 | 0 | 0 | 0 |
| / · /islemler · /kasalar | 1440 | PRO | koyu | 0 | 98·83·98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6.711/6.147/48,95/55,68 | 0/0 | 0 | 0 | 0 |
| / · /islemler · /kasalar | 1280 | BASİT | açık | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6.711/6.147/48,95/— | 0/0 | 0 | 0 | 0 |
| / · /islemler · /kasalar | 1280 | BASİT | koyu | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6.711/6.147/48,95/— | 0/0 | 0 | 0 | 0 |
| / · /islemler · /kasalar | 1280 | PRO | açık | 0 | 98·88·98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6.713/6.149/48,95/55,69 | 0/0 | 0 | 0 | 0 |
| / · /islemler · /kasalar | 1280 | PRO | koyu | 0 | 98·88·98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6.713/6.149/48,95/55,69 | 0/0 | 0 | 0 | 0 |
| / · /islemler · /kasalar | 375 | BASİT | açık | 0 | —·97·97 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6.712/6.148/48,95/— | 0/0 | 0 | 0 | 0 |
| / · /islemler · /kasalar | 375 | BASİT | koyu | 0 | 97·97·— | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | aynı | 0/0 | 0 | 0 | 0 |
| / · /islemler · /kasalar | 375 | PRO | açık | 0 | 97 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6.712/6.148/48,95/55,68 | 0/0 | 0 | 0 | 0 |
| / · /islemler · /kasalar | 375 | PRO | koyu | 0 | 97 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6.710/6.147/48,95/55,68 | 0/0 | 0 | 0 | 0 |

Notlar: (i) `/raporlar` BASİT'te K8=1 ("cari") Raporlar sayfasının metnidir — T-K1 dosyası değil, sahibi Rapor ekranı; açık kalanlara yazıldı. (ii) K2 "—" (null) ölçüm anında kart çizilmemişti (2,6 sn bekleme); aynı rota diğer kombinasyonda 97–98. (iii) 1280 PRO'nun ilk ölçümü mod PATCH'i yetişmeden koştu (EUR yoktu) → 15 sn bekleyip yeniden ölçüldü, tabloda yeniden ölçüm var. (iv) Şerit değerlerindeki küçük farklar canlı kurun dakikalar içinde değişmesidir. (v) 375 px'te `.kur-bandi` app.css:147 `@media (max-width:720px){display:none}` ile gizlidir — ilk commit'ten beri var olan tasarım (git HEAD satır 146), T-K1 değiştirmedi; saat görünür kalır. (vi) Başsız Chrome 12 kombinasyonun hepsinde: TRY 0, ₺ önek 0, yatay 0, şerit sığıyor.

Ekran görüntüleri: `ham/TUVAL-KARSILASTIRMA/T-K1-ekran/panel-{1440,1280,375}-{basit,pro}-{aydinlik,karanlik}.png` (12 dosya).

## 10. Görsel karşılaştırma (tuval `Panel.png` 1440×900 ↔ bizim 1440×900 BASİT açık)
Dosyalar: `ham/TUVAL-KARSILASTIRMA/T-K1-Panel-1440.png` (tam sayfa yan yana) · `ham/TUVAL-KARSILASTIRMA/T-K1-KurSeridi-1440.png` (şerit, 2×, alt alta).
| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Şerit sırası | HAS · GRAM 22 · USD · saat | HAS · GRAM 22 · USD · saat (PRO: + EUR, + "· Altınkaynak") | AYNI (PRO eki T-K2 kapsamı, tuval Profesyonel) |
| HAS değer biçimi | `6.877 ₺` tam lira, NBSP, ₺ sonda | `6.709 ₺` | AYNI (değer canlı veri) |
| GRAM 22 biçimi | `6.301 ₺` | `6.145 ₺` | AYNI |
| USD biçimi | `41,95 ₺` 2 hane | `48,95 ₺` | AYNI |
| Etiket | 14 px, büyük harf, .04em, `#5a6577` | 14 px, uppercase, 0,56 px (= .04em), `rgb(90,101,119)` = `#5a6577` | AYNI |
| Değer | 19 px / 600 | 19 px / 600 | AYNI |
| Öğe x konumları | HAS değeri ~427, G22 ~590, USD ~719 | 427 · 590 · 719 (ölçüm) | AYNI |
| Saat metni x | 812 | 812 (düzeltme öncesi 821) | AYNI (bu oturumda düzeltildi, § 2.5) |
| Saat biçimi | `09:02` 14 px | `21:23` 14 px, HH:mm | AYNI |
| Saat rengi | `#8a94a6` | `var(--metin-soluk)` = `#5a6577` (açık) | GEREKÇELİ FARK: `#8a94a6` için belirteç yok (sabit renk yasak) ve beyaz üstünde ~3:1 kontrastla WCAG AA'yı (4,5:1) geçmez; en yakın soluk belirteç kullanıldı |
| Panel tutarları | `57.550 ₺`, `84.200 ₺`, `38.900 ₺`, `Nakit 44.200 ₺`, `−22,0 g has` | `99.887,69 ₺`, `216.991,15 ₺`, `22.339,35 ₺`, `−3.840 ₺` | AYNI biçim (kuruşlu tutarlarda 2 hane — kapsam: "kuruş sıfır değilse '45.000,50 ₺'"; veri tuval örneği değil gerçek) |
| Arama kutusu "Müşteri, ürün ara Ctrl K", kenar çubuğu, kart içerikleri | tuvalde farklı | — | T-K1 KAPSAMI DIŞI (T-K2 kabuk / T-P1 panel şeritleri) |

## 11. DoD sözleşmesi
T-K1 yeni varlık/kayıt türü, uç, form veya düğme ÜRETMEDİ (yalnız görüntü biçimi). PATCH/DELETE/[RolGerekli]/denetim kaydı gerektiren yeni öğe yok; mevcut `#ust-kur-saat` düğmesi (kuru şimdi sor) davranışı değişmedi, yalnız hizası. Kaydet()/POST yolları, yetki ve yasal kapılar değişmedi. Sözlüğe yeni metin eklenmedi.

## 12. Dokunulan sayfalar / dosyalar (bu paketin payı)
- `dotnet/Kuyumcu.Web/Servisler/Bicim.cs` (M — T-K1 metotları)
- `dotnet/Kuyumcu.Web/Layout/UstBar.razor` (M — yalnız kur şeridi biçim çağrıları; dosyadaki diğer farklar T-K2'nin)
- `dotnet/Kuyumcu.Web/Bilesenler/Yazdirma/A4Belge.razor` (?? dizin izlenmiyor — 2 biçim çağrısı `BelgePara`)
- `dotnet/Kuyumcu.Web/wwwroot/css/tuval-kabuk.css` (?? — T-K1 saat blokları)
- `dotnet/Kuyumcu.Tests/TK1ParaBicimiTestleri.cs` (yeni), `S1cUstBarTestleri.cs`, `GiderEkranTestleri.cs`, `RaporEkranTestleri.cs`, `P57EkNoktaOndalikEkranTestleri.cs`, `P57RaporBicimEkranTestleri.cs`, `TS1TuvalSatisTestleri.cs` (yalnız beklenen değer üretimi / yorum) — `dotnet/Kuyumcu.Tests/` git'te izlenmeyen dizin (`??`).
- `git status --porcelain | grep -v -E "/(bin|obj)/"` ilgili satırlar: ` M dotnet/Kuyumcu.Web/Layout/UstBar.razor`, ` M dotnet/Kuyumcu.Web/Servisler/Bicim.cs`, `?? dotnet/Kuyumcu.Web/wwwroot/css/tuval-kabuk.css`, `?? dotnet/Kuyumcu.Tests/`, `?? dotnet/Kuyumcu.Web/Bilesenler/Yazdirma/`.
- Belge: bu rapor; `ham/TUVAL-KARSILASTIRMA/T-K1-Panel-1440.png`, `T-K1-KurSeridi-1440.png`, `T-K1-ekran/` (12 png).

## 13. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Bicim.Tl `N.NNN ₺`, NBSP, ₺ sonda, binlik nokta | SAĞLANDI | TK1_Tl_* testleri; M1-1; canlı `99.887,69 ₺`, `3.840 ₺` |
| Kuruş sıfır değilse 2 hane | SAĞLANDI | `45.000,50 ₺` testi |
| Eksi U+2212 + gerekçe | SAĞLANDI | M1-2; canlı `−3.840 ₺` U+2212; § 2.1 gerekçe |
| Kur şeridi HAS/GRAM tam, USD/EUR 2 hane, saat HH:mm | SAĞLANDI | S1cUstBar TK1 testi; M1-4/5/6/8; canlı API↔ekran tablosu |
| Kâğıt çıktı kuruş hep 2 hane + testle kilit | SAĞLANDI | TK1_BelgePara / TK1_A4Belge_*; M1-3/M1-7; canlı `3.200,00 ₺` |
| Girdi değeri değişmez | SAĞLANDI | TK1_Girdi_bicimi_DEGISMEZ_gruplamasiz |
| USD/EUR/GBP `1.250,00 $` kalır | SAĞLANDI | TK1_Para_…_doviz_DEGISMEZ; M1-9 |
| Testlerde sabit para metni yok (sözleşme testi hariç) | SAĞLANDI | S1c/Gider/Rapor testleri Bicim çağrısıyla üretir; TK1ParaBicimi sözleşmenin kendisini literal sınar (bilerek) |
| app.css'e dokunulmadı, kendi css | SAĞLANDI | yalnız tuval-kabuk.css |
| Gerçek tarayıcı 1440/1280/375 × BASİT/PRO × açık/koyu | SAĞLANDI | § 9 tablo + 12 png |
| Tuval yan yana karşılaştırma + bölge tablosu | SAĞLANDI | § 10, iki png |
| Mevcut id'ler korunur | SAĞLANDI | `#ust-kur-has/gram22/usd/eur/saat` aynı; bUnit id ile buluyor |
| Build 0 hata · testlerim 0 kırmızı · denetim TEMİZ · mutasyon · canlı · kiracı silindi · yeni kırmızı yok | SAĞLANDI | § 4–8 |
| İzolasyon (t1 DB, 5160/5161, kendi sekme) | SAĞLANDI | KUYUMCU_TEST_DB=kuyumcu_test_t1; 5199/diğer portlara dokunulmadı |

## 14. Açık kalanlar (sahibiyle)
1. `BasitModPosTestleri.YASAL_KAPI_tevsik_basit_modda_odeme_turunu_ACAR` — süit #2 ve #3'te kırmızı, tek başına yeşil — **T-SATIS** (Islemler.razor).
2. `Q102HurdaOnayEkranTestleri.Onaylaninca_POST_gider_bant_yazilir_ve_pencere_kapanir` — yük altında WaitFor zaman aşımı, tek başına yeşil — **Q10-A**.
3. `S21FisYapiTestleri.Islemler_razor_1400_satiri_asmaz` — süit #2'de kırmızı (1401 satır), süit #3'te yeşil — **T-SATIS**.
4. `/raporlar` BASİT modda K8 jargon "cari" 1 adet — Rapor ekranı sahibi (T-K1 dışı).
5. Uyarı `EtiketEkranTestleri.cs(145)` xUnit1031 — başka şeridin dosyası; bir ara derlemede `Q105FormJargonTabloTestleri.cs(288)` CS0103 hatası görüldü (Q10-5 yarım işi), 1 dk sonra düzelmişti.
6. Saat rengi için tuvaldeki `#8a94a6`'ya karşılık gelen bir belirteç istenirse (erişilebilirlik kontrastı düşük) kurucu/tasarım kararı gerekir.

## 15. Kurucuya / mali müşavire not
- Kâğıt belgelerde (fiş, fatura şablonu, gider pusulası, ekstre) tutarlar kuruşuyla (`45.000,00 ₺`) basılmaya devam eder; ekranda sıfır kuruş gizlenir. Mali müşavirden yalnız "kâğıt belgede ₺ işaretinin tutarın SONUNDA olması" için itiraz olup olmadığının teyidi istenebilir (gider pusulasında zaten P-36b'den beri sondaydı).
