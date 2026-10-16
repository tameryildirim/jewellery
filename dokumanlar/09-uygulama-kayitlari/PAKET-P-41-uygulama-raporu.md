# PAKET P-41 — Yayın çıktı envanteri (SHA-256) + fail-closed açılış kapısı

> Uygulayıcı: Taşkın (backend) + Ertuğ (frontend rolü) · Opus
> Dalga: YAYIN HATTI + ÖLÇÜM ARACI (tur 8 triyajı Katman A)
> Tarih: 2026-09-23 · **Gerçekleşen süre: 15:22 → 16:20 ≈ 1 saat** (triyaj tahmini 2,5 saat — tahmin fazlaydı; kod C# değil kabuk olduğu ve migration gerekmediği için kısaldı)
> Kaynak paket şartnamesi: `dokumanlar/09-uygulama-kayitlari/TRIYAJ-TUR8-2026-09-23.md` §2 Katman A
> **COMMIT YOK** — çalışma ağacında kalındı.

---

## 1. Ne yapıldı (özet)

Yayın hattına **çıktı envanteri** ve **iki uçlu fail-closed kapı** kondu:

1. **`betikler/yayin-butunluk.sh` (YENİ)** — `--uret` yayın dizininin SHA-256 envanterini
   (`<hedef>/.yayin-envanteri`, satır biçimi `<sha256> <yol> <bayt>`) yazar; `--dogrula`
   her satırı diske karşı ölçer (var mı · 0 bayt mı · boyut tutuyor mu · SHA tutuyor mu)
   ve ayrıca **kaynak `wwwroot` kök dosyalarının HEPSİ envanterde mi** diye bakar
   (kilavuz.html tuzağı). Tek ihlalde çıkış ≠ 0 ve ihlal eden **yol stdout'ta**.
2. **`betikler/yayinla.sh`** — publish sonrası `--uret` + `--dogrula` çağırır.
   Bütünlük kırmızıysa **`.agac-damgasi` YAZILMAZ** ve çıkış 1. İlk başarılı koşumda
   `ham/YAYIN-ENVANTER-ZORUNLU` anahtarını üretir (mevcut `YAYIN-DAMGA-ZORUNLU` deseninin ikizi).
3. **`betikler/qa-sunucu.sh`** — ağaç damgası kapısından **HEMEN SONRA** (ve `cd "$YAYIN"` ile
   KURU KOŞUM çıkışından ÖNCE) `--dogrula` çağrılır. Çıkış ≠ 0 ise **yeni çıkış kodu 14**.
   `YAYIN-ENVANTER-ZORUNLU` varken envantersiz anlık görüntü **çıkış 15**; anahtar yokken
   yalnız YÜKSEK SESLE uyarı + açılış.
4. **`betikler/testler/yayin-butunluk-testleri.sh` (YENİ)** — 8 kabuk testi; gerçek süreç çağırır.
5. **`Kuyumcu.Tests/YayinEnvanterTestleri.cs` (YENİ)** — 3 xUnit testi; betiği gerçekten koşar
   (metin denetimi değil) ve envanter kapsamını kanıtlar.

### Neden bu paket (ölçülen arıza)
- Kaynakta `Kuyumcu.Web/wwwroot/kilavuz.html` **VAR (60.885 bayt)**, yayın anlık görüntüsünde **YOK**.
- Yayın "başarılı" publish'ten çıkmıştı ve **ağaç damgası TUTUYORDU** — damga çıktının TAM
  olduğunu sormaz. Bu yüzden tur 8'de grup 1'in 6 rotası hiç açılamadı ve 1/1 puanlandı.
- Paket **faili bulmaz** (23/09 12:05'te `_framework`e kimin dokunduğu DOĞRULANAMADI, publish
  günlüğü yok); **kapısızlığı kapatır**.

---

## 2. Sapmalar ve gerekçeleri (Anayasa md.1 — gizlenmiyor)

| # | Sapma | Gerekçe |
|---|---|---|
| S-1 | Şartnamenin işaret ettiği **"Birleşik koşullar" başlığı `TRIYAJ-TUR8-2026-09-23.md` içinde YOKTUR** (belge 254 satır; başlıklar: 0/1/2/3/4/5). | Görev metnindeki **EK KOŞULLAR** bloğu (KAPSAM · DOSYA AYAK İZİ · KABUL KOŞULLARI · TEST/MUTASYON LİSTESİ) bağlayıcı kabul edildi; triyaj belgesinden §2 Katman A satırı + §3.1 seri zincir + §5 açık kalemler kullanıldı. Belgeye **DOKUNULMADI**. |
| S-2 | Şartname "canlı `/kilavuz` = `404 0`" diyor. **Şimdi ölçülen:** `/kilavuz` → **`200 1605`** (Blazor SPA geri düşüşü = `index.html`), `/kilavuz.html` → **`404 0`**. | 60.885 baytlık statik kılavuz yine de **servis EDİLMİYOR**; SPA geri düşüşü arızayı maskeliyor — yani arıza şartnamedekinden **daha sinsi**. Envanter kapısı dosya düzeyinde çalıştığı için maskelemeden etkilenmez. `/kilavuz` rotasının hangi içeriği göstermesi gerektiği **P-43'ün (duman testi) kalemi**, bu pakette karara bağlanmadı. |
| S-3 | `yayinla.sh`'in `HEDEF` satırına `${KUYUMCU_YAYIN_DIZINI:-…}` seamı eklendi (varsayılan **değişmedi**). | Kurucunun **5199'una ve yayın anlık görüntüsüne elle/dolaylı dokunmamak** için. Bütünlük kapısı uçtan uca ayrı bir hedefte kanıtlandı (§7). `qa-sunucu.sh` bu değişkeni zaten dinliyordu → hat artık simetrik. **Kalıcı tek-kaynak yol P-42'nin işidir**; P-42 uygulayıcısı bu satırı devralacak. |
| S-4 | `qa-sunucu.sh`'e `KUYUMCU_ENVANTER_ZORUNLU` test seamı eklendi (varsayılan depo yolu). | Anahtarın **VAR** ve **YOK** dallarının ikisi de deterministik sınanabilsin diye. Üretim davranışı değişmedi: değişken verilmezse depo yolu okunur. |
| S-5 | Envantere `blazor.boot.json`, `dotnet.js` ve (varsa) `blazor.webassembly.js` de eklendi. | `blazor.boot.json` **kendi kaynak listesinde yoktur**; kapsam dışı bırakılsa manifestin kendisi sessizce değiştirilebilirdi — tur 8'de tam bu sınıftan bir dokunuş oldu. Kabul koşulunun ">= 66" alt sınırı bozulmaz, aşılır. |
| S-6 | `satelliteResources` bilinçli olarak kapsam DIŞI. | İç içe (kültür → dosya) sözlüktür, düz `_framework/<ad>` yoluna oturmaz. Yanlış yol üretmektense kapsam dışı bırakıldı; bugünkü yayında bu bölüm **yok** (ölçüldü). |

**Mevzuat/yasal hüküm:** Bu pakette mevzuata bağlı hiçbir hüküm yok — tamamı yayın hattı altyapısı.
Kurucuya/mali müşavire sabitlenen bir kural üretilmedi.

---

## 3. Migration

**GEREKMEDİ.** Paket veri modeline, varlıklara, uçlara ya da veritabanına hiç dokunmuyor.
`dotnet ef` **çalıştırılmadı**. Migration sayısı değişmedi (66).

---

## 4. DoD sözleşmesi (Bertan koşulu, 2026-09-16 12:12 mührü)

| Varlık / kayıt türü | Durum |
|---|---|
| **Yeni varlık YOK** | Paket hiçbir `IKiraciVarlik` üretmiyor; tablo, PATCH ucu, DELETE ucu, `[RolGerekli]`, `DenetimServisi.YazmaKaydet` çağrısı gerektirecek **hiçbir kayıt türü doğmadı**. |
| `<hedef>/.yayin-envanteri` | **Türetilmiş çıktı, kayıt değil.** Elle düzenlenemez ve düzenlenmemelidir: `yayinla.sh` her koşumda yeniden üretir. "Düzeltme yolu" = `zsh betikler/yayinla.sh` koşmak. Değişmezlik gerekçesi budur. |
| `ham/YAYIN-ENVANTER-ZORUNLU` | **Tek yönlü anahtar** (`YAYIN-DAMGA-ZORUNLU` ile aynı desen). Bir kez açılır, kapatılması **kurucu/orkestratör kalemidir** — ölçüm aracının kapısını bir ajanın kendi kendine kapatabilmesi, kapının kendisini anlamsız kılardı. |
| Ekran / düğme | **Paket ekran açmıyor.** Hiçbir `.razor` dosyasına dokunulmadı → "her düğme ya istek atar ya disabled+gerekçe", "tek düzenleme kalıbı", "alan altı Türkçe mesaj", "yıkıcı eylem onay modalı" maddeleri **bu pakette uygulanabilir değil** (boş geçilmedi, kapsam dışı). |
| Finansal varlıkta hard DELETE | **Yok** — pakette DELETE hiç yok. |

**Dokunulan sayfalar (DoD EK 2 — TEK DOKUNUŞ):** *hiçbiri.* Bu dalgada `Pages/` altında tek satır değişmedi.

**UX sözleşmesi (Katman 8):** UX-S21…S33 maddeleri ekran sözleşmesidir; bu paket ekran üretmediği
için **uygulanabilir değildir**. Yerine getirilen en yakın ruh: **UX-S28** ("hata 'kayıt yok'a
çevrilmez") — kapı, eksik çıktıyı sessiz boş duruma DEĞİL, gerçek çıktısıyla (yol + çıkış kodu)
rapor eder ve 5199'u açmaz.

---

## 5. Derleme

```
$ dotnet build Kuyumcu.sln --nologo
    0 Uyarı
    0 Hata      (rc=0)
```

**Not (md.1):** Paket sırasında ağaç **iki kez başka şeritler yüzünden kırmızıydı** ve gizlenmedi:
- `Pages/Yedekler.razor(146,80): error CS0103: 'GerekceKaydet' adı geçerli bağlamda yok` → DALGA 7c / P-56 şeridi
- `Pages/SiparisDetay.razor(96,51): error CS0136: 't' adlı bir yerel …` → P-51/P-52 şeridi

İkisi de **P-41'in dokunduğu dosyalar değildir**; o şeritler bitene kadar beklendi, sonra ölçüm alındı.

---

## 6. Testler

### 6.1 Sayılar (önce / sonra)

| Kapsam | Önce | Sonra | Yeni |
|---|---|---|---|
| `dotnet test` (Kuyumcu.Tests) | 3982 | **3985** | **+3** |
| Kabuk testleri (`yayin-butunluk-testleri.sh`) | 0 | **8** | **+8** |
| **Toplam yeni test** | | | **11** |

### 6.2 Tam süit

```
$ KUYUMCU_TEST_DB=kuyumcu_test_p41 dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj \
    --nologo -v n --logger trx --results-directory …/scratchpad/trx-pP-41
Toplam test sayısı: 3985
     Geçti: 3985
     Başarısız: 0        (rc=0)
```

**Neden `KUYUMCU_TEST_DB=kuyumcu_test_p41`? (md.1 — ölçülmüş engel, kılıf değil)**
İlk tam süit koşumu `kuyumcu_test` üzerinde **1073 başarısız** verdi. Sebep ölçüldü:

```
MySqlConnector.MySqlException: Duplicate entry
  '01M3759FEZ5DX2A2KQ6FR27WPC-SAKLAMA_ONIZLEME-2026-09-23-E1554'
  for key 'is_calismalari.IX_is_calismalari_tenant_id_is_kodu_calisma_anahtari'
$ ps -Ao pid,lstart,command | grep testhost
 8436 Wed Sep 23 15:53:37 2026 … dotnet exec … Kuyumcu.Tests
19296 Wed Sep 23 15:55:59 2026 … dotnet exec … Kuyumcu.Tests
```

Aynı anda **iki başka şeridin testhost süreci** aynı `kuyumcu_test` veritabanını kullanıyordu
(fikstür "süreç başına EnsureDeleted+Migrate" varsayıyor). Bu bir **gerileme değil, paylaşılan
fikstür çakışmasıdır**. `TestOrtami.cs:26` zaten `KUYUMCU_TEST_DB` değişkenini okuyor; izole
veritabanında süit **3985/3985 yeşil**.

**Kararsız (flaky) tek kırmızı — kanıtla:** izole veritabanındaki ilk koşumda
`AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ` bir kez kırmızıydı
(`Bunit.Rendering.UnknownEventHandlerIdException: There is no event handler with ID '2' …`).
Yalıtılmış koşum **3/3 yeşil** (`Başarısız: 0, Başarılı: 6` × 3) ve ikinci tam süit koşumunda da
yeşil. bUnit'in bilinen "FindAll sonrası yeniden render" yarışı; P-41 hiçbir `.razor` dosyasına
dokunmadığı için **paketle ilgisizdir**. Açık kalem olarak §11'e yazıldı, sessizce geçilmedi.

### 6.3 Şartnamedeki test listesi — birebir

| Test | Sonuç |
|---|---|
| `YayinEnvanterTestleri.Envanter_TumWwwrootKokDosyalariniIcerir` | ✓ |
| `YayinEnvanterTestleri.Envanter_BootJsonVarliklariniIcerir` | ✓ |
| `YayinEnvanterTestleri.Envanter_KilavuzHtmlSatiriniIcerir` | ✓ |
| `yayin-butunluk-testleri.sh::test_tam_yayin_gecer` | ✓ |
| `…::test_eksik_dosya_reddedilir` | ✓ |
| `…::test_sifir_bayt_reddedilir` | ✓ |
| `…::test_sha_uyusmazligi_reddedilir` | ✓ |
| `…::test_kapi_eksik_dosyada_acilmaz_cikis14` | ✓ |
| `…::test_kapi_sifir_bayt_varlikta_acilmaz_cikis14` | ✓ |
| `…::test_envanter_zorunlu_anahtari_fail_closed_cikis15` | ✓ |
| `…::test_eski_cikis_kodlari_degismedi` | ✓ |

```
$ zsh betikler/testler/yayin-butunluk-testleri.sh
TOPLAM 8 · GEÇTİ 8 · KALDI 0     (rc=0)
```

---

## 7. Mutasyon kanıtları

Yöntem: kaynak `scratchpad/mut-yedek/P-41/` altına kopyalandı → `perl -0777 -pi -e` ile **tek
eşleşmeli** değişiklik → ilgili testler koşuldu → yedek geri kopyalandı → `cmp` ile **birebir
aynılık kanıtlandı**. Son mutasyondan sonra temiz `dotnet build` (0 uyarı / 0 hata).

| Kod | Değişiklik | Kırmızıya dönen test(ler) | Geri alındı (cmp) |
|---|---|---|---|
| **M-41-1** | `yayin-butunluk.sh --dogrula`: 0-bayt kontrolü `[ ! -s "$t" ]` → `[ ! -e "$t" ]` | `test_sifir_bayt_reddedilir` (`rc=6` ama `SIFIR BAYT:` satırı yok — arıza "boyut uyuşmazlığı"na dönüşüp kendi adını kaybediyor) | ✓ `cmp` birebir |
| **M-41-2** | `yayin-butunluk.sh --uret`: `wwwroot` kök taraması kaldırıldı | `Envanter_KilavuzHtmlSatiriniIcerir` **[FAIL]**, `Envanter_TumWwwrootKokDosyalariniIcerir` **[FAIL]** (`Başarısız: 2, Başarılı: 1`) · `test_eksik_dosya_reddedilir` + `test_tam_yayin_gecer` | ✓ `cmp` birebir |
| **M-41-3** | `yayin-butunluk.sh --dogrula`: SHA karşılaştırması kısa devre (`c="$s"`) | `test_sha_uyusmazligi_reddedilir` (`rc=0`, boyut 27→27 değişmedi → yalnız SHA yakalayabilirdi) | ✓ `cmp` birebir |
| **M-41-4** | `qa-sunucu.sh`: `--dogrula` çağrısı `\|\| true` ile yutuldu | `test_kapi_eksik_dosyada_acilmaz_cikis14` (`çıkış=0`, "KURU KOŞUM: tüm kapılar geçildi") · `test_kapi_sifir_bayt_varlikta_acilmaz_cikis14` | ✓ `cmp` birebir |
| **M-41-5** | `qa-sunucu.sh`: kapı `cd "$YAYIN"` satırından SONRAYA taşındı | `test_kapi_sifir_bayt_varlikta_acilmaz_cikis14` (`çıkış=0`) · `test_kapi_eksik_dosyada_acilmaz_cikis14` · `test_envanter_zorunlu_anahtari_fail_closed_cikis15` | ✓ `cmp` birebir |
| **M-41-6** | `qa-sunucu.sh`: `YAYIN-ENVANTER-ZORUNLU` kontrolü kaldırıldı | `test_envanter_zorunlu_anahtari_fail_closed_cikis15` (`anahtarlı çıkış=0`, beklenen 15) | ✓ `cmp` birebir |

**Test açığı bulunmadı** — altı mutasyonun altısı da en az bir testi kırmızıya çevirdi.
Kırılganlığın kaynağı bilinçli: testler yalnız çıkış kodunu değil **ihlal mesajının kendisini**
(`EKSİK DOSYA:` / `SIFIR BAYT:` / `SHA UYUŞMAZLIĞI:`) sınar; yoksa M-41-1 ve M-41-2 başka bir
kontrolün arkasına saklanıp yeşil kalırdı.

---

## 8. Denetim çıktısı

```
$ bash dotnet/denetim.sh
…
  ✓ P-30: DEMO_KISIT_AYRISMA   0 (giriş ekranı ile HARIC-LISTESI.md aynı H kümesi)

DENETİM TEMİZ.        (rc=0)
```

Tek ⚠ satırı (`P-30: DEMO_INSAN_SURESI (BK-45)` — 2026-09-30'a dek geçerli beyan) **önceden vardı**,
P-41 ile ilgisizdir ve `DENETİM TEMİZ` kararını değiştirmez.

---

## 9. Canlı doğrulama

**API açılmadı (5198 boş), test kiracısı oluşturulmadı** — paket veritabanına ve uçlara hiç dokunmuyor.
Canlı doğrulama **yayın hattı üzerinde** yapıldı. **5199 ve 5200'e dokunulmadı**; tüm kapı koşumları
`KUYUMCU_KURU_KOSUM=1` ile, ayrı bir hedef dizinde.

### 9.1 Uçtan uca yayın (gerçek `yayinla.sh`, gerçek `dotnet publish -c Release`)

```
$ KUYUMCU_YAYIN_DIZINI=…/scratchpad/p41-yayin zsh betikler/yayinla.sh "P-41 canli"
envanter üretildi: …/p41-yayin/.yayin-envanteri · 76 kayıt
bütünlük TEMİZ — 76 kayıt doğrulandı (…/p41-yayin)
  ağaç damgası: 4bb91a3cb586… → …/p41-yayin/.agac-damgasi
  envanter zorunluluğu AÇILDI → …/ham/YAYIN-ENVANTER-ZORUNLU
YAYIN HAZIR (P-41 canli) 15:48 · uyarı satırı: 0 · hata satırı: 0
  Kuyumcu.Api.dll Sep 23 15:47 6675968 bayt          (rc=0)

$ grep ' wwwroot/kilavuz.html ' …/p41-yayin/.yayin-envanteri
ef6f08d010e6287d2d0642783d0d056848c3ffdec94d7c8c33742f0204036c82 wwwroot/kilavuz.html 60885
```

**Envanter kapsamı:** 76 kayıt = 1 (`Kuyumcu.Api.dll`) + 6 (`wwwroot` kök) + 69 (`wwwroot/_framework/`).
69 = **66 boot varlığı** (60 assembly + 3 icu + 1 wasmNative + 2 jsModule — `blazor.boot.json`dan
ölçüldü) + `blazor.boot.json` + `dotnet.js` + `blazor.webassembly.js`.
Kabul eşiği **66 + 6 = 72 ≤ 76** ✓

### 9.2 Kapı tablosu (hepsi ölçüldü)

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | `yayinla.sh` sonrası envanter | `.yayin-envanteri` VAR, `wwwroot/kilavuz.html` satırı GEÇER | **VAR · geçti** (`… wwwroot/kilavuz.html 60885`) | §9.1 |
| 2 | Envanter varlık sayısı | ≥ 66 + kök dosya sayısı (=72) | **76** | §9.1 |
| 3 | `yayin-butunluk.sh --dogrula` | çıkış 0 | **0** — "bütünlük TEMİZ — 76 kayıt" | §9.1 |
| 4 | Tek dosya 0 baytlanınca | çıkış ≠ 0 + yol stdout'ta | **çıkış 6** · `SIFIR BAYT: wwwroot/_framework/Sahte.Derleme07.wasm` | `test_sifir_bayt_reddedilir` |
| 5 | Bütünlük kırmızıyken `.agac-damgasi` | mtime DEĞİŞMEZ | **mtime 1790167680 → 1790167680**, SHA de aynı; `yayinla rc=1`, "`.agac-damgasi` YAZILMADI" | aşağıdaki blok |
| 6 | Yayından `kilavuz.html` silinmiş + `KURU_KOSUM=1 qa-sunucu.sh` | **çıkış 14**, stdout'ta `wwwroot/kilavuz.html`, 5199 AÇILMAZ | **çıkış 14** · `EKSİK DOSYA: wwwroot/kilavuz.html` · "KURU KOŞUM: tüm kapılar geçildi" satırı **YOK** | `canli-eksik.log` |
| 7 | `_framework` varlıklarından biri 0 baytken | **çıkış 14** | **çıkış 14** · `SIFIR BAYT: wwwroot/_framework/Kuyumcu.Core.wasm` | `canli-sifir.log` |
| 8 | `YAYIN-ENVANTER-ZORUNLU` VARken envantersiz yayın | **çıkış 15** | **çıkış 15** · `HATA: YAYIN ENVANTERSİZ — …/.yayin-envanteri yok, oysa envanter zorunluluğu açık` | `canli-envantersiz.log` |
| 9 | Anahtar YOKken envantersiz yayın | yalnız UYARI + açılış | **çıkış 0** · `⚠ UYARI: yayın envanteri YOK …` + "KURU KOŞUM: tüm kapılar geçildi" | `canli-envantersiz-anahtarsiz.log` |
| 10 | Tam yayın + envanter | açılır | **çıkış 0** | `canli-tam.log` |
| 11 | Eski çıkış kodları (3/4/5/7/8/9/10/11/12/13) | DEĞİŞMEZ | **7, 8, 9 davranışla; 3/4/5/10/11/12/13 betikte tanımlı** | `test_eski_cikis_kodlari_degismedi` |

```
# 5 — bütünlük kırmızıyken damgaya DOKUNULMUYOR (kaynakta olup yayında olmayan dosya benzetimi)
$ KUYUMCU_YAYIN_DIZINI=…/p41-yayin KUYUMCU_KAYNAK_WWWROOT=…/sahte-kaynak-wwwroot zsh betikler/yayinla.sh
envanter üretildi: …/p41-yayin/.yayin-envanteri · 76 kayıt
YAYINDA OLMAYAN KAYNAK DOSYASI: wwwroot/ekstra-sayfa.html
BÜTÜNLÜK RET — yayın çıktısı eksik/bozuk (76 kayıt denetlendi).
YAYIN BÜTÜNLÜK RET — çıktı eksik/bozuk (ihlal eden yollar yukarıda).
  .agac-damgasi YAZILMADI (fail-closed, P-41).
yayinla rc=1 · damga mtime once=1790167680 sonra=1790167680 · değişti mi: HAYIR · SHA aynı mı: EVET
```

### 9.3 Bugünkü canlı 5199'un durumu (ölçüm, iddia değil)

```
$ ls …/qa-yayin/wwwroot/kilavuz.html
ls: … No such file or directory
$ curl -w '%{http_code} %{size_download}' http://localhost:5199/kilavuz        → 200 1605   (SPA geri düşüşü = index.html)
$ curl -w '%{http_code} %{size_download}' http://localhost:5199/kilavuz.html   → 404 0
$ ls -la …/p41-yayin/wwwroot/kilavuz.html                                      → 60885 bayt (yeni yayında VAR)
```

Yani **bugünkü 5199 anlık görüntüsü bu kapıdan GEÇEMEZ** (`EKSİK DOSYA: wwwroot/kilavuz.html`),
taze bir `yayinla.sh` koşumu ise geçer. Kurucunun ekranı kapatılmadı; **düzeltme P-42'nin kalıcı
yayın dizini ve orkestratörün yayın döngüsüne bırakıldı** (5199'a elle yama YASAK).

### 9.4 SQL kanıtı / kiracı

```
$ docker exec kuyumcu-mysql mysql -uroot -p"$P" -N kuyumcu_net -e "SELECT COUNT(*) FROM tenants;"
9        (başlangıçta da 9 — P-41 kiracı AÇMADI)
$ … -e "SELECT COUNT(*) FROM tenants WHERE ad LIKE 'PAKET%';"
0
$ lsof -nP -iTCP:5198 -sTCP:LISTEN → (boş)
$ lsof -nP -iTCP:5199 -sTCP:LISTEN → 12590 (kurucunun köprüsü, dokunulmadı)
```

---

## 10. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` **bu pakette anlamlı çıktı vermiyor**:
depoda tek commit var (`bbfdc95`) ve `dokumanlar/` ağacının TAMAMI izlenmiyor (`?? dokumanlar/`).
Bu yüzden dosyalar tek tek listeleniyor (ölçüm, varsayım değil):

| Dosya | Durum | Boyut |
|---|---|---|
| `dokumanlar/09-uygulama-kayitlari/betikler/yayin-butunluk.sh` | **YENİ** | 8.981 bayt |
| `dokumanlar/09-uygulama-kayitlari/betikler/testler/yayin-butunluk-testleri.sh` | **YENİ** | 14.070 bayt |
| `dokumanlar/09-uygulama-kayitlari/betikler/yayinla.sh` | değişti (bütünlük çağrısı + envanter anahtarı + `HEDEF` seamı) | 4.867 bayt |
| `dokumanlar/09-uygulama-kayitlari/betikler/qa-sunucu.sh` | değişti (çıkış 14/15 kapısı) | 18.552 bayt |
| `dokumanlar/09-uygulama-kayitlari/ham/YAYIN-ENVANTER-ZORUNLU` | **YENİ (üretilen)** | 133 bayt |
| `dotnet/Kuyumcu.Tests/YayinEnvanterTestleri.cs` | **YENİ** (`?? dotnet/Kuyumcu.Tests/YayinEnvanterTestleri.cs`) | 8.523 bayt |
| `dokumanlar/09-uygulama-kayitlari/PAKET-P-41-uygulama-raporu.md` | **YENİ** (bu dosya) | 24.534 bayt |

**Çakışma uyarısına uyuldu:** DALGA 7c'nin (`denetim.sh`, `OlcumKapisiTestleri.cs`, `/destek`,
`docs/`, `kurulum.sh`, yedek betikleri) ve PARA şeridinin (`KasaBakiyeKapisi.cs`, Kasa/Vezne/
Maliyet/Rapor modülleri, `Raporlar.razor`, `Gider.razor`) **hiçbir dosyasına dokunulmadı**.
`MIMARI-KALAN-PAKETLER-2026-09-15.md`, `TRIYAJ-TUR8-2026-09-23.md` ve `SONRAKI-OTURUM-PROMPT.md`
**değiştirilmedi**.

---

## 11. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| `yayinla.sh` sonrası `.yayin-envanteri` VAR + `wwwroot/kilavuz.html` satırı geçer | **KARŞILANDI** | §9.1 |
| Envanter varlık sayısı ≥ 66 + wwwroot kök sayısı | **KARŞILANDI** (76 ≥ 72) | §9.1 |
| `--dogrula` çıkış 0; tek dosya 0 baytlanınca çıkış ≠ 0 + yol stdout'ta | **KARŞILANDI** | §9.2/3-4 |
| Bütünlük kırmızıyken `.agac-damgasi` mtime değişmez | **KARŞILANDI** | §9.2/5 |
| kilavuz.html silinmişken `KURU_KOSUM` → çıkış 14, yol stdout'ta, 5199 açılmaz | **KARŞILANDI** | §9.2/6 |
| `_framework` varlığı 0 baytken → çıkış 14 | **KARŞILANDI** | §9.2/7 |
| `YAYIN-ENVANTER-ZORUNLU` varken envantersiz → 15; yokken uyarı + açılış | **KARŞILANDI** | §9.2/8-9 |
| Mevcut çıkış kodları (3/4/5/7/8/9/10/11/12/13) değişmez | **KARŞILANDI** | §9.2/11 |
| KURU_KOŞUM yolunda da kapı koşar | **KARŞILANDI** (kapı `cd "$YAYIN"` ve KURU çıkışından ÖNCE; M-41-5 bunu ölçüyor) | §7 |
| Test listesinin tamamı yeşil | **KARŞILANDI** (11/11) | §6.3 |
| Mutasyon listesinin tamamı kırmızıya dönüyor | **KARŞILANDI** (6/6, test açığı yok) | §7 |
| `dotnet build` 0 hata / 0 uyarı | **KARŞILANDI** | §5 |
| Tam süit 0 başarısız | **KARŞILANDI** (3985/3985, izole test DB'sinde) | §6.2 |
| `denetim.sh` TEMİZ | **KARŞILANDI** | §8 |
| 5199'a dokunulmadı, yayına elle yama yok | **KARŞILANDI** | §9.4 |
| Test kiracısı silindi / kiracı sayısı korundu | **KARŞILANDI** (kiracı hiç açılmadı; 9 → 9) | §9.4 |
| COMMIT YOK | **KARŞILANDI** | — |

---

## 12. Açık kalanlar

1. **Bugünkü 5199 anlık görüntüsü hâlâ eksik dosya servis ediyor** (`wwwroot/kilavuz.html` yok).
   P-41 kapıyı kurdu ama **yayını düzeltmedi** — düzeltme `preview_stop → yayinla.sh → preview_start`
   döngüsüyle yapılır ve **kurucunun ekranını durdurmak orkestratör/kurucu kalemidir**.
   Kapı kurulduktan sonraki **ilk yayın koşumunda kilavuz.html kendiliğinden gelir** (ölçüldü: taze
   publish onu içeriyor, §9.3).
2. **`/kilavuz` rotasının 200/1605 dönmesi (SPA geri düşüşü) ayrı bir kalemdir** — statik dosya mı
   Blazor sayfası mı servis edilmeli, bu pakette karara bağlanmadı. **P-43 (canlı duman kapısı)**
   tam bunu ölçmeli; aksi hâlde "200 döndü" yanıltıcı yeşil üretmeye devam eder.
3. **23/09 12:05'te `_framework`e kimin yazdığı hâlâ DOĞRULANAMADI** (o güne ait publish günlüğü yok).
   Kapı artık böyle bir dokunuşu **yakalar** (SHA uyuşmazlığı), ama geçmişteki faili bulmaz.
4. **`yayinla.sh`'teki `KUYUMCU_YAYIN_DIZINI` seamı P-42'ye devredilmelidir** — P-42 "yayın dizinini
   kalıcılaştır + tek kaynak yol" işini yaparken bu satırı tek kaynağa bağlamalı, ikinci bir yol
   tanımı açmamalı.
5. **`AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ` kararsız** (bUnit
   `UnknownEventHandlerIdException`). Yalıtılmış 3/3 yeşil, tam süitte 1/2 koşumda kırmızı.
   P-41 ile ilgisiz; **ayrı kalem olarak açık bırakıldı**, sessizce geçilmedi.
6. **Paylaşılan `kuyumcu_test` veritabanı, paralel şeritlerde süit ölçümünü GEÇERSİZ kılıyor**
   (ölçüldü: 1073 sahte başarısız). `TestOrtami.cs` zaten `KUYUMCU_TEST_DB` okuyor —
   **orkestratör her şerit için ayrı test veritabanı adı vermelidir**, yoksa her paketin süit
   rakamı şüpheli kalır. Bu, P-41'in ölçtüğü ikinci ölçüm-aracı arızasıdır.
7. **`kuyumcu_test_p41` veritabanı diskte duruyor** (bu koşum için oluşturuldu). Silinmesi
   kurucuya/orkestratöre bırakıldı — veritabanı düşürme geri dönülemez işlemdir.
8. **Geçici fikstür dizinleri** `scratchpad/p41-yayin*`, `scratchpad/deneme-yayin`,
   `scratchpad/sahte-kaynak-wwwroot`, `/var/folders/.../p41-testleri.*` duruyor; silme kurucuya bırakıldı.

## 13. Kurucuya / mali müşavire notlar

- **Mali müşavire not YOK** — paket mevzuat hükmü içermiyor.
- **Kurucuya:** (a) 5199'un yayını **hâlâ eksik**; kapı artık bunu görüyor ama ekranı durdurup
  yeniden yayınlamak izin gerektiriyor — sıra P-42'de. (b) `YAYIN-ENVANTER-ZORUNLU` anahtarı
  **AÇILDI**; bundan sonra envantersiz bir anlık görüntüyle 5199 açılmaz (çıkış 15). Bu bilinçli
  bir tek yönlü kapıdır; kapatılması kurucu kalemidir. (c) Tur 8 ortalamaları (3,17/2,74)
  **hâlâ karşılaştırılabilir değil** — P-41 aracın bir parçasını onardı, geçerli başlangıç noktası
  ancak P-41→P-43 tamamlandıktan sonraki **tur 9** ile bilinir.
