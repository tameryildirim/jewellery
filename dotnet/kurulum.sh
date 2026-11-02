#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════
# KUYUMCU ERP — KURULUM SİHİRBAZI / SIR ÜRETİCİ (Paket T-1 + F-6, Faz A)
#
# NEDEN VAR: bugüne kadar her kurulum appsettings.json'daki AYNI örnek sırla
# çalışabiliyordu. Bir kurulumdan alınan JWT diğerinde geçerli olurdu.
# Bu betik her kuruluma ÖZGÜ sır üretir.
#
# SIR DİSİPLİNİ (MÜHÜR 3):
#   • Üretilen hiçbir sır EKRANA ya da GÜNLÜĞE BASILMAZ — yalnız "var/yok".
#   • Dosya chmod 600 + sahip kuyumcu:kuyumcu.
#   • Var olan .env SESSİZCE EZİLMEZ; ezmek için açık --yenile bayrağı gerekir
#     ve eski dosya zaman damgalı kopyalanır.
#   • Yedek anahtarı, alan şifreleme anahtarından AYRI üretilir.
#
# Kullanım:
#   sudo ./dotnet/kurulum.sh --hedef /etc/kuyumcu/.env [--vt kuyumcu_net]
#                            [--ters-vekil true|false] [--yenile]
#                            [--adres http://localhost:5199] [--kisayol-dizin DİZİN]
#   sudo ./dotnet/kurulum.sh --hedef /etc/kuyumcu/.env --kisayol [--adres YENİ]
#        → yalnız kısayolu (yeniden) üretir; sırlara DOKUNMAZ.
#
# ADRES VE KISAYOL (PAKET P-33 · K-26 kararı):
#   Uygulamanın adresi kısayola GÖMÜLMEZ, KURULUMDA SORULUR (varsayılan
#   http://localhost:5199). Aynı paket hem tek makinede hem ağ üstünde
#   kurulur; gömülü adres ikinci kurulumda SESSİZCE yanlış yere bakardı.
#   • Adres .env'de TEK satırda tutulur: BYGATECH_ADRES=<adres>
#   • Masaüstü kısayolu O adresle üretilir (macOS .webloc · Linux .desktop;
#     Windows'ta kurulum.ps1 .url üretir).
#   • Adres değişirse: --kisayol --adres <yeni> → satır güncellenir, kısayol
#     yeniden üretilir.
#   • Adres bu makineyi gösteriyorsa (localhost/127.0.0.1:PORT) uygulama o
#     portu dinler (ASPNETCORE_URLS aynı porta hizalanır); başka bir ana
#     makineyi gösteriyorsa uygulama ters vekilin arkasında 127.0.0.1:5000'i
#     dinler (deploy/nginx-kuyumcu.conf).
# ═══════════════════════════════════════════════════════════════════════════
set -euo pipefail

HEDEF="/etc/kuyumcu/.env"
VT="kuyumcu_net"
TERS_VEKIL="true"
YENILE=0
SAHIP="kuyumcu"
ADRES=""
VARSAYILAN_ADRES="http://localhost:5199"
YALNIZ_KISAYOL=0
KISAYOL_DIZIN=""

while [ $# -gt 0 ]; do
  case "$1" in
    --hedef)      HEDEF="$2"; shift 2 ;;
    --vt)         VT="$2"; shift 2 ;;
    --ters-vekil) TERS_VEKIL="$2"; shift 2 ;;
    --sahip)      SAHIP="$2"; shift 2 ;;
    --yenile)     YENILE=1; shift ;;
    --adres)      ADRES="$2"; shift 2 ;;
    --kisayol)    YALNIZ_KISAYOL=1; shift ;;
    --kisayol-dizin) KISAYOL_DIZIN="$2"; shift 2 ;;
    *) echo "HATA: bilinmeyen seçenek '$1'" >&2; exit 2 ;;
  esac
done

# ── ADRES + KISAYOL (P-33 · K-26) ─────────────────────────────────────────
# Adres doğrulanır: şema + ana makine (+ isteğe bağlı port), yol YOK.
adres_gecerli() {
  printf '%s' "$1" | grep -Eq '^https?://[A-Za-z0-9.-]+(:[0-9]{1,5})?/?$'
}

