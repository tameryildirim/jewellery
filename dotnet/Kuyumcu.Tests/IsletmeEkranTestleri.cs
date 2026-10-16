using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// İŞLETME VE ŞUBE EKRANI
//
// TESPİT (2026-09-14): tenant bilgisi, plan, AI anahtarı ve şubenin
// düzenle/detay/pasife al uçları sunucuda vardı, ekranı yoktu. Şube yalnız
// kayıt sırasında açılıyor, sonradan adı bile düzeltilemiyordu.
//
// Sınanan kapılar:
//   • şube düzenleme DETAY ucunu çeker (özetle kaydetmek adres/telefon siler)
//   • son aktif şube pasife alınamaz (işlem yapılacak şube kalmaz)
//   • başarısız plan değişikliği ekranda kalmaz
// ═════════════════════════════════════════════════════════════════════════════

public sealed class IsletmeEkranTestleri : BunitContext
{
    private readonly List<(string Metot, string Yol, string Govde)> istekler = [];
    private bool tekSube;
    private bool planReddedilsin;

    public IsletmeEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped(_ => new HttpClient(
            new SahteApi(istekler, () => tekSube, () => planReddedilsin))
        {
            BaseAddress = new Uri("http://test.local/"),
        });
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ApiIstemci>();
        // UX-1-C4: İşletme ekranı artık ProAlan kullanıyor (basit modda gelişmiş
        // kimlik alanları katlanır). Yalnız DI kaydı eklendi — hiçbir İDDİA
        // değiştirilmedi (K4: mevcut testler zayıflatılmaz).
        Services.AddScoped<ArayuzModuServisi>();
    }

    private sealed class SahteApi(
        List<(string, string, string)> kayit,
        Func<bool> tekSubeMi,
        Func<bool> planRed) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.PathAndQuery;
            var govde = istek.Content is null ? "" : await istek.Content.ReadAsStringAsync(iptal);
            kayit.Add((istek.Method.Method, yol, govde));

            if (yol.EndsWith("/admin/plan") && planRed())
                return new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("""{"message":"Plan değiştirilemedi"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            var g = yol switch
            {
                var y when y.EndsWith("/subeler/s1") => """
                    {"id":"s1","ad":"Merkez","adres":"Şişli, İstanbul",
                     "telefon":"02123334455","aktif":true}
                    """,
                var y when y.EndsWith("/subeler") => tekSubeMi()
                    ? """[{"id":"s1","ad":"Merkez","adres":null,"telefon":null,"aktif":true}]"""
                    : """
                      [{"id":"s1","ad":"Merkez","adres":null,"telefon":null,"aktif":true},
                       {"id":"s2","ad":"Şube 2","adres":null,"telefon":null,"aktif":true}]
                      """,
                // PAKET P-27: abonelik ucu GERÇEKÇİ yanıt döner ({} varsayılanı
                // ekranı "plan yok" durumuna düşürürdü). Kiracı KURUMSAL'dadır:
                // indirilebilir planlar PRO ve BASLANGIC'tır, yükseltme YOKTUR.
                var y when y.Contains("/abonelik") => """
                    {"planKodu":"KURUMSAL","planAdi":"Kurumsal","seviye":30,
                     "donem":{"id":"a1","planKodu":"KURUMSAL","baslangic":"2026-09-01T00:00:00Z",
                              "bitis":null,"durum":"AKTIF","kaynak":"SISTEM",
                              "gerekce":null,"operatorAdi":"operator"},
                     "kalanGun":null,"suresiDolduMu":false,"yazabilirMi":true,
                     "yukseltmeKiraciyaKapali":true,
                     "yukseltmeKapaliSebebi":"Plan yükseltme işletme hesabından yapılamaz.",
                     "planlar":[{"kod":"BASLANGIC","ad":"Başlangıç","seviye":10,"aktif":true},
                                {"kod":"PRO","ad":"Pro","seviye":20,"aktif":true},
                                {"kod":"KURUMSAL","ad":"Kurumsal","seviye":30,"aktif":true}],
                     "indirilebilirPlanlar":["PRO","BASLANGIC"],
                     "gecmis":[{"id":"a1","planKodu":"KURUMSAL","baslangic":"2026-09-01T00:00:00Z",
                                "durum":"AKTIF","kaynak":"SISTEM"}],
                     "gecmisSiniri":50,"gecmisSinirinaDegdi":false}
                    """,
                var y when y.Contains("/admin") => """
                    {"id":"t1","ad":"Demo Kuyumcu","plan":"KURUMSAL","aiEtkin":false}
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

    private IRenderedComponent<Kuyumcu.Web.Pages.Isletme> Ciz()
    {
        var e = Render<Kuyumcu.Web.Pages.Isletme>();
        istekler.Clear();
        return e;
    }

    private static AngleSharp.Dom.IElement Dugme(
        IRenderedComponent<Kuyumcu.Web.Pages.Isletme> e, string metin)
        => e.FindAll("button").First(b => b.TextContent.Contains(metin));

    // ── İşletme bilgisi ──────────────────────────────────────────────────────

    [Fact]
    public void Acilista_tenant_ve_subeler_cekilir()
    {
        Render<Kuyumcu.Web.Pages.Isletme>();

        Assert.Contains(istekler, i => i.Yol.EndsWith("/admin/tenant"));
        Assert.Contains(istekler, i => i.Yol.EndsWith("/subeler"));
    }

    [Fact]
    public void Isletme_adi_ve_mevcut_plan_gosterilir()
    {
        var e = Ciz();

        Assert.Contains("Demo Kuyumcu", e.Markup);
        // PAKET P-27: plan artık abonelik kartından okunur; seçicide GEÇERLİ
        // plan seçilidir ve yalnız indirme seçenekleri listelenir.
        Assert.Equal("KURUMSAL", SeciciYardim.SeciliDeger(e, "i-plan"));
        Assert.Contains("Kurumsal", e.Find("#i-abonelik-plan").TextContent);
    }

    /// <summary>
    /// PAKET P-27 · F-3: seçicide YÜKSELTME seçeneği HİÇ YOKTUR ve yükseltmenin
    /// neden kapalı olduğu ekranda YAZILIDIR (UX-S30).
    /// </summary>
    [Fact]
    public void P27_secicide_yukseltme_secenegi_YOK_ve_sebep_yazili()
    {
        var e = Ciz();

        var secenekler = SeciciYardim.Secenekler(e, "i-plan").Select(o => o.Deger()).ToList();
        Assert.Equal(["KURUMSAL", "PRO", "BASLANGIC"], secenekler);
        Assert.Contains("yükseltme", e.Find("#i-plan-yukseltme-notu").TextContent,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>PAKET P-27: dönem geçmişi ve sunucu sınırı ekranda yazılır (UX-S29).</summary>
    [Fact]
    public void P27_donem_gecmisi_ve_sinir_notu_gosterilir()
    {
        var e = Ciz();

        Assert.Contains("KURUMSAL", e.Find("#i-abonelik-gecmis").TextContent);
        Assert.Contains("50", e.Find("#i-abonelik-sinir").TextContent);
    }

    /// <summary>
    /// PAKET P-27: seçim TEK BAŞINA yazmaz — plan indirme geri alınamaz bir
    /// daraltmadır, önce onay modalı gelir (DoD EK md.4).
    /// </summary>
    [Fact]
    public void Plan_degisikligi_ONAYDAN_SONRA_PATCH_ile_gonderilir()
    {
        var e = Ciz();
        SeciciYardim.Sec(e, "i-plan", "PRO");

        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");   // onaysız yazma YOK
        e.Find("#i-plan-onay-uygula").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "PATCH");
        Assert.EndsWith("/admin/plan", istek.Yol);
        Assert.Equal("PRO", Alan(istek.Govde, "plan"));
    }

    /// <summary>Onaydan vazgeçilince seçim GERİ ALINIR ve istek atılmaz.</summary>
    [Fact]
    public void P27_onaydan_vazgecilince_istek_atilmaz_ve_secim_geri_doner()
    {
        var e = Ciz();
        SeciciYardim.Sec(e, "i-plan", "PRO");
        e.Find("#i-plan-onay-vazgec").Click();

        Assert.DoesNotContain(istekler, i => i.Metot == "PATCH");
        Assert.Equal("KURUMSAL", SeciciYardim.SeciliDeger(e, "i-plan"));
    }

    [Fact]
    public void Basarisiz_plan_degisikligi_ekranda_KALMAZ()
    {
        // Sunucu reddettiği hâlde açılır liste "PRO" gösterirse, kullanıcı
        // sahip olmadığı bir planı satın almış sanır.
        planReddedilsin = true;
        var e = Ciz();
        SeciciYardim.Sec(e, "i-plan", "PRO");
        e.Find("#i-plan-onay-uygula").Click();

        Assert.Equal("KURUMSAL", SeciciYardim.SeciliDeger(e, "i-plan"));
        Assert.Contains("Plan değiştirilemedi", e.Markup);
    }

    [Fact]
    public void AI_anahtari_PATCH_ile_gonderilir()
    {
        var e = Ciz();
        e.Find("input[type=checkbox]").Change(true);

        var istek = Assert.Single(istekler, i => i.Metot == "PATCH");
        Assert.EndsWith("/admin/ai", istek.Yol);
        Assert.Equal("True", Alan(istek.Govde, "etkin"));
    }

    // ── Şube ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Sube_duzenleme_DETAY_ucunu_ceker()
    {
        // Liste adres/telefonu boş taşıyor; detay çekilmezse kaydette silinir.
        var e = Ciz();
        Dugme(e, "Düzenle").Click();

        Assert.Contains(istekler, i => i.Metot == "GET" && i.Yol.EndsWith("/subeler/s1"));
    }

    [Fact]
    public void Sube_duzenlemede_adres_ve_telefon_KORUNUR()
    {
        var e = Ciz();
        Dugme(e, "Düzenle").Click();
        istekler.Clear();
        Dugme(e, "Kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "PATCH");
        Assert.EndsWith("/subeler/s1", istek.Yol);
        Assert.Equal("Şişli, İstanbul", Alan(istek.Govde, "adres"));
        Assert.Equal("02123334455", Alan(istek.Govde, "telefon"));
    }

    [Fact]
    public void Yeni_sube_POST_ile_kaydedilir()
    {
        var e = Ciz();
        Dugme(e, "Yeni şube").Click();
        e.Find("#s-ad").Input("Kadıköy");
        Dugme(e, "Kaydet").Click();

        var istek = Assert.Single(istekler, i => i.Metot == "POST");
        Assert.EndsWith("/subeler", istek.Yol);
        Assert.Equal("Kadıköy", Alan(istek.Govde, "ad"));
    }

    [Fact]
    public void SON_aktif_sube_pasife_ALINAMAZ()
    {
        // Tek şube kapanırsa satış, alış ve emanet dâhil her akış kilitlenir.
        tekSube = true;
        var e = Ciz();

        Assert.True(Dugme(e, "Pasife Al").HasAttribute("disabled"));
        Assert.Contains("Tek aktif şube pasife alınamaz", e.Markup);
    }

    [Fact]
    public void Iki_sube_varken_pasife_alma_ONAY_sonrasi_DELETE_gonderir()
    {
        var e = Ciz();
        Dugme(e, "Pasife Al").Click();
        Assert.DoesNotContain(istekler, i => i.Metot == "DELETE");

        e.FindAll("button").Where(b => b.TextContent.Contains("Pasife Al")).Last().Click();

        Assert.Contains(istekler, i => i.Metot == "DELETE" && i.Yol.EndsWith("/subeler/s1"));
    }
}
