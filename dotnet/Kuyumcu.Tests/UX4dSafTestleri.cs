using Kuyumcu.Core.Domain;
using Kuyumcu.Core.I18n;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-4d — SAF (veritabanısız) TESTLER
//   madde 2/3  MASAK mesajları alan adı + kabul edilen değerler taşır
//   madde 6    yön sözlüğü TEK KAYNAK, sessiz eşleme yok
//   madde 8    doğrulama mesajları Türkçe, alan adı Türkçe, aralık anlamlı
//   madde 17   miktar+birim tek biçim
//   madde 19   telefon biçimi tek kural
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UX4dSafTestleri
{
    // ── madde 8 ──────────────────────────────────────────────────────────────

    [Fact]
    public void M8_EmailAddress_ingilizce_mesaji_TURKCE_ve_alan_adi_Turkce_olur()
    {
        var sonuc = DogrulamaSozlugu.Cevir("Email", "The Email field is not a valid e-mail address.");
        Assert.Equal("E-posta geçerli bir e-posta adresi olmalıdır (örnek: ad@ornek.com)", sonuc);
    }

    [Fact]
    public void M8_Range_ust_siniri_double_MaxValue_ise_anlamli_mesaj_verir()
    {
        var sonuc = DogrulamaSozlugu.Cevir(
            "KrediLimitiTl", "The field KrediLimitiTl must be between 0 and 1,79769313486232E+308.");
        Assert.Equal("Kredi limiti (TL) 0 veya daha büyük olmalıdır", sonuc);
        Assert.DoesNotContain("E+", sonuc);
    }

    [Fact]
    public void M8_Range_gercek_ust_sinir_varsa_araligi_yazar()
    {
        var sonuc = DogrulamaSozlugu.Cevir("VadeGun", "The field VadeGun must be between 0 and 3650.");
        Assert.Equal("Vade (gün) 0 ile 3650 arasında olmalıdır", sonuc);
    }

    [Fact]
    public void M8_Required_mesaji_Turkcelesir()
        => Assert.Equal("Ad zorunludur", DogrulamaSozlugu.Cevir("Ad", "The Ad field is required."));

    [Fact]
    public void M8_MaxLength_mesaji_Turkcelesir()
        => Assert.Equal("Açıklama en fazla 500 karakter olabilir",
            DogrulamaSozlugu.Cevir("Aciklama",
                "The field Aciklama must be a string or array type with a maximum length of '500'."));

    [Fact]
    public void M8_Elle_yazilmis_TURKCE_mesaj_DEGISTIRILMEZ()
        => Assert.Equal("Kredi limiti 0 veya daha büyük olmalıdır",
            DogrulamaSozlugu.Cevir("KrediLimitiTl", "Kredi limiti 0 veya daha büyük olmalıdır"));

    [Fact]
    public void M8_Sozlukte_olmayan_alan_PascalCase_ten_Turkce_etikete_cozulur()
        => Assert.Equal("Sertifika kurumu", DogrulamaSozlugu.AlanAdi("SertifikaKurumu"));

    [Fact]
    public void M8_Dizi_alani_kok_alan_adina_indirgenir()
        => Assert.Equal("Miktar", DogrulamaSozlugu.AlanAdi("kalemler[0].Miktar"));

    [Fact]
    public void M8_Anahtar_camelCase_uretir()
        => Assert.Equal("krediLimitiTl", DogrulamaSozlugu.Anahtar("KrediLimitiTl"));

    // ── madde 6 ──────────────────────────────────────────────────────────────

    [Fact]
    public void M6_Kasa_yonu_yalniz_GIRIS_CIKIS_kabul_eder_GELEN_reddedilir()
    {
        Assert.True(HareketYonu.KasaGecerli("GIRIS"));
        Assert.True(HareketYonu.KasaGecerli("CIKIS"));
        Assert.False(HareketYonu.KasaGecerli("GELEN"));   // sessiz eşleme YOK
        Assert.False(HareketYonu.KasaGecerli("GIDEN"));
    }

    [Fact]
    public void M6_Banka_yonu_yalniz_GELEN_GIDEN_kabul_eder_GIRIS_reddedilir()
    {
        Assert.True(HareketYonu.BankaGecerli("GELEN"));
        Assert.False(HareketYonu.BankaGecerli("GIRIS"));
    }

    [Fact]
    public void M6_Yon_mesajlari_KABUL_EDILEN_DEGERLERI_ve_karsi_sozlugu_yazar()
    {
        Assert.Contains("GIRIS", HareketYonu.KasaMesaj);
        Assert.Contains("CIKIS", HareketYonu.KasaMesaj);
        Assert.Contains("GELEN/GIDEN", HareketYonu.KasaMesaj);
        Assert.Contains("GELEN", HareketYonu.BankaMesaj);
        Assert.Contains("GIRIS/CIKIS", HareketYonu.BankaMesaj);
    }

    [Fact]
    public void M6_Desenler_sozlukle_BIREBIR_ayni()
    {
        Assert.Equal(HareketYonu.KasaDesen, string.Join("|", HareketYonu.KasaHepsi));
        Assert.Equal(HareketYonu.BankaDesen, string.Join("|", HareketYonu.BankaHepsi));
    }

    // ── madde 3 ──────────────────────────────────────────────────────────────

    [Fact]
    public void M3_Kimlik_belgesi_mesaji_KABUL_EDILEN_DEGERLERI_sayar()
    {
        foreach (var t in KimlikBelgesi.Hepsi) Assert.Contains(t, KimlikBelgesi.Mesaj);
        Assert.Contains("kabul edilen değerler", KimlikBelgesi.Mesaj);
    }

    [Fact]
    public void M3_Desen_liste_ile_BIREBIR_ayni_tek_kaynak()
        => Assert.Equal(KimlikBelgesi.Desen, string.Join("|", KimlikBelgesi.Hepsi));

    [Fact]
    public void M3_Api_tarafindaki_KimlikBelgeTuru_ayni_listeyi_dondurur()
        => Assert.Equal(KimlikBelgesi.Hepsi, Kuyumcu.Api.Data.KimlikBelgeTuru.Hepsi);

    // ── madde 19 ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("05321234567", true)]
    [InlineData("+90 532 123 45 67", true)]
    [InlineData("(0212) 555-1234", true)]
    [InlineData("abc", false)]
    [InlineData("123", false)]
    [InlineData("05321234567890123", false)]
    public void M19_Telefon_bicimi_tek_kuraldan_dogrulanir(string girdi, bool beklenen)
        => Assert.Equal(beklenen, Iletisim.TelefonGecerli(girdi));

    [Fact]
    public void M19_Bos_telefon_gecerlidir_verilmedi_demektir()
        => Assert.True(Iletisim.TelefonGecerli(null));

    // ── madde 5 ──────────────────────────────────────────────────────────────

    [Fact]
    public void M5_Nakit_tevsik_haddini_asinca_BANKA_yolu_onerilir()
        => Assert.True(Kuyumcu.Web.Servisler.TevsikOneri.Gerekli("NAKIT", "HURDA_ALIS", 30_000.01m, 30_000m));

    // SATIS-3 t12: 459 VUK GT §4.1 "…TL'yi AŞAN tutardaki…" ve sunucu (TevsikKurali "olcut > Had") — tam hadde öneri/kilit YOK.
    [Fact]
    public void M5_TAM_hadde_nakit_SERBEST_asan_degil()
        => Assert.False(Kuyumcu.Web.Servisler.TevsikOneri.Gerekli("NAKIT", "HURDA_ALIS", 30_000m, 30_000m));

    [Fact]
    public void M5_Had_altinda_nakit_uyarisi_CIKMAZ_mevcut_akis_bozulmaz()
        => Assert.False(Kuyumcu.Web.Servisler.TevsikOneri.Gerekli("NAKIT", "HURDA_ALIS", 29_999m, 30_000m));

    [Fact]
    public void M5_Havale_seciliyken_uyari_CIKMAZ()
        => Assert.False(Kuyumcu.Web.Servisler.TevsikOneri.Gerekli("HAVALE", "HURDA_ALIS", 90_000m, 30_000m));

    [Fact]
    public void M5_Takas_gibi_kapsam_disi_islemde_uyari_CIKMAZ()
        => Assert.False(Kuyumcu.Web.Servisler.TevsikOneri.Gerekli("NAKIT", "TAKAS", 90_000m, 30_000m));

    // ── madde 17 ─────────────────────────────────────────────────────────────

    [Fact]
    public void M17_Miktar_GRAM_birimi_icin_g_yazar()
        => Assert.Equal("1,000 g", Kuyumcu.Web.Servisler.Bicim.Miktar(1m, "GRAM"));

    [Fact]
    public void M17_Miktar_ADET_birimi_icin_adet_yazar_ve_g_YAZMAZ()
    {
        var s = Kuyumcu.Web.Servisler.Bicim.Miktar(1m, "ADET");
        Assert.Equal("1 adet", s);
        Assert.DoesNotContain(" g", s);
        Assert.DoesNotContain("ADET", s);   // ham kod ekrana basılmaz (UX-S31)
    }
}
