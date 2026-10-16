using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// M6 — faturalı satış (müşteri 16.10: "fişe hiç gerek yok… sisteme girdiğim anda fatura
// kessin"). POST /islemler/satis-fisi { belgeTuru: "FATURA" } → satış + fişten kuyum faturası
// AYNI istekte. SAHIP/MUDUR/MUHASEBE → KESILDI (numaralı); TEZGAHTAR → TASLAK.
// Peşin Müşteri'ye fatura KESİLMEZ (satıştan ÖNCE 400, hareket yazılmaz).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class M6FaturaliSatisHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "M6-" + ad);
    }

    /// <summary>Satış fiyatı ürünün has değerinin %10 üstü (özel matrah pozitif kalsın).</summary>
    private decimal Fiyat(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var has = db.Urunler.AsNoTracking().Single(u => u.Id == k.UrunId).HasGrami;
        var kur = db.KurSnapshots.AsNoTracking().Single(x => x.Id == k.KurId).AltinHasFiyati;
        return Math.Ceiling(has * kur * 1.10m);
    }

    private object Fis(Kiraci k, string? cariId, string? belge) => new
    {
        cariId,
        kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = Fiyat(k), hediye = false } },
        odemeler = new[] { new { arac = "KART", tutarTl = Fiyat(k), kasaId = k.KasaId } },
        kurSnapshotId = k.KurId,
        fisIskontoYuzde = 0m,
        belgeTuru = belge,
    };

    private async Task<string> PesinId(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/cari/sistem-carisi");
        return (await c.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Sahip_faturali_satista_fatura_KESILDI_ve_numarali_doner()
    {
        var k = YeniKiraci("sahip");
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis(k, k.CariId, "FATURA"));
        var y = await c.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        Assert.True(y.GetProperty("faturaHata").ValueKind == JsonValueKind.Null, y.GetProperty("faturaHata").ToString());
        Assert.False(string.IsNullOrEmpty(y.GetProperty("satis").GetProperty("fisId").GetString()));

        using var db = f.Vt.OlusturSistem();
        var fatura = await db.Faturalar.AsNoTracking().SingleAsync(x => x.TenantId == k.TenantId);
        Assert.Equal("KESILDI", fatura.Durum);
        Assert.False(string.IsNullOrEmpty(fatura.FaturaNo));
        Assert.Equal("KUYUM", fatura.KdvTuru);
        Assert.Equal(Fiyat(k), fatura.Toplam);
        Assert.Equal(k.CariId, fatura.CariId);
    }

    [Fact]
    public async Task Tezgahtar_faturali_satista_fatura_TASLAK_kalir()
    {
        var k = YeniKiraci("tezgahtar");
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis(k, k.CariId, "FATURA"));
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        using var db = f.Vt.OlusturSistem();
        var fatura = await db.Faturalar.AsNoTracking().SingleAsync(x => x.TenantId == k.TenantId);
        Assert.Equal("TASLAK", fatura.Durum);
        Assert.Null(fatura.FaturaNo);
    }

    [Fact]
    public async Task Pesin_musteriye_fatura_satistan_ONCE_400_hareket_yazilmaz()
    {
        var k = YeniKiraci("pesin");
        var pesin = await PesinId(k);
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis(k, pesin, "FATURA"));
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Contains("cariId", await c.Content.ReadAsStringAsync());
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, await db.Hareketler.CountAsync(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
        Assert.Equal(0, await db.Faturalar.CountAsync(x => x.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Fis_secilince_yanit_ESKISIYLE_ayni_fatura_olusmaz()
    {
        var k = YeniKiraci("fis");
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis(k, k.CariId, null));
        var y = await c.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        Assert.True(y.TryGetProperty("fisId", out _));            // düz yanıt — sarılmamış
        Assert.False(y.TryGetProperty("satis", out _));
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, await db.Faturalar.CountAsync(x => x.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Belge_satis_belge_ucu_varsayilani_ve_kuyumcu_haddini_dondurur_SAHIP_yazar()
    {
        var k = YeniKiraci("ayar");
        var g = await (await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/satis-belge")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("FIS", g.GetProperty("varsayilan").GetString());
        var p = await f.Ortam.Istemci(k, "SAHIP").PutAsJsonAsync("/api/v1/satis-belge", new { varsayilan = "FATURA" });
        Assert.Equal(HttpStatusCode.OK, p.StatusCode);
        Assert.Equal("FATURA", (await p.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("varsayilan").GetString());
        var t = await f.Ortam.Istemci(k, "TEZGAHTAR").PutAsJsonAsync("/api/v1/satis-belge", new { varsayilan = "FIS" });
        Assert.Equal(HttpStatusCode.Forbidden, t.StatusCode);
        var x = await f.Ortam.Istemci(k, "SAHIP").PutAsJsonAsync("/api/v1/satis-belge", new { varsayilan = "MAKBUZ" });
        Assert.Equal(HttpStatusCode.BadRequest, x.StatusCode);
    }
}
