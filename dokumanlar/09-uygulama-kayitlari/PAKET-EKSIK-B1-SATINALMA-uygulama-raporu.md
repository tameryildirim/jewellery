# PAKET EKSIK-B1-SATINALMA — Uygulama raporu

**Tarih:** 10.10.2026 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Dalga:** MASAK-ESZAMAN-2 + KAPORA-EKRAN + EKSIK-B1-SATINALMA (t5 · 5166/5167)
**Kaynak:** `EKSIK-OZELLIK-DENETIMI-2026-10-02.md` (B) B1 satır 1 ve 2, (F) madde 3.
**Durum: BİTTİ (tur 3).** Doğrulayıcının KRİTİK bulgusu (eşik üstü mal kabul çıkmaz sokağı) ve üç DÜŞÜK bulgusu kapatıldı; ayrıntı § T3. Tur 1–2 bölümleri (§ 0–13) tarihsel kayıttır; çelişen yerde § T3 geçerlidir.

## T3. Tur 3 (10.10.2026 öğleden sonra): doğrulayıcı bulgularının kapatılması

### T3.1 Bulgular ve karşılıkları
| Bulgu | Düzey | Ne yapıldı | Kanıt |
|---|---|---|---|
| Eşik üstü mal kabul yapılamıyor: `MalKabulDto` MASAK taşımıyordu, `satis.Alis`'e beyan geçmiyordu, çekmecede form yoktu | KRİTİK | `MalKabulDto.Masak` (`MasakBilgiDto?`) eklendi ve her kalemin `Alis` çağrısına `Masak = dto.Masak` ile aynen geçiriliyor. Kural motoruna dokunulmadı. Çekmeceye ortak `MasakFormu` eklendi (Banka/POS ile aynı bileşen ve uçlar): tutar = girilen adet × siparişteki birim fiyat; gereksinim tedarikçi kimliğiyle `/masak/gereksinim`'den sorulur (bugünkü bağlantılı işlemler toplamı); form yalnız sunucu kimlik isteyince açılır; eksikken "Malı teslim al" kilitli ve sebep yazılı | Doğrulayıcı testi `Sinir_cok_buyuk_tutar_999_999_999_99_masak_beyaniyla_mal_kabul_yapilabilir` artık YEŞİL. Yeni testler EB10 (HTTP) ve EK09 (bUnit). Canlı 5166 + SQL § T3.4. Chrome § T3.6 |
| Yanlış yorum "ödeme yapılmadığı için kapı tetiklenmez" + `OdemeTuru = "NAKIT"` | KRİTİK'in parçası | Yorum düzeltildi. Tevsik kapısı yalnız kasadan çıkan nakde bakar (ödenen 0 olduğu için tetiklenmez). MASAK kapısı ödeme şekline bakmaz. `NAKIT` yalnız alanın fail-closed varsayılanıdır; sözleşmede veresiye değeri yok. Canlı SQL gösterdi: mal kabul hareketlerinde `nakit_tutar = 0`, `veresiye_tutar` tutarın tamamı, `kasa_hareketleri` 0 satır. Yani anlam zaten cari borcudur | § T3.4 SQL |
| "Ödeme yoksa nakit işlem beyan formu istenmemeli" önerisi | KRİTİK'in parçası | **UYGULANMADI, gerekçeli.** Birincil kaynak (aşağıda): kimlik tespiti tutara bağlıdır, ödeme şekline değil. "Fon kaynağı / beyan formu" kademesi yönetmelikte geçmiyor; projenin TEYİTSİZ iç kademesi. Bu kademeyi ödeme şekline göre gevşetmek tahminle mevzuat kuralı yazmak olurdu. Bugünkü davranış birebir korundu (fail-closed). Mali müşavir sorusu § T3.9'da | — |
| Çıktı giriş metni irsaliyeyi saymıyor | DÜŞÜK | `Sozlukler.cs` `Cikti.Aciklama` TR+EN: "… cari ekstresi ve irsaliye dökümü …" / "… customer statement and waybill printout …". `Cikti.razor` değişmedi. YAZDIR şeridine not: yalnız sözlük metni değişti | `grep` 2 eşleşme; P17Cikti testleri yeşil (70/70 filtre) |
| İrsaliye kâğıdı ve sunucu iletileri EN'de de Türkçe | DÜŞÜK | **Kalıp korundu, bilinçli.** Diğer belge türleri gibi (`CiktiModulu.BelgeAdi`) irsaliye dökümü yasal kâğıt olarak Türkçe basılır: 213 s. VUK belgeleri Türkçe düzenlenir ve dökümün altında VUK md.230/5 atfı var. Canlı: `Accept-Language: en` → `belgeAdi = "MAL KABUL İRSALİYESİ"` | § T3.4 |
| "build 0 Uyarı" iddiası artımlı derlemeye aitti | DÜŞÜK | Düzeltildi (§ T3.8) | — |

### T3.2 Mevzuat (birincil kaynak)
- **Kaynak:** Suç Gelirlerinin Aklanmasının ve Terörün Finansmanının Önlenmesine Dair Tedbirler Hakkında Yönetmelik, konsolide metin. https://www.mevzuat.gov.tr/MevzuatMetin/21.5.200713012.pdf. 10.10.2026'da indirildi ve metne çevrildi: `/private/tmp/kuyumcu-serit/pEKSIK-B1-SATINALMA/t3/ted.txt` satır 176–191.
- **md.5/1-b:** kimlik tespiti "İşlem tutarı ya da birbiriyle bağlantılı birden fazla işlemin toplam tutarı" 370.000 TL veya üzerinde olduğunda gerekir. Ödeme şekline (nakit, veresiye) bağlı bir koşul yok. Bu yüzden toptan alışın mal kabulü de kapsamdadır ve mal kabul ucu beyanı taşımak zorundadır.
- **md.5/2:** "Kimlik tespiti, iş ilişkisi tesisinden veya işlem yapılmadan önce tamamlanır." Bu yüzden form, kabul anında ve kabulden önce doldurulur; sonradan girilmez.
- **"Nakit işlem beyan formu / fon kaynağı":** metinde geçmiyor (`grep` 0). Projenin `beyanFormuEsigi` (canlıda 2.000.000,01 TL) ve `amacBeyaniEsigi` (200.000 TL) kademeleri yapılandırmadan okunur ve TEYİTSİZ'dir. Bu pakette DEĞİŞMEDİ.

