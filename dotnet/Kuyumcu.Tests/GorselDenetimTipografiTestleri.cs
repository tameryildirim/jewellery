namespace Kuyumcu.Tests;

/// <summary>
/// Görsel denetim O-6 · O-7 · D-7 (kurucu 21.10 "tuvalle çelişen dört bulguyu da yap"): tek ekran başlığı 40/300
/// (telefonda 28), gövde 400, ölçek dışı ara boyların kalıcı katmanı. Katman tuvalden BİLEREK sapar; silinirse
/// başlık 64/200, gövde 17/300'e döner — bu test kırmızı olur.
/// </summary>
public sealed class GorselDenetimTipografiTestleri
{
    private static string Css() => File.ReadAllText(CssKaynak.Yol("Kuyumcu.Web", "wwwroot", "css", "tema-yildiz.css"));

    [Fact]
    public void Tek_baslik_40_300_telefonda_28_govde_400()
    {
        var css = Css();
        var i = css.IndexOf("TİPOGRAFİ SADELEŞTİRME", StringComparison.Ordinal);
        Assert.True(i > 0, "tipografi katmanı yok");
        var katman = css[i..];
        Assert.Contains("html:root[data-sema=\"yildiz\"] body { font-weight: 400; }", katman);
        Assert.Contains("font-size: 40px !important; font-weight: 300 !important;", katman);
        Assert.Contains("{ font-size: 28px !important; }", katman);
        Assert.Contains(".yz-sayfa-baslik > h1", katman);
        Assert.Contains("#ust-bar-rota-adi", katman);
        Assert.Contains(":is(.yz-liste-d, .yz-liste-ad):not(#_):not(#__) { font-size: 18px; font-weight: 400; }", katman);
    }
}