# Adres bu makineyi gösteriyorsa uygulama o portu dinler; değilse ters vekil
# arkasındaki iç bağlama noktası (deploy/nginx-kuyumcu.conf ile aynı).
bag_adresi() {
  local a="${1%/}" port
  case "$a" in
    http://localhost:*|http://127.0.0.1:*) port="${a##*:}"; echo "http://127.0.0.1:${port}" ;;
    *) echo "http://127.0.0.1:5000" ;;
  esac
}

adres_belirle() {
  if [ -z "$ADRES" ]; then
    if [ -t 0 ]; then
      printf 'Uygulamanın adresi (kısayol bu adrese gider) [%s]: ' "$VARSAYILAN_ADRES"
      read -r ADRES || ADRES=""
    else
      echo "  · adres sorulamadı (etkileşimsiz koşu) — varsayılan kullanılıyor: $VARSAYILAN_ADRES"
    fi
    [ -z "$ADRES" ] && ADRES="$VARSAYILAN_ADRES"
  fi
  ADRES="${ADRES%/}"
  if ! adres_gecerli "$ADRES"; then
    echo "HATA: geçersiz adres '$ADRES' (örnek: http://localhost:5199 ya da https://dukkan.ornek)." >&2
    exit 2
  fi
}

kisayol_dizini() {
  if [ -n "$KISAYOL_DIZIN" ]; then echo "$KISAYOL_DIZIN"; return; fi
  local ev="$HOME"
  if [ -n "${SUDO_USER:-}" ] && [ "$SUDO_USER" != "root" ]; then
    ev="$(eval echo "~$SUDO_USER")"
  fi
  if command -v xdg-user-dir >/dev/null 2>&1 && [ -z "${SUDO_USER:-}" ]; then
    xdg-user-dir DESKTOP
  else
    echo "$ev/Desktop"
  fi
}

# Kısayolu adresle üretir. Var olanın ÜZERİNE yazar (adres değişince yenilenmesi
# gereken tek şey budur); dosya adı sabittir → ikinci kısayol birikmez.
kisayol_uret() {
  local adres="$1" dizin dosya
  dizin="$(kisayol_dizini)"
  # Sır dosyası için konan "umask 177" dizine x biti vermez; kısayol dizini
  # alt kabukta olağan izinle açılır.
  ( umask 022; mkdir -p "$dizin" ) || { echo "HATA: kısayol dizini açılamadı: $dizin" >&2; exit 3; }
  case "$(uname -s)" in
    Darwin)
      dosya="$dizin/ByGatech.webloc"
      if ! {
        echo '<?xml version="1.0" encoding="UTF-8"?>'
        echo '<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">'
        echo '<plist version="1.0"><dict><key>URL</key><string>'"$adres"'</string></dict></plist>'
      } > "$dosya"; then
        echo "HATA: kısayol yazılamadı: $dosya" >&2; exit 3
      fi ;;
    *)
      dosya="$dizin/BYGATECH.desktop"
      if ! {
        echo '[Desktop Entry]'
        echo 'Type=Link'
        echo 'Name=ByGatech'
        echo "URL=$adres"
        echo 'Icon=web-browser'
      } > "$dosya"; then
        echo "HATA: kısayol yazılamadı: $dosya" >&2; exit 3
      fi
      chmod 755 "$dosya" ;;
  esac
  # Fail-closed: kısayol GERÇEKTEN bu adresi taşımıyorsa "✓" yazılmaz.
  grep -qF "$adres" "$dosya" || { echo "HATA: kısayol adresi taşımıyor: $dosya" >&2; exit 3; }
  if [ -n "${SUDO_USER:-}" ] && [ "$SUDO_USER" != "root" ]; then
    chown "$SUDO_USER" "$dosya" 2>/dev/null || true
  fi
  echo "  ✓ kısayol: $dosya → $adres"
}

