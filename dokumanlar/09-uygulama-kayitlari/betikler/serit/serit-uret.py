#!/usr/bin/env python3
"""Yeni serit betigi uretici (orkestrator 30.09).
Kullanim: python3 serit-uret.py <tanim.json> <hedef.js>
tanim.json: {"dalga": str, "izolasyon": {"db","api","vekil","cdp"}, "esz": str,
             "paketler": [{"no","ad","belge","kapsam","kabul","dosya_alani","ek":[str]}]}
Taban mantik serit-emanet.js'ten alinir (dongu: uygula -> dogrula -> hakem -> tasarim -> duzelt, en cok 2 tur);
yalniz GOMULU degistirilir.
"""
import json, sys, os

KOK = '/Users/ramazanakkocan/ByGatech-yerel/jewellery'
TABAN = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'serit-emanet.js')

s = open(TABAN, encoding='utf-8').read()
i = s.index('const GOMULU = ') + len('const GOMULU = ')
eski, uzunluk = json.JSONDecoder().raw_decode(s[i:])
tanim = json.load(open(sys.argv[1], encoding='utf-8'))

ortak = [k for k in eski['paketler'][0]['kosullar']
         if k.startswith('KURUCU TEMEL KURALI') or k.startswith('OLCUM ARACLARI') or k.startswith('KURUCU IZNI')]

iz = tanim['izolasyon']
izolasyon = (f"⚠ IZOLASYON (30.09): KUYUMCU_TEST_DB={iz['db']} · canli API {iz['api']} · vekil {iz['vekil']}; "
             f"CDP portu {iz['cdp']}. Baska portlara DOKUNMA (5199/5200 kurucunun inceleme sunucusu — ASLA durdurma; "
             f"5230-5233 kesif). Tarayici bolmesini KULLANMA — yalniz basliksiz Chrome (betikler/tur/cdp.mjs). "
             f"Gecici dosyalar /private/tmp/kuyumcu-serit/<paket>/ altinda. ESZAMANLI SERITLER: {tanim['esz']}. "
             f"Kendi alanin disina cikman gerekirse DUR ve rapora yaz.")
genel = [
    "HIZ VE TUR (kurucu 29.09 23:10): paket EN COK 2 tur alir. Tam suit tur basina BIR kez (son kodla); ara adimlarda filtreli test. "
    "Kalite kapilari korunur: build 0/0, denetim.sh TEMIZ, mutasyon (her kritik kural en az bir testi kirmizi yapar, geri alininca cmp AYNI), "
    "canli API + SQL kaniti, ekrana dokunan her is gercek (basliksiz) Chrome'da 1280 + 375 · BASIT + PRO · acik + koyu olculur (sayisal olcum tum kombinasyonlarda); TOKEN TASARRUFU (kurucu 08.10): Read ile goruntu EN COK 4 (en riskli kombinasyonlar). "
    "Test kiracisi is bitince `zsh kiraci-temizle.sh <id> --uygula` ile silinir (bash degil ZSH). COMMIT YOK. Dosya silme YOK (rm kancasi). "
    "Sir/parola degeri ASLA basilmaz. 'dotnet ef' --no-build YASAK (once build).",
    "MEVZUAT (kurucu, BAGLAYICI): yasal kural/esik/oran yalniz BIRINCIL kaynakla (Resmi Gazete, mevzuat.gov.tr, GIB, MASAK) yazilir; "
    "dogrulanamayan deger TEYITSIZ damgasiyla BUGUNKU davranisi birebir koruyarak tutulur ve ilgili kapi fail-closed kalir; mali musavir sorusu raporun "
    "'Kurucu/mali musavir' bolumune yazilir. Tahmin etme.",
    "SADE VE SIK ILKESI (kurucu 10.10, verbatim: 'gorunumler olsun bilgilerin gorunmesi olsun tasarima uygun sade ve sik insani bogmayan "
    "sikmayan aklini bulandirmayan sadelikte ve siklikta olmasina dikkat goster'): dokunulan her ekranda bilgi EKSIKSIZ ama sade — ekran basina "
    "tek birincil dugme; ayni bilgi/uyari tek yerde tek kez; aciklama/ipucu satiri yok (gerekirse tiklayinca); jargon yok (tezgahtar dili); "
    "isteğe bagli her sey '+ ekle'/katlanir; hata yalniz alan altinda; sayfa kisa. Tasarim hakemi bunu olcer (tekrar sayisi, aciklama satiri, "
    "birincil dugme sayisi, sayfa boyu); ihlal = bulgu. Yildiz'da tuval birebir.",
    "YILDIZ KORUMASI (kurucu 10.10: 'Yildiz tasarimi nasil guzel ve hos gordun, o sikligi sadeligi bozma'): dokundugun her ekran Yildiz "
    "semasinda da acilir; ekledigin her yeni oge (form, uyari, bant, dugme, cekmece, liste) Yildiz'da TUVAL KALIPLARIYLA cizilir (alan cizgisi, "
    ".b1/.b2/.b3, .sat satiri, kartsiz bolum, Inter + tuval olcegi, Yildiz belirtec renkleri) — eski tema kutusu/golgesi/rengi Yildiz'a sizmaz. "
    "Kabul: dokunulan ekranlar Yildiz koyu+acik gercek Chrome'da olculur; betikler/tur/tuval-kiyas.mjs ile Bugun/Tezgah/Musteriler/Tum ekranlar "
    "tablolari FARK 0 KORUNUR (bozulursa KRITIK); betikler/tur/yildiz-tipo-tara.mjs varsa kural disi dugum 0.",
    "DISK (10.10): Chrome profilini TEK dizinde tut ve yeniden kullan; buyuk cikti biriktirme (disk iki kez doldu, Docker coktu).",
]
paketler = []
for p in tanim['paketler']:
    k = []
    k += p.get('ek', [])
    k.append(f"KAYNAK (TEK DOGRULUK — ONCE TAMAMINI OKU): {p['belge']}")
    k.append('KAPSAM: ' + p['kapsam'])
    k.append('KABUL: ' + p['kabul'])
    k.append('DOSYA ALANI: ' + p['dosya_alani'])
    k += genel
    k += ortak
    k.append(izolasyon)
    if p.get('hasim') is False:
        k.insert(0, "HASIMSIZ PAKET (kurucu 30.09 21:15): bu paket yalniz gorunum; hasim dogrulayici KOSMAYACAK. Bu yuzden KENDI kanitin baglayicidir: tam suit (tur basina bir kez) 0 yeni kirmizi, denetim.sh TEMIZ, her kabul maddesi icin mutasyon, gercek basliksiz Chrome olcumu + goruntuler Read ile. Para/veri/yasal davranisina DOKUNMA; dokunman gerekirse DUR ve rapora 'hasimli pakete devir' yaz.")
    ent = {'no': p['no'], 'ad': p['ad'], 'belge': p['belge'], 'kosullar': k}
    if p.get('hasim') is False: ent['hasim'] = False
    if p.get('model'): ent['model'] = p['model']
    paketler.append(ent)

yeni = {'dalga': tanim['dalga'], 'ux_sozlesme': eski.get('ux_sozlesme', ''), 'paketler': paketler}
if 'ana_belge' in tanim:
    yeni['ana_belge'] = tanim['ana_belge']
out = s[:i] + json.dumps(yeni, ensure_ascii=False) + s[i + uzunluk:]
open(sys.argv[2], 'w', encoding='utf-8').write(out)
print('yazildi', sys.argv[2], len(paketler), 'paket')
