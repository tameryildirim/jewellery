# PAKET S2-6 — Pro modda "Diğer ▾" işlemleri (P-21 uçları) · uygulama raporu

**Tarih:** 24.09.2026 · **Şerit:** S2b (tezgâh fişi) · **Rol:** Taşkın (backend okuması) + Ertuğ (ekran) · **Commit:** yok

**Durum: BİTTİ.** Paket kapısının yedi maddesi karşılandı. Tam süitteki 13 kırmızının hiçbiri bu paketin dosyalarına dokunmuyor, sahipleri § 9'da yazılı.

---

## 1. Kısaca

- **Ne yapıldı:** Pro modda işlem sekmelerinin yanına "Diğer ▾" menüsü geldi. Menüde altı iş var: ürün ayır (rezerv), kapora (avans), konsinye, ürün değişimi, siparişten satış, çek/senet devri (ciro ve rücu).
- **Kapsanan uçlar:** P-21'in sunucudaki **15 ucunun her biri** artık ekrandan çağrılıyor. Plan "14 uç" diyordu; yansımayla sayıldığında 15 çıktı (§ 2). Böylece UX-S25'teki "uçta var, ekranda düğme yok" boşluğu kapandı.
- **Basit mod:** "Diğer" menüsü ve paneli **DOM'da yok** (test ve mutasyonla kanıtlı).
- **Sunucu:** kodu değişmedi. Migration gerekmedi.

## 2. Şartname ile kod karşılaştırması (adım 1)

| Plan 2.9'daki iddia | Koddaki durum | Ne yapıldı |
|---|---|---|
| "Sunucuda 14 uç hazır" | 15 uç var: `TicaretController` 12 (rezervler POST/GET/PATCH/iptal · avans POST/mahsup/iade/GET · degisim · konsinye-cikis/iade/acik) + `EvrakController` ciro ve rücu + `SiparisController` satışa çevir | 15'inin hepsi bağlandı. Sayı testte yansımayla ölçülüyor (`P21_uclarinin_her_biri_ekran_istek_katmaninda_var`, 15 bekleniyor). |
| İstemci bu uçları çağırmıyor | Doğru. `grep` sonucu Web'de 0 çağrı vardı. | — |
| Taslak n5'te "8 tür + Diğer (rezerv, avans, konsinye, siparişten satış, ciro)" | Değişim ucu (`/islemler/degisim`) da ekrana bağlı değildi. TAKAS sekmesi başka bir uç kullanıyor. | Değişim de "Diğer"e eklendi (UX-S25). |
| Islemler.razor ≤ 1.400 satır (S21 yapı testi) | Başlangıçta 1.399 satırdı. | Sayfaya **net 0 satır** eklendi: 1.399 → 1.399. İstek katmanı `Pages/Islemler.Diger.cs` dosyasına kondu (§ 3). |

**Mevzuat:** Bu paket hiçbir mevzuat hükmü yazmadı. Tevsik, MASAK ve gerekçe asgarileri sunucuda kaldı. Ekran eşik sayısı taşımıyor. Tek istisna gerekçe uzunluklarıdır (10 ve 20). Bunlar sunucu DTO'larının kendi sabitleri; ekran yalnız boş gönderimi durduruyor, sunucu yine reddediyor (canlı 26 → 400). Canlı adım 21'de tevsik kapısı ekranın gönderdiği gövdeyle 400 döndü: **ekran yasal kapıyı atlamıyor.**

## 3. Mimari karar ve sapmalar (gerekçeli)

1. **İstek katmanı `Pages/Islemler.Diger.cs` dosyasında** (Islemler sayfasının `partial` parçası):
   - Plan § 3 md.5'teki "TEK POST YOLU" kuralı **POS fiş gövdesi** için konmuş. Bu dosya fiş gövdesine ve `Kaydet()`'e dokunmuyor.
   - Buradaki 15 istek fiş değil. Her biri kendi sunucu kapısı olan ayrı bir uç.
   - İstek katmanı `.razor` dosyasına yazılsaydı 1.400 satır sınırı (S21 yapı testi) aşılırdı.
   - Dosya, şeridin dosya alanındaki "Pages/Islemler.razor" sayfasının parçasıdır. Sayfa dışında bir servis açılmadı; `Program.cs`'e ve DI'ya dokunulmadı.
   - **Hasım doğrulayıcı için:** `Tek_POST_yolu_Islemler_Kaydet_icinde_kalir` testi yalnız `.razor` dosyasını tarıyor. Bu dosya o testi atlatmak için açılmadı. Ayrım ayrıca kendi yapı testiyle kilitli (`Fis_Diger_bilesenleri_istek_atmaz_istek_katmani_sayfada`).
