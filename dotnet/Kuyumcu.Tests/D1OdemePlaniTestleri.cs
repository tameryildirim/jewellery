using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Isler;
using Kuyumcu.Api.Modules.Isler.Isler;
using Kuyumcu.Contracts.Cikti;
using Kuyumcu.Contracts.Sayfalar.Cariler;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-06 — kuyumcunun kendi taksitli/senetli satışı: veresiye borcu taksitlere bölünür, tahsilat sırayla düşer,
// senetli planda her taksit senet olur; gecikmiş taksit bildirilir, planlı borç vade hatırlatmasına girmez.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class D1OdemePlaniTestleri(HttpFikstur f)
{
    private async Task<(Kiraci K, HttpClient S, string HareketId, decimal Birim)> VeresiyeSatis(string ad)
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, ad);
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
        string hid;
        using (var db = f.Vt.OlusturSistem()) hid = db.Hareketler.AsNoTracking().Single(h => h.TenantId == k.TenantId && h.Tur == "SATIS").Id;
        return (k, s, hid, birim);
    }

    private static DateOnly Bugun => Kuyumcu.Api.Common.Zaman.YerelBugun();

    [Fact]
    public async Task Senetsiz_plan_esit_bolunur_tahsilat_sirayla_duser_iptal_edilir_cikti_basilir()
    {
        var (k, s, hid, birim) = await VeresiyeSatis("D1PLAN1");
        var adaylar = await s.GetFromJsonAsync<List<OdemePlaniAdayi>>($"/api/v1/odeme-planlari/adaylar?cariId={k.CariId}");
        var aday = Assert.Single(adaylar!, a => a.HareketId == hid);
        Assert.Equal(birim, aday.AcikTutar);

        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync("/api/v1/odeme-planlari", new { cariId = k.CariId, kaynakHareketId = hid, taksitSayisi = 3, ilkVade = Bugun.AddDays(-1) })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync("/api/v1/odeme-planlari", new { cariId = k.CariId, kaynakHareketId = hid, taksitSayisi = 3, ilkVade = Bugun.AddDays(30) })).StatusCode);
        var r = await s.PostAsJsonAsync("/api/v1/odeme-planlari", new { cariId = k.CariId, kaynakHareketId = hid, taksitSayisi = 3, ilkVade = Bugun.AddDays(30), aralikAy = 1 });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        var plan = (await r.Content.ReadFromJsonAsync<OdemePlaniOzet>())!;
        Assert.Equal(3, plan.Taksitler.Count);
        Assert.Equal(birim, plan.Taksitler.Sum(t => t.TutarTl));
        Assert.Equal(Bugun.AddDays(30).AddMonths(2), plan.Taksitler[2].Vade);
        Assert.All(plan.Taksitler, t => Assert.Equal("BEKLIYOR", t.Durum));
        Assert.DoesNotContain(await s.GetFromJsonAsync<List<OdemePlaniAdayi>>($"/api/v1/odeme-planlari/adaylar?cariId={k.CariId}") ?? [], a => a.HareketId == hid);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.PostAsJsonAsync("/api/v1/odeme-planlari", new { cariId = k.CariId, kaynakHareketId = hid, taksitSayisi = 2, ilkVade = Bugun })).StatusCode);

        // İlk taksit + 10 TL tahsil edilir → 1. taksit ödendi, 2. kısmi.
        var odeme = plan.Taksitler[0].TutarTl + 10m;
        var th = await s.PostAsJsonAsync($"/api/v1/cari/{k.CariId}/tahsilat", new { tlTutari = odeme, odemeTuru = "KART", kasaId = k.KasaId, kurSnapshotId = k.KurId });
        Assert.True(th.IsSuccessStatusCode, await th.Content.ReadAsStringAsync());
        var p2 = (await s.GetFromJsonAsync<OdemePlaniOzet>($"/api/v1/odeme-planlari/{plan.Id}"))!;
        Assert.Equal(odeme, p2.OdenenTl);
        Assert.Equal(["ODENDI", "KISMI", "BEKLIYOR"], p2.Taksitler.Select(t => t.Durum));
        Assert.Equal(10m, p2.Taksitler[1].OdenenTl);

        var belge = await s.GetFromJsonAsync<OrtakBelge>($"/api/v1/cikti/{BelgeTurleri.OdemePlani}/{plan.Id}");
        Assert.Equal("ÖDEME PLANI", belge!.BelgeAdi);
        Assert.Equal(3, belge.Bolumler[0].Satirlar.Count);
        Assert.Contains("vade farkı uygulanmamıştır", belge.Not);

        var iptal = (await (await s.PostAsync($"/api/v1/odeme-planlari/{plan.Id}/iptal", null)).Content.ReadFromJsonAsync<OdemePlaniOzet>())!;
        Assert.Equal("IPTAL", iptal.Durum);
        Assert.Contains(await s.GetFromJsonAsync<List<OdemePlaniAdayi>>($"/api/v1/odeme-planlari/adaylar?cariId={k.CariId}") ?? [], a => a.HareketId == hid);
    }

    [Fact]
    public async Task Senetli_planda_her_taksit_senet_olur_borc_senede_gecer_iptal_edilmez()
    {
        var (k, s, hid, birim) = await VeresiyeSatis("D1PLAN2");
        var r = await s.PostAsJsonAsync("/api/v1/odeme-planlari", new { cariId = k.CariId, kaynakHareketId = hid, taksitSayisi = 2, ilkVade = Bugun.AddDays(10), aralikAy = 2, senetli = true });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        var plan = (await r.Content.ReadFromJsonAsync<OdemePlaniOzet>())!;
        Assert.All(plan.Taksitler, t => Assert.NotNull(t.EvrakId));
        using (var db = f.Vt.OlusturSistem())
        {
            var senetler = db.KiymetliEvraklar.AsNoTracking().Where(e => e.TenantId == k.TenantId).OrderBy(e => e.VadeGunu).ToList();
            Assert.Equal(2, senetler.Count);
            Assert.All(senetler, e => Assert.Equal("SENET", e.EvrakTuru));
            Assert.Equal(birim, senetler.Sum(e => e.Tutar));
            Assert.Equal(Bugun.AddDays(10).AddMonths(2), senetler[1].VadeGunu);
            Assert.Equal(0m, db.CariBakiyeler.AsNoTracking().Single(b => b.CariId == k.CariId).TlBakiye);   // borç senede geçti
        }
        Assert.Equal(HttpStatusCode.Conflict, (await s.PostAsync($"/api/v1/odeme-planlari/{plan.Id}/iptal", null)).StatusCode);
    }

    [Fact]
    public async Task Geciken_taksit_bildirilir_planli_borc_vade_hatirlatmasina_girmez()
    {
        var (k, s, hid, _) = await VeresiyeSatis("D1PLAN3");
        var plan = (await (await s.PostAsJsonAsync("/api/v1/odeme-planlari", new { cariId = k.CariId, kaynakHareketId = hid, taksitSayisi = 2, ilkVade = Bugun })).Content.ReadFromJsonAsync<OdemePlaniOzet>())!;
        using (var db = f.Vt.OlusturSistem())
            await db.OdemePlaniTaksitleri.Where(t => t.PlanId == plan.Id && t.Sira == 1).ExecuteUpdateAsync(x => x.SetProperty(y => y.Vade, Bugun.AddDays(-5)));
        using (var db = f.Vt.Olustur(k.TenantId))
        {
            var kapsam = IsCatisiKurulum.Kapsam(db, IsCatisiKurulum.Yapilandirma());
            var t = await new TaksitHatirlatmaIsi().Calistir(new IsBaglami(kapsam, db, k.TenantId, DateTime.UtcNow), default);
            var b = Assert.Single(t.Taslaklar);
            Assert.Contains("1 taksit gecikti", b.Govde);
            var v = await new VadeHatirlatmaIsi().Calistir(new IsBaglami(kapsam, db, k.TenantId, DateTime.UtcNow), default);
            Assert.Empty(v.Taslaklar);   // borcun vadesi bugün geçti sayılsa da planlı → taksit takvimi izler
        }
        Assert.Equal("GECIKTI", (await s.GetFromJsonAsync<OdemePlaniOzet>($"/api/v1/odeme-planlari/{plan.Id}"))!.Taksitler[0].Durum);
    }
}
