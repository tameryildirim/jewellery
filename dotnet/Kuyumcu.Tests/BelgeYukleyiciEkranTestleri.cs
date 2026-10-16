using Bunit;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// BELGE YÜKLEYİCİ EKRANI — paket 4 Faz A [bUnit]
//
// "Düğme var" yetmez: hangi uca, hangi gövdeyle gidildiği kontrol edilir.
// ⚠ Depoda İLK bUnit InputFile testi — UploadFiles/InputFileContent API'si.
// ⚠ Her olay sonrası bekleme WaitForAssertion ile (sabit uyku YASAK).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class BelgeYukleyiciEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string Liste = """
        [{"id":"b1","ad":"GIA-77.pdf","tur":"SERTIFIKA","mime":"application/pdf",
          "boyut":2048,"sha256":"aa11","olusturuldu":"2026-09-16T08:00:00Z"}]
        """;

    public BelgeYukleyiciEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            if (yol.Contains("/icerik"))
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent("%PDF-1.7"u8.ToArray()),
                };

            var g = yol switch
            {
                var y when y.Contains("/belgeler?") => Liste,
                _ => """{"id":"b9","ad":"yeni.pdf","tur":"SERTIFIKA","mime":"application/pdf","boyut":10,"sha256":"bb","aktif":true}""",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<BelgeYukleyici> Ciz()
    {
        var e = Render<BelgeYukleyici>(p => p
            .Add(x => x.SahipTuru, "URUN")
            .Add(x => x.SahipId, "u1"));
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll(".dosya-satiri")));
        istekler.Clear();
        return e;
    }

    private static InputFileContent Dosya(string ad, int boyut)
    {
        var b = new byte[boyut];
        b[0] = 0xFF; b[1] = 0xD8; b[2] = 0xFF;
        return InputFileContent.CreateFromBinary(b, ad);
    }

    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    // ── Kapı ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Dosya_secilmeden_YUKLE_kapali_secilince_ACIK()
    {
        var e = Ciz();
        Assert.True(e.Find("#belge-yukle").HasAttribute("disabled"));

        e.FindComponent<InputFile>().UploadFiles(Dosya("vitrin.jpg", 1024));

        e.WaitForAssertion(() => Assert.False(e.Find("#belge-yukle").HasAttribute("disabled")));
    }

    [Fact]
    public void Boyutu_asan_dosyada_istek_GONDERILMEZ_ve_sebep_ekranda_YAZAR()
    {
        var e = Ciz();
        e.FindComponent<InputFile>().UploadFiles(Dosya("kocaman.jpg", 2 * 1024 * 1024 + 1));

        e.WaitForAssertion(() => Assert.Contains("2 MB", e.Find(".bant-hata").TextContent));
        Assert.True(e.Find("#belge-yukle").HasAttribute("disabled"));
        Assert.Empty(istekler);
    }

    [Fact]
    public void Kabul_edilmeyen_uzantida_istek_GONDERILMEZ()
    {
        var e = Ciz();
        e.FindComponent<InputFile>().UploadFiles(Dosya("zararli.svg", 512));

        e.WaitForAssertion(() => Assert.Contains("PDF", e.Find(".bant-hata").TextContent));
        Assert.Empty(istekler);
    }

    // ── Uçlar ────────────────────────────────────────────────────────────────

    [Fact]
    public void Yukleme_POST_belgeler_ucuna_gider_ve_govde_sahip_alanlarini_TASIR()
    {
        var e = Ciz();
        e.FindComponent<InputFile>().UploadFiles(Dosya("vitrin.jpg", 1024));
        e.WaitForAssertion(() => Assert.False(e.Find("#belge-yukle").HasAttribute("disabled")));

        e.Find("#belge-yukle").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "POST"));
        var istek = istekler.First(i => i.Metot == "POST");
        Assert.EndsWith("/api/v1/belgeler", istek.Yol);
        Assert.Equal("URUN", Alan(istek.Govde, "sahipTuru"));
        Assert.Equal("u1", Alan(istek.Govde, "sahipId"));
        Assert.Equal("SERTIFIKA", Alan(istek.Govde, "tur"));
        Assert.Equal("vitrin.jpg", Alan(istek.Govde, "ad"));
        Assert.False(string.IsNullOrWhiteSpace(Alan(istek.Govde, "icerikBase64")));
    }

    [Fact]
    public void Indir_dugmesi_icerik_ucunu_CAGIRIR()
    {
        var e = Ciz();
        e.FindAll("button").First(b => b.TextContent.Contains("İndir")).Click();

        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/belgeler/b1/icerik")));
    }

    [Fact]
    public void Pasife_al_DELETE_gonderir()
    {
        var e = Ciz();
        e.FindAll("button").First(b => b.TextContent.Contains("Pasife al")).Click();

        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Metot == "DELETE" && i.Yol.EndsWith("/belgeler/b1")));
    }

    [Fact]
    public void Bos_durum_metni_belge_yokken_GORUNUR()
    {
        var e = Render<BelgeYukleyici>(p => p
            .Add(x => x.SahipTuru, "URUN")
            .Add(x => x.SahipId, ""));
        e.WaitForAssertion(() => Assert.Contains("belge eklenmedi", e.Find(".bos-durum").TextContent));
    }
}
