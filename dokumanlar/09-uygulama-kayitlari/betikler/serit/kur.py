# Şerit planı üretici (orkestratör 27.09). Planlar KALICI klasörde: betikler/serit/ (25.09 geçici klasör yeniden başlatmada silindi).
import json, re, sys
KOK='/Users/ramazanakkocan/ByGatech-yerel/jewellery'
TEMEL=open(f'{KOK}/dokumanlar/09-uygulama-kayitlari/betikler/plan-satis-son.js').read()
PK=json.load(open('paketler-cikarim.json')); OZ=json.load(open('durum-ozeti.json'))
YETKI = ("⚠ YETKİ (kurucu, bu oturumda açıkça — BAĞLAYICI): 27.09 'dostum biz kaldığımız yerden devam edelim lütfen'; 25.09 'satış alış ne kadar ekran varsa hepsi için aynı anlayışı bekliyorum', "
         "'her yeri tekrar adam gibi istisnasız olarak gez ... bu tür hataları da tüm ekranlar bazında gez ve değiştir', 'front end'de de çalışsın, kontrol etsin, adam gibi tasarım istiyorum'; 28.09 'dostum ben sana bana soru sorma devam et demedim mi' (kuyruk için DAİMİ onay); 28.09 11:30 'işten kısma, şeritler vs hepsi bitsin, hızlanalım'. "
         "Bu paketteki kod değişikliği kurucunun EMRİDİR (CLAUDE.md 'izinsiz değişiklik yok' kuralı bu emirle karşılanmıştır). COMMIT YOK kuralı AYNEN geçerli.")
TASARIM_EK = open('tasarim_ek.js').read()
def uret(ad, dalga, paketler):
  s = TEMEL
  L = s.split('\n'); i=[n for n,l in enumerate(L) if l.startswith('const GOMULU = ')][0]
  g = json.loads(L[i][len('const GOMULU = '):].rstrip(';')); g['dalga']=dalga; g['paketler']=paketler
  L[i]='const GOMULU = '+json.dumps(g,ensure_ascii=False)+';'; s='\n'.join(L)
  s = s.replace("const SCRATCH = '/private/tmp/claude-501/-Users-ramazanakkocan-Desktop-ByGatech-projeler-jewellery/f8597a15-bda0-4765-a57f-77f2b6380580/scratchpad'", "const SCRATCH = '/private/tmp/kuyumcu-serit'")
  s = s.replace("const KURALLAR = `\n", "const KURALLAR = `\n"+YETKI.replace('`','\\`')+"\n", 1)
  s = s.replace("name: 'kuyumcu-satis-son'", f"name: 'kuyumcu-{ad}'")
  s = s.replace("    { title: 'Hakem',", "    { title: 'Tasarim', detail: 'Tasarim hakemi (Opus): raporu gormeden ekrani kendisi acar, doldurur, olcer; katalog K1-K23', model: 'opus' },\n    { title: 'Hakem',", 1)
  s = s.replace("const sonuclar = []", TASARIM_EK + "\nconst sonuclar = []", 1)
  old = "    if (dogrulama.karar === 'GECTI' || tur === 3) break"
  assert old in s
  s = s.replace(old, open('tasarim_dongu.js').read().rstrip('\n') + "\n" + old, 1)
  s = s.replace("{ label: `dogrula:p${p.no}:t${tur}`, phase: 'Dogrulama', effort: 'medium', schema: DOGRULAMA }", "{ label: `dogrula:p${p.no}:t${tur}`, phase: 'Dogrulama', model: 'opus', effort: 'medium', schema: DOGRULAMA }")
  assert "model: 'opus', effort: 'medium', schema: DOGRULAMA" in s and 'SCRATCH = \'/private/tmp/kuyumcu-serit\'' in s and 'YETKİ (kurucu' in s
  open(f'serit-{ad}.js','w').write(s); print('yazildi', ad, [p['no'] for p in paketler])
def devir(no, metin):
  P = dict(PK[no]); P['kosullar'] = [metin] + [k for k in P['kosullar'] if not k.startswith('⚠ DEVIR NOTU (orkestrator 25.09 09:28)')]; return P
if __name__ == '__main__':
  exec(open('paketler-ek.py').read())
