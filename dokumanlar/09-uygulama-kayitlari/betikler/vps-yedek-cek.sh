#!/bin/zsh
# 02.11 — VPS (5.10.220.150) şifreli yedeklerini Mac'e çeker: DIŞ KOPYA (sunucu ölse bile veri bizde).
# launchd: com.bygatech.vps-yedek (her gece 04:10) — kurucu kurar (bkz. com.bygatech.vps-yedek.plist).
# Günlük: ~/Library/Logs/bygatech-vps-yedek.log
# Anahtar: ~/.ssh/bygatech_vps · Hedef: ~/.kuyumcu-yedek/vps/ · 60 günden eski yerel kopyalar budanır.
# Yedekler sunucunun Yedek__SifrelemeAnahtari ile şifrelidir; o anahtarın kopyası ~/.kuyumcu/vps-yedek-anahtari (600).
HEDEF="$HOME/.kuyumcu-yedek/vps"; mkdir -p "$HEDEF"
rsync -az --timeout=120 -e "ssh -o BatchMode=yes -o ConnectTimeout=20 -i $HOME/.ssh/bygatech_vps" \
  root@5.10.220.150:/var/lib/kuyumcu/yedek-dis/ "$HEDEF/" \
  && echo "$(date '+%F %T') OK $(ls "$HEDEF" | grep -c gpg) dosya, son: $(ls -t "$HEDEF" | head -1)" \
  || echo "$(date '+%F %T') HATA rsync çıkış $?"
python3 - "$HEDEF" <<'PY'
import os, sys, time, glob
esik = time.time() - 60*86400
for f in glob.glob(os.path.join(sys.argv[1], '*.gpg')):
    if os.path.getmtime(f) < esik:
        os.remove(f); print('budandı:', os.path.basename(f))
PY
