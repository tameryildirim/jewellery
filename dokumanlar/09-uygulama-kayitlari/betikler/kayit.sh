#!/bin/zsh
# Durum dosyasına GERÇEK saatle damgalı girdi ekler. Kullanım: zsh betikler/kayit.sh "metin" (metin markdown olabilir)
D=/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari/ORKESTRATOR-DURUM.md
printf -- '- **%s** — %s\n' "$(date '+%H:%M')" "$1" >> "$D" && tail -n 1 "$D" | cut -c1-100
