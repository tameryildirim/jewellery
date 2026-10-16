#!/usr/bin/env bash
# ER diyagramlarını .mmd → .svg çizer (mermaid-cli).
# Önce şemadan üretim: python3 er-uret.py
set -euo pipefail
cd "$(dirname "$0")"

echo "── ER diyagramları çiziliyor ──"
for f in er-*.mmd; do
    cikti="${f%.mmd}.svg"
    printf '  %-34s → %s\n' "$f" "$cikti"
    npx -y @mermaid-js/mermaid-cli@11 -i "$f" -o "$cikti" -b white -w 2400 \
        2>&1 | grep -vE '^npm notice|^$|^Generating' || true
done
echo "bitti."
