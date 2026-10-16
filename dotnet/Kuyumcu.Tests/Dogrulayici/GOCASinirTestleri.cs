using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Bilesenler.Grid;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET GOC-A finansal sınır testleri (08.10.2026).
// Paket servis yolu kapısını (GirdiKapisi) ve para DTO'larına ortak öznitelikleri ekledi.
// Beklenen değerler SABİTTİR (üretim kodundan hesaplanmaz). Proje kuralı (GOC-A): fazla ondalık
// SESSİZCE YUVARLANMAZ — 400 olur; 0,005 kuruş sınırı yuvarlanmaz, reddedilir.
// olumsuz-fikstür: dosya — geçersiz değerler bilinçlidir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GOCASinirTestleri
{
    private static string Alan(ApiException ex) => AlanIsareti.Alan(ex)!;

    // ── sıfır ────────────────────────────────────────────────────────────────
    [Fact]
    public void GOCA_S01_Sifir_tutar_Pozitifte_400_SifirOlabilirde_aynen_doner()
    {
        var ex = Assert.Throws<BadRequestException>(() => GirdiKapisi.Tutar(0m, "tutar"));
        Assert.Equal("Değer 0'dan büyük olmalı.", ex.Message);
        Assert.Equal("tutar", Alan(ex));
        Assert.Equal(0m, GirdiKapisi.Tutar(0m, "sayilanNakit", TutarTuru.SifirOlabilir));
        Assert.Throws<BadRequestException>(() => GirdiKapisi.Gram(0m, "gramAgirlik"));
        Assert.Throws<BadRequestException>(() => GirdiKapisi.Has(0m, "hasGrami"));
        Assert.Throws<BadRequestException>(() => GirdiKapisi.Kur(0m, "usdTry"));
        Assert.Throws<BadRequestException>(() => GirdiKapisi.Adet(0m, "adet"));
    }

    // ── eksi ─────────────────────────────────────────────────────────────────
    [Fact]
    public void GOCA_S02_Eksi_reddedilir_isaretli_has_isaretini_korur()
    {
        var ex = Assert.Throws<BadRequestException>(() => GirdiKapisi.Tutar(-0.01m, "tutar", TutarTuru.SifirOlabilir));
        Assert.Equal("Eksi değer yazılamaz.", ex.Message);
        Assert.Throws<BadRequestException>(() => GirdiKapisi.Kur(-1m, "gramAltinFiyati"));
        Assert.Throws<BadRequestException>(() => GirdiKapisi.Gram(-0.001m, "miktar"));
        Assert.Equal(-0.0001m, GirdiKapisi.IsaretliHas(-0.0001m, "ekFark"));   // tahlil farkı eksi olabilir, işaret aynen
    }

    // ── kuruş: 0,005 sınırı YUVARLANMAZ ──────────────────────────────────────
    [Theory]
    [InlineData("0.005")]
    [InlineData("1.005")]
    [InlineData("0.004")]
    [InlineData("999999999.995")]
    public void GOCA_S03_Kurus_alti_yuvarlanmaz_400_virgulden_sonra_2_hane(string ham)
    {
        var d = decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        var ex = Assert.Throws<BadRequestException>(() => GirdiKapisi.Tutar(d, "tutar"));
        Assert.Equal("Virgülden sonra en çok 2 hane yazılır.", ex.Message);
    }

    [Fact]
    public void GOCA_S04_Tam_kurus_degismeden_doner()
    {
        Assert.Equal(0.01m, GirdiKapisi.Tutar(0.01m, "tutar"));
        Assert.Equal(1.01m, GirdiKapisi.Tutar(1.01m, "tutar"));
        Assert.Equal(12.30m, GirdiKapisi.Tutar(12.30m, "tutar"));
    }

    // ── has / gram 4 hane (0,0001) ───────────────────────────────────────────
    [Fact]
    public void GOCA_S05_Has_0_0001_kabul_gram_0_0001_red_has_7_hane_red()
    {
        Assert.Equal(0.0001m, GirdiKapisi.Has(0.0001m, "hasGrami"));
        Assert.Equal(0.123456m, GirdiKapisi.Has(0.123456m, "hasGrami"));
        var g = Assert.Throws<BadRequestException>(() => GirdiKapisi.Gram(0.0001m, "gramAgirlik"));
        Assert.Equal("Virgülden sonra en çok 3 hane yazılır.", g.Message);
        Assert.Equal(0.001m, GirdiKapisi.Gram(0.001m, "gramAgirlik"));
        var h = Assert.Throws<BadRequestException>(() => GirdiKapisi.Has(0.0000001m, "hasGrami"));
        Assert.Equal("Virgülden sonra en çok 6 hane yazılır.", h.Message);
        // kur kolonu decimal(15,4): 4 hane kabul, 5 hane red
        Assert.Equal(41.5678m, GirdiKapisi.Kur(41.5678m, "usdTry", 4));
        Assert.Throws<BadRequestException>(() => GirdiKapisi.Kur(41.56789m, "usdTry", 4));
    }

    // ── çok büyük tutar ──────────────────────────────────────────────────────
    [Fact]
    public void GOCA_S06_Cok_buyuk_tutar_999_999_999_99_kabul_kolon_tavani_ustu_400()
    {
        Assert.Equal(999_999_999.99m, GirdiKapisi.Tutar(999_999_999.99m, "tutar"));
        Assert.Equal(9_999_999_999_999.99m, GirdiKapisi.Tutar(9_999_999_999_999.99m, "tutar"));   // decimal(15,2) tam sınır
        var ex = Assert.Throws<BadRequestException>(() => GirdiKapisi.Tutar(10_000_000_000_000m, "tutar"));
        Assert.StartsWith("Değer çok büyük", ex.Message);
    }

    // ── boş / null ───────────────────────────────────────────────────────────
    [Fact]
    public void GOCA_S07_Bos_null_tutar_ve_kur_gecer_tarih_null_gecer()
    {
        Assert.Null(GirdiKapisi.Tutar((decimal?)null, "tutar"));
        Assert.Null(GirdiKapisi.Kur(null, "usdTry"));
        GirdiKapisi.IslemTarihi(null, "tarih");                // boş tarih kural dışı (zorunluluk DTO'nun)
        Assert.Null(GirdiKapisi.Uzunluk(null, 500, "aciklama", "Açıklama"));
        var ex = Assert.Throws<BadRequestException>(() => GirdiKapisi.Gerekce(null, "gerekce"));
        Assert.Equal("gerekce", Alan(ex));
    }

    // ── adet tam sayı ────────────────────────────────────────────────────────
    [Fact]
    public void GOCA_S08_Adet_kesirli_400_tam_kabul()
    {
        Assert.Equal(2m, GirdiKapisi.Adet(2m, "adet"));
        Assert.Throws<BadRequestException>(() => GirdiKapisi.Adet(1.5m, "adet"));
        Assert.Throws<BadRequestException>(() => GirdiKapisi.Adet(0.999m, "adet"));
    }

    // ── gerekçe K30: tam sınır 20 ────────────────────────────────────────────
    [Fact]
    public void GOCA_S09_Gerekce_19_red_20_kabul_kirpilmis_uzunluk_500_ustu_red()
    {
        Assert.Throws<BadRequestException>(() => GirdiKapisi.Gerekce(new string('a', 19), "g"));
        Assert.Equal(new string('a', 20), GirdiKapisi.Gerekce("  " + new string('a', 20) + "  ", "g"));
        Assert.Throws<BadRequestException>(() => GirdiKapisi.Gerekce("   " + new string('a', 18) + "   ", "g"));
        Assert.Equal(500, GirdiKapisi.Gerekce(new string('a', 500), "g", 500).Length);
        Assert.Throws<BadRequestException>(() => GirdiKapisi.Gerekce(new string('a', 501), "g", 500));
    }

    // ── işlem tarihi: bugün kabul, yarın red ─────────────────────────────────
    [Fact]
    public void GOCA_S10_Islem_tarihi_bugun_kabul_yarin_400()
    {
        var bugun = Zaman.YerelBugun(DateTime.UtcNow);
        GirdiKapisi.IslemTarihi(bugun, "tarih");
        var ex = Assert.Throws<BadRequestException>(() => GirdiKapisi.IslemTarihi(bugun.AddDays(1), "tarih"));
        Assert.Equal("tarih", Alan(ex));
        Assert.Throws<BadRequestException>(() => GirdiKapisi.IslemTarihi(new DateOnly(3000, 1, 1), "tarih"));
        Assert.Throws<BadRequestException>(() => GirdiKapisi.Aralik(new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 1)));
        GirdiKapisi.Aralik(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1));   // aynı gün aralığı geçerli
    }

    // ── Türkçe giriş (FormAlani.SayiCoz) ─────────────────────────────────────
    [Fact]
    public void GOCA_S11_Turkce_giris_45_000_ve_1_5_cozulur_belirsiz_1_500_reddedilir()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        var c = FormAlani.SayiCoz("1.500");
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, c.Sonuc);
        Assert.Null(c.Deger);
        Assert.Equal(999_999_999.99m, FormAlani.SayiCoz("999.999.999,99").Deger);
    }

    // ── sunucu DTO özniteliği: kurus alti Kasa/Gider tutarı ModelState'te red ─
    [Fact]
    public void GOCA_S12_Kasa_hareket_DTO_tutar_0_005_ve_0_DataAnnotations_red_10_01_kabul()
    {
        static bool Gecerli(decimal t)
        {
            var dto = new Kuyumcu.Api.Modules.Kasa.KasaHareketDto { Yon = "GIRIS", Tutar = t, OdemeTuru = "NAKIT" };
            return Validator.TryValidateObject(dto, new ValidationContext(dto), new List<ValidationResult>(), true);
        }
        Assert.False(Gecerli(0.005m));
        Assert.False(Gecerli(0m));
        Assert.False(Gecerli(-1m));
        Assert.True(Gecerli(10.01m));
    }

    // ── ızgara yerel sıralaması: para değeri sayı olarak (metin olarak değil) ─
    [Fact]
    public void GOCA_S13_Yerel_siralama_tutari_sayisal_siralar_1000_9dan_buyuk_eksi_en_kucuk()
    {
        var kolonlar = new List<IzgaraKolonu<(string Ad, decimal? Tutar)>>
        {
            new() { Anahtar = "tutar", Baslik = "Tutar", Deger = x => x.Tutar },
        };
        var l = new List<(string Ad, decimal? Tutar)> { ("a", 9m), ("b", 1000m), ("c", -5m), ("d", null), ("e", 0.01m) };
        Assert.Equal(["c", "e", "a", "b", "d"],
            IzgaraYerelSiralama.Uygula(l, new IzgaraSiralama("tutar", "tutar", false), kolonlar).Select(x => x.Ad));
        Assert.Equal(["d", "b", "a", "e", "c"],
            IzgaraYerelSiralama.Uygula(l, new IzgaraSiralama("tutar", "tutar", true), kolonlar).Select(x => x.Ad));
    }
}
