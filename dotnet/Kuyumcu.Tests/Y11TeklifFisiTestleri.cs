using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// Y-11 (20.10, rakip taraması § 4.1) — fiyat teklif fişi: kur kilidi zorunlu, numara TKL-…, geçerlilik = kilit bitişi,
// döküm /cikti/TEKLIF/{id}, süresi dolan teklif tezgâha alınmaz (409), normal bekletme değişmedi.
[Collection("http")]
public sealed class Y11TeklifFisiTestleri(HttpFikstur f)
{
    private static object Icerik(Kiraci k) => new
    {
        cariId = k.CariId,
        kalemler = new[] { new { urunId = k.UrunId, urunAdi = "x", adet = 1m, birimFiyat = 10_000m, hediye = false, satirIskontoYuzde = 0m } },
        fisIskontoYuzde = (decimal?)null, aciklama = "Düğün seti",
    };

    [Fact]
    public async Task Teklif_kur_kilidiyle_verilir_numara_ve_gecerlilik_tasir_dokumu_basilir_suresi_dolunca_alinmaz()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "Y11-teklif");
        var s = f.Ortam.Istemci(k, "SAHIP");

        // Kilitsiz → 400
        var kilitsiz = await s.PostAsJsonAsync("/api/v1/bekleyen-fisler", new { icerik = Icerik(k), teklif = true });
        Assert.Equal(HttpStatusCode.BadRequest, kilitsiz.StatusCode);

        var kilit = await s.PostAsJsonAsync("/api/v1/kur-kilitleri", new { kurSnapshotId = k.KurId, cariId = k.CariId, sureDakika = 60 });
        Assert.True(kilit.IsSuccessStatusCode, await kilit.Content.ReadAsStringAsync());
        var kj = JsonSerializer.Deserialize<JsonElement>(await kilit.Content.ReadAsStringAsync());
        var kilitId = kj.GetProperty("id").GetString()!;

        var r = await s.PostAsJsonAsync("/api/v1/bekleyen-fisler", new { icerik = Icerik(k), teklif = true, kurKilidiId = kilitId });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        var o = JsonSerializer.Deserialize<JsonElement>(await r.Content.ReadAsStringAsync());
        var id = o.GetProperty("id").GetString()!;
        var no = o.GetProperty("teklifNo").GetString()!;
        Assert.StartsWith("TKL-", no);
        using (var db = f.Vt.OlusturSistem())   // geçerlilik = kilidin bitişi (veritabanında birebir; JSON saat dilimi biçimi karışmasın)
        {
            var b = await db.BekleyenFisler.AsNoTracking().SingleAsync(x => x.Id == id);
            var kl = await db.KurKilitleri.AsNoTracking().SingleAsync(x => x.Id == kilitId);
            Assert.Equal(kl.Bitis, b.TeklifBitis);
            Assert.Equal(kilitId, b.KurKilidiId);
        }
        Assert.Equal(10_000m, o.GetProperty("toplamTl").GetDecimal());

        // Liste satırında numara + bitiş; döküm
        var liste = await s.GetFromJsonAsync<JsonElement>("/api/v1/bekleyen-fisler");
        var satir = liste.EnumerateArray().Single(x => x.GetProperty("id").GetString() == id);
        Assert.Equal(no, satir.GetProperty("teklifNo").GetString());
        var dokum = await s.GetStringAsync($"/api/v1/cikti/TEKLIF/{id}");
        Assert.Contains("FİYAT TEKLİFİ", dokum);
        Assert.Contains(no, dokum);
        Assert.Contains("Geçerlilik:", dokum);
        Assert.Contains("Has fiyatı (kilitli)", dokum);
        Assert.Contains("Düğün seti", dokum);

        // Geri alma: içerik kilit kimliğiyle döner
        var al = await s.PostAsJsonAsync($"/api/v1/bekleyen-fisler/{id}/al", new { });
        Assert.True(al.IsSuccessStatusCode, await al.Content.ReadAsStringAsync());
        var ic = JsonSerializer.Deserialize<JsonElement>(await al.Content.ReadAsStringAsync());
        Assert.Equal(kilitId, ic.GetProperty("kurKilidiId").GetString());

        // Süresi dolan teklif alınmaz (409); normal bekletme etkilenmez
        var r2 = await s.PostAsJsonAsync("/api/v1/bekleyen-fisler", new { icerik = Icerik(k), teklif = true, kurKilidiId = kilitId });
        var id2 = JsonSerializer.Deserialize<JsonElement>(await r2.Content.ReadAsStringAsync()).GetProperty("id").GetString()!;
        using (var db = f.Vt.OlusturSistem())
        {
            var b = await db.BekleyenFisler.SingleAsync(x => x.Id == id2);
            b.TeklifBitis = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await s.PostAsJsonAsync($"/api/v1/bekleyen-fisler/{id2}/al", new { })).StatusCode);
        Assert.Contains("SÜRESİ DOLMUŞTUR", await s.GetStringAsync($"/api/v1/cikti/TEKLIF/{id2}"));
        var normal = await s.PostAsJsonAsync("/api/v1/bekleyen-fisler", new { icerik = Icerik(k) });
        var nj = JsonSerializer.Deserialize<JsonElement>(await normal.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, nj.GetProperty("teklifNo").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, (await s.GetAsync($"/api/v1/cikti/TEKLIF/{nj.GetProperty("id").GetString()}")).StatusCode);   // teklif değil
    }
}
