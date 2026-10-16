#!/bin/zsh
# ═════════════════════════════════════════════════════════════════════════════
# P-41 — YAYIN BÜTÜNLÜK KAPISI KABUK TESTLERİ
#
# `dotnet test` kabuk betiklerini koşturamaz; oysa yayın hattının TAMAMI kabukta
# yaşıyor (yayinla.sh + yayin-butunluk.sh + qa-sunucu.sh). Tur 8'in arızası da
# tam orada oldu. Bu dosya o hattı GERÇEK süreç çağırarak sınar: fikstür bir
# yayın dizini kurar, bozar, ve kapının AÇMADIĞINI çıkış koduyla kanıtlar.
#
# KULLANIM:  zsh betikler/testler/yayin-butunluk-testleri.sh
# ÇIKIŞ:     0 = hepsi geçti · 1 = en az bir test kırmızı
#
# 5199'A DOKUNMAZ: qa-sunucu.sh yalnız KUYUMCU_KURU_KOSUM=1 ile ve kendi fikstür
# yayın dizinine yöneltilerek çağrılır; hiçbir port dinlenmez.
# ═════════════════════════════════════════════════════════════════════════════
set -u

KOK=/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery
BETIKLER="$KOK/dokumanlar/09-uygulama-kayitlari/betikler"
BUTUNLUK="$BETIKLER/yayin-butunluk.sh"
QA_SUNUCU="$BETIKLER/qa-sunucu.sh"
KAYNAK_WWWROOT="$KOK/dotnet/Kuyumcu.Web/wwwroot"

T=$(mktemp -d "${TMPDIR:-/tmp}/p41-testleri.XXXXXX") || exit 2
GECTI=0; KALDI=0
KIRMIZILAR=()

bildir() {  # bildir <ad> <0|1> [açıklama]
  if [ "$2" -eq 0 ]; then
    printf '  ✓ %s\n' "$1"; GECTI=$((GECTI+1))
  else
    printf '  ✗ %s — %s\n' "$1" "${3:-beklenen sonuç alınamadı}"
    KALDI=$((KALDI+1)); KIRMIZILAR+=("$1")
  fi
}

# ── FİKSTÜR: sentetik yayın dizini ───────────────────────────────────────────
# 66 boot varlığı (60 assembly + 3 icu + 1 wasmNative + 2 jsModule) + wwwroot
# KÖK dosyalarının GERÇEK kopyaları (kilavuz.html dâhil) + Kuyumcu.Api.dll.
# İçerik sabittir → iki fikstür birebir aynı SHA/boyutu verir.
hazirla_yayin() {  # hazirla_yayin <dizin> [hariç-tutulacak-göreli-yol]
  local D="$1" HARIC="${2:-}"
  mkdir -p "$D/wwwroot/_framework" || return 1
  [ "$HARIC" = "Kuyumcu.Api.dll" ] || printf 'SAHTE-KUYUMCU-API-IKILISI' > "$D/Kuyumcu.Api.dll"

  local f ad
  while IFS= read -r f; do
    ad="${f##*/}"
    [ "$HARIC" = "wwwroot/$ad" ] && continue
    cp "$f" "$D/wwwroot/$ad" || return 1
  done < <(find "$KAYNAK_WWWROOT" -maxdepth 1 -type f 2>/dev/null | sort)

  python3 - "$D" "$HARIC" <<'PY' || return 1
import json, os, sys
d, haric = sys.argv[1], sys.argv[2]
fw = os.path.join(d, "wwwroot", "_framework")
kaynaklar = {"assembly": {}, "icu": {}, "wasmNative": {},
             "jsModuleNative": {}, "jsModuleRuntime": {}}
adlar = []
for i in range(60):
    adlar.append(("assembly", "Sahte.Derleme%02d.wasm" % i))
for c in ("icudt_CJK.dat", "icudt_EFIGS.dat", "icudt_no_CJK.dat"):
    adlar.append(("icu", c))
adlar.append(("wasmNative", "dotnet.native.wasm"))
adlar.append(("jsModuleNative", "dotnet.native.8.0.0.test.js"))
adlar.append(("jsModuleRuntime", "dotnet.runtime.8.0.0.test.js"))
for bolum, ad in adlar:
    kaynaklar[bolum][ad] = "sha256-P41TEST"
    if haric == "wwwroot/_framework/" + ad:
        continue
    with open(os.path.join(fw, ad), "w") as g:
        g.write("VARLIK:" + ad)
with open(os.path.join(fw, "dotnet.js"), "w") as g:
    g.write("// sahte dotnet.js yukleyicisi")
boot = {"mainAssemblyName": "Kuyumcu.Web", "resources": kaynaklar}
with open(os.path.join(fw, "blazor.boot.json"), "w") as g:
    json.dump(boot, g, indent=1, sort_keys=True)
PY
  return 0
}

