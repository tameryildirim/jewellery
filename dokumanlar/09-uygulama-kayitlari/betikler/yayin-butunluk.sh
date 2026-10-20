#!/bin/zsh
# ═════════════════════════════════════════════════════════════════════════════
# P-41 — YAYIN ÇIKTI ENVANTERİ (SHA-256) + FAIL-CLOSED DOĞRULAMA
#
# NEDEN VAR (tur 8'de ÖLÇÜLEN arıza, Anayasa md.1 — iddia değil kanıt):
#   • Kaynakta `Kuyumcu.Web/wwwroot/kilavuz.html` VAR (60.885 bayt, 18/09 11:09),
#     yayın anlık görüntüsünde YOK → canlı `/kilavuz` = `404 0`.
#   • `_framework` altındaki iki `.wasm` dosyasının mtime'ı 23/09 12:05'ti, oysa
#     `blazor.boot.json` ve `.agac-damgasi` 22/09 06:05 → yayına `yayinla.sh`
#     DIŞINDA bir el dokunmuştu. KİMİN dokunduğu DOĞRULANAMADI (23/09 publish
#     günlüğü yok). Bu betik FAİLİ bulmaz; KAPISIZLIĞI kapatır.
#   • Sonuç: QA turu 8'de grup 1'in 6 rotası (/cikti, /kasalar, /sayim, /banka,
#     /pos/tahsilatlar, /yetkiler) HİÇ açılamadı ve 1/1 puanlandı. Ölçüm aracı
#     bozukken ürün puanı konuşulamaz.
#
# NE YAPAR:
#   --uret   [dizin]  → dizindeki yayın çıktısının SHA-256 envanterini
#                       `<dizin>/.yayin-envanteri` dosyasına yazar.
#                       Satır biçimi:  <sha256> <yol> <bayt>
#                       Kapsam: Kuyumcu.Api.dll + wwwroot KÖK dosyaları +
#                       çekirdek önyükleme dosyaları + blazor.boot.json'daki
#                       HER varlık (assembly/icu/wasmNative/jsModule*/pdb).
#   --dogrula [dizin] → envanterdeki her satır için: dosya VAR mı, 0 BAYT mı,
#                       boyut tutuyor mu, SHA tutuyor mu; ayrıca KAYNAK wwwroot
#                       kök dosyalarının HEPSİ envanterde mi (kilavuz.html tuzağı).
#                       Tek bir ihlalde çıkış != 0 ve ihlal eden YOL stdout'ta.
#
# ÇIKIŞ KODLARI (bu betiğin kendi ad alanı; qa-sunucu.sh'in kodlarıyla karışmaz):
#   0 temiz · 2 yayın dizini yok · 3 envanter/boot.json yok · 4 python3 yok
#   5 envanter üretilemedi (çıktı eksik) · 6 bütünlük ihlali (--dogrula)
#   64 kullanım hatası
#
# KULLANIM:  zsh betikler/yayin-butunluk.sh --uret|--dogrula [yayın-dizini]
# ORTAM:     KUYUMCU_YAYIN_DIZINI      → varsayılan yayın dizinini ezer
#            KUYUMCU_KAYNAK_WWWROOT    → kaynak wwwroot karşılaştırma kökü
# ═════════════════════════════════════════════════════════════════════════════
set -u

KOK=/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery
# VARSAYILAN HEDEF: TEK KAYNAK (P-42) — yol bu dosyada YAZILI DEĞİL.
source "$0:a:h/yayin-yolu.sh"
VARSAYILAN_HEDEF="$YAYIN_DIZINI"

KIP=${1:-}
HEDEF=${2:-${KUYUMCU_YAYIN_DIZINI:-$VARSAYILAN_HEDEF}}
KAYNAK_WWWROOT=${KUYUMCU_KAYNAK_WWWROOT:-$KOK/dotnet/Kuyumcu.Web/wwwroot}
ENVANTER="$HEDEF/.yayin-envanteri"

kullanim() {
  echo "Kullanım: zsh yayin-butunluk.sh --uret|--dogrula [yayın-dizini]"
  echo "  --uret     <dizin>/.yayin-envanteri dosyasını üretir (SHA-256)"
  echo "  --dogrula  envanteri diske karşı doğrular (fail-closed)"
}

