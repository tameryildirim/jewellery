using System.Globalization;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Cari;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// DÖVİZ BAKİYE EŞLEMESİ — SAF TESTLER (paket 27 Faz A).
// Veritabanı YOK: yuvarlama, para birimi kümesi ve kolon ayrımı tek tek sınanır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DovizBakiyeSaflikTestleri
{
    // ── Yuvarlama ───────────────────────────────────────────────────────────

    /// <summary>
    /// TL karşılığı 2 haneye ve <c>AwayFromZero</c> ile yuvarlanır (M?-yuvarlama).
    /// ⚠ Sayı <see cref="CultureInfo.InvariantCulture"/> ile biçimlenir: test
    /// projesinde kültür sabitlenmemiştir (Aşkın'ın koşulu), tr-TR'de ondalık
    /// ayırıcı virgül olur ve dizge karşılaştırması yanıltırdı.
    /// </summary>
    [Theory]
    // 100,005 → 100,01 (bankacı yuvarlaması 100,00 verirdi — AwayFromZero şart)
    [InlineData(10.0005, 10.0, "100.01")]
    [InlineData(1000, 40.25, "40250.00")]
    [InlineData(0.01, 0.005, "0.00")]       // 0,00005 → 0,00
    [InlineData(1, 1.005, "1.01")]
    public void TlKarsiligi_iki_hane_ve_AwayFromZero_yuvarlar(double miktar, double kur, string beklenen)
    {
        var sonuc = DovizBakiye.TlKarsiligi((decimal)miktar, (decimal)kur);
        Assert.Equal(beklenen, sonuc.ToString("F2", CultureInfo.InvariantCulture));
    }

    /// <summary>Negatif (borç kapatan) yönde de sıfırdan UZAĞA yuvarlanır.</summary>
    [Fact]
    public void TlKarsiligi_negatif_yonde_de_sifirdan_uzaga_yuvarlar()
    {
        var sonuc = DovizBakiye.TlKarsiligi(-1m, 1.005m);
        Assert.Equal("-1.01", sonuc.ToString("F2", CultureInfo.InvariantCulture));
    }

    // ── Para birimi kümesi ──────────────────────────────────────────────────

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    public void Gecerli_yalniz_uc_para_birimini_kabul_eder(string pb) =>
        Assert.True(DovizBakiye.Gecerli(pb));

    /// <summary>
    /// ⚠ DAVRANIŞ TESTİ (QA önerisi, birleşik koşul): "kaynakta ToUpperInvariant
    /// geçmiyor" taraması kırılgandır. Bunun yerine küçük harfli girdinin
    /// REDDEDİLDİĞİ sınanır — normalize edilseydi 'usd' geçerdi.
    /// </summary>
    [Theory]
    [InlineData("usd")]
    [InlineData("Usd")]
    [InlineData("TRY")]     // TRY tl_bakiye'dir, döviz DEĞİLDİR
    [InlineData("try")]
    [InlineData("CHF")]
    [InlineData("")]
    [InlineData(" USD")]
    [InlineData("USD ")]
    [InlineData(null)]
    public void Gecerli_normalize_ETMEZ_ve_kume_disini_reddeder(string? pb) =>
        Assert.False(DovizBakiye.Gecerli(pb));

    /// <summary>TRY özel mesaj alır: kullanıcı doğru uca yönlendirilir.</summary>
    [Fact]
    public void Dogrula_TRY_icin_dogru_yolu_soyleyen_400_verir()
    {
        var hata = Assert.Throws<BadRequestException>(() => DovizBakiye.Dogrula("TRY"));
        Assert.Contains("TRY", hata.Message);
        Assert.Contains("veresiye", hata.Message);
    }

    [Fact]
    public void Dogrula_CHF_icin_kabul_edilen_kumeyi_yazar()
    {
        var hata = Assert.Throws<BadRequestException>(() => DovizBakiye.Dogrula("CHF"));
        Assert.Contains("USD", hata.Message);
        Assert.Contains("EUR", hata.Message);
        Assert.Contains("GBP", hata.Message);
    }

    // ── Kolon ayrımı ────────────────────────────────────────────────────────

    /// <summary>
    /// <c>Ekle</c> YALNIZ hedef kolonu değiştirir. Bu değişmez olmasaydı bir
    /// carinin USD borcu EUR kolonuna yazılır ve ikisi de yanlış olurdu (M76).
    /// </summary>
    [Theory]
    [InlineData("USD", 100, 0, 0)]
    [InlineData("EUR", 0, 100, 0)]
    [InlineData("GBP", 0, 0, 100)]
    public void Ekle_yalniz_hedef_kolonu_degistirir(string pb, decimal usd, decimal eur, decimal gbp)
    {
        var b = new CariBakiye { CariId = "c", TenantId = "t" };
        DovizBakiye.Ekle(b, pb, 100m);
        Assert.Equal(usd, b.UsdBakiye);
        Assert.Equal(eur, b.EurBakiye);
        Assert.Equal(gbp, b.GbpBakiye);
        // TL ve has defterleri DOKUNULMAZ (toplama yasağının kod karşılığı).
        Assert.Equal(0m, b.TlBakiye);
        Assert.Equal(0m, b.HasGramBakiye);
    }

    [Fact]
    public void Ekle_birikimlidir_ve_negatif_delta_azaltir()
    {
        var b = new CariBakiye { CariId = "c", TenantId = "t" };
        DovizBakiye.Ekle(b, "USD", 1_000m);
        DovizBakiye.Ekle(b, "USD", -400m);
        Assert.Equal(600m, b.UsdBakiye);
        Assert.Equal(600m, DovizBakiye.Oku(b, "USD"));
    }

    [Fact]
    public void Ekle_ve_Oku_gecersiz_para_biriminde_PATLAR_sessiz_gecmez()
    {
        var b = new CariBakiye { CariId = "c", TenantId = "t" };
        Assert.Throws<BadRequestException>(() => DovizBakiye.Ekle(b, "TRY", 10m));
        Assert.Throws<BadRequestException>(() => DovizBakiye.Oku(b, "CHF"));
    }

    // ── Kur okuma (fail-closed) ─────────────────────────────────────────────

    [Fact]
    public void KurAl_kayittaki_kuru_dondurur_olmayani_null_verir()
    {
        var s = new KurSnapshot { Id = "k", TenantId = "t", UsdTry = 40m, EurTry = 44m, GbpTry = null };
        Assert.Equal(40m, DovizBakiye.KurAl(s, "USD"));
        Assert.Equal(44m, DovizBakiye.KurAl(s, "EUR"));
        Assert.Null(DovizBakiye.KurAl(s, "GBP"));
        Assert.Null(DovizBakiye.KurAl(s, "TRY"));
    }

    /// <summary>
    /// "Yaklaşık kur" YOKTUR: kur null ya da ≤ 0 ise istek reddedilir (M77).
    /// Kur 1 varsayılsaydı 1.000 USD borcu 1.000 TL sayılır, MASAK eşiği ve
    /// tevsik haddi sessizce delinirdi.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData(0d)]
    [InlineData(-1d)]
    public void KurZorunlu_kur_yok_veya_sifir_alti_ise_400_verir(double? kur)
    {
        var s = new KurSnapshot { Id = "k", TenantId = "t", UsdTry = kur is null ? null : (decimal)kur.Value };
        var hata = Assert.Throws<BadRequestException>(() => DovizBakiye.KurZorunlu(s, "USD"));
        Assert.Contains("kur", hata.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DovizTuru_yalniz_iki_defter_turunu_tanir()
    {
        Assert.True(DovizBakiye.DovizTuru("DOVIZ_VERESIYE"));
        Assert.True(DovizBakiye.DovizTuru("DOVIZ_TAHSILAT"));
        Assert.False(DovizBakiye.DovizTuru("VERESIYE"));
        Assert.False(DovizBakiye.DovizTuru("TAHSILAT"));
        Assert.False(DovizBakiye.DovizTuru(null));
    }

    /// <summary>Tür listesi GERÇEĞİ yansıtır (paket 33 Q2 kararıyla aynı gerekçe).</summary>
    [Fact]
    public void HareketTurleri_listesi_doviz_turlerini_icerir()
    {
        Assert.Contains("DOVIZ_VERESIYE", Sabitler.HareketTurleri);
        Assert.Contains("DOVIZ_TAHSILAT", Sabitler.HareketTurleri);
    }
}
