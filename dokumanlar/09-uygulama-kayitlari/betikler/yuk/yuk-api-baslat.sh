#!/bin/zsh
# ═════════════════════════════════════════════════════════════════════════════
# PAKET OLCEK-YUK — Release yayın kopyasını YÜK DB'sine (kuyumcu_test_yuk) karşı
# 5166'da ÜRETİM kipine eşdeğer (Staging) başlatır (ters vekil arkasında beyanı: yük loopback'ten gelir).
#   zsh yuk-api-baslat.sh <yayin_dizini>   → PID: $D/api-5166.pid · günlük: $D/api-5166.log
# Sır yok: MySQL parolası compose'tan, JWT ve alan şifreleme anahtarı ~/.kuyumcu'dan
# DEĞİŞKENE alınır; sistem anahtarı rastgele üretilir ve $D/sistem.anahtar (chmod 600)
# dosyasına yazılır. Hiçbiri çıktıya basılmaz. 5199/5200'e dokunmaz.
# ═════════════════════════════════════════════════════════════════════════════
set -u
YAYIN=$1
D=/private/tmp/kuyumcu-serit/OLCEK-YUK; mkdir -p $D
lsof -nP -iTCP:5166 -sTCP:LISTEN -t >/dev/null 2>&1 && { print "HATA: 5166 dolu"; exit 2; }
P=$(sed -n "s/^[[:space:]]*MYSQL_ROOT_PASSWORD:[[:space:]]*//p" /Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/docker-compose.yml | head -1 | tr -d "\r")
[ -n "$P" ] || { print "HATA: MySQL parolası okunamadı"; exit 3; }
[ -s $D/sistem.anahtar ] || { (umask 077; openssl rand -hex 32 > $D/sistem.anahtar) }
export ConnectionStrings__Kuyumcu="Server=127.0.0.1;Port=3306;Database=kuyumcu_test_yuk;Uid=root;Pwd=$P;"
unset P
export KUYUMCU_VT=kuyumcu_test_yuk
export Jwt__Secret="$(cat $HOME/.kuyumcu/jwt)"
export KUYUMCU_SIFRELEME_ANAHTARI="$(cat $HOME/.kuyumcu/sifreleme-anahtari)"
export Sistem__Anahtari="$(cat $D/sistem.anahtar)"
export Uretim__TersVekilArkasinda=true
# Staging: istek yolu Production ile BİREBİR (Program.cs yalnız IsDevelopment dallanır);
# fark yalnız açılış denetimi — yerel root parolası kara listededir ve MySQL’de kullanıcı/yetki
# DEĞİŞTİRMEMEK için root ile bağlanılır. Yük ölçümünü etkileyen bir dal yoktur.
export ASPNETCORE_ENVIRONMENT=Staging
cd $YAYIN || exit 4
nohup dotnet Kuyumcu.Api.dll --urls http://localhost:5166 > $D/api-5166.log 2>&1 &
print $! > $D/api-5166.pid
for i in {1..60}; do
  curl -s --max-time 2 -o $D/saglik.json -w '%{http_code}' http://localhost:5166/saglik 2>/dev/null | grep -q 200 && { print "API hazır pid=$(cat $D/api-5166.pid) ${i}sn"; exit 0; }
  kill -0 $(cat $D/api-5166.pid) 2>/dev/null || { print "HATA: API açılışta öldü"; tail -5 $D/api-5166.log; exit 5; }
  sleep 1
done
print "HATA: 60 sn'de hazır değil"; exit 6
