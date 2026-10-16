#!/bin/zsh
# Test kiracısı artığını siler (dalga kapısı adımı). Kalıcı fikstürlere (QA-TEST-*) ve gerçek/demo kiracılara DOKUNMAZ.
# Kullanım: zsh betikler/kiraci-temizle.sh <tenant_id> [--uygula]   (--uygula yoksa yalnız RAPOR verir)
KOK=/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery
ID="$1"; UYGULA="$2"
[ -n "$ID" ] || { echo "kullanım: zsh betikler/kiraci-temizle.sh <tenant_id> [--uygula]"; exit 2; }
# ⚠ 2026-09-21'de ÖLÇÜLEN HATA: 'grep MYSQL_ROOT_PASSWORD | head -1' docker-compose.yml'in
#   3. satırındaki YORUM satırını yakalıyordu; awk -F'[:=]' onu 53 karakterlik bir çöp
#   dizgeye çeviriyordu → 'ERROR 1045 Access denied' → SQL() 2>/dev/null yüzünden BOŞ dönüyor
#   → betik "Kiracı YOK" diyerek ÇIKIYORDU. Yani bağlanamama, "kiracı yok" diye raporlanıyordu
#   (Madde 1 ihlali: başarısızlık başka bir sonuç gibi görünüyordu). İki düzeltme:
#   (1) parola YALNIZ gerçek ayar satırından, iki nokta sonrasının TAMAMI alınarak okunur,
#   (2) bağlantı ÖNCE sınanır; bağlanamıyorsa betik farklı bir çıkış koduyla (3) DURUR.
P=$(sed -n 's/^[[:space:]]*MYSQL_ROOT_PASSWORD:[[:space:]]*//p' "$KOK/docker-compose.yml" | head -1 | tr -d '\r' | sed 's/^["'"'"']//; s/["'"'"']$//')
[ -n "$P" ] || { echo "MySQL parolası bulunamadı (docker-compose.yml)"; exit 1; }
SQL() { docker exec -e MYSQL_PWD="$P" kuyumcu-mysql mysql -uroot -N -e "$1" 2>/dev/null; }
SINAMA=$(docker exec -e MYSQL_PWD="$P" kuyumcu-mysql mysql -uroot -N -e "SELECT 1;" 2>&1)
if [ "$SINAMA" != "1" ]; then
  echo "BAĞLANILAMADI (kiracı var mı YOK MU BİLİNMİYOR): ${SINAMA%%$'\n'*}"
  echo "  → parola docker-compose.yml'deki MYSQL_ROOT_PASSWORD satırından okunur; kap adı 'kuyumcu-mysql' ayakta mı?"
  exit 3
fi
AD=$(SQL "SELECT ad FROM kuyumcu_net.tenants WHERE id='$ID';")
[ -n "$AD" ] || { echo "Kiracı YOK: $ID  (bağlantı sınandı ve ÇALIŞIYOR — bu gerçekten 'yok' demektir)"; exit 1; }
case "$AD" in
  QA-TEST-*) echo "REDDEDİLDİ: '$AD' kalıcı QA fikstürüdür, silinmez."; exit 1;;
  # P-30 (R-4): DEMO kiracısı bu betiğin silme ad kuralına EKLENMEZ. Gösterim
  # kiracısı dalga kapılarının toplu temizliğiyle süpürülemez; silinmesi
  # kurucu kararıdır (docs/RUNBOOK-DEMO.md § 4).
  DEMO-*)    echo "REDDEDİLDİ: '$AD' DEMO kiracısıdır (P-30) — silme ad kuralına dâhil değildir; silinmesi kurucu kararıdır."; exit 1;;
esac
echo "$AD" | grep -qiE '^(PAKET|TEST|QA)' || { echo "REDDEDİLDİ: '$AD' test kiracısı adına benzemiyor (PAKET*/TEST*/QA* bekleniyor). Silme kurucu kararıdır."; exit 1; }
TABLOLAR=$(SQL "SELECT table_name FROM information_schema.columns WHERE table_schema='kuyumcu_net' AND column_name='tenant_id';")
echo "== Kiracı: $AD ($ID) · tenant_id kolonu olan tablo: $(echo "$TABLOLAR" | wc -l | tr -d ' ') =="
TOPLAM=0
for T in ${=TABLOLAR}; do
  N=$(SQL "SELECT COUNT(*) FROM kuyumcu_net.\`$T\` WHERE tenant_id='$ID';")
  [ "$N" -gt 0 ] 2>/dev/null && { echo "  $T: $N satır"; TOPLAM=$((TOPLAM+N)); }
done
echo "== Toplam $TOPLAM satır + tenants 1 satır =="
if [ "$UYGULA" != "--uygula" ]; then echo "(RAPOR MODU — silmek için: zsh betikler/kiraci-temizle.sh $ID --uygula)"; exit 0; fi
for T in ${=TABLOLAR}; do SQL "SET FOREIGN_KEY_CHECKS=0; DELETE FROM kuyumcu_net.\`$T\` WHERE tenant_id='$ID'; SET FOREIGN_KEY_CHECKS=1;"; done
SQL "DELETE FROM kuyumcu_net.tenants WHERE id='$ID';"
KALAN=$(SQL "SELECT COUNT(*) FROM kuyumcu_net.tenants;")
echo "SİLİNDİ: $AD ($ID) · kalan kiracı sayısı: $KALAN"
SQL "SELECT ad FROM kuyumcu_net.tenants ORDER BY olusturuldu;" | sed 's/^/  - /'
