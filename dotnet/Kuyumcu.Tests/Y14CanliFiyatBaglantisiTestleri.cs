using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// Y-14 (20.10) — canlı fiyat bağlantısı: süreli KUR anahtarıyla oturumsuz /api/disa/v1/pano; süresi dolan 401; URUN-only 403; sorgu parametresi.
[Collection("http")]
public sealed class Y14CanliFiyatBaglantisiTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Sureli_anahtar_pano_verir_suresi_dolunca_401_kapsam_disi_403()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "Y14-fiyat");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var y = await s.PostAsJsonAsync("/api/v1/api-anahtarlari", new { ad = "Canlı fiyat bağlantısı", kapsamlar = new[] { "KUR" }, gecerlilikGun = 30 });
        Assert.True(y.IsSuccessStatusCode, await y.Content.ReadAsStringAsync());
        var yj = JsonSerializer.Deserialize<JsonElement>(await y.Content.ReadAsStringAsync());
        var anahtar = yj.GetProperty("anahtar").GetString()!;
        var ozet = yj.GetProperty("ozet");
        Assert.NotEqual(JsonValueKind.Null, ozet.GetProperty("bitis").ValueKind);

        var anonim = f.Ortam.AnonimIstemci();
        var pano = await anonim.GetAsync($"/api/disa/v1/pano?anahtar={anahtar}");
        Assert.Equal(HttpStatusCode.OK, pano.StatusCode);
        var pj = JsonSerializer.Deserialize<JsonElement>(await pano.Content.ReadAsStringAsync());
        Assert.True(pj.TryGetProperty("satirlar", out _));

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonim.GetAsync("/api/disa/v1/pano?anahtar=mhk_yok")).StatusCode);

        using (var db = f.Vt.OlusturSistem())
        {
            var a = await db.ApiAnahtarlari.SingleAsync(x => x.Id == ozet.GetProperty("id").GetString());
            a.Bitis = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonim.GetAsync($"/api/disa/v1/pano?anahtar={anahtar}")).StatusCode);

        var u = await s.PostAsJsonAsync("/api/v1/api-anahtarlari", new { ad = "Yalnız ürün", kapsamlar = new[] { "URUN" } });
        var uAnahtar = JsonSerializer.Deserialize<JsonElement>(await u.Content.ReadAsStringAsync()).GetProperty("anahtar").GetString();
        Assert.Equal(HttpStatusCode.Forbidden, (await anonim.GetAsync($"/api/disa/v1/pano?anahtar={uAnahtar}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync("/api/v1/api-anahtarlari", new { ad = "x", kapsamlar = new[] { "KUR" }, gecerlilikGun = 400 })).StatusCode);
    }
}
