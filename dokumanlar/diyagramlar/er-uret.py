#!/usr/bin/env python3
"""
ER DİYAGRAMI ÜRETECİ — Kuyumcu ERP

Diyagramlar ELLE ÇİZİLMEZ; canlı veritabanı şemasından üretilir.
Gerekçe: elle çizilen diyagram ilk şema değişikliğinde yalan olur.
Bu betik belgenin kendisini yeniden üretilebilir kılar.

Kullanım:
    python3 er-uret.py                 # .mmd dosyalarını üretir
    ./er-ciz.sh                        # .mmd -> .svg (mermaid-cli)

Kaynak: MySQL information_schema. Bağlantı KUYUMCU_ER_DSN ile değiştirilebilir.
"""
import collections
import os
import subprocess
import sys

VT = os.environ.get("KUYUMCU_ER_VT", "kuyumcu_net")
KAP = os.environ.get("KUYUMCU_ER_KAP", "kuyumcu-mysql")
SIFRE = os.environ.get("KUYUMCU_ER_SIFRE", "8yIXtcIoxE9t9V01DIvIry9uxcZLr3")
CIKTI = os.path.dirname(os.path.abspath(__file__))

# ── Konu alanları (bounded context) ──────────────────────────────────────────
# Tek diyagramda 21 tablo okunmuyor: her tablo tenants'a bağlı olduğu için
# görsel bir saç yumağı çıkıyor. Profesyonel pratik konu alanına bölmektir.
ALANLAR = {
    "kiracı-ve-kimlik": {
        "baslik": "Kiracı, Şube ve Kimlik Yönetimi",
        "tablolar": ["tenants", "subeler", "kullanicilar", "refresh_tokenler", "rol_yetkileri"],
    },
    "stok-ve-islem": {
        "baslik": "Ürün, Stok ve İşlem Akışı",
        "tablolar": ["urunler", "stoklar", "hareketler", "kur_snapshots", "subeler", "cariler"],
    },
    "finans": {
        "baslik": "Cari, Kasa, Banka ve Fatura",
        "tablolar": ["cariler", "cari_bakiyeler", "kasalar", "kasa_hareketleri",
                     "banka_hesaplari", "banka_hareketleri", "faturalar", "hareketler"],
    },
    "uyum": {
        "baslik": "Yasal Uyum (MASAK, Gider Pusulası, Denetim İzi)",
        "tablolar": ["masak_kimlik_tespitleri", "masak_beyanlari", "masak_supheli_bildirimler",
                     "gider_pusulalari", "denetim_kayitlari", "hareketler", "cariler"],
    },
}

# xxx_id → hedef tablo. Türkçe çoğul ekleri düzensiz olduğu için elle eşlenir;
# tahmin edilmez (Anayasa md.1).
HEDEF = {
    "cari_id": "cariler", "urun_id": "urunler", "sube_id": "subeler",
    "karsi_sube_id": "subeler", "kullanici_id": "kullanicilar",
    "tespit_eden_kullanici_id": "kullanicilar", "alan_kullanici_id": "kullanicilar",
    "bildiren_kullanici_id": "kullanicilar", "kasa_id": "kasalar",
    "hareket_id": "hareketler", "fatura_id": "faturalar",
    "banka_hesap_id": "banka_hesaplari", "banka_hareket_id": "banka_hareketleri",
    "kur_snapshot_id": "kur_snapshots", "kimlik_tespiti_id": "masak_kimlik_tespitleri",
    "tenant_id": "tenants",
}

# Hedefi tablo OLMAYAN alanlar:
#   ayar_id   → ayar (K14/K18/K22/K24) kodda sabittir, tablo değildir
#   varlik_id → polimorfik: denetim kaydının işaret ettiği herhangi bir varlık
TABLO_OLMAYAN = {"ayar_id", "varlik_id"}


def sema_oku():
    sorgu = (
        "SELECT CONCAT(TABLE_NAME,'|',COLUMN_NAME,'|',COLUMN_TYPE,'|',"
        "IS_NULLABLE,'|',IFNULL(COLUMN_KEY,'')) FROM information_schema.COLUMNS "
        f"WHERE TABLE_SCHEMA='{VT}' AND TABLE_NAME<>'__EFMigrationsHistory' "
        "ORDER BY TABLE_NAME, ORDINAL_POSITION;"
    )
    ham = subprocess.run(
        ["docker", "exec", KAP, "mysql", "-uroot", f"-p{SIFRE}", "-N", "-e", sorgu],
        capture_output=True, text=True, check=True).stdout
    tablolar = collections.OrderedDict()
    for satir in ham.splitlines():
        if not satir.strip() or "Warning" in satir:
            continue
        t, c, tip, nul, key = satir.split("|")
        tablolar.setdefault(t, []).append((c, tip, nul, key))
    return tablolar


