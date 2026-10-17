using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Contracts.Sayfalar.Faturalar;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// 18.10 — toplu fatura (Faturalog: "tek tuşla"; kuyumcu: gün sonu bugünün faturasız fişleri).
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class TopluFaturaHttpTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Bugunun_fisleri_listelenir_musterililer_kesilir_Pesin_sebebiyle_reddedilir_ikinci_kesim_tekrar_kesmez()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "TOPLU");
        var s = f.Ortam.Istemci(k, "SAHIP");
        // Hafif ürün: fiyat has bedelinin ÜSTÜNDE (özel matrah pozitif) ve kuyumcu fatura sınırının ALTINDA.
        var u = await s.PostAsJsonAsync("/api/v1/urunler", new { tip = "ZIYNET", kod = "TOPLU-2G", ad = "2 g Yüzük", ayarId = "K22", gramAgirlik = 2m, acilisStogu = new { adet = 5m } });
        Assert.True(u.IsSuccessStatusCode, await u.Content.ReadAsStringAsync());
        string urunId; decimal birim;
        using (var db = f.Vt.OlusturSistem())
        {
            var ur = db.Urunler.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Kod == "TOPLU-2G");
            urunId = ur.Id;
            var kur = db.KurSnapshots.AsNoTracking().Single(x => x.Id == k.KurId).AltinHasFiyati;
            birim = Math.Ceiling(ur.HasGrami * kur * 1.10m);
        }
        var pesin = (await (await s.GetAsync("/api/v1/cari/sistem-carisi")).Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetString();
        async Task Sat(string? cariId)
        {
            var r = await s.PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
            {
                cariId,
                kalemler = new[] { new { urunId, adet = 1m, birimFiyatTl = birim, hediye = false } },
                odemeler = new[] { new { arac = "KART", tutarTl = birim, kasaId = k.KasaId } },
                kurSnapshotId = k.KurId, fisIskontoYuzde = 0m,
            });
            Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        }
        await Sat(k.CariId);
        await Sat(k.CariId);
        await Sat(pesin);

        var l = await s.GetFromJsonAsync<TopluAdayListesi>("/api/v1/faturalar/toplu");
        Assert.Equal(3, l!.Adaylar.Count);
        Assert.Equal(2, l.Adaylar.Count(a => a.Kesilebilir));
        var pesinAday = Assert.Single(l.Adaylar, a => !a.Kesilebilir);
        Assert.Contains("Peşin", pesinAday.Sebep);

        var r1 = await s.PostAsJsonAsync("/api/v1/faturalar/toplu", new { anahtarlar = l.Adaylar.Select(a => a.Anahtar).ToList() });
        var sonuc = await r1.Content.ReadFromJsonAsync<TopluSonuc>();
        Assert.True(sonuc!.Kesilen == 2, string.Join(" | ", sonuc.Satirlar.Select(x => x.Hata)));
        Assert.Equal(1, sonuc.Hatali);
        Assert.All(sonuc.Satirlar.Where(x => x.FaturaId != null), x => Assert.Equal("KESILDI", x.Durum));
        using (var db = f.Vt.OlusturSistem())
        {
            var faturalar = await db.Faturalar.AsNoTracking().Where(x => x.TenantId == k.TenantId).ToListAsync();
            Assert.Equal(2, faturalar.Count);
            Assert.All(faturalar, x => { Assert.Equal("KUYUM", x.KdvTuru); Assert.False(string.IsNullOrEmpty(x.FaturaNo)); });
        }

        var l2 = await s.GetFromJsonAsync<TopluAdayListesi>("/api/v1/faturalar/toplu");
        Assert.Single(l2!.Adaylar);                                     // yalnız Peşin kaldı
        var r2 = await s.PostAsJsonAsync("/api/v1/faturalar/toplu", new { anahtarlar = sonuc.Satirlar.Where(x => x.FaturaId != null).Select(x => x.Anahtar).ToList() });
        Assert.Equal(0, (await r2.Content.ReadFromJsonAsync<TopluSonuc>())!.Kesilen);   // aynı fişe ikinci fatura YOK

        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/faturalar/toplu")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync("/api/v1/faturalar/toplu", new { anahtarlar = Array.Empty<string>() })).StatusCode);
    }
}
