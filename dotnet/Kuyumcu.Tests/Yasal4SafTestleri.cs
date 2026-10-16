using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using MHurda = Kuyumcu.Api.Modules.Hurda;

namespace Kuyumcu.Tests;

// PAKET YASAL-4 — saf (veritabanısız) kurallar: süre son günü, stopaj son ödeme günü,
// KMTS deseni (fail-closed).
public sealed class Yasal4SafTestleri
{
    private static YasalParametreSatiri Satir(string anahtar, decimal? sayi, string? metin, DateOnly bas, string teyit = "TEYITSIZ") =>
        new("t-" + anahtar, anahtar, sayi, metin, "X", bas, null, "test", "https://ornek.test", teyit, "test");

    private static YasalParametreServisi Yasal(params YasalParametreSatiri[] s) =>
        YasalParametreServisi.Yukleyicili(() => s);

    [Fact]
    public void Gider_pusulasi_son_gunu_TESLIM_GUNUNUN_degeriyle_teslim_arti_gun()
    {
        var y = Yasal(Satir(YasalAnahtar.GiderPusulasiGun, 7m, null, new DateOnly(2021, 11, 1)));
        var teslim = new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);   // yerel 26.09 13:00
        Assert.Equal(new DateOnly(2026, 10, 3), MHurda.GiderPusulasiSuresi.SonGun(new MHurda.HurdaAyarlari(), y, teslim));
        // Satır yoksa yapılandırma (açılış denetimi ikisinin eşit olduğunu garanti eder).
        Assert.Equal(new DateOnly(2026, 10, 3), MHurda.GiderPusulasiSuresi.SonGun(new MHurda.HurdaAyarlari(), null, teslim));
    }

    [Fact]
    public void Stopaj_son_odeme_gunu_izleyen_ayin_beyan_gunu_satir_yoksa_NULL()
    {
        var y = Yasal(Satir(YasalAnahtar.StopajBeyanGunu, 26m, null, new DateOnly(2026, 9, 30)));
        var s = new MHurda.StopajServisi(null!, null!, null!, null!, y);
        var (gun, teyit) = s.SonGun(2026, 10, new DateOnly(2026, 10, 1));
        Assert.Equal(new DateOnly(2026, 11, 26), gun);
        Assert.Equal("TEYITSIZ", teyit);
        // Şubat kısa → ayın son günü (31 → 28/29 değil, değer 26 olduğundan 26).
        Assert.Equal(new DateOnly(2027, 2, 26), s.SonGun(2027, 1, new DateOnly(2026, 10, 1)).Gun);
        // Parametre hiç yoksa gün UYDURULMAZ.
        var bos = new MHurda.StopajServisi(null!, null!, null!, null!, Yasal());
        Assert.Null(bos.SonGun(2026, 10, new DateOnly(2026, 10, 1)).Gun);
    }

    [Fact]
    public void Donem_yerel_aydir_ve_bicim_denetimi()
    {
        // 31.08 22:30 UTC = 01.09 01:30 yerel → Eylül dönemi.
        Assert.Equal("2026-09", MHurda.StopajServisi.Donem(new DateTime(2026, 8, 31, 22, 30, 0, DateTimeKind.Utc)));
        Assert.True(MHurda.StopajServisi.DonemGecerli("2026-08", out _, out _));
        Assert.False(MHurda.StopajServisi.DonemGecerli("2026-13", out _, out _));
        Assert.False(MHurda.StopajServisi.DonemGecerli("26-08", out _, out _));
    }

    [Fact]
    public void KMTS_desen_bosken_serbest_doluyken_uymayan_400_bozuk_desen_FAIL_CLOSED()
    {
        Assert.Equal("serbest kod", KmtsKodu.Dogrula("  serbest kod ", Yasal()));
        Assert.Null(KmtsKodu.Dogrula("   ", Yasal()));
        Assert.Throws<BadRequestException>(() => KmtsKodu.Dogrula(new string('x', 101), Yasal()));

        var bugun = YasalParametreServisi.IslemGunu(DateTime.UtcNow);
        var y = Yasal(Satir(YasalAnahtar.KmtsKodDeseni, null, "KM[0-9]{4}", bugun.AddDays(-1)));
        Assert.Equal("KM1234", KmtsKodu.Dogrula("KM1234", y));
        Assert.Throws<BadRequestException>(() => KmtsKodu.Dogrula("KM12345", y));   // tam eşleşme
        Assert.False(KmtsKodu.Uyar("ABC", "(bozuk["));   // bozuk desen → kabul edilmez
    }
}
