using System.Net;
using System.Net.Http.Json;
using Kuyumcu.Contracts.Cikti;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// D1-01 (18.10, rakip eşitleme) — kesilen faturanın kâğıt dökümü: /api/v1/cikti/FATURA/{id}.
// Kalemler, KDV matrahı, KDV ve genel toplam faturanın kendi kalemleriyle AYNI; "bilgi amaçlı döküm"
// uyarısı her zaman basılır; başka işletmenin faturası 404.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class D1FaturaCiktiTestleri(HttpFikstur f)
{
    [Fact]
    public async Task Kesilen_fatura_dokumu_kalem_ve_toplamlari_faturayla_ayni_uyari_basilir_baska_isletme_404()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1-fatura");
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
            odemeler = new[] { new { arac = "KART", tutarTl = birim, kasaId = k.KasaId } },
            kurSnapshotId = k.KurId, fisIskontoYuzde = 0m, belgeTuru = "FATURA",
        });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());

        Kuyumcu.Api.Data.Fatura fatura;
        List<Kuyumcu.Api.Data.FaturaKalemi> kalemler;
        using (var db = f.Vt.OlusturSistem())
        {
            fatura = await db.Faturalar.AsNoTracking().SingleAsync(x => x.TenantId == k.TenantId);
            kalemler = await db.FaturaKalemleri.AsNoTracking().Where(x => x.FaturaId == fatura.Id).ToListAsync();
        }
        Assert.Equal("KESILDI", fatura.Durum);

        var b = await s.GetFromJsonAsync<OrtakBelge>($"/api/v1/cikti/{BelgeTurleri.Fatura}/{fatura.Id}");
        Assert.NotNull(b);
        Assert.Equal("FATURA", b!.BelgeAdi);
        Assert.Equal(fatura.FaturaNo, b.BelgeNo);
        Assert.False(b.BelgeNoKayitKimligi);
        Assert.Contains("BİLGİ AMAÇLI", b.Uyari);
        Assert.Equal("Alıcı", b.Karsi!.Baslik);
        Assert.Equal("10000000146", b.Karsi.VergiNo);

        var bolum = Assert.Single(b.Bolumler);
        Assert.Equal(["Sıra", "Açıklama", "Miktar", "Gram", "Has gram", "Birim fiyat", "KDV", "Tutar"], bolum.SutunBasliklari);
        Assert.Equal(kalemler.Count, bolum.Satirlar.Count);

        decimal Tutar(string etiket) => b.Tutarlar.Single(t => t.Etiket == etiket).Deger;
        Assert.Equal(kalemler.Sum(x => x.KdvMatrah), Tutar("KDV matrahı"));
        Assert.Equal(kalemler.Sum(x => x.KdvTutar), Tutar("KDV"));
        Assert.Equal(kalemler.Sum(x => x.SatirToplam), Tutar("Genel toplam"));
        Assert.True(b.Tutarlar.Single(t => t.Etiket == "Genel toplam").Toplam);
        Assert.Equal(fatura.Toplam, Tutar("Genel toplam"));

        // Başka işletme aynı kimlikle faturayı göremez.
        Kiraci baska;
        using (var db = f.Vt.OlusturSistem()) baska = TestKurulum.KiraciKur(db, "D1-fatura-baska");
        Assert.Equal(HttpStatusCode.NotFound,
            (await f.Ortam.Istemci(baska, "SAHIP").GetAsync($"/api/v1/cikti/{BelgeTurleri.Fatura}/{fatura.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await s.GetAsync($"/api/v1/cikti/{BelgeTurleri.Fatura}/01ZZZZZZZZZZZZZZZZZZZZZZZZ")).StatusCode);
    }

    [Fact]
    public async Task Iptal_edilen_faturanin_kagidinda_iptal_uyarisi_var()
    {
        Kiraci k;
        using (var db = f.Vt.OlusturSistem()) k = TestKurulum.KiraciKur(db, "D1-iptal");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var c = await s.PostAsJsonAsync("/api/v1/faturalar", new { cariId = k.CariId, tur = "SATIS", kdvTuru = "STANDART", toplam = 1200m, kdvOran = 0.20m });
        Assert.True(c.IsSuccessStatusCode, await c.Content.ReadAsStringAsync());
        string id;
        using (var db = f.Vt.OlusturSistem()) id = db.Faturalar.AsNoTracking().Single(x => x.TenantId == k.TenantId).Id;

        var taslak = await s.GetFromJsonAsync<OrtakBelge>($"/api/v1/cikti/{BelgeTurleri.Fatura}/{id}");
        Assert.StartsWith("TASLAKTIR", taslak!.Uyari);
        Assert.True(taslak.BelgeNoKayitKimligi);   // taslakta fatura numarası yok — numara uydurulmaz

        var d = await s.PatchAsJsonAsync($"/api/v1/faturalar/{id}/durum", new { durum = "IPTAL" });
        Assert.True(d.IsSuccessStatusCode, await d.Content.ReadAsStringAsync());
        var iptal = await s.GetFromJsonAsync<OrtakBelge>($"/api/v1/cikti/{BelgeTurleri.Fatura}/{id}");
        Assert.StartsWith("BU FATURA İPTAL", iptal!.Uyari);
    }
}
