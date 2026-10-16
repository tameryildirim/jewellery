import json,glob,collections,sys
D=sys.argv[1] if len(sys.argv)>1 else 'sonuc2'
K=[json.loads(l) for f in glob.glob(f'{D}/olcum-*.jsonl') for l in open(f)]
sayfa=[k for k in K if k['tip'] in('sayfa','sekme')]; kat=[k for k in K if k['tip']=='katman']; hata=[k for k in K if k['tip']=='hata']
gecersiz=[k for k in sayfa if k['tip']=='sayfa' and not k.get('gecerli',True)]
print(f"kayit {len(K)} · sayfa {len([k for k in sayfa if k['tip']=='sayfa'])} · sekme {len([k for k in sayfa if k['tip']=='sekme'])} · katman {len(kat)} (acilan {sum(1 for k in kat if k.get('acildi'))}) · hata {len(hata)} · gecersiz {len(gecersiz)}")
kombo=collections.Counter((k['MOD'],k['TEMA'],k['w']) for k in sayfa if k['tip']=='sayfa'); print('kombinasyon sayisi', len(kombo), 'eksik:', [c for c in kombo if kombo[c]<42][:10])
QA=lambda t: t.upper().startswith('QA-TEST') or t.lower() in ('e-posta','r-zarar','qa-frontend')
def sorun(k):
  s=[]
  if k.get('K1_gizliGorunen'): s.append('K1 gizli-gorunen')
  if k.get('K3_tasan'): s.append('K3 tasan '+';'.join(k['ornek']['tasan'][:2]))
  if k.get('K3_kirik'): s.append('K3 kirik '+';'.join(k['ornek']['kirik'][:3]))
  if k.get('K4_boyKat',0)>2 and k.get('K4_bolum',0)>=4: s.append(f"K4 tren {k['K4_boyKat']}x{k['K4_bolum']}")
  if k.get('K5_TRY'): s.append('K5 TRY')
  if k.get('K7_hizasiz'): s.append('K7 '+';'.join(k['ornek']['hiza'][:2]))
  j=[t for t in k['ornek'].get('jargon',[]) if not QA(t)]
  if j: s.append('K8 jargon '+','.join(j))
  if k.get('K9_binen'): s.append('K9 binen '+';'.join(k['ornek']['binen'][:2]))
  if k.get('K9b_etiketBin'): s.append('K9b etiket '+';'.join(k['ek']['etiketBin'][:2]))
  if k.get('K10_elipsis') or k.get('K10b_kesik'): s.append('K10 kesik '+';'.join((k['ornek'].get('elipsis',[])+k['ek']['kes'])[:3]))
  if k.get('K11_hataBandi'): s.append('K11 HATA BANDI')
  if k.get('K12_ezik'): s.append('K12 ezik '+';'.join(k['ek']['ezik'][:3]))
  if k.get('K13_kirp'): s.append('K13 kirpilan '+';'.join(k['ek']['kirp'][:3]))
  if k.get('yatay',0)>0: s.append(f"YATAY {k['yatay']}px")
  return s
# katman taban yazı boyu: bağlı açılır listelerde en sık boy
boy=collections.Counter(b for k in kat if k.get('tur')=='bagli-acilir' for b in k.get('yaziBoylari',[]))
print('bagli-acilir yazi boylari', boy.most_common(5))
def ksorun(k):
  s=[]
  if not k.get('acildi'): return s
  if k['tur']=='SERBEST-YUZEN': s.append('SERBEST-YUZEN katman')
  if k.get('ekranDisi'): s.append('ekran-disi '+str(k['kutu']))
  if abs(k.get('sayfaKaydi',0))>40: s.append(f"sayfa-kaydi {k['sayfaKaydi']}")
  kucuk=[b for b in k.get('yaziBoylari',[]) if float(b[:-2])<14]
  if kucuk: s.append('kucuk-yazi '+','.join(kucuk))
  if k.get('icTasan'): s.append(f"ic-tasan {k['icTasan']}")
  if k.get('hataBandi'): s.append('HATA BANDI')
  if k['tur']=='bagli-acilir' and k['kutu'][2]<320 and k.get('rol')=='listbox': s.append(f"dar-liste {k['kutu'][2]}")
  return s
rota=collections.defaultdict(lambda: collections.defaultdict(set))
for k in sayfa:
  if k['tip']=='sayfa' and not k.get('gecerli',True): continue
  for s in sorun(k):
    anahtar=s.split(' ')[0]+' '+s.split(' ',1)[1][:90] if ' ' in s else s
    rota[k['rota']][anahtar].add(f"{k['MOD'][0]}{'a' if k['TEMA']=='aydinlik' else 'k'}{k['w']}"+(f"/{k['sekme'][:12]}" if k['tip']=='sekme' else ''))
for k in kat:
  for s in ksorun(k):
    rota[k['rota']][f"[{k.get('aciciMetin','')[:22]}] {s[:80]}"].add(f"{k['MOD'][0]}{'a' if k['TEMA']=='aydinlik' else 'k'}{k['w']}")
toplam=0
for r in sorted(rota):
  print(f"\n### {r}  ({len(rota[r])} bulgu)")
  for a,v in sorted(rota[r].items(), key=lambda x:-len(x[1])):
    toplam+=1; vs=sorted(v); print(f"  - {a}  ⟶ {len(vs)} durum: {','.join(vs[:8])}{'…' if len(vs)>8 else ''}")
print('\nTOPLAM benzersiz bulgu', toplam, '· temiz rota:', sorted(set(k['rota'] for k in sayfa)-set(rota)))
for h in hata[:20]: print('HATA', h.get('rota'), h.get('w'), h.get('MOD'), h.get('hata','')[:150])
