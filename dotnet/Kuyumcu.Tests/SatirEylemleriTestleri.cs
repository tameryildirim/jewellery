using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-61 — SATIR EYLEMLERİ TAŞMA MENÜSÜ (Bilesenler/SatirEylemleri.razor)
//
// Kural: eylem sayısı 3'ü aşarsa satırda ilk 2 eylem + "⋯" görünür, geri kalan
// HER eylem "⋯" penceresindedir (özellik silinmez). ≤ 3 ise hepsi satırdadır.
// Pencere yerli HTML popover'dır: "⋯" gerçek <button> (Enter/Boşluk açar),
// Esc/dışarı tıklama tarayıcı tarafından kapatır; her açılış/kapanış bir
// "toggle" olayı atar ve aria-expanded ONDAN türetilir. bUnit tarayıcı değildir:
// yerli açma/kapama burada "toggle" olayıyla benzetilir; tarayıcıdaki gerçek
// Esc davranışı canlı doğrulamada (rapor § canlı) ölçülür.
// ═══════════════════════════════════════════════════════════════════════════

public sealed class SatirEylemleriTestleri : BunitContext
{
    private readonly List<string> tiklanan = [];

    public SatirEylemleriTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private List<SatirEylemleri.Eylem> Eylemler(int adet) =>
        Enumerable.Range(1, adet)
            .Select(i => new SatirEylemleri.Eylem($"x-e{i}-r1", $"Eylem {i}",
                EventCallback.Factory.Create(this, () => tiklanan.Add($"e{i}"))))
            .ToList();

