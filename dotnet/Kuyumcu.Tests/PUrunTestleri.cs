using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Modules.Urun;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Pages;
using Kuyumcu.Web.Servisler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-URUN (ekran denetimi 10.10 · K6, Y19, O13)
//   K6  ayarın varsayılanı YOK: seçilmeden Kaydet kilitli + sebep; ad ↔ ayar çelişkisi uyarı
//       (engellemez); maden değişince ilk ayara atlamaz; sunucu ayarsız gövdeyi 400 + alan.
//   Y19 zorunlular (ad · kod · ayar · gram) ilk ekranda: alan sırası sınanır.
//   O13 Yıldız listesi AYAR sütunu milyem değil "22" / "18".
// ═════════════════════════════════════════════════════════════════════════════

public sealed class PUrunSafTestleri
{
    [Theory]
    [InlineData("22 Ayar Halka Küpe", "K22")]
    [InlineData("14 ayar kolye", "K14")]
    [InlineData("24K Külçe", "K24")]
    [InlineData("Bilezik 18 k", "K18")]
    [InlineData("21 AYAR Burma", "K21")]
    [InlineData("8 krt yüzük", "K8")]
    [InlineData("Burma Bilezik", null)]
    [InlineData("22 Kolye", null)]                       // "K" bir kelimenin başı — ayar değil
    [InlineData("0,50 karat pırlanta", null)]           // karat = taş ağırlığı, ayar değil
    [InlineData("Tektaş 1,22 ayar", null)]              // ondalık sayının parçası
    [InlineData("18 ayar ve 22 ayar set", null)]        // iki farklı ayar — tahmin edilmez
    [InlineData("22 ayar 22K bilezik", "K22")]          // aynı ayar iki yazımla
    [InlineData("", null)]
    [InlineData(null, null)]
    public void PU_S1_Addaki_ayar_cozulur(string? ad, string? beklenen) =>
        Assert.Equal(beklenen, Urunler.AdindakiAyar(ad));

    [Fact]
    public void PU_S2_Celiski_yalniz_ad_ve_secim_FARKLIYSA_uyari_verir()
    {
        var m = Urunler.AdAyarCelismesi("22 Ayar Halka Küpe", "K18", "tr");
        Assert.Equal(string.Format(PUrunSozlugu.Tr.AdAyarCelisiyor, "22 Ayar", "18 Ayar"), m);
        Assert.Null(Urunler.AdAyarCelismesi("22 Ayar Halka Küpe", "K22", "tr"));
        Assert.Null(Urunler.AdAyarCelismesi("22 Ayar Halka Küpe", "", "tr"));   // seçim yok → kilit konuşur
        Assert.Null(Urunler.AdAyarCelismesi("Halka Küpe", "K18", "tr"));
        Assert.Contains("is chosen", Urunler.AdAyarCelismesi("22 Ayar Halka Küpe", "K18", "en"));
    }

    [Fact]
    public void PU_S3_Maden_degisince_ilk_ayara_ATLAMAZ_tek_ayarli_madende_kendiliginden_konur()
    {
        Assert.Equal("", Urunler.MadeninAyariVarsayilansiz("", Kuyumcu.Core.Domain.Maden.Altin));
        Assert.Equal("", Urunler.MadeninAyariVarsayilansiz("K22", Kuyumcu.Core.Domain.Maden.Gumus));
        Assert.Equal("K22", Urunler.MadeninAyariVarsayilansiz("K22", Kuyumcu.Core.Domain.Maden.Altin));
        Assert.Equal("CELIK", Urunler.MadeninAyariVarsayilansiz("", Kuyumcu.Core.Domain.Maden.Celik));
    }

    [Fact]
    public void PU_S4_Yildiz_liste_ayari_formla_ayni_dil()
    {
        Assert.Equal("22", Urunler.YildizAyarKisa("K22"));
        Assert.Equal("18", Urunler.YildizAyarKisa("K18"));
        Assert.Equal("995", Urunler.YildizAyarKisa("K995"));
        Assert.Equal("925", Urunler.YildizAyarKisa("G925"));
        Assert.Equal("Paslanmaz Çelik", Urunler.YildizAyarKisa("CELIK"));
        Assert.Equal("ZZ", Urunler.YildizAyarKisa("ZZ"));
    }

