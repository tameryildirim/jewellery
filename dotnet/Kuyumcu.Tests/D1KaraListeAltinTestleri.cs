using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-07 — altınla öde (has borcu kapanır, fazlası aynı ayarla emanete) · kara liste (veresiye engeli).
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class D1KaraListeAltinTestleri(HttpFikstur f)
{
    private static string Ileti(string j) => JsonDocument.Parse(j).RootElement.GetProperty("message").GetString()!;

    private (decimal Emanetsiz, decimal Emanette) Has(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        var kayitli = db.CariBakiyeler.AsNoTracking().Where(b => b.CariId == k.CariId).Select(b => (decimal?)b.HasGramBakiye).FirstOrDefault() ?? 0m;
        var etki = CariBakiyeKurali.EmanetEtkisi(db, k.TenantId, k.CariId).GetAwaiter().GetResult();
        return (CariBakiyeKurali.EmanetsizHas(kayitli, etki), -etki);
    }

    [Fact]
    public async Task Altinla_ode_once_has_borcunu_kapatir_fazlasi_ayni_ayarla_emanete_girer()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1ALTIN1");
        var s = f.Ortam.Istemci(k, "TEZGAHTAR");
        var v = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync($"/api/v1/cari/{k.CariId}/veresiye", new { tlTutari = 1m, hasGrami = 5m, kurSnapshotId = k.KurId });
        Assert.True(v.IsSuccessStatusCode, await v.Content.ReadAsStringAsync());
        Assert.Equal(5m, Has(k).Emanetsiz);

        var r = await s.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/altin-tahsilat", new { ayarId = "K22", gramAgirlik = 10m, kurSnapshotId = k.KurId });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        var (emanetsiz, emanette) = Has(k);
        Assert.InRange(emanetsiz, -0.001m, 0.001m);                         // borç kapandı (gram yuvarlaması < 1 mg has)
        Assert.InRange(emanette, 10m * 0.916m - 5m - 0.001m, 10m * 0.916m - 5m + 0.001m);
        using (var db = f.Vt.OlusturSistem())
        {
            var e = db.Hareketler.AsNoTracking().Single(h => h.TenantId == k.TenantId && h.Tur == "EMANET_GIRIS");
            Assert.Equal("K22", e.EmanetAyarId);
            Assert.True(db.Hareketler.AsNoTracking().Any(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT" && h.HasGrami > 4.99m));
        }

        // Borç yokken getirilen altının tamamı emanete girer.
        var r2 = await s.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/altin-tahsilat", new { ayarId = "K22", gramAgirlik = 2m, kurSnapshotId = k.KurId });
        Assert.True(r2.IsSuccessStatusCode, await r2.Content.ReadAsStringAsync());
        Assert.InRange(Has(k).Emanette, emanette + 1.832m - 0.001m, emanette + 1.832m + 0.001m);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/altin-tahsilat", new { ayarId = "G925", gramAgirlik = 2m, kurSnapshotId = k.KurId })).StatusCode);
    }

    [Fact]
    public async Task Kara_listedeki_musteriye_veresiye_yazilmaz_sahip_kaldirinca_yazilir()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1KARA1");
        var s = f.Ortam.Istemci(k, "SAHIP");
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "TEZGAHTAR").PutAsJsonAsync($"/api/v1/cari/{k.CariId}/veresiye-engeli", new { engelli = true, sebep = "ödemedi" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PutAsJsonAsync($"/api/v1/cari/{k.CariId}/veresiye-engeli", new { engelli = true })).StatusCode);
        Assert.True((await s.PutAsJsonAsync($"/api/v1/cari/{k.CariId}/veresiye-engeli", new { engelli = true, sebep = "Üç aydır ödemedi" })).IsSuccessStatusCode);
        Assert.Contains("\"veresiyeEngeli\":true", await s.GetStringAsync("/api/v1/cari"));

        var tl = await s.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/veresiye", new { tlTutari = 1000m, hasGrami = 0m, kurSnapshotId = k.KurId });
        Assert.Equal(HttpStatusCode.BadRequest, tl.StatusCode);
        Assert.Contains("kara listede", Ileti(await tl.Content.ReadAsStringAsync()));
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/veresiye", new { tlTutari = 1m, hasGrami = 2m, kurSnapshotId = k.KurId })).StatusCode);
        var satis = await s.PostAsJsonAsync("/api/v1/islemler/satis", new { cariId = k.CariId, urunId = k.UrunId, adet = 1m, birimFiyatTl = 1000m, odemeTuru = "VERESIYE", kurSnapshotId = k.KurId });
        Assert.Equal(HttpStatusCode.BadRequest, satis.StatusCode);
        Assert.Contains("kara listede", Ileti(await satis.Content.ReadAsStringAsync()));

        Assert.True((await s.PutAsJsonAsync($"/api/v1/cari/{k.CariId}/veresiye-engeli", new { engelli = false })).IsSuccessStatusCode);
        Assert.True((await s.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/veresiye", new { tlTutari = 1000m, hasGrami = 0m, kurSnapshotId = k.KurId })).IsSuccessStatusCode);
    }
}
