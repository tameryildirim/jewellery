# Kuyumcu ERP — Üretim kurulumu (DEPLOY)

> Paket T-1 (+F-6), Faz A. **Tek yayın yolu** (MÜHÜR 3): başka bir yordam
> yoktur. `deploy/ecosystem.config.js` (PM2/Node artığı) ve kökteki eski
> Windows başlatıcısı **üretim yolu değildir**; başlıklarında uyarı taşırlar.
>
> ⚠ Bu belgenin eski sürümü, artık kaldırılmış olan Node tabanlı yığını
> anlatıyordu (paket yöneticisi komutlarıyla birlikte).
> Bugünkü yığın: **.NET 8 + Blazor WebAssembly, tek süreç** (WASM istemcisi de
> API sürecinden sunulur — MÜHÜR 6 / K7).

## 0. Mimari özeti

```
  İnternet ──TLS──> nginx / IIS  ──http──>  Kuyumcu.Api (Kestrel, 127.0.0.1:5000)
                    (ters vekil)                 ├── /api/v1/**   REST
                    yalnız vekil                 ├── /hubs/kur    SignalR
                    statik SUNMAZ                └── /  + /_framework  Blazor WASM
                                              MySQL 8.4  (yalnız iç ağ, 3306 dışa KAPALI)
```

**Güvenlik başlıklarının tek kaynağı `Program.cs`'tir** (CSP, X-Frame-Options,
X-Content-Type-Options, HSTS). Vekil şablonlarında `add_header` **yoktur**;
eklenirse başlık çift kopya gelir, iki katman ayrışır ve `dotnet/denetim.sh`
kırmızı döner.

## 1. Gereksinimler

| | Linux (VPS) | Windows (VDS) |
|---|---|---|
| İşletim sistemi | Ubuntu 22.04+ / Debian 12+ | Windows Server 2019+ |
| .NET | **gerekmez** (self-contained yayın) | **gerekmez** |
| Veritabanı | MySQL 8.4 (yerel ya da compose) | MySQL 8.4 |
| Vekil | nginx + TLS | IIS + URL Rewrite + ARR |
| RAM | 2 GB (4 GB önerilir) | 4 GB |

> **OpenVZ tabanlı VPS'lerde Docker güvenilir çalışmaz.** O makinelerde
> Docker'sız yolu (systemd + yerel MySQL) kullanın — ikisi de desteklenir.

## 2. Yayın üret (geliştirici makinesinde)

```bash
./dotnet/yayin-al.sh hepsi        # linux-x64 + win-x64
# çıktı: dotnet/yayin/<rid>/Kuyumcu.Api(.exe) + kuyumcu-migrate(.exe)
```

Self-contained kanıtı: hedef makinede `dotnet --list-sdks` **boş** olmalı ve
uygulama yine de çalışmalıdır. `--no-build` **hiçbir adımda kullanılmaz**.

## 3. Sırları üret (ASLA elle yazmayın)

```bash
sudo ./dotnet/kurulum.sh --hedef /etc/kuyumcu/.env --vt kuyumcu_net --ters-vekil true
```

```powershell
.\dotnet\kurulum.ps1 -Hedef 'C:\ProgramData\Kuyumcu\.env' -Vt kuyumcu_net
```

Betik JWT sırrı, MySQL parolası, AES-GCM alan şifreleme anahtarı ve **ayrı**
bir yedek anahtarı üretir; dosyayı `chmod 600` (Windows'ta ACL kalıtımı
kırılmış) yapar ve **ekrana hiçbir değer basmaz** — yalnız "var/yok".

Var olan `.env` **sessizce ezilmez**: `--yenile` / `-Yenile` bayrağı ister ve
eski dosyayı zaman damgalı kopyalar.

### 3a. Uygulama adresi ve masaüstü kısayolu (PAKET P-33 · K-26)

