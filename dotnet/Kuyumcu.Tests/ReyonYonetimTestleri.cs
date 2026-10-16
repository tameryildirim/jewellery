using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// REYON YÖNETİMİ VE YERLEŞİM UÇLARI — paket 25 Faz A [HTTP, gerçek MySQL]
//
// NEDEN HTTP: [RolGerekli] ve [ModulYetkisi] DENETLEYİCİ nitelikleridir; servis
// testi ikisini de görmez. Kiracı izolasyonu (404) ve DTO doğrulaması da ancak
// gerçek JSON bağlamasıyla sınanır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class ReyonYonetimTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad, decimal stok = 10m)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad, stokAdet: stok);
    }

    private async Task<string> ReyonAc(Kiraci k, string ad, string tip = "VITRIN")
    {
        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/reyonlar",
            new { subeId = k.SubeId, ad, tip });
        Assert.Equal(HttpStatusCode.Created, cevap.StatusCode);
        return (await cevap.Json()).GetProperty("id").GetString()!;
    }

    private async Task Yerlestir(Kiraci k, string reyonId, decimal adet, string rol = "SAHIP")
    {
        var c = await f.Ortam.Istemci(k, rol).PostJson("/api/v1/reyonlar/yerlesim",
            new { subeId = k.SubeId, urunId = k.UrunId, reyonId, adet });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
    }

    /// <summary>PATCH ile ad/tip/sorumlu güncellenir (UX-1-B ucu yeniden kullanılır).</summary>
    [Fact]
    public async Task PATCH_reyon_alanlarini_gunceller()
    {
        var k = YeniKiraci("P25 patch");
        var id = await ReyonAc(k, "Vitrin-1");

        var cevap = await f.Ortam.Istemci(k, "MUDUR").PatchAsJsonAsync(
            $"/api/v1/reyonlar/{id}",
            new { ad = "Pırlanta vitrini", tip = "KASA", sorumluKullaniciId = k.KullaniciId });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var govde = await cevap.Json();
        Assert.Equal("Pırlanta vitrini", govde.GetProperty("ad").GetString());
        Assert.Equal("KASA", govde.GetProperty("tip").GetString());
        Assert.Equal(k.KullaniciId, govde.GetProperty("sorumluKullaniciId").GetString());
        // UX-S23: yerleşik adet de yanıtta — ekran ikinci istek atmaz.
        Assert.Equal(0m, govde.GetProperty("yerlesikAdet").GetDecimal());
    }

    /// <summary>Aynı şubede aynı adla ikinci reyon → 409 (artık VERİTABANI da engeller).</summary>
    [Fact]
    public async Task Ayni_subede_ayni_adla_ikinci_reyon_409()
    {
        var k = YeniKiraci("P25 çakışma");
        await ReyonAc(k, "Vitrin-1");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/reyonlar",
            new { subeId = k.SubeId, ad = "Vitrin-1", tip = "VITRIN" });
        Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
    }

    /// <summary>TEZGAHTAR reyon KARTINI düzenleyemez (403), MUDUR düzenler (200).</summary>
    [Fact]
    public async Task PATCH_TEZGAHTAR_403_MUDUR_200()
    {
        var k = YeniKiraci("P25 rol");
        var id = await ReyonAc(k, "Vitrin-1");

        var tezgahtar = await f.Ortam.Istemci(k, "TEZGAHTAR")
            .PatchAsJsonAsync($"/api/v1/reyonlar/{id}", new { ad = "Olmaz" });
        Assert.Equal(HttpStatusCode.Forbidden, tezgahtar.StatusCode);

        var mudur = await f.Ortam.Istemci(k, "MUDUR")
            .PatchAsJsonAsync($"/api/v1/reyonlar/{id}", new { ad = "Olur" });
        Assert.Equal(HttpStatusCode.OK, mudur.StatusCode);
    }

    /// <summary>TEZGAHTAR yerleştirebilir/taşıyabilir ama GERİ ALAMAZ (SAHIP/MUDUR).</summary>
    [Fact]
    public async Task GeriAl_TEZGAHTAR_403()
    {
        var k = YeniKiraci("P25 geri al rol");
        var id = await ReyonAc(k, "Vitrin-1");
        await Yerlestir(k, id, 3m, "TEZGAHTAR");   // yerleştirme TEZGAHTAR'a açıktır

        var cevap = await f.Ortam.Istemci(k, "TEZGAHTAR").PostJson("/api/v1/reyonlar/geri-al",
            new { subeId = k.SubeId, urunId = k.UrunId, reyonId = id, adet = 1m });
        Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);

        var mudur = await f.Ortam.Istemci(k, "MUDUR").PostJson("/api/v1/reyonlar/geri-al",
            new { subeId = k.SubeId, urunId = k.UrunId, reyonId = id, adet = 1m });
        Assert.Equal(HttpStatusCode.OK, mudur.StatusCode);
    }

    /// <summary>M25-7 hedefi: taşıma DENETİM KAYDI yazar (append-only iz).</summary>
    [Fact]
    public async Task Tasima_denetim_kaydi_yazar()
    {
        var k = YeniKiraci("P25 denetim");
        var a = await ReyonAc(k, "A-VITRIN");
        var b = await ReyonAc(k, "B-KASA", "KASA");
        await Yerlestir(k, a, 5m);

        // ⚠ İKİ AYRI KAYIT VARDIR ve karıştırılmaz:
        //   (1) DenetimYazmaDinleyicisi'nin OTOMATİK satır izi (alan adları),
        //   (2) DenetimServisi.YazmaKaydet ile yazılan ANLAMLI iz
        //       ("kim, hangi reyondan hangi reyona, kaç adet").
        // DoD (2)'yi şart koşar; bu yüzden ölçüm (2) üzerinden yapılır.
        int once;
        using (var db = f.Vt.OlusturSistem())
            once = await db.DenetimKayitlari.CountAsync(d =>
                d.TenantId == k.TenantId && d.VarlikTuru == nameof(StokYerlesimi)
                && d.Detay != null && d.Detay.Contains("reyon taşıma"));

        var cevap = await f.Ortam.Istemci(k, "MUDUR").PostJson("/api/v1/reyonlar/tasima",
            new { subeId = k.SubeId, urunId = k.UrunId, kaynakReyonId = a, hedefReyonId = b, adet = 2m });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var oku = f.Vt.OlusturSistem();
        var anlamli = await oku.DenetimKayitlari.AsNoTracking()
            .Where(d => d.TenantId == k.TenantId && d.VarlikTuru == nameof(StokYerlesimi)
                        && d.Detay != null && d.Detay.Contains("reyon taşıma"))
            .ToListAsync();
        Assert.Equal(once + 1, anlamli.Count);

        var kayit = anlamli.OrderByDescending(d => d.Tarih).First();
        Assert.Contains("A-VITRIN", kayit.Detay);
        Assert.Contains("B-KASA", kayit.Detay);
        Assert.Contains("2", kayit.Detay!);
    }

    /// <summary>M25-6 hedefi: reyon içi taşıma DEFTERE satır YAZMAZ (ADR-0016).</summary>
    [Fact]
    public async Task Tasima_hareketler_defterine_satir_yazmaz()
    {
        var k = YeniKiraci("P25 defter");
        var a = await ReyonAc(k, "A-VITRIN");
        var b = await ReyonAc(k, "B-KASA", "KASA");
        await Yerlestir(k, a, 5m);

        int once;
        using (var db = f.Vt.OlusturSistem())
            once = await db.Hareketler.CountAsync(h => h.TenantId == k.TenantId);

        var cevap = await f.Ortam.Istemci(k, "MUDUR").PostJson("/api/v1/reyonlar/tasima",
            new { subeId = k.SubeId, urunId = k.UrunId, kaynakReyonId = a, hedefReyonId = b, adet = 2m });
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

        using var oku = f.Vt.OlusturSistem();
        Assert.Equal(once, await oku.Hareketler.CountAsync(h => h.TenantId == k.TenantId));
    }

    /// <summary>M25-5 hedefi: içinde mal duran reyon PASİFE ALINAMAZ (400).</summary>
    [Fact]
    public async Task Icinde_yerlesim_olan_reyon_pasife_alinamaz()
    {
        var k = YeniKiraci("P25 pasife alma");
        var id = await ReyonAc(k, "A-VITRIN");
        await Yerlestir(k, id, 3m);

        var kapali = await f.Ortam.Istemci(k, "MUDUR")
            .PatchAsJsonAsync($"/api/v1/reyonlar/{id}", new { aktif = false });
        Assert.Equal(HttpStatusCode.BadRequest, kapali.StatusCode);

        // Malı boşalt → artık pasife alınabilir (geri dönüş yolu var).
        var geri = await f.Ortam.Istemci(k, "MUDUR").PostJson("/api/v1/reyonlar/geri-al",
            new { subeId = k.SubeId, urunId = k.UrunId, reyonId = id, adet = 3m });
        Assert.Equal(HttpStatusCode.OK, geri.StatusCode);

        var acik = await f.Ortam.Istemci(k, "MUDUR")
            .PatchAsJsonAsync($"/api/v1/reyonlar/{id}", new { aktif = false });
        Assert.Equal(HttpStatusCode.OK, acik.StatusCode);
    }

    /// <summary>Yabancı kiracının reyonu 404 — varlığı sızdırılmaz (400 değil).</summary>
    [Fact]
    public async Task Yabanci_kiracinin_reyonu_404()
    {
        var a = YeniKiraci("P25 http kiracı A");
        var b = YeniKiraci("P25 http kiracı B");
        var yabanci = await ReyonAc(b, "B-VITRIN");

        var stok = await f.Ortam.Istemci(a, "SAHIP").GetAsync($"/api/v1/reyonlar/{yabanci}/stok");
        Assert.Equal(HttpStatusCode.NotFound, stok.StatusCode);

        var yerlesim = await f.Ortam.Istemci(a, "SAHIP").PostJson("/api/v1/reyonlar/yerlesim",
            new { subeId = a.SubeId, urunId = a.UrunId, reyonId = yabanci, adet = 1m });
        Assert.Equal(HttpStatusCode.NotFound, yerlesim.StatusCode);
    }

    /// <summary>Geçersiz adet sessizce yutulmaz: alan doğrulaması 400 döner.</summary>
    [Fact]
    public async Task Sifir_adet_400()
    {
        var k = YeniKiraci("P25 adet doğrulama");
        var id = await ReyonAc(k, "A-VITRIN");

        var cevap = await f.Ortam.Istemci(k, "SAHIP").PostJson("/api/v1/reyonlar/yerlesim",
            new { subeId = k.SubeId, urunId = k.UrunId, reyonId = id, adet = 0m });
        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
    }

    /// <summary>GET /urunler/{id}/yerlesim ATANMAMIŞ'ı hesaplayıp ayrı gösterir.</summary>
    [Fact]
    public async Task Urun_yerlesimi_atanmamisi_ayri_gosterir()
    {
        var k = YeniKiraci("P25 ürün yerleşimi");
        var id = await ReyonAc(k, "A-VITRIN");
        await Yerlestir(k, id, 4m);

        var cevap = await f.Ortam.Istemci(k, "SAHIP").GetAsync($"/api/v1/urunler/{k.UrunId}/yerlesim");
        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        var govde = await cevap.Json();
        var sube = govde.GetProperty("subeler").EnumerateArray().Single();
        Assert.Equal(10m, sube.GetProperty("stokAdedi").GetDecimal());
        Assert.Equal(4m, sube.GetProperty("yerlesikAdet").GetDecimal());
        Assert.Equal(6m, sube.GetProperty("atanmamis").GetDecimal());
    }
}