2. **Fiş bileşenleri istek atmıyor.** `Bilesenler/Fis/FisDiger*` (8 razor + 1 model) `ApiIstemci` enjekte etmiyor. Kullanıcının niyeti `DigerIstek` ile sayfaya bildiriliyor (S2-1 deseni). S21'in mevcut yapı testi de bu dosyaları tarıyor ve yeşil.
3. **Islemler.razor'a iki dokunuş, satır sayısı değişmedi:**
   - Sekme bileşenine `Secili="@(DigerAcik ? "" : tur)"` ve `TurSec="TurSecDiger"` verildi. Sekmeye dönülünce panel kapanıyor.
   - Boş satırın yerine `<FisDigerIslemler …/>` kondu.
   - `@if (yukleniyor)` satırı `@if (DigerAcik) { } else if (yukleniyor)` oldu. Panel açıkken fiş çizilmiyor: tek ekran, tek birincil iş.
   - **Hiçbir id silinmedi.** Değiştirilen satırlarda `id=` yoktu. S21 id envanteri testi yeşil.
4. **Onay modalı sayfada değil, bileşende** (`FisDigerIslemler.razor`). Bu yüzden "Pages: ModalPencere" sayacı artmadı (48, tavan 55). Modal yalnız geri alınamaz eylemler için kullanılıyor (DoD EK md.2 ve 4): ayırmayı kaldır, kaporayı geri öde, siparişi satışa çevir, çek devri, karşılıksız dönen.
5. **Tek düzenleme kalıbı:**
   - Rezerv düzenleme `Cekmece` ile yapılıyor (`#cekmece-Rezervasyon-*`).
   - Düzenlenemeyen kayıtlar `DegismezIpucu` ile gerekçesini ve doğru yolu gösteriyor: kapora (doğru yol "Kaporayı geri öde"), konsinye gönderimi (doğru yol "Geri al"), kapanmış ayırma.
6. **Taslaktan sapma:** Yok. "Diğer ▾" sekme şeridinin yanında, S3-2'nin yerli popover kalıbıyla açılıyor (JS yok, CSP istisnası yok). Satır içi `style` 0, sabit renk 0. Yeni CSS yazılmadı; mevcut belirteçli sınıflar kullanıldı (`satir-menu`, `veri-tablosu`, `bant`, `rozet`).
7. **Ekrana giden metinler:**
   - Tüm metinler `Sozlukler.FisDiger.cs` dosyasında, TR ve EN (konumsal record; eksik anahtar derleme hatası verir).
   - Jargon kaldırıldı: "rezerv" → "Ürün ayır", "avans" → "Kapora", "ciro/rücu" → "Çek/senet devri" ve "Karşılıksız dönen".
   - Rol kodu ekrana basılmıyor. Kilitli düğmenin yanında sebep yazıyor, örneğin "yalnız işletme sahibi ya da müdür".

## 4. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısından bu pakete ait olanlar:

```
 M dotnet/Kuyumcu.Web/Pages/Islemler.razor                      (2 dokunuş, 1399 → 1399 satır)
?? dotnet/Kuyumcu.Web/Pages/Islemler.Diger.cs                   (istek katmanı, partial)
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisDigerModelleri.cs
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisDigerIslemler.razor     (menü + panel + ortak onay)
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisDigerRezerv.razor
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisDigerAvans.razor
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisDigerKonsinye.razor
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisDigerDegisim.razor
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisDigerSiparis.razor
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisDigerEvrak.razor
?? dotnet/Kuyumcu.Web/Bilesenler/Fis/FisDigerKasa.razor         (ortak kasa satırı, UX-S22)
?? dotnet/Kuyumcu.Core/I18n/Sozlukler.FisDiger.cs
?? dotnet/Kuyumcu.Tests/S26DigerIslemlerTestleri.cs
```

Sözlükler hub'ına, app.css'e, sunucuya ve B/C/D şeritlerinin dosyalarına **dokunulmadı**.

