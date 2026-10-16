using System.Net;
using Bunit;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-65 — ONAY KİPİ STANDARDI [bUnit] (tur 8 · görsel E49)
//
// Sınanan sözleşme:
//   · YIKICI eylem (varsayılana dön · pasife al · erişim kapat) ONAY KİPİ
//     açılmadan istek ATMAZ; onay düğmesi tıklanınca GERÇEK istek gider.
//   · Başlık eylemi ADLANDIRIR; onay ve vazgeç düğmeleri FARKLI FİİL taşır
//     (çağıran "İptal" / "İptal et" verse bile vazgeç "Vazgeç" basılır).
//   · Geri dönülemez eylemde type-to-confirm: kelime yazılmadan onay KAPALI
//     ve kilidin sebebi YAZILI (UX-S30).
//   · Yıkıcı OLMAYAN geri dönüş (aktifleştir) onaysız çalışır — davranış
//     değişmedi.
//
// ⚠ Sahte API '{}' varsayılanıyla bırakılmadı: her ekranın kullandığı uçlar
//   gerçek sözleşme alan adlarıyla yanıt verir; tanınmayan uç 404 döner.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>P-65 testlerinin ortak yönlendirmeli sahte API'si (yol → yanıt).</summary>
internal sealed class P65SahteApi(
    List<(string Metot, string Yol, string Govde)> kayit,
    Func<string, string, string?> yonlendir) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage istek, CancellationToken iptal)
    {
        var yol = istek.RequestUri!.PathAndQuery;
        var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
        lock (kayit) kayit.Add((istek.Method.Method, yol, govde));
        var g = yonlendir(istek.Method.Method, yol);
        return new HttpResponseMessage(g is null ? HttpStatusCode.NotFound : HttpStatusCode.OK)
        {
            Content = new StringContent(g ?? """{"message":"sahte API bu ucu tanımıyor"}""",
                System.Text.Encoding.UTF8, "application/json"),
        };
    }

    /// <summary>Standart bUnit servisleri + bu sahte API.</summary>
    public static void Kur(BunitContext b, List<(string, string, string)> kayit, Func<string, string, string?> yonlendir)
    {
        b.JSInterop.Mode = JSRuntimeMode.Loose;
        b.Services.AddScoped<DilServisi>();
        b.Services.AddScoped<OturumServisi>();
        b.Services.AddScoped<ArayuzModuServisi>();
        b.Services.AddScoped<TemaServisi>();
        b.Services.AddScoped<CanliKurServisi>();
        b.Services.AddScoped<YetkiServisi>();
        b.Services.AddScoped<DuzeltmePolitikaServisi>();
        b.Services.AddScoped(_ => new HttpClient(new P65SahteApi(kayit, yonlendir))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        b.Services.AddScoped<ApiIstemci>();
    }
}

