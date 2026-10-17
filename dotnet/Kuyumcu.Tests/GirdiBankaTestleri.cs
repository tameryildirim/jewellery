using System.Net;
using Bunit;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// olumsuz-fikstür: dosya — bu dosya kuralın SINIRLARINI sınar; geçersiz değerler bilinçlidir
// (denetim.sh TEST_GECERSIZ_FIKSTUR bu dosyayı saymaz).
// ═════════════════════════════════════════════════════════════════════════════
// PAKET GIRDI-AILESI-2 — kabul örneği: /banka hesap formu ortak <Iban> ile.
// Ekran: 26. karakterde mod-97 hatası alan altında + Kaydet kilitli + sebep AYNI metin.
// Sunucu: [Iban] (Core IbanDogrulayici) aynı kuralla 400 + AYNI sözlük metni.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GirdiBankaEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public GirdiBankaEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(istekler)) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
    }

    private sealed class Sahte(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));
            var y = yol switch
            {
                var v when v.Contains("/bankalar/") && v.Contains("/hareketler") => """{"hareketler":[]}""",
                var v when v.EndsWith("/bankalar") && istek.Method == HttpMethod.Post =>
                    """{"id":"b9","bankaAdi":"QA","hesapAdi":"Vadesiz","iban":"TR330006100519786457841326","paraBirimi":"TRY","bakiye":0,"aktif":true}""",
                var v when v.Contains("/bankalar") && istek.Method == HttpMethod.Get =>
                    """{"hesaplar":[{"id":"b1","bankaAdi":"Ziraat","hesapAdi":"Ana Hesap","hesapSahibi":null,"iban":"TR200000000000000000000001","paraBirimi":"TRY","bakiye":0,"aktif":true}]}""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Banka> YeniHesap()
    {
        var e = Render<Kuyumcu.Web.Pages.Banka>().AyrintiAc();
        e.WaitForElement("#banka-yeni");
        e.Find("#banka-yeni").Click();
        e.WaitForElement("#hesap-iban");
        e.Find("#hesap-banka").Input("QA Banka");
        e.Find("#hesap-ad").Input("Vadesiz");
        return e;
    }

    [Fact]
    public void Banka_IBAN_mod97_bozuk_Kaydet_kilitli_sebep_alan_alti_metniyle_ayni_istek_yok()
    {
        var e = YeniHesap();
        e.Find("#hesap-iban").Input("TR340006100519786457841326");
        e.WaitForAssertion(() =>
        {
            var alanAlti = e.Find("#alan-hata-hesap-iban").TextContent.Trim();
            Assert.Equal(GirdiSozlugu.Tr.IbanKontrol, alanAlti);
            Assert.True(e.Find("#hesap-kaydet").HasAttribute("disabled"));
            Assert.Equal(alanAlti, e.Find("#hesap-kilit-sebep").TextContent.Trim());
        });
        Assert.Equal("TR34 0006 1005 1978 6457 8413 26", e.Find("#hesap-iban").GetAttribute("value"));
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }

    [Fact]
    public void Banka_IBAN_gecerli_POST_govdesi_bosluksuz_kilit_kalkar()
    {
        var e = YeniHesap();
        e.Find("#hesap-iban").Input("TR34 0006 1005 1978 6457 8413 26");
        e.WaitForAssertion(() => Assert.True(e.Find("#hesap-kaydet").HasAttribute("disabled")));
        e.Find("#hesap-iban").Input("tr33 0006 1005 1978 6457 8413 26");
        e.WaitForAssertion(() => Assert.False(e.Find("#hesap-kaydet").HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#hesap-kilit-sebep"));
        e.Find("#hesap-kaydet").Click();
        e.WaitForAssertion(() =>
        {
            var post = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/bankalar"));
            var g = System.Text.Json.JsonDocument.Parse(post.Govde).RootElement;
            Assert.Equal("TR330006100519786457841326", g.GetProperty("iban").GetString());
        });
    }

    [Fact]
    public void Banka_duzenleme_cekmecesi_de_ortak_Iban_ve_kilit()
    {
        var e = Render<Kuyumcu.Web.Pages.Banka>().AyrintiAc();
        e.WaitForElement("#bh-duzenle-b1");
        e.Find("#bh-duzenle-b1").Click();
        e.WaitForElement("#d-iban");
        Assert.Equal("TR20 0000 0000 0000 0000 0000 01", e.Find("#d-iban").GetAttribute("value"));
        Assert.Equal("iban", e.Find("#d-iban").GetAttribute("data-maske"));
        e.Find("#d-iban").Input("TR21 0000 0000 0000 0000 0000 01");
        e.WaitForAssertion(() => Assert.Equal(GirdiSozlugu.Tr.IbanKontrol, e.Find("#alan-hata-d-iban").TextContent.Trim()));
        Assert.Contains(GirdiSozlugu.Tr.IbanKontrol, e.Markup.Replace(e.Find("#alan-hata-d-iban").OuterHtml, ""));   // kilit sebebi
    }
}

[Collection("http")]
public sealed class GirdiBankaSunucuTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    [Fact]
    public async Task Banka_POST_mod97_bozuk_IBAN_400_ve_ekranla_ayni_sozluk_metni()
    {
        var k = YeniKiraci("GIRDI2 iban400");
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Ziraat", hesapAdi = "Vadesiz", iban = "TR340006100519786457841326", paraBirimi = "TRY",
        });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        var j = await c.Json();
        Assert.Equal(GirdiSozlugu.Tr.IbanKontrol, j.GetProperty("alanlar").GetProperty("iban")[0].GetString());
        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.BankaHesaplari.AnyAsync(h => h.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Banka_POST_eski_regexin_kabul_ettigi_TR_24_rakam_artik_400()
    {
        var k = YeniKiraci("GIRDI2 eskiregex");
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Ziraat", hesapAdi = "Vadesiz", iban = "TR000000000000000000000001", paraBirimi = "TRY",
        });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
    }

    [Fact]
    public async Task Banka_POST_bosluklu_kucuk_harf_ve_harfli_hesap_no_gecer_normallestirilir()
    {
        var k = YeniKiraci("GIRDI2 normal");
        var harfli = IbanDogrulayici.TrTamamla("00061" + "0" + "ABCDEFGH" + Random.Shared.Next(10_000_000, 99_999_999));
        var bosluklu = "tr33 0006 1005 1978 6457 8413 26";
        foreach (var iban in new[] { bosluklu, harfli })
        {
            var c = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/bankalar", new
            {
                bankaAdi = "Ziraat", hesapAdi = "Vadesiz " + iban[^4..], iban, paraBirimi = "TRY",
            });
            Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        }
        using var db = f.Vt.OlusturSistem();
        var kayitli = await db.BankaHesaplari.Where(h => h.TenantId == k.TenantId).Select(h => h.Iban).ToListAsync();
        Assert.Contains("TR330006100519786457841326", kayitli);
        Assert.Contains(harfli, kayitli);
    }

    [Fact]
    public async Task Banka_PATCH_mod97_bozuk_IBAN_400_eski_deger_korunur()
    {
        var k = YeniKiraci("GIRDI2 patch");
        var iban = TestIban.Uret();
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Ziraat", hesapAdi = "Vadesiz", iban, paraBirimi = "TRY",
        });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var id = (await c.Json()).GetProperty("id").GetString()!;
        var bozuk = iban[..2] + (iban[2] == '9' ? '0' : (char)(iban[2] + 1)) + iban[3..];
        var p = await f.Ortam.Istemci(k, "SAHIP").PatchAsync($"/api/v1/bankalar/{id}",
            System.Net.Http.Json.JsonContent.Create(new { iban = bozuk }));
        Assert.Equal(HttpStatusCode.BadRequest, p.StatusCode);
        Assert.Contains(GirdiSozlugu.Tr.IbanKontrol, await p.Content.ReadAsStringAsync());
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(iban, (await db.BankaHesaplari.SingleAsync(h => h.Id == id)).Iban);
    }

    [Theory]   // doğrulayıcı tur 1: PATCH {iban:'   '} 200 dönüp hiçbir şey yapmıyordu
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t ")]
    public async Task Banka_PATCH_bos_ya_da_yalniz_bosluk_IBAN_400_eski_deger_korunur(string bos)
    {
        var k = YeniKiraci("GIRDI2 patchbos");
        var iban = TestIban.Uret();
        var c = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Ziraat", hesapAdi = "Vadesiz", iban, paraBirimi = "TRY",
        });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var id = (await c.Json()).GetProperty("id").GetString()!;
        var p = await f.Ortam.Istemci(k, "SAHIP").PatchAsync($"/api/v1/bankalar/{id}",
            System.Net.Http.Json.JsonContent.Create(new { iban = bos, hesapAdi = "Degisti" }));
        Assert.Equal(HttpStatusCode.BadRequest, p.StatusCode);
        var j = await p.Json();
        Assert.Equal("IBAN boşaltılamaz", j.GetProperty("alanlar").GetProperty("iban")[0].GetString());
        using var db = f.Vt.OlusturSistem();
        var h = await db.BankaHesaplari.SingleAsync(x => x.Id == id);
        Assert.Equal(iban, h.Iban);
        Assert.Equal("Vadesiz", h.HesapAdi);
        // gövdede yoksa KORU (PATCH sözleşmesi)
        var p2 = await f.Ortam.Istemci(k, "SAHIP").PatchAsync($"/api/v1/bankalar/{id}",
            System.Net.Http.Json.JsonContent.Create(new { hesapAdi = "Degisti" }));
        Assert.Equal(HttpStatusCode.OK, p2.StatusCode);
        using var db2 = f.Vt.OlusturSistem();
        Assert.Equal(iban, (await db2.BankaHesaplari.SingleAsync(x => x.Id == id)).Iban);
    }

    [Fact]
    public async Task Banka_servis_yolu_da_Core_kuralini_cagirir()
    {
        var k = YeniKiraci("GIRDI2 servis");
        await using var db = f.Vt.OlusturSistem();
        var servis = new Kuyumcu.Api.Modules.Banka.BankaServisi(db);
        var dto = new Kuyumcu.Api.Modules.Banka.HesapOlusturDto
        {
            BankaAdi = "Ziraat", HesapAdi = "Vadesiz", Iban = "TR340006100519786457841326", ParaBirimi = "TRY",
        };
        var ex = await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() =>
            servis.HesapOlustur(dto, k.Islem));
        Assert.Equal(GirdiSozlugu.Tr.IbanKontrol, ex.Message);
    }
}