**Dokunulan sayfalar (DoD EK 2):**
- `Pages/Islemler.razor`: S2 şeridinin kendi sayfası; bu dalgada S2-5/S2-5b ile aynı şeritte.
- `Pages/Islemler.Diger.cs`: yeni dosya.

## 5. Derleme ve testler

- **Derleme:** `dotnet build Kuyumcu.sln --nologo` → **0 hata, 1 uyarı**. Uyarı bu pakete ait değil: `Kuyumcu.Tests/EtiketEkranTestleri.cs(145,47)` xUnit1031, başka şeridin test dosyası.
- **Paket testleri** (`S26DigerIslemlerTestleri`, **21 yeni test**): 21/21 yeşil. S21 yapı testleriyle birlikte 42/42.

| Test | Ne kanıtlıyor |
|---|---|
| Basit_modda_Diger_menusu_ve_paneli_DOMda_yok | KABUL (b) |
| Pro_modda_Diger_menusu_alti_islemi_tasir_ve_panel_fisi_gizler | 6 iş; panel fişi gizliyor; sekmeye dönünce kapanıyor |
| Fise_don_dugmesi_paneli_kapatir | "Fişe dön" düğmesi gerçek bir eylem yapıyor |
| Tezgahtar_icin_konsinye_ve_cek_devri_kilitli_ve_sebebini_yazar | UX-S30: kilitli düğme sebebini yazıyor |
| Rezerv_bos_gonderimde_istek_atmaz_alan_altina_yazar | DoD EK md.3 |
| Rezerv_acar_listeyi_yukler_ve_sunucu_sonucunu_banda_yazar | POST + GET; UX-S27 |
| Rezerv_duzenle_yalniz_degisen_alani_PATCH_eder_bosaltilan_null_gider | PATCH sözleşmesi: gövdede olmayan alan korunur, null gelen alan silinir |
| Rezerv_kaldir_gerekce_ve_ortak_onay_ister | gerekçe ≥ 10 + onay → /iptal |
| Tezgahtar_rezervi_kaldiramaz_dugme_kilitli_sebebi_yazili | sunucu [RolGerekli] kuralının ekrandaki karşılığı |
| Liste_hatasi_bos_duruma_cevrilmez | UX-S28 |
| Kapora_al_mahsup_ve_iade_uclari_ekrandan_cagrilir | GET avans + POST avans/mahsup/iade; tek kasa sorulmuyor (UX-S22) |
| Tezgahtar_kaporayi_geri_odeyemez_dugme_kilitli_sebebi_yazili | UX-S30 |
| Konsinye_gonder_ve_geri_al_uclari_ekrandan_cagrilir | 3 uç; dönmemiş adetten fazlası reddediliyor |
| Degisim_farki_kartla_kapanmaz_ve_govde_dogru_gider | KART seçeneği yok (2 seçenek); gövde doğru |
| Siparisten_satis_kalem_fiyatli_govde_ve_onayla_POST_eder | 3 uç; kapora sessizce düşülmüyor (`avansMahsupTl` null) |
| Cek_devri_ve_karsiliksiz_donen_uclari_onayla_cagrilir | ciro ve rücu; gerekçe ≥ 20 |
| P21_uclarinin_her_biri_ekran_istek_katmaninda_var | KABUL (a), yansımayla 15 uç |
| Fis_Diger_bilesenleri_istek_atmaz_istek_katmani_sayfada | tek POST yolu ayrımı |
| Rezerv_duzenleme_govdesi_yalniz_degisen_alanlari_tasir | saf test |
| Degisim_fark_turlerinde_KART_yok | saf test |
| Sozluk_TR_ve_EN_tum_anahtarlari_dolu | saf test |

- **POS ile ilgili geniş küme** (`S2|Islem|BasitMod|Pos|Karma|Iade|Barkod|Teklif|Duzeltme|Ux|Dod` filtresi): **847/847 yeşil.**
- **Tam süit:**
  - Komut: `KUYUMCU_TEST_DB=kuyumcu_test_x dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo -v n --logger trx --results-directory …/scratchpad/trx-pS2-6`
  - trx: `_Mac_2026-09-24_05_40_17.trx`
  - Sonuç: **4924 test: 4911 geçti, 13 başarısız.**
  - 13'ün hiçbiri Islemler, Fis, FisDiger ya da bu paketin sözlüğünü içermiyor. Sahipleri § 9'da.
