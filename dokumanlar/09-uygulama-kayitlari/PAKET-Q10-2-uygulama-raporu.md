# PAKET Q10-2 — /hurda: ramat çıkışı onayı + bakiye mesajı + alan hatasının kalkması — uygulama raporu

**Tarih:** 24.09.2026 (tur 1–2) · 25.09.2026 00:00–00:40 (tur 3) · **Rol:** Taşkın (backend) + Ertuğ (frontend) · **Dalga:** DEMO DÜZELTME Q10-A · **Durum:** BİTTİ (tur 3 = doğrulayıcı tur 2 bulgularının kapatılması; 7/7 paket kapısı, kanıt §T3)
**Kaynak:** QA turu 10 (`ham/qa-turu-10-bulgular.json`, rota `/hurda`), hasım doğrulayıcının tur 1 bulguları (2 KRİTİK, 2 ORTA, 2 DÜŞÜK) ve tur 2 bulguları (1 KRİTİK, 2 DÜŞÜK).
**Test kiracıları:** tur 1 `PAKETQ10-2-TEST Kuyumcu` (silindi) · tur 2 `PAKETQ10-2-TEST v2` (`01M3AEMTHM4Y47HP09ZTDZGR37`, silindi) · tur 3 `PAKETQ10-2-TEST v3` (`01M3AMN358TQPYVPVFXC05SA99`, silindi — §T3.7).

---

## T3. TUR 3 — doğrulayıcı tur 2 bulgularının kapatılması (25.09.2026)

### T3.1 Bulgular ve kapanışları

| # | Doğrulayıcı bulgusu | Kök neden (kodda doğrulandı) | Düzeltme | Kanıt |
|---|---|---|---|---|
| 1 | [KRİTİK] Onay penceresi açıkken canlı kur değişirse, kullanıcının ONAYLAMADIĞI fiyat/tutar/kurId ile geri alınamaz hurda satışı yazılıyor | `Kaydet()` yalnız özet dizgesini kuruyordu; `Gonder()` gövdeyi o anki alanlardan (`fiyat`, `tahsilEdilen ?? Tutar`, `kurId`) YENİDEN kuruyordu; `KurYansit()` pencere açıkken de `fiyat`/`kurId`'yi güncelliyordu | Gövde `Kaydet()` anında **değişmez bir kayda dondurulur**: `HurdaSatisIstegi(maden, has, fiyat, kabul, gerekçe, cari, kasa, ödeme, tahsil, kurSnapshotId)`. Özet bu kayıttan üretilir; `Gonder()` yalnız bu kaydı POST eder (alanlardan yeniden kurmaz). Vazgeç kaydı düşürür; yeniden "Kaydet" güncel değerlerle YENİ kayıt ve YENİ özet üretir. Tip, "razor'a yeni record yazılmaz" kuralı gereği `Kuyumcu.Contracts/Sayfalar/HurdaSozlesmeleri.cs`'e kondu (adı sunucu `HurdaSatisDto`'sundan bilerek farklı — P-03) | bUnit: doğrulayıcının `DQ102v2_onay_acikken_kur_degisirse_POST_onaylanan_fiyati_tasir` **YEŞİL**; yeni `Onay_acikken_kur_degisirse_POST_ONAYLANAN_fiyat_tahsil_ve_kurId_ile_gider`, `Vazgec_sonrasi_yeniden_Kaydet_GUNCEL_kurla_YENI_ozet_ve_govde_uretir` (gövde JSON ayrıştırılarak sınanır). Mutasyon M2-1…M2-4 (§T3.3). **Canlı** (§T3.5): pencere açıkken kur 6.749,47 → 7.000 oldu, arka form "Tutar: 700 ₺" gösterdi, özet 674,95 ₺'de kaldı; Onayla → gövde `birimFiyatTlHasGram 6749.47 · tahsilEdilenTl 674.95 · kurSnapshotId 01M3AMPE…` (7000'lik kurdan önceki kur); SQL `HURDA_SATIS -0.100000 674.95`, kur 6749.47 — onaydan önce o kiracıda 10 adet 7000'lik kur kaydı vardı. Görüntü `ekran/Q10-2/v3/K-kur-degisti-ozet-ayni-1280.png` |
| 2 | [DÜŞÜK] `DQ102_turkce_giris_1_5_kabul_1_500_belirsiz_red` kararsız (bUnit girdi yarışı) | Sayfa ilk yüklemeden sonra `KurYansit → InvokeAsync(StateHasChanged)` ile bir kez daha çiziliyor; `Find` ile `Input` arasına düşen çizim girdiyi kaybettiriyor (tur 2 §0.2 tanısı) | Doğrulayıcı dosyasına (`Kuyumcu.Tests/Dogrulayici/Q102SinirTestleri.cs`) uygulayıcının `Gir()` kalıbı eklendi (değer yerleşene dek yeniden gir) ve "1.500" sonrası mesaj `WaitForAssertion` ile beklenir. **Beklenti gevşetilmedi**: mesaj yine zorunlu, onay penceresi yine açılmamalı, "1,5" yine `hasGrami:1.5` taşımalı | 6 ardışık koşum (`Q102EkranSinirTestleri` + `Q102HurdaOnayEkranTestleri`, 32 test): **6/6 yeşil** (`scratchpad/kararlilik-1..6.txt`); tam süitte yeşil |
| 3 | [DÜŞÜK] Rapordaki sayılar yeniden üretilemiyor / dosya sayısı tutarsız | Tur 2 raporu §12 "olcumler/Q10-2-v2-*.txt (4)" diyordu; diskte 5 dosya var (`…-canli-api-duzeltme-oncesi.txt` dahil). Tam süit ve tenants sayıları anlık görüntüydü ama anı yazılmamıştı | §12 düzeltildi (v2: 5 dosya). Bu turdaki her sayının ölçüldüğü an yanına yazıldı (§T3.4, §T3.7). Tur 2 bölümlerindeki sayılar (5633, 14→11 tenants) **24.09 23:29 anlık görüntüsüdür**, bugünkü ağaçla karşılaştırılamaz | §12, §T3.4, §T3.7 |

