using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ProAlan — paket UX-3 Faz A (UX-S5 · UX-S15)
//
// Sınanan: bileşen YALNIZ çizim kararı verir.
//   • profesyonel modda çocuklarını çizer (DOM birebir korunur — id'ler dahil)
//   • basit modda çizmez
//   • basit modda ZorunluGorunur=true iken ÇİZER (yasal kapı açılır)
//   • BasitAlternatif verilmişse basit modda O çizilir
//
// ⚠ ArayuzModuServisi'nin kurucusunda HttpClient/ApiIstemci YOKTUR (Aşkın K2):
//   bu sınıf sahte API kurmadan yalnız OturumServisi ile ayağa kalkar.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class ProAlanEkranTestleri : BunitContext
{
    public ProAlanEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    /// <summary>Oturumu verilen ETKİN modda kurar (değeri sunucu çözmüş kabul edilir).</summary>
    private void ModuKur(string etkinMod) =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "TEZGAHTAR", "s1",
                                null, etkinMod));

    private static RenderFragment Cocuk => b =>
    {
        b.OpenElement(0, "input");
        b.AddAttribute(1, "id", "islem-vade");
        b.CloseElement();
    };

    private static RenderFragment Alternatif => b =>
    {
        b.OpenElement(0, "p");
        b.AddAttribute(1, "id", "islem-vade-sade");
        b.AddContent(2, "Peşin");
        b.CloseElement();
    };

    private IRenderedComponent<ProAlan> Ciz(bool zorunlu = false, bool alternatif = false) =>
        Render<ProAlan>(p =>
        {
            p.Add(x => x.ChildContent, Cocuk);
            p.Add(x => x.ZorunluGorunur, zorunlu);
            if (alternatif) p.Add(x => x.BasitAlternatif, Alternatif);
        });

    [Fact]
    public void Profesyonel_modda_cocuklarini_CIZER_ve_id_KORUNUR()
    {
        ModuKur(ArayuzModuServisi.Profesyonel);
        var e = Ciz();

        Assert.NotNull(e.Find("#islem-vade"));
    }

    [Fact]
    public void Basit_modda_CIZMEZ()
    {
        ModuKur(ArayuzModuServisi.Basit);
        var e = Ciz();

        Assert.Empty(e.FindAll("#islem-vade"));
    }

    [Fact]
    public void Basit_modda_ZorunluGorunur_ile_CIZER()
    {
        // YASAL KAPI: sunucu kaynaklı bayrak true olunca gizli alan AÇILIR.
        ModuKur(ArayuzModuServisi.Basit);
        var e = Ciz(zorunlu: true);

        Assert.NotNull(e.Find("#islem-vade"));
    }

    [Fact]
    public void Basit_modda_BasitAlternatif_cizilir_asil_icerik_cizilmez()
    {
        ModuKur(ArayuzModuServisi.Basit);
        var e = Ciz(alternatif: true);

        Assert.NotNull(e.Find("#islem-vade-sade"));
        Assert.Empty(e.FindAll("#islem-vade"));
    }

    [Fact]
    public void Basit_modda_ZorunluGorunur_ALTERNATIFI_EZER()
    {
        ModuKur(ArayuzModuServisi.Basit);
        var e = Ciz(zorunlu: true, alternatif: true);

        Assert.NotNull(e.Find("#islem-vade"));
        Assert.Empty(e.FindAll("#islem-vade-sade"));
    }

    [Fact]
    public void Profil_yuklenmeden_PROFESYONEL_cizilir_bugunku_ekran()
    {
        // Oturum kullanıcısı henüz yok (ilk çizim): sade yüze DÜŞÜLMEZ.
        var e = Ciz();

        Assert.NotNull(e.Find("#islem-vade"));
    }

    [Fact]
    public void Mod_degisince_bilesen_KENDINI_YENILER()
    {
        ModuKur(ArayuzModuServisi.Profesyonel);
        var e = Ciz();
        Assert.NotNull(e.Find("#islem-vade"));

        Services.GetRequiredService<ArayuzModuServisi>().Uygula("BASIT", "BASIT");

        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-vade")));
    }
}