sha256() { shasum -a 256 "$1" 2>/dev/null | awk '{print $1}'; }
bayt()   { stat -f %z "$1" 2>/dev/null; }

# blazor.boot.json içindeki varlık ADLARINI satır satır basar.
# satelliteResources bilinçli olarak DIŞARIDA: iç içe (kültür → dosya) sözlüktür
# ve düz `_framework/<ad>` yoluna oturmaz; yanlış yol üretmektense kapsam dışıdır.
boot_varliklari() {
  python3 - "$1" <<'PY'
import json, sys
try:
    d = json.load(open(sys.argv[1]))
except Exception as e:
    sys.stderr.write("blazor.boot.json ayrıştırılamadı: %s\n" % e)
    sys.exit(1)
r = d.get("resources", {})
for bolum in ("assembly", "icu", "wasmNative", "jsModuleNative",
              "jsModuleRuntime", "jsModuleWorker", "pdb", "lazyAssembly"):
    v = r.get(bolum)
    if isinstance(v, dict):
        for ad in v:
            print(ad)
PY
}

# ── --uret ───────────────────────────────────────────────────────────────────
uret() {
  [ -d "$HEDEF" ] || { echo "HATA: yayın dizini yok: $HEDEF"; return 2; }
  local BOOT="$HEDEF/wwwroot/_framework/blazor.boot.json"
  [ -r "$BOOT" ] || { echo "HATA: blazor.boot.json okunamıyor: $BOOT"; return 3; }
  command -v python3 >/dev/null 2>&1 || {
    echo "HATA: python3 yok — boot varlıkları ayrıştırılamaz. Envanter ÜRETİLMEDİ (fail-closed)."
    return 4
  }

  local -a YOLLAR
  YOLLAR=()

  # 1) Sunucu ikilisi — yayının kimliği.
  YOLLAR+=("Kuyumcu.Api.dll")

  # 2) wwwroot KÖK dosyaları. ⚠ kilavuz.html tuzağı TAM BURADA yakalanır:
  #    kök taraması kaldırılırsa kilavuz.html envantere hiç girmez ve
  #    "yayın eksik dosya servis ediyor" arızası bir daha görünmez.
  local d
  while IFS= read -r d; do
    [ -n "$d" ] && YOLLAR+=("wwwroot/${d##*/}")
  done < <(find "$HEDEF/wwwroot" -maxdepth 1 -type f 2>/dev/null | sort)

  # 3) Çekirdek önyükleme dosyaları (boot.json kendisi kendi kaynak listesinde yoktur).
  local c
  for c in blazor.boot.json dotnet.js blazor.webassembly.js; do
    [ -f "$HEDEF/wwwroot/_framework/$c" ] && YOLLAR+=("wwwroot/_framework/$c")
  done

  # 4) blazor.boot.json'daki HER varlık.
  local ad
  while IFS= read -r ad; do
    [ -n "$ad" ] && YOLLAR+=("wwwroot/_framework/$ad")
  done < <(boot_varliklari "$BOOT")

  local LISTE
  LISTE=$(printf '%s\n' "${YOLLAR[@]}" | awk 'NF && !g[$0]++')

  local ICERIK="" EKSIK=0 SAYAC=0 y t s b
  while IFS= read -r y; do
    t="$HEDEF/$y"
    if [ ! -f "$t" ]; then
      echo "ENVANTER ÜRETİLEMEDİ — çıktıda olmayan dosya: $y"
      EKSIK=1
      continue
    fi
    s=$(sha256 "$t"); b=$(bayt "$t")
    if [ -z "$s" ] || [ -z "$b" ]; then
      echo "ENVANTER ÜRETİLEMEDİ — okunamayan dosya: $y"
      EKSIK=1
      continue
    fi
    ICERIK+="$s $y $b"$'\n'
    SAYAC=$((SAYAC+1))
  done <<< "$LISTE"

  if [ "$EKSIK" -ne 0 ]; then
    echo "ENVANTER YAZILMADI (fail-closed) — yayın çıktısı eksik."
    return 5
  fi

  printf '%s' "$ICERIK" > "$ENVANTER"
  echo "envanter üretildi: $ENVANTER · $SAYAC kayıt"
  return 0
}

