using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Contracts.Sayfalar.Raporlar;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-20 — prim SATAN (personel_id) üzerinden: iade asıl satışın satanından düşer; hedef güne orantılı; prim = net × %.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class D1PrimTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Prim_satana_gore_iade_asil_satandan_duser_hedef_orantili_kaydeden_sayilmaz()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1PRIM");
        var s = f.Ortam.Istemci(k, "SAHIP");
        Assert.True((await s.PostAsJsonAsync("/api/v1/islemler/musteri-kimlik", new { cariId = k.CariId, kimlikNo = "10000000146" })).IsSuccessStatusCode);
        var yeni = await s.PostAsJsonAsync("/api/v1/kullanicilar", new
        {
            ad = "Emre", soyad = "Satıcı", email = "d1prim-emre@ornek.com.tr", sifre = "Guclu.Sifre.2026!", rol = "TEZGAHTAR", subeId = k.SubeId,
            primYuzde = 2m, aylikHedefTl = 31000m,
        });
        Assert.True(yeni.IsSuccessStatusCode, await yeni.Content.ReadAsStringAsync());
        string emre;
        using (var db = f.Vt.OlusturSistem())
        {
            var u = db.Kullanicilar.AsNoTracking().Single(x => x.Email == "d1prim-emre@ornek.com.tr");
            Assert.Equal(2m, u.PrimYuzde);
            emre = u.Id;
        }
        Assert.True((await s.PostAsJsonAsync("/api/v1/urunler", new { tip = "ZIYNET", kod = "D1PRIM-2G", ad = "2 g Yüzük", ayarId = "K22", gramAgirlik = 2m, acilisStogu = new { adet = 5m } })).IsSuccessStatusCode);
        string urun; decimal birim;
        using (var db = f.Vt.OlusturSistem())
        {
            var ur = db.Urunler.AsNoTracking().Single(u => u.TenantId == k.TenantId && u.Kod == "D1PRIM-2G");
            urun = ur.Id;
            birim = Math.Ceiling(ur.HasGrami * db.KurSnapshots.AsNoTracking().Single(x => x.Id == k.KurId).AltinHasFiyati * 1.10m);
        }
        async Task Sat(string? personelId)
        {
            var r = await s.PostAsJsonAsync("/api/v1/islemler/satis-fisi", new
            {
                cariId = k.CariId, personelId,
                kalemler = new[] { new { urunId = urun, adet = 1m, birimFiyatTl = birim, hediye = false } },
                odemeler = new[] { new { arac = "KART", tutarTl = birim, kasaId = k.KasaId } },
                kurSnapshotId = k.KurId, fisIskontoYuzde = 0m,
            });
            Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        }
        await Sat(emre); await Sat(emre); await Sat(null);   // ikisini Emre sattı (kaydeden SAHİP), biri SAHİP'in
        string emreninSatisi;
        using (var db = f.Vt.OlusturSistem())
            emreninSatisi = db.Hareketler.AsNoTracking().First(h => h.TenantId == k.TenantId && h.Tur == "SATIS" && h.PersonelId == emre).Id;
        var iade = await s.PostAsJsonAsync("/api/v1/islemler/satis-iade", new { kaynakHareketId = emreninSatisi, adet = 1m, odemeTuru = "KART", kasaId = k.KasaId, kurSnapshotId = k.KurId, aciklama = "müşteri vazgeçti" });
        Assert.True(iade.IsSuccessStatusCode, await iade.Content.ReadAsStringAsync());

        var bugun = Kuyumcu.Api.Common.Zaman.YerelBugun();
        var p = await s.GetFromJsonAsync<PrimRaporYaniti>($"/api/v1/raporlar/prim?baslangic={bugun:yyyy-MM-dd}&bitis={bugun:yyyy-MM-dd}");
        var e = Assert.Single(p!.Satirlar, x => x.PersonelId == emre);
        Assert.Equal(2, e.SatisAdet);
        Assert.Equal(birim * 2, e.SatisTl);
        Assert.Equal(birim, e.IadeTl);                                             // iade (SAHİP kaydetti) Emre'den düştü
        Assert.Equal(birim, e.NetTl);
        Assert.Equal(Math.Round(birim * 0.02m, 2), e.PrimTl);
        Assert.Equal(Math.Round(31000m / DateTime.DaysInMonth(bugun.Year, bugun.Month), 2), e.HedefTl);
        var sahip = Assert.Single(p.Satirlar, x => x.PersonelId == k.KullaniciId);
        Assert.Equal(birim, sahip.NetTl);
        Assert.Equal(0m, sahip.PrimTl);                                            // prim yüzdesi yok
        Assert.Equal(e.PrimTl, p.ToplamPrimTl);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync($"/api/v1/raporlar/prim")).StatusCode);
    }
}
