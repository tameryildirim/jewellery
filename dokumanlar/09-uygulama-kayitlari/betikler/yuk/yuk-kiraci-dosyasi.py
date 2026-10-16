#!/usr/bin/env python3
# ═════════════════════════════════════════════════════════════════════════════
# PAKET OLCEK-YUK — kiracilar.json'u YÜK DB'sinden (kuyumcu_test_yuk) yeniden kurar.
# yuk-kiraci-kur.mjs'in çıktısı /private/tmp'de kaybolursa (04.10'da oldu) kiracıları
# yeniden KURMADAN k6 betiklerinin beklediği dosya buradan üretilir.
#   python3 yuk-kiraci-dosyasi.py <cikti.json>     (chmod 600 yazılır)
# Sır: MySQL parolası compose'tan değişkene alınır, basılmaz; dosyada parola YOK.
# ═════════════════════════════════════════════════════════════════════════════
import json, os, re, subprocess, sys

cikti = sys.argv[1]
compose = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/docker-compose.yml'
p = next(m.group(1).strip() for l in open(compose, encoding='utf-8')
         for m in [re.match(r'^\s*MYSQL_ROOT_PASSWORD:\s*(.*)$', l)] if m)

def sql(q):
    r = subprocess.run(['docker', 'exec', '-e', f'MYSQL_PWD={p}', 'kuyumcu-mysql', 'mysql', '-uroot', '-N', '-B',
                        'kuyumcu_test_yuk', '-e', q], capture_output=True, text=True)
    if r.returncode: sys.exit(f'SQL hatası (çıkış {r.returncode})')
    return [l.split('\t') for l in r.stdout.splitlines() if l]

ten = sql("SELECT id, ad FROM tenants WHERE ad LIKE 'PAKET-OLCEK-YUK-TEST-K%' ORDER BY ad")
grup = lambda q: {r[0]: r[1:] for r in sql(q)}
sube = grup("SELECT tenant_id, MIN(id) FROM subeler GROUP BY tenant_id")
kasa = grup("SELECT tenant_id, MIN(id) FROM kasalar WHERE para_birimi='TRY' GROUP BY tenant_id")
kur = grup("SELECT k.tenant_id, k.id FROM kur_snapshots k JOIN (SELECT tenant_id, MAX(olusturuldu) m FROM kur_snapshots GROUP BY tenant_id) x ON x.tenant_id=k.tenant_id AND x.m=k.olusturuldu")
urun, cari, kul = {}, {}, {}
for t, i in sql("SELECT tenant_id, id FROM urunler WHERE kod REGEXP '^S[0-9]{3}-[0-9]$' ORDER BY tenant_id, kod"):
    urun.setdefault(t, []).append(i)
for t, i in sql("SELECT tenant_id, id FROM cariler WHERE ad LIKE 'Müşteri %' ORDER BY tenant_id, ad"):
    cari.setdefault(t, []).append(i)
for t, e in sql("SELECT tenant_id, email FROM kullanicilar WHERE email LIKE 'olcekyuk-%' ORDER BY tenant_id, FIELD(rol,'SAHIP','MUDUR')"):
    kul.setdefault(t, []).append(e)

ip = lambda i: f'10.66.{i // 250}.{i % 250 + 1}'
liste = []
for tid, ad in ten:
    no = ad[-3:]
    liste.append({'no': no, 'ad': ad, 'tenantId': tid, 'subeId': sube[tid][0], 'kasaId': kasa[tid][0],
                  'kurId': kur[tid][0], 'urunler': urun.get(tid, []), 'cariler': cari.get(tid, []),
                  'xff': ip(int(no)), 'kullanicilar': kul.get(tid, [])})
eksik = [k['no'] for k in liste if len(k['urunler']) != 5 or len(k['kullanicilar']) != 2]
fd = os.open(cikti, os.O_WRONLY | os.O_CREAT | os.O_TRUNC, 0o600)
with os.fdopen(fd, 'w') as f: json.dump({'kiracilar': liste}, f, indent=1)
print(f'kiracı={len(liste)} eksik={eksik}')
