#!/usr/bin/env bash
# ═════════════════════════════════════════════════════════════════════════════
# YEDEK ORTAK KİTAPLIĞI — paket T-3 Faz A (MÜHÜR 6 / K3 "tek yol")
#
# Bu dosya ÇALIŞTIRILMAZ, `source` EDİLİR. Tek giriş betiği
# dotnet/yedek-al-ve-dogrula.sh'tır; ikinci bir yedek betiği AÇILMAZ.
# Dump çağrısı TÜM depoda TEK YERDE, aşağıdaki dump_akisi() içindedir
# (denetim.sh T3-4 sayacı bunu zorlar).
#
# TASARIM KURALLARI (mühürlü):
#   • Parola ARGV'de GEÇMEZ → --defaults-extra-file (chmod 600).
#     Kanıt: yedek koşarken `ps aux` çıktısında parola görünmez.
#   • Şifresiz ara dosya YOKTUR: dump -> gzip -> gpg tek boru hattıdır,
#     diske yalnız .sql.gz.gpg iner.
#   • Anahtar: Yedek__SifrelemeAnahtari ortam değişkeni (T-1 sır hattı).
#     Alan şifreleme anahtarıyla AYNI olamaz (tek nokta çöküşü).
#   • Docker'lı ve docker'sız yol TEK betikte algılanır; iki betik olmaz.
# ═════════════════════════════════════════════════════════════════════════════

# ── Ortam ────────────────────────────────────────────────────────────────────
YEDEK_VT="${KUYUMCU_VT:-kuyumcu_net}"
YEDEK_KAP="${KUYUMCU_KAP:-kuyumcu-mysql}"
YEDEK_KULLANICI="${KUYUMCU_DB_KULLANICI:-root}"
YEDEK_SUNUCU="${KUYUMCU_DB_SUNUCU:-127.0.0.1}"
YEDEK_PORT="${KUYUMCU_DB_PORT:-3306}"
YEDEK_DIZIN="${YEDEK_DIZIN:-/var/lib/kuyumcu/yedek}"
YEDEK_DIS_HEDEF="${YEDEK_DIS_HEDEF:-}"
YEDEK_SAKLAMA_GUN="${YEDEK_SAKLAMA_GUN:-30}"
YEDEK_SAKLAMA_ETKIN="${YEDEK_SAKLAMA_ETKIN:-0}"
YEDEK_TATBIKAT_SEMA="${YEDEK_TATBIKAT_SEMA:-${YEDEK_VT}_tatbikat}"
YEDEK_CNF=""
YEDEK_KAP_CNF="/tmp/.kuyumcu-yedek-$$.cnf"
YEDEK_DOCKER=0

hata_ver() { echo "  x $*" >&2; }
bilgi()    { echo "  · $*"; }

# ── Kimlik dosyası: parola ARGV'ye GİRMEZ ───────────────────────────────────
# KUYUMCU_DB_SIFRE ortam değişkeninden okunur (systemd EnvironmentFile ya da
# geliştirmede kabuk). Betikte GÖMÜLÜ VARSAYILAN PAROLA YOKTUR — eskiden
# yedek-al-ve-dogrula.sh:30 satırında vardı, kaldırıldı (Mehmet veto koşulu).
kimlik_kur() {
  if [ -z "${KUYUMCU_DB_SIFRE:-}" ]; then
    hata_ver "KUYUMCU_DB_SIFRE tanımlı değil. Veritabanı parolası ortam değişkeninden gelir; betiğe yazılmaz."
    return 1
  fi
  YEDEK_CNF="$(mktemp "${TMPDIR:-/tmp}/.kuyumcu-yedek-XXXXXX")"
  chmod 600 "$YEDEK_CNF"
  {
    printf '[client]\nuser=%s\npassword=%s\n' "$YEDEK_KULLANICI" "$KUYUMCU_DB_SIFRE"
    printf '[mysqldump]\nuser=%s\npassword=%s\n' "$YEDEK_KULLANICI" "$KUYUMCU_DB_SIFRE"
  } > "$YEDEK_CNF"

  # Yol algılama: yerel istemci varsa docker'a HİÇ dokunma.
  if command -v mysqldump >/dev/null 2>&1 && command -v mysql >/dev/null 2>&1; then
    YEDEK_DOCKER=0
    bilgi "yol: yerel mysql istemcisi"
  elif command -v docker >/dev/null 2>&1 && docker ps --format '{{.Names}}' 2>/dev/null | grep -qx "$YEDEK_KAP"; then
    YEDEK_DOCKER=1
    # Kimlik dosyası kabın İÇİNE stdin ile kopyalanır: parola ne konak ne kap
    # tarafında argv'ye girer.
    docker exec -i "$YEDEK_KAP" sh -c "cat > $YEDEK_KAP_CNF && chmod 600 $YEDEK_KAP_CNF" < "$YEDEK_CNF"
    bilgi "yol: docker kabı ($YEDEK_KAP)"
  else
    hata_ver "Ne yerel mysql istemcisi ne de '$YEDEK_KAP' kabı bulundu."
    return 1
  fi
  trap kimlik_temizle EXIT
  return 0
}

