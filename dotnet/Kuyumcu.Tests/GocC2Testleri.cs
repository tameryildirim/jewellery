using System.Diagnostics;
using Bunit;
using Kuyumcu.Contracts.Sayfalar;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// olumsuz-fikstür: dosya — bu dosya girdi kurallarının SINIRLARINI sınar (fiş toplamından büyük ödeme, kesirli adet,
// tavan üstü açılış stoğu); geçersiz değerler bilinçlidir.
// ═════════════════════════════════════════════════════════════════════════════
// PAKET GOC-C-2 — GOC-C kapanış devri (tasarım hakemi bulguları).
//   VERI-1  karma ödemede ikinci satıra rakam rakam toplamdan büyük tutar yazılınca ilk satır SESSİZCE siliniyordu.
//   VERI-2  açılış stoğunda "1,5"/"1.5" → 15 (tam sayı maskesi ayırıcıyı yutuyordu) · 99.999.999 tavansız.
//   ham tablo · nakit sınırı notu · karma panel ölçeği · /urunler PRO 1280 binme · 375 sayfa boyu.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>VERI-1 — ödeme satırları (FisOdemeBacaklari) bileşen düzeyinde: hiçbir satır kullanıcı sormadan silinmez.</summary>
public sealed class GocC2OdemeTestleri : BunitContext
{
    public GocC2OdemeTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private static GocC2Metinleri M => GocC2Sozlugu.Tr;

    /// <summary>"+ Başka ödeme ekle" sonrası durum: Kart fişin tamamı (otomatik kalan), ikinci satır Nakit 0.</summary>
    private (IRenderedComponent<FisOdemeBacaklari> c, List<FisBacagi> b) Karma(decimal toplam = 10_000m)
    {
        var b = new List<FisBacagi>
        {
            new() { Arac = "KART", TutarTl = toplam, KasaId = "ks1" },
            new() { Arac = "NAKIT", TutarTl = 0m, KasaId = "ks1" },
        };
        var c = Render<FisOdemeBacaklari>(p => p
            .Add(x => x.Bacaklar, b)
            .Add(x => x.Kasalar, [new KasaVeri("ks1", "Ana Kasa", "TRY", true)])
            .Add(x => x.PosCihazlari, [])
            .Add(x => x.FisToplami, toplam)
            .Add(x => x.Basit, true));
        return (c, b);
    }

    [Fact]
    public void GC2E01_ikinci_satira_RAKAM_RAKAM_99999_yazilinca_HICBIR_satir_silinmez_asim_satir_altinda_kalani_ata_duzeltir()
    {
        var (c, b) = Karma();
        foreach (var metin in new[] { "9", "99", "999", "9999", "99999" })
        {
            c.Find("#islem-bacak-1-tutar").Input(metin);
            Assert.Equal(2, b.Count);                                    // eskiden 5. rakamda Kart satırı SİLİNİYORDU
            Assert.Equal(2, c.FindAll("#islem-bacaklar .bacak-satiri").Count);
            Assert.Equal("KART", b[0].Arac);
        }
        Assert.Equal(0m, b[0].TutarTl);                                  // otomatik kalan 0'a iner ama satır KALIR
        Assert.Equal(99_999m, b[1].TutarTl);
        // Aşım yazan satırın ALTINDA, fazla tutarla (sessiz değil); yalnız o satırda.
        Assert.Equal(string.Format(M.OdemeAsimi, Bicim.Tl(89_999m)), c.Find("#islem-bacak-1-asim").TextContent.Trim());
        Assert.Empty(c.FindAll("#islem-bacak-0-asim"));
        Assert.Equal("alert", c.Find("#islem-bacak-1-asim").GetAttribute("role"));
        // Açık eylem: "Kalanı ata" aşan satırı fiş toplamına indirir; Kart satırı 0'da kalır (gönderilmez — k2).
        c.Find("#islem-bacak-kalan-ata").Click();
        Assert.Equal(10_000m, b[1].TutarTl);
        Assert.Equal(0m, b[0].TutarTl);
        Assert.Equal(2, b.Count);
        Assert.Empty(c.FindAll("[id$=-asim]"));
        Assert.Single(FisFark.Gonderilecek(b));                          // 0 ₺'lik satır sunucuya gitmez
    }

