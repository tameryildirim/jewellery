using Bunit;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-S2 — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİTTİR (üretim kodundan
// hesaplanmaz). T-S2 profesyonel yüzü şu sayıları HESAPLAR/GÖSTERİR: toplam has
// (FisHesap.ToplamHas), satır/toplam işçilik (FisHesap.SatirIscilik /
// ToplamIscilik, AwayFromZero 2 hane), teklif işçiliğinin kaleme bağlanması
// (FisProKalemKarti.TeklifIscilik), fiyat listesi çözümü (Islemler.FiyatListesiCoz),
// müşteri kartındaki has/TL bakiyesi (FisProYanSutun).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class TS2SinirTestleri : BunitContext
{
    public TS2SinirTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private static UrunVeri U(string id, decimal has) => new(id, "x", "K22", null, has, null, null);
    private static FisKalemi K(string id, decimal adet, decimal? isc = null, bool hediye = false) =>
        new() { UrunId = id, UrunAdi = "x", Adet = adet, IscilikTl = isc, Hediye = hediye };
    private static Teklif T(bool var, decimal? isc, decimal? fiyat) =>
        new(var, null, fiyat, null, null, isc, null, fiyat, null, false, null);

    // ── Toplam has (hesap) ──────────────────────────────────────────────────
    [Fact] public void Has_bos_fis_sifir() => Assert.Equal(0m, FisHesap.ToplamHas([], [U("a", 3.94m)]));
    [Fact] public void Has_4_hane_kaybolmaz_0_0001_x3() => Assert.Equal(0.0003m, FisHesap.ToplamHas([K("a", 3m)], [U("a", 0.0001m)]));
    [Fact] public void Has_bilinmeyen_urun_0_sayilir_uydurulmaz() => Assert.Equal(3.94m, FisHesap.ToplamHas([K("a", 1m), K("yok", 5m)], [U("a", 3.94m)]));
    [Fact] public void Has_tuval_ornegi_3_94_arti_2x1_785() => Assert.Equal(7.51m, FisHesap.ToplamHas([K("a", 1m), K("b", 2m)], [U("a", 3.94m), U("b", 1.785m)]));

    // ── Satır işçiliği: kuruş yuvarlaması (AwayFromZero) ───────────────────
    [Fact] public void Iscilik_0_005_yukari_0_01() => Assert.Equal(0.01m, FisHesap.SatirIscilik(K("a", 1m, 0.005m)));
    [Fact] public void Iscilik_0_0049_asagi_0() => Assert.Equal(0.00m, FisHesap.SatirIscilik(K("a", 1m, 0.0049m)));
    [Fact] public void Iscilik_333_335_x1_333_34() => Assert.Equal(333.34m, FisHesap.SatirIscilik(K("a", 1m, 333.335m)));
    [Fact] public void Iscilik_hediye_0() => Assert.Equal(0m, FisHesap.SatirIscilik(K("a", 2m, 500m, hediye: true)));
    [Fact] public void Iscilik_bilinmiyor_null() => Assert.Null(FisHesap.SatirIscilik(K("a", 2m)));
    [Fact] public void Iscilik_eksi_isaret_korunur() => Assert.Equal(-1000m, FisHesap.SatirIscilik(K("a", 2m, -500m)));
    [Fact] public void Iscilik_cok_buyuk_999999999_99() => Assert.Equal(999_999_999.99m, FisHesap.SatirIscilik(K("a", 1m, 999_999_999.99m)));

    [Fact]
    public void Iscilik_toplam_bos_fis_0_eksik_degil()
        => Assert.Equal((0m, false), FisHesap.ToplamIscilik([]));

    [Fact]
    public void Iscilik_toplam_karisik_bilineni_toplar_eksik_isaretli()
        => Assert.Equal((2400m, true), FisHesap.ToplamIscilik([K("a", 1m, 1600m), K("b", 2m, 400m), K("c", 1m)]));

    // ── Teklif işçiliğinin kaleme bağlanması ────────────────────────────────
    [Fact] public void Teklif_fiyatiyla_eklenen_iscilik_tasir() => Assert.Equal(500m, FisProKalemKarti.TeklifIscilik(T(true, 500m, 10000m), 10000m));
    [Fact] public void Teklif_elle_fiyat_iscilik_null() => Assert.Null(FisProKalemKarti.TeklifIscilik(T(true, 500m, 10000m), 9999.99m));
    [Fact] public void Teklif_yok_null() => Assert.Null(FisProKalemKarti.TeklifIscilik(T(false, 500m, 10000m), 10000m));
    [Fact] public void Teklif_null_fiyat_null() => Assert.Null(FisProKalemKarti.TeklifIscilik(null, null));

    // ── Fiyat listesi çözümü ────────────────────────────────────────────────
    [Fact]
    public void Fiyat_listesi_liste_okunmadiysa_ad_yok()
        => Assert.Equal(((string?)null, false), Kuyumcu.Web.Pages.Islemler.FiyatListesiCoz(null, "fl2"));

    [Fact]
    public void Fiyat_listesi_carinin_pasif_ya_da_olmayan_listesi_varsayilana_duser()
        => Assert.Equal(("Vitrin", false), Kuyumcu.Web.Pages.Islemler.FiyatListesiCoz([("fl1", "Vitrin", true)], "fl9"));

    [Fact]
    public void Fiyat_listesi_varsayilan_yoksa_ad_uydurulmaz()
        => Assert.Equal(((string?)null, false), Kuyumcu.Web.Pages.Islemler.FiyatListesiCoz([("fl2", "Toptan", false)], null));

    // ── Ekran: alt toplam şeridi ────────────────────────────────────────────
    [Fact]
    public void Serit_cok_buyuk_genel_toplam_tr_TR()
    {
        var e = Render<FisProKalemKarti>(p => p.Add(x => x.Kalemler, [K("a", 1m, 1600m)]).Add(x => x.Urunler, [U("a", 3.94m)]).Add(x => x.Toplam, 999_999_999.99m));
        Assert.Equal("999.999.999,99 ₺", e.Find("#islem-pro-genel-toplam").TextContent.Trim());
        Assert.Equal("1.600 ₺", e.Find("#islem-pro-toplam-iscilik").TextContent.Trim());
        Assert.DoesNotContain("TRY", e.Markup);
    }

    [Fact]
    public void Serit_bos_fis_0_ve_iscilik_0()
    {
        var e = Render<FisProKalemKarti>(p => p.Add(x => x.Kalemler, []).Add(x => x.Urunler, []).Add(x => x.Toplam, 0m));
        Assert.Equal("0 ₺", e.Find("#islem-pro-genel-toplam").TextContent.Trim());
        Assert.Equal("0,00 g", e.Find("#islem-pro-toplam-has").TextContent.Trim());
    }

    [Fact]
    public void Serit_iscilik_bilinmiyorsa_tire_rakam_uydurmaz()
    {
        var e = Render<FisProKalemKarti>(p => p.Add(x => x.Kalemler, [K("a", 1m)]).Add(x => x.Urunler, [U("a", 3.94m)]).Add(x => x.Toplam, 100m));
        Assert.Equal("—", e.Find("#islem-pro-toplam-iscilik").TextContent.Trim());
    }

    /// <summary>Has 4 hane (P-57 kuralı: has 4 basamak) — 0,0003 g has ekranda "0,00 g" olarak KAYBOLMAMALI.</summary>
    [Fact]
    public void Serit_has_4_hane_ekranda_kaybolmaz()
    {
        var e = Render<FisProKalemKarti>(p => p.Add(x => x.Kalemler, [K("a", 3m)]).Add(x => x.Urunler, [U("a", 0.0001m)]).Add(x => x.Toplam, 1m));
        Assert.NotEqual("0,00 g", e.Find("#islem-pro-toplam-has").TextContent.Trim());
    }

    // ── Ekran: müşteri kartı ────────────────────────────────────────────────
    [Fact]
    public void Kart_has_borcu_tuval_5_66_ve_tl_38900()
    {
        var e = Render<FisProYanSutun>(p => p.Add(x => x.Cari, new CariVeri("c1", "Hasan", TlBakiye: 38_900m, HasGramBakiye: 5.66m)));
        Assert.Equal("Müşteri bize borçlu: 5,66 g", e.Find("#islem-pro-musteri-has").TextContent.Trim());
        Assert.Equal("Müşteri bize borçlu: 38.900 ₺", e.Find("#islem-pro-musteri-tl").TextContent.Trim());
    }

    [Fact]
    public void Kart_eksi_bakiye_alacakli_isaretsiz_tutar()
    {
        var e = Render<FisProYanSutun>(p => p.Add(x => x.Cari, new CariVeri("c1", "Hasan", TlBakiye: -0.005m, HasGramBakiye: -1.5m)));
        Assert.Equal("Biz müşteriye borçluyuz: 1,50 g", e.Find("#islem-pro-musteri-has").TextContent.Trim());
        Assert.Equal("Biz müşteriye borçluyuz: 0,01 ₺", e.Find("#islem-pro-musteri-tl").TextContent.Trim());
    }

    /// <summary>Çelişki avı: "borçlu" deyip tutarı "0,00 g" yazmak — 0,003 g has borcu 4 hanede görünmeli.</summary>
    [Fact]
    public void Kart_kucuk_has_borcu_0_00_borclu_yazilmaz()
    {
        var e = Render<FisProYanSutun>(p => p.Add(x => x.Cari, new CariVeri("c1", "Hasan", HasGramBakiye: 0.003m)));
        Assert.NotEqual("Müşteri bize borçlu: 0,00 g", e.Find("#islem-pro-musteri-has").TextContent.Trim());
    }

    // ── Türkçe sayı girişi (FormAlani.SayiCoz — satır içi fiyat/adet/iskonto) ─
    [Theory]
    [InlineData("45.000", "45000")]
    [InlineData("1,5", "1.5")]
    [InlineData("999.999.999,99", "999999999.99")]
    [InlineData("0,0001", "0.0001")]
    public void SayiCoz_Turkce(string ham, string beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.True(c.Tamam);
        Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), c.Deger);
    }

    [Fact] public void SayiCoz_belirsiz_1_500_reddedilir() => Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
    [Fact] public void SayiCoz_bos_null() => Assert.Equal((FormAlani.SayiSonucu.Bos, (decimal?)null), (FormAlani.SayiCoz("  ").Sonuc, FormAlani.SayiCoz("  ").Deger));
}

