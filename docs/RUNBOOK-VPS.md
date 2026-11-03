# RUNBOOK — ByGatech VPS yayını (DeHost · 5.10.220.150)

> 02.11.2026 kuruldu. Bu belge **tek doğruluk kaynağıdır**: sunucuda ne var, nasıl
> yayınlanır, nasıl geri alınır. Sır/parola YAZILMAZ (hepsi `/etc/kuyumcu/.env`, 600).

## 1. Kimlik

| | |
|---|---|
| Sağlayıcı / paket | DeHost — TR VPS-4GB (E5 v4), 2 vCPU · 4 GB · 40 GB NVMe · 1 sabit IPv4 · Türkiye |
| Sipariş | #20225 · 3 aylık 467,96 ₺ (KDV dahil) · sonraki ödeme **02/02/2027** (kurucu) · panel: my.dehost.com.tr |
| IP (sabit) | **5.10.220.150** — Garanti/Kuveyt Türk portallarına bu adres yazılır |
| Adres (geçici) | **https://5-10-220-150.sslip.io** — Let's Encrypt, otomatik yenileme (certbot.timer) |
| Alan adı | henüz yok; bygatech.com.tr / bygatech.tr boş (02.11). Alınınca § 7 |
| İşletim sistemi | Ubuntu 24.04.1 LTS · saat dilimi Europe/Istanbul · unattended-upgrades açık |
| SSH | yalnız anahtar (`~/.ssh/bygatech_vps`, Mac'te) · parola girişi KAPALI (`/etc/ssh/sshd_config.d/00-bygatech.conf`) · fail2ban sshd |
| Güvenlik duvarı | ufw: 22, 80, 443 açık; gerisi kapalı. 3306 yalnız 127.0.0.1 |

## 2. Yerleşim

```
/opt/kuyumcu/yayin/        self-contained linux-x64 yayın (Kuyumcu.Api + kuyumcu-migrate)   ← dotnet/yayin-al.sh linux-x64
/opt/kuyumcu/dotnet/       kurulum.sh · yedek-al-ve-dogrula.sh · yedek-ortak.sh
/opt/kuyumcu/deploy/       nginx-kuyumcu.conf · docker-compose.mysql.yml · systemd/*
/etc/kuyumcu/.env          sırlar (kurulum.sh üretti; alan şifreleme anahtarı Mac canlısıyla AYNI — demo verisi okunsun diye)
/var/lib/kuyumcu/yedek     şifreli yedekler (.sql.gz.gpg) · yedek-dis/ dış kopya dizini (Mac her gece çeker)
/etc/nginx/sites-available/kuyumcu   ters vekil + TLS (http2 `listen` biçimi; nginx 1.24)
```

Servisler: `kuyumcu.service` (uygulama, 127.0.0.1:5000, Type=notify) · `bygatech-mysql` (docker, mysql:8.4, bellek sınırı 1800 MB) · nginx · docker · fail2ban.
Zamanlayıcılar: `kuyumcu-yedek-tam.timer` (03:15 + 09/15/21:15) · `kuyumcu-yedek-talep.timer` · `kuyumcu-yedek-tatbikat.timer` (aylık geri yükleme provası) · `certbot.timer`.
Veritabanı: `kuyumcu_net` — 02.11'de Mac canlısından (demo verisi) içe aktarıldı (107 tablo, 128 migration).

## 3. Yeni sürüm yayınlama (Mac'ten)

```bash
cd ~/ByGatech-yerel/jewellery/dotnet && ./yayin-al.sh linux-x64            # derle + self-contained yayın + migrate bundle
rsync -az --delete -e "ssh -i ~/.ssh/bygatech_vps" yayin/linux-x64/ root@5.10.220.150:/opt/kuyumcu/yayin/
ssh -i ~/.ssh/bygatech_vps root@5.10.220.150 'chown -R kuyumcu:kuyumcu /opt/kuyumcu/yayin && systemctl restart kuyumcu && sleep 6 && systemctl is-active kuyumcu && curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:5000/saglik'
```

**Bekleyen migration varsa** uygulama çıkış kodu 3 ile DURUR (fail-closed 5). Sıra: önce yedek
(`systemctl start kuyumcu-yedek-tam`), sonra `docs/RUNBOOK-SEMA-YUKSELTME.md` (kuyumcu-migrate), sonra restart.

Kapı (değişmez): Mac'te build 0 hata · tam süit · denetim.sh · yayından sonra `curl https://5-10-220-150.sslip.io/` 200 + tarayıcıda 1280/375 ekran görüntüsü.

## 4. Yedek / geri yükleme

- Sunucu: her koşu `dump → gzip → gpg` (anahtar `Yedek__SifrelemeAnahtari`, alan anahtarından ayrı), sha256 kaydı `yedek_kayitlari`, dış kopya `/var/lib/kuyumcu/yedek-dis`.
- Mac: `betikler/vps-yedek-cek.sh` dış kopyayı `~/.kuyumcu-yedek/vps/`e çeker; launchd `com.bygatech.vps-yedek` (04:10) — **kurucu kurar** (plist betikler/'de). Sunucunun yedek anahtarının kopyası `~/.kuyumcu/vps-yedek-anahtari` (600).
- 02.11 kanıtı: tam yedek 29 MB · tatbikat **DOĞRULANDI** 107/107 (3 tablo dump sonrası yazıldığı için değişken) · Mac'te gpg ile çözüldü.
- Linux tuzağı (02.11 düzeltildi): longblob dump'ı GNU grep'i "binary" moda sokup satır düşürüyordu → `--hex-blob` + `grep -a`.
- Geri yükleme: `docs/YEDEK-GERI-YUKLEME.md`.

## 4a. Kur kaynakları

Üretim varsayılanı `Kur:SozlesmesizKaynaklarAcik=false` (sözleşmesiz dış kaynak kendiliğinden açılmaz — K13).
Mac canlısı gibi sunucuda da `.env` satırıyla açıldı: `Kur__SozlesmesizKaynaklarAcik=true` (02.11, kurucu "neden kaynak bağlamadık").
TRUNCGIL sunucudan ERİŞİLEMİYOR (15 sn zaman aşımı; Mac'ten 200 — veri merkezi IP'lerini engelliyor olabilir) →
o kaynağı seçen demo kiracı ALTINKAYNAK'a alındı; Altınkaynak ve Harem sunucudan 0,1–0,2 sn'de yanıt veriyor.
Satışta "fiyat kaynağı izinli" DENMEZ (sözleşme yok).

## 5. Günlükler

`journalctl -u kuyumcu -f` · `journalctl -u kuyumcu-yedek-tam` · `docker logs bygatech-mysql` · `/var/log/nginx/*.log` · `fail2ban-client status sshd`.

## 6. Kaynak kullanımı (02.11, boşta)

Bellek 1,9 / 3,9 GB (uygulama 378 MB, MySQL ~1,05 GB) · disk 7,7 / 38 GB. 8 GB'a geçiş DeHost panelinden paket yükseltme (veri taşınmaz).

## 7. Alan adı gelince

1. DNS A kaydı → 5.10.220.150 (TTL kısa).
2. `certbot certonly --webroot -w /var/www/certbot -d <alan>`; nginx `server_name` + sertifika yolları; `nginx -t && systemctl reload nginx`.
3. `bash /opt/kuyumcu/dotnet/kurulum.sh --hedef /etc/kuyumcu/.env --kisayol --adres https://<alan> --kisayol-dizin /opt/kuyumcu && systemctl restart kuyumcu`.
4. Kuveyt Türk dönüş adresi `https://<alan>/banka/kuveytturk-donus`; Garanti IP değişmez.

## 8. Kurucuya kalanlar

- Garanti portalı → IPs talebi 02.11 17:42 gönderildi (151.135.190.195 → **5.10.220.150**, Edit Pending Approval) → onay e-postası beklenir. 17:53 sunucudan ilk deneme yapıldı: Garanti test hesabı + bağlantı kayıtlı (kiracı Altın Çarşı), yanıt "Client IP does not exist in provided Portal Application IPs" (beklenen; onay gelince Hesabı Düzenle → Şimdi dene).
  **03.11 08:3x — IP ONAYI GELDİ** (apistore e-postası "Your changes have now been approved"). Sunucudaki çekici 2 dk'da bir yeniden deniyor; son yanıt artık IP hatası DEĞİL: **"Vermiş olduğunuz clientId-consentId bilgileri hatalıdır (kod 4)"** → istek Garanti iş mantığına ulaşıyor; consentId belge örneği olduğu için reddediliyor. Kalan: kurucu internet şubesinde Elektronik Hesap Özeti başvurusu → gerçek consentId → /banka Garanti test → Hesabı Düzenle. Ayrıca Shared Secret yenilenmeli (ekran görüntüsü paylaşıldı).
- launchd `com.bygatech.vps-yedek` kurulumu (§ 4).
- Alan adı satın alma (§ 7). GitHub depo adı.
- **Sunucuda yalnız demo verisi.** Gerçek müşteri verisi için şirket + KVKK/VERBİS + avukat görüşü.
