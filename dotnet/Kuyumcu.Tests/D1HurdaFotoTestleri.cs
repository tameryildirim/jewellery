using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-12 (18.10, rakip eşitleme) — hurda alımına fotoğraf: ortak belge deposu, sahip HURDA_ALIS defter satırı.
// Tezgâhtar yükler, listede görünür; satış satırına ya da başka işletmenin hurdasına bağlanamaz (404);
// fotoğraf türü yalnız JPEG/PNG kabul eder (PDF 400). Defter satırı değişmez.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class D1HurdaFotoTestleri(HttpFikstur f)
{
    private static readonly object Satici = new { saticiTipi = "NIHAI_TUKETICI", adSoyad = "Ali Veli", tckn = "10000000146", adres = "Ulus" };

    private static string JpegB64()
    {
        var b = new byte[64];
        b[0] = 0xFF; b[1] = 0xD8; b[2] = 0xFF;
        return Convert.ToBase64String(b);
    }

    [Fact]
    public async Task Hurda_alimina_foto_yuklenir_listelenir_baska_tur_ve_baska_isletme_404_pdf_400()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1-hurda-foto");
        var t = f.Ortam.Istemci(k, "TEZGAHTAR");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var h = await s.PostJson("/api/v1/islemler/hurda-alis", new Dictionary<string, object?>
        {
            ["cariId"] = k.CariId, ["ayarId"] = "K22", ["gramAgirlik"] = 1m, ["birimFiyatTlHasGram"] = 6_000m,
            ["kasaId"] = k.KasaId, ["kurSnapshotId"] = k.KurId, ["odemeTuru"] = "NAKIT", ["satici"] = Satici,
        });
        Assert.True(h.IsSuccessStatusCode, await h.Content.ReadAsStringAsync());
        string hurdaId; decimal hasOnce;
        using (var db = f.Vt.OlusturSistem())
        {
            var hr = db.Hareketler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Tur == "HURDA_ALIS");
            hurdaId = hr.Id; hasOnce = hr.HasGrami;
        }

        var y = await t.PostAsJsonAsync("/api/v1/belgeler", new { sahipTuru = "HURDA_ALIS", sahipId = hurdaId, tur = "FOTOGRAF", ad = "hurda.jpg", icerikBase64 = JpegB64() });
        Assert.True(y.IsSuccessStatusCode, await y.Content.ReadAsStringAsync());
        var liste = await t.GetFromJsonAsync<List<JsonElement>>($"/api/v1/belgeler?sahipTuru=HURDA_ALIS&sahipId={hurdaId}");
        Assert.Equal("hurda.jpg", Assert.Single(liste!).GetProperty("ad").GetString());

        // PDF fotoğraf değildir.
        var pdf = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("%PDF-1.7 x"));
        Assert.Equal(HttpStatusCode.BadRequest, (await t.PostAsJsonAsync("/api/v1/belgeler",
            new { sahipTuru = "HURDA_ALIS", sahipId = hurdaId, tur = "FOTOGRAF", ad = "a.pdf", icerikBase64 = pdf })).StatusCode);
        // Hurda olmayan defter satırı (ürün) ve başka işletme → 404.
        Assert.Equal(HttpStatusCode.NotFound, (await t.PostAsJsonAsync("/api/v1/belgeler",
            new { sahipTuru = "HURDA_ALIS", sahipId = k.UrunId, tur = "FOTOGRAF", ad = "b.jpg", icerikBase64 = JpegB64() })).StatusCode);
        Kiraci baska;
        using (var db = f.Vt.OlusturSistem()) baska = TestKurulum.KiraciKur(db, "D1-hurda-foto-b");
        Assert.Equal(HttpStatusCode.NotFound, (await f.Ortam.Istemci(baska, "SAHIP").PostAsJsonAsync("/api/v1/belgeler",
            new { sahipTuru = "HURDA_ALIS", sahipId = hurdaId, tur = "FOTOGRAF", ad = "c.jpg", icerikBase64 = JpegB64() })).StatusCode);

        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(hasOnce, db.Hareketler.AsNoTracking().Single(x => x.Id == hurdaId).HasGrami);   // defter değişmedi
    }
}
