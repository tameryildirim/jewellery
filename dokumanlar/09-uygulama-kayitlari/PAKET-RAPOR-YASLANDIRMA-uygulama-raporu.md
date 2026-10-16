# PAKET RAPOR-YASLANDIRMA — uygulama raporu (tur 1 + tur 2)

Dalga RAPOR-YASLANDIRMA · şerit t2 (`KUYUMCU_TEST_DB=kuyumcu_test_t2`, canlı API 5162, vekil 5163, CDP 9907/9908) · 01.10.2026 · Taşkın (backend) + Ertuğ (frontend).
Kaynak: `PAKET-GOC-47B-uygulama-raporu.md` § 0.7 ve § 11 (hasım ORTA bulgusu), `PAKET-YASAL-3-uygulama-raporu.md` § 00.6 (açık kalanlar: sabit döviz listeleri).
COMMIT YOK. Dosya silinmedi.


> **Durum: tur 2 (son).** Tur 1 metni § 1-14'te tarihçe olarak duruyor. Tur 2'de değişen her şey bu bölümde. Çelişki olursa **§ 0 geçerlidir**.

## 0. Tur 2: hasım bulgularının kapatılması (01.10.2026)

### 0.1 Bulgu → düzeltme
| Bulgu | Düzeltme | Dosya |
|---|---|---|
| **KRİTİK**: açılış geri alınınca kalan yeni borç "Tarihsiz"e düşüyor, "Açılış bakiyesi" rozeti alıyor. Sebep: ters açılış kaydı (tür yine ACILIS) son yazım sayılıyordu. | Kural yeniden yazıldı. BELIRSIZ yalnız iki koşul **birlikte** sağlanınca verilir: (a) açılış NET ≠ 0 ve (b) son açılıştan sonra açılış OLMAYAN yazım yok. Öteki durumlarda yaşlanma tarihi = açılış olmayan son hareketin tarihi (`MAX(tarih) WHERE tur<>'ACILIS'`); ters açılış anı sayılmaz. Açılışı hiç olmayan cari eski modelle (son işlem tarihi) dilimlenir, davranışı değişmedi. `belirsizNedeni='ACILIS'` yalnız `acilisDahil=true` iken verilir (`CariYaslandirmaKurali.BelirsizNedeni`). | `RaporModulu.cs` (CariYaslandirma projeksiyonu), `CariYaslandirmaKurali.cs` |
| **ORTA**: `acilisDahil` farklı birimlerin döviz tutarlarını tek toplamda topluyordu (USD +100 ile EUR −100 → 0). | Açılış neti üç parçada ölçülüyor: TL toplamı ≠ 0, has toplamı ≠ 0, ya da döviz `(CariId, ParaBirimi)` ikilisine göre gruplanıp **herhangi bir birimin** neti ≠ 0 (`GroupBy(ParaBirimi).Any(Sum ≠ 0)`, SQL'de `EXISTS … GROUP BY … HAVING`). Bayrak artık dilimle **aynı SQL projeksiyonundan** geliyor (`Satir.AcilisDahil`). Ayrı sayfa sorgusu kaldırıldı, dilim ile bayrak birbirinden sapamaz. | `RaporModulu.cs` |
| **DÜŞÜK**: "Tarihsiz" notu ve başlık ipucu yalnız açılışı anlatıyordu, oysa TARIH_YOK satırları da aynı sütunda. | `BelirsizNotu` iki nedeni de anlatacak şekilde genelleştirildi. Sözlüğe eklenenler: `BelirsizIpucu` (sütun başlığı), `TarihYokIpucu` ("İşlem tarihi yok" rozeti) ve `AcilisDahilIpucu` ("Açılış bakiyesi de var" rozeti). Eski `AcilisIpucu` metni bu rozette yanlış bir şey söylüyordu ("yeni borç gibi gösterilmez"). TR ve EN birlikte eklendi; konumsal kayıt olduğu için eksik anahtar derleme hatası verir. | `Sozlukler.RaporYaslandirma.cs`, `Raporlar.razor` (yalnız yaşlandırma tablosundaki 3 `title`) |
| **DÜŞÜK**: Raporlar.razor aynı dalgada iki şerit tarafından açıldı (MÜHÜR 2). | Bu paketin ürünü değil. Tur 2'de bu dosyada yalnız yaşlandırma bölümündeki üç `title` özniteliği değişti. SUBE-KAPSAM-B'nin Mutabakat ve SubeliSorgu satırlarına dokunulmadı. **Orkestratörün kapı kaydına gerekçe yazması gerekiyor** (eşzamanlı dokunuş: t1 şube parametresi, t2 yaşlandırma bölümü). | — |

Hasımın iki sınır testi (`RAPORYASLANDIRMASinirTestleri.Acilis_geri_alininca_kalan_yeni_borc_0_30_da_kalir`, `…Karma_doviz_acilisi_birimler_toplanip_sifirlanmaz_acilisDahil`) **değiştirilmedi** ve şimdi YEŞİL.

### 0.2 Yeni testler (tur 2: +7)
| Test | Tür | Ne kanıtlar |
|---|---|---|
| `RaporYaslandirmaDbTestleri.Acilis_ters_kaydindan_sonra_kalan_yeni_borc_0_30_da_acilis_bayragi_yok` | DB | açılış 1000 → 50 veresiye → ters açılış: dilim 0-30, `acilisDahil=false`, `belirsizNedeni=null`, özet 0-30=50, BELIRSIZ=0 |
| `RaporYaslandirmaDbTestleri.Karma_doviz_acilisi_birim_bazinda_netlenir_belirsiz_ve_acilisDahil` | DB | USD +100 / EUR −100 açılış: BELIRSIZ/ACILIS, `acilisDahil=true`. Ardından 50 TL veresiye: 0-30 ve `acilisDahil` hâlâ true |
| `RaporYaslandirmaSafTestleri.Belirsiz_nedeni_acilis_yalniz_acilisDahil_iken` ×4 | saf (Theory) | (null,true)→ACILIS · (null,false)→TARIH_YOK · (5,*)→null |
| `RaporYaslandirmaEkranTestleri.Tarih_yok_satiri_kendi_rozeti_ve_ipucuyla` | bUnit | TARIH_YOK satırında "İşlem tarihi yok" rozeti ve kendi ipucu var, açılış rozeti yok; not iki nedeni de söylüyor |
| `RaporYaslandirmaEkranTestleri.Acilis_satiri_ayri_sutunda_0_30_da_degil` (genişletildi) | bUnit | sütun başlığı `title` = `BelirsizIpucu`; açılış rozeti `title` = `AcilisIpucu` |

Filtreli koşum (`RaporYaslandirma|RAPORYASLANDIRMA|Yaslandirma`): **tur 1 sonu 36/36 → tur 2: 43/43 yeşil** (0 başarısız).

### 0.3 Mutasyon kanıtları (tur 2)
Yöntem `mut.zsh` (tur 2 kayıtları `/private/tmp/kuyumcu-serit/RAPOR-YASLANDIRMA/tur2/mut-*.log`). Betiğe **derleme yeniden deneme** eklendi. Sebebi: ilk M-RY-8 denemesinde başka şeridin eşzamanlı derlemesi `Kuyumcu.Web.pdb`'yi kilitledi (`MSB4018 … being used by another process`). O sırada test eski (M-RY-1'li) DLL ile koştu. **O deneme geçersiz sayıldı ve yeniden koşuldu.** Tablodaki her satırın build günlüğü "0 hata (deneme 1)".

