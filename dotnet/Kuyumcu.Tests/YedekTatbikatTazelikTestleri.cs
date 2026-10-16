using System.Net;
using Bunit;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Yedek;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-33 (B-5 · BK-30 · Köprü K-5) — SON TATBİKAT TARİHİ /yedekler'DE
// GÖRÜNÜR VE 30 GÜNDEN ESKİ OLAMAZ
//
// Sınanan sözleşme:
//   • tazelik YALNIZ BAŞARILI tatbikattan ölçülür (başarısız deneme ≠ kanıt)
//   • sınır 30 gün (dâhil); tatbikat yoksa BAYAT
//   • yapılandırma sınırı GEVŞETEMEZ (TatbikatAzamiGun 90 → 30)
//   • /yedekler/ozet alanları sunucudan gelir; ekran bayatsa kırmızı kart +
//     uyarı bandı, tazeyse bant yok (30 istemciye gömülmez)
// ═════════════════════════════════════════════════════════════════════════════

public sealed class YedekTatbikatTazelikTestleri
{
    private static readonly DateTime Simdi = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Tatbikat_HIC_yoksa_BAYATTIR()
        => Assert.True(YedekPolitikasi.TatbikatBayatMi(null, Simdi, 30));

    [Fact]
    public void Tam_30_gun_once_BAYAT_DEGIL_bir_saniye_fazlasi_BAYAT()
    {
        Assert.False(YedekPolitikasi.TatbikatBayatMi(Simdi.AddDays(-30), Simdi, 30));
        Assert.True(YedekPolitikasi.TatbikatBayatMi(Simdi.AddDays(-30).AddSeconds(-1), Simdi, 30));
        Assert.False(YedekPolitikasi.TatbikatBayatMi(Simdi.AddDays(-6), Simdi, 30));
    }

    [Fact]
    public void Tatbikat_yasi_TAM_GUN_olarak_hesaplanir()
    {
        Assert.Equal(6, YedekPolitikasi.TatbikatYasGun(Simdi.AddDays(-6).AddHours(-3), Simdi));
        Assert.Null(YedekPolitikasi.TatbikatYasGun(null, Simdi));
    }

    [Theory]
    [InlineData("90", 30)]
    [InlineData("31", 30)]
    [InlineData("7", 7)]
    [InlineData("0", 1)]
    public void Yapilandirma_30_gun_sinirini_GEVSETEMEZ(string deger, int beklenen)
    {
        var y = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Yedek:TatbikatAzamiGun"] = deger })
            .Build();
        Assert.Equal(beklenen, YedekAyarlari.Oku(y).TatbikatAzamiGun);
    }

    [Fact]
    public void Varsayilan_azami_gun_30()
        => Assert.Equal(30, YedekAyarlari.Oku(new ConfigurationBuilder().Build()).TatbikatAzamiGun);
}

[Collection("http")]
public sealed class YedekTatbikatTazelikHttpTestleri(HttpFikstur f)
{
    private static YedekTatbikati Tatbikat(DateTime tarih, string sonuc) => new()
    {
        Id = "p33-" + Guid.NewGuid().ToString("N")[..20],
        Tarih = tarih,
        TabloSayisi = 54,
        Uyusan = sonuc == YedekDurumu.Basarili ? 54 : 50,
        Uyusmayan = sonuc == YedekDurumu.Basarili ? 0 : 4,
        Sonuc = sonuc,
        SureSn = 8,
        SemaDusuruldu = true,
        Rapor = "uyusmayan_tablolar=",
    };

    [Fact]
    public async Task Ozet_son_BASARILI_tatbikati_ve_tazeligini_SUNUCUDA_hesaplar()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "P33-tatbikat");
        var simdi = DateTime.UtcNow;

        // (1) Başarılı tatbikat 40 gün önce, BAŞARISIZ olan dün → hâlâ BAYAT.
        var eski = Tatbikat(simdi.AddDays(-40), YedekDurumu.Basarili);
        using (var db = f.Vt.OlusturSistem())
        {
            db.YedekTatbikatlari.Add(eski);
            db.YedekTatbikatlari.Add(Tatbikat(simdi.AddDays(-1), YedekDurumu.Basarisiz));
            db.SaveChanges();
        }

        var cevap = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/yedekler/ozet");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var g = await cevap.Json();
        Assert.True(g.GetProperty("tatbikatBayat").GetBoolean(),
            "Başarısız tatbikat tazelik sayıldı — 'denendi' ile 'geri yüklenebildi' karıştı.");
        Assert.Equal(30, g.GetProperty("tatbikatAzamiGun").GetInt32());
        Assert.Equal(40, g.GetProperty("tatbikatYasGun").GetInt32());
        Assert.Equal("BASARISIZ", g.GetProperty("sonTatbikatSonuc").GetString());

        // (2) Başarılı tatbikat 5 gün önce → TAZE.
        using (var db = f.Vt.OlusturSistem())
        {
            db.YedekTatbikatlari.Add(Tatbikat(simdi.AddDays(-5), YedekDurumu.Basarili));
            db.SaveChanges();
        }
        var g2 = await (await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/yedekler/ozet")).Json();
        Assert.False(g2.GetProperty("tatbikatBayat").GetBoolean());
        Assert.Equal(5, g2.GetProperty("tatbikatYasGun").GetInt32());
        var sonBasarili = g2.GetProperty("sonBasariliTatbikat").GetDateTime();
        // Sunucu UTC yazar; JSON'da bölge eki yoksa değer UTC'dir.
        var utc = sonBasarili.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(sonBasarili, DateTimeKind.Utc) : sonBasarili.ToUniversalTime();
        Assert.True(Math.Abs((utc - simdi.AddDays(-5)).TotalSeconds) < 5, $"sonBasariliTatbikat={sonBasarili:o}");
    }
}

