using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-4 — ALTIN BACAĞI: GERÇEK HTTP BORU HATTI
// Servis testi [Authorize]/[ModulYetkisi]/model doğrulamasını GÖRMEZ; bu dosya
// uçları üretim boru hattında ölçer. Yanıt gövdesi AYRIŞTIRILIR.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class B4AltinBacagiHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return IslemBantFikstur.ReferansKur(db, TestKurulum.KiraciKur(db, "PAKETB4-TEST-" + ad));
    }

    private static async Task<JsonElement> Govde(HttpResponseMessage c) =>
        await c.Content.ReadFromJsonAsync<JsonElement>();

    /// <summary>Gövdedeki TÜM metin değerleri (ProblemDetails'in errors sözlüğü dahil).</summary>
    private static IEnumerable<string> Metinler(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => [e.GetString()!],
        JsonValueKind.Object => e.EnumerateObject().SelectMany(p => Metinler(p.Value)),
        JsonValueKind.Array => e.EnumerateArray().SelectMany(Metinler),
        _ => [],
    };

    [Fact]
    public async Task Tek_Etiketli_ALTIN_OdemeTuru_Artik_Reddedilir_http()
    {
        var k = YeniKiraci("tek etiket http");
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync("/api/v1/islemler/satis", new
        {
            urunId = k.UrunId, adet = 1, birimFiyatTl = 5_000, odemeTuru = "ALTIN",
            kasaId = k.KasaId, kurSnapshotId = k.KurId, cariId = k.CariId,
        });
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        var metinler = Metinler(await Govde(c)).ToList();
        Assert.Contains(metinler, m => m.Contains("'Eski altın' ödeme bacağı"));

        using var kontrol = f.Vt.OlusturSistem();
        Assert.Empty(kontrol.Hareketler.Where(h => h.TenantId == k.TenantId).Select(h => h.Id).ToList());
    }

    [Fact]
    public async Task AltinBacagi_Ayarlar_Ucu_Yapilandirmayi_Doner()
    {
        var k = YeniKiraci("ayarlar");
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/islemler/altin-bacagi/ayarlar");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var j = await Govde(c);
        Assert.Equal(JsonValueKind.Null, j.GetProperty("alisIskontoOran").ValueKind);   // K-KUR-4
        var oranlar = j.GetProperty("stopajOranlari").EnumerateArray().ToList();
        Assert.Equal(0.02m, oranlar.Single(o => o.GetProperty("saticiTipi").GetString() == "ESNAF_MUAF").GetProperty("oran").GetDecimal());
        Assert.Equal(0m, oranlar.Single(o => o.GetProperty("saticiTipi").GetString() == "NIHAI_TUKETICI").GetProperty("oran").GetDecimal());
        Assert.False(oranlar.Single(o => o.GetProperty("saticiTipi").GetString() == "MUKELLEF").GetProperty("pusulaGerekir").GetBoolean());
        Assert.True(j.GetProperty("saticiAdresiZorunlu").GetBoolean());

        var anonim = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/islemler/altin-bacagi/ayarlar");
        Assert.Equal(HttpStatusCode.Unauthorized, anonim.StatusCode);
    }

    [Fact]
    public async Task AltinBacagi_Http_Fis_Iki_Hareket_Ve_Pusula_Doner()
    {
        var k = YeniKiraci("http fis");
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId, kurSnapshotId = k.KurId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1, birimFiyatTl = 20_000 } },
            odemeler = new object[]
            {
                new { arac = "NAKIT", tutarTl = 10_840, kasaId = k.KasaId },
                new
                {
                    arac = "ALTIN", tutarTl = 9_160, ayarId = "K22", gramAgirlik = 5, birimFiyatTlHasGram = 2_000,
                    satici = new { saticiTipi = "NIHAI_TUKETICI", adSoyad = "Ali Veli", tckn = "10000000146", adres = "Ulus" },
                },
            },
        });
        Assert.True(c.IsSuccessStatusCode, $"{(int)c.StatusCode} {await c.Content.ReadAsStringAsync()}");
        var j = await Govde(c);
        var altin = j.GetProperty("altinBacagi");
        Assert.Equal(4.58m, altin.GetProperty("hasGrami").GetDecimal());
        Assert.Equal(9_160m, altin.GetProperty("netTl").GetDecimal());
        Assert.Equal(2, altin.GetProperty("belgeSayisi").GetInt32());
        Assert.StartsWith("GP-", altin.GetProperty("giderPusulasi").GetProperty("pusulaNo").GetString());
        Assert.Equal(10_840m, j.GetProperty("nakitToplam").GetDecimal());

        using var kontrol = f.Vt.OlusturSistem();
        var turler = kontrol.Hareketler.Where(h => h.TenantId == k.TenantId).Select(h => h.Tur).OrderBy(t => t).ToList();
        Assert.Equal(["HURDA_ALIS", "SATIS"], turler);
    }
}
