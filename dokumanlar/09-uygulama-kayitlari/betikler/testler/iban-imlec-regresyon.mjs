import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
const S=process.argv[2], etiket=process.argv[3];
const bek=ms=>new Promise(r=>setTimeout(r,ms));
const b=await baslat(9721, S+'/profil-'+etiket); await b.boyut(800,300);
await b.git('file://'+S+'/sayfa.html',5000); await bek(500);
for (const [ad,dizi] of [['rakamla','330006100519786457841326'],['TR ile','TR330006100519786457841326']]) {
  await b.ev(`(()=>{const e=document.getElementById('i');e.value='';e.focus();e.setSelectionRange(0,0);return 1})()`);
  const adim=[];
  for(const c of dizi){ await b.cmd('Input.dispatchKeyEvent',{type:'keyDown',text:c,key:c}); await b.cmd('Input.dispatchKeyEvent',{type:'keyUp',key:c}); await bek(20); adim.push(await b.ev(`(()=>{const e=document.getElementById('i');return e.value+'@'+e.selectionStart})()`)); }
  console.log(etiket,ad,':',adim.slice(0,4).join(' , '),'... son:',adim[adim.length-1]);
}
await b.foto(S+'/foto/iban-'+etiket+'.png',false);
b.kapat();