### T3.3 Değişen dosyalar (tur 3)
- `dotnet/Kuyumcu.Api/Modules/Satinalma/SatinalmaModulu.cs`: `MalKabulDto.Masak` eklendi, `Alis` çağrısına `Masak = dto.Masak` geçiriliyor, yorum düzeltildi (diff 15 satır).
- `dotnet/Kuyumcu.Web/Pages/SatinAlma.razor`:
  - MASAK beyanı: `@using …MasakFormu`, `@inject UyumServisi`, `#sa-masak` bloğu, `MasakHazirla`, `MasakGerekli`, `MasakKilidi`, `KabulAdetDegisti`. Gövdeye `masak` eklendi; eşik altında `null`.
  - Kişisel veri: çekmece kapanınca ve başarılı kabulden sonra model sıfırlanır.
  - Kalem özeti 375'te kırılıyordu: her "ad + değer" parçası `hucre-tek-satir` oldu (mevcut sınıf; yeni CSS ve satır içi stil yok).
  - Element kimlikleri değişmedi. Yeni kimlikler: `sa-masak`, `sa-masak-not`.
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.SatinAlma.cs`: 4 yeni anahtar, TR ve EN (`MasakKabulNotu`, `MasakKabulKilit`, `MasakKabulYukleniyor`, `MasakKabulDegismez`).
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.cs`: yalnız `Cikti.Aciklama` metni (TR + EN).
- `dotnet/Kuyumcu.Tests/EksikB1SatinalmaTestleri.cs`: yeni sınıf `EksikB1SatinalmaMasakTestleri`, test EB10.
- `dotnet/Kuyumcu.Tests/EksikB1SatinalmaEkranTestleri.cs`:
  - `UyumServisi` kaydı.
  - Gerçekçi `/masak/esikler` ve `/masak/gereksinim` sahte yanıtları. Değerler canlı 5166 ile aynı: 370.000 / 200.000 / 2.000.000,01.
  - Yeni test EK09.
  - EK03'e "eşik altında gövdede masak null, form yok" iddiası.
- Yedekler: `/private/tmp/kuyumcu-serit/pEKSIK-B1-SATINALMA/orijinal-t3/`. Migration GEREKMEDİ (şema değişmedi).
- Dokunulan sayfa: `SatinAlma.razor`. Aynı paketin kendi sayfası, doğrulayıcı bulgusunun kapanması için ikinci tur; gerekçe bu bölüm. `MasakFormu.razor` değişmedi.

### T3.4 Canlı doğrulama (5166, kiracı `PAKETEKSIK-B1-SATINALMA-TEST` = `01M4JQNFH19QHK9CW7JHXNCJQ5`)
Betik ve çıktı: `/private/tmp/kuyumcu-serit/pEKSIK-B1-SATINALMA/canli3/canli.py`, `canli-sonuc.txt`.
Canlı eşikler: kimlik 370.000 · amaç 200.000 · beyan formu 2.000.000,01 · ek belge 20.000.000,01.
Sipariş: 5 adet × 150.000 TL, onaylı.

SQL sütunları: [irsaliye | irs_kalem | ALIŞ | stok_u1 | masak_kimlik | masak_beyan]

| Adım | Beklenen | Gerçek | SQL |
|---|---|---|---|
| Gereksinim 150.000 (tedarikçisiz) / 450.000 (tedarikçiyle) | YOK / kimlik + amaç | YOK / AMAC_BEYANI, kimlikGerekli true | — |
| 1) 1 adet kabul (150.000), beyansız | 200 | 200 IRS-2026-00001 KISMI_KABUL | 1\|1\|1\|1\|0\|0 |
| Gereksinim 300.000 + tedarikçi (bugün 150.000 var) | ölçü 450.000 | AMAC_BEYANI, olcuTutarTl 450000 | — |
| 2) 2 adet (300.000; bağlantılı toplam 450.000), beyansız (TR ve EN) | 400, 0 satır | 400 "MASAK: 370.000 TL ve üstü işlemde kimlik tespiti zorunludur · İşlem amacı belirtilmelidir · …" | değişmedi |
| Hasım: yalnız kimlik (amaç ve açıklama yok) | 400 | 400 "İşlem amacı belirtilmelidir · … (şu an 0)" | değişmedi |
| Hasım: TCKN 11111111111 | 400 | 400 "TC kimlik numarası geçerli değil" | değişmedi |
| Hasım: ad soyad "1", açıklama 4 karakter | 400 | 400 "En az 2 karakter yazın." | değişmedi |
| Hasım: beyanlı ama fazla adet (9) | 400 | 400 "Kabul adedi sipariş adedini aşıyor …" | değişmedi |
| SQL hasım sonrası | önceki ile aynı | 1\|1\|1\|1.000\|0\|0 = önceki | ✓ |
| 3) 2 adet, beyanlı | 200 | 200 IRS-2026-00002 KISMI_KABUL | 2\|2\|2\|3\|**1**\|**1** |
| 4) kalan 2, beyanlı | 200 KAPANDI | 200 IRS-2026-00003 KAPANDI | 3\|3\|3\|5\|2\|2 |
| MASAK kayıtları | harekete bağlı | `HAREKET,300000.00,1 ; HAREKET,300000.00,1` | — |
| ALIŞ hareketleri [adet, tl_tutari, nakit, veresiye] | ödeme yok, cari borcu | `1, −150000, 0, 150000 ; 2, −300000, 0, 300000 ; 2, −300000, 0, 300000`; `kasa_hareketleri` 0 | — |
| Sipariş | KAPANDI 5/5 | KAPANDI, 5.000/5.000 | — |
| Yazdır IRSALIYE (TR / EN) | 200, fiyat yok | 200 "MAL KABUL İRSALİYESİ", tutar metinde yok; EN de Türkçe başlık (bilinçli, § T3.1) | — |
| TEZGAHTAR beyanlı mal kabul | 403 | 403 | — |

Temizlik:
- API (PID 20967 → 36169 → 50934 sırasıyla, her yeniden derlemede), vekil ve kendi Chrome'larım (CDP 9511–9520) durduruldu. 5166, 5167 ve 9511–9520'de dinleyen yok.
- `zsh kiraci-temizle.sh 01M4JQNFH19QHK9CW7JHXNCJQ5 --uygula` çalıştırıldı; ikinci çağrı "Kiracı YOK (bağlantı sınandı ve ÇALIŞIYOR)" dedi.
- SQL: kiracının hareket, irsaliye, MASAK, sipariş ve stok satırları toplamı 0. `PAKETEKSIK-B1-SATINALMA%` = 0.
- **tenants: başlangıç 23 → son 23.**
- `fikstur*.json` ve `anahtar.txt` `printf ''` ile boşaltıldı. Sistem anahtarı bu koşum için rastgele üretildi ve yalnız dosyadan ortama verildi. Parola ya da anahtar çıktıya yazılmadı.

### T3.5 Testler ve mutasyon
Filtreli koşumlar:
- `EksikB1Satinalma|EKSIKB1SATINALMASinir`: **26/26** (önce 25/26: doğrulayıcı KRİTİK testi kırmızıydı).
- `…|SozlesmeSekil|P37c|P17Cikti`: **70/70**, son kodla.
- `…|BankaEkran` (MASAK bileşeninin öteki kullanıcısı): **108/108**.

Mutasyon betiği `/private/tmp/kuyumcu-serit/pEKSIK-B1-SATINALMA/t3/mut.zsh`: yedek → tek eşleşmeli perl → filtreli test → geri kopya → cmp. Yedekler `/private/tmp/kuyumcu-serit/mut-yedek/EKSIK-B1-SATINALMA-t3/`.

| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| T3-M1 | `Masak = dto.Masak` yorum satırı (beyan alışa geçmez) | `Sinir_cok_buyuk_tutar_999_999_999_99_masak_beyaniyla_mal_kabul_yapilabilir`, `EB10_esik_ustu_mal_kabul_beyansiz_400_sifir_satir_beyanla_200_ve_kimlik_kaydi` | AYNI |
| T3-M2 | Ekran gövdesinde `masak` hep null | `EK09_esik_ustu_mal_kabulde_masak_formu_acilir_kilit_sebebi_ve_govdede_masak` | AYNI |
| T3-M3 | `MasakKilidi` eksik formda null (kilit yok) | EK09 | AYNI |
| T3-M4 | Gereksinim tedarikçisiz sorulur (bağlantılı toplam kaybolur) | EK09 | AYNI |
| T3-M5 | Eşik altında da kişisel veri gövdeye gider | `EK03_mal_kabul_kalan_dolu_asim_kilitler_gecerli_POST_ve_bant` | AYNI |
| T3-M6 | İstemci tarafı eşik kestirmesi silinir | **Kırmızı yok: eşdeğer.** Sunucu gereksinimi zaten kesin ölçü. Kestirme gereksiz bulunup **koddan kaldırıldı**, eşdeğer mutasyon kalmadı | AYNI |

**Kıran mutasyon 5/5.** T3-M6'nın hedefi koddan çıkarıldı.

### T3.6 Gerçek Chrome (başsız; `betikler/tur/tur.mjs` + MASAK çekmecesi için `canli3/cekmece2.mjs`; vekil 5167; CDP 9511–9520)
Ölçüm dizinleri `/private/tmp/kuyumcu-serit/pEKSIK-B1-SATINALMA/canli3/`:
- `olcum/`: /satin-alma, 8 kombinasyon.
- `olcum375/`: temiz durumda 375 yeniden ölçümü.
- `olcumc3/`: son kod, çekmece MASAK formu açık, 8 kombinasyon.
- `olcumc4/`: yalnız çekmece içi binme.

Son kodla çekmece (MASAK formu açık, eşik üstü sipariş 3 × 150.000; `#masak-baslik` her ölçümde var; `modGercek` = istenen):

| Görünüm | Genişlik | Mod × tema | Gizli görünen | Taşan | Kırık | TRY | Jargon | Ezik | Kırpılan | Kesik | Etiket binmesi | Yatay kaydırma | Kart % |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Çekmece + MASAK | 1280 | BASİT/PRO × açık/koyu (4) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 98 |
| Çekmece + MASAK | 375 | (4) | 0 | 0 | **0** | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 97 |
| /satin-alma sayfası | 1280 | (4) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 98 |
| /satin-alma sayfası | 375 | PRO açık, BASİT koyu (temiz ölçüm) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 97 |

**Gerekçeli sıfır dışı değerler:**
- **Kırık (K3), 375:** önce "· Birim fiyat | 150.000 ₺", düzeltmeden sonra "Kalan | 3 adet" kırıldı (`olcumc`, `olcumc2`). Özetin her parçası bölünmez yapıldı: `olcumc3`'te 0.
- **K9 binen = 6, çekmece açıkken sayfa ölçümü:** bütün örnekler "arka plan sayfası ⟂ modal çekmece" çiftidir (`#sa-yeni` ⟂ `#sa-detay-aciklama` gibi). Ölçer üst sınırı 6'dır. Çekmece içini ayrıca ölçtüm (`olcumc4`): 2–6 çift var ve hepsi yapışkan çekmece başlığı/alt çubuğu (`cekmece-sa-detay-baslik`, `cekmece-kapat`, `sa-detay-birincil`, `sa-detay-kilit`, `cekmece-sa-detay-iptal`) ile altından kayan gövde öğesi arasında. Gövde kaydırma kabında kırpılır; görüntüde binme yok (Read 3). Ortak `Cekmece.razor` davranışıdır, değişmedi.
- **/satin-alma 375 ilk ölçüm K9 = 6:** "Sütunları düzenle" paneli ⟂ satır. Neden: aynı oturumda 1280 geçişinde ölçerin tıkladığı "Sütunlar" paneli açık kaldı, pencere küçülünce yüzen panel satırın üstünde ölçüldü. Temiz durumda (`olcum375`, 2 kombinasyon) **0 bulgu**. Ortak `Izgara` bileşeni; bu turda değişmedi.

Read ile bakılan görüntüler (4/4):
1. `olcum/foto/satin_alma__BASIT_karanlik_375__acici6.png`: MASAK formu açık; birincil kilitli, sebep "Teslim almak için kimlik bilgilerini doldurun.", başlık tedarikçinin bugünkü toplamını (1.200.000 ₺) söylüyor.
2. `olcum/foto/satin_alma__PRO_aydinlik_375.png`: 375 K9'un kaynağı, açık kalan sütun paneli.
3. `olcumc3/foto/cekmece_masak__PRO_aydinlik_1280.png`: çekmece, kalem kartı, not, MASAK formu, yapışkan alt çubuk.
4. `olcumc3/foto/cekmece_masak__PRO_karanlik_375__alt.png`: koyu 375, beyan açıklaması sayacı, DegismezIpucu "Bu kayıt değiştirilemez … alış iadesiyle düzeltilir", irsaliye boş durumu, iptal katlanırı.

### T3.7 Tam süit ve denetim
**Tam süit** (tur başına bir kez; trx `/private/tmp/kuyumcu-serit/trx-pEKSIK-B1-SATINALMA-t3/_Mac_2026-10-10_14_00_02.trx`): **12077 toplam · 12071 geçti · 3 başarısız · 3 atlandı.** Kırmızılar:
- `RaporDortTestleri.RD_KH01` ve `RD_KH02`: devir listesindeki RAPOR-4 / GOC-D kırmızıları (önceki turda da vardı).
- `SATIS3EkranTestleri.T24_gercek_musteride_MASAK_kimligi_eksik_VE_nakit_iki_engel_TEK_cumlede(mod: "BASIT")`:
  - Yeni, tek başına koşumda da kırmızı (belirlenimci).
  - Bu paketin değil: test POS (`Islemler`) ekranınındır. Bu paketin değiştirdiği dosyalar (`SatinAlma.razor`, `SatinalmaModulu.cs`, `Sozlukler.SatinAlma.cs`, `Sozlukler.cs` içinde yalnız `Cikti.Aciklama`) `Islemler`'e girmez.
  - Aynı saatlerde başka şerit `Islemler.YasalKapi2.cs` (13:57), `Islemler.razor` (13:57) ve `Islemler.Diger.cs` (13:52) dosyalarını değiştirdi; Tests'te yeni `YasalKapi2Testleri.cs` var.
  - Sahibi: **YASAL-KAPI-2 şeridi**.
