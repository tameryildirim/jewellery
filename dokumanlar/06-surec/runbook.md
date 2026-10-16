# Runbook — İşletme ve Bakım Kılavuzu

| | |
|---|---|
| **Belge kimliği** | KYM-RB-001 |
| **Sürüm / Tarih** | 1.0 · 2026-09-13 |
| **Düzenleyen** | ByGatech A.Ş. |
| **Durum** | GÖZDEN GEÇİRİLDİ |

---

## 0. Bu belge neden var

Teknik due diligence kontrol listelerinde **runbook yokluğu** açık bir kırmızı bayraktır. Ayrıca bu projede **bus factor 1**'dir: sürüm kontrolünde tek katkı veren görünüyor.

Tasarım bilgisi 12 ADR'de ve kod içi gerekçe yorumlarında kayıtlıdır. **Eksik olan işletme bilgisiydi** — bu belge onu kapatır.

**Hedef:** Projeyi hiç görmemiş bir geliştirici bu belgeyle sistemi ayağa kaldırabilmeli, testleri koşturabilmeli ve bir olaya müdahale edebilmelidir.

---

## 1. Gereksinimler

| Araç | Sürüm | Doğrulama |
|---|---|---|
| .NET SDK | 8.0.x | `dotnet --version` |
| Docker | — | `docker ps` |
| MySQL | 8.4 (Docker kabı) | aşağıda |
| Python 3 | — | ölçüm betikleri için |
| Node / npx | — | diyagram çizimi için (opsiyonel) |

---

## 2. Sıfırdan kurulum

### 2.1 Veritabanı

```bash
docker ps --filter name=kuyumcu-mysql
# Yoksa:
docker run -d --name kuyumcu-mysql \
  -e MYSQL_ROOT_PASSWORD='<parola>' \
  -p 3306:3306 mysql:8.4
```

### 2.2 Sırlar — repoya YAZILMAZ

```bash
cd dotnet/Kuyumcu.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret"              "$(openssl rand -base64 48)"
dotnet user-secrets set "Sifreleme:Anahtar"       "$(openssl rand -base64 32)"
dotnet user-secrets set "ConnectionStrings:Kuyumcu" \
  "server=localhost;port=3306;database=kuyumcu_net;user=root;password=<parola>"
```

Şablon: `appsettings.Development.json.ornek`

> **Fail-closed:** Bu üç değerden biri eksikse uygulama **hiç açılmaz**. Bu bilinçlidir — anahtarsız çalışan bir sistem, MASAK kimlik verisini şifresiz yazardı.

### 2.3 Şema

```bash
cd dotnet
dotnet tool install --global dotnet-ef   # bir kez
dotnet ef database update --project Kuyumcu.Api
```

### 2.4 Çalıştırma

```bash
cd dotnet
ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project Kuyumcu.Api --no-launch-profile --urls http://localhost:5000
```

> `--urls` kullanın: `appsettings` içindeki `Urls` değeri ortam değişkenini ezebilir.

**Doğrulama:** `curl -s -o /dev/null -w '%{http_code}\n' http://localhost:5000/saglik` → `200`

---

## 3. Günlük geliştirme

```bash
cd dotnet
dotnet build Kuyumcu.sln
dotnet test  Kuyumcu.Tests/Kuyumcu.Tests.csproj     # ~5 sn
./denetim.sh                                         # mimari kuralları
```

**Testler `kuyumcu_test` şemasını kullanır.** `kuyumcu` veya `kuyumcu_net`'e bağlanmayı kod düzeyinde reddeder — kaza riski kapalıdır.

---

## 4. Ölçümler

