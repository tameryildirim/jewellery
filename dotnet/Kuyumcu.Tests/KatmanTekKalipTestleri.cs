using System.Diagnostics;
using System.Text.Json;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET KATMAN-1 — AÇILIR KATMAN TEK KALIP
//
// Kurucu 25.09: "Diğer işlemlere tıklıyorum, sayfanın ortasında seçim çıkıyor;
// ayrıca tipi farklı." Ölçüm (betikler/tur): `.satir-menu` popover'ları UA
// varsayılanıyla ekranın ORTASINDA açılıyordu (SERBEST-YUZEN), öğeleri 13 px.
//
// Kilitler:
//   · index.html katman.css'i (app.css'ten SONRA) ve js/katman.js'i yükler;
//   · bağlı popover kuralı UA ortalamasını ezer (inset:auto; margin:0; top/left
//     BELİRTEÇTEN) — yalnız betik `data-katman="bagli"` yazdıysa;
//   · öğe yazı boyu TEK belirteçtir (--katman-oge-boyu), 14 px altı kademe yok,
//     sabit renk yok;
//   · js/katman.js'in GERÇEK kodu node'da sahte DOM ile koşturulur: açıcının
//     altına yerleşir, yer yoksa üste döner, sağ kenara taşmaz, 375'te ekrana
//     sığar, odak ilk öğeye geçer. (Tarayıcıdaki ölçüm: rapor § tur.)
// ═══════════════════════════════════════════════════════════════════════════

