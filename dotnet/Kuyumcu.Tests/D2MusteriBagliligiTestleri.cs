using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Contracts.Sayfalar.Cariler;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// D2-12 — sadakat puanı (hesaplanır; kullanım düşülür), dilek listesi, uykuda müşteri, yüzük ölçüsü.
[Collection("http")]
public sealed class D2MusteriBagliligiTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Puan_net_alisveristen_hesaplanir_kullanim_bakiyeyi_asamaz_dilek_ve_olcu_tutulur_uykuda_listelenir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D2SADAKAT");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var t = f.Ortam.Istemci(k, "TEZGAHTAR");
        Assert.False((await s.GetFromJsonAsync<SadakatDurumu>($"/api/v1/cari/{k.CariId}/sadakat"))!.Etkin);
        Assert.Equal(HttpStatusCode.Forbidden, (await t.PutAsJsonAsync("/api/v1/sadakat/ayar", new { tlBasina = 100m })).StatusCode);
        Assert.True((await s.PutAsJsonAsync("/api/v1/sadakat/ayar", new { tlBasina = 1000m, baslangic = Kuyumcu.Api.Common.Zaman.YerelBugun().AddDays(-1) })).IsSuccessStatusCode);

        Assert.True((await s.PostAsJsonAsync("/api/v1/islemler/musteri-kimlik", new { cariId = k.CariId, kimlikNo = "10000000146" })).IsSuccessStatusCode);
        Assert.True((await s.PostAsJsonAsync("/api/v1/urunler", new { tip = "ZIYNET", kod = "D2S-2G", ad = "2 g Yüzük", ayarId = "K22", gramAgirlik = 2m, acilisStogu = new { adet = 5m } })).IsSuccessStatusCode);
        string urun; decimal birim;
        using (var db = f.Vt.OlusturSistem())
        {
            var ur = db.Urunler.AsNoTracking().Single(u => u.TenantId == k.TenantId && u.Kod == "D2S-2G");
            urun = ur.Id;
            birim = Math.Ceiling(ur.HasGrami * db.KurSnapshots.AsNoTracking().Single(x => x.Id == k.KurId).AltinHasFiyati * 1.10m);
        }
        var r = await s.PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId, kalemler = new[] { new { urunId = urun, adet = 1m, birimFiyatTl = birim, hediye = false } },
            odemeler = new[] { new { arac = "KART", tutarTl = birim, kasaId = k.KasaId } }, kurSnapshotId = k.KurId, fisIskontoYuzde = 0m,
        });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        var d = (await s.GetFromJsonAsync<SadakatDurumu>($"/api/v1/cari/{k.CariId}/sadakat"))!;
        Assert.True(d.Etkin);
        Assert.Equal(birim, d.NetAlisverisTl);
        Assert.Equal((int)Math.Floor(birim / 1000m), d.Kazanilan);
        Assert.Equal(HttpStatusCode.BadRequest, (await t.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/sadakat/kullan", new { puan = d.Bakiye + 1, aciklama = "fazla" })).StatusCode);
        var k2 = (await (await t.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/sadakat/kullan", new { puan = 3, aciklama = "Küpe hediye edildi" })).Content.ReadFromJsonAsync<SadakatDurumu>())!;
        Assert.Equal(d.Bakiye - 3, k2.Bakiye);
        Assert.Equal("Küpe hediye edildi", k2.Kullanimlar[0].Aciklama);

        var dl = await (await t.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/dilekler", new { urunId = urun, aciklama = "Bu yüzüğün 18 numarası" })).Content.ReadFromJsonAsync<List<CariDilekOzet>>();
        var dilek = Assert.Single(dl!);
        Assert.Equal("2 g Yüzük", dilek.UrunAd);
        var kap = await (await t.PostAsync($"/api/v1/cari/dilekler/{dilek.Id}/kapat", null)).Content.ReadFromJsonAsync<List<CariDilekOzet>>();
        Assert.True(kap![0].Kapali);

        Assert.True((await s.PatchAsJsonAsync($"/api/v1/cari/{k.CariId}", new { yuzukOlcusu = "16,5" })).IsSuccessStatusCode);
        Assert.Contains("\"yuzukOlcusu\":\"16,5\"", await s.GetStringAsync($"/api/v1/cari/{k.CariId}"));

        // Uykuda: satışı 200 gün öncesine çek → 180 günlük listede.
        using (var db = f.Vt.OlusturSistem())
            await db.Hareketler.Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS").ExecuteUpdateAsync(x => x.SetProperty(y => y.Tarih, DateTime.UtcNow.AddDays(-200)));
        var u = await s.GetFromJsonAsync<List<UykudaMusteri>>("/api/v1/cari/uykuda?gun=180");
        var um = Assert.Single(u!, x => x.CariId == k.CariId);
        Assert.InRange(um.GecenGun, 199, 201);
        Assert.Empty((await s.GetFromJsonAsync<List<UykudaMusteri>>("/api/v1/cari/uykuda?gun=365"))!.Where(x => x.CariId == k.CariId));
        Assert.Equal(HttpStatusCode.Forbidden, (await t.GetAsync("/api/v1/cari/uykuda")).StatusCode);
    }
}
