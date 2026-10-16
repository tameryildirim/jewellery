using System.Net;
using System.Net.Http.Json;
using System.Text;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Iceaktarim;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// VERİ İÇE AKTARIMI — paket 47 Faz A [HTTP]
//
// Servis testi göremeyenler: rol kapısı (403), oran sınırı (429), model bağlama
// (400 biçimi), JSON sözleşmesi (camelCase). Bu dosya onları ölçer.
//
// ⚠ TAVAN RAKAMLARI SABİTLENMEZ (Aşkın koşulu): yapılandırmadan okunur ve
//   "tavan + 1 → 400" biçiminde sınanır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class IceAktarimHttpTestleri(HttpFikstur f)
{
    private const string Gerekce = "Eski programdan müşteri kartları taşınıyor";

    private const string UcCari = """
        tur,ad,telefon
        MUSTERI,Ayşe Yılmaz,05321112233
        MUSTERI,Ali Demir,05321112244
        """;

    private IceAktarimAyarlari Ayar => f.Ortam.Services.GetRequiredService<IceAktarimAyarlari>();

    private static object Govde(string csv = UcCari, string kaynak = "cari", string gerekce = Gerekce) => new
    {
        kaynak,
        dosyaAdi = "liste.csv",
        icerikBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv)),
        gerekce,
        yinelenenPolitikasi = "HATA",
    };

    [Fact]
    public async Task Token_yoksa_401()
    {
        var c = f.Ortam.AnonimIstemci();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await c.PostAsJsonAsync("/api/v1/ice-aktarim/prova", Govde())).StatusCode);
    }

    [Fact]
    public async Task MUDUR_dahi_ice_aktaramaz_yalniz_SAHIP_403()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "PAKET47-TEST http rol");

        foreach (var rol in new[] { "MUDUR", "MUHASEBE", "TEZGAHTAR" })
        {
            var c = f.Ortam.Istemci(k, rol);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await c.PostAsJsonAsync("/api/v1/ice-aktarim/prova", Govde())).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await c.GetAsync("/api/v1/ice-aktarim/sablonlar")).StatusCode);
        }

        var sahip = f.Ortam.Istemci(k);
        Assert.Equal(HttpStatusCode.OK,
            (await sahip.PostAsJsonAsync("/api/v1/ice-aktarim/prova", Govde())).StatusCode);
    }

    [Fact]
    public async Task Gerekcesiz_prova_400()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "PAKET47-TEST http gerekçe");
        var c = f.Ortam.Istemci(k);

        var r = await c.PostAsJsonAsync("/api/v1/ice-aktarim/prova", Govde(gerekce: "lazım"));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("karakter", (await r.Json()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task Gecersiz_base64_400()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "PAKET47-TEST http base64");
        var c = f.Ortam.Istemci(k);

        var r = await c.PostAsJsonAsync("/api/v1/ice-aktarim/prova", new
        {
            kaynak = "cari", dosyaAdi = "x.csv", icerikBase64 = "bu-base64-degil!!", gerekce = Gerekce,
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("base64", (await r.Json()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task Dosya_tavani_asilinca_400_ve_hicbir_oturum_acilmaz()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "PAKET47-TEST http tavan");
        var c = f.Ortam.Istemci(k);

        // Tavan + 1 bayt — rakam YAPILANDIRMADAN gelir.
        var buyuk = new string('a', Ayar.MaksDosyaBayt + 1);
        var r = await c.PostAsJsonAsync("/api/v1/ice-aktarim/prova", new
        {
            kaynak = "cari", dosyaAdi = "kocaman.csv",
            icerikBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(buyuk)),
            gerekce = Gerekce,
        });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);

        using var db = f.Vt.Olustur(k.TenantId);
        Assert.Equal(0, await db.Set<IceAktarimOturumu>().CountAsync(o => o.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Onay_metni_YANLIS_ise_uygula_400()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "PAKET47-TEST http onay");
        var c = f.Ortam.Istemci(k);

        var prova = await (await c.PostAsJsonAsync("/api/v1/ice-aktarim/prova", Govde())).Json();
        var id = prova.GetProperty("oturumId").GetString();

        var r = await c.PostAsJsonAsync($"/api/v1/ice-aktarim/{id}/uygula", new { onay = "tamam" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);

        using var db = f.Vt.Olustur(k.TenantId);
        Assert.Equal(1, await db.Cariler.CountAsync(x => x.TenantId == k.TenantId));   // yalnız fikstür carisi
    }

    [Fact]
    public async Task Uygulanan_oturum_IKINCI_kez_uygulanamaz_409()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "PAKET47-TEST http 409");
        var c = f.Ortam.Istemci(k);

        var sablon = await (await c.GetAsync("/api/v1/ice-aktarim/sablonlar")).Json();
        var onay = sablon.GetProperty("onayMetni").GetString();

        var prova = await (await c.PostAsJsonAsync("/api/v1/ice-aktarim/prova", Govde())).Json();
        var id = prova.GetProperty("oturumId").GetString();

        Assert.Equal(HttpStatusCode.OK,
            (await c.PostAsJsonAsync($"/api/v1/ice-aktarim/{id}/uygula", new { onay })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await c.PostAsJsonAsync($"/api/v1/ice-aktarim/{id}/uygula", new { onay })).StatusCode);
    }

    [Fact]
    public async Task Sablon_CSV_BOM_lu_ve_baslik_satirli_doner()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "PAKET47-TEST şablon");
        var c = f.Ortam.Istemci(k);

        var r = await c.GetAsync("/api/v1/ice-aktarim/sablon/cari.csv");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("text/csv", r.Content.Headers.ContentType?.MediaType);

        var bayt = await r.Content.ReadAsByteArrayAsync();
        Assert.Equal([0xEF, 0xBB, 0xBF], bayt[..3]);
        Assert.Contains("tur,ad,telefon", Encoding.UTF8.GetString(bayt));
    }

    [Fact]
    public async Task Oran_siniri_asilinca_429()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "PAKET47-TEST http 429");
        var c = f.Ortam.Istemci(k);

        // 'ice-aktarim' politikası 5/saat. Altıncı istek kilide takılır.
        var kodlar = new List<HttpStatusCode>();
        for (var i = 0; i < 7; i++)
            kodlar.Add((await c.PostAsJsonAsync("/api/v1/ice-aktarim/prova", Govde())).StatusCode);

        Assert.Contains(HttpStatusCode.TooManyRequests, kodlar);
    }
}