| Kod | Değişiklik (tek eşleşme) | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M-RY-1 | BELIRSIZ kolu kapatıldı (`? null` → `? SonIslemTarihi`): ACILIS 0-30'a geri | `Acilis_bakiyesi_BELIRSIZ_dilimde_veresiyeler_gun_dilimlerinde_ozet_satirlarla_ayni`, `Karma_doviz_acilisi_birim_bazinda_…`, `Has_0_0001_…`, `Cok_buyuk_tutar_…`, `Eksi_acilis_…`, `Kurus_yuvarlamasi_…` (6) | cmp AYNI |
| M-RY-8 | Açılış-neti koşulu düşürüldü (tur 1'in KRİTİK hatası geri) | `Acilis_ters_kaydindan_sonra_kalan_yeni_borc_0_30_da_acilis_bayragi_yok`, `RAPORYASLANDIRMASinirTestleri.Acilis_geri_alininca_kalan_yeni_borc_0_30_da_kalir` (2) | cmp AYNI |
| M-RY-9 | Döviz neti birimden bağımsız toplandı (`GroupBy(ParaBirimi).Any` → `Sum ≠ 0`) | `Karma_doviz_acilisi_birim_bazinda_netlenir_belirsiz_ve_acilisDahil`, `RAPORYASLANDIRMASinirTestleri.Karma_doviz_acilisi_birimler_toplanip_sifirlanmaz_acilisDahil` (2) | cmp AYNI |
| M-RY-6 | `AcilisDahil = false` | `Karma_doviz_…`, `Acilis_bakiyesi_BELIRSIZ_…`, `Acilistan_sonra_islem_goren_…`, `RAPORYASLANDIRMASinirTestleri.Karma_doviz_…` (4) | cmp AYNI |
| M-RY-10 | `BelirsizNedeni` hep ACILIS (acilisDahil yok sayıldı) | `Belirsiz_nedeni_acilis_yalniz_acilisDahil_iken(gun: null, acilisDahil: False, beklenen: "TARIH_YOK")` | cmp AYNI |
| M-RY-11 | Sütun başlığı ipucu yeniden `AcilisIpucu` | `RaporYaslandirmaEkranTestleri.Acilis_satiri_ayri_sutunda_0_30_da_degil` | cmp AYNI |
| M-RY-2 | GBP kolon listesinden çıkarıldı (`&& p.Name != "GbpBakiye"`) | `Doviz_birimleri_kolonlardan_turer_ve_yazici_kumesiyle_ayni`, `Bagli_kayitlar_yalniz_GBP_…`, `Yalniz_GBP_bakiyeli_cari_…`, `RAPORYASLANDIRMASinirTestleri.Bos_kiraci_…ozetDoviz_kodlari_0` (4) | cmp AYNI |

M-RY-3, 4, 5 ve 7 tur 1'de kanıtlandı (§ 5). Değiştirdikleri kod satırlarına tur 2'de dokunulmadı. Son mutasyondan sonra `dotnet build Kuyumcu.sln`: **0 Uyarı, 0 Hata** (`tur2/b3.log`).

### 0.4 Derleme · denetim · tam süit
- `dotnet build Kuyumcu.sln --nologo` sonucu **0 Uyarı · 0 Hata**. Tur 1'deki iki uyarı başka şeritlerdendi ve onlar düzeltmiş.
- `bash ./denetim.sh` sonucu: yalnız **1 ✗**, o da `P-30: DEMO_INSAN_SURESI (BK-45)` (kapsam dışı, P-30 hariç). Bu paketin sayaçlarının hepsi ✓.
- Tam süit (`trx-pRAPOR-YASLANDIRMA/tam-tur2.trx`, son kodla) sonucu: **8928 test · 8924 geçti · 2 başarısız · 2 atlandı.** Bu paketin testlerinde kırmızı yok (`RaporYaslandirma*` / `RAPORYASLANDIRMA*`: 0).

| Kırmızı | Sahibi | Kanıt |
|---|---|---|
| `SozlesmeKapsamTestleri.Kilitsiz_tip_listesi_circir_ile_donduruldu` | SUBE-KAPSAM-B (t1) | ileti: "sunucu DTO'suyla kilitlenmemiş YENİ istemci tipi: Mutabakat". Tur 1 § 7a'daki kırmızının aynısı |
| `SozlesmeKapsamTestleri.Kapsam_orani_beyan_edilen_degerle_ayni` (58 → 59) | SUBE-KAPSAM-B (t1) | aynı `Mutabakat` tipi. Tur 1 § 7a'daki kırmızının aynısı |

Tur 1 sonundaki kırmızı listesiyle karşılaştırınca **FARK = 0**: bu paket yeni kırmızı üretmedi. Tur 1'deki öteki 10 kırmızıyı SUBE-KAPSAM-B kendi tarafında kapatmış.

### 0.5 Canlı doğrulama (API 5162, vekil 5163, CDP 9909, kiracı `PAKETRAPOR-YASLANDIRMA-TEST Altın Evi` / `01M3VTWRD1RFPEJ42J87K5GMTE`)
Betikler `/private/tmp/kuyumcu-serit/RAPOR-YASLANDIRMA/canli2/{canli.mjs,olc.mjs,sql.txt}`. Yanıtların gövdeleri okundu (`kur.log`, `acilis.log`, `rapor.log`).

| Adım | Beklenen | Gerçek |
|---|---|---|
| Davet + kayıt + giriş | 200 | 200 |
| 7 cari (`POST /cari`) | 200 | 200 ×7 |
| B/C/D `POST /cari/{id}/duzeltme` +1000/+2000/+3000 · G `POST /cari/{id}/doviz-veresiye` GBP 100 | 200 | 200 ×4 |
| A açılış 5000 · E açılış 1000 · F açılış 700 (`/kurulum/acilis/prova` + `/uygula`, üç ayrı oturum) | 200/200 | prova 200 `temiz=true` ×3 · uygula 200 ×3 (`uygulananSatir:1`, `geriAlinabilir:true`) |
| E +50 TL, F +30 TL yeni veresiye | 200 | 200 ×2 |
| **E açılışı geri al** (`POST /kurulum/acilis/geri-al`) | 200 | **200** `{"geriAlinan":1}` |
| B/C/D 10/40/100 gün geriye (SQL `cari_bakiyeler.son_islem_tarihi` + `hareketler.tarih`) | — | 1/1/1 satır |
| `GET /raporlar/cari-yaslandirma` | 200 | **200** · `ozetTl {"0-30":1780,"31-60":2000,"61-90":0,"90+":3000,"BELIRSIZ":5000}` · `ozetDoviz {"USD":0,"EUR":0,"GBP":100}` · toplamSatir 7 |
| ↳ A (yalnız açılış) | BELIRSIZ, neden ACILIS | `BELIRSIZ gun=null neden=ACILIS acilisDahil=true` |
| ↳ B/C/D | 0-30 / 31-60 / 90+ | `0-30 gun=10` · `31-60 gun=40` · `90+ gun=100` |
| ↳ **E (açılış geri alındı, 50 TL yeni borç)** | **0-30, açılış bayrağı YOK** | **`0-30 gun=0 neden=null acilisDahil=false` tl=50** (tur 1'de bu durum BELIRSIZ çıkıyordu) |
| ↳ F (açılış 700 + 30 yeni) | 0-30, `acilisDahil=true` | `0-30 gun=0 acilisDahil=true` tl=730 |
| ↳ G (yalnız GBP) | listede, GBP 100 | `0-30 doviz={"GBP":100}` |
| `GET /cari/{G}/bagli-kayitlar` | `dovizBakiyeVar=true` | 200 `true` |

**SQL kanıtı.** Kural API'den bağımsız SQL ile yeniden hesaplandı (`canli2/sql.txt`, `sql.out`). Kullanılan kural: açılış yoksa son_islem_tarihi; açılış neti ≠ 0 (TL/has, ya da `(cari_id, para_birimi)` döviz) ve açılıştan sonra açılış dışı hareket yoksa NULL; değilse açılış dışı son hareket tarihi. Sonuç API ile **birebir aynı**:

| dilim | tl | n | cariler |
|---|---|---|---|
| 0-30 | 1780.00 | 4 | Bora, Ege, Fikret, Gül |
| 31-60 | 2000.00 | 1 | Cem |
| 90+ | 3000.00 | 1 | Deniz |
| BELIRSIZ | 5000.00 | 1 | Ayten |

Döviz toplamı `usd 0.00 · eur 0.00 · gbp 100.00`. Ham satırlar:
- E: `ACILIS +1000 (…:00.953)` → `CARI_DUZELTME +50 (…:01.075)` → `ACILIS −1000 (…:01.221)`; `son_islem_tarihi = 01.221`, yani ters açılış anı. Kural bu anı saymadı.
- F: `ACILIS +700` → `CARI_DUZELTME +30`.

Karma döviz açılışı (USD/EUR) API ile kurulamıyor, çünkü `/kurulum/acilis` satırı yalnız `tlTutar/hasGram` taşıyor. Bu yüzden DB testleriyle kanıtlandı: RY-10 ve hasımın sınır testi.

### 0.6 Gerçek tarayıcı (başsız Chrome, `betikler/tur/cdp.mjs`, CDP 9909; ölçer `betikler/sikisma-olc.js`)
Rota `/raporlar?sekme=cari`. Görüntüler `canli2/foto/` altında. Read ile görülenler: `cari__PRO_aydinlik_1280-tablo.png`, `cari__PRO_karanlik_1280-tablo.png`, `cari__PRO_karanlik_375-tablo.png`, `cari__BASIT_aydinlik_375.png`.

| Mod | Tema | px | Tablo | K1 | K2 % | K3 taşan/kırık | K5 TRY | K7 | K8 | K9 | K10 | K11 | K12 | Yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| PRO | açık | 1280 | var | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| PRO | açık | 375 | var | 0 | 97 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| PRO | koyu | 1280 | var | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| PRO | koyu | 375 | var | 0 | 97 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| BASİT | açık | 1280 | — (Basit yüz "Bugün") | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| BASİT | açık | 375 | — | 0 | 97 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| BASİT | koyu | 1280 | — | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| BASİT | koyu | 375 | — | 0 | 97 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

PRO ekranında DOM'dan okunan ve görüntüde görülenler:
- Ege satırı: 0-30 sütununda `50 ₺`, Tarihsiz `0 ₺`, rozet **yok**.
- Fikret satırı: "Açılış bakiyesi de var" rozeti, `title` = `AcilisDahilIpucu`.
- Ayten satırı: "Açılış bakiyesi" rozeti, Tarihsiz `5.000 ₺`.
- Alt toplam: 0-30 `1.780 ₺` · Tarihsiz `5.000 ₺`.
- Sütun başlığı `title`: "Ne zamandan beri beklediği bilinmeyen bakiyeler: önceki programdan devralınan açılış bakiyeleri ve işlem tarihi olmayan bakiyeler."
- Not: "\"Tarihsiz\" sütunu, ne zamandan beri beklediği bilinmeyen bakiyeleri gösterir: …açılış bakiyeleri ve işlem tarihi kayıtlı olmayan bakiyeler. Bunlar yeni borç sayılmaz."

Koyu temada renk belirteçleri doğru: görüntüde koyu zemin, okunur rakamlar. 375 px'te tablo `KartListe` içinde kaydırılır, sayfa düzeyinde yatay kaydırma 0. Tur 1'deki K3 "1 kırık" ölçer yapaylığı bu turda **0** çıktı.

### 0.7 Temizlik
- API (PID 17559 → dinleyen 17576) ve vekil (PID 25417) `kill` ile durduruldu. 5162 ve 5163 LISTEN boş.
- `zsh kiraci-temizle.sh 01M3VTWRD1RFPEJ42J87K5GMTE --uygula` çalıştırıldı. İkinci koşum: "Kiracı YOK … bağlantı sınandı ve ÇALIŞIYOR".
- Kiracı dışı satırlar SQL ile silindi: `kiraci_davetleri` 1 satır (`01M3VTWR7H0FQ8RKK6TA63XMVC`) ve SİSTEM `denetim_kayitlari` 1 satır (davet). `row_count` 1/1.
- **tenants:** tur 2 başında 13, sonda 14. Fark **bu paketin değil**: `PAKETTOPLU-2-TEST …` kiracısı 13:37:28'de başka şerit (TOPLU-2) tarafından açıldı. Bu paketin kiracısı `count=0`. QA-TEST-* kiracılarına dokunulmadı.
- `fikstur.json` ve `sis.anahtar` `printf ''` ile boşaltıldı (0 bayt). Anahtar `openssl rand` ile üretildi, hiçbir çıktıya basılmadı.
- Proje ağacına mutasyon artığı bırakılmadı. Yedekler `/private/tmp/kuyumcu-serit/mut-yedek/RAPOR-YASLANDIRMA/` altında. Ağaçta görülen `Kuyumcu.Tests/{AramaHttpTestleri,MadenDefteriTestleri,P21CiroRucuTestleri}.cs.bak` dosyaları bu paketin değil: bu paket o dosyalara dokunmadı.

### 0.8 Değişen dosyalar (tur 2)
`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısında bu paketinkiler (tur 1 + tur 2):
```
 M dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs                  (tur 1)
 M dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs                (tur 1 + tur 2: yaşlandırma projeksiyonu)
 M dotnet/Kuyumcu.Web/Pages/Raporlar.razor                        (tur 1 + tur 2: yaşlandırma tablosunda 3 title)
?? dotnet/Kuyumcu.Api/Common/DovizBirimleri.cs                    (tur 1)
?? dotnet/Kuyumcu.Api/Modules/Rapor/CariYaslandirmaKurali.cs      (tur 1 + tur 2: AcilisDahil, BelirsizNedeni)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.RaporYaslandirma.cs         (tur 1 + tur 2: 3 yeni anahtar, not genelleştirildi)
?? dotnet/Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs      (tur 1)
?? dotnet/Kuyumcu.Tests/RaporYaslandirmaTestleri.cs               (tur 1 + tur 2: +7 test)
```
`Kuyumcu.Web/Pages/Cariler.razor` (M) bu paketin değil.

### 0.9 Koşul karşılama (tur 2 sonu)
| Koşul | Durum | Kanıt |
|---|---|---|
| ACILIS → ayrı BELIRSIZ dilimi (rapor, satır, ekran aynı kural) | KARŞILANDI | RY-1, RY-9, RY-10 · canlı + SQL birebir · ekran |
| Açılış geri alınınca kalan yeni borç 0-30'da, açılış rozeti yok (hasım KRİTİK) | KARŞILANDI | hasım testi yeşil · RY-9 · M-RY-8 · canlı E satırı |
| Karma döviz açılışı birim bazında netlenir (hasım ORTA) | KARŞILANDI | hasım testi yeşil · RY-10 · M-RY-9 |
| Tarihsiz notu ve ipucu iki nedeni de anlatır (hasım DÜŞÜK) | KARŞILANDI | RY-12 · M-RY-11 · ekranda title/not |
| 10/40/100 günlük veresiye kendi diliminde, SQL ile aynı | KARŞILANDI | § 0.5 |
| Sabit döviz listeleri tek kaynağa; GBP rapor ve listede görünür | KARŞILANDI | `ozetDoviz.GBP=100`, `bagli-kayitlar dovizBakiyeVar=true` · M-RY-2 |
| Mutasyonlar: ACILIS 0-30'a geri / GBP çıkar → kırmızı | KARŞILANDI | M-RY-1 (6 kırmızı) · M-RY-2 (4 kırmızı) |
| Başsız Chrome 1280 + 375 · BASİT + PRO · açık + koyu | KARŞILANDI | § 0.6 (8 ölçüm, hepsi 0) |
| build 0/0 | KARŞILANDI | 0 Uyarı 0 Hata |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | yalnız P-30 ✗ |
| Tam süit 0 yeni kırmızı | KARŞILANDI | 2 kırmızı, ikisi de SUBE-KAPSAM-B'nin (tur 1'de de vardı), fark 0 |
| Dışa aktarım aynı | UYGULANAMAZ | yaşlandırma dışa aktarım ucu yok (tur 1 S-2) |
| Test kiracısı silindi | KARŞILANDI | kendi kiracım 0. tenants 13→14 farkı TOPLU-2'nin kiracısından |

### 0.10 Açık kalanlar (tur 2'de eklenen)
1. **Raporlar.razor eşzamanlı dokunuş (MÜHÜR 2).** t1 (SUBE-KAPSAM-B, şube parametresi ve Mutabakat) ve t2 (bu paket, yaşlandırma bölümü) aynı dalgada bu dosyaya dokundu. Kapı kaydına gerekçe yazılması orkestratörün işi.
2. **SUBE-KAPSAM-B kırmızıları:** `SozlesmeKapsamTestleri` ×2 (`Mutabakat` kilitsiz tip). Sahibi t1.
3. **Arka plan işi hatası (bu paketin dışında):** Canlı API günlüğünde açılışta `is_calismalari` ve `denetim_kayitlari` INSERT'i `DbUpdateException` veriyor (zamanlanmış iş). Yaşlandırma yolunu etkilemiyor, ama Anayasa md.1 gereği burada not ediliyor. Sahibi: zamanlanmış işler / Batuhan.
4. Tur 1 açık kalanları (§ 13) aynen geçerli: FIFO yaşlandırma, cari liste/detay `usd/eur/gbpBakiye` alanları, dışa aktarım ucu.

---

# TUR 1 (tarihçe)

## 1. Yapılanlar

### Madde 1: Açılış bakiyesi artık 0-30 gün diliminde değil, ayrı "Tarihsiz" (BELIRSIZ) diliminde
- **Sorun (koda karşı doğrulandı):** `RaporModulu.CariYaslandirma`, dilimi `cari_bakiyeler.son_islem_tarihi` alanına göre belirliyordu. `CariServisi.AcilisYaz → DuzeltmeYaz` bu alana `DateTime.UtcNow` yazıyor (`CariBakiyeServisi.Uygula`, `b.SonIslemTarihi = now`). ACILIS hareketinin `tarih` alanı da aynı `now` değeri. Sonuç: önceki programdan devralınan yaşlı borç, açılış günü "0-30 gün" diliminde görünüyordu. Spesifikasyondaki "şu an 0-30" iddiası hâlâ geçerliydi.
- **Kural (tek yer: `Modules/Rapor/CariYaslandirmaKurali.cs`):** Carinin bakiyesine son dokunan yazım bir `ACILIS` hareketiyse yaşlanma tarihi **bilinmiyor** sayılır ve dilim `BELIRSIZ` olur. SQL'de bu şöyle hesaplanır: `YasTarihi = (MAX(ACILIS.tarih) ?? MinValue) >= son_islem_tarihi ? NULL : son_islem_tarihi`. Özet (`ozetTl`, 5 SQL toplamı), satır dilimi ve ekran aynı `YasTarihi` alanından beslenir.
- **Yanıta eklenen alanlar** (mevcut alanlar korundu, yanıt şekli bozulmadı):
  - `belirsizNedeni`: `"ACILIS"` ya da `"TARIH_YOK"`.
  - `acilisDahil`: açılış devrinin net tutarı (TL, has ya da döviz) hâlâ sıfırdan farklıysa `true`.
  - `dovizBakiyeleri`
  - `ozetDoviz`
- **Ekran (`Raporlar.razor`, cari yaşlandırma bölümü):**
  - "Tarihsiz" sütunu **yalnız** böyle bir satır varsa çizilir (zorunlu önce kuralı). Bu sütun 90+ ile Toplam arasında durur ve alt toplamı vardır.
  - Satırda "Açılış bakiyesi" rozeti çıkar; ipucunda gerekçe yazar.
  - Açılıştan sonra işlem görmüş carinin satırında "Açılış bakiyesi de var" rozeti çıkar.
  - Tablo altında "Tarihsiz sütunu …" notu durur.
  - Ham kod (`ACILIS`/`BELIRSIZ`) ekrana basılmaz. bUnit testi bunu doğrular.
- **Dışa aktarım:** Cari yaşlandırmanın CSV/PDF dışa aktarım yolu **yok**. Koda bakıldı: `RaporKatalogu R03` yalnız ekran ucunu taşıyor, `KartListe`'nin aktarım düğmesi yok. "Rapor + ekran + dışa aktarım aynı" koşulunun karşılanabilen kısmı rapor + ekrandır. Bu bir sapma olarak § 2'de yazılı.

### Madde 2: Sabit döviz listeleri tek kaynağa bağlandı
- Yeni dosya `Common/DovizBirimleri.cs`. Liste YASAL-3 kalıbıyla türetilir: `YaptirimServisi.BakiyeKolonlari` içinden TL ve has gram dışında kalan her `…Bakiye` kolonu alınır. Para birimi kodu kolon adından gelir (`GbpBakiye` → `GBP`). Elle yazılmış para birimi listesi yok. Sağladığı yardımcılar:
  - `Kolonlar`, `Kodlar`
  - `BakiyeVar(b)`
  - `Bakiyeler(b)`
  - SQL'e çevrilen `AcikIfadesi(...)`
  - `ToplamSecici(...)`
- `CariModulu.cs` (eski 896-898; pasife alma ön koşulu `BagliKayitlar`): elle yazılmış USD/EUR/GBP koşulu yerine `DovizBirimleri.BakiyeVar(bakiye)` kullanılıyor.
- `RaporModulu.cs` (eski 737-739 süzgeci ve 797 `dovizBakiyesiVar`):
  - Süzgeç artık `AcikSuzgeci()` ifade ağacıyla kuruluyor (TL ve has elle, döviz kolonları `DovizBirimleri`'nden).
  - `dovizBakiyesiVar = dovizBakiyeleri.Count > 0`.
  - Yeni `ozetDoviz` alanı tüm carilerin her birimdeki toplamını sunucuda hesaplar.
- **Tutarlılık kapısı:** `DovizBirimleri.Kodlar` ile yazıcının kapalı kümesi `DovizBakiye.ParaBirimleri` aynı olmak zorunda (saf test). Bakiye satırına kolon eklenip yazıcıya eklenmezse ya da tersi olursa test kırmızıya döner.
- **Ekran:**
  - Döviz bakiyeli satırda tutar ve birim rozeti cari adının yanında görünür (`100,00 £`). Birim sunucudan gelir; istemcide sabit liste yok.
  - Tablo altında `#yas-doviz-ozet` satırı var: "Döviz bakiyeleri (güne bölünmez, TL toplamına eklenmez): 100,00 £". Bu satır sunucunun `ozetDoviz` değerinden yalnız sıfır olmayan birimleri yazar (UX-S32).
- `usdBakiye`, `eurBakiye` ve `gbpBakiye` yanıt alanları yanıt şekli uyumu için duruyor. Tek kaynak `dovizBakiyeleri`.

## 2. Sapmalar ve gerekçeleri
| # | Spesifikasyon | Uygulanan | Gerekçe |
|---|---|---|---|
| S-1 | "Açılış → BELIRSIZ" | Bakiyeye **son** dokunan yazım ACILIS ise BELIRSIZ. Açılıştan sonra işlem gören cari, mevcut "son işlem tarihi" modeliyle dilimlenir ve `acilisDahil` rozeti taşır. | Bakiyeyi FIFO ile açılış payı ve yeni pay diye bölmek defterin tamamını yürümeyi gerektirir. Ayrıca mevcut yaşlandırma modeli satır başına tek dilim kullanıyor. Payı uydurmak yerine yaşlı pay rozetle söyleniyor. Vade bazlı FIFO yaşlandırma ayrı bir karar (§ 9). |
| S-2 | "rapor + ekran + dışa aktarım aynı" | Rapor ucu ve ekran aynı kuraldan besleniyor. | Cari yaşlandırmanın dışa aktarım yolu yok (koda bakıldı). Yeni bir dışa aktarım yazmak kapsam dışı. |
| S-3 | Sütun başlığı | "Tarihsiz" (EN: "Undated"). Açıklama ipucu ve tablo altı notunda. | İlk deneme "Ne zamandan beri bilinmiyor" başlığıydı. 1280 px'te tabloyu kart dışına taşırdı: Toplam ve Has sütunları kırpılıyordu (ekran görüntüsüyle görüldü). Başlık kısaltıldı, açıklama nota taşındı. |
| S-4 | Döviz rozeti | Rozet "Döviz bakiyesi var (vadelendirilmedi)" yerine tutarı basıyor ve Toplam hücresinden cari adı hücresine taşındı. Açıklama ipucu ve tablo altı notunda. | Uzun rozet Toplam sütununu genişletip tabloyu taşırıyordu (ölçüldü). |
| S-5 | `YaslandirmaYaniti.OzetTl` | İstemci sözleşmesine **bağlanmadı**. Yalnız `OzetDoviz` eklendi. | Tam süit `UX1C3EkranTestleri` (7 test) kırmızıya döndü: eski sahte yanıtlar `ozetTl:0` taşıyor ve okunmayan alan sayfayı düşürüyordu. Ekran ozetTl okumuyor. Düzeltmeden sonra 7/7 yeşil. |
| S-6 | Dosya alanı | `Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs`'e iki sözleşmeye isteğe bağlı alanlar eklendi. | Ekranın yeni alanları okuyabilmesi için zorunluydu. Değişiklik yalnız ekleme ve varsayılanlı. Başka şeridin bildirilen alanında değil. |

## 3. Migration
Gerekmedi. Şema değişmedi.

## 4. Testler
Yeni dosya `Kuyumcu.Tests/RaporYaslandirmaTestleri.cs`: **16 yeni test vakası**.
- **DB (4):**
  - `Acilis_bakiyesi_BELIRSIZ_dilimde_veresiyeler_gun_dilimlerinde_ozet_satirlarla_ayni`
  - `Acilistan_sonra_islem_goren_cari_tarihli_dilimde_ve_acilisDahil_bayragi_tasir`
  - `Yalniz_GBP_bakiyeli_cari_yaslandirmada_gorunur_ve_ozetDoviz_GBP_toplar`
  - `Bagli_kayitlar_yalniz_GBP_bakiyesini_doviz_bakiyesi_sayar`
- **Saf (9):**
  - `Doviz_birimleri_kolonlardan_turer_ve_yazici_kumesiyle_ayni`
  - `Dilim_sinirlari` × 8 sınır değeri (null, 0, 30, 31, 60, 61, 90, 91)
- **bUnit (3):**
  - `Acilis_satiri_ayri_sutunda_0_30_da_degil`
  - `GBP_bakiyesi_satirda_ve_doviz_ozetinde_gorunur`
  - `Belirsiz_satir_yoksa_sutun_cizilmez`

Filtreli koşumlar:
- `RaporYaslandirma` + `RaporlarEkran` + `CariDovizBakiye` + `CariBakiyeTestleri` + `SozlesmeSekil` + `P57RaporBicim` + `Yasal3` + `RaporSeti`: **130/130 yeşil**.
- Son kodla `UX1C3` + `RaporYaslandirma` + `RaporlarEkran` + `SozlesmeSekil` + `S33_14`: **78/78 yeşil**.

Tam süit sonuçları § 7'de.

## 5. Mutasyon kanıtları
Yöntem `/private/tmp/kuyumcu-serit/RAPOR-YASLANDIRMA/mut.zsh`:
1. Kaynak `mut-yedek/RAPOR-YASLANDIRMA/`'ya kopyalanır.
2. `perl -0777 -pi` ile tek eşleşmeli değişiklik yapılır.
3. Build alınır (her mutasyon build'i 0 hata; log `mut-<kod>-b.log`).
4. Filtreli test koşulur.
5. Yedek geri kopyalanır.
6. `cmp` ile birebir aynılık doğrulanır.

Son mutasyondan sonra temiz build alındı: 0 hata.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma |
|---|---|---|---|
| M-RY-1 | ACILIS'i 0-30'a geri koy (`YasTarihi` her zaman `SonIslemTarihi`) | `Acilis_bakiyesi_BELIRSIZ_dilimde_veresiyeler_gun_dilimlerinde_ozet_satirlarla_ayni` | cmp AYNI |
| M-RY-2 | GBP'yi kolon listesinden çıkar (`&& p.Name != "GbpBakiye"`) | `Doviz_birimleri_kolonlardan_turer_ve_yazici_kumesiyle_ayni`, `Bagli_kayitlar_yalniz_GBP_…`, `Yalniz_GBP_bakiyeli_cari_…` (3) | cmp AYNI |
| M-RY-3 | CariModulu eski elle listeye döner (yalnız USD/EUR) | `Bagli_kayitlar_yalniz_GBP_bakiyesini_doviz_bakiyesi_sayar` | cmp AYNI |
| M-RY-4 | Ekran BELIRSIZ satırını 0-30 sütununa yazar | `Acilis_satiri_ayri_sutunda_0_30_da_degil` | cmp AYNI |
| M-RY-5 | Yaşlandırma süzgecinden döviz parçası düşer | `CariDovizBakiyeTestleri.Doviz_bakiyeli_cari_yaslandirmada_GORUNUR_…`, `Yalniz_GBP_bakiyeli_cari_…` (2) | cmp AYNI |
| M-RY-6 | `acilisDahil` bayrağı hep false | `Acilis_bakiyesi_BELIRSIZ_…`, `Acilistan_sonra_islem_goren_…` (2) | cmp AYNI |
| M-RY-7 | "Tarihsiz" sütunu her zaman çizilir | `RaporlarEkranTestleri.P47_01_…` (7 başlık), `Belirsiz_satir_yoksa_sutun_cizilmez` (2) | cmp AYNI |

**Not:** M-RY-7'nin ilk denemesi geçersiz sayıldı ve tekrarlandı. O sırada SUBE-KAPSAM-B/TOPLU-2 şeridinin yarım değişikliği yüzünden derleme kırmızıydı (`Toplu2Sozlugu` yok), bu yüzden test eski DLL ile koştu. Derleme yeşile dönünce mutasyon yeniden koşuldu. Tablodaki sonuç yeniden koşumun sonucudur (build log 0 hata).

## 6. Denetim
`bash denetim.sh` sonucunda yalnız bir madde ✗: `P-30 DEMO_INSAN_SURESI`. Bu madde paket dışıdır ve görevde hariç tutuldu. Diğer bütün sayaçlar ✓; bu paketi ilgilendirenler:
- `IgnoreQueryFilters` 0
- `Hareketler.Add` 1 (değişmedi)
- `DOVIZ_ turu yazan dosya` 4 (değişmedi)
- `Pages: ModalPencere` 48/55 (artış yok)

## 7. Tam süit
Tur 1, ilk koşum (`tam.trx`): **8897 test · 8887 geçti · 8 başarısız · 2 koşmadı**.
- `UX1C3EkranTestleri` ×7 kırmızısı **bu paketindi** (S-5). Düzeltildi; filtreli koşumda 7/7 yeşil.
- `S33UrunlerEkranTestleri.S33_14` (Ürünler ekranı; bu paket dokunmadı) filtreli koşumda yeşil geçti. Başka şeridin ara durumu olduğu değerlendirildi.

Son kodla tam süit `tam2.trx`, § 7a'da.

### 7a. Son koşum (son kodla, `tam2.trx`)
**8910 test · 8908 koştu · 8896 geçti · 12 başarısız · 2 koşmadı.** Toplam test sayısı 8897'den 8910'a çıktı, çünkü SUBE-KAPSAM-B şeridi koşum sırasında yeni test dosyası ekledi (`GorunumSubesiEkranTestleri.cs`, 15:47).

**Bu paketin ürettiği kırmızı: 0.** UX1C3 ×7 bu koşumda yeşil. 12 kırmızının hepsi başka şeritlerin; sahipleri ve kanıtları aşağıda:

| Kırmızı | Sahibi | Kanıt |
|---|---|---|
| `GorunumSubesiEkranTestleri.Subeli_ekran_cagrisi_…` ×9 (PosTahsilatlar ×2, Raporlar ×4, KarZarar ×2, Hurda ×1) | SUBE-KAPSAM-B (t1) | Test dosyası koşum sırasında eklendi (15:47). Raporlar örneğinde NRE `Raporlar.razor:691`, Gün sonu (mutabakat) bölümünde; o bölüme `SubeliSorgu` ile t1 dokunuyor (satır 868-873). |
| `Toplu1SafTestleri.TARIH_TEK_…` | SUBE-KAPSAM-B (t1) | İhlal `Raporlar.razor:873`: `mutabakatGunu.ToString("yyyy-MM-dd", …)` satırı. Bu satır benim başlangıç kopyamda yok (`orijinal/Raporlar.razor` grep 0). t1 bunu `SubeliSorgu.Ekle(...)` içine yazdı. |
| `SozlesmeKapsamTestleri.Kilitsiz_tip_listesi_circir_ile_donduruldu` + `Kapsam_orani_beyan_edilen_degerle_ayni` (58→59) | SUBE-KAPSAM-B (t1) | Yeni kilitsiz istemci tipi `Mutabakat` (`UyumServisi.cs:372`). t1 bunu `Api.GetAsync<Mutabakat>` ile Raporlar.razor'a bağladı. Bu paket yeni tip eklemedi; yalnız mevcut iki sözleşmeye alan ekledi. |

## 8. Canlı doğrulama (API 5162, kiracı `PAKETRAPOR-YASLANDIRMA-TEST Altın Evi`)
Betikler `/private/tmp/kuyumcu-serit/RAPOR-YASLANDIRMA/canli/{canli.mjs,acilis.mjs}`.

| Adım | Beklenen | Gerçek |
|---|---|---|
| Davet + kayıt + giriş | 200 | 200 · tenantId `01M3VQ57BS7MNE03V61VFE9KB3` |
| 4 cari + 1 GBP carisi (`POST /cari`) | 200 | 200 ×5 |
| B/C/D `POST /cari/{id}/duzeltme` +1000/+2000/+3000 TL | 200 | 200 ×3 |
| G `POST /cari/{id}/doviz-veresiye` GBP 100 | 200 | 200 |
| A açılış `POST /kurulum/acilis/prova` + `/uygula` (CARI, 5000 TL) | 200/200 | prova 200 `temiz:true` · uygula 200 `uygulananSatir:1, tlToplam:5000` |
| B/C/D 10/40/100 gün geriye (SQL `UPDATE cari_bakiyeler.son_islem_tarihi` + `hareketler.tarih`) | — | uygulandı |
| `GET /raporlar/cari-yaslandirma` | 200; A BELIRSIZ, B 0-30, C 31-60, D 90+ | **200** · `ozetTl {"0-30":1000,"31-60":2000,"61-90":0,"90+":3000,"BELIRSIZ":5000}` · `ozetDoviz {"USD":0,"EUR":0,"GBP":100}` · toplamSatir 5 · A `BELIRSIZ gun=null neden=ACILIS acilisDahil=true` · D `90+ gun=100` · C `31-60 gun=40` · B `0-30 gun=10` · G `0-30 doviz={"GBP":100}` |
| `GET /cari/{G}/bagli-kayitlar` | dovizBakiyeVar true | **200** `dovizBakiyeVar=true` |

**SQL kanıtı.** Kural bağımsız SQL ile yeniden hesaplandı (`kuyumcu_net`; açılış son tarihi ≥ son işlem tarihi → BELIRSIZ, değilse `floor(gün)` dilimi). Sonuç API ile birebir aynı:

| dilim | tl | n |
|---|---|---|
| 0-30 | 1000.00 | 2 (B + yalnız GBP'li G) |
| 31-60 | 2000.00 | 1 |
| 90+ | 3000.00 | 1 |
| BELIRSIZ | 5000.00 | 1 |

Döviz toplamı: `usd 0.00 · eur 0.00 · gbp 100.00`. Açılış öncesi ham satırlar:
- A: `son_islem_tarihi = acilis_son = 12:31:47.059409`, türler `ACILIS`
- B/C/D: `CARI_DUZELTME`
- G: `DOVIZ_VERESIYE`

### 8a. Gerçek tarayıcı (başsız Chrome, CDP 9907; vekil 5163; ölçer `betikler/sikisma-olc.js`)
Rota `/raporlar?sekme=cari`. Görüntüler `/private/tmp/kuyumcu-serit/RAPOR-YASLANDIRMA/canli/foto/` altında; her biri Read ile görüldü.

| Mod | Tema | px | Tablo | K1 | K2 (%) | K3 taşan/kırık | K5 TRY | K7 | K8 | K9 | K10 | K11 | K12 | Yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| PRO | açık | 1280 | var | 0 | 98 | 0/1* | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| PRO | açık | 375 | var | 0 | 97 | 0/1* | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| PRO | koyu | 1280 | var | 0 | 98 | 0/1* | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| PRO | koyu | 375 | var | 0 | 97 | 0/1* | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| BASİT | açık | 1280 | — (Basit yüz "Bugün") | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| BASİT | açık | 375 | — | 0 | 97 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| BASİT | koyu | 1280 | — | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| BASİT | koyu | 375 | — | 0 | 97 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

PRO ekranında görülen metinler:
- başlık "TARİHSİZ"
- satır rozeti "Açılış bakiyesi"
- Tarihsiz alt toplamı "5.000 ₺"
- satır döviz rozeti "100,00 £"
- döviz özeti "Döviz bakiyeleri (güne bölünmez, TL toplamına eklenmez): 100,00 £"

Basit modda Raporlar ekranı S3-4 "Bugün" yüzünü gösterir. Cari yaşlandırma bu yüzde yok (tasarım gereği, bu paket değiştirmedi). Ölçüm, Basit yüzün değişmediğini kanıtlıyor.

\* **K3 "kırık" = ölçer yapaylığı (gerekçe):** Ölçer tek öğe olarak `td.hucre-vurgu "Gül Sterlinli100,00 £"` hücresini gösteriyor. Hücredeki metin düğümlerinin üst kenarları CDP ile ölçüldü:
- 1280 px: ad `top=638 h=19`, rozet `top=640 h=17`
- 375 px: ad `top=913`, rozet `top=916`

İkisi aynı satırda. Fark, rozetin küçük yazı ve dolgu yüzünden 2-3 px aşağıda başlamasından geliyor. Ölçer `round(top/4)` ile iki ayrı satır sayıyor. Ekran görüntülerinde ad ve rozet tek satırda (`cari__PRO_*_1280-tablo.png`, `cari__PRO_karanlik_375-tablo.png`). Aynı kalıp, mevcut "Has bakiyesi var" rozetinde de var.

375 px'te tablo, `KartListe` sarmalayıcısı içinde kaydırılır (P-47 deseni, `#yas-sar`). Sayfa düzeyinde yatay kaydırma 0.

### 8b. Temizlik
- API (`kill` PID) ve vekil (`kill` PID) durduruldu. 5162 ve 5163 LISTEN boş.
- `zsh kiraci-temizle.sh 01M3VQ57BS7MNE03V61VFE9KB3 --uygula` çalıştırıldı. İkinci koşum "Kiracı YOK" dedi.
- Kiracı dışı satırlar SQL ile silindi: `kiraci_davetleri` 1 satır ve SİSTEM `denetim_kayitlari` 1 satır (davet üretimi).
- **tenants: başlangıç 13 → son 13.** QA-TEST-* kiracılarına dokunulmadı.
- `fikstur.json` ve `sis.anahtar` `printf ''` ile boşaltıldı (0 bayt). Sistem anahtarı `openssl rand` ile üretildi, hiçbir çıktıya basılmadı.

## 9. DoD sözleşmesi
Yeni varlık ya da kayıt türü **yok**. Paket rapor okuma yolunu ve iki salt okunur ölçüyü değiştiriyor. Düzenleme yolları aynen geçerli:
- Açılış bakiyesi: `POST /kurulum/acilis/geri-al` (ters kayıt, mühürden önce) ya da `POST /cari/{id}/duzeltme`. Hareketler finansal kayıttır; hard DELETE yok.
- Yaşlandırma ekranı salt okunur rapor; "Raporda düzenleme yoktur" gerekçesi mevcut (C3_45).
- Yeni düğme yok. Yeni form yok.

## 10. Dokunulan sayfalar
- `Kuyumcu.Web/Pages/Raporlar.razor`: yalnız cari yaşlandırma bölümü (`rpKod == "cari"`), `DigerleriniYukle` içinde tek satır, ve `GorunenYaslananlar` üstüne eklenen yardımcılar. SUBE-KAPSAM-B'nin bu dosyadaki stok çağrısı alanına dokunulmadı.

## 11. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketinkiler)
```
 M dotnet/Kuyumcu.Api/Modules/Cari/CariModulu.cs
 M dotnet/Kuyumcu.Api/Modules/Rapor/RaporModulu.cs
 M dotnet/Kuyumcu.Web/Pages/Raporlar.razor
?? dotnet/Kuyumcu.Api/Common/DovizBirimleri.cs
?? dotnet/Kuyumcu.Api/Modules/Rapor/CariYaslandirmaKurali.cs
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.RaporYaslandirma.cs
?? dotnet/Kuyumcu.Contracts/Sayfalar/RaporlarSozlesmeleri.cs   (izlenmeyen dosya; yalnız ekleme)
?? dotnet/Kuyumcu.Tests/RaporYaslandirmaTestleri.cs
```

## 12. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| ACILIS → ayrı BELIRSIZ dilimi (rapor + ekran) | KARŞILANDI | DB testi RY-1 · bUnit RY-6 · canlı tablo + SQL birebir |
| 10/40/100 günlük veresiye kendi diliminde, SQL ile aynı | KARŞILANDI | § 8 tablo |
| Sabit döviz listeleri tek kaynağa (CariModulu 896-898, RaporModulu 737-739/797) | KARŞILANDI | `DovizBirimleri` · M-RY-2/3/5 |
| GBP bakiyeli cari rapor ve liste toplamında görünür | KARŞILANDI | `ozetDoviz.GBP=100`, satır `dovizBakiyeleri.GBP=100`, ekranda "100,00 £" |
| Mutasyon: ACILIS 0-30'a geri → kırmızı | KARŞILANDI | M-RY-1 |
| Mutasyon: GBP listeden çıkar → kırmızı | KARŞILANDI | M-RY-2 (3 kırmızı) |
| Başsız Chrome 1280 + 375 · BASİT + PRO · açık + koyu | KARŞILANDI | § 8a (K3 = ölçer yapaylığı, piksel kanıtlı) |
| build 0 hata | KARŞILANDI | 0 Hata. 2 uyarı başka şeritlerin test dosyalarında: `SUBEKAPSAMASinirTestleri.cs:49`, `KesifP2Testleri.cs:454` |
| denetim TEMİZ (P-30 hariç) | KARŞILANDI | yalnız P-30 ✗ |
| Tam süit 0 yeni kırmızı | KARŞILANDI | § 7a: 12 kırmızının hepsi SUBE-KAPSAM-B'nin; bu paketin UX1C3 ×7 kırmızısı düzeltildi |
| Dışa aktarımla aynı | UYGULANAMAZ | uç yok (S-2) |
| Test kiracısı silindi, tenants başlangıç değerinde | KARŞILANDI | 13 → 13 |

## 13. Açık kalanlar
1. **Vade bazlı (FIFO) yaşlandırma:** Açılıştan sonra işlem gören carinin yaşlı açılış payı tarihli dilimde kalıyor ve rozetle söyleniyor (S-1). Bakiyeyi pay pay dilimlere bölmek ayrı bir karar. Sahibi: Rapor.
2. **Cari listesi ve detay yanıtları** (`CariModulu.cs:613-615` ve `:1716-1718`) hâlâ `usdBakiye/eurBakiye/gbpBakiye` alanlarını sabit olarak dönüyor. Bugün GBP dahil, yani kaçak yok; yeni bir birimde istemci alanı da gerekir. Bu satırlar bu paketin dosya alanı (896-898) dışında. Sahibi: Cari.
3. **`sikisma-olc.js` K3 "kırık" ölçümü** rozetli kısa hücrede 2-3 px'lik taban kaymasını ayrı satır sayıyor (§ 8a). Ölçere tolerans eklemek orkestratör aracının işi.
4. **Cari yaşlandırma dışa aktarımı** yok (S-2).

## 14. Kurucu / mali müşavir
Mevzuat hükmü içermez. Yaşlandırma bir iç yönetim raporu; eşik, oran ya da yasal süre koda yazılmadı.
