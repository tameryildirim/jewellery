using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// KASA DÜZENLEME — paket UX-1 Faz B [HTTP, gerçek MySQL]
//
// İKİ AYRI İŞ BİR ARADA:
//  1) Bertan KOŞUL 1 (BLOKLAYICI): KasaGuncelleDto tek alanlıydı ve `Ad`
//     [Required] idi → "gövdede olmayan alan korunur" sözleşmesi bu uçta
//     YALANDI (yalnız para birimi göndermek 400 alırdı). Artık üç durum.
//  2) Mehmet K4: para birimi / şube yalnız GEÇMİŞİ TAMAMEN BOŞ kasada değişir.
//     Bakiye == 0 YETMEZ — kasa_hareketleri VE gun_kapanislari da boş olmalı.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class KasaDuzenlemeTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    /// <summary>Bakiyesiz, hareketsiz, kapanışsız yeni kasa (TestKurulum kasası 250.000 TL taşır).</summary>
    private async Task<string> TemizKasa(Kiraci k, string ad = "Döviz kasası")
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PostJson("/api/v1/kasalar", new { ad, subeId = k.SubeId, paraBirimi = "TRY" });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        return (await cevap.Json()).GetProperty("id").GetString()!;
    }

    private async Task<Kasa> KasaOku(string id)
    {
        using var db = f.Vt.OlusturSistem();
        return await db.Kasalar.AsNoTracking().SingleAsync(x => x.Id == id);
    }

    [Fact]
    public async Task Bakiyesiz_ve_hareketsiz_kasada_para_birimi_DEGISIR()
    {
        var k = YeniKiraci("UX1B kasa temiz");
        var id = await TemizKasa(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/kasalar/{id}", new { paraBirimi = "USD" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.Equal("USD", (await KasaOku(id)).ParaBirimi);
    }

    /// <summary>Bertan KOŞUL 1 — `Ad` gövdede YOKKEN istek 400 ALMAZ ve ad KORUNUR.</summary>
    [Fact]
    public async Task Ad_govdede_yoksa_KORUNUR_ve_400_ALINMAZ()
    {
        var k = YeniKiraci("UX1B kasa ad koru");
        var id = await TemizKasa(k, "Korunacak kasa");

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/kasalar/{id}", new { paraBirimi = "EUR" });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var satir = await KasaOku(id);
        Assert.Equal("Korunacak kasa", satir.Ad);
        Assert.Equal("EUR", satir.ParaBirimi);
    }

    /// <summary>Ad gönderilirse boş olamaz — kasanın adı zorunlu alandır.</summary>
    [Fact]
    public async Task Bos_ad_gonderilirse_400_ve_ad_degismez()
    {
        var k = YeniKiraci("UX1B kasa boş ad");
        var id = await TemizKasa(k, "Sağlam ad");

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/kasalar/{id}", new { ad = "  " });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Equal("Sağlam ad", (await KasaOku(id)).Ad);
    }

    /// <summary>M-UX1-03 hedefi — hareket görmüş kasada para birimi DEĞİŞMEZ (bakiye 0 olsa bile).</summary>
    [Fact]
    public async Task Hareketli_kasada_para_birimi_degismez()
    {
        var k = YeniKiraci("UX1B kasa hareketli");
        var id = await TemizKasa(k);

        // Bakiyeyi 0'da BIRAKIYORUZ: sınanan kapı BAKİYE değil, HAREKET GEÇMİŞİ.
        using (var db = f.Vt.Olustur(k))
        {
            db.KasaHareketleri.Add(new KasaHareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, KasaId = id,
                Tutar = 0m, ParaBirimi = "TRY", OdemeTuru = "NAKIT",
                Tarih = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/kasalar/{id}", new { paraBirimi = "USD" });

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        Assert.Equal("TRY", (await KasaOku(id)).ParaBirimi);
        Assert.Equal(0m, (await KasaOku(id)).Bakiye);
    }

    /// <summary>Gün kapanışı yapılmış kasada da değişmez (üçüncü kaynak — Mehmet K4).</summary>
    [Fact]
    public async Task Gun_kapanisli_kasada_para_birimi_degismez()
    {
        var k = YeniKiraci("UX1B kasa kapanışlı");
        var id = await TemizKasa(k);

        using (var db = f.Vt.Olustur(k))
        {
            db.GunKapanislari.Add(new GunKapanisi
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, KasaId = id, SubeId = k.SubeId,
                YerelGun = DateOnly.FromDateTime(DateTime.Today), SayilanNakit = 0m,
                DefterBakiye = 0m, Fark = 0m, KapatanKullaniciId = k.KullaniciId,
                KapanisZamani = DateTime.UtcNow, Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/kasalar/{id}", new { paraBirimi = "USD" });

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        Assert.Equal("TRY", (await KasaOku(id)).ParaBirimi);
    }

    /// <summary>Bakiyeli kasada şube değişimi 409 — satır olduğu gibi kalır.</summary>
    [Fact]
    public async Task Bakiyeli_kasada_sube_degismez()
    {
        var k = YeniKiraci("UX1B kasa bakiyeli");
        string ikinciSube;
        using (var db = f.Vt.OlusturSistem()) ikinciSube = TestKurulum.SubeEkle(db, k.TenantId);

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/kasalar/{k.KasaId}", new { subeId = ikinciSube });

        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        var satir = await KasaOku(k.KasaId);
        Assert.Equal(k.SubeId, satir.SubeId);
        Assert.Equal(TestKurulum.KasaAcilis, satir.Bakiye);
    }

    /// <summary>Temiz kasada şube gerçekten taşınır (kapı kapalı değil, KOŞULLU).</summary>
    [Fact]
    public async Task Temiz_kasada_sube_TASINIR()
    {
        var k = YeniKiraci("UX1B kasa taşı");
        string ikinciSube;
        using (var db = f.Vt.OlusturSistem()) ikinciSube = TestKurulum.SubeEkle(db, k.TenantId);
        var id = await TemizKasa(k);

        var cevap = await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/kasalar/{id}", new { subeId = ikinciSube });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.Equal(ikinciSube, (await KasaOku(id)).SubeId);
    }

    /// <summary>Yabancı kiracının şubesine taşınamaz (kiracı izolasyonu).</summary>
    [Fact]
    public async Task Yabanci_kiracinin_subesine_TASINAMAZ()
    {
        var a = YeniKiraci("UX1B kasa şube A");
        var b = YeniKiraci("UX1B kasa şube B");
        var id = await TemizKasa(a);

        var cevap = await f.Ortam.Istemci(a, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/kasalar/{id}", new { subeId = b.SubeId });

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        Assert.Equal(a.SubeId, (await KasaOku(id)).SubeId);
    }

    /// <summary>Mehmet (UX-1 Faz B): PATCH denetim izine alan adlarıyla yazar.</summary>
    [Fact]
    public async Task Kasa_PATCH_DENETIM_KAYDI_yazar()
    {
        var k = YeniKiraci("UX1B kasa denetim");
        var id = await TemizKasa(k);

        await f.Ortam.Istemci(k, "SAHIP")
            .PatchAsJsonAsync($"/api/v1/kasalar/{id}", new { paraBirimi = "USD" });

        using var db = f.Vt.OlusturSistem();
        var kayit = await db.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikTuru == "Kasa" && d.VarlikId == id
                        && d.Detay != null && d.Detay.Contains("değişen alanlar"))
            .SingleAsync();
        Assert.Contains("para_birimi", kayit.Detay!);
        Assert.Equal(k.KullaniciId, kayit.KullaniciId);
    }
}
