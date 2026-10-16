# RUNBOOK — Şema yükseltme (migration bundle)

> Paket T-1 (+F-6), Faz A. Tek belge kökü: `docs/` (MÜHÜR 3 / K6).
> Bu yordam **zorunludur**: uygulama şemayı KENDİ DEĞİŞTİRMEZ.

## Neden elle?

`Database.Migrate()` üretimde **kasıtlı olarak kapalıdır** (FAIL-CLOSED 5):

* Yedeksiz bir şema değişimi **geri alınamaz**.
* Birden çok süreç (api + servis + konteyner) aynı anda açılırsa migration
  yarışır ve yarım uygulanmış şema bırakır.

Bu yüzden bekleyen migration varsa üretim süreci **hizmet vermez**: çıkış
kodu `3` ile durur ve `GET /saglik/hazir` `503 {"hazir":false,"kod":"SEMA_ESKI"}`
döner. `GET /saglik` yine `200 Healthy` döner — **canlılık** ile **hazırlık**
kasıtlı olarak ayrıdır.

## 0. Önkoşul

```bash
# Yayın çıktısı ve bundle üretilmiş olmalı
./dotnet/yayin-al.sh linux-x64          # → dotnet/yayin/linux-x64/kuyumcu-migrate
```

## 1. Durumu ölç (SAYIYI TAHMİN ETME)

Migration sayısı **hiçbir belgeye sabit yazılmaz**; her zaman komut
çıktısından okunur (MÜHÜR 3, sayısal iddia yasağı).

```bash
cd dotnet
dotnet ef migrations list --project Kuyumcu.Api          # tanımlı olanlar
curl -s http://127.0.0.1:5000/saglik/hazir               # hazir: true/false
```

```sql
-- uygulanmış olanlar (şema adı KUYUMCU_VT değişkeninden)
SELECT COUNT(*) FROM __EFMigrationsHistory;
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 3;
```

## 2. ÖNCE YEDEK — atlanamaz

```bash
./dotnet/yedek-al-ve-dogrula.sh
echo "cikis kodu: $?"     # 0 DEĞİLSE DURUN. Yedeksiz migration YASAK.
```

Betik yedeği alır **ve** CHECKSUM'la geri yüklenebilirliğini doğrular.
Çıkış kodu 0 değilse bu runbook **burada biter**; önce yedek sorunu çözülür.

## 3. Servisi durdur

```bash
sudo systemctl stop kuyumcu          # Linux
# Windows: sc.exe stop kuyumcu-erp
# Docker : docker compose -f docker-compose.uretim.yml stop api
```

Uygulama açıkken şema değiştirmek, eski modelle çalışan isteklerin yarı
yolda kalmasına yol açar.

## 4. Bundle'ı çalıştır

```bash
cd /opt/kuyumcu/yayin
set -a; . /etc/kuyumcu/.env; set +a      # sırlar ortamdan; komut satırına YAZILMAZ
./kuyumcu-migrate --connection "$ConnectionStrings__Kuyumcu"
echo "cikis kodu: $?"
```

> Parolayı komut satırına açık yazmayın — `ps` çıktısında görünür.
> Bundle self-contained'dir: hedef makinede .NET SDK **gerekmez**.

## 5. Servisi başlat ve DOĞRULA

```bash
sudo systemctl start kuyumcu
curl -s -i http://127.0.0.1:5000/saglik        # 200 Healthy
curl -s -i http://127.0.0.1:5000/saglik/hazir  # 200 {"hazir":true,"kod":"HAZIR"}
journalctl -u kuyumcu -t kuyumcu-erp -n 50 --no-pager
```

`hazir:false` dönüyorsa migration uygulanmamıştır; 4. adımın çıkış kodunu
okuyun. **"Herhalde olmuştur" demeyin** (Anayasa md. 1).

## 6. Geri dönüş (rollback)

Migration başarısız olduysa **şemayı elle düzeltmeye çalışmayın**:

1. Servis kapalı kalsın.
2. 2. adımdaki yedeği geri yükleyin (`dotnet/yedek-al-ve-dogrula.sh` içindeki
   geri yükleme yordamı; CHECKSUM doğrulanır).
3. Eski yayın klasörüne dönün (`yayin/<rid>` sürüm kopyası).
4. Durumu kayda geçirin: hangi migration, hangi hata, hangi yedek.

## Sık karşılaşılan durumlar

| Belirti | Sebep | Çözüm |
|---|---|---|
| Servis `activating` → `failed`, çıkış kodu 3 | Bekleyen migration | Bu runbook, 2→5 |
| `systemctl` sürekli yeniden başlatmıyor | `RestartPreventExitStatus=3` — **kasıtlı** | Runbook'u çalıştırın |
| `/saglik` 200 ama `/saglik/hazir` 503 `VERITABANI_YOK` | DB kapalı / bağlantı yanlış | MySQL ve `.env` bağlantısı |
| Uygulama hiç açılmıyor, log'da "varsayılan/örnek değerde" | Fail-closed sır kapısı | `dotnet/kurulum.sh` |
