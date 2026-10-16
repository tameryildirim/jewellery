using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Fiyat;
using Kuyumcu.Api.Modules.Urun;
using Kuyumcu.Core.Domain.Girdi;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// M1 (müşteri görüşmesi 16.10: "bileziğin milyemi, çarpanı burada olacak, elimle
// müdahale edebileceğim; 925 ise 925") — satış milyemi + fiyat ofseti.
// Fiyat = net gram × satış milyemi/1000 × has; fark işçilik sütununa; genel liste marjı yok.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class M1SatisMilyemiTestleri
{
    // 15,2 g 22 ayar burma bilezik: has gram = 15,2 × 0,916 = 13,9232
    private static Urun Bilezik(decimal? satisMilyemi = null, decimal? ofset = null,
        decimal? iscilik = null, decimal? sabit = null, string ayar = "K22", decimal milyem = 916m) => new()
    {
        Id = "u1", TenantId = "t", Tip = "ZIYNET", Kod = "BLZ", Ad = "Burma Bilezik", AyarId = ayar,
        Milyem = milyem, GramAgirlik = 15.2m, HasGrami = 15.2m * milyem / 1000m, IscilikTl = iscilik,
        SatisFiyati = sabit, SatisMilyemi = satisMilyemi, FiyatOfsetTl = ofset,
    };
    private static FiyatListesi Perakende(decimal marj = 4m, decimal carpan = 100m) => new()
    {
        Id = "l1", TenantId = "t", Ad = "Perakende", HasMarjYuzde = marj, IscilikCarpanYuzde = carpan, Varsayilan = true,
    };

    [Fact]
    public void Bos_satis_milyemi_eski_davranisi_AYNEN_korur()
    {
        // 13,9232 × 6.600 = 91.893,12 · %4 = 3.675,72 → 95.568,84
        var t = FiyatMotoru.Hesapla(Bilezik(), 6_600m, Perakende(), null)!;
        Assert.Equal(91_893.12m, t.HasDegeri);
        Assert.Equal(3_675.72m, t.MarjTl);
        Assert.Equal(0m, t.IscilikTl);
        Assert.Equal(95_568.84m, t.ListeFiyati);
        Assert.Null(t.SatisMilyemi);
        Assert.Equal(0m, t.OfsetTl);
    }

    [Fact]
    public void Satis_milyemi_925_fiyati_gram_carpi_milyem_carpi_has_yapar_ve_marji_uygulamaz()
    {
        // 15,2 × 0,925 × 6.600 = 92.796,00; has değeri 91.893,12; fark 902,88 = milyem işçiliği
        var t = FiyatMotoru.Hesapla(Bilezik(satisMilyemi: 925m), 6_600m, Perakende(marj: 4m), null)!;
        Assert.Equal(92_796.00m, t.ListeFiyati);
        Assert.Equal(91_893.12m, t.HasDegeri);
        Assert.Equal(0m, t.MarjTl);                 // çift kâr yok
        Assert.Equal(902.88m, t.IscilikTl);         // KDV/kâr raporu işçiliği ayrı görür
        Assert.Equal(925m, t.SatisMilyemi);
        Assert.False(t.SabitFiyat);
        Assert.Equal(FiyatKaynagi.Liste, t.Kaynak);
    }

    [Fact]
    public void Sabit_TL_iscilik_satis_milyeminin_ustune_liste_carpaniyla_eklenir()
    {
        var t = FiyatMotoru.Hesapla(Bilezik(satisMilyemi: 925m, iscilik: 200m), 6_600m, Perakende(carpan: 150m), null)!;
        Assert.Equal(902.88m + 300m, t.IscilikTl);
        Assert.Equal(92_796.00m + 300m, t.ListeFiyati);
    }

    [Fact]
    public void Gecersiz_kilmada_ACIKCA_verilen_marj_satis_milyeminde_de_uygulanir()
    {
        var gk = new FiyatListesiUrunFiyati
        {
            Id = "g1", TenantId = "t", ListeId = "l1", UrunId = "u1", HasMarjYuzde = -1m, Aktif = true,
        };
        var t = FiyatMotoru.Hesapla(Bilezik(satisMilyemi: 925m), 6_600m, Perakende(marj: 4m), null, gk)!;
        Assert.Equal(-918.93m, t.MarjTl);           // 91.893,12 × −%1
        Assert.Equal(92_796.00m - 918.93m, t.ListeFiyati);
        Assert.Equal(FiyatKaynagi.UrunGecersizKilma, t.Kaynak);
    }

    [Theory]
    [InlineData(50, 92_846.00)]
    [InlineData(-96, 92_700.00)]
    public void Ofset_hesaplanan_fiyata_eklenir(decimal ofset, decimal beklenen)
    {
        var t = FiyatMotoru.Hesapla(Bilezik(satisMilyemi: 925m, ofset: ofset), 6_600m, Perakende(), null)!;
        Assert.Equal(beklenen, t.ListeFiyati);
        Assert.Equal(ofset, t.OfsetTl);
    }

    [Fact]
    public void Ofset_fiyati_eksiye_dusuremez()
    {
        var t = FiyatMotoru.Hesapla(Bilezik(ofset: -1_000_000m), 6_600m, Perakende(), null)!;
        Assert.Equal(0m, t.ListeFiyati);
        Assert.Equal(0m, t.SatisFiyati);
    }

    [Fact]
    public void Sabit_fiyatli_urunde_satis_milyemi_ve_ofset_UYGULANMAZ()
    {
        var t = FiyatMotoru.Hesapla(Bilezik(satisMilyemi: 990m, ofset: 500m, sabit: 100_000m), 6_600m, Perakende(), null)!;
        Assert.True(t.SabitFiyat);
        Assert.Equal(100_000m, t.ListeFiyati);
        Assert.Equal(0m, t.OfsetTl);
        Assert.Null(t.SatisMilyemi);
    }

    [Fact]
    public void Gumus_urunde_satis_milyemi_altin_kuruyla_CARPILMAZ_fail_closed()
    {
        // K20-4: altın dışı madene altın has kuru uygulanmaz; sabit fiyat yoksa teklif yok.
        var t = FiyatMotoru.Hesapla(Bilezik(satisMilyemi: 925m, ayar: "G925", milyem: 925m), 6_600m, Perakende(), null);
        Assert.Null(t);
    }

    [Fact]
    public void Gercek_milyemli_urunde_net_gram_gercek_milyemden_bulunur()
    {
        // Gerçek milyem 912: has 13,8624; net gram yine 15,2 → 15,2 × 0,925 × 6.600 = 92.796,00
        var t = FiyatMotoru.Hesapla(Bilezik(satisMilyemi: 925m, milyem: 912m), 6_600m, Perakende(), null)!;
        Assert.Equal(92_796.00m, t.ListeFiyati);
        Assert.Equal(91_491.84m, t.HasDegeri);
        Assert.Equal(1_304.16m, t.IscilikTl);
    }

    // ── Girdi kapısı (POST ve PATCH aynı kapı) ──

    [Theory]
    [InlineData(925)]
    [InlineData(1)]
    [InlineData(2000)]
    [InlineData(925.5)]
    public void Kapi_gecerli_satis_milyemini_kabul_eder(decimal m) =>
        UrunServisi.SatisMilyemiKapisi(m, null, "K22");

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(2000.01)]
    [InlineData(925.123)]
    public void Kapi_gecersiz_satis_milyemini_400_alanla_reddeder(decimal m)
    {
        var ex = Assert.Throws<BadRequestException>(() => UrunServisi.SatisMilyemiKapisi(m, null, "K22"));
        Assert.Equal("satisMilyemi", AlanIsareti.Alan(ex));
    }

    [Fact]
    public void Kapi_gumus_ayarda_satis_milyemini_reddeder()
    {
        var ex = Assert.Throws<BadRequestException>(() => UrunServisi.SatisMilyemiKapisi(925m, null, "G925"));
        Assert.Equal("satisMilyemi", AlanIsareti.Alan(ex));
    }

    [Fact]
    public void Kapi_ofseti_isaretli_kabul_eder_sifiri_temizlik_sayar()
    {
        UrunServisi.SatisMilyemiKapisi(null, -250.5m, "K22");
        UrunServisi.SatisMilyemiKapisi(null, 0m, "K22");
        var ex = Assert.Throws<BadRequestException>(() => UrunServisi.SatisMilyemiKapisi(null, 12.345m, "K22"));
        Assert.Equal("fiyatOfsetTl", AlanIsareti.Alan(ex));
    }

    [Fact]
    public void Core_kurali_ekranla_ayni_sinirlari_tasir()
    {
        Assert.Null(SatisMilyemiKurali.Denetle(925m));
        Assert.Null(SatisMilyemiKurali.Denetle(null));
        Assert.NotNull(SatisMilyemiKurali.Denetle(0m));
        Assert.NotNull(SatisMilyemiKurali.Denetle(2001m));
        Assert.NotNull(SatisMilyemiKurali.Denetle(925.001m));
    }
}
