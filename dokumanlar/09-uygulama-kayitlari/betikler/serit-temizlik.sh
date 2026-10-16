#!/bin/bash
# ═══════════════════════════════════════════════════════════════════════════
# ŞERİT GEÇİCİ DİZİN TEMİZLİĞİ (kurucu 29.09 00:10 "otomatik temizlik kur")
#
# NEDEN: 28.09 20:45 disk %100 doldu, Docker çöktü. Şeritler her turda
# /private/tmp/kuyumcu-serit altına proje kopyası (agac/kok), başsız Chrome
# profili ve bin/obj bırakıyor (tur başına ~2-4 GB).
#
# NE SİLER: YALNIZ /private/tmp/kuyumcu-serit altında, adı agac|agac2|kok|kopya|yayin|
#   profil*|prof-*|chrome*|bin|obj olan (prof-* 01.10 eklendi: basliksiz Chrome profilleri), 45 dakikadır DOKUNULMAMIŞ dizinler.
# NE SİLMEZ: ekran görüntüleri, trx, günlükler, raporlar, proje ağacı; ÇALIŞAN
#   bir sürecin (cwd ya da komut satırı) kullandığı şerit alt dizini
#   (ilk iki düzey, ör. OLCEK-1/v14) tümüyle korunur.
#
# Kullanım (kurucu terminalinde bir kez):
#   nohup bash .../betikler/serit-temizlik.sh > /dev/null 2>&1 &
# Durdurmak: pkill -f serit-temizlik.sh      Günlük: /private/tmp/kuyumcu-serit/temizlik.log
# Saatte bir koşar; boş disk 25 GB'ın altına inerse 10 dakikada bir koşar.
# ═══════════════════════════════════════════════════════════════════════════
KOK=/private/tmp/kuyumcu-serit
LOG=$KOK/temizlik.log

bos_gb() { df -g /System/Volumes/Data | tail -1 | awk '{print $4}'; }

temizle() {
  [ -d "$KOK" ] || return 0
  cd "$KOK" || return 0
  # Çalışan süreçlerin kullandığı şerit alt dizinleri (ilk iki düzey)
  aktif=$( { lsof -d cwd 2>/dev/null | awk '{print $NF}'; ps -Ao args 2>/dev/null | tr ' =' '\n\n'; } \
    | grep "^$KOK/" | sed -e "s|^$KOK/||" -e "s|[;\"')]*$||" | awk -F/ 'NF>=2{print $1"/"$2} NF==1{print $1}' | sort -u)
  once=$(bos_gb); n=0
  while IFS= read -r -d '' d; do
    rel=${d#./}; ust=$(echo "$rel" | awk -F/ '{print $1"/"$2}')
    if printf '%s\n' "$aktif" | grep -qxF "$ust"; then continue; fi
    if printf '%s\n' "$aktif" | grep -qxF "${rel%%/*}" && [ "$ust" = "$rel" ]; then continue; fi
    rm -rf -- "$KOK/$rel" && n=$((n+1))
  done < <(find . -mindepth 1 -maxdepth 5 -type d \( -name agac -o -name agac2 -o -name kok -o -name kopya -o -name yayin -o -name 'profil*' -o -name 'prof-*' -o -name 'chrome*' -o -name bin -o -name obj \) -mmin +45 -prune -print0 2>/dev/null)
  echo "$(date '+%Y-%m-%d %H:%M') silinen dizin: $n · boş disk: ${once} GB → $(bos_gb) GB · korunan: $(printf '%s ' $aktif)" >> "$LOG"
}

while true; do
  temizle
  if [ "$(bos_gb)" -lt 25 ]; then sleep 600; else sleep 3600; fi
done
