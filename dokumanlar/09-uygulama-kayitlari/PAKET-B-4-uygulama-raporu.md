# PAKET B-4: Karma ödemede altın bacağı (D3), uygulama raporu

**Tarih:** 2026-09-23 (tur 1) · 2026-09-24 (tur 2, § 0) · **Şerit:** S-B (backend şekillendirme) · **Rol:** Taşkın (backend) + Ertuğ (ekran bileşeni)
**Şartname:** `dokumanlar/02-mimari/sadelestirme/KARAR-HAS-DEFTERI-2026-09-23.md` (§ 4, 35 madde; § 5, 27 test; § 6, 20 mutasyon)
**Ön koşul:** P-45, ORKESTRATOR-DURUM.md "KAPIDAN GEÇENLER" tablosunda geçti olarak görünüyor (4010). Pakete bu doğrulamadan sonra başlandı.
**Commit:** YOK. Tüm değişiklikler çalışma ağacında duruyor.

---

## 0. TUR 2 (2026-09-24 00:05–00:50): doğrulama bulgularının kapatılması

Hasım doğrulayıcı tur 1'de beş bulgu bıraktı. Bu turda hepsi ya **kodla kapatıldı** ya da **kararı yazıldı / sahibine devredildi**. Ayrıntı aşağıda; tur 1'in bölümleri (§ 1–§ 12) geçerliliğini koruyor, değişen yerler işaretlendi.

### 0.1 [KRİTİK] B4-3: eski altınla ödeme ekrandan alınamıyordu → KAPANDI

**Sorun (doğrulandı):** `FisOdemeKasa.razor:202` tek ödeme listesinde `ALTIN` sunuyordu; sunucu bunu G-1 gereği 400 ile reddediyor. `AltinBacagiPaneli` hiçbir sayfaya bağlı değildi. Tezgâhtar seçeneği görüyor, seçince hata alıyor ve eski altınla ödeme almanın başka yolu yoktu.