    [Fact]
    public void GC2E02_fis_toplamina_ESIT_tutar_yazilinca_ilk_satir_0da_KALIR_asim_iletisi_YOK()
    {
        var (c, b) = Karma();
        c.Find("#islem-bacak-1-tutar").Input("10000");
        Assert.Equal(2, b.Count);
        Assert.Equal(0m, b[0].TutarTl);
        Assert.Empty(c.FindAll("[id$=-asim]"));
        Assert.Empty(c.FindAll("#islem-bacak-kalan-ata"));               // kalan 0 → düğme yok
    }

    [Fact]
    public void GC2E03_asim_DUZELTILINCE_ileti_kalkar_otomatik_kalan_ilk_satira_DONER()
    {
        var (c, b) = Karma();
        c.Find("#islem-bacak-1-tutar").Input("99999");
        Assert.NotEmpty(c.FindAll("#islem-bacak-1-asim"));
        c.Find("#islem-bacak-1-tutar").Input("4000");
        Assert.Equal(2, b.Count);
        Assert.Equal(6_000m, b[0].TutarTl);                              // Kart kalanı yeniden üstlenir
        Assert.Equal(4_000m, b[1].TutarTl);
        Assert.Empty(c.FindAll("[id$=-asim]"));
    }

    [Fact]
    public void GC2E04_uc_satirda_rakam_rakam_asim_iki_satir_da_KALIR_kalani_ata_eksiye_indirmez()
    {
        var (c, b) = Karma();
        c.Find("#islem-bacak-ekle").Click();                             // üçüncü satır (Havale, kalan 0)
        Assert.Equal(3, b.Count);
        foreach (var metin in new[] { "2", "25", "250", "2500", "25000" })
        {
            c.Find("#islem-bacak-2-tutar").Input(metin);
            Assert.Equal(3, b.Count);
        }
        c.Find("#islem-bacak-1-tutar").Input("3000");                    // Nakit 3.000 elle → aşım hâlâ var (28.000 > 10.000)
        Assert.Equal(3, b.Count);
        c.Find("#islem-bacak-kalan-ata").Click();                        // son yazılan satır (Nakit) düzeltilir: 3.000 − 18.000 → 0 (eksi değil)
        Assert.Equal(0m, b[1].TutarTl);
        Assert.Equal(3, b.Count);
        Assert.True(b.All(x => x.TutarTl >= 0m));
    }
}

/// <summary>VERI-2 — tam sayı alanları: ondalık ayırıcı sessizce yutulmaz (10 kat büyütme yok); açılış stoğu tavanı.</summary>
public sealed class GocC2GirdiTestleri : BunitContext
{
    public GocC2GirdiTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    [Fact]
    public void GC2G01_Adet_kesir_tusu_ipucu_CIZILIR_gizli_ve_Core_metniyle_bagli()
    {
        var c = Render<Adet>(p => p.Add(x => x.ElemanId, "t").Add(x => x.Etiket, "Adet"));
        Assert.Equal("1", c.Find("#t").GetAttribute("data-tam"));
        var ip = c.Find("#t-kesir");
        Assert.True(ip.HasAttribute("hidden"));                          // tuş engellenene dek gizli (girdi-maske.js açar)
        Assert.Equal(GocC2Sozlugu.Tr.KesirTusuEngeli, ip.TextContent.Trim());
        Assert.Equal("status", ip.GetAttribute("role"));
    }

    [Fact]
    public void GC2G02_her_TAM_SAYI_alani_kesir_ipucu_tasir_ondalikli_alan_TASIMAZ()
    {
        var g = Render<GunSayisi>(p => p.Add(x => x.ElemanId, "g").Add(x => x.Etiket, "Gün"));
        Assert.Equal(GocC2Sozlugu.Tr.KesirTusuEngeliGenel, g.Find("#g-kesir").TextContent.Trim());
        var s = Render<Sayi>(p => p.Add(x => x.ElemanId, "s").Add(x => x.Etiket, "Sayı").Add(x => x.TamSayi, true));
        Assert.NotNull(s.Find("#s-kesir"));
        var o = Render<Sayi>(p => p.Add(x => x.ElemanId, "o").Add(x => x.Etiket, "Gram").Add(x => x.EnCokOndalik, 3));
        Assert.Empty(o.FindAll("#o-kesir"));
        Assert.Null(o.Find("#o").GetAttribute("data-tam"));
    }

