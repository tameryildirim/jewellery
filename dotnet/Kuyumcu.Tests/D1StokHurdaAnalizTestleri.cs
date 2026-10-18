using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Isler;
using Kuyumcu.Api.Modules.Isler.Isler;
using Kuyumcu.Contracts.Sayfalar.Hurda;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-15 (18.10, rakip eşitleme) — eşik altı stok bildirimi (STOK_ESIK işi) ve hurda analizi (/hurda/analiz).
// ═════════════════════════════════════════════════════════════════════════════
[Collection("veritabani")]
public sealed class D1StokEsikIsiTestleri(TestVeritabani vt)
{
    [Fact]
    public async Task Asgari_stogun_altindaki_urun_icin_gunde_bir_bildirim_uretilir_ustundeki_icin_uretilmez()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "D1-stok-esik", stokAdet: 3m);
        using (var db = vt.OlusturSistem())
        {
            var u = db.Urunler.Single(x => x.Id == k.UrunId);
            u.MinStok = 5m;   // stok 3 < asgari 5
            db.SaveChanges();
        }
        var i = new StokEsikIsi();
        using (var db = vt.Olustur(k.TenantId))
        {
            var c = IsCatisiKurulum.Calistirici(db, IsCatisiKurulum.Yapilandirma(), i);
            await c.Calistir(i, k.TenantId, DateTime.UtcNow, null, default);
            await c.Calistir(i, k.TenantId, DateTime.UtcNow, k.KullaniciId, default);   // elle ikinci koşum: yeni bildirim YOK
        }
        List<Bildirim> l;
        using (var kontrol = vt.OlusturSistem())
            l = await kontrol.Bildirimler.AsNoTracking().Where(b => b.TenantId == k.TenantId && b.Tur == StokEsikIsi.IsKodu).ToListAsync();
        var b = Assert.Single(l);
        Assert.Equal(k.UrunId, b.IlgiliVarlikId);
        Assert.Contains("asgari 5", b.Govde);

        using (var db = vt.OlusturSistem())
        {
            db.Urunler.Single(x => x.Id == k.UrunId).MinStok = 2m;   // stok 3 ≥ asgari 2
            db.SaveChanges();
        }
        using (var db = vt.Olustur(k.TenantId))
        {
            var sonuc = await new StokEsikIsi().Calistir(new IsBaglami(IsCatisiKurulum.Kapsam(db, IsCatisiKurulum.Yapilandirma()), db, k.TenantId, DateTime.UtcNow), default);
            Assert.Empty(sonuc.Taslaklar);
        }
    }
}

[Collection("http")]
public sealed class D1HurdaAnalizTestleri(HttpFikstur f)
{
    private static readonly object Satici = new { saticiTipi = "NIHAI_TUKETICI", adSoyad = "Ali Veli", tckn = "10000000146", adres = "Ulus" };

    [Fact]
    public async Task Hurda_alislari_has_TL_ve_ortalama_has_fiyatiyla_ozetlenir_tezgahtar_403()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1-hurda-analiz");
        var s = f.Ortam.Istemci(k, "SAHIP");
        foreach (var fiyat in new[] { 6_000m, 6_500m })
        {
            var h = await s.PostJson("/api/v1/islemler/hurda-alis", new Dictionary<string, object?>
            {
                ["cariId"] = k.CariId, ["ayarId"] = "K22", ["gramAgirlik"] = 1m, ["birimFiyatTlHasGram"] = fiyat,
                ["kasaId"] = k.KasaId, ["kurSnapshotId"] = k.KurId, ["odemeTuru"] = "NAKIT", ["satici"] = Satici,
            });
            Assert.True(h.IsSuccessStatusCode, await h.Content.ReadAsStringAsync());
        }
        decimal has, tl;
        using (var db = f.Vt.OlusturSistem())
        {
            var l = db.Hareketler.AsNoTracking().Where(x => x.TenantId == k.TenantId && x.Tur == "HURDA_ALIS").ToList();
            has = l.Sum(x => Math.Abs(x.HasGrami)); tl = l.Sum(x => Math.Abs(x.TlTutari));
        }
        var a = await s.GetFromJsonAsync<HurdaAnalizi>("/api/v1/hurda/analiz?gun=30");
        var m = Assert.Single(a!.Madenler);
        Assert.Equal(2, m.AlisAdet);
        Assert.Equal(has, m.AlisHas);
        Assert.Equal(tl, m.AlisTl);
        Assert.Equal(Math.Round(tl / has, 2, MidpointRounding.AwayFromZero), m.AlisOrtHasFiyati);
        Assert.Equal(0, m.SatisAdet);
        Assert.Null(m.SatisOrtHasFiyati);   // satış yok → ortalama uydurulmaz
        Assert.Null(m.HasBasinaFark);

        Assert.Equal(HttpStatusCode.Forbidden, (await f.Ortam.Istemci(k, "TEZGAHTAR").GetAsync("/api/v1/hurda/analiz")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.GetAsync("/api/v1/hurda/analiz?gun=0")).StatusCode);
    }
}
