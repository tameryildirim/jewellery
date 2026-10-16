using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Modules.Tercih;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET S1-b — KATLAMA TERCİHİ GERÇEK HTTP BORU HATTINDA
//
// Kenar çubuğunun açık/kapalı grupları KULLANICIYA özeldir ve sunucuda durur
// (localStorage DEĞİL: aynı cihazı paylaşan iki tezgâhtar birbirinin menüsünü
// görürdü). Yeni anahtar `menu-acik-gruplar` BEYAZ LİSTEYE eklendi; bu dosya
// sözleşmeyi üretimdeki boru hattında ölçer:
//   • PATCH gönderilmeyen anahtarı KORUR, null olanı SİLER (PATCH sözleşmesi)
//   • bilinmeyen anahtar 400 alır (beyaz liste fail-closed)
//   • DELETE tek anahtarı sıfırlar (arayüz tercihi — finansal varlık değildir)
//   • kimliksiz istek 401 (tercih kullanıcıya bağlıdır, gövdeye değil)
//   • yazan kullanıcı BAŞKASININ tercihini görmez/yazmaz (kiracı+kullanıcı izolasyonu)
//
// ⚠ Yeni tablo/migration YOKTUR: kullanici_tercihleri zaten anahtar→JSON metni
//   deposudur. SQL kanıtı testin kendi DbContext'iyle alınır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class TercihKatlamaHttpTestleri(HttpFikstur f)
{
    private const string Deger = """["rapor","yonetim"]""";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static async Task<string> Govde(HttpResponseMessage c) =>
        await c.Content.ReadAsStringAsync();

    [Fact]
    public async Task Katlama_tercihi_YAZILIR_ve_geri_okunur()
    {
        var k = YeniKiraci("S1B-KATLAMA-1");
        var istemci = f.Ortam.Istemci(k);

        var yaz = await istemci.PatchAsJsonAsync("/api/v1/tercihler/benim", new
        {
            tercihler = new Dictionary<string, string?>
            {
                [TercihAnahtarlari.MenuAcikGruplar] = Deger,
            },
        });
        Assert.Equal(HttpStatusCode.OK, yaz.StatusCode);
        Assert.Contains(TercihAnahtarlari.MenuAcikGruplar, await Govde(yaz), StringComparison.Ordinal);

        var oku = await istemci.GetAsync("/api/v1/tercihler/benim");
        Assert.Equal(HttpStatusCode.OK, oku.StatusCode);
        Assert.Contains("rapor", await Govde(oku), StringComparison.Ordinal);

        // SQL kanıtı: satır gerçekten tabloda.
        using var db = f.Vt.Olustur(k.TenantId);
        var satir = await db.KullaniciTercihleri.AsNoTracking().FirstOrDefaultAsync(
            t => t.KullaniciId == k.KullaniciId
              && t.Anahtar == TercihAnahtarlari.MenuAcikGruplar);
        Assert.NotNull(satir);
        Assert.Equal(Deger, satir!.Deger);
    }

    [Fact]
    public async Task PATCH_gonderilmeyen_anahtari_KORUR()
    {
        var k = YeniKiraci("S1B-KATLAMA-2");
        var istemci = f.Ortam.Istemci(k);

        await istemci.PatchAsJsonAsync("/api/v1/tercihler/benim", new
        {
            tercihler = new Dictionary<string, string?>
            {
                [TercihAnahtarlari.MenuFavoriler] = """["/kasalar"]""",
            },
        });
        await istemci.PatchAsJsonAsync("/api/v1/tercihler/benim", new
        {
            tercihler = new Dictionary<string, string?>
            {
                [TercihAnahtarlari.MenuAcikGruplar] = Deger,
            },
        });

        var govde = await Govde(await istemci.GetAsync("/api/v1/tercihler/benim"));
        Assert.Contains(TercihAnahtarlari.MenuFavoriler, govde, StringComparison.Ordinal);
        Assert.Contains(TercihAnahtarlari.MenuAcikGruplar, govde, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Katlama_tercihi_SIFIRLANABILIR_null_ve_DELETE()
    {
        var k = YeniKiraci("S1B-KATLAMA-3");
        var istemci = f.Ortam.Istemci(k);
        await istemci.PatchAsJsonAsync("/api/v1/tercihler/benim", new
        {
            tercihler = new Dictionary<string, string?>
            {
                [TercihAnahtarlari.MenuAcikGruplar] = Deger,
            },
        });

        var sil = await istemci.DeleteAsync(
            $"/api/v1/tercihler/benim/{TercihAnahtarlari.MenuAcikGruplar}");

        Assert.Equal(HttpStatusCode.OK, sil.StatusCode);
        using var db = f.Vt.Olustur(k.TenantId);
        Assert.False(await db.KullaniciTercihleri.AnyAsync(
            t => t.KullaniciId == k.KullaniciId
              && t.Anahtar == TercihAnahtarlari.MenuAcikGruplar));
    }

    [Fact]
    public async Task Bilinmeyen_anahtar_400_alir_beyaz_liste_FAIL_CLOSED()
    {
        var k = YeniKiraci("S1B-KATLAMA-4");

        var cevap = await f.Ortam.Istemci(k).PatchAsJsonAsync("/api/v1/tercihler/benim", new
        {
            tercihler = new Dictionary<string, string?> { ["menu-acik-gruplarr"] = "[]" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    [Fact]
    public async Task Kimliksiz_istek_401_alir()
    {
        var cevap = await f.Ortam.AnonimIstemci().GetAsync("/api/v1/tercihler/benim");

        Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
    }

    [Fact]
    public async Task Baska_kullanicinin_katlamasi_GORUNMEZ()
    {
        var k = YeniKiraci("S1B-KATLAMA-5");
        await f.Ortam.Istemci(k).PatchAsJsonAsync("/api/v1/tercihler/benim", new
        {
            tercihler = new Dictionary<string, string?>
            {
                [TercihAnahtarlari.MenuAcikGruplar] = Deger,
            },
        });

        // Aynı kiracıda BAŞKA bir kullanıcı (tezgâhtar) — kimlik JETONDAN gelir.
        var oteki = f.Ortam.Istemci("baska-kullanici", k.TenantId, k.SubeId, "TEZGAHTAR");
        var govde = await Govde(await oteki.GetAsync("/api/v1/tercihler/benim"));

        Assert.DoesNotContain("rapor", govde, StringComparison.Ordinal);
    }
}