```bash
# Kapsam
dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" \
  -targetdir:KapsamRaporu -reporttypes:"Html;TextSummary"

# Karmaşıklık
python3 -m lizard -l csharp Kuyumcu.Api Kuyumcu.Core Kuyumcu.Web \
  --exclude "*/bin/*" --exclude "*/obj/*" --exclude "*Migrations*"

# Zafiyet ve lisans
dotnet list package --vulnerable --include-transitive
nuget-license -i Kuyumcu.sln -t \
  -mapping ../dokumanlar/05-guvenlik/lisans-url-eslemesi.json \
  -a "MIT;Apache-2.0;BSD-3-Clause;MS-PL;BSD-2-Clause" -err

# SBOM
dotnet-CycloneDX Kuyumcu.sln -o ../dokumanlar/05-guvenlik/uretilen \
  -fn sbom-cyclonedx.json -F Json

# Referans bütünlüğü
./yetim-tara.sh

# Yedek + geri yükleme doğrulaması
./yedek-al-ve-dogrula.sh

# ER diyagramları (şemadan yeniden üret)
cd ../dokumanlar/diyagramlar && python3 er-uret.py && ./er-ciz.sh
```

⚠ `nuget-license` ve `dotnet-CycloneDX` için `DOTNET_ROOT` gerekebilir:
```bash
export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec   # Homebrew (macOS)
export PATH="$PATH:$HOME/.dotnet/tools"
```

---

## 5. Yedekleme ve geri yükleme

### Yedek al ve doğrula
```bash
cd dotnet && ./yedek-al-ve-dogrula.sh /yedek/dizini
```

Betik yedeği alır, **ayrı bir veritabanına** geri yükler ve her tablonun sağlama toplamını (`CHECKSUM TABLE`) kaynakla karşılaştırır. Uyuşmazlıkta **sıfırdan farklı çıkış kodu** döner.

**Kaynak veritabanına asla yazılmaz.**

### Gerçek geri yükleme (olay anı)
```bash
# 1. Uygulamayı durdur
# 2. ÖNCE mevcut durumun yedeğini al (bozuk bile olsa)
docker exec kuyumcu-mysql mysqldump -uroot -p<parola> \
  --single-transaction --databases kuyumcu_net > /yedek/olay-oncesi.sql
# 3. Geri yükle
docker exec -i kuyumcu-mysql mysql -uroot -p<parola> < /yedek/<yedek>.sql
# 4. Doğrula
docker exec kuyumcu-mysql mysql -uroot -p<parola> -e \
  "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA='kuyumcu_net';"
```

> ⚠ **MySQL 8.4'te `mysqlpump` KALDIRILDI** (resmî "Features Removed" listesi). `mysqldump` veya MySQL Shell `util.dumpInstance --checksum=true` kullanılır.

---

## 6. Olay müdahalesi

### 6.1 Kiracı sızıntısı şüphesi — **EN YÜKSEK ÖNCELİK**

```bash
cd dotnet && ./yetim-tara.sh
```

| Çıkış kodu | Anlamı | Yapılacak |
|---|---|---|
| 0 | Temiz | — |
| 1 | Yetim kayıt | Uygulama hatası; kök neden aranır |
| **2** | **KİRACI SIZINTISI** | **GÜVENLİK OLAYI** |

**Çıkış kodu 2 ise:**
1. **Satırları SİLME.** Kanıttır.
2. İlgili kayıtları ve zaman damgalarını dökümle.
3. Güvenlik sorumlusuna ve Kurucu'ya bildir.
4. `kuyumcu_test`'te de tara: temiz çıkıyorsa sorun **mevcut kodda değil, geçmiş veridedir**.
   ```bash
   ./yetim-tara.sh kuyumcu_test
   ```

> **Bilinen bulgu (2026-09-13):** `kuyumcu_net`'te 2 satır sızıntı var ve bunlar 9 Eylül'deki **kendi sızma testimizin** kalıntısıdır (`Kurban Kasa` 99.999 TL, `Kurban Musteri` 77.777 TL, aralarında 26 ms). `kuyumcu_test` 392 testten sonra temizdir.

### 6.2 Uygulama açılmıyor

