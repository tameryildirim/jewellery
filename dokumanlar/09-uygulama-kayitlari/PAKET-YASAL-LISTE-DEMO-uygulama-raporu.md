# PAKET YASAL-LISTE-DEMO — uygulama raporu (02.10.2026 · t9 · API 5188 / vekil 5189) — TUR 2

Rol: Taşkın (backend) + Ertuğ (frontend ölçümü). COMMIT YOK.

**Kapı durumu (orkestratör kaydı için önerilen ifade): MEKANİZMA BİTTİ / VERİ KURUCU KARARINDA.**

Demo dükkânında Panel'deki "Yasal liste henüz yüklenmedi" bandı **GÖRÜNMEYE DEVAM EDECEK**. Nedeni: sistemde birincil kaynaktan yüklenmiş bir liste yok (`kuyumcu_net.yaptirim_kayitlari` = 0). Bant doğru bir uyarıdır.

Kuyruk maddesi (01.10 15:40 "müşteri görür") **kapanmış sayılmamalı**. Kapanması için § 14'teki kurucu kararı gerekir.

## 0. Tur 2 — önceki doğrulamanın bulguları ve kapanışı
| Bulgu | Önem | Ne yapıldı | Kanıt |
|---|---|---|---|
| Ağaçtaki Debug ikilisi fail-open çalışıyordu (liste yokken `listeEski:false`) | KRİTİK | Ağaç kaynaktan yeniden derlendi (`dotnet build Kuyumcu.sln`, 0 hata). Canlı API bu ikiliyle koşuldu: yeni kiracıda **iki uç da `listeTarihi:null, listeEski:true`** döndü. Demo betiği artık bu tutarsızlığı **hata ile durduruyor**: liste tarihi yokken `listeEski=false` gelirse "TUTARSIZ … API ikilisini kaynaktan yeniden derleyin" diyor. Eskiden "SISTEMDE_GUNCEL" yazıp demoyu düzelmiş gibi gösteriyordu. | § 7 L3/L5, M2-4/M2-5 |
| Uç düzeyinde "liste yokken listeEski=true" testi yoktu | KRİTİK | İki yeni test eklendi:<br>(a) `Liste_yokken_ozet_listeEski_true_tarih_null_fail_closed`. Bir işlem (transaction) içinde tablo boşaltılır, gerçek `YaptirimServisi.Ozet` aynı bağlantıda okunur, sonra işlem geri alınır. Beklenen: `listeTarihi=null`, `listeYasiGun=null`, `listeEski=true`, `yururlukteSatir=0`.<br>(b) `Eski_liste_bayragi_yaptirim_ve_uyum_ozet_ucuna_tasinir`. HTTP ile, ileri tarihli gün dikişi (`YaptirimOzetGunu`) kullanılarak: **/masak/yaptirim ve /masak/uyum-ozet** ikisi de `listeEski=true` ve 14 günden büyük yaş döner. | M2-1 (`listeEski = eski` → `false`) iki testi de kırmızıya çevirdi. M2-2 (uyum-ozet eşlemesi → `false`) (b)'yi kırmızıya çevirdi. |
| Ozet'in bayrağı kullandığını ölçen mutasyon yaşıyordu (D3) | ORTA | D3'ün birebir aynısı olan M2-1 artık **ölüyor**. | § 5 |
| Demo bandı hâlâ ilk ekranda, önbellek yok | KRİTİK / ORTA | **Uydurma liste girilmedi. İnternetten dosya indirilmedi.** BM/MASAK dosyasını indirmek dış bir eylemdir ve alt ajan olarak kurucu onayım yok. Dönüştürücü de hakemin önerisiyle ayrı bir küçük paket olmalı. Kurucuya açıkça **"demoda bu bant görünecek"** diye yazıldı (§ 13, § 14). | § 7 L2, § 8 |
| Rapordaki build ve süit sayıları ağacın o anki hâliyle tutmuyordu | DÜŞÜK | Bu turda gerçekten ölçülen değerler aşağıda. Başka şeritlerin kırmızıları sahipleriyle birlikte § 13'te. | § 4, § 6 |

**Hakem bulgusundaki bayat ikilinin olası kaynağı (kanıtlanmadı, yalnız gözlem):** 06:23 ikilisinin yanıtı (`listeTarihi:null` iken `listeEski:false`), hakemin kendi D3 mutasyonunun (`listeEski = eski` → `false`) ürettiği davranışla **birebir aynı**. Kaynak geri alındıktan sonra ikili yeniden derlenmemiş olabilir.

