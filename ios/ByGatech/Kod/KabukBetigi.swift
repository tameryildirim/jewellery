import Foundation

/// Web sayfasına belge başında enjekte edilen betik ve biçem.
/// Apple yüzü: SF Pro yazı tipi, sistem yazı boyutu (WebKit text-size-adjust — Safari'nin metin yakınlaştırması), dokunma titreşimi,
/// rota takibi (giriş başarısı), giriş formu yakalama ve Face ID ile doldurma köprüsü.
enum KabukBetigi {
    static let bicem = #"""
    :root.ios-kabuk, :root.ios-kabuk[data-sema], :root.ios-kabuk[data-sema][data-tema] {
      --yazi-ailesi: -apple-system, "SF Pro Text", system-ui, sans-serif !important;
      --yazi-marka: -apple-system, "SF Pro Display", system-ui, sans-serif !important;
      -webkit-tap-highlight-color: transparent;
      -webkit-font-smoothing: antialiased;
      font-variant-numeric: tabular-nums;
    }
    :root.ios-kabuk body, :root.ios-kabuk .belge, :root.ios-kabuk #emanet-sayfa .emanet-ozet-deger { font-family: var(--yazi-ailesi) !important; }
    :root.ios-kabuk :is(input, button, select, textarea) { font-family: inherit; }
    :root.ios-kabuk button, :root.ios-kabuk a { -webkit-touch-callout: none; }
    /* Yerli kabuk (iOS sekme çubuğu + üst çubuk): web'in kendi üst menüsü gizlenir, işlevleri yerli menüden sürülür. */
    :root.ios-kabuk.ios-yerli #yildiz-ust, :root.ios-kabuk.ios-yerli .yildiz-ust-bosluk { display: none !important; }
    /* Giriş amblemi: yeri korunur, üstüne yerli 3B elmas biner. */
    :root.ios-kabuk.ios-yerli #giris-amblem .amb-parla { visibility: hidden !important; }
    """#

    /// `olcek`: sistem yazı boyutu oranı (1,0 = iPhone varsayılanı). Yalnız metin ölçeklenir, düzen akar.
    static func betik(olcek: Double) -> String {
        #"""
        (function () {
          if (window.__bygatechKabuk) return; window.__bygatechKabuk = true;
          var kok = document.documentElement; kok.classList.add('ios-kabuk'); kok.classList.add('ios-yerli');
          window.__bygatechOlcek = function (o) { kok.style.setProperty('-webkit-text-size-adjust', Math.round(o * 100) + '%', 'important'); };
          window.__bygatechOlcek(\#(olcek));
          var st = document.createElement('style'); st.id = 'ios-kabuk-bicem';
          st.textContent = \#(json(bicem)); kok.appendChild(st);
          function post(tur, veri) { try { window.webkit.messageHandlers.kabuk.postMessage({ tur: tur, veri: veri === undefined ? null : veri }); } catch (e) {} }
          window.__bygatechPost = post;


          // Dokunma titreşimi: düğme/bağlantı dokunuşu hafif, form gönderimi orta.
          document.addEventListener('click', function (e) {
            var t = e.target && e.target.closest && e.target.closest('button,a,[role=button],input[type=submit],input[type=checkbox],input[type=radio],label,summary');
            if (t && !t.disabled) post('titresim', 'hafif');
          }, true);
          document.addEventListener('submit', function () { post('titresim', 'orta'); }, true);

          // Rota takibi (Blazor istemci yönlendirmesi).
          function rota() { post('rota', location.pathname); }
          var ps = history.pushState, rs = history.replaceState;
          history.pushState = function () { var r = ps.apply(this, arguments); rota(); return r; };
          history.replaceState = function () { var r = rs.apply(this, arguments); rota(); return r; };
          window.addEventListener('popstate', rota);

          // Giriş formu var mı? (Face ID düğmesi için)
          var sonForm = null;
          function formYokla() {
            var var_ = !!document.getElementById('giris-form');
            if (var_ !== sonForm) { sonForm = var_; post('girisFormu', var_); }
          }
          new MutationObserver(formYokla).observe(kok, { childList: true, subtree: true });
          document.addEventListener('DOMContentLoaded', function () { formYokla(); rota(); });

          // Giriş denemesi: e-posta + parola kabuğa gider (yalnız bu cihazda, Anahtar Zinciri).
          document.addEventListener('submit', function (e) {
            if (!e.target || e.target.id !== 'giris-form') return;
            var em = document.getElementById('giris-eposta'), pw = document.getElementById('giris-sifre');
            if (em && pw && em.value && pw.value) post('girisDenemesi', { eposta: em.value, parola: pw.value });
          }, true);

          // Yerli kabuk köprüleri: sekme geçişi, gizli web menüsündeki düğmeler, kur şeridi, amblem yeri.
          function tikla(id) { var el = document.getElementById(id); if (!el) return false; el.click(); return true; }
          window.__bygatechTikla = tikla;
          window.__bygatechGit = function (kimlik, yol) {
            if (tikla('yildiz-menu-' + kimlik)) return 'menu';
            history.pushState({}, '', yol); window.dispatchEvent(new PopStateEvent('popstate', { state: {} })); return 'rota';
          };
          window.__bygatechDil = function (kod) {
            var b = [...document.querySelectorAll('#yildiz-menu-dil button, #giris-dil button')].find(function (x) { return x.textContent.trim().toUpperCase() === kod; });
            if (b) { b.click(); return true; } return false;
          };
          window.__bygatechDurum = function () {
            var t = function (s) { var e = document.querySelector(s); return e ? e.textContent.replace(/\s+/g, ' ').trim() : null; };
            var a = document.querySelector('#yildiz-menu .aktif');
            var r = document.querySelector('#giris-amblem .amb-ik'); var rc = r ? r.getBoundingClientRect() : null;
            return JSON.stringify({ kur: t('#yildiz-kur'), kullanici: t('#yildiz-kullanici-ac'), sekme: a ? a.id.replace('yildiz-menu-', '') : null,
              mod: (document.querySelector('#yildiz-mod-profesyonel[aria-pressed="true"]') ? 'profesyonel' : (document.querySelector('#yildiz-mod-basit[aria-pressed="true"]') ? 'basit' : null)),
              dil: kok.lang || null, yol: location.pathname,
              amblem: rc ? [rc.left, rc.top, rc.width, rc.height] : null });
          };

          // Face ID ile doldur: alanlara yaz, Blazor'a haber ver, gönder.
          window.__bygatechGiris = function (eposta, parola) {
            function yaz(id, v) {
              var el = document.getElementById(id); if (!el) return false;
              var set = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set; set.call(el, v);
              el.dispatchEvent(new Event('input', { bubbles: true })); el.dispatchEvent(new Event('change', { bubbles: true })); return true;
            }
            if (!(yaz('giris-eposta', eposta) && yaz('giris-sifre', parola))) return false;
            setTimeout(function () { var b = document.getElementById('giris-gonder'); if (b) b.click(); }, 200);
            setTimeout(function () { if (document.getElementById('giris-form') && document.getElementById('giris-sifre')) post('girisBasarisiz'); }, 10000);
            return true;
          };
        })();
        """#
    }

    private static func json(_ s: String) -> String {
        String(data: try! JSONSerialization.data(withJSONObject: [s]), encoding: .utf8)!.dropFirst().dropLast().description
    }
}
