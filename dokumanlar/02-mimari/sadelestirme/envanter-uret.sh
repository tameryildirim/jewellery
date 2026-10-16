#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# ENVANTER ÜRETİCİ — "Özellik kaybı = 0" sayacının tabanı (PLAN § 7.1, S-0 md.4)
#
# SALT OKUNUR: kaynak ağaçta hiçbir dosyayı değiştirmez; yalnız --cikti ile
# verilen JSON dosyasını yazar. Varsayılan çıktı: bu betiğin yanındaki
# ENVANTER-S0.json.  S-3/S-4/S-6 sonunda aynı betik koşar, diff BOŞ olmalı.
#
# Kullanım:
#   ./envanter-uret.sh                      # varsayılan çıktı (ENVANTER-S0.json)
#   ./envanter-uret.sh --cikti /yol/x.json  # karşılaştırma turu için
#
# Ölçülemeyen hiçbir şey tahmin edilmez; dinamik id (id="@(...)"), rota şablonu
# gibi makineyle çözülemeyen kayıtlar "OLCULEMEDI_*" etiketiyle yazılır
# (Anayasa md. 1).
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

BETIK_DIZIN="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
KOK="$(cd "$BETIK_DIZIN/../../.." && pwd)"          # .../projeler/jewellery
CIKTI="$BETIK_DIZIN/ENVANTER-S0.json"

while [ $# -gt 0 ]; do
  case "$1" in
    --cikti) CIKTI="$2"; shift 2 ;;
    --kok)   KOK="$2";   shift 2 ;;
    *) echo "bilinmeyen argüman: $1" >&2; exit 2 ;;
  esac
done

WEB="$KOK/dotnet/Kuyumcu.Web"
API="$KOK/dotnet/Kuyumcu.Api"
[ -d "$WEB" ] || { echo "Kuyumcu.Web bulunamadı: $WEB" >&2; exit 3; }
[ -d "$API" ] || { echo "Kuyumcu.Api bulunamadı: $API" >&2; exit 3; }

command -v python3 >/dev/null || { echo "python3 gerekli" >&2; exit 4; }

KOK="$KOK" WEB="$WEB" API="$API" CIKTI="$CIKTI" python3 - <<'PY'
import os, re, json, hashlib, datetime, pathlib

KOK = pathlib.Path(os.environ["KOK"])
WEB = pathlib.Path(os.environ["WEB"])
API = pathlib.Path(os.environ["API"])
CIKTI = pathlib.Path(os.environ["CIKTI"])

olculemedi = []

# ── 1. ROTALAR (@page) ───────────────────────────────────────────────────────
rotalar = []
sayfa_rota = {}          # dosya adı -> [rota]
for f in sorted((WEB / "Pages").glob("*.razor")):
    metin = f.read_text(encoding="utf-8", errors="replace")
    bulunan = re.findall(r'^@page\s+"([^"]+)"', metin, re.M)
    for r in bulunan:
        rotalar.append({"rota": r, "dosya": f"Pages/{f.name}",
                        "sablon": "{" in r})
    sayfa_rota[f.name] = bulunan
    if not bulunan:
        olculemedi.append(f"Pages/{f.name}: @page yönergesi yok")

# ── 2. MENÜ ÖĞELERİ (MenuTanimi.Ogeler + AltBlok + MenusuzRotalar) ───────────
mt = (WEB / "Layout" / "MenuTanimi.cs").read_text(encoding="utf-8")

def blok(ad):
    i = mt.index(f"{ad} =")
    j = mt.index("];", i)
    return mt[i:j]

def menu_ayikla(govde, kaynak):
    out = []
    for m in re.finditer(
        r'new\(MenuGrubu\.(\w+),\s*"([^"]+)",\s*(null|"[^"]*"),\s*\[([^\]]*)\]',
        govde):
        roller = [x.strip().strip('"') for x in m.group(4).split(",") if x.strip()]
        out.append({"grup": m.group(1), "yol": m.group(2),
                    "modul": None if m.group(3) == "null" else m.group(3).strip('"'),
                    "roller": roller, "kaynak": kaynak})
    return out

