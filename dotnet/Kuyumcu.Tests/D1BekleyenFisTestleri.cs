using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-03 (18.10, rakip eşitleme) — işlem bekletme: fiş bekletilir, listede görünür, geri alınınca
// içerik aynen döner ve kayıt silinir (ikinci alan 404). Bekletme defter yazmaz; başka işletme göremez.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class D1BekleyenFisTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "D1-park-" + ad);
    }

    private static object Govde(Kiraci k, decimal adet = 2m, decimal fiyat = 1000m, string? cariId = null) => new
    {
        icerik = new
        {
            cariId,
            kalemler = new[] { new { urunId = k.UrunId, urunAdi = "istemcinin yazdığı ad", adet, birimFiyat = fiyat, hediye = false, satirIskontoYuzde = 10m } },
            fisIskontoYuzde = 0m,
            aciklama = "  vitrin 3  ",
        },
    };

    [Fact]
    public async Task Beklet_listele_geri_al_icerik_aynen_doner_ikinci_alan_404_defter_yazilmaz()
    {
        var k = YeniKiraci("akis");
        var s = f.Ortam.Istemci(k, "TEZGAHTAR");
        int hareketOnce;
        using (var db = f.Vt.OlusturSistem()) hareketOnce = await db.Hareketler.CountAsync(h => h.TenantId == k.TenantId);

        var r = await s.PostAsJsonAsync("/api/v1/bekleyen-fisler", Govde(k, cariId: k.CariId));
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        var ozet = (await r.Content.ReadFromJsonAsync<BekleyenFisOzet>())!;
        Assert.Equal(1, ozet.KalemSayisi);
        Assert.Equal(1800m, ozet.ToplamTl);          // 2 × 1000 × %90

        var liste = await s.GetFromJsonAsync<List<BekleyenFisOzet>>("/api/v1/bekleyen-fisler");
        var satir = Assert.Single(liste!);
        Assert.Equal(ozet.Id, satir.Id);
        Assert.False(string.IsNullOrEmpty(satir.KullaniciAd));

        // Başka kasa (SAHİP) geri alır: içerik aynen; ürün adı SUNUCUDAN; açıklama kırpılmış.
        var sahip = f.Ortam.Istemci(k, "SAHIP");
        var al = await sahip.PostAsync($"/api/v1/bekleyen-fisler/{ozet.Id}/al", null);
        Assert.True(al.IsSuccessStatusCode, await al.Content.ReadAsStringAsync());
        var ic = (await al.Content.ReadFromJsonAsync<BekleyenFisIcerik>())!;
        Assert.Equal(k.CariId, ic.CariId);
        var kalem = Assert.Single(ic.Kalemler);
        Assert.Equal(k.UrunId, kalem.UrunId);
        Assert.NotEqual("istemcinin yazdığı ad", kalem.UrunAdi);
        Assert.Equal(2m, kalem.Adet);
        Assert.Equal(1000m, kalem.BirimFiyat);
        Assert.Equal(10m, kalem.SatirIskontoYuzde);
        Assert.Equal("vitrin 3", ic.Aciklama);

        Assert.Equal(HttpStatusCode.NotFound, (await s.PostAsync($"/api/v1/bekleyen-fisler/{ozet.Id}/al", null)).StatusCode);
        Assert.Empty((await s.GetFromJsonAsync<List<BekleyenFisOzet>>("/api/v1/bekleyen-fisler"))!);
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(hareketOnce, await db.Hareketler.CountAsync(h => h.TenantId == k.TenantId));   // defter değişmedi
    }

    [Fact]
    public async Task Gecersiz_govde_400_baska_isletme_goremez_silinen_404()
    {
        var k = YeniKiraci("kural");
        var s = f.Ortam.Istemci(k, "SAHIP");
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync("/api/v1/bekleyen-fisler",
            new { icerik = new { kalemler = Array.Empty<object>() } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync("/api/v1/bekleyen-fisler", Govde(k, adet: 0m))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync("/api/v1/bekleyen-fisler", Govde(k, fiyat: -1m))).StatusCode);

        var baska = YeniKiraci("baska");
        // Başka işletmenin ürünü bekletilemez.
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Ortam.Istemci(baska, "SAHIP").PostAsJsonAsync("/api/v1/bekleyen-fisler", Govde(k))).StatusCode);

        var ozet = (await (await s.PostAsJsonAsync("/api/v1/bekleyen-fisler", Govde(k))).Content.ReadFromJsonAsync<BekleyenFisOzet>())!;
        var b = f.Ortam.Istemci(baska, "SAHIP");
        Assert.Empty((await b.GetFromJsonAsync<List<BekleyenFisOzet>>("/api/v1/bekleyen-fisler"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsync($"/api/v1/bekleyen-fisler/{ozet.Id}/al", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsync($"/api/v1/bekleyen-fisler/{ozet.Id}/sil", null)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await s.PostAsync($"/api/v1/bekleyen-fisler/{ozet.Id}/sil", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await s.PostAsync($"/api/v1/bekleyen-fisler/{ozet.Id}/sil", null)).StatusCode);
    }
}
