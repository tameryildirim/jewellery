using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bunit;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain.Girdi;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler.Form;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// olumsuz-fikstür: dosya — bu dosya doğrulayıcı tur 2 bulgularının SINIRLARINI sınar; geçersiz değerler bilinçlidir.
// ═════════════════════════════════════════════════════════════════════════════
// PAKET GIRDI-AILESI-2 — TUR 3 (hasım doğrulayıcı tur 2 bulguları):
//   (1) KRİTİK Telefon: ekran ve sunucu TEK kural (Core TelefonDogrulayici); ekran geçersiz diyorsa
//       Kaydet KİLİTLİ, kilit metni = alan altı metni; sunucu (işletme kimliği, şube, cari) aynı kuralla 400
//   (2) KRİTİK Tarih: yan yana kullanımda yıl kesilmez (girdi.css en küçük genişlik)
//   (3) YÜKSEK Para: varsayılan 2 ondalık; fazlası alan altı hata + kilit; sunucu (banka hareketi) 400
//   (4) YÜKSEK Bileşenler varsayılan ipucu satırı EKLEMEZ (Iban, Telefon)
//   (5) ORTA Telefon yapıştırma "+90" öneki · "00" uluslararası önek kırpılmaz · "0090" TR numarası
//   (6) ORTA Tarih: eksik tarih alandan çıkınca hata — çağıran null'u yok sayıp eski değeri geri verse de
//   (7) DÜŞÜK Yabancı IBAN için ayrı mesaj (IbanYabanciBicim)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GirdiAilesi2Tur3Testleri : BunitContext
{
    private readonly GecersizAlanKapisi kapi = new();
    private object? son;

    public GirdiAilesi2Tur3Testleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    private IRenderedComponent<CascadingValue<GecersizAlanKapisi>> Ciz<T, TDeger>(Dictionary<string, object?>? ek = null)
        where T : IComponent
    {
        return Render<CascadingValue<GecersizAlanKapisi>>(p => p
            .Add(x => x.Value, kapi)
            .Add(x => x.IsFixed, true)
            .Add(x => x.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<T>(0);
                b.AddAttribute(1, "ElemanId", "t");
                b.AddAttribute(2, "Etiket", "Alan");
                b.AddAttribute(3, "DegerChanged", EventCallback.Factory.Create<TDeger>(this, v => son = v));
                if (ek is not null) b.AddMultipleAttributes(4, ek.Select(x => new KeyValuePair<string, object>(x.Key, x.Value!)));
                b.CloseComponent();
            })));
    }

    // ── (5) Telefon maskesi: "00" önek, "0090", yapıştırma öneki ──────────────
    [Theory]
    [InlineData("00442079460958", "00442079460958")]          // 00 + yabancı: KIRPILMAZ (eskiden "0044 207 94 60")
    [InlineData("0044 20 7946 0958", "00442079460958")]
    [InlineData("00905324112233", "0090 532 411 22 33")]       // 0090 → TR gruplaması (eskiden "0090 532 41 12")
    [InlineData("Tel: +90 (532) 123 45 67", "+90 532 123 45 67")]   // yazılı önek '+'yı yutmaz
    [InlineData("Cep: 0532 123 45 67", "0532 123 45 67")]
    [InlineData("+90 532 411 22 33", "+90 532 411 22 33")]
    [InlineData("05324112233", "0532 411 22 33")]
    public void T3_Telefon_maske_00_onek_ve_yapistirma_oneki(string girilen, string beklenen)
    {
        Assert.Equal(beklenen, Telefon.Maskele(girilen));
        Assert.Equal(beklenen, Telefon.Maskele(beklenen));   // idempotent (JS ile aynı çıktı → imleç atlamaz)
        Assert.Null(TelefonDogrulayici.Denetle(beklenen));
    }

    [Fact]
    public void T3_Telefon_0090_yazimi_gecerli_TR_numarasi_olarak_kaydedilir()
    {
        var e = Ciz<Telefon, string?>();
        e.Find("#t").Input("00905324112233");
        e.Find("#t").Change("0090 532 411 22 33");
        Assert.Empty(e.FindAll("#t-hata"));
        Assert.False(kapi.Var);
        Assert.Equal("05324112233", son);   // CUMA-M saklama biçimi (TelefonBicimi 0090 → TR)
    }

    [Fact]
    public void T3_Telefon_00_yabanci_numara_kirpilmadan_gider_hata_yok()
    {
        var e = Ciz<Telefon, string?>();
        e.Find("#t").Input("00442079460958");
        e.Find("#t").Change("00442079460958");
        Assert.Equal("00442079460958", e.Find("#t").GetAttribute("value"));
        Assert.Empty(e.FindAll("#t-hata"));
        Assert.Equal("00442079460958", son);
    }

    [Fact]
    public void T3_Telefon_yapistirma_arti90_onekli_haneler_kaybolmaz()
    {
        var e = Ciz<Telefon, string?>();
        e.Find("#t").Input("Tel: +90 (532) 123 45 67");
        e.Find("#t").Change("+90 532 123 45 67");
        Assert.Empty(e.FindAll("#t-hata"));
        Assert.Equal("05321234567", son);
    }

    // ── (1) Telefon: tek kural, kilit metni = alan altı metni ─────────────────
    [Theory]
    [InlineData("0000000000", false)]
    [InlineData("05001112233", false)]
    [InlineData("5321234", false)]
    [InlineData("abc", false)]
    [InlineData("05324112233", true)]
    [InlineData("0212 555 01 01", true)]
    [InlineData("+90 532 411 22 33", true)]
    [InlineData("", true)]
    public void T3_Iletisim_TelefonGecerli_Core_TelefonDogrulayici_ile_AYNI(string tel, bool gecerli)
    {
        Assert.Equal(gecerli, Kuyumcu.Core.Domain.Iletisim.TelefonGecerli(tel));
        Assert.Equal(gecerli, string.IsNullOrEmpty(tel) || TelefonDogrulayici.Gecerli(tel));
    }

    [Theory]
    [InlineData("0000000000", "TelefonHane9")]
    [InlineData("5321234", "TelefonHane7")]
    [InlineData("05001112233", "TelefonAralik")]
    public void T3_Telefon_bileseni_kapiya_alan_alti_metnini_bildirir(string girilen, string beklenenAd)
    {
        var e = Ciz<Telefon, string?>();
        e.Find("#t").Input(girilen);
        e.Find("#t").Change(Telefon.Maskele(girilen));
        var beklenen = Beklenen(beklenenAd);
        Assert.Equal(beklenen, e.Find("#t-hata").TextContent.Trim());
        Assert.True(kapi.Var);
        Assert.Equal(beklenen, kapi.Sebep("tr"));
    }

    internal static string Beklenen(string ad) => ad switch
    {
        "TelefonHane9" => GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.TelefonHane, 9, 10), "tr"),
        "TelefonHane7" => GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.TelefonHane, 7, 10), "tr"),
        "TelefonAralik" => GirdiSozlugu.Tr.TelefonAralik,
        _ => throw new ArgumentOutOfRangeException(nameof(ad)),
    };

    // ── (4) varsayılan ipucu satırı yok ───────────────────────────────────────
    [Fact]
    public void T3_Telefon_ve_Iban_varsayilan_ipucu_satiri_EKLEMEZ()
    {
        var t = Ciz<Telefon, string?>();
        Assert.Empty(t.FindAll("#t-ipucu"));
        Assert.Null(t.Find("#t").GetAttribute("aria-describedby"));
        var i = Ciz<Iban, string?>();
        Assert.Empty(i.FindAll("#t-ipucu"));
        i.Find("#t").Input("GB82 WEST 1234");
        Assert.Empty(i.FindAll("#t-ipucu"));   // hata çıkınca ipucu + hata alt alta iki satır OLMAZ
        Assert.Single(i.FindAll("#t-hata"));
        // çağıran açıkça verirse ipucu görünür
        var v = Ciz<Telefon, string?>(new() { ["Ipucu"] = "Şubenin sabit hattı" });
        Assert.Equal("Şubenin sabit hattı", v.Find("#t-ipucu").TextContent.Trim());
    }

    // ── (3) Para: varsayılan 2 ondalık ───────────────────────────────────────
    [Fact]
    public void T3_Para_Tur_verilmese_de_2_ondalik_fazlasi_hata_ve_kilit()
    {
        var e = Ciz<Para, decimal?>();
        e.Find("#t").Input("1234,5678");
        var beklenen = GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.SayiOndalik, 2), "tr");
        Assert.Equal(beklenen, e.Find("#t-hata").TextContent.Trim());
        Assert.True(kapi.Var);
        Assert.Equal(beklenen, kapi.Sebep("tr"));
        Assert.Null(son);   // kapılı formda geçersiz değer çağırana GİTMEZ
        e.Find("#t").Input("1234,56");
        Assert.Empty(e.FindAll("#t-hata"));
        Assert.False(kapi.Var);
        Assert.Equal(1234.56m, son);
    }

    [Fact]
    public void T3_Para_EnCokOndalik_veren_cagiranin_siniri_korunur()
    {
        var e = Ciz<Para, decimal?>(new() { ["EnCokOndalik"] = (int?)4 });
        e.Find("#t").Input("1,2345");
        Assert.Empty(e.FindAll("#t-hata"));
        Assert.Equal(1.2345m, son);
    }

    // ── (6) Tarih: eksik tarih alandan çıkınca hata; çağıran null'u yok sayınca da kalır ──
    [Fact]
    public void T3_Tarih_eksik_tarih_alandan_cikinca_hata_cagiran_eski_degeri_geri_verse_de()
    {
        var eski = new DateOnly(2026, 10, 1);
        var e = Render<Tarih>(p => p
            .Add(x => x.ElemanId, "t").Add(x => x.Etiket, "Başlangıç")
            .Add(x => x.Deger, eski)
            .Add(x => x.DegerChanged, EventCallback.Factory.Create<DateOnly?>(this, v => son = v)));
        e.Find("#t").Input("3112");
        e.Find("#t").Change("31.12");
        // rapor süzgeci gibi: çağıran null'u YOK SAYAR, eski değeri yeniden verir
        e.Render(p => p.Add(x => x.Deger, eski));
        Assert.Equal("31.12", e.Find("#t").GetAttribute("value"));
        Assert.Equal(GirdiSozlugu.Tr.TarihBicim, e.Find("#t-hata").TextContent.Trim());
        // çağıran değerini GERÇEKTEN değiştirirse kutu onu gösterir
        e.Render(p => p.Add(x => x.Deger, new DateOnly(2026, 10, 5)));
        Assert.Equal("05.10.2026", e.Find("#t").GetAttribute("value"));
    }

    [Fact]
    public void T3_TarihAralik_eksik_baslangic_cikista_hata_cagiran_eski_degeri_geri_verse_de()
    {
        var bas = new DateOnly(2026, 10, 1);
        var bit = new DateOnly(2026, 10, 8);
        var e = Render<TarihAralik>(p => p
            .Add(x => x.ElemanId, "a").Add(x => x.Etiket, "Aralık")
            .Add(x => x.Bas, bas).Add(x => x.Bit, bit)
            .Add(x => x.BasChanged, EventCallback.Factory.Create<DateOnly?>(this, v => son = v)));
        e.Find("#a-bas").Input("3112");
        e.Find("#a-bas").Change("31.12");
        e.Render(p => p.Add(x => x.Bas, bas).Add(x => x.Bit, bit));
        Assert.Equal("31.12", e.Find("#a-bas").GetAttribute("value"));
        Assert.Contains(GirdiSozlugu.Tr.TarihBicim, e.Find("#a-hata").TextContent);
    }

    // ── (2) Tarih genişliği: yan yana kullanımda yıl kesilmez ────────────────
    [Fact]
    public void T3_Tarih_kutusu_en_kucuk_genislik_10_karakter_artı_dolgu()
    {
        var css = File.ReadAllText(Path.Combine(KokDizin(), "Kuyumcu.Web", "wwwroot", "css", "girdi.css"));
        Assert.Contains(".alan-girdi-satir > .girdi.tarih-girdi { min-width: calc(10ch + 1.5rem + 2px); }", css);
        // Tur 4 (gerçek Chrome): sarmalayıcı da kutu + simgeden dar kalamaz (yandaki alanın üstüne binme).
        Assert.Contains(".alan:has(> .alan-kontrol .tarih-girdi) { min-width: min-content; }", css);
    }

    internal static string KokDizin()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Kuyumcu.sln"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("Kuyumcu.sln bulunamadı");
    }

    // ── (7) yabancı IBAN mesajı ──────────────────────────────────────────────
    [Fact]
    public void T3_Yabanci_IBAN_eksik_ayri_kod_TR_ile_baslar_demez()
    {
        var h = IbanDogrulayici.Denetle("GB82 WEST 1234");
        Assert.Equal(GirdiKodu.IbanYabanciBicim, h?.Kod);
        var m = GirdiSozlugu.Mesaj(h!.Value, "tr");
        Assert.DoesNotContain("TR ile başlar", m);
        Assert.Contains("12", m);
        Assert.False(string.IsNullOrWhiteSpace(GirdiSozlugu.Mesaj(h.Value, "en")));
        Assert.Equal(GirdiKodu.IbanBicim, IbanDogrulayici.Denetle("1234")?.Kod);   // ülke kodu yoksa eski metin
    }
}

