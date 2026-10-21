// Görsel denetim düzeltmesi — ORTA + DÜŞÜK doğrulaması (başsız Chrome + CDP). node dogrula-od.mjs <sahipVekil>
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
import { writeFileSync } from 'node:fs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, ''); const bek = ms => new Promise(r => setTimeout(r, ms));
const [V] = process.argv.slice(2); const sonuc = [];
const yaz = (id, gecti, olcum) => { sonuc.push({ id, gecti, olcum }); console.log((gecti ? 'GEÇTİ ' : 'KALDI ') + id + ' · ' + JSON.stringify(olcum)); };
const b = await baslat(9990, S + '/profil-sahip'); await b.cmd('Emulation.setFocusEmulationEnabled', { enabled: true }); await b.git(V + '/__giris', 6000);
const git = async (r, ms = 3500) => { await b.ev(`Blazor.navigateTo(${JSON.stringify(r)}); 1`); await bek(ms); };
const tus = async (k, mod = 0) => { for (const t of ['keyDown', 'keyUp']) await b.cmd('Input.dispatchKeyEvent', { type: t, key: k, code: k, modifiers: mod, windowsVirtualKeyCode: k === 'Tab' ? 9 : 13 }); await bek(120); };
const KONT = `function L(c){const m=c.match(/[\\d.]+/g).map(Number).slice(0,3).map(v=>{v/=255;return v<=.03928?v/12.92:((v+.055)/1.055)**2.4});return .2126*m[0]+.7152*m[1]+.0722*m[2]} function K(a,b){const x=L(a),y=L(b);return (Math.max(x,y)+.05)/(Math.min(x,y)+.05)}`;
// O-1
await b.boyut(320, 700);
for (const r of ['/', '/cariler', '/urunler', '/kasalar', '/gider']) { await git(r); const w = await b.ev('document.documentElement.scrollWidth'); yaz(`O-1 ${r} 320 yatay kaydırma yok`, w <= 320, { scrollWidth: w }); }
// O-2
await b.boyut(1024, 768); await git('/kasalar');
{ const o = await b.ev(`(()=>{const r=document.querySelector('.yz-ozet-rakam'),s=document.querySelector('.yz-ozet-sag');const k=[...r.querySelectorAll('*'),r].map(e=>e.getBoundingClientRect().right);return {rakamSag:Math.round(Math.max(...k)),sagSol:Math.round(s.getBoundingClientRect().left),fs:getComputedStyle(r).fontSize}})()`); yaz('O-2 /kasalar 1024 rakam sağ sütuna binmez', o.rakamSag < o.sagSol, o); await b.foto(`${S}/foto/O2-kasalar-1024.png`); }
await b.boyut(1440, 900); await git('/kasalar'); { const fs = await b.ev(`getComputedStyle(document.querySelector('.yz-ozet-rakam')).fontSize`); yaz('O-2 1440 tuval rakamı 104 px', fs === '104px', { fs }); }
// O-3 / O-4
await b.boyut(1280, 900); await git('/gider');
{ const o = await b.ev(`(()=>{${KONT}; const a=document.getElementById('gd-tutar').closest('.alan'); const bg=getComputedStyle(document.body).backgroundColor; return {cizgi:getComputedStyle(a).borderBottomColor,zemin:bg,oran:+K(getComputedStyle(a).borderBottomColor,bg).toFixed(2)}})()`); yaz('O-3 /gider alan çizgisi ≥3:1', o.oran >= 3, o); }
{ await b.ev(`document.getElementById('gd-tutar').focus(); 1`); await bek(200); const o = await b.ev(`(()=>{const a=document.getElementById('gd-tutar').closest('.alan'); const s=getComputedStyle(a); return {renk:s.borderBottomColor, golge:s.boxShadow, metin:getComputedStyle(document.body).color}})()`); yaz('O-4 /gider odakta alt çizgi koyu', o.renk === o.metin && o.golge !== 'none', o); }
for (const [r, id] of [['/cariler', 'cari-arama'], ['/urunler', 'urun-arama'], ['/evraklar', 'yildiz-evrak-ara']]) {
  await git(r); const o = await b.ev(`(()=>{const i=document.getElementById(${JSON.stringify(id)}); const k=i.closest('.yz-alan'); const once=getComputedStyle(k).borderBottomColor; i.focus(); const s=getComputedStyle(k); return {once, sonra:s.borderBottomColor, golge:s.boxShadow}})()`);
  yaz(`O-4 ${r} arama odağı görünür`, o.once !== o.sonra && o.golge !== 'none', o); }
