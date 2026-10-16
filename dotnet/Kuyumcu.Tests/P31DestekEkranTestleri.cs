using System.Net;
using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-31 (R-5 · T-4b) — EKRAN: Ayarlar destek paketi kartı, HataBandi kısa
// kodu, KabukHatasi ekran hatası kodu (bUnit).
//
// Sınanan sözleşme:
//   • kanal kapalıyken "Oluştur" KİLİTLİ ve sebebi yazılı (UX-S30, BK-17)
//   • gerekçe kısa iken kilitli + n/20 sayacı; 20'yi geçince açılır
//   • oluşturma önce satır içi ONAY ister (UX-S27), sonra GERÇEK POST atar ve
//     sonucu (dosya adı) başarı bandına yazar
//   • geçmiş boşsa BosDurum; sunucu hatasında liste boşaltılmaz, hata bandı
//     çıkar (UX-S28); konu kodu HAM basılmaz (UX-S31); DegismezIpucu var
//   • SAHIP olmayan rolde kart çizilmez ve uç ÇAĞRILMAZ
//   • HataBandi ekranda KISA kodu gösterir, panoya kısa kodu yazar
//   • KabukHatasi ekran hatasının HAM mesajını basmaz; kodu sunucudan alır
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P31DestekEkranTestleri : BunitContext
{
    private readonly List<string> istekler = [];
    private readonly List<string> govdeler = [];
    private bool kanalAcik = true;
    private bool gecmisHata;
    private string gecmisKayitlar = "[]";

    public P31DestekEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<GorunumServisi>();
        Services.AddScoped<KullaniciGorunumServisi>();
        Services.AddScoped<CanliKurServisi>();
        Services.AddScoped(_ => new HttpClient(new SahteApi(this))
        {
            BaseAddress = new Uri("http://test.local/api/v1/"),
        });
        Services.AddScoped<ApiIstemci>();
        Services.AddScoped<YetkiServisi>();
        Services.AddScoped<DuzeltmePolitikaServisi>();
        Services.AddScoped<SurumServisi>();
    }

    private string Gecmis => $$"""
        {"kanalAcik":{{(kanalAcik ? "true" : "false")}},"sinir":50,"dahaFazlasiVar":false,
         "asgariGerekce":20,"konular":["KURULUM","EGITIM","HATA","VERI","DIGER"],
         "kayitlar":{{gecmisKayitlar}}}
        """;

    private sealed class SahteApi(P31DestekEkranTestleri s) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            s.istekler.Add($"{istek.Method.Method} {yol}");
            if (istek.Content is not null) s.govdeler.Add(await istek.Content.ReadAsStringAsync(iptal));

            if (yol.EndsWith("/destek/paket/gecmis") && s.gecmisHata)
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(
                        """{"message":"Geçmiş okunamadı (sunucu)","statusCode":500,"istekKimligi":"01KSTKMLK0000000000000ZZZZ"}""",
                        System.Text.Encoding.UTF8, "application/json"),
                };

            if (yol.EndsWith("/destek/paket") && istek.Method == HttpMethod.Post)
            {
                var y = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent("{\"bicim\":1}"u8.ToArray()),
                };
                y.Content.Headers.ContentType = new("application/json");
                y.Content.Headers.ContentDisposition = new("attachment") { FileName = "destek-paketi-8K4NHDCW.json" };
                return y;
            }

            var g = yol switch
            {
                var x when x.EndsWith("/destek/paket/gecmis") => s.Gecmis,
                var x when x.EndsWith("/destek/istemci-hatasi") =>
                    """{"istekKimligi":"01KSTKMLK000000000000EKRAN1","hataKodu":"00EKRAN1"}""",
                var x when x.EndsWith("/meta/surum") =>
                    """
                    {"surum":"1.0.0","derlemeZamani":"2026-09-23T06:04:00Z","derlemeZamaniKaynagi":"damga",
                     "ortam":"Production","migrationSayisi":70,"sonMigration":"20260923174110_B3",
                     "bekleyenMigrationSayisi":0}
                    """,
                var x when x.Contains("/mfa/durum") =>
                    """{"etkin":false,"kurulumBekliyor":false,"kilitli":false,"kalanKurtarma":0}""",
                var x when x.Contains("/yetkiler/benim") => """{"moduller":["cari","urun","islem","kasa","rapor"]}""",
                var x when x.EndsWith("/kullanici/gorunum", StringComparison.Ordinal) =>
                    istek.Method == HttpMethod.Patch ? B1SahteGorunumYaniti.Sarmal : B1SahteGorunumYaniti.Govde,
                var x when x.EndsWith("/gorunum", StringComparison.Ordinal) => B1SahteGorunumYaniti.Kiraci,
                _ => "{}",
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Ayarlar> Ciz(string rol = "SAHIP")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));
        return Render<Kuyumcu.Web.Pages.Ayarlar>();
    }

    private static void GerekceYaz(IRenderedComponent<Kuyumcu.Web.Pages.Ayarlar> e, string metin)
        => e.Find("#destek-gerekce").Input(metin);

    [Fact]
    public void Kanal_kapaliyken_Olustur_kilitli_ve_sebebi_yazili()
    {
        kanalAcik = false;
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#destek-olustur")));

        GerekceYaz(e, "Satış ekranı açılmıyor, yardım lütfen.");
        Assert.True(e.Find("#destek-olustur").HasAttribute("disabled"));
        Assert.Contains("güvenlik doğrulaması", e.Find("#destek-olustur-kilit").TextContent);
        Assert.NotEmpty(e.FindAll("#destek-kanal-kapali"));
    }

    [Fact]
    public void Gerekce_kisa_iken_kilitli_sayac_gorunur_20yi_gecince_acilir()
    {
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#destek-olustur")));

        GerekceYaz(e, "kısa metin");
        Assert.True(e.Find("#destek-olustur").HasAttribute("disabled"));
        Assert.Contains("(10/20)", e.Find("#destek-olustur-kilit").TextContent);

        GerekceYaz(e, "Satış ekranı açılmıyor, yardım lütfen.");
        Assert.False(e.Find("#destek-olustur").HasAttribute("disabled"));
        Assert.Empty(e.FindAll("#destek-olustur-kilit"));
    }

    [Fact]
    public void Gecersiz_hata_kodu_alan_altinda_Turkce_mesaj_ve_kilit()
    {
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#destek-olustur")));
        GerekceYaz(e, "Satış ekranı açılmıyor, yardım lütfen.");
        e.Find("#destek-hata-kodu").Input("Ayse 053");

        Assert.True(e.Find("#destek-olustur").HasAttribute("disabled"));
        Assert.Contains("8 karakter", e.Find("#destek-hata-kodu-hata").TextContent);
    }

    [Fact]
    public void Olustur_once_onay_ister_sonra_GERCEK_POST_atar_ve_sonucu_yazar()
    {
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#destek-olustur")));
        GerekceYaz(e, "Satış ekranı açılmıyor, yardım lütfen.");

        e.Find("#destek-olustur").Click();
        // Onay adımı: henüz istek YOK.
        Assert.NotEmpty(e.FindAll("#destek-onay"));
        Assert.DoesNotContain(istekler, i => i.StartsWith("POST ") && i.EndsWith("/destek/paket"));

        // Vazgeç geri döner, istek yine yok.
        e.Find("#destek-vazgec").Click();
        Assert.Empty(e.FindAll("#destek-onay"));
        Assert.DoesNotContain(istekler, i => i.StartsWith("POST ") && i.EndsWith("/destek/paket"));

        e.Find("#destek-olustur").Click();
        e.Find("#destek-onayla").Click();
        e.WaitForAssertion(() => Assert.Contains("destek-paketi-8K4NHDCW.json", e.Find("#destek-basari").TextContent));
        Assert.Contains(istekler, i => i.StartsWith("POST ") && i.EndsWith("/destek/paket"));
        // Türkçe gövde AYRIŞTIRILARAK sınanır (JSON kaçış dizileri).
        var g = System.Text.Json.JsonDocument.Parse(govdeler.Last(x => x.Contains("gerekce"))).RootElement;
        Assert.Equal("HATA", g.GetProperty("konu").GetString());
        Assert.Equal("Satış ekranı açılmıyor, yardım lütfen.", g.GetProperty("gerekce").GetString());
    }

    [Fact]
    public void Gecmis_bos_iken_BosDurum_doluyken_konu_cevrilmis_ve_DegismezIpucu()
    {
        var bos = Ciz();
        bos.WaitForAssertion(() => Assert.NotEmpty(bos.FindAll("#destek-gecmis-bos")));
        Assert.Contains("50", bos.Find("#destek-gecmis-sinir").TextContent);
        Assert.Contains("silinemez", bos.Markup);
        Assert.Empty(bos.FindAll("#destek-hata"));
    }

    [Fact]
    public void Gecmis_satiri_konu_kodunu_HAM_basmaz()
    {
        gecmisKayitlar = """[{"tarih":"2026-09-23T17:00:00Z","paketKodu":"8K4NHDCW","konu":"VERI"}]""";
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#destek-gecmis")));
        var tablo = e.Find("#destek-gecmis").TextContent;
        Assert.Contains("8K4NHDCW", tablo);
        Assert.Contains("Veri", tablo);
        Assert.DoesNotContain("VERI", tablo);
    }

    [Fact]
    public void Sunucu_hatasinda_bos_durum_degil_hata_bandi_ve_kod_UX_S28()
    {
        gecmisHata = true;
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#destek-hata")));
        Assert.Contains("Geçmiş okunamadı", e.Find("#destek-hata").TextContent);
        Assert.Empty(e.FindAll("#destek-gecmis-bos"));
        Assert.Contains("0000ZZZZ", e.Find("#destek-hata").TextContent);
    }

    [Fact]
    public void SAHIP_olmayan_rolde_kart_cizilmez_uc_cagrilmaz()
    {
        var e = Ciz("TEZGAHTAR");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ayar-kunye-surum")));
        Assert.Empty(e.FindAll("#ayar-destek"));
        Assert.DoesNotContain(istekler, i => i.Contains("/destek/"));
    }

    // ── HataBandi / KabukHatasi ──────────────────────────────────────────────

    [Fact]
    public void HataBandi_ekranda_KISA_kodu_gosterir_tam_kimlik_makine_degeri()
    {
        var e = Render<Kuyumcu.Web.Bilesenler.HataBandi>(p => p
            .Add(x => x.Mesaj, "Sunucu hatası")
            .Add(x => x.Kimlik, "01KSTKMLK0000000000000ABCD"));
        var d = e.Find("#hata-kimlik-deger");
        Assert.Equal("0000ABCD", d.TextContent.Trim());
        Assert.Equal("01KSTKMLK0000000000000ABCD", d.GetAttribute("data-deger"));

        e.Find("#hata-kimlik-kopyala").Click();
        var cagri = Assert.Single(JSInterop.Invocations, i => i.Identifier == "kuyumcuPanoyaKopyala");
        Assert.Equal("0000ABCD", cagri.Arguments[0]);
    }

    private sealed class PatlayanSayfa : ComponentBase
    {
        protected override void OnParametersSet() =>
            throw new System.Text.Json.JsonException("The JSON value could not be converted to System.Int32. Path: $.tutar");
    }

    [Fact]
    public void Ekran_hatasi_HAM_mesaji_basmaz_kodu_SUNUCUDAN_alir_yalniz_tur_gonderir()
    {
        var e = Render<Kuyumcu.Web.Bilesenler.KabukHatasi>(p =>
            p.Add(x => x.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<PatlayanSayfa>(0);
                b.CloseComponent();
            })));

        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#hata-kimlik-rozet")));
        Assert.Equal("00EKRAN1", e.Find("#hata-kimlik-deger").TextContent.Trim());
        Assert.DoesNotContain("System.Int32", e.Markup);
        Assert.DoesNotContain("$.tutar", e.Markup);
        Assert.Contains("beklenmeyen bir sorun", e.Find("#kabuk-hata-ayrinti").TextContent);

        Assert.Single(istekler, i => i.StartsWith("POST ") && i.EndsWith("/destek/istemci-hatasi"));
        var g = Assert.Single(govdeler);
        Assert.Contains("\"tur\":\"JsonException\"", g);
        Assert.DoesNotContain("Int32", g);
    }
}
