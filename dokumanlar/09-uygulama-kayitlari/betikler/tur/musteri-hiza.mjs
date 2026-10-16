// node musteri-hiza.mjs <BASIT|PROFESYONEL> <aydinlik|karanlik> <cdp> [foto]  — YILDIZ-4D: Müşteriler satır hizası 375/760/1440 (ad satır sayısı, tutar sağ kenarı tek değer, Toptancı öneki). Ortam: HIZA_VEKIL (http://localhost:5161), HIZA_PROFIL, HIZA_CIKTI.
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
import { mkdirSync } from 'node:fs';
const D=process.env.HIZA_CIKTI||'/private/tmp/kuyumcu-serit/hiza'; const V=process.env.HIZA_VEKIL||'http://localhost:5161'; const bek=ms=>new Promise(r=>setTimeout(r,ms));
const [,, MOD, TEMA, CDP, FOTO] = process.argv; mkdirSync(D + '/g', { recursive: true });
const b = await baslat(Number(CDP), process.env.HIZA_PROFIL||(D+'/profil'));
try {
  await b.boyut(375, 812); await b.git(V + '/__giris', 4000);
  for (let i = 0; i < 40; i++) { if (await b.ev(`!!localStorage.getItem('kuyumcu-access')`)) break; await bek(500); }
  const yt = (y, m, g) => b.ev(`fetch('/api/v1${y}',{method:'${m}',headers:{'Content-Type':'application/json',Authorization:'Bearer '+localStorage.getItem('kuyumcu-access')},body:${JSON.stringify(g)}}).then(r=>r.status)`);
  console.log('mod', await yt('/kullanicilar/ben/arayuz-modu', 'PATCH', JSON.stringify({ arayuzModu: MOD })), 'tema', await yt('/kullanici/gorunum', 'PATCH', JSON.stringify({ sema: 'yildiz', temaModu: TEMA })));
  for (const [w, h] of [[375, 812], [760, 900], [1440, 900]]) {
    await b.boyut(w, h); await b.git(V + '/cariler', 6000);
    for (let i = 0; i < 30 && !(await b.ev(`!!document.querySelector('#yildiz-musteri-liste a')`)); i++) await bek(500);
    await bek(600);
    const r = await b.ev(`(()=>{const sat=[...document.querySelectorAll('#yildiz-musteri-liste > a')]; const sat1=s=>{const c=s.querySelector(s0); return c};
      const sa=(c)=>{if(!c) return null; const r=c.getBoundingClientRect(); const rg=document.createRange(); rg.selectNodeContents(c); const n=new Set([...rg.getClientRects()].filter(q=>q.width>0).map(q=>Math.round(q.top))).size; return {l:Math.round(r.left),r:Math.round(r.right),t:Math.round(r.top),n,m:c.textContent.trim()}};
      const s0=null;
      const rows=sat.map(a=>({gtc:getComputedStyle(a).gridTemplateColumns, ad:sa(a.querySelector('.yz-musteri-ad')), alt:sa(a.querySelector('.yz-musteri-alt')), tut:sa(a.querySelector('.yz-musteri-tutar')), dur:sa(a.querySelector('.yz-musteri-durum'))}));
      const u=a=>[...new Set(a)];
      return {sema:document.documentElement.dataset.sema, tema:document.documentElement.getAttribute('data-tema'), satir:rows.length,
        gtc:u(rows.map(x=>x.gtc)), adSatirMax:Math.max(...rows.map(x=>x.ad.n)), tutarSatirMax:Math.max(...rows.map(x=>x.tut.n)),
        tutarSag:u(rows.map(x=>x.tut.r)), tutarSol:u(rows.map(x=>x.tut.l)), altSol:u(rows.map(x=>x.alt.l)), adSol:u(rows.map(x=>x.ad.l)),
        toptanci:rows.filter(x=>/^Toptancı ·/.test(x.alt.m)).map(x=>[x.ad.m,x.ad.n,x.alt.m,x.alt.n,x.tut.m]),
        yatay:document.documentElement.scrollWidth>innerWidth, suzgec:!!document.querySelector('#cari-suzgec-daha'),
        binen:rows.filter(x=>x.tut.t < x.alt.t-12 || (x.tut.l < x.alt.r && x.tut.t===x.alt.t && x.tut.l>x.alt.l)).length}})()`);
    console.log(`${MOD[0]}-${TEMA[0]}-${w} ${JSON.stringify(r)}`);
    if (FOTO && w === 375) await b.foto(`${D}/g/${MOD[0]}-${TEMA[0]}-375-musteriler.png`, false);
  }
} finally { b.kapat(); }
