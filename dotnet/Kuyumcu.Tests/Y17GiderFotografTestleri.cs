using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kuyumcu.Tests;

// Y-17 (20.10) — gider kaydına fiş/fatura fotoğrafı: sahip aynı kiracıdaki gider satırı; yabancı kiracı 404; gider olmayan kasa satırı 404.
[Collection("http")]
public sealed class Y17GiderFotografTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Gider_kaydina_fotograf_eklenir_listelenir_yabanci_kiraci_ve_gider_olmayan_satir_404()
    {
        Kiraci k, baska;
        using (var db = f.Vt.OlusturSistem()) { k = TestKurulum.KiraciKur(db, "Y17-gider"); baska = TestKurulum.KiraciKur(db, "Y17-baska"); }
        var s = f.Ortam.Istemci(k, "SAHIP");
        var kat = await s.PostAsJsonAsync("/api/v1/gider-kategorileri", new { ad = "Kira" });
        Assert.True(kat.IsSuccessStatusCode, await kat.Content.ReadAsStringAsync());
        var katId = (await kat.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        var g = await s.PostAsJsonAsync("/api/v1/giderler", new { kasaId = k.KasaId, giderKategoriId = katId, tutar = 1_200m, aciklama = "Ekim kirası" });
        Assert.True(g.IsSuccessStatusCode, await g.Content.ReadAsStringAsync());
        var giderId = (await g.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

        var png = Convert.ToBase64String([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52, 0, 0, 0, 1, 0, 0, 0, 1, 8, 6, 0, 0, 0]);
        var b = await s.PostAsJsonAsync("/api/v1/belgeler", new { sahipTuru = "GIDER", sahipId = giderId, tur = "FOTOGRAF", ad = "kira-fisi.png", icerikBase64 = png });
        Assert.True(b.IsSuccessStatusCode, await b.Content.ReadAsStringAsync());
        var liste = await s.GetFromJsonAsync<JsonElement>($"/api/v1/belgeler?sahipTuru=GIDER&sahipId={giderId}");
        Assert.Single(liste.EnumerateArray());

        var yabanci = await f.Ortam.Istemci(baska, "SAHIP").PostAsJsonAsync("/api/v1/belgeler", new { sahipTuru = "GIDER", sahipId = giderId, tur = "FOTOGRAF", ad = "x.png", icerikBase64 = png });
        Assert.Equal(HttpStatusCode.NotFound, yabanci.StatusCode);
        var olmayan = await s.PostAsJsonAsync("/api/v1/belgeler", new { sahipTuru = "GIDER", sahipId = "YOK", tur = "FOTOGRAF", ad = "x.png", icerikBase64 = png });
        Assert.Equal(HttpStatusCode.NotFound, olmayan.StatusCode);
    }
}
