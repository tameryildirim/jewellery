using System.Text.RegularExpressions;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Modules.Urun;
using Kuyumcu.Core.Domain;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 20 Faz A — MADEN MODELİ VE KAPISI (saf testler, veritabanı yok).
//
// Kanıtlanan şey: "has gram" bir ALTIN kavramıdır; ayar → maden eşlemesi TEK
// kaynaktan gelir; tip ile ayarın madeni tutarsızsa ürün kaydedilemez.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class MadenTestleri
{
    // ── Ayar → maden eşlemesi ────────────────────────────────────────────────

    [Theory]
    [InlineData("K22", Maden.Altin)]
    [InlineData("K995", Maden.Altin)]
    [InlineData("G925", Maden.Gumus)]
    [InlineData("PT950", Maden.Platin)]
    [InlineData("PD999", Maden.Paladyum)]
    [InlineData("CELIK", Maden.Celik)]
    public void HerAyarinMadeniDogruCozulur(string ayarId, string beklenen)
        => Assert.Equal(beklenen, Ayarlar.MadenBul(ayarId));

    [Fact]
    public void BilinmeyenAyarinMadeniNull()
        => Assert.Null(Ayarlar.MadenBul("K23"));

    /// <summary>K20-5: çelik kıymetsizdir — has gramı üretilmez.</summary>
    [Fact]
    public void CelikAyarindaToHasGramSifirDoner()
    {
        var celik = Ayarlar.Bul("CELIK")!;
        Assert.False(celik.Kiymetli);
        Assert.False(celik.HasHesaplanir);
        Assert.Equal(0m, HasHesap.ToHasGram(40m, celik));
        Assert.Equal(0m, HasHesap.HasGramHesapla(40m, 0m, "CELIK"));
    }

    /// <summary>
    /// Çelikte "gerçek milyem" elle 900 yazılsa bile has üretilemez:
    /// <c>HasHesaplanir=false</c> milyemden ÖNCE gelir. Aksi hâlde defterde
    /// sahte has altın doğardı.
    /// </summary>
    [Fact]
    public void CelikteGercekMilyemBileHasUretmez()
    {
        var sahte = Ayarlar.Bul("CELIK")! with { Milyem = 900m };
        Assert.Equal(0m, HasHesap.ToHasGram(40m, sahte));
    }

    /// <summary>Rodyum bir STOK MADENİ DEĞİLDİR (K20-7).</summary>
    [Fact]
    public void RodyumStokMadeniDegildir()
    {
        Assert.DoesNotContain(Maden.Rodyum, Maden.Hepsi);
        Assert.DoesNotContain(Ayarlar.Tumu.Values, a => a.Maden == Maden.Rodyum);
        Assert.Equal("Rodyum", Maden.Ad(Maden.Rodyum));
    }

    // ── Desen tek kaynaktan üretilir (K20-8, mutasyon M20-6) ─────────────────

    /// <summary>
    /// Desen ELLE YAZILMAZ: <c>Ayarlar.Tumu</c>'nden üretilir. Ölçü DAVRANIŞTIR
    /// (kaynak taraması değil): her geçerli ayar eşleşir, uydurma ayar eşleşmez.
    /// </summary>
    [Fact]
    public void DesenTumAyarlariKapsar()
    {
        var re = new Regex(Ayarlar.DesenTam);
        foreach (var id in Ayarlar.Tumu.Keys)
            Assert.True(re.IsMatch(id), $"Desen '{id}' ayarını kapsamıyor.");

        Assert.DoesNotMatch(re, "K23");
        Assert.DoesNotMatch(re, "G9250");
        Assert.DoesNotMatch(re, "");
        // Çelik desene KENDİLİĞİNDEN girmiş olmalı — elle eklenmiş bir dizge
        // olsaydı bu satır kırmızı olurdu.
        Assert.Matches(re, "CELIK");
        Assert.Equal(Ayarlar.Tumu.Count, Ayarlar.Desen.Split('|').Length);
    }

    /// <summary>Uzun anahtar önce: "K2|K24" sırasında K24 hiç eşleşmezdi.</summary>
    [Fact]
    public void DesenUzunAnahtariOnceDener()
    {
        var re = new Regex($"^(?:{Ayarlar.Desen})");
        Assert.Equal("K995", re.Match("K995").Value);
        Assert.Equal("PT950", re.Match("PT950").Value);
    }

    [Fact]
    public void AyarIdOzniteligiSozlugeBaglidir()
    {
        var o = new AyarIdAttribute();
        Assert.True(o.IsValid("CELIK"));
        Assert.True(o.IsValid("PT950"));
        Assert.False(o.IsValid("K23"));
        // Boş/null zorunluluk DEĞİL doğruluk sorusudur — [Required] ayrı iştir
        // (PATCH sözleşmesi "gönderilmeyen alan korunur" bozulmasın).
        Assert.True(o.IsValid(null));
        Assert.True(o.IsValid(""));
    }

    // ── MadenKapisi.UrunDogrula ──────────────────────────────────────────────

    /// <summary>K20-6, mutasyon M20-1.</summary>
    [Fact]
    public void TipVeAyarMadeniTutarsizsaRed()
    {
        var x = Assert.Throws<BadRequestException>(() =>
            MadenKapisi.UrunDogrula("GUMUS", "K22", false, null));
        Assert.Contains("gümüş", x.Message);

        Assert.Throws<BadRequestException>(() =>
            MadenKapisi.UrunDogrula("ZIYNET", "PT950", false, null));
        Assert.Throws<BadRequestException>(() =>
            MadenKapisi.UrunDogrula("PLATIN", "PD999", false, 1m));
    }

    /// <summary>PIRLANTA/SAAT/DEGERLI_TAS maden dayatmaz — kasa her madenden olabilir.</summary>
    [Fact]
    public void SerbestTiplerHerMadeniKabulEder()
    {
        MadenKapisi.UrunDogrula("SAAT", "CELIK", false, 2_500m);
        MadenKapisi.UrunDogrula("PIRLANTA", "K18", false, null);
        MadenKapisi.UrunDogrula("DEGERLI_TAS", "PT950", false, null);
    }

    /// <summary>K20-5, mutasyon M20-4: çelikte satış fiyatı ZORUNLU.</summary>
    [Fact]
    public void CelikUrunFiyatsizKaydedilemez()
    {
        var x = Assert.Throws<BadRequestException>(() =>
            MadenKapisi.UrunDogrula("CELIK", "CELIK", false, null));
        Assert.Contains("satış fiyatı zorunludur", x.Message);

        Assert.Throws<BadRequestException>(() =>
            MadenKapisi.UrunDogrula("CELIK", "CELIK", false, 0m));

        MadenKapisi.UrunDogrula("CELIK", "CELIK", false, 2_500m);   // fiyat varsa geçer
    }

    /// <summary>K20-7, mutasyon M20-5: rodyum yalnız altın/platin/paladyumda.</summary>
    [Fact]
    public void RodyumKaplamaCelikUrundeRed()
    {
        var x = Assert.Throws<BadRequestException>(() =>
            MadenKapisi.UrunDogrula("CELIK", "CELIK", true, 2_500m));
        Assert.Contains("Rodyum", x.Message);

        Assert.Throws<BadRequestException>(() =>
            MadenKapisi.UrunDogrula("GUMUS", "G925", true, null));

        MadenKapisi.UrunDogrula("ZIYNET", "K18", true, null);       // beyaz altın
        MadenKapisi.UrunDogrula("PLATIN", "PT950", true, null);
    }

    [Fact]
    public void GecersizAyarKapidaRed()
        => Assert.Throws<BadRequestException>(() =>
            MadenKapisi.UrunDogrula("ZIYNET", "K23", false, null));

    // ── MadenKapisi.HareketDogrula (K20-1) ───────────────────────────────────

    [Fact]
    public void HasBacagiSifirsaMadenAranmaz()
    {
        // TAHSILAT gibi TL-only satırlarda maden bacağı yoktur.
        MadenKapisi.HareketDogrula(new Kuyumcu.Api.Data.Hareket
        {
            Id = "h", TenantId = "t", SubeId = "s", Tur = "TAHSILAT",
            TlTutari = 1_000m, HasGrami = 0m, Maden = "",
        });
    }

    [Fact]
    public void MadensizHasBacagiPatlar()
    {
        var x = Assert.Throws<InvalidOperationException>(() =>
            MadenKapisi.HareketDogrula(new Kuyumcu.Api.Data.Hareket
            {
                Id = "h", TenantId = "t", SubeId = "s", Tur = "SATIS",
                TlTutari = 1_000m, HasGrami = -9.16m, Maden = "  ",
            }));
        Assert.Contains("madensiz", x.Message);
    }

    [Fact]
    public void BilinmeyenMadenPatlar()
    {
        var x = Assert.Throws<InvalidOperationException>(() =>
            MadenKapisi.HareketDogrula(new Kuyumcu.Api.Data.Hareket
            {
                Id = "h", TenantId = "t", SubeId = "s", Tur = "SATIS",
                TlTutari = 1_000m, HasGrami = -9.16m, Maden = "RODYUM",
            }));
        Assert.Contains("Bilinmeyen maden", x.Message);
    }
}
