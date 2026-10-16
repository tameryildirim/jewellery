using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// İSTEK KİMLİĞİ (korelasyon) — paket T-4 Faz A.
//
// Sınanan sözleşme:
//   • her yanıt X-Istek-Kimligi başlığı taşır
//   • hata gövdesindeki istekKimligi ile başlıktaki değer AYNIDIR
//   • istemcinin gönderdiği kimlik YOK SAYILIR (günlük zehirlenmesi)
//   • iki istek iki ayrı kimlik üretir
//   • 500 dalı ayrıca sınanır (404 yeşilken 500 kırılabilir — §A10 tuzağı)
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class IstekKimligiTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static string Baslik(HttpResponseMessage cevap)
    {
        Assert.True(cevap.Headers.TryGetValues(IstekKimligiAlani.Baslik, out var d),
            $"{IstekKimligiAlani.Baslik} başlığı yanıtta YOK.");
        return d!.Single();
    }

    [Fact]
    public async Task Basarili_istek_X_Istek_Kimligi_basligi_dondurur()
    {
        var k = YeniKiraci("T4 kimlik başlık");
        var cevap = await f.Ortam.Istemci(k).GetAsync("/api/v1/cari");

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var kimlik = Baslik(cevap);
        // ULID: 26 karakter, Crockford Base32.
        Assert.Equal(26, kimlik.Length);
    }

    [Fact]
    public async Task Bulunamayan_kayit_404_govdesinde_istekKimligi_alani_var()
    {
        var k = YeniKiraci("T4 kimlik 404");
        var cevap = await f.Ortam.Istemci(k).GetAsync("/api/v1/cari/YOK-123");

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        var json = await cevap.Json();
        var govde = json.GetProperty("istekKimligi").GetString();
        Assert.False(string.IsNullOrWhiteSpace(govde));
        // Mevcut Nest sözleşmesi KORUNUR.
        Assert.Equal("Not Found", json.GetProperty("error").GetString());
        Assert.Equal(404, json.GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task Basliktaki_kimlik_ile_govdedeki_kimlik_ayni()
    {
        var k = YeniKiraci("T4 kimlik eşleşme");
        var cevap = await f.Ortam.Istemci(k).GetAsync("/api/v1/cari/YOK-456");

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        Assert.Equal(Baslik(cevap), (await cevap.Json()).GetProperty("istekKimligi").GetString());
    }

    [Fact]
    public async Task Dogrulama_400_govdesinde_de_kimlik_var()
    {
        var k = YeniKiraci("T4 kimlik 400");
        // Zorunlu alanları boş gövde: doğrulama 400'ü HataYakalayici'dan GEÇMEZ,
        // çerçeve doğrudan yazar — kimlik orada da olmalı.
        var cevap = await f.Ortam.Istemci(k).PostJson("/api/v1/cari", new { });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Equal(Baslik(cevap), (await cevap.Json()).GetProperty("istekKimligi").GetString());
    }

    [Fact]
    public async Task Istemcinin_gonderdigi_X_Istek_Kimligi_yok_sayilir_sunucu_kendi_uretir()
    {
        var k = YeniKiraci("T4 kimlik zehirleme");
        var istemci = f.Ortam.Istemci(k);
        const string sahte = "ISTEMCININ-UYDURDUGU-KIMLIK";
        istemci.DefaultRequestHeaders.Add(IstekKimligiAlani.Baslik, sahte);

        var cevap = await istemci.GetAsync("/api/v1/cari/YOK-789");

        var kimlik = Baslik(cevap);
        Assert.NotEqual(sahte, kimlik);
        Assert.Equal(26, kimlik.Length);
        Assert.Equal(kimlik, (await cevap.Json()).GetProperty("istekKimligi").GetString());
    }

    [Fact]
    public async Task Iki_ayri_istek_iki_ayri_kimlik_uretir()
    {
        var k = YeniKiraci("T4 kimlik benzersiz");
        var istemci = f.Ortam.Istemci(k);

        var bir = Baslik(await istemci.GetAsync("/api/v1/cari"));
        var iki = Baslik(await istemci.GetAsync("/api/v1/cari"));

        Assert.NotEqual(bir, iki);
    }

    [Fact]
    public async Task Tokensiz_401_yanitinda_da_kimlik_var()
    {
        var cevap = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/cari");

        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
        Assert.Equal(Baslik(cevap), (await cevap.Json()).GetProperty("istekKimligi").GetString());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// HataYakalayici'nin DÖRT DALI — izole (TestServer'da 413 üretilemez, bkz.
// GovdeSiniriTestleri başlığı). 500 dalı AYRI sınanır: 404 yeşilken 500'den
// kimlik alanı düşerse bu test kırmızı olmalı.
// ═════════════════════════════════════════════════════════════════════════════
public sealed class HataKimligiDallariTestleri
{
    private static async Task<(int Kod, JsonElement Govde)> Kos(Exception firlat)
    {
        var ctx = new DefaultHttpContext();
        var akis = new MemoryStream();
        ctx.Response.Body = akis;
        ctx.Items[IstekKimligiAlani.OgeAnahtari] = "01TESTKIMLIK0000000000TEST";

        var m = new HataYakalayici(_ => throw firlat, NullLogger<HataYakalayici>.Instance);
        await m.InvokeAsync(ctx);

        akis.Position = 0;
        var metin = await new StreamReader(akis).ReadToEndAsync();
        return (ctx.Response.StatusCode, JsonDocument.Parse(metin).RootElement);
    }

    [Fact]
    public async Task Beklenmeyen_hata_500_govdesinde_kimlik_var()
    {
        var (kod, govde) = await Kos(new InvalidOperationException("patladı"));

        Assert.Equal(500, kod);
        Assert.Equal("01TESTKIMLIK0000000000TEST", govde.GetProperty("istekKimligi").GetString());
        // ⚠ 500 dalına `error` alanı EKLENMEZ (Bertan koşulu — sözleşme kaymasın).
        Assert.False(govde.TryGetProperty("error", out _));
        Assert.Equal("Internal server error", govde.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Govde_siniri_413_yanitinda_da_kimlik_var()
    {
        var (kod, govde) = await Kos(new BadHttpRequestException("too large", 413));

        Assert.Equal(413, kod);
        Assert.Equal("01TESTKIMLIK0000000000TEST", govde.GetProperty("istekKimligi").GetString());
        // 413'ün alan kümesi AYNEN korunur.
        Assert.Equal("Payload Too Large", govde.GetProperty("error").GetString());
        Assert.Equal(413, govde.GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task Esszamanlilik_409_yanitinda_da_kimlik_var()
    {
        var (kod, govde) = await Kos(new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException("yarış"));

        Assert.Equal(409, kod);
        Assert.Equal("01TESTKIMLIK0000000000TEST", govde.GetProperty("istekKimligi").GetString());
        Assert.Equal("Conflict", govde.GetProperty("error").GetString());
    }

    [Fact]
    public async Task ApiException_dalinda_alan_kumesi_korunur_ve_kimlik_eklenir()
    {
        var (kod, govde) = await Kos(new NotFoundException("cari bulunamadı"));

        Assert.Equal(404, kod);
        Assert.Equal("cari bulunamadı", govde.GetProperty("message").GetString());
        Assert.Equal("Not Found", govde.GetProperty("error").GetString());
        Assert.Equal(404, govde.GetProperty("statusCode").GetInt32());
        Assert.Equal("01TESTKIMLIK0000000000TEST", govde.GetProperty("istekKimligi").GetString());
    }
}
