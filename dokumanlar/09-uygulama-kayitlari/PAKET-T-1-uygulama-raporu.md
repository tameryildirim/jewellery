# PAKET T-1 (+F-6) — Yayın ve kurulum paketi, Faz A · UYGULAMA RAPORU

> Tarih: 2026-09-17 · Dalga 3 · Uygulayıcı: Taşkın (backend) + Ertuğ (frontend rolü)
> Durum: **BİTTİ (Faz A)** · Faz B (Ayarlar "Yayın durumu" kartı) kapsam dışı, T-4a sonrasına bağlı.

---

## 0. Özet

Temiz makinede SDK'sız, Development'sız, sabit parolasız, TLS'li kurulum yolu
kuruldu. Varsayılan/örnek sırla uygulama **açılmıyor** (canlıda kanıtlandı:
çıkış kodu 134, HTTP yanıt yok). Şema yükseltmesi tek yoldan (doğrulanmış yedek
+ migration bundle) yapılıyor; uygulama şemayı kendi değiştirmiyor. Üretim
compose'unda 3306 dışa kapalı (canlıda kanıtlandı). Güvenlik başlıkları tek
kaynaktan (Program.cs) ve tek kopya geliyor (curl kanıtı).

**Bu tur üç GERÇEK HATA yakalandı ve kapatıldı** (§ 3). Üçü de daha önceki
oturumun yazdığı ama hiç çalıştırılmamış koddaydı; ikisi üretimde uygulamanın
hiç açılmamasına, biri müşteri sunucusuna geliştirme sırlarının taşınmasına yol
açardı.

| Ölçü | Değer | Kaynak |
|---|---|---|
| Derleme | 0 hata | `dotnet build Kuyumcu.sln --nologo` |
| Uyarı | 0 (artımlı) / 4 (tam derleme, hepsi ÖNCEDEN VARDI — `MadenTestleri.cs` xUnit2008) | aynı |
| Test (önce) | 2280 | 2332 − 52 |
| Test (sonra) | **2332 geçti, 0 başarısız, 0 atlanan** | `dotnet test ... --logger trx` |
| Yeni test | **52** | filtreli koşum |
| denetim.sh | **DENETİM TEMİZ** | `dotnet/denetim.sh` |
| Migration | **YENİ MİGRATİON ÜRETİLMEDİ** (T-1 yeni varlık doğurmaz) | § 4 |
| Kiracı sayısı (önce/sonra) | 8 / 8 | `SELECT COUNT(*) FROM tenants` |

---

## 1. Yapılanlar

### 1.1 Fail-closed açılış kapısı
* `Kuyumcu.Api/Common/UretimAcilisDenetimi.cs` — 5 kural (JWT kara liste + asgari
  uzunluk, DB parolası kara listesi, KUYUMCU_VT tek kaynak, yedek anahtarı ≠ alan
  şifreleme anahtarı, TLS beyanı). `AlanSifreleme.Yukle` kalıbının ikizi:
  **Build() öncesi, tembel değil**. Hata mesajları **değer içermez**.
  *(Dosya bu turdan önce vardı; derlenmiyordu — § 3.1.)*
* `Program.cs` — çağrı TEK noktada (denetim.sh sayacı), `jwtAyarlar`'dan hemen sonra.
* Kara liste **TAM dizge** karşılaştırır. Geliştirme sırlarının DEĞERİ koda ikinci
  kez kopyalanmaz; SHA-256 parmak iziyle eşleştirilir (parmak izlerinin gerçek
  geliştirme sırlarıyla uyuştuğu bu turda doğrulandı).
* Ortam farkı **görünmez bırakılmadı**: `Uretim:DenetimiZorla=true` ile aynı
  kurallar her ortamda zorlanabilir; testler üretim davranışını bu anahtarla sınar.

### 1.2 Yayın ve kurulum
* `dotnet/yayin-al.sh` — self-contained publish (linux-x64 / win-x64) + migration
  bundle. `--no-build` hiçbir adımda yok. **Fail-closed**: `appsettings.Development.json`
  çıktıya sızarsa yayın `exit 4` ile durur.
* `dotnet/kurulum.sh` · `dotnet/kurulum.ps1` — kriptografik sır üretimi
  (`openssl rand` / `RandomNumberGenerator`), `chmod 600` / ACL kalıtımı kırık,
  **ekrana hiçbir değer basılmaz**, var olan dosya `--yenile` olmadan ezilmez.
* `deploy/systemd/kuyumcu.service` — `Type=notify`, `EnvironmentFile` **sonra**
  `Environment=ASPNETCORE_ENVIRONMENT=Production` (FAIL-CLOSED 6), journald +
  `SyslogIdentifier`, `RestartPreventExitStatus=3`, sıkılaştırma.
* `deploy/kuyumcu-service-kur.ps1` — `start.bat`ın yerini alır; `.env`i makine
  kapsamlı ortam değişkenine çevirir (değer basmadan), EventLog.
