#!/usr/bin/env bash
# ═════════════════════════════════════════════════════════════════════════════
# KILAVUZ ÜRET — PAKET P-33 (B-11)
#
# docs/KULLANIM_KILAVUZU.md (TEK KAYNAK) + docs/kilavuz-sablon.html (sunum)
# + docs/HARIC-LISTESI.md (gömülür) → Kuyumcu.Web/wwwroot/kilavuz.html
#
# Üretici Kuyumcu.Tests/Araclar/KilavuzUretici.cs'tedir; bu betik "kılavuz
# güncel mi" testini KILAVUZ_YAZ=1 ile koşar: test dosyayı yeniden yazar ve
# ardından birebir aynı olduğunu doğrular. İkinci bir üretim yolu YOKTUR.
#
# Kullanım:  ./dotnet/kilavuz-uret.sh
# Sonra:     ./dotnet/denetim.sh   (KILAVUZ_URETILMIS · KILAVUZ_EKSIK_EKRAN)
# ═════════════════════════════════════════════════════════════════════════════
set -euo pipefail
cd "$(dirname "$0")"

KILAVUZ_YAZ=1 dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo \
  --filter "FullyQualifiedName~KilavuzUretimTestleri.Kilavuz_html_TEK_KAYNAKTAN_uretilmis_ve_GUNCEL"

echo "KILAVUZ ÜRETİLDİ: Kuyumcu.Web/wwwroot/kilavuz.html ($(wc -c < Kuyumcu.Web/wwwroot/kilavuz.html | tr -d ' ') bayt)"
