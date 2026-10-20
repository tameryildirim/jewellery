// Kayıt kaplaması: altta iki satırlı başlık şeridi, sarı eylem etiketi, görünür imleç + tık halkası, bölüm kartı.
// Hepsi pointer-events:none; uygulamanın kendi öğelerine dokunmaz. documentElement'e eklenir (Blazor gövdeyi yeniden çizse de kalır).
(() => {
  if (window.__kap) return;
  const st = document.createElement('style');
  st.textContent = `
  h1:focus, h1:focus-visible{outline:none !important;box-shadow:none !important}   /* gezinince başlığa düşen odak çerçevesi kayıtta görünmesin */
  #kk-alt{position:fixed;left:284px;right:16px;bottom:12px;z-index:2147483000;pointer-events:none;background:rgba(7,11,22,.95);border-left:5px solid #f2c230;border-radius:12px;padding:12px 22px 13px;font-family:-apple-system,"Helvetica Neue",Arial,sans-serif;box-shadow:0 6px 24px rgba(0,0,0,.5)}
  #kk-alt .b{font-weight:700;font-size:22px;color:#fff;line-height:1.25} #kk-alt .s{font-size:16.5px;color:#f6e7a8;margin-top:3px;line-height:1.3}
  #kk-etiket{position:fixed;right:30px;bottom:96px;z-index:2147483001;pointer-events:none;background:#f2c230;color:#111;font:700 14px -apple-system,"Helvetica Neue",Arial,sans-serif;padding:5px 13px;border-radius:999px;display:none}
  #kk-imlec{position:fixed;left:0;top:0;width:20px;height:20px;margin:-10px 0 0 -10px;border-radius:50%;background:rgba(242,194,48,.75);border:2px solid #111;z-index:2147483003;pointer-events:none;transition:transform .38s cubic-bezier(.3,.1,.2,1);transform:translate(720px,450px)}
  #kk-halka{position:fixed;left:0;top:0;width:20px;height:20px;margin:-10px 0 0 -10px;border-radius:50%;border:3px solid #f2c230;z-index:2147483002;pointer-events:none;opacity:0}
  #kk-halka.vur{animation:kkvur .5s ease-out}
  @keyframes kkvur{0%{opacity:.95;scale:1}100%{opacity:0;scale:3.4}}
  #kk-kart{position:fixed;inset:0;z-index:2147483004;pointer-events:none;background:#0b1020;display:none;align-items:center;justify-content:center;flex-direction:column;font-family:-apple-system,"Helvetica Neue",Arial,sans-serif;text-align:center}
  #kk-kart .ust{color:#f2c230;font-size:20px;letter-spacing:.14em;text-transform:uppercase;font-weight:700}
  #kk-kart .b{color:#fff;font-size:58px;font-weight:800;margin:14px 0 10px} #kk-kart .s{color:#aeb9d1;font-size:24px}`;
  document.documentElement.appendChild(st);
  const el = (id, html = '') => { const d = document.createElement('div'); d.id = id; d.innerHTML = html; document.documentElement.appendChild(d); return d; };
  const alt = el('kk-alt', '<div class="b"></div><div class="s"></div>'), etiket = el('kk-etiket'), imlec = el('kk-imlec'), halka = el('kk-halka'), kart = el('kk-kart', '<div class="ust"></div><div class="b"></div><div class="s"></div>');
  alt.style.display = 'none';
  window.__kap = {
    altyazi(b, s) { alt.style.display = b ? 'block' : 'none'; alt.querySelector('.b').textContent = b || ''; alt.querySelector('.s').textContent = s ? 'Çözdüğü sorun: ' + s : ''; },
    etiket(t) { etiket.style.display = t ? 'block' : 'none'; etiket.textContent = t ? '▸ ' + t : ''; },
    imlec(x, y) { imlec.style.transform = `translate(${x}px,${y}px)`; },
    tik(x, y) { halka.style.transform = `translate(${x}px,${y}px)`; halka.classList.remove('vur'); void halka.offsetWidth; halka.classList.add('vur'); },
    kart(ust, b, s) { kart.style.display = b ? 'flex' : 'none'; kart.querySelector('.ust').textContent = ust || ''; kart.querySelector('.b').textContent = b || ''; kart.querySelector('.s').textContent = s || ''; },
  };
})();