* `docker-compose.uretim.yml` — mysql'de `ports` YOK (yalnız `expose`), sırlar
  `${...:?}` ile zorunlu, cpu/bellek limiti, json-file 10m×5, `MYSQL_PWD` ile
  sağlık kontrolü.
* `deploy/nginx-kuyumcu.conf` / `deploy/iis-web.config` — 80→443 yönlendirme,
  TLS, **güvenlik başlığı kopyası YOK**, `/api/v1/sistem/*` için varsayılan
  `deny all` (K4), belge yükleme 8m, SignalR yükseltmesi.
* `deploy/ecosystem.config.js` · `start.bat` · `docker-compose.yml` — "üretim
  yayın yolu değildir" uyarı başlıkları (silme kurucuya bırakıldı).

### 1.3 Uçlar
| Fiil | Yol | Durum |
|---|---|---|
| GET | `/saglik` | DEĞİŞMEDİ — anonim, gövde `Healthy`, sürüm/şema/sır sızdırmaz |
| GET | `/saglik/hazir` | Anonim; 200 `{"hazir":true,"kod":"HAZIR"}` / 503 `SEMA_ESKI` / 503 `VERITABANI_YOK`. Migration ADI verilmez. |

### 1.4 Belgeler (tek kök: `docs/`)
* `docs/DEPLOY.md` — tümüyle yeniden yazıldı (Node yığını anlatımı gitti).
  § günlük (K14), § sır rotasyonu (MFA gizlerinin yeniden şifrelenmesi dahil),
  § kabul kontrol listesi.
* `docs/RUNBOOK-SEMA-YUKSELTME.md` — YENİ; "önce yedek" zorunlu, sayısal iddia yok.
* `docs/RUNBOOK-MFA-KURTARMA.md` — YENİ; 45-API devir koşulu (1).
* `docs/RUNBOOK-PILOT-DAVET.md` — YENİ; K6.

### 1.5 denetim.sh
* Yeni sayaçlar: T1-1 (denetçi tam 1 çağrı, yalnız Program.cs), T1-2 (`DEGISTIR`
  yalnız denetçi dosyasında), T1-3 (otomatik `Migrate()` yok), T1-4 (üretim
  şablonu sırsız), T1-5 (üretim compose 3306 kapalı).
* 9. sayaç (K12) MÜHÜR 6'ya göre **tersine çevrildi**: artık nginx/IIS'te
  güvenlik başlığı kopyası **bulunmamalı** (§ 2.2).

---

## 2. Sapmalar ve gerekçeleri

### 2.1 Migration sayısı iddiası yazılmadı
Spesifikasyon "31 migration sabit" diyordu, `CLAUDE.md` "22". Ölçülen değer
**40**'tır (`kuyumcu_net.__EFMigrationsHistory`). Mühür 3'ün sayısal iddia
yasağı gereği hiçbir belgeye/teste sabit sayı yazılmadı; testler beklenen sayıyı
derlemedeki migration listesinden **türetir**.

### 2.2 Güvenlik başlıkları: nginx'ten TÜMÜ kaldırıldı (K5'ten bir adım ileri)
MÜHÜR 6/K5 yalnız `X-Frame-Options` ve `X-Content-Type-Options` satırlarının
kaldırılmasını yazıyordu. Ancak aynı mührün kanıt şartı "her başlığın **TEK
kopya** gelmesi" ve K7 "statik sunum tek yol"dur. CSP de Program.cs'ten geldiği
için nginx'te bırakılması ikinci kopya üretirdi. Bu yüzden **CSP dahil tüm
güvenlik başlıkları vekilden kaldırıldı**; nginx yalnız ters vekil + TLS.
Bunun zorunlu sonucu: `TasarimBelirtecTestleri.Nginx_kabugu_API_ile_AYNI_CSP...`
testi ve denetim.sh 9. sayacı ters yönde yeniden yazıldı (ikisi de eski K12
sözleşmesini ölçüyordu).

