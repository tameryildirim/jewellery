using Bunit;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// SİSTEM KÜNYESİ EKRANI + DESTEK KODU ROZETİ — paket T-4 Faz A.
//
// Sınanan sözleşme:
//   • Ayarlar künye kartı sürüm + migration sayısını BASAR
//   • bekleyen migration varsa KIRMIZI uyarı rozeti çıkar (sessiz geçilmez)
//   • künye kopyala düğmesi id ile bulunur, etkindir ve istek/etki üretir
//   • TEZGAHTAR rolünde şema ayrıntısı satırı ÇİZİLMEZ
//   • HataBandi kimlik verilince destek kodu rozetini gösterir, verilmeyince HİÇ
// ═════════════════════════════════════════════════════════════════════════════

public sealed class SurumEkranTestleri : BunitContext
{
    private readonly List<string> istekler = [];
    private int bekleyen;

    private string Kunye => $$"""
        {"surum":"1.4.2","derlemeZamani":"2026-09-18T06:10:00.0000000Z",
         "derlemeZamaniKaynagi":"damga","ortam":"Production",
         "migrationSayisi":33,"sonMigration":"20260917103000_YedekOtomasyonu",
         "bekleyenMigrationSayisi":{{bekleyen}}}
        """;

    public SurumEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped<ArayuzModuServisi>();
        Services.AddScoped<TemaServisi>();
        Services.AddScoped<GorunumServisi>();      // P-36b: Ayarlar Görünüm bölümü
        Services.AddScoped<KullaniciGorunumServisi>();  // B-1: Ayarlar "Benim görünümüm" kartı
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

