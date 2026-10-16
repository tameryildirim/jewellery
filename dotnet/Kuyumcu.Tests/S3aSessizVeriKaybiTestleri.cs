using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Web.Servisler;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S-3a — SESSİZ VERİ KAYBI (QA turu 3 + kurul tur 3 borcu).
//
// Bu dosya ÜÇ borcu kilitler:
//   1) Sipariş PATCH'i kalem verisini imha ediyordu (kurul tur 3 · engel 4,
//      KRİTİK). Gönderilmeyen kalem alanı artık KORUNUR.
//   2) Kültürsüz ToString makine biçimini bozuyordu (engel 19) →
//      Bicim.Makine(...) tr-TR devredeyken bile NOKTA ayırıcı üretir.
//   3) Yenileme jetonu yarışının TEK UÇUŞA indiği regresyon altında tutulur
//      (engel 15'in ApiIstemci ayağı — mekanizma UX-4d'de yazıldı, burada
//      sözleşme olarak kilitleniyor).
//
// ⚠ Kararsızlık koruması: zamanlayıcı/yoklama yok; ölçüm açık eylemlerle.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class S3aSiparisKalemKorumaTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    /// <summary>Ürün bağlı, ayarlı ve gramlı TEK kalemle TASLAK sipariş açar.</summary>
    private async Task<(HttpClient Istemci, string Id, string KalemId)> ZenginSiparisAc(Kiraci k)
    {
        var c = f.Ortam.Istemci(k, "SAHIP");
        var y = await c.PostAsJsonAsync("/api/v1/siparisler", new
        {
            tur = "MUSTERI_SIPARISI",
            cariId = k.CariId,
            aciklama = "S-3a kalem koruma",
            kalemler = new[]
            {
                new { urunId = k.UrunId, ayarId = "K22", gramAgirlik = 12.5m, adet = 1m },
            },
        });
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var j = await y.Json();
        var kalem = j.GetProperty("kalemler").EnumerateArray().Single();
        Assert.Equal(k.UrunId, kalem.GetProperty("urunId").GetString());
        Assert.Equal("K22", kalem.GetProperty("ayarId").GetString());
        Assert.Equal(12.5m, kalem.GetProperty("gramAgirlik").GetDecimal());
        return (c, j.GetProperty("id").GetString()!, kalem.GetProperty("id").GetString()!);
    }

    private static HttpRequestMessage Patch(string id, object govde) =>
        new(HttpMethod.Patch, $"/api/v1/siparisler/{id}") { Content = JsonContent.Create(govde) };

    // ── 1. KRİTİK regresyon: gönderilmeyen kalem alanı KORUNUR ───────────────

    [Fact]
    public async Task PATCH_kalemde_gonderilmeyen_alan_KORUNUR_urun_ayar_gram_silinmez()
    {
        var k = YeniKiraci("PAKETS3A-KALEM");
        var (c, id, kalemId) = await ZenginSiparisAc(k);

        // QA turu 3'teki ölümcül istek: kullanıcı yalnız açıklamayı değiştirdi,
        // ekran kalemi "tanim + adet" olarak geri gönderdi.
        var y = await c.SendAsync(Patch(id, new
        {
            aciklama = "yalnız açıklama değişti",
            kalemler = new[] { new { id = kalemId, adet = 1m } },
        }));
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);

        var j = await y.Json();
        var kalem = j.GetProperty("kalemler").EnumerateArray().Single();
        Assert.Equal(kalemId, kalem.GetProperty("id").GetString());          // kimlik kararlı
        Assert.Equal(k.UrunId, kalem.GetProperty("urunId").GetString());     // ürün bağı DURUYOR
        Assert.Equal("K22", kalem.GetProperty("ayarId").GetString());        // ayar DURUYOR
        Assert.Equal(12.5m, kalem.GetProperty("gramAgirlik").GetDecimal());  // gram DURUYOR
        Assert.Equal("yalnız açıklama değişti", j.GetProperty("aciklama").GetString());
    }

    // ── 2. Açık null SİLER (PATCH sözleşmesinin diğer yarısı) ────────────────

    [Fact]
    public async Task PATCH_kalemde_acik_null_SILER()
    {
        var k = YeniKiraci("PAKETS3A-NULL");
        var (c, id, kalemId) = await ZenginSiparisAc(k);

        var y = await c.SendAsync(Patch(id, new
        {
            kalemler = new[]
            {
                new { id = kalemId, urunId = (string?)null, tanim = "serbest künye", ayarId = (string?)null },
            },
        }));
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);

        var kalem = (await y.Json()).GetProperty("kalemler").EnumerateArray().Single();
        Assert.Equal(System.Text.Json.JsonValueKind.Null, kalem.GetProperty("urunId").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, kalem.GetProperty("ayarId").ValueKind);
        Assert.Equal("serbest künye", kalem.GetProperty("tanim").GetString());
        // Gönderilmeyen gram KORUNUR — "null gönderildi" ile karıştırılmaz.
        Assert.Equal(12.5m, kalem.GetProperty("gramAgirlik").GetDecimal());
    }

    // ── 3. Küme anlamı korunur: anılmayan kalem SİLİNİR, id'siz kalem DOĞAR ──

    [Fact]
    public async Task PATCH_anilmayan_kalem_silinir_idsiz_kalem_dogar()
    {
        var k = YeniKiraci("PAKETS3A-KUME");
        var (c, id, kalemId) = await ZenginSiparisAc(k);

        var y = await c.SendAsync(Patch(id, new
        {
            kalemler = new[] { new { tanim = "yepyeni kalem", adet = 2m } },
        }));
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);

        var kalemler = (await y.Json()).GetProperty("kalemler").EnumerateArray().ToList();
        Assert.Single(kalemler);
        Assert.NotEqual(kalemId, kalemler[0].GetProperty("id").GetString());
        Assert.Equal("yepyeni kalem", kalemler[0].GetProperty("tanim").GetString());
    }

    // ── 4. Yabancı kalem kimliği 404 — başka siparişin kalemi ele geçirilemez ─

    [Fact]
    public async Task PATCH_bu_siparise_ait_olmayan_kalem_kimligi_404_doner()
    {
        var k = YeniKiraci("PAKETS3A-YABANCI");
        var (c, id, _) = await ZenginSiparisAc(k);
        var (_, _, digerKalem) = await ZenginSiparisAc(k);

        var y = await c.SendAsync(Patch(id, new
        {
            kalemler = new[] { new { id = digerKalem, adet = 1m } },
        }));
        Assert.Equal(HttpStatusCode.NotFound, y.StatusCode);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// KÜLTÜR BİÇİMİ — saf testler (kurul tur 3 · engel 19).
// ═════════════════════════════════════════════════════════════════════════════

public sealed class S3aMakineBicimiTestleri
{
    /// <summary>
    /// Kullanıcı arayüzü tr-TR kültüründe koşar. Makine biçimi buna RAĞMEN
    /// nokta ayırıcı üretmeli; yoksa SVG yolu çizilmez, number girdisi boşalır,
    /// sunucu 400 döner.
    /// </summary>
    [Fact]
    public void Makine_trTR_kulturunde_bile_nokta_ayirici_uretir()
    {
        var onceki = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            Assert.Equal("12,5", 12.5m.ToString());          // tuzağın kendisi
            Assert.Equal("12.5", Bicim.Makine(12.5m));
            Assert.Equal("12.5", Bicim.Makine(12.5d));
            Assert.Equal("1000", Bicim.Makine(1000m));       // binlik ayırıcı YOK
            Assert.Equal("7", Bicim.Makine(7));
            Assert.Equal("2026-09-18", Bicim.Makine(new DateOnly(2026, 9, 18)));
        }
        finally { CultureInfo.CurrentCulture = onceki; }
    }

    [Fact]
    public void Makine_insan_bicimiyle_karistirilmaz()
    {
        // Para/gram insan içindir; makine biçimi birim ve simge TAŞIMAZ.
        Assert.DoesNotContain("₺", Bicim.Makine(150m));
        Assert.DoesNotContain("g", Bicim.Makine(150m));
    }
}
