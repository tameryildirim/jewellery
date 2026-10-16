#!/usr/bin/env zsh
# ═══════════════════════════════════════════════════════════════════════════
# ANLIK GÖRÜNTÜ DÖNGÜSÜ — kaynak ağacının 15 dakikada bir şifresiz yedeği
#
# NEDEN (kurucu, 2026-09-23): "iş kaybı istemiyorum; duraksama olabilir ama
# 1 saat uğraştığın iş 'kapattım' diye başa sarmasın."
#
# NE KORUR: dosya durumunu. Bir ajan yazarken kesilirse (22 Eylül'de P-28
# uygulayıcısı tam da böyle kesildi ve kaynak ağacı DERLENMEZ hâlde bıraktı),
# son sağlam anlık görüntüyle 'diff' alıp yarım yazımı görebiliriz.
# NE KORUMAZ: havadaki ajanın düşüncesi. Onu koşum önbelleği kurtarır
# (bkz. ham/KOSUM-DEVAM-RECETESI.md).
#
# Kullanım:  nohup zsh betikler/anlik-goruntu.sh &
# Durdurma:  pkill -f anlik-goruntu.sh
# ═══════════════════════════════════════════════════════════════════════════
set -u

KAYNAK=/Users/ramazanakkocan/ByGatech-yerel
HEDEF=/Users/ramazanakkocan/KuyumcuERP-yedek/anlik
ARALIK=1800         # 30 dakika (23.09: makine takasa yaziyordu, dosya-onbellek carpisi yarilandi)
TUT=10              # son 10 görüntü tutulur (~1,7 GB) — eskisi ÜZERİNE YAZILIR
KAYIT=$HEDEF/anlik-goruntu.log

mkdir -p "$HEDEF"

yaz() { print -r -- "$(date '+%d/%m %H:%M:%S')  $*" >> "$KAYIT" }

yaz "anlık görüntü döngüsü başladı (pid $$, aralık ${ARALIK}sn, halka ${TUT})"

SIRA=0
while :; do
  SIRA=$(( (SIRA % TUT) + 1 ))
  DOSYA=$HEDEF/anlik-$(printf '%02d' $SIRA).tar.gz
  BAS=$(date +%s)

  # Halka tamponu: aynı sıra numarası ÜZERİNE yazılır, silme komutu gerekmez
  # (kabuk kancası rm/mv yasaklıyor — tasarım bu kısıtla uyumlu).
  if tar -C "$KAYNAK" \
        --exclude='*/bin/*' --exclude='*/obj/*' \
        --exclude='*/bin' --exclude='*/obj' \
        -czf "$DOSYA.yazilliyor" jewellery 2>/dev/null; then
    cat "$DOSYA.yazilliyor" > "$DOSYA" 2>/dev/null
    printf '' > "$DOSYA.yazilliyor"
    BOYUT=$(stat -f %z "$DOSYA" 2>/dev/null)
    yaz "görüntü $SIRA yazıldı · $((BOYUT/1048576)) MB · $(( $(date +%s) - BAS )) sn"
  else
    yaz "UYARI: görüntü $SIRA ALINAMADI (tar sıfırdan farklı döndü) — gizlenmiyor"
  fi

  sleep "$ARALIK"
done
