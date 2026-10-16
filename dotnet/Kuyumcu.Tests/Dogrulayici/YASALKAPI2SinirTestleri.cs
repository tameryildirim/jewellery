using Kuyumcu.Contracts.Bilesenler.MasakFormu;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET YASAL-KAPI-2 sınır testleri (10.10.2026).
// Paket satış dışı para formlarının MASAK kapısını (ParaMasakKapisi) yazdı: tutar → eşik → kimlik formu.
// Fikstür (eşik/gereksinim) DOĞRULAYICININ kendisidir — uygulayıcının fikstürü kullanılmaz; beklenen
// değerler SABİTTİR (üretim kodundan hesaplanmaz). Eşik sayıları canlı /masak/esikler yanıtıyla aynıdır
// (kimlikEsigi 370.000, kimlikIstenenEnDusukTutar 200.000 — 10.10 canlı ölçüm).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class YASALKAPI2SinirTestleri
{
    private static readonly Esikler DgEsik = new(370_000m, 200_000m, 2_000_000.01m, 20_000_000.01m, 20, ["TC_KIMLIK", "PASAPORT"])
    { KimlikIstenenEnDusukTutar = 200_000m };

    /// <summary>Sunucu kademe aynası (doğrulayıcı fikstürü): ölçü = tutar + bugünkü toplam.</summary>
    private static MasakGereksinim DgGereksinim(decimal tutar, decimal bugun) =>
        tutar + bugun >= 200_000m
            ? new("AMAC_BEYANI", true, true, false, false, 20) { OlcuTutarTl = tutar + bugun, IslemTutarTl = tutar }
            : new("YOK", false, false, false, false, 0) { OlcuTutarTl = tutar + bugun, IslemTutarTl = tutar };

    private static ParaMasakKapisi Kapi(List<(decimal, string?)> sorular, decimal bugun = 0m, bool esikYok = false) =>
        new(() => Task.FromResult<Esikler?>(esikYok ? null : DgEsik),
            (t, c) => { sorular.Add((t, c)); return Task.FromResult(DgGereksinim(t, c is null ? 0m : bugun)); });

    private static void Doldur(MasakFormu.MasakBilgi m)
    {
        m.BelgeTuru = "TC_KIMLIK"; m.BelgeNo = "10000000146"; m.AdSoyad = "Deniz Ak";
        m.IslemAmaci = "Borç kapama"; m.Aciklama = "Önceki bilezik alışının borcunu kapatıyor";
    }

    [Fact]
    public async Task YK2D_01_sifir_tutar_soru_yok_kapi_kapali_govdede_beyan_yok()
    {
        var s = new List<(decimal, string?)>();
        var k = Kapi(s);
        await k.Ac("c1");
        await k.Tazele(0m, "c1");
        Assert.Empty(s);
        Assert.False(k.Gerekli);
        Assert.Null(k.Govde);
        Assert.Null(k.KilitSebebi("tr"));
    }

    [Fact]
    public async Task YK2D_02_eksi_tutar_mutlak_degerle_olculur()
    {
        var s = new List<(decimal, string?)>();
        var k = Kapi(s);
        await k.Ac(null);
        await k.Tazele(-250_000m, null);
        Assert.Equal(250_000m, k.Tutar);
        Assert.True(k.Gerekli);
        Assert.NotNull(k.Govde);
        Assert.Equal(250_000m, Assert.Single(s).Item1);
    }

    [Theory]
    [InlineData("0.01", false)]
    [InlineData("199999.99", false)]
    [InlineData("200000.00", true)]
    [InlineData("200000.01", true)]
    [InlineData("369999.99", true)]
    [InlineData("370000.00", true)]
    public async Task YK2D_03_kurus_siniri_esikte_form_acilir_altinda_acilmaz(string tutar, bool beklenen)
    {
        var t = decimal.Parse(tutar, System.Globalization.CultureInfo.InvariantCulture);
        var k = Kapi([]);
        await k.Ac(null);
        await k.Tazele(t, null);
        Assert.Equal(beklenen, k.Gerekli);
        Assert.Equal(beklenen, k.FormCizilir);
        Assert.Equal(beklenen, k.Govde is not null);
    }

    [Fact]
    public void YK2D_04_kurus_alti_0005_tutar_kutusunda_reddedilir_kapiya_ulasmaz()
    {
        // Para alanı varsayılan 2 ondalık: 199.999,995 kapıya (ve sunucunun AwayFromZero yuvarlamasına → 200.000,00) gitmez.
        Assert.NotNull(SayiKurali.Ondalik(199_999.995m, 2));
        Assert.Null(SayiKurali.Ondalik(199_999.99m, 2));
        Assert.Equal(200_000.00m, Math.Round(199_999.995m, 2, MidpointRounding.AwayFromZero));   // sunucu kuralı: bu yüzden kapı 2 hane ister
    }

    [Fact]
    public async Task YK2D_05_cok_buyuk_tutar_kesilmeden_olculur()
    {
        var s = new List<(decimal, string?)>();
        var k = Kapi(s);
        await k.Ac(null);
        await k.Tazele(999_999_999.99m, null);
        Assert.Equal(999_999_999.99m, k.Tutar);
        Assert.True(k.FormCizilir);
        Assert.Equal(999_999_999.99m, Assert.Single(s).Item1);
        Doldur(k.Model);
        Assert.Null(k.KilitSebebi("tr"));
    }

    [Fact]
    public async Task YK2D_06_bos_cari_null_sayilir_esik_altinda_soru_yok()
    {
        var s = new List<(decimal, string?)>();
        var k = Kapi(s, bugun: 190_000m);
        await k.Ac("");
        Assert.Null(k.CariId);
        await k.Tazele(25_000m, "");
        Assert.Empty(s);                 // müşteri yok → bağlantılı toplam sorulmaz
        Assert.False(k.Gerekli);
        await k.Tazele(25_000m, "c9");    // müşteri seçildi → bugünkü toplamla (190.000 + 25.000) form açılır
        Assert.True(k.FormCizilir);
        Assert.Equal((25_000m, "c9"), Assert.Single(s));
    }

    [Fact]
    public async Task YK2D_07_yukari_asagi_yukari_ret_bandi_ve_form_tutarla_gelir_gider()
    {
        var k = Kapi([]);
        await k.Ac("c1");
        await k.Tazele(250_000m, "c1");
        Doldur(k.Model);
        Assert.True(await k.Reddet(new ApiHata(400, "MASAK: Kimlik doğrulanamadı (masak.belgeNo)"), "tr"));
        Assert.Equal("Kimlik bilgileri kabul edilmedi: Kimlik doğrulanamadı", k.RedBandi);
        await k.Tazele(199_999.99m, "c1");
        Assert.Null(k.RedBandi);
        Assert.False(k.Gerekli);
        Assert.Null(k.Govde);
        await k.Tazele(25_000m, "c1");
        Assert.Null(k.RedBandi);
        await k.Tazele(250_000m, "c1");
        Assert.Equal("Kimlik bilgileri kabul edilmedi: Kimlik doğrulanamadı", k.RedBandi);
        Assert.Equal("Deniz Ak", k.Model.AdSoyad);   // beyan kaybolmaz
        await k.Tazele(250_000m, "c2");               // müşteri değişti → eski ret izi geçersiz
        Assert.Null(k.RedBandi);
    }

    [Fact]
    public void YK2D_08_ham_metin_null_ve_alan_anahtari()
    {
        Assert.Equal("", ParaMasakKapisi.SadeMetin(null));
        Assert.Equal("Açıklama kısa", ParaMasakKapisi.SadeMetin("Açıklama kısa (masak.aciklama)"));
        Assert.False(ParaMasakKapisi.MasakReddi(new ApiHata(409, "MASAK: x")));      // 400 değil → MASAK yolu değil
        Assert.False(ParaMasakKapisi.MasakReddi(new ApiHata(400, "Tevsik sınırı")));
        Assert.True(ParaMasakKapisi.MasakReddi(new ApiHata(400, "masak: küçük harf")));
    }

    [Theory]
    [InlineData("45.000", 45_000)]
    [InlineData("250.000", 250_000)]
    [InlineData("1,5", 1.5)]
    [InlineData("199.999,99", 199_999.99)]
    public void YK2D_09_turkce_giris_cozulur(string ham, double beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal((decimal)beklenen, c.Deger);
    }

    [Fact]
    public void YK2D_10_belirsiz_1500_reddedilir()
    {
        var c = FormAlani.SayiCoz("1.500");
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, c.Sonuc);
        Assert.Null(c.Deger);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("  ").Sonuc);
    }

    [Fact]
    public async Task YK2D_11_esik_alinamazsa_kapi_istemcide_kilitlemez_ret_cikis_yolunu_soyler()
    {
        var k = Kapi([], esikYok: true);
        await k.Ac("c1");
        Assert.True(k.EsikAlinamadi);
        await k.Tazele(250_000m, "c1");
        Assert.False(k.Gerekli);
        Assert.True(await k.Reddet(new ApiHata(400, "MASAK: kimlik gerekli (masak.belgeNo)"), "tr"));
        Assert.Equal("Bu tutarda kimlik gerekiyor ama kimlik formu açılamadı. Sayfayı yenileyip yeniden deneyin.", k.RedBandi);
        Assert.Equal(k.RedBandi, k.KilitSebebi("tr"));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI TUR 2 (10.10.2026) — ek sınırlar. Beklenen değerler SABİT (üretim kodundan hesaplanmaz).
// ═════════════════════════════════════════════════════════════════════════════
public sealed class YASALKAPI2SinirTestleriTur2
{
    private static readonly Esikler E2 = new(370_000m, 200_000m, 2_000_000.01m, 20_000_000.01m, 20, ["TC_KIMLIK", "PASAPORT"])
    { KimlikIstenenEnDusukTutar = 200_000m };

    private static MasakGereksinim G(decimal t) => t >= 200_000m
        ? new("AMAC_BEYANI", true, true, false, false, 20) { OlcuTutarTl = t, IslemTutarTl = t }
        : new("YOK", false, false, false, false, 0) { OlcuTutarTl = t, IslemTutarTl = t };

    private static ParaMasakKapisi K(List<decimal> sorular, bool hata = false) =>
        new(() => Task.FromResult<Esikler?>(E2),
            (t, c) => { sorular.Add(t); return hata ? throw new ApiHata(500, "sunucu") : Task.FromResult(G(t)); });

    [Fact]
    public async Task YK2T2_01_sifira_inince_form_kilit_ve_govde_kalkar()
    {
        var s = new List<decimal>(); var k = K(s);
        await k.Ac("c1");
        await k.Tazele(250_000m, "c1");
        Assert.True(k.FormCizilir);
        await k.Tazele(0m, "c1");
        Assert.False(k.Gerekli); Assert.False(k.FormCizilir); Assert.Null(k.Govde); Assert.Null(k.KilitSebebi("tr"));
        Assert.Equal(new[] { 250_000m }, s);   // 0 için sunucuya soru GİTMEZ
    }

    [Fact]
    public async Task YK2T2_02_eksi_tutar_sunucuya_mutlak_deger_olarak_sorulur()
    {
        var s = new List<decimal>(); var k = K(s);
        await k.Ac("c1");
        await k.Tazele(-200_000m, "c1");
        Assert.Equal(200_000m, k.Tutar);
        Assert.Equal(new[] { 200_000m }, s);
        Assert.True(k.FormCizilir);
    }

    [Theory]
    [InlineData("199999.99", false)]
    [InlineData("199999.995", false)]   // kapı kuruş altını yukarı YUVARLAMAZ (tutar kutusu zaten 3. haneyi reddeder)
    [InlineData("200000.00", true)]
    [InlineData("200000.001", true)]
    public async Task YK2T2_03_kurus_siniri_cari_bilinmezken(string tutar, bool beklenen)
    {
        var k = K([]);
        await k.Ac(null);
        await k.Tazele(decimal.Parse(tutar, System.Globalization.CultureInfo.InvariantCulture), null);
        Assert.Equal(beklenen, k.EsikUstu);
        Assert.Equal(beklenen, k.FormCizilir);
    }

    [Fact]
    public void YK2T2_04_ekranda_tutar_tr_TR_ve_kurus_AwayFromZero()
    {
        Assert.Equal("220.000 ₺", Kuyumcu.Web.Servisler.Bicim.Tl(220_000m));
        Assert.Equal("200.000 ₺", Kuyumcu.Web.Servisler.Bicim.Tl(199_999.995m));
        Assert.Equal("199.999,99 ₺", Kuyumcu.Web.Servisler.Bicim.Tl(199_999.994m));
        Assert.Equal("999.999.999,99 ₺", Kuyumcu.Web.Servisler.Bicim.Tl(999_999_999.99m));
    }

    [Fact]
    public async Task YK2T2_05_cok_buyuk_tutar_kesilmeden_sorulur_ve_govdeye_gider()
    {
        var s = new List<decimal>(); var k = K(s);
        await k.Ac("c1");
        await k.Tazele(999_999_999.99m, "c1");
        Assert.Equal(new[] { 999_999_999.99m }, s);
        Assert.NotNull(k.Govde);
    }

    [Fact]
    public async Task YK2T2_06_gereksinim_sorusu_coker_esik_ustunde_kilit_KALKMAZ()
    {
        var k = K([], hata: true);
        await k.Ac("c1");
        await k.Tazele(250_000m, "c1");
        Assert.True(k.SorguHatasi);
        Assert.Equal("Kimlik gereksinimi alınamadı; kayıt şimdilik kilitli. Tutarı yeniden yazın ya da sayfayı yenileyin.", k.KilitSebebi("tr"));
        Assert.True(k.Gerekli);          // eşik üstü: kapı açık sayılır (fail-closed)
        Assert.False(k.FormCizilir);     // gereksinim yok → form çizilmez ama kilit sebebi yazılı (çıkışsız değil)
    }

    [Fact]
    public void YK2T2_07_masak_disi_ret_yutulmaz()
    {
        Assert.False(ParaMasakKapisi.MasakReddi(new ApiHata(500, "MASAK: sunucu hatası")));
        Assert.False(ParaMasakKapisi.MasakReddi(new ApiHata(400, "Kasa seçin")));
        Assert.True(ParaMasakKapisi.MasakReddi(new ApiHata(400, "MASAK: İşlem amacı belirtilmelidir")));
    }

    [Fact]
    public void YK2T2_08_pro_kilit_metinlerinde_esik_sayisi_yok_TR_EN()
    {
        foreach (var m in new[] { Kuyumcu.Core.I18n.YasalKapi2Sozlugu.Tr, Kuyumcu.Core.I18n.YasalKapi2Sozlugu.En })
        {
            Assert.DoesNotMatch(@"\d", m.MasakKilitProKimlik);
            Assert.DoesNotMatch(@"\d", m.MasakKilitProAmac);
            Assert.DoesNotContain("kaydedebilirsiniz", m.FarkYok, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("can save", m.FarkYok, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("220.000", 220_000)]
    [InlineData("200.000,00", 200_000)]
    [InlineData("0,01", 0.01)]
    public void YK2T2_09_turkce_giris(string ham, double beklenen)
    {
        var c = FormAlani.SayiCoz(ham);
        Assert.Equal(FormAlani.SayiSonucu.Tamam, c.Sonuc);
        Assert.Equal((decimal)beklenen, c.Deger);
    }
}
