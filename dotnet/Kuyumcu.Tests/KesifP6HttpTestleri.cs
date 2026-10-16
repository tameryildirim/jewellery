using System.Net;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET KESIF-P6 — SUNUCU KAPILARI [HTTP]
//   • PATCH /kur/kaynak YALNIZ SAHİP (ekip oyu Mehmet+Taşkın, 30.09 18:10): /kur
//     ekranındaki ikinci seçici MÜDÜR'e kaynağı onaysız değiştirtiyordu; ekran
//     kapı değildir → uç [RolGerekli("SAHIP")], MÜDÜR 403 ve kiracı ayarı DEĞİŞMEZ.
//   • POS komisyon oranı ölçeği: sütun decimal(5,4). 0,01855 sessizce yuvarlanıyordu
//     → 400, kayıt DEĞİŞMEZ (oluşturma, PATCH, taksit oranı — üç yazma yolu).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class KesifP6HttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task KESIFP6_Kur_kaynagi_MUDURE_403_ve_kiraci_ayari_DEGISMEZ()
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "P6 kur kaynak müdür");
        var oncesi = db.Tenants.AsNoTracking().First(t => t.Id == k.TenantId).KurKaynagi;

        var mudur = await f.Ortam.Istemci(k, "MUDUR")
            .PatchJson("/api/v1/kur/kaynak", new { kaynak = "ANKARA" });
        Assert.Equal(HttpStatusCode.Forbidden, mudur.StatusCode);

        using var db2 = f.Vt.OlusturSistem();
        Assert.Equal(oncesi, db2.Tenants.AsNoTracking().First(t => t.Id == k.TenantId).KurKaynagi);
    }

    [Fact]
    public async Task KESIFP6_Kur_kaynagi_SAHIPE_ACIK_kapi_rolden_gecer()
    {
        // SAHİP rol kapısını geçer: tanımsız kod 400 (doğrulamaya ulaştı), 403 DEĞİL.
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "P6 kur kaynak sahip");

        var sahip = await f.Ortam.Istemci(k, "SAHIP")
            .PatchJson("/api/v1/kur/kaynak", new { kaynak = "YOKBOYLEKAYNAK" });
        Assert.Equal(HttpStatusCode.BadRequest, sahip.StatusCode);
    }

    private static object CihazGovde(Kiraci k, decimal oran) => new
    {
        ad = $"P6-{Guid.NewGuid():N}"[..12],
        bankaAdi = "Garanti",
        subeId = k.SubeId,
        kasaId = k.KasaId,
        pesinKomisyonOran = oran,
        valorGun = 1,
    };

    [Fact]
    public async Task KESIFP6_POS_komisyon_orani_4_haneden_INCE_ise_400_yuvarlanmaz()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "P6 POS oran ölçek");
        var c = f.Ortam.Istemci(k);

        // (1) oluşturma: 0,01855 → 400, cihaz DOĞMAZ.
        var red = await c.PostJson("/api/v1/pos/cihazlar", CihazGovde(k, 0.01855m));
        Assert.Equal(HttpStatusCode.BadRequest, red.StatusCode);
        Assert.Contains("4 ondalık", (await red.Json()).GetProperty("message").GetString());
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(0, db.PosCihazlari.AsNoTracking().Count(x => x.TenantId == k.TenantId));

        // 4 hane sınırı AÇIK: 0,0185 kabul.
        var ok = await c.PostJson("/api/v1/pos/cihazlar", CihazGovde(k, 0.0185m));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var id = (await ok.Json()).GetProperty("cihaz").GetProperty("id").GetString()!;

        // (2) PATCH: 0,01855 → 400, kayıtlı oran DEĞİŞMEZ.
        var patch = await c.PatchJson($"/api/v1/pos/cihazlar/{id}", new { pesinKomisyonOran = 0.01855m });
        Assert.Equal(HttpStatusCode.BadRequest, patch.StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(0.0185m, db.PosCihazlari.AsNoTracking().Single(x => x.Id == id).PesinKomisyonOran);

        // (3) taksit oranı: 0,01855 → 400.
        var taksit = await c.PostJson($"/api/v1/pos/cihazlar/{id}/taksit-oranlari",
            new { taksitAdedi = 3, komisyonOran = 0.01855m, valorGun = 30 });
        Assert.Equal(HttpStatusCode.BadRequest, taksit.StatusCode);
    }
}
