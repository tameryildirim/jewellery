using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-50 — NEGATİF KASADAN ÇIKIŞ YOLU: /gider formunda bakiye + eylem
// bağlantılı hata [bUnit]
//
// Kaynak: QA turu 8 triyajı (KRİTİK). Canlı fikstürde kasa eksi bakiyede;
// P-44'ten beri sunucu kapısı (KasaBakiyeKapisi) HER gideri reddeder ve
// mesajındaki sayılar InvariantCulture'dır ("-738298.80 TRY"). Ekran bu ham
// metni hata bandına basıyor, kullanıcıya hiçbir çıkış yolu göstermiyordu.
//
// Sınananlar (spesifikasyonun test listesi birebir + iki rota kanıtı):
//   1. Kasa seçilince bakiye tr-TR para biçiminde görünür, eksi bakiye kırmızı rozetle
//   2. Eksi bakiyeli kasada formun üstünde kalıcı uyarı şeridi + /raporlar/kasa bağlantısı
//   3. Yetersiz bakiye reddinde bant İKİ eylem bağlantısı taşır (sorgu dolu)
//   4. Bant role=alert taşır
//   5. Ham '-738298.80 TRY' biçimi ekranın HİÇBİR yerinde geçmez
//   +  bağlantıların hedefleri GERÇEKTEN açılır (/kasalar giriş formu · virman hedefi)
//
// ⚠ Sahte API "{}" varsayılanıyla bırakılmadı: POST /giderler gerçek P-44
//   kapı mesajının BİREBİR biçimini (KasaBakiyeKapisi.Mesaj) 400 ile döndürür.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GiderEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];

    /// <summary>Ekranda BAYAT bakiye (10.000) varken sunucunun gerçek bakiyesi eksidir.</summary>
    private const string SunucuRedMesaji =
        "'Merkez Kasa' kasasının bakiyesi yetersiz: -738298.80 TRY var, 1500.00 TRY isteniyor. Nakit kasa eksiye düşemez.";

    private const string TekKasa =
        """[{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":10000,"aktif":true,"subeId":null}]""";

    private const string IkiKasa = """
        [{"id":"ks1","ad":"Merkez Kasa","paraBirimi":"TRY","bakiye":10000,"aktif":true,"subeId":null},
         {"id":"ks2","ad":"Şube Kasa","paraBirimi":"TRY","bakiye":-738298.80,"aktif":true,"subeId":null}]
        """;

    private string kasaYaniti = TekKasa;
    private bool giderReddedilir;

    public GiderEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<UyumServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(istekler, () => kasaYaniti, () => giderReddedilir))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class SahteApi(
        List<(string, string, string)> kayit,
        Func<string> kasalar,
        Func<bool> reddet) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            var metot = istek.Method.Method;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((metot, istek.RequestUri!.PathAndQuery, govde));

            if (yol.EndsWith("/giderler") && metot == "POST" && reddet())
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(new { statusCode = 400, message = SunucuRedMesaji }),
                        System.Text.Encoding.UTF8, "application/json"),
                };

            var g = yol switch
            {
                var y when y.EndsWith("/gider-kategorileri") =>
                    """{"kategoriler":[{"id":"gk1","ad":"Kira","aciklama":null,"aktif":true,"giderAdedi":0,"giderToplamTl":0}],"toplam":1}""",
                var y when y.EndsWith("/giderler") && metot == "POST" =>
                    """{"id":"kh9","kasaId":"ks1","kasaAdi":"Merkez Kasa","kategoriId":"gk1","kategoriAdi":"Kira","tutar":1500,"paraBirimi":"TRY","aciklama":null,"tarih":"2026-09-23T06:00:00Z","kasaBakiyesi":8500}""",
                var y when y.EndsWith("/giderler") =>
                    """{"satirlar":[],"toplamAdet":0,"limit":100,"dahaFazlasiVar":false,"sinirNotu":"En çok 100 gider satırı gösterilir."}""",
                var y when y.EndsWith("/virman") =>
                    """{"satirlar":[],"toplam":0,"limit":50,"dahaFazlasiVar":false,"sinirNotu":"En son 50 virman gösterilir."}""",
                var y when y.Contains("/kasalar/") && y.Contains("/hareketler") => """{"hareketler":[]}""",
                var y when y.EndsWith("/kasalar") => kasalar(),
                var y when y.EndsWith("/bankalar") =>
                    """{"hesaplar":[{"id":"bh1","bankaAdi":"Ziraat","hesapAdi":"Vadesiz","hesapSahibi":null,"iban":"TR11","paraBirimi":"TRY","bakiye":5000,"aktif":true}]}""",
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

    private void OturumAc() =>
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", "SAHIP", "s1"));

    private IRenderedComponent<Kuyumcu.Web.Pages.Gider> Ekran()
    {
        OturumAc();
        var e = Render<Kuyumcu.Web.Pages.Gider>();
        e.WaitForAssertion(() => e.Find("#gd-kaydet"));
        return e;
    }

    /// <summary>Tek (bayat 10.000 TL) kasa + sunucu reddi: 1500 TL gider denenir.</summary>
    private IRenderedComponent<Kuyumcu.Web.Pages.Gider> SunucuReddiyleEkran()
    {
        giderReddedilir = true;
        var e = Ekran();
        Assert.Equal("gk1", e.Find("#gd-kategori-sabit").GetAttribute("data-deger"));   // TOPLU-1 UX-S22: tek kategori seçicisiz, kendiliğinden
        e.Find("#gd-tutar").Input("1500");
        Assert.False(e.Find("#gd-kaydet").HasAttribute("disabled"));   // ekran bakiyeyi YETERLİ sanıyor
        e.Find("#gd-kaydet").Click();
        e.WaitForAssertion(() => e.Find("#gd-yetersiz"));
        Assert.Single(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/giderler"));
        return e;
    }

    // ── 1 · Bakiye tr-TR biçiminde ──────────────────────────────────────────

    [Fact]
    public void Kasa_secilince_bakiye_TL_biciminde_gorunur()
    {
        kasaYaniti = IkiKasa;
        var e = Ekran();
        // Kasa seçilmeden bakiye satırı yok (hangi kasanın bakiyesi olduğu belirsiz olurdu).
        Assert.Empty(e.FindAll("#gd-kasa-bakiye"));

        SeciciYardim.Sec(e, "gd-kasa", "ks1");
        var arti = e.Find("#gd-kasa-bakiye");
        Assert.Contains(Bicim.Tl(10000m), arti.TextContent);
        Assert.Empty(arti.QuerySelectorAll(".rozet-kirmizi"));

        SeciciYardim.Sec(e, "gd-kasa", "ks2");
        var eksi = e.Find("#gd-kasa-bakiye");
        Assert.Contains("738.298,80", eksi.TextContent);
        Assert.Contains("₺", eksi.TextContent);
        Assert.Contains(Bicim.Tl(-738298.80m), eksi.TextContent);   // T-K1: eksi U+2212
        // Eksi bakiye KIRMIZI rozetle işaretlenir.
        Assert.NotEmpty(eksi.QuerySelectorAll(".rozet-kirmizi"));

        // Seçicideki her kasa da bakiyesini taşır (seçmeden görülür).
        var liste = SeciciYardim.ListeMetni(e, "gd-kasa");
        Assert.Contains(Bicim.Tl(10000m), liste);
        Assert.Contains("738.298,80", liste);
    }

    // ── 2 · Eksi kasa uyarı şeridi ──────────────────────────────────────────

    [Fact]
    public void Negatif_bakiyeli_kasada_uyari_seridi_ve_rapor_baglantisi_var()
    {
        kasaYaniti = IkiKasa;
        var e = Ekran();
        SeciciYardim.Sec(e, "gd-kasa", "ks1");
        Assert.Empty(e.FindAll("#gd-eksi-kasa-uyari"));   // artı bakiyede şerit YOK

        SeciciYardim.Sec(e, "gd-kasa", "ks2");
        var serit = e.Find("#gd-eksi-kasa-uyari");
        Assert.Contains("bant-uyari", serit.ClassName ?? "", StringComparison.Ordinal);
        Assert.Contains("738.298,80", serit.TextContent);
        var bag = e.Find("#gd-eksi-kasa-rapor");
        Assert.Equal("/raporlar/kasa", bag.GetAttribute("href"));

        // Bağlantı ÇALIŞIR: hedef rota gerçekten bir sayfaya bağlı.
        var rotalar = typeof(Kuyumcu.Web.Pages.KasaRaporu)
            .GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>().Select(r => r.Template);
        Assert.Contains("/raporlar/kasa", rotalar);

        // Eksi kasada kaydet istemcide KİLİTLİ ve sebebi yazılı (sunucu kapısı tek gerçek).
        Assert.Equal("gk1", e.Find("#gd-kategori-sabit").GetAttribute("data-deger"));   // TOPLU-1 UX-S22: tek kategori seçicisiz, kendiliğinden
        e.Find("#gd-tutar").Input("100");
        Assert.True(e.Find("#gd-kaydet").HasAttribute("disabled"));
        Assert.Contains("bakiyesini aşıyor", e.Find("#gd-kaydet-kilit").TextContent);
        Assert.DoesNotContain(istekler, i => i.Metot == "POST" && i.Yol.EndsWith("/giderler"));
    }

    // ── 3 · Bandın iki eylem bağlantısı ─────────────────────────────────────

    [Fact]
    public void Yetersiz_bakiye_bandi_iki_eylem_baglantisi_tasir()
    {
        var e = SunucuReddiyleEkran();
        var giris = e.Find("#gd-yetersiz #gd-yetersiz-giris");
        var virman = e.Find("#gd-yetersiz #gd-yetersiz-virman");
        Assert.Equal("/kasalar?kasa=ks1&yon=giris", giris.GetAttribute("href"));
        Assert.Equal("/gider?sekme=virman&hedef=ks1", virman.GetAttribute("href"));
        Assert.Equal("Kasaya giriş yap", giris.TextContent.Trim());
        Assert.Equal("Virman yap", virman.TextContent.Trim());
        // Sunucu reddi sayfa hata bandına HAM düşmez — tek bant, biçimlendirilmiş.
        Assert.Empty(e.FindAll("#gd-hata"));

        // İstemcinin erken uyarısı da AYNI bandı ve bağlantıları üretir (eksi kasa).
        kasaYaniti = IkiKasa;
        var e2 = Ekran();
        SeciciYardim.Sec(e2, "gd-kasa", "ks2");
        e2.Find("#gd-tutar").Input("50");
        Assert.Equal("/kasalar?kasa=ks2&yon=giris", e2.Find("#gd-yetersiz-giris").GetAttribute("href"));
        Assert.Equal("/gider?sekme=virman&hedef=ks2", e2.Find("#gd-yetersiz-virman").GetAttribute("href"));
    }

    // ── 4 · role=alert ──────────────────────────────────────────────────────

    [Fact]
    public void Yetersiz_bakiye_bandi_role_alert_tasir()
    {
        var e = SunucuReddiyleEkran();
        var bant = e.Find("#gd-yetersiz");
        Assert.Equal("alert", bant.GetAttribute("role"));
        Assert.Contains("bant-hata", bant.ClassName ?? "", StringComparison.Ordinal);
    }

    // ── 5 · Ham invariant biçim ekranda yok ─────────────────────────────────

    [Fact]
    public void Ham_invariant_tutar_bicimi_ekranda_gecmez()
    {
        var e = SunucuReddiyleEkran();
        var metin = e.Find("#gd-yetersiz-metin").TextContent;
        // Sunucunun sayıları ayrıştırılıp tr-TR biçimine çevrildi.
        Assert.Contains("738.298,80", metin);
        Assert.Contains(Bicim.Tl(1500m), metin);
        Assert.Contains("Merkez Kasa", metin);
        foreach (var ham in new[] { "-738298.80", "738298.80 TRY", "1500.00 TRY" })
            Assert.DoesNotContain(ham, e.Markup, StringComparison.Ordinal);

        // Eksi kasa listede/rozette/şeritte de ham biçimde basılmaz.
        kasaYaniti = IkiKasa;
        var e2 = Ekran();
        SeciciYardim.Sec(e2, "gd-kasa", "ks2");
        e2.Find("#gd-tutar").Input("50");
        _ = SeciciYardim.ListeMetni(e2, "gd-kasa");
        Assert.DoesNotContain("738298.80", e2.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("738298,80", e2.Markup, StringComparison.Ordinal);
    }

    // ── + · Bağlantıların hedefi GERÇEKTEN açılır ───────────────────────────

    [Fact]
    public void Kasaya_giris_baglantisi_kasalar_ekraninda_GIRIS_formunu_acar()
    {
        kasaYaniti = IkiKasa;
        OturumAc();
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/kasalar?kasa=ks2&yon=giris");
        var e = Render<Kuyumcu.Web.Pages.Kasalar>();
        e.WaitForAssertion(() => e.Find("#h-kaydet"));
        Assert.Equal("GIRIS", SeciciYardim.SeciliDeger(e, "h-tur"));
        Assert.Contains("Şube Kasa", e.Markup);
    }

    [Fact]
    public void Kasalar_sorgusu_tanimsiz_kasada_form_ACMAZ()
    {
        OturumAc();
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/kasalar?kasa=yok&yon=giris");
        var e = Render<Kuyumcu.Web.Pages.Kasalar>();
        e.WaitForAssertion(() => Assert.Contains("Merkez Kasa", e.Markup));
        Assert.Empty(e.FindAll("#h-kaydet"));
    }

    [Fact]
    public void Virman_baglantisi_virman_sekmesini_hedef_kasa_secili_acar()
    {
        kasaYaniti = IkiKasa;
        OturumAc();
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/gider?sekme=virman&hedef=ks2");
        var e = Render<Kuyumcu.Web.Pages.Gider>();
        e.WaitForAssertion(() => e.Find("#gd-v-hedef"));
        Assert.True(e.Find("#gd-sekme-virman").HasAttribute("aria-selected"));
        Assert.Equal("KASA:ks2", SeciciYardim.SeciliDeger(e, "gd-v-hedef"));
    }
}
