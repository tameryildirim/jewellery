using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Sayim;
using Kuyumcu.Web.Bilesenler.Form;
using MUrun = Kuyumcu.Api.Modules.Urun;

namespace Kuyumcu.Tests.Dogrulayici;

// ════════════════════════════════════════════════════════════════════════
// PAKET P-SAYIM — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT (üretim kodundan
// hesaplanmaz). Sınanan yol: POST /sayim/{id}/say → SayimServisi.Say
// "artis" (okut + Enter) ve "sayilanAdet" (mutlak) — stok miktarı.
// Kural: sayılan kolonu decimal(12,3); yuvarlama Math.Round(.,3,AwayFromZero);
// ADET biriminde tam sayı; tavan 999.999.999,999.
// ════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class PSAYIMSinirTestleri(TestVeritabani vt)
{
    private static SayimServisi Servis(KuyumcuDbContext db) => new(db, new MUrun.UrunServisi(db));

    private static decimal? Sayilan(object detay, string urunId)
    {
        var liste = (System.Collections.IList)detay.GetType().GetProperty("satirlar")!.GetValue(detay)!;
        var s = liste.Cast<object>().Single(x => (string)x.GetType().GetProperty("urunId")!.GetValue(x)! == urunId);
        return (decimal?)s.GetType().GetProperty("sayilanAdet")!.GetValue(s);
    }

    private async Task<(Kiraci K, string Id)> Kur(string ad, bool gram)
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "PSAYIM-SINIR " + ad, stokAdet: 2m);
        if (gram)
        {
            using var sdb = vt.OlusturSistem();
            TestKurulum.UrunuGramBirimiYap(sdb, k.UrunId);
        }
        using var adb = vt.Olustur(k.TenantId);
        var d = await Servis(adb).Ac(new SayimAcDto { Ad = "Sınır", SubeId = k.SubeId, KapsamTur = "TUMU" }, k.Islem);
        return (k, (string)d.GetType().GetProperty("Id")!.GetValue(d)!);
    }

    [Fact]
    public async Task DG_PS_01_sifir_artis_RED_satir_bos_kalir()
    {
        var (k, id) = await Kur("sifir", gram: false);
        using var db = vt.Olustur(k.TenantId);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, Artis = 0m }, k.Islem));
        Assert.Equal("artis", AlanIsareti.Alan(ex));
        using var kd = vt.Olustur(k.TenantId);
        Assert.Null(Sayilan(await Servis(kd).Detay(id, k.Islem), k.UrunId));
    }

    [Fact]
    public async Task DG_PS_02_eksi_artis_GRAM_urunde_de_RED_ve_mevcut_sayilan_dusmez()
    {
        var (k, id) = await Kur("eksi", gram: true);
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, SayilanAdet = 3m }, k.Islem);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, Artis = -0.5m }, k.Islem));
        Assert.Equal("artis", AlanIsareti.Alan(ex));
        using var kd = vt.Olustur(k.TenantId);
        Assert.Equal(3m, Sayilan(await Servis(kd).Detay(id, k.Islem), k.UrunId));
    }

    [Fact]
    public async Task DG_PS_03_yuvarlama_0_0005_AwayFromZero_0_001_olur()
    {
        var (k, id) = await Kur("yuv", gram: true);
        using var db = vt.Olustur(k.TenantId);
        var d = await Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, Artis = 0.0005m }, k.Islem);
        Assert.Equal(0.001m, Sayilan(d, k.UrunId));
    }

    [Fact]
    public async Task DG_PS_04_yuvarlama_0_0004_sifira_iner_ve_RED()
    {
        var (k, id) = await Kur("yuv0", gram: true);
        using var db = vt.Olustur(k.TenantId);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, Artis = 0.0004m }, k.Islem));
        Assert.Equal("artis", AlanIsareti.Alan(ex));
    }

    [Fact]
    public async Task DG_PS_05_gram_4_hane_1_2345_kolon_3_haneye_1_235_yazilir()
    {
        var (k, id) = await Kur("dort", gram: true);
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, Artis = 1.2345m }, k.Islem);
        using var kd = vt.Olustur(k.TenantId);
        Assert.Equal(1.235m, Sayilan(await Servis(kd).Detay(id, k.Islem), k.UrunId));
    }

    [Fact]
    public async Task DG_PS_06_tavan_tam_kabul_bir_fazlasi_RED_ve_kayit_bozulmaz()
    {
        var (k, id) = await Kur("tavan", gram: true);
        using var db = vt.Olustur(k.TenantId);
        var d = await Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, Artis = 999_999_999.999m }, k.Islem);
        Assert.Equal(999_999_999.999m, Sayilan(d, k.UrunId));
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, Artis = 0.001m }, k.Islem));
        Assert.Equal("artis", AlanIsareti.Alan(ex));
        using var kd = vt.Olustur(k.TenantId);
        Assert.Equal(999_999_999.999m, Sayilan(await Servis(kd).Detay(id, k.Islem), k.UrunId));
    }

    [Fact]
    public async Task DG_PS_07_bos_null_ne_artis_ne_adet_RED_alan_sayilanAdet()
    {
        var (k, id) = await Kur("bos", gram: false);
        using var db = vt.Olustur(k.TenantId);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId }, k.Islem));
        Assert.Equal("sayilanAdet", AlanIsareti.Alan(ex));
    }

    [Fact]
    public async Task DG_PS_08_ADET_biriminde_artis_1_birikir_kesir_0_5_RED()
    {
        var (k, id) = await Kur("adet", gram: false);
        using var db = vt.Olustur(k.TenantId);
        await Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, Artis = 1m }, k.Islem);
        var d = await Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, Artis = 1m }, k.Islem);
        Assert.Equal(2m, Sayilan(d, k.UrunId));
        await Assert.ThrowsAsync<BadRequestException>(() =>
            Servis(db).Say(id, new SayimSayDto { UrunId = k.UrunId, Artis = 0.5m }, k.Islem));
        using var kd = vt.Olustur(k.TenantId);
        Assert.Equal(2m, Sayilan(await Servis(kd).Detay(id, k.Islem), k.UrunId));
    }

    [Theory]
    [InlineData("45.000", "Tamam", "45000")]
    [InlineData("1,5", "Tamam", "1.5")]
    [InlineData("1.500", "Belirsiz", null)]
    [InlineData("", "Bos", null)]
    public void DG_PS_09_Turkce_adet_girisi_SayiCoz(string ham, string sonuc, string? deger)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc.ToString());
        Assert.Equal(deger is null ? null : decimal.Parse(deger, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }
}
