using System.Globalization;
using System.Net;
using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET DEGISIM-GUNSONU — testler.
//   (G) Gün sonu FARK'ın adı: BASİT cümle ve PRO kart TEK hesap (m.Fark); PRO kartları
//       Satış toplamı − Satış faturası = fark; tahsilat kartı ayrı; sıfır kalem yok; adet tek kalıp.
//   (B) /islemler üst bar başlığı seçili tür (Değişim) — yuva bileşeni.
//   (T) BASİT Değişim Verdim/Aldım fişi: id'ler aynen, tek kimlik kutusu, sade etiket, Vazgeç; PRO aynen.
//   (N) Değişim farkının nakit sınırı: Satış/Alış'la AYNI kaynak (sunucu had'i), sınır 29.999,99 / 30.000 / 30.000,01.
// ⚠ Eşik sayısı testte SAHTE SUNUCUDAN gelir (istemciye gömülmez).
// ═════════════════════════════════════════════════════════════════════════════

// ── (N) saf kural ───────────────────────────────────────────────────────────
public sealed class DegisimNakitSiniriSafTestleri
{
    // Tur 2 (doğrulayıcı DÜŞÜK "saf testler ölü yardımcıyı sınıyor"): TakasOdemeVarsayilani artık ÜRETİM yolu —
    // NakitSiniriAsildi'nin TAKAS dalı onu çağırır (Islemler.Satis4.cs); kapıya giden tutar TakasTevsikTutari (sunucu gibi
    // kuruşa yuvarlanmış). Ekran testleri (DG_N_*) aynı yolu sayfa üzerinden sınar.
    private static string? Karar(decimal fark, decimal had) => Kuyumcu.Web.Pages.Islemler.TakasOdemeVarsayilani(fark, had);

    [Theory]
    [InlineData("29999.99", null)]
    [InlineData("30000", null)]          // tam hadde nakit serbest (sunucu "olcut > Had")
    [InlineData("30000.01", "KART")]     // müşteri öder → Kart
    [InlineData("-29999.99", null)]
    [InlineData("-30000", null)]
    [InlineData("-30000.01", "HAVALE")]  // müşteriye ödenir → Havale
    [InlineData("30000.004", null)]      // kuruş altı: sunucu Yuvarla2 → 30.000,00 → nakit KABUL; istemci de Nakit bırakır
    [InlineData("30000.005", "KART")]    // AwayFromZero → 30.000,01 → sunucu RED; istemci Kart
    [InlineData("-30000.004", null)]
    [InlineData("-30000.005", "HAVALE")]
    public void DG_takas_uretim_yolu_sinir_karari(string fark, string? beklenen)
    {
        var f = decimal.Parse(fark, CultureInfo.InvariantCulture);
        Assert.Equal(beklenen, Karar(f, 30000m));
    }

    [Theory]
    [InlineData("30000.004", "30000.00")]
    [InlineData("30000.005", "30000.01")]
    [InlineData("-30000.005", "30000.01")]
    [InlineData("-0.004", "0.00")]
    public void DG_takas_tevsik_tutari_SUNUCU_gibi_kurusa_yuvarlanir_AwayFromZero(string fark, string beklenen)
    {
        var f = decimal.Parse(fark, CultureInfo.InvariantCulture);
        Assert.Equal(decimal.Parse(beklenen, CultureInfo.InvariantCulture), Kuyumcu.Web.Pages.Islemler.TakasTevsikTutari(f));
        Assert.Equal(0m, Kuyumcu.Web.Pages.Islemler.TakasTevsikTutari(null));
    }

    [Fact]
    public void DG_takas_had_BILINMIYORSA_varsayilan_yok_istemci_esik_uydurmaz()
    {
        Assert.Null(Kuyumcu.Web.Pages.Islemler.TakasOdemeVarsayilani(500000m, null));
        Assert.Null(Kuyumcu.Web.Pages.Islemler.TakasOdemeVarsayilani(500000m, 0m));
        Assert.Null(Kuyumcu.Web.Pages.Islemler.TakasOdemeVarsayilani(null, 30000m));
    }

    [Fact]
    public void DG_takas_yon_musteri_oderse_KART_biz_odersek_HAVALE()
    {
        Assert.Equal("KART", Kuyumcu.Web.Pages.Islemler.TakasNakitDisiYon(0.01m));
        Assert.Equal("HAVALE", Kuyumcu.Web.Pages.Islemler.TakasNakitDisiYon(-0.01m));
    }

    /// <summary>Satış/Alış ile AYNI kaynak: aynı tutarda satıştaki öneri ile değişimdeki öneri aynı kararı verir.</summary>
    [Theory]
    [InlineData("29999.99")]
    [InlineData("30000")]
    [InlineData("30000.01")]
    public void DG_takas_sinir_karari_satisla_AYNI(string tutar)
    {
        var t = decimal.Parse(tutar, CultureInfo.InvariantCulture);
        var satis = TevsikOneri.Gerekli("NAKIT", "SATIS", t, 30000m);
        Assert.Equal(satis, Karar(t, 30000m) is not null);
        Assert.Equal(satis, Karar(-t, 30000m) is not null);
    }
}

