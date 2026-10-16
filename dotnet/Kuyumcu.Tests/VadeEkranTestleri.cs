using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// VADE TAKİBİ EKRANI
//
// Sunucu tarafı ne kadar doğru olursa olsun, kasiyer geciken müşteriyi
// EKRANDA göremezse vade takibi yoktur. Sınanan kapılar:
//   • geciken müşteri listenin ÜSTÜNDE (arayacağı kişi ilk sırada)
//   • vadesi bilinmeyen bakiye AYRI sütunda (gecikmiş sayılmaz)
//   • ufuk gün değişince sunucuya YENİDEN sorulur (yerel filtre değil)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class VadeEkranTestleri : BunitContext
{
    private readonly List<string> yollar = [];

    public VadeEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(yollar))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    /// <summary>P-75: bu ufuk sorulunca sahte sunucu 400 döner (sunucu reddi yolu).</summary>
    private const int ReddedilenUfuk = 45;

    private sealed class SahteApi(List<string> kayit) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            kayit.Add(yol);

            if (yol.Contains($"ufukGun={ReddedilenUfuk}"))
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("""{"message":"Sunucu ufku reddetti"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                });

            // P-75: yanıttaki ufuk ve yaklaşan toplamı SORULAN ufka bağlı — "son
            // geçerli değer" sınaması hangi raporun ekranda kaldığını ayırt edebilsin
            // (7 → 7.011 · 30 → 30.011).
            var m = System.Text.RegularExpressions.Regex.Match(yol, @"ufukGun=(\d+)");
            var ufuk = m.Success ? int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 7;
            var yaklasan = (ufuk * 1000 + 11).ToString(System.Globalization.CultureInfo.InvariantCulture);

            var govde = """
                {"ufukGun":UFUK,"gecikenToplam":3000,"yaklasanToplam":YAKLASAN,"vadesizToplam":1500,
                 "cariler":[
                   {"cariId":"c2","cariAd":"Vadesi Gelmemiş","cariTur":"MUSTERI","telefon":"0555",
                    "gecikenTutar":0,"yaklasanTutar":2000,"vadesizTutar":0,
                    "enEskiGecikmeGun":null,"enYakinVade":"2026-09-20",
                    "borclar":[{"hareketId":"h2","tarih":"2026-09-15T08:00:00Z","vade":"2026-09-20",
                                "acikTutar":2000,"gecikmeGun":-5,"durum":"YAKLASIYOR"}]},
                   {"cariId":"c1","cariAd":"Geciken Musteri","cariTur":"MUSTERI","telefon":"0532",
                    "gecikenTutar":3000,"yaklasanTutar":0,"vadesizTutar":1500,
                    "enEskiGecikmeGun":12,"enYakinVade":"2026-09-03",
                    "borclar":[{"hareketId":"h1","tarih":"2026-08-20T08:00:00Z","vade":"2026-09-03",
                                "acikTutar":3000,"gecikmeGun":12,"durum":"GECIKTI"}]}]}
                """.Replace("UFUK", ufuk.ToString(System.Globalization.CultureInfo.InvariantCulture)).Replace("YAKLASAN", yaklasan);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.VadeTakibi> Ciz()
        => Render<Kuyumcu.Web.Pages.VadeTakibi>();

    [Fact]
    public void Acilista_vade_takibi_ucu_cekilir()
    {
        Ciz();
        Assert.Contains(yollar, y => y.Contains("/raporlar/vade-takibi"));
    }

    /// <summary>
    /// Geciken müşteri ÜSTTE olmalı. Sunucu sırası korunsaydı burada
    /// "Vadesi Gelmemiş" ilk satır olurdu — kasiyer önce yanlış kişiyi arardı.
    /// </summary>
    [Fact]
    public void Geciken_musteri_listenin_BASINDA()
    {
        var e = Ciz();
        var ilkSatir = e.FindAll("tbody tr").First();
        Assert.Contains("Geciken Musteri", ilkSatir.TextContent);
    }

    [Fact]
    public void Vadesi_bilinmeyen_bakiye_geciken_toplamina_KARISMAZ()
    {
        var e = Ciz();
        var ilkSatir = e.FindAll("tbody tr").First();
        var hucreler = ilkSatir.QuerySelectorAll("td");

        // sütunlar: müşteri · telefon · geciken · yaklaşan · vadesiz · ...
        Assert.Contains("3.000", hucreler[2].TextContent);
        Assert.Contains("1.500", hucreler[4].TextContent);
    }

    [Fact]
    public void Gecikme_gun_sayisi_gosterilir()
    {
        var e = Ciz();
        Assert.Contains("12", e.FindAll("tbody tr").First().TextContent);
        // TOPLU-4 tur 2 (tasarım hakemi 02.10): durum ham büyük harf kod değil, sözlükteki cümle düzeni.
        Assert.Contains("Gecikti (12 gün)", e.Markup);
        Assert.DoesNotContain("GECİKTİ", e.Markup);
    }

    /// <summary>
    /// Ufuk değişimi SUNUCUYA sorulmalı. Yerel filtre yapılsaydı ilk çağrının
    /// ufkuna göre zaten elenmiş borçlar geri gelmezdi.
    /// </summary>
    [Fact]
    public void Ufuk_gun_degisince_sunucuya_YENIDEN_sorulur()
    {
        var e = Ciz();
        yollar.Clear();
        e.Find("#vade-ufuk").Input("30");

        e.WaitForAssertion(() =>
            Assert.Contains(yollar, y => y.Contains("ufukGun=30")));
    }

    // ═══ PAKET P-75 (QA turu 8) ═════════════════════════════════════════════
    // Geçersiz ufukta TÜM kartlar kayboluyordu (kullanıcı verisini kaybettiğini
    // sanıyordu) ve 7/30/60 düğmeleri basılı durumunu ekran okuyucuya
    // söylemiyordu.

    private static string KartMetni(IRenderedComponent<Kuyumcu.Web.Pages.VadeTakibi> e)
        => string.Join(" | ", e.FindAll(".stat-izgara .stat-kart").Select(k => k.TextContent));

    private static void AriaPressedBekle(IRenderedComponent<Kuyumcu.Web.Pages.VadeTakibi> e, int basili)
    {
        foreach (var g in new[] { 7, 30, 60 })
            Assert.Equal(g == basili ? "true" : "false",
                e.Find($"#vade-ufuk-{g}").GetAttribute("aria-pressed"));
    }

    /// <summary>
    /// Geçersiz ufuk (aralık dışı, boş, kesirli) ve sunucu reddi: kartlar DOM'da
    /// KALIR ve son geçerli raporun (30 gün → yaklaşan 30.011) değerlerini gösterir.
    /// Geçersiz değer sunucuya HİÇ gitmez.
    /// </summary>
    [Fact]
    public void VadeTakibi_GecersizUfuk_KartlarEkrandaKalir()
    {
        var e = Ciz();
        e.WaitForState(() => e.FindAll(".stat-izgara .stat-kart").Count == 5);
        e.Find("#vade-ufuk").Input("30");
        e.WaitForAssertion(() => Assert.Contains("30.011", KartMetni(e)));
        yollar.Clear();

        foreach (var gecersiz in new[] { "-1", "366", "", "7,5" })
        {
            e.Find("#vade-ufuk").Input(gecersiz);
            Assert.Equal(5, e.FindAll(".stat-izgara .stat-kart").Count);
            Assert.Contains("30.011", KartMetni(e));
            Assert.Contains("3.000", KartMetni(e));
            // Tablo da kaybolmaz.
            Assert.Contains("Geciken Musteri", e.Find("tbody").TextContent);
        }
        Assert.Empty(yollar);   // geçersiz ufuk sunucuya gitmedi

        // Sunucu reddi (aralık içinde ama sunucu 400): son rapor yine KALIR.
        e.Find("#vade-ufuk").Input(ReddedilenUfuk.ToString(System.Globalization.CultureInfo.InvariantCulture));
        e.WaitForAssertion(() => Assert.Contains(yollar, y => y.Contains($"ufukGun={ReddedilenUfuk}")));
        e.WaitForAssertion(() => Assert.Contains("Sunucu ufku reddetti", e.Find("#vade-uyari").TextContent));
        Assert.Equal(5, e.FindAll(".stat-izgara .stat-kart").Count);
        Assert.Contains("30.011", KartMetni(e));
    }

    /// <summary>Geçersiz denemede TEK bir role=alert bandı; neden ve ekrandaki rakamların ufku yazar.</summary>
    [Fact]
    public void VadeTakibi_GecersizUfuk_RoleAlertUyarisiBasilir()
    {
        var e = Ciz();
        e.WaitForState(() => e.FindAll(".stat-izgara .stat-kart").Count == 5);
        Assert.Empty(e.FindAll("[role=alert]"));   // geçerli durumda bant yok

        e.Find("#vade-ufuk").Input("-1");
        // GIRDI-AILESI-2: Sayi EnAz/EnCok artık UYGULANIR → kutunun altında kendi alan hatası (.alan-hata)
        // da çıkar; "TEK bant" sözleşmesi alan dışı role=alert için geçerlidir.
        Assert.Single(e.FindAll("#vade-ufuk-hata"));
        var bantlar = e.FindAll("[role=alert]:not(.alan-hata)");
        Assert.Single(bantlar);
        Assert.Equal("vade-uyari", bantlar[0].Id);
        Assert.Contains("0 ile 365 arasında", bantlar[0].TextContent);
        Assert.Contains("son geçerli rapora", bantlar[0].TextContent);
        Assert.Contains("(7 günlük ufuk)", bantlar[0].TextContent);

        e.Find("#vade-ufuk").Input("");
        Assert.Single(e.FindAll("[role=alert]"));
        Assert.Contains("boş bırakıldı", e.Find("#vade-uyari").TextContent);

        e.Find("#vade-ufuk").Input("7,5");
        Assert.Single(e.FindAll("[role=alert]"));
        Assert.Contains("tam sayı", e.Find("#vade-uyari").TextContent);

        // Geçerli değere dönülünce bant KALKAR.
        e.Find("#vade-ufuk").Input("30");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("[role=alert]")));
    }

    /// <summary>Seçili hızlı düğmede aria-pressed=true, diğerlerinde false; sınıf ile aynı kaynak.</summary>
    [Fact]
    public void VadeTakibi_HizliSecim_AriaPressedDogru()
    {
        var e = Ciz();
        e.WaitForState(() => e.FindAll("#vade-ufuk-7").Count > 0);
        AriaPressedBekle(e, 7);   // varsayılan 7 gün

        foreach (var g in new[] { 60, 30, 7 })
        {
            e.Find($"#vade-ufuk-{g}").Click();
            e.WaitForAssertion(() => AriaPressedBekle(e, g));
            Assert.Contains("secili", e.Find($"#vade-ufuk-{g}").ClassName);
            foreach (var d in new[] { 7, 30, 60 }.Where(d => d != g))
                Assert.DoesNotContain("secili", e.Find($"#vade-ufuk-{d}").ClassName);
        }

        // Hızlı düğmeler form gönderimi tetiklemesin.
        Assert.All(e.FindAll("[id^=vade-ufuk-]").Where(b => b.TagName == "BUTTON"),
            b => Assert.Equal("button", b.GetAttribute("type")));
    }

    /// <summary>Girdi ↔ düğmeler tek kaynaktan: 30 yazınca #vade-ufuk-30 basılı; düğme girdiyi günceller.</summary>
    [Fact]
    public void VadeTakibi_GirdiVeDugmeler_TekKaynaktanSenkron()
    {
        var e = Ciz();
        e.WaitForState(() => e.FindAll("#vade-ufuk-30").Count > 0);

        e.Find("#vade-ufuk").Input("30");
        e.WaitForAssertion(() => AriaPressedBekle(e, 30));

        // Hızlı seçimde olmayan değer: hiçbir düğme basılı değil.
        e.Find("#vade-ufuk").Input("14");
        e.WaitForAssertion(() =>
        {
            foreach (var g in new[] { 7, 30, 60 })
                Assert.Equal("false", e.Find($"#vade-ufuk-{g}").GetAttribute("aria-pressed"));
        });

        // Düğme → girdi.
        e.Find("#vade-ufuk-60").Click();
        e.WaitForAssertion(() => Assert.Equal("60", e.Find("#vade-ufuk").GetAttribute("value")));
        AriaPressedBekle(e, 60);

        // Geçersiz girdi basılı düğmeyi DEĞİŞTİRMEZ (ekrandaki rapor hâlâ 60 gün).
        e.Find("#vade-ufuk").Input("-1");
        AriaPressedBekle(e, 60);

        // Sunucu reddinde basılı düğme ekrandaki raporun ufkuna döner.
        e.Find("#vade-ufuk-30").Click();
        e.WaitForAssertion(() => AriaPressedBekle(e, 30));
        e.Find("#vade-ufuk").Input(ReddedilenUfuk.ToString(System.Globalization.CultureInfo.InvariantCulture));
        e.WaitForAssertion(() => Assert.Contains("Sunucu ufku reddetti", e.Find("#vade-uyari").TextContent));
        AriaPressedBekle(e, 30);
    }

    /// <summary>Mevcut kimlikler korunur (ekran testleri ve tarayıcı turu bunları arıyor).</summary>
    [Fact]
    public void VadeTakibi_AlanKimlikleri_Korundu()
    {
        var e = Ciz();
        e.WaitForState(() => e.FindAll("#vade-ufuk-7").Count > 0);
        Assert.Equal("INPUT", e.Find("#vade-ufuk").TagName);
        foreach (var g in new[] { 7, 30, 60 })
            Assert.Equal("BUTTON", e.Find($"#vade-ufuk-{g}").TagName);
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#vade-ufuk-aciklama").TextContent));
        // Etiket girdiye bağlı kalır (for → #vade-ufuk).
        Assert.NotEmpty(e.FindAll("label[for=vade-ufuk]"));
    }
}
