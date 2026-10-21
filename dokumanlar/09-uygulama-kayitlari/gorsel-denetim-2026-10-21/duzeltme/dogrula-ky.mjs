// Görsel denetim düzeltmesi — KRİTİK + YÜKSEK doğrulaması (başsız Chrome + CDP). Kullanım: node dogrula-ky.mjs <sahipVekil> <tezgahtarVekil>
import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
import { writeFileSync, mkdirSync } from 'node:fs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, ''); mkdirSync(S + '/foto', { recursive: true });
const [SAHIP, TEZ] = process.argv.slice(2); const bek = ms => new Promise(r => setTimeout(r, ms));
const sonuc = []; const yaz = (id, gecti, olcum) => { sonuc.push({ id, gecti, olcum }); console.log((gecti ? 'GEÇTİ ' : 'KALDI ') + id + ' · ' + JSON.stringify(olcum)); };
const git = async (b, rota, ms = 3500) => { await b.ev(`Blazor.navigateTo(${JSON.stringify(rota)}); 1`); await bek(ms); };
const ustte = (b, sel) => b.ev(`(()=>{const e=document.querySelector(${JSON.stringify(sel)}); if(!e) return 'yok'; e.scrollIntoView({block:'center'}); const r=e.getBoundingClientRect(); const u=document.elementFromPoint(r.left+r.width/2, r.top+r.height/2); return u===e||e.contains(u)?'acik':('ortulu:'+(u?.id||u?.className||u?.tagName))})()`);
// ── sahip
let b = await baslat(9981, S + '/profil-sahip'); await b.git(SAHIP + '/__giris', 6000);
for (const w of [1280, 1024]) {
  await b.boyut(w, 900); await git(b, '/urunler');
  const r = {}; for (const s of ['#urun-kurallar-ac', '#urun-setler-ac', '#yildiz-urun-ayrinti']) r[s] = await ustte(b, s);
  yaz(`K-1 /urunler ${w}`, Object.values(r).every(v => v === 'acik'), r); await b.foto(`${S}/foto/K1-urunler-${w}.png`);
  await git(b, '/banka'); const r2 = {}; for (const s of ['#yildiz-banka-havale', '#yildiz-banka-ayrinti']) r2[s] = await ustte(b, s);
  yaz(`K-1 /banka ${w}`, Object.values(r2).every(v => v === 'acik'), r2);
}
for (const [w, h] of [[812, 375], [667, 375]]) {
  await b.boyut(w, h); await git(b, '/islemler'); await b.ev('window.scrollTo(0,0); 1'); await bek(500);
  const r = { barkod: await b.ev(`(()=>{const e=document.querySelector('#islem-barkod'); const r=e.getBoundingClientRect(); const u=document.elementFromPoint(r.left+r.width/2, r.top+r.height/2); return u===e||e.contains(u)?'acik':'ortulu'})()`),
              satis: await b.ev(`(()=>{const e=document.querySelector('#yildiz-tur-SATIS'); const r=e.getBoundingClientRect(); const u=document.elementFromPoint(r.left+r.width/2, r.top+r.height/2); return u===e||e.contains(u)?'acik':'ortulu'})()`),
              altPoz: await b.ev(`getComputedStyle(document.querySelector('#yildiz-tezgah-alt')).position`) };
  yaz(`Y-1 /islemler ${w}x${h}`, r.barkod === 'acik' && r.satis === 'acik', r); await b.foto(`${S}/foto/Y1-islemler-${w}x${h}.png`);
}
await b.boyut(375, 812); await git(b, '/islemler'); await b.ev('window.scrollTo(0,0); 1'); await bek(500);
{ const h = await b.ev(`Math.round(document.querySelector('#yildiz-tezgah-alt').getBoundingClientRect().height)`);
  yaz('Y-2 /islemler 375 panel yüksekliği', h / 812 <= 0.30, { h, oran: +(h / 812).toFixed(2) }); await b.foto(`${S}/foto/Y2-islemler-375.png`); }
