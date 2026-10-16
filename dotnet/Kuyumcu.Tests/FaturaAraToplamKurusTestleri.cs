using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// ARA TOPLAM + KDV == TOPLAM — KURUŞ KAPISI (ZO-BELGE hasım doğrulaması, 29.09).
//
// Canlı ölçüm: kalemsiz POST /api/v1/faturalar (STANDART, %20) ara toplamı ve
// KDV'yi AYRI AYRI yuvarlıyordu; ikisinin toplamı belge toplamından 1 kuruş
// sapıyordu:
//   999,99        → 833,33 + 166,67         = 1.000,00
//   1.234.567,89  → 1.028.806,58 + 205.761,32 = 1.234.567,90
// Düzeltme: önce KDV yuvarlanır, ara toplam = toplam − yuvarlanmış KDV.
// Beklenen değerler SABİTTİR (hesaplanmış değil) — kalıp kayarsa test kırılır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class FaturaAraToplamKurusTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static decimal Oku(JsonElement j, string ad) =>
        j.EnumerateObject().Single(p => p.Name.Equals(ad, StringComparison.OrdinalIgnoreCase)).Value.GetDecimal();

    [Theory]
    [InlineData("999.99",       "833.32",       "166.67")]
    [InlineData("1234567.89",   "1028806.57",   "205761.32")]
    public async Task Standart_yuzde20_fatura_ARA_arti_KDV_TOPLAMA_esittir(
        string toplamMetin, string beklenenAraMetin, string beklenenKdvMetin)
    {
        var toplam      = decimal.Parse(toplamMetin, System.Globalization.CultureInfo.InvariantCulture);
        var beklenenAra = decimal.Parse(beklenenAraMetin, System.Globalization.CultureInfo.InvariantCulture);
        var beklenenKdv = decimal.Parse(beklenenKdvMetin, System.Globalization.CultureInfo.InvariantCulture);
        var k = YeniKiraci($"ZO-BELGE kurus {toplamMetin}");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/faturalar",
            new { cariId = k.CariId, tur = "SATIS", toplam, kdvOran = 0.20m, kdvTuru = "STANDART" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        // HTTP yanıtı
        var j = await cevap.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(beklenenAra, Oku(j, "araToplam"));
        Assert.Equal(beklenenKdv, Oku(j, "kdvTutar"));
        Assert.Equal(toplam, Oku(j, "toplam"));

        // Saklanan satır
        using var db = f.Vt.OlusturSistem();
        var fatura = db.Faturalar.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(beklenenAra, fatura.AraToplam);
        Assert.Equal(beklenenKdv, fatura.KdvTutar);
        Assert.Equal(toplam, fatura.Toplam);
        Assert.Equal(fatura.Toplam, fatura.AraToplam + fatura.KdvTutar);
    }

    /// <summary>
    /// Aynı kapı KUYUM (ELLE has) yolunda: 10.999,99 − 10.000 has = 999,99 matrah
    /// (KDV dahil) → KDV 166,67; eski kalıp ara toplamı 10.833,33 yazıp toplamı
    /// 11.000,00'a çıkarıyordu.
    /// </summary>
    [Fact]
    public async Task Kuyum_elle_has_fatura_ARA_arti_KDV_TOPLAMA_esittir()
    {
        var k = YeniKiraci("ZO-BELGE kurus kuyum");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/faturalar",
            new { cariId = k.CariId, tur = "SATIS", toplam = 10_999.99m, kdvOran = 0.20m,
                  kdvTuru = "KUYUM", hasDeger = 10_000m });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        var fatura = db.Faturalar.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(166.67m, fatura.KdvTutar);
        Assert.Equal(10_833.32m, fatura.AraToplam);
        Assert.Equal(fatura.Toplam, fatura.AraToplam + fatura.KdvTutar);
    }
}
