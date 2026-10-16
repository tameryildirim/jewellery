using System.Diagnostics;
using Bunit;
using Kuyumcu.Contracts.Sayfalar;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// olumsuz-fikstür: dosya — bu dosya aşım ve belirsiz girdi SINIRLARINI sınar (fiş toplamından büyük ödeme, "1.500" tam sayı,
// "0" üstüne rakam); geçersiz değerler bilinçlidir.
// ═════════════════════════════════════════════════════════════════════════════
// PAKET TOPLU-7A — satış ve ürün ekranları görünüm kalanları (GOC-C-2 hasım tur 2 bulguları).
//   ✕ satırdan kopuyordu · "Kalanı ata" 3 satırda aşımı kapatmıyordu + ileti 0 tutarlı satırın altında · "1000000 adet" ·
//   "0" üstüne "99999" → "099999" · tam sayıda "1.500" iletisi "1,5" öneriyordu · /urunler PRO 1280 barkod harf harf kırılıyordu.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Ödeme satırları (FisOdemeBacaklari): ✕ yeri, ileti hedefi, "Kalanı ata" tek tık, gövde değişmezliği.</summary>
public sealed class Toplu7AOdemeTestleri : BunitContext
{
    public Toplu7AOdemeTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private (IRenderedComponent<FisOdemeBacaklari> c, List<FisBacagi> b) Karma(decimal toplam, bool basit)
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
            .Add(x => x.Basit, basit));
        return (c, b);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void T7A01_asim_iletisi_satirda_X_dugmesinden_SONRA_gelir_X_satirdan_kopmaz(bool basit)
    {
        var (c, _) = Karma(10_000m, basit);
        c.Find("#islem-bacak-1-tutar").Input("99999");
        var satir = c.Find("#islem-bacak-1-asim").ParentElement!;
        var cocuklar = satir.Children.ToList();
        var x = cocuklar.FindIndex(e => e.Id == "islem-bacak-sil-1");
        var asim = cocuklar.FindIndex(e => e.Id == "islem-bacak-1-asim");
        Assert.True(x >= 0 && asim >= 0);
        Assert.True(x < asim, "ileti ✕ düğmesinden ÖNCE gelirse tam genişlikli ileti ✕'yi alt satıra iter");
    }

    [Fact]
    public void T7A02_ileti_0_tutarli_satirin_altina_YAZILMAZ_tutari_olan_satira_yazilir()
    {
        var (c, b) = Karma(10_000m, true);
        c.Find("#islem-bacak-ekle").Click();
        c.Find("#islem-bacak-0-tutar").Input("12000");
        c.Find("#islem-bacak-1-tutar").Input("4000");
        c.Find("#islem-bacak-2-tutar").Input("0");                       // SON yazılan satır 0 ₺
        Assert.Equal(0m, b[2].TutarTl);
        Assert.Empty(c.FindAll("#islem-bacak-2-asim"));                  // eskiden iletisi 0 ₺'lik satırın altındaydı
        Assert.Single(c.FindAll("[id$=-asim]"));
        Assert.Equal("islem-bacak-1-asim", c.Find("[id$=-asim]").Id);   // tutarı olan SON satır
    }

    [Fact]
    public void T7A03_uc_satirda_Kalani_ata_TEK_tikla_asimi_kapatir_hicbir_satir_silinmez_govde_toplami_fis_toplami()
    {
        var (c, b) = Karma(10_000m, false);
        c.Find("#islem-bacak-ekle").Click();
        c.Find("#islem-bacak-0-tutar").Input("6000");
        c.Find("#islem-bacak-1-tutar").Input("6000");
        c.Find("#islem-bacak-2-tutar").Input("6000");                    // 18.000 > 10.000
        Assert.Single(c.FindAll("[id$=-asim]"));
        c.Find("#islem-bacak-kalan-ata").Click();
        Assert.Equal(3, b.Count);
        Assert.Equal(new[] { 6_000m, 4_000m, 0m }, b.Select(x => x.TutarTl).ToArray());   // hedef (son yazılan) 0, kalan fazla bir öncekinden
        Assert.Empty(c.FindAll("[id$=-asim]"));
        Assert.Empty(c.FindAll("#islem-bacak-kalan-ata"));
        // Sunucuya giden gövde: yalnız tutarı > 0 satırlar, toplam = fiş toplamı (para davranışı değişmedi).
        var giden = FisFark.Gonderilecek(b).ToList();
        Assert.Equal(2, giden.Count);
        Assert.Equal(10_000m, giden.Sum(x => x.TutarTl));
    }

    [Fact]
    public void T7A04_tek_satir_asimi_ESKI_formulle_ayni_tutar_eksiye_inmez()
    {
        var (c, b) = Karma(10_000m, true);
        c.Find("#islem-bacak-1-tutar").Input("99999");
        c.Find("#islem-bacak-kalan-ata").Click();
        Assert.Equal(10_000m, b[1].TutarTl);                             // 99.999 − 89.999 (GC2E01 ile birebir)
        Assert.Equal(0m, b[0].TutarTl);
    }

    [Fact]
    public void T7A05_eksikte_Kalani_ata_ESKI_davranis_birebir_son_altin_disi_satir_kalani_alir()
    {
        var (c, b) = Karma(10_000m, true);
        c.Find("#islem-bacak-ekle").Click();
        c.Find("#islem-bacak-0-tutar").Input("2000");
        c.Find("#islem-bacak-1-tutar").Input("1000");
        c.Find("#islem-bacak-2-tutar").Input("500");
        Assert.NotEmpty(c.FindAll("#islem-bacak-kalan-ata"));
        c.Find("#islem-bacak-kalan-ata").Click();
        Assert.Equal(7_000m, b[2].TutarTl);                              // 500 + 6.500 kalan (son satır)
        Assert.Equal(10_000m, b.Sum(x => x.TutarTl));
    }
}

