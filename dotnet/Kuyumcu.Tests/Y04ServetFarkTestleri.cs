using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// Y-04 (20.10) — gün kapanışı servet anlık görüntüsü yazar; pozisyon kartı son kapanışla farkı verir (bugünkü kapanış "önceki" sayılmaz).
[Collection("http")]
public sealed class Y04ServetFarkTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Gun_kapanisi_anlik_goruntu_yazar_ve_onceki_gunun_goruntusu_fark_olarak_doner()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "Y04-servet");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var bugun = Zaman.YerelBugun();
        using (var db = f.Vt.OlusturSistem())
        {
            db.ServetGunluk.Add(new ServetSnapshot { Id = Kimlik.Ulid(), TenantId = k.TenantId, Gun = bugun.AddDays(-1), NetTl = 100_000m, AltinNetHasGram = 10m, Olusturuldu = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        var kapanis = await s.PostAsJsonAsync($"/api/v1/kasalar/{k.KasaId}/gun-kapanisi", new { sayilanNakit = TestKurulum.KasaAcilis });
        Assert.True(kapanis.IsSuccessStatusCode, await kapanis.Content.ReadAsStringAsync());
        using (var db = f.Vt.OlusturSistem())
        {
            var bugunku = await db.ServetGunluk.AsNoTracking().SingleAsync(x => x.TenantId == k.TenantId && x.Gun == bugun);
            Assert.NotNull(bugunku.NetTl);
        }
        var p = await s.GetFromJsonAsync<JsonElement>("/api/v1/pozisyon");
        var nv = p.GetProperty("netVarlik");
        Assert.Equal(bugun.AddDays(-1).ToString("yyyy-MM-dd"), nv.GetProperty("oncekiGun").GetString());
        Assert.Equal(100_000m, nv.GetProperty("oncekiNetTl").GetDecimal());
        Assert.Equal(10m, nv.GetProperty("oncekiAltinNetHasGram").GetDecimal());
    }
}
