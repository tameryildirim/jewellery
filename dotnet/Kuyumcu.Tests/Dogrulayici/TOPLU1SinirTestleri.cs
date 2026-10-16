using System.Reflection;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET TOPLU-1 sınır testleri (01.10.2026).
// Paket şu para/tarih yollarına dokundu: KasaBakiyeMesaji (kasa reddi tutarları),
// Vezne birim fiyat Para ↔ metin dönüşümü (ParaMetni + Sayi), Bicim işletme saati.
// Beklenen değerler SABİTTİR (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class TOPLU1SinirTestleri
{
    private static readonly DilServisi Dil = new(null!);

    private static string? Kasa(string b, string i, string pb = "TRY") =>
        KasaBakiyeMesaji.Bicimle($"'Ana Kasa' kasasının bakiyesi yetersiz: {b} {pb} var, {i} {pb} isteniyor. Nakit kasa eksiye düşemez.", Dil);

    // ── KasaBakiyeMesaji ────────────────────────────────────────────────────
    [Fact]
    public void Kasa_sifir_bakiye_0_TL()
    {
        var m = Kasa("0.00", "150.00");
        Assert.Equal("'Ana Kasa' kasasında yeterli para yok: kasada 0\u00a0₺ var, 150\u00a0₺ isteniyor. Nakit kasa eksiye düşemez.", m);
    }

    [Fact]
    public void Kasa_eksi_bakiye_isareti_korunur()
    {
        var m = Kasa("-500.25", "100.00");
        Assert.NotNull(m);
        Assert.Contains("500,25\u00a0₺", m);
        Assert.Matches("[−-]500,25\u00a0₺", m!);   // işaret düşmez
        Assert.DoesNotContain("TRY", m);
    }

    [Fact]
    public void Kasa_kurus_yuvarlama_AwayFromZero_0_005()
    {
        // Sunucu 2 hane yazar; yine de 3 haneli gelirse ekran AwayFromZero (proje kuralı) uygular.
        Assert.Contains("kasada 0,01\u00a0₺ var", Kasa("0.005", "1.00"));
        Assert.Contains("kasada 0\u00a0₺ var", Kasa("0.004", "1.00"));
    }

    [Fact]
    public void Kasa_cok_buyuk_tutar()
    {
        var m = Kasa("999999999.99", "1000000000.00");
        Assert.Contains("999.999.999,99\u00a0₺", m);
        Assert.Contains("1.000.000.000\u00a0₺", m);
    }

    [Fact]
    public void Kasa_bos_null_ve_kalip_disi_null()
    {
        Assert.Null(KasaBakiyeMesaji.Bicimle(null, Dil));
        Assert.Null(KasaBakiyeMesaji.Bicimle("", Dil));
        Assert.Null(KasaBakiyeMesaji.Bicimle("Kasa kapalı", Dil));
        // Türkçe ondalık (sunucu Invariant yazar) kalıba UYMAZ → null (çağıran kendi yoluna döner, uydurma yok).
        Assert.Null(KasaBakiyeMesaji.Bicimle("'Ana Kasa' kasasının bakiyesi yetersiz: 1.500,25 TRY var, 2.000,00 TRY isteniyor.", Dil));
    }

    [Fact]
    public void Kasa_doviz_kasasi_kod_korunur_sayi_trTR()
    {
        var m = Kasa("1250.5", "2000", "USD");
        Assert.Contains("1.250,50 $", m);
        Assert.Contains("2.000,00 $", m);
        Assert.DoesNotContain("1250.5", m);
    }

    // ── Vezne birim fiyat: Para (decimal) → kalem metni (ParaMetni) → Sayi (decimal) gidiş-dönüş ──
    private static readonly Type Vezne = typeof(Kuyumcu.Web.Pages.Vezne);
    private static string ParaMetni(decimal? d) =>
        (string)Vezne.GetMethod("ParaMetni", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [d])!;
    private static decimal? Sayi(string? s) =>
        (decimal?)Vezne.GetMethod("Sayi", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [s]);

    [Theory]
    [InlineData("0", "0")]
    [InlineData("1500.50", "1500,50")]
    [InlineData("125000", "125000")]
    [InlineData("0.005", "0,005")]
    [InlineData("0.0001", "0,0001")]
    [InlineData("999999999.99", "999999999,99")]
    [InlineData("-5", "-5")]
    public void Vezne_fiyat_gidis_donus_deger_korunur(string ham, string beklenenMetin)
    {
        var d = decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        var metin = ParaMetni(d);
        Assert.Equal(beklenenMetin, metin);
        Assert.Equal(d, Sayi(metin));   // binlik ayırıcı YOK → '1.500' belirsizliğine düşmez
    }

    [Fact]
    public void Vezne_fiyat_bos_null_bos_metin()
    {
        Assert.Equal("", ParaMetni(null));
        Assert.Null(Sayi(""));
        Assert.Null(Sayi(null));
    }

    [Fact]
    public void Turkce_giris_kurallari()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Null(FormAlani.SayiCoz("1.500").Deger);
    }

    // ── TARIH-TEK: gün sınırı (UTC 21:00 = TSİ 00:00) ─────────────────────
    [Fact]
    public void Isletme_gun_siniri_tam_21_00_UTC()
    {
        var once = new DateTime(2026, 9, 30, 20, 59, 59, DateTimeKind.Utc);
        var sinir = new DateTime(2026, 9, 30, 21, 0, 0, DateTimeKind.Unspecified);
        Assert.Equal("30.09.2026 23:59:59", Bicim.TarihSaatSaniye(once));
        Assert.Equal("01.10.2026 00:00", Bicim.TarihSaat(sinir));
        Assert.Equal(new DateOnly(2026, 10, 1), Bicim.IsletmeGunu(sinir));
    }

    [Fact]
    public void Isletme_saati_uc_degerlerde_patlamaz()
    {
        // default(DateTime) (boş alan) ve MaxValue ekranı düşürmez; gün 01.01.0001 kalır.
        Assert.Null(Record.Exception(() => Bicim.TarihSaat(DateTime.MinValue)));
        Assert.StartsWith("01.01.0001", Bicim.TarihSaat(DateTime.MinValue));
        Assert.Null(Record.Exception(() => Bicim.TarihSaat(DateTime.MaxValue)));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI tur 2 (01.10.2026 ~12:00) — ek sınırlar. Beklenen değerler SABİT.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class TOPLU1SinirTestleriTur2
{
    private static readonly DilServisi Dil = new(null!);

    private static string? Kasa(string ad, string b, string i, string pb = "TRY") =>
        KasaBakiyeMesaji.Bicimle($"'{ad}' kasasının bakiyesi yetersiz: {b} {pb} var, {i} {pb} isteniyor. Nakit kasa eksiye düşemez.", Dil);

    [Fact]
    public void T2_Kasa_sifir_var_sifir_istenen()
    {
        var m = Kasa("Ana Kasa", "0.00", "0.00");
        Assert.NotNull(m);
        Assert.Contains("kasada 0 ₺ var, 0 ₺ isteniyor", m);
    }

    [Fact]
    public void T2_Kasa_eksi_yarim_kurus_AwayFromZero()
    {
        var m = Kasa("Ana Kasa", "-0.005", "1.00");
        Assert.NotNull(m);
        Assert.Matches("kasada [−-]0,01 ₺ var", m!);
    }

    [Fact]
    public void T2_Kasa_cok_buyuk_istenen_tutar_binlikli()
    {
        var m = Kasa("Ana Kasa", "0.00", "999999999.99");
        Assert.NotNull(m);
        Assert.Contains("999.999.999,99 ₺ isteniyor", m);
        Assert.DoesNotContain("TRY", m);
    }

    [Fact]
    public void T2_Kasa_doviz_yarim_kurus()
    {
        var m = Kasa("Döviz Kasası", "0.005", "10.00", "USD");
        Assert.NotNull(m);
        Assert.Contains("0,01 $", m);
        Assert.DoesNotContain("USD", m);
    }

    /// <summary>Türkçede özel ada ek kesme işaretiyle gelir ("Merkez'deki Kasa") — kalıp yine tutmalı,
    /// yoksa hata bandına ham 'TRY' ve makine sayısı geri döner (paketin kapattığı kusur).</summary>
    [Fact(Skip = "TOPLU-1 doğrulayıcı bulgusu (ORTA, 01.10): KasaBakiyeMesaji kalıbı [^']* — kesme işaretli kasa adında null döner; düzeltilince Skip kaldırılır")]
    public void T2_Kasa_adi_kesme_isaretli()
    {
        var m = Kasa("Merkez'deki Kasa", "100.00", "250.50");
        Assert.NotNull(m);
        Assert.Contains("250,50 ₺", m);
        Assert.DoesNotContain("TRY", m);
    }

    [Fact]
    public void T2_Tarih_tek_kalip_dil_ve_kisa_kaliptan_bagimsiz()
    {
        var an = new DateTime(2026, 10, 1, 7, 3, 0, DateTimeKind.Utc);       // 10:03 TSİ
        Assert.Equal("01.10.2026", Bicim.Tarih(an, "en-US"));
        Assert.Equal("01.10.2026", Bicim.Tarih(an, "tr-TR"));
        Assert.Equal("01.10.2026 10:03", Bicim.IsletmeBicim(an, "g"));
        Assert.Equal("01.10.2026", Bicim.GunBicim(new DateTime(2026, 10, 1), "d", System.Globalization.CultureInfo.GetCultureInfo("en-GB")));
        Assert.Equal("01.10.2026", Bicim.Gun(new DateOnly(2026, 10, 1)));
        // 30.09 23:59 UTC (= 01.10 02:59 TSİ) gece yarısından sonraki işlem bir gün GERİ basılmaz.
        Assert.Equal("01.10.2026", Bicim.Tarih(new DateTime(2026, 9, 30, 23, 59, 0, DateTimeKind.Unspecified), "tr-TR"));
    }

    [Fact]
    public void T2_Turkce_giris_buyuk_tutar_ve_kurus()
    {
        Assert.Equal(999999999.99m, FormAlani.SayiCoz("999.999.999,99").Deger);
        Assert.Equal(1500.5m, FormAlani.SayiCoz("1.500,5").Deger);
        Assert.Equal(0.0001m, FormAlani.SayiCoz("0,0001").Deger);
        Assert.Null(FormAlani.SayiCoz("").Deger);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI — TOPLU-1 TUR 3 (01.10.2026 14:00). Tur 3 Secici seçili değeri 2 satıra
// kıstı; "Fişten Fatura Kes" seçeneğinin metni "tarih · tür · ürün · Bicim.Tl(toplam)" olduğundan
// tutar seçicide gösterilen değerin SONUNDADIR. Tutar metninin kendisi (Bicim.Tl / Bicim.Has)
// sınırlarda doğru olmalı. Beklenenler SABİT (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class TOPLU1T3SinirTestleri
{
    [Theory]
    [InlineData("0", "0 ₺")]
    [InlineData("3200", "3.200 ₺")]
    [InlineData("1500.50", "1.500,50 ₺")]
    [InlineData("0.005", "0,01 ₺")]          // AwayFromZero: 0,005 → 0,01
    [InlineData("0.004", "0 ₺")]             // 0,004 → 0
    [InlineData("-0.005", "−0,01 ₺")]   // eksi yarım kuruş işaretini korur
    [InlineData("-0.004", "0 ₺")]            // eksi sıfıra yuvarlanınca "−0" basılmaz
    [InlineData("-2500", "−2.500 ₺")]
    [InlineData("999999999.99", "999.999.999,99 ₺")]
    [InlineData("999999999.995", "1.000.000.000 ₺")]
    public void T3_Fisten_secenek_tutar_metni_sinirlar(string ham, string beklenen)
    {
        var n = decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(beklenen, Bicim.Tl(n));
    }

    [Theory]
    [InlineData("16.65", "16,6500 has")]
    [InlineData("0.0001", "0,0001 has")]
    [InlineData("0", "0,0000 has")]
    [InlineData("0.00005", "0,0001 has")]   // F4 biçimi: yarım on-binde bir yukarı (AwayFromZero)
    public void T3_Has_dort_hane(string ham, string beklenen)
    {
        var n = decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(beklenen, Bicim.Has(n));
    }

    [Fact]
    public void T3_Turkce_giris_belirsiz_ve_bos()
    {
        Assert.Equal(45000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.False(FormAlani.SayiCoz("1.500").Tamam && FormAlani.SayiCoz("1.500").Deger == 1.5m);
        Assert.Null(FormAlani.SayiCoz(null).Deger);
        Assert.Null(FormAlani.SayiCoz("   ").Deger);
        Assert.Equal(999999999.99m, FormAlani.SayiCoz("999.999.999,99").Deger);
    }
}
