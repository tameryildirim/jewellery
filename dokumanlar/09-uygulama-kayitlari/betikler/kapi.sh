#!/bin/zsh
KAYIT_KOK=/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari
# Orkestratör kapısı: derleme (0 hata/0 uyarı) → dotnet/denetim.sh (TEMİZ) → tam test süiti (trx, 0 başarısız).
# Kullanım: zsh betikler/kapi.sh [çıktı-dizini]
KOK=/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery
# KAPI ÇIKTI KÖKÜ: oturuma özgü scratchpad DEĞİL (P-42). Kapı kanıtı oturum
# bitince kaybolmamalı; yol tek kaynaktan türer, $1 hâlâ üstündür.
source "$KAYIT_KOK/betikler/yayin-yolu.sh"
OUT=${1:-$KUYUMCU_KAPI_CIKTI_KOKU/kapi-$(date +%Y%m%d-%H%M)}
mkdir -p "$OUT"
cd "$KOK/dotnet" || exit 2
# ═══ 0/4 TEK_EL_IHLAL (Bertan K5 / BK-05) ════════════════════════════════════
# PLAN § 7'nin "aynı dosyaya iki el değmez" kuralı bugüne dek yalnız belgedeydi.
# Artık MAKİNE kapısı: ham/TEK-EL-KILIT defterinde aynı zinciri iki farklı P-xx
# açmışsa kapı derlemeye bile geçmeden KAPANIR (fail-closed).
# ÖLÇÜM: defterdeki (paket, zincir) çiftleri → zincir başına farklı paket sayısı > 1 olanlar.
KILIT="$KAYIT_KOK/ham/TEK-EL-KILIT"
if [ ! -f "$KILIT" ]; then
  echo "== 0/4 TEK_EL_IHLAL == KİLİT DEFTERİ YOK: $KILIT — kapı açılmaz (fail-closed)"; exit 3
fi
TEK_EL_IHLAL=$(grep -vE '^[[:space:]]*(#|$)' "$KILIT" \
  | awk '{ paket=$1; $1=""; sub(/^[[:space:]]+/,""); zincir=$0; if (!(zincir SUBSEP paket in g)) { g[zincir,paket]=1; n[zincir]++ } } END { i=0; for (z in n) if (n[z] > 1) i++; print i+0 }')
echo "== 0/4 TEK_EL_IHLAL = $TEK_EL_IHLAL (0 olmalı) =="
if [ "$TEK_EL_IHLAL" != "0" ]; then
  echo "  ✗ AYNI ZİNCİRİ BİRDEN FAZLA PAKET AÇMIŞ — yaz-seri ihlali:"
  grep -vE '^[[:space:]]*(#|$)' "$KILIT" \
    | awk '{ paket=$1; $1=""; sub(/^[[:space:]]+/,""); zincir=$0; if (!(zincir SUBSEP paket in g)) { g[zincir,paket]=1; n[zincir]++; p[zincir]=p[zincir] " " paket } } END { for (z in n) if (n[z] > 1) printf "      → %s :%s\n", z, p[z] }'
  echo "  Çözüm: biten paket kendi satırlarını defterden çıkarsın ($KILIT)."
  exit 3
fi

echo "== 1/4 dotnet build Kuyumcu.sln =="
dotnet build Kuyumcu.sln --nologo > "$OUT/build.log" 2>&1; B=$?
echo "build exit=$B"; grep -E '[0-9]+ (Hata|Uyarı|Error|Warning)|error [A-Z]+[0-9]+|warning [A-Z]+[0-9]+' "$OUT/build.log" | sort | uniq -c | tail -6
# ═══ 1b/4 CANLI DUMAN KAPISI (P-43) ═════════════════════════════════════════
# TUR 8 GERÇEĞİ: yayın eksik dosya servis etti (/kilavuz.html 404, _framework
# bozuk → "Failed to start platform"), grup 1'in 6 rotası hiç açılmadı ve
# kapı yine de yeşil yazılabiliyordu. Artık duman kırmızıysa kapı BURADA
# kapanır: denetim.sh'e hiç geçilmez, yani çıktıda "TEMİZ" kelimesi OLUŞMAZ.
# Çıkış kodu YUTULMAZ (|| true yasak — duman-testi.sh::test_kapi_duman_kirmiziyken_temiz_yazmaz ölçer).
echo "== 1b/4 duman-testi.sh (canlı yayın: ${KUYUMCU_DUMAN_TABAN:-http://localhost:5199}) =="
zsh "$KAYIT_KOK/betikler/duman-testi.sh" > "$OUT/duman.log" 2>&1
DUMAN=$?
echo "duman exit=$DUMAN"; grep -E '✗|SONUÇ|varlık — Content-Length' "$OUT/duman.log" | head -20
if [ "$DUMAN" != "0" ]; then
  echo "== SONUÇ: DUMAN KAPISI KIRMIZI (exit=$DUMAN) — yayın eksik/bozuk servis ediyor; kapı KAPANDI, denetim/test koşturulmadı · kayıt: $OUT/duman.log =="
  exit 7
