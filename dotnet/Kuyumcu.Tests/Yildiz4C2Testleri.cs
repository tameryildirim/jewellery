using System.Text.RegularExpressions;
using Bunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YILDIZ-4C-2 — 4C'nin gerçek (başsız) Chrome ölçümünde çıkan farkların kilidi.
// Ölçüm (10.10, 1440 × 900, BASİT koyu, karma hâli):
//   • araç seçicisi tetiği %100 genişlikte kalıyordu → "Kart · An…" (tuval "Nakit · Ana Kasa"
//     eki kesilmez);
//   • açılan araç listesi 100 px (sikisma K6 "dar seçici" 2/2);
//   • gizli "Tutar" etiketinin iç metni 1 px kutudan taşıyordu (sikisma K3 2/0).
// Görsel eşitliğin asıl kanıtı Chrome ölçümüdür; bu sınıf CSS sözleşmesini kilitler.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class Yildiz4C2Testleri
{
    private const string Kok = "html:root[data-sema=\"yildiz\"] .yz .yz-tezgah-karma .bacak-satiri";

    private static string Css() =>
        Regex.Replace(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css"), @"/\*.*?\*/", "", RegexOptions.Singleline);

    private static string Kural(string css, string secici)
    {
        var i = css.IndexOf(secici + " {", StringComparison.Ordinal);
        Assert.True(i >= 0, "kural yok: " + secici);
        var a = css.IndexOf('{', i);
        return css[(a + 1)..css.IndexOf('}', a)];
    }

    [Fact]
    public void Y4C2_01_karma_arac_secicisi_icerik_kadar_kasa_eki_kesilmez()
    {
        var css = Css();
        var secici = $"{Kok} > .yz-bacak-ad > .secici-alan,\n{Kok} > .yz-bacak-ad > .secici-alan :is(.secici-kap, .secici-tetik)";
        var kural = Kural(css, secici);
        Assert.Contains("width: max-content;", kural);
        Assert.Contains("max-width: 100%;", kural);
    }

    [Fact]
    public void Y4C2_02_karma_arac_listesi_dar_degil()
    {
        var kural = Kural(Css(), $"{Kok} > .yz-bacak-ad .secici-liste-kap");
        Assert.Contains("min-width: 320px;", kural);
        Assert.Contains("max-width: none;", kural);
    }

    [Fact]
    public void Y4C2_03_gizli_etiketin_ic_metni_kirpilir()
    {
        var kural = Kural(Css(), $"{Kok} :is(.alan, .secici-alan) > :is(label, .alan-etiket) *");
        Assert.Contains("overflow: hidden;", kural);
    }

    [Fact]
    public void Y4C2_05_karma_tutar_ve_tl_isareti_tek_satirda_375()
    {
        var css = Css();
        Assert.Contains("flex-wrap: nowrap;", Kural(css, $"{Kok} > .alan[id$=\"-tutar-sar\"] .alan-girdi-satir"));
        var girdi = Kural(css, $"{Kok} > .alan[id$=\"-tutar-sar\"] .alan-girdi-satir > .girdi");
        Assert.Contains("flex: 1 1 0;", girdi);
        Assert.Contains("min-width: 0;", girdi);
    }

    [Fact]
    public void Y4C2_06_karma_satiri_tuval_sat_geometrisinde_tutar_sag_ucta()
    {
        var css = Css();
        var satir = Kural(css, Kok);
        Assert.Contains("grid-template-columns: minmax(0, 1fr) auto 150px;", satir);
        Assert.Contains("padding: 22px 0;", satir);             // tuval .sat{padding:22px 0}
        Assert.Contains("column-gap: 24px;", satir);            // tuval .sat{gap:24px}
        Assert.Contains("align-items: center;", satir);
        Assert.Contains("grid-column: 3;", Kural(css, $"{Kok} > .alan[id$=\"-tutar-sar\"]"));
        var sil = Kural(css, $"{Kok} > .yz-bacak-sil");
        Assert.Contains("grid-column: 2;", sil);
        Assert.Contains("height: 29px;", sil);
        Assert.Contains("gap: 0;", Kural(css, "html:root[data-sema=\"yildiz\"] .yz .yz-bacaklar-govde"));   // satırlar arası boşluk yok (74 px aralık)
        Assert.Contains("grid-template-columns: minmax(0, 1fr) auto 130px;", css);   // dar ekran: tutar yine sağ uçta
    }

    [Fact]
    public void Y4C2_04_yeni_kurallar_yalniz_yildiz_kapsaminda_ve_sabit_renksiz()
    {
        var css = Css();
        foreach (var s in new[] { $"{Kok} > .yz-bacak-ad .secici-liste-kap", $"{Kok} :is(.alan, .secici-alan) > :is(label, .alan-etiket) *" })
        {
            Assert.StartsWith("html:root[data-sema=\"yildiz\"]", s);
            Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgb\(", Kural(css, s));
        }
    }
}

/// <summary>
/// PAKET YILDIZ-4C-2 — Peşin art arda sorusu Yıldız tezgâhında eski <c>.btn</c> taşımaz (gerçek Chrome ESKI
/// sayacı 10-basari hâlinde 2 buldu: #pesin-soru-evet / #pesin-soru-hayir). Öteki şemalar özgün sınıfla.
/// </summary>
public sealed class Yildiz4C2SoruTestleri : Bunit.BunitContext
{
    public Yildiz4C2SoruTestleri()
    {
        JSInterop.Mode = Bunit.JSRuntimeMode.Loose;
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<Kuyumcu.Web.Servisler.DilServisi>(Services);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Y4C2_07_pesin_sorusu_yildizda_b2_platinde_ozgun_btn(bool yildiz)
    {
        var evet = 0; var hayir = 0;
        var e = Render<Kuyumcu.Web.Bilesenler.Fis.PesinArtArdaSoru>(p => p
            .Add(x => x.Goster, true).Add(x => x.Yildiz, yildiz)
            .Add(x => x.Evet, () => evet++).Add(x => x.Hayir, () => hayir++));
        foreach (var id in new[] { "#pesin-soru-evet", "#pesin-soru-hayir" })
        {
            var d = e.Find(id);
            Assert.Equal(yildiz, d.ClassList.Contains("yz-b2"));
            Assert.Equal(!yildiz, d.ClassList.Contains("btn"));
        }
        e.Find("#pesin-soru-evet").Click();
        e.Find("#pesin-soru-hayir").Click();
        Assert.Equal((1, 1), (evet, hayir));   // davranış AYNI: iki düğme de geri çağrıyı tetikler
    }
}
