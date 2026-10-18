using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-14 — mali müşavir salt okunur hesabı: muhasebenin gördüğünü görür, hiçbir kaydı değiştiremez.
// Sunucu kapısı (SaltOkunurKapisi) tek noktadan reddeder; işaret yalnız MUHASEBE rolünde verilebilir.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class D1SaltOkunurTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Salt_okunur_jeton_okur_ama_yazamaz_auth_serbest()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1SALT");
        var m = f.Ortam.Istemci(k.KullaniciId, k.TenantId, k.SubeId, "MUHASEBE", saltOkunur: true);

        Assert.Equal(HttpStatusCode.OK, (await m.GetAsync("/api/v1/cari")).StatusCode);
        var yaz = await m.PostAsJsonAsync("/api/v1/cari", new { ad = "Salt Deneme", tur = "BIREYSEL" });
        Assert.Equal(HttpStatusCode.Forbidden, yaz.StatusCode);
        Assert.Contains("yalnız okuma", JsonDocument.Parse(await yaz.Content.ReadAsStringAsync()).RootElement.GetProperty("message").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await m.PatchAsJsonAsync($"/api/v1/cari/{k.CariId}", new { ad = "X" })).StatusCode);
        // Oturum uçları kapıya takılmaz (çıkış yapabilmeli).
        Assert.NotEqual(HttpStatusCode.Forbidden, (await m.PostAsJsonAsync("/api/v1/auth/logout", new { })).StatusCode);
        // Aynı rol, işaretsiz → yazabilir (kapı yalnız işarete bakar).
        var normal = f.Ortam.Istemci(k.KullaniciId, k.TenantId, k.SubeId, "MUHASEBE");
        Assert.NotEqual(HttpStatusCode.Forbidden, (await normal.PostAsJsonAsync("/api/v1/cari", new { ad = "Salt Deneme", tur = "BIREYSEL" })).StatusCode);
    }

    [Fact]
    public async Task Salt_okunur_yalniz_muhasebe_rolunde_verilir_ve_listede_gorunur()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1SALT2");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var kotu = await s.PostAsJsonAsync("/api/v1/kullanicilar", new
        {
            ad = "Ali", soyad = "Yılmaz", email = "d1salt-kotu@ornek.com.tr", sifre = "Guclu.Sifre.2026!", rol = "TEZGAHTAR", saltOkunur = true, subeId = k.SubeId,
        });
        Assert.Equal(HttpStatusCode.BadRequest, kotu.StatusCode);
        Assert.Contains("muhasebe", JsonDocument.Parse(await kotu.Content.ReadAsStringAsync()).RootElement.GetProperty("message").GetString());

        var iyi = await s.PostAsJsonAsync("/api/v1/kullanicilar", new
        {
            ad = "Ayşe", soyad = "Demir", email = "d1salt-mm@ornek.com.tr", sifre = "Guclu.Sifre.2026!", rol = "MUHASEBE", saltOkunur = true, subeId = k.SubeId,
        });
        Assert.True(iyi.IsSuccessStatusCode, await iyi.Content.ReadAsStringAsync());
        string id;
        using (var db = f.Vt.OlusturSistem())
        {
            var u = db.Kullanicilar.AsNoTracking().Single(x => x.TenantId == k.TenantId && x.Email == "d1salt-mm@ornek.com.tr");
            Assert.True(u.SaltOkunur);
            id = u.Id;
        }
        var liste = await s.GetStringAsync("/api/v1/kullanicilar");
        Assert.Contains("\"saltOkunur\":true", liste);

        // Rol değişince işaret düşmezse 400; işaret kapatılarak rol değişir.
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PatchAsJsonAsync($"/api/v1/kullanicilar/{id}/rol", new { rol = "TEZGAHTAR", saltOkunur = true })).StatusCode);
        Assert.True((await s.PatchAsJsonAsync($"/api/v1/kullanicilar/{id}/rol", new { rol = "TEZGAHTAR", saltOkunur = false })).IsSuccessStatusCode);
        using (var db = f.Vt.OlusturSistem())
            Assert.False(db.Kullanicilar.AsNoTracking().Single(x => x.Id == id).SaltOkunur);
    }
}
