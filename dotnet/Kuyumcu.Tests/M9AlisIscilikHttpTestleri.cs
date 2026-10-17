using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// M9 — alış işçilik belgesi (müşteri 16.10: "aradaki masrafı fatura etmen lazım, yoksa kâr görünür").
// Toptancıdan has bedelinin %3 üstüne alınan ürün: fark işçiliktir; belgesi yoksa uyarı sayılır.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class M9AlisIscilikHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "M9-" + ad);
    }

    private (decimal HasBedeli, decimal Birim) Fiyat(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var kur = db.KurSnapshots.AsNoTracking().Single(x => x.Id == k.KurId).AltinHasFiyati;
        var hasBedeli = Math.Round(k.UrunHasGrami * kur, 2, MidpointRounding.AwayFromZero);
        return (hasBedeli, Math.Round(hasBedeli * 1.03m, 2, MidpointRounding.AwayFromZero));
    }

    private async Task<string> Alis(Kiraci k, decimal birim)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/islemler/alis", new
        {
            cariId = k.CariId, urunId = k.UrunId, adet = 1m, birimFiyatTl = birim, odemeTuru = "HAVALE",
            kasaId = k.KasaId, kurSnapshotId = k.KurId,
        });
        Assert.True(c.IsSuccessStatusCode, await c.Content.ReadAsStringAsync());
        using var db = f.Vt.OlusturSistem();
        return db.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId && h.Tur == "ALIS")
            .OrderByDescending(h => h.Olusturuldu).Select(h => h.Id).First();
    }

    private async Task<JsonElement> Liste(Kiraci k) =>
        await (await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/alis-iscilik")).Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Iscilikli_alis_belgesiz_sayilir_fatura_girilince_uyaridan_cikar()
    {
        var k = YeniKiraci("fatura");
        var (hasBedeli, birim) = Fiyat(k);
        var alis = await Alis(k, birim);

        var l1 = await Liste(k);
        Assert.Equal(1, l1.GetProperty("belgesizSayisi").GetInt32());
        var satir = l1.GetProperty("satirlar").EnumerateArray().Single();
        Assert.Equal(alis, satir.GetProperty("hareketId").GetString());
        Assert.Equal(birim - hasBedeli, satir.GetProperty("iscilikTahminiTl").GetDecimal());
        Assert.Equal(JsonValueKind.Null, satir.GetProperty("belge").ValueKind);

        var p = await f.Ortam.Istemci(k, "MUHASEBE").PutAsJsonAsync($"/api/v1/alis-iscilik/{alis}", new
        { belgeTuru = "FATURA", belgeNo = "ABC2026000000123", belgeTarihi = DateOnly.FromDateTime(DateTime.Today), iscilikTl = birim - hasBedeli });
        Assert.True(p.IsSuccessStatusCode, await p.Content.ReadAsStringAsync());

        var l2 = await Liste(k);
        Assert.Equal(0, l2.GetProperty("belgesizSayisi").GetInt32());
        Assert.Equal("FATURA", l2.GetProperty("satirlar")[0].GetProperty("belge").GetProperty("belgeTuru").GetString());
    }

    [Fact]
    public async Task Belgesiz_beyan_kaydedilir_ama_uyari_SURER()
    {
        var k = YeniKiraci("belgesiz");
        var (hasBedeli, birim) = Fiyat(k);
        var alis = await Alis(k, birim);
        var p = await f.Ortam.Istemci(k, "SAHIP").PutAsJsonAsync($"/api/v1/alis-iscilik/{alis}", new
        { belgeTuru = "BELGESIZ", iscilikTl = birim - hasBedeli });
        Assert.True(p.IsSuccessStatusCode, await p.Content.ReadAsStringAsync());
        Assert.Equal(1, (await Liste(k)).GetProperty("belgesizSayisi").GetInt32());
    }

    [Fact]
    public async Task Gecersiz_girdiler_400_ve_tezgahtar_403()
    {
        var k = YeniKiraci("ret");
        var (_, birim) = Fiyat(k);
        var alis = await Alis(k, birim);
        var s = f.Ortam.Istemci(k, "SAHIP");
        async Task<string?> Alan(object govde)
        {
            var r = await s.PutAsJsonAsync($"/api/v1/alis-iscilik/{alis}", govde);
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            return await r.Content.ReadAsStringAsync();
        }
        Assert.Contains("belgeNo", await Alan(new { belgeTuru = "FATURA", belgeTarihi = DateOnly.FromDateTime(DateTime.Today), iscilikTl = 10m }));
        Assert.Contains("belgeTarihi", await Alan(new { belgeTuru = "FATURA", belgeNo = "X1", iscilikTl = 10m }));
        Assert.Contains("belgeTarihi", await Alan(new { belgeTuru = "FATURA", belgeNo = "X1", belgeTarihi = DateOnly.FromDateTime(DateTime.Today.AddDays(3)), iscilikTl = 10m }));
        Assert.Contains("iscilikTl", await Alan(new { belgeTuru = "BELGESIZ", iscilikTl = birim + 1m }));
        Assert.Contains("iscilikTl", await Alan(new { belgeTuru = "BELGESIZ", iscilikTl = 0m }));
        await Alan(new { belgeTuru = "MAKBUZ", iscilikTl = 10m });

        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/alis-iscilik")).StatusCode);
        var yok = await s.PutAsJsonAsync("/api/v1/alis-iscilik/01ZZZZZZZZZZZZZZZZZZZZZZZZ", new { belgeTuru = "BELGESIZ", iscilikTl = 1m });
        Assert.Equal(HttpStatusCode.NotFound, yok.StatusCode);
    }

    [Fact]
    public async Task Has_bedeline_esit_ya_da_altindaki_alis_listeye_GIRMEZ()
    {
        var k = YeniKiraci("iscsiz");
        var (hasBedeli, _) = Fiyat(k);
        await Alis(k, hasBedeli);
        Assert.Empty((await Liste(k)).GetProperty("satirlar").EnumerateArray());
    }
}