Adres kısayola **gömülmez, kurulumda sorulur** (varsayılan
`http://localhost:5199`). Aynı paket hem tek makinede hem ağ üstünde kurulur;
gömülü adres ikinci kurulumda sessizce yanlış yere bakardı.

```bash
# kurulum adresi sorar (etkileşimsiz koşuda varsayılanı kullanır ve bunu yazar)
sudo ./dotnet/kurulum.sh --hedef /etc/kuyumcu/.env --adres https://dukkan.ornek
# adres değişti → yalnız satır + kısayol yenilenir, sırlara dokunulmaz
sudo ./dotnet/kurulum.sh --hedef /etc/kuyumcu/.env --kisayol --adres <yeni-adres>
```

```powershell
.\dotnet\kurulum.ps1 -Hedef 'C:\ProgramData\Kuyumcu\.env' -Adres https://dukkan.ornek
.\dotnet\kurulum.ps1 -Hedef 'C:\ProgramData\Kuyumcu\.env' -SadeceKisayol -Adres <yeni-adres>
```

- Adres `.env`'de **tek satırdır**: `BYGATECH_ADRES=<adres>`.
- Kısayol o adresle üretilir: macOS `BYGATECH.webloc`, Linux `BYGATECH.desktop`,
  Windows `BYGATECH.url` (masaüstü; `--kisayol-dizin` / `-KisayolDizin` ile değişir).
- Bağlama noktası adresten türer: adres bu makineyi gösteriyorsa
  (`http://localhost:PORT`) uygulama o portu dinler; başka bir ana makineyi
  gösteriyorsa ters vekilin arkasındaki iç noktayı (§ 0) dinler.
- Geçersiz adres (yol taşıyan, şemasız) reddedilir; hiçbir dosya yazılmaz.

### Neden fail-closed

Uygulama şu beş durumda **HİÇ AÇILMAZ** (`Common/UretimAcilisDenetimi.cs`):

| # | Kural | Sebep |
|---|---|---|
| 1 | `Jwt:Secret` örnek/varsayılan ya da 32 bayttan kısa | Aynı sır her müşteride → bir kurulumun token'ı diğerinde geçerli |
| 2 | DB parolası `DEGISTIR` ya da geliştirme parolası | Tekrar eden parola, kuruldaki KRİTİK bulgu |
| 3 | `Sifreleme:Anahtar` yok | MASAK/KVKK kimlik verisi şifresiz yazılamaz |
| 4 | Ne Kestrel HTTPS ne `Uretim:TersVekilArkasinda=true` | TLS'siz üretim onaylanmaz |
| 5 | Bekleyen migration var | Yedeksiz şema değişimi geri alınamaz (çıkış kodu **3**) |

Hata mesajları **değer içermez** ("Jwt:Secret varsayılan/örnek değerde").

## 4a. Kurulum — Linux + systemd (Docker'sız)

```bash
sudo useradd --system --home /opt/kuyumcu --shell /usr/sbin/nologin kuyumcu
sudo mkdir -p /opt/kuyumcu/yayin /var/lib/kuyumcu
sudo rsync -a dotnet/yayin/linux-x64/ /opt/kuyumcu/yayin/
sudo chown -R kuyumcu:kuyumcu /opt/kuyumcu /var/lib/kuyumcu

sudo cp deploy/systemd/kuyumcu.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now kuyumcu
systemctl is-active kuyumcu          # → active
```

## 4b. Kurulum — Docker (üretim compose)

```bash
docker compose --env-file /etc/kuyumcu/.env -f docker-compose.uretim.yml up -d
```

* MySQL'de **`ports` anahtarı yoktur** — 3306 yalnız iç ağda (`expose`).
  Ana makineden `mysql --host 127.0.0.1 --port 3306` **reddedilmelidir**.
* CPU/bellek limiti ve `json-file` log rotasyonu (10m × 5) tanımlıdır.
* Sağlık kontrolü parolayı komut satırında taşımaz (`MYSQL_PWD`).

