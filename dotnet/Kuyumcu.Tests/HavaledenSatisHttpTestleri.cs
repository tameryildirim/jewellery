using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// 17.10 HAVALEDEN SATIŞ (kurucu: "para gelince listeye düşer, tezgâhtar ne sattıysa seçer, fatura kesilir,
// arka planda fişe dönüşür"). Banka hareketine bağlı HAVALE bacağı kasaya YAZILMAZ; dekont satışa bağlanır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class HavaledenSatisHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "HS-" + ad, urunGram: 1m);   // has ≈ 6.800 TL → 10.000 TL satış had altında
    }

    private async Task<(string Hesap, string Dekont)> Havale(Kiraci k, decimal tutar)
    {
        var s = f.Ortam.Istemci(k, "SAHIP");
        var h = await (await s.PostAsJsonAsync("/api/v1/bankalar", new { bankaAdi = "Ziraat", hesapAdi = "Ticari", iban = TestIban.Uret(), paraBirimi = "TRY" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var hesap = h.GetProperty("id").GetString()!;
        var d = await (await s.PostAsJsonAsync($"/api/v1/bankalar/{hesap}/hareket", new { yon = "GELEN", tutar, cariId = k.CariId }))
            .Content.ReadFromJsonAsync<JsonElement>();
        return (hesap, d.GetProperty("hareketId").GetString()!);
    }

    private static object Fis(Kiraci k, decimal fiyat, object odeme, string? belge = "FATURA") => new
    {
        cariId = k.CariId, kurSnapshotId = k.KurId, fisIskontoYuzde = 0m, belgeTuru = belge,
        kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = fiyat, hediye = false } },
        odemeler = new[] { odeme },
    };

    [Fact]
    public async Task Tezgahtar_bekleyen_havaleyi_gorur_satar_fatura_kesilir_kasaya_yazilmaz_dekont_satisa_baglanir()
    {
        var k = Kur("ana");
        var (_, dekont) = await Havale(k, 10_000m);
        var t = f.Ortam.Istemci(k, "TEZGAHTAR");

        var liste = await t.GetFromJsonAsync<JsonElement>("/api/v1/islemler/havale-bekleyen");
        var satir = liste.EnumerateArray().Single(x => x.GetProperty("id").GetString() == dekont);
        Assert.Equal(10_000m, satir.GetProperty("tutar").GetDecimal());
        Assert.False(satir.TryGetProperty("karsiIban", out _));                    // personel IBAN görmez

        decimal kasaOnce;
        using (var db = f.Vt.OlusturSistem()) kasaOnce = db.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId).Bakiye;

        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/islemler/satis-fisi",
            Fis(k, 10_000m, new { arac = "HAVALE", tutarTl = 10_000m, bankaHareketId = dekont }));
        Assert.True(c.IsSuccessStatusCode, await c.Content.ReadAsStringAsync());

        using (var db = f.Vt.OlusturSistem())
        {
            Assert.Equal(kasaOnce, db.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId).Bakiye);   // kasaya girmedi
            var b = db.BankaHareketleri.AsNoTracking().Single(x => x.Id == dekont);
            Assert.Equal(Kuyumcu.Api.Modules.Banka.CariKarari.Satis, b.CariKarari);
            Assert.NotNull(b.HareketId);
            Assert.Equal(b.HareketId, b.KararHareketId);
            Assert.Equal("KESILDI", db.Faturalar.AsNoTracking().Single(x => x.TenantId == k.TenantId).Durum);
            var kalem = db.OdemeKalemleri.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Arac == "HAVALE");
            Assert.Null(kalem.KasaId);
        }
        Assert.DoesNotContain((await t.GetFromJsonAsync<JsonElement>("/api/v1/islemler/havale-bekleyen")).EnumerateArray(),
            x => x.GetProperty("id").GetString() == dekont);

        // Aynı havale ikinci kez kullanılamaz
        var ikinci = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/islemler/satis-fisi",
            Fis(k, 10_000m, new { arac = "HAVALE", tutarTl = 10_000m, bankaHareketId = dekont }));
        Assert.Equal(HttpStatusCode.Conflict, ikinci.StatusCode);

        // Satışa bağlı havale stornolanamaz, kararı geri alınamaz
        var s = f.Ortam.Istemci(k, "SAHIP");
        var hesap = (await f.Vt.OlusturSistem().BankaHareketleri.AsNoTracking().SingleAsync(x => x.Id == dekont)).BankaHesapId;
        Assert.Equal(HttpStatusCode.Conflict, (await s.PostAsJsonAsync($"/api/v1/bankalar/{hesap}/hareketler/{dekont}/storno", new { gerekce = "deneme için yanlış girildi sanıldı" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await s.PostAsync($"/api/v1/bankalar/hareketler/{dekont}/karar/geri-al", null)).StatusCode);

        // Düzeltme yolu SATIŞ İADESİ (satış stornolanmaz): havaleli satış nakit iade edilir; havale satışta kalır.
        string satisHareket;
        using (var db = f.Vt.OlusturSistem()) satisHareket = db.BankaHareketleri.AsNoTracking().Single(x => x.Id == dekont).HareketId!;
        var iade = await s.PostAsJsonAsync("/api/v1/islemler/satis-iade", new
        {
            kaynakHareketId = satisHareket, adet = 1m, odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId,
            aciklama = "müşteri vazgeçti",
        });
        Assert.True(iade.IsSuccessStatusCode, await iade.Content.ReadAsStringAsync());
        using (var db = f.Vt.OlusturSistem())
        {
            var b = db.BankaHareketleri.AsNoTracking().Single(x => x.Id == dekont);
            Assert.Equal(Kuyumcu.Api.Modules.Banka.CariKarari.Satis, b.CariKarari);
            Assert.Equal(satisHareket, b.HareketId);
            Assert.Equal(kasaOnce - 10_000m, db.Kasalar.AsNoTracking().Single(x => x.Id == k.KasaId).Bakiye);   // para iadesi kasadan
        }
        Assert.DoesNotContain((await t.GetFromJsonAsync<JsonElement>("/api/v1/islemler/havale-bekleyen")).EnumerateArray(),
            x => x.GetProperty("id").GetString() == dekont);
    }

    [Fact]
    public async Task Hatali_havale_bacaklari_400_ve_hicbir_sey_yazilmaz()
    {
        var k = Kur("red");
        var (_, dekont) = await Havale(k, 10_000m);
        var s = f.Ortam.Istemci(k, "SAHIP");
        foreach (var odeme in new object[]
        {
            new { arac = "HAVALE", tutarTl = 10_000m, bankaHareketId = dekont, kasaId = k.KasaId },   // kasa verilemez
            new { arac = "KART", tutarTl = 10_000m, bankaHareketId = dekont, kasaId = k.KasaId },     // yalnız havale
        })
            Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis(k, 10_000m, odeme))).StatusCode);
        // tutar havaleye eşit değil (fiş 9.000, havale 10.000)
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync("/api/v1/islemler/satis-fisi",
            Fis(k, 9_000m, new { arac = "HAVALE", tutarTl = 9_000m, bankaHareketId = dekont }))).StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.Hareketler.Count(h => h.TenantId == k.TenantId && h.Tur == "SATIS"));
        Assert.Null(db.BankaHareketleri.AsNoTracking().Single(x => x.Id == dekont).CariKarari);
    }
}