// ── Ekran: /isletme kimlik + şube çekmecesi (kilit = alan altı metni) ─────────────
public sealed class GirdiAilesi2Tur3IsletmeEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    public GirdiAilesi2Tur3IsletmeEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<KullaniciGorunumServisi>();
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "QA", "Test", "qa@ornek.com", "SAHIP", "s1"));
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Isletme> KimlikAc()
    {
        var c = Render<Kuyumcu.Web.Pages.Isletme>();
        c.WaitForElement("#i-kimlik-duzenle").Click();
        c.WaitForElement("#cekmece-isletme-kimlik");
        return c;
    }

    [Theory]
    [InlineData("0000000000", "TelefonHane9")]
    [InlineData("5321234", "TelefonHane7")]          // eskiden kilitte "yalnız rakam, boşluk …" yazıyordu
    [InlineData("05001112233", "TelefonAralik")]     // eskiden Kaydet AÇIKTI
    public void T3_Isletme_kimlik_telefon_gecersiz_Kaydet_kilitli_kilit_metni_alan_alti_ile_ayni(string girilen, string ad)
    {
        var c = KimlikAc();
        c.Find("#ikimlik-telefon").Input(girilen);
        c.Find("#ikimlik-telefon").Change(Telefon.Maskele(girilen));
        var beklenen = GirdiAilesi2Tur3Testleri.Beklenen(ad);
        c.WaitForAssertion(() => Assert.Equal(beklenen, c.Find("#ikimlik-telefon-hata").TextContent.Trim()));
        Assert.True(c.Find("#cekmece-isletme-kimlik-kaydet").HasAttribute("disabled"));
        var kilit = c.Find("#cekmece-isletme-kimlik-kilit").TextContent.Trim();
        Assert.EndsWith(beklenen, kilit);
        Assert.DoesNotContain("yalnız rakam", kilit);
        c.Find("#cekmece-isletme-kimlik-kaydet").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH" && i.Yol.Contains("tenant/kimlik"));

        c.Find("#ikimlik-telefon").Input("0532 411 22 33");
        c.Find("#ikimlik-telefon").Change("0532 411 22 33");
        c.WaitForAssertion(() => Assert.False(c.Find("#cekmece-isletme-kimlik-kaydet").HasAttribute("disabled")));
        Assert.Empty(c.FindAll("#ikimlik-telefon-hata"));
    }

    [Fact]
    public void T3_Isletme_sube_telefon_aralik_disi_Kaydet_kilitli_kilit_metni_alan_alti_ile_ayni()
    {
        var c = Render<Kuyumcu.Web.Pages.Isletme>();
        c.WaitForElement("#s-yeni-sube-ac").Click();
        c.WaitForElement("#cekmece-sube");
        c.Find("#s-ad").Input("PAKETGIRDI-AILESI-2-TEST Şube");
        c.Find("#s-telefon").Input("05001112233");
        c.Find("#s-telefon").Change("0500 111 22 33");
        c.WaitForAssertion(() => Assert.Equal(GirdiSozlugu.Tr.TelefonAralik, c.Find("#s-telefon-hata").TextContent.Trim()));
        Assert.True(c.Find("#cekmece-sube-kaydet").HasAttribute("disabled"));
        Assert.EndsWith(GirdiSozlugu.Tr.TelefonAralik, c.Find("#cekmece-sube-kilit").TextContent.Trim());
    }

    private sealed class SahteApi(List<(string, string, string)> kayit) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, istek.RequestUri!.PathAndQuery, govde));
            var g = yol switch
            {
                var y when y.EndsWith("/admin/tenant/kimlik") || y.EndsWith("/admin/tenant") =>
                    """{"id":"t1","ad":"QA-TEST Kuyumculuk","plan":"BASLANGIC","aiEtkin":false,"varsayilanArayuzModu":"PROFESYONEL","adres":null,"telefon":null,"vergiDairesi":null,"vergiNo":"1234567890","kurKaynagi":"ALTINKAYNAK","maliyetYontemi":"FIFO"}""",
                var y when y.EndsWith("/subeler") =>
                    """[{"id":"s1","ad":"Merkez","adres":"Çarşı","telefon":"05321112233","aktif":true}]""",
                var y when y.EndsWith("/abonelik") =>
                    """{"planKodu":"BASLANGIC","planAdi":"Başlangıç","yazabilirMi":true,"planlar":[],"gecmis":[]}""",
                var y when y.EndsWith("/yetkiler/benim") => """{"moduller":["cari","urun"]}""",
                var y when y.EndsWith("/kullanici/gorunum", StringComparison.Ordinal) =>
                    istek.Method == HttpMethod.Patch ? B1SahteGorunumYaniti.Sarmal : B1SahteGorunumYaniti.Govde,
                var y when y.EndsWith("/gorunum", StringComparison.Ordinal) => B1SahteGorunumYaniti.Kiraci,
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(g, Encoding.UTF8, "application/json") };
        }
    }
}