menu_ogeleri = menu_ayikla(blok("Ogeler"), "Ogeler") + \
               menu_ayikla(blok("AltBlok"), "AltBlok")

menusuz = dict(re.findall(r'\["([^"]+)"\]\s*=\s*"([^"]+)"', blok("MenusuzRotalar")))

grup_sirasi = re.findall(r'MenuGrubu\.(\w+),', blok("GrupSirasi"))

# ── 3. BASİT MOD ROTALARI (BasitMenu.Yollar) ────────────────────────────────
bm = (WEB / "Servisler" / "BasitMenu.cs").read_text(encoding="utf-8")
i = bm.index("Yollar ="); j = bm.index("];", i)
basit_yollar = re.findall(r'"([^"]+)"', bm[i:j])

# ── 4. POS İŞLEM TÜRLERİ (Islemler.razor TumTurler) ─────────────────────────
isl = (WEB / "Pages" / "Islemler.razor").read_text(encoding="utf-8")
i = isl.index("TumTurler =>"); j = isl.index("];", i)
pos_turleri = re.findall(r'\("([A-Z_]+)",', isl[i:j])

# ── 5. SAYFA EYLEMLERİ ──────────────────────────────────────────────────────
# Ölçülen iki kayıt: (a) statik DOM id'leri (§ 3.7 sözleşmesi),
# (b) @onclick işleyici adları. Dinamik id'ler sayılır ama adlandırılamaz.
ID_RE      = re.compile(r'\bid="([A-Za-z][A-Za-z0-9:_-]*)"')
ID_DIN_RE  = re.compile(r'\bid="@')
ONCLICK_RE = re.compile(r'@onclick="@?\(?\s*(?:\(\s*\)\s*=>\s*)?([A-Za-z_][A-Za-z0-9_.]*)')

sayfa_eylemleri = {}
for f in sorted((WEB / "Pages").glob("*.razor")):
    metin = f.read_text(encoding="utf-8", errors="replace")
    statik = sorted(set(ID_RE.findall(metin)))
    dinamik = len(ID_DIN_RE.findall(metin))
    isleyici = sorted(set(ONCLICK_RE.findall(metin)))
    anahtarlar = sayfa_rota.get(f.name) or [f"OLCULEMEDI_ROTASIZ:Pages/{f.name}"]
    for rota in anahtarlar:
        sayfa_eylemleri[rota] = {
            "dosya": f"Pages/{f.name}",
            "dom_id": statik,
            "dom_id_sayisi": len(statik),
            "dinamik_id_sayisi": dinamik,
            "onclick_isleyici": isleyici,
            "onclick_sayisi": len(isleyici),
            "gorunurluk": "OLCULMEDI_S0",   # S-1+ turlarında: gorunur|katli|pro
        }
    if dinamik:
        olculemedi.append(
            f"Pages/{f.name}: {dinamik} dinamik id (id=\"@...\") — statik ad çözülemedi")

# ── 6. PAYLAŞILAN BİLEŞEN id'LERİ (sayfa dışı, id sözleşmesine dahil) ───────
bilesen_id = {}
for f in sorted((WEB / "Bilesenler").rglob("*.razor")):
    metin = f.read_text(encoding="utf-8", errors="replace")
    statik = sorted(set(ID_RE.findall(metin)))
    if statik:
        bilesen_id[str(f.relative_to(WEB))] = statik
for f in sorted((WEB / "Layout").rglob("*.razor")):
    metin = f.read_text(encoding="utf-8", errors="replace")
    statik = sorted(set(ID_RE.findall(metin)))
    if statik:
        bilesen_id[str(f.relative_to(WEB))] = statik

