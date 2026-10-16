using Kuyumcu.Contracts.Cikti;
using Kuyumcu.Web.Bilesenler.Yazdirma;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-K1 — PARA BİÇİMİ TUVALE ("Sade Yüz Taslağı", kurucu 24.09 17:20)
//
//   EKRAN   Bicim.Tl      "57.550 ₺" · "45.000,50 ₺" · "−22.000 ₺" (U+2212, NBSP)
//   KUR     Bicim.TlTam   has/gram 22 tam lira · Bicim.TlKurus USD/EUR 2 hane
//   KÂĞIT   Bicim.BelgePara / A4Belge  kuruş HER ZAMAN 2 hane ("45.000,00 ₺", VUK)
//   GİRDİ   Bicim.Girdi   DEĞİŞMEZ ("45000,00")
//   DÖVİZ   Bicim.Para    DEĞİŞMEZ ("1.250,00 $")
//
// ⚠ Bu dosya biçim SÖZLEŞMESİNİN kendisidir: beklenenler bilerek LİTERALDİR.
// Diğer ekran testleri beklenen değeri Bicim.* çağrısıyla üretir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TK1ParaBicimiTestleri
{
    private const string B = "\u00a0₺";   // bölünmez boşluk + ₺

    [Theory]
    [InlineData("57550", "57.550" + B)]
    [InlineData("45000.50", "45.000,50" + B)]
    [InlineData("45000.5", "45.000,50" + B)]
    [InlineData("0", "0" + B)]
    [InlineData("860", "860" + B)]
    [InlineData("1234567.899", "1.234.567,90" + B)]
    [InlineData("1.005", "1,01" + B)]
    [InlineData("99.999", "100" + B)]
    [InlineData("-22000", "\u221222.000" + B)]
    [InlineData("-125.5", "\u2212125,50" + B)]
    [InlineData("-0.004", "0" + B)]
    public void TK1_Tl_isaret_sonda_sifir_kurus_yazilmaz(string girdi, string beklenen)
    {
        var n = decimal.Parse(girdi, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(beklenen, Bicim.Tl(n));
    }

    [Fact]
    public void TK1_Tl_eksi_isareti_U2212_tire_DEGIL_ve_simge_basta_DEGIL()
    {
        var s = Bicim.Tl(-22000m);
        Assert.StartsWith("\u2212", s);
        Assert.DoesNotContain("-", s);
        Assert.False(s.StartsWith("₺"), s);
        Assert.DoesNotContain("TRY", s);
    }

    [Theory]
    [InlineData("45000", "45.000,00" + B)]
    [InlineData("41.95", "41,95" + B)]
    [InlineData("42", "42,00" + B)]
    [InlineData("-10", "\u221210,00" + B)]
    public void TK1_TlKurus_kurus_HER_ZAMAN_iki_hane(string girdi, string beklenen)
    {
        var n = decimal.Parse(girdi, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(beklenen, Bicim.TlKurus(n));
    }

    [Theory]
    [InlineData("6876.60", "6.877" + B)]
    [InlineData("6876.49", "6.876" + B)]
    [InlineData("6300.50", "6.301" + B)]
    [InlineData("6877", "6.877" + B)]
    public void TK1_TlTam_tam_liraya_yuvarlar(string girdi, string beklenen)
    {
        var n = decimal.Parse(girdi, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(beklenen, Bicim.TlTam(n));
    }

    [Fact]
    public void TK1_Para_TRY_ekran_bicimi_doviz_DEGISMEZ()
    {
        Assert.Equal(Bicim.Tl(57550m), Bicim.Para(57550m, "TRY"));
        Assert.Equal("57.550" + B, Bicim.Para(57550m, null));
        Assert.Equal("1.250,00 $", Bicim.Para(1250m, "USD"));
        Assert.Equal("1.250,00 €", Bicim.Para(1250m, "EUR"));
        Assert.Equal("1.250,00 £", Bicim.Para(1250m, "GBP"));
    }

    [Fact]
    public void TK1_Girdi_bicimi_DEGISMEZ_gruplamasiz()
    {
        Assert.Equal("45000,00", Bicim.Girdi(45000m, "0.00"));
        Assert.Equal("45000", Bicim.Girdi(45000m));
    }

    [Fact]
    public void TK1_Saat_HH_mm_iki_nokta_kulturden_BAGIMSIZ()
    {
        var onceki = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            // fi-FI saat ayırıcısı "." — kültürlü "HH:mm" burada "09.02" basardı.
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fi-FI");
            Assert.Equal("09:02", Bicim.Saat(new DateTime(2026, 9, 23, 9, 2, 59)));
            Assert.Equal("21:45", Bicim.Saat(new DateTime(2026, 9, 23, 21, 45, 0)));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = onceki; }
    }

    // ── KÂĞIT: kuruş her zaman iki hane (resmî belge tutarı, VUK) ─────────────

    [Fact]
    public void TK1_BelgePara_TRY_kurusu_HER_ZAMAN_yazar_ekrandan_AYRISIR()
    {
        Assert.Equal("45.000,00" + B, Bicim.BelgePara(45000m, "TRY"));
        Assert.Equal("45.000,00" + B, Bicim.BelgePara(45000m, null));
        Assert.Equal("45.000,00" + B, Bicim.BelgePara(45000m, ""));
        Assert.Equal("1.250,00 $", Bicim.BelgePara(1250m, "USD"));
        // Ayrım kilidi: ekran sıfır kuruşu ATAR, kâğıt ATMAZ.
        Assert.NotEqual(Bicim.Tl(45000m), Bicim.BelgePara(45000m, "TRY"));
    }

    private static readonly string[] FisBicimi =
        [SutunBicimi.Metin, SutunBicimi.Metin, SutunBicimi.Has, SutunBicimi.Para];

    [Fact]
    public void TK1_A4Belge_kalem_hucresi_kurusu_iki_hane_basar()
    {
        var bolum = new CiktiBolum("Kalemler", ["Tür", "Açıklama", "Has gram", "Tutar"], [], null, FisBicimi);
        var s = new CiktiSatir(["Satış", "Bilezik", "13.9232", "45000.00"], ParaBirimi: "TRY");
        Assert.Equal("45.000,00" + B, A4Belge.Hucre(bolum, s, 3));
    }

    [Fact]
    public void TK1_A4Belge_tutar_satiri_kurusu_iki_hane_basar()
    {
        Assert.Equal("57.550,00" + B, A4Belge.Tutar(new CiktiTutar("Genel toplam", 57550m, "TRY", true)));
        Assert.Equal("1.250,00 $", A4Belge.Tutar(new CiktiTutar("Toplam", 1250m, "USD")));
        Assert.Equal("13,9232 has", A4Belge.Tutar(new CiktiTutar("Has", 13.9232m, "HAS_GRAM")));
    }
}
