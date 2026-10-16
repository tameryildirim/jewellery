using System.Globalization;
using System.Reflection;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET DEGISIM-GUNSONU — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ; beklenen değerler SABİT LİTERAL. Kapsam: (1) değişim farkının
// nakit dışı ödeme varsayılanı (|fark| > had, tam hadde nakit serbest; had bilinmezse yok),
// (2) gün sonu FARK'ının sınıflaması (Faturasiz / FazlaFatura / Genel / Yok) ve PRO
// kartlarının çıkarma tutarlılığı (Satış toplamı − farka giren fatura = Fark),
// (3) takas fark önizlemesi (TakasGirisi) has 4 hane ve kuruş sınırı, (4) Türkçe giriş.
// Not: Raporlar'ın yardımcıları internal → yansıma ile çağrılır (üretim kodu değişmez).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class DEGISIMGUNSONUSinirTestleri
{
    private static decimal D(string s) => decimal.Parse(s, CultureInfo.InvariantCulture);

    // ── (1) Değişim farkı: nakit sınırı ──────────────────────────────────────
    [Theory]
    [InlineData("0", null)]                     // sıfır fark
    [InlineData("30000", null)]                 // tam had: nakit serbest
    [InlineData("30000.01", "KART")]            // 1 kuruş fazlası
    [InlineData("-30000", null)]
    [InlineData("-30000.01", "HAVALE")]         // eksi: müşteriye ödenir → Havale (işaret doğru)
    [InlineData("999999999.99", "KART")]        // çok büyük
    [InlineData("-999999999.99", "HAVALE")]
    public void DGS_takas_varsayilan_sinir(string fark, string? beklenen)
        => Assert.Equal(beklenen, Islemler.TakasOdemeVarsayilani(D(fark), 30000m));

    [Fact]
    public void DGS_takas_had_null_sifir_eksi_ise_istemci_esik_uydurmaz()
    {
        Assert.Null(Islemler.TakasOdemeVarsayilani(999999999.99m, null));
        Assert.Null(Islemler.TakasOdemeVarsayilani(999999999.99m, 0m));
        Assert.Null(Islemler.TakasOdemeVarsayilani(999999999.99m, -1m));
        Assert.Null(Islemler.TakasOdemeVarsayilani(null, 30000m));
    }

    /// <summary>
    /// Fail-closed yön: sunucu |Yuvarla2(fark)| &gt; had ise nakdi reddeder. İstemci sunucunun reddedeceği
    /// hiçbir farkta Nakit'i varsayılan BIRAKMAMALI (0,005 sınırı AwayFromZero → 30.000,01).
    /// </summary>
    [Theory]
    [InlineData("30000.005")]
    [InlineData("30000.0051")]
    [InlineData("-30000.005")]
    public void DGS_sunucunun_reddettigi_farkta_istemci_NAKIT_birakmaz(string fark)
    {
        var f = D(fark);
        var sunucu = Math.Abs(Math.Round(f, 2, MidpointRounding.AwayFromZero)) > 30000m;
        Assert.True(sunucu);
        Assert.NotNull(Islemler.TakasOdemeVarsayilani(f, 30000m));
    }

    /// <summary>
    /// Kuruş altı pencere (BULGU, DÜŞÜK): 30.000,004 sunucuda 30.000,00'a yuvarlanır → nakit KABUL.
    /// DEGISIM-GUNSONU tur 2'de DÜZELTİLDİ (bu test yorumundaki talimatla bilinçli güncellendi): istemci de kapıya
    /// sunucu gibi kuruşa yuvarlanmış |fark| verir (Islemler.TakasTevsikTutari) → Nakit bırakır.
    /// </summary>
    [Fact]
    public void DGS_kurus_alti_pencere_istemci_sunucudan_SIKI_belgelenir()
    {
        var f = 30000.004m;
        Assert.False(Math.Abs(Math.Round(f, 2, MidpointRounding.AwayFromZero)) > 30000m);   // sunucu: nakit serbest
        Assert.Null(Islemler.TakasOdemeVarsayilani(f, 30000m));                              // istemci: sunucuyla aynı karar
    }

    [Theory]
    [InlineData("0.01", "KART")]
    [InlineData("0", "KART")]
    [InlineData("-0.01", "HAVALE")]
    public void DGS_nakit_disi_yon_isareti(string fark, string beklenen)
        => Assert.Equal(beklenen, Islemler.TakasNakitDisiYon(D(fark)));

    // ── (2) Gün sonu farkı sınıflaması ───────────────────────────────────────
    private static Mutabakat M(decimal satis, decimal fark, int fSayi, decimal fTl, decimal bagli, int bagsizSayi, decimal bagsizTl, decimal satisFat)
        => new("2026-10-03",
            new MutabakatTahsilat(0m, 0m, 0m, 0m, 0m),
            new MutabakatSatis(4, satis, 1, satis - fTl, fSayi, fTl),
            new MutabakatAlis(0, 0m, 0, 0m),
            new MutabakatHurda(0, 0m, 0m, 0, 0m, 0m, 0, 0),
            new MutabakatFatura(1, satisFat, 0m, satisFat),
            fark, fark == 0m, [], [],
            new MutabakatFaturaEslesme(1, bagli, bagsizSayi, bagsizTl, fTl, null, null));

    private static object Cagir(string ad, params object[] a) =>
        typeof(Raporlar).GetMethod(ad, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(null, a)!;

    [Theory]
    [InlineData("0", "0", 0, "0", "0", 0, "0", "Yok")]
    [InlineData("98766.52", "98766.52", 9, "98766.52", "0", 0, "0", "Faturasiz")]
    [InlineData("5000", "-800", 0, "0", "5000", 1, "800", "FazlaFatura")]
    [InlineData("10000", "1500", 0, "0", "7000", 1, "1500", "Genel")]
    [InlineData("10000", "0.01", 1, "0.01", "9999.99", 0, "0", "Faturasiz")]       // 1 kuruş fark
    [InlineData("10000", "-0.01", 0, "0", "10000", 1, "0.01", "FazlaFatura")]       // −1 kuruş
    [InlineData("10000", "500", 1, "500.01", "9500", 0, "0", "Genel")]             // faturasız ≠ fark (1 kuruş) → genel
    [InlineData("999999999.99", "999999999.99", 1, "999999999.99", "0", 0, "0", "Faturasiz")]
    public void DGS_gun_sonu_fark_turu(string satis, string fark, int fSayi, string fTl, string bagli, int bsz, string bszTl, string beklenen)
    {
        var m = M(D(satis), D(fark), fSayi, D(fTl), D(bagli), bsz, D(bszTl), 0m);
        Assert.Equal(beklenen, Cagir("FarkTuru", m).ToString());
    }

    [Theory]
    [InlineData("10000", "1500", "7000", "1500", "9900")]
    [InlineData("5000", "-800", "5000", "800", "5800")]
    [InlineData("98766.52", "98766.52", "0", "0", "0")]
    [InlineData("999999999.99", "0.01", "999999999.98", "0", "999999999.98")]
    public void DGS_PRO_kart_cikarmasi_tutar(string satis, string fark, string bagli, string bszTl, string satisFat)
    {
        var m = M(D(satis), D(fark), 0, 0m, D(bagli), D(bszTl) == 0m ? 0 : 1, D(bszTl), D(satisFat));
        var giren = (decimal)Cagir("FarkaGirenFatura", m);
        Assert.Equal(D(fark), D(satis) - giren);
    }

    [Fact]
    public void DGS_eslesme_bloğu_yoksa_satis_faturasi_toplami()
    {
        var m = M(10000m, 100m, 0, 0m, 0m, 0, 0m, 9900m) with { FaturaEslesme = null };
        Assert.Equal(9900m, (decimal)Cagir("FarkaGirenFatura", m));
    }

    [Fact]
    public void DGS_tahsilat_alt_metni_sifir_kalem_yok_hepsi_sifirsa_null()
    {
        Assert.Null(Cagir2("TahsilatAltMetni", new MutabakatTahsilat(0m, 0m, 0m, 0m, 0m), "tr"));
        var s = (string)Cagir2("TahsilatAltMetni", new MutabakatTahsilat(0.01m, 0m, 0m, 0m, 0.01m), "tr")!;
        Assert.Contains("0,01", s);
        Assert.DoesNotContain(' ', s);   // kalem içi bölünmez
        Assert.DoesNotContain("TRY", s);
    }

    private static object? Cagir2(string ad, params object[] a) =>
        typeof(Raporlar).GetMethod(ad, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(null, a);

    // ── (3) Takas önizlemesi: has 4 hane, kuruş ──────────────────────────────
    [Fact]
    public void DGS_takas_has_4_hane_ve_fark()
    {
        var t = new TakasGirisi { EskiAyar = "K24", EskiGram = 0.0001m, EskiFiyat = 1000m, YeniFiyat = 1m, YeniAdet = 1 };
        Assert.Equal(0.0001m, t.EskiHas);
        Assert.Equal(0.1m, t.EskiDeger);
        Assert.Equal(0.9m, t.Fark);
    }

    [Fact]
    public void DGS_takas_bos_ve_sifir_girdide_fark_null()
    {
        Assert.Null(new TakasGirisi { EskiGram = null, EskiFiyat = 1000m, YeniFiyat = 1m }.Fark);
        Assert.Null(new TakasGirisi { EskiGram = 0m, EskiFiyat = 1000m, YeniFiyat = 1m }.Fark);
        Assert.Null(new TakasGirisi { EskiGram = 1m, EskiFiyat = 1000m, YeniFiyat = 0m }.Fark);
        Assert.Null(new TakasGirisi { EskiGram = -1m, EskiFiyat = 1000m, YeniFiyat = 5m }.Fark);   // eksi gram reddedilir
    }

    // ── (4) Türkçe giriş ─────────────────────────────────────────────────────
    [Theory]
    [InlineData("45.000", "Tamam", "45000")]
    [InlineData("1,5", "Tamam", "1.5")]
    [InlineData("1.500", "Belirsiz", null)]
    [InlineData("", "Bos", null)]
    [InlineData("999.999.999,99", "Tamam", "999999999.99")]
    public void DGS_turkce_giris(string ham, string sonuc, string? deger)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc.ToString());
        Assert.Equal(deger is null ? null : D(deger), c.Deger);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// TUR 2 HASIM DOĞRULAYICI (03.10) — ek sınır testleri; beklenenler SABİT LİTERAL.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class DEGISIMGUNSONUSinirTestleriTur2
{
    private static decimal D(string s) => decimal.Parse(s, CultureInfo.InvariantCulture);

    // Spesifikasyonun açık sınırları: 29.999,99 / 30.000 / 30.000,01 (iki yön)
    [Theory]
    [InlineData("29999.99", null)]
    [InlineData("-29999.99", null)]
    [InlineData("30000", null)]
    [InlineData("30000.01", "KART")]
    [InlineData("-30000.01", "HAVALE")]
    [InlineData("0.01", null)]
    [InlineData("-0.01", null)]
    public void DGS2_spesifikasyon_sinirlari(string fark, string? beklenen)
        => Assert.Equal(beklenen, Islemler.TakasOdemeVarsayilani(D(fark), 30000m));

    // Kapıya giden tutar: kuruş yuvarlaması AwayFromZero (0,005 sınırı), işaretsiz, null → 0, çok büyük
    [Theory]
    [InlineData("0", "0")]
    [InlineData("0.004", "0")]
    [InlineData("0.005", "0.01")]
    [InlineData("-0.005", "0.01")]
    [InlineData("-30000.004", "30000")]
    [InlineData("999999999.99", "999999999.99")]
    [InlineData("-999999999.995", "1000000000.00")]
    public void DGS2_takas_tevsik_tutari_kurus(string fark, string beklenen)
        => Assert.Equal(D(beklenen), Islemler.TakasTevsikTutari(D(fark)));

    [Fact]
    public void DGS2_takas_tevsik_tutari_null_sifir()
        => Assert.Equal(0m, Islemler.TakasTevsikTutari(null));

    // Had 1 kuruş: en küçük pozitif had da sunucu kuralıyla (olcut > had)
    [Fact]
    public void DGS2_had_bir_kurus()
    {
        Assert.Null(Islemler.TakasOdemeVarsayilani(0.01m, 0.01m));
        Assert.Equal("KART", Islemler.TakasOdemeVarsayilani(0.02m, 0.01m));
        Assert.Equal("HAVALE", Islemler.TakasOdemeVarsayilani(-0.02m, 0.01m));
    }

    private static Mutabakat M(decimal satis, decimal fark, int fSayi, decimal fTl, decimal bagli, int bagsizSayi, decimal bagsizTl)
        => new("2026-10-03",
            new MutabakatTahsilat(0m, 0m, 0m, 0m, 0m),
            new MutabakatSatis(4, satis, 1, satis - fTl, fSayi, fTl),
            new MutabakatAlis(0, 0m, 0, 0m),
            new MutabakatHurda(0, 0m, 0m, 0, 0m, 0m, 0, 0),
            new MutabakatFatura(1, bagli + bagsizTl, 0m, bagli + bagsizTl),
            fark, fark == 0m, [], [],
            new MutabakatFaturaEslesme(1, bagli, bagsizSayi, bagsizTl, fTl, null, null));

    private static object? Cagir(string ad, params object[] a) =>
        typeof(Raporlar).GetMethod(ad, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(null, a);

    // Ekranda gösterilen fark: fazla fatura işaretsiz (ad yönü söyler), diğerleri aynen
    [Theory]
    [InlineData("5000", "-800", 0, "0", "5000", 1, "800", "800")]
    [InlineData("10000", "-0.01", 0, "0", "10000", 1, "0.01", "0.01")]
    [InlineData("98766.52", "98766.52", 9, "98766.52", "0", 0, "0", "98766.52")]
    [InlineData("10000", "1500", 0, "0", "7000", 1, "1500", "1500")]
    [InlineData("0", "0", 0, "0", "0", 0, "0", "0")]
    [InlineData("999999999.99", "-999999999.99", 0, "0", "999999999.99", 1, "999999999.99", "999999999.99")]
    public void DGS2_fark_gosterimi(string satis, string fark, int fSayi, string fTl, string bagli, int bsz, string bszTl, string beklenen)
    {
        var m = M(D(satis), D(fark), fSayi, D(fTl), D(bagli), bsz, D(bszTl));
        Assert.Equal(D(beklenen), (decimal)Cagir("FarkGosterimi", m)!);
    }

    // Etiket TR + EN: türün adı (sabit metin), EN'de de dolu
    [Theory]
    [InlineData("tr", "98766.52", 9, "98766.52", 0, "0", "Faturası kesilmemiş satış")]
    [InlineData("tr", "-800", 0, "0", 1, "800", "Satıştan fazla kesilen fatura")]
    [InlineData("tr", "1500", 0, "0", 1, "1500", "Satış ile fatura farkı")]
    [InlineData("en", "98766.52", 9, "98766.52", 0, "0", "Sales without an invoice")]
    [InlineData("en", "-800", 0, "0", 1, "800", "Invoiced more than sold")]
    public void DGS2_fark_etiketi_tr_en(string dil, string fark, int fSayi, string fTl, int bsz, string bszTl, string beklenen)
    {
        var f = D(fark);
        var m = M(10000m, f, fSayi, D(fTl), 10000m - f - D(bszTl), bsz, D(bszTl));
        var d = Kuyumcu.Core.I18n.DegisimGunSonuSozlugu.Al(dil);
        Assert.Equal(beklenen, (string)Cagir("FarkEtiketi", m, d)!);
    }

    // Tahsilat alt metni: eksi kalem yazılır (sıfır değil), EN'de 'TRY' yok, kuruş yuvarlaması AwayFromZero
    [Fact]
    public void DGS2_tahsilat_alt_metni_eksi_ve_kurus_ve_en()
    {
        var tr = (string)Cagir("TahsilatAltMetni", new MutabakatTahsilat(-500m, 0.005m, 0m, 0m, -499.995m), "tr")!;
        Assert.Contains("500", tr);
        Assert.Contains("0,01", tr);
        Assert.DoesNotContain("TRY", tr);
        var en = (string)Cagir("TahsilatAltMetni", new MutabakatTahsilat(1234.5m, 0m, 0m, 0m, 1234.5m), "en")!;
        Assert.Contains("1.234,50", en);   // para tr-TR biçimi her dilde
        Assert.DoesNotContain("TRY", en);
        Assert.DoesNotContain("Havale", en);
    }

    // Bicim.Tl kuruş sınırı: 0,005 → 0,01 ; 0,004 → 0
    [Fact]
    public void DGS2_bicim_tl_kurus_siniri()
    {
        Assert.Equal(Bicim.Tl(0.01m), Bicim.Tl(0.005m));
        Assert.Equal(Bicim.Tl(0m), Bicim.Tl(0.004m));
        Assert.Contains("999.999.999,99", Bicim.Tl(999999999.99m));
    }

    // Türkçe giriş: belirsiz/eksi
    [Theory]
    [InlineData("30.000,01", "Tamam", "30000.01")]
    [InlineData("29.999,99", "Tamam", "29999.99")]
    [InlineData("0,005", "Tamam", "0.005")]
    [InlineData("2.500", "Belirsiz", null)]
    public void DGS2_turkce_giris_sinir(string ham, string sonuc, string? deger)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(sonuc, c.Sonuc.ToString());
        Assert.Equal(deger is null ? null : D(deger), c.Deger);
    }
}