### T3.2 Değişen kod (tur 3)
- `dotnet/Kuyumcu.Web/Pages/Hurda.razor`: `Kaydet()` gövdeyi `onayGovdesi` olarak dondurur ve özeti ondan üretir; `Gonder()` yalnız `onayGovdesi`'ni POST eder; `OnayKapat()` ve başarı yolu kaydı düşürür. DOM id'leri, rota, metinler değişmedi.
- `dotnet/Kuyumcu.Contracts/Sayfalar/HurdaSozlesmeleri.cs`: `HurdaSatisIstegi` record'u (+13 satır).
- `dotnet/Kuyumcu.Tests/Q102HurdaOnayEkranTestleri.cs`: +2 bUnit testi (yukarıda).
- `dotnet/Kuyumcu.Tests/Dogrulayici/Q102SinirTestleri.cs`: `Gir()` yardımcısı + `DQ102_turkce…` içinde `Gir`/`WaitForAssertion` (bulgu 2'nin istediği düzeltme; beklenti aynı).
- Sunucu tarafı (Modules/Hurda) bu turda DEĞİŞMEDİ. app.css, Q10-B dosyaları, `Sozlukler.cs` HUB'ı: dokunulmadı. Migration: gerekmedi.

### T3.3 Mutasyonlar (tur 3)
Yöntem: yedek `scratchpad/mut-yedek/Q10-2/<kod>.yedek` → `perl -0777 -pi` → `dotnet build Kuyumcu.Tests.csproj --no-incremental` → filtreli test → yedeği geri kopyala → `cmp`. Hepsi **"GERI ALINDI (cmp birebir)"**. Son mutasyondan sonra `dotnet build Kuyumcu.sln`: **0 uyarı 0 hata**.

| Kod | Değişiklik | Kırmızıya dönen test(ler) |
|---|---|---|
| M2-1 | `Gonder()` gövdesinde fiyat canlı alandan: `govde with { BirimFiyatTlHasGram = fiyat!.Value }` | Onay_acikken_kur_degisirse_POST_ONAYLANAN_fiyat_tahsil_ve_kurId_ile_gider · DQ102v2_onay_acikken_kur_degisirse_POST_onaylanan_fiyati_tasir |
| M2-2 | Kur kimliği canlıdan: `govde with { KurSnapshotId = kurId! }` | Onay_acikken_kur_degisirse_POST_ONAYLANAN_fiyat_tahsil_ve_kurId_ile_gider |
| M2-3 | Tahsil canlı tutardan: `govde with { TahsilEdilenTl = tahsilEdilen ?? Tutar }` | Onay_acikken_kur_degisirse_POST_ONAYLANAN… · DQ102v2_onay_acikken… |
| M2-4 | Bayat gövde: `onayGovdesi ??= new …` + `OnayKapat` kaydı düşürmez | Vazgec_sonrasi_yeniden_Kaydet_GUNCEL_kurla_YENI_ozet_ve_govde_uretir |
| M1-6 (tekrar) | Ekran sınırı `hg > sb.HasGram` → `>=` | Alti_haneli_bakiyeye_ESIT…, Bakiyeye_ESIT_cikis_gecerlidir…, DQ102_cok_buyuk_tutar_ozette_tr_bicimli, DQ102_bakiyeye_esit_4_haneli… |
| M1-7 (tekrar) | Sunucu kapısı `istenen > bakiye` → `istenen > bakiye + 0.000001m` | Alti_haneli_bakiye_arti_0_000001_GECMEZ_defter_eksiye_dusmez · DQ102v2S_bakiye_arti_0_0000005_yarim_yukari_reddedilir |

**Dürüst not:** M2-4'ün İLK koşumu (artımlı derleme ile) 32/32 yeşil verdi. Aynı mutasyonu elle uyguladım: `Vazgec_sonrasi…` 3/3 koşumda **kırmızı**. İlk koşumdaki derleme çıktısı 14 satırdı, yani Razor değişikliği derlemeye girmemişti (bayat dll). Bu yüzden betik `--no-incremental`'a çevrildi ve M2-1…M2-4 yeniden koşuldu. Tablodaki sonuçlar bu yeniden koşumdandır.

### T3.4 Testler ve denetim (tur 3)
- **Derleme** (`dotnet build Kuyumcu.sln --nologo`, 25.09 ~00:18): 0 uyarı, 0 hata. (Artımlı ara derlemelerde görülen tek uyarı `EtiketEkranTestleri.cs` xUnit1031, bu paketin değil.)
- **Filtreli** (`Q102|P20|Hurda`, 25.09 ~00:16): **147/147 yeşil** (tur 2'de 145 + bu turun 2 yeni testi).
- **Kararlılık:** Q102 bUnit sınıfları 6 ardışık koşum 32/32.
- **Tam süit** (`trx-pQ10-2/_Mac_2026-09-25_00_18_57.trx`, KUYUMCU_TEST_DB=kuyumcu_test_l): **5685/5685 geçti, 0 başarısız**. Başkasına ait kırmızı yok; karşılaştırma farkı = 0 (madde 7 sağlandı).
- **Denetim** (`dotnet/denetim.sh`, 25.09 00:36): 287 ✓, **1 ✗**: `P-37c: SAYFA_BASI_KAPSAM toplam=47 serit=44 muaf=3 acik=0` (muafiyet tavanı 2, 3 sayfada `SAYFA_BASI_MUAFIYETI` var: `Ayarlar.razor` (25.09 00:08 değişti), `Ekranlar.razor`, `Panel.razor`). Bu paket bu sayfalara dokunmadı → **başka şeridin** (Ayarlar/Panel sahibi; Panel Q10-B alanı). Bu paketin sayaçları ✓: record 3 (tavan 3; ilk denemede razor içindeki record 4'e çıkarıp ✗ vermişti → Contracts'a taşındı), Pages ModalPencere 48 (tavan 55), SABIT_RENK 0, HAM_INPUT 42 (tavan 42), MODAL_GOVDE_DISI_KAYDIRMA 0, SOZLUK_SAYI 0.

### T3.5 Canlı doğrulama (API 5174 · vekil 5173 · tarayıcı bölmesi kendi sekmem tab-52)
Kurulum: kiracı `PAKETQ10-2-TEST v3` (`/sistem/davetler` + `/auth/kayit`), "Ana Kasa" TRY + 100.000 TL, `POST /islemler/hurda-alis` K22 10,005 g → 200; bakiye 9,16458. Tur 2'de dolu olan tarayıcı bölmesinin sekme sınırı bu turda açıktı; **ölçümler ve senaryo tarayıcı bölmesinde** yapıldı. Bölme gizli olduğu için bölmeden dosyaya ekran görüntüsü alınamadı ("Browser pane is not displayed"). Dosya görüntüleri aynı vekil ve API üzerinden CDP ile gerçek Chrome'da (headless=new) çekildi.

| Adım | Beklenen | Gerçek |
|---|---|---|
| 0,1 g → "Ramat çıkışını kaydet" | onay açılır, POST 0 | özet "…Birim fiyat: 6.749,47 ₺ · Tutar: 674,95 ₺ · Tahsil edilen: 674,95 ₺ (Nakit) · Kasa: Ana Kasa", POST 0 |
| Pencere açıkken kur değişir (test kiracısına `POST /kur/manuel` 7000 → 201) | form yeni kuru gösterebilir, **özet ve gönderilecek gövde değişmez** | üst bar HAS 7.000 ₺, fiyat alanı 7000, "Tutar: 700 ₺"; özet aynen 6.749,47 / 674,95 (`ozet_ayni=true`) |
| Onayla | gövde onaylanan değerleri taşır | tek POST: `{"hasGrami":0.1,"birimFiyatTlHasGram":6749.4700,…,"tahsilEdilenTl":674.95,"kurSnapshotId":"01M3AMPECAXYWFCMKEMTPPBR5D"}`; bant "Hurda satışı kaydedildi: Altın 0,100 g · 674,95 ₺ · Kalan hurda: 9,06458 g" |
| Vazgeç | istek yok | pencere kapandı, POST artışı 0 (ayrıca matriste 8 Vazgeç + CDP'de 9 Vazgeç: hepsi 0) |
| 9,16458 (bakiye 9,06458 iken) | ret, gerçek sebep | "Kalan hurdayı aşıyorsunuz (9,06458 g mevcut)" |
| 9,06468 (bakiye + 0,0001) | ret | aynı mesaj |
| 9,06458'e düzelt (= bakiye) | mesaj kalkar | `#alan-hata-hrd-has` = null |
| 0 / −1 → Kaydet | alan mesajı, pencere yok | "Sıfırdan büyük bir değer girin.", pencere yok, POST 0 |
| = bakiye, Havale → Kaydet → Onayla | hata yok, POST 1 | bant "Altın 9,06458 g · 61.181,11 ₺ · Kalan hurda: 0,000 g"; kart "0,000 g · Çıkan has: 9,16458 g" |

**API sınırları** (`olcumler/Q10-2-v3-canli-api-sinir.txt`, yeni alışla bakiye 9,16458): 0 → 400 · −1 → 400 · 9,164581 → 400 ("elde 9,16458 … 9,164581 isteniyor") · 9,16468 (+0,0001) → 400 · 9,16458 (= bakiye) → 200, kalan 0,0.

**SQL** (`olcumler/Q10-2-v3-sql.txt`, `…-sql-kur-dondurma.txt`; kiracı silinmeden önce):
```
HURDA_ALIS   9.164580  -22453.22 NAKIT   kur 01M3AMN9…
HURDA_SATIS -0.100000    674.95  NAKIT   kur 01M3AMPE… (6749.47, 21:21:12)  ← pencere açıkken kur 7000 olmuştu; onaylanan yazıldı
HURDA_SATIS -9.064580  61181.11  HAVALE  ← tarayıcıda "= bakiye" çıkışı
HURDA_ALIS   9.164580  -22453.22 NAKIT
HURDA_SATIS -9.164580   9164.58  HAVALE  ← API "= bakiye" 200
HURDA_ALIS   9.164580  -22453.22 NAKIT   (ölçüm matrisi için)
sonraki_7000_kurlar (onay anından önce) = 10
```

**Ölçüm matrisi** (`sikisma-olc.js`, tarayıcı bölmesi; `olcumler/Q10-2-v3-sikisma.txt`):

| Durum | Genişlik | Mod | Tema | K1 | K2 % | K3 | K4 | K5 | K6 | K7 | K8 | K9 | K10 | K11 | yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| form | 1280 | PRO | açık / koyu | 0 | 98 | 0/0 | 2×1 | 0 | 0/1 | 0 | 0 | 0 | 0 | 0 | 0 |
| form | 1280 | BASİT | açık / koyu | 0 | 98 | 0/0 | 2×1 | 0 | 0/1 | 0 | 0 (**) | 0 | 0 | 0 | 0 |
| form | 375 | PRO | açık / koyu | 0 | 97 | 0/0 | 2,6×1 | 0 | 0/1 | 0 | 0 | 0 | 0 | 0 | 0 |
| form | 375 | BASİT | açık / koyu | 0 | 97 | 0/0 | 2,5×1 | 0 | 0/1 | 0 | 0 | 0 | 0 | 0 | 0 |
| onay | 1280 | PRO / BASİT | açık / koyu | 0 | 98 | 0/0 | 2×2 | 0 | – | 0 | 0 | 6 (*) | 0 | 0 | 0 |
| onay | 375 | PRO / BASİT | açık / koyu | 0 | 97 | 0/0 | 2,5–2,6×2 | 0 | – | 0 | 0 | 6 (*) | 0 | 0 | 0 |

- (*) K9 = 6 yalnız pencere açıkken çıkar; çiftlerin hepsi perdenin ALTINDAKİ form metni ile pencere metnidir. Pencere İÇİ ölçüm (375 PRO koyu): 7 metin, binen 0, taşan 0. Kutu 1280'de 384–896 px, 375'te 12–363 px.
- (**) 1280 BASİT koyu ilk ölçümde K8 = 4 ("mutabakat", "cari") çıktı: mod düğmesine basıldıktan 1,5 sn sonra ölçülmüştü; düğme seçiliydi ama içerik henüz PRO metnindeydi. 0,6 sn sonra aynı durumda yeniden ölçüldü: mod BASİT, K8 = 0. Aynı durumun açık tema ölçümü de 0.
- Görüntüler: `ekran/Q10-2/v3/F-{PRO,BASIT}-{1280,375}-{aydinlik,karanlik}.png` (8), `O-…` (8, onay açık), `K-kur-oncesi-1280.png`, `K-kur-degisti-ozet-ayni-1280.png` → toplam **18**.

### T3.6 Gözlem (bu paketin alanı dışında, düzeltilmedi)
Görüntülerde (`F-PRO-1280-aydinlik.png`) "Ramata çıkış" kartının gövdesinde iç boşluk yok: "Maden: …" satırı ve girdi kutuları kartın sol kenarına yapışık, "g"/"₺" birimi kartın sağ kenarında. Bu bir kart gövdesi dolgu kuralıdır (`wwwroot/css/app.css`). Bu şeridin app.css'e dokunması yasak; sahibi Q10-B / app.css. K1…K11 sayaçlarının hiçbiri bunu ölçmüyor. Kurucunun gözüne takılabilir.

### T3.7 Test kiracısı ve portlar
- **Kiracı `01M3AMN358TQPYVPVFXC05SA99`:** `tenant_id` kolonu olan 70 tablodan silindi, `kiraci_davetleri` (kullanan_tenant_id) ve `tenants` satırı da silindi. Denetim toplamı **kalan = 0**; `PAKETQ10-2%` = 0; davet (`q102v3-%`) = 0.
- **`tenants` sayısı:** 25.09 00:20'de başlangıç **13**, sonda (00:36) **13**. Liste bileşimi paralel şeritler yüzünden değişti: `PAKETT-M1-TEST` gitti, `PAKETT-S1-TEST-DOG` geldi (ikisi de başka şeritlerin). `QA-TEST-` kiracılarına dokunulmadı.
- **Portlar:** 5174 (API), 5173 (vekil), 9347 (CDP Chrome) kapatıldı; `lsof` üçü için boş. 5199/5177/5178/5179/5181/5200'e dokunulmadı. Tarayıcıda yalnız kendi sekmem (tab-52) açıldı ve kapatıldı; görüntü alanı `desktop`'a döndürüldü.
- **Sırlar:** sistem anahtarı API'ye ortam değişkeniyle dosyadan verildi, çıktıya yazılmadı. Fikstür, oturum ve anahtar dosyaları (chmod 600, scratchpad) iş sonunda `printf '' >` ile boşaltıldı.

---

## 0. TUR 2 — doğrulayıcı bulgularının kapatılması (24.09; bu bölümün sayıları 24.09 23:29 anlık görüntüsüdür)

| # | Doğrulayıcı bulgusu | Yeniden ürettim mi? | Kök neden | Düzeltme | Kanıt |
|---|---|---|---|---|---|
| 1 | [KRİTİK] Aşım mesajı ve bakiye 3 haneye YUKARI yuvarlanıyor ("9,165 g mevcut" deyip 9,165'i reddediyor) | Evet. Canlıda 10,005 g × K22 alışı yaptım, SQL has_grami = 9.164580 | `Bicim.Gram` = `ToString("F3")`. Kartta, ipucunda, aşım mesajında ve seçenek listesinde kullanılıyordu | `Hurda.razor` içinde `HassasGram(n)`: `"0.000###"` (tr-TR). En az 3, en çok 6 hane gösterir ve yuvarlama yapmaz; defterin kesinliği `decimal(15,6)`. Sayfadaki bütün gram gösterimleri bu tek biçime geçti (23 kullanım): kart, giren/çıkan, ipucu, mesaj, seçenekler, özet, başarı bandı, defter, çekmece | Canlı: kart "9,16458 g", ipucu "Kalan hurda: 9,16458 g", 9,165 yazınca "Kalan hurdayı aşıyorsunuz (9,16458 g mevcut)" (`ekran/Q10-2/v2/D0`, `D1`). `DQ102_asim…` YEŞİL |
| 2 | [KRİTİK] Onay özeti gönderilen gramı yanlış gösteriyor (POST 9,1604, özet "9,160 g") | Evet (aynı kök) | Özette `Bicim.Gram` kullanılıyordu | Özette de `HassasGram` kullanılıyor, yani gönderilen değer aynen görünür | Canlı özet: "Altın · Bizim kaydımız: 9,1604 g · Alıcının kabul ettiği: 9,1604 g · … Tutar: 61.523,72 ₺" (`v2/D2`). `DQ102_onay_ozeti…` YEŞİL |
| 3 | [ORTA] Tarayıcı bölmesi yerine headless Chrome; BASİT ölçümü 429 ile doğrulanamadı | 429'u ben de ürettim: ardışık hızlı koşumda sayfa "Sunucu şu anda çok yoğun (istek sınırı)" bandı gösterdi. Bant doğru; UX-S28 hata bandı var, liste boşaltılmıyor | `/auth/yenile` oran sınırı 10/dk (`OranSiniri.cs`). Her sayfa yüklemesi oturumu yeniliyor | Ölçüm betiğine her durum arasında 15 sn bekleme eklendi; mod değişimi ile koşum arasında 30–60 sn bekleniyor. **BASİT matrisi 429'suz tamamlandı** (§7) | `olcumler/Q10-2-v2-sikisma-BASIT.txt`. Tarayıcı bölmesi: `tabs_create` yine "tab cap reached" döndü (9 sekme, hepsi başkasının). Başka sekmeye dokunma yasağı yüzünden **CDP ile gerçek Google Chrome (headless=new)** kullanıldı. Sapma §3'te |
| 4 | [ORTA] Sınır testleri yalnız 3 haneli has kullanıyor | Evet | Fikstürde bakiye 9.16 sabitti | Fikstürün bakiyesi, defteri ve fire notu değiştirilebilir oldu. 6 haneli (9,16458) bakiyeyle **4 yeni bUnit + 3 yeni DB testi** eklendi (§4) | M1-1…M1-8 mutasyonları (§5) |
| 5 | [DÜŞÜK] Doğrulayıcı betiği jeton bastı | — (doğrulayıcı aracı) | — | Benim kurulum betiklerim (`q102v2/kur.py`, `kur2.py`, `giris.py`) yanıt gövdesini yalnız hata durumunda basıyor. Jetonları `chmod 600` dosyaya yazıyor, çıktıya hiçbir jeton/parola düşmüyor | Bu turun bütün komut çıktıları |
| 6 | [DÜŞÜK] Ağaçta başka şeritlerin `.mutorj`/`.bak` artıkları | — | Başka şeritler | Bu turda proje ağacına geçici dosya bırakmadım. Mutasyon yedekleri scratchpad'de (`mut-yedek/Q10-2/`). Silme işi kurucuda | — |

### 0.1 Tur 2'de ölçerken bulduğum YENİ kusurlar (düzeltildi)

**(A) SUNUCU KAPISI 1e-6 AŞIMA İZİN VERİYORDU → has defteri EKSİYE düşüyordu.** Bu fail-closed ihlalidir. Canlı 5174'te bakiye 9,16458 iken `POST /islemler/hurda-satis` has = 9,164581 → **200** döndü; SQL'de `HURDA_SATIS -9.164581` satırı oluştu ve `/hurda/bakiye` **−0,000001** gösterdi (`olcumler/Q10-2-v2-canli-api-duzeltme-oncesi.txt`).
- **Kök neden:** `HurdaStokKapisi.DogrulaCikis` içinde `cikisHas > bakiye + 0.000001m`. `decimal` kesin bir türdür, böyle bir paya gerek yoktur.
- **Düzeltme:** istenen değer deftere yazılacağı kesinlikte yuvarlanıyor (`Math.Round(…, 6, AwayFromZero)`, yani Yuvarla6 ile aynı) ve `istenen > bakiye` ile karşılaştırılıyor. Sınır dahildir. Sıfır kontrolü de yuvarlanmış değerle yapılıyor; böylece 0,0000004 deftere 0 olarak yazılamaz.
- **Canlı sonrası:** bakiye 9,164579 → 9,164580 **400** · 9,164679 **400** · 9,164579 **200** → bakiye 0,000000 (§7).

**(B) K8 — BASİT modda 5 jargon.** `sikisma-olc.js` K8 artık büyük/küçük harfe duyarsız ve BASİT modda şunları saydı: "MUTABAKAT" (sunucunun fire notu), "mutabakat" (tahlil farkı etiketi, defter sütunu, `DegismezNotu`), "cari" (`DuzeltmeNotu`).
- **Düzeltme:** BASİT karşılıklar paket sözlüğüne kondu (`Sozlukler.HurdaOnay.cs`: `TahlilFarkiBasit`, `TahlilFarkiNotuBasit`, `RamatciBasit`, `DegismezNotuBasit`, `DuzeltmeNotuBasit`, TR+EN). `Hurda.razor` `Dil.BasitMi` ile seçiyor; bu, `Banka.razor`/Q10-5'teki mevcut kalıptır.
- PRO metni hub `Sozlukler.cs`'te OLDUĞU GİBİ kalır.
- Sunucu fire notundan "(MUTABAKAT)" çıkarıldı ("…bu bir fire değil TAHLİL FARKIDIR…").
- **Sonuç:** BASİT'in 8 durumunda da K8 = 0 (§7).

### 0.2 Tur 2'de bulduğum test kararsızlığı (bUnit yarışı)

`DQ102_turkce_giris_1_5_kabul_1_500_belirsiz_red` (doğrulayıcının dosyası) ve kendi `Asim_mesaji_deger_bakiye_icine_duzeltilince_KALKAR` testim sıraya bağlı olarak kırmızı veriyordu.
- **Ölçüm:** DQ102_turkce tek başına 3/3 yeşil. `DQ102_sifir` ile aynı koşumda 5/5 kırmızı; sonraki koşumlarda 1/4 kırmızı.
- **Özgün razor (tur 1) ile de kırmızı**, yani bu turun değişikliği değil.
- **Tanı:** Testi geçici olarak işaretleyip çalıştırdım, sonra `cmp` ile geri aldım. Hata anında kutu `value=""` idi. Sayfa ilk yüklemeden sonra bir kez daha çiziliyor (`CanliKurServisi` → `KurYansit` → `InvokeAsync(StateHasChanged)`). `Find` ile `Input` arasına düşen bu çizim olay kimliğini yeniliyor ve girdi kayboluyor.
- **Kendi dosyamda** `Gir()` yardımcısı girdiyi, kutunun değeri tutana kadar yeniden veriyor. Sınanan kural değişmedi. Sonuç: 4 ardışık koşumda benim 19 bUnit testim 19/19 yeşil.
- **Doğrulayıcının dosyasına dokunmadım.** Öneri: `DQ102_turkce…` içinde `Input("1.500")` sonrası `WaitForAssertion` ya da aynı `Gir` kalıbı. Bu, testin beklentisini gevşetmez.
- Son tam süitte bu test **yeşil** geçti (5633/5633).

---

## 1. Bulgular kendi ölçümümle yeniden üretildi (tur 1, kod yazmadan önce)

| QA 10 bulgusu | Koddaki kök neden (ölçüldü) |
|---|---|
| KRİTİK: ramat çıkışı onaysız, tek tıkla kaydediliyor | `Kaydet()` doğrulamadan sonra doğrudan `POST /islemler/hurda-satis` atıyordu. Onay adımı yoktu. |
| YÜKSEK: bakiye yeterliyken "Bu madende hurda bakiyesi yok" | Aşımda yanlış sebep yazılıyordu (`KilitBakiyeYok`). Mesaj yalnız gönderimde yazılıyordu ve bir sonraki başarılı gönderime kadar kalıyordu. **Ölçülen bakiye:** oturum şubesinde seçili madenin has bakiyesi. Ayar kırılımı yok. Kaynak `HurdaStokKapisi.Bakiye` ile aynı (`/hurda/bakiye`). Tur 2'de sunucudaki 1e-6 payı kaldırıldı (§0.1-A). |
| ORTA: hata değer düzeltilince kalkmıyor | `Secici` (kasa, maden) ve `GerekceAlani` ortak girdi ailesinde değil. `CariSecildi` bütün mesajları siliyordu. |
| ORTA (ek): "Tahsilat kasası" zorunlu işareti yok | `Secici hrd-kasa` `Zorunlu` almıyordu. |

## 2. Yapılanlar

**Ekran (`Kuyumcu.Web/Pages/Hurda.razor`):**
- **Onay (tur 1):**
  - #hrd-kaydet yalnız doğrular ve **P-65 `OnayKipi`** (`Kimlik="hrd-onay"`) penceresini açar. POST yalnız pencerenin "Kaydet" düğmesiyle gider; Vazgeç istek atmaz.
  - Pencere şunları gösterir: özet (maden · bizim kaydımız · alıcı kabulü · birim fiyat · tutar · tahsil edilen (araç) · kasa), "değiştirilemez ve silinemez" uyarısı ve gerçek düzeltme yolu (düğme adı `Dil.T.Hurda.Duzelt` yer tutucusundan gelir).
- **Bakiye mesajı:**
  - Tur 1: `BakiyeSiniriOlc()` tek yerde, canlı ve gönderimde ölçer. Mesaj "Kalan hurdayı aşıyorsunuz ({0} mevcut)".
  - Tur 2: `{0}` artık yuvarlanmamış bakiyedir (`HassasGram`).
- **Alan hatası:** `form.Sil` (Q10-5 ortak API) kasa/maden/gerekçe değişiminde yalnız o alanı siler. `hrd-has` alanını ortak `FormAlani.DefterdenSil` temizler.
- **Kasa:** `hrd-kasa` `Zorunlu="true"`.
- **Tur 2:**
  - `HassasGram` (6 haneye kadar, yuvarlamasız): 23 kullanım.
  - BASİT modda jargonsuz metin seçimi: 5 yardımcı özellik.
- DOM id'leri değişmedi. Yeni id'ler `hrd-onay-*`.

**Sözlük:** `Kuyumcu.Core/I18n/Sozlukler.HurdaOnay.cs` (paket alan sözlüğü, TR+EN konumsal record; eksik anahtar derleme hatası verir). Tur 2'de 5 BASİT anahtarı eklendi. Hub `Sozlukler.cs`'e ve `Sozlukler.Katalog.cs`'e dokunulmadı.

**Backend (`Kuyumcu.Api/Modules/Hurda/HurdaStokKapisi.cs`, tur 2):**
- Çıkış kapısında 1e-6 payı kaldırıldı; istenen 6 haneye yuvarlanıp kesin karşılaştırılıyor. Sıfır kontrolü yuvarlanmış değerle yapılıyor.
- `FireKapaliMesaji` içinden "(MUTABAKAT)" çıkarıldı.
- Tek yazma yolu değişmedi (`HareketFabrikasi`).

**Migration:** gerekmedi (şema değişikliği yok).

## 3. Sapmalar ve gerekçeler
1. **Yazarak onay kullanılmadı** (`GeriDonulemez=false`). Günlük tezgâh işlemi için özet, geri alınamaz uyarısı ve gerçek yol yeterli; tek tıkla kayıt artık mümkün değil (bUnit + mutasyon + canlı ağ ölçümü).
2. **Tarayıcı bölmesi yerine CDP ile gerçek Chrome (headless=new).**
   - Bölmede 9 sekme açık ve hepsi başka şeritlerin; `tabs_create` → "tab cap reached". Başka sekmelere dokunmak yasak.
   - Ölçüm aynı `sikisma-olc.js` ile yapıldı ve betik sayfaya enjekte edildi. Oturum aynı inceleme vekiliyle açıldı (5174 → 5173, `/__giris`). Chrome `--lang=tr-TR` ile kendi profil dizininde (`scratchpad/q102v2/chrome-profil`) koştu.
   - Orkestratörün iki yolu var: sekme sınırını boşaltmak ya da CDP yolunu yazılı olarak kabul etmek.
3. **BASİT metinleri merkezi `BasitDilSozlugu` (Sozlukler.Katalog.cs) yerine paket sözlüğünde.**
   - Merkezi katmanın koruma testi (`KatalogK1Testleri.KAT_K8_02`) büyük/küçük harfe DUYARLI bir desen kullanıyor. PRO metinlerindeki küçük harfli "mutabakat" ve "cari" jargon sayılmıyor ve eşleme orada "gereksiz eşleme" diye reddediliyor. Oysa ölçüm aracı artık harfe duyarsız.
   - Katalog dosyası ve testi bu şeridin alanı dışında. Bu yüzden Q10-5/Banka'daki `Dil.BasitMi ? …Basit : …` kalıbı kullanıldı.
   - Sahibine not: §9.
4. **Önce görüntüsü:** tur 2'nin "önce" kanıtı doğrulayıcının görüntüleridir (`scratchpad/br/ekran/D1-9165-celiski.png`, `D2-onay-ozet-9160.png`). Eski kodu ortak ağaçta yeniden yayınlamak paralel şeritleri bozardı. "Sonra" görüntüleri `ekran/Q10-2/v2/D0…D3`.

## 4. Testler

| Kapsam | Toplam | Başarılı | Başarısız |
|---|---|---|---|
| Q102HurdaOnayEkranTestleri (bUnit) | 19 (tur 1: 12) | 19 | 0 |
| Q102HurdaSinirTestleri (gerçek MySQL `kuyumcu_test_l`) | 9 (tur 1: 6) | 9 | 0 |
| Doğrulayıcının Q102EkranSinirTestleri + Q102SunucuSinirTestleri | 15 | 15 | 0 (tam süitte; DQ102_turkce kararsız, §0.2) |
| P20EkranTestleri + P20bEkranTestleri | 22 | 22 | 0 |
| Filtreli (Q102 + Hurda + P20 + Katalog) | 181 | 180 | 1 = DQ102_turkce (kararsız, §0.2) |
| **Tam süit** (`trx-pQ10-2/_Mac_2026-09-24_23_23_14.trx`) | **5633** | **5633** | **0** |

**Tur 2'de eklenen testler (10):**

`Q102HurdaOnayEkranTestleri` (7):
- `Alti_haneli_bakiye_kart_ipucu_ve_asim_mesajinda_YUVARLANMADAN_yazilir`
- `Alti_haneli_bakiyeye_ESIT_cikis_gecer_ozet_ve_govde_ayni_degeri_tasir`
- `Alti_haneli_bakiye_arti_0_000001_reddedilir_POST_ATILMAZ`
- `Onay_ozeti_gonderilen_4_haneli_grami_AYNEN_gosterir_kabul_dahil`
- `Basit_modda_hurda_ekrani_jargon_TASIMAZ_defter_tahlil_farki_ve_fire_notu_dahil`
- `Pro_modda_hurda_ekrani_PRO_metnini_korur_ozellik_silinmez`
- `Sozluk_basit_metinler_TR_EN_dolu_ve_jargonsuz`

`Q102HurdaSinirTestleri` (3):
- `Alti_haneli_bakiye_arti_0_000001_GECMEZ_defter_eksiye_dusmez`
- `Alti_haneli_bakiyeye_ESIT_cikis_GECER_bakiye_tam_sifir`
- `Cikis_kapisi_alti_haneden_kucuk_pozitifi_sifir_sayar_reddeder`

Test fikstüründe bakiye, defter ve fire notu artık değiştirilebilir. `Gir()` yardımcısı eklendi (§0.2).

## 5. Mutasyon kanıtları (tur 2)

**Yöntem:**
1. Yedek `scratchpad/mut-yedek/Q10-2/` altına alındı.
2. `perl -0777 -pi` ile tek değişiklik yapıldı.
3. Derlendi. Derleme başka şeridin dosya kilidine takılırsa 4 kez yeniden denendi; yeşil derleme olmadan sonuç sayılmadı.
4. Filtreli test koşuldu.
5. Yedek geri kopyalandı ve `cmp` ile birebir aynı olduğu doğrulandı.

Hepsi **"GERI ALINDI (cmp birebir)"** çıktı. Son mutasyondan sonra `dotnet build Kuyumcu.sln`: **0 hata**. İki uyarı var, ikisi de bu paketin değil: `EtiketEkranTestleri.cs` xUnit1031 ve çalışan API dll'i yüzünden MSB3026 kopya yeniden denemesi.

| Kod | Değişiklik | Kırmızıya dönen test(ler) |
|---|---|---|
| M1-1 | `HassasGram` biçimi `"F3"` (eski yuvarlama) | Alti_haneli_bakiye_kart_ipucu…, Alti_haneli_bakiyeye_ESIT…, Onay_ozeti_gonderilen_4_haneli…, DQ102_asim…, DQ102_onay_ozeti… |
| M1-2 | Yalnız özetteki "bizim kaydımız" → `Bicim.Gram` | Onay_ozeti_gonderilen_4_haneli_grami_AYNEN_gosterir_kabul_dahil |
| M1-3 | Yalnız aşım mesajı → `Bicim.Gram` | Alti_haneli_bakiye_kart_ipucu_ve_asim_mesajinda…, DQ102_asim_mesajindaki… |
| M1-4 | Yalnız sınır ipucu → `Bicim.Gram` | Alti_haneli_bakiye_kart_ipucu_ve_asim_mesajinda… |
| M1-5 | Yalnız bakiye kartı → `Bicim.Gram` | Alti_haneli_bakiye_kart_ipucu_ve_asim_mesajinda… |
| M1-6 | Ekran sınırı `>` → `>=` | Alti_haneli_bakiyeye_ESIT…, Bakiyeye_ESIT_cikis_gecerlidir…, DQ102_bakiyeye_esit_4_haneli…, (+ DQ102_cok_buyuk…) |
| M1-7 | Sunucu kapısına eski pay (`bakiye + 0.000001m`) | Alti_haneli_bakiye_arti_0_000001_GECMEZ_defter_eksiye_dusmez |
| M1-8 | Sıfır kontrolü yuvarlanmamış değerle (`cikisHas <= 0m`) | Cikis_kapisi_alti_haneden_kucuk_pozitifi_sifir_sayar_reddeder |
| M1-9 | BASİT'te de PRO tahlil farkı etiketi | Basit_modda_hurda_ekrani_jargon_TASIMAZ… |
| M1-10 | BASİT'te de PRO `DegismezNotu` | Basit_modda_hurda_ekrani_jargon_TASIMAZ… |
| M1-11 | Sunucu fire notuna "(MUTABAKAT)" geri | Sozluk_basit_metinler_TR_EN_dolu_ve_jargonsuz, Basit_modda_hurda_ekrani_jargon_TASIMAZ… |

**Notlar:**
- M1-6, M1-9, M1-10 ve M1-11 koşumlarında `DQ102_turkce…` da kırmızı listede görünüyor. Bu, mutasyondan bağımsız kararsızlıktır (§0.2).
- M1-1 ve M1-2'de görülen `Asim_mesaji…KALKAR` kırmızısı `Gir()` öncesiydi, yani aynı yarıştı; `Gir()` sonrası 4 koşumda yeşil.
- M1-4 ve M1-5'in ilk koşumu başka şeridin derleme kilidi (`blazor.boot.json`, "being used by another process") yüzünden bayat dll ile çalıştı. Sonuç sayılmadı; betik yeniden denemeli derlemeye çevrildi ve iki mutasyon yeniden koşuldu. Tablodaki sonuçlar yeşil derlemeli koşumdandır.
- Tur 1 mutasyonları (M-Q102-1…11) tur 1 raporunda. M-Q102-8 ve M-Q102-10 sunucunun eski pay davranışını kilitliyordu; tur 2'de pay bilinçli olarak kaldırıldı (§0.1-A) ve yeni davranış M1-7 ile kilitlendi.

## 6. Denetim
`dotnet/denetim.sh` → **DENETİM TEMİZ** (288 ✓, 0 ✗, çıkış 0).

| Sayaç | Değer |
|---|---|
| Pages ModalPencere | 48 (tavan 55) — artış yok |
| SABIT_RENK | 0 |
| HAM_INPUT | 42 (tavan 42) |
| MODAL_GOVDE_DISI_KAYDIRMA | 0 |
| SOZLUK_SAYI | 0 |

## 7. Canlı doğrulama (API 5174 · vekil 5173 · gerçek Chrome/CDP)

**Kurulum:**
- Kiracı `PAKETQ10-2-TEST v2`, "Ana Kasa" (TRY) açıldı. Kasaya 100.000 TL giriş yapıldı.
- `POST /islemler/hurda-alis` (K22 · 10,005 g) → 200.
- SQL: `HURDA_ALIS ALTIN 9.164580`; kolon tipi `decimal(15,6)`.

**Davranış senaryosu (1280 · PRO · açık):**

| Adım | Beklenen | Gerçek |
|---|---|---|
| Bakiye kartı / ipucu | tam değer | "Altın 9,16458 g · Giren has: 9,16458 g" / "Kalan hurda: 9,16458 g" (`v2/D0`) |
| 9,165 yaz (aşım 0,00042) | gerçek sebep, çelişkisiz bakiye | "Kalan hurdayı aşıyorsunuz (9,16458 g mevcut)" (`v2/D1`) |
| 9,164581 yaz (bakiye+0,000001) | ret | "Kalan hurdayı aşıyorsunuz (9,16458 g mevcut)" |
| 9,1604'e düzelt | mesaj kalkar | `#alan-hata-hrd-has` = null |
| Kaydet | onay, POST 0 | onay=true, POST=0. Özet: "Bizim kaydımız: 9,1604 g · Alıcının kabul ettiği: 9,1604 g · … Tutar: 61.523,72 ₺ · … Kasa: Ana Kasa" (`v2/D2`) |
| Vazgeç | istek yok | onay=false, POST=0 |
| 9,16458 (= bakiye), Havale → Kaydet → Kaydet | hata yok, POST 1, gövde aynen | hata=null; POST=1; gövde `hasGrami=9.16458`; bant "Hurda satışı kaydedildi: Altın 9,16458 g · 61.554,54 ₺ · Kalan hurda: 0,000 g"; kart "0,000 g · Çıkan has: 9,16458 g" (`v2/D3`) |

**Aynı akışta Nakit ile deneme:** 61.551,98 TL nakit tahsil edilmek istendi. Sunucu mevcut tevsik kapısıyla reddetti (400; "30.000,00 TL haddini aşıyor … VUK mükerrer md.257"). Ret pencerede ve bantta göründü, satır yazılmadı. Bu mevcut ve doğru bir mevzuat kapısıdır; bu paket dokunmadı. Eşik sunucu yapılandırmasından gelir.

**API sınırları** (`olcumler/Q10-2-v2-canli-api.txt`). Düzeltmeden sonra; ilk kapı düzeltmesinden önceki kusur §0.1-A'da.

| has | HTTP | Yanıt |
|---|---|---|
| 0 | 400 | "Has gram 1E-06 veya daha büyük olmalıdır" |
| −1 | 400 | aynı |
| 9,164580 (bakiye 9,164579 + 0,000001) | 400 | "Bu şubede yeterli hurda yok: elde 9,164579 … 9,16458 … isteniyor" |
| 9,164679 (bakiye + 0,0001) | 400 | aynı biçimde ret |
| 9,164579 (= bakiye) | 200 | hasGrami 9.164579, kalanHurdaHasGram 0.0 |

**SQL kanıtı** (`olcumler/Q10-2-v2-sql.txt`, kiracı silinmeden önce):
```
HURDA_ALIS   ALTIN  9.164580  -22453.22  NAKIT
HURDA_SATIS  ALTIN -9.164580   61554.54  HAVALE   ← tarayıcıdaki onaylı "= bakiye" çıkışı
HURDA_ALIS   ALTIN  9.164580  -22453.22  NAKIT
HURDA_SATIS  ALTIN -9.164581    9164.58  HAVALE   ← DÜZELTME ÖNCESİ kusur: 1e-6 aşım 200 aldı (§0.1-A)
HURDA_ALIS   ALTIN  9.164580  -22453.22  NAKIT
HURDA_SATIS  ALTIN -9.164579    9164.58  HAVALE   ← düzeltme sonrası "= bakiye"
bakiye (ALTIN) = 0.000000
```

**Ağ ölçümü (CDP `Network.requestWillBeSent`):**
- Senaryoda ve iki ölçüm matrisinde toplam 17 kez "Kaydet → Vazgeç" yapıldı. Vazgeç yolunda `POST /islemler/hurda-satis` = **0**.
- Onaylanan 1 satış → POST 1.

**Ölçüm matrisi** (`sikisma-olc.js`; `olcumler/Q10-2-v2-sikisma-PRO.txt`, `…-BASIT.txt`). Görüntüler: `ekran/Q10-2/v2/F-*` (form), `O-*` (onay açık).

| Durum | Genişlik | Mod | Tema | K1 | K2 % | K3 taşan/kırık | K5 TRY | K6 | K7 | K8 | K9 binen | K10 | K11 | yatay |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| form | 1280 | PRO | açık / koyu | 0 | 98 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| form | 375 | PRO | açık / koyu | 0 | 97 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| form | 1280 | BASİT | açık / koyu | 0 | 98 | 0/0 | 0 | 0 | 0 | **0** (önce 5) | 0 | 0 | 0 | 0 |
| form | 375 | BASİT | açık / koyu | 0 | 97 | 0/0 | 0 | 0 | 0 | **0** (önce 5) | 0 | 0 | 0 | 0 |
| onay | 1280 | PRO / BASİT | açık / koyu | 0 | 98 | 0/0 | 0 | – | 0 | 0 | 6 (*) | 0 | 0 | 0 |
| onay | 375 | PRO / BASİT | açık / koyu | 0 | 97 | 0/0 | 0 | – | 0 | 0 | 6 (*) | 0 | 0 | 0 |

- **K4:** form 2,0–2,7 ekran, 1 bölüm; onay 2 bölüm. Eşik (≥4 bölüm) aşılmadı.
- **Onay kutusu:** 1280'de 384–896 px, 375'te 12–363 px. Kutu içinde taşan 0, üst üste binen 0.
- **(*) K9 = 6** yalnız pencere açıkken çıkıyor. Sayılan çiftlerin hepsi perdenin ALTINDA kalan form yazısı ile pencere metnidir (ör. `#hrd-has-sinir ⟂ #modal-… "Ramat çıkışı onayı"`). Pencere kapalıyken 16 durumun hepsinde K9 = 0.
- **429:** ilk BASİT ve PRO matrislerinden birer durum oran sınırına takıldı; sayfa "Sunucu şu anda çok yoğun" bandını gösterdi (görüntü sonraki koşumda üzerine yazıldı). Beklemeli betikle yeniden koşuldu. Tablodaki bütün satırlar 429'suz koşumdandır.

## 8. DoD sözleşmesi

| Varlık | Düzenleme yolu | Salt okunur gerekçesi |
|---|---|---|
| Hurda satışı (HURDA_SATIS, finansal) | Has farkı için "Tahlil farkını düzelt" çekmecesi (ek fark bacağı); para tarafı için müşteri düzeltme fişi | Yerinde değişmez ve bu sürümde stornolanmaz; hard DELETE yasak. Bunu `DegismezIpucu` (BASİT modda jargonsuz metinle) ve onay penceresi (geri alınamaz uyarısı + gerçek yol) söyler. |

- Bu paket **yeni varlık açmadı.** `/islemler/hurda-satis` tek yazma yolu. HareketFabrikasi atlanmadı. `[RolGerekli]`/`[ModulYetkisi]` ve denetim kaydı mevcut uçlarda değişmedi.
- **Dokunulan sayfalar:** `Pages/Hurda.razor` (tek; tur 1 ve tur 2 aynı paketin iki turu).

## 9. Açık kalanlar
- **Tam süit 0 kırmızı**, dolayısıyla karşılaştırılacak başkasına ait kırmızı yok.
- **Doğrulayıcı testi `DQ102_turkce_giris_1_5_kabul_1_500_belirsiz_red` sıraya bağlı kararsız** (§0.2). Kök neden bUnit girdi yarışı; ürün hatası değil. Özgün razor ile de kırmızı. Sahibi: hasım doğrulayıcı. Önerilen düzeltme: `Input` sonrası `WaitForAssertion` ya da `Gir` kalıbı.
- **`KatalogK1Testleri.KAT_K8_02`** jargon deseni büyük/küçük harfe duyarlı, `sikisma-olc.js` K8 ise duyarsız. Merkezi BASİT katmanına küçük harfli "mutabakat/cari" eşlemesi eklenemiyor. Sahibi: katalog (K1) şeridi. Öneri: desene `RegexOptions.IgnoreCase` eklemek; o zaman Hurda BASİT metinleri merkezi katmana taşınabilir.
- **`Gram.razor` önizlemesi** (`Bilesenler/Form/Gram.razor`, Q10-B alanı) `Bicim.Gram` (3 hane) kullanıyor. Önizleme yalnız kutudaki sayı farklı bir değer söylediğinde çiziliyor (K7), bu yüzden /hurda'da görünmedi. Ama 4+ haneli bir değer yuvarlanmış gösterilebilir. Sahibi: Q10-B / ortak Form.
- **DTO iletisi "Has gram 1E-06 veya daha büyük olmalıdır"** ham bilimsel gösterim içeriyor (`Modules/Satis/SatisModulu.cs`, bu şeridin alanı dışında). Ekran bu isteği zaten göndermiyor.
- **Ağaçta başka şeritlerin `.mutorj`/`.bak` artıkları** (doğrulayıcı listesi) duruyor. Silme kurucuda.
- **Tarayıcı bölmesi sekme sınırı** dolu. Orkestratör ya boşaltmalı ya da CDP yolunu kabul etmeli (§3.2).

## 10. Mevzuat
Bu pakette mevzuat hükmü yok: eşik, oran ya da kod eklenmedi. Canlıda görülen tevsik (30.000 TL) reddi mevcut kapıdır ve bu paket dokunmadı. Mali müşavir notu gerekmedi.

## 11. Test kiracısı temizliği
- **Kiracı `01M3AEMTHM4Y47HP09ZTDZGR37`:** `tenant_id` kolonu olan 70 tablodan silindi. Denetim sorgusunda kalan satır **0**; `tenants` satırı ve `kiraci_davetleri` satırı da silindi (`PAKETQ10-2%` = 0).
- **`tenants` sayısı:** tur başında 14, benim kiracımla 15, sonda 11. Benim kiracım +1/−1 oldu. Aradaki düşüş başka şeritlerin kendi kiracılarını kapatmasıdır. Kalan liste: 5 demo/kurucu, 4 `QA-TEST-`, `DEMO-Vitrin`, `PAKETQ10-4-TEST`. `QA-TEST-` kiracılarına dokunulmadı.
- **Portlar:** 5174 (API), 5173 (vekil) ve 9347 (headless Chrome CDP) kapatıldı; `lsof` boş. 5199/5177/5178/5179/5181/5200 portlarına dokunulmadı.

## 12. Değişen dosyalar (`git status --porcelain | grep -v -E "/(bin|obj)/"`, bu paketinkiler)

**Tur 2'de değişen:**
- `dotnet/Kuyumcu.Web/Pages/Hurda.razor`: `HassasGram`, BASİT metin seçimi.
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.HurdaOnay.cs`: 5 BASİT anahtarı (TR+EN).
- `dotnet/Kuyumcu.Api/Modules/Hurda/HurdaStokKapisi.cs`: çıkış kapısında pay kaldırıldı, fire notunda jargon kaldırıldı.
- `dotnet/Kuyumcu.Tests/Q102HurdaOnayEkranTestleri.cs`: +7 test, değişken fikstür, `Gir()`.
- `dotnet/Kuyumcu.Tests/Q102HurdaSinirTestleri.cs`: +3 test.

**Tur 1'den (değişmedi):** `P20EkranTestleri.cs`, `P20bEkranTestleri.cs`.

**Kanıtlar:** `dokumanlar/09-uygulama-kayitlari/ekran/Q10-2/v2/*.png` (20), `olcumler/Q10-2-v2-*.txt` (5: canli-api, canli-api-duzeltme-oncesi, sikisma-BASIT, sikisma-PRO, sql — tur 3 düzeltmesi; tur 2 metni "4" diyordu).

`git status` notu: `Modules/Hurda/`, `Hurda.razor` ve `Sozlukler.HurdaOnay.cs` depoda izlenmeyen (`??`) dosyalardır. Depoda tek ilk commit var; commit yapılmadı.

## 13. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Ramat çıkışında onay zorunlu; özet (maden/gram/has/tutar/kasa); geri alınamaz uyarısı + gerçek yol | ✓ | `v2/D2`, `O-*` (16), bUnit, tur 1 M-Q102-1 |
| Vazgeç'te istek yok (ağ ölçümü) | ✓ | CDP POST=0 (17 Vazgeç) |
| Bakiye yeterliyken hata yok (SQL kanıtı) | ✓ | SQL 9.164580 → tarayıcıda = bakiye çıkışı 200, kalan 0; M1-6 |
| Mesajdaki/özetteki gram gerçek değer (doğrulayıcı KRİTİK 1-2) | ✓ | `v2/D1`, `v2/D2`; DQ102_asim/DQ102_onay yeşil; M1-1…M1-5 |
| Alan düzeltilince hata kalkar | ✓ | canlı 9,164581 → 9,1604 mesaj null; bUnit has/kasa/gerekçe |
| Finansal sınırlar (0, eksi, = bakiye, +0,0001, +0,000001) | ✓ | ekran + DB + canlı API; M1-6…M1-8 |
| Sunucu fail-closed: has defteri eksiye düşmez | ✓ (tur 2'de düzeltildi) | §0.1-A; M1-7 |
| Gerçek tarayıcı 1280+375 × BASİT+PRO × açık+koyu; K1…K11 | ✓ (CDP yolu, §3.2) | §7 tablosu; K8 5 → 0 |
| Ortak Form çözümü kopyalanmadı | ✓ | `form.Sil` + ortak `FormAlani`; `Gir()` yalnız testte |
| app.css / Q10-B dosyaları / hub sözlüğe dokunulmadı | ✓ | §12 |
| Tam süit + denetim | ✓ (paket kapısı) | tur 2: 5633/5633, DENETİM TEMİZ (24.09 23:29) · tur 3: 5685/5685 (25.09 00:18); denetim 287 ✓ + 1 ✗ başka şeridin (P-37c, §T3.4) |
| (tur 3) Onaylanan özet = gönderilen gövde; pencere açıkken kur değişse de onaylanmayan fiyat/tutar/kurId gitmez | ✓ | §T3.1-1; canlı gövde + SQL; `K-kur-degisti-ozet-ayni-1280.png`; M2-1…M2-4; DQ102v2 yeşil |
| (tur 3) Doğrulayıcı testi kararlı (beklenti gevşetilmeden) | ✓ | 6/6 ardışık koşum yeşil; tam süit yeşil |
| (tur 3) Rapor sayıları anıyla, dosya sayısı doğru | ✓ | §T3.4, §T3.7, §12 |

### Tur 3 açık kalanlar
- `dotnet/denetim.sh` → `P-37c: SAYFA_BASI_KAPSAM` ✗ (muafiyet 3 > tavan 2: Ayarlar, Ekranlar, Panel). Sahibi: Ayarlar/Panel şeridi (Panel = Q10-B). Bu paket ilgili sayfalara dokunmadı.
- "Ramata çıkış" kart gövdesinde iç dolgu yok (§T3.6). Sahibi: app.css / Q10-B.
- Tur 2 açık kalanları (§9) geçerli: KAT_K8_02 harf duyarlılığı (katalog şeridi), `Gram.razor` önizlemesinin 3 hane yuvarlaması (Q10-B), DTO iletisindeki "1E-06" (Modules/Satis), ağaçtaki başka şeritlerin artıkları (kurucu). Tur 2'deki "`DQ102_turkce…` kararsız" maddesi tur 3'te KAPANDI; "tarayıcı bölmesi sekme sınırı" maddesi bu turda yaşanmadı.
