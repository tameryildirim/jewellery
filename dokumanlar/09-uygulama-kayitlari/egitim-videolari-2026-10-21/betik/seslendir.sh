#!/bin/zsh
# Tüm videoların anlatımını onaylı sesle üret (sırayla; her video kendi klipler-egitim-<id> dizini). Kullanım: zsh seslendir.sh [id ...]
set -e
B="${0:A:h}"; U="$HOME/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/demo-kayit-2026-10-01-uzun/seslendirme"
PY="$HOME/.cache/bygatech-ses-venv-v3/bin/python"
IDS=("$@"); [ ${#IDS[@]} -eq 0 ] && IDS=(satis hurda emanet banka fatura masak kasa-kapanis etiket rapor)
cd "$U"; cp "$B/anlatim_egitim.py" "$U/anlatim_egitim.py"
for id in "${IDS[@]}"; do
  echo "=== $id"; PLAN="$B/plan-$id.json" PYTORCH_ENABLE_MPS_FALLBACK=1 "$PY" klon-sunum-uret.py anlatim_egitim "egitim-$id" 2>&1 | grep -v -i 'warn\|futurewarning\|deprecat' | tail -n 20
done
echo "SESLENDIRME BITTI"