agac_damgasi() {
  find "$KOK/dotnet/Kuyumcu.Api" "$KOK/dotnet/Kuyumcu.Web" "$KOK/dotnet/Kuyumcu.Core" \
       \( -name '*.cs' -o -name '*.razor' -o -name '*.csproj' \) 2>/dev/null \
    | grep -v '/obj/\|/bin/' | sort | xargs shasum 2>/dev/null | shasum | awk '{print $1}'
}

# Sahte sır dizini: qa-sunucu.sh'in sır kapılarını (exit 3/5/8) geçmek için.
# Değerler TEST DEĞERİDİR, gerçek sır değildir ve hiçbir yere basılmaz.
hazirla_sirlar() {  # hazirla_sirlar <dizin> [eksik-anahtar]
  local S="$1" EKSIK="${2:-}"
  mkdir -p "$S"; chmod 700 "$S"
  local a
  for a in jwt baglanti sifreleme-anahtari; do
    [ "$a" = "$EKSIK" ] && continue
    printf 'p41-test-degeri-%s-yalnizca-fikstur' "$a" > "$S/$a"
    chmod 600 "$S/$a"
  done
}

# qa-sunucu.sh'i kuru koşumda çağırır; çıkış kodunu ve çıktısını döndürür.
kapi_kos() {  # kapi_kos <yayın-dizini> <sır-dizini> <envanter-zorunlu-yolu> <çıktı-dosyası>
  KUYUMCU_KURU_KOSUM=1 \
  KUYUMCU_YAYIN_DIZINI="$1" \
  KUYUMCU_SIR_DIZINI="$2" \
  KUYUMCU_ENVANTER_ZORUNLU="$3" \
    zsh "$QA_SUNUCU" > "$4" 2>&1
  return $?
}

echo "═══ P-41 yayın bütünlük kabuk testleri ═══"
echo "  fikstür kökü: $T"

# ── 1) test_tam_yayin_gecer ──────────────────────────────────────────────────
test_tam_yayin_gecer() {
  local D="$T/tam"
  hazirla_yayin "$D" || { bildir test_tam_yayin_gecer 1 "fikstür kurulamadı"; return; }
  zsh "$BUTUNLUK" --uret "$D" > "$T/1-uret.log" 2>&1
  local RC_U=$?
  zsh "$BUTUNLUK" --dogrula "$D" > "$T/1-dogrula.log" 2>&1
  local RC_D=$?
  local KAYIT=0
  [ -r "$D/.yayin-envanteri" ] && KAYIT=$(grep -c . "$D/.yayin-envanteri")
  local VARLIK; VARLIK=$(grep -c ' wwwroot/_framework/' "$D/.yayin-envanteri" 2>/dev/null || echo 0)
  if [ $RC_U -eq 0 ] && [ $RC_D -eq 0 ] && [ "$VARLIK" -ge 66 ] && [ "$KAYIT" -ge 72 ]; then
    bildir test_tam_yayin_gecer 0
  else
    bildir test_tam_yayin_gecer 1 "uret=$RC_U dogrula=$RC_D varlık=$VARLIK kayıt=$KAYIT (beklenen 0/0/≥66/≥72)"
  fi
}

