using Kuyumcu.Api.Modules.Tevsik;
using Kuyumcu.Web.Bilesenler.Form;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET YASAL-2 — HASIM DOĞRULAYICI (Aşkın + Mehmet) finansal sınır testleri.
// Taksit kuralı (TB-15, saf) ve bildirim son günü (TB-16, saf) sınırları.
// Beklenen değerler ELLE yazılmıştır; üretim kodundan hesaplanmaz.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class YASAL2SinirTestleri
{
    private static decimal Had30(DateOnly _) => 30_000m;
    private static readonly DateOnly G = new(2026, 9, 1);

    private static TaksitKaynagi P(string id, decimal kalan, decimal kaynak, TaksitKaynak tur = TaksitKaynak.Islem) =>
        new(id, kalan, tur, kaynak, G);

    // ── sıfır ──
    [Fact]
    public void Sifir_odeme_dagitimi_BOS_sifir_nakit_RED_DEGIL()
    {
        Assert.Empty(TaksitKurali.FifoDagit(0m, 0m, [P("b", 62_000m, 62_000m)]));
        var s = TaksitKurali.TaksitDegerlendir(0m, TaksitKurali.FifoDagit(5_000m, 0m, [P("b", 62_000m, 62_000m)]), Had30);
        Assert.False(s.Red);
        Assert.Equal(0m, s.IzinliNakit);
    }

    // ── eksi: işaret mutlak değere indirgenir (tediye yönü de aynı kural) ──
    [Fact]
    public void Eksi_nakit_ve_eksi_odeme_mutlak_degerle_degerlendirilir_RED()
    {
        var d = TaksitKurali.FifoDagit(-20_000m, 0m, [P("b", 62_000m, 62_000m)]);
        Assert.Single(d);
        Assert.Equal(20_000m, d[0].Tuketilen);
        Assert.True(TaksitKurali.TaksitDegerlendir(-20_000m, d, Had30).Red);
    }

    // ── kuruş sınırı: tam had serbest, 1 kuruş üstü red; izinli nakitten 1 kuruş fazla red ──
    [Theory]
    [InlineData("30000.00", false)]
    [InlineData("30000.01", true)]
    [InlineData("29999.99", false)]
    public void Kurus_siniri_kaynak_tutar(string kaynak, bool buyuk)
    {
        var k = decimal.Parse(kaynak, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(buyuk, TaksitKurali.Buyuk(new TaksitParti("p", 1m, TaksitKaynak.Islem, k, G), Had30));
    }

    [Fact]
    public void Kurus_siniri_izinli_nakit_10000_tam_gecer_10000_01_red()
    {
        TaksitKaynagi[] partiler = [P("k", 10_000m, 10_000m), P("b", 50_000m, 50_000m)];
        var d = TaksitKurali.FifoDagit(15_000m, 0m, partiler);
        Assert.False(TaksitKurali.TaksitDegerlendir(10_000.00m, d, Had30).Red);
        var r = TaksitKurali.TaksitDegerlendir(10_000.01m, d, Had30);
        Assert.True(r.Red);
        Assert.Equal(10_000m, r.IzinliNakit);
    }

    // ── çok büyük tutar ──
    [Fact]
    public void Cok_buyuk_tutar_999999999_99_tasma_yok_buyuk_islem()
    {
        const decimal dev = 999_999_999.99m;
        var d = TaksitKurali.FifoDagit(dev, 0m, [P("b", dev, dev)]);
        var s = TaksitKurali.TaksitDegerlendir(1m, d, Had30);
        Assert.True(s.Red);
        Assert.Equal(dev, s.BuyukKaynakTutar);
        Assert.Equal(0m, TaksitKurali.NakitAlinabilir(0m, [P("b", dev, dev)], Had30));
    }

    // ── boş / null ──
    [Fact]
    public void Bos_parti_listesi_NakitAlinabilir_null_ve_tum_odeme_FAZLA_red_yok()
    {
        Assert.Null(TaksitKurali.NakitAlinabilir(0m, [], Had30));
        var d = TaksitKurali.FifoDagit(40_000m, 0m, []);
        Assert.Single(d);
        Assert.Equal(TaksitKaynak.Fazla, d[0].Tur);
        Assert.False(TaksitKurali.TaksitDegerlendir(40_000m, d, Had30).Red);
    }

    [Fact]
    public void Kaynaksiz_bakiye_nakit_gecer_dogrulanamayan_tutar_nakitle_sinirli()
    {
        var d = TaksitKurali.FifoDagit(8_000m, 5_000m, [P("b", 62_000m, 62_000m)]);
        var s = TaksitKurali.TaksitDegerlendir(5_000m, d, Had30);
        Assert.False(s.Red);
        Assert.Equal(5_000m, s.Dogrulanamayan);
        var s2 = TaksitKurali.TaksitDegerlendir(3_000m, d, Had30);
        Assert.Equal(3_000m, s2.Dogrulanamayan);
    }

    // ── bildirim son günü: yıl sonu + tatil, 0 iş günü ──
    [Fact]
    public void Son_gun_yil_donumu_yilbasi_atlanir_2027_01_08()
    {
        // 31.12.2026 Perşembe → 1 Ocak (tatil, Cuma) atlanır; 4-8 Ocak 2027 = 5 iş günü.
        var son = TaksitKurali.BildirimSonGunu(new DateOnly(2026, 12, 31), 5,
            new HashSet<DateOnly> { new(2027, 1, 1) });
        Assert.Equal(new DateOnly(2027, 1, 8), son);
    }

    [Fact]
    public void Son_gun_cumartesi_ihlal_pazartesiden_sayilir()
    {
        // 24.10.2026 Cumartesi → 26,27,28,30 Ekim (29 tatil), 2 Kasım.
        var son = TaksitKurali.BildirimSonGunu(new DateOnly(2026, 10, 24), 5,
            new HashSet<DateOnly> { new(2026, 10, 29) });
        Assert.Equal(new DateOnly(2026, 11, 2), son);
    }

    // ── Türkçe giriş (tahsilat Para alanının ortak kapısı) ──
    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("30.000,01", "30000.01")]
    public void Turkce_giris_kabul(string ham, string beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.True(c.Tamam);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }

    [Fact]
    public void Turkce_giris_belirsiz_1_500_REDDEDILIR()
    {
        var c = FormAlani.SayiCoz("1.500");
        Assert.False(c.Tamam);
    }

    // ══ TUR 3 DOĞRULAYICI EKİ (30.09 23:xx) — red cümlesi sınırları ══

    [Fact]
    public void T3_mesaj_izinli_SIFIR_tutarsiz_nakit_alinamaz_0_00_YOK()
    {
        var s = new TaksitSonuc(true, 0m, 0m, 62_000m, new DateOnly(2026, 9, 27), 30_000m);
        var m = TevsikKapisi.TaksitMesaji(s, alacakYonu: true);
        Assert.Equal("Bu borç 27.09.2026 tarihli, 30.000,00 TL'yi aşan bir işlemden kalıyor; nakit alınamaz, kartla ya da havaleyle alın.", m);
    }

    [Fact]
    public void T3_mesaj_izinli_1_kurus_en_cok_0_01()
    {
        var s = new TaksitSonuc(true, 0.01m, 0m, 62_000m, new DateOnly(2026, 9, 27), 30_000m);
        Assert.Equal("Bu ödemede en çok 0,01 TL nakit alınabilir; kalanı kartla ya da havaleyle alın.",
            TevsikKapisi.TaksitMesaji(s, alacakYonu: true));
    }

    [Fact]
    public void T3_doviz_kesirli_TL_karsiligi_izinli_kurus_hassasiyetinde_kalir_0_00_yazilmaz()
    {
        // Döviz tahsilatı TL karşılığı kuruş altı olabilir (1.234,5678); izinli pay ise defter
        // partilerinden (decimal(15,2)) gelir → mesajdaki rakam kuruşa tam, "0,00" doğmaz.
        // NOT (doğrulayıcı): saf fonksiyona kuruş altı IzinliNakit (0,004) verilirse "en çok 0,00 TL"
        // yazar — DB şeması (15,2) nedeniyle üretimde ulaşılamaz; DÜŞÜK bulgu olarak raporlandı.
        var d = TaksitKurali.FifoDagit(1_234.5678m, 0m, [P("k", 0.01m, 0.01m), P("b", 50_000m, 50_000m)]);
        var s = TaksitKurali.TaksitDegerlendir(1_234.5678m, d, Had30);
        Assert.True(s.Red);
        Assert.Equal(0.01m, s.IzinliNakit);
        var m = TevsikKapisi.TaksitMesaji(s, alacakYonu: true, dovizMi: true);
        Assert.Equal("Bu ödemede en çok 0,01 TL karşılığı nakit alınabilir; kalanı kartla ya da havaleyle alın.", m);
    }

    [Fact]
    public void T3_mesaj_cok_buyuk_izinli_999_999_999_99_tr_TR_bicim()
    {
        var s = new TaksitSonuc(true, 999_999_999.99m, 0m, 2_000_000_000m, new DateOnly(2026, 9, 27), 30_000m);
        Assert.Equal("Bu ödemede en çok 999.999.999,99 TL karşılığı nakit alınabilir; kalanı kartla ya da havaleyle alın.",
            TevsikKapisi.TaksitMesaji(s, alacakYonu: true, dovizMi: true));
    }

    [Fact]
    public void T3_mesaj_buyuk_gun_null_genel_cumle()
    {
        var s = new TaksitSonuc(true, 0m, 0m, null, null, null);
        Assert.Equal("Bu borç nakit sınırını aşan bir işlemden kalıyor; nakit ödenemez, kartla ya da havaleyle ödeyin.",
            TevsikKapisi.TaksitMesaji(s, alacakYonu: false));
    }

    [Fact]
    public void T3_K11_gercek_ResmiTatiller_ile_23_10_2026_son_gun_02_11_2026()
    {
        var tatil = Kuyumcu.Api.Common.ResmiTatiller.Tumu(2026, null);
        Assert.Equal(new DateOnly(2026, 11, 2), TaksitKurali.BildirimSonGunu(new DateOnly(2026, 10, 23), 5, tatil));
    }

    [Fact]
    public void T3_kismen_izinli_FIFO_kaynaksiz_0_005_ve_buyuk_parti_izinli_0_005()
    {
        var d = TaksitKurali.FifoDagit(10m, 0.005m, [P("b", 50_000m, 50_000m)]);
        var s = TaksitKurali.TaksitDegerlendir(0.01m, d, Had30);
        Assert.True(s.Red);
        Assert.Equal(0.005m, s.IzinliNakit);
        Assert.Equal(0.005m, s.Dogrulanamayan);
    }
}
