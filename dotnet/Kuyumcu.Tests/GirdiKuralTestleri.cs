using System.ComponentModel.DataAnnotations;
using Kuyumcu.Api.Common.Dogrulama;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;

namespace Kuyumcu.Tests;

// olumsuz-fikstür: dosya — bu dosya kuralın SINIRLARINI sınar; geçersiz değerler bilinçlidir
// (denetim.sh TEST_GECERSIZ_FIKSTUR bu dosyayı saymaz).
// ═════════════════════════════════════════════════════════════════════════════
// PAKET GIRDI-AILESI-2 — Core girdi kurallarının SINIR testleri (saf, DB'siz).
// Kabul ölçütü 1: K1–K3, K7–K21 (+K30) için Core'da TEK kural; her kuralın alt,
// üst, tam sınır ve bir fazla testi. Kaynak: GIRDI-KURALLARI-ENVANTER-2026-10-08 (B).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GirdiKuralTestleri
{
    private static GirdiKodu? K(GirdiHatasi? h) => h?.Kod;

    // ── K1 IBAN ─────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("TR330006100519786457841326")]
    [InlineData("tr33 0006 1005 1978 6457 8413 26")]   // küçük harf + boşluk kabul
    [InlineData("GB82WEST12345698765432")]               // yabancı (ISO 13616 örneği)
    [InlineData("DE89370400440532013000")]
    public void K1_Iban_gecerli(string iban) => Assert.Null(IbanDogrulayici.Denetle(iban));

    [Fact]
    public void K1_Iban_hesap_no_bolumu_HARF_icerebilir_teblig_md4()
    {
        var iban = IbanDogrulayici.TrTamamla("00061" + "0" + "ABCDEFGH12345678");
        Assert.Null(IbanDogrulayici.Denetle(iban));
    }

    [Fact]
    public void K1_Iban_banka_kodunda_ya_da_rezervde_harf_IbanBicim()
    {
        Assert.Equal(GirdiKodu.IbanBicim, K(IbanDogrulayici.Denetle("TR3300A6100519786457841326")));
        Assert.Equal(GirdiKodu.IbanBicim, K(IbanDogrulayici.Denetle("TR3300061A0519786457841326")));   // 10. konum (rezerv)
        Assert.Equal(GirdiKodu.IbanBicim, K(IbanDogrulayici.Denetle("1233000610")));
    }

    [Fact]
    public void K1_Iban_kontrol_hanesi_tutmazsa_IbanKontrol()
    {
        Assert.Equal(GirdiKodu.IbanKontrol, K(IbanDogrulayici.Denetle("TR340006100519786457841326")));
        Assert.Equal(GirdiKodu.IbanKontrol, K(IbanDogrulayici.Denetle("TR330006100519786457841327")));
        Assert.Equal(GirdiKodu.IbanKontrol, K(IbanDogrulayici.Denetle("TR000000000000000000000001")));   // eski fikstür
    }

    [Theory]
    [InlineData("TR33000610051978645784132", 25)]   // bir eksik
    [InlineData("TR3300061005197864578413261", 27)] // bir fazla
    public void K1_Iban_TR_uzunluk_tam_26(string iban, int uzunluk)
    {
        var h = IbanDogrulayici.Denetle(iban);
        Assert.Equal(GirdiKodu.IbanUzunluk, K(h));
        Assert.Equal(uzunluk, h!.Value.S1);
    }

    [Fact]
    public void K1_TestIban_ve_TrTamamla_her_zaman_mod97_1()
    {
        for (var i = 0; i < 200; i++)
        {
            var u = TestIban.Uret();
            Assert.Equal(26, u.Length);
            Assert.Equal(1, IbanDogrulayici.Mod97(u));
            Assert.Null(IbanDogrulayici.Denetle(u));
        }
        Assert.Equal(TestIban.Uret("a"), TestIban.Uret("a"));
        Assert.NotEqual(TestIban.Uret("a"), TestIban.Uret("b"));
    }

    // ── K2 TELEFON ──────────────────────────────────────────────────────────
    [Theory]
    [InlineData("05324112233")]
    [InlineData("0532 411 22 33")]
    [InlineData("5324112233")]
    [InlineData("+90 532 411 22 33")]
    [InlineData("00905324112233")]
    [InlineData("02121234567")]
    [InlineData("04641234567")]
    [InlineData("08501234567")]
    [InlineData("08001234567")]
    [InlineData("05011234567")]   // 501 alt uç
    [InlineData("05491234567")]   // 530–549 üst uç
    [InlineData("+44 20 7946 0958")]
    public void K2_Telefon_gecerli(string tel) => Assert.Null(TelefonDogrulayici.Denetle(tel));

    [Theory]
    [InlineData("05001112233", GirdiKodu.TelefonAralik)]   // 500 BTK aralığında değil (eski fikstür)
    [InlineData("05021112233", GirdiKodu.TelefonAralik)]   // 502 listede yok
    [InlineData("05501112233", GirdiKodu.TelefonAralik)]   // 550 listede yok
    [InlineData("01234567890", GirdiKodu.TelefonAralik)]   // 1xx
    [InlineData("08111234567", GirdiKodu.TelefonAralik)]   // 811 hizmet listesinde değil
    [InlineData("0000000000", GirdiKodu.TelefonHane)]      // 0 + 9 hane
    [InlineData("053241122", GirdiKodu.TelefonHane)]       // 9 ulusal hane (bir eksik)
    [InlineData("053241122334", GirdiKodu.TelefonHane)]    // 11 ulusal hane (bir fazla)
    [InlineData("0532abc2233", GirdiKodu.TelefonKarakter)]
    [InlineData("05+324112233", GirdiKodu.TelefonKarakter)] // '+' yalnız başta
    [InlineData("+4412345", GirdiKodu.TelefonYabanci)]     // 7 hane — E.164 alt
    [InlineData("+4412345678901234", GirdiKodu.TelefonYabanci)] // 16 hane — bir fazla
    public void K2_Telefon_gecersiz(string tel, GirdiKodu beklenen) => Assert.Equal(beklenen, K(TelefonDogrulayici.Denetle(tel)));

    [Fact]
    public void K2_Telefon_E164_tam_15_hane_gecer() => Assert.Null(TelefonDogrulayici.Denetle("+441234567890123"));

    // ── K3 E-POSTA ──────────────────────────────────────────────────────────
    [Theory]
    [InlineData("ali.veli@ornek.com.tr")]
    [InlineData("a@b.co")]
    [InlineData("  Ali@Ornek.COM  ")]
    public void K3_Eposta_gecerli(string e) => Assert.Null(EpostaDogrulayici.Denetle(e));

    [Theory]
    [InlineData("ali")]
    [InlineData("ali@ornek")]          // alan adında nokta yok (iş kuralı)
    [InlineData("ali @ornek.com")]
    [InlineData("a@@ornek.com")]
    [InlineData("@ornek.com")]
    [InlineData("ali@")]
    [InlineData("ali@ornek..com")]
    [InlineData("ali@.com")]
    [InlineData("ali.@ornek.com")]
    [InlineData("ali@-ornek.com")]
    public void K3_Eposta_bicim_hatasi(string e) => Assert.Equal(GirdiKodu.EpostaBicim, K(EpostaDogrulayici.Denetle(e)));

    [Fact]
    public void K3_Eposta_yerel_64_sinir_ve_toplam_254()
    {
        Assert.Null(EpostaDogrulayici.Denetle(new string('a', 64) + "@ornek.com"));
        Assert.Equal(GirdiKodu.EpostaUzun, K(EpostaDogrulayici.Denetle(new string('a', 65) + "@ornek.com")));
        var alan = string.Join('.', Enumerable.Repeat(new string('b', 60), 4)) + ".co";   // 246
        var e254 = new string('a', 254 - 1 - alan.Length) + "@" + alan;
        Assert.Equal(254, e254.Length);
        Assert.Null(EpostaDogrulayici.Denetle(e254));
        Assert.Equal(GirdiKodu.EpostaUzun, K(EpostaDogrulayici.Denetle("a" + e254)));
        Assert.Equal("ali@ornek.com", EpostaDogrulayici.Normallestir("  Ali@Ornek.COM "));
    }

    // ── K8 TARİH ────────────────────────────────────────────────────────────
    private static readonly DateOnly Bugun = new(2026, 10, 8);

    [Theory]
    [InlineData("08102026", 2026, 10, 8)]
    [InlineData("08.10.2026", 2026, 10, 8)]
    [InlineData("2026-10-08", 2026, 10, 8)]
    [InlineData("29.02.2024", 2024, 2, 29)]
    public void K8_Tarih_coz_gecerli(string ham, int y, int a, int g)
    {
        var (h, d) = TarihPenceresi.Coz(ham);
        Assert.Null(h);
        Assert.Equal(new DateOnly(y, a, g), d);
    }

    [Theory]
    [InlineData("31.02.2026", GirdiKodu.TarihGecersiz)]
    [InlineData("29.02.2025", GirdiKodu.TarihGecersiz)]
    [InlineData("00.10.2026", GirdiKodu.TarihGecersiz)]
    [InlineData("08.13.2026", GirdiKodu.TarihGecersiz)]
    [InlineData("0810202", GirdiKodu.TarihBicim)]      // bir hane eksik
    [InlineData("081020261", GirdiKodu.TarihBicim)]    // bir hane fazla
    [InlineData("ab.10.2026", GirdiKodu.TarihBicim)]
    public void K8_Tarih_coz_gecersiz(string ham, GirdiKodu beklenen) => Assert.Equal(beklenen, K(TarihPenceresi.Coz(ham).Hata));

    [Fact]
    public void K8_Serbest_pencere_1900_2100_sinirlari()
    {
        Assert.Equal(GirdiKodu.TarihErken, K(TarihPenceresi.Denetle(new DateOnly(1899, 12, 31), TarihTuru.Serbest, Bugun)));
        Assert.Null(TarihPenceresi.Denetle(new DateOnly(1900, 1, 1), TarihTuru.Serbest, Bugun));
        Assert.Null(TarihPenceresi.Denetle(new DateOnly(2100, 12, 31), TarihTuru.Serbest, Bugun));
        Assert.Equal(GirdiKodu.TarihGec, K(TarihPenceresi.Denetle(new DateOnly(2101, 1, 1), TarihTuru.Serbest, Bugun)));
        Assert.Equal(GirdiKodu.TarihGec, K(TarihPenceresi.Denetle(new DateOnly(3000, 1, 1), TarihTuru.Serbest, Bugun)));
    }

    [Fact]
    public void K8a_Islem_ileri_tarih_yok_geri_bir_yil()
    {
        Assert.Null(TarihPenceresi.Denetle(Bugun, TarihTuru.Islem, Bugun));
        Assert.Equal(GirdiKodu.TarihIleri, K(TarihPenceresi.Denetle(Bugun.AddDays(1), TarihTuru.Islem, Bugun)));
        Assert.Null(TarihPenceresi.Denetle(Bugun.AddYears(-1), TarihTuru.Islem, Bugun));
        Assert.Equal(GirdiKodu.TarihCokEski, K(TarihPenceresi.Denetle(Bugun.AddYears(-1).AddDays(-1), TarihTuru.Islem, Bugun)));
    }

    [Fact]
    public void K8b_Dogum_18_yas_TMK_md11_ve_120_yil()
    {
        Assert.Null(TarihPenceresi.Denetle(Bugun.AddYears(-18), TarihTuru.Dogum, Bugun));
        Assert.Equal(GirdiKodu.DogumResit, K(TarihPenceresi.Denetle(Bugun.AddYears(-18).AddDays(1), TarihTuru.Dogum, Bugun)));
        Assert.Equal(GirdiKodu.DogumGelecek, K(TarihPenceresi.Denetle(Bugun.AddDays(1), TarihTuru.Dogum, Bugun)));
        Assert.Null(TarihPenceresi.Denetle(Bugun.AddYears(-120), TarihTuru.Dogum, Bugun));
        Assert.Equal(GirdiKodu.DogumCokEski, K(TarihPenceresi.Denetle(Bugun.AddYears(-120).AddDays(-1), TarihTuru.Dogum, Bugun)));
    }

    [Fact]
    public void K8c_Vade_bugun_ile_10_yil()
    {
        Assert.Null(TarihPenceresi.Denetle(Bugun, TarihTuru.Vade, Bugun));
        Assert.Equal(GirdiKodu.TarihErken, K(TarihPenceresi.Denetle(Bugun.AddDays(-1), TarihTuru.Vade, Bugun)));
        Assert.Null(TarihPenceresi.Denetle(Bugun.AddYears(10), TarihTuru.Vade, Bugun));
        Assert.Equal(GirdiKodu.TarihGec, K(TarihPenceresi.Denetle(Bugun.AddYears(10).AddDays(1), TarihTuru.Vade, Bugun)));
        // Çağıranın daha dar üst sınırı (rezerv ≤ 1 yıl) kesişir.
        Assert.Equal(GirdiKodu.TarihGec, K(TarihPenceresi.Denetle(Bugun.AddYears(1).AddDays(1), TarihTuru.Vade, Bugun, enGec: Bugun.AddYears(1))));
    }

    [Fact]
    public void K8d_Keside_TEYITSIZ_bugunku_davranis_yalniz_ortak_pencere()
    {
        Assert.Null(TarihPenceresi.Denetle(Bugun.AddYears(3), TarihTuru.Keside, Bugun));
        Assert.Null(TarihPenceresi.Denetle(Bugun.AddYears(-30), TarihTuru.Keside, Bugun));
        Assert.Equal(TarihPenceresi.Pencere(TarihTuru.Serbest, Bugun), TarihPenceresi.Pencere(TarihTuru.Keside, Bugun));
    }

    [Fact]
    public void K8e_Aralik_ters_ve_5_yil()
    {
        Assert.Equal(GirdiKodu.AralikTers, K(TarihPenceresi.AralikDenetle(Bugun, Bugun.AddDays(-1))));
        Assert.Null(TarihPenceresi.AralikDenetle(Bugun, Bugun));
        Assert.Null(TarihPenceresi.AralikDenetle(Bugun.AddYears(-5), Bugun));
        Assert.Equal(GirdiKodu.AralikUzun, K(TarihPenceresi.AralikDenetle(Bugun.AddYears(-5).AddDays(-1), Bugun)));
        Assert.Equal(GirdiKodu.TarihErken, K(TarihPenceresi.Denetle(new DateOnly(1999, 12, 31), TarihTuru.Rapor, Bugun)));
        Assert.Null(TarihPenceresi.Denetle(new DateOnly(2027, 12, 31), TarihTuru.Rapor, Bugun));
        Assert.Equal(GirdiKodu.TarihGec, K(TarihPenceresi.Denetle(new DateOnly(2028, 1, 1), TarihTuru.Rapor, Bugun)));
    }

    // ── K9 TUTAR ────────────────────────────────────────────────────────────
    [Fact]
    public void K9_Tutar_pozitif_sinirlar()
    {
        Assert.Null(TutarKurali.Denetle(0.01m));
        Assert.Equal(GirdiKodu.SayiSifir, K(TutarKurali.Denetle(0m)));
        Assert.Equal(GirdiKodu.SayiEksi, K(TutarKurali.Denetle(-0.01m)));
        Assert.Equal(GirdiKodu.SayiOndalik, K(TutarKurali.Denetle(0.001m)));
        Assert.Equal(GirdiKodu.SayiOndalik, K(TutarKurali.Denetle(10.005m)));
        Assert.Null(TutarKurali.Denetle(TutarKurali.Tavan));
        Assert.Equal(GirdiKodu.SayiTavan, K(TutarKurali.Denetle(TutarKurali.Tavan + 0.01m)));
    }

    [Fact]
    public void K9a_K9b_Tutar_sifir_olabilir_ve_isaretli()
    {
        Assert.Null(TutarKurali.Denetle(0m, TutarTuru.SifirOlabilir));
        Assert.Equal(GirdiKodu.SayiEksi, K(TutarKurali.Denetle(-0.01m, TutarTuru.SifirOlabilir)));
        Assert.Null(TutarKurali.Denetle(-5m, TutarTuru.Isaretli));
        Assert.Equal(GirdiKodu.SayiSifir, K(TutarKurali.Denetle(0m, TutarTuru.Isaretli)));
        Assert.Equal(GirdiKodu.SayiTavan, K(TutarKurali.Denetle(-TutarKurali.Tavan - 0.01m, TutarTuru.Isaretli)));
    }

    // ── K10/K11 GRAM, TAŞ, HAS ──────────────────────────────────────────────
    [Fact]
    public void K10_Gram_ve_tas_brutten_kucuk()
    {
        Assert.Null(AgirlikKurali.Gram(0.001m));
        Assert.Equal(GirdiKodu.SayiSifir, K(AgirlikKurali.Gram(0m)));
        Assert.Equal(GirdiKodu.SayiEksi, K(AgirlikKurali.Gram(-1m)));
        Assert.Equal(GirdiKodu.SayiOndalik, K(AgirlikKurali.Gram(0.0001m)));
        Assert.Null(AgirlikKurali.Gram(AgirlikKurali.GramTavan));
        Assert.Equal(GirdiKodu.SayiTavan, K(AgirlikKurali.Gram(AgirlikKurali.GramTavan + 0.001m)));
        Assert.Null(AgirlikKurali.Tas(0m, 5m));
        Assert.Null(AgirlikKurali.Tas(4.999m, 5m));
        Assert.Equal(GirdiKodu.TasBrut, K(AgirlikKurali.Tas(5m, 5m)));
        Assert.Equal(GirdiKodu.TasBrut, K(AgirlikKurali.Tas(5.001m, 5m)));
        Assert.Equal(GirdiKodu.SayiEksi, K(AgirlikKurali.Tas(-0.001m, 5m)));
    }

    [Fact]
    public void K11_Has_6_ondalik()
    {
        Assert.Null(AgirlikKurali.Has(0.000001m));
        Assert.Equal(GirdiKodu.SayiOndalik, K(AgirlikKurali.Has(0.0000001m)));
        Assert.Equal(GirdiKodu.SayiSifir, K(AgirlikKurali.Has(0m)));
        Assert.Equal(GirdiKodu.SayiEksi, K(AgirlikKurali.Has(-1m)));
        Assert.Equal(GirdiKodu.SayiTavan, K(AgirlikKurali.Has(AgirlikKurali.HasTavan + 0.000001m)));
    }

    // ── K12 MİLYEM · K13 AYAR · K14 ADET ────────────────────────────────────
    [Fact]
    public void K12_Milyem_1_1000_ve_sapma_uyarisi()
    {
        Assert.Null(MilyemKurali.Denetle(1m));
        Assert.Null(MilyemKurali.Denetle(1000m));
        Assert.Equal(GirdiKodu.MilyemAralik, K(MilyemKurali.Denetle(0.999m)));
        Assert.Equal(GirdiKodu.MilyemAralik, K(MilyemKurali.Denetle(1000.001m)));
        Assert.Null(MilyemKurali.Denetle(916.5m));
        Assert.Equal(GirdiKodu.SayiOndalik, K(MilyemKurali.Denetle(916.0001m)));
        Assert.False(MilyemKurali.SapmaVar(916m, "K22"));
        Assert.False(MilyemKurali.SapmaVar(966m, "K22"));
        Assert.True(MilyemKurali.SapmaVar(967m, "K22"));
    }

    [Fact]
    public void K13_Ayar_yalniz_liste()
    {
        Assert.Null(AyarKurali.Denetle("K22"));
        Assert.Null(AyarKurali.Denetle("K995"));
        Assert.Equal(GirdiKodu.AyarBilinmiyor, K(AyarKurali.Denetle("K23")));
    }

    [Fact]
    public void K14_Adet_tam_sayi_en_az_1()
    {
        Assert.Null(AdetKurali.Denetle(1m));
        Assert.Equal(GirdiKodu.SayiEnAz, K(AdetKurali.Denetle(0m)));
        Assert.Equal(GirdiKodu.SayiEnAz, K(AdetKurali.Denetle(-1m)));
        Assert.Equal(GirdiKodu.SayiTam, K(AdetKurali.Denetle(1.5m)));
        Assert.Null(AdetKurali.Denetle(AdetKurali.Tavan));
        Assert.Equal(GirdiKodu.SayiTavan, K(AdetKurali.Denetle(AdetKurali.Tavan + 1)));
    }

    // ── K15 YÜZDE · K16 KUR · K17 GÜN ───────────────────────────────────────
    [Fact]
    public void K15_Yuzde_ve_marj()
    {
        Assert.Null(YuzdeKurali.Denetle(0m));
        Assert.Null(YuzdeKurali.Denetle(100m));
        Assert.Equal(GirdiKodu.YuzdeAralik, K(YuzdeKurali.Denetle(100.01m)));
        Assert.Equal(GirdiKodu.YuzdeAralik, K(YuzdeKurali.Denetle(-0.01m)));
        Assert.Equal(GirdiKodu.SayiOndalik, K(YuzdeKurali.Denetle(12.345m)));
        Assert.Null(YuzdeKurali.Denetle(1.8555m, 4));
        Assert.Null(YuzdeKurali.Marj(500m));
        Assert.Equal(GirdiKodu.SayiEnCok, K(YuzdeKurali.Marj(500.01m)));
        Assert.Equal(GirdiKodu.SayiEksi, K(YuzdeKurali.Marj(-1m)));
    }

    [Fact]
    public void K16_Kur_pozitif_6_ondalik()
    {
        Assert.Equal(GirdiKodu.SayiSifir, K(KurKurali.Denetle(0m)));
        Assert.Equal(GirdiKodu.SayiEksi, K(KurKurali.Denetle(-41m)));
        Assert.Null(KurKurali.Denetle(0.000001m));
        Assert.Equal(GirdiKodu.SayiOndalik, K(KurKurali.Denetle(41.0000001m)));
    }

    [Fact]
    public void K17_Gun_sayisi_tam_ve_aralik()
    {
        Assert.Null(GunSayisiKurali.Denetle(0m, 0, 90));
        Assert.Null(GunSayisiKurali.Denetle(90m, 0, 90));
        Assert.Equal(GirdiKodu.GunAralik, K(GunSayisiKurali.Denetle(91m, 0, 90)));
        Assert.Equal(GirdiKodu.GunAralik, K(GunSayisiKurali.Denetle(1m, 2, 12)));
        Assert.Equal(GirdiKodu.SayiTam, K(GunSayisiKurali.Denetle(1.5m, 0, 90)));
    }

    // ── K18 BARKOD · K19/K20 AD · K21 ADRES · K7 PASAPORT · K30 GEREKÇE ─────
    [Fact]
    public void K18_Barkod_ascii_ve_ean13_uyarisi()
    {
        Assert.Null(BarkodKurali.Denetle("K0123456789"));
        Assert.Equal(GirdiKodu.BarkodBosluk, K(BarkodKurali.Denetle(" AB")));
        Assert.Equal(GirdiKodu.BarkodKarakter, K(BarkodKurali.Denetle("ÇAY")));
        Assert.Null(BarkodKurali.Denetle(new string('A', 50)));
        Assert.Equal(GirdiKodu.BarkodUzunluk, K(BarkodKurali.Denetle(new string('A', 51))));
        Assert.Null(BarkodKurali.Ean13Uyari("4006381333931"));
        Assert.Equal(GirdiKodu.Ean13Kontrol, K(BarkodKurali.Ean13Uyari("4006381333932")));
        Assert.Null(BarkodKurali.Ean13Uyari("400638133393"));   // 12 hane: EAN-13 değil
    }

    [Fact]
    public void K19_K20_Ad_ve_unvan()
    {
        Assert.Equal(GirdiKodu.AdKisa, K(AdKurali.Denetle("A")));
        Assert.Null(AdKurali.Denetle("Al"));
        Assert.Equal(GirdiKodu.AdHarf, K(AdKurali.Denetle("12")));
        Assert.Equal(GirdiKodu.AdHarf, K(AdKurali.Denetle("..")));
        Assert.Null(AdKurali.Denetle(new string('a', 100)));
        Assert.Equal(GirdiKodu.AdUzun, K(AdKurali.Denetle(new string('a', 101))));
        Assert.Null(AdKurali.Unvan(new string('a', 200)));
        Assert.Equal(GirdiKodu.AdUzun, K(AdKurali.Unvan(new string('a', 201))));
        Assert.Equal("Ayşe Nur Demir", AdKurali.Normallestir("  Ayşe   Nur  Demir "));
    }

    [Fact]
    public void K21_Adres_10_500()
    {
        Assert.Equal(GirdiKodu.AdresKisa, K(AdresKurali.Denetle("Kızılay 1")));        // 9
        Assert.Null(AdresKurali.Denetle("Kızılay 12"));                                // 10
        Assert.Equal(GirdiKodu.AdresHarf, K(AdresKurali.Denetle("1234567890")));
        Assert.Null(AdresKurali.Denetle("a" + new string('b', 499)));
        Assert.Equal(GirdiKodu.AdresUzun, K(AdresKurali.Denetle("a" + new string('b', 500))));
    }

    [Fact]
    public void K7_Pasaport()
    {
        Assert.Null(PasaportKurali.Denetle("U12345678"));
        Assert.Null(PasaportKurali.Denetle(" u12345678 "));
        Assert.Null(PasaportKurali.Denetle("AB123"));                                   // 5 alt sınır
        Assert.Equal(GirdiKodu.PasaportBicim, K(PasaportKurali.Denetle("AB12")));        // 4
        Assert.Null(PasaportKurali.Denetle(new string('A', 20)));
        Assert.Equal(GirdiKodu.PasaportBicim, K(PasaportKurali.Denetle(new string('A', 21))));
        Assert.Equal(GirdiKodu.PasaportBicim, K(PasaportKurali.Denetle("U 12345678")));
        Assert.Equal(GirdiKodu.PasaportBicim, K(PasaportKurali.Denetle("U-12345678")));
    }

    [Fact]
    public void K30_Gerekce_20_1000()
    {
        Assert.Equal(GirdiKodu.GerekceKisa, K(GerekceKurali.Denetle(new string('a', 19))));
        Assert.Null(GerekceKurali.Denetle(new string('a', 20)));
        Assert.Null(GerekceKurali.Denetle(new string('a', 1000)));
        Assert.Equal(GirdiKodu.GerekceUzun, K(GerekceKurali.Denetle(new string('a', 1001))));
    }

    // ── K4/K5 köprüsü: TCKN-DOGRULAMA KimlikNoDogrulayici (yeniden yazılmadı) ──
    [Fact]
    public void K5_K4_Kimlik_koprusu_KimlikNoDogrulayici_sonucunu_cevirir()
    {
        Assert.Null(KimlikKurali.Tckn("10000000146"));
        Assert.Equal(GirdiKodu.TcknGecersiz, K(KimlikKurali.Tckn("10000000147")));
        Assert.Equal(GirdiKodu.KimlikHane, K(KimlikKurali.Tckn("1000000014")));
        Assert.Equal(GirdiKodu.KimlikRakam, K(KimlikKurali.Tckn("1000000014a")));
        Assert.Null(KimlikKurali.Vkn("1234567890"));
        Assert.Equal(GirdiKodu.KimlikHane, K(KimlikKurali.Vkn("123456789")));
        Assert.Null(KimlikKurali.TcknVeyaVkn("1234567890"));
        Assert.Equal(GirdiKodu.TcknGecersiz, K(KimlikKurali.TcknVeyaVkn("11111111111")));
        Assert.Equal(KimlikNoSozlugu.Tr.TcknGecersiz, GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.TcknGecersiz), "tr"));
    }

    // ── SayisalAlan EnAz/EnCok'un Core karşılığı ─────────────────────────────
    [Fact]
    public void SayiKurali_Aralik_EnAz_EnCok()
    {
        Assert.Equal(GirdiKodu.SayiEksi, K(SayiKurali.Aralik(-5m, 0m, null)));
        Assert.Null(SayiKurali.Aralik(0m, 0m, null));
        Assert.Equal(GirdiKodu.SayiEnAz, K(SayiKurali.Aralik(0m, 0.01m, null)));
        Assert.Null(SayiKurali.Aralik(100m, null, 100m));
        Assert.Equal(GirdiKodu.SayiEnCok, K(SayiKurali.Aralik(100.01m, null, 100m)));
        Assert.Equal(GirdiKodu.SayiEnAz, K(SayiKurali.Aralik(-11m, -10m, null)));   // eksi alt sınırda "en az"
    }

    // ── Sözlük: her kod iki dilde, yer tutucu kalmaz ─────────────────────────
    [Fact]
    public void Sozluk_her_kod_iki_dilde()
    {
        foreach (var kod in Enum.GetValues<GirdiKodu>())
        {
            var h = new GirdiHatasi(kod, 1m, 2m, new DateOnly(2026, 10, 8));
            foreach (var dil in new[] { "tr", "en" })
            {
                var m = GirdiSozlugu.Mesaj(h, dil);
                Assert.False(string.IsNullOrWhiteSpace(m), $"{kod}/{dil}");
                Assert.DoesNotContain("{", m);
            }
            Assert.NotEqual(GirdiSozlugu.Mesaj(h, "tr"), GirdiSozlugu.Mesaj(h, "en"));
        }
        Assert.Equal("Değer çok büyük (en çok 9.999.999.999.999,99).",
            GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.SayiTavan, TutarKurali.Tavan), "tr"));
        Assert.Equal("Tarih 01.01.1900 tarihinden önce olamaz.",
            GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.TarihErken, Gun: new DateOnly(1900, 1, 1)), "tr"));
    }

    // ── Sunucu öznitelikleri: AYNI kural, AYNI metin ─────────────────────────
    private sealed class OrnekDto
    {
        [Iban] public string? Iban { get; set; }
        [Telefon] public string? Telefon { get; set; }
        [Eposta] public string? Eposta { get; set; }
        [TarihPenceresi(TarihTuru.Dogum)] public DateOnly? Dogum { get; set; }
        [Tutar] public decimal? Tutar { get; set; }
        [Tutar(TutarTuru.Isaretli)] public decimal? Fark { get; set; }
        [Gram] public decimal? Gram { get; set; }
        [Has] public decimal? Has { get; set; }
        [Milyem] public decimal? Milyem { get; set; }
        [Ayar] public string? Ayar { get; set; }
        [Adet] public decimal? Adet { get; set; }
        [Yuzde] public decimal? Yuzde { get; set; }
        [Kur] public decimal? Kur { get; set; }
        [GunSayisi(0, 90)] public int? Valor { get; set; }
        [Barkod] public string? Barkod { get; set; }
        [Ad] public string? Ad { get; set; }
        [Unvan] public string? Unvan { get; set; }
        [Adres] public string? Adres { get; set; }
        [Pasaport] public string? Pasaport { get; set; }
        [Gerekce] public string? Gerekce { get; set; }
    }

    private static List<ValidationResult> Dogrula(OrnekDto d)
    {
        var s = new List<ValidationResult>();
        Validator.TryValidateObject(d, new ValidationContext(d), s, validateAllProperties: true);
        return s;
    }

    [Fact]
    public void Oznitelik_bos_dto_gecer_null_ve_bosluk_zorunluluk_degil()
    {
        Assert.Empty(Dogrula(new OrnekDto()));
        Assert.Empty(Dogrula(new OrnekDto { Iban = "  ", Telefon = "", Eposta = " " }));
    }

    [Fact]
    public void Oznitelik_gecerli_degerler_gecer()
    {
        Assert.Empty(Dogrula(new OrnekDto
        {
            Iban = "TR330006100519786457841326", Telefon = "05324112233", Eposta = "a@b.co",
            Dogum = Kuyumcu.Api.Common.Zaman.YerelBugun().AddYears(-30), Tutar = 1m, Fark = -1m, Gram = 1m, Has = 1m,
            Milyem = 916m, Ayar = "K22", Adet = 2m, Yuzde = 18m, Kur = 41.5m, Valor = 3, Barkod = "K0123456789",
            Ad = "Ayşe", Unvan = "Anadolu A.Ş.", Adres = "Kızılay Mah. 1. Sok. No 5", Pasaport = "U12345678",
            Gerekce = "Müşteri iade talebi ile düzeltildi",
        }));
    }

    [Fact]
    public void Oznitelik_her_kural_400_metni_sozlukle_ayni()
    {
        var s = Dogrula(new OrnekDto
        {
            Iban = "TR340006100519786457841326", Telefon = "05001112233", Eposta = "ali@ornek",
            Dogum = Kuyumcu.Api.Common.Zaman.YerelBugun().AddDays(1), Tutar = 0m, Fark = 0m, Gram = -1m, Has = 0.0000001m,
            Milyem = 1001m, Ayar = "K23", Adet = 1.5m, Yuzde = 101m, Kur = 0m, Valor = 91, Barkod = "ÇAY",
            Ad = "1", Unvan = "12", Adres = "Ulus", Pasaport = "U 1", Gerekce = "kısa",
        });
        string M(string alan) => Assert.Single(s, r => r.MemberNames.Contains(alan)).ErrorMessage!;
        string T(GirdiKodu k, decimal? a = null, decimal? b = null) => GirdiSozlugu.Mesaj(new GirdiHatasi(k, a, b), "tr");
        Assert.Equal(GirdiSozlugu.Tr.IbanKontrol, M("Iban"));
        Assert.Equal(GirdiSozlugu.Tr.TelefonAralik, M("Telefon"));
        Assert.Equal(GirdiSozlugu.Tr.EpostaBicim, M("Eposta"));
        Assert.Equal(GirdiSozlugu.Tr.DogumGelecek, M("Dogum"));
        Assert.Equal(GirdiSozlugu.Tr.SayiSifir, M("Tutar"));
        Assert.Equal(GirdiSozlugu.Tr.SayiSifir, M("Fark"));
        Assert.Equal(GirdiSozlugu.Tr.SayiEksi, M("Gram"));
        Assert.Equal(T(GirdiKodu.SayiOndalik, 6), M("Has"));
        Assert.Equal(T(GirdiKodu.MilyemAralik, 1, 1000), M("Milyem"));
        Assert.Equal(GirdiSozlugu.Tr.AyarBilinmiyor, M("Ayar"));
        Assert.Equal(GirdiSozlugu.Tr.SayiTam, M("Adet"));
        Assert.Equal(T(GirdiKodu.YuzdeAralik, 0, 100), M("Yuzde"));
        Assert.Equal(GirdiSozlugu.Tr.SayiSifir, M("Kur"));
        Assert.Equal(T(GirdiKodu.GunAralik, 0, 90), M("Valor"));
        Assert.Equal(GirdiSozlugu.Tr.BarkodKarakter, M("Barkod"));
        Assert.Equal(T(GirdiKodu.AdKisa, 2), M("Ad"));
        Assert.Equal(GirdiSozlugu.Tr.AdHarf, M("Unvan"));
        Assert.Equal(T(GirdiKodu.AdresKisa, 10), M("Adres"));
        Assert.Equal(T(GirdiKodu.PasaportBicim, 5, 20), M("Pasaport"));
        Assert.Equal(T(GirdiKodu.GerekceKisa, 20, 4), M("Gerekce"));
        Assert.Equal(20, s.Count);
    }

    [Fact]
    public void Oznitelik_TarihAraligi_400()
    {
        var ex = Assert.Throws<Kuyumcu.Api.Common.BadRequestException>(() =>
            TarihAraligi.Denetle(new DateOnly(2026, 10, 8), new DateOnly(2026, 10, 7)));
        Assert.Equal(GirdiSozlugu.Tr.AralikTers, ex.Message);
        TarihAraligi.Denetle(new DateOnly(2026, 10, 7), new DateOnly(2026, 10, 8));
    }
}