## 4c. Kurulum — Windows servisi

```powershell
.\deploy\kuyumcu-service-kur.ps1 -YayinDizini 'C:\Kuyumcu\yayin'
```

`.env` içeriği makine kapsamlı ortam değişkenlerine yazılır (değerler ekrana
basılmaz) ve `ASPNETCORE_ENVIRONMENT` **Production'a sabitlenir**.

## 5. Ters vekil + TLS

```bash
sudo cp deploy/nginx-kuyumcu.conf /etc/nginx/sites-available/kuyumcu
sudo ln -s /etc/nginx/sites-available/kuyumcu /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
```

IIS için `deploy/iis-web.config` dosyasını site kökene `web.config` adıyla
kopyalayın (URL Rewrite + ARR gerekir).

**Sertifika:** T-1 TLS'i *zorunlu kılar*; sertifika **edinimi** F-7'dedir
(Let's Encrypt + otomatik yenileme). T-1 kabulü için elle üretilmiş/geçici
sertifika yeterlidir — bu, gerçek müşteri kurulumunun kanıtı **sayılmaz**.

`ngx_brotli` opsiyoneldir ve şablonda **yoktur** (modül kuruluysa ayrı bir
conf parçasıyla ekleyin; yoksa `nginx -t` düşer). `gzip` zorunludur.

### Doğrulama

```bash
curl -I http://<alan>/                 # 301, Location: https://...
curl -I https://<alan>/                # 200 + Strict-Transport-Security (TEK kopya)
curl -I https://<alan>/_framework/blazor.webassembly.js   # aynı başlıklar
```

Her güvenlik başlığı **tek kopya** gelmelidir. İki kopya görürseniz vekile
`add_header` eklenmiştir — kaldırın.

## 6. Şema

Uygulama şemayı **kendi değiştirmez**. Yordam: `docs/RUNBOOK-SEMA-YUKSELTME.md`
(önce doğrulanmış yedek, sonra `kuyumcu-migrate`).

```bash
curl -s http://127.0.0.1:5000/saglik         # 200 Healthy  (CANLILIK)
curl -s http://127.0.0.1:5000/saglik/hazir   # 200 hazir:true (HAZIRLIK)
```

## 7. Günlük (K14 — kalıcı günlük asgarisi)

| Ortam | Hedef | Saklama/boyut |
|---|---|---|
| systemd | journald, `SyslogIdentifier=kuyumcu-erp` | `/etc/systemd/journald.conf`: `SystemMaxUse`, `MaxRetentionSec` |
| Docker | `json-file` | `max-size: 10m`, `max-file: 5` |
| Windows | EventLog (kaynak: servis adı) | Olay Görüntüleyici → günlük boyutu |

```bash
journalctl -u kuyumcu -t kuyumcu-erp -n 200 --no-pager
```

**Maskeleme:** günlüğe sır, kişisel veri ve **istek gövdesi yazılmaz**;
`appsettings.Production.json` seviyesi `Warning`, EF komut günlüğü (SQL +
parametre) kapalıdır.

> **Saklama süresi rakamı burada BELİRLENMEZ.** VUK/KVKK dayanağı **T-3**
> paketinde belirlenir; T-1 yalnız *yapılandırılabilirliği* sağlar. Bugün bir
> gün sayısı yazmak varsayım olurdu (Anayasa md. 1).

## 8. Yedek

**Tek yedek yolu** `dotnet/yedek-al-ve-dogrula.sh`'tır (ikinci bir yedek komutu
belgelenmez — `denetim.sh` YEDEK_TEK_YOL).

```bash
./dotnet/yedek-al-ve-dogrula.sh ; echo "cikis kodu: $?"    # 0 = CHECKSUM eşleşti
```