# ── 2) test_eksik_dosya_reddedilir ───────────────────────────────────────────
# Envanter TAM yayından üretilir, sonra kilavuz.html'siz bir yayına taşınır:
# tur 8'in birebir senaryosu. Kapı "EKSİK DOSYA" demek ZORUNDA.
test_eksik_dosya_reddedilir() {
  local A="$T/tam" B="$T/eksik"
  [ -r "$A/.yayin-envanteri" ] || zsh "$BUTUNLUK" --uret "$A" >/dev/null 2>&1
  hazirla_yayin "$B" "wwwroot/kilavuz.html" || { bildir test_eksik_dosya_reddedilir 1 "fikstür"; return; }
  cp "$A/.yayin-envanteri" "$B/.yayin-envanteri"
  zsh "$BUTUNLUK" --dogrula "$B" > "$T/2.log" 2>&1
  local RC=$?
  if [ $RC -ne 0 ] && grep -q '^EKSİK DOSYA: wwwroot/kilavuz.html$' "$T/2.log"; then
    bildir test_eksik_dosya_reddedilir 0
  else
    bildir test_eksik_dosya_reddedilir 1 "rc=$RC · 'EKSİK DOSYA: wwwroot/kilavuz.html' satırı yok"
  fi
}

# ── 3) test_sifir_bayt_reddedilir ────────────────────────────────────────────
# "Dosya var mı" kontrolü 0 baytlık dosyayı GÖREMEZ; ayrı mesajlı ayrı kontrol.
test_sifir_bayt_reddedilir() {
  local D="$T/sifir"
  hazirla_yayin "$D" || { bildir test_sifir_bayt_reddedilir 1 "fikstür"; return; }
  zsh "$BUTUNLUK" --uret "$D" >/dev/null 2>&1
  printf '' > "$D/wwwroot/_framework/Sahte.Derleme07.wasm"
  zsh "$BUTUNLUK" --dogrula "$D" > "$T/3.log" 2>&1
  local RC=$?
  if [ $RC -ne 0 ] && grep -q '^SIFIR BAYT: wwwroot/_framework/Sahte.Derleme07.wasm$' "$T/3.log"; then
    bildir test_sifir_bayt_reddedilir 0
  else
    bildir test_sifir_bayt_reddedilir 1 "rc=$RC · 'SIFIR BAYT: …Sahte.Derleme07.wasm' satırı yok"
  fi
}

# ── 4) test_sha_uyusmazligi_reddedilir ───────────────────────────────────────
# İçerik BOYUT DEĞİŞTİRMEDEN bozulur → yalnız SHA karşılaştırması yakalayabilir.
test_sha_uyusmazligi_reddedilir() {
  local D="$T/sha"
  hazirla_yayin "$D" || { bildir test_sha_uyusmazligi_reddedilir 1 "fikstür"; return; }
  zsh "$BUTUNLUK" --uret "$D" >/dev/null 2>&1
  local Y="$D/wwwroot/_framework/Sahte.Derleme11.wasm"
  local ESKI_BOYUT; ESKI_BOYUT=$(stat -f %z "$Y")
  # "VARLIK:Sahte.Derleme11.wasm" ile AYNI uzunlukta farklı içerik
  python3 -c "
import sys
u=int(sys.argv[2]); open(sys.argv[1],'w').write('X'*u)
" "$Y" "$ESKI_BOYUT"
  local YENI_BOYUT; YENI_BOYUT=$(stat -f %z "$Y")
  zsh "$BUTUNLUK" --dogrula "$D" > "$T/4.log" 2>&1
  local RC=$?
  if [ "$ESKI_BOYUT" = "$YENI_BOYUT" ] && [ $RC -ne 0 ] \
     && grep -q '^SHA UYUŞMAZLIĞI: wwwroot/_framework/Sahte.Derleme11.wasm$' "$T/4.log"; then
    bildir test_sha_uyusmazligi_reddedilir 0
  else
    bildir test_sha_uyusmazligi_reddedilir 1 "rc=$RC boyut $ESKI_BOYUT→$YENI_BOYUT · 'SHA UYUŞMAZLIĞI' satırı yok"
  fi
}