// ═════════════════════════════════════════════════════════════════════════════
// PAKET T-S2 — HASIM DOĞRULAYICI TUR 2 (düzeltme turu, 25.09) SINIR TESTLERİ
//
// Bağımsız yazıldı; beklenen değerler SABİT. Kapsam: PRO geniş tablonun satır içi
// düzeltmeleri (adet/fiyat — FormAlani.SayiCoz tr-TR) ile işçilik/has/gram
// hücrelerinin birlikte doğru ölçeklenmesi, geçersiz girişin kalemi BOZMAMASI,
// kuruş (AwayFromZero, 0,005) ve 4 hane has/gram gösterimi, çok büyük tutar,
// yasal kapılarda sunucu yanıtı yokken istemcinin EŞİK SAYISI UYDURMAMASI.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class TS2SinirTestleriTur2 : BunitContext
{
    public TS2SinirTestleriTur2()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        GocC2Ortak.IzgaraServisleri(Services);   // GOC-C-2: tuval tabloları ortak ızgarada (ApiIstemci ister)
    }

    private static UrunVeri Urun(string id = "u1", decimal has = 3.94m, decimal gram = 4.30m) =>
        new(id, "Burma", "K22", "22 Ayar", has, null, 10000m, Milyem: 916m, GramAgirlik: gram);

    private (IRenderedComponent<FisKalemTablosu> E, FisKalemi K, FormDurumu F) Tablo(decimal adet = 1m, decimal? isc = 500m, decimal has = 3.94m, decimal gram = 4.30m)
    {
        var k = new FisKalemi { UrunId = "u1", UrunAdi = "Burma", Adet = adet, BirimFiyat = 10000m, OncekiFiyat = 10000m, IscilikTl = isc };
        var f = new FormDurumu();
        var e = Render<FisKalemTablosu>(p => p
            .Add(x => x.Kalemler, [k]).Add(x => x.Urunler, [Urun(has: has, gram: gram)])
            .Add(x => x.Form, f).Add(x => x.Satis, true).Add(x => x.Basit, false));
        return (e, k, f);
    }

    private static void Duzelt(IRenderedComponent<FisKalemTablosu> e, string alan, string metin)
    {
        if (alan != "adet" && e.FindAll($"#si-kalem-{alan}-u1-ac").Count == 0) e.Find("#islem-kalem-duzenle-0").Click();
        e.Find($"#si-kalem-{alan}-u1-ac").Click();
        e.Find($"#si-kalem-{alan}-u1").Input(metin);
        e.Find($"#si-kalem-{alan}-u1-kaydet").Click();
    }

    private static string Hucre(IRenderedComponent<FisKalemTablosu> e, int i) =>
        N(e.FindAll("#islem-kalem-pro tbody tr.fis-pro-satir td")[i].TextContent.Trim());
    /// <summary>Bicim.Tl ₺ öncesi bölünmez boşluk yazar; karşılaştırma için düz boşluğa indirilir.</summary>
    private static string N(string s) => s.Replace("\u00A0", " ").Replace("\u202F", " ").Trim();

    // ── Satır içi ADET (tr-TR) → işçilik/has/gram BİRLİKTE ölçeklenir ──────
    [Fact]
    public void Adet_1_virgul_5_iscilik_750_has_5_91_gram_6_45()
    {
        var (e, k, f) = Tablo();
        Duzelt(e, "adet", "1,5");
        e.WaitForAssertion(() => Assert.Equal(1.5m, k.Adet));
        Assert.True(f.Gecerli);
        Assert.Equal("750 ₺", N(e.Find("#islem-kalem-iscilik-0").TextContent));
        Assert.Equal("6,45", Hucre(e, 4));   // 4,30 × 1,5
        Assert.Equal("5,91", Hucre(e, 5));   // 3,94 × 1,5
        Assert.Equal("15.000 ₺", Hucre(e, 8));
    }

    [Fact]
    public void Adet_belirsiz_1_500_reddedilir_kalem_ve_iscilik_DEGISMEZ_mesaj_var()
    {
        var (e, k, f) = Tablo();
        Duzelt(e, "adet", "1.500");
        e.WaitForAssertion(() => Assert.True(f.Var("si-kalem-adet-u1")));
        Assert.Equal(1m, k.Adet);
        Assert.False(string.IsNullOrWhiteSpace(f.Mesaj("si-kalem-adet-u1")));
        Assert.Equal("500 ₺", N(e.Find("#islem-kalem-iscilik-0").TextContent));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-2")]
    [InlineData("")]
    public void Adet_sifir_eksi_bos_reddedilir(string metin)
    {
        var (e, k, f) = Tablo();
        Duzelt(e, "adet", metin);
        // boş giriş SatirIciDuzenle katında da reddedilebilir: her iki yolda alan altı mesaj görünür olmalı
        e.WaitForAssertion(() => Assert.True(f.Var("si-kalem-adet-u1") || e.FindAll("#si-kalem-adet-u1-hata").Count == 1));
        Assert.Equal(1m, k.Adet);
    }

    // ── Satır içi FİYAT: geçersiz giriş teklifin işçiliğini SİLMEZ; geçerli elle fiyat siler ──
    [Fact]
    public void Fiyat_eksi_reddedilir_iscilik_KORUNUR()
    {
        var (e, k, f) = Tablo();
        Duzelt(e, "fiyat", "-5");
        e.WaitForAssertion(() => Assert.True(f.Var("si-kalem-fiyat-u1")));
        Assert.Equal(10000m, k.BirimFiyat);
        Assert.Equal(500m, k.IscilikTl);
        Assert.Equal("500 ₺", N(e.Find("#islem-kalem-iscilik-0").TextContent));
    }

    [Fact]
    public void Fiyat_belirsiz_1_500_reddedilir_iscilik_KORUNUR()
    {
        var (e, k, f) = Tablo();
        Duzelt(e, "fiyat", "1.500");
        e.WaitForAssertion(() => Assert.True(f.Var("si-kalem-fiyat-u1")));
        Assert.Equal(10000m, k.BirimFiyat);
        Assert.Equal(500m, k.IscilikTl);
    }

    [Fact]
    public void Fiyat_45_000_turkce_45000_TL_iscilik_bilinmez_tire()
    {
        var (e, k, _) = Tablo();
        Duzelt(e, "fiyat", "45.000");
        e.WaitForAssertion(() => Assert.Equal(45000m, k.BirimFiyat));
        Assert.Null(k.IscilikTl);
        Assert.Equal("—", N(e.Find("#islem-kalem-iscilik-0").TextContent));
        Assert.Equal("45.000 ₺", Hucre(e, 8));
    }

    // ── SABİT FİYATLI ürün: sunucu teklifi fiyatı AYRIŞTIRMAZ (iscilikTl=0, hasDegeri=0, sabitFiyat=true).
    //    Canlıda ölçüldü (25.09, kiracı PAKETT-S2-TEST-DOG): ürün kartı işçiliği 1.600 ₺ olan Burma bilezik
    //    satirda "0 ₺", şeritte "İşçilik 0 ₺" yazıyor. İşçilik BİLİNMİYOR demektir → null ("—"), 0 DEĞİL.
    [Fact]
    public void Sabit_fiyatli_teklifte_iscilik_0_UYDURULMAZ_bilinmiyor_null()
    {
        var sabit = new Teklif(true, null, 28750m, 0m, 0m, 0m, null, 28750m, null, true, null);
        Assert.Null(FisProKalemKarti.TeklifIscilik(sabit, 28750m));
    }

    // ── Kuruş yuvarlaması (AwayFromZero) ────────────────────────────────────
    [Fact] public void Iscilik_0_335_x3_1_005_yukari_1_01() =>
        Assert.Equal(1.01m, FisHesap.SatirIscilik(new FisKalemi { UrunId = "a", UrunAdi = "x", Adet = 3m, IscilikTl = 0.335m }));
    [Fact] public void Iscilik_gram_adet_0_0001_x100_0_01() =>
        Assert.Equal(0.01m, FisHesap.SatirIscilik(new FisKalemi { UrunId = "a", UrunAdi = "x", Adet = 0.0001m, IscilikTl = 100m }));
    [Fact] public void Iscilik_toplam_satir_satir_yuvarlanir_0_005_x2_0_02() =>
        Assert.Equal((0.02m, false), FisHesap.ToplamIscilik([
            new FisKalemi { UrunId = "a", UrunAdi = "x", Adet = 1m, IscilikTl = 0.005m },
            new FisKalemi { UrunId = "b", UrunAdi = "x", Adet = 1m, IscilikTl = 0.005m }]));

    // ── Has / gram 4 hane, çok büyük, sıfır ─────────────────────────────────
    [Fact] public void Gram_0_0005_yukari_0_001() => Assert.Equal("0,001", FisHesap.GramSayi(0.0005m));
    [Fact] public void Has_sifir_0_00() => Assert.Equal("0,00", FisHesap.HasSayi(0m));
    [Fact] public void Has_cok_buyuk_999999_9999() => Assert.Equal("999.999,9999", FisHesap.HasSayi(999_999.9999m));
    [Fact] public void Has_eksi_isaret_korunur() => Assert.Equal("-1,2346", FisHesap.HasSayi(-1.23456m));

    [Fact]
    public void Tablo_has_0_0001_x_adet_3_hucrede_0_0003()
    {
        var (e, _, _) = Tablo(adet: 3m, has: 0.0001m, gram: 0.0001m);
        Assert.Equal("0,0003", Hucre(e, 5));
        Assert.Equal("0,00", Hucre(e, 4));   // gram 3 hane: 0,0003 → 0,000 → "0,00"
    }

    [Fact]
    public void Serit_toplam_has_cok_buyuk_ve_iscilik_cok_buyuk()
    {
        var k = new FisKalemi { UrunId = "u1", UrunAdi = "x", Adet = 1m, IscilikTl = 999_999_999.99m };
        var e = Render<FisProKalemKarti>(p => p.Add(x => x.Kalemler, [k]).Add(x => x.Urunler, [Urun(has: 999_999.9999m)]).Add(x => x.Toplam, 999_999_999.99m));
        Assert.Equal("999.999,9999 g", N(e.Find("#islem-pro-toplam-has").TextContent));
        Assert.Equal("999.999.999,99 ₺", N(e.Find("#islem-pro-toplam-iscilik").TextContent));
    }

    // ── Müşteri kartı: çok büyük / tam sıfır ────────────────────────────────
    [Fact]
    public void Kart_cok_buyuk_tl_borcu_ve_sifir_has_hesap_kapali()
    {
        var P = SatisProSozlugu.Tr;
        var e = Render<FisProYanSutun>(p => p.Add(x => x.Cari, new CariVeri("c1", "Hasan", TlBakiye: 999_999_999.99m, HasGramBakiye: 0m)));
        Assert.Equal("Müşteri bize borçlu: 999.999.999,99 ₺", N(e.Find("#islem-pro-musteri-tl").TextContent));
        Assert.Equal(P.HesapSifir, N(e.Find("#islem-pro-musteri-has").TextContent));
    }

    // ── Yasal kapılar: sunucu yanıtı yokken istemci EŞİK SAYISI yazmaz ─────
    [Fact]
    public void Yasal_kapilar_sunucu_yoksa_etiketlerde_RAKAM_yok_durum_Bilinmiyor()
    {
        var P = SatisProSozlugu.Tr;
        var e = Render<FisProYanSutun>(p => p.Add(x => x.NakitHad, (decimal?)null).Add(x => x.NakitAsildi, (bool?)null).Add(x => x.KimlikEsigi, (decimal?)null));
        Assert.DoesNotMatch(@"\d", e.Find("#islem-pro-nakit-etiket").TextContent);
        Assert.DoesNotMatch(@"\d", e.Find("#islem-pro-kimlik-etiket").TextContent);
        Assert.Equal(P.Bilinmiyor, N(e.Find("#islem-pro-nakit").TextContent));
        Assert.Equal(P.Bilinmiyor, N(e.Find("#islem-pro-kimlik").TextContent));
    }

    [Fact]
    public void Yasal_kapilar_kimlik_esigi_sunucudan_asildi_kimlik_gerekir()
    {
        var P = SatisProSozlugu.Tr;
        var e = Render<FisProYanSutun>(p => p.Add(x => x.KimlikEsigi, 185_000m).Add(x => x.KimlikAsildi, true));
        Assert.Contains("185.000 ₺", N(e.Find("#islem-pro-kimlik-etiket").TextContent));
        Assert.Equal(P.KimlikGerekir, N(e.Find("#islem-pro-kimlik").TextContent));
    }
    // ── TUR 3 doğrulayıcı ekleri (2026-09-25) — beklenenler SABİT ─────────────
    // Kampanya indirimli teklif: sunucu iscilikTl'yi İNDİRİM ÖNCESİ döndürür (FiyatMotoru:113),
    // satisFiyati = liste − indirim. Kalem teklif fiyatıyla (9.900) eklendiğinde ekranda
    // İNDİRİM ÖNCESİ 500 ₺ işçilik yazmak uydurmadır: ya bilinmiyor (null) ya da 400 olmalı.
    [Fact]
    public void Kampanya_indirimli_teklifte_iscilik_indirim_oncesi_500_YAZILMAZ()
    {
        var t = new Teklif(true, null, 10_000m, 9_000m, 500m, 500m, 100m, 9_900m, "k1", false, 10_000m);
        var r = FisProKalemKarti.TeklifIscilik(t, 9_900m);
        Assert.True(r is null || r == 400m, $"beklenen null ya da 400, gerçek {r}");
    }

    [Fact] public void Has_0_00005_yukari_0_0001() => Assert.Equal("0,0001", FisHesap.HasSayi(0.00005m));
    [Fact] public void Has_1_23455_yukari_1_2346() => Assert.Equal("1,2346", FisHesap.HasSayi(1.23455m));
    [Fact] public void Has_yarim_adet_3_94_x_0_5_1_97() => Assert.Equal(1.97m, FisHesap.ToplamHas([new FisKalemi { UrunId = "a", UrunAdi = "x", Adet = 0.5m }], [new UrunVeri("a", "x", "K22", null, 3.94m, null, null)]));
    [Fact] public void Iscilik_eksi_0_005_asagi_eksi_0_01() => Assert.Equal(-0.01m, FisHesap.SatirIscilik(new FisKalemi { UrunId = "a", UrunAdi = "x", Adet = 1m, IscilikTl = -0.005m }));
}
