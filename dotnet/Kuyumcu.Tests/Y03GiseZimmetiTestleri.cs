using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Contracts.Sayfalar.Gise;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// Y-03 (20.10, rakip Ariyen "Gişe Kasam") — gişe zimmeti: zimmetle → tezgâhtar kabul (açılış nakdi virman) → satış zimmetten
// düşer → takviye (kendi talebini onaylayamaz) → akşam sayım (fark → gerekçe) → sayılan nakit ana kasaya devir.
[Collection("http")]
public sealed class Y03GiseZimmetiTestleri(HttpFikstur f)
{
    private sealed record Ortam(Kiraci K, string TezgahtarId, string MudurId, string GiseKasaId);

    private Ortam Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, ad);
        var now = DateTime.UtcNow;
        var tezgahtarId = Kimlik.Ulid(); var mudurId = Kimlik.Ulid(); var giseId = Kimlik.Ulid();
        db.Kullanicilar.Add(new Kullanici
        {
            Id = tezgahtarId, TenantId = k.TenantId, SubeId = k.SubeId, Ad = "Ayşe", Soyad = "Tezgâh", Email = $"{tezgahtarId}@test.local",
            SifreHash = "x", Rol = "TEZGAHTAR", Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.Kullanicilar.Add(new Kullanici
        {
            Id = mudurId, TenantId = k.TenantId, SubeId = k.SubeId, Ad = "Mehmet", Soyad = "Müdür", Email = $"{mudurId}@test.local",
            SifreHash = "x", Rol = "MUDUR", Aktif = true, Olusturuldu = now, Guncellendi = now,
        });
        db.Kasalar.Add(new Kasa
        {
            Id = giseId, TenantId = k.TenantId, SubeId = k.SubeId, Ad = "Gişe 1", ParaBirimi = "TRY", Bakiye = 0m,
            SorumluKullaniciId = tezgahtarId, Aktif = true, Guncellendi = now,
        });
        db.SaveChanges();
        return new Ortam(k, tezgahtarId, mudurId, giseId);
    }

    private HttpClient Sahip(Ortam o) => f.Ortam.Istemci(o.K, "SAHIP");
    private HttpClient Tezgahtar(Ortam o) => f.Ortam.Istemci(o.TezgahtarId, o.K.TenantId, o.K.SubeId, "TEZGAHTAR");
    private HttpClient Mudur(Ortam o) => f.Ortam.Istemci(o.MudurId, o.K.TenantId, o.K.SubeId, "MUDUR");

    private async Task<decimal> Bakiye(string kasaId)
    {
        using var db = f.Vt.OlusturSistem();
        return await db.Kasalar.Where(k => k.Id == kasaId).Select(k => k.Bakiye).FirstAsync();
    }

    private static async Task<T> Oku<T>(HttpResponseMessage r)
    {
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<T>())!;
    }

    [Fact]
    public async Task Yedi_adim_zimmetle_kabul_satis_takviye_sayim_devir()
    {
        var o = Kur("Y03-akis");
        var sahip = Sahip(o); var tezgahtar = Tezgahtar(o); var mudur = Mudur(o);

        // 1) yönetici zimmetler: 5 adet ürün + 500 TL açılış nakdi (ana kasadan)
        var d = await Oku<GiseZimmetDetay>(await sahip.PostAsJsonAsync("/api/v1/gise", new
        {
            kasaId = o.GiseKasaId, personelId = o.TezgahtarId, kaynakKasaId = o.K.KasaId, acilisNakit = 500m,
            kalemler = new[] { new { urunId = o.K.UrunId, adet = 5m } },
        }));
        Assert.Equal("TEKLIF", d.Ozet.Durum);
        Assert.Equal(TestKurulum.KasaAcilis, await Bakiye(o.K.KasaId));   // para henüz el değiştirmedi

        // 2) tezgâhtar listesinde görür; yönetici kabul edemez (403); tezgâhtar kabul eder → virman
        var liste = await Oku<GiseListesi>(await tezgahtar.GetAsync("/api/v1/gise"));
        Assert.Single(liste.Acik); Assert.False(liste.YonetimMi);
        Assert.Equal(HttpStatusCode.Forbidden, (await sahip.PostAsync($"/api/v1/gise/{d.Ozet.Id}/kabul", null)).StatusCode);
        d = await Oku<GiseZimmetDetay>(await tezgahtar.PostAsync($"/api/v1/gise/{d.Ozet.Id}/kabul", null));
        Assert.Equal("KABUL", d.Ozet.Durum);
        Assert.Equal(TestKurulum.KasaAcilis - 500m, await Bakiye(o.K.KasaId));
        Assert.Equal(500m, await Bakiye(o.GiseKasaId));
        Assert.Equal(500m, d.BeklenenNakit); Assert.False(d.NakitGizli);

        // 4) satış tezgâhtarın defterinden okunur: 2 adet satıldı → beklenen 3
        using (var db = f.Vt.OlusturSistem())
        {
            db.Hareketler.Add(new Hareket
            {
                Id = Kimlik.Ulid(), TenantId = o.K.TenantId, SubeId = o.K.SubeId, Tur = "SATIS", Tarih = DateTime.UtcNow,
                TlTutari = 10_000m, HasGrami = 0m, ParaBirimi = "TRY", UrunId = o.K.UrunId, Adet = 2m,
                KullaniciId = o.TezgahtarId, KurSnapshotId = o.K.KurId, Olusturuldu = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }
        d = await Oku<GiseZimmetDetay>(await tezgahtar.GetAsync($"/api/v1/gise/{d.Ozet.Id}"));
        var u = Assert.Single(d.Urunler);
        Assert.Equal(5m, u.Zimmet); Assert.Equal(2m, u.Satilan); Assert.Equal(3m, u.Beklenen);

        // 3) takviye: tezgâhtar 1 adet ister; tezgâhtar karar veremez (rol); sahip onaylar → zimmet 6, beklenen 4
        d = await Oku<GiseZimmetDetay>(await tezgahtar.PostAsJsonAsync($"/api/v1/gise/{d.Ozet.Id}/takviye", new { urunId = o.K.UrunId, adet = 1m }));
        var talep = Assert.Single(d.Takviyeler); Assert.Equal("TALEP", talep.Durum); Assert.Equal(1, d.Ozet.BekleyenTakviye);
        Assert.Equal(HttpStatusCode.Forbidden, (await tezgahtar.PostAsJsonAsync($"/api/v1/gise/{d.Ozet.Id}/takviye/{talep.Id}/karar", new { onay = true })).StatusCode);
        d = await Oku<GiseZimmetDetay>(await sahip.PostAsJsonAsync($"/api/v1/gise/{d.Ozet.Id}/takviye/{talep.Id}/karar", new { onay = true }));
        Assert.Equal("KABUL", d.Takviyeler[0].Durum);
        Assert.Equal(6m, d.Urunler[0].Zimmet); Assert.Equal(4m, d.Urunler[0].Beklenen);

        // 3b) sahip 100 TL nakit takviye ister; KENDİ talebini onaylayamaz (403); müdür onaylar → gişe 600
        d = await Oku<GiseZimmetDetay>(await sahip.PostAsJsonAsync($"/api/v1/gise/{d.Ozet.Id}/takviye", new { nakitTutar = 100m }));
        var nakitTalep = d.Takviyeler.First(t => t.NakitTutar == 100m);
        var kendi = await sahip.PostAsJsonAsync($"/api/v1/gise/{d.Ozet.Id}/takviye/{nakitTalep.Id}/karar", new { onay = true });
        Assert.Equal(HttpStatusCode.Forbidden, kendi.StatusCode);
        Assert.Contains("Kendi talebinizi", await kendi.Content.ReadAsStringAsync());
        d = await Oku<GiseZimmetDetay>(await mudur.PostAsJsonAsync($"/api/v1/gise/{d.Ozet.Id}/takviye/{nakitTalep.Id}/karar", new { onay = true }));
        Assert.Equal(600m, await Bakiye(o.GiseKasaId));
        Assert.Equal(600m, d.BeklenenNakit);

        // 5) fark var, gerekçe yok → 400 (alan aciklama); kapanmadı
        var farkli = await tezgahtar.PostAsJsonAsync($"/api/v1/gise/{d.Ozet.Id}/kapat", new
        {
            sayilanNakit = 590m, satirlar = new[] { new { urunId = o.K.UrunId, sayilanAdet = 4m } },
        });
        Assert.Equal(HttpStatusCode.BadRequest, farkli.StatusCode);
        Assert.Contains("aciklama", await farkli.Content.ReadAsStringAsync());
        Assert.Equal(600m, await Bakiye(o.GiseKasaId));

        // 6) sayım + devir: 590 sayıldı (−10), ürün 4 (fark 0) → fark gişeye yazılır, 590 ana kasaya döner, gişe 0
        var s = await Oku<GiseKapatSonucu>(await tezgahtar.PostAsJsonAsync($"/api/v1/gise/{d.Ozet.Id}/kapat", new
        {
            sayilanNakit = 590m, aciklama = "Akşam sayımında on lira eksik çıktı; bozuk para kutusu kontrol edilecek",
            satirlar = new[] { new { urunId = o.K.UrunId, sayilanAdet = 4m } },
        }));
        Assert.Equal("KAPANDI", s.Detay.Ozet.Durum);
        Assert.Equal(-10m, s.NakitFark); Assert.Equal(590m, s.DevredilenNakit); Assert.Equal(0, s.FarkliUrun);
        Assert.Equal(0m, await Bakiye(o.GiseKasaId));
        Assert.Equal(TestKurulum.KasaAcilis - 500m - 100m + 590m, await Bakiye(o.K.KasaId));
        Assert.Equal(4m, s.Detay.Urunler[0].Sayilan); Assert.Equal(0m, s.Detay.Urunler[0].Fark);
        Assert.Equal(600m, s.Detay.BeklenenNakit); Assert.Equal(590m, s.Detay.SayilanNakit);

        // 7) vardiya devri: liste geçmişe düştü, kasa yeni zimmete açık
        liste = await Oku<GiseListesi>(await sahip.GetAsync("/api/v1/gise"));
        Assert.Empty(liste.Acik); Assert.Single(liste.Gecmis); Assert.True(liste.YonetimMi);
    }

    [Fact]
    public async Task Kurallar_kendine_zimmet_stok_asimi_cift_acik_zimmet_reddedilir()
    {
        var o = Kur("Y03-kural");
        var sahip = Sahip(o);
        var kendine = await sahip.PostAsJsonAsync("/api/v1/gise", new { kasaId = o.GiseKasaId, personelId = o.K.KullaniciId, kalemler = new[] { new { urunId = o.K.UrunId, adet = 1m } } });
        Assert.Equal(HttpStatusCode.BadRequest, kendine.StatusCode);
        var stok = await sahip.PostAsJsonAsync("/api/v1/gise", new { kasaId = o.GiseKasaId, personelId = o.TezgahtarId, kalemler = new[] { new { urunId = o.K.UrunId, adet = 1000m } } });
        Assert.Equal(HttpStatusCode.BadRequest, stok.StatusCode);
        Assert.Contains("yetersiz", await stok.Content.ReadAsStringAsync());
        var bos = await sahip.PostAsJsonAsync("/api/v1/gise", new { kasaId = o.GiseKasaId, personelId = o.TezgahtarId, kalemler = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.BadRequest, bos.StatusCode);
        var ilk = await Oku<GiseZimmetDetay>(await sahip.PostAsJsonAsync("/api/v1/gise", new { kasaId = o.GiseKasaId, personelId = o.TezgahtarId, kalemler = new[] { new { urunId = o.K.UrunId, adet = 1m } } }));
        var ikinci = await sahip.PostAsJsonAsync("/api/v1/gise", new { kasaId = o.GiseKasaId, personelId = o.TezgahtarId, kalemler = new[] { new { urunId = o.K.UrunId, adet = 1m } } });
        Assert.Equal(HttpStatusCode.Conflict, ikinci.StatusCode);
        // teklif iptal edilir; kabul edilmiş zimmet iptal edilemez (sayımla kapanır)
        var iptal = await Oku<GiseZimmetDetay>(await sahip.PostAsync($"/api/v1/gise/{ilk.Ozet.Id}/iptal", null));
        Assert.Equal("IPTAL", iptal.Ozet.Durum);
        Assert.Equal(HttpStatusCode.Conflict, (await sahip.PostAsync($"/api/v1/gise/{ilk.Ozet.Id}/iptal", null)).StatusCode);
    }

    [Fact]
    public async Task Kor_sayim_acikken_tezgahtar_beklenen_nakdi_gormez_yonetici_gorur()
    {
        var o = Kur("Y03-kor");
        var sahip = Sahip(o); var tezgahtar = Tezgahtar(o);
        Assert.True((await sahip.PutAsJsonAsync("/api/v1/kasalar/kor-sayim", new { etkin = true })).IsSuccessStatusCode);
        var d = await Oku<GiseZimmetDetay>(await sahip.PostAsJsonAsync("/api/v1/gise", new { kasaId = o.GiseKasaId, personelId = o.TezgahtarId, kaynakKasaId = o.K.KasaId, acilisNakit = 300m }));
        d = await Oku<GiseZimmetDetay>(await tezgahtar.PostAsync($"/api/v1/gise/{d.Ozet.Id}/kabul", null));
        Assert.True(d.NakitGizli); Assert.Equal(0m, d.BeklenenNakit);
        var y = await Oku<GiseZimmetDetay>(await sahip.GetAsync($"/api/v1/gise/{d.Ozet.Id}"));
        Assert.False(y.NakitGizli); Assert.Equal(300m, y.BeklenenNakit);
    }
}
