using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// D1-20 "dene-gör" (20.10) — prim simülasyonu: oran verilince prim o oranla hesaplanır, kayıtlı yüzde değişmez, yanıt oranı söyler.
[Collection("http")]
public sealed class D120PrimSimulasyonTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Oran_verilince_prim_o_oranla_hesaplanir_kayitli_yuzde_degismez_gecersiz_oran_400()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D120-sim");
        var s = f.Ortam.Istemci(k, "SAHIP");
        using (var db = f.Vt.Olustur(k))
        {
            (await db.Kullanicilar.SingleAsync(u => u.Id == k.KullaniciId)).PrimYuzde = 2m;
            var now = DateTime.UtcNow;
            HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(20_000m), new Hareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS", Tarih = now, TlTutari = 20_000m, HasGrami = 2m,
                Maden = Maden.Altin, ParaBirimi = "TRY", KurSnapshotId = k.KurId, CariId = k.CariId, UrunId = k.UrunId,
                KullaniciId = k.KullaniciId, PersonelId = k.KullaniciId, Olusturuldu = now,
            });
            await db.SaveChangesAsync();
        }
        var bugun = Zaman.YerelBugun().ToString("yyyy-MM-dd");
        var normal = await s.GetFromJsonAsync<JsonElement>($"/api/v1/raporlar/prim?baslangic={bugun}&bitis={bugun}");
        var satir = normal.GetProperty("satirlar").EnumerateArray().Single(x => x.GetProperty("personelId").GetString() == k.KullaniciId);
        Assert.Equal(400m, satir.GetProperty("primTl").GetDecimal());   // 20.000 × %2
        Assert.Equal(JsonValueKind.Null, normal.GetProperty("simulasyonOran").ValueKind);

        var sim = await s.GetFromJsonAsync<JsonElement>($"/api/v1/raporlar/prim?baslangic={bugun}&bitis={bugun}&oran=5");
        var simSatir = sim.GetProperty("satirlar").EnumerateArray().Single(x => x.GetProperty("personelId").GetString() == k.KullaniciId);
        Assert.Equal(1_000m, simSatir.GetProperty("primTl").GetDecimal());   // 20.000 × %5
        Assert.Equal(5m, simSatir.GetProperty("primYuzde").GetDecimal());
        Assert.Equal(5m, sim.GetProperty("simulasyonOran").GetDecimal());
        Assert.Equal(1_000m, sim.GetProperty("toplamPrimTl").GetDecimal());

        Assert.Equal(HttpStatusCode.BadRequest, (await s.GetAsync($"/api/v1/raporlar/prim?baslangic={bugun}&bitis={bugun}&oran=150")).StatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(2m, (await db.Kullanicilar.AsNoTracking().SingleAsync(u => u.Id == k.KullaniciId)).PrimYuzde);   // kayıt değişmedi
    }
}