# ── 5) test_kapi_eksik_dosyada_acilmaz_cikis14 ───────────────────────────────
test_kapi_eksik_dosyada_acilmaz_cikis14() {
  local D="$T/kapi-eksik" S="$T/sirlar"
  hazirla_sirlar "$S"
  local A="$T/tam"
  [ -r "$A/.yayin-envanteri" ] || zsh "$BUTUNLUK" --uret "$A" >/dev/null 2>&1
  hazirla_yayin "$D" "wwwroot/kilavuz.html" || { bildir test_kapi_eksik_dosyada_acilmaz_cikis14 1 "fikstür"; return; }
  cp "$A/.yayin-envanteri" "$D/.yayin-envanteri"
  agac_damgasi > "$D/.agac-damgasi"
  kapi_kos "$D" "$S" "$T/yok-boyle-bir-dosya" "$T/5.log"
  local RC=$?
  if [ $RC -eq 14 ] && grep -q 'wwwroot/kilavuz.html' "$T/5.log" && ! grep -q 'KURU KOŞUM: tüm kapılar geçildi' "$T/5.log"; then
    bildir test_kapi_eksik_dosyada_acilmaz_cikis14 0
  else
    bildir test_kapi_eksik_dosyada_acilmaz_cikis14 1 "çıkış=$RC (beklenen 14) · $(tail -n 2 "$T/5.log" | tr '\n' ' ')"
  fi
}

# ── 6) test_kapi_sifir_bayt_varlikta_acilmaz_cikis14 ─────────────────────────
# Tur 8'in GERÇEK arızasının birebir tekrarı: dosya "var" ama içi boş.
test_kapi_sifir_bayt_varlikta_acilmaz_cikis14() {
  local D="$T/kapi-sifir" S="$T/sirlar"
  hazirla_sirlar "$S"
  hazirla_yayin "$D" || { bildir test_kapi_sifir_bayt_varlikta_acilmaz_cikis14 1 "fikstür"; return; }
  zsh "$BUTUNLUK" --uret "$D" >/dev/null 2>&1
  printf '' > "$D/wwwroot/_framework/dotnet.native.wasm"
  agac_damgasi > "$D/.agac-damgasi"
  kapi_kos "$D" "$S" "$T/yok-boyle-bir-dosya" "$T/6.log"
  local RC=$?
  if [ $RC -eq 14 ] && grep -q 'dotnet.native.wasm' "$T/6.log"; then
    bildir test_kapi_sifir_bayt_varlikta_acilmaz_cikis14 0
  else
    bildir test_kapi_sifir_bayt_varlikta_acilmaz_cikis14 1 "çıkış=$RC (beklenen 14) · $(tail -n 2 "$T/6.log" | tr '\n' ' ')"
  fi
}

# ── 7) test_envanter_zorunlu_anahtari_fail_closed_cikis15 ────────────────────
# Anahtar VARSA envantersiz yayın açılmaz (15); anahtar YOKSA yalnız uyarı + açılış.
test_envanter_zorunlu_anahtari_fail_closed_cikis15() {
  local D="$T/kapi-envantersiz" S="$T/sirlar" Z="$T/ENVANTER-ZORUNLU"
  hazirla_sirlar "$S"
  hazirla_yayin "$D" || { bildir test_envanter_zorunlu_anahtari_fail_closed_cikis15 1 "fikstür"; return; }
  agac_damgasi > "$D/.agac-damgasi"
  printf 'p41 test anahtari\n' > "$Z"

  kapi_kos "$D" "$S" "$Z" "$T/7a.log"; local RC_A=$?
  kapi_kos "$D" "$S" "$T/yok-boyle-bir-dosya" "$T/7b.log"; local RC_B=$?

  if [ $RC_A -eq 15 ] && grep -q 'YAYIN ENVANTERSİZ' "$T/7a.log" \
     && [ $RC_B -eq 0 ] && grep -q 'yayın envanteri YOK' "$T/7b.log"; then
    bildir test_envanter_zorunlu_anahtari_fail_closed_cikis15 0
  else
    bildir test_envanter_zorunlu_anahtari_fail_closed_cikis15 1 \
      "anahtarlı çıkış=$RC_A (beklenen 15) · anahtarsız çıkış=$RC_B (beklenen 0)"
  fi
}

