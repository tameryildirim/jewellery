using System.Text.Json;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET BICIM-2 — SAYI/PARA GİRDİ BİÇİMİ TEK KALIP [saf + bUnit]
//
// Kurucu 25.09 ~12:10 (Satış karma ödeme): solda kalem tablosu "113.363,16 ₺",
// sağda ödeme bacağı kutularında "50000" ve "63363,16" — "tüm formatları
// gözden geçir". Kök: SayisalAlan kutuya DAİMA Bicim.Girdi (gruplamasız,
// P-78) basıyordu.
//
// Sözleşme (CUMA-M TelefonBicimli kalıbı):
//   • ODAK DIŞI  → Bicim.ParaGirdi / GramGirdi / SayiGirdi ("50.000", "63.363,16")
//   • ODAKTA     → ham düzenlenebilir metin ("50000", "63363,16"); yazarken
//                  değer YENİDEN YAZILMAZ (imleç atlamaz)
//   • BLUR       → metin SayiCoz ile yeniden çözülür, ham normalleşir, biçimlenir
//   • Biçimli metin SayiCoz'a HİÇ gitmez: P-78 "1.500" belirsizliği korunur,
//     ama kutunun kendi "1.500" gösterimi hata doğurmaz.
//   • POST gövdesine giden değer decimal (makine biçimi) — DEĞİŞMEDİ.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class Bicim2SafBicimTestleri
{
    [Theory]
    [InlineData("50000", "50.000")]
    [InlineData("63363.16", "63.363,16")]
    [InlineData("1500.5", "1.500,50")]
    [InlineData("0.5", "0,50")]
    [InlineData("999", "999")]
    [InlineData("1250000", "1.250.000")]
    [InlineData("12.345", "12,345")]        // ikiden fazla hane YUVARLANMAZ
    [InlineData("-2500", "-2.500")]
    public void B2_S1_ParaGirdi_gruplu_kurus_sifirsa_yazilmaz_degilse_iki_hane(string ham, string beklenen) =>
        Assert.Equal(beklenen, Bicim.ParaGirdi(decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData("12.5", "12,500")]
    [InlineData("1234.5", "1234,500")]      // P-57: gramda binlik YOK (Bicim.Gram ile AYNI)
    [InlineData("1.1255", "1,1255")]
    public void B2_S2_GramGirdi_Bicim_Gram_kurali_ile_ayni(string ham, string beklenen)
    {
        var d = decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(beklenen, Bicim.GramGirdi(d));
        if (decimal.Round(d, 3) == d) Assert.Equal(beklenen + " g", Bicim.Gram(d));   // tek kural
    }

    [Theory]
    [InlineData("77.6014", "77,6014")]
    [InlineData("2", "2,0000")]
    public void B2_S3_HasGirdi_dort_hane(string ham, string beklenen)
    {
        var d = decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(beklenen, Bicim.HasGirdi(d));
        Assert.Equal(beklenen + " has", Bicim.Has(d));
    }

    [Theory]
    [InlineData("12000", "12.000")]
    [InlineData("41.95", "41,95")]
    [InlineData("916", "916")]
    public void B2_S4_SayiGirdi_gruplu(string ham, string beklenen) =>
        Assert.Equal(beklenen, Bicim.SayiGirdi(decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void B2_S5_tablo_gosterim_kurallari_DEGISMEDI_ve_Girdi_hala_gruplamasiz()
    {
        Assert.Equal("50.000 ₺", Bicim.Tl(50000m));
        Assert.Equal("63.363,16 ₺", Bicim.Tl(63363.16m));
        Assert.Equal("12,500 g", Bicim.Gram(12.5m));
        Assert.Equal("50000", Bicim.Girdi(50000m));
        Assert.Equal("63363,16", Bicim.Girdi(63363.16m));
    }

    [Fact]
    public void B2_S6_odak_disi_bicim_SayiCoz_ile_geri_okunabilir_ama_1500_belirsiz_kalir()
    {
        // Odak dışı "63.363,16" SayiCoz'a gitseydi bile birebir okunurdu …
        Assert.Equal(63363.16m, FormAlani.SayiCoz(Bicim.ParaGirdi(63363.16m)).Deger);
        // … ama "1.500" P-78 gereği BELİRSİZDİR: bu yüzden biçimli metin SayiCoz'a GİTMEZ
        // (bileşen testi B2_B5 bunu kilitler).
        Assert.Equal("1.500", Bicim.ParaGirdi(1500m));
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
    }
}

public sealed class Bicim2BilesenTestleri : BunitContext
{
    public Bicim2BilesenTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private IRenderedComponent<Para> ParaKutusu(decimal? deger, Action<decimal?> geri) =>
        Render<Para>(p => p
            .Add(x => x.ElemanId, "b2-para")
            .Add(x => x.Etiket, "Tutar")
            .Add(x => x.Deger, deger)
            .Add(x => x.DegerChanged, EventCallback.Factory.Create<decimal?>(this, geri)));

    private string Deger(IRenderedComponent<Para> c) => c.Find("#b2-para").GetAttribute("value")!;

    [Fact]
    public void B2_B1_Para_odak_disi_gruplu_odakta_ham()
    {
        var c = ParaKutusu(50000m, _ => { });
        Assert.Equal("50.000", Deger(c));             // odak dışı: tablo ile aynı dil
        Assert.Equal("50000", c.Find("#b2-para").GetAttribute("data-deger"));   // düzenlenebilir değer (Secici sözleşmesi)
        Assert.Equal("₺", c.Find("#b2-para-birim").TextContent.Trim());   // simge son ekte, kutuda DEĞİL

        c.Find("#b2-para").Focus();
        Assert.Equal("50000", Deger(c));              // odakta: ham, düzenlenebilir

        c.Find("#b2-para").Blur();
        Assert.Equal("50.000", Deger(c));
    }

    [Fact]
    public void B2_B2_yazilan_63363_16_blurda_63_363_16_olur_deger_decimal()
    {
        decimal? gelen = null;
        var c = ParaKutusu(null, v => gelen = v);
        var k = c.Find("#b2-para");
        k.Focus();
        k.Input("63363,16");
        Assert.Equal("63363,16", Deger(c));           // yazarken gruplanmaz
        Assert.Equal(63363.16m, gelen);
        c.Find("#b2-para").Blur();
        Assert.Equal("63.363,16", Deger(c));
        Assert.Equal(63363.16m, gelen);               // değer değişmedi, yalnız gösterim

        c.Find("#b2-para").Focus();
        Assert.Equal("63363,16", Deger(c));
    }

    [Fact]
    public void B2_B3_kurus_varsa_iki_hane_1500_5_blurda_1_500_50()
    {
        decimal? gelen = null;
        var c = ParaKutusu(null, v => gelen = v);
        c.Find("#b2-para").Focus();
        c.Find("#b2-para").Input("1500,5");
        c.Find("#b2-para").Blur();
        Assert.Equal("1.500,50", Deger(c));
        Assert.Equal(1500.5m, gelen);
    }

    [Fact]
    public void B2_B4_yazarken_deger_yeniden_yazilmaz()
    {
        var c = ParaKutusu(null, _ => { });
        c.Find("#b2-para").Focus();
        foreach (var adim in new[] { "5", "50", "500", "5000", "50000", "50000,", "50000,1" })
        {
            c.Find("#b2-para").Input(adim);
            Assert.Equal(adim, Deger(c));             // kullanıcının yazdığı AYNEN — "5.000" olmaz
        }
    }

    [Fact]
    public void B2_B5_odak_disi_1_500_gosterimi_SayiCoz_a_gitmez_hata_dogmaz()
    {
        var cagri = 0;
        var c = ParaKutusu(1500m, _ => cagri++);
        Assert.Equal("1.500", Deger(c));
        Assert.Equal("false", c.Find("#b2-para").GetAttribute("aria-invalid"));

        // Kullanıcı kutuya girip hiçbir şey yazmadan çıkar: değer, hata, çağrı YOK.
        c.Find("#b2-para").Focus();
        Assert.Equal("1500", Deger(c));
        c.Find("#b2-para").Blur();
        Assert.Equal("1.500", Deger(c));
        Assert.Empty(c.FindAll("#b2-para-hata"));
        Assert.Equal(0, cagri);
    }

    [Fact]
    public void B2_B6_kullanici_1_500_yazarsa_P78_belirsizlik_hatasi_korunur_kutu_aynen_kalir()
    {
        decimal? gelen = 7m;
        var c = ParaKutusu(7m, v => gelen = v);
        c.Find("#b2-para").Focus();
        c.Find("#b2-para").Input("1.500");
        c.Find("#b2-para").Blur();
        Assert.Equal(SayiGirisiSozlugu.Tr.Belirsiz, c.Find("#b2-para-hata").TextContent.Trim());
        Assert.Equal("1.500", Deger(c));              // kullanıcının yazdığı — biçimli "7" DEĞİL
        Assert.Equal(7m, gelen);                      // belirsiz girdi değeri değiştirmedi
        Assert.Equal("true", c.Find("#b2-para").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void B2_B7_blurda_SayiCoz_normallestirir_noktali_binlik_odaga_donunce_ham()
    {
        decimal? gelen = null;
        var c = ParaKutusu(null, v => gelen = v);
        c.Find("#b2-para").Focus();
        c.Find("#b2-para").Input("50.000");           // geçerli Türkçe binlik (P-78)
        Assert.Equal(50000m, gelen);
        c.Find("#b2-para").Blur();
        Assert.Equal("50.000", Deger(c));
        c.Find("#b2-para").Focus();
        Assert.Equal("50000", Deger(c));              // blur SayiCoz ile hamı normalleştirdi
    }

    [Fact]
    public void B2_B8_cozulemeyen_metin_odak_disinda_aynen_kalir()
    {
        var c = ParaKutusu(10m, _ => { });
        c.Find("#b2-para").Focus();
        c.Find("#b2-para").Input("12a");
        c.Find("#b2-para").Blur();
        Assert.Equal("12a", Deger(c));
        Assert.NotEmpty(c.FindAll("#b2-para-hata"));
    }

    [Fact]
    public void B2_B9_Gram_odak_disi_uc_hane_binliksiz_odakta_ham()
    {
        decimal? gelen = null;
        var c = Render<Gram>(p => p
            .Add(x => x.ElemanId, "b2-gram")
            .Add(x => x.Etiket, "Gram")
            .Add(x => x.Deger, 12.5m)
            .Add(x => x.DegerChanged, EventCallback.Factory.Create<decimal?>(this, v => gelen = v)));
        Assert.Equal("12,500", c.Find("#b2-gram").GetAttribute("value"));
        c.Find("#b2-gram").Focus();
        Assert.Equal("12,5", c.Find("#b2-gram").GetAttribute("value"));
        c.Find("#b2-gram").Input("1234,75");
        Assert.Equal("1234,75", c.Find("#b2-gram").GetAttribute("value"));
        c.Find("#b2-gram").Blur();
        Assert.Equal("1234,750", c.Find("#b2-gram").GetAttribute("value"));
        Assert.Equal(1234.75m, gelen);
    }

    [Fact]
    public void B2_B10_Sayi_odak_disi_gruplu_odakta_ham()
    {
        decimal? gelen = null;
        var c = Render<Sayi>(p => p
            .Add(x => x.ElemanId, "b2-sayi")
            .Add(x => x.Etiket, "Adet")
            .Add(x => x.Deger, null)
            .Add(x => x.DegerChanged, EventCallback.Factory.Create<decimal?>(this, v => gelen = v)));
        c.Find("#b2-sayi").Focus();
        c.Find("#b2-sayi").Input("12000");
        Assert.Equal("12000", c.Find("#b2-sayi").GetAttribute("value"));
        c.Find("#b2-sayi").Blur();
        Assert.Equal("12.000", c.Find("#b2-sayi").GetAttribute("value"));
        Assert.Equal(12000m, gelen);
        c.Find("#b2-sayi").Focus();
        Assert.Equal("12000", c.Find("#b2-sayi").GetAttribute("value"));
    }

    [Fact]
    public void B2_B12_odak_olayi_kacsa_bile_yazilan_metin_bicimle_ezilmez()
    {
        // input olayı = kullanıcı kutuda yazıyor (otomatik doldurma / olay sırası).
        var c = ParaKutusu(null, _ => { });
        c.Find("#b2-para").Input("50000");
        Assert.Equal("50000", Deger(c));
        c.Find("#b2-para").Blur();
        Assert.Equal("50.000", Deger(c));
    }

    [Fact]
    public void B2_B11_disaridan_gelen_deger_odak_disinda_bicimli()
    {
        var c = ParaKutusu(100m, _ => { });
        c.Render(p => p.Add(x => x.Deger, 113363.16m));
        Assert.Equal("113.363,16", Deger(c));
    }
}

/// <summary>Gerçek sayfa: /gider — Para kutusu gruplu görünür, POST gövdesine decimal gider.</summary>
public sealed class Bicim2GiderPostTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public Bicim2GiderPostTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var metot = istek.Method.Method;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((metot, istek.RequestUri!.PathAndQuery, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/gider-kategorileri") =>
                    """{"kategoriler":[{"id":"gk1","ad":"Kira","aciklama":null,"aktif":true,"giderAdedi":0,"giderToplamTl":0}],"toplam":1}""",
                var y when y.EndsWith("/giderler") && metot == "POST" =>
                    """{"id":"kh9","kasaId":"ks1","kasaAdi":"Merkez Kasa","kategoriId":"gk1","kategoriAdi":"Kira","tutar":63363.16,"paraBirimi":"TRY","aciklama":null,"tarih":"2026-09-25T06:00:00Z","kasaBakiyesi":36636.84}""",
                var y when y.EndsWith("/giderler") =>
                    """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":"En çok 100 gider satırı gösterilir."}""",
                var y when y.EndsWith("/virman") =>
                    """{"satirlar":[],"toplam":0,"limit":50,"dahaFazlasiVar":false,"sinirNotu":"En son 50 virman gösterilir."}""",
                var y when y.Contains("/kasalar/") && y.Contains("/hareketler") => """{"hareketler":[]}""",
                var y when y.EndsWith("/kasalar") =>
                    """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":100000,"aktif":true,"subeId":null}]""",
                var y when y.EndsWith("/bankalar") => """{"hesaplar":[]}""",
                var y when y.Contains("/kur/guncel") => """{"id":"kur1","hasAlis":4100,"hasSatis":4150}""",
                var y when y.Contains("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                _ => "{}",
            };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    [Fact]
    public void B2_P1_gider_tutari_kutuda_63_363_16_POST_govdesinde_decimal_63363_16()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));
        var e = Render<Kuyumcu.Web.Pages.Gider>();
        e.WaitForAssertion(() => e.Find("#gd-kaydet"));
        Assert.Equal("gk1", e.Find("#gd-kategori-sabit").GetAttribute("data-deger"));   // TOPLU-1 UX-S22: tek kategori seçicisiz, kendiliğinden
        e.Find("#gd-tutar").Focus();
        e.Find("#gd-tutar").Input("63363,16");
        e.Find("#gd-tutar").Blur();
        Assert.Equal("63.363,16", e.Find("#gd-tutar").GetAttribute("value"));

        e.Find("#gd-kaydet").Click();
        e.WaitForAssertion(() => Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/giderler")));
        var govde = istekler.Single(i => i.Metot == "POST" && i.Yol.EndsWith("/giderler")).Govde;
        using var j = JsonDocument.Parse(govde);
        var tutar = j.RootElement.GetProperty("tutar");
        Assert.Equal(JsonValueKind.Number, tutar.ValueKind);    // makine biçimi: sayı, metin DEĞİL
        Assert.Equal(63363.16m, tutar.GetDecimal());
        Assert.DoesNotContain("63.363", govde);                 // biçimli gösterim gövdeye SIZMADI
    }
}
