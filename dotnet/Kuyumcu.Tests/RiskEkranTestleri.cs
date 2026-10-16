using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 40-A — RİSK SEKMESİ (bUnit).
//
// SINANAN ŞEYLER:
//   • Sekme açılınca /masak/risk/ozet ÇAĞRILIR (DoD EK md.1 — düğme gerçekten
//     istek atar; SahteApi giden URL'leri sayar).
//   • Cari SEÇİLMEDEN tekil /masak/risk/{id} ucu ÇAĞRILMAZ (ön yükleme yasağı,
//     Mehmet PLAN-2 + K40-3 — tekil uç okuma izi düşürür).
//   • Gerekçe kısayken Kaydet KAPALI (disabled="@(false)" tuzağı).
//   • Yanıtta kimlik belge numarası alanı YOK — sahte API tam yanıt dönse bile
//     ekran basmaz.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class RiskEkranTestleri : BunitContext
{
    private const string OzetJson = """
        {"dusuk":2,"orta":1,"yuksek":1,"pepSayisi":1,"tazelenecek":0,"profilsiz":3,
         "kuralEtkin":false,"listeSiniri":50,
         "yuksekRiskliler":[{"cariId":"c9","cariAd":"Yüksek Riskli Müşteri","puan":75,
           "seviye":"YUKSEK","kaynak":"HESAP","sebepler":["PEP_BEYANI","NAKIT_AGIRLIK"],
           "olusturuldu":"2026-09-17T08:00:00Z"}]}
        """;

    /// <summary>
    /// ⚠ BİLİNÇLİ OLARAK FAZLA ALAN: sunucu kimlik belge numarası DÖNDÜRMESE de
    /// ekranın onu basmadığı sınanmalı. Sahte API "kötü" davranır, ekran doğru.
    /// </summary>
    private const string ProfilJson = """
        {"cariId":"c9","cariAd":"Yüksek Riskli Müşteri","pepBeyani":true,
         "pepGorev":"Belediye meclis üyesi","pepBeyanTarihi":"2026-09-01T08:00:00Z",
         "belgeNo":"10000000146",
         "guncel":{"id":"p2","cariId":"c9","kaynak":"HESAP","puan":75,"seviye":"YUKSEK",
           "sebepler":["PEP_BEYANI","NAKIT_AGIRLIK"],"gerekce":null,"kullaniciId":"u1",
           "hesaplanmaTarihi":"2026-09-17T08:00:00Z","olusturuldu":"2026-09-17T08:00:00Z"},
         "tarihce":[{"id":"p2","cariId":"c9","kaynak":"HESAP","puan":75,"seviye":"YUKSEK",
           "sebepler":["PEP_BEYANI","NAKIT_AGIRLIK"],"gerekce":null,"kullaniciId":"u1",
           "hesaplanmaTarihi":"2026-09-17T08:00:00Z","olusturuldu":"2026-09-17T08:00:00Z"},
          {"id":"p1","cariId":"c9","kaynak":"ELLE","puan":30,"seviye":"ORTA",
           "sebepler":[],"gerekce":"Uyum görevlisi kararı","kullaniciId":"u1",
           "hesaplanmaTarihi":"2026-09-10T08:00:00Z","olusturuldu":"2026-09-10T08:00:00Z"}],
         "kuralEtkin":false,"gerekceAsgari":20,"tazelemeGunu":30}
        """;

    private const string CarilerJson = """
        [{"id":"c9","ad":"Yüksek Riskli Müşteri","riskSeviye":"YUKSEK","pepBeyani":true},
         {"id":"c1","ad":"Normal Müşteri","riskSeviye":null,"pepBeyani":false}]
        """;

    private const string MasakOzetJson = """
        {"acikBildirim":0,"suresiGecmis":0,"yaklasan":0,"son30GunTespit":0}
        """;

    public RiskEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi())
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        SahteApi.Istekler.Clear();
    }

    private sealed class SahteApi : HttpMessageHandler
    {
        internal static readonly List<string> Istekler = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            Istekler.Add(istek.RequestUri!.ToString());
            var yol = istek.RequestUri!.AbsolutePath;
            // ⚠ "{} varsayılanı" YOK: her uca GERÇEKÇİ yanıt verilir, yoksa
            //   ekran sessizce boş kalır ve test yanlış güven verirdi.
            var govde =
                yol.EndsWith("/masak/risk/ozet") ? OzetJson
                : yol.Contains("/masak/risk/") ? ProfilJson
                : yol.Contains("/cari") ? CarilerJson
                : yol.EndsWith("/masak/ozet") ? MasakOzetJson
                : "[]";
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Uyum> RiskSekmesi()
    {
        var ekran = Render<Kuyumcu.Web.Pages.Uyum>();
        ekran.Find("#uy-sekme-risk").Click();
        return ekran;
    }

    // ── Sekme açılınca özet çekilir (düğme GERÇEKTEN istek atar) ─────────────

    [Fact]
    public void Risk_sekmesi_acilinca_risk_ozet_ucu_cagrilir()
    {
        RiskSekmesi();
        Assert.Contains(SahteApi.Istekler, u => u.Contains("/masak/risk/ozet"));
    }

    // ── ÖN YÜKLEME YASAĞI (Mehmet koşulu) ───────────────────────────────────

    [Fact]
    public void Cari_secilmeden_tekil_risk_ucu_HIC_cagrilmaz()
    {
        RiskSekmesi();

        var tekil = SahteApi.Istekler
            .Where(u => u.Contains("/masak/risk/") && !u.Contains("/masak/risk/ozet"))
            .ToList();

        Assert.Empty(tekil);
    }

    [Fact]
    public void Cari_secilince_tekil_uc_cagrilir_ve_profil_gosterilir()
    {
        var ekran = RiskSekmesi();
        SeciciYardim.Sec(ekran, "uy-risk-cari", "c9");

        Assert.Contains(SahteApi.Istekler, u => u.Contains("/masak/risk/c9"));
        Assert.Equal("75", ekran.Find("#uy-risk-puan").TextContent.Trim());
    }

    // ── KVKK: kimlik verisi ekrana basılmaz ─────────────────────────────────

    [Fact]
    public void Sahte_API_belge_no_donse_bile_ekran_BASMAZ()
    {
        var ekran = RiskSekmesi();
        SeciciYardim.Sec(ekran, "uy-risk-cari", "c9");

        Assert.DoesNotContain("10000000146", ekran.Markup);
    }

    // ── UX-S31: ham kod ekrana basılmaz ─────────────────────────────────────

    [Fact]
    public void Ham_seviye_ve_sebep_kodlari_ekrana_BASILMAZ()
    {
        var ekran = RiskSekmesi();
        SeciciYardim.Sec(ekran, "uy-risk-cari", "c9");

        var sebepler = ekran.Find("#uy-risk-sebepler").TextContent;
        Assert.DoesNotContain("PEP_BEYANI", sebepler);
        Assert.DoesNotContain("NAKIT_AGIRLIK", sebepler);
        Assert.Contains("Siyasi nüfuz", sebepler);
        Assert.Equal("Yüksek", ekran.Find("#uy-risk-seviye").TextContent.Trim());
    }

    // ── UX-S30: kural kapalıysa SEBEBİ yazar ────────────────────────────────

    [Fact]
    public void Kural_kapaliyken_sebebi_ekranda_yazar()
    {
        var ekran = RiskSekmesi();
        Assert.NotEmpty(ekran.FindAll("#uy-risk-kapali"));
        Assert.Contains("durdurmaz", ekran.Find("#uy-risk-durdurmaz").TextContent);
    }

    // ── Gerekçe < 20 → Kaydet KAPALI (disabled="@(false)" tuzağı) ────────────

    [Fact]
    public void Gerekce_kisayken_elle_degistir_KAYDET_KAPALI()
    {
        var ekran = RiskSekmesi();
        SeciciYardim.Sec(ekran, "uy-risk-cari", "c9");
        ekran.Find("#uy-risk-elle-ac").Click();

        ekran.Find("#uy-risk-elle-gerekce").Input("kısa");

        var kaydet = ekran.Find("#cekmece-risk-elle-kaydet");
        Assert.True(kaydet.HasAttribute("disabled"));
        Assert.NotEmpty(ekran.FindAll("#uy-risk-elle-hata"));
    }

    [Fact]
    public void Gerekce_yeterince_uzunsa_KAYDET_ACILIR_ve_istek_atar()
    {
        var ekran = RiskSekmesi();
        SeciciYardim.Sec(ekran, "uy-risk-cari", "c9");
        ekran.Find("#uy-risk-elle-ac").Click();
        ekran.Find("#uy-risk-elle-gerekce").Input("Uyum görevlisi incelemesi sonucu seviye değiştirildi.");

        var kaydet = ekran.Find("#cekmece-risk-elle-kaydet");
        Assert.False(kaydet.HasAttribute("disabled"));

        SahteApi.Istekler.Clear();
        kaydet.Click();
        Assert.Contains(SahteApi.Istekler, u => u.Contains("/masak/risk/c9/elle"));
    }

    // ── DoD EK md.1: "yeniden hesapla" gerçekten POST atar ──────────────────

    [Fact]
    public void Yeniden_hesapla_dugmesi_POST_hesapla_ucuna_gider()
    {
        var ekran = RiskSekmesi();
        SeciciYardim.Sec(ekran, "uy-risk-cari", "c9");

        SahteApi.Istekler.Clear();
        ekran.Find("#uy-risk-hesapla").Click();

        Assert.Contains(SahteApi.Istekler, u => u.Contains("/masak/risk/c9/hesapla"));
    }

    // ── UX-S25: değiştirilemeyen kalem SEBEBİYLE birlikte görünür ───────────

    [Fact]
    public void Gecmis_profil_satiri_icin_DegismezIpucu_gosterilir()
    {
        var ekran = RiskSekmesi();
        SeciciYardim.Sec(ekran, "uy-risk-cari", "c9");

        Assert.NotEmpty(ekran.FindAll("#degismez-ipucu-CariRiskProfili"));
        Assert.Contains("yalnız eklenen", ekran.Markup); // KESIF-P8 #87: jargon yerine düz cümle
        Assert.DoesNotContain("append-only", ekran.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // ── UX-S29: sunucu sınırı ekranda yazılır ──────────────────────────────

    [Fact]
    public void Cari_secicisinin_sunucu_siniri_ekranda_yazar()
    {
        Assert.Contains("200", RiskSekmesi().Find("#uy-risk-cari-sar").ParentElement!.TextContent);
    }
}