- **Önceki taban ölçülemedi:** Görev "önce tam süiti koş" diyordu, iki kez denendi, ikisi de geçersiz oldu:
  - İlk koşu: aynı ağaçta başka bir ajan eşzamanlı `dotnet build --no-incremental` koştu. `MvcTestingAppManifest.json` kilitlendi (MSB4018).
  - İkinci koşu: `Kuyumcu.Api.deps.json` bulunamadı; 840 sahte kırmızı çıktı.
  - Bu yüzden (7) maddesi "fark" olarak şöyle ölçüldü: kırmızı listesi tek tek incelendi ve her birinin bu paket dışındaki bir dosyaya bağlı olduğu gösterildi (§ 9).

## 6. Mutasyon kanıtları

Yöntem: yedek `scratchpad/mut-yedek/S2-6/` altına alındı → `perl -0777 -pi` ile tek eşleşmeli değişiklik → `dotnet test --filter S26DigerIslemler` → yedek geri kopyalandı → `cmp` birebir.

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alma (cmp) |
|---|---|---|---|
| M26-1 | `@if (!Basit)` → `@if (true)` (basit modda menü) | Basit_modda_Diger_menusu_ve_paneli_DOMda_yok ¹ | BİREBİR |
| M26-2 | `DigerAcik => false` (panel fişi gizlemiyor) | Pro_modda_Diger_menusu_alti_islemi_tasir_ve_panel_fisi_gizler | BİREBİR |
| M26-3 | `TurSecDiger` içinden `diger.Sec(null)` silindi | Pro_modda_Diger_menusu_alti_islemi_tasir_ve_panel_fisi_gizler | BİREBİR |
| M26-4 | PATCH gövdesi açıklamayı her zaman gönderiyor | Rezerv_duzenle_yalniz_degisen_alani_PATCH_eder_bosaltilan_null_gider · Rezerv_duzenleme_govdesi_yalniz_degisen_alanlari_tasir | BİREBİR |
| M26-5r | `FarkTurleri` listesine KART eklendi | Degisim_farki_kartla_kapanmaz_ve_govde_dogru_gider · Degisim_fark_turlerinde_KART_yok ² | BİREBİR |
| M26-6 | `SahipMudur` TEZGAHTAR'ı da kabul ediyor | Tezgahtar_icin_konsinye_ve_cek_devri_kilitli… · Tezgahtar_rezervi_kaldiramaz… · Tezgahtar_kaporayi_geri_odeyemez… | BİREBİR |
| M26-7 | Rezervde ürün zorunluluğu silindi | Rezerv_bos_gonderimde_istek_atmaz_alan_altina_yazar | BİREBİR |
| M26-8 | `RucuGerekceAsgari` 20 → 1 | Cek_devri_ve_karsiliksiz_donen_uclari_onayla_cagrilir | BİREBİR |
| M26-9 | Kapora `null` yerine `0` olarak sessizce gidiyor | Siparisten_satis_kalem_fiyatli_govde_ve_onayla_POST_eder | BİREBİR |
| M26-10 | Liste hatası boş listeye çevriliyor (UX-S28 ihlali) | Liste_hatasi_bos_duruma_cevrilmez | BİREBİR |
| M26-11 | Rücu yolu `/rucu` → `/geri` | P21_uclarinin_her_biri_ekran_istek_katmaninda_var · Cek_devri_ve_karsiliksiz_donen… | BİREBİR |
| M26-12r | `TekKasaId => null` (UX-S22) | Kapora_al_mahsup_ve_iade… · Siparisten_satis… · Degisim_farki… ³ | BİREBİR |
| M26-13 | Onay modalının "Evet" düğmesi istek atmıyor | Kapora_al… · Siparisten_satis… · Rezerv_kaldir… · Cek_devri… | BİREBİR |

**Ek açıklamalar:**

- **¹ M26-1:** Toplu koşuda mutasyon **yakalanmadı**, 21/21 yeşil kaldı. Aynı mutasyon elle tek başına yeniden koşulunca test **kırmızı** oldu: `Assert.Empty() Failure: Collection was not empty`. Günlük: `pS2-6/m1b.log`.
  - Olası neden **DOĞRULANMADI**: Toplu koşu sırasında aynı ağaçta başka ajanların eşzamanlı derlemesi sürüyordu.
