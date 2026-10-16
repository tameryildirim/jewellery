using System.Net;
using System.Text;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET 32 — KİRACI İZOLASYONU + ROL KAPISI [HTTP, gerçek MySQL]
//
// KiraciIzolasyonTestleri.cs SERVİS düzeyinde ölçer; ekstre uçları yalnız HTTP
// üzerinden kullanılabildiği (controller'da rol ve modül yetkisi var) için bu
// dosya HTTP koleksiyonundadır. İkisi aynı kuralı iki katmandan ölçer.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class EkstreIzolasyonTestleri(HttpFikstur f)
{
    private const string Baslik = "tarih;aciklama;borc;alacak;bakiye;referans;karsiIban;karsiUnvan";
    private const string Icerik = Baslik + "\n01.09.2026;A;;100,00;1;R1;;X\n";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static string Iban() =>
        TestIban.Uret();

    private async Task<string> HesapAc(Kiraci k)
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/bankalar", new
        {
            bankaAdi = "Ziraat", hesapAdi = "Ticari", iban = Iban(), paraBirimi = "TRY",
        });
        return (await cevap.Json()).GetProperty("id").GetString()!;
    }

    private async Task<(string Ithalat, string Satir)> Ekstre(Kiraci k, string hesapId)
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson(
            $"/api/v1/bankalar/{hesapId}/ekstre/yukle",
            new { dosyaAdi = "e.csv", format = "CSV", icerikBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Icerik)) });
        var ithalat = (await cevap.Json()).GetProperty("ithalatId").GetString()!;

        await using var db = f.Vt.OlusturSistem();
        var satir = await db.EkstreSatirlari.FirstAsync(s => s.IthalatId == ithalat);
        return (ithalat, satir.Id);
    }

    // ── Kiracı izolasyonu ───────────────────────────────────────────────────

    [Fact]
    public async Task Yabanci_kiracinin_hesabina_ekstre_yuklenemez_404()
    {
        var a = YeniKiraci("P32-IZO-A");
        var b = YeniKiraci("P32-IZO-B");
        var hesapA = await HesapAc(a);

        var cevap = await f.Ortam.Istemci(b, "SAHIP").PostJson(
            $"/api/v1/bankalar/{hesapA}/ekstre/yukle",
            new { dosyaAdi = "e.csv", format = "CSV", icerikBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Icerik)) });

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);

        await using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, await db.EkstreIthalatlari.CountAsync(i => i.TenantId == b.TenantId));
    }

    [Fact]
    public async Task Yabanci_kiracinin_ithalatini_GET_edemez_404()
    {
        var a = YeniKiraci("P32-IZO-C");
        var b = YeniKiraci("P32-IZO-D");
        var (ithalat, _) = await Ekstre(a, await HesapAc(a));

        var cevap = await f.Ortam.Istemci(b, "SAHIP")
            .GetAsync($"/api/v1/bankalar/ekstre/{ithalat}?limit=100&offset=0");

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
    }

    [Fact]
    public async Task Yabanci_kiracinin_satirini_PATCH_edemez_404()
    {
        var a = YeniKiraci("P32-IZO-E");
        var b = YeniKiraci("P32-IZO-F");
        var (_, satir) = await Ekstre(a, await HesapAc(a));

        var cevap = await f.Ortam.Istemci(b, "SAHIP").PatchAsync(
            $"/api/v1/bankalar/ekstre/satir/{satir}",
            System.Net.Http.Json.JsonContent.Create(new { durum = "ATLANDI" }));

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);

        await using var db = f.Vt.OlusturSistem();
        Assert.Equal(EkstreSatirDurumu.Yeni, (await db.EkstreSatirlari.FirstAsync(s => s.Id == satir)).Durum);
    }

    [Fact]
    public async Task Yabanci_kiracinin_ithalatini_ONAYLAYAMAZ_404()
    {
        var a = YeniKiraci("P32-IZO-G");
        var b = YeniKiraci("P32-IZO-H");
        var hesapA = await HesapAc(a);
        var (ithalat, satir) = await Ekstre(a, hesapA);

        var cevap = await f.Ortam.Istemci(b, "SAHIP").PostJson(
            $"/api/v1/bankalar/ekstre/{ithalat}/onayla", new { satirIdler = new[] { satir } });

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);

        await using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, await db.BankaHareketleri.CountAsync(h => h.BankaHesapId == hesapA));
    }

    [Fact]
    public async Task Yabanci_kiracinin_carisine_satir_baglanamaz_404()
    {
        var a = YeniKiraci("P32-IZO-I");
        var b = YeniKiraci("P32-IZO-J");
        var (_, satir) = await Ekstre(a, await HesapAc(a));

        var cevap = await f.Ortam.Istemci(a, "SAHIP").PatchAsync(
            $"/api/v1/bankalar/ekstre/satir/{satir}",
            System.Net.Http.Json.JsonContent.Create(new { cariId = b.CariId }));

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
    }

    [Fact]
    public async Task Uc_yeni_varlik_da_IKiraciVarlik_uygular()
    {
        // Global filtrenin uygulanabilmesinin ÖN KOŞULU budur; işaret
        // unutulursa KuyumcuDbContext zaten açılışta patlar (beyaz liste yok).
        Assert.True(typeof(IKiraciVarlik).IsAssignableFrom(typeof(EkstreIthalati)));
        Assert.True(typeof(IKiraciVarlik).IsAssignableFrom(typeof(EkstreSatiri)));
        Assert.True(typeof(IKiraciVarlik).IsAssignableFrom(typeof(CariIban)));
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Global_filtre_yabanci_kiracinin_satirlarini_GIZLER()
    {
        var a = YeniKiraci("P32-IZO-K");
        var b = YeniKiraci("P32-IZO-L");
        var (ithalat, _) = await Ekstre(a, await HesapAc(a));

        await using var dbB = f.Vt.Olustur(b);
        Assert.Empty(await dbB.EkstreSatirlari.Where(s => s.IthalatId == ithalat).ToListAsync());
        Assert.Empty(await dbB.EkstreIthalatlari.Where(i => i.Id == ithalat).ToListAsync());
    }

    // ── Rol kapısı ──────────────────────────────────────────────────────────

    [Fact]
    public async Task TEZGAHTAR_ekstre_yukleyemez_403()
    {
        var k = YeniKiraci("P32-ROL-A");
        var hesap = await HesapAc(k);

        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson(
            $"/api/v1/bankalar/{hesap}/ekstre/yukle",
            new { dosyaAdi = "e.csv", format = "CSV", icerikBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Icerik)) });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    [Fact]
    public async Task TEZGAHTAR_ekstre_onaylayamaz_403()
    {
        var k = YeniKiraci("P32-ROL-B");
        var (ithalat, satir) = await Ekstre(k, await HesapAc(k));

        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson(
            $"/api/v1/bankalar/ekstre/{ithalat}/onayla", new { satirIdler = new[] { satir } });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
    }

    [Fact]
    public async Task MUHASEBE_ekstre_yukleyebilir_200()
    {
        var k = YeniKiraci("P32-ROL-C");
        var hesap = await HesapAc(k);

        var cevap = await f.Ortam.Istemci(k, "MUHASEBE").PostJson(
            $"/api/v1/bankalar/{hesap}/ekstre/yukle",
            new { dosyaAdi = "e.csv", format = "CSV", icerikBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Icerik)) });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
    }

    [Fact]
    public async Task Kimliksiz_istek_401()
    {
        var k = YeniKiraci("P32-ROL-D");
        var hesap = await HesapAc(k);

        var cevap = await f.Ortam.AnonimIstemci().PostJson(
            $"/api/v1/bankalar/{hesap}/ekstre/yukle",
            new { dosyaAdi = "e.csv", format = "CSV", icerikBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Icerik)) });

        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    [Fact]
    public async Task Sablon_ucu_beklenen_basliklari_doner()
    {
        var k = YeniKiraci("P32-SABLON");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").GetAsync("/api/v1/bankalar/ekstre/sablon.csv");

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.Contains("tarih;aciklama;borc;alacak", await cevap.Metin());
    }
}
