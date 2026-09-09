# Kuyumcu ERP — Güzel Hosting Dağıtım Rehberi

## Önce önemli gerçek: paylaşımlı "Windows hosting" bu uygulamayı çalıştıramaz

Güzel Hosting'in (guzel.net.tr) güncel ürün yelpazesi incelendi (Temmuz 2026):

| Ürün | Durum | Bu uygulama için uygun mu? |
|---|---|---|
| Windows paylaşımlı hosting | **Artık satışta yok** (sayfa 404) | — |
| Linux paylaşımlı hosting | LiteSpeed / PHP odaklı | ❌ Node.js süreci çalıştıramaz → NestJS API barınamaz |
| **Public Cloud (VPS)** | Yalnızca Linux (AlmaLinux/Ubuntu/Debian), OpenVZ 7 | ✅ (Docker'sız kurulumla) |
| **Private Cloud (VDS)** | **Windows Server 2019/2022** veya Linux | ✅ Windows isteğinize birebir uyar |

Uygulamanın üç parçası var: **NestJS API** (kalıcı Node.js süreci), **React web**
(statik dosyalar) ve **MySQL 8**. Kalıcı Node süreci ancak VPS/VDS'te çalışır.

**Önerimiz:**
- Windows'ta ısrar ediyorsanız → **Private Cloud VDS + Windows Server 2022** (Bölüm A)
- Maliyet/sadelik öncelikliyse → **Public Cloud VPS + Ubuntu 22.04** (Bölüm B) —
  TR-VPS-3 (2 CPU / 2 GB RAM, ~567 TL/ay) MySQL dahil rahat yeter; TR-VPS-2 (1 GB)
  başlangıç için sınırda çalışır.

> Not: Public Cloud OpenVZ 7 sanallaştırma kullanır — Docker orada güvenilir
> çalışmaz. Bu yüzden her iki rehber de Docker'sız, doğrudan kurulum anlatır.
> (`docker-compose.yml` yalnızca geliştirme içindir.)

---

## Ortak hazırlık (kendi bilgisayarınızda)

1. Üretim derlemeleri (üçü de doğrulandı):
   ```bash
   pnpm --filter @kuyum/core build
   pnpm --filter @kuyum/db build
   pnpm --filter @kuyum/api build     # → apps/api/dist
   pnpm --filter @kuyum/web build     # → apps/web/dist (statik)
   ```
2. Alan adınızın **A kaydını** sunucu IP'sine yönlendirin (Güzel Hosting DNS panelinden).
3. Üretim için **yeni** JWT secret ve MySQL şifresi üretin — geliştirmedekileri
   asla taşımayın:
   ```powershell
   node -e "console.log(require('crypto').randomBytes(48).toString('base64url'))"
   ```

---

## Bölüm A — Windows Server 2022 VDS

### A1. Sipariş ve bağlantı
1. guzel.net.tr → Private Cloud (VDS) → paket seçin, işletim sistemi **Windows 2022**.
2. Teslim e-postasındaki IP + Administrator şifresiyle **RDP** ile bağlanın
   (Uzak Masaüstü Bağlantısı).

### A2. Yazılım kurulumu (PowerShell, yönetici)
```powershell
# Node.js 20 LTS + Git (winget yoksa nodejs.org ve git-scm.com'dan MSI)
winget install OpenJS.NodeJS.LTS Git.Git
npm install -g pnpm

# MySQL 8 Community — dev.mysql.com/downloads/installer → "Server only"
#   - root şifresi belirleyin (güçlü, not alın)
#   - Windows Service olarak kurun (varsayılan)
```
MySQL kurulduktan sonra veritabanını açın:
```powershell
mysql -u root -p -e "CREATE DATABASE kuyumcu CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci;"
```

### A3. Uygulamayı yerleştir
```powershell
cd C:\
git clone <repo-adresi> kuyumcu   # veya dosyaları kopyalayın (node_modules HARİÇ)
cd C:\kuyumcu
pnpm install
pnpm --filter @kuyum/core build ; pnpm --filter @kuyum/db build
pnpm --filter @kuyum/api build  ; pnpm --filter @kuyum/web build
```

`C:\kuyumcu\apps\api\.env` (üretim değerleriyle):
```env
DATABASE_URL=mysql://root:GUCLU_SIFRE@localhost:3306/kuyumcu
JWT_SECRET=URETTIGINIZ_YENI_SECRET
NODE_ENV=production
PORT=4000
```

Migration:
```powershell
$env:DATABASE_URL = 'mysql://root:GUCLU_SIFRE@localhost:3306/kuyumcu'
pnpm --filter @kuyum/db db:migrate
```

### A4. API'yi Windows servisi yap (NSSM)
[nssm.cc](https://nssm.cc/download) → `nssm.exe`'yi `C:\nssm\` içine koyun:
```powershell
C:\nssm\nssm.exe install KuyumcuAPI "C:\Program Files\nodejs\node.exe" "C:\kuyumcu\apps\api\dist\main.js"
C:\nssm\nssm.exe set KuyumcuAPI AppDirectory  C:\kuyumcu\apps\api
C:\nssm\nssm.exe set KuyumcuAPI AppStdout     C:\kuyumcu\logs\api.log
C:\nssm\nssm.exe set KuyumcuAPI AppStderr     C:\kuyumcu\logs\api-err.log
C:\nssm\nssm.exe start KuyumcuAPI
```
Kontrol: `curl http://localhost:4000/api/v1/kur/guncel` → 401 dönmeli (ayakta demek).

### A5. IIS ile web + proxy
1. **Rol ekle:** Server Manager → Add Roles → Web Server (IIS).
2. **Modüller:** [URL Rewrite](https://www.iis.net/downloads/microsoft/url-rewrite)
   ve [Application Request Routing (ARR)](https://www.iis.net/downloads/microsoft/application-request-routing) kurun.
3. IIS Manager → sunucu düğümü → **Application Request Routing Cache →
   Server Proxy Settings → "Enable proxy"** işaretle.
4. Yeni site: fiziksel yol `C:\kuyumcu\apps\web\dist`, host name alan adınız, port 80.
5. Repo'daki **`deploy/iis-web.config`** dosyasını `C:\kuyumcu\apps\web\dist\web.config`
   olarak kopyalayın (SPA fallback + `/api` → 4000 proxy'si hazır).

### A6. SSL (Let's Encrypt — ücretsiz)
[win-acme](https://www.win-acme.com/) indirin → `wacs.exe` çalıştırın →
"Create certificate (default settings)" → IIS sitenizi seçin. Sertifika kurulur
ve otomatik yenilenir (zamanlanmış görev oluşturur).

### A7. Güvenlik sıkılaştırma
- Windows Firewall: yalnızca **80, 443, 3389 (RDP)** açık; **3306 ve 4000 kapalı**
  (yalnız localhost kullanılıyor).
- RDP'yi mümkünse kendi sabit IP'nize kısıtlayın.
- Yedekleme: zamanlanmış görevle günlük `mysqldump`:
  ```powershell
  mysqldump -u root -pGUCLU_SIFRE kuyumcu > C:\yedek\kuyumcu_$(Get-Date -Format yyyyMMdd).sql
  ```
  (Güzel Hosting VDS'lerde ayrıca günlük imaj yedeği alır.)

### A8. Test
`https://alanadiniz.com` → giriş ekranı gelmeli; İşletme Kur ile ilk tenant'ı
açın. (İsterseniz test verisi: sunucuda `node scripts/seed.mjs` — API'deki
e-posta/şifreyi script başındaki sabitlerde güncelleyin.)

---

## Bölüm B — Ubuntu 22.04 VPS (alternatif, daha ekonomik)

```bash
# 1) Temel kurulum
apt update && apt upgrade -y
curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && apt install -y nodejs git nginx mysql-server
npm i -g pnpm pm2

# 2) MySQL
mysql_secure_installation
mysql -e "CREATE DATABASE kuyumcu CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci;
          CREATE USER 'kuyumcu'@'localhost' IDENTIFIED BY 'GUCLU_SIFRE';
          GRANT ALL ON kuyumcu.* TO 'kuyumcu'@'localhost'; FLUSH PRIVILEGES;"

# 3) Uygulama
cd /var/www && git clone <repo-adresi> kuyumcu && cd kuyumcu
pnpm install
pnpm --filter @kuyum/core build && pnpm --filter @kuyum/db build
pnpm --filter @kuyum/api build  && pnpm --filter @kuyum/web build
mkdir -p /var/www/kuyumcu/web && cp -r apps/web/dist/* /var/www/kuyumcu/web/

# apps/api/.env dosyasını üretim değerleriyle oluşturun (Bölüm A3'teki gibi,
# kullanıcı: kuyumcu), sonra:
DATABASE_URL='mysql://kuyumcu:GUCLU_SIFRE@localhost:3306/kuyumcu' pnpm --filter @kuyum/db db:migrate

# 4) API servisi (pm2)
pm2 start deploy/ecosystem.config.js && pm2 save && pm2 startup

# 5) nginx + SSL
cp deploy/nginx-kuyumcu.conf /etc/nginx/sites-available/kuyumcu   # alan adını düzenleyin
ln -s /etc/nginx/sites-available/kuyumcu /etc/nginx/sites-enabled/
nginx -t && systemctl reload nginx
apt install -y certbot python3-certbot-nginx && certbot --nginx -d alanadiniz.com

# 6) Güvenlik duvarı
ufw allow OpenSSH && ufw allow 'Nginx Full' && ufw enable
```

---

## Güncelleme prosedürü (her iki ortamda)

```bash
git pull
pnpm install
pnpm --filter @kuyum/core build && pnpm --filter @kuyum/db build
pnpm --filter @kuyum/api build  && pnpm --filter @kuyum/web build
pnpm --filter @kuyum/db db:migrate          # yeni migration varsa
# Windows: nssm restart KuyumcuAPI   |   Linux: pm2 restart kuyumcu-api
# Web statikleri: dist içeriğini site köküne yeniden kopyala (IIS'te gerek yok — site zaten dist'i gösteriyor)
```

## Kontrol listesi

- [ ] VDS/VPS siparişi verildi, IP alındı
- [ ] Alan adı A kaydı IP'ye yönlendi
- [ ] Node 20 + pnpm + MySQL 8 kuruldu
- [ ] `kuyumcu` veritabanı `utf8mb4_turkish_ci` ile oluşturuldu
- [ ] `.env` üretim değerleriyle yazıldı (YENİ secret + şifre)
- [ ] Migration'lar uygulandı
- [ ] API servis olarak çalışıyor (NSSM / pm2) ve boot'ta başlıyor
- [ ] Web statikleri IIS/nginx'ten sunuluyor, `/api` proxy'si çalışıyor
- [ ] SSL kuruldu, http→https yönlendirmesi açık
- [ ] 3306/4000 dışarıya kapalı
- [ ] Günlük mysqldump yedeği zamanlandı
