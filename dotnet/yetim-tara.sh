#!/usr/bin/env bash
# ═════════════════════════════════════════════════════════════════════════════
# YETİM KAYIT VE KİRACI SIZINTISI TARAMASI — ByGatech
#
# ADR-0009'un ÖN ŞARTI. Üç oy birden (Bertan, Mehmet, Aşkın) bu taramanın
# yabancı anahtar eklenmeden ÖNCE çalıştırılmasını şart koştu.
#
# İKİ AYRI SORU SORAR — ve ikisi farklı ağırlıktadır:
#
#   A) YETİM      : `*_id` dolu ama hedef kayıt YOK.
#                   → uygulama hatası / veri tutarsızlığı.
#
#   B) KİRACI     : hedef kayıt VAR ama BAŞKA KİRACIYA ait.
#      SIZINTISI    → GÜVENLİK OLAYI. Sessizce düzeltilmez; raporlanır.
#                     (Mehmet'in K3 koşulu, Anayasa md.1)
#
# Basit (tek sütunlu) yabancı anahtar (B)'yi ENGELLEMEZ — yalnız "bu id var mı"
# diye sorar, hangi kiracıya ait olduğunu sormaz. Bu yüzden ADR-0009'da kompozit
# `(id, tenant_id)` FK kararlaştırıldı; bu betik o kararın veri ön koşuludur.
#
# SALT OKUNURDUR. Hiçbir INSERT/UPDATE/DELETE içermez.
# Tek bir bulgu varsa çıkış kodu ≠ 0 (CI'da fail-closed kapı).
#
# Kullanım:  ./yetim-tara.sh [veritabani]
# ═════════════════════════════════════════════════════════════════════════════
set -uo pipefail

KAP="${KUYUMCU_KAP:-kuyumcu-mysql}"
VT="${1:-${KUYUMCU_VT:-kuyumcu_net}}"
SIFRE="${KUYUMCU_SIFRE:-8yIXtcIoxE9t9V01DIvIry9uxcZLr3}"

my() { docker exec "$KAP" mysql -uroot -p"$SIFRE" -N -e "$1" 2>/dev/null; }

# ── Sütun → hedef tablo eşlemesi ─────────────────────────────────────────────
# ELLE yazılır, tahmin edilmez. Türkçe çoğul ekleri düzensiz olduğu için
# otomatik türetme hatalı eşleme üretir (Anayasa md.1).
#
# FK'YA DÖNÜŞTÜRÜLEMEYENLER (Bertan'ın tespiti, ADR-0009):
#   varlik_id : POLİMORFİK — denetim kaydı herhangi bir varlığı işaret eder,
#               hedef tablo `varlik_turu` sütunuyla belirlenir. Tek hedef yok.
#   ayar_id   : HEDEF TABLOSU YOK — ayar (K14/K18/K22/K24) kodda sabittir,
#               veritabanında `ayarlar` diye bir tablo bulunmaz.
# Bu ikisi bilinçli olarak KAPSAM DIŞIDIR; "39/39 ilişkiye FK" sözü verilemez.
ESLEME="
banka_hareketleri|banka_hesap_id|banka_hesaplari
banka_hareketleri|cari_id|cariler
banka_hareketleri|fatura_id|faturalar
cari_bakiyeler|cari_id|cariler
denetim_kayitlari|kullanici_id|kullanicilar
faturalar|banka_hareket_id|banka_hareketleri
faturalar|cari_id|cariler
gider_pusulalari|cari_id|cariler
gider_pusulalari|hareket_id|hareketler
gider_pusulalari|sube_id|subeler
gider_pusulalari|duzeltilen_id|gider_pusulalari
hareketler|cari_id|cariler
hareketler|karsi_sube_id|subeler
hareketler|kullanici_id|kullanicilar
hareketler|kur_snapshot_id|kur_snapshots
hareketler|sube_id|subeler
hareketler|urun_id|urunler
kasa_hareketleri|hareket_id|hareketler
kasa_hareketleri|kasa_id|kasalar
kasalar|sube_id|subeler
kullanicilar|sube_id|subeler
masak_beyanlari|alan_kullanici_id|kullanicilar
masak_beyanlari|duzeltilen_id|masak_beyanlari
masak_beyanlari|hareket_id|hareketler
masak_beyanlari|kimlik_tespiti_id|masak_kimlik_tespitleri
masak_kimlik_tespitleri|cari_id|cariler
masak_kimlik_tespitleri|duzeltilen_id|masak_kimlik_tespitleri
masak_kimlik_tespitleri|hareket_id|hareketler
masak_kimlik_tespitleri|tespit_eden_kullanici_id|kullanicilar
masak_supheli_bildirimler|bildiren_kullanici_id|kullanicilar
masak_supheli_bildirimler|cari_id|cariler
masak_supheli_bildirimler|hareket_id|hareketler
masak_supheli_bildirimler|kimlik_tespiti_id|masak_kimlik_tespitleri
refresh_tokenler|kullanici_id|kullanicilar
stoklar|sube_id|subeler
stoklar|urun_id|urunler
"

