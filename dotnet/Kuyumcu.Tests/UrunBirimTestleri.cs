using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MSatis = Kuyumcu.Api.Modules.Satis;
using MUrun = Kuyumcu.Api.Modules.Urun;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ÇİFT BİRİM STOK — paket 21. GRAM üründe 1 adet = 1 g sözleşmesi SUNUCUDA zorlanır;
// mevcut adet×has ve adet×fiyat hesapları gramlı stoğu değiştirmeden taşır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class UrunBirimTestleri(TestVeritabani vt)
{
    private static MUrun.UrunServisi Urun(KuyumcuDbContext db) => new(db);
    private static MSatis.SatisServisi Satis(KuyumcuDbContext db) => new(db, NullLogger<MSatis.SatisServisi>.Instance);
    private static T Prop<T>(object o, string ad) => (T)o.GetType().GetProperties().Single(p => string.Equals(p.Name, ad, StringComparison.OrdinalIgnoreCase)).GetValue(o)!;

    private static MUrun.UrunOlusturDto Dto(string birim, decimal gram) => new()
    {
        Tip = "KULCE", Kod = $"GR-{Kimlik.Ulid()[^6..]}", Ad = "Granül 24 Ayar", AyarId = "K995",
        GramAgirlik = gram, Birim = birim,
    };

    [Fact]
    public async Task GRAM_urunde_gram_agirlik_1e_ZORLANIR_has_gram_basina_olur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Birim gram");
        using var db = vt.Olustur(k.TenantId);
        var y = await Urun(db).Olustur(Dto("GRAM", gram: 250m), k.Islem);   // istemci 250 gönderdi
        var id = Prop<string>(y, "id");
        using var kontrol = vt.OlusturSistem();
        var u = kontrol.Urunler.AsNoTracking().Single(x => x.Id == id);
        Assert.Equal("GRAM", u.Birim);
        Assert.Equal(1m, u.GramAgirlik);
        Assert.Equal(0.995m, u.HasGrami);          // 1 g × 995/1000
    }

    [Fact]
    public async Task Varsayilan_birim_ADET_ve_gram_agirlik_korunur()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Birim adet");
        using var db = vt.Olustur(k.TenantId);
        var dto = Dto("ADET", gram: 7.5m);
        var y = await Urun(db).Olustur(dto, k.Islem);
        using var kontrol = vt.OlusturSistem();
        var u = kontrol.Urunler.AsNoTracking().Single(x => x.Id == Prop<string>(y, "id"));
        Assert.Equal("ADET", u.Birim);
        Assert.Equal(7.5m, u.GramAgirlik);
    }

    /// <summary>2,5 g satış → stok 2,5 g düşer, has bacağı 2,5 × 0,995 = 2,4875.</summary>
    [Fact]
    public async Task Gramli_urun_KESIRLI_miktarla_satilir_stok_ve_has_gram_uzerinden_duser()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Birim satış");
        using var db = vt.Olustur(k.TenantId);
        var id = Prop<string>(await Urun(db).Olustur(Dto("GRAM", 1m), k.Islem), "id");
        await Satis(db).Alis(new MSatis.AlisOlusturDto { CariId = k.CariId, UrunId = id, Adet = 10m, BirimFiyatTl = 6_000m, OdemeTuru = "HAVALE", OdenenTl = 60_000m, KasaId = k.KasaId, KurSnapshotId = k.KurId }, k.Islem);
        await Satis(db).Satis(new MSatis.SatisOlusturDto { CariId = k.CariId, UrunId = id, Adet = 2.5m, BirimFiyatTl = 6_500m, OdemeTuru = "KART", KasaId = k.KasaId, KurSnapshotId = k.KurId }, k.Islem);

        using var kontrol = vt.OlusturSistem();
        Assert.Equal(7.5m, kontrol.Stoklar.AsNoTracking().Single(s => s.UrunId == id).Adet);
        var h = kontrol.Hareketler.AsNoTracking().Single(x => x.UrunId == id && x.Tur == "SATIS");
        Assert.Equal(16_250m, h.TlTutari);
        Assert.Equal(-2.4875m, h.HasGrami);
    }

    [Fact]
    public async Task Guncellemede_GRAMa_gecis_gram_agirligi_1e_ceker()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "Birim güncelle");
        using var db = vt.Olustur(k.TenantId);
        var id = Prop<string>(await Urun(db).Olustur(Dto("ADET", 12m), k.Islem), "id");
        await Urun(db).Guncelle(id, new MUrun.UrunGuncelleDto { Birim = "GRAM" }, k.Islem);
        using var kontrol = vt.OlusturSistem();
        var u = kontrol.Urunler.AsNoTracking().Single(x => x.Id == id);
        Assert.Equal(1m, u.GramAgirlik);
        Assert.Equal(0.995m, u.HasGrami);
    }
}
