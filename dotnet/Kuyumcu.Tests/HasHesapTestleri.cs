using Kuyumcu.Core.Domain;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// Çekirdek hesap — veritabanı gerektirmez.
// Has gram = (gram − taş) × milyem / 1000, 6 hane hassasiyet.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class HasHesapTestleri
{
    [Theory]
    [InlineData("K8", 333)]
    [InlineData("K14", 585)]
    [InlineData("K18", 750)]
    [InlineData("K21", 875)]
    [InlineData("K22", 916)]
    [InlineData("K24", 1000)]
    [InlineData("G800", 800)]
    [InlineData("G925", 925)]
    [InlineData("G950", 950)]
    public void Ayarlar_MilyemDegerleriSabit(string ayarId, int milyem)
    {
        Assert.Equal(milyem, Ayarlar.Bul(ayarId)!.Milyem);
    }

    [Fact]
    public void HasGram_YirmiIkiAyar()
    {
        Assert.Equal(7.236400m, HasHesap.HasGramHesapla(7.9m, 0m, "K22"));
    }

    [Fact]
    public void HasGram_TasAgirligiDusulur()
    {
        // (10 − 1,5) × 0,916 = 7,786
        Assert.Equal(7.786000m, HasHesap.HasGramHesapla(10m, 1.5m, "K22"));
    }

    [Fact]
    public void HasGram_YirmiDortAyarSafAltinAynenKalir()
    {
        Assert.Equal(5.000000m, HasHesap.HasGramHesapla(5m, 0m, "K24"));
    }

    [Fact]
    public void HasGram_GumusMilyemiUygulanir()
    {
        Assert.Equal(11.100000m, HasHesap.HasGramHesapla(12m, 0m, "G925"));
    }

    [Fact]
    public void HasGram_GercekMilyemAyarStandardininYerineGecer()
    {
        // İmalattaki fiili saflık 900 — ayar standardı 916 kullanılmamalı
        var gercek = Ayarlar.Bul("K22")! with { Milyem = 900m };
        Assert.Equal(7.110000m, HasHesap.ToHasGram(7.9m, gercek));
    }

    [Fact]
    public void HasGram_AltiHaneyeYuvarlanir()
    {
        // 1,2345678 × 0,916 = 1,13086411... → 1,130864
        Assert.Equal(1.130864m, HasHesap.HasGramHesapla(1.2345678m, 0m, "K22"));
    }

    [Fact]
    public void HasGram_GecersizAyarReddedilir()
    {
        Assert.Throws<ArgumentException>(() => HasHesap.HasGramHesapla(10m, 0m, "K19"));
        Assert.Null(Ayarlar.Bul("K19"));
    }
}
