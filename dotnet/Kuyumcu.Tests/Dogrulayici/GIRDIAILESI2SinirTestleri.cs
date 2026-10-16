using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET GIRDI-AILESI-2 finansal sınır testleri (08.10.2026).
// Paket Core'da tutar / gram / has / kur / adet kurallarını ve sunucu özniteliklerini yazdı.
// Beklenen değerler SABİTTİR (üretim kodundan hesaplanmaz). Proje kuralı: fazla ondalık
// SESSİZCE YUVARLANMAZ, hata olur (envanter K9 "fazlası sessizce yuvarlanmaz, hata").
// olumsuz-fikstür: dosya — geçersiz değerler bilinçlidir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GIRDIAILESI2SinirTestleri
{
    private static GirdiKodu? K(GirdiHatasi? h) => h?.Kod;

    // ── sıfır ────────────────────────────────────────────────────────────────
    [Fact]
    public void Sifir_tutar_Pozitifte_red_SifirOlabilirde_kabul_Isaretlide_red()
    {
        Assert.Equal(GirdiKodu.SayiSifir, K(TutarKurali.Denetle(0m)));
        Assert.Null(TutarKurali.Denetle(0m, TutarTuru.SifirOlabilir));
        Assert.Equal(GirdiKodu.SayiSifir, K(TutarKurali.Denetle(0m, TutarTuru.Isaretli)));
        Assert.Equal(GirdiKodu.SayiSifir, K(AgirlikKurali.Gram(0m)));
        Assert.Equal(GirdiKodu.SayiSifir, K(AgirlikKurali.Has(0m)));
        Assert.Equal(GirdiKodu.SayiSifir, K(KurKurali.Denetle(0m)));
        Assert.Equal(GirdiKodu.SayiEnAz, K(AdetKurali.Denetle(0m)));
    }

    // ── eksi ─────────────────────────────────────────────────────────────────
    [Fact]
    public void Eksi_deger_reddedilir_Isaretli_tutarda_isaret_korunur()
    {
        Assert.Equal(GirdiKodu.SayiEksi, K(TutarKurali.Denetle(-0.01m)));
        Assert.Equal(GirdiKodu.SayiEksi, K(TutarKurali.Denetle(-5m, TutarTuru.SifirOlabilir)));
        Assert.Null(TutarKurali.Denetle(-5m, TutarTuru.Isaretli));
        Assert.Equal(GirdiKodu.SayiEksi, K(AgirlikKurali.Gram(-0.001m)));
        Assert.Equal(GirdiKodu.SayiEksi, K(AgirlikKurali.Has(-0.0001m)));
        Assert.Equal(GirdiKodu.SayiEksi, K(AgirlikKurali.Tas(-1m, 10m)));
        Assert.Equal(GirdiKodu.SayiEksi, K(KurKurali.Denetle(-1m)));
        // SayisalAlan EnAz=0 → "-5" artık aralık dışı (ölü parametre kapandı)
        Assert.Equal(GirdiKodu.SayiEksi, K(SayiKurali.Aralik(-5m, 0m, null)));
    }

    // ── kuruş yuvarlaması (0,005 sınırı) ──────────────────────────────────────
    [Fact]
    public void Kurus_0_005_sessizce_yuvarlanmaz_hata_0_01_gecer()
    {
        Assert.Equal(GirdiKodu.SayiOndalik, K(TutarKurali.Denetle(0.005m)));
        Assert.Equal(GirdiKodu.SayiOndalik, K(TutarKurali.Denetle(0.004m)));
        Assert.Equal(GirdiKodu.SayiOndalik, K(TutarKurali.Denetle(10.015m)));
        Assert.Null(TutarKurali.Denetle(0.01m));
        Assert.Null(TutarKurali.Denetle(10.10m));
        Assert.Null(TutarKurali.Denetle(10.100m));   // sondaki sıfır ondalık sayılmaz
    }

    // ── has / gram 4 hane ─────────────────────────────────────────────────────
    [Fact]
    public void Has_0_0001_gecer_7_hane_red_Gram_4_hane_red()
    {
        Assert.Null(AgirlikKurali.Has(0.0001m));
        Assert.Null(AgirlikKurali.Has(0.000001m));
        Assert.Equal(GirdiKodu.SayiOndalik, K(AgirlikKurali.Has(0.0000001m)));
        Assert.Equal(GirdiKodu.SayiOndalik, K(AgirlikKurali.Gram(0.0001m)));
        Assert.Null(AgirlikKurali.Gram(0.001m));
        Assert.Equal(GirdiKodu.TasBrut, K(AgirlikKurali.Tas(10m, 10m)));
        Assert.Null(AgirlikKurali.Tas(9.999m, 10m));
    }

    // ── çok büyük tutar ───────────────────────────────────────────────────────
    [Fact]
    public void Cok_buyuk_tutar_999_999_999_99_gecer_kolon_tavani_ustu_red()
    {
        Assert.Null(TutarKurali.Denetle(999_999_999.99m));
        Assert.Null(TutarKurali.Denetle(9_999_999_999_999.99m));
        Assert.Equal(GirdiKodu.SayiTavan, K(TutarKurali.Denetle(10_000_000_000_000.00m)));
        Assert.Equal(GirdiKodu.SayiTavan, K(AgirlikKurali.Gram(1_000_000_000m)));
        Assert.Equal(GirdiKodu.SayiTavan, K(KurKurali.Denetle(1_000_000_000m)));
    }

    [Fact]
    public void Decimal_sinirinda_kesirli_dev_deger_istisna_firlatmaz_hata_kodu_doner()
    {
        // 29 anlamlı hane: decimal'a sığar, ama ×10 taşar. Kural İSTİSNA değil hata kodu dönmeli
        // (aksi hâlde sunucuda 500, ekranda yakalanmamış Blazor hatası).
        var dev = 7922816251426433759354395033.6m;
        var h = Record.Exception(() => TutarKurali.Denetle(dev));
        Assert.Null(h);
        Assert.NotNull(TutarKurali.Denetle(dev));
        Assert.Null(Record.Exception(() => AgirlikKurali.Has(dev)));
        Assert.Null(Record.Exception(() => KurKurali.Denetle(dev)));
    }

    // ── boş / null ────────────────────────────────────────────────────────────
    [Fact]
    public void Bos_null_degerde_kural_null_doner_zorunluluk_cagiranin()
    {
        Assert.Null(TutarKurali.Denetle(null));
        Assert.Null(AgirlikKurali.Gram(null));
        Assert.Null(AgirlikKurali.Has(null));
        Assert.Null(KurKurali.Denetle(null));
        Assert.Null(AdetKurali.Denetle(null));
        Assert.Null(IbanDogrulayici.Denetle(null));
        Assert.Null(IbanDogrulayici.Denetle("   "));
        Assert.False(IbanDogrulayici.Gecerli("   "));
    }

    // ── Türkçe giriş (FormAlani.SayiCoz) ─────────────────────────────────────
    [Fact]
    public void Turkce_giris_45_000_ve_1_5_cozulur_belirsiz_1_500_reddedilir()
    {
        var a = FormAlani.SayiCoz("45.000");
        var b = FormAlani.SayiCoz("1,5");
        var c = FormAlani.SayiCoz("1.500");
        Assert.Equal(45000m, a.Deger);
        Assert.Equal(1.5m, b.Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, c.Sonuc);
        Assert.Null(c.Deger);
    }

    // ── sunucu özniteliği: aynı kural + aynı sözlük metni ─────────────────────
    private sealed class OrnekDto
    {
        [Tutar] public decimal? Tutar { get; set; }
        [Has] public decimal? Has { get; set; }
        [Iban] public string? Iban { get; set; }
    }

    [Fact]
    public void Oznitelik_0_005_tutar_ve_bozuk_IBAN_ayni_sozluk_metniyle_reddeder()
    {
        var dto = new OrnekDto { Tutar = 0.005m, Has = 0.0001m, Iban = "TR340006100519786457841326" };
        var sonuc = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(dto, new ValidationContext(dto), sonuc, true));
        Assert.Equal(2, sonuc.Count);
        Assert.Contains(sonuc, r => r.MemberNames.Contains("Tutar") && r.ErrorMessage == GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.SayiOndalik, 2), "tr"));
        Assert.Contains(sonuc, r => r.MemberNames.Contains("Iban") && r.ErrorMessage == GirdiSozlugu.Tr.IbanKontrol);
    }

    // ── IBAN: bilinen geçerli örnek + tek hane değişimi ──────────────────────
    [Fact]
    public void Iban_bilinen_gecerli_ornek_ve_son_hane_degisimi()
    {
        Assert.Null(IbanDogrulayici.Denetle("TR330006100519786457841326"));
        Assert.Equal(GirdiKodu.IbanKontrol, K(IbanDogrulayici.Denetle("TR330006100519786457841327")));
        Assert.Equal(GirdiKodu.IbanUzunluk, K(IbanDogrulayici.Denetle("TR")));
    }

    // ═══ TUR 2 (hasım doğrulayıcı, 08.10 06:2x) — Gram bileşeni Tur=Gram/Tas/Has ve sunucu öznitelikleri ═══
    // Beklenenler SABİT; AgirlikKurali/TutarKurali/KurKurali'dan hesaplanmaz.
    [Theory]
    [InlineData("0.001", null)]                       // gram 3 ondalık: en küçük geçerli
    [InlineData("0.0001", GirdiKodu.SayiOndalik)]     // gram 4 hane: RED (sessiz yuvarlama yok)
    [InlineData("0", GirdiKodu.SayiSifir)]
    [InlineData("-0.001", GirdiKodu.SayiEksi)]
    [InlineData("999999999.999", null)]               // decimal(12,3) tavanı tam sınır
    [InlineData("1000000000", GirdiKodu.SayiTavan)]   // tavan + 1
    public void T2_Gram_turu_sinirlari(string d, GirdiKodu? beklenen) =>
        Assert.Equal(beklenen, K(AgirlikKurali.Denetle(decimal.Parse(d, System.Globalization.CultureInfo.InvariantCulture), AgirlikTuru.Gram)));

    [Theory]
    [InlineData("0.0001", null)]                      // has 6 ondalık: 4 hane geçer
    [InlineData("0.000001", null)]
    [InlineData("0.0000001", GirdiKodu.SayiOndalik)]
    [InlineData("999999999.999999", null)]
    [InlineData("1000000000", GirdiKodu.SayiTavan)]
    public void T2_Has_turu_sinirlari(string d, GirdiKodu? beklenen) =>
        Assert.Equal(beklenen, K(AgirlikKurali.Denetle(decimal.Parse(d, System.Globalization.CultureInfo.InvariantCulture), AgirlikTuru.Has)));

    [Fact]
    public void T2_Tas_brut_esitken_red_bir_binde_eksikken_gecer_brut_yoksa_yalniz_bicim()
    {
        Assert.Equal(GirdiKodu.TasBrut, K(AgirlikKurali.Tas(2.500m, 2.500m)));
        Assert.Null(AgirlikKurali.Tas(2.499m, 2.500m));
        Assert.Equal(GirdiKodu.TasBrut, K(AgirlikKurali.Tas(0.001m, 0m)));   // brüt 0 iken her taş > brüt
        Assert.Null(AgirlikKurali.Tas(0m, 2.5m));                               // taş 0 olabilir
        Assert.Null(AgirlikKurali.Tas(1.5m, null));
        Assert.Equal(GirdiKodu.SayiOndalik, K(AgirlikKurali.Tas(0.0005m, 2.5m)));
    }

    [Fact]
    public void T2_Tur_verilmezse_agirlik_kurali_yok()
    {
        Assert.Null(AgirlikKurali.Denetle(-5m, null));
        Assert.Null(AgirlikKurali.Denetle(0m, null));
    }

    [Fact]
    public void T2_Kur_6_ondalik_sinir_ve_sifir_eksi()
    {
        Assert.Null(KurKurali.Denetle(0.000001m));
        Assert.Equal(GirdiKodu.SayiOndalik, K(KurKurali.Denetle(0.0000001m)));
        Assert.Equal(GirdiKodu.SayiSifir, K(KurKurali.Denetle(0m)));
        Assert.Equal(GirdiKodu.SayiEksi, K(KurKurali.Denetle(-1m)));
    }

    [Fact]
    public void T2_Isaretli_tutar_sifiri_reddeder_eksi_kurusu_2_haneyle_kabul_eder()
    {
        Assert.Equal(GirdiKodu.SayiSifir, K(TutarKurali.Denetle(0m, TutarTuru.Isaretli)));
        Assert.Null(TutarKurali.Denetle(-0.01m, TutarTuru.Isaretli));
        Assert.Equal(GirdiKodu.SayiOndalik, K(TutarKurali.Denetle(-0.005m, TutarTuru.Isaretli)));
        Assert.Null(TutarKurali.Denetle(-999_999_999.99m, TutarTuru.Isaretli));
        Assert.Equal(GirdiKodu.SayiTavan, K(TutarKurali.Denetle(-10_000_000_000_000m, TutarTuru.Isaretli)));
    }

    private sealed class AgirlikDto
    {
        [Gram] public decimal? Gram { get; set; }
        [Has] public decimal? Has { get; set; }
        [Tutar(TutarTuru.SifirOlabilir)] public decimal? Nakit { get; set; }
    }

    [Fact]
    public void T2_Sunucu_Gram_Has_Tutar_oznitelikleri_sozluk_metniyle()
    {
        var dto = new AgirlikDto { Gram = 0.0001m, Has = -1m, Nakit = 0m };
        var sonuc = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(dto, new ValidationContext(dto), sonuc, true));
        Assert.Equal(2, sonuc.Count);   // Nakit = 0 SifirOlabilir'de GEÇER
        Assert.Contains(sonuc, r => r.MemberNames.Contains("Gram") && r.ErrorMessage == "Virgülden sonra en çok 3 hane yazılır.");
        Assert.Contains(sonuc, r => r.MemberNames.Contains("Has") && r.ErrorMessage == "Eksi değer yazılamaz.");
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI TUR 5 (08.10 13:xx, Aşkın + Mehmet) — kendi sınır testlerim.
// Beklenenler SABİT yazıldı; üretim kodundan hesaplanmaz. olumsuz-fikstür: dosya.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class GIRDIAILESI2Tur5SinirTestleri
{
    private static GirdiKodu? K(GirdiHatasi? h) => h?.Kod;

    [Theory]
    [InlineData("0", TutarTuru.Pozitif, GirdiKodu.SayiSifir)]
    [InlineData("0", TutarTuru.SifirOlabilir, null)]
    [InlineData("0", TutarTuru.Isaretli, GirdiKodu.SayiSifir)]
    [InlineData("-0.01", TutarTuru.Pozitif, GirdiKodu.SayiEksi)]
    [InlineData("-0.01", TutarTuru.SifirOlabilir, GirdiKodu.SayiEksi)]
    [InlineData("-0.01", TutarTuru.Isaretli, null)]
    [InlineData("0.005", TutarTuru.Pozitif, GirdiKodu.SayiOndalik)]
    [InlineData("-0.005", TutarTuru.Isaretli, GirdiKodu.SayiOndalik)]
    [InlineData("0.01", TutarTuru.Pozitif, null)]
    [InlineData("1.500", TutarTuru.Pozitif, null)]                 // sondaki sıfır hane sayılmaz (decimal ölçeği 3)
    [InlineData("1.501", TutarTuru.Pozitif, GirdiKodu.SayiOndalik)]
    [InlineData("999999999.99", TutarTuru.Pozitif, null)]
    [InlineData("9999999999999.99", TutarTuru.Pozitif, null)]     // decimal(15,2) tam tavan
    [InlineData("10000000000000.00", TutarTuru.Pozitif, GirdiKodu.SayiTavan)]
    [InlineData("-10000000000000", TutarTuru.Isaretli, GirdiKodu.SayiTavan)]
    public void DG5_Tutar_sinirlari(string d, TutarTuru tur, GirdiKodu? beklenen) =>
        Assert.Equal(beklenen, K(TutarKurali.Denetle(decimal.Parse(d, System.Globalization.CultureInfo.InvariantCulture), tur)));

    [Fact]
    public void DG5_Bos_null_her_kuralda_null()
    {
        Assert.Null(TutarKurali.Denetle(null));
        Assert.Null(AgirlikKurali.Has(null));
        Assert.Null(AdetKurali.Denetle(null));
        Assert.Null(KurKurali.Denetle(null));
        Assert.Null(IbanDogrulayici.Denetle("   "));
        Assert.Null(TelefonDogrulayici.Denetle(""));
    }

    [Theory]
    [InlineData("0.0001", null)]                         // has 4 hane geçer
    [InlineData("0.000001", null)]                       // has 6 hane geçer
    [InlineData("0.0000001", GirdiKodu.SayiOndalik)]     // 7 hane red
    [InlineData("999999999.999999", null)]
    [InlineData("1000000000", GirdiKodu.SayiTavan)]
    public void DG5_Has_sinirlari(string d, GirdiKodu? beklenen) =>
        Assert.Equal(beklenen, K(AgirlikKurali.Has(decimal.Parse(d, System.Globalization.CultureInfo.InvariantCulture))));

    [Theory]
    [InlineData("1", null)]
    [InlineData("0", GirdiKodu.SayiEnAz)]
    [InlineData("1.5", GirdiKodu.SayiTam)]
    [InlineData("-1", GirdiKodu.SayiEnAz)]
    [InlineData("2.000", null)]                          // 2,000 tam sayıdır
    public void DG5_Adet_sinirlari(string d, GirdiKodu? beklenen) =>
        Assert.Equal(beklenen, K(AdetKurali.Denetle(decimal.Parse(d, System.Globalization.CultureInfo.InvariantCulture))));

    [Theory]
    [InlineData("1000", null)]
    [InlineData("1000.001", GirdiKodu.MilyemAralik)]
    [InlineData("0.999", GirdiKodu.MilyemAralik)]
    [InlineData("916.6667", GirdiKodu.SayiOndalik)]
    public void DG5_Milyem_sinirlari(string d, GirdiKodu? beklenen) =>
        Assert.Equal(beklenen, K(MilyemKurali.Denetle(decimal.Parse(d, System.Globalization.CultureInfo.InvariantCulture))));

    [Fact]
    public void DG5_Turkce_giris_SayiCoz()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Equal(999999999.99m, FormAlani.SayiCoz("999.999.999,99").Deger);
    }

    [Theory]
    [InlineData("tr33 0006 1005 1978 6457 8413 26", null)]            // küçük harf + boşluk normalleşir
    [InlineData("TR330006100519786457841326 ", null)]
    [InlineData("TR33000610051978645784132", GirdiKodu.IbanUzunluk)]   // 25
    [InlineData("TR3300061005197864578413261", GirdiKodu.IbanUzunluk)] // 27
    [InlineData("TR33A006100519786457841326", GirdiKodu.IbanBicim)]    // banka kodunda harf
    [InlineData("TR430006100519786457841326", GirdiKodu.IbanKontrol)]  // kontrol hanesi
    [InlineData("DE89370400440532013000", null)]
    [InlineData("DE89370400440532013001", GirdiKodu.IbanKontrol)]
    public void DG5_Iban_sinirlari(string iban, GirdiKodu? beklenen) =>
        Assert.Equal(beklenen, K(IbanDogrulayici.Denetle(iban)));

    [Theory]
    [InlineData("0532 411 22 33", null)]
    [InlineData("+90 532 411 22 33", null)]
    [InlineData("0090 532 411 22 33", null)]
    [InlineData("+90 500 111 22 33", GirdiKodu.TelefonAralik)]
    [InlineData("0532 411 22 3", GirdiKodu.TelefonHane)]
    [InlineData("0532-411-22-33a", GirdiKodu.TelefonKarakter)]
    [InlineData("0212 555 12 34", null)]
    [InlineData("0612 555 12 34", GirdiKodu.TelefonAralik)]
    public void DG5_Telefon_sinirlari(string tel, GirdiKodu? beklenen) =>
        Assert.Equal(beklenen, K(TelefonDogrulayici.Denetle(tel)));

    private sealed class Dg5Dto
    {
        [Telefon] public string? Telefon { get; set; }
        [Tutar(TutarTuru.SifirOlabilir)] public decimal? Nakit { get; set; }
    }

    [Fact]
    public void DG5_Sunucu_ozniteligi_telefon_aralik_ve_eksi_nakit_ayni_metin()
    {
        var dto = new Dg5Dto { Telefon = "0500 111 22 33", Nakit = -5m };
        var sonuc = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(dto, new ValidationContext(dto), sonuc, true));
        Assert.Equal(2, sonuc.Count);
        Assert.Contains(sonuc, r => r.MemberNames.Contains("Telefon") && r.ErrorMessage == "Bu numara Türkiye'de kullanılan bir telefon numarası değil.");
        Assert.Contains(sonuc, r => r.MemberNames.Contains("Nakit") && r.ErrorMessage == "Eksi değer yazılamaz.");
    }
}
