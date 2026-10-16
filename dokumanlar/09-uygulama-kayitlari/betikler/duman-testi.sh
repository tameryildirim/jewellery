#!/bin/zsh
# ═════════════════════════════════════════════════════════════════════════════
# P-43 — CANLI DUMAN KAPISI (yayın gerçekten açılıyor mu?)
#
# NEDEN VAR (tur 8'de ÖLÇÜLEN arıza, Anayasa md.1 — iddia değil kanıt):
#   • Yayın anlık görüntüsü EKSİK dosya servis etti. `wwwroot/kilavuz.html`
#     kaynakta 60.845 bayt VAR, yayında YOKTU → canlı `/kilavuz.html` = 404/0.
#   • `_framework` altındaki varlıkların bir kısmı 404 dönünce tarayıcı konsolu
#     "SRI's integrity checks failed" → "Error in mono_download_assets" →
#     "Failed to start platform" verdi ve uygulama HİÇ önyüklenmedi.
#   • Sonuç: QA turu 8'de grup 1'in 6 rotası (/cikti/{Tur}/{Id}, /kasalar,
#     /sayim, /banka, /pos/tahsilatlar, /yetkiler) hiç açılamadı ve 1/1
#     puanlandı. ÖLÇÜM ARACI bozukken ürün puanı konuşulamaz: tur 8
#     ortalamaları (3,17 / 2,74) KARŞILAŞTIRILABİLİR DEĞİLDİR.
#
#   Bu betik FAİLİ bulmaz (23/09 publish günlüğü yok, KİMİN yazdığı
#   DOĞRULANAMADI); KAPISIZLIĞI kapatır. Bir daha eksik yayın sessizce
#   servis edilemez: kapı kırmızıysa `kapi.sh` "TEMİZ" yazmaz.
#
# İKİ KATMAN (ikisi de geçmeden çıkış 0 olmaz):
#   (A) DOSYA katmanı  — yayın dizini diskte eksiksiz mi (sunucu gerekmez)
#   (B) CANLI katmanı  — sunucu o dosyaları GERÇEKTEN 200 ile veriyor mu
#   Sunucu ayakta değilse çıkış 4'tür; "atlandı" sayılmaz (fail-closed).
#
# ÇIKIŞ KODLARI (kendi ad alanı):
#   0 temiz · 2 yayın dizini yok · 3 boot.json yok · 4 canlı sunucu yok
#   5 python3 yok · 6 duman ihlali · 64 kullanım hatası
#
# KULLANIM:
#   zsh betikler/duman-testi.sh [--taban URL] [--yayin DIZIN] [--liste]
# ORTAM:
#   KUYUMCU_DUMAN_TABAN   → canlı taban adres (varsayılan http://localhost:5199)
#   KUYUMCU_YAYIN_DIZINI  → yayın dizini (varsayılan: yayin-yolu.sh · P-42)
# ═════════════════════════════════════════════════════════════════════════════
set -u

KOK=/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery
BETIKLER="$0:a:h"
# Yayın dizini TEK KAYNAKTAN gelir (P-42); bu dosyada yol literali yoktur.
source "$BETIKLER/yayin-yolu.sh"

TABAN=${KUYUMCU_DUMAN_TABAN:-http://localhost:5199}
HEDEF=$YAYIN_DIZINI
KIPLISTE=0

while [ $# -gt 0 ]; do
  case "$1" in
    --taban) TABAN=${2:-}; shift 2 ;;
    --yayin) HEDEF=${2:-}; shift 2 ;;
    --liste) KIPLISTE=1; shift ;;
    -h|--yardim)
      echo "Kullanım: zsh duman-testi.sh [--taban URL] [--yayin DIZIN] [--liste]"; exit 0 ;;
    *) echo "✗ bilinmeyen seçenek: $1"; exit 64 ;;
  esac
done
TABAN=${TABAN%/}

# ── KAPSAM: tur 8'de ENGELLENDİ dönen ALTI rota (elle yazıldı çünkü kapının
#    KANITI budur: bu altı rota bir daha sessizce ölmeyecek). /cikti/{Tur}/{Id}
#    parametrelidir; SPA yedek yolu için somut bir örnek kullanılır.
ENGELLENEN_ROTALAR=(/cikti/satis/1 /kasalar /sayim /banka /pos/tahsilatlar /yetkiler)