    [Fact]
    public void GC2G03_Adet_1_virgul_5_ve_1_nokta_5_ASLA_15_olmaz_kural_hatasi_ve_deger_bildirilmez()
    {
        foreach (var metin in new[] { "1,5", "1.5" })
        {
            decimal? giden = -1m;
            var c = Render<Adet>(p => p.Add(x => x.ElemanId, "a").Add(x => x.Etiket, "Adet").Add(x => x.GecersizdeBosalt, true)
                .Add(x => x.DegerChanged, EventCallback.Factory.Create<decimal?>(this, v => giden = v)));
            c.Find("#a").Input(metin);
            Assert.NotEqual(15m, giden);
            Assert.Null(giden);
            Assert.Equal("true", c.Find("#a").GetAttribute("aria-invalid"));
        }
    }

    [Fact]
    public void GC2G04_GRAM_miktari_1_virgul_5_ve_1_nokta_5_BIR_BUCUK()
    {
        foreach (var metin in new[] { "1,5", "1.5" })
        {
            decimal? giden = null;
            var c = Render<Sayi>(p => p.Add(x => x.ElemanId, "m").Add(x => x.Etiket, "Miktar").Add(x => x.EnAz, 0m).Add(x => x.EnCokOndalik, 3)
                .Add(x => x.EnCok, AlanSinirlari.AcilisStokEnCok)
                .Add(x => x.DegerChanged, EventCallback.Factory.Create<decimal?>(this, v => giden = v)));
            c.Find("#m").Input(metin);
            Assert.Equal(1.5m, giden);
        }
    }

    [Fact]
    public void GC2G05_acilis_stogu_TAVANI_ekranda_99999999_alan_alti_hata_1000000_gecer()
    {
        decimal? giden = -1m;
        var c = Render<Adet>(p => p.Add(x => x.ElemanId, "u-acilis-stok").Add(x => x.Etiket, "Açılış stoğu")
            .Add(x => x.EnCok, AlanSinirlari.AcilisStokEnCok).Add(x => x.GecersizdeBosalt, true)
            .Add(x => x.DegerChanged, EventCallback.Factory.Create<decimal?>(this, v => giden = v)));
        c.Find("#u-acilis-stok").Input("99999999");
        Assert.Null(giden);
        Assert.Equal("true", c.Find("#u-acilis-stok").GetAttribute("aria-invalid"));
        Assert.Contains("1.000.000", c.Markup, StringComparison.Ordinal);
        c.Find("#u-acilis-stok").Input("1000000");
        Assert.Equal(1_000_000m, giden);
    }

    [Fact]
    public void GC2G06_acilis_tavani_TEK_sabit_sunucu_ve_iki_ekran_alani_ayni()
    {
        Assert.Equal(AlanSinirlari.AcilisStokEnCok, Kuyumcu.Api.Modules.Urun.AcilisAdediAttribute.EnCok);
        var razor = CssKaynak.Metin("Kuyumcu.Web", "Pages", "Urunler.razor");
        var acilis = System.Text.RegularExpressions.Regex.Matches(razor, @"<(Sayi|Adet) ElemanId=""u-acilis-stok""[^>]*>");
        Assert.Equal(2, acilis.Count);                                   // GRAM (Sayi) + ADET (Adet)
        Assert.All(acilis, m => Assert.Contains("EnCok=\"@AlanSinirlari.AcilisStokEnCok\"", m.Value, StringComparison.Ordinal));
    }
}

