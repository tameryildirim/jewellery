using System.Net;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Kuyumcu.Web.Bilesenler.Form;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ═══════════════════════════════════════════════════════════
// HASIM DOĞRULAYICI (Aşkın + Mehmet) — PAKET EKSIK-B1-SATINALMA finansal sınır testleri.
// Kapsam: POST /satinalma (birim fiyat, adet) + POST /satinalma/{id}/mal-kabul (stok miktarı,
// ALIŞ hareketi TL tutarı). Beklenen değerler SABİT ve elle hesaplandı (üretim kodundan türetilmez).
// Yuvarlama: adet kolonu decimal(12,3) — MalKabul Math.Round(.., 3, AwayFromZero);
// TL decimal(15,2) — proje kuralı MidpointRounding.AwayFromZero.
// ═══════════════════════════════════════════════════════════

[Collection("http")]
public sealed class EKSIKB1SATINALMASinirTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad, string? birim = null)
    {
        using var db = f.Vt.OlusturSistem();
        var k = TestKurulum.KiraciKur(db, "PAKETEKSIK-B1-SATINALMA-TEST dg " + ad);
        db.SaveChanges();
        if (birim is not null)
        {
            var u = db.Urunler.First(x => x.Id == k.UrunId && x.TenantId == k.TenantId);
            u.Birim = birim;
            db.SaveChanges();
        }
        return k;
    }

    private static async Task<JsonElement> Tamam(HttpResponseMessage r)
    {
        var g = await r.Metin();
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"{(int)r.StatusCode}: {g}");
        return JsonDocument.Parse(g).RootElement;
    }

    private static async Task<(string Id, string Kalem)> OnayliSiparis(HttpClient c, Kiraci k, decimal adet, decimal fiyat)
    {
        var y = await Tamam(await c.PostJson("/api/v1/satinalma", new
        {
            tedarikciCariId = k.CariId,
            kalemler = new[] { new { urunId = k.UrunId, adet, birimFiyatTl = fiyat } },
        }));
        var id = y.GetProperty("id").GetString()!;
        var d = await Tamam(await c.PostAsync($"/api/v1/satinalma/{id}/onayla", null));
        return (id, d.GetProperty("kalemler")[0].GetProperty("id").GetString()!);
    }

    private decimal Stok(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Stoklar.AsNoTracking().Where(s => s.UrunId == k.UrunId && s.SubeId == k.SubeId)
            .Select(s => s.Adet).FirstOrDefault();
    }

    private List<decimal> AlisTl(Kiraci k)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Hareketler.AsNoTracking().Where(h => h.TenantId == k.TenantId && h.Tur == "ALIS")
            .Select(h => h.TlTutari).ToList();
    }

    [Fact]
    public async Task Sinir_sifir_ve_eksi_adet_ile_eksi_fiyat_400_sifir_satir()
    {
        var k = Kur("sifir-eksi");
        var c = f.Ortam.Istemci(k, "SAHIP");
        foreach (var (adet, fiyat) in new[] { (0m, 100m), (-1m, 100m), (1m, -0.01m) })
        {
            var r = await c.PostJson("/api/v1/satinalma", new
            {
                tedarikciCariId = k.CariId,
                kalemler = new[] { new { urunId = k.UrunId, adet, birimFiyatTl = fiyat } },
            });
            Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{adet}/{fiyat}: {(int)r.StatusCode}");
        }
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.SatinalmaSiparisleri.AsNoTracking().Count(s => s.TenantId == k.TenantId));

        var (id, kalem) = await OnayliSiparis(c, k, 2m, 100m);
        var stokOnce = Stok(k);
        var r0 = await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = (decimal?)null } } });
        Assert.Equal(HttpStatusCode.BadRequest, r0.StatusCode);                 // boş/null adet
        Assert.Equal(stokOnce, Stok(k));
        Assert.Empty(AlisTl(k));
    }

    [Fact]
    public async Task Sinir_gram_adet_3_hane_0_0004_reddedilir_0_0005_yukari_yuvarlanir()
    {
        var k = Kur("gram", "GRAM");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var (id, kalem) = await OnayliSiparis(c, k, 1m, 1000m);
        var stokOnce = Stok(k);

        var r = await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 0.0004m } } });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);                  // 0,000'a yuvarlanır → sıfır
        Assert.Equal(stokOnce, Stok(k));

        var ok = await Tamam(await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 0.0005m } } }));
        Assert.Equal("KISMI_KABUL", ok.GetProperty("yeniDurum").GetString());
        Assert.Equal(stokOnce + 0.001m, Stok(k));                               // AwayFromZero: 0,0005 → 0,001
        Assert.Equal(new[] { -1.00m }, AlisTl(k));                              // 0,001 g × 1000 TL = 1,00 TL; ALIŞ = borç (eksi işaret, SatisModulu:1144)
    }

    [Fact]
    public async Task Sinir_kurus_birim_fiyat_1234_565_ve_0_005_yuvarlama()
    {
        var k = Kur("kurus");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var (id, _) = await OnayliSiparis(c, k, 1m, 1234.565m);
        var d = await Tamam(await c.GetAsync($"/api/v1/satinalma/{id}"));
        Assert.Equal(1234.57m, d.GetProperty("kalemler")[0].GetProperty("birimFiyatTl").GetDecimal());

        var (id2, kalem2) = await OnayliSiparis(c, k, 1m, 0.005m);
        await Tamam(await c.PostJson($"/api/v1/satinalma/{id2}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem2, adet = 1m } } }));
        Assert.Equal(new[] { -0.01m }, AlisTl(k));                              // 0,005 → 0,01 (AwayFromZero); ALIŞ eksi işaretli
    }

    /// <summary>
    /// ÇOK BÜYÜK TUTAR (999.999.999,99) — ve genel olarak MASAK eşiği (370.000 TL, kümülatif cari) üstü mal kabul.
    /// Alış servisi bu tutarda MASAK beyanı ister (kimlik, işlem amacı, açıklama ≥ 20). Mal kabul ucu
    /// beyanı TAŞIYABİLMELİ; aksi hâlde eşik üstü toptan alış ekrandan hiç teslim alınamaz (çıkmaz sokak).
    /// Beklenen: beyanla 200, stok +1, ALIŞ −999.999.999,99 (kuruş kaybı yok), kâğıtta fiyat yok.
    /// </summary>
    [Fact]
    public async Task Sinir_cok_buyuk_tutar_999_999_999_99_masak_beyaniyla_mal_kabul_yapilabilir()
    {
        var k = Kur("buyuk");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var (id, kalem) = await OnayliSiparis(c, k, 1m, 999_999_999.99m);
        var stokOnce = Stok(k);
        var masak = new
        {
            belgeTuru = "PASAPORT", belgeNo = "U7654322", adSoyad = "Jane Roe", uyruk = "FRA",
            dogumTarihi = "1980-03-03", adres = "Paris", islemAmaci = "Ticari",
            aciklama = "Toptanciden buyuk tutarli ticari mal alimi sinir testi",
            fonKaynagi = "Ticari gelir", ekBelgeReferansi = "EK-3",
        };
        var r = await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, masak, kalemler = new[] { new { kalemId = kalem, adet = 1m } } });
        var y = await Tamam(r);
        Assert.Equal("KAPANDI", y.GetProperty("yeniDurum").GetString());
        Assert.Equal(stokOnce + 1m, Stok(k));
        Assert.Equal(new[] { -999_999_999.99m }, AlisTl(k));
        var b = await c.GetAsync($"/api/v1/cikti/IRSALIYE/{y.GetProperty("irsaliyeId").GetString()}");
        var metin = await b.Metin();
        Assert.Equal(HttpStatusCode.OK, b.StatusCode);
        Assert.DoesNotContain("999999999", metin);
        Assert.DoesNotContain("999.999.999", metin);
    }

    [Fact]
    public void Sinir_turkce_giris_FormAlani_SayiCoz()
    {
        Assert.Equal(45_000m, FormAlani.SayiCoz("45.000").Deger);
        Assert.Equal(1.5m, FormAlani.SayiCoz("1,5").Deger);
        Assert.Equal(FormAlani.SayiSonucu.Belirsiz, FormAlani.SayiCoz("1.500").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz("").Sonuc);
        Assert.Equal(FormAlani.SayiSonucu.Bos, FormAlani.SayiCoz(null).Sonuc);
    }

    // ── DOĞRULAYICI TUR 2 (2026-10-10): mal kabulde SATIRDA verilen birim fiyat (sipariş fiyatını ezen yol) ──

    [Fact]
    public async Task Sinir_dg2_mal_kabul_satir_fiyati_eksi_400_sifir_satir()
    {
        var k = Kur("dg2-eksi");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var (id, kalem) = await OnayliSiparis(c, k, 2m, 100m);
        var stokOnce = Stok(k);
        var r = await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 1m, birimFiyatTl = -0.01m } } });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(stokOnce, Stok(k));
        Assert.Empty(AlisTl(k));
    }

    [Fact]
    public async Task Sinir_dg2_mal_kabul_satir_fiyati_kurus_1234_565_ve_0_005_yukari()
    {
        var k = Kur("dg2-kurus");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var (id, kalem) = await OnayliSiparis(c, k, 2m, 100m);
        await Tamam(await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 1m, birimFiyatTl = 1234.565m } } }));
        await Tamam(await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 1m, birimFiyatTl = 0.005m } } }));
        var tl = AlisTl(k).OrderBy(x => x).ToList();
        Assert.Equal(new[] { -1234.57m, -0.01m }, tl);                          // AwayFromZero, ALIŞ eksi işaretli
    }

    /// <summary>Satır tutarı (adet × fiyat) TL kolonunu (decimal(15,2)) taşırsa 500 değil 400 ve 0 satır.</summary>
    [Fact]
    public async Task Sinir_dg2_mal_kabul_tutar_tasmasi_500_degil_400_sifir_satir()
    {
        var k = Kur("dg2-tasma");
        var c = f.Ortam.Istemci(k, "SAHIP");
        var (id, kalem) = await OnayliSiparis(c, k, 100_000m, 1m);
        var stokOnce = Stok(k);
        var r = await c.PostJson($"/api/v1/satinalma/{id}/mal-kabul",
            new { kurSnapshotId = k.KurId, kalemler = new[] { new { kalemId = kalem, adet = 100_000m, birimFiyatTl = 999_999_999.99m } } });
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode}: {await r.Metin()}");
        Assert.Equal(stokOnce, Stok(k));
        Assert.Empty(AlisTl(k));
        using var db = f.Vt.OlusturSistem();
        Assert.Equal(0, db.Irsaliyeler.AsNoTracking().Count(i => i.TenantId == k.TenantId));
    }
}
