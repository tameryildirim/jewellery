namespace Kuyumcu.Tests;

/// <summary>
/// Kurucu 24.10 (/ayarlar?sekme=guvenlik, Sistem künyesi): "aşağıdan yukarıya çıkamıyorum". Sayfa İÇİNDEKİ katlanır
/// kutular (Görünüm/Güvenlik ayrıntısı, sade ayrıntı, kılavuz ekran listesi) kendi kutusunda kayar; `overscroll-behavior:
/// contain` kutunun başında/sonunda tekerleği sayfaya geçirmiyor, kullanıcı kutuda hapsoluyordu. Sayfa içi kutuda zincir
/// AÇIK (auto) kalır; contain yalnız katmanlar (çekmece, pencere, açılır liste) içindir.
/// </summary>
public sealed class KaydirmaZinciriTestleri
{
    private static string Css(string ad) => File.ReadAllText(CssKaynak.Yol("Kuyumcu.Web", "wwwroot", "css", ad));

    [Theory]
    [InlineData("katman.css", ".gk-ayrinti[open] > .gk-ayrinti-govde")]
    [InlineData("sade.css", ".sade-ayrinti-govde")]
    [InlineData("sade.css", "#kl-ekranlar-ayrinti > #kl-ekranlar")]
    public void Sayfa_ici_katlanir_kutu_tekerlegi_hapsetmez(string dosya, string secici)
    {
        var css = Css(dosya);
        var i = css.IndexOf(secici + " {", StringComparison.Ordinal);
        Assert.True(i >= 0, secici + " kuralı yok");
        var govde = css[i..css.IndexOf('}', i)];
        Assert.Contains("overflow-y: auto", govde);                 // kutu kendi içinde kayar (tasarım kararı korunur)
        Assert.Contains("overscroll-behavior: auto", govde);        // …ama zincir sayfaya açık
        Assert.DoesNotContain("overscroll-behavior: contain", govde);
    }

    [Fact]
    public void Katmanlarda_contain_kalir()
    {
        // Açılır liste ve çekmece/pencere gövdesi: kaydırma sayfaya SIZMAZ (kasıtlı; değişmedi).
        Assert.Contains("overscroll-behavior: contain", Css("app.css")[Css("app.css").IndexOf(".secici-liste {", StringComparison.Ordinal)..]);
        Assert.Contains("overscroll-behavior: contain", Css("app.css")[Css("app.css").IndexOf(".modal-govde {", StringComparison.Ordinal)..]);
    }
}
