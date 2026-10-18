using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Contracts.Cikti;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-10 (18.10, rakip eşitleme) — cari hesap mutabakat formu: /api/v1/cikti/CARI_MUTABAKAT/{cariId}.
// Bakiyeler ekrandakiyle AYNI kaynaktan, yönü yazılı ve işaretsiz; cevap satırları ve iki imza; Peşin Müşteri 400;
// başka işletmenin carisi 404.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class D1MutabakatTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Veresiye_satis_sonrasi_form_TL_bakiyesini_bize_borclu_ve_isaretsiz_yazar()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1-mutabakat");
        decimal birim;
        using (var db = f.Vt.OlusturSistem())
        {
            var kur = db.KurSnapshots.AsNoTracking().Single(x => x.Id == k.KurId).AltinHasFiyati;
            birim = Math.Ceiling(k.UrunHasGrami * kur * 1.10m);
        }
        var s = f.Ortam.Istemci(k, "SAHIP");
        Assert.True((await s.PostAsJsonAsync("/api/v1/islemler/musteri-kimlik", new { cariId = k.CariId, kimlikNo = "10000000146" })).IsSuccessStatusCode);
        var r = await s.PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
        {
            cariId = k.CariId,
            kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = birim, hediye = false } },
            odemeler = new[] { new { arac = "VERESIYE", tutarTl = birim, kasaId = (string?)null } },
            kurSnapshotId = k.KurId, fisIskontoYuzde = 0m, belgeTuru = "FATURA",
        });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        decimal tl;
        using (var db = f.Vt.OlusturSistem()) tl = db.CariBakiyeler.AsNoTracking().Single(b => b.CariId == k.CariId).TlBakiye;
        Assert.True(tl > 0m);

        var b = await s.GetFromJsonAsync<OrtakBelge>($"/api/v1/cikti/{BelgeTurleri.CariMutabakat}/{k.CariId}");
        Assert.Equal("CARİ HESAP MUTABAKAT FORMU", b!.BelgeAdi);
        var para = b.Bolumler.Single(x => x.Baslik == "Para bakiyesi");
        var tlSatir = para.Satirlar.Single(x => x.Hucreler[0] == "Türk lirası");
        Assert.Equal("Bize borçlu", tlSatir.Hucreler[1]);
        Assert.Equal(tl, decimal.Parse(tlSatir.Hucreler[2], System.Globalization.CultureInfo.InvariantCulture));
        Assert.Single(b.Bolumler.Single(x => x.Baslik == "Altın bakiyesi").Satirlar);
        Assert.Equal(2, b.Bolumler.Single(x => x.Baslik == "Cevabınız").Satirlar.Count);
        Assert.Equal(2, b.ImzaBasliklari.Length);
        Assert.Contains("Mutabık olup olmadığınızı", b.Not);
    }

    [Fact]
    public async Task Pesin_musteri_400_baska_isletme_404()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1-mutabakat-k");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var pesin = (await (await s.GetAsync("/api/v1/cari/sistem-carisi")).Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.BadRequest, (await s.GetAsync($"/api/v1/cikti/{BelgeTurleri.CariMutabakat}/{pesin}")).StatusCode);

        Kiraci baska;
        using (var db = f.Vt.OlusturSistem()) baska = TestKurulum.KiraciKur(db, "D1-mutabakat-b");
        Assert.Equal(HttpStatusCode.NotFound,
            (await f.Ortam.Istemci(baska, "SAHIP").GetAsync($"/api/v1/cikti/{BelgeTurleri.CariMutabakat}/{k.CariId}")).StatusCode);
        // Boş hesapta da form çıkar: TL ve has satırı "Borç yok".
        var bos = await s.GetFromJsonAsync<OrtakBelge>($"/api/v1/cikti/{BelgeTurleri.CariMutabakat}/{k.CariId}");
        Assert.Equal("Borç yok", bos!.Bolumler.Single(x => x.Baslik == "Para bakiyesi").Satirlar[0].Hucreler[1]);
    }
}
