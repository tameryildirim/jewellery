using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// YERLEŞİM EKRANLARI — paket 25 Faz A [bUnit]
//
// Ölçülen: yerleşik adedin görünmesi, taşıma çekmecesinin kilit kuralları
// (DoD: her düğme ya istek atar ya disabled + sebep), reyon içeriği paneli ve
// sunucunun yerleşim UYARISININ ekranda gösterilmesi (UX-S28 — yutulmaz).
//
// ⚠ SAHTE API GERÇEK SUNUCU ŞEKLİNİ döndürür (S-2 dersi: "{}" varsayılanı ve
//   uydurma zarf, üründe olmayan bir sözleşmeyi doğrular).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YerlesimEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string Reyonlar = """
        [{"id":"r1","subeId":"s1","ad":"A-VITRIN","tip":"VITRIN","aktif":true,"urunSayisi":2,"yerlesikAdet":4},
         {"id":"r2","subeId":"s1","ad":"B-KASA","tip":"KASA","aktif":true,"urunSayisi":0,"yerlesikAdet":0}]
        """;

    private const string ReyonIcerik = """
        {"reyonId":"r1","reyonAdi":"A-VITRIN","tip":"VITRIN","subeId":"s1","aktif":true,
         "toplamSatir":1,"limit":200,"ofset":0,"sinirdaMi":false,
         "satirlar":[{"urunId":"u1","kod":"K-1","ad":"Burma bilezik","birim":"ADET",
                      "ayarId":"K22","maden":"ALTIN","adet":4,"toplamHasGram":36.6}]}
        """;

    public YerlesimEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();   // UX-3 Faz B: Islemler artik bu servisi enjekte ediyor
        Services.AddScoped<ApiIstemci>();
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
                var y when y.Contains("/reyonlar/") && y.EndsWith("/stok") => ReyonIcerik,
                var y when y.Contains("/reyonlar/tasima") =>
 """{"mesaj":"Mal reyonlar arasında taşındı","subeId":"s1","urunId":"u1", "stokAdedi":10,"yerlesikAdet":4,"atanmamis":6}""",
                var y when y.Contains("/reyonlar") => Reyonlar,
                var y when y.Contains("/urun-kategorileri") => "[]",
                var y when y.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                // P-10: KART bacagi POS cihazi listesini okur.
                var p10c when p10c.Contains("/pos/cihazlar") =>
                    """[{"id":"pc1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"s1","kasaId":"ks1","pesinKomisyonOran":1.5,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.UrunAgaci> Ciz()
    {
        var e = Render<Kuyumcu.Web.Pages.UrunAgaci>().AyrintiAc();
        istekler.Clear();
        return e;
    }

    /// <summary>Reyon satırında YERLEŞİK ADET görünür (kart sayısından ayrı).</summary>
    [Fact]
    public void Reyon_satirinda_yerlesik_adet_gosterilir()
    {
        var e = Ciz();
        var dugme = e.Find("#r-icerik-r1");
        Assert.Contains("4", dugme.TextContent);
        // İki sayı da ekrandadır: kart sayısı (2) ve yerleşik adet (4).
        Assert.Contains("2", e.Find("#r-icerik-r1").ParentElement!.ParentElement!.TextContent);
    }

    /// <summary>UX-S23: yerleşik adet, o kayıtlara giden BAĞLANTIDIR.</summary>
    [Fact]
    public void Yerlesik_adet_tiklaninca_reyon_icerigi_acilir()
    {
        var e = Ciz();
        e.Find("#r-icerik-r1").Click();

        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.Contains("/reyonlar/r1/stok"));
        Assert.Contains("Burma bilezik", e.Find("#reyon-icerik-kart").TextContent);
    }

    /// <summary>DoD: boş reyonun "Taşı" düğmesi DISABLED ve sebebi yazılı.</summary>
    [Fact]
    public void Bos_reyonda_tasi_dugmesi_kilitli_ve_sebebi_yazar()
    {
        var e = Ciz();
        Assert.True(e.Find("#r-tasi-r2").HasAttribute("disabled"));
        Assert.Contains("mal yok", e.Find("#r-tasi-kilit-r2").TextContent);
        Assert.False(e.Find("#r-tasi-r1").HasAttribute("disabled"));
    }

    /// <summary>Hedef reyon seçilmeden Kaydet KAPALI kalır ve sebebi yazar.</summary>
    [Fact]
    public void Tasima_hedef_secilmeden_kaydet_kapali()
    {
        var e = Ciz();
        e.Find("#r-tasi-r1").Click();

        // Önce ürün bile seçilmemişken sebep ÜRÜN'dür.
        Assert.True(e.Find("#cekmece-reyon-tasima-kaydet").HasAttribute("disabled"));
        Assert.Contains("ürün seçilmelidir", e.Find("#cekmece-reyon-tasima-kilit").TextContent);

        // Ürün seçilince eksik koşul HEDEF REYON olur ve bu da yazılır.
        SeciciYardim.Sec(e, "tasi-urun", "u1");
        Assert.True(e.Find("#cekmece-reyon-tasima-kaydet").HasAttribute("disabled"));
        Assert.Contains("Hedef reyon", e.Find("#cekmece-reyon-tasima-kilit").TextContent);
    }

    /// <summary>Adet 0 ya da kaynaktakinden büyükse Kaydet KAPALI + alan altı mesaj.</summary>
    [Fact]
    public void Tasima_adet_gecersizse_kaydet_kapali()
    {
        var e = Ciz();
        e.Find("#r-tasi-r1").Click();
        SeciciYardim.Sec(e, "tasi-urun", "u1");
        SeciciYardim.Sec(e, "tasi-hedef", "r2");

        // 0 adet → sessiz etkisizlik YOK, alan altında Türkçe mesaj var.
        Assert.Contains("sıfırdan büyük", e.Find("#tasi-adet-hata").TextContent);
        Assert.True(e.Find("#cekmece-reyon-tasima-kaydet").HasAttribute("disabled"));

        // Kaynakta 4 varken 9 istenirse yine kapalı ve sebebi yazılı.
        e.Find("#tasi-adet").Input("9");
        Assert.Contains("yeterli mal yok", e.Find("#tasi-adet-asim").TextContent);
        Assert.True(e.Find("#cekmece-reyon-tasima-kaydet").HasAttribute("disabled"));
    }

    /// <summary>Geçerli taşıma GERÇEK istek atar (DoD: düğme ya istek atar ya kilitli).</summary>
    [Fact]
    public void Gecerli_tasima_istek_atar()
    {
        var e = Ciz();
        e.Find("#r-tasi-r1").Click();
        SeciciYardim.Sec(e, "tasi-urun", "u1");
        SeciciYardim.Sec(e, "tasi-hedef", "r2");
        e.Find("#tasi-adet").Input("2");

        var kaydet = e.Find("#cekmece-reyon-tasima-kaydet");
        Assert.False(kaydet.HasAttribute("disabled"));
        kaydet.Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.Contains("/reyonlar/tasima"));
        var govde = System.Text.Json.JsonDocument.Parse(istek.Govde).RootElement;
        Assert.Equal("r1", govde.GetProperty("kaynakReyonId").GetString());
        Assert.Equal("r2", govde.GetProperty("hedefReyonId").GetString());
        Assert.Equal(2m, govde.GetProperty("adet").GetDecimal());
        Assert.Equal("s1", govde.GetProperty("subeId").GetString());
    }

    // ═══ ORTAK-1 · KEŞİF #4 (30.09, KRİTİK) — taşıma adedi "1.500" ═══════════════════════════════
    // Kapısız: "1.5" ara değeri (≤ 4) geçerli sayılıyor, kutu hata yazarken Kaydet 1,5 adet taşıyordu.
    [Fact]
    public void OK1_K4_tasima_adedi_belirsiz_1_500_POST_YOK_sebep_yazar()
    {
        var e = Ciz();
        e.Find("#r-tasi-r1").Click();
        SeciciYardim.Sec(e, "tasi-urun", "u1");
        SeciciYardim.Sec(e, "tasi-hedef", "r2");
        foreach (var a in new[] { "1", "1.", "1.5", "1.50", "1.500" }) e.Find("#tasi-adet").Input(a);
        e.Find("#tasi-adet").Blur();
        Assert.Equal("1.500", e.Find("#tasi-adet").GetAttribute("value"));
        Assert.True(e.Find("#cekmece-reyon-tasima-kaydet").HasAttribute("disabled"));
        // Sebep genel "adet geçersiz" kuralı değil, KAPININ cümlesi (kutunun etiketiyle "sayıyı düzeltin").
        Assert.Contains(string.Format(Kuyumcu.Core.I18n.Satis3Sozlugu.Al("tr").AlanDuzeltin,
                                      e.Find("label[for='tasi-adet']").TextContent.Replace("*", "").Trim()),
                        e.Find("#cekmece-reyon-tasima-kilit").TextContent);
        istekler.Clear();
        e.Find("#cekmece-reyon-tasima-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "POST");
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// İŞLEMLER EKRANI — reyonsuz satışın UYARISI yutulmaz (UX-S28) ve isteğe bağlı
// reyon gövdeye konur (UX-S22: tek reyon varsa seçici çıkmaz, değer atanır).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YerlesimUyariEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    private const string Uyari =
        "Reyon seçilmediği için mal şu reyonlardan düşüldü: A-VITRIN (4). "
        + "Doğru reyondan düşmesi için işlemde reyon seçin.";

    /// <summary>Kaç reyon döneceği testten ayarlanır (tek reyon / çok reyon).</summary>
    private string reyonlar = """
        [{"id":"r1","subeId":"s1","ad":"A-VITRIN","tip":"VITRIN","aktif":true,"urunSayisi":0,"yerlesikAdet":4}]
        """;

    public YerlesimUyariEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => reyonlar))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();   // UX-3 Faz B: Islemler artik bu servisi enjekte ediyor
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
    }

    private sealed class SahteApi(
        List<(string, string, string)> kayit, Func<string> reyonlar) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            var y = yol switch
            {
                // Sunucunun GERÇEK yanıtı: hareket alanları + EK yerlesimUyarisi.
                var v when v.Contains("/islemler/satis") =>
                    $$"""{"id":"h1","fisId":"f1","yerlesimUyarisi":"{{Uyari}}"}""",
                var v when v.Contains("/tevsik/gunluk") =>
 """{"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true, "bugunkuNakitToplam":0,"kalanNakitHakki":30000, "islemTutariKuraliDevrede":false}""",
                var v when v.Contains("/fiyat/teklif") =>
 """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0, "iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null, "sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/reyonlar") => reyonlar(),
                var v when v.Contains("/urunler") =>
 """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19, "iscilikTl":0,"satisFiyati":10000}]""",
                var v when v.Contains("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/esikler") =>
 """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000, "ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20, "belgeTurleri":["TC_KIMLIK","PASAPORT"]}""",
                // P-10: KART bacagi POS cihazi listesini okur.
                var p10c when p10c.Contains("/pos/cihazlar") =>
                    """[{"id":"pc1","ad":"Ziraat POS","bankaAdi":"Ziraat","subeId":"s1","kasaId":"ks1","pesinKomisyonOran":1.5,"valorGun":1,"paraBirimi":"TRY","aktif":true,"bekleyenAlacak":0,"bugunYatacakNet":0,"tahsilatVar":false}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> SatisEkrani()
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        SeciciYardim.Sec(e, "islem-cari", "c1");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        return e;
    }

    /// <summary>
    /// Reyonsuz satışta sunucunun döndürdüğü uyarı EKRANDA BİR BANDDA çıkar —
    /// yanıt okunup atılmaz (UX-S28 · Anayasa md.1).
    /// </summary>
    [Fact]
    public void Reyonsuz_satista_donen_uyari_bandda_gosterilir()
    {
        // Oturumun şubesi bilinmediği için reyon seçici çıkmaz; gövdede reyon
        // gitmez ve sunucu uyarı döndürür.
        var e = SatisEkrani();
        e.FindAll("button").First(b => b.TextContent.Contains("Kaydet")).Click();

        e.WaitForElement("#islem-yerlesim-uyari");
        Assert.Contains("A-VITRIN", e.Find("#islem-yerlesim-uyari").TextContent);
    }

    /// <summary>Uyarı yoksa band HİÇ çizilmez (gereksiz gürültü yok).</summary>
    [Fact]
    public void Uyari_yoksa_band_cizilmez()
    {
        var e = SatisEkrani();
        Assert.Empty(e.FindAll("#islem-yerlesim-uyari"));
    }
}
