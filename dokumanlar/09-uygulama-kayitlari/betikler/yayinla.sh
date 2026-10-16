#!/bin/zsh
# QA yayın anlık görüntüsü: Release publish → $HOME/.kuyumcu-yayin (5199 "kuyumcu-qa" bu dizinden koşar).
# Sıra: preview_stop(kuyumcu-qa) → zsh betikler/yayinla.sh → preview_start "kuyumcu-qa". Kullanım: zsh betikler/yayinla.sh [etiket]
KOK=/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery
# HEDEF: TEK KAYNAK (P-42). Yol artık bu dosyada YAZILI DEĞİL; yayin-yolu.sh
# tanımlar (varsayılan $HOME/.kuyumcu-yayin, KUYUMCU_YAYIN_DIZINI hâlâ üstün).
# Böylece yayın oturumdan bağımsızdır ve qa-sunucu.sh/nobetci.sh/yayin-butunluk.sh
# ile AYNI dizini konuşur (tur 8: beş kopya yol, sessiz tutarsızlık).
source "$0:a:h/yayin-yolu.sh"
kuyumcu_yayin_devral
HEDEF="$YAYIN_DIZINI"
LOG="$HEDEF-publish-$(date '+%H%M').log"
# ── YAZMA KİLİDİNİ AÇ (P-42) ────────────────────────────────────────────────
# Önceki başarılı yayın dizini SALT-OKUNUR bırakılır (aşağıdaki a-w). publish
# o dizine yazamazdı → ikinci koşum "Access denied" ile ölürdü. Bu satır kilidi
# yalnız yayın süresince açar; kapanışı envanter doğrulandıktan SONRA yapılır.
[ -d "$HEDEF" ] && chmod -R u+w "$HEDEF" 2>/dev/null
cd "$KOK/dotnet" || exit 2
if dotnet publish Kuyumcu.Api/Kuyumcu.Api.csproj -c Release -o "$HEDEF" --nologo > "$LOG" 2>&1; then
  U=$(grep -c ' warning ' "$LOG"); H=$(grep -c ' error ' "$LOG")
  # ── ÇIKTI BÜTÜNLÜĞÜ (P-41, fail-closed) ─────────────────────────────────────
  # publish'in "başarılı" dönmesi, servis edilecek dosyaların TAM olduğunu
  # KANITLAMAZ: tur 8'de publish yeşildi ama yayında kilavuz.html YOKTU
  # (/kilavuz = 404 0) ve _framework'e yayinla.sh DIŞINDA bir el dokunmuştu.
  # Artık her yayın kendi SHA-256 envanterini taşır ve envanter doğrulanmadan
  # AĞAÇ DAMGASI YAZILMAZ — damgasız yayını qa-sunucu.sh zaten açmaz (exit 9),
  # yani kapı iki uçtan da kapanır.
  BUTUNLUK="$KOK/dokumanlar/09-uygulama-kayitlari/betikler/yayin-butunluk.sh"
  if [ ! -r "$BUTUNLUK" ]; then
    echo "YAYIN BÜTÜNLÜK RET — bütünlük betiği yok: $BUTUNLUK"
    echo "  .agac-damgasi YAZILMADI (fail-closed, P-41)."; exit 1
  fi
  if ! zsh "$BUTUNLUK" --uret "$HEDEF"; then
    echo "YAYIN BÜTÜNLÜK RET — envanter ÜRETİLEMEDİ (çıktı eksik)."
    echo "  .agac-damgasi YAZILMADI (fail-closed, P-41)."; exit 1
  fi
  if ! zsh "$BUTUNLUK" --dogrula "$HEDEF"; then
    echo "YAYIN BÜTÜNLÜK RET — çıktı eksik/bozuk (ihlal eden yollar yukarıda)."
    echo "  .agac-damgasi YAZILMADI (fail-closed, P-41)."; exit 1
  fi
  # ── AĞAÇ DAMGASI (B-2): bu anlık görüntünün hangi kaynaktan üretildiğini
  #    yanına yaz. qa-sunucu.sh açılırken damgayı ŞİMDİKİ ağaçla karşılaştırır ve
  #    uyuşmazsa 5199'u AÇMAZ. İçerik karması kullanılır (mtime DEĞİL — K-20
  #    iCloud eşitlemesi mtime'ları oynatıyor ve sahte "bayat" üretirdi).
  find "$KOK/dotnet/Kuyumcu.Api" "$KOK/dotnet/Kuyumcu.Web" "$KOK/dotnet/Kuyumcu.Core" \
       \( -name '*.cs' -o -name '*.razor' -o -name '*.csproj' \) 2>/dev/null \
    | grep -v '/obj/\|/bin/' | sort | xargs shasum 2>/dev/null | shasum | awk '{print $1}' > "$HEDEF/.agac-damgasi"
  echo "  ağaç damgası: $(cut -c1-12 "$HEDEF/.agac-damgasi")… → $HEDEF/.agac-damgasi"
  # ── DAMGA ZORUNLULUK ANAHTARI (P-02 düzeltme turu) ──────────────────────────
  # İlk başarılı yayından sonra "damgasız anlık görüntü" artık hoş görülmez:
  # qa-sunucu.sh bu dosyayı görünce damgasız hâli UYUŞMAZLIK sayar ve 5199'u
  # AÇMAZ (exit 9). Depoda sürümlüdür → oturumdan bağımsızdır.
  ZORUNLU="$KOK/dokumanlar/09-uygulama-kayitlari/ham/YAYIN-DAMGA-ZORUNLU"
  if [ ! -f "$ZORUNLU" ]; then
    printf 'yayinla.sh ilk basarili kosumu: %s\nBu dosya VARSA damgasiz yayin fail-closed kapanir (qa-sunucu.sh exit 9).\n' "$(date '+%Y-%m-%d %H:%M')" > "$ZORUNLU"
    echo "  damga zorunluluğu AÇILDI → $ZORUNLU"
  fi
  # ── ENVANTER ZORUNLULUK ANAHTARI (P-41 — DAMGA anahtarının ikizi) ───────────
  # İlk başarılı bütünlük koşumundan sonra "envantersiz anlık görüntü" artık hoş
  # görülmez: qa-sunucu.sh bu dosyayı görünce envantersiz hâli fail-closed sayar
  # ve 5199'u AÇMAZ (exit 15). Depoda sürümlüdür → oturumdan bağımsızdır.
  ENV_ZORUNLU="$KOK/dokumanlar/09-uygulama-kayitlari/ham/YAYIN-ENVANTER-ZORUNLU"
  if [ ! -f "$ENV_ZORUNLU" ]; then
    printf 'yayin-butunluk.sh ilk basarili kosumu: %s\nBu dosya VARSA envantersiz yayin fail-closed kapanir (qa-sunucu.sh exit 15).\n' "$(date '+%Y-%m-%d %H:%M')" > "$ENV_ZORUNLU"
    echo "  envanter zorunluluğu AÇILDI → $ENV_ZORUNLU"
  fi
  # ── YAZMA KİLİDİNİ KAPAT (P-42, fail-closed'in ikinci ucu) ─────────────────
  # ÖLÇÜLEN ARIZA (23/09 12:05): yayına `yayinla.sh` DIŞINDA bir el dokundu —
  # iki .wasm dosyası tur KOŞARKEN değişti, publish günlüğü YOKTU, KİMİN
  # dokunduğu doğrulanamadı; grup 1'in 6 rotası bozuk yayına denk geldi.
  # Bu satır faili bulmaz, KAPISIZLIĞI kapatır: envanter doğrulandıktan sonra
  # yayın SALT-OKUNUR olur; elle yama artık sessizce başarılı olamaz.
  # Sıra BAĞLAYICI: envanter+damga YAZILDIKTAN sonra, aksi hâlde kendi
  # damgamızı yazamayız.
  chmod -R a-w "$HEDEF" 2>/dev/null
  if [ -w "$HEDEF/wwwroot/index.html" ]; then
    echo "YAYIN KİLİT RET — salt-okunur yapılamadı: $HEDEF hâlâ yazılabilir."; exit 1
  fi
  echo "  yayın SALT-OKUNUR kilitlendi (chmod -R a-w) — elle yama artık sessiz geçmez"
  echo "YAYIN HAZIR ${1:+($1) }$(date '+%H:%M') → $HEDEF · uyarı satırı: $U · hata satırı: $H · günlük: $LOG"
  ls -la "$HEDEF/Kuyumcu.Api.dll" | awk '{print "  Kuyumcu.Api.dll " $6 " " $7 " " $8 " " $5 " bayt"}'
  [ -d "$HEDEF/wwwroot/_framework" ] && echo "  wwwroot/_framework var (Blazor WASM istemcisi dâhil)" || echo "  UYARI: wwwroot/_framework YOK"
else
  echo "YAYIN BAŞARISIZ — son 20 satır:"; tail -n 20 "$LOG"; exit 1
fi
