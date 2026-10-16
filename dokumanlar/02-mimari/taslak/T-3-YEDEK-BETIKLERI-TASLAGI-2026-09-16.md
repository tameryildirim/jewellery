# T-3 — Yedek otomasyonu betik taslakları

> **TASLAK — uygulama T-3 paketi tarafından; bu belge kod değildir.**
> Fenced bloklar tasarım metnidir; hiçbiri çalıştırılmadı, hiçbir betik/kod dosyasına
> yazılmadı. Uygulama T-3'te gerçek dosyalar üretilir ve kanıtla raporlanır (Anayasa md.1).
> Hat: 3-P paralel belge/betik hattı (MÜHÜR 5 KOŞUL 4). Tarih: 2026-09-16.
> Bu tur salt okunur geçti: kod derleme/test/git/docker/migration YOK.

## 0. Bağlayıcı çerçeve ve iki çelişki (önce oku)

**ÇELİŞKİ 1 — betik dizini.** 3-P satırı `dotnet/betikler/` diyor (**Kaynak:**
`dokumanlar/09-uygulama-kayitlari/ham/muhur5.md:13`); paketin bağlayıcı koşulu ise "Betik
konumu tek: mevcut düzen `dotnet/` kökü; `dotnet/betikler/` yeni dizini açılmaz ya da tüm
betikler tek seferde taşınır" diyor (**Kaynak:** `dokumanlar/02-mimari/kalan-paketler/
PAKET-T-3-yedek-otomasyonu-ve-dogrulanmis-geri-yukleme-doc.md`, § Birleşik koşullar md.1 ve
Bertan oyunun 1. koşulu). MÜHÜR 3 "TEK YAYIN YOLU — betikler yalnız dotnet/ kökü" kararıyla
birlikte bu taslak **`dotnet/` kökünü** varsayar. **DOĞRULANMALI:** tek yol Tamer Bey
mührüyle seçilmeli; karar kayda geçmeden yeni dizin açılmaz.

**ÇELİŞKİ 2 — runbook konumu.** Paket `deploy/YEDEK-GERI-YUKLEME.md` diyor (§ Servis ve
dosyalar); MÜHÜR 3 "runbook'lar yalnız `docs/`" diyor. **DOĞRULANMALI** — taslak
`docs/YEDEK-GERI-YUKLEME.md` varsayar.

**Bu belgenin DEĞİŞTİRMEDİĞİ dosyalar:** `dotnet/denetim.sh` (öneriler § 9'da ayrı liste),
`Program.cs`, `Entities.cs`, `KuyumcuDbContext.cs`, `ModulKayit.cs`, `Sozlukler.cs`,
`appsettings*`, `deploy/*`, nginx conf, `index.html`, `app.css`.

## 1. Mevcut betiğin yapısı ve kusurları

**Kaynak:** `dotnet/yedek-al-ve-dogrula.sh:1-111` (elle çalışır).

| # | Bulgu | Kanıt (satır) |
|---|---|---|
| 1 | Docker zorunlu: `my()`, dump ve geri yükleme `docker exec "$KAP"` ile koşar; parola aynı satırlarda argv'de geçiyor (`-p"$SIFRE"`) → `ps aux` ile görünür | `:36`, `:39`, `:76` |
| 2 | **Gömülü varsayılan parola VAR** (değeri bu belgeye yazılmaz) | `:30` |
| 3 | Şifresiz düz `.sql` diske yazılıyor; varsayılan dizin `/tmp` | `:31`, `:34`, `:41` |
| 4 | Saklama, dış kopya, sha256, zamanlayıcı yok; karşılaştırma `python3` heredoc'una bağlı | betik geneli · `:83-106` |
| 5 | Doğrulama ayrı şemaya yapılıyor ve şema iş sonunda düşürülüyor (DOĞRU, korunmalı) | `:32`, `:73`, `:108` |
| 6 | `GROUP_CONCAT` tuzağı çözülmüş; tablo listesi satır satır okunuyor (korunacak ders) | `:45-51` |

**Korunacak çekirdek:** "yedek alınıyor bir iddia, geri yüklenebiliyor bir kanıttır" ilkesi ve
`CHECKSUM TABLE` karşılaştırması (`:44-106`) — yeni betikler yerine geçmez, **çağırır** (§ 8).

**docker-compose.yml:** servis `mysql`, konteyner `kuyumcu-mysql`, imaj `mysql:8.4`, volume
`kuyumcu_mysql_data:/var/lib/mysql`, port 3306 dışa açık
(**Kaynak:** `docker-compose.yml:2-4`, `:10`, `:11-12`, `:20-21`). Dosyada düz metin kök parola
satırı **vardır** (`:7`, healthcheck `:15`) — değer yazılmaz; T-1 sır disiplinine taşınması ayrı iş.

## 2. `dotnet/yedek.sh` TASLAĞI — docker'lı/docker'sız tek betik

Kurallar: parola **argv'de geçmez** (`--defaults-extra-file`), şifresiz ara dosya **diske
düşmez**, anahtar yoksa **fail-closed**.

```bash
#!/usr/bin/env bash
# TASLAK — dotnet/yedek.sh · modlar: tam | artimli | talep | tatbikat
set -euo pipefail
VT="${KUYUMCU_VT:-kuyumcu_net}"                       # T-1 ile TEK değişken adı
YEDEK_DIZIN="${YEDEK_DIZIN:-/var/lib/kuyumcu/yedek}"
YEDEK_ANAHTARI="${YEDEK_ANAHTARI:-}"                  # ALAN anahtarından AYRI (koşul)
SAKLAMA_GUN="${SAKLAMA_GUN:-30}"; MOD="${1:-tam}"
DAMGA="$(date +%Y%m%d-%H%M%S)"                        # Europe/Istanbul

# FAIL-CLOSED 2 — anahtar yok/kısa ya da alan anahtarıyla aynı ise hiç başlama
if [ -z "$YEDEK_ANAHTARI" ] || [ "${#YEDEK_ANAHTARI}" -lt 32 ]; then
  echo "HATA: YEDEK_ANAHTARI yok ya da 32 karakterden kısa; şifresiz yedek üretilmez." >&2
  exit 3
fi
[ "$YEDEK_ANAHTARI" = "${KUYUMCU_ALAN_ANAHTARI:-__yok__}" ] \
  && { echo "HATA: yedek anahtarı alan şifreleme anahtarıyla AYNI olamaz." >&2; exit 4; }

# Kimlik dosyası: parola argv'ye GİRMEZ. Çıkışta içerik boşaltılır; dosya PrivateTmp ile gider.
KIMLIK="$(mktemp)"; chmod 600 "$KIMLIK"
trap 'printf "" > "$KIMLIK"' EXIT
printf '[client]\nuser=%s\npassword=%s\n' \
       "${KUYUMCU_DB_KULLANICI:?}" "${KUYUMCU_DB_SIFRE:?}" > "$KIMLIK"

# YOL ALGILAMA — yerel istemci varsa docker'a hiç dokunma
if command -v mysqldump >/dev/null 2>&1 || command -v mariadb-dump >/dev/null 2>&1; then
  DUMP="$(command -v mysqldump || command -v mariadb-dump)"
  ISTEMCI="$(command -v mysql || command -v mariadb)"
  dump() { "$DUMP" --defaults-extra-file="$KIMLIK" "$@"; }
  sql()  { "$ISTEMCI" --defaults-extra-file="$KIMLIK" -N -e "$1"; }
elif command -v docker >/dev/null 2>&1 && [ -n "${KUYUMCU_KAP:-}" ]; then
  docker cp "$KIMLIK" "$KUYUMCU_KAP":/tmp/.k.cnf     # geriye dönük yol; ÜRETİMDE KULLANILMAZ
  dump() { docker exec -i "$KUYUMCU_KAP" mysqldump --defaults-extra-file=/tmp/.k.cnf "$@"; }
  sql()  { docker exec -i "$KUYUMCU_KAP" mysql  --defaults-extra-file=/tmp/.k.cnf -N -e "$1"; }
else
  echo "HATA: ne yerel mysqldump/mariadb-dump ne de KUYUMCU_KAP+docker bulundu." >&2; exit 5
fi

# --single-transaction yalnız InnoDB'de tutarlıdır → motor taraması (paket § Riskler)
MYISAM="$(sql "SELECT COUNT(*) FROM information_schema.TABLES
               WHERE TABLE_SCHEMA='$VT' AND ENGINE<>'InnoDB';")"
[ "${MYISAM:-0}" = "0" ] || { echo "HATA: InnoDB olmayan tablo var." >&2; exit 6; }

# TAM YEDEK: dump -> gzip -> şifreleme -> tek .age dosyası (ara .sql YOK)
mkdir -p "$YEDEK_DIZIN"; chmod 700 "$YEDEK_DIZIN"
CIKTI="${YEDEK_DIZIN}/${VT}-tam-${DAMGA}.sql.gz.age"
dump --single-transaction --routines --triggers --events \
     --set-gtid-purged=AUTO --databases "$VT" \
  | gzip -9 \
  | age --passphrase --output "$CIKTI"        # parola stdin'den; argv'de değil
# gpg alternatifi: | gpg --batch --symmetric --cipher-algo AES256 \
#                        --passphrase-file "$ANAHTAR_DOSYASI" --output "$CIKTI"
sha256sum "$CIKTI" > "${CIKTI}.sha256"        # macOS karşılığı: shasum -a 256
BOYUT="$(wc -c < "$CIKTI")"                   # yedek_kayitlari.boyut_bayt
```

**Notlar:** `--set-gtid-purged=AUTO` — GTID açıksa dump başına `SET @@GLOBAL.GTID_PURGED`
yazılır, ayrı şemaya geri yüklerken sorun çıkarır → tatbikat yolunda `OFF` kullanılmalı
(**DOĞRULANMALI**). `age` mi `gpg` mi: ikisi de simetrik AES-256 verir; `age` tek ikili, `gpg`
çoğu Linux'ta kurulu — **ekip oyu gerekir**, taslak `age` varsayıp `gpg` yedeğini yazar.
Anahtar dosya olarak tutulursa **yolu** `appsettings`te durur (SIR değil, YOL); dosya
`chmod 600` ve yedek hedefinden **farklı dizin/izin setinde** olur (Mehmet koşulu).
"Önce yedek al" runbook adımı `bash dotnet/yedek.sh tam` çağırır; çıkış ≠ 0 ise T-1 yayın
adımı **durur**. `talep` modu `yedek_talepleri`nde `durum='BEKLIYOR'` satırını okur, `tam`
akışını çağırır, satırı `ISLENDI` + `yedek_kaydi_id` ile günceller — API süreç başlatmaz.

## 3. `dotnet/yedek-dogrula.sh` TASLAĞI — sha256 + gerçek geri yükleme

```bash
#!/usr/bin/env bash
# TASLAK — dotnet/yedek-dogrula.sh <yedek-dosyasi.age>
set -euo pipefail
DOSYA="${1:?yedek dosyası}"; TATBIKAT_VT="${TATBIKAT_VT:-kuyumcu_tatbikat}"
BASLA="$(date +%s)"

sha256sum -c "${DOSYA}.sha256"            # 1) uyuşmazsa set -e ile düşer → çıkış != 0

# 2) geçici şemaya HAM geri yükleme (dotnet ef database update KULLANILMAZ)
sql "DROP DATABASE IF EXISTS \`$TATBIKAT_VT\`;
     CREATE DATABASE \`$TATBIKAT_VT\` CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci;"
age --decrypt --output - "$DOSYA" | gunzip \
  | sed "s/\`${VT}\`/\`${TATBIKAT_VT}\`/g" \
  | grep -v '^CREATE DATABASE' | grep -v '^USE ' \
  | mysql --defaults-extra-file="$KIMLIK" "$TATBIKAT_VT"

# 3) tablo sayısı karşılaştırması
TS_K="$(sql "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA='$VT';")"
TS_H="$(sql "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA='$TATBIKAT_VT';")"
[ "$TS_K" = "$TS_H" ] || { echo "HATA: tablo sayısı uyuşmuyor ($TS_K/$TS_H)" >&2; exit 1; }

# 4) __EFMigrationsHistory teyidi — şema sürümü birebir mi (adet + son MigrationId)
mg() { sql "SELECT COUNT(*), MAX(MigrationId) FROM \`$1\`.\`__EFMigrationsHistory\`;"; }
[ "$(mg "$VT")" = "$(mg "$TATBIKAT_VT")" ] \
  || { echo "HATA: migration geçmişi uyuşmuyor." >&2; exit 1; }
SON_H="$(sql "SELECT MAX(MigrationId) FROM \`$TATBIKAT_VT\`.\`__EFMigrationsHistory\`;")"

# 5) satır bazlı kanıt — MEVCUT betiğin CHECKSUM karşılaştırması ÇAĞRILIR (ikiz yol yok, § 8)
KUYUMCU_HEDEF_VT="$TATBIKAT_VT" bash "$(dirname "$0")/yedek-al-ve-dogrula.sh" --yalniz-karsilastir

# 6) rapor satırı (makine okunur; VERİ DEĞERİ YAZILMAZ)
printf 'TATBIKAT\t%s\ttablo=%s\tmigration=%s\tsure_sn=%s\tsonuc=%s\n' \
       "$(date -Iseconds)" "$TS_H" "$SON_H" "$(( $(date +%s) - BASLA ))" "BASARILI"

# 7) temizlik — kaynak şemaya ASLA yazılmadı; tatbikat şeması düşürülür
sql "DROP DATABASE IF EXISTS \`$TATBIKAT_VT\`;"
```

Rapor satırı `yedek_tatbikatlari`na yazılır (`tablo_sayisi`, `uyusan`, `uyusmayan`, `sonuc`,
`sure_sn`, `rapor` — **yalnız uyuşmayan tablo adları, veri değeri yazılmaz**).
**Kaynak:** PAKET-T-3 § Veri modeli, `yedek_tatbikatlari` satırı.

## 4. Artımlı yedek: binlog mu, 6 saatlik tam dump mu

| Ölçüt | binlog kopyası | 6 saatte bir tam dump |
|---|---|---|
| RPO / Depolama | dakikalar · küçük (yalnız değişim) | bir periyot · tam boyut × günde dört |
| Geri yükleme | tam + binlog replay (PITR prosedürü gerekir) | tek dosya, düz akış |
| Ön koşul / Risk | `log_bin` AÇIK · replay test edilmezse kâğıt üstünde kalır | yok · dump penceresi satış saatine taşabilir |

**Öneri (kuyumcu ölçeği, tek sunucu): binlog kopyası.** Gerekçe: paket artımlıyı zaten
"binlog kopyası" diye tanımlıyor (§ Kapsam, systemd satırı) ve belge deposu (LONGBLOB)
büyüdükçe günde dört tam dump'ın boyutu/süresi sessizce artar (§ Riskler). Biçim: kapanmış
binlog dosyaları şifrelenip off-site'a kopyalanır (`mysqlbinlog --read-from-remote-server
--raw` ya da doğrudan dosya kopyası).

**DOĞRULANMALI (rakam iddiası yok):** (a) `log_bin` açık mı, saklama süresi kaç; (b) gerçek
şemanın tam dump boyutu/süresi — **ölçülmedi**, bu belgede rakam yazılmaz; (c) ölçüm 6 saatlik
tam dump'ı ucuz gösterirse basitlik lehine karar değişebilir → ekip oyu. PITR prosedürünün
kendisi kapsam dışı (§ Dışında: "PITR prosedürü 1.1'e").

## 5. Off-site — Backblaze B2 taslak komutları

**Maliyet satırı (kaynaklı, aynen):** "Yedek depolama — Backblaze B2 (100 GB, S3 uyumlu) |
ALTYAPI | 0.6 USD | AYLIK | 10 kiracı: 2,9 · 50: 0,6 · 100: 0,3 | alt 29 TL (B2 100 GB) /
olası 29 TL / üst 364 TL (Hetzner Object Storage taban) | KAYNAKLI".
**Kaynak:** `dokumanlar/02-mimari/F-8-BIRIM-EKONOMI-2026-09-16.md:15` (fiyat referansı aynı
belge `:133`). Hesap açılışı ve ödeme **Kurucu kalemi** (`F-8-BIRIM-EKONOMI-2026-09-16.md:80`).

```ini
# TASLAK — /etc/kuyumcu/rclone.conf  (chmod 600, sahip: kuyumcu)
# Sır DEĞERLERİ burada DURMAZ; rclone ortam değişkeninden okur:
#   RCLONE_CONFIG_B2_ACCOUNT / RCLONE_CONFIG_B2_KEY → EnvironmentFile=/etc/kuyumcu/yedek.env
[b2]
type = b2
hard_delete = true
```

```bash
# TASLAK — yedek.sh dış kopya adımı
rclone copy "$CIKTI"          "b2:${B2_KOVA}/kuyumcu/" --checksum --no-traverse
rclone copy "${CIKTI}.sha256" "b2:${B2_KOVA}/kuyumcu/" --checksum --no-traverse
rclone check "$CIKTI" "b2:${B2_KOVA}/kuyumcu/" --one-way || exit 7   # FAIL-CLOSED 3
# b2 CLI alternatifi: b2 file upload "$B2_KOVA" "$CIKTI" "kuyumcu/$(basename "$CIKTI")"
```

**30 gün saklama — iki katman.** (1) **Kova (lifecycle):** taslak biçim `b2 bucket update
"$B2_KOVA" --lifecycle-rule '{"daysFromHidingToDeleting":30,"fileNamePrefix":"kuyumcu/"}'` —
**DOĞRULANMALI:** b2 CLI'nin güncel alt komut adı ve JSON alanları sürüme göre değişiyor,
resmî belgeden teyit edilir. (2) **Yerel (betik):** `find "$YEDEK_DIZIN" -name '*.age' -mtime
+"$SAKLAMA_GUN" -print` — **FAIL-CLOSED 4:** son 30 günde `dogrulama='DOGRULANDI'` bir TAM
yedek yoksa liste yalnız yazdırılır, hiçbir dosya silinmez. Silme adımı yalnız üretim
sunucusunda koşar; geliştirme makinesinde TurkReport hook'u silme komutlarını engeller →
betik geliştirme modunda bu adımı atlamalı (**DOĞRULANMALI**).

**Anahtarlar:** `B2_KOVA`, hesap kimliği ve uygulama anahtarı `/etc/kuyumcu/yedek.env`
(`EnvironmentFile`) içinde; `appsettings*`e yazılmaz (MÜHÜR 3 sır disiplini + Mehmet'in "dış
hedef kimliği ikinci sır yolu doğurmasın" koşulu). Yedek şifreleme anahtarı bu `.env`den de
**ayrı** dosyadadır.

## 6. systemd birim + timer TASLAKLARI

Konum `deploy/systemd/` (MÜHÜR 3 TEK YAYIN YOLU); T-1'in `kuyumcu` kullanıcısı ve
`EnvironmentFile` düzenine yaslanır, **ikinci ortam düzeni kurmaz.**

```ini
# TASLAK — deploy/systemd/kuyumcu-yedek-tam.service
[Unit]
Description=Kuyumcu ERP - gecelik tam yedek
After=network-online.target mysql.service
OnFailure=kuyumcu-yedek-bildirim@%n.service
[Service]
Type=oneshot
User=kuyumcu
Group=kuyumcu
EnvironmentFile=/etc/kuyumcu/yedek.env
WorkingDirectory=/opt/kuyumcu
ExecStart=/bin/bash /opt/kuyumcu/dotnet/yedek.sh tam
NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
PrivateTmp=true
ReadWritePaths=/var/lib/kuyumcu/yedek
UMask=0077
```
Diğer üç `.service` aynı kalıptadır; yalnız `ExecStart` modu değişir (`artimli`, `talep`,
`tatbikat`).

```ini
# TASLAK — timer'lar (her biri kendi .timer dosyası; hepsinde [Install] WantedBy=timers.target)
# kuyumcu-yedek-tam.timer      → OnCalendar=*-*-* 03:15:00 · Persistent=true · RandomizedDelaySec=300
# kuyumcu-yedek-artimli.timer  → OnCalendar=*-*-* 00,06,12,18:45:00 · Persistent=true
# kuyumcu-yedek-tatbikat.timer → OnCalendar=*-*-01 04:30:00 · Persistent=true
# kuyumcu-yedek-talep.timer    → OnBootSec=2min · OnUnitActiveSec=60s
[Timer]
OnCalendar=*-*-* 03:15:00
Persistent=true
RandomizedDelaySec=300
Unit=kuyumcu-yedek-tam.service
[Install]
WantedBy=timers.target
```

`kuyumcu-yedek-bildirim@.service` aynı kalıptadır (`Type=oneshot`, `User=kuyumcu`, aynı
`EnvironmentFile`, `ExecStart=... yedek.sh bildirim %i`); bildirim kanalı (e-posta/webhook)
seçilmedi — **DOĞRULANMALI**. `Persistent=true`: sunucu kapalıyken kaçan koşu açılışta telafi
edilir. Saat dilimi `Europe/Istanbul` varsayılır (**DOĞRULANMALI**). Zamanlama tek yerdedir:
uygulamada ikinci zamanlayıcı açılmaz (PAKET-T-3 § İş kuralları).

**Windows Görev Zamanlayıcı notu:** systemd yalnız Linux üretimde koşar, macOS/Windows'ta
doğrulanamaz (§ Riskler). Karşılığı: `schtasks /Create /TN "Kuyumcu-Yedek-Tam" /TR "bash
C:\kuyumcu\dotnet\yedek.sh tam" /SC DAILY /ST 03:15 /RU kuyumcu`; `Persistent=true` karşılığı
görev özelliklerindeki "Run task as soon as possible after a scheduled start is missed".
**DOĞRULANMALI:** Windows üretim hedefi var mı; yoksa bu not bilgi amaçlıdır.

## 7. Aylık geri-yükleme tatbikatı — akış ve rapor şablonu

**Akış:** timer tetikler → en yeni TAM yedek seçilir → `yedek-dogrula.sh` koşar (sha256 →
`kuyumcu_tatbikat` şemasına HAM restore → tablo sayısı → `__EFMigrationsHistory` → CHECKSUM) →
sonuç `yedek_tatbikatlari`na yazılır → tatbikat şeması **düşürülür** → başarısızsa `OnFailure`
bildirimi + ekranda kırmızı bant. Kaynak şemaya hiçbir aşamada yazılmaz.

```markdown
# Geri-yükleme tatbikatı — <YYYY-AA>
- Yedek dosyası: <ad> · alınma: <ISO8601> · boyut: <bayt — ÖLÇÜLDÜ>
- sha256 doğrulama: <GEÇTİ / KALDI>
- Tablo sayısı kaynak/tatbikat: <n> / <n>
- Son migration kaynak/tatbikat: <MigrationId> / <MigrationId>
- CHECKSUM: uyuşan <n> · uyuşmayan <n> · uyuşmayan tablo adları: <liste — VERİ DEĞERİ YOK>
- **RPO ölçümü:** son başarılı yedek ile olay anı arasındaki fark = <ÖLÇÜLDÜ, dk>
- **RTO ölçümü:** çözme + yükleme + doğrulama toplam süresi = <ÖLÇÜLDÜ, sn>
- Taahhüt karşılaştırması: hedef runbook'ta yazılı; ölçülen hedefi aşıyorsa BULGU açılır.
- Sonuç: BASARILI / BASARISIZ · Tatbikat şeması düşürüldü: EVET / HAYIR
- Koşan birim: kuyumcu-yedek-tatbikat.service · çıkış kodu: <n>
```

**Rakam uydurulmaz (md.1):** RPO/RTO alanları yalnız ölçüm çıktısıyla dolar. Paketteki
"RPO ≤ 6 saat / RTO ≤ 2 saat" bir **taahhüt hedefidir** (§ Servis ve dosyalar, runbook
satırı), ölçüm sonucu değildir; ikisi raporda ayrı satırlardadır.

## 8. Mevcut `yedek-al-ve-dogrula.sh` ile ilişki — tek yol ilkesi

**Karar (öneri): GENİŞLETİR, yerine geçmez.** Bağlayıcı koşulun lafzı: "yedek-al-ve-dogrula.sh'ın
docker'sızlaştırılması ile yeni yedek.sh arasında **ikiz dump yolu doğmasın**: dump+doğrulama
tek fonksiyon kümesinde toplansın, yedek.sh onu çağırsın ... denetim.sh sayacıyla zorlansın."
**Kaynak:** PAKET-T-3 § Birleşik koşullar md.2 (ve Bertan oyunun 2. koşulu).

1. `dotnet/yedek-ortak.sh` — source edilebilir kitaplık: `kimlik_dosyasi()`, `dump()`, `sql()`,
   `parmak_izi()`, `karsilastir()`. Mevcut betiğin tablo listeleme mantığı (`:52-64`) ve
   karşılaştırması (`:83-106`) buraya taşınır, davranış aynı kalır.
2. `dotnet/yedek-al-ve-dogrula.sh` ince kabuğa iner: ortağı source eder; docker bağımlılığı ve
   `:30` gömülü parolası **kaldırılır**; elle çalıştırma arayüzü korunur, `--yalniz-karsilastir`
   bayrağı eklenir.
3. `yedek.sh` ve `yedek-dogrula.sh` aynı ortağı çağırır; ikinci dump çağrısı yazılmaz (D4 sayacı).

**python3 heredoc'u** ortak kitaplıkta kalabilir; saf `awk`/`diff` ile de yazılabilir —
bağımlılık azaltma lehine **DOĞRULANMALI / ekip oyu**. Taslak çalışan mantığın yeniden
yazılmamasını önerir.

## 9. DOĞRULANMALI listesi ve uygulayıcıya notlar

**DOĞRULANMALI (varsayım yapılmadı):**
1. Betik dizini `dotnet/` kökü mü `dotnet/betikler/` mi (§ 0 ÇELİŞKİ 1); runbook yolu `docs/`
   mi `deploy/` mi (§ 0 ÇELİŞKİ 2) — ikisi de mühür ister.
2. Üretimdeki MySQL sürümü ve sunucudaki `mysqldump` istemci sürümü uyumu (geliştirmede
   `docker-compose.yml:3` 8.4; üretim ayrı — PAKET-T-3 § Açık sorular son maddesi).
3. `log_bin` açık mı, binlog saklama süresi kaç (§ 4 kararının temeli); GTID açık mı
   (`--set-gtid-purged` davranışı buna bağlı).
4. Şemada InnoDB olmayan tablo var mı — Faz A'da bir kez elle de koşulmalı.
5. `age` mi `gpg` mi — ekip oyu; sunucuda hangisi kurulu.
6. Tam dump boyutu ve süresi — **ölçülmedi**; LONGBLOB belge deposu büyümesi belirleyecek.
7. B2 lifecycle komutunun güncel sözdizimi (CLI sürümüne bağlı).
8. `OnFailure` bildirim kanalı (e-posta/webhook) seçilmedi; sunucu saat dilimi
   `Europe/Istanbul` mı; Windows üretim hedefi var mı.
9. Yedek anahtarının sunucu dışı kopyası nerede duracak — Kurucu kalemi; kayıp = tüm
   yedeklerin kaybı (§ Riskler md.1).
10. Elle yedekte 60 sn kuyruk gecikmesi kabul mü (§ Açık sorular).

**Uygulayıcıya notlar:** **MFA kapısı** — 45 bitmeden gerçek üretim yayını yok (MÜHÜR 3);
betikler hazırlık ortamında doğrulanır, "timer üretimde çalışıyor" iddiası ancak üretimde
kanıtlanır. **Migration üretimi bu hatta YASAK** (MÜHÜR 5 KOŞUL 5): üç tablonun migration'ı
yalnız seri kod hattında, tek elden üretilir. Canlı doğrulamaya kapanış kanıtı eklenmeli:
tatbikat şeması düşürüldü + test kiracısı silindi (tenants = 5) — Aşkın koşulu, KOŞUL 12.
`ps aux` kanıtı: dump koşarken parolanın süreç listesinde **görünmediği** komut çıktısıyla
kanıtlanır, tasarım metni yeterli değil. Ekranda "bu, tüm kiracıları kapsayan sistem
yedeğidir" uyarısı zorunlu. Tatbikat raporunun "yalnız tablo adı, veri değeri yazılmaz"
kuralı somut test + mutasyon kanıtıyla doğrulanır. (Son üçü Mehmet koşulları.)

**`denetim.sh`'a ÖNERİLEN sayaçlar — bu belge `denetim.sh`'ı DEĞİŞTİRMEZ, yalnız önerir:**

| # | Sayaç | Beklenen |
|---|---|---|
| D1 | `Kuyumcu.Api` altında `Process.Start` / `ProcessStartInfo` | 0 |
| D2 | Betiklerde dump çıktısının şifrelenmeden `> *.sql` yönlendirilmesi | 0 |
| D3 | Betiklerde `-p"` / `--password=` kalıbı (parola argv'de) | 0 |
| D4 | Dump çağrısının geçtiği betik dosyası sayısı (ikiz yol kapısı) | 1 (`yedek-ortak.sh`) |
| D5 | `appsettings*` içinde B2 kimliği / yedek anahtarı benzeri **değer** | 0 (yalnız YOL) |
| D6 | Yedek betiklerinde `docker exec` satırı (geriye dönük blok dışında) | 0 |

## 10. API sözleşmesi taslağı — uygulama içi "Yedekler" ekranı (kod değil)

Salt okunur, `SAHIP` rolü, **dosya indirme ucu YOK**. **Kaynak:** PAKET-T-3 § Uçlar ve
§ İş kuralları.

```
GET /api/v1/yedekler/ozet            → 200 (SAHIP) / 403 (diğer roller)
{
  "sonYedek": { "tur":"TAM", "zaman":"<ISO8601>", "boyutBayt":<int>,
                "durum":"BASARILI|BASARISIZ" },
  "dogrulama":        "BEKLIYOR|DOGRULANDI|UYUSMADI",
  "dogrulamaTarih":   "<ISO8601|null>",
  "disKopya":         "YOK|KOPYALANDI|HATA",
  "saklananKosu":     <int>,
  "rpoDakika":        <int>,   // son artımlıdan bu yana ÖLÇÜLEN dakika
  "rtoDakikaTaahhut": <int>    // yapılandırmadan; ÖLÇÜM DEĞİL, taahhüt
}

GET /api/v1/yedekler?limit=50   → [{ "id","tur","baslangic","bitis","durum","boyutBayt",
       "sifreli","disKopya","dogrulama","tabloSayisi" }]   // dosya ADI ve YOL DÖNMEZ
GET /api/v1/yedekler/tatbikatlar→ [{ "tarih","tabloSayisi","uyusan","uyusmayan","sonuc",
       "sureSn" }]                                          // rapor yalnız tablo ADI taşır
POST  /api/v1/yedekler/talep      → 201 | 400 (gerekçe < 20) | 429 (bekleyen talep var)
PATCH /api/v1/yedekler/talep/{id} → 200 (durum=IPTAL) | 400 (işlenmiş)  // hard DELETE YOK
YOK:  GET /api/v1/yedekler/{id}/indir → 404 (bilinçli; dosya TÜM kiracıların verisini taşır)
```

API **hiçbir koşulda süreç/komut çalıştırmaz**; elle yedek yalnız `yedek_talepleri` satırıdır,
betiğin `talep` modu kuyruğu tüketir. Liste okuması `ListeOkumaKaydet`, talep açma/iptal
`YazmaKaydet` ile denetim izine yazılır.

*Bu belge taslaktır; kod değildir. Uygulama T-3 paketinde, yukarıdaki DOĞRULANMALI kalemler
kapatıldıktan sonra gerçek dosyalar üretilir, koşturulur ve kanıtla raporlanır.*