fi
echo "== 2/4 denetim.sh =="
./denetim.sh > "$OUT/denetim.log" 2>&1; D=$?
echo "denetim exit=$D"; tail -4 "$OUT/denetim.log"
echo "== 3/4 dotnet test (tam süit, trx) =="
dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo --logger "trx;LogFileName=kapi.trx" --results-directory "$OUT" > "$OUT/test.log" 2>&1; T=$?
echo "test exit=$T"; grep -E 'Passed!|Failed!|Geçti!|Başarısız!|Toplam test|Total tests|Başarısız: |Failed: ' "$OUT/test.log" | tail -4
if [ -f "$OUT/kapi.trx" ]; then
  echo "trx özet:"; grep -o '<Counters [^/]*/>' "$OUT/kapi.trx" | head -1
  grep -o 'testName="[^"]*" [^>]*outcome="Failed"' "$OUT/kapi.trx" | sed 's/ computerName.*outcome/ outcome/' | head -20
fi
echo "== SONUÇ: tek_el=$TEK_EL_IHLAL build=$B denetim=$D test=$T (hepsi 0 olmalı) · kayıtlar: $OUT =="
if [ "$B" = 0 ] && [ "$D" = 0 ] && [ "$T" = 0 ]; then
  TS=$(grep -o "total=\"[0-9]*\"" "$OUT/kapi.trx" | head -1 | grep -o "[0-9]*")

# QA METRİKLERİ (MÜHÜR 5 KOŞUL 8): son tarayıcı turunun kritik bulgu / düzenlenemeyen giriş / puanları kapı çıktısına basılır (kaynak ham/qa-turu-N.json)
SONTUR=$(ls "$KAYIT_KOK"/ham/qa-turu-*.json 2>/dev/null | sed -E 's/.*qa-turu-([0-9]+)\.json/\1/' | sort -n | tail -1)
if [ -n "$SONTUR" ]; then
  node -e '
const q=JSON.parse(require("fs").readFileSync(process.argv[1],"utf8"));const R=Object.values(q.rotalar);const n=R.length;
const b=R.reduce((t,r)=>t+r.bulgular.length,0),k=R.reduce((t,r)=>t+r.bulgular.filter(x=>/KRIT/i.test(x.ciddiyet)).length,0),d=R.reduce((t,r)=>t+r.duzenlenemeyen.length,0);
const g=(R.reduce((t,r)=>t+r.gorsel,0)/n).toFixed(2),u=(R.reduce((t,r)=>t+r.kullanici_dostu,0)/n).toFixed(2);
console.log("== QA metrikleri (tur "+q.tur+", "+n+" sayfa): kritik "+k+" · düzenlenemeyen veri girişi "+d+" · toplam bulgu "+b+" · görsel "+g+"/5 · kullanıcı dostu "+u+"/5 == (HAZIR eşiği: kritik 0, düzenlenemeyen 0)");
' "$KAYIT_KOK/ham/qa-turu-$SONTUR.json"
else
  echo "== QA metrikleri: henüz tarayıcı turu kaydı yok (ham/qa-turu-N.json) =="
fi

  [ -n "$TS" ] && node "$KOK/dokumanlar/09-uygulama-kayitlari/betikler/damgala.js" "$TS" "${2:-orkestratör kapısı}"
  exit 0
fi
exit 1
