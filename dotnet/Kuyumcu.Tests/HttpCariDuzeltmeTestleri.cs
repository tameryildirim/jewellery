using System.Net;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// MANUEL DÜZELTME FİŞİ — HTTP KAPISI (paket 33).
//
// NEDEN SERVİS TESTİ YETMEZ: [RolGerekli] bir DENETLEYİCİ niteliğidir; servis
// testi onu hiç görmez. Elle bakiye müdahalesinin TEZGÂHTAR'a kapalı olduğu
// ancak gerçek HTTP isteğiyle kanıtlanabilir (ekran gizlemesi kapı değildir).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class HttpCariDuzeltmeTestleri(HttpFikstur f)
{
    private const string Gerekce = "Eski sistemden devir farkı — mutabakat 2026-09";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static object Govde(decimal tl, decimal has, string gerekce, Kiraci k)
        => new { tlDelta = tl, hasDelta = has, gerekce, kurSnapshotId = k.KurId };

    [Fact]
    public async Task TEZGAHTAR_duzeltme_fisi_kesemez_403()
    {
        var k = YeniKiraci("HTTP düzeltme tezgâhtar");
        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR")
            .PostJson($"/api/v1/cari/{k.CariId}/duzeltme", Govde(1_250m, 0m, Gerekce, k));

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);

        // Kapı GERÇEKTEN kapandı: deftere hiçbir şey yazılmadı.
        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.Hareketler.AsNoTracking()
            .AnyAsync(h => h.TenantId == k.TenantId && h.Tur == "CARI_DUZELTME"));
    }

    [Fact]
    public async Task SAHIP_duzeltme_fisi_keser_200_ve_bakiye_doner()
    {
        var k = YeniKiraci("HTTP düzeltme sahip");
        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson($"/api/v1/cari/{k.CariId}/duzeltme", Govde(1_250m, -0.5m, Gerekce, k));

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        var json = await cevap.Json();
        var bakiye = json.GetProperty("bakiye");
        Assert.Equal(1_250m, bakiye.GetProperty("tlBakiye").GetDecimal());
        Assert.Equal(-0.5m, bakiye.GetProperty("hasGramBakiye").GetDecimal());

        using var db = f.Vt.OlusturSistem();
        var h = await db.Hareketler.AsNoTracking()
            .SingleAsync(x => x.TenantId == k.TenantId && x.Tur == "CARI_DUZELTME");
        Assert.Contains(Gerekce, h.Aciklama);
        Assert.Equal(k.KullaniciId, h.KullaniciId);   // "kim yaptı" HTTP yolunda dolu
    }

    [Fact]
    public async Task MUHASEBE_duzeltme_fisi_kesebilir_ama_KISA_GEREKCE_400_alir()
    {
        var k = YeniKiraci("HTTP düzeltme muhasebe");
        var cevap = await f.Ortam.Istemci(k, "MUHASEBE")
            .PostJson($"/api/v1/cari/{k.CariId}/duzeltme", Govde(1_250m, 0m, "kısa", k));

        // Rol geçti (403 DEĞİL), gerekçe kapısına takıldı.
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }
}
