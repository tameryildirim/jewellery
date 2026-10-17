using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// 18.10 — müşteri kartında EKSİK TC/VKN'nin tamamlanması (kurucu: "Faturalog kadar sade… müşteri hemen
// bulunacak… faturaya direkt çevrilecek"). Kuyumcu fatura haddi üstünde alıcı kimliği zorunlu; kartta yoksa
// tezgâhta yazılır ya da banka havalesinden öğrenilir. Kayıtlı numara ASLA ezilmez.
// ═════════════════════════════════════════════════════════════════════════════
[Collection("http")]
public sealed class MusteriKimlikTamamlaHttpTestleri(HttpFikstur f)
{
    private const string Tc1 = "10000000146";
    private const string Tc2 = "10000000078";

    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, "KIM-" + ad);
    }

    private string? VergiNo(Kiraci k, string cariId)
    {
        using var db = f.Vt.OlusturSistem();
        return db.Cariler.AsNoTracking().Single(c => c.Id == cariId && c.TenantId == k.TenantId).VergiNo;
    }

    private async Task<string> CariAc(Kiraci k, string ad, string? tc = null)
    {
        var c = await f.Ortam.Istemci(k, "SAHIP").PostAsJsonAsync("/api/v1/cari", new { tur = "MUSTERI", ad, vergiNo = tc });
        return (await c.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Tezgahtar_bos_karta_TC_yazar_kayitli_numara_EZILMEZ_baskasinin_numarasi_409()
    {
        var k = YeniKiraci("uc");
        var a = await CariAc(k, "Bos Kartli");
        var b = await CariAc(k, "Dolu Kartli", Tc2);
        var t = f.Ortam.Istemci(k, "TEZGAHTAR");

        var r1 = await t.PostAsJsonAsync("/api/v1/islemler/musteri-kimlik", new { cariId = a, kimlikNo = Tc1 });
        Assert.True(r1.IsSuccessStatusCode, await r1.Content.ReadAsStringAsync());
        Assert.Equal(Tc1, VergiNo(k, a));
        Assert.True((await t.PostAsJsonAsync("/api/v1/islemler/musteri-kimlik", new { cariId = a, kimlikNo = Tc1 })).IsSuccessStatusCode);   // aynı → sorun yok

        Assert.Equal(HttpStatusCode.Conflict, (await t.PostAsJsonAsync("/api/v1/islemler/musteri-kimlik", new { cariId = b, kimlikNo = Tc1 })).StatusCode);   // kartında başkası
        Assert.Equal(Tc2, VergiNo(k, b));
        var c = await CariAc(k, "Ucuncu");
        var cak = await t.PostAsJsonAsync("/api/v1/islemler/musteri-kimlik", new { cariId = c, kimlikNo = Tc2 });
        Assert.Equal(HttpStatusCode.Conflict, cak.StatusCode);
        Assert.Contains("Dolu Kartli", (await cak.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
        Assert.Null(VergiNo(k, c));

        Assert.Equal(HttpStatusCode.BadRequest, (await t.PostAsJsonAsync("/api/v1/islemler/musteri-kimlik", new { cariId = c, kimlikNo = "11111111111" })).StatusCode);
        var pesin = (await (await t.GetAsync("/api/v1/cari/sistem-carisi")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.BadRequest, (await t.PostAsJsonAsync("/api/v1/islemler/musteri-kimlik", new { cariId = pesin, kimlikNo = Tc1 })).StatusCode);
    }

    [Fact]
    public async Task Havale_TC_tasiyorsa_IBANdan_bulunan_musterinin_bos_kartina_TC_yazilir()
    {
        var k = YeniKiraci("havale");
        var s = f.Ortam.Istemci(k, "SAHIP");
        var a = await CariAc(k, "Havaleci Musteri");
        var iban = TestIban.Uret();
        Assert.True((await s.PostAsJsonAsync($"/api/v1/cari/{a}/ibanlar", new { iban })).IsSuccessStatusCode);
        var hc = await s.PostAsJsonAsync("/api/v1/bankalar", new { bankaAdi = "Demo Bank", hesapAdi = "Vadesiz", iban = TestIban.Uret(), paraBirimi = "TRY" });
        var hesap = (await hc.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var d = await s.PostAsJsonAsync($"/api/v1/bankalar/{hesap}/hareket", new { yon = "GELEN", tutar = 1000m, karsiTarafAd = "HAVALECI MUSTERI", karsiIban = iban, karsiKimlikNo = Tc1 });
        Assert.True(d.IsSuccessStatusCode, await d.Content.ReadAsStringAsync());
        Assert.Equal(Tc1, VergiNo(k, a));
    }

    [Fact]
    public async Task Fatura_haddi_ustu_satis_TCsiz_kartta_400_TC_yazilinca_ayni_satis_faturali_gecer()
    {
        var k = YeniKiraci("satis");
        var a = await CariAc(k, "Buyuk Alici");
        decimal birim;
        using (var db = f.Vt.OlusturSistem())
        {
            var kur = db.KurSnapshots.AsNoTracking().Single(x => x.Id == k.KurId).AltinHasFiyati;
            birim = Math.Ceiling(k.UrunHasGrami * kur * 1.10m);
        }
        var adet = Math.Ceiling(40_000m / birim);
        object Fis() => new
        {
            cariId = a,
            kalemler = new[] { new { urunId = k.UrunId, adet, birimFiyatTl = birim, hediye = false } },
            odemeler = new[] { new { arac = "KART", tutarTl = birim * adet, kasaId = k.KasaId } },
            kurSnapshotId = k.KurId, fisIskontoYuzde = 0m, belgeTuru = "FATURA",
        };
        var s = f.Ortam.Istemci(k, "SAHIP");
        var r1 = await s.PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis());
        Assert.Equal(HttpStatusCode.BadRequest, r1.StatusCode);
        Assert.Contains("cariId", await r1.Content.ReadAsStringAsync());

        Assert.True((await f.Ortam.Istemci(k, "TEZGAHTAR").PostAsJsonAsync("/api/v1/islemler/musteri-kimlik", new { cariId = a, kimlikNo = Tc1 })).IsSuccessStatusCode);
        var r2 = await s.PostAsJsonAsync("/api/v1/islemler/satis-fisi", Fis());
        Assert.True(r2.IsSuccessStatusCode, await r2.Content.ReadAsStringAsync());
        using var db2 = f.Vt.OlusturSistem();
        var fatura = await db2.Faturalar.AsNoTracking().SingleAsync(x => x.TenantId == k.TenantId);
        Assert.Equal("KESILDI", fatura.Durum);
        Assert.Equal(a, fatura.CariId);
    }
}