# ── 8) test_eski_cikis_kodlari_degismedi ─────────────────────────────────────
# Yeni kapı, mevcut kapıların ÖNÜNE geçmemeli ve kodlarını değiştirmemeli.
test_eski_cikis_kodlari_degismedi() {
  local S="$T/sirlar" SJ="$T/sirlar-jwtsiz" HATA=0 NOT=""
  hazirla_sirlar "$S"; hazirla_sirlar "$SJ" jwt

  # (a) exit 9 — damgasız yayın + damga zorunluluğu açık (depoda VAR)
  local D9="$T/eski-9"
  hazirla_yayin "$D9" >/dev/null 2>&1
  zsh "$BUTUNLUK" --uret "$D9" >/dev/null 2>&1
  kapi_kos "$D9" "$S" "$T/yok-boyle-bir-dosya" "$T/8a.log"
  [ $? -eq 9 ] || { HATA=1; NOT="$NOT damgasız≠9;"; }

  # (b) exit 7 — damga UYUŞMAZLIĞI
  local D7="$T/eski-7"
  hazirla_yayin "$D7" >/dev/null 2>&1
  zsh "$BUTUNLUK" --uret "$D7" >/dev/null 2>&1
  printf 'deadbeefdeadbeefdeadbeefdeadbeefdeadbeef\n' > "$D7/.agac-damgasi"
  kapi_kos "$D7" "$S" "$T/yok-boyle-bir-dosya" "$T/8b.log"
  [ $? -eq 7 ] || { HATA=1; NOT="$NOT bayat≠7;"; }

  # (c) exit 8 — JWT sırrı yok (yeni kapıdan SONRA gelir, yani kapı onu yutmamalı)
  local D8="$T/eski-8"
  hazirla_yayin "$D8" >/dev/null 2>&1
  zsh "$BUTUNLUK" --uret "$D8" >/dev/null 2>&1
  agac_damgasi > "$D8/.agac-damgasi"
  kapi_kos "$D8" "$SJ" "$T/yok-boyle-bir-dosya" "$T/8c.log"
  [ $? -eq 8 ] || { HATA=1; NOT="$NOT jwtsiz≠8;"; }

  # (d) geri kalan kodlar betikte hâlâ TANIMLI
  local k
  for k in 3 4 5 10 11 12 13; do
    grep -qE "^[[:space:]]*(exit $k|.*\bexit $k\b)" "$QA_SUNUCU" || { HATA=1; NOT="$NOT exit$k yok;"; }
  done

  bildir test_eski_cikis_kodlari_degismedi $HATA "$NOT"
}

# ═════════════════════════════════════════════════════════════════════════════
# P-42 — YAYIN SONRASI SALT-OKUNUR + İKİNCİ KOŞUM
#
# ÖLÇÜLEN ARIZA (23/09 12:05): yayına `yayinla.sh` DIŞINDA bir el dokundu; iki
# .wasm dosyası tur KOŞARKEN değişti, publish günlüğü YOKTU, KİMİN dokunduğu
# doğrulanamadı. Yazılabilir bir yayın dizini bunu davet ediyor. P-42 dizini
# envanter doğrulandıktan SONRA salt-okunur kilitler.
#
# İKİNCİ KOŞUM neden ayrı bir test: salt-okunur kilit, bir sonraki yayını da
# kilitler. `chmod -R u+w` geri alması olmasaydı ikinci `yayinla.sh` "Access
# denied" ile ölür ve kurucu bir daha YENİ sürüm göremezdi — kapı, kapattığı
# arızadan daha büyük bir arıza doğururdu.
#
# ⚠ Bu iki test GERÇEK `dotnet publish -c Release` koşar (dakikalar sürer) ve
#   5199'a DOKUNMAZ: hedef, fikstür dizinidir (KUYUMCU_YAYIN_DIZINI).
# ═════════════════════════════════════════════════════════════════════════════
P42_YAYIN="$T/p42-yayin"
P42_LOG1="$T/p42-yayin-1.log"
P42_LOG2="$T/p42-yayin-2.log"
YAYINLA="$BETIKLER/yayinla.sh"

