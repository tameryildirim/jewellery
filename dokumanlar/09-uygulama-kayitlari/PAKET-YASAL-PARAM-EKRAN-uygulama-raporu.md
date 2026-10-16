# PAKET YASAL-PARAM-EKRAN — Uygulama raporu

**Konu:** Yasal sınırlar tek kaynak (tablo) + bugünden geçerli değişiklik ekrandan (Resmî Gazete künyesi zorunlu, gerekçe, iki adımlı onay, denetim izi) + ByGatech merkezi yönetim / kuyumcu salt okunur. Ek kapsam: MASAK-370 tasarım hakemi bulguları (kimlik eşiği metin birliği) ve hasımın DÜŞÜK maddeleri.
**Tarih:** 8 Ekim 2026 · **Rol:** Taşkın (backend) + Ertuğ (frontend) · Opus · şerit t2 (API 5162, vekil 5163, CDP 9905/9906, test DB `kuyumcu_test_t2`)
**Durum (tur 3, 08.10 06:45):** BİTTİ — önceki doğrulamanın 8 bulgusu (1 KRİTİK + 2 ORTA + 5 DÜŞÜK; 1 ORTA kapsam dışı) kapatıldı; ayrıntı **§ 15 TUR 3** (önceki bölümlerin üzerine yazar). Tam süit 10.925 / 1 kırmızı (başka şeridin: `IkonTestleri` ← `Grid/Izgara.razor`, GRID şeridi); bu paketin yeni kırmızısı 0.
**Durum (tur 2, 08.10 04:55):** BİTTİ — önceki doğrulamanın 8 bulgusu (3 KRİTİK + 5 DÜŞÜK) kapatıldı; ayrıntı **§ 14 TUR 2** (bu bölüm önceki bölümlerin üzerine yazar). Tam süit 10.854 / 0 kırmızı. Tur 1 durumu: BİTTİ (§ 6). Sır olayı § 12 md.1'de açıkça yazılıdır. COMMIT YOK. Silinen dosya yok.

## 1. Spesifikasyon ↔ kod (mevcut durum doğrulaması)

| İddia (MEVZUAT-KUYUMCU-BELGE § 3 / görev) | Koddaki gerçek (08.10) | Sonuç |
|---|---|---|
| `/yasal-sinirlar` bugün başlayan değeri girdirmiyor | `YasalParametreServisi.Ekle` `bas <= bugun` → 409 | Doğru → değiştirildi (bugün serbest, geçmiş 400) |
| Aynı değer appsettings'te; açılışta farklıysa sunucu açılmıyor | `Program.cs` YASAL_PARAM_AYRISMA → `throw` | Doğru → tablodan okunan anahtarda uyarıya çevrildi |
| Eşik yalnız tablodan okunuyor | `MasakKuralMotoru.EsiklerGunu` tablodan; `MasakEsikleri` yalnız veritabanısız kurulum | Doğru; **MasakKuralMotoru'na dokunmak gerekmedi** |
| Sistem yöneticisi kimliği | Rol YOK; `SistemAnahtariKapisi` (X-Sistem-Anahtari + X-Sistem-Operator), JWT ile geçilemez | Yeni rol icat edilmedi; bu kapı kullanıldı |
| Ekranda sistem yöneticisi arayüzü | Hiç yok (MÜHÜR 4 K1: menüye/rol listesine dokunulmadı) | Form `/yasal-sinirlar?yonetim=1` altında açıldı (§ 2.4) |
| "Birleşik koşullar" bölümü | Kaynak belgede bu başlık YOK (5 bölüm) | Bağlayıcı koşullar görev metnindeki EK KOŞULLAR'dan alındı |

## 2. Yapılanlar

### 2.1 Tek kaynak (Bertan + Mehmet oyu)
- **Karar:** Mevzuat eşiklerinin tek kaynağı `yasal_parametreler` tablosudur. appsettings kopyaları **kaldırılmadı** (5199/5200 yayın yapılandırması ve veritabanısız testler onları okur); yalnız veritabanısız kurulumun varsayılanıdır.
- **Açılış denetimi** (`YasalParametreServisi.AcilisDegerlendir` + `Program.cs`):
  - İşlem anında **tablodan okunan** anahtar (`YasalAnahtar.TablodanOkunan`: MASAK eşikleri, tevsik haddi, SİB, saklama, KMTS) farklıysa → `YASAL_PARAM_YAPILANDIRMA_FARKLI` **uyarısı**, sunucu açılır, kapı tablodaki değeri uygular. Ekrandan bugünden geçerli değişiklik sunucuyu "açılmaz" duruma düşürmez.
  - Kodun **hâlâ yapılandırmadan okuduğu** anahtar (gider pusulası günü, stopaj oranları, KDV genel oranı) farklıysa → gerçek ayrışma: `YASAL_PARAM_AYRISMA`, sunucu açılmaz (fail-closed korunur).
  - Tablo okunamazsa: mevcut davranış — kapılar işlem anında 503 (fail-closed).
  - **Hasım DÜŞÜK (MASAK-370) kapatıldı:** şema eskiyken denetim artık tümüyle atlanmıyor; farklar hesaplanıp `YASAL_PARAM_DENETIM_ERTELENDI` satırına yazılıyor, yalnız engel erteleniyor.
- Gerekçe (Bertan): iki kaynak var ama tek **yetkili** kaynak var; kopya yalnız DB'siz modda okunur. Gerekçe (Mehmet): gevşeme yok — kapılar tabloyu okur; yapılandırmadan okunan anahtarın engeli aynen sürer.

### 2.2 Bugünden geçerli değişiklik (`YasalParametreServisi.Dogrula/Onizle/Ekle`)
- Başlangıç **bugün** olabilir; **geçmiş → 400** (`alan: baslangic`, eskiden 409). Geçmiş yeniden yazılmaz: eski satırın yalnız `gecerlilik_bitis`'i bir kez dolar.
- **Resmî Gazete tarihi + sayısı ZORUNLU** (yeni sütunlar). Tarih ileri gün olamaz; değer RG tarihinden önce başlayamaz; sayı rakamla başlar.
- Gerekçe ≥ 20 karakter (mevcut `GerekceAsgari`). Değer sırası: anahtar → teyit → başlangıç → değer → RG → kaynak → gerekçe.
- **İkinci onay adımı:** `POST /api/v1/sistem/yasal-parametreler/onizleme` HİÇBİR ŞEY YAZMAZ; eski → yeni değer, geçerlilik günü, `bugunMu`, yön (`SIKILASTIRMA`/`GEVSETME`/`AYNI`/`DEGISIKLIK`), etkilenecek açık işlem sayısı ve **onay kodu** (içerik + o günkü satırın SHA-256 özeti) döner. Kayıt onay kodu olmadan **400**, önizlemeden sonra içerik ya da eski satır değiştiyse **409**.
- Dayanak / sade açıklama boşsa o anahtarın bugünkü satırından türetilir (UX-S21); bugünkü satırın dayanağı boşsa (ör. ek belge eşiği tohumu) **uydurulmaz → 400 `dayanak`**. Teyit boşsa DOGRULANDI (RG künyesi zorunlu olduğu için).
- **Etkilenecek açık işlem:** tutarı eski–yeni değer arasında kalan AÇIK siparişlerin sayısı (tüm kiracılar, yalnız COUNT; TL sınırlarında). Gerekçe: mevzuat belgesi § 5 "önce açılıp sonra kapanan işlemler" — bunlar kapanırken yeni değerle değerlendirilir. `SistemKapsami` izin listesi madde 9'a ek olarak yazıldı.
- **Denetim izi** (`DenetimServisi.SistemYazmaKaydet`, yeniden yazılmadı): anahtar · eski → yeni · başlangıç (BUGÜNDEN GEÇERLİ) · yön · teyit · operatör · RG künyesi · kapanan satır · gerekçe · kaynak adresi (1000 karakter kırpmasına karşı kimlik/künye önde).
- Önbellek kayıttan hemen sonra tazelenir: aynı günün sonraki işlemi yeni değeri okur, yeniden başlatma gerekmez.
- **Uyarı metinleri ayrı:** sıkılaştırma "bugünün bundan sonraki işlemleri yeni sınırla değerlendirilir; bugün daha önce tamamlanmış işlemler yeniden değerlendirilmez"; gevşetme "bugün daha önce kimlik alınmış işlemler etkilenmez".

### 2.3 Yetki
- Değiştirme yalnız `SistemAnahtariKapisi` (ByGatech sistem yöneticisi). SAHİP JWT ile önizleme ve kayıt **403**; yanlış anahtar **403**; doğru anahtar **200/201** (test + canlı).
- Kuyumcu (SAHİP/MÜDÜR/MUHASEBE) `/yasal-sinirlar`'da yalnız görür: değer, "Ne zamandan beri", **"Kaynak: Resmî Gazete 7 Ekim 2026, sayı 33393"**, rozet. Katlanır başlık: "Devletin rakamları — mevzuat değişince ByGatech günceller".

### 2.4 Ekran
- `Bilesenler/YasalParamFormu.razor` (yeni): yalnız `/yasal-sinirlar?yonetim=1` iken çizilir. Zorunlu önce: sınır, yeni değer, başlangıç (varsayılan bugün), RG tarihi/sayısı, kaynak adresi (seçilen sınırın bugünkü kaynağı önerilir), gerekçe, sistem anahtarı (kaydedilmez, istekten sonra boşaltılır), ad. Dayanak/teyit "Diğer bilgiler" katlanırında; bugünkü dayanak yoksa dayanak alanı açıkta ve zorunlu.
- Boş/geçersiz "Önizle": alan altı Türkçe hatalar, istek atılmaz. Sunucu alan hatası alanın altına, alansız hata banda.
- Onay: ortak `ModalPencere` (yıkıcı/geri dönüşsüz onay kalıbı) — eski → yeni, geçerlilik + kaynak, yön uyarısı, açık sipariş sayısı; "Vazgeç" / "Onayla ve kaydet". Başarı bandı sunucunun döndürdüğü eski/yeni/başlangıcı yazar; liste yeniden okunur.
- İstek yolu: `ApiIstemci.SistemPostAsync` (yeni, fırtına kapısından geçer, JWT eklemez; P-07 "ApiIstemci dışı HttpClient = 0" korunur).

