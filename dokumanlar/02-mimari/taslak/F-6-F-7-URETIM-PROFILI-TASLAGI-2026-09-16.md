# TASLAK — uygulama T-1(+F-6) paketi tarafından; bu belge kod değildir

> F-6 üretim profili + F-7 TLS/HSTS/Let's Encrypt taslağı · 2026-09-16 · Hat: 3-P (paralel belge hattı, MÜHÜR 5 KOŞUL 4).
> Bu belgede hiçbir sır DEĞERİ yoktur. Aşağıdaki yaml/conf/ini blokları **taslaktır**; hiçbiri bu oturumda bir dosyaya uygulanmamıştır.
> Tek-yazar dosyalarına (Program.cs, appsettings*, nginx conf, deploy/*, dotnet/ altı) bu hatta YAZILMAZ — uygulama T-1 seri hattında, tek elden yapılır.

## 0. Dayanak ve bugünkü durum (kanıt)

| Konu | Bugün | Kaynak |
|---|---|---|
| MySQL portu dışa açık | `ports: - "3306:3306"` | docker-compose.yml:9-10 |
| Root parolası düz metin gömülü | var (değer YAZILMADI) | docker-compose.yml:7 |
| Healthcheck parolayı komut satırında taşıyor | var | docker-compose.yml:14-15 |
| nginx yalnız 80 dinliyor, TLS/HSTS yok | var | deploy/nginx-kuyumcu.conf:12-13 |
| nginx `X-Frame-Options SAMEORIGIN` | var | deploy/nginx-kuyumcu.conf:71 |
| Program.cs `X-Frame-Options DENY` + CSP `frame-ancestors 'none'` | var | Program.cs:310, 315-327 |
| `UseHsts()` yalnız Development dışında | var | Program.cs:331-332 |
| `UseHttpsRedirection()` yalnız Development dışında | var | Program.cs:337-338 |
| `UseForwardedHeaders()` boru hattının başında, KnownProxies=loopback + `TersVekil:GuvenilirVekiller` | var | Program.cs:272-291, 294 |
| `/saglik` anonim health check | var | Program.cs:253, 347 |
| `deploy/systemd/` dizini | **YOK** (bu paketle açılacak) | `ls deploy/` → ecosystem.config.js, iis-web.config, nginx-kuyumcu.conf |
| `docs/` tek belge kökü | var (DEPLOY.md dâhil) | `ls docs/` |

Bağlayıcı çerçeve: T-1 belgesi § Kapsam/İş kuralları (FAIL-CLOSED 1-6), § Mühür 4 eki **K14** (kalıcı günlük asgarisi), **K4** (`/api/v1/sistem/*` IP kısıtı), **K6** (docs/ tek kök) — kaynak: PAKET-T-1-…-birlesik.md:214-216. MÜHÜR 5 KOŞUL 4/5: bu hatta tek-yazar dosyası ve migration üretimi YASAK — kaynak: ham/muhur5.md § Koşullar.

---

## 1. docker-compose.uretim.yml TASLAĞI

Kaynak: T-1 § Servis ve dosyalar (`docker-compose.uretim.yml` YENİ maddesi) + K14 (json-file rotasyonu).

```yaml
# TASLAK — docker-compose.uretim.yml (uygulama: T-1)
# Sır DEĞERİ yok; tüm sırlar .env dosyasından gelir (env_file).
name: kuyumcu-uretim

services:
  mysql:
    image: mysql:8.4
    container_name: kuyumcu-mysql
    restart: unless-stopped
    env_file: [.env]
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD:?zorunlu}   # .env'den; varsayılan YOK -> fail-closed
      MYSQL_DATABASE: ${KUYUMCU_VT:?zorunlu}                 # tek kaynak DB adı (T-1 is kurali)
    # ports ANAHTARI YOK -- 3306 dışa açılmaz (T-1 is kurali: "ASLA yayimlamaz")
    expose: ["3306"]
    networks: [ic]
    command: --character-set-server=utf8mb4 --collation-server=utf8mb4_turkish_ci
    volumes:
      - kuyumcu_mysql_data:/var/lib/mysql
      - ./yedek:/yedek                                       # yedek-al-ve-dogrula.sh cikti hedefi
    healthcheck:   # parola komut satirinda TASINMAZ; MYSQL_PWD ile verilir
      test: ["CMD-SHELL", "MYSQL_PWD=\"$$MYSQL_ROOT_PASSWORD\" mysqladmin ping -h 127.0.0.1 -u root --silent"]
      interval: 10s
      timeout: 5s
      retries: 12
      start_period: 30s
    deploy: { resources: { limits: { cpus: "2.0", memory: 2G } } }   # DOGRULANMALI
    logging:
      driver: json-file
      options: { max-size: "10m", max-file: "5" }  # K14

  api:
    image: kuyumcu/api:${KUYUMCU_SURUM:?zorunlu}   # T-4a surum damgasi ile beslenir
    container_name: kuyumcu-api
    restart: unless-stopped
    depends_on:
      mysql: { condition: service_healthy }
    env_file: [.env]
    environment:
      ASPNETCORE_ENVIRONMENT: Production           # FAIL-CLOSED 6: Development yasak
      ASPNETCORE_URLS: http://+:5000               # TLS vekilde sonlanir
      Uretim__TersVekilArkasinda: "true"           # FAIL-CLOSED 4 beyani
    expose: ["5000"]
    networks: [ic, dis]
    volumes:
      - ./veri/koruma-anahtarlari:/var/kuyumcu/dpkeys   # DataProtection kaliciligi (DOGRULANMALI)
    healthcheck:
      test: ["CMD-SHELL", "wget -qO- http://127.0.0.1:5000/saglik >/dev/null || exit 1"]
      interval: 15s
      retries: 5
      start_period: 40s
    deploy: { resources: { limits: { cpus: "2.0", memory: 1G } } }   # DOGRULANMALI
    logging:
      driver: json-file
      options: { max-size: "10m", max-file: "5" }  # K14

networks:
  ic:  { internal: true }    # mysql yalnizca burada
  dis: {}                    # yalnizca api<->nginx

volumes:
  kuyumcu_mysql_data:
```

Notlar: `mysql` yalnız `ic` ağında (nginx/host teması yok). `migrate` compose'ta **servis değildir** — bundle elle, yedek sonrası çalışır (FAIL-CLOSED 5). `KUYUMCU_VT` tek kaynak DB adıdır; compose/appsettings/yedek betiği aynı değişkeni okur.

---

## 2. .env.ornek + kurulumda sır üretimi

### 2.1 `.env.ornek` TASLAĞI (yalnız anahtar ADLARI; gerçek değer YOK)

```ini
# TASLAK — .env.ornek · gercek .env ASLA repoda tutulmaz; DEGISTIR- onekli deger ile Production ACILMAZ.

KUYUMCU_VT=DEGISTIR-veritabani-adi
KUYUMCU_SURUM=DEGISTIR-surum-etiketi

MYSQL_ROOT_PASSWORD=DEGISTIR-uretimde-uretilir
ConnectionStrings__Kuyumcu=DEGISTIR-baglanti-dizgesi-parola-env-den

Jwt__Secret=DEGISTIR-en-az-32-bayt-base64
Sifreleme__Anahtar=DEGISTIR-32-bayt-base64-aes-gcm
Yedek__SifrelemeAnahtari=DEGISTIR-32-bayt-base64-AYRI-anahtar   # MUHUR 3: alan sifreleme anahtarindan AYRI

Kur__VarsayilanKaynak=DEGISTIR-kaynak-adi
Kur__SozlesmesizKaynaklarAcik=false

Sistem__Anahtari=DEGISTIR-pilot-davet-anahtari                  # K4/K6: /api/v1/sistem/* icin
TersVekil__GuvenilirVekiller__0=DEGISTIR-vekil-ip
```

Anahtar adlarının dayanağı: `appsettings.json` → `ConnectionStrings:Kuyumcu` (:9-10), `Jwt:Secret` (:19-20), `Kur:VarsayilanKaynak` / `Kur:SozlesmesizKaynaklarAcik` (:14-15), `Cors:Origins` (:59-60); `appsettings.Development.json` → `Sifreleme:Anahtar` (:24-25). `Sistem:Anahtari`, `Yedek:SifrelemeAnahtari`, `Uretim:TersVekilArkasinda` **bugün appsettings'te YOK** — T-1/F-2-min ile doğar (DOĞRULANMALI: kesin ad uygulayıcı tarafından sabitlenecek).

### 2.2 Sır üretimi betiğinin AKIŞI (`dotnet/kurulum.sh` / `kurulum.ps1`)

1. Ön kontrol: hedef `.env` **varsa dur** (üzerine yazma yok); yalnız `--yenile` bayrağıyla ve zaman damgalı kopya alarak devam (T-1 iş kuralı).
2. `umask 077` ile başla; dosya daha oluşurken 600 olsun.
3. Üret: `openssl rand -base64 48` → `Jwt__Secret`; `openssl rand -base64 32` → `Sifreleme__Anahtar`; **ayrı** `openssl rand -base64 32` → `Yedek__SifrelemeAnahtari`; `openssl rand -base64 32` → `Sistem__Anahtari`; `openssl rand -base64 24` (alfanümerik süzülmüş) → `MYSQL_ROOT_PASSWORD`.
4. `ConnectionStrings__Kuyumcu` dizgesini üretilen parola + `KUYUMCU_VT` ile kur.
5. Yaz: `/etc/kuyumcu/.env`, sahibi `kuyumcu:kuyumcu`, izin `chmod 600` (dizin 750). Windows'ta makine kapsamlı ortam değişkeni + ACL yalnız servis hesabı.
6. Ekrana **yalnız var/yok** basılır: `Jwt__Secret: var (48 bayt)` gibi; hiçbir değer yazdırılmaz, `set -x` kapalı, kabuk geçmişine sır düşmez.
7. Doğrulama turu: hiçbir değer `DEGISTIR-` ile başlamıyor, hepsi asgari uzunlukta, `Sifreleme__Anahtar != Yedek__SifrelemeAnahtari` → değilse hata ile çık.

---

## 3. appsettings.Production.json TASLAĞI (sır YOK)

```jsonc
// TASLAK — dotnet/Kuyumcu.Api/appsettings.Production.json
// Sir ICERMEZ: ConnectionStrings/Jwt/Sifreleme bloklari YOKTUR, ortam degiskeninden gelir.
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Kuyumcu": "Information"          // is olaylari; istek govdesi/PII/sir LOGLANMAZ (K14)
    }
  },
  "AllowedHosts": "DEGISTIR-alan-adi",
  "Urls": "http://+:5000",              // TLS vekilde; Kestrel'de sertifika yok
  "Uretim": {
    "TersVekilArkasinda": true          // FAIL-CLOSED 4 beyani
  },
  "TersVekil": {
    "GuvenilirVekiller": []             // bos = yalniz loopback (Program.cs:277-279)
  },
  "Kur": {
    "SozlesmesizKaynaklarAcik": false   // uretimde kapali
  },
  "Cors": { "Origins": [] },
  "Sistem": { "Anahtari": "DEGISTIR-ortam-degiskeninden-gelir" },
  "Yedek": { "Dizin": "/var/kuyumcu/yedek" }   // yalniz YOL; sifreleme anahtari env'de
}
```
- `Masak`, `Tevsik`, `Belge`, `Hurda`, `Takvim` blokları `appsettings.json`'dan miras kalır, tekrarlanmaz (T-1 § Servis ve dosyalar).
- `.ornek` karşılığı repoda tutulur; gerçek dosya sır tutmadığı için repoda kalabilir — **sır yalnızca `.env`de**.

---

## 4. nginx üretim TASLAĞI (F-6 + F-7)

### 4.1 Tek kaynak kararı — güvenlik başlıkları

**Karar (öneri): uygulama başlıkları tek kaynak = Program.cs; nginx başlık EKLEMEZ.** Gerekçe: (a) Program.cs:308-327 zaten tam set gönderir (CSP dâhil) ve API + WASM statiklerinin ikisini de aynı süreç sunar (Program.cs `UseBlazorFrameworkFiles`); (b) nginx `add_header` bir `location` bloğunda başka `add_header` varsa üst blok başlıklarını **düşürür** — iki kaynak sessiz boşluk üretir; (c) CSP'nin Blazor kısıtları (`wasm-unsafe-eval`) kodla birlikte değişir, conf ile senkron tutulamaz.
**Tek istisna HSTS:** 80→443 yönlendirmesini nginx yaptığı için HSTS başlığı da vekilde verilir. Program.cs:331-332'deki `UseHsts()` bu durumda **çift başlık üretmez** mi? → **DOĞRULANMALI**: ASP.NET Core HSTS ara katmanı yalnız HTTPS isteklerde başlık basar; vekil arkasında `X-Forwarded-Proto: https` ile istek HTTPS sayılır, yani ikisi de basabilir. Uygulayıcı ya nginx'teki HSTS'i kaldırır ya Program.cs'te `UseHsts()`'i vekil beyanı varken atlar — **tek yerde** kalmalı. (Bu satır Program.cs'e kod gerektirebilir; bkz. § 9.)

### 4.2 Bilinen uyumsuzluk — X-Frame-Options

`deploy/nginx-kuyumcu.conf:71` **SAMEORIGIN** ↔ `Program.cs:310` **DENY** ve CSP `frame-ancestors 'none'` (Program.cs:327). Modern tarayıcıda `frame-ancestors` XFO'yu ezer, yani gerçek davranış DENY'dır; nginx'teki SAMEORIGIN yanıltıcıdır. **Öneri: nginx'teki `X-Content-Type-Options` ve `X-Frame-Options` satırları KALDIRILIR** (tek kaynak = Program.cs).

### 4.3 Conf TASLAĞI

```nginx
# TASLAK — deploy/nginx-kuyumcu.conf (uretim profili)
map $http_upgrade $connection_upgrade { default upgrade; '' close; }

server {                                   # 80: yalniz ACME + 301
    listen 80; listen [::]:80;
    server_name DEGISTIR.ornek.com;

    location /.well-known/acme-challenge/ { root /var/www/certbot; }   # F-7
    location / { return 301 https://$host$request_uri; }
}

server {
    listen 443 ssl; listen [::]:443 ssl; http2 on;
    server_name DEGISTIR.ornek.com;

    ssl_certificate     /etc/letsencrypt/live/DEGISTIR.ornek.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/DEGISTIR.ornek.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3; ssl_prefer_server_ciphers off;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305;
    ssl_session_cache shared:SSL:10m; ssl_session_tickets off;
    ssl_stapling on; ssl_stapling_verify on;

    # HSTS — TEK KAYNAK kararina gore burada VEYA Program.cs'te; ikisinde birden DEGIL.
    # preload: DOGRULANMALI — preload listesine girmek geri donusu zor; tum alt alan
    # adlari HTTPS olmadan includeSubDomains/preload ACILMAZ. Once 300 gun, sonra karar.
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    # NOT: burada baska add_header YOKTUR (CSP/XFO/nosniff tek kaynak = Program.cs).

    root /var/www/kuyumcu/web; index index.html;

    # ── K4: /api/v1/sistem/* yalnizca IP allow-list ──────────────────────────
    location ^~ /api/v1/sistem/ {
        allow 127.0.0.1;
        allow DEGISTIR-yonetim-ip/32;      # kurulumda doldurulur
        deny all;                          # 45-API bitene kadar da gecerli
        proxy_pass http://127.0.0.1:5000;
        include snippets/kuyumcu-proxy.conf;   # http_version 1.1 + Host/X-Real-IP/X-Forwarded-For/X-Forwarded-Proto
    }

    # Belge yukleme yollari — bugunku :24-52 icerigi KORUNUR (8m siniri)
    location ~ ^/api/v1/(belgeler|urunler) {
        client_max_body_size 8m;
        proxy_pass http://127.0.0.1:5000;
        include snippets/kuyumcu-proxy.conf;   # http_version 1.1 + Host/X-Real-IP/X-Forwarded-For/X-Forwarded-Proto
    }

    location /api/ {
        proxy_pass http://127.0.0.1:5000;
        include snippets/kuyumcu-proxy.conf;   # http_version 1.1 + Host/X-Real-IP/X-Forwarded-For/X-Forwarded-Proto
        proxy_set_header Upgrade $http_upgrade;          # SignalR
        proxy_set_header Connection $connection_upgrade;
        proxy_read_timeout 300s;
    }

    # ── Blazor WASM statikleri ───────────────────────────────────────────────
    types { application/wasm wasm; }
    location ~* (^/_framework/|\.(wasm|dat|blat|dll)$) {
        expires 1y; add_header Cache-Control "public, immutable";
        gzip_static on; brotli_static on;                 # brotli modulu: DOGRULANMALI
    }
    location = /index.html { add_header Cache-Control "no-cache"; }
    location = /service-worker.js { add_header Cache-Control "no-cache"; }
    location /assets/ { expires 7d; add_header Cache-Control "public, immutable"; }
    location / { try_files $uri $uri/ /index.html; }

    gzip on; gzip_vary on; gzip_min_length 1024;
    gzip_types text/css application/javascript application/json image/svg+xml application/wasm;
}
```
- `brotli` / `brotli_static` çekirdek nginx'te yoktur (ngx_brotli modülü gerekir) — **DOĞRULANMALI**; yoksa `gzip_static` yeterlidir, blok modülsüz `nginx -t` hatası verir.

---

## 5. F-7 — Let's Encrypt otomasyonu

**İlk sertifika (443 açılmadan önce, tavuk-yumurta):**
1. Yalnız § 4.3'teki **80 bloğuyla** başla (443 bloğu yorumda), `nginx -t && systemctl reload nginx`.
2. `certbot certonly --webroot -w /var/www/certbot -d DEGISTIR.ornek.com --agree-tos -m DEGISTIR-eposta --no-eff-email`
3. Sertifika dosyaları oluştuktan sonra 443 bloğunu aç, `nginx -t && systemctl reload nginx`.
4. Alternatif (alan adı henüz yönlenmiyorsa / joker isteniyorsa): `--dns-<saglayici>` eklentisiyle DNS-01. Sağlayıcı API anahtarı da `.env` disiplinine tabidir (600, değer belgede YOK).

**Yenileme:** certbot paketi kendi `certbot.timer` birimini kurar (`systemctl list-timers certbot.timer` ile doğrulanır — **DOĞRULANMALI**, dağıtıma göre `cron.d/certbot` olabilir). Yenileme sonrası nginx'in yeni sertifikayı alması için deploy hook:
`/etc/letsencrypt/renewal-hooks/deploy/10-nginx-reload.sh` (chmod 755) → `#!/bin/sh` + `nginx -t && systemctl reload nginx`.
**Kuru prova:** `certbot renew --dry-run` çıktısı kurulum runbook'unda kanıt olarak saklanır. Sertifika süresi `/saglik/hazir` gövdesine **konmaz** (bilgi sızıntısı); izleme ayrı yapılır.

---

## 6. systemd birim TASLAĞI + Windows notu

**Tek yayın yolu (MÜHÜR 3):** systemd birimleri yalnız `deploy/systemd/`, betikler yalnız `dotnet/` kökü, runbook'lar yalnız `docs/`. Dizin bugün **yok**, T-1 ile açılır. (T-1 § Servis ve dosyalar `deploy/kuyumcu.service` diyor; MÜHÜR 3 kararı `deploy/systemd/kuyumcu.service` konumunu dayatır — uygulayıcı **`deploy/systemd/`** kullanır.)

```ini
# TASLAK — deploy/systemd/kuyumcu.service
[Unit]
Description=Kuyumcu ERP API
After=network-online.target mysql.service
Wants=network-online.target

[Service]
Type=notify                          # UseSystemd ile (T-1 § Servis)
User=kuyumcu
Group=kuyumcu
WorkingDirectory=/opt/kuyumcu/yayin
ExecStart=/opt/kuyumcu/yayin/Kuyumcu.Api
EnvironmentFile=/etc/kuyumcu/.env    # 600, sahibi kuyumcu
Environment=ASPNETCORE_ENVIRONMENT=Production
Restart=on-failure
RestartSec=5

StandardOutput=journal          # K14
StandardError=journal
SyslogIdentifier=kuyumcu

# Sertlestirme
NoNewPrivileges=true
PrivateTmp=true
PrivateDevices=true
ProtectSystem=strict
ProtectHome=true
ProtectKernelTunables=true
ProtectKernelModules=true
ProtectControlGroups=true
RestrictSUIDSGID=true
RestrictNamespaces=true
LockPersonality=true
CapabilityBoundingSet=
AmbientCapabilities=
MemoryDenyWriteExecute=false         # JIT/WASM nedeniyle false — DOGRULANMALI
ReadWritePaths=/var/kuyumcu

[Install]
WantedBy=multi-user.target
```

**journald saklama (K14):** `/etc/systemd/journald.conf.d/kuyumcu.conf` → `[Journal]` `Storage=persistent`, `SystemMaxUse=2G`, `MaxRetentionSec=1year` (**DOĞRULANMALI**: MASAK 8 yıl saklama **defter** verisi içindir; uygulama günlüğü süresi hukuk görüşüne bağlı).
**Windows Service notu:** `sc.exe create kuyumcu-erp binPath= "...\yayin\win-x64\Kuyumcu.Api.exe" start= auto obj= "NT SERVICE\kuyumcu"` (T-1 § Servis). Ortam değişkenleri **makine kapsamında**, ACL yalnız servis hesabına. Kalıcı günlük = **EventLog** (K14); `.env` dosyası yerine Windows'ta makine ortam değişkenleri kullanılır, dosya kullanılırsa ACL ile servis hesabına kısıtlanır.

---

## 7. UretimAcilisDenetimi — fail-closed kontrol listesi

Kaynak: T-1 § İş kuralları FAIL-CLOSED 1-6.

| # | Kontrol | Sonuç |
|---|---|---|
| 1 | `Jwt:Secret` `DEGISTIR-` önekli / Development'taki dizgeyle aynı / < 32 bayt | Build() öncesi `InvalidOperationException`, süreç ≠0 kodla biter |
| 2 | Bağlantı dizgesi parolası `DEGISTIR` ya da Development ile birebir aynı | açılış durur |
| 3 | `Sifreleme:Anahtar` yok/geçersiz | zaten durur (Common/AlanSifreleme.cs:115-140 kalıbı) |
| 4 | `Yedek:SifrelemeAnahtari` yok **veya** `Sifreleme:Anahtar` ile aynı | durur (MÜHÜR 3 sır disiplini) |
| 5 | Ne Kestrel HTTPS uç noktası ne `Uretim:TersVekilArkasinda=true` | durur (TLS'siz üretim yok) |
| 6 | `GetPendingMigrationsAsync()` boş değil (Production) | hizmet vermez; log + çıkış kodu 3. Otomatik `Migrate()` **açılmaz** |
| 7 | `ASPNETCORE_ENVIRONMENT=Development` + üretim birimi | başlatılamaz |
| 8 | `Sistem:Anahtari` yok/`DEGISTIR-` | `/api/v1/sistem/*` uçları kapalı açılır (K4) |
| 9 | TLS beyanı var ama sertifika/vekil doğrulanamıyor | **uyarı** (durdurmaz) |
- Tüm mesajlar **değer içermez**: "Jwt:Secret varsayılan/örnek değerde" biçimi. Sır loga/`/saglik` gövdesine/hata sayfasına yazılmaz.
- `/saglik` **değişmez** (anonim, Healthy/Unhealthy). `/saglik/hazir` yalnız `{hazir:false, kod}` döner; migration adı verilmez.

---

## 8. `docs/DEPLOY.md` runbook iskeleti (yalnız başlıklar — K6: tek docs kökü)

1. Kapsam ve önkoşullar (45-API/MFA kapısı: **45 bitmeden gerçek üretim yayını YOK**)
2. Donanım/işletim sistemi asgarisi — DOĞRULANMALI
3. Alan adı + DNS + güvenlik duvarı (yalnız 80/443 dışa; 3306 ASLA)
4. **Önce yedek al** — `dotnet/yedek-al-ve-dogrula.sh`, CHECKSUM kanıtı dosyalanır (ilk kurulumda "yedek yok" kaydı)
5. Sır üretimi — `dotnet/kurulum.sh` / `kurulum.ps1`, `.env` 600, ekranda yalnız var/yok
6. Yayın alma — `dotnet/yayin-al.sh` (self-contained, `--no-build` YASAK), çıktı `yayin/<rid>/`
7. Şema yükseltmesi — migration bundle (`kuyumcu-migrate`), **adım 4'teki yedek olmadan çalıştırılmaz**
8. Servis kurulumu — Linux `deploy/systemd/kuyumcu.service` · Windows `sc.exe` · Docker `docker-compose.uretim.yml`
9. TLS — F-7 akışı (§ 5), `certbot renew --dry-run` kanıtı
10. nginx kurulumu ve `nginx -t` çıktısı
11. Açılış doğrulaması — `/saglik` 200, `/saglik/hazir` 200, HTTPS zorlaması, HSTS başlığı tek kopya
12. Pilot daveti — `curl` + `X-Sistem-Anahtari` örneği + SMS/WhatsApp davet metni şablonu (K6; SMTP yok)
13. Günlükler/saklama (K14) · 14. Yedek-geri yükleme provası · 15. Geri alma · 16. Sorun giderme
17. **Kaldırılıyor:** `deploy/ecosystem.config.js` ve `start.bat` üretim yolu olmaktan çıkar; başlıklarına "GELİŞTİRME içindir" uyarısı (ikiz yayın yolu yasağı)

---

## 9. DOĞRULANMALI listesi + T-1 uygulayıcısına notlar

### 9.1 DOĞRULANMALI
1. HSTS'in çift basılması: `UseHsts()` (Program.cs:331-332) vekil arkasında da başlık basıyor mu? Tek kaynak hangisi olacak?
2. HSTS `preload` alınacak mı (geri dönüşü zor) ve `includeSubDomains` tüm alt alan adlarında HTTPS var mı?
3. ngx_brotli modülü hedef nginx derlemesinde var mı?
4. `certbot.timer` mı `cron.d/certbot` mi (dağıtıma göre).
5. `MemoryDenyWriteExecute=false` gerçekten gerekli mi (.NET JIT) — sertleştirme kaybı ölçülmeli.
6. journald `MaxRetentionSec` için hukuki saklama süresi (MASAK 8 yıl **defter** verisi içindir; uygulama günlüğü ayrı).
7. Kaynak limitleri (cpus/memory) — hedef donanım bilinmiyor, rakam uydurulmadı.
8. `Sistem:Anahtari`, `Yedek:SifrelemeAnahtari`, `Uretim:TersVekilArkasinda`, `TersVekil:GuvenilirVekiller` anahtar adlarının kesin yazımı (bugün appsettings'te yalnız `TersVekil:GuvenilirVekiller` Program.cs:281-282'de geçiyor; diğer üçü **yok**).
9. DataProtection anahtar kalıcılığı bugün nasıl sağlanıyor? (grep edilmedi) — konteyner yeniden doğduğunda oturumlar düşer mi?
10. `docker-compose.yml`'deki mevcut DB adı ile `appsettings.Development.json`'daki ad çelişkisi (T-1 § Kodda bugünkü durum) — `KUYUMCU_VT` tek kaynağı bu çelişkiyi kapatır, geçiş adımı yazılmalı.
11. `deploy/kuyumcu.service` (T-1 metni) ↔ `deploy/systemd/kuyumcu.service` (MÜHÜR 3) konum çelişkisi — bu belge MÜHÜR 3'ü esas aldı, Tamer Bey mühürlemeli.
12. `iis-web.config` (deploy/) üretim yolu olarak kalacak mı, yoksa 17. maddedeki gibi işaretlenecek mi.

### 9.2 Program.cs'e KOD gerektiren satırlar (tek-el, seri hat)
- `UretimAcilisDenetimi.Dogrula(...)` çağrısı (Build() öncesi) — FAIL-CLOSED 1,2,4,5,8.
- Bekleyen migration kontrolü + çıkış kodu 3; `/saglik/hazir` eşlemesi.
- `UseWindowsService()` / `UseSystemd()` host uzantıları.
- TLS beyanı kontrolü (FAIL-CLOSED 4) ve — karar 4.1'e göre — `UseHsts()`'in tek kaynak hâline getirilmesi.

### 9.3 Program.cs'e KOD EKLENMEYEN K14 kalemleri (ayrı, K14 lafzı gereği)
- systemd `StandardOutput=journal` + `SyslogIdentifier` → `deploy/systemd/kuyumcu.service`
- docker `logging: json-file` rotasyonu → `docker-compose.uretim.yml`
- Windows EventLog → servis kurulum betiği
- Günlük seviyesi + maskeleme politikası → `appsettings.Production.json` `Logging` bloğu
- Saklama/boyut sınırı → `journald.conf.d/kuyumcu.conf`
- Runbook § 13 → `docs/DEPLOY.md`

### 9.4 Bu hattın sınırları (MÜHÜR 5 KOŞUL 4/5)
Bu oturumda **yalnız bu belge** yazıldı. `Program.cs`, `appsettings*`, `deploy/*`, `dotnet/` altı, `docker-compose*.yml` **değiştirilmedi**; derleme/test/git/docker/migration **çalıştırılmadı**; hiçbir sır değeri okunup yazılmadı.