| Belirti | Sebep | Çözüm |
|---|---|---|
| `Jwt:Secret tanımlı değil` | Sır yok | §2.2 |
| `Şifreleme anahtarı yok` | Anahtar yok | §2.2 — **fail-closed, kasıtlı** |
| `ConnectionStrings:Kuyumcu tanımlı değil` | Bağlantı yok | §2.2 |
| Port bağlanamıyor | Port dolu | `--urls` ile başka port |
| `IKiraciVarlik` hatası | Yeni varlık arayüzü uygulamamış | Varlığa `IKiraciVarlik` ekle — **fail-closed, kasıtlı** |

### 6.3 Testler toplu kırmızı

Tek tek değil **hepsi birden** kırmızıysa, neredeyse her zaman altyapı sorunudur:

| Sebep | Kontrol |
|---|---|
| MySQL kapalı | `docker ps` |
| Şema eski | `dotnet ef database update` |
| İki fikstür şemayı siliyor | `TestOrtami.SemayiBirKezKur` — bu bir kez **132 testi** düşürdü |

### 6.4 429 alınıyor (oran sınırı)

Beklenen davranış olabilir. Kontrol: giriş 5/dk (IP+e-posta), kayıt 3/saat, genel 100/dk.

> **Üretimde tüm kullanıcılar aynı kovaya düşüyorsa:** `UseForwardedHeaders` yapılandırmasını kontrol edin. Bu hata bir kez yaşandı — nginx `X-Forwarded-For` gönderiyordu ama uygulama okumuyordu, herkesin IP'si `127.0.0.1` görünüyordu.

### 6.5 Denetim izi yazılamıyor

`DenetimIziDegistirilemez` istisnası **doğru davranıştır**: bir kod yolu denetim kaydını güncellemeye veya silmeye çalışıyor. Kod düzeltilir, koruma kaldırılmaz.

---

## 7. Bilinen açıklar ve geçici çözümler

| Açık | Durum | Geçici çözüm |
|---|---|---|
| Sırlar sürüm kontrolünde (TB-01) | Açık | Yeni kurulumlarda **user-secrets** kullanın; mevcut sırlar **döndürülmeli** |
| `bin`/`obj` sürüm kontrolünde (TB-02) | Açık | `.gitignore` yeni eklemeleri engelliyor; mevcutları indeksten çıkarmak commit ister → Kurucu kararı |
| Veritabanında FK yok (TB-04) | Karara bağlandı | `yetim-tara.sh` düzenli çalıştırılır |
| CI/CD | `.github/workflows/ci.yml` yazıldı | Commit edilmesi Kurucu kararı |
| İzleme/uyarı yok | Açık | Sağlık ucu elle: `/saglik` |

---

## 8. Yayın öncesi kontrol listesi

- [ ] `dotnet test` → 0 başarısız
- [ ] `./denetim.sh` → TEMİZ
- [ ] `./yetim-tara.sh` → çıkış kodu 0
- [ ] `./yedek-al-ve-dogrula.sh` → çıkış kodu 0
- [ ] `dotnet list package --vulnerable` → 0
- [ ] Lisans kapısı → çıkış kodu 0
- [ ] **Dolu veritabanına göç denendi** (boş DB'ye migration yetmez)
- [ ] Sırlar ortam değişkenlerinde, dosyada değil
- [ ] `Cors:Origins` üretim değerleriyle
- [ ] `TersVekil:GuvenilirVekiller` topolojiye uygun
- [ ] HTTPS ve HSTS etkin
- [ ] **Mehmet'in yayın vetosu yazılı olarak kalktı**

> Son madde bugün **karşılanmamıştır**.

---

## 9. İletişim ve yetki

| Konu | Kime |
|---|---|
| Mimari karar | Bertan (02) |
| Kalite kapısı | Aşkın (04) |
| **Güvenlik olayı** | **Mehmet (06)** — veto yetkisi |
| Yayın | Batuhan (05) + Mehmet onayı |
| Sözleşme, para, hukuki metin, git geçmişi | **Kurucu** |
