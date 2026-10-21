// O-10 uzun içerik ölçümü: ad/tutar öğelerine uzun değer yazar, binme (K9) ve kutudan taşma (K3) sayar.
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, ''); const bek = ms => new Promise(r => setTimeout(r, ms));
const [V, ...R] = process.argv.slice(2);
const b = await baslat(9985, S + '/profil-sahip'); await b.git(V + '/__giris', 6000);
const OLC = `(()=>{
 const AD='Abdurrahmangazi Kuyumculuk Kıymetli Madenler Ticaret Limited Şirketi', TUT='9.999.999.999,99';
 const adSel='.yildiz-bugun-sat-metin, .yz-musteri-ad, .yz-liste-ad, td:nth-child(3)';
 const tutSel='.yildiz-bugun-tam, .yz-musteri-tutar, .yz-liste-d';
 let a=0,t=0; document.querySelectorAll('main '+adSel).forEach((e,i)=>{ if(i<6&&e.children.length===0){e.textContent=AD;a++} });
 document.querySelectorAll('main '+tutSel).forEach((e,i)=>{ if(i<6&&e.children.length===0){e.textContent=(e.textContent.includes('₺')?TUT+' ₺':TUT);t++} });
 return new Promise(res=>requestAnimationFrame(()=>requestAnimationFrame(()=>{
  const yap=[...document.querySelectorAll('main *')].filter(e=>{const r=e.getBoundingClientRect(); return e.children.length===0 && r.width>0 && e.textContent.trim() && getComputedStyle(e).visibility!=='hidden'});
  const binen=[]; for(let i=0;i<yap.length;i++) for(let j=i+1;j<yap.length;j++){const p=yap[i].getBoundingClientRect(),q=yap[j].getBoundingClientRect(); const w=Math.min(p.right,q.right)-Math.max(p.left,q.left), h=Math.min(p.bottom,q.bottom)-Math.max(p.top,q.top); if(w>2&&h>2) binen.push(yap[i].textContent.trim().slice(0,25)+' ⟂ '+yap[j].textContent.trim().slice(0,25))}
  const tasan=yap.filter(e=>{const r=e.getBoundingClientRect(), p=e.parentElement.getBoundingClientRect(); return r.right>p.right+1 && getComputedStyle(e.parentElement).overflow==='visible'}).map(e=>e.className+':'+e.textContent.trim().slice(0,20));
  const kesik=[...document.querySelectorAll('main .hucre-tek-satir')].filter(e=>e.scrollWidth>e.clientWidth+1).length;
  res({degisen:{a,t}, binen:binen.slice(0,6), binenSayi:binen.length, tasan:tasan.slice(0,6), tasanSayi:tasan.length, kesik});
 })));
})()`;
for (const r of R) { const [rota, w] = r.split('@'); await b.boyut(+w, 900); await b.ev(`Blazor.navigateTo(${JSON.stringify(rota)}); 1`); await bek(4000);
  const sonuc = await b.ev(OLC, true); console.log(rota, w, JSON.stringify(sonuc)); await b.foto(`${S}/foto/O10-${rota.replace(/\W/g,'') || 'kok'}-${w}.png`); }
b.kapat();