/// <summary>VERI-2 — girdi-maske.js tuş/yapıştırma davranışı gerçek dosyayla (node'da, sahte DOM ile tuş tuş).</summary>
public sealed class GocC2MaskeTestleri
{
    private static string Node(string betik)
    {
        var maske = CssKaynak.Yol("Kuyumcu.Web", "wwwroot", "js", "girdi-maske.js");
        var psi = new ProcessStartInfo("node") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        if (!File.Exists("/usr/bin/node") && File.Exists("/opt/homebrew/bin/node")) psi.FileName = "/opt/homebrew/bin/node";
        psi.ArgumentList.Add("-");
        psi.ArgumentList.Add(maske);
        using var p = Process.Start(psi)!;
        p.StandardInput.Write(betik);
        p.StandardInput.Close();
        var cikti = p.StandardOutput.ReadToEnd();
        var hata = p.StandardError.ReadToEnd();
        p.WaitForExit();
        Assert.True(p.ExitCode == 0, hata);
        return cikti.Trim();
    }

    /// <summary>Sahte pencere/öğe: beforeinput → (engellenmediyse) yaz → input; yapıştırma paste dinleyicisinden.</summary>
    private const string Sahne = """
        const fs = require('fs');
        const h = {};
        const ipucu = { attrs: { hidden: '' }, removeAttribute(a) { delete this.attrs[a]; }, setAttribute(a, v) { this.attrs[a] = v; } };
        global.window = { addEventListener: (t, f) => { (h[t] = h[t] || []).push(f); } };
        global.document = { getElementById: id => id === 'k-kesir' ? ipucu : null };
        global.Event = function (t) { this.type = t; };
        eval(fs.readFileSync(process.argv[2], 'utf8'));
        function kutu(tam) {
          const a = { 'data-maske': 'sayi' }; if (tam) a['data-tam'] = '1';
          return { tagName: 'INPUT', id: 'k', value: '', selectionStart: 0, selectionEnd: 0, attrs: a,
            hasAttribute(n) { return n in this.attrs; }, getAttribute(n) { return n in this.attrs ? this.attrs[n] : null; },
            setAttribute(n, v) { this.attrs[n] = String(v); }, removeAttribute(n) { delete this.attrs[n]; },
            setSelectionRange(s, e) { this.selectionStart = s; this.selectionEnd = e; },
            dispatchEvent(ev) { (h[ev.type] || []).forEach(f => f({ target: this })); } };
        }
        function yaz(el, metin) {
          for (const ch of metin) {
            let engel = false;
            const ev = { target: el, inputType: 'insertText', data: ch, preventDefault() { engel = true; } };
            (h.beforeinput || []).forEach(f => f(ev));
            if (engel) continue;
            el.value = el.value.slice(0, el.selectionStart) + ch + el.value.slice(el.selectionEnd);
            el.selectionStart = el.selectionEnd = el.selectionStart + 1;
            (h.input || []).forEach(f => f({ target: el }));
          }
          return el.value;
        }
        function yapistir(el, metin) {
          const ev = { target: el, clipboardData: { getData: () => metin }, preventDefault() {} };
          (h.paste || []).forEach(f => f(ev));
          return el.value;
        }
        const r = {};
        let k = kutu(true); r.tamVirgul = yaz(k, '1,5'); r.ipucuAcik = !('hidden' in ipucu.attrs);
        k = kutu(true); r.tamNokta = yaz(k, '1.5');
        k = kutu(true); r.tamKirkBesBin = yaz(k, '45.000'); r.binlikIpucuKapali = ('hidden' in ipucu.attrs) && !k.hasAttribute('data-kesir-engel');
        k = kutu(true); r.tamBin = yaz(k, '1.000');
        k = kutu(true); r.tamBinVirgulBes = yaz(k, '1.000,5');
        k = kutu(true); r.tamYapistir = yapistir(k, '1,5');
        k = kutu(true); r.tamOnIki = yaz(k, '12');
        k = kutu(true); r.tamHarf = yaz(k, '1a2');
        k = kutu(false); r.ondalikVirgul = yaz(k, '1,5');
        k = kutu(false); r.ondalikNokta = yaz(k, '1.5');
        process.stdout.write(JSON.stringify(r));
        """;

