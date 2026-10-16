using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// VEZNE UÇLARI — GERÇEK HTTP BORU HATTI (paket 19 Faz A).
//
// Servis testi [ModulYetkisi] ve [RolGerekli] niteliklerini GÖRMEZ. Ölçülen:
//   • fiş uçları ModulYetkisi("islem") — TEZGAHTAR geçer
//   • TAHSİL ucu AYRI controller'da ve ModulYetkisi("kasa") ile kapalı
//   • PATCH/GET /admin/vezne yalnız SAHİP (AdminController sınıf düzeyi kapı)
//
// ⚠ KOD GERÇEĞİ (Aşkın düzeltmesi): YetkiModulu.cs TEZGAHTAR varsayılanı "kasa"
//   modülünü İÇERİR. "TEZGAHTAR tahsil edemez" senaryosu bu yüzden önce
//   PUT /yetkiler ile kasa yetkisini KALDIRIR; ardından 403 bekler. Varsayılan
//   kiracıda ise tahsil 200 döner ve o da ayrıca sınanır.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class HttpVezneTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static object FisGovdesi(Kiraci k, decimal birimFiyat = 1_000m) => new
    {
        cariId = k.CariId,
        kurSnapshotId = k.KurId,
        aciklama = "HTTP vezne testi",
        kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = birimFiyat, iscilikTl = 0m } },
    };

    private static object TahsilGovdesi(Kiraci k, decimal tutar, string arac = "NAKIT") => new
    {
        kurSnapshotId = k.KurId,
        bacaklar = new[] { new { arac, tutarTl = tutar, kasaId = arac == "VERESIYE" ? null : k.KasaId } },
    };

    private async Task<(HttpClient Istemci, string Id)> VezneyeGonderilmisFis(
        Kiraci k, string rol = "SAHIP", decimal tutar = 1_000m)
    {
        var c = f.Ortam.Istemci(k, rol);
        var ac = await c.PostAsJsonAsync("/api/v1/vezne/fisler", FisGovdesi(k, tutar));
        Assert.Equal(HttpStatusCode.OK, ac.StatusCode);
        var id = (await ac.Json()).GetProperty("id").GetString()!;
        var veznele = await c.PostAsJsonAsync($"/api/v1/vezne/fisler/{id}/veznele", new { });
        Assert.Equal(HttpStatusCode.OK, veznele.StatusCode);
        return (c, id);
    }

    /// <summary>TEZGAHTAR rolünden bir modül anahtarını kaldırır (SAHİP yoluyla).</summary>
    private async Task ModuluKaldir(Kiraci k, string modul)
    {
        var sahip = f.Ortam.Istemci(k);
        var mevcut = await (await sahip.GetAsync("/api/v1/yetkiler")).Json();
        var matris = new Dictionary<string, string[]>();
        foreach (var rol in mevcut.GetProperty("matris").EnumerateObject())
        {
            var anahtarlar = rol.Value.EnumerateArray().Select(x => x.GetString()!).ToList();
            if (rol.Name == "TEZGAHTAR") anahtarlar.Remove(modul);
            matris[rol.Name] = [.. anahtarlar];
        }
        var y = await sahip.PutAsJsonAsync("/api/v1/yetkiler", new { matris });
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
    }

    // ── Fiş uçları: ModulYetkisi("vezne") ───────────────────────────────────
    // ⚠ P-09 (Y-39) SÖZLEŞME DEĞİŞİKLİĞİ: vezne fiş uçları "islem" anahtarını
    //   ödünç alıyordu; artık KENDİ anahtarı ("vezne") vardır. Varsayılan
    //   matris bugünkü erişimi korur (TEZGÂHTAR "vezne"yi de alır); değişen,
    //   yetkinin AYRI kapatılabilmesidir. PARA ucu hâlâ "kasa"dadır.

    [Fact]
    public async Task Tezgahtar_fis_acabilir_ve_vezneye_gonderebilir()
    {
        var k = YeniKiraci("P19H-tezgahtar");
        var (_, id) = await VezneyeGonderilmisFis(k, "TEZGAHTAR");

        using var db = f.Vt.OlusturSistem();
        var fis = db.VezneFisleri.AsNoTracking().Single(x => x.Id == id);
        Assert.Equal(VezneDurum.Veznede, fis.Durum);
        Assert.Equal(1_000m, fis.ToplamTl);
    }

    [Fact]
    public async Task Vezne_yetkisi_kaldirilan_tezgahtar_fis_ACAMAZ()
    {
        var k = YeniKiraci("P19H-vezne-yok");
        await ModuluKaldir(k, "vezne");

        var tezgahtar = f.Ortam.Istemci(k, "TEZGAHTAR");
        var y = await tezgahtar.PostAsJsonAsync("/api/v1/vezne/fisler", FisGovdesi(k));
        Assert.Equal(HttpStatusCode.Forbidden, y.StatusCode);
    }

    // ── Tahsil ucu: ModulYetkisi("kasa") ────────────────────────────────────

    [Fact]
    public async Task Kasa_yetkisi_OLAN_tezgahtar_ayrim_kapaliyken_tahsil_EDEBILIR()
    {
        // Kod gerçeği: TEZGAHTAR varsayılanı "kasa" İÇERİR → 200 beklenir.
        var k = YeniKiraci("P19H-kasa-var");
        var (c, id) = await VezneyeGonderilmisFis(k, "TEZGAHTAR");

        var y = await c.PostAsJsonAsync($"/api/v1/vezne/fisler/{id}/tahsil", TahsilGovdesi(k, 1_000m));
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var j = await y.Json();
        Assert.False(string.IsNullOrEmpty(j.GetProperty("hareketFisId").GetString()));
    }

    [Fact]
    public async Task Kasa_yetkisi_KALDIRILAN_tezgahtar_tahsil_EDEMEZ()
    {
        var k = YeniKiraci("P19H-kasa-yok");
        var (c, id) = await VezneyeGonderilmisFis(k, "TEZGAHTAR");
        await ModuluKaldir(k, "kasa");

        var y = await c.PostAsJsonAsync($"/api/v1/vezne/fisler/{id}/tahsil", TahsilGovdesi(k, 1_000m));
        Assert.Equal(HttpStatusCode.Forbidden, y.StatusCode);

        // Defterde satır DOĞMADI.
        using var db = f.Vt.OlusturSistem();
        Assert.Empty(db.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Vezne_ayrimi_acikken_hazirlayan_kendi_fisini_tahsil_edemez_400()
    {
        var k = YeniKiraci("P19H-dortgoz");
        var sahip = f.Ortam.Istemci(k);
        var ayar = await sahip.PatchAsJsonAsync("/api/v1/admin/vezne", new { zorunlu = true });
        Assert.Equal(HttpStatusCode.OK, ayar.StatusCode);

        var (c, id) = await VezneyeGonderilmisFis(k, "TEZGAHTAR");
        var y = await c.PostAsJsonAsync($"/api/v1/vezne/fisler/{id}/tahsil", TahsilGovdesi(k, 1_000m));
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
    }

    [Fact]
    public async Task Ikinci_tahsil_409_verir_ve_defterde_TEK_fis_kalir()
    {
        var k = YeniKiraci("P19H-cift");
        var (c, id) = await VezneyeGonderilmisFis(k);

        var ilk = await c.PostAsJsonAsync($"/api/v1/vezne/fisler/{id}/tahsil", TahsilGovdesi(k, 1_000m));
        Assert.Equal(HttpStatusCode.OK, ilk.StatusCode);
        var fisId = (await ilk.Json()).GetProperty("hareketFisId").GetString()!;

        var ikinci = await c.PostAsJsonAsync($"/api/v1/vezne/fisler/{id}/tahsil", TahsilGovdesi(k, 1_000m));
        Assert.Equal(HttpStatusCode.Conflict, ikinci.StatusCode);

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(1, db.Hareketler.AsNoTracking().Count(h => h.TenantId == k.TenantId && h.FisId == fisId));
        Assert.Equal(1, db.Hareketler.AsNoTracking().Count(h => h.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Baska_kiracinin_fis_kimligiyle_tahsil_404()
    {
        var a = YeniKiraci("P19H-kiraci-A");
        var b = YeniKiraci("P19H-kiraci-B");
        var (_, id) = await VezneyeGonderilmisFis(a);

        var bIstemci = f.Ortam.Istemci(b);
        var y = await bIstemci.PostAsJsonAsync($"/api/v1/vezne/fisler/{id}/tahsil", TahsilGovdesi(b, 1_000m));
        Assert.Equal(HttpStatusCode.NotFound, y.StatusCode);
    }

    // ── Ayar ucu: SAHİP ─────────────────────────────────────────────────────

    [Fact]
    public async Task Vezne_ayari_yalniz_SAHIP_tarafindan_degistirilir()
    {
        var k = YeniKiraci("P19H-ayar");
        var mudur = f.Ortam.Istemci(k, "MUDUR");
        var m = await mudur.PatchAsJsonAsync("/api/v1/admin/vezne", new { zorunlu = true });
        Assert.Equal(HttpStatusCode.Forbidden, m.StatusCode);

        var sahip = f.Ortam.Istemci(k);
        var once = await (await sahip.GetAsync("/api/v1/admin/vezne")).Json();
        Assert.False(once.GetProperty("zorunlu").GetBoolean());       // varsayılan KAPALI

        var s = await sahip.PatchAsJsonAsync("/api/v1/admin/vezne", new { zorunlu = true });
        Assert.Equal(HttpStatusCode.OK, s.StatusCode);
        var sonra = await (await sahip.GetAsync("/api/v1/admin/vezne")).Json();
        Assert.True(sonra.GetProperty("zorunlu").GetBoolean());

        // Denetim izi düşer (KVKK/ADR-0004): ayar değişikliği izlenebilir.
        using var db = f.Vt.OlusturSistem();
        Assert.Contains(db.DenetimKayitlari.AsNoTracking().Where(d => d.TenantId == k.TenantId),
            d => d.VarlikTuru == "KiraciAyari");
    }

    [Fact]
    public async Task Kuyruk_ucu_sunucu_sinirini_ve_ayar_bayragini_bildirir()
    {
        var k = YeniKiraci("P19H-kuyruk");
        var (c, _) = await VezneyeGonderilmisFis(k);

        var j = await (await c.GetAsync("/api/v1/vezne/kuyruk?durum=VEZNEDE&limit=200")).Json();
        Assert.Equal(VezneFisi.AzamiLimit, j.GetProperty("azamiLimit").GetInt32());
        Assert.False(j.GetProperty("vezneZorunlu").GetBoolean());
        Assert.Equal(1, j.GetProperty("toplam").GetInt32());
        Assert.False(j.GetProperty("dahaFazlasiVar").GetBoolean());
    }
}
