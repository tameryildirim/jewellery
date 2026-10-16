using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests.Dogrulayici;

// ════════════════════════════════════════════════════════════════════════
// PAKET SATIS-2 — HASIM DOĞRULAYICI SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT metindir (üretim
// kodundan hesaplanmaz). SATIS-2 hesap yapmaz; para GÖSTEREN yeni yolu
// (PRO alt çubuk ödeme özeti "Nakit · Kasa · tutar" — FisProOdemeHapi.TekOzet),
// yeni saf fonksiyonu (Islemler.KisaAd) ve Katlanir sayı rozetini sınar.
// Biçim: Bicim.Tl — AwayFromZero 2 hane, binlik '.', ondalık ',', NBSP + ₺,
// eksi işareti U+2212.
// ════════════════════════════════════════════════════════════════════════
public sealed class SATIS2SinirTestleri : BunitContext
{
    public SATIS2SinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        GocC2Ortak.IzgaraServisleri(Services);   // GOC-C-2: tuval tabloları ortak ızgarada (ApiIstemci ister)
    }

    private string Ozet(decimal toplam, string? tur = "NAKIT", string? kasa = "Ana Kasa")
    {
        var c = Render<FisProOdemeHapi>(p => p
            .Add(x => x.Karma, false).Add(x => x.Toplam, toplam)
            .Add(x => x.OdemeTuru, tur).Add(x => x.KasaAdi, kasa));
        return c.Find("#islem-pro-odeme-ozet").TextContent.Trim();
    }

    // ── Ödeme özeti tutarı (para gösteren yeni yol) ─────────────────────────
    [Fact] public void Ozet_sifir_0_TL() => Assert.Equal("Nakit · Ana Kasa · 0 ₺", Ozet(0m));
    [Fact] public void Ozet_kurus_0_005_yukari_0_01() => Assert.Equal("Nakit · Ana Kasa · 0,01 ₺", Ozet(0.005m));
    [Fact] public void Ozet_kurus_0_0049_asagi_0() => Assert.Equal("Nakit · Ana Kasa · 0 ₺", Ozet(0.0049m));
    [Fact] public void Ozet_kurus_1500_505_yukari() => Assert.Equal("Nakit · Ana Kasa · 1.500,51 ₺", Ozet(1500.505m));
    [Fact] public void Ozet_cok_buyuk_999_999_999_99() => Assert.Equal("Nakit · Ana Kasa · 999.999.999,99 ₺", Ozet(999_999_999.99m));
    [Fact] public void Ozet_eksi_isaret_korunur() => Assert.Equal("Nakit · Ana Kasa · −1.500,50 ₺", Ozet(-1500.5m));
    [Fact] public void Ozet_TRY_kalibi_yok_binlik_nokta() => Assert.DoesNotContain("TRY", Ozet(45000m));

    // ── KisaAd (satıcı özet yazımı) — boş / null / Türkçe büyük harf ─────────
    [Fact] public void KisaAd_null_null_bos() => Assert.Equal("", Kuyumcu.Web.Pages.Islemler.KisaAd(null, null));
    [Fact] public void KisaAd_soyadsiz_yalniz_ad() => Assert.Equal("Ayşe", Kuyumcu.Web.Pages.Islemler.KisaAd(" Ayşe ", "  "));
    [Fact] public void KisaAd_turkce_i_buyuk_I_noktali() => Assert.Equal("Ali İ.", Kuyumcu.Web.Pages.Islemler.KisaAd("Ali", "ince"));
    [Fact] public void KisaAd_adsiz_soyadli_bas_bosluk_yok() => Assert.Equal("Y.", Kuyumcu.Web.Pages.Islemler.KisaAd(null, "Yılmaz"));

    // ── Katlanir sayı rozeti: 0 bilinen sıfırdır ("0" yazar), null bilinmez ──
    [Fact]
    public void Katlanir_sayi_0_rozet_0_yazar()
    {
        var c = Render<Katlanir>(p => p.Add(x => x.Id, "s0").Add(x => x.Baslik, "Eski kayıtlar").Add(x => x.Sayi, 0));
        Assert.Equal("0", c.Find("#s0-sayi").TextContent.Trim());
    }

    // ═══ DOĞRULAYICI TUR 2 (25.09 akşam) — bağımsız sınır testleri ════════════
    // Beklenen değerler SABİT metin/sayı; NBSP normalize edilir (Bicim NBSP + ₺ basar).
    private static string N(string s) => s.Replace(' ', ' ').Replace(' ', ' ').Trim();

    private IRenderedComponent<FisNakitUyari> Uyari(decimal? had, decimal toplam) =>
        Render<FisNakitUyari>(p => p.Add(x => x.Had, had).Add(x => x.Toplam, toplam));

    // Nakit uyarısı görünürlüğü: toplam ≥ had × 0,8 (ekran oranı) — 30.000 had → 24.000 sınırı
    [Fact] public void D2_Nakit_uyari_sifir_toplam_gizli() => Assert.True(Uyari(30_000m, 0m).Find("#islem-nakit-uyari").HasAttribute("hidden"));
    [Fact] public void D2_Nakit_uyari_eksi_toplam_gizli() => Assert.True(Uyari(30_000m, -50_000m).Find("#islem-nakit-uyari").HasAttribute("hidden"));
    [Fact] public void D2_Nakit_uyari_sinirin_kurus_alti_gizli() => Assert.True(Uyari(30_000m, 23_999.99m).Find("#islem-nakit-uyari").HasAttribute("hidden"));
    [Fact] public void D2_Nakit_uyari_tam_sinirda_gorunur() => Assert.False(Uyari(30_000m, 24_000.00m).Find("#islem-nakit-uyari").HasAttribute("hidden"));
    [Fact] public void D2_Nakit_uyari_cok_buyuk_toplam_gorunur() => Assert.False(Uyari(30_000m, 999_999_999.99m).Find("#islem-nakit-uyari").HasAttribute("hidden"));
    [Fact] public void D2_Nakit_uyari_had_sifir_tutucu_gorunur_sayisiz() {
        var c = Uyari(0m, 0m);
        Assert.False(c.Find("#islem-nakit-uyari").HasAttribute("hidden"));
        Assert.DoesNotContain("0 ₺", N(c.Find("#islem-nakit-uyari").TextContent));   // uydurma rakam yok
    }
    [Fact] public void D2_Nakit_uyari_had_eksi_tutucu_gorunur() => Assert.False(Uyari(-1m, 0m).Find("#islem-nakit-uyari").HasAttribute("hidden"));
    [Fact] public void D2_Nakit_uyari_had_kurusu_tr_TR() {
        var t = N(Uyari(30_000.005m, 0m).Find("#islem-nakit-uyari").TextContent);
        Assert.Contains("30.000,01 ₺", t);                                            // AwayFromZero, tr-TR
        Assert.DoesNotContain("TRY", t);
    }
    [Fact] public void D2_Nakit_uyari_had_null_gorunur_rakam_yok() {
        var t = N(Uyari(null, 1_000_000m).Find("#islem-nakit-uyari").TextContent);
        Assert.DoesNotMatch(@"\d", t);
    }

    // Son satışlar (PRO tablo): tutar Math.Abs + tr-TR; eksi (iade kaydı) işaretsiz; büyük tutar; kuruş
    private string SonSatisTutar(decimal tl)
    {
        var c = Render<FisSonSatislar>(p => p
            .Add(x => x.Satislar, new List<Kuyumcu.Contracts.Sayfalar.Islemler.IadeAdayi> {
                new("h1", new DateTime(2026, 9, 25, 10, 0, 0), tl, "u1", "Yüzük", 1m, 0m, 1m, null) })
            .Add(x => x.Yuklendi, true).Add(x => x.Kirilim, new OdemeKirilimi()).Add(x => x.Basit, false));
        c.Find("#islem-son-satislar-ac").Click();
        return N(c.Find("#islem-son-satislar tbody td.sayi.hucre-vurgu").TextContent);
    }
    [Fact] public void D2_SonSatis_sifir() => Assert.Equal("0 ₺", SonSatisTutar(0m));
    [Fact] public void D2_SonSatis_eksi_mutlak() => Assert.Equal("1.500,50 ₺", SonSatisTutar(-1500.5m));
    [Fact] public void D2_SonSatis_kurus_0_005_yukari() => Assert.Equal("0,01 ₺", SonSatisTutar(0.005m));
    [Fact] public void D2_SonSatis_kurus_0_0049_asagi() => Assert.Equal("0 ₺", SonSatisTutar(0.0049m));
    [Fact] public void D2_SonSatis_cok_buyuk() => Assert.Equal("999.999.999,99 ₺", SonSatisTutar(999_999_999.99m));

    // Türkçe giriş (tek merkez FormAlani.SayiCoz) — paket girişe dokunmadı; gerileme bekçisi
    [Theory]
    [InlineData("45.000", "Tamam", "45000")]
    [InlineData("1,5", "Tamam", "1.5")]
    [InlineData("0,0001", "Tamam", "0.0001")]
    [InlineData("999.999.999,99", "Tamam", "999999999.99")]
    [InlineData("1.500", "Belirsiz", null)]
    [InlineData("", "Bos", null)]
    [InlineData(null, "Bos", null)]
    public void D2_SayiCoz_turkce(string? ham, string sonuc, string? deger)
    {
        var r = Kuyumcu.Web.Bilesenler.Form.FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, r.Sonuc.ToString());
        Assert.Equal(deger is null ? null : decimal.Parse(deger, System.Globalization.CultureInfo.InvariantCulture), r.Deger);
    }
}
