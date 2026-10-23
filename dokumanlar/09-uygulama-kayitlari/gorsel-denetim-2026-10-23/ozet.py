#!/usr/bin/env python3
"""Görsel denetim 2 — ham/*.jsonl → özet tablolar (stdout, markdown) + bulgu adayları + temas sayfaları (foto/sayfa/*.jpg)."""
import json, os, glob, collections, sys
S = os.path.dirname(os.path.abspath(__file__))
def oku(ad):
    p = f'{S}/ham/{ad}.jsonl'
    return [json.loads(l) for l in open(p)] if os.path.exists(p) else []

def matris(kim):
    satirlar = oku('matris-' + kim)
    print(f'\n### Matris — {kim} ({len(satirlar)} durum)\n')
    print('| Durum | Rota | K1 gizli | K3 taşan/kırık | K3c | K9 binen | K10 elipsis | K12 | Yatay | CLS | Boy sayısı | Ölçek dışı | Kontrast<4,5 | Çift id | <24px hedef | Ağ/JS hata | Hata |')
    print('|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|')
    ozet = collections.defaultdict(lambda: collections.Counter())
    for s in satirlar:
        o = s['olc']; d = s['durum']; r = s['rota']
        if 'hata' in o:
            print(f"| {d} | {r} | | | | | | | | | | | | | | | {o['hata'][:60]} |"); ozet[d]['hata'] += 1; continue
        t = o['tipo']; k = o['kontrast']
        hatalar = [h for h in s['ag'] if not h.startswith('HTTP 401') and not h.startswith('HTTP 404 /yoklama')]
        vals = dict(K1=o['K1_gizliGorunen'], K3=o['K3_tasan'] + o['K3_kirik'], K3c=o['K3c_kelimeKirik'], K9=o['K9_binen'], K10=o['K10_elipsis'], K12=o['K12_dugmeDikey'], Y=o['yatay'], cls=1 if o['cls'] > 0.1 else 0, disi=len(t['olcekDisi']), kontrast=k['sayi'], cift=len(o['ciftId']), hedef=o['kucukHedef']['sayi'], ag=len(hatalar))
        for kk, v in vals.items():
            if v: ozet[d][kk] += 1
        bayrak = any(vals.values())
        print(f"| {d} | {r} | {o['K1_gizliGorunen']} | {o['K3_tasan']}/{o['K3_kirik']} | {o['K3c_kelimeKirik']} | {o['K9_binen']} | {o['K10_elipsis']} | {o['K12_dugmeDikey']} | {o['yatay']} | {o['cls']} | {t['boySayisi']} | {len(t['olcekDisi'])} | {k['sayi']} | {len(o['ciftId'])} | {o['kucukHedef']['sayi']} | {len(hatalar)} | {'**bak**' if bayrak else ''} |")
    print('\n**Durum başına sayaç>0 olan rota sayısı:**\n')
    print('| Durum | ' + ' | '.join(['K1', 'K3', 'K3c', 'K9', 'K10', 'K12', 'Yatay', 'CLS>0,1', 'Ölçek dışı', 'Kontrast', 'Çift id', 'Küçük hedef', 'Ağ/JS', 'Hata']) + ' |')
    print('|---|' + '---|' * 14)
    for d, c in ozet.items():
        print(f'| {d} | ' + ' | '.join(str(c[k]) for k in ['K1', 'K3', 'K3c', 'K9', 'K10', 'K12', 'Y', 'cls', 'disi', 'kontrast', 'cift', 'hedef', 'ag', 'hata']) + ' |')
    return satirlar

