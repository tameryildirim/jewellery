using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using Bunit;
using Kuyumcu.Api.Modules.Urun;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET URUN-FORM-2 — yeni ürün penceresi ret anı
//   F1 adet 999999999 → "Adet çok büyük — en fazla 1.000.000 girilebilir."
//   F2 adet 0 / eksi  → "Adet sıfırdan büyük olmalı."; sınırlar (0,001 ve 1.000.000) GEÇER
//   F3 sözlük TR+EN dolu
//   F4 ekran: alana bağlı retde bant = kısa genel cümle, alan altı = tam cümle (tek yazım)
//   F5 ekran: alansız retde bant tam cümle (hata yutulmaz)
//   F6 ortak kaydırma: modal gövdesi içinde scrollIntoView YOK, alt kenar hesabı VAR; CSS kuralları VAR
// ═════════════════════════════════════════════════════════════════════════════
public sealed class UrunForm2Testleri
{
    private static string Dogrula(decimal adet)
    {
        var dto = new AcilisStoguDto { Adet = adet };
        var sonuc = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), sonuc, true);
        return sonuc.Count == 0 ? "" : sonuc[0].ErrorMessage!;
    }

    [Fact]
    public void F1_Cok_buyuk_adet_tezgahtar_cumlesiyle_reddedilir()
    {
        Assert.Equal("Adet çok büyük — en fazla 1.000.000 girilebilir.", Dogrula(999_999_999m));
        Assert.Equal(UrunAcilisSozlugu.Tr.AdetCokBuyuk, Dogrula(1_000_000.001m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void F2_Sifir_ve_eksi_adet_sifirdan_buyuk_cumlesiyle_reddedilir(int adet)
        => Assert.Equal("Adet sıfırdan büyük olmalı.", Dogrula(adet));

    [Fact]
    public void F2b_Sinir_degerleri_gecer()
    {
        Assert.Equal("", Dogrula(0.001m));
        Assert.Equal("", Dogrula(1_000_000m));
    }

    [Fact]
    public void F3_Sozluk_TR_ve_EN_dolu_ve_farkli()
    {
        foreach (var m in new[] { UrunAcilisSozlugu.Tr, UrunAcilisSozlugu.En })
        {
            Assert.False(string.IsNullOrWhiteSpace(m.AdetCokBuyuk));
            Assert.False(string.IsNullOrWhiteSpace(m.AdetSifirdanBuyuk));
        }
        Assert.NotEqual(UrunAcilisSozlugu.Tr.AdetCokBuyuk, UrunAcilisSozlugu.En.AdetCokBuyuk);
        Assert.NotEqual(Sozlukler.Tr.Urun.FormAlanHatasiGenel, Sozlukler.En.Urun.FormAlanHatasiGenel);
    }

    [Fact]
    public void F6_Ortak_kaydirma_modal_govdesinde_ata_kaplari_kaydirmaz_ve_alt_kenari_hesaplar()
    {
        var kok = KokBul();
        var js = File.ReadAllText(Path.Combine(kok, "Kuyumcu.Web/wwwroot/js/cekmece.js"));
        Assert.Contains("closest('.modal-govde')", js);
        Assert.Contains("e.focus({ preventScroll: true });", js);
        Assert.Contains("govde.scrollTop += k.bottom", js);
        Assert.Contains("requestAnimationFrame(function () { goster();", js);
        var css = File.ReadAllText(Path.Combine(kok, "Kuyumcu.Web/wwwroot/css/app.css"));
        Assert.Contains(".modal-govde { scroll-padding-bottom", css);
        Assert.Contains(".modal-panel .alan-hatali .alan-ipucu { display: none; }", css);
    }

    private static string KokBul()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("Kuyumcu.sln bulunamadı");
    }
}

public sealed class UrunForm2EkranTestleri : BunitContext
{
    private string postYaniti = "";

    public UrunForm2EkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(UrunForm2EkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            if (istek.Method == HttpMethod.Post && yol.EndsWith("/urunler"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                { Content = new StringContent(t.postYaniti, Encoding.UTF8, "application/json") });
            var g = yol switch
            {
                var y when y.Contains("/urunler/stok-adetleri") => """{"urunler":[]}""",
                var y when y.EndsWith("/urunler/madenler") => """
                    {"madenler":[{"maden":"ALTIN","madenAdi":"Altın","rodyumKaplanabilir":true,
                      "ayarlar":[{"id":"K22","ad":"22 Ayar","milyem":916,"kiymetli":true,"hasHesaplanir":true}]}],
                     "etiket":{"rodyumIbaresiGoster":false,"rodyumIbaresi":"rodyum kaplama"}}
                    """,
                var y when y.Contains("/urun-kategorileri") => "[]",
                var y when y.Contains("/reyonlar") => "[]",
                var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez"}]""",
                var y when y.Contains("/cari") => "[]",
                var y when y.Contains("/kur/pariteler") => """{"kaynak":"TEST","pariteler":[]}""",
                var y when y.Contains("/urunler") => "[]",
                _ => "{}",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(g, Encoding.UTF8, "application/json") });
        }
    }

    private static string Yanit(string mesaj, string? alan) => System.Text.Json.JsonSerializer.Serialize(
        alan is null
            ? (object)new { message = mesaj, error = "Bad Request", statusCode = 400 }
            : new { message = mesaj, alanlar = new Dictionary<string, string[]> { [alan] = [mesaj] }, error = "Bad Request", statusCode = 400 });

    private IRenderedComponent<Kuyumcu.Web.Pages.Urunler> Gonder(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("k1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Kuyumcu.Web.Pages.Urunler>();
        e.Find("#urun-yeni-ac").Click();
        e.Find("#u-ad").Input("Form2 bilezik");
        e.Find("#u-kod").Input("F2-E1");
        e.Find("#u-gram").Input("10");
        SeciciYardim.Sec(e, "u-ayar", "K18");   // P-URUN (K6): ayarın varsayılanı yok — kullanıcı seçer
        e.Find("#u-acilis-stok").Input("5");
        e.Find("#u-kaydet").Click();
        return e;
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void F4_Alana_bagli_retde_bant_kisa_genel_cumle_alan_alti_tam_cumle_tek_yazim(string mod)
    {
        var tam = UrunAcilisSozlugu.Tr.AdetCokBuyuk;
        postYaniti = Yanit(tam, "acilisStogu.adet");
        var e = Gonder(mod);
        e.WaitForAssertion(() => Assert.Contains(tam, e.Find("#alan-hata-u-acilis-stok").TextContent));
        var bant = e.Find(".modal-alt #u-urun-hata").TextContent;
        Assert.Contains(Sozlukler.Tr.Urun.FormAlanHatasiGenel, bant);
        Assert.DoesNotContain(tam, bant);
        Assert.Equal(1, e.Markup.Split(tam).Length - 1);   // sayfada tam cümle TEK kez
    }

    [Fact]
    public void F5_Alansiz_retde_bant_tam_cumleyi_tasir()
    {
        postYaniti = Yanit("Beklenmeyen bir ret", null);
        var e = Gonder(ArayuzModuServisi.Basit);
        e.WaitForAssertion(() => Assert.Contains("Beklenmeyen bir ret", e.Find(".modal-alt #u-urun-hata").TextContent));
        Assert.DoesNotContain(Sozlukler.Tr.Urun.FormAlanHatasiGenel, e.Find(".modal-alt #u-urun-hata").TextContent);
    }
}
