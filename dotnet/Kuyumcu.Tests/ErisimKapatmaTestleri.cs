using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ERİŞİM KAPATMA — KULLANICI VE KASA
//
// TESPİT (2026-09-14, uç kapsamı ölçümü): `DELETE /kullanicilar/{id}` ve
// `DELETE /kasalar/{id}` sunucuda çalışıyordu, ekranda karşılığı yoktu.
//
// ⚠ Kullanıcı tarafı bir GÜVENLİK boşluğuydu: işten ayrılan bir çalışanın
// erişimi üründen kapatılamıyordu; şifresini bildiği sürece girmeye devam
// ediyordu.
//
// İki kapı sınanır:
//   • SAHİP kendi erişimini kapatamaz (kendini kilitlerdi)
//   • BAKİYELİ kasa kapatılamaz ("o para nereye gitti" cevapsız kalır)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KullaniciErisimTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string Liste = """
        [{"id":"ben","ad":"Ramazan","soyad":"A.","email":"sahip@ornek.com","rol":"SAHIP",
          "aktif":true,"sonGiris":null},
         {"id":"u2","ad":"Ayşe","soyad":"K.","email":"ayse@ornek.com","rol":"TEZGAHTAR",
          "aktif":true,"sonGiris":null},
         {"id":"u3","ad":"Pasif","soyad":"K.","email":"pasif@ornek.com","rol":"TEZGAHTAR",
          "aktif":false,"sonGiris":null}]
        """;

    public KullaniciErisimTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, Liste))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    internal sealed class SahteApi(List<(string, string, string)> kayit, string govdeMetni)
        : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var g = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery, g));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    istek.RequestUri.AbsolutePath.EndsWith("/gun-kapanisi/onizleme") ? Onizleme() : govdeMetni,
                    System.Text.Encoding.UTF8, "application/json"),
            };
        }

        /// <summary>
        /// PARA-DEMO: Günü Kapat çekmecesi sayılacak nakdi bu uçtan okur. Gerçekçi yanıt:
        /// kart/havale yok → nakit defteri = listedeki ilk kasanın bakiyesi.
        /// </summary>
        private string Onizleme()
        {
            var bakiye = 0m;
            using (var d = System.Text.Json.JsonDocument.Parse(govdeMetni))
                if (d.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array
                    && d.RootElement.GetArrayLength() > 0
                    && d.RootElement[0].TryGetProperty("bakiye", out var b))
                    bakiye = b.GetDecimal();
            var t = bakiye.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return $$"""{"toplamBakiye":{{t}},"kartToplam":0,"havaleToplam":0,"nakitDefter":{{t}}}""";
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Kullanicilar> Ciz()
    {
        var oturum = Services.GetRequiredService<OturumServisi>();
        oturum.KullaniciAta(new OturumKullanici("ben", "Ramazan", "A.", "sahip@ornek.com", "SAHIP", null));
        var e = Render<Kuyumcu.Web.Pages.Kullanicilar>();
        istekler.Clear();
        return e;
    }

    private static AngleSharp.Dom.IElement Satir(
        IRenderedComponent<Kuyumcu.Web.Pages.Kullanicilar> e, string eposta)
        => e.FindAll("tbody tr").First(tr => tr.TextContent.Contains(eposta));

    [Fact]
    public void Baskasinin_satirinda_ERISIMI_KAPAT_dugmesi_vardir()
    {
        var e = Ciz();
        var satir = Satir(e, "ayse@ornek.com");

        Assert.Contains(satir.QuerySelectorAll("button"),
                        b => b.TextContent.Contains("Erişimi Kapat"));
    }

    [Fact]
    public void KENDI_satirinda_erisim_kapatma_dugmesi_YOKTUR()
    {
        // SAHİP kendini kilitlerse işletmeyi yönetecek kimse kalmaz.
        var e = Ciz();
        var satir = Satir(e, "sahip@ornek.com");

        Assert.DoesNotContain(satir.QuerySelectorAll("button"),
                              b => b.TextContent.Contains("Erişimi Kapat"));
    }

    [Fact]
    public void Zaten_pasif_kullanicida_dugme_YOKTUR()
    {
        var e = Ciz();
        var satir = Satir(e, "pasif@ornek.com");

        Assert.DoesNotContain(satir.QuerySelectorAll("button"),
                              b => b.TextContent.Contains("Erişimi Kapat"));
    }

    [Fact]
    public void Erisim_kapatma_ONAY_ister_once_istek_gitmez()
    {
        var e = Ciz();
        Satir(e, "ayse@ornek.com").QuerySelectorAll("button")
            .First(b => b.TextContent.Contains("Erişimi Kapat")).Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "DELETE");
    }

    [Fact]
    public void Onaylaninca_DELETE_dogru_kullaniciya_gonderilir()
    {
        var e = Ciz();
        Satir(e, "ayse@ornek.com").QuerySelectorAll("button")
            .First(b => b.TextContent.Contains("Erişimi Kapat")).Click();
        e.FindAll("button").Where(b => b.TextContent.Contains("Erişimi Kapat")).Last().Click();

        Assert.Contains(istekler, i => i.Metot == "DELETE" && i.Yol.EndsWith("/kullanicilar/u2"));
    }
}

public sealed class KasaKapatmaTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string Kasalar = """
        [{"id":"k1","ad":"Boş Kasa","paraBirimi":"TRY","bakiye":0,"aktif":true},
         {"id":"k2","ad":"Dolu Kasa","paraBirimi":"TRY","bakiye":15000,"aktif":true}]
        """;

    public KasaKapatmaTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(
            new KullaniciErisimTestleri.SahteApi(istekler, Kasalar))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        // PAKET S3-6: /kasalar özet kartlarını ProAlan'a aldı (iki yüz, tek gövde);
        // ProAlan mod servisini ister. Varsayılan mod PROFESYONEL → DOM bugünküyle aynı.
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Kasalar> Ciz()
    {
        var e = Render<Kuyumcu.Web.Pages.Kasalar>();
        istekler.Clear();
        return e;
    }

    private static AngleSharp.Dom.IElement Kart(
        IRenderedComponent<Kuyumcu.Web.Pages.Kasalar> e, string ad)
        => e.FindAll(".stat-izgara > .kart").First(k => k.TextContent.Contains(ad));

    [Fact]
    public void Bos_kasa_pasife_ALINABILIR()
    {
        var e = Ciz();
        var dugme = Kart(e, "Boş Kasa").QuerySelectorAll("button")
            .First(b => b.TextContent.Contains("Pasife Al"));

        Assert.False(dugme.HasAttribute("disabled"));
    }

    [Fact]
    public void BAKIYELI_kasa_pasife_ALINAMAZ()
    {
        // İçinde 15.000 TL varken kapatılan kasa, parayı defterden kaybeder.
        var e = Ciz();
        var kart = Kart(e, "Dolu Kasa");
        var dugme = kart.QuerySelectorAll("button").First(b => b.TextContent.Contains("Pasife Al"));

        Assert.True(dugme.HasAttribute("disabled"));
        Assert.Contains("Bakiyesi sıfır olmayan kasa", kart.TextContent);
    }

    [Fact]
    public void Onaylaninca_DELETE_dogru_kasaya_gonderilir()
    {
        var e = Ciz();
        Kart(e, "Boş Kasa").QuerySelectorAll("button")
            .First(b => b.TextContent.Contains("Pasife Al")).Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "DELETE");

        e.FindAll("button").Where(b => b.TextContent.Contains("Pasife Al")).Last().Click();

        Assert.Contains(istekler, i => i.Metot == "DELETE" && i.Yol.EndsWith("/kasalar/k1"));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// GÜN SONU KAPANIŞI — EKRAN
//
// Sayım alanı BOŞ başlar. Defter bakiyesiyle doldurulsaydı kullanıcı saymadan
// Kaydet'e basar, fark hiç görünmez ve kapanış bir tutanak değil bir tıklama
// olurdu.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GunKapanisiEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string Kasalar = """
        [{"id":"k1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":12500.75,"aktif":true}]
        """;

    public GunKapanisiEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(
            new KullaniciErisimTestleri.SahteApi(istekler, Kasalar))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        // PAKET S3-6: /kasalar özet kartlarını ProAlan'a aldı (iki yüz, tek gövde);
        // ProAlan mod servisini ister. Varsayılan mod PROFESYONEL → DOM bugünküyle aynı.
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Kasalar> Ac()
    {
        var e = Render<Kuyumcu.Web.Pages.Kasalar>();
        e.Find("#kasa-gun-kapat-k1").Click();
        istekler.Clear();
        return e;
    }

    private static AngleSharp.Dom.IElement Onayla(
        IRenderedComponent<Kuyumcu.Web.Pages.Kasalar> e) => e.Find("#gun-kapat-onayla");

    [Fact]
    public void Her_aktif_kasada_GUN_KAPAT_dugmesi_vardir()
    {
        var e = Render<Kuyumcu.Web.Pages.Kasalar>();
        Assert.Single(e.FindAll("#kasa-gun-kapat-k1"));
    }

    [Fact]
    public void Sayim_alani_BOS_baslar_defterle_doldurulmaz()
    {
        var e = Ac();
        // PARA-DEMO-2 (madde 2): sayım alanı BOŞ başlar (0 da yazılmaz — açılışta sahte fark yok).
        Assert.Equal("", e.Find("#gun-sayilan").GetAttribute("value") ?? "");
    }

    [Fact]
    public void Fark_varsa_GEREKCE_alani_acilir_ve_kaydet_KAPALI()
    {
        var e = Ac();
        e.Find("#gun-sayilan").Input("12000");   // defter 12.500,75 → fark −500,75

        Assert.Single(e.FindAll("#gun-aciklama"));
        Assert.True(Onayla(e).HasAttribute("disabled"));
        Assert.Contains("gerekçe zorunludur", e.Markup);
    }

    [Fact]
    public void Gerekce_girilince_kaydet_ACILIR()
    {
        var e = Ac();
        e.Find("#gun-sayilan").Input("12000");
        e.Find("#gun-aciklama").Input("Akşam sayımında eksik çıktı");

        Assert.False(Onayla(e).HasAttribute("disabled"));
    }

    [Fact]
    public void Sayim_defterle_tutarsa_gerekce_ISTENMEZ()
    {
        var e = Ac();
        e.Find("#gun-sayilan").Input("12500.75");

        Assert.Empty(e.FindAll("#gun-aciklama"));
        Assert.False(Onayla(e).HasAttribute("disabled"));
    }

    [Fact]
    public void Kapanis_dogru_uca_SAYILAN_tutarla_gonderilir()
    {
        var e = Ac();
        e.Find("#gun-sayilan").Input("12000");
        e.Find("#gun-aciklama").Input("Akşam sayımında eksik çıktı");
        Onayla(e).Click();

        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Yol.EndsWith("/kasalar/k1/gun-kapanisi")));
        var istek = istekler.First(i => i.Metot == "POST");
        Assert.Equal("12000", Alan(istek.Govde, "sayilanNakit"));
        Assert.Equal("Akşam sayımında eksik çıktı", Alan(istek.Govde, "aciklama"));
    }
}