/// <summary>Biçim ve ileti: stok miktarı binlikli, tam sayı alanında "1.500" iletisi ondalık önermez.</summary>
public sealed class Toplu7ABicimTestleri : BunitContext
{
    public Toplu7ABicimTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    [Theory]
    [InlineData(1_000_000, "1.000.000 adet")]
    [InlineData(1_000, "1.000 adet")]
    [InlineData(999, "999 adet")]
    [InlineData(10, "10 adet")]
    public void T7A06_ADET_miktari_binlikli_tr_TR(int adet, string beklenen) =>
        Assert.Equal(beklenen, Bicim.Miktar(adet, "ADET"));

    [Fact]
    public void T7A07_kesirli_adet_ve_gram_biçimi_degismedi()
    {
        Assert.Equal("4,25 adet", Bicim.Miktar(4.25m, "ADET"));
        Assert.Equal(Bicim.Gram(1_500.5m), Bicim.Miktar(1_500.5m, "GRAM"));
    }

    [Fact]
    public void T7A08_tam_sayi_alaninda_1_500_ondalik_ONERMEZ_ondalikli_alanda_eski_metin()
    {
        var tr = SayiGirisiSozlugu.Tr;
        Assert.DoesNotContain("1,5", tr.BelirsizTam);
        Assert.Contains("1500", tr.BelirsizTam);
        Assert.Contains("1,5", tr.Belirsiz);                             // ondalıklı alanın metni DEĞİŞMEDİ
        Assert.DoesNotContain("1,5", SayiGirisiSozlugu.En.BelirsizTam);

        var tam = Render<Sayi>(p => p.Add(x => x.ElemanId, "t").Add(x => x.Etiket, "Adet").Add(x => x.TamSayi, true));
        tam.Find("#t").Input("1.500");
        Assert.Equal("true", tam.Find("#t").GetAttribute("aria-invalid"));
        Assert.Contains(tr.BelirsizTam, tam.Markup);
        Assert.DoesNotContain("1,5 ya da", tam.Markup);

        var ond = Render<Sayi>(p => p.Add(x => x.ElemanId, "o").Add(x => x.Etiket, "Gram").Add(x => x.EnCokOndalik, 3));
        ond.Find("#o").Input("1.500");
        Assert.Contains(tr.Belirsiz, ond.Markup);
    }
}