# ── 7. API UÇLARI ([Route] sınıf öneki + [HttpX("...")]) ────────────────────
api_uclari = []
# [HttpPost("x")] ve [HttpPost("x"), RolGerekli(...)] biçimlerinin ikisi de yakalanır
HTTP_RE  = re.compile(r'\[Http(Get|Post|Put|Patch|Delete|Head|Options)(?:\("([^"]*)"\))?(?=[\],])')
ROUTE_RE = re.compile(r'\[Route\("([^"]+)"\)\]')
for f in sorted(API.rglob("*.cs")):
    if any(p in f.parts for p in ("obj", "bin")):
        continue
    metin = f.read_text(encoding="utf-8", errors="replace")
    satirlar = metin.splitlines()
    onek = None
    for n, s in enumerate(satirlar, 1):
        if s.lstrip().startswith("//"):      # yorumdaki [HttpGet] uç değildir
            continue
        r = ROUTE_RE.search(s)
        if r:
            onek = r.group(1)
            continue
        h = HTTP_RE.search(s)
        if h:
            fiil, yol = h.group(1).upper(), h.group(2)
            if onek is None:
                tam = f"OLCULEMEDI_ONEKSIZ:{yol or ''}"
                olculemedi.append(
                    f"{f.relative_to(KOK)}:{n}: [Http{fiil}] sınıf [Route] öneki bulunamadı")
            else:
                tam = "/" + onek if not yol else "/" + onek + "/" + yol
            api_uclari.append({"fiil": fiil, "yol": tam,
                               "dosya": str(f.relative_to(KOK)), "satir": n})

api_uclari.sort(key=lambda x: (x["yol"], x["fiil"]))

# ── 8. ÖZET + YAZIM ─────────────────────────────────────────────────────────
envanter = {
    "surum": "S-0",
    "uretim_zamani_utc": datetime.datetime.now(datetime.timezone.utc)
                          .strftime("%Y-%m-%dT%H:%M:%SZ"),
    "kok": str(KOK),
    "uretici": "dokumanlar/02-mimari/sadelestirme/envanter-uret.sh",
    "kural": "Her kayıt S-6'da gorunur|katli|pro durumundan birinde olmalı; "
             "kaybolan kayıt = dalga KALDI (PLAN § 7.1).",
    "ozet": {
        "rota": len(rotalar),
        "sayfa_dosyasi": len(sayfa_rota),
        "menu_ogesi": len(menu_ogeleri),
        "menusuz_rota": len(menusuz),
        "basit_mod_rota": len(basit_yollar),
        "pos_turu": len(pos_turleri),
        "statik_dom_id_sayfalarda": sum(v["dom_id_sayisi"] for v in sayfa_eylemleri.values()),
        "statik_dom_id_bilesenlerde": sum(len(v) for v in bilesen_id.values()),
        "onclick_isleyici": sum(v["onclick_sayisi"] for v in sayfa_eylemleri.values()),
        "api_ucu": len(api_uclari),
        "olculemedi_kalem": len(olculemedi),
    },
    "rotalar": rotalar,
    "menu_gruplari": grup_sirasi,
    "menu_ogeleri": menu_ogeleri,
    "menusuz_rotalar": menusuz,
    "basit_mod_rotalari": basit_yollar,
    "pos_turleri": pos_turleri,
    # Görev şartnamesinin istediği düz biçim: {rota: [eylem]} — S-6 diff'i bunun
    # üzerinden alınır (ayrıntı için sayfa_eylemleri_ayrinti).
    "sayfa_eylemleri": {
        r: sorted(["id:" + i for i in v["dom_id"]] +
                  ["onclick:" + h for h in v["onclick_isleyici"]])
        for r, v in sayfa_eylemleri.items()
    },
    "sayfa_eylemleri_ayrinti": sayfa_eylemleri,
    "bilesen_dom_id": bilesen_id,
    "api_uclari": api_uclari,
    "olculemedi": olculemedi,
}

govde = json.dumps(envanter, ensure_ascii=False, indent=2, sort_keys=False)
CIKTI.write_text(govde + "\n", encoding="utf-8")
print(f"yazıldı: {CIKTI}")
print("özet:", json.dumps(envanter["ozet"], ensure_ascii=False))
print("sha256:", hashlib.sha256(govde.encode("utf-8")).hexdigest())
PY
