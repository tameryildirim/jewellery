using System.Net;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// TAHSİLAT KASA BACAĞI — HTTP KAPISI (paket UX-4b madde 9).
//
// NEDEN SERVİS TESTİ YETMEZ: koşullu zorunluluk DTO'da IValidatableObject ile
// ifade edildi ve bu YALNIZ denetleyici yolunda çalışır. Servis testi 400
// biçimini, [RolGerekli] kapısını ve JSON sözleşmesini hiç görmez.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class HttpTahsilatKasaTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad, string kasaDoviz = "TRY")
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad, kasaDoviz: kasaDoviz);
    }

    [Fact]
    public async Task KasaIdsiz_NAKIT_tahsilat_400_doner()
    {
        var k = YeniKiraci("HTTP tahsilat kasasız");
        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson(
            $"/api/v1/cari/{k.CariId}/tahsilat",
            new { tlTutari = 1_000m, odemeTuru = "NAKIT", kurSnapshotId = k.KurId });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        // Yanıt BİÇİMİ bu 400'ü kimin ürettiğini söyler: model doğrulaması
        // (IValidatableObject) message alanını DİZİ olarak döndürür; servis içi
        // BadRequestException ise düz metin. Yani bu iddia DTO kapısını sınar,
        // servis içi elle kontrolü DEĞİL (onu servis testleri sınıyor).
        var hataJson = await cevap.Json();
        Assert.Equal(System.Text.Json.JsonValueKind.Array, hataJson.GetProperty("message").ValueKind);
        Assert.Contains("kasa", hataJson.GetProperty("message")[0].GetString()!, StringComparison.OrdinalIgnoreCase);

        // Kapı GERÇEKTEN kapandı: ne cari hareketi ne kasa hareketi yazıldı.
        using var db = f.Vt.OlusturSistem();
        Assert.False(await db.Hareketler.AsNoTracking()
            .AnyAsync(h => h.TenantId == k.TenantId && h.Tur == "TAHSILAT"));
        Assert.False(await db.KasaHareketleri.AsNoTracking()
            .AnyAsync(h => h.TenantId == k.TenantId));
    }

    [Fact]
    public async Task KasaIdli_NAKIT_tahsilat_200_ve_kasa_bakiyesi_ARTAR()
    {
        var k = YeniKiraci("HTTP tahsilat kasalı");
        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson(
            $"/api/v1/cari/{k.CariId}/tahsilat",
            new { tlTutari = 1_000m, odemeTuru = "NAKIT", kasaId = k.KasaId, kurSnapshotId = k.KurId });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(TestKurulum.KasaAcilis + 1_000m, TestKurulum.KasaBakiye(db, k.KasaId));
        var kh = await db.KasaHareketleri.AsNoTracking().SingleAsync(x => x.TenantId == k.TenantId);
        Assert.Equal(1_000m, kh.Tutar);
    }

    [Fact]
    public async Task Doviz_kasasina_TL_tahsilat_400_doner()
    {
        var k = YeniKiraci("HTTP tahsilat USD kasa", kasaDoviz: "USD");
        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson(
            $"/api/v1/cari/{k.CariId}/tahsilat",
            new { tlTutari = 750m, odemeTuru = "KART", kasaId = k.KasaId, kurSnapshotId = k.KurId });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Contains("TRY", await cevap.Metin());
    }

    // ── "Kasasız tahsilat" raporu — Mehmet koşul 3 ───────────────────────────

    [Fact]
    public async Task Kasasiz_tahsilat_raporu_TEZGAHTARA_kapali_403()
    {
        var k = YeniKiraci("HTTP kasasız rapor yetki");
        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR")
            .GetAsync("/api/v1/cari/kasasiz-tahsilatlar?esik=2026-09-16");

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    [Fact]
    public async Task Kasasiz_tahsilat_raporu_ESIKSIZ_400_doner()
    {
        // Eşik TAHMİN EDİLMEZ: yapılandırma da sorgu da boşsa fail-closed.
        var k = YeniKiraci("HTTP kasasız rapor eşiksiz");
        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .GetAsync("/api/v1/cari/kasasiz-tahsilatlar");

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    [Fact]
    public async Task Kasasiz_tahsilat_raporu_SAHIP_icin_200_ve_esigi_yansitir()
    {
        var k = YeniKiraci("HTTP kasasız rapor sahip");
        var esik = DateTime.UtcNow.AddMinutes(5).ToString("O");
        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .GetAsync($"/api/v1/cari/kasasiz-tahsilatlar?esik={Uri.EscapeDataString(esik)}");

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var json = await cevap.Json();
        Assert.Equal(0, json.GetProperty("adet").GetInt32());
        // camelCase sözleşmesi
        Assert.True(json.TryGetProperty("satirlar", out _));
    }
}