def fk_sayisi():
    """Veritabanında ENFORCE EDİLEN yabancı anahtar sayısı — iddia değil ölçüm."""
    sorgu = ("SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS "
             f"WHERE TABLE_SCHEMA='{VT}' AND CONSTRAINT_TYPE='FOREIGN KEY';")
    ham = subprocess.run(
        ["docker", "exec", KAP, "mysql", "-uroot", f"-p{SIFRE}", "-N", "-e", sorgu],
        capture_output=True, text=True, check=True).stdout
    return int([s for s in ham.splitlines() if s.strip().isdigit()][0])


def tip_ad(t):
    tam = t.lower()
    kok = tam.split("(")[0].split(" ")[0]
    if kok == "tinyint":
        return "bool" if "(1)" in tam else "byte"
    return {"varchar": "string", "char": "string", "longtext": "text", "text": "text",
            "datetime": "datetime", "timestamp": "datetime", "decimal": "decimal",
            "int": "int", "bigint": "long", "date": "date", "double": "double",
            "float": "float", "varbinary": "binary", "blob": "binary"}.get(kok, kok)


def diyagram(tablolar, kapsam=None, baslik="", kiraci_baglari=True, fk=0):
    if kapsam:
        secili = collections.OrderedDict(
            (t, k) for t, k in tablolar.items() if t in kapsam)
    else:
        secili = tablolar

    sat = [
        f"%% {baslik}",
        "%% ŞEMADAN ÜRETİLMİŞTİR — elle çizilmemiştir (er-uret.py).",
        f"%% Kaynak: MySQL information_schema / {VT}",
        f"%% Veritabanında enforce edilen FK kısıtı: {fk} (ölçüldü).",
        "%% FK işaretleri MANTIKSAL yabancı anahtardır; bütünlük uygulama katmanında korunur.",
        "erDiagram",
    ]
    for t, kolonlar in secili.items():
        sat.append(f"    {t} {{")
        for c, tip, nul, key in kolonlar:
            bayrak = "PK" if key == "PRI" else ("UK" if key == "UNI" else
                                                ("FK" if c.endswith("_id") else ""))
            yorum = ' "kiraci"' if c == "tenant_id" else (' "mantiksal"' if bayrak == "FK" else "")
            sat.append(f"        {tip_ad(tip)} {c} {bayrak}{yorum}".rstrip())
        sat.append("    }")

    iliskiler = set()
    for t, kolonlar in secili.items():
        for c, _, nul, key in kolonlar:
            if not c.endswith("_id") or key == "PRI" or c in TABLO_OLMAYAN:
                continue
            if c == "duzeltilen_id":
                iliskiler.add((t, t, "düzeltir", "zero"))
                continue
            if c == "tenant_id" and not kiraci_baglari:
                continue
            h = HEDEF.get(c)
            if not h or h == t or h not in secili:
                continue
            iliskiler.add((h, t, c[:-3], "zero" if nul == "YES" else "one"))

    sat.append("")
    for h, t, ad, kard in sorted(iliskiler):
        ok = "o{" if kard == "zero" else "|{"
        sat.append(f'    {h} ||--{ok} {t} : "{ad}"')
    return "\n".join(sat) + "\n", len(secili), len(iliskiler)


def main():
    tablolar = sema_oku()
    fk = fk_sayisi()
    sutun = sum(len(k) for k in tablolar.values())
    print(f"şema okundu: {len(tablolar)} tablo · {sutun} sütun · enforce FK: {fk}")

    uretilen = []
    # Genel görünüm — kiracı bağları GİZLİ (her tablo tenants'a bağlı; çizilirse okunmaz)
    icerik, t, i = diyagram(tablolar, None, "Kuyumcu ERP — Genel Veri Modeli", False, fk)
    yol = os.path.join(CIKTI, "er-genel.mmd")
    open(yol, "w", encoding="utf-8").write(icerik)
    uretilen.append(("er-genel", t, i))

    # Tam görünüm — kiracı bağları DAHİL (ek/arşiv)
    icerik, t, i = diyagram(tablolar, None, "Kuyumcu ERP — Tam Veri Modeli (kiracı bağları dahil)", True, fk)
    open(os.path.join(CIKTI, "er-tam.mmd"), "w", encoding="utf-8").write(icerik)
    uretilen.append(("er-tam", t, i))

    # Konu alanları
    for ad, tanim in ALANLAR.items():
        icerik, t, i = diyagram(tablolar, set(tanim["tablolar"]),
                                f"Kuyumcu ERP — {tanim['baslik']}", False, fk)
        open(os.path.join(CIKTI, f"er-{ad}.mmd"), "w", encoding="utf-8").write(icerik)
        uretilen.append((f"er-{ad}", t, i))

    print()
    for ad, t, i in uretilen:
        print(f"  {ad+'.mmd':<32} {t:>2} tablo · {i:>2} ilişki")
    return 0


if __name__ == "__main__":
    sys.exit(main())