### 2.5 EK KAPSAM — MASAK-370 tasarım hakemi bulguları
| Bulgu | Önem | Yapılan |
|---|---|---|
| Kimlik eşiği üç yerde üç farklı şey söylüyor | KRİTİK | (a) /yasal-sinirlar 200.000 kartı: "Bu tutar ve üstünde işlemin amacı yazılır ve kimlik alınır (mali müşavir teyidi bekleniyor)". (b) Satış kilit metni tetikleyen kademeyi söyler (`Islemler.YasalParam.cs`): amaç kademesi → "Kimlik ve işlem amacı eksik: 200.000 ₺ ve üstünde kimlik ve işlem amacı alınır."; kimlik eşiği → "Kimlik eksik: 370.000 ₺ ve üstünde kimlik zorunlu (MASAK)."; "MASAK eşiği aşıldı" genel cümlesi yalnız sunucu ölçüyü bildirmediğinde kalır. (c) PRO yan panel etiketi "{0} ve üstünde kimlik" (200.000'e "kimlik eşiği" denmiyor). (d) MASAK formu PRO açıklaması tek cümle kalıbı: "Kimlik 370.000 ₺ ve üstünde zorunlu (MASAK). 200.000 ₺ ve üstünde işlem amacı beyanıyla birlikte de kimlik istenir (mali müşavir teyidi bekleniyor). Bilgiler şifreli saklanır." Sayılar sunucudan (`/masak/esikler`). Davranış değişmedi. |
| Bağlantılı işlemde başlık "(tutar yüksek)" | ORTA | `/masak/gereksinim` yanıtına `olcuTutarTl` + `islemTutarTl`; `MasakFormu` başlığı "Kimlik gerekli — bu müşterinin bugünkü toplamı X ₺" (yeni satır eklenmedi, başlık değişti). |
| "Doğrulandı" rozeti yanında "teyit bekliyor" notu | DÜŞÜK | T-1 toplama notu "Dayanak ve geçmiş" katlanırına taşındı (id değişmedi). |
| Kümülatif red notunda iki farklı eşik | DÜŞÜK | Not, ana cümleyle aynı kuralla seçiliyor (toplam ≥ kimlik eşiği → kimlik eşiği). |
| Program.cs şema eskiyken denetimi tümüyle erteliyor | DÜŞÜK | § 2.1 — farklar hesaplanıp yazılıyor. |
| Dönem geçişinde kapı sunucunun bugününü kullanıyor | DÜŞÜK | `MasakKapisi.Dogrula/DogrulaKumulatif(esikGunu)`; `BankaModulu` geç eşleştirmesi dekontun oluşturulduğu yerel günün eşiğini veriyor. Test: 06.10 tarihli 190.000 → kimlik zorunlu; bugün 190.000 → serbest. |
| MASAK formu isteğe bağlı alanlar / BASİT-PRO başlık; /uyum para biçimi, koyu sekme | ORTA/DÜŞÜK (kapsam dışı) | Dokunulmadı — satış/MASAK sadeleştirme ve para biçimi paketlerine (§ 12). |

### 2.6 Sapmalar ve gerekçeleri
1. **Dosya alanı dışı dokunuşlar** (hepsi EK KAPSAM'ın gereği ya da denetim kuralının zorunluluğu): `MasakKapisi.cs`, `MasakModulu.cs`, `BankaModulu.cs` (1 çağrı), `SistemKapsami.cs` (yalnız yorum), `ApiIstemci.cs` (yeni yöntem), `UyumServisi.cs` (kayda init özellik), `MasakFormu.razor`, `Islemler.razor` (1 ifade) + `Islemler.YasalParam.cs`, `Sozlukler.YasalSinirlar.cs`, `Sozlukler.SatisPro.cs`, `YasalParametre.cs` (2 sütun), `YasalSinirlarSozlesmeleri.cs`, `YasalParametreModulu.cs`. `Sozlukler.cs` HUB'a anahtar eklenmedi.
2. **Testi değişen başka dosyalar** (davranış/metin değiştiği için): `YasalParametreHttpTestleri.cs` (iki adımlı yazma yardımcısı; K5 geçmiş 409→400; K7 ikiye bölündü), `YasalSinirlarEkranTestleri.cs` (başlık metni; T-1 notu katlanır içinde), `SATIS3EkranTestleri.cs` + `SATIS3T24YasalMesajTestleri.cs` (kilit metni tetikleyen eşikle), `ZOISLEMAEkranTestleri.cs` ZO29c (eski kural "eşik sayısı formda basılmaz" → yeni ilke: eşik istemciye gömülmez, formda yalnız sunucunun değeri tek cümle kalıbıyla basılır).
3. **Canlı doğrulama ayrı veritabanında** (`kuyumcu_ype_t2`, sıfırdan 88 migration ile kuruldu): kurucunun 5199/5200 sunucuları `kuyumcu_net`'i okuyor; bugünden geçerli 370.000 → 150.000 test değişikliği orada bir yeniden başlatmada görünebilirdi. `kuyumcu_net`'e yalnız şema migration'ı uygulandı; MASAK değeri hiç değişmedi.
4. **Paralel şerit etkisi (gözlem):** `MasakFormu.razor` düzenlemem bir kez paket öncesi hâline döndü (dosya önceki yedeğimle birebir aynı çıktı; başka bir şeridin mutasyon geri yüklemesi olduğu düşünülüyor). Değişiklik yeniden uygulandı ve tam süitten önce `cmp` ile doğrulandı. Ayrıca artımlı derleme iki kez eski DLL üretti; dosyalar `touch` edilip yeniden derlendi.

## 3. DoD sözleşmesi
| Varlık | Düzenleme yolu | Not |
|---|---|---|
| `yasal_parametreler` satırı | Yeni değer: `POST /sistem/yasal-parametreler` (+ `/onizleme`), sistem anahtarı + operatör, denetim kaydı. Ekranda ByGatech formu. | APPEND-ONLY (ADR-0005): satır silinmez/güncellenmez; düzeltme = yeni satır (bugün ya da ileri). Geçmiş düzeltmesi yalnız migration + denetim. Kuyumcu ekranında DegismezIpucu gerekçesi + "ByGatech günceller". |
| `resmi_gazete_tarihi/sayisi` sütunları | Yeni satırda zorunlu | Eski tohumlarda NULL (uydurulmaz); yalnız `masak370-01` birincil kaynakla dolduruldu. |
| Yeni varlık | Yok | Finansal tabloya yazma/DELETE yok. |

Dokunulan sayfalar (razor): `Pages/YasalSinirlar.razor`, `Pages/Islemler.razor` (tek ifade; gerekçe: EK KAPSAM KRİTİK satış kilit metni), `Bilesenler/MasakFormu.razor` (EK KAPSAM ORTA/KRİTİK), `Bilesenler/YasalParamFormu.razor` (yeni).

## 4. Migration
- Ad: `20261007222228_YasalParamResmiGazete` — `yasal_parametreler`'e `resmi_gazete_tarihi DATE NULL`, `resmi_gazete_sayisi VARCHAR(20) NULL`; `masak370-01` için RG 2026-10-07 / 33393 (MEVZUAT-KUYUMCU-BELGE § 2, birincil kaynak) doldurma.
- Sıra: `dotnet build Kuyumcu.sln` 0 hata → `dotnet ef migrations add YasalParamResmiGazete` (build'li) → `.cs` 1.222 B (Up'ta iki AddColumn), `.Designer.cs` 259.350 B; Designer farkı önceki migration'a göre yalnız iki sütun.
- Öncesi yedek: `/private/tmp/kuyumcu-serit/YASAL-PARAM-EKRAN/yedek-yasal-oncesi.sql` (16.950 B).
- `dotnet ef database update` (kuyumcu_net): `Applying migration '20261007222228_YasalParamResmiGazete'. Done.` SQL: `masak370-01 370000 2026-10-07 NULL 2026-10-07 33393`; RG dolu satır 1.

## 5. Derleme ve denetim
- `dotnet build Kuyumcu.sln --nologo` (son, mutasyonlardan sonra): **0 Hata · 0 Uyarı** (artımlı). Artımlı olmayan derlemede 4 uyarı: `SUBEKAPSAMASinirTestleri.cs(49)` CS8601, `KesifP2Testleri.cs(455)`, `SonKalanlarTestleri.cs(584,813)` — hepsi bu paketten önce de vardı; bu paketin dosyalarında uyarı 0.
- `bash dotnet/denetim.sh`: ilk koşumda 2 ihlal **bu paketindi** ve düzeltildi: `P-11 DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM 48>47` (catch içinde `onizleme = null`) ve `TOPLU-1 TARIH_TEK 2` (formda ham `ToString("yyyy-MM-dd")` → `Bicim.Makine`). Son koşum: **306 ✓, 1 ✗ — yalnız P-30 DEMO_INSAN_SURESI** (kabulde hariç).

## 6. Testler
| Koşum | Toplam | Geçti | Kırmızı | Atlanan |
|---|---|---|---|---|
| **Tam süit (tek koşum, son kod)** `/private/tmp/kuyumcu-serit/trx-pYASAL-PARAM-EKRAN/_Mac_2026-10-08_02_20_27.trx` | **10554** | **10551** | **1** | 2 |

- Tek kırmızı: `Yasal4HttpTestleri.Mutabakat_bugun_iptal_edilen_yenisiz_pusula_YENIDEN_DUZENLENECEK_belgesiz_degil` (beklenen 1, gerçek 0). **Bu paketin değil:** devir kaydında "Yasal4 UTC gün sınırı — önceden var" (03.10) ve TCKN-DOGRULAMA raporunun § 9 listesinde aynı test var; koşum 23:20 UTC'de (yerel 02:20) gün sınırının hemen sonrasındaydı. Bu paket gider pusulası/Yasal4 koduna dokunmadı. **Yeni kırmızı farkı = 0.**
- Not: tam süitten hemen önceki derleme paralel şeridin Web derlemesiyle dosya kilidine çarptı (`MSB4018 IOException`, `ConvertDllsToWebCil`); test DLL'leri bir önceki temiz derlemedendi (kod değişikliği yoktu). Ardından yeniden derleme **0 hata** ve DLL'lerde bu paketin dizgeleri doğrulandı; paket testleri son DLL ile yeniden koşuldu: 62/62.
- Yeni test vakası: **24** (YasalParamHttp 7 + YasalParamSaf 10 + YasalParamEkran 4 + YasalParamMasakFormu 2 + K7 yeni 1). Paket öncesi toplam 10554 − 24 = 10530 (hesaplanmış; paket öncesinde tam süit koşulmadı).

Yeni test dosyası `Kuyumcu.Tests/YasalParamEkranTestleri.cs` (4 sınıf):
- `YasalParamHttpTestleri` (7): bugünden geçerli RG+onay 201 · aynı gün sonraki işlem yeni eşikle · dün eski eşikle · denetim izi; RG tarihi/sayısı yok, RG ileri, geçmiş, gerekçe kısa → 400 alanlı (önizleme ve kayıt); dayanak boşsa 400; onay kodu yok 400 / uyuşmaz 409; SAHİP JWT 403, yanlış anahtar 403, sistem anahtarı 200; açık sipariş sayısı (teslim edilen sayılmaz) + TL dışı null; `/masak/gereksinim` ölçü alanları.
- `YasalParamSafTestleri` (10 vaka): yön tablosu (7 vaka), açılış uyarı/engel ayrımı, kümülatif not eşiği, işlem günü eşiği.
- `YasalParamEkranTestleri` bUnit (4): kuyumcu salt okunur (form yok, RG kaynağı, not, T-1 katlanırda, `/sistem/` isteği yok); boş Önizle alan hataları ve istek yok; önizleme → onay penceresi (eski→yeni, bugünden, sıkılaşıyor, açık sipariş) → kayıt onay koduyla → başarı bandı, liste yeniden okundu, anahtar boşaltıldı; vazgeç kayıt atmaz + sunucu alan hatası alanın altına.
- `YasalParamMasakFormuTestleri` (2): bağlantılı toplam başlığı (BASİT+PRO); tek işlemde eski başlık + PRO tek cümle kalıbı.
- `YasalParametreHttpTestleri`: K7 yeni iki test (tablodan okunan farklı → açılır, tablo değeri uygulanır; yapılandırmadan okunan farklı → açılmaz).

Filtreli koşumlar (son kod): `YasalParam|YasalParametre|YasalSinirlarEkran|SATIS3 T19|SATIS3T24|ZO29` → **93/93**; geniş MASAK/satış/banka/hurda/ekran süzgeci → 3213/3214 (tek kırmızı `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA…` WaitFor zaman aşımı; tek başına koşumda 6/6 yeşil — yük altında kararsızlık).

## 7. Mutasyon kanıtları (hepsi `perl -0777 -pi` tek eşleşme → filtreli test → yedek geri → `cmp` AYNI)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M1-1 | `bas < bugun` → `bas < bugun.AddDays(-1)` (dün serbest) | YasalParametreHttpTestleri.K5_gecmis_baslangic_400… | AYNI |
| M1-2 | RG tarihi yoksa başlangıçtan türet | İLK KOŞUM 0 KIRMIZI (TEST AÇIĞI: ileri tarihte "RG ileri" hatası aynı alana düşüyordu) → test mesajı ve bugün başlangıcıyla güçlendirildi → RG_tarihi_yok_400_… KIRMIZI | AYNI |
| M1-3 | Onay kodu denetimi kapalı | Onay_kodu_yok_400_onay_kodu_uyusmaz_409…, Mehmet_SAHIP_JWT…, Bugunku_satirin_dayanagi_BOSSA… | AYNI |
| M1-4 | Açılışta her fark ENGEL | Acilis_tablodan_okunan_fark_UYARI…, K7_YASAL_PARAM_EKRAN_tablodan_okunan…_ACILIR | AYNI |
| M1-5 | Yön ters | Yon_… (5 vaka), Bugunden_gecerli…, Onizleme_acik_siparis… | AYNI |
| M1-6 | Kümülatif notu hep amaç kademesi | Kumulatif_notu_toplam_kimlik_esigine_ulasinca… | AYNI |
| M1-7 | `esikGunu` yok sayılır | Esik_gunu_verilirse_ISLEM_GUNUNUN_esigi… | AYNI |
| M1-8 | Açık sipariş süzgeci (durum) kaldırıldı | Onizleme_acik_siparis_sayisi… (1 yerine 2) | AYNI |
| M1-9 | Formda gerekçe ön denetimi `g < 0` | Yonetim_bos_onizle_alan_alti_hatalar_istek_ATILMAZ | AYNI |
| M1-10 | MasakFormu bağlantılı başlık koşulu ters | Baglantili_toplamda_baslik…, Tek_islemde_baslik… | AYNI |
| M1-11 | Yönetim modu ters (`!= "1"`) | Kuyumcu_salt_okunur_form_YOK… + 3 yönetim testi | AYNI |
| M1-12 | Satış kilit metni kimlik dalı amaç metniyle | SATIS3EkranTestleri.T19_iki_kalem_her_biri_esik_ALTI_toplam_USTU… | AYNI |

Kayıtlar: `/private/tmp/kuyumcu-serit/YASAL-PARAM-EKRAN/mut-M1-*.log`. Son mutasyondan sonra temiz derleme 0/0.

## 8. Canlı doğrulama (API 5162, `kuyumcu_ype_t2`, son derleme; yanıtlar okundu)
Test kiracısı `PAKETYASALPARAMEKRAN-TEST` sistem davetiyle API'den kuruldu (parola rastgele, 600 izinli dosya; çıktıya basılmadı). Sistem anahtarı rastgele, yalnız süreç ortamında ve 600 izinli dosyada.

| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /masak/esikler (önce) | 370.000 | 200 · 370000 / 200000 |
| Dekont 160.000 kimliksiz (değişiklikten ÖNCE) | 200 | **200** (yazıldı) |
| Önizleme RG tarihi yok / RG sayısı yok | 400 alanlı | **400** `resmiGazeteTarihi` / **400** `resmiGazeteSayisi` |
| Kayıt RG tarihi yok | 400 | **400** |
| Önizleme geçmiş başlangıç (dün) | 400 | **400** `baslangic` "Geçmiş tarihli yasal değer yazılamaz…" |
| Önizleme / kayıt SAHİP JWT | 403 | **403 / 403** "Sistem yönetimi anahtarı geçersiz." |
| Kayıt onay kodu yok | 400 | **400** `onayKodu` |
| Önizleme (sistem anahtarı) | 200 | **200** eski 370000 → yeni 150000 · bugunMu true · yön SIKILASTIRMA · açık sipariş 0 · RG 2026-10-08/99999 |
| Kayıt (onay koduyla) bugünden geçerli 370.000 → 150.000 | 201 | **201** · kapananSatirId masak370-01 |
| GET /masak/esikler (sonra) | 150.000 | 200 · 150000 (kimlikIstenen 150000) |
| GET /masak/esikler?tarih=dün | 370.000 | 200 · 370000 |
| Dekont 160.000 kimliksiz (değişiklikten SONRA, aynı gün) | 400 | **400** "MASAK: 150.000 TL ve üstü işlemde kimlik tespiti zorunludur" |
| GET /yasal-parametreler (kuyumcu) | RG görünür | 200 · 150000 · 2026-10-08 · DOGRULANDI · RG 2026-10-08 / 99999 |
| Açılış: env `Masak__KimlikEsigi=185000`, tablo 370.000 | Sunucu açılır | `/saglik/hazir` **HAZIR**; günlük `YASAL_PARAM_YAPILANDIRMA_FARKLI — tablo esas…`; /masak/esikler **370000** (tablodan) |

Bağımsız SQL (`ham/YASAL-PARAM-EKRAN-olcum/canli-sql.txt`):
```
banka_hareketleri: 160000.00 GELEN NULL BEKLIYOR 2026-10-07 23:02:58 UTC (=08.10 02:02 yerel)  ← yalnız ÖNCEKİ dekont; sonraki yazılmadı
yasal_parametreler: 01M4C9P5… 150000 2026-10-08 NULL DOGRULANDI 2026-10-08 99999 sistem:ramazan-ype
                    masak370-01 370000 2026-10-07 2026-10-08 DOGRULANDI 2026-10-07 33393
denetim_kayitlari:  "yasal parametre eklendi · MASAK_KIMLIK_ESIGI_TL · eski 370000.000000 → yeni 150000 · başlangıç 2026-10-08 (BUGÜNDEN GEÇERLİ) · yön SIKILASTIRMA · teyit DOGRULANDI · operatör: ramazan-ype · kaynak RG 2026-10-08/99999 · önceki satır masak370-01 … kapandı · gerekçe: …"
GERİ ALMA SONRASI: masak370-01 370000 2026-10-07 NULL DOGRULANDI 33393   ← gerçek değer aynen
```
Not: Betikteki cari oluşturma yolu yanlıştı (`/cariler` → 404); bu yüzden iki dekont da müşterisiz girildi (tek işlem kuralı). Senaryo (aynı gün, değişiklik öncesi 200 / sonrası 400) bundan etkilenmedi; bağlantılı toplam davranışı HTTP testleriyle sınandı.

Kapanış: test satırı SQL ile silindi, `masak370-01` yeniden açıldı (370.000 açık uçlu). `kuyumcu_net`'te MASAK satırı hiç değişmedi (`masak370-01 370000 NULL`). Test kiracısı `kuyumcu_ype_t2`'den kiraci-temizle algoritmasıyla silindi: 75 tablodan 180 satır + tenants satırı; kalan kiracı 0. **kuyumcu_net tenants: 17 → 17.** API, vekil ve başsız Chrome durduruldu; 5162/5163/9905/9906'da LISTEN yok; 5199/5200'e dokunulmadı.

## 9. Gerçek tarayıcı ölçümü (başsız Chrome, `tur.mjs` + `sikisma`, vekil 5163; onay penceresi `onay.mjs`, CDP 9906)
Tam tablo: `ham/YASAL-PARAM-EKRAN-olcum/olcum-tablo.txt`, `onay-tablo.txt`, ham `olcum-*.jsonl`; görüntüler `foto/` (32).

| rota | genişlik | mod | tema | taşan | kırık | binen | yatay | gizli-görünen | TRY | kırp |
|---|---|---|---|---|---|---|---|---|---|---|
| /yasal-sinirlar | 1440 · 1280 · 375 | BASIT + PRO | açık + koyu | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| /yasal-sinirlar?yonetim=1 | 1440 · 1280 · 375 | BASIT + PRO | açık + koyu | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| onay penceresi (form doldurulup Önizle) | 1280 · 375 | BASIT + PRO | açık + koyu | 0 | 0 (ilk turda 375'te "Onayla ve kaydet" kırılıyordu → `.yasal-onay-eylem` düzeltmesi, yeniden ölçüldü) | 6* | 0 | 0 | 0 | 0 |

\* Onay penceresindeki 6 "binen" ölçümün tamamı modalın altındaki sayfa öğesiyle kesişmedir (örnekler `#modal-… ⟂ alttaki kart`); modal dışı binen 0.
- 24 sayfa + 8 onay ölçümü, geçersiz 0, hata 0. Gerçek mod her kayıtta istenen modla aynı.
- K4 "tren": kuyumcu sayfası 1440'ta 1 kat, 375'te 2–2,1 kat (paket öncesi yapı). Yönetim rotasında 4–9,2 kat: tur aracı önceki rotada açtığı katlanırları açık bıraktı (görüntüde bütün "Dayanak ve geçmiş" açık) ve form eklendi; yalnız ByGatech'in açtığı rotadır.
- Dar sütun (K2): kartlar 3 sütunlu ızgara (değişmedi); form iki/üç sütunlu ızgara, 375'te tek sütun.
- **Read ile bakılan görüntüler:** `yasal_sinirlar_yonetim_1__BASIT_aydinlik_1440.png` (kimlik kartında "Kaynak: Resmî Gazete 7 Ekim 2026, sayı 33393", 200.000 kartında yeni metin, form zorunlu alanları ve "Diğer bilgiler" kapalı), `yasal_sinirlar__PRO_karanlik_375.png` (kuyumcu: form yok, başlık "mevzuat değişince ByGatech günceller", T-1 notu kartta değil), `onay__Basit_karanlik_375.png` (ilk tur: düğme kırılması görüldü), `onay__Profesyonel_aydinlik_375.png` (düzeltme sonrası: "370.000 ₺ → 250.000 ₺", "Bugünden (8 Ekim 2026) geçerli. Kaynak: Resmî Gazete…", sıkılaşıyor uyarısı, açık sipariş 0, iki düğme tek satır).

## 10. Değişen dosyalar
Depoda yalnız "first commit" var; `git status --porcelain` bu dosyaları ayırt etmiyor (çoğu `??`). Liste elle; öncesi kopyaları `/private/tmp/kuyumcu-serit/YASAL-PARAM-EKRAN/onceki/`.
- Yeni: `Kuyumcu.Api/Data/Migrations/20261007222228_YasalParamResmiGazete.cs` + `.Designer.cs`, `Kuyumcu.Core/I18n/Sozlukler.YasalParam.cs`, `Kuyumcu.Web/Bilesenler/YasalParamFormu.razor`, `Kuyumcu.Web/Pages/Islemler.YasalParam.cs`, `Kuyumcu.Tests/YasalParamEkranTestleri.cs`.
- Değişen (API): `Common/YasalParametreServisi.cs`, `Data/YasalParametre.cs`, `Data/SistemKapsami.cs` (yorum), `Data/Migrations/KuyumcuDbContextModelSnapshot.cs` (ef), `Program.cs` (yalnız açılış denetimi), `Modules/Sistem/SistemModulu.cs`, `Modules/Yasal/YasalParametreModulu.cs`, `Modules/Masak/MasakKapisi.cs`, `Modules/Masak/MasakModulu.cs`, `Modules/Banka/BankaModulu.cs`.
- Değişen (Contracts/Core/Web): `Contracts/Sayfalar/YasalSinirlarSozlesmeleri.cs`, `Core/I18n/Sozlukler.YasalSinirlar.cs`, `Core/I18n/Sozlukler.SatisPro.cs`, `Web/Servisler/ApiIstemci.cs`, `Web/Servisler/UyumServisi.cs`, `Web/Bilesenler/MasakFormu.razor`, `Web/Pages/YasalSinirlar.razor`, `Web/Pages/Islemler.razor`, `Web/wwwroot/css/yasal.css`.
- Değişen testler: `YasalParametreHttpTestleri.cs`, `YasalSinirlarEkranTestleri.cs`, `SATIS3EkranTestleri.cs`, `SATIS3T24YasalMesajTestleri.cs`, `ZOISLEMAEkranTestleri.cs`.
- `appsettings.json` değişmedi (bilinçli, § 2.1).
- Belge/kanıt: bu rapor, `dokumanlar/09-uygulama-kayitlari/ham/YASAL-PARAM-EKRAN-olcum/`.

## 11. Koşul karşılama
| Koşul | Durum | Kanıt |
|---|---|---|
| (1) Tek kaynak tablo; yanlış config sunucuyu açılmaz bırakmaz; tablo okunamazsa fail-closed | ✓ | § 2.1; K7 iki test; M1-4; canlı açılış satırı |
| (2) Bugünden geçerli; RG tarih+sayı zorunlu; gerekçe ≥20; onay penceresi (eski→yeni, tarih, açık işlem); denetim izi; sıkılaştırma/gevşetme ayrı metin; geçmiş yazılamaz | ✓ | § 2.2, § 8, M1-1/2/3/5/8 |
| (3) Yetki yalnız sistem anahtarı; SAHİP 403, sistem yöneticisi 200 (Mehmet) | ✓ | Mehmet_SAHIP_JWT…; canlı 403/200 |
| (4) Ekran: yönetim formu + kuyumcu salt okunur + "ByGatech günceller" | ✓ | § 2.4, § 9, bUnit 4 test |
| EK KAPSAM KRİTİK/ORTA/DÜŞÜK (hakem + hasım) | ✓ (kapsam dışı ikisi devredildi) | § 2.5 |
| Mevzuat: yeni değer uydurulmadı; 370.000 ve 200.000 aynen | ✓ | § 8 geri alma sonrası; kuyumcu_net hiç değişmedi |
| Migration prosedürü (önce build, --no-build yok) | ✓ | § 4 |
| Mutasyon ≥1 kırmızı, cmp AYNI | ✓ | § 7 (12/12; 1 test açığı kapatıldı) |
| Build 0/0 · denetim TEMİZ (P-30 hariç) | ✓ | § 5 |
| Canlı API + bağımsız SQL | ✓ | § 8 |
| Başsız Chrome 1440/1280/375 · BASİT+PRO · açık+koyu · Read | ✓ | § 9 |
| Test kiracısı silindi; tenants sayısı | ✓ | § 8 |
| COMMIT yok · dosya silme yok | ✓ | — |
| Sır basılmadı | ✗ (bir kez) | § 12 madde 1 |

## 12. Açık kalanlar (sahibiyle)
1. **Kurucu / Mehmet — sır olayı (gizlenmedi):** canlı SQL adımında zsh'te `$Q` değişkeni kelimelere bölünmediği için `docker exec … -p<parola>` komut metni "command not found" hatasıyla **benim araç çıktıma bir kez düştü**. Aynı satırlar bir geçici dosyaya da yazılmıştı; dosya hemen `printf "" >` ile boşaltıldı ve kanıt dizininde parola geçmediği grep ile doğrulandı. Proje ağacına ve rapora yazılmadı. **Öneri: MySQL root parolasının döndürülmesi** (kurucu kararı).
2. **Batuhan / orkestratör:** `kuyumcu_ype_t2` veritabanı (canlı doğrulama için kurulan ayrı veritabanı, kiracısı silindi) yerinde duruyor; silinmesi orkestratörün disk temizliği kararına bırakıldı.
3. **Batuhan / kurucu onayıyla:** 5199/5200 yayını bu sürümle güncellenmeden yeni ekran ve RG sütunları orada görünmez. Eski yayın yeni sütunlardan etkilenmez (EF fazla sütunu okumaz).
4. **Aşkın / satış-MASAK sadeleştirme paketi:** hakemin kapsam dışı ORTA (MASAK formunda isteğe bağlı alanlar açık, BASİT/PRO başlık farkı, "TCKN") ve DÜŞÜK (/uyum tablo para biçimi, koyu tema sekmeleri) maddeleri.
5. **Bertan — gün çözünürlüğü:** tablo GÜN çözünürlüklüdür. Bugünden geçerli değişiklikten önce kapanmış işlemler kapanışta okunan değerle kayıtlıdır ve yeniden açılmaz; ancak o günün işlemini SONRADAN yeniden değerlendiren raporlar (RiskServisi dönem bölmesi) bütün günü yeni değerle okur. Saat çözünürlüklü yürürlük gerekirse ayrı paket.
6. Tam süitte bu paketin dışındaki kırmızılar § 13'te sahipleriyle.

## 13. Kurucuya ve mali müşavire notlar
- **Mali müşavir — M-2 (açık):** 200.000 TL amaç beyanı kademesinin kuyumcuya ve 370.000 altında kimliğe uygulanıp uygulanmadığı. Ekran artık bunu açıkça "mali müşavir teyidi bekleniyor" diye söylüyor; davranış değişmedi.
- **Mali müşavir — dönem geçişi:** geç eşleştirilen dekontta artık işlem gününün (dekontun oluşturulduğu yerel gün) eşiği uygulanıyor (MEVZUAT-KUYUMCU-BELGE § 5: "Program işlem gününü esas alıyor"). Kaparo/vadeli satış gibi açık işlemlerde hangi günün esas alınacağı teyit bekliyor; onay penceresi bu işlemlerin sayısını gösteriyor.
- **Kurucu:** ByGatech yöneticisi formu `/yasal-sinirlar?yonetim=1` adresindedir; menüde yoktur. Yetki sunucudaki sistem anahtarıdır (yapılandırma sırrı); anahtarsız kimse yazamaz.

---

## 14. TUR 2 (08.10, 03:10–04:55) — önceki doğrulamanın bulguları

Bu bölüm § 1–13'ün üzerine yazar. Kanıt dizini: `ham/YASAL-PARAM-EKRAN-olcum/tur2/`. Tur öncesi kopyalar: `/private/tmp/kuyumcu-serit/YASAL-PARAM-EKRAN/tur2-once/`.

### 14.1 Bulgu → yapılan
| # | Bulgu | Önem | Yapılan |
|---|---|---|---|
| 1 | Türkçe harfli ad ("Tasarım Hakemi") Önizle'de sayfayı çökertiyor | KRİTİK | Kök neden: tarayıcı ASCII dışı HTTP başlığını reddediyor (`HttpRequestException`). İstemci adı **RFC 5987** biçiminde gönderir (`ApiIstemci.OperatorBasligiDegeri` → `UTF-8''Tasar%C4%B1m%20Hakemi`); sunucu `SistemAnahtariKapisi.OperatorCoz` ile çözer (kapıdaki ≥3 karakter kuralı ve `SistemController.Operator` çözülmüş değeri kullanır; önek yoksa değer olduğu gibi — curl/betik uyumu). Form Önizle/Kaydet artık `HttpRequestException / TaskCanceledException / InvalidOperationException / FormatException`'ı da yakalar ve hata bandına "Sunucuya ulaşılamadı…" yazar (sayfa düşmez, UX-S28). **Gerçek başsız Chrome'da** "Tasarım Hakemi" adıyla 8/8 + 2 ileri tarih kombinasyonunda onay penceresi açıldı (§ 14.6); canlı API'de denetim izine `operatör: Tasarım Hakemi`, oluşturana `sistem:Tasarım Hakemi` yazıldı (§ 14.5). |
| 2 | Aynı satışta iki farklı kimlik eşiği: PRO "Yasal kapılar" 200.000 diyor | KRİTİK | Tek seçici `Web/Servisler/MasakKademe.cs` (yeni): Toplam / Kimlik / Amaç / Yok / Bilinmiyor. Kilit metni (`Islemler.YasalParam.cs`), MASAK formu başlığı ve PRO paneli bu tek kuralla seçer. Panel: kimlik satırı **"370.000 ₺ ve üstünde kimlik (MASAK)"** (md.5/1-b eşiği; durum Altında / Kimlik gerekir), amaç kademesi **ayrı satır** "200.000 ₺ ve üstünde işlem amacıyla kimlik — teyit bekleniyor" (durum Kimlik ve amaç gerekir). Ölçü aynı (`MasakKademe.Olcu`: bağlantılı toplam ya da işlem tutarı). 375 px'te uzun etiket kartın dışına taşıyordu (ilk ölçüm turunda görüntüde görüldü; ölçer K3 bunu yakalamadı) → `yasal.css`'e yalnız bu iki satır için sarma kuralı (`.yasal-masak-satir`), yeniden ölçüldü, panel-dışı öğe 0. |
| 3 | MASAK formunda isteğe bağlı alanlar açık, BASİT/PRO başlıkları farklı, PRO'da "TCKN" | KRİTİK | (a) **Tek başlık, iki modda aynı**, tetikleyen kademeyi söyler: "Kimlik gerekli — 370.000 ₺ ve üstünde zorunlu" / "Kimlik ve işlem amacı gerekli — 200.000 ₺ ve üstünde" / "Kimlik gerekli — bu müşterinin bugünkü toplamı X ₺"; kademe bilinmiyorsa (eski sunucu) eski sade başlık, sayı uydurulmaz. PRO'nun başlığı tekrarlayan rozeti kaldırıldı; MASAK atıflı tek cümle kalıbı yalnız PRO'da `#masak-kural`. (b) Doğum tarihi, uyruk ve (zorunlu değilse) adres kapalı **"Diğer bilgiler (isteğe bağlı)"** katlanırında (`Katlanir Id="masak-diger"`); gider pusulası adresi zorunluysa (`AdresZorunlu`) adres dışarıda kalır. (c) Belge no etiketi iki modda belge türüne göre ("T.C. kimlik numarası" / "Pasaport numarası" …); Kaydet kilidindeki eksik alan adı da aynı kaynaktan (`MasakFormu.BelgeNoEtiketiFor`, `Islemler.Toplu5.cs`). |
| 4 | Onay penceresindeki uyarı ileri tarihte de "bugün" diyor | DÜŞÜK | `YonMetni(o)` `BugunMu`'ya bakar; ileri tarih için ayrı metinler: "Sınır sıkılaşıyor/gevşiyor: 15 Ekim 2026 gününden itibaren işlemler yeni sınırla değerlendirilir. O günden önce(ki)…". |
| 5 | Yeni değerde akla yatkınlık sınırı yok | DÜŞÜK | Sunucu: bugünkü değeri pozitif olan sınırda yeni değer **0/negatif olamaz ve bugünkünün 10 katından fazla büyüyüp küçülemez → 400 (`degerSayi`)** (`AklaYatkinKat=10`; mevzuat değeri DEĞİL, yazım hatası koruması; gerçekten böyle bir değişiklik ByGatech veritabanı düzeltmesiyle yazılır). Değer ≥2 kat ya da ≤yarı değişiyorsa önizleme `buyukDegisim=true` döner ve onay penceresi ek uyarı basar ("Değer bugünküne göre çok değişiyor…"). |
| 6 | AYNI değer yeni satır olarak yazılıyor; aynı gün düzeltme yolu yok | DÜŞÜK | AYNI → **409** (`degerSayi`, "aynı değer yeni satır olarak yazılmaz"). Aynı gün ikinci yazım bilinçli olarak 409 kalır (o andan sonraki işlemler değerle değerlendirilmeye başladı; geçmiş yeniden yazılmaz). Yol ekranda: bugünden geçerli onay penceresinde `DegismezIpucu` — "Bugün başlayan değer aynı gün ikinci kez yazılamaz… Düzeltme: yarından geçerli doğru değeri yazın; bugünkü işlemlerin düzeltmesini ByGatech veritabanı düzeltmesiyle (denetim kaydıyla) yapar." Canlıda: aynı gün ikinci düzeltme 409, yarından geçerli önizleme 200 (§ 14.5). |
| 7 | Sistem anahtarı kiracı uygulamasındaki forma yazılıyor; operatör adı beyan | DÜŞÜK | Spesifikasyon "mevcut sistem anahtarı yapısını kullan" dediği için yapı korundu (anahtar kaydedilmez, kayıttan sonra boşaltılır, sunucuda doğrulanır). **Açık kalan (Mehmet notu):** ayrı ByGatech yönetim arayüzü + kişiye bağlı operatör kimliği — § 14.9. |
| 8 | `MasakModulu.Gereksinim` yorumu yanlış | DÜŞÜK | Yorum düzeltildi: yanıt kademe + `OlcuTutarTl` (aynı kiracının bugünkü bağlantılı toplamı) + `IslemTutarTl` taşır. |

### 14.2 Sapmalar ve gerekçeleri (tur 2)
- **Dosya alanı dışı dokunuşlar** (hepsi devredilen KRİTİK maddelerin gereği): `Bilesenler/Fis/FisProYanSutun.razor` (PRO paneli — hakemin işaret ettiği yer), `Pages/Islemler.Toplu5.cs` (kilitteki alan adı = alanın etiketi; yalnız etiket kaynağı), `Web/Servisler/MasakKademe.cs` (yeni, tek seçici), `Api/Modules/Sistem/SistemAnahtariKapisi.cs` + `SistemModulu.cs` (yalnız operatör başlığının çözülmesi; Telemetri/Abonelik denetleyicileri aynı başlığı ham okur — ekranda Türkçe adla çağrılmıyorlar, değiştirilmedi, § 14.9), `Core/I18n/Sozlukler.SatisPro.cs` (yalnız `KimlikEsigi` değeri: "… kimlik (MASAK)"), `wwwroot/css/yasal.css` (iki satırlık sarma kuralı; sabit renk yok). `Sozlukler.cs` HUB'a anahtar EKLENMEDİ; yeni metinler `Sozlukler.YasalParam.cs`.
- **Dokunulan sayfalar (tek dokunuş kaydı):** `Pages/Islemler.razor` bu pakette ikinci kez açıldı (tek ifade: `FisProYanSutun`'a `MasakKimlikEsigi/AmacEsigi/MasakOlcu` parametreleri). Gerekçe: tasarım hakeminin KRİTİK maddesi aynı ekranın PRO panelindeydi; aynı paketin ikinci turu. `Bilesenler/MasakFormu.razor`, `Bilesenler/YasalParamFormu.razor`, `Bilesenler/Fis/FisProYanSutun.razor`.
- **Değişen eski testler (davranış/metin bilinçli değişti):** `EkranTestleri.cs` (MasakFormuEkranTestleri: katlanır açılır, tek başlık, PRO etiket TCKN'siz), `Masak370EkranTestleri.cs` (panel: kimlik satırı 370.000 + ayrı 200.000 amaç satırı — "kimliği isteyen 200.000 panelde kaybolmaz" niyeti korunarak), `ZOISLEMAEkranTestleri.cs`, `Toplu5Testleri.cs` (BASİT/PRO aynı başlık; PRO'da MASAK atfı `#masak-kural`'da), `ParaDemo2EkranTestleri.cs`, `ParaDemo3Testleri.cs` (PRO etiket = BASİT etiket), `KesifP5Testleri.cs` (ön dolu adres katlanırda — açılıp okunur), `YasalParamEkranTestleri.cs`.
- **Eşdeğer mutant (M2-4):** `AklaYatkin` içindeki `yeni > 0m` terimi, eski > 0 iken alt sınır (`yeni*10 ≥ eski`) tarafından zaten karşılanır; terim kaldırılınca davranış değişmez → hiçbir test kırmızı olamaz. Test açığı değil; yerine alt sınırı kaldıran M2-4b koşuldu (kırmızı). Terim okunurluk için kod içinde bırakıldı.
- **Proje ağacında boş dosya (benim hatam, gizlenmiyor):** bir heredoc denemesi `dotnet/Kuyumcu.Web/Pages/Islemler.YasalParam.cs.tmp` adlı **0 baytlık** dosya bıraktı. Derlemeye girmez (uzantı `.tmp`); silme kancası (rm/mv yasak) nedeniyle silinemedi → **kurucunun silmesi gerekir**.

### 14.3 Derleme ve denetim
- `dotnet build Kuyumcu.sln --nologo` (son, mutasyonlardan ve son düzenlemeden sonra): **0 Hata, 0 Uyarı** (`t2-b6.log`).
- `bash dotnet/denetim.sh`: **314 ✓, 1 ✗ — yalnız P-30 DEMO_INSAN_SURESI** (kabulde hariç). Ara koşumda `GIRDI-2 EKRAN_SUNUCU_UZUNLUK_FARKI 170 > 169` bu turun ürünüydü (MASAK formundaki adres alanı iki dalda sabit `EnFazla="500"` taşıyordu) → `AlanSinirlari.Adres` sabitine bağlandı → **168 ≤ 169** ✓.

### 14.4 Testler
| Koşum | Toplam | Geçti | Kırmızı | Atlanan |
|---|---|---|---|---|
| Filtreli (YasalParam/YasalParametre/Masak370/MasakFormu/ParaDemo2-3/KesifP5/ZOISLEMA/Toplu5/TS2/SATIS3) | 918 | 917 | 0 | 1 |
| Filtreli ek (`Tur2_*`, Tek_islemde, + ilk geniş koşumda yük altında kırmızı görünen CumaM/GirdiBanka/Toplu5Cari tek başına) | 64 | 64 | 0 | 0 |
| **Tam süit (tek koşum, son kod)** `/private/tmp/kuyumcu-serit/trx-pYASAL-PARAM-EKRAN-t2/_Mac_2026-10-08_04_49_08.trx` | **10854** | **10852** | **0** | 2 |

- Tur 1'in tek kırmızısı (`Yasal4HttpTestleri…YENIDEN_DUZENLENECEK…`, UTC gün sınırı, bu paketin değil) bu koşumda yeşil. **Yeni kırmızı farkı = 0.**
- Tur 2'de yeni test vakası: **29** — HTTP 1 (`Tur2_AYNI_deger_409_akla_yatkin_disi_400_buyuk_degisim_isaretli_Turkce_operator_cozulur_denetime_yazilir`), saf 16 (akla yatkınlık 7, büyük değişim 4, operatör başlığı çözme 5), bUnit form 4 (Türkçe ad ASCII kodlu gider · ağ hatası Önizle/Kaydet banda · ileri tarih uyarısı + büyük değişim · bugünden geçerlide aynı gün DegismezIpucu), MASAK formu 1 (isteğe bağlı alanlar kapalı / zorunlu adres dışarıda / TCKN'siz etiket, iki mod), `MasakKademe` 6, PRO panel 1. `Tek_islemde_baslik…` testi yeni kurala göre yeniden yazıldı (sayılmadı).

### 14.5 Canlı doğrulama (API 5162, `kuyumcu_ype_t2`, son kod; yanıtlar okundu — `tur2/canli2-api.json`, `canli2-sql.txt`, `canli2-ek.txt`)
Test kiracısı `PAKETYASALPARAMEKRAN-TEST-T2` sistem davetiyle API'den kuruldu (parola rastgele, 600 izinli dosya; basılmadı).

| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /masak/esikler (önce) | 370.000 / 200.000 | 200 · 370000 / 200000 |
| Dekont C1 160.000 kimliksiz (değişiklikten ÖNCE) | 200 | **200** |
| Önizleme RG tarihi yok / RG sayısı yok / kayıt RG tarihi yok | 400 | **400 / 400 / 400** |
| Önizleme geçmiş başlangıç (dün) | 400 | **400** |
| Önizleme / kayıt SAHİP JWT | 403 | **403 / 403** |
| Kayıt onay kodu yok | 400 | **400** |
| **TUR2** Önizleme AYNI değer (370.000) | 409 | **409** `degerSayi` "aynı değer yeni satır olarak yazılmaz" |
| **TUR2** Önizleme 0 / 999.999.999,99 | 400 | **400 / 400** `degerSayi` "10 katından fazla değişemez (sıfır da olamaz)" |
| **TUR2** Önizleme kodlu ama çözülünce 2 harf ("İz") | 400 | **400** "X-Sistem-Operator başlığı zorunludur (en az 3 karakter)" |
| Önizleme — operatör **"Tasarım Hakemi"** (RFC 5987) | 200 | **200** · 370000 → 150000 · bugunMu true · SIKILASTIRMA · açık sipariş 0 |
| Kayıt (onay koduyla) bugünden geçerli 370.000 → 150.000 | 201 | **201** · kapananSatirId masak370-01 |
| GET /masak/esikler (sonra) / ?tarih=dün | 150.000 / 370.000 | **150000 / 370000** |
| Dekont C2 160.000 kimliksiz (aynı gün, değişiklikten SONRA) | 400 | **400** "MASAK: 150.000 TL ve üstü işlemde kimlik tespiti zorunludur (… kümülatif 160.000,00 TL — eşik 150.000 TL …)" |
| GET /yasal-parametreler (kuyumcu) | RG görünür | 200 · 150000 · 2026-10-08 · DOGRULANDI · RG 2026-10-08/99999 |
| **TUR2** Aynı gün ikinci düzeltme (150.000 → 160.000 bugün) | 409 | **409** "aynı ya da daha ileri tarihli bir değer zaten var" |
| **TUR2** Düzeltme yolu: yarından geçerli önizleme | 200 | **200** · bugunMu false · GEVSETME |
| Açılış: env `Masak__KimlikEsigi=185000`, tablo 370.000 | Sunucu açılır | `/saglik/hazir` **HAZIR**; günlük `YASAL_PARAM_YAPILANDIRMA_FARKLI — tablo esas alınır … yapılandırma 185000 ≠ tablo 370000`; /masak/esikler **370000** |

Bağımsız SQL (`--default-character-set=utf8mb4`):
```
yasal_parametreler  masak370-01  370000  2026-10-07  2026-10-08  DOGRULANDI  2026-10-07  33393  migration:MasakKimlikEsigi370
yasal_parametreler  01M4CG9CG1…  150000  2026-10-08  NULL        DOGRULANDI  2026-10-08  99999  sistem:Tasarım Hakemi
denetim  yasal parametre eklendi · MASAK_KIMLIK_ESIGI_TL · eski 370000.000000 → yeni 150000 · başlangıç 2026-10-08 (BUGÜNDEN GEÇERLİ) · yön SIKILASTIRMA · teyit DOGRULANDI · operatör: Tasarım Hakemi · kaynak RG 2026-10-08/99999 · önceki satır masak370-01 2026-10-08 itibarıyla kapandı · gerekçe: …
banka_hareketleri  160000.00 GELEN 2026-10-08 00:58:20 (UTC)  ← yalnız ÖNCEKİ dekont; sonraki yazılmadı
GERI-ALMA-SONRASI  masak370-01  370000  2026-10-07  NULL  DOGRULANDI  33393   ← gerçek değer aynen
```
Not: geri alma SQL ile yapıldıktan sonra API önbelleği 150.000'i tutmaya devam ettiği için API yeniden başlatıldı (yukarıdaki açılış denetimi bu yeniden başlatmadır); tarayıcı ölçümleri gerçek 370.000/200.000 değerleriyle yapıldı.

Kapanış: API (PID'ler), vekil ve başsız Chrome durduruldu; 5162/5163/9907–9909'da LISTEN yok; 5199/5200'e dokunulmadı. Test kiracısı `kuyumcu_ype_t2`'den kiraci-temizle algoritmasıyla silindi (75 tablo, kalan satır 0; tenants 0). `kiraci-temizle.sh` yalnız `kuyumcu_net`'e baktığı için algoritma aynı kurallarla elle uygulandı. **kuyumcu_net tenants: 17 → 17**; `kuyumcu_net` ve `kuyumcu_ype_t2`'de `masak370-01 370000 NULL`.

### 14.6 Gerçek tarayıcı ölçümü (başsız Chrome, vekil 5163; `sikisma.js` + `ekstra.js`)
Tablolar: `tur2/satis-tablo.txt`, `tur2/onay3-tablo.txt`, `tur2/tur2-analiz.txt`; ham `*.jsonl`; görüntüler `tur2/satis/`, `tur2/onay/`, `tur2/foto/`.

**Satış ekranı — MASAK formu + PRO "Yasal kapılar"** (kayıt yapılmadı; müşteri YPE C2, 250.000 ve 400.000; bağlantılı toplam YPE C1):
| senaryo | genişlik | mod × tema | başlık (iki modda aynı) | kilit metni | PRO kimlik satırı | PRO amaç satırı | taşan · kırık · binen · yatay · gizli-görünen · TRY · kırp · ezik | panel dışı öğe |
|---|---|---|---|---|---|---|---|---|
| 250.000 | 1440 · 1280 · 375 | BASİT+PRO × açık+koyu (12) | Kimlik ve işlem amacı gerekli — 200.000 ₺ ve üstünde | Kimlik ve işlem amacı eksik: 200.000 ₺ ve üstünde kimlik ve işlem amacı alınır. | 370.000 ₺ ve üstünde kimlik (MASAK) · **Altında** | 200.000 ₺ … teyit bekleniyor · **Kimlik ve amaç gerekir** | 0·0·0·0·0·0·0·0 | 0 |
| 400.000 | 1280 · 375 | BASİT+PRO × açık+koyu (8) | Kimlik gerekli — 370.000 ₺ ve üstünde zorunlu | Kimlik eksik: 370.000 ₺ ve üstünde kimlik zorunlu (MASAK). | … · **Kimlik gerekir** | … · **Kimlik ve amaç gerekir** | 0·0·0·0·0·0·0·0 | 0 |
| bağlantılı toplam 410.000 | PRO açık 1280 · BASİT koyu 375 | 2 | Kimlik gerekli — bu müşterinin bugünkü toplamı 410.000 ₺ | Kimlik eksik: bu müşterinin bugünkü toplamı 410.000 ₺; 200.000 ₺ ve üstünde kimlik alınır. | … · Kimlik gerekir | … · Kimlik ve amaç gerekir | 0·…·0 | 0 |

Her kayıtta: doğum tarihi/adres alanı açılışta YOK, "Diğer bilgiler (isteğe bağlı)" katlanırı var, rozet yok, belge no etiketi "T.C. kimlik numarası" (TCKN yok); gerçek mod istenen modla aynı. Dar sütun (K2): BASİT 1280/1440'ta en geniş blok %69–73 (≥ %60), PRO %97–98.
- İlk ölçüm turu iki **ölçüm/ekran** sorunu yakaladı ve ikisi de düzeltilip yeniden ölçüldü: (1) PRO 375'te yeni etiketler kartın dışına taşıyordu ("Kimlik gerek…" kesik; ölçer K3 = 0 göstermesine rağmen görüntüde görüldü) → `yasal.css` sarma kuralı + betiğe "panel dışı öğe" ölçümü eklendi; (2) amaç satırı `metin-amber` sınıfıyla bağlantı mavisi görünüyordu (Platin şemada `metin-amber` = vurgu mavisi) → `metin-kirmizi` (kimlik satırının "gerekir" rengiyle aynı). Kasa yokken açılan kasa listesi başlığa biniyordu (K9=1) — ölçüm senaryosu hatası; test kiracısına kasa eklenip bütün satış ölçümü yeniden koşuldu.

**Onay penceresi** (`/yasal-sinirlar?yonetim=1`, ad **"Tasarım Hakemi"**, Önizle — KAYDEDİLMEDİ): 1280 · 375 × Basit/Profesyonel × açık/koyu = 8 + ileri tarihli 2 (Basit açık 1280, Profesyonel koyu 375): modal 10/10 açıldı, hata bandı yok → Türkçe ad gerçek Chrome'da istek atıyor. taşan 0 · kırık 0 · yatay 0 · K1 0 · TRY 0 · kırp 0; binen 6 = yalnız modal ⟂ alttaki sayfa (modal dışı örnek 0, tur 1 ile aynı).
**Sayfa turu** (`tur.mjs`, `/yasal-sinirlar` + `?yonetim=1`, 1440·1280·375 × BASİT/PRO × açık/koyu = 24 sayfa, 286 katman): geçersiz 0, hata 0, K3/K9/yatay/K1/K5/K13 hepsi 0. K4 "tren" bulguları tur 1 ile aynı (yönetim rotasında açık bırakılan katlanırlar + form; kuyumcu rotası 375'te 2,1 kat).

**Read ile bakılan görüntüler:** `satis/masak__p250__BASIT_aydinlik_1440.png` (tek başlık, kapalı "Diğer bilgiler", zorunlu alanlar), `satis/masak__p400__PRO_aydinlik_1280.png` (aynı başlık kalıbı, MASAK tek cümlesi, kilit "370.000 ₺ … (MASAK)"), `satis/panel__p400__PRO_karanlik_375.png` (ilk turda taşma görüldü → düzeltme sonrası satırlar sarıyor), `satis/panel__p250__PRO_aydinlik_375.png` (kilit 200.000 amaç ↔ panel kimlik "Altında" + amaç "Kimlik ve amaç gerekir", kırmızı), `satis/masak__p250__BASIT_karanlik_375.png`, `onay/onay__Basit_aydinlik_1280.png` (bugünden: sıkılaşıyor uyarısı + DegismezIpucu aynı gün düzeltme yolu), `onay/onay-ileri__Profesyonel_karanlik_375.png` (ileri tarih: "15 Ekim 2026 gününden itibaren…", büyük değişim uyarısı, "bugün" yok), `foto/yasal_sinirlar__PRO_karanlik_1280.png`.

### 14.7 Mutasyon kanıtları — tur 2 (her biri `perl -0777 -pi` tek eşleşme → `dotnet build` 0 hata → filtreli test → yedek geri → `cmp` AYNI; kayıtlar `/private/tmp/kuyumcu-serit/YASAL-PARAM-EKRAN/mut-M2-*.log`)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M2-1 | Operatör başlığı kodlanmadan gönderilir | YasalParamEkranTestleri.Tur2_Turkce_harfli_ad_basliga_ASCII_kodlu_gider_sunucu_cozer_sayfa_COKMEZ | AYNI |
| M2-2 | Önizle ağ hatası yakalanmaz (`throw;`) | Tur2_ag_hatasi_ONIZLE_ve_KAYDET_sayfayi_dusurmez_hata_bandina_yazar | AYNI |
| M2-3 | AYNI değer denetimi kapalı | YasalParamHttpTestleri.Tur2_AYNI_deger_409_… | AYNI |
| M2-4 | `AklaYatkin`'dan `yeni > 0m` kaldırıldı | **0 kırmızı — EŞDEĞER MUTANT** (alt sınır terimi aynı durumu zaten reddediyor; § 14.2) | AYNI |
| M2-4b | `AklaYatkin` alt sınırı kaldırıldı | YasalParamSafTestleri.Tur2_akla_yatkinlik_bandi(370000→36999), Tur2_AYNI_deger_409_… | AYNI |
| M2-5 | Uyarı her zaman "bugün" dalı | Tur2_ileri_tarihte_uyari_BUGUN_demez_… | AYNI |
| M2-6 | Panel kimlik satırı yine "istenen en düşük" (200.000) | YasalParamMasakFormuTestleri.Tur2_PRO_yasal_kapilar_…, Masak370EkranTestleri.Islemler_PRO_yan_sutun_… (2 vaka) | AYNI |
| M2-7 | "Diğer bilgiler" katlanırı açık başlar | Tur2_istege_bagli_alanlar_KAPALI_…, MasakFormuEkranTestleri.ZO28 / ZO28b / ZO28c / ZO28d | AYNI |
| M2-8 | Seçici amaç kademesini "Kimlik" sayar | Tur2_MasakKademe_tek_secici (250.000, 369.999,99), Tek_islemde_baslik_tetikleyen_kademeyi_soyler_… | AYNI |
| M2-9 | Sunucu operatör başlığını çözmez | Tur2_operator_basligi_cozulur (3 vaka), Tur2_AYNI_deger_409_… (denetimde "Tasarım Hakemi") | AYNI |
| M2-10 | PRO etiketi yine "Belge No (TCKN / Pasaport)" | MasakFormuEkranTestleri.ZO28d_BASIT_belge_no_…, Tur2_istege_bagli_…, ParaDemo2IslemEkranTestleri.PD2E_8_…(PROFESYONEL) | AYNI |
| M2-11 | PRO'da amaç kademesi başlığı eski "MASAK Kimlik Tespiti" | Tek_islemde_baslik_tetikleyen_kademeyi_soyler_IKI_MODDA_AYNI_… | AYNI |
Son mutasyondan sonra temiz derleme 0/0; tam süit son kodla koşuldu.

### 14.8 DoD sözleşmesi (tur 2)
Yeni varlık/kayıt türü YOK. `yasal_parametreler` APPEND-ONLY kalır; düzeltme yolu artık ekranda yazılı (`DegismezIpucu`: aynı gün → yarından geçerli yeni satır ya da ByGatech DB düzeltmesi). Migration gerekmedi.

### 14.9 Açık kalanlar (sahibiyle) — tur 2
1. **Kurucu:** `dotnet/Kuyumcu.Web/Pages/Islemler.YasalParam.cs.tmp` (0 bayt, benim artığım) silinmeli — kanca nedeniyle silemedim.
2. **Mehmet (DÜŞÜK, ileride):** sistem anahtarının kiracı uygulamasındaki forma yazılması ve serbest metin operatör adı — ayrı ByGatech yönetim arayüzü + kişiye bağlı operatör kimliği. `TelemetriModulu`/`AbonelikModulu` denetleyicileri operatör başlığını hâlâ ham okur (ekranları yok; curl/betik ASCII gönderir) — ileride `SistemAnahtariKapisi.OperatorCoz`'a bağlanmalı.
3. **Ölçüm aracı (Aşkın):** `sikisma.js` K3, `white-space: nowrap` öğenin kartın sağından taşmasını yakalamadı (görüntüde görüldü) — "kap dışı öğe" ölçüsü ölçere eklenmeli.
4. **Mali müşavir (değişmedi):** M-2 200.000 amaç kademesinin kimlik kapsamı ve dönem geçişinde açık işlemler (§ 13).
5. Tur 1 § 12'deki maddeler (sır olayı, `kuyumcu_ype_t2` veritabanı, 5199/5200 yayını, /uyum para biçimi) aynen geçerli.


---

## 15. TUR 3 (08.10, 05:55–06:45) — önceki doğrulamanın bulguları

Bu bölüm § 1–14'ün üzerine yazar. Kanıt dizini: `ham/YASAL-PARAM-EKRAN-olcum/tur3/` (`satis/`, `onay/`, `liste/`, `canli3-*.json|txt`). Tur öncesi kopyalar: `/private/tmp/kuyumcu-serit/YASAL-PARAM-EKRAN/tur3-once/`.

### 15.1 Bulgu → yapılan
| # | Bulgu | Önem | Yapılan |
|---|---|---|---|
| 1 | Bağlantılı toplamda kilit satırı her zaman en düşük kademeyi (200.000) yazıyor | KRİTİK | Yeni `MasakKademe.ToplamKademesi(e, g)` — `MasakKademe.Sec` ile AYNI kural, ölçü sunucunun bugünkü toplamı: toplam ≥ kimlik eşiği → **"Kimlik eksik: bu müşterinin bugünkü toplamı X; 370.000 ₺ ve üstünde kimlik zorunlu (MASAK)."**; amaç kademesinde → **"Kimlik ve işlem amacı eksik: bu müşterinin bugünkü toplamı X; 200.000 ₺ ve üstünde kimlik ve işlem amacı alınır."**; iki kademenin de altında (beklenmeyen) → genel cümle, sayı uydurulmaz. `Islemler.YasalParam.cs` artık `KimlikIstenenTutar()` basmıyor. Her iki dal için bUnit (satış ekranı, sahte sunucu 370.000/200.000) + saf test. Canlı Chrome: 185.000 dekont + 185.000 fiş → 370.000 dalı; 100.000 + 150.000 → 250.000 amaç dalı (§ 15.5). |
| 2 | Toplam başlığı yalnız "Kimlik gerekli", form amaç da istiyor | ORTA | `MasakFormu` toplam dalında `Gereksinim.AmacGerekli` ise **"Kimlik ve işlem amacı gerekli — bu müşterinin bugünkü toplamı X"** (`BaslikToplamAmac`); amaç istenmiyorsa eski başlık. |
| 3 | Sınır seçilince adres doluyor ama "https:// ile başlamalı" hatası kalıyor | ORTA | `SinirDegisti` adresi önerince `Temizle("kaynakUrl")`. bUnit: boş gönder → sınır seç → hata yok. Chrome 8/8: `urlHata=false`, alan kırmızı değil. |
| 4 | Yer tutucu dolu değer gibi; önerilen eski RG adresi kalıyor | DÜŞÜK | Yer tutucu **"Resmî Gazete bağlantısını yapıştırın"**. Önerilen adres, kullanıcı ona dokunmadıysa ve RG tarihi önerilen satırınkinden farklı girildiyse temizlenir (elle yazılan adrese dokunulmaz); onay penceresinde **"Kaynak adresi: …"** satırı. |
| 5 | Hata durumunda "Sınır" etiketi kırmızıya dönmüyor | DÜŞÜK | `yasal.css`: `.yasal-yonetim .secici-alan:has(.secici-tetik.hatali) > .alan-etiket { color: var(--tehlike); }` (ortak `Secici` DOM'una dokunulmadı). Chrome 8/8: boş gönderimde Sınır etiketi diğer etiketlerle aynı renk (açık `rgb(192,38,45)`, koyu `rgb(255,170,162)`), seçimden sonra normal. |
| 6 | Kartlarda tekrar ve anlamsız satır | DÜŞÜK | 200.000 kartı başlığındaki "(mali müşavir teyidi bekleniyor)" kaldırıldı (rozet yeterli). Teyitsiz kartta "Ne zamandan beri" satırı HİÇ basılmaz. Kimlik akordeonu notu: "Aynı gün aynı müşterinin işlemleri toplanır. **Toplama kuralı: mali müşavir teyidi bekleniyor.**" (`YasalParamSozlugu.ToplamaKurali`; "okuma" iç terimi yok). |
| 7 | Onay penceresinde teknik metin | DÜŞÜK | "Yanlış girdiyseniz yarından geçerli doğru değeri yazın." · "Etkilenecek açık sipariş: N". |
| 8 | SAHİP `?yonetim=1` ile formu ve anahtar alanını görüyor | DÜŞÜK | **Görünürlük sunucudan:** `SistemAyarlari.YonetimKiracilari` (yapılandırma, varsayılan BOŞ = hiçbir kiracıda görünmez, fail-closed) + `YonetimKiracisiMi(tenantId)` (anahtar kullanılamıyorsa da false). `GET /yasal-parametreler` yanıtına `yonetimAcik` eklendi. Ekran: `?yonetim=1` VE `yonetimAcik` → form; aksi hâlde yalnız not **"Bu bölüm ByGatech içindir. Yasal sınırları mevzuat değişince ByGatech günceller."** — form ve "Sistem anahtarı" alanı DOM'da YOK. Yeni rol icat edilmedi; bu yetki DEĞİL görünürlük — yazma yine yalnız sistem anahtarıyla (ByGatech kiracısının SAHİP JWT'si de sistem ucunda 403, § 15.4). |
| — | Satış üst bandında "yasal bir kapıya takıldı" jargonu | ORTA · KAPSAM DIŞI | `Sozlukler.cs` HUB (YasalKapiAcildi) — bu pakette HUB'a dokunmak yasak; § 15.8'e taşındı. |

### 15.2 Sapmalar ve gerekçeleri (tur 3)
- **Görünürlük kararı (Bertan + Mehmet oyu):** "Sistem bağlamı" ekranda bilinemiyordu (kiracı uygulamasında sistem rolü yok; MÜHÜR 4 K1 rol açmayı yasaklıyor). Seçenekler: (a) yalnız istemci yapılandırması — aynı dağıtımdaki tüm kiracılarda açılırdı, reddedildi; (b) anahtar alanını önce sormak — SAHİP yine anahtar alanını görürdü, reddedildi; (c) **sunucu yapılandırmasında ByGatech'in yönetim kiracısı listesi** — seçildi: varsayılan boş (fail-closed), rol değil, yazma yetkisini değiştirmiyor. `appsettings.json` → `Sistem.YonetimKiracilari: []` + açıklama. Üretimde ByGatech kendi kiracı kimliğini ortam değişkeniyle (`Sistem__YonetimKiracilari__0`) verir.
- **Dosya alanı dışı dokunuşlar:** `Api/Modules/Sistem/SistemAnahtariKapisi.cs` (yalnız `SistemAyarlari`'na görünürlük listesi), `Contracts/Sayfalar/YasalSinirlarSozlesmeleri.cs` (`YasalListeVeri.YonetimAcik`, varsayılan false), `Core/I18n/Sozlukler.YasalSinirlar.cs` (yalnız 200.000 kartının başlığı TR+EN), `Web/Servisler/MasakKademe.cs`, `Web/Bilesenler/MasakFormu.razor` (tek ifade), `Web/wwwroot/css/yasal.css` (2 kural, sabit renk yok). `Sozlukler.cs` HUB'a anahtar EKLENMEDİ (yeni anahtarlar `Sozlukler.YasalParam.cs`: HataToplamAmac, BaslikToplamAmac, KaynakYerTutucu, OnayKaynak, YonetimKapali).
- **Dokunulan sayfalar (tek dokunuş kaydı):** `Pages/YasalSinirlar.razor`, `Bilesenler/YasalParamFormu.razor`, `Bilesenler/MasakFormu.razor`, `Pages/Islemler.YasalParam.cs` — hepsi bu paketin önceki turlarında açılmıştı; gerekçe: aynı paketin 3. turu, devralınan bulgular. `Islemler.razor` bu turda AÇILMADI.
- **Değişen eski testler:** `YasalSinirlarEkranTestleri` (teyitsiz KDV kartında başlangıç satırı artık YOK; not metni `ToplamaKurali`), `YasalParamEkranTestleri` (liste yanıtına `yonetimAcik:true`; `Doldur` yeni RG adresini yazar çünkü yeni RG tarihi eski öneriyi temizler; ilk test gövdedeki adresi yeni adresle denetler).
- **Bilinçli olarak yapılmayan:** tek işlemde ≥ 370.000 (örn. 400.000) başlık "Kimlik gerekli — 370.000 ₺ ve üstünde zorunlu" kalıyor; sunucu bu tutarda amaç alanlarını da istiyor. Bulgu yalnız toplam dalını işaret ediyordu; tur 2'de hakemin geçirdiği metni ikinci kez değiştirmemek ve tam süiti tur başına bir kez koşma kuralı için § 15.8'e yazıldı.

### 15.3 Derleme ve denetim
- `dotnet build Kuyumcu.sln --nologo` (son mutasyondan sonra, temiz): **0 Hata, 0 Uyarı** (`t3-b4.log`). Not: ilk derlemede Tests projesi baştan derlenirken başka dosyalarda 4 analizör uyarısı göründü (`Dogrulayici/SUBEKAPSAMASinirTestleri.cs:49`, `KesifP2Testleri.cs:455`, `SonKalanlarTestleri.cs:584/813` — bu paketin değil) ve o sırada başka şeridin `GridTestleri.cs:884/891` derleme hatası vardı (06:05'te başka şerit düzenliyordu; birkaç dakika sonra kendiliğinden düzeldi).
- `bash dotnet/denetim.sh`: **313 ✓, 2 ✗** — `P-30 DEMO_INSAN_SURESI` (kabulde hariç) ve `Layout+Bilesenler emoji ikon 1` ← `Bilesenler/Grid/Izgara.razor` (GRID şeridi, 06:27'de değişti; bu paketin dosyası değil). Bu paketin sayaçlarının hepsi ✓.

### 15.4 Canlı doğrulama (API 5162, `kuyumcu_ype_t2`, son kod; yanıtlar okundu — `tur3/canli3-api.json`, `canli3-sql.txt`, `canli3-yonetim.json`)
Test kiracıları `PAKETYASALPARAMEKRAN-TEST-T3` (kuyumcu) ve `PAKETYASALPARAMEKRAN-TEST-T3-BYGATECH` (yönetim kiracısı) sistem davetiyle kuruldu (parolalar rastgele, 600 izinli dosya, iş sonunda boşaltıldı; basılmadı).

| Adım | Beklenen | Gerçek |
|---|---|---|
| GET /yasal-parametreler `yonetimAcik` (yapılandırma yok) | false | 200 · **false** |
| Önizleme RG tarihi yok / RG sayısı yok / kayıt RG tarihi yok | 400 | **400 / 400 / 400** |
| Önizleme geçmiş başlangıç | 400 | **400** |
| Önizleme / kayıt SAHİP JWT | 403 | **403 / 403** |
| Kayıt onay kodu yok | 400 | **400** |
| AYNI değer / 0 / 999.999.999,99 / kodlu 2 harfli ad | 409 / 400 / 400 / 400 | **409 / 400 / 400 / 400** |
| Önizleme + kayıt bugünden geçerli 370.000 → 150.000 ("Tasarım Hakemi") | 200 / 201 | **200 / 201** |
| /masak/esikler sonra / ?tarih=dün | 150.000 / 370.000 | **150000 / 370000** |
| Dekont C1 160.000 kimliksiz ÖNCE / C2 160.000 SONRA | 200 / 400 | **200 / 400** |
| Aynı gün ikinci düzeltme / yarından geçerli önizleme | 409 / 200 | **409 / 200** |
| **TUR3** Yeniden başlatma: `Sistem__YonetimKiracilari__0=<BYGATECH>` + `Masak__KimlikEsigi=185000` | sunucu AÇILIR | `/saglik/hazir` **HAZIR**; günlük `YASAL_PARAM_YAPILANDIRMA_FARKLI — tablo esas alınır … yapılandırma 185000 ≠ tablo 370000.000000` |
| **TUR3** kuyumcu SAHİP: yonetimAcik / kimlikEsigi / JWT ile sistem önizleme | false / 370000 / 403 | **false / 370000 / 403** |
| **TUR3** ByGatech kiracısı SAHİP: yonetimAcik / kimlikEsigi / JWT ile sistem önizleme | true / 370000 / 403 | **true / 370000 / 403** (görünürlük ≠ yetki) |

Bağımsız SQL (`--default-character-set=utf8mb4`, kuyumcu_ype_t2):
```
yasal_parametreler  01M4CR5Y9J…  150000  2026-10-08  NULL        DOGRULANDI  2026-10-08  99999  sistem:Tasarım Hakemi
yasal_parametreler  masak370-01  370000  2026-10-07  2026-10-08  DOGRULANDI  2026-10-07  33393  migration:MasakKimlikEsigi370
denetim_kayitlari   SISTEM GUNCELLEME YasalParametre · eski 370000 → yeni 150000 · başlangıç 2026-10-08 (BUGÜNDEN GEÇERLİ) · yön SIKILASTIRMA · operatör: Tasarım Hakemi · kaynak RG 2026-10-08/99999 · önceki satır masak370-01 kapandı · gerekçe … · kaynak adresi …
banka_hareketleri   160000 (C1) · 100000 (C3) · 185000 (C4) GELEN — C2'nin değişiklik sonrası dekontu YAZILMADI
GERI-ALMA-SONRASI   masak370-01  370000  2026-10-07  NULL  DOGRULANDI  33393
```
(İlk denetim sorgusu yanlış kolon adıyla `ERROR 1054` verdi; `detay` kolonuyla yeniden koşuldu — dosyada yazılı.)

Kapanış: API, vekil ve başsız Chrome durduruldu; 5162/5163/9908–9910'da LISTEN yok; 5199'a dokunulmadı. İki test kiracısı `kuyumcu_ype_t2`'den kiraci-temizle algoritmasıyla silindi (75 tablo, kalan satır 0; ype tenants **0 → 0**). `kuyumcu_net`'e bu turda HİÇ yazılmadı: başlangıçta 18, sonda 17 (aradaki fark başka şeridin kendi kiracısını silmesi; bu paketin değil). `kuyumcu_net` ve `kuyumcu_ype_t2`'de `masak370-01 370000 NULL` — gerçek değer aynen.

### 15.5 Gerçek tarayıcı ölçümü (başsız Chrome, vekil 5163, CDP 9908/9909/9910; `sikisma.js` + `ekstra.js`)
**Satış ekranı** (`tur3/satis/`, kayıt yapılmadı; 21 koşum):
| senaryo | genişlik × mod × tema | başlık (iki modda aynı) | Kaydet kilidi | PRO kimlik · amaç satırı | taşan · kırık · binen · yatay · gizli-görünen · TRY · kırp | panel dışı |
|---|---|---|---|---|---|---|
| C4: 185.000 dekont + 185.000 fiş (toplam 370.000) | 1440 BASİT açık + 1280/375 × BASİT/PRO × açık/koyu (9) | Kimlik ve işlem amacı gerekli — bu müşterinin bugünkü toplamı 370.000 ₺ | Kimlik eksik: bu müşterinin bugünkü toplamı 370.000 ₺; **370.000 ₺ ve üstünde kimlik zorunlu (MASAK).** | Kimlik gerekir · Kimlik ve amaç gerekir | 0 · 0 · 0 · 0 · 0 · 0 · 0 | 0 |
| C3: 100.000 dekont + 150.000 fiş (toplam 250.000) | 1280/375 × BASİT/PRO × açık/koyu (8) | Kimlik ve işlem amacı gerekli — bu müşterinin bugünkü toplamı 250.000 ₺ | Kimlik ve işlem amacı eksik: bu müşterinin bugünkü toplamı 250.000 ₺; **200.000 ₺ ve üstünde kimlik ve işlem amacı alınır.** | Altında · Kimlik ve amaç gerekir | 0 · … · 0 | 0 |
| C2 tek işlem 250.000 (gerileme) | BASİT açık 1280 · PRO koyu 375 | Kimlik ve işlem amacı gerekli — 200.000 ₺ ve üstünde | Kimlik ve işlem amacı eksik: 200.000 ₺ ve üstünde kimlik ve işlem amacı alınır. | Altında · Kimlik ve amaç gerekir | 0 · … · 0 | 0 |
| C2 tek işlem 400.000 (gerileme) | BASİT açık 1280 · PRO koyu 375 | Kimlik gerekli — 370.000 ₺ ve üstünde zorunlu | Kimlik eksik: 370.000 ₺ ve üstünde kimlik zorunlu (MASAK). | Kimlik gerekir · Kimlik ve amaç gerekir | 0 · … · 0 | 0 |
Dar sütun (K2): BASİT 1280/1440 %69–73, 375 %97; PRO %97–98. `K10b_kesik=1` yalnız PRO 1280'de `#islem-kalem-gram-0 "100,00"` (kalem ızgarası; tur 2'de de 7/22 koşumda aynı öğe — bu paketin değil, § 15.8).

**Yönetim formu — ByGatech kiracısı** (`tur3/onay/`, 1280/375 × BASİT/PRO × açık/koyu = 8 × 3 adım = 24 görüntü; KAYDEDİLMEDİ): (1) boş Önizle → Sınır etiketi diğer etiketlerle aynı hata renginde (8/8), yer tutucu "Resmî Gazete bağlantısını yapıştırın", "Adres https:// ile başlamalı." görünür; (2) sınır seçilince adres önerilir, URL hatası YOK, alan hatalı DEĞİL, Sınır etiketi normal renk (8/8); (3) RG tarihi bugün girilince önerilen 07.10 adresi temizlendi (`urlSonraRg=""`, 8/8), yeni adres yazıldı → onay penceresi açıldı (8/8), "Kaynak adresi: https://…20261008-olcum.pdf", "Etkilenecek açık sipariş: 0", ipucu "Yanlış girdiyseniz yarından geçerli doğru değeri yazın.". Ölçü: taşan/kırık/yatay/gizli-görünen/TRY/kırp 0; binen 6 = yalnız modal ⟂ alttaki sayfa (tur 1–2 ile aynı).

**Kuyumcu SAHİP — `/yasal-sinirlar` ve `?yonetim=1`** (`tur3/liste/`, 1440/1280/375 × BASİT/PRO × açık/koyu, 20 sayfa): `?yonetim=1`'de form YOK, "Sistem anahtarı" alanı YOK, not var (8/8); 200.000 kartı başlığı parantezsiz, teyitsiz kartlarda "Ne zamandan beri" satırı YOK, doğrulanmış kimlik kartında "Ne zamandan beri: 7 Ekim 2026"; kimlik akordeon notu "Toplama kuralı: mali müşavir teyidi bekleniyor". Bütün ölçüler 0; K2 %97–98.

**Read ile bakılan görüntüler:** `satis/kilit__p185toplam__BASIT_aydinlik_1440.png` (hakemin senaryosu: başlık "Kimlik ve işlem amacı gerekli — … 370.000 ₺", kilit "370.000 ₺ ve üstünde kimlik zorunlu (MASAK)", 200.000 yok), `satis/kilit__p150amactoplam__BASIT_aydinlik_375.png` (amaç dalı kilidi), `satis/panel__p185toplam__PRO_aydinlik_375.png` (kilit + PRO panel aynı kademe), `onay/1-bos-hata__BASIT_aydinlik_1280.png` (Sınır etiketi kırmızı, yer tutucu), `onay/2-sinir-secildi__PRO_karanlik_1280.png` (adres dolu, URL hatası yok), `onay/3-onay__PRO_karanlik_375.png` (kaynak adresi satırı, sade metinler), `liste/liste__BASIT_aydinlik_1440.png` (kartlar + not), `liste/yonetim1__PRO_karanlik_375.png` (kuyumcuda yalnız not).

### 15.6 Testler
| Koşum | Toplam | Geçti | Kırmızı | Atlanan |
|---|---|---|---|---|
| Filtreli (YasalParam / YasalSinirlar / Masak370 / MasakFormu / SATIS3 / YASALPARAMEKRAN) | 709 | 708 | 0 | 1 |
| Filtreli `Tur3` + YasalSinirlarEkran + `Yonetim_` (yeni testler adlarıyla `t3-f2.log`) | hepsi yeşil | | 0 | |
| **Tam süit (tek koşum, son kod)** `/private/tmp/kuyumcu-serit/trx-pYASAL-PARAM-EKRAN-t3/_Mac_2026-10-08_06_14_47.trx` | **10925** | **10922** | **1** | 2 |

- Tek kırmızı: `IkonTestleri.Layout_ve_Bilesenler_altinda_emoji_KALMADI` ← `Bilesenler/Grid/Izgara.razor` (GRID şeridi; denetim.sh emoji sayacıyla aynı kaynak). Bu paketin dosyalarında emoji yok. **Bu paketin yeni kırmızı farkı = 0.**
- Tur 3'te yeni test: **13 yöntem / 18 vaka** — bUnit yönetim 5 (bayat URL hatası + yer tutucu + seçici hatalı · RG tarihi değişince öneri temizlenir / elle adres korunur · onay kaynak satırı + sade metinler · SAHİP `?yonetim=1` form/anahtar YOK · kart satırları), saf 4 yöntem/9 vaka (`ToplamKademesi` 6 sınır + 1, `YonetimKiracisiMi`, denetleyici `yonetimAcik`), HTTP 1 (SAHİP `yonetimAcik=false`), MASAK formu 1 (toplam + amaç başlığı), satış ekranı 2 (kilit metni iki dal).

### 15.7 Mutasyon kanıtları — tur 3 (`mut3.sh`: yedek → tek eşleşme → `dotnet build` 0 hata → filtreli test → `cp` + `touch` geri → `cmp` AYNI; kayıtlar `/private/tmp/kuyumcu-serit/YASAL-PARAM-EKRAN/mut-M3-*.log`)
| Kod | Değişiklik | Kırmızı test(ler) | cmp |
|---|---|---|---|
| M3-1 | `ToplamKademesi`'nden kimlik eşiği dalı kaldırıldı | SATIS3EkranTestleri.Tur3_baglantili_toplam_KIMLIK_esiginde_kilit_370000_MASAK_der_200000_DEMEZ · YasalParamTur3SafTestleri.Tur3_ToplamKademesi_MasakKademe_ile_ayni_kural(370000, 410000) | AYNI |
| M3-2 | Kilit kimlik dalı yine `KimlikIstenenTutar()` (eski hata) | SATIS3EkranTestleri.Tur3_baglantili_toplam_KIMLIK_… | AYNI |
| M3-3 | Toplam başlığı amaçtan bağımsız `BaslikToplam` | YasalParamTur3MasakFormuTestleri.Tur3_baglantili_toplamda_form_amac_istiyorsa_… · SATIS3EkranTestleri.Tur3_baglantili_toplam_KIMLIK_… | AYNI |
| M3-4 | `SinirDegisti`'den `Temizle("kaynakUrl")` kaldırıldı | YasalParamEkranTestleri.Tur3_bos_gonder_sonra_sinir_sec_kaynak_hatasi_BAYAT_kalmaz_… | AYNI |
| M3-5 | RG tarihi değişince öneri temizlenmez | YasalParamEkranTestleri.Tur3_RG_tarihi_degisince_onerilen_eski_adres_temizlenir_… | AYNI |
| M3-6 | Form `yonetimAcik`'e bakmadan açılır | YasalParamEkranTestleri.Tur3_kuyumcu_SAHIP_yonetim_1_ile_formu_ve_sistem_anahtari_alanini_GORMEZ_not_gorur | AYNI |
| M3-7 | `YonetimKiracisiMi` listeyi `||` ile atlar | YasalParamTur3HttpTestleri.Tur3_kuyumcu_SAHIP_liste_yanitinda_yonetimAcik_FALSE · YasalParamTur3SafTestleri.Tur3_YonetimKiracisiMi_… · Tur3_liste_ucu_yonetimAcik_… | AYNI |
| M3-8 | Başlangıç satırı teyitsizde de basılır | YasalParamEkranTestleri.Tur3_kartlar_teyitsizde_… · YasalSinirlarEkranTestleri.Ekran_SAHIP_kartlar_sade_ad_tr_deger_rozet_sozlukten_ham_kod_yok | AYNI |
Son mutasyondan sonra temiz derleme 0/0.

### 15.8 DoD sözleşmesi (tur 3) ve açık kalanlar (sahibiyle)
- **DoD:** yeni varlık/kayıt türü YOK, migration YOK. `yasal_parametreler` APPEND-ONLY (düzeltme: yarından geçerli yeni satır; ekranda `DegismezIpucu`). Yeni yapılandırma `Sistem.YonetimKiracilari` yalnız görünürlük; yazma yetkisi değişmedi (sistem anahtarı + iki adımlı onay + denetim izi).
1. **GRID şeridi:** `Bilesenler/Grid/Izgara.razor` emoji (`IkonTestleri` kırmızı + denetim.sh ✗) — bu paketin değil.
2. **Satış/MASAK formu sadeleştirme paketi:** (a) tek işlemde ≥ 370.000 başlık "Kimlik gerekli — 370.000 ₺ ve üstünde zorunlu" iken sunucu amaç alanlarını da istiyor — toplam dalındaki kuralın ("amaç isteniyorsa başlık söyler") tek işlem dalına da uygulanması önerilir; (b) üst banttaki "yasal bir kapıya takıldı" jargonu (`Sozlukler.cs:4819 YasalKapiAcildi`, HUB — ayrı paket); (c) PRO 1280 kalem ızgarasında `#islem-kalem-gram-0` kesik ölçüsü (tur 2'de de vardı).
3. **Kurucu:** `dotnet/Kuyumcu.Web/Pages/Islemler.YasalParam.cs.tmp` (0 bayt, tur 2 artığı) hâlâ silinmeli — kanca nedeniyle silinemiyor. Üretimde formun görünmesi için ByGatech kiracı kimliği `Sistem__YonetimKiracilari__0` ortam değişkeniyle verilmeli (verilmezse form hiçbir yerde görünmez — güvenli varsayılan).
4. **Mehmet (değişmedi, ileride):** ayrı ByGatech yönetim arayüzü + kişiye bağlı operatör kimliği; `TelemetriModulu`/`AbonelikModulu` operatör başlığını ham okur.
5. **Mali müşavir (değişmedi):** M-2 200.000 amaç kademesinin kimlik kapsamı; dönem geçişinde açık işlemler (§ 13).

### 15.9 Değişen dosyalar (tur 3)
`dotnet/Kuyumcu.Core/I18n/Sozlukler.YasalParam.cs` · `dotnet/Kuyumcu.Core/I18n/Sozlukler.YasalSinirlar.cs` · `dotnet/Kuyumcu.Web/Servisler/MasakKademe.cs` · `dotnet/Kuyumcu.Web/Pages/Islemler.YasalParam.cs` · `dotnet/Kuyumcu.Web/Bilesenler/MasakFormu.razor` · `dotnet/Kuyumcu.Web/Bilesenler/YasalParamFormu.razor` · `dotnet/Kuyumcu.Web/Pages/YasalSinirlar.razor` · `dotnet/Kuyumcu.Web/wwwroot/css/yasal.css` · `dotnet/Kuyumcu.Contracts/Sayfalar/YasalSinirlarSozlesmeleri.cs` · `dotnet/Kuyumcu.Api/Modules/Yasal/YasalParametreModulu.cs` · `dotnet/Kuyumcu.Api/Modules/Sistem/SistemAnahtariKapisi.cs` · `dotnet/Kuyumcu.Api/appsettings.json` · `dotnet/Kuyumcu.Tests/YasalParamEkranTestleri.cs` · `dotnet/Kuyumcu.Tests/YasalSinirlarEkranTestleri.cs` · `dotnet/Kuyumcu.Tests/YasalParamTur3Testleri.cs` (yeni). (Depo tek "first commit"te; bu dosyaların çoğu `git status`'ta `??` görünür.) COMMIT YOK; silinen dosya yok.
