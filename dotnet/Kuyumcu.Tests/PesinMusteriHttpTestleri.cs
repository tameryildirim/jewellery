using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-3 — PEŞİN MÜŞTERİ: GERÇEK HTTP BORU HATTI
//
// Servis testi [Authorize]/[ModulYetkisi]/[RolGerekli] niteliklerini ve hata
// ara katmanını GÖRMEZ; bu dosya uçları üretim boru hattında ölçer. Yanıt
// gövdesi AYRIŞTIRILIR (yalnız durum koduna bakılmaz).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class PesinMusteriHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "PAKETB3-TEST-" + ad);
    }

    private static async Task<JsonElement> Govde(HttpResponseMessage c) =>
        await c.Content.ReadFromJsonAsync<JsonElement>();

    private async Task<string> PesinId(Kiraci k)
    {
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/cari/sistem-carisi");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        return (await Govde(c)).GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task B3H_01_jetonsuz_istek_401()
    {
        var c = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/cari/sistem-carisi");
        Assert.Equal(HttpStatusCode.Unauthorized, c.StatusCode);
    }

    [Fact]
    public async Task B3H_02_tezgahtar_karti_okur_camelCase_ve_ikinci_cagri_ayni_kart()
    {
        var k = YeniKiraci("okuma");
        var c = await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/cari/sistem-carisi");
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var j = await Govde(c);
        Assert.Equal("Peşin Müşteri", j.GetProperty("ad").GetString());
        Assert.True(j.GetProperty("sistemCarisi").GetBoolean());
        Assert.True(j.GetProperty("aktif").GetBoolean());
        var id = j.GetProperty("id").GetString();
        Assert.False(string.IsNullOrEmpty(id));

        Assert.Equal(id, await PesinId(k));

        // Liste ucu kartı sistemCarisi=true ile, sıradan cariyi false ile döner.
        var liste = await Govde(await f.Ortam.Istemci(k).GetAsync("/api/v1/cari?limit=50"));
        var satirlar = liste.EnumerateArray().ToList();
        Assert.True(satirlar.Single(s => s.GetProperty("id").GetString() == id).GetProperty("sistemCarisi").GetBoolean());
        Assert.False(satirlar.Single(s => s.GetProperty("id").GetString() == k.CariId).GetProperty("sistemCarisi").GetBoolean());
    }

    [Fact]
    public async Task B3H_03_DELETE_400_ve_kart_aktif_kalir()
    {
        var k = YeniKiraci("delete");
        var id = await PesinId(k);
        var c = await f.Ortam.Istemci(k, "SAHIP").DeleteAsync($"/api/v1/cari/{id}");
        Assert.Equal(HttpStatusCode.BadRequest, c.StatusCode);
        Assert.Contains("silinemez", (await Govde(c)).GetProperty("message").GetString());

        var detay = await Govde(await f.Ortam.Istemci(k).GetAsync($"/api/v1/cari/{id}"));
        Assert.True(detay.GetProperty("aktif").GetBoolean());
        Assert.True(detay.GetProperty("sistemCarisi").GetBoolean());
    }

    [Fact]
    public async Task B3H_04_PATCH_aktif_false_400_ad_degisikligi_200()
    {
        var k = YeniKiraci("patch");
        var id = await PesinId(k);
        var istemci = f.Ortam.Istemci(k, "SAHIP");

        var pasif = await istemci.PatchAsJsonAsync($"/api/v1/cari/{id}", new { aktif = false });
        Assert.Equal(HttpStatusCode.BadRequest, pasif.StatusCode);

        var ad = await istemci.PatchAsJsonAsync($"/api/v1/cari/{id}", new { ad = "Peşin Satış" });
        Assert.Equal(HttpStatusCode.OK, ad.StatusCode);
        var j = await Govde(ad);
        Assert.Equal("Peşin Satış", j.GetProperty("ad").GetString());
        Assert.True(j.GetProperty("aktif").GetBoolean());
        Assert.True(j.GetProperty("sistemCarisi").GetBoolean());
    }

    [Fact]
    public async Task B3H_05_satis_fisi_esik_ustu_400_esik_alti_201_ya_da_200()
    {
        var k = YeniKiraci("fis");
        var id = await PesinId(k);
        var istemci = f.Ortam.Istemci(k, "TEZGAHTAR");

        object Fis(decimal tutar, string arac) => new
        {
            cariId = id,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = tutar } },
            odemeler = new[] { new { arac, tutarTl = tutar, kasaId = k.KasaId } },
            kurSnapshotId = k.KurId,
        };

        var ust = await istemci.PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis(200_000m, "HAVALE"));
        Assert.Equal(HttpStatusCode.BadRequest, ust.StatusCode);
        var mesaj = (await Govde(ust)).GetProperty("message").GetString()!;
        Assert.Contains("gerçek müşteri", mesaj);

        var alt = await istemci.PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis(5_000m, "NAKIT"));
        Assert.True(alt.IsSuccessStatusCode, $"{(int)alt.StatusCode}: {await alt.Content.ReadAsStringAsync()}");

        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal(1, kontrol.Hareketler.Count(h => h.TenantId == k.TenantId && h.CariId == id && h.Tur == "SATIS"));
    }
}