- **² M26-5:** İlk koşuda yalnız saf test kırmızı oldu; ekran testi bu açığı yakalamadı. Açık kapatıldı: ekran testine "yalnız 2 seçenek" doğrulaması eklendi. Mutasyon yeniden koşulunca (M26-5r) ekran testi de kırmızı oldu.
- **³ M26-12:** İlk koşuda başka şeridin o anki derleme kırığı ölçümü engelledi: `Cariler.razor(227,64): error CS0103 'CariEylemleri'`. Ağaç yeniden derlenebilir hale gelince M26-12r olarak tekrarlandı.
- **Son durum:** Son mutasyondan sonra TEMİZ `dotnet build` alındı (0 hata) ve paket testleri 42/42 yeşil.

## 7. Denetim (`dotnet/denetim.sh`)

- **1. koşu** (paket kodu eklendikten sonra): **DENETİM TEMİZ.**
- **2. koşu** (sonda): 3 ✗ var, üçü de **aynı yabancı kökten**:
  - `Pages+Bilesenler+Layout record tanımı 4 (tavan 3)`
  - `P-10: BU_PAKETTE_EKLENEN_RECORD +1`
  - `P-02: OLCUM_TABANI_KOPYA 1 sapma`
  - Kök: başka şeridin yeni `Bilesenler/SatirEylemleri.razor:97 public sealed record Eylem(`. `grep -rn "record " … --include='*.razor'` bu paketin hiçbir razor dosyasında eşleşme vermiyor.
- **Bu paketin sayaçları:** hepsi ✓.
  - satır içi style 109 (tavan 196)
  - Pages ModalPencere 48 (tavan 55)
  - P-06 DUZENLENEMEYEN_GIRIS 3/3 · DUZENLEME_KALIBI_YOK_SAYFA 0 · ALAN_ANAHTARSIZ_DOGRULAMA 0 · BU_PAKETTE_EKLENEN_RECORD 0
  - P-16 BU_PAKETTE_EKLENEN_RECORD 0 (Islemler)

## 8. Canlı doğrulama (5192, `kuyumcu_net`)

Sürücü: `scratchpad/pS2-6/canli.sh` + `canli.py` (sır basmaz).

- Gövdeler `Islemler.Diger.cs`'in ekrandan gönderdiği alan kümesiyle birebir aynı.
- **Tur 1:** Şu sorunlar çıktı: evrak yanıt anahtarı yanlış okundu (`evrak`, betik hatası); adım 21 tevsik kapısında 400 aldı. Betik düzeltildi. Tur 1 kiracısı silindi.
- **Tur 2** aşağıda. Çıktılar: `canli-cikti.txt`, `canli-sql.txt`; tur 1 kopyaları `tur1-*`.

