using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// M6b — tezgâhta TC/VKN ile müşteri bul, yoksa aç (POST /islemler/musteri-ac). Tezgâhtar da kullanır.
[Collection("http")]
public sealed class TezgahMusteriAcHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "M6B " + ad);
    }

    private static async Task<JsonElement> Tamam(HttpResponseMessage r)
    {
        var g = await r.Metin();
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"{(int)r.StatusCode}: {g}");
        return JsonDocument.Parse(g).RootElement;
    }

    [Fact]
    public async Task M6b_01_yeni_TC_ad_ile_acilir_ikinci_kez_ayni_kart_bulunur()
    {
        var k = Kur("yeni");
        var tc = Yasal3Yardim.Tckn();
        var t = f.Ortam.Istemci(k, "TEZGAHTAR");
        var y = await Tamam(await t.PostJson("/api/v1/islemler/musteri-ac", new { kimlikNo = tc, ad = "Ayşe Yılmaz" }));
        Assert.True(y.GetProperty("yeni").GetBoolean());
        var id = y.GetProperty("id").GetString()!;
        using (var db = f.Vt.OlusturSistem())
        {
            var c = db.Cariler.AsNoTracking().Single(x => x.Id == id && x.TenantId == k.TenantId);
            Assert.Equal(tc, c.VergiNo);
            Assert.Equal("TCKN", c.KimlikTuru);
            Assert.Equal("MUSTERI", c.Tur);
            Assert.Equal("Ayşe Yılmaz", c.Ad);
        }
        var ikinci = await Tamam(await t.PostJson("/api/v1/islemler/musteri-ac", new { kimlikNo = tc }));   // ad gerekmez
        Assert.False(ikinci.GetProperty("yeni").GetBoolean());
        Assert.Equal(id, ikinci.GetProperty("id").GetString());
    }

    [Fact]
    public async Task M6b_02_kayitli_musteri_TC_ile_bulunur_yeni_kart_acilmaz()
    {
        var k = Kur("kayitli");
        string tc;
        using (var db = f.Vt.OlusturSistem()) tc = TestKurulum.CariyeTcknYaz(db, k);
        int once;
        using (var db = f.Vt.OlusturSistem()) once = db.Cariler.Count(c => c.TenantId == k.TenantId);
        var y = await Tamam(await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson("/api/v1/islemler/musteri-ac", new { kimlikNo = tc, ad = "Başka Ad" }));
        Assert.False(y.GetProperty("yeni").GetBoolean());
        Assert.Equal(k.CariId, y.GetProperty("id").GetString());
        using (var db = f.Vt.OlusturSistem()) Assert.Equal(once, db.Cariler.Count(c => c.TenantId == k.TenantId));
    }

    [Theory]
    [InlineData("11111111111", "Ali Veli")]     // TC kontrol hanesi tutmuyor
    [InlineData("12345", "Ali Veli")]           // hane sayısı
    [InlineData("1234567890a", "Ali Veli")]     // rakam dışı
    [InlineData(null, "Ali Veli")]
    public async Task M6b_03_gecersiz_kimlik_400_kart_acilmaz(string? no, string ad)
    {
        var k = Kur("gecersiz");
        int once;
        using (var db = f.Vt.OlusturSistem()) once = db.Cariler.Count(c => c.TenantId == k.TenantId);
        var r = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson("/api/v1/islemler/musteri-ac", new { kimlikNo = no, ad });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        using (var db = f.Vt.OlusturSistem()) Assert.Equal(once, db.Cariler.Count(c => c.TenantId == k.TenantId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    [InlineData("12")]        // en az bir harf (K19)
    [InlineData("A")]         // en az 2 karakter
    public async Task M6b_04_yeni_numarada_gecersiz_ad_400(string? ad)
    {
        var k = Kur("ad");
        var r = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson("/api/v1/islemler/musteri-ac", new { kimlikNo = Yasal3Yardim.Tckn(), ad });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }
}