/// <summary>girdi-maske.js (gerçek dosya, node): "0" üstüne rakam → "099999" olmaz.</summary>
public sealed class Toplu7AMaskeTestleri
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

    private const string Sahne = """
        const fs = require('fs');
        const h = {};
        global.window = { addEventListener: (t, f) => { (h[t] = h[t] || []).push(f); } };
        global.document = { getElementById: id => null };
        global.Event = function (t) { this.type = t; };
        eval(fs.readFileSync(process.argv[2], 'utf8'));
        function kutu(deger, tam, bas, son) {
          const a = { 'data-maske': 'sayi' }; if (tam) a['data-tam'] = '1';
          return { tagName: 'INPUT', id: 'k', value: deger, selectionStart: bas, selectionEnd: son, attrs: a,
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
        const r = {};
        r.paraSifirUstu = yaz(kutu('0', false, 1, 1), '99999');
        r.tamSifirUstu = yaz(kutu('0', true, 1, 1), '25');
        r.sifirBasinda = yaz(kutu('0', false, 0, 0), '7');
        r.sifirVirgul = yaz(kutu('0', false, 1, 1), ',5');
        r.tumuSecili = yaz(kutu('0', false, 0, 1), '7');
        r.onSifir = yaz(kutu('10', false, 2, 2), '5');
        r.bosKutu = yaz(kutu('', false, 0, 0), '5');
        process.stdout.write(JSON.stringify(r));
        """;

    [Fact]
    public void T7A09_tutar_0_iken_rakam_yazilinca_0_yerine_gecer_099999_olmaz_baska_yazimlar_degismez()
    {
        using var j = System.Text.Json.JsonDocument.Parse(Node(Sahne));
        var r = j.RootElement;
        Assert.Equal("99999", r.GetProperty("paraSifirUstu").GetString());   // eskiden "099999"
        Assert.Equal("25", r.GetProperty("tamSifirUstu").GetString());
        Assert.Equal("7", r.GetProperty("sifirBasinda").GetString());
        Assert.Equal("0,5", r.GetProperty("sifirVirgul").GetString());       // "0," ondalık yazımı BOZULMAZ
        Assert.Equal("7", r.GetProperty("tumuSecili").GetString());
        Assert.Equal("105", r.GetProperty("onSifir").GetString());           // 10 + 5 etkilenmez
        Assert.Equal("5", r.GetProperty("bosKutu").GetString());
    }
}

/// <summary>CSS sözleşmesi: /urunler PRO barkod tek satır, ileti ✕'den sonra.</summary>
public sealed class Toplu7ACssTestleri
{
    private static string Css => CssKaynak.YorumsuzCss(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "zo-islem.css"));

    [Fact]
    public void T7A10_barkod_hucresi_ICERIK_kutusunda_asgari_genislik_EAN13_tek_satir_uzun_barkod_sarar()
    {
        Assert.Matches(@"td\[data-kolon=""barkod""\] \{[^}]*box-sizing: content-box; width: 16ch; min-width: 12\.5ch; overflow-wrap: anywhere", Css);
        Assert.DoesNotContain("width: 15ch", Css, StringComparison.Ordinal);
    }

    [Fact]
    public void T7A11_asim_iletisi_EN_SON_dizilir_ve_urun_adi_sutunu_yer_birakir()
    {
        Assert.Contains(".bacak-satiri > .bacak-asim { flex-basis: 100%; margin: 0; order: 9; }", Css, StringComparison.Ordinal);
        Assert.Matches(@"td\.hucre-vurgu\[data-kolon=""ad""\]:nth-child\(2\) \{ min-width: calc\(var\(--tablo-yazi\) \* 11\); \}", Css);
        Assert.Matches(@"\[id\^=""u-stok-adet-""\] \{ padding-inline: 6px; \}", Css);
    }
}