### 2.3 `deploy/kuyumcu.service` değil `deploy/systemd/kuyumcu.service`
Bertan itirazı + MÜHÜR 3: systemd birimleri tek dizinde (T-3 timer'ları da orada).

### 2.4 MFA runbook'unda gerçek şema adları kullanıldı
45-API devir metni "DB'de `mfa_aktif=0` yapma yordamı" diyordu. Kodda böyle bir
kolon **yok**: tablo `kullanici_mfa`, kolonlar `durum` (`BEKLIYOR`|`ETKIN`) ve
`gizli_sifreli`. Runbook gerçek şemaya göre yazıldı (tahmin edilmedi).

### 2.5 Pilot davet runbook'unda `curl` örneği YAZILMADI
`/api/v1/sistem/*` uçları kaynak ağacında **yoktur** (0 eşleşme); F-2-min
teslimidir. Var olmayan bir uca örnek yazmak uydurma olurdu (Anayasa md. 1).
T-1'in kurduğu şey nginx tarafındaki varsayılan-reddet kapısıdır; runbook'un
davet üretimi bölümü F-2-min'de doldurulacak şekilde açıkça işaretlendi.

### 2.6 `appsettings.Production.json.ornek` birebir kopya olarak tutuldu
Şablonun kendisi zaten sırsız olduğu için `.ornek` saf tekrardır. Spesifikasyon
istediği için tutuldu, ama ayrışma riski **test edilen bir değişmeze** çevrildi
(`ProductionSablonuVeOrnegiBirebirAynidir`).

### 2.7 `TasarimZamaniFabrika.cs` yedek bağlantı dizgesinden `password=DEGISTIR` çıkarıldı
denetim.sh T1-2 sayacı yakaladı: kara listeye alınan şablon sırrın kod içindeki
ikinci kopyasıydı. Tasarım zamanında veritabanına bağlanılmadığı için parolasız
dizge yeterlidir; gerçek bağlantı `KUYUMCU_BAGLANTI` ortam değişkeninden gelir.

### 2.8 Süre
Mühür 4'teki "+0,25–0,5 gün" büyüme tahmini **doğrulanamadı**: bu paket tek bir
oturumda (yaklaşık yarım gün ajan çalışması) uygulandı; insan-gün karşılığı
ölçülmedi. Tahmini onaylamak da reddetmek de veri gerektirir — yazılmadı.

---

## 3. Bu turda yakalanan GERÇEK HATALAR (Anayasa md. 1 — gizlenmedi)

### 3.1 Derleme KIRMIZI devralındı
İş başlarken `dotnet build` **2 hata** veriyordu: `Program.cs`'te
`builder.Host.UseWindowsService()` / `UseSystemd()` çağrılıyordu ama
`Microsoft.Extensions.Hosting.WindowsServices` ve `.Systemd` paket başvuruları
csproj'da **yoktu**. İkisi eklendi (8.0.1).

```
Program.cs(22,14): error CS1061: 'ConfigureHostBuilder' bir 'UseWindowsService' tanımı içermiyor
Program.cs(23,1):  error CS1929: 'ConfigureHostBuilder' bir 'UseSystemd' tanımı içermiyor
```

### 3.2 `/saglik/hazir` her çağrıda 500 dönüyordu
Uç anonimdi ama `KuyumcuDbContext`'i **doğrudan enjekte ediyordu**; istek
bağlamında kiracı olmadığı için DI çözümlemesi patlıyor ve gövde yazılmadan 500
dönüyordu. Var olan doğru mekanizma (`SistemKapsami`) kullanılacak şekilde
düzeltildi. Testle kapatıldı (`HazirUcuSemaGuncelken200Doner`).

### 3.3 Üretim profilinde uygulama HİÇ AÇILMIYORDU
Açılıştaki şema hazırlık kapısı `app.Services.CreateScope()` ile DbContext
çözüyordu → **her Production başlangıcı** şu hatayla çöküyordu:

```
Unhandled exception. System.InvalidOperationException: Kiracı bağlamı çözülemedi:
KuyumcuDbContext kiracı ya da sistem bağlamı olmadan kullanılamaz.
   at Program.<Main>$(String[] args) in .../Program.cs:line 478
```

Gerçek üretim provasında yakalandı (hiçbir test Production ortamını
çalıştırmıyor). `SistemKapsami` ile düzeltildi + regresyon sayacı eklendi
(`UretimAcilisKapisiSistemKapsamiKullanir`).

### 3.4 Geliştirme sırları yayın çıktısına kopyalanıyordu
İlk gerçek publish provasında `appsettings.Development.json` self-contained
çıktıya girdi — geliştirme MySQL parolası ve JWT sırrı müşteri sunucusuna
taşınırdı. csproj'a `CopyToPublishDirectory="Never"` eklendi, betiğe fail-closed
kontrol (`exit 4`) kondu, ikisi de testle bağlandı.

### 3.5 (Yan etki, kayda geçiriliyor) MySQL parolası çıkarma reçetesi bozuldu
`docker-compose.yml`e eklenen uyarı başlığı, görev metnindeki
`grep MYSQL_ROOT_PASSWORD ... | head -1 | awk '{print $2}'` reçetesinin artık
yorum satırını yakalamasına yol açıyor. Bu turda parola
`appsettings.Development.json`'dan okundu. Sonraki oturumlar için reçete
güncellenmelidir (kurucuya not, § 9).

---

## 4. Migration

**Gerekmedi.** T-1 yeni varlık/tablo doğurmaz; `IKiraciVarlik` yüzeyi
değişmedi, `dotnet ef migrations add` çalıştırılmadı.
`__EFMigrationsHistory` yalnız **okunur** (hazırlık kapısı). Kanıt:

```
kuyumcu_net: SELECT COUNT(*) FROM __EFMigrationsHistory → 40   (önce = sonra)
```

Prova şemasına (`kuyumcu_prova`) mevcut migration'lar uygulandı ve prova
sonunda şema **düşürüldü** (§ 7).

---

## 5. Test koşumları

| Kapsam | Toplam | Başarılı | Başarısız | Atlanan |
|---|---|---|---|---|
| T-1 dört yeni dosya (filtreli) | 52 | 52 | 0 | 0 |
| Tam süit (`--logger trx`) | **2332** | **2332** | **0** | 0 |

Yeni dosyalar: `UretimAcilisDenetimiTestleri.cs` (15 · saf),
`YayinDosyaSozlesmesiTestleri.cs` (22 · saf),
`MigrationHazirlikTestleri.cs` (5 · DB),
`YayinAcilisHttpTestleri.cs` (10 · HTTP).

İzolasyon: HTTP testleri `[CollectionDefinition("YayinAcilis",
DisableParallelization = true)]` koleksiyonunda koşar; süreç ortam değişkenleri
`try/finally` ile geri alınır (Aşkın koşulu, M-T1-05).

**Kapsam dışı bırakılan test dosyası:** `AyarlarYayinKartiTestleri.cs` (bUnit) —
Ayarlar "Yayın durumu" kartı **Faz B**'dir (T-4a ve F-7 sonrası). Faz A'da
Blazor sayfası açılmadı, bu yüzden bUnit bacağı yok.

---

## 6. Mutasyon kanıtları

Yöntem: kaynak scratchpad'e kopyalandı → `perl -0777 -pi -e` ile tek eşleşmeli
değişiklik → filtreli koşum → yedek geri kopyalandı → `cmp` birebir aynı.
**Her mutasyondan sonra `cmp` temiz döndü.**

| Kod | Değişiklik | Kırmızıya dönen test |
|---|---|---|
| M-T1-01b | JWT kara liste karşılaştırması kaldırıldı (`if (false)`) | `VarsayilanJwtSirriUretimdeAcilisiDurdurur`, `GelistirmeJwtSirriUretimdeAcilisiDurdurur`, `DenetimiZorlaAnahtariGelistirmedeDeAyniKuraliUygular`, `HataMesajiSirDegeriniIcermez`, `YayinAcilisHttpTestleri.VarsayilanSirliFabrikaAcilistaInvalidOperationExceptionAtar` |
| M-T1-01 (varyant) | Yalnız SHA-256 parmak izi listesi devre dışı | `GelistirmeJwtSirriUretimdeAcilisiDurdurur`, `GelistirmeParolasiUretimdeAcilisiDurdurur`, `HataMesajiSirDegeriniIcermez` |
| M-T1-02 | `AsgariSirBayt` 32 → 1 | `KisaJwtSirriUretimdeAcilisiDurdurur`, `HataMesajiSirDegeriniIcermez` |
| M-T1-03 | Parola kara listesi kaldırıldı | `DegistirParolasiUretimdeAcilisiDurdurur`, `GelistirmeParolasiUretimdeAcilisiDurdurur`, `HataMesajiSirDegeriniIcermez`, `GelistirmeParolasiylaUretimProfiliAcilmaz` |
| M-T1-04 | TLS/ters vekil beyanı kontrolü kaldırıldı | `TlsBeyaniYokkenUretimdeAcilisDurur`, `TlsBeyaniOlmadanUretimProfiliAcilmaz` |
| M-T1-05 (varyant) | `Dogrula` Build() SONRASINA taşındı | **KIRMIZI DÖNMEDİ** — WebApplicationFactory'de davranış eşdeğer; gerçek risk tembel DI fabrikasıdır (aşağıdaki 05b). Dürüstçe kaydedildi. |
| M-T1-05b | `Dogrula` **tembel DI fabrikasına** taşındı (AlanSifreleme'deki canlı hatanın aynısı) | `VarsayilanSirliFabrikaAcilistaInvalidOperationExceptionAtar`, `GelistirmeParolasiylaUretimProfiliAcilmaz`, `TlsBeyaniOlmadanUretimProfiliAcilmaz` |
| M-T1-06 | `/saglik/hazir` bekleyen migration kontrolü `false`landı | `HazirUcuBekleyenMigrationVarken503Doner` |
| M-T1-07 | `Program.cs`e `Database.Migrate()` eklendi | `UygulamaSemayiKendiDegistirmez_MigrationsHistorySatirSayisiDegismez` |
| M-T1-07b | Aynı mutasyon, denetim kapısı | `denetim.sh` çıkış 1 — `✗ T-1: otomatik Migrate() AÇILMIŞ 1 (beklenen 0)` |
| M-T1-08 | `appsettings.Production.json`a `Jwt:Secret` eklendi | `ProductionSablonuHicbirSirAnahtariIcermez`, `ProductionSablonuVeOrnegiBirebirAynidir` |
| M-T1-09 | `docker-compose.uretim.yml`ye `ports 3306:3306` eklendi | `ComposeUretimDosyasi3306PortuYayimlamaz` |
| M-T1-10 | Denetçi hata mesajına sır değeri gömüldü | `HataMesajiSirDegeriniIcermez` |
| M-T1-11b | İkinci bir `Dogrula` çağrısı eklendi | `denetim.sh` çıkış 1 — `✗ T-1: açılış denetçisi çağrı sayısı 2 çağrı / 2 Program.cs` |
| M-T1-12 | `/saglik` gövdesine sürüm + bağlantı dizgesi eklendi | `SaglikUcuSirVeSurumSizdirmaz` |

**Test açığı kapatıldı:** M-T1-06 ilk denemede hiçbir testi kırmıyordu (ölçüm
yalnız DB katmanındaydı). Bunun üzerine gerçek bir "eski şema" kuran
(`n-1` migration'a kadar göç ettirilmiş ayrı veritabanı) HTTP testi yazıldı;
mutasyon ondan sonra kırmızıya döndü. M-T1-05 için de zayıf varyantın yerine
gerçek riski (tembellik) hedefleyen 05b eklendi.

Son mutasyondan sonra: `dotnet build` **0 hata**, `denetim.sh` **TEMİZ**.

---

## 7. Canlı doğrulama (port 5198 — 5199'a DOKUNULMADI)

| # | Adım | Beklenen | Gerçek | Kanıt |
|---|---|---|---|---|
| 1 | Development'ta `/saglik` | 200 `Healthy` | **200 `Healthy`** | `curl -i` |
| 2 | Development'ta `/saglik/hazir` | 200 `hazir:true` | **200 `{"hazir":true,"kod":"HAZIR"}`** | `curl -i` |
| 3 | FAIL-CLOSED 1: `Jwt__Secret=DEGISTIR-...` (Production) | süreç açılmaz, HTTP yok | **çıkış kodu 134**, `curl exit 7` (bağlantı reddi) | `InvalidOperationException: Jwt:Secret varsayılan/örnek değerde.` |
| 4 | 3'ün log'unda sır değeri | 0 | **0** (`grep -c` = 0) | aynı log |
| 5 | FAIL-CLOSED 2a: parola `DEGISTIR` | süreç açılmaz | **çıkış 134** | `Veritabanı parolası varsayılan/örnek değerde.` |
| 6 | FAIL-CLOSED 2b: GELİŞTİRME parolası | süreç açılmaz, log'da parola yok | **çıkış 134**, `grep -c` = 0 | aynı |
| 7 | FAIL-CLOSED 4: TLS beyanı yok | süreç açılmaz | **çıkış 134** | `TLS beyan edilmedi...` |
| 8 | Meşru üretim profili (kuruluma özgü sır + ters vekil beyanı) | açılır | **5198 dinliyor**, `/saglik` 200, `/saglik/hazir` 200 | `lsof` + `curl` |
| 9 | Bekleyen migration (prova şemasından son satır silindi) | `/saglik` 200, `/saglik/hazir` 503 | **200 `Healthy`** / **503 `{"hazir":false,"kod":"SEMA_ESKI"}`** | `curl -w '%{http_code}'` |
| 10 | Satır geri konunca | 200 | **200 `{"hazir":true,"kod":"HAZIR"}`** | aynı |
| 11 | HSTS tek kopya (`Host: ornek.com`, `X-Forwarded-Proto: https`) | 1 kopya, preload yok | **`Strict-Transport-Security: max-age=2592000`, kopya sayısı 1** | `curl -I \| grep -c` |
| 12 | Statik varlıkta güvenlik başlıkları (`/_framework/blazor.webassembly.js`) | 3 başlık | **3** (HSTS + X-Frame-Options + CSP) | `curl -I \| grep -c` |
| 13 | Gerçek publish (`yayin-al.sh linux-x64`) | self-contained + bundle | **çıkış 0**, `yayin/linux-x64/` üretildi, `kuyumcu-migrate` **136 MB** | betik çıktısı |
| 14 | Publish çıktısında geliştirme ayarı | olmamalı | ilk provada **VARDI → düzeltildi**; ikinci provada yalnız `appsettings.json` + `appsettings.Production.json` | `ls` |
| 15 | `kurulum.sh` — sır üretimi | ekrana değer basmaz, izin 600 | **8 anahtar "var"**, izin **600**, sızan değer **0** | çıktı + programatik karşılaştırma |
| 16 | `kurulum.sh` ikinci çağrı | ezmez | **çıkış 1, "Sırlar SESSİZCE EZİLMEZ"** | çıktı |
| 17 | `docker-compose.uretim.yml` mysql ayakta | 3306 ana makineye açık DEĞİL | **`PORTLAR:[3306/tcp, 33060/tcp]`** — host eşlemesi yok | `docker ps` |
| 18 | Aynı anda iç ağdan bağlantı | başarılı | **`kuyumcu_prova2` listelendi** | `compose exec` + `MYSQL_PWD` |
| 19 | Günlük rotasyonu (çalışan konteyner) | json-file 10m×5 | **`json-file map[max-file:5 max-size:10m]`** | `docker inspect` |
| 20 | Yedek kapısı `yedek-al-ve-dogrula.sh` | çıkış 0 | **çıkış 0 — tablo 50 · uyuşan 50 · uyuşmayan 0** | betik çıktısı |
| 21 | Temizlik | prova izleri yok | `kuyumcu_prova`, `kuyumcu_prova` kullanıcısı, `kuyumcu_t1_eski`, `kuyumcu_t1_hazir` **düşürüldü**; kalan: `kuyumcu`, `kuyumcu_net`, `kuyumcu_test`; compose yığını `down -v` | SQL + `docker ps -a` |
| 22 | Kiracı sayısı | 8 → 8 | **8** (T-1 kiracı açmaz; `PAKET T-1-TEST` kiracısı oluşturulmadı) | `SELECT COUNT(*) FROM tenants` |
| 23 | 5198 serbest | 0 dinleyen | **0** | `lsof -nP -iTCP:5198 -sTCP:LISTEN` |

### Yapılamayan canlı adımlar (dürüstçe)
* **`dotnet --list-sdks` BOŞ makinede çalıştırma:** bu makinede SDK kurulu; temiz
  bir hedef makine yok. Self-contained çıktının SDK'sız çalıştığı **kanıtlanmadı**,
  yalnız self-contained üretildiği kanıtlandı.
* **systemd / Windows Service gerçek kurulumu:** macOS'ta systemd yok, Windows
  makinesi yok. Birim dosyaları metin sözleşmesi olarak test edildi; `systemctl
  start` / `sc.exe create` **çalıştırılmadı**.
* **`nginx -t`:** bu makinede nginx kurulu değil. Conf metin sözleşmesiyle
  test edildi; nginx sözdizimi **doğrulanmadı**.
* **Gerçek TLS sertifikasıyla 301/200 zinciri:** sertifika edinimi F-7'dedir.
* **win-x64 publish:** yalnız linux-x64 üretildi (süre).
* **Uçtan uca duman testi (giriş + satış fişi POST + SQL sayım):** T-1 yeni iş
  kuralı getirmediği ve prova şeması boş kurulduğu için yapılmadı.

---

## 8. Koşul karşılama tablosu

| Koşul | Durum | Kanıt |
|---|---|---|
| Migration sayısı sabit yazılmaz | ✔ | § 2.1; test beklentiyi `GetMigrations()`tan türetir |
| Denetçi Build() öncesi, tembel değil | ✔ | `Program.cs:38`; M-T1-05b kırmızı |
| `/saglik/hazir` MapHealthChecks yanında | ✔ | `Program.cs:400` civarı |
| Kara liste TAM dizge, yanlış pozitif yok | ✔ | `KaraListeTamDizgeKarsilastirir_MesruSirKilitlenmez` |
| Development/Testing'te kara liste uygulanmaz | ✔ | `GelistirmeOrtamindaDevBaglantisiKabulEdilir`; 2332 test yeşil |
| Ortam farkı görünmez bırakılmaz | ✔ | `Uretim:DenetimiZorla`; `DenetimiZorlaAnahtariGelistirmedeDeAyniKuraliUygular` |
| Üretim compose: düz parola yok, 3306 kapalı | ✔ | canlı § 7/17-18; M-T1-09 |
| nginx: 80→443 + HSTS (tek kopya) | ✔ | canlı § 7/11; `NginxConf80PortundaYalnizYonlendirmeYapar` |
| systemd birimleri tek dizinde (`deploy/systemd/`) | ✔ | § 2.3 |
| Runbook'lar tek kökte (`docs/`) | ✔ | `RunbooklarTekBelgeKokundeYasar` |
| Sır dosyası `chmod 600` + sahiplik | ✔ | canlı § 7/15 |
| Kurulum betiği sır sızdırmıyor (otomatik adım) | ✔ | canlı § 7/15 + `KurulumBetigiUretilenSirriEkranaBasmaz` |
| xUnit collection izolasyonu (M-T1-05) | ✔ | `YayinAcilisKoleksiyonu` |
| "Bekleyen migration" yalnız prova şemasında | ✔ | `kuyumcu_prova` / `kuyumcu_t1_*`; geliştirme geçmişine dokunulmadı |
| Mehmet kapısı: 45-API bitmeden gerçek kurulum yok | ✔ (kod yazımı muaf) | `docs/DEPLOY.md` § 10 son madde |
| MÜHÜR 6 K5/K6/K7 — başlık tek kaynak, tek kopya | ✔ | canlı § 7/11-12; denetim 9. sayaç |
| K14 — kalıcı günlük asgarisi, Program.cs'e kod eklenmedi | ✔ | systemd/compose/appsettings; günlükle ilgili tek satır C# yazılmadı |
| K4 — `/api/v1/sistem/*` IP kısıtı | ✔ | `NginxConfSistemUclariniAgKatmanindaKisitlar` |
| K6 — pilot davet runbook'u `docs/` altında | ✔ (kısmi) | § 2.5 — davet `curl`ü F-2-min'e bırakıldı |
| 45-API devir (1) — SAHİP kilitlenme runbook'u | ✔ | `docs/RUNBOOK-MFA-KURTARMA.md` |
| 45-API devir (2) — rotasyonda MFA gizlerinin yeniden şifrelenmesi | ✔ | `docs/DEPLOY.md` § 9 |
| 45-API devir (3) — doğrulayıcı uygulama satırı | ✔ (boş, işaretli) | runbook'ta "45-UI'de doldurulacak" |
| Tek yayın yolu; ikiz yol doğmadı | ✔ | uyarı başlıkları + `EskiYayinYollariUretimYoluDegildirUyarisiTasir` |
| Yeni varlık yok → DoD PATCH/DELETE maddesi tetiklenmedi | ✔ | § 9 DoD sözleşmesi |
| Tek dokunuş (razor) | ✔ | **hiçbir `.razor` dosyasına dokunulmadı** |
| COMMIT YOK | ✔ | `git status` kirli, commit yapılmadı |

---

## 9. DoD sözleşmesi (Bertan koşulu)

**T-1 Faz A hiçbir yeni varlık, tablo, kayıt türü ya da veri girişi doğurmaz.**
Bu yüzden "her yeni varlık PATCH + DELETE/pasife alma + yetki + denetim kaydı +
ekranda Düzenle" maddesi **tetiklenmez**.

| Üretilen şey | Düzenlenebilir mi? | Nasıl / neden değil |
|---|---|---|
| `/etc/kuyumcu/.env` (sırlar) | Evet — `kurulum.sh --yenile` | Sessiz ezme YASAK; eski dosya zaman damgalı kopyalanır. Rotasyonun veri kaybı riski `docs/DEPLOY.md` § 9'da anlatılır. |
| systemd birimi / IIS web.config / nginx conf | Evet — dosyayı düzenle + servisi yeniden yükle | Yapılandırma dosyasıdır, veritabanı kaydı değildir. |
| `__EFMigrationsHistory` | **Salt okunur** (uygulama için) | FAIL-CLOSED 5: yazma yalnız migration bundle üzerinden; uygulama şemayı kendi değiştirmez (`denetim.sh` T1-3). |
| `/saglik`, `/saglik/hazir` yanıtları | — | Salt okunur yoklama uçları; yazma yok, denetim kaydı gerekmez. |
| Yayın çıktısı (`dotnet/yayin/`) | Evet — yeniden `yayin-al.sh` | Derleme ürünü; `.gitignore`da. |

**Ekran/düğme maddesi:** Faz A'da tek bir Blazor sayfası açılmadı, tek bir düğme
eklenmedi. "Her düğme istek atar ya da disabled + gerekçe" maddesi boş geçer.
UX-S21…S33 sözleşmesi de veri girişi olmadığı için tetiklenmez.

**Finansal varlıkta hard DELETE:** T-1 hiçbir finansal tabloya dokunmaz.
Prova sırasında silinen tek satır, **prova şemasındaki** (`kuyumcu_prova`)
`__EFMigrationsHistory` kaydıydı ve aynı adımda geri konuldu; üretim/geliştirme
verisine dokunulmadı.

---

## 10. Değişen dosyalar

`git status --porcelain | grep -v -E "/(bin|obj)/"` çıktısından T-1'e ait olanlar:

**Değiştirilen (M):**
```
.gitignore
deploy/ecosystem.config.js
deploy/iis-web.config
deploy/nginx-kuyumcu.conf
docker-compose.yml
docs/DEPLOY.md
dotnet/Kuyumcu.Api/Kuyumcu.Api.csproj
dotnet/Kuyumcu.Api/Program.cs
dotnet/Kuyumcu.Api/Data/TasarimZamaniFabrika.cs
dotnet/denetim.sh
dotnet/Kuyumcu.Tests/TasarimBelirtecTestleri.cs
start.bat
```

**Yeni (??):**
```
deploy/.env.ornek
deploy/systemd/kuyumcu.service
deploy/kuyumcu-service-kur.ps1
docker-compose.uretim.yml
docs/RUNBOOK-SEMA-YUKSELTME.md
docs/RUNBOOK-MFA-KURTARMA.md
docs/RUNBOOK-PILOT-DAVET.md
dotnet/yayin-al.sh
dotnet/kurulum.sh
dotnet/kurulum.ps1
dotnet/Kuyumcu.Api/appsettings.Production.json
dotnet/Kuyumcu.Api/appsettings.Production.json.ornek
dotnet/Kuyumcu.Tests/UretimAcilisDenetimiTestleri.cs
dotnet/Kuyumcu.Tests/YayinDosyaSozlesmesiTestleri.cs
dotnet/Kuyumcu.Tests/MigrationHazirlikTestleri.cs
dotnet/Kuyumcu.Tests/YayinAcilisHttpTestleri.cs
dokumanlar/09-uygulama-kayitlari/PAKET-T-1-uygulama-raporu.md
dotnet/yayin/            (derleme ürünü, .gitignore'da)
```

`dotnet/Kuyumcu.Api/Common/UretimAcilisDenetimi.cs` önceki oturumdan kalmıştı;
bu turda derlenir hale getirildi ve testlendi. Diğer `M` işaretli dosyalar
(modüller, razor sayfaları, `Sozlukler.cs` …) **bu paketten önce de kirliydi** —
T-1 onlara dokunmadı.

---

## 11. Açık kalanlar

1. **Temiz makine provası yapılmadı** — SDK'sız hedefte çalışma kanıtı yok
   (§ 7 "yapılamayan"). Faz A kabulü bu kanıt olmadan **yarım sayılmalıdır**;
   gerçek müşteri kurulumunun kanıtı değildir.
2. **`nginx -t` doğrulanmadı** (nginx kurulu değil). Sözdizimi riski açık.
3. **systemd / Windows Service birimleri çalıştırılmadı.**
4. **win-x64 publish üretilmedi.**
5. **Faz B** (Ayarlar "Yayın durumu" kartı, etkileşimli sihirbaz, tek arşiv,
   kurulum kaydı) yapılmadı — T-4a/F-7 sonrasına bağlı.
6. **F-7** (Let's Encrypt otomasyonu) kapsam dışı; şablon `/etc/letsencrypt/...`
   yolunu varsayar ve ACME webroot bloğu hazırdır.
7. **`ecosystem.config.js` ve `start.bat` silinmedi** — uyarı başlığı eklendi;
   silme kurucunun kararı.
8. **MFA doğrulayıcı uygulama satırı boş** — 45-UI (dalga 2c) dolduracak.
9. **Pilot davet `curl` örneği yok** — F-2-min teslimine bağlı.
10. **`appsettings.json` ve `appsettings.Development.json` hâlâ örnek sır taşıyor.**
    Bu **kasıtlıdır** (geliştirme akışı bozulmasın) ve fail-closed kapısının
    kara listesi tam olarak bu değerleri hedefler. Yayın çıktısına Development
    dosyası artık girmiyor; ana `appsettings.json` giriyor ama içindeki
    `DEGISTIR` değerleriyle uygulama **açılmıyor**.

---

## 12. Kurucuya / mali müşavire notlar

1. **Mevzuat — hiçbir hüküm koda sabitlenmedi.** T-1'in mevzuat dayanakları
   (KVKK m.12, KVKK Veri Güvenliği Rehberi, VUK 219/A) spesifikasyonda
   "DOĞRULANMALI/yorum" işaretliydi. Bu paket hiçbirini rakama çevirmedi:
   **günlük saklama süresi bir gün sayısı olarak yazılmadı**, yalnız journald /
   docker / EventLog tarafında *yapılandırılabilir* kılındı. Saklama süresinin
   VUK/KVKK dayanağı **T-3'te** belirlenecek. Mali müşavir/hukukçu görüşü
   alınmadı.
2. **Silme kararı sizde:** `deploy/ecosystem.config.js` ve `start.bat` üretim
   yolu olmaktan çıkarıldı ama silinmedi (izinsiz silme yasağı + hook kısıtı).
   Silinmeleri "tek yayın yolu" mührünü görsel olarak da tamamlar.
3. **Gerçek kurulum provası için bir hedef makine gerekiyor** (temiz Ubuntu
   22.04 VPS ya da Windows Server VDS). Faz A'nın kalan tek kanıtı budur.
4. **Alan adı + TLS sertifikası** kurucuya çıkan kalemler arasında; F-7 bunları
   otomatikleştirecek ama ilk sertifika ve alan adı kararı sizde.
5. **45-API (MFA) kapısı yürürlükte:** kod hazır, ama gerçek müşteri kurulumu
   MFA tamamlanmadan ve `docs/RUNBOOK-MFA-KURTARMA.md` okunmadan yapılmayacak
   (Mehmet mührü). Tek SAHİP kullanıcının telefonunu kaybetmesi gerçek bir
   kilitlenme riskidir; müşteriye **kurtarma kodlarını yazdırma** uyarısı
   kurulumda sözlü olarak da verilmelidir.
6. **Ortam tuzağı güncellemesi (§ 3.5):** `docker-compose.yml`e uyarı başlığı
   eklendiği için MySQL parolasını `grep MYSQL_ROOT_PASSWORD | head -1 | awk
   '{print $2}'` ile çıkarma reçetesi artık yorum satırını yakalıyor. Sonraki
   oturum talimatlarında reçete `grep -m1 'MYSQL_ROOT_PASSWORD:' ` biçimine
   çekilmeli ya da parola `appsettings.Development.json`'dan okunmalı.
