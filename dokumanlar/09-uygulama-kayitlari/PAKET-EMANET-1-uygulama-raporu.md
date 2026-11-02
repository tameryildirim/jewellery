# PAKET EMANET-1: Emanet ekranında önce zorunlu olanlar (uygulama raporu)

- **Dalga:** T8 (EMANET-1 + DUGME-2)
- **Rol:** Taşkın (backend) ve Ertuğ (frontend); uygulayıcı Opus
- **Tarih:** 2026-09-25 (son güncelleme 2026-09-30 06:50)
- **İzolasyon:** test veritabanı `kuyumcu_test_t9` · canlı API 5188 · oturum vekili 5189 · CDP portları 9981–9997
- **Durum:** BITTI (30.09 06:50 on üçüncü devralma turu § 0000000000000: orkestratörün KRİTİK güvenlik gerilemesi kapatıldı. Bir sekme depodaki jeton çiftini yalnız aynı kullanıcı ve aynı kiracıya aitse benimsiyor; başka kullanıcıda istek göndermeden, nedenini söyleyerek /giris'e düşüyor. 9 mutasyon kırmızı, canlıda Veli jetonlu istek 0, tam süit 7714/7713 (tek kırmızı KATMAN2), denetim.sh TEMİZ. Önceki: 30.09 05:40 on ikinci devralma turu § 000000000000: keşif B006 iki sekme jeton yarışı istemcide sekmeler arası tek uçuşla kapatıldı, sunucu rotasyonu ve çalıntı tespiti korundu; 3 mutasyon kırmızı, canlı 2 ve 3 sekme 0 düşme, gerçek yeniden kullanım aileyi iptal ediyor. Önceki: 28.09 21:55 on birinci devralma turu § 00000000000: tasarım hakeminin iki KRİTİK bulgusu (tablo altı değiştirilemez kutuları; iade/ayrıntı çekmecesi teknik satırları) ve DÜŞÜK has 4 hane/gram boş bulgusu kapatıldı; filtreli 258/258, tam süit 6801/6803 (2 kırmızı SATIS-3 ve ORTAK-1), denetim.sh TEMİZ, 7 mutasyon kırmızı + cmp AYNI, canlı HTTP+SQL, tur.mjs 16/16 K = 0, tutarlılık farkı 0, 84 durum + 36 çekmece içi ölçüm, 13 görüntü görüldü; test kiracısı silindi.)

## 0000000000000. On üçüncü devralma turu (30.09 05:55–06:50, t9 · API 5188 · vekil 5189 · CDP 9993–9997): B006 güvenlik gerilemesi

Emanet ekranına (razor/css/sözlük) bu turda da **dokunulmadı**. Bu tur yalnız orkestratörün 05:52 KRİTİK bulgusunu kapattı: § 000000000000'daki B006 düzeltmesi, depodaki çift kime ait olursa olsun benimsiyordu.

### 0000000000000.1 Kök neden (koda karşı doğrulandı)

`OturumServisi.BaskaSekmeninCiftiniBenimseAsync` yalnız `depoRefresh != RefreshToken` koşuluna bakıyordu. Ortak bilgisayar senaryosu şöyle:
1. Ali'nin iki sekmesi açık. Sekme 1'de Ali çıkıyor, Veli giriyor.
2. Sekme 2'de Ali'nin erişim jetonu doluyor ve 401 geliyor.
3. Sekme 2 Veli'nin çiftini benimsiyor.

Sonuç: ekranda "Ali, Tezgâhtar" görünüyordu ama bütün istekler Veli (Patron) kimliğiyle gidiyordu. Doğrulayıcının geçici testi bunu kırmızıyla kanıtlamıştı.

### 0000000000000.2 Düzeltme

- **Kimlik jetonun kendisinden okunuyor** (`OturumServisi.cs`, yeni `JetonKimligi`): bellekteki erişim jetonunun `sub` ve `tenantId` iddiaları depodakiyle **birebir** eşleşmeli. İddia adları `TokenServisi.AccessUret` ile aynı. Karşılaştırma isimle değil kimlikle yapılıyor. Bu imza doğrulaması değildir; yetki kararını sunucu her istekte imzayla veriyor.
- **Fail-closed:** jetonlardan biri okunamazsa eşleşme yok sayılıyor ve çift benimsenmiyor.
- **Sonuç üç durumlu** (`CiftBenimseme`):
  - `Yok`: depoda farklı çift yok, sekme sunucuda yeniler.
  - `Benimsendi`: aynı kişi. Profilin rolü ve şubesi yeni jetondan yenileniyor.
  - `BaskaKullanici`: aşağıdaki gibi ele alınıyor.
- **Başka kullanıcı** (`ApiIstemci.YenileAsync` → `OturumuDusurAsync`):
  - Sunucuya **hiçbir istek gitmiyor**. Ne Veli'nin jetonu kullanılıyor ne de Ali'nin jetonuyla yenileme ya da çıkış deneniyor.
  - Oturum **yalnız bellekte** temizleniyor (`BaskaKullaniciYuzundenBirak`). localStorage'a dokunulmuyor, çünkü o çift Veli'nin açık oturumu. `CikisAsync` çağrılsaydı Veli de düşerdi.
  - Sekme /giris'e gidiyor. Giriş ekranı nedeni HataBandi'nda yazıyor: "Bu tarayıcıda başka bir kullanıcı oturum açtı — yeniden giriş yapın." (`GirisSozlugu.BaskaKullaniciOturumActi`, TR+EN).
- **Rol/şube yenileme:** sunucuda yenileyen sekmede de (`TokenKaydetAsync`, `sub == Kullanici.Id` ise) profil yeni jetondan güncelleniyor. İlk canlı koşumda 3 sekmeden sunucuda yenileyen sekme eski rolü (Müdür) gösteriyordu, benimseyen iki sekme yeni rolü (Tezgâhtar). Bu tutarsızlık bu değişiklikle kapandı. Jetonda ad alanı yok. `sub` aynı olduğu için kişi aynı; ad değişikliği bir sonraki `/auth/ben` yüklemesinde gelir.
- **Düşmüş sekmeden yeniden giriş:** düşmüş bir sekmede `oturumDusuruldu` bayrağı önceden bütün istekleri gönderilmeden kesiyordu, `/auth/giris` dahil. "Yeniden giriş yapın" diyen ekrandan giriş yapılamazdı. Artık giriş öncesi anonim uçlar (`/auth/giris`, `/auth/kayit`, `/genel/kisitlar`) gidiyor. Yeni oturum açılınca bayrak kalkıyor. Korumalı uçlar düşmüş sekmede gönderilmeden kesilmeye devam ediyor (UX4d M15 testleri yeşil).
- **Bildirim tüketilmiyor:** canlıda giriş ekranı iki kez çizildi (kabuk ve istemci ayrı ayrı yönlendiriyor). İlk sürüm bildirimi okurken silince ikinci çizimde bant boş kaldı; bu t41 ilk koşumunda görüldü (görüntü `t41/olc/g3` ilk sürüm). Bildirim artık yeni oturum açılana kadar duruyor (`TokenKaydetAsync` siliyor).

Değişen dosyalar:
- `Kuyumcu.Web/Servisler/OturumServisi.cs`
- `Kuyumcu.Web/Servisler/ApiIstemci.cs`
- `Kuyumcu.Web/Pages/Giris.razor`: yalnız `OnInitialized`, 5 satır.
- `Kuyumcu.Core/I18n/GirisSozlugu.cs`: 1 metin, TR+EN.
- `Kuyumcu.Tests/SekmeJetonYarisiTestleri.cs`

Sunucu kuralı (rotasyon + yeniden kullanımda aile iptali) **değişmedi**.

**Tek dokunuş gerekçesi:** `Giris.razor` ve `GirisSozlugu.cs` başka bir şeridin (S1-e) dosyası. Güvenlik kabulü "mesajla /giris'e yönlendirir, sessiz değil" istediği için bu değişiklik zorunluydu. Değişiklik yalnız ekleme: mevcut id'ler, düzen ve metinler değişmedi.

### 0000000000000.3 Testler (yeni: 4 metot, 6 durum)

| Test | Ne sınar |
|---|---|
| `B006_Guvenlik_baska_kullanicinin_ciftini_benimsemez_onun_jetonuyla_istek_gondermez_nedeniyle_duser` (aliCikti, baskaKiraci) × 3 | (a) Ali çıktı, Veli girdi. (b) Ali çıkmadan Veli girdi, Ali'nin ailesi hâlâ geçerli. (c) Kullanıcı kimliği aynı ama **başka kiracı**. Üçünde de: Veli jetonlu istek **0**, yenileme 0, aile iptali yok, sekme düşer, `Kullanici` null, `/giris`, bildirim kodu dolu, sonraki istek de gönderilmez. Veli'nin depodaki çifti **silinmez** ve Veli'nin sekmesi çalışır |
| `B006_Guvenlik_ayni_kullanici_benimseyince_rol_ve_sube_yeni_jetondan_yenilenir` | Başka yerde rol ve şube değişti. Yenileyen sekmede de benimseyen sekmede de rol MUDUR ve şube sube-2. Tek yenileme, ad korunur, düşme yok |
| `B006_Guvenlik_okunamayan_jeton_fail_closed_benimsenmez` | Depoda kimliği okunamayan çift var: benimsenmez, gönderilmez, düşer |
| `SekmeJetonGuvenlikEkranTestleri.B006_Guvenlik_baska_kullanici_yuzunden_dusen_sekme_giriste_nedeni_gorur_ve_yeniden_girebilir` (bUnit) | Veli jetonlu ve `/auth/*` istek yok, `localStorage.removeItem` çağrısı yok. Giris iki kez çizilir, ikisi de mesajı gösterir. Aynı sekmeden `/auth/giris` gider, yeni oturumla korumalı istek geçer. Yeni oturumda bildirim kalkar |

Mevcut B006 testleri (2 ve 3 sekme, çıkış, sahte sunucu kuralı) opak `access-0` yerine JWT biçimli jetonla koşuyor. Sahte sunucu artık aileyi kişiye bağlıyor ve korumalı isteklerin jetonunu kaydediyor.

- Gerçek ağaç, `dotnet build Kuyumcu.sln`: **0 Hata**. Tek uyarı MSB3026, yani eşzamanlı şeridin dll kopyalama yarışı. Kod uyarısı değil. Kopya ağaçta 0 Uyarı 0 Hata.
- Filtreli koşumlar:
  - SekmeJeton + UX4dOturum + S1eGiris: **41/41**.
  - Bunlara AuthTestleri, Oturum, P07Dayaniklilik ve CikisGuvenlik eklenince: **118/118**.

### 0000000000000.4 Mutasyonlar

Yöntem: anlık kopya ağaç `/private/tmp/kuyumcu-serit/EMANET-1/t41/agac`. Her mutasyonda `perl -0777` ile tek eşleşme → build 0 Hata → filtreli 41 test → yedeği geri kopyala → `cmp`. Yedekler `/private/tmp/kuyumcu-serit/mut-yedek/EMANET-1/t41/`, betikler `t41/mut41.sh`, `mut41b.sh`, `mut41c.sh`.

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| MG1 | Kimlik denetimi kaldırıldı (`if (false)`) | Güvenlik theory (F,F), (T,F), (T,T) + fail-closed + ekran testi | AYNI |
| MG2 | Kiracı karşılaştırması kaldırıldı | Güvenlik theory (aliCikti: True, baskaKiraci: True) | AYNI |
| MG3 | Benimsemede rol/şube yenileme kaldırıldı | `…ayni_kullanici_benimseyince_rol_ve_sube…` | AYNI |
| MG4 | Başka kullanıcıda yerel bırakma yerine `CikisAsync` (depoyu siler) | Güvenlik theory × 3 + ekran testi | AYNI |
| MG5 | Düşmüş sekmede anonim uçlar da kesildi | Ekran testi (yeniden giriş) | AYNI |
| MG6 | Giriş ekranında mesaj gösterimi kaldırıldı | Ekran testi | AYNI |
| MG7 | Fail-closed kaldırıldı (okunamayan jeton benimsenir) | `…okunamayan_jeton_fail_closed_benimsenmez` | AYNI |
| MG9 | Yeni oturumda bildirim temizliği kaldırıldı | Ekran testi | AYNI |
| MG10 | Sunucuda yenilemede profil rol/şube güncellemesi kaldırıldı | `…ayni_kullanici_benimseyince_rol_ve_sube…` | AYNI |

MG1, MG2, MG4, MG5 ve MG7 bildirim düzeltmesinden önceki sürümde koşuldu. Mutasyona uğrayan satırlar sonradan değişmedi. MG3 son sürümde yeniden koşuldu. Son mutasyondan sonra kopya ağaçta temiz build: 0 Uyarı 0 Hata. Beş dosya gerçek ağaçla `cmp` AYNI.

### 0000000000000.5 Canlı doğrulama (API 5188 kopya ağaçtan `--no-build`, `Jwt__AccessDakika=1`)

Ortam:
- Başsız Chrome, aynı profil, CDP. Tarayıcı bölmesi kullanılmadı.
- Oturumlar `inceleme-vekili.py` ile parolasız açıldı. Vekil fikstürü A ile B arasında değiştirildi.
- `Sistem__Anahtari` bu tur için rastgele üretildi, ortam değişkeninden okundu, basılmadı. Kullanımdan sonra dosyası boşaltıldı.
- Kiracılar: `PAKETEMANET-1-TEST T41` (senaryo 1 ve 2) ve `T41B` (ölçüm). Her birinde Ali (SAHIP, kayıt) ve Veli (MUDUR, `/kullanicilar` 201).
- İstek kimliği sekme 2'nin CDP Network olaylarından alındı: her API isteğinin `Authorization` jetonunun `sub`'ı çözüldü.

**Senaryo 1 — başka kullanıcı** (`t41/guvenlik41.mjs`, çıktı `guvenlik41b.out`):

| Adım | Sonuç |
|---|---|
| Açılış | Sekme 1 ve 2 `/emanet`, kart "Ali S." |
| Sekme 1'de Ali gerçek `#kenar-cikis` düğmesiyle çıktı | Sekme 1 `/giris`, depo boş |
| Sekme 1'de Veli girdi | Sekme 1 `/emanet`, "Veli M." |
| 100 sn sonra sekme 2 gezinme yaptı | Sekme 2 **`/giris`**, bant: "Bu tarayıcıda başka bir kullanıcı oturum açtı — yeniden giriş yapın." |
| Sekme 2'nin istekleri (Veli girişinden sonra) | `ALI /bildirimler/sayi`, `ALI /kur/guncel` (401), `JETONSUZ /genel/kisitlar` ×2. **Veli jetonlu istek 0**, `/auth/yenile` 0 |
| Sekme 1 (Veli) `/cariler` | Çalışıyor, "Veli M. · Müdür". Depo yerinde |
| Sekme 2'de giriş formundan yeniden giriş (Ali) | `JETONSUZ /auth/giris` → `/`, "Ali S.". Sonraki bütün istekler `ALI` |

İlk koşumda (`guvenlik41.out`) istek tarafı aynıydı: Veli jetonlu istek 0. Ancak giriş ekranındaki bant boştu ("sessiz"). Bu, § .2'deki "bildirim tüketilmiyor" düzeltmesine götürdü ve ikinci koşum geçti.

**Senaryo 2 — aynı kullanıcı, 3 sekme, rol değişimi** (`t41/ayni41.mjs`, çıktı `ayni41b.out`):
- Veli (Müdür) üç sekmede açık. Ali API'den `PATCH /kullanicilar/{Veli}/rol {TEZGAHTAR}` → **200**.
- 100 sn sonra üç sekme aynı anda gezinme yaptı.
- Sonuç: üçü de `/`'da, **üçü de "Tezgâhtar"**, düşme 0.
- Ağ: tek `sekme3 200 /auth/yenile`, `/auth/cikis` 0. API günlüğünde bu koşumda tek yenileme RequestId'si var.
- Yeni rol yüzünden paneldeki "Bu işlem için gerekli rol" bantları sunucunun doğru 403'ü. Ekranın sorunu, aşağıda açık kalanlarda.

SQL (`refresh_tokenler`, T41 sonunda):

| Kişi | iptal_edildi=0 | iptal_edildi=1 |
|---|---|---|
| ALI | 7 | 2 |
| VELI | 5 | 5 |

İki ailede de etkin jeton kaldı, yani aile iptali **yok**. `kullanicilar.rol` (Veli) = TEZGAHTAR.

**Gerçek tarayıcı ölçümü — değişen ekran /giris (bildirim bandıyla):** `t41/olc41.mjs`, kiracı T41B. Aynı senaryo, düşmüş sekme 2'de `sikisma-olc.js` `__olcTam` ölçümü:

| Genişlik | Tema | K1 | K3 | K5 | K6 | K7 | K8 | K9 | K10 | K11 | K12 | Yatay | Bant |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1280 | aydınlık | 0 | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 440 px, sağ 525 |
| 1280 | koyu | 0 | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 440 px |
| 375 | aydınlık | 0 | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 343 px, sağ 359 < 375 |
| 375 | koyu | 0 | 0/0 | 0 | 0/0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 343 px |

Açıklamalar:
- K2 `null`: tek sütunlu form.
- K4: 1280'de 1×2, 375'te 1,3×2 (tek form, "tren" yok).
- BASİT/PROFESYONEL ayrımı giriş ekranında yok, çünkü oturum öncesi.

Ekran görüntüleri Read ile **görüldü**. Kopyaları `ham/TUVAL-KARSILASTIRMA/EMANET-1-t41-*.png` altında:
- `g1-sekme2-ali`: Ali'nin sekmesi, Emanet.
- `g3-sekme2-dustu`: bantlı giriş, koyu 1280.
- `g4-sekme1-veli-calisiyor`: Veli, Müşteriler.
- `a3-veli-sekme`: yeni rol "Tezgâhtar", menü 27 ekran.
- `giris-bildirim-1280-aydinlik`, `-1280-karanlik`, `-375-aydinlik`, `-375-karanlik`: bant formun üstünde, sığıyor, kırık yok.

### 0000000000000.6 Temizlik

- API ve vekil `kill` ile durduruldu. `lsof` 5188/5189 **boş**. Chrome profilleri (`profil-guvenlik*`, `profil-ayni*`, `profil-olc`) süreci **0**. 5199/5200'e dokunulmadı.
- Kiracılar `kiraci-temizle.sh` ile **zsh**'de silindi:
  - T41: 301 satır + tenants.
  - T41B: aynı yolla.
- Sonrasında information_schema'daki 70 `tenant_id` tablosunda iki kiracı kimliğiyle **0** satır kaldı. `tenants` `PAKETEMANET%` **0**.
- `tenants`: başlangıç 12, T41 silindikten sonra 15, son **14**. Artış eşzamanlı şeritlerin kendi kiracıları: `temizle.log` listesinde PAKETQ10-4, T-S2, ZO-BELGE, ORTAK-1, KATMAN2. QA-TEST kiracılarına dokunulmadı.
- `anahtar`, `fikstur*.json`, `jeton`, `durum41.json` `printf '' >` ile boşaltıldı.
- ⚠ **Proje ağacına artık kaldı (bu tur, benim hatam):** ekran görüntülerini `ham/TUVAL-KARSILASTIRMA/`'ya kopyalarken önce düz adlı kopyalar oluştu. Bunlar `printf ''` ile **0 bayta** indirildi: `a3-veli-sekme.png`, `g1-sekme2-ali.png`, `g3-sekme2-dustu.png`, `g4-sekme1-veli-calisiyor.png`, `giris-bildirim-{1280,375}-{aydinlik,karanlik}.png` (8 dosya). Dosya silme yasak olduğu için silme kurucuya bırakıldı. Asıl kopyalar `EMANET-1-t41-*.png` adlı dosyalar.

### 0000000000000.7 Şerit kapısı

1. Build: 0 Hata. Tek uyarı MSB3026, eşzamanlı kopya yarışı; kopya ağaçta 0/0.
2. Bu turun testleri 0 başarısız: filtreli 41/41 ve 118/118.
3. denetim.sh (gerçek ağaç, 06:35): **DENETİM TEMİZ**.
4. Mutasyon: 9/9 kırmızı, cmp AYNI.
5. Canlı: iki kullanıcı ve aynı kullanıcı senaryoları HTTP + CDP ağ + SQL ile kanıtlandı. Değişen /giris ekranı 1280/375 × aydınlık/koyu ölçüldü.
6. Kiracılar silindi (0 satır).
7. Tam süit (gerçek ağaç, trx `/private/tmp/kuyumcu-serit/trx-pEMANET-1-t41`): **7714 test, 7713 geçti, 1 başarısız**. Tek kırmızı `Dogrulayici.KATMAN2SinirTestleri.Asenkron_tasimada_ikinci_tiklama_yazilir_URETIM_benzeri`: KATMAN şeridinin testi, önceki turda da kırmızı ya da kararsız listelenmişti. Bu paketin ürettiği yeni kırmızı: **0**.

### 0000000000000.8 DoD sözleşmesi

Yeni varlık yok, migration yok, sunucu ucu yok. `OturumServisi.GirisBildirimi` bellek içi bir durum; kalıcı değil ve düzenlenecek bir kayıt değil. Mevcut id'ler değişmedi. Yeni metin sözlükte (GirisSozlugu, TR+EN).

### 0000000000000.9 Açık kalanlar (sahibiyle)

- **Kurucu:** 0 baytlık artıklar silinmeli:
  - `ham/TUVAL-KARSILASTIRMA/` altındaki 8 dosya (§ .6, bu tur).
  - `dotnet/mb-MB1.log`, `mb-MB2.log`, `mb-MB3.log` (önceki tur).
  - `dotnet/denetim.sh.mutbak` (bu pakete ait değil).
- **Panel sahibi (ORTAK-1):** TEZGAHTAR rolüyle Panel'de "Bugün Özeti" ve "Bekleyen işler" kutularında "Bu işlem için gerekli rol: SAHIP veya MUDUR veya MUHASEBE" kırmızı bandı çıkıyor (`a3-veli-sekme.png`). Yetkisiz kutu ya gizlenmeli ya da sade bir "bu kutu yöneticilere açık" bilgisi göstermeli.
- **KATMAN:** `KATMAN2SinirTestleri.Asenkron_tasimada_ikinci_tiklama_yazilir_URETIM_benzeri` kırmızı.
- **Mehmet (güvenlik), bilgi için:** istemci kimliği JWT yükünden okuyor ve imzayı doğrulamıyor. Bu doğru, çünkü amaç "iki jeton aynı kişinin mi" karşılaştırması; yetkiyi her istekte sunucu imzayla veriyor. Depoya sahte jeton yazabilen bir saldırgan zaten aynı kökende kod çalıştırıyor demektir.
- **Orkestratör:** `kiraci-temizle.sh` bash denetimi önerisi (önceki tur) duruyor. Bu turda betik zsh ile çağrıldı ve doğru çalıştı.

## 000000000000. On ikinci devralma turu (30.09 04:50–05:40, t9 · API 5188 · vekil 5189 · CDP 9991–9992): keşif B006, iki sekmede jeton yarışı

Ağaçtaki Emanet işi korundu; bu turda Emanet ekranına (razor/css/sözlük) **dokunulmadı**. Orkestratörün KRİTİK saydığı keşif bulgusu B006 kapatıldı: aynı kullanıcı iki sekmede çalışınca yenileme jetonu yarışı bütün oturumları düşürüyordu.

### 000000000000.1 Kök neden (koda karşı doğrulandı)

- `Kuyumcu.Api/Modules/Auth/AuthModulu.cs` `Yenile`: iptal edilmiş jeton yeniden gelirse `TumTokenlerIptalEt` çağrılıyor ve kullanıcının bütün jetonları düşüyor. Bu davranış **doğru** ve korunuyor.
- `Kuyumcu.Web/Servisler/OturumServisi.cs` `YukleAsync`: jetonlar yalnız açılışta localStorage'dan okunup bellekte tutuluyordu.
- `Kuyumcu.Web/Servisler/ApiIstemci.cs` `TekUcuslaYenileAsync`: tek uçuş yalnız **aynı sekmenin** isteklerini birleştiriyordu. İki sekme aynı eski jetonla ayrı ayrı `/auth/yenile` çağırıyor, ikinci çağrı yeniden kullanım sayılıyor ve bütün aile iptal ediliyordu.

### 000000000000.2 Seçilen çözüm: istemcide sekmeler arası tek uçuş; sunucu gevşetilmedi

Yenileme artık tarayıcı genelinde **tek kilit** altında yapılıyor (`navigator.locks`; yoksa localStorage kilidi, en çok 15 sn). Kilidi alan sekme jetonu depodan **taze** okur:
- Depodaki yenileme jetonu bellektekinden farklıysa başka bir sekme zaten yenilemiş demektir. Sekme o çifti benimser ve sunucuya **hiç gitmez**.
- Aynıysa sunucuda yeniler. Yeni çift depoya yazılır ve kilit bırakılır.
- Depo boşsa (başka sekme çıkış yaptıysa) bellekteki jetonla dener. Sunucu 401 döner, sekme düşer. Bu doğru davranış.

Sunucu tarafına tolerans penceresi (grace) **eklenmedi**. Gerekçe: pencere içinde çalınan jeton da geçerli bir çift alırdı, yani güvenlik gevşerdi. İstemci çözümü sorunu kökten kapatıyor. Birincil kaynaklar:
- RFC 6749 §10.4 (https://www.rfc-editor.org/rfc/rfc6749.txt): "one of them will present an invalidated refresh token, which will inform the authorization server of the breach."
- RFC 9700 (OAuth 2.0 Security BCP) §4.14.2 (https://www.rfc-editor.org/rfc/rfc9700.txt): rotasyonda eski jeton geçersizleşir; geçersiz jeton gelince sunucu "will revoke the active refresh token". Sunucumuz bunu yapıyor ve yapmaya devam ediyor.

Değişen/yeni dosyalar:
- `Kuyumcu.Web/wwwroot/js/oturum-kilidi.js` (yeni): `window.kuyumcuOturumKilidi.al/birak`. Kendi kökümüzden, eval yok, CSP istisnası yok.
- `Kuyumcu.Web/wwwroot/index.html`: betik Blazor'dan önce yükleniyor.
- `Kuyumcu.Web/Servisler/OturumServisi.cs`: `SekmeKilidiAlAsync`, `SekmeKilidiBirakAsync`, `BaskaSekmeninCiftiniBenimseAsync`. JS yoksa (test ortamı, eski tarayıcı) kilitsiz sürer. Bu, eski davranıştan kötü olamaz.
- `Kuyumcu.Web/Servisler/ApiIstemci.cs`: `YenileAsync` = kilit → benimse ya da `SunucudaYenileAsync` → `finally` içinde bırak. Sekme içi tek uçuş aynen duruyor.
- `dotnet/denetim.sh` ve `Kuyumcu.Tests/TasarimBelirtecTestleri.cs`: index.html betik sayısı 9 → 10, KATMAN-1 örneğindeki gibi yazılı gerekçeyle.
- `Kuyumcu.Tests/SekmeJetonYarisiTestleri.cs` (yeni, 5 test).

### 000000000000.3 Testler

| Test | Ne sınar |
|---|---|
| `B006_Cok_sekme_ayni_anda_sure_dolumunda_hicbiri_dusmez(2)` ve `(3)` | 2 ve 3 sekme, her biri ayrı DI kapsamı (ayrı bellek). Ortak olan yalnız localStorage ve tarayıcı kilidi. Sahte sunucu gerçek kuralı birebir uyguluyor (rotasyon + yeniden kullanımda aile iptali). Bütün sekmeler 200 alıyor, aile iptal edilmiyor, **1** yenileme, **0** çıkış, hiçbir sekme `/giris`'e düşmüyor |
| `B006_Baska_sekme_cikis_yaptiysa_bu_sekme_de_duser_ve_yeniden_kullanim_iptali_korunur` | Çıkış öteki sekmede de oturumu kapatıyor |
| `B006_Sahte_sunucu_gercek_sunucu_kuralini_uygular_iptal_jetonu_aileyi_duser` | Test sunucusunun kuralı gevşek değil |
| `B006_index_kilit_betigini_Blazordan_ONCE_yukler_ve_betik_navigator_locks_kullanir` | Betik bağlantısı ve kilit API'si |
| Mevcut `AuthTestleri.Iptal_edilmis_token_yeniden_kullanilirsa_TUM_oturumlar_kapanir` | Gerçek sunucuda (HTTP + DB) yeniden kullanım → bütün aile iptal. Değiştirilmedi, yeşil |

- Gerçek ağaç, `dotnet build Kuyumcu.sln`: **0 Uyarı 0 Hata**.
- Filtreli koşum (Auth, Oturum, Token, UX4dOturum, CikisGuvenlik, P07Dayaniklilik, SekmeJeton, bütün Emanet/EMANET1/P37c/MigrationHijyeni/OlcumKapisi/UX4c/KilitGerekce/UX1C2/S35KurVitrin): **360/360**.
- Betik sayısı düzeltmesinden sonra SekmeJeton + TasarimBelirtec + KatmanTekKalip + MigrationHazirlik: **66/66**.

### 000000000000.4 Mutasyonlar

Yöntem: anlık kopya ağaç `/private/tmp/kuyumcu-serit/EMANET-1/t40/agac`. Gerçek ağaçta eşzamanlı şeritlerin build'i dll kilidi ve yarım dosya hatası verdi; ilk MB1 denemesi bu yüzden eski ikiliyle koştu ve geçersiz sayıldı. Her mutasyonda `perl -0777` ile tek eşleşme → build 0 Hata → filtreli test → yedeği geri kopyala → `cmp`. Yedekler `/private/tmp/kuyumcu-serit/mut-yedek/EMANET-1/t40/`, betik `t40/mut40.sh`, çıktı `t40/mut40c.out`.

| Kod | Değişiklik | Kırmızıya dönen test | cmp |
|---|---|---|---|
| MB1 | Sekmeler arası kilit kaldırıldı (`kilitKimligi = null`) | `B006_Cok_sekme_…(2)`, `(3)` | AYNI |
| MB2 | Başka sekmenin çiftini benimseme kaldırıldı | `B006_Cok_sekme_…(2)`, `(3)` | AYNI |
| MB3 | Sunucuda yeniden kullanım tespiti (`TumTokenlerIptalEt`) kaldırıldı | `AuthTestleri.Iptal_edilmis_token_yeniden_kullanilirsa_TUM_oturumlar_kapanir` | AYNI |

Son mutasyondan sonra kopya ağaçta temiz build: 0 Uyarı 0 Hata. Gerçek ağaç dosyaları kopyayla `cmp` AYNI.

### 000000000000.5 Canlı doğrulama (başsız Chrome, aynı profilde 2 ve 3 sekme)

- API 5188 kopya ağaçtan `--no-build` ile başlatıldı. `Jwt__AccessDakika=1` verildi; ClockSkew 30 sn olduğu için erişim jetonu ~90 sn'de doluyor. `Sistem__Anahtari` bu tur için üretilen rastgele değer: ortam değişkeninden okundu, chmod 600 dosyada tutuldu, hiçbir çıktıya basılmadı.
- Kiracı `PAKETEMANET-1-TEST T40`. Oturum `inceleme-vekili.py` (5189) ile parolasız açıldı. Betik: `t40/iki-sekme.mjs`.
- Senaryo: aynı Chrome profilinde N sekme `/emanet` açıyor → 105 sn bekleniyor (erişim jetonu doluyor) → sekmeler **aynı anda** `Blazor.navigateTo('/cari')` ile korumalı istek atıyor (sayfa yenilenmiyor, sekmeler bellekteki eski jetonu taşıyor) → 6 sn sonra durum okunuyor → `/emanet`'e dönülüyor.

| Senaryo | Önce | Sonra | Son | API günlüğü | SQL `refresh_tokenler` |
|---|---|---|---|---|---|
| 2 sekme | 2× `/emanet` | 2× `/cari`, jeton var | 2× `/emanet` | tek `POST /auth/yenile` (tek RequestId), `/auth/cikis` yok | iptal 1 (döndürülen) · etkin 3 |
| 3 sekme | 3× `/emanet` | 3× `/cari` | 3× `/emanet` | tek `POST /auth/yenile` (RequestId `…912R:00000001`) | aile iptal edilmedi |
| Gerçek yeniden kullanım (`t40/reuse40.py`) | giriş 200 → `yenile(r0)` **200** | `yenile(r0)` tekrar **401** "Refresh token geçersiz veya süresi dolmuş" | `yenile(r1)` meşru jeton **401** | — | kullanıcının **8/8** jetonu `iptal_edildi=1` (aile iptali korunuyor) |

Ekran görüntüleri Read ile görüldü: `t40/olc/sekme2-1.png`, `sekme2-2.png`, `sekme3-1..3.png`. Yenilemeden sonra bütün sekmeler oturumu açık halde Emanet ekranında (kullanıcı "Emanet S.", müşteri seçici, "Emanet al / Emanet iade et"). Giriş ekranı yok.

**Gerçek tarayıcı kuralı:** bu turda ekrana dokunan dosya yok (razor/css/sözlük değişmedi). Yalnız bir JS köprüsü ve servis kodu değişti. Emanet ölçümleri § 00000000000.4'teki (t39) haliyle geçerli. Sekme görüntüleri yalnız oturumun ayakta kaldığının kanıtı.

### 000000000000.6 Temizlik

- API ve vekil `kill` ile durduruldu; `lsof` 5188/5189 **boş**. `profil-sekme` Chrome süreci **0**. 5199/5200'e dokunulmadı.
- ⚠ Kiracı silme sırasında bir hata oldu. `kiraci-temizle.sh` önce `bash` ile çağrıldı. Betik zsh'e özgü `${=TABLOLAR}` kullandığı için tablo döngüsü çalışmadı (günlük: "bad substitution") ve yalnız `tenants` satırı silindi; 72 yetim satır kaldı. Bu satırlar aynı tablo listesiyle (information_schema, `tenant_id` kolonu olan 70 tablo) zsh'de silindi: cari_bakiyeler 3, cariler 3, denetim_kayitlari 42, hareketler 3, kullanicilar 1, kur_snapshots 10, refresh_tokenler 8, subeler 1, telemetri_olcumleri 1. Sonra bu kiracı kimliğiyle **0** satır kaldı; `tenants` `PAKETEMANET%` **0**.
- `tenants`: başlangıçta 14, kiracı açıldıktan sonra 15, son **13**. Bir eksik, eşzamanlı bir şeridin kendi kiracısını silmesinden (liste `t40/temizle.log`). QA-TEST kiracılarına dokunulmadı.
- `anahtar`, `fikstur.json`, `jeton`, `durum39.json` `printf '' >` ile boşaltıldı.
- ⚠ Proje ağacında artık kaldı: ilk mutasyon betiğindeki bir yol hatası build günlüklerini `dotnet/mb-MB1.log`, `mb-MB2.log`, `mb-MB3.log` olarak proje ağacına yazdı. İçerikleri `t40/`'a kopyalandı ve dosyalar `printf ''` ile **0 bayta** indirildi. Silme kurucuya bırakıldı (dosya silme yasak).

### 000000000000.7 Şerit kapısı

1. Build: 0 Hata, 0 Uyarı.
2. Bu turun testleri 0 başarısız: filtreli 360/360 ve 66/66.
3. denetim.sh: bu paketin sayaçları ✓ (betik sayısı 10 ✓). Tek ✗ `P-13 GEREKCESIZ_KILITLI_ALAN 1 (Cariler.razor:1)`. Bu dosya başka bir şeridin alanında ve bu turda dokunulmadı.
4. Mutasyon: 3/3 kırmızı, cmp AYNI.
5. Canlı: 2 ve 3 sekme senaryosu ile yeniden kullanım senaryosu, HTTP + SQL kanıtıyla.
6. Kiracı silindi (yetim satırlar dahil 0).
7. Tam süit (gerçek ağaç, 05:10–05:22): 7675 test, 7668 geçti, **6 başarısız**:
   - `TasarimBelirtecTestleri.Mod_yazicisi_…_yeni_betik_eklemez` **bu paketindi** (betik sayısı 9 → 10). Gerekçesiyle güncellendi, yeniden koşulunca yeşil.
   - `MigrationHazirlikTestleri.EksikMigrationliSemadaBekleyenListesiDolar` tek başına koşulunca yeşil (eşzamanlı DB koşumu).
   - Dört KATMAN2 testi (`KATMAN2SinirTestleri.Asenkron_…`, `KATMAN2MenuTercihEkranTestleri.Kalici_hatada_…`, `.On_hizli_tiklama_…`, `.Yazma_surerken_…`) KATMAN şeridine ait. `MenuTercihServisi.cs` 05:32'de hâlâ düzenleniyordu. Bu testler 401 ya da yenileme yolunu kullanmıyor; tek başına koşumda da 2 farklı KATMAN2 testi kırmızı çıktı (kararsız, sahibi KATMAN).
   - Bu paketin ürettiği yeni kırmızı: **0**.

### 000000000000.8 Açık kalanlar (sahibiyle)

- **Kurucu:** proje ağacındaki 0 baytlık `dotnet/mb-MB1.log`, `mb-MB2.log`, `mb-MB3.log` (bu tur) ve `dotnet/denetim.sh.mutbak` (18.09, bu pakete ait değil) silinmeli.
- **Orkestratör:** `kiraci-temizle.sh` bash ile çağrılınca tabloları atlayıp yalnız `tenants` satırını siliyor ve yine de "SİLİNDİ" diyor. Bu, Anayasa md.1 açısından bir risk. Betiğin başına bir zsh denetimi (`[ -n "$ZSH_VERSION" ] || exec zsh "$0" "$@"`) önerilir. Betik bu paketin alanı dışında, dokunulmadı.
- **KATMAN:** KATMAN2 menü tercihi testleri kararsız ya da kırmızı.
- **Cariler sahibi:** denetim.sh P-13 `Cariler.razor:1`.
- **Mehmet (güvenlik), bilgi için:** `navigator.locks` yalnız güvenli kökende (https ya da localhost) var. Düz http yayında localStorage kilidine düşülüyor. Bu en iyi çaba bir kilit; sunucu kuralı değişmediği için güvenlik açığı doğmaz, en kötü durumda eski davranışa döner.

## 00000000000. On birinci devralma turu (28.09 21:25–21:55, t9 · API 5188 · vekil 5189 · CDP 9988–9998)

Ağaçtaki iş korundu. Başlangıçta gerçek ağaçtaki `Emanet.razor`, `mut-yedek/EMANET-1/M57.orj` ile **birebir aynı** çıktı (21:01 zaman damgası başka bir eşitlemeden). Bu turda doğrulayıcının onuncu tur sonrası bulguları kapatıldı: tasarım hakeminin iki KRİTİK bulgusu ve EMANET-1'e düşen DÜŞÜK bulgu. Test API'si önceki turdaki gibi başlatıldı: rastgele tek kullanımlık `Sistem__Anahtari`, ortam değişkeninden, chmod 600 dosya. Değer hiçbir çıktıya basılmadı.

### 00000000000.1 Bulgular ve kapanış

| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| [KRİTİK, hakem] "Eski kayıtlar" açılınca tablonun altında iki kalıcı "Bu kayıt değiştirilemez" kutusu vardı. Biri Emanet'le ilgisiz "çok satırlı fiş" metnini taşıyordu | `Emanet.razor`: iki `DegismezIpucu` (`EmanetHareketi`, `EmanetHareketiFinansalAlan`) tablonun altından **kaldırıldı**. UX-S25/S30 hâlâ karşılanıyor: kilitli satırın "Geri al" düğmesi yalnız o satıra ait tek sebebi `title` içinde taşır (`GeriAlKilitSebebi`). Düzeltme yolu her satırda düğme olarak duruyor | bUnit **E38** (BASİT+PRO, `#e-gecmis .degismez-ipucu` = 0) · E18 güncellendi · canlı: 12/12 ölçümde `degismezKutu = 0` · görüntü `…t39-Mehmet-gecmis-*` |
| [KRİTİK, hakem] İade çekmecesinde BASİT modda "Toplam has yalnız altındır…" teknik notu vardı | `#c-maden-notu` `ProAlan` içine alındı: yalnız PRO'da, yalnız çok madende çizilir. Doğrulayıcı testi X1 (PRO varsayılanı) değiştirilmedi ve yeşil | bUnit **E39** (BASİT'te yok) · E27/E27b/X1 yeşil · canlı: BASİT `madenNotu = 0`, PRO `1` · görüntü `…Ayse-iade-BASIT-aydinlik-1440` (not yok), `…Ayse-iade-PRO-karanlik-1440` (not var) |
| [KRİTİK, hakem] Ayrıntı çekmecesinde ayrı "Değiştirilemez" başlığı, "Gram ve ayar kayıtta has grama çevrilip saklanır." satırı, kilit kutusu ve "Geri al (düzeltme fişi)" düğmesi vardı | `#eh-detay-kilitli-baslik` h3, `#eh-detay-gram-notu` ve `DegismezIpucu EmanetHareketiKilitli` **kaldırıldı**. Kilitli alanlar (`#eh-detay-kilitli`, id korundu) girdisiz yazılır. Geri alınabilir kayıtta tek kısa satır `#eh-detay-duzeltme` "Yanlışsa Geri al ile düzeltin." (yeni sözlük anahtarı `EmanetSadeSozlugu.DetayDuzeltme`, TR+EN). Geri alınamayan kayıtta mevcut tek satır sebep (`#eh-detay-geri-al-kilit-*`) kalır. Düğme metni `S.SatirGeriAl` = "Geri al" | bUnit **E40** (BASİT+PRO) · S35_14/S35_15 güncellendi · canlı: 12/12 `detayBaslik 0 · gramNotu 0 · kutu 0 · duzeltme "Yanlışsa Geri al ile düzeltin." · düğme "Geri al"` · görüntü `…Mehmet-ayrinti-*` |
| [DÜŞÜK] İade has alanı 3 haneydi ("5,000"), özet ise 4 hane. Gram silinince "0" görünüyordu | Yeni `Pages/EmanetParcalari/EmanetHasGirdi.cs` (`Gram`'dan türer, yalnız odak dışı biçim `Bicim.HasGirdi` = 4 hane). Ortak `Gram` bileşenine dokunulmadı. `cGram` → `decimal?` oldu: silinince alan boş kalır. Gövdeye gram hiç gitmiyor, POST gövdesi değişmedi | bUnit **E41** · canlı: 24/24 iade ölçümünde `c-has = "11,4500"`, gram 10 → sil sonrası `c-gram = ""` |
| [DÜŞÜK] 375 px'te 68 karakterlik unvan için K6 = 3 | Kod değişikliği yok (onuncu tur gerekçesi § 0000000000.2 geçerli). Kalıcı çözüm ortak alanda: `Secici.razor` alt satırı ya da `sikisma-olc` `.secici-alt` ayrı sayımı | Açık kalanlar |
| [DÜŞÜK] denetim.sh Banka P-39 ve başka şeritlerin tam süit kırmızıları | Bu turda `denetim.sh` gerçek ağaçta **DENETİM TEMİZ** (Banka düzeltilmiş). Tam süit kırmızıları sahipleriyle aşağıda | § .2 |

Eski kayıtlar tablosunda `GeriAlKilitSebebi` hâlâ `NedenFis` ("birden çok satırlı bir fişin parçasıdır…") döndürebiliyor. Bu metin yalnız o satır gerçekten çok bacaklı bir fişe bağlıysa (`FisId` dolu) ve yalnız düğmenin `title` ipucunda çıkar. Koşulsuz basılan kutu kaldırıldı. Emanet uçları tek bacaklı olduğu için bu durum canlıda oluşmuyor.

### 00000000000.2 Derleme, testler, mutasyon, denetim

- `dotnet build Kuyumcu.sln --nologo`: **0 Hata**. Tek uyarı `Kuyumcu.Tests/EtiketEkranTestleri.cs(145,47) xUnit1031`, ORTAK-1/UrunEtiket alanında (EMANET-1 değil). Kopya ağaçta son temiz build: **0 Uyarı 0 Hata**.
- Not: ilk build eşzamanlı bir şeridin build'iyle çakıştı. Web dll'i düzenlemenin yarısını içeriyordu ve E41 yanlış kırmızı verdi. Dosyalara `touch` yapıldı, yeniden build alındı, sonuç yeşil. Aynı etki önceki turlarda da görülmüştü.
- Filtreli (gerçek ağaç, `KUYUMCU_TEST_DB=kuyumcu_test_t9`, `Emanet|EMANET1|P37c|MigrationHijyeni|OlcumKapisi|UX4cEkranTestleri|KilitGerekceTestleri|UX1C2EkranTestleri|S35KurVitrin`): **258/258 geçti**. Yeni testler: E38 ×2, E39, E40 ×2, E41 (6 test).
- Tam süit (trx `/private/tmp/kuyumcu-serit/trx-pEMANET-1-t39/`): **6803 toplam, 6801 geçti, 2 başarısız**. İkisi de başka şeride ait: `Dogrulayici.SATIS3SinirTestleriTur7.Tur7_takas_fiyati_CIKMAYAN_urun_onceki_urunun_fiyatini_TASIMAZ` (SATIS-3; takas fiyatı kutusu) ve `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1; 15/16). **Şerit kapısı madde 7:** EMANET-1 yeni kırmızı üretmedi.
- `dotnet/denetim.sh` (gerçek ağaç): **DENETİM TEMİZ** (rc=0).

Mutasyonlar kopya ağaçta koşuldu (`t39/kok`, gerçek ağaçtan `rsync`). Adımlar: `perl -0777` ile tek eşleşme, build, filtreli test (`Emanet|EMANET1|UX1C2|S35KurVitrin`, 184 test), yedek `mut-yedek/EMANET-1/<kod>.orj` geri kopyalanır, `cmp` ile karşılaştırılır. Betik `t39/mut39.sh`, çıktı `t39/mut39.out`.

| Kod | Mutasyon | Kırmızı testler | Geri alındı |
|---|---|---|---|
| M58 | Tablo altına `DegismezIpucu EmanetHareketi` geri eklendi | E38 (BASİT), E38 (PRO), E18, UX1C2.Emanet_COK_BACAKLI_fis_satirinda_STORNO_KILITLI_ve_gerekce_VAR | cmp AYNI |
| M59 | `#c-maden-notu` `ProAlan` sarmalayıcısı kaldırıldı | E39_Basit_modda_iade_cekmecesinde_maden_notu_yok | cmp AYNI |
| M60 | Ayrıntı düğmesi `@M.GeriAl` ("Geri al (düzeltme fişi)") | E40 ×2, S35_15 | cmp AYNI |
| M61 | Ayrıntı çekmecesine kilit kutusu geri eklendi | E40 ×2, S35_14 | cmp AYNI |
| M62 | `cGram = v;` → `cGram = v ?? 0m;` | E41_Iade_has_alani_4_hane_gram_silinince_bos | cmp AYNI |
| M63 | `EmanetHasGirdi` → `Bicim.GramGirdi` | E41_… | cmp AYNI |
| M41 (kabul) | `<Katlanir Id="e-gecmis" BaslangictaAcik="true"` | 26 test (E2, E10, E16, E18, E26, E38 ×2, E40 ×2, Z5, V3, Q4, S21, S35_12…18, P12 B43, UX1C2 ×4, Elimizdeki_emanet…) | cmp AYNI |

Mutasyonlardan sonra gerçek ağaç ile kopya ağaç 6 dosyada **cmp AYNI**. Gerçek ağaçta mutasyon artığı yok.

### 00000000000.3 Canlı doğrulama (API 5188 `dotnet run --no-build`, kiracı `PAKETEMANET-1-TEST T39`)

| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /sistem/davetler` · `POST /auth/kayit` · `POST /auth/giris` | 201 · 201 · 200 | 201 · 201 · 200 |
| `POST /cari` ×3 (68 karakterlik ticari unvan dahil) | 201 | 201 ×3 |
| `GET /kur/guncel` | 200 + id | 200, id var |
| `POST /emanet/giris` G925 1500 g · K22 12,5 g (Ayşe) · K22 10 g (Mehmet) | 200 | 200 ×3 |
| `GET /emanet/{Ayşe}` · `{Mehmet}` · `{Emanetsiz}` | karma · 9,16 · boş | 200 GUMUS 1387,5 + ALTIN 11,45 (2 hareket) · 200 ALTIN 9,16 · 200 bakiye yok, 0 hareket |
| `POST /emanet/giris` K14 5,5 g → `POST /islemler/{id}/storno` | 200 · 200 | 200 · 200; Mehmet 9,16'da kaldı, ters kayıt `stornoKaynakId` dolu |

SQL (`hareketler` ⋈ `cariler` ⋈ `tenants`, kiracı adıyla):

```
cari                 tur           maden  has_grami     aciklama                storno
Ayşe Yılmaz          EMANET_GIRIS  GUMUS  1387.500000   Gümüş tepsi             0
Ayşe Yılmaz          EMANET_GIRIS  ALTIN    11.450000   Bilezik                 0
Mehmet Karaosmanoğlu EMANET_GIRIS  ALTIN     9.160000   Bilezik                 0
Mehmet Karaosmanoğlu EMANET_GIRIS  ALTIN     3.217500   Yanlis ayar             0
Mehmet Karaosmanoğlu EMANET_GIRIS  ALTIN    -3.217500   STORNO: Yanlis ayar gi  1
```

### 00000000000.4 Gerçek tarayıcı (başsız Chrome + CDP, oturum `inceleme-vekili.py` ile parolasız)

**tur.mjs** `/emanet` × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375} = 16 sayfa kaydı (`t39/tur/olcum-1..4.jsonl`). K1..K13 **= 0**, yatay kaydırma **0**. Boy katı 1440/960/375'te 1,0. 1280'de BASİT 1,7, PRO 1,1; bunun sebebi turun açtığı kenar çubuğu menü grupları (KATMAN, onuncu turla aynı). `analiz.txt` bulguları: (a) 375'te global arama paleti SERBEST-YÜZEN (KATMAN, 4 durum); (b) "kombinasyon eksik" satırı. (b) analizcinin tema anahtarını okuyamamasından kaynaklanıyor: kayıtlarda `tema` alanı yok. Dört log dosyası `bitti 1..4` diyor ve 16 sayfa kaydının hepsi mevcut. Onuncu turdaki `analiz.txt`'de aynı satır var.

**tutarlilik.mjs** `/emanet` 1440: **mod farkı 0**. Düğmeler BASİT ve PRO'da aynı: `btn-birincil btn-buyuk` h52 17 px "Emanet al", `btn-buyuk btn-ikincil` h52 17 px "Emanet iade et".

**Durum ölçümü** (`t39/d39.mjs`, `sikisma-olc.js __olc` + ek sayaçlar): {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 375} × 7 durum = **84 ölçüm** (`t39/d-BASIT.log`, `d-PRO.log`):

| Durum | K1..K8, K10..K13 | Özel sayaçlar (hepsi 12/12) |
|---|---|---|
| Açılış · Emanetsiz · Ayşe (karma) | 0 | "Eksik olan" yok · " 0,0000" yok · büyük harf vurgu yok |
| Ayşe → İade çekmecesi | 0 (K9 aşağıda) | `c-has` "11,4500" (4 hane) · `#c-maden-notu` BASİT 0 / PRO 1 |
| İade: gram 10 → sil | 0 | `c-gram` "" (boş, "0" değil) · has "11,4500" (varsayılana döndü) |
| Mehmet → Eski kayıtlar açık | **0 (K9 dahil)** | görünür `.degismez-ipucu` **0** · "storno/Değiştirilemez/çok satırlı" metni **0** |
| Mehmet → Ayrıntı çekmecesi | 0 (K9 aşağıda) | başlık 0 · gram notu 0 · kutu 0 · "Yanlışsa Geri al ile düzeltin." · düğme "Geri al" |

K9 (binen): açık çekmece durumlarında sayfa ölçüsü 3–6 veriyor. Örneklerin hepsi perdenin altında kalan sayfa öğeleri ile çekmece öğesi çiftleri (ör. `#sayfa-basi-emanet-baslik ⟂ label "Elimizdeki emanet"`). Bu, ölçüm aracının bilinen sınırı (önceki turların açık kalanı: `checkVisibility` süzgeci önerisi). Bu yüzden **çekmece içi** ayrıca ölçüldü (`t39/c39.mjs`): iade, giriş ve ayrıntı çekmecesi × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 375} = **36 ölçüm**. Sonuç: binen **0**, taşan **0**, çekmece yatay kaydırma **0**.

Büyük harf: ölçümün yakaladığı "GÜNLÜK", "GRAM", "TARİH", "AÇIKLAMA" kelimeleri kaynakta küçük harfli. Büyük görünmelerinin sebebi ortak `app.css` `text-transform` kuralı (kenar çubuğu grup başlığı, üst bar kur etiketi, tablo `th`). Emanet'e özel değil; `sikisma-olc` K8 bunları saymıyor.

### 00000000000.5 Ekran görüntüleri (Read ile GÖRÜLDÜ, 13 adet; toplam 30 dosya `ham/TUVAL-KARSILASTIRMA/EMANET-1-t39-*`)

- `acilis-BASIT-aydinlik-1440`: başlık, tek sade cümle, (i) "Emanet nedir?", tek müşteri seçici, iki eşit büyük eylem, "Önce müşteri seçin." Tek ekran.
- `acilis-PRO-aydinlik-375`: aynı düzen alt alta, yatay kaydırma yok.
- `Mehmet-gecmis-BASIT-aydinlik-1440`: Eski kayıtlar açık, 3 satır. Tablonun altında **kutu yok**. "Geri alma kaydı" ve "Bu kayıt geri alındı" rozetleri görünüyor. "Ayrıntı" ve "Geri al" kesilmeden sığıyor.
- `Mehmet-gecmis-PRO-karanlik-1440`: aynı düzen, koyu belirteçler doğru, kutu yok.
- `Mehmet-gecmis-BASIT-aydinlik-375`, `…-PRO-aydinlik-375`: uzun unvan 2 satır, tablo kendi kabında kayıyor.
- `Mehmet-ayrinti-BASIT-aydinlik-1440`, `…-BASIT-aydinlik-375`, `…-PRO-aydinlik-1280`, `…-PRO-karanlik-1440`: Tür, Tarih, Açıklama, Has gram, Maden, Müşteri alanları. Tek küçük satır "Yanlışsa Geri al ile düzeltin." ve kırmızı "Geri al". "Değiştirilemez" başlığı, gram/ayar notu ve kilit kutusu **yok**.
- `Ayse-iade-BASIT-aydinlik-1440`, `…-BASIT-aydinlik-375`: Gümüş ve Altın madde listesi. "Toplam has yalnız altındır" notu **yok**. Has alanı "11,4500".
- `Ayse-iade-PRO-karanlik-1440`: PRO'da not görünüyor (gerekçeli fark: PRO ayrıntı modu, doğrulayıcı testi X1 bunu istiyor).

Emanet için tuval ekranı (`Emanet.dc.html`) hâlâ yok. Bölge tablosu § 7.5'teki gibi geçerli.

### 00000000000.6 Temizlik

API ve vekil `kill` ile durduruldu (PID dosyaları `t39/api.pid`, `t39/vekil.pid`). `lsof` 5188/5189 **boş**. `t39` profilli Chrome süreci **0**. 5199/5200 ve diğer şeritlerin portlarına dokunulmadı. Kiracı `betikler/kiraci-temizle.sh <id> --uygula` ile silindi; kimlik SQL'den değişkene alındı, basılmadı. Sonra: `tenants` `PAKETEMANET%` **0**, `kullanicilar` `emanet1-t39-%` **0**, bu kiracının `hareketler` satırı **0**. `tenants` kiracı açılmadan önce **19** (açıldıktan sonra 20, `PAKETEMANET%` 1), son **18**. Düşüş eşzamanlı şeritlerin kendi kiracılarını silmesinden; `PAKETEMANET%` 0 ölçütü sağlandı. QA-TEST kiracılarına dokunulmadı. `anahtar`, `fikstur.json`, `jeton`, `durum39.json` `printf '' >` ile boşaltıldı (0 bayt). Proje ağacında geçici dosya yok.

### 00000000000.7 DoD sözleşmesi, dokunulan sayfalar, değişen dosyalar

- Yeni varlık, uç ya da migration yok; sunucu değişmedi. Emanet hareketi: açıklama satır içi düzenlenir (PATCH, P-12). Has, maden ve müşteri kilitli; düzeltme yolu satırdaki ve Ayrıntı'daki "Geri al" düğmesi (48-A `/islemler/{id}/storno`, ortak onay + gerekçe). Kilitli düğme sebebini `title` ve Ayrıntı satırında söylüyor. DegismezIpucu kutusunun kaldırılması düzeltme yolunu ortadan kaldırmadı; yol düğme olarak duruyor (UX-S25 "ya düzenle/geri al yolu vardır").
- Dokunulan sayfa: `Pages/Emanet.razor`. Aynı şerit, aynı paket, doğrulayıcı/hakem bulgusu gerekçesiyle.
- Değişen dosyalar (bu tur): `dotnet/Kuyumcu.Web/Pages/Emanet.razor`, `dotnet/Kuyumcu.Web/Pages/EmanetParcalari/EmanetHasGirdi.cs` (yeni), `dotnet/Kuyumcu.Core/I18n/Sozlukler.EmanetSade.cs` (`DetayDuzeltme` TR+EN), `dotnet/Kuyumcu.Tests/EmanetSadeTestleri.cs` (E18 güncel, E38–E41 yeni), `dotnet/Kuyumcu.Tests/S35KurVitrinEmanetTestleri.cs` (S35_14, S35_15: Emanet ayrıntı çekmecesi beklentisi hakem kararına göre), `dotnet/Kuyumcu.Tests/UX1C2EkranTestleri.cs` (yalnız `Emanet_COK_BACAKLI…` yöntemi: kutu yerine düğme `title` gerekçesi). Doğrulayıcı testleri (`Dogrulayici/EMANET1SinirTestleri.cs`) DEĞİŞTİRİLMEDİ.

### 00000000000.8 Açık kalanlar (sahibiyle)

- **SATIS-3:** `Dogrulayici.SATIS3SinirTestleriTur7.Tur7_takas_fiyati_CIKMAYAN_urun_onceki_urunun_fiyatini_TASIMAZ` kırmızı ("u2'nin fiyatı çıkmadı ama kutuda önceki ürünün fiyatı duruyor: '10.000'").
- **ORTAK-1:** `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` kırmızı (15/16); `EtiketEkranTestleri.cs(145)` xUnit1031 uyarısı.
- **Ortak alan (Secici / ölçüm aracı):** 375 px'te 68 karakterlik unvanda K6 = 3 (ad 2 satır + telefon 1 satır). Kalıcı çözüm: `Secici.razor` alt satırı kardeş eleman olmalı ya da `sikisma-olc` `.secici-alt`'ı ayrı saymalı.
- **Ölçüm aracı:** `sikisma-olc` K9, açık çekmecenin perdesi altındaki sayfa öğelerini sayıyor (`checkVisibility`/perde süzgeci önerisi). `tur/analiz.py` "kombinasyon eksik" satırı tema alanı olmadan yanlış raporluyor.
- **KATMAN:** 375 px'te global arama paleti serbest yüzüyor.
- **Tasarım:** Emanet için tuval ekranı (`Emanet.dc.html`) yok.
- **Orkestratör:** eşzamanlı şerit build'leri aynı `bin/obj`'yi paylaşıyor; bir şeridin yarım düzenlemesi başka şeridin derlemesine girebiliyor (bu turda E41 yanlış kırmızı verdi, `touch` + yeniden build ile giderildi).

## 0000000000. Onuncu devralma turu (28.09 20:20–20:45, t9 · API 5188 · vekil 5189): canlı doğrulama ve gerçek tarayıcı kanıtı

Kod bu turda DEĞİŞMEDİ. Başlangıçta gerçek ağaçtaki `Emanet.razor` dokuzuncu turun son mutasyon yedeğiyle (`mut-yedek/EMANET-1/M57.orj`) **cmp AYNI**; `if (false)` artığı 0. Dosyanın 20:06 zaman damgası başka bir şeridin build/eşitlemesinden; içerik aynı. Kurucu bu turda test API'sini rastgele tek kullanımlık sistem anahtarıyla başlatmayı onayladı ("test anahtarına izin"). Anahtar `python3 secrets` ile üretildi, `t37/anahtar` (chmod 600) dosyasına yazıldı, `Sistem__Anahtari` ortam değişkeniyle yalnız `localhost:5188` API'sine verildi. Değer hiçbir çıktıya basılmadı; iş sonunda dosya `printf '' >` ile boşaltıldı.

### 0000000000.1 Canlı doğrulama (API 5188 `dotnet run --no-build`, kiracı `PAKETEMANET-1-TEST T37`)

| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /sistem/davetler` (anahtar ve operatör başlığı, gerekçeli) | 201 | 201 |
| `POST /auth/kayit` · `POST /auth/giris` | 201 · 200 | 201 · 200 |
| `POST /cari` ×3 (Emanetsiz Musteri, Ayşe Yılmaz, 68 karakterlik ticari unvan) | 201 | 201 ×3 |
| `GET /kur/guncel` | 200 + id | 200, id var |
| `POST /emanet/giris` G925 1500 g · K22 12,5 g (Ayşe) · K22 10 g (Mehmet) | 200 | 200 ×3 |
| `GET /emanet/{Ayşe}` | GUMUS + ALTIN bakiyesi | 200: GUMUS 1387,5 · ALTIN 11,45 has, 2 hareket |
| `GET /emanet/{Mehmet}` · `/emanet/{Emanetsiz}` | 9,16 · boş | 200: ALTIN 9,16 · 200: bakiye yok, 0 hareket |
| `POST /emanet/giris` K14 5,5 g → `POST /islemler/{id}/storno` | 200 · 200 | 200 · 200; Mehmet bakiyesi 9,16'da kaldı |

SQL kanıtı (`hareketler`, kiracı adıyla birleştirilmiş):

```
cari                                   tur           maden  has_grami     aciklama                storno
Ayşe Yılmaz                            EMANET_GIRIS  GUMUS  1387.500000   Gümüş tepsi             0
Ayşe Yılmaz                            EMANET_GIRIS  ALTIN    11.450000   Bilezik                 0
Mehmet Karaosmanoğlu … Limited Şirketi EMANET_GIRIS  ALTIN     9.160000   Bilezik                 0
Mehmet Karaosmanoğlu … Limited Şirketi EMANET_GIRIS  ALTIN     3.217500   Yanlis ayar             0
Mehmet Karaosmanoğlu … Limited Şirketi EMANET_GIRIS  ALTIN    -3.217500   STORNO: Yanlis ayar gi  1
```

Storno 48-A motoruyla (`/islemler/{id}/storno`) yapıldı; ekranda "STORNO" kelimesi basılmıyor (aşağıda K8 = 0 ve görüntü).

### 0000000000.2 Gerçek tarayıcı ölçümleri (başsız Chrome + CDP, oturum `inceleme-vekili.py` ile parolasız)

**tur.mjs** `/emanet` × {BASIT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375} = 16 kombinasyon (`t37/tur/olcum-*.jsonl`, `analiz.txt`):

| Mod | Tema | 1440 | 1280 | 960 | 375 |
|---|---|---|---|---|---|
| BASIT | aydınlık | K1..K13 = 0 · yatay 0 · boy 1,0 | K = 0 · boy 1,7* | K = 0 · boy 1,0 | K = 0 · boy 1,0 |
| BASIT | karanlık | K = 0 · boy 1,0 | K = 0 · boy 1,7* | K = 0 | K = 0 |
| PRO | aydınlık | K = 0 · boy 1,0 | K = 0 · boy 1,1* | K = 0 | K = 0 |
| PRO | karanlık | K = 0 · boy 1,0 | K = 0 · boy 1,1* | K = 0 | K = 0 |

(K1 gizli görünen, K3 taşan/kırık, K5 TRY, K7 hizasız, K8 jargon, K9/K9b binen, K10/K10b elips/kesik, K11 hata bandı, K12 ezik, K13 kırpılan: 16 kombinasyonun hepsinde 0.) *1280 boy katı: tur'un katman adımı kenar çubuğu menü gruplarını açtıktan sonra alınan tam sayfa görüntüsünde kenar çubuğu uzuyor (`tur/foto/emanet__BASIT_aydinlik_1280.png` görüldü). Emanet içerik alanı tek ekran. Kenar çubuğu KATMAN şeridinin alanı. `analiz.txt` tek bulgu: 375'te global arama paleti SERBEST-YÜZEN (4 durum). Bu da KATMAN şeridinin `GlobalArama` bileşeni, önceki turlarda da açık kalan olarak yazıldı.

**tutarlilik.mjs** `/emanet` 1440: **mod farkı 0**. Düğme sınıfları BASIT ve PRO'da aynı: `btn-birincil btn-buyuk` h52 17 px "Emanet al", `btn-buyuk btn-ikincil` h52 17 px "Emanet iade et".

**Durum ölçümü** (`t37/d37.mjs`, `sikisma-olc.js __olc`): BASIT/PRO × aydınlık/karanlık × 1440/375 × 4 durum = 32 ölçüm:

| Durum | K1..K13 (32 ölçümün hepsi) | 1440 boy | 375 boy | İade düğmesi | Özet metni | "Eksik olan" · "0,0000" |
|---|---|---|---|---|---|---|
| Açılış (müşteri seçilmedi) | 0 | 1,00 | 1,00 | disabled, "Önce müşteri seçin." | yok | yok · yok |
| Emanetsiz müşteri | 0 | 1,00 | 1,00 | disabled, "Bu müşterinin emaneti yok." | "Emaneti yok" | yok · yok |
| Karma (Ayşe: gümüş + altın) | 0 | 1,00 | 1,00 | etkin | "Gümüş 1387,5000 has · Altın 11,4500 has" | yok · yok |
| Geri alınmış kayıtlı müşteri, Eski kayıtlar AÇIK (Mehmet) | 0 (K8 jargon 0) | 1,07 | 1,56–1,62 | etkin | "9,1600 has" | yok · yok |

Eski kayıtlar açılışta her durumda **kapalı** (`#e-gecmis-ac aria-expanded=false`); yalnız betik açınca açık. Geri alınmış kayıtta tür "Geri alma kaydı", satır rozeti "Bu kayıt geri alındı", açıklama "Geri alma: …"; "STORNO" ya da "storno" metni yok (dokuzuncu tur öncesi KRİTİK bulgu canlıda kapalı). Not: tablo başlıkları (TARİH, TÜR, HAS GRAM, AÇIKLAMA, EYLEMLER) kaynakta küçük harfli; büyük görünmeleri `app.css`'teki ortak `th` stilinden (`text-transform`) geliyor. `sikisma-olc` K8 bunu saymıyor. Tüm tablolarda ortak olduğu için Emanet'e özel ezme yapılmadı.

**p37.mjs** (dokuzuncu turun ORTA bulgusu ve K6), BASIT ve PRO × aydınlık/karanlık × 1440/1280/375 = 12 ölçüm:

| Ölçüm | Beklenen | 12 ölçümün hepsinde |
|---|---|---|
| İade çekmecesi açılışı | `#e-cikis-hesap` kapalı, has = tümü | open=false, has 11,45 |
| Gram 10 yazıldı | bölüm açık, has hesaplandı | open=true, has 9,16 |
| Gram silindi | bölüm AÇIK kalır, odak alanda, has varsayılana döner | open=true, odak `c-gram`, has 11,45 |
| Giriş: taş 1 → sil | detay açılır ve silince açık kalır | [kapalı, açık, açık] |

Dokuzuncu turda kodla kapatılan ORTA bulgu gerçek tarayıcıda da kapalı.

**K6 (seçici listesi), 375 px, 68 karakterlik ticari unvan:** `__seciciOlc` 375'te `#e-cari genislik=345 enCokSatir=3 sagTasma=0` veriyor (BASIT/PRO, aydınlık/karanlık; 1440 ve 1280'de 0). Dokuzuncu turun tahmini ölçümle **doğrulandı**: ad **2 satır** (16 px), telefon alt satırı **1 satır**. Araç ikisini tek `span.secici-ad` içinde saydığı için 3 çıkıyor. CSS ile düzeltme canlıda denendi (`t37/k6dene.mjs`, stil sayfaya enjekte edildi, dosyaya YAZILMADI):

| Deneme | Satır |
|---|---|
| Mevcut (ad 16 px, telefon ayrı satır) | 3 |
| Telefon adın arkasında satır içi, 16 px | 4 |
| Aynı, ad 15 px | 3 |
| Aynı, ad 14 px | 3 |

Kanvas ölçümü: ad metni 16 px'te 568 px, telefon 92 px. Kullanılabilir genişlik 293 px (liste 345, dolgu 24, işaret sütunu 18). Ad ile telefonun 2 satıra sığması için okunur yazı boyunda satır başına en az ~334 px gerekiyor, sözcük kırılma payı hariç. Bu yüzden 375'te bu uzunlukta bir unvan için K6 = 0 yalnız şu yollarla sağlanabilir: yazıyı 14 px altına indirmek (tur `kucuk-yazi` sayar), adı elips ile kesmek (K10 ve bilgi kaybı) ya da telefonu kaldırmak (sekizinci turun KRİTİK "telefonla arama" düzeltmesini geri alır). Üçü de daha kötü. **CSS değiştirilmedi.** Bu, gerekçeli fark olarak yazıldı: `tur.mjs` resmi K1..K13 ölçümünde K6 yok ve 16/16 = 0; ekran görüntüsü `EMANET-1-t37-secici-uzun-unvan-{BASIT,PRO}-aydinlik-375.png` (görüldü: ad 2 satır, telefon altında, taşma ve kesik yok). Kalıcı çözüm ortak alanda: ya `Bilesenler/Secici.razor` alt satırı `.secici-ad` kardeşi yapar, ya da `sikisma-olc` `.secici-alt`'ı ayrı sayar (açık kalanlara yazıldı).

### 0000000000.3 Ekran görüntüleri (Read ile GÖRÜLDÜ, 13 adet)

`ham/TUVAL-KARSILASTIRMA/` altında:
- `EMANET-1-t38-acilis-BASIT-aydinlik-1440.png`: başlık, tek sade cümle, (i) "Emanet nedir?", tek müşteri seçici, iki eşit büyük eylem; "Önce müşteri seçin." Tek ekran.
- `EMANET-1-t38-Emanetsiz-BASIT-aydinlik-1440.png`: "Emaneti yok"; iade disabled + "Bu müşterinin emaneti yok."; Eski kayıtlar 0, kapalı.
- `EMANET-1-t38-Ayse-BASIT-karanlik-1440.png`: karma özet "Gümüş 1387,5000 has · Altın 11,4500 has"; koyu tema belirteçleri doğru.
- `EMANET-1-t38-Mehmet-gecmis-BASIT-aydinlik-1440.png`: Eski kayıtlar açık; "Geri alma kaydı", "Bu kayıt geri alındı"; "Ayrıntı"/"Geri al" kesilmeden sığıyor (tasarım hakeminin önceki KRİTİK taşma bulgusu kapalı).
- `EMANET-1-t38-Mehmet-gecmis-PRO-karanlik-1440.png`: PRO koyu, aynı düzen.
- `EMANET-1-t38-Ayse-BASIT-aydinlik-375.png`: iki eylem alt alta, karma özet iki satırda, madde parçaları bölünmeden.
- `EMANET-1-t38-Mehmet-gecmis-BASIT-aydinlik-375.png`, `…-PRO-aydinlik-375.png`: uzun unvan seçicide 2 satır; tablo kendi kabında yatay kayıyor (sayfa yatay kaydırma 0).
- `EMANET-1-t38-Emanetsiz-PRO-aydinlik-375.png`: iade disabled + sebep.
- `EMANET-1-t37-iade-gram-silindi-BASIT-aydinlik-375.png`, `…-PRO-aydinlik-1440.png`: gram silindikten sonra "Ayar ve gramdan hesapla" bölümü açık, has 11,450 (varsayılan).
- `EMANET-1-t37-secici-uzun-unvan-BASIT-aydinlik-375.png`: K6 durumu (yukarıda).
- `t37/tur/foto/emanet__BASIT_aydinlik_1280.png`: 1280 boy katının kaynağı (kenar çubuğu).

Gözlem (DÜŞÜK, bu turda değiştirilmedi): iade çekmecesinde "Gram ağırlık (isteğe bağlı)" alanı silinince "0" gösteriyor (`cGram` null olamayan `decimal`, ilk sürümden beri). Davranış doğru (has varsayılana döner). Boş göstermek için alanı `decimal?` yapmak gerekir; bu, gövde hesabına dokunduğu için ayrı küçük iş olarak yazıldı.

Emanet için tuval ekranı (`Emanet.dc.html`) hâlâ yok; bölge tablosu § 7.5'teki gibi geçerli.

### 0000000000.4 Temizlik

API (PID dosyası `t37/api.pid`) ve vekil `kill` ile durduruldu; `lsof` 5188/5189 **boş**; `t37` profilli Chrome süreçleri **0**. 5199/5200 ve diğer şeritlerin API'lerine (5160/5168/5170/5193/5198) dokunulmadı. Kiracı `betikler/kiraci-temizle.sh <id> --uygula` ile silindi. Kimlik SQL'den değişkene alındı, basılmadı. Sonra: `tenants` içinde `PAKETEMANET%` **0**; `kullanicilar` `emanet1-t37-%` **0**; bu kiracının `hareketler` yetimi **0**. `tenants` başlangıç **20** → son **18**; düşüş eşzamanlı şeritlerin kendi kiracılarını silmesinden. QA-TEST kiracılarına dokunulmadı. `anahtar`, `fikstur.json`, `jeton`, `durum37.json` `printf '' >` ile boşaltıldı (0 bayt). Proje ağacına geçici dosya yazılmadı. Ekran görüntüleri yalnız `ham/TUVAL-KARSILASTIRMA/` altında.

### 0000000000.5 Kapı özeti (şerit kapısı 7 madde)

1. build 0 hata: dokuzuncu tur (kod değişmedi, cmp AYNI) · 2. filtreli 207/207 · 3. denetim.sh TEMİZ · 4. mutasyon M53–M57 + M41 + M37 kırmızı, cmp AYNI · 5. **canlı doğrulama: bu turda YAPILDI** (§ .1–.3) · 6. test kiracısı silindi · 7. tam süitte EMANET-1 yeni kırmızısı 0 (tek kırmızı ORTAK-1 O8).

## 000000000. Dokuzuncu devralma turu (28.09 19:38–20:00, t9 · API 5188 · vekil 5189)

Ağaçtaki iş korundu. Başlangıçta gerçek ağaçtaki `Emanet.razor`, sekizinci turun son yedeği `mut-yedek/EMANET-1/M52.orj` ile **cmp AYNI** çıktı; yarım mutasyon yok. `Dogrulayici/EMANET1SinirTestleri.cs` doğrulayıcı tarafından 19:25'te genişletilmiş (Y1–Y5). DEĞİŞTİRİLMEDİ ve hepsi yeşil. Devir notu 2'deki X5 (KRİTİK) ile 375 px karma özet (DÜŞÜK) yedinci turda kapanmıştı; ağaçta duruyorlar (`VarsayilanIadeHasKur`, `.emanet-ozet-parca { white-space: nowrap }`). X5 bu turda da yeşil; M37 mutasyonu onu yine kırmızıya çeviriyor.

### 000000000.1 Bulgular ve kapanış

| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| [ORTA] İade çekmecesinde gram silinince `#e-cikis-hesap` odak içindeyken kapanıyor, has bayat kalıyor (girişte aynı kalıp: taş/not silinince `#e-giris-detay` kapanıyor) | `Emanet.razor`: `open` artık "yapışkan" bayraklarla hesaplanıyor: `CikisHesapAcik => cikisHesapYapiskan \|\| cGram > 0`, `CikisNotAcik`, `GirisDetayAcik => girisDetayYapiskan \|\| …`. Bayrak değer girilince kurulur, çekmece açılınca (`GirisModalAc`/`CikisModalAc`) sıfırlanır. Değer silinince bölüm kapanmaz. Blazor `open` özniteliğini yalnız değer değişince yazar, bu yüzden kullanıcının kendi aç/kapa seçimi de korunur. `IadeHasHesapla`: `else if (madenDegisti) VarsayilanIadeHasKur();` → `else VarsayilanIadeHasKur();`. Gram silinince, has elle yazılmadıysa "tümünü iade et" varsayılanına döner. Elle yazıldıysa korunur. Taş ve not alanlarının satır içi lambdaları `GirisTasDegisti` / `GirisAciklamaDegisti` / `CikisAciklamaDegisti` yöntemlerine alındı. Kimlikler, gövdeler ve uçlar değişmedi | bUnit **E36_Iade_gram_silinince_bolum_acik_kalir_has_varsayilana_doner**, **E37_Giris_tas_ve_not_silinince_detay_acik_kalir** · mutasyon M53–M57 |
| [DÜŞÜK] 375 px'te uzun ticari unvan seçici listesinde 3 satır (K6) | **Bu turda kod değişikliği YOK.** Sekizinci turun `@media (max-width:600px) … .secici-ad { font-size:16px }` kuralı ağaçta duruyor. Olası neden şu: sekizinci turda seçeneğe telefon ikinci satır (`.secici-alt`) olarak eklendi, `sikisma-olc.js` `satir()` ise `.secici-ad` içindeki TÜM metin düğümlerini sayıyor. Bu durumda 2 ad satırı + 1 telefon satırı = 3 olur. Bu bir **tahmindir**; tarayıcıda ölçülemedi (§ .3). Ölçmeden CSS değiştirmek gerçek tarayıcı kuralına aykırı olduğu için dokunulmadı. Ölçüm betiği hazır: `/private/tmp/kuyumcu-serit/EMANET-1/t37/p37.mjs` (ad ve telefon satırlarını ayrı sayar) | AÇIK |
| [DÜŞÜK] Kiracı sayısı paralel koşumda karşılaştırılamaz | Ölçüt `PAKETEMANET% = 0` yapıldı. Başlangıç: `tenants` 19, `PAKETEMANET%` **0**. Bu tur kiracı AÇILMADI (§ .3), son: `PAKETEMANET%` 0 | SQL (§ .4) |
| [KRİTİK, süreç] Tasarım hakemi görüntü alamadı (davet engeli) | Aynı engel bu turda uygulayıcıda da oluştu (§ .3). Çözüm kurucu/orkestratörde | AÇIK |

### 000000000.2 Derleme, testler, mutasyon, denetim

- `dotnet build Kuyumcu.sln --nologo` (gerçek ağaç): **0 Hata**. 2 uyarı var, ikisi de `Kuyumcu.Tests/EtiketEkranTestleri.cs(145,47) xUnit1031`. Bu dosya EMANET-1'in değil (ORTAK-1 / UrunEtiket alanı). Mutasyon kopyasında son temiz build: **0 Uyarı 0 Hata**.
- Filtreli (gerçek ağaç, `Emanet|EMANET1|P37c|MigrationHijyeni|OlcumKapisi|UX4cEkranTestleri|KilitGerekceTestleri`, `KUYUMCU_TEST_DB=kuyumcu_test_t9`): **207/207 geçti**. Önceki 200'e E36, E37 ve doğrulayıcının Y1–Y5 testleri eklendi.
- Tam süit (gerçek ağaç, trx `/private/tmp/kuyumcu-serit/trx-pEMANET-1-t37/`): **6716 toplam, 6715 geçti, 1 başarısız**. Kırmızı olan `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` ORTAK-1'e ait ve önceki turlarda da kırmızıydı. Sekizinci turdaki iki `KarmaOdemeEkranTestleri` kırmızısı bu koşumda yeşil. **Şerit kapısı madde 7:** EMANET-1 yeni kırmızı üretmedi.
- `dotnet/denetim.sh` (gerçek ağaç): **DENETİM TEMİZ** (rc=0).

Mutasyonlar kopya ağaçta koşuldu (`t34/kok`, `rsync` ile gerçek ağaçla eşitlendi). Her biri `perl -0777` ile tek eşleşme, ardından build, filtreli test (`Emanet|EMANET1`, 133 test), yedeği geri kopyalama ve `cmp` adımlarından geçti. Betik `/private/tmp/kuyumcu-serit/EMANET-1/t37/mut37.sh`, çıktı `mut37.out`.

| Kod | Mutasyon | Kırmızı testler | Geri alındı |
|---|---|---|---|
| M53 | `else VarsayilanIadeHasKur();` → `else if (false) …` | E36_…, E28b_Gram_girilmeden_gumus_ayari_secilince_varsayilan_gumus_neti | cmp AYNI |
| M54 | `CikisHesapAcik => cGram > 0` (yapışkan bayrak yok) | E36_… | cmp AYNI |
| M55 | `GirisDetayAcik`'ten `girisDetayYapiskan` çıkarıldı | E37_… | cmp AYNI |
| M56 | `CikisModalAc`'ta `cikisHesapYapiskan = false` sıfırlaması silindi | E36_… | cmp AYNI |
| M57 | `GirisTasDegisti`'nde bayrak kurulumu silindi | E37_… | cmp AYNI |
| M41 (kabul) | `<Katlanir Id="e-gecmis" BaslangictaAcik="true"` | 21 test (E2, E10, E16, E18, E26, V3, Z5, S21, S35_12…18, UX1C2 ×4, P12 B43, Elimizdeki_emanet…) | cmp AYNI |
| M37 (X5) | `else if (!cHasElle)` → `else if (false)` | Dogrulayici.EMANET1SinirTestleri.X5_Karma_madene_gecince_varsayilan_o_madenin_neti, E28_…, E28b_…, E36_… | cmp AYNI |

Mutasyonlardan sonra kopya ile gerçek ağaçta `Emanet.razor` ve `EmanetSadeTestleri.cs` **cmp AYNI**.

### 000000000.3 Gerçek tarayıcı ve canlı doğrulama: KOŞULAMADI (engel)

Test kiracısı açmak için bir davet gerekiyor (`POST /sistem/davetler`, sistem anahtarı ister; `POST /auth/kayit` davetsiz 403 döner). Önceki turlar bunu şöyle yaptı: API rastgele bir sistem anahtarıyla (`Sistem__Anahtari`) başlatıldı. Bu turda aynı adım (`dotnet run … --urls http://localhost:5188`, anahtar ortam değişkeninden) oturumun **otomatik izin sınıflandırıcısı tarafından reddedildi** (gerekçe: "Credential Exploration"). Hemen ardından yapılan salt okunur `ls`/`lsof` denetimi de reddedildi. Sınıflandırıcının talimatı gereği bu sonuca başka bir yoldan gidilmedi. Örneğin davet tablosuna elle satır eklenmedi ve başka kiracıların kimlik bilgileri kullanılmadı. Bu yüzden şunlar koşulamadı: API 5188 / vekil 5189, `tur.mjs`, `tutarlilik.mjs`, `sikisma-olc.js` K1..K13 + K6, ekran görüntüleri ve canlı HTTP+SQL. Engel, tasarım hakeminin bildirdiğiyle aynı.

Hazır bekleyenler (engel kalkınca koşulacak):
- `t37/kur37.py`: kiracı `PAKETEMANET-1-TEST T37`, 3 müşteri (uzun unvanlı biri dahil), karma altın/gümüş emanet.
- `t37/storno37.py`: geri alınmış kayıt durumu.
- `t37/p37.mjs`: 1440/1280/375 × açık/koyu, BASİT ve PRO. Ölçtükleri: K6 (ad satırı ile telefon satırı ayrı), iade gram 10 → sil (`open`, has, odak), giriş taş 1 → sil (`open`). Ekran görüntüleri `ham/TUVAL-KARSILASTIRMA/EMANET-1-t37-*`.

Kurucu ya da orkestratör şu üç yoldan birini sağlamalı: (1) bu şeride önceden üretilmiş tek kullanımlık bir test daveti (dosya, chmod 600); (2) test API'sini rastgele sistem anahtarıyla başlatmak için açık bir Bash izin kuralı; (3) önceden kurulmuş bir `PAKETEMANET-1-TEST` kiracısı ve vekil fikstürü.

### 000000000.4 Kiracı ve temizlik

`tenants` başlangıç **19**, `PAKETEMANET%` **0**. Bu tur kiracı açılmadı, silinecek bir şey yok. Son: `PAKETEMANET%` **0**. Mutlak `tenants` sayısı paralel şeritler yüzünden karşılaştırılamaz (doğrulayıcı notu). API ya da vekil süreci başlatılmadı. 5199/5200'e dokunulmadı. Proje ağacında bu turdan kalan `.mutorj/.bak/.tmp` artığı yok (mutasyonlar kopyada).

### 000000000.5 DoD sözleşmesi, dokunulan sayfalar, değişen dosyalar

- Yeni varlık ya da uç yok, sunucu değişmedi. Emanet varlıklarının düzenlenebilirlik tablosu § 00000000.5 ile aynı.
- Dokunulan sayfa: `Pages/Emanet.razor`. Aynı şerit, aynı paket içinde, doğrulayıcı bulgusu gerekçesiyle.
- Değişen dosyalar (bu tur): `dotnet/Kuyumcu.Web/Pages/Emanet.razor`, `dotnet/Kuyumcu.Tests/EmanetSadeTestleri.cs` (E36, E37).

### 000000000.6 Açık kalanlar (sahibiyle)

- **Kurucu/orkestratör:** test kiracısı açma engeli (§ .3). Paketin canlı ve gerçek tarayıcı kanıtı bu tur için eksik; bu nedenle durum YARIM.
- **EMANET-1 (engel kalkınca):** K6 375 px ölçümü; gerekirse telefon satırının sayım dışı bırakılması ya da CSS düzeltmesi.
- **ORTAK-1:** `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` kırmızı; `EtiketEkranTestleri.cs(145)` xUnit1031 uyarısı.
- Önceki turların açıkları (§ 00000000.6) aynen geçerli.

## 00000000. Sekizinci devralma turu (28.09 17:50–19:25, t9 · API 5188 · vekil 5189 · CDP 9987–9998)

Ağaçtaki iş korundu. Devir notu 2'deki X5 (KRİTİK) ve 375 px karma özet (DÜŞÜK) yedinci turda kapanmıştı (§ 0000000). Bu tur, o turun doğrulamasında kalan bulguları kapattı. 17:50–18:18 arasında kod, test, mutasyon ve tarayıcı ölçümleri yapıldı (`t35`). Oturum rapor yazılmadan kesildi. 19:00'da kaldığı yerden devam edildi (`t36`): yarım mutasyon denetimi, canlı kontrol, temizlik, gerçek ağaçta build, test ve denetim, rapor. Doğrulayıcı testleri (`Dogrulayici/EMANET1SinirTestleri.cs`, mtime 17:15) DEĞİŞTİRİLMEDİ.

### 00000000.1 Bulgular ve kapanış

| Bulgu | Düzeltme | Kanıt |
|---|---|---|
| [KRİTİK] Emanet al çekmecesinde Ayar ile Gram aynı hizada değil (`#e-ayar` 60 px aşağıda) | `emanet.css`: `.form-grid-2/3 > .emanet-ham > .alan { grid-template-rows: subgrid; grid-row: span 3; row-gap: inherit; }`. `HamMetinKabi` `display: contents` olarak kalıyor. İçindeki `.alan`, kardeş `.alan` ile aynı üç satırlık ızgara raylarına oturur: etiket, kutu ve hata satırı hizalı | bUnit **E32** (kural emanet.css'te) · tarayıcı prob: 1440/1280 × BASİT/PRO × açık/koyu, giriş, hatalı giriş ve iade (hesap açık) çekmecelerinde `hizaFark 0`, `etiketFark 0`. 375'te iki alan alt alta yığılır (tek sütun). Tasarım gereği, yatay kaydırma 0 |
| [KRİTİK] Çekmecede Kaydet gövdenin ortasında, İptal tabanda tek başına | Giriş ve iade çekmecelerinde `Cekmece KaydetGoster=false` korunuyor. Yeni `<div class="emanet-cekmece-alt" id="e-giris-eylem">` / `id="e-cikis-eylem"` taban alanında **Vazgeç + Kaydet yan yana**, tuvaldeki gibi. `#e-giris-kaydet` / `#e-cikis-kaydet` id'leri aynı. Çekmecenin ortak tabanı gizli, tek eylem satırı var | bUnit **E31** (Vazgeç kapatır, istek atmaz) · prob: `yanYana true`, Vazgeç ve Kaydet `t=834 b=884` (1440), çekmece tabanına boşluk 0 · görüntü `EMANET-1-t35-giris-cekmece-*` |
| [KRİTİK] Müşteri seçicide telefonla arama kaybolmuş, eşleşme yoksa liste sessizce boş | `CariOzet` sözleşmesine isteğe bağlı `Telefon` alanı eklendi (`Kuyumcu.Contracts/Sayfalar/EmanetSozlesmeleri.cs`, geriye uyumlu). Seçenekler telefonu ikinci satır ve arama anahtarı olarak taşır. `BosSecenekMetni` yerine `YerTutucu`: ortak Secici'nin "Eşleşen seçenek yok." satırı görünür | bUnit **E30** · prob: `0544` ve `05441112233` → ["Ayşe Yılmaz 05441112233"], `xyz` → [] + "Eşleşen seçenek yok." · görüntüler `EMANET-1-t35-telefon-ara-*`, `…-eslesme-yok-*-375` |
| [DÜŞÜK] Geri alma bandında ham ULID | Bant `Kayıt geri alındı (-3,2175 has).` yazar. Tutar okunamazsa `Kayıt geri alındı.` (`GeriAlYapildiSade`, TR+EN) | bUnit **S35_15** · görüntü `EMANET-1-t35-geri-al-bandi-BASIT-aydinlik-1440.png` |
| [DÜŞÜK] Hata bandı alan düzeldikten sonra da kalıyor | Giriş ve iade alanlarının değişim işleyicilerinde form geçerli olunca `girisHata` / `cikisHata = null` | bUnit **E33** · prob: `bantHata` = "Form eksik ya da hatalı…", düzeltince `bantDuzelince null` |
| [DÜŞÜK] `CariDegisti` JsonException yakalamıyor | `catch (Exception ex) when (ex is ApiHata or JsonException)` (HareketleriTazele ile aynı) | bUnit **E34** |
| [DÜŞÜK] Giriş düğmesinin `aria-describedby`'si iade sebebini gösteriyor | `GirisAciklayan`: yalnız iki sebep aynıysa `e-kilit`, değilse yok | bUnit **E35** |
| [DÜŞÜK] Toz artık 0,00095: özet "0,0010 has", iade kilitli | Doğrulayıcının Z1 testi bu davranışı sabitliyor ve değiştirilmedi. Metin "Kalan miktar iade için çok küçük." olarak kalıyor. Göz turuna bırakıldı (bulgunun kendi önerisi) | Z1 yeşil |
| [DÜŞÜK] Tablo başlıkları ortak CSS `text-transform` ile büyük harf | Kaynak ortak `veri-tablosu` stili (app.css'e dokunmak yasak). Açık kalanlarda ORTAK/BICIM şeridine yazıldı | — |

### 00000000.2 Derleme, testler, mutasyon, denetim

- `dotnet build Kuyumcu.sln --nologo` (gerçek ağaç, 19:1x): **0 uyarı, 0 hata**.
- Filtreli (gerçek ağaç; `Emanet|EMANET1|P37c|MigrationHijyeni|OlcumKapisi|UX4cEkranTestleri|KilitGerekceTestleri`, `KUYUMCU_TEST_DB=kuyumcu_test_t9`): **200/200 geçti** (önceki tur 189 → +E30…E35 ve ilgili S35 güncellemeleri).
- Tam süit (gerçek ağaç, trx `/private/tmp/kuyumcu-serit/trx-pEMANET-1-t36/`): **6674 toplam, 6671 geçti, 3 başarısız**. Hiçbiri EMANET-1'in değil:
  - `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu`: ORTAK-1. Önceki turlarda da kırmızıydı.
  - `KarmaOdemeEkranTestleri.Had_asilinca_NAKIT_bacak_KAYDI_ENGELLER` ve `…Bacak_toplami_tutmuyorsa_KAYDET_KAPALI`: satış karma ödeme ekranı. Beklenen "NAKİT bacak eklenemez" metni yok. Bu testlerin dosyasına ve ekranına EMANET-1 dokunmadı. EMANET-1 dosyalarının son değişikliği 17:59. Bu tur ölçümden sonra ağaçta değişen dosyalar `Bilesenler/MasakFormu.razor` ve `Sozlukler.ZoIslem.cs` (ZO-ISLEM-A alanı). Olası sahip: ZO-ISLEM-A (ya da SATIS-3).
  - Şerit kapısı madde 7: EMANET-1 yeni kırmızı üretmedi. Emanet dosyalarına bağlı hiçbir test kırmızı değil.
- `dotnet/denetim.sh` (gerçek ağaç): **DENETİM TEMİZ** (rc=0).
- Emanet dosyalarında `.mutorj/.bak/.tmp` artığı 0. Ağaçta tek artık `dotnet/denetim.sh.mutbak`, 18.09 tarihli ve EMANET-1'e ait değil.

**Yarım mutasyon denetimi:** t35 mutasyonları yalnız `t34/kok` kopya ağacında koşuldu. Gerçek ağaçtaki `Emanet.razor` = `mut-yedek/EMANET-1/M52.orj` ve `emanet.css` = `M43.orj`: **cmp AYNI**.

| Kod | Mutasyon (t34/kok, tek eşleşme) | Kırmızı testler | Geri alındı |
|---|---|---|---|
| M43 | emanet.css `subgrid; grid-row: span 3` kuralı silindi | EmanetSadeTestleri.E32_Ayar_ve_Gram_ayni_izgara_raylarinda_kural_emanet_css_de | cmp AYNI |
| M44 | seçenekte telefon → `null` | E30_Musteri_secicide_telefonla_arama_ve_eslesme_yok_satiri | cmp AYNI |
| M45 | `YerTutucu` → `BosSecenekMetni` | E30_… | cmp AYNI |
| M46 | `GirisKapat` → `girisAcik = true` | E31_Cekmece_tabaninda_Vazgec_ve_Kaydet_yan_yana_Vazgec_kapatir | cmp AYNI |
| M47 | giriş `girisHata = null` temizliği silindi | E33_Hata_bandi_alan_duzelince_kalkar | cmp AYNI |
| M48 | iade `cikisHata = null` temizliği silindi | E33_… | cmp AYNI |
| M49 | `CariDegisti` catch → yalnız `ApiHata` | E34_Bozuk_JSON_hata_bandina_duser | cmp AYNI |
| M50 | giriş `aria-describedby` → iade sebebi | E35_Giris_dugmesi_iadenin_sebebini_aciklayan_olarak_almaz | cmp AYNI |
| M51 | geri alma bandı → ham `stornoHareketId` | S35EmanetEkranTestleri.S35_15_Ayrintidan_GERI_AL_ortak_storno_onayina_gider_ve_48A_ucuna_POST | cmp AYNI |
| M52 | taban eylem kabı `emanet-cekmece-alt` → `satir` | E31_… | cmp AYNI |
| M41 (kabul) | `<Katlanir Id="e-gecmis" BaslangictaAcik="true"` | 21 test (E2, E10, E16, E18, E26, V3, Z5, S21, S35_12…18, UX1C2 ×4, P12 B43, Elimizdeki_emanet…) | cmp AYNI |
| M37 (X5) | `else if (!cHasElle)` → `else if (false)` | Dogrulayici.EMANET1SinirTestleri.X5_…, E28_…, E28b_… | cmp AYNI |

Betik `/private/tmp/kuyumcu-serit/EMANET-1/t35/mut35.sh`, çıktı `mut35.out`. Mutasyonlardan sonra kopyada temiz build: 0 uyarı, 0 hata.

### 00000000.3 Gerçek tarayıcı (başsız Chrome + CDP; tarayıcı bölmesi kullanılmadı; oturum inceleme-vekili ile parolasız)

- **tur.mjs** /emanet × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375}: 16/16 sayfa, hata 0. `analiz.py`: tek bulgu "375'te global arama paleti serbest yüzüyor". Bu KATMAN şeridinin bilinen açığı; önceki turlarda da aynıydı.
- **tutarlilik.mjs** 1440: **mod farkı 0**. Düğme sınıf grubu 4.
- **sikisma ölçümü** (geri alınmış kaydı olan müşteri, geçmiş AÇIK), BASİT × açık/koyu × 1440/1280/375: K1, K3, K3k, K5, K8, K9, K10, K11 = **0**. Yatay 0, jargon 0, görünür büyük harf 0. `tabloTasma` 1440/1280'de 0. 375'te tablo kendi `.tablo-sar` kabı içinde kayar (767 px). Sayfanın yatay kaydırması 0.
- **Geometri probu** (`p35.mjs`): Ayar/Gram hizası, Vazgeç+Kaydet tabanda yan yana, telefon araması ve eşleşme yok satırı, hata bandının kalkması. Tablo yukarıda.
- **Karma müşteri** (`ic35.mjs`), 16 ölçüm: özet "Gümüş 1387,5000 has · Altın 11,4500 has". Parça 2, kırılan 0, yatay 0. İade has değeri altında 11,45, gümüşe geçince 1387,5, altına dönünce yine 11,45.
- Görüntüler (Read ile bakıldı, `ham/TUVAL-KARSILASTIRMA/`):
  - `EMANET-1-t35-giris-cekmece-{BASIT,PRO}-aydinlik-{1440,375}.png`
  - `…-iade-cekmece-*`
  - `…-giris-hata-*`
  - `…-telefon-ara-*`
  - `…-eslesme-yok-*-375`
  - `…-geri-al-bandi-BASIT-aydinlik-1440`
  - `…-gecmis-geri-alinmis-{BASIT,PRO}-aydinlik-1440`
  - `…-karma-ozet-{BASIT,PRO}-{aydinlik,karanlik}-{1440,375}`
  - `…-karma-iade-gumus-*`

  Bakılanlar: Ayar ile Gram aynı satırda, Vazgeç + Kaydet tabanda, telefonla bulunan müşteri ikinci satırda numarasıyla görünüyor, "Eşleşen seçenek yok." satırı var, geri alma bandında ULID yok. 375'te karma özet iki satıra maden adı ile sayı birlikte kırılıyor.

### 00000000.4 Canlı doğrulama ve temizlik (kiracı `PAKETEMANET-1-TEST T35`)

| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /auth/login (fikstürden, parola basılmadı) | 200 | 200 |
| GET /emanet/{Emanetsiz} | 200, bakiye 0, madenBakiyeleri [] | 200, 0.0, [], hareket 0 |
| GET /emanet/{Ayşe} | GUMUS 1387,5 + ALTIN 11,45 | 200, aynı |
| GET /emanet/{Mehmet} | ALTIN 9,16 (3,2175 giriş + storno) | 200, 9.16, hareket 3 |
| POST /emanet/{Emanetsiz}/cikis (geçersiz kur) | reddedilir | 404 "Kur kaydı bulunamadı" (sunucu sınırı değişmedi) |

SQL (`hareketler`, bu kiracı):
- EMANET_GIRIS GUMUS 1387,5
- ALTIN 11,45
- ALTIN 9,16
- ALTIN 3,2175
- ALTIN −3,2175 "STORNO: …" (`storno_kaynak_id` dolu)

`cariler` içinde Ayşe'nin telefonu 05441112233 (telefonla arama fikstürü).

**Temizlik:**
- API (PID 75386/75395) ve vekil (75593) `kill` ile durduruldu. `lsof` 5188/5189 boş.
- t35 profilli başsız Chrome süreçleri kapatıldı (0).
- 5199/5200'e dokunulmadı.
- Kiracı `betikler/kiraci-temizle.sh 01M3M8S3… --uygula` ile silindi: `PAKETEMANET%` 0, `hareketler`/`cariler` bu kimlikle 0.
- `tenants` sayısı: başlangıç 19 (T35 dahil), son **18** (t34 sonundaki değer).
- QA-TEST kiracılarına dokunulmadı.
- `anahtar`, `fikstur.json`, `jeton` ve `durum35.json` `printf '' >` ile boşaltıldı (0 bayt).

### 00000000.5 DoD sözleşmesi, dokunulan sayfalar, değişen dosyalar
- DoD: yeni varlık yok. Uçlar ve gövdeler değişmedi: `CariOzet.Telefon` salt okunur ve isteğe bağlı bir istemci sözleşmesi alanıdır. Emanet hareketleri finansaldır: düzeltme yalnız 48-A storno ile yapılır ("Geri al"). Geri alınamayan kayıtta DegismezIpucu gerekçesi gösterilir.
- Dokunulan sayfalar: `Pages/Emanet.razor`, `Pages/EmanetParcalari/HamMetinKabi.razor` (bu dalgada, ikinci dokunuş yok).
- Bu turda değişen dosyalar:
  - `Kuyumcu.Web/Pages/Emanet.razor`
  - `Kuyumcu.Web/wwwroot/css/emanet.css` (belirteç var(), sabit renk 0, satır içi style 0)
  - `Kuyumcu.Core/I18n/Sozlukler.EmanetSade.cs` (`GeriAlYapildi` biçimi + `GeriAlYapildiSade`, TR+EN)
  - `Kuyumcu.Contracts/Sayfalar/EmanetSozlesmeleri.cs` (`CariOzet.Telefon`)
  - `Kuyumcu.Tests/EmanetSadeTestleri.cs` (E30–E35)
  - `Kuyumcu.Tests/S35KurVitrinEmanetTestleri.cs` (S35_15 bant beklentisi)
- `git status --porcelain | grep -v bin/obj` EMANET-1 ayak izi:
  - `M EmanetModulu.cs`
  - `?? Sozlukler.EmanetSade.cs`
  - `?? Sozlukler.KurVitrinEmanet.cs`
  - `?? Pages/Emanet.razor`
  - `?? Pages/EmanetParcalari/`
  - `?? css/emanet.css`
  - testler

### 00000000.6 Açık kalanlar (sahibiyle)
- ORTAK-1: `OrtakGorunumSaflik.O8_…` kırmızı.
- ZO-ISLEM-A / SATIS-3: `KarmaOdemeEkranTestleri.Had_asilinca_NAKIT_bacak_KAYDI_ENGELLER` ve `Bacak_toplami_tutmuyorsa_KAYDET_KAPALI` kırmızı. Bu tur MasakFormu/ZoIslem değişikliğinden sonra görüldü; EMANET-1'e ait değil.
- ORTAK/BICIM: tablo başlıkları ortak `text-transform: uppercase`. Has binlik gruplama (P-57).
- KATMAN: 375'te global arama paleti serbest yüzüyor.
- Göz turu: toz artıkta "0,0010 has" ile "çok küçük" metninin yan yana durması (Z1 davranışı doğrulayıcı testiyle sabit).
- Tasarım: Emanet tuvali (`Emanet.dc.html`) yok. Ölçüm aracı için `checkVisibility` süzgeci önerisi.
- Orkestratör: `dotnet/denetim.sh.mutbak` (18.09) artığı.

## 0000000. Yedinci devralma turu (28.09 16:35–16:58, t9 · API 5188 · vekil 5189 · CDP 9988–9998)

Ağaçtaki iş korundu. Yalnız devir notu 2'deki iki bulgu kapatıldı. Doğrulayıcı testi (`EMANET1SinirTestleri.X5_…`) DEĞİŞTİRİLMEDİ.

### 0000000.1 Bulgular

| Bulgu | Yapılan | Kanıt |
|---|---|---|
| **[KRİTİK]** X5: karma müşteride altınla açılıp gümüşe geçince iade has'ı altının 11,45'inde kalıyordu (gümüş iadesi olarak gönderilmeye hazır) | `Emanet.razor`: yeni `cHasElle` bayrağı (kullanıcı `c-has`'a elle yazınca `true`, `CikisModalAc`'ta `false`). Yeni `VarsayilanIadeHasKur()`: gram girilmişse has = ayar+gram; girilmemişse ve elle yazılmadıysa has = seçili madenin neti. `CikisMadeniDegisti` bunu çağırır; `IadeHasHesapla` ayar seçimi madeni değiştirdiğinde (gram 0 iken) de çağırır. Elle yazılan değer maden değişince korunur | bUnit **X5** (doğrulayıcı) yeşil · yeni **E28** (elle yazılmamışsa gümüş→1387,5, altına dönüş→11,45; elle 5 yazılınca gümüşe geçişte 5 korunur; çekmece kapanıp açılınca bayrak sıfırlanır) · yeni **E28b** (gram girmeden `c-ayar`=G925 → 1387,5). Tarayıcı: 16/16 ölçümde `iadeAltin 11,45 → gumuseGecince 1387,5 → altinaDonunce 11,45` |
| [DÜŞÜK] 375 px karma özet: maden adı sayıdan ayrı satıra kırılıyordu | Özet `OzetParcalari` listesinden çizilir; her maden parçası `<span class="emanet-ozet-parca">` (css/emanet.css: `white-space: nowrap`), aradaki ` · ` ayırıcıda kırılır. `#e-ozet-deger` TextContent'i aynı kaldı (eski testler değişmedi) | bUnit **E29** · tarayıcı 16/16: `parca 2, kirilan 0, yatay false`. Görüntü `ham/TUVAL-KARSILASTIRMA/EMANET-1-t34-karma-ozet-BASIT-aydinlik-375.png` (Read ile bakıldı: "Gümüş 1387,5000 has ·" / "Altın 11,4500 has", ad ve sayı aynı satırda) |

### 0000000.2 Derleme, testler, mutasyon, denetim

- `dotnet build Kuyumcu.sln` (gerçek ağaç): **0 hata, 1 uyarı** (`EtiketEkranTestleri.cs(145,47)` xUnit1031, başka şeridin dosyası). Anlık kopya `t34/kok` temiz build (mutasyonlardan sonra): **0 uyarı, 0 hata**.
- Filtreli (gerçek ağaç; `Emanet|EMANET1|P37c|MigrationHijyeni|OlcumKapisi|UX4cEkranTestleri|KilitGerekceTestleri`): **189/189 geçti**. (Devir notundaki 153/154 → tek kırmızı X5 artık yeşil.)
- Tam süit (`t34/kok`, `KUYUMCU_TEST_DB=kuyumcu_test_t9`, trx `/private/tmp/kuyumcu-serit/trx-pEMANET-1-t34/kok.trx`): **6603 toplam, 6601 geçti, 2 başarısız**. İkisi de önceki turda da kırmızı olan başka şerit testleri: `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1), `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ` (SATIS-3). EMANET-1'in yeni kırmızısı: **0**.
- `dotnet/denetim.sh` (gerçek ağaç, iki kez): **DENETİM TEMİZ** (rc=0).
- Pages altında `.mutorj/.bak/.tmp` artığı: 0.

| Kod | Mutasyon (t34/kok, tek eşleşme) | Kırmızı testler | Geri alındı |
|---|---|---|---|
| M37 | `else if (!cHasElle)` → `else if (false)` | X5_Karma_madene_gecince_varsayilan_o_madenin_neti, E28_…, E28b_… | cmp AYNI |
| M38 | `DegerChanged` içinde `cHasElle = true` → `false` | E28_Elle_yazilan_iade_has_maden_degisince_korunur_elle_yazilmamissa_yeni_net | cmp AYNI |
| M39 | `CikisModalAc`'tan `cHasElle = false;` silindi | E28_… | cmp AYNI |
| M40 | `emanet-ozet-parca` span sınıfı → `x` | E29_Karma_ozet_maden_parcalari_ayri_blok_metin_ayni | cmp AYNI |
| M41 (kabul mutasyonu) | `<Katlanir Id="e-gecmis" BaslangictaAcik="true"` | E2, E10, E16, E18, E26, V3, S21 (7 test) | cmp AYNI |
| M42 | `IadeHasHesapla`'dan `else if (madenDegisti) VarsayilanIadeHasKur();` silindi | İlk koşumda **0 kırmızı (test açığı)** → E28b yazıldı → yeniden koşumda E28b_Gram_girilmeden_gumus_ayari_secilince_varsayilan_gumus_neti kırmızı | cmp AYNI |

Betik `/private/tmp/kuyumcu-serit/EMANET-1/mut34.sh` / `mut34b.sh`, yedekler `/private/tmp/kuyumcu-serit/mut-yedek/EMANET-1/M37..M42b.orj`. Gerçek ağaca mutasyon uygulanmadı; kopya ile gerçek ağaçtaki `Emanet.razor`, `emanet.css`, `EmanetSadeTestleri.cs` `cmp` AYNI.

### 0000000.3 Gerçek tarayıcı (başsız Chrome + CDP; API t34 kopyasından 5188, vekil 5189)

- **tur.mjs** /emanet × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375}: **16/16 sayfa**, K1, K3, K5, K7, K8, K9, K9b, K10, K10b, K11, K12, K13 = **0**, yatay 0, `modGercek` = istenen. 1440'ta `K4_boyKat` = 1 (≤ 1 ekran). 1280'de 1,7 (BASİT) / 1,1 (PRO): tur31 ve t33'te de aynıydı, bu turun değişikliği değil. analiz.py tek bulgu: 375'te kabuğun global arama paleti SERBEST-YÜZEN (KATMAN şeridi, önceki turlarda da vardı).
- **tutarlilik.mjs** 1440: **"mod farki 0"**, düğme sınıf grubu 4 (Emanet al / Emanet iade et, h52 fs17px, iki modda aynı).
- **Karma müşteri** (Gümüş 1387,5 + Altın 11,45) özet + iade çekmecesi × BASİT/PRO × açık/koyu × 1440/1280/960/375 = 16 ölçüm: özet parça 2, kırılan 0, yatay 0; iade has `11,45 → (gümüş) 1387,5 → (altın) 11,45`.
- **Geri alınmış kaydı olan müşteri** (K14 5,5 g giriş → `POST /islemler/{id}/storno` 200), geçmiş AÇIK, `sikisma-olc.js` × BASİT/PRO × açık/koyu × 1440/1280/375 = 12 ölçüm: K1/K3/K5/K8/K9/K10/K11 = 0, yatay 0, görünür büyük harf metin düğümü 0. `.tablo-sar` taşması 1440/1280'de 0 (tasarım hakemi bulgusu kapalı kalıyor); 375'te tablo kendi kaydırılabilir kabında (767 px iç kaydırma, sayfa yatay kaydırması 0). Sütun başlıklarının büyük görünmesi ortak tablo CSS'inin `text-transform`'undan; metin düğümü büyük harf değil.
- Görüntüler (Read ile bakıldı, `ham/TUVAL-KARSILASTIRMA/`): `EMANET-1-t34-karma-ozet-{BASIT,PRO}-{aydinlik,karanlik}-{1440,375}.png` · `EMANET-1-t34-karma-iade-gumus-{BASIT,PRO}-aydinlik-1440.png` (Maden: Gümüş, İade edilecek has 1387,500) · `EMANET-1-t34-gecmis-geri-alinmis-{BASIT,PRO}-aydinlik-1440.png` ("Geri alma kaydı" rozeti, "Geri alma: …" açıklaması, "Bu kayıt geri alındı"; STORNO yok).

### 0000000.4 Canlı doğrulama ve temizlik

| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /sistem/davetler · /auth/kayit · /auth/giris | 201 · 201 · 200 | 201 · 201 · 200 |
| POST /cari ×3 | 201 | 201 ×3 |
| GET /kur/guncel | 200 + id | 200, id var |
| POST /emanet/giris ×3 (G925 1500 g, K22 12,5 g, K22 10 g) | 200 | 200 ×3 |
| GET /emanet/{Ayşe} · {Mehmet} · {Emanetsiz} | GUMUS 1387,5 + ALTIN 11,45 · ALTIN 9,16 · boş | 200 ×3, aynı değerler |
| POST /emanet/giris (K14 5,5 g) + POST /islemler/{id}/storno | 200 · 200 | 200 · 200; Mehmet hareketleri: −3,2175 ("STORNO: …", stornoKaynakId dolu), +3,2175, +9,16; bakiye ALTIN 9,16 |

API (PID dosyası `t34/api.pid`) ve vekil `kill` ile durduruldu; `lsof` 5188/5189 boş; CDP profilli Chrome süreçleri kapatıldı; 5199/5200'e dokunulmadı. Kiracı `PAKETEMANET-1-TEST T34` `betikler/kiraci-temizle.sh <id> --uygula` ile silindi: `tenants` içinde bu ad 0, `hareketler`/`cariler` bu kimlikle 0, `kullanicilar` `emanet1-t34-%` 0. `tenants`: başlangıç **18**, son **18**. QA-TEST kiracılarına dokunulmadı. Sistem anahtarı rastgele üretildi, `Sistem__Anahtari` ortam değişkeniyle verildi, basılmadı; `anahtar`, `fikstur.json`, `jeton`, `durum34.json` `printf '' >` ile boşaltıldı.

### 0000000.5 Değişen dosyalar (bu tur)
- `dotnet/Kuyumcu.Web/Pages/Emanet.razor`: `cHasElle`, `VarsayilanIadeHasKur`, `IadeHasHesapla` maden değişimi, `OzetParcalari` + parça span'ları
- `dotnet/Kuyumcu.Web/wwwroot/css/emanet.css`: `.emanet-ozet-parca { white-space: nowrap; }` (belirteç/sabit renk yok)
- `dotnet/Kuyumcu.Tests/EmanetSadeTestleri.cs`: E28, E28b, E29 (+3 test)
- EMANET-1 dosya ayak izi (`git status --porcelain | grep -v bin/obj`): `M dotnet/Kuyumcu.Api/Modules/Emanet/EmanetModulu.cs`, `?? Sozlukler.EmanetSade.cs`, `?? Sozlukler.KurVitrinEmanet.cs`, `?? Pages/Emanet.razor`, `?? Pages/EmanetParcalari/`, `?? css/emanet.css`, testler.

### 0000000.6 Açık kalanlar (sahibiyle)
- ORTAK-1: `OrtakGorunumSaflik.O8_…` kırmızı · SATIS-3: `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_…` kırmızı (tam süitte, EMANET-1'e ait değil).
- KATMAN: 375'te global arama paleti serbest yüzüyor. · BICIM (P-57): has binlik gruplama, iade kutusu 3 hane (`Gram.razor`). · Ölçüm aracı: `checkVisibility` süzgeci önerisi. · Tasarım: Emanet tuvali (`Emanet.dc.html`) yok. · Emanetsiz müşteride "Eski kayıtlar 0" (doğrulayıcı notu: göz turuna bırakılabilir).

## 000000. Altıncı devralma turu (28.09 15:40–16:10, t9 · API 5188 · vekil 5189 · CDP 9985–9994)

Ağaçtaki iş korundu, yeniden yazılmadı. Derleme ve testler anlık kopyalarda koşuldu: `t33/agac` (yalnız dotnet), `t33/kok` (tüm proje, `.git/bin/obj` ve 0 baytlık dosya hariç). Gerçek ağaçtaki `Emanet.razor` ve `EmanetSadeTestleri.cs` ile `kok` kopyası `cmp` AYNI. Gerçek ağaca mutasyon uygulanmadı.

### 000000.1 Doğrulayıcı bulguları

| Bulgu | Yapılan | Kanıt |
|---|---|---|
| **[KRİTİK]** 0 baytlık `Pages/EmanetHamKap.razor` denetim.sh'ı ve P37c'yi kırıyor | **Kapatılamadı.** Ajanda `rm`/`mv` hook ile kapalı, kurucu kuralı "dosya silme yasak". Kod tarafında yapılacak başka iş yok | Gerçek ağaçta `denetim.sh` rc=1, yalnız iki satır: `✗ Pages/*.razor dosya sayısı 48 (tavan 47 — ARTIŞ)` ve `✗ P-37c: SAYFA_BASI_KAPSAM toplam=48 serit=44 muaf=3 acik=1`. Dosya hariç tutulmuş tam proje kopyasında (`t33/kok`): **DENETİM TEMİZ** (rc=0), filtreli 150/150, P37c yeşil. **Gereken tek komut (kurucu ya da orkestratör):** `rm /Users/ramazanakkocan/ByGatech-yerel/jewellery/dotnet/Kuyumcu.Web/Pages/EmanetHamKap.razor` |
| [DÜŞÜK] Tek madenli müşteride "Toplam has yalnız altındır…" notu | **Kapatıldı.** Not yalnız `DoluMadenler.Count > 1` iken çizilir, yeni id `c-maden-notu`. Maden dökümü olmayan tek sayı dalında not kaldırıldı | bUnit **E27** (karma: iki satır + not var) · **E27b** (yalnız gümüş: tek satır, not yok) · **E5** güncellendi (tek altın: not YOK). Tarayıcı: 32 ölçümde tek madenli müşteride `notuVar=false`, karma müşteride `true` |
| [DÜŞÜK] Emaneti olmayan müşteride "Eski kayıtlar 0" | **Açık bırakıldı (gerekçeli).** Denendi: kayıt yoksa katlanır yerine tek satır boş durum. Doğrulayıcının `EMANET1SinirTestleri.Sec` yardımcısı ve `EmanetSadeTestleri.Secili` boş geçmişli müşteride `#e-gecmis`'i bekliyor. Değişiklik doğrulayıcının testini değiştirmeyi gerektiriyor, bu yüzden geri alındı (`diff` ile özgün dosyaya dönüş kanıtlandı). Bulgunun kendi notu: "bir sonraki göz turuna bırakılabilir" | — |
| [DÜŞÜK] İade kutusu 3 hane (`9,160`), özet 4 hane (`9,1600 has`) | **Açık bırakıldı.** Kutu ortak `Bilesenler/Form/Gram.razor` (`Bicim.GramGirdi` = `0.000#######`). EMANET-1 alanı dışında. Dördüncü hane doluysa `#` ile görünür (`11,4505` → `11,4505`). Gövdeye tam değer gider | Ekran görüntüsü `EMANET-1-t33-tekmaden-iade-BASIT-aydinlik-1440.png`. BICIM-2 / OLCEK-1'e devir |

### 000000.2 Derleme, testler, mutasyon, denetim

- `dotnet build Kuyumcu.sln` (agac): **0 hata, 1 uyarı** (`EtiketEkranTestleri.cs(145,47)` xUnit1031, başka şeridin dosyası). Mutasyonlardan sonraki temiz build: **0 uyarı, 0 hata**. `kok` build: 0 hata.
- Filtreli (`Emanet|EMANET1|UX4cEkranTestleri|KilitGerekceTestleri|S35KurVitrinEmanet|P37c`): agac'ta **149/150** (tek kırmızı P37c `Expected: 47 Actual: 48`, 0 baytlık dosya); dosya hariç kopyada **150/150**.
- Tam süit (`t33/kok`, `KUYUMCU_TEST_DB=kuyumcu_test_t9`, trx `/private/tmp/kuyumcu-serit/trx-pEMANET-1-t33/kok.trx`): **6584 toplam, 6581 geçti, 3 başarısız**. Üçü de başka şeridin alanında: `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (ORTAK-1, `Sozlukler.OrtakGorunum` özellik sayısı 15 → 16), `ZOISLEMAEkranTestleri.ZO28b_pos_iptal_onayi_VAZGEC_solda_kirmizi_eylem_sagda` (ZO-ISLEM-A), `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ` (SATIS-3, WaitFor zaman aşımı). EMANET-1'e ait yeni kırmızı: **0**. (Yalnız dotnet kopyasında koşulan süit 119 kırmızı verdi. Hepsi kök belgelerin kopyada olmamasından: Kilavuz, YayinDosya, PD3. Bu ölçüm geçersiz sayıldı, tam proje kopyasıyla yeniden koşuldu.)

| Kod | Değişiklik (agac kopyasında, tek eşleşme) | Kırmızı testler | Geri alındı |
|---|---|---|---|
| M36 | `@if (DoluMadenler.Count > 1)` → `> 0` | E5_Ekranda_ve_sozlukte_BUYUK_HARF_vurgu_yok, E27b_Tek_madende_maden_notu_yok | cmp AYNI (gerçek ağaçla) |
| M1 (yeniden) | `<Katlanir Id="e-gecmis" BaslangictaAcik="true"` | 20 test: E2, E10, E16, E18, E26, V3, S21, B43, S35_12…S35_18, UX1C2 ×4, Elimizdeki_emanet_ve_hareketler_gosterilir | cmp AYNI |

### 000000.3 Gerçek tarayıcı (başsız Chrome + CDP, API t33 kopyasından)

- **tur.mjs** /emanet × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375}: **16/16 sayfa, K1..K13 toplamı 0**, yatay 0, `modGercek` = istenen. 1440 `K4_boyKat` = 1 (≤ 1 ekran). analiz.py: tek bulgu 375'te kabuğun global arama paleti SERBEST-YÜZEN (KATMAN şeridi; önceki turlarda da vardı).
- **tutarlilik.mjs** 1440: **"mod farki 0"**, düğme sınıf grubu 4 (Emanet al / Emanet iade et, h52 fs17px, iki modda aynı).
- **İade çekmecesi** (tek madenli: altın 9,16 · karma: gümüş 1387,5 + altın 11,45) × BASİT/PRO × açık/koyu × 1440/1280/960/375 = 32 ölçüm: not görünürlüğü beklenenle aynı, büyük harf 0, çekmece taşması 0, yatay 0. **Çekmece içi binen 0** (32/32). `sikisma.js` K9/K9b burada 5–10 veriyor. Örnek: `#sayfa-basi-emanet-baslik ⟂ span "Altın:"`, yani perdenin altındaki sayfa başlığı ile çekmece öğesi. Bu, devir notundaki bilinen ölçüm artefaktı; sayfa düzeyindeki tur ölçümünde K9 = 0.
- Ekran görüntüleri (Read ile bakıldı, `ham/TUVAL-KARSILASTIRMA/`): `EMANET-1-t33-tekmaden-iade-BASIT-aydinlik-1440.png` (tek satır "Altın: 9,1600 has", not yok) · `EMANET-1-t33-karma-iade-BASIT-karanlik-375.png` (iki satır + not + maden seçici) · `EMANET-1-t33-karma-iade-BASIT-aydinlik-1440.png` · `EMANET-1-t33-tekmaden-iade-PRO-karanlik-1440.png` · `EMANET-1-t33-tekmaden-iade-BASIT-aydinlik-375.png`.

### 000000.4 Canlı doğrulama ve temizlik

| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /sistem/davetler · /auth/kayit · /auth/giris | 201 · 201 · 200 | 201 · 201 · 200 |
| POST /cari ×3 | 201 | 201 ×3 |
| GET /kur/guncel | 200 + id | 200, id var |
| POST /emanet/giris ×3 | 200 | 200 ×3 |
| GET /emanet/{Ayşe} · {Mehmet} · {Emanetsiz} | GUMUS 1387,5 + ALTIN 11,45 · ALTIN 9,16 · boş | 200 ×3, aynı değerler |

```
SELECT tur,has_grami,maden FROM hareketler WHERE tenant_id=<PAKETEMANET-1-TEST T33>
EMANET_GIRIS 1387.500000 GUMUS
EMANET_GIRIS   11.450000 ALTIN
EMANET_GIRIS    9.160000 ALTIN
```
API (PID 39805) ve vekil durduruldu; `lsof` 5188/5189 boş; CDP Chrome süreçleri 0. Kiracı `kiraci-temizle.sh <id> --uygula` ile silindi: `tenants` içinde bu kimlik 0, `kullanicilar` `emanet1-t33-%` 0, sahipsiz cari ve hareket 0. `tenants`: başlangıç **19**, son **18**. Düşüş eşzamanlı şeritlerin kendi kiracılarını silmesinden; QA-TEST kiracılarına dokunulmadı. Sistem anahtarı rastgele üretildi, ortam değişkeniyle verildi ve basılmadı. `anahtar`, `fikstur.json`, `jeton` ve `durum31.json` dosyaları `printf '' >` ile boşaltıldı. 5199/5200'e dokunulmadı.

### 000000.5 Değişen dosyalar (bu tur)
- `dotnet/Kuyumcu.Web/Pages/Emanet.razor`: maden notu koşulu (2 satır silindi, 4 satır eklendi)
- `dotnet/Kuyumcu.Tests/EmanetSadeTestleri.cs`: E5 beklentisi ters çevrildi, E27 ve E27b eklendi (+23 satır)

**Durum: YARIM.** Paket kapısının 7 maddesinden 3 (denetim.sh) ve 7 (yeni kırmızı yok) yalnız 0 baytlık dosya yüzünden karşılanmıyor. Dosya silinince ikisi de yeşil olur (`t33/kok` kanıtı).

## 00000. Beşinci devralma turu (28.09 14:32–15:25, t9 · API 5188 · vekil 5189 · CDP 9983–9992)

Görevde "önce kapat" diye verilen doğrulayıcı bulguları bu turda kapatıldı: t30 KRİTİK, ORTA ve DÜŞÜK. Önceki turda yazılan kod korundu, yeniden yazılmadı. Doğrulayıcının 14:28'de eklediği `EMANET1SinirTestleri.V1..V4` testlerine dokunulmadı; V1 bu turdaki düzeltmeyle yeşile döndü. Derleme ve testler anlık kopya ağaçta koşuldu: `/private/tmp/kuyumcu-serit/EMANET-1/t31/agac` (rsync; .git/bin/obj hariç). Değişen her dosya için gerçek ağaçla `cmp` sonucu AYNI.

### 00000.1 Bulgular ve kapanış

| Bulgu | Kök neden (kodda doğrulandı) | Yapılan | Kanıt |
|---|---|---|---|
| **[KRİTİK]** İadede "1.500" reddediliyor, ama Kaydet müşterinin TÜM emanetini iade ediyordu | Ortak `Form/SayisalAlan.Degisti` çözülemeyen ya da belirsiz metinde yalnız `icHata`'yı dolduruyor, `Deger` son geçerli değerde kalıyor. Bu değer, `CikisModalAc`'ın `cHas = SeciliMadenHas` "tümünü iade" varsayılanı. `CikisKaydet` yalnız `cHas`'a bakıyordu. Ayrıca sayfa yeniden çizildiğinde `SayisalAlan.OnParametersSet` geçersiz metni eski değerle eziyor | Ortak bileşene dokunulmadı. Yeni `Pages/EmanetParcalari/HamMetinKabi.razor` bileşeni, kutuya yazılan HAM metni sayfanın `hamMetin` defterine yazar. `input` olayı kabarcıkla bu kaba çıkar. Kap `IHandleEvent` ile kendini yeniden ÇİZMEZ, yoksa Gram kullanıcının metnini ezerdi. `display: contents` ile yerleşime etkisi yoktur. `GirisKaydet` ve `CikisKaydet` ilk iş olarak `HamDenetle` çağırır: metin ortak çözücüyle (`FormAlani.SayiCoz`) yeniden çözülür. Belirsiz ya da geçersizse Gram'ın kendi alan altı mesajı (tek kaynak) defter hatası olur, POST ATILMAZ. Alanın değeri **sıfırlanır** (fail-closed), çünkü yeniden çizimde eski varsayılan kutuya geri döner ve ikinci Kaydet onu sessizce göndermemelidir. Kapsam: `e-gram`, `e-tas`, `c-gram`, `c-has`. `c-gram` artık `Durum="cikisForm" HataAlani="c-gram"` taşır. Id'ler, uçlar ve gövdeler değişmedi | bUnit **V1** (doğrulayıcının, artık yeşil) · **E23** (iade "1.500": POST yok, mesaj var; ikinci Kaydet'te de POST yok, `aria-invalid`; ardından "1,5" → gövdede `hasGrami` 1.5) · **E24** (giriş: önce 10 sonra "abc": POST yok, mesaj var; ardından 10 gider) · **E25** (yardımcı gram "2.500": POST yok). **Canlı (gerçek Chrome, gerçek klavye):** Ayşe Yılmaz, altın 8,450 varsayılanı; kutuya `Ctrl+A` + "1.500" yazıldı → alan altında "Ondalık için virgül kullanın: 1,5 ya da 1500". Kaydet → çekmece açık, kutu 0, mesaj yerinde. İkinci Kaydet → "Sıfırdan büyük bir değer girin.". Ardından "1,5" → "Emanet iadesi kaydedildi.". DB'de bu kiracıda tümünü iade eden hiçbir satır YOK; yalnız geçerli adımlardan gelen `EMANET_CIKIS -1.500000 ALTIN` satırları var (aşağıda). Giriş çekmecesinde "10" ve ardından "abc" → POST yok, yeni `EMANET_GIRIS` yok |
| **[ORTA]** Uzun ticari unvan seçicide kesiliyordu (K10) ve mobil listede 3 satıra bölünüyordu (K6) | `emanet.css` `.secici-alan { max-width: 520px }` ve app.css `.secici-metin { white-space: nowrap; text-overflow: ellipsis }`. 375 px listede ad sütunu 293 px, 18 px yazı: 3 satır (ölçüldü: 18 → 3, 16 → 2, 15 → 2, 14 → 2) | Seçici üst sınırı 760 px. `#e-cari` tetiğinde seçili ad kesilmez (`white-space: normal; text-overflow: clip`), tetik boyu büyür (`height: auto`). ≤ 600 px'te yalnız `#e-cari-liste-kap` içindeki seçenek adı 16 px. Üç nokta yok, ad tam okunur | Gerçekçi ad "Mehmet Karaosmanoğlu Kuyumculuk ve Mücevherat Ticaret Limited Şirketi" ölçüm matrisine eklendi. 1440/1280: tetik 760 × 44, metin sw 547 = cw (K10 = 0). 960: 606 px, K10 = 0. 375: iki satır, tetik 60 px, K10 = 0. K6: 48 ölçümde dar = 0 |
| **[DÜŞÜK]** Eski kayıtlarda maden ayrımı yoktu | Tabloda maden sütunu yoktu | Tür hücresinde rozetin yanında maden adı: altın dışı satırda her zaman, geçmişte birden çok maden varsa altın satırında da (`SatirMadenAdi`; maden bilinmiyorsa uydurulmaz). Has hücresinde yalnız sayı kalır (V3/E16 sözleşmesi) | bUnit **E26** (karma: ["Gümüş","Altın"]; has hücrelerinde harf yok) · **E26b** (yalnız altın: etiket yok). Canlı: karma müşteri geçmişi `['Altın' ×4, 'Gümüş']`. Ekran görüntüsü `EMANET-1-t31-karma-acik-BASIT-aydinlik-1440.png` |
| [ORTA] Has binlik ayırıcısı | `Bicim.cs:81` `F4` (EMANET-1 alanı dışında) | Değişmedi | Açık kalan, T-K1-BICIM |
| [DÜŞÜK] Doğrulayıcı kiracısı | Doğrulayıcı silmiş | Bu turun kiracısı silindi (aşağıda) | — |

**Sapma ve gerekçe (dosya yeri):** Yardımcı bileşen önce `Pages/EmanetHamKap.razor` olarak yazıldı. Tam süit ve denetim bunun Pages kökünü 47 → 48'e çıkardığını gösterdi: `P37cPanelVeKapsamTestleri` ve denetim'in "Pages/*.razor dosya sayısı" ile "P-37c SAYFA_BASI_KAPSAM" sayaçları. Pages kökünde yalnız `@page` taşıyan ekranlar durur. Bileşen `Pages/EmanetParcalari/HamMetinKabi.razor`'a yeniden yazıldı (EMANET-1 alanı `Pages/Emanet*`). Eski dosya `printf '' >` ile **0 bayta** indirildi; kullanılmıyor. `rm`/`mv` yasak olduğu için dosyayı kaldıramadım. Bir ara P37c testini `@page` süzgecine çeviren bir düzenleme denendi. Başkasının sözleşmesini gevşetmek olduğu için **geri alındı**; P37c dosyası özgün t30 kopyasıyla `cmp` AYNI. **Orkestratör/kurucu için tek komut:** `rm dotnet/Kuyumcu.Web/Pages/EmanetHamKap.razor`.

### 00000.2 Derleme, testler, denetim

- `dotnet build Kuyumcu.sln` (t31): **0 hata, 1 uyarı** (`EtiketEkranTestleri.cs(145,47)` xUnit1031; başka şeridin dosyası). Mutasyonlardan sonraki temiz build: 0 uyarı, 0 hata.
- Filtreli (`Emanet|EMANET1|UX4cEkranTestleri|KilitGerekceTestleri|S35KurVitrinEmanet`): **132/132 geçti** (önceki tur 123; +4 doğrulayıcı V1–V4, +5 bu tur E23–E26b). P37c ile birlikte 143 test: 142 geçti; P37c, 0 baytlık dosya yüzünden kırmızı.
- **Dosya silinmiş hâlin kanıtı** (`t32` anlık kopyası; `rsync --exclude Pages/EmanetHamKap.razor`, silme yapılmadı): `denetim.sh` → **DENETİM TEMİZ**; filtreli P37c + Emanet **143/143**.
- **Tam süit** (t31, `KUYUMCU_TEST_DB=kuyumcu_test_t9`, trx `/private/tmp/kuyumcu-serit/trx-pEMANET-1-t31/tam3.trx`): `total=6531 passed=6527 failed=4`
  - `P37cPanelVeKapsamTestleri.Her_ekran_ortak_arac_seridini_kullanir_muafiyet_YAZILIDIR`: **bu turun ürünü** (0 baytlık dosya; yukarıda).
  - `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` (Expected 15, Actual 16; `OrtakGorunumSozlugu`): **ORTAK-1**. tam, tam2 ve tam3'te sürekli kırmızı; Emanet'e ait değil.
  - `P20bEkranTestleri.Defterde_ham_tur_kodu_BASILMAZ_sozlukten_cozulur`: **kararsız**. Aynı ikiliyle yalnız koşumda 2 geçti, 1 kaldı. tam ve tam2'de yeşildi. Sahibi Islemler/defter (SATIS-3).
  - `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ`: **kararsız**. Yalnız koşumda 3/3 geçti, tam ve tam2'de yeşildi. Sahibi SATIS-3.
- **denetim.sh** (gerçek ağaç): 289 ✓, 2 ✗ → `Pages/*.razor dosya sayısı 48 (tavan 47)` ve `P-37c: SAYFA_BASI_KAPSAM toplam=48 … acik=1`. İkisi de yalnız 0 baytlık dosyadan. EMANET-1 sayaçları ✓: ModalPencere 48 ≤ 55 · P-12 STORNO_GORUNMEYEN_EKRAN 0 · P-39 TABLO_SARMALAYICISIZ 0 · P-17 ORTAK_BELGE_TURU 4.

### 00000.3 Mutasyonlar (t31; `perl -0777` tek eşleşme → build → filtreli 100 test → yedek `/private/tmp/kuyumcu-serit/mut-yedek/EMANET-1/<kod>.orj` geri kopyalandı → `cmp`; betik `/private/tmp/kuyumcu-serit/EMANET-1/mut31.sh`)

| Kod | Mutasyon | Kırmızıya dönen testler | Geri alma |
|---|---|---|---|
| M1 | `<Katlanir Id="e-gecmis" BaslangictaAcik="true"` (açık varsayılan) | E2, E10, E16, E18, E26, Elimizdeki_emanet_ve_hareketler_gosterilir, S21, S35_12…S35_18, V3 (15) | cmp AYNI |
| M30 | İade `HamDenetle(cikisForm, "c-has", …)` satırı silindi | E23, V1 | cmp AYNI |
| M31 | `sifirla();` silindi (fail-closed sıfırlama yok) | E23 | cmp AYNI |
| M32 | `SatirMadenAdi` hep null | E26 | cmp AYNI |
| M33 | Giriş `HamDenetle(girisForm, "e-gram", …)` silindi | E24 | cmp AYNI |
| M34 | İade yardımcı gram `HamDenetle(cikisForm, "c-gram", …)` silindi | E25 | cmp AYNI |
| M35 | `HamMetinKabi.Geldi` metni deftere yazmaz | E23, E24, E25, V1 | cmp AYNI |

7/7 kırmızı; test açığı yok. M1–M34 yeniden adlandırmadan önceki t31 `Emanet.razor` üzerinde koşuldu. Sonraki fark yalnız etiket adı (`EmanetHamKap` → `HamMetinKabi`) ve bir `@using` satırı. M35 son dosyada koşuldu. Gerçek ağaca mutasyon uygulanmadı.

### 00000.4 Gerçek tarayıcı (başsız Chrome + ham CDP; tarayıcı bölmesi kullanılmadı; oturum inceleme-vekili ile parolasız; son derleme)

- **Durum matrisi** (`/private/tmp/kuyumcu-serit/EMANET-1/olc31.mjs`, sikisma.js + ekstra.js; seçili müşteride `__olcTam` ile K6 da ölçüldü): 4 durum × geçmiş kapalı/açık × {BASİT, PRO} (sıralı koşum, `modGercek` = istenen) × {aydınlık, karanlık} × {1440, 1280, 960, 375} = **112 ölçüm, hata 0**. **K toplamı 0** (K6 48 ölçümde dar 0, K10 0) · "0,0000" 0 · "Eksik olan" 0 · görünür/`title` storno 0 · büyük harf kelime 0 · kesik düğme 0 · yatay kaydırma 0. Durumlar: müşteri seçilmedi, emaneti olmayan, karma (Gümüş 1387,5 + Altın), uzun unvan (Altın 9,16).

| Uzun unvan · geçmiş kapalı | K | `#e-cari` tetik (g × y) | metin sw/cw | Özet |
|---|---|---|---|---|
| 1440 · BASİT/PRO · açık/koyu | 0 | 760 × 44 | 547/547 | 9,1600 has |
| 1280 · BASİT/PRO · açık/koyu | 0 | 760 × 44 | 547/547 | 9,1600 has |
| 960 · BASİT/PRO · açık/koyu | 0 | 606 × 44 | 547/547 | 9,1600 has |
| 375 · BASİT/PRO · açık/koyu | 0 (K6 dar 0) | 321 × 60 (iki satır) | 279/279 | 9,1600 has |

| Karma müşteri · geçmiş açık | K | Tablo sw/cw | Maden etiketleri |
|---|---|---|---|
| 1440 | 0 | 1090/1090 | Altın ×4, Gümüş |
| 1280 | 0 | 930/930 | aynı |
| 960 | 0 | 610/610 | aynı |
| 375 | 0 | 692/317 (kap içi kaydırma; sayfa yatay 0) | aynı |

1440 açılış içerik alt kenarı (geçmiş kapalı) 408 / 520 / 516 / 491 px, 900'ün altında: **≤ 1 ekran**.
- **tur.mjs** /emanet × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 1280, 960, 375}: **16/16 sayfa, K1..K13 toplamı 0**, yatay 0, `modGercek` = istenen. analiz.py: tek bulgu 375'te kabuğun global arama paleti SERBEST-YÜZEN (KATMAN; önceki turlarda da vardı).
- **tutarlilik.mjs** 1440: **"mod farki 0"**, düğme sınıf grubu 4.
- **Ekran görüntüleri** (`ham/TUVAL-KARSILASTIRMA/`, Read ile bakıldı): `EMANET-1-t31-uzun-kapali-BASIT-aydinlik-1280.png` (tam unvan tek satır, üç nokta yok) · `EMANET-1-t31-uzun-acik-BASIT-aydinlik-375.png` (unvan iki satır, iki eylem alt alta) · `EMANET-1-t31-uzun-kapali-PRO-aydinlik-1440.png` · `EMANET-1-t31-karma-acik-BASIT-aydinlik-1440.png` (tür hücresinde "Altın"/"Gümüş") · `EMANET-1-t31-karma-acik-PRO-karanlik-1440.png` · `EMANET-1-t31-emanetsiz-kapali-BASIT-aydinlik-1440.png` · `EMANET-1-t31-iade-1500-kaydet-engellendi-1440.png` (kutu 0, "Ondalık için virgül kullanın…", "Form eksik ya da hatalı" bandı, çekmece açık) · `EMANET-1-t31-giris-abc-kaydet-engellendi-1440.png`. Emanet'in kendi tuvali yok; bölge tablosu § 7.5 geçerli.

### 00000.5 Canlı doğrulama (API 5188 t31 kopya ağacından, kiracı `PAKETEMANET-1-TEST T31`; sistem anahtarı bu tur için üretilen rastgele değer, ortam değişkeninden, basılmadı)

| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /sistem/davetler · /auth/kayit · /auth/giris | 201 · 201 · 200 | 201 · 201 · 200 |
| POST /cari ×3 (Emanetsiz, Ayşe Yılmaz, uzun unvan) | 201 | 201 ×3 |
| GET /kur/guncel | 200 + id | 200, id var |
| POST /emanet/giris (G925 1500 g, K22 12,5 g, K22 10 g) | 200 | 200 ×3 |
| GET /emanet/{Ayşe} · {uzun} · {Emanetsiz} | GUMUS 1387,5 + ALTIN 11,45 · ALTIN 9,16 · boş | 200 ×3, aynı değerler |
| Ekran: iade "1.500" → Kaydet (×2) | POST yok | POST yok (DB'de yeni satır yok) |
| Ekran: iade "1,5" → Kaydet | 200, bant | "Emanet iadesi kaydedildi.", özet −1,5 |
| Ekran: giriş "10" sonra "abc" → Kaydet | POST yok | POST yok (yeni `EMANET_GIRIS` yok) |

```
SELECT h.tur,h.has_grami,h.maden,h.tarih FROM hareketler h JOIN tenants t … WHERE t.ad='PAKETEMANET-1-TEST T31'
EMANET_GIRIS  1387.500000 GUMUS 11:45:29
EMANET_GIRIS    11.450000 ALTIN 11:45:29
EMANET_GIRIS     9.160000 ALTIN 11:45:29
EMANET_CIKIS    -1.500000 ALTIN 11:47:22   (kritik31 koşum 1: "11,451.500" + Kaydet×2 → yok; "1,5" → bu satır)
EMANET_CIKIS    -1.500000 ALTIN 11:48:15   (koşum 2: "01.500" → yok; "1,5" → bu satır)
EMANET_CIKIS    -1.500000 ALTIN 11:49:04   (koşum 3: "1.500" varsayılan 8,450 → yok; "1,5" → bu satır)
EMANET_CIKIS    -1.500000 ALTIN 12:06:54   (koşum 4, son derleme: "1.500" varsayılan 6,950 → yok; "1,5" → bu satır)
maden toplamı: GUMUS 1387.5 (1) · ALTIN 14.61 (6)
```
Tümünü iade eden (−11,45 / −9,95 / −8,45 / −6,95) satır **yok**. Dört koşumun her birinde yalnız geçerli "1,5" adımı yazıldı. Saatler UTC.

**Temizlik:** API (PID) ve vekil (PID) durduruldu; `lsof` 5188/5189 boş; CDP Chrome süreçleri kapandı; 5199/5200'e dokunulmadı. Kiracı `betikler/kiraci-temizle.sh <id> --uygula` ile silindi. Kiracı kimliği jetonun yükünden okundu, basılmadı. `tenant_id` kolonu olan 70 tabloda bu kiracıya ait kalan satır **0**, `tenants` içinde **0**. `tenants`: başlangıç **18** → son **19**; artış eşzamanlı şeritlerin kiracılarından (ör. `PAKETSATIS-3-DOGRULAMA-TEST`, `PAKETZO-ISLEM-A-TEST Hakem`), bu turun kiracısı yok. QA-TEST kiracılarına dokunulmadı. Geçici sır dosyaları (`anahtar`, `fikstur.json`, `jeton`, `durum31.json`) `printf '' >` ile boşaltıldı. Not: başka kiracıların yetim satırları (`denetim_kayitlari` 146, `is_calismalari` 15, `kur_snapshots` 9) önceden bilinen "arka plan işi silinmiş kiracıya yazıyor" açığıdır; bu turun kiracısına ait değildir.

### 00000.6 DoD sözleşmesi, dokunulan sayfalar, değişen dosyalar

- Yeni varlık yok; uç, gövde ve migration değişikliği yok (**migration gerekmedi**). § 000.6 / § 4 aynen geçerli.
- Dokunulan sayfalar (bu tur): `Pages/Emanet.razor` (EMANET-1'in kendi sayfası; gerekçe doğrulayıcı KRİTİK/ORTA/DÜŞÜK bulguları), yeni `Pages/EmanetParcalari/HamMetinKabi.razor`.
- Değişen dosyalar (bu tur, `git status --porcelain | grep -v -E "/(bin|obj)/"` içinde): `?? dotnet/Kuyumcu.Web/Pages/Emanet.razor` · `?? dotnet/Kuyumcu.Web/Pages/EmanetParcalari/HamMetinKabi.razor` (yeni) · `?? dotnet/Kuyumcu.Web/Pages/EmanetHamKap.razor` (**0 bayt, silinmeli**) · `?? dotnet/Kuyumcu.Web/wwwroot/css/emanet.css` · `dotnet/Kuyumcu.Tests/EmanetSadeTestleri.cs` (E23–E26b, sahte API `karmaMaden`). `P37cPanelVeKapsamTestleri.cs` denendi ve geri alındı (özgünle cmp AYNI). Önceki turlardan: `M EmanetModulu.cs`, `?? Sozlukler.EmanetSade.cs`, `?? Sozlukler.KurVitrinEmanet.cs`, testler.

### 00000.7 Açık kalanlar (sahibiyle)

- **Orkestratör/kurucu:** `rm dotnet/Kuyumcu.Web/Pages/EmanetHamKap.razor` (0 bayt, bu turun artığı). Silinince denetim TEMİZ ve P37c yeşil olur (t32'de kanıtlandı).
- **ORTAK-1:** `OrtakGorunumSaflik.O8_basit_metinler_jargon_tasimaz_iki_dil_dolu` kırmızı (15 ≠ 16).
- **SATIS-3 (Islemler/defter):** `P20bEkranTestleri.Defterde_ham_tur_kodu_BASILMAZ_sozlukten_cozulur` ve `AlisOdemeAraciEkranTestleri.SATISTAN_ALISA_gecince_VERESIYE_secimi_TASINMAZ` kararsız.
- **Orkestratör / T-K1-BICIM:** has binlik gruplama (`Bicim.Has` `F4` → `N4` kararı; P-57). Hakem YÜKSEK ve doğrulayıcı ORTA bulgusu; EMANET-1 alanı dışında.
- **Ortak bileşen (sahipsiz, Bilesenler/Form):** `SayisalAlan` çözülemeyen metinde `Deger`'i son geçerli değerde tutuyor ve yeniden çizimde metni eziyor. Önceden dolu sayısal alanı olan her ekranda aynı risk var (EMANET-1 yalnız kendi sayfasında kapattı). Önerim: geçersiz girdide `GirdiGecersiz` bayrağının dışa açılması ya da `FormDurumu`'na yazılması.
- **Tasarım (küçük):** müşteri kartında seçici 760 px, özet ayracı 520 px (özet 27.09 kararıyla dar tutuldu). Hizalama tasarım hakemine.
- Önceki turdan aynen: KATMAN (375'te global arama paleti serbest yüzen; Secici'de sunucu araması yok) · ölçüm aracı `checkVisibility` önerisi · Emanet tuvali (`Emanet.dc.html`) yok · başka şeritlerin `.mutorj/.tmp` artıkları.

## 0000. Dördüncü devralma turu (28.09 13:48–14:20, t9 · API 5188 · vekil 5189 · CDP 9982–9990)

Orkestratör devir notu: 28.09 11:15 koşumu kod yazmadan döndü (sayılmaz); son doğrulama da çalışmadı. Görevdeki "SON DOĞRULAMA (KALDI)" bulguları (K8 storno jargonu, E5 test açığı, BASİT tabloda taşma, has binlik) ikinci turun bulgularıdır ve § 000 / § 00'da kapatılmıştı. Bu turda **kod yazılmadı, ağaçtaki iş korundu**; ağacın sağlamlığı denetlendi ve bütün kabul ölçümleri GÜNCEL ağaçta (akşam-1810 yayını + 28.09 sabah şeritleri dahil) yeniden koşuldu.

### 0000.1 Ağaç denetimi (yarım mutasyon var mı?)

- `Emanet.razor` mtime 28.09 10:58:58, `Dogrulayici/EMANET1SinirTestleri.cs` 10:57 — § 000 raporundan (07:11) SONRA; doğrulayıcının 10:52 koşumuna ait. Yedek yok (`/private/tmp/kuyumcu-serit/EMANET-1` silinmişti). Bu yüzden mutasyon noktaları tek tek okundu: `<Katlanir Id="e-gecmis"` üzerinde `BaslangictaAcik` YOK (kapalı varsayılan) · `AciklamaGoster` "Geri alma: …" sunumu yerinde · `IadeAltSinir = 0.001m` ve `CikisKaydet`'te `if (cHas > 0 && !IadeEdilebilir(cHas))` · `SifirMi` `AwayFromZero` · `catch … ozetBilinmiyor = true` · `S.AramaSinir`. Hiçbir yerde `if (false)` yok. Filtreli testler yeşil (aşağıda) → **ağaçta yarım mutasyon yok.** EMANET1SinirTestleri'ne doğrulayıcı 4 test eklemiş (S25 vb.); hepsi yeşil.
- Anlık kopya `/private/tmp/kuyumcu-serit/EMANET-1/t30/agac` (rsync; .git/bin/obj hariç) ile gerçek ağaç `Emanet.razor` → `cmp` AYNI.

### 0000.2 Derleme, testler, denetim

- `dotnet build Kuyumcu.sln` (kopya ağaç): ilk build **0 hata, 1 uyarı** (`EtiketEkranTestleri.cs(145,47)` xUnit1031 — başka şeridin dosyası); mutasyonlardan sonraki temiz build **0 uyarı, 0 hata**.
- Filtreli (`EmanetSadeTestleri|EMANET1SinirTestleri|EmanetEkranTestleri|S35KurVitrinEmanet|UX4cEkranTestleri|KilitGerekceTestleri|Emanet`): **123/123 geçti** (önceki tur 119; +4 doğrulayıcı testi).
- **Tam süit** (kopya ağaç, `KUYUMCU_TEST_DB=kuyumcu_test_t9`, trx `/private/tmp/kuyumcu-serit/trx-pEMANET-1-t30/tam.trx`): `Counters total=6498 executed=6498 passed=6498 failed=0`. **Bu paketin yeni kırmızısı: 0.**
- **denetim.sh** (gerçek ağaç): 290 ✓, 1 ✗ → `OLCEK-1: MOD_OLCU_BILDIRIMI (beklenen 0) 1` (`app.css: html[data-mod="profesyonel"] .sayfa-basi-eylem .btn { height: 32px }` — OLCEK-1 şeridinin sayacı ve app.css; EMANET-1 alanı dışı). EMANET-1 sayaçları ✓: ModalPencere 48 ≤ 55, P-12 STORNO_GORUNMEYEN_EKRAN 0, P-39 TABLO_SARMALAYICISIZ 0, P-17 ORTAK_BELGE_TURU 4.

### 0000.3 Mutasyonlar (kopya ağaç; `perl -0777` tek eşleşme → build → filtreli 91 test → yedek `/private/tmp/kuyumcu-serit/mut-yedek/EMANET-1/<kod>.orj` geri kopyalandı → `cmp`)

| Kod | Mutasyon | Kırmızıya dönen testler | Geri alma |
|---|---|---|---|
| M1 | `<Katlanir Id="e-gecmis" BaslangictaAcik="true"` (açık varsayılan) | E2, E10, E16, E18, Elimizdeki_emanet_ve_hareketler_gosterilir, S21, S35_12…S35_18 (13) | cmp AYNI |
| M21 | `AciklamaGoster` ham "STORNO: …" döndürür | E18_Geri_alinmis_kayitta_jargon_ve_buyuk_harf_yok_sebep_satira_ozgu | cmp AYNI |
| M24 | İstemci iade alt sınırı `if (false)` | E19_Iade_alt_sinirin_altinda_istek_atmaz_alan_alti_mesaj, S25_Iade_0_0009_reddedilir_0_001_noktali_JSON_ile_gider | cmp AYNI |
| M29 | Yükleme hatasında `ozetBilinmiyor` kaldırıldı | E21_Yukleme_hatasinda_Emaneti_yok_DENMEZ_kilit_sebebi_hata | cmp AYNI |

4/4 kırmızı; test açığı yok. Gerçek ağaca mutasyon uygulanmadı. (M2–M28'in geri kalanı kod değişmediği için § 000/§ 00/§ 0'daki koşumlarla geçerli.)

### 0000.4 Gerçek tarayıcı (başsız Chrome + ham CDP; tarayıcı bölmesi kullanılmadı; oturum inceleme-vekili ile parolasız)

- **tur.mjs** /emanet × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 960, 375}: 12/12 sayfa, `modGercek` = istenen mod, hata 0; **K1..K13 toplamı her kombinasyonda 0**, yatay kaydırma 0. analiz.py tek bulgu: 375'te kabuğun global arama paleti SERBEST-YÜZEN (KATMAN, önceki turlarda da vardı).
- **tutarlilik.mjs** 1440: **"mod farki 0"**, düğme sınıf grubu 4 (Emanet al / Emanet iade et h52 · 17 px, iki modda aynı).
- **Durum matrisi** (`/private/tmp/kuyumcu-serit/EMANET-1/durum.mjs`, sikisma.js + ekstra.js + ek ölçüm): 7 müşteri durumu (seçilmedi · emanetsiz · emanetli · yalnız gümüş · toz 0,0005 · 12 kayıtlı · **geri alınmış**) × geçmiş kapalı/açık × {BASİT, PRO} × {açık, koyu} × {1440, 1280, 960, 375} = **208 ölçüm, hata 0**: K toplamı 0 · "0,0000" 0 · "Eksik olan" 0 · görünür metinde `storno*` 0 · `title` özniteliklerinde `storno*` 0 · büyük harf kelime 0 (tablo başlıkları hariç) · kesik düğme 0 · yatay kaydırma 0.

| Geri alınmış müşteri, geçmiş açık | K | Jargon | Tablo sw/cw | Kesik düğme |
|---|---|---|---|---|
| 1440 · BASİT/PRO · açık/koyu | 0 | 0 | 1090/1090 | 0 |
| 1280 · BASİT/PRO · açık/koyu | 0 | 0 | 930/930 | 0 |
| 960 · BASİT/PRO · açık/koyu | 0 | 0 | 610/610 | 0 |
| 375 · BASİT/PRO · açık/koyu | 0 | 0 | 1082/317 (kap içi kaydırma; sayfa yatay 0) | 0 |

| Durum @1440 BASİT açık | Özet | İade | Kilit metni | İçerik alt kenarı / 900 (geçmiş kapalı) |
|---|---|---|---|---|
| Müşteri seçilmedi | — | kilitli | Önce müşteri seçin. | 408 |
| Emaneti olmayan | Emaneti yok | kilitli | Bu müşterinin emaneti yok. | 520 |
| Emanetli (K22 10 g) | 9,1600 has | açık | — | 491 |
| Yalnız gümüş (G925 100 g) | Gümüş 92,5000 has | açık | — | 491 |
| Toz artık (0,0005) | 0,0005 has | kilitli | Kalan miktar iade için çok küçük. | 520 |
| 12 kayıtlı | 71,4480 has | açık | — | 491 |
| Geri alınmış (K14 5,5 g geri alındı + K22 2 g) | 1,8320 has | açık | — | 491 |

1440 açılışı her durumda ≤ 1 ekran boyu.

**Ekran görüntüleri** (`ham/TUVAL-KARSILASTIRMA/`, Read ile bakıldı): `EMANET-1-t30-geri-acik-BASIT-aydinlik-1440.png` (üç satır: "Geri alma kaydı / Geri alma: Yanlis ayar girildi…", "Bu kayıt geri alındı" rozeti, kilitli "Geri al"; düğmeler tam, tarih tek satır; iki DegismezIpucu sade dil), `EMANET-1-t30-geri-acik-BASIT-aydinlik-375.png` (kap içi kaydırma, iki eylem alt alta), `EMANET-1-t30-geri-acik-PRO-karanlik-1440.png`, `EMANET-1-t30-emanetsiz-kapali-PRO-karanlik-1440.png` ("Emaneti yok", iade kilitli + "Bu müşterinin emaneti yok.", Eski kayıtlar 0 kapalı), `EMANET-1-t30-emanetli-kapali-BASIT-aydinlik-1440.png`, `EMANET-1-t30-tuval-yan-yana-1440.png` (Musteriler tuvali ile yan yana; Emanet'in kendi tuvali yok, bölge tablosu § 7.5 geçerli; bu turda bölge farkı değişmedi).

### 0000.5 Canlı doğrulama (API 5188 kopya ağaçtan, kiracı `PAKETEMANET-1-TEST T30`; sistem anahtarı bu tur için üretilen rastgele değer, ortam değişkeninden, basılmadı)

| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /sistem/davetler · /auth/kayit · /auth/giris | 201 · 201 · 200 | 201 · 201 · 200 |
| POST /cari ×6 | 201 | 201 ×6 |
| GET /kur/guncel | 200 + id | 200, id var |
| POST /emanet/giris ×17 | 200 | 200 ×17 |
| POST /emanet/{toz}/cikis 0,9155 | 200 | 200 |
| POST /emanet/{toz}/cikis 0,0005 | 400 | **400** `hasGrami: "Has gram 0,001 veya daha büyük olmalıdır"` |
| POST /islemler/{K14 5,5 g}/storno (48-A) | 200 | **200** `tur: EMANET_GIRIS`, `stornoHareketId` döndü |
| GET /emanet/{…} | emanetsiz boş; emanetli ALTIN 9,16; gümüşlü GUMUS 92,5; toz 0,0005; 12 kayıtlı 71,448/12; geri alınmış 1,832/3 (biri ters kayıt) | 200 ×6, aynı değerler |
| PATCH /emanet/hareketler/{id} `{"aciklama":"Bilezik - duzeltildi"}` | 200 | 200, açıklama değişti |
| PATCH aynı uç `{}` | koru | 400 "Düzeltilecek alan gönderilmedi…"; ardından GET: açıklama korunmuş |

```
SELECT c.ad,h.tur,h.has_grami,h.storno_kaynak_id IS NOT NULL,LEFT(h.aciklama,40) … WHERE t.ad='PAKETEMANET-1-TEST T30'
Emanetli Musteri      EMANET_GIRIS   9.160000 0 Bilezik - duzeltildi
Geri Alinmis Musteri  EMANET_GIRIS   3.217500 0 Yanlis ayarla girilen bilezik
Geri Alinmis Musteri  EMANET_GIRIS   1.832000 0 Yuzuk
Geri Alinmis Musteri  EMANET_GIRIS  -3.217500 1 STORNO: Yanlis ayar girildi, dogrusu yen
Gumuslu Musteri       EMANET_GIRIS  92.500000 0 Gumus kolye
Toz Musteri           EMANET_GIRIS   0.916000 0 Kucuk yuzuk
Toz Musteri           EMANET_CIKIS  -0.915500 0 kismi iade - toz artik
SUM: Toz 0.000500 (2) · Cok Kayitli 71.448000 (12) · Geri Alinmis 1.832000 (3)
```

**Temizlik:** API ve vekil PID ile durduruldu; `lsof` 5188 ve 5189 boş; 5199'a dokunulmadı. Kiracı `PAKETEMANET-1-TEST T30` `tenant_id` kolonu olan 70 tablodan ve `tenants`'tan silindi; silmeden sonra kalan satır **0**. `tenants`: başlangıç **20** → son **20** (arada eşzamanlı şeritlerin kiracıları değişti; `PAKETEMANET-1-TEST Dogrulayici` doğrulayıcınındır, QA-TEST kiracıları (4) — dokunulmadı). Geçici sır dosyaları (fikstür, anahtar, jeton) `printf '' >` ile boşaltıldı.

### 0000.6 DoD sözleşmesi, dokunulan sayfalar, değişen dosyalar

- Yeni varlık yok; uç/gövde değişikliği yok. § 000.6 / § 4 aynen geçerli: emanet hareketi açıklaması `PATCH /emanet/hareketler/{id}` (SatirIciDuzenle; canlıda 200 kanıtlandı), gram/ayar/maden/müşteri salt okunur + DegismezIpucu, düzeltme yalnız 48-A (`POST /islemler/{id}/storno`; canlıda 200).
- Dokunulan sayfalar (bu tur): **yok** (kod değişmedi).
- Değişen dosyalar (EMANET-1 alanı, `git status --porcelain | grep -v -E "/(bin|obj)/"`): ` M dotnet/Kuyumcu.Api/Modules/Emanet/EmanetModulu.cs` · `?? dotnet/Kuyumcu.Core/I18n/Sozlukler.EmanetSade.cs` · `?? dotnet/Kuyumcu.Core/I18n/Sozlukler.KurVitrinEmanet.cs` · `?? dotnet/Kuyumcu.Web/Pages/Emanet.razor` · `?? dotnet/Kuyumcu.Web/wwwroot/css/emanet.css` · testler `EmanetSadeTestleri.cs`, `UX4cEkranTestleri.cs`, `Dogrulayici/EMANET1SinirTestleri.cs` — hepsi önceki turların işi; bu turda yalnız bu rapor ve 6 yeni ekran görüntüsü (`ham/TUVAL-KARSILASTIRMA/EMANET-1-t30-*.png`).

### 0000.7 Açık kalanlar (sahibiyle)

- **Orkestratör / T-K1-BICIM:** has binlik gruplama (P-57, `Bicim.cs`) — hakem YÜKSEK bulgusu; EMANET-1 alanı dışında, § 00.8 değişmedi.
- **OLCEK-1:** denetim.sh `MOD_OLCU_BILDIRIMI = 1` (app.css profesyonel `.sayfa-basi-eylem .btn { height: 32px }`).
- **Orkestratör:** başka şeritlerin ağaçtaki artıkları (`Pages/Islemler.Sade.cs.tmp`, `denetim.sh.mutbak` vb.) — EMANET-1'e ait değil, dokunulmadı.
- **KATMAN:** 375'te global arama paleti SERBEST-YÜZEN; Secici'de sunucu araması yok.
- **Ölçüm aracı:** sikisma.js K9/K9b çekmece perdesi altındaki ve kapalı `<details>` içindeki öğeleri sayabiliyor; `checkVisibility` süzgeci önerisi (§ 000.8) duruyor.
- **Tasarım:** Emanet için tuval ekranı (`Emanet.dc.html`) yok.

## 000. Üçüncü devralma turu (28.09, t9 · API 5188 · vekil 5189 · CDP 9982–9995)

Doğrulayıcı ikinci turda KALDI verdi (1 KRİTİK + 1 ORTA + 2 DÜŞÜK + 1 DÜŞÜK başka şeritlere ait). 28.09 gece kesilen oturum düzeltmeleri ağaca yazmış, testleri eklemiş, mutasyonları koşmuştu; ancak rapor güncellenmemiş, tam süit kararsız kalmış (kopya ağaçta kök dosyalar eksikti) ve tarayıcı ölçümünde giriş başarısız olmuştu. Bu turda **ağaçtaki iş korundu, kod yeniden yazılmadı**; bütün kabul ölçümleri güncel ağaçta baştan koşuldu. Ağaçta yarım mutasyon yok: `mut-yedek/EMANET-1/Emanet.razor` 27.09 21:47 tarihli eski yedektir (ağaçtaki 28.09 01:42 sürümü daha yeni), t28 kopyası ile gerçek ağaç `Emanet.razor`, `Sozlukler.EmanetSade.cs`, `EmanetSadeTestleri.cs`, `UX4cEkranTestleri.cs`, `EMANET1SinirTestleri.cs`, `emanet.css` için `cmp` AYNI.

### 000.1 Bulgular ve kapanış

| Bulgu | Yapılan (sunum katmanı; sunucu metni/ucu değişmedi) | Kanıt |
|---|---|---|
| [KRİTİK] Geri alınmış kayıtta "STORNO: …", "Stornolandı", hub `HareketDegismez` jargonu (K8=2) | (1) `EmanetSadeSozlugu`'na sade karşılıklar (TR+EN): `HareketDegismez` "Bu kayıt geri alınamaz: ya daha önce geri alındı, ya kendisi bir geri alma kaydıdır, ya da birden çok satırlı bir fişin parçasıdır.", `HareketDegismezYolu`, `GeriAlindi` rozeti "Bu kayıt geri alındı", `GeriAlBaslik/GeriAlOnay/GeriAlGerekce/GeriAlYapildi`, `KurYokGeriAl`, satıra özgü kilit sebebi `NedenGeriAlindi` / `NedenGeriAlmaKaydi` / `NedenFis` (`GeriAlKilitSebebi`). Tablo satırı `title`, tablo altı DegismezIpucu, Ayrıntı çekmecesi ve ortak onay modalı bunları kullanır. (2) Ters kaydın açıklaması ekranda `AciklamaGoster`: yalnız `StornoKaynakId` dolu ve metin `"STORNO: "` ile başlıyorsa → "Geri alma: {gerekçe}"; sunucudaki metin aynen kalır | bUnit **E18** (sahte API'ye `stornoKaynakId` dolu satır h82 + stornolanmış h81 + fişli h83 + açık h84; sayfa, satır `title`'ları, Ayrıntı çekmecesi ve onay modalında `/storno\p{L}*/i` = 0 ve büyük harf kelime = 0; sözlükteki tüm TR metinlerde "storno" 0). Mutasyonlar M21, M22, M23, M26, M27 → E18 kırmızı. **Canlı:** geri alınmış kaydı olan müşteride BASİT/PRO × açık/koyu × 1440/1280/960/375, geçmiş açık: `stornoJargon` 0, K8 0, büyük harf 0 (16/16 ölçüm) |
| [ORTA] E5 geri alma durumunu kapsamıyordu (test açığı) | Yeni **E18** (yukarıda) — c-geri sahte müşterisi | M21/M22/M23/M26/M27 kırmızı (test açığı kapandı) |
| [DÜŞÜK] `AramaDaraltin` teknik dil ("sunucu sınırı (50 kayıt)") | `EmanetSadeSozlugu.AramaSinir` sade metin; `#e-cari-sinir` bunu yazar (UX4c testindeki beklenen de sözlükten) | bUnit **E20**; mutasyon M25 → E20 kırmızı |
| [DÜŞÜK] İadede 0 < has < 0,001 istemcide engellenmiyordu | `CikisKaydet`: `cHas > 0 && !IadeEdilebilir(cHas)` → `c-has` altında "İade edilecek miktar en az 0,0010 has olmalı." (`IadeAltSinir` = sunucu `[Range(0.001, …)]`), istek atılmaz | bUnit **E19** (0,0005 → POST yok, alan altı mesaj); mutasyon M24 → E19 kırmızı. Canlı: aynı değer sunucuda 400 (sunucu kuralı değişmedi) |
| [DÜŞÜK] Başka şeritlerin `.mutorj/.bak/.tmp` artıkları | EMANET-1'e ait değil; dokunulmadı | Orkestratöre (açık kalanlar) |
| (Bu turda bulunan) UX-S28: özet yüklenirken/yükleme hatasında "Emaneti yok" deniyordu | `yukleniyor` → özet ve kilit "Yükleniyor…"; ilk yükleme hata verirse `ozetBilinmiyor` → özet "—", kilit sebebi "Bilgi alınamadı…" (boş sayılmaz) | bUnit **E21**, **E22**; mutasyon M28 → E22, M29 → E21 kırmızı |

### 000.2 Derleme, testler, denetim

- `dotnet build Kuyumcu.sln` (gerçek ağaç): **0 hata, 1 uyarı** (`EtiketEkranTestleri.cs(145,47)` xUnit1031 — başka şeridin dosyası). Anlık kopya ağaçta temiz build (mutasyonlardan sonra): **0 uyarı 0 hata**.
- Filtreli (gerçek ağaç; `EmanetSadeTestleri|EMANET1SinirTestleri|EmanetEkranTestleri|S35KurVitrinEmanet|UX4cEkranTestleri|KilitGerekceTestleri|Emanet`): **119/119 geçti**. Mutasyon filtresi (ilk dört sınıf): taban **60/60 geçti**.
- **Yeni testler bu tur (5):** E18, E19, E20, E21, E22 (`EmanetSadeTestleri`). `UX4cEkranTestleri` sınır metni beklentisi sözlükten.
- **Tam süit** (anlık kopya ağaç = gerçek ağaç + kök dosyalar, bin/obj/.git hariç; `KUYUMCU_TEST_DB=kuyumcu_test_t9`; trx `/private/tmp/kuyumcu-serit/trx-pEMANET-1-t29/tam2.trx`): **6470 test · 6470 geçti · 0 başarısız.** (İlk koşum 119 kırmızı verdi: kopyaya yalnız `dotnet/` alınmıştı, kök belgeleri/`deploy`/`docs` okuyan testler dosya bulamadı; kök dosyalar eklenince 0. Bu bir kopya eksikliğiydi, kod kırmızısı değil.) **Bu paketin yeni kırmızısı: 0.** Devir notundaki başka şerit kırmızıları (DenetimIziEkranTestleri ×7, P39.K5, MigrationHijyeni) bu anlık görüntüde yeşildi.
- **denetim.sh** (gerçek ağaç): **DENETİM TEMİZ** — ModalPencere 48 ≤ 55, P-12 STORNO_GORUNMEYEN_EKRAN 0, P-39 TABLO_SARMALAYICISIZ 0.

### 000.3 Mutasyonlar (anlık kopya ağaç `t29/agac`; `perl -0777` tek eşleşme → build → filtreli test → yedeği geri kopyala → `cmp`)

| Kod | Mutasyon | Kırmızıya dönen testler | Geri alma |
|---|---|---|---|
| M1 | Eski kayıtlar katlanırı `BaslangictaAcik="true"` (açık varsayılan) | E2, E10, E16, E18, Elimizdeki_emanet_ve_hareketler_gosterilir, S21, S35_12…S35_18 (13) | cmp AYNI |
| M21 | "Geri alma: …" sunumu kaldırıldı (ham "STORNO: …") | E18 | cmp AYNI |
| M22 | Rozet yeniden hub `Kabuk.StornoluKayit` ("Stornolandı") | E18 | cmp AYNI |
| M23 | Satıra özgü "Daha önce geri alındı." sebebi kapatıldı | E18 | cmp AYNI |
| M24 | İstemci alt sınır kontrolü `if (false)` | E19 | cmp AYNI |
| M25 | Liste sınırı yeniden hub `AramaDaraltin` | E20 | cmp AYNI |
| M26 | DegismezIpucu yolu yeniden hub `HareketDegismezYolu` | E18 | cmp AYNI |
| M27 | Onay modalı başlığı yeniden hub `StornoBaslik` | E18 | cmp AYNI |
| M28 | Özette "Yükleniyor…" satırı silindi | E22 | cmp AYNI |
| M29 | Yükleme hatasında `ozetBilinmiyor` bayrağı kaldırıldı | E21 | cmp AYNI |

10/10 kırmızı; test açığı yok. Mutasyonlar gerçek ağaca uygulanmadı; sonda kopya `Emanet.razor` = gerçek `Emanet.razor` (cmp AYNI).

### 000.4 Gerçek tarayıcı (başsız Chrome 154 + ham CDP; tarayıcı bölmesi kullanılmadı; oturum inceleme-vekili ile parolasız)

**Durum matrisi** (`t29/durum.mjs`, sikisma.js + ekstra.js): 7 müşteri durumu (seçilmedi · emanetsiz · emanetli · yalnız gümüş · toz 0,0005 · 12 kayıtlı · **geri alınmış**) × geçmiş kapalı/açık × {BASİT, PRO} × {açık, koyu} × {1440, 1280, 960, 375} = **192 sayfa + 16 iade çekmecesi = 208 ölçüm, hata 0.**

- Sayfa (192): **K1, K3, K5, K7, K8, K9, K9b, K10, K10b, K11, K12, K13 = 0**; yatay kaydırma 0; "0,0000" 0; "Eksik olan" 0; storno jargonu 0; büyük harf kelime 0 (tablo başlıkları hariç). K2/K4 bilgi amaçlı.
- İade çekmecesi (16): çekmece İÇİ binen **0** (ayrı ölçüm `cekmece.mjs`, 1440/1280/960/375; çekmece yatay kaydırma 0). Aracın saydığı K9 3–6: hepsi perde altındaki SAYFA öğesi ⟂ çekmece öğesi (ör. `#sayfa-basi-emanet-baslik ⟂ label "Elimizdeki emanet"`); K9b 1–5: kapalı `<details>` ("+ Ayar ve gramdan hesapla", "+ Not ekle") içindeki GÖRÜNMEZ etiketler (`checkVisibility` = false, metin boş; Chrome kapalı details içeriğini boyutlu yerleşimde tutuyor). Görünür kusur değil — gerekçeli.

| Geri alınmış müşteri, geçmiş açık | K8 | Jargon | Tablo sw/cw | Kesik düğme | Kırık tarih | Has ekli hücre | Satır yük. |
|---|---|---|---|---|---|---|---|
| 1440 · BASİT/PRO · açık/koyu | 0 | 0 | 1090/1090 | 0 | 0 | 0 | 61 |
| 1280 · BASİT/PRO · açık/koyu | 0 | 0 | 930/930 | 0 | 0 | 0 | 61 |
| 960 · BASİT/PRO · açık/koyu | 0 | 0 | 610/610 | 0 | 0 | 0 | 122 (düğmeler alt alta) |
| 375 · BASİT/PRO · açık/koyu | 0 | 0 | 1087/317 (kap içi kaydırma; sayfa yatay 0) | 0 | 0 | 0 | 69 |

Ekranda görünen metinler (geri alınmış satırlar): "Geri alma kaydı" (tür rozeti), "Geri alma: Yanlis ayar girildi, dogrusu yeniden girilecek", "Bu kayıt geri alındı", "Ayrıntı", "Geri al".

| Durum @1440 BASİT açık | Özet | İade | Kilit metni | İçerik alt kenarı / 900 |
|---|---|---|---|---|
| Müşteri seçilmedi | — | kilitli | Önce müşteri seçin. | 406 |
| Emaneti olmayan | Emaneti yok | kilitli | Bu müşterinin emaneti yok. | 518 |
| Emanetli (K22 10 g) | 9,1600 has | açık | — | 489 |
| Yalnız gümüş (G925 100 g) | Gümüş 92,5000 has | açık | — | 489 |
| Toz artık (0,0005) | 0,0005 has | kilitli | Kalan miktar iade için çok küçük. | 518 |
| 12 kayıtlı | 71,4480 has | açık | — | 489 |
| Geri alınmış (K14 5,5 g geri alındı + K22 2 g) | 1,8320 has | açık | — | 489 |

1440 açılışı her durumda ≤ 1 ekran boyu.

**tur.mjs** /emanet × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 960, 375}: 12/12 sayfa, hata 0, K1..K13 = 0 (K2/K4/K6 bilgi). analiz.py tek bulgu: 375'te kabuğun global arama paleti SERBEST-YÜZEN (KATMAN bileşeni, önceki turlarda da vardı). "eksik kombinasyon" satırı aracın tam site eşiğidir, tek rota koşumunda anlamsız.

**tutarlilik.mjs** 1440 (CDP 9991): **"mod farki 0"**, düğme sınıf grubu 4 (Emanet al / Emanet iade et h52 · 17 px, iki modda aynı).

**Ekran görüntüleri** (`ham/TUVAL-KARSILASTIRMA/`, Read ile bakıldı): `EMANET-1-t29-geri-alinmis-BASIT-1440.png` (üç satır: "Geri alma kaydı / Geri alma: …", "Bu kayıt geri alındı" rozeti, kilitli "Geri al"; iki DegismezIpucu sade), `EMANET-1-t29-geri-alinmis-PRO-karanlik-1280.png`, `EMANET-1-t29-geri-alinmis-BASIT-375.png`, `EMANET-1-t29-bos-musteri-1440.png`, `EMANET-1-t29-toz-artik-1440.png`, `EMANET-1-t29-iade-cekmece-1440.png`, `EMANET-1-t29-tuval-yan-yana-1440.png` (Musteriler tuvali ile yan yana; Emanet'in kendi tuvali yok; bölge tablosu § 7.5 geçerli, bu turda değişen bölge yalnız "Eski kayıtlar" metinleri — yerleşim aynı).

### 000.5 Canlı doğrulama (API 5188, kiracı `PAKETEMANET-1-TEST T29`)

| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /sistem/davetler · /auth/kayit · /auth/giris | 201 · 201 · 200 | 201 · 201 · 200 |
| POST /cari ×6 | 201 | 201 ×6 |
| GET /kur/guncel | 200 + id | 200, id var |
| POST /emanet/giris ×17 (K22 10; G925 100; K22 1; K22 1…12; K14 5,5; K22 2) | 200 | 200 ×17 |
| POST /islemler/{K14 5,5 g}/storno (48-A) | 200 | **200** `tur: EMANET_GIRIS`, ters hareket id döndü |
| POST /emanet/{toz}/cikis 0,9155 | 200 | 200 |
| POST /emanet/{toz}/cikis 0,0005 | 400 | **400** `hasGrami: "Has gram 0,001 veya daha büyük olmalıdır"` |
| GET /emanet/{geri alınmış} | 1,832; 3 hareket, biri ters kayıt | 200: `1.832 [('ALTIN',1.832)]`, `(-3.2175,'STORNO: Yanlis ayar…',True)` |
| GET /emanet/{gümüşlü} · {toz} · {12 kayıtlı} | [GUMUS 92,5] · 0,0005 · 71,448 | 200 · 200 · 200 (aynı değerler) |

```
SELECT c.ad,h.tur,h.has_grami,h.storno_kaynak_id IS NOT NULL,LEFT(h.aciklama,40) FROM hareketler h JOIN cariler c … WHERE tenant='PAKETEMANET-1-TEST T29'
Geri Alinmis Musteri  EMANET_GIRIS   3.217500 0 Yanlis ayarla girilen bilezik
Geri Alinmis Musteri  EMANET_GIRIS   1.832000 0 Yuzuk
Geri Alinmis Musteri  EMANET_GIRIS  -3.217500 1 STORNO: Yanlis ayar girildi, dogrusu yen
Gumuslu Musteri       EMANET_GIRIS  92.500000 0 Gumus kolye
Toz Musteri           EMANET_GIRIS   0.916000 0 Kucuk yuzuk
Toz Musteri           EMANET_CIKIS  -0.915500 0 kismi iade - toz artik
```
Sunucu ters kaydı "STORNO: …" olarak saklamaya devam ediyor (değişmedi); ekranda "Geri alma: …" görünüyor (§ 000.4).

**Temizlik:** API (PID) ve vekil (PID) `kill` ile durduruldu; `lsof` 5188/5189 boş. Kiracılarım `PAKETEMANET-1-TEST T29` ve önceki kesilen oturumdan kalan `PAKETEMANET-1-TEST T28` (`tenant_id` kolonu olan 70 tablodan ve `tenants`'tan) silindi; silmeden sonra sayım: kalan satır **0**, `PAKETEMANET%` kiracısı **0**. `tenants`: başlangıç 17 (T28 dahil) → son **18**; fark, çalışma sırasında eşzamanlı şeritlerin açtığı `PAKET-OLCEK-1F-TEST` (03:43 UTC) ve `PAKETZO-ISLEM-A-TEST` (03:48 UTC). QA-TEST kiracılarına (4) dokunulmadı.

### 000.6 DoD sözleşmesi

Yeni varlık yok; uçlar/gövdeler değişmedi (`EmanetModulu.cs`'e bu turda dokunulmadı). § 4 aynen geçerli: emanet hareketi açıklaması `PATCH /emanet/hareketler/{id}` (SatirIciDuzenle), gram/ayar/maden/müşteri salt okunur + DegismezIpucu, düzeltme yalnız 48-A (`POST /islemler/{id}/storno`). Yeni düğme yok; kilitli "Geri al" düğmesi satıra özgü sebebini `title`'da ve Ayrıntı çekmecesinde yazar.

**Dokunulan sayfalar (bu tur):** `Pages/Emanet.razor` — aynı paketin doğrulayıcı KALDI düzeltmesi (ikinci dalga dokunuşu değil).

### 000.7 Değişen dosyalar (bu tur; `git status --porcelain | grep -v -E "/(bin|obj)/"` içinde EMANET-1 alanı)

| Dosya | Değişiklik |
|---|---|
| `dotnet/Kuyumcu.Web/Pages/Emanet.razor` | `AciklamaGoster` ("STORNO: " → "Geri alma: "), `GeriAlKilitSebebi`, sade rozet/ipucu/modal metinleri, `AramaSinir`, istemci iade alt sınırı, `yukleniyor`/`ozetBilinmiyor` (UX-S28) |
| `dotnet/Kuyumcu.Core/I18n/Sozlukler.EmanetSade.cs` | `HareketDegismez`, `HareketDegismezYolu`, `GeriAlindi`, `GeriAlmaAciklama`, `GeriAlBaslik/Onay/Gerekce/Yapildi`, `KurYokGeriAl`, `NedenGeriAlindi/GeriAlmaKaydi/Fis`, `AramaSinir`, `IadeEnAz`, `Yukleniyor`, `BilgiAlinamadi` (TR+EN) |
| `dotnet/Kuyumcu.Tests/EmanetSadeTestleri.cs` | E18–E22 + sahte API geri alınmış müşteri |
| `dotnet/Kuyumcu.Tests/UX4cEkranTestleri.cs` | sınır metni beklentisi sözlükten |
| `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/EMANET-1-t29-*.png` | 7 görüntü |

Değişmeyen: `EmanetModulu.cs`, `emanet.css` (00:25 sürümü), id'ler.

### 000.8 Açık kalanlar (sahibiyle)

- **Orkestratör / T-K1-BICIM:** has binlik gruplama kararı (§ 00.8, değişmedi).
- **Orkestratör:** başka şeritlerin ağaçtaki `.mutorj/.bak/.tmp/.mutbak` artıkları (HareketFabrikasi, Fiyatlandirma, Islemler, Urunler, Sayim, AramaHttp/MadenDefteri/P21CiroRucu testleri, denetim.sh.mutbak) — EMANET-1'e ait değil, dokunulmadı. Arka plan işinin silinen kiracıya yazması (§ 00.8).
- **KATMAN:** 375'te global arama paleti SERBEST-YÜZEN; Secici'nin sunucu araması yok (50+ müşteride `#e-cari-ara` gerekli).
- **Ölçüm aracı (orkestratör):** sikisma.js K9/K9b, açık çekmecenin perdesi altındaki sayfa öğelerini ve kapalı `<details>` içindeki görünmez etiketleri sayıyor; `checkVisibility` ve üst katman (inert/perde) süzgeci önerilir.
- Tasarım: Emanet için tuval ekranı (`Emanet.dc.html`) yok.
- Mevzuat: yeni hüküm yok; 0,001 alt sınırı sunucu DTO'sunun mevcut kaydı hassasiyeti kuralı.

## 00. İkinci devralma turu (27→28.09, t9 · API 5188 · vekil 5189 · CDP 9981–9990)

Doğrulayıcı KALDI verdi (2 KRİTİK tasarım hakemi + 1 ORTA + 4 DÜŞÜK). Ağaçtaki iş korundu; yalnız bulgular kapatıldı ve kabul ölçümleri güncel ağaçta yeniden koşuldu. Ağaçta yarım mutasyon yoktu (`/private/tmp/kuyumcu-serit/EMANET-1` duruyordu; Emanet testleri başlangıçta yeşildi).

**Yöntem notu:** paylaşılan ağaçta başka şeritler eşzamanlı build/test koşuyordu (ilk mutasyon denemesinde test DLL'i başka şeridin build'i sırasında yoktu → "bağımsız değişken geçersiz", geçersiz sayıldı). Bu yüzden mutasyonlar, tam süit ve canlı API, ağacın bin/obj/.git hariç anlık kopyasında (`/private/tmp/kuyumcu-serit/EMANET-1/t27/agac`) koşuldu. Kopya ile gerçek ağaç arasında `Emanet.razor` için `cmp` AYNI; gerçek ağaç son build 0 hata ve filtreli 147/147 ayrıca doğrulandı. Böylece mutasyonlar başka şeritlerin testlerine hiç sızmadı.

### 00.1 Bulgular ve kapanış

| Bulgu | Yapılan | Kanıt |
|---|---|---|
| [KRİTİK, hakem] Eski kayıtlar tablosu BASİT 1280/1440'ta taşıyor; "Geri al (düzeltme fişi)" kesik; tarih iki satır | Satır düğmesi kısa **"Geri al"** (`EmanetSadeSozlugu.SatirGeriAl`; tam adı ve açıklaması Ayrıntı çekmecesinde `M.GeriAl` olarak duruyor). İki düğme `.emanet-satir-eylem` içinde yan yana, `nowrap`. Tarih hücresi `td.emanet-tarih` `nowrap`. ≤1100 px'te düğmeler alt alta (960'ta kap kenarında kesik görünüyordu). `eh-storno-{id}` ve `eh-detay-{id}` kimlikleri değişmedi | Tarayıcı DOM ölçümü (aşağıda § 00.3): BASİT ve PRO, açık ve koyu, 1440'ta `.tablo-sar` 1090 = 1090, 1280'de 930 = 930, 960'ta 610 = 610; düğme sağ kenarı kabın içinde; kesik düğme 0; kırılan tarih 0; satır yüksekliği 69 px (önce ~100). bUnit E16; mutasyon M20 |
| [KRİTİK, hakem] Has binlik ayırıcısız ("1127787,6456 has"); tabloda "has" eki tekrar ediyor | **Tabloda hücre yalnız sayı** (`HasSayi` = `Bicim.Has` kuralı, birim eki yok; birim sütun başlığında). **Binlik gruplama YAPILMADI — orkestratör kararı gerekiyor:** `Bicim.Has`'ın gruplamasızlığı P-57'nin bilinçli kararı ("1.234,5670 has" gibi nokta hem binlik hem ondalık sanılmasın; `Bicim.cs:74-81`, `HasGirdi` aynı kural). `Bicim.cs` T-K1/BICIM alanı. Emanet'e yerel gruplama eklemek, has miktarını tüm diğer ekranlardan farklı yazdırırdı (UX-S31 tek kaynak ihlali). Doğrulayıcı da düzeltmeyi "T-K1/Bicim sahibiyle orkestratör kararı" diye yönlendirdi | E16 (hücrede "has" yok); mutasyon M19. Ölçümde tüm tablolarda `hasEkli` = 0. Gruplama açık kalan olarak aşağıda |
| [ORTA] Sunucunun reddettiği toz artıkta iade açık | Kilit eşiği sunucunun alt sınırına eşitlendi: `IadeAltSinir = 0.001m` (`EmanetModulu.cs:52` `[Range(0.001, …)]`). Altındaki pozitif artıkta tek kısa sebep **"Kalan miktar iade için çok küçük."**. İade çekmecesinin maden seçicisi de yalnız iade edilebilir (≥ 0,001) madenleri listeler; varsayılan maden seçimi aynı kuralı kullanır. S3 bu kurala göre güncellendi | E13, E14 (tam 0,001 → açık), S3; mutasyon M15, M17. **Canlı:** 0,916 − 0,9155 → SQL neti 0,000500; ekranda "0,0005 has", iade kilitli, sebep yazılı (4 mod×tema × 4 genişlik); aynı müşteriye `POST /emanet/{c}/cikis` 0,0005 → **400** "Has gram 0,001 veya daha büyük olmalıdır" (kilit sunucuyla aynı çizgide) |
| [DÜŞÜK] İade çekmecesinde isteğe bağlı Ayar/Gram açıkta | Ayar + gram yardımcısı **"+ Ayar ve gramdan hesapla"** katlanırında (`<details id="e-cikis-hesap">`, gram > 0 ise açık gelir). Zorunlu "İade edilecek has gram" dışarıda. Kimlikler (`c-ayar`, `c-gram`, `c-hesaplanan`) değişmedi | E17; mutasyon M18. Tarayıcıda: `checkVisibility` ile `#c-ayar`/`#c-gram` görünmez, `#c-has` görünür; çekmece metni "Elimizdeki emanet · Altın: 9,1600 has · … · + Ayar ve gramdan hesapla · İade edilecek has gram * · + Not ekle · Kaydet" |
| [DÜŞÜK] İki yuvarlama kuralı (SifirMi ToEven / Bicim AwayFromZero) | `SifirMi` artık `MidpointRounding.AwayFromZero` (Bicim "F4" ile aynı). 0,00005 → "0,0001 has" ve iade kilitli + "Kalan miktar…" (çelişki yok). S12 bu kurala göre güncellendi | E15, S12; mutasyon M16 |
| [DÜŞÜK] Özet etiketiyle değer arasında ~800 px | `.emanet-ozet { max-width: 520px }` (seçici genişliği) | Ölçüm: etiket-değer arası 192–283 px (1280/1440) |
| [DÜŞÜK] Silinen kiracıya arka plan işi yazıyor; kiraci-temizle.sh yetim satırları temizlemiyor | Paket dışı (arka plan işi + orkestratör betiği). Bu turda kiracımı kendi betiğimle 70 tablodan sildim ve silmeden sonra saydım: kalan 0 | Orkestratöre not (açık kalanlar) |

### 00.2 Testler

- `dotnet build Kuyumcu.sln`: **0 hata**, 1 uyarı (`EtiketEkranTestleri.cs(145,47)` xUnit1031, başka şeridin dosyası).
- **Yeni testler (5)** `EmanetSadeTestleri`: E13 toz artıkta kilit ve sebep · E14 tam 0,001'de açık · E15 yarım sınır 0,00005 · E16 tablo hücresi yalnız sayı + "Geri al" + tarih sınıfı · E17 hesap yardımcısı katlanırda kapalı, gram girilince açık. Sahte API'ye gerçekçi c5/c6/c7 yanıtları eklendi.
- **Güncellenen doğrulayıcı testleri (2)** `EMANET1SinirTestleri`: S3 (0,0001 → yazılır ama iade kilitli, sebep "Kalan miktar…"; doğrulayıcının istediği) ve S12 (0,00005 → "0,0001 has" + kilitli + sebep; yeni tek yuvarlama kuralının sonucu). Beklenenler sabit literal kaldı.
- Filtreli koşum (Emanet'e dokunan tüm sınıflar): **147/147 geçti** (kopya ağaçta ve gerçek ağaçta ayrı ayrı).
- **Tam süit** (kopya ağaç, kök dosyalar dahil; trx: `/private/tmp/kuyumcu-serit/trx-pEMANET-1-t27`): **6399 test · 6391 geçti · 8 başarısız.** Hiçbiri Emanet'e ait değil. **Bu paketin yeni kırmızısı: 0.** Sahipleri: KATMAN `KatmanTekKalipTestleri.K1_21…` · OLCEK-1 `OlcekTekTestleri.OL12…` · ORTAK-1 `OrtakGorunumEkranTestleri.B6…` · SATIS-3 `S24EskiAltinEkranTestleri` ×2 ve `SATIS3EkranTestleri` ×2 · ZO-ISLEM-A `ZOISLEMAEkranTestleri.ZO_pos_defter_rozeti…`.
- **denetim.sh** (gerçek ağaç, 28.09 00:1x): **DENETİM TEMİZ** — ModalPencere 48 ≤ 55, P-12 STORNO_GORUNMEYEN_EKRAN 0, P-39 TABLO_SARMALAYICISIZ 0.

### 00.3 Mutasyonlar (kopya ağaç; `perl -0777` tek eşleşme → build → filtreli test → yedeği geri kopyala → `cmp`)

| Kod | Mutasyon | Kırmızıya dönen testler | Geri alma |
|---|---|---|---|
| M1 | Eski kayıtlar katlanırı `BaslangictaAcik="true"` (**açık varsayılan**) | E2, E10, E16, EmanetEkranTestleri.Elimizdeki_emanet_ve_hareketler_gosterilir, S35_12…S35_18 (11) | cmp AYNI |
| M15 | `IadeAltSinir` 0,001 → 0,0001 | E13, S3 | cmp AYNI |
| M16 | `SifirMi` yarım uzağa → ToEven | E15, S12 | cmp AYNI |
| M17 | Toz artık sebebi kaldırıldı (hep "emaneti yok") | E13, E15, S3, S12 | cmp AYNI |
| M18 | Hesap katlanırı hep açık | E17 | cmp AYNI |
| M19 | Tablo hücresi yeniden `Bicim.Has` (has ekli) | E16 | cmp AYNI |
| M20 | Satır düğmesi yeniden hub `Kabuk.Storno` | E16 | cmp AYNI |

7/7 kırmızı; test açığı yok. Mutasyonlar gerçek ağaca hiç uygulanmadı; son durumda kopya `Emanet.razor` = gerçek `Emanet.razor` (cmp). Gerçek ağaçta son build 0 hata.

### 00.4 Gerçek tarayıcı (başsız Chrome + ham CDP; tarayıcı bölmesi kullanılmadı; oturum inceleme-vekili ile parolasız)

**Müşteri durumları** (`t27/durum.mjs`, sikisma.js + ekstra.js + tablo ölçümü): 6 durum (seçilmedi · emanetsiz · emanetli · yalnız gümüş · toz artık 0,0005 · 12 kayıtlı) × geçmiş kapalı/açık × iade çekmecesi × {BASİT, PRO} × {açık, koyu} × {1440, 1280, 960, 375} = 176 + 88 (960/375 düzeltme sonrası yeniden) ölçüm.

- Sayfa: **K1, K3, K5, K7, K8, K9, K9b, K10, K10b, K11, K12, K13 = 0**; yatay kaydırma 0; "0,0000" 0; "Eksik olan" 0; büyük harf metin 0 (tablo başlıkları hariç, § 3-S3). K2/K4 bilgi amaçlı dağılım sayaçlarıdır, kusur değil.
- İade çekmecesi: K9 = 2/durum, yalnız çekmece ile karartılmış perdenin altındaki sayfa öğeleri arasında (`#sayfa-basi-emanet-baslik ⟂ label "Elimizdeki emanet"`); çekmece içi binen 0 (önceki turla aynı).

| Tablo ölçümü (Eski kayıtlar açık) | scrollWidth / clientWidth | Düğme sağ / kap sağ | Kesik düğme | Kırık tarih | Satır yük. |
|---|---|---|---|---|---|
| 1440 · BASİT/PRO · açık/koyu (4 müşteri) | 1090 / 1090 | 1225–1267 / 1399 | 0 | 0 | 69 |
| 1280 · BASİT/PRO · açık/koyu | 930 / 930 | 1113–1149 / 1239 | 0 | 0 | 69 |
| 960 · BASİT/PRO · açık/koyu (düzeltme sonrası) | 610 / 610 | 868–901 / 919 | 0 | 0 | 69 (toz: 101, iki düğme alt alta) |
| 375 | 564–648 / 317 (kap içi kaydırma; sayfa yatay kaydırma 0) | — | 0 | 0 | 69 |

| Durum @1440 BASİT açık | Özet | İade | Kilit metni | İçerik alt kenarı / 900 |
|---|---|---|---|---|
| Müşteri seçilmedi | — | kilitli | Önce müşteri seçin. | 406 |
| Emaneti olmayan | Emaneti yok | kilitli | Bu müşterinin emaneti yok. | 522 |
| Emanetli (K22 10 g) | 9,1600 has | açık | — | 493 |
| Yalnız gümüş (G925 100 g) | Gümüş 92,5000 has | açık | — | 493 |
| Toz artık (0,0005) | 0,0005 has | **kilitli** | **Kalan miktar iade için çok küçük.** | 522 |
| 12 kayıtlı | 71,4480 has | açık | — | 493 |

1440 açılışı her durumda ≤ 1 ekran boyu.

**tur.mjs** /emanet × {BASİT, PRO} × {aydınlık, karanlık} × {1440, 960, 375}: 12/12 sayfa, hata 0. analiz.py tek bulgu: 375'te kabuğun global arama paleti SERBEST-YÜZEN (KATMAN bileşeni; önceki turlarda da vardı). ("eksik" satırı, aracın tam site turu için koyduğu ≥42 sayfa eşiğidir; tek rota koşumunda anlamsız.)

**tutarlilik.mjs** 1440 (CDP 9990, araç artık parametreli): **"mod farki 0"**, düğme sınıf grubu 4 (iki büyük düğme h52 / 16 px, iki modda aynı).

**Ekran görüntüleri** (`ham/TUVAL-KARSILASTIRMA/`, Read ile bakıldı): `EMANET-1-t27-eski-kayitlar-BASIT-1440.png`, `…-BASIT-1280.png` (12 satır, düğmeler tam, tek satır tarih), `…-eski-kayitlar-960.png`, `…-eski-kayitlar-375.png`, `EMANET-1-t27-toz-artik-kilitli-1440.png`, `EMANET-1-t27-iade-cekmece-hesap-kapali-1440.png`, `EMANET-1-t27-bos-musteri-1440.png`, `EMANET-1-t27-gumuslu-PRO-karanlik-1440.png`, `EMANET-1-t27-tuval-yan-yana-1440.png` (Musteriler tuvali ile yan yana; Emanet'in kendi tuvali yok, bölge tablosu § 7.5 geçerli; bu turda değişen bölge yalnız "Geçmiş liste": satır eylemleri kısa ve yan yana — tuvaldeki tablo satır dili ile AYNI).

### 00.5 Canlı doğrulama (API 5188, kiracı `PAKETEMANET-1-TEST T27`)

| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /sistem/davetler · /auth/kayit · /auth/giris | 201 · 201 · 200 | 201 · 201 · 200 |
| POST /cari ×5 | 201 | 201 ×5 |
| GET /kur/guncel | 200 + id | 200, id var |
| POST /emanet/giris (K22 10 g; G925 100 g; K22 1 g; K22 1…12 g ×12) | 200 | 200 ×15 |
| POST /emanet/{toz}/cikis 0,9155 | 200 | 200 |
| POST /emanet/{toz}/cikis 0,0005 | 400 | **400** `hasGrami: "Has gram 0,001 veya daha büyük olmalıdır"` |
| GET /emanet/{…} | toz 0,0005; gümüş [GUMUS 92,5], altın 0 | 200: toz `0.0005 [('ALTIN',0.0005)]`; gümüşlü `0.0 [('GUMUS',92.5)]`; emanetli 9,16; 12 kayıtlı 71,448 / 12 hareket |

```
SELECT c.ad,h.tur,h.has_grami,h.tl_tutari,h.aciklama FROM hareketler h JOIN cariler c … WHERE tenant='PAKETEMANET-1-TEST T27' AND tur LIKE 'EMANET%'
Emanetli Musteri  EMANET_GIRIS   9.160000 0.00 Bilezik
Gumuslu Musteri   EMANET_GIRIS  92.500000 0.00 Gumus kolye
Toz Musteri       EMANET_GIRIS   0.916000 0.00 Kucuk yuzuk
Toz Musteri       EMANET_CIKIS  -0.915500 0.00 kismi iade - toz artik
SUM(has_grami): Toz 0.000500 (2 satır) · Cok Kayitli 71.448000 (12 satır)
```

**Temizlik:** API ve vekil PID ile durduruldu; `lsof` 5188 ve 5189 boş. Kiracı (`01M3JAVQ…`) `tenant_id` kolonu olan 70 tablodan ve `tenants`'tan silindi; silmeden sonra sayım: kalan satır 0, `PAKETEMANET%` kiracısı 0. `tenants` başlangıçta 18, sonda 19: fark eşzamanlı şeridin kiracısı (`PAKETZO-ISLEM-A-TEST`, 21:21 UTC). QA-TEST kiracılarına dokunulmadı.

### 00.6 DoD sözleşmesi

Yeni varlık yok; uçlar/gövdeler değişmedi (`EmanetModulu.cs`'e bu turda dokunulmadı). Emanet hareketi açıklaması `PATCH /emanet/hareketler/{id}` (SatirIciDuzenle), gram/ayar/maden/müşteri salt okunur + DegismezIpucu, düzeltme yalnız 48-A stornosu — § 4 aynen geçerli. Yeni düğme yok; "Geri al" aynı `StornoIste` → ortak onay modalı yolunu kullanır; disabled iken `title` sebebi ve tablo altı DegismezIpucu duruyor. Yeni `<details>` yalnız yerel katlanır (istek atmaz).

**Dokunulan sayfalar (bu tur):** `Pages/Emanet.razor` — aynı paketin (EMANET-1) doğrulayıcı KALDI düzeltmesi; başka dalgaya ait ikinci dokunuş değil.

### 00.7 Değişen dosyalar (bu tur)

| Dosya | Değişiklik |
|---|---|
| `dotnet/Kuyumcu.Web/Pages/Emanet.razor` | İade alt sınırı (0,001) + toz sebebi, `SifirMi` yarım uzağa, iade maden seçicisi `IadeMadenleri`, hesap katlanırı `#e-cikis-hesap`, tabloda `HasSayi`, kısa "Geri al", `emanet-tarih`/`emanet-satir-eylem` sınıfları |
| `dotnet/Kuyumcu.Core/I18n/Sozlukler.EmanetSade.cs` | `IadeCokKucuk`, `CikisHesapEkle`, `SatirGeriAl` (TR+EN) |
| `dotnet/Kuyumcu.Web/wwwroot/css/emanet.css` | Özet 520 px; tarih ve satır eylemleri `nowrap`; ≤1100 px düğmeler alt alta (renk yok, belirteç dışı değer yok) |
| `dotnet/Kuyumcu.Tests/EmanetSadeTestleri.cs` | E13–E17 + sahte API c5/c6/c7 |
| `dotnet/Kuyumcu.Tests/Dogrulayici/EMANET1SinirTestleri.cs` | S3, S12 yeni kurala göre |
| `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/EMANET-1-t27-*.png` | 9 görüntü |

### 00.8 Açık kalanlar (sahibiyle)

- **Orkestratör / T-K1-BICIM — has binlik gruplama (hakem KRİTİK'i):** `Bicim.Has` P-57 kararıyla gruplamasız. Karar: ya P-57 revize edilip `Bicim.Has` "#,##0.0000" olur (tüm ekranlar birlikte; S6/S13 literalleri "999.999.999,9900 has" olarak güncellenir), ya P-57 korunur ve hakem bulgusu gerekçeli kapanır. EMANET-1 alanı `Bicim.cs`'e dokunamadığı ve yerel gruplama UX-S31'i bozacağı için uygulanmadı.
- **Orkestratör:** arka plan işi silinmiş kiracıya `is_calismalari`/`denetim_kayitlari` yazıyor; `kiraci-temizle.sh` tenant satırı yokken yetim satırları temizlemiyor.
- **KATMAN:** 375'te global arama paleti SERBEST-YÜZEN; Secici'nin sunucu araması yok (50+ müşteride `#e-cari-ara` kutusu gerekli kalıyor).
- **Tam süitteki 8 kırmızı** (başkasına ait): SATIS-3 ×4, ZO-ISLEM-A ×1, KATMAN ×1, OLCEK-1 ×1, ORTAK-1 ×1 (adlar § 00.2).
- Tasarım: Emanet için tuval ekranı (`Emanet.dc.html`) hâlâ yok.
- Mevzuat: yeni hüküm yok; 0,001 alt sınırı sunucu DTO'sunun mevcut kuralı (mevzuat değil, kayıt hassasiyeti).

## 0. Devralma turu (27.09, t9 · API 5188 · vekil 5189 · CDP 9981–9997)

Paket 25.09'da kota bitince kesilmişti. Ağaçtaki iş korundu, yeniden yazılmadı. Bu tur doğrulayıcının iki KRİTİK bulgusunu kapattı, kabul ölçümlerini güncel ağaçta yeniden koştu ve 960 px'te bulunan yeni bir kırılmayı düzeltti.

### 0.1 Doğrulayıcı bulguları (KALDI) — durum

| Bulgu | Koddaki düzeltme (ağaçta, orkestratör 25.09 18:0x + önceki uygulayıcı) | Kanıt (27.09) |
|---|---|---|
| [KRİTİK] Yalnız gümüş emanetli müşteride özet maden adsız "92,5000 has" yazıyor, aynı anda "emaneti yok" diyor, iade kilitli | `OzetMetni`: tek maden altın değilse "{MadenAdi} {Has}". Kilit `IadeEdilecekVar` ile MADEN BAZLI: herhangi bir madenin 4 haneye yuvarlanmış neti > 0 ise iade açık. İade çekmecesi `DoluMadenler` listesini kullanıyor | bUnit E11 + S8 yeşil; mutasyon M12/M13 kırmızı. Canlıda G925 100 g → ekranda **"Gümüş 92,5000 has"**, iade AÇIK, `#e-kilit` yok; 4 mod×tema × 3 genişlik |
| [KRİTİK] 4 haneye yuvarlanınca 0 olan bakiye "0,0000 has" basılıyor | `SifirMi(g) => Math.Round(g,4)==0`; özet, kilit ve çekmece listesi bunu kullanıyor | bUnit E12 + S5 yeşil; mutasyon M14 kırmızı. Canlıda 0,916 giriş − 0,91596 iade → SQL neti 0,000040 → ekranda **"Emaneti yok"**, iade kilitli, "0,0000" sayısı 0 |

### 0.2 Bu turda yapılan kod değişikliği

- `wwwroot/css/emanet.css`: "Eski kayıtlar" tablosunda `th` ve `td.sayi` için `white-space: nowrap`. 27.09 ölçümünde BASİT/aydınlık/960'ta "Has gram" başlığı iki satıra kırılıyordu (K3_kirik = 1, 3 durum). Değer hücresi de "9,1600 / has" diye bölünüyordu. Tablo zaten `.tablo-sar` kaydırma kabında. Düzeltmeden sonra 960, 1440 ve 375'te K = 0 ve yatay kaydırma 0. Renk yok, belirteç ya da `text-transform` eklenmedi.
- Başka dosyaya dokunulmadı. Ağaçta yarım mutasyon YOK: `/private/tmp` yeniden başlatmada silinmişti. Bu yüzden Emanet testlerinin hepsinin yeşil olması ve `Katlanir.razor`'da `acik = BaslangictaAcik;` satırı kontrol edildi.

### 0.3 Testler (güncel ağaç)

- `dotnet build Kuyumcu.sln`: 21:44'te 0 uyarı, 0 hata. Mutasyonlardan sonraki temiz build de 0/0 (21:47).
  - Son build (22:30) 1 hata verdi: `SATIS3EkranTestleri.cs(349,96): CS0117 'Sozluk' bir 'Tr' tanımı içermiyor`. Bu SATIS-3 şeridinin o sırada yazılan test dosyası, EMANET-1'e ait değil.
- Filtreli koşum (Emanet'e dokunan tüm sınıflar + `EMANET1SinirTestleri`): **138/138 geçti**.
- Emanet, `TasarimBelirtec` ve ortam testleri gerçek ağaçta: **100/100 geçti**.
- Bu turda yeni test yazılmadı. E11 ve E12 (EmanetSadeTestleri) ile doğrulayıcının S1–S11'i ağaçta vardı; toplam paket testi 12 + 14.

### 0.4 Mutasyonlar (27.09'da yeniden koşuldu; `perl -0777` ile TEK eşleşme → filtreli test → yedeği geri kopyala → `cmp`)

| Kod | Mutasyon | Kırmızıya dönen testler | Geri alma |
|---|---|---|---|
| M1 | Katlanır `BaslangictaAcik="true"` (**açık varsayılan**) | E2, E10, EmanetEkranTestleri.Elimizdeki_emanet_ve_hareketler_gosterilir, S35_12…S35_18 (10) | cmp AYNI |
| M2 | Özet sıfırda `Bicim.Has(0m)` | S1, S5, E3, E12 | cmp AYNI |
| M3 | Kilit metnine "Eksik olan: " öneki | S1, E1, E3, E12 | cmp AYNI |
| M12 | Tek maden gümüşken maden adı yazılmaz | S8, E11 | cmp AYNI |
| M13 | İade kilidi yalnız altına bakar (`ElimizdekiHas`) | S8, E11 | cmp AYNI |
| M14 | `SifirMi` yuvarlamasız (`gram == 0m`) | S5, E12 | cmp AYNI |

6/6 kırmızı verdi. Önceki turdaki M4–M11 (§ 6) kod değişmediği için geçerli. Son mutasyondan sonra temiz build 0 uyarı, 0 hata.

### 0.5 Gerçek tarayıcı (başsız Chrome + ham CDP, tarayıcı bölmesi kullanılmadı)

- **tur.mjs** /emanet × {BASIT, PRO} × {aydınlık, karanlık} × {1440, 960, 375}: 12/12 kombinasyon ölçüldü.
  - K1..K13 = 0.
  - analiz.py tek bulgu verdi: 375'te kabuğun global arama paleti (`Müşteri, ürün ya da fi…`) için SERBEST-YÜZEN. Bu KATMAN şeridinin bileşeni; 25.09'da da vardı.
- **Müşteri seçili durumlar** (`durum.mjs`): 5 müşteri durumu × geçmiş kapalı/açık × 2 mod × 2 tema × 3 genişlik.
  - Toplam 88 + 64 + 16 + 32 ölçüm.
  - Düzeltmeden sonra: K1, K3, K5, K7…K13 = 0; yatay kaydırma 0; "0,0000" 0; "Eksik olan" 0; tablo başlıkları dışında büyük harf 0.
  - İlk koşumdaki 8 "secenek-yok" kaydı betiğin bekleme zamanlamasından geldi. Beklemeli ikinci koşumda (durum3/4/5) 0.
- **tutarlilik.mjs** 1440 BASİT↔PRO: **"mod farki 0"**, düğme sınıf grubu 4 (iki büyük düğme h52, 16 px, iki modda aynı).
  - CDP portu 9997 olan scratchpad kopyasıyla koşuldu. Araç dosyasına dokunulmadı.

| Durum @1440 BASİT aydınlık | Özet | İade | Kilit metni | İçerik alt kenarı / 900 |
|---|---|---|---|---|
| Müşteri seçilmedi | — | kilitli | Önce müşteri seçin. | 406 |
| Emaneti olmayan | Emaneti yok | kilitli | Bu müşterinin emaneti yok. | 522 |
| Emanetli (K22 10 g) | 9,1600 has | açık | — | 493 (geçmiş açık 730) |
| Yalnız gümüş (G925 100 g) | **Gümüş 92,5000 has** | **açık** | — | 493 (730) |
| Artık 0,00004 has | **Emaneti yok** | kilitli | Bu müşterinin emaneti yok. | 522 (828) |

1440 açılışı her durumda 1 ekran boyunun altında.

**Ekran görüntüleri** (`ham/TUVAL-KARSILASTIRMA/`, Read ile bakıldı):
- `EMANET-1-v2-gumuslu-musteri-1440.png`, `EMANET-1-v2-artik-0_00004-musteri-1440.png`, `EMANET-1-v2-emanetli-musteri-1440.png`: tuval kart dili (Musteriler.png) ile yan yana.
- `EMANET-1-v2-gumuslu-PRO-karanlik-1440.png`
- `EMANET-1-v2-eski-kayitlar-acik-960.png`: düzeltmeden sonra "Has gram" ve "9,1600 has" tek satırda.
- `EMANET-1-v2-gumuslu-375.png`: 375'te iki eylem alt alta, yatay kaydırma yok.

Emanet için tuval ekranı hâlâ yok. Bölge tablosu § 7.5'teki gibi geçerli; bu turda bölge farkı değişmedi.

### 0.6 Canlı doğrulama (API 5188, kiracı `PAKETEMANET-1-TEST Kuyumculuk`)

- API 5188'de koşuldu. Diğer şeritlerin eşzamanlı `--no-incremental` build'leri bin klasörünü silip durduğu için API, bin'in `/private/tmp/kuyumcu-serit/EMANET-1/api-bin` altındaki anlık kopyasından `dotnet Kuyumcu.Api.dll` ile çalıştırıldı.
- Sistem anahtarı, bu tur için üretilmiş rastgele bir değerle `Sistem__Anahtari` ortam değişkeninden verildi (dosya chmod 600; değer basılmadı).

| Adım | Beklenen | Gerçek |
|---|---|---|
| POST /sistem/davetler | 201 | 201 |
| POST /auth/kayit · /auth/giris | 201 · 200 | 201 · 200 |
| POST /cari ×4 | 201 | ilk deneme 400 "Cari türü zorunludur" (betik `tip` yazmıştı), `tur: MUSTERI` ile 201 ×4 |
| GET /kur/guncel | 200 + id | 200, id var |
| POST /emanet/giris K22 10 g · G925 100 g · K22 1 g | 200 | 200: has 9.16 · 92.5 · 0.916 |
| POST /emanet/{c}/cikis 0.9166 (fazla) | 400 | 400 "Yetersiz altın emaneti. Mevcut: 0.9160…" |
| POST /emanet/{c}/cikis 0.91596 | 200 | 200 → neti 4e-05 |
| POST gümüşlü müşteriden `maden: ALTIN` iade | 400 | 400 "Yetersiz altın emaneti. Mevcut: 0.0000…" (K20-9 kuralı sağlam) |
| GET /emanet/{gümüşlü} | has 0, madenler [GUMUS 92.5] | 200, `mevcutHasGramBakiye 0.0`, `[('GUMUS', 92.5)]` |

```
SELECT c.ad,h.tur,h.has_grami,h.tl_tutari,h.aciklama FROM hareketler h JOIN cariler c … WHERE tenant='PAKETEMANET-1-TEST' AND tur LIKE 'EMANET%'
Artikli Musteri  EMANET_CIKIS  -0.915960 0.00 kismi iade - artik testi
Artikli Musteri  EMANET_GIRIS   0.916000 0.00 Kucuk yuzuk
Emanetli Musteri EMANET_GIRIS   9.160000 0.00 Bilezik
Gumuslu Musteri  EMANET_GIRIS  92.500000 0.00 Gumus kolye
```

**Temizlik:**
- API ve vekil PID ile durduruldu; `lsof` 5188 ve 5189 boş.
- Kiracı, `tenant_id` kolonu olan 70 tablodan ve `tenants`'tan silindi. Kalan satır 0; `PAKETEMANET%` kiracısı 0.
- `tenants` sayısı başlangıçta 17, sonda 18. Fark eşzamanlı şeritlerin kiracıları (PAKETZO-ISLEM-A-TEST, PAKETKATMAN-1b-TEST Olcum vb. listede). QA-TEST kiracılarına dokunulmadı.

### 0.7 Tam süit ve denetim (şerit kapısı madde 7)

- **Tam süit:**
  - Yöntem: ağaç 22:04:21'de bin/obj hariç `/private/tmp/kuyumcu-serit/EMANET-1/j2/dotnet`'e kopyalandı, orada build edilip koşuldu. Aynı dizinde koşulan ilk deneme, başka şeritlerin `--no-incremental` build'leri DLL'leri silince 956 "Could not load file" hatası verdi ve geçersiz sayıldı.
  - Sonuç: **6333 test · 6300 geçti · 33 başarısız** (trx: `/private/tmp/kuyumcu-serit/trx-pEMANET-1c`). Hiçbiri Emanet sayfasını ya da bu paketin dosyalarını kullanmıyor.
  - Devir notundaki 8 kırmızı (DenetimIzi ×7, P39.K5) bu koşumda YEŞİL.
  - 5'i kopya ortamından: `.gitignore`, `.claude/launch.json` ve `.github` kopyaya bağlanmamıştı. Bu 5 test gerçek ağaçta koşuldu ve YEŞİL: Gitignore_yeni_mutorj, LaunchJson_YayinYoluSabitDegil, inceleme_sunucusu_URETIM…, denetim_sh_BASH_DISI…, YayinCiktisiKaynakAgacinaGIRMEZ.
  - 28'i başka şeritlerin yarım işi:
    - KarmaOdeme ×5, B4AltinBacagi ×4, P10PosBacagi ×4, S21Fis ×4, S24EskiAltin ×2, DovizBacagi ×2, S22TezgahFisi ×1. Hata: `'Modu' … no registered service` (Fis bileşenleri ve AltinBacagiPaneli). Sahibi SATIS-3.
    - UX4b ×2 (POS cihaz seçici) ve ZOISLEMA ×1. Sahibi ZO-ISLEM-A.
    - S34RaporBugun ×1 ve RaporEkran ZB19 ×1. Sahibi ZO-BELGE.
    - OrtakGorunum G1 ×1. Sahibi ORTAK-1.
    - OlcumKapisi ×2 ve MigrationHijyeni ×1: kopya ortamı ya da orkestratör. Emanet dışı.
  - **Bu paketin ürettiği yeni kırmızı: 0.**
- **denetim.sh:**
  - 22:07'de **DENETİM TEMİZ** (289 ✓): ModalPencere 48 ≤ 55, P-12 STORNO_GORUNMEYEN_EKRAN 0.
  - 22:31'de P-39 TABLO_SARMALAYICISIZ = 2 çıktı. Dosya `PosTahsilatlar.razor` (ZO-ISLEM-A şeridi, o dakikalarda değişti); aynı kayıt P39.K5'i de kırıyor. Emanet'in tablosu `tablo-sar` içinde.

### 0.8 Açık kalanlar (sahibiyle)

- SATIS-3: `SATIS3EkranTestleri.cs` CS0117 derleme hatası ve Fis/AltinBacagi 'Modu' servis kırmızıları.
- ZO-ISLEM-A: PosTahsilatlar.razor'da tablo sarmalayıcısız ×2 (denetim ✗, P39.K5) ve UX4b/ZOISLEMA kırmızıları.
- KATMAN: 375'te global arama paleti SERBEST-YÜZEN. Secici'nin sunucu araması yok (50'den çok müşteride `#e-cari-ara` kutusu gerekli kalıyor). "Grubun açık/kapalı durumu…" bandı QA'ya ayrı bulgu.
- Tasarım: Emanet için tuval ekranı (`Emanet.dc.html`) yok.
- Araç: `tutarlilik.mjs` CDP portu 9371'e sabit yazılı; parametreli olmalı.
- Güvenlik notu (Mehmet'e): `appsettings.Development.json` anahtarlarını listelerken yazdığım sed süzgeci `Sifreleme:Anahtar` geliştirme değerini maskeleyemedi. Değer yalnız benim araç çıktıma düştü; rapora ve başka bir yere yazılmadı. Geliştirme anahtarıdır, ama üretimde aynı değer kullanılmamalı.
- Mevzuat: yeni hüküm yok. "Emanet işletme malı değildir" metni mevcut bilgi; mali müşavir notu § 9'daki gibi.

## 1. Spesifikasyonun koda karşı doğrulanması

| Spesifikasyondaki iddia (kurucu görüntüsü, 25.09) | Koddaki durum (öncesi) | Sonuç |
|---|---|---|
| Başlık altında büyük harfli açıklama: "Bu mal SİZİN DEĞİLDİR…" | `Dil.T.Emanet.Aciklama` (hub) → `SayfaBasi` | DOĞRU. Artık `EmanetSadeSozlugu.Aciklama` ("Müşterinin bırakıp sonra geri alacağı altın.") kullanılıyor |
| Küçük "+ Emanet Al" düğmesi | `SayfaBasi Eylem` içinde `btn-kucuk` | DOĞRU. Düğme iki büyük eşit eylemden biri oldu |
| Ayrı "Müşteri ara" kutusu + seçici + tam genişlikte "↩ İade Et" | `<Arama e-cari-ara>` + `<Secici e-cari>` + `#e-cikis-ac` | DOĞRU. Artık tek seçici var, ayrı kutu yalnız liste sınırdayken çıkıyor (§ 3-S1) |
| "Eksik olan: Bu müşterinin elimizde emaneti yok…" | `@Dil.T.Yonetim.KilitSebebi: @CikisKilitSebebi` | DOĞRU. Önek kaldırıldı, sebep tek kısa cümle ("Bu müşterinin emaneti yok.") |
| "ELİMİZDEKİ EMANET 0,0000 has" | `StatKart` (app.css büyük harf) + `Bicim.Has(0)` | DOĞRU. Artık kendi özet satırı var; sıfırsa "Emaneti yok" yazıyor |
| "⚠ …değerlemesine GİRMEZ." | `EmanetMulkiyetUyarisi` her zaman görünüyordu | DOĞRU. Bilgi artık kapalı (i) ipucunda (`<details>`), büyük harf yok |
| Boş "Emanet hareketleri" kartı | Her zaman açık kart | DOĞRU. Artık "Eski kayıtlar" katlanırı var ve KAPALI geliyor |
| `KurSnapshotId` kullanıcıya sorulmuyor | `kur!.Id` otomatik gidiyor | Zaten doğruydu; değişmedi |
| Taş ağırlığı ve açıklama isteğe bağlı | Formda hep açıktı | Artık "+ Taş ağırlığı ve not ekle" katlanırının arkasında |
| Riskte anılan "Grubun açık/kapalı durumu kaydedilemedi" bandı | Emanet.razor'da yok. Menü tercih kanalına (KATMAN şeridi) ait | Kapsam dışı; QA'ya ayrı bulgu olarak bildirilir |

**Sunucu:** Uçlar, gövdeler, kilit KOŞULLARI, yasal kurallar ve mevcut DOM id'leri değişmedi. `EmanetModulu.cs`'e dokunulmadı; `git status`ta görünen `M` işareti önceki paketlerden kalma. Migration gerekmedi. Mevzuat hükmü eklenmedi; "emanet kâr-zarara girmez" metni önceden vardı, yalnız yeri ve biçimi değişti.

## 2. Yapılanlar

- **`Pages/Emanet.razor`**
  - Açılış sırası: sade cümle → (i) "Emanet nedir?" ipucu → müşteri kartı (tek seçici ve seçilince emanet özeti) → iki büyük eşit eylem (`#e-giris-ac` "Emanet al" birincil, `#e-cikis-ac` "Emanet iade et" ikincil; ikisi de `btn-buyuk`) → tek kısa kilit sebebi (`#e-kilit`) → "Eski kayıtlar" (`Katlanir Id="e-gecmis"`, kapalı, kayıt sayısı rozetiyle).
  - Liste yükleme hatası katlanırın üstünde bant olarak görünür (`#e-liste-hata`, UX-S28). Hata varken rozet çizilmez ve boş durum basılmaz.
  - Özet (`OzetMetni`): sıfırsa "Emaneti yok". Has 4 haneyle yalnız sıfır değilse yazılır. Birden çok maden varsa madenler ayrı yazılır (K20-2).
  - Emanet al çekmecesi: taş ağırlığı ve not "+ Taş ağırlığı ve not ekle" (`<details id="e-giris-detay">`) arkasında. Alanda hata ya da değer varsa katlanır AÇIK gelir, böylece sessiz etkisizlik olmaz.
  - İade çekmecesi: not "+ Not ekle" arkasında. Ayar/gram hesap yardımcısı görünür kaldı (mevcut testler ve UX-S21).
  - Çekmece başlıkları düğme adıyla aynı: "Emanet al" / "Emanet iade et" (UX-S31).
  - Canlı ölçümde (K8) hub metninde "cari"/"storno" jargonu çıktı. Bu yüzden `DegismezIpucu EmanetHareketiFinansalAlan` artık S3-5'in sade metinlerini (`M.KilitSebebi`, `M.DuzeltmeYolu`) kullanıyor.
  - `Dil.T.Urun.MadenUyarisi` ("Toplam has YALNIZ altındır") yerine büyük harfsiz `S.MadenNotu` kullanılıyor.
- **`Kuyumcu.Core/I18n/Sozlukler.EmanetSade.cs` (yeni):** `EmanetSadeMetinleri` konumsal record'u, TR ve EN. Eksik anahtar derleme hatası verir. Hub'a dokunulmadı.
- **`wwwroot/css/emanet.css` (yeni):** Kurallar `#emanet-sayfa` kapsamlı. Renkler yalnız belirteçten, satır içi stil ve `text-transform` yok. `index.html`'e `sade.css`'ten sonra, `olcek.css`/`ortak.css`'ten önce bağlandı (O1: ortak.css en sonda kalıyor).
- **Katlanir:** SATIS-2'nin ortak `Bilesenler/Katlanir.razor` bileşeni hazırdı ve kullanıldı; bileşene dokunulmadı. Not: koşum sırasında bir kez `acik = true;` satırı görüldü. Bu SATIS-2'nin mutasyon anıydı ve kısa süre sonra geri alındı. Aynı ağaçta eşzamanlı mutasyon, Emanet testlerinde anlık kırmızı üretebilir.

## 3. Sapmalar ve gerekçeleri

- **S1: `#e-cari-ara` artık koşullu.** Kapsam "ayrı arama kutusu YOK" diyor, sözleşme ise "id'ler korunur" diyor. İkisi şöyle uzlaştırıldı:
  - Kutu yalnız liste sunucu sınırına (50) değdiğinde ya da kullanıcı zaten sorgu yazmışken çıkıyor. 50'den az müşteride seçicinin kendi arama kutusu (`#e-cari-ara-girdi`) listenin tamamını yerinde süzdüğü için eksiksiz.
  - İd, bileşen ve `/cari?aktif=true&q=&limit=50` sözleşmesi aynı kaldı.
  - Secici'yi (KATMAN şeridi) sunucu aramasına bağlamak alan dışı olduğu için yapılmadı.
  - UX4c M10 testleri (60 müşteri) değişmeden yeşil. S35_18, sınırdaki listeyle koşacak biçimde uyarlandı.
- **S2: Tuval.** `tuval/project/` altında Emanet ekranı YOK (Ayarlar, Ekranlar, Giris, Musteriler, Panel, Profesyonel, Tezgah var). Bu yüzden görsel karşılaştırma tuvalin kart dili üzerinden yapıldı: Musteriler.png'deki 1 px ayraç, 10–12 px köşe, 18 px Manrope, "Daha fazla" katlanırı. Bölge tablosu § 7'de.
- **S3: Tablo başlıkları büyük harf.** "Eski kayıtlar" açıldığında tablo başlıkları (TARİH/TÜR…) büyük harf görünüyor. Bu app.css'in ortak `th` stili ve tuval Musteriler'de de aynı ("MÜŞTERİ · TELEFON"), yani vurgu metni değil, tasarımın başlık dili. Kapalı açılışta ve tüm açıklama/uyarı metinlerinde büyük harf 0.
- **S4: Mevcut testlerde uyarlama (6 dosya).** Davranış değiştiği için şu testler güncellendi: satırlar artık katlanırda (açma tıklaması eklendi), düğme metinleri değişti (id ile bulunuyor), kilit sebebi yeni kısa metin. Kilit KOŞULU iddiaları aynı kaldı.

## 4. DoD sözleşmesi (yeni varlık yok)

| Varlık | Düzenleme yolu | Not |
|---|---|---|
| Emanet hareketi: açıklama | `PATCH /emanet/hareketler/{id}` (SatirIciDuzenle, P-12) | Değişmedi; artık "Eski kayıtlar" açılınca görünüyor |
| Emanet hareketi: gram, ayar, maden, müşteri | Salt okunur. `DegismezIpucu` ile gerekçe ve yol ("Yanlışsa önce geri alın…") | Düzeltme yalnız 48-A stornosuyla yapılır (`/islemler/{id}/storno`); storno taklidi yok |
| Emanet giriş/çıkış | `POST /emanet/giris`, `POST /emanet/{cariId}/cikis` | Gövdeler değişmedi |

Yeni düğmeler: `#e-giris-ac` ve `#e-cikis-ac` gerçek çekmece açar ya da `disabled` olur ve sebebi `#e-kilit`'te yazılır. İpucu ve "+ ekle" kontrolleri yerel `<details>/<summary>`: istek atmıyor, JS gerektirmiyor. Pages altında yeni ModalPencere kullanımı yok (denetim: 48, tavan 55).

**Dokunulan sayfalar:** `Pages/Emanet.razor` (bu dalgada tek dokunuş).

## 5. Testler

**Yeni: `Kuyumcu.Tests/EmanetSadeTestleri.cs` (10 bUnit testi)**

| Test | Sınanan davranış |
|---|---|
| E1 | Açılışta yalnız zorunlu öğeler var: tek sebep, "Eksik olan" yok |
| E2 | Eski kayıtlar KAPALI geliyor, tıklayınca açılıyor, sayı rozeti var |
| E3 | Emaneti olmayan müşteride "Emaneti yok" yazıyor, 0,0000 yok, iade kilitli |
| E4 | Emaneti olan müşteride has 4 hane, iade açık |
| E5 | Ekranda, iade çekmecesinde ve sözlükte (TR+EN) büyük harf vurgu yok; çekmece başlığı düğme adıyla aynı |
| E6 | İki büyük eşit eylem, tek birincil |
| E7 | Ayrı arama kutusu yalnız 50 sınırında çıkıyor; `/cari?aktif=true&q=…&limit=50` sözleşmesi korunuyor |
| E8 | (i) ipucu kapalı; sade açıklama cümlesi yerinde |
| E9 | İsteğe bağlı alanlar katlanırda; hata olunca katlanır açılıyor, POST yok |
| E10 | Geçmiş yükleme hatası katlanır kapalıyken de görünüyor; boş durum basılmıyor |

**Filtreli koşum (Emanet'e dokunan tüm sınıflar):** `EmanetSadeTestleri`, `EmanetEkranTestleri`, `S35EmanetEkranTestleri`, `UX4cEkranTestleri`, `P12EkranTestleri`, `KilitGerekceTestleri`, `UX1C2EkranTestleri`, `P37cPanelVeKapsamTestleri` → **122/122 geçti** (son durum, 0 başarısız).

**Tam süit** (`--logger trx`, `scratchpad/trx-pEMANET-1`): **6287 test · 6279 geçti · 8 başarısız.** Hiçbiri Emanet'e ait değil (§ 9). Paket öncesi tam süit koşulmadı, bu yüzden "önce" sayısı ölçülmedi. Bu paketin eklediği test sayısı 10.

## 6. Mutasyon kanıtları

Yöntem: kaynak scratchpad'e yedeklendi → `perl -0777` ile tek eşleşmeli değişiklik yapıldı → build (0 hata) → filtreli test → yedek geri kopyalandı → `cmp` ile birebir aynı olduğu doğrulandı. Son mutasyondan sonra temiz build: 0 hata, 0 uyarı.

| Kod | Mutasyon | Kırmızıya dönen test | Geri alma |
|---|---|---|---|
| M1 | Katlanır `BaslangictaAcik="true"` (**açık varsayılan**) | E2, E10, EmanetEkranTestleri.Elimizdeki_emanet_ve_hareketler_gosterilir, S35_12…S35_18 (toplam 10) | cmp AYNI |
| M2 | Özet sıfırda `Bicim.Has(0)` | E3 | cmp AYNI |
| M3 | Sebebe "Eksik olan: " öneki | E3, KilitGerekceTestleri.Emanet_kilitli_dugmeler_sebebini_yazar | cmp AYNI |
| M4 | Ayrı arama kutusu her zaman görünür | E1 | cmp AYNI |
| M5 | Giriş katlanırı hiç açılmaz (`false`) | E9 | cmp AYNI |
| M9 | Giriş katlanırı hep açık (`true`) | E9 | cmp AYNI |
| M6 | Katlanır dışındaki liste hata bandı kaldırıldı | E10 | cmp AYNI |
| M7 | İade düğmesinden `btn-buyuk` kaldırıldı | E6 | cmp AYNI |
| M8 | Sözlükte "GİRMEZ" (büyük harf) | E5 | cmp AYNI |
| M10 | İade çekmecesinde hub'ın "YALNIZ" metni | E5 | cmp AYNI |
| M11 | İade çekmecesi başlığı hub'ın "İade Et" metni | E5 | cmp AYNI |

11/11 mutasyon kırmızı verdi; test açığı yok.

## 7. Gerçek tarayıcı ölçümü

**Yöntem:** Başsız Chrome ve ham CDP (`betikler/tur`); tarayıcı bölmesi kullanılmadı. Oturum `inceleme-vekili.py` ile parolasız açıldı (fikstür chmod 600).

### 7.1 Açılış ekranı (tur.mjs + analiz.py)

- Kapsam: /emanet × {BASIT, PRO} × {aydınlık, karanlık} × {1440, 960, 375} = 12 kombinasyon.
- İlk tur: K9 binen 2 bulgu çıktı (`#e-kilit`'in −6 px kenar boşluğu düğmeye biniyordu). Düzeltildi.
- İkinci tur (tur3): **TOPLAM benzersiz bulgu 0 · temiz rota ['/emanet']**.
- Son tur (tur5): Emanet için 0 bulgu. Tek kayıt, PRO 375'te kabuğun global arama paleti (`#ust-kayit-ara` → `#kabuk-arama-palet`) için "SERBEST-YÜZEN". Bu KATMAN şeridinin bileşeni; tur3'te yoktu.

### 7.2 Müşteri seçili durumlar (kendi CDP betiğim `durum.mjs`, sikisma.js + ekstra.js)

- 48 sayfa durumu ölçüldü: {emaneti olmayan, emaneti olan} × {geçmiş kapalı, açık} × 2 mod × 2 tema × 3 genişlik.
  - **K1, K3, K5 (TRY), K7, K8, K9, K9b, K10, K10b, K11, K12, K13 = 0**
  - Yatay kaydırma 0
  - "0,0000" 0
  - Kapalı açılışta büyük harf 0
- 24 çekmece durumu ölçüldü:
  - K9 binen değerleri yalnız çekmece katmanı ile arkasındaki sayfa öğesi arasında çıktı (ör. `#sayfa-basi-emanet-baslik ⟂ label "Ayar"`). Bu, karartılmış perdenin altındaki sayfa ile çekmece arasındaki katmanlar arası örtüşme. Çekmece içi binen 0.
  - Diğer K'ler 0, yatay kaydırma 0.
  - İsteğe bağlı katlanırlar KAPALI geldi (`detayAcik=false`).

| Durum @1440 aydınlık | İçerik alt kenarı / ekran | Özet | Kilit |
|---|---|---|---|
| Müşteri seçilmedi | ≈390 / 900 | — | "Önce müşteri seçin." |
| Emaneti olmayan, geçmiş kapalı | 522 / 900 | Emaneti yok | "Bu müşterinin emaneti yok." |
| Emaneti olan, geçmiş kapalı | 493 / 900 | 9,1600 has | — |
| Emaneti olan, geçmiş açık | 730 / 900 | 9,1600 has | — |

1440 açılış ekranı ≤ 1,0 ekran boyu koşulunu sağlıyor. Belgenin kaydırma yüksekliği bazı kombinasyonlarda 900'ü geçiyor; bunu kabuk (kenar menü) üretiyor, emanet içeriği değil.

### 7.3 Tutarlılık (tutarlilik.mjs, 1440)

BASIT↔PRO: **"mod farki 0"** (düğme sınıf grubu 4). Aracın CDP portu (9371), izolasyon aralığı dışında olduğu için scratchpad kopyasında 9997'ye alındı. Araç dosyasına dokunulmadı.

### 7.4 Ekran görüntüleri (Read ile bakıldı)

Hepsi `ham/TUVAL-KARSILASTIRMA/` altında:

- `EMANET-1-emanet-acilis-musteri-secilmedi-1440.png`
- `EMANET-1-emanet-bos-musteri-1440.png` (tuvalle yan yana)
- `EMANET-1-emanet-emanetli-musteri-1440.png` (tuvalle yan yana)
- `EMANET-1-emanet-al-cekmece-1440.png` (tuvalle yan yana)
- `EMANET-1-emanet-bos-musteri-375.png`
- `EMANET-1-emanet-eski-kayitlar-acik-PRO-karanlik-1440.png`

### 7.5 Bölge bölge karşılaştırma (tuval kart dili: Musteriler; Emanet'in kendi tuvali yok)

| Bölge | Tuvalde | Bizde | Durum |
|---|---|---|---|
| Kabuk (menü, üst bar) | ByGatech kabuğu | Aynı kabuk (KATMAN/ORTAK şeritleri) | AYNI |
| Sayfa başlığı ve açıklama | Başlık, tek soluk cümle | "Emanet" + "Müşterinin bırakıp sonra geri alacağı altın." | AYNI dil |
| Arama / seçici | Tek arama kutusu ("Ad ya da telefon") | Tek müşteri seçici (içinde arama) | GEREKÇELİ FARK: seçim bir kayda bağlanır (S1) |
| İçerik kartı | 1 px ayraç, 10–12 px köşe, beyaz yüzey | Müşteri kartı 12 px, 1 px ayraç, gölgesiz | AYNI |
| Birincil eylem | Tek dolu mavi düğme (Kaydet) | "Emanet al" dolu mavi, "Emanet iade et" aynı boy çerçeveli | GEREKÇELİ FARK: kapsam "iki büyük eşit eylem" istiyor; birincil tek |
| İsteğe bağlı alanlar | "Daha fazla (adres, TC…)" katlanırı | "+ Taş ağırlığı ve not ekle" katlanırı | AYNI ilke |
| Geçmiş liste | Açık tablo | "Eski kayıtlar" katlanırı, kapalı | GEREKÇELİ FARK: kurucu 25.09 kuralı (geçmiş kapalı) |
| Tablo başlığı | Büyük harf küçük başlık | Aynı (açıldığında) | AYNI (S3) |
| Uyarı metinleri | Büyük harf yok | Büyük harf yok; bilgi (i) ipucunda | AYNI |

## 8. Canlı doğrulama (API 5188, kiracı `PAKETEMANET-1-TEST Kuyumculuk`)

| Adım | Beklenen | Gerçek |
|---|---|---|
| `POST /sistem/davetler` (anahtar ve operatör başlığıyla, gerekçeli) | 201 | 201 (ilk deneme 400: gerekçe zorunlu; gerekçe eklendi) |
| `POST /auth/kayit` | 201 | 201 |
| `POST /auth/giris` | 200 | 200 |
| `POST /cari` ×2 (Emanetli / Emanetsiz Müşteri) | 201 | 201 ×2 |
| `GET /kur/guncel` | 200 + id | 200, id var |
| `POST /emanet/giris` (K22, 10 g) | 200 | 200 |
| `GET /emanet/{c1}` | bakiye 9,16 has, 1 hareket | 200, `mevcutHasGramBakiye=9.16`, 1 hareket |
| `GET /emanet/{c2}` | bakiye 0, 0 hareket | 200, 0.0, 0 |

**SQL kanıtı:**

```
SELECT tur, has_grami, tl_tutari, aciklama FROM hareketler
 WHERE tenant_id=<kiracı> AND tur LIKE 'EMANET%';
→ EMANET_GIRIS | 9.160000 | 0.00 | Bilezik
```

tl_tutari = 0: emanet işletme malı değildir.

**Ekranda:**

- Emaneti olmayan müşteride "Emaneti yok", iade kilitli ve "Bu müşterinin emaneti yok."
- Emaneti olan müşteride "9,1600 has", iade açık, "Eski kayıtlar 1" kapalı.
- Açılınca satır, Ayrıntı ve Storno görünüyor.

**Temizlik:**

- API (dotnet run ve alt süreci Kuyumcu.Api) ve vekil durduruldu. `lsof` 5188 ve 5189 boş.
- Kiracı, `tenant_id` kolonu olan 70 tablodan ve `tenants`'tan silindi. Kalan satır 0; API durdurulduktan sonra yeniden sayıldı, yine 0.
- `tenants` sayısı başlangıçta 16, sonda 18. Fark diğer şeritlerin eşzamanlı test kiracılarından geliyor (PAKETOLCEK-1-TEST, PAKETKATMAN-1B-TEST, PAKETZO-BELGE-TEST vb.). EMANET-1 kiracısı 0. QA-TEST kiracılarına dokunulmadı.

## 9. Denetim ve açık kalanlar

**`dotnet/denetim.sh`:** 288 ✓, 1 ✗.

- ✗ `P-39: TABLO_SARMALAYICISIZ = 6`. Dosyalar Faturalar.razor ×2, Evraklar.razor, DenetimIzi.razor, IslemDefteri.razor, KasaRaporu.razor; hepsi CUMA-J şeridinin alanı. Emanet.razor'un tablosu `tablo-sar` içinde (yerel betikle doğrulandı).
- Emanet sayaçları ✓: `ModalPencere 48 ≤ 55`, `P-12 STORNO_GORUNMEYEN_EKRAN 0`, `P-17 ORTAK_BELGE_TURU 4`.
- İlk koşumda görülen `P-23 ODEME_KALEMSIZ_YOL 3`, `PAKETSATIS-2-TEST` kiracısının canlı satırlarıydı (SQL ile doğrulandı); ikinci koşumda yoktu.

**Tam süitteki kırmızılar (başkasına ait):**

- `DenetimIziEkranTestleri` ×7: sayfa artık `zo-belge` kökü taşıyor ve "KVKK md.12" metni yok. Sahibi CUMA-J / ZO-BELGE şeridi.
- `P39TasarimKokleriTestleri.K5_Her_tablo_KENDI_kaydirma_kabindadir`: yukarıdaki 6 tablo. Sahibi CUMA-J.

**Açık kalanlar:**

- Emanet için tuval ekranı yok. Tasarım hakemi isterse bir `Emanet.dc.html` tuvali üretilmeli.
- Riskte anılan "Grubun açık/kapalı durumu kaydedilemedi" bandı Emanet'e değil, menü tercih kanalına (KATMAN) ait; QA'ya ayrı bulgu olarak bildirilir.
- Seçicinin kendi arama kutusu sunucuda aramıyor. 50'den çok müşteride ikinci kutu gerekli kalıyor. Tek kutuda sunucu araması için `Secici`'ye bir "SorguDegisti" geri çağrısı gerekir; bu KATMAN şeridinin alanı.
- Menü satırında "Storno" sözcüğü (hub `Kabuk.Storno`) PRO'da kalıyor. BASIT'te K8 = 0 ölçüldü.

**Kurucu / mali müşavir notu:** Mevzuat hükmü eklenmedi ve değiştirilmedi. "Emanet altın işletmenin malı değildir; kâr-zarar ve stok değerlemesine girmez" cümlesi önceki paketlerden gelen mevcut bilginin büyük harfsiz hâli.

## 10. Değişen dosyalar

Kaynak: `git status --porcelain | grep -v -E "/(bin|obj)/"` ve bu paket.

| Dosya | Değişiklik |
|---|---|
| `dotnet/Kuyumcu.Web/Pages/Emanet.razor` | Değişti |
| `dotnet/Kuyumcu.Web/wwwroot/css/emanet.css` | Yeni |
| `dotnet/Kuyumcu.Web/wwwroot/index.html` | Tek `<link>` eklendi |
| `dotnet/Kuyumcu.Core/I18n/Sozlukler.EmanetSade.cs` | Yeni |
| `dotnet/Kuyumcu.Tests/EmanetSadeTestleri.cs` | Yeni |
| `dotnet/Kuyumcu.Tests/EmanetKurEkranTestleri.cs` | Uyarlandı |
| `dotnet/Kuyumcu.Tests/S35KurVitrinEmanetTestleri.cs` | Uyarlandı |
| `dotnet/Kuyumcu.Tests/UX4cEkranTestleri.cs` | Uyarlandı |
| `dotnet/Kuyumcu.Tests/P12EkranTestleri.cs` | Uyarlandı |
| `dotnet/Kuyumcu.Tests/UX1C2EkranTestleri.cs` | Uyarlandı |
| `dotnet/Kuyumcu.Tests/KilitGerekceTestleri.cs` | Uyarlandı |
| `dokumanlar/09-uygulama-kayitlari/ham/TUVAL-KARSILASTIRMA/EMANET-1-*.png` | 6 görüntü |

## 11. Koşul karşılama

| Koşul | Durum | Kanıt |
|---|---|---|
| Tek müşteri seçici, ayrı arama kutusu yok | KARŞILANDI (sınırda gerekçeli istisna) | E1, E7, M4 |
| İki büyük eşit eylem | KARŞILANDI | E6, M7; ekran görüntüsü |
| İade disabled + tek kısa sebep, "Eksik olan" yok | KARŞILANDI | E1, E3, M3 |
| 0 ise "Emaneti yok", 0,0000 yok | KARŞILANDI | E3, M2; canlı "Emaneti yok" |
| Has 4 hane yalnız sıfır değilse | KARŞILANDI | E4; canlı 9,1600 has |
| Eski kayıtlar katlanırı KAPALI | KARŞILANDI | E2, M1 |
| Tek sade cümle, büyük harf vurgu yok, (i) ipucu | KARŞILANDI | E5, E8, M8, M10 |
| İsteğe bağlı alanlar "+ ekle" arkasında | KARŞILANDI | E9, M5, M9 |
| Uçlar, gövdeler ve id'ler değişmedi | KARŞILANDI | 122 mevcut ve yeni Emanet testi yeşil |
| K1..K13 = 0, 12 kombinasyon | KARŞILANDI | tur3/tur5 açılış 0; 48 durum 0 |
| BASIT↔PRO fark 0 | KARŞILANDI | tutarlilik: "mod farki 0" |
| 1440 açılış ≤ 1 ekran | KARŞILANDI | İçerik alt kenarı 493–740 / 900 |
| Boş ve emanetli müşteri ekran görüntüleri | KARŞILANDI | § 7.4 |
| app.css ve hub'a dokunulmadı | KARŞILANDI | git status |
