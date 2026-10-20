using Kuyumcu.Api.Modules.Banka;

namespace Kuyumcu.Tests;

// M8 kalanı (20.10) — banka açıklamasından TC/VKN ve gönderen adı (saf). Geçersiz sayı kimlik sayılmaz;
// etiketsiz açıklamadan ad ÇIKARILMAZ (HAVALE, EFT, şube kodu müşteri adı değildir).
public sealed class AciklamaAyristiriciTestleri
{
    [Theory]
    [InlineData("AYŞE DEMİR 10000000146 HAVALE", "10000000146")]
    [InlineData("VKN 1234567890 ODEME", "1234567890")]
    [InlineData("DEKONT 12345678901 HAVALE", null)]          // geçersiz TCKN — kimlik değil
    [InlineData("TR120006200000000012345678 EFT", null)]   // IBAN parçası — kimlik değil
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Kimlik_yalniz_gecerli_TCKN_ya_da_VKN(string? aciklama, string? beklenen) =>
        Assert.Equal(beklenen, AciklamaAyristirici.Ayristir(aciklama).KimlikNo);

    [Theory]
    [InlineData("Gönd: AYŞE DEMİR 0157-Enpara", "AYŞE DEMİR")]
    [InlineData("GÖNDEREN: HATİCE DEMİRKOL / HAVALE", "HATİCE DEMİRKOL")]
    [InlineData("gonderen ali veli 10000000146", "ali veli")]
    [InlineData("AYŞE DEMİR HAVALE", null)]                 // etiket yok → ad çıkarılmaz
    [InlineData("Gönd: 12345 HAVALE", null)]                 // ad yerine sayı
    [InlineData("Gönd: A", null)]                            // tek harf
    public void Ad_yalniz_gonderen_etiketinden(string aciklama, string? beklenen) =>
        Assert.Equal(beklenen, AciklamaAyristirici.Ayristir(aciklama).Ad);

    [Fact]
    public void Ikisi_birlikte()
    {
        var i = AciklamaAyristirici.Ayristir("Gönd: HATİCE DEMİRKOL 10000000146 HAVALE");
        Assert.Equal(("10000000146", "HATİCE DEMİRKOL"), (i.KimlikNo, i.Ad));
    }
}
