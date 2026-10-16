using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Contracts.Sayfalar.Kasalar;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Bilesenler.Yildiz;
using Kuyumcu.Web.Pages;

namespace Kuyumcu.Tests.Dogrulayici;

// ═══════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET P-DIL-BORC-KASA finansal sınır testleri.
// Paket yalnız GÖSTERİM değiştirir (borç/alacak kalıbı + kasa büyük rakamı); sınırlar
// gösterilen tutarın İŞARETİ, kuruş yuvarlaması ve büyük tutar biçimi üzerindedir.
// Beklenen değerler SABİT ve elle yazıldı (üretim kodundan türetilmez).
// Yuvarlama kuralı: Bicim.Tl / YildizTezgahBicim / YildizMusteriBicim → 2 hane, AwayFromZero.
// Para metni: "<gövde> ₺", eksi U+2212.
// ═══════════════════════════════════════════════════════════
public sealed class PDILBORCKASASinirTestleri
{
    private const string TL = " ₺";

    // ── Sıfır ──
    [Fact]
    public void Sifir_bakiye_borc_yok_der_eksi_sifir_de()
    {
        Assert.Equal("Borç yok", BorcDiliSozlugu.Durum(0m, "0" + TL, "tr"));
        Assert.Equal("Borç yok", BorcDiliSozlugu.Durum(-0.00m, "0" + TL, "tr"));
        Assert.Equal("Borç yok", YildizTezgahBicim.MusteriDurumu(new CariVeri("c0", "Sıfır", TlBakiye: 0m), "tr"));
    }

    // ── Eksi: işaret kalıpta söylenir, tutar mutlak ──
    [Fact]
    public void Eksi_bakiye_biz_borcluyuz_tutar_eksisiz()
    {
        var m = YildizTezgahBicim.MusteriDurumu(new CariVeri("t1", "Anadolu", TlBakiye: -3840m), "tr");
        Assert.Equal("Biz müşteriye borçluyuz: 3.840" + TL, m);
        Assert.DoesNotContain("−", m);
        Assert.DoesNotContain("-", m);
        Assert.Equal("Müşteri bize borçlu: 1.250,50" + TL,
            YildizTezgahBicim.MusteriDurumu(new CariVeri("m1", "Selin", TlBakiye: 1250.5m), "tr"));
    }