    private IRenderedComponent<SatirEylemleri> Ciz(IReadOnlyList<SatirEylemleri.Eylem> eylemler) =>
        Render<SatirEylemleri>(p => p
            .Add(c => c.Onek, "x")
            .Add(c => c.SatirId, "r1")
            .Add(c => c.SatirAdi, "Ayşe Demir")
            .Add(c => c.Eylemler, eylemler));

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(9)]
    public void UctenFazlaEylem_TasmaMenusuneDuser(int adet)
    {
        var e = Ciz(Eylemler(adet));

        // Satırda görünen: ilk 2 eylem + "⋯" = 3 düğme.
        var gorunen = e.Find("#x-eylem-r1").QuerySelectorAll("button").ToList();
        Assert.Equal(3, gorunen.Count);
        Assert.Equal(["x-e1-r1", "x-e2-r1", "x-menu-ac-r1"], gorunen.Select(b => b.Id));
        Assert.Equal(SatirEylemleri.BirincilSinir, gorunen.Count - 1);

        // Taşan HER eylem pencerede, sırası korunur; sonda Kapat.
        var menu = e.Find("#x-menu-r1");
        Assert.Equal("auto", menu.GetAttribute("popover"));
        var ogeler = menu.QuerySelectorAll("button").ToList();
        Assert.Equal(
            Enumerable.Range(3, adet - 2).Select(i => $"x-e{i}-r1").Append("x-menu-kapat-r1"),
            ogeler.Select(b => b.Id));
        // Her öğe pencereyi kapatır (popovertargetaction=hide) VE kendi işini yapar.
        Assert.All(ogeler, b =>
        {
            Assert.Equal("x-menu-r1", b.GetAttribute("popovertarget"));
            Assert.Equal("hide", b.GetAttribute("popovertargetaction"));
        });
        e.Find($"#x-e{adet}-r1").Click();
        Assert.Equal([$"e{adet}"], tiklanan);
        // Görünen birincil eylem de gerçek iş yapar (DoD EK 1).
        e.Find("#x-e1-r1").Click();
        Assert.Equal([$"e{adet}", "e1"], tiklanan);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void UcVeAzEylem_HepsiSatirda_MenuYok(int adet)
    {
        var e = Ciz(Eylemler(adet));
        var gorunen = e.Find("#x-eylem-r1").QuerySelectorAll("button").ToList();
        Assert.Equal(adet, gorunen.Count);
        Assert.Empty(e.FindAll("#x-menu-ac-r1"));
        Assert.Empty(e.FindAll("#x-menu-r1"));
    }

    [Fact]
    public void TasmaMenusu_KlavyeIleAcilirVeEscKapatir()
    {
        var e = Ciz(Eylemler(5));
        var ac = e.Find("#x-menu-ac-r1");

        // Klavye: gerçek <button type="button"> — Enter/Boşluk yerli olarak etkinleştirir;
        // popovertarget ile pencereyi JS'siz açar. Erişilebilir ad ve ilişkiler tam.
        Assert.Equal("BUTTON", ac.TagName);
        Assert.Equal("button", ac.GetAttribute("type"));
        Assert.Null(ac.GetAttribute("tabindex"));
        Assert.Equal("x-menu-r1", ac.GetAttribute("popovertarget"));
        Assert.Equal("x-menu-r1", ac.GetAttribute("aria-controls"));
        Assert.Equal("dialog", ac.GetAttribute("aria-haspopup"));
        Assert.Equal("Ayşe Demir — Diğer işlemler", ac.GetAttribute("aria-label"));
        Assert.Equal("false", ac.GetAttribute("aria-expanded"));

        // Pencere "auto" popover: Esc ve dışarı tıklama tarayıcıda "light dismiss" ile kapatır.
        var menu = e.Find("#x-menu-r1");
        Assert.Equal("auto", menu.GetAttribute("popover"));
        Assert.Equal("dialog", menu.GetAttribute("role"));
        Assert.Equal("x-menu-r1-baslik", menu.GetAttribute("aria-labelledby"));
        Assert.Contains("Esc", menu.TextContent);

        // Açılış (Enter) → tarayıcı "toggle" atar → aria-expanded=true.
        menu.TriggerEvent("ontoggle", EventArgs.Empty);
        Assert.Equal("true", e.Find("#x-menu-ac-r1").GetAttribute("aria-expanded"));

        // Esc → tarayıcı kapatır, yine "toggle" → aria-expanded=false.
        e.Find("#x-menu-r1").TriggerEvent("ontoggle", EventArgs.Empty);
        Assert.Equal("false", e.Find("#x-menu-ac-r1").GetAttribute("aria-expanded"));

        // Yeniden aç → öğe seç: öğe işini yapar, pencere kapanınca aria-expanded=false.
        e.Find("#x-menu-r1").TriggerEvent("ontoggle", EventArgs.Empty);
        Assert.Equal("true", e.Find("#x-menu-ac-r1").GetAttribute("aria-expanded"));
        e.Find("#x-e4-r1").Click();
        e.Find("#x-menu-r1").TriggerEvent("ontoggle", EventArgs.Empty);
        Assert.Equal(["e4"], tiklanan);
        Assert.Equal("false", e.Find("#x-menu-ac-r1").GetAttribute("aria-expanded"));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(4, 1)]
    public void DarSutun_TekBirincil_GerisiTasar(int adet, int beklenenGorunenEylem)
    {
        var e = Render<SatirEylemleri>(p => p
            .Add(c => c.Onek, "x").Add(c => c.SatirId, "r1").Add(c => c.SatirAdi, "FTR-1")
            .Add(c => c.Birincil, 1).Add(c => c.DarSutun, true)
            .Add(c => c.Eylemler, Eylemler(adet)));
        var gorunen = e.Find("#x-eylem-r1").QuerySelectorAll("button").ToList();
        Assert.Equal("x-e1-r1", gorunen[0].Id);
        Assert.Equal(beklenenGorunenEylem, gorunen.Count(b => !b.ClassList.Contains("satir-menu-ac")));
        // Birincili aşan eylem varsa (tek öğe bile) "⋯"ye taşar.
        Assert.Equal(adet > 1, e.FindAll("#x-menu-ac-r1").Count == 1);
        Assert.Equal(adet - 1, e.FindAll("#x-menu-r1 button").Count(b => b.Id != "x-menu-kapat-r1"));
    }

    [Fact]
    public void Birincil_ust_siniri_asamaz()
    {
        var e = Render<SatirEylemleri>(p => p
            .Add(c => c.Onek, "x").Add(c => c.SatirId, "r1")
            .Add(c => c.Birincil, 7).Add(c => c.Eylemler, Eylemler(8)));
        Assert.Equal(SatirEylemleri.BirincilSinir + 1, e.Find("#x-eylem-r1").QuerySelectorAll("button").Length);
    }

    [Fact]
    public void KilitliEylem_disabled_ve_sebebi_yazili()
    {
        var eylemler = Eylemler(4);
        eylemler[3] = new(eylemler[3].Id, eylemler[3].Etiket, eylemler[3].Tikla, kilitli: true, kilitNedeni: "Kur yok");
        eylemler[0] = new(eylemler[0].Id, eylemler[0].Etiket, eylemler[0].Tikla,
                          tehlike: true, ekSinif: "metin-kirmizi", ariaEtiket: "Özel ad");
        var e = Ciz(eylemler);

        var kilitli = e.Find("#x-e4-r1");
        Assert.True(kilitli.HasAttribute("disabled"));
        Assert.Equal("Kur yok", kilitli.GetAttribute("title"));
        var ilk = e.Find("#x-e1-r1");
        Assert.Contains("btn-tehlike", ilk.ClassList);
        Assert.Contains("metin-kirmizi", ilk.ClassList);
        Assert.Equal("Özel ad", ilk.GetAttribute("aria-label"));
        Assert.False(ilk.HasAttribute("disabled"));
    }
}
