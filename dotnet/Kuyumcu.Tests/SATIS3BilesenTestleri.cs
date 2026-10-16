using Bunit;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

#pragma warning disable CS0618 // GOC-C-2 tur 2: FisFark.SifirSatirlariDusur [Obsolete] — ekrandan çağrılmaz; burada yalnız eski saf kural sözleşmesi sınanır

// ═════════════════════════════════════════════════════════════════════════════
// PAKET SATIS-3 — bileşen düzeyi: eski altın ödeme paneli (KAYNAK 2 md.1/2/4/7/8) ve
// Diğer formlarının isteğe bağlı alan + geçmiş bölüm katlanması (kurucu 11:35).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class SATIS3BilesenTestleri : BunitContext
{
    public SATIS3BilesenTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private static AltinBacagiAyarlari Ayarlar(bool adresZorunlu = true) => new(
    [
        new("NIHAI_TUKETICI", 0m, true),
        new("ESNAF_MUAF", 0.02m, true),
        new("MUKELLEF", 0m, false),
    ], null, SaticiAdresiZorunlu: adresZorunlu);

    private IRenderedComponent<AltinBacagiPaneli> Panel(bool basit, bool adresZorunlu = true) =>
        Render<AltinBacagiPaneli>(p => p
            .Add(x => x.Ayarlar, Ayarlar(adresZorunlu))
            .Add(x => x.HasFiyati, 6_827.20m)
            .Add(x => x.FisToplamTl, 20_000m)
            .Add(x => x.DigerBacaklarTl, 0m)
            .Add(x => x.Basit, basit)
            .Add(x => x.Giris, new AltinBacagiGirisi { AyarId = "K22", Gram = 5m, Fiyat = 2_000m, FiyatElle = true }));

    [Fact]
    public void Altin_paneli_BASIT_teknik_metinler_GIZLI_PRO_gorunur()
    {
        var b = Panel(basit: true);
        foreach (var id in new[] { "altin-aciklama", "altin-satici-not", "altin-belgeler", "altin-fiyat-referans" })
            Assert.True(b.Find($"#{id}").HasAttribute("hidden"), id);
        Assert.Empty(b.FindAll("#altin-tl-ipucu"));
        var p = Panel(basit: false);
        foreach (var id in new[] { "altin-aciklama", "altin-satici-not", "altin-belgeler", "altin-fiyat-referans" })
            Assert.False(p.Find($"#{id}").HasAttribute("hidden"), id);
    }

    [Fact]
    public void Altin_paneli_has_karsiligi_BIR_KEZ_kaynak_rozeti_ETIKET_satirinda_fiyat_kutusu_HIZALI()
    {
        var c = Panel(basit: true);
        Assert.Empty(c.FindAll("#altin-gram-onizleme"));   // gram altında "… has" tekrarı yok
        Assert.Contains("has", c.Find("#altin-has").TextContent);
        var rozet = c.Find("#altin-fiyat-kaynak");
        Assert.Contains("Elle", rozet.TextContent);
        Assert.NotNull(rozet.Closest(".alan-etiket-satir, label, .alan"));
        // fiyat kutusu Ayar ve Gram ile aynı ızgaranın DOĞRUDAN hücresi (iç içe .alan yok)
        var izgara = c.Find("#altin-bacagi > .form-grid-3");
        Assert.Empty(izgara.QuerySelectorAll(".alan .alan #altin-fiyat"));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Altin_paneli_adres_etiketi_TEK_dogru(bool zorunlu, bool istegeYazar)
    {
        var c = Panel(basit: true, adresZorunlu: zorunlu);
        var etiket = c.Find("label[for=altin-satici-adres]").TextContent;
        Assert.Contains(Satis3Sozlugu.Tr.SaticiAdres, etiket);
        Assert.Equal(istegeYazar, etiket.Contains("isteğe", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("numeric", c.Find("#altin-satici-tckn").GetAttribute("inputmode"));
    }

    private static DigerBaglam Baglam() => new(
        [new CariVeri("c1", "Anadolu Holding A.Ş.")],
        [new UrunVeri("u1", "22 Ayar Burma Bilezik", "K22", "22 Ayar", 3.94m, 0m, 10_000m)],
        [new KasaVeri("ks1", "Merkez Kasa", "TRY", true)], [], true, "MUDUR");

    [Fact]
    public void Diger_URUN_AYIR_son_gun_ve_aciklama_KATLI_gecmis_KAPALI()
    {
        var c = Render<FisDigerRezerv>(p => p.Add(x => x.Durum, new DigerIslemDurumu()).Add(x => x.Baglam, Baglam()));
        Assert.False(c.Find("#rezerv-ayrinti").HasAttribute("open"));
        Assert.NotNull(c.Find("#rezerv-ayrinti #rezerv-son-gun"));
        Assert.NotNull(c.Find("#rezerv-ayrinti #rezerv-aciklama"));
        Assert.Null(c.Find("#rezerv-adet").Closest("details"));   // zorunlu alan açıkta
        Assert.False(c.Find("#rezerv-gecmis").HasAttribute("open"));
    }

    [Fact]
    public void Diger_KAPORA_aciklama_KATLI_mahsup_ve_iade_bolumleri_KAPALI()
    {
        var c = Render<FisDigerAvans>(p => p.Add(x => x.Durum, new DigerIslemDurumu()).Add(x => x.Baglam, Baglam()));
        Assert.False(c.Find("#avans-ayrinti").HasAttribute("open"));
        Assert.NotNull(c.Find("#avans-ayrinti #avans-aciklama"));
        Assert.Null(c.Find("#avans-tutar").Closest("details"));
        Assert.False(c.Find("#avans-mahsup-gecmis").HasAttribute("open"));
        Assert.False(c.Find("#avans-iade-gecmis").HasAttribute("open"));
    }

    [Fact]
    public void Diger_KONSINYE_aciklama_KATLI_liste_KAPALI()
    {
        var c = Render<FisDigerKonsinye>(p => p.Add(x => x.Durum, new DigerIslemDurumu()).Add(x => x.Baglam, Baglam()));
        Assert.False(c.Find("#konsinye-ayrinti").HasAttribute("open"));
        Assert.False(c.Find("#konsinye-gecmis").HasAttribute("open"));
    }

    [Fact]
    public void OtoDengele_elle_yazilmayan_SON_TL_bacagi_hedef_altin_ve_doviz_HEDEF_OLMAZ()
    {
        var kart = new FisBacagi { Arac = "KART", TutarTl = 10_000m };
        var altin = new FisBacagi { Arac = FisAltinBacagi.Arac, TutarTl = 2_500m };
        var l = new List<FisBacagi> { kart, altin };
        Assert.True(FisHesap.OtoDengele(l, 10_000m));
        Assert.Equal(7_500m, kart.TutarTl);
        Assert.Equal(2_500m, altin.TutarTl);
        kart.ElleYazildi = true;
        altin.TutarTl = 3_000m;
        Assert.False(FisHesap.OtoDengele(l, 10_000m));   // elle yazılana dokunulmaz
        Assert.Equal(7_500m, kart.TutarTl);
        Assert.False(FisHesap.OtoDengele([new FisBacagi { TutarTl = 1m }], 10_000m));   // tek bacak: dokunulmaz
    }

    [Fact]
    public void Son_satis_ZAMANI_bugun_saat_eski_gun_YALNIZ_tarih()
    {
        // tur.mjs 28.09 (BASİT 1280, K10): "27.09 23:02 · Anadolu A." dar sütunda kesiliyordu — eski günde saat yazılmaz.
        var tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
        var simdi = DateTime.UtcNow;
        Assert.Equal(simdi.ToLocalTime().ToString("HH:mm", tr), FisSonSatislar.Zaman(simdi));
        var eski = simdi.AddDays(-2);
        Assert.Equal(eski.ToLocalTime().ToString("dd.MM", tr), FisSonSatislar.Zaman(eski));
        Assert.DoesNotContain(":", FisSonSatislar.Zaman(eski));
    }
    // ── Doğrulama 4. tur (28.09) ────────────────────────────────────────────
    [Fact]
    public void Hurda_hesap_satiri_has_ve_tutar_SUNUCU_formuluyle_eksikse_tire_kesinti_notu_YALNIZ_esnafta()
    {
        var h = new HurdaGirisi { Gram = 12.5m, Ayar = "K18", Fiyat = 6_587.09m, SaticiTipi = "NIHAI_TUKETICI" };
        var c = Render<FisHurdaAlis>(p => p.Add(x => x.Hurda, h).Add(x => x.Ayarlar, new[] { "K18", "K22" }).Add(x => x.Basit, true));
        var ayar = Kuyumcu.Core.Domain.Ayarlar.Bul("K18")!;
        var has = Kuyumcu.Core.Domain.HasHesap.ToHasGram(12.5m, ayar);
        Assert.Equal(Bicim.Has(has), c.Find("#hurda-hesap-has").TextContent.Trim());
        Assert.Equal(Bicim.Tl(Math.Round(has * 6_587.09m, 2, MidpointRounding.AwayFromZero)), c.Find("#hurda-hesap-tutar").TextContent.Trim());
        Assert.Equal(Math.Round(has * 6_587.09m, 2, MidpointRounding.AwayFromZero), h.HesaplananTutar);   // sunucu Yuvarla2 ile aynı (2 hane)
        Assert.Empty(c.FindAll("#hurda-hesap-not"));

        var bos = new HurdaGirisi { Ayar = "K18", SaticiTipi = "ESNAF_MUAF" };
        var d = Render<FisHurdaAlis>(p => p.Add(x => x.Hurda, bos).Add(x => x.Ayarlar, new[] { "K18" }).Add(x => x.Basit, true));
        Assert.Equal("—", d.Find("#hurda-hesap-has").TextContent.Trim());
        Assert.Equal("—", d.Find("#hurda-hesap-tutar").TextContent.Trim());
        Assert.Single(d.FindAll("#hurda-hesap-not"));
    }

    [Fact]
    public void Kalem_tablosu_BASIT_ad_KENDI_span_inda_ayar_gram_ikinci_satir_verisinde()
    {
        var urunler = new List<UrunVeri> { new("u1", "14 Ayar Zirkon Taşlı Kolye", "K14", null, 0m, null, null, GramAgirlik: 3.2m) };
        var kalemler = new List<FisKalemi> { new() { UrunId = "u1", UrunAdi = "14 Ayar Zirkon Taşlı Kolye", Adet = 1, BirimFiyat = 10_000m } };
        var c = Render<FisKalemTablosu>(p => p.Add(x => x.Kalemler, kalemler).Add(x => x.Urunler, urunler)
            .Add(x => x.Form, new FormDurumu()).Add(x => x.Satis, true).Add(x => x.Basit, true));
        var td = c.Find("#islem-kalem-tablo td.fis-tuval-ad");
        Assert.Equal("14 Ayar Zirkon Taşlı Kolye", td.QuerySelector(".fis-tuval-ad-metin")!.TextContent);
        // GOC-C-2: ortak ızgara td'si satıra özgü öznitelik almaz → tam ad title'ı ad span'ında, "ayar · gram" ikinci satır
        // td'nin data-ayrinti'si (::after) yerine aynı hücredeki span'da (aynı metin, aynı biçim).
        Assert.Equal("14 Ayar Zirkon Taşlı Kolye", td.QuerySelector(".fis-tuval-ad-metin")!.GetAttribute("title"));
        Assert.Contains("g", td.QuerySelector(".fis-tuval-ad-ayrinti")!.TextContent);
    }

    // ═══ t3 TASARIM HAKEMİ (28.09) ═══════════════════════════════════════════

    [Fact]
    public void T3_PRO_teknik_notlar_TEK_katli_BILGI_ipucunda_buyuk_harf_vurgusu_YOK()
    {
        var p = Panel(basit: false);
        var bilgi = p.Find("#altin-bilgi");
        Assert.Equal("DETAILS", bilgi.TagName);
        Assert.False(bilgi.HasAttribute("open"));
        foreach (var id in new[] { "altin-aciklama", "altin-satici-not", "altin-belgeler", "altin-fiyat-referans" })
            Assert.NotNull(bilgi.QuerySelector($"#{id}"));
        Assert.DoesNotContain("İKİ", p.Find("#altin-belgeler").TextContent);
        Assert.Empty(p.FindAll("#altin-tl-ipucu"));
        Assert.True(Panel(basit: true).Find("#altin-bilgi").HasAttribute("hidden"));
    }

    [Fact]
    public void T3_Guncel_fiyati_KULLAN_dokunusla_doldurur_rozet_KURDAN_elle_yazinca_ELLE()
    {
        AltinBacagiDurumu? son = null;
        var g = new AltinBacagiGirisi { AyarId = "K22", Gram = 5m };
        var c = Render<AltinBacagiPaneli>(p => p
            .Add(x => x.Ayarlar, Ayarlar()).Add(x => x.HasFiyati, 6_827.20m).Add(x => x.FisToplamTl, 20_000m)
            .Add(x => x.Giris, g).Add(x => x.Degisti, (AltinBacagiDurumu d) => son = d));
        Assert.Null(g.Fiyat);                                  // K-KUR-4: kendiliğinden DOLMAZ
        c.Find("#altin-fiyat-kurdan").Click();
        Assert.Equal(6_827.20m, g.Fiyat);
        Assert.Equal(Sozlukler.Tr.Islem.KurdanGeldi, c.Find("#altin-fiyat-kaynak").TextContent.Trim());
        Assert.Empty(c.FindAll("#altin-fiyat-kurdan"));       // değer zaten güncel → düğme yok
        Assert.NotNull(son);
        c.Find("#altin-fiyat").Input("6500");
        Assert.Equal(AltinBacagiSozlugu.Tr.FiyatElle, c.Find("#altin-fiyat-kaynak").TextContent.Trim());
    }

    [Fact]
    public void T3_Diger_formlari_iki_kasada_ILK_kasa_varsayilan_secilir_kullanici_secimi_EZILMEZ()
    {
        var baglam = new DigerBaglam([], [], [new KasaVeri("k1", "Ana Kasa", "TRY", true), new KasaVeri("k2", "Vitrin", "TRY", true)], [], true, "SAHIP");
        var d = new DigerIslemDurumu();
        Render<FisDigerKasa>(p => p.Add(x => x.Durum, d).Add(x => x.Baglam, baglam).Add(x => x.Onek, "avans"));
        Assert.Equal("k1", d.KasaId);
        var d2 = new DigerIslemDurumu { KasaId = "k2" };
        Render<FisDigerKasa>(p => p.Add(x => x.Durum, d2).Add(x => x.Baglam, baglam).Add(x => x.Onek, "avans"));
        Assert.Equal("k2", d2.KasaId);
    }

    [Fact]
    public void T6_Diger_formlari_varsayilan_kasa_OTURUMUN_subesinden_yoksa_BOS_ve_secici_zorunlu()
    {
        // Sunucu sırasında ilk kasa başka şubenin (s2); oturum s1 → s1'in kasası.
        var kasalar = new List<KasaVeri> { new("k1", "Kadıköy", "TRY", true, "s2"), new("k2", "Merkez", "TRY", true, "s1") };
        var d = new DigerIslemDurumu();
        Render<FisDigerKasa>(p => p.Add(x => x.Durum, d).Add(x => x.Baglam, new DigerBaglam([], [], kasalar, [], true, "TEZGAHTAR", "s1")).Add(x => x.Onek, "avans"));
        Assert.Equal("k2", d.KasaId);
        // Oturumun şubesinde kasa yok → sessizce başka şubenin kasası ATANMAZ; seçici görünür (fail-closed).
        var d2 = new DigerIslemDurumu();
        var e2 = Render<FisDigerKasa>(p => p.Add(x => x.Durum, d2).Add(x => x.Baglam, new DigerBaglam([], [], kasalar, [], true, "TEZGAHTAR", "s9")).Add(x => x.Onek, "avans"));
        Assert.True(string.IsNullOrEmpty(d2.KasaId));
        Assert.Single(e2.FindAll("#avans-kasa"));
    }

    /// <summary>SATIS-3 t9 (doğrulayıcı DÜŞÜK): bilinmeyen eski ayar has=0 verir — takas TAMAM sayılmaz (yanlış fark kaydedilemez).</summary>
    [Fact]
    public void SATIS3_T9_takas_bilinmeyen_eski_ayar_TAMAM_degil()
    {
        var t = new TakasGirisi { EskiAyar = "K22", EskiGram = 5m, EskiFiyat = 6800m, YeniUrunId = "u1", YeniAdet = 1, YeniFiyat = 10000m };
        Assert.True(t.Tamam);
        t.EskiAyar = "K99";
        Assert.Equal(0m, t.EskiHas);
        Assert.False(t.Tamam);
    }

    // ═══ t10 — karma ödeme ve sunucu mesajı saf kuralları ═══════════════════════════════════════
    [Fact]
    public void SATIS3_T10_yeni_odeme_satiri_MEVCUTLA_AYNI_aracla_acilmaz()
    {
        Assert.Equal("NAKIT", FisFark.YeniBacakAraci([]));
        Assert.Equal("KART", FisFark.YeniBacakAraci([new FisBacagi { Arac = "NAKIT" }]));
        Assert.Equal("NAKIT", FisFark.YeniBacakAraci([new FisBacagi { Arac = "KART" }]));
        Assert.Equal("NAKIT", FisFark.YeniBacakAraci([new FisBacagi { Arac = "ALTIN" }]));
        Assert.Equal("HAVALE", FisFark.YeniBacakAraci([new FisBacagi { Arac = "NAKIT" }, new FisBacagi { Arac = "KART" }]));
    }

    [Fact]
    public void SATIS3_T10_otomatik_0_TL_satir_DUSER_elle_yazilan_ve_duzenlenen_KALIR()
    {
        var nakit = new FisBacagi { Arac = "NAKIT", TutarTl = 0m };
        var kart = new FisBacagi { Arac = "KART", TutarTl = 100m, ElleYazildi = true };
        var l = new List<FisBacagi> { nakit, kart };
        Assert.True(FisFark.SifirSatirlariDusur(l, kart));
        Assert.Equal([kart], l);
        var elle = new FisBacagi { Arac = "NAKIT", TutarTl = 0m, ElleYazildi = true };
        var l2 = new List<FisBacagi> { elle, kart };
        Assert.False(FisFark.SifirSatirlariDusur(l2, kart));   // kullanıcı 0 yazdıysa dokunulmaz
        var tek = new List<FisBacagi> { new() { Arac = "NAKIT", TutarTl = 0m } };
        Assert.False(FisFark.SifirSatirlariDusur(tek, null));  // en az bir satır kalır
        var duz = new FisBacagi { Arac = "NAKIT", TutarTl = 0m };
        var l3 = new List<FisBacagi> { duz, kart };
        Assert.False(FisFark.SifirSatirlariDusur(l3, duz));    // düzenlenen satır düşmez
    }

    [Fact]
    public void SATIS3_T10_sunucu_metnindeki_TL_tutari_Bicim_Tl_ile_yazilir()
    {
        var m = Kuyumcu.Web.Pages.Islemler.TlSembolu("Fark 127.937,79 TL, eşik (30.000,00 TL).");
        Assert.Equal($"Fark {Bicim.Tl(127937.79m)}, eşik ({Bicim.Tl(30000m)}).", m);
        Assert.Equal($"MASAK: {Bicim.Tl(185000m)} ve üstü", Kuyumcu.Web.Pages.Islemler.TlSembolu("MASAK: 185.000 TL ve üstü"));
    }

    // SATIS-3 t12 (doğrulayıcı KRİTİK): 459 VUK GT §4.1 "…TL'yi AŞAN tutardaki…" — metin "ve üstü" demez, "üstündeki / above" der.
    [Fact]
    public void T12_nakit_hadd_metinleri_ASAN_anlaminda_ve_ustu_DEMEZ_iki_dil()
    {
        foreach (var m in new[] { Satis3Sozlugu.Al("tr").NakitHaddiKilit, SatisTuvalSozlugu.Al("tr").NakitUyari })
        {
            Assert.DoesNotContain("ve üstü", m);
            Assert.Contains("{0} üstündeki", m);
        }
        foreach (var m in new[] { Satis3Sozlugu.Al("en").NakitHaddiKilit, SatisTuvalSozlugu.Al("en").NakitUyari })
        {
            Assert.DoesNotContain("or more", m);
            Assert.Contains("above {0}", m);
        }
    }
}