    // ── Kuruş yuvarlaması (0,005 sınırı, AwayFromZero) ──
    [Theory]
    [InlineData("0.004", "Borç yok")]
    [InlineData("-0.004", "Borç yok")]
    [InlineData("0.005", "Müşteri bize borçlu: 0,01 ₺")]
    [InlineData("-0.005", "Biz müşteriye borçluyuz: 0,01 ₺")]
    [InlineData("12.345", "Müşteri bize borçlu: 12,35 ₺")]
    [InlineData("-12.344", "Biz müşteriye borçluyuz: 12,34 ₺")]
    public void Kurus_yuvarlamasi_tezgah_durumu(string bakiye, string beklenen)
    {
        var b = decimal.Parse(bakiye, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(beklenen, YildizTezgahBicim.MusteriDurumu(new CariVeri("c", "X", TlBakiye: b), "tr"));
    }

    [Fact]
    public void Kurus_yuvarlamasi_musteriler_ust_satir_sayilari()
    {
        static Kuyumcu.Contracts.Sayfalar.Cariler.CariVeri C(string id, decimal tl) => new(id, id, "MUSTERI", null, null, tl, 0m);
        var liste = new[]
        {
            C("a", 0.004m),   // yuvarlanınca 0 → sayılmaz
            C("b", -0.004m),  // 0 → sayılmaz
            C("c", 0.005m),   // 0,01 → bize borçlu
            C("d", -0.005m),  // −0,01 → biz borçluyuz
            C("e", -999999999.99m),
        };
        var (bize, biz) = YildizMusteriBicim.Sayilar(liste);
        Assert.Equal(1, bize);
        Assert.Equal(2, biz);
        Assert.Equal("5 müşteri · 1 bize borçlu · 2 biz borçluyuz", YildizMusteriSozlugu.Ust(5, bize, biz, "tr"));
    }

    // ── Çok büyük tutar ──
    [Fact]
    public void Cok_buyuk_tutar_kalip_ve_buyuk_rakam()
    {
        Assert.Equal("Müşteri bize borçlu: 999.999.999,99" + TL,
            YildizTezgahBicim.MusteriDurumu(new CariVeri("m", "Büyük", TlBakiye: 999999999.99m), "tr"));
        Assert.Equal("Biz müşteriye borçluyuz: 999.999.999,99" + TL,
            YildizTezgahBicim.MusteriDurumu(new CariVeri("m", "Büyük", TlBakiye: -999999999.99m), "tr"));
        var ay = new KasaNakitAyrimiOzeti("k", "TRY", 999999999.99m, 999999999.99m, 0m, 0m);
        Assert.Equal(999999999.99m, Kasalar.BuyukRakam(999999999.99m, ay));
        Assert.Equal(("999.999.999", ",99" + TL), YildizParaBicim.BuyukPara(Kasalar.BuyukRakam(999999999.99m, ay), "TRY"));
    }

    // ── Kasa büyük rakamı: nakit 0 / eksi / null ayrım ──
    [Fact]
    public void Kasa_buyuk_rakam_nakit_sifirsa_SIFIR_toplama_dusmez()
    {
        // Kasadaki her şey kart+havale: çekmece boş → büyük rakam 0, toplam 5.000 DEĞİL.
        var ay = new KasaNakitAyrimiOzeti("k", "TRY", 5000m, 0m, 3000m, 2000m);
        Assert.Equal(0m, Kasalar.BuyukRakam(5000m, ay));
    }

    [Fact]
    public void Kasa_buyuk_rakam_nakit_eksi_isaretiyle_gosterilir()
    {
        // Kart girişi 10.000, nakit çıkışı 12.000,50 → bakiye −2.000,50; nakit −12.000,50.
        var ay = new KasaNakitAyrimiOzeti("k", "TRY", -2000.5m, -12000.5m, 10000m, 0m);
        Assert.Equal(-12000.5m, Kasalar.BuyukRakam(-2000.5m, ay));
        Assert.Equal(("−12.000", ",50" + TL), YildizParaBicim.BuyukPara(-12000.5m, "TRY"));
    }

    [Fact]
    public void Kasa_buyuk_rakam_ayrim_null_ise_bakiye_ve_etiket_nakit_demez()
    {
        Assert.Equal(312999.49m, Kasalar.BuyukRakam(312999.49m, null));
        Assert.DoesNotContain("nakit", KasaNakitSozlugu.KasaToplami("tr").Split('(')[0], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Çekmecedeki nakit", KasaNakitSozlugu.CekmecedekiNakit("tr"));
    }

    // ── Boş/null: bilinmeyen toplam cümleye girmez ──
    [Fact]
    public void Bugun_cumlesi_null_toplam_cumleye_girmez_sifir_toplam_girer()
    {
        var yok = YildizBugunBicim.Cumle(new YildizBugunVerisi { SatisAdet = 0, AlacakTl = null, BorcTl = 250m }, "tr");
        Assert.Equal("Bugün satış yok.", yok);
        var sifir = YildizBugunBicim.Cumle(new YildizBugunVerisi { SatisAdet = 1, AlacakTl = 0m, BorcTl = 0m }, "tr");
        Assert.Equal("Bugünkü satış · 1 işlem. Müşteriler bize borçlu: 0" + TL + ". Biz müşterilere borçluyuz: 0" + TL + ".", sifir);
        Assert.Null(YildizTezgahBicim.MusteriDurumu(null, "tr"));
    }

    // ── has/gram 4 hane: emanet iade alanı has gram girer; çözümleme 4 haneyi korur ──
    [Fact]
    public void Has_gram_dort_hane_turkce_giris()
    {
        Assert.Equal(new FormAlani.SayiCozumu(FormAlani.SayiSonucu.Tamam, 11.4500m), FormAlani.SayiCoz("11,4500"));
        Assert.Equal(new FormAlani.SayiCozumu(FormAlani.SayiSonucu.Tamam, 0.0001m), FormAlani.SayiCoz("0,0001"));
    }

    // ── Türkçe giriş ──
    [Fact]
    public void Turkce_giris_45000_15_ve_belirsiz_1500_reddi()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
    }

    // ── EN: aynı işaret mantığı ──
    [Fact]
    public void Ingilizce_kalip_isaret_ayni()
    {
        Assert.Equal("We owe the customer: 3.840" + TL,
            YildizTezgahBicim.MusteriDurumu(new CariVeri("t1", "Anadolu", TlBakiye: -3840m), "en"));
        Assert.Equal("No balance", BorcDiliSozlugu.Durum(0m, "0", "en"));
    }
}

// ═══════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI TUR 2 (Aşkın + Mehmet, 11.10) — Y18 emanet "bırakıldığı gibi" sınırları.
// EmanetBirakildigi.Ozetle: gram 3 hane, has 6 hane, AwayFromZero; null = bilinmiyor (fail-closed).
// Beklenen değerler SABİT ve elle yazıldı.
// ═══════════════════════════════════════════════════════════
public sealed class PDILBORCKASAEmanetSinirTestleri
{
    private static Kuyumcu.Core.Domain.EmanetIz G(string? ayar, decimal? gram, decimal has, decimal? tas = null) => new(ayar, gram, tas, has);
    private static IReadOnlyList<Kuyumcu.Core.Domain.EmanetAyarSatiri>? O(params Kuyumcu.Core.Domain.EmanetIz[] s) => Kuyumcu.Core.Domain.EmanetBirakildigi.Ozetle(s);

