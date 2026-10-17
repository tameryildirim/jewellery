using Bunit;
using Kuyumcu.Web.Layout;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// VERİ İÇE AKTARIM EKRANI — paket 47 Faz A [bUnit]
//
// "Düğme var" yetmez: hangi uca hangi gövdeyle gidildiği ve KİLİTLİ düğmenin
// sebebini SÖYLEYİP söylemediği ölçülür (DoD/1 + UX-S30).
// ⚠ Her olay sonrası bekleme WaitForAssertion ile (sabit uyku YASAK).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class IceAktarimEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string Sablonlar = """
        {"onayMetni":"VERİLERİ İÇE AKTARMAYI ONAYLIYORUM","maksSatir":5000,
         "maksDosyaBayt":2097152,"gerekceAsgari":20,"politikalar":["HATA","ATLA"],
         "kaynaklar":[
           {"ad":"cari","kolonlar":[{"ad":"tur","zorunlu":true,"ornek":"MUSTERI"},
                                     {"ad":"ad","zorunlu":true,"ornek":"Ayşe"}]},
           {"ad":"urun","kolonlar":[{"ad":"kod","zorunlu":true,"ornek":"BLZ-1"}]}]}
        """;

    private const string ProvaYaniti = """
        {"oturumId":"o1","kaynak":"cari","durum":"PROVA","yinelenenPolitikasi":"HATA",
         "toplamSatir":3,"gecerliSatir":2,"hataliSatir":1,"atlananSatir":0,
         "yazilanKayit":0,"onayMetni":"VERİLERİ İÇE AKTARMAYI ONAYLIYORUM",
         "bilinmeyenKolonlar":["eskiProgramId"],
         "ilkHatalar":[{"satirNo":4,"hataKodu":"TAS_KOKENI","hataMesaji":"Kıymetli taş içeren ürünün kökeni belirtilmelidir"}]}
        """;

    private const string GecmisYaniti = """
        {"toplam":1,"limit":20,"kayitlar":[
          {"id":"o0","kullaniciId":"u1","kaynakAd":"cari","dosyaAdi":"eski.csv",
           "gerekce":"Eski programdan müşteri kartları taşınıyor","durum":"PROVA",
           "yinelenenPolitikasi":"HATA","toplamSatir":3,"gecerliSatir":3,"hataliSatir":0,
           "atlananSatir":0,"uygulananSatir":0,"olusturuldu":"2026-09-17T08:00:00Z",
           "uygulandi":null,"geriAlindi":null}]}
        """;

    public IceAktarimEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        // P-36b kapanışı (P-37 artığı): KenarCubugu favori/sıra tercihini bu
        // servisten okur; Program.cs'te kayıtlıydı, fikstürlerde DEĞİLDİ →
        // kabuk çizen her test DI hatasıyla kırmızıydı.
        Services.AddScoped<MenuTercihServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            if (yol.EndsWith(".csv"))
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent("tur,ad\r\n"u8.ToArray()),
                };

            var g = yol switch
            {
                var y when y.EndsWith("/ice-aktarim/sablonlar") => Sablonlar,
                var y when y.Contains("/ice-aktarim/gecmis") => GecmisYaniti,
                var y when y.EndsWith("/ice-aktarim/prova") => ProvaYaniti,
                var y when y.EndsWith("/uygula") =>
                    """{"oturumId":"o1","durum":"UYGULANDI","uygulananSatir":2,"atlananSatir":0,"hataliSatir":1,"geriAlinabilir":true}""",
                var y when y.EndsWith("/geri-al") =>
                    """{"oturumId":"o1","durum":"GERI_ALINDI","pasifeAlinan":2,"mesaj":"Pasife alındı"}""",
                var y when y.Contains("/yetkiler/benim") =>
                    """{"moduller":["cari","urun","islem","kasa","rapor"]}""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.VeriIceAktarim> Ciz()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.VeriIceAktarim>();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ia-prova")));
        istekler.Clear();
        return e;
    }

    // GOC-47B: ekran artık dosyanın BAŞLIK satırını okur (sütun eşleştirme); zorunlu kolonları
    // (tur, ad) taşımayan dosyada prova kilitlenir. Fikstür gerçek bir başlık satırıyla başlar.
    private static InputFileContent Csv(int boyut = 64) =>
        InputFileContent.CreateFromText("tur,ad\n" + new string('a', Math.Max(0, boyut - 7)), "liste.csv");

    private static void GerekceYaz(IRenderedComponent<Kuyumcu.Web.Pages.VeriIceAktarim> e) =>
        e.Find("#ia-gerekce").Input("Eski programdan müşteri kartları taşınıyor");

    // ── PAKET CUMA-J (demo taraması /veri-ice-aktarim K8: "Müşteri / cari kartları") ──
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CJ_07_Kaynak_tur_etiketi_BASIT_modda_cari_demez_PRO_metni_korunur(bool basit)
    {
        Services.GetRequiredService<DilServisi>().BasitModuBagla(() => basit);
        var e = Ciz();
        e.Find("#ia-gecmis-ac").Click();   // ZO-BELGE: geçmiş KAPALI katlanırda
        e.WaitForAssertion(() => Assert.Contains("eski.csv", e.Markup));
        var satir = e.FindAll("tr").First(t => t.TextContent.Contains("eski.csv"));
        var tur = satir.QuerySelectorAll("td")[1].TextContent.Trim();
        if (basit)
        {
            Assert.Equal("Müşteri kartları", tur);
            Assert.DoesNotMatch(CumaJJargon.Re, tur);
        }
        else
            Assert.Equal(Kuyumcu.Core.I18n.Sozlukler.Tr.IceAktarim.KaynakCari, tur);
    }

    [Fact]
    public void CJ_12_Tek_baslik_tek_etiket_ve_kart_govdesi()
    {
        // 4. tur (doğrulayıcı DÜŞÜK): SayfaBasi + sayfa-baslik h1 çift başlık;
        // "Ne aktarılacak" üç, "Gerekçe" iki kez görünüyordu; kart içeriği kenara yapışıktı.
        var e = Ciz();
        var d = Services.GetRequiredService<DilServisi>().T;
        Assert.Empty(e.FindAll(".sayfa-baslik"));
        // ZO-BELGE 28.09 (UX-S31): başlık menü adıyla aynı (MenuAdlari tek kaynak).
        Assert.Equal(1, e.FindAll("h1, h2, .sayfa-basi-baslik").Count(h => h.TextContent.Trim() == Kuyumcu.Core.I18n.MenuAdlari.Tr.VeriIceAktar.Ad));
        // ZO-BELGE 29.09 devam 9 (hakem k3 · K17 "açıklama metni satırı 0"): başlık altında açıklama YOK.
        Assert.Empty(e.FindAll("#sayfa-basi-veri-ice-aktarim-aciklama"));
        Assert.Equal(0, e.FindAll("p").Count(x => x.TextContent.Trim() == Kuyumcu.Core.I18n.ZoBelgeSozlugu.Tr.IceAciklama));
        Assert.Equal(1, e.FindAll("label, .alan-etiket, h2").Count(x => x.TextContent.Trim() == d.IceAktarim.Kaynak));
        Assert.Equal(1, e.FindAll("label, .alan-etiket").Count(x => x.TextContent.Trim().StartsWith(d.DisaAktarim.Gerekce)));
        Assert.Equal("Dosyayı hazırlayın", e.Find("#ia-adim1-baslik").TextContent.Trim());
        foreach (var kart in e.FindAll(".kart"))
            Assert.NotNull(kart.QuerySelector(":scope > .kart-govde"));
    }

    // ── Kilitli düğme SEBEBİNİ SÖYLER (UX-S30 + DoD/1) ───────────────────────

    [Fact]
    public void Dosya_ve_gerekce_yokken_PROVA_kilitli_ve_sebep_YAZAR()
    {
        var e = Ciz();

        Assert.True(e.Find("#ia-prova").HasAttribute("disabled"));
        Assert.NotNull(e.Find("#ia-kilit-sebep"));
        Assert.Empty(istekler);                       // sessiz etkisizlik yok, istek de yok
    }

    [Fact]
    public void Dosya_ve_gerekce_verilince_PROVA_acilir_ve_UCA_gider()
    {
        var e = Ciz();
        GerekceYaz(e);
        e.FindComponent<InputFile>().UploadFiles(Csv());

        e.WaitForAssertion(() => Assert.False(e.Find("#ia-prova").HasAttribute("disabled")));

        e.Find("#ia-prova").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "POST" && i.Yol.EndsWith("/ice-aktarim/prova")));
        var govde = istekler.First(i => i.Yol.EndsWith("/ice-aktarim/prova")).Govde;
        Assert.Contains("\"kaynak\":\"cari\"", govde);
        Assert.Contains("\"yinelenenPolitikasi\":\"HATA\"", govde);
        Assert.Contains("icerikBase64", govde);
    }

    // ── Kuru prova sonucu ────────────────────────────────────────────────────

    [Fact]
    public void Prova_sonucu_hata_tablosunda_satir_no_ve_mesaj_gorunur()
    {
        var e = Ciz();
        GerekceYaz(e);
        e.FindComponent<InputFile>().UploadFiles(Csv());
        e.WaitForAssertion(() => Assert.False(e.Find("#ia-prova").HasAttribute("disabled")));
        e.Find("#ia-prova").Click();

        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ia-hata-tablosu")));
        var tablo = e.Find("#ia-hata-tablosu").TextContent;
        Assert.Contains("4", tablo);                          // satır no
        Assert.Contains("TAS_KOKENI", tablo);
        Assert.Contains("kökeni", tablo);

        // Kuru prova kanıtı EKRANDA: sunucunun döndürdüğü "yazılan kayıt = 0".
        Assert.Contains("0", e.Find("#ia-yazilan").TextContent);
        // Tanınmayan kolon HATA değil UYARI'dır ve ekranda yazar.
        Assert.Contains("eskiProgramId", e.Find("#ia-bilinmeyen").TextContent);
    }

    [Fact]
    public void Onay_kutusu_yazilmadan_UYGULA_pasif_ve_istek_GITMEZ()
    {
        var e = Ciz();
        GerekceYaz(e);
        e.FindComponent<InputFile>().UploadFiles(Csv());
        e.WaitForAssertion(() => Assert.False(e.Find("#ia-prova").HasAttribute("disabled")));
        e.Find("#ia-prova").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ia-uygula")));

        Assert.True(e.Find("#ia-uygula").HasAttribute("disabled"));
        Assert.NotNull(e.Find("#ia-uygula-kilit"));

        istekler.Clear();
        e.Find("#ia-onay").Input("yanlış metin");
        e.WaitForAssertion(() => Assert.True(e.Find("#ia-uygula").HasAttribute("disabled")));

        e.Find("#ia-onay").Input("VERİLERİ İÇE AKTARMAYI ONAYLIYORUM");
        e.WaitForAssertion(() => Assert.False(e.Find("#ia-uygula").HasAttribute("disabled")));

        e.Find("#ia-uygula").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/uygula")));
    }

    [Fact]
    public void Uygulandiktan_sonra_GERI_AL_gorunur_ve_ONAY_ISTER()
    {
        var e = Ciz();
        GerekceYaz(e);
        e.FindComponent<InputFile>().UploadFiles(Csv());
        e.WaitForAssertion(() => Assert.False(e.Find("#ia-prova").HasAttribute("disabled")));
        e.Find("#ia-prova").Click();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ia-onay")));
        e.Find("#ia-onay").Input("VERİLERİ İÇE AKTARMAYI ONAYLIYORUM");
        e.WaitForAssertion(() => Assert.False(e.Find("#ia-uygula").HasAttribute("disabled")));
        e.Find("#ia-uygula").Click();

        // UX-S27: sonuç bandında sunucunun döndürdüğü sayı yazar.
        e.WaitForAssertion(() => Assert.Contains("2", e.Find("#ia-basari").TextContent));
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ia-geri-al")));

        istekler.Clear();
        e.Find("#ia-geri-al").Click();
        // Yıkıcı eylem ÖNCE onay ister — tıklama tek başına istek ATMAZ (DoD/4).
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#ia-onay-evet")));
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/geri-al"));

        e.Find("#ia-onay-evet").Click();
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Yol.EndsWith("/geri-al")));
    }

    // ── Geçmiş + düzenleme yolu (UX-S25) ─────────────────────────────────────

    [Fact]
    public void Gecmisteki_PROVA_oturumunun_gerekcesi_SATIR_ICI_duzenlenir_PATCH_gider()
    {
        var e = Ciz();
        e.Find("#ia-gecmis-ac").Click();   // ZO-BELGE: geçmiş KAPALI katlanırda
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#si-iceaktarim-gerekce-o0-ac")));

        e.Find("#si-iceaktarim-gerekce-o0-ac").Click();
        e.Find("#si-iceaktarim-gerekce-o0").Input("Mali müşavir listesinden düzeltilmiş kart göçü");
        e.Find("#si-iceaktarim-gerekce-o0-kaydet").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Metot == "PATCH" && i.Yol.EndsWith("/ice-aktarim/o0")));
    }

    [Fact]
    public void Sablon_indirme_dugmesi_SUNUCUYA_gider()
    {
        var e = Ciz();
        e.Find("#ia-sablon-indir").Click();

        e.WaitForAssertion(() => Assert.Contains(istekler,
            i => i.Yol.EndsWith("/ice-aktarim/sablon/cari.csv")));
    }

    [Fact]
    public void Kapsam_notu_gecmis_hareketin_AKTARILMADIGINI_yazar()
    {
        var e = Ciz();
        // ZO-BELGE 29.09 devam 9: not "Dosyayı nasıl hazırlarım?" katlanırında (açılışta açıklama satırı yok).
        e.Find("#ia-nasil-ac").Click();
        // ZO-BELGE (göz #16): kural YAZILI kalır ama "MASAK/tevsik" jargonu olmadan.
        var not = e.Find("#ia-kapsam-notu").TextContent;
        Assert.Contains("aktarılmaz", not);
        Assert.Contains("kasa hareketleri", not);
        Assert.DoesNotContain("MASAK", not);
    }

    // ── Menü kapısı ──────────────────────────────────────────────────────────

    // TEMA TEMİZLİĞİ 17.10: kenar çubuğu kaldırıldı; menünün bütünü "Tüm ekranlar" sayfasıdır
    // (üst menünün son sözcüğü). Menü kapısı orada sınanır.
    private List<string> TumEkranYollari(OturumKullanici k)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(k);
        var e = Render<Kuyumcu.Web.Pages.Ekranlar>().KareleriBekle();
        return e.KareYollari();
    }

    [Fact]
    public void SAHIP_olmayan_kullanicida_menu_baglantisi_YOK()
    {
        var yollar = TumEkranYollari(new OturumKullanici("u2", "Müdür", "Kullanıcı", "m@ornek.com", "MUDUR", "s1"));

        Assert.Contains("/", yollar);   // sayfa gerçekten çizildi (boş listede yanlış yeşil yok)
        Assert.DoesNotContain("/veri-ice-aktarim", yollar);
    }

    [Fact]
    public void SAHIP_kullanicida_menu_baglantisi_VAR()
    {
        var yollar = TumEkranYollari(new OturumKullanici("u1", "Sahip", "Kullanıcı", "s@ornek.com", "SAHIP", "s1"));

        Assert.Contains("/veri-ice-aktarim", yollar);
    }
}
