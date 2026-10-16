using Bunit;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-S1 — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİTTİR (üretim kodundan
// hesaplanmaz). T-S1 tezgâh yüzü şu sayıları GÖSTERİR: VERDİM "Toplam · N g"
// (FisHesap.ToplamGram, tr-TR 2 hane — tuval "10,40 g"), VERDİM/ALDIM tutarı
// ve Fark (Bicim.Para/Tl, AwayFromZero), nakit uyarı kutusundaki SUNUCU eşiği
// (FisNakitUyari). Ürün adıyla arama (BasitPos.AdlaBul) çoklu eşleşmede
// UYDURMAMALI.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class TS1SinirTestleri : BunitContext
{
    public TS1SinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        GocC2Ortak.IzgaraServisleri(Services);   // GOC-C-2: tuval tabloları ortak ızgarada (ApiIstemci ister)
    }

    private static UrunVeri U(string id, decimal g) => new(id, "x", "K22", null, 0m, null, null, GramAgirlik: g);
    private static FisKalemi K(string id, decimal adet) => new() { UrunId = id, UrunAdi = "x", Adet = adet };

    // ── Toplam gram ─────────────────────────────────────────────────────────
    [Fact]
    public void Gram_sifir_bos_fis_0()
        => Assert.Equal(0m, FisHesap.ToplamGram([], [U("a", 4.30m)]));

    [Fact]
    public void Gram_4_hane_kaybolmaz_0_0001_x3()
        => Assert.Equal(0.0003m, FisHesap.ToplamGram([K("a", 3m)], [U("a", 0.0001m)]));

    [Fact]
    public void Gram_gram_birimli_kalem_kesirli_adet()
        => Assert.Equal(1.5m * 1m, FisHesap.ToplamGram([K("g", 1.5m)], [U("g", 1m)]));

    [Fact]
    public void Gram_eksi_adet_isaret_korunur()
        => Assert.Equal(-4.30m, FisHesap.ToplamGram([K("a", -1m)], [U("a", 4.30m)]));

    // ── Fark ────────────────────────────────────────────────────────────────
    [Fact]
    public void Fark_tek_odeme_null_bacak_0_tamam()
    {
        Assert.Equal(0m, FisFark.Hesapla(999_999_999.99m, null));
        Assert.Equal(FarkDurumu.Tamam, FisFark.Durum(0m));
    }

    [Fact]
    public void Fark_kurus_1_eksik_ve_fazla_kaydet_kapali()
    {
        var b = new[] { new FisBacagi { Arac = "NAKIT", TutarTl = 50_649.99m } };
        Assert.Equal(0.01m, FisFark.Hesapla(50_650m, b));
        Assert.Equal(FarkDurumu.Eksik, FisFark.Durum(0.01m));
        Assert.False(FisFark.KaydetAcik(0.01m));
        Assert.Equal(FarkDurumu.Fazla, FisFark.Durum(-0.01m));
        Assert.False(FisFark.KaydetAcik(-0.01m));
    }

    [Fact]
    public void Fark_cok_buyuk_tutar_tam_kapanir()
    {
        var b = new[] { new FisBacagi { TutarTl = 999_999_998.99m }, new FisBacagi { Arac = "KART", TutarTl = 1m } };
        Assert.Equal(0m, FisFark.Hesapla(999_999_999.99m, b));
    }

    // ── Para gösterimi (AwayFromZero, tr-TR, TRY yok) ───────────────────────
    [Fact]
    public void Tl_kurus_yuvarlama_0_005_yukari_0_004_asagi()
    {
        Assert.Equal("0,01 ₺", Bicim.Tl(0.005m));
        Assert.Equal("0 ₺", Bicim.Tl(0.004m));
        Assert.Equal("−0,01 ₺", Bicim.Tl(-0.005m));
    }

    [Fact]
    public void Tl_cok_buyuk_ve_sifir()
    {
        Assert.Equal("999.999.999,99 ₺", Bicim.Para(999_999_999.99m, "TRY"));
        Assert.Equal("0 ₺", Bicim.Para(0m, "TRY"));
        Assert.DoesNotContain("TRY", Bicim.Para(50_650m, "TRY"));
    }

    // ── Nakit uyarı kutusu: eşik YALNIZ sunucudan, uydurma yok ───────────────
    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-5")]
    public void Nakit_uyarisi_had_yok_sifir_eksi_SAYISIZ(string? had)
    {
        decimal? h = had is null ? null : decimal.Parse(had, System.Globalization.CultureInfo.InvariantCulture);
        var e = Render<FisNakitUyari>(p => p.Add(x => x.Had, h));
        Assert.Equal(SatisTuvalSozlugu.Tr.NakitUyariSayisiz, e.Find("#islem-nakit-uyari").TextContent.Trim());
        Assert.DoesNotContain("30", e.Markup);
    }

    [Fact]
    public void Nakit_uyarisi_sunucu_esigi_tr_TR_ve_kurus()
    {
        var e = Render<FisNakitUyari>(p => p.Add(x => x.Had, 30_000m));
        Assert.Equal(string.Format(SatisTuvalSozlugu.Tr.NakitUyari, Kuyumcu.Web.Servisler.Bicim.Tl(30_000m)), e.Find("#islem-nakit-uyari").TextContent.Trim());
        var k = Render<FisNakitUyari>(p => p.Add(x => x.Had, 999_999_999.99m));
        Assert.Contains("999.999.999,99 ₺", k.Find("#islem-nakit-uyari").TextContent);
        Assert.DoesNotContain("TRY", k.Markup);
    }

    // ── VERDİM alt çubuğu gram biçimi (tuval 2 hane) ────────────────────────
    [Theory]
    [InlineData("10.40", "Toplam · 10,40 g")]
    [InlineData("0.005", "Toplam · 0,01 g")]
    [InlineData("0", "Toplam · 0,00 g")]
    [InlineData("12345.6", "Toplam · 12.345,60 g")]
    public void Verdim_toplam_gram_tr_TR(string gram, string beklenen)
    {
        var g = decimal.Parse(gram, System.Globalization.CultureInfo.InvariantCulture);
        var e = Render<FisVerdimAldim>(p => p.Add(x => x.ToplamGram, g).Add(x => x.Toplam, 0m).Add(x => x.KalemVar, false));
        Assert.Equal(beklenen, e.Find("#islem-verdim-gram").TextContent.Trim());
    }

    // ── Ürün adıyla bul: boş/null/kısa, çoklu eşleşme UYDURULMAZ, tr-TR İ/ı ───
    [Fact]
    public void AdlaBul_bos_null_tek_harf_aranmaz()
    {
        var l = new[] { ("1", "Alyans") };
        Assert.Equal((null, 0), BasitPos.AdlaBul(l, null!));
        Assert.Equal((null, 0), BasitPos.AdlaBul(l, "   "));
        Assert.Equal((null, 0), BasitPos.AdlaBul(l, "A"));
    }

    [Fact]
    public void AdlaBul_coklu_eslesme_null_ve_sayi_birebir_ad_kazanir()
    {
        var l = new[] { ("1", "Burma Bilezik"), ("2", "Burma Bilezik İnce"), ("3", "Alyans") };
        Assert.Equal((null, 2), BasitPos.AdlaBul(l, "burma"));
        Assert.Equal(("1", 1), BasitPos.AdlaBul(l, "BURMA BİLEZİK"));
        Assert.Equal(("2", 1), BasitPos.AdlaBul(l, "ince"));
    }

    // ── Türkçe sayı girişi (FormAlani.SayiCoz — Alınan/Para alanları) ───────
    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("0,0001", "0.0001")]
    [InlineData("999.999.999,99", "999999999.99")]
    public void SayiCoz_Turkce_girisler(string ham, string beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }

    // ── DOĞRULAMA TUR 3 (25.09) — basit VERDİM satır hücreleri (gram + tutar) ──
    //   Beklenenler SABİT; tuval tablosu satır gramını 2 hane, tutarı Bicim.Tl ile basar.
    private IReadOnlyList<string> BasitSatir(decimal gram, decimal adet, decimal birim, string ayarId = "K22")
    {
        var u = new List<UrunVeri> { new("a", "x", ayarId, null, 0m, null, null, GramAgirlik: gram) };
        var k = new List<FisKalemi> { new() { UrunId = "a", UrunAdi = "Yüzük", Adet = adet, BirimFiyat = birim } };
        var e = Render<FisKalemTablosu>(p => p.Add(x => x.Kalemler, k).Add(x => x.Urunler, u)
                                             .Add(x => x.Form, new FormDurumu()).Add(x => x.Basit, true).Add(x => x.Satis, true));
        return e.FindAll("#islem-kalem-tablo tbody tr:first-child td").Select(h => h.TextContent.Trim()).ToList();
    }

    [Fact]
    public void Dog3_basit_satir_gram_0_005_yukari_0_004_asagi_2_hane()
    {
        Assert.Equal("0,01", BasitSatir(0.005m, 1m, 1m)[2]);
        Assert.Equal("0,00", BasitSatir(0.004m, 1m, 1m)[2]);
        Assert.Equal("4,30", BasitSatir(4.30m, 1m, 1m)[2]);
        Assert.Equal("12,90", BasitSatir(4.30m, 3m, 1m)[2]);   // gram × adet
    }

    [Fact]
    public void Dog3_basit_satir_tutar_kurus_yuvarlama_ve_buyuk_tutar_ve_sifir()
    {
        Assert.Equal("0,01 ₺", BasitSatir(1m, 1m, 0.005m)[3]);   // Bicim NBSP ile ayırır
        Assert.Equal("0 ₺", BasitSatir(1m, 1m, 0m)[3]);
        Assert.Equal("999.999.999,99 ₺", BasitSatir(1m, 1m, 999_999_999.99m)[3]);
        Assert.Equal("22", BasitSatir(1m, 1m, 1m)[1]);            // ham kod "K22" basılmaz
        Assert.Equal("—", BasitSatir(1m, 1m, 1m, ayarId: null!)[1]);  // ayar yok → uydurulmaz
    }

    [Fact]
    public void Dog3_toplam_gram_4_hane_buyuk_ve_urun_karti_yok_0()
    {
        Assert.Equal(999_999.9999m, FisHesap.ToplamGram([K("a", 1m)], [U("a", 999_999.9999m)]));
        Assert.Equal(0m, FisHesap.ToplamGram([K("yok", 5m)], [U("a", 4.30m)]));
    }

    [Fact]
    public void Dog3_TekBelge_bos_fis_tek_sayilir_iki_kalem_duz_satis_degil()
    {
        Assert.True(FisYazdir.TekBelge(iade: false, kalemSayisi: 0, fisUcu: false));
        Assert.False(FisYazdir.TekBelge(iade: false, kalemSayisi: 2, fisUcu: false));
        Assert.True(FisYazdir.TekBelge(iade: false, kalemSayisi: 2, fisUcu: true));
    }

    [Fact]
    public void SayiCoz_belirsiz_1_500_reddedilir_bos_null()
    {
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
    }
}
