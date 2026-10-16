#!/bin/zsh
# ═════════════════════════════════════════════════════════════════════════════
# PAKET OLCEK-YUK — bir yük koşumu: k6 + kaynak örneklemesi + MySQL özet farkı.
#   zsh yuk-kos.sh <kiraci> <sure> <etiket>
# Çıktılar: $D/kosum-<etiket>/ (k6 özeti, kaynak örnekleri, en pahalı sorgular).
# Sır: MySQL parolası docker-compose.yml'den değişkene alınır, BASILMAZ.
# ═════════════════════════════════════════════════════════════════════════════
set -u
KIRACI=$1; SURE=$2; ETIKET=$3
BETIK=${0:a:h}
D=/private/tmp/kuyumcu-serit/OLCEK-YUK
O=$D/kosum-$ETIKET; mkdir -p $O
P=$(sed -n "s/^[[:space:]]*MYSQL_ROOT_PASSWORD:[[:space:]]*//p" /Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/docker-compose.yml | head -1 | tr -d "\r")
APIPID=$(cat $D/api-5166.pid)
SQL_OZET="SELECT digest, count_star, sum_timer_wait, LEFT(digest_text,300) FROM performance_schema.events_statements_summary_by_digest WHERE schema_name='kuyumcu_test_yuk'"
docker exec kuyumcu-mysql mysql -uroot -p"$P" -N -e "$SQL_OZET" 2>/dev/null > $O/ps-once.tsv
docker exec kuyumcu-mysql mysql -uroot -p"$P" -N -e "SELECT ROUND(SUM(data_length+index_length)/1048576) FROM information_schema.tables WHERE table_schema='kuyumcu_test_yuk'" 2>/dev/null > $O/db-mb.txt

# Kaynak örnekleyici: 5 sn'de bir API süreci (ps) + MySQL konteyneri (docker stats)
( while kill -0 $APIPID 2>/dev/null && [ ! -f $O/bitti ]; do
    a=$(ps -o %cpu=,rss= -p $APIPID | awk '{print $1, int($2/1024)}')
    m=$(docker stats --no-stream --format '{{.CPUPerc}} {{.MemUsage}}' kuyumcu-mysql | awk '{gsub("%","",$1); print $1, $2}')
    print "$(date +%T) $a $m" >> $O/kaynak.txt
    sleep 5
  done ) &
ORN=$!

KIRACI_DOSYASI=$D/kiracilar.json KIRACI=$KIRACI SURE=$SURE RAMPA=${RAMPA:-60s} OZET=$O/k6-ozet.json \
  k6 run -q $BETIK/k6-cok-kiraci.js > $O/k6.log 2>&1
K6=$?
touch $O/bitti; wait $ORN 2>/dev/null
docker exec kuyumcu-mysql mysql -uroot -p"$P" -N -e "$SQL_OZET" 2>/dev/null > $O/ps-sonra.tsv
python3 - $O <<'PY'
import sys, csv
o = sys.argv[1]
def oku(f):
    r = {}
    for l in open(f, encoding='utf-8', errors='replace'):
        p = l.rstrip('\n').split('\t')
        if len(p) >= 4: r[p[0]] = (int(p[1]), int(p[2]), p[3])
    return r
a, b = oku(f'{o}/ps-once.tsv'), oku(f'{o}/ps-sonra.tsv')
fark = []
for k, (n, t, txt) in b.items():
    n0, t0, _ = a.get(k, (0, 0, ''))
    if n - n0 > 0: fark.append(((t - t0) / 1e12, n - n0, (t - t0) / 1e9 / max(1, n - n0), txt))
fark.sort(reverse=True)
with open(f'{o}/sql-en-pahali.txt', 'w') as f:
    f.write('toplam_sn\tadet\tort_ms\tsorgu\n')
    for x in fark[:15]: f.write(f'{x[0]:.2f}\t{x[1]}\t{x[2]:.1f}\t{x[3]}\n')
PY
awk '{c+=$2; if($2>cm)cm=$2; r+=$3; if($3>rm)rm=$3; mc+=$4; if($4>mm)mm=$4; n++} END {printf "api_cpu_ort=%.0f%% api_cpu_tepe=%.0f%% api_rss_tepe=%dMB mysql_cpu_ort=%.0f%% mysql_cpu_tepe=%.0f%% ornek=%d\n", c/n, cm, rm, mc/n, mm, n}' $O/kaynak.txt > $O/kaynak-ozet.txt
grep "^{" $O/k6.log | tail -1 > $O/k6-satir.json
print "k6_cikis=$K6"; cat $O/kaynak-ozet.txt; cat $O/k6-satir.json | head -c 400; echo
