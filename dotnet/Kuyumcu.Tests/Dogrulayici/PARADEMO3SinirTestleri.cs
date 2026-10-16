using System.Globalization;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Modules.Kasa;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PARA-DEMO-3 — HASIM DOĞRULAYICI (Aşkın + Mehmet) finansal sınır testleri.
// Kasa kapısı mesajı (sunucu tr-TR + işaret) ve istemci ayrıştırıcısı (KasaBakiyeMesaji)
// sınırlarda: sıfır, eksi, kuruş yuvarlaması (AwayFromZero, 0,005), çok büyük tutar,
// boş/null, döviz işareti, Türkçe giriş (FormAlani.SayiCoz). Beklenen değerler ELLE yazıldı.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class PARADEMO3SinirTestleri
{
    private const string Nbsp = " ";
    private const string Eksi = "−";

    private static string Mesaj(string ad, decimal bakiye, string pb, decimal delta)
    {
        var ex = Assert.Throws<BadRequestException>(() => KasaBakiyeKapisi.Dogrula(ad, bakiye, pb, delta));
        return ex.Message;
    }

    [Fact]
    public void PD3S_sifir_delta_ve_sifir_bakiye_gecer_bir_kurus_asim_reddedilir()
    {
        KasaBakiyeKapisi.Dogrula("K", 0m, "TRY", 0m);          // sıfır çıkış: ret yok
        KasaBakiyeKapisi.Dogrula("K", 100m, "TRY", -100m);     // tam bakiye kadar: ret yok (eşit sınır)
        var m = Mesaj("K", 0m, "TRY", -0.01m);
        Assert.Equal($"'K' kasasının bakiyesi yetersiz: 0,00{Nbsp}₺ var, 0,01{Nbsp}₺ isteniyor. Nakit kasa eksiye düşemez.", m);
        var t = KasaBakiyeMesaji.Ayristir(m)!.Value;
        Assert.Equal(0m, t.Bakiye);
        Assert.Equal(0.01m, t.Istenen);
        Assert.Equal("TRY", t.ParaBirimi);
    }

    [Fact]
    public void PD3S_eksi_bakiye_U2212_ile_yazilir_ve_istemci_isaretiyle_okur()
    {
        var m = Mesaj("Ana Kasa", -500.25m, "TRY", -1m);
        Assert.Contains($"{Eksi}500,25{Nbsp}₺ var", m);
        Assert.DoesNotContain("TRY", m);
        var t = KasaBakiyeMesaji.Ayristir(m)!.Value;
        Assert.Equal(-500.25m, t.Bakiye);
        Assert.Equal(1m, t.Istenen);
    }

    [Fact]
    public void PD3S_kurus_yuvarlamasi_AwayFromZero_0005_siniri()
    {
        // 0,005 → 0,01 (AwayFromZero); 0,004 → 0,00; −0,004 → "0,00" (eksi sıfır basılmaz).
        Assert.Contains($": 0,01{Nbsp}₺ var", Mesaj("K", 0.005m, "TRY", -1m));
        Assert.Contains($": 0,00{Nbsp}₺ var", Mesaj("K", 0.004m, "TRY", -1m));
        Assert.Contains($": 0,00{Nbsp}₺ var", Mesaj("K", -0.004m, "TRY", -1m));
        Assert.Contains($"var, 1.000,01{Nbsp}₺ isteniyor", Mesaj("K", 0m, "TRY", -1000.005m));
        Assert.Contains($"var, 1.000,00{Nbsp}₺ isteniyor", Mesaj("K", 0m, "TRY", -1000.004m));
    }

    [Fact]
    public void PD3S_cok_buyuk_tutar_999_999_999_99_tam_gidip_doner()
    {
        var m = Mesaj("K", 999_999_999.99m, "TRY", -1_000_000_000m);
        Assert.Contains($"999.999.999,99{Nbsp}₺ var, 1.000.000.000,00{Nbsp}₺ isteniyor", m);
        var t = KasaBakiyeMesaji.Ayristir(m)!.Value;
        Assert.Equal(999_999_999.99m, t.Bakiye);
        Assert.Equal(1_000_000_000m, t.Istenen);
    }

    [Fact]
    public void PD3S_bos_null_ve_kalip_disi_null_doner()
    {
        Assert.Null(KasaBakiyeMesaji.Ayristir(null));
        Assert.Null(KasaBakiyeMesaji.Ayristir(""));
        Assert.Null(KasaBakiyeMesaji.Bicimle(null, new DilServisi(null!)));
        Assert.Null(KasaBakiyeMesaji.Bicimle("", new DilServisi(null!)));
        Assert.Null(KasaBakiyeMesaji.Ayristir("'K' kasasının bakiyesi yetersiz: abc var, 1 isteniyor"));
    }

    [Fact]
    public void PD3S_doviz_isareti_ve_taninmayan_kod_uydurulmaz()
    {
        var usd = Mesaj("D", 1250.5m, "USD", -2000m);
        Assert.Contains($"1.250,50{Nbsp}$ var", usd);
        Assert.Equal("USD", KasaBakiyeMesaji.Ayristir(usd)!.Value.ParaBirimi);
        var eur = Mesaj("E", 10m, "EUR", -20m);
        Assert.Equal("EUR", KasaBakiyeMesaji.Ayristir(eur)!.Value.ParaBirimi);
        var xau = Mesaj("X", 10m, "XAU", -20m);
        Assert.Contains($"10,00{Nbsp}XAU var", xau);
        Assert.Equal("XAU", KasaBakiyeMesaji.Ayristir(xau)!.Value.ParaBirimi);
    }

    [Fact]
    public void PD3S_sunucu_bicimi_istek_kulturunden_bagimsiz_enUS()
    {
        var once = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var m = Mesaj("K", 312_547.91m, "TRY", -2_000_000m);
            Assert.Contains($"312.547,91{Nbsp}₺ var, 2.000.000,00{Nbsp}₺ isteniyor", m);
            Assert.DoesNotContain("312547.91", m);
            Assert.DoesNotContain("TRY", m);
        }
        finally { CultureInfo.CurrentCulture = once; }
    }

    [Fact]
    public void PD3S_eski_invariant_bicim_gecis_uyumu_sayilari_ayni()
    {
        var t = KasaBakiyeMesaji.Ayristir("'Ana Kasa' kasasının bakiyesi yetersiz: 312547.91 TRY var, 2000000.00 TRY isteniyor. Nakit kasa eksiye düşemez.")!.Value;
        Assert.Equal(312_547.91m, t.Bakiye);
        Assert.Equal(2_000_000m, t.Istenen);
        Assert.Equal("TRY", t.ParaBirimi);
    }

    [Fact]
    public void PD3S_turkce_giris_SayiCoz_45000_1v5_belirsiz_1500()
    {
        Assert.Equal(45_000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// PARA-DEMO-3 TUR 2 — HASIM DOĞRULAYICI sınır testleri (nakit ayrımlı biçimleyici, araç payı
// kalıbı, tek ödeme adı üreticisi). Beklenen değerler ELLE yazıldı (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class PARADEMO3SinirTur2Testleri
{
    private const string N = "\u00a0";
    private static readonly DilServisi Tr = new(null!);
    private const string ToplamRed = "'K' kasasının bakiyesi yetersiz: 400,00\u00a0₺ var, 1.000,00\u00a0₺ isteniyor. Nakit kasa eksiye düşemez.";

    private static Kuyumcu.Contracts.Sayfalar.Kasalar.KasaNakitAyrimiOzeti Ayrim(decimal toplam, decimal nakit, decimal kart, decimal havale) =>
        new("k1", "TRY", toplam, nakit, kart, havale);

    [Fact]
    public void PD3S2_eksi_nakit_payi_sifira_kirpilir_eksi_basilmaz()
    {
        var m = KasaBakiyeMesaji.Bicimle(ToplamRed, Tr, Ayrim(400m, -100m, 500m, 0m));
        Assert.Equal($"'K' kasasında nakit yetersiz: çekmecede 0{N}₺ nakit var, 1.000{N}₺ nakit isteniyor. Kasadaki kart (500{N}₺) ve havale (0{N}₺) tahsilatı nakit olarak verilemez.", m);
    }

    [Fact]
    public void PD3S2_kart_havale_sifir_ya_da_toplami_sifir_ise_toplam_cumlesi_aynen()
    {
        const string beklenen = "'K' kasasında yeterli para yok: kasada 400\u00a0₺ var, 1.000\u00a0₺ isteniyor. Nakit kasa eksiye düşemez.";
        Assert.Equal(beklenen, KasaBakiyeMesaji.Bicimle(ToplamRed, Tr, Ayrim(400m, 400m, 0m, 0m)));
        // PD2 açık kalan 9: havale payı eksi olabilir → kart + havale = 0 → nakit cümlesine çevrilmez.
        Assert.Equal(beklenen, KasaBakiyeMesaji.Bicimle(ToplamRed, Tr, Ayrim(400m, 400m, 2_496m, -2_496m)));
    }

    [Fact]
    public void PD3S2_kurus_0005_AwayFromZero_nakit_cumlesinde()
    {
        var m = KasaBakiyeMesaji.Bicimle(ToplamRed, Tr, Ayrim(400m, 0.005m, 0.005m, 399.99m));
        Assert.Equal($"'K' kasasında nakit yetersiz: çekmecede 0,01{N}₺ nakit var, 1.000{N}₺ nakit isteniyor. Kasadaki kart (0,01{N}₺) ve havale (399,99{N}₺) tahsilatı nakit olarak verilemez.", m);
    }

    [Fact]
    public void PD3S2_arac_payi_kalibi_kurus_ve_cok_buyuk_tutar_havale()
    {
        var sunucu = $"'K' kasasındaki havale tahsilatı 0,01{N}₺; 999.999.999,99{N}₺ havaleyle çıkarılamaz.";
        Assert.Equal($"'K' kasasındaki havale tahsilatı 0,01{N}₺; 999.999.999,99{N}₺ havale olarak çıkarılamaz.", KasaBakiyeMesaji.Bicimle(sunucu, Tr));
        var en = Ingilizce();
        Assert.Equal($"'K' holds 0,01{N}₺ of transfer takings; 999.999.999,99{N}₺ cannot be paid out by transfer.", KasaBakiyeMesaji.Bicimle(sunucu, en));
        // Pay sıfır: "0,00 ₺" → "0 ₺".
        Assert.Equal($"'K' kasasındaki kart tahsilatı 0{N}₺; 1{N}₺ kart olarak çıkarılamaz.",
            KasaBakiyeMesaji.Bicimle($"'K' kasasındaki kart tahsilatı 0,00{N}₺; 1,00{N}₺ kartla çıkarılamaz.", Tr));
    }

    [Fact]
    public void PD3S2_bos_null_mesaj_ayrimli_yolda_da_null()
    {
        Assert.Null(KasaBakiyeMesaji.Bicimle(null, Tr, Ayrim(1m, 0m, 1m, 0m)));
        Assert.Null(KasaBakiyeMesaji.Bicimle("", Tr, Ayrim(1m, 0m, 1m, 0m)));
        Assert.Null(KasaBakiyeMesaji.Bicimle("Kasa kapalı", Tr, Ayrim(1m, 0m, 1m, 0m)));
    }

    [Fact]
    public void PD3S2_odeme_adi_tek_uretici_bos_bacak_bilinmeyen_kod()
    {
        Assert.Equal("Kart + Havale", Kuyumcu.Core.I18n.ParaDemo3Sozlugu.OdemeAdi("KARMA", ["KART", "HAVALE"], "tr"));
        Assert.Equal("Card + Transfer", Kuyumcu.Core.I18n.ParaDemo3Sozlugu.OdemeAdi("KARMA", ["KART", "HAVALE"], "en"));
        Assert.Equal("Karma ödeme", Kuyumcu.Core.I18n.ParaDemo3Sozlugu.OdemeAdi("KARMA", [], "tr"));
        Assert.Equal("Karma ödeme", Kuyumcu.Core.I18n.ParaDemo3Sozlugu.OdemeAdi("KARMA", null, "tr"));
        Assert.Equal("Havale", Kuyumcu.Core.I18n.ParaDemo3Sozlugu.OdemeAdi("HAVALE", null, "tr"));
    }

    private static DilServisi Ingilizce()
    {
        var d = new DilServisi(new BosJs());
        d.DegistirAsync("en").GetAwaiter().GetResult();
        return d;
    }

    private sealed class BosJs : Microsoft.JSInterop.IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult(default(TValue)!);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => ValueTask.FromResult(default(TValue)!);
    }
}
