using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Modules.Yetki;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-2 — ALTIN POZİSYONU UÇLARI: GERÇEK HTTP BORU HATTI
//
// Servis testi [RolGerekli]/[ModulYetkisi]/[Authorize] niteliklerini GÖRMEZ;
// bu dosya kapıyı üretimdeki boru hattında ölçer. Yanıt gövdesi AYRIŞTIRILIP
// camelCase sözleşmesi ve sayılar sınanır (yalnız durum koduna bakılmaz).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class AltinPozisyonuHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    [Fact]
    public async Task B2H_01_jetonsuz_istek_401()
    {
        var c = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/pozisyon");
        Assert.Equal(HttpStatusCode.Unauthorized, c.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/pozisyon")]
    [InlineData("/api/v1/pozisyon/cariler")]
    public async Task B2H_02_tezgahtar_403_alir(string yol)
    {
        var k = YeniKiraci("PAKETB2-TEST-rol");
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync(yol);
        Assert.Equal(HttpStatusCode.Forbidden, c.StatusCode);
    }

    [Fact]
    public async Task B2H_03_sahip_200_ve_yanit_sekli_camelCase()
    {
        var k = YeniKiraci("PAKETB2-TEST-sekil");
        var c = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/pozisyon");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);

        var j = await c.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ALTIN", j.GetProperty("anaMaden").GetString());
        var a = j.GetProperty("altin");
        Assert.Equal(916m, a.GetProperty("vitrinGram").GetDecimal());
        Assert.Equal(916m, a.GetProperty("netGram").GetDecimal());
        Assert.Equal(0m, a.GetProperty("cariAlacakGram").GetDecimal());
        Assert.True(j.GetProperty("donemdenBagimsiz").GetBoolean());
        Assert.Equal(0, j.GetProperty("digerMadenler").GetArrayLength());
        // Birleşik "toplam has" alanı YOK (K20-2): yanıtta böyle bir anahtar olamaz.
        Assert.False(j.TryGetProperty("toplamHas", out _));
    }

    [Fact]
    public async Task B2H_04_kirilim_400_ve_200()
    {
        var k = YeniKiraci("PAKETB2-TEST-kirilim");
        var ist = f.Ortam.Istemci(k, "SAHIP");

        var kotu = await ist.GetAsync("/api/v1/pozisyon/cariler?maden=BAKIR");
        Assert.Equal(HttpStatusCode.BadRequest, kotu.StatusCode);
        var kotuGovde = await kotu.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("maden", kotuGovde.GetProperty("message").GetString()!, StringComparison.OrdinalIgnoreCase);

        var iyi = await ist.GetAsync("/api/v1/pozisyon/cariler?maden=ALTIN&yon=BORC&limit=10");
        Assert.Equal(HttpStatusCode.OK, iyi.StatusCode);
        var j = await iyi.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("BORC", j.GetProperty("yon").GetString());
        Assert.Equal(10, j.GetProperty("limit").GetInt32());
        Assert.Equal(0, j.GetProperty("toplamSatir").GetInt32());
        Assert.False(j.GetProperty("dahaFazlasiVar").GetBoolean());
    }

    /// <summary>
    /// [RolGerekli] kapısının KENDİ başına ölçümü: TEZGÂHTAR'a "rapor" modülü
    /// yetki matrisinden VERİLİR (ModulYetkisi kapısı AÇILIR — /yetkiler/benim ile
    /// kanıtlanır); pozisyon yine 403 döner, çünkü rol listesi (SAHIP/MUDUR/MUHASEBE)
    /// ayrı ve bağımsız bir kapıdır. B2H_02 bunu ölçemez: orada 403 zaten modül
    /// kapısından gelir (doğrulayıcı bulgusu M-B2-9).
    /// </summary>
    [Theory]
    [InlineData("/api/v1/pozisyon")]
    [InlineData("/api/v1/pozisyon/cariler")]
    public async Task B2H_05_rapor_modulu_verilmis_tezgahtar_rol_kapisindan_403_alir(string yol)
    {
        var k = YeniKiraci("PAKETB2-TEST-rolkapisi");
        var sahip = f.Ortam.Istemci(k, "SAHIP");
        var matris = new Dictionary<string, string[]>
        {
            ["MUDUR"] = Moduller.Varsayilan["MUDUR"],
            ["MUHASEBE"] = Moduller.Varsayilan["MUHASEBE"],
            ["TEZGAHTAR"] = [.. Moduller.Varsayilan["TEZGAHTAR"].Append("rapor").Distinct()],
        };
        var kaydet = await sahip.PutAsJsonAsync("/api/v1/yetkiler", new { matris });
        Assert.Equal(HttpStatusCode.OK, kaydet.StatusCode);

        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");
        // Modül kapısı gerçekten AÇIK: 403'ün kaynağı ModulYetkisi olamaz.
        var benim = await (await tezgahtar.GetAsync("/api/v1/yetkiler/benim")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("rapor", benim.GetProperty("moduller").EnumerateArray().Select(x => x.GetString()));

        var c = await tezgahtar.GetAsync(yol);
        Assert.Equal(HttpStatusCode.Forbidden, c.StatusCode);

        // Aynı modül matrisinde MUHASEBE (listede) geçer — kapı role göre, topyekûn kapanmadı.
        var muhasebe = await f.Ortam.Istemci(k, "MUHASEBE").GetAsync(yol);
        Assert.Equal(HttpStatusCode.OK, muhasebe.StatusCode);
    }
}
