# PAKET QA10-KRITIK: QA turu 10'un 3 KRİTİK bulgusunun doğrulama raporu

**Rol:** Taşkın (backend) ve Ertuğ (frontend), Opus uygulayıcı · **Dalga:** QA10-KRITIK (30.09, t6) · **Tarih:** 30.09.2026, 20:30–21:10
**İzolasyon:** test DB `kuyumcu_test_t6`, canlı API **5168** (PID 97664), vekil **5169** (PID 11161), başsız Chrome CDP **9611/9612**. Hepsi iş sonunda durduruldu; `lsof` ile 5168, 5169, 9611 ve 9612'de dinleyen süreç kalmadığı görüldü. 5199, 5200 ve 5230–5233 portlarına dokunulmadı. Tarayıcı bölmesi kullanılmadı.
**Durum (tur 2, 30.09 22:30–22:50):** **BİTTİ.** Tur 1'de üç KRİTİK bulgunun güncel ağaçta zaten kapalı olduğu kanıtlanmıştı (§1–§11, aşağıda korunuyor). Hasım doğrulayıcının tasarım hakemi yeni bir KRİTİK buldu: BASİT modda geri alma sonrası bant "Gider stornolandı" diyordu. Tur 2 bunu kapattı (§0). Değişen ürün dosyası: `Gider.razor` (yalnız `StornoOnayla` içindeki iki bant satırı). Yeni dosyalar: `Sozlukler.QA10.cs` ve `QA10KritikTestleri.cs`.

---

## 0. TUR 2 — doğrulayıcı bulgularının kapanışı

### 0.1 Bulgu ve düzeltme
| Bulgu | Ciddiyet | Sonuç |
|---|---|---|
| Tasarım hakemi: BASİT modda geri alma sonrası bant "Gider stornolandı" diyor (jargon), aynı tablodaki rozet "Geri alındı" | KRİTİK | **KAPANDI.** `Kuyumcu.Web/Pages/Gider.razor:937`: `basari = Dil.BasitMi ? QA10Sozlugu.Al(Dil.Dil).GiderGeriAlindiBasit : Dil.T.Gider.StornoYapildi;` Aynı akışın virman dalı da aynı kusuru taşıyordu ("Virman stornolandı · 2 bacak"). `:929-931`: BASİT'te "Virman geri alındı (iki taraf birlikte)", PRO'da hub metni + sözlükten "{0} bacak". Eskiden " bacak" çıplak Türkçe metindi ve EN'de de "bacak" basıyordu; artık EN "{0} legs". |
| Mutasyon geri alımında `cp -p` artımlı derlemeyi atlatıyor | DÜŞÜK | Bu turda geri alma düz `cp` ile yapıldı. Son mutasyondan sonra `dotnet build Kuyumcu.sln --no-incremental` koşuldu: 0 hata. |
| 5199 dinlemiyor | ORTA | Bu paketin işi değil. 22:50'de hâlâ dinleyen yok (`lsof` 0 satır). 5199'a dokunulmadı; yeniden başlatma orkestratörde (gece yetkisi). |
| denetim.sh P-23 ✗ | DÜŞÜK | Hâlâ 3 satır. Sahibi bu kez `PAKETYASAL2-TEST-TUR3 …` kiracısı (SATIS/KARMA, 19:29 UTC). Bu paketin sayacı değil. |
| Uygulayıcının /hurda kanıtı eski koddan | DÜŞÜK | Bu turda /hurda güncel kodla 8 kombinasyonda yeniden ölçüldü (§0.5). |
| `AlisOdemeAraciEkranTestleri…VERESIYE_secimi_TASINMAZ` kararsız | DÜŞÜK | Bu turun tam süitinde yeşil. Sahibi başka şerit. |

**Neden `Sozlukler.QA10.cs`:** Hub (`Sozlukler.cs`) ve BASİT kaplaması (`Sozlukler.Katalog.cs`, `BasitDilSozlugu`) P8 şeridinin alanı, dokunulmadı. Metin görev tanımındaki dosya adıyla ayrı dosyada tutuluyor. Seçimi `Dil.BasitMi` ile ekran yapıyor; bu, aynı sayfadaki "Geri alındı" rozetinin kalıbıyla aynı (`Gider.razor:185`). Kalıp tek dokunuş kuralı gereği yalnız `StornoOnayla` gövdesinde kaldı. Modal başlığı, düğme ve gerekçe etiketi zaten BASİT kaplamasıyla "Geri al (düzeltme fişi)" / "Geri alma sebebi" gösteriyordu. Tarayıcıda görüldü, bkz. §0.5.