public sealed class YedekTatbikatEkranTestleri : BunitContext
{
    private string ozet = "";

    private static string Ozet(bool bayat, string? sonBasarili, int? yas) => $$"""
        {"sonYedek":{"tur":"TAM","zaman":"2026-09-17T03:15:00Z","bitis":"2026-09-17T03:16:00Z",
          "boyutBayt":3760268,"durum":"BASARILI","sifreli":true,"tabloSayisi":54,"hataOzeti":null},
         "dogrulama":"DOGRULANDI","dogrulamaTarih":"2026-09-17T04:30:00Z",
         "sonDogrulanmisYedek":"2026-09-17T03:15:00Z","disKopya":"KOPYALANDI",
         "saklananKosu":7,"saklamaGun":30,"rpoDakikaOlculen":95,"rpoDakikaTaahhut":360,
         "rtoDakikaTaahhut":120,"rtoDakikaOlculen":1,"sonTatbikat":"2026-09-17T04:30:00Z",
         "sonTatbikatSonuc":"BASARILI","azamiLimit":50,"sonKosuSaglikli":true,"saklamaSilmeAktif":true,
         "sonBasariliTatbikat":{{(sonBasarili is null ? "null" : $"\"{sonBasarili}\"")}},
         "tatbikatYasGun":{{(yas is null ? "null" : yas.ToString())}},"tatbikatAzamiGun":30,
         "tatbikatBayat":{{(bayat ? "true" : "false")}}}
        """;

    public YedekTatbikatEkranTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
        Services.AddScoped<OturumServisi>();
        Services.AddScoped(_ => new HttpClient(new Sahte(this)) { BaseAddress = new Uri("http://test.local/api/v1/") });
        Services.AddScoped<ApiIstemci>();
    }

    private sealed class Sahte(YedekTatbikatEkranTestleri t) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken c)
        {
            var yol = r.RequestUri!.PathAndQuery;
            var govde =
                yol.Contains("/yedekler/ozet") ? t.ozet :
                yol.Contains("/yedekler/tatbikatlar") ? """{"limit":12,"azamiLimit":50,"tatbikatlar":[]}""" :
                yol.Contains("/yedekler/talepler") ? """{"limit":20,"azamiLimit":50,"talepler":[]}""" :
                yol.Contains("/yedekler") ? """{"limit":50,"azamiLimit":50,"dahaFazlasiVar":false,"kayitlar":[]}""" :
                "{}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(govde, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    [Fact]
    public void Bayat_tatbikat_KIRMIZI_UYARI_bandi_ve_hic_yapilmadi_yazar()
    {
        ozet = Ozet(bayat: true, sonBasarili: null, yas: null);
        var c = Render<Kuyumcu.Web.Pages.Yedekler>();
        c.WaitForAssertion(() => Assert.NotEmpty(c.FindAll("#yd-tatbikat-bayat")));

        var bant = c.Find("#yd-tatbikat-bayat");
        Assert.Equal("alert", bant.GetAttribute("role"));
        Assert.Equal(YedekTatbikatSozlugu.BayatUyari("tr", 30), bant.TextContent.Trim());
        Assert.Contains(YedekTatbikatSozlugu.KartEtiketi("tr"), c.Markup);
        Assert.Contains(YedekTatbikatSozlugu.HicYok("tr"), c.Markup);
    }

    [Fact]
    public void Taze_tatbikat_TARIHINI_gosterir_bant_YOK()
    {
        ozet = Ozet(bayat: false, sonBasarili: "2026-09-20T09:00:00Z", yas: 3);
        var c = Render<Kuyumcu.Web.Pages.Yedekler>();
        c.WaitForAssertion(() => Assert.Contains(YedekTatbikatSozlugu.KartEtiketi("tr"), c.Markup));

        Assert.Empty(c.FindAll("#yd-tatbikat-bayat"));
        var beklenenTarih = new DateTime(2026, 9, 20, 9, 0, 0, DateTimeKind.Utc).ToLocalTime().ToString("g", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
        Assert.Contains(beklenenTarih, c.Markup);
        Assert.Contains(YedekTatbikatSozlugu.KartAlt("tr", 3, 30), c.Markup);
    }
}
