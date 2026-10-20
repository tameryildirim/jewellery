#!/usr/bin/env python3
"""Screencast karelerinden gerçek zamanlı video: kare süreleri zaman damgalarından, sabit 12 fps, H.264.
Kullanım: python3 video-yap.py <kayit_dizini> <cikti.mp4>"""
import json, os, subprocess, sys
K, CIKTI = sys.argv[1], sys.argv[2]; k = json.load(open(os.path.join(K, "kayit.json"))); kareler = k["kareler"]; T0 = k["T0"]; sure = k["sure"]
kisa = [x for x in kareler if x["w"] != 1440 or x["h"] != 900]; assert not kisa, f"1440x900 olmayan kare: {len(kisa)}"
with open(os.path.join(K, "liste.txt"), "w") as f:
    f.write("ffconcat version 1.0\n")
    # ilk kare kayıt başlangıcından (T0) itibaren görünür say
    for i, x in enumerate(kareler):
        bas = max(0.0, x["ts"] - T0) if i else 0.0
        son = (kareler[i + 1]["ts"] - T0) if i + 1 < len(kareler) else sure
        f.write(f"file 'kareler/{x['ad']}'\nduration {max(0.001, son - bas):.4f}\n")
    f.write(f"file 'kareler/{kareler[-1]['ad']}'\n")
subprocess.run(["ffmpeg", "-y", "-v", "error", "-f", "concat", "-safe", "0", "-i", os.path.join(K, "liste.txt"), "-vf", "fps=12,format=yuv420p", "-c:v", "libx264", "-profile:v", "high", "-crf", "20", "-preset", "medium", "-t", f"{sure:.3f}", "-movflags", "+faststart", CIKTI], check=True)
d = float(subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", CIKTI], capture_output=True, text=True).stdout)
print(f"video: {CIKTI} · {d:.1f} sn (kayıt {sure:.1f} sn) · {len(kareler)} kaynak kare · ilk kare T0+{kareler[0]['ts']-T0:.2f} sn")