Bu tur kendi mutasyonlarından sonra temiz derleme yaptı (§ 5). Betik ve testler artık böyle bir ikiliyle karşılaşınca hata veriyor ya da kırmızıya dönüyor.

## 1. Kök neden (dosya:satır)
- **Hangi liste:** 6415 sayılı Kanun kapsamındaki malvarlığı dondurma (yaptırım) listesi. Tablo: `yaptirim_kayitlari` (`dotnet/Kuyumcu.Api/Data/Yaptirim.cs:26`, `YaptirimKaydi`).
- **Kiracıya bağlı mı:** HAYIR, **kiracı-üstü (sistem geneli)**. `IKiraciVarlik` değil (`KuyumcuDbContext.cs:924` beyaz listesinde). Kiracıya bağlı bir "son yükleme" kaydı yok.
- **Nereden yazılır:** Tek yol `POST /api/v1/sistem/yaptirim-listesi` (`Modules/Sistem/SistemModulu.cs:552` → `YaptirimServisi.ListeGuncelle`). Zamanlanmış bir indirici yok.
- **Bant neden çıkıyor:** `YaptirimServisi.Ozet` (`YaptirimServisi.cs:415-…`) en yeni `liste_tarihi`'ni okur ve `ListeDurumu` (`:129-134`) ile değerlendirir. Liste yoksa sonuç `(null, Eski=true)` olur (fail-closed). `kuyumcu_net` tablosunda `count(*)` = 0 (bu turda başta ve sonda ölçüldü).
- **Sonuç:** Kusur yeni kiracıya özgü değil; liste sistemde hiç yüklenmemiş. **Bant doğru uyarıdır, gizlenmedi.**

## 2. Yapılanlar (tur 1 + tur 2)
1. `dotnet/Kuyumcu.Api/Modules/Masak/YaptirimServisi.cs`
   - (t1) Saf `ListeDurumu(son, bugun)` eklendi.
   - (t1) `BirincilKaynakKonaklari` ve `BirincilKaynakMi` eklendi. `ListeGuncelle` birincil olmayan konakta 400 verir.
   - (t2) İmza `Ozet(db, tenantId, DateOnly? bugun = null)` oldu. `bugun` yalnız testlerden gelir; null ise sistem işlem günü kullanılır, yani üretim davranışı aynı kaldı.
   - (t2) Yeni DI dikişi: `public sealed record YaptirimOzetGunu(DateOnly Bugun)`. **Üretimde kayıtlı değil.**
2. `dotnet/Kuyumcu.Api/Modules/Masak/SaklamaYaptirimModulu.cs` (t2): `GET /masak/yaptirim` ve `GET /masak/uyum-ozet`, `OzetGunu` değerini `HttpContext.RequestServices.GetService(YaptirimOzetGunu)` ile okuyup Ozet'e geçirir. Üretimde bu değer null'dır. Rol öznitelikleri değişmedi.
3. `dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js`
   - (t1) Yasal liste adımı ve önbellek doğrulaması eklendi. Uydurma liste YOK.
   - (t2) Saf `yasalListeDurumu(ozet)` eklendi:
     - liste tarihi yok → `YUKLENMEDI`
     - liste tarihi yok ama `listeEski=false` → **hata (TUTARSIZ)**
     - liste tarihi var, `listeEski=false` → `SISTEMDE_GUNCEL`
     - diğer durumlar → `ESKI`
   - (t2) Yükleme sonrası denetim de bu fonksiyonu kullanıyor.
