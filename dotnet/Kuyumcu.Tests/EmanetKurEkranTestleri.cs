using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// EMANET EKRANI
//
// Modülün ÜÇ UCU DA sunucuda çalışıyordu, ekranı hiç yoktu: kuyumcunun en
// temel işlerinden biri — müşteri altınını teslim almak — üründen yapılamıyordu.
//
// Sınanan kapılar:
//   • kurSnapshotId ZORUNLU — kur alınamazsa kayıt yapılamaz
//   • iade elimizdeki emanetten fazla olamaz
//   • emanetin işletmenin malı olmadığı ekranda yazar
// ═════════════════════════════════════════════════════════════════════════════

public sealed class EmanetEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool kurDusuk;

    public EmanetEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => kurDusuk))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(
        List<(string, string, string)> kayit, Func<bool> kurDusukMu) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            if (yol.Contains("/kur/guncel") && kurDusukMu())
                return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("""{"message":"kaynak yok"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            var g = yol switch
            {
                var y when y.Contains("/kur/guncel") => """{"id":"kur-9","altinHasFiyati":6800}""",
                var y when y.Contains("/emanet/") => """
                    {"cariId":"c1","mevcutHasGramBakiye":12.5,
                     "hareketler":[{"id":"h1","tur":"EMANET_GIRIS","tarih":"2026-09-10T10:00:00Z",
                                    "hasGrami":12.5,"aciklama":"Bilezik 2 adet"}]}
                    """,
                var y when y.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    private static AngleSharp.Dom.IElement Dugme(
        IRenderedComponent<Kuyumcu.Web.Pages.Emanet> e, string metin)
        // PAKET EMANET-1: düğme metinleri "Emanet al" / "Emanet iade et" oldu;
        // eylem düğmeleri artık KİMLİKLE bulunur (id'ler değişmedi).
        => metin switch
        {
            "Emanet Al" => e.Find("#e-giris-ac"),
            "İade Et" => e.Find("#e-cikis-ac"),
            _ => e.FindAll("button").First(b => b.TextContent.Contains(metin)),
        };

    private IRenderedComponent<Kuyumcu.Web.Pages.Emanet> MusteriSecili()
    {
        // TEMA TEMİZLİĞİ 17.10: Yıldız yolu — listede müşterinin satırına bas (özgün gövde), seçicide c1.
        var e = Render<Kuyumcu.Web.Pages.Emanet>().EmanetSatiriAc("h1");
        SeciciYardim.Sec(e, "e-cari", "c1");
        istekler.Clear();
        return e;
    }

    [Fact]
    public void Musteri_secilince_emanet_gecmisi_cekilir()
    {
        // TEMA TEMİZLİĞİ 17.10: Yıldız'da müşteri, listedeki satırına basılarak seçilir (özgün gövde o müşteriyle
        // açılır); seçiciden yeniden seçim de geçmişi yeniden çeker.
        var e = Render<Kuyumcu.Web.Pages.Emanet>();
        e.WaitForElement("#yildiz-emanet-musteri-h1", TimeSpan.FromSeconds(5));
        istekler.Clear();
        e.EmanetSatiriAc("h1");
        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/emanet/c1"));

        istekler.Clear();
        SeciciYardim.Sec(e, "e-cari", "c1");
        e.WaitForAssertion(() => Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/emanet/c1")));
    }

    [Fact]
    public void Elimizdeki_emanet_ve_hareketler_gosterilir()
    {
        var e = MusteriSecili();

        Assert.Contains("12,5000 has", e.Markup);   // P-57: has tr-TR (virgül ondalık)
        // PAKET EMANET-1: hareketler "Eski kayıtlar" katlanırında KAPALI gelir.
        Assert.DoesNotContain("Bilezik 2 adet", e.Markup);
        e.WaitForElement("#e-gecmis-ac").Click();
        Assert.Contains("Bilezik 2 adet", e.Markup);
    }

    [Fact]
    public void Emanetin_isletme_mali_OLMADIGI_ekranda_yazar()
    {
        // Bu bir uyarı metni değil, muhasebe gerçeği: emanet kâr-zarara girmez.
        var e = MusteriSecili();

        Assert.Contains("işletmenin malı değildir", e.Markup);
    }

    [Fact]
    public void Emanet_girisi_KUR_SNAPSHOT_ID_ile_gonderilir()
    {
        var e = MusteriSecili();
        Dugme(e, "Emanet Al").Click();
        e.Find("#e-gram").Input("20");
        Dugme(e, "Kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST");
        Assert.EndsWith("/emanet/giris", istek.Yol);
        Assert.Equal("kur-9", Alan(istek.Govde, "kurSnapshotId"));
        Assert.Equal("c1", Alan(istek.Govde, "cariId"));
        Assert.Equal("20", Alan(istek.Govde, "gramAgirlik"));
    }

    [Fact]
    public void Kur_alinamazsa_emanet_kaydi_YAPILAMAZ()
    {
        // Kur olmadan yazılan emanet, has hesabı olmayan bir emanettir.
        kurDusuk = true;
        var e = Render<Kuyumcu.Web.Pages.Emanet>().EmanetSatiriAc("h1");
        SeciciYardim.Sec(e, "e-cari", "c1");

        Assert.True(Dugme(e, "Emanet Al").HasAttribute("disabled"));
        Assert.Contains("kur anlık görüntüsü olmadan", e.Markup);
    }

    [Fact]
    public void Iade_elimizdekinden_FAZLA_olamaz()
    {
        var e = MusteriSecili();
        Dugme(e, "İade Et").Click();
        e.Find("#c-has").Input("99");

        Assert.True(e.FindAll("button").First(b => b.TextContent.Contains("Kaydet"))
                     .HasAttribute("disabled"));
        Assert.Contains("elinizdeki emanetten fazla olamaz", e.Markup);
    }

    [Fact]
    public void Iade_POST_ile_dogru_cariye_gonderilir()
    {
        var e = MusteriSecili();
        Dugme(e, "İade Et").Click();
        e.Find("#c-has").Input("5");
        Dugme(e, "Kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST");
        Assert.EndsWith("/emanet/c1/cikis", istek.Yol);
        Assert.Equal("5", Alan(istek.Govde, "hasGrami"));
        Assert.Equal("kur-9", Alan(istek.Govde, "kurSnapshotId"));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// KUR EKRANI
//
// Manuel kur girişi, geçmiş ve sarrafiye pariteleri sunucuda vardı, ekranı
// yoktu: interneti kesilen kuyumcu kur giremiyor, yanlış giren biri geçmişten
// doğrulayamıyordu.
// ═════════════════════════════════════════════════════════════════════════════

// PAKET P-65: sınıf partial — kilit gerekçesi testleri KurEkranTestleri.cs dosyasında (aynı fikstür).
public sealed partial class KurEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public KurEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        // PAKET S3-5: Kur ekranı iki yüzlü (ArayuzModuServisi); oturumsuz
        // varsayılan PROFESYONEL olduğundan bu sınıfın gördüğü DOM değişmez.
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            var g = yol switch
            {
                var y when y.Contains("/kur/gecmis") => """
                    [{"id":"k2","altinHasFiyati":6810,"kaynak":"MANUAL",
                      "olusturuldu":"2026-09-14T08:00:00Z","ayarFiyatlari":[]},
                     {"id":"k1","altinHasFiyati":6800,"kaynak":"ALTINKAYNAK",
                      "olusturuldu":"2026-09-13T08:00:00Z","ayarFiyatlari":[]}
                    """.TrimEnd()
                    // PAKET S1-i: geçmiş ilk bakışta 10 satır — 12 dış kaynak satırı daha (toplam 14).
                    + string.Concat(Enumerable.Range(3, 12).Select(i =>
                        $$""",{"id":"k{{i}}","altinHasFiyati":{{6800 - i}},"kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-{{(13 - (i % 12)):00}}T07:00:00Z","ayarFiyatlari":[]}"""))
                    + "]",
                var y when y.Contains("/kur/pariteler") => """
                    {"kaynak":"ALTINKAYNAK","pariteler":[
                      {"kod":"CEYREK_YENI","ad":"Yeni Çeyrek","alis":11500,"satis":11800,"birim":"adet"}]}
                    """,
                var y when y.Contains("/kur/guncel") || y.Contains("/kur/guncelle") => """
                    {"id":"kur-9","altinHasFiyati":6800,"gramAltinFiyati":7100,"usdTry":34.2,
                     "kaynak":"ALTINKAYNAK","olusturuldu":"2026-09-14T09:00:00Z",
                     "ayarFiyatlari":[{"ayarId":"K22","milyem":916,"gramFiyati":6228.8}]}
                    """,
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private static string? Alan(string govde, string ad)
    {
        var e = System.Text.Json.JsonDocument.Parse(govde).RootElement;
        return e.TryGetProperty(ad, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null
            ? v.ToString() : null;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Kur> Ciz()
    {
        var e = Render<Kuyumcu.Web.Pages.Kur>().AyrintiAc();
        istekler.Clear();
        return e;
    }

    private static AngleSharp.Dom.IElement Dugme(
        IRenderedComponent<Kuyumcu.Web.Pages.Kur> e, string metin)
        => e.FindAll("button").First(b => b.TextContent.Contains(metin));

    [Fact]
    public void Acilista_guncel_gecmis_ve_pariteler_cekilir()
    {
        Render<Kuyumcu.Web.Pages.Kur>().AyrintiAc();

        Assert.Contains(istekler, i => i.Yol.Contains("/kur/guncel"));
        Assert.Contains(istekler, i => i.Yol.Contains("/kur/gecmis"));
        Assert.Contains(istekler, i => i.Yol.Contains("/kur/pariteler"));
    }

    [Fact]
    public void Ayar_bazli_gram_fiyatlari_gosterilir()
    {
        var e = Ciz();

        // PAKET S3-5 · UX-S31 / PLAN § 5B md.2: ham ayar kodu (K22) ekrana
        // BASILMAZ; tek kaynaktaki görünen ad ("22 Ayar") yazılır.
        Assert.Contains("22 Ayar", e.Markup);
        Assert.DoesNotContain(">K22<", e.Markup);
        Assert.Contains("916", e.Markup);
    }

    [Fact]
    public void Sarrafiye_pariteleri_alis_ve_satisla_listelenir()
    {
        var e = Ciz();

        Assert.Contains("Yeni Çeyrek", e.Markup);
        Assert.Contains("CEYREK_YENI", e.Markup);
    }

    [Fact]
    public void MANUEL_girilen_kur_gecmiste_ayirt_edilir()
    {
        // Bir fiyat tartışmasında "bunu biz elle girmişiz" cevabı ekrandan
        // okunabilmeli; manuel kayıt gri değil amber rozetle işaretlenir.
        var e = Ciz();
        // PAKET S3-5 · UX-S31: satır ham kodla ("MANUAL") değil okunur adla bulunur.
        var manuelSatir = e.FindAll("tbody tr").First(tr => tr.TextContent.Contains("Elle girilen"));

        Assert.NotNull(manuelSatir.QuerySelector(".rozet-amber"));
    }

    [Fact]
    public void Manuel_kur_POST_ile_kaydedilir()
    {
        var e = Ciz();
        Dugme(e, "Manuel Kur Gir").Click();
        e.Find("#m-has").Input("6900");
        Dugme(e, "Kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST");
        Assert.EndsWith("/kur/manuel", istek.Yol);
        Assert.Equal("6900", Alan(istek.Govde, "altinHasFiyati"));
    }

    [Fact]
    public void Manuel_form_mevcut_degerlerle_acilir()
    {
        // Kuyumcu genelde tek kalemi düzeltir; sıfırdan başlatmak, girilmeyen
        // alanları sessizce sıfırlama riski doğurur.
        var e = Ciz();
        Dugme(e, "Manuel Kur Gir").Click();

        // BICIM-2: odak DIŞINDA sayı/para kutusu gruplu.
        Assert.Equal("6.800", e.Find("#m-has").GetAttribute("value"));
        Assert.Equal("7.100", e.Find("#m-gram").GetAttribute("value"));   // BICIM-2: odak dışı kutu gruplu
    }

    [Fact]
    public void Simdi_cek_dis_kaynak_ucuna_gider()
    {
        var e = Ciz();
        Dugme(e, "Şimdi çek").Click();

        Assert.Contains(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/kur/guncelle"));
    }

    [Fact]
    public void Gecmis_tarih_araligiyla_cekilir()
    {
        var e = Ciz();
        // PAKET GOC-A: geçmiş aralığı ortak TarihAralik (gg.aa.yyyy metin kutusu; id kur-gecmis-bas korunur).
        e.Find("#kur-gecmis-bas").Change("01.09.2026");

        Assert.Contains(istekler, i => i.Yol.Contains("baslangic=2026-09-01"));
    }
}