# ── --dogrula ────────────────────────────────────────────────────────────────
dogrula() {
  [ -d "$HEDEF" ] || { echo "BÜTÜNLÜK RET — yayın dizini yok: $HEDEF"; return 2; }
  [ -r "$ENVANTER" ] || { echo "BÜTÜNLÜK RET — envanter yok: $ENVANTER"; return 3; }

  local HATA=0 SAYAC=0 s y b t g c
  while read -r s y b; do
    [ -n "$s" ] || continue
    SAYAC=$((SAYAC+1))
    t="$HEDEF/$y"
    if [ ! -e "$t" ]; then
      echo "EKSİK DOSYA: $y"; HATA=1; continue
    fi
    # 0 BAYT AYRI BİR ARIZADIR: tur 8'de yayın "vardı" ama boştu; "var mı"
    # kontrolü bunu GÖREMEZ. Kendi mesajıyla raporlanır.
    if [ ! -s "$t" ]; then
      echo "SIFIR BAYT: $y"; HATA=1; continue
    fi
    g=$(bayt "$t")
    if [ "$g" != "$b" ]; then
      echo "BOYUT UYUŞMAZLIĞI: $y (envanter $b bayt, disk $g bayt)"; HATA=1; continue
    fi
    c=$(sha256 "$t")
    if [ "$c" != "$s" ]; then
      echo "SHA UYUŞMAZLIĞI: $y"; HATA=1; continue
    fi
  done < "$ENVANTER"

  # ── KAYNAK KARŞILAŞTIRMASI (kilavuz.html tuzağı) ───────────────────────────
  # Envanter kendi içinde tutarlı olabilir ama EKSİK üretilmiş olabilir:
  # yayın kilavuz.html'i hiç içermezse envanter de içermez ve yukarıdaki
  # döngü hiçbir şey göremez. Bu yüzden KAYNAK wwwroot kök dosyalarının
  # HEPSİ envanterde aranır.
  if [ -d "$KAYNAK_WWWROOT" ]; then
    local d ad KAYNAK_SAYISI=0
    while IFS= read -r d; do
      [ -n "$d" ] || continue
      ad="${d##*/}"
      # PAKET-44 / PWA-1 (20.10): service-worker.published.js yalnız KAYNAKTA durur — Blazor yayında
      # onu service-worker.js'in YERİNE yazar (csproj <ServiceWorker PublishedContent>). Yayında aynı adla
      # bulunmaması doğaldır; yayındaki service-worker.js envanterde ayrıca aranır (tek istisna, ad sabit).
      if [ "$ad" = "service-worker.published.js" ]; then
        grep -q " wwwroot/service-worker.js " "$ENVANTER" || { echo "YAYINDA OLMAYAN KAYNAK DOSYASI: wwwroot/service-worker.js (PublishedContent yazılmamış)"; HATA=1; }
        continue
      fi
      KAYNAK_SAYISI=$((KAYNAK_SAYISI+1))
      if ! grep -q " wwwroot/$ad " "$ENVANTER"; then
        echo "YAYINDA OLMAYAN KAYNAK DOSYASI: wwwroot/$ad"
        HATA=1
      fi
    done < <(find "$KAYNAK_WWWROOT" -maxdepth 1 -type f 2>/dev/null | sort)
    if [ "$KAYNAK_SAYISI" -eq 0 ]; then
      echo "⚠ UYARI: kaynak wwwroot BOŞ göründü ($KAYNAK_WWWROOT) — kaynak karşılaştırması ÖLÇÜLEMEDİ."
    fi
  else
    echo "⚠ UYARI: kaynak wwwroot okunamadı ($KAYNAK_WWWROOT) — kaynak karşılaştırması ATLANDI."
  fi

  if [ "$HATA" -ne 0 ]; then
    echo "BÜTÜNLÜK RET — yayın çıktısı eksik/bozuk ($SAYAC kayıt denetlendi)."
    return 6
  fi
  echo "bütünlük TEMİZ — $SAYAC kayıt doğrulandı ($HEDEF)"
  return 0
}

case "$KIP" in
  --uret)    uret;    exit $? ;;
  --dogrula) dogrula; exit $? ;;
  *)         kullanim; exit 64 ;;
esac
