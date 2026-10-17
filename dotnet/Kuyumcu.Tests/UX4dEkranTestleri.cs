using System.Net;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-4d — EKRAN TARAFI (bUnit)
//   madde 10 Uyum: "Gönderildi"/"Kapat" GERÇEK istek atar (prompt kalktı)
//   madde 13 Cari: "Pasife Al" önce ONAY ister
//   madde 14 Kılavuz: iframe YOK, yeni sekme bağlantısı + bölüm listesi var
//   madde 18 Uyum: belge no araması 0 sonuçta ESKİ listeyi göstermez
//   madde 22 Denetim izi / kâr-zarar: aynı istek İKİ KEZ gitmez
//   madde 23 Ters tarih aralığında istek HİÇ atılmaz, alan altı mesaj çıkar
//   madde 24 Kur: sözleşmesiz kaynak uyarısı / dış kaynak kapalı şeridi
//   madde  1 Ürünler: açılış stoğu alanı var ve gövdede gider
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UX4dEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string kurKaynakYaniti =
        """{"kaynaklar":["ALTINKAYNAK"],"varsayilan":"ALTINKAYNAK","secili":"ALTINKAYNAK","sozlesmesizKaynaklarAcik":true,"seciliSozlesmesiz":true,"manuelKod":"MANUAL"}""";
    private string tespitListesi = "[]";
    private string aramaYaniti =
        """{"adet":0,"toplamTutarTl":0,"ilkIslem":null,"sonIslem":null,"kayitlar":[]}""";

    public UX4dEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(
            new SahteApi(istekler, () => kurKaynakYaniti, () => aramaYaniti, () => tespitListesi))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
    }

    private int Sayim(string parca) => istekler.Count(i => i.Yol.Contains(parca));

    // ── madde 14 ─────────────────────────────────────────────────────────────

    [Fact]
    public void M14_Kilavuz_sayfasinda_IFRAME_YOKTUR()
    {
        var c = Render<Kuyumcu.Web.Pages.Kilavuz>();
        Assert.Empty(c.FindAll("iframe"));
    }

    [Fact]
    public void M14_Kilavuz_yeni_sekmede_acilir_ve_bolum_baglantilari_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.Kilavuz>();

        var bag = c.Find("#kl-yeni-sekme");
        Assert.Equal("/kilavuz.html", bag.GetAttribute("href"));
        Assert.Equal("_blank", bag.GetAttribute("target"));
        Assert.Contains("noopener", bag.GetAttribute("rel"));

        Assert.NotEmpty(c.FindAll("#kl-bolumler a"));
        Assert.NotNull(c.Find("#kl-bolum-kur"));
    }

    [Fact]
    public void M14_Kilavuz_bos_DEGIL_aciklama_metni_gosterilir()
        => Assert.False(string.IsNullOrWhiteSpace(
            Render<Kuyumcu.Web.Pages.Kilavuz>().Find("#kl-aciklama").TextContent));

    // ── madde 24 ─────────────────────────────────────────────────────────────

    [Fact]
    public void M24_Sozlesmesiz_kaynak_seciliyken_PILOT_uyarisi_gorunur()
    {
        // SADE-1 doğrulama bulgusu: tam hukuki metin ROL'e bağlı — SAHİP görür.
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Sahip", "t@ornek.com", "SAHIP", "s1", null, "PROFESYONEL"));
        var c = Render<Kuyumcu.Web.Pages.Kur>().AyrintiAc();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-sozlesmesiz")));
        // PAKET SADE-1 (göz #25, kurucu kararı): günlük ekranda hukuki paragraf yerine
        // TEK satır "Deneme kur kaynağı" rozeti; tam "sözleşmesiz" metni SİLİNMEDİ —
        // rozetin ipucunda (title) ve profesyonel "Kaynak" sekmesinde durur.
        // TOPLU-3 (demo bulgusu): rozet dili "Deneme" yerine sade "Ücretsiz kur kaynağı"; koşul ve ipucu aynı.
        Assert.Contains(Kuyumcu.Core.I18n.Toplu3Sozlugu.Tr.KurUcretsizRozet, c.Find("#kur-sozlesmesiz").TextContent);
        Assert.DoesNotContain("Deneme", c.Find("#kur-sozlesmesiz").TextContent);
        Assert.Contains("sözleşmesiz", c.Find("#kur-sozlesmesiz").GetAttribute("title"));
        Assert.Empty(c.FindAll("#kur-dis-kapali"));
    }

    [Fact]
    public void M24_Bayrak_kapaliyken_DIS_KAYNAK_KAPALI_seridi_ve_manuel_yol_yazar()
    {
        kurKaynakYaniti =
            """{"kaynaklar":[],"varsayilan":"MANUAL","secili":"MANUAL","sozlesmesizKaynaklarAcik":false,"seciliSozlesmesiz":false,"manuelKod":"MANUAL"}""";

        var c = Render<Kuyumcu.Web.Pages.Kur>().AyrintiAc();
        c.WaitForAssertion(() => Assert.NotNull(c.Find("#kur-dis-kapali")));
        Assert.Contains("manuel", c.Find("#kur-dis-kapali").TextContent);
        Assert.Empty(c.FindAll("#kur-sozlesmesiz"));
    }

    // ── madde 18 ─────────────────────────────────────────────────────────────

    [Fact]
    public void M18_Belge_no_aramasi_0_sonucta_BOS_DURUM_gosterir()
    {
        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        c.WaitForState(() => c.FindAll("#ara-belge").Count > 0);

        c.Find("#ara-belge").Input("99999999999");

        c.WaitForAssertion(() => Assert.Contains("bulunamadı", c.Markup));
        // Sonuç özeti de basılır: "0 kayıt"
        Assert.Contains("0", c.Find("#uy-arama-ozet").TextContent);
    }

    [Fact]
    public void M18_Arama_ucu_GERCEKTEN_cagrilir()
    {
        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        c.WaitForState(() => c.FindAll("#ara-belge").Count > 0);
        c.Find("#ara-belge").Input("10000000146");
        c.WaitForAssertion(() => Assert.True(Sayim("kimlik-tespitleri/ara") >= 1));
    }

    [Fact]
    public void M18_Dolu_listede_arama_0_sonuc_verirse_ESKI_liste_EKRANDAN_KALKAR()
    {
        tespitListesi = """
            [{"id":"t1","hareketId":null,"cariId":null,"islemTutariTl":250000,
              "belgeTuru":"TC_KIMLIK","belgeNo":"*******0146","adSoyad":"Ayşe Demir",
              "tarih":"2026-09-16T09:00:00Z","saklamaBitis":"2034-09-16T09:00:00Z",
              "tespitEdenKullaniciId":"u1"}]
            """;

        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        c.WaitForAssertion(() => Assert.Contains("Ayşe Demir", c.Markup));

        c.Find("#ara-belge").Input("99999999999");   // sunucu 0 kayıt döner

        c.WaitForAssertion(() => Assert.DoesNotContain("Ayşe Demir", c.Markup));
        Assert.Contains("bulunamadı", c.Markup);
    }

    // ── madde 10 ─────────────────────────────────────────────────────────────

    [Fact]
    public void M10_Gonderildi_dugmesi_ONAY_modali_acar_ve_ISTEK_atar()
    {
        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        SupheliSekmesineGec(c);

        c.Find("#uy-gonderildi-sb1").Click();
        c.Find("#uy-referans").Input("MASAK-2026-0001");
        c.Find("#uy-gonderim-onayla").Click();

        c.WaitForAssertion(() => Assert.True(Sayim("supheli/sb1/gonderildi") == 1));
    }

    [Fact]
    public void M10_Referans_bos_birakilirsa_ALAN_ALTI_Turkce_mesaj_cikar_ve_istek_ATILMAZ()
    {
        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        SupheliSekmesineGec(c);

        c.Find("#uy-gonderildi-sb1").Click();
        c.Find("#uy-gonderim-onayla").Click();

        Assert.Contains("zorunlu", c.Find("#uy-referans-hata").TextContent);
        Assert.Equal(0, Sayim("gonderildi"));
    }

    [Fact]
    public void M10_Kapat_dugmesi_20_karakterden_kisa_gerekcede_istek_ATMAZ()
    {
        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        SupheliSekmesineGec(c);

        c.Find("#uy-kapat-sb1").Click();
        c.Find("#uy-kapatma-gerekce").Input("kısa");
        c.Find("#uy-kapatma-onayla").Click();

        Assert.Contains("en az 20", c.Find("#uy-kapatma-hata").TextContent);
        Assert.Equal(0, Sayim("/kapat"));
    }

    [Fact]
    public void M10_Kapat_gerekce_yeterliyse_ISTEK_atar()
    {
        var c = Render<Kuyumcu.Web.Pages.Uyum>();
        SupheliSekmesineGec(c);

        c.Find("#uy-kapat-sb1").Click();
        c.Find("#uy-kapatma-gerekce").Input(new string('a', 25));
        c.Find("#uy-kapatma-onayla").Click();

        c.WaitForAssertion(() => Assert.True(Sayim("supheli/sb1/kapat") == 1));
    }

    private void SupheliSekmesineGec(IRenderedComponent<Kuyumcu.Web.Pages.Uyum> c)
    {
        c.WaitForState(() => c.FindAll("[role=tab]").Count >= 2);
        c.FindAll("[role=tab]")[1].Click();
        c.WaitForState(() => c.FindAll("#uy-gonderildi-sb1").Count > 0);
    }

    // ── madde 22 ve 23 ───────────────────────────────────────────────────────

    [Fact]
    public void M22_Denetim_izinde_AYNI_filtre_ikinci_kez_istek_ATMAZ()
    {
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.WaitForState(() => Sayim("/denetim") > 0);
        c.Find("#dz-suzgec-ac").Click();   // ZO-BELGE: süzgeç KAPALI katlanırda (istek atmaz)
        var ilk = Sayim("/denetim");

        // Aynı değer yeniden seçildi (tarayıcıda tek eylemin iki kez tetiklenmesi).
        SeciciYardim.Sec(c, "f-eylem", "");
        SeciciYardim.Sec(c, "f-eylem", "");

        Assert.Equal(ilk, Sayim("/denetim"));
    }


    [Fact]
    public void M23_Denetim_izinde_TERS_aralik_istek_ATMAZ_ve_alan_alti_mesaj_cikar()
    {
        var c = Render<Kuyumcu.Web.Pages.DenetimIzi>();
        c.WaitForState(() => Sayim("/denetim") > 0);
        c.Find("#dz-suzgec-ac").Click();   // ZO-BELGE: süzgeç KAPALI katlanırda

        c.Find("#f-baslangic").Change("2026-09-16");
        var oncesi = Sayim("/denetim");
        c.Find("#f-bitis").Change("2026-09-01");

        Assert.Equal(oncesi, Sayim("/denetim"));
        // PAKET GOC-D: ekranın çıplak cümlesi yerine sunucuyla TEK kural + sözlük metni (Core RaporDonemKurali → Kabuk.TarihAraligi).
        Assert.Contains(Kuyumcu.Core.I18n.Sozlukler.Tr.Kabuk.TarihAraligi, c.Find("#f-aralik-hata").TextContent);
    }

    [Fact]
    public void M23_Kar_zararda_TERS_aralik_istek_ATMAZ()
    {
        var c = Render<Kuyumcu.Web.Pages.KarZarar>();
        c.WaitForState(() => Sayim("kar-zarar") > 0);

        c.Find("#kz-bas").Change("2026-09-16");
        var oncesi = Sayim("kar-zarar");
        c.Find("#kz-bit").Change("2026-09-01");

        Assert.Equal(oncesi, Sayim("kar-zarar"));
        Assert.Contains("Bitiş tarihi", c.Find("#kz-aralik-hata").TextContent);
    }

    // ── madde 1 ──────────────────────────────────────────────────────────────

    [Fact]
    public void M1_Urun_kartinda_ACILIS_STOGU_alani_ve_DegismezIpucu_vardir()
    {
        var c = Render<Kuyumcu.Web.Pages.Urunler>().AyrintiAc();
        c.WaitForState(() => c.FindAll("button").Any(b => b.TextContent.Contains("Yeni")));
        c.FindAll("button").First(b => b.TextContent.Contains("Yeni")).Click();

        Assert.NotNull(c.Find("#u-acilis-stok"));
        var ipucu = c.Find("#u-acilis-stok-ipucu").TextContent;
        Assert.Contains("alış", ipucu);
        Assert.Contains("sayım", ipucu);
    }

    private sealed class SahteApi(
        List<(string, string, string)> kayit,
        Func<string> kurKaynak,
        Func<string> arama,
        Func<string> tespitler) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var tam = istek.RequestUri!.PathAndQuery;
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, tam, govde));

            var g = yol switch
            {
                var y when y.EndsWith("/kur/kaynaklar") => kurKaynak(),
                var y when y.Contains("kimlik-tespitleri/ara") => arama(),
                var y when y.EndsWith("/masak/kimlik-tespitleri") => tespitler(),
                var y when y.EndsWith("/masak/supheli") => """
                    [{"id":"sb1","hareketId":null,"cariId":null,"kimlikTespitiId":null,
                      "islemTutariTl":250000,"gerekce":"Gerekçe metni",
                      "tespitTarihi":"2026-09-16T09:00:00Z","sonBildirimTarihi":"2026-09-30T09:00:00Z",
                      "kalanSaat":200,"durum":"ACIK","gonderimTarihi":null,"gonderimReferansi":null,
                      "kapatmaGerekcesi":null,"bildirenKullaniciId":"u1",
                      "saklamaBitis":"2034-09-16T09:00:00Z","olusturuldu":"2026-09-16T09:00:00Z",
                      "guncellendi":"2026-09-16T09:00:00Z"}]
                    """,
                var y when y.EndsWith("/masak/ozet") =>
                    """{"acikBildirim":1,"suresiGecmis":0,"yaklasan":1,"son30GunTespit":0}""",
                var y when y.EndsWith("/masak/saklama-onizleme") =>
                    """{"kimlikTespiti":0,"beyan":0,"enYakinSaklamaBitisi":null}""",
                var y when y.EndsWith("/denetim/ozet") =>
                    """{"toplam":0,"hassasAdet":0,"enEskiKayit":null,"eylemler":[],"turler":[]}""",
                var y when y.Contains("/denetim") =>
                    """{"toplam":0,"limit":50,"offset":0,"kayitlar":[]}""",
                var y when y.Contains("kar-zarar/urun") => """{"satirlar":[]}""",
                var y when y.Contains("kar-zarar") =>
                    """{"baslangic":"2026-09-01","bitis":"2026-09-16","satisAdedi":0,"hasilatTl":0,"maliyetTl":0,"karTl":0,"hasilatHas":0,"maliyetHas":0,"karHas":0,"maliyetiBilinenAdet":0,"maliyetiBilinmeyenAdet":0,"kuruBulunamayanSatis":0,"guvenilirlik":"TAM","uyari":null}""",
                var y when y.Contains("stok-degerlemesi") =>
                    """{"toplamMaliyetTl":0,"toplamHas":0,"satirlar":[]}""",
                var y when y.EndsWith("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":6850,"usdTry":34.0,"eurTry":37.0,"gbpTry":43.5,"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-16T09:00:00Z","ayarFiyatlari":[]}""",
                var y when y.Contains("/kur/") => "[]",
                var y when y.EndsWith("/urunler") => "[]",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var y when y.EndsWith("/reyonlar") => "[]",
                var y when y.Contains("/urun-kategorileri") => "[]",
                var y when y.Contains("/cari") => "[]",
                // P-10: Urunler ekrani parite kodlarini TEK KAYNAKTAN okur (UX-S31).
                var p10p when p10p.Contains("/kur/pariteler") =>
                    """{"kaynak":"TEST","pariteler":[{"kod":"BILEZIK_22","ad":"22 Ayar Bilezik","alis":6800,"satis":6900,"birim":"GRAM"}]}""",
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }
}