| # | Adım | Beklenen | Gerçek | Özet |
|---|---|---|---|---|
| 1 | POST /auth/kayit (PAKETS2-6-TEST) | 201 | 201 | jeton var |
| 2 | Kurulum: kur, kasa, 2 ürün (stok 20), 2 cari | hepsi var | 201/200/201/200/201/201 | hepsi var |
| 3 | Kurulum: POST /islemler/satis | 201 | 200 ⁴ | hareket var |
| 4 | POST /islemler/rezervler | 201 | 201 | satılabilir 16, bloke 2 |
| 5 | GET /islemler/rezervler?durum=AKTIF&limit=50 | 200, 1 satır | 200 | 1 satır |
| 6 | PATCH rezervler/{id} {adet:3} | 200, açıklama korunur | 200 | adet 3, açıklama 'vitrin arkası' |
| 7 | PATCH rezervler/{id} {aciklama:null} | 200, açıklama silinir | 200 | adet 3, açıklama None |
| 8 | POST rezervler/{id}/iptal (gerekçe) | 200, IPTAL | 200 | IPTAL, satılabilir 18 |
| 9 | GET /islemler/avans/{cariId} | 200, 0 | 200 | 0 |
| 10 | POST /islemler/avans (1.000 TL nakit) | 201, açık 1000 | 201 | 1000 |
| 11 | POST /islemler/avans/mahsup (400) | 200, açık 600 | 200 | 600 |
| 12 | POST /islemler/avans/iade (100, gerekçe) | 200, açık 500 | 200 | 500 |
| 13 | POST /islemler/konsinye-cikis (2) | 201 | 201 | satılabilir 18 |
| 14 | GET /islemler/konsinye-acik?limit=50 | 200, açık 2 | 200 | 2 |
| 15 | POST /islemler/konsinye-iade (1) | 200, kalan 1 | 200 | 1 |
| 16 | POST /islemler/degisim (fark nakit) | 201 | 201 | fark 2000, MUSTERI_ODER, kalan iade hakkı 1 |
| 17 | POST /islemler/degisim, fark KART (ekran sunmuyor) | 400 | 400 | "Değişim farkı bu sürümde NAKIT ya da VERESIYE ile kapanır" |
| 18 | Kurulum: POST /siparisler + ONAYLANDI→HAZIR | 2xx/200/200 | 200/200/200 | sipariş var |
| 19 | GET /siparisler?durum=HAZIR&limit=50 | 200, listede | 200 | listede |
| 20 | GET /siparisler/{id} | 200, kalem var | 200 | S-2026-000001 |
| 21 | POST satışa-çevir NAKIT 10.000 (günün nakit toplamı 23.100 iken) | 400 tevsik (ekran yasal kapıyı atlamaz) | 400 | "Tevsik zorunluluğu … 30.000,00 TL haddini 3.100,00 TL aşıyor … 459 s. VUK Genel Tebliği" |
| 21b | POST satışa-çevir açık hesap + kapora 200 | 201 | 201 | TESLIM_EDILDI, 1 kalem, düşülen kapora 200, açık kapora 300 |
| 22 | Kurulum: POST /evraklar (çek 5.000) | 201 | 201 | evrak var |
| 23 | GET /evraklar?durum=PORTFOYDE&limit=50 | izinli geçişlerde CIRO_EDILDI | 200 | [TAHSILE_VERILDI, TAHSIL_EDILDI, KARSILIKSIZ, CIRO_EDILDI] |
| 24 | POST /evraklar/{id}/ciro | 200 | 200 | CIRO_EDILDI |
| 25 | GET /evraklar?durum=CIRO_EDILDI&limit=50 | listede | 200 | listede |
| 26 | POST /evraklar/{id}/rucu, gerekçe 4 karakter | 400 | 400 | "Rücu gerekçesi en az 20 karakter olmalıdır" |
| 27 | POST /evraklar/{id}/rucu, gerekçe 37 karakter | 200 | 200 | KARSILIKSIZ |
| 28 | GET /_framework/Kuyumcu.Web.wasm (sunulan ekran) | 200, menü ve yol içinde | 200 | `islem-diger-ac`=True, `/islemler/konsinye-iade`=True |
| 29 | GET /islemler (SPA) | 200 | 200 | index.html var |

⁴ Satış ucu 201 değil **200** döndürüyor. Bu mevcut sözleşme; bu paket o uca dokunmadı. Beklenen değer tabloda yanlış yazılmıştı; sapma değil.

**SQL kanıtı** (tur 2 kiracısı; `canli-sql.txt`, `canli-sql-denetim.txt`):

```
rezervasyonlar : IPTAL · adet 3.000 · aciklama NULL=1 · kapanis_gerekcesi dolu
hareketler     : AVANS_ALINDI 1 (1000) · AVANS_MAHSUP 2 (-600 = 400 + sipariş 200) · AVANS_IADE 1 (-100)
                 KONSINYE_CIKIS 1 · KONSINYE_IADE 1 · SATIS 3 · SATIS_IADE 1 (değişim bacağı)
                 EVRAK_GIRIS 1 · EVRAK_CIRO 1 · EVRAK_RUCU 1 · EVRAK_KARSILIKSIZ 1
SATIS + siparis_id dolu : 1 · siparisler: S-2026-000001 TESLIM_EDILDI · kiymetli_evraklar: S26-CK-1 KARSILIKSIZ
denetim_kayitlari : Rezervasyon EKLEME 1 / GUNCELLEME 3 · Siparis EKLEME 1 / GUNCELLEME 3 · KiymetliEvrak 4/2 · Hareket 16/1
```

**Temizlik:**
- API (PID 62961) durduruldu. `lsof -nP -iTCP:5192 -sTCP:LISTEN` boş.
- İki test kiracısı `betikler/kiraci-temizle.sh --uygula` ile silindi (tur 2: 173 satır + tenants). İki davet satırı silindi; kalan 0.
- 90 saniye sonra geç yazım süpürmesi yapıldı: tenant_id kolonu olan tüm tablolarda iki kimlik için kalıntı **0**.
- **tenants:** başlangıçta 10, sonda 11. Artan 1 bu paketin değil: `PAKETP-61-TEST Kuyumculuk`, oluşturulma 02:41:50 UTC, P-61 şeridine ait. Bu paketin açtığı iki kiracının ikisi de silindi.
- QA-TEST-*, DEMO ve diğer kiracılara dokunulmadı. 5199 ve 5200 süreçlerine dokunulmadı.