def ornekler(satirlar, kim):
    print(f'\n### Örnekler — {kim} (sayaç>0 olanların ham örnekleri)\n')
    for s in satirlar:
        o = s['olc']
        if 'hata' in o: continue
        ek = []
        if o['K3_tasan'] or o['K3_kirik']: ek.append('K3 ' + json.dumps(o['ornek'].get('tasan', [])[:2] + o['ornek'].get('kirik', [])[:2], ensure_ascii=False))
        if o['K9_binen']: ek.append('K9 ' + json.dumps(o['ornek'].get('binen', [])[:2], ensure_ascii=False))
        if o['K3c_kelimeKirik']: ek.append('K3c ' + json.dumps(o['ornek'].get('kelimeKirik', [])[:2], ensure_ascii=False))
        if o['K10_elipsis']: ek.append('K10 ' + json.dumps(o['ornek'].get('elipsis', [])[:2], ensure_ascii=False))
        if o['yatay']: ek.append('YATAY')
        if o['cls'] > 0.1: ek.append(f"CLS {o['cls']}")
        if o['tipo']['olcekDisi']: ek.append('ölçek dışı ' + json.dumps(o['tipo']['olcekDisi'][:4], ensure_ascii=False))
        if o['kontrast']['sayi']: ek.append('kontrast ' + json.dumps(o['kontrast']['ornek'][:3], ensure_ascii=False))
        if o['ciftId']: ek.append('çift id ' + json.dumps(o['ciftId'][:4]))
        if o['kucukHedef']['sayi']: ek.append('küçük hedef ' + json.dumps(o['kucukHedef']['ornek'][:3], ensure_ascii=False))
        h1 = [h for h in o['tipo']['h1'] if h['g']]
        if len(h1) != 1: ek.append(f"görünür h1 {len(h1)} " + json.dumps(o['tipo']['h1'], ensure_ascii=False)[:160])
        hatalar = [h for h in s['ag'] if not h.startswith('HTTP 401')]
        if hatalar: ek.append('ağ/JS ' + json.dumps(hatalar[:3], ensure_ascii=False))
        if ek: print(f"- **{s['durum']} {s['rota']}** → " + ' · '.join(ek))

def acilir(kim):
    L = oku('acilir-' + kim)
    if not L: return
    print(f'\n### Açılır listeler — {kim} ({len(L)} kayıt)\n')
    print('| Durum | Rota | Katman | Öğe | Açıldı | Kutu | Taşma s/l/a/ü | Örtülü nokta | Seçenek | Satır px | En uzun satır | Kesik | Kaydırma | aria açık→kapalı | Kapandı | Değer korundu | Seçili px | Girdi px | h1 px | Başlık gibi |')
    print('|---|' * 20)
    for x in L:
        if 'hata' in x: print(f"| {x.get('durum')} | {x.get('rota')} | {x.get('katman','')} | {x.get('id','')} | HATA {x['hata'][:50]} |" + ' |' * 15); continue
        if x.get('secicisiz'): print(f"| {x['durum']} | {x['rota']} | {x['katman']} | — | açıcıda seçici yok (yeni tetik {x['yeniTetik']}) |" + ' |' * 15); continue
        t = x['tasma'] or {}
        print(f"| {x['durum']} | {x['rota']} | {x['katman']} | `{x['id']}` \"{x['deger']}\" | {'evet' if x['acildi'] else 'HAYIR'} | {x['kutu']} | {t.get('sag','–')}/{t.get('sol','–')}/{t.get('alt','–')}/{t.get('ust','–')} | {x['ortulu'] if x['ortulu'] is not None else '–'} | {x['secenek']} | {x['satirBoy']} | {x['enUzunSatir']} | {x['kesik']} | {'evet' if x['kaydirma'] else 'hayır'} | {x['ariaAcik']}→{x['ariaKapali']} | {'evet' if x['kapandi'] else 'HAYIR'} | {'evet' if x['degerKorundu'] else 'HAYIR'} | {x['seciliFs']}/{x['seciliFw']} | {x['girdiFs']} | {x['h1Fs']} | {'EVET' if x['baslikGibi'] else 'hayır'} |")
    sor = [x for x in L if 'hata' not in x and not x.get('secicisiz') and (not x['acildi'] or (x['tasma'] and any(x['tasma'].values())) or x['ortulu'] or x['kesik'] or x['enUzunSatir'] > 2 or not x['kapandi'] or not x['degerKorundu'] or x['baslikGibi'] or (x['satirBoy'] and x['satirBoy'] < 40))]
    print(f'\n**Sorunlu açılır kayıt:** {len(sor)} / {len([x for x in L if "hata" not in x and not x.get("secicisiz")])}')
    for x in sor: print(f"- {x['durum']} {x['rota']} {x['katman']} `{x['id']}`: " + ', '.join(k for k, v in [('AÇILMADI', not x['acildi']), ('taşma ' + json.dumps(x['tasma']), x['tasma'] and any(x['tasma'].values())), (f"örtülü {x['ortulu']}", x['ortulu']), (f"kesik {x['kesik']}", x['kesik']), (f"{x['enUzunSatir']} satır", x['enUzunSatir'] > 2), ('KAPANMADI', not x['kapandi']), ('DEĞER DEĞİŞTİ', not x['degerKorundu']), ('BAŞLIK GİBİ', x['baslikGibi']), (f"satır {x['satirBoy']}px", x['satirBoy'] and x['satirBoy'] < 40)] if v))

