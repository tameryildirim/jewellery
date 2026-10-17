using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Fis;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// SATIS-3 t22 — tasarım hakemi YÜKSEK: "Kapora (Diğer formları) yasal eşiği önceden denetlemiyor;
// 'Kapora al' etkin, tıklanınca sayfanın dibinde büyük harfli ham VUK paragrafı çıkıyor."
// KABUL: (1) nakit kapora sunucunun haddini (/tevsik/gunluk — sayı istemciye gömülmez) AŞARSA
// düğme PASİF, yanında kısa sebep, tık POST atmaz; tam hadde nakit serbest (sunucu "olcut > Had");
// kart/havaleye geçince açılır ve gövdede KART gider. (2) had bilinmezken ön denetim yok; sunucunun
// tevsik reddi düğmenin YANINDA kısa, büyük harfsiz tek cümle olur — sayfadaki hata bandına ham
// paragraf düşmez. Gövdeler JSON olarak ayrıştırılarak sınanır.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class SATIS3T22KaporaNakitTestleri : BunitContext
{
    private static readonly Satis3Metinleri S3 = Satis3Sozlugu.Tr;
    private const string HamRed =
        "Tevsik zorunluluğu: işlemin toplam tutarı 1.234.567,50 TL, ödemenin BİR KISMININ nakit yapılması da (459 s. VUK Genel Tebliği §4.1.2; dayanak: VUK mükerrer md.257).";

    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private readonly SahteApi api;

    public SATIS3T22KaporaNakitTestleri()
    {
        api = new SahteApi(istekler);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(api) { BaseAddress = new Uri("http://test.local/") });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        /// <summary>Sunucunun bildirdiği nakit haddi; 0 = bilinmiyor (ön denetim yok).</summary>
        public decimal Had { get; set; } = 30000m;
        /// <summary>Seçili carinin açık kaporası (iade senaryosu için &gt; 0).</summary>
        public decimal AcikAvans { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            if (yol.StartsWith("/api/v1", StringComparison.Ordinal)) yol = yol[7..];
            var metot = istek.Method.Method;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (kayit) kayit.Add((metot, yol, govde));

            if (metot == "POST" && yol == "/islemler/avans")
            {
                using var d = JsonDocument.Parse(govde);
                if (d.RootElement.GetProperty("odemeTuru").GetString() == "NAKIT" && d.RootElement.GetProperty("tutar").GetDecimal() > 30000m)
                    return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                    { Content = new StringContent(JsonSerializer.Serialize(new { message = HamRed }), System.Text.Encoding.UTF8, "application/json") };
            }

            // t23: sunucu kapora İADESİNİ ve siparişten satışı da tevsikle reddeder (SatisModulu.P21 AvansIade, SiparisiSatisaCevir → SatisFisi).
            if (metot == "POST" && (yol == "/islemler/avans/iade" || yol.EndsWith("/satisa-cevir", StringComparison.Ordinal)))
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                { Content = new StringContent(JsonSerializer.Serialize(new { message = HamRed }), System.Text.Encoding.UTF8, "application/json") };

            var had = Had.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var y = (metot, yol) switch
            {
                (_, var v) when v.EndsWith("/cari/sistem-carisi") => """{"id":"pm1","ad":"Peşin Müşteri","sistemCarisi":true}""",
                (_, var v) when v.Contains("/kur/guncel") => """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                (_, var v) when v.Contains("/tevsik/gunluk") =>
                    $$"""{"had":{{had}},"kuralEtkin":true,"kumulatifHesaplandi":false,"bugunkuNakitToplam":0,"kalanNakitHakki":{{had}},"islemTutariKuraliDevrede":true}""",
                (_, var v) when v.Contains("/fiyat/teklif") => """{"teklifVar":false,"sebep":"LISTE_YOK"}""",
                (_, var v) when v.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":9.16,"iscilikTl":0,"satisFiyati":10000}]""",
                (_, var v) when v.Contains("/kasalar") => """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                (_, var v) when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                (_, var v) when v.Contains("/masak/esikler") => """{"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,"ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,"belgeTurleri":["TC_KIMLIK"]}""",
                (_, var v) when v.Contains("/pos/cihazlar") => "[]",
                (_, var v) when v.Contains("/iade-adaylari") => "[]",
                (_, var v) when v.Contains("/reyon") => "[]",
                (_, var v) when v.Contains("/islemler/personeller") => """{"satirlar":[],"toplam":0,"limit":50,"sinirAsildi":false}""",
                ("GET", var v) when v.StartsWith("/siparisler?") =>
                    """{"limit":50,"toplam":1,"dahaFazlasiVar":false,"siparisler":[{"id":"sp1","no":"SIP-000007","durum":"HAZIR","cariId":"c1","cariAd":"Ayşe Demir"}]}""",
                ("GET", "/siparisler/sp1") =>
                    """{"id":"sp1","no":"SIP-000007","durum":"HAZIR","cariId":"c1","kalemler":[{"id":"sk1","urunId":"u1","urunAd":"Burma Bilezik","tanim":null,"gramAgirlik":10,"adet":1}]}""",
                ("GET", var v) when v.StartsWith("/islemler/avans/") =>
                    $$"""{"cariId":"c1","acikAvans":{{AcikAvans.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}""",
                ("POST", "/islemler/avans") => """{"hareketId":"a1","cariId":"c1","tutar":50000,"acikAvans":50000}""",
                (_, var v) when v.Contains("/cari") => """[{"id":"pm1","ad":"Peşin Müşteri"},{"id":"c1","ad":"Anadolu Holding A.Ş."}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(y, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Panel(string tur)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", null, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#islem-tur-SATIS");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yildiz-tezgah-kaydet")));   // TEMA TEMİZLİĞİ 17.10: satış fişi Yıldız tezgâhı
        e.Find($"#islem-diger-{tur.ToLowerInvariant()}").Click();
        e.WaitForElement("#islem-diger-panel");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-diger-kur-yok")));
        return e;
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> Kapora()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", null, ArayuzModuServisi.Profesyonel));
        var e = Render<Kuyumcu.Web.Pages.Islemler>().TezgahBekle();
        e.WaitForElement("#islem-tur-SATIS");
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#yildiz-tezgah-kaydet")));   // TEMA TEMİZLİĞİ 17.10: satış fişi Yıldız tezgâhı
        e.Find($"#islem-diger-{DigerTurleri.Avans.ToLowerInvariant()}").Click();
        e.WaitForElement("#islem-diger-panel");
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#islem-diger-kur-yok")));
        SeciciYardim.Sec(e, "avans-cari", "c1");
        e.WaitForAssertion(() => Assert.Equal(1, Say("GET", "/islemler/avans/c1")));
        return e;
    }

    private int Say(string metot, string yol) { lock (istekler) return istekler.Count(i => i.Metot == metot && i.Yol == yol); }

    private JsonElement SonGovde(string yol)
    {
        lock (istekler)
        {
            var s = istekler.Last(i => i.Metot == "POST" && i.Yol == yol);
            return JsonDocument.Parse(s.Govde).RootElement.Clone();
        }
    }

    [Fact]
    public void T22_Kapora_nakit_haddi_asinca_PASIF_kisa_sebep_POST_YOK_karta_gecince_acilir()
    {
        var e = Kapora();
        e.Find("#avans-tutar").Input("1234567,50");

        var beklenen = string.Format(S3.DigerNakitHaddi, Bicim.Tl(30000m));
        e.WaitForAssertion(() => Assert.True(e.Find("#avans-al").HasAttribute("disabled")));
        Assert.Equal(beklenen, e.Find("#avans-al-kilit").TextContent.Trim());
        Assert.Equal(beklenen, e.Find("#avans-al").GetAttribute("title"));
        Assert.DoesNotContain("BİR KISMI", e.Markup, StringComparison.Ordinal);
        e.Find("#avans-al").Click();
        Assert.Equal(0, Say("POST", "/islemler/avans"));

        SeciciYardim.Sec(e, "avans-odeme", "KART");
        e.WaitForAssertion(() => Assert.False(e.Find("#avans-al").HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#avans-al-kilit"));
        e.Find("#avans-al").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/islemler/avans")));
        var g = SonGovde("/islemler/avans");
        Assert.Equal("KART", g.GetProperty("odemeTuru").GetString());
        Assert.Equal(1234567.50m, g.GetProperty("tutar").GetDecimal());
    }

    [Fact]
    public void T22_Kapora_tam_hadde_nakit_SERBEST_sunucu_gibi_buyuktur_kiyasi()
    {
        var e = Kapora();
        e.Find("#avans-tutar").Input("30000");
        e.WaitForAssertion(() => Assert.False(e.Find("#avans-al").HasAttribute("disabled")));
        Assert.Empty(e.FindAll("#avans-al-kilit"));
        e.Find("#avans-tutar").Input("30000,01");
        e.WaitForAssertion(() => Assert.True(e.Find("#avans-al").HasAttribute("disabled")));
    }

    [Fact]
    public void T22_Had_bilinmezken_sunucu_reddi_dugme_yaninda_KISA_metin_hata_bandinda_ham_paragraf_YOK()
    {
        api.Had = 0m;   // sunucu haddi bildirmedi → istemci ön denetim yapmaz (sayı gömülmez), kapı sunucuda
        var e = Kapora();
        e.Find("#avans-tutar").Input("50000");
        e.WaitForAssertion(() => Assert.False(e.Find("#avans-al").HasAttribute("disabled")));
        e.Find("#avans-al").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/islemler/avans")));

        e.WaitForAssertion(() => Assert.Equal(S3.DigerNakitRed, e.Find("#avans-al-red").TextContent.Trim()));
        Assert.Empty(e.FindAll("#islem-diger-hata"));
        Assert.DoesNotContain("Tevsik zorunluluğu", e.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("BİR KISMI", e.Markup, StringComparison.Ordinal);

        // Kart'a geçip yeniden denenince ret metni kalkar ve kayıt başarıyla biter.
        SeciciYardim.Sec(e, "avans-odeme", "KART");
        e.Find("#avans-al").Click();
        e.WaitForAssertion(() => Assert.Equal(2, Say("POST", "/islemler/avans")));
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#avans-al-red")));
    }

    [Fact]
    public void T22_Kisa_metinlerde_buyuk_harf_vurgusu_YOK_TR_EN_dolu()
    {
        foreach (var m in new[] { Satis3Sozlugu.Tr, Satis3Sozlugu.En })
        {
            foreach (var t in new[] { m.DigerNakitHaddi, m.DigerNakitRed })
            {
                Assert.False(string.IsNullOrWhiteSpace(t));
                // İlk harf dışında art arda iki büyük harf (BİR, KISMI…) yok.
                Assert.DoesNotMatch(@"\p{Lu}{2,}", t);
            }
        }
    }

    // ── t23 (hasım KRİTİK): kısa ret metni YALNIZ onu çizen eylemlerde; kapora iadesi ve siparişten satışta
    // sunucunun tevsik reddi sessiz kalmaz — ortak hata bandında GÖRÜNÜR (Anayasa md.1, UX-S28).

    [Fact]
    public void T23_Kapora_iadesi_tevsik_reddi_SESSIZ_DEGIL_hata_bandinda_gorunur()
    {
        api.AcikAvans = 40000m;
        var e = Kapora();
        e.WaitForAssertion(() => Assert.NotNull(e.Find("#avans-acik")));
        e.Find("#avans-iade-tutar").Input("40000");
        e.Find("#avans-iade-gerekce").Input("Müşteri siparişi iptal etti, kapora geri");
        e.Find("#avans-iade").Click();
        e.WaitForElement("#islem-diger-onay-evet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/islemler/avans/iade")));
        e.WaitForAssertion(() => Assert.Contains("Tevsik", e.Find("#islem-diger-hata").TextContent, StringComparison.Ordinal));
        Assert.Empty(e.FindAll("#islem-diger-basari"));
    }

    [Fact]
    public void T23_Siparisten_satis_tevsik_reddi_SESSIZ_DEGIL_hata_bandinda_gorunur()
    {
        var e = Panel(DigerTurleri.Siparis);
        SeciciYardim.Sec(e, "siparis-sec", "sp1");
        e.WaitForAssertion(() => Assert.Equal(1, Say("GET", "/siparisler/sp1")));
        e.WaitForElement("#siparis-fiyat-sk1").Input("45000");
        e.Find("#siparis-cevir").Click();
        e.WaitForElement("#islem-diger-onay-evet").Click();
        e.WaitForAssertion(() => Assert.Equal(1, Say("POST", "/siparisler/sp1/satisa-cevir")));
        e.WaitForAssertion(() => Assert.Contains("Tevsik", e.Find("#islem-diger-hata").TextContent, StringComparison.Ordinal));
        Assert.Empty(e.FindAll("#islem-diger-basari"));
    }
}
