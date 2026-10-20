#!/usr/bin/env python3
"""Eğitim videosu üretimi: kayit-<id>/ (kareler + kayit.json) + klipler-egitim-<id>.json → egitim-<id>.mp4 (anlatım, −16 LUFS,
bölüm işaretleri) + BOLUMLER-<id>.md. Kullanım: python3 uret-egitim.py <id> [<id> ...]"""
import json, os, subprocess, sys
H = os.path.dirname(os.path.abspath(__file__)); C = os.path.dirname(H)
U = os.path.expanduser("~/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/demo-kayit-2026-10-01-uzun/seslendirme")
GECIKME = 0.4
def kos(*a): return subprocess.run(list(a), check=True, capture_output=True, text=True)
def sure(p): return float(kos("ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", p).stdout)
def mmss(t): t = int(round(t)); return f"{t//60:02d}:{t%60:02d}"
for vid in sys.argv[1:]:
    K = os.path.join(H, f"kayit-{vid}"); k = json.load(open(os.path.join(K, "kayit.json"))); plan = json.load(open(os.path.join(H, f"plan-{vid}.json")))
    assert k["gercekSure"], "kayıt gerçek klip süreleriyle çekilmemiş"; assert not k["sorunlar"], f"kayıtta sorun var: {k['sorunlar']}"
    sessiz = os.path.join(K, "sessiz.mp4"); print(kos("python3", os.path.join(H, "video-yap.py"), K, sessiz).stdout.strip())
    S = sure(sessiz)
    klipler = {x["id"]: x for x in json.load(open(os.path.join(U, f"klipler-egitim-{vid}.json")))}
    girdiler, filtre, etiketler = [], [], []
    for i, a in enumerate(k["adimlar"]):
        c = klipler[a["id"]]; bas = a["bas"] + GECIKME; son = k["adimlar"][i + 1]["bas"] if i + 1 < len(k["adimlar"]) else S
        assert bas + c["net"] <= son + 0.05, f"{a['id']}: klip sığmıyor ({bas + c['net']:.1f} > {son:.1f})"
        girdiler += ["-i", c["wav"]]; filtre.append(f"[{i+1}:a]adelay={int(bas*1000)}|{int(bas*1000)},apad[a{i}]"); etiketler.append(f"[a{i}]")
    karisim = os.path.join(K, "anlatim.wav")
    fc = ";".join(filtre) + ";" + "".join(etiketler) + f"amix=inputs={len(etiketler)}:normalize=0,atrim=0:{S:.3f},loudnorm=I=-16:TP=-1.5:LRA=11[out]"
    kos("ffmpeg", "-y", "-v", "error", "-i", sessiz, *girdiler, "-filter_complex", fc, "-map", "[out]", "-ar", "48000", "-ac", "2", karisim)
    meta = os.path.join(K, "bolumler.ffmeta"); bol = k["bolumler"] + [{"grup": "Bitiş", "bas": k["adimlar"][-1]["son"]}]
    with open(meta, "w") as f:
        f.write(";FFMETADATA1\n")
        for j, bb in enumerate(bol):
            b0 = int(bb["bas"] * 1000); b1 = int((bol[j + 1]["bas"] if j + 1 < len(bol) else S) * 1000)
            f.write(f"[CHAPTER]\nTIMEBASE=1/1000\nSTART={b0}\nEND={max(b0+1, b1)}\ntitle={bb['grup']}\n")
    cikti = os.path.join(C, f"egitim-{vid}.mp4")
    kos("ffmpeg", "-y", "-v", "error", "-i", sessiz, "-i", karisim, "-i", meta, "-map_metadata", "2", "-map", "0:v", "-map", "1:a", "-c:v", "copy", "-c:a", "aac", "-b:a", "128k", "-movflags", "+faststart", cikti)
    with open(os.path.join(C, f"BOLUMLER-{vid}.md"), "w") as f:
        f.write(f"# {plan['baslik']} — {plan['alt']} (`egitim-{vid}.mp4`, {mmss(S)})\n\n| Başlangıç | Bölüm | Çözdüğü sorun |\n|---|---|---|\n")
        for a in k["adimlar"]: f.write(f"| {mmss(a['bas'])} | {a['sira']}/{a['toplam']} {a['baslik']} | {plan['adimlar'][a['sira']-1]['sorun']} |\n")
    print(f"HAZIR {cikti.split('/')[-1]} · {mmss(S)} · {os.path.getsize(cikti)//1024} KB · {len(k['adimlar'])} bölüm")