- **Bu paketin yeni kırmızısı: 0.**

Süitten sonra koda iki küçük değişiklik girdi: istemci eşik kestirmesinin kaldırılması (T3-M6) ve özet parçalarına `hucre-tek-satir`. İkisi de yalnız `SatinAlma.razor`'da. Son kodla filtreli koşum 70/70 yeşil. Tam süit tur başına bir kez kuralı gereği yinelenmedi.

**denetim.sh** (son kod):
- Paket sayaçları ✓: `Pages/*.razor 51 (tavan 51)`, `ModalPencere 48 (tavan 55)`, `SOZLESME_KILITSIZ_TIP 58 (tavan 58)`.
- Sonuç DENETİM BAŞARISIZ. Kalan üç madde başkasına ait:
  - `P-30 DEMO_INSAN_SURESI`: kurucu kronometresi.
  - `GIRDI-2 TEST_GECERSIZ_FIKSTUR 1`: MASAK-ESZAMAN-2, `MASAKESZAMANSinirTestleri.cs:187`.
  - **`P-11 DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48 > 47`.** Bu turun ilk sürümünde `MasakHazirla` catch'inde `masakGereksinim = null;` vardı ve sayaca +1 yazdı. Kaldırıldı (gereksinim sorgudan önce zaten düşürülüyor). Dosya bazında sayım: `SatinAlma.razor` ve `Irsaliyeler.razor` **0** katkı. Kalan 48'in dağılımı (Faturalar 6, Cariler 6, Islemler 2, …) `/private/tmp/kuyumcu-serit/pEKSIK-B1-SATINALMA/t3/p11-dagilim.txt` dosyasında. Fazla 1 başka şeridin; önceki turda bu sayaç ✓ idi, aradaki değişen sayfalar `Islemler.*` (YASAL-KAPI-2).

### T3.8 Derleme düzeltmesi
- Son kodla `dotnet build Kuyumcu.sln`: **0 Hata · 0 Uyarı** (artımlı).
- Tam yeniden derlemede (Tests projesi yeniden derlenince) **6 uyarı** var, araya başka şerit girince 7 (`YasalKapi2Testleri.cs:177 CS0649`):
  - `SUBEKAPSAMASinirTestleri.cs:49 CS8601`
  - `KesifP2Testleri.cs:455 xUnit2029`
  - `SonKalanlarTestleri.cs:593/824 xUnit2013`
  - `Yildiz2Testleri.cs:194/234 xUnit1031`
- **Hiçbiri bu paketin satırında değil.** Paketin Yildiz2 değişikliği yalnız 103 ve 220–223. satırlardaki sayaçlardır.
- Önceki raporun "0 Uyarı" iddiası artımlı derlemeye aitti; burada düzeltildi.

### T3.9 Kurucu / mali müşavir (tur 3)
- **Mali müşavir sorusu (TEYİTSİZ kademe):**
  - Tedarikçiden veresiye (cari borcu) mal kabulünde projenin "fon kaynağı / nakit işlem beyan formu" kademesi (canlı eşik 2.000.000,01 TL) uygulanmalı mı?
  - Tedbirler Yön. md.5/1-b kimlik tespitini ödeme şeklinden bağımsız tutar ölçüsüne bağlar; "beyan formu" ise yönetmelikte geçmiyor. Kademe bugün ödeme şeklinden bağımsız çalışıyor ve öyle bırakıldı (fail-closed).
  - Cevaba göre yapılandırma değişir; kod sabiti yok.
- **Bilgi:** MASAK kapı iletileri `Accept-Language: en` isteğinde de Türkçe dönüyor. Doğrudan `/islemler/alis` aynı davranışı gösterdi; mevcut MASAK kalıbı, bu paketin değil. MASAK sahibine not.
- **Bilgi (DÜŞÜK, ortak bileşen):** `MasakFormu` başlığı "bu **müşterinin** bugünkü toplamı" diyor; mal kabulde karşı taraf tedarikçidir. `MasakFormu.razor` bu paketin dosya alanında değil; MASAK/ortak bileşen sahibine not.
- Fiyat, teklif ya da tahsilat metni üretilmedi.


## 0. Bu tur ne yaptı
Paketin kodu 09.10 gecesi (1. tur) yazılmış, Docker 23:42'de çöktüğü için mutasyon, canlı doğrulama, ölçüm ve rapor yarım kalmıştı (1. turun M1-1 mutasyonu "transient failure" ile DB'ye bağlanamadan bitmişti). Bu turda (2. tur):
1. Derleme + paket testleri yeniden koşuldu.
2. Mutasyonlar baştan yapıldı (15 mutasyon). İki açık bulundu: biri test güçlendirilerek kapatıldı, biri eşdeğer mutasyon olarak belgelendi.
3. Tam süit paketin ürettiği **3 kırmızıyı** yakaladı: sayfa sayacı 49→51 (P37c, 2 iddia) ve sözleşme cırcırında 5 yeni kilitsiz istemci tipi. Üçü de düzeltildi; 5 tip **canlı JSON şekil kilidine** alındı, yani cırcır gevşetilmedi.
4. Gerçek Chrome sipariş çekmecesinde ölçerin yakalamadığı iki görsel kusur gösterdi: ürün adı üç satıra kırılıyordu ve "Bu teslimatta gelen" etiketi iki kez basılıyordu. Kalemler kart listesine çevrildi ve yeniden ölçüldü.
5. Canlı API (5166) üzerinde baştan sona akış, hasım denemeleri, TEZGAHTAR, başka kiracı ve eşzamanlı çift kabul SQL kanıtıyla doğrulandı.

## 1. Spesifikasyon ↔ kod doğrulaması (sapmalar)
| Spesifikasyon iddiası | Koddaki durum (10.10) | Karar |
|---|---|---|
| B1-1: sunucuda 8 satın alma ucu var, ekran ve menü yok | Doğru: 8 uç (`SatinalmaController`). 1. turda yeni sunucu mantığı yazılmadı; yalnız hasım kapsamındaki eksikler en küçük eklemeyle kapatıldı: çift kalem 400, ADET'e kesir 400, kilit + kilit sonrası denetimle çift kabulde 409, uzun metin artık sessizce kesilmiyor (400), beklenen tarih penceresi | Uygun |
| B1-2: irsaliyede 10 uç var, basılabilir belge türü yok | Doğru. `BelgeTurleri.Irsaliye` ve `CiktiServisi.IrsaliyeBelgesi` eklendi. Belgede fiyat basılmaz, en üstte "bilgi amaçlı döküm" uyarısı durur | Uygun |
| Yetki: SAHİP/MÜDÜR (TEZGAHTAR 403, menüde görünmez) | Sunucu GET uçları zaten SAHİP+MÜDÜR+MUHASEBE açıktı. **SAPMA:** MUHASEBE ekranı salt okur görür ("Yeni sipariş" kilitli ve kilidin sebebi yazılı); yazma uçları SAHİP+MÜDÜR. TEZGAHTAR 403 alır ve menüde görmez | Sunucu kapısına sadık kalındı (gerekçe: muhasebe irsaliye ve fatura bağını görmek zorunda) |
| Mal kabul stok hareketi üretir; atomik olmalı | `SatisServisi.Alis` → `HareketFabrikasi` (tek yazma yolu) üzerinden yazılır, URUN-ACILIS-ATOMIK kalıbıyla tek işlemde. Canlıda ret = 0 satır | Uygun |
| Tüm ekranlar karosu otomatik | Karolar `MenuTanimi.Ogeler`'den üretiliyor; 39 → 41 | Uygun |