kimlik_temizle() {
  if [ -n "$YEDEK_CNF" ] && [ -f "$YEDEK_CNF" ]; then
    printf '' > "$YEDEK_CNF"
    command rm -f "$YEDEK_CNF"
  fi
  if [ "$YEDEK_DOCKER" = "1" ]; then
    docker exec "$YEDEK_KAP" sh -c "rm -f $YEDEK_KAP_CNF" 2>/dev/null
  fi
  return 0
}

# ── SQL çalıştırma (tek nokta) ──────────────────────────────────────────────
# ⚠ STDIN /dev/null'a BAĞLANIR ve bu hayati: `docker exec -i` açık stdin'i
#   YUTAR. Bu redirect yokken parmak_izi() döngüsünün beslendiği tablo listesi
#   ilk çağrıda tükeniyor ve tatbikat 54 tablo yerine 1 tablo karşılaştırıp
#   "DOĞRULANDI" diyordu (2026-09-17 ölçümü — sahte yeşil, Anayasa md.1).
my() {   # $1 = SQL, $2 = (isteğe bağlı) şema
  local sema="${2:-}"
  if [ "$YEDEK_DOCKER" = "1" ]; then
    docker exec -i "$YEDEK_KAP" mysql "--defaults-extra-file=$YEDEK_KAP_CNF" -N ${sema:+"$sema"} -e "$1" 2>/dev/null </dev/null
  else
    mysql "--defaults-extra-file=$YEDEK_CNF" -h "$YEDEK_SUNUCU" -P "$YEDEK_PORT" -N ${sema:+"$sema"} -e "$1" 2>/dev/null </dev/null
  fi
}

# SQL akışını bir şemaya yükler (geri yükleme yolu; stdin'den okur).
my_akis() {   # $1 = hedef şema
  if [ "$YEDEK_DOCKER" = "1" ]; then
    docker exec -i "$YEDEK_KAP" mysql "--defaults-extra-file=$YEDEK_KAP_CNF" "$1" 2>/dev/null
  else
    mysql "--defaults-extra-file=$YEDEK_CNF" -h "$YEDEK_SUNUCU" -P "$YEDEK_PORT" "$1" 2>/dev/null
  fi
}

# ── TEK DUMP ÇAĞRI NOKTASI (denetim.sh T3-4: bu dosyada 1, başka yerde 0) ────
# Çıktı STDOUT'a akar; çağıran boru hattına bağlar. Diske düz .sql YAZILMAZ.
# --hex-blob (VPS 02.11 ölçümü): longblob (belgeler.icerik) düz kaçışla yazılınca
# Linux GNU grep geri yükleme borusunda akışı "binary" sayıp o satırları düşürdü;
# tatbikat belgeler tablosunu GERÇEK uyuşmazlık diye kırmızı verdi. Hex yazım
# dump'ı salt ASCII tutar; macOS BSD grep'te görünmeyen bu tuzak kapanır.
dump_akisi() {
  if [ "$YEDEK_DOCKER" = "1" ]; then
    docker exec -i "$YEDEK_KAP" mysqldump "--defaults-extra-file=$YEDEK_KAP_CNF" \
      --single-transaction --routines --triggers --events --hex-blob --databases "$YEDEK_VT" 2>/dev/null
  else
    mysqldump "--defaults-extra-file=$YEDEK_CNF" -h "$YEDEK_SUNUCU" -P "$YEDEK_PORT" \
      --single-transaction --routines --triggers --events --hex-blob --databases "$YEDEK_VT" 2>/dev/null
  fi
}

# InnoDB olmayan tablo taraması: --single-transaction yalnız InnoDB'de tutarlıdır.
# Sayıyı DÖNER; karar çağıranındır (rapora ÖLÇÜM olarak yazılır, varsayılmaz).
innodb_disi_say() {
  my "SELECT COUNT(*) FROM information_schema.TABLES
      WHERE TABLE_SCHEMA='$YEDEK_VT' AND ENGINE IS NOT NULL AND ENGINE <> 'InnoDB';" | tr -d ' '
}

