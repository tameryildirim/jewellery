# Eğitim videosu anlatımı: PLAN ortam değişkenindeki plan-<id>.json'dan okunur (klon-sunum-uret.py arayüzü: PARCALAR, VIDEO_SURE).
# Başlangıçlar kayıttan önce bilinmez → 1000 sn aralıklı yer tutucu (pencere denetimi devre dışı).
import json, os
_p = json.load(open(os.environ["PLAN"]))
PARCALAR = [(a["id"], f'{_p["baslik"]} {a["sira"]}/{a["toplam"]} · {a["baslik"]}', i * 1000.0, [tuple(c) for c in a["cumleler"]]) for i, a in enumerate(_p["adimlar"])]
VIDEO_SURE = len(_p["adimlar"]) * 1000.0