### 0.2 Derleme ve testler
- `dotnet build Kuyumcu.sln --nologo`: **0 hata, 2 uyarı.** İki uyarı da başka şeritlerin test dosyalarında: `Dogrulayici/SUBEKAPSAMASinirTestleri.cs(49)` CS8601 (SUBE-KAPSAM) ve `KesifP2Testleri.cs(451)` xUnit2029 (KESIF-P2). Bu paketin dosyalarında uyarı yok. Son mutasyondan sonraki `--no-incremental` derleme de aynı: 0 hata, aynı 2 uyarı.
- Filtreli koşum `QA10Kritik|P19Ekran|Q101|Q102|Q103`: **175/175 başarılı.** Tur 1'deki 144 teste 5 yeni QA10 testi ve P19 ekran testleri eklendi.
- Yeni testler (`Kuyumcu.Tests/QA10KritikTestleri.cs`, 5 adet):
  `QA10_BASIT_gider_geri_alininca_bantta_STORNO_gecmez_Gider_geri_alindi_yazar` ·
  `QA10_PRO_gider_geri_alininca_bant_muhasebe_dilini_KORUR` ·
  `QA10_BASIT_virman_geri_alininca_bantta_STORNO_ve_BACAK_gecmez` ·
  `QA10_PRO_virman_bandi_bacak_sayisini_SOZLUKTEN_yazar` ·
  `QA10_sozluk_TR_EN_dolu_ve_BASIT_metinler_jargonsuz`.
  Sahte API storno uçlarına gerçekçi yanıt veriyor (virman `bacakSayisi=2`). POST'lar sayılıyor ve tam olarak 1 olmaları isteniyor.