// ── (N)(T)(B) ekran ─────────────────────────────────────────────────────────
public sealed class DegisimEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string? tevsik = """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":30000,"islemTutariKuraliDevrede":false}""";
    private string gereksinim = """{"kademe":"YOK","kimlikGerekli":false,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""";
    private static readonly TimeSpan Bekle = TimeSpan.FromSeconds(5);
    /// <summary>true: /tevsik/gunluk Değişim'e geçilene kadar düşer (Satış'ta had hiç alınamamış sayfa).</summary>
    private bool hadYalnizTakasta, takasaGecildi;

    public DegisimEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(DegisimEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            if (yol.Contains("/tevsik/gunluk") && (t.tevsik is null || (t.hadYalnizTakasta && !t.takasaGecildi)))
                return ParaDemoSahte.Hata(500, "uç düştü");
            return ParaDemoSahte.Tamam(yol switch
            {
                var v when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","aktif":true,"sistemCarisi":true}""",
                var v when v.EndsWith("/cari") => """
                    [{"id":"c1","ad":"Tuna Akarsu","tur":"MUSTERI","telefon":null,"email":null,"tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"riskSeviye":null,"pepBeyani":false,"sistemCarisi":false,"aktif":true},
                     {"id":"pm1","ad":"Peşin Müşteri","tur":"MUSTERI","telefon":null,"email":null,"tlBakiye":0,"hasGramBakiye":0,"usdBakiye":0,"eurBakiye":0,"gbpBakiye":0,"riskSeviye":null,"pepBeyani":false,"sistemCarisi":true,"aktif":true}]
                    """,
                var v when v.Contains("/tevsik/gunluk") => t.tevsik!,
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":1000,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":3.206,"iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Ana Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                var v when v.Contains("/masak/gereksinim") => t.gereksinim,
                var v when v.Contains("/islemler/altin-bacagi/ayarlar") => """{"stopajOranlari":[{"saticiTipi":"NIHAI_TUKETICI","oran":0,"pusulaGerekir":true}],"alisIskontoOran":null,"saticiAdresiZorunlu":true}""",
                var v when v.Contains("/islemler/takas") => """{"id":"h9","fisId":"f9"}""",
                var v when v.Contains("/reyonlar") || v.Contains("/islemler/son") || v.Contains("/pos/cihazlar") || v.Contains("/islemler/personeller") || v.Contains("/iade-adaylari") => "[]",
                _ => "{}",
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Ekran(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Sahip", "t@ornek.com", "SAHIP", "s1", null, mod));
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#islem-tur-SATIS");
        takasaGecildi = true;
        e.Find("#islem-tur-TAKAS").Click();
        e.WaitForElement("#takas-satici");
        return e;
    }

    /// <summary>
    /// Fark = yeni fiyat − eski has × has fiyatı. Has fiyatı 1.000 (sahte kur), eski 1 g 24 ayar = 1 has →
    /// eski değer 1.000; yeni fiyat = 1.000 + hedef fark (has × fiyat kuruşa kadar tam — sınır testi için).
    /// </summary>
    private static void Doldur(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, decimal fark, string cari = "c1")
    {
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        SeciciYardim.Sec(e, "islem-cari", cari);
        SeciciYardim.Sec(e, "takas-eski-ayar", "K24");   // 1.000 milyem → 1 g = 1 has (kuruşa tam)
        // ISLEM-BANT-2: eksi farkı 1.000'in katıysa has fiyatı 1.000'de (işlem bandı 500–1.100 içinde) kalır, fark GRAMDAN kurulur —
        // "1 g × 201.000" mutlak sınır dışıydı ve Kaydet artık bant dışında kilitli. Kuruşlu sınır farkları eski yoldan.
        var gramIle = fark < 0m && fark % 1000m == 0m;
        e.Find("#takas-eski-gram").Input(gramIle ? (1m - fark / 1000m).ToString("0", tr) : "1");
        e.Find("#takas-eski-fiyat").Input((fark >= 0m || gramIle ? 1000m : 1000m - fark).ToString("0.00", tr));
        SeciciYardim.Sec(e, "takas-yeni-urun", "u1");
        e.Find("#takas-yeni-fiyat").Input((fark >= 0m ? 1000m + fark : 1000m).ToString("0.00", tr));
    }

    /// <summary>Seçili ödeme türü: seçici görünürse onun değeri; BASİT'te Nakit sade satırla yazılır (seçici gizli).</summary>
    private static string? Odeme(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e) =>
        e.FindAll("#islem-odeme").Count > 0 ? SeciciYardim.SeciliDeger(e, "islem-odeme")
        : e.FindAll("#islem-odeme-sade").Count > 0 ? (e.Find("#islem-odeme-sade").TextContent.Contains(Sozlukler.Tr.Islem.OdemeNakit) ? "NAKIT" : null)
        : "NAKIT";   // BASİT Değişim: had altındaki nakit farkta ödeme türü SORULMAZ (FisOdemeKasa — zorunlu önce); gövdede NAKIT

    [Theory]
    [InlineData("29999.99", "NAKIT")]
    [InlineData("30000", "NAKIT")]
    [InlineData("30000.01", "KART")]
    public void DG_N_BASIT_degisim_farki_nakit_siniri_satisla_ayni_kaynak(string fark, string beklenen)
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        Doldur(e, decimal.Parse(fark, CultureInfo.InvariantCulture));
        e.WaitForAssertion(() => Assert.Equal(beklenen, Odeme(e)), Bekle);
        if (beklenen == "KART")
            Assert.Equal(string.Format(KesifP5Sozlugu.Tr.OdemeOtomatikKart, Bicim.Tl(30000m)), e.Find("#islem-odeme-otomatik").TextContent.Trim());
        else
            Assert.Empty(e.FindAll("#islem-odeme-otomatik"));
    }

    [Fact]
    public void DG_N_PRO_degisimde_biz_odersek_HAVALE_otomatik()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        Doldur(e, -30000.01m);
        e.WaitForAssertion(() => Assert.Equal("HAVALE", Odeme(e)), Bekle);
        Assert.Equal(string.Format(KesifP5Sozlugu.Tr.OdemeOtomatikHavale, Bicim.Tl(30000m)), e.Find("#islem-odeme-otomatik").TextContent.Trim());
    }

    [Fact]
    public void DG_N_had_BILINMIYORSA_degisimde_nakit_kalir()
    {
        tevsik = null;
        var e = Ekran(ArayuzModuServisi.Basit);
        Doldur(e, 50000m);
        e.WaitForAssertion(() => Assert.Equal("NAKIT", Odeme(e)), Bekle);
        Assert.Empty(e.FindAll("#islem-odeme-otomatik"));
    }

    [Fact]
    public void DG_N_had_Satista_ALINAMAMISSA_Degisime_gecince_sunucudan_alinir_KART()
    {
        hadYalnizTakasta = true;
        var e = Ekran(ArayuzModuServisi.Basit);
        Doldur(e, 30000.01m);
        e.WaitForAssertion(() => Assert.Equal("KART", Odeme(e)), Bekle);
    }

    [Fact]
    public void DG_N_fark_YONU_degisince_otomatik_arac_da_degisir_KART_HAVALE()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        Doldur(e, 30000.01m);   // müşteri öder → Kart
        e.WaitForAssertion(() => Assert.Equal("KART", Odeme(e)), Bekle);
        // Eski altın büyüdü: fark müşteriye ödenecek yöne döndü → Havale (canlı ölçümde Kart kalıyordu).
        e.Find("#takas-eski-gram").Input("100");   // 100 has × 1.000 = 100.000 → fark −68.999,99
        e.WaitForAssertion(() => Assert.Equal("HAVALE", Odeme(e)), Bekle);
        Assert.Equal(string.Format(KesifP5Sozlugu.Tr.OdemeOtomatikHavale, Bicim.Tl(30000m)), e.Find("#islem-odeme-otomatik").TextContent.Trim());
    }

    [Fact]
    public void DG_N_ELLE_secilen_arac_fark_yonu_degisse_de_EZILMEZ()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        Doldur(e, 30000.01m);
        e.WaitForAssertion(() => Assert.Equal("KART", Odeme(e)), Bekle);
        SeciciYardim.Sec(e, "islem-odeme", "KART");   // kullanıcı elle seçti
        e.Find("#takas-eski-gram").Input("100");   // 100 has × 1.000 = 100.000 → fark −68.999,99
        e.WaitForAssertion(() => Assert.Equal("KART", Odeme(e)), Bekle);
        Assert.Empty(e.FindAll("#islem-odeme-otomatik"));
    }

    // ═══ (T) BASİT Değişim fişi: Verdim / Aldım ═══════════════════════════════

    private static string Etiket(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, string id) =>
        e.Find($"#{id}-sar .alan-etiket-metin").TextContent.Trim();

    [Fact]
    public void DG_T_BASIT_degisim_VERDIM_ALDIM_fisinde_id_ler_AYNEN_etiketler_sade()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        var d = DegisimGunSonuSozlugu.Tr;
        var tuval = e.Find("#takas-tuval");
        Assert.Contains("fis-tuval", tuval.ClassName ?? "", StringComparison.Ordinal);
        Assert.Equal(d.VerdimUrun, e.Find("#takas-verdim .fis-tuval-baslik").TextContent.Trim());
        Assert.Equal(d.AldimEskiAltin, e.Find("#takas-aldim .fis-tuval-baslik").TextContent.Trim());
        // VERDİM: tek arama kutusu + boş durum; ALDIM: eski altın alanları (id'ler AYNEN).
        Assert.NotNull(e.Find("#takas-verdim #islem-barkod"));
        Assert.Equal(d.VerdimBos, e.Find("#takas-verdim-bos").TextContent.Trim());
        foreach (var id in new[] { "takas-eski-ayar", "takas-eski-gram", "takas-eski-fiyat" })
            Assert.NotNull(e.Find($"#takas-aldim #{id}"));
        Assert.Equal(d.EskiGram, Etiket(e, "takas-eski-gram"));
        Assert.Equal(d.EskiHasFiyati, Etiket(e, "takas-eski-fiyat"));
        // Değişim alanlarının etiketleri birim eki ("(₺)", "(g)") taşımaz.
        var etiketler = e.FindAll("#takas-tuval label[for^='takas-'] .alan-etiket-metin").Select(x => x.TextContent.Trim()).ToList();
        Assert.NotEmpty(etiketler);
        Assert.All(etiketler, x => { Assert.DoesNotContain("(₺)", x, StringComparison.Ordinal); Assert.DoesNotContain("(g)", x, StringComparison.Ordinal); });
        // Fark henüz yok: "—" + ne zaman hesaplanacağı; ödeme sorulmaz (zorunlu önce).
        Assert.Equal("—", e.Find("#takas-tuval-fark-tutar").TextContent.Trim());
        // DEGISIM-2 (K17): bekleme cümlesi ayrı satır değil — boş fark çubuğunun ipucu (title); satır YOK.
        Assert.Equal(d.FarkBekleniyor, e.Find("#takas-tuval-fark").GetAttribute("title"));
        Assert.Empty(e.FindAll("#takas-tuval-fark-bekle"));
        Assert.Empty(e.FindAll("#takas-tuval-odeme"));
        // Eski ayrı fark kartı tuvalde çizilmez (fark tek yerde).
        Assert.Empty(e.FindAll("#takas-tuval .takas-fark-toplam"));
    }

    [Fact]
    public void DG_T_BASIT_fark_ALDIM_altinda_yon_ve_tutarla_odeme_bolumu_acilir()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        Doldur(e, 5000m);
        e.WaitForAssertion(() => Assert.Equal($"{Bicim.Para(5000m, "TRY")} — {Sozlukler.Tr.Islem.FarkMusteriOder}",
            e.Find("#takas-tuval-fark-tutar").TextContent.Trim()), Bekle);
        Assert.Contains("dg-fark-acik", e.Find("#takas-tuval-fark").ClassName ?? "", StringComparison.Ordinal);   // DEGISIM-2: açık fark UYARI rengi (iki modda tek anlam)
        Assert.Equal(Bicim.Para(6000m, "TRY"), e.Find("#takas-verdim-tutar").TextContent.Trim());
        Assert.Equal(Bicim.Para(1000m, "TRY"), e.Find("#takas-aldim-tutar").TextContent.Trim());
        Assert.Equal(DegisimGunSonuSozlugu.Tr.FarkOdemesi, e.Find("#takas-tuval-odeme-baslik").TextContent.Trim());
        Assert.Equal(DegisimGunSonuSozlugu.Tr.YeniFiyat, e.Find("#takas-yeni-fiyat-sar .alan-etiket-metin").TextContent.Trim());
        Assert.NotNull(e.Find("#takas-verdim #takas-yeni-fiyat"));
        Assert.Empty(e.FindAll("#takas-verdim-bos"));
    }

    [Fact]
    public void DG_T_PRO_degisim_AYNEN_tuval_YOK_eski_etiketler()
    {
        var e = Ekran(ArayuzModuServisi.Profesyonel);
        Assert.Empty(e.FindAll("#takas-tuval"));
        Assert.Equal(Sozlukler.Tr.Urun.GramAgirlik, Etiket(e, "takas-eski-gram"));
        Assert.Equal(Sozlukler.Tr.Islem.SaticiKimligi, e.Find("#takas-satici > legend").TextContent.Trim());
        Assert.Equal(ParaDemoSozlugu.Tr.TakasKimlikNotu, e.Find("#takas-satici-not").TextContent.Trim());
    }

    [Fact]
    public void DG_T_BASIT_kimlik_TEK_kutu_jargon_YOK_MASAK_acikken_satici_kumesi_cercevesiz()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        var d = DegisimGunSonuSozlugu.Tr;
        Assert.Equal(d.KimlikBaslikSade, e.Find("#takas-satici > legend").TextContent.Trim());
        Assert.Equal(d.KimlikNotuSade, e.Find("#takas-satici-not").TextContent.Trim());
        Assert.DoesNotContain("pusula", e.Find("#takas-satici").TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dg-kimlik-birlesik", e.Find("#takas-satici").ClassName ?? "", StringComparison.Ordinal);

        // MASAK kimlik kutusu açılınca (tutar eşik üstü) satıcı kümesi çerçevesiz: ekranda TEK kimlik kutusu.
        gereksinim = """{"kademe":"YALNIZ_KIMLIK","kimlikGerekli":true,"amacGerekli":false,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""";
        Doldur(e, 200000m);
        SeciciYardim.Sec(e, "takas-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#masak-belge-no", Bekle);
        e.WaitForAssertion(() => Assert.Contains("dg-kimlik-birlesik", e.Find("#takas-satici").ClassName ?? "", StringComparison.Ordinal), Bekle);
        Assert.Contains("gorsel-gizli", e.Find("#takas-satici > legend").ClassName ?? "", StringComparison.Ordinal);
        Assert.Contains("gorsel-gizli", e.Find("#takas-satici-not").ClassName ?? "", StringComparison.Ordinal);
        // Kural AYNEN: kimlik eksikken Kaydet kilitli.
        Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled"));
    }

    // ── tur 2 (tasarım hakemi KRİTİK): Peşin Müşteri adı satıcıya ön dolmaz; kilit tek adım ──
    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void DG_K_Pesin_Musteri_iken_satici_adi_ON_DOLMAZ_bos_ve_zorunlu(string mod)
    {
        var e = Ekran(mod);
        Doldur(e, 5000m, "pm1");
        SeciciYardim.Sec(e, "takas-satici-tip", "NIHAI_TUKETICI");
        e.WaitForElement("#takas-satici-ad", Bekle);
        Assert.True(string.IsNullOrEmpty(e.Find("#takas-satici-ad").GetAttribute("value")));
        Assert.DoesNotContain(Sozlukler.Tr.Islem.CaridenGeldi, e.Find("#takas-satici").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Peşin Müşteri", e.Find("#takas-satici").InnerHtml, StringComparison.Ordinal);
        // Kural AYNEN: ad soyad zorunlu → Kaydet kilitli, kilit adı söyler.
        Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled"));
        Assert.Contains(ParaDemo2Sozlugu.Tr.AlanAdSoyad, e.Find("#islem-kaydet").GetAttribute("title") ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void DG_K_gercek_musteri_adi_on_dolar_Pesine_donulunce_BOSALIR_elle_yazilan_EZILMEZ()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        Doldur(e, 5000m, "c1");
        SeciciYardim.Sec(e, "takas-satici-tip", "NIHAI_TUKETICI");
        e.WaitForAssertion(() => Assert.Equal("Tuna Akarsu", e.Find("#takas-satici-ad").GetAttribute("value")), Bekle);
        SeciciYardim.Sec(e, "islem-cari", "pm1");
        e.WaitForAssertion(() => Assert.True(string.IsNullOrEmpty(e.Find("#takas-satici-ad").GetAttribute("value"))), Bekle);
        // elle yazılan ad müşteri değişince korunur
        e.Find("#takas-satici-ad").Input("Ali Veli");
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForAssertion(() => Assert.Equal("Ali Veli", e.Find("#takas-satici-ad").GetAttribute("value")), Bekle);
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void DG_K_Pesin_ve_kimlik_esigi_ustu_kilit_TEK_adim_alan_SAYMAZ(string mod)
    {
        gereksinim = """{"kademe":"AMAC","kimlikGerekli":true,"amacGerekli":true,"beyanFormuGerekli":false,"ekBelgeGerekli":false,"asgariAciklamaUzunlugu":20}""";
        var e = Ekran(mod);
        Doldur(e, -200000m, "pm1");
        SeciciYardim.Sec(e, "takas-satici-tip", "NIHAI_TUKETICI");
        e.WaitForAssertion(() => Assert.Equal(DegisimGunSonuSozlugu.Tr.OnceMusteriSecin,
            e.Find("#islem-kaydet").GetAttribute("title")), Bekle);
        Assert.True(e.Find("#islem-kaydet").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#masak-belge-no"));   // kimlik kutusu ekranda yok → alanları da sayılmaz
        // gerçek müşteri seçilince alanlar TEKRARSIZ ve ekrandakiler sayılır
        SeciciYardim.Sec(e, "islem-cari", "c1");
        e.WaitForElement("#masak-belge-no", Bekle);
        e.WaitForAssertion(() =>
        {
            var kilit = e.Find("#islem-kaydet").GetAttribute("title") ?? "";
            Assert.NotEqual(DegisimGunSonuSozlugu.Tr.OnceMusteriSecin, kilit);
            Assert.DoesNotContain(ParaDemo2Sozlugu.Tr.AlanTckn + ",", kilit, StringComparison.Ordinal);   // satıcı "TCKN" + kutu "T.C. kimlik numarası" ikilisi yok
            var parca = kilit[(kilit.IndexOf(':') + 1)..].TrimEnd('.').Split(", ").Select(x => x.Trim()).ToList();
            Assert.Equal(parca.Count, parca.Distinct().Count());
        }, Bekle);
    }

    [Fact]
    public void DG_T_BASIT_Vazgec_degisim_fisini_bosaltir_istek_ATMAZ()
    {
        var e = Ekran(ArayuzModuServisi.Basit);
        Assert.Empty(e.FindAll("#islem-vazgec"));   // boş fişte Vazgeç yok (boşa tıklanan düğme doğmaz)
        Doldur(e, 5000m);
        e.WaitForElement("#islem-vazgec", Bekle);
        int postOnce; lock (istekler) postOnce = istekler.Count(i => i.Metot == "POST");
        e.Find("#islem-vazgec").Click();
        e.WaitForAssertion(() => Assert.Equal("—", e.Find("#takas-tuval-fark-tutar").TextContent.Trim()), Bekle);
        Assert.True(string.IsNullOrEmpty(e.Find("#takas-eski-gram").GetAttribute("value")));
        Assert.Empty(e.FindAll("#takas-yeni-secili"));
        Assert.Empty(e.FindAll("#islem-vazgec"));
        lock (istekler) Assert.Equal(postOnce, istekler.Count(i => i.Metot == "POST"));
    }

    // ═══ (B) üst bar başlığı ═══════════════════════════════════════════════════

    [Fact]
    public void DG_B_Degisim_secilince_sayfa_UST_BAR_basligini_Degisim_yazar_Satista_Satis()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Sahip", "t@ornek.com", "SAHIP", "s1", null, ArayuzModuServisi.Basit));
        var yuva = new Kuyumcu.Web.Layout.UstBarSekmeYuvasi();
        var kok = Render<Microsoft.AspNetCore.Components.CascadingValue<Kuyumcu.Web.Layout.UstBarSekmeYuvasi>>(p => p
            .Add(x => x.Value, yuva)
            .Add(x => x.ChildContent, (Microsoft.AspNetCore.Components.RenderFragment)(b =>
            {
                b.OpenComponent<Kuyumcu.Web.Pages.Islemler>(0);
                b.CloseComponent();
            })));
        kok.WaitForElement("#islem-tur-SATIS");
        var yol = new Uri(Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri).AbsolutePath;
        kok.WaitForAssertion(() => Assert.Equal(MenuAdlari.Icin(Sozlukler.Tr).Satis.Ad, yuva.BaslikIcin(yol)), Bekle);
        kok.Find("#islem-tur-TAKAS").Click();
        kok.WaitForAssertion(() => Assert.Equal(SatisTuvalSozlugu.Tr.KaroDegisim, yuva.BaslikIcin(yol)), Bekle);
        // Şerit başlığı aynı adı görsel gizli taşır (çift başlık yok), DOM'da kalır.
        Assert.Contains("sayfa-basi--baslik-ustte", kok.Find("#sayfa-basi-islemler").ClassName ?? "", StringComparison.Ordinal);
        Assert.Equal(SatisTuvalSozlugu.Tr.KaroDegisim, kok.Find("#sayfa-basi-islemler-baslik").TextContent.Trim());
        // Başka rotada eski başlık okunmaz.
        Assert.Null(yuva.BaslikIcin("/raporlar"));
    }
}

// ── (B) yuvanın saf sözleşmesi ─────────────────────────────────────────────
public sealed class DegisimUstBarYuvaTestleri
{
    [Fact]
    public void DG_B_yuva_basligi_yalniz_AYNI_rotada_sahibi_birakinca_kalkar()
    {
        var y = new Kuyumcu.Web.Layout.UstBarSekmeYuvasi();
        var sayfa = new object(); var baska = new object();
        var degisti = 0; y.Degisti += () => degisti++;
        y.BaslikAta(sayfa, "Değişim", "/islemler");
        Assert.Equal("Değişim", y.BaslikIcin("/islemler"));
        Assert.Equal("Değişim", y.BaslikIcin("/islemler/"));
        Assert.Null(y.BaslikIcin("/raporlar"));
        Assert.Null(y.Icerik);                         // sekme yuvası (kur şeridini gizleyen) DOLMAZ
        y.BaslikAta(sayfa, "Değişim", "/islemler");    // aynı değer → olay yok
        Assert.Equal(1, degisti);
        y.BaslikBirak(baska);                          // sahibi değil → kalmaz
        Assert.Equal("Değişim", y.BaslikIcin("/islemler"));
        y.BaslikBirak(sayfa);
        Assert.Null(y.BaslikIcin("/islemler"));
        y.BaslikAta(sayfa, "  ", "/islemler");
        Assert.Null(y.BaslikIcin("/islemler"));
    }
}

// ── (G) gün sonu ekranı ─────────────────────────────────────────────────────
public sealed class DegisimGunSonuEkranTestleri : BunitContext
{
    private string mutabakat = Mutabakat(fark: 27389.71m, satis: 27389.71m, faturasizSayi: 3, faturasizTl: 27389.71m,
        bagliTl: 0m, bagsizSayi: 0, bagsizTl: 0m, nakit: 12485.67m, kart: 0m, havale: 0m, satisFaturasi: 0m);

    /// <summary>Sunucu yanıtı (RaporModulu.PosFaturaMutabakati biçimi; sayılar InvariantCulture).</summary>
    internal static string Mutabakat(decimal fark, decimal satis, int faturasizSayi, decimal faturasizTl,
        decimal bagliTl, int bagsizSayi, decimal bagsizTl, decimal nakit, decimal kart, decimal havale, decimal satisFaturasi)
    {
        string S(decimal d) => d.ToString(CultureInfo.InvariantCulture);
        var toplam = nakit + kart + havale;
        var mutabik = fark == 0m && faturasizSayi == 0 && bagsizSayi == 0 ? "true" : "false";
        var bagsizListe = bagsizSayi == 0 ? "[]" : $$"""[{"id":"f1","faturaNo":"GIB2026000000001","tarih":"2026-10-03T09:00:00Z","toplam":{{S(bagsizTl)}},"cariId":null,"durum":"KESILDI"}]""";
        return $$"""
            {"tarih":"2026-10-03",
             "tahsilat":{"nakit":{{S(nakit)}},"kart":{{S(kart)}},"havale":{{S(havale)}},"diger":0,"toplam":{{S(toplam)}},"virmanGiris":0,"acilisDevri":0},
             "satis":{"sayisi":4,"toplamTl":{{S(satis)}},"faturaliSayisi":1,"faturaliTl":{{S(satis - faturasizTl)}},"faturasizSayisi":{{faturasizSayi}},"faturasizTl":{{S(faturasizTl)}}},
             "alis":{"sayisi":0,"toplamTl":0,"faturaSayisi":0,"faturaToplamTl":0},
             "hurda":{"sayisi":1,"odenenTl":500,"hasGrami":1,"pusulaSayisi":1,"pusulaBrutTl":500,"stopajToplamTl":0,"belgesizSayisi":0,"gecikmisPusulaSayisi":0},
             "fatura":{"sayisi":1,"toplamTl":{{S(satisFaturasi)}},"kdvToplam":0,"satisFaturasiTl":{{S(satisFaturasi)}}},
             "fark":{{S(fark)}},"mutabik":{{mutabik}},"uyarilar":[],
             "faturasizIslemler":[{"id":"h1","tarih":"2026-10-03T09:00:00Z","tlTutari":{{S(faturasizTl)}},"odemeTuru":"VERESIYE","cariId":"c1","odemeAraclari":null}],
             "faturaEslesme":{"bagliSayisi":1,"bagliTl":{{S(bagliTl)}},"bagimsizSayisi":{{bagsizSayi}},"bagimsizTl":{{S(bagsizTl)}},"faturasizTl":{{S(faturasizTl)}},"farkTanimi":"FARK = satış toplamı − fişe bağlı faturalar − fişe bağlanmamış faturalar","bagimsizFaturalar":{{bagsizListe}}
            """ + "}}";
    }

    public DegisimGunSonuEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<RaporServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(DegisimGunSonuEkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            return Task.FromResult(ParaDemoSahte.Tamam(yol switch
            {
                var y when y.EndsWith("/raporlar/katalog") => """{"toplam":0,"faz":"A","raporlar":[]}""",
                var y when y.EndsWith("/raporlar/aylik") => """{"yil":2026,"ay":10,"gunler":[]}""",
                var y when y.EndsWith("/raporlar/gunluk") => """{"tarih":"2026-10-03","satis":{"adet":4,"toplamTl":27389.71,"toplamHas":1.2},"alis":{"adet":0,"toplamTl":0,"toplamHas":0},"hurda":{"toplamHas":0,"odenenTl":0},"netHasGramHareketi":-1.2,"hareketSayisi":4,"kasalar":[{"id":"k1","ad":"Ana Kasa","bakiye":12485.67,"paraBirimi":"TRY"}]}""",
                var y when y.EndsWith("/kasalar/nakit-ayrimi") => """[{"kasaId":"k1","paraBirimi":"TRY","toplam":12485.67,"nakit":12485.67,"kart":0,"havale":0}]""",
                var y when y.EndsWith("/raporlar/pos-fatura-mutabakati") => t.mutabakat,
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                var y when y.EndsWith("/pozisyon") => """{"anaMaden":"ALTIN","altin":{"maden":"ALTIN","vitrinGram":10,"hurdaGram":0,"cariAlacakGram":0,"cariBorcGram":0,"netGram":10,"alacakliCariSayisi":0,"borcluOlduguCariSayisi":0,"konsinyeGram":0,"emanetGram":0},"digerMadenler":[],"madenlerToplanmaz":false,"defterBakiyeFarki":0,"hesaplanmaAni":"2026-10-03T09:00:00Z","donemdenBagimsiz":true}""",
                var y when y.Contains("/bildirimler") => """{"durum":"ACIK","limit":5,"azamiLimit":100,"kapaliGosterimGun":7,"dahaFazlasiVar":false,"bildirimler":[]}""",
                _ => "{}",
            }));
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Raporlar> Ciz(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Raporlar>();
        if (mod == ArayuzModuServisi.Profesyonel)
        {
            e.WaitForElement("#rp-sekme-gunsonu").Click();
            e.WaitForElement("#mut-kartlar");
        }
        else e.WaitForElement("#bugun-gun-sonu .t5-kirilim");
        return e;
    }

    private static List<(string Etiket, string Deger, string? Alt)> Kartlar(IRenderedComponent<Kuyumcu.Web.Pages.Raporlar> e) =>
        e.FindAll("#mut-kartlar .stat-kart").Select(k => (k.QuerySelector(".stat-etiket")!.TextContent.Trim(),
            k.QuerySelector(".stat-deger")!.TextContent.Trim(), k.QuerySelector(".stat-alt")?.TextContent.Trim())).ToList();

    private static decimal Tl(string metin) =>
        decimal.Parse(metin.Replace("₺", "").Replace("\u00A0", "").Replace('\u2212', '-').Trim(), NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"));

    [Fact]
    public void DG_G_BASIT_fark_satiri_ADIYLA_faturasi_kesilmemis_satis_tutar_m_Fark()
    {
        var e = Ciz(ArayuzModuServisi.Basit);
        var d = DegisimGunSonuSozlugu.Tr;
        Assert.Equal($"{d.FarkFaturasiz}: {Bicim.Tl(27389.71m)}", e.Find("#bugun-fark-satir").TextContent.Trim());
        Assert.Equal(Bicim.Tl(27389.71m), e.Find("#bugun-fark-tutar").TextContent.Trim());
        Assert.Equal(d.AciklamaFaturasiz, e.Find("#bugun-fark-aciklama").TextContent.Trim());
        Assert.DoesNotContain(RaporBugunSozlugu.Tr.FarkAciklama, e.Find("#bugun-gun-sonu").TextContent, StringComparison.Ordinal);   // "kasaya giren para" YOK
        Assert.Equal(string.Format(d.FaturasizAdet, 3), e.Find("#bugun-faturasiz .panel-liste-ad").TextContent.Trim());
    }

    [Fact]
    public void DG_G_PRO_kartlar_SATIS_eksi_FATURA_esittir_FARK_tahsilat_farkin_yaninda_DEGIL()
    {
        mutabakat = Mutabakat(fark: 1500m, satis: 10000m, faturasizSayi: 0, faturasizTl: 0m, bagliTl: 7000m,
            bagsizSayi: 1, bagsizTl: 1500m, nakit: 4000m, kart: 0m, havale: 0m, satisFaturasi: 9900m);
        // satisFaturasi (kiracı geneli 9.900) ≠ farka giren (bağlı 7.000 + bağsız 1.500 = 8.500): kart FARKA GİRENİ yazar.
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        var k = Kartlar(e);
        var d = DegisimGunSonuSozlugu.Tr;
        Assert.Equal([d.SatisToplami, Sozlukler.Tr.Mutabakat.SatisFaturasi, d.FarkGenel, Sozlukler.Tr.Mutabakat.Durum, Sozlukler.Tr.Mutabakat.Tahsilat],
            k.Select(x => x.Etiket).ToList());
        // Gözle sağlanan çıkarma: Satış toplamı − Satış faturası (farka giren: bağlı + bağsız) = Fark (sunucunun m.Fark'ı).
        Assert.Equal(Tl(k[2].Deger), Tl(k[0].Deger) - Tl(k[1].Deger));
        Assert.Equal(Bicim.TlKurus(1500m), k[2].Deger);
        Assert.Equal(Bicim.TlKurus(8500m), k[1].Deger);
    }

    [Fact]
    public void DG_G_PRO_faturasiz_fark_karti_ADI_ve_sifir_kalem_YOK_kalem_bolunmez()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        var k = Kartlar(e);
        var d = DegisimGunSonuSozlugu.Tr;
        Assert.Equal(d.FarkFaturasiz, k[2].Etiket);
        Assert.Equal(Tl(k[2].Deger), Tl(k[0].Deger) - Tl(k[1].Deger));
        var alt = k[4].Alt!;
        Assert.Equal(string.Format(Toplu5Sozlugu.Tr.KirilimNakit, Bicim.Tl(12485.67m)).Replace(' ', ' '), alt);
        Assert.DoesNotContain(Toplu5Sozlugu.Tr.KirilimHavale.Split(' ')[0], alt, StringComparison.Ordinal);   // "Havale 0 ₺" yok
        Assert.DoesNotContain(' ', alt);   // kalem içi bölünmez
    }

    [Fact]
    public void DG_G_PRO_fatura_satistan_FAZLA_ise_adi_soyler()
    {
        mutabakat = Mutabakat(fark: -800m, satis: 5000m, faturasizSayi: 0, faturasizTl: 0m, bagliTl: 5000m,
            bagsizSayi: 1, bagsizTl: 800m, nakit: 5000m, kart: 0m, havale: 0m, satisFaturasi: 5800m);
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        var k = Kartlar(e);
        Assert.Equal(DegisimGunSonuSozlugu.Tr.FarkFazlaFatura, k[2].Etiket);
        // tur 2 (doğrulayıcı DÜŞÜK "çift olumsuz"): ad yönü söyler, tutar eksi işaretsiz → fatura − satış = fazla
        Assert.Equal(800m, Tl(k[2].Deger));
        Assert.DoesNotContain("-", k[2].Deger, StringComparison.Ordinal);
        Assert.DoesNotContain("−", k[2].Deger, StringComparison.Ordinal);
        Assert.Equal(Tl(k[2].Deger), Tl(k[1].Deger) - Tl(k[0].Deger));
    }

    [Fact]
    public void DG_G_BASIT_fazla_fatura_tutari_ISARETSIZ_aciklama_TARIHSIZ()
    {
        mutabakat = Mutabakat(fark: -800m, satis: 5000m, faturasizSayi: 0, faturasizTl: 0m, bagliTl: 5000m,
            bagsizSayi: 1, bagsizTl: 800m, nakit: 5000m, kart: 0m, havale: 0m, satisFaturasi: 5800m);
        var e = Ciz(ArayuzModuServisi.Basit);
        var d = DegisimGunSonuSozlugu.Tr;
        Assert.Equal($"{d.FarkFazlaFatura}: {Bicim.TlKurus(800m)}", e.Find("#bugun-fark-satir").TextContent.Trim());
        Assert.Equal(d.AciklamaFazlaFatura, e.Find("#bugun-fark-aciklama").TextContent.Trim());
        Assert.DoesNotContain("Bugün", d.AciklamaFazlaFatura, StringComparison.Ordinal);   // PRO'da geçmiş gün seçilebilir
        Assert.DoesNotContain("today", DegisimGunSonuSozlugu.En.AciklamaFazlaFatura, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DG_G_PRO_ozet_tablosu_adet_TEK_kalip()
    {
        var e = Ciz(ArayuzModuServisi.Profesyonel);
        var hucreler = e.FindAll("#mut-ozet-tablo td.sayi").Select(h => h.TextContent.Trim()).ToList();
        Assert.NotEmpty(hucreler);
        Assert.All(hucreler, h => Assert.Matches(@"^\d+ adet · ", h));
        Assert.NotNull(e.Find("#mut-faturasiz-tablo"));
    }

    [Fact]
    public void DG_G_BASIT_mutabik_ise_fark_satiri_YOK()
    {
        mutabakat = Mutabakat(fark: 0m, satis: 5000m, faturasizSayi: 0, faturasizTl: 0m, bagliTl: 5000m,
            bagsizSayi: 0, bagsizTl: 0m, nakit: 5000m, kart: 0m, havale: 0m, satisFaturasi: 5000m);
        var e = Ciz(ArayuzModuServisi.Basit);
        Assert.NotNull(e.Find("#bugun-gun-sonu-tamam"));
        Assert.Empty(e.FindAll("#bugun-fark-satir"));
    }
}

// ── (N) sunucu: değişim farkının nakit sınırı (SatisModulu.Takas → tevsik.Dogrula |fark|) — kural DEĞİŞMEDİ,
//    sınır davranışı sabitlenir: tam hadde nakit serbest, 1 kuruş fazlası durur; Kart'la aynı fark geçer. ──
[Collection("veritabani")]
public sealed class DegisimTevsikSinirDbTestleri(TestVeritabani vt)
{
    private static Kuyumcu.Api.Modules.Satis.SatisServisi Satis(Kuyumcu.Api.Data.KuyumcuDbContext db) =>
        new(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<Kuyumcu.Api.Modules.Satis.SatisServisi>.Instance);

    [Theory]
    [InlineData("29999.99", "NAKIT", true)]
    [InlineData("30000", "NAKIT", true)]
    [InlineData("30000.01", "NAKIT", false)]
    [InlineData("30000.01", "KART", true)]
    public async Task DG_N_sunucu_takas_nakit_farki_sinirda(string farkMetin, string odeme, bool kabul)
    {
        var fark = decimal.Parse(farkMetin, CultureInfo.InvariantCulture);
        Kiraci k;
        using (var s = vt.OlusturSistem()) k = IslemBantFikstur.ReferansKur(s, TestKurulum.KiraciKur(s, $"DG takas {farkMetin} {odeme}"), 2_000m);
        using var db = vt.Olustur(k);
        var dto = new Kuyumcu.Api.Modules.Satis.TakasDto
        {
            CariId = k.CariId, EskiUrunAyarId = "K24", EskiUrunGram = 1m, EskiUrunBirimFiyatTlHasGram = 1_000m,
            YeniUrunId = k.UrunId, YeniUrunAdet = 1m, YeniUrunBirimFiyatTl = 1_000m + fark,
            OdemeTuru = odeme, KasaId = k.KasaId, KurSnapshotId = k.KurId,
            Satici = new Kuyumcu.Api.Modules.Hurda.HurdaSaticiDto
            { SaticiTipi = "NIHAI_TUKETICI", AdSoyad = "Deneme Vatandaş", Tckn = "10000000146", Adres = "Çarşı Mah. 1" },
        };
        if (kabul)
        {
            await Satis(db).Takas(dto, k.Islem);
            using var kontrol = vt.OlusturSistem();
            Assert.Single(kontrol.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId && h.Tur == "TAKAS"));
        }
        else
        {
            var hata = await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() => Satis(db).Takas(dto, k.Islem));
            Assert.Contains("nakit sınırı", hata.Message);
            using var kontrol = vt.OlusturSistem();
            Assert.Empty(kontrol.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId && h.Tur == "TAKAS"));
        }
    }
}

// ── (B) üst bar: sayfanın yazdığı başlık h1'de; kur şeridi ve mod anahtarı GİZLENMEZ ──
public sealed class DegisimUstBarEkranTestleri : BunitContext
{
    private sealed class SahteApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            return Task.FromResult(ParaDemoSahte.Tamam(yol switch
            {
                var y when y.Contains("/tercihler/benim") => """{"tercihler":{},"gorunumSubesi":{"kayitli":null,"subeId":null,"tumSubeler":true,"gecersizTercih":false,"deger":"TUMU"},"mesaj":"Tercihler kaydedildi"}""",
                var y when y.Contains("/yetkiler/benim") => """{"moduller":["cari","urun","islem","kasa","rapor"]}""",
                var y when y.EndsWith("/subeler") => """[{"id":"s0","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/bildirimler/sayi") => """{"sayi":0,"enYuksekOnem":"BILGI"}""",
                var y when y.EndsWith("/kur/guncel") => """{"id":"kur-1","tenantId":"t1","altinHasFiyati":6877,"gramAltinFiyati":6912.40,"usdTry":41.95,"eurTry":49.10,"gbpTry":56.30,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-10-03T09:02:00Z","ayarFiyatlari":[]}""",
                _ => """{"sonuclar":[],"limit":10,"dahaVar":false}""",
            }));
        }
    }

    public DegisimUstBarEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi()) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<MenuTercihServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    [Fact]
    public void DG_B_UstBar_h1_sayfanin_basligini_yazar_kur_seridi_ve_mod_anahtari_DURUR_sayfa_kapaninca_rota_adi()
    {
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/islemler");
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Ramazan", "Akkocan", "r@ornek.com", "SAHIP", "s0", null, ArayuzModuServisi.Basit));
        var yuva = new Kuyumcu.Web.Layout.UstBarSekmeYuvasi();
        var goster = true;
        void Parametre(ComponentParameterCollectionBuilder<Microsoft.AspNetCore.Components.CascadingValue<Kuyumcu.Web.Layout.UstBarSekmeYuvasi>> p) => p
            .Add(x => x.Value, yuva)
            .Add(x => x.ChildContent, (Microsoft.AspNetCore.Components.RenderFragment)(b =>
            {
                b.OpenComponent<Kuyumcu.Web.Layout.UstBar>(0);
                b.CloseComponent();
                if (goster)
                {
                    b.OpenComponent<Kuyumcu.Web.Layout.UstBarBasligi>(1);
                    b.AddAttribute(2, "Baslik", SatisTuvalSozlugu.Tr.KaroDegisim);
                    b.CloseComponent();
                }
            }));
        var kok = Render<Microsoft.AspNetCore.Components.CascadingValue<Kuyumcu.Web.Layout.UstBarSekmeYuvasi>>(Parametre);
        kok.WaitForAssertion(() => Assert.Equal(SatisTuvalSozlugu.Tr.KaroDegisim, kok.Find("header.ust-bar h1").TextContent.Trim()));
        Assert.NotNull(kok.Find("#ust-kur-serit"));
        Assert.NotNull(kok.Find("#ust-mod-anahtar"));
        Assert.Empty(kok.FindAll("#ust-sayfa-sekmeleri"));   // sekme yuvası dolmadı (kabuk kuralı tetiklenmez)

        goster = false;
        kok.Render(Parametre);
        kok.WaitForAssertion(() => Assert.Equal(MenuAdlari.Icin(Sozlukler.Tr).Satis.Ad, kok.Find("header.ust-bar h1").TextContent.Trim()));
    }
}