## 2. Mevzuat (birincil kaynak)
- **İrsaliyede fiyat basılmaz.** Kaynak 213 s. VUK md.230/5 (https://www.mevzuat.gov.tr/MevzuatMetin/1.4.213.pdf, metin `/private/tmp/kuyumcu-serit/pEKSIK-B1-SATINALMA/vuk.txt` satır 3183-3196). Alıntı: "…fiyat ve bedel ile ilgili bilgiler hariç olmak üzere…". Belgede "Kalemler: Sıra · Ürün/tanım · Kod · Miktar" var, `tutarlar: []`. Canlı JSON'da fiyat yok.
- **Döküm yasal irsaliye yerine geçmez.** Sistem numarası, anlaşmalı matbaanın seri-sıra numarası değildir. Bu yüzden kâğıda "BİLGİ AMAÇLI DÖKÜMDÜR — yasal sevk irsaliyesi ya da e-İrsaliye yerine geçmez (VUK md.230/5)" basılır. Davranış fail-closed beyandır.
- **İrsaliye tarihi:** bugün dahil, ileri tarih yok, en çok 1 yıl geri. Bu bir İŞ KURALIDIR (Core `TarihPenceresi.Islem`), mevzuat hükmü değildir; kodda böyle işaretli.

## 3. DoD sözleşmesi
| Varlık | Oluştur | Düzenle (PATCH: yoksa koru, null ise sil) | Pasife al / geri dönüş | Denetim | Ekranda |
|---|---|---|---|---|---|
| Satın alma siparişi | POST /satinalma | PATCH /satinalma/{id} (yalnız TASLAK) | POST /{id}/iptal (gerekçe ≥ asgari, hard DELETE yok). Kabul görmüş sipariş iptal edilemez → DegismezIpucu "alış iadesi" | DenetimServisi (mevcut) | Düzenle (Cekmece, oluştur = düzenle), Onayla, Malı teslim al, İptal (OnayKipi) |
| Mal kabul | POST /{id}/mal-kabul | Düzenlenmez: deftere yazılır | Geri dönüş yolu ALIŞ İADESİ ya da storno (DegismezIpucu) | Alis üzerinden | Kalan önceden dolu, aşımda kilit + sebep, bantta irsaliye no ve yeni durum |
| İrsaliye (sevk) | POST /irsaliyeler (GIDEN) | PATCH /irsaliyeler/{id} | POST /{id}/iptal. Deftere bağlı (mal kabul) irsaliye 409 + gerekçe | mevcut | Düzenle (değişiklik yokken kilitli), Faturaya bağla / bağı kaldır, İptal, Yazdır |
| İrsaliye (mal kabul, GELEN) | mal kabulden doğar | PATCH (açıklama, hesap kodu, tarih) | İptal 409: "Önce ALIŞ İADESİ ya da storno ile defteri düzeltin" | mevcut | DegismezIpucu |
| İrsaliye dökümü | — | salt okunur belge | — | `OkumaKaydet` (KVKK izi) | "Bu kayıt değiştirilemez" ipucu |

## 4. Testler
- Paket testleri: `EksikB1SatinalmaHttpTestleri` EB01–EB08 + `EksikB1SatinalmaEszamanTestleri` EB09 (gerçek MySQL) ve `EksikB1SatinalmaEkranTestleri` EK01–EK08 (bUnit; EK02 dört rol). Toplam 17 test yöntemi, **19 koşum, 19/19 yeşil**.
- Bu turda güçlendirilen ve eklenen testler:
  - **EB03:** ADET'e kesir mal kabulün kendi kapısında, `alanlar.kalemler` ve "Kalem 1" ile reddedilir. Mutasyon açığı T2-M1-2'yi kapatır.
  - **EK03:** çekmecede tablo yok; "Bu teslimatta gelen" etiketi kalem başına bir kez basılır; `#sa-detay-bilgi` ortak `uy-tanim` düzeninde.
  - **SozlesmeSekilTestleri:** 5 yeni şekil kilidi koşumu (SatinAlmaListesi, SatinAlmaDetay, MalKabulSonucu, IrsaliyeListesi, IrsaliyeDetay). Listeler dolu fikstürle sınanır (sipariş → onay → 2 adet kabul), yani satır şekli de kilitlidir.
- Sayaç güncellemeleri: P37c sayfa sayısı 49 → 51 ve muaf olmayan 46 → 48; iki yeni ekran da `SayfaBasi` kullanır.
- Filtreli son koşum `SozlesmeSekil|SozlesmeKapsam|P37cPanelVeKapsam|EksikB1Satinalma`: **54/54 yeşil**. SATIS3 yeniden koşumu: 175/175.
- **Tam süit** (son kod, trx `/private/tmp/kuyumcu-serit/trx-pEKSIK-B1-SATINALMA/_Mac_2026-10-10_13_09_32.trx`): **12061 toplam · 12048 geçti · 10 başarısız · 3 atlandı.** Bu paketin kırmızısı yok:
  - **RD_KH01 / RD_KH02** (RaporDortTestleri): "Rapor dönemi en çok 400 gün" (GOC-D kuralı, RAPOR-4 testinin tarih aralıkları). Önceki koşumda (tam3) da kırmızıydı. Bu paketin dosyası değil.
  - **SATIS3EkranTestleri YKG01/02/03/07 × 2 mod** (8 koşum): `WaitForFailedException` (5 sn zaman aşımı). Koşum sırasında başka bir şerit `Islemler.YasalKapiGeri.cs` (13:10) ve `SiparisDetay.razor` (13:11) dosyalarını değiştirip `Kuyumcu.Web.dll`'i yeniden derledi (13:11:37). Aynı ikililerle yeniden koşum **175/175 geçti**, yani kararsız. Önceki tam koşumda (tam3) bunlar yeşildi.
  - Önceki tam koşumda (tam3: 12060 / 12055 geçti / 2 başarısız) yalnız RD_KH01/02 kırmızıydı. Bu paketin ürettiği üç kırmızı (P37c, SozlesmeKapsam ×2) ilk tam koşumda bulunmuş ve düzeltilmişti.
- **Önce/sonra:** 09.10 KAPORA kapısında 11970 test; bugün 12061 (eşzamanlı şeritler dahil).

## 5. Mutasyon tablosu (`mut.sh`: yedek → tek eşleşmeli perl → filtreli test → geri kopya → cmp)
| Kod | Değişiklik | Kırmızı test | cmp |
|---|---|---|---|
| T2-M1-1 | Çift kalem denetimi `!= Count` → `== -1` | EB03 | AYNI |
| T2-M1-2 | Mal kabulde `UrunAdetKapisi.Dogrula` yorum satırı | **YOK** → açık. Alış servisi geç reddediyordu, test yalnız 400 + 0 satır bakıyordu | AYNI |
| T2-M1-2b | Aynı mutasyon, EB03 güçlendirildikten sonra | EB03 | AYNI |
| T2-M1-3 | `KilitSonrasiDenetle` atlanır (çift kabul) | EB09 | AYNI |
| T2-M1-4 | `Kirp` aşırı uzun metni sessizce keser | EB03, EB06, EB07 | AYNI |
| T2-M1-5 | Çıktıda irsaliye rol kapısı kapatılır | EB08 | AYNI |
| T2-M1-6 | Sevk irsaliyesi `TarihDenetle(dto.Tarih)` silinir | EB07 | AYNI |
| T2-M1-7 | Sipariş `BeklenenTarihDenetle` silinir | EB06 | AYNI |
| T2-M1-8 | Detayda `tedarikciAdi` → `tedarikciAd` (alan kayması) | SozlesmeSekil (SatinAlmaDetay, MalKabulSonucu) | AYNI |
| T2-M2-1 | İrsaliye PATCH değişmeyen `hesapKodu`'nu da gönderir | EK07 | AYNI |
| T2-M2-2 | MUHASEBE'ye yazma yetkisi | EK02(MUHASEBE) | AYNI |
| T2-M2-3 | Kabul kutusundan `EnCok` kaldırılır | **YOK — eşdeğer mutasyon.** Aşım üç katmanda yakalanıyor (EnCok, `KabulHatasi` alan iletisi, `DetayKilidi`); tek katmanı silmek davranışı değiştirmiyor. Test açığı değil | AYNI |
| T2-M2-4 | Liste yazdır bağlantısında dönüş adresi kaybolur | EK06 | AYNI |
| T2-M2-5 | Kalan adet önceden doldurulmaz | EK03 | AYNI |
| T2-M2-6 | Mal kabul gövdesinde kur kimliği null | EK03 | AYNI |
| T2-M2-7 | Detay özetinden `uy-tanim` düşer (yeni düzen) | EK03 | AYNI |

Kıran mutasyon 14/15; açık 1 (kapatıldı); eşdeğer 1 (gerekçeli). Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 hata · 0 uyarı** (artımlı; tam yeniden derlemedeki 6 uyarı başkasının — § T3.8).

## 6. Denetim (`dotnet/denetim.sh`)
Bu paketin sayaçları ✓:
- `Pages/*.razor dosya sayısı 51 (tavan 51)`
- `Pages: ModalPencere kullanımı 48 (tavan 55)` (yeni düzenleme modalı yok; düzenleme Cekmece'de, iptal OnayKipi'nde)
- `P-03: SOZLESME_KILITSIZ_TIP 58/117 (tavan 58)`: 5 yeni tip kilitli, tavan artmadı
- P02 rota 51

Sonuç **DENETİM BAŞARISIZ**, iki madde başkasına ait (KAPORA-EKRAN raporunda da aynı ikisi):
- `P-30 DEMO_INSAN_SURESI`: kurucu kronometresi.
- `GIRDI-2 TEST_GECERSIZ_FIKSTUR 1`: `Kuyumcu.Tests/Dogrulayici/MASAKESZAMANSinirTestleri.cs:187` (MASAK-ESZAMAN-2).

## 7. Canlı doğrulama (5166, kiracı `PAKETEKSIK-B1-SATINALMA-TEST`, betik `/private/tmp/kuyumcu-serit/pEKSIK-B1-SATINALMA/canli2/canli.py`, çıktı `canli-sonuc.txt`)
SQL sütunları: [sipariş | irsaliye | irsaliye kalemi | ALIŞ hareketi | stok (ürün 1)]
| Adım | Beklenen | Gerçek |
|---|---|---|
| Sipariş hasım: ADET 1,5 · −1 · 0 · fiyat −5 · geçmiş beklenen tarih · 600 karakter açıklama (TR+EN) | 400 + 0 satır | 400 ×7 ("Kalem 1: Küsuratlı yazılamaz; tam sayı girin." · "Tarih 10.10.2026 tarihinden önce olamaz." · "En çok 500 karakter yazılır." / "At most 500 characters."). SQL 0\|0\|0\|0\|0 |
| Geçerli sipariş (2 kalem) | 200 TASLAK | 200 SAS-2026-00001 TASLAK |
| PATCH yalnız hesapKodu | açıklama korunur | 200, açıklama "Ekim bilezik siparisi" korundu |
| PATCH hesapKodu null | silinir | 200, hesapKodu None |
| Onayla | ONAYLANDI | ONAYLANDI, malKabulEdilebilir True |
| Mal kabul hasım: fazla 6/5 · −1 · 0 · 1,5 ADET · aynı kalem 3+3 · karışık (2 geçerli + 9 fazla) · kursuz · EN | 400, 0 satır | 400 ×8 (EN: "The received quantity exceeds the ordered quantity…"). SQL 1\|0\|0\|0\|0 (karışıkta geçerli satır da yazılmadı = atomik) |
| Kısmi kabul (2+2) | 200 KISMI_KABUL | IRS-2026-00001; SQL 1\|1\|2\|2\|2 |
| **Eşzamanlı çift kabul** (kalan 3, iki istek 3+3) | biri 200, biri 409 | [200, 409]; SQL 1\|2\|3\|3\|**5** (stok yalnız bir kez arttı) |
| Kapanmış siparişe kabul | 409 | 409 "…(şu an: KAPANDI)"; SQL değişmedi |
| Kalem kabul durumu | 5/5, 2/2 | `0,5.000,5.000 ; 1,2.000,2.000` · sipariş KAPANDI · bağsız irsaliye kalemi 0 |
| Yazdır IRSALIYE (SAHİP, MUHASEBE) | 200, fiyat yok | 200 / 200. "MAL KABUL İRSALİYESİ · IRS-2026-00001 · Dayanak: Satın alma siparişi SAS-2026-00001". Sütunlar Sıra/Ürün/Kod/Miktar, `tutarlar: []` |
| TEZGAHTAR: GET/POST satınalma, mal kabul, GET irsaliyeler, çıktı | 403 | 403 ×5 |
| MUHASEBE POST /satinalma | 403 | 403 |
| Sevk irsaliyesi hasım: ileri tarih · 1,5 ADET · −2 · 600 karakter | 400 | 400 ×4; SQL değişmedi |
| Sevk geçerli → PATCH → iptal | 200 / 200 / 200 IPTAL | IRS-2026-00003 GIDEN → "(duzeltildi)" → IPTAL |
| Mal kabul irsaliyesini iptal | 409 + yol | 409 "…Önce ALIŞ İADESİ ya da storno ile defteri düzeltin." |
| **Başka kiracı** (`…-TEST2`): sipariş GET, mal kabul, irsaliye GET, çıktı, PATCH | 404 | 404 ×5; SQL değişmedi |
| Sipariş iptal → iptal edilmiş siparişe kabul | 200 IPTAL / 409 | 200 IPTAL / 409 "…(şu an: IPTAL)" |
| SQL son | — | irsaliyeler: `IRS-…-00001,GELEN,ACIK,1 ; …-00002,GELEN,ACIK,1 ; …-00003,GIDEN,IPTAL,0`. ALIŞ hareketi 3 adet, toplam 7,000 (5 + 2) |

## 8. Gerçek Chrome ölçümü (başsız Chrome, `betikler/tur/tur.mjs` + `sikisma.js`/`ekstra.js`, CDP 9512–9514, vekil 5167)
Son ölçüm `/private/tmp/kuyumcu-serit/pEKSIK-B1-SATINALMA/olcum4/` (98 görüntü): 3 rota × {1280, 375} × {BASİT, PRO} × {açık, koyu} = **24/24 geçerli sayfa** (modGercek = istenen). Her sayfada açılan katmanlar da ölçüldü: 150 katman, 74 açılan (yeni sipariş çekmecesi, sipariş detay/mal kabul çekmecesi, irsaliye detayı).

| Rota | Genişlik | Mod × tema | Taşan | Kırık | Binen | Gizli görünen | TRY kalıbı | Yatay kaydırma | Ezik | Kırpılan | Kesik | Kart % |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| /satin-alma | 1280 | BASİT/PRO × açık/koyu (4) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 98 |
| /satin-alma | 375 | (4) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 97 |
| /irsaliyeler | 1280 | (4) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 98 |
| /irsaliyeler | 375 | (4) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 97 |
| /cikti/IRSALIYE/{id} | 1280 | (4) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 90 (A4 kâğıt genişliği) |
| /cikti/IRSALIYE/{id} | 375 | (4) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 97 |

Gerekçeli sıfır dışı değerler:
- **K8 jargon = 1:** yalnız çıktı + BASİT. Değer "SATINALMA-TEST", yani kâğıtta "Düzenleyen" olarak basılan test kiracısının adı. Arayüz metni değil, test verisi.
- **Katman ekranDisi = 6:** yalnız `/cikti` sayfasındaki ortak "i" (summary) açıklama düğmesi. 375'te SERBEST-YUZEN, 1280'de bağlı-açılır. Bu, `Cikti.razor` ve yazdırma bileşeninin ortak davranışıdır; bu pakette değişmedi (diff 0). Bu paketin ekranlarında ekranDisi 0.

**Ölçerin yakalamadığı, görüntüde görülen ve düzeltilen kusurlar** (2. ölçüm `olcum2`):
1. Sipariş çekmecesindeki kalem tablosu 1280'de dar kaldı: ürün adı 3 satıra kırıldı, giriş kutusunun etiketi sütun başlığını tekrarladı ve kutu bunu "(isteğe bağlı)" diye gösterdi. 375'te etiket iki kez basıldı.
2. Detay özetinde etiket ve değer aynı ağırlıktaydı.

Düzeltme: kalemler kart listesi oldu (`kart-govde`). Ürün adının altında tek satır özet var: sipariş · gelen · kalan · birim fiyat. Kutu tek etiketlidir; tek kalan kalemde zorunlu (*) olur, kalanı biten kalemde "Bu kalemin tamamı teslim alındı." yazar. Özet listeleri ortak `uy-tanim` sınıfını kullanır; yeni CSS yok. Kimlikler değişmedi.

Read ile bakılan görüntüler (4/4):
- `olcum2/satin_alma__PRO_karanlik_375__acici7.png` (kusur)
- `olcum2/satin_alma__PRO_aydinlik_1280__acici5.png` (kusur)
- `olcum4/cikti_IRSALIYE_…__BASIT_aydinlik_1280.png` (kâğıt doğru, fiyat yok, uyarı üstte, menüde "Belge ve uyum: Faturalar · İrsaliyeler")
- `olcum4/satin_alma__BASIT_karanlik_375__acici7.png` (düzeltilmiş kart)

## 9. Temizlik
- API (dotnet run sarmalayıcısı ve alt süreç), vekil ve kendi Chrome'larım (CDP 9512/9513/9514) kendi PID'lerimle durduruldu. 5166, 5167 ve 9512–9514'te dinleyen yok.
- Eski tam süit koşumumu (tam2) kod değiştiği için kendi PID'lerimle durdurdum. Başka şeridin testhost'una dokunulmadı.
- `kiraci-temizle.sh … --uygula` iki kiracı için çalıştırıldı: `01M4JKABH4MSD5GEJKXXP0XW4J` (`PAKETEKSIK-B1-SATINALMA-TEST`) ve `01M4JKB65GWX04X3CK4YJRCK79` (`…-TEST2`). İkinci çağrı her biri için "Kiracı YOK (bağlantı sınandı ve ÇALIŞIYOR)" dedi. `PAKETEKSIK-B1-SATINALMA%` = 0.
- **tenants: başlangıç 22 → son 23.** Fark bu paketin değil: `PAKETYKG-HAKEM-TEST 86d322` 10:15 UTC'de (13:15 yerel) başka bir şerit tarafından, benim başlangıç ölçümümden sonra açıldı. QA-TEST- kiracılarına dokunulmadı.
- Sır dosyaları (`fikstur.json`, `fikstur-muh.json`, `anahtar.txt`) `printf ''` ile boşaltıldı. Parola, jeton ya da anahtar çıktıya yazılmadı.
- Proje ağacına .bak/.mutorj bırakılmadı. Mutasyon yedekleri `/private/tmp/kuyumcu-serit/mut-yedek/EKSIK-B1-SATINALMA/` altında.

## 10. Değişen dosyalar (paket: 1. tur + 2. tur; ağaç büyük ölçüde untracked olduğu için liste yedek karşılaştırmasıyla çıkarıldı)
**Sunucu:**
- `dotnet/Kuyumcu.Api/Modules/Satinalma/SatinalmaModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Irsaliye/IrsaliyeModulu.cs`
- `dotnet/Kuyumcu.Api/Modules/Cikti/CiktiModulu.cs`
- `dotnet/Kuyumcu.Api/Common/SatirKilidi.cs` (yeni kilit tablosu)

**Sözleşme ve çekirdek:**
- `dotnet/Kuyumcu.Contracts/Sayfalar/SatinAlmaSozlesmeleri.cs` (yeni)
- `dotnet/Kuyumcu.Contracts/Cikti/CiktiSozlesmeleri.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.SatinAlma.cs` (yeni; 2. tur `KalemTamam` TR+EN)
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Menu.cs`
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.Yildiz.cs`
- `dotnet/Kuyumcu.Core/I18n/SunucuIletiSozlugu.Veri.cs`
- `dotnet/Kuyumcu.Core/Domain/Girdi/GirdiHatasi.cs` (AlanSinirlari)
- `dotnet/Kuyumcu.Core/Domain/IzgaraTercihi.cs`
- `dotnet/Kuyumcu.Core/Erisim/RotaModulleri.cs`

**Ekran:**
- `dotnet/Kuyumcu.Web/Pages/SatinAlma.razor` (yeni)
- `dotnet/Kuyumcu.Web/Pages/Irsaliyeler.razor` (yeni)
- `dotnet/Kuyumcu.Web/Layout/MenuTanimi.cs`
- `dotnet/Kuyumcu.Web/Layout/RotaBasliklari.cs`
- `dotnet/Kuyumcu.Web/Bilesenler/Yildiz/YildizIkonlari.cs`

**Testler:**
- `dotnet/Kuyumcu.Tests/EksikB1SatinalmaTestleri.cs` (yeni)
- `dotnet/Kuyumcu.Tests/EksikB1SatinalmaEkranTestleri.cs` (yeni)
- Sayaç ve kilit güncellemeleri:
  - `MenuMimarisiTestleri.cs`
  - `P17CiktiPanoTestleri.cs`
  - `Yildiz2Testleri.cs`
  - `Yildiz4BTestleri.cs`
  - `RotaBaslikTestleri.cs`
  - `S1gMenuTuvalTestleri.cs`
  - `S1jMenuAramaTestleri.cs`
  - 2. tur: `P37cPanelVeKapsamTestleri.cs`
  - 2. tur: `SozlesmeSekilTestleri.cs`

**Denetim, betik ve belge:**
- `dotnet/denetim.sh` (sayfa 49 → 51, P02 51)
- `dokumanlar/09-uygulama-kayitlari/betikler/qa-turu.js` (ROTA_KAPSAM 51)
- `docs/KULLANIM_KILAVUZU.md` (Satın alma ve İrsaliyeler bölümleri)
- `dotnet/Kuyumcu.Web/wwwroot/kilavuz.html` (üretildi)

**Dokunulan sayfalar (tek dokunuş kuralı):** `SatinAlma.razor`, `Irsaliyeler.razor` (ikisi de yeni). `Cikti.razor` değişmedi.

**Görev dosya alanı dışına çıkılan yerler:**
- `SozlesmeSekilTestleri.cs` ve `P37cPanelVeKapsamTestleri.cs`: bu paketin yeni ekranları ve sözleşme tipleri bu iki sayacı/cırcırı kırdı. Düzeltmenin tek doğru yeri bunlar. Cırcır gevşetilmedi, tipler kilitlendi. Yedekler `/private/tmp/kuyumcu-serit/pEKSIK-B1-SATINALMA/orijinal-t2/` altında.
- 1. turdaki sayaç güncellemeleri de aynı gerekçeyle yapıldı (yedekler `orijinal/` altında).

## 11. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| Ekranlar menüde TR+EN, Tüm ekranlar karosu | ✓ | EK08 ("Satın alma"/"Purchasing", "İrsaliyeler"/"Waybills"); Yildiz2 41 karo; Chrome menüsü |
| Sipariş → kısmi kabul → tam kabul → irsaliye yazdır, canlı + SQL | ✓ | § 7 |
| Hasım geçersizleri 400 + SQL 0 satır | ✓ | § 7: sipariş 7, mal kabul 8, sevk 4 |
| Fazla kabul, eksi, kapanmış siparişe kabul, ADET'e kesir, çift kabul (idempotency), başka kiracı | ✓ | 400 / 409 / 404 + eşzamanlı [200, 409], stok 5 |
| URUN-ACILIS-ATOMIK (ret = 0 satır) | ✓ | karışık kabulde geçerli satır da yazılmadı |
| TEZGAHTAR 403 + menüde yok | ✓ | canlı 403 ×5; EK08 |
| Mutasyon ≥ 4 kırmızı, cmp AYNI | ✓ | 14 kıran mutasyon |
| Gerçek Chrome 1280+375 · BASİT+PRO · açık+koyu | ✓ | § 8, 24/24 |
| build 0/0 | ✓ (düzeltildi, § T3.8) | artımlı 0 Hata · 0 Uyarı; tam yeniden derlemede Tests projesinde 6 (sonra 7) uyarı, hiçbiri bu paketin satırında değil |
| denetim: paket sayaçları | ✓ | § 6 (2 başkasına ait madde) |
| Tam süit 0 yeni kırmızı | ✓ | § 4 |
| Test kiracısı silindi | ✓ | § 9 |
| COMMIT yok, dosya silme yok | ✓ | — |

## 12. Açık kalanlar
1. **RaporDortTestleri RD_KH01/RD_KH02:** "Rapor dönemi en çok 400 gün" (GOC-D kuralı) ile RAPOR-4 testinin aralıkları çatışıyor. Sahibi: RAPOR-4 / GOC-D şeridi.
2. **SATIS3EkranTestleri YKG01/02/03/07:** eşzamanlı derleme sırasında zaman aşımı, yeniden koşumda 175/175. Sahibi: t2 (ISLEM-BANT / YASAL-KAPI-GERI). Dosyaları koşum sırasında değişti.
3. **denetim.sh:** `P-30 DEMO_INSAN_SURESI` (kurucu kronometresi) ve `GIRDI-2 TEST_GECERSIZ_FIKSTUR` (`MASAKESZAMANSinirTestleri.cs:187`, MASAK-ESZAMAN-2).
4. **Ortak çıktı sayfasındaki "i" açıklama düğmesi** 375'te serbest yüzen katman açıyor. `Cikti.razor` ve yazdırma bileşeni, tüm belge türlerinde aynı; bu paketin değil. YAZDIR şeridine not.
5. **T2-M2-3 eşdeğer mutasyon:** kabul aşımı üç katmanda yakalanıyor. İstenirse katmanlardan biri sadeleştirilebilir (Ertuğ'a not, davranış değişmez).

## 13. Kurucu / mali müşavir
- **Mali müşavir sorusu:** Sistemin bastığı irsaliye dökümü "bilgi amaçlı" damgalıdır. Yasal sevk irsaliyesi için anlaşmalı matbaa basımı ya da e-İrsaliye mi kullanılacak? Kuyumcu e-İrsaliye zorunluluğu kapsamında mı (ciro eşiği)? Bu paket e-İrsaliye eşiğini ARAŞTIRMADI, koda da yazmadı. Döküm her durumda "yerine geçmez" der (fail-closed).
- **Mali müşavir sorusu:** VUK md.230/5 "fiili sevk tarihi" ileri tarihli düzenlenebilir mi? Ekran bugün ileri tarihi iş kuralı olarak reddediyor; bu bir mevzuat hükmü değildir.
- Fiyat, teklif ya da tahsilat metni üretilmedi (ücretsiz pilot kuralı).
