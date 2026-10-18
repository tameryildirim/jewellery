using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-09 (18.10, rakip eşitleme) — hesap dönüştürme: bakiye bir birimden ötekine YÖNÜYLE taşınır, kaynak bakiyeden
// fazlası çevrilmez, oran varsayılmaz (iki tutar istekte), defterde CARI_DUZELTME bacakları (para el değiştirmez).
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class D1HesapDonusturTestleri(HttpFikstur f)
{
    [Fact]
    public async Task TL_borcu_has_ve_dolara_yonuyle_tasinir_sinirlar_400_tezgahtar_403()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1-donustur");
        var s = f.Ortam.Istemci(k, "SAHIP");
        // Başlangıç: müşteri bize 10.000 TL borçlu (düzeltme fişiyle).
        var d = await s.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/duzeltme",
            new { tlDelta = 10_000m, hasDelta = 0m, gerekce = "Test başlangıç borcu — dönüştürme senaryosu", kurSnapshotId = k.KurId });
        Assert.True(d.IsSuccessStatusCode, await d.Content.ReadAsStringAsync());

        (decimal Tl, decimal Has, decimal Usd) Bakiye()
        {
            using var db = f.Vt.OlusturSistem();
            var b = db.CariBakiyeler.AsNoTracking().Single(x => x.CariId == k.CariId);
            return (b.TlBakiye, b.HasGramBakiye, b.UsdBakiye);
        }

        var r = await s.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/hesap-donustur",
            new { kaynak = "TRY", hedef = "HAS", kaynakMiktar = 6_000m, hedefMiktar = 1.25m, kurSnapshotId = k.KurId });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        Assert.Equal((4_000m, 1.25m, 0m), Bakiye());   // borç borç olarak taşındı

        var r2 = await s.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/hesap-donustur",
            new { kaynak = "TRY", hedef = "USD", kaynakMiktar = 4_000m, hedefMiktar = 100m, kurSnapshotId = k.KurId });
        Assert.True(r2.IsSuccessStatusCode, await r2.Content.ReadAsStringAsync());
        Assert.Equal((0m, 1.25m, 100m), Bakiye());

        using (var db = f.Vt.OlusturSistem())
        {
            var l = db.Hareketler.AsNoTracking().Where(h => h.CariId == k.CariId && h.Aciklama!.Contains("Hesap dönüştürme")).ToList();
            Assert.Equal(3, l.Count);   // TRY+HAS tek bacak · TRY bacağı + USD bacağı
            Assert.All(l, h => Assert.Equal("CARI_DUZELTME", h.Tur));
        }

        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/hesap-donustur",
            new { kaynak = "HAS", hedef = "TRY", kaynakMiktar = 2m, hedefMiktar = 10_000m, kurSnapshotId = k.KurId })).StatusCode);   // bakiyeden fazla
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/hesap-donustur",
            new { kaynak = "TRY", hedef = "HAS", kaynakMiktar = 1m, hedefMiktar = 1m, kurSnapshotId = k.KurId })).StatusCode);   // TL bakiyesi yok
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/hesap-donustur",
            new { kaynak = "HAS", hedef = "HAS", kaynakMiktar = 1m, hedefMiktar = 1m, kurSnapshotId = k.KurId })).StatusCode);   // aynı hesap
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync($"/api/v1/cari/{k.CariId}/hesap-donustur",
            new { kaynak = "HAS", hedef = "TRY", kaynakMiktar = 1m, hedefMiktar = 5_000m, kurSnapshotId = k.KurId })).StatusCode);

        // Alacak (biz borçluyuz) yönü de korunur: -500 TL → -0,1 has.
        var a = await s.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/duzeltme",
            new { tlDelta = -500m, hasDelta = 0m, gerekce = "Test alacak bakiyesi — dönüştürme yönü", kurSnapshotId = k.KurId });
        Assert.True(a.IsSuccessStatusCode, await a.Content.ReadAsStringAsync());
        var r3 = await s.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/hesap-donustur",
            new { kaynak = "TRY", hedef = "HAS", kaynakMiktar = 500m, hedefMiktar = 0.1m, kurSnapshotId = k.KurId });
        Assert.True(r3.IsSuccessStatusCode, await r3.Content.ReadAsStringAsync());
        Assert.Equal((0m, 1.15m, 100m), Bakiye());
    }
}
