using Bunit;
using Kuyumcu.Core.Kilavuz;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Kuyumcu.Tests;

// Y-01 (21.10, rakip Hasfiyat/Pusula/KuyumSmart) — ekran başına eğitim videosu: üretilmiş liste, dosyalar ve Kılavuz ekranı.
public sealed class Y01EgitimVideolariTestleri : BunitContext
{
    public Y01EgitimVideolariTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder().Build());
    }

    private static string VideoDizini() => Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", "wwwroot", "video");

    [Fact]
    public void Liste_dokuz_video_tasir_her_biri_bolumlu_ve_sureli()
    {
        Assert.Equal(9, EgitimVideolari.Hepsi.Count);
        Assert.Equal(EgitimVideolari.Hepsi.Count, EgitimVideolari.Hepsi.Select(v => v.Kimlik).Distinct().Count());
        foreach (var v in EgitimVideolari.Hepsi)
        {
            Assert.Matches("^[a-z-]+$", v.Kimlik);
            Assert.InRange(v.SureSn, 30, 180);                       // "1–2 dk" sözü: 30 sn – 3 dk
            Assert.InRange(v.Bolumler.Count, 2, 8);
            Assert.Equal(v.Bolumler.OrderBy(b => b.Sn).Select(b => b.Baslik), v.Bolumler.Select(b => b.Baslik));   // başlangıçlar artan
            Assert.All(v.Bolumler, b => Assert.False(string.IsNullOrWhiteSpace(b.Baslik)));
        }
        Assert.Equal("1:10", EgitimVideolari.Sure(69.5));
        Assert.Equal("0:53", EgitimVideolari.Sure(52.8));
    }

    [Fact]
    public void Her_videonun_mp4_dosyasi_wwwroot_video_altinda_ve_bos_degil()
    {
        foreach (var v in EgitimVideolari.Hepsi)
        {
            var yol = Path.Combine(VideoDizini(), $"egitim-{v.Kimlik}.mp4");
            Assert.True(File.Exists(yol), yol);
            Assert.InRange(new FileInfo(yol).Length, 200_000, 20_000_000);   // 200 KB – 20 MB (SW ön belleğine girmez; mp4 UZANTILAR dışı)
            Assert.StartsWith("/video/egitim-", v.Dosya);
        }
        var sw = File.ReadAllText(Path.Combine(KilavuzUretimTestleri.DotnetKoku(), "Kuyumcu.Web", "wwwroot", "service-worker.published.js"));
        Assert.DoesNotContain("mp4", sw);
    }

    [Fact]
    public void Kilavuz_ekrani_videolari_oynaticiyla_ve_bolum_dugmeleriyle_cizer()
    {
        var c = Render<Kuyumcu.Web.Pages.Kilavuz>();
        var videolar = c.FindAll("#kl-videolar li.egitim-video");
        Assert.Equal(EgitimVideolari.Hepsi.Count, videolar.Count);
        foreach (var v in EgitimVideolari.Hepsi)
        {
            var oynatici = c.Find($"#kl-video-oynatici-{v.Kimlik}");
            Assert.Equal(v.Dosya, oynatici.GetAttribute("src"));
            Assert.Equal("metadata", oynatici.GetAttribute("preload"));   // 9 video açılışta indirilmez
            Assert.NotNull(oynatici.GetAttribute("controls"));
            Assert.Equal(v.Bolumler.Count, c.FindAll($"#kl-video-{v.Kimlik} .egitim-video-bolumler button").Count);
        }
        Assert.Contains(EgitimVideolari.Hepsi.Count.ToString(), c.Find("#kl-egitim-aciklama").TextContent);
        // Bölüm düğmesi JS'ye atlama çağrısı yapar (kuyumcuVideoAtla)
        c.Find($"#kl-video-{EgitimVideolari.Hepsi[0].Kimlik} .egitim-video-bolumler button").Click();
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "kuyumcuVideoAtla");
    }
}
