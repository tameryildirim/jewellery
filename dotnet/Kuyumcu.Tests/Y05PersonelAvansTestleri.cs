using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// Y-05 (20.10) — personel avansı: gider kaydı personele bağlanır (aktif kullanıcı; yabancı 400), prim raporunda avans düşülür (net ödenecek).
[Collection("http")]
public sealed class Y05PersonelAvansTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Avans_gideri_personele_baglanir_prim_raporunda_dusulur_yabanci_personel_400()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "Y05-avans");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var kat = await s.PostAsJsonAsync("/api/v1/gider-kategorileri", new { ad = "Personel" });
        var katId = (await kat.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        using (var db = f.Vt.Olustur(k))
        {
            (await db.Kullanicilar.SingleAsync(u => u.Id == k.KullaniciId)).PrimYuzde = 10m;
            var now = DateTime.UtcNow;
            HareketFabrikasi.Ekle(db, OdemeIzi.Nakitten(50_000m), new Hareket
            {
                Id = Kimlik.Ulid(), TenantId = k.TenantId, SubeId = k.SubeId, Tur = "SATIS", Tarih = now, TlTutari = 50_000m, HasGrami = 5m,
                Maden = Maden.Altin, ParaBirimi = "TRY", KurSnapshotId = k.KurId, CariId = k.CariId, UrunId = k.UrunId,
                KullaniciId = k.KullaniciId, PersonelId = k.KullaniciId, Olusturuldu = now,
            });
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync("/api/v1/giderler", new { kasaId = k.KasaId, giderKategoriId = katId, tutar = 1_000m, personelId = "YOK" })).StatusCode);
        var g = await s.PostAsJsonAsync("/api/v1/giderler", new { kasaId = k.KasaId, giderKategoriId = katId, tutar = 1_000m, aciklama = "Ekim avansı", personelId = k.KullaniciId });
        Assert.True(g.IsSuccessStatusCode, await g.Content.ReadAsStringAsync());
        var giderId = (await g.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        using (var db = f.Vt.OlusturSistem())
            Assert.Equal(k.KullaniciId, (await db.KasaHareketleri.AsNoTracking().SingleAsync(x => x.Id == giderId)).PersonelId);

        var liste = await s.GetFromJsonAsync<JsonElement>("/api/v1/giderler?limit=50");
        var satir = liste.GetProperty("satirlar").EnumerateArray().Single(x => x.GetProperty("id").GetString() == giderId);
        Assert.False(string.IsNullOrEmpty(satir.GetProperty("personelAd").GetString()));

        var bugun = Zaman.YerelBugun().ToString("yyyy-MM-dd");
        var prim = await s.GetFromJsonAsync<JsonElement>($"/api/v1/raporlar/prim?baslangic={bugun}&bitis={bugun}");
        var p = prim.GetProperty("satirlar").EnumerateArray().Single(x => x.GetProperty("personelId").GetString() == k.KullaniciId);
        Assert.Equal(5_000m, p.GetProperty("primTl").GetDecimal());    // 50.000 × %10
        Assert.Equal(1_000m, p.GetProperty("avansTl").GetDecimal());
        Assert.Equal(4_000m, p.GetProperty("netOdemeTl").GetDecimal());
    }
}
