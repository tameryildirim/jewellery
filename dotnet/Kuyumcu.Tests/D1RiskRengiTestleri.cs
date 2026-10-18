using Kuyumcu.Contracts.Sayfalar.Cariler;
using Kuyumcu.Web.Bilesenler.Yildiz;

namespace Kuyumcu.Tests;

// D1-07 — müşteri risk rengi (saf): kara liste / 30+ gün gecikme kırmızı; gecikme ya da limitin %80'i "dikkat".
public sealed class D1RiskRengiTestleri
{
    private static CariVeri C(decimal tl, decimal? limit = null, bool engel = false) =>
        new("c1", "Selin", "BIREYSEL", null, null, tl, 0m, VeresiyeEngeli: engel, VeresiyeEngelSebebi: engel ? "ödemedi" : null, KrediLimitiTl: limit);

    [Fact]
    public void Risk_kurallari()
    {
        Assert.Null(YildizMusteriBicim.Risk(C(1000m), null));
        Assert.Equal("kirmizi", YildizMusteriBicim.Risk(C(0m, engel: true), null));
        Assert.Equal("kirmizi", YildizMusteriBicim.Risk(C(1000m), 31));
        Assert.Equal("sari", YildizMusteriBicim.Risk(C(1000m), 3));
        Assert.Equal("sari", YildizMusteriBicim.Risk(C(8000m, 10000m), null));
        Assert.Null(YildizMusteriBicim.Risk(C(7999m, 10000m), null));
    }

    [Fact]
    public void Kara_listedeki_musteri_satirinda_veresiye_kapali_yazar_ve_kirmizidir()
    {
        var s = YildizMusteriBicim.Satir(C(500m, engel: true), null, null, "0532", "tr");
        Assert.StartsWith("veresiye kapalı", s.Durum);
        Assert.Equal("kirmizi", s.Risk);
    }
}
