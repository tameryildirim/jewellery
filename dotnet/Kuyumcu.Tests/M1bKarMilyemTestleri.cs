using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Maliyet;
using Kuyumcu.Api.Modules.Urun;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// M1b — kâr-zarar raporu ALIŞ milyeminden maliyet bilir (müşteri 16.10: "has değeri
// maliyet olarak gözüküyor… 3.600 kâr etmiş görünürsem üstüne vergi alır").
// Seçim sırası: parti maliyeti → kart TL maliyeti → alış milyemi × satışın KENDİ kuru.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class M1bKarMilyemTestleri(TestVeritabani vt)
{
    private static DateOnly Bugun => Zaman.YerelBugun();
    private static JsonElement J(object o) => JsonDocument.Parse(JsonSerializer.Serialize(o)).RootElement;

    private static async Task<string> MilyemliUrun(KuyumcuDbContext db, Kiraci k, string kod, decimal? alisMilyemi, decimal? kartTl = null)
    {
        await new UrunServisi(db).Olustur(new UrunOlusturDto
        {
            Tip = "ZIYNET", Kod = kod, Ad = "Bilezik " + kod, AyarId = "K22", GramAgirlik = 4m,
            AlisMilyemi = alisMilyemi, AlisMaliyeti = kartTl, AcilisStogu = new AcilisStoguDto { Adet = 3m },
        }, k.Islem);
        return (await db.Urunler.AsNoTracking().SingleAsync(u => u.TenantId == k.TenantId && u.Kod == kod)).Id;
    }

    private static Task Sat(KuyumcuDbContext db, Kiraci k, string urunId, decimal tutar) =>
        new MSatis.SatisServisi(db, NullLogger<MSatis.SatisServisi>.Instance).Satis(new MSatis.SatisOlusturDto
        {
            UrunId = urunId, Adet = 1m, BirimFiyatTl = tutar, OdemeTuru = "NAKIT", KasaId = k.KasaId, KurSnapshotId = k.KurId,
        }, k.Islem);

    [Fact]
    public async Task Alis_milyemi_olan_urunun_kari_satis_eksi_milyem_maliyetidir_has_bacagi_da_bilinir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "M1b-kar");
        using var db = vt.Olustur(k);
        var urun = await MilyemliUrun(db, k, "M1B-925", 925m);
        await Sat(db, k, urun, 26_000m);   // nakit sınırının (30.000) altında

        // maliyet has = 4 × 0,925 = 3,7 g; TL = 3,7 × 6.827,20 (test kuru) = 25.260,64
        var d = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal("TAM", d.GetProperty("karDurumu").GetString());
        Assert.Equal(25_260.64m, d.GetProperty("maliyetTl").GetDecimal());
        Assert.Equal(739.36m, d.GetProperty("karTl").GetDecimal());
        Assert.Equal(1m, d.GetProperty("kartMaliyetliAdet").GetDecimal());
        Assert.NotEqual(JsonValueKind.Null, d.GetProperty("karHas").ValueKind);   // has bacağı artık BİLİNİR
        // M9: milyem maliyeti satış kuruyla → altın fiyat farkı 0, kârın tamamı makas (işçilik) kârı.
        Assert.Equal(739.36m, d.GetProperty("makasKariTl").GetDecimal());
        Assert.Equal(0m, d.GetProperty("altinFiyatKariTl").GetDecimal());

        var u = J(await new KarZararServisi(db).UrunBazli(k.Islem, Bugun, Bugun, null, 50));
        var satir = Assert.Single(u.GetProperty("satirlar").EnumerateArray());
        Assert.Equal(739.36m, satir.GetProperty("karTl").GetDecimal());
        Assert.Equal(739.36m, satir.GetProperty("makasKariTl").GetDecimal());
        Assert.Equal(0m, satir.GetProperty("altinFiyatKariTl").GetDecimal());
    }

    [Fact]
    public async Task Kart_TL_maliyeti_varsa_alis_milyemine_ONCELIKLIDIR()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "M1b-kart");
        using var db = vt.Olustur(k);
        var urun = await MilyemliUrun(db, k, "M1B-KART", 925m, kartTl: 24_000m);
        await Sat(db, k, urun, 26_000m);

        var d = J(await new KarZararServisi(db).Donem(k.Islem, Bugun, Bugun, null));
        Assert.Equal(2_000m, d.GetProperty("karTl").GetDecimal());
        Assert.Equal(JsonValueKind.Null, d.GetProperty("karHas").ValueKind);
    }

    [Fact]
    public void Secim_sirasi_parti_kart_milyem()
    {
        var kart = new Dictionary<string, decimal> { ["k"] = 100m };
        var milyem = new Dictionary<string, decimal> { ["m"] = 4.625m };
        var kur = new Dictionary<string, decimal> { ["h"] = 6_000m };
        Assert.Equal((50m, 0.01m, false), KarZararServisi.BirimMaliyetSec(50m, 0.01m, "k", "h", kart, milyem, kur));
        Assert.Equal((100m, null, true), KarZararServisi.BirimMaliyetSec(null, null, "k", "h", kart, milyem, kur));
        Assert.Equal((27_750m, 4.625m, true), KarZararServisi.BirimMaliyetSec(null, null, "m", "h", kart, milyem, kur));
        Assert.Equal((null, null, false), KarZararServisi.BirimMaliyetSec(null, null, "m", "kursuz", kart, milyem, kur));
    }
}
