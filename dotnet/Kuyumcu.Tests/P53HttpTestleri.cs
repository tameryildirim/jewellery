using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-53 — TASLAK FATURA DÜZENLEME: GERÇEK HTTP BORU HATTI.
//
// Ekran (Faturalar.razor) kuyum taslağında toplamı KİLİTLER ve yalnız
// cari/tarih/açıklama gönderir. Bu testler o gövdenin sunucuda 200 döndüğünü,
// toplam gönderilirse 400'ün sürdüğünü (aynanın öbür yüzü) ve standart
// taslakta toplamın düzenlenebildiğini ölçer.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class P53HttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    private static async Task<string> IkinciCari(HttpClient c)
    {
        var y = await c.PostAsJsonAsync("/api/v1/cari",
            new { ad = "P53 İkinci Müşteri", tur = "MUSTERI" });
        Assert.Equal(HttpStatusCode.Created, y.StatusCode);
        return (await y.Json()).GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task KuyumTaslaginaCariGuncellemesi200Doner()
    {
        var k = YeniKiraci("P53H-KUYUM");
        var sahip = f.Ortam.Istemci(k);

        // ELLE kaynaklı kuyum taslağı — 17.10 P0-5'ten beri API bu yolu AÇMAZ; P0-5 öncesi doğmuş
        // taslakların düzeltilebildiği (geçmiş veri) doğrudan tabloya yazılarak sınanır.
        var id = Kuyumcu.Api.Common.Kimlik.Ulid();
        using (var tohum = f.Vt.OlusturSistem())
        {
            tohum.Faturalar.Add(new Kuyumcu.Api.Data.Fatura
            {
                Id = id, TenantId = k.TenantId, CariId = k.CariId, Tur = "SATIS", KdvTuru = "KUYUM",
                HasDeger = 500m, Toplam = 1200m, KdvOran = 0.20m, KdvTutar = 116.67m, AraToplam = 1083.33m,
                Durum = "TASLAK", HasDegerKaynagi = "ELLE", Tarih = DateTime.UtcNow,
                Olusturuldu = DateTime.UtcNow, Guncellendi = DateTime.UtcNow,
            });
            tohum.SaveChanges();
        }

        // Ekranın gönderdiği gövde (toplam YOK) → 200 ve alanlar yansır.
        var yeniCari = await IkinciCari(sahip);
        var y = await sahip.PatchAsJsonAsync($"/api/v1/faturalar/{id}",
            new { cariId = yeniCari, tarih = "2026-09-20", aciklama = "P53 kuyum düzeltme" });
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var yj = await y.Json();
        Assert.Equal(yeniCari, yj.GetProperty("cariId").GetString());
        Assert.Equal("P53 kuyum düzeltme", yj.GetProperty("aciklama").GetString());
        Assert.Equal(1200m, yj.GetProperty("toplam").GetDecimal());

        // Listede yansır (ekranın okuduğu uç).
        var liste = await (await sahip.GetAsync("/api/v1/faturalar")).Json();
        var satir = liste.GetProperty("faturalar").EnumerateArray()
            .Single(x => x.GetProperty("id").GetString() == id);
        Assert.Equal(yeniCari, satir.GetProperty("cariId").GetString());
        Assert.Equal("P53 İkinci Müşteri", satir.GetProperty("cariAd").GetString());
        Assert.Equal("KUYUM", satir.GetProperty("kdvTuru").GetString());

        // Aynanın öbür yüzü: toplam GÖNDERİLİRSE sunucu 400 ile reddeder ve
        // düzeltme yolunu metinde yazar.
        var red = await sahip.PatchAsJsonAsync($"/api/v1/faturalar/{id}", new { toplam = 1500m });
        Assert.Equal(HttpStatusCode.BadRequest, red.StatusCode);
        var mesaj = (await red.Json()).GetProperty("message").GetString()!;
        Assert.Contains("Taslağı iptal edip yeniden kesin", mesaj);

        using var db = f.Vt.OlusturSistem();
        var kayit = await db.Faturalar.AsNoTracking()
            .SingleAsync(x => x.Id == id && x.TenantId == k.TenantId);
        Assert.Equal(1200m, kayit.Toplam);
        Assert.Equal(yeniCari, kayit.CariId);
    }

    [Fact]
    public async Task StandartTaslaktaToplamDuzenlenebilir()
    {
        var k = YeniKiraci("P53H-STANDART");
        var sahip = f.Ortam.Istemci(k);

        var olustur = await sahip.PostAsJsonAsync("/api/v1/faturalar",
            new { cariId = k.CariId, tur = "SATIS", toplam = 1200m, kdvOran = 0.20m });
        Assert.Equal(HttpStatusCode.OK, olustur.StatusCode);
        var id = (await olustur.Json()).GetProperty("id").GetString()!;

        var y = await sahip.PatchAsJsonAsync($"/api/v1/faturalar/{id}", new { toplam = 1500m });
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var yj = await y.Json();
        Assert.Equal(1500m, yj.GetProperty("toplam").GetDecimal());
        Assert.Equal(250m, yj.GetProperty("kdvTutar").GetDecimal());
        Assert.Equal(1250m, yj.GetProperty("araToplam").GetDecimal());
    }
}
