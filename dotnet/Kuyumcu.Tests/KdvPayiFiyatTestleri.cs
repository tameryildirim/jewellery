using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Fatura;
using Kuyumcu.Api.Modules.Fiyat;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// Fiyat kuralı 8 — KDV PAYI FİYATIN ÜSTÜNE (müşteri 16.10 ses kaydı):
//   04:59 "binde bir gir. Diyoruz ki 60 lira da KDV'ye. 05:07 Bunun üzerine ekle, fiyatın içinde onu ekle."
//   26:43 "100 bin lira girecek. Binde bir 100 bin lira. 100 bin liranda %20 çekici olacak."
// Faturada KDV = toplam × binde/1000 × oran (OzelMatrahMotoru.HesaplaBinde). Fiyat bu KDV'yi üstüne
// ekler: toplam − KDV = KDV'den önceki fiyat (kârsız bilezik KDV kadar zarar etmez).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class KdvPayiFiyatTestleri
{
    private static readonly FiyatListesi Liste = new() { Id = "l", TenantId = "t", Ad = "P", HasMarjYuzde = 0m, IscilikCarpanYuzde = 100m };

    // 10 g 22 ayar (916): has 9,16 g. Alış milyemi 925 (maliyet), satış milyemi boş → kârsız.
    private static Urun Bilezik(decimal? binde, decimal? kdvYuzde = null) => new()
    {
        Id = "u", TenantId = "t", Tip = "ZIYNET", Kod = "B", Ad = "Bilezik", AyarId = "K22",
        Milyem = 916m, HasGrami = 9.16m, AlisMilyemi = 925m, KdvMatrahBinde = binde, KdvOrani = kdvYuzde,
    };

    [Fact]
    public void Binde_1_KDV_payi_fiyatin_USTUNE_eklenir_fatura_KDVsi_cikinca_maliyet_TAM_kalir()
    {
        const decimal kur = 6578.38m;                                    // 10 g × 0,925 × kur ≈ 60.850
        var t = FiyatMotoru.Hesapla(Bilezik(1m), kur, Liste, null)!;
        var oncesi = t.ListeFiyati - t.KdvPayiTl;
        Assert.Equal(Math.Round(10m * 0.925m * kur, 2, MidpointRounding.AwayFromZero), oncesi);
        Assert.True(t.KdvPayiTl > 0m);

        var fatura = OzelMatrahMotoru.HesaplaBinde([], t.SatisFiyati, 1m, 0.20m);
        Assert.Equal(t.KdvPayiTl, fatura.KdvTutar);                      // fiyattaki pay = faturadaki KDV
        Assert.Equal(oncesi, t.SatisFiyati - fatura.KdvTutar);            // kuyumcunun eline maliyet TAM kalır
    }

    [Fact]
    public void Kayittaki_ornek_100_bin_TL_binde_1_yuzde_20_KDV_20_TL()
    {
        var f = OzelMatrahMotoru.HesaplaBinde([], 100_000m, 1m, 0.20m);
        Assert.Equal(100m, f.Matrah);
        Assert.Equal(20m, f.KdvTutar);
    }

    [Fact]
    public void Urunun_kendi_KDV_orani_varsa_o_kullanilir()
    {
        var t20 = FiyatMotoru.Hesapla(Bilezik(1m), 6000m, Liste, null)!;
        var t10 = FiyatMotoru.Hesapla(Bilezik(1m, kdvYuzde: 10m), 6000m, Liste, null)!;
        Assert.True(t10.KdvPayiTl < t20.KdvPayiTl);
        Assert.Equal(t10.ListeFiyati - t10.KdvPayiTl, t20.ListeFiyati - t20.KdvPayiTl);
    }

    [Fact]
    public void Binde_yoksa_ya_da_sabit_fiyatsa_KDV_payi_EKLENMEZ()
    {
        Assert.Equal(0m, FiyatMotoru.Hesapla(Bilezik(null), 6000m, Liste, null)!.KdvPayiTl);
        var sabit = Bilezik(1m); sabit.SatisFiyati = 50_000m;
        var t = FiyatMotoru.Hesapla(sabit, 6000m, Liste, null)!;
        Assert.Equal(0m, t.KdvPayiTl);
        Assert.Equal(50_000m, t.SatisFiyati);
    }
}
