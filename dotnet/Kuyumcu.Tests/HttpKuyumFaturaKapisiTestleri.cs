using System.Net;
using System.Net.Http.Json;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// KUYUM FATURASINDA HAS BEDELİ — FAIL-CLOSED KAPI (paket UX-1-C2, 38-A devri).
//
// 38-A hasım doğrulayıcısının kapanmayan ORTA bulgusu:
//   FaturaModulu ELLE yolunda `hasDeger = dto.HasDeger ?? 0m` yazıyordu.
//   KdvTuru = KUYUM + hareket bağı YOK + HasDeger gönderilmemiş bir POST'ta
//   has_deger = 0 yazılıyor ve KDV TÜM tutar üzerinden hesaplanıyordu — yani
//   kuyum faturası SESSİZCE standart faturaya dönüşüyor, vergi yanlış çıkıyordu.
//   Ekran bu yolu kapatıyordu (Faturalar.razor KuyumEngeli) ama API doğrudan
//   çağrılabilir; kapı sunucuda da kapatıldı.
//
// ⚠ REGRESYON SÖZLEŞMESİ: RaporFaturaTestleri.KuyumKdv_HasBedeliMatrahDisiKalir
//   HasDeger'i AÇIKÇA gönderir (381.476,63) — o test bu kapıdan etkilenmez ve
//   adı/gövdesi bu pakette DEĞİŞTİRİLMEDİ.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class HttpKuyumFaturaKapisiTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static object Govde(Kiraci k, string kdvTuru, decimal? hasDeger) => hasDeger is null
        ? new { cariId = k.CariId, tur = "SATIS", toplam = 10_000m, kdvOran = 0.20m, kdvTuru }
        : new { cariId = k.CariId, tur = "SATIS", toplam = 10_000m, kdvOran = 0.20m, kdvTuru, hasDeger };

    // ── (1) KUYUM + hareket bağı yok + has bedeli YOK → 400 ──────────────────

    [Fact]
    public async Task Kuyum_faturasinda_HAS_BEDELI_gonderilmezse_400_doner()
    {
        var k = YeniKiraci("C2 kuyum has yok");

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostAsJsonAsync("/api/v1/faturalar", Govde(k, "KUYUM", null));

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        var metin = await cevap.Content.ReadAsStringAsync();
        Assert.Contains("has bedeli", metin, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Satır YAZILMAZ: fail-closed kapı yarım kayıt bırakmaz.</summary>
    [Fact]
    public async Task Kuyum_faturasi_reddedilince_FATURA_SATIRI_YAZILMAZ()
    {
        var k = YeniKiraci("C2 kuyum satir yok");

        await f.Ortam.Istemci(k, "SAHIP")
            .PostAsJsonAsync("/api/v1/faturalar", Govde(k, "KUYUM", null));

        using var db = f.Vt.OlusturSistem();
        Assert.Empty(db.Faturalar.Where(x => x.TenantId == k.TenantId));
    }

    // ── (2) AÇIKÇA 0 gönderilse bile reddedilir ─────────────────────────────

    [Fact]
    public async Task Kuyum_faturasinda_has_bedeli_SIFIR_gonderilse_de_400_doner()
    {
        var k = YeniKiraci("C2 kuyum has sifir");

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostAsJsonAsync("/api/v1/faturalar", Govde(k, "KUYUM", 0m));

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    // ── (3) Has bedeli verilince kuyum faturası KESİLİR (kapı dar değil) ────

    [Fact]
    public async Task Kuyum_faturasi_has_bedeli_verilince_KESILIR_ve_KDV_matrah_disi_kalir()
    {
        var k = YeniKiraci("C2 kuyum has var");

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostAsJsonAsync("/api/v1/faturalar", Govde(k, "KUYUM", 8_000m));

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        var fatura = db.Faturalar.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(8_000m, fatura.HasDeger);
        // KDV yalnız (10.000 − 8.000) üzerinden: 2.000 − 2.000/1,20 = 333,33
        Assert.Equal(333.33m, Math.Round(fatura.KdvTutar, 2));
    }

    // ── (4) STANDART fatura bu kapıdan ETKİLENMEZ ───────────────────────────

    [Fact]
    public async Task Standart_fatura_has_bedeli_olmadan_KESILEBILIR()
    {
        var k = YeniKiraci("C2 standart");

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostAsJsonAsync("/api/v1/faturalar", Govde(k, "STANDART", null));

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var db = f.Vt.OlusturSistem();
        var fatura = db.Faturalar.Single(x => x.TenantId == k.TenantId);
        Assert.Equal(0m, fatura.HasDeger);
        Assert.Equal(1_666.67m, Math.Round(fatura.KdvTutar, 2));   // tüm tutar matrahta
    }

    // ── (5) 48-A doğrulayıcı devri: rol yetersizliği 409 DEĞİL 403 ──────────

    [Fact]
    public async Task TEZGAHTAR_storno_denemesi_403_alir_409_DEGIL()
    {
        var k = YeniKiraci("C2 storno rol");

        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync(
            $"/api/v1/islemler/{Kuyumcu.Api.Common.Kimlik.Ulid()}/storno",
            new { gerekce = "Yetkisiz rol denemesi — kapı 403 dönmeli, 409 değil.", kurSnapshotId = k.KurId });

        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        Assert.NotEqual(HttpStatusCode.Conflict, cevap.StatusCode);
    }
}
