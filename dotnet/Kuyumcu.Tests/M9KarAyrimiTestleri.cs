using Kuyumcu.Api.Modules.Maliyet;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// M9 — kâr raporunda altın kârının iki parçası (müşteri 16.10: "3.600 kâr etmiş görürsem
// üstüne vergi alır… hiç kâr etmediğim şeyden"). Has kârı (g) zaten vardı; TL kârı artık
// makas (işçilik) kârı + altın fiyat farkı kârı olarak ayrılır ve toplamı TL kârına eşittir.
// 10 g bilezik, alış milyemi 925 → has maliyeti 9,25 g.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class M9KarAyrimiTestleri
{
    private static (decimal? Makas, decimal? Altin) Ayir(decimal satisTl, decimal maliyetTl, decimal maliyetHas, decimal satisKuru)
        => KarZararServisi.KarAyrimi(
            [satisTl],
            [("s1", 1m, maliyetTl, maliyetHas)],
            new Dictionary<string, decimal> { ["s1"] = satisKuru });

    [Fact]
    public void Karsiz_bilezik_ayni_kurda_alinip_satilinca_iki_parca_da_sifir()
    {
        var (makas, altin) = Ayir(satisTl: 9.25m * 6000m, maliyetTl: 9.25m * 6000m, maliyetHas: 9.25m, satisKuru: 6000m);
        Assert.Equal(0m, makas);
        Assert.Equal(0m, altin);
    }

    [Fact]
    public void Altin_yukselince_kazanc_ALTIN_FIYAT_farkina_yazilir_makas_sifir_kalir()
    {
        // 6.000'den alındı, 6.200'den aynı milyemle (925) satıldı.
        var (makas, altin) = Ayir(satisTl: 9.25m * 6200m, maliyetTl: 9.25m * 6000m, maliyetHas: 9.25m, satisKuru: 6200m);
        Assert.Equal(0m, makas);
        Assert.Equal(1850m, altin);                                   // 9,25 × 200
    }

    [Fact]
    public void Satis_milyemi_yuksekse_fark_MAKAS_karina_yazilir_ve_toplam_TL_karina_esit()
    {
        // 925'ten alındı, 950'den satıldı, kur 6.200 (alışta 6.000).
        var satis = 9.5m * 6200m;                                     // 58.900
        var maliyet = 9.25m * 6000m;                                  // 55.500
        var (makas, altin) = Ayir(satis, maliyet, 9.25m, 6200m);
        Assert.Equal(1550m, makas);                                   // 0,25 g × 6.200
        Assert.Equal(1850m, altin);                                   // 9,25 g × 200
        Assert.Equal(satis - maliyet, makas + altin);
    }

    [Fact]
    public void Iade_satiri_hasilattan_ve_maliyetten_dusulur()
    {
        var (makas, altin) = KarZararServisi.KarAyrimi(
            [58900m, -58900m],
            [("s1", 1m, 55500m, 9.25m), ("i1", -1m, 55500m, 9.25m)],
            new Dictionary<string, decimal> { ["s1"] = 6200m, ["i1"] = 6200m });
        Assert.Equal(0m, makas);
        Assert.Equal(0m, altin);
    }

    [Fact]
    public void Kuru_ya_da_has_maliyeti_bilinmeyen_satir_varsa_UYDURULMAZ_null()
    {
        Assert.Equal((null, null), KarZararServisi.KarAyrimi(
            [1000m], [("s1", 1m, 900m, 0.15m)], new Dictionary<string, decimal>()));
        Assert.Equal((null, null), KarZararServisi.KarAyrimi(
            [1000m], [("s1", 1m, 900m, null)], new Dictionary<string, decimal> { ["s1"] = 6000m }));
        Assert.Equal((null, null), KarZararServisi.KarAyrimi(
            [], [], new Dictionary<string, decimal>()));
    }
}