# ── Şifreleme anahtarı — FAIL-CLOSED 2 ──────────────────────────────────────
# Ekip kararı (MÜHÜR 6 / K9): gpg simetrik AES-256. Gerekçe: gpg her dağıtımın
# temel deposunda vardır ve müşteri sunucusuna ikinci bir ikili dosya (age)
# taşımayı gerektirmez. Anahtar DOSYASI değil, T-1'in ortam değişkeni hattı
# kullanılır — ikinci sır yolu açılmaz.
anahtar_dogrula() {
  local a="${Yedek__SifrelemeAnahtari:-}"
  if [ -z "$a" ]; then
    hata_ver "Yedek şifreleme anahtarı (Yedek__SifrelemeAnahtari) TANIMSIZ — şifresiz yedek alınmaz."
    return 1
  fi
  if [ "${#a}" -lt 32 ]; then
    hata_ver "Yedek şifreleme anahtarı 32 karakterden kısa — zayıf/şifresiz yedek alınmaz."
    return 1
  fi
  if [ -n "${KUYUMCU_SIFRELEME_ANAHTARI:-}" ] && [ "$a" = "$KUYUMCU_SIFRELEME_ANAHTARI" ]; then
    hata_ver "Yedek anahtarı alan şifreleme anahtarıyla AYNI — ayrı olmalıdır (tek nokta çöküşü)."
    return 1
  fi
  if ! command -v gpg >/dev/null 2>&1; then
    hata_ver "gpg bulunamadı — şifreleme yapılamıyor, yedek alınmaz."
    return 1
  fi
  return 0
}

sifrele() {   # stdin -> stdout (AES-256; parola fd 3'ten, argv'de GEÇMEZ)
  gpg --batch --quiet --yes --symmetric --cipher-algo AES256 \
      --passphrase-fd 3 --pinentry-mode loopback 3<<<"${Yedek__SifrelemeAnahtari}"
}

coz() {       # stdin -> stdout
  gpg --batch --quiet --yes --decrypt \
      --passphrase-fd 3 --pinentry-mode loopback 3<<<"${Yedek__SifrelemeAnahtari}"
}

sha256_hesapla() {
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$1" | awk '{print $1}'
  else
    shasum -a 256 "$1" | awk '{print $1}'
  fi
}

# ── Parmak izi / karşılaştırma (mevcut betikten TAŞINDI, davranış aynı) ─────
# ⚠ GROUP_CONCAT KULLANILMAZ: group_concat_max_len 1024 bayttır ve şema
#   büyüyünce sağlama SQL'i cümlenin ortasından SESSİZCE kesiliyordu
#   (ölçüm 2026-09-15). Tablo listesi satır satır okunur.
tablolar() { my "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA='$1' ORDER BY TABLE_NAME;"; }

parmak_izi() {   # $1 = şema, $2 = çıktı dosyası
  printf '' > "$2"
  local t liste
  # Liste ÖNCE tümüyle okunur; döngü içinde alt süreç beslenmez (stdin tuzağı).
  liste="$(tablolar "$1")"
  [ -z "$liste" ] && return 0
  while IFS= read -r t; do
    [ -z "$t" ] && continue
    my "CHECKSUM TABLE \`$1\`.\`$t\`;" >> "$2"
  done <<< "$liste"
}

# Karşılaştırma: uyuşan/uyuşmayan sayıları ve UYUŞMAYAN TABLO ADLARINI yazar.
# ⚠ MEHMET KOŞULU: hiçbir satır/alan DEĞERİ basılmaz, yalnız tablo ADI.
karsilastir() {   # $1 kaynak dosya, $2 hedef dosya, $3 kaynak şema, $4 hedef şema
  python3 - "$1" "$2" "$3" "$4" <<'PY'
import sys
def oku(yol, sema):
    d = {}
    for satir in open(yol, encoding='utf-8'):
        p = satir.strip().split('\t')
        if len(p) >= 2:
            d[p[0].replace(sema + '.', '')] = p[1]
    return d
kaynak = oku(sys.argv[1], sys.argv[3])
hedef  = oku(sys.argv[2], sys.argv[4])
uyusmaz = [t for t in kaynak if kaynak[t] != hedef.get(t)]
print(f"TABLO={len(kaynak)}")
print(f"UYUSAN={len(kaynak)-len(uyusmaz)}")
print(f"UYUSMAYAN={len(uyusmaz)}")
print("ADLAR=" + ",".join(sorted(uyusmaz)))
sys.exit(1 if uyusmaz else 0)
PY
}

# ── yedek_kayitlari yazımı ──────────────────────────────────────────────────
kayit_ac() {   # $1 = tür -> id yazar
  local id
  id="y$(date -u +%Y%m%d%H%M%S)$(printf '%04d' $((RANDOM % 10000)))"
  my "INSERT INTO yedek_kayitlari (id,tur,baslangic,durum,sifreli,dis_kopya,dogrulama)
      VALUES ('$id','$1',UTC_TIMESTAMP(),'SURUYOR',0,'YOK','BEKLIYOR');" "$YEDEK_VT"
  echo "$id"
}