# .env'deki TEK adres satırını yazar/günceller ve bağlama noktasını
# (ASPNETCORE_URLS) adresle hizalar. Sırlara dokunmaz; dosya izni 600 kalır.
adres_satirini_yaz() {
  local hedef="$1" adres="$2" bag
  bag="$(bag_adresi "$adres")"
  if grep -q '^BYGATECH_ADRES=' "$hedef"; then
    ADRES_YENI="$adres" perl -pi -e 's/^BYGATECH_ADRES=.*$/BYGATECH_ADRES=$ENV{ADRES_YENI}/' "$hedef"
  else
    echo "BYGATECH_ADRES=$adres" >> "$hedef"
  fi
  # Bağlama noktası adresle AYNI kuraldan türer (bag_adresi) — adres ile
  # uygulamanın dinlediği port hiçbir zaman ayrışmaz. İKİ anahtar da yazılır:
  # etkin olan "Urls"tur (appsettings.Production.json'daki "Urls"u ezen tek
  # ortam anahtarı — P-33 yerel kap tatbikatında ÖLÇÜLDÜ: yalnız
  # ASPNETCORE_URLS verilince uygulama 5000'i dinledi); ASPNETCORE_URLS eski
  # belgeler/araçlar okuduğu için aynı değerle tutulur.
  local anahtar
  for anahtar in Urls ASPNETCORE_URLS; do
    if grep -q "^${anahtar}=" "$hedef"; then
      BAG_YENI="$bag" ANAHTAR="$anahtar" perl -pi -e 's/^\Q$ENV{ANAHTAR}\E=.*$/$ENV{ANAHTAR}=$ENV{BAG_YENI}/' "$hedef"
    else
      echo "${anahtar}=$bag" >> "$hedef"
    fi
  done
  chmod 600 "$hedef"
  if [ "$(grep -c '^BYGATECH_ADRES=' "$hedef")" != "1" ]; then
    echo "HATA: $hedef içinde BYGATECH_ADRES satırı TEK değil." >&2
    exit 3
  fi
}

if [ "$YALNIZ_KISAYOL" -eq 1 ]; then
  [ -f "$HEDEF" ] || { echo "HATA: '$HEDEF' yok; önce kurulum yapılmalı." >&2; exit 2; }
  if [ -z "$ADRES" ]; then
    ADRES="$(sed -n 's/^BYGATECH_ADRES=//p' "$HEDEF" | head -1)"
  fi
  adres_belirle
  adres_satirini_yaz "$HEDEF" "$ADRES"
  kisayol_uret "$ADRES"
  echo "KISAYOL ÜRETİLDİ — sırlara dokunulmadı. Adres: $ADRES (satır: BYGATECH_ADRES)"
  exit 0
fi

adres_belirle

command -v openssl >/dev/null 2>&1 || { echo "HATA: openssl bulunamadı." >&2; exit 2; }

if [ -f "$HEDEF" ] && [ "$YENILE" -eq 0 ]; then
  echo "DURDU: '$HEDEF' zaten var. Sırlar SESSİZCE EZİLMEZ."
  echo "       Yenilemek için: $0 --hedef '$HEDEF' --yenile"
  echo "       (Uyarı: JWT sırrı değişirse tüm oturumlar düşer; şifreleme"
  echo "        anahtarı değişirse şifreli kimlik alanları OKUNAMAZ ve MFA"
  echo "        gizleri yeniden şifrelenmezse TÜM MFA kullanıcıları kilitlenir —"
  echo "        docs/DEPLOY.md § sır rotasyonu.)"
  exit 1
fi

if [ -f "$HEDEF" ]; then
  KOPYA="$HEDEF.$(date +%Y%m%d%H%M%S).yedek"
  cp -p "$HEDEF" "$KOPYA"
  chmod 600 "$KOPYA"
  echo "  ✓ eski dosya kopyalandı: $KOPYA (izin 600)"
fi

mkdir -p "$(dirname "$HEDEF")"
chmod 750 "$(dirname "$HEDEF")" 2>/dev/null || true

# ── SIR ÜRETİMİ ────────────────────────────────────────────────────────────
# openssl rand: kriptografik rastgelelik. Değerler DEĞİŞKENDE kalır; hiçbiri
# echo edilmez. base64 çıktısındaki satır sonları temizlenir.
JWT_SIR="$(openssl rand -base64 48 | tr -d '\n=')"
DB_PAROLA="$(openssl rand -base64 32 | tr -d '\n=/+')"
SIFRELEME="$(openssl rand -base64 32 | tr -d '\n')"
YEDEK_ANAHTAR="$(openssl rand -base64 32 | tr -d '\n')"