test_yayin_sonrasi_salt_okunur() {
  local HATA=0 NOT=""
  if [ ! -r "$YAYINLA" ]; then
    bildir test_yayin_sonrasi_salt_okunur 1 "yayinla.sh yok: $YAYINLA"; return
  fi
  KUYUMCU_YAYIN_DIZINI="$P42_YAYIN" zsh "$YAYINLA" p42-test-1 > "$P42_LOG1" 2>&1
  local RC=$?
  [ $RC -eq 0 ] || { HATA=1; NOT="$NOT ilk yayın çıkış=$RC;"; }
  if [ -f "$P42_YAYIN/wwwroot/index.html" ]; then
    # ASIL İDDİA: yayın bittikten sonra dosya YAZILAMAZ olmalı.
    [ -w "$P42_YAYIN/wwwroot/index.html" ] && { HATA=1; NOT="$NOT index.html hâlâ yazılabilir;"; }
    # Gerçek yazma denemesi (izin biti yalan söyleyemesin).
    if ( printf 'ELLE-YAMA' >> "$P42_YAYIN/wwwroot/index.html" ) 2>/dev/null; then
      HATA=1; NOT="$NOT elle yama BAŞARILI oldu (kilit yok);"
    fi
    [ -w "$P42_YAYIN/Kuyumcu.Api.dll" ] && { HATA=1; NOT="$NOT Kuyumcu.Api.dll hâlâ yazılabilir;"; }
  else
    HATA=1; NOT="$NOT yayın üretilmedi (wwwroot/index.html yok, günlük: $P42_LOG1);"
  fi
  bildir test_yayin_sonrasi_salt_okunur $HATA "$NOT"
}

test_ikinci_yayin_kosumu_basarili() {
  local HATA=0 NOT=""
  if [ ! -d "$P42_YAYIN" ]; then
    bildir test_ikinci_yayin_kosumu_basarili 1 "ilk yayın koşmadı; ikinci koşum sınanamaz"; return
  fi
  KUYUMCU_YAYIN_DIZINI="$P42_YAYIN" zsh "$YAYINLA" p42-test-2 > "$P42_LOG2" 2>&1
  local RC=$?
  [ $RC -eq 0 ] || { HATA=1; NOT="$NOT ikinci yayın çıkış=$RC (u+w geri alma çalışmıyor, günlük: $P42_LOG2);"; }
  grep -q 'YAYIN HAZIR' "$P42_LOG2" 2>/dev/null || { HATA=1; NOT="$NOT 'YAYIN HAZIR' satırı yok;"; }
  # İkinci koşum da kilidi geri kurmalı — kilit tek seferlik olamaz.
  [ -f "$P42_YAYIN/wwwroot/index.html" ] && [ -w "$P42_YAYIN/wwwroot/index.html" ]     && { HATA=1; NOT="$NOT ikinci koşumdan sonra kilit YOK;"; }
  bildir test_ikinci_yayin_kosumu_basarili $HATA "$NOT"
}

test_tam_yayin_gecer
test_eksik_dosya_reddedilir
test_sifir_bayt_reddedilir
test_sha_uyusmazligi_reddedilir
test_kapi_eksik_dosyada_acilmaz_cikis14
test_kapi_sifir_bayt_varlikta_acilmaz_cikis14
test_envanter_zorunlu_anahtari_fail_closed_cikis15
test_eski_cikis_kodlari_degismedi
test_yayin_sonrasi_salt_okunur
test_ikinci_yayin_kosumu_basarili

echo "───────────────────────────────────────────"
printf 'TOPLAM %d · GEÇTİ %d · KALDI %d\n' $((GECTI+KALDI)) $GECTI $KALDI
if [ $KALDI -gt 0 ]; then
  printf 'KIRMIZILAR: %s\n' "${KIRMIZILAR[*]}"
  echo "fikstürler incelemek için duruyor: $T"
  exit 1
fi
echo "fikstür kökü: $T (geçici dizin; silmek kurucuya bırakıldı)"
exit 0