kayit_kapat() {  # $1 id · $2 durum · $3 dosya · $4 boyut · $5 sha · $6 şifreli(0/1) · $7 dış kopya · $8 tablo · $9 hata
  local hata_sql="NULL"
  if [ -n "${9:-}" ]; then
    hata_sql="'$(printf '%s' "$9" | sed "s/'/''/g" | cut -c1-500)'"
  fi
  my "UPDATE yedek_kayitlari SET durum='$2', bitis=UTC_TIMESTAMP(), dosya_adi='$3',
      boyut_bayt=$4, sha256='$5', sifreli=$6, dis_kopya='$7', tablo_sayisi=$8,
      hata_ozeti=$hata_sql WHERE id='$1';" "$YEDEK_VT"
}

# ── Dış kopya — FAIL-CLOSED 3 ───────────────────────────────────────────────
# Hedef bir DİZİN ya da 'rclone:uzak:kova' biçimindedir; yapılandırmayla
# değiştirilir (B2 zorunlu DEĞİLDİR). TANIMSIZ hedef de HATA'dır: tek kopya
# yedek, yedek değildir (Mühür 2, Mehmet).
dis_kopya_al() {   # $1 = dosya -> 'KOPYALANDI' | 'HATA'
  if [ -z "$YEDEK_DIS_HEDEF" ]; then
    hata_ver "Dış kopya hedefi (YEDEK_DIS_HEDEF) tanımsız — tek kopya yedek, yedek değildir."
    echo "HATA"; return 1
  fi
  case "$YEDEK_DIS_HEDEF" in
    rclone:*)
      local uzak="${YEDEK_DIS_HEDEF#rclone:}"
      if command -v rclone >/dev/null 2>&1 && rclone copy "$1" "$uzak" >/dev/null 2>&1; then
        echo "KOPYALANDI"; return 0
      fi
      hata_ver "rclone ile dış kopya alınamadı ($uzak)."
      echo "HATA"; return 1 ;;
    *)
      if mkdir -p "$YEDEK_DIS_HEDEF" 2>/dev/null && cp "$1" "$YEDEK_DIS_HEDEF/" 2>/dev/null; then
        echo "KOPYALANDI"; return 0
      fi
      hata_ver "Dış kopya dizinine yazılamadı ($YEDEK_DIS_HEDEF)."
      echo "HATA"; return 1 ;;
  esac
}

# ── Saklama temizliği — FAIL-CLOSED 4 ───────────────────────────────────────
# Son SAKLAMA_GUN gün içinde DOĞRULANMIŞ bir yedek yoksa HİÇBİR dosya silinmez;
# yalnız listelenir. Geliştirme makinesinde varsayılan KAPALIDIR
# (YEDEK_SAKLAMA_ETKIN=1 ile açılır; systemd birimi bunu verir — MÜHÜR 6 / K8).
saklama_temizle() {
  if [ "$YEDEK_SAKLAMA_ETKIN" != "1" ]; then
    bilgi "saklama temizliği KAPALI (YEDEK_SAKLAMA_ETKIN=1 değil) — hiçbir dosya silinmedi"
    return 0
  fi
  local dogrulanmis
  dogrulanmis="$(my "SELECT COUNT(*) FROM yedek_kayitlari WHERE durum='BASARILI'
     AND dogrulama='DOGRULANDI'
     AND baslangic >= UTC_TIMESTAMP() - INTERVAL $YEDEK_SAKLAMA_GUN DAY;" "$YEDEK_VT" | tr -d ' ')"
  if [ "${dogrulanmis:-0}" = "0" ]; then
    hata_ver "Son $YEDEK_SAKLAMA_GUN günde DOĞRULANMIŞ yedek YOK — saklama temizliği HİÇBİR dosyayı silmedi (fail-closed)."
    find "$YEDEK_DIZIN" -maxdepth 1 -name '*.sql.gz.gpg' -mtime "+$YEDEK_SAKLAMA_GUN" 2>/dev/null \
      | sed 's/^/      (silinmedi) /'
    return 0
  fi
  local sayac=0 d
  while IFS= read -r d; do
    [ -z "$d" ] && continue
    command rm -f "$d" && sayac=$((sayac + 1))
  done < <(find "$YEDEK_DIZIN" -maxdepth 1 -name '*.sql.gz.gpg' -mtime "+$YEDEK_SAKLAMA_GUN" 2>/dev/null)
  bilgi "saklama: $sayac dosya silindi (> $YEDEK_SAKLAMA_GUN gün)"
  # ⚠ Üstveri satırı SİLİNMEZ: "ne zaman yedek aldık" izi denetim için kalır.
}
