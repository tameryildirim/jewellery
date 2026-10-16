using System.Net;
using System.Text;
using System.Text.Json;
using Kuyumcu.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Tests.Dogrulayici;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET PATCH-2 — HASIM DOĞRULAYICI FİNANSAL SINIR TESTLERİ (Aşkın + Mehmet)
//
// Uygulayıcıdan BAĞIMSIZ yazıldı; beklenen değerler SABİT (üretim kodundan
// hesaplanmaz). Paketin dokunduğu yollar:
//   • Genel serileştirici: tırnaklı sayı ("1.500", "45.000", "1,5") her DTO'lu yazma ucunda 400
//   • JSON tür hatası 400 gövdesi: anahtar "alan" (JSON yolu "$." öneki yok), "dto" yok
//   • Fatura taslak kalem PATCH miktarı: ÖNCE 3 haneye (AwayFromZero) yuvarla, SONRA > 0
// Sınırlar: 0 · eksi · yarım bin (0,0025 → 0,003 AwayFromZero; ToEven 0,002 verirdi) ·
// gram 4 hane (0,0001) · çok büyük (999.999.999,99) · null · Türkçe dize.
// ═════════════════════════════════════════════════════════════════════════════

[Collection("http")]
public sealed class PATCH2SinirHttpTestleri(HttpFikstur f)
{
    private Kiraci Kur(string ad)
    {
        using var db = f.Vt.OlusturSistem();
        return TestKurulum.KiraciKur(db, $"PATCH2SINIR {ad}");
    }