**Neden bu paket bağladı (S2-4'ü beklemeden):** S2-4 şartı "B-4 kapıdan geçmiş olmalı" diyor; B-4 ise B4-3 yüzünden geçemiyordu. Bu bir kilitlenmeydi. Doğrulayıcı "G-1 ile ekran bağlantısı aynı dalgada sevk edilmeli, biri tek başına sevk edilmemeli" dedi. Bu turun görev listesinde S-2 eşzamanlı şerit olarak yasaklanmamıştı. Bu yüzden bağlantı burada yapıldı. S-2 şeridi aynı saatlerde `Islemler.razor` ve `Bilesenler/Fis/*` üzerinde S2-2 mutasyonları koşuyordu. Çakışmayı önlemek için yalnız küçük, yerel `Edit` yamaları yapıldı ve her yamadan önce dosya yeniden okundu. Tam dosya üzerine yazılmadı. 00:33'te S-2'nin `Islemler.razor`'u kendi başına sıkıştırdığı görüldü (1397 satır). B-4 satırları o sürümde de duruyor (grep: 7 eşleşme).

**Yapılanlar:**
| Dosya | Değişiklik |
|---|---|
| `Bilesenler/Fis/FisOdemeKasa.razor` | SATIS tek ödeme listesinden `ALTIN` **çıkarıldı** (sunucunun reddedeceği seçenek gösterilmiyor). Yerine `#islem-odeme-altin-ipucu` eklendi: "Eski altınla ödeme için 'Karma ödeme' seçeneğini açıp 'Altın' bacağı ekleyin." Etiketler sözlükten okunuyor (UX-S31). |
| `Bilesenler/Fis/FisOdemeBacaklari.razor` | `BacakAraclari` listesine `("ALTIN", Dil.T.Islem.OdemeAltin)` eklendi (B4-3). ALTIN bacağında TL giriş alanı yerine `AltinBacagiPaneli` çiziliyor (B4-31..35). Kasa seçicisi çizilmiyor (B4-5). Başka bir bacak ALTIN ise bu bacağın listesinde ALTIN yok (B4-6). "Kalanı ata" ALTIN bacağına yazmıyor; hedef yoksa düğme `disabled` (DoD EK 1: boşa tıklanan düğme yok). Bacak ALTIN'a geçince `AltinSecildi` ile sayfa ayarları yüklüyor. |
| `Bilesenler/Fis/FisAltinBacagi.cs` (YENİ) | Fiş tarafındaki ALTIN kuralları tek yerde toplandı: `AltinMi`, `KasaAlir`, `AracSecenekleri`, `AracDegisti`, `DurumYaz`, `KalanHedefi`, `Tamam`, `Govde`, `PusulaNotu`, `NotBirlestir`. İstek atmıyor (S21 yapı testi: dosyada `ApiIstemci`/`GetAsync`/`Api.` yok). |
| `Bilesenler/Fis/FisModelleri.cs` | `FisBacagi`'ye iki alan eklendi: `Altin` (girdiler) ve `AltinDurum` (panelin kilit ve eksik bildirimi). |
| `Servisler/AltinBacagiGirisi.cs` | Yeni `AltinBacagiAyarKaynagi.Yukle(ApiIstemci)` eklendi: `GET /islemler/altin-bacagi/ayarlar`. Eksik yanıtta ya da hatada null dönüyor (fail-closed); panel `#altin-ayarlar-yok` bandını gösteriyor ve kayıt kilitli kalıyor (UX-S28). |
| `Pages/Islemler.razor` | `HediyeAltinCakismasi` **kaldırıldı** (tek ödemede ALTIN yok, fiş ALTIN bacağını tanıyor). `KarmaGonderilebilir` şunları kontrol ediyor: ALTIN bacağı kasa istemiyor; `FisAltinBacagi.Tamam` en çok bir ALTIN bacağı, satıcı kimliği, ayar/gram/fiyat ve FARK 0 şartlarını arıyor. Bunlardan biri eksikse **sunucuya gidilmiyor** (B4-32/33). Gövdede ALTIN bacağı kendi sözleşmesiyle gidiyor: ayar, gram, has fiyatı, satıcı; kasaId **yok**. Girdiler kayıt anında yeniden hesaplanıyor. Başarı bandına sunucunun döndürdüğü **gider pusulası numarası** yazılıyor (UX-S27). Satır sayısı ≤ 1400 (S21 kabulü; ölçüm 1397). |
| `Kuyumcu.Core/I18n/AltinBacagiSozlugu.cs` | `PusulaDogdu` ve `TekOdemedeYok` anahtarları eklendi (TR + EN; konumsal record, eksik anahtar derleme hatası veriyor). `Sozlukler.cs` hub'ına dokunulmadı. |

**Mevcut hâl korundu:** DOM id kaldırılan **0**. `Islemler.razor`, `FisOdemeBacaklari.razor` ve `FisOdemeKasa.razor` için önce/sonra `id="..."` listesi karşılaştırıldı. Tek fark eklenen `islem-odeme-altin-ipucu`. Uç sayısı 375 → 375. Tek ödemenin geri kalanı değişmedi: NAKIT, KART, HAVALE, VERESIYE. **Basit mod:** karma ödeme `ProAlan` içinde ve basit modda ALTIN zaten hiç yoktu. Tur 2 bunu değiştirmedi; basit modda karmanın açılması S2-4'ün kararı ("Basit modda karma ödeme HER ZAMAN açık").

**Değişen eski test (gerekçeli, silme yok):** `PosBarkodTestleri.SATIS_secicisinde_ALTIN_ve_VERESIYE_HALA_VARDIR` testinin adı `SATIS_secicisinde_VERESIYE_HALA_VAR_ALTIN_bacaga_tasindi` oldu. İddia artık şu: tek listede ALTIN **yok**, VERESIYE var, ipucu var. Eski iddia G-1 sonrası ekranın sunucunun reddedeceği seçeneği göstermesini şart koşuyordu; bu doğrulayıcının KRİTİK bulgusunun ta kendisiydi.

**Yeni testler (10):** `Kuyumcu.Tests/B4AltinBacagiFisEkranTestleri.cs`
- `B4AltinBacagiFisEkranTestleri` (bUnit, `Islemler` sayfası uçtan uca, 6 test):
  - W-1 `AltinBacagi_Tek_Odeme_Listesinde_ALTIN_Yok_Ipucu_Var`
  - W-2 `AltinBacagi_Karma_Listesinde_Var_Secilince_Panel_Acilir_Kasa_Yok`: kasa yok, TL girdisi yok, ayarlar GET'i atılıyor, iki belge notu görünüyor.
  - W-3 `AltinBacagi_Ikinci_Bacakta_ALTIN_Secenegi_Cikmaz`
  - W-4 `AltinBacagi_Satici_Kimligi_Eksikken_Kaydet_Kilitli_Tamamlaninca_Acik`
  - W-5 `AltinBacagi_Govde_Altin_Sozlesmesini_Tasir_Pusula_No_Banda_Yazilir`: Türkçe JSON gövdesi ayrıştırılarak sınanıyor. ayarId K22, gram 5, fiyat 2000, satıcı alanları, `kasaId` yok, tutarTl 9160, NAKIT 840 + ks1; pusula no bantta.
  - W-6 `AltinBacagi_Kalani_Ata_Altin_Bacagina_Yazmaz`
- `B4AltinBacagiFisSafTestleri` (4 test): `…Arac_Degisince_Kasa_Bosalir_Cikinca_Girdi_Temizlenir`, `…Tamam_Iki_Altin_Ya_Da_Kilitli_Durumda_False`, `…Kasa_Almaz_Kalan_Hedefi_Olmaz_Govde_Yalniz_Altinda`, `…Pusula_Notu_Yanittan_Okunur_Yoksa_Null`.

**Ekran bağlantısı mutasyonları (11/11 kırmızı, hepsi `cmp` birebir; yedek `scratchpad/mut-yedek/B-4-t2/`, sürücü `scratchpad/b4t2/mut.py`):**

| Kod | Mutasyon | Kırmızı dönen test(ler) | cmp yedek / gerçek ağaç |
|---|---|---|---|
| WM-1 | Tek ödeme listesine ALTIN geri eklendi | W-1 `AltinBacagi_Tek_Odeme_Listesinde_ALTIN_Yok_Ipucu_Var` | birebir / birebir |
| WM-2 | Bacak listesinden ALTIN çıkarıldı | W-2, W-3, W-4, W-5, W-6 | birebir / birebir |
| WM-3 | `KasaAlir`: ALTIN kasa alır | W-2, W-4, W-5, W-6, `…Kasa_Almaz_Kalan_Hedefi_Olmaz…` | birebir / birebir |
| WM-4 | `Tamam`: satıcı/fark kilidi kaldırıldı | W-4, `…Tamam_Iki_Altin_Ya_Da_Kilitli_Durumda_False` | birebir / birebir |
| WM-5 | İkinci bacakta ALTIN süzgeci kaldırıldı | W-3 | birebir / birebir |
| WM-6 | Kalanı ata son bacağa (ALTIN dahil) | W-6, `…Kalan_Hedefi…` | birebir / birebir |
| WM-7 | ALTIN gövdesi üretilmiyor (genel gövdeye düşüyor) | W-5 | birebir / birebir |
| WM-8 | İki not birleşmiyor (pusula notu düşüyor) | `…Pusula_Notu_Yanittan_Okunur_Yoksa_Null` | birebir / birebir |
| WM-8b | `Islemler` pusula notunu banda yazmıyor | W-5 | birebir / birebir |
| WM-9 | ALTIN seçilince ayarlar yüklenmiyor | W-2, W-4, W-5, W-6 | birebir / birebir |
| WM-10 | ALTIN'a geçişte kasa boşaltılmıyor | `…Arac_Degisince_Kasa_Bosalir…` | birebir / birebir |

**Yöntem notu (paralel şerit):** Aynı ağaçta S-2 şeridi FisFark.cs, BasitPos.cs, FisVerdimAldim, FisIslemIzgarasi ve FisKalemGirisi üzerinde **mutasyon koşuyordu** (`mut-yedek/S2-2/M-S22-1..10`, 00:17–00:24). Paylaşılan `bin/` bu yüzden bir ara S-2'nin mutant derlemesini taşıdı: `FisFark.Hesapla` işareti çevrilmişti ve 19000 ≠ 1000 çıktı. Karma testleri bu yüzden sahte kırmızı döndü. Ölçümler bu nedenle **yalıtılmış bir kopyada** yapıldı: `scratchpad/b4iz/`, `bin/obj` hariç ağacın rsync'i, S-2'nin mutasyon yedekleriyle `cmp` yapılarak mutantsız olduğu doğrulandı. Son tam süit, 00:39'da **taze** alınan bir anlık görüntüde koşuldu. O an için S-2 ve S3-2 yedeklerinin gerçek ağaçla aynı olduğu, yani devam eden bir mutasyon olmadığı `cmp` ile doğrulandı.

### 0.2 [ORTA] G-5: satış raporu altın bacağını göstermiyor → SAHİBİNE DEVREDİLDİ (PARA)

`Modules/Rapor/SatisRaporlari.cs` PARA şeridinin alanı ve bu turun görev metni de o dosyaya dokunmayı yasaklıyor. Kod yazılmadı. Sahibi için gereken bilgi şu: kaynak `odeme_kalemleri.arac='ALTIN'`. Canlıda üç ALTIN kalemi ölçüldü; `hareket_id` HURDA_ALIS satırını gösteriyor, `kasa_id` NULL. Ödeme aracı kırılımına "Eski altın (mahsup)" satırı eklenmeli ve toplam kartı bunu nakde katmamalı.

### 0.3 [ORTA] satis-fisi ucunda rol kapısı → KARAR: KAPI KONMADI (ekip oyu, gerekçe aşağıda)

Soru şuydu: ALTIN bacaklı fiş bir HURDA_ALIS hareketi ve gider pusulası doğuruyor, `hurda-alis` ise `[RolGerekli("SAHIP","MUDUR","MUHASEBE")]` istiyor. `satis-fisi`'ne rol kapısı konmalı mı? Ölçümle cevaplandı:

| Ölçüt | Bulgu (kod) |
|---|---|
| Aynı iş, mevcut yol | `POST /islemler/takas` (eski ürün → yeni ürün, tezgâhta eski altın alımı) **rol kısıtsız**: `SatisModulu.cs` controller, yalnız sınıf düzeyinde `[ModulYetkisi("islem")]`. |
| B-4 öncesi hâl | Tek etiketli `OdemeTuru="ALTIN"` `POST /islemler/satis` üzerinden **tüm rollere** açıktı; pusulasız ve kimliksiz alınıyordu, kasaya da TL yazıyordu. |
| Para riski | ALTIN bacağında kasadan para **çıkmıyor** (B4-5/B4-19, T-4, canlı 7: kasa yalnız nakit kadar arttı). `hurda-alis`'in MUHASEBE+ kapısının asıl gerekçesi kasadan nakit çıkışı; o risk burada yok. |
| Kötüye kullanım izi | Her ALTIN bacağı satıcı kimliği (VUK md.234, eşiksiz) ve gider pusulası no ile yazılıyor. HURDA_ALIS satırı hurda has bakiyesine düşüyor (T-23); sayım farkı hurda stoğunda görünür. |
| Sonuç | Rol kapısı koymak, tezgâhtarı bugün yapabildiği işten (takas, eski altınla ödeme) mahrum bırakırdı. Bu "özellik silinmez" kuralına aykırı bir gerileme olurdu. |

**Karar:** `satis-fisi` ucuna rol kapısı **konmadı**. Mevcut `[ModulYetkisi("islem")]` + `[Authorize]` korundu. B-4 yetkiyi genişletmedi, aksine sıkılaştırdı: kimlik, pusula ve kasa dışı yazım zorunlu. **Mehmet'e (güvenlik) not:** Bu karar Taşkın ve Ertuğ'un ölçümüne dayanıyor. İleride "tezgâhtar eski altın kabul edemez" diye bir iş kuralı istenirse bu yetki ayarı rolle değil, `ModulYetkisi` altında ayrı bir izin anahtarıyla açılmalı (ör. `islem.eski-altin`). O durumda aynı anahtar `takas`'a da uygulanmalı, yoksa iki yol arasında asimetri doğar. Bu bir öneri; kodda yok.

### 0.4 [ORTA] T-24 fiş stornosu yok → AYRI PAKET OLARAK PLANLANDI (orkestratöre)

48-A motoru SATIS ve HURDA_ALIS'i bilinçli olarak stornolamıyor (§ 2 S-1). Bu turda motor genişletilmedi, çünkü storno taklidi yasak (DoD EK 2). Önerilen paket **"FİŞ STORNOSU (B-4 eki)"** şunları kapsamalı:
1. Fişin bütün hareketleri tek transaction'da ters kayıtla dönmeli: SATIS, HURDA_ALIS ve ödeme kalemleri.
2. Stok ve maliyet partisi geri alınmalı.
3. Gider pusulası iptal edilmeli (mevcut `POST /gider-pusulalari/{id}/iptal`).
4. Kasa ters hareketi yazılmalı.
5. Test: T-24'ün şartnamedeki hâli ("iki hareket ters işaretle döner, hurda has bakiyesi ve kasa başlangıca eşitlenir").

Bugünkü düzeltme yolu satış iadesi ve pusula iptali. Ekranda bu yol, DegismezIpucu ile anlatılıyor.

### 0.5 [DÜŞÜK] Ağaçtaki eski artıklar ve paylaşılan test VT → SAHİBİNE

Silme yetkisi yok, hook `rm`'yi engelliyor. Durum şöyle:
- Sep 21 tarihli `.mutorj` ve `.bak` artıkları `denetim.sh`'ın **beyaz listesinde**. Beyaz liste çürürse `MigrationHijyeniTestleri.Mutorj_beyaz_listesi_curumez` kırmızıya dönüyor. Bu yüzden onları silmek de bir karar gerektiriyor: artık ve liste birlikte temizlenmeli. **Orkestratöre** bırakıldı.
- Sıfır baytlık `FiyatListesiVeKampanya` migration kopyaları da aynı beyaz listede.
- `MigrationHazirlikTestleri` sabit `kuyumcu_t1_eski` adını kullanıyor. Bu bir paralel test altyapısı sorunu (**orkestratör / QA altyapısı**). Bu turun tam süitinde kırmızı dönmedi.
- Bu turda proje ağacına `.mutorj`, `.bak` ya da `.tmp` **bırakılmadı**. Tüm yedekler `scratchpad/mut-yedek/B-4-t2/` altında.

### 0.6 Tur 2 kapıları (ölçüm)

| Kapı | Sonuç | Kanıt |
|---|---|---|
| Derleme | `dotnet build Kuyumcu.sln` → **0 Uyarı, 0 Hata** | yalıtılmış taze kopya, `scratchpad/b4t2/iz-build3.log`. Gerçek ağaçtaki ilk derleme de 0 Hata verdi; 2 uyarı MSB3026 dosya kilidiydi (başka şeridin test koşumu). |
| B-4 testleri (filtreli) | `B4AltinBacagi*` + S21/S22/Karma/PosBarkod/Doviz/P23 → **127/127** | `scratchpad/b4t2/iz-test3.log` + taze tam süit |
| Tam süit | **4612 / 4612 başarılı, 0 başarısız** | trx `scratchpad/trx-pB-4/_Mac_2026-09-24_00_40_14.trx` |
| Test sayısı | Tur 1 sonu 4541 → tur 2 ölçümü 4612. B-4 tur 2 **+10** test ekledi, 1 testin adı değişti, **0 silindi**. Aradaki fark diğer şeritlerin eklemeleri. | |
| (7) Yeni kırmızı | **0.** Tam süitte kırmızı yok. | |
| denetim.sh | Gerçek ağaç ve yalıtılmış kopya aynı sonucu veriyor: **tek ✗** `Pages: @onclick'siz ve disabled'sız <button> 74 (tavan 71)`. **B-4 kaynaklı değil:** B-4'ün dokunduğu tek sayfa olan `Islemler.razor` bu sayaçta önce 0, sonra 0. Bileşenler (`Bilesenler/`) bu sayaca girmiyor. HEAD'e göre artış gösteren son düzenlenmiş sayfalar: `Cariler.razor` 1→15 (S3-A, 00:33), `SiparisDetay.razor` 0→2 (SERİT A, 00:33), `Raporlar.razor` 0→1 (S3-B, 00:34). B-4'ün sayaçları hep ✓: Hareketler.Add 1, P-08 0, P-23 ×3, IgnoreQueryFilters 0, B-3 ESIK_SABIT_SUNUCUDA 0 ve ESIK_ISTEMCIDE 4/4, P-18 SABIT_RENK 0, ModalPencere 55/55. | `scratchpad/b4t2/denetim2.log` |
| Uç sayısı | 375 → 375 | `grep -rn "\[Http" Kuyumcu.Api` |
| DOM id | kaldırılan **0**, eklenen 1 (`islem-odeme-altin-ipucu`) | önce/sonra id listesi |
| Özellik kaybı | `envanter-uret.sh` yeniden koşuldu. ENVANTER-S0'a göre "eksik" görünen `islemler` id'lerinin **hepsi** `Bilesenler/Fis/*` altında duruyor (16/16 grep; S2-1 taşıması). `giris` id'leri S-1'e ait. B-4 kaynaklı silinen **0**. Uç: 372 statik (S0: 361). | `scratchpad/b4t2/envanter.json` |
| Canlı (5193) | 19/19 adım beklenen kodla döndü (aşağıda) | `scratchpad/pB-4-t2/canli-cikti.txt` |
| Test kiracısı | `PAKETB4-TEST-CANLI2` silindi (`kiraci-temizle.sh --uygula`; ikinci ölçüm "Kiracı YOK, bağlantı sınandı"). Davet satırı silindi (0). `tenants` önce **11**, sonra **11**. QA-TEST-*, DEMO ve PAKETS3-2-TEST'e dokunulmadı. | |
| API durduruldu | pid 96872/96864 kill edildi; `lsof :5193` boş | |

**Canlı doğrulama tur 2** (API yalıtılmış taze derlemeden, 5193, `kuyumcu_net`). Tur 1'in 17 adımı aynen tekrarlandı ve hepsi beklenen kodu döndü. Yeni iki adım:

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 18 | **EKRANIN gönderdiği gövde** (`FisAltinBacagi.Govde` biçimi, `tedarikciFaturaNo: null` dahil): 10.000 = ALTIN 9.160 (K22 5 g × 2.000) + NAKIT 840 | 200 | **200** | fisId var · net 9160 · belge 2 · GP-2026-000003 · kasa farkı **+840** (altın payı kasaya girmedi) |
| 19 | Eski ekranın tek-ödeme biçimi fiş ucunda (`arac: ALTIN`, altın alanları yok, kasa var) | 400 | **400** | "Eski altın bacağına kasa verilemez — altın kasaya TL olarak girmez" (seçeneğin ekrandan kaldırılma gerekçesi) |

SQL (test kiracısı): 3 fiş için 3 SATIS (brüt, `odeme_turu=KARMA`, `fis_id` dolu) ve 3 HURDA_ALIS (−net / +4,58 has, ALTIN, `fis_id` boş, adet NULL) yazıldı. `kur_snapshot_id` tekil sayısı 1. `gider_pusulalari` GP-2026-000001..3 (hareket = HURDA_ALIS ✓). `odeme_kalemleri`: ALTIN ×3 (kasa NULL, hareket=HURDA_ALIS), NAKIT ×3 (kasa dolu). `kasa_hareketleri` yalnız NAKIT: 10840 · 11023,20 · 840. `cari_bakiyeler` TL 0,00 · has 0,000000. G-3: `odeme_turu='ALTIN'` sayısı **0**.

**Dokunulan sayfalar (DoD EK 2, TEK DOKUNUŞ):** `Pages/Islemler.razor`. Gerekçe: S-2 şeridi bu sayfayı aynı dalgada açıyor (S2-1/S2-2). B-4'ün dokunuşu KRİTİK bulgu B4-3'ün kapatılmasıdır; S2-4 ile B-4 arasındaki kilitlenmeyi çözer. B-4'ün sayfadaki izi `FisAltinBacagi` / `altinAyarlari` / `AltinAyarlariYukle` geçen 7 satır ile HediyeAltinCakismasi'nin kaldırılmasıdır (grep ile sayılabilir). **S2-4'e kalan iş:** basit modda karma ödemenin görünür olması (`ZorunluGorunur`) ve tasarım tuvaline göre yerleşim. Bağlantı, kilit ve gövde hazır.

---

## 1. Özet

Müşterinin ödeme olarak verdiği eski altın artık satış fişinin **ALTIN bacağı** olarak girilebiliyor. Bu bacak her iki deftere birden yazılıyor:

- **SATIS** hareketi **brüt** tutarla yazılıyor (`TlTutari = +fiş toplamı`, `HasGrami = −satılan ürünün hası`). Altın mahsubu bu tutardan düşülmüyor.
- Aynı transaction içinde **ayrı bir HURDA_ALIS** hareketi yazılıyor: `TlTutari = −net`, `HasGrami = +eski altının hası`. Maden ayardan geliyor, `KurSnapshotId` fişinkiyle aynı. Ödeme izi `OdemeIzi.Yok`, adet null.
- **Gider pusulası** aynı commit'te doğuyor. Pusula brüt tutarı ve stopajı, hareket net tutarı taşıyor.
- Altın bacağı **kasaya yazılmıyor**, **cariye has yazmıyor** ve **nakit toplamına katılmıyor**.
- **Tevsik ve MASAK** kapıları fişin **brüt** toplamına bakıyor. "Yarısını altınla verelim" denerek had delinemiyor.
- Tek etiketli `OdemeTuru = "ALTIN"` artık **400** dönüyor ve hata metni kullanıcıyı altın bacağına yönlendiriyor (G-1). `Sabitler.OdemeTurleri` listesi "ALTIN" değerini koruyor (G-2).
- **G-3:** Canlı veritabanında `hareketler.odeme_turu='ALTIN'` satır sayısı **0** (ölçüldü, § 7).

**Migration:** gerekmedi (B4-24). `odeme_kalemleri.arac` kolonu `varchar(10)` ve "ALTIN" buna sığıyor. Yeni kolon ya da tablo açılmadı.

---

## 2. Spesifikasyon koda karşı doğrulandı: sapmalar ve gerekçeleri

| # | Şartname ne varsayıyordu | Kodda ne bulundu | Ne yapıldı / gerekçe |
|---|---|---|---|
| S-1 | T-24 "fiş stornolanınca iki hareket de ters işaretle döner" | 48-A storno motoru (`StornoServisi.StornolanabilirTurler`) **SATIS ve HURDA_ALIS türlerini bilerek stornolamıyor**. SATIS için düzeltme yolu satış iadesi. HURDA_ALIS `ErtelenenTurler` içinde (stok ve maliyet partisi geri alması gerekiyor, kısmi geri alma yapılmıyor). | Storno taklidi yasak (DoD EK 2), motor genişletilmedi. T-24 **reddin simetrisi** olarak yazıldı: iki hareketin hiçbiri tek başına geri alınamıyor, kasa ve hurda has bakiyesi değişmiyor, yarım geri alınmış bir fiş doğamıyor. Gerçek "fiş stornosu" ayrı bir karar ve paket konusu (§ 9). |
| S-2 | B4-3 / B4-31..35: `Islemler.razor` BacakAraclari listesine ALTIN eklenmesi ve ekranın doğrudan Islemler'e yazılması | ORKESTRATOR-DURUM (22:05) ve `betikler/plan-s2.js`: `Pages/Islemler.razor` ile `Bilesenler/Fis/*` **S-2 şeridinin dosya alanı**. S2-1 bu sırada Islemler'i bileşenlere bölüyordu (`Bilesenler/Fis/FisOdemeBacaklari.razor` 22:06'da doğdu). **S2-4 "altın bacağı ekranı" paketi B-4'ün geçmesini bekliyor.** | Islemler.razor'a **dokunulmadı**. Ekran tarafı, S-2 kurallarına uyan bağımsız bir bileşen olarak teslim edildi: `Bilesenler/AltinBacagiPaneli.razor` istek atmıyor, ApiIstemci enjekte etmiyor, durumu `Degisti` ile bildiriyor. Yanında `Servisler/AltinBacagiGirisi.cs` modeli var. B4-31..35'in hepsi bu bileşende ve bUnit testleriyle kilitli (T-26, T-27). **Bileşenin Islemler'e bağlanması, BacakAraclari'ye ALTIN eklenmesi ve tek ödeme listesindeki ALTIN seçeneğinin kaldırılması S2-4'e devredildi** (§ 9). |
| S-3 | B4-3: yeni sözlük anahtarı `Sozlukler.cs`'e eklenecekti | `Sozlukler.cs` ORTAK HUB ve düzenlenmesi yasak. `Dil.T.Islem.OdemeAltin` zaten var. | Yeni metinler ayrı dosyaya yazıldı: `Kuyumcu.Core/I18n/AltinBacagiSozlugu.cs` (TR + EN, konumsal record; eksik anahtar derleme hatası veriyor). Satıcı tipi, ad, TCKN, adres, ayar ve gram metinleri mevcut `Dil.T.Islem` / `Dil.T.Urun` anahtarlarından okunuyor (UX-S31: aynı kavram tek ad). |
| S-4 | G-5: `SatisRaporlari.cs` ödeme aracı kırılımına "ALTIN (mahsup)" kalemi | Dosya `Modules/Rapor` altında, yani **PARA şeridinin alanında** (dokunulmaz). | Yapılmadı. PARA şeridine devredildi (§ 9). Bağ kayıtta hazır: ALTIN ödeme kalemi `odeme_kalemleri(fis_id, hareket_id → HURDA_ALIS)` çiftini taşıyor. |
| S-5 | B4-20 "gider pusulası ZORUNLU" | Mevcut `HurdaAlis` kuralına göre **MÜKELLEF** satıcıda pusula düzenlenmiyor, belge satıcının faturası. | Aynı kural korundu. Mükellefte `TedarikciFaturaNo` zorunlu ve HURDA_ALIS satırında `fatura_no` olarak duruyor. Yanıttaki `belgeSayisi` bu durumda 1 dönüyor, ekran da "belge satıcının faturasıdır" diyor. "Alım var, belgesi yok" hâli yine doğamıyor. |
| S-6 | B4-23 kur `IptalEdildi=false` | `IslemDogrulayici.KurDogrula` iptal bayrağına bakmıyor. | Kural **yalnız altın bacağı olan fişte** uygulandı. Tüm satışlara yaymak mevcut davranışı değiştirirdi (mevcut hâl bozulmasın şartı). |
| S-7 | HURDA_ALIS satırının fiş bağı | `fis_id` kümesini fatura damgası (`FaturaModulu.FisHareketleriBul`) ve fiş toplamı sayacı okuyor. | HURDA_ALIS satırında `FisId` **bilerek boş**. Doldurulsaydı satış faturası eksi tutarlı bir alış satırı taşırdı. Bağ ALTIN ödeme kaleminde kuruluyor (`fis_id` = fiş, `hareket_id` = HURDA_ALIS). `odeme_turu` NULL, çünkü para el değiştirmedi ve kırılım kalemde duruyor. |
| S-8 | T-3 / M-4: `CariBakiyeGuncelle(..., hasGramDelta: 0m)` | Altın bacağı için ayrı bir cari yazımı yok (fiş kapanıyor). | M-4 veresiye bacağının cari çağrısına uygulandı. T-3 bu yüzden NAKIT + ALTIN + VERESIYE fişiyle yazıldı: has bakiyesi 0 kalıyor, TL bakiyesi yalnız veresiye kadar artıyor. |

**Mevzuat:** Bu pakette yeni bir mevzuat hükmü **yazılmadı**. Oranlar yapılandırmadan okunuyor (`Hurda:EsnafMuafStopajOran` 0.02, `Hurda:NihaiTuketiciStopajOran` 0). Yeni `Hurda:AlisIskontoOran` anahtarının varsayılanı **null**, yani ön-dolgu yok ve fiyat elle giriliyor. `Kdv:OzelMatrahKodu` ve `Masak` eşikleri değişmedi. `denetim.sh` ADR-0005 / B-3 `ESIK_SABIT_SUNUCUDA` sayacı 0.

---

## 3. Yapılanlar (şartname maddeleri)

| Madde | Uygulama | Kanıt |
|---|---|---|
| B4-1 | `OdemeBacagiDto.Arac` = `NAKIT\|KART\|HAVALE\|VERESIYE\|DOVIZ\|ALTIN` | T-19 saf (regex = `OdemeAraci.Tumu`) |
| B4-2 | `OdemeAraci.Altin` + `Tumu`; `KasayaGirer(ALTIN)=false` | T-19, P23 testi güncellendi |
| B4-3 | Sözlük: ayrı dosya (S-3). **Tur 2:** BacakAraclari'ye ALTIN eklendi, panel fişe bağlandı, tek ödemedeki ALTIN kaldırıldı (§ 0.1) | W-1..W-6, WM-1..WM-10, canlı 18/19 |
| B4-4 | `AyarId`, `GramAgirlik`, `BirimFiyatTlHasGram`, `Satici` yalnız ALTIN'da; başka bacakta gelirse 400 | T-18, canlı adım 17 |
| B4-5 | ALTIN bacağında KasaId gelirse 400 | T-17, canlı 10 |
| B4-6 | En fazla bir ALTIN bacağı | T-16, M-20, canlı 11 |
| B4-7 | `EnFazlaBacak = 5` değişmedi | T-19 saf |
| B4-8..10 | Tek kaynak: `Kuyumcu.Core/Domain/AltinBacagiHesap.Hesapla` (sunucu ve ekran aynı formülü kullanıyor) | T-13, M-13 |
| B4-11 | İstemci TutarTl ≠ sunucu neti ise 400 | T-8, canlı 12 |
| B4-12 | Pusulaya brüt tutar yazılıyor | T-5, M-10 |
| B4-13 | NakitToplam / VeresiyeToplam altını içermiyor | T-9, M-6, M-15 |
| B4-14 | Toleranssız denklik korundu | T-7, M-11, canlı 16 |
| B4-15 | Yazma yalnız `HareketFabrikasi.Ekle` üzerinden; `Hareketler.Add` API'de 1 yerde | denetim ✓ |
| B4-16 | SATIS brüt yazılıyor | T-2, M-1 |
| B4-17 | Ayrı HURDA_ALIS satırı: tür, net, has, maden, kur, cari, adet=null | T-1, T-12, T-14, M-2/3/12/14 |
| B4-18 | `OdemeIzi.Yok`; OdemeIzi sözleşmesine alan eklenmedi | M-15 |
| B4-19 | Cariye has yok, kasaya yazım yok | T-3, T-4, M-4, M-5, canlı 7 |
| B4-20 | Pusula aynı commit'te; satıcı kimliği eşiksiz | T-5, T-6, T-22, M-9, M-9b, M-19 |
| B4-21 | MASAK fiş brütüyle | T-11, M-8 |
| B4-22 | Tevsik fiş brütüyle; TevsikKapisi açık madde 3 kapandı (yorum güncellendi) | T-10, M-7, canlı 15 |
| B4-23 | İptal edilmiş kur, altın bacaklı fişte 400 | T-15 |
| B4-24 | Migration yok | § 1 |
| B4-25..30 | Yapılandırma varsayılanları korundu; `AlisIskontoOran=null` eklendi; yeni sabit yok | T-25, M-18, denetim |
| B4-31..35 | `AltinBacagiPaneli.razor` (sıra, salt okunur has/TL, satıcı bloğu, FARK kilidi, iki belge notu) | T-26, T-27 + 2 ek bUnit testi |
| G-1 | `SatisOlusturDto` regex'inden ALTIN çıkarıldı (yönlendiren mesajla) + servis içinde de elle kontrol | T-20 (dto + servis + http), M-16, canlı 9 |
| G-2 | `Sabitler.OdemeTurleri` "ALTIN"ı koruyor; geçmiş satır tür kapısından geçiyor | T-21, M-17 |
| G-3 | Canlıda `odeme_turu='ALTIN'` = **0** | § 7 SQL |

**Yeni uç:** `GET /api/v1/islemler/altin-bacagi/ayarlar`. Satıcı tipine göre stopaj oranlarını, `alisIskontoOran` (null) ve `saticiAdresiZorunlu` değerlerini döndürüyor; salt okunur. Yetki: controller `[ModulYetkisi("islem")]` + `[Authorize]`, satış fişiyle aynı. Mevcut hiçbir uç kaldırılmadı (envanter: API silinen 0).

---

## 4. DoD sözleşmesi (hangi kayıt nasıl düzenleniyor)

| Kayıt | Düzenleme yolu | Salt okunur mu, neden |
|---|---|---|
| SATIS hareketi (altın bacaklı fiş) | Satış iadesi (`POST /islemler/satis-iade`) | Finansal hareket: hard DELETE yasak. 48-A storno SATIS'ı desteklemiyor (bilinçli, S-1). |
| HURDA_ALIS hareketi (altın bacağı) | Yok. Storno `ErtelenenTurler` içinde (fail-closed) | Stok ve maliyet partisi geri alması gerekiyor; kısmi geri alma yapılmıyor. Doğru düzeltme yolu § 9'daki fiş stornosu paketi. |
| Gider pusulası | **Mevcut** `PATCH /gider-pusulalari/{id}` (finansal olmayan alanlar, denetim kaydıyla) + `POST /gider-pusulalari/{id}/iptal` | Brüt/net tutar hareketten türüyor ve değişmiyor. |
| Ödeme kalemi (ALTIN) | Yok (P-23 kuralı: kalem fişle birlikte doğuyor ve ölüyor) | Açıklayıcı kayıt, defter değil. |
| `Hurda:AlisIskontoOran` | Yapılandırma (appsettings / ortam) | Ticari karar kurucuda. |
| Ekran (panel) | Her girdi (ayar, gram, fiyat, satıcı tipi, ad, TCKN, adres, fatura no) kayıttan önce serbestçe değişiyor (B4-34). TL ve has için **giriş alanı yok**; bunlar türetiliyor (B4-31, `KaynakRozeti` "Hesaplandı"). | Paneldeki düğme sayısı 0 (DoD EK 1). |

Yeni varlık türü açılmadı. Denetim kaydı: pusula PATCH ve iptal yolları mevcut `DenetimServisi.YazmaKaydet` çağrılarını kullanıyor; hareket ve pusula yazımı satış fişinin mevcut akışına katılıyor.

---

## 5. Testler

**Paket öncesi tam süit (22:08):** 4451 test · 4444 başarılı · **7 başarısız**. Kırmızıların hepsi başka şeritlere ait:
`IkonTestleri.Layout_ve_Bilesenler_altinda_emoji_KALMADI`, `GiderEkranTestleri.Yetersiz_bakiye_bandi_iki_eylem_baglantisi_tasir`, `GiderEkranTestleri.Ham_invariant_tutar_bicimi_ekranda_gecmez`, `KilavuzUretimTestleri.Kilavuz_html_TEK_KAYNAKTAN_uretilmis_ve_GUNCEL`, `KilavuzUretimTestleri.Uretim_DAMGASI_kaynak_karmasini_tasir`, `YedekTatbikatTazelikHttpTestleri.Ozet_son_BASARILI_tatbikati_ve_tazeligini_SUNUCUDA_hesaplar`, `S1fGorunumKaynakTestleri.S1f_K2_CSS_blogu_TUVAL_olculerini_BELIRTECLE_tasir`.

**Paket sonrası tam süit (23:48, trx `scratchpad/trx-pB-4/_Mac_2026-09-23_23_48_13.trx`):** **4541 / 4541 başarılı, 0 başarısız.** Test sayısı düşmedi. Bu paketin kattığı test sayısı 38; aradaki fark diğer şeritlerin eklemeleri. **Bu paket yeni kırmızı üretmedi.**

**B-4 filtreli koşu:** `FullyQualifiedName~B4AltinBacagi` → 38/38. Mutasyonların hepsi geri alındıktan sonra da 38/38 yeşil kaldı. İlgili eski testler (`P23OdemeKalemi`, `OdemeSepetiTestleri`, `SatisFisiTestleri`): 79/79.

Yeni test dosyaları ve testler (38):

- `Kuyumcu.Tests/B4AltinBacagiTestleri.cs`
  - `B4AltinBacagiSafTestleri`, 12 test: T-8, T-9 (saf), T-13, T-16, T-17, T-18, T-19 (saf), T-20 (dto), T-21, T-25, T-6 (saf).
  - `B4AltinBacagiTestleri` (MySQL), 19 test: T-1, T-2, T-3, T-4, T-5, T-6, T-7, T-9, T-10, T-11, T-12, T-14, T-15, T-19, T-20, T-21 (geçmiş satır), T-22, T-23, T-24, T-25 (uç).
- `Kuyumcu.Tests/B4AltinBacagiHttpTestleri.cs`, 3 test: T-20 http, ayarlar ucu (200 + 401), uçtan uca fiş.
- `Kuyumcu.Tests/B4AltinBacagiEkranTestleri.cs` (bUnit), 4 test: **T-26** `AltinBacagi_Ekran_Fark_Sifir_Olmadan_Kaydetmez`, **T-27** `AltinBacagi_Ekran_Alanlar_Duzenlenebilir`, K-KUR-4 ön-dolgu testi, ayarlar yüklenemediğinde kilit testi.

**Değişen eski test:** `P23OdemeKalemiTestleri`. İki iddia şartname gereği güncellendi, hiçbir test silinmedi:
1. `VERESIYE_kasaya_GIRMEZ_digerleri_girer` artık ALTIN'ı da "kasaya girmez" olarak doğruluyor.
2. `Gecersiz_arac_listede_YOKTUR` içindeki `Gecerli("ALTIN") == false` iddiası, ALTIN artık geçerli bir araç olduğu için (B4-2) `Gecerli("DIGER")` / `Gecerli("KARMA") == false` ile değiştirildi.

---

## 6. Mutasyon kanıtları (21 mutasyon, hepsi kırmızı, hepsi `cmp` ile birebir geri alındı)

**Yöntem:** Mutasyondan önce kaynak `scratchpad/mut-yedek/B-4/` altına kopyalandı. Ardından `perl -0777 -pi` ile tek eşleşmeli değişiklik yapıldı (eşleşme sayısı önce 1 olarak doğrulandı). `dotnet build Kuyumcu.Api --no-incremental` sonrasında `dotnet test --filter FullyQualifiedName~B4AltinBacagi` koşuldu. Yedek geri kopyalandı ve `cmp` ile doğrulandı. Sürücü: `scratchpad/b4-mut.py`.

**Not:** İlk turda paralel şeritlerin derlemesi yüzünden **bayat DLL** kullanıldı: M2 koşusu M1'in ikili dosyasını çalıştırdı ve M1 ile aynı sonucu verdi. Bu tur iptal edildi. M5 uygulanmış hâlde kalan `SatisModulu.cs` yedekten geri kopyalanıp `cmp` ile doğrulandı. Tüm liste Api/Core **artımsız derlemeyle** baştan koşuldu. Aşağıdaki sonuçlar ikinci turdandır.

| Kod | Mutasyon | Kırmızı dönen testler | cmp |
|---|---|---|---|
| M-1 | SATIS TlTutari = tutar − altın net | T-1 `AltinBacagi_IkiDeftere_Birden_Yazar`, T-2 `AltinBacagi_Satis_BRUT_Yazilir` | birebir |
| M-2 | HURDA_ALIS HasGrami = 0 | T-1, T-23 `AltinBacagi_Hurda_Has_Bakiyesine_Duser`, T-12 `…Gumus_Altina_Karismaz` | birebir |
| M-3 | HURDA_ALIS TlTutari = 0 | T-1, T-5 `AltinBacagi_GiderPusulasi_Ayni_Committe_Dogar` | birebir |
| M-4 | CariBakiyeGuncelle has deltası = altın hası | T-3 `AltinBacagi_Cariye_Has_Yazmaz` | birebir |
| M-5 | Altın için KasaGuncelle(+net) eklendi | T-4 `AltinBacagi_Kasaya_Yazmaz` | birebir |
| M-6 | NakitToplam'a altın eklendi | T-9 (DB + saf), T-10, Http uçtan uca | birebir |
| M-7 | tevsik islemToplamTutari = NakitToplam | T-10 `AltinBacagi_Tevsik_Fis_Brutu_Uzerinden_Tetiklenir` | birebir |
| M-8 | MASAK tutarı = fiş − altın net | T-11 `AltinBacagi_Masak_Esigi_Bruten_Olculur` | birebir |
| M-9 | Pusula koşullu (brüt ≤ 100.000 ise yok) | T-5, T-22, Http uçtan uca | birebir |
| M-9b | Satıcı kimliği koşullu (küçük tutarda atla) | T-6 `AltinBacagi_Satici_Kimligi_Yoksa_400` (DB + saf) | birebir |
| M-10 | Pusulaya net yazıldı | T-5 | birebir |
| M-11 | Tolerans 0,01 | T-7 `AltinBacagi_Fis_Denkligi_Toleranssiz` | birebir |
| M-12 | Maden = Altın sabitlendi | T-12 `AltinBacagi_Maden_Ayardan_Gelir_Gumus_Altina_Karismaz` | birebir |
| M-13 | Has = gram × milyem / 1000 (elle, yuvarlamasız) | T-13 `AltinBacagi_Kiymetsiz_Ayarda_Has_Sifir` | birebir |
| M-14 | HURDA_ALIS'e en yeni kur kaydı verildi | T-14 `AltinBacagi_Tek_KurSnapshot` | birebir |
| M-15 | OdemeIzi.Yok → Nakitten(net) | T-9, T-10 | birebir |
| M-16 | SatisOlusturDto regex'ine ALTIN geri eklendi | T-20 `Tek_Etiketli_ALTIN_OdemeTuru_Artik_Reddedilir_dto` | birebir |
| M-17 | Sabitler.OdemeTurleri'nden ALTIN silindi | T-21 `Sabitler_OdemeTurleri_ALTIN_Icerir`, `…_Gecmis_Satir_Okunur` | birebir |
| M-18 | AlisIskontoOran varsayılanı 0.02 | T-25 `AltinBacagi_Yapilandirma_Varsayilani_Tutucu` (+ `_uc` + Http ayarlar) | birebir |
| M-19 | HURDA_ALIS SaveChanges + commit ile transaction dışına alındı | T-22 `AltinBacagi_Tum_Yazmalar_Tek_Transactionda` ve 10 test daha | birebir |
| M-20 | ALTIN bacak sayısı kapısı kaldırıldı (>99) | T-16 `AltinBacagi_Birden_Fazla_Bacak_Reddedilir` | birebir |

Son mutasyondan sonra temiz derleme: `dotnet build Kuyumcu.sln --nologo` → **0 Uyarı, 0 Hata**.

---

## 7. Canlı doğrulama (5193, `kuyumcu_net`)

Sürücü: `scratchpad/pB-4/canli.sh` + `canli.py`. Davet satırı doğrudan eklendi, parola rastgele üretildi. Hiçbir sır ekrana basılmadı. Görev metninin genel kısmı 5198 diyordu, izolasyon maddesi 5193 diyordu; pakete özgü **5193** kullanıldı. 5194–5200 portlarına dokunulmadı.

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | POST /auth/kayit | 201 | 201 | jeton var |
| 2 | GET /kur/guncel | 200 + id | 200 | kur var |
| 3 | GET /islemler/altin-bacagi/ayarlar | 200, iskonto null | 200 | alisIskontoOran=null; NIHAI 0 · ESNAF 0.02 · MUKELLEF 0 |
| 4 | POST /cari + /urunler (+ /kasalar) | 201 | 201/201 (+200) | — |
| 5 | Stok 10 | 200 | 200 | — |
| 6 | Fiş 20.000 = NAKIT 10.840 + ALTIN (K22 5 g × 2.000, bireysel) | 200 | 200 | has 4.58 · net 9160 · belge 2 · GP-2026-000001 · nakitToplam 10840 |
| 7 | Kasa bakiye farkı | +10.840 | +10.840 | altın payı kasaya girmedi |
| 8 | Esnaf muaf satıcı | 200 | 200 | brüt 9160 · stopaj 183,2 · net 8976,8 |
| 9 | POST /satis odemeTuru=ALTIN | 400 | 400 | "…'Eski altın' ödeme bacağı ekleyin…" |
| 10 | ALTIN + kasaId | 400 | 400 | "kasa verilemez" |
| 11 | İki ALTIN bacağı | 400 | 400 | "yalnız BİR eski altın" |
| 12 | İstemci TL 1 kuruş sapmış | 400 | 400 | "sunucu hesabıyla uyuşmuyor … net 9.160,00" |
| 13 | Satıcı kimliği yok | 400 | 400 | "VUK md.234" |
| 14 | Has fiyatı boş | 400 | 400 | "TÜRETMEZ" |
| 15 | Tevsik: 40.000 fiş, 5.000 nakit + 35.000 altın | 400 | 400 | "işlemin toplam tutarı 40.000,00 TL" |
| 16 | Fiş denkliği 1 kuruş | 400 | 400 | "fark 0,01 TL" |
| 17 | NAKIT bacağında gram | 400 | 400 | "yalnız ALTIN bacağına aittir" |

SQL kanıtı (test kiracısı):
```
-- hareketler (tur, tl, has, maden, odeme_turu, nakit, veresiye, kur_var, fis_id_bos, adet)
SAYIM_DUZELTME   0.00    91.600000 ALTIN NULL  0.00     0.00 1 1 NULL   (stok kurulumu)
HURDA_ALIS   -9160.00     4.580000 ALTIN NULL  0.00     0.00 1 1 NULL
SATIS        20000.00    -9.160000 ALTIN KARMA 10840.00 0.00 1 0 1.000
HURDA_ALIS   -8976.80     4.580000 ALTIN NULL  0.00     0.00 1 1 NULL
SATIS        20000.00    -9.160000 ALTIN KARMA 11023.20 0.00 1 0 1.000
-- kur_snapshot_id tekil sayısı: 1
-- gider_pusulalari
GP-2026-000001 NIHAI_TUKETICI K22 5.000 4.580000 9160.00 0.0000   0.00 9160.00 (hareket=HURDA_ALIS ✓)
GP-2026-000002 ESNAF_MUAF     K22 5.000 4.580000 9160.00 0.0200 183.20 8976.80 (hareket=HURDA_ALIS ✓)
-- odeme_kalemleri: NAKIT 10840 (kasa dolu) · ALTIN 9160 (kasa NULL, hareket=HURDA_ALIS) · NAKIT 11023.20 · ALTIN 8976.80
-- kasa_hareketleri: yalnız NAKIT 10840.00 ve NAKIT 11023.20
-- cari_bakiyeler: tl 0.00 · has 0.000000
-- G-3: SELECT COUNT(*) FROM hareketler WHERE odeme_turu='ALTIN'  → 0 (tüm kuyumcu_net)
```

**Temizlik:** API (pid 43545 / 43624) durduruldu; `lsof :5193` boş. `PAKETB4-TEST-CANLI` kiracısı `betikler/kiraci-temizle.sh --uygula` ile tenant_id taşıyan tüm tablolardan silindi (61 satır + tenants 1). Davet satırı da silindi. İkinci ölçüm "Kiracı YOK" döndü (bağlantı sınandı). `tenants`: önce **10**, sonra **10**. `QA-TEST-*` ve DEMO kiracılarına dokunulmadı.

---

## 8. Kapılar

- **Derleme:** `dotnet build Kuyumcu.sln --nologo` → 0 Uyarı, 0 Hata. (Ara bir derlemede görülen tek uyarı `S1fGorunumKartiTestleri.cs:460` xUnit2029 idi; S-1 şeridine ait. Son derlemede görünmedi.)
- **Denetim:** `bash ./denetim.sh` → **DENETİM TEMİZ** (EXIT 0). İlgili sayaçlar:
  - `Hareketler.Add` yalnız HareketFabrikasi'nda (1).
  - `P-08 HAREKETFABRIKASI_DISI_YAZMA` 0.
  - `P-23 ODEME_KALEMSIZ_YOL` (canlı) 0.
  - `P-23 FIS_TOPLAM_ODEME_KALEMI_FARKI` (canlı) 0.
  - `FIS_TOPLAM_CALISMA_ZAMANI_KAPISI` 1.
  - `IgnoreQueryFilters` 0.
  - `B-3 ESIK_SABIT_SUNUCUDA` 0.
  - `P-18 SABIT_RENK` 0.
  - Pages ModalPencere 55 (tavan 55, artış yok).
- **Uç sayısı** (`grep -rn "\[Http" Kuyumcu.Api`): önce 373, sonra 375. B-4 +1 (`altin-bacagi/ayarlar`); diğer +1 paralel şeritten. Envanter kıyası: **API silinen uç 0**.
- **DOM id:** Mevcut hiçbir razor dosyasına dokunulmadı. Yeni id'ler yalnız `altin-*` önekli. Envanter kıyasında "kaybolan" görünen tek id'ler `/giris` sayfasındaki `altinGrad`, `parilti` ve `tasGrad`; bunlar Giris.razor içinde, S-1 şeridinin dosyası, B-4 ile ilgisiz. `islemler` sayfasından eksilen id'lerin hepsi `Bilesenler/Fis/*` altında hâlâ duruyor (S2-1 taşıması).
- **Özellik kaybı:** `envanter-uret.sh --cikti scratchpad/b4-envanter.json`. B-4 kaynaklı silinen öğe 0; eklenen: 1 uç + `Bilesenler/AltinBacagiPaneli.razor` id'leri.

---

## 9. Açık kalanlar (sahibiyle) — tur 2 sonrası

1. ~~S2-4 bağlantısı~~ **KAPANDI (tur 2, § 0.1).** S2-4'e kalan: basit modda karma ödemenin görünmesi (`ZorunluGorunur`) ve tasarım tuvaline göre yerleşim.
2. **PARA şeridi (G-5):** `Modules/Rapor/SatisRaporlari.cs` ödeme aracı kırılımına "Eski altın (mahsup)" satırı eklenecek. Kaynak `odeme_kalemleri.arac='ALTIN'` (hareket_id → HURDA_ALIS, kasa_id NULL) (§ 0.2).
3. **Fiş stornosu (yeni paket, orkestratör):** Kapsam § 0.4'te. T-24'ün şartnamedeki hâli bu pakette yazılır.
4. **Mehmet (güvenlik):** Rol kapısı konmadı. Kararın gerekçesi § 0.3'te, izin anahtarı önerisi Mehmet'in onayında.
5. **MASAK kümülatifi:** Aynı cari için gün içi SATIS (brüt) + HURDA_ALIS (net) mutlak toplamı alınıyor. Bu tutucu bir hesap; mali müşavir teyidinde not edilmeli (değişmedi).
6. **Başka şeritlerin denetim bulgusu:** `Pages: @onclick'siz ve disabled'sız <button> 74 (tavan 71)`. Aday sahipler S3-A (`Cariler.razor`), SERİT A (`SiparisDetay.razor`) ve S3-B (`Raporlar.razor`) (§ 0.6). B-4'ün bu sayaca katkısı 0.
7. **Orkestratör / QA altyapısı:** `.mutorj`/`.bak` artıkları ve sıfır baytlık migration kopyaları beyaz listeyle birlikte temizlenmeli. `MigrationHazirlikTestleri`'nin sabit VT adı paralel koşumda risk taşıyor (§ 0.5).
8. **Paylaşılan `bin/` riski (orkestratöre):** Paralel şeritlerden biri mutasyon koşarken diğerinin `--no-build` testleri mutant DLL'i çalıştırıyor. Tur 2'de bu ölçüldü: S-2'nin `FisFark` mutantı yüzünden Karma testleri sahte kırmızı döndü. Öneri: her şerit ya ayrı bir kopyada derlesin ya da mutasyonlar şerit dışı bir kopyada koşsun.

## 10. Kurucuya ve mali müşavire notlar (K-10 ailesi, paketi durdurmuyor)

- **Asıl soru (§ 7.1):** Eski altınla kısmi ödeme (a) gider pusulalı ayrı bir hurda alımı ve **brüt** satış faturası mıdır, yoksa (b) mahsup edilip **net** fatura mıdır? Kod (a)'yı uyguluyor.
- VUK md.234'ün 7338 s. Kanun sonrası güncel metni: **DOĞRULANAMADI**. Pusula ve satıcı kimliği fail-closed zorunlu tutuldu.
- MASAK kimlik tespiti eşiği: yapılandırmadaki mevcut değer **değiştirilmedi**. Birincil rehberden gözle doğrulanması gerekiyor.
- Hurda alımında kur kaynağı (İKO mu, BİST KMP mi): mevzuatta belirlenmemiş. Sistem fiyat türetmiyor, dükkân elle giriyor (`Hurda:AlisIskontoOran` = null). Kurucu isterse oran yapılandırmadan açılabilir.
- 459 s. Tebliğ karşısında trampa: kapı yalnız **sıkılaştı** (fiş brütü). Teyit bekleniyor.
- Kesilen stopajın vergi dairesine borç olarak muhasebeleştirilmesi: üründe yok, B-4 ile de açık kaldı.
- TAKAS yolunda gider pusulası olmaması ile B-4'te zorunlu olması arasındaki asimetri kayda geçti; TAKAS değiştirilmedi.

---

## 11. Değişen / eklenen dosyalar (B-4)

```
M  dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.cs             (G-1 regex+servis kontrolü, SatisFisi altın akışı, yeni GET ucu, kur projeksiyonu)
M  dotnet/Kuyumcu.Api/Modules/Satis/OdemeSepeti.cs             (izlenmeyen dosya; ALTIN bacağı sözleşmesi + AltinBacaginiCoz)
M  dotnet/Kuyumcu.Api/Data/OdemeKalemi.cs                      (izlenmeyen dosya; OdemeAraci.Altin, KasayaGirer)
M  dotnet/Kuyumcu.Api/Modules/Hurda/GiderPusulasiKapisi.cs      (AlisIskontoOran = null)
M  dotnet/Kuyumcu.Api/Modules/Tevsik/TevsikKapisi.cs            (açık madde 3 yorumu: kapandı)
A  dotnet/Kuyumcu.Api/Modules/Satis/SatisModulu.AltinBacagi.cs
A  dotnet/Kuyumcu.Core/Domain/AltinBacagiHesap.cs
A  dotnet/Kuyumcu.Core/I18n/AltinBacagiSozlugu.cs
A  dotnet/Kuyumcu.Web/Bilesenler/AltinBacagiPaneli.razor
A  dotnet/Kuyumcu.Web/Servisler/AltinBacagiGirisi.cs
A  dotnet/Kuyumcu.Tests/B4AltinBacagiTestleri.cs
A  dotnet/Kuyumcu.Tests/B4AltinBacagiHttpTestleri.cs
A  dotnet/Kuyumcu.Tests/B4AltinBacagiEkranTestleri.cs
M  dotnet/Kuyumcu.Tests/P23OdemeKalemiTestleri.cs               (iki iddia B4-2'ye göre güncellendi)
A  dokumanlar/09-uygulama-kayitlari/PAKET-B-4-uygulama-raporu.md
-- tur 2 --
M  dotnet/Kuyumcu.Web/Pages/Islemler.razor                    (HediyeAltinCakismasi kalktı; ALTIN kilidi, gövde, pusula notu, ayar yükleme)
M  dotnet/Kuyumcu.Web/Bilesenler/Fis/FisOdemeBacaklari.razor   (izlenmeyen; ALTIN aracı + panel + kasa yok + tek ALTIN + kalanı ata)
M  dotnet/Kuyumcu.Web/Bilesenler/Fis/FisOdemeKasa.razor        (izlenmeyen; tek ödemeden ALTIN çıktı + ipucu)
M  dotnet/Kuyumcu.Web/Bilesenler/Fis/FisModelleri.cs           (izlenmeyen; FisBacagi.Altin / AltinDurum)
A  dotnet/Kuyumcu.Web/Bilesenler/Fis/FisAltinBacagi.cs
M  dotnet/Kuyumcu.Web/Servisler/AltinBacagiGirisi.cs           (AltinBacagiAyarKaynagi)
M  dotnet/Kuyumcu.Core/I18n/AltinBacagiSozlugu.cs              (PusulaDogdu, TekOdemedeYok — TR+EN)
A  dotnet/Kuyumcu.Tests/B4AltinBacagiFisEkranTestleri.cs
M  dotnet/Kuyumcu.Tests/PosBarkodTestleri.cs                   (1 test yeniden adlandırıldı, iddia G-1'e göre)
```
Tur 1'de dokunulmayan, tur 2'de gerekçeyle dokunulan dosyalar: `Islemler.razor`, `Bilesenler/Fis/*` (§ 0.1). Hiç dokunulmayanlar: `Sozlukler.cs`, `Modules/Rapor/*`, `Modules/Kasa/*`, mimari belge ve şartname, `SONRAKI-OTURUM-PROMPT.md`.

---

## 12. Koşul karşılama (tur 2 sonrası)

| Koşul | Durum | Kanıt |
|---|---|---|
| Ön koşul P-45 geçti | ✓ | ORKESTRATOR-DURUM "KAPIDAN GEÇENLER" |
| 35 madde (B4-3 dahil) | ✓ B4-3 tur 2'de ekrana bağlandı. G-5 PARA şeridinde (dosya yasağı). | § 0.1, § 3 |
| 27 test (T-1..T-27) + tur 2 bağlantı testleri | ✓ 38 + 10 test, hepsi yeşil. T-24 "reddin simetrisi" (§ 2 S-1, § 0.4). | § 5, § 0.1 |
| 20 mutasyon (+M-9b) + 11 bağlantı mutasyonu | ✓ 21/21 + 11/11 kırmızı, hepsi cmp birebir | § 6, § 0.1 |
| Build 0 hata / 0 uyarı | ✓ | § 0.6 |
| (7) Yeni kırmızı yok | ✓ tam süit 4612/4612 | § 0.6 |
| Test sayısı düşmedi | ✓ 4541 → 4612; B-4 +10, silinen 0 | § 0.6 |
| denetim.sh, B-4 sayaçları | ✓ Tek ✗ başka şeridin (dead-button 74/71); B-4 katkısı 0 | § 0.6 |
| Uç kaldırılmadı | ✓ 375 → 375 | § 0.6 |
| DOM id değişmedi | ✓ kaldırılan 0 | § 0.6 |
| Özellik kaybı 0 | ✓ eski altınla ödeme ekrana **geri geldi**; envanterde B-4 kaynaklı silinen yok | § 0.1, § 0.6 |
| Migration gerekmedi | ✓ | B4-24 |
| Canlı doğrulama + SQL | ✓ 19/19 | § 0.6 |
| Test kiracısı silindi | ✓ tenants 11 → 11 | § 0.6 |
| Mevzuat sabiti yok / tutucu varsayılan | ✓ Oranlar sunucudan; hata olursa kilit (fail-closed) | T-25, M-18, `AltinBacagiAyarKaynagi` |
| Doğrulayıcı bulguları | KRİTİK kapandı. ORTA×3: karar yazıldı / sahibine devredildi. DÜŞÜK: orkestratöre. | § 0.1–§ 0.5 |