    [Fact]
    public void PU_S5_Sozluk_TR_EN_dolu_ve_farkli()
    {
        foreach (var p in typeof(PUrunMetinleri).GetProperties())
        {
            var tr = (string)p.GetValue(PUrunSozlugu.Tr)!;
            var en = (string)p.GetValue(PUrunSozlugu.En)!;
            Assert.False(string.IsNullOrWhiteSpace(tr), p.Name);
            Assert.False(string.IsNullOrWhiteSpace(en), p.Name);
            Assert.NotEqual(tr, en);
        }
    }
}

public sealed class PUrunEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private string sema = "platin";

    private const string Liste = """
        [{"id":"u1","tip":"ZIYNET","kod":"BLZ-01","ad":"Burma Bilezik","ayarId":"K22",
          "gramAgirlik":15.5,"tasAgirlik":0,"hasGrami":14.19,"barkod":"869001","aktif":true},
         {"id":"u2","tip":"ZIYNET","kod":"YZK-02","ad":"Tektaş Yüzük","ayarId":"K18",
          "gramAgirlik":2.3,"tasAgirlik":0,"hasGrami":1.72,"barkod":null,"aktif":true}]
        """;

    public PUrunEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(PUrunEkranTestleri t) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            lock (t.istekler) t.istekler.Add((istek.Method.Method, yol, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/gorunum") => $$"""{"sema":"{{t.sema}}","yaziOlcegi":"18","temaModu":"karanlik"}""",
                var y when y.Contains("/urunler/stok-adetleri") => """{"urunler":[{"urunId":"u1","toplamAdet":3,"toplamHasGram":40},{"urunId":"u2","toplamAdet":1,"toplamHasGram":1.72}]}""",
                var y when y.EndsWith("/stok") => """{"urunId":"u1","toplamAdet":3,"toplamHasGram":40,"subeStoklar":[]}""",
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
                var y when y.Contains("/kur/guncel") => """{"id":""}""",
                var y when y.Contains("/kmts/ayar") => """{"kmtsKullanimi":false}""",
                var y when y.EndsWith("/urunler/u1") => """
                    {"id":"u1","tip":"ZIYNET","kod":"BLZ-01","ad":"Burma Bilezik","ayarId":"K22",
                     "gramAgirlik":15.5,"tasAgirlik":0,"iscilikTl":0,"barkod":"869001",
                     "kategoriId":null,"reyonId":null,"satisFiyati":null,"aciklama":null,
                     "pariteKodu":null,"tasKokeni":"TAS_YOK"}
                    """,
                var y when y.Contains("/urunler") => istek.Method == HttpMethod.Post ? """{"id":"u9"}""" : Liste,
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(g, Encoding.UTF8, "application/json") };
        }
    }

    private List<(string Metot, string Yol, string Govde)> Istekler() { lock (istekler) return [.. istekler]; }

    private IRenderedComponent<Urunler> Ac(string mod)
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("k1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1", mod, mod));
        var e = Render<Urunler>();
        e.WaitForElement("#urun-yeni-ac").Click();
        e.WaitForElement("#u-ayar");
        return e;
    }

    private static string? Govde(string json, string ad)
    {
        var k = JsonDocument.Parse(json).RootElement;
        return k.TryGetProperty(ad, out var v) && v.ValueKind != JsonValueKind.Null ? v.ToString() : null;
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void PU_B1_Ayar_BOS_acilir_secilmeden_Kaydet_KILITLI_sebep_yazili_POST_yok(string mod)
    {
        var e = Ac(mod);
        Assert.True(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "u-ayar")));
        Assert.Equal("true", e.Find("#u-ayar").GetAttribute("aria-required"));
        e.Find("#u-ad").Input("22 Ayar Halka Küpe");
        e.Find("#u-kod").Input("KPE-22");
        e.Find("#u-gram").Input("3,40");
        Assert.True(e.Find("#u-kaydet").HasAttribute("disabled"));
        Assert.Equal(PUrunSozlugu.Tr.AyarSecilmedi, e.Find("#u-kaydet-kilit").TextContent.Trim());
        Assert.Empty(e.FindAll("#u-ayar-celiski"));   // seçim yokken çelişki yok — tek bilgi tek yerde

        lock (istekler) istekler.Clear();
        e.Find("#u-kaydet").Click();   // bUnit disabled düğmeye de olay gönderir → kod içi koruma da sınanır
        Assert.DoesNotContain(Istekler(), i => i.Metot == "POST");
        Assert.Equal(PUrunSozlugu.Tr.AyarSecilmedi, e.Find("#u-ayar-hata").TextContent.Trim());
    }

    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void PU_B2_Ad_22_secim_18_UYARI_gorunur_ama_kaydi_ENGELLEMEZ_govdede_secilen_ayar(string mod)
    {
        var e = Ac(mod);
        e.Find("#u-ad").Input("22 Ayar Halka Küpe");
        e.Find("#u-kod").Input("KPE-22");
        e.Find("#u-gram").Input("3,40");
        SeciciYardim.Sec(e, "u-ayar", "K18");
        e.WaitForAssertion(() => Assert.Equal(
            string.Format(PUrunSozlugu.Tr.AdAyarCelisiyor, "22 Ayar", "18 Ayar"),
            e.Find("#u-ayar-celiski").TextContent.Trim()));
        Assert.False(e.Find("#u-kaydet").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#u-kaydet-kilit"));

        lock (istekler) istekler.Clear();
        e.Find("#u-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "POST" && i.Yol.EndsWith("/urunler")));
        Assert.Equal("K18", Govde(Istekler().First(i => i.Metot == "POST").Govde, "ayarId"));
    }

    [Fact]
    public void PU_B3_Dogru_ayar_secilince_uyari_YOK_govdede_K22()
    {
        var e = Ac(ArayuzModuServisi.Basit);
        e.Find("#u-ad").Input("22 Ayar Halka Küpe");
        e.Find("#u-kod").Input("KPE-22");
        e.Find("#u-gram").Input("3,40");
        SeciciYardim.Sec(e, "u-ayar", "K22");
        Assert.Empty(e.FindAll("#u-ayar-celiski"));
        e.Find("#u-kaydet").Click();
        e.WaitForAssertion(() => Assert.Contains(Istekler(), i => i.Metot == "POST" && i.Yol.EndsWith("/urunler")));
        Assert.Equal("K22", Govde(Istekler().First(i => i.Metot == "POST").Govde, "ayarId"));
    }

    [Fact]
    public void PU_B4_Maden_degisince_ayar_BOSALIR_ve_Kaydet_yeniden_KILITLENIR()
    {
        var e = Ac(ArayuzModuServisi.Profesyonel);
        SeciciYardim.Sec(e, "u-ayar", "K22");
        Assert.Empty(e.FindAll("#u-kaydet-kilit"));
        SeciciYardim.Sec(e, "u-tip", "SAAT");
        SeciciYardim.Sec(e, "u-maden", "GUMUS");
        Assert.True(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "u-ayar")));
        Assert.Equal(PUrunSozlugu.Tr.AyarSecilmedi, e.Find("#u-kaydet-kilit").TextContent.Trim());
    }

    [Fact]
    public void PU_B5_Yeniden_acilista_onceki_secim_SIZMAZ()
    {
        var e = Ac(ArayuzModuServisi.Basit);
        SeciciYardim.Sec(e, "u-ayar", "K22");
        e.Find("#urun-yeni-ac").Click();   // pencere yeniden açılır
        e.WaitForAssertion(() => Assert.True(string.IsNullOrEmpty(SeciciYardim.SeciliDeger(e, "u-ayar"))));
        Assert.Equal(PUrunSozlugu.Tr.AyarSecilmedi, e.Find("#u-kaydet-kilit").TextContent.Trim());
    }

    /// <summary>Y19 — zorunlular (ad · kod · ayar · gram) formun BAŞINDA; isteğe bağlılar onlardan sonra.</summary>
    [Theory]
    [InlineData(ArayuzModuServisi.Basit)]
    [InlineData(ArayuzModuServisi.Profesyonel)]
    public void PU_B6_Zorunlular_formun_basinda(string mod)
    {
        var e = Ac(mod);
        var html = e.Find(".modal-govde").InnerHtml;
        int Konum(string id) { var i = html.IndexOf($"id=\"{id}\"", StringComparison.Ordinal); Assert.True(i >= 0, id); return i; }
        // Sıra: ad / maden+ayar / kod+gram / barkod+açılış stoğu / ürün grubu / "Daha fazla" — telefonda
        // (tek sütun) ilk beş kutu ad · maden · ayar · kod · gram. Maden ayardan ÖNCE (PAKET 20 bağı).
        var sira = new[] { "u-ad", "u-maden", "u-ayar", "u-kod", "u-gram", "u-barkod", "u-acilis-stok", "u-tip", "u-daha-fazla" };
        for (var i = 1; i < sira.Length; i++)
            Assert.True(Konum(sira[i - 1]) < Konum(sira[i]), $"{sira[i - 1]} → {sira[i]} sırası bozuk");
    }

    [Fact]
    public void PU_B7_Duzenleme_formunda_da_ayni_uyari()
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("k1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1",
                ArayuzModuServisi.Profesyonel, ArayuzModuServisi.Profesyonel));
        var e = Render<Urunler>();
        // TEMA TEMİZLİĞİ 17.10: Yıldız ürün listesinde satıra (ürün adına) dokunmak düzenleme formunu açar
        // (Urunler.Yildiz.cs → MEVCUT DuzenleAc) — özgün listenin "Düzenle" düğmesiyle aynı yol.
        e.WaitForElement("#yildiz-urun-ad-u1").Click();
        e.WaitForElement("#d-ad");
        Assert.Empty(e.FindAll("#d-ayar-celiski"));
        e.Find("#d-ad").Input("18 Ayar Burma Bilezik");
        e.WaitForAssertion(() => Assert.Equal(
            string.Format(PUrunSozlugu.Tr.AdAyarCelisiyor, "18 Ayar", "22 Ayar"),
            e.Find("#d-ayar-celiski").TextContent.Trim()));
    }

    /// <summary>O13 — Yıldız listesi AYAR sütunu formla aynı dil: "22" / "18", milyem (916/750) DEĞİL.</summary>
    [Fact]
    public async Task PU_B8_Yildiz_listesi_ayar_sutunu_22_18_milyem_degil()
    {
        sema = "yildiz";
        await Services.GetRequiredService<GorunumServisi>().YukleAsync();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("k1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1",
                ArayuzModuServisi.Basit, ArayuzModuServisi.Basit));
        var e = Render<Urunler>();
        e.WaitForElement("#yildiz-urun-ad-u1");
        // Satır = div#yildiz-urun-<id>, hücreler doğrudan çocuk (button/span); sütun sırası
        // Ürün · Barkod · AYAR · Gram · Adet · Fiyat → AYAR üçüncü hücre.
        var h1 = e.Find("#yildiz-urun-u1").Children.Select(t => t.TextContent.Trim()).ToList();
        var h2 = e.Find("#yildiz-urun-u2").Children.Select(t => t.TextContent.Trim()).ToList();
        Assert.Equal("22", h1[2]);
        Assert.Equal("18", h2[2]);
        Assert.Contains("22", h1);
        Assert.Contains("18", h2);
        Assert.DoesNotContain("916", h1);
        Assert.DoesNotContain("750", h2);
    }
}

[Collection("veritabani")]
public sealed class PUrunVeriTestleri(TestVeritabani vt)
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task PU_D1_Servis_ayarsiz_urunu_400_ve_alan_ayarId_ile_REDDEDER_satir_yok(string? ayar)
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETP-URUN-TEST D1-" + (ayar?.Length ?? -1));
        var hata = await Assert.ThrowsAsync<BadRequestException>(() =>
            new UrunServisi(vt.Olustur(k)).Olustur(new UrunOlusturDto
            {
                Tip = "ZIYNET", Kod = "PU-D1", Ad = "22 Ayar Halka Küpe", AyarId = ayar!,
                GramAgirlik = 3.4m, Birim = "ADET",
            }, k.Islem));
        Assert.Equal(PUrunSozlugu.Tr.AyarZorunlu, hata.Message);
        Assert.Equal("ayarId", AlanIsareti.Alan(hata));
        Assert.Equal(0, await vt.OlusturSistem().Urunler.AsNoTracking().CountAsync(u => u.TenantId == k.TenantId && u.Kod == "PU-D1"));
    }

    [Fact]
    public async Task PU_D2_Secilen_ayar_AYNEN_yazilir_milyem_ve_has_ondan()
    {
        var sistem = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(sistem, "PAKETP-URUN-TEST D2");
        await new UrunServisi(vt.Olustur(k)).Olustur(new UrunOlusturDto
        {
            Tip = "ZIYNET", Kod = "PU-D2", Ad = "22 Ayar Halka Küpe", AyarId = "K22",
            GramAgirlik = 3.4m, Birim = "ADET",
        }, k.Islem);
        var u = await vt.OlusturSistem().Urunler.AsNoTracking().SingleAsync(x => x.TenantId == k.TenantId && x.Kod == "PU-D2");
        Assert.Equal("K22", u.AyarId);
        Assert.Equal(916m, u.Milyem);
        Assert.Equal(Math.Round(3.4m * 916m / 1000m, 4), Math.Round(u.HasGrami, 4));
    }
}

[Collection("http")]
public sealed class PUrunHttpTestleri(HttpFikstur f)
{
    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(true, "")]
    public async Task PU_H1_Ayarsiz_POST_400_urun_YAZILMAZ(bool alanVar, string? deger)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETP-URUN-TEST H1-" + alanVar + (deger is null ? "n" : "b"));
        var c = f.Ortam.Istemci(k, "SAHIP");
        object govde = alanVar
            ? new { tip = "ZIYNET", kod = "PU-H1", ad = "22 Ayar Halka Küpe", ayarId = deger, gramAgirlik = 3.4m, birim = "ADET" }
            : new { tip = "ZIYNET", kod = "PU-H1", ad = "22 Ayar Halka Küpe", gramAgirlik = 3.4m, birim = "ADET" };
        var r = await c.PostJson("/api/v1/urunler", govde);
        var metin = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode}: {metin}");
        Assert.Contains("ayar", metin, StringComparison.OrdinalIgnoreCase);
        using var s = f.Vt.OlusturSistem();
        Assert.Equal(0, await s.Urunler.AsNoTracking().CountAsync(u => u.TenantId == k.TenantId && u.Kod == "PU-H1"));
    }

    [Fact]
    public async Task PU_H2_Ayarli_POST_201_secilen_ayarla()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "PAKETP-URUN-TEST H2");
        var r = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/urunler",
            new { tip = "ZIYNET", kod = "PU-H2", ad = "22 Ayar Halka Küpe", ayarId = "K22", gramAgirlik = 3.4m, birim = "ADET" });
        Assert.True(r.StatusCode == HttpStatusCode.Created, $"{(int)r.StatusCode}: {await r.Content.ReadAsStringAsync()}");
        using var s = f.Vt.OlusturSistem();
        Assert.Equal("K22", (await s.Urunler.AsNoTracking().SingleAsync(u => u.TenantId == k.TenantId && u.Kod == "PU-H2")).AyarId);
    }
}