Otomasyon (zamanlayıcı, şifreli dış kopya, tatbikat) **T-3** paketidir.
Geri yükleme tatbikatı **en geç 30 günde bir** koşar; son başarılı tatbikatın
tarihi `/yedekler` ekranında görünür, 30 günü aşarsa ekran kırmızı uyarı verir
(PAKET P-33 · BK-30). Ayrıntı ve tatbikat kaydı: `docs/YEDEK-GERI-YUKLEME.md`.

## 9. Sır rotasyonu

> ⚠ **En tehlikeli bakım işlemi budur.** Sırayı bozmak veri kaybı üretir.

| Sır | Değişince ne olur | Yordam |
|---|---|---|
| `Jwt__Secret` | Tüm oturumlar düşer (beklenen) | Servisi durdur → `.env` güncelle → başlat |
| `MYSQL_ROOT_PASSWORD` | Bağlantı kopar | Önce MySQL'de `ALTER USER`, sonra `.env` |
| `Yedek__SifrelemeAnahtari` | **Eski yedekler açılamaz** | Eski anahtarı arşivle, silme |
| `KUYUMCU_SIFRELEME_ANAHTARI` | **Şifreli alanlar okunamaz** | Aşağıdaki zorunlu adımlar |

### Alan şifreleme anahtarı rotasyonu — zorunlu adımlar

1. `./dotnet/yedek-al-ve-dogrula.sh` → çıkış kodu **0** olmadan devam etmeyin.
2. Servisi durdurun.
3. **MFA gizlerini yeniden şifreleyin** (45-API devir koşulu 2):
   `kullanici_mfa.gizli_sifreli` alanları **eski** anahtarla çözülüp **yeni**
   anahtarla yazılmazsa, MFA'sı açık **her kullanıcı kilitlenir** ve her biri
   için `docs/RUNBOOK-MFA-KURTARMA.md` gerekir.
4. Aynı şey şifreli **kimlik alanları** (MASAK/KVKK) için de geçerlidir.
5. Doğrulama sorgusu — rotasyondan sonra `v1:` önekli **eski sürüm** kayıt
   kalmamalıdır:

```sql
SELECT COUNT(*) AS yeniden_sifrelenmemis
  FROM kullanici_mfa
 WHERE durum = 'ETKIN' AND gizli_sifreli NOT LIKE CONCAT(?, ':%');   -- ? = yeni anahtar sürümü
```

6. Servisi başlatın, **bir** MFA kullanıcısıyla gerçek giriş yapıp doğrulayın.
   "Herhalde olmuştur" denmez.

> Anahtar kasası (KMS) entegrasyonu ve otomatik rotasyon **1.1'e** devreder.

## 10. Kabul kontrol listesi

- [ ] `dotnet --list-sdks` boş, uygulama çalışıyor (self-contained)
- [ ] `.env` izni 600 / ACL kırılmış, sahibi servis hesabı
- [ ] Varsayılan sırla açılış **durur** (log'da değer yok)
- [ ] TLS beyanı olmadan açılış **durur**
- [ ] `http://` → **301**, `https://` → 200 + HSTS (tek kopya)
- [ ] Ana makineden 3306 **reddediliyor**
- [ ] `/saglik` 200, `/saglik/hazir` 200
- [ ] `systemctl kill -s KILL kuyumcu` sonrası süreç kendiliğinden dönüyor
- [ ] `./dotnet/yedek-al-ve-dogrula.sh` çıkış kodu 0
- [ ] `/yedekler` ekranında "Son geri yükleme tatbikatı" 30 günden yeni (kırmızı bant yok)
- [ ] `.env`'de tek `BYGATECH_ADRES` satırı var; masaüstü kısayolu bu adresi açıyor
- [ ] Teslimat adımları `docs/RUNBOOK-TESLIMAT.md`'ye göre işaretlendi
- [ ] **45-API (MFA) kapısı:** gerçek müşteri kurulumu MFA tamamlanmadan
      yapılmaz (Mehmet mührü) ve `docs/RUNBOOK-MFA-KURTARMA.md` okunmuştur
