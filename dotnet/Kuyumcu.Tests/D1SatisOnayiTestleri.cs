using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-05 — eşikli satış onayı · kişisel iskonto sınırı · personel uyarıları.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class D1SatisOnayiTestleri(HttpFikstur f)
{
    private static JsonElement Kok(string j) => JsonDocument.Parse(j).RootElement;

    private async Task<(Kiraci K, decimal Liste)> Kur(string ad)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, ad);
        var s = f.Ortam.Istemci(k, "SAHIP");
        Assert.True((await s.PostAsJsonAsync("/api/v1/fiyat/listeler", new { ad = "Perakende", tur = "PERAKENDE", hasMarjYuzde = 20m, iscilikCarpanYuzde = 100m, varsayilan = true })).IsSuccessStatusCode);
        var t = Kok(await s.GetStringAsync($"/api/v1/fiyat/teklif?urunId={k.UrunId}&kurSnapshotId={k.KurId}"));
        return (k, t.GetProperty("satisFiyati").GetDecimal());
    }

    private static object Fis(Kiraci k, decimal birim, string? onayTalebiId = null, string? urunId = null) => new
    {
        cariId = k.CariId,
        kalemler = new[] { new { urunId = urunId ?? k.UrunId, adet = 1m, birimFiyatTl = birim, hediye = false } },
        odemeler = new[] { new { arac = "KART", tutarTl = birim, kasaId = k.KasaId } },
        kurSnapshotId = k.KurId, fisIskontoYuzde = 0m, onayTalebiId, belgeTuru = "FATURA",
    };

    private static object Talep(Kiraci k, decimal birim) => new
    {
        cariId = k.CariId, kurSnapshotId = k.KurId, fisIskontoYuzde = 0m,
        kalemler = new[] { new { urunId = k.UrunId, adet = 1m, birimFiyatTl = birim, hediye = false } },
    };

    private async Task Kimlik(Kiraci k) =>
        Assert.True((await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/islemler/musteri-kimlik", new { cariId = k.CariId, kimlikNo = "10000000146" })).IsSuccessStatusCode);

    [Fact]
    public async Task Esik_asan_indirim_onay_ister_talep_onaylaninca_bir_kez_ve_yalniz_ayni_satista_gecer()
    {
        var (k, liste) = await Kur("D1ONAY1");
        await Kimlik(k);
        var s = f.Ortam.Istemci(k, "SAHIP");
        var t = f.Ortam.Istemci(k, "TEZGAHTAR");
        Assert.True((await s.PutAsJsonAsync("/api/v1/satis-onayi/ayarlar", new { iskontoYuzde = 5m, zararina = false })).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await t.PutAsJsonAsync("/api/v1/satis-onayi/ayarlar", new { iskontoYuzde = 50m })).StatusCode);

        var birim = Math.Round(liste * 0.90m, 2);   // %10 indirim > %5 eşik
        var r1 = await t.PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis(k, birim));
        Assert.Equal(HttpStatusCode.Conflict, r1.StatusCode);
        Assert.Equal("ONAY_GEREKLI", Kok(await r1.Content.ReadAsStringAsync()).GetProperty("kod").GetString());

        var ac = await t.PostAsJsonAsync("/api/v1/satis-onayi/talepler", Talep(k, birim));
        Assert.True(ac.IsSuccessStatusCode, await ac.Content.ReadAsStringAsync());
        var talep = (await ac.Content.ReadFromJsonAsync<OnayTalebiOzet>())!;
        Assert.Equal("BEKLIYOR", talep.Durum);
        Assert.Contains("ISKONTO", talep.Sebepler);
        Assert.Equal(birim, talep.ToplamTl);
        Assert.InRange(talep.IndirimYuzde, 9.9m, 10.1m);
        // Aynı içerikle ikinci istek yeni talep açmaz.
        Assert.Equal(talep.Id, (await (await t.PostAsJsonAsync("/api/v1/satis-onayi/talepler", Talep(k, birim))).Content.ReadFromJsonAsync<OnayTalebiOzet>())!.Id);
        using (var db = f.Vt.OlusturSistem())
            Assert.Single(db.Bildirimler.AsNoTracking().Where(b => b.TenantId == k.TenantId && b.Tur == "SATIS_ONAYI"));
        Assert.Equal(HttpStatusCode.Forbidden, (await t.GetAsync("/api/v1/satis-onayi/talepler")).StatusCode);
        Assert.Contains(await s.GetFromJsonAsync<List<OnayTalebiOzet>>("/api/v1/satis-onayi/talepler") ?? [], x => x.Id == talep.Id);

        // Karar verilmeden talep geçmez; tezgâhtar kendi talebini onaylayamaz.
        Assert.Equal(HttpStatusCode.Conflict, (await t.PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis(k, birim, talep.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await t.PostAsJsonAsync($"/api/v1/satis-onayi/talepler/{talep.Id}/onayla", new { })).StatusCode);

        Assert.True((await s.PostAsJsonAsync($"/api/v1/satis-onayi/talepler/{talep.Id}/onayla", new { not = "olur" })).IsSuccessStatusCode);
        Assert.Equal("ONAYLANDI", (await t.GetFromJsonAsync<OnayTalebiOzet>($"/api/v1/satis-onayi/talepler/{talep.Id}"))!.Durum);
        // Başka içerik (daha büyük indirim) aynı onayla geçmez.
        Assert.Equal(HttpStatusCode.Conflict, (await t.PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis(k, Math.Round(liste * 0.80m, 2), talep.Id))).StatusCode);
        var gec = await t.PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis(k, birim, talep.Id));
        Assert.True(gec.IsSuccessStatusCode, await gec.Content.ReadAsStringAsync());
        Assert.Equal("KULLANILDI", (await t.GetFromJsonAsync<OnayTalebiOzet>($"/api/v1/satis-onayi/talepler/{talep.Id}"))!.Durum);
        // Aynı onay ikinci kez kullanılamaz.
        Assert.Equal(HttpStatusCode.Conflict, (await t.PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis(k, birim, talep.Id))).StatusCode);
        // Eşiğin altındaki satış onaysız geçer; sahip onay vericidir (sorulmaz).
        // (MASAK kümülatifine takılmamak için satış yerine talep ucuyla ölçülür: onay gerekmiyorsa 400.)
        Assert.Equal(HttpStatusCode.BadRequest, (await t.PostAsJsonAsync("/api/v1/satis-onayi/talepler", Talep(k, Math.Round(liste * 0.98m, 2)))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync("/api/v1/satis-onayi/talepler", Talep(k, birim))).StatusCode);
    }

    [Fact]
    public async Task Kisisel_iskonto_siniri_tutar_ve_zararina_esigi_reddedilen_talep_gecmez()
    {
        var (k, liste) = await Kur("D1ONAY2");
        await Kimlik(k);
        var s = f.Ortam.Istemci(k, "SAHIP");
        var t = f.Ortam.Istemci(k, "TEZGAHTAR");
        using (var db = f.Vt.OlusturSistem())
            await db.Kullanicilar.Where(u => u.Id == k.KullaniciId).ExecuteUpdateAsync(x => x.SetProperty(y => y.IskontoSiniriYuzde, 2m));
        var birim = Math.Round(liste * 0.95m, 2);
        var r = await t.PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis(k, birim));
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        var talep = (await (await t.PostAsJsonAsync("/api/v1/satis-onayi/talepler", Talep(k, birim))).Content.ReadFromJsonAsync<OnayTalebiOzet>())!;
        Assert.Equal(["KISISEL"], talep.Sebepler);
        Assert.True((await s.PostAsJsonAsync($"/api/v1/satis-onayi/talepler/{talep.Id}/reddet", new { not = "çok indirim" })).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await t.PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis(k, birim, talep.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await s.PostAsJsonAsync($"/api/v1/satis-onayi/talepler/{talep.Id}/onayla", new { })).StatusCode);   // karar verilmiş

        // Kişisel sınır kalkınca; işletme tutar eşiği ve zararına kuralı.
        using (var db = f.Vt.OlusturSistem())
            await db.Kullanicilar.Where(u => u.Id == k.KullaniciId).ExecuteUpdateAsync(x => x.SetProperty(y => y.IskontoSiniriYuzde, (decimal?)null));
        Assert.True((await s.PutAsJsonAsync("/api/v1/satis-onayi/ayarlar", new { tutarTl = 1000m, zararina = true })).IsSuccessStatusCode);
        var t2 = (await (await t.PostAsJsonAsync("/api/v1/satis-onayi/talepler", Talep(k, liste))).Content.ReadFromJsonAsync<OnayTalebiOzet>())!;
        Assert.Equal(["TUTAR"], t2.Sebepler);
        var zarar = Math.Round(t2.HasDegeriTl * 0.5m, 2);
        var t3 = (await (await t.PostAsJsonAsync("/api/v1/satis-onayi/talepler", Talep(k, zarar))).Content.ReadFromJsonAsync<OnayTalebiOzet>())!;
        Assert.Contains("ZARARINA", t3.Sebepler);
        // Onay gerektirmeyen satış için talep açılmaz.
        Assert.True((await s.PutAsJsonAsync("/api/v1/satis-onayi/ayarlar", new { zararina = false })).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await t.PostAsJsonAsync("/api/v1/satis-onayi/talepler", Talep(k, liste))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PutAsJsonAsync("/api/v1/satis-onayi/ayarlar", new { mesaiBaslangic = "09:00" })).StatusCode);
    }

    [Fact]
    public async Task Mesai_disi_satis_ve_saatte_uc_iade_patrona_bildirilir()
    {
        var (k, _) = await Kur("D1ONAY3");
        await Kimlik(k);
        var s = f.Ortam.Istemci(k, "SAHIP");
        // Ucuz ürün: üç satış MASAK günlük kümülatif eşiğinin altında kalsın.
        Assert.True((await s.PostAsJsonAsync("/api/v1/urunler", new { tip = "ZIYNET", kod = "D1ONAY-2G", ad = "2 g Yüzük", ayarId = "K22", gramAgirlik = 2m, acilisStogu = new { adet = 5m } })).IsSuccessStatusCode);
        string ucuz;
        using (var db = f.Vt.OlusturSistem()) ucuz = db.Urunler.AsNoTracking().Single(u => u.TenantId == k.TenantId && u.Kod == "D1ONAY-2G").Id;
        var liste = Kok(await s.GetStringAsync($"/api/v1/fiyat/teklif?urunId={ucuz}&kurSnapshotId={k.KurId}")).GetProperty("satisFiyati").GetDecimal();
        var t = f.Ortam.Istemci(k, "TEZGAHTAR");
        var simdi = Kuyumcu.Api.Common.Zaman.UtcdenYerele(DateTime.UtcNow);
        var bas = simdi.AddHours(2).ToString("HH:mm"); var bit = simdi.AddHours(3).ToString("HH:mm");
        Assert.True((await s.PutAsJsonAsync("/api/v1/satis-onayi/ayarlar", new { mesaiBaslangic = bas, mesaiBitis = bit })).IsSuccessStatusCode);
        var satislar = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            var r = await t.PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis(k, liste, null, ucuz));
            Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        }
        using (var db = f.Vt.OlusturSistem())
        {
            Assert.Single(db.Bildirimler.AsNoTracking().Where(b => b.TenantId == k.TenantId && b.Tur == "PERSONEL_MESAI"));   // günde bir kez
            satislar = await db.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId && h.Tur == "SATIS").Select(h => h.Id).ToListAsync();
        }
        foreach (var id in satislar)
        {
            var r = await t.PostAsJsonAsync("/api/v1/islemler/satis-iade", new { kaynakHareketId = id, adet = 1m, odemeTuru = "VERESIYE", kurSnapshotId = k.KurId, aciklama = "müşteri vazgeçti" });
            Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        }
        using (var db = f.Vt.OlusturSistem())
        {
            var b = Assert.Single(db.Bildirimler.AsNoTracking().Where(b => b.TenantId == k.TenantId && b.Tur == "PERSONEL_IADE"));
            Assert.Contains("3 iade", b.Govde);
            Assert.Equal("SAHIP,MUDUR", b.GorunurRoller);
        }
    }
}