## 9. Açık kalanlar (sahipleriyle)

Tam süitin 13 kırmızısı. Hiçbiri Islemler sayfasını ya da Fis/FisDiger bileşenlerini çizmiyor.

- **P-57 (Şerit E) · `Servisler/Bicim.cs` tr-TR has biçimi değişikliği.** Beklenen "12,5000 has", çıkan "12.5000 has" gibi. Etkilenen testler:
  - BicimTestleri.Has_TurkceVirgulOndalikBasar
  - P57.RaporEkranTestleri.HasVeGramHucreleri_NoktaOndalikIcermez
  - EmanetEkranTestleri.Elimizdeki_emanet_ve_hareketler_gosterilir
  - S35EmanetEkranTestleri.S35_14_…
  - UX1C3EkranTestleri.C3_41_…
  - UX4cEkranTestleri.M10_… (emanet)
- **`Bilesenler/SatirEylemleri.razor`'u açan şerit** (yeni razor record `Eylem`; orkestratör kaydında adı yok):
  - SozlesmeYansimaTestleri.Razor_code_blogunda_istemci_record_tanimi_acilmiyor
  - OlcumKapisiTestleri.SAYFA_RECORD_TAVAN_genis_kapsamda_ve_olculen_degere_esit
  - OlcumKapisiTestleri.P02_blogundaki_her_sayac_olcum_komutu_tasir_ve_taban_surumlu
  - denetim.sh'taki 3 ✗ de aynı kökten.
- **Fiyatlandirma / Vitrin / GiderPusula / Emanet sayfalarını yeniden yazan şeritler** (bu sayfalar ağaçta yeni dosya olarak duruyor):
  - FiyatlandirmaUX4aEkranTestleri.Pasife_al_TAM_govdeyle_PATCH_atar
  - UX1C2EkranTestleri.Fiyatlandirma_pasife_alinca_KENDI_mesajini_yazar
  - P17VitrinEkranTestleri.Ozel_kalemde_varsayilana_don_ACIK_ve_istek_atar (Şerit D: Vitrin.razor)
  - GiderPusulaEkranTestleri.Satirda_DuzenleEylemiVar (Şerit C: GiderPusulalari)
- **Bu paketin kendi açıkları:**
  - Tarayıcıda uçtan uca tıklama yapılmadı. Test kullanıcısının parolasını bir giriş alanına yazmak ajan kuralıyla yasak (S2-5 ve S2-5b ile aynı durum). Ekranın varlığı canlı 28'de (sunulan wasm), davranışı 21 bUnit testinde kanıtlı.
  - Siparişten satışta ödeme **tek bacak** (nakit, kart, havale ya da açık hesap). Karma ödeme bu panelde yok; sunucu çok bacağı kabul ediyor. Karma ödeme gerekirse FisOdemeBacaklari bileşeni bu panele bağlanmalı; ayrı iş, orkestratöre not.
  - KART araçlı sipariş dönüşümünde POS cihazı seçimi yok. Sunucu cihaz isterse 400 döner ve mesaj banda yazılır; ekran sessiz kalmaz.

## 10. DoD sözleşmesi

**Bu paket yeni bir varlık ya da kayıt türü doğurmadı** (sunucu değişmedi). Mevcut P-21 kayıtlarının ekrandaki düzenleme yolu:

