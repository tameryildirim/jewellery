using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// M7 — banka sağlayıcısı + demo bankası + çekici (müşteri 16.10: "düşer düşmez hemen şak ekranda
// görüyoruz… şimdilik simülasyon"). Testlerde arka plan çekicisi kapalıdır (Banka__CekimEtkin=false);
// çekim "havale gönder" / "şimdi çek" ile tetiklenir.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class M7BankaSaglayiciHttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "M7-" + ad);
    }

    private async Task<string> HesapAc(Kiraci k, bool sahte = true)
    {
        var s = f.Ortam.Istemci(k, "SAHIP");
        var c = await s.PostAsJsonAsync("/api/v1/bankalar", new { bankaAdi = "Demo Bank", hesapAdi = "Vadesiz", iban = TestIban.Uret(), paraBirimi = "TRY" });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var id = (await c.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
        if (sahte)
        {
            var p = await s.PutAsJsonAsync($"/api/v1/bankalar/{id}/saglayici", new { kod = "SAHTE" });
            Assert.True(p.IsSuccessStatusCode, await p.Content.ReadAsStringAsync());
        }
        return id;
    }

    [Fact]
    public async Task Demo_havale_hemen_iceri_alinir_IBANdan_musteri_bulunur_karar_kutusuna_duser_ikinci_cekim_YAZMAZ()
    {
        var k = YeniKiraci("havale");
        var hesap = await HesapAc(k);
        var s = f.Ortam.Istemci(k, "SAHIP");
        var iban = TestIban.Uret();
        Assert.True((await s.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/ibanlar", new { iban })).IsSuccessStatusCode);

        var h = await s.PostAsJsonAsync($"/api/v1/bankalar/{hesap}/sahte-havale", new { tutar = 2500m, gonderenAd = "Test Müşteri", gonderenIban = iban, aciklama = "bilezik" });
        var y = await h.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(h.IsSuccessStatusCode, y.ToString());
        Assert.Equal(1, y.GetProperty("yeni").GetInt32());

        using (var db = f.Vt.OlusturSistem())
        {
            var b = await db.BankaHareketleri.AsNoTracking().SingleAsync(x => x.TenantId == k.TenantId);
            Assert.StartsWith("SAHTE-", b.SaglayiciReferans);
            Assert.Equal(k.CariId, b.CariId);
            Assert.Equal("ESLESTI", b.Durum);
            Assert.Equal(2500m, b.Tutar);
            Assert.NotNull((await db.BankaHesaplari.AsNoTracking().SingleAsync(x => x.Id == hesap)).SonCekim);
        }
        var bekleyen = await s.GetFromJsonAsync<JsonElement>("/api/v1/bankalar/karar-bekleyen");
        Assert.Single(bekleyen.EnumerateArray());

        var tekrar = await (await s.PostAsync($"/api/v1/bankalar/{hesap}/cek", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, tekrar.GetProperty("yeni").GetInt32());
        using var db2 = f.Vt.OlusturSistem();
        Assert.Equal(1, await db2.BankaHareketleri.CountAsync(x => x.TenantId == k.TenantId));
    }

    [Fact]
    public async Task TC_ile_gelen_ve_bilinmeyen_gonderen_YENI_musteri_olarak_acilir()
    {
        var k = YeniKiraci("yeni");
        var hesap = await HesapAc(k);
        var s = f.Ortam.Istemci(k, "SAHIP");
        var h = await s.PostAsJsonAsync($"/api/v1/bankalar/{hesap}/sahte-havale", new { tutar = 900m, gonderenAd = "Hatice Demirkol", gonderenKimlikNo = "10000000146" });
        Assert.True(h.IsSuccessStatusCode, await h.Content.ReadAsStringAsync());
        using var db = f.Vt.OlusturSistem();
        var cari = await db.Cariler.AsNoTracking().SingleAsync(c => c.TenantId == k.TenantId && c.Ad == "Hatice Demirkol");
        Assert.Equal("10000000146", cari.VergiNo);
        Assert.Equal(cari.Id, (await db.BankaHareketleri.AsNoTracking().SingleAsync(x => x.TenantId == k.TenantId)).CariId);
    }

    [Fact]
    public async Task Saglayicisiz_hesapta_demo_havale_409_ve_cek_yeni_vermez()
    {
        var k = YeniKiraci("elle");
        var hesap = await HesapAc(k, sahte: false);
        var s = f.Ortam.Istemci(k, "SAHIP");
        var h = await s.PostAsJsonAsync($"/api/v1/bankalar/{hesap}/sahte-havale", new { tutar = 100m, gonderenAd = "Ali Veli" });
        Assert.Equal(HttpStatusCode.Conflict, h.StatusCode);
        var c = await (await s.PostAsync($"/api/v1/bankalar/{hesap}/cek", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, c.GetProperty("yeni").GetInt32());
    }

    [Fact]
    public async Task Gecersiz_girdiler_400_roller_403()
    {
        var k = YeniKiraci("ret");
        var hesap = await HesapAc(k);
        var s = f.Ortam.Istemci(k, "SAHIP");
        async Task<string> Ret(object govde)
        {
            var r = await s.PostAsJsonAsync($"/api/v1/bankalar/{hesap}/sahte-havale", govde);
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            return await r.Content.ReadAsStringAsync();
        }
        await Ret(new { tutar = 0m, gonderenAd = "Ali Veli" });
        await Ret(new { tutar = 10m, gonderenAd = "Ali Veli", gonderenIban = "TR000000000000000000000001" });
        Assert.Contains("gonderenKimlikNo", await Ret(new { tutar = 10m, gonderenAd = "Ali Veli", gonderenKimlikNo = "11111111111" }));
        await Ret(new { tutar = 10m, gonderenAd = "12345" });
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PutAsJsonAsync($"/api/v1/bankalar/{hesap}/saglayici", new { kod = "ZIRAAT" })).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync($"/api/v1/bankalar/{hesap}/sahte-havale", new { tutar = 10m, gonderenAd = "Ali Veli" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "MUHASEBE").PutAsJsonAsync($"/api/v1/bankalar/{hesap}/saglayici", new { kod = (string?)null })).StatusCode);
        Assert.True((await f.Ortam.Istemci(k, "MUHASEBE").PostAsJsonAsync($"/api/v1/bankalar/{hesap}/sahte-havale", new { tutar = 10m, gonderenAd = "Ali Veli" })).IsSuccessStatusCode);
    }

    [Fact]
    public async Task Ayni_saglayici_referansi_ikinci_kez_YAZILAMAZ_tekil_indeks()
    {
        var k = YeniKiraci("tekil");
        var hesap = await HesapAc(k);
        using var db = f.Vt.Olustur(k);
        var banka = new Kuyumcu.Api.Modules.Banka.BankaServisi(db);
        await banka.HareketEkle(hesap, new Kuyumcu.Api.Modules.Banka.HareketEkleDto { Yon = "GELEN", Tutar = 50m }, k.Islem, saglayiciReferans: "R-1");
        using var db2 = f.Vt.Olustur(k);
        await Assert.ThrowsAsync<Kuyumcu.Api.Common.ConflictException>(() =>
            new Kuyumcu.Api.Modules.Banka.BankaServisi(db2).HareketEkle(hesap,
                new Kuyumcu.Api.Modules.Banka.HareketEkleDto { Yon = "GELEN", Tutar = 51m, Aciklama = "farkli" }, k.Islem, saglayiciReferans: "R-1"));
    }
}