IHLAL=0
GECEN=0
not()   { printf '%s\n' "$1"; }
gecti() { GECEN=$((GECEN+1)); printf '  ✓ %s\n' "$1"; }
kaldi() { IHLAL=$((IHLAL+1)); printf '  ✗ %s\n' "$1"; }

# HTTP durum + gövde boyutu (gövde OKUNUR; -o /dev/null ile körlemesine
# geçmek YASAK — 404 gövdesi de 200 gibi "boyut" verebilir).
http_kod_bayt() { curl -s -o /dev/null -w '%{http_code} %{size_download}' --max-time 15 "$1" 2>/dev/null || echo "000 0"; }
# HEAD: durum + Content-Length (0 bayt servis edilen varlık tur 8'in kök nedeni).
head_kod_uzunluk() {
  local h; h=$(curl -sI --max-time 15 "$1" 2>/dev/null) || { echo "000 -"; return; }
  local kod uzunluk
  kod=$(printf '%s' "$h" | awk 'NR==1 {print $2}')
  uzunluk=$(printf '%s' "$h" | awk 'BEGIN{IGNORECASE=1} /^Content-Length:/ {gsub(/\r/,"",$2); print $2; exit}')
  echo "${kod:-000} ${uzunluk:--}"
}

echo "═══ P-43 CANLI DUMAN KAPISI ═══"
echo "  yayın dizini : $HEDEF"
echo "  canlı taban  : $TABAN"

# ── ÖN KOŞULLAR (fail-closed) ────────────────────────────────────────────────
command -v python3 >/dev/null 2>&1 || { echo "✗ python3 yok — boot.json ayrıştırılamaz"; exit 5; }
[ -d "$HEDEF" ] || { echo "✗ YAYIN DİZİNİ YOK: $HEDEF — önce: zsh betikler/yayinla.sh"; exit 2; }
BOOT="$HEDEF/wwwroot/_framework/blazor.boot.json"
[ -f "$BOOT" ] || { echo "✗ blazor.boot.json YOK: $BOOT"; exit 3; }

# Canlı sunucu ayakta mı? Değilse ATLAMA YOK — kapı kapanır.
KOK_KOD=$(http_kod_bayt "$TABAN/" | awk '{print $1}')
if [ "$KOK_KOD" = "000" ]; then
  echo "✗ CANLI SUNUCU YANIT VERMİYOR: $TABAN (çıkış 4 — 'atlandı' sayılmaz)"
  echo "  Başlatmak için: zsh $BETIKLER/qa-sunucu.sh"
  exit 4
fi

# ── boot.json varlık adları (satelliteResources kapsam dışı: iç içe sözlüktür
#    ve düz `_framework/<ad>` yoluna oturmaz — yanlış yol üretmektense dışarıda)
VARLIKLAR=$(python3 - "$BOOT" <<'PY'
import json, sys
r = json.load(open(sys.argv[1]))["resources"]
for k, v in r.items():
    if not isinstance(v, dict):
        continue
    for ad, sha in v.items():
        if isinstance(sha, str):
            print(ad)
PY
)
VARLIK_SAYISI=$(printf '%s\n' "$VARLIKLAR" | grep -c . )

if [ "$KIPLISTE" = 1 ]; then
  printf '%s\n' "$VARLIKLAR"; exit 0
fi