# Fail-closed öz denetim: üretilen sır kısa çıkarsa dosya HİÇ yazılmaz.
if [ "${#JWT_SIR}" -lt 32 ] || [ "${#DB_PAROLA}" -lt 16 ]; then
  echo "HATA: sır üretimi beklenen uzunlukta değil; dosya yazılmadı." >&2
  exit 3
fi
if [ "$SIFRELEME" = "$YEDEK_ANAHTAR" ]; then
  echo "HATA: yedek anahtarı ile alan şifreleme anahtarı aynı çıktı." >&2
  exit 3
fi

# Dosya ÖNCE 600 izniyle oluşturulur, SONRA doldurulur: sır hiçbir an
# dünya-okunabilir bir dosyada durmaz.
umask 177
printf '' > "$HEDEF"
chmod 600 "$HEDEF"

{
  echo "# ByGatech üretim sırları — $(date '+%Y-%m-%d %H:%M:%S')"
  echo "# dotnet/kurulum.sh üretti. ELLE DÜZENLEMEYİN, KOPYALAMAYIN, PAYLAŞMAYIN."
  echo "ASPNETCORE_ENVIRONMENT=Production"
  echo "KUYUMCU_VT=$VT"
  echo "MYSQL_ROOT_PASSWORD=$DB_PAROLA"
  echo "ConnectionStrings__Kuyumcu=server=127.0.0.1;port=3306;database=$VT;user=root;password=$DB_PAROLA"
  echo "Jwt__Secret=$JWT_SIR"
  echo "KUYUMCU_SIFRELEME_ANAHTARI=$SIFRELEME"
  echo "Yedek__SifrelemeAnahtari=$YEDEK_ANAHTAR"
  echo "Uretim__TersVekilArkasinda=$TERS_VEKIL"
  # "Urls" ETKİN anahtardır (appsettings.Production.json "Urls"unu ezer);
  # ASPNETCORE_URLS aynı değerle uyumluluk için tutulur (bkz. adres_satirini_yaz).
  echo "Urls=$(bag_adresi "$ADRES")"
  echo "ASPNETCORE_URLS=$(bag_adresi "$ADRES")"
  echo "BYGATECH_ADRES=$ADRES"
} >> "$HEDEF"

if id "$SAHIP" >/dev/null 2>&1; then
  # Grup adı kullanıcı adından farklı olabilir (macOS'ta "staff"); önce
  # kullanıcı:grup denenir, olmazsa yalnız kullanıcı. Sessiz başarısızlık YOK.
  if chown "$SAHIP:$SAHIP" "$HEDEF" 2>/dev/null || chown "$SAHIP" "$HEDEF"; then
    echo "  ✓ sahip: $SAHIP"
  else
    echo "  ⚠ sahiplik değiştirilemedi (root gerekebilir) — dosya izni 600 olarak kaldı."
  fi
else
  echo "  ⚠ '$SAHIP' kullanıcısı yok; sahiplik değiştirilmedi (useradd --system $SAHIP)."
fi

# ── ÇIKTI: YALNIZ VAR/YOK ──────────────────────────────────────────────────
echo
echo "KURULUM SIRLARI ÜRETİLDİ — hiçbir değer ekrana basılmadı."
for A in ASPNETCORE_ENVIRONMENT KUYUMCU_VT MYSQL_ROOT_PASSWORD \
         ConnectionStrings__Kuyumcu Jwt__Secret KUYUMCU_SIFRELEME_ANAHTARI \
         Yedek__SifrelemeAnahtari Uretim__TersVekilArkasinda BYGATECH_ADRES; do
  if grep -q "^$A=" "$HEDEF"; then echo "  ✓ $A: var"; else echo "  ✗ $A: YOK"; fi
done
echo
echo "Dosya : $HEDEF  (izin $(stat -f '%Lp' "$HEDEF" 2>/dev/null || stat -c '%a' "$HEDEF"))"
echo
kisayol_uret "$ADRES"
echo "Adres  : $ADRES (tek satır: BYGATECH_ADRES; değiştirmek için --kisayol --adres <yeni>)"
echo "YEDEKLEYİN: KUYUMCU_SIFRELEME_ANAHTARI kaybolursa şifreli kimlik"
echo "verileri GERİ OKUNAMAZ (MASAK/KVKK alanları). docs/DEPLOY.md § sır rotasyonu."