4. `dotnet/Kuyumcu.Tests/YasalListeDemoTestleri.cs`: 17 testten **20 teste** çıktı (t2'de +3).
5. Ekran (Panel.razor, UyumYasalBantlari.razor, Sozlukler.YasalListe.cs): **değişmedi**. Bant metni TOPLU-3'te sahibe yönelik sade eylem diline zaten alınmıştı. Dokunulan sayfa YOK, yalnız ölçüm yapıldı.
6. Tarama ve eşleşme (tarama noktaları 1–5, kilit, bildirim) **DEĞİŞMEDİ**.

### Sapmalar ve gerekçe
- Spesifikasyonda "Birleşik koşullar" başlığı yok. Bağlayıcı kaynak olarak kuyruğun `01.10 15:40` ve `02.10 05:57` maddeleri ile görev metnindeki 5 adım alındı.
- Test dikişi (`YaptirimOzetGunu`) üretim koduna eklendi. Gerekçe: tablo kiracı-üstü ve test veritabanında paylaşılıyor. Eskilik bayrağının uca taşındığını HTTP ile deterministik ölçmenin tek yolu gün dikişi. Dikiş üretimde kayıtlı değil, davranışı değiştirmez.
- Demo veritabanına liste **yüklenmedi** (§ 0, § 14).

## 3. Migration
Gerekmedi (şema değişmedi).

## 4. Testler
| Koşum | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| Filtreli t2 (YasalListeDemo + Yaptirim + Yasal3 + YASAL3 + Toplu3) | 115 | 115 | 0 | 0 |
| YasalListeDemo (adlarıyla, -v n) | 20 | 20 | 0 | 0 |
| Tam süit t2 (`/private/tmp/kuyumcu-serit/trx-pYASAL-LISTE-DEMO-t2/_Mac_2026-10-02_06_54_06.trx`) | 9176 | 9169 | 5 | 2 |

- Tam süitteki 5 kırmızının **hiçbiri bu paketin dosyalarında değil** (§ 13).
- Bu paketin ürettiği yeni kırmızı: **0**.
- Tur 2'de eklenen testler:
  - `YasalListeDemoHttpTestleri.Liste_yokken_ozet_listeEski_true_tarih_null_fail_closed`
  - `YasalListeDemoHttpTestleri.Eski_liste_bayragi_yaptirim_ve_uyum_ozet_ucuna_tasinir`
  - `YasalListeDemoBetikTestleri.Betik_liste_tarihi_yoksa_yuklenmedi_der_fail_open_yaniti_hata`

## 5. Mutasyonlar
Yöntem: yedek `/private/tmp/kuyumcu-serit/YASAL-LISTE-DEMO/mut-yedek/` altında; değişiklik perl ile tek eşleşmeli yapıldı. Her mutasyon geri alındıktan sonra `cmp` sonucu AYNI çıktı.

| Kod | Değişiklik | Kırmızı test(ler) | Geri alındı |
|---|---|---|---|
| M1-1 (t1) | `ListeDurumu`: null → `(null,false)` | Hic_yuklenmemis_liste_eski_sayilir_fail_closed | cmp AYNI |
| M1-2 (t1) | `gun > EskiListeGun` → `>=` | Ondort_gunluk_liste_guncel_onbes_gunluk_eski | cmp AYNI |
| M1-3 (t1) | birincil kaynak kapısı devre dışı | Birincil_olmayan_kaynak_400_satir_eklenmez | cmp AYNI |
| M1-4 (t1) | konak eşleşmesi `StartsWith` | Birincil_kaynak_konagi(…ornek.com…) | cmp AYNI |
| M1-5 (t1) | Ozet listeyi kiracıya bağlıyor | Yuklu_sistem_listesi_yeni_kiraciya_gorunur_bant_cikmaz | cmp AYNI |
| M1-6 (t1) | betikte konak denetimi devre dışı | Birincil_olmayan_konak_reddedilir | cmp AYNI |
| M1-7 (t1) | betiğin konak listesine ornek.com eklendi | Betik_konak_listesi_sunucuyla_ayni, Birincil_olmayan_konak_reddedilir | cmp AYNI |
| **M2-1** | `YaptirimServisi.Ozet`: `listeEski = eski,` → `listeEski = false,` (hakemin D3'ü) | **Eski_liste_bayragi_yaptirim_ve_uyum_ozet_ucuna_tasinir, Liste_yokken_ozet_listeEski_true_tarih_null_fail_closed** | cmp AYNI |
| **M2-2** | `UyumOzet`: `listeEski = y.GetProperty("listeEski").GetBoolean(),` → `false` | **Eski_liste_bayragi_yaptirim_ve_uyum_ozet_ucuna_tasinir** | cmp AYNI |
| **M2-3** | `ListeDurumu`: `return (null, true)` → `(null, false)` | Hic_yuklenmemis_liste_eski_sayilir_fail_closed, **Liste_yokken_ozet_listeEski_true_tarih_null_fail_closed** | cmp AYNI |
| **M2-4** | betik: TUTARSIZ hatası kapatıldı (`if (false)`) | **Betik_liste_tarihi_yoksa_yuklenmedi_der_fail_open_yaniti_hata** | cmp AYNI |
| **M2-5** | betik: `!ozet \|\| !ozet.listeTarihi` → `!ozet` (yalnız bayrağa güven) | **Betik_liste_tarihi_yoksa_yuklenmedi_der_fail_open_yaniti_hata** | cmp AYNI |

- Son mutasyondan sonra temiz `dotnet build Kuyumcu.sln`: **0 Uyarı 0 Hata** (`t2/build2.log`).
- İlk t2 derlemesinde 6 uyarı vardı; hiçbiri bu paketin dosyası değil:
  - başka şeritlerin test dosyaları: SUBEKAPSAMASinirTestleri CS8601, KesifP2Testleri xUnit2029, ParaDemo2EkranTestleri xUnit2013 ×2 ve CS8631
  - eşzamanlı derlemede MSB3026 kopyalama yarışı

## 6. Denetim
`bash dotnet/denetim.sh`: 303 ✓, 1 ✗.
- Tek ✗: `P-30: DEMO_INSAN_SURESI (BK-45)`. Kabul ölçütünde hariç tutulan madde.
- Bu paketin sayacı kırmızı değil (`t2/denetim.log`).

## 7. Canlı doğrulama (API 5188, yeni derlenmiş ikili, `kuyumcu_net`)
| Adım | Beklenen | Gerçek |
|---|---|---|
| Başlangıç SQL | — | tenants=17 · yaptirim_kayitlari=0 |
| L2 `demo-tohumla.js --prova-kiraci` (önbelleksiz). Kiracı: `PAKET-YASAL-LISTE-DEMO-T2-TEST` (01M3XBH9BKM9K2M8H04AFZZW7E) | yükleme yok, durum rapora yazılır | kod 0 · `yasalListe={"durum":"YUKLENMEDI","listeTarihi":null,"listeEski":true}` · stderr: "Panel'de yasal liste bandı görünecek (doğru uyarı)." |
| L3 SAHİP `GET /masak/uyum-ozet` | 200, listeEski=true, tarih null | **200** · `listeTarihi=null, listeEski=true, listeYasiGun=null, acikTam=0` |
| L3b SAHİP `GET /masak/yaptirim` | 200, listeEski=true | **200** · `listeTarihi=null, listeEski=true, listeYasiGun=null, yururlukteSatir=0` |
| L1 `POST /sistem/yaptirim-listesi`, kaynakUrl=https://example.com/liste | 400, satır eklenmez | **400** "Kaynak adresi birincil kaynak olmalı (MASAK, Resmî Gazete ya da BM Güvenlik Konseyi listesi)." · SQL yaptirim_kayitlari=0 · kiracının eşleşmesi=0 |
| L5 betik `yasalListeDurumu({listeTarihi:null,listeEski:false})` | hata | `HATA TUTARSIZ: sunucu liste tarihi yokken listeEski=false döndü (fail-open) …` · `{null,true}` → `YUKLENMEDI` |
| Bitiş | kiracı silinmiş, portlar boş | `zsh kiraci-temizle.sh 01M3XBH9… --uygula` kod 0 · tenants=**17** · kalan=0 · yaptirim_kayitlari=0 · 5188/5189/9981/9982 portlarında dinleyen yok |

## 8. Gerçek tarayıcı ölçümü
Başsız Chrome; vekil 5189; `tur/tur.mjs` (CDP 9981) ve bant ölçeri `t2/bant/bant.mjs` (CDP 9982); rota `/` = Panel.

| Mod | Tema | Gen. | tur: geçerli · taşma/binme/kırık/gizli-görünen/TRY/yatay | Bant `#uyb-liste`: görünür · konum · boyut · sağ taşma · kesik |
|---|---|---|---|---|
| BASIT | açık | 1280 | ✓ · 0/0/0/0/0/0 | görünür · y=181 · 964×45 · −24 · 0 |
| BASIT | açık | 375 | ✓ · 0/0/0/0/0/0 | görünür · y=300 · 351×87 · −12 · 0 |
| BASIT | koyu | 1280 | ✓ · 0/0/0/0/0/0 | görünür · y=181 · 964×45 · −24 · 0 |
| BASIT | koyu | 375 | ✓ · 0/0/0/0/0/0 | görünür · y=300 · 351×87 · −12 · 0 |
| PRO | açık | 1280 | ✓ · 0/0/0/0/0/0 | görünür · y=165 · 964×45 · −24 · 0 |
| PRO | açık | 375 | ✓ · 0/0/0/0/0/0 | görünür · y=301 · 351×87 · −12 · 0 |
| PRO | koyu | 1280 | ✓ · 0/0/0/0/0/0 | görünür · y=165 · 964×45 · −24 · 0 |
| PRO | koyu | 375 | ✓ · 0/0/0/0/0/0 | görünür · y=301 · 351×87 · −12 · 0 |

- Bant metni 8 kombinasyonda da aynı: "Yasal liste henüz yüklenmedi. Yüklenmesi için destek hattını arayın. Ayrıntı →".
- `analiz.py`: 8 sayfa, hata 0, geçersiz 0. Tek bulgu **K4 tren 2.5×5 (BASIT 375)**: Panel'in genel uzunluğu, bandın dışında (TOPLU-4).
- Dar sütun: bant ana alanın %98'ini kaplıyor (K2 97–98).
- Read ile görülen görüntüler (`/private/tmp/kuyumcu-serit/YASAL-LISTE-DEMO/` altında):
  - `t2/tur/foto/kok__BASIT_aydinlik_1280.png`, `t2/tur/foto/kok__PRO_aydinlik_1280.png`
  - `t2/tur/kok__BASIT_karanlik_375-kucuk.png`, `t2/tur/kok__PRO_karanlik_375-kucuk.png`, `t2/tur/kok__BASIT_karanlik_1280-kucuk.png`
  - `t2/bant/panel__PRO_aydinlik_375.png`, `t2/bant/panel__PRO_karanlik_1280.png`

## 9. Mevzuat / birincil kaynak
- 6415 s. Kanun: https://www.mevzuat.gov.tr/MevzuatMetin/1.5.6415.pdf
- MASAK listeleri (md.5/6/7): https://masak.hmb.gov.tr/bkk-ile-malvarliklari-dondurulanlar (sunucu yalnız başlık döndürdü)
- MASAK Uygulama Rehberi: https://ms.hmb.gov.tr/uploads/sites/12/2022/04/Uygulama-Rehberi_2022.pdf
- BMGK konsolide listesi: https://scsanctions.un.org/consolidated/
- 14 günlük eskilik eşiği mevzuat değil, iç politikadır; değiştirilmedi. Bu turda yeni mevzuat hükmü yazılmadı.

## 10. DoD sözleşmesi
- Yeni varlık ya da kayıt türü YOK.
- `yaptirim_kayitlari` kiracı açısından salt okunurdur: ulusal listedir, yazma yalnız sistem ucundan yapılır, satır silinmez, kaldırma damgalanır. Gerekçe Uyum → Yasal liste panelinde yazılı.
- `YaptirimOzetGunu` bir kayıt türü değil, test dikişidir.
- Yeni düğme yok.

## 11. Değişen dosyalar
Bu paketin dosyaları. `git status --porcelain | grep -v -E "/(bin|obj)/"` bunları `??` olarak gösterir; depoda yalnız ilk commit var.
- `dotnet/Kuyumcu.Api/Modules/Masak/YaptirimServisi.cs` (t1+t2)
- `dotnet/Kuyumcu.Api/Modules/Masak/SaklamaYaptirimModulu.cs` (t2; yalnız `OzetGunu` dikişi)
- `dotnet/Kuyumcu.Tests/YasalListeDemoTestleri.cs` (yeni; t2'de +3 test)
- `dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js` (yasal liste adımı + `yasalListeDurumu`)
- `dokumanlar/09-uygulama-kayitlari/PAKET-YASAL-LISTE-DEMO-uygulama-raporu.md` (bu rapor)

## 12. Koşul karşılama
| Koşul | Durum |
|---|---|
| (1) Hangi liste, nereden, kiracıya bağlı mı | ✓ § 1 |
| (2) Yeni kiracıda neden "yüklenmedi" | ✓ Tablo boş (SQL 0), kiracıya bağlı kayıt yok |
| (3) Sistem listesi yeni kiracıya görünür; liste yoksa bant kalır | ✓ HTTP testi + M1-5. Canlıda iki uç fail-closed (L3/L3b). Bant 8 kombinasyonda görünür. |
| (4) Yalnız birincil kaynak; uydurma yok; önbellek varsa demo adımı | Mekanizma ✓: sunucu kapısı + betik adımı. Veri ✗: **önbellek yok, demoda bant görünecek; kurucu kararı gerekiyor** (§ 14). |
| (5) Tarama değişmedi, fail-closed kalıyor | ✓ Tarama testleri yeşil; uç düzeyinde fail-closed testleri eklendi (M2-1/2/3) |
| Build 0/0 · denetim (P-30 hariç) · tam süitte 0 yeni kırmızı | ✓ (§ 5, § 6, § 13) |
| Chrome 1280/375 · BASIT/PRO · açık/koyu | ✓ § 8 |
| Bellek: en çok 1 Chrome + 1 API, iş bitince kapatıldı | ✓ (Chrome'lar sırayla, tek tek açıldı) |

## 13. Açık kalanlar
- **Demo ve canlı veritabanında liste BOŞ.** Panel bandı demoda **görünmeye devam eder** (doğru uyarı). Kuyruk maddesi 01.10 15:40 KAPANMADI: MEKANİZMA BİTTİ / VERİ KURUCU KARARINDA.
- Tam süitte başka şeritlerin 5 kırmızısı var; hiçbiri bu paketin dosyalarına dokunmuyor:
  - `ParaDemo2KasaEkranTestleri.PD2E_2_Gunu_Kapat_acilista_sayilan_BOS_fark_YOK_kilit_sebebi_TEK_ve_altlikta` → PARA-DEMO (Kasalar.razor)
  - `P36c2GocBitisTestleri.C36c2_1_Serbest_ekranlarda_ham_select_KALMAZ` ("ham <select>: FisVerdimAldim.razor") → PARA-DEMO (Fiş)
  - `TS1TuvalSatisTestleri.Aldim_tek_odeme_satiri_Nakit_kasa_tutar_Fark_alt_cubukta_onayli` → PARA-DEMO (Fiş/Satış)
  - `KesifSozlukTaramaTestleri.P8_04_karsilik_listesi_PROdan_farkli_yer_tutucu_ayni_cift_yol_yok` (RaporSeti.SinirUyarisi) → PARA-DEMO / TOPLU-4 (Raporlar)
  - `KatalogK1Testleri.KAT_K8_02_Basit_katmanin_her_TR_metni_jargonsuz_ve_prodan_farkli` (Uyum_RiskCariSecilmedi; Uyum.razor 06:25'te değişti) → TOPLU-4 (Uyum görünüm)
- K4 tren (Panel 375 BASIT uzunluğu) → TOPLU-4.
- `denetim.sh` P-30 DEMO_INSAN_SURESI ✗ → P-30 sahibi.
- Gözlem (bu paketin alanı değil): Panel'de bant varken de "Bekleyen iş: Başka iş yok. Yasal uyarılar üstteki kutularda" yazıyor.

## 14. Kurucu / mali müşavir
- **Kurucu — liste kaynağı (KARAR GEREKİYOR):** Demodan önce bandın kalkmasının tek doğru yolu birincil kaynaklı gerçek bir listedir. Önerilen adımlar:
  1. Kurucu onayı.
  2. Ayrı bir küçük paket: BM konsolide XML (`https://scsanctions.un.org/resources/xml/en/consolidated.xml`) ya da MASAK listesini `POST /sistem/yaptirim-listesi` gövdesine çeviren bir dönüştürücü.
  3. Bu dönüştürücüyle yerel önbellek dosyası üretmek.
  4. `KUYUMCU_YAPTIRIM_ONBELLEK=<dosya> node demo-tohumla.js …` ile yüklemek.

  Yüklemeden sonra betik sunucudan `listeEski=false` okuyamazsa başarısız olur.
- **Bu karar gelmezse demoda Panel'in üstünde "Yasal liste henüz yüklenmedi. Yüklenmesi için destek hattını arayın." bandı görünecek.**
- **Mali müşavir / hukuk:** 6415 md.5'e göre BM listesindeki kişiler için Türkiye'de hukuki etki, Cumhurbaşkanı kararının Resmî Gazete'de yayımıyla başlar. Bu nedenle RG tarihi bilinmeyen satırlarda 30 günlük bildirim süresinin liste tarihinden başlatılması (bugünkü tutucu davranış) teyit edilmeli.