- **Tam süit** (tur 2'de bir kez, son kodla; trx `/private/tmp/kuyumcu-serit/trx-pQA10-KRITIK-t2/_Mac_2026-09-30_22_38_33.trx`): **8152 test · 8151 geçti · 0 başarısız · 1 atlandı.** Yeni kırmızı yok.

### 0.3 Mutasyon (tur 2)
Yöntem: yedek `/private/tmp/kuyumcu-serit/QA10-KRITIK/mut-yedek/Gider.razor.yedek`, ardından `perl -0777 -pi` ile tek eşleşmeli değişiklik, ardından `--filter QA10Kritik -v n`. Geri alma **düz `cp`** ile yapıldı ve `cmp` ile doğrulandı. Son adımda `--no-incremental` derleme koşuldu.

| Kod | Değişiklik | Kırmızıya dönen test | Geri alındı |
|---|---|---|---|
| MQA10-1 | Gider dalı: `Dil.BasitMi ? … : ` kaldırıldı, her modda "Gider stornolandı" basılıyor | `QA10_BASIT_gider_geri_alininca_bantta_STORNO_gecmez_Gider_geri_alindi_yazar` | cmp AYNI |
| MQA10-2 | Virman dalı: BASİT kolu kaldırıldı | `QA10_BASIT_virman_geri_alininca_bantta_STORNO_ve_BACAK_gecmez` | cmp AYNI |

Günlükler: `/private/tmp/kuyumcu-serit/QA10-KRITIK/mut-MQA10-{1,2}.log`. Tur 1'in altı mutasyonu (M1-1…M3-2, §3) başka koda aitti ve o kod değişmedi.

### 0.4 denetim.sh
297 ✓ ve 1 ✗ (**DENETİM BAŞARISIZ**, çıkış 1). Tek ✗ yine `P-23 ODEME_KALEMSIZ_YOL=3`. SQL ile bakıldı: üç satır da `PAKETYASAL2-TEST-TUR3 Altın Sarayı Kuyumculuk` kiracısına ait, SATIS/KARMA, 2026-09-30 19:29 UTC. Bu paket ödeme yazmıyor. **Bu paketin sayaçları ✓.** Pages/ altında yeni ModalPencere düzenleme kullanımı yok; tek değişiklik bant metni.

### 0.5 Canlı doğrulama ve gerçek tarayıcı (API 5168 → vekil 5169 → başsız Chrome CDP 9613)
API 5168 (PID 86952) rastgele sistem anahtarıyla başlatıldı; anahtar hiçbir yere yazılmadı. Test kiracısı `PAKETQA10-KRITIK-TEST Kuyumcu` (`01M3SXA70JC5X2AGGN5AA7GKV7`) şu adımlarla kuruldu: davet 201, kayıt 201, giriş 200. API adımları:
- **Gider:** 125,50 → `toplamlar` 125.5, ardından storno 200 → toplam **0** (adet 2), ardından 150 → toplam **150**, ardından 40, 30 ve 20 eklendi → toplam **240**.
- **Hurda:** hurda-alış 10 g K22 200 → bakiye 9,16 g.
- **İşletme kimliği:** PATCH ile 4 alan 200. VKN "12345" **400**, telefon "abc" **400**. `vergiDairesi:null` 200: alan silindi, diğer alanlar korundu. Ardından geri yazıldı, 200.

Tarayıcıda geri alma akışı, BASİT ve PRO karşılaştırmasıyla (`olc2.mjs`; ham veri `olcum2/olcum.jsonl`, tablo `olcum2/tablo.txt`):

| Kombinasyon | Satır | Bant (okundu) | storno POST | Toplam önce → sonra |
|---|---|---|---|---|
| 1280 · BASİT · açık | 40 ₺ | **"Gider geri alındı"** | 1 | 240 → 200 ₺ |
| 375 · BASİT · koyu | 30 ₺ | **"Gider geri alındı"** | 1 | 200 → 170 ₺ |
| 1280 · PRO · açık | 20 ₺ | "Gider stornolandı" (muhasebe dili korunur) | 1 | 170 → 150 ₺ |

SQL: `kasa_hareketleri` tablosunda bu kiracının gider satırları **9** (5 gider + 4 storno). `SUM(-tutar)` = **150.00**, ekrandaki son "Toplam gider: 150 ₺" ile aynı. `hareketler` tablosunda yalnız **HURDA_ALIS 1** var, **HURDA_SATIS 0**: onay penceresi 8 kez açıldı ve 8 kez Vazgeç'e basıldı. `denetim_kayitlari` tablosunda `TenantKimlik` **3** satır.

Ölçüm tablosu (sikisma-olc.js; rota × genişlik × mod × tema; K1 gizli-görünen, K3 taşan/kırık, K5 "TRY", K8 jargon, K10 elips, K11 hata bandı, K12 dikey düğme):

| Rota / durum | 1280 BASİT açık/koyu | 375 BASİT açık/koyu | 1280 PRO açık/koyu | 375 PRO açık/koyu |
|---|---|---|---|---|
| /gider sayfa | K1..K12=0, yatay 0 (K2 %98) | K1..K12=0, yatay 0 (K2 %97) | tümü 0 | tümü 0 |
| /gider geri alma sonrası | açık: tümü 0, bant "Gider geri alındı" | koyu: tümü 0, bant "Gider geri alındı" | açık: tümü 0, bant "Gider stornolandı" | — |
| /hurda sayfa | tümü 0 | tümü 0 | tümü 0 | tümü 0 |
| /hurda onay penceresi → Vazgeç | açıldı, POST 0 → kapandı, POST 0; K9=6* | aynı | aynı | aynı |
| /isletme sayfa | K8=1** diğerleri 0 | K8=1** | tümü 0 | tümü 0 |
| /isletme kimlik çekmecesi | 5 alan, 4'ü dolu; K9=5* K8=1** | 5 alan; K9=6* | 5 alan; K9=4* | 5 alan; K9=6* |

\* K9: ölçer, açık katmanın arkasındaki sayfa yazısını "binen" sayıyor. Tur 1'de katman içi ayrıca ölçülmüştü: binen 0 (§5.4). \*\* K8: tek eşleşme "KRITIK-TEST", yani test kiracısının adı; ürün metni değil.

**Read ile görülen görüntüler** (`/private/tmp/kuyumcu-serit/QA10-KRITIK/olcum2/foto/`):
- `gider-1280-BASIT-aydinlik-storno-sonra.png`: üst çubukta Basit seçili, yeşil bant "Gider geri alındı", "Toplam gider: 200 ₺".
- `gider-375-BASIT-karanlik-storno-sonra.png`: koyu tema, bant "Gider geri alındı", yatay taşma yok.
- `gider-1280-PRO-aydinlik-storno-sonra.png`: Profesyonel seçili, bant "Gider stornolandı", "Toplam gider: 150 ₺".
- `gider-1280-BASIT-aydinlik-storno-onay.png`: modal başlığı ve kırmızı düğme "Geri al (düzeltme fişi)", alan "Geri alma sebebi (en az 20 karakter)", 57/1000 sayacı, İptal düğmesi. BASİT akışta "storno" kelimesi hiçbir yerde geçmiyor.

### 0.6 Temizlik (tur 2)
- `zsh kiraci-temizle.sh 01M3SXA70JC5X2AGGN5AA7GKV7`: rapor modunda 139 satır + 1 tenants satırı çıktı. Ardından `--uygula` ile silindi. `information_schema` üzerinden `tenant_id` kolonu olan 71 tablo tarandı; kalan satır toplamı **0**. Kendi davet satırım (`01M3SXA6Y9MP0HF7F733MQZXG7`) silindi (ROW_COUNT 1). Diğer davet satırı `01M3SVTWJ55…` başka bir şeride ait, dokunulmadı.
- tenants: başlangıçta **15**, sonda **15**. QA-TEST fikstürlerine dokunulmadı.
- API (86952) ve vekil durduruldu. 5168, 5169 ve 9613'te dinleyen süreç yok. Fikstür, anahtar ve durum dosyaları `printf ''` ile boşaltıldı.

### 0.7 Değişen dosyalar (tur 2)
- `dotnet/Kuyumcu.Web/Pages/Gider.razor`: yalnız `StornoOnayla` içindeki iki bant satırı (diff 11 satır). **Tek dokunuş:** bu sayfa QA10-KRITIK dosya alanında. Gerekçe: hakem bulgusu aynı akışın bandını gösteriyordu.
- `dotnet/Kuyumcu.Core/I18n/Sozlukler.QA10.cs` (yeni, TR ve EN, konumsal kayıt).
- `dotnet/Kuyumcu.Tests/QA10KritikTestleri.cs` (yeni, 5 test).
- Migration gerekmedi. Commit yok.

### 0.8 DoD sözleşmesi (tur 2)
Yeni varlık yok. Gider ve virman hâlâ yalnız storno ile düzeltiliyor: hard DELETE yok, onay modalı ve gerekçe zorunlu. Değişen tek şey sonuç bandının BASİT moddaki dili (UX-S27 bant korunuyor, UX-S31 aynı kavram tek ad: "Geri alındı").

---

## 1. Kaynak ve güncel ağaçta doğrulama

Kaynak: `ham/qa-turu-10-bulgular.json` dosyasında `ciddiyet=KRITIK` olan 3 kayıt var: `/gider`, `/hurda` ve `/isletme`. QA turu 10 eski bir yayında ölçülmüştü. Bu yüzden her bulgu önce koda karşı doğrulandı.

| # | Bulgu (QA10) | Güncel ağaçta durum | Kod kanıtı (dosya:satır) | Kapatan paket |
|---|---|---|---|---|
| K1 | /gider: storno sonrası "Toplam gider" mutlak değerle toplanıyor (251 ₺, olması gereken 0) | **KAPALI** | `Kuyumcu.Api/Modules/Gider/GiderModulu.cs:357-362`: toplam sunucuda hesaplanır, formül `g.Sum(x => -x.Tutar)`, işaretli ve para birimine göre ayrı. `Math.Abs` yok. `Kuyumcu.Web/Pages/Gider.razor:507-510`: `ToplamMetni` istemcide toplam hesaplamaz, sunucunun `Toplamlar` değerini basar. Toplam gelmezse "—" yazar. `:142-143` `#gd-toplam` | Q10-1 |
| K2 | /hurda: ramat çıkışı onaysız, tek tıkla ve geri alınamaz biçimde kaydediliyor | **KAPALI** | `Kuyumcu.Web/Pages/Hurda.razor:477-484`: `OnayKipi` (`#hrd-onay-*`) kullanılır. `:1050-1085`: `Kaydet()` gövdeyi dondurur, özeti (maden, has gram, kabul, birim fiyat, tutar, tahsil, kasa) üretir ve yalnız pencereyi açar. `:1105-1114`: `Gonder()` tek POST atar ve bunu yalnız onayla, yalnız dondurulmuş gövdeyle yapar. Açıklama metni `Sozlukler.HurdaOnay.cs:44` (TR) ve `:62` (EN): "…kaydedildikten sonra değiştirilemez ve silinemez." | Q10-2 |
| K3 | /isletme: kimlik penceresinde yalnız ad düzenlenebiliyor | **KAPALI** | `Kuyumcu.Web/Pages/Isletme.razor:476-512`: `Cekmece` 5 alan içerir (`#ikimlik-ad`, `-adres`, `-telefon`, `-vergidairesi`, `-vergino`). Alanlar her modda görünür, `ProAlan` içinde değildir. `:887-902`: `KimlikKaydet` PATCH `/admin/tenant/kimlik` ile beş alanı gönderir. `Kuyumcu.Api/Modules/Admin/AdminModulu.cs:406` sınıf düzeyinde `[RolGerekli("SAHIP")]` taşır. `:428-430` `[HttpPatch("tenant/kimlik")]`. `:322` `KimlikGuncelle` şu kuralları uygular: gövdede olmayan alan korunur, null gelen alan silinir. VKN/TCKN `VergiKimlik.Mesaj` ile, telefon `Iletisim.TelefonGecerli` ile, ad `IsletmeAdi` ile doğrulanır. Değişiklik `denetim?.YazmaKaydet(... "TenantKimlik" ...)` ile denetim kaydına yazılır ve VKN ile telefon maskelenir. | Q10-3 (UX-1-B ucunu kullanır) |

**Ekip oyu kararlarına uyum (30.09 20:40):**
1. Gider toplamı işaretli toplanıyor. Storno satırı eksi olarak girer ve "Toplam gider" net gideri gösterir. Bunu hem bUnit hem API testleri doğruluyor.
2. Ramat çıkışında özet onay penceresi var: miktar, maden, kasa, tutar ve "değiştirilemez ve silinemez" cümlesi. İptal edilince istek atılmaz, onaylanınca tek POST atılır. Storno ile geri alma bu paketin kapsamında değil.
3. İşletme kimliğinde adres, telefon, vergi dairesi ve VKN/TCKN düzenlenebiliyor. SAHIP rolü, denetim kaydı ve doğrulama kuralları yerinde. KESIF-P1'in İşletme onay kalıbına dokunulmadı; paket hiçbir dosyayı değiştirmedi.

**Sapma ve gerekçesi:** Görev tanımındaki dosya alanında "yeni testler QA10KritikTestleri.cs" ve "Sozlukler.QA10.cs" geçiyor. Bunlar **açılmadı**, çünkü üç kural da zaten mevcut ve adı belli testlerle korunuyor (144 test). Mutasyonlar bu testleri kırmızıya çevirdi (§3). Aynı kuralı sınayan kopya test yazmak kapsam dışı olurdu. Koşul da "kapanmışsa kanıtla ve yalnız kalanları yap" diyor.

## 2. Derleme ve testler

- `dotnet build Kuyumcu.sln --nologo`: **0 uyarı, 0 hata** (build1). Son mutasyondan sonraki temiz derleme de **0 uyarı, 0 hata** (build2).
- Filtreli koşum `FullyQualifiedName~Q101|Q102|Q103` (bu paketin testleri): **144/144 başarılı, 0 başarısız.** Kapsanan sınıflar: `Q101GiderToplamTestleri`, `Q101GiderToplamEkranTestleri`, `Dogrulayici/Q101SinirTestleri`, `Q101RaporStornoTestleri`, `Q102HurdaOnayEkranTestleri`, `Q102HurdaSinirTestleri`, `Dogrulayici/Q102SinirTestleri`, `Q103IsletmeKimlik*Testleri`.
- Tam süit, `--logger trx --results-directory /private/tmp/kuyumcu-serit/trx-pQA10-KRITIK` (20:46–20:51, son kodla, tek koşum): **8101 test · 8098 geçti · 2 başarısız · 1 atlandı.**
  - `KesifP7Testleri.Segment_secili_dolgu_HER_temada_okunur` (`KesifP7Testleri.cs:290`, beklenen 3, gelen 0). Sahibi **KESIF-P7 (t1)**. Test ortak CSS dosyasını okuyor; o sırada t1 şeridi `app.css`, `dugme.css` ve benzeri dosyalar üzerinde çalışıyordu.
  - `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter_DOGRUDAN_ekler_bekleyen_arama_IPTAL` (`SATIS3EkranTestleri.cs:221`, beklenen 0, gelen 1). Sahibi **SATIS-3**. Zamanlamaya bağlı bir bUnit testi.
  - İkisi aynı derlemeyle tek başına **2 kez** yeniden koşuldu ve ikisinde de **2/2 geçti.** Bu paket ürün kodunda hiçbir dosyayı değiştirmedi (§6), bu yüzden **yeni kırmızı sayısı 0.** İki test yük altında kararsız davranıyor.

## 3. Mutasyon kanıtı

Yöntem: kaynak dosya `/private/tmp/kuyumcu-serit/mut-yedek/QA10-KRITIK/<kod>.yedek` altına kopyalandı. Ardından `perl -0777 -pi -e` ile tek eşleşmeli değişiklik yapıldı ve `dotnet test --filter <ilgili> -v n` koşuldu. Sonra yedek geri kopyalandı ve `cmp` ile karşılaştırıldı. Altı mutasyonun hepsinde sonuç **GERİ ALINDI (cmp birebir aynı)**. İş sonunda altı dosya yedekleriyle yeniden karşılaştırıldı ve hepsi aynı çıktı.

| Kod | Değişiklik | Kırmızıya dönen testler |
|---|---|---|
| M1-1 | `GiderModulu.cs`: `g.Sum(x => -x.Tutar)` yerine `g.Sum(x => Math.Abs(x.Tutar))` (ölçülen kusurun kendisi) | 13 test. Örnekler: `Q101_gider_ve_stornosu_toplamda_NET_SIFIR_olur_kasa_bakiyesi_doner`, `Q101_kismi_storno_yalniz_stornolanan_gideri_duser`, `Q101_para_birimleri_ayri_toplanir_KARISMAZ`, `DQ101b_uc_gider_bir_storno_limit_1_iken_toplam_125_25`, `DQ101c_ikinci_storno_reddedilir_toplam_sifir_kalir` |
| M1-2 | `Gider.razor` `ToplamMetni`: sunucu toplamı yerine istemcide `giderler.Sum(Math.Abs)` | `Q101_ekran_storno_sonrasi_toplami_sunucudan_NET_SIFIR_gosterir`, `Q101_ekran_kismi_stornoda_sunucunun_150_toplamini_yazar`, `Q101_ekran_para_birimlerini_ayri_yazar` |
| M2-1 | `Hurda.razor` `Kaydet()`: pencere açılınca `_ = Gonder();` ekleniyor, yani onaysız POST | 10 test. Örnekler: `Kaydet_tiklaninca_POST_ATILMAZ_once_ozetli_onay_penceresi_acilir`, `Vazgec_istek_ATMAZ_ve_pencere_kapanir`, `Onaylaninca_POST_gider_bant_yazilir_ve_pencere_kapanir`, `Onay_penceresi_acikken_her_dugme_ya_istek_atar_ya_DISABLED` |
| M2-2 | `Hurda.razor` `OnayKipi`: `Vazgec="OnayKapat"` yerine `Vazgec="Gonder"` | `Vazgec_istek_ATMAZ_ve_pencere_kapanir`, `Vazgec_sonrasi_yeniden_Kaydet_GUNCEL_kurla_YENI_ozet_ve_govde_uretir` |
| M3-1 | `AdminModulu.cs`: `Uygula(dto.AdresGonderildi, …)` yerine `Uygula(false, …)`, yani adres sunucuda yazılmıyor | `Q103_H03_Bos_VKN_siler_gonderilmeyen_alanlar_korunur`, `Q103_H04_Denetim_izi_ESKI_YENI_yazar_tam_deger_yazmaz`, `Q103_H05_Cikti_belgesi_YENI_adres_ve_VKN_basar` |
| M3-2 | `Isletme.razor` `KimlikKaydet`: `vergiNo` gövdeden düşürülüyor | `Q103_E02_BASIT_modda_gövde_dort_alani_TASIR`, `Q103_E04_Bos_VKN_acik_null_gonderir_silme` |

Günlükler: `/private/tmp/kuyumcu-serit/QA10-KRITIK/mut-<kod>.log`.

## 4. denetim.sh

`bash dotnet/denetim.sh`: 297 ✓ ve 1 ✗, çıkış kodu 1 (**DENETİM BAŞARISIZ**). Tek ✗ şu satır:
`✗ P-23: ODEME_KALEMSIZ_YOL (beklenen 0) 3 hareket ödeme yazdı ama kalem yazmadı`
Bu sayaç canlı `kuyumcu_net` verisini ölçüyor. Üç satırın kaynağını SQL ile belirledim. Üçü de **başka bir şeridin test kiracısına** ait: `PAKETYASAL2-TEST-HAKEM Altın Sarayı Kuyumculuk` (`01M3SQHTE445AB1438ACXXW7DJ`), `tur=SATIS`, `odeme_turu=KARMA`, oluşturulma 17:59:50 UTC (benim koşumumun ortası). Benim kiracımda bu sayaca düşen satır yok; kiracım silinmeden önce de yoktu. Bu paket kod değiştirmediği için hiçbir kod sayacı etkilenmedi. **Bu paketin sayaçları ✓.** Yukarıdaki ✗'in sahibi SUBE-YASAL23/YASAL2 hakem şeridi (açık kalanlar).

## 5. Canlı doğrulama (API 5168 → `kuyumcu_net`)

Test kiracısı `PAKETQA10-KRITIK-TEST Kuyumcu` (`01M3SPX306S4C366QF370BFJXH`) şu adımlarla kuruldu: `POST /sistem/davetler` **201**, ardından `POST /auth/kayit` **201**, ardından `POST /auth/giris` **200**. Sistem anahtarı rastgele üretildi, değeri hiçbir çıktıya yazılmadı. Başlangıçta tenants sayısı **13** idi.

### 5.1 Gider (K1)
| Adım | HTTP | Yanıt (okundu) |
|---|---|---|
| POST /kasalar (Merkez Kasa, TRY) · POST /kasalar/{id}/hareket GIRIS 5000 | 200 · 200 | — |
| POST /gider-kategorileri {Kira} | 200 | — |
| POST /giderler 125,50 | 200 | id `01M3SPY3XEZRDVY4A9NPFXADEZ` |
| GET /giderler (storno öncesi) | 200 | `toplamlar=[{"paraBirimi":"TRY","tutar":125.5,"adet":1}]` |
| POST /kasalar/{k}/hareket/{id}/storno | 200 | — |
| GET /giderler (storno sonrası) | 200 | `toplamlar=[{"paraBirimi":"TRY","tutar":0,"adet":2}]` · satırlar `[-125.5, storno], [125.5, stornolandı]` |
| POST /giderler 150 → GET /giderler | 200 · 200 | `toplamlar=[{"paraBirimi":"TRY","tutar":150,"adet":3}]` |

SQL: `kasa_hareketleri` tablosunda üç satır var: −125.50, +125.50 ve −150.00. `SUM(-tutar)` = **150.00**, API'nin döndürdüğü toplamla aynı. Eski kusur mutlak toplam alıyordu; o hâliyle sonuç 401 olurdu.

### 5.2 Hurda (K2)
Hazırlık: kasaya GIRIS 60000 (200), ardından `POST /islemler/hurda-alis` ile 10 g K22 alındı (200). `/hurda/bakiye` sonucu Altın 9,16 g. Tarayıcıda, 8 kombinasyonun her birinde (§5.4) şu adımlar izlendi: 0,5 g girildi, "Ramat çıkışını kaydet" tıklandı, onay penceresi açıldı ve bu sırada **hurda-satis POST sayısı 0** kaldı. Ardından Vazgeç tıklandı; pencere kapandı ve **POST sayısı yine 0** kaldı. Yalnız 1280 · PRO · açık tema kombinasyonunda Kaydet onaylandı. O kombinasyonda tam **1 POST** gitti (`POST /api/v1/islemler/hurda-satis`), pencere kapandı ve şu bant çıktı: "Hurda satışı kaydedildi: Altın 0,500 g · 3.286,36 ₺ · Kalan hurda: 8,660 g".
SQL: `hareketler` tablosunda `tur LIKE 'HURDA%'` koşuluyla **HURDA_ALIS 9.160000** ve **tek HURDA_SATIS −0.500000 / 3286.36** var. Pencere 8 kez açılıp 7 kez vazgeçildi, yine de yalnız bir kayıt oluştu.

### 5.3 İşletme kimliği (K3)
| Adım | HTTP | Yanıt |
|---|---|---|
| GET /admin/tenant | 200 | adres, telefon, vergi dairesi ve vergi no **null** |
| PATCH /admin/tenant/kimlik {adres, telefon, vergiDairesi, vergiNo} | 200 | dört alan da yeni değerle döndü |
| PATCH {vergiNo:"12345"} | **400** | `alanlar.vergiNo: "Vergi no 10 hane, TC kimlik no 11 hane olmalıdır"` |
| PATCH {telefon:"abc"} | **400** | `alanlar.telefon: "Telefon yalnız rakam, …(10–13 hane)"` |
| PATCH {vergiDairesi:null} | 200 | vergi dairesi null oldu; adres, telefon ve vergi no **korundu** (PATCH sözleşmesi) |
| PATCH {vergiDairesi:"Beyazit"} | 200 | geri yazıldı |

SQL: `tenants` satırında adres, telefon, vergi dairesi ve vergi no yeni değerleriyle duruyor. `denetim_kayitlari` tablosunda `varlik_turu='TenantKimlik'` olan **3 satır** var: 4 alanlık değişiklik, vergi dairesinin silinmesi ve geri yazılması. Tam VKN'nin geçtiği denetim satırı sayısı **0**, yani değer maskeleniyor.

### 5.4 Gerçek tarayıcı ölçümü (başsız Chrome, CDP 9611 → vekil 5169 → API 5168)
Betik: `/private/tmp/kuyumcu-serit/QA10-KRITIK/olc.mjs`. Ölçüm için `sikisma-olc.js` sayfaya enjekte edildi. Sayfa ölçümünde `__olcTam`, katman açıkken `__olc` kullanıldı. Mod, üst çubuktaki düğmeyle seçildi ve `aria-pressed` ile doğrulandı. Tema `kuyumcuTema.uygula` ile ayarlandı. Ham veri `olcum/olcum.jsonl`, özet `olcum/tablo.txt`, ekran görüntüleri `olcum/foto/` altında (41 PNG).

| Rota / durum | 1280 BASİT açık/koyu | 375 BASİT açık/koyu | 1280 PRO açık/koyu | 375 PRO açık/koyu |
|---|---|---|---|---|
| /gider sayfa | K1..K12=0, yatay 0, K2 %98; **"Toplam gider: 150 ₺"** | aynı, K2 %97 | aynı | aynı |
| /hurda sayfa | tümü 0, yatay 0 | tümü 0 | tümü 0 | tümü 0 |
| /hurda onay penceresi | açıldı, POST 0; K1/K3/K5/K8/K10/K11/K12/yatay=0; K9=6* | aynı | aynı | aynı |
| /hurda vazgeç | pencere kapandı, POST 0 | aynı | aynı (1280 PRO açıkta **Onay**: 1 POST + bant) | aynı |
| /isletme sayfa | tümü 0, yalnız K8=1** | K8=1** | tümü 0 | tümü 0 |
| /isletme kimlik çekmecesi | **5 alan** (ad, adres, telefon, vergi dairesi, vergi no), dördü dolu; K9=5* K8=1** | 5 alan; K9=6* | 5 alan; K9=4* | 5 alan; K9=6* |

\* **K9 (binen):** Ölçer, katmanın **arkasında kalan sayfa yazılarını** katmanın yazılarıyla çakışmış sayıyor. Örnekler: `h3 "Elimizdeki hurda" ⟂ #modal "Ramat çıkışı onayı"`, `p "Süre uzatımı yalnız sistem" ⟂ "Telefon"`. Bunu ayırmak için katmanın **içini** ayrıca ölçtüm (`katman.mjs`, `olcum/katman.jsonl`). 1280 ve 375 genişlikte, BASİT ve PRO modda, `ASIDE.cekmece` ile `DIV.modal-panel` için sonuç aynı: **katman içi binen 0 · katman dışına taşan 0 · yatay 0.** Çekmece 480×800 (375'te 375×812), modal 512×348 (375'te 351×444).
\** **K8 (jargon):** Tek eşleşme `"KRITIK-TEST"`. Bu, benim açtığım test kiracısının adı ("PAKETQA10-KRITIK-TEST Kuyumcu"). Ürün metni değil, fikstür adıdır.

**Gözle kontrol edilen ekran görüntüleri (Read):**
- `isletme-375-BASIT-aydinlik-kimlik.png`: beş alan dolu, "(isteğe bağlı)" etiketleri, belge notu, altta İptal ve Kaydet. Taşma yok.
- `hurda-1280-BASIT-aydinlik-onay.png` ve `hurda-375-BASIT-karanlik-onay.png`: başlık "Ramat çıkışı onayı", "değiştirilemez ve silinemez" cümlesi, özet (maden, gram, kabul, birim fiyat, tutar, tahsil, kasa), düzeltme yolu, Vazgeç ve kırmızı Kaydet.
- `hurda-1280-PRO-aydinlik-onaylandi.png`: yeşil başarı bandı; kalan hurda 8,660 g; çıkan has 0,500 g.
- `gider-1280-PRO-karanlik.png`: "Toplam gider: 150 ₺"; satırlar 150 ₺, −125,50 ₺ "Storno satırı", 125,50 ₺ "Stornolandı".
- `gider-375-BASIT-aydinlik.png`: "Toplam gider: 150 ₺". Tablo kendi kabında yatay kaydırılıyor ve bunu söyleyen not ekranda yazılı. Sayfanın kendisinde yatay kaydırma 0.

## 6. Değişen dosyalar (tur 1 — tur 2 için bkz. §0.7)

Ürün ağacında **değişen dosya yok.** Altı mutasyonun hepsi `cmp` ile birebir geri alındı ve iş sonunda altı dosya mutasyon öncesi yedekleriyle yeniden karşılaştırılıp aynı bulundu. `git status --porcelain | grep -v -E "/(bin|obj)/"` bu dosyaları (`Gider.razor`, `Hurda.razor`, `Isletme.razor`, `GiderModulu.cs`, `AdminModulu.cs`) M ya da ?? olarak gösteriyor. Bu, depo ilk commit'ine göre önceki paketlerin (Q10-1, Q10-2, Q10-3, UX-1-B) izidir; bu paketin değişikliği değildir. Yeni dosya yalnız bu rapor. Geçici dosyalar `/private/tmp/kuyumcu-serit/QA10-KRITIK/` ve `/private/tmp/kuyumcu-serit/mut-yedek/QA10-KRITIK/` altında. Migration gerekmedi.

## 7. DoD sözleşmesi

| Varlık | Düzenleme yolu | Not |
|---|---|---|
| Gider (kasa_hareketleri, finansal) | Hard DELETE yok. Düzeltme `POST /kasalar/{id}/hareket/{hareketId}/storno` ile yapılır, ekranda "Storno / Geri al" düğmesi var | `DegismezIpucu` gerekçesi ekranda yazılı |
| Hurda satışı (hareketler, finansal) | Yerinde değişmez. Onay penceresi bu gerekçeyi ve düzeltme yolunu ("Tahlil farkını düzelt" ve müşteri düzeltme fişi) yazar. Storno ile geri alma **kurucu kararı bekliyor** (kuyrukta, bu pakette değil) | onay zorunlu (UX-S27) |
| İşletme kimliği (tenants) | `PATCH /admin/tenant/kimlik` (SAHIP) ile. Gövdede olmayan alan korunur, null gelen alan silinir. Denetim kaydı `YazmaKaydet` ile yazılır. Ekranda "İşletme kimliğini düzenle" çekmecesi 5 alanla açılır | yeni varlık yok |

## 8. Koşul karşılama (tur 1 — tur 2 kanıtları §0)

| Koşul | Durum | Kanıt |
|---|---|---|
| Önce güncel ağaçta doğrula, kapanmışsa kanıtla | ✓ | §1, dosya:satır ile |
| Gider toplamı işaretli, storno eksi, bUnit ve API testi | ✓ (zaten kapalı) | Q101 testleri, M1-1 ve M1-2 kırmızı, canlıda 0 → 150 |
| Ramat onay penceresi: özet ve "geri alınamaz"; İptal'de istek 0, Onay'da tek POST | ✓ (zaten kapalı) | Q102 testleri, M2-1 ve M2-2 kırmızı, tarayıcıda 8×0 POST ve 1×1 POST, SQL'de tek HURDA_SATIS |
| İşletme kimliğinde 4 alan düzenlenebilir, SAHIP rolü, denetim, VKN/TCKN kuralı | ✓ (zaten kapalı) | Q103 testleri, M3-1 ve M3-2 kırmızı, canlıda 200/400 yanıtları ve 3 denetim satırı |
| KESIF-P1 İşletme onay kalıbı bozulmadı | ✓ | dosyaya dokunulmadı |
| build 0/0 | ✓ | build1 ve build2 |
| Filtreli testler yeşil | ✓ | 144/144 |
| Mutasyon kırmızı ve cmp aynı | ✓ | 6/6 |
| Canlı API ve SQL | ✓ | §5 |
| Başsız Chrome: 1280+375 · BASİT+PRO · açık+koyu, görüntü Read ile görüldü | ✓ | §5.4 |
| denetim.sh: paketin sayaçları ✓ | ✓ | tek ✗ başka şeridin canlı verisinden (§4) |
| Tam süitte yeni kırmızı 0 | ✓ | 2 kırmızı başka şeritlerin, tek başına koşulunca 2/2 yeşil |
| Test kiracısı silindi | ✓ | §9 |
| Commit yok, dosya silme yok, sır basılmadı | ✓ | — |

## 9. Temizlik

- `zsh kiraci-temizle.sh 01M3SPX306S4C366QF370BFJXH` önce rapor modunda koşuldu: 174 satır + 1 tenants satırı. Ardından `--uygula` ile silindi. `information_schema` üzerinden `tenant_id` kolonu olan bütün tablolar tarandı ve kalan satır **0** çıktı. Davet satırı `kiraci_davetleri` (`01M3SPX20HSNXZVGKJEHMN0BVF`) tek satır olarak silindi (ROW_COUNT 1).
- tenants sayısı: başlangıçta **13**, şimdi **15**. Fark başka şeritlerin koşum sırasında açtığı iki kiracıdan geliyor: `PAKETKESIF-P7-TEST Tur2` (17:52 UTC) ve `PAKETYASAL2-TEST-HAKEM …` (17:59 UTC). Benim kiracım için sayım **0.** QA-TEST fikstürlerine dokunulmadı.
- Fikstür, anahtar ve durum dosyaları `printf ''` ile boşaltıldı. Süreçler (API 97664, vekil 11161, başsız Chrome'lar) durduruldu ve portlar boş.

## 10. Açık kalanlar

1. **[denetim ✗, sahibi: SUBE-YASAL23 / YASAL2 hakem şeridi]** P-23 `ODEME_KALEMSIZ_YOL=3`. Satırlar `PAKETYASAL2-TEST-HAKEM` kiracısının KARMA ödemeli 3 SATIS hareketi. O şeridin kiracısı silinince ya da kalem yazımı düzeltilince sayaç kendiliğinden kapanır.
2. **[Kararsız test, sahibi: KESIF-P7 (t1)]** `KesifP7Testleri.Segment_secili_dolgu_HER_temada_okunur` tam süitte kırmızı, tek başına yeşil. Test CSS dosyasını diskten okuyor ve t1 şeridi o sırada CSS düzenliyordu.
3. **[Kararsız test, sahibi: SATIS-3]** `SATIS3EkranTestleri.Barkod_dizisi_ve_Enter_DOGRUDAN_ekler_bekleyen_arama_IPTAL` tam süitte kırmızı, tek başına 2/2 yeşil. Zamanlamaya duyarlı.
4. **[Kurucu kararı, kuyrukta]** Hurda satışının storno ile geri alınması (ekip kararı 2 gereği bu pakette değil).
5. **[Gözlem, kapsam dışı, sahibi: /gider ekranının sahibi]** Tek gider kategorisi varken "Kategori" seçicisi "—" ile açılıyor. UX-S22'ye göre tek seçenek otomatik konmalı. Bu bir QA10 KRİTİK bulgusu değil ve dosya alanım yalnız toplam hesabıyla sınırlı olduğu için dokunulmadı.
6. **[Ölçer iyileştirmesi, sahibi: orkestratör betikleri]** `sikisma-olc.js` K9, açık katmanın (çekmece, modal) arkasındaki sayfa yazılarını "binen" sayıyor. `katman.mjs`'teki katman içi ölçüm ölçere eklenebilir.

## 11. Kurucu / mali müşavir

Mevzuat hükmü içeren bir değişiklik yapılmadı. VKN (10 hane) ve TCKN (11 hane) biçim kuralı mevcut `Kuyumcu.Core.Domain.VergiKimlik` içinde duruyor ve Q10-3'te yazıldı. Bu pakette ona dokunulmadı.
