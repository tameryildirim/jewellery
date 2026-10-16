using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// KARMA ÖDEME EKRANI — çok bacaklı satış fişi
//
// Sınanan kapılar:
//   • bacak toplamı fiş tutarına TAM eşit değilse Kaydet kapalı
//   • fiş toplamı haddi aşıyorsa NAKİT bacak eklenemez (§4.1.2) — eşik
//     SUNUCUDAN gelir, ekrana gömülmez
//   • karma fiş TEK POST ile /islemler/satis-fisi ucuna gider (kalem başına DEĞİL)
//   • veresiye bacağı için müşteri zorunlu
// ═════════════════════════════════════════════════════════════════════════════

public sealed class KarmaOdemeEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool nakitKapali;

    public KarmaOdemeEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => nakitKapali))
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
        List<(string, string, string)> kayit, Func<bool> kapaliMi) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            var y = yol switch
            {
                var v when v.Contains("/tevsik/gunluk") =>
                    $$"""
                    {"had":30000,"kuralEtkin":true,"kumulatifHesaplandi":true,
                     "bugunkuNakitToplam":0,"kalanNakitHakki":30000,
                     "islemTutariKuraliDevrede":{{(kapaliMi() ? "true" : "false")}}}
                    """,
                var v when v.Contains("/fiyat/teklif") => """{"teklifVar":true,"listeFiyati":10000,"hasDegeri":9500,"marjTl":0,"iscilikTl":500,"indirimTutari":0,"satisFiyati":10000,"kampanyaId":null,"sabitFiyat":false,"indirimOncesiFiyat":null}""",
                var v when v.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"ayarFiyatlari":[]}""",
                var v when v.Contains("/urunler") => """
                    [{"id":"u1","ad":"Burma Bilezik","ayarId":"K22","hasGrami":14.19,
                      "iscilikTl":0,"satisFiyati":10000}]
                    """,
                var v when v.Contains("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","aktif":true}]""",
                var v when v.Contains("/subeler") => """[{"id":"s1","ad":"Merkez","aktif":true}]""",
                var v when v.Contains("/cari") => """[{"id":"c1","ad":"Ayşe Demir"}]""",
                var v when v.Contains("/masak/esikler") => """
                    {"kimlikEsigi":185000,"amacBeyaniEsigi":185000,"beyanFormuEsigi":185000,
                     "ekBelgeEsigi":185000,"asgariAciklamaUzunlugu":20,
                     "belgeTurleri":["TC_KIMLIK","PASAPORT"]}
                    """,
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

    private static System.Text.Json.JsonElement Govde(string g) =>
        System.Text.Json.JsonDocument.Parse(g).RootElement;

    /// <summary>Ekranı çizer, bir kalem ekler (10.000 TL) ve karma kipini açar.</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Islemler> KarmaEkran(int kalemAdet = 1)
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        SeciciYardim.Sec(e, "islem-cari", "c1");
        for (var i = 0; i < kalemAdet; i++)
        {
            SeciciYardim.Sec(e, "islem-urun", "u1");
            e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        }
        e.Find("#islem-karma").Change(true);
        // ⚠ Anahtar EŞZAMANSIZ bir önizleme çağrısı tetikler (@bind:after).
        // Yeniden çizim beklenmeden ilerlemek testi yük altında kırılgan yapar.
        e.WaitForElement("#islem-bacak-ekle");
        istekler.Clear();
        return e;
    }

    /// <summary>Bacak ekler ve satırının çizilmesini bekler.</summary>
    private static void BacakEkle(IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, int ix)
    {
        e.Find("#islem-bacak-ekle").Click();
        e.WaitForElement($"#islem-bacak-{ix}-arac");
    }

    private static void Bacak(
        IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e, int ix, string arac, string tutar)
    {
        SeciciYardim.Sec(e, $"islem-bacak-{ix}-arac", arac);
        e.Find($"#islem-bacak-{ix}-tutar").Input(tutar);
        // VERESIYE'de kasa alanı çizilmez. SATIS-3 (KAYNAK 5): tek uygun kasada seçici HİÇ
        // çizilmez, kasa otomatik atanır (gövde testleri kasaId'yi ayrıca doğrular).
        if (arac != "VERESIYE" && e.FindAll($"#islem-bacak-{ix}-kasa").Count > 0)
            SeciciYardim.Sec(e, $"islem-bacak-{ix}-kasa", "ks1");
    }

    private static AngleSharp.Dom.IElement Kaydet(
        IRenderedComponent<Kuyumcu.Web.Pages.Islemler> e)
        => e.FindAll("button").First(b => b.TextContent.Contains("Kaydet"));

    [Fact]
    public void Karma_kipi_kapaliyken_bacak_listesi_YOKTUR()
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>();

        Assert.Empty(e.FindAll("#islem-bacak-ekle"));
        Assert.Single(e.FindAll("#islem-odeme"));   // tek ödeme seçicisi yerinde
    }

    [Fact]
    public void Karma_kipi_acilinca_tek_odeme_secicisi_GIZLENIR()
    {
        // İki ayrı ödeme girişi aynı anda açık kalırsa hangisinin geçerli olduğu
        // belirsizleşir; sunucuya da ikisi birden gönderilemez.
        var e = KarmaEkran();

        Assert.Empty(e.FindAll("#islem-odeme"));
        Assert.Single(e.FindAll("#islem-bacak-ekle"));
    }

    [Fact]
    public void Bacak_eklenince_KALAN_tutar_onerilir()
    {
        // Tezgâhtar rakamı elle bölmek zorunda kalmamalı.
        var e = KarmaEkran();
        BacakEkle(e, 0);

        Assert.Equal("10000", SeciciYardim.SeciliDeger(e, "islem-bacak-0-tutar"));
    }

    [Fact]
    public void Bacak_toplami_tutmuyorsa_KAYDET_KAPALI()
    {
        var e = KarmaEkran();
        BacakEkle(e, 0);
        Bacak(e, 0, "NAKIT", "9000");   // fiş 10.000, bacak 9.000

        Assert.True(Kaydet(e).HasAttribute("disabled"));
        // SATIS-3 sade dil: kilit sebebi farkın YÖNÜNÜ ve tutarını söyler (eski uzun kural metni çizilmez).
        var beklenen = string.Format(Kuyumcu.Core.I18n.Satis3Sozlugu.Tr.FarkEksik, Bicim.Tl(1000m));
        Assert.Contains(beklenen, e.Markup);
        Assert.DoesNotContain("birebir eşit olmalıdır", e.Markup);

        // Fazla tarafı da aynı kapıdan geçer: 11.000 > 10.000 → Kaydet yine kapalı, sebep "Fazla".
        Bacak(e, 0, "NAKIT", "11000");
        Assert.True(Kaydet(e).HasAttribute("disabled"));
        Assert.Contains(string.Format(Kuyumcu.Core.I18n.Satis3Sozlugu.Tr.FarkFazla, Bicim.Tl(1000m)), e.Markup);

        // Tutunca kilit kalkar — kapıyı kapatan fark idi.
        Bacak(e, 0, "NAKIT", "10000");
        Assert.False(Kaydet(e).HasAttribute("disabled"));
    }

    [Fact]
    public void Bacak_toplami_tuttugunda_KAYDET_ACILIR()
    {
        var e = KarmaEkran();
        BacakEkle(e, 0);
        Bacak(e, 0, "NAKIT", "6000");
        BacakEkle(e, 1);
        Bacak(e, 1, "KART", "4000");

        Assert.False(Kaydet(e).HasAttribute("disabled"));
    }

    [Fact]
    public void Karma_fis_TEK_POST_ile_satis_fisi_ucuna_gider()
    {
        // Kalem başına POST atılsaydı bacaklar hangi kaleme ait olurdu belirsiz
        // kalır, kısmi başarısızlıkta fişin yarısı yazılırdı.
        var e = KarmaEkran(kalemAdet: 2);          // 2 × 10.000 = 20.000
        BacakEkle(e, 0);
        Bacak(e, 0, "NAKIT", "12000");
        BacakEkle(e, 1);
        Bacak(e, 1, "VERESIYE", "8000");
        Kaydet(e).Click();

        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Yol.EndsWith("/islemler/satis-fisi")));

        var postlar = istekler.Where(i => i.Metot == "POST").ToList();
        Assert.Single(postlar);                                  // TEK gönderim
        Assert.DoesNotContain(istekler, i => i.Yol.EndsWith("/islemler/satis"));

        var g = Govde(postlar[0].Govde);
        // Aynı ürün iki kez eklendiğinde ekran YENİ SATIR açmaz, adedi artırır —
        // kalem sayısı 1, adet 2. Fiş toplamı yine 20.000 TL.
        var kalemlerJson = g.GetProperty("kalemler");
        Assert.Equal(1, kalemlerJson.GetArrayLength());
        Assert.Equal(2, kalemlerJson[0].GetProperty("adet").GetDecimal());
        var odemeler = g.GetProperty("odemeler");
        Assert.Equal(2, odemeler.GetArrayLength());
        Assert.Equal("NAKIT", odemeler[0].GetProperty("arac").GetString());
        Assert.Equal(12000, odemeler[0].GetProperty("tutarTl").GetDecimal());
        Assert.Equal("VERESIYE", odemeler[1].GetProperty("arac").GetString());
        // Veresiye bacağına kasa GÖNDERİLMEZ — sunucu reddeder.
        Assert.Equal(System.Text.Json.JsonValueKind.Null, odemeler[1].GetProperty("kasaId").ValueKind);
    }

    [Fact]
    public void Had_asilinca_NAKIT_bacak_KAYDI_ENGELLER()
    {
        // §4.1.2 — eşiğin kendisi sunucudan gelir (/tevsik/gunluk), ekrana gömülmez.
        nakitKapali = true;
        var e = KarmaEkran();
        BacakEkle(e, 0);
        Bacak(e, 0, "NAKIT", "10000");

        Assert.True(Kaydet(e).HasAttribute("disabled"));
        // SATIS-3 sade dil: "bacak" jargonu, büyük harf vurgusu ve eşik sayısı metinde YOK.
        Assert.Contains(Kuyumcu.Core.I18n.Satis3Sozlugu.Tr.NakitAlinamaz, e.Markup);
        Assert.DoesNotContain("NAKİT bacak eklenemez", e.Markup);

        // Kilidi kapatan nakit kuralıdır: aynı tutar karta çevrilince Kaydet açılır.
        Bacak(e, 0, "KART", "10000");
        Assert.False(Kaydet(e).HasAttribute("disabled"));
    }

    [Fact]
    public void Had_asilsa_bile_NAKITSIZ_bacaklar_KAYDEDILEBILIR()
    {
        // Kural nakdi kapatır, satışı değil.
        nakitKapali = true;
        var e = KarmaEkran();
        BacakEkle(e, 0);
        Bacak(e, 0, "KART", "10000");

        Assert.False(Kaydet(e).HasAttribute("disabled"));
    }

    [Fact]
    public void Ekran_acilinca_tevsik_onizlemesi_SUNUCUDAN_sorulur()
    {
        var e = Render<Kuyumcu.Web.Pages.Islemler>();
        SeciciYardim.Sec(e, "islem-cari", "c1");
        SeciciYardim.Sec(e, "islem-urun", "u1");
        e.FindAll("button").First(b => b.TextContent.Trim() == "Ekle").Click();
        e.Find("#islem-karma").Change(true);

        e.WaitForAssertion(() =>
            Assert.Contains(istekler, i => i.Yol.Contains("/tevsik/gunluk")));
        Assert.Contains(istekler, i => i.Yol.Contains("fisToplamTl=10000"));
    }

    [Fact]
    public void Satistan_alisa_gecince_karma_kipi_KAPANIR()
    {
        var e = KarmaEkran();
        e.FindAll("button").First(b => b.TextContent.Contains("Alış")).Click();

        Assert.Empty(e.FindAll("#islem-bacak-ekle"));
        Assert.Empty(e.FindAll("#islem-karma"));
    }
}