    private static Task<HttpResponseMessage> Ham(HttpClient c, HttpMethod m, string yol, string json) =>
        c.SendAsync(new HttpRequestMessage(m, yol) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    /// <summary>400 + alanlar içinde <paramref name="alan"/>; "$" önekli anahtar ve "dto" YOK.</summary>
    private static async Task Alan400(HttpResponseMessage r, string alan)
    {
        var govde = await r.Content.ReadAsStringAsync();
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{(int)r.StatusCode} {govde}");
        var alanlar = JsonDocument.Parse(govde).RootElement.GetProperty("alanlar");
        var anahtarlar = alanlar.EnumerateObject().Select(p => p.Name).ToList();
        Assert.True(anahtarlar.Contains(alan), $"alanlar.{alan} yok: {govde}");
        Assert.DoesNotContain(anahtarlar, a => a.StartsWith('$'));
        Assert.DoesNotContain("dto", anahtarlar);
    }

    private (decimal gram, decimal? satis) Urun(string id)
    {
        using var o = f.Vt.OlusturSistem();
        var u = o.Urunler.AsNoTracking().Single(x => x.Id == id);
        return (u.GramAgirlik, u.SatisFiyati);
    }

    private async Task<(string fid, string kid, HttpClient c)> Taslak(string ad)
    {
        var k = Kur(ad);
        var c = f.Ortam.Istemci(k);
        var y = await Ham(c, HttpMethod.Post, "/api/v1/faturalar",
            "{\"cariId\":\"" + k.CariId + "\",\"tur\":\"SATIS\",\"toplam\":1200,\"kdvOran\":0.20,"
            + "\"kalemler\":[{\"aciklama\":\"Sinir\",\"miktar\":1,\"birimFiyat\":1200}]}");
        var g = await y.Content.ReadAsStringAsync();
        Assert.True(y.StatusCode == HttpStatusCode.OK, g);
        var fid = JsonDocument.Parse(g).RootElement.GetProperty("id").GetString()!;
        using var o = f.Vt.OlusturSistem();
        var kid = o.FaturaKalemleri.AsNoTracking().Single(x => x.FaturaId == fid).Id;
        return (fid, kid, c);
    }

    private (decimal miktar, decimal satir, decimal toplam) Kalem(string fid, string kid)
    {
        using var o = f.Vt.OlusturSistem();
        var kl = o.FaturaKalemleri.AsNoTracking().Single(x => x.Id == kid);
        return (kl.Miktar, kl.SatirToplam, o.Faturalar.AsNoTracking().Single(x => x.Id == fid).Toplam);
    }

    // ── sıfır / eksi / null — fatura kalem miktarı ──────────────────────────

    [Theory]
    [InlineData("0")]
    [InlineData("-0.0004")]
    [InlineData("-1")]
    [InlineData("null")]
    [InlineData("0.0004999")]
    public async Task PS2_fatura_kalem_PATCH_miktar_sifir_eksi_null_400_alan_miktar_DEGISMEZ(string deger)
    {
        var (fid, kid, c) = await Taslak("miktar " + deger);
        var r1 = await Ham(c, HttpMethod.Patch, $"/api/v1/faturalar/{fid}/kalemler/{kid}", "{\"iscilikTl\":100}");
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal((1.000m, 1300.00m, 1300.00m), Kalem(fid, kid));
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/faturalar/{fid}/kalemler/{kid}",
            "{\"miktar\":" + deger + "}"), "miktar");
        Assert.Equal((1.000m, 1300.00m, 1300.00m), Kalem(fid, kid));
    }

    // ── yarım bin: PAKET GOC-A (K10) — 4. ondalık YUVARLANMAZ (eski: 0,0025 → 0,003 AwayFromZero) ──
    // Fazla ondalık 400 alan:miktar, kalem DEĞİŞMEZ; 3 ondalıklı 0,003 aynen yazılır (3,60).

    [Fact]
    public async Task PS2_fatura_kalem_PATCH_miktar_0_0025_yuvarlanmaz_400_0_003_satir_3_60()
    {
        var (fid, kid, c) = await Taslak("miktar yarim bin");
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/faturalar/{fid}/kalemler/{kid}", "{\"miktar\":0.0025}"), "miktar");
        Assert.Equal((1.000m, 1200.00m, 1200.00m), Kalem(fid, kid));
        var r = await Ham(c, HttpMethod.Patch, $"/api/v1/faturalar/{fid}/kalemler/{kid}", "{\"miktar\":0.003}");
        Assert.True(r.StatusCode == HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        // 0,003 × 1200 = 3,60
        Assert.Equal((0.003m, 3.60m, 3.60m), Kalem(fid, kid));
    }

    // ── Türkçe dize — fatura kalem PATCH (JsonElement yolu) ─────────────────

    [Theory]
    [InlineData("\"45.000\"")]
    [InlineData("\"1,5\"")]
    [InlineData("\"1.500\"")]
    public async Task PS2_fatura_kalem_PATCH_Turkce_dize_miktar_400_DEGISMEZ(string deger)
    {
        var (fid, kid, c) = await Taslak("miktar dize");
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/faturalar/{fid}/kalemler/{kid}",
            "{\"miktar\":" + deger + "}"), "miktar");
        Assert.Equal((1.000m, 1200.00m, 1200.00m), Kalem(fid, kid));
    }

    // ── ürün: gram 4 hane, çok büyük tutar, Türkçe dize ──────────────────────

    [Fact]
    public async Task PS2_urun_PATCH_gram_4_hane_0_0001_birebir()
    {
        var k = Kur("gram 4 hane");
        var r = await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}", "{\"gramAgirlik\":1.0001}");
        Assert.True(r.StatusCode == HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        Assert.Equal(1.0001m, Urun(k.UrunId).gram);
    }

    [Fact]
    public async Task PS2_urun_PATCH_cok_buyuk_999999999_99_SAYI_birebir_DIZE_400()
    {
        var k = Kur("cok buyuk");
        var c = f.Ortam.Istemci(k);
        var r = await Ham(c, HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}", "{\"satisFiyati\":999999999.99}");
        Assert.True(r.StatusCode == HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        Assert.Equal(999_999_999.99m, Urun(k.UrunId).satis);
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}",
            "{\"satisFiyati\":\"999.999.999,99\"}"), "satisFiyati");
        Assert.Equal(999_999_999.99m, Urun(k.UrunId).satis);
    }

    [Theory]
    [InlineData("satisFiyati", "\"45.000\"")]
    [InlineData("satisFiyati", "\"1.500\"")]
    [InlineData("gramAgirlik", "\"1,5\"")]
    [InlineData("gramAgirlik", "\"1.5000\"")]
    [InlineData("alisMaliyeti", "\"2.500\"")]
    public async Task PS2_urun_PATCH_Turkce_dize_400_alan_temiz_SQL_DEGISMEZ(string alan, string deger)
    {
        var k = Kur("urun dize");
        var once = Urun(k.UrunId);
        await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Patch, $"/api/v1/urunler/{k.UrunId}",
            "{\"" + alan + "\":" + deger + "}"), alan);
        Assert.Equal(once, Urun(k.UrunId));
    }

    // ── genel ayar: paketin dokunmadığı DTO'lu uç (rezervasyon POST) ─────────

    [Theory]
    [InlineData("\"1.500\"")]
    [InlineData("1e30")]
    public async Task PS2_GENEL_rezerv_POST_adet_dize_ve_1e30_400_alan_adet_rezerv_ACILMAZ(string deger)
    {
        var k = Kur("rezerv post");
        int Sayi() { using var o = f.Vt.OlusturSistem(); return o.Rezervasyonlar.Count(x => x.TenantId == k.TenantId); }
        await Alan400(await Ham(f.Ortam.Istemci(k), HttpMethod.Post, "/api/v1/islemler/rezervler",
            "{\"urunId\":\"" + k.UrunId + "\",\"cariId\":\"" + k.CariId + "\",\"adet\":" + deger + "}"), "adet");
        Assert.Equal(0, Sayi());
    }
    // ── genel ayar KAPSAMAYAN yol: JsonElement gövdeli PATCH (rezervasyon adedi) ──
    // HASIM BULGUSU (PATCH-2 doğrulama, 02.10): SatisModulu.P21.cs RezervGuncelle `adOge.GetDecimal()`
    // dize/aralık dışı sayıda InvalidOperationException/FormatException → 500. Düzeltilince Skip kaldırılır.
    // PAKET PATCH-3: düzeltildi (RezervGuncelle → KolonTavani.JsonSayi) — Skip kaldırıldı.
    [Theory]
    [InlineData("\"1.500\"")]
    [InlineData("1e30")]
    public async Task PS2_rezerv_PATCH_adet_dize_ve_1e30_400_alan_adet_500_DEGIL(string deger)
    {
        var k = Kur("rezerv patch");
        var c = f.Ortam.Istemci(k);
        var y = await Ham(c, HttpMethod.Post, "/api/v1/islemler/rezervler",
            "{\"urunId\":\"" + k.UrunId + "\",\"cariId\":\"" + k.CariId + "\",\"adet\":1}");
        var g = await y.Content.ReadAsStringAsync();
        Assert.True((int)y.StatusCode is 200 or 201, g);
        var id = JsonDocument.Parse(g).RootElement.GetProperty("id").GetString()!;
        decimal Adet() { using var o = f.Vt.OlusturSistem(); return o.Rezervasyonlar.AsNoTracking().Single(x => x.Id == id).Adet; }
        await Alan400(await Ham(c, HttpMethod.Patch, $"/api/v1/islemler/rezervler/{id}", "{\"adet\":" + deger + "}"), "adet");
        Assert.Equal(1.000m, Adet());
    }
}