// ── CSS / bağ kilitleri (görünüm tarayıcıda ölçüldü — rapor § 3) ──
public sealed class DegisimCssTestleri
{
    private static string Kok => KilavuzUretimTestleri.DotnetKoku();
    private static string Css => File.ReadAllText(Path.Combine(Kok, "Kuyumcu.Web", "wwwroot", "css", "degisim-gunsonu.css"));

    [Fact]
    public void DG_C_css_toplu5ten_SONRA_olcekten_ONCE_baglanir()
    {
        var html = File.ReadAllText(Path.Combine(Kok, "Kuyumcu.Web", "wwwroot", "index.html"));
        var t5 = html.IndexOf("css/toplu5.css", StringComparison.Ordinal);
        var dg = html.IndexOf("css/degisim-gunsonu.css", StringComparison.Ordinal);
        var ol = html.IndexOf("css/olcek.css", StringComparison.Ordinal);
        Assert.True(t5 > 0 && dg > t5 && ol > dg, $"{t5} {dg} {ol}");
    }

    [Fact]
    public void DG_C_sabit_renk_YOK_ve_olculen_kurallar_yerinde()
    {
        var govde = System.Text.RegularExpressions.Regex.Replace(Css, @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgb\(|hsl\(", govde);
        Assert.DoesNotContain(".tuval-tezgah", govde, StringComparison.Ordinal);   // yüz sınıfıyla ölçü yazılmaz (OLCEK-1)
        Assert.Matches(@"#takas-verdim \.fis-arama-ad \{[^}]*white-space: normal", govde);
        Assert.Matches(@"#mut-faturasiz-tablo \{ width: 100%; \}", govde);
        Assert.Matches(@"#mut-ozet-tablo td:first-child,\s*#mut-faturasiz-tablo :is\(th, td\):first-child \{ padding-left: 1\.5rem; \}", govde);
        Assert.Matches(@"#takas-satici\.dg-kimlik-birlesik \{[^}]*border: 0", govde);
        // tur 2 (hakem KRİTİK 1280): ALDIM bilinçli 2+1 (Has fiyatı tam satır), birim eki kutunun yanında, arama tam genişlik
        Assert.Matches(@"\.dg-takas-yeni \{ grid-template-columns: repeat\(2, minmax\(0, 1fr\)\)", govde);
        Assert.Matches(@"\.dg-takas-eski > :nth-child\(3\) \{ grid-column: 1 / -1; \}", govde);
        Assert.Matches(@"#takas-tuval \.alan-girdi-satir \{ flex-wrap: nowrap; \}", govde);
        Assert.Matches(@"#takas-verdim \.barkod-satiri \{ margin-right: 0; \}", govde);
        Assert.DoesNotContain("minmax(120px", govde, StringComparison.Ordinal);
    }
}