def klavye(kim):
    L = oku('klavye-' + kim)
    if not L: return
    print(f'\n### Klavye turu — {kim}\n')
    print('| Rota | İlk durak | Adım | Görünmez odak | Görünmez olanlar |')
    print('|---|---|---|---|---|')
    for x in L:
        if 'hata' in x: print(f"| {x['rota']} | HATA {x['hata'][:60]} | | | |"); continue
        print(f"| {x['rota']} | {x['ilk']} | {x['adim']} | {x['gorunmez']} | {', '.join(s[:-1] for s in x['sira'] if s.endswith('!'))[:140]} |")
    print(f"\nİlk durak 'icerige-gec' olmayan rota: {[x['rota'] for x in L if x.get('ilk') not in (None, 'icerige-gec')]}")

def dil():
    L = oku('dil-sahip')
    if not L: return
    print('\n### EN dili (1280)\n')
    print('| Rota | lang | h1 | Taşan | Yatay | Türkçe kalan (veri hariç değil) | Örnek |')
    print('|---|---|---|---|---|---|---|')
    for x in L:
        if 'hata' in x: print(f"| {x['rota']} | HATA | | | | | |"); continue
        print(f"| {x['rota']} | {x['lang']} | {x['h1']} | {x['tasan']} | {x['yatay']} | {x['turkce']} | {'; '.join(x['ornek'][:3])[:120]} |")

def temas():
    try:
        from PIL import Image
    except ImportError:
        print('PIL yok'); return
    os.makedirs(f'{S}/foto/sayfa', exist_ok=True)
    for durum in sorted({os.path.basename(f).split('__')[1].rsplit('.', 1)[0] for f in glob.glob(f'{S}/foto/matris/*')}):
        dosyalar = sorted(glob.glob(f'{S}/foto/matris/*__{durum}.*'))
        W = 375 if '375' in durum else (320 if '320' in durum else 640); kol = 6 if W <= 375 else 3; H = 900 if W <= 375 else 520
        for i in range(0, len(dosyalar), kol * 2):
            grup = dosyalar[i:i + kol * 2]
            sayfa = Image.new('RGB', (kol * (W + 8), 2 * (H + 24)), 'white')
            for j, f in enumerate(grup):
                im = Image.open(f).convert('RGB'); im.thumbnail((W, H))
                x, y = (j % kol) * (W + 8), (j // kol) * (H + 24)
                sayfa.paste(im, (x, y + 20))
                try:
                    from PIL import ImageDraw
                    ImageDraw.Draw(sayfa).text((x + 2, y + 2), os.path.basename(f).split('__')[0][:40], fill='black')
                except Exception: pass
            sayfa.save(f'{S}/foto/sayfa/{durum}-{i // (kol * 2) + 1:02d}.jpg', quality=70)
    print('\ntemas sayfaları:', len(glob.glob(f'{S}/foto/sayfa/*.jpg')))

if __name__ == '__main__':
    for kim in ['sahip', 'tezgahtar']:
        s = matris(kim); ornekler(s, kim)
    acilir('sahip'); klavye('sahip'); dil()
    if '--foto' in sys.argv: temas()
