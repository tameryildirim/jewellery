using Kuyumcu.Contracts.Sayfalar.Cariler;
using Kuyumcu.Web.Servisler;
using SayfaCariler = Kuyumcu.Web.Pages.Cariler;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-M1 — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİTTİR (üretim kodundan
// hesaplanmaz). T-M1 BASİT yüzde bakiyeyi GÖSTERİR (Borç sütunu = Bicim.Tl,
// Has sütunu = Bicim.Has) ve bakiye İŞARETİNE göre "Borçlu" segmentini süzer
// (SegmentSuz). Sayı GİRDİSİ (FormAlani.SayiCoz) bu pakette yok → kapsam dışı.
// Yuvarlama kuralı projede MidpointRounding.AwayFromZero (Bicim.Tl).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TM1SinirTestleri
{
    private static CariVeri C(string id, decimal tl, decimal has, string? seg = null) =>
        new(id, id, "MUSTERI", null, null, tl, has, Segment: seg);

    private static string[] Borclu(params CariVeri[] l) =>
        [.. SayfaCariler.SegmentSuz(l, SayfaCariler.SegBorclu).Select(c => c.Id)];

    [Fact]
    public void DTM1_sifir_bakiye_borclu_sayilmaz()
        => Assert.Empty(Borclu(C("sifir", 0m, 0m)));

    [Fact]
    public void DTM1_eksi_bakiye_borclu_sayilmaz_isaret_korunur()
    {
        Assert.Empty(Borclu(C("tl", -0.01m, 0m), C("has", 0m, -0.0001m)));
        Assert.Equal("−0,01 ₺", Bicim.Tl(-0.01m));
    }

    [Fact]
    public void DTM1_has_4_hane_en_kucuk_borc_borclu_sayilir_ve_4_hane_basilir()
    {
        Assert.Equal(["h"], Borclu(C("h", 0m, 0.0001m)));
        Assert.Equal("0,0001 has", Bicim.Has(0.0001m));
    }

    [Fact]
    public void DTM1_kurus_yuvarlama_0_005_sifirdan_uzaga()
    {
        Assert.Equal("0,01 ₺", Bicim.Tl(0.005m));
        Assert.Equal("−0,01 ₺", Bicim.Tl(-0.005m));
        Assert.Equal("0 ₺", Bicim.Tl(0.004m));
        // Kuruş altı artı bakiye yine BORÇLU'dur (süzgeç görünen değere değil, veriye bakar).
        Assert.Equal(["k"], Borclu(C("k", 0.004m, 0m)));
    }

    [Fact]
    public void DTM1_cok_buyuk_tutar_eksiksiz_basilir_ve_suzulur()
    {
        Assert.Equal("999.999.999,99 ₺", Bicim.Tl(999_999_999.99m));
        Assert.Equal("−999.999.999,99 ₺", Bicim.Tl(-999_999_999.99m));
        Assert.Equal(["b"], Borclu(C("b", 999_999_999.99m, 0m), C("e", -999_999_999.99m, 0m)));
    }

    [Theory]
    [InlineData(null, "—")]
    [InlineData("", "—")]
    [InlineData("   ", "—")]
    [InlineData("+90 (532) 411-22-33", "0532 411 22 33")]   // CUMA-M: +90 → tuval biçimi
    [InlineData("0532 411 22 33", "0532 411 22 33")]
    // GIRDI-AILESI-2 tur 3: "0090" = 00 uluslararası önek + 90 → TR numarası olarak tanınır (doğrulayıcı ORTA bulgusu).
    [InlineData("00905324112233", "0532 411 22 33")]
    [InlineData("00442079460958", "00442079460958")]   // yabancı (tanınmayan) biçim AYNEN
    [InlineData("12345", "12345")]
    public void DTM1_telefon_bos_null_ve_tanimsiz_bicim(string? girdi, string beklenen)
        => Assert.Equal(beklenen, SayfaCariler.TelefonGoster(girdi));

    [Fact]
    public void DTM1_bos_liste_ve_bilinmeyen_segment_hepsini_dondurur()
    {
        Assert.Empty(SayfaCariler.SegmentSuz([], SayfaCariler.SegBorclu));
        var l = new[] { C("a", 1m, 0m), C("b", -1m, 0m) };
        Assert.Equal(2, SayfaCariler.SegmentSuz(l, "yok-boyle-segment").Count);
        // Toptancı büyük/küçük harf: sunucu kodu "TOPTAN" — "toptan" eşleşmez (tek kaynak kod).
        Assert.Empty(SayfaCariler.SegmentSuz([C("t", 0m, 0m, "toptan")], SayfaCariler.SegToptanci));
    }

    // ── Tur 2 doğrulayıcı eki (24.09 23:3x) ──
    [Fact]
    public void DTM1_r2_toptanci_null_ve_bireysel_suzulmez_hepsi_tam_doner()
    {
        var l = new[] { C("n", 0m, 0m, null), C("b", 0m, 0m, "BIREYSEL"), C("t", 0m, 0m, "TOPTAN") };
        Assert.Equal(["t"], SayfaCariler.SegmentSuz(l, SayfaCariler.SegToptanci).Select(c => c.Id).ToArray());
        Assert.Equal(3, SayfaCariler.SegmentSuz(l, SayfaCariler.SegHepsi).Count);
    }

    [Fact]
    public void DTM1_r2_karisik_isaret_TL_eksi_has_arti_borclu_sayilir()
        => Assert.Equal(["x"], Borclu(C("x", -151_300m, 0.0001m)));

    [Fact]
    public void DTM1_r2_kisa_ad_bos_ve_turkce_buyuk_harf()
    {
        Assert.Equal("", SayfaCariler.KisaAd(""));
        Assert.Equal("ayşe İ.", SayfaCariler.KisaAd("ayşe ilhan"));
        Assert.Equal("sistem", SayfaCariler.KisaAd("sistem"));
    }
}

// ── Tur 3 doğrulayıcı eki (25.09 00:3x, Aşkın + Mehmet) — beklenenler SABİT ──
public sealed class TM1SinirTestleriTur3
{
    private static readonly Kuyumcu.Core.I18n.MusteriTuvalMetinleri Tr = Kuyumcu.Core.I18n.MusteriTuvalSozlugu.Tr;

    [Fact]
    public void DTM1_r3_borc_sutunu_sifir_kurus_atilir_kurus_varsa_iki_hane()
    {
        Assert.Equal("0\u00A0₺", Bicim.Tl(0m));
        Assert.Equal("38.900\u00A0₺", Bicim.Tl(38_900m));
        Assert.Equal("12.000,50\u00A0₺", Bicim.Tl(12_000.5m));
        Assert.Equal("99.999,99\u00A0₺", Bicim.Tl(99_999.99m));
        Assert.Equal("−1.500\u00A0₺", Bicim.Tl(-1_500m));
    }

    [Fact]
    public void DTM1_r3_has_yarim_ondalik_ve_negatif()
    {
        Assert.Equal("0,0001 has", Bicim.Has(0.00005m));
        Assert.Equal("0,0000 has", Bicim.Has(0.00004m));
        Assert.Equal("-22,0000 has", Bicim.Has(-22m));
    }

    [Fact]
    public void DTM1_r3_zaman_gece_yarisi_siniri()
    {
        var simdi = new DateTime(2026, 9, 25, 0, 5, 0);
        Assert.Equal("bugün 00:00", SayfaCariler.ZamanMetni(new DateTime(2026, 9, 25, 0, 0, 0), simdi, Tr, "tr"));
        Assert.Equal("dün 23:59", SayfaCariler.ZamanMetni(new DateTime(2026, 9, 24, 23, 59, 0), simdi, Tr, "tr"));
        Assert.Equal("23.09.2026 23:59", SayfaCariler.ZamanMetni(new DateTime(2026, 9, 23, 23, 59, 0), simdi, Tr, "tr"));
    }

    [Fact]
    public void DTM1_r3_belirsiz_saat_UTC_sayilir()
    {
        var belirsiz = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Unspecified);
        var beklenen = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc).ToLocalTime();
        Assert.Equal(beklenen, SayfaCariler.Yerel(belirsiz));
        var yerel = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Local);
        Assert.Equal(yerel, SayfaCariler.Yerel(yerel));
    }

    [Theory]
    [InlineData("  Ayşe   Yılmaz ", "Ayşe Y.")]
    [InlineData("Ayşe", "Ayşe")]
    public void DTM1_r3_kisa_ad_fazla_bosluk(string ad, string beklenen)
        => Assert.Equal(beklenen, SayfaCariler.KisaAd(ad));

    [Theory]
    [InlineData("0 532 411 22 33", "0532 411 22 33")]
    [InlineData("905324112233", "0532 411 22 33")]   // CUMA-M
    [InlineData("0532411223", "0532411223")]      // 10 hane ama 0'la başlıyor → tanınmaz, AYNEN
    [InlineData("053241122334", "053241122334")]  // 12 hane, 90 değil → AYNEN
    public void DTM1_r3_telefon_hane_sinirlari(string girdi, string beklenen)
        => Assert.Equal(beklenen, SayfaCariler.TelefonGoster(girdi));
}