await b.foto(`${S}/foto/O4-evraklar-odak.png`);
// O-5
await git('/kilavuz'); { let bulundu = null; for (let i = 0; i < 40 && !bulundu; i++) { await tus('Tab'); bulundu = await b.ev(`(()=>{const e=document.activeElement; if(e.tagName!=='VIDEO') return null; const s=getComputedStyle(e); return {outline:s.outlineStyle+' '+s.outlineWidth, fv:e.matches(':focus-visible')}})()`); }
  yaz('O-5 /kilavuz video odak halkası', !!bulundu && bulundu.outline.startsWith('solid'), bulundu); }
// O-9
await git('/cariler'); { const r = await b.ev(`(()=>{const e=document.querySelector('.yz-b1'); const q=e.getBoundingClientRect(); return {x:q.left+q.width/2,y:q.top+q.height/2, op:getComputedStyle(e).opacity}})()`);
  await b.cmd('Input.dispatchMouseEvent', { type: 'mouseMoved', x: r.x, y: r.y }); await bek(200); const h = await b.ev(`getComputedStyle(document.querySelector('.yz-b1')).opacity`);
  await b.cmd('Input.dispatchMouseEvent', { type: 'mousePressed', x: r.x, y: r.y, button: 'left', clickCount: 1 }); await bek(100); const a = await b.ev(`(()=>{const s=getComputedStyle(document.querySelector('.yz-b1')); return s.opacity+'|'+s.transform})()`);
  await b.cmd('Input.dispatchMouseEvent', { type: 'mouseReleased', x: r.x, y: r.y, button: 'left', clickCount: 1 }); await bek(800);
  yaz('O-9 birincil düğme hover ≠ basılı ≠ normal', r.op !== h && h !== a.split('|')[0], { normal: r.op, hover: h, basili: a }); await b.ev(`document.querySelector('.cekmece-kapat, [id$=kapat]')?.click(); 1`); }
// O-12
await git('/urunler/etiketler'); { const w = await b.ev(`Math.round(document.getElementById('yildiz-etiket-adet').getBoundingClientRect().width)`); yaz('O-12 etiket adet girdisi ≥ 30 px', w >= 30, { w }); }
// O-21
await git('/satin-alma'); await b.ev(`[...document.querySelectorAll('button,a')].find(e=>e.innerText.trim().startsWith('+ Yeni sipariş'))?.click(); 1`); await bek(1500);
{ const o = await b.ev(`(async()=>{document.querySelector('.cekmece .secici-tetik').click(); await new Promise(r=>setTimeout(r,1500)); const e=[...document.querySelectorAll('.secici-bos')].find(x=>x.offsetParent); return e?{h:Math.round(e.getBoundingClientRect().height), t:e.innerText}:null})()`); yaz('O-21 seçici ipucu satırı 44 px', o && o.h >= 44, o); }
// D-5 / D-12 belge
await git('/gider-pusulalari'); { const id = await b.ev(`(()=>{const a=[...document.querySelectorAll('[id^=yildiz-pusula-01], [id^=gp-mal-]')][0]; return a? a.id.replace(/^(yildiz-pusula-|gp-mal-)/,''):null})()`);
  if (id) { await git(`/gider-pusulalari/${id}/belge`); const o = await b.ev(`(()=>{const e=[...document.querySelectorAll('.belge h2, .belge-tablo th, .belge-no .etiket')].map(x=>parseFloat(getComputedStyle(x).fontSize)); return {enKucuk:Math.min(...e), adet:e.length}})()`); yaz('D-5 belge önizleme etiketleri ≥12 px', o.enKucuk >= 12, o); await b.foto(`${S}/foto/D5-pusula-belge.png`);
    await b.ev(`(async()=>{ if (window.kuyumcuDil) {} })()`); } else yaz('D-5 pusula bulunamadı', false, {}); }
