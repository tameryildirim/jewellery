using System.Net;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// DÖVİZ DEFTERİ — HTTP KAPISI (paket 27 Faz A).
//
// NEDEN SERVİS TESTİ YETMEZ: [RolGerekli] bir DENETLEYİCİ niteliğidir; servis
// testi onu hiç görmez. Döviz VERESİYE'nin tezgâhtara kapalı, döviz
// TAHSİLAT'ın açık olduğu ancak gerçek HTTP isteğiyle kanıtlanır. Ayrıca
// yanıtın camelCase alan adları burada dondurulur (istemci sözleşmesi).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class HttpCariDovizTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad, decimal? gbp = null)
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, ad);
        TestKurulum.KurDoviz(db, k.KurId, usd: 40m, eur: 44m, gbp: gbp);
        return k;
    }

    private string UsdKasa(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KasaEkle(db, k.TenantId, k.SubeId, "USD");
    }

    // ── Rol kapıları ────────────────────────────────────────────────────────

    [Fact]
    public async Task TEZGAHTAR_doviz_veresiye_yazamaz_403()
    {
        var k = YeniKiraci("HTTP döviz veresiye tezgâhtar");
        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson(
            $"/api/v1/cari/{k.CariId}/doviz-veresiye",
            new { paraBirimi = "USD", tutar = 100m, kurSnapshotId = k.KurId });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);

        // Kapı GERÇEKTEN kapandı: deftere hiçbir şey yazılmadı.
        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.Hareketler.AsNoTracking()
            .AnyAsync(h => h.TenantId == k.TenantId && h.Tur == "DOVIZ_VERESIYE"));
    }

    /// <summary>Döviz tahsilat mevcut tahsilatla AYNI rol yüzeyindedir.</summary>
    [Fact]
    public async Task TEZGAHTAR_doviz_tahsilat_yapabilir_200()
    {
        var k = YeniKiraci("HTTP döviz tahsilat tezgâhtar");
        var kasa = UsdKasa(k);

        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson(
            $"/api/v1/cari/{k.CariId}/doviz-tahsilat",
            new { paraBirimi = "USD", tutar = 100m, kasaId = kasa, kurSnapshotId = k.KurId });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        var h = await db.Hareketler.AsNoTracking()
            .SingleAsync(x => x.TenantId == k.TenantId && x.Tur == "DOVIZ_TAHSILAT");
        Assert.Equal("USD", h.ParaBirimi);
        Assert.Equal(-100m, h.DovizTutar);
        Assert.Equal(k.KullaniciId, h.KullaniciId);
    }

    [Fact]
    public async Task Kimliksiz_istek_401()
    {
        var k = YeniKiraci("HTTP döviz kimliksiz");
        var cevap = await f.Ortam.AnonimIstemci().PostJson(
            $"/api/v1/cari/{k.CariId}/doviz-veresiye",
            new { paraBirimi = "USD", tutar = 100m, kurSnapshotId = k.KurId });

        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    // ── Sözleşme: camelCase döviz alanları ──────────────────────────────────

    /// <summary>
    /// Bakiye ucu üç döviz alanını DÖNER ve sıfır bakiyede <c>0</c> verir —
    /// <c>null</c> DEĞİL. İstemci "alan yok" ile "bakiye yok"u ayırmak zorunda
    /// kalmasın (S-2 sözleşme dondurma).
    /// </summary>
    [Fact]
    public async Task Bakiye_ucu_uc_doviz_alanini_camelCase_dondurur()
    {
        var k = YeniKiraci("HTTP döviz bakiye");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var bos = await (await istemci.GetAsync($"/api/v1/cari/{k.CariId}/bakiye")).Json();
        Assert.Equal(0m, bos.GetProperty("usdBakiye").GetDecimal());
        Assert.Equal(0m, bos.GetProperty("eurBakiye").GetDecimal());
        Assert.Equal(0m, bos.GetProperty("gbpBakiye").GetDecimal());

        var yazma = await istemci.PostJson(
            $"/api/v1/cari/{k.CariId}/doviz-veresiye",
            new { paraBirimi = "EUR", tutar = 250m, kurSnapshotId = k.KurId });
        Assert.Equal(HttpStatusCode.OK, yazma.StatusCode);

        var dolu = await (await istemci.GetAsync($"/api/v1/cari/{k.CariId}/bakiye")).Json();
        Assert.Equal(250m, dolu.GetProperty("eurBakiye").GetDecimal());
        Assert.Equal(0m, dolu.GetProperty("usdBakiye").GetDecimal());
        Assert.Equal(0m, dolu.GetProperty("tlBakiye").GetDecimal());   // TL kirlenmedi
    }

    [Fact]
    public async Task Liste_ucu_doviz_alanlarini_dondurur_ve_sifirda_0_verir()
    {
        var k = YeniKiraci("HTTP döviz liste");
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        await istemci.PostJson($"/api/v1/cari/{k.CariId}/doviz-veresiye",
            new { paraBirimi = "USD", tutar = 75m, kurSnapshotId = k.KurId });

        var liste = await (await istemci.GetAsync("/api/v1/cari")).Json();
        var satir = liste.EnumerateArray().Single(e => e.GetProperty("id").GetString() == k.CariId);
        Assert.Equal(75m, satir.GetProperty("usdBakiye").GetDecimal());
        Assert.Equal(0m, satir.GetProperty("eurBakiye").GetDecimal());
        Assert.Equal(0m, satir.GetProperty("gbpBakiye").GetDecimal());
    }

    // ── Doğrulama biçimi ────────────────────────────────────────────────────

    /// <summary>
    /// Küme dışı para birimi model bağlamada takılır: 400 ve mevcut hata
    /// biçimi KORUNUR (DTO RegularExpression yüzeyi bozulmamış).
    /// </summary>
    [Theory]
    [InlineData("CHF")]
    [InlineData("TRY")]
    [InlineData("usd")]
    public async Task Gecersiz_para_birimi_400_verir(string pb)
    {
        var k = YeniKiraci($"HTTP geçersiz PB {pb}");
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/cari/{k.CariId}/doviz-veresiye",
            new { paraBirimi = pb, tutar = 100m, kurSnapshotId = k.KurId });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.Hareketler.AsNoTracking()
            .AnyAsync(h => h.TenantId == k.TenantId && h.Tur == "DOVIZ_VERESIYE"));
    }

    /// <summary>Kuru olmayan para biriminde 400 ve gövdede "kur" geçer.</summary>
    [Fact]
    public async Task Kuru_olmayan_para_biriminde_400_ve_mesajda_kur_gecer()
    {
        var k = YeniKiraci("HTTP GBP kur yok");      // gbp = null
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/cari/{k.CariId}/doviz-veresiye",
            new { paraBirimi = "GBP", tutar = 100m, kurSnapshotId = k.KurId });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        var govde = await cevap.Content.ReadAsStringAsync();
        Assert.Contains("kur", govde, StringComparison.OrdinalIgnoreCase);

        using var db = f.Vt.OlusturSistem();
        Assert.Empty(await db.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId).ToListAsync());
    }

    /// <summary>
    /// ⚠ TL karşılığı İSTEMCİDEN GELMEZ: gövdeye "tlTutari" konsa bile sunucu
    /// kendi kuruyla hesaplar; gönderilen değer hiçbir yere yazılmaz (M86).
    /// </summary>
    [Fact]
    public async Task Istemcinin_gonderdigi_TL_degeri_YOK_SAYILIR_sunucu_hesaplar()
    {
        var k = YeniKiraci("HTTP TL sunucuda");
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/cari/{k.CariId}/doviz-veresiye",
            new { paraBirimi = "USD", tutar = 100m, tlTutari = 1m, tlKarsiligi = 1m, kurSnapshotId = k.KurId });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        var h = await db.Hareketler.AsNoTracking()
            .SingleAsync(x => x.TenantId == k.TenantId && x.Tur == "DOVIZ_VERESIYE");
        Assert.Equal(4_000m, h.TlTutari);     // 100 × 40 — istemcinin 1'i DEĞİL
    }
}
