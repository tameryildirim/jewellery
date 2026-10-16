/* ══════════════════════════════════════════════════════════════════════════
 * ⛔ BU DOSYA ÜRETİM YAYIN YOLU DEĞİLDİR — KULLANMAYIN (Paket T-1, MÜHÜR 3).
 *
 * Kaldırılan Node/NestJS yığınından kalma PM2 tanımıdır (dist/main.js artık
 * YOKTUR). Üretim yayını TEK YOLDAN yapılır:
 *   • Linux  : deploy/systemd/kuyumcu.service
 *   • Windows: deploy/kuyumcu-service-kur.ps1
 *   • Docker : docker-compose.uretim.yml
 * Rehber: docs/DEPLOY.md · Şema: docs/RUNBOOK-SEMA-YUKSELTME.md
 *
 * Dosyanın SİLİNMESİ kurucunun kararına bırakıldı (izinsiz silme yasağı).
 * ═════════════════════════════════════════════════════════════════════════ */

/**
 * (TARİHSEL) PM2 süreç tanımı — API'yi kalıcı servis olarak çalıştırır.
 *
 * Kullanım (repo kökünden):
 *   pm2 start deploy/ecosystem.config.js
 *   pm2 save
 *   Linux:   pm2 startup            (boot'ta otomatik başlat)
 *   Windows: pm2-installer veya NSSM tercih edin (rehbere bakın)
 *
 * .env dosyası apps/api/.env konumundan okunur (cwd oraya ayarlı).
 */
module.exports = {
  apps: [
    {
      name: 'kuyumcu-api',
      cwd: './apps/api',
      script: 'dist/main.js',
      instances: 1,
      autorestart: true,
      max_memory_restart: '512M',
      env: {
        NODE_ENV: 'production',
        PORT: 4000,
      },
      out_file: './logs/api-out.log',
      error_file: './logs/api-err.log',
      merge_logs: true,
      time: true,
    },
  ],
}
