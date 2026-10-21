using Kuyumcu.Core.Domain;
using MKur = Kuyumcu.Api.Modules.Kur;

namespace Kuyumcu.Tests;

/// <summary>
/// 21.10 kurucu: "ürünler birkaç yerde farklı liste geliyor (eleman sayısı, isimler) · eski yeni tam liste gelmesi lazım".
/// Fiyat kalemlerinin kodu, sırası ve TR/EN adı TEK kaynaktan (Core FiyatKalemleri); sunucu adları, EN çeviri ve kur
/// ekranı aynı listeyi okur.
/// </summary>
public sealed class FiyatKalemleriTekKaynakTestleri
{
    [Fact]
    public void Katalog_her_yeni_sarrafiyenin_eskisini_hemen_ardindan_tasir()
    {
        var kodlar = FiyatKalemleri.Tumu.Select(k => k.Kod).ToList();
        foreach (var t in new[] { "CEYREK", "YARIM", "TAM", "ATA", "GREMSE" })
            Assert.Equal(kodlar.IndexOf($"{t}_YENI") + 1, kodlar.IndexOf($"{t}_ESKI"));
        Assert.Equal(kodlar.Count, kodlar.Distinct().Count());
    }

    [Fact]
    public void Sunucu_adlari_ve_EN_ceviri_katalogla_ayni()
    {
        Assert.Equal(FiyatKalemleri.Tumu.Select(k => k.Kod), MKur.PariteKodlari.Adlar.Keys);
        foreach (var k in FiyatKalemleri.Tumu)
        {
            Assert.Equal(k.Tr, MKur.PariteKodlari.Ad(k.Kod));
            Assert.Equal(k.En, Kuyumcu.Core.I18n.DilEkranSozlugu.KurAdi("en", k.Kod, "x"));
        }
    }

    [Fact]
    public void Kur_ekrani_kaynagin_tum_kalemlerini_katalog_sirasiyla_gosterir()
    {
        // Sağlayıcı sırası karışık gelse de (Ons önde, eski çeyrek sonda) ekran sırası katalog sırasıdır; süzgeç yok.
        var gelen = new[] { "ONS", "CEYREK_ESKI", "HAS", "GUMUS", "CEYREK_YENI", "BILINMEYEN" };
        var sira = gelen.OrderBy(FiyatKalemleri.Sira).ToArray();
        Assert.Equal(new[] { "HAS", "CEYREK_YENI", "CEYREK_ESKI", "GUMUS", "ONS", "BILINMEYEN" }, sira);
        Assert.Equal("Eski Çeyrek", FiyatKalemleri.Ad("CEYREK_ESKI", "tr"));
        Assert.Equal("Old quarter coin", FiyatKalemleri.Ad("CEYREK_ESKI", "en"));
        Assert.Equal("Sunucu adı", FiyatKalemleri.Ad("BILINMEYEN", "tr", "Sunucu adı"));
    }

    [Fact]
    public void Gun_farki_onceki_gun_has_fiyatina_gore()
    {
        Assert.Equal("▲ 0,3%", Kuyumcu.Web.Pages.Kur.YildizGunFarki(6656m, 6636m, "tr"));
        Assert.Equal("▼ 1,0%", Kuyumcu.Web.Pages.Kur.YildizGunFarki(6600m, 6666.67m, "tr"));
        Assert.Null(Kuyumcu.Web.Pages.Kur.YildizGunFarki(6600m, null, "tr"));
    }
}