// D-6
await git('/kilavuz'); { const lh = await b.ev(`(()=>{const e=document.getElementById('kl-aciklama'); const s=getComputedStyle(e); return +(parseFloat(s.lineHeight)/parseFloat(s.fontSize)).toFixed(2)})()`); yaz('D-6 kılavuz açıklaması satır aralığı 1,5', lh >= 1.45, { lh }); }
// D-8 / D-9
await b.boyut(375, 812); await git('/kasalar'); { const o = await b.ev(`(()=>{const c=document.querySelector('.yz-ozet-cumle'), e=document.querySelector('.yz-ozet-eylem'); return Math.round(e.getBoundingClientRect().top - c.getBoundingClientRect().bottom)})()`); yaz('D-8 /kasalar 375 cümle–düğme arası ≥16', o >= 16, { bosluk: o }); }
await git('/urunler'); { const o = await b.ev(`(()=>{const s=document.querySelector('.yz-liste--urun .yz-liste-tablo > .yz-liste-sat:not(.yz-liste-sat--bas)'); const h=[...s.querySelectorAll('[data-etiket]')].map(x=>getComputedStyle(x,'::before').content); return {etiketler:h, gorunum:getComputedStyle(s).display}})()`); yaz('D-9 /urunler 375 değerler etiketli', o.etiketler.length >= 4 && o.etiketler.every(c => c && c !== 'none'), o); await b.foto(`${S}/foto/D9-urunler-375.png`); }
await b.boyut(1280, 900);
// D-10
await git('/veri-ice-aktarim'); { const o = await b.ev(`(()=>{const i=document.getElementById('ia-dosya'); const d=document.getElementById('ia-dosya-dugme'); return {girdiGen:i&&Math.round(i.getBoundingClientRect().width), dugme:d&&d.innerText, yok:document.getElementById('ia-dosya-yok')?.innerText}})()`); yaz('D-10 özel dosya düğmesi', o.girdiGen <= 1 && !!o.dugme, o); }
// D-13
for (const r of ['/kur', '/evraklar', '/gider-pusulalari', '/vitrin']) { await git(r); const c = await b.ev(`(()=>{const c={};document.querySelectorAll('[id]').forEach(e=>c[e.id]=(c[e.id]||0)+1);return Object.keys(c).filter(k=>c[k]>1)})()`); yaz(`D-13 ${r} çift id yok`, c.length === 0, { cift: c }); }
// D-14
await b.boyut(1280, 800); await git('/gider'); { const o = await b.ev(`(async()=>{const t=document.getElementById('gd-personel'); t.scrollIntoView({block:'center'}); t.click(); await new Promise(r=>setTimeout(r,800)); const l=[...document.querySelectorAll('.secici-liste-kap')].find(x=>x.offsetParent); if(!l) return null; const r=l.getBoundingClientRect(); return {ust:Math.round(r.top), alt:Math.round(r.bottom), vh:innerHeight}})()`); yaz('D-14 /gider Personel listesi görünüm alanında', o && o.alt <= o.vh && o.ust >= 0, o); }
// D-3
await git('/raporlar'); { const t = await b.ev(`document.body.innerText`); yaz('D-3 başlık yazımı cümle düzeni', !/Kâr-Zarar|Satış Analizi|Veri Dışa Aktarımı/.test(t), { karZarar: /Kâr-zarar/.test(t) }); }
// D-12 EN
await b.ev(`(async()=>{await fetch('/api/v1/bildirimler/isler/KASA_KAPANIS/calistir',{method:'POST',headers:{'Content-Type':'application/json'}}).catch(()=>0); return 1})()`);
writeFileSync(`${S}/dogrula-od.json`, JSON.stringify(sonuc, null, 1));
console.log(`TOPLAM ${sonuc.filter(x => x.gecti).length}/${sonuc.length}`);
b.kapat();
