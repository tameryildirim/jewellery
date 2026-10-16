using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// BELGE UÇLARI — GERÇEK HTTP BORU HATTI (paket 4)
//
// Servis testi göremez: [RolGerekli] kapısı, oran sınırı (429), ETag/304,
// Content-Disposition, JSON sözleşmesi (camelCase).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class BelgeHttpTestleri(HttpFikstur f)
{
    private static string PdfB64(string ek = "") =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("%PDF-1.7 " + ek));

    private static object Govde(string sahipTuru, string sahipId, string tur, string icerik, string ad = "d.pdf") =>
        new { sahipTuru, sahipId, tur, ad, icerikBase64 = icerik };

    [Fact]
    public async Task Yukleme_200_doner_ve_yanit_camelCase_sha256_tasir()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "Http belge yukle");
        var c = f.Ortam.Istemci(k);

        var y = await c.PostAsJsonAsync("/api/v1/belgeler", Govde("URUN", k.UrunId, "SERTIFIKA", PdfB64("http")));
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);

        var j = await y.Json();
        Assert.Equal("URUN", j.GetProperty("sahipTuru").GetString());
        Assert.Equal(64, j.GetProperty("sha256").GetString()!.Length);
    }

    /// <summary>⚠ Mehmet K4-5: DELETE ucu rol kapılıdır — TEZGAHTAR pasife alamaz.</summary>
    [Fact]
    public async Task TEZGAHTAR_yukleyebilir_ama_PASIFE_ALAMAZ_403()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "Http belge rol");
        var tezgahtar = f.Ortam.Istemci(k.KullaniciId, k.TenantId, k.SubeId, "TEZGAHTAR");

        var y = await tezgahtar.PostAsJsonAsync("/api/v1/belgeler", Govde("URUN", k.UrunId, "SERTIFIKA", PdfB64("rol")));
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var id = (await y.Json()).GetProperty("id").GetString()!;

        var sil = await tezgahtar.DeleteAsync($"/api/v1/belgeler/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, sil.StatusCode);

        // Eski ürün yolundaki DELETE ucu da aynı kapıya tabidir.
        var eski = await tezgahtar.DeleteAsync($"/api/v1/urunler/belgeler/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, eski.StatusCode);
    }

    /// <summary>⚠ Mehmet K4-7: kiracı şartı 304'ten ÖNCE — yabancı kiracı 404 alır, 304 değil.</summary>
    [Fact]
    public async Task Icerik_ucu_ETag_doner_304_calisir_YABANCI_KIRACI_404()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "Http belge etag");
        var k2 = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "Http belge etag yabanci");
        var c = f.Ortam.Istemci(k);

        var y = await c.PostAsJsonAsync("/api/v1/belgeler", Govde("URUN", k.UrunId, "SERTIFIKA", PdfB64("etag")));
        var j = await y.Json();
        var id = j.GetProperty("id").GetString()!;
        var sha = j.GetProperty("sha256").GetString()!;

        var icerik = await c.GetAsync($"/api/v1/belgeler/{id}/icerik");
        Assert.Equal(HttpStatusCode.OK, icerik.StatusCode);
        Assert.Equal($"\"{sha}\"", icerik.Headers.ETag!.ToString());
        Assert.Equal("attachment", icerik.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Equal("application/pdf", icerik.Content.Headers.ContentType!.MediaType);
        Assert.StartsWith("%PDF-", await icerik.Content.ReadAsStringAsync());

        var istek = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/belgeler/{id}/icerik");
        istek.Headers.IfNoneMatch.Add(new EntityTagHeaderValue($"\"{sha}\""));
        Assert.Equal(HttpStatusCode.NotModified, (await c.SendAsync(istek)).StatusCode);

        // Yabancı kiracı: If-None-Match ile bile VARLIK SIZDIRILMAZ.
        var c2 = f.Ortam.Istemci(k2);
        Assert.Equal(HttpStatusCode.NotFound, (await c2.GetAsync($"/api/v1/belgeler/{id}/icerik")).StatusCode);

        var yabanciIstek = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/belgeler/{id}/icerik");
        yabanciIstek.Headers.IfNoneMatch.Add(new EntityTagHeaderValue($"\"{sha}\""));
        Assert.Equal(HttpStatusCode.NotFound, (await c2.SendAsync(yabanciIstek)).StatusCode);
    }

    [Fact]
    public async Task Liste_ucu_JSON_yanitinda_icerik_alani_YOKTUR()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "Http belge liste");
        var c = f.Ortam.Istemci(k);
        await c.PostAsJsonAsync("/api/v1/belgeler", Govde("URUN", k.UrunId, "SERTIFIKA", PdfB64("liste")));

        var y = await c.GetAsync($"/api/v1/belgeler?sahipTuru=URUN&sahipId={k.UrunId}");
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var satir = (await y.Json()).EnumerateArray().Single();
        Assert.DoesNotContain(satir.EnumerateObject(),
            p => p.Name.Equals("icerik", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(satir.EnumerateObject(),
            p => p.Name.Equals("icerikBase64", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Uzantisi_pdf_ama_icerigi_metin_olan_dosya_400_ve_SIFIR_SATIR()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "Http belge sahte pdf");
        var c = f.Ortam.Istemci(k);

        var y = await c.PostAsJsonAsync("/api/v1/belgeler",
            Govde("URUN", k.UrunId, "SERTIFIKA", Convert.ToBase64String("sadece metin"u8.ToArray()), "sahte.pdf"));
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
        Assert.Contains("imza", (await y.Json()).GetProperty("message").GetString()!);

        using var kontrol = f.Vt.OlusturSistem();
        Assert.Equal(0, await kontrol.Belgeler.CountAsync(b => b.TenantId == k.TenantId));
    }

    /// <summary>
    /// ⚠ Mehmet K4-6 / Aşkın: 'belge' politikası GlobalLimiter ile ZİNCİRLENİR,
    /// ezilmez. Kullanıcı başına 30/dk → 31. POST 429.
    /// </summary>
    [Fact]
    public async Task Otuz_birinci_yukleme_429_doner()
    {
        var k = TestKurulum.KiraciKur(f.Vt.OlusturSistem(), "Http belge oran");
        var c = f.Ortam.Istemci(k);

        HttpStatusCode son = HttpStatusCode.OK;
        for (var i = 0; i < 31; i++)
        {
            // Tavana takılmamak için her istek AYNI ürüne gitmez; içerik geçersiz
            // olsa bile oran sınırı uçtan ÖNCE çalışır (400 de kotayı harcar).
            var y = await c.PostAsJsonAsync("/api/v1/belgeler",
                Govde("URUN", k.UrunId, "SERTIFIKA", Convert.ToBase64String("yok"u8.ToArray())));
            son = y.StatusCode;
            if (son == HttpStatusCode.TooManyRequests) break;
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, son);
    }
}
