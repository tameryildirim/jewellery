#!/bin/zsh
# Kalan videolar: anlatım klipleri tamamlanınca (json uzunluğu = adım sayısı) kaydet + birleştir. kasa-kapanis en son (günü kilitler).
B="${0:A:h}"; U="$HOME/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/demo-kayit-2026-10-01-uzun/seslendirme"
for id in banka fatura masak etiket rapor kasa-kapanis; do
  n=$(python3 -c "import json;print(len(json.load(open('$B/plan-$id.json'))['adimlar']))")
  for i in {1..240}; do
    k=$(python3 -c "import json,os;p='$U/klipler-egitim-$id.json';print(len(json.load(open(p))) if os.path.exists(p) else 0)" 2>/dev/null || echo 0)
    [ "$k" -ge "$n" ] && break; sleep 30
  done
  echo "=== $id ($k/$n klip) $(date +%H:%M)"
  node "$B/egitim-kaydet.mjs" "$id" 2>&1 | grep -E 'BİTTİ|SORUN|DUR' | cut -c1-300
  python3 "$B/uret-egitim.py" "$id" 2>&1 | tail -1
done
echo "ZINCIR BITTI $(date +%H:%M)"
