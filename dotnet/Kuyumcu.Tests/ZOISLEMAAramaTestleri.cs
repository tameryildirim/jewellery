using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Modules.Arama;
using Kuyumcu.Contracts.Arama;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET ZO-ISLEM-A 28.09 (2. tasarım hakemi turu, KRİTİK) — POS eşleştirmesinin
// banka hareketi araması.
//   Bulgu: alanın etiketi "Bankaya yatan tutar" iken TUTAR ARANMIYORDU ("103" →
//   sonuç yok) ve bankaya GELEN 103.439,43 ₺ listede "-103439.43 TRY" görünüyordu
//   (yön "GIRIS" ile karşılaştırılıyordu; banka yönü GELEN/GIDEN).
//   Sözleşme: sayısal sorgu tutarın BAŞINDAN eşleşir (yazdıkça daralır; tr-TR ve
//   nokta ondalıklı yazım aynı sonucu verir), GELEN "+", GIDEN "−", biçim ekranın
//   Bicim.Para'sı ile BİREBİR aynı.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class ZOISLEMAAramaHttpTestleri(HttpFikstur f)
{
    private static async Task<AramaYaniti> Ara(HttpClient c, string q)
    {
        var cevap = await c.GetAsync($"/api/v1/arama?tur={AramaTurleri.BankaHareket}&q={Uri.EscapeDataString(q)}");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return (await cevap.Content.ReadFromJsonAsync<AramaYaniti>(
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)))!;
    }

    [Fact]
    public async Task ZO28b_banka_hareketi_TUTARLA_aranir_GELEN_arti_GIDEN_eksi_ekran_bicimi()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "ZO28b arama");
        var gelen = PosYardim.BankaHareketiYaz(f.Vt, k, 103_439.43m, "GELEN");
        var giden = PosYardim.BankaHareketiYaz(f.Vt, k, 1_250.50m, "GIDEN");
        var c = f.Ortam.Istemci(k);

        // Yazdıkça daralır: "103" tutarın başından eşleşir.
        var y = await Ara(c, "103");
        var s = Assert.Single(y.Sonuclar);
        Assert.Equal(gelen, s.Kimlik);
        // 28.09 (doğrulayıcı DÜŞÜK): beklenen SABİT metin — üretim kodundan (Bicim.Para)
        // hesaplanmaz; "\u00A0" = ekran para kuralındaki bölünmez boşluk.
        Assert.EndsWith("+103.439,43\u00A0₺", s.AltBilgi);
        Assert.DoesNotContain("TRY", s.AltBilgi);
        Assert.DoesNotContain("-", s.AltBilgi!.Split('·')[1]);

        // tr-TR ve nokta ondalıklı tam yazım aynı kaydı bulur.
        foreach (var q in new[] { "103.439,43", "103439,43", "103439.43", "103.439" })
            Assert.Equal(gelen, Assert.Single((await Ara(c, q)).Sonuclar).Kimlik);

        // GIDEN eksi (U+2212) ile ve ekran biçiminde.
        var g = Assert.Single((await Ara(c, "1250,5")).Sonuclar);
        Assert.Equal(giden, g.Kimlik);
        Assert.EndsWith("−1.250,50\u00A0₺", g.AltBilgi);

        // Eşleşmeyen tutar → boş (hata değil).
        Assert.Empty((await Ara(c, "999")).Sonuclar);
    }

    // 28.09 (doğrulayıcı DÜŞÜK): "1.250" belirsiz — binlik (1.250 ₺) ve ondalık (1,25 ₺)
    // yorumlarının İKİSİ de bulunur; "1.250,00" belirsiz değildir (yalnız 1.250 ₺).
    [Fact]
    public async Task ZO28c_belirsiz_1_250_IKI_yorumu_da_bulur_virgullu_yazim_TEK_yorum()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "ZO28c arama");
        var binlik = PosYardim.BankaHareketiYaz(f.Vt, k, 1_250m, "GELEN");
        var ondalik = PosYardim.BankaHareketiYaz(f.Vt, k, 1.25m, "GELEN");
        var c = f.Ortam.Istemci(k);

        var iki = (await Ara(c, "1.250")).Sonuclar.Select(s => s.Kimlik).ToHashSet();
        Assert.Equal(new HashSet<string> { binlik, ondalik }, iki);

        var tek = Assert.Single((await Ara(c, "1.250,00")).Sonuclar);
        Assert.Equal(binlik, tek.Kimlik);
        Assert.EndsWith("+1.250\u00A0₺", tek.AltBilgi);
    }
}

public sealed class ZOISLEMAAramaSafTestleri
{
    [Theory]
    [InlineData("103", "103")]
    [InlineData("103.439,43", "103439.43")]
    [InlineData("103439,43", "103439.43")]
    [InlineData("103439.43", "103439.43")]
    [InlineData("1.250", "1250")]
    [InlineData("2.456.789,45 ₺", "2456789.45")]
    [InlineData("+1250,5", "1250.5")]
    [InlineData("Garanti", null)]
    [InlineData("12a", null)]
    [InlineData("1,2,3", null)]
    [InlineData("1.2.3", null)]
    [InlineData("1.2,3", null)]            // virgüllü yazımda nokta yalnız geçerli binlik grubu
    [InlineData("12.345,6", "12345.6")]
    [InlineData("0.050", "0.050")]         // 28.09 3. doğrulama: grup 0 ile başlamaz → binlik DEĞİL
    [InlineData("0.050,00", null)]         // virgüllü yazımda da "0.050" geçerli binlik grubu değil
    [InlineData("012.000", "012.000")]
    public void ZO28b_tutar_oneki_cozumu(string girdi, string? beklenen) =>
        Assert.Equal(beklenen, AramaServisi.TutarOnekiCoz(girdi));

    [Theory]
    [InlineData("1.250", "1250|1.25")]      // belirsiz: binlik + ondalık
    [InlineData("12.340", "12340|12.34")]
    [InlineData("1.255", "1255")]           // 3 ondalık olamaz (kolon 2 ondalık)
    [InlineData("1.250.000", "1250000")]    // çok noktalı: belirsiz değil
    [InlineData("1.250,00", "1250.00")]     // virgüllü: belirsiz değil
    [InlineData("103", "103")]
    [InlineData("Garanti", "")]
    [InlineData("0.050", "0.05")]           // 28.09 3. doğrulama: "0050" anlamsız öneki ÜRETİLMEZ
    [InlineData("0.055", "0.055")]
    public void ZO28c_tutar_onekleri_belirsiz_yazimda_IKI_yorum(string girdi, string beklenen) =>
        Assert.Equal(beklenen, string.Join("|", AramaServisi.TutarOnekleri(girdi)));

    [Theory]
    [InlineData("103439.43", "TRY")]
    [InlineData("57550", "TRY")]
    [InlineData("0.005", "TRY")]
    [InlineData("1250.5", "USD")]
    [InlineData("99.9", "EUR")]
    public void ZO28b_sunucu_ekran_parasi_Bicim_Para_ile_BIREBIR(string tutar, string pb)
    {
        var n = decimal.Parse(tutar, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(Bicim.Para(n, pb), AramaServisi.EkranPara(n, pb));
    }
}
