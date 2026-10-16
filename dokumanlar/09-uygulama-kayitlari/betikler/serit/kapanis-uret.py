#!/usr/bin/env python3
# Kapanış koşumu üreticisi (orkestratör 29.09 00:40).
# NEDEN: şerit betiği her paketi uygulayıcıdan başlatıyor; hasım GEÇTİ vermiş ama
# tasarım hakemi son düzeltmeyi görmemiş paketler için 2-3 saatlik tam koşum israf.
# Kapanış: TASARIM HAKEMİ ile başlar → KALDI ise uygulayıcı düzeltir → hasım doğrular
# (KALDI ise bir düzeltme daha) → tasarım yeniden; en çok 3 tur. Çapraz hakem YOK:
# kapanışta iki bağımsız ölçücü (hasım + tasarım) var; hakem çelişkileri orkestratörce
# karara bağlandı (AKTIF-KOSUM).
# Kullanım: python3 kapanis-uret.py serit-satis3.js kapanis-satis3.js "<önceki koşum özeti>"
import sys, re
kaynak, hedef, ozet = sys.argv[1], sys.argv[2], sys.argv[3]
ilk = sys.argv[4] if len(sys.argv) > 4 else ''  # bilinen tasarım bulguları: varsa 1. tur tasarım atlanır, doğrudan düzeltmeyle başlar
t = open(kaynak).read()
i = t.index('\nfor (const p of paketler) {')
govde = t[:i]
govde = re.sub(r"name: '([^']+)'", lambda m: "name: '" + m.group(1) + "-kapanis'", govde, count=1)
govde = govde.replace("phases: [", "phases: [\n    { title: 'Kapanis', detail: 'Tasarim hakemi ile baslar; hasim GECTI vermis paketi kapatir' },", 1)
ilk_js = ilk.replace('\\', '\\\\').replace('`', "'").replace('${', '$ {')
ozet_js = ozet.replace('\\', '\\\\').replace('`', "'").replace('${', '$ {')
dongu = r'''
const KAPANIS_OZET = `__OZET__` + ' || HIZ / TEST EKONOMISI (orkestrator 29.09 23:00, kurucu hizlandiralim): UYGULAYICI tam suiti KOSMAZ (paket filtresi + ilgili kume yeter); tam suiti yalniz HASIM DOGRULAYICI bir kez kendi KUYUMCU_TEST_DB ile kosar. Olcum: degisen ekranlar 1440/960/375 x BASIT/PRO x acik/koyu.'
const ILK_KRITIK = `__ILK__`
const agirBul = th => th && th.bulgular ? th.bulgular.filter(b => ['KRITIK', 'YUKSEK'].includes(String(b.ciddiyet).toUpperCase()) && !String(b.baslik).startsWith('KAPSAM DIŞI')) : []
// HIZ (orkestrator 29.09 23:00, kurucu 'hizlandiralim'): kapanista yalniz KRITIK tasarim bulgusu engeller; YUKSEK'ler sonucta doner, orkestrator TOPLU-TASARIM-KUYRUGU'na yazar.
const kritikBul = th => agirBul(th).filter(b => String(b.ciddiyet).toUpperCase() === 'KRITIK')
const thGecti = th => !!(th && (th.gorulen_goruntu || 0) >= 4 && kritikBul(th).length === 0)
const listele = arr => arr.map(b => `- [${b.ciddiyet}] ${b.baslik}: ${b.kanit}${b.oneri ? ' → ' + b.oneri : ''}`).join('\n')
for (const p of paketler) {
  let uyg = { paket_no: p.no, durum: 'BITTI', ozet: KAPANIS_OZET }
  let dogrulama = { karar: 'GECTI', bulgular: [], not: 'onceki kosumda hasim GECTI (bkz. KAPANIS_OZET)' }
  let th = null, tur = 0, gecti = false
  while (tur < 2) { // kurucu 29.09 23:10: en cok 2 tur
    tur++
    let kritik
    if (tur === 1 && ILK_KRITIK.trim()) {
      kritik = ILK_KRITIK
      log(`Paket ${p.no} kapanış: bilinen tasarım bulgularıyla doğrudan düzeltme`)
    } else {
      phase('Tasarim')
      th = await agent(tasarimPrompt(p), { label: `tasarim:p${p.no}:k${tur}`, phase: 'Tasarim', model: 'sonnet', effort: 'medium', schema: TASARIM })
      dogrulama.tasarim = th
      if (thGecti(th)) { gecti = true; log(`Paket ${p.no} kapanış tur ${tur}: tasarım GECTI (görüntü ${th.gorulen_goruntu})`); break }
      log(`Paket ${p.no} kapanış tur ${tur}: tasarım KALDI (ağır ${agirBul(th).length}, görüntü ${th ? th.gorulen_goruntu : 0})`)
      if (tur === 2) break
      kritik = th ? listele(agirBul(th).map(b => ({ ...b, baslik: 'TASARIM HAKEMİ — ' + b.baslik }))) : '- [KRITIK] TASARIM HAKEMİ dönmedi'
      if (th && (th.gorulen_goruntu || 0) < 12) kritik += `\n- [KRITIK] TASARIM HAKEMİ yeterli görüntüye bakmadı: gorulen_goruntu=${th.gorulen_goruntu}`
    }
    for (let d = 0; d < 1; d++) {
      phase('Uygulama')
      uyg = await agent(uygulamaPrompt(p, kritik), { label: `duzelt:p${p.no}:k${tur}.${d + 1}`, phase: 'Uygulama', model: 'opus', effort: 'medium', schema: UYGULAMA })
      if (!uyg) break
      phase('Dogrulama')
      dogrulama = await agent(dogrulamaPrompt(p, uyg), { label: `dogrula:p${p.no}:k${tur}.${d + 1}`, phase: 'Dogrulama', model: 'opus', effort: 'medium', schema: DOGRULAMA })
      if (!dogrulama) break
      log(`Paket ${p.no} kapanış doğrulama ${tur}.${d + 1}: ${dogrulama.karar} · bulgu ${dogrulama.bulgular.length}`)
      if (dogrulama.karar === 'GECTI') break
      kritik = listele(dogrulama.bulgular)
    }
    if (!uyg || !dogrulama || dogrulama.karar !== 'GECTI') break
  }
  const beyan = (uyg && uyg.durum ? String(uyg.durum) : '').toUpperCase()
  const uygulayiciBitti = beyan === 'BITTI' || beyan === 'BİTTİ'
  gecti = !!(gecti && dogrulama && dogrulama.karar === 'GECTI' && uygulayiciBitti)
  sonuclar.push({ paket: p.no, ad: p.ad, gecti, tur, uygulama: uyg, dogrulama, kapanis: true })
  if (!gecti) { log(`Paket ${p.no} KAPANIŞTA GEÇMEDİ — orkestratör denetimi gerekli`); break }
}
return { dalga: dalga + ' — KAPANIS', sonuclar, tamamlanan: sonuclar.filter(s => s.gecti).map(s => s.paket), duran: sonuclar.find(s => !s.gecti)?.paket || null }
'''.replace('__OZET__', ozet_js).replace('__ILK__', ilk_js)
open(hedef, 'w').write(govde + dongu)
print('yazildi', hedef)
