#!/bin/zsh
# ═════════════════════════════════════════════════════════════════════════════
# P-42 — YAYIN DİZİNİ: TEK KAYNAK YOL (kalıcı, oturumdan bağımsız)
#
# NEDEN VAR (tur 8'de ÖLÇÜLEN arıza — iddia değil kanıt, Anayasa md.1):
#   • Yayın anlık görüntüsü OTURUMA ÖZGÜ bir /private/tmp scratchpad dizininde
#     yaşıyordu. O dizin oturum bitince yok olur → kurucunun 5199 ekranı bir
#     daha AÇILMAZ ve gösterilecek ürün kalmaz.
#   • Aynı yol BEŞ ayrı dosyaya elle kopyalanmıştı (yayinla.sh, qa-sunucu.sh,
#     yayin-butunluk.sh, nobetci.sh ×2). Biri güncellenip diğeri unutulunca
#     "sunucu başka dizinden koşar, envanter başka dizini doğrular" sessiz
#     tutarsızlığı doğar. Tur 8'de tam bu oldu.
#   • 23/09 12:05'te yayına `yayinla.sh` DIŞINDA bir el dokundu (publish günlüğü
#     yok, 6 dosya değişti). Yazılabilir bir yayın dizini bunu davet ediyor;
#     yayın sonrası salt-okunur kilidi (yayinla.sh) bu betikle birlikte gelir.
#
# NE YAPAR: yayın dizinini TEK yerde tanımlar. Tüketen her betik bunu `source`
# eder; hiçbir betikte ikinci bir yol yazılı DEĞİLDİR.
#
# ÖNCELİK SIRASI:
#   1) $KUYUMCU_YAYIN_DIZINI        ← açıkça verilmişse HER ZAMAN üstündür
#                                      (fikstür/test/geçici koşumlar bunu kullanır)
#   2) $HOME/.kuyumcu-yayin         ← KALICI VARSAYILAN (oturumdan bağımsız)
#
# KULLANIM:  source "<betikler>/yayin-yolu.sh"   → $YAYIN_DIZINI dolu gelir
#            zsh yayin-yolu.sh --yaz             → yolu stdout'a basar (betik dışı tüketici)
#
# ESKİ DİZİN: varsa BİR KEZ kopyalanır (taşınmaz). Silmeyi KURUCU yapar —
# bu depoda `rm`/`mv` kancası bloklar ve zaten geri dönüşü olmayan iş kurucunundur.
# ═════════════════════════════════════════════════════════════════════════════

KUYUMCU_YAYIN_VARSAYILAN="$HOME/.kuyumcu-yayin"
YAYIN_DIZINI="${KUYUMCU_YAYIN_DIZINI:-$KUYUMCU_YAYIN_VARSAYILAN}"

# Kapı kanıtlarının kökü de aynı gerekçeyle oturumdan bağımsızdır (kapi.sh).
KUYUMCU_KAPI_CIKTI_KOKU="${KUYUMCU_KAPI_CIKTI_KOKU:-$HOME/.kuyumcu-kapi}"
mkdir -p "$KUYUMCU_KAPI_CIKTI_KOKU" 2>/dev/null

# Eski, oturuma özgü yayın dizininden BİR KEZ devralma.
# Yol buraya SABİT yazılmaz: kurucunun makinesinde hangi oturum klasörü kaldıysa
# o bulunur. Böylece bu dosyada da oturuma özgü bir yayın yolu literali olmaz.
kuyumcu_yayin_devral() {
  # Açıkça bir dizin verilmişse (fikstür/test/geçici koşum) DEVRALMA YOK:
  # o dizin çağıranın malıdır, eski anlık görüntüyle kirletilmez.
  [ -n "${KUYUMCU_YAYIN_DIZINI:-}" ] && return 0
  [ -f "$YAYIN_DIZINI/Kuyumcu.Api.dll" ] && return 0   # yeni dizin zaten dolu
  local eski
  # Oturum kökü SABİT YAZILMAZ: kullanıcı kimliğinden türetilir ve joker ile
  # aranır. Böylece bu dosyada da oturuma özgü bir yayın yolu literali olmaz
  # (kabul koşulu: betiklerde oturuma özgü yayın yolu kalmayacak).
  local eski_kok="/private/tmp/claude-$(id -u)"
  eski=$(ls -dt $eski_kok/*/*/scratchpad/qa-yayin 2>/dev/null | head -1)
  [ -n "$eski" ] && [ -f "$eski/Kuyumcu.Api.dll" ] || return 1
  mkdir -p "$YAYIN_DIZINI" || return 1
  chmod -R u+w "$YAYIN_DIZINI" 2>/dev/null
  cp -R "$eski/." "$YAYIN_DIZINI/" 2>/dev/null || return 1
  echo "  yayın dizini devralındı (kopya): $eski → $YAYIN_DIZINI"
  echo "  eski dizini SİLMEK kurucuya bırakıldı (rm kancası bloklar)."
  return 0
}

if [ "${1:-}" = "--yaz" ]; then
  printf '%s\n' "$YAYIN_DIZINI"
fi