    // ── Sıfır / boş ──
    [Fact]
    public void Sifir_ve_bos_emanet_bos_liste_null_degil()
    {
        Assert.Empty(O()!);
        Assert.Empty(O(G("K22", 0m, 0m))!);
        // Ayarsız ama has'ı 0 olan satır özeti bozmaz.
        var s = Assert.Single(O(G(null, null, 0m), G("K22", 10m, 9.16m))!);
        Assert.Equal(("K22", 10m, 9.16m), (s.AyarId, s.Gram, s.Has));
    }

    // ── Eksi: kalandan fazla iade / işaretleri çelişen iz → bilinmiyor ──
    [Fact]
    public void Eksi_kalan_ya_da_isaret_celiskisi_null()
    {
        Assert.Null(O(G("K22", 5m, 4.58m), G("K22", -6m, -5.496m)));
        Assert.Null(O(G("K22", -5m, -4.58m)));                 // yalnız iade: has eksi → bilinmiyor
        Assert.Null(O(G("K22", 5m, -1m)));   // gram artı, has eksi → bilinmiyor
        Assert.Null(O(G("K99", 5m, 4m)));                       // tanınmayan ayar uydurulmaz
    }

    // ── Yuvarlama: gram 3 hane AwayFromZero (0,0005 sınırı), has 6 hane ──
    [Fact]
    public void Gram_yuvarlama_0_0005_sinirinda_yukari()
    {
        var s = Assert.Single(O(G("K22", 1.0005m, 0.9165m))!);
        Assert.Equal(1.001m, s.Gram);
        Assert.Equal(0.9165m, s.Has);
        Assert.Equal("1,001 g", EmanetDiliSozlugu.Gram(1.0005m, "tr"));
        Assert.Equal("12,50 g", EmanetDiliSozlugu.Gram(12.5m, "tr"));
        Assert.Equal("0,00 g", EmanetDiliSozlugu.Gram(0m, "tr"));
    }

    // ── has 4 hane: 0,0001 g gram kalıntısı 3 haneye 0 yuvarlanır; has kalıntısı varsa özet bilinmez ──
    [Fact]
    public void Has_0_0001_kalintisi_gram_sifirken_bilinmiyor()
    {
        Assert.Null(O(G("K22", 12.5m, 11.45m), G("K22", -12.5m, -11.4499m)));
        // Has kalıntısı tam 0 ise emanet kapanmış sayılır.
        Assert.Empty(O(G("K22", 12.5m, 11.45m), G("K22", -12.5m, -11.45m))!);
    }

    // ── Çok büyük tutar ──
    [Fact]
    public void Cok_buyuk_gram_bicimi_ve_ozet()
    {
        Assert.Equal("999.999.999,99 g", EmanetDiliSozlugu.Gram(999_999_999.99m, "tr"));
        Assert.Equal("999,999,999.99 g", EmanetDiliSozlugu.Gram(999_999_999.99m, "en"));
        var s = Assert.Single(O(G("K24", 999_999_999.999m, 999_000_000m))!);
        Assert.Equal(999_999_999.999m, s.Gram);
        Assert.Equal("24 ayar · 999.999.999,999 g", EmanetDiliSozlugu.AyarGram("K24", s.Gram, "tr"));
    }

    // ── Taş: net = brüt − taş; taş null = 0 ──
    [Fact]
    public void Tas_null_sifir_sayilir_tas_brutten_duser()
    {
        Assert.Equal(12m, Assert.Single(O(G("K22", 12.5m, 10.992m, 0.5m))!).Gram);
        Assert.Equal(12.5m, Assert.Single(O(G("K22", 12.5m, 11.45m, null))!).Gram);
        Assert.Equal("22 ayar · 12,50 g · taş 0,50 g", EmanetDiliSozlugu.Satir("K22", 12.5m, 0.5m, "tr"));
        Assert.Equal("22 ayar · 12,50 g", EmanetDiliSozlugu.Satir("K22", 12.5m, 0m, "tr"));
    }

    // ── Türkçe giriş (iade alanı c-gram FormAlani.SayiCoz'dan geçer) ──
    [Fact]
    public void Iade_grami_turkce_giris()
    {
        Assert.Equal(12.5m, FormAlani.SayiCoz("12,50").Deger);
        Assert.Equal(0.001m, FormAlani.SayiCoz("0,001").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Equal(0.001m, Kuyumcu.Api.Modules.Emanet.EmanetServisi.IadeHasToleransi);
    }
}
