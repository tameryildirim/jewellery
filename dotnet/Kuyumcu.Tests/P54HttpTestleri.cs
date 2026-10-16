using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-54 — TASLAK FATURA KALEMİ DÜZELTME: GERÇEK HTTP BORU HATTI.
//
// Ekran (Faturalar.razor) kalem hücrelerini satır içi düzenletir ve TEK alan
// gönderir: miktar | birimFiyat | iscilikTl | indirimTutari | aciklama.
// Bu testler ekranın dayandığı sözleşmeyi ölçer:
//   • yanıt KalemYaniti'dır ve başlık toplamı KALEMLERDEN yeniden toplanmıştır
//     (ekran toplamı bu yanıttan okur, kendisi hesaplamaz);
//   • miktar 0 → 400 + Türkçe mesaj, veri DEĞİŞMEZ;
//   • işçilik null = alan silinir, sayı olmayan değer 400 (eskiden 500);
//   • kalem SİLME ucu YOKTUR (ölçüldü) — ekran bu yüzden gerekçe yazar;
//   • kesilmiş belgenin kalemi değişmez (400 + düzeltme yolu).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class P54HttpTestleri(HttpFikstur f)
{
    private Kiraci YeniKiraci(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, ad);
    }

    /// <summary>İki kalemli STANDART taslak: 2×1000 + 500 = 2.500.</summary>
    private static async Task<(string FaturaId, string KalemA, string KalemB)> KalemliTaslak(
        Kiraci k, HttpClient c)
    {
        var y = await c.PostAsJsonAsync("/api/v1/faturalar", new
        {
            cariId = k.CariId, tur = "SATIS", toplam = 2500m, kdvOran = 0.20m,
            kalemler = new object[]
            {
                new { aciklama = "P54 bilezik", miktar = 2m, birimFiyat = 1000m },
                new { aciklama = "P54 kolye", miktar = 1m, birimFiyat = 500m },
            },
        });
        var govde = await y.Content.ReadAsStringAsync();
        Assert.True(y.StatusCode == HttpStatusCode.OK, $"{(int)y.StatusCode} {govde}");
        var id = JsonDocument.Parse(govde).RootElement.GetProperty("id").GetString()!;

        var kalemler = (await (await c.GetAsync($"/api/v1/faturalar/{id}/kalemler")).Json())
            .GetProperty("kalemler").EnumerateArray().ToList();
        Assert.Equal(2, kalemler.Count);
        return (id, kalemler[0].GetProperty("id").GetString()!, kalemler[1].GetProperty("id").GetString()!);
    }

    [Fact]
    public async Task Kalem_miktari_PATCH_ile_duzelir_ve_BASLIK_TOPLAMI_yanitta_gelir()
    {
        var k = YeniKiraci("P54H-MIKTAR");
        var sahip = f.Ortam.Istemci(k);
        var (id, a, _) = await KalemliTaslak(k, sahip);

        var y = await sahip.PatchAsJsonAsync($"/api/v1/faturalar/{id}/kalemler/{a}", new { miktar = 3m });
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var yj = await y.Json();
        // Yanıt KalemYaniti: başlık toplamı 3×1000 + 500 = 3.500 (kalemlerden toplandı).
        Assert.Equal(3500m, yj.GetProperty("toplam").GetDecimal());
        var ka = yj.GetProperty("kalemler").EnumerateArray().Single(x => x.GetProperty("id").GetString() == a);
        Assert.Equal(3m, ka.GetProperty("miktar").GetDecimal());
        Assert.Equal(3000m, ka.GetProperty("satirToplam").GetDecimal());

        using var db = f.Vt.OlusturSistem();
        var fatura = await db.Faturalar.AsNoTracking().SingleAsync(x => x.Id == id && x.TenantId == k.TenantId);
        Assert.Equal(3500m, fatura.Toplam);
        Assert.Equal("TASLAK", fatura.Durum);
    }

    [Fact]
    public async Task Miktar_SIFIR_400_Turkce_mesaj_ve_veri_DEGISMEZ()
    {
        var k = YeniKiraci("P54H-SIFIR");
        var sahip = f.Ortam.Istemci(k);
        var (id, a, _) = await KalemliTaslak(k, sahip);

        var y = await sahip.PatchAsJsonAsync($"/api/v1/faturalar/{id}/kalemler/{a}", new { miktar = 0m });
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
        // PAKET GOC-A: ileti Core K10 kuralının sözlük metni (ekranın alan altı metniyle AYNI), alan adı önekli.
        Assert.Equal("Miktar: " + Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(new Kuyumcu.Core.Domain.Girdi.GirdiHatasi(
            Kuyumcu.Core.Domain.Girdi.GirdiKodu.SayiSifir), "tr"), (await y.Json()).GetProperty("message").GetString());

        using var db = f.Vt.OlusturSistem();
        var kalem = await db.FaturaKalemleri.AsNoTracking().SingleAsync(x => x.Id == a && x.TenantId == k.TenantId);
        Assert.Equal(2m, kalem.Miktar);
        Assert.Equal(2500m, (await db.Faturalar.AsNoTracking()
            .SingleAsync(x => x.Id == id && x.TenantId == k.TenantId)).Toplam);
    }

    [Fact]
    public async Task Iscilik_NULL_ile_silinir_SAYI_OLMAYAN_deger_400_doner()
    {
        var k = YeniKiraci("P54H-ISCILIK");
        var sahip = f.Ortam.Istemci(k);
        var (id, a, _) = await KalemliTaslak(k, sahip);

        var ekle = await sahip.PatchAsJsonAsync($"/api/v1/faturalar/{id}/kalemler/{a}", new { iscilikTl = 120m });
        Assert.Equal(HttpStatusCode.OK, ekle.StatusCode);
        Assert.Equal(2620m, (await ekle.Json()).GetProperty("toplam").GetDecimal());

        // PATCH sözleşmesi: null = SİL.
        var sil = await sahip.PatchAsync($"/api/v1/faturalar/{id}/kalemler/{a}",
            new StringContent("""{"iscilikTl":null}""", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, sil.StatusCode);
        var sj = await sil.Json();
        Assert.Equal(2500m, sj.GetProperty("toplam").GetDecimal());
        var ka = sj.GetProperty("kalemler").EnumerateArray().Single(x => x.GetProperty("id").GetString() == a);
        Assert.Equal(JsonValueKind.Null, ka.GetProperty("iscilikTl").ValueKind);

        // Sayı olmayan değer → 400 (500 değil), Türkçe mesaj alan adını söyler.
        var bozuk = await sahip.PatchAsync($"/api/v1/faturalar/{id}/kalemler/{a}",
            new StringContent("""{"indirimTutari":"abc"}""", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, bozuk.StatusCode);
        Assert.Contains("indirimTutari", (await bozuk.Json()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task Kalem_SILME_ucu_YOKTUR_ve_kalem_yerinde_kalir()
    {
        var k = YeniKiraci("P54H-SILME");
        var sahip = f.Ortam.Istemci(k);
        var (id, a, _) = await KalemliTaslak(k, sahip);

        var y = await sahip.DeleteAsync($"/api/v1/faturalar/{id}/kalemler/{a}");
        Assert.False(y.IsSuccessStatusCode, $"DELETE beklenmedik biçimde başarılı: {(int)y.StatusCode}");
        Assert.True(y.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed,
            $"beklenen 404/405, gelen {(int)y.StatusCode}");

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(2, await db.FaturaKalemleri.AsNoTracking()
            .CountAsync(x => x.FaturaId == id && x.TenantId == k.TenantId));
    }

    [Fact]
    public async Task Kesilmis_faturanin_kalemi_400_ve_DUZELTME_YOLU_yazar()
    {
        var k = YeniKiraci("P54H-KESILDI");
        var sahip = f.Ortam.Istemci(k);
        var (id, a, _) = await KalemliTaslak(k, sahip);

        var kes = await sahip.PatchAsJsonAsync($"/api/v1/faturalar/{id}/durum", new { durum = "KESILDI" });
        Assert.Equal(HttpStatusCode.OK, kes.StatusCode);

        var y = await sahip.PatchAsJsonAsync($"/api/v1/faturalar/{id}/kalemler/{a}", new { miktar = 5m });
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
        Assert.Contains("iptal edip yeni belge", (await y.Json()).GetProperty("message").GetString());

        using var db = f.Vt.OlusturSistem();
        Assert.Equal(2m, (await db.FaturaKalemleri.AsNoTracking()
            .SingleAsync(x => x.Id == a && x.TenantId == k.TenantId)).Miktar);
    }
}
