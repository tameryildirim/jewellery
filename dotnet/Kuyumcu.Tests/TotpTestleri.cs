using System.Security.Cryptography;
using System.Text;
using Kuyumcu.Api.Common;
using Kuyumcu.Core.I18n;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 45 — TOTP saf birim testleri.
//
// "RFC 6238 uyumlu" cümlesi, Ek B test vektörleri YEŞİL olmadan hiçbir belgeye
// yazılmaz (Bertan + Mehmet koşulu). Bu dosya o kanıttır.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TotpTestleri
{
    /// <summary>RFC 6238 Ek B: SHA-1 gizi ASCII "12345678901234567890".</summary>
    private static readonly byte[] RfcGizi = Encoding.ASCII.GetBytes("12345678901234567890");

    [Theory]
    [InlineData(59L, "287082")]
    [InlineData(1111111109L, "081804")]
    [InlineData(1234567890L, "005924")]
    public void Rfc6238_ek_b_vektorleri_birebir_uretilir(long unixSaniye, string beklenen)
    {
        var adim = Totp.Adim(DateTime.UnixEpoch.AddSeconds(unixSaniye), 30);
        Assert.Equal(beklenen, Totp.Uret(RfcGizi, adim));
    }

    [Fact]
    public void Kod_alti_haneye_sifirla_doldurulur()
    {
        // T=1234567890 → 89005924; 6 haneli hâli başında SIFIR taşır.
        var adim = Totp.Adim(DateTime.UnixEpoch.AddSeconds(1234567890), 30);
        var kod = Totp.Uret(RfcGizi, adim);
        Assert.Equal(6, kod.Length);
        Assert.StartsWith("00", kod, StringComparison.Ordinal);
    }

    [Fact]
    public void Base32_gidis_donus_kayipsiz_ve_dolgusuz()
    {
        for (var i = 0; i < 100; i++)
        {
            var giz = RandomNumberGenerator.GetBytes(20);
            var metin = Base32.Kodla(giz);
            Assert.DoesNotContain('=', metin);
            Assert.Equal(metin, metin.ToUpperInvariant());
            Assert.Equal(giz, Base32.Coz(metin));
        }
    }

    [Fact]
    public void Drift_29_saniye_kabul_edilir()
    {
        // Adım sınırına oturtulmuş çapa: 3000 sn = tam 100. adım.
        var an = DateTime.UnixEpoch.AddSeconds(3000);
        var kod = Totp.Uret(RfcGizi, Totp.Adim(an, 30));

        Assert.True(Totp.Dogrula(RfcGizi, kod, an, 30, 1, out _));
        Assert.True(Totp.Dogrula(RfcGizi, kod, an.AddSeconds(-29), 30, 1, out _));
        Assert.True(Totp.Dogrula(RfcGizi, kod, an.AddSeconds(29), 30, 1, out _));
    }

    [Fact]
    public void Drift_31_saniye_reddedilir()
    {
        var an = DateTime.UnixEpoch.AddSeconds(3000);
        var kod = Totp.Uret(RfcGizi, Totp.Adim(an, 30));

        // -31 sn → iki adım geride; +61 sn → iki adım ileride. ±1 pencere DIŞI.
        Assert.False(Totp.Dogrula(RfcGizi, kod, an.AddSeconds(-31), 30, 1, out _));
        Assert.False(Totp.Dogrula(RfcGizi, kod, an.AddSeconds(61), 30, 1, out _));
    }

    [Fact]
    public void Dogrulanan_adim_disari_verilir()
    {
        var an = DateTime.UnixEpoch.AddSeconds(3000);
        var beklenenAdim = Totp.Adim(an, 30);
        var kod = Totp.Uret(RfcGizi, beklenenAdim);

        Assert.True(Totp.Dogrula(RfcGizi, kod, an, 30, 1, out var kullanilan));
        Assert.Equal(beklenenAdim, kullanilan);
    }

    [Fact]
    public void Otpauth_uri_anahtari_ve_parametreleri_tasir()
    {
        var uri = Totp.OtpauthUri(Marka.AdAscii, "usta@ornek.com", "JBSWY3DPEHPK3PXP", 6, 30);
        Assert.StartsWith("otpauth://totp/", uri, StringComparison.Ordinal);
        Assert.Contains("secret=JBSWY3DPEHPK3PXP", uri, StringComparison.Ordinal);
        Assert.Contains("digits=6", uri, StringComparison.Ordinal);
        Assert.Contains("period=30", uri, StringComparison.Ordinal);
        // P-D2: yayıncı adı tek kaynaktan gelir; eski ürün adı geri yazılırsa bu satır kırmızıya döner.
        Assert.Contains("issuer=" + Marka.AdAscii, uri, StringComparison.Ordinal);
        Assert.DoesNotContain("Kuyumcu", uri, StringComparison.OrdinalIgnoreCase);
    }
}
