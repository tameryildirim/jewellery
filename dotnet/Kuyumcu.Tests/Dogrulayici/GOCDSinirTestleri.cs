using System.Reflection;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Bilesenler.Grid;

namespace Kuyumcu.Tests.Dogrulayici;

// ═══════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET GOC-D finansal sınır testleri.
// Paket, kâr-zarar / satış analizi / rapor merkezi / işlem defteri tutar, gram ve
// has sütunlarını ortak ızgaraya taşıdı: hücre metni artık IzgaraKolonu.BicimliDeger
// yolundan basılıyor. Beklenen değerler ELLE yazıldı (üretim kodundan hesaplanmaz).
// Projenin para yuvarlaması: Bicim.Tl → MidpointRounding.AwayFromZero, 2 hane.
// ═══════════════════════════════════════════════════════════════════════════
public sealed class GOCDSinirTestleri
{
    private sealed record Satir(decimal? Deger);

    private static string Hucre(IzgaraHucreTuru tur, decimal? deger, string? paraBirimi = null)
    {
        var kolon = new IzgaraKolonu<Satir>
        {
            Anahtar = "k", Baslik = "k", Tur = tur, Deger = s => s.Deger,
            ParaBirimi = paraBirimi is null ? null : _ => paraBirimi,
        };
        var m = typeof(IzgaraKolonu<Satir>).GetMethod("BicimliDeger", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("BicimliDeger bulunamadı");
        return (string)m.Invoke(kolon, [new Satir(deger)])!;
    }

    private const string Nb = " ";

    [Fact]
    public void GDSN01_sifir_tutar_izgarada_0_TL() =>
        Assert.Equal("0" + Nb + "₺", Hucre(IzgaraHucreTuru.Tl, 0m));

    [Fact]
    public void GDSN02_eksi_tutar_isareti_eksi_isaretiyle_ve_kurusla() =>
        Assert.Equal("−1.234,50" + Nb + "₺", Hucre(IzgaraHucreTuru.Tl, -1234.5m));

    [Fact]
    public void GDSN03_kurus_yuvarlamasi_0_005_yukari_0_004_asagi()
    {
        Assert.Equal("0,01" + Nb + "₺", Hucre(IzgaraHucreTuru.Tl, 0.005m));
        Assert.Equal("0" + Nb + "₺", Hucre(IzgaraHucreTuru.Tl, 0.004m));
        Assert.Equal("−0,01" + Nb + "₺", Hucre(IzgaraHucreTuru.Tl, -0.005m));
        Assert.Equal("1,13" + Nb + "₺", Hucre(IzgaraHucreTuru.Tl, 1.125m));   // AwayFromZero (bankacı yuvarlaması 1,12 verirdi)
    }

    [Fact]
    public void GDSN04_has_dort_hane_gram_uc_hane_tr_TR()
    {
        Assert.StartsWith("0,0001 ", Hucre(IzgaraHucreTuru.Has, 0.0001m));
        Assert.StartsWith("1.234,5679 ", Hucre(IzgaraHucreTuru.Has, 1234.56789m));
        Assert.Equal("0,001 g", Hucre(IzgaraHucreTuru.Gram, 0.001m));
        Assert.DoesNotContain("TRY", Hucre(IzgaraHucreTuru.Para, 5m, "TRY"));
    }

    [Fact]
    public void GDSN05_cok_buyuk_tutar_999_999_999_99()
    {
        Assert.Equal("999.999.999,99" + Nb + "₺", Hucre(IzgaraHucreTuru.Tl, 999_999_999.99m));
        Assert.Equal("999.999.999,99 $", Hucre(IzgaraHucreTuru.Para, 999_999_999.99m, "USD"));
    }

    [Fact]
    public void GDSN06_bos_deger_tire_0_degil() =>
        Assert.Equal("—", Hucre(IzgaraHucreTuru.Tl, null));

    [Fact]
    public void GDSN07_turkce_sayi_girisi_SayiCoz()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
    }

    [Fact]
    public void GDSN08_rapor_donemi_artik_yil_400_gun_tam_sinir()
    {
        var bugun = new DateOnly(2026, 10, 9);
        // 29.02.2024'ü kapsayan 400 günlük dönem: 01.01.2024 → 04.02.2025 (366 + 34 = 400 gün, uçlar dahil)
        Assert.Null(RaporDonemKurali.Denetle(new DateOnly(2024, 1, 1), new DateOnly(2025, 2, 3), bugun, "tr"));
        Assert.NotNull(RaporDonemKurali.Denetle(new DateOnly(2024, 1, 1), new DateOnly(2025, 2, 4), bugun, "tr"));
        // tek gün (bas = bit) geçerli
        Assert.Null(RaporDonemKurali.Denetle(bugun, bugun, bugun, "tr"));
    }
}
