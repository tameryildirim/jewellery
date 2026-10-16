#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════
# KUYUMCU ERP — YAYIN BETİĞİ (Paket T-1 + F-6, Faz A)
#
# NE YAPAR:
#   1. Çözümü Release derler (--no-build ASLA kullanılmaz — ortam tuzağı).
#   2. Self-contained yayın üretir: hedef makinede .NET SDK/runtime GEREKMEZ.
#      `dotnet --list-sdks` BOŞ iken uygulama çalışır (self-contained kanıtı).
#   3. Migration bundle üretir: kuyumcu-migrate(.exe). Şema yükseltmesi
#      uygulamanın kendi süreciyle DEĞİL, bu tek araçla yapılır
#      (bkz. FAIL-CLOSED 5, docs/RUNBOOK-SEMA-YUKSELTME.md).
#
# NEDEN PublishSingleFile DEĞİL: Blazor WASM statikleri (wwwroot/_framework)
#   tek dosyaya gömülemez; klasör yayını zorunludur.
#
# Kullanım:  ./dotnet/yayin-al.sh [linux-x64|win-x64|hepsi]   (varsayılan: hepsi)
# Çıktı:     dotnet/yayin/<rid>/
# ═══════════════════════════════════════════════════════════════════════════
set -euo pipefail
cd "$(dirname "$0")"

HEDEF="${1:-hepsi}"
case "$HEDEF" in
  linux-x64|win-x64) RIDLER="$HEDEF" ;;
  hepsi)             RIDLER="linux-x64 win-x64" ;;
  *) echo "HATA: bilinmeyen hedef '$HEDEF' (linux-x64 | win-x64 | hepsi)" >&2; exit 2 ;;
esac

CIKTI="$(pwd)/yayin"
mkdir -p "$CIKTI"

echo "── 1/3  Release derleme (derlemesiz kısayol YOK) ───────────────────"
dotnet build Kuyumcu.sln -c Release --nologo

for RID in $RIDLER; do
  echo "── 2/3  Yayın: $RID ────────────────────────────────────────────────"
  dotnet publish Kuyumcu.Api/Kuyumcu.Api.csproj \
    -c Release -r "$RID" --self-contained true \
    -p:PublishSingleFile=false \
    -p:InvariantGlobalization=false \
    -o "$CIKTI/$RID" --nologo

  # Şablon ve sır dosyaları yayına SIZMASIN: geliştirme ayarları üretimde
  # okunmamalı (FAIL-CLOSED 6 ile aynı amaç).
  if [ -f "$CIKTI/$RID/appsettings.Development.json" ]; then
    echo "  ✗ DURDU: appsettings.Development.json yayın çıktısında." >&2
    echo "    Geliştirme parolası ve JWT sırrı müşteri sunucusuna taşınırdı." >&2
    echo "    Kuyumcu.Api.csproj'daki CopyToPublishDirectory=Never satırı silinmiş olabilir." >&2
    exit 4
  fi

  # ── AĞAÇ DAMGASI (B-2): yayın = ağaç kanıtı yayının İÇİNE yazılır. Müşteri
  #    sunucusunda "hangi kaynaktan derlendi" sorusu tahminle değil dosyayla
  #    cevaplanır; QA tarafında qa-sunucu.sh aynı damgayı fail-closed kontrol eder.
  #    İçerik karması (mtime DEĞİL — K-20 iCloud eşitlemesi mtime'ları oynatıyor).
  find Kuyumcu.Api Kuyumcu.Web Kuyumcu.Core \
       \( -name '*.cs' -o -name '*.razor' -o -name '*.csproj' \) 2>/dev/null \
    | grep -v '/obj/\|/bin/' | sort | xargs shasum 2>/dev/null | shasum | awk '{print $1}' > "$CIKTI/$RID/.agac-damgasi"
  echo "  ✓ ağaç damgası: $(cut -c1-12 "$CIKTI/$RID/.agac-damgasi")…"

  BOYUT=$(du -sh "$CIKTI/$RID" | cut -f1)
  echo "  ✓ $RID → $CIKTI/$RID ($BOYUT)"
done

echo "── 3/3  Migration bundle (kuyumcu-migrate) ─────────────────────────"
# Bundle da self-contained üretilir: şema yükseltmesi yapan makinede de SDK
# aranmaz. --no-build KULLANILMAZ.
for RID in $RIDLER; do
  UZANTI=""; [ "$RID" = "win-x64" ] && UZANTI=".exe"
  dotnet ef migrations bundle \
    --project Kuyumcu.Api --startup-project Kuyumcu.Api \
    --configuration Release --self-contained --target-runtime "$RID" \
    --output "$CIKTI/$RID/kuyumcu-migrate$UZANTI" --force
  echo "  ✓ kuyumcu-migrate$UZANTI → $CIKTI/$RID"
done

echo
echo "YAYIN TAMAM. Sıradaki adımlar:"
echo "  • Sır üretimi   : ./dotnet/kurulum.sh --hedef /etc/kuyumcu/.env"
echo "  • Servis kurulum: deploy/systemd/kuyumcu.service  (Windows: deploy/kuyumcu-service-kur.ps1)"
echo "  • Şema          : ÖNCE ./dotnet/yedek-al-ve-dogrula.sh, SONRA kuyumcu-migrate"
echo "                    (docs/RUNBOOK-SEMA-YUKSELTME.md)"
ls -l "$CIKTI"