    [Fact]
    public void GC2M01_tam_sayi_kutusunda_1_virgul_5_ASLA_15_olmaz_ipucu_acilir_nokta_BINLIK_kalir_45_000_45e_dusmez_ondalikli_kutu_1_5_yazar()
    {
        using var j = System.Text.Json.JsonDocument.Parse(Node(Sahne));
        var r = j.RootElement;
        Assert.Equal("1", r.GetProperty("tamVirgul").GetString());       // ayırıcı ve ardından gelen kesir hanesi YAZILMAZ (eskiden "15")
        Assert.True(r.GetProperty("ipucuAcik").GetBoolean());            // engel sessiz değil: kutunun altındaki ipucu açılır
        // tur 2 (hasım KRİTİK): "." tr-TR BİNLİK ayırıcıdır — tuşta engellenmez, kesir kipine sokmaz; metin C# SayiCoz'a
        // kalır ("1.5" → küsurat hatası + kilit, DG11; "45.000" → 45000, DG10). Eskiden "45.000" → "45" (1000 kat küçülme).
        Assert.Equal("1.5", r.GetProperty("tamNokta").GetString());
        Assert.Equal("45.000", r.GetProperty("tamKirkBesBin").GetString());
        Assert.True(r.GetProperty("binlikIpucuKapali").GetBoolean());   // binlik yazana "küsuratlı yazılamaz" DENMEZ
        Assert.Equal("1.000", r.GetProperty("tamBin").GetString());
        Assert.Equal("1.000", r.GetProperty("tamBinVirgulBes").GetString());   // binlikten SONRA gelen virgül + kesir yine yazılmaz
        Assert.Equal("1,5", r.GetProperty("tamYapistir").GetString());   // yapıştırma ayırıcıyı YUTMAZ → C# "Küsuratlı yazılamaz" der
        Assert.Equal("12", r.GetProperty("tamOnIki").GetString());
        Assert.Equal("12", r.GetProperty("tamHarf").GetString());
        Assert.Equal("1,5", r.GetProperty("ondalikVirgul").GetString());
        Assert.Equal("1.5", r.GetProperty("ondalikNokta").GetString());
    }
}