public sealed class KatmanTekKalipTestleri
{
    private static string KatmanCss() => CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "katman.css");
    private static string KatmanJs() => CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "js", "katman.js");

    [Fact]
    public void K1_01_index_katman_css_app_cssten_SONRA_ve_katman_js_yuklenir()
    {
        var html = CssKaynak.YorumsuzKod(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "index.html"));
        var app = html.IndexOf("css/app.css", StringComparison.Ordinal);
        var katman = html.IndexOf("css/katman.css", StringComparison.Ordinal);
        Assert.True(app >= 0 && katman > app, $"katman.css app.css'ten sonra yüklenmeli (app={app}, katman={katman})");
        Assert.Contains("<script src=\"js/katman.js\"></script>", html);
        Assert.DoesNotContain("<script>", html);   // CSP: satır içi betik yok
    }

    [Fact]
    public void K1_02_bagli_popover_UA_ortalamasini_ezer_konum_BELIRTECTEN_gelir()
    {
        var b = CssKaynak.Bildirimler(KatmanCss())
            .Where(x => x.Secici.Contains(".satir-menu[popover][data-katman=\"bagli\"]:popover-open", StringComparison.Ordinal)
                        && string.IsNullOrEmpty(x.AtKural))
            .ToDictionary(x => x.Ozellik, x => x.Deger);

        Assert.Equal("fixed", b["position"]);
        Assert.Equal("auto", b["inset"]);
        Assert.Equal("0", b["margin"]);
        Assert.Equal("var(--katman-ust)", b["top"]);
        Assert.Equal("var(--katman-sol)", b["left"]);
        Assert.StartsWith("var(--katman-azami-boy", b["max-height"], StringComparison.Ordinal);
        Assert.Equal("auto", b["overflow-y"]);
        // KESIF-P7 (TOPLU KATMAN-1b [YÜKSEK]): menü en çok 22rem (uzun açıklama 632 px'e genişletiyordu).
        Assert.Equal("min(22rem, calc(100vw - 2 * var(--katman-pay)))", b["max-width"]);
    }

    [Fact]
    public void K1_03_dar_ekranda_bagli_menu_ekran_genisligine_sigar()
    {
        var dar = CssKaynak.Bildirimler(KatmanCss())
            .Where(x => x.AtKural.Contains("max-width: 480px", StringComparison.Ordinal))
            .ToList();
        Assert.Contains(dar, x => x.Secici.Contains(".satir-menu[popover][data-katman=\"bagli\"]:popover-open", StringComparison.Ordinal)
                                  && x.Ozellik == "width" && x.Deger == "calc(100vw - 2 * var(--katman-pay))");
        // KATMAN-1b: palet BAĞLI değil (perdeli komut paleti) — dar ekranda yine tam genişlik.
        Assert.Contains(dar, x => x.Secici == ".kayit-arama .kayit-arama-palet"
                                  && x.Ozellik == "width" && x.Deger == "calc(100vw - 2 * var(--katman-pay))");
    }

    [Fact]
    public void K1_04_oge_yazi_boyu_TEK_belirtec_ve_govde_kademesine_bagli()
    {
        var bildirimler = CssKaynak.Bildirimler(KatmanCss());
        var kok = bildirimler.Single(x => x.Secici == ":root" && x.Ozellik == "--katman-oge-boyu");
        Assert.StartsWith("var(--yazi-govde", kok.Deger, StringComparison.Ordinal);

        // Satır menüsü öğesi, seçici seçeneği ve arama paleti sonucu AYNI belirteçten.
        foreach (var secici in new[] { ".satir-menu-ogeler > .btn", ".secici-liste-kap .secici-secenek", ".kayit-arama-palet .secici-sonuc" })
        {
            var boy = bildirimler.Where(x => x.Secici.Contains(secici, StringComparison.Ordinal) && x.Ozellik == "font-size").ToList();
            Assert.True(boy.Count > 0, $"{secici} için font-size yok");
            Assert.All(boy, x => Assert.Equal("var(--katman-oge-boyu)", x.Deger));
        }

        // 28.09: seçici listesindeki arama girdisi (.girdi) olcek.css kontrol ölçeğine (0,2,1) yenilmez.
        Assert.Contains(bildirimler, x => x.Secici.Contains("html[data-mod] .secici-liste-kap .secici-ara", StringComparison.Ordinal)
                                          && x.Ozellik == "font-size" && x.Deger == "var(--katman-oge-boyu)");

        // 14 px altına inen kademe (--yazi-xs / --yazi-sm: 16 px ölçekte 12/13 px) ve sabit px boyu YOK;
        // rem değeri ancak düğme tabanı (.875rem = 14 px) ve üstü olabilir.
        Assert.All(bildirimler.Where(x => x.Ozellik == "font-size"), x =>
        {
            Assert.DoesNotContain("--yazi-xs", x.Deger);
            Assert.DoesNotContain("--yazi-sm", x.Deger);
            Assert.DoesNotMatch(@"\dpx", x.Deger);
            var rem = System.Text.RegularExpressions.Regex.Match(x.Deger, @"^(\d*\.?\d+)rem$");
            if (rem.Success)
                Assert.True(decimal.Parse(rem.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) >= 0.875m, x.Deger);
        });
    }

    [Fact]
    public void K1_05_katman_css_sabit_renk_tasimaz_zemin_kenar_golge_seciciyle_ayni()
    {
        var bildirimler = CssKaynak.Bildirimler(KatmanCss());
        Assert.All(bildirimler, x =>
        {
            Assert.DoesNotMatch("#[0-9a-fA-F]{3,8}\\b", x.Deger);
            Assert.DoesNotContain("rgb(", x.Deger);
            Assert.DoesNotContain("hsl(", x.Deger);
        });
        var menu = bildirimler.Where(x => x.Secici == ".satir-menu[popover]").ToDictionary(x => x.Ozellik, x => x.Deger);
        Assert.Equal("var(--yuzey)", menu["background"]);
        Assert.Equal("var(--yaricap)", menu["border-radius"]);
        Assert.Equal("var(--golge-lg)", menu["box-shadow"]);
        Assert.Equal("1px solid var(--kenarlik)", menu["border"]);
    }

    [Fact]
    public void K1_06_katman_js_CSP_uyumlu_tek_yardimci()
    {
        var js = CssKaynak.YorumsuzKod(KatmanJs());
        Assert.DoesNotContain("eval(", js);
        Assert.DoesNotContain("new Function", js);
        Assert.Contains("window.kuyumcuKatman", js);
        Assert.Contains("'beforetoggle'", js);
        Assert.Contains("'toggle'", js);
        Assert.Contains("hidePopover", js);        // açıcı görüş alanından çıkınca kapanır
        Assert.Contains("MutationObserver", js);   // Blazor'un koşullu çizdiği palet
    }

    [Fact]
    public void K1_07_seciciler_kalan_alanla_sinirlanir()
    {
        // GERÇEK js/secici-yerlesim.js, node + sahte DOM: 812 px yüksek ekranda tetik 530..574'te,
        // panel henüz kısa (60 px) — altta 240 px'ten az yer var, üstte çok → YUKARI açılır ve
        // azami boy üstte kalan alandır (liste sonradan büyüse de ekran dışına çıkmaz).
        var yukari = SeciciYerlestir(812, 530, 574, 60);
        Assert.Equal("yukari", yukari.Sinif);
        Assert.Equal("522px", yukari.Azami);     // 530 - 8
        Assert.True(yukari.Oznitelik);
        // Altta bol yer var → aşağı açılır, azami boy alttaki alandır.
        var asagi = SeciciYerlestir(812, 100, 144, 60);
        Assert.Equal("", asagi.Sinif);
        Assert.Equal("660px", asagi.Azami);      // 812 - 144 - 8

        var kural = CssKaynak.Bildirimler(KatmanCss())
            .Single(x => x.Secici.Contains(".secici-liste-kap[data-katman-azami]", StringComparison.Ordinal) && x.Ozellik == "max-height");
        Assert.Equal("min(24rem, var(--secici-azami-boy))", kural.Deger);
    }

    [Fact]
    public void K1_07b_akis_ici_liste_yukari_acilmaz_kendini_gorus_alanina_kaydirir()
    {
        // Görsel denetim D-14 (21.10): Yıldız formunda liste akış içinde (position: relative) — .yukari etkisizdi,
        // /gider "Personel" listesi görüş alanının 52 px altına taşıyordu. Akış içi panelde yukarı sınıfı konmaz,
        // azami boy görüş alanı − tetik − 2×PAY − ARALIK olur ve panel açıldıktan sonra kendini görüş alanına kaydırır.
        var r = SeciciYerlestir(800, 480, 524, 326, "relative");
        Assert.Equal("", r.Sinif);
        Assert.Equal("736px", r.Azami);   // 800 − 44 − 16 − 4
        Assert.True(r.Oznitelik);
        Assert.True(r.Kaydirildi);
        // Mutlak konumlu (eski kabuk) panelde eski karar AYNEN: yukarı açılır; görsel denetim 2 (23.10) sonrası panel de
        // en yakın konuma kaydırılır (Yıldız kabuğunda kaydıran kap .ana-icerik — sayfa dibindeki liste görüş alanına gelsin).
        var m = SeciciYerlestir(812, 530, 574, 60, "absolute");
        Assert.Equal("yukari", m.Sinif);
        Assert.True(m.Kaydirildi);
    }

    // ── js/katman.js'in GERÇEK kodu, node + sahte DOM ─────────────────────────

    private sealed record Sonuc(string? Ust, string? Sol, string? Azami, string? Katman, string? Yon, bool Odak);

    /// <summary>
    /// Gerçek katman.js'i node'da sahte DOM ile yükler; açıcıya tıklatır, "toggle" olayını atar
    /// ve katmana yazılan belirteçleri döndürür. node yoksa test KIRMIZI olur (atlanmaz —
    /// denetim.sh'taki `node --check` kapısı da node ister).
    /// </summary>
    private static Sonuc Yerlestir(double gw, double gh, double aL, double aT, double aR, double aB, double kw, double kh)
    {
        var yol = CssKaynak.Yol("Kuyumcu.Web", "wwwroot", "js", "katman.js");
        var kosum = """
            const fs = require('fs'); const vm = require('vm');
            const [yol, gw, gh, aL, aT, aR, aB, kw, kh] = process.argv.slice(2).map((x, i) => i ? Number(x) : x);
            const L = {}; let odak = false;
            const acici = { isConnected: true, getAttribute: a => a === 'popovertarget' ? 'm1' : null,
              getBoundingClientRect: () => ({ left: aL, top: aT, right: aR, bottom: aB, width: aR - aL, height: aB - aT }) };
            acici.closest = () => acici;
            const props = {}; const attrs = {};
            const katman = { id: 'm1', style: { setProperty: (k, v) => { props[k] = v; }, removeProperty: k => { delete props[k]; } },
              setAttribute: (k, v) => { attrs[k] = v; }, getAttribute: k => attrs[k],
              matches: s => s === '[popover].satir-menu',
              getBoundingClientRect: () => ({ width: kw, height: kh }),
              querySelector: () => ({ focus: () => { odak = true; } }) };
            const document = { readyState: 'complete', body: null, documentElement: { clientWidth: gw, clientHeight: gh },
              addEventListener: (t, f) => { L[t] = f; }, querySelector: () => acici, querySelectorAll: () => [], getElementById: () => null };
            const window = { innerWidth: gw, innerHeight: gh, addEventListener: () => {}, requestAnimationFrame: f => f() };
            vm.runInNewContext(fs.readFileSync(yol, 'utf8'), { window, document });
            L.click({ target: acici });
            L.toggle({ target: katman, newState: 'open' });
            console.log(JSON.stringify({ ust: props['--katman-ust'] ?? null, sol: props['--katman-sol'] ?? null,
              azami: props['--katman-azami-boy'] ?? null, katman: attrs['data-katman'] ?? null, yon: attrs['data-katman-yon'] ?? null, odak }));
            """;
        using var d = JsonDocument.Parse(Node(kosum, yol, gw, gh, aL, aT, aR, aB, kw, kh));
        string? S(string ad) => d.RootElement.GetProperty(ad).ValueKind == JsonValueKind.Null ? null : d.RootElement.GetProperty(ad).GetString();
        return new Sonuc(S("ust"), S("sol"), S("azami"), S("katman"), S("yon"), d.RootElement.GetProperty("odak").GetBoolean());
    }

    [Fact]
    public void K1_08_acicinin_ALTINA_yerlesir_ve_odak_ilk_ogeye_gecer()
    {
        // 1440×900, açıcı sol yarıda (100..140 × 100..130), katman 320×200.
        var s = Yerlestir(1440, 900, 100, 100, 140, 130, 320, 200);
        Assert.Equal("bagli", s.Katman);
        Assert.Equal("alt", s.Yon);
        Assert.Equal("134px", s.Ust);     // açıcı altı + 4 px
        Assert.Equal("100px", s.Sol);     // sol kenarlar hizalı
        Assert.Equal("758px", s.Azami);   // 900 - 130 - 4 - 8
        Assert.True(s.Odak);
    }

    [Fact]
    public void K1_09_altta_yer_yoksa_USTE_doner_ve_azami_boy_kalan_alandir()
    {
        // Açıcı ekranın dibinde (800..830), katman 200 yüksek → üstte açılır.
        var s = Yerlestir(1440, 900, 100, 800, 140, 830, 320, 200);
        Assert.Equal("ust", s.Yon);
        Assert.Equal("596px", s.Ust);     // 800 - 4 - 200
        Assert.Equal("788px", s.Azami);   // 800 - 4 - 8
    }

    [Fact]
    public void K1_10_sag_kenardaki_acicida_sag_kenarlar_hizalanir_ekrana_tasmaz()
    {
        // "⋯" satır sonunda (1400..1432): katmanın sağı açıcının sağına hizalanır.
        var s = Yerlestir(1440, 900, 1400, 300, 1432, 332, 320, 200);
        Assert.Equal("1112px", s.Sol);   // 1432 - 320
        // Açıcı sağ kenara yapışık (1420..1440): sağ kenar 1440 - 8'i geçmez.
        var t = Yerlestir(1440, 900, 1420, 300, 1440, 332, 320, 200);
        Assert.Equal("1112px", t.Sol);   // 1440 - 8 - 320
    }

    [Fact]
    public void K1_11_375_pxte_katman_ekran_genisligine_sigar()
    {
        // Katman 400 px istiyor; 375'te genişlik 375 - 16 = 359 → sol = 8.
        var s = Yerlestir(375, 812, 300, 200, 340, 240, 400, 300);
        Assert.Equal("8px", s.Sol);
        Assert.Equal("alt", s.Yon);
    }

    // ── İç kaydırma (doğrulama 25.09, KRİTİK): 375 px'te "⋯"/"Diğer" menüsünün alt
    //    öğelerine ulaşılamıyordu. Kök neden: kaydırma yakalama evresinde dinleniyordu,
    //    katmanın KENDİ kaydırması da yeniden yerleştirmeyi tetikliyordu ve yerleştirme
    //    doğal boyu azami boyu KALDIRARAK ölçüyordu → kutu büyüyor, tarayıcı scrollTop'u
    //    0'a kısıyordu. Sahte DOM tarayıcının bu kısmasını taklit eder: azami boy
    //    kaldırılınca kutu içeriğe göre büyür ve scrollTop [0, scrollHeight-clientHeight]
    //    aralığına kısılır. ─────────────────────────────────────────────────────

    private sealed record KaydirmaSonuc(int AzamiYazim, int AzamiSilme, double ScrollTop, string? Ust, string? Azami, string? Yon);

    /// <param name="hedef">"katman" | "oge" (katman içindeki düğme) | "sayfa" (document)</param>
    private static KaydirmaSonuc Kaydir(string hedef, double yeniUst, double yeniAlt, double acUst = 300, double acAlt = 340, double eskiAzami = 0)
    {
        var yol = CssKaynak.Yol("Kuyumcu.Web", "wwwroot", "js", "katman.js");
        var kosum = """
            const fs = require('fs'); const vm = require('vm');
            const [yol, hedef, yU, yA, aU, aA, eskiAzami] = process.argv.slice(2).map((x, i) => i < 2 ? x : Number(x));
            const GW = 375, GH = 667, ICERIK = 414, KENAR = 2;          // doğrulamadaki /cariler "⋯": sh=414
            const L = {}; const W = {};
            let ar = { left: 300, top: aU, right: 340, bottom: aA };   // açıcı ("⋯" satır sonunda)
            const acici = { isConnected: true, getAttribute: a => a === 'popovertarget' ? 'm1' : null,
              getBoundingClientRect: () => ({ ...ar, width: ar.right - ar.left, height: ar.bottom - ar.top }) };
            acici.closest = () => acici;
            const props = eskiAzami ? { '--katman-azami-boy': eskiAzami + 'px' } : {}; const attrs = {}; const sayac = { yaz: 0, sil: 0 };
            let st = 0;
            const oh = () => { const z = props['--katman-azami-boy']; const tavan = z ? parseFloat(z) : GH - 16; return Math.min(ICERIK + KENAR, tavan); };
            const kis = () => { st = Math.max(0, Math.min(st, ICERIK - (oh() - KENAR))); };
            const katman = { id: 'm1', nodeType: 1,
              style: { setProperty: (k, v) => { if (k === '--katman-azami-boy') sayac.yaz++; props[k] = v; kis(); },
                       removeProperty: k => { if (k === '--katman-azami-boy') sayac.sil++; delete props[k]; kis(); } },
              setAttribute: (k, v) => { attrs[k] = v; }, getAttribute: k => attrs[k],
              matches: s => s === '[popover].satir-menu',
              closest: s => s.indexOf('[popover].satir-menu') >= 0 ? katman : null,
              get scrollHeight() { return ICERIK; }, get offsetHeight() { return oh(); }, get clientHeight() { return oh() - KENAR; },
              get scrollTop() { return st; }, set scrollTop(v) { st = v; kis(); },
              getBoundingClientRect: () => ({ width: GW - 16, height: oh() }),
              querySelector: () => ({ focus: () => {} }) };
            const oge = { nodeType: 1, closest: s => katman.closest(s) };
            const document = { nodeType: 9, readyState: 'complete', body: null, documentElement: { clientWidth: GW, clientHeight: GH },
              addEventListener: (t, f) => { L[t] = f; }, querySelector: () => acici,
              querySelectorAll: s => s.indexOf(':popover-open') >= 0 ? [katman] : [], getElementById: () => null };
            const window = { innerWidth: GW, innerHeight: GH, addEventListener: (t, f) => { W[t] = f; }, requestAnimationFrame: f => f() };
            vm.runInNewContext(fs.readFileSync(yol, 'utf8'), { window, document });
            L.click({ target: acici });
            L.toggle({ target: katman, newState: 'open' });
            katman.scrollTop = 90;                    // kullanıcı menüyü aşağı kaydırdı
            sayac.yaz = 0; sayac.sil = 0;
            if (hedef === 'sayfa') ar = { left: 300, top: yU, right: 340, bottom: yA };
            if (hedef !== 'yok') {
            const t = hedef === 'katman' ? katman : hedef === 'oge' ? oge : document;
            W.scroll({ target: t });
            }
            console.log(JSON.stringify({ yaz: sayac.yaz, sil: sayac.sil, st, ust: props['--katman-ust'] ?? null,
              azami: props['--katman-azami-boy'] ?? null, yon: attrs['data-katman-yon'] ?? null }));
            """;
        using var d = JsonDocument.Parse(Node(kosum, yol, hedef, yeniUst, yeniAlt, acUst, acAlt, eskiAzami));
        var r = d.RootElement;
        string? S(string ad) => r.GetProperty(ad).ValueKind == JsonValueKind.Null ? null : r.GetProperty(ad).GetString();
        return new KaydirmaSonuc(r.GetProperty("yaz").GetInt32(), r.GetProperty("sil").GetInt32(),
            r.GetProperty("st").GetDouble(), S("ust"), S("azami"), S("yon"));
    }

    [Theory]
    [InlineData("katman")]
    [InlineData("oge")]
    public void K1_12_katman_ici_kaydirma_yeniden_yerlestirmez_scrollTop_korunur(string hedef)
    {
        // 375×667, açıcı 300..340: altta 315 px yer, içerik 416 → azami 315, iç kaydırma.
        var s = Kaydir(hedef, 0, 0);
        Assert.Equal("315px", s.Azami);
        Assert.Equal(0, s.AzamiYazim);   // katmanın kendi kaydırması konuma/azami boya DOKUNMAZ
        Assert.Equal(0, s.AzamiSilme);
        Assert.Equal(90, s.ScrollTop);   // kullanıcının kaydırdığı yer korunur
    }

    [Fact]
    public void K1_13_sayfa_kaydirilinca_yeniden_konumlar_ama_azami_boyu_kaldirmaz_scrollTop_korunur()
    {
        // Sayfa kaydı: açıcı 330..370'e indi → altta 285, üstte 318 px; içerik (416) sığmaz → ÜSTE döner.
        var s = Kaydir("sayfa", 330, 370);
        Assert.True(s.AzamiYazim > 0);   // sayfa kaydırmasında yeniden konumlama ÇALIŞIR
        Assert.Equal(0, s.AzamiSilme);   // doğal boy azami boy kaldırılmadan ölçülür
        Assert.Equal("ust", s.Yon);
        Assert.Equal("318px", s.Azami);  // 330 - 4 - 8
        Assert.Equal("8px", s.Ust);      // DOĞAL boyla: 330 - 4 - min(416, 318)
        Assert.Equal(90, s.ScrollTop);
    }

    [Fact]
    public void K1_14_yon_karari_DOGAL_boyla_verilir_onceki_acilistan_kalan_azami_boy_yaniltmaz()
    {
        // Önceki açılıştan 150 px azami boy kalmış; açıcı 360..400 → altta 255, üstte 348 px.
        // Doğal boy 416 altta sığmaz → ÜSTE döner. Kısılmış kutu boyu (150) ölçülseydi
        // "altta sığıyor" sanılır, menü alta açılıp 255 px'e kısılırdı.
        var s = Kaydir("yok", 0, 0, acUst: 360, acAlt: 400, eskiAzami: 150);
        Assert.Equal("ust", s.Yon);
        Assert.Equal("348px", s.Azami);  // 360 - 4 - 8
        Assert.Equal("8px", s.Ust);      // 360 - 4 - 348
    }

    // ── KATMAN-1 tur 3: KIRPAN ATA (doğrulayıcı KRİTİK bulgusu) ─────────────
    // Ölçüldü: Satış PRO 1024×768, #islem-urun tetiği 445–489, liste yukarı 114–441;
    // atası .islem-pro-kalem-kart (overflow:hidden) 339–647 → listenin yalnız 339 altı
    // görünüyordu. GERÇEK secici-yerlesim.js, node + sahte DOM: ata zinciri
    // panel → .secici-kap (visible) → kart (overflow değişken) → body. `.sabit` sınıfı
    // gelince sahte panel position:fixed gibi belirteçten konumlanır.

    [Fact]
    public void K1_15_kirpan_ata_paneli_keseceginde_panel_SABIT_olur_ve_ekranda_tetige_bagli_tam_gorunur()
    {
        var s = SeciciKirpan(kartTasma: "hidden", kartUst: 339, kartAlt: 647);
        Assert.Contains("sabit", s.Siniflar);
        Assert.Contains("yukari", s.Siniflar);
        Assert.Equal("327px", s.Props["--secici-alt"]);   // 768 − 445 + 4: panel alt kenarı tetiğin 4 px üstünde
        Assert.False(s.Props.ContainsKey("--secici-ust"));
        Assert.Equal("433px", s.Props["--secici-azami-boy"]); // 445 − 4 − 8
        Assert.Equal("40px", s.Props["--secici-sol"]);    // tetiğin sol kenarı
        Assert.Equal("320px", s.Props["--secici-gen"]);
        // Sahte fixed kutu: ekran içinde, kırpan kart DIŞINA taşsa da kesilmez (fixed).
        Assert.True(s.PanelUst >= 8 && s.PanelAlt <= 441, $"panel {s.PanelUst}-{s.PanelAlt}");

        // CSS: `.sabit` panel kırpan atadan position:fixed ile kurtulur; konum belirteçten.
        var sabit = CssKaynak.Bildirimler(KatmanCss())
            .Where(x => x.Secici.Contains(".secici-liste-kap.sabit", StringComparison.Ordinal))
            .ToDictionary(x => x.Ozellik, x => x.Deger);
        Assert.Equal("fixed", sabit["position"]);
        Assert.Equal("var(--secici-alt, auto)", sabit["bottom"]);
        Assert.Equal("var(--secici-sol, var(--katman-pay))", sabit["left"]);
        Assert.Equal("var(--secici-gen, auto)", sabit["width"]);
    }

    [Fact]
    public void K1_16_kirpan_ata_yoksa_ya_da_panel_ataya_sigiyorsa_eski_yerlesim_SABIT_yok()
    {
        var gorunur = SeciciKirpan(kartTasma: "visible", kartUst: 339, kartAlt: 647);
        Assert.DoesNotContain("sabit", gorunur.Siniflar);
        Assert.Contains("yukari", gorunur.Siniflar);
        Assert.False(gorunur.Props.ContainsKey("--secici-alt"));

        // Kart overflow:hidden ama panel (yukarı, 327 px) kartın içine sığıyor → sabit gerekmez.
        var sigan = SeciciKirpan(kartTasma: "hidden", kartUst: 20, kartAlt: 760);
        Assert.DoesNotContain("sabit", sigan.Siniflar);
    }

    [Fact]
    public void K1_17_sabit_panel_sayfa_kayinca_tetikle_gider_kendi_ic_kaydirmasi_yeniden_yerlestirmez()
    {
        var s = SeciciKirpan(kartTasma: "hidden", kartUst: 339, kartAlt: 647, kaydir: 100);
        // Sayfa kaydırması (hedef: document) → tetik 100 px yukarı: 345–389 → alt = 768 − 345 + 4.
        Assert.Equal("427px", s.KaydirmaSonrasiAlt);
        // Panelin kendi iç kaydırması → hiçbir belirteç yazılmaz/silinmez.
        Assert.Equal(0, s.IcKaydirmaYazim);
    }

    private sealed record KirpanSonuc(string[] Siniflar, Dictionary<string, string> Props, double PanelUst, double PanelAlt,
        string? KaydirmaSonrasiAlt, int IcKaydirmaYazim);

    private static KirpanSonuc SeciciKirpan(string kartTasma, double kartUst, double kartAlt, double kaydir = 0)
    {
        var yol = CssKaynak.Yol("Kuyumcu.Web", "wwwroot", "js", "secici-yerlesim.js");
        var kosum = """
            const fs = require('fs'); const vm = require('vm');
            const [yol, tasma, kU, kA, kay] = process.argv.slice(2).map((x, i) => (i === 0 || i === 1) ? x : Number(x));
            const GW = 1024, GH = 768, PH = 327, PW = 320;
            let tU = 445, tA = 489; const TL = 40;
            const sinif = new Set(); const props = {}; const attrs = {}; let yazim = 0;
            const body = { nodeType: 1 };
            const kart = { nodeType: 1, parentElement: body, getBoundingClientRect: () => ({ top: kU, bottom: kA, left: 0, right: GW, width: GW, height: kA - kU }) };
            const kap = { nodeType: 1, parentElement: kart, getBoundingClientRect: () => ({ top: tU, bottom: tA, left: TL, right: TL + 200, width: 200, height: tA - tU }) };
            const ic = { nodeType: 1 };
            const panel = { parentElement: kap,
              contains: n => n === panel || n === ic,
              classList: { add: c => sinif.add(c), remove: c => sinif.delete(c), contains: c => sinif.has(c) },
              style: { setProperty: (k, v) => { props[k] = v; yazim++; }, removeProperty: k => { delete props[k]; yazim++; } },
              setAttribute: (k, v) => { attrs[k] = v; }, removeAttribute: k => { delete attrs[k]; },
              getBoundingClientRect: () => {
                const px = k => parseFloat(props[k]);
                if (sinif.has('sabit')) {
                  const w = px('--secici-gen') || PW, l = px('--secici-sol') || 8;
                  const h = Math.min(PH, px('--secici-azami-boy') || PH);
                  const top = ('--secici-ust' in props) ? px('--secici-ust') : GH - px('--secici-alt') - h;
                  return { top, bottom: top + h, left: l, right: l + w, width: w, height: h };
                }
                const top = sinif.has('yukari') ? tU - 4 - PH : tA + 4;
                return { top, bottom: top + PH, left: TL, right: TL + PW, width: PW, height: PH };
              } };
            const tetik = { getBoundingClientRect: () => ({ left: TL, right: TL + 200, width: 200, top: tU, bottom: tA, height: tA - tU }), scrollIntoView: () => {} };
            const document = { nodeType: 9, body, documentElement: { clientWidth: GW, clientHeight: GH },
              getElementById: id => id === 'p' ? panel : tetik };
            const dinleyiciler = {};
            const window = { innerWidth: GW, innerHeight: GH,
              addEventListener: (ad, f) => { (dinleyiciler[ad] = dinleyiciler[ad] || []).push(f); },
              getComputedStyle: el => el === kart ? { overflow: tasma, overflowX: tasma, overflowY: tasma } : { overflow: 'visible', overflowX: 'visible', overflowY: 'visible' } };
            vm.runInNewContext(fs.readFileSync(yol, 'utf8'), { window, document });
            window.kuyumcuSecici.yerlestir('p', 't');
            const r = panel.getBoundingClientRect();
            const sonuc = { siniflar: [...sinif], props: { ...props }, pu: r.top, pa: r.bottom, kalt: null, ic: -1 };
            if (kay > 0) {
              tU -= kay; tA -= kay;
              (dinleyiciler.scroll || []).forEach(f => f({ target: document }));
              sonuc.kalt = props['--secici-alt'] ?? null;
              yazim = 0;
              (dinleyiciler.scroll || []).forEach(f => f({ target: ic }));
              sonuc.ic = yazim;
            }
            console.log(JSON.stringify(sonuc));
            """;
        using var d = JsonDocument.Parse(Node(kosum, yol, kartTasma, kartUst, kartAlt, kaydir));
        var r = d.RootElement;
        return new KirpanSonuc(
            r.GetProperty("siniflar").EnumerateArray().Select(x => x.GetString()!).ToArray(),
            r.GetProperty("props").EnumerateObject().ToDictionary(x => x.Name, x => x.Value.GetString()!),
            r.GetProperty("pu").GetDouble(), r.GetProperty("pa").GetDouble(),
            r.GetProperty("kalt").ValueKind == JsonValueKind.Null ? null : r.GetProperty("kalt").GetString(),
            r.GetProperty("ic").GetInt32());
    }

    private sealed record SeciciSonuc(string Sinif, string? Azami, bool Oznitelik, bool Kaydirildi);

    private static SeciciSonuc SeciciYerlestir(double gh, double tUst, double tAlt, double panelBoy, string konum = "")
    {
        var yol = CssKaynak.Yol("Kuyumcu.Web", "wwwroot", "js", "secici-yerlesim.js");
        var kosum = """
            const fs = require('fs'); const vm = require('vm');
            const [yol, gh, tU, tA, pb] = process.argv.slice(2, 7).map((x, i) => i ? Number(x) : x);
            const konum = process.argv[7] || ''; let kaydi = false;
            const sinif = new Set(); const props = {}; const attrs = {};
            const panel = { classList: { add: c => sinif.add(c), remove: c => sinif.delete(c), contains: c => sinif.has(c) },
              style: { setProperty: (k, v) => { props[k] = v; }, removeProperty: k => { delete props[k]; } },
              setAttribute: (k, v) => { attrs[k] = v; }, removeAttribute: k => { delete attrs[k]; },
              getBoundingClientRect: () => ({ left: 10, right: 330, width: 320, top: tA + 4, bottom: tA + 4 + pb, height: pb }),
              scrollIntoView: () => { kaydi = true; } };
            const tetik = { getBoundingClientRect: () => ({ left: 10, right: 330, width: 320, top: tU, bottom: tA, height: tA - tU }), scrollIntoView: () => {} };
            const document = { getElementById: id => id === 'p' ? panel : tetik, documentElement: { clientWidth: 1440, clientHeight: gh } };
            const window = { innerWidth: 1440, innerHeight: gh };
            if (konum) { window.getComputedStyle = () => ({ position: konum }); window.requestAnimationFrame = f => f(); }
            vm.runInNewContext(fs.readFileSync(yol, 'utf8'), { window, document });
            window.kuyumcuSecici.yerlestir('p', 't');
            console.log(JSON.stringify({ sinif: [...sinif].filter(c => c === 'yukari').join(''), azami: props['--secici-azami-boy'] ?? null, oz: 'data-katman-azami' in attrs, kaydi }));
            """;
        using var d = JsonDocument.Parse(Node(kosum, yol, gh, tUst, tAlt, panelBoy, konum));
        var r = d.RootElement;
        return new SeciciSonuc(r.GetProperty("sinif").GetString()!,
            r.GetProperty("azami").ValueKind == JsonValueKind.Null ? null : r.GetProperty("azami").GetString(),
            r.GetProperty("oz").GetBoolean(), r.GetProperty("kaydi").GetBoolean());
    }

    // ── KATMAN-1b: GÖRÜNMEZ (0×0) AÇICI ─────────────────────────────────────────
    //    Doğrulama tur 3 (KRİTİK): masaüstünde #ust-kayit-ara display:none → kutusu 0×0
    //    (0,0)'da; eski koşul (a.bottom < 0 || a.top > g.h) onu reddetmiyordu → palet
    //    data-katman="bagli" alıp SOL ÜST KÖŞEYE yapışıyordu. Gerçek katman.js node'da
    //    sahte DOM ile: MutationObserver yoluyla (data-katman-acici) ve popover toggle
    //    yoluyla koşturulur.

    private sealed record GorunmezSonuc(string? Katman, string? Ust, string? Sol, bool Odak, string? OnceKatman, string? IlkKare);

    /// <param name="yol">"oge" (data-katman-acici, MutationObserver) | "popover" (toggle)</param>
    /// <param name="onceGorunur">true: açıcı önce görünür (bağ kurulur), sonra gizlenir ve pencere boyu değişir.</param>
    private static GorunmezSonuc Gorunmez(string yol, bool onceGorunur, double aW = 0, double aH = 0, string sonra = "gizli")
    {
        var js = CssKaynak.Yol("Kuyumcu.Web", "wwwroot", "js", "katman.js");
        var kosum = """
            const fs = require('fs'); const vm = require('vm');
            const [js, yol, onceGorunur, aW, aH, sonra] = process.argv.slice(2);
            let gorunur = onceGorunur === 'True'; let disarida = false;
            const W = Number(aW), H = Number(aH);
            const L = {}; const WL = {}; let odak = false; let gozlem = null; let ilkKare = null;
            const kutu = () => disarida ? { left: 1200, top: 1000, right: 1400, bottom: 1040, width: 200, height: 40 }
                                     : gorunur ? { left: 1200, top: 20, right: 1400, bottom: 60, width: 200, height: 40 }
                                       : { left: 0, top: 0, right: W, bottom: H, width: W, height: H };
            const acici = { id: 'ust-kayit-ara', isConnected: true, getBoundingClientRect: kutu,
              getAttribute: a => a === 'popovertarget' ? 'm1' : null };
            acici.closest = () => acici;
            const props = {}; const attrs = yol === 'oge' ? { 'data-katman-acici': 'ust-kayit-ara' } : {};
            const katman = { id: 'm1', nodeType: 1, style: { setProperty: (k, v) => { props[k] = v; }, removeProperty: k => { delete props[k]; } },
              setAttribute: (k, v) => { attrs[k] = v; }, getAttribute: k => attrs[k] ?? null, removeAttribute: k => { delete attrs[k]; },
              matches: s => yol === 'oge' ? s === '[data-katman-acici]' : s === '[popover].satir-menu',
              closest: () => null, querySelectorAll: () => [],
              getBoundingClientRect: () => ({ left: 0, top: 0, right: 320, bottom: 200, width: 320, height: 200 }),
              scrollHeight: 200, offsetHeight: 200, clientHeight: 200,
              querySelector: () => ({ focus: () => { odak = true; } }) };
            const document = { readyState: 'complete', body: {}, documentElement: { clientWidth: 1440, clientHeight: 900 },
              addEventListener: (t, f) => { L[t] = f; },
              querySelector: () => acici,
              querySelectorAll: s => (yol === 'oge' && s === '[data-katman-acici]') ? [katman] : [],
              getElementById: id => id === 'ust-kayit-ara' ? acici : null };
            const window = { innerWidth: 1440, innerHeight: 900, addEventListener: (t, f) => { WL[t] = f; }, requestAnimationFrame: f => f() };
            class MutationObserver { constructor(f) { gozlem = f; } observe() {} }
            vm.runInNewContext(fs.readFileSync(js, 'utf8'), { window, document, MutationObserver });
            const ac = () => {
              if (yol === 'oge') gozlem([{ addedNodes: [katman] }]);
              else { L.click({ target: acici }); L.beforetoggle({ target: katman, newState: 'open' }); ilkKare = attrs['data-katman'] ?? null; L.toggle({ target: katman, newState: 'open' }); }
            };
            ac();
            const onceKatman = attrs['data-katman'] ?? null;
            if (onceGorunur === 'True') { if (sonra === 'disarida') disarida = true; else gorunur = false; odak = false; if (yol === 'oge') WL.resize(); else ac(); }
            console.log(JSON.stringify({ katman: attrs['data-katman'] ?? null, ust: props['--katman-ust'] ?? null,
              sol: props['--katman-sol'] ?? null, odak, onceKatman, ilkKare }));
            """;
        using var d = JsonDocument.Parse(Node(kosum, js, yol, onceGorunur, aW, aH, sonra));
        var r = d.RootElement;
        string? S(string ad) => r.GetProperty(ad).ValueKind == JsonValueKind.Null ? null : r.GetProperty(ad).GetString();
        return new GorunmezSonuc(S("katman"), S("ust"), S("sol"), r.GetProperty("odak").GetBoolean(), S("onceKatman"), S("ilkKare"));
    }

    [Theory]
    [InlineData("oge")]
    [InlineData("popover")]
    public void K1_18_gorunmez_0x0_aciciya_BAGLANILMAZ_sol_ust_koseye_yapismaz(string yol)
    {
        var s = Gorunmez(yol, onceGorunur: false);
        Assert.Null(s.Katman);   // data-katman="bagli" YAZILMAZ → katman kendi CSS düzeninde (palet: üst orta)
        Assert.Null(s.Ust);      // konum belirteci de yazılmaz (0×0 kutudan "4px / 8px" üretilmez)
        Assert.Null(s.Sol);
        Assert.Null(s.IlkKare);  // popover: beforetoggle (ilk kare) da 0×0 kutuya bağlamaz — "sol üst parlama" yok
    }

    [Fact]
    public void K1_19_acici_sonradan_gizlenirse_bag_COZULUR_belirtecler_silinir()
    {
        // Önce görünür açıcıya bağlanır (koruma, gerçek bağı engellememeli) …
        var s = Gorunmez("oge", onceGorunur: true);
        Assert.Equal("bagli", s.OnceKatman);
        // … pencere genişleyip açıcı display:none olunca (375 → 1440) bağ çözülür.
        Assert.Null(s.Katman);
        Assert.Null(s.Ust);
        Assert.Null(s.Sol);
    }

    [Fact]
    public void K1_22_bagli_oge_acicisi_gorus_alani_DISINA_cikinca_bag_COZULUR_eski_belirtec_kalmaz()
    {
        // Doğrulama 27.09 (DÜŞÜK): açıcı görünür ama görüş alanı dışına çıkınca (top > pencere boyu)
        // yerlestir() bağı çözmeden false dönüyordu → popover olmayan data-katman-acici öğesinde
        // data-katman="bagli" ve eski --katman-ust/-sol kalıyordu (ölü kod yolu).
        var s = Gorunmez("oge", onceGorunur: true, sonra: "disarida");
        Assert.Equal("bagli", s.OnceKatman);
        Assert.Null(s.Katman);
        Assert.Null(s.Ust);
        Assert.Null(s.Sol);
    }

    [Fact]
    public void K1_20_kutusu_olan_ama_eni_ya_da_boyu_0_acici_da_gorunmez_sayilir()
    {
        Assert.Null(Gorunmez("oge", false, aW: 0, aH: 40).Katman);
        Assert.Null(Gorunmez("oge", false, aW: 200, aH: 0).Katman);
    }

    [Fact]
    public void K1_21_kayit_paleti_perdeli_komut_paleti_ust_orta_375te_tam_genislik_cekmece_yazi_TEK()
    {
        var b = CssKaynak.Bildirimler(KatmanCss());
        // Palet artık bağlı kuralı taşımaz (bağlı palet = sol üst köşe regresyonu).
        Assert.DoesNotContain(b, x => x.Secici.Contains(".kayit-arama-palet[data-katman", StringComparison.Ordinal));
        // Masaüstü: app.css'in üst-orta düzenini EZMEZ (top/left/transform yazılmaz), boy ekranı aşmaz.
        var genel = b.Where(x => x.Secici == ".kayit-arama .kayit-arama-palet" && x.AtKural.Length == 0).ToList();
        Assert.DoesNotContain(genel, x => x.Ozellik is "top" or "left" or "transform");
        Assert.Contains(genel, x => x.Ozellik == "max-height" && x.Deger == "calc(100vh - 10vh - var(--katman-pay))");
        Assert.Contains(genel, x => x.Ozellik == "overflow-y" && x.Deger == "auto");
        // 375: tam genişlik, üstte 8 px.
        var dar = b.Where(x => x.Secici == ".kayit-arama .kayit-arama-palet" && x.AtKural.Contains("max-width: 480px", StringComparison.Ordinal)).ToList();
        Assert.Contains(dar, x => x.Ozellik == "top" && x.Deger == "var(--katman-pay)");
        Assert.Contains(dar, x => x.Ozellik == "width" && x.Deger == "calc(100vw - 2 * var(--katman-pay))");
        // app.css'teki palet düzeni (tek kaynak): üst orta, sabit.
        var app = CssKaynak.Bildirimler(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "app.css"))
            .Where(x => x.Secici == ".kayit-arama-palet" && x.AtKural.Length == 0).ToList();
        Assert.Contains(app, x => x.Ozellik == "position" && x.Deger == "fixed");
        Assert.Contains(app, x => x.Ozellik == "left" && x.Deger == "50%");
        Assert.Contains(app, x => x.Ozellik == "transform" && x.Deger == "translateX(-50%)");

        // Çekmece öğeleri (× 28 px, .dugme 14 px, girdi 16 px karışıktı) → TEK belirteç; Vitrin pano
        // çekmecesine özel .875rem kuralı kalktı.
        // 28.09: seçici html[data-mod] ile başlar — olcek.css (0b) kontrol ölçeği (0,2,1, katman.css'ten
        // SONRA yüklenir) .ana-icerik içindeki çekmecede girdileri 16 / "Daha fazla"yı 14 px yapıyordu.
        var cek = b.Single(x => x.Secici.StartsWith("html[data-mod] .cekmece :is(", StringComparison.Ordinal) && x.Ozellik == "font-size");
        Assert.Equal("var(--katman-oge-boyu)", cek.Deger);
        foreach (var oge in new[] { "button", "label", "input", ".dugme" })
            Assert.Contains(oge, cek.Secici, StringComparison.Ordinal);
        Assert.DoesNotContain(b, x => x.Secici.Contains("#cekmece-pano-kalem", StringComparison.Ordinal));
        // 28.09: 18 px'te yarım sütundaki seçici değeri kesiliyordu (#d-ayar 184 > 176 px) → çekmecede kesilmez, sarar.
        var sar = b.Where(x => x.Secici == "html[data-mod] .cekmece .secici-tetik > .secici-metin").ToList();
        Assert.Contains(sar, x => x.Ozellik == "white-space" && x.Deger == "normal");
        Assert.Contains(sar, x => x.Ozellik == "text-overflow" && x.Deger == "clip");
        // × kapat küçülünce görünür çerçeveli kutuya girer (tuval müşteri çekmecesi kalıbı), sabit renk yok.
        var kapat = b.Where(x => x.Secici == ".cekmece .cekmece-bas .cekmece-kapat").ToList();
        Assert.Contains(kapat, x => x.Ozellik == "border" && x.Deger == "1px solid var(--ayrac)");
        Assert.Contains(kapat, x => x.Ozellik == "background" && x.Deger == "var(--yuzey)");
        Assert.DoesNotContain(b, x => x.Ozellik == "font-size" && x.Deger.Contains("rem", StringComparison.Ordinal));
    }

    [Fact]
    public void K1_23_KIYAS5_Kasalar_satir_eylemlerinde_EMOJI_YOK_ince_cizgi_Ikon_var()
    {
        // KIYAS 28.09 bulgu 5: "🌙 Günü Kapat", "♻ Aktife al", "✏️ Düzenle", "🗑 Pasife al".
        // Razor yorumları (@* … *@) sayılmaz; işaretlemede emoji kalmamalı.
        var razor = CssKaynak.Metin("Kuyumcu.Web", "Pages", "Kasalar.razor");
        var yorumsuz = System.Text.RegularExpressions.Regex.Replace(razor, @"@\*.*?\*@", "",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        var emoji = System.Text.RegularExpressions.Regex.Matches(yorumsuz,
            @"[☀-➿]|\uD83C[\uDF00-\uDFFF]|\uD83D[\uDC00-\uDEFF]|\uD83E[\uDD00-\uDEFF]");
        Assert.True(emoji.Count == 0, "Kasalar.razor'da emoji: " + string.Join(" ", emoji.Select(m => m.Value)));
        foreach (var (id, ikon) in new[] {
                     ("kasa-aktife-al-", "Ikonlar.Onay"), ("kasa-gun-kapat-", "Ikonlar.Saat"),
                     ("kasa-duzenle-", "Ikonlar.Kalem"), ("kasa-pasife-al-", "Ikonlar.Carpi") })
        {
            var i = yorumsuz.IndexOf(id, StringComparison.Ordinal);
            Assert.True(i > 0, id + " bulunamadı");
            var son = yorumsuz.IndexOf("</button>", i, StringComparison.Ordinal);
            Assert.Contains("<Ikon Ad=\"@" + ikon + "\" />", yorumsuz[i..son], StringComparison.Ordinal);
        }
    }

    [Fact]
    public void K1_24_375te_Kasalar_kart_eylemleri_2x2_IZGARA_Diger_acicisi_tek_basina_alta_dusmez()
    {
        // 29.09 doğrulama: 375'te "⋯" ikinci satırda tek başına kalıyordu. Dar ekranda kart eylemleri
        // iki eşit sütunlu ızgaradır; kural yalnız 480 px altında ve yalnız #kasa-liste içinde.
        var b = CssKaynak.Bildirimler(KatmanCss());
        var dar = b.Where(x => x.Secici == "#kasa-liste .satir-eylemler"
                               && x.AtKural.Contains("max-width: 480px", StringComparison.Ordinal)).ToList();
        Assert.Contains(dar, x => x.Ozellik == "display" && x.Deger == "grid");
        Assert.Contains(dar, x => x.Ozellik == "grid-template-columns" && x.Deger == "repeat(2, minmax(0, 1fr))");
        Assert.DoesNotContain(b, x => x.Secici.Contains("#kasa-liste", StringComparison.Ordinal) && x.AtKural.Length == 0);
        // Açıcı gerçekten aynı kümede: "⋯" düğmesi #kasa-eylem-… kapsayıcısının içinde.
        var razor = CssKaynak.Metin("Kuyumcu.Web", "Pages", "Kasalar.razor");
        var kume = razor.IndexOf("id=\"@($\"kasa-eylem-{kasa.Id}\")\"", StringComparison.Ordinal);
        Assert.True(kume > 0, "kasa-eylem kapsayıcısı yok");
        Assert.Contains("id=\"kasa-liste\"", razor, StringComparison.Ordinal);
        var kapanis = razor.IndexOf("</div>", kume, StringComparison.Ordinal);
        Assert.Contains("kasa-menu-ac-", razor[kume..kapanis], StringComparison.Ordinal);
    }

    [Fact]
    public void K1_26_Palet_YOK_satiri_KISA_12rem_kutu_degil_oge_yazi_boyunda()
    {
        // Tasarım hakemi 29.09 (KRİTİK): app.css .bos-durum 12rem yükseklikli kutudur; palette gerçek
        // sonucu aşağı itiyordu. Paletteki "yok" satırı kısa ve öğe yazı boyundadır (tek yazı boyu).
        var b = CssKaynak.Bildirimler(KatmanCss());
        var kutu = b.Where(x => x.Secici == ".kayit-arama-palet .kabuk-arama-yok" && x.AtKural.Length == 0).ToList();
        Assert.Contains(kutu, x => x.Ozellik == "height" && x.Deger == "auto");
        var p = b.Where(x => x.Secici == ".kayit-arama-palet .kabuk-arama-yok p").ToList();
        Assert.Contains(p, x => x.Ozellik == "font-size" && x.Deger == "var(--katman-oge-boyu)");
        var razor = CssKaynak.Metin("Kuyumcu.Web", "Bilesenler", "GlobalArama.razor");
        Assert.Contains("class=\"bos-durum kabuk-arama-yok\" id=\"kabuk-arama-yok\"", razor, StringComparison.Ordinal);
        // 30.09 canlı ölçüm: palet bölüm başlığı 13 px, tür rozeti 12 px idi (küçük yazı < 14 px).
        Assert.Contains(b, x => x.Secici == ".kayit-arama-palet .kabuk-arama-baslik" && x.Ozellik == "font-size" && x.Deger == "var(--yazi-md)");
        Assert.Contains(b, x => x.Secici == ".kayit-arama-palet .secici-sonuc .rozet" && x.Ozellik == "font-size" && x.Deger == "var(--katman-oge-boyu)");
    }

    [Fact]
    public void K1_25_Vitrin_pano_ayarlari_tablosunda_kalem_adi_HER_GENISLIKTE_kirilmaz()
    {
        // 29.09 ölçümü: 1440/768/375'te #vitrin-ayarlar ilk sütunu ("Yeni Tam (Teklik)") iki satırdı.
        // Kural medya sorgusuz (her genişlik), yalnız ilk sütun, colspan'lı boş durum satırı hariç.
        var b = CssKaynak.Bildirimler(KatmanCss());
        var k = b.Single(x => x.Secici == ".ana-icerik #vitrin-ayarlar > tbody > tr > td:first-child:not([colspan])");
        Assert.Equal("white-space", k.Ozellik);
        Assert.Equal("nowrap", k.Deger);
        Assert.Equal("", k.AtKural);
        // Nowrap tabloyu 1440'ta 23 px taşırıyordu → başlıktaki "değiştirilebilir" rozeti kendi satırında.
        var rozet = b.Where(x => x.Secici == ".ana-icerik #vitrin-ayarlar > thead > tr > th > .rozet" && x.AtKural.Length == 0).ToList();
        Assert.Contains(rozet, x => x.Ozellik == "display" && x.Deger == "block");
        Assert.Contains(rozet, x => x.Ozellik == "width" && x.Deger == "fit-content");
        // "Kur kaynağı ayarları →" 1024'te "→" ile bölünüyordu.
        Assert.Contains(b, x => x.Secici == ".ana-icerik #vitrin-kur-kaynagi" && x.Ozellik == "white-space"
                                && x.Deger == "nowrap" && x.AtKural.Length == 0);
        // Tablo gerçekten kaydırılabilir kapta: sayfa değil tablo kayar.
        // GOC-C-2: tablo ortak ızgarada (TabloId="vitrin-ayarlar") — kaydırma kabı ızgaranın KartListe'sidir (tablo-sar).
        var razor = CssKaynak.Metin("Kuyumcu.Web", "Pages", "Vitrin.razor");
        var i = razor.IndexOf("TabloId=\"vitrin-ayarlar\"", StringComparison.Ordinal);
        Assert.True(i > 0);
        Assert.Contains("<Izgara ", razor[Math.Max(0, i - 160)..i], StringComparison.Ordinal);
    }

    /// <summary>node'u stdin betiğiyle koşturur; node yoksa ya da betik hata verirse test KIRMIZI olur.</summary>
    private static string Node(string kosum, params object[] argumanlar)
    {
        var psi = new ProcessStartInfo("node")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("-");
        foreach (var a in argumanlar)
            psi.ArgumentList.Add(Convert.ToString(a, System.Globalization.CultureInfo.InvariantCulture)!);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("node başlatılamadı");
        p.StandardInput.Write(kosum);
        p.StandardInput.Close();
        var cikti = p.StandardOutput.ReadToEnd();
        var hata = p.StandardError.ReadToEnd();
        p.WaitForExit();
        Assert.True(p.ExitCode == 0, $"node çıkış {p.ExitCode}: {hata}");
        return cikti;
    }
}
