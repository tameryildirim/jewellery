using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// 17.10 kurucu: stok girişi tek adımlı "Mal geldi" — sipariş + onay + teslim alma tek istekte; reddedilen
// teslim almada açılan sipariş aynı istekte iptal edilir (yarım sipariş kalmaz).
[Collection("http")]
public sealed class MalGeldiHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "MALGELDI " + ad);
    }

    private decimal Stok(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Stoklar.AsNoTracking().Where(s => s.TenantId == k.TenantId && s.UrunId == k.UrunId).Sum(s => (decimal?)s.Adet) ?? 0m;
    }

    private List<SatinalmaSiparisi> Siparisler(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.SatinalmaSiparisleri.AsNoTracking().Where(s => s.TenantId == k.TenantId).OrderBy(s => s.Id).ToList();
    }

    [Fact]
    public async Task MG_01_tek_istekte_stoga_girer_siparis_kapanir_irsaliye_acilir()
    {
        var k = Kur("tamam");
        var once = Stok(k);
        var c = f.Ortam.Istemci(k, "SAHIP");
        var r = await c.PostJson("/api/v1/satinalma/mal-geldi", new
        {
            tedarikciCariId = k.CariId, kurSnapshotId = k.KurId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 2m, birimFiyatTl = 1_000m } },
        });
        Assert.True(r.StatusCode == HttpStatusCode.OK, await r.Metin());
        Assert.Equal(once + 2m, Stok(k));
        var s = Assert.Single(Siparisler(k));
        Assert.Equal(SatinalmaDurumu.Kapandi, s.Durum);
        using var db = f.Vt.OlusturSistem();
        Assert.Single(db.Irsaliyeler.AsNoTracking().Where(i => i.TenantId == k.TenantId && i.SatinalmaSiparisId == s.Id).ToList());
    }

    [Fact]
    public async Task MG_02_eksik_satir_400_ve_siparis_acilmaz()
    {
        var k = Kur("eksik");
        var c = f.Ortam.Istemci(k, "SAHIP");
        foreach (var govde in new object[]
        {
            new { tedarikciCariId = k.CariId, kurSnapshotId = k.KurId, kalemler = new[] { new { urunId = (string?)null, adet = 1m, birimFiyatTl = 10m } } },
            new { tedarikciCariId = k.CariId, kurSnapshotId = k.KurId, kalemler = new[] { new { urunId = (string?)k.UrunId, adet = 0m, birimFiyatTl = 10m } } },
            new { tedarikciCariId = k.CariId, kurSnapshotId = k.KurId, kalemler = new[] { new { urunId = (string?)k.UrunId, adet = 1m, birimFiyatTl = -1m } } },
            new { tedarikciCariId = (string?)null, kurSnapshotId = k.KurId, kalemler = new[] { new { urunId = (string?)k.UrunId, adet = 1m, birimFiyatTl = 10m } } },
        })
            Assert.Equal(HttpStatusCode.BadRequest, (await c.PostJson("/api/v1/satinalma/mal-geldi", govde)).StatusCode);
        Assert.Empty(Siparisler(k));
    }

    [Fact]
    public async Task MG_03_teslim_alma_reddedilirse_siparis_iptal_edilir_stok_degismez()
    {
        var k = Kur("red");
        var once = Stok(k);
        var c = f.Ortam.Istemci(k, "SAHIP");
        var r = await c.PostJson("/api/v1/satinalma/mal-geldi", new
        {
            tedarikciCariId = k.CariId, kurSnapshotId = "01AAAAAAAAAAAAAAAAAAAAAAAA",   // olmayan kur: alış reddeder
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 1_000m } },
        });
        Assert.False(r.IsSuccessStatusCode, await r.Metin());
        Assert.NotEqual(HttpStatusCode.InternalServerError, r.StatusCode);
        Assert.Equal(once, Stok(k));
        var s = Assert.Single(Siparisler(k));
        Assert.Equal(SatinalmaDurumu.Iptal, s.Durum);
        Assert.StartsWith("Mal geldi tamamlanamadı", s.IptalGerekce);
    }

    [Fact]
    public async Task MG_04_tezgahtar_403()
    {
        var k = Kur("rol");
        var r = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson("/api/v1/satinalma/mal-geldi", new
        {
            tedarikciCariId = k.CariId, kurSnapshotId = k.KurId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = 10m } },
        });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Empty(Siparisler(k));
    }
}