public sealed class OnayKipiTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    // ── Sahte veriler (sunucu sözleşmesi alan adlarıyla) ─────────────────────
    private const string PanoGuncel = """
        {"kaynak":"ALTINKAYNAK","zaman":"2026-09-24T08:00:00Z",
         "satirlar":[{"kod":"GRAM","etiket":"Gram Altın","alis":6800.00,"satis":6900.00,"birim":"GRAM"}],
         "atlananlar":[],"tavanSatir":16,"okunabilirlikEsigi":12,"okunabilirlikUyarisi":false}
        """;

    private const string PanoAyarlari = """
        {"ayarlar":[
           {"kod":"GRAM","ad":"Gram Altın","etiket":null,"sira":0,"alisMarjTl":-10.00,"satisMarjTl":20.00,
            "yuvarlamaAdimi":10,"aktif":true,"ozel":true}],
         "yuvarlamaAdimlari":[0,1,5,10,50,100],"tavanSatir":16,"okunabilirlikEsigi":12,"aktifSayisi":1}
        """;

    private const string PanoDonulmus = """
        {"ayarlar":[
           {"kod":"GRAM","ad":"Gram Altın","etiket":null,"sira":0,"alisMarjTl":0,"satisMarjTl":0,
            "yuvarlamaAdimi":10,"aktif":true,"ozel":false}],
         "yuvarlamaAdimlari":[0,1,5,10,50,100],"tavanSatir":16,"okunabilirlikEsigi":12,"aktifSayisi":1}
        """;

    private const string FiyatListeleri = """
        [{"id":"fl1","ad":"Perakende","tur":"PERAKENDE","hasMarjYuzde":5,"iscilikCarpanYuzde":100,"varsayilan":true,"aktif":true},
         {"id":"fl2","ad":"Toptan","tur":"TOPTAN","hasMarjYuzde":2,"iscilikCarpanYuzde":50,"varsayilan":false,"aktif":true},
         {"id":"fl3","ad":"Eski","tur":"TOPTAN","hasMarjYuzde":1,"iscilikCarpanYuzde":50,"varsayilan":false,"aktif":false}]
        """;

    private const string Kampanyalar = """
        [{"id":"km1","ad":"Eylül","baslangic":"2026-09-01","bitis":"2026-09-20","hedefTur":"TUMU","hedefId":null,"indirimTuru":"TUTAR_YUZDE","deger":10,"aktif":true}]
        """;

    private const string Kullanicilar = """
        [{"id":"u1","ad":"QA","soyad":"Test","email":"qa@ornek.com","rol":"SAHIP","aktif":true,"sonGiris":null,"arayuzModu":null,"subeId":"s1","mfaEtkin":false},
         {"id":"u2","ad":"Ayşe","soyad":"Yılmaz","email":"ayse@ornek.com","rol":"TEZGAHTAR","aktif":true,"sonGiris":null,"arayuzModu":null,"subeId":"s1","mfaEtkin":true}]
        """;

    public OnayKipiTestleri()
    {
        P65SahteApi.Kur(this, istekler, Yonlendir);
    }

    private static string? Yonlendir(string metot, string yol) => yol switch
    {
        var y when y.Contains("/pano/guncel") => PanoGuncel,
        var y when y.Contains("/varsayilana-don") => PanoDonulmus,
        var y when y.Contains("/pano/ayarlar") => PanoAyarlari,
        var y when y.Contains("/onizleme") => """{"toplam":0,"limit":50,"offset":0,"satirlar":[]}""",
        var y when y.Contains("/fiyat/listeler") && metot == "GET" => FiyatListeleri,
        var y when y.Contains("/fiyat/listeler/") => """{"id":"fl2"}""",
        var y when y.Contains("/fiyat/kampanyalar") && metot == "GET" => Kampanyalar,
        var y when y.Contains("/fiyat/kampanyalar/") => """{"id":"km1"}""",
        var y when y.Contains("/urun-kategorileri/duz") => """[{"id":"kat1","ad":"Bilezik","ustId":null}]""",
        var y when y.Contains("/urunler") => """[{"id":"u1","ad":"Burma Bilezik","ayarId":"A22","hasGrami":9.16}]""",
        var y when y.Contains("/kur/guncel") => """{"id":"kur1","altinHasFiyati":4100}""",
        var y when y.EndsWith("/subeler") => """[{"id":"s1","ad":"Merkez","adres":null,"telefon":null,"aktif":true}]""",
        var y when y.EndsWith("/kullanicilar") => Kullanicilar,
        var y when y.Contains("/kullanicilar/") => """{"id":"u2"}""",
        _ => null,
    };

    private List<(string Metot, string Yol, string Govde)> Istekler() { lock (istekler) return [.. istekler]; }
    private void Temizle() { lock (istekler) istekler.Clear(); }

    private void OturumAc() =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "QA", "Test", "qa@ornek.com", "SAHIP", "s1"));

    // ── 1) YIKICI EYLEM ONAYSIZ ÇALIŞMAZ ─────────────────────────────────────

    [Fact]
    public void YikiciEylem_OnaysizCalismaz()
    {
        var e = Render<Kuyumcu.Web.Pages.Vitrin>();
        GocC2Ortak.AyarlariAc(e);   // GOC-C-2: ayar tablosu PRO'da da kapalı katlanırla gelir (hakem K4 /vitrin 375) — test önce açar; beklenen GEVŞETİLMEDİ.

        // (a) ÇEKMECEDEN: eskiden tek tıkla POST atıyordu (P-17). Artık onay kipi açılır.
        e.Find("#vitrin-duzenle-GRAM").Click();
        e.WaitForElement("#pano-varsayilana-don");
        Temizle();
        e.Find("#pano-varsayilana-don").Click();
        Assert.DoesNotContain(Istekler(), i => i.Metot == "POST");
        Assert.NotNull(e.Find("#vitrin-varsayilana-don-onay-kipi"));
        Assert.Equal(OnayKilitSozlugu.Tr.VitrinDonusBaslik, e.Find(".modal-baslik h2").TextContent.Trim());

        // Onay → GERÇEK istek.
        e.Find("#vitrin-varsayilana-don-evet-GRAM").Click();
        e.WaitForAssertion(() => Assert.Single(Istekler(), i =>
            i.Metot == "POST" && i.Yol.EndsWith("/pano/ayarlar/GRAM/varsayilana-don", StringComparison.Ordinal)));
        e.WaitForAssertion(() => Assert.Empty(e.FindAll("#vitrin-varsayilana-don-onay-kipi")));
    }

    [Fact]
    public void YikiciEylem_Satirdan_da_onay_kipinden_gecer_Vazgec_istek_atmaz()
    {
        var e = Render<Kuyumcu.Web.Pages.Vitrin>();
        GocC2Ortak.AyarlariAc(e);   // GOC-C-2: ayar tablosu PRO'da da kapalı katlanırla gelir (hakem K4 /vitrin 375) — test önce açar; beklenen GEVŞETİLMEDİ.
        Temizle();

        e.Find("#vitrin-varsayilana-don-GRAM").Click();
        Assert.NotNull(e.Find("#vitrin-varsayilana-don-onay-kipi"));
        e.Find("#vitrin-varsayilana-don-vazgec-GRAM").Click();

        Assert.Empty(e.FindAll("#vitrin-varsayilana-don-onay-kipi"));
        Assert.DoesNotContain(Istekler(), i => i.Metot == "POST");
    }

    [Fact]
    public void YikiciEylem_Fiyatlandirma_pasife_alma_onaysiz_PATCH_atmaz()
    {
        var e = Render<Kuyumcu.Web.Pages.Fiyatlandirma>();
        e.WaitForElement("#fl-durum-fl2");
        Temizle();

        e.Find("#fl-durum-fl2").Click();
        Assert.DoesNotContain(Istekler(), i => i.Metot == "PATCH");
        var kip = e.Find("#fl-pasif-onay-kipi");
        Assert.Contains("Toptan", kip.TextContent, StringComparison.Ordinal);
        // Geri dönüş yolu (aktifleştir) pencerede YAZILI (UX-S25).
        Assert.Contains(Sozlukler.Tr.Fiyat.Aktiflestir, e.Find("#fl-pasif-geri-donus").TextContent, StringComparison.Ordinal);

        e.Find("#fl-pasif-onayla").Click();
        e.WaitForAssertion(() =>
        {
            var p = Assert.Single(Istekler(), i => i.Metot == "PATCH" && i.Yol.EndsWith("/fiyat/listeler/fl2"));
            Assert.False(System.Text.Json.JsonDocument.Parse(p.Govde).RootElement.GetProperty("aktif").GetBoolean());
        });

        // Kampanya da aynı kalıp.
        Temizle();
        e.Find("#km-durum-km1").Click();
        Assert.DoesNotContain(Istekler(), i => i.Metot == "PATCH");
        e.Find("#km-pasif-onayla").Click();
        e.WaitForAssertion(() =>
            Assert.Single(Istekler(), i => i.Metot == "PATCH" && i.Yol.EndsWith("/fiyat/kampanyalar/km1")));
    }

    [Fact]
    public void Aktiflestirme_yikici_degildir_onaysiz_calisir_davranis_degismedi()
    {
        var e = Render<Kuyumcu.Web.Pages.Fiyatlandirma>();
        e.WaitForElement("#fl-durum-fl3");
        Temizle();

        e.Find("#fl-durum-fl3").Click();
        e.WaitForAssertion(() =>
        {
            var p = Assert.Single(Istekler(), i => i.Metot == "PATCH" && i.Yol.EndsWith("/fiyat/listeler/fl3"));
            Assert.True(System.Text.Json.JsonDocument.Parse(p.Govde).RootElement.GetProperty("aktif").GetBoolean());
        });
        Assert.Empty(e.FindAll("#fl-pasif-onay-kipi"));
    }

    [Fact]
    public void YikiciEylem_Kullanici_erisim_kapatma_onaysiz_DELETE_atmaz()
    {
        OturumAc();
        var e = Render<Kuyumcu.Web.Pages.Kullanicilar>();
        e.WaitForAssertion(() => Assert.Contains("ayse@ornek.com", e.Markup, StringComparison.Ordinal));
        Temizle();

        e.FindAll("button").First(b => b.TextContent.Trim() == Sozlukler.Tr.Kullanici.PasifeAl).Click();
        Assert.DoesNotContain(Istekler(), i => i.Metot == "DELETE");
        var kip = e.Find("#k-pasife-onay-kipi");
        Assert.Contains("ayse@ornek.com", kip.TextContent, StringComparison.Ordinal);
        Assert.Equal(OnayKilitSozlugu.Tr.KullaniciPasifBaslik, e.Find(".modal-baslik h2").TextContent.Trim());

        e.Find("#k-pasife-onayla").Click();
        e.WaitForAssertion(() =>
            Assert.Single(Istekler(), i => i.Metot == "DELETE" && i.Yol.EndsWith("/kullanicilar/u2")));
    }

    // ── 2) ONAY ve VAZGEÇ FARKLI FİİL ────────────────────────────────────────

    [Fact]
    public void OnayVeVazgecDugmeleri_AyniFiiliTasimaz()
    {
        // (a) Bileşen: /yedekler'deki hata kalıbı — "İptal et" eylemine "İptal" vazgeçi
        //     verilirse bileşen onu YOK SAYAR ve "Vazgeç" basar.
        var c = Render<OnayKipi>(p => p
            .Add(x => x.Acik, true)
            .Add(x => x.Kimlik, "deneme")
            .Add(x => x.Eylem, "Yedeklemeyi iptal et")
            .Add(x => x.OnayMetni, "İptal et")
            .Add(x => x.VazgecMetni, "İptal"));
        var onay = c.Find("#deneme-onayla").TextContent.Trim();
        var vazgec = c.Find("#deneme-vazgec").TextContent.Trim();
        Assert.Equal(OnayKilitSozlugu.Tr.Vazgec, vazgec);
        Assert.False(OnayKipi.AyniFiil(onay, vazgec), $"'{onay}' / '{vazgec}' aynı fiil");
        // Başlık EYLEMİ adlandırır.
        Assert.Equal("Yedeklemeyi iptal et", c.Find(".modal-baslik h2").TextContent.Trim());

        // (b) Yardımcı: aynı fiil kökü yakalanır (büyük/küçük harf, Türkçe İ).
        Assert.True(OnayKipi.AyniFiil("İptal", "iptal et"));
        Assert.False(OnayKipi.AyniFiil("Vazgeç", "Pasife al"));

        // (c) Bağlanan üç ekranın gerçek penceresi.
        var v = Render<Kuyumcu.Web.Pages.Vitrin>();
        GocC2Ortak.AyarlariAc(v);   // GOC-C-2: ayar tablosu PRO'da da kapalı katlanırla gelir (hakem K4 /vitrin 375) — test önce açar; beklenen GEVŞETİLMEDİ.
        v.Find("#vitrin-varsayilana-don-GRAM").Click();
        Assert.False(OnayKipi.AyniFiil(
            v.Find("#vitrin-varsayilana-don-evet-GRAM").TextContent,
            v.Find("#vitrin-varsayilana-don-vazgec-GRAM").TextContent));

        var f = Render<Kuyumcu.Web.Pages.Fiyatlandirma>();
        f.WaitForElement("#fl-durum-fl2");
        f.Find("#fl-durum-fl2").Click();
        Assert.False(OnayKipi.AyniFiil(f.Find("#fl-pasif-onayla").TextContent, f.Find("#fl-pasif-vazgec").TextContent));
        Assert.Equal(OnayKilitSozlugu.Tr.Vazgec, f.Find("#fl-pasif-vazgec").TextContent.Trim());
    }

    // ── 3) GERİ DÖNÜLEMEZ: type-to-confirm ──────────────────────────────────

    [Fact]
    public void GeriDonulemez_eylemde_kelime_yazilmadan_onay_ACILMAZ_sebebi_yazili()
    {
        var onaylandi = 0;
        var c = Render<OnayKipi>(p => p
            .Add(x => x.Acik, true)
            .Add(x => x.Kimlik, "gd")
            .Add(x => x.Eylem, "Kaydı kalıcı olarak kapat")
            .Add(x => x.OnayMetni, "Kalıcı kapat")
            .Add(x => x.GeriDonulemez, true)
            .Add(x => x.OnayKelimesi, "KAPAT")
            .Add(x => x.Onayla, () => { onaylandi++; }));

        Assert.True(c.Find("#gd-onayla").HasAttribute("disabled"));
        Assert.Contains(OnayKilitSozlugu.Tr.GeriDonulemezUyari, c.Find("#gd-geri-donulemez").TextContent, StringComparison.Ordinal);
        Assert.Contains("KAPAT", c.Find("#gd-kilit").TextContent, StringComparison.Ordinal);

        c.Find("#gd-yazarak").Input("KAPA");
        Assert.True(c.Find("#gd-onayla").HasAttribute("disabled"));

        c.Find("#gd-yazarak").Input("KAPAT");
        Assert.False(c.Find("#gd-onayla").HasAttribute("disabled"));
        Assert.Empty(c.FindAll("#gd-kilit"));
        c.Find("#gd-onayla").Click();
        Assert.Equal(1, onaylandi);
    }
}
