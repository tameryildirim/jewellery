using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// DG-1 (20.10) — yaklaşan doğum günleri: aktif + doğum tarihi yazılı müşteriler, yıl dönümü bugünden ≤ N gün; pasif ve uzak olanlar yok.
[Collection("http")]
public sealed class DG1DogumGunuTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Onumuzdeki_30_gunde_dogum_gunu_olanlar_gun_sirasiyla_pasif_ve_uzak_olanlar_haric()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "DG1-dogum");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var bugun = Zaman.YerelBugun();
        using (var db = f.Vt.Olustur(k))
        {
            Cari Yap(string ad, DateOnly dogum, bool aktif = true) => new()
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, Ad = ad, Tur = "MUSTERI", Aktif = aktif, DogumTarihi = dogum,
                Telefon = "05321112233", Guncellendi = DateTime.UtcNow,
            };
            db.Cariler.AddRange(
                Yap("Üç Gün", new DateOnly(1990, bugun.AddDays(3).Month, bugun.AddDays(3).Day)),
                Yap("Bugün", new DateOnly(1985, bugun.Month, bugun.Day)),
                Yap("Uzak", new DateOnly(1990, bugun.AddDays(60).Month, bugun.AddDays(60).Day)),
                Yap("Pasif", new DateOnly(1990, bugun.AddDays(1).Month, bugun.AddDays(1).Day), aktif: false));
            await db.SaveChangesAsync();
        }
        var l = await s.GetFromJsonAsync<JsonElement>("/api/v1/cari/dogum-gunleri?gun=30");
        var adlar = l.GetProperty("satirlar").EnumerateArray().Select(x => x.GetProperty("ad").GetString()).ToList();
        Assert.Equal(["Bugün", "Üç Gün"], adlar);
        var bugunSatir = l.GetProperty("satirlar").EnumerateArray().First();
        Assert.Equal(0, bugunSatir.GetProperty("gunSonra").GetInt32());
        Assert.Equal(bugun.Year - 1985, bugunSatir.GetProperty("yas").GetInt32());
        Assert.Equal(30, l.GetProperty("gun").GetInt32());
    }
}