// ── Sunucu: aynı kural, aynı metin, 400 ────────────────────────────────────────
[Collection("http")]
public sealed class GirdiAilesi2Tur3SunucuTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static Task<HttpResponseMessage> Ham(HttpClient c, HttpMethod m, string yol, string json) =>
        c.SendAsync(new HttpRequestMessage(m, yol) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    /// <summary>400 + alan; gövde AYRIŞTIRILIR (Türkçe kaçışlar). Dönen: alanın ilk iletisi.</summary>
    private static async Task<string> Red(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var alanlar = JsonDocument.Parse(govde).RootElement.GetProperty("alanlar");
        var p = alanlar.EnumerateObject().First(x => x.Name.Equals(alan, StringComparison.OrdinalIgnoreCase));
        return p.Value.EnumerateArray().First().GetString() ?? "";
    }

    [Theory]
    [InlineData("0000000000", "TelefonHane9")]
    [InlineData("05001112233", "TelefonAralik")]
    public async Task T3_Isletme_kimlik_PATCH_gecersiz_telefon_400_ekranla_ayni_metin(string tel, string ad)
    {
        var k = YeniKiraci("GIRDI2T3 kimlik " + ad);
        var c = f.Ortam.Istemci(k, "SAHIP");
        var m = await Red(await Ham(c, HttpMethod.Patch, "/api/v1/admin/tenant/kimlik", JsonSerializer.Serialize(new { telefon = tel })), "telefon");
        Assert.Equal(GirdiAilesi2Tur3Testleri.Beklenen(ad), m);
        using (var db = f.Vt.OlusturSistem())
            Assert.Null((await db.Tenants.AsNoTracking().SingleAsync(t => t.Id == k.TenantId)).Telefon);
        var ok = await Ham(c, HttpMethod.Patch, "/api/v1/admin/tenant/kimlik", """{"telefon":"0212 555 01 01"}""");
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task T3_Sube_POST_ve_PATCH_gecersiz_telefon_400_ekranla_ayni_metin()
    {
        var k = YeniKiraci("GIRDI2T3 sube");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var m = await Red(await Ham(c, HttpMethod.Post, "/api/v1/subeler", """{"ad":"T3 Şube","telefon":"05001112233"}"""), "telefon");
        Assert.Equal(GirdiSozlugu.Tr.TelefonAralik, m);
        var m2 = await Red(await Ham(c, HttpMethod.Patch, $"/api/v1/subeler/{k.SubeId}", """{"telefon":"0000000000"}"""), "telefon");
        Assert.Equal(GirdiAilesi2Tur3Testleri.Beklenen("TelefonHane9"), m2);
        var ok = await Ham(c, HttpMethod.Post, "/api/v1/subeler", """{"ad":"T3 Şube 2","telefon":"0532 411 22 33"}""");
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task T3_Cari_POST_aralik_disi_telefon_400_ekranla_ayni_metin()
    {
        var k = YeniKiraci("GIRDI2T3 cari");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var m = await Red(await Ham(c, HttpMethod.Post, "/api/v1/cari", """{"ad":"T3 Müşteri","tur":"MUSTERI","telefon":"05001112233"}"""), "telefon");
        Assert.Equal(GirdiSozlugu.Tr.TelefonAralik, m);
    }

    [Fact]
    public async Task T3_Banka_hareketi_2den_fazla_ondalik_400_yuvarlanmaz()
    {
        var k = YeniKiraci("GIRDI2T3 banka");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var ac = await c.PostAsJsonAsync("/api/v1/bankalar", new { bankaAdi = "T3", hesapAdi = "Vadesiz", iban = TestIban.Uret(), paraBirimi = "TRY" });
        Assert.True(ac.IsSuccessStatusCode, await ac.Content.ReadAsStringAsync());
        var id = JsonDocument.Parse(await ac.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString()!;
        var m = await Red(await Ham(c, HttpMethod.Post, $"/api/v1/bankalar/{id}/hareket", """{"yon":"GELEN","tutar":1234.5678}"""), "tutar");
        Assert.Equal(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.SayiOndalik, 2), "tr"), m);
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(0, await db.BankaHareketleri.AsNoTracking().CountAsync(x => x.TenantId == k.TenantId));
        var ok = await Ham(c, HttpMethod.Post, $"/api/v1/bankalar/{id}/hareket", """{"yon":"GELEN","tutar":1234.57}""");
        Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
        using var db2 = f.Vt.OlusturSistem();
        Assert.Equal(1234.57m, (await db2.BankaHareketleri.AsNoTracking().SingleAsync(x => x.TenantId == k.TenantId)).Tutar);
    }

    [Fact]
    public async Task T3_Banka_yabanci_eksik_IBAN_400_TR_ile_baslar_demez()
    {
        var k = YeniKiraci("GIRDI2T3 yabanci");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var m = await Red(await Ham(c, HttpMethod.Post, "/api/v1/bankalar",
            """{"bankaAdi":"HSBC","hesapAdi":"GBP","iban":"GB82 WEST 1234","paraBirimi":"GBP"}"""), "iban");
        Assert.Equal(GirdiSozlugu.Mesaj(new GirdiHatasi(GirdiKodu.IbanYabanciBicim, 12), "tr"), m);
    }
}

/// <summary>
/// Tur 4 (mutasyon açığı M3-7): işletme kimliği PATCH'inde telefon kuralı HTTP yolunda hem DTO
/// özniteliğinde ([Telefon]) hem serviste var; HTTP testi yalnız özniteliği ölçüyordu. Servis yolu
/// (DataAnnotations koşmaz) AYRI ölçülür: aynı Core kuralı, aynı sözlük metni, satır değişmez.
/// </summary>
[Collection("veritabani")]
public sealed class GirdiAilesi2Tur4ServisTestleri(TestVeritabani vt)
{
    [Theory]
    [InlineData("0000000000")]
    [InlineData("05001112233")]
    [InlineData("5321234")]
    public async Task T4_Isletme_kimlik_servis_yolu_gecersiz_telefon_reddeder_telefon_DEGISMEZ(string tel)
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "T4-tel-servis");
        using var ctx = vt.Olustur(k);
        var servis = new Kuyumcu.Api.Modules.Admin.AdminServisi(ctx, TestKurulum.Denetim(ctx));

        var ex = await Assert.ThrowsAsync<Kuyumcu.Api.Common.BadRequestException>(() =>
            servis.KimlikGuncelle(new Kuyumcu.Api.Modules.Admin.TenantKimlikGuncelleDto { Telefon = tel }, k.Islem));
        Assert.Equal(GirdiSozlugu.Mesaj(TelefonDogrulayici.Denetle(tel)!.Value, "tr"), ex.Message);

        using var oku = vt.OlusturSistem();
        Assert.Null(oku.Tenants.AsNoTracking().Single(t => t.Id == k.TenantId).Telefon);
    }

    [Fact]
    public async Task T4_Isletme_kimlik_servis_yolu_gecerli_telefon_yazilir()
    {
        using var db = vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "T4-tel-servis-ok");
        using var ctx = vt.Olustur(k);
        var servis = new Kuyumcu.Api.Modules.Admin.AdminServisi(ctx, TestKurulum.Denetim(ctx));

        await servis.KimlikGuncelle(new Kuyumcu.Api.Modules.Admin.TenantKimlikGuncelleDto { Telefon = "0212 555 01 01" }, k.Islem);

        using var oku = vt.OlusturSistem();
        Assert.False(string.IsNullOrWhiteSpace(oku.Tenants.AsNoTracking().Single(t => t.Id == k.TenantId).Telefon));
    }
}