printf '═══ YETİM VE KİRACI SIZINTISI TARAMASI ═══\n'
printf 'veritabanı : %s\n' "$VT"
printf 'tarih      : %s\n\n' "$(date '+%Y-%m-%d %H:%M:%S')"
printf '%-27s %-26s %-24s %8s %8s %8s  %s\n' \
    "ÇOCUK TABLO" "SÜTUN" "HEDEF TABLO" "DOLU" "YETİM" "SIZINTI" "DURUM"
printf '%.0s─' {1..118}; printf '\n'

TOPLAM_YETIM=0
TOPLAM_SIZINTI=0
TARANAN=0

while IFS='|' read -r cocuk sutun ebeveyn; do
    [ -z "${cocuk:-}" ] && continue
    TARANAN=$((TARANAN + 1))

    DOLU=$(my "SELECT COUNT(*) FROM \`$VT\`.\`$cocuk\` WHERE \`$sutun\` IS NOT NULL;")
    YETIM=$(my "SELECT COUNT(*) FROM \`$VT\`.\`$cocuk\` c
                LEFT JOIN \`$VT\`.\`$ebeveyn\` p ON c.\`$sutun\` = p.id
                WHERE c.\`$sutun\` IS NOT NULL AND p.id IS NULL;")
    SIZINTI=$(my "SELECT COUNT(*) FROM \`$VT\`.\`$cocuk\` c
                  JOIN \`$VT\`.\`$ebeveyn\` p ON c.\`$sutun\` = p.id
                  WHERE c.tenant_id <> p.tenant_id;")

    DOLU=${DOLU:-0}; YETIM=${YETIM:-0}; SIZINTI=${SIZINTI:-0}
    TOPLAM_YETIM=$((TOPLAM_YETIM + YETIM))
    TOPLAM_SIZINTI=$((TOPLAM_SIZINTI + SIZINTI))

    if   [ "$SIZINTI" -gt 0 ]; then DURUM="⛔ KİRACI SIZINTISI"
    elif [ "$YETIM"   -gt 0 ]; then DURUM="✗ YETİM"
    else                            DURUM="✓"
    fi

    printf '%-27s %-26s %-24s %8s %8s %8s  %s\n' \
        "$cocuk" "$sutun" "$ebeveyn" "$DOLU" "$YETIM" "$SIZINTI" "$DURUM"
done <<< "$ESLEME"

printf '%.0s─' {1..118}; printf '\n'
printf 'taranan ilişki: %s · toplam yetim: %s · toplam kiracı sızıntısı: %s\n\n' \
    "$TARANAN" "$TOPLAM_YETIM" "$TOPLAM_SIZINTI"

printf 'KAPSAM DIŞI (FK kurulamaz — ADR-0009):\n'
printf '  denetim_kayitlari.varlik_id  → polimorfik (hedef `varlik_turu` ile belirlenir)\n'
printf '  urunler.ayar_id              → hedef tablo yok (ayar kodda sabit)\n'
printf '  gider_pusulalari.ayar_id     → hedef tablo yok (ayar kodda sabit)\n\n'

if [ "$TOPLAM_SIZINTI" -gt 0 ]; then
    printf '⛔ KİRACI SIZINTISI BULUNDU — bu bir GÜVENLİK OLAYIDIR.\n'
    printf '   Sessizce düzeltilmez. Mehmet (06 Güvenlik) ve Kurucu bilgilendirilir.\n'
    exit 2
fi
if [ "$TOPLAM_YETIM" -gt 0 ]; then
    printf '✗ YETİM KAYIT BULUNDU — yabancı anahtar eklenmeden önce çözülmelidir.\n'
    printf '  (MySQL 8.4: ADD FOREIGN KEY mevcut veride yetim varsa HATA verir.)\n'
    exit 1
fi
printf '✅ TEMİZ — yetim kayıt yok, kiracı sızıntısı yok.\n'
exit 0