    private sealed class SahteApi(SurumEkranTestleri sahip) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage istek, CancellationToken iptal)
        {
            var yol = istek.RequestUri!.AbsolutePath;
            sahip.istekler.Add($"{istek.Method.Method} {yol}");

            var g = yol switch
            {
                var y when y.EndsWith("/meta/surum") => sahip.Kunye,
                var y when y.EndsWith("/meta/surum/ayrinti") =>
                    """
                    {"surum":"1.4.2","migrationSayisi":33,
                     "sonMigration":"20260917103000_YedekOtomasyonu",
                     "uygulanmis":["20260910120000_Ilk","20260917103000_YedekOtomasyonu"],
                     "bekleyen":[],"uyari":null}
                    """,
                var y when y.Contains("/mfa/durum") =>
                    """{"etkin":false,"kurulumBekliyor":false,"kilitli":false,"kalanKurtarma":0}""",
                var y when y.Contains("/yetkiler/benim") =>
                    """{"moduller":["cari","urun","islem","kasa","rapor"]}""",
                var y when y.Contains("/kur/guncel") =>
                    """{"id":"kur-1","altinHasFiyati":6800,"gramAltinFiyati":4200}""",
                var y when y.Contains("/auth/ben") =>
                    """{"id":"u1","ad":"Test","soyad":"Kullanici","email":"t@t.local","rol":"SAHIP","subeId":null}""",
                // ⚠ B-1: "{}" varsayılanı bu uca YETMEZ (servis "yüklenmedi" sayar).
                var y when y.EndsWith("/kullanici/gorunum", StringComparison.Ordinal) =>
                    istek.Method == HttpMethod.Patch
                        ? B1SahteGorunumYaniti.Sarmal : B1SahteGorunumYaniti.Govde,
                var y when y.EndsWith("/gorunum", StringComparison.Ordinal) => B1SahteGorunumYaniti.Kiraci,
                _ => "{}",
            };

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(g, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private IRenderedComponent<Kuyumcu.Web.Pages.Ayarlar> Ciz(string rol = "SAHIP")
    {
        Services.GetRequiredService<OturumServisi>().KullaniciAta(
            new OturumKullanici("u1", "Test", "Kullanıcı", "t@ornek.com", rol, "s1"));
        return Render<Kuyumcu.Web.Pages.Ayarlar>();
    }

    [Fact]
    public void Ayarlar_kunye_karti_surum_ve_migration_sayisini_basar()
    {
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ayar-kunye-surum")));

        Assert.Equal("1.4.2", e.Find("#ayar-kunye-surum").TextContent.Trim());
        Assert.Equal("33", e.Find("#ayar-kunye-migration").TextContent.Trim());
        Assert.Contains("20260917103000_YedekOtomasyonu", e.Find("#ayar-kunye-son-migration").TextContent);
        // Künye SUNUCUDAN çekilir; ekranda sabit sürüm dizgesi yoktur.
        Assert.Contains(istekler, i => i.StartsWith("GET ") && i.EndsWith("/meta/surum"));
    }

    [Fact]
    public void Bekleyen_migration_varsa_uyari_rozeti_gorunur()
    {
        bekleyen = 2;
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ayar-kunye-bekleyen")));

        Assert.Contains("2", e.Find("#ayar-kunye-bekleyen").TextContent);
        Assert.NotEmpty(e.FindAll("#ayar-kunye-sema-eski"));
        // "Şema güncel" yeşil rozeti bu durumda ÇİZİLMEZ (sessiz yeşil yasak).
        Assert.Empty(e.FindAll("#ayar-kunye-sema"));
    }

    [Fact]
    public void Sema_guncelken_yesil_rozet_cikar()
    {
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ayar-kunye-sema")));
        Assert.Empty(e.FindAll("#ayar-kunye-bekleyen"));
    }

    [Fact]
    public void Kunye_kopyala_dugmesi_id_ile_bulunur_ve_etkin()
    {
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ayar-kunye-kopyala")));

        var dugme = e.Find("#ayar-kunye-kopyala");
        Assert.False(dugme.HasAttribute("disabled"));
        dugme.Click();

        // Interop yoksa düğme SESSİZCE ÖLMEZ: elle kopyalama yolu yazılır ve
        // künye satırı salt okunur alanda seçilebilir durur.
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ayar-kunye-elle")));
        // P-36c/3: künye satırı artık devre dışı bir kutu değil, SALT OKUNUR
        // metindir (Bilesenler/Form/SaltOkunur) — seçilip kopyalanabilir, ama
        // "düzenlenebilir alan" gibi görünmez. Kimlik (#ayar-kunye-satir) aynı.
        Assert.Contains("1.4.2", e.Find("#ayar-kunye-satir").TextContent);
    }

    [Fact]
    public void SAHIP_rolunde_ayrinti_dugmesi_gercek_istek_atar()
    {
        var e = Ciz();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ayar-kunye-ayrinti")));

        e.Find("#ayar-kunye-ayrinti").Click();

        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ayar-kunye-ayrinti-liste")));
        Assert.Contains(istekler, i => i.StartsWith("GET ") && i.EndsWith("/meta/surum/ayrinti"));
        Assert.Contains("20260917103000_YedekOtomasyonu", e.Find("#ayar-kunye-ayrinti-liste").TextContent);
    }

    [Fact]
    public void TEZGAHTAR_rolunde_migration_ayrinti_satiri_gorunmez()
    {
        var e = Ciz("TEZGAHTAR");
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#ayar-kunye-surum")));

        Assert.Empty(e.FindAll("#ayar-kunye-ayrinti"));
        // Ayrıntı ucu İSTENMEZ de (ekran sunucu kapısını zorlamaz).
        Assert.DoesNotContain(istekler, i => i.EndsWith("/meta/surum/ayrinti"));
    }

    [Fact]
    public void HataBandi_kimlik_verilince_destek_kodu_rozetini_gosterir()
    {
        var e = Render<Kuyumcu.Web.Bilesenler.HataBandi>(p => p
            .Add(x => x.Mesaj, "Sunucu hatası")
            .Add(x => x.Id, "hata-bandi")
            .Add(x => x.Kimlik, "01KSTKMLK000000000000ABCD"));

        Assert.NotEmpty(e.FindAll("#hata-kimlik-rozet"));
        // P-36c/3: destek kodu artık yazılamaz bir kutu değil, SALT OKUNUR
        // metindir; MAKİNE değeri Secici ile aynı sözleşmeden okunur (data-deger).
        Assert.Equal("01KSTKMLK000000000000ABCD", e.Find("#hata-kimlik-deger").GetAttribute("data-deger"));
        Assert.Contains("Sunucu hatası", e.Markup);

        // Kopyala düğmesi gerçek bir etki üretir (DoD EK md.1).
        e.Find("#hata-kimlik-kopyala").Click();
        e.WaitForAssertion(() => Assert.NotEmpty(e.FindAll("#hata-kimlik-elle")));
    }

    [Fact]
    public void HataBandi_kimlik_yokken_rozet_hic_cizilmez()
    {
        var e = Render<Kuyumcu.Web.Bilesenler.HataBandi>(p => p
            .Add(x => x.Mesaj, "Sunucu hatası"));

        Assert.Empty(e.FindAll("#hata-kimlik-rozet"));
        Assert.Empty(e.FindAll("#hata-kimlik-kopyala"));
        Assert.Contains("Sunucu hatası", e.Markup);
    }
}
