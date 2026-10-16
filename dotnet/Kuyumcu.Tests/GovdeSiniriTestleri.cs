using Kuyumcu.Api.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// GÖVDE SINIRI → 413 (paket 4, Mehmet K4-1)
//
// ⚠ NEDEN HTTP TESTİ DEĞİL: [RequestSizeLimit] sınırı KESTREL'de uygulanır;
// WebApplicationFactory'nin TestServer'ında gövde sınırı devrede DEĞİLDİR
// (ölçüldü: 8,4 MB gövdeli istek 413 değil, uç kapısından 400 alıyor).
// Bu yüzden eşleme middleware düzeyinde sınanır; 413'ün canlıda gerçekten
// döndüğü 5198 üzerinde curl ile ayrıca kanıtlandı (uygulama raporu).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GovdeSiniriTestleri
{
    private static async Task<(int Kod, string Govde)> Kos(Exception firlat)
    {
        var ctx = new DefaultHttpContext();
        var akis = new MemoryStream();
        ctx.Response.Body = akis;

        var m = new HataYakalayici(_ => throw firlat, NullLogger<HataYakalayici>.Instance);
        await m.InvokeAsync(ctx);

        akis.Position = 0;
        return (ctx.Response.StatusCode, await new StreamReader(akis).ReadToEndAsync());
    }

    [Fact]
    public async Task Govde_siniri_asilinca_413_doner_500_DEGIL()
    {
        var (kod, govde) = await Kos(new BadHttpRequestException("too large", 413));
        Assert.Equal(413, kod);
        Assert.Contains("Payload Too Large", govde);
        Assert.DoesNotContain("Internal server error", govde);
    }

    [Fact]
    public async Task Bozuk_govde_400_doner()
    {
        var (kod, govde) = await Kos(new BadHttpRequestException("bozuk", 400));
        Assert.Equal(400, kod);
        Assert.DoesNotContain("Internal server error", govde);
    }

    [Fact]
    public async Task Beklenmeyen_hata_HALA_500_doner()
    {
        var (kod, govde) = await Kos(new InvalidOperationException("patladı"));
        Assert.Equal(500, kod);
        Assert.Contains("Internal server error", govde);
        Assert.DoesNotContain("patladı", govde);   // iç ayrıntı sızmaz
    }
}
