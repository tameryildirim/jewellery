using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-04 — kur kilidi: müşteriye söylenen kur, süresince satışta geçerli (tazelik kapısı yerine kilit kapısı).
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class D1KurKilidiTestleri(HttpFikstur f)
{
    private static string Ileti(string json) => JsonDocument.Parse(json).RootElement.GetProperty("message").GetString()!;

    private static async Task<HttpResponseMessage> Sat(HttpClient s, Kiraci k, decimal birim, string? cariId, string? kilitId) =>
        await s.PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = birim, hediye = false } },
            odemeler = new[] { new { arac = "KART", tutarTl = birim, kasaId = k.KasaId } },
            kurSnapshotId = k.KurId, kurKilidiId = kilitId, fisIskontoYuzde = 0m, belgeTuru = cariId == k.CariId ? "FATURA" : null,   // test ürünü fatura sınırının üstünde
        });

    [Fact]
    public async Task Bayat_kurla_kilitsiz_satis_reddedilir_kilitle_gecer_baska_musteri_dolmus_ve_kaldirilmis_kilit_gecmez()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1KILIT");
        var s = f.Ortam.Istemci(k, "SAHIP");
        decimal birim;
        using (var db = f.Vt.OlusturSistem())
        {
            var kur = db.KurSnapshots.AsNoTracking().Single(x => x.Id == k.KurId).AltinHasFiyati;
            birim = Math.Ceiling(k.UrunHasGrami * kur * 1.10m);
        }
        // Kimlik: test ürünü fatura/kimlik sınırına takılmasın diye müşteriye geçerli TC.
        Assert.True((await s.PostAsJsonAsync("/api/v1/islemler/musteri-kimlik", new { cariId = k.CariId, kimlikNo = "10000000146" })).IsSuccessStatusCode);

        var ac = await s.PostAsJsonAsync("/api/v1/kur-kilitleri", new { kurSnapshotId = k.KurId, cariId = k.CariId, sureDakika = 1440 });
        Assert.True(ac.IsSuccessStatusCode, await ac.Content.ReadAsStringAsync());
        var kilit = (await ac.Content.ReadFromJsonAsync<KurKilidiOzet>())!;
        Assert.Equal(k.CariId, kilit.CariId);
        Assert.Contains(await s.GetFromJsonAsync<List<KurKilidiOzet>>($"/api/v1/kur-kilitleri?cariId={k.CariId}") ?? [], x => x.Id == kilit.Id);

        // Kur "dünkü" olur (elle kur yalnız girildiği gün geçerli) — kilitsiz satış 409.
        using (var db = f.Vt.OlusturSistem())
            await db.KurSnapshots.Where(x => x.Id == k.KurId).ExecuteUpdateAsync(x => x.SetProperty(y => y.Olusturuldu, DateTime.UtcNow.AddDays(-1)));
        var kilitsiz = await Sat(s, k, birim, k.CariId, null);
        Assert.Equal(HttpStatusCode.Conflict, kilitsiz.StatusCode);
        Assert.Contains("önceki güne", Ileti(await kilitsiz.Content.ReadAsStringAsync()));

        // Başka müşteri (Peşin) ile kilit kullanılamaz.
        var pesin = (await (await s.GetAsync("/api/v1/cari/sistem-carisi")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        var baska = await Sat(s, k, birim, pesin, kilit.Id);
        Assert.Equal(HttpStatusCode.BadRequest, baska.StatusCode);
        Assert.Contains("başka bir müşteriye", Ileti(await baska.Content.ReadAsStringAsync()));

        // Kilitle aynı müşteriye satış GEÇER (kilidin kur kaydıyla).
        var kilitli = await Sat(s, k, birim, k.CariId, kilit.Id);
        Assert.True(kilitli.IsSuccessStatusCode, await kilitli.Content.ReadAsStringAsync());
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(k.KurId, db.Hareketler.AsNoTracking().First(h => h.TenantId == k.TenantId && h.Tur == "SATIS").KurSnapshotId);

        // Süresi dolan kilit 409.
        using (var db = f.Vt.OlusturSistem())
            await db.KurKilitleri.Where(x => x.Id == kilit.Id).ExecuteUpdateAsync(x => x.SetProperty(y => y.Bitis, DateTime.UtcNow.AddMinutes(-1)));
        var doldu = await Sat(s, k, birim, k.CariId, kilit.Id);
        Assert.Equal(HttpStatusCode.Conflict, doldu.StatusCode);
        Assert.Contains("süresi doldu", Ileti(await doldu.Content.ReadAsStringAsync()));
        Assert.DoesNotContain(await s.GetFromJsonAsync<List<KurKilidiOzet>>("/api/v1/kur-kilitleri") ?? [], x => x.Id == kilit.Id);

        // Kaldırılan kilit 409 (süre geri verilse bile).
        using (var db = f.Vt.OlusturSistem())
            await db.KurKilitleri.Where(x => x.Id == kilit.Id).ExecuteUpdateAsync(x => x.SetProperty(y => y.Bitis, DateTime.UtcNow.AddHours(1)));
        Assert.Equal(HttpStatusCode.NoContent, (await s.PostAsync($"/api/v1/kur-kilitleri/{kilit.Id}/kaldir", null)).StatusCode);
        var kaldirildi = await Sat(s, k, birim, k.CariId, kilit.Id);
        Assert.Equal(HttpStatusCode.Conflict, kaldirildi.StatusCode);
        Assert.Contains("kaldırılmış", Ileti(await kaldirildi.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task Bayat_kur_kilitlenemez_tezgahtar_en_cok_60_dakika_sure_sinirlari()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1KILIT2");
        var t = f.Ortam.Istemci(k, "TEZGAHTAR");
        Assert.Equal(HttpStatusCode.Forbidden, (await t.PostAsJsonAsync("/api/v1/kur-kilitleri", new { kurSnapshotId = k.KurId, sureDakika = 120 })).StatusCode);
        Assert.True((await t.PostAsJsonAsync("/api/v1/kur-kilitleri", new { kurSnapshotId = k.KurId, sureDakika = 60 })).IsSuccessStatusCode);
        var s = f.Ortam.Istemci(k, "SAHIP");
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync("/api/v1/kur-kilitleri", new { kurSnapshotId = k.KurId, sureDakika = 4 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync("/api/v1/kur-kilitleri", new { kurSnapshotId = k.KurId, sureDakika = 43201 })).StatusCode);

        using (var db = f.Vt.OlusturSistem())
            await db.KurSnapshots.Where(x => x.Id == k.KurId).ExecuteUpdateAsync(x => x.SetProperty(y => y.Olusturuldu, DateTime.UtcNow.AddDays(-1)));
        Assert.Equal(HttpStatusCode.Conflict, (await s.PostAsJsonAsync("/api/v1/kur-kilitleri", new { kurSnapshotId = k.KurId, sureDakika = 60 })).StatusCode);
    }
}
