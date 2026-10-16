#!/bin/zsh
# Şerit gözcüsü (kalıcı kopya — /private/tmp yeniden başlatmada silinir). Her 15 sn: CPU, swap, boş disk.
# ALARM: CPU>90 art arda 12 ölçüm (3 dk), swap>12000M, disk<8GB → /private/tmp/kuyumcu-serit/ALARM
D=/private/tmp/kuyumcu-serit; mkdir -p $D; Y=0
while true; do
  C=$(ps -A -o %cpu | awk '{s+=$1} END {printf "%.2f", s/'$(sysctl -n hw.ncpu)'}')
  S=$(sysctl vm.swapusage | awk '{print $7}' | tr -d 'M')
  K=$(df -g / | tail -1 | awk '{print $4}')
  echo "$(date '+%H:%M:%S') $C sw=${S}M disk=${K}G" >> $D/cpu.log
  if [ ${C%.*} -gt 90 ]; then Y=$((Y+1)); else Y=0; fi
  [ $Y -ge 12 ] && echo "$(date '+%F %T') CPU ALARM $C" >> $D/ALARM
  [ ${S%.*} -gt 12000 ] && echo "$(date '+%F %T') SWAP ALARM ${S}M" >> $D/ALARM
  [ $K -lt 8 ] && echo "$(date '+%F %T') DISK ALARM ${K}G" >> $D/ALARM
  sleep 15
done