/// <summary>Ekran: ham tablo → ortak ızgara, nakit notu, karma panel ölçeği, /urunler PRO binme, son satışlar kırılımı.</summary>
public sealed class GocC2EkranTestleri : BunitContext
{
    public GocC2EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        GocC2Ortak.IzgaraServisleri(Services);
    }

    [Theory]
    [InlineData("Pages/Vitrin.razor", "vitrin-ayarlar")]
    [InlineData("Bilesenler/Fis/FisKalemTablosu.razor", "islem-kalem-tablo")]
    [InlineData("Bilesenler/Fis/FisKalemTablosu.razor", "islem-kalem-pro")]
    [InlineData("Bilesenler/Fis/FisVerdimAldim.razor", "islem-aldim-tablo")]
    [InlineData("Bilesenler/Fis/FisSonSatislar.razor", "islem-son-satislar")]
    public void GC2T01_bes_ham_tablo_ORTAK_IZGARADA_kimlikler_korunur(string dosya, string kimlik)
    {
        var m = CssKaynak.Metin(["Kuyumcu.Web", .. dosya.Split('/')]);
        var kod = System.Text.RegularExpressions.Regex.Replace(m, @"@\*.*?\*@", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.DoesNotMatch(@"<table\b", kod);
        Assert.Contains("<Izgara ", kod, StringComparison.Ordinal);
        if (kimlik != "islem-son-satislar") Assert.Contains($"TabloId=\"{kimlik}\"", kod, StringComparison.Ordinal);
        Assert.Contains("GRID1_TAVAN=8", CssKaynak.Metin("denetim.sh"), StringComparison.Ordinal);
    }

    [Fact]
    public void GC2T02_nakit_siniri_notu_TEK_kisa_satir_TAM_cumle_ipucunda_sozlesme_aynen()
    {
        var c = Render<FisNakitUyari>(p => p.Add(x => x.Had, 30_000m).Add(x => x.Toplam, 25_000m));
        Assert.Equal(string.Format(GocC2Sozlugu.Tr.NakitSiniriKisa, Bicim.Tl(30_000m)), c.Find("#islem-nakit-uyari-kisa").TextContent.Trim());
        Assert.False(c.Find("#islem-nakit-uyari-kutu").HasAttribute("hidden"));
        var tam = c.Find("#islem-nakit-uyari");
        Assert.Equal(string.Format(SatisTuvalSozlugu.Tr.NakitUyari, Bicim.Tl(30_000m)), tam.TextContent.Trim());
        Assert.NotNull(tam.Closest("details#islem-nakit-uyari-ipucu"));  // tam cümle ⓘ içinde (kapalı)
        Assert.Equal("1", tam.GetAttribute("data-yakin"));
        // Tuvalin iki sütunlu yerleşiminde not SAĞ sütunda kalır (dış kutu kimliğiyle; ölçümde sol sütuna düştüğü görüldü).
        var css = CssKaynak.YorumsuzCss(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "zo-islem.css"));
        Assert.Contains(".islem-sayfa.tuval-tezgah > #islem-nakit-uyari-kutu { grid-column: 2; grid-row: 3; }", css, StringComparison.Ordinal);
        var gizli = Render<FisNakitUyari>(p => p.Add(x => x.Had, 30_000m).Add(x => x.Toplam, 1_000m));
        Assert.True(gizli.Find("#islem-nakit-uyari-kutu").HasAttribute("hidden"));
        Assert.True(gizli.Find("#islem-nakit-uyari").HasAttribute("hidden"));
    }

    [Fact]
    public void GC2T03_urunler_PRO_barkod_KARAKTERDE_sarar_durum_suzgeci_asgari_genislik_375_kart_iki_sutun()
    {
        var css = CssKaynak.YorumsuzCss(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "zo-islem.css"));
        // TOPLU-7A: eski "width: 15ch" (border-box) EAN'ı ikiye bölüyordu → içerik kutusu + asgari genişlik (ayrıntı: Toplu7ATestleri).
        Assert.Matches(@"#urun-liste-sar > \.veri-tablosu > tbody > tr > td\[data-kolon=""barkod""\] \{[^}]*box-sizing: content-box; width: 16ch; min-width: 12\.5ch; overflow-wrap: anywhere", css);
        Assert.Matches(@"html\[data-mod=""profesyonel""\] #urun-durum-filtre-sar \{ min-width: 6rem; \}", css);
        Assert.Matches(@"#urun-liste-sar > \.izgara-tablo > tbody > tr \{ grid-template-columns: repeat\(2, minmax\(0, 1fr\)\)", css);
    }

    [Fact]
    public void GC2T04_karma_panel_SUTUN_genisligine_gore_satir_duzeninde_asim_iletisi_tam_satir()
    {
        var css = CssKaynak.YorumsuzCss(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "zo-islem.css"));
        Assert.Contains("#islem-aldim > .fis-tuval-icerik { container: gc2-aldim / inline-size; }", css, StringComparison.Ordinal);
        Assert.Contains("@container gc2-aldim (min-width: 380px)", css, StringComparison.Ordinal);
        Assert.Contains(".bacak-satiri > .bacak-asim { flex-basis: 100%;", css, StringComparison.Ordinal);
    }

    [Fact]
    public void GC2T05_son_satislar_kirilim_IZGARANIN_ALTINDA_panel_dugmeye_bagli()
    {
        var satislar = new List<IadeAdayi> { new("h1", DateTime.UtcNow, 10_000m, "u1", "Burma bilezik", 1m, 0m, 1m, null) };
        var kirilim = new OdemeKirilimi();
        kirilim.Ac("h1");
        var c = Render<FisSonSatislar>(p => p.Add(x => x.Satislar, satislar).Add(x => x.Yuklendi, true).Add(x => x.Kirilim, kirilim)
            .Add(x => x.Basit, false));
        c.Find("#islem-son-satislar-ac").Click();
        var dugme = c.WaitForElement("#islem-kirilim-h1");
        Assert.Equal("islem-kirilim-govde-h1", dugme.GetAttribute("aria-controls"));
        var panel = c.Find("#islem-kirilim-govde-h1");
        Assert.Contains("islem-kirilim-satir", panel.ClassName ?? "", StringComparison.Ordinal);
        Assert.Null(panel.Closest("table"));                             // satır altı colspan DEĞİL
        Assert.NotNull(c.Find("#islem-defter-liste-sar table.islem-son-tablo"));
    }
}