for (const r of ['/kullanicilar', '/raporlar', '/ayarlar', '/uyum']) {
  await git(b, r); const g = await b.ev(`(()=>{const h=document.querySelector('#ust-bar-yz-baslik h1'); const s=document.querySelector('.ust-bar .ust-sag'); if(!h||!s) return null; return {h1Yuk:Math.round(h.getBoundingClientRect().height), bosluk:Math.round(s.getBoundingClientRect().top - h.getBoundingClientRect().bottom)}})()`);
  yaz(`Y-6 ${r} 375`, g && g.h1Yuk <= 60 && g.bosluk <= 24, g);
}
await b.foto(`${S}/foto/Y6-uyum-375.png`);
await b.boyut(1280, 900);
for (const r of ['/gise', '/e-belgeler', '/kvkk', '/vitrin']) { await git(b, r); const u = await b.ev(`!!document.querySelector('#ust-bar-yz-ust')`); yaz(`Y-5 ${r} Yıldız başlığı`, u, { ust: u }); }
await b.foto(`${S}/foto/Y5-vitrin-1280.png`);
// Y-3/Y-4: koyu tema + baskı
await git(b, '/raporlar/islem-defteri'); await b.ev(`window.kuyumcuTema.uygula('karanlik'); 1`); await bek(600);
const baskiOncesi = await b.ev(`document.documentElement.getAttribute('data-tema')`);
await b.ev(`window.dispatchEvent(new Event('beforeprint')); 1`); await b.cmd('Emulation.setEmulatedMedia', { media: 'print' }); await bek(500);
{ const r = await b.ev(`({tema:document.documentElement.getAttribute('data-tema'), baslik:document.querySelector('#baski-baslik')?.innerText, renk:getComputedStyle(document.querySelector('main')).color, zemin:getComputedStyle(document.body).backgroundColor, tabloGorunum:getComputedStyle(document.querySelector('.izgara-tablo')||document.body).display, dugme:[...document.querySelectorAll('main button')].filter(x=>getComputedStyle(x).display!=='none' && !x.classList.contains('katlanir-baslik')).length})`);
  const pdf = await b.cmd('Page.printToPDF', { printBackground: true, paperWidth: 8.27, paperHeight: 11.69 }); writeFileSync(`${S}/foto/Y3Y4-islem-defteri-koyu.pdf`, Buffer.from(pdf.data, 'base64'));
  yaz('Y-3/Y-4/O-16 islem defteri baskı (koyu tema)', r.tema === 'aydinlik' && !!r.baslik && r.tabloGorunum === 'table' && r.dugme === 0, r); }
await b.cmd('Emulation.setEmulatedMedia', { media: '' }); await b.ev(`window.dispatchEvent(new Event('afterprint')); 1`); await bek(300);
{ const t = await b.ev(`document.documentElement.getAttribute('data-tema')`); yaz('Y-3 baskı sonrası tema geri döndü', t === baskiOncesi, { once: baskiOncesi, sonra: t }); await b.ev(`window.kuyumcuTema.uygula('aydinlik'); 1`); }
// Y-8: sahte offline olayı, sunucu ulaşılırken bant çıkmamalı
await git(b, '/'); await b.ev(`window.dispatchEvent(new Event('offline')); 1`); await bek(5500);
{ const v = await b.ev(`!!document.querySelector('#pwa-cevrimdisi')`); yaz('Y-8 offline olayı + sunucu ayakta → bant yok', !v, { bant: v }); }
b.kapat();
// ── tezgâhtar
b = await baslat(9982, S + '/profil-tez'); await b.git(TEZ + '/__giris', 6000); await b.boyut(1280, 900);
await b.cmd('Network.enable'); const red = [];
const bosalt = setInterval(() => { for (const d of b.olay.splice(0)) if (d.method === 'Network.responseReceived' && d.params.response.status === 403) red.push(d.params.response.url.replace(/^.*\/api\/v1/, '')); }, 50);
red.length = 0; await git(b, '/', 5000); yaz('O-19 tezgâhtar / açılışında 403', red.length === 0, { red: [...new Set(red)] });
for (const r of ['/raporlar/kar-zarar', '/raporlar/vade-takibi', '/raporlar/islem-defteri', '/kullanicilar', '/bildirimler', '/evraklar']) {
  red.length = 0; await git(b, r);
  const d = await b.ev(`({kapi:!!document.querySelector('#yetki-kapisi'), hamKod:/\\((rapor|kullanici|bildirim|evrak)\\)/.test(document.body.innerText), rolBandi:/gerekli rol/i.test(document.body.innerText)})`);
  yaz(`Y-7 tezgâhtar ${r}`, d.kapi && !d.hamKod && !d.rolBandi, { ...d, istek403: red.length });
}
await b.foto(`${S}/foto/Y7-tezgahtar-evraklar.png`);
await git(b, '/faturalar'); { const d = await b.ev(`({bant:!!document.querySelector('#fk-ozel-matrah-hata'), metin:/Özel matrah ayarı okunamadı/.test(document.body.innerText)})`); yaz('Y-7 tezgâhtar /faturalar özel matrah bandı yok', !d.bant && !d.metin, d); }
clearInterval(bosalt); b.kapat();
writeFileSync(`${S}/dogrula-ky.json`, JSON.stringify(sonuc, null, 1));
console.log(`\nÖZET: ${sonuc.filter(x => x.gecti).length}/${sonuc.length} geçti`); process.exit(0);
