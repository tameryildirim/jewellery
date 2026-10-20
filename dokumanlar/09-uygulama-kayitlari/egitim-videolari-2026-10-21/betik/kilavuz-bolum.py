#!/usr/bin/env python3
"""Eğitim videoları → (1) Kuyumcu.Core/Kilavuz/EgitimVideolari.cs (üretilmiş liste: kimlik, başlık, süre, bölümler),
(2) docs/KULLANIM_KILAVUZU.md: "## Eğitim videoları {#egitim-videolari}" bölümü + ilgili ekran bölümlerine tek satır.
Kılavuz üreticisi dış bağlantıya izin vermez; videolar Kılavuz EKRANINDAN (/kilavuz) izlenir, md yalnız anlatır.
Kaynak: kayit-<id>/kayit.json (varsa; yoksa plan ve süre 0 — yer tutucu). Kullanım: python3 kilavuz-bolum.py"""
import json, os, re, subprocess
H = os.path.dirname(os.path.abspath(__file__)); C = os.path.dirname(H)
KOK = os.path.expanduser("~/ByGatech-yerel/jewellery"); MD = f"{KOK}/docs/KULLANIM_KILAVUZU.md"; CS = f"{KOK}/dotnet/Kuyumcu.Core/Kilavuz/EgitimVideolari.cs"
SIRA = ["satis", "hurda", "emanet", "banka", "fatura", "masak", "kasa-kapanis", "etiket", "rapor"]
EKRAN = {"satis": "{#satis}", "hurda": "{#hurda}", "emanet": "{#emanet}", "banka": "{#banka}", "fatura": "{#faturalar}", "masak": "{#uyum}",
         "kasa-kapanis": "{#kasalar}", "etiket": "{#etiketler}", "rapor": "{#raporlar}"}
EN = {"satis": "Sales", "hurda": "Scrap purchase", "emanet": "Safekeeping", "banka": "Bank", "fatura": "Invoices", "masak": "Compliance (MASAK)",
      "kasa-kapanis": "Register closing", "etiket": "Labels", "rapor": "Reports"}
def mmss(t): t = int(round(t)); return f"{t//60}:{t%60:02d}"
def sure(p): return float(subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", p], capture_output=True, text=True).stdout) if os.path.exists(p) else 0.0
V = []
for vid in SIRA:
    plan = json.load(open(os.path.join(H, f"plan-{vid}.json"))); ky = os.path.join(H, f"kayit-{vid}", "kayit.json")
    adimlar = json.load(open(ky))["adimlar"] if os.path.exists(ky) else [dict(bas=0.0, baslik=a["baslik"], sira=a["sira"], toplam=a["toplam"]) for a in plan["adimlar"]]
    V.append(dict(id=vid, baslik=plan["baslik"], alt=plan["alt"], en=EN[vid], sure=sure(os.path.join(C, f"egitim-{vid}.mp4")), bolumler=[(a["bas"], a["baslik"]) for a in adimlar]))
# 1) C# listesi (üretilmiş; elle düzenlenmez)
def cs(s): return '"' + s.replace('\\', '\\\\').replace('"', '\\"') + '"'
out = ["// ÜRETİLMİŞ DOSYA — dokumanlar/09-uygulama-kayitlari/egitim-videolari-2026-10-21/betik/kilavuz-bolum.py; elle düzenlenmez.",
       "// Y-01 (21.10): ekran başına eğitim videosu (Kılavuz ekranı › Eğitim videoları); dosyalar wwwroot/video/egitim-<kimlik>.mp4.",
       "namespace Kuyumcu.Core.Kilavuz;", "",
       "/// <summary>Bir eğitim videosu: kimlik (dosya adı), başlık, alt başlık, süre (sn) ve bölüm başlangıçları (sn, başlık).</summary>",
       "public sealed record EgitimVideosu(string Kimlik, string Baslik, string Alt, string BaslikEn, double SureSn, IReadOnlyList<(double Sn, string Baslik)> Bolumler)",
       "{", "    public string Dosya => $\"/video/egitim-{Kimlik}.mp4\";", "}", "",
       "public static class EgitimVideolari", "{", "    public static readonly IReadOnlyList<EgitimVideosu> Hepsi =", "    ["]
for v in V:
    b = ", ".join(f"({x:.1f}, {cs(t)})" for x, t in v["bolumler"])
    out.append(f"        new({cs(v['id'])}, {cs(v['baslik'])}, {cs(v['alt'])}, {cs(v['en'])}, {v['sure']:.1f}, [{b}]),")
out += ["    ];", "", "    /// <summary>mm:ss (kültürden bağımsız).</summary>",
        "    public static string Sure(double sn) { var t = (int)System.Math.Round(sn); return $\"{t / 60}:{t % 60:00}\"; }", "}", ""]
open(CS, "w", encoding="utf-8").write("\n".join(out))
# 2) Kılavuz md
bolum = ["## Eğitim videoları {#egitim-videolari}", "", "<!-- egitim-videolari:bas -->",
         "Her ekran için bir–iki dakikalık sesli eğitim videosu vardır. **Kılavuz** ekranının (Yönetim › Kılavuz) **Eğitim videoları** listesinden izlenir; her videonun altında bölüm başlangıçları yazar, videoda da bölüm işaretleri vardır. Videolar örnek verilerle çekilmiştir; tutarlar örnektir.", ""]
satirlar = {}
for v in V:
    bl = " · ".join(f"{mmss(x)} {t}" for x, t in v["bolumler"])
    bolum.append(f"- **{v['baslik']} — {v['alt']}** ({mmss(v['sure'])}): {bl}")
    satirlar[v["id"]] = f"**Eğitim videosu:** [{v['baslik']} — {v['alt']} ({mmss(v['sure'])})](#egitim-videolari) — Kılavuz ekranından izlenir."
bolum += ["", "<!-- egitim-videolari:son -->", ""]
s = open(MD, encoding="utf-8").read()
if "<!-- egitim-videolari:bas -->" in s:
    s = re.sub(r"## Eğitim videoları \{#egitim-videolari\}\n\n<!-- egitim-videolari:bas -->.*?<!-- egitim-videolari:son -->\n\n", lambda m: "\n".join(bolum), s, flags=re.S)
else:
    s = s.replace("## Bu sürümde olmayanlar {#haric}", "\n".join(bolum) + "## Bu sürümde olmayanlar {#haric}")
for vid, capa in EKRAN.items():
    m = re.search(r"^##+ [^\n]*" + re.escape(capa) + r"[^\n]*\n", s, re.M); assert m, capa
    bas = m.end(); son = s.find("\n## ", bas); blok = s[bas:son]
    yeni = re.sub(r"\*\*Eğitim videosu:\*\*[^\n]*\n\n", "", blok)
    i = yeni.find("-->\n\n"); i = (i + 5) if i >= 0 else 0
    yeni = yeni[:i] + satirlar[vid] + "\n\n" + yeni[i:]
    s = s[:bas] + yeni + s[son:]
open(MD, "w", encoding="utf-8").write(s); print("üretildi:", CS.split('/')[-1], "+ kılavuz md ·", len(V), "video ·", sum(1 for v in V if v["sure"] > 0), "hazır")