# ═══ test_kilavuz_html_200_ve_60kb ═══════════════════════════════════════════
# /kilavuz.html hem DİSKTE hem CANLIDA olmalı ve 60.000 bayttan büyük olmalı.
# Eşik: kaynak 60.845 bayt; 60.000 altı = içerik kırpılmış/yerine boş dosya konmuş.
test_kilavuz_html_200_ve_60kb() {
  echo "── test_kilavuz_html_200_ve_60kb"
  local dosya="$HEDEF/wwwroot/kilavuz.html" bayt=0
  if [ -f "$dosya" ]; then bayt=$(stat -f %z "$dosya" 2>/dev/null || echo 0); fi
  if [ "$bayt" -ge 60000 ]; then
    gecti "dosya: wwwroot/kilavuz.html $bayt bayt (>= 60000)"
  else
    kaldi "kilavuz.html: yayın dizininde YOK ya da $bayt bayt (>= 60000 olmalı) — $dosya"
  fi
  local s kod boyut
  s=$(http_kod_bayt "$TABAN/kilavuz.html"); kod=${s% *}; boyut=${s#* }
  if [ "$kod" = "200" ] && [ "$boyut" -ge 60000 ]; then
    gecti "canlı: /kilavuz.html $kod / $boyut bayt"
  else
    kaldi "kilavuz.html: $kod / $boyut bayt"
  fi
}

# ═══ test_index_html_200 ═════════════════════════════════════════════════════
test_index_html_200() {
  echo "── test_index_html_200"
  local s kod boyut
  s=$(http_kod_bayt "$TABAN/index.html"); kod=${s% *}; boyut=${s#* }
  if [ "$kod" = "200" ] && [ "$boyut" -gt 0 ]; then
    gecti "canlı: /index.html $kod / $boyut bayt"
  else
    kaldi "index.html: $kod / $boyut bayt"
  fi
}

# ═══ test_bootjson_200 ═══════════════════════════════════════════════════════
test_bootjson_200() {
  echo "── test_bootjson_200"
  local s kod boyut
  s=$(http_kod_bayt "$TABAN/_framework/blazor.boot.json"); kod=${s% *}; boyut=${s#* }
  if [ "$kod" = "200" ] && [ "$boyut" -gt 0 ]; then
    gecti "canlı: /_framework/blazor.boot.json $kod / $boyut bayt"
  else
    kaldi "blazor.boot.json: $kod / $boyut bayt"
  fi
}

# ═══ test_bootjson_varliklari_content_length_sifirdan_buyuk ══════════════════
# TUR 8'İN KÖK NEDENİ BURADA YAKALANIR: 404 ya da 0 baytlık tek bir varlık
# "Failed to start platform" demektir; uygulama HİÇ açılmaz.
test_bootjson_varliklari_content_length_sifirdan_buyuk() {
  echo "── test_bootjson_varliklari_content_length_sifirdan_buyuk"
  local iyi=0 kotu=0 ad s kod uz
  while IFS= read -r ad; do
    [ -z "$ad" ] && continue
    s=$(head_kod_uzunluk "$TABAN/_framework/$ad"); kod=${s% *}; uz=${s#* }
    if [ "$kod" = "200" ] && [ "$uz" != "-" ] && [ "$uz" -gt 0 ] 2>/dev/null; then
      iyi=$((iyi+1))
    else
      kotu=$((kotu+1))
      kaldi "_framework/$ad: $kod / Content-Length $uz"
    fi
  done <<< "$VARLIKLAR"
  if [ "$kotu" = 0 ]; then
    gecti "$iyi/$VARLIK_SAYISI varlık — Content-Length > 0"
  else
    not "  → $iyi/$VARLIK_SAYISI varlık sağlam, $kotu varlık BOZUK (önyükleme kırılır)"
  fi
}

# ═══ test_engellenen_alti_rota_200 ═══════════════════════════════════════════
# Tur 8'de bu altı rota HİÇ açılamadı. Kapının asıl kanıtı bu satırdır.
test_engellenen_alti_rota_200() {
  echo "── test_engellenen_alti_rota_200"
  local r s kod boyut kotu=0
  for r in $ENGELLENEN_ROTALAR; do
    s=$(http_kod_bayt "$TABAN$r"); kod=${s% *}; boyut=${s#* }
    if [ "$kod" = "200" ] && [ "$boyut" -gt 0 ]; then
      gecti "rota $r → $kod / $boyut bayt"
    else
      kotu=$((kotu+1)); kaldi "rota $r → $kod / $boyut bayt"
    fi
  done
  [ "$kotu" = 0 ] && not "  → tur 8'de engellenen 6 rotanın hepsi 200"
}

# ═══ test_kapi_duman_kirmiziyken_temiz_yazmaz ════════════════════════════════
# Kapı çağrısı yutulursa (|| true, ||:, `set +e` sonrası yok sayma) duman testi
# süs olur. Bu kontrol KAYNAK metnidir: kapi.sh duman testini çağırıyor mu,
# çıkışını bir değişkene alıp SONUCA bağlıyor mu, ve kırmızıda denetim
# çıktısından (yani "TEMİZ" kelimesinden) ÖNCE duruyor mu.
test_kapi_duman_kirmiziyken_temiz_yazmaz() {
  echo "── test_kapi_duman_kirmiziyken_temiz_yazmaz"
  local kapi="$BETIKLER/kapi.sh"
  if [ ! -f "$kapi" ]; then kaldi "kapi.sh bulunamadı: $kapi"; return; fi
  local cagri
  cagri=$(grep -n 'duman-testi\.sh' "$kapi" | grep -v '^[0-9]*:[[:space:]]*#')
  if [ -z "$cagri" ]; then
    kaldi "kapi.sh duman-testi.sh'i HİÇ çağırmıyor"
    return
  fi
  gecti "kapi.sh duman-testi.sh'i çağırıyor"
  if printf '%s\n' "$cagri" | grep -qE '\|\|[[:space:]]*(true|:)'; then
    kaldi "kapi.sh duman testi çıkışını '|| true' ile YUTUYOR"
  else
    gecti "duman testi çıkışı yutulmuyor (|| true yok)"
  fi
  if grep -qE '^[[:space:]]*DUMAN=\$\?' "$kapi" && grep -qE '\[ "\$DUMAN" != "0" \]' "$kapi"; then
    gecti "çıkış kodu DUMAN değişkenine alınıp sonuca bağlanıyor"
  else
    kaldi "kapi.sh duman çıkış kodunu sonuca bağlamıyor (DUMAN=\$? / != 0 kontrolü yok)"
  fi
  # Sıra kanıtı: duman aşaması denetim aşamasından ÖNCE ve kırmızıda exit var.
  local sat_duman sat_denetim
  sat_duman=$(grep -nE '^[[:space:]]*DUMAN=\$\?' "$kapi" | head -1 | cut -d: -f1)
  sat_denetim=$(grep -nE '^\./denetim\.sh' "$kapi" | head -1 | cut -d: -f1)
  if [ -n "$sat_duman" ] && [ -n "$sat_denetim" ] && [ "$sat_duman" -lt "$sat_denetim" ]; then
    gecti "duman aşaması denetim.sh'ten ÖNCE (satır $sat_duman < $sat_denetim) → kırmızıda 'TEMİZ' basılmaz"
  else
    kaldi "duman aşaması denetim.sh'ten önce değil (duman=$sat_duman denetim=$sat_denetim)"
  fi
}

# ═══ test_engellenen_grup_ortalamadan_ayrilir (qa-turu.js) ═══════════════════
# Ölçüm bütünlüğü: tamamı önyükleme hatasıyla ENGELLENDİ dönen bir grup
# ortalamaya KATILMAZ; tur raporu iki ortalamayı ayrı ayrı basar.
test_engellenen_grup_ortalamadan_ayrilir() {
  echo "── test_engellenen_grup_ortalamadan_ayrilir (qa-ozet.mjs --ozet · qa-turu.js ikizi)"
  local tur8="$BETIKLER/../ham/qa-turu-8.json"
  if [ ! -f "$tur8" ]; then kaldi "tur 8 ham verisi yok: $tur8"; return; fi
  # ÖLÇÜLEN KOD qa-turu.js'in KENDİ bloğudur (turun gerçekte koşturduğu kod):
  # blok saf olduğu için (import/require/yan etki yok) node'a olduğu gibi verilir.
  # CLI (qa-ozet.mjs) çıktısı da alınır ve BİREBİR aynı olmalıdır.
  local cikti cli blok
  blok=$(awk '/P-43-ORTALAMA-BASLA/,/P-43-ORTALAMA-BITIS/' "$BETIKLER/qa-turu.js")
  if [ -z "$blok" ]; then kaldi "qa-turu.js'te P-43 ortalama bloğu YOK"; return; fi
  cikti=$(printf '%s\n%s\n' "$blok" \
    'const v = JSON.parse(require("fs").readFileSync(process.argv[process.argv.length - 1], "utf8")); console.log(ozetMetni(v.tur ?? "?", v.sonuclar || []))' \
    | node - "$tur8" 2>&1) || { kaldi "qa-turu.js bloğu node ile çalışmadı"; printf '%s\n' "$cikti" | tail -5; return; }
  printf '%s\n' "$cikti" | sed 's/^/    /'
  cli=$(node "$BETIKLER/qa-ozet.mjs" --ozet "$tur8" 2>&1) || { kaldi "qa-ozet.mjs --ozet çıkış != 0"; printf '%s\n' "$cli" | tail -5; return; }
  if [ "$cli" = "$cikti" ]; then
    gecti "qa-turu.js bloğu ile qa-ozet.mjs --ozet aynı metni basıyor"
  else
    kaldi "qa-turu.js bloğu ile qa-ozet.mjs --ozet FARKLI metin basıyor"
  fi
  if printf '%s' "$cikti" | grep -q 'tüm rotalar' && printf '%s' "$cikti" | grep -q 'ölçülebilen rotalar'; then
    gecti "iki ortalama ayrı ayrı basıldı"
  else
    kaldi "iki ortalama ayrı basılmıyor"
  fi
  if printf '%s' "$cikti" | grep -q 'ölçülemedi (ortalamaya KATILMADI): grup 1 · 6 rota'; then
    gecti "engellenen grup 'ölçülemedi' olarak raporlandı"
  else
    kaldi "engellenen grup ayrı raporlanmıyor"
  fi
  if printf '%s' "$cikti" | grep -q '3,50' && printf '%s' "$cikti" | grep -q '3,00'; then
    gecti "tur 8 ölçülebilen ortalaması 3,50 / 3,00"
  else
    kaldi "tur 8 ölçülebilen ortalaması 3,50 / 3,00 çıkmadı"
  fi
}

# ═══ test_ortalama_blogu_iki_dosyada_ayni ════════════════════════════════════
# qa-turu.js koşum sandbox'ında çalışır (gövdesi sarılır, `return` içerir,
# import/require YOK) → node ile çalıştırılamaz. Bu yüzden CLI qa-ozet.mjs'de,
# hesap bloğu İKİ DOSYADA KOPYA. Kopya sessizce ayrışmasın diye BAYT BAYT
# karşılaştırılır: ayrışırsa tur raporu ile CLI farklı rakam basardı.
test_ortalama_blogu_iki_dosyada_ayni() {
  echo "── test_ortalama_blogu_iki_dosyada_ayni"
  local a b
  a=$(awk '/P-43-ORTALAMA-BASLA/,/P-43-ORTALAMA-BITIS/' "$BETIKLER/qa-turu.js")
  b=$(awk '/P-43-ORTALAMA-BASLA/,/P-43-ORTALAMA-BITIS/' "$BETIKLER/qa-ozet.mjs")
  if [ -z "$a" ] || [ -z "$b" ]; then
    kaldi "ortalama bloğu bulunamadı (qa-turu.js: ${#a} bayt, qa-ozet.mjs: ${#b} bayt)"
  elif [ "$a" = "$b" ]; then
    gecti "ortalama bloğu iki dosyada birebir aynı (${#a} bayt)"
  else
    kaldi "ortalama bloğu AYRIŞMIŞ — qa-turu.js ${#a} bayt, qa-ozet.mjs ${#b} bayt"
  fi
}

# ═══ test_canli_yayin_diskteki_yayinla_ayni ══════════════════════════════════
# 23/09 P-43 ÖLÇÜMÜ: dosya katmanı ~/.kuyumcu-yayin'i (P-42) doğruluyor, oysa
# 5199 süreci hâlâ ESKİ oturum scratchpad'indeki anlık görüntüden koşuyordu
# (canlı boot.json karması ≠ diskteki). "Doğrulanan dizin" ile "servis edilen
# dizin" ayrışırsa dosya katmanının yeşili YALANDIR → ayrı ihlal.
test_canli_yayin_diskteki_yayinla_ayni() {
  echo "── test_canli_yayin_diskteki_yayinla_ayni"
  local canli disk
  canli=$(curl -s --max-time 15 "$TABAN/_framework/blazor.boot.json" 2>/dev/null | shasum -a 256 | awk '{print $1}')
  disk=$(shasum -a 256 "$BOOT" | awk '{print $1}')
  if [ -n "$canli" ] && [ "$canli" = "$disk" ]; then
    gecti "canlı boot.json = diskteki yayın boot.json (${disk[1,12]}…)"
  else
    kaldi "canlı sunucu BAŞKA bir yayını servis ediyor: canlı boot.json ${canli[1,12]}… ≠ disk ${disk[1,12]}… ($BOOT) — sunucu yayin-yolu.sh'in dizininden yeniden açılmalı (qa-sunucu.sh)"
  fi
}

test_kilavuz_html_200_ve_60kb
test_canli_yayin_diskteki_yayinla_ayni
test_index_html_200
test_bootjson_200
test_bootjson_varliklari_content_length_sifirdan_buyuk
test_engellenen_alti_rota_200
test_kapi_duman_kirmiziyken_temiz_yazmaz
test_engellenen_grup_ortalamadan_ayrilir
test_ortalama_blogu_iki_dosyada_ayni

echo "═══ SONUÇ: geçen $GECEN · ihlal $IHLAL ═══"
if [ "$IHLAL" != "0" ]; then
  echo "✗ DUMAN KAPISI KIRMIZI — yayın eksik/bozuk servis ediyor. Ölçüm aracı güvenilmez."
  exit 6
fi
echo "✓ DUMAN KAPISI TEMİZ"
exit 0