| Varlık / kayıt | Ekrandaki yol | Dayanak |
|---|---|---|
| Rezervasyon | Düzenle → **Cekmece** (PATCH: gönderilmeyen alan korunur, null gelen alan silinir). "Ayırmayı kaldır": gerekçe + ortak onay → /iptal (SAHIP/MUDUR). Kapanmış kayıtta DegismezIpucu. | DuzeltmePolitikasi `D("Rezervasyon", …/rezervler/{id})` |
| AVANS_ALINDI (kasa hareketi) | Salt okunur; DegismezIpucu → doğru yol "Kaporayı geri öde" (/avans/iade, ortak onay) | `T("AVANS_ALINDI", Defter, …/avans/iade)` |
| AVANS_MAHSUP / AVANS_IADE | Ters kayıt ucu yok (politika StornoYok). Ekranda düzenle düğmesi yok. | `T(…, StornoYok)` |
| KONSINYE_CIKIS | Salt okunur; DegismezIpucu → doğru yol satır başına "Geri al" (/konsinye-iade) | `T("KONSINYE_CIKIS", Defter, …/konsinye-iade)` |
| Değişim fişi (SATIS_IADE + SATIS) | Doğan satış son satışlar listesinde görünüyor; o kartın mevcut DegismezIpucu ve iade yolu geçerli | S2-1 FisSonSatislar |
| Siparişten doğan satış | Aynı: son satışlar listesine düşüyor (sayfa tazeleniyor) | — |
| EVRAK_CIRO | Geri alma yolu aynı panelde: "Karşılıksız dönen" (/rucu, gerekçe ≥ 20, onay) | `T("EVRAK_CIRO", Defter, …/rucu)` |
| EVRAK_RUCU | Düzeltme yolu Evraklar ekranındaki tahsil-edildi | `T("EVRAK_RUCU", Defter, …/tahsil-edildi)` |

- **Finansal kayıtlarda hard DELETE yok.** Bu paketin açtığı bir silme ucu yok.
- **Yetki:** sunucu `[ModulYetkisi]` ve `[RolGerekli]` olduğu gibi kaldı. Ekran yalnız kilitli düğmenin sebebini söylüyor.
- **Denetim kaydı:** sunucu yazıyor (canlı SQL: Rezervasyon EKLEME 1 / GUNCELLEME 3).

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Her P-21 ucu ekrandan çağrılabilir (UX-S25) | ✓ | 15/15 · yansıma testi + 8 bUnit uç testi + M26-11 + canlı 4–27 |
| Basit modda Diğer DOM'da yok | ✓ | Basit_modda_… + M26-1 |
| Pro modda 8 sekme + "Diğer ▾" | ✓ | Pro_modda_… · taslak n5 |
| Tek POST yolu, Fis bileşeni istek atmaz | ✓ | S21 yapı testi + Fis_Diger_bilesenleri_istek_atmaz… |
| Islemler.razor ≤ 1.400, id silinmedi | ✓ | 1399 satır · S21 id envanteri yeşil · değişen satırlarda id yok |
| PATCH sözleşmesi + testi | ✓ | Rezerv_duzenle_… + M26-4 + canlı 6/7 |
| Her düğme istek atar ya da disabled + sebep | ✓ | Tezgahtar_* testleri, `#…-kilit` notları, kur yoksa bant |
| Boş/geçersiz gönderimde alan altı mesaj | ✓ | Rezerv_bos_… · M26-7 · Konsinye/Kapora/Evrak hata id'leri |
| Yıkıcı eylem = ortak onay | ✓ | M26-13 (4 test kırmızı) |
| UX-S22 tek kasa | ✓ | `#…-kasa-tek` · M26-12r |
| UX-S27 sonuç bandı | ✓ | başarı bandı sunucu değerini yazıyor (adet, açık kapora, fark, sipariş no) |
| UX-S28 hata ≠ boş durum | ✓ | Liste_hatasi_… + M26-10 |
| UX-S29 sınır | ✓ | `sinirAsildi` / `dahaFazlasiVar` → "İlk N kayıt" bandı |
| UX-S31/S5B jargon 0, TR+EN | ✓ | Sozlukler.FisDiger.cs · sözlük testi |
| UX-S32 para birimi | ✓ | `Bicim.Para(…, "TRY")` / evrakta kendi para birimi |
| Yetki ≠ mod, eşik istemcide yok | ✓ | canlı 21 (tevsik 400) · 17 (KART 400) · 26 (gerekçe 400) |
| Sabit renk / inline style / CDN yok | ✓ | FisDiger* style=0 · denetim style 109 |
| Paket kapısı (1)–(7) | ✓ | § 5–9 |

## 12. Kurucuya ve mali müşavire notlar

- **Mevzuat hükmü yazılmadı.** Canlı 21, 459 sıra no'lu VUK Genel Tebliği tevsik kapısının bu yeni ekrandan da işlediğini gösteriyor. Hüküm ve eşik sunucuda, yapılandırmadan okunuyor. Mali müşavirden istenen yeni bir teyit yok.
